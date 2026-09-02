# A2 Schema and Executable-Semantics Closure Record

**Document Class:** Stage-gate evidence record\
**Status:** OPEN — implemented candidate; review and owner approval pending\
**Version:** 0.10\
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
| 4 | Fresh review over pushed current candidate | **PENDING** | §8. Retracted at v0.4 (`A2-R4-001`) and still open at v0.10: ten rounds are recorded, and round 10 was the independent pass round 9 owed — but the current artifact carries round-10 corrections and no round has reviewed that corrected artifact. `test_closure_condition_4_is_only_claimed_with_a_review_of_this_tree` enforces the link between this cell and the ledger |
| 5 | Every finding terminal | **Complete** | §8; twenty-three findings, all `Blocker` / `Resolved`, in `architecture-governance/review-ledger.json` |
| 6 | Project-owner approval | **PENDING** | Non-delegable. No agent may satisfy this row |
| 7 | Approved candidate landed on A3 base | **PENDING** | Blocked by rows 4 and 6; must match the approved digest bundle |

**Row 4 was claimed at v0.3 and is retracted.** The claim was wrong in a way worth stating
plainly rather than quietly correcting: round 3 reviewed `678f0f2`, the material subject then moved
by 150 lines — the `A2-R3-001` fix, its schema change, its tests — and the commit carrying the
completion claim was itself never reviewed. The gate's pushed-candidate wording is stronger than
FR-AG-018's, and the party satisfying a condition does not get to relax it. Row 4 becomes claimable
only after a fresh review of the artifact as pushed, and `test_the_current_artifact_has_not_yet_been_reviewed`
now fails if a round claims the current tree without one.

Rows 1, 2, 3 and 5 are satisfied. Rows 4, 6 and 7 are not. **A2 is OPEN. A3 is BLOCKED.**

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
| `python3 -m unittest tools.tests.test_architecture_governance_semantics` | 148 governance fixtures, PASS |
| `python3 -m unittest tools.tests.test_recurring_defect_lint` | 9 phantom-stream context fixtures, PASS |
| `python3 -m unittest tools.tests.test_assembly_tier_check` | 8 assembly-tier fixtures, PASS |
| `python3 -m unittest discover -s tools/tests -p 'test_*.py'` | 165 total fixtures, PASS — **0 skipped on full history, 2 skipped under a shallow checkout** |
| `python3 tools/recurring-defect-lint.py --repo .` | 0 ERROR |
| `python3 tools/assembly-tier-check.py --repo .` | PASS |
| `python3 tools/doc-consistency-check.py --repo .` | PASS |
| JSON parse + `$ref` resolution over all canonical schemas/seeds | PASS |
| `python3 -m py_compile` over the reference module and suite | PASS |
| `git diff --check` | PASS |

**Full history and CI are different runs, and a gate line must say which.** `Spec hygiene checks`
uses `actions/checkout@v4` with no `fetch-depth`, so CI runs at depth 1. Exactly two fixtures are
history-dependent — `test_every_recorded_digest_matches_the_revision_it_names` and
`test_status_timestamps_equal_first_publication_commit_time` — and both skip there, naming every
revision they could not reach. That is `A2-R5-001`'s all-or-nothing rule working, not a failure: partial
verification is never presented as complete. But it means **a 0-skipped result is a claim about a
full-history run only.** Reproduce the CI condition with
`git clone --depth 1 file://$PWD <dir> -b <branch>` and expect `165 tests, OK (skipped=2)`; a local
full-history run of the same commit gives `165 tests, OK` with none skipped. Both were run for the
round-10 landing. Neither number is the other's evidence.

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
| 3 | `678f0f2` — corrected at v0.4; v0.3 recorded the post-fix digest for a review performed pre-fix | `77c1d54643b287bf7bd4b0b901e419e4ca02aaf370befe3db6b473ec487e2bd7` |
| 4 | `11547d4` as pushed — independent review | `5d4daacd091c2afae57ff00d9c3f99ddff8a3a179654fb21f1f6b06e7fcc0bba` |
| 5 | `5ebc3f7` as pushed — second independent review | `906ce9559f961f3d4a91cce89ea03cd45c1bc03945093611e8d4ace9f9dd1ad6` |
| 6 | `7d4e949` as pushed — third independent review | `deb9bf31d14d4f89615a6f8d85b78a3ba2e55506f80857371e2f2249ed40d59c` |
| 7 | `c349fb6` as pushed — fourth independent review, landed by the owner as PR #346 | see the ledger |
| 8 | `a034fc3` as pushed — automated review on PR #347 | see the ledger |
| 9 | `c927a95` as pushed — verification pass over the round-8 corrections | see the ledger |
| 10 | `6bce84f` as pushed — independent review of the round-9 remediation | see the ledger |

