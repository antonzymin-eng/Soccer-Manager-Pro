# Performance Optimization Strategy Specification #18 — Appendices

**Created:** May 13, 2026
**Last Updated:** May 14, 2026 (v0.3 PASS-2 adversarial-review fix pass)
**Purpose:** Reference artifacts cited by §3 / §4 / §5 / §6 / §7 / §9.
Appendix A baseline-record format binds to #16 §3.2.4.1 (KD-11);
Appendix B is the paste-ready per-spec §6 schema; Appendix C is the
roll-up table; Appendix D is the approved-spec §6 survey (Stage 0+1
fillable); Appendix E is the Stage-0 local runbook; Appendix F is the
dashboard schema catalogue; Appendix G is the spec-specific glossary.

---

## Appendix A — Baseline Record File Format

> **Binding:** the on-disk byte layout conforms to Spec #16 §3.2.4.1
> canonical record format. Per inverted KD-3, record
> format is #16-authoritative; #18 only declares which fields the
> baseline-capture path emits into the canonical layout.

### A.1 Logical schema

Baseline records have two logical sections: **session manifest** (per
§3.3.2) and **captured metrics**.

**Session manifest fields:**

| Field | Type | Required | Source |
|-------|------|----------|--------|
| `git_sha` | string (40 hex) | yes | Build under measurement |
| `seed` | uint64 | yes | KD-6 recorded seed |
| `environment_fingerprint` | structured | yes | #16 §4.8 |
| `platform_pin` | structured | yes | `certification-platform.md` Stage 0 row |
| `scenario_manifest_id` | string | yes | #16 §5 scenario ID |
| `session_start_utc` | RFC 3339 timestamp | yes | wall-clock bookkeeping only |
| `session_end_utc` | RFC 3339 timestamp | yes | wall-clock bookkeeping only |
| `hardware_counters` | structured | yes | CPU model, core count, thermal state |
| `harness_version` | semver | yes | Stage 0 `tools/perf-harness/` version; Stage 0+1 `tests/perf/` version |

**Captured-metric fields:**

| Field | Type | Required | Notes |
|-------|------|----------|-------|
| `per_tick_ms_p50` | float ms | yes | Median per-tick / per-frame budget consumption |
| `per_tick_ms_p99` | float ms | yes | p99 per-tick / per-frame budget consumption |
| `per_method_alloc_bytes` | map<method, uint64> | yes | Per-method managed allocation totals |
| `cache_miss_counters` | map<counter, uint64> | optional | Where hardware counters available |
| `loop_tag` | enum {`LOOP-TACTICAL-10HZ`, `LOOP-PHYSICS-60HZ`} | yes | Per KD-8 |
| `pass_fail_vs_threshold` | enum {pass, fail, advisory} | yes | Advisory at capture time; authoritative at gate-evaluation time |
| `threshold_cited` | string | yes | The FR-PO-### or per-spec §6 number compared against |

### A.2 Versioning

The record carries a `format_version` field whose semantics are
inherited from #16 §3.2.4.1. Section authors MUST
re-grep #16 at draft time to confirm the canonical byte layout.

### A.3 Stage 0 placeholder

At Stage 0, baseline records live at `docs/specs/performance-
optimization/baselines/<spec>/<scenario>-<seed>-<git_sha>.json` in a
JSON projection of the schema above. The atomic migration to
`tests/data/baselines/<spec>/` (binary canonical layout) happens at
the first `src/` commit per FR-PO-074; no migration script needed
because the JSON projection round-trips into the canonical binary
layout via #16 §3.2.4.1 serializer.

---

## Appendix B — Per-Spec §6 Schema Template

Paste-ready Markdown template. Every per-spec §6 MUST conform to this
shape per FR-PO-002 (KD-2 / KD-8 / KD-10).

