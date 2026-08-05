// File:     src/injuries-medical/tests/MedicalStepTests.cs
// Created:  2026-08-05
// Modified: 2026-08-05
// Author:   —
// Spec:     Injuries & Medical #41 §3.1–§3.4 + Appendices A/B/C; Code Standards #20
// Purpose:  T-MD-DET-001/003/005/006/007/009, T-MD-ORD-001, T-MD-SEV-001/002, T-MD-REC-001,
//           T-MD-MOD-001/002, T-MD-NEU-001/002, T-MD-AVAIL-001, T-MD-FAT-001, T-MD-FAIL-004/006 — the
//           §3.6 worked example, the KD-6 same-call gate, the keyed-draw properties, the #29 -> #41
//           seam, and the fail-loud gates.

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

        /// <summary>A risk input high enough that the clamp pins it to the draw denominator, so <c>draw &lt; risk</c> holds for every possible draw — an occurrence is certain.</summary>
        private static InjuryRiskContribution CertainOccurrenceRisk() =>
            new InjuryRiskContribution(InjuriesMedicalConstants.InjuryRiskMax * 4);

        /// <summary>A risk input that clamps to zero, so no draw can ever be below it — an occurrence is impossible.</summary>
        private static InjuryRiskContribution ImpossibleOccurrenceRisk() => InjuryRiskContribution.None;

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
        public void WorkedExample_RiskAssembly_Is2900()
        {
            int risk = MedicalStep.AssembleRiskScore(
                new InjuryRiskContribution(3000),
                new MatchLoad(appearanceDays: 2, hardContacts: 0),
                WorkedExampleAttributes(),
                MedicalModifier.Identity);

            // 1×3000 + 150×2 − 400 = 2900, unchanged by the ×1000/1000 identity multiplier.
            Assert.AreEqual(2900, risk);
        }

        [Test]
        public void WorkedExample_Draw1500AgainstRisk2900_IsMinor_TTMDSEV001()
        {
            // 1_500_000 < 2900 × 600 = 1_740_000 ⇒ Minor. The integer cross-multiply, no division.
            Assert.AreEqual(InjurySeverity.Minor, MedicalStep.ClassifySeverityFromDraw(draw: 1500, risk: 2900));
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
                Advance(ref state, day, ImpossibleOccurrenceRisk());
                Assert.AreEqual(InjurySeverity.Minor, state.Severity, "still injured on day " + day);
            }

            Assert.AreEqual(1, state.RecoveryRemaining);

            Advance(ref state, 212, ImpossibleOccurrenceRisk());
            Assert.AreEqual(InjurySeverity.None, state.Severity);
            Assert.AreEqual(0, state.RecoveryRemaining);
        }

        // ── KD-6 — the same-call ordering guarantee ─────────────────────────────────

        [Test]
        public void RecoveringToZero_CannotAlsoReinjure_InOneCall_TTMDORD001()
        {
            var state = InjuryState.Create();
            state.Severity = InjurySeverity.Minor;
            state.RecoveryRemaining = 1;
            state.LastAdvancedWorldDay = 211;
            state.InjuryCount = 1;

            // Occurrence is CERTAIN under this risk — so if the gate read the post-countdown state
            // instead of the entry state, this call would heal and immediately re-injure, and the
            // player's injury would appear never to end.
            Advance(ref state, 212, CertainOccurrenceRisk());

            Assert.AreEqual(InjurySeverity.None, state.Severity, "recovered this call...");
            Assert.AreEqual(1, state.InjuryCount, "...and NOT re-injured by the same call (KD-6 / FR-MD-004).");

            // The next call finds the player healthy at entry, so the certain occurrence now fires.
            Advance(ref state, 213, CertainOccurrenceRisk());

            Assert.AreNotEqual(InjurySeverity.None, state.Severity);
            Assert.AreEqual(2, state.InjuryCount);
            Assert.Greater(state.RecoveryRemaining, 0, "F1: an injured player always has recovery outstanding.");
        }

        // ── Idempotency, day-0, day gap ─────────────────────────────────────────────

        [Test]
        public void AlreadyAdvancedDay_IsANoOp_TTMDDET005()
        {
            var state = InjuryState.Create();
            Advance(ref state, 100, CertainOccurrenceRisk());
            InjuryState afterFirst = state;

            Advance(ref state, 100, CertainOccurrenceRisk());
            Advance(ref state, 99, CertainOccurrenceRisk());

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

            Advance(ref state, 0, CertainOccurrenceRisk());

            // With a 0 sentinel this call would have read as "already advanced" and the player would
            // never have been evaluated at all.
            Assert.AreEqual(0u, state.LastAdvancedWorldDay);
            Assert.AreEqual(1, state.InjuryCount, "world day 0 is a legitimate first evaluation day.");
        }

        [Test]
        public void DayGap_FailsLoud_TTMDDET007()
        {
            var state = InjuryState.Create();
            Advance(ref state, 100, ImpossibleOccurrenceRisk());

            PlayerAttributes a = WorkedExampleAttributes();
            InjuryRiskContribution risk = ImpossibleOccurrenceRisk();

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
            InjuryRiskContribution risk = ImpossibleOccurrenceRisk();

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
                Assert.Less(draw, InjuriesMedicalConstants.OccurrenceDrawDenom);
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

            for (uint day = 0; day < 400; day++)
            {
                Advance(ref state, day, CertainOccurrenceRisk(), occurrenceEnabled: false);
            }

            Assert.AreEqual(InjurySeverity.None, state.Severity);
            Assert.AreEqual(0, state.InjuryCount,
                "with the dial off, severity can only ever fall toward None — no draw is issued at all.");
            Assert.AreEqual(399u, state.LastAdvancedWorldDay, "the cursor still advances; only the draw is skipped.");
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
                MedicalStep.AssembleRiskScore(
                    new InjuryRiskContribution(3000), MatchLoad.None, WorkedExampleAttributes(),
                    MedicalModifier.Identity),
                3000 - 400);
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
        }

        [Test]
        public void ZeroRecoverySpeed_FailsLoud_TTMDFAIL006()
        {
            var state = InjuryState.Create();
            PlayerAttributes a = WorkedExampleAttributes();
            InjuryRiskContribution risk = CertainOccurrenceRisk();

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
            InjuryRiskContribution risk = ImpossibleOccurrenceRisk();

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
            InjuryRiskContribution risk = ImpossibleOccurrenceRisk();

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
                new InjuryRiskContribution(3000), new MatchLoad(3, 0), ordinary, MedicalModifier.Identity);

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
            Assert.LessOrEqual(wornRisk, InjuriesMedicalConstants.OccurrenceDrawDenom);

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
            // spec mandates its own term, so this is spec-faithful — but it means a player's
            // robustness is priced in twice, the two [GT] tables cannot be tuned independently, and
            // #29's saturated 'maximum risk' NEVER means certain occurrence at #41 (the [1,20]
            // attribute floor guarantees #41 always subtracts at least its mean-1 row). Recorded here
            // rather than left to be rediscovered during the balance pass.
            Assert.Greater(maxRisk, 0, "the worst realistic player must still carry real risk.");
            Assert.Less(maxRisk, InjuriesMedicalConstants.OccurrenceDrawDenom,
                "double mitigation: #41 re-subtracts on attributes #29 already priced in, so the " +
                "producer's ceiling cannot reach the consumer's.");
        }

        [Test]
        public void HardContacts_AreWeightedZeroAtStage2()
        {
            PlayerAttributes a = WorkedExampleAttributes();

            Assert.AreEqual(
                MedicalStep.AssembleRiskScore(new InjuryRiskContribution(3000), new MatchLoad(2, 0), a, MedicalModifier.Identity),
                MedicalStep.AssembleRiskScore(new InjuryRiskContribution(3000), new MatchLoad(2, 40), a, MedicalModifier.Identity),
                "KD-3: the ledger-derived field is deep-tier only, so populating it early is harmless — " +
                "raising its weight is a config change, not a formula rewrite.");
        }

        [Test]
        public void RiskScore_ClampsAtBothEnds()
        {
            PlayerAttributes a = WorkedExampleAttributes();

            Assert.AreEqual(0,
                MedicalStep.AssembleRiskScore(InjuryRiskContribution.None, MatchLoad.None, a, MedicalModifier.Identity),
                "the mitigation exceeds the raw risk here; the result floors at 0 rather than going negative.");

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
                ref state, PlayerId, a, CertainOccurrenceRisk(), MatchLoad.None, MedicalModifier.Identity,
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
// | Version | Date       | Author | Notes                            |
// | 1.0     | 2026-08-05 | —      | Initial implementation (#41 T0).                                   |
// | 1.1     | 2026-08-05 | —      | AR pass 1 (M): dropped the DrawOccurrence position-independence     |
// |         |            |        | test (a pure function of its arguments — it could not fail); the    |
// |         |            |        | id moves to the two-player test, which drives AdvanceMedicalDay.    |
// |         |            |        | + the #29 -> #41 seam test: the one cross-assembly contract in this |
// |         |            |        | landing had no test at all.                                         |
#endregion
