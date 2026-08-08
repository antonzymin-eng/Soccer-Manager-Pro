// File:     src/injuries-medical/tests/MedicalStepTests.cs
// Created:  2026-08-05
// Modified: 2026-08-08 (AR pass 16 L2: the ceiling arm locked — v1.8)
// Author:   —
// Spec:     Injuries & Medical #41 §3.1–§3.4 + Appendices A/B/C; Code Standards #20
// Purpose:  T-MD-DET-001/003/005/006/007/009, T-MD-ORD-001, T-MD-SEV-001/002, T-MD-REC-001,
//           T-MD-MOD-001/002, T-MD-NEU-001/002, T-MD-AVAIL-001, T-MD-FAT-001, T-MD-FAIL-004/006 — the
//           §3.6 worked example, the KD-6 same-call gate, the keyed-draw properties, the #29 -> #41
//           seam, and the fail-loud gates. Plus one test carrying no spec id: the daily occurrence
//           PROBABILITY through the real producer chain, recorded as the balance pass's baseline.

using System;

using NUnit.Framework;

using TacticalDirector.PlayerDatabase;
using TacticalDirector.TrainingSystem;

namespace TacticalDirector.InjuriesMedical.Tests
{
    [TestFixture]
    public sealed class MedicalStepTests
    {
        private const ulong WorldSeed = 0x5EED_1234_ABCD_0001UL;
        private const int PlayerId = 501;

        /// <summary>Attributes whose three robustness fields mean exactly 14 — §3.6's worked example.</summary>
        private static PlayerAttributes WorkedExampleAttributes()
        {
            PlayerAttributes a = PlayerAttributes.CreateDefault();
            a.Strength = 14;
            a.Stamina = 14;
            a.Balance = 14;
            return a;
        }

        /// <summary>
        /// A risk input that saturates the <c>InjuryRiskMax</c> clamp — the HIGHEST risk any input can
        /// produce. Since ERR-041-011 that is a 1.6% daily probability, not certainty: the draw is
        /// uniform in the ~60×-larger <c>OCCURRENCE_DRAW_DENOM</c>, so "an occurrence happens" is
        /// forced by pairing this with a day whose keyed draw is known to fall under the clamp
        /// (<see cref="HotDay"/>) — deterministic, because the draw is a pure function of
        /// <c>(seed, playerId, day)</c>.
        /// </summary>
        private static InjuryRiskContribution MaxOccurrenceRisk() =>
            new InjuryRiskContribution(InjuriesMedicalConstants.InjuryRiskMax * 4);

        /// <summary>
        /// The first world day at or after <paramref name="start"/> whose keyed occurrence draw for
        /// <paramref name="playerId"/> falls below the <c>InjuryRiskMax</c> clamp — a day on which a
        /// max-risk player IS injured. Pure scan over the keyed derivation; ~1.6% of days qualify, so
        /// the bound is generous and the result is a constant of (WorldSeed, playerId).
        /// </summary>
        private static uint HotDay(uint start, int playerId = PlayerId)
        {
            for (uint day = start; day < start + 10_000; day++)
            {
                int draw = MedicalStep.DrawOccurrence(
                    WorldSeed, playerId,
                    MedicalStep.DeriveActionOrdinal(day, InjuriesMedicalConstants.DRAW_PURPOSE_OCCURRENCE));
                if (draw < InjuriesMedicalConstants.InjuryRiskMax)
                {
                    return day;
                }
            }

            Assert.Fail("No hot day within 10,000 of day " + start + " — the draw distribution is broken.");
            return 0;
        }

        /// <summary>The first player id whose keyed draw on world day 0 falls below the clamp — for the day-0 sentinel test, whose day cannot move.</summary>
        private static int HotPlayerOnDayZero()
        {
            ulong ordinal = MedicalStep.DeriveActionOrdinal(0, InjuriesMedicalConstants.DRAW_PURPOSE_OCCURRENCE);
            for (int pid = 1; pid < 100_000; pid++)
            {
                if (MedicalStep.DrawOccurrence(WorldSeed, pid, ordinal) < InjuriesMedicalConstants.InjuryRiskMax)
                {
                    return pid;
                }
            }

            Assert.Fail("No hot player on day 0 within 100,000 ids — the draw distribution is broken.");
            return 0;
        }

        /// <summary>
        /// The LOWEST risk the assembly produces for the worked-example player —
        /// <c>InjuryRiskContribution.None</c> assembles to <c>BaselineDailyRisk − 400 = 3600</c>
        /// (0.36%/day), NOT zero: since ERR-041-011 nobody is injury-proof, so "no occurrence
        /// happens" in the tests that walk days with this input holds because today's keyed draws
        /// over those specific (seed, player, day) windows contain no sub-3600 draw — deterministic,
        /// but a reseed would need re-checking (the AR pass-1 note; renamed from
        /// ImpossibleOccurrenceRisk, which the baseline term made a lie).
        /// </summary>
        private static InjuryRiskContribution MinimumOccurrenceRisk() => InjuryRiskContribution.None;

        private static void Advance(
            ref InjuryState state,
            uint worldDay,
            InjuryRiskContribution risk,
            bool occurrenceEnabled = true,
            int playerId = PlayerId)
        {
            PlayerAttributes a = WorkedExampleAttributes();
            MedicalStep.AdvanceMedicalDay(
                ref state, playerId, a, risk, MatchLoad.None, MedicalModifier.Identity,
                worldDay, WorldSeed, occurrenceEnabled);
        }

        // ── §3.6 — the worked example, term by term ─────────────────────────────────

