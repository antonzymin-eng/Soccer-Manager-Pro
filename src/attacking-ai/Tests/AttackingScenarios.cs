// File:     src/attacking-ai/Tests/AttackingScenarios.cs
// Created:  2026-07-03
// Modified: 2026-07-03
// Author:   —
// Spec:     Attacking AI #15 §3.13 (10-step pipeline),
//           Testing Strategy & Framework #19 §3.3.1 / §3.3.2 / Appendix A.1,
//           Code Standards #20
// Purpose:  Per-spec closed-loop scenario corpus for Spec #15 (owned by #15 per #19
//           §3.3.1 / KD-8). Drives the REAL AttackingAITick.Tick 10-step orchestrator
//           headlessly on the #19 ScenarioRunner — the production entry point that had
//           zero executing coverage (the 104 existing tests all exercise pure functions;
//           AttackingAITick was never instantiated or ticked). Positioning view + snapshot
//           are built via the same recipe MatchEngine.FillAttackingSnapshot uses.

using System;
using System.Globalization;

using UnityEngine;

using TacticalDirector.PositioningAI;
using TacticalDirector.PressingAI;
using TacticalDirector.TestingStrategy;

namespace TacticalDirector.AttackingAI.Tests
{
    /// <summary>
    /// Builds the Spec #15 scenario index. Manifest paths follow the #19 §3.3.5 layout;
    /// fixture_refs are empty at Stage 0 (initial state is constructed in code; canonical
    /// fixture files land at Stage 0+1 per #19 KD-10). The attacking pipeline draws no
    /// per-tick randomness, so recorded seeds are canonical identifiers per FR-TS-025, not
    /// behaviour inputs. Tier B (bounded behavioural envelopes per #16 §1.1.1).
    /// </summary>
    internal static class AttackingScenarios
    {
        public const string PipelineRunsPath  = "tests/scenarios/attacking-ai/attack-pipeline-runs-and-assigns";
        public const string DeterministicPath = "tests/scenarios/attacking-ai/two-instance-determinism";

        public const ulong PipelineRunsSeed  = 1UL;
        public const ulong DeterministicSeed = 2UL;

        private const int MaxEntity = 21;   // SQUAD_SIZE − 1
        private const int SquadSize = 22;   // both teams; matches AttackingSnapshot.Agents.Length
        private const int Ticks     = 20;   // exercises the AttackHysteresis dwell counters

        private const int AttackingTeamId = 0;
        private const int GkEntityId       = 0;   // team-0 goalkeeper (excluded from pool)
        private const int CarrierEntityId  = 1;   // team-0 ball carrier (excluded from pool)

        public static ScenarioIndex BuildIndex()
        {
            return new ScenarioIndex(new[]
            {
                Entry(PipelineRunsPath,  "attack-pipeline-runs-and-assigns", PipelineRunsSeed,  AttackPipelineRunsAndAssigns),
                Entry(DeterministicPath, "two-instance-determinism",         DeterministicSeed, TwoInstanceDeterminism),
            });
        }

        private static ScenarioIndexEntry Entry(string manifestPath, string name, ulong seed, ScenarioBody body)
        {
            var manifest = new ScenarioManifest(
                name,
                owningSpecIds: new[] { 15 },
                seed: seed,
                tierClassification: TestTier.TierB,
                fixtureRefs: Array.Empty<string>(),
                formatVersion: TestingStrategyConstants.SCENARIO_MANIFEST_FORMAT_VERSION);
            return new ScenarioIndexEntry(manifestPath, manifest, new ClosedLoopScenario(manifest, body));
        }

        // ── harness (mirrors MatchEngine construction of the attacking subsystem) ─────────

        private static readonly StyleProfile Profile = StyleProfile.Possession;

        private static AttackingAITick BuildAttacking()
        {
            var pos = new PositioningAITick(FormationFamily.F442, MaxEntity);
            pos.SeedFromFormation(new PositioningPerceptionSnapshot(11)); // per-team squad size; seeds Phase.InPoss
            var view = new PositioningAIView(pos);
            return new AttackingAITick(view, Profile, MaxEntity);
        }

        // Team-0 in possession pushing toward +X; a spread of outfield attackers so role
        // assignment (support / runner / width-hold / weak-side) has meaningful geometry.
        // Baseline slots are finite (never the F2 SENTINEL_NO_SLOT), so the pool is non-empty.
        private static AttackingSnapshot BuildAttackSituation()
        {
            var snap = new AttackingSnapshot();
            snap.AttackingTeamId     = AttackingTeamId;
            snap.BallPosition        = new Vector2(55f, 34f);
            snap.BallCarrierEntityId = CarrierEntityId;
            snap.BallCarrierPosition = new Vector2(55f, 34f);
            snap.TeamAttackAngle     = 0f;   // attacking +X in the canonical frame

            for (int i = 0; i < SquadSize; i++)
            {
                int teamId       = i < 11 ? 0 : 1;
                bool isGoalkeeper = i == GkEntityId || i == 11;
                Vector2 pos       = AgentPosition(i);
                snap.Agents[i] = new AttackingAgentSnapshot(
                    entityId:     i,
                    teamId:       teamId,
                    position:     pos,
                    baselineSlot: pos,                       // finite ⇒ never the F2 sentinel
                    line:         LineForEntity(i),
                    isGoalkeeper: isGoalkeeper,
                    hasBall:      i == CarrierEntityId,
                    isActive:     true,
                    pace:         0.5f,
                    stamina:      0.5f,
                    dribbling:    0.5f);
            }
            return snap;
        }

