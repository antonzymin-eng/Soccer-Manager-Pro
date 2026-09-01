# A2 Schema and Executable-Semantics Closure Record

**Document Class:** Stage-gate evidence record\
**Status:** OPEN — implemented candidate; review and owner approval pending\
**Version:** 0.3\
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

| # | Condition | State | Evidence |
|---|---|---|---|
| 1 | Eight-category scope map | **Complete** | §2 |
| 2 | Canonical schemas / single control source | **Complete** | §3, §7 |
| 3 | Executable representative fixtures | **Complete** | §4, §7 |
| 4 | Fresh review over pushed current candidate | **Complete** | §8; three recorded rounds, each bound to the tree it reviewed. Carries a reviewer-independence limitation |
| 5 | Every finding terminal | **Complete** | §8; nine findings, all `Blocker` / `Resolved`, in `architecture-governance/review-ledger.json` |
| 6 | Project-owner approval | **PENDING** | Non-delegable. No agent may satisfy this row |
| 7 | Approved candidate landed on A3 base | **PENDING** | Blocked by row 6; must match the approved digest bundle |

Rows 1–5 are agent-satisfiable and are satisfied. Rows 6 and 7 are not, and nothing in this
record should be read as advancing them. **A2 is OPEN. A3 is BLOCKED.**

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
| `python3 -m unittest tools.tests.test_architecture_governance_semantics` | 137 governance fixtures, PASS |
| `python3 -m unittest tools.tests.test_assembly_tier_check` | 8 assembly-tier fixtures, PASS |
| `python3 -m unittest discover -s tools/tests -p 'test_*.py'` | 145 total fixtures, PASS |
| `python3 tools/recurring-defect-lint.py --repo .` | 0 ERROR |
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

## 8. Fresh review record (condition 4) and finding state (condition 5)

### 8.1 Subject identity

The **material review subject** is the frozen contract itself: the ten schema documents, the six
non-ledger seed registries, `tools/architecture-governance/*.py`, and the governance fixture suite.

`review-ledger.json` is **excluded by construction**, per §3.8 — recording the review run must not
recursively invalidate the subject it records. Tracking prose is excluded because it is not the
contract.

| Round | Artifact reviewed | Material subject digest |
|---|---|---|
| 1 | `origin/main` at `e7a3ba13` — the candidate as reported did not exist | `b422ac967703dcd59c70c8e18adb0d5ed9ab8c37276863e4e5e9821f269bcbd2` |
| 2 | `dae398a6` | `5e1dd7fc8811a1b2eba91aa576c5c5222403c0e9fe2042524b17a917e65f439b` |
| 3 | This candidate | `5d4daacd091c2afae57ff00d9c3f99ddff8a3a179654fb21f1f6b06e7fcc0bba` |

Each round binds the tree it actually reviewed; stamping one digest across all three would misreport
rounds 1 and 2. The digests are **mechanically reproducible**, not asserted: the latest round's value
recomputes in `DurableReviewLedgerTests`, which also proves the three are distinct. A later reviewer
verifies this record with `python3 -m unittest tools.tests.test_architecture_governance_semantics`
on any checkout — no trust in this document is required.

### 8.2 Method

Round 3 read the surfaces rounds 1 and 2 had not: exception routing and the §3.6 authority boundary,
property transition and history, review-ledger convergence and append-only behaviour, baseline
transitions, and the new proof-artifact contract. Each was probed adversarially against the executable
semantics rather than read for plausibility — twelve constructed violations, of which eleven were
correctly rejected. Two apparent acceptances were checked against the plan and found correct
(`inactive → strict` is a declared legal transition; equal-specificity rules conflict at resolution,
not at document validation). One was a real defect, recorded below as `A2-R3-001`.

### 8.3 Findings

Nine findings across three rounds, all `Disposition: Blocker` / `Status: Resolved`, recorded in
`docs/tracking/architecture-governance/review-ledger.json` under series `A2-SCHEMA-FREEZE`.

