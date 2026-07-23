// File:     src/match-engine/tests/MatchEngineGkHeadingScenarios.cs
// Created:  2026-07-23
// Author:   —
// Spec:     GK/Heading engine-integration design supplement
//           (docs/tracking/gk-heading-engine-integration-design.md; the closed-loop scenario item),
//           GK/Heading scenario design supplement (docs/tracking/gk-heading-scenario-design.md),
//           Testing Strategy & Framework #19 §3.3.1 / §3.3.3 / §3.3.5 / Appendix A.1 / KD-8,
//           Deterministic Simulation #16 §3.1 (7-phase tick) / §4.8, Heading #10, Goalkeeper #11,
//           Agent Movement #2, Code Standards #20
// Purpose:  The deferred closed-loop scenario for the opt-in GK (#11) / Heading (#10) engine wiring.
//           Boots a real EnableGkHeading()-on MatchEngine and locks, at the Simulation layer, the
//           composition the unit suite does not exercise: (a) flag-on forward + restore-through-harness
//           determinism, (b) a save/header trigger firing through the NATURAL RunTick AI phase (not the
//           TestOnly drive seam), (c) a same-stimulus flag-off contrast committing nothing, and
//           (d) the flag-on world staying finite / on-pitch across a multi-second composed run.

using System;

using TacticalDirector.DeterministicSim;
using TacticalDirector.TestingStrategy;

using UnityEngine;

namespace TacticalDirector.MatchEngine
{
    /// <summary>
    /// Builds the GK/Heading flag-on composition scenario index (#19 §3.3.5 cross-spec/ layout; KD-8
    /// ownership). The scenario body boots a real <see cref="MatchEngine"/> with
    /// <see cref="MatchEngine.EnableGkHeading"/> set and records envelope predicates for the
    /// Simulation-layer composition of the opt-in GK (#11) / Heading (#10) wiring. Tier B.
    /// </summary>
    internal static class MatchEngineGkHeadingScenarios
    {
        public const string CompositionPath =
            TestingStrategyConstants.SCENARIO_PATH_CROSS_SPEC_PREFIX + "gk-heading-flag-on-composition";

        // Distinct from the away-team/capstone seeds so a shared-seed regression cannot mask this one.
        public const ulong CompositionSeed = 0x11ED9A5EC0DEBA5EUL;

        // 300 ticks @ 60 Hz = 5 s of composed match time; 300 / AI_PHASE_STRIDE = 50 tactical strides,
        // so the flag-on 10 Hz GK/Heading drive runs the exact NumTicks / AI_PHASE_STRIDE count the
        // ai-stride-cadence predicate locks. Restore is taken mid-run at RestoreSaveTick and continued
        // for RestoreContinueTicks (both summing to NumTicks).
        private const int NumTicks             = 300;
        private const int RestoreSaveTick      = 180;
        private const int RestoreContinueTicks = 120;

        // Owning specs: the specs the asserted behaviour is load-bearing on. Agent Movement (#2) for the
        // on-pitch/clamp bound; Heading (#10) + Goalkeeper (#11) for the orchestrators the flag-on drive
        // runs and the projections the triggers commit; Deterministic Simulation (#16) for the 7-phase
        // tick + digest chain + restore; Testing Strategy (#19) for the harness.
        private static readonly int[] OwningSpecIds = { 2, 10, 11, 16, 19 };

        // Pitch envelope (Ball Physics #1 §1.2: corner origin, X 0–105 m, Y 0–68 m). Agents are clamped
        // each physics tick to pitch ± Agent Movement SafetyConstants.PitchBoundaryBuffer (5 m); a small
        // epsilon absorbs float fuzz at the clamp edge. Matches the away-team/capstone convention.
        private const float PitchLengthM   = 105.0f;
        private const float PitchWidthM    = 68.0f;
        private const float AgentBufferM   = 5.0f;
        private const float BallBufferM    = 20.0f; // Ball Physics Limits.PitchBuffer
        private const float BoundsEpsilonM = 0.5f;
        // The only world coordinate with no engine clamp is ball height; a generous ceiling catches a
        // gross divergence without false-failing a legitimately lofted ball.
        private const float BallMaxHeightM = 100.0f;

