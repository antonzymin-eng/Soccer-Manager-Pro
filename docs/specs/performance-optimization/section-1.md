# Performance Optimization Strategy Specification #18 — Section 1: Purpose & Scope

**Created:** May 13, 2026
**Last Updated:** May 14, 2026 (v0.3 PASS-2 adversarial-review fix pass)
**Purpose:** Establishes what Spec #18 governs, what it excludes, the
eleven cross-cutting key decisions (KD-1 … KD-11) that bind the rest of
the document, the upstream / downstream contracts, and the version
history.

---

## 1.1 What This Specification Covers

Spec #18 is the project's authoritative governance document for **how
performance budgets are tracked, measured, regression-gated, and
remediated** across the System XI codebase. It does not own
the per-subsystem budget numbers — those are owned by each spec's §6
(or §4.5 in Shot Mechanics #6's case) — and it does not own the
determinism regression scenarios or the canonical record format
authoritative copies of which live in Spec #16. Its scope is:

- **Budget roll-up authority.** Spec #18 ratifies the per-spec §6
  budget declarations into a single cross-spec roll-up table, publishes
  the schema every §6 must conform to, and runs the re-allocation
  procedure when total exceeds platform headroom. It does not override
  per-spec §6 numbers (KD-2). See §3.1.
- **Loop-separation tagging (10 Hz tactical / 60 Hz physics).** Every
  budget number in every per-spec §6 carries a loop tag; cross-loop
  totals are forbidden (KD-8). See §3.2.
- **Determinism-bound profiling methodology.** Every profiling session
  runs an #16 §5 regression scenario verbatim, with recorded seed,
  `EnvironmentFingerprint`, and platform pin (KD-6). See §3.3.
- **Optimization ladder.** Measure → Attribute → Fix → Verify → Lock,
  with mandatory rungs and statistical-significance gating. See §3.4.
- **Performance regression gates, with #16 / #19 boundary.** Spec #18
  owns the performance and allocation gates; Spec #16 §5 owns the
  determinism gate; Spec #19 §6 owns the functional gate. Composition
  rule pinned per gate (KD-3 / KD-4). See §3.5, §6.3.
- **Degradation policy.** Tier A outputs MUST NOT vary under
  performance pressure; Tier B may vary within declared tolerance
  *declared at spec time, not at runtime*; Tier C (render LOD, debug
  overlay, telemetry sampling, dashboard refresh) is the only
  acceptable runtime degradation surface (KD-7). See §3.6.
- **Hot-path enumeration and zero-allocation enforcement.** The set of
  hot paths is the union of every per-spec §6 budget table; the
  allocation budget on every hot-path entry is zero per CLAUDE.md
  "zero-allocation architecture in the game loop" (KD-10). See §3.7.
- **Trace pipeline ownership (KD-3 inverted).** Spec #18 owns the
  trace pipeline architecture — channel registry, verbosity tiers,
  sampling rules, channel-to-sink routing, instrumentation API, and
  dashboard aggregation. Every trace record conforms to the canonical
  record format at #16 §3.2.4.1; every trace point emitted inside the
  canonical tick pipeline (#16 §3.1.2) requires #16-owner sign-off
  (emission-veto authority). See §3.8.
- **Baseline reproducibility and storage.** Every baseline is
  reproducible from recorded git SHA, seed, `EnvironmentFingerprint`,
  and platform pin; baselines live in a version-controlled location
  with the format declared in Appendix A (KD-11). See §3.4, §4.2.

**Applicability.**

- **Primary:** every `src/<spec>/` subsystem with a §6 budget once
  coding begins (Stage 0+1 transition).
- **Secondary (governance-only):** every spec's §6 section under
  `docs/specs/`. Spec #18 publishes the roll-up rule, the budget-
  tagging schema, and the loop-tag mandate those §6 sections must
  conform to; it does not rewrite them (KD-2).

For rule mechanics see §3; for reflexive conformance verification of
this spec itself see §5; for CI orchestration and defect triage see
§6.

## 1.2 What Is Out of Scope

Each line cites the owning document.

- **Determinism regression scenarios and tier classification** → Spec
  #16 §5 / §1.3.1 (KD-3).
- **Canonical golden-trace record format** → Spec #16 §3.2.4.1 (KD-3,
  KD-11).
- **Determinism-of-emission constraints / veto over tick-pipeline
  trace points** → Spec #16 §3.1.2 (KD-3). The trace pipeline
  *architecture itself* is OWNED by Spec #18 per inverted KD-3 — not
  out of scope.
- **Functional / behavioural regression gates** → Spec #19 §3 / §6
  (KD-4).
- **Test pyramid, coverage targets, flake handling** → Spec #19 (KD-4).
- **Numeric correctness of physics / AI formulas** → owning specs
  (#1–#8) §3.
- **Fixed64 numeric perf budgets** → Spec #9 §6 (Stage 5+ scope per
  CLAUDE.md "Fixed64 stage scope decision").
- **C# code style and banned-allocation patterns** → Spec #20 §3.
- **CI server choice, build commands, profiler invocation commands,
  IDE configuration** → `src/CLAUDE.md` (deferred until coding begins).
- **Asset-pipeline / render-thread budgets** → Stage 1+ specs (not
  gameplay loops).
- **Dynamic runtime degradation for Tier A / Tier B outputs** →
  permanently out of scope per KD-7 (§7.4).

## 1.3 Key Design Decisions

Eleven cross-cutting decisions referenced throughout this spec. The
**authoritative definition** of each KD is in this section file (here);
the codification map below names the §3 / §5 / §6 subsection that
publishes the rule mechanics. `outline-detailed.md` is a drafting
artifact and is **no longer authoritative** once this section file is
APPROVED — if the two diverge, this file wins. Sequencing-constraint
text and status-caveat text live in §1.4 (dependency contracts), not
in §1.3, so the KD definitions here remain stable when upstream spec
status changes.

- **KD-1 — Cite-not-redefine.** Spec #18 never restates a CLAUDE.md
  invariant or a rule already published by another approved spec. It
  cites and binds. Tick-rate definitions, coordinate system, fatigue
  convention, and the zero-allocation mandate are cited from CLAUDE.md;
  per-spec budget numbers are cited from each spec's §6 (or §4.5).
- **KD-2 — Per-spec §6 sections remain authoritative for their own
  budget; Spec #18 RATIFIES, does not OVERRIDE.** Each approved spec's
  §6 (or §4.5) declares its own per-frame / per-tick budget; that
  declaration is the authoritative source for that subsystem. Spec #18
  publishes (a) the roll-up table aggregating every per-spec budget,
  (b) the budget-allocation rule any new spec must follow when claiming
  a budget slice, (c) the regression-gate policy that enforces those
  budgets in CI, and (d) the re-allocation procedure when total exceeds
  platform headroom. Re-allocation is handled via §3.1.5, never via
  unilateral #18 override.
- **KD-3 — Boundary with Deterministic Simulation #16 (inverted in
  outline v1.1, May 13, 2026).** #16 retains authority over (a) the
  determinism regression scenarios at #16 §5, (b) the canonical
  golden-trace record format at #16 §3.2.4.1, and (c) the
  determinism-of-emission constraints / veto authority over trace
  points inside the canonical tick pipeline at #16 §3.1. Spec #18 owns
  (a) the trace pipeline architecture (channel registry, verbosity
  tiers, sampling rules, channel-to-sink routing), (b) the
  instrumentation API surface consumed by `src/<spec>/`, (c) dashboard
  aggregation, and (d) the profiling methodology that consumes #16 §5
  scenarios verbatim. Binding mechanic: every record emitted via a
  #18-owned channel conforms to #16 §3.2.4.1; FR-PO-058a (§3.8.3)
  enforces determinism-of-emission. Status caveats and sequencing
  constraints with #16 live in §1.4.
