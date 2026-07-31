// File:     src/training-system/TrainingSchedule.cs
// Created:  2026-07-31
// Modified: 2026-07-31
// Author:   —
// Spec:     Training System #29 §2.2 / §4.2 / FR-TR-003 / FR-TR-019; Code Standards #20
// Purpose:  The read-only VIEW over a club's per-player TrainingState.Focus values — never a stored
//           copy of focus, and never separately serialized.

using System;

namespace TacticalDirector.TrainingSystem
{
    /// <summary>
    /// A read-only view / iteration over one <see cref="ClubTrainingBlock"/>'s per-player focus values
    /// (FR-TR-003). It stores NO focus of its own and is NOT serialized (FR-TR-019).
    /// </summary>
    /// <remarks>
    /// WHY A VIEW AND NOT A TABLE. Focus is a persistent field on <see cref="TrainingState.Focus"/> —
    /// the single source of truth, changed only by <see cref="ClubTrainingBlock.SetFocus"/>. A schedule
    /// that held its own copy would be a second writeable representation of the same fact, and the two
    /// would drift the first time one was updated without the other (and a serialized copy would then
    /// restore a schedule disagreeing with the states it describes). Reading through to the block makes
    /// that class of bug unrepresentable: a <see cref="ClubTrainingBlock.SetFocus"/> is visible here
    /// immediately because there is nothing else to update.
    /// <para>
    /// Iteration follows the block's deterministic ascending-<c>PlayerId</c> order. The struct holds a
    /// reference to the live block, so it is a window, not a snapshot — treat it as valid only for as
    /// long as the block it came from.
    /// </para>
    /// </remarks>
    public readonly struct TrainingSchedule
    {
        private readonly ClubTrainingBlock _block;

        /// <summary>Initializes a view over <paramref name="block"/>. Obtained via <see cref="ClubTrainingBlock.Schedule"/>.</summary>
        /// <exception cref="ArgumentNullException"><paramref name="block"/> is null.</exception>
        public TrainingSchedule(ClubTrainingBlock block)
        {
            _block = block ?? throw new ArgumentNullException(nameof(block));
        }

        /// <summary>The club whose focus values this view reads.</summary>
        public int ClubId => Block.ClubId;

        /// <summary>The number of entries in the view (== the block's player count).</summary>
        public int Count => Block.Count;

        /// <summary>The (player, focus) pair at <paramref name="index"/> in ascending-<c>PlayerId</c> order.</summary>
        public Entry EntryAt(int index)
        {
            ClubTrainingBlock block = Block;
            return new Entry(block.PlayerIdAt(index), block.StateAt(index).Focus);
        }

        /// <summary>Reads one player's current focus through to the owning state.</summary>
        /// <exception cref="ArgumentException">No state exists for <paramref name="playerId"/> (F7).</exception>
        public TrainingFocus GetFocus(int playerId) => Block.GetState(playerId).Focus;

        /// <summary>Allocation-free iteration over the view in ascending-<c>PlayerId</c> order.</summary>
        public Enumerator GetEnumerator() => new Enumerator(Block);

        // A default-constructed TrainingSchedule has no block behind it. Refused fail-loud rather than
        // reporting an empty schedule, which would read as "this club trains nobody".
        private ClubTrainingBlock Block =>
            _block ?? throw new InvalidOperationException(
                "TrainingSchedule must be obtained from ClubTrainingBlock.Schedule; the default value has no club.");

        /// <summary>One (player, focus) pair read from the underlying state.</summary>
        public readonly struct Entry
        {
            /// <summary>Initializes the pair.</summary>
            public Entry(int playerId, TrainingFocus focus)
            {
                PlayerId = playerId;
                Focus = focus;
            }

            /// <summary>The player.</summary>
            public int PlayerId { get; }

            /// <summary>That player's persistent focus, read through to <see cref="TrainingState.Focus"/>.</summary>
            public TrainingFocus Focus { get; }
        }

        /// <summary>Struct enumerator over the view — no allocation, deterministic ascending-id order.</summary>
        public struct Enumerator
        {
            private readonly ClubTrainingBlock _block;
            private int _index;

            internal Enumerator(ClubTrainingBlock block)
            {
                _block = block;
                _index = -1;
            }

            /// <summary>The entry at the current position.</summary>
            public Entry Current => new Entry(_block.PlayerIdAt(_index), _block.StateAt(_index).Focus);

            /// <summary>Advances to the next entry.</summary>
            public bool MoveNext()
            {
                _index++;
                return _index < _block.Count;
            }
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-07-31 | —      | AR-1 H-2: the FR-TR-003 derived read-only focus view (no stored copy,
// |         |            |        | not serialized), over ClubTrainingBlock's ascending-PlayerId order. |
#endregion
