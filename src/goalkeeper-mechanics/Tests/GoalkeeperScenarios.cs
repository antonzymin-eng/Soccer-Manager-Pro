// File:     src/goalkeeper-mechanics/Tests/GoalkeeperScenarios.cs
// Created:  2026-07-03
// Modified: 2026-08-03
// Author:   —
// Spec:     Goalkeeper Mechanics #11 §4.6.1 / §4.6.2 (10 Hz + 60 Hz orchestrator),
//           Testing Strategy & Framework #19 §3.3.1 / §3.3.2 / Appendix A.1,
//           Code Standards #20
// Purpose:  Per-spec closed-loop scenario corpus for Spec #11 (owned by #11 per #19
//           §3.3.1 / KD-8). Drives the REAL GoalkeeperMechanics 10 Hz TacticalTick +
//           60 Hz Update orchestrators headlessly on the #19 ScenarioRunner — the two
//           production entry points that had zero executing coverage (the ~40 existing
//           tests all exercise pure sub-system functions; GoalkeeperMechanics was never
//           instantiated, TacticalTick / Update were never called). The orchestrator is
//           observed through its own constructor-injected dependencies (the FR-CS-051..054
//           design point that makes it testable): the IGoalkeeperRngService draw log and
//           the IGoalkeeperBallSystem call log. The driven regime is publish-free (a save
//           attempt whose dive misses a far ball), so no EventBus boot is required.

using System;
using System.Globalization;

using UnityEngine;

using TacticalDirector.AgentMovement;
using TacticalDirector.BallPhysics;
using TacticalDirector.GoalkeeperMechanics;
using TacticalDirector.TestingStrategy;

namespace TacticalDirector.GoalkeeperMechanics.Tests
{
    /// <summary>
    /// Builds the Spec #11 scenario index. Manifest paths follow the #19 §3.3.5 layout;
    /// fixture_refs are empty at Stage 0 (initial state is constructed in code; canonical
    /// fixture files land at Stage 0+1 per #19 KD-10). The driven save-miss regime makes no
    /// per-frame RNG decisions of its own (the sole draw is the §3.3 dive-timing jitter), so
    /// recorded seeds are canonical identifiers per FR-TS-025, not behaviour inputs. Tier B
    /// (bounded behavioural envelopes per #16 §1.1.1).
    /// </summary>
    internal static class GoalkeeperScenarios
    {
        public const string SaveLaunchPath   = "tests/scenarios/goalkeeper-mechanics/save-launch-executes-dive";
        public const string DeterministicPath = "tests/scenarios/goalkeeper-mechanics/two-instance-determinism";

        public const ulong SaveLaunchSeed    = 1UL;
        public const ulong DeterministicSeed = 2UL;

        // ── world layout ───────────────────────────────────────────────────────────────

        private const int Gk0AgentId = 0;   // team-0 goalkeeper (defends X = 0)
        private const int Gk1AgentId = 1;   // second GK slot; quiet control (no committed intent)
        private const int AgentCount = 2;   // one entry per gkAgentId
        private const int TacticalTicks = 15;
        private const int CommitAtTick  = 1;  // GK0 is in Anticipate after this tactical tick

        public static ScenarioIndex BuildIndex()
        {
            return new ScenarioIndex(new[]
            {
                Entry(SaveLaunchPath,    "save-launch-executes-dive", SaveLaunchSeed,    SaveLaunchExecutesDive),
                Entry(DeterministicPath, "two-instance-determinism",  DeterministicSeed, TwoInstanceDeterminism),
            });
        }

        private static ScenarioIndexEntry Entry(string manifestPath, string name, ulong seed, ScenarioBody body)
        {
            var manifest = new ScenarioManifest(
                name,
                owningSpecIds: new[] { 11 },
                seed: seed,
                tierClassification: TestTier.TierB,
                fixtureRefs: Array.Empty<string>(),
                formatVersion: TestingStrategyConstants.SCENARIO_MANIFEST_FORMAT_VERSION);
            return new ScenarioIndexEntry(manifestPath, manifest, new ClosedLoopScenario(manifest, body));
        }

