# Event System Specification #17 — Section 4: Architecture & Integration

**Created:** May 13, 2026
**Last Updated:** May 13, 2026
**Version:** 0.1 (initial section-file draft from `outline-detailed.md` v1.1)
**Status:** DRAFT

> Section heading order follows `outline-detailed.md` v1.1
> §"SECTION 4" (Module Layout → Interfaces → Subscriber Registration
> → Phase Integration → File Manifest), superseding the v0.0 stub.

---

## 4.1 Module Layout (Stage 1 target shape)

The Spec #17 implementation lives at `src/event-system/` once
Stage 0 → 1 transition activates code authoring (CLAUDE.md
"No code exists yet"). The target module layout:

| File | Purpose | Owns |
|------|---------|------|
| `src/event-system/EventBus.cs` | Publish / subscribe entry points (§3.2.1, §3.2.2). | `Publish<T>` overloads, `Subscribe<T>` overloads, `SubscriptionToken`. |
| `src/event-system/EventLedger.cs` | Tier A/B ring buffer + per-tick serialisation (§3.2.3, §3.4.2, §4.4). | Ring buffer storage, `DrainTick`, `SerializeLedger`. |
| `src/event-system/CosmeticChannel.cs` | Tier C immediate-synchronous dispatch + per-tick publication-count table (§3.2.3, §3.5.3, §3.6.2). | Subscriber arrays per Tier C ordinal, publication-count `u16[256]`, deterministic drop predicate. |
| `src/event-system/EventRegistry.cs` | Appendix A registry (§2.4.2). Generated from spec text at build time (Stage 1 build step). | Ordinal → tier / producer-phase / `maxPerTick` lookup tables. |
| `src/event-system/EventConstants.cs` | §3.10 constants catalogue. Generated. | `EVENT_QUEUE_CAPACITY`, `COSMETIC_PER_TICK_PUBLICATION_BUDGET`, `MAX_EVENT_DISPATCH_DEPTH`, `EVENT_TYPE_ORDINAL_WIDTH`, `PAYLOAD_VERSION_WIDTH`, `DOMAIN_TAG_EVENT_LEDGER`, all `ERR_EVT_*` codes. |

**Per-spec event structs live with their owning spec**, not in
`src/event-system/`:

