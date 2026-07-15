// File:     src/player-database/PlayerPosition.cs
// Created:  2026-07-15
// Modified: 2026-07-15
// Author:   —
// Spec:     Squad/Player Data Layer design supplement (docs/tracking/squad-player-data-design.md) KD-4
// Purpose:  Coarse squad-management position classification for roster generation/display.

namespace TacticalDirector.PlayerDatabase
{
    /// <summary>
    /// Coarse squad-management position. NOT positioning-ai's 13-value <c>RoleId</c> (granular
    /// formation-slot role) — no shared type, no cross-reference. A future T-phase may define a
    /// PlayerPosition -> RoleId mapping when squads are wired into formation seeding; not invented
    /// here (Interface Design Principle). Design doc KD-4.
    /// ORDINAL STABILITY: append-only. Ordinal indexes <see cref="PlayerDatabaseConstants.PositionAttributeBias"/>.
    /// </summary>
    public enum PlayerPosition
    {
        Goalkeeper = 0,
        Defender = 1,
        Midfielder = 2,
        Forward = 3
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                    |
// | 1.0     | 2026-07-15 | —      | Initial implementation.  |
#endregion
