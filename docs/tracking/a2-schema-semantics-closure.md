# A2 Schema and Executable-Semantics Closure Record

**Document Class:** Stage-gate evidence record\
**Status:** **CLOSED** — all seven conditions satisfied; approved candidate landed on `main` at `693db56` with the digest verified\
**Version:** 1.0\
**Created:** September 1, 2026\
**Owning plan:** `docs/planning/project-architecture-governance-integration-plan.md` §11 A2\
**Candidate branch:** `codex/a2-complete-schema-freeze`\
**Base:** `origin/main` at `e7a3ba13`

---

## 1. Gate state

This record implements the seven-condition A2 closure gate added by integration-plan v0.18 and
strengthened by v0.19.
Implementation, merge, review, approval, and closure are distinct: A2 stayed **OPEN** and A3 **BLOCKED**
until every row below was complete against the same subject-digest bundle. As of September 2, 2026 they
are, and this record is closed.

| # | Condition | State | Evidence |
|---|---|---|---|
| 1 | Eight-category scope map | **Complete** | §2 |
| 2 | Canonical schemas / single control source | **Complete** | §3, §7 |
| 3 | Executable representative fixtures | **Complete** | §4, §7 |
| 4 | Fresh review over pushed current candidate | **Complete** | §8. Retracted at v0.4 (`A2-R4-001`), claimable at v0.13: `A2-RUN-011` is an independent review of `1f0e68a` as pushed that returned **no findings**, so for the first time in this series no remediation followed a round and the reviewed subject is still the current one. Digest `4160b164…` recomputes identically from `1f0e68a` and from this tree. `test_closure_condition_4_is_only_claimed_with_a_review_of_this_tree` enforces the link and would fail this cell otherwise |
| 5 | Every finding terminal | **Complete** | §8; twenty-three findings, all `Blocker` / `Resolved`, in `architecture-governance/review-ledger.json` |
| 6 | Project-owner approval | **Complete** | §6. Recorded September 1, 2026: the project owner approved the candidate at `9954e90`, material subject digest `4160b164…`. Still non-delegable — this cell records a human decision, it does not substitute for one |
| 7 | Approved candidate landed on A3 base | **Complete** | §6. Merged to `main` at `693db56` on September 2, 2026. The landed material subject recomputes to `4160b164…` — identical at `1f0e68a` (reviewed), `9954e90` (approved), `0221491` (branch head), `693db56` (merge) and `origin/main`. Nothing changed on the way in |

**Row 4 was claimed at v0.3 and is retracted.** The claim was wrong in a way worth stating
plainly rather than quietly correcting: round 3 reviewed `678f0f2`, the material subject then moved
by 150 lines — the `A2-R3-001` fix, its schema change, its tests — and the commit carrying the
completion claim was itself never reviewed. The gate's pushed-candidate wording is stronger than
FR-AG-018's, and the party satisfying a condition does not get to relax it. Row 4 becomes claimable
only after a fresh review of the artifact as pushed, and
`test_closure_condition_4_is_only_claimed_with_a_review_of_this_tree` fails if this cell claims
Complete without one. **That is the test which permits the claim below**, and it is mechanical:
row 4 may read Complete only while some recorded round's digest IS the current material subject.

**All seven conditions are satisfied. A2 is CLOSED. A3 is unblocked.**

Closure does not mean the contract is beyond revision — it means this stage-gate's seven conditions are
met against one specific artifact, named by digest. Any later change inside the material subject is a
change to an approved contract and takes the A5/A6 schema-evolution route, not a silent edit.

**What moved at v0.13, stated against the standard that retracted it.** Row 4 required a fresh review
of the artifact *as pushed*. Eleven rounds are recorded and ten of them could not satisfy it, for one
recurring reason: each round found something, the fix moved the material subject, and the round that
reviewed the pre-fix tree no longer described the artifact. Round 11 is the first to return no
findings, so nothing followed it into the contract.

The only changes made after it are in files the material subject **excludes by construction** — the
review-ledger entry recording the round (§3.8: recording a run must not recursively invalidate the
subject it records), tracking prose, and a stale test name in a CI comment. That is not an argument,
it is arithmetic: `4160b164…` recomputes identically from `1f0e68a` and from this tree, and the row-4
fixture refuses the claim if it ever stops doing so. *(Written at v0.13, when rows 6 and 7 were still
open; both were satisfied the following day — see §6.)*

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
| `python3 -m unittest tools.tests.test_architecture_governance_semantics` | 149 governance fixtures, PASS |
| `python3 -m unittest tools.tests.test_recurring_defect_lint` | 9 phantom-stream context fixtures, PASS |
| `python3 -m unittest tools.tests.test_assembly_tier_check` | 8 assembly-tier fixtures, PASS |
| `python3 -m unittest discover -s tools/tests -p 'test_*.py'` | 166 total fixtures, PASS, **0 skipped** — in CI and on full history alike |
| `python3 tools/recurring-defect-lint.py --repo .` | 0 ERROR |
| `python3 tools/assembly-tier-check.py --repo .` | PASS |
| `python3 tools/doc-consistency-check.py --repo .` | PASS |
| JSON parse + `$ref` resolution over all canonical schemas/seeds | PASS |
| `python3 -m py_compile` over the reference module and suite | PASS |
| `git diff --check` | PASS |

