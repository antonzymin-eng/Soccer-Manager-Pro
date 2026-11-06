// File:     src/client-app/CareerScreenText.cs
// Created:  2026-11-06
// Modified: 2026-11-06
// Author:   —
// Spec:     Interactive Unity client §5-P7, UI / Client Framework #38, Code Standards #20
// Purpose:  Localized career copy and presentation over the existing immutable career projections.

using System;
using System.Collections.Generic;
using System.Globalization;

namespace TacticalDirector.ClientApp
{
    /// <summary>Independent career catalogue using the shared S0 localizer, typed formatting and pseudo-locale admission.</summary>
    public sealed class CareerScreenText
    {
        /// <summary>All career roles; independent of the approved S0 match copy register.</summary>
        public static IReadOnlyList<S0TextRole> All { get; } = Array.AsReadOnly(new[]
        {
            new S0TextRole("heading.home", "Career Home", "", "ui.career."),
            new S0TextRole("heading.new", "New Career", "", "ui.career."),
            new S0TextRole("heading.load", "Load Career", "", "ui.career."),
            new S0TextRole("heading.changed", "What changed", "", "ui.career."),
            new S0TextRole("heading.attention", "Needs attention", "", "ui.career."),
            new S0TextRole("heading.next", "Next fixture", "", "ui.career."),
            new S0TextRole("heading.table", "League table", "", "ui.career."),
            new S0TextRole("heading.save", "Save career", "", "ui.career."),
            new S0TextRole("action.new", "New Career", "", "ui.career."),
            new S0TextRole("action.load", "Load Career", "", "ui.career."),
            new S0TextRole("action.continue", "Continue", "", "ui.career."),
            new S0TextRole("action.start", "Start career", "", "ui.career."),
            new S0TextRole("action.back", "Back", "", "ui.career."),
            new S0TextRole("action.quit", "Quit to main menu", "", "ui.career."),
            new S0TextRole("action.retry", "Try again", "", "ui.career."),
            new S0TextRole("action.refresh", "Refresh", "", "ui.career."),
            new S0TextRole("action.overwrite", "Overwrite this slot", "", "ui.career."),
            new S0TextRole("action.cancel", "Cancel", "", "ui.career."),
            new S0TextRole("action.save", "Save to {0}", "S", "ui.career."),
            new S0TextRole("action.load_slot", "Load {0}", "S", "ui.career."),
            new S0TextRole("action.advance", "Advance one day", "", "ui.career."),
            new S0TextRole("action.matchday", "Play matchday", "", "ui.career."),
            new S0TextRole("action.roll", "Start next season", "", "ui.career."),
            new S0TextRole("action.none", "Progress unavailable", "", "ui.career."),
            new S0TextRole("reason.busy", "Working. Please wait before saving, loading or leaving this screen.", "", "ui.career."),
            new S0TextRole("reason.matchday", "Playing the matchday may take a few minutes.", "", "ui.career."),
            new S0TextRole("reason.no_save", "No loadable career save. Start a new career or choose a ready slot.", "", "ui.career."),
            new S0TextRole("reason.selection", "Choose a club to start your career.", "", "ui.career."),
            new S0TextRole("reason.setup", "Choose your club. Your world is generated automatically.", "", "ui.career."),
            new S0TextRole("reason.autosave", "Autosave: written when a career starts, after each matchday and at the season boundary.", "", "ui.career."),
            new S0TextRole("reason.breakable", "Saves may become incompatible with later development builds.", "", "ui.career."),
            new S0TextRole("reason.quit", "Save to a manual slot before leaving to keep changes since the last autosave.", "", "ui.career."),
            new S0TextRole("reason.unready", "Career storage is unavailable. Try again.", "", "ui.career."),
            new S0TextRole("reason.confirm", "Replace {0}? Its existing career save will be overwritten.", "S", "ui.career."),
            new S0TextRole("reason.error", "Could not complete this action. Try again.", "", "ui.career."),
            new S0TextRole("reason.autosave_error", "Autosave failed. Your career has progressed. Try a manual save.", "", "ui.career."),
            new S0TextRole("reason.refresh_error", "Your action completed, but the screen could not refresh. Refresh to try again.", "", "ui.career."),
            new S0TextRole("reason.discard", "Quit to the main menu and discard progress since your last save?", "", "ui.career."),
            new S0TextRole("action.discard", "Discard progress and quit", "", "ui.career."),
            new S0TextRole("reason.saved", "Saved to {0}.", "S", "ui.career."),
            new S0TextRole("slot.autosave", "Autosave", "", "ui.career."),
            new S0TextRole("slot.manual", "Manual slot {0}", "I", "ui.career."),
            new S0TextRole("slot.empty", "Empty: no save in this slot.", "", "ui.career."),
            new S0TextRole("slot.ready", "Ready", "", "ui.career."),
            new S0TextRole("slot.damaged", "Damaged: this save cannot be loaded.", "", "ui.career."),
            new S0TextRole("slot.incompatible", "Incompatible with this build. Save migration is unavailable.", "", "ui.career."),
            new S0TextRole("slot.summary", "{0}: {1}. {2}, season {3}, day {4}, rounds {5}/{6}, position {7}.", "SSSISIII", "ui.career."),
            new S0TextRole("home.summary", "{0} | Season {1} | Day {2} | Rounds {3}/{4} | Position {5}", "SISIII", "ui.career."),
            new S0TextRole("home.attention", "Injured players: {0}. Board target: position {1} or better. {2} Job security: {3}/1000.", "IISI", "ui.career."),
            new S0TextRole("objective.on", "On track.", "", "ui.career."),
            new S0TextRole("objective.off", "Below target.", "", "ui.career."),
            new S0TextRole("next.fixture", "{0} vs {1}, {2}. Matchday {3}, day {4}; {5} day(s) away.", "SSSISI", "ui.career."),
            new S0TextRole("next.home", "home", "", "ui.career."),
            new S0TextRole("next.away", "away", "", "ui.career."),
            new S0TextRole("next.complete", "Season complete. Follow the available progress action.", "", "ui.career."),
            new S0TextRole("next.bye", "No managed fixture in the next round. Follow the available progress action.", "", "ui.career."),
            new S0TextRole("change.started", "Your career has started.", "", "ui.career."),
            new S0TextRole("change.resumed", "Career resumed from a save.", "", "ui.career."),
            new S0TextRole("change.day", "Advanced to day {0}.", "S", "ui.career."),
            new S0TextRole("change.round", "Matchday complete: {0} {1} - {2} {3}.", "SIIS", "ui.career."),
            new S0TextRole("change.round_bye", "Matchday complete. No managed fixture was played.", "", "ui.career."),
            new S0TextRole("change.roll", "Season complete: position {0}, target {1}. {2}", "IIS", "ui.career."),
            new S0TextRole("change.none", "No recent change.", "", "ui.career."),
            new S0TextRole("table.row", "{0}. {1} | Played {2} | W {3} D {4} L {5} | Goals {6}:{7} | GD {8} | Points {9}", "ISIIIIIISI", "ui.career."),
            new S0TextRole("new.selected", "Selected club: {0}. Board target: position {1} or better.", "SI", "ui.career."),
            new S0TextRole("new.club", "{0} | Select club", "S", "ui.career."),
        });
        private readonly S0TextFormatter _text;
        /// <summary>Uses one fixed admitted formatting context.</summary>
        public CareerScreenText(S0TextFormatter text) { _text = text ?? throw new ArgumentNullException(nameof(text)); }
        /// <summary>Resolves an authored career label.</summary>
        public string Label(string role) => _text.Label(role);
        /// <summary>Formats a typed career role with a stable cache cell.</summary>
        public string Format(string cell, string role, params object[] values) => _text.Format(cell, role, values);
        /// <summary>Displays the named autosave or numbered manual slot.</summary>
        public string SlotName(CareerSaveSlot slot) => slot == CareerSaveSlot.Autosave ? Label("slot.autosave") : Format("slot.name." + slot, "slot.manual", (int)slot);
        /// <summary>Explains every slot state explicitly, including why loading is unavailable.</summary>
        public string Slot(CareerSlotSummary slot)
        {
            string state;
            switch (slot.State)
            {
                case CareerSlotState.Empty: state = Label("slot.empty"); break;
                case CareerSlotState.Ready: state = Label("slot.ready"); break;
                case CareerSlotState.Damaged: state = Label("slot.damaged"); break;
                case CareerSlotState.Incompatible: state = Label("slot.incompatible"); break;
                default: throw new ArgumentOutOfRangeException(nameof(slot));
            }
            if (slot.State == CareerSlotState.Empty || slot.State == CareerSlotState.Damaged)
                return SlotName(slot.Slot) + ": " + state;
            return Format("slot." + slot.Slot, "slot.summary", SlotName(slot.Slot), state, slot.ManagedClubName,
                slot.SeasonNumber + 1, Number(slot.WorldDay), slot.RoundsPlayed, slot.RoundCount, slot.LeaguePosition);
        }
        /// <summary>Names the single legal progress action without exposing enum identifiers.</summary>
        public string Progress(CareerProgressAction action)
        {
            switch (action)
            {
                case CareerProgressAction.AdvanceDay: return Label("action.advance");
                case CareerProgressAction.PlayMatchday: return Label("action.matchday");
                case CareerProgressAction.StartNextSeason: return Label("action.roll");
                default: return Label("action.none");
            }
        }
        /// <summary>Formats Career Home's context from existing data only.</summary>
        public string Summary(CareerHomeViewModel home) => Format("home.summary", "home.summary", home.ManagedClubName,
            home.SeasonNumber + 1, Number(home.WorldDay), home.RoundsPlayed, home.RoundCount, home.LeaguePosition);
        /// <summary>Formats current injury and board attention.</summary>
        public string Attention(CareerHomeViewModel home) => Format("home.attention", "home.attention", home.InjuredPlayerCount,
            home.ObjectiveTargetPosition, Label(home.IsObjectiveOnTrack ? "objective.on" : "objective.off"), home.JobSecurityPerMille);
        /// <summary>Formats the next fixture, bye or completed-season state.</summary>
        public string Next(CareerHomeViewModel home) => !home.HasNextFixture
            ? Label(home.IsSeasonComplete ? "next.complete" : "next.bye")
            : Format("next", "next.fixture", home.ManagedClubName, home.NextOpponentName,
                Label(home.NextFixtureIsHome ? "next.home" : "next.away"), home.NextFixtureRoundIndex + 1,
                Number(home.NextFixtureDay), Math.Max(0, home.DaysUntilNextMatchday));
        /// <summary>Formats the last committed career operation.</summary>
        public string Changed(CareerHomeViewModel home)
        {
            CareerChange change = home.LastChange;
            switch (change.Kind)
            {
                case CareerChangeKind.Started: return Label("change.started");
                case CareerChangeKind.Resumed: return Label("change.resumed");
                case CareerChangeKind.DayAdvanced: return Format("change", "change.day", Number(change.WorldDay));
                case CareerChangeKind.RoundPlayed:
                    if (!change.HasManagedResult) return Label("change.round_bye");
                    bool isHome = change.ManagedResult.HomeClubId == home.ManagedClubId;
                    return Format("change", "change.round", isHome ? home.ManagedClubName : home.LastResultOpponentName,
                        change.ManagedResult.HomeGoals, change.ManagedResult.AwayGoals,
                        isHome ? home.LastResultOpponentName : home.ManagedClubName);
                case CareerChangeKind.SeasonRolled: return Format("change", "change.roll", change.Roll.FinalPosition,
                    change.Roll.TargetPosition, Label(change.Roll.ObjectiveMet ? "objective.on" : "objective.off"));
                default: return Label("change.none");
            }
        }
        /// <summary>Formats a full named league-table row; signed goal difference remains text.</summary>
        public string TableRow(CareerHomeViewModel home, int index)
        {
            var row = home.Table[index];
            return Format("table." + index, "table.row", index + 1, home.TableClubNames[index], row.Played,
                row.Won, row.Drawn, row.Lost, row.GoalsFor, row.GoalsAgainst,
                row.GoalDifference.ToString(CultureInfo.InvariantCulture), row.Points);
        }
        private static string Number(uint value) => value.ToString(CultureInfo.InvariantCulture);
    }
}
#region VersionHistory
// | Version | Date       | Author | Notes |
// | 1.0     | 2026-11-06 | —      | P7b localized screen content, state reasons and complete data rows. |
// | 1.1     | 2026-11-06 | —      | Independent career keys, localized generic failure copy and discard confirmation. |
#endregion
