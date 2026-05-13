# Performance Optimization Strategy Specification #18 — Detailed Outline

**Created:** May 13, 2026
**Last Updated:** May 13, 2026
**Version:** 1.0
**Status:** DRAFT — addresses all 13 findings (6 H / 5 M / 2 L) from the
May 6, 2026 adversarial review at the bottom of `outline.md`. Ready for
section-file authoring.
**Companion documents:** `outline.md` (high-level + adversarial review).
**Unblocks:** Spec #19 KD-2 sequencing precondition "Spec #18 has at
least an outline-level draft with §4 and §7 headers" (per
`docs/specs/testing-strategy/outline-detailed.md` v1.1 §1.4). After this
file lands, the §6.1 / §7.2 references in #19 §6 cease being citations
against a `NOT STARTED` spec and the corresponding `TBD-NORMATIVE` tags
in #19 can begin resolving (full resolution still requires #18 reaching
`APPROVED`).

---

## PURPOSE OF THIS DOCUMENT

Expansion of `outline.md` into a section-by-section subsection plan that
resolves every finding from the May 6, 2026 adversarial review. For every
subsection: the rules / FRs it will publish, the boundary declarations it
will hold, and the cross-references it will emit. Detailed enough that
`section-1.md` … `section-9-approval-checklist.md` and `appendices.md`
can be drafted directly from this document.

This document does **not** publish FR text in normative form — that text
lands in `section-2.md`. The detailed outline records every FR's intended
rule, conformance level, and source so the FR table can be authored
mechanically.

---

## CROSS-CUTTING DESIGN DECISIONS

These decisions are referenced throughout the outline. They are stated
once here and cited below by KD-number, never restated.

- **KD-1 — Cite-not-redefine.** Spec #18 never restates a CLAUDE.md
  invariant or a rule already published by another approved spec. It
  cites and binds. In particular: tick-rate definitions, coordinate
  system, fatigue convention, and "zero-allocation game loop" mandate
  are cited from CLAUDE.md; per-spec budget numbers are cited from each
  spec's §6 (or §4.5 in Shot Mechanics #6's case).

- **KD-2 — Per-spec §6 sections remain authoritative for their own
  budget; Spec #18 RATIFIES, does not OVERRIDE.** Each approved spec's
  §6 (or §4.5) declares its own per-frame / per-tick budget; that
  declaration is the authoritative source for that subsystem's budget.
  Spec #18 publishes (a) the roll-up table that aggregates every
  per-spec budget into a system-wide picture, (b) the budget-allocation
  rule any new spec must follow when claiming a budget slice, (c) the
  regression-gate policy that enforces those budgets in CI, and (d) the
  procedure for re-allocating budget between subsystems when total
  exceeds platform headroom. Spec #18 does not republish per-spec
  numbers — it links to each spec's §6 by reference.
  - **Authority resolution (H3 of `outline.md` review).** Reading (a)
    "ratify, read-only roll-up" is adopted; reading (b) "override per-spec
    §6" is rejected. Rationale: per-spec budgets are derived from per-spec
    algorithmic structure (e.g., Shot Mechanics #6 §4.5's 0.05ms total is
    derived from its algorithm complexity, not from a top-down quota);
    overriding them would invalidate the per-spec §6 sections that have
    already been adversarially reviewed and approved. Re-allocation when
    total exceeds platform headroom is handled via §3.1.5 negotiation
    procedure, not by unilateral #18 override.

