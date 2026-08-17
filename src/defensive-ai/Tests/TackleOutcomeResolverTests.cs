// File:     src/defensive-ai/Tests/TackleOutcomeResolverTests.cs
// Created:  2026-08-12
// Modified: 2026-08-12
// Author:   —
// Spec:     Defensive AI #14 §3.6.5, §5, Code Standards #20
// Purpose:  Locks for the §3.6.5 tackle outcome resolution — the partition, the monotonicities, the
//           continuity requirement, and the two numerical guarantees that keep the mapping total.

using System;

using NUnit.Framework;

namespace TacticalDirector.DefensiveAI.Tests
{
    /// <summary>
    /// §3.6.5 duel resolution. These are deliberately NOT "run it twice, same answer" determinism
    /// tests — that shape is self-referential and passes with every constant rewritten. Each case
    /// below pins a property the football depends on and fails if the corresponding term is deleted.
    /// </summary>
    [TestFixture]
    public class TackleOutcomeResolverTests
    {
        private static TackleDuelInputs Inputs(
            float tackling = 0.5f,
            float aggression = 0.5f,
            float dribbling = 0.5f,
            float balance = 0.5f,
            float approachAngle = 0f,
            float reachFraction = 0f)
        {
            return new TackleDuelInputs(tackling, aggression, dribbling, balance, approachAngle, reachFraction);
        }

        /// <summary>Sweeps the whole draw range and returns the share of each outcome, which is the
        /// only honest way to assert on a probabilistic resolver: a single draw tells you nothing about
        /// the model, only about that draw.</summary>
        private static (double Missed, double Won, double Loose, double Foul) Distribution(
            in TackleDuelInputs inputs, int samples = 20001)
        {
            int missed = 0, won = 0, loose = 0, foul = 0;
            for (int i = 0; i < samples; i++)
            {
                float u = i / (float)samples;
                switch (TackleOutcomeResolver.Resolve(in inputs, u))
                {
                    case TackleOutcome.Missed: missed++; break;
                    case TackleOutcome.BallWon: won++; break;
                    case TackleOutcome.BallLoose: loose++; break;
                    default: foul++; break;
                }
            }

            return (missed / (double)samples, won / (double)samples,
                    loose / (double)samples, foul / (double)samples);
        }

        [Test]
        public void AllFourOutcomesAreReachable()
        {
            // The BallLoose arm is the one at risk of being unreachable-by-construction: it is the
            // remainder of a remainder, so a sign error in either inverse transform deletes it
            // silently while the other three still look healthy.
            var d = Distribution(Inputs(tackling: 0.5f, dribbling: 0.5f, balance: 0.5f));

            Assert.That(d.Missed, Is.GreaterThan(0.0), "MISSED unreachable");
            Assert.That(d.Won, Is.GreaterThan(0.0), "BALL_WON unreachable");
            Assert.That(d.Loose, Is.GreaterThan(0.0), "BALL_LOOSE unreachable");
            Assert.That(d.Foul, Is.GreaterThan(0.0), "FOUL unreachable");
        }

        [Test]
        public void OutcomeSharesMatchTheClosedFormDistribution()
        {
            // AR-1 M-1: this used to assert that the four shares sum to 1.0, which is TRUE OF ANY
            // implementation — Distribution() bins every draw into exactly one bucket, so a Resolve
            // that returned Missed unconditionally passed it. The real property is that each share
            // equals the product the §3.6.5.3 pseudocode predicts, which fails on a sign error, a
            // swapped branch, or a transposed share.
            var inputs = new TackleDuelInputs(
                tacklerTackling: 0.70f, tacklerAggression: 0.60f,
                carrierDribbling: 0.80f, carrierBalance: 0.60f,
                approachAngle: 0.40f, reachFraction: 0.60f);

            double engage = TackleOutcomeResolver.EngageProbability(inputs);
            double foul = TackleOutcomeResolver.FoulShare(inputs);
            double clean = TackleOutcomeResolver.CleanShare(inputs);

            var d = Distribution(inputs);

            Assert.Multiple(() =>
            {
                Assert.That(d.Missed, Is.EqualTo(1.0 - engage).Within(0.002), "MISSED share");
                Assert.That(d.Foul, Is.EqualTo(engage * foul).Within(0.002), "FOUL share");
                Assert.That(d.Won, Is.EqualTo(engage * (1.0 - foul) * clean).Within(0.002), "BALL_WON share");
                Assert.That(d.Loose, Is.EqualTo(engage * (1.0 - foul) * (1.0 - clean)).Within(0.002),
                    "BALL_LOOSE share");
            });

            // The §3.6.5.7 figures, so the spec's own worked example cannot drift from the code.
            Assert.Multiple(() =>
            {
                Assert.That(d.Missed, Is.EqualTo(0.5897).Within(0.002));
                Assert.That(d.Foul, Is.EqualTo(0.0583).Within(0.002));
                Assert.That(d.Won, Is.EqualTo(0.0993).Within(0.002));
                Assert.That(d.Loose, Is.EqualTo(0.2527).Within(0.002));
            });
        }