        // ── injected test doubles (constructor-injected dependency observation) ──────────

        // Records ball-system interactions. The save-miss regime writes nothing (no catch, no
        // parry/deflect/spill), so the recorded counts are the "no terminal contact" observable.
        private sealed class RecordingBallSystem : IGoalkeeperBallSystem
        {
            public int ApplyKickCount;
            public int SetPossessorCount;
            public int ParkBallCount;
            public int GetBallPossessorIdCount;

            public void ApplyKick(Vector3 velocity, Vector3 spin, int agentId, float matchTimeMs)
                => ApplyKickCount++;

            public void SetPossessor(int agentId) => SetPossessorCount++;

            // ERR-011-008 — a claim is SetPossessor AND ParkBall; recorded separately so a scenario
            // can prove the ball was actually arrested, not merely flagged.
            public void ParkBall() => ParkBallCount++;

            // Ball is loose throughout the driven regime; -1 is the "no possessor" sentinel.
            public int GetBallPossessorId()
            {
                GetBallPossessorIdCount++;
                return -1;
            }
        }

        // Records draw-site invocations. Returns are constant (deterministic) — the point is the
        // draw LOG, which is the composition signal: a recorded dive-timing-jitter Gaussian draw
        // proves the 10 Hz SaveIntent flowed through TacticalTick into the 60 Hz Diving → Airborne
        // dive-launch branch of the orchestrator.
        private sealed class RecordingRng : IGoalkeeperRngService
        {
            public int GaussianCount;
            public int FloatCount;
            public int LastGaussianDrawSite = -1;
            public uint LastGaussianDomainTag;
            public bool AllDrawsGoalkeeperDomain = true;

            public float NextFloat(int drawSiteId, uint domainTag)
            {
                FloatCount++;
                CheckDomain(domainTag);
                return 0.5f;
            }

            public float NextGaussian(int drawSiteId, uint domainTag)
            {
                GaussianCount++;
                LastGaussianDrawSite = drawSiteId;
                LastGaussianDomainTag = domainTag;
                CheckDomain(domainTag);
                return 0.0f;
            }

            private void CheckDomain(uint domainTag)
            {
                if (domainTag != GoalkeeperConstants.DomainTagGoalkeeper)
                {
                    AllDrawsGoalkeeperDomain = false;
                }
            }
        }

        // ── harness ──────────────────────────────────────────────────────────────────────

        // Mid-range attribute block (all 10/20, fully rested, team 0). Finite ⇒ dive kinematics
        // produce finite reach/peak values.
        private static GoalkeeperAgentAttributes MidAttrs()
        {
            return new GoalkeeperAgentAttributes
            {
                Reflexes = 10f, Handling = 10f, Composure = 10f,
                Strength = 10f, Aerial   = 10f, Balance   = 10f,
                OneVsOne = 10f, Pace     = 10f, Throwing  = 10f,
                Kicking  = 10f, Fatigue  = 0.0f, TeamId    = 0
            };
        }

        // GK0 near its own goal line; a ball parked deep in GK0's attacking third but far enough
        // that the dive envelope (arm + body reach + launch displacement, a few metres) can never
        // reach it — so the save MISSES and the whole run stays publish-free (no contact ⇒ no
        // SaveAttemptedEvent / BallClaimedEvent, so no EventBus boot).
        private static AgentState[] BuildAgents()
        {
            var agents = new AgentState[AgentCount];
            agents[Gk0AgentId] = AgentState.CreateAtPosition(new Vector2(2f, 34f), Vector2.right);
            agents[Gk1AgentId] = AgentState.CreateAtPosition(new Vector2(103f, 34f), Vector2.left);
            return agents;
        }

