// File:     src/client-app/S0ScreenContent.cs
// Created:  2026-10-08
// Modified: 2026-10-08
// Author:   —
// Spec:     S0 binding contracts §4, Localization #49, Code Standards #20
// Purpose:  Compiled S0 role catalogue and typed argument schemas, consumed by the shipping shell.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using TacticalDirector.Localization;

namespace TacticalDirector.ClientApp
{
    /// <summary>Immutable authored content; every product string passes through the production localizer.</summary>
    public static class S0ScreenContent
    {
        private static readonly ReadOnlyCollection<S0TextRole> Roles = Array.AsReadOnly(new[]
        {
            new S0TextRole("action.back", "Back", ""),
            new S0TextRole("action.cancel", "Cancel", ""),
            new S0TextRole("action.compare", "Compare all seven Mentalities", ""),
            new S0TextRole("action.demo", "Play a demo match", ""),
            new S0TextRole("action.faster", "Faster", ""),
            new S0TextRole("action.mentality", "Change Mentality", ""),
            new S0TextRole("action.partial", "Show partial statistics — incomplete", ""),
            new S0TextRole("action.pause", "Pause", ""),
            new S0TextRole("action.report", "View match report", ""),
            new S0TextRole("action.resume", "Resume", ""),
            new S0TextRole("action.return", "Return to main menu", ""),
            new S0TextRole("action.slower", "Slower", ""),
            new S0TextRole("action.start", "Start match", ""),
            new S0TextRole("action.statistics_close", "Close statistics", ""),
            new S0TextRole("action.statistics_open", "Open statistics", ""),
            new S0TextRole("action.submit", "Submit change", ""),
            new S0TextRole("action.substitution", "Make substitution", ""),
            new S0TextRole("context.mentality_choice", "Home Mentality — choose one", ""),
            new S0TextRole("context.menu", "Manage the home side in one demo match. The opponent is AI-managed.", ""),
            new S0TextRole("context.setup", "You manage Home. Away is AI-managed.", ""),
            new S0TextRole("context.speed_steps", "Speed steps: 1×, 3×, 5×, 10×.", ""),
            new S0TextRole("context.submit", "Choose a change, then submit it. Your team changes only when you see Applied.", ""),
            new S0TextRole("context.substitution", "Choose a player by name and shirt number, then an unused bench player. The substitution takes effect when the request is applied", ""),
            new S0TextRole("dialog.mentality", "Change Home Mentality", ""),
            new S0TextRole("dialog.substitution", "Make Home substitution", ""),
            new S0TextRole("effect.attacking", "More risk in on-ball choices; higher defensive line.", ""),
            new S0TextRole("effect.balanced", "Standard risk in on-ball choices and standard defensive line.", ""),
            new S0TextRole("effect.cautious", "Slightly less risk in on-ball choices; slightly deeper defensive line.", ""),
            new S0TextRole("effect.defensive", "Less risk in on-ball choices; deeper defensive line.", ""),
            new S0TextRole("effect.positive", "Slightly more risk in on-ball choices; slightly higher defensive line.", ""),
            new S0TextRole("effect.very_attacking", "Most risk in on-ball choices; highest defensive line.", ""),
            new S0TextRole("effect.very_defensive", "Least risk in on-ball choices; deepest defensive line.", ""),
            new S0TextRole("field.incoming", "Incoming bench player", ""),
            new S0TextRole("field.outgoing", "Outgoing home player", ""),
            new S0TextRole("field.requested_mentality", "Requested Mentality", ""),
            new S0TextRole("heading.changes", "Home team changes", ""),
            new S0TextRole("heading.feedback", "Change request feedback", ""),
            new S0TextRole("heading.match", "Match View", ""),
            new S0TextRole("heading.menu", "From the touchline", ""),
            new S0TextRole("heading.report", "Post-Match Report", ""),
            new S0TextRole("heading.setup", "Tactics Setup", ""),
            new S0TextRole("heading.statistics", "Match statistics", ""),
            new S0TextRole("identity.goalkeeper", "goalkeeper", ""),
            new S0TextRole("mentality.attacking", "Attacking", ""),
            new S0TextRole("mentality.balanced", "Balanced", ""),
            new S0TextRole("mentality.cautious", "Cautious", ""),
            new S0TextRole("mentality.defensive", "Defensive", ""),
            new S0TextRole("mentality.positive", "Positive", ""),
            new S0TextRole("mentality.very_attacking", "Very Attacking", ""),
            new S0TextRole("mentality.very_defensive", "Very Defensive", ""),
            new S0TextRole("pitch.description", "Match pitch. Home H attacks right", ""),
            new S0TextRole("pitch.direction", "Home attacks right → • Away attacks left ←", ""),
            new S0TextRole("pitch.legend", "H/A identify Home/Away player shirts. A white-outlined substitute marker identifies an applied substitution.", ""),
            new S0TextRole("pitch.waiting", "Pitch appears after the first frame.", ""),
            new S0TextRole("reason.choice_changed", "This choice is no longer available. Choose an available player.", ""),
            new S0TextRole("reason.ended", "Match ended — playback and team changes are unavailable.", ""),
            new S0TextRole("reason.fastest", "Already at the fastest speed — faster is unavailable.", ""),
            new S0TextRole("reason.mentality_pending", "Mentality request pending — another request is unavailable until resolved.", ""),
            new S0TextRole("reason.no_pair", "No legal outgoing player and unused bench player pair is available.", ""),
            new S0TextRole("reason.resume", "Resume to apply pending requests.", ""),
            new S0TextRole("reason.slowest", "Already at real time — slower is unavailable.", ""),
            new S0TextRole("reason.substitution_cap", "All substitutions used.", ""),
            new S0TextRole("reason.substitution_pending", "Substitution request pending — another request is unavailable until resolved.", ""),
            new S0TextRole("reason.waiting", "Match starting — controls unavailable until the first frame.", ""),
            new S0TextRole("result.draw", "Draw", ""),
            new S0TextRole("result.loss", "Loss", ""),
            new S0TextRole("result.win", "Win", ""),
            new S0TextRole("state.clock_waiting", "Clock awaiting first frame", ""),
            new S0TextRole("state.first_half", "First half", ""),
            new S0TextRole("state.full_time", "Full time", ""),
            new S0TextRole("state.paused", "Paused", ""),
            new S0TextRole("state.score_unavailable", "—", ""),
            new S0TextRole("state.second_half", "Second half", ""),
            new S0TextRole("state.waiting", "Waiting for first frame", ""),
            new S0TextRole("statistics.caption", "Match statistics", ""),
            new S0TextRole("statistics.closed", "Statistics panel closed at full time.", ""),
            new S0TextRole("statistics.column", "Statistic", ""),
            new S0TextRole("statistics.corners", "Corners", ""),
            new S0TextRole("statistics.fouls", "Fouls", ""),
            new S0TextRole("statistics.goal_kicks", "Goal kicks", ""),
            new S0TextRole("statistics.goals", "Goals recorded", ""),
            new S0TextRole("statistics.loose_ball", "Possession shares include loose-ball time", ""),
            new S0TextRole("statistics.offsides", "Offsides", ""),
            new S0TextRole("statistics.possession", "Possession %", ""),
            new S0TextRole("statistics.red", "Red cards", ""),
            new S0TextRole("statistics.report_available", "Final statistics are available in the match report.", ""),
            new S0TextRole("statistics.retained", "Statistics panel retained at full time.", ""),
            new S0TextRole("statistics.substitutions", "Substitutions", ""),
            new S0TextRole("statistics.territory", "Territorial %", ""),
            new S0TextRole("statistics.throw_ins", "Throw-ins", ""),
            new S0TextRole("statistics.xg", "Expected goals (xG)", ""),
            new S0TextRole("statistics.yellow", "Yellow cards", ""),
            new S0TextRole("status.applied", "Applied", ""),
            new S0TextRole("status.not_applied", "Not applied", ""),
            new S0TextRole("status.pending", "Pending", ""),
            new S0TextRole("status.refused", "Refused", ""),
            new S0TextRole("status.send_failure", "Send failure", ""),
            new S0TextRole("tag.current", "Current", ""),
            new S0TextRole("tag.requested", "Requested", ""),
            new S0TextRole("team.away", "Away", ""),
            new S0TextRole("team.away_short", "A", ""),
            new S0TextRole("team.home", "Home", ""),
            new S0TextRole("team.home_short", "H", ""),
            new S0TextRole("feedback.earlier", "Earlier change feedback ({0})", "I"),
            new S0TextRole("feedback.mentality_applied", "Applied — Mentality: {0} at minute {1}.", "SI"),
            new S0TextRole("feedback.mentality_not_applied", "Not applied — Mentality: {0} — match ended before it could apply.", "S"),
            new S0TextRole("feedback.mentality_pending", "Pending — Mentality: {0} — waiting to be applied.", "S"),
            new S0TextRole("feedback.mentality_pending_paused", "Pending — Mentality: {0} — waiting; resume to continue.", "S"),
            new S0TextRole("feedback.mentality_refused", "Refused — Mentality: {0} — current Mentality unchanged.", "S"),
            new S0TextRole("feedback.mentality_send_failure", "Send failure — Mentality: {0} — request could not be sent; current Mentality unchanged.", "S"),
            new S0TextRole("feedback.substitution_applied", "Applied — {0} {1} (shirt {2}) → {3} {4} (shirt {5}, bench slot {6}) at minute {7}.", "SSISSIII"),
            new S0TextRole("feedback.substitution_not_applied", "Not applied — {0} {1} (shirt {2}) → {3} {4} (shirt {5}, bench slot {6}); match ended before it could apply.", "SSISSII"),
            new S0TextRole("feedback.substitution_pending", "Pending — {0} {1} (shirt {2}) → {3} {4} (shirt {5}, bench slot {6}); waiting to be applied.", "SSISSII"),
            new S0TextRole("feedback.substitution_pending_paused", "Pending — {0} {1} (shirt {2}) → {3} {4} (shirt {5}, bench slot {6}); waiting; resume to continue.", "SSISSII"),
            new S0TextRole("feedback.substitution_refused", "Refused — {0} {1} (shirt {2}) → {3} {4} (shirt {5}, bench slot {6}); substitution count unchanged.", "SSISSII"),
            new S0TextRole("feedback.substitution_send_failure", "Send failure — {0} {1} (shirt {2}) → {3} {4} (shirt {5}, bench slot {6}); request could not be sent; substitution count unchanged.", "SSISSII"),
            new S0TextRole("identity.bench", "{0}: {1} {2} — shirt {3} — bench slot {4}", "SSSII"),
            new S0TextRole("identity.bench_keeper", "{0}: {1} {2} — shirt {3} — bench slot {4} — {5}", "SSSIIS"),
            new S0TextRole("identity.player", "{0}: {1} {2} — shirt {3}", "SSSI"),
            new S0TextRole("identity.player_keeper", "{0}: {1} {2} — shirt {3} — {4}", "SSSIS"),
            new S0TextRole("mentality.choice", "{0} — {1}", "SS"),
            new S0TextRole("mentality.current", "Current Mentality: {0}", "S"),
            new S0TextRole("minute", "Minute {0}", "I"),
            new S0TextRole("pitch.keeper_description", "{0}: {1} {2} — shirt {3}. Goalkeeper.", "SSSI"),
            new S0TextRole("pitch.keeper_substitute_description", "{0}: {1} {2} — shirt {3}. Goalkeeper. Applied substitute.", "SSSI"),
            new S0TextRole("pitch.marker", "{0}{1}", "SI"),
            new S0TextRole("pitch.marker_substitute", "{0}{1} ↔", "SI"),
            new S0TextRole("pitch.player_description", "{0}: {1} {2} — shirt {3}.", "SSSI"),
            new S0TextRole("pitch.substitute_description", "{0}: {1} {2} — shirt {3}. Applied substitute.", "SSSI"),
            new S0TextRole("result.home", "Full time — Home result: {0}", "S"),
            new S0TextRole("scoreline", "{0} – {1}", "II"),
            new S0TextRole("setup.ready", "Ready to start with {0}.", "S"),
            new S0TextRole("speed", "Selected speed {0}×", "I"),
            new S0TextRole("speed_paused", "Selected speed {0}× • Paused", "I"),
            new S0TextRole("statistics.incomplete_final", "Statistics incomplete — stopped at minute {0}. Final score remains available.", "I"),
            new S0TextRole("statistics.incomplete_live", "Statistics stopped at minute {0}. The match continues; these figures are incomplete.", "I"),
            new S0TextRole("statistics.partial_caption", "Partial statistics — stopped at minute {0} — incomplete, not full-match totals.", "I"),
            new S0TextRole("substitutions.used", "Substitutions used: {0} / {1}", "II"),
            new S0TextRole("value.decimal", "{0:F1}", "F"),
            new S0TextRole("value.integer", "{0:D}", "I"),
            new S0TextRole("value.percentage", "{0:F1}%", "F"),
        });
        /// <summary>The complete static/dynamic role register, in ordinal family order.</summary>
        public static IReadOnlyList<S0TextRole> All => Roles;

