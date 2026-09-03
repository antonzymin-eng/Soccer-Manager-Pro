# Testing Strategy & Framework Specification #19 — Appendices

**Created:** May 12, 2026
**Last Updated:** September 3, 2026
**Version:** 0.3
**Status:** AMENDMENT DRAFT (A3.2a; May 15, 2026 approved baseline remains in force)
**Amendment plan:** `docs/planning/project-architecture-governance-integration-plan.md` v0.37, §7; A3.2a
**Purpose:** Paste-ready schemas, exemplar property catalogue, per-spec
§5 schema template, approved-spec §5 survey, local runbook, glossary,
and canonical architecture-proof contract/examples.

**Version History:**

| Version | Date         | Author      | Notes |
|---------|--------------|-------------|-------|
| 0.1     | May 12, 2026 | Claude Code | Initial draft from `outline-detailed.md` v1.1. Appendices A–F populated. |
| 0.2     | May 12, 2026 | Claude Code | Self-critique sweep. #16 §5 → §3.2.4.1 (canonical schema, A.1 / A.3 / glossary); #16 §1.3 → §1.1.1 (tier classification, A.1 / C template); #16 §4 → §4.8 (env fingerprint, A.3); #16 §7 → §5 (regression-suite glossary). L4 boundary-saturation / fatigue properties tightened to cite CLAUDE.md instead of restating values. L7 Appendix D `(reserved)` → `(to be assigned at survey time)`. |
| 0.3     | September 2, 2026 | Codex | **A3.2a governance amendment draft.** Adds Appendix G: the A2 proof-artifact/closure/execution contract, material-subject versus provenance examples, precise freshness cases, bounded/N/A limits, and schema-shaped failure-injection/mutation records. These examples are illustrative; canonical schemas and reference semantics v2.1.0 remain authoritative. A3.4 reapproval remains required. |

---

## Appendix A — Scenario / Fixture Manifest Schema

JSON-schema-style declaration. Binding to #16 §3.2.4.1
(`SerializeCanonical` normative byte-level schema) per KD-10. Final
extension and on-disk encoding pinned at Stage 0+1.

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
  "tier_classification": "A" | "B" | "C",       // per #16 §1.1.1
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
layout per #16 §3.2.4.1):

| Offset | Field | Type | Notes |
|--------|-------|------|-------|
| 0 | magic | `uint32` | `0x54534653` ("TSFS"); fixture-format-sentinel |
| 4 | format_version | `uint32` | matches `format_version` in §A.1 |
| 8 | spec_id | `uint32` | capturing-spec ID (FR-TS-071) |
| 12 | seed | `uint64` | source seed (FR-TS-071) |
| 20 | env_fingerprint_len | `uint32` | length of fingerprint string |
| 24 | env_fingerprint | `byte[len]` | verbatim from #16 §4.8 |
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
cited from #16 / #18.

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


---

## Appendix G — Architecture Proof Artifact & Closure Contract

Appendix G explains §3.11 and FR-TS-086–097. It is **not** a second
schema and does not supersede A2. Machine validation uses the canonical
Draft 2020-12 schemas under
`docs/tracking/architecture-governance/schemas/` and
`tools/architecture-governance/reference_semantics.py` version 2.1.0.

### G.1 Canonical Authorities

The relevant frozen A2 authorities are:

| Concern | Canonical authority |
|---|---|
| Shared enums, selector-v1, execution states, proof classes, change types | `docs/tracking/architecture-governance/schemas/common.schema.json` |
| Applicability-rule shape | `docs/tracking/architecture-governance/schemas/applicability-rules.schema.json` |
| Reusable proof-artifact shape | `docs/tracking/architecture-governance/schemas/proof-artifact.schema.json` |
| Applicability, closure, freshness and execution truth | `tools/architecture-governance/reference_semantics.py` v2.1.0 |

If an example below ever disagrees with those authorities, the example is
wrong and MUST be corrected; it does not amend the machine contract.

### G.2 Proof Artifact Field Contract

A reusable proof artifact requires:

- schema_version;
- proof_id and one canonical proof_class;
- requirement_property_refs and applicability_rule_ids;
- result: pass, fail, na, or bounded;
- subject_scope_digest;
- dependency_closure with dependency_ids, typed edges,
  relation_policy_digest, and current change_type;
- content_fingerprints and configuration_fingerprints;
- one or more tool_identities;
- execution_records (which may be empty only when applicability does not
  itself require execution);
- created metadata and revalidation_history.

Optional provenance_revision and provenance_tree identify where the record
was created/stored. They are **not** material freshness keys.

A result of na requires exactly one approved N/A limitation record. A result
of bounded requires exactly one approved bounded-substitute record. Neither
record may accompany a different result.

### G.3 Proof-Class Closure

| Proof class | Required relation groups |
|---|---|
| structural-reachability | common + structural |
| lifecycle-order | common + structural + lifecycle |
| failure-injection | common + structural + lifecycle + executable |
| mutation | common + structural + lifecycle + executable |