        [Test]
        public void WorkedExample_RiskAssembly_Is6600()
        {
            // §3.6 as re-derived at ERR-041-011: 1×3000 + 4000 (baseline, before the mitigation —
            // position normative) − 400 = 6600, unchanged by the ×1000/1000 identity multiplier.
            int risk = MedicalStep.AssembleRiskScore(
                new InjuryRiskContribution(3000),
                MatchLoad.None,
                WorkedExampleAttributes(),
                MedicalModifier.Identity);

            Assert.AreEqual(6600, risk);

            // And the same player in a congested week saturates the clamp: 3000 + 2×5600 + 4000 −
            // 400 = 17800 → InjuryRiskMax (16000). The ceiling is the 1.6% hard cap, not the formula.
            Assert.AreEqual(InjuriesMedicalConstants.InjuryRiskMax, MedicalStep.AssembleRiskScore(
                new InjuryRiskContribution(3000),
                new MatchLoad(appearanceDays: 2, hardContacts: 0),
                WorkedExampleAttributes(),
                MedicalModifier.Identity));
        }

        [Test]
        public void WorkedExample_Draw3500AgainstRisk6600_IsMinor_TTMDSEV001()
        {
            // 3500 × 1000 = 3_500_000 < 6600 × 600 = 3_960_000 ⇒ Minor. Integer cross-multiply.
            Assert.AreEqual(InjurySeverity.Minor, MedicalStep.ClassifySeverityFromDraw(draw: 3500, risk: 6600));
            Assert.AreEqual(7, MedicalStep.AssignRecoveryDays(InjurySeverity.Minor, MedicalModifier.Identity));
        }

        [Test]
        public void SeverityBoundaries_LandInTheHigherTier_TTMDSEV002()
        {
            // With risk = 1000 the per-mille numerators are the boundaries directly: Minor below 600,
            // Moderate below 900, Serious above. A draw exactly AT a boundary belongs to the higher
            // tier, because the comparison is strictly less-than.
            Assert.AreEqual(InjurySeverity.Minor, MedicalStep.ClassifySeverityFromDraw(599, 1000));
            Assert.AreEqual(InjurySeverity.Moderate, MedicalStep.ClassifySeverityFromDraw(600, 1000));
            Assert.AreEqual(InjurySeverity.Moderate, MedicalStep.ClassifySeverityFromDraw(899, 1000));
            Assert.AreEqual(InjurySeverity.Serious, MedicalStep.ClassifySeverityFromDraw(900, 1000));
        }

        [Test]
        public void RecoveryCountdown_RunsTheTierToZeroThenHeals_TTMDREC001()
        {
            var state = InjuryState.Create();
            state.Severity = InjurySeverity.Minor;
            state.RecoveryRemaining = 7;
            state.LastAdvancedWorldDay = 205;

            // Days 206..211 tick the counter down without healing; only day 212 reaches zero.
            for (uint day = 206; day <= 211; day++)
            {
                Advance(ref state, day, MinimumOccurrenceRisk());
                Assert.AreEqual(InjurySeverity.Minor, state.Severity, "still injured on day " + day);
            }

            Assert.AreEqual(1, state.RecoveryRemaining);

            Advance(ref state, 212, MinimumOccurrenceRisk());
            Assert.AreEqual(InjurySeverity.None, state.Severity);
            Assert.AreEqual(0, state.RecoveryRemaining);
        }

        // ── KD-6 — the same-call ordering guarantee ─────────────────────────────────

        [Test]
        public void RecoveringToZero_CannotAlsoReinjure_InOneCall_TTMDORD001()
        {
            // A day whose keyed draw falls under the max-risk clamp: an eligible max-risk player IS
            // injured on this day, so if the gate read the post-countdown state instead of the entry
            // state, the call below would heal and immediately re-injure.
            uint hot = HotDay(1);

            var state = InjuryState.Create();
            state.Severity = InjurySeverity.Minor;
            state.RecoveryRemaining = 1;
            state.LastAdvancedWorldDay = hot - 1;
            state.InjuryCount = 1;

            Advance(ref state, hot, MaxOccurrenceRisk());

            Assert.AreEqual(InjurySeverity.None, state.Severity, "recovered this call...");
            Assert.AreEqual(1, state.InjuryCount, "...and NOT re-injured by the same call (KD-6 / FR-MD-004).");

            // Walk day by day (F7 forbids a gap) to the NEXT hot day: the player is healthy at entry
            // there, so the occurrence fires.
            uint next = HotDay(hot + 1);
            for (uint day = hot + 1; day < next; day++)
            {
                Advance(ref state, day, MinimumOccurrenceRisk());
            }

            Advance(ref state, next, MaxOccurrenceRisk());

            Assert.AreNotEqual(InjurySeverity.None, state.Severity);
            Assert.AreEqual(2, state.InjuryCount);
            Assert.Greater(state.RecoveryRemaining, 0, "F1: an injured player always has recovery outstanding.");
        }

        // ── Idempotency, day-0, day gap ─────────────────────────────────────────────

        [Test]
        public void AlreadyAdvancedDay_IsANoOp_TTMDDET005()
        {
            // A hot day, so the first advance really draws an occurrence — a re-run that re-drew
            // would be visible in InjuryCount rather than vacuously equal.
            uint hot = HotDay(1);
            var state = InjuryState.Create();
            Advance(ref state, hot, MaxOccurrenceRisk());
            Assert.AreEqual(1, state.InjuryCount, "precondition: the first advance drew an occurrence");
            InjuryState afterFirst = state;

            Advance(ref state, hot, MaxOccurrenceRisk());
            Advance(ref state, hot - 1, MaxOccurrenceRisk());

            Assert.AreEqual(afterFirst.Severity, state.Severity);
            Assert.AreEqual(afterFirst.RecoveryRemaining, state.RecoveryRemaining);
            Assert.AreEqual(afterFirst.InjuryCount, state.InjuryCount,
                "re-running an advanced day must not draw again (F6).");
            Assert.AreEqual(afterFirst.LastAdvancedWorldDay, state.LastAdvancedWorldDay);
        }

