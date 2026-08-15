# Event System Specification #17 — Section 3: Technical Specification

**Created:** May 13, 2026
**Last Updated:** August 15, 2026, later
**Version:** 1.0.5
**Status:** DRAFT

> This section provides the **mechanics** for every FR-EVT-### named
> in §2.2. Rule statements live in §2; this section says how each rule
> is realised. Section heading order follows `outline-detailed.md`
> v1.1 §"SECTION 3" and supersedes the v0.0 stub layout.

---

## 3.1 Event Typed-Contract Mechanics (FR-EVT-001 … 016)

### 3.1.1 Struct layout enforcement

Every event type MUST satisfy the §2.4.1 skeleton. Compliance is
verified by a §5.3 contract test that walks the Appendix A registry
and reflects each struct's field order, asserting:

1. The first six fields match the header layout exactly
   (`eventTypeOrdinal: byte`, `payloadVersion: byte`,
   `_reserved: ushort`, `tick: uint`, `subsystemOrdinal: ushort`,
   `intraPhaseDrawIndex: ushort`).
2. The struct is decorated with
   `[StructLayout(LayoutKind.Sequential)]`.
3. All fields are `readonly`.
4. No reference-typed fields are present (§3.1.4).

Implementations of the §3.4.2 `SerializeCanonical` routine
themselves walk the declared field order; any deviation between the
in-memory struct and the registry row is caught by the §5.3 P1
property test (publish then subscribe → identical bytes).

### 3.1.2 Ordinal allocation

`eventTypeOrdinal` is byte-wide (`0x00`–`0xFF`; 256 max at Stage 0).
Assignment is **monotonic** within Spec #17 and downstream-spec
appends:

- Spec #17 v1.0 seeds ordinals `0x01`–`0x0B` (Appendix A; 11 rows).
- `0x00` is reserved as "invalid / sentinel" and MUST NOT be
  allocated.
- Future specs allocate the next free ordinal at their `IN REVIEW`
  commit and update Appendix A in the same revision. Collisions are
  prevented by the single-table registry.
- Stage 5+ expansion to two-byte ordinals is reserved in §7.3 and is
  triggered per D5 (§7.5) when the registry approaches 200 rows.

### 3.1.3 Tier metadata

The tier tag lives on the **registry row** (Appendix A), not on the
struct. Tier-aware APIs (`Publish`, `Subscribe`) take the tier via
a generic constraint:

```csharp
where T : struct, IEventA   // Tier A
where T : struct, IEventB   // Tier B
where T : struct, IEventC   // Tier C
```

`IEventA`, `IEventB`, `IEventC` are empty marker interfaces declared
in §4.2. Per CLAUDE.md "Interface Design Principle", marker
interfaces are permitted because **both** sides are specified here:

- Producer side: the `EventBus.Publish<T>` overload (§3.2.1).
- Consumer side: the `EventLedger` dispatcher (§4.4) and, for
  `IEventB`, the #16 §3.5 Tier-B tolerance application path
  (`TBD-NORMATIVE`).

Why `IEventB` is not phantom: tier vocabulary is normatively owned
by #16 §1.3.1 `TBD-NORMATIVE` and omitting it at Stage 0 would force
Stage 5+ Tier-B traffic onto Tier A paths, silently breaking the
per-tier digest contract (§3.4.2). KD-3 records this rationale.

### 3.1.4 Payload-field type whitelist

| Allowed | Forbidden |
|---------|-----------|
| Integer primitives (`byte`, `sbyte`, `short`, `ushort`, `int`, `uint`, `long`, `ulong`) | `string` |
| `float` (Stage 0 baseline; Fixed64 re-verification at Stage 5+ per §7.3) | `class` / any reference type |
| `Vector3` — Stage 0 `float`-backed struct per Ball Physics #1 §1.2 / Appendix C (corner-origin); Fixed64 re-verification at Stage 5+ per §7.3 | `IList<T>` / `T[]` (reference) / `IEnumerable<T>` |
| Fixed-size struct payloads (recursively whitelist-compliant) | `UnityEngine.Object` and all derived types |
| `EntityId` per #16 §2 / #2 §2.5 (XC-002-001 EntityId no-reuse) | `decimal` (not canonical-serialization compatible) |
| Plain enums backed by allowed integer types | `Nullable<T>` (extra padding bit) |

**String-like data is represented by enum + ordinal lookup** (e.g.,
player names are `EntityId`, not strings; competition names are
enum ordinals).

### 3.1.5 Anti-patterns (rationale rows for §5.3 unit-test design)

| Anti-pattern | Why forbidden |
|--------------|---------------|
| Class-typed event (`public class FoulCommittedEvent`) | Violates KD-8 zero-allocation; class instantiation allocates on the GC heap, breaking FR-EVT-048. |
| Reference-typed payload field (e.g., `string`, `List<int>`, `Player`) | Breaks #16 §3.2.4.1 canonical serialization (no canonical bytes for managed references). |
| Tier-A event with a `Vector3` field carrying a **continuous aggregate** (e.g., team formation centroid) | Cross-platform parity hazard; classify as Tier B and use #16 §3.5 tolerance rules. |
| Two events with semantically distinct payloads sharing one ordinal | Violates FR-EVT-003 uniqueness; replay-stability breaks. |

## 3.2 Publish / Subscribe Semantics (FR-EVT-017 … 033)

### 3.2.1 Publish API surface (KD-4, KD-8)

```csharp
public static class EventBus
{
    // ERR-017-002: ONE method — NOT three constraint-only overloads. C# does
    // not permit overloading on generic constraints alone (CS0111), so the
    // originally specified triple could never compile. Tier routing happens
    // at the entry point via a per-closed-type cached marker flag
    // (EventTierCache<T>: type-initialisation reflection only; the JIT folds
    // the flags to constants — zero steady-state dispatch cost).
    public static void Publish<T>(in T evt) where T : struct;
}
```

