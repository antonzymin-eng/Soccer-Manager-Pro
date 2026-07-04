// File:     src/pressing-ai/Tests/PressingScenarios.cs
// Created:  2026-07-03
// Modified: 2026-07-03
// Author:   —
// Spec:     Pressing AI #13 §3.2–§3.11 / §3.13 (8-step pipeline),
//           Testing Strategy & Framework #19 §3.3.1 / §3.3.2 / Appendix A.1,
//           Code Standards #20
// Purpose:  Per-spec closed-loop scenario corpus for Spec #13 (owned by #13 per #19
//           §3.3.1 / KD-8). Drives the REAL PressingAITick.Tick 8-step orchestrator
//           headlessly on the #19 ScenarioRunner — the production entry point that had
//           zero executing coverage (the 91 existing tests all exercise pure functions;
//           PressingAITick was never instantiated or ticked). Positioning view + pass
//           ring + snapshot are built via the same recipe MatchEngine uses.

using System;
using System.Globalization;

using UnityEngine;

using TacticalDirector.PositioningAI;
using TacticalDirector.TestingStrategy;

namespace TacticalDirector.PressingAI.Tests
{
    /// <summary>
    /// Builds the Spec #13 scenario index. Manifest paths follow the #19 §3.3.5 layout;
    /// fixture_refs are empty at Stage 0 (initial state is constructed in code; canonical
    /// fixture files land at Stage 0+1 per #19 KD-10). The pressing pipeline draws no
    /// per-tick randomness, so recorded seeds are canonical identifiers per FR-TS-025, not
    /// behaviour inputs. Tier B (bounded behavioural envelopes per #16 §1.1.1).
    /// </summary>
    internal static class PressingScenarios
    {
        public const string PipelineRunsPath   = "tests/scenarios/pressing-ai/press-pipeline-runs-and-assigns";
        public const string DeterministicPath  = "tests/scenarios/pressing-ai/two-instance-determinism";

        public const ulong PipelineRunsSeed  = 1UL;
        public const ulong DeterministicSeed = 2UL;

        private const int MaxEntity = 21;   // SQUAD_SIZE − 1
        private const int Ticks = 20;       // exercises the debounce / role-hysteresis dwell counters

        public static ScenarioIndex BuildIndex()
        {
            return new ScenarioIndex(new[]
            {
                Entry(PipelineRunsPath,  "press-pipeline-runs-and-assigns", PipelineRunsSeed,  PressPipelineRunsAndAssigns),
                Entry(DeterministicPath, "two-instance-determinism",        DeterministicSeed, TwoInstanceDeterminism),
            });
        }

        private static ScenarioIndexEntry Entry(string manifestPath, string name, ulong seed, ScenarioBody body)
        {
            var manifest = new ScenarioManifest(
                name,
                owningSpecIds: new[] { 13 },
                seed: seed,
                tierClassification: TestTier.TierB,
                fixtureRefs: Array.Empty<string>(),
                formatVersion: TestingStrategyConstants.SCENARIO_MANIFEST_FORMAT_VERSION);
            return new ScenarioIndexEntry(manifestPath, manifest, new ClosedLoopScenario(manifest, body));
        }

        // ── harness (mirrors MatchEngine construction of the pressing subsystem) ──────────

        private static PressingAITick BuildPressing()
        {
            var pos = new PositioningAITick(FormationFamily.F442, MaxEntity);
            pos.SeedFromFormation(new PositioningPerceptionSnapshot(11)); // per-team squad size
            var view = new PositioningAIView(pos);
            return new PressingAITick(view, new PassEventRing(8), MaxEntity);
        }

        // A pressing-team back three converging on an opposing ball carrier near pitch centre.
        private static PressingSnapshot BuildPressSituation()
        {
            PressingSnapshot snap = SnapshotFactory.MakeDefault(); // ball (50,34), carrier=10, press team 0
            SnapshotFactory.SetAgent(snap, 0, 0, SnapshotFactory.PressingTeamId, new Vector2(46f, 34f));
            SnapshotFactory.SetAgent(snap, 1, 1, SnapshotFactory.PressingTeamId, new Vector2(43f, 30f));
            SnapshotFactory.SetAgent(snap, 2, 2, SnapshotFactory.PressingTeamId, new Vector2(43f, 38f));
            // The carrier itself (opposing team) — its position anchors the press target.
            SnapshotFactory.SetAgent(snap, 3, SnapshotFactory.CarrierEntityId, SnapshotFactory.OpposingTeamId,
                new Vector2(50f, 34f));
            return snap;
        }

