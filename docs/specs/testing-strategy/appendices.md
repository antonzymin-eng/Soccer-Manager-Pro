# Testing Strategy & Framework Specification #19 — Appendices

**Created:** May 12, 2026
**Last Updated:** May 12, 2026
**Purpose:** Paste-ready schemas, exemplar property catalogue, per-spec
§5 schema template, approved-spec §5 survey (schema only at #19
approval), local runbook, and glossary.

**Version History:**

| Version | Date         | Author      | Notes |
|---------|--------------|-------------|-------|
| 0.1     | May 12, 2026 | Claude Code | Initial draft from `outline-detailed.md` v1.1. Appendices A–F populated. |
| 0.2     | May 12, 2026 | Claude Code | Self-critique sweep. #16 §5 → §3.2.4.1 (canonical schema, A.1 / A.3 / glossary); #16 §1.3 → §1.1.1 (tier classification, A.1 / C template); #16 §4 → §4.8 (env fingerprint, A.3); #16 §7 → §5 (regression-suite glossary). L4 boundary-saturation / fatigue properties tightened to cite CLAUDE.md instead of restating values. L7 Appendix D `(reserved)` → `(to be assigned at survey time)`. |

---

## Appendix A — Scenario / Fixture Manifest Schema

JSON-schema-style declaration. Binding to #16 §3.2.4.1
`[TBD-NORMATIVE]` (`SerializeCanonical` normative byte-level schema)
per KD-10. Final extension and on-disk encoding pinned at Stage 0+1.

### A.1 Scenario Manifest Entry

```jsonc
{
  "name": "<kebab-case-string>",                 // required, unique within manifest
  "owning_spec_ids": [<int>, ...],               // required, ≥ 1 (per-spec) or ≥ 2 (cross-spec)
  "seed": <uint64>,                              // required, recorded verbatim (FR-TS-025)
  "expected_outcome_envelope": {                  // required, no "implicit pass" (FR-TS-030)
    "predicates": [
      { "field": "<dotted-path>",
        "op": "in_range | equals | within_tolerance | bitwise_equal",
        "value": <type-dependent>,
        "tolerance": <optional-float> }
      // ...
    ]
  },
  "tier_classification": "A" | "B" | "C",       // per #16 §1.1.1 [TBD-NORMATIVE]
  "fixture_refs": ["tests/data/fixtures/<path>", ...],
  "format_version": <int>,                       // validated by §3.3.4
  "provenance_edges": [                          // optional (FR-TS-074)
    { "kind": "derives_from", "fixture": "<path>" }
  ],
  "metadata": {                                  // optional
    "author": "<string>",
    "creation_date": "<ISO-8601>",
    "notes": "<string>"
  }
}
```

### A.2 Root Manifest (`tests/scenarios/index.<ext>`)

The `<ext>` final pin is deferred to Stage 0+1 (D1 in §7.5); the
illustrative example below uses `.json` syntax.

```jsonc
{
  "schema_version": 1,
  "scenarios": [
    { "name": "...", "manifest_path": "tests/scenarios/<owning-spec>/<name>.<ext>" },
    // ...
  ]
}
```

### A.3 Fixture File Header