- One method; the event's tier is resolved from its tier marker
  interface (IEventA / IEventB / IEventC) through cached
  per-closed-type flags. `T` MUST implement **exactly one** tier
  marker (FR-EVT-009a); zero or multiple markers raise a tier
  contract violation at the entry point. *(ERR-017-002: supersedes
  the v1.0 "three overloads, compiler picks the path" design, which
  was illegal C#.)*
- The Tier A / Tier B path includes a **debug-build assertion**
  that the current pipeline phase is `Events` (or, equivalently,
  that publication is happening through the same-tick draining
  path). Violation → `ERR_DS_PHASE_OWNERSHIP` (alias of
  #16 §3.6.1 `TBD-NORMATIVE`; FR-EVT-082). The assertion is
  compiled out in release builds; Spec #20 lint (Stage 0+1) catches
  misuse statically.
- The Tier C path has no phase restriction. It is permitted from
  any phase; its effects are excluded from the digest by KD-3 /
  FR-EVT-014.

### 3.2.2 Subscribe API surface

```csharp
// ERR-017-002: one method (constraint-only overloads are CS0111); tier routing
// as in §3.2.1. Tier C routes to CosmeticChannel.SubscribeFromBus (internal
// seam); the public CosmeticChannel.Subscribe keeps its IEventC constraint.
public static SubscriptionToken Subscribe<T>(EventHandler<T> handler)
    where T : struct;

public delegate void EventHandler<T>(in T evt) where T : struct;

public readonly struct SubscriptionToken { /* opaque */ }
```

Lifecycle:

- Subscriber registration for Tier A/B is permitted **only** before
  the first `Events` phase of the match (boot phase). Runtime
  registration of Tier A/B subscribers post-init raises
  `ERR_EVT_REGISTRATION_PHASE` (FR-EVT-021) — a distinct code
  from tier-marker mismatch. *(ERR-017-002 note: with the
  single-method surface, the exactly-one-marker contract
  (FR-EVT-009a) is enforced at the EventBus entry point at runtime;
  the caller-identity concern of FR-EVT-016 / FR-EVT-076 remains a
  Spec #20 lint matter — §4.3.3.)*
- Tier C subscriber runtime register / unregister is permitted
  (FR-EVT-022); UI and VFX systems use this.
- `SubscriptionToken` is a struct (no class allocation; FR-EVT-073).
  `Unsubscribe(token)` is permitted for Tier C only.

### 3.2.3 Queue mechanics

**Tier A / B:** writes enter a pre-allocated ring buffer keyed by
`(producingPhase, intraPhaseDrawIndex)`. Drain happens in the **same
tick's** `Events` phase per #16 §3.6.1 `TBD-NORMATIVE` "event
ledger" WriteSet. The ring buffer is sized at
`EVENT_QUEUE_CAPACITY` (§3.10).

**Tier C:** writes flow directly to the cosmetic channel with
**immediate synchronous dispatch** — no delivery queue, no ring
buffer. Subscribers fire on the publishing thread (single-threaded
Stage 0 runtime per #16 §3.1 `TBD-NORMATIVE`). The only Tier C
storage is the per-tick **publication-count table** (one `u16`
counter per `eventTypeOrdinal`; 256 rows; reset at tick boundary)
which feeds the §3.6.2 deterministic drop predicate. This table is
not a delivery buffer and never holds payload bytes. When the drop
predicate fires, the publish call is a no-op — subscribers are not
invoked (FR-EVT-044).

### 3.2.4 Intra-tick canonical order (KD-6 mechanics; FR-EVT-027 … 030)

**Order key (FM-017-002):**

```
EventIntraTickSortKey =
    (producingPhaseIndex,
     subsystemOrdinal,
     entityId,
     eventTypeOrdinal,
     intraPhaseDrawIndex)
```

Each component:

- `producingPhaseIndex` — index into the #16 §3.1.2
  `TBD-NORMATIVE` phase table.
- `subsystemOrdinal` — assigned per #16 §3.1.1 `TBD-NORMATIVE`
  ordering rules.
- `entityId` — ascending per #16 §3.1.1 `array<T>` ordering.
- `eventTypeOrdinal` — from Appendix A.
- `intraPhaseDrawIndex` — counter described below; parallel to
  #16 §3.2.5.1 `TBD-NORMATIVE` intra-stream draw index.

**Counter scope (normative; resolves PASS 2 finding 4).**
`intraPhaseDrawIndex` is a `ushort` counter scoped
**per-tick, per-producingPhase**:

- Reset to zero at producing-phase entry.
- Incremented monotonically on every Tier A / Tier B publish call
  within that phase regardless of producing subsystem.
- The (`producingPhaseIndex`, `subsystemOrdinal`, `entityId`,
  `eventTypeOrdinal`, `intraPhaseDrawIndex`) tuple is therefore
  unique within a tick by construction, satisfying §5 property P2
  (sort-key total order).
- Second-order publishes from inside the same-tick `Events`-phase
  dispatch (§3.2.5) reuse the **`Events`-phase** counter (itself
  fresh per tick), preserving uniqueness under BFS dispatch up to
  `MAX_EVENT_DISPATCH_DEPTH` = 8 and the FR-EVT-046a per-handler
  out-degree cap.

**Sort-tuple attribution for second-order publishes (normative).**
A Tier A/B event enqueued from inside a `DrainTick`-invoked handler
takes its FM-017-002 sort-tuple components as follows:

| Component | Value at second-order publish |
|-----------|-------------------------------|
| `producingPhaseIndex` | `phaseIndex(Events)` — the BFS layer happens inside `Events`-phase dispatch, NOT inherited from the original first-order publisher's phase. |
| `subsystemOrdinal` | The currently-executing **handler's** subsystem ordinal (not the dispatcher's, and not the originating first-order publisher's). The dispatcher reads this from the handler-registration record. |
| `entityId` | Taken from the secondary event's payload, per the unchanged §3.2.4 component definition. Handlers that aggregate over multiple entities use `EntityId.None` (sentinel; reserved per #16 §2 `TBD-NORMATIVE`); registration-order acts as the de-facto tiebreaker via the `intraPhaseDrawIndex` increment. |
| `eventTypeOrdinal` | From the secondary event's `eventTypeOrdinal` field. |
| `intraPhaseDrawIndex` | Next available index of the `Events`-phase counter (reset at `DrainTick` entry; see §4.4.1). |

Because `producingPhaseIndex = Events` for every second-order
publish, all second-order events sort AFTER all first-order events
(which have lower phase indices). Within the second-order set,
ordering resolves by `(subsystemOrdinal, entityId, eventTypeOrdinal,
intraPhaseDrawIndex)` as usual.

**Sort timing.** Sort over the accumulated tick queue is performed
**once** at `Events`-phase entry against the in-place ring buffer;
not on every publish (FR-EVT-029). The sort routine uses a
stackalloc'd scratch buffer sized to `EVENT_QUEUE_CAPACITY` to
preserve KD-8 (§6.2 allocation budget).

This ordering is the **only** permitted iteration order over Tier
A/B events within a tick; the subscriber-dispatch loop walks it
(FR-EVT-030).

### 3.2.5 Subscriber lifetime (FR-EVT-073 … 078)

- Subscribers registered before first `Events` phase; dispatched in
  registration order (FR-EVT-074; deterministic given that
  registration itself is performed by deterministic boot code).
- **No re-entrant publish blocking.** A Tier A/B handler MAY publish
  another Tier A/B event during dispatch; the new event is appended
  to the same-tick queue and dispatched after the current pass per
  §3.2.5 BFS rule (FR-EVT-075). Second-order events are processed
  in the **same** tick before phase exit. FIFO order over the
  second-order draws is preserved by `intraPhaseDrawIndex`
  incrementing on each enqueue.
- **Maximum dispatch depth** (§3.10 constant):
  `MAX_EVENT_DISPATCH_DEPTH = 8` `[GT]`. Exceeding bound →
  `ERR_EVT_QUEUE_OVERFLOW` (FR-EVT-047).
- **Maximum per-handler out-degree = 1** (normative; FR-EVT-046a /
  FR-EVT-046b). A single Tier A/B handler invocation MAY publish
  **at most one** secondary Tier A/B event during its dispatch.
  Combined with the depth bound, this makes the §6.3.2 worst-case
  ring-buffer occupancy additive (`first-order × depth`) rather
  than multiplicative (`first-order × out-degree^depth`). Tier C
  publishes from inside a Tier A/B handler are NOT counted against
  this bound (Tier C is immediate-dispatch and has its own §3.6.2
  drop predicate). The dispatcher implements the cap by maintaining
  a per-handler enqueue counter that is reset at each handler-
  invocation boundary; a second secondary publish from the same
  handler raises `ERR_EVT_QUEUE_OVERFLOW`.
- **Handler exceptions.** Tier A/B handler throwing → escalate
  (halt tick, write crash dump per #16 §3.10 failure-mode table
  `TBD-NORMATIVE`; provisional anchor for tick-fail / crash-dump
  path). Tier C handler throwing → log + suppress.

## 3.3 Tick-Rate Split (FR-EVT-034 … 040) — KD-5 mechanics

### 3.3.1 Producing-phase / cadence map

| Event type | Producing phase | Cadence | Tier | Status |
|------------|-----------------|---------|------|--------|
| `BallContactEvent` | Physics | 60 Hz | A | seeded |
| `ShotExecutedEvent` | Resolve | 60 Hz (event-driven) | A | seeded |
| `BallCrossedLineEvent` | Physics | 60 Hz | A | seeded |
| `PressTriggeredEvent` | AI | 10 Hz (stride) | A | future — populated at #13 IN REVIEW |
| `MarkAssignedEvent` | AI | 10 Hz (stride) | A | future — populated at #14 IN REVIEW |
| `PossessionChangedEvent` | Resolve | event-driven | A | seeded |
| `GoalAwardedEvent` | Resolve | event-driven | A | seeded |
| `FoulCommittedEvent` | Resolve | event-driven | A | seeded |
| `CardIssuedEvent` | Resolve | event-driven | A | seeded |
| `SubstitutionEvent` | Resolve | event-driven | A | seeded |
| `VfxImpactCue` | Resolve | event-driven | C | seeded |
| `UiNotificationCue` | Resolve | event-driven | C | seeded |
| `TickHeartbeatEvent` | `Snapshot` | 60 Hz | C | seeded |

**Status column.** `seeded` rows are present in the §2.4.2 initial
registry (11 rows). `future` rows are listed as forward-looking
examples of the AI-phase cadence model; they are **NOT** part of
the Spec #17 v1.0 registry contract and must be appended to
Appendix A by their owning specs at IN REVIEW time (§3.7.4 cross-
spec ordering).

### 3.3.2 AI-stride interaction (KD-5)

- On non-stride ticks, the `AI` phase is `AI_NoOp`
  (#16 §3.1.2 `TBD-NORMATIVE`). `AI_NoOp` MUST NOT publish Tier A
  or Tier B events (FR-EVT-037) — its WriteSet is empty per
  #16 §3.6.1 `TBD-NORMATIVE`.
- `TickHeartbeatEvent` is published by the `Snapshot` phase once
  per tick (canonical producer per Appendix A row `0x09`;
  FR-EVT-038). Because Tier C events have no phase restriction
  (§3.2.1), any phase MAY also emit a `TickHeartbeatEvent` via the
  cosmetic channel — for example, `AI_NoOp` may emit one on non-
  stride ticks as a diagnostic convenience — but this is a non-
  binding implementation choice. The canonical producer remains
  `Snapshot`, which runs every tick.
- Tier C events from any phase are out-of-band by KD-4 and do not
  contribute to the phase WriteSet or the authoritative digest.

### 3.3.3 Tick-boundary determinism

Authoritative events never cross tick boundaries on the
authoritative path (FR-EVT-039). Every queue entry is drained by
end of same-tick `Events` phase. If a handler enqueues a second-
order event, that event is dispatched in the same tick (§3.2.5).

### 3.3.4 Anti-patterns

| Anti-pattern | Why forbidden |
|--------------|---------------|
| Publishing a Tier A event from `Physics` and expecting same-phase delivery | Tier A delivery is in `Events`, never in `Physics`. The queue holds the publication; only the dispatcher runs in `Events`. |
| Cross-tick aggregation of Tier A counts on the publishing side | The ledger is the source of truth; aggregation lives in a subscriber, not a publisher (FR-EVT-040). |
| Tactical-cadence publish from a non-stride `AI_NoOp` tick | FR-EVT-037 violation; raises `ERR_DS_PHASE_OWNERSHIP`. |

## 3.4 Determinism Contracts & Digest (FR-EVT-027 … 033) — KD-6 mechanics

### 3.4.1 Citation

`#16 §3.2.2 TBD-NORMATIVE` owns the outer per-phase digest formula.
Spec #17 declares only the **inner serialisation** of the
`Events`-phase `phaseScopeFields`.

### 3.4.2 `phaseScopeFields` layout for the `Events` phase

```
PhaseScopeFields[Events] =
    SerializeCanonical(
        DOMAIN_TAG_EVENT_LEDGER ‖ EventLedgerRecord
    )
```

Where:

- `DOMAIN_TAG_EVENT_LEDGER` = `0x15` is the domain-tag entry
  allocated in #16 §3.4 v1.0.1 (May 14, 2026) as the next value
  after `DOMAIN_TAG_ENV_FP = 0x14`. Tag is `[CROSS]` — owned by
  #16's domain-tag namespace; consumed read-only here. ERR-017-001
  RESOLVED at #16 May 14, 2026 (`docs/tracking/spec-error-log.md`).
- `EventLedgerRecord` layout per §2.4.4.
- `SerializeCanonical` is the #16 §3.2.4.1 `TBD-NORMATIVE`
  routine; padding rules (e.g., `_reserved` normalization)
  follow #16 §3.2.4.1 exactly.

### 3.4.3 Formula identifiers

- **FM-017-001** `EventLedgerDigestScope` — the §3.4.2 expression
  above. Cited by §3.4 and re-cited by §3.2.4 (sort feeds the
  serialization).
- **FM-017-002** `EventIntraTickSortKey =
  (producingPhaseIndex, subsystemOrdinal, entityId,
  eventTypeOrdinal, intraPhaseDrawIndex)` — §3.2.4 sort key.

### 3.4.4 Worked example

Deferred to Appendix B (B.1 empty `Events` phase, B.2 single-event
ledger, B.3 two-event mixed-producer ledger).

### 3.4.5 Cross-spec citation guard

Parallel to Spec #19 §3.6.1 cite-precision guard: every "#16 §3.x.x"
subsection-number citation in this spec MUST be re-grepped against
the current `deterministic-sim/section-3.md` at draft time. Numbers
may have shifted across #16's adversarial passes; the
`TBD-NORMATIVE` tag survives the re-grep but the subsection number
may need an update.

**Reproducible grep pattern (normative for §9.2 Q11):**

```
grep -nE '#1[69] §[0-9.]+ ?(`TBD-NORMATIVE`|TBD-NORMATIVE)' \
    docs/specs/event-system/section-*.md \
    docs/specs/event-system/appendices.md
```

This single pattern catches both `#16 §x.x.x TBD-NORMATIVE` and
`#19 §x.x TBD-NORMATIVE` citations. After the M2 fix (PASS 1
critique), Spec #17 uses a single qualifier vocabulary
(`TBD-NORMATIVE`) and the `[TBD-CITE]` form has been retired —
the §9.2 Q11 audit grep no longer needs to OR the two patterns.

## 3.5 Zero-Allocation Hot-Loop Mechanics (FR-EVT-048 … 054) — KD-8

### 3.5.1 Ring-buffer sizing

Per-tick capacity `EVENT_QUEUE_CAPACITY = 1024` slots `[GT]`. Sized
from §6.3 worst-case publish-rate analysis (full-match 90-min sim
with BFS dispatch-depth fanout under the FR-EVT-046a per-handler
out-degree cap). Derivation: 64 first-order Tier A/B events per
tick worst case × `MAX_EVENT_DISPATCH_DEPTH` (8) = 512 BFS fanout
ceiling (additive across levels because each handler enqueues ≤ 1
secondary event); ×2 headroom = 1024. The additivity is load-
bearing — see §6.3.2.

### 3.5.2 Subscriber-list storage

Pre-allocated `EventHandler<T>[]` per event type. Capacity pinned
at startup (FR-EVT-051). Resize is a pre-Stage-1 design error.
Subscriber-list iteration uses an indexed `for` loop over the
pre-allocated array; `IEnumerable<T>`-backed iteration is banned
(see §3.5.4 wording). `foreach` over the raw `EventHandler<T>[]`
array is permitted (compiler emits indexed access without an
enumerator allocation); the indexed-`for` style is preferred for
explicitness.

### 3.5.3 Cosmetic channel storage

Tier C dispatch is immediate-synchronous per §3.2.3 (no delivery
queue). The only Tier C storage is a per-tick
**publication-count table** sized to the ordinal-namespace width:

- Fixed `256` slots (one byte-wide ordinal per row).
- Each slot holds a `u16` counter.
- Total ≈ 512 bytes — stack-allocatable per tick (FR-EVT-054).
- Counter table is reset at the start of every tick (FR-EVT-025).

The aggregate per-tick publication ceiling
`COSMETIC_PER_TICK_PUBLICATION_BUDGET = 4096` `[GT]` is a **sanity
ceiling** (sum of per-ordinal `maxPerTick` values from Appendix A;
§6.3 worst-case envelope), **NOT** a queue capacity. Tier C has
no delivery queue.

### 3.5.4 Banned APIs in publish path (cross-listed with Spec #20 §3.x)

- `new T[…]` / `new List<T>` / any class instantiation.
- `List<T>.Add` on hot-path lists.
- `foreach` over a type that implements `IEnumerable<T>` (the
  compiler emits an allocating `GetEnumerator()` call when the
  target is not a fixed-size array, `Span<T>`, or a struct
  enumerator). `foreach` over a `T[]` or `Span<T>` is permitted
  because the compiler emits indexed access without an enumerator
  allocation.
- `Action<…>` / `Func<…>` instantiated with **value-type generic
  arguments** (each invocation boxes the value-type argument).
  **Exempt:** custom struct-ref delegates declared with an `in T`
  parameter and `where T : struct` constraint — e.g.,
  `delegate void EventHandler<T>(in T evt) where T : struct` (§3.2.2)
  — these avoid boxing because the value is passed by reference and
  the generic constraint forces a struct-only argument at the call
  site. Spec #20 lint must distinguish the two cases.
- LINQ (`Select`, `Where`, `OrderBy`, …).
- `string.Format`, interpolated strings that emit `string.Format`
  calls, `string.Concat`.
- `async` / `await`.
- Reflection (`typeof(T).GetFields()`, `Activator.CreateInstance`).
- `System.Random` (use `DeterministicRngService` per #16 §3.2.5
  `TBD-NORMATIVE` if randomness is needed; never inside the
  publish path itself).

### 3.5.5 Verification

Per-event allocation budget asserted in §5.3 unit test
(`Assert.AllocatedBytes(0)` per publish call; FR-EVT-048).
Allocation tracker is the Stage 0+1 microbenchmark suite (D1; tool
pinned by Spec #18 — `NOT STARTED`).

## 3.6 Queue Overflow & No-Drop Policy (FR-EVT-041 … 047) — KD-7

### 3.6.1 Authoritative path (Tier A/B)

- Queue is sized for §6.3 worst case + ×2 headroom over the
  dispatch-depth-bounded worst case (§3.5.1).
- Overflow is a **hard fail**: `ERR_EVT_QUEUE_OVERFLOW`
  (`0x1701`) raised by `Publish<T>`. Caller is responsible for
  crash handling per #16 §3.10 failure-mode table
  (`TBD-NORMATIVE`; provisional anchor for tick-fail path).
- Overflow MUST NOT be recovered by drop on the authoritative
  path; recovery is via simulation halt and bug fix.
- Dispatch-depth overflow during second-order BFS dispatch is
  routed to the same `ERR_EVT_QUEUE_OVERFLOW` code.

### 3.6.2 Cosmetic path (Tier C)

- Per-event-type publication rate cap stored on the Appendix A
  registry row (`maxPerTick`). If exceeded, the publish call
  deterministically **drops** the event (does not record it,
  does not invoke subscribers).
- **Drop predicate (FR-EVT-043):**

  ```
  drop(tick, eventTypeOrdinal) :=
      publicationCountThisTick[eventTypeOrdinal]
          > registry.maxPerTick(eventTypeOrdinal)
  ```

- The predicate is a **pure function** of `(tick, eventTypeOrdinal,
  publicationCountThisTick)` — it does **NOT** read queue depth
  (queue depth is not part of authoritative state and is replay-
  unstable). This makes the drop decision deterministic across
  replay.
- Drop is logged to the Tier C trace channel; does NOT enter the
  ledger (FR-EVT-045).

### 3.6.3 Anti-pattern

A "soft drop" policy that reads queue depth at publish time is
explicitly forbidden. Drop predicates MUST be pure functions of
`(tick, eventTypeOrdinal, publicationCountThisTick)`.

## 3.7 Versioning, Migration, Deprecation (FR-EVT-055 … 060) — KD-9

### 3.7.1 Registry row evolution rules

| Operation | Allowed? | Mechanics |
|-----------|----------|-----------|
| Adding a payload field | Yes | Append at end of payload; bump `payloadVersion`; update Appendix A row; the previous version row is retained for replay-corpus compatibility (KD-9). |
| Field width change in place | No | Mint new `eventTypeOrdinal`; deprecate old row per §3.7.3. |
| Field removal | No | Mint new `eventTypeOrdinal`; deprecate old row. |
| Field reorder (after `APPROVED`) | No | Mint new `eventTypeOrdinal`; deprecate old row. |
| Tier change | No | Mint new `eventTypeOrdinal`; deprecate old row. |
| Producer phase change | Yes, **with #16 §3.6.1 WriteSet back-prop** | Update Appendix A `Producer phase` column; no `payloadVersion` bump. Constraint: new producer phase must still publish only Tier A/B from `Events`-phase WriteSet at drain time. **Back-prop requirement:** the #16 §3.6.1 phase WriteSet table records which phase enqueues each Tier A/B event; a change in producer phase therefore requires a coordinated back-prop into #16 (parallel to ERR-017-001 / `DOMAIN_TAG_EVENT_LEDGER` allocation). File a new `spec-error-log.md` row at the time the change is proposed and resolve it atomically with the registry-row edit. **Replay-stability note:** producer-phase changes shift FM-017-002 sort-tuple component 1 and break G1 golden against pre-change replay corpora; the old registry row is retained per KD-9 deprecation rules so that old corpora continue to deserialise, but newly-captured goldens use the updated phase. If preserving golden-byte parity is required, treat the change as forbidden in place and mint a new ordinal under V5 (tier-change-style discipline). |

### 3.7.2 Migration semantics

- **Older version on load.** Replay corpus / fixture load
  encounters `(eventTypeOrdinal, oldVersion)`. Replay parses
  successfully (Appendix A retains old version rows). Subscriber
  sees the explicit `payloadVersion` field and dispatches the
  right shape.
- **Newer version on load.** Replay corpus encounters
  `(eventTypeOrdinal, versionNewerThanCurrent)`. Hard fail:
  `ERR_EVT_VERSION_INCOMPATIBLE` (`0x1704`).

### 3.7.3 Deprecation

A deprecated ordinal is marked `DEPRECATED` in Appendix A but **not
deleted**. Producers MUST NOT publish a deprecated ordinal in new
code (FR-EVT-060); consumers MAY still subscribe for replay-corpus
compatibility.

### 3.7.4 Cross-spec ordering

An event added by a downstream spec (e.g., #10
`HeaderExecutedEvent`) is allocated its ordinal at **that spec's
`IN REVIEW` commit**. The single-table registry in Appendix A
prevents collision. Spec #17 does NOT pre-allocate ordinals for
known-future events; the downstream spec authors append the row at
the moment its rule statement reaches IN REVIEW status.

## 3.8 Edge Cases

| ID | Trigger | Behaviour | FR | Error code |
|----|---------|-----------|----|------------|
| EC-017-001 | Tier A `Publish<T>` called from a non-`Events` producing phase (raw call, bypassing queue) | Debug-build assertion fires; release path raises `ERR_DS_PHASE_OWNERSHIP` | FR-EVT-010, FR-EVT-082 | `ERR_DS_PHASE_OWNERSHIP` |
| EC-017-002 | Queue exceeds `EVENT_QUEUE_CAPACITY` during a single tick | Hard fail; halt tick | FR-EVT-041 | `ERR_EVT_QUEUE_OVERFLOW` |
| EC-017-003 | Fixture load encounters unknown `eventTypeOrdinal` | Hard fail at load | FR-EVT-080 | `ERR_EVT_ORDINAL_UNKNOWN` |
| EC-017-004 | Fixture load encounters `payloadVersion > currentRegistryVersion` | Hard fail at load | FR-EVT-081 | `ERR_EVT_VERSION_INCOMPATIBLE` |
| EC-017-005a | Subscriber registers with wrong tier marker (authoritative code subscribing to Tier C, etc.) | Compile-time rejection via `Subscribe<T>` generic constraint + Spec #20 lint; no runtime error code | FR-EVT-016, FR-EVT-076 | *(compile-time; no runtime code)* |
| EC-017-005b | Runtime Tier A/B register/unregister attempt post-boot | Registration rejected | FR-EVT-021 | `ERR_EVT_REGISTRATION_PHASE` |
| EC-017-006 | Second-order BFS dispatch depth exceeds `MAX_EVENT_DISPATCH_DEPTH` | Hard fail; halt tick | FR-EVT-046, FR-EVT-047 | `ERR_EVT_QUEUE_OVERFLOW` |

Additional rule-application notes:

- **3.8.1 Match-replay seeking.** When the replay system jumps to a
  prior snapshot, the event ledger is reconstructed from the
  per-tick `EventLedgerRecord` in `SnapshotPayload`
  (#16 §3.2.3 `TBD-NORMATIVE`). Subscribers do NOT receive replayed
  events by default; replay-aware subscribers opt in via the Stage
  1+ `IReplayEventReader` channel (FR-EVT-077).
- **3.8.2 Save mid-tick.** Forbidden by
  #16 §3.7 `TBD-NORMATIVE`
  (`LEGAL_SAVE_BOUNDARIES = { EndOfSnapshot }`). The event ledger
  is always whole at save time.
- **3.8.3 Subscriber re-entry.** A Tier A handler that publishes
  another Tier A event during dispatch is permitted; the new event
  is appended to the same-tick queue and dispatched after the
  current pass per §3.2.5 BFS rule. Maximum nesting per §3.2.5
  constant (`MAX_EVENT_DISPATCH_DEPTH = 8`).
- **3.8.4 Multi-producer same-event same-tick.** Permitted; ordering
  resolves by §3.2.4 sort key. The per-tick-per-producingPhase
  `intraPhaseDrawIndex` counter (§3.2.4) makes the sort key unique
  by construction — identical-key collisions cannot occur, so no
  registration-order tiebreaker is needed.
- **3.8.5 Empty `Events` phase.** Digest contribution is the
  canonical empty-array byte string per #16 §3.2.4.1 `TBD-NORMATIVE`
  `array<T>` rules (`00 00 00 00` for `count`). Phase digest is
  still emitted (FR-EVT-032).
- **3.8.6 Cross-tier handler attempt.** A class designed to handle
  both Tier A and Tier C streams MUST register twice with two
  different generic constraints; the dispatcher does **not**
  implicitly fan out across tiers.

## 3.9 Error Codes (cross-reference)

Full numeric values, mnemonics, and triggers live in §3.10
constants catalogue and §2.5 failure-modes table. Each error code
cites the FR-EVT-### it catches and the §3.x rule it enforces.

## 3.10 Constants Catalogue

Per CLAUDE.md "Constant Tags", every numeric and identifier
constant declared in this spec appears here with its source tag.
Constants live in their designated `.cs` constant catalogue at
implementation time (Stage 0+1).

| Constant | Value | Tag | Notes |
|----------|-------|-----|-------|
| `EVENT_QUEUE_CAPACITY` | `1024` | `[GT]` | §3.5.1 / §6.3; ring-buffer slot count per tick. Derivation: 64 × `MAX_EVENT_DISPATCH_DEPTH` (8) × 2 headroom = 1024 — additive across BFS levels because FR-EVT-046a caps per-handler out-degree at 1. |
| `COSMETIC_PER_TICK_PUBLICATION_BUDGET` | `4096` | `[GT]` | §3.5.3 / §6.3; aggregate publication ceiling, **NOT** a delivery queue (Tier C is immediate-dispatch per §3.2.3). |
| `MAX_EVENT_DISPATCH_DEPTH` | `8` | `[GT]` | §3.2.5; BFS depth bound for second-order Tier A/B dispatch. |
| `EVENT_TYPE_ORDINAL_WIDTH` | `1 byte` (`0x00`–`0xFF`) | `[GT]` | §3.1.2; design decision (not a physical constant). Stage 5+ expansion to 2 bytes reserved in §7.3 / D5 §7.5. |
| `PAYLOAD_VERSION_WIDTH` | `1 byte` (`0x00`–`0xFF`) | `[GT]` | §3.1; §3.7; design decision. |
| `CARD_KIND_YELLOW` | `0` | `[FIXED]` | Appendix A row 0x06; `CardIssuedEvent.CardKind` domain-ordinal encoding for a first (or non-promoting) caution — the wire encoding a producer (match-engine) writes and a consumer (discipline) reads, not a designer-tunable value (so `[GT]`, including the design-fixed sub-class below, is the wrong tag; `[FIXED]` per the root `CLAUDE.md` tag table). Added ERR-017-004: the encoding had no catalogue home in this spec despite #17 owning it (Appendix A: "#17 (default owner)"), so two downstream catalogues had each declared it independently, under two different tags. |
| `CARD_KIND_RED` | `1` | `[FIXED]` | Appendix A row 0x06; `CardIssuedEvent.CardKind` domain-ordinal encoding for a straight red, as `CARD_KIND_YELLOW`. ERR-017-004. |
| `CARD_KIND_SECOND_YELLOW` | `2` | `[FIXED]` | Appendix A row 0x06; `CardIssuedEvent.CardKind` domain-ordinal encoding for a second caution promoted to a dismissal — the producer emits this as ONE event, never a yellow-then-red pair, as `CARD_KIND_YELLOW`. ERR-017-004. |
| `FOUL_ORDINAL_NONE` | `0xFFFF` | `[FIXED]` | Appendix A row 0x06; `CardIssuedEvent.FoulOrdinal` sentinel for "procedural card, no associated `FoulCommittedEvent`" — widened from `0xFF` alongside the field's `byte`→`ushort` widening (AR-5 L-1, `CardIssuedEvent.cs` v1.2, 2026-06-02). Same wire-format reasoning as the `CARD_KIND_*` rows immediately above: a producer/consumer-agreed sentinel, not designer-tunable. Had no catalogue home in this spec until now (L3, reviewed-findings pass, 2026-08-15) — the sentinel existed only as prose in `CardIssuedEvent.cs`, ERR-017-004's exact defect recurring on the sibling payload field of the same event. |
| `DOMAIN_TAG_EVENT_LEDGER` | `0x15` | `[CROSS]` | §3.4.2; allocated in #16 §3.4 v1.0.1 (next value after `DOMAIN_TAG_ENV_FP = 0x14`) per ERR-017-001 RESOLVED May 14, 2026; #16 owns the namespace, #17 consumes read-only. |
| `ERR_EVT_QUEUE_OVERFLOW` | `0x1701` | `[GT]` | §2.5 / §3.6.1; error-code allocation from `0x17NN` reserved block; designer-chosen, locked at approval. |
| *(reserved slot `0x1702`)* | — | — | Tier-marker mismatch is compile-time only (FR-EVT-016, FR-EVT-076); slot recovered; no runtime code allocated. |
| `ERR_EVT_ORDINAL_UNKNOWN` | `0x1703` | `[GT]` | §2.5 / §3.7.2. |
| `ERR_EVT_VERSION_INCOMPATIBLE` | `0x1704` | `[GT]` | §2.5 / §3.7.2. |
| `ERR_EVT_REGISTRATION_PHASE` | `0x1705` | `[GT]` | §2.5 / §3.2.2; lifecycle violation (runtime register/unregister of Tier A/B subscribers post-boot). |

Notes:

- The `0x17NN` block is **reserved** for Spec #17. It MUST NOT
  collide with #16's `0x16NN` block (verified at §9.2 quality-
  checklist row).
- All `[GT]` constants have rationale recorded in §6.3 and §3.5.1 /
  §3.5.3 / §3.2.5 as applicable.
- One `[CROSS]` constant (`DOMAIN_TAG_EVENT_LEDGER = 0x15`,
  imported from #16 §3.4 v1.0.1). Originally tagged
  `[CROSS-PENDING]` per CLAUDE.md "Constant Tags" while #16 was
  `IN PROGRESS`; promoted to `[CROSS]` atomically with #16 Tier 2
  `APPROVED` on May 14, 2026 (ERR-017-001 RESOLVED).
- **`[GT]` tag sub-classes.** CLAUDE.md "Constant Tags" defines
  `[GT]` as "Designer sets value; must live in tunable config".
  Spec #17 uses `[GT]` for one sub-class the §6.3.4 re-tuning
  trigger distinguishes from the runtime-tunable set, plus a
  **wire-format carve-out (added 2026-08-15, ERR-017-005)** that
  is `[FIXED]`, not `[GT]`, at all:
  - **Runtime-tunable `[GT]`** — `EVENT_QUEUE_CAPACITY`,
    `COSMETIC_PER_TICK_PUBLICATION_BUDGET`,
    `MAX_EVENT_DISPATCH_DEPTH`. These are the standard
    designer-set sizing constants; re-tuned per §6.3.4 against
    first measurements. Declared `public static readonly` +
    `Config.GetInt(...)` in the implementing catalogue — the
    storage shape a `[GT]` constant needs to actually be
    config-bindable.
  - **Wire-format encodings — `[FIXED]`, not a `[GT]` sub-class**
    (ERR-017-004 / ERR-017-005) — `CARD_KIND_YELLOW`,
    `CARD_KIND_RED`, `CARD_KIND_SECOND_YELLOW`. A value a producer
    (match-engine) writes and a consumer (discipline) reads, where
    a change after publication is a payload-format break rather
    than a balance edit — the same consequence class as
    `EVENT_TYPE_ORDINAL_WIDTH` below, not something a designer
    tunes. Declared `public const byte`, which is structurally
    incompatible with `[GT]`'s config-loader contract (a `const`
    inlines at compile time into every consuming assembly; it
    cannot be read from `Config.GetX` at boot). `[FIXED]` is
    correct here per the root `CLAUDE.md` tag table even though the
    value is a design decision rather than a physical law: the tag
    records *how* a value may change (never, once published), not
    *why* it has the value it has. This corrects the reading below
    that reserved `[FIXED]` for physics-law-derived values only —
    see the note after the list.
  - **Design-fixed `[GT]`** — `EVENT_TYPE_ORDINAL_WIDTH`,
    `PAYLOAD_VERSION_WIDTH`, and the `ERR_EVT_*` numeric codes.
    These are designer-set **at design time** but are NOT
    runtime-tunable: changing them after publication breaks
    replay-corpus compatibility (ordinal/version widths) or
    crash-dump triage (error codes). The §6.3.4 re-tuning trigger
    does NOT apply to design-fixed `[GT]` constants; their
    rationale is recorded once (§3.10 row) and locked at
    approval.
    **Checked 2026-08-15 (ERR-017-005), not fixed here: every
    constant in this sub-class is ALSO declared `public const`**
    (`EventTypeOrdinalWidth`, `PayloadVersionWidth`,
    `ErrEvtQueueOverflow`, `ErrEvtOrdinalUnknown`,
    `ErrEvtVersionIncompatible`, `ErrEvtRegistrationPhase`,
    `ErrEvtUnregisteredOrdinal`, `ErrEvtOrdinalCollision`, all in
    `EventSystemConstants.cs`) — the identical structural shape
    that failed `CARD_KIND_*`'s `[GT]` test above at ERR-017-004.
    Recorded, not resolved, in this pass: closing the inconsistency
    means either retagging this whole sub-class `[FIXED]` (matching
    what the code already does) or converting it to
    `public static readonly` + `Config.GetX` (matching what `[GT]`
    promises), and that choice is a separate decision this note
    does not make.

  CLAUDE.md's `[FIXED]` tag was read here, until 2026-08-15, as
  reserved for physics-law-derived values — which is why the
  wire-format encodings above were first captured as a `[GT]`
  sub-class rather than tagged `[FIXED]`. That reading is corrected
  by ERR-017-004/ERR-017-005: the root tag table defines `[FIXED]`
  structurally ("Derived from physics; never tune" is the physics
  *example*, not the whole rule — a `public const` value a
  producer/consumer pair agrees on as wire format is equally
  "never tune"), not by physical origin, so a payload encoding
  meets it without introducing a new tag. The remaining two
  design-fixed-`[GT]` rows (ordinal/version width, error codes) are
  unaffected by this correction; whether they too should move is
  the open question the checked-but-not-fixed note above leaves for
  a later pass.

## 3.11 Version History

| Version | Date         | Author      | Notes                                                                 |
|---------|--------------|-------------|-----------------------------------------------------------------------|
| 0.1     | May 13, 2026 | Claude Code | Initial section-file draft from `outline-detailed.md` v1.1. FM-017-001, FM-017-002 published. EC-017-001 … 006 published. Section heading order superseded the v0.0 stub. |
| 0.2     | May 13, 2026 | Claude Code | PASS 1 critique resolution. §3.2.4 added normative second-order publish sort-tuple attribution table (M3). §3.2.5 added per-handler out-degree cap = 1 (H1). §3.7.1 producer-phase-change row now requires #16 §3.6.1 WriteSet back-prop (M6). §3.5.4 / §3.5.2 reworded foreach + Action/Func bans (L4/L5). §3.10 added new `ERR_EVT_REGISTRATION_PHASE` row (L3) and `[GT]` tag-subclass note (M8). §3.4.5 added explicit grep pattern (L9). TickHeartbeatEvent cadence row → `AI_NoOp` (H2). §3.8 EC-017-005 split into 005a/005b (L3). Replaced `[TBD-CITE]` with `TBD-NORMATIVE` at §3.2.5 / §3.6.1 (M2). Renamed `producerSubsystem` → `subsystemOrdinal` (M4). |
| 0.3     | May 13, 2026 | Claude Code | PASS 2 critique resolution. H-2-1: §3.3.1 cadence map row reverted to `Snapshot`; §3.3.2 rewritten so `Snapshot` is canonical producer and `AI_NoOp` MAY is retained as non-binding example. H-2-2: §3.2.2 "separate from ERR_EVT_TIER_MISMATCH" reworded to compile-time-only; EC-017-005a updated to compile-time/lint-only; §3.10 `ERR_EVT_TIER_MISMATCH` row replaced with reserved-slot note; `ERR_EVT_REGISTRATION_PHASE` note simplified. |
| 1.0.1   | May 15, 2026 | Claude Code | Patch revision (no behavioral change). `[CROSS-PENDING]` → `[CROSS]` promotion of `DOMAIN_TAG_EVENT_LEDGER` following #16 §3.4 v1.0.1 allocation of value `0x15` (May 14, 2026). §3.4.2 prose updated to inline the literal value and re-tag; §3.10 catalogue row updated to `0x15` / `[CROSS]`; §3.10 trailing notes prose updated. ERR-017-001 RESOLVED; this revision closes the #17-side mechanical residual. |
| 1.0.2   | June 12, 2026 | Claude Code | ERR-017-002 patch (behavioral surface unchanged at call sites). §3.2.1 / §3.2.2 Publish/Subscribe API corrected from three constraint-only overloads to ONE method with cached tier-marker dispatch — C# forbids overloading on generic constraints alone (CS0111), so the v1.0 surface could never compile; the implementation (`EventBus.cs` v1.9, `EventTierCache.cs` v1.0, `CosmeticChannel.cs` v1.9, five spec `EventBusStub.cs` files) was patched in the same commit. Exactly-one-marker contract (FR-EVT-009a) now enforced at the entry point at runtime; §3.2.2 compile-time-mismatch note re-anchored accordingly. Found by the first-ever compile of the assembly on the dotnet CI gate (tools/dotnet-ci). |
| 1.0.3   | August 15, 2026 | Claude Code | ERR-017-004 back-prop (Discipline & Suspensions #44 adversarial review round 4, M24). §3.10 gained three new catalogue rows — `CARD_KIND_YELLOW` / `CARD_KIND_RED` / `CARD_KIND_SECOND_YELLOW`, tagged `[FIXED]` — the `CardIssuedEvent.CardKind` domain-ordinal encoding already normative in Appendix A row 0x06 but never given a catalogue home. `[FIXED]`, not `[GT]`: these are not designer-tunable (the M24 fix's own first draft mistagged them `[GT]`, self-corrected before landing — a producer/consumer wire-format byte is a `[FIXED]`-per-root-`CLAUDE.md` case, and the design-fixed-`[GT]`-subclass bullet list is unchanged). `src/event-system/EventSystemConstants.cs` is the implementing catalogue (`CARD_KIND_YELLOW`/`CARD_KIND_RED`/`CARD_KIND_SECOND_YELLOW`, ALL_CAPS in a new `#region Fixed`); `src/match-engine/MatchEngineConstants.cs` and `src/discipline/DisciplineConstants.cs` mirror it `[CROSS]`/PascalCase (single-consumer routing, src/CLAUDE.md's "[CROSS] mirrors" rule) instead of each declaring the encoding independently. No behavioral change — same byte values (0/1/2) throughout; pure catalogue/documentation addition. |
| 1.0.4   | August 15, 2026, later | Claude Code | ERR-017-005 (reviewed-findings pass, M2). §3.10's "`[GT]` tag sub-classes" note reworded: the wire-format carve-out (`CARD_KIND_*`, now `[FIXED]` per ERR-017-004) is stated explicitly instead of folded silently into the "design-fixed `[GT]`" bullet it used to share a sentence with, and the closing sentence that read "CLAUDE.md's `[FIXED]` tag is reserved for physics-law-derived values" — no longer accurate once a wire-format encoding is tagged `[FIXED]` — is corrected. Also records, checked but not resolved, that the remaining design-fixed-`[GT]` rows (`EVENT_TYPE_ORDINAL_WIDTH`, `PAYLOAD_VERSION_WIDTH`, the `ERR_EVT_*` codes) are themselves declared `public const` in `EventSystemConstants.cs` — the same structural shape ERR-017-004 used to retag `CARD_KIND_*` — an open question this revision does not decide. No code change; `EventSystemConstants.cs` was already correct (v1.5). |
| 1.0.5   | August 15, 2026, later | Claude Code | Reviewed-findings pass, L3. New §3.10 row `FOUL_ORDINAL_NONE = 0xFFFF`, `[FIXED]` — `CardIssuedEvent.FoulOrdinal`'s "no associated foul" sentinel, which had no catalogue home anywhere in this spec (it existed only as prose in `CardIssuedEvent.cs`) despite being ERR-017-004's exact defect shape on the sibling payload field of the same event. Declared in `src/event-system/EventSystemConstants.cs` beside the `CARD_KIND_*` rows and mirrored `[CROSS]` in `src/match-engine/MatchEngineConstants.cs`; the one production call site (`MatchEngine.cs`'s `foulOrdinal: 0xFFFF` literal) is outside this pass's ownership and still needs repointing at the new mirror. |
