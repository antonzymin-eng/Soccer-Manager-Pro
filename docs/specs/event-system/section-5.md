# Event System Specification #17 — Section 5: Test Plan

**Created:** May 13, 2026
**Last Updated:** May 13, 2026
**Version:** 0.1 (initial section-file draft from `outline-detailed.md` v1.1)
**Status:** DRAFT

> Section heading order follows `outline-detailed.md` v1.1
> §"SECTION 5" (Test Strategy → Stage-Gated Activation → Catalogue
> → FR-to-Verification Traceability → Determinism Test Consumption
> → Fixtures), superseding the v0.0 stub.

---

## 5.1 Test Strategy

Spec #17 publishes 82 FRs (§2.2). This section maps every FR to its
verification mechanism.

- **Test framework / runner** is governed by Spec #19 §3
  `TBD-NORMATIVE` per KD-2 (#19 is `IN REVIEW`). Spec #17 cites #19
  but does not select tooling.
- **Stage 0 verification:** manual review against §3 mechanics and
  Appendix A registry rows. Stage 0 FRs reduce to "this row of
  Appendix A satisfies the schema check" or "this §3.x mechanic
  matches the rule statement in §2.2".
- **Stage 0+1 activation:** tooling activates per the FR's
  "Activation stage" column in §2.2 (KD-12). First activation
  trigger is the first `src/event-system/` code commit.

## 5.2 Stage-Gated Activation Table (KD-12)

Per-FR activation status. Most FRs activate at Stage 0+1; a few
are enforceable at Stage 0 because they apply to spec drafts
themselves (Appendix A registry rows, typed-contract structural
rules).

| FR range | Stage 0 status | Activation stage | Activation criterion |
|----------|----------------|------------------|----------------------|
| FR-EVT-001 … 002 | Active (Appendix A schema enforced) | Stage 0 | Spec-review check + registry-validator |
| FR-EVT-003 … 005 | Active (registry uniqueness + monotonicity) | Stage 0 | Registry-validator (Stage 0); Stage 0+1 retains. |
| FR-EVT-006 | Partial (canonical byte layout in Appendix B examples) | Stage 0+1 | First `src/event-system/` code committed. |
| FR-EVT-007 | Active (whitelist enforced in Appendix A payload schemas) | Stage 0 | Spec-review check. |
| FR-EVT-008 | Spec review only | Stage 0+1 | Spec #20 lint. |
| FR-EVT-009 | Active (Appendix A tier column) | Stage 0 | Registry-validator. |
| FR-EVT-010 … 016 | Spec review only | Stage 0+1 | Spec #20 lint + runtime debug assert. |
| FR-EVT-017 … 026 | Inactive | Stage 0+1 | First `EventBus.cs` commit. |
| FR-EVT-027 … 033 | Inactive | Stage 0+1 | First `EventLedger.cs` commit. |
| FR-EVT-034 … 040 | Spec review only | Stage 0+1 | First integration test commit. |
| FR-EVT-041 … 047 | Inactive | Stage 0+1 | First `EventLedger.cs` commit. |
| FR-EVT-048 … 054 | Inactive | Stage 0+1 | First allocation-tracker test commit. |
| FR-EVT-055 … 060 | Active (versioning rules apply at registry-row authoring) | Stage 0 | Registry-validator. |
| FR-EVT-061 … 066 | Inactive (or Stage 0 spec-review only for FR-EVT-066) | Stage 0+1 | First trace-channel commit. |
| FR-EVT-067 … 072 | Spec review only | Stage 0+1 (FR-EVT-067 … 070) / Stage 5+ (FR-EVT-072) | Spec #20 lint + Stage 5+ Fixed64 re-verification suite. |
| FR-EVT-073 … 078 | Inactive | Stage 0+1 | First `EventBus.cs` commit. |
| FR-EVT-079 | Active (error-code namespace check) | Stage 0 | Registry-validator. |
| FR-EVT-080 … 081 | Inactive | Stage 0+1 | First fixture-loader commit. |
| FR-EVT-082 | Spec review only | Stage 0+1 | Runtime debug assert. |

## 5.3 Test Catalogue

Target ratios per Spec #19 §3.1.2 `TBD-NORMATIVE` per KD-2 (resolves
PASS 1 finding 12).

