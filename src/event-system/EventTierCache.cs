// File:     src/event-system/EventTierCache.cs
// Created:  2026-06-12
// Modified: 2026-06-12
// Author:   —
// Spec:     Event System #17 §3.2.1 (ERR-017-002), Code Standards #20
// Purpose:  Per-closed-type tier-marker cache backing EventBus's single-method tier
//           dispatch. C# forbids overloading on generic constraints alone (CS0111), so
//           the §3.2.1 "three overloads" surface is implemented as one method that
//           routes on these cached flags.

using System;

namespace TacticalDirector.EventSystem
{
    /// <summary>
    /// Caches which tier marker interface (IEventA / IEventB / IEventC) the closed
    /// event type <typeparamref name="T"/> implements. The static readonly flags are
    /// initialised exactly once per closed type at type-initialisation (boot path) via
    /// <c>Type.IsAssignableFrom</c> — reflection never runs on the publish/subscribe
    /// hot path (FR-CS-034 carve-out: type-init only; the JIT folds the flags to
    /// constants in steady state). ERR-017-002.
    /// </summary>
    /// <typeparam name="T">Event struct type.</typeparam>
    internal static class EventTierCache<T> where T : struct
    {
        /// <summary>True iff <typeparamref name="T"/> carries the Tier A marker (IEventA).</summary>
        internal static readonly bool IsTierA = typeof(IEventA).IsAssignableFrom(typeof(T));

        /// <summary>True iff <typeparamref name="T"/> carries the Tier B marker (IEventB).</summary>
        internal static readonly bool IsTierB = typeof(IEventB).IsAssignableFrom(typeof(T));

        /// <summary>True iff <typeparamref name="T"/> carries the Tier C marker (IEventC).</summary>
        internal static readonly bool IsTierC = typeof(IEventC).IsAssignableFrom(typeof(T));

        /// <summary>
        /// True iff exactly one tier marker is implemented — the FR-EVT-009a contract.
        /// Zero markers (a non-event struct reached EventBus) and multiple markers
        /// (tier ambiguity) are both rejected by the EventBus entry points.
        /// </summary>
        internal static readonly bool IsValid =
            (IsTierA ? 1 : 0) + (IsTierB ? 1 : 0) + (IsTierC ? 1 : 0) == 1;
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                                |
// | 1.0     | 2026-06-12 | —      | Initial implementation (ERR-017-002): cached tier-marker flags for  |
// |         |            |        | EventBus single-method dispatch. The prior three-overload surface   |
// |         |            |        | (distinct only by IEventA/B/C constraint) was CS0111 — constraints  |
// |         |            |        | are not part of a method signature — so the assembly never compiled.|
#endregion
