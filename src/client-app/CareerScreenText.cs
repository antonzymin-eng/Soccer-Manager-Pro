// File:     src/client-app/CareerScreenText.cs
// Created:  2026-11-03
// Modified: 2026-11-05 (§5-P8 transfer and finance copy)
// Author:   —
// Spec:     Interactive Unity client §5-P7, UX detailed plan §7.5, Localization #49 KD-5
// Purpose:  Separate career copy and host-free presentation formatting over the shared typed formatter.

using System;
using System.Text;

using TacticalDirector.Localization;
using TacticalDirector.SeasonSave;
using TacticalDirector.Transfers;

namespace TacticalDirector.ClientApp
{
    /// <summary>Career-only static keys and display patterns; no S0 catalogue/hash change.</summary>
    public sealed class CareerScreenText
    {
        private static S0TextRole R(string key, string text, string types = "") => new S0TextRole("ui.career.", key, text, types);
        private static readonly S0TextRole[] Roles =
        {
            R("new", "New career"), R("continue", "Continue career"), R("load", "Load career"),
            R("home", "Career home"), R("start", "Start career"), R("back", "Back"),
            R("quit", "Return to Main Menu (without saving)"), R("retry", "Retry career storage"),
            R("busy", "Working... Please wait; navigation and changes are unavailable."),
            R("no_save", "No loadable career save. Start a career or choose Load to inspect slots."),
            R("choose", "Choose a club. The league is generated; the default objective is a top-half finish."),
            R("select", "Select {0}", "S"), R("selected", "Selected: {0}", "S"),
            R("selection.ready", "Club selected. Ready to start."),
            R("warning", "Saves may become incompatible until the release-readiness checkpoint. Keep a manual save before leaving."),
            R("day", "Advance one day"), R("matchday", "Resolve matchday (on Career Home)"), R("season", "Start next season"),
            R("none", "No progress action available"), R("table", "League table"), R("save", "Save to manual slot {0}", "I"),
            R("slot.auto", "Autosave"), R("slot.manual", "Manual slot {0}", "I"),
            R("empty", "Empty"), R("ready", "Ready"), R("damaged", "Damaged or unreadable - cannot load"),
            R("incompatible", "Incompatible with this build - cannot load"),
            R("slot", "{0}: {1}", "SS"), R("slot.facts", "{0} | {1} | Season {2}, day {3}, round {4}/{5}, position {6}", "SSIIIII"),
            R("overview", "{0} | Season {1}, day {2} | Position {3} | Rounds {4}/{5}", "SIIIII"),
            R("attention", "Injured players: {0}. Board target: position {1} or better. {2}", "IIS"),
            R("ontrack", "Objective on track."), R("offtrack", "Objective off track."),
            R("next", "Next: {0} ({1}), day {2}, in {3} days.", "SSII"), R("home.venue", "home"), R("away.venue", "away"),
            R("complete", "No remaining fixture this season."), R("result", "Last result: {0} {1} - {2} {3}", "SIIS"),
            R("changed", "Last action: {0}", "S"), R("change.None", "No recent action"),
            R("change.Started", "Career started"), R("change.Resumed", "Career resumed"),
            R("change.DayAdvanced", "Day advanced"), R("change.RoundPlayed", "Round resolved"), R("change.SeasonRolled", "New season started"),
            R("row", "{0}. {1} | Played {2}, W {3}, D {4}, L {5}, GF {6}, GA {7}, points {8}", "ISIIIIIII"),
            R("autosave.error", "Autosave failed: {0}", "S"),
            // §5-P8 Transfers / Club Finances (ux-s2-transfers-finances.md §3–§4).
            R("transfers", "Transfers"), R("finances", "Club finances"),
            R("transfer.window.open", "Transfer window open until day {0}.", "I"),
            R("transfer.window.closed", "Transfer window closed. The next window opens at the start of next season."),
            R("transfer.budget", "Remaining budget: {0} of {1}.", "II"),
            R("transfer.squad", "Squad: {0} / {1}", "II"),
            R("transfer.tab.others", "Other clubs' players"), R("transfer.tab.own", "Your squad"),
            R("position.Goalkeeper", "Goalkeeper"), R("position.Defender", "Defender"),
            R("position.Midfielder", "Midfielder"), R("position.Forward", "Forward"),
            R("transfer.row", "{0} | {1} | {2} | age {3} | ability {4} | estimated value {5}", "SSSIII"),
            R("transfer.injured", "Injured"),
            R("transfer.contract", "Contract: {0} per period, {1} seasons", "II"), R("transfer.nocontract", "No contract"),
            R("transfer.offer", "Make offer"), R("transfer.fee", "Fee"), R("transfer.wage", "Wage per period"),
            R("transfer.length", "Contract seasons"), R("transfer.buyer", "Selling to"),
            R("transfer.outcome.Accepted", "Offer accepted. The transfer is complete."),
            R("transfer.outcome.Rejected", "Offer rejected: too far from their valuation."),
            R("transfer.outcome.CounterOffered", "Close: the club is open to a better offer."),
            R("transfer.outcome.InsufficientBudget", "Not enough budget: {0} remaining.", "I"),
            R("transfer.outcome.SquadFull", "Your squad is full ({0}). Sell a player first.", "I"),
            R("transfer.outcome.SquadDepleted.buy", "That club cannot release him without dropping below a matchday squad."),
            R("transfer.outcome.SquadDepleted.sell", "You cannot drop below a matchday squad."),
            R("finance.balance", "Balance: {0}", "I"), R("finance.balance.debt", "Balance: overdrawn by {0}", "I"),
            R("finance.transfer", "Transfer budget: {0} (committed {1}, remaining {2})", "III"),
            R("finance.wage", "Wage budget: {0}", "I"),
            R("finance.contracts", "Contracted wages: {0} per period across {1} contracts", "II"),
            R("finance.wagebill", "Wage bill: not tracked yet"),
            R("finance.settlement", "Budgets are set again at the end of each season.")
        };
        /// <summary>Creates an admitted lifetime-fixed career formatter, optionally with authored pseudo-locale rows.</summary>
        public CareerScreenText(TemplateCatalogue.StaticRow[] selectedRows = null, LocaleId? locale = null)
        {
            var rows = new TemplateCatalogue.StaticRow[Roles.Length];
            for (int i = 0; i < rows.Length; i++) rows[i] = new TemplateCatalogue.StaticRow(Roles[i].Key, Roles[i].BasePattern);
            Text = new S0TextFormatter(Roles, "ui.career.", new TemplateCatalogue(LocaleId.BaseLocale, rows), selectedRows, locale);
        }
        /// <summary>The admitted typed formatter, sharing the S0 formatting rules but no S0 content.</summary>
        public S0TextFormatter Text { get; }
        /// <summary>Static localized career label.</summary>
        public string Label(string role) => Text.Label(role);
        /// <summary>Current primary action label; no interactive career edge is promised.</summary>
        public string Progress(CareerProgressAction action) => Label(action == CareerProgressAction.AdvanceDay ? "day" :
            action == CareerProgressAction.PlayMatchday ? "matchday" : action == CareerProgressAction.StartNextSeason ? "season" : "none");
        /// <summary>Visible save state; incompatible/damaged slots remain listed.</summary>
        public string Slot(in CareerSlotSummary row)
        {
            string cell = "slot." + (int)row.Slot;
            string name = row.Slot == CareerSaveSlot.Autosave ? Label("slot.auto") : Text.Format(cell + ".name", "slot.manual", (int)row.Slot);
            string state = Label(row.State == CareerSlotState.Empty ? "empty" : row.State == CareerSlotState.Ready ? "ready" :
                row.State == CareerSlotState.Incompatible ? "incompatible" : "damaged");
            string status = Text.Format(cell + ".state", "slot", name, state);
            if (row.State == CareerSlotState.Empty || row.State == CareerSlotState.Damaged)
                return status;
            return Text.Format(cell + ".row", "slot.facts", status, row.ManagedClubName,
                row.SeasonNumber + 1, checked((int)row.WorldDay), row.RoundsPlayed, row.RoundCount, row.LeaguePosition);
        }
        /// <summary>Ordered career context, change, attention and next fixture, sourced only from the supplied snapshot.</summary>
        public string Summary(in CareerHomeViewModel home)
        {
            if (!home.HasCareer) return "";
            var s = new StringBuilder(Text.Format("overview", "overview", home.ManagedClubName, home.SeasonNumber + 1,
                checked((int)home.WorldDay), home.LeaguePosition, home.RoundsPlayed, home.RoundCount));
            s.AppendLine().Append(Text.Format("changed", "changed", Label("change." + home.LastChange.Kind)));
            if (home.LastChange.HasManagedResult)
            {
                MatchResult result = home.LastChange.ManagedResult;
                // MatchResult stores goals, not a second presentation score authority.
                s.AppendLine().Append(Text.Format("result", "result",
                    result.HomeClubId == home.ManagedClubId ? home.ManagedClubName : home.LastResultOpponentName,
                    result.HomeGoals, result.AwayGoals,
                    result.AwayClubId == home.ManagedClubId ? home.ManagedClubName : home.LastResultOpponentName));
            }
            s.AppendLine().Append(Text.Format("attention", "attention", home.InjuredPlayerCount, home.ObjectiveTargetPosition,
                Label(home.IsObjectiveOnTrack ? "ontrack" : "offtrack")));
            s.AppendLine().Append(home.HasNextFixture ? Text.Format("next", "next", home.NextOpponentName,
                Label(home.NextFixtureIsHome ? "home.venue" : "away.venue"), checked((int)home.NextFixtureDay), home.DaysUntilNextMatchday) : Label("complete"));
            return s.ToString();
        }
        /// <summary>Window line for the Transfers header (§5-P8).</summary>
        public string Window(in TransferMarketViewModel market) => market.IsWindowOpen
            ? Text.Format("transfer.window", "transfer.window.open", checked((int)market.WindowCloseDay))
            : Label("transfer.window.closed");

