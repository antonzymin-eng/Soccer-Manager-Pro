// File:     src/match-client-core/tests/PlaybackSpeedLadderTests.cs
// Created:  2026-08-07
// Modified: 2026-08-07
// Author:   —
// Spec:     Interactive Unity client (docs/tracking/interactive-unity-client-design.md §5-P5, §5-P0),
//           Testing Strategy #19, Code Standards #20
// Purpose:  Locks the speed ladder's order, its end-clamping, and the invariant that every rung is a
//           multiplier the streamer will actually accept.

using System;

using NUnit.Framework;

using TacticalDirector.MatchClientCore;
using TacticalDirector.MatchViewer;

namespace TacticalDirector.MatchClientCore.Tests
{
    [TestFixture]
    public sealed class PlaybackSpeedLadderTests
    {
        [Test]
        public void TheLadderCarriesTheMasterPlansFourMultipliers()
        {
            Assert.AreEqual(4, PlaybackSpeedLadder.RungCount);

            // Pause is deliberately absent — it is a streamer state, not a multiplier.
            float[] rungs = PlaybackSpeedLadder.SnapshotRungs();
            CollectionAssert.DoesNotContain(rungs, 0f);
        }

        [Test]
        public void RungsAscend_SoFasterAlwaysMeansAHigherIndex()
        {
            // The load-bearing ordering property: StepFaster returns index+1, which is only "faster"
            // if the array is sorted ascending. A retune that put 10x first would leave every
            // stepping method quietly inverted, and nothing else here would notice.
            float[] rungs = PlaybackSpeedLadder.SnapshotRungs();

            for (int i = 1; i < rungs.Length; i++)
            {
                Assert.Less(rungs[i - 1], rungs[i],
                    "Rung " + (i - 1) + " must be slower than rung " + i + ".");
            }
        }

        [Test]
        public void AMatchOpensAtRealTime()
        {
            Assert.AreEqual(PlaybackSpeedLadder.SlowestIndex, PlaybackSpeedLadder.RealTimeIndex);
            Assert.AreEqual(MatchClientConstants.Speed1x,
                PlaybackSpeedLadder.MultiplierAt(PlaybackSpeedLadder.RealTimeIndex));
        }

        [Test]
        public void EveryRungIsAMultiplierTheStreamerWillAccept()
        {
            // The §5-P0 note said the cap must be >= 10 so 10x is not refused. This asserts it
            // rather than restating it: if a [GT] retune or a lowered cap breaks the pairing, this
            // fails here instead of the fastest button throwing mid-match and the rest working.
            foreach (float multiplier in PlaybackSpeedLadder.SnapshotRungs())
            {
                Assert.GreaterOrEqual(multiplier, MatchViewerConstants.MinLiveSpeedMultiplier);
                Assert.LessOrEqual(multiplier, MatchViewerConstants.MaxLiveSpeedMultiplier);
            }
        }

        [Test]
        public void StepFasterWalksUpAndThenHoldsAtTheTop()
        {
            int index = PlaybackSpeedLadder.SlowestIndex;

            for (int step = 0; step < PlaybackSpeedLadder.RungCount - 1; step++)
            {
                int next = PlaybackSpeedLadder.StepFaster(index);
                Assert.AreEqual(index + 1, next);
                index = next;
            }

            Assert.AreEqual(PlaybackSpeedLadder.FastestIndex, index);

            // Clamps rather than wrapping: a "faster" click at 10x must not drop the viewer to 1x.
            Assert.AreEqual(PlaybackSpeedLadder.FastestIndex,
                PlaybackSpeedLadder.StepFaster(PlaybackSpeedLadder.FastestIndex));
        }

        [Test]
        public void StepSlowerWalksDownAndThenHoldsAtTheBottom()
        {
            int index = PlaybackSpeedLadder.FastestIndex;

            for (int step = 0; step < PlaybackSpeedLadder.RungCount - 1; step++)
            {
                int next = PlaybackSpeedLadder.StepSlower(index);
                Assert.AreEqual(index - 1, next);
                index = next;
            }

            Assert.AreEqual(PlaybackSpeedLadder.SlowestIndex, index);
            Assert.AreEqual(PlaybackSpeedLadder.SlowestIndex,
                PlaybackSpeedLadder.StepSlower(PlaybackSpeedLadder.SlowestIndex));
        }

        [Test]
        public void EveryRungRoundTripsThroughTryIndexOf()
        {
            for (int i = 0; i < PlaybackSpeedLadder.RungCount; i++)
            {
                float multiplier = PlaybackSpeedLadder.MultiplierAt(i);

                Assert.IsTrue(PlaybackSpeedLadder.TryIndexOf(multiplier, out int found));
                Assert.AreEqual(i, found);
            }
        }

        [Test]
        public void TryIndexOfRefusesAMultiplierTheLadderNeverHandedOut()
        {
            Assert.IsFalse(PlaybackSpeedLadder.TryIndexOf(2f, out int index));
            Assert.AreEqual(-1, index, "The failed out-value must not alias rung 0.");
        }

        [Test]
        public void AnIndexOffEitherEndIsRefusedRatherThanClamped()
        {
            foreach (int bad in new[] { -1, PlaybackSpeedLadder.RungCount })
            {
                Assert.IsFalse(PlaybackSpeedLadder.IsRung(bad));
                Assert.Throws<ArgumentOutOfRangeException>(
                    () => PlaybackSpeedLadder.MultiplierAt(bad));
                Assert.Throws<ArgumentOutOfRangeException>(
                    () => PlaybackSpeedLadder.StepFaster(bad));
                Assert.Throws<ArgumentOutOfRangeException>(
                    () => PlaybackSpeedLadder.StepSlower(bad));
            }
        }

        [Test]
        public void SnapshotHandsOutACopy_NotTheLadderItself()
        {
            float[] first = PlaybackSpeedLadder.SnapshotRungs();
            first[0] = 999f;

            Assert.AreEqual(MatchClientConstants.Speed1x, PlaybackSpeedLadder.SnapshotRungs()[0]);
            Assert.AreEqual(MatchClientConstants.Speed1x,
                PlaybackSpeedLadder.MultiplierAt(PlaybackSpeedLadder.SlowestIndex));
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-08-07 | —      | Initial creation (§5-P5 host-free half): order, end-clamping,   |
// |         |            |        | round-trip, refusal and copy-not-view locks, plus the pairing   |
// |         |            |        | assertion that every rung is inside the streamer's cap.         |
#endregion
