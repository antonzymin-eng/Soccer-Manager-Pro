// File:     src/goalkeeper-mechanics/Tests/GoalkeeperConversionTests.cs
// Created:  2026-07-28
// Modified: 2026-07-28
// Author:   —
// Spec:     Goalkeeper Mechanics #11 §3.2 / §3.5 / §4.6, Code Standards #20
// Purpose:  Unit locks for the gk-catch-parry-conversion pass (ERR-011-005 / ERR-011-006):
//           the §3.2.3 reaction window frozen at the dive-launch frame, and the detection-stamp
//           lifecycle (episode-onset fallback via OnThreatArmed, shot-stamp precedence,
//           clear-on-disarm / clear-on-resolution, mid-dive preservation). Driven through the
//           REAL 10 Hz + 60 Hz orchestrator with the GoalkeeperScenarios harness pattern.

using NUnit.Framework;

using UnityEngine;

using TacticalDirector.AgentMovement;
using TacticalDirector.BallPhysics;
using TacticalDirector.GoalkeeperMechanics;

namespace TacticalDirector.GoalkeeperMechanics.Tests
{
    [TestFixture]
    public class GoalkeeperConversionTests
    {
        private const int Gk0 = 0;
        private const int Gk1 = 1;
        private const int AgentCount = 2;

        private static readonly int[] GkAgentIds = { Gk0, Gk1 };

        // ── test doubles (the GoalkeeperScenarios pattern — observation through the
        //    constructor-injected dependencies) ─────────────────────────────────────────

        private sealed class SilentBallSystem : IGoalkeeperBallSystem
        {
            public void ApplyKick(Vector3 velocity, Vector3 spin, int agentId, float matchTimeMs) { }
            public void SetPossessor(int agentId) { }
            public int GetBallPossessorId() => -1;
        }

        private sealed class ZeroRng : IGoalkeeperRngService
        {
            public float NextFloat(int drawSiteId, uint domainTag) => 0.5f;
            public float NextGaussian(int drawSiteId, uint domainTag) => 0.0f;
        }

        private static GoalkeeperAgentAttributes MidAttrs()
        {
            return new GoalkeeperAgentAttributes
            {
                Reflexes = 10f,
                Handling = 10f,
                Composure = 10f,
                Strength = 10f,
                Aerial = 10f,
                Balance = 10f,
                OneVsOne = 10f,
                Pace = 10f,
                Throwing = 10f,
                Kicking = 10f,
                Fatigue = 0.0f,
                TeamId = 0
            };
        }

        private static GoalkeeperMechanics NewGk() =>
            new GoalkeeperMechanics(new SilentBallSystem(), new ZeroRng());

        // GK0 near its own goal; ball inside GK0's defensive third (threatening ⇒ the tactical
        // machine walks Resting → Set → Anticipate) but far outside every reach envelope, so a
        // launched dive always misses and the run stays publish-free (no EventBus boot needed).
        private static AgentState[] BuildAgents()
        {
            var agents = new AgentState[AgentCount];
            agents[Gk0] = AgentState.CreateAtPosition(new Vector2(2f, 34f), Vector2.right);
            agents[Gk1] = AgentState.CreateAtPosition(new Vector2(103f, 34f), Vector2.left);
            return agents;
        }

        private static BallState FarThreateningBall() =>
            BallState.CreateAtPosition(new Vector3(30f, 20f, 0.11f));

        private static SaveIntent Intent() => new SaveIntent
        {
            TargetHand = HandEnum.Right,
            ClutchFirmness = 0.6f,
            DeflectionTarget = null,
            AttemptCommittedTick = 0
        };

        /// <summary>
        /// Drives the orchestrator tick-by-tick with a continuous 60 Hz frame/time base
        /// (time = frame × FrameMs), arming the threat at <paramref name="armAtTick"/> and
        /// committing the save one tactical tick after Anticipate is reachable. Returns the
        /// dive-launch frame read back from the orchestrator's own snapshot seam (−1 if no
        /// dive launched within <paramref name="tacticalTicks"/>).
        /// </summary>
        private static int RunUntilDive(
            GoalkeeperMechanics gk, int armAtTick, int commitAtTick, int tacticalTicks,
            out float armTimeMs)
        {
            AgentState[] agents = BuildAgents();
            BallState ball = FarThreateningBall();
            float frameMs = GoalkeeperConstants.FrameMs;
            int framesPerTick = GoalkeeperConstants.FramesPerTacticalTick;
            armTimeMs = -1f;

            for (int t = 0; t < tacticalTicks; t++)
            {
                gk.TacticalTick(t, agents, ball, GkAgentIds);

                if (t == armAtTick)
                {
                    armTimeMs = t * framesPerTick * frameMs;
                    gk.OnThreatArmed(Gk0, armTimeMs, 20f, MidAttrs());
                }

                if (t == commitAtTick)
                {
                    gk.CommitSaveIntent(Gk0, Intent(), MidAttrs());
                }

                for (int f = 0; f < framesPerTick; f++)
                {
                    int frame = t * framesPerTick + f;
                    gk.Update(frame, frame * frameMs, agents, ball, GkAgentIds);
                }
            }

            // The launch frame is reset to −1 once the dive resolves (the contact state's
            // ReactionWindowAchieved survives), so tests that need the frame stop the run
            // while the keeper is still airborne.
            return gk.CaptureState().DiveLaunchFrames[Gk0];
        }