        /// <summary>Budget and squad lines for the Transfers header.</summary>
        public string Budget(in TransferMarketViewModel market) =>
            Text.Format("transfer.budget", "transfer.budget", Money(market.RemainingBudget), Money(market.TransferCeiling))
            + Environment.NewLine
            + Text.Format("transfer.squad", "transfer.squad", market.ManagedSquadCount, market.SquadCapacity);

        /// <summary>One player row; <paramref name="cell"/> keeps each list row's formatter cache separate.</summary>
        public string Player(int cell, in TransferPlayerRow row)
        {
            string line = Text.Format("transfer.row." + cell, "transfer.row", row.Name, row.ClubName,
                Label("position." + row.Position), row.Age, row.CurrentAbility, Money(row.EstimatedValue));
            if (!row.IsAvailable)
            {
                line += " | " + Label("transfer.injured");
            }

            if (row.IsManaged)
            {
                line += " | " + (row.HasContract
                    ? Text.Format("transfer.contract." + cell, "transfer.contract", Money(row.ContractWagePerPeriod), row.ContractSeasons)
                    : Label("transfer.nocontract"));
            }

            return line;
        }

        /// <summary>The message for a typed #31 outcome; <paramref name="isBuy"/> picks the squad-depleted wording.</summary>
        public string Outcome(TransferSubmissionOutcome outcome, bool isBuy, in TransferMarketViewModel market)
        {
            switch (outcome)
            {
                case TransferSubmissionOutcome.InsufficientBudget:
                    return Text.Format("transfer.outcome", "transfer.outcome.InsufficientBudget", Money(market.RemainingBudget));
                case TransferSubmissionOutcome.SquadFull:
                    return Text.Format("transfer.outcome", "transfer.outcome.SquadFull", market.SquadCapacity);
                case TransferSubmissionOutcome.SquadDepleted:
                    return Label(isBuy ? "transfer.outcome.SquadDepleted.buy" : "transfer.outcome.SquadDepleted.sell");
                case TransferSubmissionOutcome.Accepted:
                case TransferSubmissionOutcome.Rejected:
                case TransferSubmissionOutcome.CounterOffered:
                    return Label("transfer.outcome." + outcome);
                default:
                    throw new ArgumentOutOfRangeException(nameof(outcome), outcome, "Unknown transfer outcome.");
            }
        }

