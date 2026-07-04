// File:     src/heading-mechanics/Tests/HeadingScenarios.cs
// Created:  2026-07-04
// Modified: 2026-07-04
// Author:   —
// Spec:     Heading Mechanics #10 §4.6 (60 Hz orchestrator), §3.2 eligibility, §3.3 jump kinematics,
//           Testing Strategy & Framework #19 §3.3.1 / §3.3.2 / Appendix A.1, Code Standards #20
// Purpose:  Per-spec closed-loop scenario corpus for Spec #10 (owned by #10 per #19 §3.3.1 / KD-8).
//           Drives the REAL HeadingMechanics 60 Hz Update orchestrator + CommitIntent headlessly on
//           the #19 ScenarioRunner — the production entry points with zero executing coverage (the
//           30+ existing tests all exercise pure sub-system functions; HeadingMechanics is
//           instantiated nowhere, Update/CommitIntent are called nowhere, and every §5.2/§5.3
//           integration test is an Assert.Ignore stub). Like Goalkeeper #11, the orchestrator is
//           constructor-injected (IHeadingBallSystem + IHeadingRngService) with no public state
//           accessor, so it is observed through its own injected dependencies. The driven regime is
//           a committed header whose ball is parked far out of reach, so the header never reaches
//           contact — publish-free (no HeaderExecutedEvent / HeaderAttemptFailedEvent), so no
//           EventBus boot is required.

using System;
using System.Globalization;

using UnityEngine;

using TacticalDirector.AgentMovement;
using TacticalDirector.BallPhysics;
using TacticalDirector.TestingStrategy;

namespace TacticalDirector.HeadingMechanics.Tests
{
    /// <summary>
    /// Builds the Spec #10 scenario index. Manifest paths follow the #19 §3.3.5 layout; fixture_refs
    /// are empty at Stage 0 (initial state is constructed in code; canonical fixture files land at
    /// Stage 0+1 per #19 KD-10). The driven no-contact regime makes no per-frame RNG decisions (the
    /// §3.4 contact-quality Gaussians and the §3.7 duel-tiebreak uniform fire only at a contact frame,
    /// which is never reached), so recorded seeds are canonical identifiers per FR-TS-025, not
    /// behaviour inputs. Tier B (bounded behavioural envelopes per #16 §1.1.1).
    /// </summary>
    internal static class HeadingScenarios
    {
        public const string RunsPublishFreePath = "tests/scenarios/heading-mechanics/committed-header-runs-publish-free";
        public const string DeterministicPath   = "tests/scenarios/heading-mechanics/two-instance-determinism";

        public const ulong RunsPublishFreeSeed = 1UL;
        public const ulong DeterministicSeed   = 2UL;

        // ── world layout ─────────────────────────────────────────────────────────────────
        private const int HeaderAgentId = 0;   // team-0 attacker committing the header
        private const int CommitFrame   = 0;   // CommitIntent stamps the ball snapshot at frame 0
        private const int StartFrame    = 2;   // first Update; the 2-frame gap trips the §3.2 FR-HE-033
                                               // stale re-query (GetBallState), which is only reachable
                                               // PAST the aerial gate — a positive liveness signal.

        public static ScenarioIndex BuildIndex()
        {
            return new ScenarioIndex(new[]
            {
                Entry(RunsPublishFreePath, "committed-header-runs-publish-free", RunsPublishFreeSeed, RunsPublishFree),
                Entry(DeterministicPath,   "two-instance-determinism",          DeterministicSeed,   TwoInstanceDeterminism),
            });
        }

        private static ScenarioIndexEntry Entry(string manifestPath, string name, ulong seed, ScenarioBody body)
        {
            var manifest = new ScenarioManifest(
                name,
                owningSpecIds: new[] { 10 },
                seed: seed,
                tierClassification: TestTier.TierB,
                fixtureRefs: Array.Empty<string>(),
                formatVersion: TestingStrategyConstants.SCENARIO_MANIFEST_FORMAT_VERSION);
            return new ScenarioIndexEntry(manifestPath, manifest, new ClosedLoopScenario(manifest, body));
        }