        [Test]
        public void FreshState_AdvancesWorldDayZero_TTMDDET006()
        {
            var state = InjuryState.Create();
            Assert.AreEqual(InjuriesMedicalConstants.MEDICAL_NOT_ADVANCED_SENTINEL, state.LastAdvancedWorldDay);

            // Day 0 cannot move, so the PLAYER moves: one whose keyed draw on day 0 is hot, proving
            // the day really was evaluated rather than skipped.
            Advance(ref state, 0, MaxOccurrenceRisk(), playerId: HotPlayerOnDayZero());

            // With a 0 sentinel this call would have read as "already advanced" and the player would
            // never have been evaluated at all.
            Assert.AreEqual(0u, state.LastAdvancedWorldDay);
            Assert.AreEqual(1, state.InjuryCount, "world day 0 is a legitimate first evaluation day.");
        }

        [Test]
        public void DayGap_FailsLoud_TTMDDET007()
        {
            var state = InjuryState.Create();
            Advance(ref state, 100, MinimumOccurrenceRisk());

            PlayerAttributes a = WorkedExampleAttributes();
            InjuryRiskContribution risk = MinimumOccurrenceRisk();

            Assert.Throws<ArgumentException>(
                () => MedicalStep.AdvanceMedicalDay(
                    ref state, PlayerId, a, risk, MatchLoad.None, MedicalModifier.Identity,
                    102, WorldSeed, occurrenceEnabled: true),
                "a gap silently under-advances recovery AND skips an occurrence evaluation (F7).");

            Assert.AreEqual(100u, state.LastAdvancedWorldDay, "a refused advance mutates nothing.");
        }

        [Test]
        public void AdvancingTheSentinelDay_FailsLoud()
        {
            var state = InjuryState.Create();
            PlayerAttributes a = WorkedExampleAttributes();
            InjuryRiskContribution risk = MinimumOccurrenceRisk();

            Assert.Throws<ArgumentException>(
                () => MedicalStep.AdvanceMedicalDay(
                    ref state, PlayerId, a, risk, MatchLoad.None, MedicalModifier.Identity,
                    InjuriesMedicalConstants.MEDICAL_NOT_ADVANCED_SENTINEL, WorldSeed, occurrenceEnabled: true));
        }

        // ── KD-1 — the keyed, position-independent draw ─────────────────────────────

        [Test]
        public void Draw_IsInRange_AndSeparatesNeighbouringKeys()
        {
            ulong ordinal = MedicalStep.DeriveActionOrdinal(213, 0);

            int a = MedicalStep.DrawOccurrence(WorldSeed, 501, ordinal);
            int b = MedicalStep.DrawOccurrence(WorldSeed, 502, ordinal);
            int c = MedicalStep.DrawOccurrence(WorldSeed, 501, MedicalStep.DeriveActionOrdinal(214, 0));
            int d = MedicalStep.DrawOccurrence(WorldSeed + 1, 501, ordinal);

            foreach (int draw in new[] { a, b, c, d })
            {
                Assert.GreaterOrEqual(draw, 0);
                Assert.Less(draw, InjuriesMedicalConstants.OCCURRENCE_DRAW_DENOM);
            }

            // Adjacent player ids, adjacent days and adjacent seeds must not produce correlated
            // injury luck — each key component goes through a full finalizer for exactly this reason.
            Assert.AreNotEqual(a, b);
            Assert.AreNotEqual(a, c);
            Assert.AreNotEqual(a, d);
        }

        [Test]
        public void ActionOrdinal_IsAFixedRadixBijection_TTMDDET009()
        {
            const uint Day = 205;

            Assert.AreEqual(
                (ulong)Day * InjuriesMedicalConstants.DRAW_PURPOSE_RADIX,
                MedicalStep.DeriveActionOrdinal(Day, InjuriesMedicalConstants.DRAW_PURPOSE_OCCURRENCE));

            // Append parity: adding a future purpose ordinal leaves every existing
            // (worldDay, Occurrence) ordinal untouched, because the radix is fixed rather than the
            // purpose COUNT. A count-based radix would shift all of them and change history.
            Assert.AreEqual(
                MedicalStep.DeriveActionOrdinal(Day, 0) + 1,
                MedicalStep.DeriveActionOrdinal(Day, 1));
            Assert.AreNotEqual(
                MedicalStep.DeriveActionOrdinal(Day, InjuriesMedicalConstants.DRAW_PURPOSE_RADIX - 1),
                MedicalStep.DeriveActionOrdinal(Day + 1, 0),
                "the radix must keep one day's purposes clear of the next day's.");

            Assert.Throws<ArgumentOutOfRangeException>(
                () => MedicalStep.DeriveActionOrdinal(Day, InjuriesMedicalConstants.DRAW_PURPOSE_RADIX));
        }

        [Test]
        public void SaveRestore_AcrossADrawBoundary_ContinuesIdentically_TTMDDET001()
        {
            var uninterrupted = InjuryState.Create();
            for (uint day = 200; day <= 213; day++)
            {
                Advance(ref uninterrupted, day, new InjuryRiskContribution(2900));
            }

            // At T0 there is no codec yet, so the "save" is a value copy of the whole state. What it
            // pins is that NOTHING outside InjuryState carries continuation state — no cursor, nothing
            // to lose by saving immediately after a draw resolves (FR-MD-007).
            InjuryState restored = uninterrupted;

            for (uint day = 214; day <= 230; day++)
            {
                Advance(ref uninterrupted, day, new InjuryRiskContribution(2900));
                Advance(ref restored, day, new InjuryRiskContribution(2900));
            }

            Assert.AreEqual(uninterrupted.Severity, restored.Severity);
            Assert.AreEqual(uninterrupted.RecoveryRemaining, restored.RecoveryRemaining);
            Assert.AreEqual(uninterrupted.InjuryCount, restored.InjuryCount);
            Assert.AreEqual(uninterrupted.LastAdvancedWorldDay, restored.LastAdvancedWorldDay);
        }

