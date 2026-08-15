// File:     src/match-client-core/tests/LiveFrameLatchTests.cs
// Created:  2026-08-15
// Modified: 2026-08-15
// Author:   —
// Spec:     Interactive Unity client (docs/tracking/interactive-unity-client-design.md §5-P4b,
//           §12 rule 1), Testing Strategy #19, Code Standards #20
// Purpose:  Locks the previous/current frame latch AR pass M-6 extracted out of
//           MatchClientBehaviour.AdvanceFrame: the first-frame seed, a new-tick shift (including a
//           multi-tick jump), and the same-tick no-op.

using NUnit.Framework;

using UnityEngine;

using TacticalDirector.MatchEngine;
using TacticalDirector.MatchViewer;

namespace TacticalDirector.MatchClientCore.Tests
{
    [TestFixture]
    public sealed class LiveFrameLatchTests
    {
        private const int Roster = MatchEngineConstants.SQUAD_SIZE;

        private static LiveMatchFrame Frame(ulong tick) =>
            new LiveMatchFrame(
                tick, Vector3.zero, MatchEngineConstants.NO_POSSESSION,
                new Vector2[Roster], new LiveAgentCue[Roster], new int[MatchEngineConstants.TEAM_COUNT],
                new Scoreline(0, 0), false, MatchPeriod.FirstHalf, RestartBanner.None);

        [Test]
        public void BeforeAnyFrame_HasFrameIsFalse()
        {
            var latch = new LiveFrameLatch();
            Assert.IsFalse(latch.HasFrame);
        }

        [Test]
        public void FirstFrame_SeedsPreviousAndCurrentToItself()
        {
            var latch = new LiveFrameLatch();
            LiveMatchFrame first = Frame(10UL);

            bool changed = latch.TryAccept(in first, 1.0f);

            Assert.IsTrue(changed, "the first frame ever must count as a change");
            Assert.IsTrue(latch.HasFrame);
            Assert.AreEqual(10UL, latch.Previous.Tick);
            Assert.AreEqual(10UL, latch.Current.Tick);
            Assert.AreEqual(0f, latch.SecondsSinceCurrent(1.0f), 1e-6f);
        }

        [Test]
        public void NewTick_ShiftsCurrentIntoPrevious()
        {
            var latch = new LiveFrameLatch();
            latch.TryAccept(Frame(10UL), 1.0f);

            LiveMatchFrame second = Frame(11UL);
            bool changed = latch.TryAccept(in second, 1.5f);

            Assert.IsTrue(changed);
            Assert.AreEqual(10UL, latch.Previous.Tick, "the old CURRENT becomes the new previous");
            Assert.AreEqual(11UL, latch.Current.Tick);
            Assert.AreEqual(0f, latch.SecondsSinceCurrent(1.5f), 1e-6f, "arrival clock resets on the new tick");
            Assert.AreEqual(0.3f, latch.SecondsSinceCurrent(1.8f), 1e-6f);
        }

        [Test]
        public void MultiTickJump_StillShiftsCurrentIntoPrevious()
        {
            // Nothing in TryAccept assumes ticks arrive one at a time — a paused-then-resumed or a
            // slow-render-frame stream can hand it a frame many ticks ahead of the latched one.
            var latch = new LiveFrameLatch();
            latch.TryAccept(Frame(100UL), 1.0f);

            LiveMatchFrame jumped = Frame(140UL);
            bool changed = latch.TryAccept(in jumped, 2.0f);

            Assert.IsTrue(changed);
            Assert.AreEqual(100UL, latch.Previous.Tick);
            Assert.AreEqual(140UL, latch.Current.Tick);
        }

        [Test]
        public void SameTickAgain_IsANoOp()
        {
            var latch = new LiveFrameLatch();
            latch.TryAccept(Frame(10UL), 1.0f);
            latch.TryAccept(Frame(11UL), 1.5f);

            LiveMatchFrame repeat = Frame(11UL);
            bool changed = latch.TryAccept(in repeat, 1.9f);

            Assert.IsFalse(changed, "the same tick republished must not count as a change");
            Assert.AreEqual(10UL, latch.Previous.Tick, "unchanged");
            Assert.AreEqual(11UL, latch.Current.Tick, "unchanged");
            Assert.AreEqual(0.4f, latch.SecondsSinceCurrent(1.9f), 1e-6f,
                "the arrival clock must NOT reset — a same-tick republish is not a new interval");
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-08-15 | —      | Initial creation (AR pass M-6): first-frame seed, new-tick      |
// |         |            |        | shift, multi-tick jump, and same-tick no-op — the four cases    |
// |         |            |        | LiveFrameLatch.TryAccept resolves.                               |
#endregion