- **KD-4 — Boundary with Testing Strategy #19.** Spec #19 §6 owns the
  CI orchestration policy and functional regression gates; Spec #18
  §3.5 owns the performance regression gates and §3.7 owns the
  allocation gate. Both feed the single CI orchestrator declared in
  #19 §6.2. A run producing "all tests pass but perf regressed N%" is
  a Spec #18 §3.5 block; a run where a perf benchmark times out
  because a functional assert threw is a Spec #19 §3.7 (flake)
  block. Status caveats with #19 live in §1.4.
- **KD-5 — Stage-gated activation.** Sections that presume an
  implemented codebase (CI perf gates, dashboards, automated baseline
  capture) are written as contracts that activate at the Stage 0 →
  Stage 1 transition. They are first-class normative content but are
  not enforceable during the spec-writing phase. Stage 0 has manual
  offline benchmarking against synthetic harnesses only (§6.2). Per-FR
  activation status is tracked in §5.2.
- **KD-6 — Determinism-aware profiling.** All profiling sessions MUST
  run under #16 §5 regression scenarios with explicit recorded seeds.
  Wall-clock or random-seed profiling runs are forbidden — they are
  not comparable across revisions and cannot bisect a regression. Seed
  selection at session start is permitted; the selected seed MUST be
  logged with the captured baseline. This is the performance-side
  equivalent of #19 KD-7 ("Determinism-aware fuzz testing").