        [Test]
        public void TwoPlayersOnTheSameDay_DoNotInfluenceEachOther_TTMDDET003()
        {
            var alone = InjuryState.Create();
            var interleaved = InjuryState.Create();
            var neighbour = InjuryState.Create();

            for (uint day = 300; day <= 320; day++)
            {
                Advance(ref alone, day, new InjuryRiskContribution(2900), playerId: 700);
            }

            for (uint day = 300; day <= 320; day++)
            {
                Advance(ref neighbour, day, new InjuryRiskContribution(2900), playerId: 701);
                Advance(ref interleaved, day, new InjuryRiskContribution(2900), playerId: 700);
            }

            Assert.AreEqual(alone.Severity, interleaved.Severity);
            Assert.AreEqual(alone.InjuryCount, interleaved.InjuryCount,
                "the club's roster-iteration order must not change any player's injury history — that " +
                "is the whole point of a keyed draw over a shared cursor (FR-MD-006).");
            Assert.AreEqual(alone.RecoveryRemaining, interleaved.RecoveryRemaining);
        }

        // ── Behaviour-neutral identity (Appendix C) ─────────────────────────────────

        [Test]
        public void DialOff_IsRecoveryOnly_NoInjuryEverOccurs_TTMDNEU001()
        {
            var state = InjuryState.Create();

            // Walk through at least one KNOWN-hot day at max risk, or the zero-injury assertion is
            // satisfiable by luck rather than by the dial.
            uint lastDay = HotDay(0) + 10;
            for (uint day = 0; day <= lastDay; day++)
            {
                Advance(ref state, day, MaxOccurrenceRisk(), occurrenceEnabled: false);
            }

            Assert.AreEqual(InjurySeverity.None, state.Severity);
            Assert.AreEqual(0, state.InjuryCount,
                "with the dial off, severity can only ever fall toward None — no draw is issued at " +
                "all, even through a day whose draw WOULD have injured a max-risk player.");
            Assert.AreEqual(lastDay, state.LastAdvancedWorldDay, "the cursor still advances; only the draw is skipped.");
        }

        [Test]
        public void CreateIsTheHealthyIdentity_TTMDNEU002()
        {
            var state = InjuryState.Create();

            Assert.AreEqual(InjurySeverity.None, state.Severity);
            Assert.AreEqual(0, state.RecoveryRemaining);
            Assert.AreEqual(0, state.InjuryCount);
            Assert.AreEqual(InjuriesMedicalConstants.MEDICAL_NOT_ADVANCED_SENTINEL, state.LastAdvancedWorldDay);
            Assert.IsTrue(MedicalStep.IsAvailable(state));
        }

        // ── Availability, staff seam, fail-loud gates ───────────────────────────────

        [Test]
        public void IsAvailable_IsTrueOnlyWhenHealthy_TTMDAVAIL001()
        {
            var state = InjuryState.Create();
            Assert.IsTrue(MedicalStep.IsAvailable(state));

            foreach (InjurySeverity severity in Enum.GetValues(typeof(InjurySeverity)))
            {
                state.Severity = severity;
                Assert.AreEqual(severity == InjurySeverity.None, MedicalStep.IsAvailable(state));
            }
        }

        [Test]
        public void IdentityModifier_LeavesBothTermsUnscaled_TTMDMOD001()
        {
            Assert.AreEqual(
                InjuriesMedicalConstants.RecoveryDaysFor(InjurySeverity.Moderate),
                MedicalStep.AssignRecoveryDays(InjurySeverity.Moderate, MedicalModifier.Identity),
                "KD-5: a no-staff game recovers in exactly the tier's recovery-days constant.");

            Assert.AreEqual(
                3000 + InjuriesMedicalConstants.BaselineDailyRisk - 400,
                MedicalStep.AssembleRiskScore(
                    new InjuryRiskContribution(3000), MatchLoad.None, WorkedExampleAttributes(),
                    MedicalModifier.Identity),
                "the identity occurrence multiplier leaves the assembled score at #29's contribution " +
                "plus the baseline term less the mean-14 robustness row (ERR-041-011).");
        }

        [Test]
        public void RecoverySpeed_ScalesAssignedDays_NotThePerTickDecrement_TTMDMOD002()
        {
            var fastPhysio = new MedicalModifier(
                InjuriesMedicalConstants.MEDICAL_MODIFIER_IDENTITY_PERMILLE,
                InjuriesMedicalConstants.MEDICAL_MODIFIER_IDENTITY_PERMILLE * 2);

            int tierDays = InjuriesMedicalConstants.RecoveryDaysFor(InjurySeverity.Moderate);

            // A per-tick multiplier against a fixed integer base of 1 would truncate to a no-op and a
            // "twice as fast" physio would change nothing at all. Applying it once, to the assigned
            // days, is what makes the seam actually do something (FR-MD-014).
            Assert.AreEqual(tierDays / 2, MedicalStep.AssignRecoveryDays(InjurySeverity.Moderate, fastPhysio));

            var absurdPhysio = new MedicalModifier(
                InjuriesMedicalConstants.MEDICAL_MODIFIER_IDENTITY_PERMILLE,
                InjuriesMedicalConstants.MEDICAL_MODIFIER_IDENTITY_PERMILLE * 10000);

            Assert.AreEqual(1, MedicalStep.AssignRecoveryDays(InjurySeverity.Minor, absurdPhysio),
                "the floor of 1 is load-bearing: 0 assigned days would leave RecoveryRemaining == 0 " +
                "while Severity != None, an F1 breach written straight into the save.");

            // The CEILING arm (AR pass 16 L2 — locked for the first time: replacing RecoveryMax
            // with int.MaxValue in the clamp left all 67 tests green while producing the state
            // ValidateState refuses at slot 4). A slow physio (mult 200) on the Serious tier takes
            // the raw division to 60 * 1000 / 200 = 300, past the 240 ceiling FR-MD-014 pins.
            var slowPhysio = new MedicalModifier(
                InjuriesMedicalConstants.MEDICAL_MODIFIER_IDENTITY_PERMILLE, 200);
            Assert.AreEqual(InjuriesMedicalConstants.RecoveryMax,
                MedicalStep.AssignRecoveryDays(InjurySeverity.Serious, slowPhysio),
                "the ceiling arm: an assignment past RECOVERY_MAX would be persisted by the codec " +
                "and refused by ValidateState the next day — the clamp keeps it in the field's " +
                "declared range (FR-MD-014 as completed at AR pass 15 M2).");

            // ...and the same floor inverts on the None tier, whose recovery-days are 0 by the F1
            // invariant: floored to 1 it would write RecoveryRemaining == 1 against Severity == None.
            // No caller passes None today (ClassifySeverityFromDraw cannot return it); the guard keeps
            // that true for the next one rather than leaving a breach one call site away.
            Assert.Throws<ArgumentOutOfRangeException>(
                () => MedicalStep.AssignRecoveryDays(InjurySeverity.None, MedicalModifier.Identity));
        }