        // KD-3 save stimulus: team 0's keeper defends x = 0. A loose ball 5 m out driving at the goal at
        // 10 m/s arms SaveArmed for team 0 (the geometry the unit suite proves commits).
        private static readonly Vector3 SaveBallPosition = new Vector3(5f, 34f, 0.11f);
        private static readonly Vector3 SaveBallVelocity = new Vector3(-10f, 0f, 0f);
        // KD-3 header stimulus: a loose airborne ball at an outfield agent's feet-plus-head — within head
        // range, above control height. Position is recomputed at the agent's live position each tick.
        private const float HeaderBallHeightM = 1.0f;

        private enum TriggerKind
        {
            Save,
            Header,
        }

        public static ScenarioIndex BuildIndex()
        {
            var manifest = new ScenarioManifest(
                "gk-heading-flag-on-composition",
                owningSpecIds: OwningSpecIds,
                seed: CompositionSeed,
                tierClassification: TestTier.TierB,
                fixtureRefs: Array.Empty<string>(),
                formatVersion: TestingStrategyConstants.SCENARIO_MANIFEST_FORMAT_VERSION);
            return new ScenarioIndex(new[]
            {
                new ScenarioIndexEntry(
                    CompositionPath,
                    manifest,
                    new ClosedLoopScenario(manifest, GkHeadingComposition)),
            });
        }

        // The composed GK/Heading flag-on locks. Determinism + restore + structural invariants run on pure
        // free-play flag-on engines (no TestOnly mutation); the trigger + flag-off predicates drive their
        // own isolated engines with the world stimulus scripted but the trigger firing left to RunTick.
        private static void GkHeadingComposition(ScenarioContext context)
        {
            ulong seed = context.RunSeed;

            RunObservations a = RunFreePlayFlagOn(seed, NumTicks);
            RunObservations b = RunFreePlayFlagOn(seed, NumTicks);

            // The host advanced exactly NumTicks 60 Hz ticks with the flag on.
            context.Envelope.CheckEquals("tick-count", a.FinalTick, NumTicks);

            // The 10 Hz GK/Heading + AI drive strided exactly NumTicks / AI_PHASE_STRIDE times — the
            // loop-separation composition holds with the flag on, not just for a default engine.
            long expectedAiTicks = NumTicks / DeterministicSimConstants.AI_PHASE_STRIDE;
            context.Envelope.CheckEquals("ai-stride-cadence", a.AiPhaseRunCount, expectedAiTicks);

            // The flag-on Physics/Resolve loop kept the ball finite and inside the engine's clamp envelope
            // across the whole 5 s composed run.
            context.Envelope.CheckTrue(
                "flagon-ball-on-pitch", a.BallInBounds,
                "the ball left the pitch envelope or went non-finite under the flag; first-bad-tick="
                    + a.FirstBadBallTick);

            // Every agent stayed finite and on (or within a small margin of) the pitch with the flag on
            // (Agent Movement #2 clamp) — the flag-on GK/Heading physics drive did not destabilise anyone.
            context.Envelope.CheckTrue(
                "flagon-agents-on-pitch", a.AgentsInBounds,
                "an agent left the pitch envelope or went non-finite under the flag; first-bad-tick="
                    + a.FirstBadAgentTick);

            // Forward determinism: two same-seed flag-on free-play runs produce byte-identical per-tick
            // digest chains (also re-locks EventBus.ResetForNewMatch across two in-process flag-on matches).
            context.Envelope.CheckTrue(
                "flagon-two-run-determinism", DigestChainsEqual(a.Digests, b.Digests, out int fwdDiverge),
                "the flag-on snapshot digest chain diverged between two same-seed runs at tick " + fwdDiverge);

            // Restore determinism through the harness: save@N -> restore -> tick to N+K is byte-identical to
            // an uninterrupted flag-on free-play run to N+K. Exercises the flag-on (v18) snapshot path
            // end-to-end via the in-memory MatchSaveManager blob API.
            context.Envelope.CheckTrue(
                "flagon-restore-continuity",
                RestoreContinuityMatches(seed, RestoreSaveTick, RestoreContinueTicks, out int resDiverge),
                "the flag-on restore continuation diverged from the uninterrupted run at tick " + resDiverge);

            // The projections are a LIVE consumer through the natural RunTick AI phase: the scripted world
            // stimulus makes RunTick's RunAiPhase -> DriveGkHeadingTactical commit a save / header seeded
            // from ToGoalkeeper / ToHeading (not a directly-called drive seam).
            context.Envelope.CheckTrue(
                "save-trigger-commits-via-pipeline", TriggerCommitsThroughPipeline(seed, TriggerKind.Save),
                "the save trigger did not commit through RunTick's AI phase under the on-goal stimulus.");
            context.Envelope.CheckTrue(
                "header-trigger-commits-via-pipeline", TriggerCommitsThroughPipeline(seed, TriggerKind.Header),
                "the header trigger did not commit through RunTick's AI phase under the airborne-ball stimulus.");

            // Flag-off contrast against the SAME stimulus: a flag-off engine ignores the ball stimulus
            // entirely and commits nothing — the byte-identical-to-pre-wiring contract at the Simulation
            // layer (non-vacuous: the same stimulus makes the flag-on engine commit above).
            context.Envelope.CheckTrue(
                "flagoff-save-stimulus-no-commit", !FlagOffCommitsUnderStimulus(seed, TriggerKind.Save),
                "a flag-off engine committed a save under the on-goal stimulus (should ignore it).");
            context.Envelope.CheckTrue(
                "flagoff-header-stimulus-no-commit", !FlagOffCommitsUnderStimulus(seed, TriggerKind.Header),
                "a flag-off engine committed a header under the airborne-ball stimulus (should ignore it).");
        }

