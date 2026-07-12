// File:     src/match-engine/ManagerProfile.cs
// Created:  2026-07-11
// Modified: 2026-07-11
// Author:   —
// Spec:     Tactical Presets #26 §2.2.3 (FR-TP-020, F4), Appendix A.2; Code Standards #20
// Purpose:  Per-AI-managed-team manager disposition: Aggression / Caution scalars plus the
//           PatienceIntervals hold multiplier. Constructed from the Appendix A.2 archetype rows
//           or explicitly; NaN-gated at construction (F4).

using System;

using TacticalDirector.TacticalInstructions;

namespace TacticalDirector.MatchEngine
{
    /// <summary>
    /// A manager-AI disposition profile (#26 §2.2.3): <see cref="Aggression"/> scales the
    /// deficit-urgency term and <see cref="Caution"/> the lead-protection term of the §3.4
    /// adaptation ladder; <see cref="PatienceIntervals"/> multiplies the post-switch hold
    /// (FR-TP-011). Immutable; validated at construction (F4 — a non-finite or out-of-range
    /// float is refused, the project NaN-gate pattern: <c>!(x &gt;= 0f &amp;&amp; x &lt;= 1f)</c>
    /// fails for NaN).
    /// </summary>
    public readonly struct ManagerProfile
    {
        /// <summary>Deficit-urgency scale, [0, 1] (#26 §3.4).</summary>
        public float Aggression { get; }

        /// <summary>Lead-protection scale, [0, 1] (#26 §3.4).</summary>
        public float Caution { get; }

        /// <summary>Post-switch hold multiplier, ≥ 1 (FR-TP-011).</summary>
        public int PatienceIntervals { get; }

        /// <summary>Constructs a profile; refuses non-finite / out-of-range parameters (F4).</summary>
        public ManagerProfile(float aggression, float caution, int patienceIntervals)
        {
            if (!(aggression >= 0f && aggression <= 1f))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(aggression), aggression, "Aggression must be finite in [0, 1] (#26 F4).");
            }
            if (!(caution >= 0f && caution <= 1f))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(caution), caution, "Caution must be finite in [0, 1] (#26 F4).");
            }
            if (patienceIntervals < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(patienceIntervals), patienceIntervals,
                    "PatienceIntervals must be >= 1 (#26 §2.2.3).");
            }
            Aggression = aggression;
            Caution = caution;
            PatienceIntervals = patienceIntervals;
        }

        /// <summary>
        /// Builds the profile for an Appendix A.2 archetype ordinal (Aggressive 0 / Pragmatic 1 /
        /// Balanced 2). An out-of-range ordinal fails loud (the F2 restore-seam gate — a serialized
        /// <c>ProfileOrdinal</c> beyond the catalogue throws here).
        /// </summary>
        public static ManagerProfile FromArchetype(byte archetypeOrdinal)
        {
            if (archetypeOrdinal >= TacticalPresetsConstants.MANAGER_ARCHETYPE_COUNT)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(archetypeOrdinal), archetypeOrdinal,
                    "Archetype ordinal beyond the A.2 catalogue (#26 F2).");
            }
            return new ManagerProfile(
                TacticalPresetsConstants.ArchetypeAggression[archetypeOrdinal],
                TacticalPresetsConstants.ArchetypeCaution[archetypeOrdinal],
                TacticalPresetsConstants.ArchetypePatienceIntervals[archetypeOrdinal]);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                              |
// | 1.0     | 2026-07-11 | —      | Initial implementation (#26 §2.2.3). |
#endregion
