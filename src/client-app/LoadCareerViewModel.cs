// File:     src/client-app/LoadCareerViewModel.cs
// Created:  2026-10-18
// Modified: 2026-10-18
// Author:   —
// Spec:     docs/tracking/interactive-unity-client-design.md §5-P7 (D-P7-2, D-10),
//           ux-experience-architecture.md §6–§7, UI / Client Framework #38 §3.1, Code Standards #20
// Purpose:  The Load Career picker's projection, plus what Main Menu needs to enable Continue: every
//           slot's summary, the latest loadable slot, and the last load failure kept visible.

using System.Collections.ObjectModel;

namespace TacticalDirector.ClientApp
{
    /// <summary>
    /// Load Career's view model (§5-P7). <see cref="CanContinue"/> is Main Menu's Continue enablement;
    /// when false its "why disabled" is "no loadable save" (ux-experience-architecture §6).
    /// <see cref="LastError"/> stays set until the next successful load or refresh, so a failed load is
    /// never transient-only (§7 rule 5).
    /// </summary>
    public readonly struct LoadCareerViewModel
    {
        /// <summary>Every slot, indexed by <see cref="CareerSaveSlot"/> ordinal.</summary>
        public readonly ReadOnlyCollection<CareerSlotSummary> Slots;

        /// <summary>True when some slot is loadable — Continue is enabled.</summary>
        public readonly bool CanContinue;

        /// <summary>The slot Continue would load (valid when <see cref="CanContinue"/>).</summary>
        public readonly CareerSaveSlot LatestSlot;

        /// <summary>The last load failure's message, or null.</summary>
        public readonly string LastError;

        /// <summary>Constructs the view model.</summary>
        public LoadCareerViewModel(
            ReadOnlyCollection<CareerSlotSummary> slots,
            bool canContinue,
            CareerSaveSlot latestSlot,
            string lastError)
        {
            Slots = slots;
            CanContinue = canContinue;
            LatestSlot = latestSlot;
            LastError = lastError;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-10-18 | —      | Initial creation (§5-P7).                                      |
#endregion
