// File:     src/decision-tree/ActionSelector.cs
// Created:  2026-05-29
// Modified: 2026-06-14 (audit AR-3 fix pass)
// Author:   —
// Spec:     Decision Tree #8 §3.3, Code Standards #20
// Purpose:  Step 5 of the 6-step pipeline. Injects deterministic composure noise into
//           each option's EffectiveUtility, selects the highest-scoring option, and
//           constructs the AgentAction. Pure function: no side effects. §3.3.

using UnityEngine;
using TacticalDirector.PassMechanics;
using TacticalDirector.ShotMechanics;

namespace TacticalDirector.DecisionTree
{
    /// <summary>
    /// Step 5: composure noise injection + deterministic action selection.
    /// Returns the winning AgentAction and selection metadata.
    /// Decision Tree #8 §3.3.
    /// </summary>
    internal static class ActionSelector
    {
        private struct SelectionMetadata
        {
            public int  CandidateCount;
            public bool TiebreakerApplied;
            public bool FallbackToHold;
        }

        /// <summary>
        /// Selects the highest-EffectiveUtility option after composure noise injection.
        /// On empty candidate list, falls back to HOLD (§3.3.6).
        /// §3.3.2.
        /// </summary>
        internal static AgentAction SelectAction(
            ActionOption[] optionBuffer,
            int count,
            in DecisionContext ctx,
            out bool tiebreakerApplied,
            out bool fallbackToHold)
        {
            tiebreakerApplied = false;
            fallbackToHold    = false;

            // §3.3.6: no-viable-option fallback
            if (count == 0)
            {
                fallbackToHold = true;
                return AgentAction.HoldFallback(ctx.AgentId, ctx.CurrentFrame);
            }

            // ── Noise injection (§3.3.3 / §3.3.4) ───────────────────────────────
            float noiseBound = ComposureWeights.NOISE_MAX
                * (1.0f - ctx.A_Composure * ComposureWeights.COMPOSURE_SUPPRESSION);

            for (int i = 0; i < count; i++)
            {
                int ordinal = (int)optionBuffer[i].Type;
                float rawNoise = ComputeOptionNoise(
                    ctx.MatchSeed, ctx.AgentId, ctx.CurrentFrame, ordinal);
                optionBuffer[i].EffectiveUtility =
                    optionBuffer[i].BaseUtility + rawNoise * noiseBound;
            }

            // ── Winner selection (§3.3.2.1) ───────────────────────────────────
            int    bestIdx  = 0;
            float  bestUtil = optionBuffer[0].EffectiveUtility;

            for (int i = 1; i < count; i++)
            {
                float diff = optionBuffer[i].EffectiveUtility - bestUtil;
                if (diff > ComposureWeights.TIEBREAK_EPSILON)
                {
                    bestIdx  = i;
                    bestUtil = optionBuffer[i].EffectiveUtility;
                    // §3.3 pseudocode: a strictly better candidate resets the tie flag —
                    // INV-SEL-07 requires TiebreakerApplied to describe the WINNER, not
                    // any candidate seen earlier in the scan (AR-2 L: flag previously
                    // latched true even after a clear-margin winner emerged).
                    tiebreakerApplied = false;
                }
                else if (Mathf.Abs(diff) <= ComposureWeights.TIEBREAK_EPSILON)
                {
                    // Tiebreak: lower ActionType ordinal wins (deterministic) (§3.3.5)
                    if ((int)optionBuffer[i].Type < (int)optionBuffer[bestIdx].Type)
                    {
                        bestIdx  = i;
                        bestUtil = optionBuffer[i].EffectiveUtility;
                    }
                    tiebreakerApplied = true;
                }
            }

            return BuildAgentAction(in optionBuffer[bestIdx], ctx.AgentId, ctx.CurrentFrame);
        }

        // ── §3.3.3 Deterministic noise hash ───────────────────────────────────

