// ============================================================================
// File:     src/club-finances/ClubFinanceEntry.cs
// Created:  2026-09-04
// Modified: 2026-09-04
// Author:   Codex / Anton
// Specs:    Spec #20 §3.6.2 (style & docs governance)
//           Spec #40 FR-FN-002/020/021, §4.4 (ClubId-keyed persisted finance state)
// Purpose:  Couples one stable ClubId to its #40-owned finance value for canonical save framing.
// ============================================================================

namespace TacticalDirector.ClubFinances
{
    /// <summary>One persisted per-club finance record, keyed by stable <see cref="ClubId"/>.</summary>
    public readonly struct ClubFinanceEntry
    {
        /// <summary>Stable club identity from #27.</summary>
        public readonly int ClubId;

        /// <summary>#40-owned financial state for <see cref="ClubId"/>.</summary>
        public readonly ClubFinances Finances;

        /// <summary>Creates a ClubId-keyed finance record.</summary>
        public ClubFinanceEntry(int clubId, in ClubFinances finances)
        {
            ClubId = clubId;
            Finances = finances;
        }
    }
}

#region VersionHistory
// Version | Date       | Author        | Change
// --------|------------|---------------|----------------------------------------------
// 1.0     | 2026-09-04 | Codex / Anton | Initial #40 T1a persisted entry value.
#endregion
