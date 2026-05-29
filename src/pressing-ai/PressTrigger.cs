// File:     src/pressing-ai/PressTrigger.cs
// Created:  2026-05-29
// Modified: 2026-05-29
// Author:   —
// Spec:     Pressing AI #13 §3.2, Code Standards #20
// Purpose:  Debounce state struct for the four press triggers. Holds dwell/release
//           counters as plain int fields — zero allocation, no arrays.

namespace TacticalDirector.PressingAI
{
    /// <summary>
    /// Debounce counters for the four canonical press triggers.
    /// Stored as eight explicit int fields (4 dwell + 4 release) to avoid any
    /// heap allocation on the hot path. Pressing AI #13 §3.2.
    ///
    /// Lifecycle: construct once per team at match start; mutate in place via
    /// TriggerEvaluator.UpdateDebounce() each tick.
    /// </summary>
    public struct PressTrigger
    {
        // ── BadTouch ──────────────────────────────────────────────────────────
        /// <summary>Consecutive ticks the BadTouch raw condition has been true. §3.2.</summary>
        public int BadTouchDwell;

        /// <summary>Consecutive ticks the BadTouch raw condition has been false after last dwell. §3.2.</summary>
        public int BadTouchRelease;

        // ── BackwardPass ─────────────────────────────────────────────────────
        /// <summary>Consecutive ticks the BackwardPass raw condition has been true. §3.2.</summary>
        public int BackwardPassDwell;

        /// <summary>Consecutive ticks the BackwardPass raw condition has been false after last dwell. §3.2.</summary>
        public int BackwardPassRelease;

        // ── SidelineTrap ─────────────────────────────────────────────────────
        /// <summary>Consecutive ticks the SidelineTrap raw condition has been true. §3.2.</summary>
        public int SidelineTrapDwell;

        /// <summary>Consecutive ticks the SidelineTrap raw condition has been false after last dwell. §3.2.</summary>
        public int SidelineTrapRelease;

        // ── WeakReceiver ─────────────────────────────────────────────────────
        /// <summary>Consecutive ticks the WeakReceiver raw condition has been true. §3.2.</summary>
        public int WeakReceiverDwell;

        /// <summary>Consecutive ticks the WeakReceiver raw condition has been false after last dwell. §3.2.</summary>
        public int WeakReceiverRelease;

        /// <summary>Resets all dwell and release counters to zero.</summary>
        public void Reset()
        {
            BadTouchDwell       = 0;
            BadTouchRelease     = 0;
            BackwardPassDwell   = 0;
            BackwardPassRelease = 0;
            SidelineTrapDwell   = 0;
            SidelineTrapRelease = 0;
            WeakReceiverDwell   = 0;
            WeakReceiverRelease = 0;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-29 | —      | Initial implementation. |
#endregion
