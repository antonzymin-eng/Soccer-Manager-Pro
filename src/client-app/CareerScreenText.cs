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
            new S0TextRole("career.heading.home", "Career Home", ""),
            new S0TextRole("career.heading.new", "New Career", ""),
            new S0TextRole("career.heading.load", "Load Career", ""),
            new S0TextRole("career.heading.changed", "What changed", ""),
            new S0TextRole("career.heading.attention", "Needs attention", ""),
            new S0TextRole("career.heading.next", "Next fixture", ""),
            new S0TextRole("career.heading.table", "League table", ""),
            new S0TextRole("career.heading.save", "Save career", ""),
            new S0TextRole("career.action.new", "New Career", ""),
            new S0TextRole("career.action.load", "Load Career", ""),
            new S0TextRole("career.action.continue", "Continue", ""),
            new S0TextRole("career.action.start", "Start career", ""),
            new S0TextRole("career.action.back", "Back", ""),
            new S0TextRole("career.action.quit", "Quit to main menu", ""),
            new S0TextRole("career.action.retry", "Try again", ""),
            new S0TextRole("career.action.refresh", "Refresh save slots", ""),
            new S0TextRole("career.action.overwrite", "Overwrite this slot", ""),
            new S0TextRole("career.action.cancel", "Cancel", ""),
            new S0TextRole("career.action.save", "Save to {0}", "S"),
            new S0TextRole("career.action.load_slot", "Load {0}", "S"),
            new S0TextRole("career.action.advance", "Advance one day", ""),
            new S0TextRole("career.action.matchday", "Play matchday", ""),
            new S0TextRole("career.action.roll", "Start next season", ""),
            new S0TextRole("career.action.none", "Progress unavailable", ""),
            new S0TextRole("career.reason.busy", "Working. Please wait before saving, loading or leaving this screen.", ""),
            new S0TextRole("career.reason.matchday", "Playing the matchday may take a few minutes.", ""),
            new S0TextRole("career.reason.no_save", "No loadable career save. Start a new career or choose a ready slot.", ""),
            new S0TextRole("career.reason.selection", "Choose a club to start your career.", ""),
            new S0TextRole("career.reason.setup", "Choose your club. Your world is generated automatically.", ""),
            new S0TextRole("career.reason.autosave", "Autosave: written when a career starts, after each matchday and at the season boundary.", ""),
            new S0TextRole("career.reason.breakable", "Saves may become incompatible with later development builds.", ""),
            new S0TextRole("career.reason.quit", "Save to a manual slot before leaving to keep changes since the last autosave.", ""),
            new S0TextRole("career.reason.unready", "Career storage is unavailable. Try again.", ""),
            new S0TextRole("career.reason.confirm", "Replace {0}? Its existing career save will be overwritten.", "S"),
            new S0TextRole("career.reason.error", "Could not complete the operation: {0}", "S"),
            new S0TextRole("career.reason.autosave_error", "Autosave failed: {0}. Your career has progressed. Try a manual save.", "S"),
            new S0TextRole("career.reason.saved", "Saved to {0}.", "S"),
            new S0TextRole("career.slot.autosave", "Autosave", ""),
            new S0TextRole("career.slot.manual", "Manual slot {0}", "I"),
            new S0TextRole("career.slot.empty", "Empty: no save in this slot.", ""),
            new S0TextRole("career.slot.ready", "Ready", ""),
            new S0TextRole("career.slot.damaged", "Damaged: this save cannot be loaded.", ""),
            new S0TextRole("career.slot.incompatible", "Incompatible with this build. Save migration is unavailable.", ""),
            new S0TextRole("career.slot.summary", "{0}: {1}. {2}, season {3}, day {4}, rounds {5}/{6}, position {7}.", "SSSISIII"),
            new S0TextRole("career.home.summary", "{0} | Season {1} | Day {2} | Rounds {3}/{4} | Position {5}", "SISIII"),
            new S0TextRole("career.home.attention", "Injured players: {0}. Board target: position {1} or better. {2} Job security: {3}/1000.", "IISI"),
            new S0TextRole("career.objective.on", "On track.", ""),
            new S0TextRole("career.objective.off", "Below target.", ""),
            new S0TextRole("career.next.fixture", "{0} vs {1}, {2}. Matchday {3}, day {4}; {5} day(s) away.", "SSSISI"),
            new S0TextRole("career.next.home", "home", ""),
            new S0TextRole("career.next.away", "away", ""),
            new S0TextRole("career.next.complete", "Season complete. Follow the available progress action.", ""),
            new S0TextRole("career.next.bye", "No managed fixture in the next round. Follow the available progress action.", ""),
            new S0TextRole("career.change.started", "Your career has started.", ""),
            new S0TextRole("career.change.resumed", "Career resumed from a save.", ""),
            new S0TextRole("career.change.day", "Advanced to day {0}.", "S"),
            new S0TextRole("career.change.round", "Matchday complete: {0} {1} - {2} {3}.", "SIIS"),
            new S0TextRole("career.change.round_bye", "Matchday complete. No managed fixture was played.", ""),
            new S0TextRole("career.change.roll", "Season complete: position {0}, target {1}. {2}", "IIS"),
            new S0TextRole("career.change.none", "No recent change.", ""),
            new S0TextRole("career.table.row", "{0}. {1} | Played {2} | W {3} D {4} L {5} | Goals {6}:{7} | GD {8} | Points {9}", "ISIIIIIISI"),
            new S0TextRole("career.new.selected", "Selected club: {0}. Board target: position {1} or better.", "SI"),
            new S0TextRole("career.new.club", "{0} | Select club", "S"),
        });
        private readonly S0TextFormatter _text;
        /// <summary>Uses one fixed admitted formatting context.</summary>
        public CareerScreenText(S0TextFormatter text) { _text = text ?? throw new ArgumentNullException(nameof(text)); }
        /// <summary>Resolves an authored career label.</summary>
        public string Label(string role) => _text.Label("career." + role);
        /// <summary>Formats a typed career role with a stable cache cell.</summary>
        public string Format(string cell, string role, params object[] values) => _text.Format(cell, "career." + role, values);
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
#endregion
