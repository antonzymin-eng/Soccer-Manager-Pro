# Performance Optimization Strategy Specification #18 — Section 7: Future Extensions

**Created:** May 13, 2026
**Last Updated:** May 14, 2026 (v0.2 PASS-1 adversarial-review fix pass)
**Purpose:** Enumerates the Stage 0+1 transition deliverables, the
Stage 1 deliverables, the Stage 5+ extensions, the permanent
exclusions (rules that never change posture), and the deferred-
decisions tracker (D1 … D10).

---

## 7.1 Stage 0+1 Transition Deliverables

Items that activate at first `src/` commit + `certification-platform.
md` Stage 0 row populated:

- **Profiler pin** (§3.3.5; §7.5 D1).
- **Allocation-tracker pin** (§6.1; §7.5 D2).
- **Benchmark-framework pin** (§6.1; §7.5 D3).
- **`tools/budget-auditor.py`** (§5.3) initial implementation.
- **`tools/hot-path-union.json`** builder (§3.7.2).
- **Pre-commit hook script** (`tools/run-perf-local.sh`; §6.2 /
  Appendix E).
- **First `src/CLAUDE.md` perf section** (per §4.6).
- **`certification-platform.md` Stage 0 row pinned** (precondition,
  not produced by #18 itself; tracked in CLAUDE.md OPEN ISSUES).
- **§3.5.2 +5% threshold re-evaluated** against actual baseline
  variance (parallel to Spec #19 §3.1.2 pyramid-ratio re-evaluation).

## 7.2 Stage 1 Deliverables

Items that activate when the dashboard front-end ships:

- **Dashboard front-end** + `IDashboardSink` interface declaration
  (deferred from §4.4 per CLAUDE.md "Interface Design Principle";
  declared once the dashboard consumer is concretely specified).
- **Tier C degradation table populated** (§3.6.4).
- **Milestone-baseline trend dashboard** (§3.8.6).
- **Appendix D approved-spec §6 survey populated** (deferred from §9.2
  per §3.1.2 grandfather rule; parallel to Spec #19 Appendix D
  down-scope per its M3 finding).
- **Baseline-reproducibility auditor automated** (§5.4).
- **Network sink for trace channels** (§3.8.2).

## 7.3 Stage 5+ Extensions

Items deferred to the Stage 5 multiplayer scope:

- **Per-platform budget divergence under Fixed64** (§3.9.3; Spec #9
  dependency).
- **Multiplayer perf-cert layer** (Stage 5 multiplayer scope per
  CLAUDE.md "Fixed64 stage scope decision").
- **Cross-platform parity dashboard.**

## 7.4 Permanent Exclusions

Rules whose posture is never changed by any future stage:

- **Tier A dynamic degradation paths** — never permitted (§3.6.2;
  KD-7).
- **"Threshold relaxation by per-PR creep"** — caught by §3.5.6
  absolute guard; never silently accepted.
- **Trace record format that diverges from #16 §3.2.4.1** — never
  permitted (§3.8.8 anti-pattern; KD-11 binding).
  - *Note:* under inverted KD-3, Spec #18 *owns* the trace pipeline,
    so "parallel trace pipeline" is no longer a coherent
    anti-pattern — but a parallel *record format* still is.
- **Trace point inside #16 §3.1.2 without #16-owner sign-off** — never
  permitted (§3.8.3 emission-veto authority).
- **Wall-clock-seeded profiling runs** — never accepted into baseline
  corpus (§3.3.6; KD-6).
- **Per-spec §6 override by #18** — KD-2 permanent rule.

## 7.5 Deferred Decisions Tracker

| ID | Decision | Resolution stage | Notes |
|----|----------|------------------|-------|
| D1 | Sampling profiler pin (Unity Profiler + Tracy vs Superluminal) | Stage 0+1 | Selection criteria in §6.1 |
| D2 | Allocation-tracker pin | Stage 0+1 | Must be IL2CPP-compatible |
| D3 | Benchmark framework pin (BenchmarkDotNet vs Unity Performance Testing Extension) | Stage 0+1 | Must support statistical-significance reporting per §3.4.3 |
| D4 | CI provider | `src/CLAUDE.md` | Parallel to Spec #19 KD-3; selection criteria in §6.1 |
| D5 | Stage 1 adaptive-degradation posture (any Tier B / Tier C dynamic fallbacks?) | Stage 1+ | Strict default: NO dynamic degradation at Stage 0 |
| D6 | Per-platform budget reconciliation rule under Fixed64 | Stage 5+ | Tied to Spec #9 multiplayer scope |
| D7 | Engine-overhead headroom number (§3.1.4) | Stage 0+1 | Once Unity LTS + scripting backend pinned |
| D8 | §3.4.3 statistical-significance N pin | Stage 0+1 | Provisional: 30 samples / 95% CI |
| D9 | §3.5.2 +5% threshold pin (may tighten/loosen after first 30 days of CI data) | Stage 0+1 | Tie to first-month CI baseline variance measurement (§7.1 re-evaluation deliverable) |
| D10 | §3.8.2 verbosity-tier numeric semantics (sampling rate per tier, channel-to-sink defaults) | Stage 0+1 | Pinned once instrumented-profiler chosen |

## 7.6 Version History

| Version | Date         | Author      | Notes |
|---------|--------------|-------------|-------|
| 0.2     | May 14, 2026 | Claude Code | PASS-1 findings resolved: H-3 D9 resolution stage Stage 1→Stage 0+1 (ERR-018-004); L-9 §7.4 emission-veto citation #16 §3.1→§3.1.2. |
| 0.1     | May 13, 2026 | Claude Code | Initial draft from `outline-detailed.md` v1.1 §7. Stage 0+1 transition deliverables, Stage 1 deliverables, Stage 5+ extensions, permanent exclusions (KD-7 / KD-3 / KD-6 / KD-2 / KD-11), and deferred-decisions tracker (D1 … D10) authored. `IDashboardSink` deferral to §7.2 explicit per CLAUDE.md "Interface Design Principle". |
| 0.2     | May 14, 2026 | Claude Code | PASS-1 adversarial-review fix pass (`ERR-018-004`). §7.5 D9 resolution stage re-anchored "Stage 1" → "Stage 0+1" to match FR-PO-031 + §7.1; rolling 30-day re-evaluation no longer delays the Stage 0+1 activation. |