- **KD-7 — Degradation paths restricted to Tier C only (deterministic-
  replay safety).** Per CLAUDE.md "Deterministic replay is a hard
  requirement" and #16 §1.3 tier classification: Tier A (authoritative)
  outputs MUST NOT vary under performance pressure; Tier B
  (bounded-authoritative) outputs MAY vary within their declared
  tolerance but only via paths declared at spec time, not adopted at
  runtime; Tier C (non-authoritative) — render LOD, debug overlay
  fidelity, telemetry sampling rate, dashboard refresh frequency — is
  the only acceptable runtime degradation surface. Stage 0 declares NO
  dynamic degradation paths; budget enforcement is via measurement +
  manual remediation.
- **KD-8 — Loop separation (10 Hz tactical vs 60 Hz physics).** Per
  CLAUDE.md "Heartbeat Tick Rate", every per-spec budget number MUST
  carry a loop tag: `[LOOP-TACTICAL-10HZ]` or `[LOOP-PHYSICS-60HZ]`.
  Untagged numbers are a §5.3 / §5.5 conformance failure. Cross-loop
  subsystems declare separate budgets per loop.
- **KD-9 — Reference platform pin.** Performance budgets are only
  meaningful against a pinned reference platform. Spec #18 binds to
  `docs/tracking/certification-platform.md` Stage 0 row for OS, Unity
  LTS revision, scripting backend (Mono / IL2CPP), compiler-flag set,
  worker thread count, and SIMD feature level. Status caveat in §1.4.
- **KD-10 — Hot-path enumeration policy.** The set of hot paths is the
  union of every per-spec §6 (or §4.5) budget table. Spec #18 does not
  maintain a separate authoritative hot-path list. Per CLAUDE.md "When
  Writing Code: zero-allocation architecture in the game loop", the
  allocation budget on every hot-path entry is zero. Spec #18 §3.7
  publishes the *roll-up rule* (how to enumerate the union) and the
  *enforcement mechanism* (per-build alloc-tracking diff against the
  union), not individual entries.
