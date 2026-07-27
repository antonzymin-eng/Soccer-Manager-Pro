// File:     src/match-client-web/MatchClientRouter.cs
// Created:  2026-07-27
// Modified: 2026-07-27
// Author:   —
// Spec:     Path-to-playable roadmap §7 (B6, option (b)); interactive Unity client design note
//           §5-P2/§6.4 (playback is not a game command); UI / Client Framework #38 §3.3;
//           Match Analytics #37; Code Standards #20
// Purpose:  Maps a request path + query onto the client's three surfaces — read a frame, change
//           playback, express a manager intent — plus the statistics report. Pure with respect to
//           transport, so every routing decision is testable without a socket.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

using UnityEngine;

using TacticalDirector.MatchEngine;
using TacticalDirector.MatchViewer;
using TacticalDirector.TacticalInstructions;
using TacticalDirector.UiFramework;

namespace TacticalDirector.MatchClientWeb
{
    /// <summary>
    /// The client's routing table over a <see cref="MatchClientHost"/>.
    ///
    /// <para><b>The route split is the privilege split.</b> <c>/frame</c> and <c>/report</c> read;
    /// <c>/playback</c> changes when ticks happen but never what is in them (so it is deliberately
    /// NOT a manager command and never touches the queue — §6.4); <c>/intent</c> is the only route
    /// that can change the match, and it goes through the queue so the change lands on a tick
    /// boundary and appears in the replay log. Collapsing these into one endpoint would make that
    /// distinction a convention instead of a structure.</para>
    ///
    /// <para><b>Everything is refused loudly.</b> An unparseable enum, an out-of-range team, an
    /// unknown action: 400 with the reason. Never a silent default — an intent that quietly became
    /// <c>Balanced</c> would be indistinguishable from one the manager meant.</para>
    /// </summary>
    public sealed class MatchClientRouter
    {
        private readonly MatchClientHost _host;

        /// <summary>Routes over <paramref name="host"/>.</summary>
        /// <exception cref="ArgumentNullException"><paramref name="host"/> is null.</exception>
        public MatchClientRouter(MatchClientHost host)
        {
            _host = host ?? throw new ArgumentNullException(nameof(host));
        }

        /// <summary>
        /// Routes one request. <paramref name="method"/> and <paramref name="pathAndQuery"/> are the
        /// first two tokens of an HTTP request line.
        /// </summary>
        public MatchClientResponse Route(string method, string pathAndQuery)
        {
            if (method != "GET")
            {
                return MatchClientResponse.Text(405, "Method Not Allowed");
            }
            if (string.IsNullOrEmpty(pathAndQuery))
            {
                return MatchClientResponse.Text(400, "Bad Request: empty path");
            }

            string path  = pathAndQuery;
            string query = string.Empty;
            int q = pathAndQuery.IndexOf('?');
            if (q >= 0)
            {
                path  = pathAndQuery.Substring(0, q);
                query = pathAndQuery.Substring(q + 1);
            }

            switch (path)
            {
                case "/":
                case "/index.html":
                    return MatchClientResponse.Html(MatchClientPage.Build(_host.Session.Streamer));
                case "/frame":
                    return MatchClientResponse.Json(FrameJson());
                case "/report":
                    return MatchClientResponse.Json(ReportJson());
                case "/playback":
                    return Playback(ParseQuery(query));
                case "/intent":
                    return Intent(ParseQuery(query));
                default:
                    return MatchClientResponse.Text(404, "Not Found");
            }
        }

        // ── /frame — the #38 projection, serialized ─────────────────────────────────────────────

