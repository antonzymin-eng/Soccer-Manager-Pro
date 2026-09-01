# A2 Schema and Executable-Semantics Closure Record

**Document Class:** Stage-gate evidence record\
**Status:** OPEN — implemented candidate; review and owner approval pending\
**Version:** 0.2\
**Created:** September 1, 2026\
**Owning plan:** `docs/planning/project-architecture-governance-integration-plan.md` §11 A2\
**Candidate branch:** `codex/a2-complete-schema-freeze`\
**Base:** `origin/main` at `e7a3ba13`

---

## 1. Gate state

This record implements the seven-condition A2 closure gate added by integration-plan v0.18 and
strengthened by v0.19.
Implementation, merge, review, approval, and closure are distinct. A2 remains **OPEN** and A3 remains
**BLOCKED** until every row below is complete against the same subject-digest bundle.

| Condition | State | Evidence |
|---|---|---|
| Eight-category scope map | Complete | §2 |
| Canonical schemas / single control source | Candidate verified on the pushed branch | §3; requires fresh remote review |
| Executable representative fixtures | Candidate verified on the pushed branch | §4, §7; requires fresh remote review |
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
| `python3 -m unittest tools.tests.test_architecture_governance_semantics` | 128 governance fixtures, PASS |
| `python3 -m unittest tools.tests.test_assembly_tier_check` | 8 assembly-tier fixtures, PASS |
| `python3 -m unittest discover -s tools/tests -p 'test_*.py'` | 136 total fixtures, PASS |
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

## 7. Second-review remediation (v0.2)

The second review accepted the first round of corrections and raised five further findings. All five
are addressed on this branch; three were treated as closure blockers.

| # | Finding | Disposition |
|---|---|---|
| 1 | Review-ledger and baseline validators defaulted to their permissive branch while the property registry deliberately failed closed | **Blocker — fixed.** `prior_ledger` and `prior_baseline` now use the `_NOT_PROVIDED` sentinel; an omitted trusted prior, an omitted live violation set, or a missing current digest for a `final_review` run raises `ReviewStateUncertainty` / `ActivationBaselineUncertainty` in strict mode. `None` remains reserved for the positive claim that no prior existed. `strict_activation` is deliberately unchanged: it adds a requirement rather than relaxing one, so defaulting it on would wrongly demand final activation of every caller. |
| 2 | `proof-artifact.schema.json` was frozen with no executable counterpart | **Blocker — fixed.** `validate_proof_artifact` mirrors the frozen shape and binds it to A2 execution truth: a `pass` result cannot outrun a non-passing execution, and a `bounded` result converts only the states `evaluate_execution_truth` permits, through a substitute #19 explicitly allows. Failure-injection and mutation records are bound to their proof class in both directions, and target selectors resolve against supplied semantic facts. A5/A6 productionize this; A2 owns the frozen shape, per §3.1's rule that the executable demonstration precedes the freeze. |
| 3 | No schema declared `$id`, so 111+ relative `$ref`s had no base URI outside file loading | **Fixed.** Every schema pins `https://schemas.tactical-director.internal/architecture-governance/<name>`. The namespace is deliberately non-dereferenceable and under a reserved TLD: `$id` must be stable and resolvable as a URI, not fetchable, and a repo-internal contract set should never imply a published endpoint. |
| 4 | Schema and Python were two independent structural descriptions with no agreement check | **Blocker — fixed.** `tools/architecture-governance/schema_validator.py` is a bounded Draft 2020-12 validator over exactly the keywords these ten schemas use. It **raises `UnsupportedKeyword` on any keyword it does not implement**, and a test asserts its coverage set against the keywords actually present — without that guard a silently skipped keyword would make every differential pass vacuously. The assertion is deliberately **one-directional**: every fixture the semantics accept must also satisfy its frozen schema. The converse is false by design, because the semantics enforce cross-record rules (append-only history, legal transitions, dependency closure, Disposition×Status) that JSON Schema cannot express. |
| 5 | `REFERENCE_SEMANTICS_VERSION` regressed 1.9.0 → 2.0.0 → 1.10.0, undocumented | **Fixed — 2.0.0 restored by owner decision.** The module now raises at import without `common.schema.json`; that mandatory external dependency breaks any standalone import contract and is a major change. Integration plan v0.19 carries the rationale and annotates the superseded v0.18 claim in place. The value is stamped into every proof snapshot and compared by equality in `assess_proof_freshness`, so the restore is mechanically inert — it is a signalling correction, and it also removes the published regression. |

Findings 1, 2 and 4 were treated as closure blockers because each undercuts closure condition 3
directly. Finding 4 is a **regression guard, not a defect fix**: the review probed `uniqueItems`,
`minimum`, conditional `not`/`required` prohibitions and the seed and fixture corpus, and the two
descriptions agreed on every case. What was missing was anything preventing future drift, which the
differential now supplies — proven live by injecting a schema-only `required` field and observing the
new test fail.

---

## Version History

| Version | Date | Author | Notes |
|---|---|---|---|
| 0.2 | September 1, 2026 | — | Records second-review remediation in new §7: the per-proof artifact validator that closes the last frozen contract without an executable counterpart; fail-closed sentinel defaults on the review-ledger and activation-baseline validators, with `strict_activation` deliberately excluded; canonical `$id` on all ten schemas so relative `$ref` resolves by URI; and the bounded stdlib Draft 2020-12 validator behind a one-directional schema/semantics differential that raises on any unimplemented keyword. Restores `REFERENCE_SEMANTICS_VERSION` 2.0.0 by owner decision. Test split 104/8/112 → 128/8/136. A2 remains OPEN; A3 remains BLOCKED. |
| 0.1 | September 1, 2026 | — | Creates the explicit A2 closure gate record after the first review found the candidate unpushed, shared enum control duplicated, and A2 completion undefined. Records the eight-category/ten-schema/seven-state-file mapping, pure-stdlib single-source design, exact test split, and pending non-delegable approval. A2 OPEN; A3 BLOCKED. |
