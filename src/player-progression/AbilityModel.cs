// File:     src/player-progression/AbilityModel.cs
// Created:  2026-07-24
// Modified: 2026-07-24
// Author:   —
// Spec:     Player Progression & Lifecycle #28 §3.1.2 / §3.2 (CA/PA model + weighted spend); Code Standards #20
// Purpose:  Pure, draw-free ability arithmetic: the derived CurrentAbility summary, the age-band
//           classifier, and the deterministic weighted attribute spend/drain. Runs on the world tick
//           (day cadence), NOT the 60 Hz hot path — plain arrays are fine here (KD-6 class).

using TacticalDirector.PlayerDatabase;

namespace TacticalDirector.PlayerProgression
{
    /// <summary>
    /// Pure ability arithmetic for #28 (§3.1.2 / §3.2). <see cref="ComputeCA"/> derives the CurrentAbility
    /// cache from the [1,20] attributes (never a second accumulator, FR-PG-003); <see cref="ClassifyAgeBand"/>
    /// maps a derived age to its growth band; <see cref="TrySpendOnePoint"/> / <see cref="DrainOnePoint"/>
    /// are the deterministic attribute mutations the daily step drives.
    /// </summary>
    public static class AbilityModel
    {
        /// <summary>The three growth bands. No separate AgeBand.cs — §4.2 keeps it here.</summary>
        public enum AgeBand
        {
            /// <summary>Age &lt; GROWTH_AGE: gains points (§4.3).</summary>
            Growth,

            /// <summary>GROWTH_AGE ≤ age ≤ DECLINE_AGE: no change (§4.3).</summary>
            Stable,

            /// <summary>Age &gt; DECLINE_AGE: loses points (§4.3).</summary>
            Decline
        }

        /// <summary>
        /// Classifies a derived age into its growth band (§4.3). Boundaries match Appendix A:
        /// Growth = <c>&lt; GROWTH_AGE</c>, Decline = <c>&gt; DECLINE_AGE</c> (so age 30 stays Stable),
        /// Stable in between — symmetric strict boundaries on both ends.
        /// </summary>
        /// <param name="ageYears">The player's derived age in whole years.</param>
        public static AgeBand ClassifyAgeBand(int ageYears)
        {
            if (ageYears < PlayerProgressionConstants.GROWTH_AGE)
            {
                return AgeBand.Growth;
            }
            if (ageYears > PlayerProgressionConstants.DECLINE_AGE)
            {
                return AgeBand.Decline;
            }
            return AgeBand.Stable;
        }

        /// <summary>
        /// The derived CurrentAbility summary (§3.2): the position-weighted mean of the 31 [1,20]
        /// attributes, mapped linearly [ATTRIBUTE_MIN, ATTRIBUTE_MAX] → [0, ABILITY_MAX] with integer
        /// floor division. Weights are <c>1 + PositionAttributeBias</c> so a position's signature
        /// attributes count more. Integer-only, so a restore recomputes it bit-exact (FR-PG-003).
        /// </summary>
        /// <param name="attrs">The player's canonical [1,20] attributes.</param>
        /// <param name="pos">The player's coarse position (indexes the bias/weight table).</param>
        public static int ComputeCA(in PlayerAttributes attrs, PlayerPosition pos)
        {
            return ComputeCAFromArray(attrs.ToArray(), pos);
        }

        // The exact weighting is a §3.2 [GT] balance detail; the shape (weight = 1 + bias, linear scale)
        // is the contract. Operates on an AttrIdx-ordered array so it can be reused during spend/drain
        // candidate evaluation without a PlayerAttributes round-trip per candidate.
        private static int ComputeCAFromArray(int[] a, PlayerPosition pos)
        {
            int[] bias = PlayerDatabaseConstants.PositionAttributeBias[(int)pos];
            long numer = 0;   // Σ weight_i * attr_i
            long sumW = 0;    // Σ weight_i
            for (int i = 0; i < AttrIdx.Count; i++)
            {
                long w = 1 + bias[i];
                numer += w * a[i];
                sumW += w;
            }
            long span = PlayerProgressionConstants.ATTRIBUTE_MAX - PlayerProgressionConstants.ATTRIBUTE_MIN;
            long scaled = (numer - sumW * PlayerProgressionConstants.ATTRIBUTE_MIN)
                          * PlayerProgressionConstants.ABILITY_MAX
                          / (sumW * span);
            return (int)scaled;
        }

        /// <summary>
        /// Raises the next attribute by the deterministic weighted order (§3.1.2): highest
        /// <c>PositionAttributeBias</c> weight first, ties by ascending <see cref="AttrIdx"/>. An
        /// attribute at ATTRIBUTE_MAX, or whose +1 raise would push the derived CA past
        /// <c>lifecycle.PotentialAbility</c>, is skipped (F1). Signature mirrors the §3.1 pseudocode's
        /// <c>(ref record, ref lifecycle)</c>.
        /// </summary>
        /// <returns><c>true</c> if a point was spent; <c>false</c> if none is raisable (caller leaves the cursor — no thrash).</returns>
        public static bool TrySpendOnePoint(ref PlayerRecord rec, ref PlayerLifecycle life)
        {
            int[] bias = PlayerDatabaseConstants.PositionAttributeBias[(int)rec.Position];
            int[] a = rec.Attributes.ToArray();
            int maxBias = MaxBias(bias);

            // Highest bias level first, ties ascending index: pick the first attribute below MAX whose
            // raise keeps CA ≤ PA.
            for (int level = maxBias; level >= 0; level--)
            {
                for (int i = 0; i < AttrIdx.Count; i++)
                {
                    if (bias[i] != level || a[i] >= PlayerProgressionConstants.ATTRIBUTE_MAX)
                    {
                        continue;
                    }
                    a[i] += 1;
                    if (ComputeCAFromArray(a, rec.Position) <= life.PotentialAbility)
                    {
                        CommitAttributes(ref rec, a);
                        return true;
                    }
                    a[i] -= 1; // overshoots PA — revert and try the next candidate
                }
            }
            return false;
        }

        /// <summary>
        /// Symmetric decline (§3.1): lowers the next attribute by the mirror order — lowest
        /// <c>PositionAttributeBias</c> weight first, ties by ascending <see cref="AttrIdx"/> — so a
        /// declining player sheds their least-emphasised attributes first. An attribute at ATTRIBUTE_MIN
        /// is skipped; a fully-drained player is a no-op (the caller's cursor still advances toward 0).
        /// </summary>
        public static void DrainOnePoint(ref PlayerRecord rec, ref PlayerLifecycle life)
        {
            int[] bias = PlayerDatabaseConstants.PositionAttributeBias[(int)rec.Position];
            int[] a = rec.Attributes.ToArray();
            int maxBias = MaxBias(bias);

            for (int level = 0; level <= maxBias; level++)
            {
                for (int i = 0; i < AttrIdx.Count; i++)
                {
                    if (bias[i] != level || a[i] <= PlayerProgressionConstants.ATTRIBUTE_MIN)
                    {
                        continue;
                    }
                    a[i] -= 1;
                    CommitAttributes(ref rec, a);
                    return;
                }
            }
        }

        private static int MaxBias(int[] bias)
        {
            int max = 0;
            for (int i = 0; i < AttrIdx.Count; i++)
            {
                if (bias[i] > max)
                {
                    max = bias[i];
                }
            }
            return max;
        }

        private static void CommitAttributes(ref PlayerRecord rec, int[] a)
        {
            PlayerAttributes attrs = rec.Attributes;
            attrs.FromArray(a);
            rec.Attributes = attrs;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-07-24 | —      | Initial implementation. |
#endregion