Following the A0 record's rule, the A2 gate is **not a blanket Blocker citation**: each finding's
`requirement_property` names the specific pre-existing condition the defect made false. Per Governance
§1.6 no finding cites a gate this review authored — `A2-R2-002` cites §3.1's freeze rule, which has
governed since plan v0.3, not v0.19's restatement of it. `A2-R2-004` is the one finding whose Blocker
status rests on **project-owner designation** rather than a pre-existing rule, and it is recorded that
way because no live divergence was demonstrated; it is a regression guard.

| ID | Round | Severity | Summary |
|---|---|---|---|
| A2-R1-001 | 1 | Critical | The reported candidate did not exist in the repository |
| A2-R1-002 | 1 | Medium | Shared enum control duplicated between Python and the schemas |
| A2-R1-003 | 1 | Medium | The plan equated landing on `main` with A2 closure |
| A2-R2-001 | 2 | Medium | Review-ledger and baseline validators defaulted to their permissive branch |
| A2-R2-002 | 2 | Medium | `proof-artifact.schema.json` was frozen with no executable counterpart |
| A2-R2-003 | 2 | Medium | No schema declared `$id`, so relative `$ref` had no base URI |
| A2-R2-004 | 2 | Low | Nothing enforced schema/semantics agreement beyond the shared enums |
| A2-R2-005 | 2 | Low | `REFERENCE_SEMANTICS_VERSION` regressed within the branch, undocumented |
| A2-R3-001 | 3 | Medium | A property under an `FR-CS-`/`FR-TS-` id captured that requirement's waiver authority |

### 8.4 Outcome and limitations

No run is marked `CONVERGED` and no run carries `final_review`. That is deliberate and is locked by a
test: convergence is not an agent's to declare while the owner gate is open, and FR-AG-018's fresh
review over the current artifact is a separate question from FR-AG-019/020 convergence.

**Reviewer-independence limitation.** Rounds 2 and 3 were performed by the same assistant that applied
the remediation for the findings they raised. FR-AG-018 requires a fresh review over the current
artifact; it does not require a different reviewer, and the A0 closure review recorded the identical
limitation (`a0-governance-adoption-review.md` §4). **No independence is claimed here.** Project-owner
approval is a separate condition and remains open at row 6.

**Surfaces this review did not exhaustively verify**, recorded rather than implied: a field-by-field
re-derivation of each schema against Governance §3.3 and §7.1, and line-by-line reading of every
validator branch. Both are recorded in the ledger's `unverified_surfaces`.

---

## Version History

| Version | Date | Author | Notes |
|---|---|---|---|
| 0.3 | September 1, 2026 | — | Satisfies the five agent-satisfiable closure conditions and records the evidence. New §8 carries the fresh-review record: per-round material subject digests (mechanically recomputed by `DurableReviewLedgerTests`, not asserted), method, and the nine-finding set now recorded in the durable `review-ledger.json` under series `A2-SCHEMA-FREEZE`, all `Blocker`/`Resolved`. Round 3 found and fixed `A2-R3-001`, an authority-boundary defect letting a property under an `FR-CS-`/`FR-TS-` id capture that requirement's waiver routing. Records the reviewer-independence limitation and the unverified surfaces explicitly. Conditions 6 (owner approval) and 7 (landing) remain **PENDING** and are not agent-satisfiable. Test split 128/8/136 → 137/8/145. A2 remains OPEN; A3 remains BLOCKED. |
| 0.2 | September 1, 2026 | — | Records second-review remediation in new §7: the per-proof artifact validator that closes the last frozen contract without an executable counterpart; fail-closed sentinel defaults on the review-ledger and activation-baseline validators, with `strict_activation` deliberately excluded; canonical `$id` on all ten schemas so relative `$ref` resolves by URI; and the bounded stdlib Draft 2020-12 validator behind a one-directional schema/semantics differential that raises on any unimplemented keyword. Restores `REFERENCE_SEMANTICS_VERSION` 2.0.0 by owner decision. Test split 104/8/112 → 128/8/136. A2 remains OPEN; A3 remains BLOCKED. |
| 0.1 | September 1, 2026 | — | Creates the explicit A2 closure gate record after the first review found the candidate unpushed, shared enum control duplicated, and A2 completion undefined. Records the eight-category/ten-schema/seven-state-file mapping, pure-stdlib single-source design, exact test split, and pending non-delegable approval. A2 OPEN; A3 BLOCKED. |
