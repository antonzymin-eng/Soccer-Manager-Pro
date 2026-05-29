// File:     src/attacking-ai/AttackIntentSnapshot.cs
// Created:  2026-05-29
// Modified: 2026-05-29
// Author:   —
// Spec:     Attacking AI #15 §2.2.6, §4.5.3, Code Standards #20
// Purpose:  Read-only projection of the per-tick AttackDirective and AttackIntent[] array.
//           Consumed by test harnesses (§5) and Stage 1 event channels (ERR-015-003/004).

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
        public AttackDirective Directive    { get; }

        /// <summary>Per-agent intent array; length <see cref="IntentCount"/>; EntityId-ascending.</summary>
        public AttackIntent[]  Intents      { get; }

        /// <summary>Number of valid entries in <see cref="Intents"/>.</summary>
        public int             IntentCount  { get; }

        /// <summary>Tick index at which this snapshot was captured.</summary>
        public int             TickIndex    { get; }

        /// <summary>Constructs an AttackIntentSnapshot.</summary>
        public AttackIntentSnapshot(
            AttackDirective directive, AttackIntent[] intents, int intentCount, int tickIndex)
        {
            Directive   = directive;
            Intents     = intents;
            IntentCount = intentCount;
            TickIndex   = tickIndex;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-29 | —      | Initial implementation. |
#endregion
