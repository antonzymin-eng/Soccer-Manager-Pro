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

Per-FR verification table indexed by FR-EVT-###. Columns:
`FR-EVT-### | Verification Mechanism | Tooling | Activation Stage | Output Artifact`.

The §9.2 quality checklist row Q2 ("Every FR row resolves to a §5.x
verification mechanism") is satisfied by this table. Stage 0 rows
resolve to "manual spec-review against §3 mechanics or Appendix A
row" — degenerate but honest, with the activation column flagging
the upgrade path to Stage 0+1 tooling. The pattern follows Spec #20
§5.5 / Spec #19 §5.6 (full table, with each row's degeneracy
visible).

### 5.4.1 Typed-contract rules (FR-EVT-001 … 016)

| FR-EVT-### | Verification | Tooling | Activation | Artifact |
|------------|--------------|---------|------------|----------|
| FR-EVT-001 | Struct-layout reflection over Appendix A registry rows | `tests/event-system/struct_layout_test.cs` (C# reflection) | Stage 0+1 (Stage 0: spec-review per row) | Test report; Stage 0 evidence is the Appendix A schema column. |
| FR-EVT-002 | Canonical-bytes golden + struct-layout reflection (header order, no implicit padding) | `tests/event-system/canonical_bytes_test.cs` + `tests/data/event-system/golden/g1-phase-digest-60s.fixture` | Stage 0+1 (Stage 0: Appendix B B.2 / B.3 worked examples) | Byte-comparison report. |
| FR-EVT-003 | Registry-row uniqueness scan over `Ordinal` column | `tools/spec-validator/registry-uniqueness.py` | Stage 0 | Validator log. |
| FR-EVT-004 | Registry-row append-only audit (no `Ordinal` row ever deleted; `Deprecated` retains row) | Same validator + spec-review at every Appendix A edit | Stage 0 | Validator log + spec-review diff. |
| FR-EVT-005 | P3 property test (version-migration parse for every `(ordinal, payloadVersion)` row in Appendix A) | `tests/event-system/version_migration_property.cs` | Stage 0+1 | Property-test report. |
| FR-EVT-006 | Canonical-bytes golden (G1) | `tests/data/event-system/golden/g1-phase-digest-60s.fixture` | Stage 0+1 (Stage 0: Appendix B) | Byte-comparison report. |
| FR-EVT-007 | Appendix A payload-schema validator (whitelist scan) | `tools/spec-validator/payload-type-whitelist.py` | Stage 0 | Validator log. |
| FR-EVT-008 | Spec #20 banned-API lint (`tools/spec20-lint/no-reference-payload.rule`) | Spec #20 lint | Stage 0+1 (Stage 0: spec-review) | Lint report. |
| FR-EVT-009 | Registry-row schema validator (tier column populated; values ∈ {A, B, C}) | `tools/spec-validator/tier-column.py` | Stage 0 | Validator log. |
| FR-EVT-009a | Registry-row marker-interface scan via reflection (exactly one `IEventA`/`IEventB`/`IEventC` per struct) | `tests/event-system/single_marker_test.cs` + Spec #20 lint | Stage 0+1 (Stage 0: spec-review per registry row) | Test report + lint report. |
| FR-EVT-010 | Debug-build assertion at every Tier A `Publish<T>` call site (phase check) | `EventBus.Publish<IEventA>` `Debug.Assert(currentPhase == Events.Producing)` | Stage 0+1 | Assertion failures in test runs. |
| FR-EVT-011 | G1 golden (Tier A bytes contribute to digest) | `tests/data/event-system/golden/g1-phase-digest-60s.fixture` | Stage 0+1 | Byte-comparison report. |
| FR-EVT-012 | Snapshot round-trip integration test (serialize → load → re-deserialize) | `tests/event-system/snapshot_roundtrip_test.cs` | Stage 0+1 | Integration test report. |
| FR-EVT-013 | Stage 5+ Tier B activation; not enforceable at Stage 0 (no Tier B events seeded) | Spec #16 §3.5 tolerance suite (Stage 5+) | Stage 5+ | Stage 5+ activation log. |
| FR-EVT-014 | G1 golden Tier C exclusion check (Tier C publications during the scripted scenario do NOT shift G1 bytes) | `tests/event-system/g1_tier_c_isolation_test.cs` | Stage 0+1 | Test report. |
| FR-EVT-015 | Snapshot-payload Tier C absence check (`SerializeLedger` output contains no Tier C ordinals) | `tests/event-system/snapshot_tier_c_exclusion_test.cs` | Stage 0+1 | Integration test report. |
| FR-EVT-016 | Spec #20 lint + spec-review at every Tier-C subscription site in gameplay code | `tools/spec20-lint/no-authoritative-cosmetic-subscribe.rule` | Stage 0+1 (Stage 0: spec-review) | Lint report. |

### 5.4.2 Publish / Subscribe semantics (FR-EVT-017 … 033)

| FR-EVT-### | Verification | Tooling | Activation | Artifact |
|------------|--------------|---------|------------|----------|
| FR-EVT-017 | Compile-time check (`EventBus.Publish<T>` signature surface) | C# compiler + `tests/event-system/api_surface_test.cs` | Stage 0+1 | Compile report. |
| FR-EVT-018 | Compile-time check (`in T evt` parameter); Spec #20 lint | Compiler + `tools/spec20-lint/in-ref-only.rule` | Stage 0+1 | Compile + lint report. |
| FR-EVT-019 | Compile-time check + Spec #20 lint (no closure capture in handler delegate) | Compiler + `tools/spec20-lint/no-closure-capture.rule` | Stage 0+1 | Compile + lint report. |
| FR-EVT-020 | Registration-lifecycle unit test (subscriber registered before first `Events` phase) | `tests/event-system/boot_registration_test.cs` | Stage 0+1 | Test report. |
| FR-EVT-021 | Runtime registration-lifecycle test (post-init Tier A/B register → `ERR_EVT_REGISTRATION_PHASE`) | `tests/event-system/post_init_registration_test.cs` | Stage 0+1 | Test report. |
| FR-EVT-022 | Runtime registration unit test (Tier C register/unregister permitted at runtime) | `tests/event-system/tier_c_runtime_test.cs` | Stage 0+1 | Test report. |
| FR-EVT-023 | Ring-buffer drain integration test (`Publish → DrainTick → handler`) | `tests/event-system/drain_roundtrip_test.cs` | Stage 0+1 | Integration test report. |
| FR-EVT-024 | Cosmetic-channel synchronous-dispatch unit test (handler fires on publisher's thread) | `tests/event-system/cosmetic_sync_test.cs` | Stage 0+1 | Test report. |
| FR-EVT-025 | Per-tick counter reset unit test (publication-count table re-zeroed at `OnTickBoundary`) | `tests/event-system/counter_reset_test.cs` | Stage 0+1 | Test report. |
| FR-EVT-026 | Handler-exception integration test (Tier A halts tick; Tier C logs+suppresses) | `tests/event-system/handler_exception_test.cs` | Stage 0+1 | Integration test report. |
| FR-EVT-027 | P2 property test (sort-key total order) | `tests/event-system/sort_key_property.cs` | Stage 0+1 | Property-test report. |
| FR-EVT-028 | `intraPhaseDrawIndex` reset / monotonicity unit test | `tests/event-system/draw_index_test.cs` | Stage 0+1 | Test report. |
| FR-EVT-029 | Drain-time sort unit test (sort runs at `DrainTick` entry, not per publish) | `tests/event-system/sort_timing_test.cs` | Stage 0+1 | Test report. |
| FR-EVT-030 | Dispatch-order unit test (subscribers see canonical sort order) | `tests/event-system/dispatch_order_test.cs` | Stage 0+1 | Test report. |
| FR-EVT-031 | G1 golden (FM-017-001 phase digest sub-scope) | `tests/data/event-system/golden/g1-phase-digest-60s.fixture` | Stage 0+1 | Byte-comparison report. |
| FR-EVT-032 | Empty-`Events`-phase digest unit test (canonical empty-array bytes emitted) | `tests/event-system/empty_phase_digest_test.cs` | Stage 0+1 | Test report. |
| FR-EVT-033 | Spec #20 lint (no `System.Random`, no `DateTime.Now` in publish path) | `tools/spec20-lint/no-nondeterministic-source.rule` | Stage 0+1 | Lint report. |

### 5.4.3 Tick-rate split (FR-EVT-034 … 040)

| FR-EVT-### | Verification | Tooling | Activation | Artifact |
|------------|--------------|---------|------------|----------|
| FR-EVT-034 | Physics-cadence drain integration test (publish in `Physics` → drain in same-tick `Events`) | `tests/event-system/physics_cadence_test.cs` | Stage 0+1 | Integration test report. |
| FR-EVT-035 | Resolve-cadence drain integration test | `tests/event-system/resolve_cadence_test.cs` | Stage 0+1 | Integration test report. |
| FR-EVT-036 | Tactical stride integration test (`tick % 6 == 0` gating) | `tests/event-system/ai_stride_test.cs` | Stage 0+1 | Integration test report. |
| FR-EVT-037 | `AI_NoOp` empty-WriteSet unit test (no Tier A/B publish on non-stride tick) | `tests/event-system/ai_noop_writeset_test.cs` | Stage 0+1 | Test report. |
| FR-EVT-038 | `AI_NoOp` cosmetic-publish permission unit test (Tier C `TickHeartbeatEvent` permitted) | `tests/event-system/ai_noop_heartbeat_test.cs` | Stage 0+1 | Test report. |
| FR-EVT-039 | G1 golden + tick-boundary invariant assertion (every queued entry drained in same tick) | `tests/data/event-system/golden/g1-phase-digest-60s.fixture` + `tests/event-system/tick_boundary_invariant_test.cs` | Stage 0+1 | Test report. |
| FR-EVT-040 | Spec-review at every Tier A publish call site (no cross-tick aggregator on publisher side) | Spec-review | Stage 0 | Review diff. |

### 5.4.4 Queue overflow + BFS bounds (FR-EVT-041 … 047 + 046a / 046b)

| FR-EVT-### | Verification | Tooling | Activation | Artifact |
|------------|--------------|---------|------------|----------|
| FR-EVT-041 | Capacity-overflow unit test (`Publish` past `EVENT_QUEUE_CAPACITY` → `ERR_EVT_QUEUE_OVERFLOW`) | `tests/event-system/queue_overflow_test.cs` | Stage 0+1 | Test report. |
| FR-EVT-042 | Soak test (full match; Tier A/B drop counter MUST be zero) | `tests/event-system/full_match_no_drop_soak.cs` | Stage 0+1 | Soak report. |
| FR-EVT-043 | G2 golden + replay-stability re-run | `tests/data/event-system/golden/g2-cosmetic-drops-60s.fixture` | Stage 0+1 | Byte-comparison report. |
| FR-EVT-044 | Drop predicate firing unit test (drop → publish is no-op; subscribers NOT invoked) | `tests/event-system/drop_noop_test.cs` | Stage 0+1 | Test report. |
| FR-EVT-045 | Drop-trace unit test (drops appear on Tier C trace channel, NOT in ledger) | `tests/event-system/drop_trace_test.cs` | Stage 0+1 | Test report. |
| FR-EVT-046 | BFS depth-cap unit test (depth ≤ `MAX_EVENT_DISPATCH_DEPTH`) | `tests/event-system/bfs_depth_test.cs` | Stage 0+1 | Test report. |
| FR-EVT-046a | Per-handler out-degree unit test (≤ 1 secondary publish per invocation) | `tests/event-system/handler_outdegree_test.cs` | Stage 0+1 | Test report. |
| FR-EVT-046b | Per-handler out-degree violation → `ERR_EVT_QUEUE_OVERFLOW` unit test | Same fixture as FR-EVT-046a | Stage 0+1 | Test report. |
| FR-EVT-047 | Depth-overflow unit test (depth past cap → `ERR_EVT_QUEUE_OVERFLOW`) | `tests/event-system/bfs_depth_overflow_test.cs` | Stage 0+1 | Test report. |

### 5.4.5 Zero-allocation hot loop (FR-EVT-048 … 054)

| FR-EVT-### | Verification | Tooling | Activation | Artifact |
|------------|--------------|---------|------------|----------|
| FR-EVT-048 | Allocation-tracker assertion (`Assert.AllocatedBytes(0)` per `Publish<T>`) | BenchmarkDotNet (Spec #18 D1 pin) + `tests/event-system/publish_alloc_test.cs` | Stage 0+1 | Bench report. |
| FR-EVT-049 | Allocation-tracker assertion per `DrainTick` | Same | Stage 0+1 | Bench report. |
| FR-EVT-050 | Allocation-tracker assertion per `SerializeLedger` | Same + caller-`Span<byte>` test | Stage 0+1 | Bench report. |
| FR-EVT-051 | Subscriber-array sizing unit test (pinned at startup, no resize) | `tests/event-system/subscriber_array_test.cs` | Stage 0+1 | Test report. |
| FR-EVT-052 | Spec #20 lint (banned-API list) | `tools/spec20-lint/no-banned-api-publish-path.rule` | Stage 0+1 | Lint report. |
| FR-EVT-053 | Spec #20 lint (no closure capture) | `tools/spec20-lint/no-closure-capture.rule` | Stage 0+1 | Lint report. |
| FR-EVT-054 | Stack-frame sizing unit test (publication-count table ≤ 512 bytes) | `tests/event-system/cosmetic_table_size_test.cs` | Stage 0+1 | Test report. |

### 5.4.6 Versioning + deprecation (FR-EVT-055 … 060)

| FR-EVT-### | Verification | Tooling | Activation | Artifact |
|------------|--------------|---------|------------|----------|
| FR-EVT-055 | Appendix A diff validator (append-only field add; `payloadVersion` bumped) | `tools/spec-validator/registry-evolution.py` | Stage 0 | Validator log; spec-review diff. |
| FR-EVT-056 | Registry-evolution validator (field-removal → must mint new ordinal) | Same | Stage 0 | Validator log. |
| FR-EVT-057 | Registry-evolution validator (no reorder after approval) | Same | Stage 0 | Validator log. |
| FR-EVT-058 | Registry-evolution validator (no width change in place) | Same | Stage 0 | Validator log. |
| FR-EVT-059 | Registry-evolution validator (no tier change in place) | Same | Stage 0 | Validator log. |
| FR-EVT-060 | Spec-review at every deprecation flip; producer-side lint forbids publishing deprecated ordinals | Spec-review + `tools/spec20-lint/no-publish-deprecated.rule` | Stage 0+1 | Review diff + lint report. |

### 5.4.7 Instrumentation (FR-EVT-061 … 066)

| FR-EVT-### | Verification | Tooling | Activation | Artifact |
|------------|--------------|---------|------------|----------|
| FR-EVT-061 | Per-Tier-A-publish trace-byte unit test (≤ 16 bytes) | `tests/event-system/trace_size_test.cs` | Stage 0+1 | Test report. |
| FR-EVT-062 | Aggregated Tier C trace unit test (one entry per `(tick, ordinal)`) | `tests/event-system/trace_tier_c_agg_test.cs` | Stage 0+1 | Test report. |
| FR-EVT-063 | §5 registry vs §6.5.1 channel-name diff (spec-review) | Spec-review | Stage 0 | Review diff. |
| FR-EVT-064 | Soak-test footprint assertion (≤ 2 MB per match) | `tests/event-system/full_match_no_drop_soak.cs` (instrumentation footprint check) | Stage 0+1 | Soak report. |
| FR-EVT-065 | Per-publish cost budget audit at #16 approval | Manual audit referencing #16 §8.2 envelope | Stage 0+1 (gated on #16 APPROVED) | Audit memo. |
| FR-EVT-066 | Spec-review (no `IReplayEventReader` declaration in Stage 0 codebase) | Spec-review | Stage 0 | Review diff. |

### 5.4.8 Stage 5+ "do not preclude" (FR-EVT-067 … 072)

| FR-EVT-### | Verification | Tooling | Activation | Artifact |
|------------|--------------|---------|------------|----------|
| FR-EVT-067 | Struct-layout reflection (canonical-compatible declaration) | `tests/event-system/struct_layout_test.cs` | Stage 0+1 | Test report. |
| FR-EVT-068 | Spec #20 lint (no `UnityEngine.Object` reference in payload) | `tools/spec20-lint/no-unity-singleton.rule` | Stage 0+1 | Lint report. |
| FR-EVT-069 | Registry validator (single-table check) | `tools/spec-validator/registry-uniqueness.py` | Stage 0 | Validator log. |
| FR-EVT-070 | Spec-review (no wire-format claims in Stage 0 text) | Spec-review | Stage 0 | Review diff. |
| FR-EVT-071 | Stage 1+ tracking review (when registry approaches 200 rows) | Tracking review against `appendices.md` row count | Stage 1+ | Tracking entry. |
| FR-EVT-072 | Spec #9 Fixed64 re-verification suite (Stage 5+) | Stage 5+ Fixed64 suite | Stage 5+ | Stage 5+ activation log. |

### 5.4.9 Subscriber-lifetime (FR-EVT-073 … 078)

| FR-EVT-### | Verification | Tooling | Activation | Artifact |
|------------|--------------|---------|------------|----------|
| FR-EVT-073 | `SubscriptionToken` struct-shape unit test (no class allocation) | `tests/event-system/subscription_token_test.cs` | Stage 0+1 | Test report. |
| FR-EVT-074 | Registration-order dispatch unit test (deterministic) | `tests/event-system/registration_order_test.cs` | Stage 0+1 | Test report. |
| FR-EVT-075 | Re-entrant publish FIFO unit test (`intraPhaseDrawIndex` monotonic at enqueue) | `tests/event-system/reentrant_fifo_test.cs` | Stage 0+1 | Test report. |
| FR-EVT-076 | Wrong-tier-marker registration unit test (→ `ERR_EVT_TIER_MISMATCH`) | `tests/event-system/wrong_marker_test.cs` | Stage 0+1 | Test report. |
| FR-EVT-077 | Stage 1+ replay-channel separation test (ordinary subscribers do NOT see replay) | `tests/event-system/replay_isolation_test.cs` | Stage 1+ | Stage 1+ activation log. |
| FR-EVT-078 | Spec #20 lint (`in T evt` parameter required) | `tools/spec20-lint/in-ref-only.rule` | Stage 0+1 | Lint report. |

### 5.4.10 Error codes + failure modes (FR-EVT-079 … 082)

| FR-EVT-### | Verification | Tooling | Activation | Artifact |
|------------|--------------|---------|------------|----------|
| FR-EVT-079 | Error-code namespace validator (`0x17NN` block scan; non-collision with `0x16NN`) | `tools/spec-validator/error-code-block.py` + grep | Stage 0 | Validator log. |
| FR-EVT-080 | Fixture-load unit test (unknown ordinal → `ERR_EVT_ORDINAL_UNKNOWN`) | `tests/event-system/unknown_ordinal_test.cs` | Stage 0+1 | Test report. |
| FR-EVT-081 | Fixture-load unit test (newer `payloadVersion` → `ERR_EVT_VERSION_INCOMPATIBLE`) | `tests/event-system/version_incompat_test.cs` | Stage 0+1 | Test report. |
| FR-EVT-082 | Debug-build assertion in `Publish<IEventA>` overload (non-`Events` phase → `ERR_DS_PHASE_OWNERSHIP`) | `Debug.Assert` + `tests/event-system/phase_ownership_test.cs` | Stage 0+1 | Test report. |

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
| 0.2     | May 13, 2026 | Claude Code | PASS 1 critique H3 resolution. §5.4 expanded from 6 example rows to the full FR-to-verification table — 82 base FRs + 3 added FRs (FR-EVT-046a, 046b, 009a), partitioned into §5.4.1 … §5.4.10. Every FR row has Tooling + Activation + Artifact columns populated. §9.2 Q2 evidence row updated. |