        [Test]
        public void RunningAtTheManConnectsMoreOftenThanRunningAcrossHim()
        {
            // The ONLY thing §2.2.3's approach angle legitimately encodes. If TackleEngageCommitmentK
            // were deleted these two would be equal.
            float head_on = TackleOutcomeResolver.EngageProbability(Inputs(approachAngle: 0f));
            float across = TackleOutcomeResolver.EngageProbability(Inputs(approachAngle: (float)(Math.PI / 2)));

            Assert.That(head_on, Is.GreaterThan(across));
        }

        [Test]
        public void RunningAwayIsNoWorseThanRunningAcross_TheClampNotANegative()
        {
            // cos is negative beyond pi/2, so without the clamp a defender running away would get a
            // NEGATIVE commitment term and connect LESS often than one moving sideways — which is not a
            // football statement, it is an artefact of using cos as a weight.
            float across = TackleOutcomeResolver.EngageProbability(Inputs(approachAngle: (float)(Math.PI / 2)));
            float away = TackleOutcomeResolver.EngageProbability(Inputs(approachAngle: (float)Math.PI));

            Assert.That(away, Is.EqualTo(across).Within(1e-6f));
        }

        [Test]
        public void ClosingOnTheBallConnectsMoreOftenThanReachingAtFullStretch()
        {
            float onTop = TackleOutcomeResolver.EngageProbability(Inputs(reachFraction: 0f));
            float stretch = TackleOutcomeResolver.EngageProbability(Inputs(reachFraction: 1f));

            Assert.That(onTop, Is.GreaterThan(stretch));
        }

        [Test]
        public void BetterTacklerFoulsLess_AndMoreAggressiveTacklerFoulsMore()
        {
            float clumsy = TackleOutcomeResolver.FoulShare(Inputs(tackling: 0.1f));
            float clean = TackleOutcomeResolver.FoulShare(Inputs(tackling: 0.9f));
            Assert.That(clean, Is.LessThan(clumsy), "Tackling must reduce the foul share");

            float calm = TackleOutcomeResolver.FoulShare(Inputs(aggression: 0.1f));
            float wild = TackleOutcomeResolver.FoulShare(Inputs(aggression: 0.9f));
            Assert.That(wild, Is.GreaterThan(calm), "Aggression must raise the foul share");
        }

        [Test]
        public void TheEdgeOverTheCarrierDrivesCleanWinsRatherThanLooseBalls()
        {
            // The substantive football claim of §3.6.5: beating your man cleanly requires being better
            // than him, and merely matching him mostly knocks the ball loose.
            var outclassed = Inputs(tackling: 0.3f, dribbling: 0.9f, balance: 0.9f);
            var dominant = Inputs(tackling: 0.95f, dribbling: 0.2f, balance: 0.2f);

            Assert.That(TackleOutcomeResolver.CleanShare(outclassed),
                Is.LessThan(TackleOutcomeResolver.CleanShare(dominant)));

            // And the direction that matters for possession: against a good dribbler, a connecting
            // non-foul challenge more often knocks it loose than wins it.
            var d = Distribution(outclassed);
            Assert.That(d.Loose, Is.GreaterThan(d.Won),
                "against a superior dribbler the ball should more often be knocked free than taken cleanly");
        }

        [Test]
        public void CarrierBalanceHelpsHimRetainTheBall()
        {
            // Guards the TackleRetainBalanceWeight term specifically: with only the Dribbling term
            // live, these two would be equal.
            float unsteady = TackleOutcomeResolver.CleanShare(Inputs(balance: 0.0f));
            float rockSolid = TackleOutcomeResolver.CleanShare(Inputs(balance: 1.0f));

            Assert.That(rockSolid, Is.LessThan(unsteady));
        }