```markdown
## 6 Performance Analysis

### 6.1 Total Per-Tick Budget

| Loop | Total per-tick budget | Source tag |
|------|-----------------------|------------|
| `[LOOP-TACTICAL-10HZ]` | _N_ ms | `[GT]` |
| `[LOOP-PHYSICS-60HZ]` | _N_ ms | `[GT]` |

### 6.2 Per-Tick Budget Breakdown

| Subroutine | Loop tag | Budget (ms) | Allocation budget (bytes/tick) | Source tag |
|------------|----------|-------------|--------------------------------|------------|
| _routine A_ | `[LOOP-…]` | _N_ | 0 | `[GT]` |
| _routine B_ | `[LOOP-…]` | _N_ | 0 | `[GT]` |

Allocation budget MUST be 0 for every hot-path entry per Spec #18 §3.7
(KD-10). Exemptions require `[HotPathAllocExempt]` per Spec #18 §3.7.5
(governance identifier declared in §3.7.5; C# attribute definition
deferred to Stage 0+1) with lead-developer sign-off.

### 6.3 Worst-Case Input Parameters

State the input conditions under which §6.1 budget is measured. For
example: "22 active agents, max possession contention,
`HIGH_DENSITY_SCENARIO_ID` from #16 §5".

### 6.4 Headroom Multiplier

`[GT]` _multiplier_ — typical 1.2× to 1.5×. Rationale: <one line>.

### 6.5 Cross-Spec Budget Consumption

For each `[CROSS]` or `[CROSS-PENDING]` budget consumed from another
spec, cite the source spec, section, and the value being consumed.

### 6.6 Version History

| Version | Date | Author | Notes |
|---------|------|--------|-------|
```

---

## Appendix C — Budget Roll-up Table

Read-only roll-up of every per-spec §6 budget into a single cross-spec
table per platform target (FR-PO-003, §3.1.3).

**Platform target (Stage 0 placeholder):** `[EST]` — pinned at Stage
0+1 once `certification-platform.md` Stage 0 row populated.

| Spec ID | Declared budget | Loop tag | Alloc budget | Citation (link to spec §6) | Last verified date |
|---------|-----------------|----------|--------------|----------------------------|--------------------|
| #1 Ball Physics | _TBD survey_ | _TBD survey_ | 0 | `ball-physics/section-6.md` | _Stage 0+1_ |
| #2 Agent Movement | _TBD survey_ | _TBD survey_ | 0 | `agent-movement/section-6.md` | _Stage 0+1_ |
| #3 Collision System | _TBD survey_ | _TBD survey_ | 0 | `collision-system/section-6.md` | _Stage 0+1_ |
| #4 First Touch | _TBD survey_ | _TBD survey_ | 0 | `first-touch/section-6.md` | _Stage 0+1_ |
| #5 Pass Mechanics | _TBD survey_ | _TBD survey_ | 0 | `pass-mechanics/section-6.md` | _Stage 0+1_ |
| #6 Shot Mechanics | 0.05 ms total (~0.017 ms estimated) | _TBD survey for loop tag_ | 0 | `shot-mechanics/section-4.md §4.5` | per outline.md adversarial review |
| #7 Perception | _TBD survey_ | _TBD survey_ | 0 | `perception-system/section-6.md` | _Stage 0+1_ |
| #8 Decision Tree | _TBD survey_ | _TBD survey_ | 0 | `decision-tree/section-6.md` | _Stage 0+1_ |
| #17 Event System | _TBD survey_ | _TBD survey_ | 0 | `event-system/section-6.md` | _Stage 0+1_ |

**Headroom row** (Stage 0 placeholder):

| Loop | Frame/tick window | Engine overhead | Gameplay-loop slice | Source tag |
|------|-------------------|-----------------|---------------------|------------|
| `[LOOP-PHYSICS-60HZ]` | ~16.67 ms | _TBD_ | _TBD_ | `[EST]` |
| `[LOOP-TACTICAL-10HZ]` | 100 ms | _TBD_ | _TBD_ | `[EST]` |

The engine-overhead row is pinned at Stage 0+1 (§7.5 D7).

---

## Appendix D — Approved-Spec §6 Survey

Survey of #1 … #8 / #17 §6 sections rated against the Appendix B
schema. **Scope at #18 approval:** Appendix D ships with the schema
and table headers populated; row contents are a Stage 0+1 deliverable
(§7.2). The survey itself is *not* a #18-approval gate (per §9.2
Quality Checklist row); KD-2 grandfather dilution remains visible via
the empty rows even before the survey is filled in. Stage 1 trigger
for actual per-spec revisions remains unchanged. Parallel to Spec #19
Appendix D scope.

| Spec ID | Total per-tick budget published? | Loop tag present? | Alloc budget published (0 on hot path)? | Worst-case inputs declared? | Headroom multiplier declared? | Schema-conforming? | Missing fields | Remediation `ERR-018-NNN` |
|---------|----------------------------------|-------------------|------------------------------------------|------------------------------|-------------------------------|--------------------|----------------|---------------------------|
| #1 Ball Physics | _Stage 0+1 survey_ | | | | | | | |
| #2 Agent Movement | _Stage 0+1 survey_ | | | | | | | |
| #3 Collision System | _Stage 0+1 survey_ | | | | | | | |
| #4 First Touch | _Stage 0+1 survey_ | | | | | | | |
| #5 Pass Mechanics | _Stage 0+1 survey_ | | | | | | | |
| #6 Shot Mechanics | Yes (0.05 ms; §4.5) | _Stage 0+1 survey_ | _Stage 0+1 survey_ | _Stage 0+1 survey_ | _Stage 0+1 survey_ | _Stage 0+1 survey_ | _Stage 0+1 survey_ | _Stage 0+1 survey_ |
| #7 Perception | _Stage 0+1 survey_ | | | | | | | |
| #8 Decision Tree | _Stage 0+1 survey_ | | | | | | | |
| #17 Event System | _Stage 0+1 survey_ | | | | | | | |