        [Test]
        public void ZeroRecoverySpeed_FailsLoud_TTMDFAIL006()
        {
            var state = InjuryState.Create();
            PlayerAttributes a = WorkedExampleAttributes();
            InjuryRiskContribution risk = MaxOccurrenceRisk();

            Assert.Throws<ArgumentException>(
                () => MedicalStep.AdvanceMedicalDay(
                    ref state, PlayerId, a, risk, MatchLoad.None, default(MedicalModifier),
                    100, WorldSeed, occurrenceEnabled: true),
                "default(MedicalModifier) is all-zero: ×0 risk and a divide-by-zero recovery scale.");

            Assert.Throws<ArgumentException>(
                () => MedicalStep.AssembleRiskScore(risk, MatchLoad.None, a, default(MedicalModifier)));
        }

        [Test]
        public void IncoherentState_FailsLoud_TTMDFAIL004()
        {
            PlayerAttributes a = WorkedExampleAttributes();
            InjuryRiskContribution risk = MinimumOccurrenceRisk();

            var healthyButRecovering = InjuryState.Create();
            healthyButRecovering.RecoveryRemaining = 5;

            Assert.Throws<ArgumentException>(
                () => MedicalStep.AdvanceMedicalDay(
                    ref healthyButRecovering, PlayerId, a, risk, MatchLoad.None, MedicalModifier.Identity,
                    100, WorldSeed, occurrenceEnabled: false));

            var injuredButHealed = InjuryState.Create();
            injuredButHealed.Severity = InjurySeverity.Serious;

            Assert.Throws<ArgumentException>(
                () => MedicalStep.AdvanceMedicalDay(
                    ref injuredButHealed, PlayerId, a, risk, MatchLoad.None, MedicalModifier.Identity,
                    100, WorldSeed, occurrenceEnabled: false));
        }

        [Test]
        public void UndefinedSeverityOnState_FailsLoud_F4()
        {
            PlayerAttributes a = WorkedExampleAttributes();
            InjuryRiskContribution risk = MinimumOccurrenceRisk();

            var state = InjuryState.Create();
            state.Severity = (InjurySeverity)200;
            state.RecoveryRemaining = 3;

            Assert.Throws<ArgumentOutOfRangeException>(
                () => MedicalStep.AdvanceMedicalDay(
                    ref state, PlayerId, a, risk, MatchLoad.None, MedicalModifier.Identity,
                    100, WorldSeed, occurrenceEnabled: false));
        }

        // ── The #29 boundary ────────────────────────────────────────────────────────

        [Test]
        public void RiskAssembly_RisesWithTrainingAndLoad_AndFallsWithRobustness()
        {
            PlayerAttributes ordinary = WorkedExampleAttributes();
            PlayerAttributes robust = PlayerAttributes.CreateDefault();
            robust.Strength = 20;
            robust.Stamina = 20;
            robust.Balance = 20;

            int baseline = MedicalStep.AssembleRiskScore(
                new InjuryRiskContribution(3000), MatchLoad.None, ordinary, MedicalModifier.Identity);

            int moreTraining = MedicalStep.AssembleRiskScore(
                new InjuryRiskContribution(4000), MatchLoad.None, ordinary, MedicalModifier.Identity);

            int moreMatches = MedicalStep.AssembleRiskScore(
                new InjuryRiskContribution(3000), new MatchLoad(1, 0), ordinary, MedicalModifier.Identity);
            Assert.Less(moreMatches, InjuriesMedicalConstants.InjuryRiskMax,
                "precondition: unclamped, or the magnitude comparisons below are clamp artefacts.");

            int moreRobust = MedicalStep.AssembleRiskScore(
                new InjuryRiskContribution(3000), MatchLoad.None, robust, MedicalModifier.Identity);

            Assert.Greater(moreTraining, baseline, "#29's contribution passes through with weight 1.");
            Assert.Greater(moreMatches, baseline, "the Stage-2 match-load term.");
            Assert.Less(moreRobust, baseline, "the own-attribute mitigation, deterministic and never RNG.");
        }

        // ── The #29 → #41 seam (the reason both assemblies landed together) ─────────