        [Test]
        public void NoInputProducesACliff()
        {
            // Football-judgment doctrine §6 P1, asserted rather than asserted-in-prose. Walking each
            // continuous input in fine steps, no probability may jump: a threshold anywhere in this
            // model shows up here as a step.
            //
            // EXTENDED at AR-1 M-8. This used to walk EngageProbability only — the one function with
            // no clamp that binds — so it could not have seen the plateau it now documents in
            // CleanShare. The step tolerance is also tightened from 0.01 (which was ~12x the true
            // per-step delta and would have missed a real sub-0.01 cliff) to a multiple of the largest
            // legitimate step each walk can take.
            AssertNoStep(a => TackleOutcomeResolver.EngageProbability(Inputs(approachAngle: a)),
                0f, (float)Math.PI, "engage vs approachAngle");
            AssertNoStep(r => TackleOutcomeResolver.EngageProbability(Inputs(reachFraction: r)),
                0f, 1f, "engage vs reachFraction");
            AssertNoStep(t => TackleOutcomeResolver.FoulShare(Inputs(tackling: t)),
                0f, 1f, "foulShare vs tackling");
            AssertNoStep(a => TackleOutcomeResolver.FoulShare(Inputs(aggression: a)),
                0f, 1f, "foulShare vs aggression");
            AssertNoStep(t => TackleOutcomeResolver.CleanShare(Inputs(tackling: t)),
                0f, 1f, "cleanShare vs tackling");
        }

        [Test]
        public void CleanShareHasAPlateauAgainstAnEliteCarrier_RECORDED_NOT_FIXED()
        {
            // AR-1 M-8, and it is a real football-judgment §6 P1/P2 violation, locked here rather than
            // quietly left: cleanShare = clamp01(Base + EdgeK·(tackling − retain)) is pinned at ZERO
            // whenever edge <= −Base/EdgeK. Against a maxed dribbler (retain = 1.0) that is every
            // tackler at or below tackling = 0.5 — raw attributes 1 THROUGH 10 are indistinguishable
            // and all have exactly no chance of a clean win.
            //
            // Not fixed here: the fix is a ramp with no plateau (the ERR-008-019 half-width precedent),
            // which changes a [GT]-governed shape and is therefore the calibration pass's business
            // under KD-W1, not a wiring pass's. Locked so the plateau cannot widen unnoticed and so
            // the pass that fixes it has a failing test to delete.
            var eliteCarrier = new Func<float, TackleDuelInputs>(t =>
                Inputs(tackling: t, dribbling: 1f, balance: 1f));

            Assert.That(TackleOutcomeResolver.CleanShare(eliteCarrier(0.05f)), Is.Zero,
                "raw 1 should be at the plateau floor");
            Assert.That(TackleOutcomeResolver.CleanShare(eliteCarrier(0.50f)), Is.Zero,
                "raw 10 is still at the plateau floor — the whole bottom half of the range is flat");
            Assert.That(TackleOutcomeResolver.CleanShare(eliteCarrier(0.55f)), Is.GreaterThan(0f),
                "above the plateau the term must move again, or the clamp is swallowing everything");
        }

        /// <summary>Walks a probability over [lo, hi] and fails on any step larger than a small
        /// multiple of the mean step — the shape a threshold makes.</summary>
        private static void AssertNoStep(Func<float, float> f, float lo, float hi, string what)
        {
            const int Steps = 2000;
            float prev = f(lo);
            float span = Math.Abs(f(hi) - f(lo));
            float tolerance = Math.Max(4f * span / Steps, 1e-5f);

            for (int i = 1; i <= Steps; i++)
            {
                float x = lo + (hi - lo) * i / Steps;
                float y = f(x);
                Assert.That(Math.Abs(y - prev), Is.LessThan(tolerance),
                    $"{what} stepped at {x} ({prev} -> {y}, tolerance {tolerance})");
                prev = y;
            }
        }

        [Test]
        public void TheTwoDefensiveClampsAreStructurallyUnreachable_AndThisLocksThatFact()
        {
            // AR-1 M-2/M-3, second attempt — the FIRST attempt was also tautological and a mutation
            // run proved it: deleting the TACKLE_FOUL_SHARE_CEILING clamp AND the uniform clamp left
            // every case in this file green.
            //
            // The reason is arithmetic, not a weak assertion. At the shipped [GT]s:
            //   max raw foulShare = Base + AggressionK              (Tackling only subtracts)
            //   max engage        = Base + CommitmentK + ProximityK
            // Both sit far below the values their clamps defend against, so no input can reach either
            // clamp and no test on this resolver's OUTPUTS can detect its removal. They are defensive
            // code with no reachable input — the RushCommitFatiguePenaltyM situation (W1 §7 item 6) —
            // and the honest response is the same: say so, and lock the CONDITION under which they
            // stop being unreachable.
            //
            // So this does not pretend to exercise the clamps. It asserts the headroom, and fails the
            // moment a retune makes either guard live — which is exactly when a real lock is needed.
            float maxRawFoulShare = DefensiveAIConstants.TackleFoulShareBase
                                    + DefensiveAIConstants.TackleFoulShareAggressionK;
            Assert.That(maxRawFoulShare, Is.LessThan(DefensiveAIConstants.TACKLE_FOUL_SHARE_CEILING),
                "the foul-share ceiling has become reachable — it now needs a lock that exercises it, "
                + "and Resolve's second inverse transform needs a tested divide-by-zero guard");

            float maxEngage = DefensiveAIConstants.TackleEngageBase
                              + DefensiveAIConstants.TackleEngageCommitmentK
                              + DefensiveAIConstants.TackleEngageProximityK;
            Assert.That(maxEngage, Is.LessThan(1f),
                "engage can now reach 1, so the uniform ceiling is live and needs its own lock");

            Assert.That(DefensiveAIConstants.TACKLE_FOUL_SHARE_CEILING, Is.GreaterThan(0f).And.LessThan(1f));
        }

