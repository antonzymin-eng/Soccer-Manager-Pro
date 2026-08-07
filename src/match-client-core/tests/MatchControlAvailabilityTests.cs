// File:     src/match-client-core/tests/MatchControlAvailabilityTests.cs
// Created:  2026-08-07
// Modified: 2026-08-07
// Author:   —
// Spec:     Interactive Unity client (docs/tracking/interactive-unity-client-design.md §5-P5, §6.2,
//           §6.3), Testing Strategy #19, Code Standards #20
// Purpose:  Locks the three control-enablement states, the resolution from a live frame, and the two
//           decisions most likely to be "tidied" away later — that a frameless streamer is not read
//           as a running match, and that saving survives full time.

using NUnit.Framework;

using UnityEngine;

using TacticalDirector.MatchClientCore;
using TacticalDirector.MatchViewer;

namespace TacticalDirector.MatchClientCore.Tests
{
    [TestFixture]
    public sealed class MatchControlAvailabilityTests
    {
        private static LiveMatchFrame Frame(bool matchEnded) =>
            new LiveMatchFrame(
                tick:              1UL,
                ballPosition:      new Vector3(52.5f, 34f, 0.11f),
                possessingAgentId: -1,
                agentPositions:    new Vector2[0],
                agentCues:         new LiveAgentCue[0],
                substitutionsUsed: new int[0],
                score:             default,
                matchEnded:        matchEnded,
                period:            default,
                restart:           default);

        [Test]
        public void ARunningMatchOffersEverything()
        {
            MatchControlAvailability live = MatchControlAvailability.Live;

            Assert.IsTrue(live.TacticalInputEnabled);
            Assert.IsTrue(live.SubstitutionEnabled);
            Assert.IsTrue(live.PlaybackControlsEnabled);
            Assert.IsTrue(live.SaveEnabled);
            Assert.AreEqual(MatchControlLockReason.None, live.LockReason);
        }

        [Test]
        public void FullTimeLocksEveryMatchFacingControl()
        {
            MatchControlAvailability ended = MatchControlAvailability.FullTime;

            Assert.IsFalse(ended.TacticalInputEnabled);
            Assert.IsFalse(ended.SubstitutionEnabled);
            Assert.IsFalse(ended.PlaybackControlsEnabled);
            Assert.AreEqual(MatchControlLockReason.MatchEnded, ended.LockReason);
        }

        [Test]
        public void BeforeTheFirstFrameEverythingMatchFacingIsLocked_WithItsOwnReason()
        {
            MatchControlAvailability waiting = MatchControlAvailability.AwaitingFirstFrame;

            Assert.IsFalse(waiting.TacticalInputEnabled);
            Assert.IsFalse(waiting.SubstitutionEnabled);
            Assert.IsFalse(waiting.PlaybackControlsEnabled);

            // Distinct from MatchEnded on purpose: this state is transient, and a UI may want to
            // show "starting" rather than "finished" for it.
            Assert.AreEqual(MatchControlLockReason.AwaitingFirstFrame, waiting.LockReason);
        }

        [Test]
        public void SavingIsOfferedInEveryState_IncludingFullTime()
        {
            // §6.3: a finished match is exactly when a viewer wants to save, and ServiceOnce() exists
            // so the capture needs no tick. Locking save alongside the other full-time controls is
            // the obvious "tidy-up" and it would make a completed match unsaveable.
            Assert.IsTrue(MatchControlAvailability.AwaitingFirstFrame.SaveEnabled);
            Assert.IsTrue(MatchControlAvailability.Live.SaveEnabled);
            Assert.IsTrue(MatchControlAvailability.FullTime.SaveEnabled);
        }

        [Test]
        public void AFramelessStreamerIsNotReadAsARunningMatch()
        {
            // The defect this guards: TryGetLatestFrame's out-parameter on a false return is
            // default(LiveMatchFrame), whose MatchEnded is false. A From() that read the frame
            // unconditionally would resolve a match that has not started to Live — every control
            // live, against no match state at all.
            MatchControlAvailability resolved =
                MatchControlAvailability.From(hasFrame: false, frame: default);

            Assert.AreEqual(MatchControlLockReason.AwaitingFirstFrame, resolved.LockReason);
            Assert.IsFalse(resolved.TacticalInputEnabled);
        }

        [Test]
        public void AFrameResolvesToLiveOrFullTimeByItsMatchEndedFlag()
        {
            MatchControlAvailability running =
                MatchControlAvailability.From(hasFrame: true, frame: Frame(matchEnded: false));
            Assert.AreEqual(MatchControlLockReason.None, running.LockReason);
            Assert.IsTrue(running.TacticalInputEnabled);

            MatchControlAvailability ended =
                MatchControlAvailability.From(hasFrame: true, frame: Frame(matchEnded: true));
            Assert.AreEqual(MatchControlLockReason.MatchEnded, ended.LockReason);
            Assert.IsFalse(ended.TacticalInputEnabled);
        }

        [Test]
        public void TheDefaultLockReasonReadsAsUnlocked()
        {
            // Mirrors ManagerCommandKind.None / IntentKind.None: a default must not alias a real
            // reason. Here it must specifically not read as MatchEnded.
            Assert.AreEqual(MatchControlLockReason.None, default(MatchControlLockReason));
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-08-07 | —      | Initial creation (§5-P5 host-free half): the three states, the  |
// |         |            |        | frame resolution, the frameless-is-not-live guard, and the      |
// |         |            |        | §6.3 save-survives-full-time lock.                              |
#endregion
