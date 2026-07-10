// File:     src/tactical-instructions/BuildUpStructure.cs
// Created:  2026-07-10
// Modified: 2026-07-10
// Author:   —
// Spec:     Build-Up Structures #24 §2.2.1, Tactical Instructions #21 §2.2.1 (back-prop
//           ERR-021-006), Code Standards #20
// Purpose:  Team build-up structure dial: which scripted own/middle-third positional overlay
//           (#24 FM-BU-02) applies to the composed formation shape while in possession.

namespace TacticalDirector.TacticalInstructions
{
    /// <summary>
    /// Scripted build-up structure (#24 §2.2.1; #21-owned after back-prop ERR-021-006). 4-valued;
    /// <see cref="None"/> (zero value) is the exact identity: zero overlay offsets, so a default
    /// match is byte-identical to pre-#24 (FR-BU-005 / KD-6). The ordinal keys the
    /// <c>BuildUpOverlayCatalogue</c> tables (#24 Appendix A).
    /// ORDINAL STABILITY: byte-backed, APPEND-only (FR-BU-010); append at the end only.
    /// </summary>
    public enum BuildUpStructure : byte
    {
        /// <summary>Identity row (FR-BU-005). No overlay. #24 §3.2.</summary>
        None = 0,

        /// <summary>Fullbacks tuck beside the centre-backs; central mids drop. #24 Appendix A.1.</summary>
        BackThree = 1,

        /// <summary>Central midfield pair forms a deep pivot; central forwards drop to link. #24 Appendix A.2.</summary>
        DoublePivot = 2,

        /// <summary>Fullbacks step in and up toward the half-space. #24 Appendix A.3.</summary>
        InvertedFullBacks = 3
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                   |
// | 1.0     | 2026-07-10 | —      | Initial implementation (#24 T0 scaffolding). |
#endregion