---

## Appendix E — Stage-0 Local Runbook

Shell-script outline for `tools/run-perf-local.sh`. Stage 0
deliverable; concrete commands land in `src/CLAUDE.md` per §4.6.

```sh
#!/usr/bin/env bash
# tools/run-perf-local.sh
# Stage 0 local pre-commit perf-gate harness.
# Runs schema-conformance + loop-tag auditors against docs/specs/ only.
# Synthetic-harness invocation against tools/perf-harness/ for anchor baselines.

set -euo pipefail

# 1. Schema-conformance auditor (Spec #18 §5.3 manual fallback).
#    Walks every approved spec's §6 against Appendix B template.
#    Failures logged as ERR-018-NNN candidates.
python3 tools/budget-auditor.py --mode schema --docs docs/specs/

# 2. Loop-tag auditor (Spec #18 §5.5 manual fallback).
#    Regex pass over every §6 budget number for [LOOP-…] tag.
python3 tools/budget-auditor.py --mode loop-tag --docs docs/specs/

# 3. Synthetic-harness invocation (anchor baselines, no src/ yet).
#    Each scenario runs against tools/perf-harness/<scenario>.cs with
#    a recorded seed and best-available EnvironmentFingerprint stub.
for SCENARIO in tools/perf-harness/scenarios/*.manifest.json; do
    tools/perf-harness/run.sh \
        --scenario "$SCENARIO" \
        --seed "$(python3 tools/select-seed.py)" \
        --output docs/specs/performance-optimization/baselines/ \
        --mark-anchor
done

# 4. Reviewer pastes output into PR description (FR-PO-071).
echo "Stage 0 local perf-gate complete. Paste output above into PR."
```

`tools/budget-auditor.py` and `tools/perf-harness/run.sh` are Stage
0+1 deliverables (§7.1). At Stage 0 the auditor's behaviour is a
manual review against §3.1.2 schema and §3.2.2 loop-tag mandate; the
script above is the structure into which the automated implementation
will land.

---

## Appendix F — Trace Channel Registry & Dashboard Schema Catalogue

Paste-ready schemas for the §3.8.2 trace pipeline. F.0 is the
**channel registry schema** (Stage 0 deliverable per §3.8.2; populated
entries are a Stage 1 deliverable). F.1 … F.5 are the **dashboard
schemas** for §3.8.6 dashboards (Stage 1 deliverables). Every
dashboard cites its upstream channel from the F.0 registry, so F.0
ships before F.1 … F.5 can resolve their data-source fields.

### F.0 Channel Registry Schema

The channel registry is the catalogue of every named instrumentation
channel emitted by `src/` code under the §3.8.2 trace pipeline. Each
row in the populated Stage 1 registry conforms to this schema.

| Field | Type | Required | Notes |
|-------|------|----------|-------|
| `channel_name` | string (dotted, lower-snake — e.g., `perf.budget`) | yes | Globally unique; namespace prefix is the owning subsystem (`perf`, `ai`, `physics`, …) |
| `owning_subsystem` | string (spec ID, e.g., `#18`, `#8`) | yes | The spec whose §6 budget this channel reports against; for #18-internal channels (`perf.*`) the owner is #18 |
| `default_verbosity` | enum {`minimal`, `standard`, `debug`, `exhaustive`} | yes | Per §3.8.2 / FR-PO-055 |
| `sampling_rule` | enum {`every-tick`, `per-N-ticks`, `event-driven`} | yes | Per §3.8.2 / FR-PO-056; for `per-N-ticks` the integer `sample_n` field MUST be populated |
| `sample_n` | uint | conditional | Required iff `sampling_rule = per-N-ticks` |
| `sink_routing` | list of enum {`ring-buffer`, `file-sink`, `network-sink`} | yes | Per §3.8.2; `network-sink` Stage 1+ only |
| `determinism_class` | enum {`tier-a`, `tier-b`, `tier-c`} | yes | Per #16 §1.3 tier classification; Tier A / B channels MUST be determinism-clean per FR-PO-058a |
| `inside_tick_pipeline` | bool | yes | If true, every channel-emission point sits inside #16 §3.1 canonical tick pipeline and requires recorded #16-owner sign-off per FR-PO-058a |
| `sign_off_log_ref` | string (`ERR-NNN` or `spec-error-log.md` row ID) | conditional | Required iff `inside_tick_pipeline = true`; cites the row recording #16-owner sign-off |
| `record_format_version` | semver | yes | Pinned to the #16 §3.2.4.1 canonical-record-format version active at channel-registry-row creation date |
| `owner_contact` | string | yes | Spec-author or subsystem-owner GitHub handle / role title |
| `created_date` | RFC 3339 date | yes | Registry-row creation date |
| `version_history` | list of {date, semver, notes} | yes | Append-only; every channel-schema change records a row |

