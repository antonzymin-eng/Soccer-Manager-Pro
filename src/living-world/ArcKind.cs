// File:     src/living-world/ArcKind.cs
// Created:  2026-06-21
// Modified: 2026-08-08
// Author:   —
// Spec:     Living World System #22 §2.2.4, §2.2.6, §3.4, Appendix C, Code Standards #20
// Purpose:  ArcKind enum: the emergent narrative arc types.

namespace TacticalDirector.LivingWorld
{
    /// <summary>
    /// The emergent narrative arc kinds. #22 §3.4 / Appendix C.
    ///
    /// ORDINAL STABILITY (FR-LW-028): embedded in saved Arc state — APPEND-only. The ordinal order is
    /// ALSO the deterministic evaluation order for non-entity-scoped (squad/board-level) arcs
    /// (FR-LW-017), so reordering would change spawn order, RNG draw order, and episode pinning.
    /// </summary>
    public enum ArcKind : byte
    {
        /// <summary>vol-2 §2.2 pulse divergence across cliques + §2.4 ego clash.</summary>
        DressingRoomSplit = 0,

        /// <summary>Repeated low-Affinity journalist episodes after §7.1 traps.</summary>
        MediaVendetta = 1,

        /// <summary>Results vs. the vol-3 §4.1 board-archetype patience profile.</summary>
        BoardPatienceCollapse = 2,

        /// <summary>vol-2 §2.4 ego clash on an overlapping high-reputation pair.</summary>
        WonderkidVsVeteran = 3,
    }
}

#region VersionHistory
// | Version | Date       | Author       | Notes                                                     |
// | 1.0     | 2026-06-21 | —            | Initial file. |
// | 1.1     | 2026-08-08 | Claude Code  | Added the required #region VersionHistory block (FR-CS-058; tools/recurring-defect-lint.py hygiene pass). |
#endregion
