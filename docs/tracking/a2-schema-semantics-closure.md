# A2 Schema and Executable-Semantics Closure Record

**Document Class:** Stage-gate evidence record\
**Status:** OPEN — implemented candidate; review and owner approval pending\
**Version:** 0.1\
**Created:** September 1, 2026\
**Owning plan:** `docs/planning/project-architecture-governance-integration-plan.md` §11 A2\
**Candidate branch:** `codex/a2-complete-schema-freeze`\
**Base:** `origin/main` at `e7a3ba13`

---

## 1. Gate state

This record implements the seven-condition A2 closure gate added by integration-plan v0.18.
Implementation, merge, review, approval, and closure are distinct. A2 remains **OPEN** and A3 remains
**BLOCKED** until every row below is complete against the same subject-digest bundle.

| Condition | State | Evidence |
|---|---|---|
| Eight-category scope map | Complete | §2 |
| Canonical schemas / single control source | Candidate verified locally | §3; requires fresh remote review |
| Executable representative fixtures | Candidate verified locally | §4; requires fresh remote review |
| Fresh review over pushed current candidate | Pending | Must review the remotely visible current branch head |
| Every finding terminal | Pending | Owned by the fresh review |
| Project-owner approval | Pending | Non-delegable |
| Approved candidate landed on A3 base | Pending | Must match the approved digest bundle |

## 2. Eight-category scope map

The §3.1 count is eight machine-contract categories. It does not imply eight empty registries.

| §3.1 category | Canonical schema | Durable state artifact |
|---|---|---|
| Runtime-surface classification | `schemas/runtime-surface-classifications.schema.json` | `runtime-surface-classifications.json` |
| Applicability resolution | `schemas/applicability-rules.schema.json` | `applicability-rules.json` |
| Integration contracts | `schemas/integration-contracts.schema.json` | `integration-contracts.json` |
| Property records | `schemas/property-registry.schema.json` | `property-registry.json` |
| Governance exceptions | `schemas/exceptions.schema.json` | `exceptions.json` |
| Reusable proof artifacts | `schemas/proof-artifact.schema.json` | Per-proof records only; no meaningless empty registry |
| Adversarial-review state | `schemas/review-ledger.schema.json` | `review-ledger.json` |
| Temporary activation baseline | `schemas/temporary-activation-baseline.schema.json` | `temporary-activation-baseline.json` |

Two schema documents sit outside that eight-category count:

- `schemas/common.schema.json` — the single machine source for shared enums, transition maps,
  fallback maps, and dependency-relation groups;
- `schemas/bootstrap-runtime-surfaces.schema.json` — the finite A4-only auxiliary for runtime intent
  the compiler cannot infer. A2 does not create a live bootstrap artifact.

Therefore the candidate intentionally contains **ten schemas and seven seed state artifacts**.

## 3. IP-5 single-source verification

`common.schema.json` owns the shared control values. `reference_semantics.py` loads those values with
stdlib `json` and `pathlib`; it does not carry independent enum copies. Domain schemas reference the
common definitions rather than restating them. Import-time consistency checks reject malformed transition,
Disposition×Status, fallback, and dependency-relation group data.

The governance suite additionally verifies:

- every non-common schema contains no independent `enum` declaration;
- selector discriminator branches cover exactly the canonical selector-kind enum;
- every schema `$ref` resolves inside the committed schema set; and
- the reference module imports only `hashlib`, `json`, `math`, and `pathlib`.

No `jsonschema` or other third-party dependency is introduced.

## 4. Candidate verification

The current local candidate must be re-run and recorded by the fresh review after the branch is pushed.
The expected split is explicit so an aggregate cannot hide missing discovery:

| Command | Expected result |
|---|---|
| `python3 -m unittest tools.tests.test_architecture_governance_semantics` | 104 governance fixtures, PASS |
| `python3 -m unittest tools.tests.test_assembly_tier_check` | 8 assembly-tier fixtures, PASS |
| `python3 -m unittest discover -s tools/tests -p 'test_*.py'` | 112 total fixtures, PASS |
| `python3 tools/assembly-tier-check.py --repo .` | PASS |
| `python3 tools/doc-consistency-check.py --repo .` | PASS |
| JSON parse + `$ref` resolution over all canonical schemas/seeds | PASS |
| `python3 -m py_compile` over the reference module and suite | PASS |
| `git diff --check` | PASS |

## 5. Pre-review corrections

The first attempted review correctly established that the candidate was local-only and therefore
unreviewable. It also identified two real design gaps before a remote review began:

1. shared enums were duplicated between Python and JSON schemas; corrected by making
   `common.schema.json` the executable source; and
2. the plan equated landing with A2 closure; corrected by integration-plan v0.18's explicit gate.

The test-count and file-count challenges are resolved by §§2 and 4. They remain mandatory checks for
the fresh review rather than relying on this record's assertion.

## 6. Approval and closure

No approval is recorded. An agent MUST NOT change this record to `CLOSED` without explicit project-owner
approval of the exact reviewed subject-digest bundle. A3 MUST NOT begin before that approval, terminal
finding state, matching landing, and closure update.

---

## Version History

| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | September 1, 2026 | — | Creates the explicit A2 closure gate record after the first review found the candidate unpushed, shared enum control duplicated, and A2 completion undefined. Records the eight-category/ten-schema/seven-state-file mapping, pure-stdlib single-source design, exact test split, and pending non-delegable approval. A2 OPEN; A3 BLOCKED. |