        // Pure flag-on free-play: ticks NumTicks with EnableGkHeading() and no TestOnly_* mutation, capturing
        // the per-tick digest chain + the finite/on-pitch observations. No stimulus, so no restore-replay
        // coupling (KD-4).
        private static RunObservations RunFreePlayFlagOn(ulong seed, int ticks)
        {
            var engine = new MatchEngine(seed);
            engine.EnableGkHeading();

            var result = new RunObservations(ticks);

            for (int t = 1; t <= ticks; t++)
            {
                engine.RunTick();
                result.Digests[t - 1] = engine.CurrentSnapshotDigest;

                if (result.BallInBounds && !IsBallInBounds(engine.BallView.Position))
                {
                    result.BallInBounds = false;
                    result.FirstBadBallTick = t;
                }
                if (result.AgentsInBounds && !AllAgentsInBounds(engine))
                {
                    result.AgentsInBounds = false;
                    result.FirstBadAgentTick = t;
                }
            }

            result.FinalTick = (int)engine.CurrentTick;
            result.AiPhaseRunCount = (long)engine.AiPhaseRunCount;
            return result;
        }

        // KD-3: fire a save/header trigger through the natural RunTick pipeline. Re-forces the world stimulus
        // immediately before each RunTick so physics drift cannot move the loose ball out of trigger range
        // before a stride tick's RunAiPhase -> DriveGkHeadingTactical reads it; bounded to 2 * AI_PHASE_STRIDE
        // ticks (offset-independent margin — any AI_PHASE_STRIDE-length window contains a stride tick).
        private static bool TriggerCommitsThroughPipeline(ulong seed, TriggerKind kind)
        {
            var engine = new MatchEngine(seed);
            engine.EnableGkHeading();
            int agent = kind == TriggerKind.Header ? FirstOutfieldAgent(engine) : -1;

            for (int i = 0; i < 2 * DeterministicSimConstants.AI_PHASE_STRIDE; i++)
            {
                ApplyStimulus(engine, kind, agent);
                engine.RunTick();
                if (Committed(engine, kind))
                {
                    return true;
                }
            }
            return false;
        }

        // Same stimulus, flag OFF: DriveGkHeadingTactical is a no-op, so nothing commits. Returns whether the
        // flag-off engine committed (expected false).
        private static bool FlagOffCommitsUnderStimulus(ulong seed, TriggerKind kind)
        {
            var engine = new MatchEngine(seed);   // no EnableGkHeading()
            int agent = kind == TriggerKind.Header ? FirstOutfieldAgent(engine) : -1;

            for (int i = 0; i < 2 * DeterministicSimConstants.AI_PHASE_STRIDE; i++)
            {
                ApplyStimulus(engine, kind, agent);
                engine.RunTick();
                if (Committed(engine, kind))
                {
                    return true;
                }
            }
            return false;
        }

        private static void ApplyStimulus(MatchEngine engine, TriggerKind kind, int headerAgent)
        {
            if (kind == TriggerKind.Save)
            {
                engine.TestOnly_ForceBallLoose(SaveBallPosition, SaveBallVelocity);
                return;
            }
            Vector2 p = engine.AgentView(headerAgent).Position;
            engine.TestOnly_ForceBallLoose(new Vector3(p.x, p.y, HeaderBallHeightM), Vector3.zero);
        }