        [Test]
        public void TrainingRiskFlowsFromTheProducerIntoTheOccurrenceRisk_TTMDFAT001()
        {
            // The one cross-assembly contract in this landing: #29 publishes the scalar (FR-TR-017),
            // #41 consumes it read-only (FR-MD-009). Every other test here hand-builds an
            // InjuryRiskContribution with a literal, which would not notice a scale or units mismatch
            // between the producer and the consumer — so drive the real producer.
            PlayerAttributes a = WorkedExampleAttributes();

            TrainingState fresh = TrainingState.Create(TrainingFocus.Balanced);
            fresh.Condition = TrainingSystemConstants.ConditionMax;
            fresh.TrainingFatigue = 0;

            TrainingState worn = TrainingState.Create(TrainingFocus.Fitness);
            worn.Condition = TrainingSystemConstants.ConditionMax / 2;
            worn.TrainingFatigue = TrainingSystemConstants.TrainingFatigueMax / 2;

            int freshRisk = MedicalStep.AssembleRiskScore(
                TrainingStep.ComputeInjuryRisk(fresh, a), MatchLoad.None, a, MedicalModifier.Identity);
            int wornRisk = MedicalStep.AssembleRiskScore(
                TrainingStep.ComputeInjuryRisk(worn, a), MatchLoad.None, a, MedicalModifier.Identity);

            Assert.Greater(wornRisk, freshRisk,
                "a tired, under-conditioned player must be at higher occurrence risk than a fresh one — " +
                "if the two systems disagreed about the scale, this ordering is what would break.");

            // Both ends of #29's clamped output must land inside the range #41 draws against, or the
            // comparison in §3.1 is not on one scale (the coupling High-1 of AR pass 1 turned on).
            Assert.GreaterOrEqual(freshRisk, 0);
            Assert.LessOrEqual(wornRisk, InjuriesMedicalConstants.InjuryRiskMax);

            // The worst case a real player can reach — and the recorded fact that it does NOT saturate.
            TrainingState wrecked = TrainingState.Create(TrainingFocus.Physical);
            wrecked.Condition = TrainingSystemConstants.ConditionMin;
            wrecked.TrainingFatigue = TrainingSystemConstants.TrainingFatigueMax;
            PlayerAttributes frail = PlayerAttributes.CreateDefault();
            frail.Strength = 1;
            frail.Stamina = 1;
            frail.Balance = 1;

            int maxRisk = MedicalStep.AssembleRiskScore(
                TrainingStep.ComputeInjuryRisk(wrecked, frail), MatchLoad.None, frail, MedicalModifier.Identity);

            // BOTH layers mitigate on the SAME three physical attributes: #29 §3.4 subtracts its
            // robustness term before clamping, then #41 §3.4 subtracts its own from the result. Each
            // spec mandates its own term, so this is spec-faithful and recorded (ERR-041-003). Since
            // the balance pass (ERR-041-011) the old "never reaches the consumer's ceiling" fact is
            // GONE — BaselineDailyRisk pushes the worst case past the clamp, deliberately — so the
            // recorded property is now the clamp itself: the worst reachable case saturates at
            // InjuryRiskMax exactly, and InjuryRiskMax over the [FIXED] denominator is the hard daily
            // probability ceiling (1.6% at today's values). Anchored to the [GT] ceiling, NOT to the
            // 100x-larger denominator — an assertion against the denominator would be the
            // always-true-disjunction class this suite has already burned on.
            Assert.Greater(maxRisk, 0, "the worst realistic player must still carry real risk.");
            Assert.AreEqual(InjuriesMedicalConstants.InjuryRiskMax, maxRisk,
                "the worst reachable case saturates the clamp — the ceiling is real, not decorative.");
            Assert.LessOrEqual(maxRisk, InjuriesMedicalConstants.OCCURRENCE_DRAW_DENOM,
                "and the clamp is what keeps every daily probability <= 1 (ERR-041-011 invariant).");
        }