        private string FrameJson()
        {
            MatchFrameView view = _host.Project();
            var sb = new StringBuilder(2_048);
            sb.Append('{');

            if (view.IsEmpty)
            {
                // Distinguishable from tick 0 with everything at the origin: a View must be able to
                // tell "nothing has happened yet" from "the match is at kickoff".
                sb.Append("\"ready\":false}");
                return sb.ToString();
            }

            sb.Append("\"ready\":true");
            Num(sb, ",\"tick\":", view.Tick);
            Num(sb, ",\"homeScore\":", view.HomeScore);
            Num(sb, ",\"awayScore\":", view.AwayScore);
            sb.Append(",\"matchEnded\":").Append(view.MatchEnded ? "true" : "false");
            sb.Append(",\"period\":\"").Append(view.Period).Append('"');
            sb.Append(",\"lastRestart\":\"").Append(view.LastRestart).Append('"');
            Num(sb, ",\"lastRestartTeam\":", view.LastRestartTeam);
            Num(sb, ",\"lastRestartTick\":", view.LastRestartTick);
            Num(sb, ",\"possessingAgentId\":", view.PossessingAgentId);

            sb.Append(",\"ball\":[");
            Num(sb, string.Empty, view.BallPosition.x);
            Num(sb, ",", view.BallPosition.y);
            Num(sb, ",", view.BallPosition.z);
            sb.Append(']');

            sb.Append(",\"agents\":[");
            IReadOnlyList<Vector2> positions = view.AgentPositions;
            IReadOnlyList<LiveAgentCue> cues = view.AgentCues;
            for (int i = 0; i < positions.Count; i++)
            {
                if (i > 0) { sb.Append(','); }
                sb.Append('{');
                Num(sb, "\"x\":", positions[i].x);
                Num(sb, ",\"y\":", positions[i].y);
                LiveAgentCue cue = i < cues.Count ? cues[i] : default;
                Num(sb, ",\"yellow\":", cue.YellowCards);
                sb.Append(",\"off\":").Append(cue.IsSentOff ? "true" : "false");
                sb.Append(",\"sub\":").Append(cue.IsSubstitute ? "true" : "false");
                sb.Append('}');
            }
            sb.Append(']');

            sb.Append(",\"subsUsed\":[");
            IReadOnlyList<int> subs = view.SubstitutionsUsed;
            for (int i = 0; i < subs.Count; i++)
            {
                if (i > 0) { sb.Append(','); }
                Num(sb, string.Empty, subs[i]);
            }
            sb.Append(']');

            sb.Append('}');
            return sb.ToString();
        }

        // ── /report — the #37 statistics ────────────────────────────────────────────────────────

        private string ReportJson()
        {
            var result = _host.BuildReport();
            var sb = new StringBuilder(1_024);

            sb.Append('{');
            Num(sb, "\"observedTicks\":", _host.ObservedTicks);

            // A frozen report is worse than an absent one, so the fault is reported alongside the
            // numbers rather than left for a reader to infer from figures that stopped moving.
            Exception fault = _host.AnalyticsFault;
            sb.Append(",\"healthy\":").Append(fault == null ? "true" : "false");
            if (fault != null)
            {
                sb.Append(",\"fault\":\"").Append(Escape(fault.Message)).Append('"');
            }

            sb.Append(",\"home\":");
            Team(sb, result.Home, result.HomeAdvanced);
            sb.Append(",\"away\":");
            Team(sb, result.Away, result.AwayAdvanced);
            sb.Append('}');

            return sb.ToString();
        }

        private static void Team(
            StringBuilder sb,
            in TacticalDirector.MatchAnalytics.MatchStatline line,
            in TacticalDirector.MatchAnalytics.AdvancedStatline advanced)
        {
            sb.Append('{');
            Num(sb, "\"goals\":", line.Goals);
            Num(sb, ",\"possession\":", line.PossessionSharePercent);
            Num(sb, ",\"territorial\":", advanced.TerritorialPercent);
            Num(sb, ",\"fouls\":", line.Fouls);
            Num(sb, ",\"yellow\":", line.YellowCards);
            Num(sb, ",\"red\":", line.RedCards);
            Num(sb, ",\"offsides\":", line.Offsides);
            Num(sb, ",\"corners\":", line.Corners);
            Num(sb, ",\"throwIns\":", line.ThrowIns);
            Num(sb, ",\"goalKicks\":", line.GoalKicks);
            Num(sb, ",\"subs\":", line.Substitutions);
            // xG is producer-gated (#37 F4): the flag travels so the page renders "—" rather than a
            // 0.00 that reads like a measurement of no threat.
            sb.Append(",\"xgAvailable\":").Append(advanced.LiveXgAvailable ? "true" : "false");
            Num(sb, ",\"xg\":", advanced.XgSum);
            sb.Append('}');
        }

        // ── /playback — pacing only, never the match ────────────────────────────────────────────