        private static readonly int[] PressingEntities = { 0, 1, 2 };

        // FNV-1a-64 over the integer content of the directive + per-agent assignments —
        // float-free, so a pure determinism signal for the composed pipeline output.
        private static ulong Digest(PressingAITick p)
        {
            PressDirective d = p.LastDirective;
            ulong h = 14695981039346656037UL;
            h = Mix(h, d.PrimaryPresserId);
            h = Mix(h, d.CoverShadowCount);
            h = Mix(h, (int)d.ActiveTriggers);
            h = Mix(h, d.Shadow0.DefenderId); h = Mix(h, d.Shadow0.ReceiverId);
            h = Mix(h, d.Shadow1.DefenderId); h = Mix(h, d.Shadow1.ReceiverId);
            for (int k = 0; k < PressingEntities.Length; k++)
            {
                PressAssignment a = p.GetAssignment(PressingEntities[k]);
                h = Mix(h, a.EntityId);
                h = Mix(h, (int)a.Role);
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

        // Executes the never-before-run 8-step orchestrator over a realistic press across 20 ticks
        // (so the debounce / role-dwell hysteresis counters actually advance) and asserts the
        // composed output is structurally well-formed regardless of behaviour: a directive with a
        // bounded cover-shadow count, and every queried pressing agent carrying a defined PressRole.
        private static void PressPipelineRunsAndAssigns(ScenarioContext context)
        {
            PressingAITick p = BuildPressing();
            PressingSnapshot snap = BuildPressSituation();
            for (int h = 0; h < Ticks; h++)
            {
                snap.TickIndex = h;
                p.Tick(snap);
            }

            PressDirective d = p.LastDirective;
            context.Envelope.CheckInRange("cover-shadow-count-bounded", d.CoverShadowCount, 0f, 2f);
            context.Envelope.CheckTrue("primary-presser-id-valid", d.PrimaryPresserId >= -1,
                "PrimaryPresserId=" + d.PrimaryPresserId.ToString(CultureInfo.InvariantCulture));

            bool rolesValid = true;
            for (int k = 0; k < PressingEntities.Length; k++)
            {
                PressRole role = p.GetAssignment(PressingEntities[k]).Role;
                rolesValid &= (int)role >= (int)PressRole.HoldShape && (int)role <= (int)PressRole.CoverShadow;
            }
            context.Envelope.CheckTrue("all-assignments-have-defined-role", rolesValid,
                "every pressing agent's assignment carries a defined PressRole (HoldShape/PrimaryPress/CoverShadow)");
        }

        // Two independently-constructed pressing subsystems driven over an identical situation and
        // tick sequence must produce byte-identical directives + assignments (the Simulation-layer
        // determinism lock the pure-function unit suite cannot express).
        private static void TwoInstanceDeterminism(ScenarioContext context)
        {
            PressingAITick p1 = BuildPressing();
            PressingAITick p2 = BuildPressing();
            PressingSnapshot s1 = BuildPressSituation();
            PressingSnapshot s2 = BuildPressSituation();
            for (int h = 0; h < Ticks; h++)
            {
                s1.TickIndex = h; p1.Tick(s1);
                s2.TickIndex = h; p2.Tick(s2);
            }

            ulong d1 = Digest(p1);
            ulong d2 = Digest(p2);
            context.Envelope.CheckTrue("two-instance-digest-equal", d1 == d2,
                "d1=" + d1.ToString(CultureInfo.InvariantCulture) + " d2=" + d2.ToString(CultureInfo.InvariantCulture));
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-03 | —      | Initial implementation: Spec #13 closed-loop scenario corpus  |
// |         |            |        | on the #19 ScenarioRunner — drives the real PressingAITick    |
// |         |            |        | 8-step orchestrator (previously zero executing coverage; the  |
// |         |            |        | 91 unit tests never instantiate or Tick it).                  |
#endregion