        /// <summary>
        /// The rate the wired system actually produces, measured through the real #29 → #41 chain.
        /// <para>
        /// <b>The balance pass's AFTER numbers</b> (ERR-041-011; the BEFORE numbers this test locked
        /// from T0 to the pass were 23.1% / exactly-0-forever / 43.1% per day — two to three orders of
        /// magnitude out in both directions). Under the retune the same three career states, plus the
        /// appearance rows the BEFORE version could not have (ERR-041-010(b) had no record to read),
        /// land in the football band: fractions of a percent per day, matches the dominant term, and
        /// a hard 1.6% ceiling from the <c>InjuryRiskMax</c> clamp.
        /// </para>
        /// <para>
        /// Asserted in <b>per-hundred-thousand</b>, not per-mille: at the per-million denominator a
        /// per-mille read is <c>risk / 1000</c>, which hides ±300 of risk inside one output unit —
        /// the resolution trap the evidence advisor flagged on the old helper.
        /// </para>
        /// </summary>
        [Test]
        public void DailyOccurrenceProbability_ThroughTheRealChain_MatchesTheBalancePassFit()
        {
            PlayerAttributes average = PlayerAttributes.CreateDefault();

            // 1. The state #30 inserts for a regen at the season boundary — fully rested, perfectly
            //    average, never trained a day. The conditioning shortfall now reads as an elevated
            //    ramp-in risk (0.63%/day, decaying as conditioning climbs over ~39 days), not the
            //    BEFORE version's 23%-per-day absurdity.
            Assert.AreEqual(631, DailyOccurrencePer100k(TrainingState.Create(TrainingFocus.Balanced), average),
                "a freshly inserted regen carries 0.63%/day during ramp-in.");

            // 2. The steady state of the DEFAULT focus. Balanced's fatigue still nets exactly 0 and
            //    conditioning still reaches the ceiling — but BaselineDailyRisk keeps a fit player at
            //    a real 0.37%/day floor (≈1 injury per season with no matches), where the BEFORE
            //    version converged on exactly 0 forever: a subsystem indistinguishable from OFF.
            TrainingState peak = TrainingState.Create(TrainingFocus.Balanced);
            peak.Condition = TrainingSystemConstants.ConditionMax;
            peak.TrainingFatigue = 0;
            Assert.AreEqual(371, DailyOccurrencePer100k(peak, average),
                "the default focus floors at the baseline term, not at injury-proof-forever.");

            // 3. Sustained half-fatigue on a Fitness regime: elevated (0.83%/day ≈ +2.2x the fit
            //    floor), no longer the BEFORE version's 43%/day.
            TrainingState halfSpent = TrainingState.Create(TrainingFocus.Fitness);
            halfSpent.Condition = TrainingSystemConstants.ConditionMax;
            halfSpent.TrainingFatigue = TrainingSystemConstants.TrainingFatigueMax / 2;
            Assert.AreEqual(831, DailyOccurrencePer100k(halfSpent, average),
                "half-fatigued sits at 0.83%/day — elevated, not absurd.");

            // 4. The appearance term, through the real chain (the pass's LARGEST constant — the old
            //    characterization passed MatchLoad.None and left it unmeasured). One match in the
            //    window adds 0.56%/day for the window's 7 days: ≈3.9% cumulative per match, ~1.5
            //    match-driven injuries over a 38-round season for an ever-present starter (E-1's
            //    match:training dominance).
            Assert.AreEqual(931, DailyOccurrencePer100k(peak, average, new MatchLoad(1, 0)),
                "a fit starter the week after a match: 0.93%/day.");

            // 5. Two matches in one window: congestion is PRICED (1.49%/day), no longer flattened
            //    into the cap — the AR pass-1 ceiling raise (10000 → 16000) is what bought this row
            //    its own value instead of the clamp's. FORMULA PROBE, not live behaviour (AR pass
            //    5): with DaysBetweenRounds == AppearanceWindowDays == 7 the wired Stage-0 schedule
            //    can never put two matches in one window — AppearanceDays is always 0 or 1 — so
            //    this row exercises the term a congested cup calendar (#43) will reach, today.
            Assert.AreEqual(1491, DailyOccurrencePer100k(peak, average, new MatchLoad(2, 0)),
                "a congested week prices at 1.49%/day — real congestion risk, not the clamp.");

            // 6. The hard ceiling still exists and still binds: a half-fatigued player in the same
            //    congested week assembles past InjuryRiskMax and caps at exactly 1.6%/day. What sits
            //    beyond the cap is the residual the research supplement's R-2 refit inherits (§10).
            Assert.AreEqual(1600, DailyOccurrencePer100k(halfSpent, average, new MatchLoad(2, 0)),
                "the InjuryRiskMax clamp is the hard 1.6%/day ceiling.");
        }

        /// <summary>
        /// The daily occurrence probability in per-hundred-thousand, driven end to end: #29 computes
        /// the risk scalar from the training state, #41 assembles it and compares against a draw
        /// uniform in <c>[0, OCCURRENCE_DRAW_DENOM)</c>, so the probability IS the assembled risk over
        /// that denominator.
        /// </summary>
        private static int DailyOccurrencePer100k(
            in TrainingState training, in PlayerAttributes attributes)
        {
            return DailyOccurrencePer100k(in training, in attributes, MatchLoad.None);
        }

        private static int DailyOccurrencePer100k(
            in TrainingState training, in PlayerAttributes attributes, in MatchLoad load)
        {
            int risk = MedicalStep.AssembleRiskScore(
                TrainingStep.ComputeInjuryRisk(training, attributes),
                in load,
                attributes,
                MedicalModifier.Identity);

            return (int)((long)risk * 100_000 / InjuriesMedicalConstants.OCCURRENCE_DRAW_DENOM);
        }

        [Test]
        public void HardContacts_AreWeightedZeroAtStage2()
        {
            PlayerAttributes a = WorkedExampleAttributes();

            // Operands deliberately BELOW the clamp (AR pass 1: at MatchLoad(2, …) both sides
            // saturated InjuryRiskMax and the test passed with HardContactWeight mutated to 100 —
            // a clamp-induced tautology). The precondition keeps a future retune from silently
            // re-clamping it.
            int withoutContacts = MedicalStep.AssembleRiskScore(
                new InjuryRiskContribution(3000), new MatchLoad(0, 0), a, MedicalModifier.Identity);
            Assert.Less(withoutContacts, InjuriesMedicalConstants.InjuryRiskMax,
                "precondition: the operand must sit below the clamp, or the equality is vacuous.");
            Assert.AreEqual(
                withoutContacts,
                MedicalStep.AssembleRiskScore(
                    new InjuryRiskContribution(3000), new MatchLoad(0, 40), a, MedicalModifier.Identity),
                "KD-3: the ledger-derived field is deep-tier only, so populating it early is harmless — " +
                "raising its weight is a config change, not a formula rewrite.");
        }

        [Test]
        public void RiskScore_ClampsAtBothEnds()
        {
            PlayerAttributes a = WorkedExampleAttributes();

            // Since ERR-041-011 the baseline term (before mitigation, position normative) keeps every
            // valid-input score ABOVE the floor at the default constants — which is the point: nobody
            // is injury-proof. So the floor itself is exercised through the clamp helper (a negative
            // sum is reachable only under a config that shrinks the baseline below the mitigation),
            // and the design fact is asserted on the most robust player representable.
            Assert.AreEqual(0, MedicalStep.ClampLong(-1, 0, InjuriesMedicalConstants.InjuryRiskMax),
                "the floor is 0 — a negative assembled sum must clamp, not go negative.");
            PlayerAttributes iron = PlayerAttributes.CreateDefault();
            iron.Strength = 20;
            iron.Stamina = 20;
            iron.Balance = 20;
            Assert.Greater(
                MedicalStep.AssembleRiskScore(InjuryRiskContribution.None, MatchLoad.None, iron, MedicalModifier.Identity),
                0,
                "even the most robust player keeps a positive baseline floor — nobody is injury-proof (ERR-041-011).");

            Assert.AreEqual(InjuriesMedicalConstants.InjuryRiskMax,
                MedicalStep.AssembleRiskScore(
                    new InjuryRiskContribution(int.MaxValue / 2), new MatchLoad(1000, 0), a, MedicalModifier.Identity),
                "the ceiling — and the widened intermediate is what stops this overflowing on the way there.");
        }