        /// <summary>The read-only Club Finances lines (§5-P8), in the Gate C order.</summary>
        public string Finances(in ClubFinancesViewModel finances)
        {
            if (!finances.HasCareer) return "";
            var s = new StringBuilder(finances.Balance < 0
                ? Text.Format("finance.balance", "finance.balance.debt", Money(-finances.Balance))
                : Text.Format("finance.balance", "finance.balance", Money(finances.Balance)));
            s.AppendLine().Append(Text.Format("finance.transfer", "finance.transfer", Money(finances.TransferBudget),
                Money(finances.CommittedTransferSpend), Money(finances.RemainingTransferBudget)));
            s.AppendLine().Append(Text.Format("finance.wage", "finance.wage", Money(finances.WageBudget)));
            s.AppendLine().Append(Text.Format("finance.contracts", "finance.contracts", Money(finances.ContractedWages), finances.ContractCount));
            s.AppendLine().Append(Label("finance.wagebill"));
            s.AppendLine().Append(Label("finance.settlement"));
            return s.ToString();
        }

        // The shared formatter's typed-integer slots are int; every #31/#40 amount in play fits (the #40 budget
        // ceiling is 50,000,000), and an amount that does not is a broken invariant, so it fails loud.
        private static int Money(long amount) => checked((int)amount);

        /// <summary>Table in the owning source's position order, without inferred statistics.</summary>
        public string Table(in CareerHomeViewModel home)
        {
            var s = new StringBuilder();
            if (home.Table == null) return "";
            for (int i = 0; i < home.Table.Count; i++)
            {
                LeagueTableRow row = home.Table[i];
                if (i > 0) s.AppendLine();
                s.Append(Text.Format("table.row." + i, "row", i + 1, home.TableClubNames[i], row.Played, row.Won, row.Drawn,
                    row.Lost, row.GoalsFor, row.GoalsAgainst, row.Points));
            }
            return s.ToString();
        }
    }
}
#region VersionHistory
// | Version | Date | Author | Notes |
// | 1.0 | 2026-11-03 | — | Isolated career role register, slot states and Home formatting. |
// | 1.1 | 2026-11-04 | — | Localized slot composition, neutral selection/change copy, D-10 disclosure and distinct row cache keys. |
// | 1.2 | 2026-11-05 | — | §5-P8: transfer/finance roles, window/budget/row/outcome/finance formatting; outcomes keyed by #31's typed result. |
#endregion
