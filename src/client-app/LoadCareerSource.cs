// File:     src/client-app/LoadCareerSource.cs
// Created:  2026-10-18
// Modified: 2026-10-18
// Author:   —
// Spec:     docs/tracking/interactive-unity-client-design.md §5-P7 (D-P7-2, D-10),
//           ux-experience-architecture.md §7, UI / Client Framework #38 §3.1, Code Standards #20
// Purpose:  The Load Career picker's source and its commands — refresh the slot list from disk, load a
//           chosen slot, Continue the latest — installing the resumed career in the holder on success
//           and keeping the failure visible otherwise.

using System;
using System.Collections.ObjectModel;
using System.IO;

using TacticalDirector.SeasonSave;
using TacticalDirector.UiFramework;

namespace TacticalDirector.ClientApp
{
    /// <summary>
    /// Drives Load Career and Main Menu's Continue (§5-P7 D-P7-2). The slot list is read from disk on
    /// construction and on <see cref="Refresh"/> only — never per <see cref="Project"/>, which returns
    /// the cached value. A load is context-replacing (ux-experience-architecture §7 rule 4): on success
    /// the holder's career is replaced; on failure it is left untouched and the message is kept in
    /// <see cref="LoadCareerViewModel.LastError"/>.
    /// </summary>
    public sealed class LoadCareerSource : IViewModelSource<LoadCareerViewModel>
    {
        private readonly CareerSessionHolder _holder;
        private readonly CareerSaveSlots _slots;
        private readonly RoundResolutionMode _mode;
        private LoadCareerViewModel _view;

        /// <summary>Creates the source and reads the slot list once.</summary>
        /// <exception cref="ArgumentNullException"><paramref name="holder"/> or <paramref name="slots"/> is null.</exception>
        public LoadCareerSource(
            CareerSessionHolder holder,
            CareerSaveSlots slots,
            RoundResolutionMode mode = RoundResolutionMode.ManagedThroughEngine)
        {
            _holder = holder ?? throw new ArgumentNullException(nameof(holder));
            _slots = slots ?? throw new ArgumentNullException(nameof(slots));
            _mode = mode;
            Refresh();
        }

        /// <inheritdoc />
        public LoadCareerViewModel Project() => _view;

        /// <summary>Re-reads every slot from disk and clears the last error.</summary>
        public void Refresh() => Rebuild(null);

        /// <summary>
        /// Loads <paramref name="slot"/> and makes it the active career. Returns false — leaving the
        /// active career untouched and the reason in <see cref="LoadCareerViewModel.LastError"/> — when
        /// the slot is not loadable or its save fails to decode.
        /// </summary>
        public bool TryLoad(CareerSaveSlot slot) => TryResume(slot);

        /// <summary>Loads the latest loadable slot (Continue). False with a reason when there is none or it fails.</summary>
        public bool TryContinue()
        {
            if (!_slots.TryGetLatest(out CareerSlotSummary latest))
            {
                Rebuild("There is no loadable career save to continue.");
                return false;
            }

            return TryResume(latest.Slot);
        }

        private bool TryResume(CareerSaveSlot slot)
        {
            CareerSession session;
            try
            {
                session = CareerSession.Resume(_slots, slot, _mode);
            }
            catch (Exception e) when (e is InvalidOperationException || e is InvalidDataException || e is IOException
                || e is UnauthorizedAccessException || e is ArgumentException || e is FormatException)
            {
                // A context-replacing operation that failed: keep the current career, keep the reason.
                Rebuild("Could not load " + slot + ": " + e.Message);
                return false;
            }

            _holder.Set(session);
            Rebuild(null);
            return true;
        }

        private void Rebuild(string error)
        {
            CareerSlotSummary[] rows = _slots.List();
            bool canContinue = _slots.TryGetLatest(out CareerSlotSummary latest);
            _view = new LoadCareerViewModel(
                new ReadOnlyCollection<CareerSlotSummary>(rows),
                canContinue,
                canContinue ? latest.Slot : CareerSaveSlot.Autosave,
                error);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-10-18 | —      | Initial creation (§5-P7).                                      |
#endregion
