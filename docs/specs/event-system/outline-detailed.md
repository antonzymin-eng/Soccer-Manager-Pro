# Event System Specification #17 — Detailed Outline

**Created:** May 12, 2026
**Last Updated:** May 12, 2026
**Version:** 1.1
**Status:** DRAFT — expansion of `outline.md` v1.0. v1.0 resolved all 12
findings from the May 6 adversarial review of `outline.md`. v1.1 applies
PASS 2 ADVERSARIAL REVIEW findings (4H / 6M / 5L; review block appended
at end of file). All H and M findings resolved in this revision; L
findings addressed in their respective subsections. Ready for
section-file authoring. `SPEC_INDEX.md` row 17 advanced to `IN PROGRESS`
atomically with this revision.
**Companion documents:** `outline.md` (high-level v1.0 + May 6
adversarial review).

---

## PURPOSE OF THIS DOCUMENT

Expansion of `outline.md` into a section-by-section subsection plan
that resolves every finding from the May 6, 2026 adversarial review.
For every subsection: the rules / FRs it will publish, the boundary
declarations it will hold, and the cross-references it will emit.
Detailed enough that `section-1.md` … `section-9-approval-checklist.md`
and `appendices.md` can be drafted directly from this document.

This document does **not** publish FR text in normative form — that
text lands in `section-2.md`. The detailed outline records every FR's
intended rule, conformance level, and source so the FR table can be
authored mechanically.