        // ── ERR-011-006: stamp lifecycle ─────────────────────────────────────────────────

        [Test]
        public void OnThreatArmed_FirstCallStamps_SecondCallIsNoOp()
        {
            GoalkeeperMechanics gk = NewGk();

            gk.OnThreatArmed(Gk0, 1000f, 20f, MidAttrs());
            float first = gk.CaptureState().ShotDetectedTickMs[Gk0];
            Assert.Greater(first, 0f, "The episode's first OnThreatArmed must stamp detection.");

            float expected = GoalkeeperReactionPipeline.ComputeShotDetectedTickMs(1000f, MidAttrs());
            Assert.AreEqual(expected, first, 1e-3f,
                "The arming stamp must run through the §3.2.1 detection-latency formula.");

            gk.OnThreatArmed(Gk0, 5000f, 20f, MidAttrs());
            Assert.AreEqual(first, gk.CaptureState().ShotDetectedTickMs[Gk0], 0f,
                "A live stamp must win — OnThreatArmed is a no-op until the episode ends (KD-C2).");
        }

        [Test]
        public void OnShotExecutedEvent_OverwritesArmingStamp()
        {
            GoalkeeperMechanics gk = NewGk();

            gk.OnThreatArmed(Gk0, 1000f, 20f, MidAttrs());
            gk.OnShotExecutedEvent(Gk0, 1400f, 24f, MidAttrs());

            float expected = GoalkeeperReactionPipeline.ComputeShotDetectedTickMs(1400f, MidAttrs());
            Assert.AreEqual(expected, gk.CaptureState().ShotDetectedTickMs[Gk0], 1e-3f,
                "A true shot CONTACT is the newest live threat and must overwrite the arming stamp.");
        }

        [Test]
        public void ClearSaveIntent_ClearsDetectionStamp()
        {
            GoalkeeperMechanics gk = NewGk();

            gk.OnThreatArmed(Gk0, 1000f, 20f, MidAttrs());
            gk.ClearSaveIntent(Gk0);

            GoalkeeperTickState s = gk.CaptureState();
            Assert.AreEqual(0f, s.ShotDetectedTickMs[Gk0], 0f,
                "Disarm must clear the stamp — a stale shot must not date a later episode's dive (ERR-011-006).");
            Assert.AreEqual(0f, s.RequiredReactionMs[Gk0], 0f);
        }

        [Test]
        public void ClearSaveIntent_MidDive_PreservesStamp()
        {
            GoalkeeperMechanics gk = NewGk();

            // Arm + commit + drive to a real dive, then try to disarm while airborne.
            RunUntilDive(gk, armAtTick: 2, commitAtTick: 3, tacticalTicks: 5, out _);
            GoalkeeperTickState mid = gk.CaptureState();
            Assert.AreEqual(GoalkeeperState.Airborne, mid.States[Gk0],
                "Precondition: the keeper is airborne inside the driven window.");
            float stamp = mid.ShotDetectedTickMs[Gk0];
            Assert.Greater(stamp, 0f, "Precondition: a live stamp exists for the dive.");

            gk.ClearSaveIntent(Gk0);

            Assert.AreEqual(stamp, gk.CaptureState().ShotDetectedTickMs[Gk0], 0f,
                "ClearSaveIntent is a no-op mid-dive — a live attempt keeps its stamp.");
        }

