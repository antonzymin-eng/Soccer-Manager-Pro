// File:     src/client-app/CareerProgressAction.cs
// Created:  2026-10-18
// Modified: 2026-10-18
// Author:   —
// Spec:     docs/tracking/interactive-unity-client-design.md §5-P7 (D-P7-3 day-granular progress),
//           Season & Competition Loop #30 §3.3/§3.4/§3.5, Code Standards #20
// Purpose:  The one progress action Career Home offers at any moment. Exactly one is legal at a time,
//           derived from #30's state by CareerProgress — the button label follows the domain state,
//           never the other way round (ux-detailed-plan §7.4).

namespace TacticalDirector.ClientApp
{
    /// <summary>
    /// The single progress action legal on Career Home right now (§5-P7 D-P7-3). <see cref="None"/> is
    /// the zero value and is never returned by <see cref="CareerProgress.NextAction"/>; it exists so a
    /// <c>default</c> view model cannot alias a real action.
    /// </summary>
    public enum CareerProgressAction
    {
        /// <summary>Zero-value sentinel. Never a legal action.</summary>
        None = 0,

        /// <summary>Advance the world one calendar day (#30 <c>AdvanceDays(1)</c>).</summary>
        AdvanceDay = 1,

        /// <summary>The clock is on the pending round's fixture day: resolve the round (#30 <c>AdvanceAndPlayNextRound()</c>).</summary>
        PlayMatchday = 2,

        /// <summary>Every round is played and the final fixture day is complete: roll the season (#30 <c>RollToNextSeason()</c>).</summary>
        StartNextSeason = 3,
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-10-18 | —      | Initial creation (§5-P7): the three progress actions + None.   |
#endregion
