# Event System Specification #17 — Section 2: Functional Requirements & Data Structures

**Created:** May 13, 2026
**Last Updated:** May 13, 2026
**Version:** 1.0.1
**Status:** DRAFT

> Section-heading layout follows `outline-detailed.md` v1.1 §"SECTION 2"
> (Conformance Levels → FR Catalogue → Failure-to-Comply → Data
> Structures → Failure Modes), superseding the placeholder headings in
> the v0.0 stub. The CLAUDE.md 9-section template names this slot
> "Functional requirements, data structures, failure modes" — the
> heading order here is a strict superset.

---

## 2.1 Conformance Levels

This spec uses RFC 2119 keywords (MUST / MUST NOT / SHOULD / SHOULD
NOT / MAY) with the same "exception-with-sign-off" semantics
documented in Spec #20 §2.1:

- **MUST / MUST NOT** — violation blocks Stage 0 → 1 transition or
  produces a runtime hard fail (`ERR_EVT_*` family — see §2.5).
- **SHOULD / SHOULD NOT** — violation requires written exception
  filed in `docs/tracking/spec-error-log.md` with lead-developer
  sign-off.
- **MAY** — discretionary; no exception process required.

All FR rows in §2.2 carry an explicit conformance-level column.

## 2.2 Functional Requirement Catalogue

Each FR has the columns
`ID | Statement | Level | Source citation | Verification (§5.x) |
Activation stage` (KD-12).

The catalogue is organized by topical partition. Rule **mechanics**
live in §3 — this table holds the rule **statement** and the
traceability metadata.

| FR Range | Topic | Rule mechanics in |
|----------|-------|-------------------|
| FR-EVT-001 … 008 | Event typed-contract rules (struct, ordinal, version) | §3.1, §3.7 |
| FR-EVT-009 … 016 | Tier classification (Tier A / B / C; KD-3) | §3.1.3, Appendix A |
| FR-EVT-017 … 026 | Publish / subscribe semantics (KD-4, KD-6) | §3.2 |
| FR-EVT-027 … 033 | Intra-tick ordering & digest contribution (KD-6) | §3.2.4, §3.4 |
| FR-EVT-034 … 040 | Tick-rate split (10 Hz / 60 Hz; KD-5) | §3.3 |
| FR-EVT-041 … 047 | Queue overflow & no-drop policy (KD-7) | §3.4, §3.6 |
| FR-EVT-048 … 054 | Zero-allocation hot-loop policy (KD-8) | §3.5, §6.2 |
| FR-EVT-055 … 060 | Versioning / migration / deprecation (KD-9) | §3.7, Appendix A |
| FR-EVT-061 … 066 | Instrumentation / trace channels (KD-11) | §5, §6.5 |
| FR-EVT-067 … 072 | Stage 5+ "do not preclude" constraints (KD-10) | §7.3 |
| FR-EVT-073 … 078 | Subscriber-registration / lifetime semantics | §3.2.5, §4.3 |
| FR-EVT-079 … 082 | Error codes & failure modes | §3.6, §3.9 |

### 2.2.1 Detailed FR Table