        private static BallState BuildBall()
        {
            // ERR-011-002 re-anchor (was x = 75, encoding the inverted wake predicate), then
            // ERR-011-007 re-anchor: the ball must be CLOSING on the keeper's plane inside the
            // §3.3.6 commit lead, or the commit gate correctly HOLDS the dive and this scenario's
            // dive-launch predicate would be starved by its own stimulus (a parked ball is not a
            // save situation under the corrected transition). The scenario never integrates ball
            // physics, so the closing geometry is constant: from (8, 30) at vx = −12 the predicted
            // time-to-plane is (2 − 8)/(−12) = 0.5 s, inside the 0.6 s full-need lead
            // (|predictedY − gkY| = 4 m ≥ DiveLaunchDisplacementM), so the gate opens immediately.
            //
            // The scenario's INTENT is unchanged and is still exactly met: reach Anticipate, launch
            // one dive, miss, mutate no ball state. The ball stays comfortably unreachable: GK0
            // stands at (2, 34), the ball 7.2 m away, against a reach envelope of under 2 m plus at
            // most DiveLaunchDisplacementM (2.2 m) of lateral travel. x = 8 ≤ 35 keeps it inside
            // GK0's defensive third (threatening ⇒ Resting → Set → Anticipate).
            BallState ball = BallState.CreateAtPosition(new Vector3(8f, 30f, 0.11f));
            ball.Velocity = new Vector3(-12f, 0f, 0f);
            return ball;
        }

        private static readonly int[] GkAgentIds = { Gk0AgentId, Gk1AgentId };

        // Drives the real 10 Hz + 60 Hz orchestrator: TacticalTick once per tactical tick, then
        // FramesPerTacticalTick Updates, committing a SaveIntent on GK0 exactly once (after it has
        // reached Anticipate) so the orchestrator carries it Anticipate → Diving → Airborne and
        // launches the dive. No re-commit ⇒ exactly one dive ⇒ exactly one jitter draw.
        private static void RunSaveMiss(GoalkeeperMechanics gk)
        {
            AgentState[] agents = BuildAgents();
            BallState ballState = BuildBall();

            var saveIntent = new SaveIntent
            {
                TargetHand = HandEnum.Right,
                ClutchFirmness = 0.6f,
                DeflectionTarget = null,
                AttemptCommittedTick = CommitAtTick + 1
            };

            int framesPerTick = GoalkeeperConstants.FramesPerTacticalTick;

            for (int t = 0; t < TacticalTicks; t++)
            {
                gk.TacticalTick(t, agents, ballState, GkAgentIds);

                if (t == CommitAtTick)
                {
                    gk.CommitSaveIntent(Gk0AgentId, saveIntent, MidAttrs());
                }

                for (int f = 0; f < framesPerTick; f++)
                {
                    int frame = t * framesPerTick + f;
                    float matchMs = frame * GoalkeeperConstants.FrameMs;
                    gk.Update(frame, matchMs, agents, ballState, GkAgentIds);
                }
            }
        }