| Layer | Target ratio | Examples |
|-------|--------------|----------|
| Unit | ≥ 60% | Publish/subscribe correctness; ordinal-allocation uniqueness; struct-layout reflection check; per-publish allocation = 0 bytes assertion (`Assert.AllocatedBytes(0)`); ring-buffer capacity edge; canonical sort key (FM-017-002) total-order property; `intraPhaseDrawIndex` reset / monotonicity; Tier C drop predicate purity; second-order dispatch BFS up to depth 8; `ERR_EVT_TIER_MISMATCH` rejection at runtime register; `EVENT_QUEUE_CAPACITY` overflow → `ERR_EVT_QUEUE_OVERFLOW`. |
| Integration | ≤ 25% | EventBus + EventLedger round-trip; `Publish → DrainTick → Subscriber` end-to-end with byte-level assertion; `SerializeLedger → SnapshotPayload → load → re-deserialize` round-trip (Spec #19 §3.8 fixture format); phase-WriteSet ownership check across the `Input → … → Snapshot` pipeline; tier-mismatch rejection at registration; producing-phase `intraPhaseDrawIndex` reset behaviour across the boundary `Physics → Resolve`. |
| Simulation | ≤ 12% | Full-match (90-min) run; verify zero allocations after warm-up (allocation tracker); verify ledger digest matches expected golden (G1); verify aggregate event count per tick ≤ §6.3 budget; verify no `ERR_EVT_QUEUE_OVERFLOW` across the run. |
| End-to-end / soak | ≤ 3% | 1-hour soak; verify no queue-depth drift across repeated replays of the same seed; KD-7 no-drop assertion across full match (Tier A/B drop counter MUST be zero); Tier C drop counter behaviour matches FM-deterministic predicate at every tick. |

### 5.3.1 Property tests (Spec #19 §3.4 `TBD-NORMATIVE`)

- **P1 — Publish/subscribe byte identity.** `Publish<T>(in T evt)`
  followed by `Subscribe<T>` invocation MUST produce a subscriber-
  visible `T` whose bytes equal the original `evt` (idempotence).
  Domain: every event in Appendix A.
- **P2 — Sort-key total order.** Over the FM-017-002 tuple, the
  sort is a total order with no ties. Domain: any tick whose Tier
  A/B queue is non-empty (FR-EVT-027, FR-EVT-028).
- **P3 — Version-migration parse.** For every
  `(ordinal, payloadVersion)` ever recorded in Appendix A,
  fixture-load deserialises into the version-appropriate struct
  shape (FR-EVT-055; KD-9 retains deprecated rows).

### 5.3.2 Determinism golden fixtures

- **G1 — Phase-digest golden.** A 60-second scripted match
  (deterministic seed) produces a canonical
  `PhaseScopeFields[Events]` byte sequence (FM-017-001). The
  golden is committed under
  `tests/data/event-system/golden/g1-phase-digest-60s.fixture` per
  Spec #19 §3.8 `TBD-NORMATIVE` layout. Tier A delivery only;
  Tier C events MUST NOT influence the digest (FR-EVT-014).
- **G2 — Cosmetic-channel drop record.** Same scripted match,
  captured at Tier C trace channel. Golden records the exact
  drop-predicate output across the run; replay of the same seed
  MUST produce a byte-identical Tier C drop record (FR-EVT-043
  replay-stability).

## 5.4 FR-to-Verification Traceability

Per-FR verification table indexed by FR-EVT-### (full table
generated at IN REVIEW commit; this section publishes the schema
and stage-0 acknowledgement of the same degenerate row pattern
seen in Spec #20 §5.5 and Spec #19 §5.6).

Columns: `FR-EVT-### | Verification Mechanism | Tooling | Activation Stage | Output Artifact`.

Stage 0 rows resolve to "manual review against §3 mechanics or
Appendix A row" — acknowledged degenerate. The §9.2 quality
checklist row "Every FR row resolves to a §5.x verification
mechanism" is therefore satisfied at Stage 0 by a §5.4 row that
points back at §3 or Appendix A, with the activation column
flagging the upgrade path to Stage 0+1 tooling.

Example rows (full table to land at IN REVIEW):

| FR-EVT-### | Verification | Tooling | Activation | Artifact |
|------------|--------------|---------|------------|----------|
| FR-EVT-001 | Struct-layout reflection over Appendix A | C# reflection test in `tests/event-system/struct_layout_test.cs` | Stage 0+1 | Test report; Stage 0: spec-review confirms each Appendix A row references a struct skeleton conforming to §2.4.1. |
| FR-EVT-003 | Registry-row uniqueness scan | `tools/spec-validator/registry-uniqueness.py` | Stage 0 | Validator log. |
| FR-EVT-027 | P2 property test (sort-key total order) | `tests/event-system/sort_key_property.cs` | Stage 0+1 | Property-test report. |
| FR-EVT-031 | G1 golden | `tests/data/event-system/golden/g1-phase-digest-60s.fixture` | Stage 0+1 | Byte-comparison report against `tests/event-system/g1-golden_test.cs`. |
| FR-EVT-043 | G2 golden + replay-stability re-run | `tests/data/event-system/golden/g2-cosmetic-drops-60s.fixture` | Stage 0+1 | Byte-comparison report. |
| FR-EVT-048 | Allocation-tracker assertion | BenchmarkDotNet (Spec #18 pin, D1) | Stage 0+1 | Bench report. |

## 5.5 Determinism Test Consumption (binds to #16 §7 / Spec #19 §3.2)

- Spec #17 does **NOT** operate its own determinism regression
  tier (KD-2). The G1 / G2 golden tests defined in §5.3.2 feed the
  #16 §7 `TBD-NORMATIVE` regression suite via the Spec #19 §3.4.3
  `TBD-NORMATIVE` capture-and-promote path.
- **Boundary review check.** Any change to:
  - #16 §3.1.2 phase ordering,
  - #16 §3.2.2 digest formula,
  - #16 §3.6.1 WriteSet table, or
  - #16 §3.9.2 snapshot layout,

  triggers a Spec #17 §3.4 / §4.4 review (the consumer-side change
  may require an FM-017-001 update or a `DOMAIN_TAG_EVENT_LEDGER`
  re-anchor).
- **TBD-NORMATIVE tag resolution as a Stage 0+1 quality gate.**
  When #16 reaches `APPROVED`, every `#16 §x.x.x TBD-NORMATIVE`
  reference in Spec #17 is re-grepped; tags are removed atomically
  with confirmation that subsection numbers still resolve (§3.4.5
  cite-precision guard). The §9.2 quality checklist row tracks
  the outstanding tag count.

## 5.6 Test-Data Fixtures (binds to Spec #19 §3.8 / KD-10)

- Golden event-ledger fixtures stored at
  `tests/data/event-system/golden/<scenario>.fixture` per Spec #19
  §3.8.2 `TBD-NORMATIVE` layout.
- Each fixture conforms to #16 §5 `TBD-NORMATIVE` canonical save
  format (Spec #19 KD-10 binding via `TBD-NORMATIVE` until #19
  reaches `APPROVED`).
- Fixture provenance recorded per Spec #19 §3.8.4 `TBD-NORMATIVE`:
  scripted scenario name, deterministic seed, host platform pin
  reference (`docs/tracking/certification-platform.md`), commit
  SHA at capture time, capturing tester / agent.
- Fixture pinning is a Stage 0+1 deliverable; Spec #17 publishes
  the path and provenance schema, but the fixture **bytes** are
  captured against actual code at Stage 0 → 1 transition.

## 5.7 Version History

| Version | Date         | Author      | Notes                                                                 |
|---------|--------------|-------------|-----------------------------------------------------------------------|
| 0.1     | May 13, 2026 | Claude Code | Initial section-file draft from `outline-detailed.md` v1.1. Pyramid ratios, P1/P2/P3 property tests, G1/G2 golden fixtures, stage-gated activation table published. Section heading order superseded the v0.0 stub. |