Every fixture under `tests/data/fixtures/` carries the header (binary
layout per #16 §3.2.4.1 `[TBD-NORMATIVE]`):

| Offset | Field | Type | Notes |
|--------|-------|------|-------|
| 0 | magic | `uint32` | `0x54534653` ("TSFS"); fixture-format-sentinel |
| 4 | format_version | `uint32` | matches `format_version` in §A.1 |
| 8 | spec_id | `uint32` | capturing-spec ID (FR-TS-071) |
| 12 | seed | `uint64` | source seed (FR-TS-071) |
| 20 | env_fingerprint_len | `uint32` | length of fingerprint string |
| 24 | env_fingerprint | `byte[len]` | verbatim from #16 §4.8 [TBD-NORMATIVE] |
| 24+len | capture_date | `byte[19]` | ISO-8601 YYYY-MM-DDTHH:MM:SS |
| ... | body | `byte[]` | payload conforming to #16 §3.2.4.1 canonical layout |

### A.4 Fixture-Migration Manifest

When `format_version` bumps, a migration script lives at
`tests/data/migrations/v<old>-to-v<new>.{py,cs}` (language pinned at
Stage 0+1). Invocation:

```
tools/fixture-migrate.py --from <old> --to <new> --in <path> --out <path>
```

No silent migration (FR-TS-070); the validator rejects unknown
versions until the migration is explicitly run.

---

## Appendix B — Property-Test Catalogue

Full enumeration of property categories named in §3.4.4 with one
exemplar property per category. Per-property: name, owning spec, tier
classification, expected invariant.

| Property | Category | Owning Spec | Tier | Invariant |
|----------|----------|-------------|------|-----------|
| `prop_ballphysics_kinetic_energy_non_increasing` | Physics invariant | #1 | A | KE after collision ≤ KE before collision (within #16 tolerance row). |
| `prop_collision_no_interpenetration` | Physics invariant | #3 | A | After collision resolution, separation ≥ 0 along contact normal. |
| `prop_decisiontree_state_reachability` | State-machine reachability | #8 | A | Every declared state in the decision tree is reachable from `Root` via a finite input sequence. |
| `prop_passmechanics_intent_envelope_well_formed` | State-machine reachability | #5 | A | Every intent parameter is within its declared range; out-of-range inputs deterministically reject. |
| `prop_savestate_snapshot_load_idempotent` | Idempotence | #16 (consumed) | A | `Snapshot(s); Load; Snapshot(s')` ⇒ `s == s'` bitwise. |
| `prop_agentmovement_path_idempotent` | Idempotence | #2 | A | Re-issuing the same move command at the same tick is a no-op. |
| `prop_perception_event_aggregation_commutative` | Commutativity / associativity | #7 | B | Aggregating perception events in different orders yields equivalent observed-state hashes within tolerance row. |
| `prop_ballphysics_boundary_saturation` | Boundary saturation | #1 | A | Ball position at the X / Y / Z bounds of the CLAUDE.md coordinate convention produces no NaN / Infinity in the subsequent step. |
| `prop_fatigue_monotonic_no_recovery` | Monotonicity | #2 | B | Across a match, in absence of recovery events, fatigue is non-decreasing per the CLAUDE.md fatigue convention. |
| `prop_shotmechanics_intent_envelope_bounded` | Boundary saturation | #6 | A | Shot velocity intent within declared range never produces a goal-detection NaN. |
| `prop_firsttouch_outcome_in_envelope` | Boundary saturation | #4 | A | First-touch outcome stays within the spec-declared error envelope across the full intent grid. |

Property names follow §3.1.4 (`prop_<layer>_<system>_<property>`).
Property authors add new properties by appending to this table and
recording the owning-spec §5 entry per FR-TS-047.

---

## Appendix C — Per-Spec §5 Schema Template

Paste-ready Markdown template every per-spec §5 must conform to
(FR-TS-046 … 052). This is the artifact KD-6 + KD-4 mandate.

```markdown
# Spec #<NN> — Section 5: Test Plan

## 5.1 Test Count by Taxonomy Layer

| Layer | Count | Notes |
|-------|-------|-------|
| Unit | <int> | |
| Integration | <int> | |
| Simulation | <int> | |
| Determinism (consumed from #16 §5) | <int or "—"> | Owned by #16 |
| End-to-end / soak | <int> | |

(Pyramid-contract check per Spec #19 §3.1.2.)

## 5.2 Property Test List

| Property | Tier (A/B/C) | Owning Module |
|----------|--------------|----------------|
| `prop_<...>` | <A/B/C> | <module> |

## 5.3 Scenario List

| Scenario | Manifest Path | Tier |
|----------|---------------|------|
| <name> | `tests/scenarios/<owning-spec>/<name>.json` | <A/B/C> |

## 5.4 Coverage Targets (Per Tier per KD-9)

| Tier | Line | Branch |
|------|------|--------|
| A | ≥ 98% | ≥ 95% |
| B | ≥ 90% | ≥ 80% |
| C | lint-only | — |

## 5.5 Determinism-Tier Classification of Authoritative Fields

| Field | Tier | Source (#16 §1.1.1) |
|-------|------|---------------------|
| <fully-qualified-name> | <A/B/C> | #16 §1.1.1 row <name> |

## 5.6 Approval-Checklist Linkage

| Test ID | Verifies §9 Row |
|---------|------------------|
| `<test_id>` | §9.<x>.<y> |

## 5.7 Version History
```

### C.1 Checklist-Auditor Output Format

`tools/checklist-auditor.py` emits the following structured report
(consumed by CI):

```jsonc
{
  "spec_id": <int>,
  "spec_path": "docs/specs/<folder>/",
  "audit_date": "<ISO-8601>",
  "rows": [
    {
      "row_id": "§9.1.<N>",
      "claim": "<string>",
      "evidence": "<file-path or check-name>",
      "status": "RESOLVED" | "BLOCK",
      "details": "<optional resolution diagnostic>"
    }
  ],
  "summary": {
    "total": <int>,
    "resolved": <int>,
    "blocked": <int>
  }
}
```

Exit non-zero if `summary.blocked > 0`.

---

## Appendix D — Approved-Spec §5 Survey

> **Scope at #19 approval (M3 in `outline-detailed.md`).** Appendix D
> ships at #19 approval with the schema and the table headers populated
> below. **Row contents are a Stage 0+1 deliverable** (§7.2); the
> survey itself is *not* a #19 approval gate. KD-6 dilution remains
> *visible* via the empty rows even before the survey is filled in.
> Stage 1 trigger for actual per-spec revisions is unchanged.

| Spec ID | Spec Title | Schema-Conforming Y/N | Missing Fields | Remediation Ticket |
|---------|------------|------------------------|----------------|---------------------|
| #1 | Ball Physics | _TBD_ | _TBD_ | `ERR-019-NNN` (to be assigned at survey time) |
| #2 | Agent Movement | _TBD_ | _TBD_ | `ERR-019-NNN` (to be assigned at survey time) |
| #3 | Collision System | _TBD_ | _TBD_ | `ERR-019-NNN` (to be assigned at survey time) |
| #4 | First Touch | _TBD_ | _TBD_ | `ERR-019-NNN` (to be assigned at survey time) |
| #5 | Pass Mechanics | _TBD_ | _TBD_ | `ERR-019-NNN` (to be assigned at survey time) |
| #6 | Shot Mechanics | _TBD_ | _TBD_ | `ERR-019-NNN` (to be assigned at survey time) |
| #7 | Perception System | _TBD_ | _TBD_ | `ERR-019-NNN` (to be assigned at survey time) |
| #8 | Decision Tree | _TBD_ | _TBD_ | `ERR-019-NNN` (to be assigned at survey time) |

The `ERR-019-NNN` namespace is **not pre-reserved** in
`spec-error-log.md`; specific `NNN` digits are allocated at survey
time (Stage 0+1 deliverable per §7.2) so per-spec ID ordering can be
chosen by the surveyor without colliding with unrelated `ERR-019-NNN`
findings that may be filed in the meantime. Population is gated on
per-spec revision cycles per §3.5.4 (KD-4 no-forced-re-open rule).

---

## Appendix E — Stage-0 Local Runbook

Concrete shell-script outline for `tools/run-tests-local.sh`. Stage 0:
no `src/` exists, so the runbook walks `docs/specs/` only.

```bash
#!/usr/bin/env bash
# tools/run-tests-local.sh
# Stage 0: spec-only checks. Extend at Stage 0+1 to invoke src/-side tests.
set -euo pipefail

REPO_ROOT="$(git rev-parse --show-toplevel)"
cd "$REPO_ROOT"

echo "[1/3] Walking SPEC_INDEX.md for spec-folder list..."
# (Stage 0 manual: reviewer cross-checks SPEC_INDEX rows vs docs/specs/<folder>/)

echo "[2/3] Running approval-checklist auditor (§5.3 manual at Stage 0)..."
# Manual walk. At Stage 0+1 replace with:
#   python tools/checklist-auditor.py --root docs/specs/

echo "[3/3] Running per-spec §5 schema-conformance auditor (§5.4 manual at Stage 0)..."
# Manual walk. At Stage 0+1 replace with:
#   python tools/spec5-schema-auditor.py --root docs/specs/

echo "Done. Paste output above into the PR description."
```

The Stage 0+1 extension adds:

- `dotnet test` invocation for `src/<spec>/tests/`.
- `coverlet` invocation for per-tier coverage.
- `python tools/checklist-auditor.py` automated walk.
- `python tools/spec5-schema-auditor.py` automated walk.

---

## Appendix F — Glossary

Spec #19-specific terms only. Determinism / performance terms are
cited from #16 / #18 `[TBD-NORMATIVE]`.

- **Determinism layer.** The test layer owned by #16 §5. Consumed by
  Spec #19 as a required layer; not redefined here.
- **Eviction.** Permanent deletion of a test that has been quarantined
  ≥ 3 times in 90 days (§3.7.4).
- **Fixture.** An on-disk file under `tests/data/fixtures/` consumed
  by a scenario or unit test. Format conforms to #16 §3.2.4.1 (KD-10).
- **Flake.** A test that, on the same revision under the same
  `EnvironmentFingerprint`, produces different pass / fail outcomes
  across two runs (§3.7.1).
- **Golden trace.** A recorded reference output (under
  `tests/data/golden/`) used by Tier A bitwise-equality assertions.
- **KD-N.** Key decision N, declared in §1.3 and cited throughout.
- **Pyramid contract.** The ratio constraint on test-count
  distribution across taxonomy layers (§3.1.2).
- **Quarantine.** Temporary classification (≤ 14 days) of a flaky test
  that allows it to execute without blocking merge (§3.7.3).
- **Scenario.** A scripted multi-system test described by a manifest
  entry (Appendix A.1) and run by `ScenarioRunner.Run` (§3.3.3).
- **Stage-gated.** A rule that is normative content but activates only
  at the Stage 0 → Stage 1 transition (KD-5).
- **`TBD-NORMATIVE`.** Marker on a citation whose authoritative source
  is not yet approved; per KD-2 (#16) and KD-3 (#18). Removal is a
  §9.2 quality-checklist row.
- **Tooling test.** Stage 0 test that exercises tools under `tools/`
  (e.g., the checklist auditor). Conforms to KD-6 but not to §3.1
  pyramid contract (§3.9.4).
