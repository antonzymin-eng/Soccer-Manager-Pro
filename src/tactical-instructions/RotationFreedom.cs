// File:     src/tactical-instructions/RotationFreedom.cs
// Created:  2026-07-10
// Modified: 2026-07-10
// Author:   —
// Spec:     Positional Rotations #25 §2.2.1, Tactical Instructions #21 §2.2.1 (back-prop
//           ERR-021-007), Code Standards #20
// Purpose:  Team positional-rotation freedom dial: whether (and how readily) adjacent slot pairs
//           may organically exchange their formation-slot bindings mid-match (#25 FM-RO-01..02).

namespace TacticalDirector.TacticalInstructions
{
    /// <summary>
    /// Positional-rotation freedom (#25 §2.2.1; #21-owned after back-prop ERR-021-007). 3-valued;
    /// <see cref="Off"/> (zero value) disables all rotation evaluation, so a default match is
    /// byte-identical to pre-#25 (FR-RO-011 / KD-8). <see cref="Conservative"/> and
    /// <see cref="Free"/> scale the trigger advantage via
    /// <c>PositioningAIConstants.ROTATION_ADVANTAGE_SCALAR_*</c> (#25 §3.1 — Off is implemented as
    /// the dial gate, never as arithmetic).
    /// ORDINAL STABILITY: byte-backed, APPEND-only (FR-RO-012); append at the end only.
    /// </summary>
    public enum RotationFreedom : byte
    {
        /// <summary>Identity row (FR-RO-011). No rotation evaluation. #25 §3.2.</summary>
        Off = 0,

        /// <summary>Rotations require a 1.5× advantage margin. #25 §3.1.</summary>
        Conservative = 1,

        /// <summary>Rotations require the base advantage margin (1.0×). #25 §3.1.</summary>
        Free = 2
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                   |
// | 1.0     | 2026-07-10 | —      | Initial implementation (#25 T0 scaffolding). |
#endregion