        private static float ComputeOptionNoise(
            ulong matchSeed,
            int agentId,
            int heartbeatTick,
            int actionTypeOrdinal)
        {
            // Pack the three deterministic inputs into disjoint bit fields before the
            // SplitMix64 mix. Each input is masked to its allotted width so an
            // out-of-range value cannot bleed into the next field and silently
            // correlate noise (e.g. a heartbeat tick past ~1.05M frames, or a stray
            // agentId ≥ 32). The masked layout reproduces the prior shift layout
            // bit-for-bit on every in-range input, so determinism is unchanged.
            //   agentId           : bits  0–4  (5 bits  → [0, 31];  AgentId range 0–21)
            //   heartbeatTick     : bits  5–24 (20 bits → [0, ~1.05M]; > a full match)
            //   actionTypeOrdinal : bits 25–27 (3 bits  → [0, 7];   7 action types)
            const int AgentIdBits   = 5;
            const int HeartbeatBits = 20;
            const int OrdinalBits   = 3;
            const ulong AgentIdMask   = (1UL << AgentIdBits)   - 1UL;
            const ulong HeartbeatMask = (1UL << HeartbeatBits) - 1UL;
            const ulong OrdinalMask   = (1UL << OrdinalBits)   - 1UL;

            ulong packed   = ((ulong)agentId           & AgentIdMask)
                           | (((ulong)heartbeatTick    & HeartbeatMask) << AgentIdBits)
                           | (((ulong)actionTypeOrdinal & OrdinalMask)  << (AgentIdBits + HeartbeatBits));
            ulong combined = matchSeed ^ packed;
            ulong hash     = SplitMix64(combined);

            // Convert upper 24 bits to float in [0, 1), then map to [-1, +1)
            float unit = (hash >> 40) * (1.0f / (1UL << 24));
            return unit * 2.0f - 1.0f;
        }

        private static ulong SplitMix64(ulong x)
        {
            unchecked  // Spec #16 §3.4.4: deliberate 64-bit wrap-around; not an overflow bug
            {
                x += 0x9E3779B97F4A7C15UL;
                x = (x ^ (x >> 30)) * 0xBF58476D1CE4E5B9UL;
                x = (x ^ (x >> 27)) * 0x94D049BB133111EBUL;
                return x ^ (x >> 31);
            }
        }

        // ── AgentAction construction from winning ActionOption ─────────────────

        private static AgentAction BuildAgentAction(
            in ActionOption opt,
            int agentId,
            int currentFrame)
        {
            // Pre-populate PassParams / ShotParams from the option.
            // ActionDispatcher fills AgentId and FrameNumber at dispatch time (§3.5.2/3).
            PassRequest passParams = default;
            ShotRequest shotParams = default;

            if (opt.Type == ActionType.PASS)
            {
                passParams = new PassRequest
                {
                    AgentId          = agentId,
                    PassType         = opt.DerivedPassType,
                    CrossSubType     = opt.DerivedCrossSubType,
                    TargetAgentId    = opt.TargetAgentId,
                    TargetPosition   = new UnityEngine.Vector3(opt.TargetPosition.x, opt.TargetPosition.y, 0f),
                    IntendedDistance = opt.IntendedDistance,
                    Urgency          = opt.Urgency,
                    IsWeakFoot       = opt.IsWeakFoot,
                    TeamId           = 0,   // filled by ActionDispatcher from DecisionContext
                    FrameNumber      = currentFrame
                };
            }
            else if (opt.Type == ActionType.SHOOT)
            {
                shotParams = new ShotRequest
                {
                    AgentId       = agentId,
                    PowerIntent   = opt.PowerIntent,
                    ContactZone   = opt.DerivedContactZone,
                    SpinIntent    = opt.SpinIntent,
                    PlacementTarget = opt.PlacementTarget,
                    IsWeakFoot    = opt.IsWeakFoot,
                    DistanceToGoal = opt.DistanceToGoal,
                    TeamId        = 0,   // filled by ActionDispatcher
                    FrameNumber   = currentFrame
                };
            }

            return new AgentAction(
                agentId,
                opt.Type,
                opt.TargetAgentId,
                opt.TargetPosition,
                passParams,
                shotParams,
                opt.EffectiveUtility,
                currentFrame);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-05-29 | —      | Initial implementation.                                            |
// | 1.1     | 2026-05-29 | —      | AR-1 H-2: SplitMix64 wrapped in unchecked{} per FR-CS-044.        |
// | 1.2     | 2026-06-11 | —      | Audit AR-2 L: tiebreakerApplied resets on a strict improvement per |
// |         |            |        |   the §3.3 selection pseudocode (INV-SEL-07 winner semantics).     |
// | 1.3     | 2026-06-14 | —      | Audit AR-3 L-1: ComputeOptionNoise bit-fields masked to their       |
// |         |            |        |   allotted widths (5/20/3) so an out-of-range frame or agentId      |
// |         |            |        |   cannot bleed into the action-ordinal field and correlate noise.   |
// |         |            |        |   In-range layout (and determinism) unchanged; magic 5/25 promoted  |
// |         |            |        |   to named local bit-width consts.                                 |
#endregion