- **KD-3 — Boundary with Deterministic Simulation #16.** Spec #16 §7
  ("Determinism Regression Suite") is the authoritative owner of the
  determinism regression scenarios; Spec #16 §8 ("Trace Channels") is
  the authoritative owner of the trace-channel architecture and
  per-verbosity-tier instrumentation costs. Spec #18 consumes both as
  inputs: profiling sessions (§3.3) run the #16 §7 scenarios verbatim
  for reproducibility; dashboards (§3.8) aggregate the per-tick metrics
  surfaced by #16 §8's trace pipeline. Spec #18 does not duplicate
  trace-channel definitions and does not add new determinism scenarios.
  - **Status caveat (May 13, 2026).** Per `SPEC_INDEX.md`, Spec #16 is
    `IN PROGRESS`, not `APPROVED`. All §3.3 / §3.8 / §3.5 citations of
    "#16 §7", "#16 §8" are tagged `TBD-NORMATIVE` (pattern adopted from
    #16 §8.3.1 per CLAUDE.md OPEN ISSUES). Section files MUST carry the
    tag verbatim on every #16 citation; tag removal is a §9.2
    quality-checklist row and is gated on #16 approval.
  - **Sequencing constraint.** Per CLAUDE.md OPEN ISSUES, #16's Tier 2
    final approval is gated on `#9 / #17 / #18 / #19 reaching IN
    REVIEW`. Spec #18 binds substantively to #16 §7 / §8 but does not
    itself gate #16 Tier 2 once #18 reaches `IN REVIEW`. Resolution
    path mirrors #19's: (1) #18 reaches `IN REVIEW` with
    `TBD-NORMATIVE` citations to #16; (2) #16 reaches Tier 2
    `APPROVED`; (3) #18's `TBD-NORMATIVE` tags resolve and #18 advances
    to `APPROVED`.

- **KD-4 — Boundary with Testing Strategy #19.** Spec #19 §6 owns the
  CI orchestration policy and functional regression gates; Spec #18 §3.5
  owns the performance regression gates. Both feed the single CI
  orchestrator described in #19 §6.2. Specifically: a CI run that
  produces "all tests pass but a perf budget regressed by N%" is a
  Spec #18 §3.5 block, not a Spec #19 functional block. Conversely, a
  CI run where a perf benchmark times out because a functional assert
  threw is a Spec #19 §3.7 (flake / functional failure) block, not a
  Spec #18 regression. Boundary is enforced by per-gate ownership in
  §3.5.3 gate-composition table.
  - **Status caveat (May 13, 2026).** Per `SPEC_INDEX.md`, Spec #19 is
    `IN REVIEW`, not `APPROVED`. Citations of "#19 §6", "#19 §3.7" are
    tagged `TBD-NORMATIVE`. #19's own approval is gated in part on
    #18's outline existing (this file); the symmetric tag removal on
    #18's side waits for #19 `APPROVED`.

- **KD-5 — Stage-gated activation.** Sections that presume an
  implemented codebase (CI perf gates, dashboards, automated baseline
  capture) are written as contracts that activate at the Stage 0 →
  Stage 1 transition. They are first-class normative content of this
  spec but are not enforceable during the spec-writing phase. Stage 0
  has manual offline benchmarking against synthetic harnesses only
  (§6.2 local runbook). Per-FR activation status tracked in §5.2.

- **KD-6 — Determinism-aware profiling.** All profiling sessions MUST
  run under #16 §7's determinism regression scenarios with explicit
  recorded seeds. Wall-clock or random-seed profiling runs are forbidden
  (they are not comparable across revisions and cannot bisect a
  regression). Seed selection at session start is permitted; the
  selected seed MUST be logged with the captured baseline. This is the
  performance-side equivalent of #19 KD-7 ("Determinism-aware fuzz
  testing").

- **KD-7 — Degradation paths restricted to Tier C only (deterministic-
  replay safety).** Per CLAUDE.md "Deterministic replay is a hard
  requirement" and #16 §1.3 tier classification:
  - **Tier A (authoritative)** outputs MUST NOT vary under performance
    pressure. Any degradation path that changes a Tier A output (ball
    physics result, agent decision, event emission) is forbidden.
  - **Tier B (bounded-authoritative)** outputs MAY vary within their
    declared tolerance under degradation; budget-stress fallbacks that
    stay within Tier B tolerance are permitted but must be declared at
    spec time, not adopted at runtime.
  - **Tier C (non-authoritative)** outputs — render LOD, debug overlay
    fidelity, telemetry sampling rate, dashboard refresh frequency —
    are the only acceptable runtime degradation surface.
  - **Stage 0 posture.** Stage 0 declares NO dynamic degradation paths.
    All Stage 0 budget enforcement is via measurement + manual
    remediation (i.e., the developer optimizes the code path until it
    fits the budget). Runtime adaptive fallbacks are a Stage 1+ design
    decision tracked as a deferred decision in §7.5.

- **KD-8 — Loop separation (10 Hz tactical vs 60 Hz physics).** Per
  CLAUDE.md "Heartbeat Tick Rate", every per-spec budget number MUST be
  tagged with the loop it lives on. The 10 Hz tactical loop has 100ms
  per tick; the 60 Hz physics loop has ~16.67ms per frame. Budgets
  cross-listed against the wrong loop are a category error and are
  caught by the §3.2 budget-tagging audit.

- **KD-9 — Reference platform pin.** Performance budgets are only
  meaningful against a pinned reference platform. Spec #18 binds to
  `docs/tracking/certification-platform.md` (Stage 0 row) for OS,
  Unity LTS revision, scripting backend (Mono / IL2CPP), compiler-flag
  set, worker thread count, and SIMD feature level.
  - **Status caveat (May 13, 2026).** Per CLAUDE.md OPEN ISSUES,
    `certification-platform.md` Stage 0 row is mostly `_TBD_` /
    `⏳ Not pinned`. Spec #18 drafting does NOT require those pins to
    be filled in; first activation of a perf gate (Stage 0+1
    transition) does. §1.4 + §5.2 stage-gating table mark every
    activation FR with "criterion: certification-platform Stage 0 row
    populated".

- **KD-10 — Hot-path enumeration policy.** The set of "hot paths" that
  must remain allocation-free is the union of every per-spec §6 (or
  §4.5 in Shot Mechanics #6's case) budget table; Spec #18 does not
  maintain a separate authoritative hot-path list. The allocation
  budget on every hot path is zero per CLAUDE.md "When Writing Code:
  zero-allocation architecture in the game loop". Spec #18 §3.7
  publishes the *roll-up rule* (how to enumerate the union) and the
  *enforcement mechanism* (per-build alloc-tracking dump compared
  against the union), not the individual entries.

- **KD-11 — Baseline reproducibility & storage.** Every baseline
  capture is reproducible from (a) the recorded git SHA, (b) the
  recorded seed (KD-6), (c) the recorded `EnvironmentFingerprint` (per
  #16 §4), and (d) the pinned platform (KD-9). Baselines live in a
  version-controlled location (`tests/data/baselines/` once `src/`
  exists; Stage 0 placeholder
  `docs/specs/performance-optimization/baselines/`) with the format
  declared in Appendix A. Capture cadence: per-PR delta at Stage 0+1,
  full re-baseline at each Stage milestone.

---

## SECTION 1 — PURPOSE & SCOPE (`section-1.md`)

### 1.1 What This Specification Covers

**Subsection target length:** ~40 lines.

**Content:**
- Opening declarative scope statement: this spec governs how performance
  budgets are tracked, measured, regression-gated, and remediated across
  the Tactical Director codebase.
- Bullet list of governance areas (9 items): budget roll-up authority
  (KD-2), loop-separation tagging (KD-8), determinism-bound profiling
  methodology (KD-6), optimization ladder (measure → attribute → fix →
  verify → lock), performance regression gates with #16/#19 boundary
  (KD-3 / KD-4), degradation-path restrictions (KD-7), hot-path
  enumeration policy (KD-10), instrumentation/dashboard mechanics with
  #16 §8 boundary, baseline reproducibility (KD-11).
- Applicability block:
  - **Primary:** every `src/<spec>/` subsystem with a §6 budget once
    coding begins.
  - **Secondary (governance-only):** every spec's §6 section in
    `docs/specs/`. Spec #18 publishes the roll-up rule and budget-tagging
    schema those §6 sections must conform to; it does not rewrite them
    (KD-2).
- Closing pointer to §3 (mechanics) and §5 (verification).

### 1.2 What Is Out of Scope

**Subsection target length:** ~30 lines.

One-line entries with the owning document:

- Determinism regression scenarios and tier classification → Spec #16 §7 / §1.3 (KD-3).
- Trace channel architecture and per-verbosity instrumentation cost → Spec #16 §8 (KD-3).
- Functional / behavioural regression gates → Spec #19 §3 / §6 (KD-4).
- Test pyramid, coverage targets, flake handling → Spec #19 (KD-4).
- Numeric correctness of physics/AI formulas → owning specs (#1–#8) §3.
- Fixed64 numeric perf budgets → Spec #9 §6 (Stage 5+ scope per CLAUDE.md).
- C# code style and banned-allocation patterns → Spec #20 §3.
- CI server choice, build commands, IDE configuration → `src/CLAUDE.md` (deferred until coding begins).
- Asset-pipeline / render-thread budgets → Stage 1+ specs (not gameplay loops).
- Dynamic runtime degradation (LOD, adaptive fidelity) for Tier A / Tier B → permanently out of scope per KD-7.

### 1.3 Key Design Decisions

Full restatement of KD-1 … KD-11 with one-line rationale and the section
that codifies each:

| KD | Topic | Codified in |
|----|-------|-------------|
| KD-1 | Cite-not-redefine | All sections |
| KD-2 | Per-spec §6 ratify, not override | §3.1, §3.1.5 |
| KD-3 | Boundary with #16 §7 / §8 | §3.3, §3.8, §5.7 |
| KD-4 | Boundary with #19 §6 | §3.5, §6.3 |
| KD-5 | Stage-gated activation | §5.2, §7 |
| KD-6 | Determinism-aware profiling | §3.3, §3.3.4 |
| KD-7 | Degradation paths restricted to Tier C | §3.6, §7.4 |
| KD-8 | Loop separation (10 Hz / 60 Hz) | §3.2 |
| KD-9 | Reference platform pin | §1.4, §3.2.5 |
| KD-10 | Hot-path enumeration policy | §3.7 |
| KD-11 | Baseline reproducibility & storage | §3.4, §4.2, Appendix A |

### 1.4 Dependencies and Integration Contracts

- **Upstream (substantive):**
  - Root `CLAUDE.md` (tick rates, zero-allocation mandate, deterministic
    replay, Stage 0 host platform pin).
  - Spec #16 (Deterministic Simulation) §1.3 tier classification, §4
    `EnvironmentFingerprint`, §7 regression scenarios, §8 trace
    channels. **Status:** `IN PROGRESS`. All citations tagged
    `TBD-NORMATIVE` until #16 approval (KD-3 status caveat). Section
    authors MUST grep `deterministic-sim/section-*.md` for current
    subsection numbers at draft time (#16 has been through three
    adversarial passes and subsection numbering may have shifted).
- **Upstream (consulted):**
  - Spec #19 (Testing Strategy) §6 CI orchestration, §3.7 flake
    handling. **Status:** `IN REVIEW`. Citations tagged
    `TBD-NORMATIVE` per KD-4 status caveat.
  - Spec #20 (Code Standards) §3 zero-allocation rules.
  - Each approved spec's §6 (or §4.5 in Shot Mechanics #6's case): #1
    Ball Physics §6, #2 Agent Movement §6, #3 Collision System §6, #4
    First Touch §6, #5 Pass Mechanics §6, #6 Shot Mechanics §4.5
    (verified — Shot Mechanics §4.5 declares 0.05ms total / ~0.017ms
    estimated per outline.md adversarial review verified premises), #7
    Perception §6, #8 Decision Tree §6, #17 Event System §6.
- **Bidirectional sequencing with #16:** Per CLAUDE.md OPEN ISSUES, #18
  reaching `IN REVIEW` is a precondition for #16's Tier 2 `APPROVED`;
  #16's `APPROVED` is a precondition for #18's own `APPROVED` (so
  `TBD-NORMATIVE` tags can be resolved). See KD-3 sequencing constraint.
- **Bidirectional sequencing with #19:** #19's advancement past
  `IN REVIEW` is gated on #18 having an outline-level draft with §4
  and §7 headers (this document). #18's approval is not symmetrically
  gated on #19's approval, but `TBD-NORMATIVE` tags on #19 citations
  can only resolve once #19 is `APPROVED`.
- **Downstream:**
  - Every per-spec §6 (consumes Spec #18 budget-tagging schema and
    roll-up rule).
  - `src/CLAUDE.md` (consumes pinned tooling, profiler invocation
    commands).
  - CI configuration files (Stage 1+).
- **Cross-spec constants imported:** none. Spec #18 imports tier
  *vocabulary* from #16 §1.3 by reference (KD-1 cite-not-redefine); no
  `[CROSS]` constant declarations. Per-spec budget numbers are cited by
  reference, not republished.
- **Stage 0 host platform pin:** Spec #18's regression gates require
  the pins named in `docs/tracking/certification-platform.md`. Drafting
  Spec #18 does not require those pins to be filled in; first
  activation of a perf gate (Stage 0+1 transition) does. Tracked as
  §5.2 activation criterion.

### 1.5 Version History

Standard version-history table (initially empty, populated on draft).

---

## SECTION 2 — FUNCTIONAL REQUIREMENTS & BUDGET GOVERNANCE MODEL (`section-2.md`)

### 2.1 Conformance Levels

- MUST / SHOULD / MAY (RFC 2119 cited).
- "Exception with sign-off" semantics identical to Spec #20 §2.1 / Spec
  #19 §2.1.

### 2.2 Functional Requirement Catalogue

All FR-PO-### live here with rule statement, conformance level, source
citation, and verification pointer (`§5.x`). Detailed outline names the
partition; section file fills in every numbered FR.

| FR Range | Topic | Rule mechanics in |
|----------|-------|-------------------|
| FR-PO-001 … 008 | Budget roll-up authority & per-spec §6 schema (KD-2) | §3.1 |
| FR-PO-009 … 015 | Loop separation (10 Hz / 60 Hz) tagging (KD-8) | §3.2 |
| FR-PO-016 … 023 | Profiling methodology, determinism binding (KD-6) | §3.3 |
| FR-PO-024 … 030 | Optimization ladder (measure → attribute → fix → verify → lock) | §3.4 |
| FR-PO-031 … 040 | Performance regression gates, boundary with #16 / #19 (KD-3 / KD-4) | §3.5, §6.3 |
| FR-PO-041 … 047 | Degradation policy, Tier C only (KD-7) | §3.6 |
| FR-PO-048 … 053 | Hot-path enumeration & zero-allocation enforcement (KD-10) | §3.7 |
| FR-PO-054 … 062 | Instrumentation & dashboard mechanics, boundary with #16 §8 (KD-3) | §3.8 |
| FR-PO-063 … 068 | Baseline reproducibility & storage (KD-11) | §3.4, §4.2 |
| FR-PO-069 … 074 | Stage-0 manual benchmarking & local runbook (KD-5) | §6.2 |
| FR-PO-075 … 080 | Reporting cadence & defect lifecycle | §6.4, §6.5 |

Each FR row: `ID | Statement | Level | Source citation | Verification (§5.x) | Activation stage`.

### 2.3 Failure-to-Comply Modes

- **Budget overrun** (subsystem exceeds declared §6 budget by >N%, N
  pinned in §3.5.2): regression gate blocks merge.
- **Allocation in hot path** (KD-10): regression gate blocks merge;
  Tier A allocation is a Critical defect (§6.5).
- **Untagged budget loop** (KD-8): per-spec §6 review rejects spec.
- **Non-deterministic profiling run** (KD-6): baseline rejected at
  capture time; not entered into baseline corpus.
- **Tier-A degradation path proposed** (KD-7): spec review rejects.
- **Per-spec §6 schema drift** (KD-2): §5.3 conformance auditor flags.

### 2.4 Data Structures (informational)

- Spec #18 defines no runtime data structures used by gameplay.
- Performance-harness data structures (`BaselineRecord`,
  `BudgetRollupEntry`, `ProfilingSessionManifest`) are declared in §4
  and Appendix A; their on-disk encoding conforms to #16 §5 canonical
  binary layout where applicable (KD-11).

### 2.5 Failure Modes

Spec #18's own failure modes (in addition to §2.3):
- Per-spec §6 schema drift — discovered by §5.3 conformance check.
- Baseline non-reproducibility (missing seed, missing fingerprint,
  missing platform pin) — caught by §3.4.4 baseline validator.
- Budget-allocation total exceeds platform headroom — handled by §3.1.5
  re-allocation procedure; never silently truncated.
- Dashboard divergence from #16 §8 trace pipeline — caught by §3.8.3
  boundary audit.

### 2.6 Version History

---

## SECTION 3 — TECHNICAL SPECIFICATION (rule mechanics) (`section-3.md`)

> Each subsection cites the FR-PO-### IDs it implements (defined in
> §2.2) and provides the *mechanics*. It does not redefine the rule
> statement.

### 3.1 Budget Roll-up & Per-Spec §6 Schema (FR-PO-001 … 008)

- 3.1.1 Authority statement (KD-2):
  - Per-spec §6 (or §4.5 in #6's case) declares the spec's budget;
    Spec #18 ratifies via roll-up; Spec #18 does not override.
- 3.1.2 Per-spec §6 schema:
  - Every §6 MUST publish: total per-tick budget (ms), per-tick budget
    by loop tag (10 Hz / 60 Hz per KD-8), allocation budget (always 0
    on hot paths per KD-10), worst-case input parameters that yield
    the budget, and a "headroom" multiplier reserved for variance.
  - Schema published in Appendix B as paste-ready template.
- 3.1.3 Roll-up table:
  - Single roll-up table per platform target maintained in Appendix C;
    columns: spec ID, declared budget, loop tag, alloc budget,
    citation (link to spec §6), last verified date.
  - Roll-up table is read-only relative to per-spec §6; updating it is
    a mechanical sync, not a design decision.
- 3.1.4 Platform headroom:
  - 60 Hz frame budget: ~16.67ms per frame; minus engine overhead
    (renderer, audio, input poll, GC pump) leaves the gameplay-loop
    slice. Concrete engine-overhead number pinned at Stage 0+1 once
    Unity LTS revision and scripting backend are fixed in
    `certification-platform.md`.
  - 10 Hz tick budget: 100ms per tick; same headroom decomposition.
  - **Stage 0 placeholder.** Until `certification-platform.md` is
    pinned, headroom is recorded as `[EST]` per CLAUDE.md constant
    tags, with a §3.1.4 placeholder row in Appendix C explicitly
    marking it.
- 3.1.5 Re-allocation procedure:
  - Triggered when the §3.1.3 roll-up total exceeds the §3.1.4 headroom
    on any platform target.
  - Process: lead-developer convenes an explicit re-allocation review;
    each affected spec's §6 is amended (with version-history entry);
    Spec #18 §3.1.3 roll-up table updated atomically.
  - No silent re-allocation; no unilateral #18 override.
  - The procedure is documented as a normative section here (§3.1.5)
    rather than left implicit.

### 3.2 Loop Separation & Per-Tick Budget Mechanics (FR-PO-009 … 015)

- 3.2.1 Citation: CLAUDE.md "Heartbeat Tick Rate" — 10 Hz tactical, 60
  Hz physics.
- 3.2.2 Loop-tag mandate:
  - Every budget number in every spec's §6 MUST carry a loop tag:
    `[LOOP-TACTICAL-10HZ]` or `[LOOP-PHYSICS-60HZ]`.
  - Untagged numbers are a §5.3 conformance failure.
- 3.2.3 Cross-loop subsystems:
  - Subsystems that run in both loops (e.g., Decision Tree #8 produces
    tactical decisions at 10 Hz but reads physics state updated at 60
    Hz) declare separate budgets for the work each loop performs.
- 3.2.4 Aggregation rule:
  - The 60 Hz budget total includes only 60 Hz tagged entries; the 10
    Hz budget total includes only 10 Hz tagged entries.
  - Mixed totals are forbidden — they obscure where time is spent and
    invite category errors.
- 3.2.5 Platform target pinning (KD-9):
  - Budgets are stated against the platform pinned in
    `certification-platform.md` Stage 0 row.
  - Until that row is pinned, all numeric budgets carry both the loop
    tag AND the `[EST]` source tag per CLAUDE.md "Constant Tags";
    these are promoted to `[GT]` or `[FIXED]` once the platform pin
    lands.
- 3.2.6 Anti-patterns:
  - "Per-second" budget (ambiguous — is that 10 ticks or 60 frames?).
  - "Per-call" budget without amortized call rate.
  - Budget cited without loop tag.

### 3.3 Profiling Methodology — Determinism-Bound (FR-PO-016 … 023)

- 3.3.1 Citation: KD-6 (determinism-aware profiling); KD-3 boundary
  with #16 §7 (regression scenarios).
- 3.3.2 Profiling session contract:
  - Every session declares: git SHA, recorded seed, recorded
    `EnvironmentFingerprint` (from #16 §4), platform pin per KD-9,
    scenario manifest (#16 §7 scenario ID), session start/end
    timestamps, hardware perf-counter snapshot.
  - Sessions missing any field are not entered into the baseline
    corpus (§3.4.4 validator rejection).
- 3.3.3 Scenario binding:
  - Spec #18 does not author its own scenarios. Every profiling session
    runs an #16 §7 scenario verbatim.
  - Cross-scenario profiling (a #19 KD-8 cross-spec scenario) is
    permitted; the manifest ID and seed are recorded the same way.
- 3.3.4 Sampling cadence:
  - Sampling-profiler default: 1 kHz wall-clock samples (10 Hz tactical
    loop produces 100 samples per tick) — `[EST]`, pinned to chosen
    profiler at Stage 0+1.
  - Instrumented-profiler default: full-function-entry/exit tracing on
    every hot path (KD-10 union); off by default in shipping builds,
    on by default in baseline-capture builds.
- 3.3.5 Profiler-pin policy:
  - Stage 0: profiler choice deferred; Stage 0 sessions use a manual
    `Stopwatch` harness (Appendix E §6.2 runbook).
  - Stage 0+1: Unity Profiler + Superluminal / Tracy (or equivalent;
    selection criteria parallel to Spec #19 §6.1 — must support
    deterministic re-play, must emit per-frame breakdown, must support
    headless / batch-mode capture for CI).
- 3.3.6 Anti-patterns:
  - Profiling against wall-clock-seeded gameplay.
  - Profiling in editor-mode without scripting-backend pin (Mono vs
    IL2CPP give very different numbers).
  - Capturing without recording `EnvironmentFingerprint`.

### 3.4 Optimization Ladder (FR-PO-024 … 030)

- 3.4.1 Five-rung ladder:
  1. **Measure** — capture baseline per §3.3 session contract.
  2. **Attribute** — identify which function / allocation site /
     cache-miss site dominates.
  3. **Fix** — apply the smallest local change that addresses the
     dominant attribution.
  4. **Verify** — capture post-fix baseline; compare against §3.5.2
     regression-gate threshold; require improvement to be statistically
     significant (§3.4.3 confidence interval rule).
  5. **Lock** — record the new baseline; update Appendix C roll-up
     row; close optimization ticket.
- 3.4.2 Anti-skipping rule:
  - Each rung is mandatory; "I'm sure this will be faster" without
    Measure → Attribute is forbidden. Optimization PRs without
    baseline evidence are blocked at review.
- 3.4.3 Statistical-significance rule:
  - Improvement claims require N samples (N pinned at Stage 0+1) with
    a non-overlapping confidence interval against the pre-fix
    baseline.
  - Below-significance "improvements" are not entered into the
    baseline; they are recorded as a §6.4 defect-class "Inconclusive".
- 3.4.4 Baseline validator (KD-11):
  - Validator checks every captured baseline against §3.3.2 session
    contract; rejects sessions missing any field.
  - Reproducibility check: validator MAY (Stage 0+1) re-run the
    session under the recorded seed + fingerprint + platform pin and
    confirm the captured metric matches within §3.4.3 confidence
    interval.
- 3.4.5 Optimization ticket lifecycle:
  - Tickets reference the FR-PO ID of the gate they're addressing, the
    baseline SHA, and the target metric improvement.
  - Closed tickets reference the post-fix baseline SHA.

### 3.5 Performance Regression Gates (FR-PO-031 … 040)

- 3.5.1 Citation: KD-3 (boundary with #16); KD-4 (boundary with #19);
  KD-5 (Stage-gated activation).
- 3.5.2 Gate threshold:
  - Default regression threshold: post-PR baseline must be within +5%
    (per-spec, per-loop) of the pre-PR baseline for the same scenario,
    seed, and platform pin. `[GT]` — pinned at Stage 0+1.
  - Allocation regression threshold: any non-zero allocation on a hot
    path (KD-10 union) blocks merge regardless of magnitude.
  - Per-spec overrides: a spec's §6 MAY declare a tighter threshold
    (e.g., #6 Shot Mechanics §4.5 already declares a 0.05ms total
    budget; deviations >5% from 0.017ms estimated cite #6 §4.5
    authority, not §3.5.2 default).
- 3.5.3 Gate composition (boundary with #16 / #19):
  - **Functional gate** (Spec #19 §6.2 authority): block on test fail.
  - **Determinism gate** (Spec #16 §7 authority): block on bitwise
    mismatch.
  - **Performance gate** (Spec #18 §3.5.2 authority): block on
    threshold exceeded.
  - **Allocation gate** (Spec #18 §3.7 authority): block on hot-path
    allocation.
  - Gate-composition table records ownership; no gate is "soft"; flake
    quarantine (Spec #19 §3.7) applies to functional gates only —
    perf-gate flake is a determinism failure (KD-6 violation) and
    routes to #16 §7 triage, not #19 §3.7 quarantine.
- 3.5.4 Stage-0 posture (KD-5):
  - Stage 0: no CI perf gate active. Performance regressions are
    surfaced via the §6.2 local runbook against synthetic harnesses
    that exercise pre-`src/` profiling tooling.
  - Stage 0+1: CI perf gate activates with the §3.5.2 threshold
    enforced on per-PR baselines.
- 3.5.5 Anti-patterns:
  - "Threshold exceeded but feature is important" exception: handled
    via the same exception-with-sign-off semantics as Spec #20 §2.1 /
    Spec #19 §2.1, not via silent threshold bypass.
  - Per-PR threshold relaxation by repeated +5% increments
    ("budget creep"): caught by Stage-0+1 absolute-threshold guard
    (§3.5.6).
- 3.5.6 Absolute-threshold guard:
  - Independent of per-PR delta gate, a parallel guard compares
    against the *milestone baseline* (last Stage milestone). Drift
    beyond +10% of milestone baseline blocks merge regardless of how
    incremental the per-PR deltas were. Prevents budget creep.

### 3.6 Degradation Policy — Tier C Only (FR-PO-041 … 047)

- 3.6.1 Citation: KD-7; #16 §1.3 tier classification.
- 3.6.2 Tier A invariant:
  - Authoritative outputs (ball state, agent position, agent decision,
    event emission) MUST NOT vary under performance pressure. Any
    proposed degradation path that touches a Tier A output is rejected
    at spec review.
- 3.6.3 Tier B tolerance:
  - Bounded-authoritative outputs MAY vary within their declared
    tolerance. Tier B degradation paths MUST be declared at spec time
    in the owning spec's §6 (not adopted at runtime), and the
    tolerance band MUST be cited from the owning spec.
- 3.6.4 Tier C surface:
  - Render LOD, debug overlay fidelity, telemetry sampling, dashboard
    refresh — the only acceptable runtime degradation surface.
  - Tier C degradation paths are declared in §3.6.4 itemized table
    (Stage 1 deliverable; Stage 0 declares the policy + an empty
    table).
- 3.6.5 Stage-0 posture:
  - Stage 0 declares NO dynamic degradation paths at all. All Stage 0
    budget enforcement is manual remediation.
  - Stage 1 first-real-code posture for adaptive degradation is a
    deferred decision (D5 in §7.5).
- 3.6.6 Anti-patterns:
  - "Skip a physics sub-step under load" — Tier A violation.
  - "Run AI decision tree every other tick under load" — Tier A
    violation.
  - "Reduce trace verbosity under load" — permitted (Tier C); declare
    in §3.6.4 table.

### 3.7 Hot-Path Enumeration Policy & Zero-Allocation Enforcement (FR-PO-048 … 053)

- 3.7.1 Citation: KD-10; CLAUDE.md "zero-allocation architecture in
  the game loop".
- 3.7.2 Enumeration rule:
  - The set of "hot paths" is the union of every approved spec's §6
    budget table. No separate hot-path list is maintained.
  - The union is materialized at build time as `tools/hot-path-
    union.json` (Stage 0+1 deliverable; Stage 0 placeholder lists the
    file structure in Appendix D).
- 3.7.3 Allocation budget:
  - Every hot-path entry has allocation budget = 0 bytes per tick.
  - Per-build allocation tracker (Stage 0+1) dumps managed-allocation
    counts per method; the dump is diff'd against the §3.7.2 union
    to identify violators.
- 3.7.4 Enforcement mechanism:
  - CI alloc-tracker step (Stage 0+1) blocks merge on any non-zero
    allocation in a §3.7.2 union method.
  - Editor-mode runs do not enforce (Mono GC behaviour differs from
    IL2CPP); enforcement requires the IL2CPP build per `certification-
    platform.md`.
- 3.7.5 Exemption procedure:
  - Genuine one-shot allocations (e.g., scene-load buffer growth) are
    exempted via an attribute (`[HotPathAllocExempt]`) declared in
    Spec #20 §3 (cite, do not redefine — coordinate with #20 author
    if attribute is not yet declared).
  - Exemptions require lead-developer sign-off and a comment citing
    the rationale.
- 3.7.6 Anti-patterns:
  - "It only allocates once at warmup": still on the hot path → still
    blocks; use the §3.7.5 exemption attribute.
  - Boxing of value types in interface dispatch.
  - LINQ on hot paths (banned per Spec #20 §3 — cite).

### 3.8 Instrumentation & Dashboard Mechanics (FR-PO-054 … 062)

- 3.8.1 Citation: KD-3 (boundary with #16 §8); #16 §8 owns trace
  channels; Spec #18 owns aggregated dashboards.
- 3.8.2 Source of per-tick metrics:
  - All per-tick metrics consumed by Spec #18 dashboards originate in
    #16 §8 trace channels. Spec #18 does not instrument new
    trace points.
  - If a metric Spec #18 needs is not surfaced by #16 §8, the
    resolution is to file a back-prop request against #16 §8 (recorded
    in `spec-error-log.md` as `ERR-018-NNN`), not to add a parallel
    trace pipeline.
- 3.8.3 Dashboard architecture:
  - Dashboards consume the canonical trace dump from #16 §8 (format
    per #16 §5 canonical binary layout — KD-11 binding).
  - Aggregation logic (rolling averages, p99 windows, regression
    bands) lives entirely in Spec #18's dashboard implementation; the
    raw trace pipeline is untouched.
- 3.8.4 Dashboard catalogue (Stage 1 deliverable; Stage 0 declares
  schema in Appendix F):
  - Per-spec per-tick budget dashboard.
  - Per-PR delta dashboard.
  - Milestone-baseline trend dashboard.
  - Allocation-tracker dashboard.
  - Flake/determinism cross-reference dashboard (joins #16 §7 flake
    data with #18 §3.4.4 baseline validator output).
- 3.8.5 Refresh cadence:
  - Per-PR delta: synchronous with CI run.
  - Milestone trend: weekly; nightly at Stage 1.
- 3.8.6 Anti-patterns:
  - Standing up a parallel trace pipeline.
  - Embedding dashboard logic in `src/` gameplay code (dashboards live
    in `tools/` per §4.3).
  - Reading from a trace dump without canonical-layout validation
    (KD-11 binding).

### 3.9 Edge Cases

- 3.9.1 Spec-time perf claims (e.g., #6 Shot Mechanics §4.5's "0.017ms
  estimated"): treated as `[EST]` baseline anchors; first Stage 0+1
  baseline capture promotes the estimate to a measured value tagged
  `[GT]` if within ±20% of estimate, or files an `ERR-018-NNN` review
  finding if not.
- 3.9.2 Editor-only / debug-tool perf: outside KD-10 hot-path union;
  alloc-tracker exempt; functional rules still apply.
- 3.9.3 Multi-platform divergence (Stage 5+): when Stage 5 multiplayer
  activates, budgets per platform pin (KD-9) may diverge; reconciliation
  is a Stage 5 deferred decision (D6 in §7.5), not a Stage 0 concern.
- 3.9.4 First-tick warmup: the first N ticks after scene load are
  exempt from §3.5.2 regression gates (warmup allocations, JIT for
  Mono); N pinned at Stage 0+1.
- 3.9.5 Soak runs: long-horizon profiling (≥ one full match) is owned
  by #19 §3.1 end-to-end / soak layer for *test execution*; Spec #18
  §3.3 governs the perf-metric capture *from* those runs. Both apply,
  no overlap.

### 3.10 Constants Catalogue (governance metadata only)

- This spec declares **no physical constants**. Numeric thresholds it
  publishes (regression % threshold, absolute-threshold guard %, alloc
  budget = 0, headroom multipliers) are governance values tagged
  `[GT]` with rationale recorded inline. Section retained per template
  with one-line justification.
- **KD-6 evidence artifact for governance numbers (parallel to Spec
  #19 §3.10 L5 convention).** Each `[GT]` governance number's evidence
  is the citation line in this spec's body text that introduces the
  number — for example, the +5% per-PR threshold's evidence is
  `section-3.md §3.5.2`, the +10% milestone-baseline guard's evidence
  is `section-3.md §3.5.6`. The approval-checklist auditor (§5.3 /
  Spec #19 §5.3) resolves these citations by confirming the cited
  file path contains the literal number claimed. No separate
  `tools/governance-numbers.md` file is created.
- Per-spec physical budgets cited (not republished) live in each
  spec's §6 / §4.5; the citation list is the §3.1.3 roll-up table.

### 3.11 Version History

---

## SECTION 4 — ARCHITECTURE & INTEGRATION (`section-4.md`)

### 4.1 Benchmark Scene & Harness Layout (shape, not concrete paths)

- Convention: `tools/perf-harness/` houses Stage 0 synthetic harnesses
  (no `src/` yet); `tests/perf/` houses Stage 0+1 production benchmarks
  bound to `src/<spec>/` subsystems.
- Within `tests/perf/<spec>/`: `scenarios/` (manifests bound to #16 §7
  scenario IDs), `baselines/` (per §4.2), `results/` (transient CI
  outputs).
- Cross-spec scenarios: reuse Spec #19 KD-8 cross-spec scenarios at
  `tests/scenarios/cross-spec/`; Spec #18 does not author parallel
  scenarios.

### 4.2 Baseline Storage Layout (KD-11)

- `tests/data/baselines/` root with subfolders per spec.
- Per-baseline file format declared in Appendix A; fields: session
  manifest (per §3.3.2), captured metrics (per-tick ms, allocation
  bytes, cache-miss counters where available), pass/fail vs §3.5.2
  threshold.
- Stage 0 placeholder location:
  `docs/specs/performance-optimization/baselines/`. Migration to
  `tests/data/baselines/` is atomic with first `src/` commit; format
  is identical (no migration script needed).
- Format conforms to #16 §5 canonical binary layout where applicable
  (KD-11 binding).

### 4.3 Profiling & Dashboard API Surface

- `IPerfHarness` consumed by per-spec benchmark runners. Single
  concrete implementation; no IoC container (parallel to Spec #20
  §3.5.5 anti-pattern list and Spec #19 §4.3).
- `BaselineRecord` value type: immutable; serialized per Appendix A.
- `BudgetRollupEntry` value type: read-only view onto a per-spec §6
  declaration; recomputed at build time, never edited by hand.
- Dashboard helpers live in `tools/perf-dashboard/`; never reference
  `src/<spec>/` gameplay assemblies (Spec #20 §4.1 dependency-arrow
  rule).

### 4.4 Interface Contracts (this spec exposes)

- `IPerfHarness` — implemented by `tests/perf/` harness; consumed by
  `ScenarioRunner` (Spec #19 §3.3.3). Producer = Spec #18 §3.3 harness
  authors; consumer = Spec #19 scenario runner. Both sides specified
  → permitted under CLAUDE.md "Interface Design Principle".
- `IBudgetSource` — implemented by each per-spec §6 metadata extractor;
  consumed by `tools/hot-path-union.json` builder (§3.7.2). Both sides
  specified.
- Both live in `tools/perf-harness/` and `tools/` respectively per
  §4.1 / §4.3; no game-state code may reference them.
- **`IDashboardSink` is intentionally NOT declared here.** Per the
  CLAUDE.md "Interface Design Principle" (only declare interfaces when
  both sides are specified — ERR-001 / ERR-004 hazard), the dashboard
  consumer (a web UI, a Grafana plugin, an in-editor panel) is
  unspecified at Stage 0. The interface is deferred to §7.2 Stage 1
  deliverables and is declared once the dashboard front-end is
  concretely specified. Parallel to Spec #19's `IFlakeReporter`
  deferral.

### 4.5 CI Pipeline Topology — Perf Step (shape only; concrete config Stage 1+)

- Pre-commit pipeline: no perf step (too slow for pre-commit).
- PR pipeline: per-spec-changed perf benchmark + alloc-tracker step;
  block on §3.5.2 threshold or §3.7.4 alloc violation.
- Nightly pipeline: full perf benchmark suite + absolute-threshold
  guard (§3.5.6) + milestone-baseline trend update.
- Diagram: trigger → benchmark → gate → exit criteria. Concrete CI
  provider selection deferred to `src/CLAUDE.md` (parallel to Spec
  #19 KD-3); selection criteria recorded in §6.1.
- Composition with #19's functional pipeline: perf step runs after
  functional step; functional failure short-circuits perf step
  (no point measuring a broken build).

### 4.6 Pointer to `src/CLAUDE.md`

- Concrete paths, profiler invocation commands, allocation-tracker
  invocation, and CI perf-step configuration land in `src/CLAUDE.md`
  when coding begins. Spec #18 declares the *shape*; `src/CLAUDE.md`
  declares the *paths*. Parallel to Spec #19 §4.6.

### 4.7 Version History

---

## SECTION 5 — TEST PLAN (CONFORMANCE VERIFICATION OF THIS SPEC ITSELF) (`section-5.md`)

> **Slot reconciliation:** The template's §5 ("Test Plan") is reflexive
> for a meta-spec: this section verifies Spec #18 against itself.
> Per-spec §6 conformance verification (which Spec #18 mandates for
> *other* specs) is mechanics-defined in §3.1 above; auditor mechanics
> live here in §5.3. Parallel slot reconciliation to Spec #19 §5.

### 5.1 Conformance Verification Model

- Spec #18 publishes its FRs (§2.2). This section maps every FR to its
  verification mechanism.
- Stage 0: manual review (no code yet, parallel to Spec #19 §5.1 /
  Spec #20 §5.1).
- Stage 0+1: tooling activates per FR's "Activation stage" column in
  §2.2.

### 5.2 Stage-Gated Activation Table (KD-5)

- Per-FR table: `FR-PO-### | Stage 0 status | Activation stage | Activation criterion`.
- Most FRs read "Stage 0+1" with criterion "first `src/` code
  committed AND `certification-platform.md` Stage 0 row pinned".
- A few read "Stage 0" with criterion "applies to spec drafts now"
  (notably the per-spec §6 schema FRs FR-PO-001 … 008, the loop-tag
  mandate FR-PO-009 … 015, and the degradation-tier-policy FRs
  FR-PO-041 … 047 which constrain *spec writing* not *code*).

### 5.3 Per-Spec §6 Schema-Conformance Auditor

- Mechanics for FR-PO-001 … 008.
- Manual at Stage 0: reviewer walks every approved spec's §6 against
  the Appendix B template; gaps logged as `ERR-018-NNN` per §3.1.2.
- Automated at Stage 0+1: `tools/budget-auditor.py` (or equivalent;
  final language pin parallel to Python tooling rule in CLAUDE.md
  "When Writing Code") parses §6 sections, validates schema, emits
  rollup table.
- Approved specs (#1–#8, #17) survey-only at Stage 0; gaps logged as
  `ERR-018-NNN` rows in `spec-error-log.md`; remediation happens at
  next natural revision of each spec (KD-2 grandfather rule, parallel
  to Spec #19 §3.5.4 acknowledged dilution policy).
- New specs (#9, #10–#16, #18 itself, #19, #20): schema-conforming on
  first draft or §9 approval is blocked.

### 5.4 Baseline-Reproducibility Auditor (KD-11)

- Mechanics for FR-PO-063 … 068.
- Stage 0: not applicable (no `src/` to baseline).
- Stage 0+1: for every baseline file in `tests/data/baselines/`, the
  auditor re-runs the recorded session manifest (seed, fingerprint,
  platform pin, scenario ID) and confirms the recaptured metric
  matches within §3.4.3 confidence interval.
- Failure: baseline marked stale; PR that introduced it blocked from
  merge.

### 5.5 Loop-Tag Conformance Auditor (KD-8)

- Mechanics for FR-PO-009 … 015.
- Manual at Stage 0: §5.3 auditor's pass simultaneously walks every
  §6 budget number for the `[LOOP-TACTICAL-10HZ]` / `[LOOP-PHYSICS-
  60HZ]` tag.
- Automated at Stage 0+1: `tools/budget-auditor.py` regex pass
  rejects untagged numbers.

### 5.6 FR-to-Verification Traceability

- Single table indexed by FR-PO-###; columns: `Verification Mechanism |
  Tooling | Activation Stage | Output Artifact`.
- Stage 0 most rows resolve to "manual review against §3 mechanics" —
  acknowledged degenerate (parallel to Spec #19 §5.6 / Spec #20 §5.5).

### 5.7 Boundary-Verification (KD-3 / KD-4)

- **#16 boundary check:** any change to #16 §7 (scenarios) or #16 §8
  (trace channels) that affects scenario IDs, manifest format, or
  trace-channel schema triggers a Spec #18 §3.3 / §3.8 review
  (recorded in §1.4 dependency list).
- **#19 boundary check:** any change to #19 §6 (CI orchestration)
  that affects gate-composition or gate-ownership triggers a Spec
  #18 §3.5.3 review.
- Boundary breaches discovered in CI runs (e.g., perf gate firing on
  a functional flake) are routed to §6.4 defect-triage as a
  "Boundary defect" class.

### 5.8 Version History

---

## SECTION 6 — CI ORCHESTRATION POLICY & TRIAGE (`section-6.md`)

> **Slot reconciliation:** Replaces the template's "Performance
> Analysis" slot. A meta-spec has no algorithm to analyse; it codifies
> the CI orchestration policy and defect-lifecycle rules. Justification
> in §1.3 KD-4 (boundary with Spec #19) and KD-5 (Stage gating).
> Parallel slot reconciliation to Spec #19 §6.

### 6.1 Tooling Standards (Stage-gated per KD-5)

- Stage 0: no tooling activates. This subsection enumerates *selection
  criteria*, not chosen tools.
- Stage 0+1 tool slate (selection finalized at transition):
  - Sampling profiler: Unity Profiler + Tracy or Superluminal
    (selection criteria: deterministic re-play support, headless
    batch-mode capture for CI, per-frame breakdown emission).
  - Allocation tracker: Unity Memory Profiler or equivalent
    IL2CPP-compatible tool (selection criteria: per-method alloc
    counts, integratable into CI step §3.7.4).
  - Benchmark framework: BenchmarkDotNet or Unity Performance Testing
    Extension (selection criteria: statistical significance reporting
    per §3.4.3, scenario-manifest binding per #16 §7).
  - CI provider: deferred to `src/CLAUDE.md` (parallel to Spec #19
    §6.1). Selection criteria: must support the three pipeline shapes
    in §4.5, must support gate composition with #16 §7 determinism gate
    and #19 §6.2 functional gate, must expose per-step pass/fail at
    gate-composition granularity (§3.5.3).

### 6.2 Stage-0 Local-Only Runbook

- Until CI activates, the same gate composition runs locally:
  - Pre-commit hook script (Stage 0 deliverable; Appendix E):
    `tools/run-perf-local.sh` invokes the manual §5.3 schema-conformance
    auditor and §5.5 loop-tag auditor against `docs/specs/` only.
  - Stage 0 manual benchmarking is against synthetic harnesses in
    `tools/perf-harness/` (no `src/` yet); produces "anchor" baselines
    that exercise tooling but do not yet represent gameplay code.
- Output of local runbook → reviewer pastes into PR description.

### 6.3 CI Perf-Gate Topology (Stage 0+1, boundary with #19)

- Spec #18 declares performance regression gates (§3.5.2 threshold,
  §3.5.6 absolute guard, §3.7 alloc gate).
- Spec #19 §6.2 declares functional regression gates and orchestrates
  composition.
- Gate composition rule (KD-4 binding, also recorded in §3.5.3):
  - Functional gate failure (Spec #19) → block merge.
  - Determinism gate failure (#16 §7) → block merge.
  - Performance gate failure (Spec #18) → block merge.
  - Allocation gate failure (Spec #18) → block merge.
  - No gate is "soft"; perf-gate exception requires lead-developer
    sign-off (§3.5.5).

### 6.4 Defect Lifecycle & Triage (FR-PO-075 … 080)

- 6.4.1 Defect classes:
  - **Budget overrun** (subsystem exceeds §3.5.2 threshold) → fix
    code or re-allocate budget via §3.1.5.
  - **Allocation regression** (non-zero alloc on hot path) → fix
    code; alloc on Tier A path is Critical.
  - **Baseline non-reproducibility** (KD-11 violation) → re-capture or
    investigate environment drift.
  - **Boundary defect** (perf gate firing on functional flake, or
    vice versa) → route to §5.7 boundary review.
  - **Inconclusive optimization** (§3.4.3 significance failure) →
    backlog with date target.
- 6.4.2 Triage cadence:
  - PR-blocking failures: investigated within 24 hours (parallel to
    Spec #19 §6.4.2).
  - Inconclusive optimizations: reviewed weekly.
  - Boundary defects: reviewed at next spec-revision cycle of the
    boundary spec (#16 or #19).
- 6.4.3 Severity scale:
  - **Critical** — Tier A allocation, blocks Stage milestone.
  - **High** — >+10% milestone-baseline drift (§3.5.6 trip), blocks
    current sprint.
  - **Medium** — per-PR threshold trip on Tier B path, backlogged
    with date target.
  - **Low** — inconclusive optimization, backlog with no date.
- 6.4.4 Defect-to-FR traceability:
  - Every defect cites the FR it violated (Spec #18 FR or owning-spec
    §6 budget number). Defects without FR citation are themselves a
    procedural violation (parallel to Spec #19 §6.4.4).

### 6.5 Reporting Cadence

- Stage 0: monthly survey of §5.3 schema-conformance auditor output +
  §5.5 loop-tag auditor output appended to
  `docs/tracking/PROGRESS.md`.
- Stage 0+1: per-PR delta + weekly dashboard (§3.8.5).
- Stage 1: per-PR delta + nightly dashboard + monthly retrospective.

### 6.6 Functional-Related Cross-Listing

- FR-PO-031 … 040 (regression gates) cite Spec #19 §6.2 by reference
  per KD-4. No functional gate rules republished here.

### 6.7 Version History

---

## SECTION 7 — FUTURE EXTENSIONS (`section-7.md`)

### 7.1 Stage 0+1 Transition Deliverables

- Profiler pin (§3.3.5).
- Allocation tracker pin (§6.1).
- Benchmark framework pin (§6.1).
- `tools/budget-auditor.py` (§5.3) initial implementation.
- `tools/hot-path-union.json` builder (§3.7.2).
- Pre-commit hook script (§6.2).
- First `src/CLAUDE.md` perf section.
- `certification-platform.md` Stage 0 row pinned (precondition, not
  produced by #18 itself; tracked in CLAUDE.md OPEN ISSUES).
- §3.5.2 +5% threshold re-evaluated against actual baseline variance
  (parallel to Spec #19 §3.1.2 pyramid-ratio re-evaluation).

### 7.2 Stage 1 Deliverables

- Dashboard front-end + `IDashboardSink` interface declaration
  (deferred from §4.4 per CLAUDE.md "Interface Design Principle";
  declared once dashboard consumer is concretely specified).
- Tier C degradation table populated (§3.6.4).
- Milestone-baseline trend dashboard (§3.8.4).
- Appendix D approved-spec §6 survey populated (deferred from §9.2
  per §3.1.2 grandfather rule; parallel to Spec #19 Appendix D
  down-scope per its M3 finding).
- Baseline-reproducibility auditor (§5.4) automated.

### 7.3 Stage 5+ Extensions

- Per-platform budget divergence under Fixed64 (§3.9.3; Spec #9
  dependency).
- Multiplayer perf-cert layer (Stage 5 multiplayer scope per CLAUDE.md
  "Fixed64 stage scope decision").
- Cross-platform parity dashboard.

### 7.4 Permanent Exclusions

- Tier A dynamic degradation paths — never permitted (§3.6.2; KD-7).
- "Threshold relaxation by per-PR creep" — caught by §3.5.6 absolute
  guard; never silently accepted.
- Parallel trace pipeline outside #16 §8 — never permitted (§3.8.6
  anti-pattern).
- Wall-clock-seeded profiling runs — never accepted into baseline
  corpus (§3.3.6; KD-6).
- Per-spec §6 override by #18 — KD-2 permanent rule.

### 7.5 Deferred Decisions Tracker

- D1 — Sampling profiler pin (Unity Profiler + Tracy vs Superluminal) — Stage 0+1.
- D2 — Allocation tracker pin — Stage 0+1.
- D3 — Benchmark framework pin (BenchmarkDotNet vs Unity Performance Testing Extension) — Stage 0+1.
- D4 — CI provider — `src/CLAUDE.md` (KD-4).
- D5 — Stage 1 adaptive degradation posture (any Tier B / Tier C dynamic fallbacks?) — Stage 1+.
- D6 — Per-platform budget reconciliation rule under Fixed64 — Stage 5+.
- D7 — Engine-overhead headroom number (§3.1.4) — Stage 0+1 once Unity LTS + backend pinned.
- D8 — §3.4.3 statistical-significance N pin — Stage 0+1.
- D9 — §3.5.2 +5% threshold pin (may tighten/loosen after first 30 days of CI data) — Stage 1.

### 7.6 Version History

---

## SECTION 8 — REFERENCES & CITATION AUDIT (`section-8.md`)

### 8.1 Source Register

- Root `CLAUDE.md` (tick rates; zero-allocation mandate; deterministic
  replay; Stage 0 host platform pin; "Interface Design Principle";
  Fixed64 stage scope decision).
- Spec #16 (Deterministic Simulation) — §1.3 tier classification, §4
  `EnvironmentFingerprint`, §5 canonical save format, §7 regression
  scenarios, §8 trace channels.
- Spec #19 (Testing Strategy) — §3.1 taxonomy, §3.7 flake handling,
  §6 CI orchestration.
- Spec #20 (Code Standards) — §3 zero-allocation rules,
  `[HotPathAllocExempt]` attribute.
- Per-spec §6 (or §4.5 in #6's case): #1 Ball Physics §6, #2 Agent
  Movement §6, #3 Collision System §6, #4 First Touch §6, #5 Pass
  Mechanics §6, #6 Shot Mechanics §4.5, #7 Perception §6, #8 Decision
  Tree §6, #17 Event System §6.
- `docs/tracking/certification-platform.md` (placeholder at draft
  time; see KD-9 status caveat).
- `docs/planning/development-best-practices.md`.
- `docs/planning/master-development-plan.md`.
- RFC 2119 (MUST/SHOULD/MAY).
- External: profiler/tracker/benchmark-framework URLs + retrieval
  dates (Stage 0+1 deliverable; placeholders at draft).

### 8.2 Verification Notes

- Every CLAUDE.md citation in §3 verified against current CLAUDE.md
  text on this spec's drafting date.
- Every Spec #16 / #19 / #20 citation verified against the current
  approved-or-draft text and section number per `SPEC_INDEX.md`.
- Every per-spec §6 citation verified for existence (subsection
  number may need re-grep at draft time per §1.4 status caveats).

### 8.3 Cross-Spec Citation Audit

- Spec #18 is **cited by** Spec #19 §6 (CI orchestration boundary)
  and (downstream) every per-spec §6 once the per-spec §6 schema is
  ratified.
- Spec #18 cites #16 (substantive: tiers, regression scenarios,
  trace channels, save format), #19 (boundary: CI orchestration,
  flake handling), #20 (boundary: zero-alloc rules).
- No `[CROSS]` constants are imported (Spec #18 declares none).
- Tier vocabulary cited from #16 §1.3 by reference only (KD-1).
- Per-spec budget numbers cited by reference only (KD-2); never
  republished.

### 8.4 Constant Provenance Summary

- Spec #18 declares no physical constants.
- Governance numerics (+5% regression threshold, +10% milestone
  guard, alloc budget = 0, sampling cadence, statistical-significance
  N) are `[GT]` per §3.10; rationale recorded inline at point of
  declaration.

### 8.5 Version History

---

## SECTION 9 — APPROVAL CHECKLIST (`section-9-approval-checklist.md`)

> Spec #18 binds to Spec #19's KD-6 programmatic-verification mandate.
> Every approval-checklist row in this checklist MUST cite either a
> file path or a programmatic check name (parallel to Spec #19 §9
> self-application).

### 9.1 Content Checklist

- All required sections present (incl. template-slot reconciliation in
  §5 / §6).
- All FR-PO-### present in §2.2 with conformance level and activation
  stage.
- KD-1 … KD-11 each codified in at least one §3 / §5 / §6 subsection.
- Boundary statements with #16 §7 / §8 (KD-3), #19 §6 (KD-4), and
  every per-spec §6 (KD-2) explicit.

### 9.2 Quality Checklist

- Cite-not-redefine rule audited (no #16 / #19 / #20 / per-spec §6
  restatements).
- Every FR row resolves to a §5.x verification mechanism.
- Every approval-checklist row in *this* checklist cites either a file
  path or a check name (KD-6 self-application via Spec #19 §3.5.1).
- All cross-references (XC-/FM-/EC-/ERR-) resolve.
- Per-spec §6 schema (Appendix B) present and complete.
- All `TBD-NORMATIVE`-tagged citations of #16 (KD-3) and #19 (KD-4)
  enumerated; outstanding tags listed for the reviewer.
- **Appendix D survey is NOT a #18-approval gate.** The survey of
  #1–#8 / #17 §6 sections is a Stage 0+1 deliverable (§7.2); for
  #18's own approval the requirement is only that Appendix D *exists
  with the schema and an empty / partial table*. Completing the survey
  rows is deferred so #18's approval is not converted into a 9-spec
  audit task. Parallel to Spec #19 Appendix D scope decision.

### 9.3 Review Checklist

- Open issues logged in `CLAUDE.md` "OPEN ISSUES" if any.
- Lead-developer sign-off captured.
- `spec-error-log.md` updated with any cross-spec drift discovered
  during drafting (ERR-018-NNN rows for any per-spec §6 schema gaps
  found).
- `SPEC_INDEX.md` status updated atomically with sign-off.
- Spec #19's `TBD-NORMATIVE` tags pointing at "Spec #18 §4 / §7"
  preconditions (per Spec #19 KD-3 status caveat) audited: those
  preconditions are this file (§4 + §7 headers present) — confirm
  #19 reviewer is aware that #18 advancement past `IN REVIEW` is
  symmetric with #18 advancing toward `APPROVED`.

### 9.4 Decision

- Status block (`IN REVIEW` / `APPROVED` / `SUSPENDED` / `DEFERRED`).
- Approval evidence: file paths to programmatically-verifiable sources
  (KD-6 self-application — every row of this checklist must comply).
- **Evidence-artifact convention for `[GT]` governance numbers
  (parallel to Spec #19 §9.4 L5 convention).** Per §3.10, each
  governance number's evidence is the section-file citation that
  publishes the number (e.g., `section-3.md §3.5.2` for the +5%
  threshold). Checklist rows pointing at `[GT]` numbers MUST cite the
  section-file path verbatim; the §5.3 auditor (or Spec #19 §5.3
  checklist auditor under KD-6) confirms the literal number is
  present at that path.

---

## APPENDICES (`appendices.md`)

- **Appendix A — Baseline Record File Format.**
  Paste-ready binary-layout / JSON-schema declaration; field names,
  types, required / optional, version-field semantics; binding to
  #16 §5 canonical layout (KD-11). Includes profiling-session manifest
  fields per §3.3.2.

- **Appendix B — Per-Spec §6 Schema Template.**
  Paste-ready Markdown template every per-spec §6 must conform to.
  Sections: total per-tick budget table, per-loop breakdown (with
  loop tags per KD-8), worst-case input parameters, headroom
  multiplier, alloc budget declaration (always 0 on hot paths per
  KD-10), citation row for any cross-spec budget consumed. This is
  the artifact KD-2 mandates for new specs and surveys for approved
  specs.

- **Appendix C — Budget Roll-up Table.**
  Read-only roll-up of every per-spec §6 budget into a single
  cross-spec table per platform target. Columns: spec ID, declared
  budget, loop tag, alloc budget, citation (link to spec §6), last
  verified date. Stage 0 deliverable: schema + first manual pass on
  #1–#8 / #17. Stage 0+1: auto-generated by `tools/budget-auditor.py`.

- **Appendix D — Approved-Spec §6 Survey.**
  Table of #1 … #8 / #17 §6 sections rated against Appendix B schema.
  Columns: spec ID, schema-conforming Y/N, missing fields, remediation
  ERR-018-NNN. **Scope at #18 approval:** Appendix D ships with the
  schema and the table headers populated; row contents are a Stage
  0+1 deliverable (§7.2). The survey itself is *not* a #18 approval
  gate; KD-2 grandfather dilution remains visible via the empty rows
  even before the survey is filled in. Stage 1 trigger for actual
  per-spec revisions remains unchanged. Parallel to Spec #19 Appendix
  D scope.

- **Appendix E — Stage-0 Local Runbook.**
  Concrete shell-script outline for `tools/run-perf-local.sh`:
  pre-commit checks against `docs/specs/` only (no `src/` yet);
  invocation of §5.3 schema-conformance auditor and §5.5 loop-tag
  auditor; synthetic-harness invocation against `tools/perf-harness/`
  anchor scenarios.

- **Appendix F — Dashboard Schema Catalogue.**
  Paste-ready schema for each Stage 1 dashboard enumerated in §3.8.4.
  Per-dashboard: data source (which #16 §8 trace channel), aggregation
  rule, refresh cadence, alert threshold (where applicable).

- **Appendix G — Glossary.**
  Spec #18-specific terms only (hot path, baseline, regression
  threshold, milestone-baseline drift, optimization ladder rung,
  headroom multiplier). Determinism / tier / scenario terms cited
  from #16; pyramid / coverage / flake terms cited from #19.

---

## VERSION HISTORY

| Version | Date         | Author      | Notes                                                                                                                                                                                         |
|---------|--------------|-------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| 1.0     | May 13, 2026 | Claude Code | Initial detailed outline drafted from `outline.md` adversarial review. Addresses all 13 findings (6 H / 5 M / 2 L). Resolution map below. Ready for section-file authoring; unblocks Spec #19 KD-3 precondition. |

---

## ADVERSARIAL-REVIEW FINDINGS RESOLUTION MAP

For traceability — every finding in `outline.md` adversarial review
section is resolved by a specific subsection above.

| Finding | Severity | Resolved by |
|---------|----------|-------------|
| 1 — Missing metadata header | H | Top of this file |
| 2 — Section plan deviates from template (§7 used for validation, §8 for reporting, no references slot) | H | Re-mapped: §5 reflexive test plan, §6 slot reconciliation for CI orchestration / triage, §7 future extensions, §8 references. Slot reconciliations stated in §5 and §6 headers (parallel to Spec #19) |
| 3 — Authority over per-subsystem budgets unresolved | H | KD-2 explicit: "ratify, read-only roll-up"; §3.1; §3.1.5 re-allocation procedure |
| 4 — Fallback/degradation strategies conflict with deterministic replay | H | KD-7 explicit: Tier A forbidden, Tier B declared-at-spec-time only, Tier C only acceptable runtime surface; §3.6; §7.4 permanent exclusion |
| 5 — Boundary with Testing Strategy #19 §5 unstated | H | KD-4 explicit: #19 owns functional gates, #18 owns perf gates; §3.5.3 gate-composition table; §6.3 |
| 6 — Boundary with Deterministic Simulation #16 §8 unstated | H | KD-3 explicit: #16 §8 owns trace channels, #18 owns aggregated dashboards; §3.8; §5.7 boundary check |
| 7 — CI gates infeasible at Stage 0 | M | KD-5 explicit: Stage-gated activation; §5.2 activation table; §6.2 Stage-0 local-only runbook; Appendix E |
| 8 — Baseline-capture ownership undefined | M | KD-11; §3.4.4 baseline validator; §4.2 storage layout; §5.4 reproducibility auditor; Appendix A format |
| 9 — Platform target list missing | M | KD-9; §1.4 platform-pin dependency; §3.2.5; binds to `certification-platform.md` Stage 0 row |
| 10 — No reference to fixed timestep / tick-rate budget split | M | KD-8; §3.2 dedicated subsection on loop separation; §5.5 loop-tag conformance auditor |
| 11 — Profiling methodology must use deterministic seed and trace | M | KD-6; §3.3.2 session contract; §3.3.6 anti-patterns (wall-clock-seeded forbidden) |
| 12 — Reporting cadence unstated | L | §6.5 explicit cadence: Stage 0 monthly survey, Stage 0+1 per-PR + weekly, Stage 1 per-PR + nightly + monthly retrospective |
| 13 — No fast-path / hot-path enumeration policy | L | KD-10; §3.7 dedicated subsection: hot paths = union of per-spec §6 tables, no separate authoritative list, alloc budget always 0 |