For any class, persistence relations (serializer, schema, resource) join the
closure only when the current applicability subject's change_type is
persistence-boundary or external-resource-dependency.

The closure starts from the applicability-resolved requirement/property
nodes and follows the allowed relations. This makes transitive dependencies
part of the proof even when an artifact author did not list them manually.
An author MAY widen a dependency declaration; the author cannot narrow the
mechanically derived closure.

### G.4 Schema-Shaped Reusable Pass Artifact

This record is illustrative only. The hexadecimal fingerprints are synthetic,
so it is **not reusable project evidence**. The important identity property is
that provenance_tree is merely provenance while subject_scope_digest binds
the material proof subject. The execution record carries the **same**
subject_scope_digest as the proof.

~~~json
{
  "schema_version": "1.0.0",
  "proof_id": "PROOF-EXAMPLE-STRUCTURAL-001",
  "proof_class": "structural-reachability",
  "requirement_property_refs": ["FR-CS-075", "AP-EXAMPLE-001"],
  "applicability_rule_ids": ["AR-EXAMPLE-RUNTIME-SERVICE"],
  "result": "pass",
  "subject_scope_digest": "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
  "provenance_revision": "example-producing-revision",
  "provenance_tree": "tree-that-contains-this-proof-record",
  "dependency_closure": {
    "dependency_ids": [
      "requirement:FR-CS-075",
      "contract:example-service",
      "root:example-host",
      "symbol:example-registration"
    ],
    "edges": [
      {
        "source": "requirement:FR-CS-075",
        "target": "contract:example-service",
        "relation": "contract"
      },
      {
        "source": "contract:example-service",
        "target": "root:example-host",
        "relation": "root"
      },
      {
        "source": "root:example-host",
        "target": "symbol:example-registration",
        "relation": "registration"
      }
    ],
    "relation_policy_digest": "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb",
    "change_type": "new-runtime-service"
  },
  "content_fingerprints": {
    "requirement:FR-CS-075": "cccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccc",
    "contract:example-service": "dddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddd",
    "root:example-host": "eeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeee",
    "symbol:example-registration": "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff"
  },
  "inventory_digest": "1111111111111111111111111111111111111111111111111111111111111111",
  "asmdef_digest": "2222222222222222222222222222222222222222222222222222222222222222",
  "configuration_fingerprints": {
    "config:example-host": "3333333333333333333333333333333333333333333333333333333333333333"
  },
  "tool_identities": [
    {
      "tool_id": "architecture-governance-reference",
      "semantic_version": "2.1.0",
      "content_digest": "4444444444444444444444444444444444444444444444444444444444444444"
    }
  ],
  "execution_records": [
    {
      "execution_id": "EXEC-EXAMPLE-001",
      "command_or_test": "example structural proof command",
      "runner": "example-runner",
      "environment": "example-environment",
      "subject_scope_digest": "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
      "execution_state": "passed",
      "started_at": "2026-09-02T18:00:00-07:00",
      "ended_at": "2026-09-02T18:00:01-07:00",
      "result_artifact": "example-result.json"
    }
  ],
  "created": {
    "actor": "example-agent",
    "at": "2026-09-02T18:00:02-07:00"
  },
  "revalidation_history": []
}
~~~

The example intentionally does not claim that the displayed synthetic
subject digest can be recomputed from the prose/example nodes. Production
evidence obtains that digest from the canonical resolver.

### G.5 Freshness Examples

**Case A — committed record does not invalidate itself.** The proof above is
committed in tree T2 after being produced from material subject S. If the
containing tree changes from T1 to T2 solely because the proof record was
added, the material closure for S is unchanged. Recomputed
subject_scope_digest remains equal, so the proof remains fresh. Provenance
may be updated or retained as history; it is not recursively part of S.

**Case B — unrelated change remains current.** A mapped documentation or
code dependency changes, but that dependency is outside the proof's derived
dependency_ids and the current applicability/closure fingerprint is otherwise
unchanged. The changed-files decision is proven-non-impact and the proof MAY
remain current.

**Case C — transitive dependency change stales proof.** The example host's
registration symbol depends on a configuration/extractor/structural node in
the derived closure, and that node's fingerprint or topology changes. Even
if the proof file itself is untouched, the current subject digest changes.
The proof is stale and the applicable proof MUST be regenerated or
revalidated.

**Case D — new applicable root stales proof.** Compiler/discovery facts add a
new runtime root that falls within the same resolved obligation. The derived
dependency set changes, so the old structural proof no longer establishes
complete coverage.

### G.6 Execution-State Truth Table

| State | Required execution satisfied? | Bounded substitute |
|---|---:|---|
| passed | Yes | Forbidden alongside pass |
| failed | No | Cannot satisfy |
| skipped | No | Cannot satisfy |
| excluded | No by itself | MAY satisfy only when the exact obligation explicitly permits FR-TS-096 |
| unavailable | No by itself | MAY satisfy only when the exact obligation explicitly permits FR-TS-096 |
| not-run | No by itself | MAY satisfy only when the exact obligation explicitly permits FR-TS-096 |
| runner-failed | No | Cannot satisfy |

