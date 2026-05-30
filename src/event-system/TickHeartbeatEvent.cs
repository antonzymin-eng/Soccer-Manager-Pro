// File:     src/event-system/TickHeartbeatEvent.cs
// Created:  2026-05-30
// Modified: 2026-05-30
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
        // Header bytes are NOT written for Tier C events (§2.4.2). The CLR imposes a
        // minimum struct size of 1 byte even for empty sequential structs; MemoryMarshal
        // handles this correctly (Unsafe.SizeOf<TickHeartbeatEvent>() = 1).
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                                      |
// | 1.0     | 2026-05-30 | —      | Initial implementation.                                                    |
// | 1.1     | 2026-05-30 | —      | AR-1 L-2: comment corrected (CLR minimum struct size is 1, not 0 bytes).  |
#endregion
