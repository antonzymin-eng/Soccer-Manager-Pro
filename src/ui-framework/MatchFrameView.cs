// File:     src/ui-framework/MatchFrameView.cs
// Created:  2026-07-25
// Modified: 2026-07-27 (P1 richer observation frame: agent cues, substitution counts, period, restart —
//           the last two as Scoreline / RestartBanner carriers after AR-1 M-6)
// Author:   —
// Spec:     UI / Client Framework #38 §2.2 (FR-UI-002/007/008, failure modes F1/F4),
//           docs/tracking/ui-framework-t0-implementation-plan.md KD-U4, Code Standards #20
// Purpose:  The immutable match view model: one rendered instant of a match (ball, agents, score,
//           possession, clock). Every value is copied and gated at construction, so what a screen binds
//           is a snapshot it cannot write through and cannot be surprised by.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using UnityEngine;

using TacticalDirector.MatchEngine;
using TacticalDirector.MatchViewer;

namespace TacticalDirector.UiFramework
{
    /// <summary>
    /// An immutable projection of one match frame (#38 §2.2).
    /// <para>
    /// WHY THIS COPIES (KD-U4): <c>LiveMatchFrame.AgentPositions</c> is a <c>Vector2[]</c> whose
    /// ownership is handed over by convention. Wrapping it would put a mutable array behind an
    /// "immutable" view model — the F4 defect, and the exact shape of the <c>MatchReplay</c> AR-3
    /// finding, where a <c>ReadOnlyCollection</c> over a live list turned out to be a view rather than a
    /// snapshot. So the array is copied at construction and exposed only through a read-only collection
    /// over that private copy.
    /// </para>
    /// <para>
    /// Everything entering the view is gated fail-loud (F1 / FR-UI-008): agent count against
    /// <c>SQUAD_SIZE</c>, the possessing id against <c>[-1, SQUAD_SIZE)</c> (−1 being the engine's loose
    /// ball sentinel), and every sampled coordinate NaN-gated. A malformed frame is a bug upstream, and
    /// the render loop is the worst place to discover it quietly.
    /// </para>
    /// </summary>
    public readonly struct MatchFrameView
    {
        // Stored as HasFrame, not IsEmpty, and that is load-bearing: `Empty` is `default(MatchFrameView)`,
        // where every field is zero — so an `IsEmpty` FIELD would default to false and the empty view
        // would cheerfully report itself as carrying a frame. The zero value has to mean "empty", so the
        // field means "has frame" and the emptiness question is answered by negating it.
        private readonly bool _hasFrame;

        /// <summary>True when this view carries no frame — nothing has been published yet (F5).</summary>
        public bool IsEmpty => !_hasFrame;

        /// <summary>The 60 Hz physics tick this frame was sampled at.</summary>
        public readonly ulong Tick;

        /// <summary>Ball position (x, y, z) in metres, corner-origin (Ball Physics #1 §1.2).</summary>
        public readonly Vector3 BallPosition;

        /// <summary>Possessing agent's roster index, or −1 when the ball is loose.</summary>
        public readonly int PossessingAgentId;

        /// <summary>Goals, home and away. Non-negativity is guaranteed by <see cref="Scoreline"/>
        /// itself, so this view no longer re-checks it (AR-1 M-6).</summary>
        public readonly Scoreline Score;

        /// <summary>True once full time has fired.</summary>
        public readonly bool MatchEnded;

        /// <summary>Which period the clock is in (P1 KD-P1-2).</summary>
        public readonly MatchPeriod Period;

        /// <summary>The most recent restart observed by the frame producer, or
        /// <see cref="RestartBanner.None"/>. The banner derives its awarded team and tick from its own
        /// cue, so an empty view reports −1 / 0 rather than "home team, tick 0" (AR-1 M-3).</summary>
        public readonly RestartBanner Restart;

        private readonly ReadOnlyCollection<Vector2> _agentPositions;
        private readonly ReadOnlyCollection<LiveAgentCue> _agentCues;
        private readonly ReadOnlyCollection<int> _substitutionsUsed;

        /// <summary>
        /// Agent positions (x, y) in metres, indexed by roster index. Read-only over this view's own
        /// copy — never the producer's array. Empty on an empty view.
        /// </summary>
        public IReadOnlyList<Vector2> AgentPositions =>
            _agentPositions ?? EmptyPositions;

        /// <summary>
        /// Per-agent booking / sent-off / substitute cues, in lockstep with <see cref="AgentPositions"/>.
        /// Copied and read-only for the same reason positions are (KD-U4). Empty on an empty view.
        /// </summary>
        public IReadOnlyList<LiveAgentCue> AgentCues =>
            _agentCues ?? EmptyCues;

        /// <summary>Substitutions used, indexed by team id. Empty on an empty view.</summary>
        public IReadOnlyList<int> SubstitutionsUsed =>
            _substitutionsUsed ?? EmptySubs;

        private static readonly ReadOnlyCollection<Vector2> EmptyPositions =
            new ReadOnlyCollection<Vector2>(new Vector2[0]);

        private static readonly ReadOnlyCollection<LiveAgentCue> EmptyCues =
            new ReadOnlyCollection<LiveAgentCue>(new LiveAgentCue[0]);

        private static readonly ReadOnlyCollection<int> EmptySubs =
            new ReadOnlyCollection<int>(new int[0]);

        /// <summary>An explicitly empty view — what the match screen renders before the first frame (F5).</summary>
        public static MatchFrameView Empty => default;

        /// <summary>
        /// Projects <paramref name="frame"/> into an immutable view, copying and gating every value.
        /// </summary>
        /// <exception cref="ArgumentException">The frame is malformed: a null or wrongly-sized agent
        /// or cue array, an out-of-range possessing id, or a non-finite coordinate (F1). A negative
        /// score is impossible by then — <see cref="Scoreline"/> refuses one at its own construction.</exception>
        public MatchFrameView(in LiveMatchFrame frame)
        {
            Vector2[] source = frame.AgentPositions;
            if (source == null)
            {
                throw new ArgumentException("LiveMatchFrame.AgentPositions is null.", nameof(frame));
            }
            if (source.Length != MatchEngineConstants.SQUAD_SIZE)
            {
                throw new ArgumentException(
                    "LiveMatchFrame carries " + Inv(source.Length) + " agent positions; expected " +
                    Inv(MatchEngineConstants.SQUAD_SIZE) + " (SQUAD_SIZE).", nameof(frame));
            }
            if (frame.PossessingAgentId < -1 || frame.PossessingAgentId >= MatchEngineConstants.SQUAD_SIZE)
            {
                throw new ArgumentException(
                    "LiveMatchFrame.PossessingAgentId " + Inv(frame.PossessingAgentId) +
                    " is outside [-1, SQUAD_SIZE).", nameof(frame));
            }
            RequireFinite(frame.BallPosition.x, "BallPosition.x");
            RequireFinite(frame.BallPosition.y, "BallPosition.y");
            RequireFinite(frame.BallPosition.z, "BallPosition.z");

            // P1: the cue array is gated exactly as strictly as the positions it annotates. A frame
            // whose cues and positions are different lengths cannot be drawn coherently, and finding
            // that out in the render loop is the F1 failure this constructor exists to prevent.
            LiveAgentCue[] cueSource = frame.AgentCues;
            if (cueSource == null)
            {
                throw new ArgumentException("LiveMatchFrame.AgentCues is null.", nameof(frame));
            }
            if (cueSource.Length != source.Length)
            {
                throw new ArgumentException(
                    "LiveMatchFrame carries " + Inv(cueSource.Length) + " agent cues but " +
                    Inv(source.Length) + " agent positions.", nameof(frame));
            }

            int[] subsSource = frame.SubstitutionsUsed;
            if (subsSource == null)
            {
                throw new ArgumentException("LiveMatchFrame.SubstitutionsUsed is null.", nameof(frame));
            }
            if (subsSource.Length != MatchEngineConstants.TEAM_COUNT)
            {
                throw new ArgumentException(
                    "LiveMatchFrame carries " + Inv(subsSource.Length) + " substitution counts; expected " +
                    Inv(MatchEngineConstants.TEAM_COUNT) + " (TEAM_COUNT).", nameof(frame));
            }

            var copy = new Vector2[source.Length];
            for (int i = 0; i < source.Length; i++)
            {
                RequireFinite(source[i].x, "AgentPositions[" + Inv(i) + "].x");
                RequireFinite(source[i].y, "AgentPositions[" + Inv(i) + "].y");
                copy[i] = source[i];
            }

            var cueCopy = new LiveAgentCue[cueSource.Length];
            for (int i = 0; i < cueSource.Length; i++)
            {
                cueCopy[i] = cueSource[i];
            }

            var subsCopy = new int[subsSource.Length];
            for (int i = 0; i < subsSource.Length; i++)
            {
                subsCopy[i] = subsSource[i];
            }

            _hasFrame          = true;
            Tick               = frame.Tick;
            BallPosition       = frame.BallPosition;
            PossessingAgentId  = frame.PossessingAgentId;
            Score              = frame.Score;
            MatchEnded         = frame.MatchEnded;
            Period             = frame.Period;
            Restart            = frame.Restart;
            _agentPositions    = new ReadOnlyCollection<Vector2>(copy);
            _agentCues         = new ReadOnlyCollection<LiveAgentCue>(cueCopy);
            _substitutionsUsed = new ReadOnlyCollection<int>(subsCopy);
        }

        // Non-finite covers NaN AND ±Infinity: a NaN-only gate passes an infinity straight through to a
        // renderer, which is the same class of hole the match-viewer NaN-gates were hardened against.
        private static void RequireFinite(float value, string what)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                throw new ArgumentException("LiveMatchFrame." + what + " is not finite.", "frame");
            }
        }

        /// <summary>Culture-invariant int formatting for the diagnostic messages above.</summary>
        private static string Inv(int value) =>
            value.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-25 | —      | Initial creation (#38 T0): immutable match VM — array copied   |
// |         |            |        | (never wrapped), SQUAD_SIZE / possession-id / score / finite   |
// |         |            |        | gates, and an explicit Empty for the pre-first-frame case.     |
// | 1.1     | 2026-07-27 | —      | P1 richer observation frame: + AgentCues / SubstitutionsUsed   |
// |         |            |        | (both copied, never wrapped — same KD-U4 reasoning), Period    |
// |         |            |        | and the latched last restart. Both new arrays are gated like   |
// |         |            |        | positions: non-null, and lengths coherent with the positions   |
// |         |            |        | array / TEAM_COUNT respectively.                               |
// | 1.2     | 2026-07-27 | —      | P1 AR-1 M-6 / M-3: the score and restart fields collapse into  |
// |         |            |        | Scoreline and RestartBanner. The negative-score gate moves     |
// |         |            |        | INTO Scoreline, so a negative score can no longer reach a      |
// |         |            |        | frame at all — refused a layer earlier rather than re-checked  |
// |         |            |        | here. M-3 closes structurally: Empty is default(MatchFrameView) |
// |         |            |        | hence default(RestartBanner), which derives its team and tick   |
// |         |            |        | from its own cue and so reports NO_RESTART_TEAM / 0 rather     |
// |         |            |        | than the "home team, tick 0" the v1.1 loose fields gave.        |
#endregion