        [Test]
        public void ResolveIsTotalOverADenseSweepOfEveryInput()
        {
            // What IS worth locking about the mapping: every combination the model can present — plus
            // the out-of-contract draws a caller's rounding hands it — produces one of the four
            // declared outcomes. Fails on a NaN, an unhandled branch, or a fifth value leaking out.
            float[] draws = { -0.5f, 0f, 0.0001f, 0.5f, 0.9999f, 1f, 42f };

            for (int a = 0; a <= 10; a++)
            {
                for (int t = 0; t <= 10; t++)
                {
                    var probe = Inputs(
                        tackling: t / 10f, aggression: a / 10f,
                        dribbling: (10 - t) / 10f, balance: a / 10f,
                        approachAngle: (float)(Math.PI * a / 10.0), reachFraction: t / 10f);

                    foreach (float u in draws)
                    {
                        TackleOutcome o = TackleOutcomeResolver.Resolve(in probe, u);
                        Assert.That(
                            o == TackleOutcome.Missed || o == TackleOutcome.BallWon
                            || o == TackleOutcome.BallLoose || o == TackleOutcome.Foul,
                            Is.True, $"undeclared outcome {o} at u={u}");
                    }
                }
            }
        }

        [Test]
        public void WorkedExampleMatchesSection365()
        {
            // §3.6.5.7's numbers, so the spec's example cannot drift from the code without a failure.
            var inputs = new TackleDuelInputs(
                tacklerTackling: 0.70f, tacklerAggression: 0.60f,
                carrierDribbling: 0.80f, carrierBalance: 0.60f,
                approachAngle: 0.40f, reachFraction: 0.60f);

            Assert.That(TackleOutcomeResolver.EngageProbability(inputs), Is.EqualTo(0.41027f).Within(0.0001f));
            Assert.That(TackleOutcomeResolver.FoulShare(inputs), Is.EqualTo(0.142f).Within(0.0001f));
            Assert.That(TackleOutcomeResolver.CleanShare(inputs), Is.EqualTo(0.282f).Within(0.0001f));
            Assert.That(TackleOutcomeResolver.Resolve(inputs, 0.30f), Is.EqualTo(TackleOutcome.BallLoose));

            // §3.6.5.7's SECOND tackler, which nothing locked until AR-1 M-10 — and the spec's
            // arithmetic for it was wrong, because raising Tackling lowers the foul share too and the
            // example recomputed only the clean share. Locked now so that cannot recur silently.
            var better = new TackleDuelInputs(
                tacklerTackling: 0.85f, tacklerAggression: 0.60f,
                carrierDribbling: 0.80f, carrierBalance: 0.60f,
                approachAngle: 0.40f, reachFraction: 0.60f);

            Assert.That(TackleOutcomeResolver.FoulShare(better), Is.EqualTo(0.127f).Within(0.0001f));
            Assert.That(TackleOutcomeResolver.CleanShare(better), Is.EqualTo(0.372f).Within(0.0001f));
            Assert.That(TackleOutcomeResolver.Resolve(better, 0.30f), Is.EqualTo(TackleOutcome.BallLoose),
                "three attribute points better still knocks the ball free on this draw");
            Assert.That(TackleOutcomeResolver.Resolve(better, 0.18f), Is.EqualTo(TackleOutcome.BallWon),
                "below the 0.1853 threshold the same challenge is won cleanly");
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                            |
// | 1.0     | 2026-08-12 | —      | Initial. §3.6.5 resolver locks (wiring backlog W2): the four-way  |
// |         |            |        | partition, each monotonicity as its own isolating case, the P1   |
// |         |            |        | continuity requirement asserted rather than claimed, the [FIXED] |
// |         |            |        | ceiling's numerical guarantee, and the §3.6.5.7 worked example.  |
#endregion