The current working tree is **not** in this table. That is the point of row 4 being open.

Each round binds the tree it actually reviewed; stamping one digest across all of them would
misreport the earlier rounds. **Every** recorded digest is recomputed from the commit its scope
names — corrected at v0.4 per `A2-R4-002`, which found that v0.3 verified only the latest and merely
asserted the rest were distinct, while claiming more than that. Distinctness is not identity.

The verification is bounded, and the bound is stated rather than glossed: `git` history must be
present. It is **all-or-nothing** — corrected at v0.5 per `A2-R5-001`, which found that v0.4 skipped
unavailable revisions individually and skipped the test only when none resolved, so a shallow checkout
could verify one digest of five and still report a green tick under a name asserting all of them. A
single missing revision now skips the whole check and names what is missing. CI checks out shallow, so
that is the expected path, not an edge case. Where history is present,
`python3 -m unittest tools.tests.test_architecture_governance_semantics` verifies every digest here.

The digests are deliberately **not** asserted to be distinct. Two rounds may legitimately review an
unchanged material subject and correctly carry the same digest; the requirement is that each digest
match its named subject, not that it differ from its neighbours.

### 8.2 Method

Round 3 read the surfaces rounds 1 and 2 had not: exception routing and the §3.6 authority boundary,
property transition and history, review-ledger convergence and append-only behaviour, baseline
transitions, and the new proof-artifact contract. Each was probed adversarially against the executable
semantics rather than read for plausibility — twelve constructed violations, of which eleven were
correctly rejected. Two apparent acceptances were checked against the plan and found correct
(`inactive → strict` is a declared legal transition; equal-specificity rules conflict at resolution,
not at document validation). One was a real defect, recorded below as `A2-R3-001`.

### 8.3 Findings

Twenty-three findings across ten rounds, all `Disposition: Blocker` / `Status: Resolved`, recorded in
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
| A2-R4-001 | 4 | High | Condition 4 was marked complete before any review of the artifact it names |
| A2-R4-002 | 4 | Medium | The digest proof was overstated for the historical rounds |
| A2-R4-003 | 4 | Low | Phantom-stream regression coverage used isolated positives only |
| A2-R5-001 | 5 | Medium | Historical-digest verification could PASS having checked only part of what its name claims |
| A2-R5-002 | 5 | Medium | The ledger recorded round-4 events dated after the commit asserting them complete |
| A2-R5-003 | 5 | Low | `A2-R4-002` cited FR-AG-034 for text that is FR-AG-032's |
| A2-R6-001 | 6 | Medium | The timestamp remediation still recorded false provenance, and its regression did not test the claim |
| A2-R7-001 | 7 | Low | The status-timestamp regression bounded an interval while the record claimed a specific value |
| A2-R8-001 | 8 | Medium | An unsealed migration baseline could absorb new violations indefinitely |
| A2-R8-002 | 8 | Medium | A proof could be certified by an execution that ran against a different subject |
| A2-R8-003 | 8 | Medium | An `intentionally-disabled` contract with an unusable disable anchor passed the validator |
| A2-R9-001 | 9 | Medium | The round-8 anti-ratchet fix made the plan's own `inactive → migration` transition unreachable |
| A2-R9-002 | 9 | Low | The integration plan's header version drifted seven revisions behind its own history |
| A2-R10-001 | 10 | Medium | `REFERENCE_SEMANTICS_VERSION` stayed at `2.0.0` across three changes to what the semantics accept |

### 8.4 Outcome and limitations

No run is marked `CONVERGED` and no run carries `final_review`. That is deliberate and is locked by a
test: convergence is not an agent's to declare while the owner gate is open, and FR-AG-018's fresh
review over the current artifact is a separate question from FR-AG-019/020 convergence.

**Status timestamps — the model, stated plainly.** *The exact review and resolution event
times are not recoverable, and this record does not pretend otherwise.* `at` is **publication/recording
provenance**: the commit time at which the finding first appeared in the committed ledger. It is not the
time the review occurred or the time the resolution was performed. The reviewed and resolving revisions
remain separate evidence.

