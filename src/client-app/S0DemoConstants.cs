// File:     src/client-app/S0DemoConstants.cs
// Created:  2026-10-06
// Modified: 2026-10-06
// Author:   —
// Spec:     P5b lifecycle/identity plan §§4–7, S0 binding contracts §2, Code Standards #20
// Purpose:  Frozen approved S0 demo inputs; no gameplay tuning.

namespace TacticalDirector.ClientApp
{
    /// <summary>Owner-approved fixture/setup constants from binding contracts §2.1.</summary>
    public static class S0DemoConstants
    {
        /// <summary>[FIXED] S0 repeatable demo seed.</summary>
        public const ulong SEED = 1UL;
        /// <summary>[FIXED] Approved home club identity.</summary>
        public const int HOME_CLUB_ID = 1;
        /// <summary>[FIXED] Approved away club identity.</summary>
        public const int AWAY_CLUB_ID = 2;
        /// <summary>[FIXED] Approved player count per team.</summary>
        public const int PLAYERS_PER_SQUAD = 18;
        /// <summary>[FIXED] Approved synthetic player age.</summary>
        public const int AGE = 25;
        /// <summary>[FIXED] Immutable content revision for evidence records.</summary>
        public const string FIXTURE_REVISION = "S0-approved-demo-v1";
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes |
// | 1.0     | 2026-10-06 | —      | Consumed P5b lifecycle/identity implementation. |
#endregion
