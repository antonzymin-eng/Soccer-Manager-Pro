// File:     src/event-system/VfxImpactCue.cs
// Created:  2026-05-30
// Modified: 2026-05-30
// Author:   —
// Spec:     Event System #17 §2.4.2, Appendix A row 0x0A, Code Standards #20
// Purpose:  Tier C cosmetic cue published when a physics impact should spawn a VFX.
//           Ordinal 0x0A; no ring-buffer entry; immediate dispatch via CosmeticChannel.
//           MaxPerTick=64 (deterministic drop predicate; §3.6.2).

using System.Runtime.InteropServices;

using UnityEngine;

namespace TacticalDirector.EventSystem
{
    /// <summary>
    /// Published by physics systems to request a VFX particle effect at the impact site.
    /// Tier C: immediate dispatch via CosmeticChannel; not included in digest (FR-EVT-011).
    /// Ordinal <c>0x0A</c>. MaxPerTick=64.
    /// Event System #17 Appendix A / §2.4.2.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct VfxImpactCue : IEventC
    {
        // ── Payload fields (Appendix A row 0x0A) ─────────────────────────────────────
        /// <summary>World-space position of the impact (m). Corner-origin coordinate system (#1 §1.2).</summary>
        public readonly Vector3 ImpactPoint;
        /// <summary>Impact kind ordinal (domain-specific enum; e.g., BallOnGround/BallOnPlayer/Tackle).</summary>
        public readonly byte ImpactKind;
        /// <summary>Intensity scalar [0–255]; maps to VFX system particle count / size.</summary>
        public readonly byte Intensity;

        /// <summary>
        /// Constructs a <see cref="VfxImpactCue"/>.
        /// </summary>
        public VfxImpactCue(Vector3 impactPoint, byte impactKind, byte intensity)
        {
            ImpactPoint = impactPoint;
            ImpactKind  = impactKind;
            Intensity   = intensity;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-30 | —      | Initial implementation. |
#endregion