- **KD-11 — Baseline reproducibility and storage.** Every baseline is
  reproducible from (a) the recorded git SHA, (b) the recorded seed
  (KD-6), (c) the recorded `EnvironmentFingerprint` (per #16 §4.8), and
  (d) the pinned platform (KD-9). Baselines live in a version-
  controlled location (`tests/data/baselines/` once `src/` exists;
  Stage 0 placeholder `docs/specs/performance-optimization/baselines/`)
  with the format declared in Appendix A. Capture cadence: per-PR
  delta at Stage 0+1, full re-baseline at each Stage milestone.
  Record-format binding: baseline records and golden-trace records
  both conform to #16 §3.2.4.1 (the KD-3 inversion preserves #16's
  authority over record format).

**Codification map.**

| KD | Topic | Codified in |
|----|-------|-------------|
| KD-1 | Cite-not-redefine | All sections |
| KD-2 | Per-spec §6 ratify, not override | §3.1, §3.1.5 |
| KD-3 | Boundary with #16 (inverted: #18 owns trace pipeline; #16 owns record format §3.2.4.1, regression scenarios §5, emission-veto over #16 §3.1.2) | §3.3, §3.8.3, §3.8.4, §5.7.1 |
| KD-4 | Boundary with #19 §6 | §3.5, §6.3 |
| KD-5 | Stage-gated activation | §5.2, §7 |
| KD-6 | Determinism-aware profiling | §3.3, §3.3.4 |
| KD-7 | Degradation paths restricted to Tier C | §3.6, §7.4 |
| KD-8 | Loop separation (10 Hz / 60 Hz) | §3.2 |
| KD-9 | Reference platform pin | §1.4, §3.2.5 |
| KD-10 | Hot-path enumeration policy | §3.7 |
| KD-11 | Baseline reproducibility & storage | §3.4, §4.2, Appendix A |

## 1.4 Dependencies and Integration Contracts

**Upstream (substantive).**

- Root `CLAUDE.md` — tick rates (10 Hz tactical / 60 Hz physics),
  zero-allocation mandate in the game loop, deterministic-replay
  hard requirement, "Interface Design Principle", Fixed64 stage-scope
  decision, Stage 0 host-platform-pin convention.
- Spec #16 (Deterministic Simulation) — §1.3.1 determinism tiers,
  §3.1 canonical tick pipeline (emission-veto surface), §3.2.4.1
  canonical record format, §4 `EnvironmentFingerprint`, §5 regression
  scenarios / test catalogue. **Status: `IN PROGRESS`.** All citations
  tagged per KD-3. Section-number citations were
  grep-verified against `deterministic-sim/section-*.md` on May 13,
  2026 (outline v1.1 correction — v1.0 had cited §7 for regression
  scenarios, §5 for record format, and §8 for "trace channels"; only
  §5 / §3.2.4.1 actually exist; trace channels are now owned by #18
  per inverted KD-3). Section authors MUST re-grep at draft time —
  #16 has been through three adversarial passes and subsection
  numbering may shift again.

**Upstream (consulted).**

- Spec #19 (Testing Strategy & Framework) §3.7 flake handling, §6 CI
  orchestration. **Status: `IN REVIEW`.** All citations tagged
  per KD-4 status caveat.
- Spec #20 (Code Standards & Style Guide) §3 zero-allocation rules
  and `[HotPathAllocExempt]` attribute. **Status: `APPROVED` (May 11,
  2026).**
- Each approved spec's §6 (or §4.5 in #6's case): #1 Ball Physics §6,
  #2 Agent Movement §6, #3 Collision System §6, #4 First Touch §6, #5
  Pass Mechanics §6, #6 Shot Mechanics §4.5 (0.05ms total / ~0.017ms
  estimated — verified against `shot-mechanics/section-4.md` per
  outline.md adversarial review), #7 Perception §6, #8 Decision Tree
  §6, #17 Event System §6.

**Bidirectional sequencing with #16** (per CLAUDE.md OPEN ISSUES).

- #16's Tier 2 final approval is gated on `#9 / #17 / #18 / #19`
  reaching `IN REVIEW`. #17 is already `APPROVED` (beyond the gate).
- #16's `APPROVED` status is a precondition for #18's own `APPROVED`
  status (so tags can resolve).