**Stage 0 status:** schema declared (this F.0 section). **Stage 1
status:** registry rows populated. **Audit hook:** §5.7.1 boundary
check walks every row with `inside_tick_pipeline = true` and confirms
`sign_off_log_ref` resolves to a present `spec-error-log.md` row.

**Stage 0 anchor rows** (illustrative; schema-conforming reference
entries published here so F.1 … F.5 dashboard data-source citations
resolve at draft time. Populated subsystem-channel rows are a Stage 1
deliverable per §3.8.2 / §7.2):

| `channel_name` | `owning_subsystem` | `default_verbosity` | `sampling_rule` | `sink_routing` | `determinism_class` | `inside_tick_pipeline` | `sign_off_log_ref` | `record_format_version` | `owner_contact` | `created_date` |
|---|---|---|---|---|---|---|---|---|---|---|
| `perf.budget` | `#18` | `standard` | `every-tick` | [`ring-buffer`, `file-sink`] | `tier-c` | false | _(n/a)_ | _per #16 §3.2.4.1 active version_ | Spec #18 author | 2026-05-14 |
| `perf.alloc` | `#18` | `debug` | `per-N-ticks` (`sample_n` = 1) | [`ring-buffer`, `file-sink`] | `tier-c` | false | _(n/a)_ | _per #16 §3.2.4.1 active version_ | Spec #18 author | 2026-05-14 |
| `perf.trace` | `#18` | `exhaustive` | `every-tick` | [`ring-buffer`, `file-sink`] | `tier-c` | true | `ERR-018-NNN` (filed when first tick-pipeline emission point is added) | _per #16 §3.2.4.1 active version_ | Spec #18 author | 2026-05-14 |

