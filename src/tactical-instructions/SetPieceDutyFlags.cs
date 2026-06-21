// File:     src/tactical-instructions/SetPieceDutyFlags.cs
// Created:  2026-06-21
// Modified: 2026-06-21
// Author:   —
// Spec:     Tactical Instructions #21 §2.2.2, Code Standards #20
// Purpose:  Per-agent set-piece duty assignment flags (Stage 1+). This spec defines
//           only the duty-assignment fields; set-piece EXECUTION is out of scope (§1.1).

using System;

namespace TacticalDirector.TacticalInstructions
{
    /// <summary>
    /// Set-piece taker duties (Stage 1+). Only the duty-assignment surface is defined here; set-piece
    /// execution routines are deferred (§1.1 / §7.3). <see cref="None"/> is the
    /// <see cref="PlayerInstructions.Default"/> value.
    /// BIT-POSITION STABILITY (FR-TI-007): byte-backed [Flags]; bit positions are APPEND-only and the
    /// mask is capped at the 8-flag byte ceiling. Do not renumber existing flags.
    /// </summary>
    [Flags]
    public enum SetPieceDutyFlags : byte
    {
        /// <summary>No set-piece duty (default).</summary>
        None = 0,

        /// <summary>Designated free-kick taker.</summary>
        FreeKickTaker = 1 << 0,

        /// <summary>Designated corner taker.</summary>
        CornerTaker = 1 << 1,

        /// <summary>Designated penalty taker.</summary>
        PenaltyTaker = 1 << 2
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                              |
// | 1.0     | 2026-06-21 | —      | Initial implementation (T0 #21).   |
#endregion