        [Test]
        public void AdvanceMedicalDay_NeverWritesAttributes()
        {
            var state = InjuryState.Create();
            PlayerAttributes a = WorkedExampleAttributes();
            PlayerAttributes copy = a;

            MedicalStep.AdvanceMedicalDay(
                ref state, PlayerId, a, MaxOccurrenceRisk(), MatchLoad.None, MedicalModifier.Identity,
                100, WorldSeed, occurrenceEnabled: true);

            // #41 reads #27 attributes and #29's scalar; it writes neither, and it has no path to any
            // fatigue accumulator at all (FR-MD-009 — structurally, not by convention).
            // As above in #29's suite: REDUNDANT with the compiler, since `attributes` is `in`. Kept as a
            // guard on the signature, not as evidence about the body.
            Assert.AreEqual(copy.ToArray(), a.ToArray());
        }

        [Test]
        public void ViewModel_IsAValueCopy_AndAgreesWithIsAvailable()
        {
            var state = InjuryState.Create();
            state.Severity = InjurySeverity.Moderate;
            state.RecoveryRemaining = 12;
            state.InjuryCount = 3;

            MedicalViewModel view = MedicalViewModel.Create(state);

            Assert.AreEqual(InjurySeverity.Moderate, view.Severity);
            Assert.AreEqual(12, view.RecoveryRemaining);
            Assert.AreEqual(3, view.InjuryCount);
            Assert.IsFalse(view.Available);
            Assert.AreEqual(MedicalStep.IsAvailable(state), view.Available);

            state.Severity = InjurySeverity.None;
            state.RecoveryRemaining = 0;
            Assert.AreEqual(InjurySeverity.Moderate, view.Severity,
                "the view is a copy — mutating the state behind it must not change what an observer holds.");
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-08-05 | —      | Initial implementation (#41 T0).                                   |
// | 1.1     | 2026-08-05 | —      | AR pass 1 (M): dropped the DrawOccurrence position-independence    |
// |         |            |        | test (a pure function of its arguments — it could not fail); the   |
// |         |            |        | id moves to the two-player test, which drives AdvanceMedicalDay.   |
// |         |            |        | + the #29 -> #41 seam test: the one cross-assembly contract in     |
// |         |            |        | this landing had no test at all.                                   |
// | 1.2     | 2026-08-05 | —      | AR pass 4 (L): T-MD-MOD-001's second assertion had the computed    |
// |         |            |        | value in NUnit's `expected` slot, so a failure would have reported |
// |         |            |        | the two sides the wrong way round.                                 |
// | 1.3     | 2026-08-05 | —      | AR pass 5 (M): + the daily-occurrence-probability characterization |
// |         |            |        | driven through the real #29 -> #41 chain. Every other occurrence   |
// |         |            |        | test here forces the outcome with a risk the producer cannot       |
// |         |            |        | reach, so nothing measured what the wired system would do: a fresh |
// |         |            |        | regen is 23% likely to be injured on day 0 and the DEFAULT focus   |
// |         |            |        | converges on exactly 0 forever. Recorded, not retuned (KD-W1).     |
// |         |            |        | (L): + the AssignRecoveryDays None-tier guard.                     |
// | 1.4     | 2026-08-07 | —      | Balance pass D3 (ERR-041-011): the characterization test moves to |
// |         |            |        | the AFTER numbers at per-100k resolution and gains the MatchLoad  |
// |         |            |        | rows (the old one measured the pass's largest constant not at     |
// |         |            |        | all); certainty is unreachable at the per-million denominator, so |
// |         |            |        | CertainOccurrenceRisk becomes MaxOccurrenceRisk + a deterministic |
// |         |            |        | HotDay/HotPlayerOnDayZero scan over the keyed derivation; the     |
// |         |            |        | worked example re-derives to 6600 (+ the clamp line); the         |
// |         |            |        | TrainingRiskFlows lock re-anchors to the InjuryRiskMax clamp      |
// |         |            |        | (its old denominator bound became an always-true).                |
// | 1.5     | 2026-08-07 | —      | Balance-pass AR pass 1 (2M + L): the HardContacts lock and the    |
// |         |            |        | RiskAssembly moreMatches operand were clamp-saturated tautologies |
// |         |            |        | after the retune — both moved below the clamp with explicit       |
// |         |            |        | preconditions; ImpossibleOccurrenceRisk renamed Minimum (the      |
// |         |            |        | baseline term made "impossible" a lie); characterization rows     |
// |         |            |        | re-fit to the 16000 ceiling (congestion priced 1.49%, cap 1.6%).  |
// | 1.6     | 2026-08-07 | —      | Balance-pass AR pass 2 (L, doc only): three "1%" ceiling residues |
// |         |            |        | (MaxOccurrenceRisk, HotDay, the characterization summary) updated |
// |         |            |        | to the 1.6% / ~60x figures the M3 headroom raise made current.    |
// | 1.7     | 2026-08-08 | —      | Balance-pass AR pass 5 (L9, comment): the MatchLoad(2,0)       |
// |         |            |        | congestion rows named as FORMULA PROBES — the Stage-0 cadence  |
// |         |            |        | (DaysBetweenRounds == window == 7) cannot produce two matches  |
// |         |            |        | in one window; that input arrives with #43's cup calendars.    |
// | 1.8     | 2026-08-08 | —      | Balance-pass AR pass 16 (L2): the assignment CEILING arm locked    |
// |         |            |        | for the first time — a mutant replacing RecoveryMax with          |
// |         |            |        | int.MaxValue had left the whole suite green.                      |
#endregion