        private static bool Committed(MatchEngine engine, TriggerKind kind)
            => kind == TriggerKind.Save
                ? engine.TestOnly_LastCommittedSaveAttrs.HasValue
                : engine.TestOnly_LastCommittedHeaderAttrs.HasValue;

        // Save@N -> Encode -> Restore -> tick K == an uninterrupted flag-on free-play run to N+K.
        // The reference chain is 0-indexed (post-tick (i+1) digest at index i), so the restored engine's
        // post-tick (N+j) digest is compared against reference[N + j - 1].
        private static bool RestoreContinuityMatches(ulong seed, int n, int k, out int divergeTick)
        {
            byte[][] reference = RunFreePlayFlagOn(seed, n + k).Digests;

            var engine = new MatchEngine(seed);
            engine.EnableGkHeading();
            for (int t = 0; t < n; t++)
            {
                engine.RunTick();
            }

            byte[] blob = MatchSaveManager.Encode(engine);
            MatchEngine restored = MatchSaveManager.Restore(blob);

            for (int j = 1; j <= k; j++)
            {
                restored.RunTick();
                if (!BytesEqual(restored.CurrentSnapshotDigest, reference[n + j - 1]))
                {
                    divergeTick = n + j;
                    return false;
                }
            }
            divergeTick = -1;
            return true;
        }

        private static int FirstOutfieldAgent(MatchEngine engine)
        {
            for (int i = 0; i < MatchEngineConstants.SQUAD_SIZE; i++)
            {
                if (!engine.AgentIsGoalkeeper(i))
                {
                    return i;
                }
            }
            return -1;
        }

        private static bool AllAgentsInBounds(MatchEngine engine)
        {
            for (int i = 0; i < MatchEngineConstants.SQUAD_SIZE; i++)
            {
                Vector2 p = engine.AgentView(i).Position;
                float bound = AgentBufferM + BoundsEpsilonM;
                if (!float.IsFinite(p.x) || !float.IsFinite(p.y)
                    || p.x < -bound || p.x > PitchLengthM + bound
                    || p.y < -bound || p.y > PitchWidthM + bound)
                {
                    return false;
                }
            }
            return true;
        }

        private static bool IsBallInBounds(Vector3 p)
        {
            float bound = BallBufferM + BoundsEpsilonM;
            return float.IsFinite(p.x) && float.IsFinite(p.y) && float.IsFinite(p.z)
                && p.x >= -bound && p.x <= PitchLengthM + bound
                && p.y >= -bound && p.y <= PitchWidthM + bound
                && p.z >= -BoundsEpsilonM && p.z <= BallMaxHeightM;
        }

        private static bool DigestChainsEqual(byte[][] a, byte[][] b, out int divergeTick)
        {
            int n = Math.Min(a.Length, b.Length);
            for (int i = 0; i < n; i++)
            {
                if (!BytesEqual(a[i], b[i]))
                {
                    divergeTick = i + 1;
                    return false;
                }
            }
            divergeTick = -1;
            return true;
        }

        private static bool BytesEqual(byte[] x, byte[] y)
        {
            if (x == null || y == null || x.Length != y.Length)
            {
                return false;
            }
            for (int i = 0; i < x.Length; i++)
            {
                if (x[i] != y[i])
                {
                    return false;
                }
            }
            return true;
        }

        // Per-run observation sink. Test-side bookkeeping (heap allocation is fine off the hot path).
        private sealed class RunObservations
        {
            public readonly byte[][] Digests;
            public int  FinalTick;
            public long AiPhaseRunCount;
            public bool BallInBounds      = true;
            public bool AgentsInBounds    = true;
            public int  FirstBadBallTick  = -1;
            public int  FirstBadAgentTick = -1;

            public RunObservations(int ticks)
            {
                Digests = new byte[ticks][];
            }
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-07-23 | —      | Initial implementation. The deferred GK/Heading closed-loop        |
// |         |            |        | scenario on the #19 ScenarioRunner (design supplement              |
// |         |            |        | gk-heading-scenario-design.md). Flag-on free-play forward +        |
// |         |            |        | restore-through-harness determinism, structural on-pitch/finite    |
// |         |            |        | invariants, save/header triggers firing through the natural        |
// |         |            |        | RunTick AI phase, and a same-stimulus flag-off no-commit contrast.  |
// |         |            |        | Owning specs {2,10,11,16,19}; path under                           |
// |         |            |        | SCENARIO_PATH_CROSS_SPEC_PREFIX. No production change.             |
#endregion