Three iterations exposed why this distinction matters. v0.3 used invented round times; v0.5 replaced
those with artifact/build times while describing them as event/commit times (`A2-R5-002`,
`A2-R6-001`). v0.6 then documented `at` as derived from first publication but only tested that it fell
somewhere between the reviewed artifact and publication. That still admitted unsupported intermediate
timestamps. The regression now checks the claim it actually makes:

> `at` **=** commit time where the finding first appears in the committed ledger.

It separately requires that publication to occur strictly after the artifact reviewed. On incomplete
history the check skips wholesale rather than presenting partial verification as complete.

**Reviewer independence — partially addressed at v0.4.** Rounds 2 and 3 were performed by the same
assistant that applied the remediation for the findings they raised; no independence was claimed for
them, on the A0 precedent (`a0-governance-adoption-review.md` §4). **Round 4 was performed by a
different reviewer** and is the first independent pass over this candidate. It found the condition-4
sequencing defect that the non-independent rounds had missed — which is the argument for independence
made concretely rather than in principle. The corrections it produced are again non-independent, so a
round-5 pass over the pushed result is owed before row 4 is claimed.

**Round 10 was independent** — the pass round 9 owed — and found one defect. `A2-R10-001`: rounds 8
and 9 changed three admission rules while `REFERENCE_SEMANTICS_VERSION` stayed at `2.0.0`, though that
value is an input to `subject_scope_digest` and is compared by equality to raise
`proof-semantics-changed`. What makes it worth more than a version bump is that a guard was already
there and did not help: `test_reference_semantics_version_is_pinned` forces a bump to be a deliberate
edit, so the value could never drift by accident — but it asserts only that the version *is what it is*,
never that it *moved when the semantics did*. Two rounds passed green with the line untouched. A pin
that cannot fail for the reason you care about reads as coverage and is not. The replacement is honest
about its own limit: whether a bump was owed is a judgement no fixture can settle, so the new mechanism
locks only what is mechanical — the constant against every document citing it — and the judgement is
stated in the pin's docstring rather than implied away.

Round 10 also corrected a recording habit rather than a defect. Discovery results had been reported as
`0 skipped` without saying which run that describes; CI checks out at depth 1 and two history-dependent
fixtures skip there. §4 now separates the two runs and gives the command to reproduce the CI condition.

**Round 9** was a verification pass over the round-8 corrections, and found that one of them was a
regression. `A2-R9-001`: the `A2-R8-001` anti-ratchet fix rejected *every* baseline addition measured
against a trusted prior, which also closed the `inactive → migration` edge that §3.9 declares legal —
leaving the repository's own `inactive`, empty baseline with no forward path short of `strict`. It is
recorded here rather than quietly corrected because of **how** it survived: round 8 added fixtures
proving the illegitimate path now fails, and none proving the legitimate path still works. A rule that
tightens needs both, and asserting only the first is how a fix passes its own review. The suite could
not have caught it either way — every `prior_baseline` fixture in the file passed a *migration* prior,
so the `inactive` one was never constructed. That is round 8's own finding about fixture-bounded
differentials (`A2-R8-003`) recurring inside the commit that recorded it, which is the strongest
available evidence that the lesson needs a mechanism and not a note. `A2-R9-002` is unrelated and minor:
this record's owning plan had been citing itself as v0.18 for seven revisions.

**Round 9 is not independent.** It was performed by the same assistant that produced the round-8
remediation, in a separate session with no shared context — which is weaker than rounds 4–7 and is not
put forward as equivalent. It is recorded as a verification pass, not an independent review, and a
**round-10 independent pass over the pushed result is owed before row 4 is claimed.**

**Round 8** was an automated review on pull request #347 and found three defects in the frozen contract
itself — the first round since round 3 to do so, rather than in the record-keeping. That matters for how
much weight the preceding clean-looking rounds should carry: the contract had not been re-probed while
attention was on provenance. **Round 7** was a fourth independent pass, landed by the project owner as
PR #346. **Round 6** was a third independent pass, over `7d4e949`, and found that round 5's timestamp fix had
replaced fictional future times with fictional earlier ones. **Round 5** was a second independent pass,
over `5ebc3f7`. It found three further issues, two of them
in the round-4 remediation itself. Its corrections are again non-independent, so a **round-6** pass
over the pushed result is owed before row 4 is claimed. Each independent round so far has found a
defect the preceding non-independent work did not. Round 6's corrections therefore still require a fresh
round-7 review before row 4 can be claimed.