**CI now verifies the provenance chain; until v0.11 it never had.** Exactly two fixtures are
history-dependent — `test_every_round_digest_recomputes_from_the_tree_it_names` and
`test_status_timestamps_equal_first_publication_commit_time`. `Spec hygiene checks` used
`actions/checkout@v4` at its default depth 1, so both **skipped in every CI run of this candidate**,
naming the revisions they could not reach. That is `A2-R5-001`'s all-or-nothing rule working — partial
verification is never presented as complete — but the consequence was that the digest chain and the
timestamp equality rule, on which this whole record rests, were only ever checked on a contributor's
local clone. A green badge is not evidence of a check that never ran.

`spec-hygiene` now sets `fetch-depth: 0` (that job only; every other job stays shallow). All ten
revisions the ledger names are ancestors of the candidate head, so a full fetch reaches each one, and
both fixtures execute in CI. **A `0 skipped` result is now a claim about CI as well as local.**

The skip path remains reachable and remains correct: a shallow clone still cannot verify these, and
still says so rather than passing quietly. Reproduce it with
`git clone --depth 1 file://$PWD <dir> -b <branch>` — expect `166 tests, OK (skipped=2)`. It is no
longer the CI path.

**The guard is guarded.** `fetch-depth: 0` is one line, and removing it would silently un-verify both
fixtures again with the job still green — the same blind spot, reachable by a one-line edit. So a
missing-history condition is now a **failure** whenever `GITHUB_ACTIONS=true`, and additionally under
`GOVERNANCE_REQUIRE_HISTORY=1` for CI systems that do not set it. `GITHUB_ACTIONS` is the trigger rather
than an opt-in flag, because a guard you must remember to enable is the class of guard this replaces.
Locally the skip is preserved: a contributor with a shallow clone gets an honest skip, not a red suite.
All three skip paths route through one `unverifiable` helper — missing named revisions in either
fixture, and incomplete ledger publication history.

`test_the_ci_history_guard_is_not_inert` pins both directions and both triggers, because `A2-R10-001`
was not really about a version constant: it was that a fixture read as coverage while being unable to
fail for the reason anyone cared about. Measured, on a depth-1 clone: unarmed `166 tests, OK
(skipped=2)`; `GITHUB_ACTIONS=true` `FAILED (failures=2)`; `GOVERNANCE_REQUIRE_HISTORY=1` `FAILED
(failures=2)`; and on full history with `GITHUB_ACTIONS=true`, `166 tests, OK`.

**Consequence for the publish→bind two-step — push the pair, never the publishing commit alone.** At a
commit that publishes a finding but before the commit that binds its `at`, the ledger is genuinely
inconsistent, and the equality regression now *fails* rather than skipping. Verified at `1635aa3`:
`A2-R10-001-E1 does not equal its first publication commit time` (`01:51:50Z` recorded against
`01:53:27Z` published). That is the rule working, not a defect — but it was previously masked in CI by
the shallow checkout, so it is stated here rather than left to be rediscovered as a mystery red.

## 5. Pre-review corrections

The first attempted review correctly established that the candidate was local-only and therefore
unreviewable. It also identified two real design gaps before a remote review began:

1. shared enums were duplicated between Python and JSON schemas; corrected by making
   `common.schema.json` the executable source; and
2. the plan equated landing with A2 closure; corrected by integration-plan v0.18's explicit gate.

The test-count and file-count challenges are resolved by §§2 and 4. They remain mandatory checks for
the fresh review rather than relying on this record's assertion.

## 6. Approval and closure

**All seven conditions are satisfied. This record is CLOSED, September 2, 2026.**

| Step | Evidence |
|---|---|
| Reviewed | `A2-RUN-011`, independent, over `1f0e68a` as pushed — **no findings** |
| Approved | Project owner, September 1, 2026, against material subject digest `4160b164…` |
| Landed | `main` at `693db56`, merged September 2, 2026 |
| Verified | The landed digest recomputes to `4160b164…` — identical at `1f0e68a`, `9954e90`, `0221491`, `693db56` and `origin/main` |

