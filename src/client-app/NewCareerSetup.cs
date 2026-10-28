// File:     src/client-app/NewCareerSetup.cs
// Created:  2026-10-18
// Modified: 2026-10-18
// Author:   —
// Spec:     docs/tracking/interactive-unity-client-design.md §5-P7 (D-P7-1), roadmap C4 (league
//           bootstrap → season start → save slot), UI / Client Framework #38 §3.1, Code Standards #20
// Purpose:  The New Career screen's source and its two commands: prepare a world for a seed, select a
//           club, and start — which composes the career, autosaves it, and makes it the active career.

using System;

using TacticalDirector.SeasonSave;
using TacticalDirector.UiFramework;

namespace TacticalDirector.ClientApp
{
    /// <summary>
    /// Drives New Career (§5-P7 C4). <see cref="Prepare"/> generates the league the player chooses
    /// from; <see cref="Select"/> records the choice; <see cref="Start"/> hands both to
    /// <see cref="CareerSession.StartNew"/> and installs the result in the holder. The seed is supplied
    /// by the caller — the host binding owns entropy (no wall clock or <c>System.Random</c> here).
    /// <para>
    /// Previewing generates the league once more than starting strictly needs; generation is
    /// deterministic in the seed (league-bootstrap KD-4), so the league previewed and the league started
    /// are the same clubs by construction, which the tests lock.
    /// </para>
    /// </summary>
    public sealed class NewCareerSetup : IViewModelSource<NewCareerViewModel>
    {
        private readonly CareerSessionHolder _holder;
        private readonly CareerSaveSlots _slots;
        private readonly RoundResolutionMode _mode;
        private NewCareerViewModel _view;
        private League _league;

        /// <summary>Creates the setup.</summary>
        /// <exception cref="ArgumentNullException"><paramref name="holder"/> or <paramref name="slots"/> is null.</exception>
        public NewCareerSetup(
            CareerSessionHolder holder,
            CareerSaveSlots slots,
            RoundResolutionMode mode = RoundResolutionMode.ManagedThroughEngine)
        {
            _holder = holder ?? throw new ArgumentNullException(nameof(holder));
            _slots = slots ?? throw new ArgumentNullException(nameof(slots));
            _mode = mode;
        }

        /// <summary>True once <see cref="Prepare"/> has generated a league.</summary>
        public bool IsPrepared => _league != null;

        /// <inheritdoc />
        public NewCareerViewModel Project() => _view;

        /// <summary>Generates the league for <paramref name="worldSeed"/> and clears any selection.</summary>
        public void Prepare(ulong worldSeed)
        {
            _league = LeagueBootstrap.Generate(worldSeed);
            _view = new NewCareerViewModel(worldSeed, _league.Clubs, false, 0, _league.DefaultObjective().TargetPositionOrBetter);
        }

        /// <summary>Selects <paramref name="clubId"/> as the player's club.</summary>
        /// <exception cref="InvalidOperationException">Nothing is prepared.</exception>
        /// <exception cref="ArgumentException"><paramref name="clubId"/> is not in the prepared league.</exception>
        public void Select(int clubId)
        {
            RequirePrepared();
            bool found = false;
            foreach (Club club in _league.Clubs)
            {
                if (club.ClubId == clubId)
                {
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                throw new ArgumentException("Club " + clubId + " is not in the prepared league.", nameof(clubId));
            }

            _view = new NewCareerViewModel(_view.WorldSeed, _view.Clubs, true, clubId, _view.DefaultObjectivePosition);
        }

        /// <summary>
        /// Starts the career for the prepared seed and selected club, autosaves it, installs it as the
        /// active career, and returns it. The caller then moves <c>StartCareer</c> on the flow.
        /// </summary>
        /// <exception cref="InvalidOperationException">Nothing is prepared or no club is selected.</exception>
        public CareerSession Start()
        {
            RequirePrepared();
            if (!_view.HasSelection)
            {
                throw new InvalidOperationException("Select a club before starting the career.");
            }

            CareerSession session = CareerSession.StartNew(_view.WorldSeed, _view.SelectedClubId, _slots, _mode);
            _holder.Set(session);
            return session;
        }

        private void RequirePrepared()
        {
            if (_league == null)
            {
                throw new InvalidOperationException("Prepare a world before selecting or starting.");
            }
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-10-18 | —      | Initial creation (§5-P7 / C4).                                 |
#endregion
