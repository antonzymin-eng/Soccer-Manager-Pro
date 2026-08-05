// File:     src/training-system/TrainingSchedule.cs
// Created:  2026-08-05
// Modified: 2026-08-05
// Author:   —
// Spec:     Training System #29 §2.2 / FR-TR-003 / FR-TR-019; Code Standards #20
// Purpose:  A read-only VIEW over a club's per-player TrainingState.Focus values. Stores no focus of
//           its own and is never serialized — focus lives only on TrainingState.Focus.

using System;

namespace TacticalDirector.TrainingSystem
{
    /// <summary>
    /// A read-only view over one club's training focuses (FR-TR-003). It borrows the caller's two
    /// parallel arrays — player ids and their states — and exposes focus by index or by player id.
    /// <para>
    /// <b>It stores no focus of its own and is NOT serialized</b> (FR-TR-019). A stored copy would be
    /// a second source of truth that drifts the first time <see cref="TrainingStep.SetFocus"/> writes
    /// one side and not the other; the view exists precisely so a caller that wants to iterate focuses
    /// does not need such a copy.
    /// </para>
    /// <para>
    /// The view is a window, not a snapshot: it reads whatever the borrowed arrays hold at call time.
    /// That is deliberate — it means a focus change is visible through an already-constructed view —
    /// but it also means the caller MUST NOT resize the arrays while a view over them is alive.
    /// </para>
    /// </summary>
    public readonly struct TrainingSchedule
    {
        private readonly int[] _playerIds;
        private readonly TrainingState[] _states;

        /// <summary>Opens a view over a club's parallel player-id / training-state arrays.</summary>
        /// <param name="playerIds">The club's player ids, in the club's deterministic roster order.</param>
        /// <param name="states">The matching training states — index <c>i</c> is the state of <paramref name="playerIds"/>[i].</param>
        /// <exception cref="ArgumentNullException">Either array is null.</exception>
        /// <exception cref="ArgumentException">The arrays have different lengths — a mismatch is a roster-lifecycle bug (FR-TR-025), not something to iterate to the shorter length.</exception>
        public TrainingSchedule(int[] playerIds, TrainingState[] states)
        {
            if (playerIds == null)
            {
                throw new ArgumentNullException(nameof(playerIds));
            }

            if (states == null)
            {
                throw new ArgumentNullException(nameof(states));
            }

            if (playerIds.Length != states.Length)
            {
                throw new ArgumentException(
                    "playerIds and states must be parallel; a length mismatch is a roster-lifecycle bug (FR-TR-025).",
                    nameof(states));
            }

            _playerIds = playerIds;
            _states = states;
        }

        /// <summary>The number of players in the view. Zero for a <c>default</c> view.</summary>
        public int Count => _playerIds == null ? 0 : _playerIds.Length;

        /// <summary>The player id at <paramref name="index"/>.</summary>
        /// <param name="index">A zero-based index into the club's roster order.</param>
        /// <exception cref="IndexOutOfRangeException">The index is outside <c>[0, Count)</c>.</exception>
        public int PlayerIdAt(int index) => _playerIds[index];

        /// <summary>The focus at <paramref name="index"/>, read live from the underlying state.</summary>
        /// <param name="index">A zero-based index into the club's roster order.</param>
        /// <exception cref="IndexOutOfRangeException">The index is outside <c>[0, Count)</c>.</exception>
        public TrainingFocus FocusAt(int index) => _states[index].Focus;

        /// <summary>
        /// Looks a player's focus up by id. Returns false for an unknown player rather than throwing —
        /// this is a query, and F2's refusal semantics belong to the <see cref="TrainingStep.SetFocus"/>
        /// command, not to a read.
        /// </summary>
        /// <param name="playerId">The player to look up.</param>
        /// <param name="focus">The player's focus when found; <c>Balanced</c> otherwise.</param>
        public bool TryGetFocus(int playerId, out TrainingFocus focus)
        {
            int index = IndexOf(playerId);
            if (index < 0)
            {
                focus = TrainingFocus.Balanced;
                return false;
            }

            focus = _states[index].Focus;
            return true;
        }

        /// <summary>The index of <paramref name="playerId"/> in the view, or -1 when absent.</summary>
        /// <param name="playerId">The player to locate.</param>
        public int IndexOf(int playerId)
        {
            if (_playerIds == null)
            {
                return -1;
            }

            for (int i = 0; i < _playerIds.Length; i++)
            {
                if (_playerIds[i] == playerId)
                {
                    return i;
                }
            }

            return -1;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                            |
// | 1.0     | 2026-08-05 | —      | Initial implementation (#29 T0, FR-TR-003 view). |
#endregion
