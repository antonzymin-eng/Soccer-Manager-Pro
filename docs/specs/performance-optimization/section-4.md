# Performance Optimization Strategy Specification #18 — Section 4: Architecture & Integration

**Created:** May 13, 2026
**Last Updated:** May 13, 2026
**Purpose:** Declares the architectural *shape* of Spec #18's
benchmark harness, baseline storage, profiling / dashboard API surface,
interface contracts, CI pipeline topology, and the pointer to
`src/CLAUDE.md` where concrete paths land. Spec #18 declares no
runtime gameplay structures — every file and interface here is
test-side / tooling-side.

---

## 4.1 Benchmark Scene & Harness Layout (shape, not concrete paths)

- **`tools/perf-harness/`** — Stage 0 synthetic harnesses (no `src/`
  yet). Anchor scenarios that exercise the profiling tooling but do
  not yet represent gameplay code (FR-PO-069, FR-PO-072).
- **`tests/perf/`** — Stage 0+1 production benchmarks bound to
  `src/<spec>/` subsystems. Created atomically with the first `src/`
  commit per FR-PO-074.
- **`tests/perf/<spec>/`** — per-spec benchmark directory. Subfolders:
  - `scenarios/` — manifests bound to #16 §5 scenario IDs
   .
  - `baselines/` — per §4.2 layout.
  - `results/` — transient CI outputs; not version-controlled.
- **Cross-spec scenarios** — re-use Spec #19 KD-8 cross-spec scenarios
  at `tests/scenarios/cross-spec/`. Spec #18 does NOT author parallel
  scenarios (KD-3 binding for scenario authority; Spec #19 owns the
  cross-spec catalogue).

