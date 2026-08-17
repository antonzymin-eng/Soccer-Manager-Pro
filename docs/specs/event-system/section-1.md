# Event System Specification #17 — Section 1: Purpose & Scope

**Created:** May 13, 2026
**Last Updated:** May 13, 2026
**Version:** 1.0.1
**Status:** DRAFT — section-file authoring per `outline-detailed.md` v1.1.
`SPEC_INDEX.md` row 17 = `IN PROGRESS`.
**Companion documents:** `outline.md` (v1.0 + May 6 adversarial review),
`outline-detailed.md` (v1.1 + May 12 PASS 2 adversarial review).

---

## 1.1 What This Specification Covers

Spec #17 defines the typed event-ledger architecture that the
Deterministic Simulation `Events` phase (#16 §3.1.2 `TBD-NORMATIVE`
per KD-2) operates against. It governs every gameplay event produced
or consumed by Stage 0+ subsystems, the byte-level layout that those
events present to the per-tick digest (#16 §3.2.2 `TBD-NORMATIVE`),
and the publish/subscribe machinery that delivers them.

Governance areas:

1. Typed event-payload contracts (`readonly struct`, header field
   order, `eventTypeOrdinal`, `payloadVersion`).
2. Tier classification rule (Tier A / B / C; KD-3 — vocabulary cited
   from #16 §1.3.1 `TBD-NORMATIVE`).
3. Publish/subscribe semantics + intra-phase ordering (KD-4, KD-6).
4. Zero-allocation hot-loop guarantees (KD-8).
5. Queue-overflow / no-drop policy on authoritative paths (KD-7).
6. Event-contract versioning rules (KD-9).
7. Instrumentation / trace-channel registry (KD-11; binds to #16
   §8.2 `TBD-NORMATIVE`).
8. Stage 5+ "do not preclude" multiplayer constraints (KD-10).

**Applicability:**

- **Primary:** every event-publishing or subscribing call site in
  Stage 0 gameplay code (activated at Stage 0 → Stage 1 transition
  per KD-12).
- **Secondary:** every spec from #1–#20 that names an event in its
  §2.4 / §4 surface. Examples already published: Shot Mechanics #6
  `ShotExecutedEvent` (§2.4 / §4). Examples scheduled: Heading
  Mechanics #10 `HeaderExecutedEvent`, Goalkeeper Mechanics #11
  `SaveAttemptedEvent` / `BallParriedEvent` / `BallCaughtEvent`,
  AI specs #13–#15 `PressTriggeredEvent` / `MarkAssignedEvent` /
  `RunCalledEvent`, Statistics Engine (Stage 1+).

For publish/subscribe mechanics and digest contribution see §3; for
worst-case sizing and frame-budget contribution see §6.

## 1.2 What Is Out of Scope

Each item below is owned by the cited document and is **not**
redefined here (KD-1 cite-not-redefine):

- Phase pipeline ordering, phase-digest preimage, snapshot inclusion
  → Spec #16 §3.1.2 / §3.2.2 / §3.2.3 `TBD-NORMATIVE` (KD-2).
- Tier vocabulary definition → Spec #16 §1.3.1 `TBD-NORMATIVE`
  (KD-3 cites; does not redefine).
- RNG draw mechanics inside event handlers → Spec #16 §3.2.5
  `TBD-NORMATIVE` (consumers route through
  `DeterministicRngService`).
- Multiplayer wire format, network transport, lossy delivery →
  Stage 5+ extension named in §7.3 (KD-10).
- C# code style and banned-API rules (no LINQ, no `System.Random`,
  no `IEnumerable` in hot path) → Spec #20.
- Per-event unit-test catalogues for individual event types →
  owning spec's §5 (Spec #19 §3 `TBD-NORMATIVE` governance applies).
- Performance regression gate thresholds → Spec #18 §4 / §7
  (`NOT STARTED`; gates owned by #18 when published).
- Save/load file format → Spec #16 §3.9.2 `SnapshotPayload` layout
  `TBD-NORMATIVE`.
- Editor-tool / debug-overlay UI → Stage 1+ tooling.

## 1.3 Key Design Decisions

| KD | Topic | Codified in |
|----|-------|-------------|
| KD-1 | Cite-not-redefine — never restate a CLAUDE.md invariant or an approved-spec rule | All sections |
| KD-2 | Boundary with Deterministic Simulation #16 (`Events`-phase WriteSet); `TBD-NORMATIVE` + `[CROSS-PENDING]` qualifier convention | §3.1, §3.2, §4 |
| KD-3 | Tier A / B / C event classification (cited from #16 §1.3.1) — `IEventB` is NOT a phantom interface because both sides are specified here | §2.4, §3.1, Appendix A |
| KD-4 | Authoritative-vs-cosmetic publish path separation; single API surface, tier-tag-routed | §3.2, §4.2 |
| KD-5 | Tick-rate split — 10 Hz tactical / 60 Hz physics; AI stride per #16 `tick % 6 == 0` | §3.2, §3.3 |
| KD-6 | Determinism contracts — intra-tick canonical ordering + digest sub-scope | §3.2, §3.4, §5 |
| KD-7 | No-drop on authoritative paths; `ERR_EVT_QUEUE_OVERFLOW` hard fail; Tier C drop predicate is pure function of `(tick, eventTypeOrdinal, publicationCountThisTick)` | §3.4, §3.6 |
| KD-8 | Zero-allocation hot-loop policy (struct events, `in`-ref publish, pre-allocated subscriber arrays) | §3.5, §6.2 |
| KD-9 | Event-contract versioning (`eventTypeOrdinal` + `payloadVersion`, append-only, deprecation retains rows) | §2.4, §3.7, Appendix A |
| KD-10 | Stage 5+ "do not preclude" multiplayer | §7.3 |
| KD-11 | Instrumentation budget binding to #16 §8.2 (`TBD-NORMATIVE`) | §5, §6.5 |
| KD-12 | Stage-gated activation — contracts that presume implemented code are normative now, enforceable at Stage 0 → 1 | §5.2, §7 |

Full rationale and binding text for each KD lives in
`outline-detailed.md` §"CROSS-CUTTING DESIGN DECISIONS" (v1.1) and
is referenced by KD-number throughout this spec.

## 1.4 Dependencies and Integration Contracts

**Upstream (substantive):**

- Root `CLAUDE.md` — project invariants: "When Writing Code"
  struct-based zero-allocation rule; "Heartbeat Tick Rate" (10 Hz
  tactical / 60 Hz physics); "Interface Design Principle" (only
  declare interfaces when both sides are specified).
- Spec #16 (Deterministic Simulation) — §1.3 tier classification,
  §3.1 phase pipeline, §3.2 digests, §3.6.1 phase WriteSet table,
  §3.9.2 on-disk snapshot layout, §8 trace channels.
  **Status:** `IN PROGRESS`. All citations of these subsections in
  Spec #17 are tagged `TBD-NORMATIVE` per KD-2 until #16 reaches
  `APPROVED`.

**Upstream (consulted):**

- Spec #6 (Shot Mechanics) `APPROVED` — §2.4 / §4
  `ShotExecutedEvent` is the seed Tier A event surface that Spec
  #17 inherits and formalises in the Appendix A registry.
- Spec #20 (Code Standards) `APPROVED` — §3.x zero-allocation lint
  rules and the struct-event pattern (already cites
  `ShotExecutedEvent`).

**Downstream (consumers of #17's contracts):**

- Spec #10 (Heading Mechanics) — `HeaderExecutedEvent` (Tier A).
- Spec #11 (Goalkeeper Mechanics) — `ShotExecutedEvent` consumer;
  `SaveAttemptedEvent` / `BallParriedEvent` / `BallCaughtEvent`
  publisher (Tier A).
- Specs #13–#15 (Pressing AI, Defensive AI, Attacking AI) —
  tactical-cadence events (`PressTriggeredEvent`,
  `MarkAssignedEvent`, `RunCalledEvent`).
- Spec #19 (Testing Strategy) — consumes the ordering/throughput
  test catalogue defined in §5 and feeds determinism golden
  fixtures through the §3.4.3 capture-and-promote path
  `TBD-NORMATIVE`.
- Spec #18 (Performance Optimization) — consumes the per-publish
  cost budget declared in §6.3 / §6.4.
- Statistics Engine (Stage 1+) — consumes the Tier A event stream
  for match-stat aggregation.

**Bidirectional sequencing with #16:** Per CLAUDE.md OPEN ISSUES,
#16's Tier 2 final approval is gated on `#9 / #17 / #18 / #19
reaching IN REVIEW`. Spec #17 in turn binds substantively to #16.
Resolution order: (1) #17 reaches `IN REVIEW` with `TBD-NORMATIVE`
citations to #16; (2) #16 reaches Tier 2 `APPROVED`; (3) #17's
`TBD-NORMATIVE` tags are resolved and #17 advances to `APPROVED`.
`SPEC_INDEX.md` status transitions MUST follow this order.

**Cross-spec constants imported:** one `[CROSS]` entry —
`DOMAIN_TAG_EVENT_LEDGER = 0x15`, allocated in #16 §3.4 v1.0.1
(May 14, 2026) per ERR-017-001 RESOLVED
(`docs/tracking/spec-error-log.md`). Originally tagged
`[CROSS-PENDING]` while #16 was `IN PROGRESS`; promoted to
`[CROSS]` atomically with #16 Tier 2 `APPROVED`. Tier vocabulary
and pipeline phase names are cited (not imported as constants)
from #16. No other `[CROSS]` constants expected in §3.10.

**Stage 0 host platform pin:** test execution requires the pins
named in `docs/tracking/certification-platform.md`. Drafting Spec
#17 does not require those pins to be filled in; first CI
activation (Stage 0 → 1 transition) does.

## 1.5 Glossary (Spec #17-local terms)

| Term | Meaning |
|------|---------|
| Event ledger | Per-tick authoritative store of Tier A / B events. Owned by the `Events` phase per #16 §3.6.1 `TBD-NORMATIVE`. |
| Cosmetic channel | Out-of-band Tier C delivery path. Immediate synchronous dispatch (§3.2.3); never part of authoritative state. |
| `eventTypeOrdinal` | Byte-wide stable identifier; never reused after publication (KD-9). Globally unique across all specs (Appendix A is the single registry). |
| `payloadVersion` | Byte-wide append-only version on each event struct (KD-9). |
| Second-order dispatch | Re-entrant Tier A/B publish from inside a handler during the same-tick `Events` phase. Bounded by `MAX_EVENT_DISPATCH_DEPTH` (§3.2.5). |
| Phase / digest / tier | Vocabulary owned by #16 (not redefined here). |

## 1.6 Version History

| Version | Date         | Author      | Notes                                                                 |
|---------|--------------|-------------|-----------------------------------------------------------------------|
| 0.1     | May 13, 2026 | Claude Code | Initial section-file draft from `outline-detailed.md` v1.1.           |
| 0.2     | May 13, 2026 | Claude Code | PASS 1 critique resolution. No content change to §1 — `[CROSS-PENDING]` qualifier mentioned in §1.4 + KD-2 is now formally sanctioned in CLAUDE.md "Constant Tags" table (M1; landed in the same revision). |
| 1.0.1   | May 15, 2026 | Claude Code | Patch revision (no behavioral change). §1.4 "Cross-spec constants imported" updated: `DOMAIN_TAG_EVENT_LEDGER` `[CROSS-PENDING]` → `[CROSS]`, literal value `0x15` inlined, ERR-017-001 marked RESOLVED. Reflects #16 §3.4 v1.0.1 allocation (May 14, 2026). |
