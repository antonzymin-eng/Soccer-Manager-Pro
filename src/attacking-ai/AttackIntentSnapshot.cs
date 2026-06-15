// File:     src/attacking-ai/AttackIntentSnapshot.cs
// Created:  2026-05-29
// Modified: 2026-06-15
// Author:   —
// Spec:     Attacking AI #15 §2.2.6, §4.5.3, Code Standards #20
// Purpose:  Read-only projection of the per-tick AttackDirective and AttackIntent[] array.
//           Consumed by test harnesses (§5) and Stage 1 event channels (ERR-015-003/004).

using System;

namespace TacticalDirector.AttackingAI
{
    /// <summary>
    /// Read-only snapshot of one 10 Hz tick's attacking output. Zero-copy view over the
    /// internal intent buffer — does not own the memory it exposes (§2.2.6).
    /// Consumed by test harnesses and Stage 1 event channels (§4.5.3).
    /// Attacking AI #15 §2.2.6.
    /// </summary>
    public readonly struct AttackIntentSnapshot
    {
        /// <summary>Team-level directive for this tick.</summary>
        public AttackDirective Directive { get; }

        /// <summary>
        /// Per-agent intent view, bounded to the valid intent count (EntityId-ascending).
        /// <see cref="ArraySegment{T}.Count"/> is the number of valid entries — iterate that,
        /// NOT the backing array length. This is a read-only WINDOW over the orchestrator's
        /// authoritative buffer (ERR-015-010): it shares storage, so consumers MUST NOT mutate
        /// elements, and the contents change on the next Tick(). §2.2.6.
        /// (Spec §2.2.6 originally specified <c>ReadOnlySpan&lt;AttackIntent&gt;</c>, which a
        /// non-ref <c>readonly struct</c> cannot hold; <c>ArraySegment</c> is the bounded,
        /// zero-allocation equivalent.)
        /// </summary>
        public ArraySegment<AttackIntent> Intents { get; }

        /// <summary>Tick index at which this snapshot was captured.</summary>
        public int TickIndex { get; }

        /// <summary>
        /// Constructs an AttackIntentSnapshot. The view is bounded to
        /// <paramref name="intentCount"/> entries of <paramref name="intents"/>.
        /// </summary>
        public AttackIntentSnapshot(
            AttackDirective directive, AttackIntent[] intents, int intentCount, int tickIndex)
        {
            Directive = directive;
            Intents   = new ArraySegment<AttackIntent>(intents, 0, intentCount);
            TickIndex = tickIndex;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-29 | —      | Initial implementation. |
// | 1.1     | 2026-06-15 | —      | AR-5 M-1 (ERR-015-010): replaced the raw `AttackIntent[] Intents` + `IntentCount` pair (whose array Length was SQUAD_SIZE, not the valid count — the doc claimed "length IntentCount", exposing stale entries and the full mutable buffer) with a bounded `ArraySegment<AttackIntent>` view. Spec §2.2.6's `ReadOnlySpan` field is uncompilable in a non-ref readonly struct; ArraySegment is the zero-alloc bounded equivalent. |
#endregion
