// File:     src/living-world/LivingWorldMath.cs
// Created:  2026-06-21
// Modified: 2026-08-08
// Author:   —
// Spec:     Living World System #22 §3.1, §3.2, §4.4, Code Standards #20
// Purpose:  Pure deterministic helpers for the living-world layer: the §3.1 layer update/decay rule
//           and the FR-LW-021 deterministic eviction-tiebreak comparator. No engine, no RNG, no I/O —
//           every function is a pure deterministic transform, unit-testable in isolation (T0).

namespace TacticalDirector.LivingWorld
{
    /// <summary>
    /// Pure, deterministic math for the living-world layer. #22 §3.1 / §3.2 / §4.4.
    /// </summary>
    public static class LivingWorldMath
    {
        /// <summary>Clamp to [0,1] without engine dependency (#22 §3.1).</summary>
        public static float Clamp01(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);

        /// <summary>
        /// §3.1 event update of an owned layer value toward its event target, clamped.
        /// x' = x + v·δ·(1 − x) for δ ≥ 0; x + v·δ·x for δ &lt; 0. Asymptotic toward [0,1] — never
        /// overshoots. Applied ONLY to owned layers (Affinity / Trust); PlayerEdge is never written
        /// here (FR-LW-004 / KD-9). Reduces to a no-op when δ = 0 (FR-LW-034).
        /// </summary>
        /// <param name="x">Current layer value in [0,1].</param>
        /// <param name="delta">Signed event impact in [−1,1].</param>
        /// <param name="volatility">Layer responsiveness v in (0,1].</param>
        public static float ApplyEvent(float x, float delta, float volatility)
        {
            float target = delta >= 0f
                ? x + volatility * delta * (1f - x)
                : x + volatility * delta * x;
            return Clamp01(target);
        }

        /// <summary>
        /// §3.1 relaxation of a layer value toward a per-entity baseline at rate r per worldTick.
        /// x' = x + r·(b − x). Reduces to a no-op when x == b (FR-LW-034).
        /// DECLARED-BUT-UNCONSUMED in production (AR-3 L-1): the per-entity baseline b is
        /// vol-2-owned state, so the WorldLoop phase-3 caller lands with the phase-2 human-systems
        /// wiring (KD-10). Exercised by T-LW-U-009/010 until then.
        /// </summary>
        public static float ApplyDecay(float x, float baseline, float rate)
            => x + rate * (baseline - x);

        /// <summary>
        /// FR-LW-021 deterministic eviction tiebreak for episodes. Returns a negative value if
        /// <paramref name="a"/> should be evicted before <paramref name="b"/>: lower salience first,
        /// ties broken by older worldTick, then lower episodeId. A total order — no container-order
        /// dependence — so eviction is replay-stable.
        /// </summary>
        public static int CompareEvictability(in MemoryEpisode a, in MemoryEpisode b)
        {
            int c = a.Salience.CompareTo(b.Salience);
            if (c != 0) return c;
            c = a.WorldTick.CompareTo(b.WorldTick);
            if (c != 0) return c;
            return a.EpisodeId.CompareTo(b.EpisodeId);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author       | Notes                                                     |
// | 1.0     | 2026-06-21 | —            | Initial file. |
// | 1.1     | 2026-07-02 | —            | Substantive edit; no version-history row was recorded for it at the time (FR-CS-058 gap, predates this hygiene pass). |
// | 1.2     | 2026-08-08 | Claude Code  | Added the required #region VersionHistory block (FR-CS-058; tools/recurring-defect-lint.py hygiene pass). |
#endregion