Spec #17 governs the **event ledger** machinery that the
Deterministic Simulation `Events` phase (#16 §3.1.2, §3.6.1) already
mandates. Spec #17 is therefore a downstream consumer of #16's phase
contract and an upstream publisher of the typed event surface
consumed by Goalkeeper Mechanics #11, Heading Mechanics #10,
Statistics Engine (Stage 1+), and replay/UI tooling.

---

## CROSS-CUTTING DESIGN DECISIONS

These decisions are referenced throughout the outline. They are
stated once here and cited below by KD-number, never restated.

- **KD-1 — Cite-not-redefine.** Spec #17 never restates a CLAUDE.md
  invariant or a rule already published by another approved spec
  (Ball Physics #1 coordinate convention, Decision Tree #8 parameter-
  based-physics rule, Shot Mechanics #6 `ShotExecutedEvent` payload
  fields, etc.). It cites and binds.

- **KD-2 — Boundary with Deterministic Simulation #16.** Spec #16 is
  the **authoritative** owner of:
  - Phase pipeline (#16 §3.1.2: `Input → Intent → AI/AI_NoOp →
    Physics → Resolve → Events → Snapshot`).
  - Phase WriteSet table (#16 §3.6.1) which already names
    "event ledger" as the `Events` phase WriteSet.
  - Per-phase digest preimage (#16 §3.2.2) and the snapshot-payload
    inclusion rule (#16 §3.2.3 / §3.9.2).
  - Tier classification (#16 §1.3 / §1.3.1).
  - Save boundary (#16 §3.7: `LEGAL_SAVE_BOUNDARIES = { EndOfSnapshot }`).
  Spec #17 *implements* the data structure and pub/sub mechanics
  inside the `Events` phase. It does **not** add new phases, alter
  phase ordering, or relax WriteSet constraints.
  - **Status caveat (May 12, 2026).** Per `SPEC_INDEX.md`, Spec #16
    is `IN PROGRESS`, not `APPROVED`. All citations of "#16 §1.3.1",
    "#16 §3.1.2", "#16 §3.2.2", "#16 §3.2.3", "#16 §3.6.1", "#16 §3.7",
    "#16 §6.2", "#16 §8.2" are tagged `TBD-NORMATIVE` (pattern
    adopted from #16 §8.3.1 and Spec #19 KD-2 per CLAUDE.md OPEN
    ISSUES) until #16 reaches `APPROVED`. Section files MUST carry
    the tag verbatim on every #16 citation; tag removal is a §9.2
    quality-checklist row and is gated on #16 approval.
  - **Status caveat — Spec #19 (testing).** Spec #19 is `IN REVIEW`,
    not `APPROVED` (CLAUDE.md OPEN ISSUES). Every citation in this
    spec of "#19 §3.1.2" (test pyramid ratios), "#19 §3.4" (property
    tests), "#19 §3.8" (fixture governance), and "#19 §3.2 / §3.4.3"
    (determinism-suite consumption) carries the same `TBD-NORMATIVE`
    tag until #19 reaches `APPROVED`.
  - **`[CROSS-PENDING]` qualifier.** CLAUDE.md `[CROSS]` requires the
    upstream spec to be `APPROVED`. For constants imported from a
    spec that is currently `IN PROGRESS` / `IN REVIEW`, use the
    `[CROSS-PENDING]` qualifier paired with a `TBD-NORMATIVE` tag on
    the citation. At upstream `APPROVED` time the qualifier is
    rewritten to `[CROSS]` and the `TBD-NORMATIVE` tag is removed in
    the same revision. Used by `DOMAIN_TAG_EVENT_LEDGER` in §3.10.
  - **Sequencing constraint.** Per CLAUDE.md OPEN ISSUES, #16's
    Tier 2 final approval is gated on `#9 / #17 / #18 / #19 reaching
    IN REVIEW`. Spec #17 in turn binds substantively to #16. The
    resolution path is: (1) #17 reaches `IN REVIEW` with
    `TBD-NORMATIVE` citations to #16; (2) #16 reaches Tier 2
    `APPROVED`; (3) #17's `TBD-NORMATIVE` tags are resolved and #17
    advances to `APPROVED`. `SPEC_INDEX.md` status transitions for
    #17 MUST follow this order.

- **KD-3 — Event Tier classification (binds to #16 §1.3.1).** Every
  event type declared in or against this spec carries a tier tag:
  - **Tier A (authoritative state-changing):** event payload is part
    of the per-tick `Events`-phase digest (#16 §3.2.2) and is
    serialized into the `SnapshotPayload` event-ledger field (#16
    §3.2.3 / §3.9.2 layout). Loss or reordering of a Tier A event
    breaks replay parity and is a `ERR_DS_PHASE_OWNERSHIP`-class
    error if it originates outside the `Events` phase's WriteSet.
    Examples: `ShotExecutedEvent`, `GoalAwardedEvent`,
    `PossessionChangedEvent`, foul/card events.
  - **Tier B (bounded-authoritative):** event payload is digested
    but uses #16 §3.5 Tier-B tolerance rules for any continuous
    fields. Reserved for cross-platform parity concerns; not
    expected to populate at Stage 0 (parallel to #16 §1.3). **Why
    `IEventB` is not a phantom interface (CLAUDE.md "Interface
    Design Principle"):** the tier vocabulary is normatively owned
    by #16 §1.3.1 — Spec #17 must model all three tiers because
    omitting one would silently force Tier B traffic onto Tier A
    paths at Stage 5+ migration time, breaking the per-tier digest
    contract (§3.4.2). Both sides of `IEventB` are specified here:
    the **publisher** side is the `EventBus.Publish<T> where T :
    IEventB` overload (§3.2.1); the **consumer** side is the
    `EventLedger` dispatcher (§4.4) and the Tier-B tolerance
    application path declared in #16 §3.5. This satisfies the
    "both sides specified" test even though no Stage 0 event type
    populates Tier B.
  - **Tier C (cosmetic / observability only):** VFX, UI, telemetry,
    audio cue events. NOT digested. NOT part of `SnapshotPayload`.
    Loss is permitted; ordering across phases is permitted. Tier C
    events MUST NOT be subscribed by authoritative gameplay code
    (this constraint is checked at draft-review time and again at
    Spec #20 §3.x linter activation time post-Stage 0+1).
  Tier vocabulary is **cited** from #16 §1.3.1 by reference; not
  redefined. Every event-type registry row in §3.1 / Appendix A
  MUST carry a tier tag.

- **KD-4 — Authoritative-vs-cosmetic publish path separation.** The
  publish API is a single surface (`EventBus.Publish<T>(in T evt)`)
  with the path selected at compile time by the tier tag on `T`.
  Tier A / B events route through the in-tick ledger writer (only
  callable from the `Events` phase per #16 §3.6.1). Tier C events
  route through an out-of-band cosmetic channel that is permitted
  from any phase but whose effects are explicitly excluded from the
  digest (#16 §3.2.2). The two paths are statically distinguishable
  by the tier tag on the event struct and by the WriteSet rules of
  the calling phase. A Tier A publish call from any phase other
  than `Events` is a `ERR_DS_PHASE_OWNERSHIP` violation per #16
  §3.6.1.

- **KD-5 — Tick-rate split.** Per CLAUDE.md "Heartbeat Tick Rate"
  and #16 §3.1.2:
  - **10 Hz tactical / AI** (per #16 stride: `AI` phase runs on
    `tick % 6 == 0`). Tactical-cadence events (decision-tree
    outputs, possession decisions, marking-assignment changes) are
    *queued* during their producing phases (`AI`, `Resolve`) and
    *flushed* during the same tick's `Events` phase.
  - **60 Hz physics tick** (every tick). Physics-cadence events
    (`BallContactEvent`, `ShotExecutedEvent`,
    `BallCrossedLineEvent`) are queued during `Physics` and
    `Resolve` and flushed during the same tick's `Events` phase.
  Neither tactical nor physics events cross tick boundaries within
  the authoritative path; the queue is drained every tick by the
  `Events` phase. KD-5 is the canonical cite for "which subsystem
  publishes in which phase" (resolves outline finding 9).

- **KD-6 — Determinism contracts (binds to #16 §3.1.1 / §3.2.2).**
  - Intra-phase ordering of event publication MUST be deterministic.
    The canonical order within the `Events` phase is:
    (a) producing-phase order (`AI`/`AI_NoOp`, `Physics`, `Resolve`)
    of the queue entries;
    (b) within a producing-phase batch, by `(subsystemOrdinal,
    entityId, eventTypeOrdinal, intraPhaseDrawIndex)` —
    `subsystemOrdinal` and `entityId` per #16 §3.1.1 ordering rules;
    `eventTypeOrdinal` per Appendix A registry; `intraPhaseDrawIndex`
    parallel to #16 §3.2.5.1 intra-stream ordering.
  - Event-ledger digest is a sub-scope of the `Events` phase digest
    (#16 §3.2.2). FM-017-001 (§3.2.2) defines the canonical
    serialization for that sub-scope.
  - No event publication is conditional on wall-clock time,
    `System.Random`, or any unstable iteration order (banned per
    Spec #20).

- **KD-7 — No-drop policy on authoritative paths (resolves outline
  finding 4).** Tier A and Tier B events MUST NOT be dropped on the
  authoritative path. The per-tick event queue is sized to a
  worst-case budget (§3.4 + §6.3) and a queue-full condition is a
  hard error (`ERR_EVT_QUEUE_OVERFLOW`), not a silent drop. Drop
  decisions based on transient queue depth would be non-reproducible
  across replay (queue depth is not part of authoritative state) and
  are therefore forbidden. Tier C events MAY be dropped under a
  *deterministic* policy that does not read queue depth — currently
  "drop if event-type publication rate exceeds the static cap
  declared in Appendix A for that type"; otherwise no drop.

- **KD-8 — Zero-allocation in the hot loop (binds to CLAUDE.md
  "When Writing Code" struct-based zero-allocation rule; resolves
  outline finding 6).** The publish path commits to:
  - Event payloads are `readonly struct`s (no classes).
  - `EventBus.Publish<T>(in T evt)` takes `in`-reference; no boxing.
  - The event ledger uses a pre-allocated ring buffer sized to the
    per-tick budget (§6.3). No `new[]` in the hot path.
  - Subscribers are registered at startup; subscriber-list iteration
    uses a pre-allocated array. No LINQ, no `IEnumerable`, no
    delegate-list allocation per publish.
  - No closures captured in subscriber registration; subscriber
    handlers take `in T evt` parameter only.
  - Spec #20 §3.x banned-API enforcement covers this at lint time
    once Stage 0+1 activates.

- **KD-9 — Event-contract versioning (resolves outline finding 8;
  promoted from appendix to §1.6 / §2.4).** Every event struct
  carries a `eventTypeOrdinal` (stable, never reused) and a
  `payloadVersion` byte. Adding a field bumps `payloadVersion` and
  appends to the canonical-serialization layout (#16 §3.2.4.1
  `array<T>` rules apply). Removing a field is forbidden once the
  event is published in an `APPROVED` spec — it triggers a new
  `eventTypeOrdinal` instead. The registry in Appendix A records
  every (ordinal, version) pair ever published; deprecated rows are
  retained for replay-corpus compatibility. The full versioning rule
  set lives in §2.4 (data structures) with mechanics in §3.7;
  Appendix A holds the registry table.

- **KD-10 — Stage 5+ multiplayer "do not preclude" (resolves outline
  finding 7).** Per CLAUDE.md "Fixed64 stage scope decision",
  cross-platform / multiplayer parity is Stage 5+. Spec #17 commits
  to:
  - Event structs are `readonly struct` with explicit field order
    (compatible with #16 §3.2.4.1 canonical serialization).
  - No Unity-engine-specific singletons, no `UnityEngine.Object`
    references in event payloads.
  - `eventTypeOrdinal` namespace is reserved globally (single
    registry, Appendix A) so future networked event multiplexing has
    a stable identifier space.
  - Wire-format design (compression, framing, ack semantics, lossy
    Tier C transport) is **out of scope** at Stage 0 and is named
    in §7 as a Stage 5+ deliverable. Spec #17 §8 is references, not
    multiplayer compatibility (resolves outline finding 2).

- **KD-11 — Instrumentation budget binding to #16 §8.2 (resolves
  outline finding 11).** Tracing, counters, and debug-replay hooks
  declared in §5 fit within #16 §8.2's instrumentation envelope.
  Spec #17 does not republish #16's budget numbers; it consumes them
  and declares its own per-publish instrumentation cost (§6.3) so
  the sum can be audited at #16 §8.2 budget-check time. Per-event
  trace channel names are defined here; trace-channel format is
  cited from #16 §8.
  - **Status caveat.** "#16 §8.2" tagged `TBD-NORMATIVE` per KD-2.

- **KD-12 — Stage-gated activation.** Sections that presume an
  implemented codebase (per-publish allocation budget enforcement,
  CI lint rules for tier-mismatch subscription, event-ledger byte
  layout in `SnapshotPayload`) are contracts that *activate* at the
  Stage 0 → Stage 1 transition. They are first-class normative
  content of this spec but are not enforceable during the
  spec-writing phase. Activation status is tracked per-FR in §5.

---

## SECTION 1 — PURPOSE & SCOPE (`section-1.md`)

### 1.1 What This Specification Covers

**Subsection target length:** ~40 lines.

**Content:**
- Opening declarative scope statement: Spec #17 defines the typed
  event-ledger architecture that the Deterministic Simulation
  `Events` phase (#16 §3.1.2) operates against.
- Bullet list of governance areas (8 items):
  1. Typed event-payload contracts (`readonly struct`, fields, ordinals).
  2. Tier classification rule (Tier A / B / C; KD-3).
  3. Publish/subscribe semantics + intra-phase ordering (KD-6).
  4. Zero-allocation hot-loop guarantees (KD-8).
  5. Queue-overflow / no-drop policy on authoritative paths (KD-7).
  6. Event-contract versioning rules (KD-9).
  7. Instrumentation / trace-channel registry (KD-11).
  8. Stage 5+ "do not preclude" multiplayer constraints (KD-10).
- Applicability block:
  - **Primary:** every event-publishing or subscribing call site in
    Stage 0 gameplay code (once Stage 1 begins).
  - **Secondary:** every spec from #1–#20 that names an event in its
    §2.4 / §4 (Shot Mechanics #6 `ShotExecutedEvent`, Heading #10
    `HeaderExecutedEvent`, Goalkeeper #11 events, Statistics Engine).
- Closing pointer to §3 (mechanics) and §6 (budgets).

### 1.2 What Is Out of Scope

**Subsection target length:** ~30 lines.

One-line entries with the owning document:
- Phase pipeline ordering, phase-digest preimage, snapshot inclusion
  → Spec #16 §3.1.2 / §3.2.2 / §3.2.3 (KD-2).
- Tier vocabulary definition → Spec #16 §1.3.1 (KD-3 cites; does not
  redefine).
- RNG draw mechanics inside event handlers → Spec #16 §3.2.5
  (consumers route through `DeterministicRngService`).
- Multiplayer wire format / network transport / lossy delivery →
  Stage 5+ extension named in §7.3 (KD-10).
- C# code style and banned-API rules (no LINQ, no `System.Random`,
  no `IEnumerable` in hot path) → Spec #20.
- Per-event unit test catalogues for individual event types →
  owning spec's §5 (Spec #19 governance applies).
- Performance regression gate thresholds → Spec #18 §4 / §7.
- Save/load file format → Spec #16 §3.9.2 `SnapshotPayload` layout.
- Editor-tool / debug-overlay UI → Stage 1+ tooling.

### 1.3 Key Design Decisions

Full restatement of KD-1 … KD-12 with one-line rationale and the
section that codifies each:

| KD | Topic | Codified in |
|----|-------|-------------|
| KD-1 | Cite-not-redefine | All sections |
| KD-2 | Boundary with #16 (`Events` phase WriteSet) | §3.1, §3.2, §4 |
| KD-3 | Tier A / B / C event classification | §2.4, §3.1, Appendix A |
| KD-4 | Authoritative-vs-cosmetic publish path separation | §3.2, §4.2 |
| KD-5 | Tick-rate split (10 Hz tactical / 60 Hz physics) | §3.2, §3.3 |
| KD-6 | Determinism contracts (ordering + digest) | §3.2, §3.4, §5 |
| KD-7 | No-drop on authoritative; `ERR_EVT_QUEUE_OVERFLOW` | §3.4, §3.6 |
| KD-8 | Zero-allocation hot-loop policy | §3.5, §6.2 |
| KD-9 | Event-contract versioning | §2.4, §3.7, Appendix A |
| KD-10 | Stage 5+ "do not preclude" multiplayer | §7.3 |
| KD-11 | Instrumentation budget binding to #16 §8.2 | §5, §6.3 |
| KD-12 | Stage-gated activation | §5.2, §7 |

### 1.4 Dependencies and Integration Contracts

- **Upstream (substantive):**
  - Root `CLAUDE.md` (project invariants; "When Writing Code"
    struct-based zero-allocation rule; "Heartbeat Tick Rate";
    "Interface Design Principle" — only write interfaces when both
    sides specified).
  - Spec #16 (Deterministic Simulation) §1.3 tier classification,
    §3.1 phase pipeline, §3.2 digests, §3.6.1 phase WriteSet table,
    §3.9.2 on-disk snapshot layout, §8 trace channels.
    **Status:** `IN PROGRESS`. All citations tagged `TBD-NORMATIVE`
    per KD-2.
- **Upstream (consulted):**
  - Spec #6 (Shot Mechanics) §2.4 / §4 — `ShotExecutedEvent` is an
    existing published surface that #17 inherits and formalises.
  - Spec #20 (Code Standards) §3.x — zero-allocation lint rules,
    struct event pattern at §3.x.x (cited example: `ShotExecutedEvent`).
- **Downstream (consumers of #17's contracts):**
  - Spec #10 (Heading Mechanics) — will declare `HeaderExecutedEvent`
    against this spec's tier-A schema.
  - Spec #11 (Goalkeeper Mechanics) — consumes `ShotExecutedEvent`,
    publishes `SaveAttemptedEvent`.
  - Specs #13–#15 (Pressing AI, Defensive AI, Attacking AI) — will
    publish tactical-cadence events (`PressTriggeredEvent`,
    `MarkAssignedEvent`, etc.).
  - Spec #19 (Testing Strategy) — consumes ordering/throughput test
    catalogue defined here.
  - Spec #18 (Performance Optimization) — consumes per-publish cost
    budget declared in §6.3.
  - Statistics Engine (Stage 1+ spec) — consumes Tier A event stream
    for match-stat aggregation.
- **Bidirectional sequencing with #16:** #17's `IN REVIEW` status is
  a precondition for #16's Tier 2 `APPROVED` (per CLAUDE.md OPEN
  ISSUES); #16's `APPROVED` status is a precondition for #17's own
  `APPROVED`. See KD-2 sequencing constraint.
- **Cross-spec constants imported:** none direct. Spec #17 imports
  tier *vocabulary* from #16 §1.3 by reference (KD-1
  cite-not-redefine). Pipeline phase names cited from #16 §3.1 by
  reference. No `[CROSS]` constant declarations expected in §3.10.
- **Stage 0 host platform pin:** test execution requires the pins
  named in `docs/tracking/certification-platform.md`. Drafting Spec
  #17 does not require those pins to be filled in; first CI
  activation (Stage 0+1 transition) does.

### 1.5 Glossary (Spec #17-local terms)

- **Event ledger** — the per-tick authoritative store of Tier A / B
  events. Owned by the `Events` phase per #16 §3.6.1.
- **Cosmetic channel** — the out-of-band Tier C delivery path. Not
  part of authoritative state.
- **eventTypeOrdinal** — globally stable byte identifier; never
  reused after publication.
- **payloadVersion** — append-only version byte on each event
  struct.
- Phase-ownership / digest / tier vocabulary cited from #16 (not
  restated).

### 1.6 Version History

Standard version-history table (initially empty, populated on
draft).

---

## SECTION 2 — FUNCTIONAL REQUIREMENTS & DATA STRUCTURES (`section-2.md`)

### 2.1 Conformance Levels

- MUST / SHOULD / MAY (RFC 2119 cited).
- "Exception with sign-off" semantics identical to Spec #20 §2.1.

### 2.2 Functional Requirement Catalogue

All FR-EVT-### live here with rule statement, conformance level,
source citation, and verification pointer (`§5.x`). Detailed
outline names the partition; section file fills in every numbered
FR.

| FR Range | Topic | Rule mechanics in |
|----------|-------|-------------------|
| FR-EVT-001 … 008 | Event typed-contract rules (struct, ordinal, version) | §3.1, §3.7 |
| FR-EVT-009 … 016 | Tier classification (A / B / C; KD-3) | §3.1.3, Appendix A |
| FR-EVT-017 … 026 | Publish / subscribe semantics (KD-4, KD-6) | §3.2 |
| FR-EVT-027 … 033 | Intra-tick ordering & digest contribution (KD-6) | §3.2.4, §3.4 |
| FR-EVT-034 … 040 | Tick-rate split (10 Hz / 60 Hz; KD-5) | §3.3 |
| FR-EVT-041 … 047 | Queue overflow & no-drop policy (KD-7) | §3.4, §3.6 |
| FR-EVT-048 … 054 | Zero-allocation hot-loop policy (KD-8) | §3.5, §6.2 |
| FR-EVT-055 … 060 | Versioning / migration / deprecation (KD-9) | §3.7, Appendix A |
| FR-EVT-061 … 066 | Instrumentation / trace channels (KD-11) | §5, §6.3 |
| FR-EVT-067 … 072 | Stage 5+ "do not preclude" constraints (KD-10) | §7.3 |
| FR-EVT-073 … 078 | Subscriber-registration / lifetime semantics | §3.2.5, §4.3 |
| FR-EVT-079 … 082 | Error codes & failure modes | §3.6, §3.9 |

Each FR row: `ID | Statement | Level | Source citation | Verification (§5.x) | Activation stage`.

### 2.3 Failure-to-Comply Modes

- Phase ownership violation (Tier A publish from non-`Events`
  phase) → `ERR_DS_PHASE_OWNERSHIP` per #16 §3.6.1.
- Queue overflow → `ERR_EVT_QUEUE_OVERFLOW` (hard fail; no drop).
- Tier-mismatch subscription (authoritative subscriber on Tier C
  stream) → lint failure (Stage 0+1) / spec-review failure (Stage 0).
- Allocation in publish path → Spec #20 lint failure (Stage 0+1).
- Versioning violation (field removed without ordinal bump) →
  schema-validator failure at fixture load (per Spec #19 §3.3.4).

### 2.4 Data Structures

#### 2.4.1 Event-struct skeleton (normative)

```
[StructLayout(LayoutKind.Sequential)]
public readonly struct <Name>Event
{
    public readonly byte   eventTypeOrdinal;   // KD-9; from Appendix A
    public readonly byte   payloadVersion;     // KD-9
    public readonly ushort _reserved;          // padding; canonical zero
    public readonly uint   tick;               // physics tick at publish
    public readonly ushort producerSubsystem;  // #16 §3.1.1 subsystemOrdinal
    public readonly ushort intraPhaseDrawIndex;// #16 §3.2.5.1
    // ── payload fields appended in canonical declaration order ──
}
```

- Field order is normative: header (12 bytes fixed —
  `eventTypeOrdinal` 1 + `payloadVersion` 1 + `_reserved` 2 +
  `tick` 4 + `producerSubsystem` 2 + `intraPhaseDrawIndex` 2)
  precedes payload. Header layout is identical for Tier A / B / C;
  the tier classification is metadata on the registry row
  (Appendix A), not a runtime byte.
- **Canonical-vs-in-memory layout.** The §2.4.1 skeleton defines
  the **canonical serialized** layout consumed by §3.4.2
  `SerializeCanonical`. In-memory C# struct layout is permitted to
  differ — `[StructLayout(LayoutKind.Sequential)]` without
  `Pack = 1` is sufficient because the canonical form is produced
  by the §3.4.2 serializer, which writes fields explicitly in the
  declared order with no implicit padding. `Pack = 1` is NOT
  required (it would impose cross-platform-suspect alignment
  costs); the serializer is the only authoritative source of
  on-disk and digest bytes.
- Padding rule cites #16 §3.2.4.1 (`_reserved` is normalized to
  zero on serialize / digest).

#### 2.4.2 Event registry (Appendix A)

The registry is the canonical list of every event type, its
ordinal, current version, tier, producer phase(s), and payload
field schema. Spec #17 §2.4 specifies the *shape* of registry
rows; Appendix A holds the *table*. Initial registry rows at
Spec #17 approval time:

| Ordinal (hex) | Type | Tier | Producer phase | Owning spec | Version | First published in |
|---------------|------|------|----------------|-------------|---------|---------------------|
| `0x01` | `ShotExecutedEvent` | A | Resolve | #6 (cited; not redefined here) | 1 | #17 v1.0 (registry seed); payload from #6 §2.4 |
| `0x02` | `BallContactEvent` | A | Physics | #1 / #3 | 1 | #17 v1.0 |
| `0x03` | `BallCrossedLineEvent` | A | Physics | #1 | 1 | #17 v1.0 |
| `0x04` | `PossessionChangedEvent` | A | Resolve | #17 (default owner) | 1 | #17 v1.0 |
| `0x05` | `FoulCommittedEvent` | A | Resolve | #17 (default owner) | 1 | #17 v1.0 |
| `0x06` | `CardIssuedEvent` | A | Resolve | #17 (default owner) | 1 | #17 v1.0 |
| `0x07` | `GoalAwardedEvent` | A | Resolve | #17 (default owner) | 1 | #17 v1.0 |
| `0x08` | `SubstitutionEvent` | A | Resolve | #17 (default owner) | 1 | #17 v1.0 |
| `0x09` | `TickHeartbeatEvent` | C | Snapshot | #17 (default owner) | 1 | #17 v1.0 |
| `0x0A` | `VfxImpactCue` | C | Resolve | #17 (default owner) | 1 | #17 v1.0 |
| `0x0B` | `UiNotificationCue` | C | Resolve | #17 (default owner) | 1 | #17 v1.0 |

The `First published in` column is the audit trail for deprecation
rationale (KD-9 retains deprecated rows indefinitely). Future-spec
appended rows must populate this column with `<spec> <version>` at
the IN REVIEW commit that adds the row.

Future specs append their event types to this table at the time
they reach `IN REVIEW`:
- #10 Heading Mechanics → `HeaderExecutedEvent` (Tier A).
- #11 Goalkeeper Mechanics → `SaveAttemptedEvent`,
  `BallParriedEvent`, `BallCaughtEvent` (Tier A).
- #13–#15 AI specs → `PressTriggeredEvent`, `MarkAssignedEvent`,
  `RunCalledEvent` (Tier A).

(Resolves outline finding 5 — event inventory exists at draft
time and is not abstract.)

#### 2.4.3 Versioning rules (KD-9 mechanics outline)

Detail moved to §3.7; §2.4.3 records normative rule statements
only (one paragraph each):
- Adding a payload field → append at end, bump `payloadVersion`.
- Removing a field → forbidden; mint a new `eventTypeOrdinal`.
- Reordering fields → forbidden after the event reaches `APPROVED`.
- Width changes on existing fields → forbidden; mint new ordinal.
- Tier changes on existing event → forbidden; mint new ordinal.

#### 2.4.4 Ledger record layout (binds to #16 §3.9.2)

The per-tick event ledger, when serialized into `SnapshotPayload`,
is laid out as:
```
EventLedgerRecord[T] = [
    count: u32,
    records: array<EventRecord>,
]
EventRecord = [
    header (12 bytes, §2.4.1),
    payloadBytes: variable (canonical encoding per #16 §3.2.4.1)
]
```
- Only Tier A and Tier B records appear in `EventLedgerRecord`.
- Tier C records never appear in `SnapshotPayload` (KD-3).
- Domain-tag byte for `EventLedgerRecord` preimage assignment is
  reserved and cited in §3.4 (`DOMAIN_TAG_EVENT_LEDGER` —
  numeric value pinned in #16 §3.4 domain-tag table once #17
  approval triggers a tag allocation).

### 2.5 Failure Modes

- `ERR_EVT_QUEUE_OVERFLOW` (0x1701 — provisional, pinned at
  approval) — Tier A/B publish exceeds per-tick budget (§6.3).
- `ERR_EVT_TIER_MISMATCH` (0x1702) — subscriber registered against
  wrong tier channel.
- `ERR_EVT_ORDINAL_UNKNOWN` (0x1703) — fixture load encounters
  ordinal not in Appendix A registry.
- `ERR_EVT_VERSION_INCOMPATIBLE` (0x1704) — payload version newer
  than current registry row.
- `ERR_EVT_PHASE_OWNERSHIP` — aliased to #16
  `ERR_DS_PHASE_OWNERSHIP`; Tier A publish from non-`Events` phase.
- Error-code numeric pins recorded in §3.4 constants catalogue;
  they MUST NOT collide with #16's `0x16NN` block.

### 2.6 Version History

---

## SECTION 3 — TECHNICAL SPECIFICATION (rule mechanics) (`section-3.md`)

> Each subsection cites the FR-EVT-### IDs it implements (defined
> in §2.2) and provides the *mechanics*. It does not redefine the
> rule statement.

### 3.1 Event Typed-Contract Mechanics (FR-EVT-001 … 016)

- 3.1.1 Struct layout enforcement: rule that every event satisfies
  the §2.4.1 skeleton; verified by §5 contract test that walks the
  registry and reflects each struct's field order.
- 3.1.2 Ordinal allocation: ordinals are byte-wide (256 max at
  Stage 0); assignment is monotonic within Spec #17 + downstream
  spec additions. Stage 5+ note: a two-byte ordinal expansion is
  reserved at §7.3.
- 3.1.3 Tier metadata: tier tag lives on the registry row, not on
  the struct. Tier-aware APIs (publish, subscribe) take the tier
  via generic constraint (`where T : struct, IEventA` /
  `IEventB` / `IEventC`) — marker interfaces declared in §4.2.
  Marker interfaces are permitted under CLAUDE.md "Interface
  Design Principle" because both producer (publisher) and
  consumer (`EventBus` dispatcher) are specified here.
- 3.1.4 Payload-field type whitelist:
  - Allowed: integer primitives, `float`, `Vector3` (Stage 0
    `float`-backed struct per Ball Physics #1 §1.2; Fixed64
    re-verification per §7.3 at Stage 5+), fixed-size struct
    payloads, `EntityId` (per #16 §2 / #2 §2.5).
  - Forbidden: `string`, `class`, `IList<>`, any reference type,
    `UnityEngine.Object` references.
  - String-like data is represented by enum + ordinal lookup
    (e.g., player name is an `EntityId` not a string).
- 3.1.5 Anti-patterns:
  - Class-typed event ("violates KD-8 zero-allocation").
  - Reference-typed payload field ("breaks #16 §3.2.4.1 canonical
    serialization").
  - Tier-A event with `Vector3` field carrying continuous
    aggregate (use #16 §3.5 Tier-B classification instead).

### 3.2 Publish / Subscribe Semantics (FR-EVT-017 … 033)

- 3.2.1 Publish API surface (KD-4, KD-8):
  - `void EventBus.Publish<T>(in T evt) where T : struct, IEventA`
  - `void EventBus.Publish<T>(in T evt) where T : struct, IEventB`
  - `void EventBus.Publish<T>(in T evt) where T : struct, IEventC`
  - Three overloads, statically distinguished by tier marker
    interface. Phase-ownership check happens inside the IEventA /
    IEventB overloads at debug builds (asserts current phase ==
    `Events`); compiled out in release after Stage 0+1 lint catches
    misuse statically.
- 3.2.2 Subscribe API surface:
  - `SubscriptionToken Subscribe<T>(EventHandler<T> handler)` with
    same generic constraint trichotomy. Returned token is a struct
    (no class allocation).
  - `EventHandler<T>` is a `delegate void EventHandler<T>(in T evt);`
    declared in §4.2.
  - Subscriber registration is permitted at startup ONLY (no
    runtime register/unregister in the authoritative hot path).
    Runtime subscription for Tier C is permitted; for Tier A/B it
    is a design error and is rejected with `ERR_EVT_TIER_MISMATCH`
    if attempted post-init.
- 3.2.3 Queue mechanics:
  - Tier A / B: writes enter a pre-allocated ring buffer keyed by
    `(producingPhase, intraPhaseDrawIndex)`. Drain happens in the
    same tick's `Events` phase per #16 §3.6.1 WriteSet (the
    "event ledger" WriteSet).
  - Tier C: writes flow directly to the cosmetic channel with
    **immediate synchronous dispatch** — no delivery queue, no
    ring buffer. Subscribers fire on the publishing thread (single-
    threaded Stage 0 runtime per #16 §3.1). The only Tier C
    storage is the per-tick **publication-count table** (one
    counter per `eventTypeOrdinal`, reset at tick boundary) which
    feeds the §3.6.2 deterministic drop predicate; this table is
    not a delivery buffer and never holds payload bytes. When the
    drop predicate fires, the publish call is a no-op (subscribers
    are not invoked).
- 3.2.4 Intra-tick canonical order (KD-6 mechanics):
  - Order key: `(producingPhaseIndex, subsystemOrdinal, entityId,
    eventTypeOrdinal, intraPhaseDrawIndex)`.
  - `producingPhaseIndex` per #16 §3.1.2 phase index table.
  - `subsystemOrdinal` per #16 §3.1.1.
  - `entityId` ascending per #16 §3.1.1 array<T> ordering.
  - `eventTypeOrdinal` per Appendix A.
  - `intraPhaseDrawIndex` parallel to #16 §3.2.5.1 RNG intra-stream
    draw index.
  - **Counter scope (normative).** `intraPhaseDrawIndex` is a
    `ushort` counter scoped **per-tick, per-producingPhase**. Reset
    to zero at producing-phase entry; incremented monotonically on
    every Tier-A / Tier-B publish call within that phase regardless
    of producing subsystem. The (`producingPhaseIndex`,
    `subsystemOrdinal`, `entityId`, `eventTypeOrdinal`,
    `intraPhaseDrawIndex`) tuple is therefore unique within a tick
    by construction, satisfying §5 property P2 (sort-key
    total-order). Second-order publishes from inside the same-tick
    `Events`-phase dispatch (§3.2.5) reuse the `Events`-phase
    counter (which is itself fresh per tick), preserving the
    invariant under BFS dispatch up to `MAX_EVENT_DISPATCH_DEPTH`.
  - Sort is performed once at `Events`-phase entry against the
    accumulated tick queue; not on every publish.
  - This ordering is the *only* permitted iteration order over
    Tier A/B events within a tick; the subscriber-dispatch loop
    walks it.
- 3.2.5 Subscriber lifetime (FR-EVT-073 … 078):
  - Subscribers registered before first `Events` phase; dispatched
    in registration order (deterministic).
  - No re-entrant publish from inside a Tier A/B handler (handler
    enqueue from inside `Events` is permitted but the dispatcher
    drains breadth-first within the same phase; second-order events
    are processed in the *same* tick before phase exit). FIFO order
    over the second-order draws is preserved by `intraPhaseDrawIndex`
    incrementing on each enqueue.
  - Maximum dispatch depth: configurable (§3.10 constant
    `MAX_EVENT_DISPATCH_DEPTH = 8` `[GT]`). Exceeding bound →
    `ERR_EVT_QUEUE_OVERFLOW`.
  - Handler exceptions: Tier A/B → escalate (halt tick, write
    crash dump per Spec #16 `[TBD-CITE: tick-fail / crash-dump
    path; provisional anchor #16 §3.10 failure-mode table]`).
    Tier C → log + suppress.

### 3.3 Tick-Rate Split (FR-EVT-034 … 040) — KD-5 mechanics

- 3.3.1 Producing-phase / cadence map:
  | Event type (examples) | Producing phase | Cadence | Tier |
  |-----------------------|-----------------|---------|------|
  | `BallContactEvent` | Physics | 60 Hz | A | seeded |
  | `ShotExecutedEvent` | Resolve | 60 Hz (event-driven) | A | seeded |
  | `BallCrossedLineEvent` | Physics | 60 Hz | A | seeded |
  | `PressTriggeredEvent` | AI | 10 Hz (stride) | A | **future — populated at #13 IN REVIEW** |
  | `MarkAssignedEvent` | AI | 10 Hz | A | **future — populated at #14 IN REVIEW** |
  | `PossessionChangedEvent` | Resolve | event-driven | A | seeded |
  | `GoalAwardedEvent` | Resolve | event-driven | A | seeded |
  | `VfxImpactCue` | Resolve | event-driven | C | seeded |
  | `TickHeartbeatEvent` | Snapshot | 60 Hz | C | seeded |

  Rightmost column: `seeded` rows are present in the §2.4.2 initial
  registry (11 rows). `future` rows are listed as forward-looking
  examples of the AI-phase cadence model; they are NOT part of the
  Spec #17 v1.0 registry contract and must be appended to
  Appendix A by their owning specs at IN REVIEW time.
- 3.3.2 AI-stride interaction (KD-5):
  - On non-stride ticks, the `AI` phase is `AI_NoOp` (#16 §3.1.2).
    `AI_NoOp` MUST NOT publish Tier A/B events (its WriteSet is
    empty per #16 §3.6.1). It MAY publish a single `TickHeartbeatEvent`
    only if the implementation chooses to use that telemetry hook;
    if it does, the hook is Tier C and runs through the cosmetic
    channel, NOT the AI phase WriteSet.
  - Tier C diagnostic events from `AI_NoOp` are out-of-band by
    KD-4 and so do not violate #16's empty-WriteSet rule.
- 3.3.3 Tick-boundary determinism:
  - Authoritative events never cross tick boundaries on the
    authoritative path. Every queue entry is drained by end of
    same-tick `Events` phase. If a handler enqueues a second-order
    event, that event is dispatched in the same tick (3.2.5).
- 3.3.4 Anti-patterns:
  - Publishing a Tier A event from `Physics` and expecting
    same-phase delivery (Tier A delivery is in `Events`, never in
    `Physics`).
  - Cross-tick aggregation of Tier A counts on the publishing
    side (the ledger is the source of truth; aggregation lives in
    a subscriber, not a publisher).

### 3.4 Determinism Contracts & Digest (FR-EVT-027 … 033) — KD-6 mechanics

- 3.4.1 Citation: #16 §3.2.2 phase-digest formula owns the outer
  preimage. Spec #17 declares the inner serialisation of the
  `Events`-phase `phaseScopeFields`.
- 3.4.2 `phaseScopeFields` layout for the `Events` phase:
  ```
  PhaseScopeFields[Events] = SerializeCanonical(
      DOMAIN_TAG_EVENT_LEDGER ‖ EventLedgerRecord[T]
  )
  ```
  - `DOMAIN_TAG_EVENT_LEDGER` is a new domain-tag entry to be
    added to #16 §3.4 domain-tag table at #17 approval time
    (tagged `TBD-NORMATIVE`).
  - `EventLedgerRecord[T]` layout per §2.4.4.
- 3.4.3 Formula identifiers:
  - **FM-017-001** `EventLedgerDigestScope` (the §3.4.2 expression
    above). Cited by §3.4 and re-cited by §3.2.4.
  - **FM-017-002** `EventIntraTickSortKey`
    `= (producingPhaseIndex, subsystemOrdinal, entityId,
    eventTypeOrdinal, intraPhaseDrawIndex)`.
- 3.4.4 Worked example (deferred to Appendix B): canonical byte
  encoding of a 2-record event ledger for one tick.
- 3.4.5 Cross-spec citation guard (parallel to Spec #19 §3.6.1
  cite-precision guard): every "#16 §3.x.x" subsection-number
  citation in this spec MUST be re-grepped against current
  `deterministic-sim/section-3.md` at draft time. Numbers may have
  shifted across #16's adversarial passes.

### 3.5 Zero-Allocation Hot-Loop Mechanics (FR-EVT-048 … 054) — KD-8

- 3.5.1 Ring-buffer sizing: per-tick capacity `EVENT_QUEUE_CAPACITY
  = 1024` slots `[GT]`. Sized from §6.3 worst-case publish-rate
  analysis (full-match 90-min sim).
- 3.5.2 Subscriber-list storage: pre-allocated `EventHandler<T>[]`
  per event type. Capacity pinned at startup; resize is a
  pre-Stage-1 design error.
- 3.5.3 Cosmetic channel storage: Tier C dispatch is immediate-
  synchronous per §3.2.3 (no delivery queue). The only Tier C
  storage is a per-tick **publication-count table** sized to the
  ordinal-namespace width — fixed at `256` slots (one byte-wide
  ordinal per row), each holding a `u16` counter; total ~512 bytes,
  stack-allocatable per tick. Counter table is reset at the start
  of every tick. The aggregate per-tick publication ceiling
  `COSMETIC_PER_TICK_PUBLICATION_BUDGET = 4096` `[GT]` is a
  *sanity ceiling* (sum of per-ordinal `maxPerTick` values from
  Appendix A; §6.3 worst-case envelope), not a queue capacity.
- 3.5.4 Banned APIs in publish path (cross-listed with Spec #20):
  - `new T[…]`, `List<T>.Add` on hot-path lists,
    `IEnumerable<T>` foreach over reference enumerator,
    `Action<…>` (boxes), LINQ, `string.Format`, allocation-emitting
    interpolated strings, async/await, reflection in publish.
- 3.5.5 Verification: per-event allocation budget asserted in §5.3
  unit test (`Assert.AllocatedBytes(0)` per publish call).

### 3.6 Queue Overflow & No-Drop Policy (FR-EVT-041 … 047) — KD-7

- 3.6.1 Authoritative path (Tier A/B):
  - Queue is sized for §6.3 worst case + 4× headroom.
  - Overflow is a hard fail: `ERR_EVT_QUEUE_OVERFLOW` raised by
    `Publish<T>`, caller is responsible for crash handling (Spec
    #16 §X tick-fail path).
  - Overflow MUST NOT be recovered by drop on the authoritative
    path; recovery is via simulation halt and bug fix.
- 3.6.2 Cosmetic path (Tier C):
  - Per-event-type publication rate cap stored on the Appendix A
    registry row (`maxPerTick`). If exceeded, the publish call
    deterministically *drops* the event (not records it). The drop
    predicate is `(tick, eventTypeOrdinal, publicationCountThisTick
    > maxPerTick)` — *not* queue-depth-dependent, therefore
    replay-stable.
  - Drop is logged to the Tier C trace channel; does NOT enter the
    ledger.
- 3.6.3 Anti-pattern: a "soft drop" policy that reads queue depth
  at publish time. This is explicitly forbidden — drop predicates
  must be pure functions of `(tick, eventTypeOrdinal,
  publicationCountThisTick)`.

### 3.7 Versioning, Migration, Deprecation (FR-EVT-055 … 060) — KD-9

- 3.7.1 Registry row evolution rules (mechanics for §2.4.3 rule
  statements):
  - Adding a field: append to end; bump `payloadVersion`;
    Appendix A row updated; new row entry for old version retained
    for replay-corpus compatibility.
  - Field width change: forbidden in place; new ordinal required.
  - Field removal: forbidden in place; new ordinal required.
  - Tier change: forbidden in place; new ordinal required.
- 3.7.2 Migration semantics:
  - Replay corpus / fixture load encounters
    `(eventTypeOrdinal, oldVersion)`: replay still parses
    (Appendix A retains old version rows); subscriber sees an
    explicit version field and dispatches the right shape.
  - Replay corpus encounters
    `(eventTypeOrdinal, versionNewerThanCurrent)`: hard fail
    `ERR_EVT_VERSION_INCOMPATIBLE`.
- 3.7.3 Deprecation:
  - A deprecated ordinal is marked `DEPRECATED` in Appendix A but
    not deleted. Producers MUST NOT publish a deprecated ordinal
    in new code; consumers MAY still subscribe for replay-corpus
    compatibility.
- 3.7.4 Cross-spec ordering: an event added by a downstream spec
  (e.g., #10 `HeaderExecutedEvent`) gets its ordinal allocated at
  that spec's `IN REVIEW` time. Ordinal collision is prevented by
  the single-table registry in Appendix A.

### 3.8 Edge Cases (rule-application carve-outs)

- 3.8.1 Match-replay seeking: when the replay system jumps to a
  prior snapshot, the event ledger is reconstructed from the
  per-tick `EventLedgerRecord` field in `SnapshotPayload` (#16
  §3.2.3). Subscribers do NOT receive replayed events by default;
  replay-aware subscribers opt in via a separate replay-channel
  hook (`IReplayEventReader`, declared §4.2; Stage 1+ activated).
- 3.8.2 Save mid-tick: forbidden by #16 §3.7
  (`LEGAL_SAVE_BOUNDARIES = { EndOfSnapshot }`). Event ledger is
  always whole at save time.
- 3.8.3 Subscriber re-entry: a Tier A handler that publishes
  another Tier A event during dispatch is permitted; the new event
  is appended to the same-tick queue and dispatched after the
  current pass per §3.2.5 BFS rule. Maximum nesting per §3.2.5
  constant.
- 3.8.4 Multi-producer same-event same-tick: permitted; ordering
  resolves by §3.2.4 sort key. The per-tick-per-producingPhase
  `intraPhaseDrawIndex` counter (§3.2.4) makes the sort key unique
  by construction — identical-key collisions cannot occur, so no
  registration-order tiebreaker is needed.
- 3.8.5 Empty `Events` phase: digest contribution is the canonical
  empty-array byte string per #16 §3.2.4.1 `array<T>` rules
  (`00 00 00 00` for count). Phase digest is still emitted.
- 3.8.6 Cross-tier handler attempt: a class designed to handle
  both Tier A and Tier C streams MUST register twice with two
  different generic constraints; the dispatcher does not implicitly
  fan out.

### 3.9 Error Codes (cross-reference)

- Full error-code list in §3.10 constants catalogue with hex
  values and short descriptions. Each row cites the FR-EVT-### it
  catches and the §3.x rule it enforces.

### 3.10 Constants Catalogue

- Every numeric and identifier constant declared in this spec
  appears here with a source tag (`[GT]`, `[EST]`, `[FIXED]`,
  `[DERIVED]`, `[CROSS]`) per CLAUDE.md "Constant Tags".
- Expected rows:
  | Constant | Value | Tag | Notes |
  |----------|-------|-----|-------|
  | `EVENT_QUEUE_CAPACITY` | 1024 | `[GT]` | §3.5.1 / §6.3 |
  | `COSMETIC_PER_TICK_PUBLICATION_BUDGET` | 4096 | `[GT]` | §3.5.3 / §6.3 — aggregate publication ceiling, NOT a delivery queue (Tier C is immediate-dispatch per §3.2.3) |
  | `MAX_EVENT_DISPATCH_DEPTH` | 8 | `[GT]` | §3.2.5 |
  | `EVENT_TYPE_ORDINAL_WIDTH` | 1 byte | `[GT]` | §3.1.2; design decision (not a physical constant); Stage 5+ expansion in §7.3 |
  | `PAYLOAD_VERSION_WIDTH` | 1 byte | `[GT]` | §3.1; §3.7; design decision |
  | `DOMAIN_TAG_EVENT_LEDGER` | (TBD-NORMATIVE; allocated in #16 §3.4 at #17 IN REVIEW — see ERR-017-001) | `[CROSS-PENDING]` | §3.4.2; KD-2 qualifier — promoted to `[CROSS]` when #16 reaches `APPROVED` |
  | `ERR_EVT_QUEUE_OVERFLOW` | `0x1701` | `[GT]` | §2.5 / §3.6.1 — error-code allocation from `0x17NN` reserved block; designer-chosen, locked at approval |
  | `ERR_EVT_TIER_MISMATCH` | `0x1702` | `[GT]` | §2.5 / §3.2.5 |
  | `ERR_EVT_ORDINAL_UNKNOWN` | `0x1703` | `[GT]` | §2.5 / §3.7.2 |
  | `ERR_EVT_VERSION_INCOMPATIBLE` | `0x1704` | `[GT]` | §2.5 / §3.7.2 |
- Error-code numeric pins (`0x17NN`) are reserved for Spec #17;
  must not collide with #16's `0x16NN` block.
- Constants live in their designated `.cs` constant catalogues at
  implementation time (CLAUDE.md "Constant Tags" rule); spec
  declares only the values and tags.

### 3.11 Version History

---

## SECTION 4 — ARCHITECTURE & INTEGRATION (`section-4.md`)

### 4.1 Module Layout (Stage 1 target shape)

- `src/event-system/EventBus.cs` — publish/subscribe entry points.
- `src/event-system/EventLedger.cs` — Tier A/B ring buffer +
  per-tick serialisation.
- `src/event-system/CosmeticChannel.cs` — Tier C immediate-
  synchronous dispatch + per-tick publication-count table (no
  delivery queue per §3.2.3 / §3.5.3).
- `src/event-system/EventRegistry.cs` — Appendix A registry,
  generated from spec at build time (Stage 1 build step).
- `src/event-system/EventConstants.cs` — §3.10 catalogue, generated.
- Per-spec event structs live with their owning spec
  (`src/shot-mechanics/ShotExecutedEvent.cs` etc.) per Spec #20
  layout. Spec #17 does NOT own those files.

### 4.2 Interface Contracts (this spec exposes)

Per CLAUDE.md "Interface Design Principle" — declared only because
both producer and consumer sides are specified here:

- `interface IEventA` — empty marker for Tier A event structs.
- `interface IEventB` — empty marker for Tier B event structs.
- `interface IEventC` — empty marker for Tier C event structs.
- `delegate void EventHandler<T>(in T evt) where T : struct;`
- `struct SubscriptionToken` — opaque handle for `Unsubscribe`.
- `interface IReplayEventReader` — Stage 1+ (KD-12 stage-gated);
  declared at Stage 1 alongside replay-tool consumer (deferred per
  CLAUDE.md "Interface Design Principle" because the replay
  tool's consumer side is unspecified at Stage 0).

Spec #17 intentionally does NOT declare:
- `IEventPublisher` — would be a phantom interface (only one
  concrete `EventBus`).
- `ITransport` — Stage 5+ multiplayer (KD-10); no consumer
  specified at Stage 0.

### 4.3 Subscriber Registration Model

- Static registration during boot phase (before first `Events`
  phase). `EventBus.RegisterStartupSubscribers(...)` invoked once
  from match-init.
- Per-event-type subscriber arrays sized at registration; no
  resize after init for Tier A/B.
- Tier C runtime subscribe permitted via separate API
  (`CosmeticChannel.Subscribe<T>`) — UI / VFX systems use this.

### 4.4 Phase Integration

- `EventBus.DrainTick()` is called by the `Events` phase scheduler
  (#16 §3.1.2) at the boundary between `Resolve` and `Snapshot`.
- `EventBus.SerializeLedger(in Span<byte> dst)` is called by
  `Snapshot` phase to emit `EventLedgerRecord` bytes into
  `SnapshotPayload` (#16 §3.2.3 / §3.9.2).
- Both calls are no-allocation. The `Snapshot` phase serializer
  passes a pre-allocated payload buffer.

### 4.5 File / Module Manifest

- Manifest update to `docs/tracking/file-manifest.md` at spec
  approval (deferred — populated when section files are drafted).

### 4.6 Version History

---

## SECTION 5 — TEST PLAN (`section-5.md`)

### 5.1 Test Strategy

- Spec #17 publishes its FRs (§2.2). This section maps every FR to
  its verification mechanism.
- Test framework / runner per Spec #19 §3 (cited; not chosen here).
- Stage 0: manual review against §3 mechanics.
- Stage 0+1: tooling activates per FR's "Activation stage" column
  in §2.2 (KD-12).

### 5.2 Stage-Gated Activation Table (KD-12)

- Per-FR table: `FR-EVT-### | Stage 0 status | Activation stage |
  Activation criterion`.
- Most FRs read "Stage 0+1" with criterion "first `src/event-system/`
  code committed".
- A few read "Stage 0" with criterion "applies to spec drafts now":
  FR-EVT-001 … 008 (typed-contract rules — checkable against
  Appendix A registry rows) and FR-EVT-055 … 060 (versioning rules —
  enforceable at Appendix A row authoring).

### 5.3 Test Catalogue (target ratios per Spec #19 §3.1.2 `TBD-NORMATIVE` per KD-2 — resolves outline finding 12)

| Layer | Target count | Examples |
|-------|--------------|----------|
| Unit | ≥ 60% | Publish/subscribe correctness; ordinal allocation; struct-layout reflection; per-publish allocation = 0 bytes assertion; ring-buffer capacity edge; canonical sort key (FM-017-002) |
| Integration | ≤ 25% | EventBus + EventLedger round-trip; serialize→snapshot→load→re-deserialize; phase-WriteSet ownership check; tier-mismatch rejection at registration |
| Simulation | ≤ 12% | Full-match (90-min) run; verify zero allocations after warm-up; verify ledger digest matches expected golden; verify event count ≤ §6.3 budget |
| End-to-end / soak | ≤ 3% | 1-hour soak; verify no queue-depth drift across replays; KD-7 no-drop assertion across full match |

- Property tests (Spec #19 §3.4):
  - **P1** publish then subscribe → handler receives identical
    bytes (idempotence).
  - **P2** sort-key total-order property over §3.2.4 key tuple.
  - **P3** version-migration property: any
    `(ordinal, payloadVersion)` in Appendix A registry deserialises
    to its expected shape.
- Determinism golden:
  - **G1** golden phase digest for a 60-second scripted match
    against `EventLedgerRecord` (Tier A delivery only).
  - **G2** golden cosmetic-channel drop record for same match
    (Tier C; not part of authoritative digest).

### 5.4 FR-to-Verification Traceability

- Single table indexed by FR-EVT-###; columns:
  `Verification Mechanism | Tooling | Activation Stage | Output Artifact`.
- Stage 0 rows resolve to "manual review against §3 mechanics or
  Appendix A row" — acknowledged degenerate (parallel to Spec #20
  §5.5 / Spec #19 §5.6 acknowledgement).

### 5.5 Determinism Test Consumption (binds to #16 §7 / Spec #19 §3.2)

- Spec #17 does NOT operate its own determinism regression tier
  (KD-2). The G1 / G2 golden tests above feed the #16 §7
  regression suite via the Spec #19 §3.4.3 capture-and-promote
  path.
- Boundary review check: any change to #16 §3.1.2 phase ordering,
  §3.2.2 digest formula, §3.6.1 WriteSet table, or §3.9.2 snapshot
  layout triggers a Spec #17 §3.4 / §4.4 review.

### 5.6 Test-Data Fixtures (binds to Spec #19 §3.8 / KD-10)

- Golden event-ledger fixtures stored at
  `tests/data/event-system/golden/<scenario>.fixture` per Spec #19
  §3.8.2 layout.
- Each fixture conforms to #16 §5 canonical save format (Spec #19
  KD-10 binding).
- Fixture provenance recorded per Spec #19 §3.8.4.

### 5.7 Version History

---

## SECTION 6 — PERFORMANCE ANALYSIS & BUDGETS (`section-6.md`)

> **Slot reconciliation:** This section IS the template's
> "Performance Analysis" slot (resolves outline finding 2; the v1.0
> `outline.md` put error handling here, which violated CLAUDE.md
> 9-section template).

### 6.1 Complexity Analysis

- Publish (Tier A/B): O(1) amortised. Single ring-buffer slot
  write + counter increment.
- Publish (Tier C): O(handlers) — immediate-dispatch over
  pre-allocated subscriber array.
- `DrainTick`: O(n log n) where n = events-per-tick; the
  intra-tick sort (§3.2.4) dominates. n bounded by
  `EVENT_QUEUE_CAPACITY` (1024).
- `SerializeLedger`: O(n) over Tier A/B records.

### 6.2 Allocation Budget (KD-8)

- Per publish: 0 bytes (asserted §5.3 unit test).
- Per `DrainTick`: 0 bytes (sort is in-place over a stackalloc
  sort scratch buffer sized at constant capacity).
- Per `SerializeLedger`: 0 bytes (writes to caller-provided
  `Span<byte>`).
- Per startup subscriber registration: O(handler-count) bytes,
  one-time, off hot path.

### 6.3 Worst-Case Publish-Rate Analysis (sizes the constants in §3.10)

- Tier A worst case at 60 Hz (full match, peak action):
  - Per physics tick: `BallContactEvent` (≤ 2), other Tier A
    (≤ 6). Margin × 4 for unforeseen.
  - Per tactical tick (10 Hz, every 6th tick): AI events (≤ 22 —
    one per player). Margin × 2.
  - Aggregate per-tick ceiling: ≤ 64.
- Tier C worst case at 60 Hz (peak VFX):
  - Per physics tick: ≤ 32 VFX cues + ≤ 16 UI notifications.
  - Aggregate ≤ 256 / tick under stress.
- Tier A ring-buffer sizing: `EVENT_QUEUE_CAPACITY = 1024`. Derived
  as: 64 first-order events × `MAX_EVENT_DISPATCH_DEPTH` (8) = 512
  worst-case BFS fanout; doubled for unforeseen growth = 1024.
  Headroom is therefore ×2 over the dispatch-depth-bounded worst
  case, not ×16 over the first-order ceiling alone.
- Tier C publication-budget sizing: Tier C has no delivery queue
  (§3.5.3); `COSMETIC_PER_TICK_PUBLICATION_BUDGET = 4096` is the
  aggregate per-tick publication ceiling (×16 safety margin over
  256) — used only as a sanity bound on the sum of per-ordinal
  `maxPerTick` rows in Appendix A.
- Numbers are `[GT]`; revisited at Stage 0+1 against first real
  measurements (parallel to Spec #20 §5.3 numeric re-tuning).

### 6.4 Frame-Budget Contribution (binds to #16 §6 / Spec #18 §4)

- Spec #16 §6 budget table currently allocates "Resolve + Events =
  18% `TBD-NORMATIVE` per KD-2" of frame budget. Spec #17 declares
  its share:
  - `DrainTick` ≤ 0.3 ms / frame (60 Hz target = 16.67 ms/frame).
  - `SerializeLedger` ≤ 0.2 ms / frame.
  - Combined ≤ 3% of frame budget (well within the 18%
    Resolve+Events combined allocation; Resolve's share owned by
    its parent spec, not #17).
- Cited from #16 §6.2 (TBD-NORMATIVE per KD-2). Performance
  regression gate thresholds OWNED BY Spec #18 §4 (KD-3 / Spec #19
  KD-3 parallel).

### 6.5 Instrumentation Budget (KD-11; binds to #16 §8.2)

- Per-publish instrumentation cost: ≤ 16 bytes of trace-channel
  output (one entry per Tier A publish; Tier C aggregated).
- Trace channel names declared in §5; channel format cited from
  #16 §8.
- Total event-system instrumentation footprint per match: ≤ 2 MB
  uncompressed at peak event rate (well inside #16 §8.2 envelope
  once it pins).

### 6.6 Profiling Plan

- Stage 1 deliverable: per-publish microbenchmark suite (BenchmarkDotNet
  or equivalent — Spec #18 pins the tool).
- Stage 1 deliverable: full-match profile with allocation tracker
  asserting zero allocations after warm-up.

### 6.7 Version History

---

## SECTION 7 — FUTURE EXTENSIONS (`section-7.md`)

### 7.1 Stage 0+1 Transition Deliverables

- `src/event-system/` initial implementation.
- Appendix A registry generated as `EventRegistry.cs`.
- Per-publish allocation assertion test.
- Golden ledger fixture for 60-second scripted scenario.
- KD-12 stage-gated FRs flipped to "active".

### 7.2 Stage 1 Deliverables

- Replay-event reader (`IReplayEventReader`) — interface declared
  once the replay tool's consumer side is specified (CLAUDE.md
  "Interface Design Principle").
- Per-tier dashboards for event-rate monitoring.
- Stage-1 second-order dispatch profile (validate §3.2.5 BFS
  depth claim).

### 7.3 Stage 5+ Extensions (resolves outline finding 7; KD-10 mechanics)

- Two-byte `eventTypeOrdinal` expansion if registry exceeds 256
  rows (currently allocated through ordinal `0x0B`; 244 free).
- Wire-format design: per-event framing, compression, ack
  semantics. Out of scope at Stage 0.
- Networked event multiplexing: which Tier A events are
  authoritative-server-only vs client-replicable. Out of scope at
  Stage 0.
- Lossy Tier C transport (UDP-style) for cosmetic cues over the
  network. Out of scope at Stage 0.
- Fixed64 (Spec #9) re-verification of event payload arithmetic
  fields when Spec #9 migrates the engine off `float` (Stage 5+).

### 7.4 Permanent Exclusions

- "Soft drop" policy on Tier A/B paths — never permitted (KD-7).
- Runtime register/unregister of Tier A/B subscribers — never
  permitted (§3.2.2 anti-pattern).
- Class-typed events — never permitted (KD-8).
- Cross-tick aggregation on the publisher side — never permitted
  (§3.3.4 anti-pattern).

### 7.5 Deferred Decisions Tracker

- D1 — Microbenchmark tool pin — Stage 0+1 (Spec #18 dependency).
- D2 — Trace-channel binary format — Stage 0+1 (Spec #16 §8
  dependency).
- D3 — Replay-event-reader interface — Stage 1 (CLAUDE.md
  interface principle).
- D4 — Multiplayer wire format — Stage 5+ (KD-10).
- D5 — Ordinal width expansion trigger date — Stage 1+ (when
  registry approaches 200 rows).
- D6 — Per-event trace-channel verbosity defaults — Stage 0+1.

### 7.6 Version History

---

## SECTION 8 — REFERENCES & CITATION AUDIT (`section-8.md`)

> **Slot reconciliation:** This section IS the template's
> "References" slot (resolves outline finding 2; the v1.0
> `outline.md` put multiplayer compatibility here, which violated
> CLAUDE.md 9-section template — multiplayer is now §7.3).

### 8.1 Source Register

- Root `CLAUDE.md` (project invariants; "When Writing Code"
  rules; "Heartbeat Tick Rate"; "Interface Design Principle").
- Spec #16 (Deterministic Simulation) — §1.3 tier classification,
  §3.1 phase pipeline, §3.2 digests, §3.6.1 phase WriteSet table,
  §3.9.2 on-disk snapshot layout, §6 frame budget, §8 trace
  channels.
- Spec #6 (Shot Mechanics) — §2.4 / §4 `ShotExecutedEvent`
  reference payload.
- Spec #18 (Performance Optimization) — §4 / §7 performance gates.
- Spec #19 (Testing Strategy) — §3.1 pyramid, §3.4 property tests,
  §3.8 fixture governance, §3.2 / §3.4.3 determinism-suite
  consumption + capture path.
- Spec #20 (Code Standards) — §3.x zero-allocation lint rules and
  struct-event pattern example (already cites
  `ShotExecutedEvent`).
- `docs/planning/development-best-practices.md`.
- `docs/planning/master-development-plan.md`.
- RFC 2119 (MUST / SHOULD / MAY).

### 8.2 Verification Notes

- Every CLAUDE.md citation in §3 verified against current
  CLAUDE.md text on this spec's drafting date.
- Every #16 citation verified against current
  `deterministic-sim/section-X.md` per `SPEC_INDEX.md`.
- Every #16 citation tagged `TBD-NORMATIVE` per KD-2 until #16
  reaches `APPROVED`.
- `ShotExecutedEvent` field-list cited from
  `shot-mechanics/section-2-7-to-2-9.md` (Shot Mechanics #6 §2.4).

### 8.3 Cross-Spec Citation Audit

- Spec #17 is **cited by** every downstream spec that names an
  event (#10, #11, #13–#15, Statistics Engine).
- Spec #17 cites #16 (substantive: phases, digests, WriteSet,
  tier vocabulary), #6 (event-payload reference), #18 (boundary:
  perf gates), #19 (boundary: testing governance), #20 (boundary:
  banned-API enforcement).
- `[CROSS]` constants imported: `DOMAIN_TAG_EVENT_LEDGER` from #16
  §3.4 domain-tag table (TBD-NORMATIVE allocation at #17
  approval).
- Cross-reference IDs declared by this spec: FM-017-001,
  FM-017-002, EC-017-001 … 006 (§3.8 edge cases), ERR-017-NNN
  (none open at draft time).

### 8.4 Constant Provenance Summary

- All `[GT]` constants (queue capacities, dispatch depth, error
  codes) have rationale recorded in §3.10 and §6.3.
- One `[CROSS]` constant (`DOMAIN_TAG_EVENT_LEDGER`) cites #16 §3.4.
- No `[EST]` constants at draft time.
- All `[FIXED]` constants (ordinal width, version width, error
  codes) have one-line justification at §3.10 row.

### 8.5 Version History

---

## SECTION 9 — APPROVAL CHECKLIST (`section-9-approval-checklist.md`)

### 9.1 Content Checklist

- All required sections present (incl. template-slot reconciliation
  in §6 / §8).
- All FR-EVT-### present in §2.2 with conformance level and
  activation stage.
- KD-1 … KD-12 each codified in at least one §3 / §4 / §5 / §6
  subsection.
- Boundary statements with #16 §3 (KD-2) and #19 §3 (KD-11
  testing) explicit.
- Event registry (Appendix A) populated with at least the 11
  initial rows in §2.4.2.

### 9.2 Quality Checklist

- Cite-not-redefine rule audited (no #16 / #18 / #19 / #20
  restatements).
- Every FR row resolves to a §5.x verification mechanism.
- Every approval-checklist row in *this* checklist cites either
  a file path or a check name (KD-6 self-application from Spec
  #19 KD-6).
- All cross-references (XC-/FM-/EC-/ERR-) resolve.
- All `TBD-NORMATIVE`-tagged citations of #16 (KD-2) and #18
  (KD-3) enumerated; outstanding tags listed for the reviewer.
- Tier classification consistent across §2.4 / §3.1.3 / Appendix A
  (every event has exactly one tier).
- Ordinal uniqueness verified across Appendix A registry rows.
- Error-code numeric pins (`0x17NN`) verified against #16 `0x16NN`
  block for non-collision.

### 9.3 Review Checklist

- Open issues logged in `CLAUDE.md` "OPEN ISSUES" if any.
- Lead-developer sign-off captured.
- `spec-error-log.md` updated with any cross-spec drift discovered
  during drafting. ERR-017-001 (`DOMAIN_TAG_EVENT_LEDGER` allocation
  back-prop into #16 §3.4) filed May 12, 2026; closure tracked at
  #17 IN REVIEW commit.
- `SPEC_INDEX.md` status updated atomically with sign-off.
- KD-2 sequencing constraint satisfied (#17 reaches `IN REVIEW`
  → #16 reaches Tier 2 `APPROVED` → #17 advances to `APPROVED`).

### 9.4 Decision

- Status block (`IN REVIEW` / `APPROVED` / `SUSPENDED` /
  `DEFERRED`).
- Approval evidence: file paths to programmatically-verifiable
  sources (Spec #19 KD-6 self-application — every row of this
  checklist must comply).
- Evidence-artifact convention for `[GT]` governance numbers
  parallel to Spec #19 §9.4 L5: section-file citation IS the
  evidence; auditor confirms literal number is present at cited
  path.

---

## APPENDICES (`appendices.md`)

- **Appendix A — Event Type Registry.**
  Full table of every event type ever published. Columns:
  `Ordinal | Type | Tier | ProducerPhase | OwningSpec |
  CurrentVersion | PayloadFieldList | DeprecatedY/N`. Initial rows
  per §2.4.2 (11 rows). Schema for downstream specs to append
  rows at their own `IN REVIEW` time.

- **Appendix B — Canonical Byte Encoding Worked Examples.**
  - B.1 — Empty `Events` phase (`count = 0`).
  - B.2 — Single-event ledger (`ShotExecutedEvent` only).
  - B.3 — Two-event mixed-producer ledger (`Physics`-produced
    `BallContactEvent` + `Resolve`-produced
    `PossessionChangedEvent`); demonstrates §3.2.4 sort key.
  - Each example shows preimage bytes per #16 §3.2.4.1
    SerializeCanonical rules.

- **Appendix C — Versioning Migration Recipes.**
  - Recipe 1: adding a payload field — Appendix A row update +
    `payloadVersion` bump.
  - Recipe 2: changing a field width — new ordinal allocation;
    old ordinal marked deprecated.
  - Recipe 3: deprecating an event type — ordinal retained, all
    new code must not publish.

- **Appendix D — Glossary.**
  Spec #17-specific terms only: event ledger, cosmetic channel,
  `eventTypeOrdinal`, `payloadVersion`, second-order dispatch.
  Phase / digest / tier vocabulary cited from #16.

- **Appendix E — Failure-Mode Decision Table.**
  Edge-case ID `EC-017-NNN | Trigger | Behaviour | Error code`
  parallel to #16 §3.10 table. Populated at draft time with
  EC-017-001 … 006 covering: Tier A from non-Events phase, queue
  overflow, ordinal unknown on load, version newer than registry,
  cross-tier subscription, dispatch-depth exceeded.

---

## VERSION HISTORY

| Version | Date         | Author      | Notes                                                                                                         |
|---------|--------------|-------------|---------------------------------------------------------------------------------------------------------------|
| 1.0     | May 12, 2026 | Claude Code | Initial detailed outline drafted from `outline.md` v1.0. Addresses all 12 findings from May 6, 2026 adversarial review. Resolution map below. |
| 1.1     | May 12, 2026 | Claude Code | PASS 2 ADVERSARIAL REVIEW applied (4H / 6M / 5L). All H and M findings resolved in-place; L findings addressed in target subsections. ERR-017-001 filed in `spec-error-log.md` for #16 §3.4 domain-tag back-prop. `SPEC_INDEX.md` row 17 advanced to `IN PROGRESS`. KD-2 expanded to cover #19 TBD-NORMATIVE and `[CROSS-PENDING]` qualifier convention. KD-3 Tier-B Interface Design Principle justification added. §3.2.4 `intraPhaseDrawIndex` counter scope pinned (per-tick-per-producingPhase). |

---

## ADVERSARIAL-REVIEW FINDINGS RESOLUTION MAP

Traceability — every finding in `outline.md` adversarial review
section is resolved by a specific subsection above.

| Finding | Severity | Resolved by |
|---------|----------|-------------|
| 1 — Missing metadata header | H | Top of this file |
| 2 — Section plan deviates from CLAUDE.md template (errors in §6, multiplayer in §8) | H | §6 IS performance analysis (slot-reconciliation note in header); §8 IS references (slot-reconciliation note in header); §7.3 holds Stage 5+ multiplayer; §3.9 / §3.10 holds error codes |
| 3 — Boundary with Deterministic Simulation #16 unstated (event ordering, immediate vs queued, dropped-event policy) | H | KD-2 (citation map); §3.1 (typed contracts within #16 `Events` phase); §3.2 (publish path tied to #16 §3.6.1 WriteSet); §3.3 (tick-rate split per #16 §3.1.2); §3.4 (FM-017-001 inner-digest formula keyed to #16 §3.2.2); §4.4 (phase-integration entry points) |
| 4 — Dropped-event policy conflicts with replay determinism | H | KD-7; §3.6 splits authoritative-no-drop (hard fail `ERR_EVT_QUEUE_OVERFLOW`) from cosmetic deterministic-drop predicate (pure function of `tick / ordinal / publicationCountThisTick`, not queue depth) |
| 5 — Existing event types not enumerated | H | §2.4.2 initial registry (11 rows incl. `ShotExecutedEvent`, `BallContactEvent`, `PossessionChangedEvent`, `GoalAwardedEvent`, etc.); Appendix A schema |
| 6 — Allocation-free path policy underspecified | M | KD-8 (commits to `readonly struct`, `in`-ref publish, ring-buffer subscriber list, no closures); §3.5 mechanics; §6.2 budget |
| 7 — Multiplayer compatibility scoped wrong | M | KD-10 "do not preclude"; §7.3 Stage 5+ explicit; §8 freed up for references |
| 8 — Versioning strategy in appendix only | M | KD-9; promoted to §2.4 (data structure) + §3.7 (mechanics); Appendix A holds registry table, not rules |
| 9 — Tick-rate split unstated | M | KD-5; §3.3 producing-phase / cadence map; §3.3.2 AI-stride interaction |
| 10 — Authoritative-vs-cosmetic distinction missing | M | KD-3 (Tier A / B / C bound to #16 §1.3.1); KD-4 (publish path separation); §3.2.1 publish-API trichotomy; §3.6 split drop policy |
| 11 — Instrumentation budget not cited | L | KD-11 binding to #16 §8.2; §5 trace-channel registry; §6.5 per-publish instrumentation cost |
| 12 — Test ratios unstated | L | §5.3 explicit pyramid-percentage table parallel to Spec #19 §3.1.2 |

---

## PASS 2 ADVERSARIAL REVIEW — May 12, 2026

> Reviewer: AI agent (`claude/review-event-system-specs-WPnsN`). Scope:
> `outline-detailed.md` v1.0 measured against `CLAUDE.md`, the 9-section
> template, Deterministic Simulation #16 (IN PROGRESS), Spec #19 (IN
> REVIEW), Spec #20 (APPROVED), and `SPEC_INDEX.md`.
> Severity legend: **H** blocks section-file authoring; **M** must
> resolve during section-file draft; **L** follow-up before §9 sign-off.
> All findings below are resolved in v1.1 of this file.

### Verified premises
- `SPEC_INDEX.md` row 17 was `NOT STARTED` at review time; advanced to
  `IN PROGRESS` in v1.1 atomically with this review block.
- All 12 May 6 (PASS 1) findings demonstrably addressed in
  `FINDINGS RESOLUTION MAP` above.
- 12-byte header layout sums correctly (1+1+2+4+2+2 = 12 bytes).
- Error-code `0x17NN` block does not collide with #16 `0x16NN`.

### Findings (all resolved in v1.1)

1. **[H] `Status: DRAFT` in header contradicted `SPEC_INDEX.md`
   `NOT STARTED`.** Pure-tracking mismatch; same class as the
   "fabricated checklist values" trap in CLAUDE.md. **Resolved:**
   `SPEC_INDEX.md` row 17 advanced to `IN PROGRESS` in the same
   revision that lands this v1.1; header status sync note added.

2. **[H] Tier B was a phantom-interface surface at Stage 0.** KD-3
   declared Tier B "not expected to populate at Stage 0" but §3.2.1 /
   §4.2 / §6.2 referenced `IEventB` machinery throughout. Matched the
   ERR-001/004 phantom-interface failure pattern. **Resolved:** KD-3
   amended with explicit Interface Design Principle justification —
   tier vocabulary is owned by #16 §1.3.1, both sides of `IEventB`
   are specified here (publisher = `EventBus.Publish<T>` overload;
   consumer = `EventLedger` dispatcher + #16 §3.5 Tier-B tolerance
   application path).

3. **[H] `DOMAIN_TAG_EVENT_LEDGER` allocation created a chicken-and-egg
   with #16 §3.4.** No mechanism existed for #17 to register a
   domain-tag need with #16. **Resolved:** ERR-017-001 filed in
   `spec-error-log.md` parallel to the ERR-016-002 back-prop precedent
   (`XC-002-001` / `XC-008-001` pattern); §3.10 row updated to
   `[CROSS-PENDING]` with ERR-017-001 anchor; §9.3 references the
   ERR row.

4. **[H] `intraPhaseDrawIndex` reset / assignment semantics
   underspecified — broke sort-key total-order property P2.** With
   BFS second-order dispatch up to depth 8, the per-phase counter
   scope was ambiguous and re-entrant publishes could collide.
   **Resolved:** §3.2.4 amended with normative counter-scope
   declaration (per-tick, per-producingPhase, reset to zero at
   producing-phase entry, monotonic increment across all
   subsystems within that phase); §3.8.4 simplified accordingly
   (no registration-order tiebreaker needed — sort key is unique
   by construction).

5. **[M] §3.2.5 cited "Spec #16 §X tick-fail path" with `§X`
   unfilled.** Same hazard as fabricated checklist values.
   **Resolved:** replaced with explicit `[TBD-CITE]` and provisional
   anchor (#16 §3.10 failure-mode table) per the project's
   TBD-citation precedent.

6. **[M] §6.4 cited "#16 §6 Resolve + Events = 18%" as if pinned.**
   #16 is `IN PROGRESS`. **Resolved:** 18% tagged `TBD-NORMATIVE`
   inline in §6.4 to match KD-2 convention.

7. **[M] §3.3.1 cadence-map table conflated initial-registry events
   with future-spec events** (`PressTriggeredEvent`,
   `MarkAssignedEvent`). **Resolved:** added "Status" rightmost
   column distinguishing `seeded` (in §2.4.2 v1.0 registry) from
   `future — populated at #N IN REVIEW` rows; clarifying paragraph
   appended.

8. **[M] `[FIXED]` tag was misapplied to error codes and ordinal
   widths in §3.10.** Per CLAUDE.md "Constant Tags",  `[FIXED]` is
   physics-derived; error codes and protocol widths are designer-set.
   **Resolved:** `ERR_EVT_*` and `EVENT_TYPE_ORDINAL_WIDTH` /
   `PAYLOAD_VERSION_WIDTH` retagged `[GT]` with rationale notes.

9. **[M] `[CROSS]` tag was used for `DOMAIN_TAG_EVENT_LEDGER`
   despite #16 not being `APPROVED`.** **Resolved:** new
   `[CROSS-PENDING]` qualifier introduced in KD-2 paired with
   `TBD-NORMATIVE`; promotion to `[CROSS]` occurs at #16
   `APPROVED`. §3.10 row updated.

10. **[M] §5.3 test-pyramid ratios cited Spec #19 without
    `TBD-NORMATIVE`.** #19 is `IN REVIEW`. **Resolved:** §5.3
    heading tagged `TBD-NORMATIVE per KD-2`; KD-2 expanded with a
    Spec #19 status caveat.

11. **[L] §6.3 worst-case queue derivation omitted BFS dispatch-depth
    fanout.** **Resolved:** derivation rewritten as `64 × 8 = 512
    worst-case BFS fanout`, doubled to 1024; ×2 headroom over the
    dispatch-depth-bounded worst case (not ×16 over first-order).

12. **[L] §3.1.4 `Vector3` precision not pinned to Stage 0 float.**
    **Resolved:** §3.1.4 amended to cite Ball Physics #1 §1.2 and
    point at §7.3 for Fixed64 re-verification.

13. **[L] §2.4.1 `[StructLayout(LayoutKind.Sequential)]` was ambiguous
    between in-memory and canonical-serialization layouts.**
    **Resolved:** §2.4.1 amended with explicit canonical-vs-in-memory
    clarification; `Pack = 1` ruled out; §3.4.2 `SerializeCanonical`
    is the only authoritative byte source.

14. **[L] Appendix A registry schema lacked a "first-published-in"
    audit column.** Needed for long-term deprecation traceability
    given KD-9 retains deprecated rows indefinitely. **Resolved:**
    column added to §2.4.2 table; 11 seed rows populated.

15. **[L] No `spec-error-log.md` entry was filed for the #16 §3.4
    domain-tag back-prop need (finding 3 above).** **Resolved:**
    ERR-017-001 filed in `docs/tracking/spec-error-log.md` parallel
    to ERR-016-002 precedent.

### Recommended next steps
- Section-file authoring may now proceed against this v1.1 outline.
- During `section-3.md` drafting, re-grep #16 subsection numbers
  against current `deterministic-sim/section-3.md` (per §3.4.5
  cite-precision guard) — #16 is `IN PROGRESS` and section numbers
  may have shifted.
- At #17 IN REVIEW commit, submit the patch to #16 §3.4 domain-tag
  table referenced by ERR-017-001.

