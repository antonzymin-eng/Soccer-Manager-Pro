// File:     src/pressing-ai/PassEventRing.cs
// Created:  2026-05-29
// Modified: 2026-05-29
// Author:   —
// Spec:     Pressing AI #13 §3.1.2, §4.2, Code Standards #20
// Purpose:  Fixed-capacity ring buffer that retains the most recent pass-attempt events
//           for backward-pass trigger evaluation. Zero allocation after construction.

using TacticalDirector.PassMechanics;

namespace TacticalDirector.PressingAI
{
    /// <summary>
    /// Fixed-capacity ring buffer of <see cref="PassAttemptEvent"/> structs.
    /// The most recent <c>Capacity</c> events are retained; older entries are overwritten.
    /// Pressing AI #13 §3.1.2.
    ///
    /// Lifecycle: allocate once per match → call Push() on every pass event → call
    /// TryGetLatest() in TriggerEvaluator to read the most recent event.
    /// All operations are zero allocation.
    /// </summary>
    public sealed class PassEventRing
    {
        private readonly PassAttemptEvent[] _buffer;
        private int _head;   // index of the next write slot
        private int _count;  // number of valid entries

        /// <summary>Maximum number of events retained.</summary>
        public int Capacity => _buffer.Length;

        /// <summary>Number of valid events currently stored (≤ Capacity).</summary>
        public int Count => _count;

        /// <summary>Allocates a ring buffer with the given capacity.</summary>
        /// <param name="capacity">Maximum events retained. Must be ≥ 1.</param>
        public PassEventRing(int capacity)
        {
            _buffer = new PassAttemptEvent[capacity];
            _head   = 0;
            _count  = 0;
        }

        /// <summary>Pushes a new event into the ring, overwriting the oldest when full.</summary>
        /// <param name="evt">The pass attempt event to store.</param>
        public void Push(PassAttemptEvent evt)
        {
            _buffer[_head] = evt;
            _head = (_head + 1) % _buffer.Length;
            if (_count < _buffer.Length)
                _count++;
        }

        /// <summary>
        /// Returns the most recently pushed event.
        /// </summary>
        /// <param name="evt">Receives the most recent event if available.</param>
        /// <returns>True when at least one event has been pushed; false otherwise.</returns>
        public bool TryGetLatest(out PassAttemptEvent evt)
        {
            if (_count == 0)
            {
                evt = default;
                return false;
            }

            int latestIdx = (_head - 1 + _buffer.Length) % _buffer.Length;
            evt = _buffer[latestIdx];
            return true;
        }

        /// <summary>Clears all stored events. Call at match reset.</summary>
        public void Clear()
        {
            _head  = 0;
            _count = 0;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-29 | —      | Initial implementation. |
#endregion
