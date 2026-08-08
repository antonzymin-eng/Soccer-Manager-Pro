// File:     src/living-world/RelationshipLayer.cs
// Created:  2026-06-21
// Modified: 2026-08-08
// Author:   —
// Spec:     Living World System #22 §2.2.6, §3.1, Appendix C, Code Standards #20
// Purpose:  RelationshipLayer enum: identifies which layer a stored 0.0–1.0 value
//           belongs to on a RelationshipEdge.

namespace TacticalDirector.LivingWorld
{
    /// <summary>
    /// Identifies the relationship layer a stored 0.0–1.0 value belongs to. #22 §2.2.6 / Appendix C.
    ///
    /// ORDINAL STABILITY (FR-LW-028): the byte ordinals are the bit positions of the
    /// RelationshipEdge.ActiveLayers mask and are embedded in saved state. New members MUST be
    /// APPENDED — never inserted or reordered — or save compatibility and the active-layer mask break.
    ///
    /// Applicability by node-type pairing (#22 §2.2.1 / Appendix C): PlayerEdge is valid only on
    /// player↔player pairs (read-only mirror of vol-2 §2.1; never mutated here); Affinity on
    /// manager↔non-player pairs; Trust on manager↔contact pairs.
    /// </summary>
    public enum RelationshipLayer : byte
    {
        /// <summary>vol-2 §2.1 relationship strength. Read-only mirror; player↔player only.</summary>
        PlayerEdge = 0,

        /// <summary>Manager's personal relationship with an individual. Manager↔non-player only.</summary>
        Affinity = 1,

        /// <summary>Directional: will the target act on the manager's word. Manager↔contact.</summary>
        Trust = 2,
    }
}

#region VersionHistory
// | Version | Date       | Author       | Notes                                                     |
// | 1.0     | 2026-06-21 | —            | Initial file. |
// | 1.1     | 2026-08-08 | Claude Code  | Added the required #region VersionHistory block (FR-CS-058; tools/recurring-defect-lint.py hygiene pass). |
#endregion