        /// <summary>Copies authored rows into the dependency-free generic L2 catalogue.</summary>
        public static TemplateCatalogue CreateBaseCatalogue()
        {
            var rows = new TemplateCatalogue.StaticRow[Roles.Count];
            for (int i = 0; i < rows.Length; i++)
                rows[i] = new TemplateCatalogue.StaticRow(Roles[i].Key, Roles[i].BasePattern);
            return new TemplateCatalogue(LocaleId.BaseLocale, rows);
        }

        /// <summary>Explicit coverage for every S0 role, including dynamic static-key patterns.</summary>
        public static CatalogueCoverage Coverage()
        {
            var keys = new LocalizationKey[Roles.Count];
            for (int i = 0; i < keys.Length; i++)
                keys[i] = Roles[i].Key;
            return new CatalogueCoverage(keys);
        }
    }
}
#region VersionHistory
// | Version | Date       | Author | Notes |
// | 1.0     | 2026-10-08 | —      | Complete reviewed S0 role register and coverage. |
// | 1.1     | 2026-10-08 | —      | Localized pitch legend explains single Tab entry and read-only arrow inspection. |
// | 1.2     | 2026-10-08 | —      | Restore owner-approved pitch legend verbatim; keyboard inspection remains in the focus binding. |
#endregion