        private MatchClientResponse Playback(Dictionary<string, string> args)
        {
            if (!args.TryGetValue("action", out string action))
            {
                return MatchClientResponse.Text(400, "Bad Request: /playback needs action=pause|resume|speed");
            }

            LiveMatchStreamer streamer = _host.Session.Streamer;
            switch (action)
            {
                case "pause":
                    streamer.Pause();
                    return MatchClientResponse.Json("{\"ok\":true,\"action\":\"pause\"}");
                case "resume":
                    streamer.Resume();
                    return MatchClientResponse.Json("{\"ok\":true,\"action\":\"resume\"}");
                case "speed":
                    if (!args.TryGetValue("value", out string raw) ||
                        !float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out float speed))
                    {
                        return MatchClientResponse.Text(400, "Bad Request: speed needs a numeric value=");
                    }
                    try
                    {
                        streamer.SetSpeedMultiplier(speed);
                    }
                    catch (ArgumentOutOfRangeException ex)
                    {
                        return MatchClientResponse.Text(400, "Bad Request: " + ex.Message);
                    }
                    return MatchClientResponse.Json("{\"ok\":true,\"action\":\"speed\"}");
                default:
                    return MatchClientResponse.Text(400, "Bad Request: unknown playback action '" + action + "'");
            }
        }

        // ── /intent — the only route that can change the match ──────────────────────────────────