A runner reporting **skipped** is not the same as a deliberate **excluded**
state. A normal framework skip, ignore attribute, quarantine skip, unsupported
assembly skip, conditional job skip, or equivalent observed skip remains
unsatisfied and cannot be relabelled after the fact to obtain bounded
satisfaction.

Example: a required proof execution reports skipped:

~~~json
{
  "execution_id": "EXEC-EXAMPLE-SKIPPED",
  "command_or_test": "Example.Tests.RequiredArchitectureProof",
  "runner": "example-runner",
  "environment": "example-environment",
  "subject_scope_digest": "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
  "execution_state": "skipped",
  "started_at": "2026-09-02T18:10:00-07:00",
  "ended_at": "2026-09-02T18:10:00-07:00"
}
~~~

That execution **does not satisfy** a required obligation, with or without a
bounded substitute.

A deliberately excluded execution is also unsatisfied by default:

~~~json
{
  "execution_id": "EXEC-EXAMPLE-EXCLUDED",
  "command_or_test": "Example.Tests.RequiredArchitectureProof",
  "runner": "example-runner",
  "environment": "example-environment",
  "subject_scope_digest": "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
  "execution_state": "excluded",
  "started_at": "2026-09-02T18:11:00-07:00",
  "ended_at": "2026-09-02T18:11:00-07:00"
}
~~~

It can become satisfied only if the exact obligation explicitly permits a
bounded substitute and the artifact result is bounded with a complete
approved record such as:

~~~json
{
  "authority_ref": "FR-TS-096",
  "approval_ref": "APPROVAL-EXAMPLE-001",
  "justification": "Exact proof is unavailable in the certified runner for this bounded surface.",
  "omitted_surface_or_uncertainty": "Alternate-host execution remains unobserved; structural equivalence only is established."
}
~~~

This record bounds uncertainty; it does not erase the omitted surface and
does not constitute a Governance FR-AG-026 surface exclusion.

### G.7 N/A Is Applicability, Not Execution Success

A proof result of na uses the same four-field approved-limitation shape, but
only after the matched applicability rule permits the exact N/A reason and
any required approval exists. It means the proof obligation is not applicable
for that resolved trigger/surface; it does not mean a required execution ran
successfully.

A lifecycle phase that genuinely does not exist may be represented by the
Spec #20 declaration/N/A contract. A lifecycle phase that exists but was
skipped by the runner cannot be converted to N/A.

### G.8 Failure-Injection Record

A failure-injection proof additionally carries the exact perturbation identity:

~~~json
{
  "failure_injection": {
    "condition_or_input": "registration dependency unavailable",
    "target_selector": {
      "assembly": "Example.Runtime",
      "kind": "method",
      "containing_type_id": "Example.MatchHost",
      "member_name": "Activate",
      "parameter_type_ids": [],
      "generic_arity": 0,
      "is_static": false
    },
    "expected_path": "activation rejects startup and emits the owned failure signal",
    "executed_command_or_test": "Example.Tests.ActivationDependencyUnavailable",
    "observed_result": "expected rejection and failure signal observed",
    "tool_environment_identity": "example-runner / example-environment"
  }
}
~~~

The selector must resolve when semantic facts are supplied. Recording a
condition against the wrong overload or unresolved target is not valid proof.

### G.9 Mutation Record

A mutation proof additionally carries the exact mutant and restoration record:

~~~json
{
  "mutation": {
    "base_subject_digest": "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
    "target_selector": {
      "assembly": "Example.Runtime",
      "kind": "method",
      "containing_type_id": "Example.MatchHost",
      "member_name": "Activate",
      "parameter_type_ids": [],
      "generic_arity": 0,
      "is_static": false
    },
    "operator_or_mutant_digest": "remove-required-registration / mutant-example-001",
    "baseline_execution": "EXEC-BASELINE-001 passed",
    "mutant_execution": "EXEC-MUTANT-001 failed",
    "expected_detector": "Example.Tests.RequiredRegistrationIsReachable",
    "observed_detector_failure": "required-registration assertion failed",
    "tool_identity": "targeted-governance-mutation-example",
    "restoration_clean_state": true
  }
}
~~~

A no-op/equivalent mutant, wrong target, surviving expected detector, or
unrestored working state cannot satisfy FR-TS-091.

### G.10 Gate Consumption Boundary

The architecture/evidence gate consumes the applicability result, current
derived closure, proof artifact, exact execution results, and Governance
convergence state. It does not create new architectural properties and does
not infer convergence from severity.

A proof example in this appendix is never evidence merely because it is
schema-shaped. Real merge-blocking evidence must be produced from the current
repository, validated by the canonical A2 semantics, and meet the activation
conditions defined by the approved A3/A4/A8 integration stages.
