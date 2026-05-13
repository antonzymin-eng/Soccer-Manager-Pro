# Event System Specification #17 — Section 8: References & Citation Audit

**Created:** May 13, 2026
**Last Updated:** May 13, 2026
**Version:** 0.1 (initial section-file draft from `outline-detailed.md` v1.1)
**Status:** DRAFT

> **Slot reconciliation.** This section IS the CLAUDE.md 9-section
> template's "References" slot. `outline.md` v1.0 placed
> multiplayer-compatibility content here, which violated the
> template; PASS 1 finding 2 mandates this section be references
> with multiplayer content moved to §7.3.

---

## 8.1 Source Register

### 8.1.1 Project documents

| Source | Purpose | Status |
|--------|---------|--------|
| Root `CLAUDE.md` | Project invariants: "When Writing Code" struct-based zero-allocation rule; "Heartbeat Tick Rate" (10 Hz tactical / 60 Hz physics); "Interface Design Principle"; "Constant Tags" rules. | Authoritative. |
| `docs/planning/development-best-practices.md` | Engineering practices baseline. | Authoritative. |
| `docs/planning/master-development-plan.md` | Stage / phase progression. | Authoritative. |
| `docs/specs/SPEC_INDEX.md` | Canonical spec numbering and approval status; Spec #17 row = `IN PROGRESS`. | Authoritative. |
| `docs/tracking/PROGRESS.md` | Schedule and milestone log. | Authoritative. |
| `docs/tracking/spec-error-log.md` | ERR-017-001 (`DOMAIN_TAG_EVENT_LEDGER` back-prop into #16 §3.4). | Authoritative. |
| `docs/tracking/certification-platform.md` | Stage 0 host-platform pins. | Placeholder (Stage 0 pins TBD per CLAUDE.md OPEN ISSUES). |

### 8.1.2 Upstream specs

| Spec | Sections cited | Status | TBD-NORMATIVE? |
|------|----------------|--------|----------------|
| #1 Ball Physics | §1.2 / Appendix C (coordinate system) | APPROVED | No |
| #6 Shot Mechanics | §2.4 / §4 (`ShotExecutedEvent` payload reference) | APPROVED | No |
| #16 Deterministic Simulation | §1.3 (tiers), §3.1 (phase pipeline), §3.2 (digests), §3.2.4.1 (canonical serialization / `array<T>` rules), §3.4 (domain-tag table), §3.5 (Tier-B tolerance), §3.6.1 (phase WriteSet), §3.7 (save boundary), §3.9.2 (snapshot layout), §3.10 (failure-mode table — `TBD-NORMATIVE` anchor), §6 / §6.2 (frame budget), §7 (regression suite), §8 / §8.2 (trace channels / instrumentation envelope) | IN PROGRESS | Yes (all #16 citations per KD-2) |
| #19 Testing Strategy | §3.1.2 (test pyramid), §3.2 (determinism suite consumption), §3.4 (property tests), §3.4.3 (capture-and-promote), §3.6.1 (cite-precision guard pattern), §3.8 (fixture governance) | IN REVIEW | Yes (per KD-2 §19 status caveat) |
| #20 Code Standards | §2.1 (conformance levels semantics), §3.x (banned-APIs and struct-event pattern citing `ShotExecutedEvent`), §5.5 (Stage-0 acknowledgement of degenerate verification rows) | APPROVED | No |
| #18 Performance Optimization | §4 / §7 (perf gates) | NOT STARTED | Acknowledged — gates owned by #18 when published. |
| #9 Fixed64 Math | (Stage 5+ re-verification) | NOT STARTED | Stage 5+ only; not blocking. |

### 8.1.3 External standards

| Standard | Use |
|----------|-----|
| RFC 2119 | MUST / SHOULD / MAY keyword semantics (§2.1). |

## 8.2 Verification Notes

The following citation-audit checks were performed during draft.
Each check is a §9.2 quality-checklist row.

- Every `CLAUDE.md` citation in §3 verified against the May 13,
  2026 CLAUDE.md text.
- Every `#16 §x.x.x` citation verified against the current
  `deterministic-sim/section-X.md` (per §3.4.5 cite-precision
  guard) at draft date. **Status:** subsection numbers may shift
  during #16's continuing adversarial passes; tags remain
  `TBD-NORMATIVE` until #16 reaches `APPROVED`.
- `ShotExecutedEvent` field list cited from
  `shot-mechanics/section-2-7-to-2-9.md` (Shot Mechanics #6 §2.4).
- `EntityId` no-reuse guarantee cited from #2 §2.5 (XC-002-001)
  and #8 §1.7.3 (XC-008-001) per CLAUDE.md OPEN ISSUES resolution.
- Spec #20 `ShotExecutedEvent` struct-event example confirmed
  present in the published spec; cross-referenced from §3.5.4.

## 8.3 Cross-Spec Citation Audit

### 8.3.1 Spec #17 is cited by

- Spec #10 (Heading Mechanics) — will cite Spec #17 for the Tier A
  classification of `HeaderExecutedEvent`.
- Spec #11 (Goalkeeper Mechanics) — will cite Spec #17 for tier
  classification of `SaveAttemptedEvent`, `BallParriedEvent`,
  `BallCaughtEvent`.
- Specs #13 / #14 / #15 (Pressing / Defensive / Attacking AI) — will
  cite Spec #17 for Tier A classification + AI-stride tick-rate
  rule.
- Statistics Engine (Stage 1+ spec) — will subscribe to Tier A
  event stream defined in Appendix A.

### 8.3.2 Spec #17 cites

| Cited | Type of citation |
|-------|------------------|
| #16 | Substantive: phase pipeline (§3.1), digest formula (§3.2), WriteSet table (§3.6.1), snapshot layout (§3.9.2), tier vocabulary (§1.3.1), Tier-B tolerance (§3.5), domain-tag table (§3.4 — ERR-017-001 back-prop), failure-mode table (§3.10 anchor), trace-channel format (§8), frame-budget envelope (§6.2). |
| #6 | Substantive: `ShotExecutedEvent` payload reference (§2.4 / §4). |
| #18 | Boundary: performance regression gate thresholds owned by #18 §4 / §7. |
| #19 | Boundary: testing governance (pyramid ratios, fixture format, determinism-suite capture path). |
| #20 | Boundary: banned-API enforcement (§3.x); struct-event pattern example. |
| #1 | Coordinate convention for Stage 0 `Vector3` (§3.1.4). |
| #2, #8 | XC-002-001 / XC-008-001 EntityId no-reuse (§3.1.4 payload-field whitelist). |
| #9 | Forward reference only — Stage 5+ Fixed64 re-verification. Non-blocking. |

### 8.3.3 Cross-reference IDs declared

| ID | Subject | Section |
|----|---------|---------|
| FM-017-001 | `EventLedgerDigestScope` formula | §3.4.2, §3.4.3 |
| FM-017-002 | `EventIntraTickSortKey` formula | §3.2.4, §3.4.3 |
| EC-017-001 | Tier A publish from non-`Events` phase | §3.8 |
| EC-017-002 | Queue overflow | §3.8 |
| EC-017-003 | Unknown ordinal at load | §3.8 |
| EC-017-004 | Version newer than registry | §3.8 |
| EC-017-005 | Cross-tier subscription | §3.8 |
| EC-017-006 | Dispatch-depth exceeded | §3.8 |
| ERR-017-001 | `DOMAIN_TAG_EVENT_LEDGER` allocation back-prop into #16 §3.4 | Filed in `docs/tracking/spec-error-log.md` May 12, 2026; closure at #17 IN REVIEW. |

### 8.3.4 `[CROSS]` / `[CROSS-PENDING]` constants imported

| Constant | From | Tag at Stage 0 | Promotion |
|----------|------|----------------|-----------|
| `DOMAIN_TAG_EVENT_LEDGER` | #16 §3.4 domain-tag table | `[CROSS-PENDING]` (numeric value TBD-NORMATIVE) | Promoted to `[CROSS]` when #16 reaches `APPROVED` and the back-prop tag is allocated. Tracked by ERR-017-001. |

No other `[CROSS]` constants declared at draft time.

## 8.4 Constant Provenance Summary

| Constant | Tag | Rationale source |
|----------|-----|------------------|
| `EVENT_QUEUE_CAPACITY = 1024` | `[GT]` | §6.3.1 / §6.3.2 derivation (`64 × 8 × 2 = 1024`); additive across BFS levels because FR-EVT-046a caps per-handler secondary publishes at 1. |
| `COSMETIC_PER_TICK_PUBLICATION_BUDGET = 4096` | `[GT]` | §6.3.3 sanity ceiling (×16 over 256). |
| `MAX_EVENT_DISPATCH_DEPTH = 8` | `[GT]` | §3.2.5 design choice; bounded BFS for re-entrant publish. |
| `EVENT_TYPE_ORDINAL_WIDTH = 1 byte` | `[GT]` | §3.1.2 design decision; Stage 5+ expansion §7.3. |
| `PAYLOAD_VERSION_WIDTH = 1 byte` | `[GT]` | §3.1 / §3.7 design decision. |
| `DOMAIN_TAG_EVENT_LEDGER` | `[CROSS-PENDING]` | #16 §3.4 (back-prop via ERR-017-001). |
| `ERR_EVT_QUEUE_OVERFLOW = 0x1701` | `[GT]` | §2.5 / §3.6.1; reserved `0x17NN` block. |
| `ERR_EVT_TIER_MISMATCH = 0x1702` | `[GT]` | §2.5 / §3.2.5. |
| `ERR_EVT_ORDINAL_UNKNOWN = 0x1703` | `[GT]` | §2.5 / §3.7.2. |
| `ERR_EVT_VERSION_INCOMPATIBLE = 0x1704` | `[GT]` | §2.5 / §3.7.2. |
| `ERR_EVT_REGISTRATION_PHASE = 0x1705` | `[GT]` (design-fixed) | §2.5 / §3.2.2; lifecycle violation, distinct from tier-mismatch. |

- No `[EST]` constants at draft time.
- No `[FIXED]` constants in this spec (events are not physics; per
  CLAUDE.md "Constant Tags" — `[FIXED]` is reserved for physics-
  law-derived values).
- No `[DERIVED]` constants in this spec.
- `[GT]` covers two sub-classes here (per §3.10 note): **runtime-
  tunable** (`EVENT_QUEUE_CAPACITY`,
  `COSMETIC_PER_TICK_PUBLICATION_BUDGET`,
  `MAX_EVENT_DISPATCH_DEPTH` — subject to §6.3.4 re-tuning) and
  **design-fixed** (`EVENT_TYPE_ORDINAL_WIDTH`,
  `PAYLOAD_VERSION_WIDTH`, all `ERR_EVT_*` codes — locked at
  approval; changing them breaks replay corpus or crash-dump
  triage).

## 8.5 Version History

| Version | Date         | Author      | Notes                                                                 |
|---------|--------------|-------------|-----------------------------------------------------------------------|
| 0.1     | May 13, 2026 | Claude Code | Initial section-file draft from `outline-detailed.md` v1.1. Source register, citation audit, cross-reference ID enumeration, constant provenance summary published. Section heading order superseded the v0.0 stub. |
| 0.2     | May 13, 2026 | Claude Code | PASS 1 critique resolution. §8.1.2 updated `#16 §3.10` row to drop `[TBD-CITE]` (M2). §8.4 added `ERR_EVT_REGISTRATION_PHASE = 0x1705` row (L3) and `[GT]` tag-subclass notes (M8). §8.4 `EVENT_QUEUE_CAPACITY` rationale notes FR-EVT-046a additivity (H1). |
