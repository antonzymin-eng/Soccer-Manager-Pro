// File:     src/event-system/TickHeartbeatEvent.cs
// Created:  2026-05-30
// Modified: 2026-11-24
// Author:   —
// Spec:     Event System #17 §2.4.2, Appendix A row 0x09, Code Standards #20
// Purpose:  Tier C cosmetic event published once per tick by TickOrchestrator.
//           Ordinal 0x09; no ring-buffer entry; immediate dispatch via CosmeticChannel.
//           MaxPerTick=1 (deterministic drop predicate drops duplicates within a tick).

using System.Runtime.InteropServices;

namespace TacticalDirector.EventSystem
{
    /// <summary>
    /// Published once per physics tick by TickOrchestrator to drive cosmetic systems
    /// that need a reliable per-tick heartbeat signal.
    /// Tier C: immediate dispatch via CosmeticChannel; not included in digest (FR-EVT-011).
    /// Ordinal <c>0x09</c>. MaxPerTick=1.
    /// Event System #17 Appendix A / §2.4.2.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct TickHeartbeatEvent : IEventC
    {
        // ── No domain payload beyond the implicit tick context. ──────────────────────
        // CosmeticChannel dispatches this immediately; subscribers receive it to trigger
        // per-tick cosmetic updates (particle budgets, animation sampling, etc.).
        // Header bytes are NOT written for Tier C events (§2.4.2).
        //
        // The struct holds one reserved byte so its size is 1 on every runtime. CoreCLR gives an
        // empty struct size 1, but Unity's Mono reports 0 through MemoryMarshal.AsBytes. The
        // registry then stored struct size 0 for ordinal 0x09, IsRegistered(0x09) was false, and
        // every CosmeticChannel publish threw ERR_EVT_UNREGISTERED_ORDINAL under Unity only.
        // The byte is always 0 and carries no meaning; Tier C payloads are not serialised or digested.
#pragma warning disable CS0169 // never read: it exists only to fix the struct size
        private readonly byte _reserved;
#pragma warning restore CS0169
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                                      |
// | 1.0     | 2026-05-30 | —      | Initial implementation.                                                    |
// | 1.1     | 2026-05-30 | —      | AR-1 L-2: comment corrected (CLR minimum struct size is 1, not 0 bytes).  |
// | 1.2     | 2026-11-24 | —      | Unity Mono fix: one reserved byte so the struct size is 1 on Mono as well |
// |         |            |        | as CoreCLR. Mono measured the empty struct as 0 bytes, which left 0x09    |
// |         |            |        | unregistered and failed seven EventSystem tests in Unity only.            |
#endregion