- Resolution path: (1) #18 reaches `IN REVIEW` with
  citations to #16; (2) #16 reaches Tier 2 `APPROVED`; (3) #18's
  tags resolve and #18 advances to `APPROVED`.
  `SPEC_INDEX.md` status transitions for #18 MUST follow this order.

**Bidirectional sequencing with #19.**

- Per `testing-strategy/outline-detailed.md` v1.1 §1.4, #19's
  advancement past `IN REVIEW` was gated in part on #18 having an
  outline-level draft with §4 and §7 headers — that precondition is
  satisfied by `outline-detailed.md` v1.0 (May 13, 2026, earlier same
  day) and by this section file's existence. #18's `APPROVED` status
  is not symmetrically gated on #19, but tags on #19
  citations can only resolve once #19 is `APPROVED`.

**Downstream.**

- Every per-spec §6 (consumes Spec #18 budget-tagging schema and
  roll-up rule).
- `src/CLAUDE.md` (consumes pinned tooling, profiler invocation
  commands, allocation-tracker invocation, CI perf-step config —
  Stage 1).
- CI configuration files (Stage 1+).

**Cross-spec constants imported.** None. Spec #18 imports tier
*vocabulary* from #16 §1.3 by reference (KD-1 cite-not-redefine); no
`[CROSS]` constant declarations. Per-spec budget numbers are cited by
reference, not republished (KD-2).

**Stage 0 host platform pin.** Spec #18's regression gates require
the pins named in `docs/tracking/certification-platform.md`. Drafting
Spec #18 does not require those pins to be filled in; first activation
of a perf gate (Stage 0+1 transition) does. Tracked as a §5.2
activation criterion. Per CLAUDE.md OPEN ISSUES, every Stage 0 row
except CPU architecture is currently `_TBD_` / `⏳ Not pinned`.

## 1.5 Version History

| Version | Date         | Author      | Notes |
|---------|--------------|-------------|-------|
| 0.3     | May 14, 2026 | Claude Code | PASS-2 adversarial-review fix pass (`ERR-018-015`, `ERR-018-018` partial). Header `Last Updated` field corrected from May 13 → May 14. §1.3 codification map KD-3 row expanded from `§3.3, §3.8, §5.7` → `§3.3, §3.8.3, §3.8.4, §5.7.1` (L-4). §1.5 v0.1 row prose superseded-by note added (L-6 framing reconciliation). |
| 0.2     | May 14, 2026 | Claude Code | PASS-1 findings resolved: L-9 #16 §3.1→§3.1.2 (§1.1 scope, §1.2 out-of-scope, §1.3 KD-3 table); L-10 #16 §4→§4.8 EnvironmentFingerprint (§1.3 KD-11). Status caveat: v0.1 described the IN REVIEW flip as "author-driven"; PASS-1 ERR-018-011 clarified that SPEC_INDEX.md must be updated atomically with §9.4. That atomic update landed in v0.2; the v0.1 prose below is preserved as historical record. |
| 0.1     | May 13, 2026 | Claude Code | Initial draft from `outline-detailed.md` v1.1. §1.1 / §1.2 / §1.3 / §1.4 / §1.5 authored. KD-3 inverted per outline v1.1 — Spec #18 owns trace pipeline; Spec #16 retains canonical record format (§3.2.4.1), regression scenarios (§5), and emission-veto over tick-pipeline trace points (§3.1). All #16 / #19 citations tagged per KD-3 / KD-4 status caveats. SPEC_INDEX flip to `IN REVIEW` is **author-driven**, not review-driven: it reflects "draft complete, awaiting lead-developer sign-off" per CLAUDE.md status definition. The §9 approval-checklist rows have not been walked. (NOTE: superseded by v0.2 status caveat — PASS-1 ERR-018-011 ruled this framing a procedural violation; SPEC_INDEX.md must be flipped atomically, not author-driven.) |
