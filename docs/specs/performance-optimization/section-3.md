# Performance Optimization Strategy Specification #18 — Section 3: Technical Specification

**Created:** May 13, 2026
**Last Updated:** May 14, 2026 (v0.3 PASS-2 adversarial-review fix pass)
**Purpose:** Publishes the rule mechanics behind every FR-PO-### in §2.
This section does not restate FR statements; each subsection cites the
FR-PO-### range it implements and provides the *how*.

---

## 3.1 Budget Roll-up & Per-Spec §6 Schema (FR-PO-001 … 008)

### 3.1.1 Authority statement (KD-2)

Per-spec §6 (or §4.5 in Shot Mechanics #6's case) declares the spec's
budget. Spec #18 ratifies via the §3.1.3 roll-up; Spec #18 does not
override. Rationale: per-spec budgets are derived from per-spec
algorithmic structure (e.g., Shot Mechanics §4.5's 0.05ms total derives
from the algorithm complexity declared in #6 §4 — not from a top-down
quota); overriding them would invalidate the per-spec §6 sections that
were adversarially reviewed and approved.

### 3.1.2 Per-spec §6 schema

Every §6 MUST publish:

- **Total per-tick budget** (ms).
- **Per-tick budget by loop tag** (10 Hz / 60 Hz, per KD-8).
- **Allocation budget** — always 0 on hot paths per KD-10; positive
  values permitted only on declared one-shot warmup paths exempted via
  the `[HotPathAllocExempt]` attribute (Spec #18 §3.7.5; first-
  implementation site at first `src/` commit).
- **Worst-case input parameters** that yield the budget (e.g., "22
  active agents, max possession contention").
- **Headroom multiplier** — a dimensionless factor reserved for
  variance; `[GT]` typically in the 1.2× – 1.5× range, pinned by the
  owning spec.
- **Cross-spec budget consumption** — for each `[CROSS]` or
  `[CROSS-PENDING]` budget value consumed from another spec's §6, cite
  the source spec, section, and value being consumed. Required only
  when the spec actually consumes a cross-spec budget; omitted otherwise.

Schema published in **Appendix B** as a paste-ready template.

### 3.1.3 Roll-up table

A single roll-up table per platform target is maintained in
**Appendix C**. Columns:

| Spec ID | Declared budget | Loop tag | Alloc budget | Citation (link to spec §6) | Last verified date |

Roll-up updates are mechanical sync from per-spec §6 — never a design
decision (FR-PO-004). The Stage 0+1 `tools/budget-auditor.py` (§5.3)
emits this table programmatically; Stage 0 reviewers populate it by
hand.

### 3.1.4 Platform headroom

- **60 Hz frame budget:** ~16.67 ms per frame. Minus engine overhead
  (renderer, audio, input poll, GC pump) leaves the gameplay-loop
  slice. The concrete engine-overhead number is pinned at Stage 0+1
  once Unity LTS revision and scripting backend are fixed in
  `certification-platform.md`. Tracked as §7.5 D7.
- **10 Hz tick budget:** 100 ms per tick. Same headroom decomposition.
- **Stage 0 placeholder.** Until `certification-platform.md` is pinned,
  headroom is recorded as `[EST]` per CLAUDE.md constant tags, with a
  §3.1.4 placeholder row in Appendix C explicitly marking it.

### 3.1.5 Re-allocation procedure (FR-PO-005, FR-PO-008)

Triggered when the §3.1.3 roll-up total exceeds the §3.1.4 headroom on
any platform target.

Process:

1. Lead-developer convenes an explicit re-allocation review.
2. Each affected spec's §6 is amended (with a version-history entry on
   that spec) to declare a revised budget.
3. Spec #18 §3.1.3 roll-up table is updated atomically with the §6
   amendments.
4. The re-allocation review is logged as an `ERR-018-NNN` row in
   `spec-error-log.md` for traceability.

No silent re-allocation; no unilateral #18 override (per KD-2).

## 3.2 Loop Separation & Per-Tick Budget Mechanics (FR-PO-009 … 015)

### 3.2.1 Citation

CLAUDE.md "Heartbeat Tick Rate" — 10 Hz tactical, 60 Hz physics. These
are different loops; per-spec budgets MUST be split by which loop they
live on.

### 3.2.2 Loop-tag mandate

Every budget number in every spec's §6 MUST carry one of:

- `[LOOP-TACTICAL-10HZ]` — runs on the 10 Hz tactical heartbeat.
- `[LOOP-PHYSICS-60HZ]` — runs on the 60 Hz physics / render frame.

Untagged numbers are a §5.5 conformance failure.

### 3.2.3 Cross-loop subsystems

Subsystems that run in both loops (e.g., Decision Tree #8 produces
tactical decisions at 10 Hz but reads physics state updated at 60 Hz)
MUST declare separate budgets for the work each loop performs. Example
intent in #8 §6 would read:

- Tactical decision selection: 0.4 ms `[LOOP-TACTICAL-10HZ]`
- Per-frame perception buffer read: 0.02 ms `[LOOP-PHYSICS-60HZ]`

### 3.2.4 Aggregation rule

The 60 Hz budget total includes only 60 Hz-tagged entries; the 10 Hz
budget total includes only 10 Hz-tagged entries. Mixed-loop totals are
forbidden — they obscure where time is spent and invite category
errors.

### 3.2.5 Platform target pinning (KD-9)

Budgets are stated against the platform pinned in `docs/tracking/
certification-platform.md` Stage 0 row. Until that row is pinned, all
numeric budgets carry both the loop tag AND the `[EST]` source tag per
CLAUDE.md "Constant Tags"; these are promoted to `[GT]` or `[FIXED]`
once the platform pin lands.

### 3.2.6 Anti-patterns

- "Per-second" budget (ambiguous — is that 10 ticks or 60 frames?).
- "Per-call" budget without amortized call rate.
- Budget cited without loop tag.

## 3.3 Profiling Methodology — Determinism-Bound (FR-PO-016 … 023)

### 3.3.1 Citation

KD-6 (determinism-aware profiling). KD-3 inverted boundary with #16 §5
(regression scenarios — #16 retains authority).

### 3.3.2 Profiling session contract

Every profiling session declares:

- Git SHA of the build under measurement.
- Recorded seed (KD-6). Stage 0+1 sessions obtain the seed via
  `tools/select-seed.py` (Stage 0+1 deliverable; §7.1); Stage 0
  sessions use a manually chosen deterministic seed per Appendix E
  runbook.
- Recorded `EnvironmentFingerprint` per #16 §4.8.
- Platform pin per KD-9.
- Scenario manifest ID — references an #16 §5 scenario verbatim
 .
- Session start / end timestamps (wall-clock; used for run bookkeeping
  only, never for in-game state).
- Hardware perf-counter snapshot (CPU model, core count, thermal
  state).

Sessions missing any field are not entered into the baseline corpus —
the §3.4.4 validator rejects them.

### 3.3.3 Scenario binding (KD-3 inverted)

Spec #18 does not author its own scenarios. Every profiling session
runs an #16 §5 scenario verbatim — #16 retains authority over scenario
definitions per inverted KD-3. Cross-scenario profiling (a Spec #19
KD-8 cross-spec scenario) is permitted; the manifest ID and seed are
recorded the same way.

### 3.3.4 Sampling cadence

- **Sampling-profiler default:** 1 kHz wall-clock samples — the 10 Hz
  tactical loop produces 100 samples per tick; the 60 Hz physics loop
  produces ≈16.67 samples per frame (1000 Hz / 60 Hz over a ~16.67 ms
  frame window). `[EST]`; pinned to the chosen profiler at Stage 0+1
  (§7.5 D1).
- **Instrumented-profiler default:** full function-entry / exit tracing
  on every hot path (KD-10 union). Off by default in shipping builds;
  on by default in baseline-capture builds.

### 3.3.5 Profiler-pin policy

- **Stage 0:** profiler choice deferred (§7.5 D1). Stage 0 sessions use
  a manual `Stopwatch` harness — see Appendix E for the runbook.
- **Stage 0+1:** Unity Profiler + Superluminal / Tracy (or equivalent;
  selection criteria parallel Spec #19 §6.1 — must support
  deterministic re-play, must emit per-frame breakdown, must support
  headless / batch-mode capture for CI).

### 3.3.6 Anti-patterns

- Profiling against wall-clock-seeded gameplay (KD-6 violation;
  FR-PO-020).
- Profiling in editor-mode without scripting-backend pin (Mono vs
  IL2CPP give very different numbers; FR-PO-021).
- Capturing without recording `EnvironmentFingerprint` (KD-11
  violation; FR-PO-017).

## 3.4 Optimization Ladder (FR-PO-024 … 030)

### 3.4.1 Five-rung ladder

1. **Measure.** Capture pre-fix baseline per the §3.3 session contract.
2. **Attribute.** Identify which function / allocation site / cache-
   miss site dominates the captured metric.
3. **Fix.** Apply the smallest local change that addresses the dominant
   attribution.
4. **Verify.** Capture post-fix baseline against the same scenario,
   seed, and platform pin. Compare against §3.5.2 regression-gate
   threshold and require the improvement to be statistically
   significant per §3.4.3.
5. **Lock.** Record the new baseline. Atomic-update the Appendix C
   roll-up row. Close the optimization ticket.

### 3.4.2 Anti-skipping rule

Each rung is mandatory. "I'm sure this will be faster" without
Measure → Attribute is forbidden. Optimization PRs without pre-fix
baseline evidence are blocked at review (FR-PO-025).

### 3.4.3 Statistical-significance rule

Improvement claims require N **independent profiling runs** (not N
within-run tick samples) with a non-overlapping 95% confidence interval
against the pre-fix baseline, each run under a distinct recorded seed
and the same scenario + platform pin. N is `[GT]`, pinned at Stage 0+1
(§7.5 D8) — provisional value 30 runs / 95% CI per Spec #19 §3.4.3
parallel convention.

Below-significance "improvements" are not entered into the baseline;
they are recorded as a §6.4 "Inconclusive" defect class.

### 3.4.4 Baseline validator (KD-11)

The baseline validator checks every captured baseline against the §3.3.2
session contract and rejects sessions missing any field.

Reproducibility check (Stage 0+1): the validator MUST re-run the
session under the recorded seed + fingerprint + platform pin and
confirm the captured metric matches within the §3.4.3 confidence
interval. Mismatches mark the baseline stale per FR-PO-068. Silently
skipping the re-run is itself an FR-PO-067 violation and is
merge-blocking per FR-PO-068. (Stage 0 carve-out per §5.4: at Stage 0
there is no `src/` and no scenario-deterministic runtime to re-run
against; the MUST activates with the rest of FR-PO-063 … 068 at
Stage 0+1.)

### 3.4.5 Optimization-ticket lifecycle

Tickets are tracked in the project issue tracker; cross-spec defects
are additionally logged as `ERR-018-NNN` rows in `spec-error-log.md`
(§8.1.6). Tickets reference:

- The FR-PO ID of the gate they're addressing.
- The pre-fix baseline SHA.
- The target metric improvement.

Closed tickets additionally reference the post-fix baseline SHA.

## 3.5 Performance Regression Gates (FR-PO-031 … 040)

### 3.5.1 Citation

KD-3 (boundary with #16); KD-4 (boundary with #19); KD-5 (stage-gated
activation).

### 3.5.2 Gate threshold

- **Default regression threshold:** post-PR baseline MUST be within
  +5% (`[GT]`, §7.5 D9; provisional — set conservatively ahead of
  first CI data; re-evaluated at Stage 0+1 against measured baseline
  variance) of the pre-PR baseline for the same scenario, seed, and
  platform pin, per spec, per loop.
- **Allocation regression threshold:** any non-zero allocation on a
  hot path (KD-10 union) blocks merge regardless of magnitude.
- **Per-spec overrides:** a spec's §6 MAY declare a tighter threshold.
  Example: Shot Mechanics #6 §4.5 declares a 0.05 ms total budget with
  a ~0.017 ms spec-time `[EST]` anchor. The first Stage 0+1 capture
  applies the §3.9.1 ±20% promotion tolerance (the gate's MAY override
  surface is not exercised yet — the value is still an `[EST]` anchor,
  not a measured `[GT]` baseline). Once promoted, subsequent per-PR
  captures apply the §3.5.2 default +5% gate against the measured
  baseline, or a tighter override if #6 §4.5 declares one.

### 3.5.3 Gate composition (boundary with #16 / #19)

| Gate | Authority | Block condition |
|------|-----------|-----------------|
| Functional | Spec #19 §6.2 | Any functional test fails |
| Determinism | Spec #16 §5 + §3.2.4.1 | Bitwise mismatch against canonical-record-format golden trace |
| Performance | Spec #18 §3.5.2 (this spec) | §3.5.2 threshold exceeded |
| Allocation | Spec #18 §3.7 (this spec) | Non-zero allocation on hot-path entry |

No gate is "soft". Flake quarantine (Spec #19 §3.7)
applies to functional gates only — perf-gate variance exceeding the
§3.5.2 threshold is treated as a potential KD-6 violation, triggers
root-cause analysis per §6.4; confirmed non-determinism routes to
#16 §5 triage, not #19 §3.7 quarantine.

### 3.5.4 Stage-0 posture (KD-5)

- **Stage 0:** no CI perf gate active. Performance regressions are
  surfaced via the §6.2 local runbook against synthetic harnesses that
  exercise pre-`src/` profiling tooling.
- **Stage 0+1:** CI perf gate activates with §3.5.2 threshold enforced
  on per-PR baselines.

### 3.5.5 Anti-patterns

- "Threshold exceeded but feature is important" exception: handled via
  the same exception-with-sign-off semantics as Spec #20 §2.1 / Spec
  #19 §2.1, not via silent threshold bypass (FR-PO-040).
- Per-PR threshold relaxation by repeated +5% increments ("budget
  creep"): caught by the §3.5.6 absolute-threshold guard.

### 3.5.6 Absolute-threshold guard (FR-PO-039)

Independent of the per-PR delta gate, a parallel guard compares against
the milestone baseline (last Stage milestone). Drift beyond +10%
(`[GT]`, provisional — set conservatively at twice the §3.5.2 per-PR
threshold; re-evaluated at Stage 0+1 against measured baseline variance
on the same cadence as §3.5.2 / §7.5 D9) of the milestone baseline
blocks merge regardless of how incremental the per-PR deltas were.
Prevents budget creep.

## 3.6 Degradation Policy — Tier C Only (FR-PO-041 … 047)

### 3.6.1 Citation

KD-7; #16 §1.3 tier classification.

### 3.6.2 Tier A invariant

Authoritative outputs (ball state, agent position, agent decision,
event emission) MUST NOT vary under performance pressure. Any proposed
degradation path that touches a Tier A output is rejected at spec
review (FR-PO-041, FR-PO-042).

### 3.6.3 Tier B tolerance

Bounded-authoritative outputs MAY vary within their declared tolerance.
Tier B degradation paths MUST be declared at spec time in the owning
spec's §6 (not adopted at runtime), and the tolerance band MUST be
cited from the owning spec (FR-PO-043, FR-PO-044 / KD-1).

### 3.6.4 Tier C surface

Render LOD, debug overlay fidelity, telemetry sampling, dashboard
refresh — the only acceptable runtime degradation surface.

Tier C degradation paths are declared in the §3.6.4 itemized table
below (Stage 1 deliverable; Stage 0 declares the policy + an empty
table).

| Path | Trigger | Action | Reversibility | Owner |
|------|---------|--------|---------------|-------|
| _(reserved; Stage 1)_ | | | | |

### 3.6.5 Stage-0 posture

Stage 0 declares NO dynamic degradation paths at all. All Stage 0
budget enforcement is manual remediation (FR-PO-046). Stage 1
adaptive-degradation posture is a deferred decision (§7.5 D5).

### 3.6.6 Anti-patterns

- "Skip a physics sub-step under load" — Tier A violation.
- "Run AI decision tree every other tick under load" — Tier A
  violation.
- "Reduce trace verbosity under load" — permitted (Tier C); declare in
  the §3.6.4 table.

## 3.7 Hot-Path Enumeration Policy & Zero-Allocation Enforcement (FR-PO-048 … 053)

### 3.7.1 Citation

KD-10; CLAUDE.md "When Writing Code: zero-allocation architecture in
the game loop".

### 3.7.2 Enumeration rule

The set of hot paths is the union of every approved spec's §6 budget
table. No separate hot-path list is maintained. The union is
materialized at build time as `tools/hot-path-union.json` (Stage 0+1
deliverable; Stage 0 placeholder structure declared in Appendix D).

### 3.7.3 Allocation budget

Every hot-path entry has allocation budget = 0 bytes per tick. The
per-build allocation tracker (Stage 0+1) dumps managed-allocation
counts per method; the dump is diff'd against the §3.7.2 union to
identify violators.

### 3.7.4 Enforcement mechanism

A CI alloc-tracker step (Stage 0+1) blocks merge on any non-zero
allocation in a §3.7.2 union method. Editor-mode runs do not enforce
(Mono GC behaviour differs from IL2CPP); enforcement requires the
IL2CPP build per `certification-platform.md` (FR-PO-052).

### 3.7.5 Exemption procedure — `[HotPathAllocExempt]` attribute (declared here)

Genuine one-shot allocations (e.g., scene-load buffer growth) are
exempted via the `[HotPathAllocExempt]` attribute. Spec #18 owns the
governance identifier (the attribute name) and the exemption policy
(below); the concrete C# `Attribute` signature — target enum,
constructor argument shape, companion-comment grammar — is a Stage 0+1
deliverable that lands at first `src/` commit alongside the allocation-
tracker pin (§7.5 D2). Per CLAUDE.md "Interface Design Principle"
(ERR-001 / ERR-004 hazard), the signature is not fixed at spec time
because its consumer (the allocation-tracker CI step) is not yet
specified.

The governance contract that holds regardless of signature:

- Every exemption MUST carry a rationale (one or more sentences).
- Every exemption MUST be authorized by lead-developer sign-off recorded
  as a row in `spec-error-log.md`.
- Every exempted call site MUST be marked at the source level so the
  alloc-tracker CI step can exclude it from the §3.7.4 diff.

Spec #20 (`APPROVED` May 11, 2026) is not the declarer of the
attribute; outline v1.0's "declared in Spec #20 §3" claim was an
inherited citation drift (filed as `ERR-018-002`, resolved in v0.2
by relocating ownership here). If a future Spec #20 revision adopts
the attribute into its zero-allocation catalogue, the binding is
`[CROSS]` from Spec #20 back into this section — not the other way
around.

Exemptions require lead-developer sign-off and a comment citing the
rationale (FR-PO-053).

### 3.7.6 Anti-patterns

- "It only allocates once at warmup": still on the hot path → still
  blocks; use the §3.7.5 exemption attribute.
- Boxing of value types in interface dispatch.
- LINQ on hot paths (banned per Spec #20 §3.3 / Appendix D "alloc-hot-path" category).

## 3.8 Trace Pipeline & Dashboard Mechanics (FR-PO-054 … 062)

> **KD-3 inversion (outline v1.1).** Spec #18 owns the trace pipeline
> (channels, verbosity tiers, sampling, instrumentation API, dashboard
> aggregation). Spec #16 retains authority over (a) the canonical
> record format at #16 §3.2.4.1, (b) determinism-of-emission
> constraints for trace points inside the canonical tick pipeline at
> #16 §3.1, and (c) the regression-scenario corpus at #16 §5. Outline
> v1.0's "#16 §8 trace channels" citation was an artifact of inherited
> drift from #19 v0.1 and is retracted (#16 §8 is "References &
> Citation Audit" — no §8 trace-channel section exists in #16). See
> `ERR-018-001`.

### 3.8.1 Citation

KD-3 inverted; #16 §3.2.4.1 for record-format binding; #16 §3.1.2 for
emission-veto authority over tick-pipeline trace points.

### 3.8.2 Trace pipeline architecture (#18-owned)

- **Channel registry.** Named channels per subsystem, declared in
  the Appendix F catalogue. **Schema** (channel name, owning
  subsystem, default verbosity, sampling rule, sink routing,
  determinism class, tick-pipeline flag, sign-off log reference,
  record-format version, owner, created date, version history) is
  published in **Appendix F.0 (Stage 0 deliverable)**; **populated
  channel rows** are a Stage 1 deliverable.
- **Verbosity tiers** (FR-PO-055):
  - `minimal` — production / shipping builds.
  - `standard` — development.
  - `debug` — issue investigation.
  - `exhaustive` — golden-trace capture.
  Concrete tier semantics pinned at Stage 0+1 (§7.5 D10).
- **Sampling rules per tier** (FR-PO-056):
  - `exhaustive` — every-tick capture.
  - `debug` — per-N-ticks, dense (captures at higher rate than
    `standard`; N pinned at Stage 0+1, §7.5 D10).
  - `standard` — per-M-ticks, sampled (lower rate than `debug`;
    M pinned at Stage 0+1, §7.5 D10).
  - `minimal` — event-driven only.
- **Channel-to-sink routing.** In-memory ring buffer (default), file
  sink (baseline-capture builds), network sink (Stage 1+ telemetry).
- **Instrumentation API surface.** Declared in §4.3; consumed by
  `src/<spec>/` code.

### 3.8.3 Determinism-of-emission constraints (FR-PO-058a, new in outline v1.1)

Every trace point emitted by a #18-owned channel MUST conform to #16's
determinism rules:

- No wall-clock-derived field.
- No `System.Random` field.
- No managed allocation on hot-path tick code.
- No field that captures `EnvironmentFingerprint`-divergent data (CPU
  brand string, locale, etc.).

**#16 veto authority.** Any trace point #18 proposes to insert *inside*
the canonical tick pipeline (#16 §3.1.2,) requires
#16-owner sign-off. Trace points emitted *outside* the tick pipeline
(editor-only tooling, CI harness, post-tick aggregation) do not require
#16 sign-off but still must conform to the four constraints above.

Enforcement: §5.7 boundary review walks the channel registry at each
#18 revision and flags any registry entry that emits inside #16 §3.1
without recorded #16-owner sign-off.

### 3.8.4 Record-format binding (KD-11)

Trace records and baseline records both serialize to the canonical
binary layout at #16 §3.2.4.1.

The trace pipeline does NOT define a parallel record format — that
authority remains with #16. #18 only chooses *which* records are
emitted, *when*, *into which channel*, and *how* they are aggregated
downstream.

Drift between #18's emitted records and #16 §3.2.4.1's layout is a
§5.7 boundary-review-blocking finding.

### 3.8.5 Dashboard architecture

Dashboards consume the trace records emitted by §3.8.2 channels in the
§3.8.4 / #16 §3.2.4.1 format. Aggregation logic (rolling averages, p99
windows, regression bands) lives entirely in Spec #18's dashboard
implementation in `tools/perf-dashboard/`. Gameplay code in `src/`
MUST NOT reference dashboard helpers (Spec #20 §4.1 dependency-arrow
rule).

### 3.8.6 Dashboard catalogue (Stage 1 deliverable; Stage 0 declares schema in Appendix F)

- Per-spec per-tick budget dashboard.
- Per-PR delta dashboard.
- Milestone-baseline trend dashboard.
- Allocation-tracker dashboard.
- Flake/determinism cross-reference dashboard (joins #16 §5 flake data
  with #18 §3.4.4 baseline validator output).

### 3.8.7 Refresh cadence

- Per-PR delta: synchronous with the CI run.
- Milestone trend: weekly; nightly at Stage 1.

### 3.8.8 Anti-patterns

- **Adding a trace point inside #16 §3.1.2 without #16-owner sign-off**
  (violates emission-veto authority; §5.7 boundary block).
- Emitting a trace record in a non-canonical layout (violates KD-11
  binding to #16 §3.2.4.1).
- Embedding dashboard logic in `src/` gameplay code (dashboards live
  in `tools/` per §4.3).
- Trace channel that captures wall-clock or `EnvironmentFingerprint`-
  divergent fields without explicit Tier C tagging (Tier A / B
  channels MUST be determinism-clean).

## 3.9 Edge Cases

- **3.9.1 — Spec-time perf claims** (e.g., Shot Mechanics #6 §4.5's
  "0.017 ms estimated"): treated as `[EST]` baseline anchors; the
  first Stage 0+1 baseline capture promotes the estimate to a measured
  value tagged `[GT]` if within ±20% `[GT]` of estimate, or files an
  `ERR-018-NNN` review finding if not.
- **3.9.2 — Editor-only / debug-tool perf:** outside the KD-10
  hot-path union; alloc-tracker exempt; functional rules still apply.
- **3.9.3 — Multi-platform divergence (Stage 5+):** when Stage 5
  multiplayer activates, budgets per platform pin (KD-9) may diverge;
  reconciliation is a Stage 5 deferred decision (§7.5 D6), not a
  Stage 0 concern.
- **3.9.4 — First-tick warmup:** the first N ticks after scene load
  are exempt from §3.5.2 regression gates (warmup allocations, JIT for
  Mono); N pinned at Stage 0+1.
- **3.9.5 — Soak runs:** long-horizon profiling (≥ one full match) is
  owned by Spec #19 §3.1 end-to-end / soak layer
  for *test execution*;
  Spec #18 §3.3 governs the perf-metric capture *from* those runs.
  Both apply, no overlap.

## 3.10 Constants Catalogue (governance metadata only)

Spec #18 declares **no physical constants**. Numeric thresholds it
publishes are governance values tagged `[GT]` with rationale recorded
inline at the point of declaration:

| Value | Tag | Defined in | Rationale |
|-------|-----|------------|-----------|
| Per-PR regression threshold = +5% | `[GT]` | §3.5.2 | Below first-Stage-1 measured variance band; tightenable at §7.5 D9 |
| Absolute-threshold guard = +10% | `[GT]` | §3.5.6 | Twice per-PR threshold; catches creep without false-positives on legitimate stepwise growth |
| Hot-path allocation budget = 0 bytes/tick | `[FIXED]` | §3.7.3 | Mandated by CLAUDE.md "When Writing Code: zero-allocation architecture in the game loop"; non-tunable invariant |
| Sampling-profiler default = 1 kHz | `[EST]` | §3.3.4 | Pinned to chosen profiler at Stage 0+1 (§7.5 D1) |
| Statistical-significance N = 30 samples / 95% CI | `[EST]` | §3.4.3 | Pinned at Stage 0+1 (§7.5 D8); parallel to Spec #19 §3.4.3 |
| Headroom multiplier (per spec) | `[GT]` | §3.1.2 | Owning-spec discretion; typical 1.2× – 1.5× |
| First-tick warmup count N | `[EST]` | §3.9.4 | Pinned at Stage 0+1 once Mono/IL2CPP warmup characteristics measured |
| `[EST]`→`[GT]` promotion tolerance = ±20% | `[GT]` | §3.9.1 | Twice the +5% per-PR threshold; absorbs first-measurement variance when promoting a spec-time `[EST]` anchor to a measured `[GT]` baseline. Above ±20% files an `ERR-018-NNN` review finding. |
| Per-spec p50/p99 rolling window N = 100 captures | `[GT]` | Appendix F.1 | Stage 0+1 pin; rolling-window size for per-spec budget-dashboard percentiles. Tied to first-month CI capture volume; tightenable once Stage 1 dashboard front-end data confirms variance characteristics. |
| Flake-rate boundary-defect routing threshold = 1% | `[GT]` | Appendix F.5 | Stage 1 governance pin; flake rate above 1% on the perf-baseline-validator output routes to §5.7.3 boundary-defect class (rather than the §6.4 normal defect class). 1% chosen as the inflection where a measurement-noise hypothesis is exhausted and a determinism / boundary hypothesis becomes more likely. |

**Evidence-artifact convention** (parallel to Spec #19 §3.10 L5
convention and Spec #19 §9.4). Each `[GT]` governance number's
evidence is the citation line in this spec's body text that introduces
the number — for example, the +5% per-PR threshold's evidence is
`section-3.md §3.5.2`; the +10% milestone-baseline guard's evidence is
`section-3.md §3.5.6`. The §5.3 / §9 auditor (or the Spec #19 §5.3
checklist auditor under #19 KD-6) resolves these citations by
confirming the cited file path contains the literal number claimed.
No separate `tools/governance-numbers.md` file is created.

Per-spec physical budgets cited (not republished) live in each spec's
§6 / §4.5; the citation list is the §3.1.3 roll-up table.

## 3.11 Version History

| Version | Date         | Author      | Notes |
|---------|--------------|-------------|-------|
| 0.3     | May 14, 2026 | Claude Code | PASS-2 adversarial-review fix pass (`ERR-018-013`, `ERR-018-014`, `ERR-018-016`, `ERR-018-018`, plus L-1 / L-2 / L-5 / L-7 / L-8 housekeeping). §3.10 — three duplicate-constant rows deleted (±20% promotion tolerance, N=100 dashboard window, 1% flake threshold; root cause PR #59 + PR #60 merge collision). §3.5.2 Shot Mechanics example rewritten to apply §3.9.1 ±20% promotion tolerance at first capture, then +5% gate thereafter. §3.7.5 deferred concrete C# attribute signature to Stage 0+1 (D2) per CLAUDE.md Interface Design Principle; retained signature-independent governance contract (rationale, sign-off, source-level marker). §3.1.2 schema added `Cross-spec budget consumption` field aligning with Appendix B §6.5. §3.3.4 sampling-profiler count fixed `~17` → `≈16.67`. §3.5.6 absolute-threshold guard gained "(provisional)" qualifier matching §3.5.2. §3.5.3 gate-composition table "(this section)" → "(this spec)". §3.7.6 LINQ anti-pattern "— cite" placeholder resolved to Spec #20 §3.3 / Appendix D "alloc-hot-path". Duplicate v0.2 version-history row consolidated. |
| 0.2     | May 14, 2026 | Claude Code | PASS-1 adversarial-review fix pass (`ERR-018-002` / 003 / 005 / 006 / 007 / 008 / 010). §3.1.2 + §3.7.5 reworded — `[HotPathAllocExempt]` declared in #18 §3.7.5 (no longer cites Spec #20 §3); first-implementation site at first `src/` commit. §3.4.4 MAY → MUST with Stage 0 carve-out + FR-PO-068 merge-blocking link. §3.8.2 channel-registry bullet rewritten to cite **Appendix F.0** (Stage 0 schema deliverable). §3.10 — Hot-path allocation budget re-tagged `[GT]` → `[FIXED]`; ±20% promotion tolerance, N=100 rolling-window, 1% flake-rate threshold added with rationale. §3.3.5, §3.4.3, §3.9.5 gain `(TBD-NORMATIVE; #19 status IN REVIEW)` parenthetical. §3.9.1 inline `[GT]` tag on ±20%. Also: standard/debug verbosity tiers differentiated; 60Hz sample count added §3.3.4; N clarified as independent runs §3.4.3; seed-selection tool stub added §3.3.2; `[GT]` provisional note in §3.5.2; #16 §3.1→§3.1.2 emission-veto citations; #16 §4→§4.8 EnvironmentFingerprint; perf-gate flake claim softened §3.5.3; ticket storage added §3.4.5. |
| 0.1     | May 13, 2026 | Claude Code | Initial draft from `outline-detailed.md` v1.1 §3. Eleven subsections (§3.1 … §3.11) cover budget roll-up, loop separation, profiling, optimization ladder, regression gates, degradation policy, hot-path enumeration, trace pipeline (KD-3 inverted), edge cases, governance constants. All #16 / #19 citations tagged. |