        // FNV-1a-64 over the integer content of both injected logs — float-free, a pure
        // determinism signal for the composed orchestrator run.
        private static ulong Digest(RecordingRng rng, RecordingBallSystem ball)
        {
            ulong h = 14695981039346656037UL;
            h = Mix(h, rng.GaussianCount);
            h = Mix(h, rng.FloatCount);
            h = Mix(h, rng.LastGaussianDrawSite);
            h = Mix(h, unchecked((int)rng.LastGaussianDomainTag));
            h = Mix(h, rng.AllDrawsGoalkeeperDomain ? 1 : 0);
            h = Mix(h, ball.ApplyKickCount);
            h = Mix(h, ball.SetPossessorCount);
            h = Mix(h, ball.GetBallPossessorIdCount);
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

        // Executes the never-before-run 10 Hz + 60 Hz orchestrator over a committed save whose
        // dive misses a far ball. Asserts, through the injected dependencies alone, that the
        // composition actually ran the save pipeline (the §3.3 dive-timing jitter draw fired
        // exactly once, from the correct draw site, under the goalkeeper domain tag) and that a
        // missed save produces no ball-state mutation (no catch / parry / deflect / spill).
        private static void SaveLaunchExecutesDive(ScenarioContext context)
        {
            var ball = new RecordingBallSystem();
            var rng = new RecordingRng();
            var gk = new GoalkeeperMechanics(ball, rng);

            RunSaveMiss(gk);

            // Composition liveness: SaveIntent → Diving → Airborne dive-launch executed exactly
            // once (one dive ⇒ one §3.3.2 timing-jitter Gaussian draw; no re-commit ⇒ no re-dive).
            context.Envelope.CheckTrue("dive-jitter-drawn-once", rng.GaussianCount == 1,
                "GaussianCount=" + rng.GaussianCount.ToString(CultureInfo.InvariantCulture)
                + " (expected exactly one §3.3.2 dive-timing-jitter draw)");

            context.Envelope.CheckTrue("dive-jitter-draw-site-correct",
                rng.LastGaussianDrawSite == GoalkeeperConstants.DrawSiteDiveTimingJitter,
                "drawSite=" + rng.LastGaussianDrawSite.ToString(CultureInfo.InvariantCulture));

            // The save path draws no uniform values (only the timing-jitter Gaussian).
            context.Envelope.CheckTrue("no-uniform-draws", rng.FloatCount == 0,
                "FloatCount=" + rng.FloatCount.ToString(CultureInfo.InvariantCulture));

            // Every registered draw carries DOMAIN_TAG_GOALKEEPER (0x1D).
            context.Envelope.CheckTrue("all-draws-goalkeeper-domain", rng.AllDrawsGoalkeeperDomain,
                "at least one draw used a non-goalkeeper domain tag");

            // A missed dive mutates no ball state (FR-CS: fail-loud only on contact).
            context.Envelope.CheckTrue("miss-produces-no-ball-mutation",
                ball.ApplyKickCount == 0 && ball.SetPossessorCount == 0,
                "ApplyKick=" + ball.ApplyKickCount.ToString(CultureInfo.InvariantCulture)
                + " SetPossessor=" + ball.SetPossessorCount.ToString(CultureInfo.InvariantCulture));
        }

        // Two independently-constructed orchestrators driven over the identical situation and
        // tick sequence must produce byte-identical injected-dependency logs (the Simulation-layer
        // determinism lock the pure-function sub-system suite cannot express).
        private static void TwoInstanceDeterminism(ScenarioContext context)
        {
            var ball1 = new RecordingBallSystem();
            var rng1 = new RecordingRng();
            RunSaveMiss(new GoalkeeperMechanics(ball1, rng1));

            var ball2 = new RecordingBallSystem();
            var rng2 = new RecordingRng();
            RunSaveMiss(new GoalkeeperMechanics(ball2, rng2));

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
// | 1.0     | 2026-07-03 | —      | Initial implementation: Spec #11 closed-loop scenario corpus  |
// |         |            |        | on the #19 ScenarioRunner — drives the real GoalkeeperMechanics|
// |         |            |        | 10 Hz TacticalTick + 60 Hz Update orchestrators (previously    |
// |         |            |        | zero executing coverage; the sub-system suite never instantiates|
// |         |            |        | or ticks the orchestrator), observed via injected dependencies.|
// | 1.1 | 2026-07-27 | — | ERR-011-002 fallout: sim_goalkeeper_save_launch_executes_dive placed the ball at|
// |     |            |   | x = 75 to wake keeper 0, i.e. it had ENCODED the inverted predicate. Re-anchored|
// |     |            |   | to x = 30 with the intent unchanged — the Phase-H "tests encoded the old|
// |     |            |   | contract" class.                                                        |
// | 1.3 | 2026-07-28 | — | gk-contact-rate (ERR-011-007) re-anchor: the scenario ball CLOSES on the    |
// |     |            |   | keeper's plane inside the SS3.3.6 commit lead (a parked ball now correctly |
// |     |            |   | holds the dive, starving the dive-launch predicate). Intent preserved.     |
// | 1.4     | 2026-08-03 | —      | ERR-011-008: RecordingBallSystem gains ParkBall (counted separately, so a scenario can prove the ball was arrested and not merely flagged). |
#endregion