        [Test]
        public void SaveResolution_ClearsDetectionStamp()
        {
            GoalkeeperMechanics gk = NewGk();

            // 5 tactical ticks reach the dive; the dive lasts DivePhaseDurationMs (~600 ms = 6
            // tactical ticks), after which ground re-entry resolves it to Recovering.
            RunUntilDive(gk, armAtTick: 2, commitAtTick: 3, tacticalTicks: 14, out _);

            GoalkeeperTickState s = gk.CaptureState();
            Assert.AreEqual(GoalkeeperState.Recovering, s.States[Gk0],
                "Precondition: the missed dive resolved to Recovering.");
            Assert.AreEqual(0f, s.ShotDetectedTickMs[Gk0], 0f,
                "A resolved save clears its stamp — it must not date the NEXT episode (ERR-011-006).");
        }

        // ── ERR-011-005: the window is frozen at the dive-launch frame ───────────────────

        [Test]
        public void ReactionWindow_FrozenAtDiveLaunch_AndNotRedatedByFlightTime()
        {
            GoalkeeperMechanics gk = NewGk();

            RunUntilDive(gk, armAtTick: 2, commitAtTick: 3, tacticalTicks: 5, out float armTimeMs);

            GoalkeeperTickState s = gk.CaptureState();
            Assert.AreEqual(GoalkeeperState.Airborne, s.States[Gk0],
                "Precondition: the dive launched inside the driven window.");

            int launchFrame = s.DiveLaunchFrames[Gk0];
            Assert.GreaterOrEqual(launchFrame, 0, "Precondition: a launch frame was recorded.");

            // The expected window, recomputed from the same pure §3.2 pipeline the orchestrator
            // uses, anchored at the LAUNCH frame's match time.
            float launchTimeMs = launchFrame * GoalkeeperConstants.FrameMs;
            float stamp = GoalkeeperReactionPipeline.ComputeShotDetectedTickMs(armTimeMs, MidAttrs());
            float required = GoalkeeperReactionPipeline.ComputeRequiredReactionMs(
                attrs: MidAttrs(), ballSpeedMps: 20f, state: GoalkeeperState.Anticipate);
            float expected = GoalkeeperReactionPipeline.ComputeReactionWindowAchieved(
                launchTimeMs - stamp, required);

            float frozen = s.ContactStates[Gk0].ReactionWindowAchieved;
            Assert.AreEqual(expected, frozen, 1e-4f,
                "The window must be computed at the dive COMMIT (§3.2.5's worked-example anchor), "
                + "not at the contact frame (ERR-011-005).");
            Assert.Greater(frozen, 0.3f,
                "A promptly-committed dive must score a live window — the pre-fix per-frame "
                + "anchor clamped this to ~0 for every realistic flight time.");

            // Let the dive run on several more frames: the frozen value must not decay with
            // flight time — that decay is exactly the pre-fix defect.
            AgentState[] agents = BuildAgents();
            BallState ball = FarThreateningBall();
            for (int f = launchFrame + 1; f < launchFrame + 10; f++)
            {
                gk.Update(f, f * GoalkeeperConstants.FrameMs, agents, ball, GkAgentIds);
            }

            Assert.AreEqual(frozen, gk.CaptureState().ContactStates[Gk0].ReactionWindowAchieved, 0f,
                "The launch-frame window is FROZEN — later frames must not re-date it.");
        }

        [Test]
        public void ReactionWindow_LateCommit_ScoresLowerThanPromptCommit()
        {
            // Same geometry, stamp planted much earlier relative to the commit: the dive
            // launches late against its required reaction time and must score lower — the
            // discrimination the window exists to provide.
            GoalkeeperMechanics prompt = NewGk();
            RunUntilDive(prompt, armAtTick: 2, commitAtTick: 3, tacticalTicks: 5, out _);
            float promptWindow = prompt.CaptureState().ContactStates[Gk0].ReactionWindowAchieved;

            GoalkeeperMechanics late = NewGk();
            RunUntilDive(late, armAtTick: 0, commitAtTick: 6, tacticalTicks: 8, out _);
            float lateWindow = late.CaptureState().ContactStates[Gk0].ReactionWindowAchieved;

            Assert.Less(lateWindow, promptWindow,
                "A dive committed long after detection must score a lower window than a prompt one.");
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-07-28 | —      | Initial. gk-catch-parry-conversion locks: ERR-011-005 (window      |
// |         |            |        | frozen at the dive-launch frame, not re-dated by flight time) and  |
// |         |            |        | ERR-011-006 (stamp lifecycle: OnThreatArmed episode-onset seed,    |
// |         |            |        | shot-stamp precedence, clear-on-disarm/resolution, mid-dive        |
// |         |            |        | preservation), driven through the real 10 Hz + 60 Hz orchestrator. |
#endregion
