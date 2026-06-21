// File:     src/tactical-instructions/TacticFormation.cs
// Created:  2026-06-21
// Modified: 2026-06-21
// Author:   —
// Spec:     Tactical Instructions #21 §2.2.4, §3.1, Code Standards #20
// Purpose:  Local formation-family analogue of #12 FormationFamily (KD-2). Exactly
//           the three #12 families today; widen alongside #12 (FR-TI-004 / §3.1).
//           This layer never references the #12 enum.

namespace TacticalDirector.TacticalInstructions
{
    /// <summary>
    /// Team formation family (FR-TI-004). Local analogue of #12 <c>FormationFamily</c>; the consumer
    /// translates it downward (§3.1). Exactly the three #12 Stage-0 families — a Stage-1 widening that
    /// adds a family with no #12 table is rejected/clamped by the consumer (F5). <see cref="F442"/>
    /// (index 0) is the <see cref="TeamTactic.Balanced"/> identity.
    /// ORDINAL STABILITY: byte-backed, APPEND-only (FR-TI-007); the ordinals match #12 FormationFamily.
    /// Append at the end only, in lock-step with #12.
    /// </summary>
    public enum TacticFormation : byte
    {
        /// <summary>4-4-2 flat → #12 FormationFamily.F442 (identity). §3.1.</summary>
        F442 = 0,

        /// <summary>4-3-3 → #12 FormationFamily.F433. §3.1.</summary>
        F433 = 1,

        /// <summary>4-2-3-1 → #12 FormationFamily.F4231. §3.1.</summary>
        F4231 = 2
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                              |
// | 1.0     | 2026-06-21 | —      | Initial implementation (T0 #21).   |
#endregion