These three `perf.*` rows are anchor entries owned by Spec #18 itself
(observability of #18's own roll-up / alloc / dashboard pipeline); they
are the upstream channels for F.1 … F.5 dashboards. Per-subsystem
channels (e.g. `ai.*`, `physics.*`) land at Stage 0+1 as each
`src/<spec>/` subsystem instruments itself.

### F.1 Per-Spec Per-Tick Budget Dashboard

- **Data source:** `perf.budget` channel; verbosity `standard` or
  higher.
- **Aggregation:** per-spec p50 / p99 over last N=100 captures (`[GT]`,
  pinned at Stage 0+1).
- **Refresh cadence:** per-PR delta synchronous; milestone trend
  weekly (Stage 1: nightly).
- **Alert threshold:** §3.5.2 +5% per-PR; §3.5.6 +10% absolute.

### F.2 Per-PR Delta Dashboard

- **Data source:** `perf.budget` + `perf.alloc` channels; verbosity
  `standard`.
- **Aggregation:** post-PR baseline vs pre-PR baseline, per spec, per
  loop.
- **Refresh cadence:** per CI run.
- **Alert threshold:** FR-PO-031 (perf) / FR-PO-032 (alloc).

### F.3 Milestone-Baseline Trend Dashboard

- **Data source:** `perf.budget` channel; verbosity `standard`.
- **Aggregation:** rolling trend per spec, per loop, from last Stage
  milestone.
- **Refresh cadence:** weekly (Stage 1: nightly).
- **Alert threshold:** §3.5.6 +10% absolute.

### F.4 Allocation-Tracker Dashboard

- **Data source:** `perf.alloc` channel; verbosity `debug` or higher.
- **Aggregation:** per-method per-build managed-allocation totals;
  diff against hot-path union (§3.7.2).
- **Refresh cadence:** per CI run.
- **Alert threshold:** any non-zero alloc on a hot-path entry
  (FR-PO-032 / FR-PO-051).

### F.5 Flake/Determinism Cross-Reference Dashboard

- **Data source:** joins #16 §5 flake data with
  #18 §3.4.4 baseline-validator output.
- **Aggregation:** per-scenario flake rate vs perf-baseline staleness.
- **Refresh cadence:** weekly.
- **Alert threshold:** flake rate > 1% (`[GT]`; catalogued in
  §3.10 / §8.4; Stage 1 governance pin) triggers boundary-defect
  routing (§5.7.3).

---

## Appendix G — Glossary

Spec #18-specific terms only. Determinism / tier / scenario terms are
cited from #16; pyramid / coverage / flake terms are cited from #19.

- **Hot path.** A code path enumerated by the union of every per-spec
  §6 budget table (KD-10 / §3.7.2). Allocation budget = 0 bytes/tick.
- **Baseline.** A captured measurement of a per-spec budget under the
  §3.3.2 session contract; serialized per Appendix A.
- **Regression threshold.** The §3.5.2 +5% per-PR boundary or the
  §3.5.6 +10% absolute-threshold guard against the milestone baseline.
- **Milestone-baseline drift.** The delta of the current baseline
  vs. the milestone-baseline at the last Stage transition; gated by
  the §3.5.6 absolute-threshold guard.
- **Optimization ladder rung.** One of the five mandatory steps in
  §3.4.1: Measure, Attribute, Fix, Verify, Lock.
- **Headroom multiplier.** A dimensionless `[GT]` factor (typically
  1.2× to 1.5×) reserved by each per-spec §6 to absorb measurement
  variance and worst-case fluctuation.
- **Trace channel.** A named instrumentation surface declared in the
  §3.8.2 channel registry; emits records conforming to #16 §3.2.4.1.
- **Channel-to-sink routing.** The §3.8.2 mapping from a trace channel
  to its destination: in-memory ring buffer, file sink, or network
  sink.
- **Verbosity tier.** One of `minimal`, `standard`, `debug`,
  `exhaustive` per §3.8.2 / FR-PO-055.
- **Anchor baseline.** A Stage 0 baseline captured against synthetic
  harness in `tools/perf-harness/` (per FR-PO-072); marked "anchor /
  Stage 0" and never cited as a gameplay baseline.
- **Emission-veto.** Spec #16's authority over trace points emitted
  inside the canonical tick pipeline at #16 §3.1.2; cited by §3.8.3
  and enforced by §5.7.

---

## Version History

| Version | Date         | Author      | Notes |
|---------|--------------|-------------|-------|
| 0.3     | May 14, 2026 | Claude Code | PASS-2 adversarial-review fix pass (`ERR-018-012`, `ERR-018-014`). Appendix F.0 de-duplicated — the second `### F.0 Channel Registry Schema` section (introduced by PR #60 merge collision) was removed; the canonical 13-field schema is retained; `perf.budget` / `perf.alloc` / `perf.trace` anchor rows from the duplicate were grafted in as Stage 0 illustrative entries so F.1 … F.5 dashboard data-source citations resolve at draft time. Duplicate v0.2 version-history row consolidated. |
| 0.1     | May 13, 2026 | Claude Code | Initial draft from `outline-detailed.md` v1.1 Appendices block. Appendix A (baseline record format with #16 §3.2.4.1 binding per KD-11), Appendix B (per-spec §6 schema paste-ready template), Appendix C (roll-up table headers + Shot Mechanics #6 row populated; remaining cells `_TBD_` per Appendix D survey scope), Appendix D (survey headers; row contents Stage 0+1 deliverable per §9.2), Appendix E (Stage-0 local-runbook shell-script outline), Appendix F (dashboard schema catalogue for §3.8.6 dashboards), Appendix G (spec-specific glossary). All #16 / #19 citations tagged. |
| 0.2     | May 14, 2026 | Claude Code | PASS-1 adversarial-review fix pass (`ERR-018-002` / 005 / 010). Appendix B exemption clause rewritten — `[HotPathAllocExempt]` cites Spec #18 §3.7.5 (no longer cites Spec #20 §3). New **Appendix F.0 Channel Registry Schema** authored before F.1 (13 fields: channel name, owning subsystem, default verbosity, sampling rule, sample_n, sink routing, determinism class, inside-tick-pipeline flag, sign-off log ref, record-format version, owner, created date, version history); F-header rewritten to reflect F.0 Stage 0 schema deliverable + F.1 … F.5 Stage 1 populated rows. Appendix F.5 inline `[GT]` tag appended to "> 1%" flake-rate threshold. Also: Appendix G emission-veto entry #16 §3.1→§3.1.2; Appendix A #16 §4→§4.8 EnvironmentFingerprint. |