- `src/shot-mechanics/ShotExecutedEvent.cs`
- `src/heading-mechanics/HeaderExecutedEvent.cs` (Stage 1+; #10)
- `src/goalkeeper-mechanics/SaveAttemptedEvent.cs` (Stage 1+; #11)
- … etc.

This matches Spec #20 layout. Spec #17 does **NOT** own those
struct files; it owns only the registry row in Appendix A and the
publish/subscribe machinery.

## 4.2 Interface Contracts (this spec exposes)

Per CLAUDE.md "Interface Design Principle", an interface is
declared **only when both producer and consumer sides are
specified here**.

### 4.2.1 Marker interfaces

```csharp
public interface IEventA { }   // Tier A marker — authoritative state-changing
public interface IEventB { }   // Tier B marker — bounded-authoritative (Stage 5+)
public interface IEventC { }   // Tier C marker — cosmetic / observability
```

- Both sides specified:
  - **Producer:** the matching `EventBus.Publish<T>` overload
    (§3.2.1).
  - **Consumer:** the `EventLedger` dispatcher (§4.4) for IEventA;
    the same dispatcher plus #16 §3.5 `TBD-NORMATIVE` Tier-B
    tolerance application path for IEventB; the
    `CosmeticChannel` for IEventC.
- KD-3 records the rationale for keeping `IEventB` at Stage 0
  despite no Stage 0 Tier B events: tier vocabulary is normatively
  owned by #16 §1.3.1 `TBD-NORMATIVE`; omitting Tier B at Stage 0
  would silently push Stage 5+ Tier-B traffic onto Tier A paths
  and break the per-tier digest contract.

### 4.2.2 Delegate and token types

```csharp
public delegate void EventHandler<T>(in T evt) where T : struct;

public readonly struct SubscriptionToken
{
    internal readonly ushort eventTypeOrdinal;
    internal readonly ushort subscriberIndex;
}
```

- `EventHandler<T>` always takes `in T evt` (FR-EVT-019,
  FR-EVT-078). Passing by value or by `ref` is a Spec #20 lint
  failure.
- `SubscriptionToken` is opaque to subscribers; only the channel
  that issued it can resolve it. Struct (no class allocation;
  FR-EVT-073).

### 4.2.3 Stage 1+ deferred interface

```csharp
// Stage 1+ — not declared at Stage 0 (KD-12; CLAUDE.md
// "Interface Design Principle" — consumer side, the replay
// tooling, is unspecified at Stage 0).
public interface IReplayEventReader { /* deferred */ }
```

This interface is **named here for forward reference** but is
**NOT declared** in the Stage 0 codebase or in any other Stage 0
spec. It activates only when the replay-tool consumer side reaches
spec at Stage 1+ (FR-EVT-066, FR-EVT-077). Including the deferred
declaration in spec text is non-normative; it documents the
boundary so downstream specs can pre-coordinate.

### 4.2.4 Interfaces this spec intentionally does NOT declare

| Interface | Why deferred |
|-----------|--------------|
| `IEventPublisher` | Phantom — only one concrete `EventBus` exists; no second implementation is foreseen. CLAUDE.md "Interface Design Principle" forbids the speculative abstraction. |
| `ITransport` | Stage 5+ multiplayer (KD-10); no Stage 0 consumer is specified. |
| `IEventSerializer` | The `SerializeCanonical` routine (#16 §3.2.4.1 `TBD-NORMATIVE`) is the sole serializer; no second strategy. |

## 4.3 Subscriber Registration Model

### 4.3.1 Static (boot-phase) registration

```csharp
public static class EventBus
{
    // Called once from match-init, before the first `Events` phase.
    public static void RegisterStartupSubscribers(
        IBootSubscriberRegistration boot);
}
```

- Boot phase happens before the first tick (and therefore before
  the first `Events` phase).
- Per-event-type subscriber arrays are **sized at registration**
  and **never resized** for Tier A/B (FR-EVT-051).
- Registration order is deterministic given that boot code is
  itself deterministic; FR-EVT-074 relies on this.

### 4.3.2 Runtime registration (Tier C only)

```csharp
public static class CosmeticChannel
{
    public static SubscriptionToken Subscribe<T>(EventHandler<T> handler)
        where T : struct, IEventC;

    public static void Unsubscribe(SubscriptionToken token);
}
```

- Permitted at any time during match (FR-EVT-022).
- UI and VFX subsystems use this surface.
- Tier C subscriber arrays grow with a one-time pre-Stage-1
  budget; runtime growth uses a pre-allocated overflow slot count
  declared in `EventConstants.cs` at Stage 0+1 (sizing TBD with
  first measurements; tracked in §6 §6.3 follow-up).

### 4.3.3 Rejection paths

| Attempt | Result |
|---------|--------|
| `Subscribe<T>` post-init with `T : IEventA` or `T : IEventB` | `ERR_EVT_TIER_MISMATCH` (FR-EVT-021). |
| `Subscribe<T>` with handler that captures a closure | Spec #20 lint failure (FR-EVT-053). Detection: compile-time check on the handler delegate's target. |
| Authoritative gameplay code calling `CosmeticChannel.Subscribe` | Spec-review failure at Stage 0; Spec #20 lint failure at Stage 0+1 (FR-EVT-016). |

## 4.4 Phase Integration

The Spec #16 `TBD-NORMATIVE` pipeline (`Input → Intent → AI / AI_NoOp
→ Physics → Resolve → Events → Snapshot`) calls into Spec #17 at
two well-defined points:

### 4.4.1 `Events` phase entry → drain

```csharp
// Called by the Events-phase scheduler at the boundary between
// Resolve and Snapshot (#16 §3.1.2 TBD-NORMATIVE).
public static void EventBus.DrainTick();
```

`DrainTick` is responsible for:

1. Sorting the accumulated tick queue once by the FM-017-002
   sort key (§3.2.4).
2. Dispatching every Tier A/B record in canonical order to its
   registered subscribers (FR-EVT-030).
3. Handling second-order re-entrant publishes per the §3.2.5 BFS
   rule until depth or queue is exhausted (or
   `ERR_EVT_QUEUE_OVERFLOW` fires).
4. Updating the per-publish trace channel (§6.5).
5. Allocating **0 bytes** (FR-EVT-049). The sort uses a stackalloc
   scratch buffer sized to `EVENT_QUEUE_CAPACITY`.

### 4.4.2 `Snapshot` phase → serialize ledger

```csharp
// Called by the Snapshot phase to emit EventLedgerRecord bytes
// into the SnapshotPayload (#16 §3.2.3 / §3.9.2 TBD-NORMATIVE).
public static int EventBus.SerializeLedger(in Span<byte> dst);
//                                              ↑
//                  caller-provided pre-allocated buffer
```

- Returns the number of bytes written.
- Writes only Tier A and Tier B records (FR-EVT-012).
- Walks the same canonical sort order produced by `DrainTick`.
- Allocates **0 bytes** (FR-EVT-050).

### 4.4.3 Tick-boundary reset

```csharp
public static void EventBus.OnTickBoundary();
// Resets per-tick state (Tier C publication-count table; queue
// pointers; intraPhaseDrawIndex counters per producing phase).
```

- Called at the very end of `Snapshot` phase, after
  `SerializeLedger` has read the queue.
- Resets the per-tick publication-count table (FR-EVT-025).
- Resets the `intraPhaseDrawIndex` counter for each producing
  phase to zero in preparation for the next tick (§3.2.4).
- Clears the ring buffer (zero out `count` and slot pointers; slot
  payload bytes are left in place — they will be overwritten on
  next publish).

### 4.4.4 Producing-phase publish hooks

Tier A/B publishes happen **inside** producing phases (`Physics`,
`Resolve`, `AI` on stride ticks). They are not direct calls to
the dispatcher — they enqueue. The producing phase keeps its own
local `intraPhaseDrawIndex` counter (per §3.2.4 normative counter-
scope declaration). At producing-phase entry, the counter is reset
to zero by the phase scheduler.

```
foreach producingPhase in [AI/AI_NoOp, Physics, Resolve]:
    phase.IntraPhaseDrawIndex = 0
    // ... phase body runs; each Publish<T>() inside the phase
    //     increments phase.IntraPhaseDrawIndex by 1 ...

Events phase:
    EventBus.DrainTick()         // sort + dispatch
Snapshot phase:
    EventBus.SerializeLedger(...) // emit into SnapshotPayload
    EventBus.OnTickBoundary()    // reset per-tick state
```

## 4.5 File / Module Manifest

The Spec #17 contribution to `docs/tracking/file-manifest.md` will
be appended at spec approval (deferred until §9 sign-off). Stub
entries:

| Path | Status | Owning spec | Notes |
|------|--------|-------------|-------|
| `docs/specs/event-system/section-1.md` | current | #17 | §1 Purpose & Scope |
| `docs/specs/event-system/section-2.md` | current | #17 | §2 FRs & data structures |
| `docs/specs/event-system/section-3.md` | current | #17 | §3 Mechanics |
| `docs/specs/event-system/section-4.md` | current | #17 | §4 Architecture (this file) |
| `docs/specs/event-system/section-5.md` | current | #17 | §5 Test plan |
| `docs/specs/event-system/section-6.md` | current | #17 | §6 Performance & budgets |
| `docs/specs/event-system/section-7.md` | current | #17 | §7 Future extensions |
| `docs/specs/event-system/section-8.md` | current | #17 | §8 References & citation audit |
| `docs/specs/event-system/section-9-approval-checklist.md` | current | #17 | §9 Approval |
| `docs/specs/event-system/appendices.md` | current | #17 | Appendices A–E |
| `docs/specs/event-system/outline.md` | reference | #17 | High-level v1.0 outline + May 6 adversarial review |
| `docs/specs/event-system/outline-detailed.md` | reference | #17 | Detailed outline v1.1 + May 12 PASS 2 review |

The manifest update lands at the §9 IN REVIEW commit.

## 4.6 Version History

| Version | Date         | Author      | Notes                                                                 |
|---------|--------------|-------------|-----------------------------------------------------------------------|
| 0.1     | May 13, 2026 | Claude Code | Initial section-file draft from `outline-detailed.md` v1.1. Five integration entry points pinned (`DrainTick`, `SerializeLedger`, `OnTickBoundary`, `RegisterStartupSubscribers`, `CosmeticChannel.Subscribe`). Section heading order superseded the v0.0 stub. |
