# Performance Optimization Strategy Specification #18 — Outline

## Purpose
Provide a repeatable framework to keep simulation and AI workloads within frame-time budgets.

## Scope
Budgets, profiling methodology, optimization lifecycle, CI gates, degradation modes, and reporting.

## Section Plan
- Section 1 — Subsystem budgets (AI, physics, collisions, eventing, render prep).
- Section 2 — Profiling methodology, benchmark scenes, and sampling cadence.
- Section 3 — Optimization ladder (measure → attribute → fix → verify → lock).
- Section 4 — Regression thresholds and CI performance-gate policy.
- Section 5 — Fallback/degradation strategies under budget stress.
- Section 6 — Performance instrumentation and dashboard metrics.
- Section 7 — Validation protocols for improvements and non-regressions.
- Section 8 — Reporting format for ongoing performance health.
- Section 9 — Approval checklist.
- Appendices — Baseline captures and benchmark scene definitions.

---

## ADVERSARIAL REVIEW — May 6, 2026

> Reviewer: AI agent (claude/review-spec-sections-JQ8jy). Scope: this outline file
> measured against `CLAUDE.md`, the 9-section template, and adjacent specs.
> Severity legend: **H** = blocks draft start; **M** = must resolve during draft;
> **L** = follow-up.

### Verified premises
- Spec #18 status in `SPEC_INDEX.md`: NOT STARTED.
- Per-spec budgets already exist in approved specs: Shot Mechanics #6 §4.5
  (0.05ms total, ~0.017ms estimated). Pass Mechanics #5 has comparable
  budgets (currently SUSPENDED).
- CLAUDE.md "When Writing Code": Stage 0 uses `float`; struct-based,
  zero-allocation architecture in the game loop; deterministic replay is
  a hard requirement.

### Findings

1. **[H] Missing metadata header.** Same gap as siblings. Add per Shot
   Mechanics #6 outline header.

2. **[H] Section plan deviates from CLAUDE.md template.** Per template,
   §6 is performance budgets *of this spec* (meta-trivial here), §7 is
   future extensions, §8 is references. Current outline puts validation
   in §7 (future-extension slot) and reporting in §8 (references slot).
   No references slot. Re-map.

3. **[H] Authority over per-subsystem budgets unresolved.** §1 "subsystem
   budgets (AI, physics, collisions, eventing, render prep)" — but every
   approved spec already declares its own §6 / §4.5 budget. Two readings
   are possible:
   (a) Spec #18 *ratifies* the per-spec budgets (read-only roll-up).
   (b) Spec #18 *overrides* them and per-spec §6 sections become
       advisory.
   Both have consequences. Pick one before drafting; otherwise an audit
   will find conflicting authoritative budgets.

4. **[H] "Fallback/degradation strategies" conflict with deterministic
   replay.** §5 implies dynamic LOD or simulation-fidelity reduction under
   stress. CLAUDE.md mandates deterministic replay; any degradation
   path that produces different authoritative outputs breaks replay.
   Outline must restrict degradation to Tier C (cosmetic) per
   Deterministic Simulation #16 §1.3, or admit Stage 0 has no degradation.

5. **[H] Boundary with Testing Strategy #19 §5 unstated.** §7 "validation
   protocols for improvements and non-regressions" duplicates Spec #19
   "quality gates for spec-complete and implementation-ready states".
   Either Spec #18 owns performance regressions and Spec #19 owns
   functional regressions, or one defers to the other. Pre-commit.

6. **[H] Boundary with Deterministic Simulation #16 §8 unstated.** §6
   "performance instrumentation and dashboard metrics" overlaps with
   #16 §8 (trace channels, performance budgets per verbosity tier).
   Either #16 owns the trace pipeline and #18 owns aggregated dashboards,
   or boundary must be drawn elsewhere.

7. **[M] CI gates infeasible at Stage 0.** §4 "CI performance-gate policy"
   presumes a CI environment that does not exist in spec phase. Approval
   Checklist must either define Stage-0-feasible offline benchmarking
   only, or scope CI to Stage 1+ implementation phase.

8. **[M] Baseline-capture ownership undefined.** Appendices "baseline
   captures" must live somewhere version-controlled and reproducible.
   Outline does not name a location, format, or refresh cadence. Without
   this, baselines are non-verifiable — repeat of ERR-005 risk.

9. **[M] Platform target list missing.** Performance budgets only mean
   anything against a target platform. CLAUDE.md "Fixed64 stage scope"
   defers cross-platform parity to Stage 5+, but Stage 0 still has a
   reference platform (presumably developer Windows + Unity 2022 LTS).
   Declare.

10. **[M] No reference to fixed timestep / tick-rate budget split.**
    The 10 Hz tactical loop has 100ms per tick; the 60 Hz physics loop
    has ~16.67ms per frame. Per-tick budgets must be split by which
    loop they live on. Outline does not surface this.

11. **[M] Profiling methodology must use deterministic seed and trace.**
    §2 "profiling methodology" should pre-commit to running the determinism
    regression scenarios from #16 §7 — otherwise profiling sessions are
    not comparable across runs.

12. **[L] Reporting cadence unstated.** §8 "reporting format" without a
    cadence (per-PR, per-week, per-milestone) is incomplete.

13. **[L] No fast-path / hot-path enumeration policy.** A performance
    spec without an explicit list of "hot paths to keep allocation-free"
    invites scope drift. Reuse the per-spec §6 budget tables as the
    canonical hot-path list.

### Recommended next steps
- Add full metadata header.
- Re-map Section Plan to CLAUDE.md 9-section template.
- Resolve ratify-vs-override authority over per-spec budgets.
- Cross-link to #16 §8 (instrumentation) and #19 §5 (quality gates) with
  explicit ownership boundaries.
- Restrict degradation paths to Tier C only or remove §5.