**The approval was bound to a digest, and the landing was checked against it rather than assumed.**
That is the whole point of the bundle: a merge can reorder, drop or transform content, and "the PR merged"
is not evidence that what landed is what was approved. Recomputed from `main` after the merge, it is.

**What closure does not mean.** It does not mark the review series converged: no run carries
`final_review` and none is `CONVERGED`, and that is deliberate — FR-AG-019/020 convergence is a separate
question from FR-AG-018's fresh review, the seven-condition gate never required it, and review runs are
immutable snapshots that must not be retro-labelled. It does not put the contract beyond revision either:
any later change inside the material subject is a change to an **approved** contract and takes the A5/A6
schema-evolution route. The approval does not transfer to a different digest.

**A3 is unblocked** — approval, terminal finding state, matching landing and this closure update all
hold. Unblocked is not started; beginning A3 remains a separate decision.

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
| 11 | `1f0e68a` as pushed — independent review, **no findings**; the subject row 4 is claimed against | `4160b1644ebe75f771f01d7f1db67278126c827849c6e0da35657eb37d454254` |

The current working tree is **not** in this table. That is the point of row 4 being open.

Each round binds the tree it actually reviewed; stamping one digest across all of them would
misreport the earlier rounds. **Every** recorded digest is recomputed from the commit its scope
names — corrected at v0.4 per `A2-R4-002`, which found that v0.3 verified only the latest and merely
asserted the rest were distinct, while claiming more than that. Distinctness is not identity.

The verification is bounded, and the bound is stated rather than glossed: `git` history must be
present. It is **all-or-nothing** — corrected at v0.5 per `A2-R5-001`, which found that v0.4 skipped
unavailable revisions individually and skipped the test only when none resolved, so a shallow checkout
could verify one digest of five and still report a green tick under a name asserting all of them. A
single missing revision now skips the whole check and names what is missing. CI checked out shallow
until v0.11, so that was the expected path; `spec-hygiene` now fetches full history and the check
actually runs there. Where history is present,
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

Twenty-three findings across eleven rounds, all `Disposition: Blocker` / `Status: Resolved`, recorded in
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

**Round 11 is the round that ends the loop, and it is worth being precise about why.** It was
independent, it reviewed `1f0e68a` as pushed, and it returned **no findings** — the first time in this
series that a round did not move the contract. Every earlier round failed row 4 the same way: it found
something real, the fix changed the material subject, and the round that had reviewed the pre-fix tree
no longer described the artifact. That is not a flaw in the rounds; it is what row 4 is for. It also
verified two surfaces this record had carried as explicitly unverified since v0.3 — Governance §3.3
property fields and §7.1 exception fields against the frozen schemas and semantics — and independently
confirmed `Spec hygiene checks` at 166/166 with 0 skipped, which is the CI-history hardening checked by
someone other than its author.