        // ── injected test doubles (constructor-injected dependency observation) ────────────

        // Records ball-system interactions. The no-contact regime writes nothing (ApplyKick is
        // reached only on successful contact, FR-HE-006). GetBallState is the composition-liveness
        // signal: it fires from Evaluate step 2 (FR-HE-033), which is only reached once the §3.2
        // aerial gate has passed — i.e. the committed header actually launched and the eligibility
        // pipeline ran.
        private sealed class RecordingBallSystem : IHeadingBallSystem
        {
            public int ApplyKickCount;
            public int GetBallStateCount;
            private readonly BallState _ball;

            public RecordingBallSystem(BallState ball) => _ball = ball;

            public BallState GetBallState(float matchTime)
            {
                GetBallStateCount++;
                return _ball;   // static far ball — re-query never brings it into the head volume
            }

            public void ApplyKick(Vector3 velocity, Vector3 spin, int agentId, float matchTime)
                => ApplyKickCount++;
        }

        // Records draw-site invocations. Returns are constant (deterministic); the point is the draw
        // LOG. A no-contact run draws nothing (contact-quality + duel RNG are contact-gated), so a
        // non-zero count would be a composition regression.
        private sealed class RecordingRng : IHeadingRngService
        {
            public int FloatCount;
            public int GaussianCount;

            public float NextFloat(int drawSiteId)   { FloatCount++;    return 0.5f; }
            public float NextGaussian(int drawSiteId) { GaussianCount++; return 0.0f; }
        }

        // ── harness ──────────────────────────────────────────────────────────────────────

        private static HeadingAgentAttributes Attrs()
        {
            return new HeadingAgentAttributes
            {
                Heading = 14, Strength = 13, Balance = 12, Fatigue = 0.0f, TeamId = 0
            };
        }

        // Header agent airborne (JOGGING ⇒ passes the §3.2 aerial gate) mid-pitch; every other slot
        // is inactive (no committed intent) and never indexed by the orchestrator.
        private static AgentState[] BuildAgents()
        {
            var agents = new AgentState[HeadingMechanicsConstants.MaxAgents];
            for (int i = 0; i < agents.Length; i++)
            {
                agents[i] = AgentState.CreateAtPosition(new Vector2(52f, 34f), Vector2.right);
            }
            agents[HeaderAgentId].CurrentState = AgentMovementState.JOGGING;
            return agents;
        }

        // Ball parked far from the header agent with zero velocity ⇒ FindContactFrame never returns a
        // frame ⇒ the header is never eligible and never reaches contact (publish-free).
        private static BallState BuildBall() => BallState.CreateAtPosition(new Vector3(10f, 10f, 0.11f));

        // Drives the real 60 Hz orchestrator: commit one header, then Update from StartFrame up to the
        // apex frame. Ticking stops AT the apex (never past IdealContactFrame), so the not-eligible
        // path never emits PositionedPoorly and the run stays publish-free.
        private static void RunNoContact(HeadingMechanics heading)
        {
            AgentState[] agents = BuildAgents();
            BallState ball = BuildBall();

            var intent = new HeaderIntent
            {
                PowerIntent = 0.7f,
                ContactPointIntent = Vector2.zero,
                TargetIntent = new Vector3(90f, 34f, 2.0f),
                AttemptCommittedTick = 0,
                SetPieceContext = SetPieceContext.OpenPlay
            };

            heading.CommitIntent(HeaderAgentId, intent, Attrs(), ball, CommitFrame);

            int apexFrame = HeadingJumpKinematics.ComputeApexFrame(StartFrame);
            for (int frame = StartFrame; frame <= apexFrame; frame++)
            {
                float matchMs = frame * HeadingMechanicsConstants.FrameMs;
                heading.Update(agents, ball, frame, matchMs);
            }
        }