| ID | Statement | Level | Source | Verification | Activation |
|----|-----------|-------|--------|--------------|------------|
| FR-EVT-001 | Every event type is declared as a `readonly struct` with the §2.4.1 header layout. | MUST | KD-8; CLAUDE.md "When Writing Code" | §5.3 unit (struct-layout reflection) | Stage 0 (registry rows authorable now) |
| FR-EVT-002 | The canonical serialized layout of each event begins with the fixed 12-byte header (`eventTypeOrdinal` 1B + `payloadVersion` 1B + `_reserved` 2B + `tick` 4B + `subsystemOrdinal` 2B + `intraPhaseDrawIndex` 2B) followed by payload fields in declaration order; the in-memory C# struct uses `[StructLayout(LayoutKind.Sequential)]` without `Pack = 1`, and §3.4.2 `SerializeCanonical` is the sole authoritative source of on-disk and digest bytes. | MUST | §2.4.1; §3.4.2 | §5.3 unit (canonical-bytes golden) | Stage 0 |
| FR-EVT-003 | `eventTypeOrdinal` values are byte-wide and globally unique across the Appendix A registry. | MUST | KD-9 | §5.3 unit (registry uniqueness scan) | Stage 0 |
| FR-EVT-004 | `eventTypeOrdinal` values are never reused after publication, even on deprecation. | MUST | KD-9 | Spec-review (Stage 0); registry-validator (Stage 0+1) | Stage 0 |
| FR-EVT-005 | `payloadVersion` is monotonically incremented when a field is appended; resets only when a new `eventTypeOrdinal` is minted. | MUST | KD-9 | §5.3 unit (P3 version-migration property) | Stage 0 |
| FR-EVT-006 | The canonical serialized layout of an event payload follows the field declaration order with no implicit padding. | MUST | §2.4.1 / §3.4.2 | §5.3 unit (canonical-bytes golden) | Stage 0+1 |
| FR-EVT-007 | Event payload fields are restricted to the §3.1.4 whitelist (integer primitives, `float`, Stage-0 `Vector3`, fixed-size struct payloads, `EntityId`). | MUST | §3.1.4 | Spec-review (Stage 0); §5.3 unit (Stage 0+1) | Stage 0 |
| FR-EVT-008 | Reference-typed payload fields are forbidden in event structs. | MUST NOT | §3.1.4; KD-8 | Spec #20 lint (Stage 0+1) | Stage 0+1 |
| FR-EVT-009 | Every event type in Appendix A carries exactly one tier tag (A, B, or C). | MUST | KD-3 | §5.3 unit (registry-row schema validator) | Stage 0 |
| FR-EVT-009a | An event struct MUST implement exactly one tier-marker interface (`IEventA` XOR `IEventB` XOR `IEventC`). Implementing two markers on a single struct is forbidden because it creates ambiguous `EventBus.Publish<T>` overload resolution and silently routes through whichever overload the compiler picks. | MUST | KD-3; §4.2.1 | §5.3 unit (registry-row marker scan via reflection); Spec #20 lint | Stage 0+1 |
| FR-EVT-010 | Tier A events MUST be published only from the `Events` phase WriteSet per #16 §3.6.1. | MUST | KD-2 / KD-4 | §5.3 unit (`ERR_DS_PHASE_OWNERSHIP` assertion at debug builds) | Stage 0+1 |
| FR-EVT-011 | Tier A payload bytes are included in the per-tick `Events`-phase digest sub-scope (FM-017-001). | MUST | KD-3 / KD-6 | §5.3 G1 golden | Stage 0+1 |
| FR-EVT-012 | Tier A records are serialized into the `EventLedgerRecord` block of `SnapshotPayload` (#16 §3.9.2 `TBD-NORMATIVE`). | MUST | KD-3 | §5.3 integration (snapshot round-trip) | Stage 0+1 |
| FR-EVT-013 | Tier B events follow Tier A inclusion rules but use #16 §3.5 Tier-B tolerance for continuous fields. | MUST | KD-3 | Stage 5+ activation (no Tier B Stage 0 events) | Stage 5+ |
| FR-EVT-014 | Tier C events are NOT included in the per-tick digest. | MUST NOT | KD-3 | §5.3 G1 golden (cosmetic delta = 0 in digest) | Stage 0+1 |
| FR-EVT-015 | Tier C events are NOT included in `SnapshotPayload`. | MUST NOT | KD-3 | §5.3 integration | Stage 0+1 |
| FR-EVT-016 | Authoritative gameplay code MUST NOT subscribe to Tier C streams. Enforcement is at compile time via the `CosmeticChannel.Subscribe<T>` generic constraint and Spec #20 lint; no runtime error code is issued (§3.2.2 / §4.3.3). | MUST NOT | KD-3 | Spec-review (Stage 0); Spec #20 lint (Stage 0+1) | Stage 0+1 |
| FR-EVT-017 | The publish API is the single overloaded surface `EventBus.Publish<T>(in T evt)` with `T : struct, IEventA/IEventB/IEventC`. | MUST | KD-4 / KD-8 | §5.3 unit | Stage 0+1 |
| FR-EVT-018 | `EventBus.Publish<T>` takes `in T evt` — never `T evt` by value, never `ref T evt`. | MUST | KD-8 | §5.3 unit; Spec #20 lint | Stage 0+1 |
| FR-EVT-019 | Subscriber registration uses `delegate void EventHandler<T>(in T evt) where T : struct;` (no closures captured). | MUST | KD-8 | §5.3 unit; Spec #20 lint | Stage 0+1 |
| FR-EVT-020 | Tier A/B subscribers MUST be registered before the first `Events` phase of the match. | MUST | §3.2.2 | §5.3 unit | Stage 0+1 |
| FR-EVT-021 | Runtime register/unregister of Tier A/B subscribers post-init MUST raise `ERR_EVT_REGISTRATION_PHASE`. The lifecycle violation (registration after boot) is distinct from tier-marker mismatch, which is compile-time only (FR-EVT-016 / FR-EVT-076). | MUST | §3.2.2 | §5.3 unit | Stage 0+1 |
| FR-EVT-022 | Tier C subscribers MAY be added or removed at runtime. | MAY | §3.2.2 | §5.3 unit | Stage 0+1 |
| FR-EVT-023 | Tier A/B writes enter the pre-allocated ring buffer; drain occurs in the same tick's `Events` phase. | MUST | §3.2.3 | §5.3 integration | Stage 0+1 |
| FR-EVT-024 | Tier C dispatch is immediate-synchronous on the publishing thread (no delivery queue). | MUST | §3.2.3 | §5.3 unit | Stage 0+1 |
| FR-EVT-025 | The cosmetic-channel publication-count table is reset at every tick boundary. | MUST | §3.2.3 / §3.5.3 | §5.3 unit | Stage 0+1 |
| FR-EVT-026 | A Tier A handler exception halts the tick and writes a crash dump (#16 §3.10 failure-mode table, `TBD-NORMATIVE`). A Tier C handler exception is logged and suppressed. | MUST | §3.2.5 | §5.3 integration | Stage 0+1 |
| FR-EVT-027 | Intra-tick canonical order is the lexicographic tuple `(producingPhaseIndex, subsystemOrdinal, entityId, eventTypeOrdinal, intraPhaseDrawIndex)` (FM-017-002). | MUST | KD-6 | §5.3 P2 property | Stage 0+1 |
| FR-EVT-028 | `intraPhaseDrawIndex` is a `ushort` counter scoped per-tick, per-producingPhase; reset to zero at producing-phase entry; incremented monotonically on every Tier A/B publish in that phase. | MUST | §3.2.4 | §5.3 unit | Stage 0+1 |
| FR-EVT-029 | Sort over the accumulated tick queue is performed once at `Events`-phase entry, not on every publish. | MUST | §3.2.4 | §5.3 unit | Stage 0+1 |
| FR-EVT-030 | Subscriber dispatch over Tier A/B events walks the FM-017-002 sort order; no other iteration order is permitted. | MUST | §3.2.4 | §5.3 unit | Stage 0+1 |
| FR-EVT-031 | The `Events`-phase digest sub-scope is `PhaseScopeFields[Events] = SerializeCanonical(DOMAIN_TAG_EVENT_LEDGER ‖ EventLedgerRecord[T])` (FM-017-001). | MUST | KD-6 / §3.4.2 | §5.3 G1 golden | Stage 0+1 |
| FR-EVT-032 | An empty `Events` phase still emits a digest with the canonical empty-array byte string (`count = 00 00 00 00`). | MUST | §3.8.5 | §5.3 unit | Stage 0+1 |
| FR-EVT-033 | Event publication MUST NOT be conditional on `System.Random`, wall-clock time, or unstable iteration order. | MUST NOT | KD-6; Spec #20 | Spec #20 lint | Stage 0+1 |
| FR-EVT-034 | Physics-cadence Tier A events (e.g., `BallContactEvent`, `BallCrossedLineEvent`) are queued during the `Physics` phase and flushed during the same tick's `Events` phase. | MUST | KD-5 | §5.3 integration | Stage 0+1 |
| FR-EVT-035 | Resolve-cadence Tier A events (e.g., `ShotExecutedEvent`, `PossessionChangedEvent`, `GoalAwardedEvent`) are queued during the `Resolve` phase and flushed during the same tick's `Events` phase. | MUST | KD-5 | §5.3 integration | Stage 0+1 |
| FR-EVT-036 | Tactical Tier A events are queued only on stride ticks (`tick % 6 == 0`) during the `AI` phase. | MUST | KD-5; #16 §3.1.2 | §5.3 integration | Stage 0+1 |
| FR-EVT-037 | `AI_NoOp` (non-stride ticks) MUST NOT publish Tier A or Tier B events. | MUST NOT | §3.3.2 | §5.3 unit | Stage 0+1 |
| FR-EVT-038 | The `Snapshot` phase publishes `TickHeartbeatEvent` (Tier C) once per tick via the cosmetic channel (canonical producer; Appendix A row `0x09`). Any phase MAY also publish `TickHeartbeatEvent` via the cosmetic channel (e.g., `AI_NoOp` on non-stride ticks as an implementation choice); such publications are non-binding relative to the canonical `Snapshot` producer (§3.3.2). | MAY | §3.3.2 | §5.3 unit | Stage 0+1 |
| FR-EVT-039 | Authoritative events MUST NOT cross tick boundaries — every queued entry is drained by end of same-tick `Events` phase. | MUST | §3.3.3 | §5.3 G1 golden | Stage 0+1 |
| FR-EVT-040 | Cross-tick aggregation of Tier A counts on the publishing side is forbidden; aggregation lives in subscribers. | MUST NOT | §3.3.4 | Spec-review (Stage 0) | Stage 0 |
| FR-EVT-041 | Tier A/B publish that would exceed `EVENT_QUEUE_CAPACITY` raises `ERR_EVT_QUEUE_OVERFLOW`. | MUST | KD-7 | §5.3 unit | Stage 0+1 |
| FR-EVT-042 | Tier A/B events MUST NOT be dropped on the authoritative path under any condition. | MUST NOT | KD-7 | §5.3 soak test | Stage 0+1 |
| FR-EVT-043 | The Tier C drop predicate is exactly `(publicationCountThisTick > registry.maxPerTick(eventTypeOrdinal))`; it MUST NOT read queue depth or any non-tick-deterministic state. | MUST | KD-7 / §3.6.2 | §5.3 unit (replay-stability) | Stage 0+1 |
| FR-EVT-044 | When a Tier C drop predicate fires, subscribers are NOT invoked; the publish call becomes a no-op. | MUST | §3.2.3 | §5.3 unit | Stage 0+1 |
| FR-EVT-045 | Tier C drops are logged to the Tier C trace channel; they do NOT enter the ledger. | MUST | §3.6.2 | §5.3 unit | Stage 0+1 |
| FR-EVT-046 | Second-order dispatch depth in a single `Events` phase MUST NOT exceed `MAX_EVENT_DISPATCH_DEPTH` (8). | MUST | §3.2.5 | §5.3 unit | Stage 0+1 |
| FR-EVT-046a | A single Tier A/B handler invocation MUST NOT publish more than one secondary Tier A/B event (per-handler out-degree cap = 1). Required so that the §6.3.2 worst-case `EVENT_QUEUE_CAPACITY` derivation remains additive (`first-order × depth`) rather than multiplicative (`first-order × out-degree^depth`). | MUST | §3.2.5; KD-7 / KD-8 | §5.3 unit (out-degree assertion in dispatch wrapper) + Spec #20 lint (per-handler enqueue counter) | Stage 0+1 |
| FR-EVT-046b | Out-degree-cap violation raises `ERR_EVT_QUEUE_OVERFLOW` (same code as depth-cap violation; both routes are bounded-BFS failures). | MUST | §3.2.5 | §5.3 unit | Stage 0+1 |
| FR-EVT-047 | Dispatch-depth overflow raises `ERR_EVT_QUEUE_OVERFLOW`. | MUST | §3.2.5 | §5.3 unit | Stage 0+1 |
| FR-EVT-048 | `EventBus.Publish<T>` allocates 0 bytes per call (verified at debug build via allocation tracker). | MUST | KD-8 / §6.2 | §5.3 unit (`Assert.AllocatedBytes(0)`) | Stage 0+1 |
| FR-EVT-049 | `EventBus.DrainTick` allocates 0 bytes per call. | MUST | KD-8 / §6.2 | §5.3 unit | Stage 0+1 |
| FR-EVT-050 | `EventBus.SerializeLedger(in Span<byte> dst)` allocates 0 bytes and writes into the caller-provided span. | MUST | KD-8 / §6.2 | §5.3 unit | Stage 0+1 |
| FR-EVT-051 | Subscriber-list storage is pre-allocated `EventHandler<T>[]` per event type with capacity pinned at startup. | MUST | KD-8 | §5.3 unit | Stage 0+1 |
| FR-EVT-052 | The publish path MUST NOT call `new T[…]`, `List<T>.Add`, LINQ, `Action<…>` / `Func<…>` instantiated with value-type generic arguments (custom struct-ref delegates with `in T` parameter and `where T : struct` are exempt per §3.5.4), `string.Format`, interpolated strings that emit `string.Format`, `async`/`await`, or reflection. | MUST NOT | KD-8 / §3.5.4; Spec #20 | Spec #20 lint | Stage 0+1 |
| FR-EVT-053 | Subscriber handlers MUST NOT capture closures (compile-time check via Spec #20 lint). | MUST NOT | KD-8 | Spec #20 lint | Stage 0+1 |
| FR-EVT-054 | The Tier C publication-count table is stack-allocatable (≤ 512 bytes, `u16[256]`). | MUST | §3.5.3 | §5.3 unit | Stage 0+1 |
| FR-EVT-055 | Adding a payload field to an existing event appends it after all current fields and bumps `payloadVersion`. | MUST | KD-9 / §3.7.1 | Registry-validator (Stage 0); §5.3 P3 (Stage 0+1) | Stage 0 |
| FR-EVT-056 | Removing a payload field from a published event is forbidden; a new `eventTypeOrdinal` MUST be minted instead. | MUST NOT | KD-9 / §3.7.1 | Registry-validator | Stage 0 |
| FR-EVT-057 | Reordering existing payload fields after an event reaches `APPROVED` is forbidden. | MUST NOT | KD-9 | Registry-validator | Stage 0 |
| FR-EVT-058 | Changing the width of an existing payload field is forbidden in place; a new `eventTypeOrdinal` MUST be minted. | MUST NOT | KD-9 | Registry-validator | Stage 0 |
| FR-EVT-059 | Changing the tier of an existing event is forbidden in place; a new `eventTypeOrdinal` MUST be minted. | MUST NOT | KD-9 | Registry-validator | Stage 0 |
| FR-EVT-060 | Deprecated ordinals are retained in Appendix A (marked `DEPRECATED`); consumers MAY subscribe for replay-corpus compatibility, producers MUST NOT publish. | MUST | §3.7.3 | Registry-validator | Stage 0 |
| FR-EVT-061 | Every Tier A publish emits ≤ 16 bytes of trace-channel output. | MUST | §6.5 | §5.3 unit | Stage 0+1 |
| FR-EVT-062 | Tier C trace output is aggregated per `eventTypeOrdinal` per tick. | MUST | §6.5 | §5.3 unit | Stage 0+1 |
| FR-EVT-063 | Trace channel names are declared in §5; format is cited from #16 §8 `TBD-NORMATIVE`. | MUST | KD-11 | §5; Stage 0+1 | Stage 0+1 |
| FR-EVT-064 | Event-system instrumentation footprint ≤ 2 MB uncompressed per match at peak event rate. | SHOULD | §6.5 | §5.3 soak | Stage 0+1 |
| FR-EVT-065 | The instrumentation cost MUST fit within #16 §8.2 `TBD-NORMATIVE` envelope; Spec #17 declares its per-publish cost but does not republish #16's budget numbers. | MUST | KD-11 | §6.5 budget audit at #16 approval | Stage 0+1 |
| FR-EVT-066 | Debug-replay hooks (`IReplayEventReader`) are Stage 1+ and MUST NOT be declared at Stage 0 (CLAUDE.md "Interface Design Principle"). | MUST NOT | §4.2 / KD-12 | Spec-review | Stage 0 |
| FR-EVT-067 | Event structs MUST be `readonly struct` with explicit field order, compatible with #16 §3.2.4.1 canonical serialization. | MUST | KD-10 | §5.3 unit | Stage 0+1 |
| FR-EVT-068 | Event payloads MUST NOT reference `UnityEngine.Object` or any engine-specific singleton. | MUST NOT | KD-10 / §3.1.4 | Spec #20 lint | Stage 0+1 |
| FR-EVT-069 | The `eventTypeOrdinal` namespace is global and single-registry (Appendix A) to provide a stable identifier space for future networked multiplexing. | MUST | KD-10 | Registry-validator | Stage 0 |
| FR-EVT-070 | Wire format design (framing, compression, ack semantics, lossy Tier C transport) is out of scope at Stage 0 and is named in §7.3 as a Stage 5+ deliverable. | MUST | KD-10 | Spec-review | Stage 0 |
| FR-EVT-071 | Two-byte ordinal expansion is reserved at §7.3 and is triggered when the registry approaches 200 rows (D5 in §7.5). | SHOULD | KD-10 / §7.3 | Tracking review at registry-row 200 | Stage 1+ |
| FR-EVT-072 | Fixed64 (Spec #9) re-verification of event payload arithmetic fields is a Stage 5+ deliverable (§7.3). | MUST | KD-10 / §7.3 | Spec #9 re-verification suite | Stage 5+ |
| FR-EVT-073 | `Subscribe<T>` returns a `struct SubscriptionToken` opaque handle; no class allocation. | MUST | §3.2.2 | §5.3 unit | Stage 0+1 |
| FR-EVT-074 | Subscriber dispatch order over a given event type follows registration order (deterministic). | MUST | §3.2.5 | §5.3 unit | Stage 0+1 |
| FR-EVT-075 | Re-entrant publish from inside a Tier A/B handler enqueues a second-order event in the same tick; FIFO order is preserved by `intraPhaseDrawIndex` increment on enqueue. | MUST | §3.2.5 | §5.3 unit | Stage 0+1 |
| FR-EVT-076 | Subscribers cannot register against the wrong tier marker; enforcement is compile-time via the `Subscribe<T>` generic constraint and Spec #20 lint. | MUST | §3.2.2 / §4.3.3 | Spec #20 lint | Stage 0+1 |
| FR-EVT-077 | Replay-aware subscribers opt in via the separate `IReplayEventReader` channel (Stage 1+); ordinary subscribers do NOT receive replayed events. | MUST | §3.8.1 | Stage 1+ activation | Stage 1+ |
| FR-EVT-078 | Subscribers' handler methods MUST take `in T evt`; passing `T evt` by value is rejected by Spec #20 lint. | MUST | KD-8 | Spec #20 lint | Stage 0+1 |
| FR-EVT-079 | All Spec #17 runtime error codes occupy the reserved `0x17NN` block and MUST NOT collide with #16's `0x16NN` block. | MUST | §3.10 | Registry-validator (Stage 0) | Stage 0 |
| FR-EVT-080 | Fixture load that encounters an `eventTypeOrdinal` not in Appendix A raises `ERR_EVT_ORDINAL_UNKNOWN`. | MUST | §2.5 / §3.7.2 | §5.3 unit | Stage 0+1 |
| FR-EVT-081 | Fixture load that encounters a `payloadVersion` newer than the current registry row raises `ERR_EVT_VERSION_INCOMPATIBLE`. | MUST | §2.5 / §3.7.2 | §5.3 unit | Stage 0+1 |
| FR-EVT-082 | A Tier A publish from any phase other than `Events` is aliased to `ERR_DS_PHASE_OWNERSHIP` (#16 §3.6.1 `TBD-NORMATIVE`); Spec #17 does not define a separate code. | MUST | KD-2 / §2.5 | §5.3 unit (debug build assert) | Stage 0+1 |

## 2.3 Failure-to-Comply Modes

Top-level non-compliance routes:

- **Phase-ownership violation** — Tier A publish from a phase
  outside `Events` → `ERR_DS_PHASE_OWNERSHIP` per #16 §3.6.1
  `TBD-NORMATIVE`. Aliased into Spec #17 as FR-EVT-082; no separate
  #17 code.
- **Queue overflow** — Tier A/B publish past `EVENT_QUEUE_CAPACITY`
  → `ERR_EVT_QUEUE_OVERFLOW` (hard fail; no drop per KD-7).
- **Tier-mismatch subscription** — authoritative subscriber against
  a Tier C stream → spec-review failure at Stage 0; Spec #20 lint
  failure at Stage 0+1. Enforcement is compile-time only via the
  `Subscribe<T>` generic constraint; no runtime error code is
  issued (FR-EVT-016 / FR-EVT-076 — see §3.2.2 / §4.3.3).
- **Post-init Tier A/B registration** — runtime attempt at
  register/unregister of a Tier A/B subscriber after boot phase
  ended → `ERR_EVT_REGISTRATION_PHASE` (`0x1705`) per §2.5 /
  §3.2.2; FR-EVT-021; lifecycle violation distinct from tier-marker
  mismatch.
- **Allocation in publish path** — Spec #20 §3.x banned-API lint
  failure at Stage 0+1.
- **Versioning violation** — field removed without ordinal bump,
  field width changed in place, tier changed in place — registry
  schema-validator failure at fixture load (Spec #19 §3.3.4
  `TBD-NORMATIVE` governance).

Routing table:

| Violation | Stage 0 enforcement | Stage 0+1 enforcement |
|-----------|---------------------|------------------------|
| Field type outside §3.1.4 whitelist | Spec review | Spec #20 lint |
| Subscriber closure capture | Spec review | Spec #20 lint |
| Tier-mismatch subscription | Spec review | Spec #20 lint (compile-time generic constraint; no runtime code) |
| Post-init Tier A/B registration | Spec review | Runtime `ERR_EVT_REGISTRATION_PHASE` (FR-EVT-021) |
| Queue overflow | n/a (no code) | Runtime `ERR_EVT_QUEUE_OVERFLOW` |
| Allocation in publish path | Spec review | Spec #20 lint + allocation-tracker test (§5.3) |
| Registry row violates KD-9 invariants | Spec review + registry-validator | Same + fixture-load `ERR_EVT_VERSION_INCOMPATIBLE` / `ERR_EVT_ORDINAL_UNKNOWN` |

## 2.4 Data Structures

### 2.4.1 Event-struct skeleton (normative)

```csharp
[StructLayout(LayoutKind.Sequential)]
public readonly struct <Name>Event
{
    public readonly byte   eventTypeOrdinal;    // KD-9; from Appendix A
    public readonly byte   payloadVersion;      // KD-9
    public readonly ushort _reserved;           // padding; canonical zero
    public readonly uint   tick;                // physics tick at publish
    public readonly ushort subsystemOrdinal;    // #16 §3.1.1 subsystem ordinal
    public readonly ushort intraPhaseDrawIndex; // #16 §3.2.5.1 (TBD-NORMATIVE)
    // ── payload fields appended in canonical declaration order ──
}
```

Field-order rules (normative):

- The 12-byte header is fixed and identical for Tier A / B / C:
  `1 + 1 + 2 + 4 + 2 + 2 = 12 bytes`. Tier classification is
  metadata on the registry row (Appendix A), **not** a runtime
  byte.
- Payload fields follow the header in canonical declaration order;
  the §3.4.2 `SerializeCanonical` routine writes them explicitly in
  that order with no implicit padding.

**Canonical-vs-in-memory layout.** The skeleton above defines the
**canonical serialized** layout consumed by §3.4.2
`SerializeCanonical`. The in-memory C# struct layout is permitted
to differ — `[StructLayout(LayoutKind.Sequential)]` without
`Pack = 1` is sufficient because the canonical form is produced by
the §3.4.2 serializer, which writes fields explicitly. `Pack = 1`
is NOT required (it would impose cross-platform-suspect alignment
costs); the serializer is the **only** authoritative source of
on-disk and digest bytes.

Padding rule: `_reserved` is normalized to zero on serialize /
digest per #16 §3.2.4.1 `TBD-NORMATIVE`.

### 2.4.2 Event registry (Appendix A schema)

The registry is the canonical list of every event type, its
ordinal, current version, tier, producer phase(s), and payload
field schema. Spec #17 §2.4 specifies the **shape** of registry
rows; Appendix A holds the **table**.

Initial registry rows at Spec #17 approval time (11 seeds):

| Ordinal (hex) | Type | Tier | Producer phase | Owning spec | Version | First published in |
|---------------|------|------|----------------|-------------|---------|---------------------|
| `0x01` | `ShotExecutedEvent` | A | Resolve | #6 (cited; payload not redefined) | 1 | #17 v1.0 (registry seed); payload from #6 §2.4 |
| `0x02` | `BallContactEvent` | A | Physics | #1 / #3 | 1 | #17 v1.0 |
| `0x03` | `BallCrossedLineEvent` | A | Physics | #1 | 1 | #17 v1.0 |
| `0x04` | `PossessionChangedEvent` | A | Resolve | #17 (default owner) | 1 | #17 v1.0 |
| `0x05` | `FoulCommittedEvent` | A | Resolve | #17 (default owner) | 1 | #17 v1.0 |
| `0x06` | `CardIssuedEvent` | A | Resolve | #17 (default owner) | 1 | #17 v1.0 |
| `0x07` | `GoalAwardedEvent` | A | Resolve | #17 (default owner) | 1 | #17 v1.0 |
| `0x08` | `SubstitutionEvent` | A | Resolve | #17 (default owner) | 1 | #17 v1.0 |
| `0x09` | `TickHeartbeatEvent` | C | `Snapshot` | #17 (default owner) | 1 | #17 v1.0 |
| `0x0A` | `VfxImpactCue` | C | Resolve | #17 (default owner) | 1 | #17 v1.0 |
| `0x0B` | `UiNotificationCue` | C | Resolve | #17 (default owner) | 1 | #17 v1.0 |

The `First published in` column is the audit trail for deprecation
rationale (KD-9 retains deprecated rows indefinitely). Future-spec
appended rows populate this column with `<spec> <version>` at the
IN REVIEW commit that adds the row.

Future specs append their event types to this table at the time
they reach `IN REVIEW`:

- #10 Heading Mechanics → `HeaderExecutedEvent` (Tier A).
- #11 Goalkeeper Mechanics → `SaveAttemptedEvent`,
  `BallParriedEvent`, `BallCaughtEvent` (Tier A).
- #13–#15 AI specs → `PressTriggeredEvent`, `MarkAssignedEvent`,
  `RunCalledEvent` (Tier A).

### 2.4.3 Versioning rule statements (KD-9 mechanics in §3.7)

| Rule | Statement | FR | Mechanics |
|------|-----------|----|-----------|
| V1 | Adding a payload field → append at end, bump `payloadVersion`. | FR-EVT-055 | §3.7.1 |
| V2 | Removing a field → forbidden; mint a new `eventTypeOrdinal`. | FR-EVT-056 | §3.7.1 |
| V3 | Reordering fields → forbidden after the event reaches `APPROVED`. | FR-EVT-057 | §3.7.1 |
| V4 | Width changes on existing fields → forbidden; mint new ordinal. | FR-EVT-058 | §3.7.1 |
| V5 | Tier changes on existing event → forbidden; mint new ordinal. | FR-EVT-059 | §3.7.1 |

### 2.4.4 Ledger record layout (binds to #16 §3.9.2 `TBD-NORMATIVE`)

The per-tick event ledger, when serialized into `SnapshotPayload`,
is laid out as:

```
EventLedgerRecord = [
    count:   u32,
    records: array<EventRecord>,
]

EventRecord = [
    header (12 bytes, §2.4.1),
    payloadBytes: variable (canonical encoding per #16 §3.2.4.1)
]
```

- Only Tier A and Tier B records appear in `EventLedgerRecord`.
- Tier C records never appear in `SnapshotPayload` (KD-3 / FR-EVT-015).
- The domain-tag byte for `EventLedgerRecord` preimage assignment
  is `DOMAIN_TAG_EVENT_LEDGER = 0x15` (see §3.4 / §3.10),
  allocated in #16 §3.4 v1.0.1 (May 14, 2026) per ERR-017-001
  RESOLVED. Tag `[CROSS]` — owned by #16's namespace, consumed
  read-only here.

## 2.5 Failure Modes

| Code | Mnemonic | Trigger | Mechanics | Caused by FR |
|------|----------|---------|-----------|--------------|
| `0x1701` | `ERR_EVT_QUEUE_OVERFLOW` | Tier A/B publish past `EVENT_QUEUE_CAPACITY`, OR second-order dispatch past `MAX_EVENT_DISPATCH_DEPTH`, OR per-handler out-degree past 1 (FR-EVT-046a/b). | §3.6.1, §3.2.5 | FR-EVT-041, FR-EVT-047, FR-EVT-046b |
| `0x1702` | *(reserved — slot recovered; not allocated)* | Tier-marker mismatch is compile-time only (FR-EVT-016, FR-EVT-076); no runtime code is needed. | — | — |
| `0x1703` | `ERR_EVT_ORDINAL_UNKNOWN` | Fixture load encounters an `eventTypeOrdinal` not in Appendix A. | §3.7.2 | FR-EVT-080 |
| `0x1704` | `ERR_EVT_VERSION_INCOMPATIBLE` | Fixture load encounters a `payloadVersion` newer than the current registry row. | §3.7.2 | FR-EVT-081 |
| `0x1705` | `ERR_EVT_REGISTRATION_PHASE` | Runtime register/unregister of a Tier A/B subscriber after the boot phase ended. | §3.2.2 | FR-EVT-021 |
| (aliased) | `ERR_EVT_PHASE_OWNERSHIP` | Alias to #16 `ERR_DS_PHASE_OWNERSHIP`; Tier A publish from a non-`Events` phase. | §3.2.1 | FR-EVT-010, FR-EVT-082 |

Numeric pins (`0x1701` … `0x1705`) are allocated from the reserved
`0x17NN` block. They MUST NOT collide with #16's `0x16NN` block
(checked at §9.2 quality-checklist row). Error-code values are
recorded in §3.10 constants catalogue.

## 2.6 Version History

| Version | Date         | Author      | Notes                                                                 |
|---------|--------------|-------------|-----------------------------------------------------------------------|
| 0.1     | May 13, 2026 | Claude Code | Initial section-file draft from `outline-detailed.md` v1.1. 82 FRs published with full conformance/source/verification/activation columns. Section heading order superseded the v0.0 stub. |
| 0.2     | May 13, 2026 | Claude Code | PASS 1 critique resolution. Added FR-EVT-046a/046b (per-handler out-degree cap = 1; H1) and FR-EVT-009a (single-marker constraint; L6). Reworded FR-EVT-002 for canonical-vs-in-memory layout (M5). Renamed `producerSubsystem` → `subsystemOrdinal` in §2.4.1 (M4). Replaced `[TBD-CITE]` with `TBD-NORMATIVE` at FR-EVT-026 (M2). FR-EVT-021 retargeted at new `ERR_EVT_REGISTRATION_PHASE = 0x1705`; §2.5 grew the new error row (L3). Updated TickHeartbeatEvent registry row to `AI_NoOp` (H2). |
| 0.3     | May 13, 2026 | Claude Code | PASS 2 critique resolution. H-2-1: reverted TickHeartbeatEvent producer phase to `Snapshot` in §2.4.2 seed table; updated FR-EVT-038 to name `Snapshot` as canonical producer with `AI_NoOp` as non-binding example. H-2-2: removed `ERR_EVT_TIER_MISMATCH` runtime code; `0x1702` slot marked reserved in §2.5; FR-EVT-016 / FR-EVT-076 retargeted to compile-time / Spec #20 lint only. M-2-1/M-2-2: §2.3 prose corrected post-init lifecycle route to `ERR_EVT_REGISTRATION_PHASE`; routing table gained a "Post-init Tier A/B registration" row and corrected the "Tier-mismatch" row to lint-only. M-2-3: FR-EVT-052 reworded to carve out struct-ref delegates with `in T` parameter (§3.5.4 exempt). M-2-5: FR-EVT-046a/046b moved before FR-EVT-047 (ID-sort order). |
| 1.0.1   | May 15, 2026 | Claude Code | Patch revision (no behavioral change). §2.4.4 `DOMAIN_TAG_EVENT_LEDGER` `[CROSS-PENDING]` → `[CROSS]`, literal value `0x15` inlined per #16 §3.4 v1.0.1 (ERR-017-001 RESOLVED May 14, 2026). |