Concrete CI provider, profiler binary, and dashboard host are deferred
to `src/CLAUDE.md` per §4.6 (parallel to Spec #19 §4.6).

## 4.2 Baseline Storage Layout (KD-11)

- **Root location:** `tests/data/baselines/` with subfolders per spec
  (FR-PO-064).
- **Stage 0 placeholder:** `docs/specs/performance-optimization/
  baselines/`. Migration to `tests/data/baselines/` is atomic with the
  first `src/` commit; format is identical so no migration script is
  needed.
- **Per-baseline file format** declared in Appendix A. Fields:
  - Session manifest per §3.3.2 (git SHA, seed,
    `EnvironmentFingerprint`, platform pin, scenario manifest ID,
    timestamps, hardware perf-counter snapshot).
  - Captured metrics: per-tick ms breakdown, per-method allocation
    bytes, cache-miss counters where available.
  - Pass/fail vs §3.5.2 threshold (advisory at capture time;
    authoritative at gate-evaluation time).
- **Record-format binding** (KD-11): the on-disk format conforms to
  #16 §3.2.4.1 canonical record format. Per inverted
  KD-3, the record format is #16-authoritative even though the trace
  pipeline that emits records is #18-owned.

## 4.3 Profiling & Dashboard API Surface

### 4.3.1 `IPerfHarness`

Consumed by per-spec benchmark runners. **Single concrete
implementation** — no IoC container, parallel to Spec #20 §3.5.5
anti-pattern list and Spec #19 §4.3. Producer: Spec #18 §3.3 harness
authors. Consumer: Spec #19 `ScenarioRunner` (Spec #19 §3.3.3). Both
sides specified → permitted under CLAUDE.md "Interface Design
Principle".

### 4.3.2 `BaselineRecord` (value type)

Immutable; serialized per Appendix A. Carries the §3.3.2 session
manifest and captured metrics. On-disk encoding conforms to #16
§3.2.4.1.

### 4.3.3 `BudgetRollupEntry` (value type)

Read-only view onto a per-spec §6 declaration. Recomputed at build
time by `tools/budget-auditor.py` (§5.3); never edited by hand. Field
parallel to the §3.1.3 roll-up-table columns.

### 4.3.4 Dashboard helpers

Live in **`tools/perf-dashboard/`**. They MUST NOT reference
`src/<spec>/` gameplay assemblies (Spec #20 §4.1 dependency-arrow rule;
FR-PO-060).

## 4.4 Interface Contracts (this spec exposes)

- **`IPerfHarness`** — implemented by `tests/perf/` harness; consumed
  by Spec #19 `ScenarioRunner`. Producer = Spec #18 §3.3 harness
  authors; consumer = Spec #19 scenario runner. Both sides specified
  → permitted under CLAUDE.md "Interface Design Principle".
- **`IBudgetSource`** — implemented by each per-spec §6 metadata
  extractor; consumed by `tools/hot-path-union.json` builder (§3.7.2).
  Both sides specified.
- Both live in `tools/perf-harness/` and `tools/` respectively per
  §4.1 / §4.3; no game-state code may reference them.

**`IDashboardSink` is intentionally NOT declared here.** Per the
CLAUDE.md "Interface Design Principle" (only declare interfaces when
both sides are specified — ERR-001 / ERR-004 hazard from CLAUDE.md
"Things That Have Gone Wrong"), the dashboard consumer (a web UI, a
Grafana plugin, an in-editor panel) is unspecified at Stage 0. The
interface is deferred to §7.2 Stage 1 deliverables and is declared
once the dashboard front-end is concretely specified. Parallel to
Spec #19's `IFlakeReporter` deferral.

## 4.5 CI Pipeline Topology — Perf Step (shape only; concrete config Stage 1+)

- **Pre-commit pipeline:** no perf step (too slow for pre-commit).
  Schema-conformance and loop-tag auditors only — see §6.2 local
  runbook.
- **PR pipeline:** per-spec-changed perf benchmark + alloc-tracker
  step. Block on §3.5.2 threshold (FR-PO-036) or §3.7.4 alloc
  violation (FR-PO-037).
- **Nightly pipeline:** full perf benchmark suite + absolute-threshold
  guard (§3.5.6, FR-PO-039) + milestone-baseline trend update.

**Pipeline diagram (shape).**

```
trigger
   │
   ├─→ functional gate (Spec #19 §6.2)
   │      └─ on failure: block, short-circuit perf step
   │
   ├─→ determinism gate (Spec #16 §5 + §3.2.4.1)
   │      └─ on failure: block
   │
   ├─→ performance gate (Spec #18 §3.5.2)
   │      └─ on failure: block
   │
   ├─→ allocation gate (Spec #18 §3.7.4)
   │      └─ on failure: block
   │
   └─→ exit (all gates pass → merge eligible)
```

Concrete CI provider selection is deferred to `src/CLAUDE.md`
(parallel to Spec #19 KD-3); selection criteria recorded in §6.1.

**Composition with #19's functional pipeline.** Perf step runs after
the functional step; functional failure short-circuits the perf step
(no point measuring a broken build).

## 4.6 Pointer to `src/CLAUDE.md`

Concrete paths, profiler invocation commands, allocation-tracker
invocation, and CI perf-step configuration land in `src/CLAUDE.md`
when coding begins. Spec #18 declares the *shape*; `src/CLAUDE.md`
declares the *paths*. Parallel to Spec #19 §4.6.

Deferred items (§7.5 D1–D10) that consume `src/CLAUDE.md` slots:

- D1 — sampling-profiler binary + invocation command.
- D2 — allocation-tracker binary + invocation command.
- D3 — benchmark-framework binary + invocation command.
- D4 — CI provider + perf-step config path.
- D7 — engine-overhead headroom number for §3.1.4.

## 4.7 Version History

| Version | Date         | Author      | Notes |
|---------|--------------|-------------|-------|
| 0.1     | May 13, 2026 | Claude Code | Initial draft from `outline-detailed.md` v1.1 §4. Declares benchmark harness layout, baseline storage layout (KD-11 binding), `IPerfHarness` / `IBudgetSource` interface contracts (both sides specified per CLAUDE.md "Interface Design Principle"), `IDashboardSink` deferred to Stage 1 per CLAUDE.md ERR-001 / ERR-004 hazard, CI pipeline topology, and `src/CLAUDE.md` pointer. All #16 / #19 citations tagged. |