        // FNV-1a-64 over the integer content of both injected logs — float-free, a pure determinism
        // signal for the composed orchestrator run.
        private static ulong Digest(RecordingRng rng, RecordingBallSystem ball)
        {
            ulong h = 14695981039346656037UL;
            h = Mix(h, rng.FloatCount);
            h = Mix(h, rng.GaussianCount);
            h = Mix(h, ball.ApplyKickCount);
            h = Mix(h, ball.GetBallStateCount);
            return h;
        }

        private static ulong Mix(ulong h, int value)
        {
            unchecked // Spec #16 §3.4.4: deliberate 64-bit wrap-around; not an overflow bug
            {
                h ^= (uint)value;
                h *= 1099511628211UL;
            }
            return h;
        }

        // ── scenarios ─────────────────────────────────────────────────────────────────────

        // Executes the never-before-run 60 Hz orchestrator over a committed header whose ball is out
        // of reach. Asserts, through the injected dependencies alone, that the composition actually
        // launched and ran the §3.2 eligibility pipeline (the FR-HE-033 stale re-query fired exactly
        // once — only reachable past the aerial gate) and that a no-contact attempt is inert: no
        // randomness drawn (contact-quality + duel RNG are contact-gated) and no ball mutation
        // (ApplyKick is never called on a failed / no-contact attempt, FR-HE-006).
        private static void RunsPublishFree(ScenarioContext context)
        {
            var ball = new RecordingBallSystem(BuildBall());
            var rng = new RecordingRng();
            var heading = new HeadingMechanics(ball, rng);

            RunNoContact(heading);

            // Composition liveness: the committed header launched and Evaluate ran (the §3.2 step-2
            // FR-HE-033 re-query is unreachable until the aerial gate passes).
            context.Envelope.CheckTrue("eligibility-pipeline-executed", ball.GetBallStateCount == 1,
                "GetBallStateCount=" + ball.GetBallStateCount.ToString(CultureInfo.InvariantCulture)
                + " (expected exactly one §3.2 FR-HE-033 stale re-query)");

            // No contact frame ⇒ no §3.4 contact-quality / §3.7 duel-tiebreak draws.
            context.Envelope.CheckTrue("no-contact-no-draws",
                rng.FloatCount == 0 && rng.GaussianCount == 0,
                "FloatCount=" + rng.FloatCount.ToString(CultureInfo.InvariantCulture)
                + " GaussianCount=" + rng.GaussianCount.ToString(CultureInfo.InvariantCulture));

            // FR-HE-006: ApplyKick is never called on a failed / no-contact attempt.
            context.Envelope.CheckTrue("no-contact-no-ball-mutation", ball.ApplyKickCount == 0,
                "ApplyKickCount=" + ball.ApplyKickCount.ToString(CultureInfo.InvariantCulture));
        }

        // Two independently-constructed orchestrators driven over the identical situation and tick
        // sequence must produce byte-identical injected-dependency logs (the Simulation-layer
        // determinism lock the pure-function sub-system suite cannot express).
        private static void TwoInstanceDeterminism(ScenarioContext context)
        {
            var ball1 = new RecordingBallSystem(BuildBall());
            var rng1 = new RecordingRng();
            RunNoContact(new HeadingMechanics(ball1, rng1));

            var ball2 = new RecordingBallSystem(BuildBall());
            var rng2 = new RecordingRng();
            RunNoContact(new HeadingMechanics(ball2, rng2));

            ulong d1 = Digest(rng1, ball1);
            ulong d2 = Digest(rng2, ball2);
            context.Envelope.CheckTrue("two-instance-digest-equal", d1 == d2,
                "d1=" + d1.ToString(CultureInfo.InvariantCulture)
                + " d2=" + d2.ToString(CultureInfo.InvariantCulture));
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-04 | —      | Initial implementation: Spec #10 closed-loop scenario corpus  |
// |         |            |        | on the #19 ScenarioRunner — drives the real HeadingMechanics  |
// |         |            |        | 60 Hz Update + CommitIntent orchestrator (previously zero     |
// |         |            |        | executing coverage; every §5.2/§5.3 integration test is an    |
// |         |            |        | Assert.Ignore stub), observed via injected dependencies.      |
#endregion
