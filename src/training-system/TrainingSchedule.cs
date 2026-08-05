// File:     src/training-system/TrainingSchedule.cs
// Created:  2026-08-05
// Modified: 2026-08-05
// Author:   —
// Spec:     Training System #29 §2.2 / FR-TR-003 / FR-TR-019 / FR-TR-023; Code Standards #20
// Purpose:  The club-scoped handle over a club's per-player TrainingState.Focus values: the reads, and
//           the one FR-TR-023 write. Stores no focus of its own and is never serialized — focus lives
//           only on TrainingState.Focus, and binding the id/state arrays once is what stops a caller
//           pairing one club's ids with another club's states.

using System;

namespace TacticalDirector.TrainingSystem
{
    /// <summary>
    /// The club-scoped handle over one club's training states (FR-TR-003). It binds the caller's two
    /// parallel arrays — player ids and their states — once, at construction, and is the only way to
    /// reach a focus: by index, by player id, or through the <see cref="TrySetFocus"/> command.
    /// <para>
    /// <b>Binding the pair once is the point.</b> Every club in a generated league has the same squad
    /// size, so an API taking the two arrays as separate arguments accepts one club's ids alongside
    /// another club's states without tripping any length check — and then resolves the player against
    /// the first and writes the second, corrupting a different club's player with no error at all.
    /// There is no call site here that can re-pair them wrongly, because there is only one pairing.
    /// </para>
    /// <para>
    /// <b>It stores no focus of its own and is NOT serialized</b> (FR-TR-019). Focus lives only on
    /// <see cref="TrainingState.Focus"/>; a stored copy would be a second source of truth that drifts
    /// the first time one side is written and not the other. Reads and the one write both go straight
    /// to the borrowed state array, so no such copy exists to drift.
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

        /// <summary>Binds a club's parallel player-id / training-state arrays into one handle.</summary>
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
        /// this is a query, and F2's refusal semantics belong to the <see cref="TrySetFocus"/>
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

        /// <summary>
        /// The weekly focus command (FR-TR-023). Writes <see cref="TrainingState.Focus"/> — the single
        /// source of truth — for one player of this club.
        /// <para>
        /// An unknown player is <b>refused</b> (returns false, no mutation): the roster is authoritative
        /// and a stale id from a UI is not a crash (F2). An out-of-contract focus <b>fails loud</b>: an
        /// undefined enum ordinal is a bug in the caller, and clamping it to Balanced would persist a
        /// silently wrong focus into the save (F4 / FR-TR-021).
        /// </para>
        /// <para>
        /// This is the one mutating member on the handle, and it lives here rather than on
        /// <see cref="TrainingStep"/> so that the ids and the states it writes are provably the pair
        /// bound at construction — see the type doc.
        /// </para>
        /// </summary>
        /// <param name="playerId">The player whose focus is being set.</param>
        /// <param name="focus">The new focus.</param>
        /// <returns>True when the focus was written; false when the player is not on this club's roster.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="focus"/> is not a defined ordinal (F4 / FR-TR-021).</exception>
        public bool TrySetFocus(int playerId, TrainingFocus focus)
        {
            if (!TrainingSystemConstants.IsDefinedFocus(focus))
            {
                throw new ArgumentOutOfRangeException(nameof(focus), focus, "Undefined TrainingFocus (F4 / FR-TR-021).");
            }

            int index = IndexOf(playerId);
            if (index < 0)
            {
                return false;   // unknown player — refused, not thrown (F2)
            }

            _states[index].Focus = focus;
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
// | 1.0     | 2026-08-05 | —      | Initial implementation (#29 T0, FR-TR-003 view).                     |
// | 1.1     | 2026-08-05 | —      | AR pass 1 (H): + TrySetFocus. The FR-TR-023 command moved here from  |
// |         |            |        | TrainingStep, whose two-array signature let one club's ids be paired |
// |         |            |        | with another club's states — same length, silent wrong-club write.   |
#endregion