**Surfaces this review did not exhaustively verify**, recorded rather than implied: a field-by-field
re-derivation of each schema against Governance §3.3 and §7.1, and line-by-line reading of every
validator branch. Both are recorded in the ledger's `unverified_surfaces`.

---

## Version History

| Version | Date | Author | Notes |
|---|---|---|---|
| 0.10 | September 1, 2026 | — | Records round 10, the independent pass round 9 owed. `A2-R10-001` (Medium): rounds 8 and 9 changed three admission rules while `REFERENCE_SEMANTICS_VERSION` stayed at `2.0.0`, though that value is an input to `subject_scope_digest` and is compared by equality in `assess_proof_freshness`. Advanced to `2.1.0` (MINOR, per the module's `1.0.0 → 1.9.0` precedent; v0.19 reserved MAJOR for the import-contract break), covering both rounds together and restored rather than back-dated, with the versioning policy now stated at the constant. No proof artifact exists, so nothing recorded is invalidated. `test_reference_semantics_version_is_pinned` existed throughout and did not help — it asserts the value is what it is, never that it moved when the semantics did — and that limit is now written into the pin; a new fixture locks the constant to every document citing it. §4 additionally separates local full-history discovery (0 skipped) from shallow-CI discovery (2 history-dependent fixtures skip by design), a recording correction rather than a defect. Row 4 stays PENDING: round 10's corrections are again non-independent, so a round-11 pass is owed. Test split 147/9/8 = 164 → 148/9/8 = 165. A2 remains OPEN; A3 remains BLOCKED. |
| 0.9 | September 1, 2026 | — | Records round 9, a verification pass over the round-8 corrections which found that one of them was a regression. `A2-R9-001` (Medium): the `A2-R8-001` anti-ratchet fix rejected every baseline addition against a trusted prior, closing the `inactive → migration` edge §3.9 declares legal and leaving this repository's own `inactive`, empty baseline with no forward path. Reproduced against that committed document — rejected at `c927a95`, accepted at `a034fc3`. Additions are now permitted only on that entry edge, which cannot be re-entered because no transition returns to `inactive`. It survived because round 8 pinned the illegitimate path failing and never the legitimate path still working, and because every `prior_baseline` fixture in the suite passed a *migration* prior — `A2-R8-003`'s own lesson about fixture-bounded differentials, recurring in the commit that recorded it. `A2-R9-002` (Low): the owning integration plan's header read v0.18 while its history stood at v0.25, so seven revisions of citations resolved against a stale self-description; corrected and recorded. Round 9 is explicitly **not independent** — same assistant as the round-8 remediation, separate session — so a round-10 independent pass is owed before row 4 is claimed. Test split 143/9/8 = 160 → 147/9/8 = 164. A2 remains OPEN; A3 remains BLOCKED. |
| 0.8 | September 1, 2026 | — | Records rounds 7 and 8. Round 8 (automated review on PR #347) found three defects in the frozen contract itself, the first since round 3 to do so rather than in the record-keeping. `A2-R8-001`: additions to an activation baseline were rejected only after sealing, so an unsealed migration baseline could absorb a new violation and its own live-set entry in one revision and never engage the ratchet — §3.9 states "New violations fail" without qualification, and additions are now measured against the trusted prior whatever its seal state. `A2-R8-002`: a proof's executions were never bound to its subject digest, so a passing record from an unrelated subject certified it; equality is now required, recorded as a deliberate narrowing because the plan defines no subsumption relation. `A2-R8-003`: an `intentionally-disabled` contract with `{}` as its disable anchor passed the validator while the schema required three fields — a live schema/semantics divergence the differential could not see because no fixture carried a malformed anchor. The anchor shape now has one owner used by both the validator and the evaluator. Round 7 (`A2-R7-001`) is recorded for completeness. Test split 139/9/8 = 156 → 143/9/8 = 160. A2 remains OPEN; A3 remains BLOCKED. |
| 0.7 | September 1, 2026 | — | Follow-up provenance correction after independent review: v0.6 said `at` was derived from the commit that first published a finding but its regression accepted any timestamp inside the review→publication interval, and `A2-R6-001` itself carried such an unsupported intermediate value. `at` is now unambiguously first-publication commit time; the regression requires exact equality and separately proves publication is after the reviewed artifact. `A2-R6-001` is corrected to `c349fb6`'s `2026-09-01T23:13:27Z`. Row 4 remains PENDING; no frozen schema/semantics mechanism changed. |
| 0.6 | September 1, 2026 | — | Third independent review; row 4 stays PENDING. `A2-R6-001`: round 5's timestamp fix replaced fictional future times with fictional earlier ones — a finding's Open event was stamped at the commit time of the artifact reviewed, placing each discovery at or before the thing discovered, while resolutions were build times described as commit times. The record now states plainly that exact review times are **not recoverable**: `at` is when a transition was RECORDED into this ledger, derived from the commit that first published the finding, with reviewed and resolving revisions in `evidence`. The regression brackets every timestamp between the reviewed artifact and the publishing commit, with a **strict** lower bound — a first cut used `≥` and a probe showed it missed the very defect it replaced. All three shapes are proven to fail. Test split 138/9/8 = 155 → 139/9/8 = 156. A2 remains OPEN; A3 remains BLOCKED. |
| 0.5 | September 1, 2026 | — | Second independent review; row 4 stays PENDING. `A2-R5-001`: historical-digest verification skipped unavailable revisions one at a time and skipped the test only when none resolved, so a shallow checkout could verify one digest of five and still report PASS under a name asserting all — it is now all-or-nothing and names what is missing. `A2-R5-002`: round-4 status events were stamped 69 minutes after the commit asserting them complete; every timestamp now derives from a real commit and a regression rejects future-dated or out-of-order history. `A2-R5-003`: `A2-R4-002` cited FR-AG-034 for FR-AG-032's text; both are now cited correctly. Also drops the round-digest distinctness assertion, an invariant governance does not require, and removes a self-referential parameter that fed the ledger its own digests. Row 4's cell is now mechanically tied to the ledger. Test split 138/9/8 = 155 → 139/9/8 = 156. A2 remains OPEN; A3 remains BLOCKED. |
| 0.4 | September 1, 2026 | — | **Retracts the v0.3 claim that condition 4 was complete**, on independent review finding `A2-R4-001`: round 3 reviewed `678f0f2`, the material subject then moved 150 lines, and the commit asserting completion was itself never reviewed. Row 4 returns to PENDING and a test now fails if any round claims the current tree without a review of it. Round 3's recorded digest is corrected to the tree it actually reviewed. `A2-R4-002`: every recorded digest is now recomputed from the commit its scope names, rather than only the latest being verified while more was claimed; the shallow-clone bound is stated and skips explicitly. `A2-R4-003`: `tools/tests/test_recurring_defect_lint.py` adds mixed positive/negative context fixtures and pins the adjacent-negation bound; the reviewer's bullet-in-a-negative-list concern was checked and is correct suppression. Round 4 is the first independent review of this candidate. Test split 137/8/145 → 138 governance + 9 lint + 8 assembly-tier = 155. A2 remains OPEN; A3 remains BLOCKED. |
| 0.3 | September 1, 2026 | — | Satisfies the five agent-satisfiable closure conditions and records the evidence. New §8 carries the fresh-review record: per-round material subject digests (mechanically recomputed by `DurableReviewLedgerTests`, not asserted), method, and the nine-finding set now recorded in the durable `review-ledger.json` under series `A2-SCHEMA-FREEZE`, all `Blocker`/`Resolved`. Round 3 found and fixed `A2-R3-001`, an authority-boundary defect letting a property under an `FR-CS-`/`FR-TS-` id capture that requirement's waiver routing. Records the reviewer-independence limitation and the unverified surfaces explicitly. Conditions 6 (owner approval) and 7 (landing) remain **PENDING** and are not agent-satisfiable. Test split 128/8/136 → 137/8/145. A2 remains OPEN; A3 remains BLOCKED. |
| 0.2 | September 1, 2026 | — | Records second-review remediation in new §7: the per-proof artifact validator that closes the last frozen contract without an executable counterpart; fail-closed sentinel defaults on the review-ledger and activation-baseline validators, with `strict_activation` deliberately excluded; canonical `$id` on all ten schemas so relative `$ref` resolves by URI; and the bounded stdlib Draft 2020-12 validator behind a one-directional schema/semantics differential that raises on any unimplemented keyword. Restores `REFERENCE_SEMANTICS_VERSION` 2.0.0 by owner decision. Test split 104/8/112 → 128/8/136. A2 remains OPEN; A3 remains BLOCKED. |
| 0.1 | September 1, 2026 | — | Creates the explicit A2 closure gate record after the first review found the candidate unpushed, shared enum control duplicated, and A2 completion undefined. Records the eight-category/ten-schema/seven-state-file mapping, pure-stdlib single-source design, exact test split, and pending non-delegable approval. A2 OPEN; A3 BLOCKED. |