        // Deterministic spread across the attacking third for team-0 outfielders; team-1
        // agents are irrelevant (the pool builder filters them by team) but positioned validly.
        private static Vector2 AgentPosition(int entityId)
        {
            if (entityId >= 11)
                return new Vector2(30f, 10f + (entityId - 11) * 4f);   // team-1, parked deep
            float x = 45f + entityId * 3f;                             // 45..75 m up the pitch
            float y = 8f + entityId * 5f;                              // fanned across the width
            if (y > 60f) y = 60f;
            return new Vector2(x, y);
        }

        private static LineId LineForEntity(int entityId)
        {
            if (entityId <= 3) return LineId.Defense;
            if (entityId <= 7) return LineId.Midfield;
            return LineId.Attack;
        }

        // Team-0 outfield, non-carrier entity ids that enter the pool (GK 0 + carrier 1 excluded).
        private static readonly int[] PoolEntities = { 2, 3, 4, 5, 6, 7, 8, 9, 10 };

        // FNV-1a-64 over the integer content of the directive + per-agent intents — float-free,
        // a pure determinism signal for the composed pipeline output.
        private static ulong Digest(AttackingAITick a)
        {
            AttackDirective d = a.LastDirective;
            ulong h = 14695981039346656037UL;
            h = Mix(h, d.TeamId);
            h = Mix(h, d.OverloadActive ? 1 : 0);
            h = Mix(h, (int)d.OverloadFlank);
            h = Mix(h, d.TransitionHoldTick);
            for (int k = 0; k < PoolEntities.Length; k++)
            {
                AttackIntent intent = a.GetIntent(PoolEntities[k]);
                h = Mix(h, intent.AgentEntityId);
                h = Mix(h, (int)intent.Role);
                RunParameters? rp = intent.RunParameters;
                h = Mix(h, rp.HasValue ? 1 : 0);
                if (rp.HasValue) h = Mix(h, rp.Value.RunTriggerTick);
            }
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

        // Executes the never-before-run 10-step orchestrator over a realistic IN_POSSESSION attack
        // across 20 ticks (so the AttackHysteresis dwell counters actually advance) and asserts the
        // composed output is structurally well-formed regardless of behaviour: every pool agent
        // carries a defined AttackRole; the InvariantEnforcer runner cap holds (§3.11 / FR-AT-018);
        // and the per-agent AttackIntent RunParameters?-vs-Role contract is coherent (FR-AT-011).
        private static void AttackPipelineRunsAndAssigns(ScenarioContext context)
        {
            AttackingAITick a = BuildAttacking();
            AttackingSnapshot snap = BuildAttackSituation();
            for (int h = 0; h < Ticks; h++)
            {
                snap.TickIndex = h;
                a.Tick(snap);
            }

            bool rolesValid    = true;
            bool runParamsValid = true;
            int  runnerCount    = 0;
            for (int k = 0; k < PoolEntities.Length; k++)
            {
                AttackIntent intent = a.GetIntent(PoolEntities[k]);
                AttackRole role = intent.Role;
                rolesValid &= (int)role >= (int)AttackRole.HoldWidth && (int)role <= (int)AttackRole.WeakSide;

                bool isRunner = role == AttackRole.Runner;
                if (isRunner) runnerCount++;
                // FR-AT-011 / §2.2.2: RunParameters is non-null iff the role is Runner.
                runParamsValid &= (intent.RunParameters.HasValue == isRunner);
            }

            context.Envelope.CheckTrue("all-intents-have-defined-role", rolesValid,
                "every pool agent's intent carries a defined AttackRole (HoldWidth/SupportBall/Runner/WeakSide)");
            context.Envelope.CheckTrue("runparams-coherent-with-role", runParamsValid,
                "RunParameters is non-null exactly when Role == Runner (FR-AT-011 / §2.2.2)");
            context.Envelope.CheckInRange("runner-count-within-cap", runnerCount, 0f, Profile.MaxRunners);

            AttackDirective d = a.LastDirective;
            context.Envelope.CheckTrue("overload-flank-defined", d.OverloadActive
                    ? (d.OverloadFlank == Flank.Left || d.OverloadFlank == Flank.Right)
                    : true,
                "OverloadFlank is a defined Flank when the directive declares an overload (§2.2.1)");
        }

        // Two independently-constructed attacking subsystems driven over an identical situation and
        // tick sequence must produce byte-identical directives + intents (the Simulation-layer
        // determinism lock the pure-function unit suite cannot express).
        private static void TwoInstanceDeterminism(ScenarioContext context)
        {
            AttackingAITick a1 = BuildAttacking();
            AttackingAITick a2 = BuildAttacking();
            AttackingSnapshot s1 = BuildAttackSituation();
            AttackingSnapshot s2 = BuildAttackSituation();
            for (int h = 0; h < Ticks; h++)
            {
                s1.TickIndex = h; a1.Tick(s1);
                s2.TickIndex = h; a2.Tick(s2);
            }

            ulong d1 = Digest(a1);
            ulong d2 = Digest(a2);
            context.Envelope.CheckTrue("two-instance-digest-equal", d1 == d2,
                "d1=" + d1.ToString(CultureInfo.InvariantCulture) + " d2=" + d2.ToString(CultureInfo.InvariantCulture));
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-03 | —      | Initial implementation: Spec #15 closed-loop scenario corpus  |
// |         |            |        | on the #19 ScenarioRunner — drives the real AttackingAITick   |
// |         |            |        | 10-step orchestrator (previously zero executing coverage; the |
// |         |            |        | 104 unit tests never instantiate or Tick it).                 |
#endregion
