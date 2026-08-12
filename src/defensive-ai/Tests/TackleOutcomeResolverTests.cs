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
        public void OutcomeSharesPartitionTheDrawRange()
        {
            var d = Distribution(Inputs());
            Assert.That(d.Missed + d.Won + d.Loose + d.Foul, Is.EqualTo(1.0).Within(1e-9),
                "the four outcomes must partition [0,1) exactly — a gap is a draw that resolves to nothing");
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
            // Football-judgment doctrine §6 P1, asserted rather than asserted-in-prose. Walking the
            // approach angle and the reach fraction in fine steps, the engage probability must never
            // jump: a threshold anywhere in this model would show up here as a step.
            float prevAngle = TackleOutcomeResolver.EngageProbability(Inputs(approachAngle: 0f));
            for (int i = 1; i <= 1000; i++)
            {
                float a = (float)(Math.PI * i / 1000.0);
                float p = TackleOutcomeResolver.EngageProbability(Inputs(approachAngle: a));
                Assert.That(Math.Abs(p - prevAngle), Is.LessThan(0.01f),
                    $"engage probability stepped at approachAngle={a}");
                prevAngle = p;
            }

            float prevReach = TackleOutcomeResolver.EngageProbability(Inputs(reachFraction: 0f));
            for (int i = 1; i <= 1000; i++)
            {
                float r = i / 1000f;
                float p = TackleOutcomeResolver.EngageProbability(Inputs(reachFraction: r));
                Assert.That(Math.Abs(p - prevReach), Is.LessThan(0.01f),
                    $"engage probability stepped at reachFraction={r}");
                prevReach = p;
            }
        }

        [Test]
        public void FoulShareStaysBelowOne_SoTheSecondTransformNeverDividesByZero()
        {
            // TACKLE_FOUL_SHARE_CEILING's whole reason to be [FIXED]. Driven to the extremes the [GT]s
            // allow, the share must still leave headroom.
            float extreme = TackleOutcomeResolver.FoulShare(Inputs(aggression: 1f, tackling: 0f));
            Assert.That(extreme, Is.LessThanOrEqualTo(DefensiveAIConstants.TACKLE_FOUL_SHARE_CEILING));
            Assert.That(DefensiveAIConstants.TACKLE_FOUL_SHARE_CEILING, Is.LessThan(1f));
        }

        [Test]
        public void ADrawOfExactlyOneStillResolves()
        {
            // A total mapping: u = 1.0 is outside the contract but arrives from a caller's rounding,
            // and an unhandled value here would be an unreachable-outcome bug rather than a throw.
            Assert.DoesNotThrow(() => TackleOutcomeResolver.Resolve(Inputs(), 1.0f));
            Assert.DoesNotThrow(() => TackleOutcomeResolver.Resolve(Inputs(), -0.5f));
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