        private MatchClientResponse Intent(Dictionary<string, string> args)
        {
            if (!args.TryGetValue("kind", out string kindRaw))
            {
                return MatchClientResponse.Text(400, "Bad Request: /intent needs kind=");
            }
            if (!Enum.TryParse(kindRaw, ignoreCase: true, out IntentKind kind) || kind == IntentKind.None)
            {
                return MatchClientResponse.Text(400, "Bad Request: unknown intent kind '" + kindRaw + "'");
            }

            try
            {
                ManagerIntent intent;
                switch (kind)
                {
                    case IntentKind.SetTeamTactic:
                        intent = BuildTeamTactic(args);
                        break;
                    case IntentKind.Substitute:
                        intent = BuildSubstitution(args);
                        break;
                    default:
                        // SetPlayerTactic needs the whole per-agent instruction set, which has no
                        // screen yet; refusing is honest — a partial tactic assembled from defaults
                        // would apply eleven instructions the manager never chose.
                        return MatchClientResponse.Text(
                            501, "Not Implemented: " + kind + " has no browser surface yet");
                }

                _host.Dispatch(in intent);
                return MatchClientResponse.Json("{\"ok\":true,\"kind\":\"" + kind + "\"}");
            }
            catch (ArgumentException ex)
            {
                return MatchClientResponse.Text(400, "Bad Request: " + ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return MatchClientResponse.Text(409, "Conflict: " + ex.Message);
            }
        }

        private static ManagerIntent BuildTeamTactic(Dictionary<string, string> args)
        {
            int teamId = RequireTeam(args);

            // Only the dials the browser exposes are read; the rest keep their Balanced identity, so
            // an omitted dial is explicitly "unchanged from the neutral baseline" rather than an
            // accidental value.
            TeamTactic b = TeamTactic.Balanced;
            var tactic = new TeamTactic(
                mentality:          ParseEnum(args, "mentality", b.Mentality),
                formation:          b.Formation,
                tempo:              ParseEnum(args, "tempo", b.Tempo),
                width:              ParseEnum(args, "width", b.Width),
                passing:            ParseEnum(args, "passing", b.Passing),
                pressing:           ParseEnum(args, "pressing", b.Pressing),
                lineOfEngagement:   ParseEnum(args, "lineOfEngagement", b.LineOfEngagement),
                defensiveLine:      b.DefensiveLine,
                defensiveWidth:     b.DefensiveWidth,
                transitionWon:      b.TransitionWon,
                transitionLost:     b.TransitionLost,
                offsideTrap:        b.OffsideTrap,
                triggerPressMask:   b.TriggerPressMask,
                focusPlay:          b.FocusPlay,
                gkDistribution:     b.GkDistribution,
                timeWasting:        b.TimeWasting,
                markingOrientation: b.MarkingOrientation,
                dismarkIntensity:   b.DismarkIntensity,
                buildUpStructure:   b.BuildUpStructure,
                rotationFreedom:    b.RotationFreedom);

            return ManagerIntent.SetTeamTactic(teamId, in tactic);
        }

        private static ManagerIntent BuildSubstitution(Dictionary<string, string> args)
        {
            int teamId = RequireTeam(args);
            int outSlot = RequireInt(args, "outSlot");
            int bench   = RequireInt(args, "bench");
            SubstitutionReason reason = ParseEnum(args, "reason", SubstitutionReason.Tactical);

            return ManagerIntent.Substitute(teamId, outSlot, bench, reason);
        }

        // ── Parsing helpers — all fail loud ─────────────────────────────────────────────────────

        private static int RequireTeam(Dictionary<string, string> args)
        {
            int team = RequireInt(args, "team");
            if (team < 0 || team >= MatchEngineConstants.TEAM_COUNT)
            {
                throw new ArgumentException("team must be 0 (home) or 1 (away); got " + team + ".");
            }
            return team;
        }

        private static int RequireInt(Dictionary<string, string> args, string key)
        {
            if (!args.TryGetValue(key, out string raw) ||
                !int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
            {
                throw new ArgumentException("missing or non-numeric '" + key + "'.");
            }
            return value;
        }

        private static T ParseEnum<T>(Dictionary<string, string> args, string key, T fallback) where T : struct
        {
            if (!args.TryGetValue(key, out string raw) || raw.Length == 0)
            {
                return fallback;
            }
            if (!Enum.TryParse(raw, ignoreCase: true, out T parsed) || !Enum.IsDefined(typeof(T), parsed))
            {
                // Enum.TryParse happily accepts "7" for an enum with three members, so IsDefined is
                // load-bearing: an undefined dial reaching a [GT] lookup table indexes out of range.
                throw new ArgumentException(
                    "'" + raw + "' is not a valid " + typeof(T).Name + " for '" + key + "'.");
            }
            return parsed;
        }

        private static Dictionary<string, string> ParseQuery(string query)
        {
            var args = new Dictionary<string, string>(StringComparer.Ordinal);
            if (string.IsNullOrEmpty(query)) { return args; }

            foreach (string pair in query.Split('&'))
            {
                if (pair.Length == 0) { continue; }
                int eq = pair.IndexOf('=');
                string key = eq >= 0 ? pair.Substring(0, eq) : pair;
                string val = eq >= 0 ? pair.Substring(eq + 1) : string.Empty;
                args[key] = val;   // last wins; a duplicated key is a client bug, not a server decision
            }
            return args;
        }

        private static void Num(StringBuilder sb, string prefix, float v)
        {
            sb.Append(prefix);
            // A non-finite value would emit bare NaN, which is not JSON and silently breaks the whole
            // document rather than one field. The engine's own gates make it unreachable; this makes
            // it harmless if it ever is not.
            sb.Append(float.IsFinite(v) ? v.ToString("0.###", CultureInfo.InvariantCulture) : "null");
        }

        private static void Num(StringBuilder sb, string prefix, int v) =>
            sb.Append(prefix).Append(v.ToString(CultureInfo.InvariantCulture));

        private static void Num(StringBuilder sb, string prefix, long v) =>
            sb.Append(prefix).Append(v.ToString(CultureInfo.InvariantCulture));

        private static void Num(StringBuilder sb, string prefix, ulong v) =>
            sb.Append(prefix).Append(v.ToString(CultureInfo.InvariantCulture));

        private static string Escape(string s)
        {
            if (string.IsNullOrEmpty(s)) { return string.Empty; }
            var sb = new StringBuilder(s.Length + 8);
            foreach (char c in s)
            {
                switch (c)
                {
                    case '"':  sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n");  break;
                    case '\r': sb.Append("\\r");  break;
                    case '\t': sb.Append("\\t");  break;
                    default:
                        if (c < ' ') { sb.Append(' '); } else { sb.Append(c); }
                        break;
                }
            }
            return sb.ToString();
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-27 | —      | Initial creation (B6): the four routes and their privilege     |
// |         |            |        | split, #38 frame + #37 report serialization, and fail-loud     |
// |         |            |        | parsing (incl. the Enum.IsDefined guard).                      |
#endregion