Round 11 did note stale test-name commentary — a CI comment and this record's own §1 naming fixtures
that had been renamed — and judged it non-blocking. It is corrected here, in excluded files only, and a
second stale citation the round did not catch (`test_the_current_artifact_has_not_yet_been_reviewed` in
§1, the row-4 retraction paragraph itself) is corrected with it. **A mechanism for this class is not
landed and is deliberately deferred:** a fixture asserting every cited `test_*` name resolves would live
in the fixture suite, which is inside the material subject, so landing it would move the digest and
re-open row 4 for a twelfth round. That trade belongs to the owner, batched with the next material
change, not taken unilaterally to tidy a comment.

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
| 1.0 | September 2, 2026 | — | **A2 CLOSED.** Row 7 satisfied: the approved candidate merged to `main` at `693db56`, and the landed material subject was **recomputed** — not assumed — to `4160b164…`, identical at `1f0e68a` (reviewed by `A2-RUN-011`), `9954e90` (approved), `0221491` (branch head), `693db56` (merge commit) and `origin/main`. Nothing changed on the way in, which is the check the digest-bound approval exists to make possible: "the PR merged" is not evidence that what landed is what was approved. §6 rewritten as the closure record with all four steps and their evidence. All seven conditions now hold; **A3 is unblocked**, which is not the same as started. Deliberately NOT done: no review run is marked `CONVERGED` or `final_review` — FR-AG-019/020 convergence is a separate question from FR-AG-018's fresh review, the seven-condition gate never required it, and runs are immutable snapshots that must not be retro-labelled; the test enforcing this was left in place rather than relaxed to match the new state. Records only — no schema, executable semantics, fixture or finding changed; discovery holds at 149/9/8 = 166. |
| 0.14 | September 1, 2026 | — | **Closure condition 6 recorded: the project owner approved the candidate at `9954e90`**, material subject digest `4160b164…` — the same subject `A2-RUN-011` reviewed at `1f0e68a`, unchanged since. §6 rewritten from "no approval is recorded" to the recorded approval, and states the binding explicitly: the approval attaches to that digest and does not transfer, so any change inside the material subject returns row 6 to PENDING and requires a fresh approval, while excluded files (tracking prose, the review ledger, CI configuration) may change without disturbing it. Row 7 is now the only outstanding condition — merge the candidate onto the base A3 builds on, verify the landed digest still recomputes to `4160b164…`, then mark this record `CLOSED`. No run is marked `CONVERGED` and none carries `final_review`: that remains locked while row 7 is open, and is not what owner approval releases. **A2 stays OPEN until the approved candidate lands; A3 stays BLOCKED.** Records only — no schema, semantics, fixture, or finding changed, and the test count holds at 149/9/8 = 166. |
| 0.13 | September 1, 2026 | — | **Row 4 moves to Complete.** `A2-RUN-011` is an independent review of `1f0e68a` as pushed that returned **no findings** — the first round in this series after which nothing followed into the contract, which is exactly the condition row 4 has been waiting for since its v0.4 retraction. Digest `4160b164…` recomputes identically from `1f0e68a` and from this tree, and `test_closure_condition_4_is_only_claimed_with_a_review_of_this_tree` would refuse the cell otherwise. The round also verified Governance §3.3 property fields and §7.1 exception fields — carried as explicitly unverified since v0.3 — and independently confirmed `Spec hygiene checks` at 166/166, 0 skipped. Corrections made after the round are confined to files the material subject excludes by construction: the ledger entry recording the run, tracking prose, and stale fixture names in a CI comment and in §1 (including `test_the_current_artifact_has_not_yet_been_reviewed`, which the round did not catch). A fixture pinning cited test names is deliberately **not** landed — it would sit inside the material subject and re-open row 4 for a twelfth round; that trade is the owner's to make with the next material change. **Rows 6 and 7 remain PENDING and are not agent-satisfiable.** Test count unchanged at 149/9/8 = 166. A2 remains OPEN; A3 remains BLOCKED. |
| 0.12 | September 1, 2026 | — | Hardens the v0.11 fix against its own removal, at the round-10 reviewer's recommendation, before round 11. `fetch-depth: 0` is one line and dropping it would silently un-verify both history-dependent fixtures with the job still green. A missing-history condition is now a **failure** whenever `GITHUB_ACTIONS=true`, and additionally under `GOVERNANCE_REQUIRE_HISTORY=1` for other CI systems; `GITHUB_ACTIONS` is the trigger rather than an opt-in flag, because a guard you must remember to enable is the class of guard this replaces. Local skips are preserved. All three skip paths route through one `unverifiable` helper — missing revisions in either fixture, and incomplete ledger publication history. `test_the_ci_history_guard_is_not_inert` pins both directions and both triggers, `A2-R10-001`'s lesson applied to the guard itself. Also records a consequence measured at `1635aa3`: the publish→bind two-step must be pushed as a pair, since the equality regression now fails rather than skips at a publishing commit. No frozen executable semantics changed, so **no `REFERENCE_SEMANTICS_VERSION` bump is owed**. Test split 148/9/8 = 165 → 149/9/8 = 166. Row 4 stays PENDING. A2 remains OPEN; A3 remains BLOCKED. |
| 0.11 | September 1, 2026 | — | Acts on round 10's evidence note instead of only recording it. The two history-dependent fixtures — `test_every_recorded_digest_matches_the_revision_it_names` and `test_status_timestamps_equal_first_publication_commit_time` — had skipped in **every** CI run of this candidate, because `Spec hygiene checks` checked out at the `actions/checkout` default depth of 1. The digest chain and the timestamp equality rule this record rests on were therefore only ever verified on a contributor's local clone, never by the gate. `spec-hygiene` now sets `fetch-depth: 0` — that job only; every other job stays shallow — and all ten ledger-named revisions were confirmed ancestors of the candidate head, so the fetch reaches each. §4 and §8.1 are corrected accordingly: a `0 skipped` result is now a claim about CI as well as local. The shallow skip path stays reachable and stays correct; it is simply no longer the CI path. Workflow only — no fixture, schema, semantics, or finding changed, and the count holds at 148/9/8 = 165. Row 4 stays PENDING. A2 remains OPEN; A3 remains BLOCKED. |
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
