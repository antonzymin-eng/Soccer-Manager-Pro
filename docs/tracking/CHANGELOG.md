# CLAUDE.md — Change Log

> **Created:** July 31, 2026
> **Purpose:** The `**Last Updated:**` entry chain formerly carried in the header of
> `CLAUDE.md`. Newest first; each entry records what landed, what was measured, and
> what was deliberately not done.
> Split out of `CLAUDE.md` on July 31, 2026. Content is **verbatim** — entries were moved, never edited, reordered, or deduplicated.

**Appending a new entry:** add it at the top of the chain below and re-label the
previous newest entry `**Last Updated (prior):**`. The chain is the record — do not
break it, and do not edit historical entries.

---

> **Last Updated:** September 4, 2026 — **A3.3 reconciliation gate run over the combined #19 + #20 amendment candidate; two findings corrected, one recorded as already dispositioned, three recorded for A3.4. Documentation only.**
> A checking pass, not drafting: the seven required gates — count, range, traceability, exception-route, stale-reference, documentation, and repository — were run over the combined candidate, and only findings inside A3 scope were corrected.
> **PASSED as found.** *Count:* `section-2.md` carries 81 unique FR-CS rows (plus sub-clauses FR-CS-046a/046b) and 97 unique FR-TS rows; both partition tables sum to their stated totals with no duplicate and no gap. *Range:* FR-CS-001 … 081 and FR-TS-001 … 097 are contiguous; nothing above the declared ceiling exists (`FR-CS-082/083` appear only in #20 §2.2.10's forward-allocation sentence and `outline-detailed.md`'s mirror of it). *Traceability:* #20 §5.5 carries 83 leading-cell rows covering every FR plus both sub-clauses; #19 §5.2 and §5.6 cover FR-TS-001 … 097 with no gap once the FR-TS-075 … 078/080, 079 and 086 … 092/094 … 097, 093 splits are read as the bands they are; §3.5.6/§3.5.7 and §3.11.1 … §3.11.11 exist as cited; every `FR-AG-` id cited by either spec resolves in Governance; every `Appendix A–G` reference resolves to a real heading. **The appendix examples were validated against the frozen A2 contract, not read**: #19 Appendix G.4 validates clean against `proof-artifact.schema.json` through `tools/architecture-governance/schema_validator.py`, G.2's required-field list matches that schema's `required` array exactly, and G's execution / bounded / failure-injection / mutation records match `$defs/execution`, `$defs/approvedLimitation`, `$defs/perturbation` and `$defs/mutation` with no unknown and no missing required field; #20 Appendix F.2 matches `integration-contracts.schema.json` `$defs/contract` and F.3 matches `runtime-surface-classifications.schema.json`'s surface item, both exactly. *Exception route:* #19 §2.1, §2.3 and §7.2 and #20 §2.3 Mode 3 each state that the route cannot waive an admitted property, required proof, concrete correctness/integrity failure, or Governance Blocker; #19 §3.7.3 carries the FR-TS-063/077 quarantine qualifier. *Repository:* `doc-consistency-check.py` PASS, `assembly-tier-check.py` PASS, tooling unit tests 166/166, `recurring-defect-lint.py` 0 ERROR tree-wide. `SPEC_INDEX.md` is untouched, no file claims the architecture/evidence gate is active, and the four-gate topology in #19 §6.2.4 agrees with FR-TS-076 and §2.2.
> **Corrected — (1) ten version-history rows out of order across six #19 files.** `recurring-defect-lint.py` reported 10 ERRORs, all in `docs/specs/testing-strategy/`, all introduced by A3.2a/A3.2b prepending amendment rows to tables that read oldest-first: `section-3.md` (0.6, 0.1, 0.2, 0.3, 0.5, 0.4), `section-4.md` and `section-6.md` (0.4 before 0.3), `section-5.md` and `section-7.md` (0.5, 0.4, 0.3 reversed), `outline-detailed.md` (1.3 before 1.2). This is the same defect A3.1b fixed on the #20 side at `76389c6e` ("version-row ordering"), and it is repaired the same way — **rows reordered, no row edited, added, or removed, and no version bumped**: verified by sorting each file's lines before and after and comparing digests, which match. The lint now reports **0 ERROR tree-wide**, the project's standing bar.
> **Corrected — (2) a false #20 status assertion in `docs/agent-guides/project-reference.md`.** Its OPEN ISSUES index still carried *"Assembly layer taxonomy (Spec #20 §3.5.2) places 19 of 35 assemblies — ERR-020-002 proposal filed, awaiting owner sign-off"*. Both halves are false: `ERR-020-002` and `ERR-020-003` were ADOPTED and landed August 17, 2026, the entry has lived in `open-issues-resolved.md` since, and `tools/assembly-tier-check.py` seats **35 of 35** production assemblies at this head (re-run, PASS). The plan's §6.4 acceptance names "approval/status assertions" as a required repo-wide #20 sweep, and that section's own contract says a resolved entry moves to the archive rather than being re-inlined — so the stale bullet is deleted, not annotated. It is the last live surface still describing the retired taxonomy.
> **Found, and deliberately NOT changed — the retired `Physics → Mechanics → AI → UI` arrow chain in #20's outlines.** The plan's #20 acceptance also names "arrow wording", and the sweep hits `outline-detailed.md:232`/`:405` and `outline-mid.md:226`, which still describe FR-CS-046 and §3.5.2 in the retired three-layer terms. These are **already dispositioned**: `spec-error-log.md`'s `ERR-020-002` record carries an explicit "Deliberately not changed, so a later sweep does not have to rediscover the list" paragraph naming these exact sites, on the ground that the outlines are pre-authoring artifacts and the section files carry the contract. Reversing a recorded disposition is not a reconciliation correction, so the gate records the hit and leaves it. `section-2.md`'s version-history quotation of the retired chain is likewise intentional history.
> **Recorded for A3.4, not fixed here — three observations that would be new drafting.** (a) #19 §3.6.5, the coverage-exemption procedure, does not restate or point at the §2.1 boundary that §7.2 and §2.3 both carry; §2.1 governs, so nothing contradicts, but the owning mechanics subsection is silent. (b) #19 §2.2 has no FR-total footer and no renumbering prohibition of the kind #20 §2.2.10 carries; 97 is derivable from the partition table and stated in §9.1.2, but the two specs are asymmetric here — and this predates A3. (c) §2.2's per-FR Activation value disagrees with §5.2's band for eight baseline rows (FR-TS-013/015/018/020/022/038/039/043); §5.2's own preamble says "most FRs read Stage 0+1", so the bands are summaries rather than per-FR claims, and all eight are pre-A3 baseline rows outside this amendment's scope.
> **Self-review addendum (same pass).** Three checks were run *after* the gate report was first written, because two of its figures had been asserted from repository history rather than measured, and one gate had been claimed without its tool being run. All three now hold, and the figures are unchanged. (a) **"35 of 35 assemblies"** was taken from the `ERR-020-002` record; `python3 tools/assembly-tier-check.py --repo .` now reports it directly — 35 folders under `src/`, 35 placed (33 in ordered tiers + 2 out-of-band Infrastructure), 148 production references, 0 upward, graph acyclic. (b) **"83 traceability rows"** was counted file-wide in `section-5.md`, not per-section; re-counted per heading, all 83 sit in §5.5 as claimed. (c) **`check_drift.sh` had not been run** when the repository gate was reported: it is clean — 1 bare `Last Updated:` label in each chain, no header chain in `CLAUDE.md`/`README.md`, tracking dates matching their git-touch dates — apart from the pre-existing `UNPARSED` on root `CLAUDE.md`'s OPEN ISSUES header, which this pass does not touch and which the September 3 entry below already records.
> **Self-review addendum — one gate dimension had been skipped, and is now run.** The landed FR statements were never diffed against the amendment the plan proposes. All twenty amended rows (FR-CS-074 … 081, FR-TS-086 … 097) were compared to §6.1/§7.1: **no semantic drift.** Every difference is editorial — slash shorthand expanded to prose, RFC-2119 keywords bolded — with one real exception, FR-TS-088, whose landed text narrows the plan's bounded-substitute escape to "only where FR-TS-096 applies" and adds the Governance FR-AG-026 boundary sentence. That narrowing is **recorded**, in `section-2.md`'s own v0.3 row ("separates bounded substitutes from Governance-approved surface exclusions"), so it is a traceable tightening rather than undocumented drift. Two further checks came back clean: `§2.2.9` is not a stale pointer anywhere (A3.1a moved the FR *footer* to §2.2.10 and made §2.2.9 the architecture partition, which is what every citation names), and #20 §3's `*Implements:*` tags at §3.5, §3.5.6 and §3.5.7 union to FR-CS-074–081 with no gap, each agreeing with that FR's §2.2 "Mechanics §" pointer.
> **Recorded, not fixed — `project-reference.md`'s OPEN ISSUES index is broadly out of sync, independently of this pass.** It now carries 14 bullets against 21 entries in `open-issues.md`. Deleting the stale #20 bullet did not widen that gap — it was 14 live bullets before and 14 after, since the deleted one pointed at an archived issue — but the seven-entry shortfall is pre-existing drift in a file whose own header disclaims status authority, and reconciling it is neither #19/#20 work nor A3's.
> **Expected, not drift:** `doc-consistency-check.py` again prints `NOTE: the registry share ("N% of the registry") agreement group matched no surface`. The entry directly below predicted exactly this — that group's only in-scope surface was that entry's own `~36%` figure, and relabelling it `(prior)` takes it out of scan scope again. Reviving the group means bringing `data-contract-index.md` and `project-reference.md` into scope, which remains a separate decision. The "excused as dated records" total also moves 46 → 45 (chronicle 7 → 6): that is the deleted `project-reference.md` bullet, not a new excusal.
> **No gate run is owed** — no `.cs`, `.asmdef`, or `tools/dotnet-ci` file moved. No schema, executable semantics, runtime code, workflow, required-status, `SPEC_INDEX.md`, spec status, or open-issue state changed; no spec version was bumped. Six #19 files WERE modified, so this is not a "no spec file touched" landing: their version-history rows were reordered and nothing else, no row's text changed, and the `76389c6e` precedent for that exact repair on the #20 side bumped no version either. A3.4 reapproval and A8 enforcement remain pending.
>
> **Last Updated (prior):** September 3, 2026 — **Root `README.md` restructured from project ledger to orientation document; its header chain archived verbatim; the maintenance rule that regrew it amended and mechanically enforced. Documentation and tooling only.**
> **The file was 1,446 lines and internally contradictory.** Its header carried **38 chain entries over 564 lines — 55,578 of 116,766 bytes (47.6%)** — and both the header's `**Current Stage:**` line and the file's closing line claimed *"All 26 approved specs have active `src/` implementations"*, while the body of the same file correctly recorded **53 approved specs and 19 of them with no assembly**. A reader who skimmed got the wrong answer; a reader who persevered got the right one. `GETTING STARTED` still listed Unity 6 recertification and `FR-PO-052` as outstanding at items 26–27 against the same file's own record of both completing July 19; `NEXT IMMEDIATE STEPS` was labelled "as of July 26, 2026"; the header was dated August 29 with A2 since closed (September 2) and A3 in progress. **This was not repaired by refreshing the numbers**, which would have reset the drift clock on a structurally drift-prone file: the README duplicated `SPEC_INDEX.md`'s spec schedule, the roadmap's implementation sequencing and the CHANGELOG's history, and hand-maintained counts in several places at once.
> **(1) The chain moved verbatim to new `docs/tracking/CHANGELOG-readme.md`** — byte-for-byte, verified by `diff`, on the `CHANGELOG.md` (July 31) and `landing-history.md` (August 22) precedents, with a preamble stating its provenance, its non-authority and that it is closed to appending. **(2) `README.md` is now 226 lines** and carries project vision, one dated status snapshot, a repository map, a documentation-authority table, an architecture overview, a getting-started path, and an explicit **README maintenance contract** naming what must never return to it. **(3) The single status snapshot is machine-checked.** It is phrased so `tools/doc-consistency-check.py` can verify it — `35 production assemblies` matches the cardinality oracle and `19 of the 53 ... no `src/` assembly` matches the oracle-less agreement group, whose denominator is checked against the measured spec-folder count. Removing the chain also removed the `**Last Updated (prior):**` marker that froze most of the file from scanning, so README goes from almost entirely frozen to **10,490 chars in scope, 1 cardinality + 1 agreement figure**, both live. **(4) The rule that regrew the chain is amended.** `landing-close-out/SKILL.md` item 5 required a README status-summary and `Last Updated` update on *every* landing and delegated it to `doc-scribe` as rote transcription; it now reads "usually nothing", scopes the only permitted edit to replacing the one snapshot when a landing falsifies a fact in it, and is removed from the delegation list because that judgment is not transcription. The skill description and the `check_drift.sh` pre-check paragraph follow. **(5) Enforcement, not prose.** `check_drift.sh`'s "root `CLAUDE.md` carries no header chain" guard is generalised over a `CLAUDE.md` / `README.md` table and README leaves the staleness loop (it no longer declares a date to compare). **The guard was proven to fire** by reintroducing a `**Last Updated:**` line into `README.md`, observing `FAIL: README.md has 1 'Last Updated' line(s)`, and restoring — verification by execution, not by reading. **The guard also sets the script's exit status** (PR #354 review, `chatgpt-codex-connector` P2): it printed `FAIL` and the script still exited **0**, reproduced, so any automation or chained close-out command reading the status saw a forbidden chain as a passing check. The guard now sets `chain_violation` and the script exits **1** *after* printing the complete report, so no section is lost to an early exit. It is deliberately the script's **only** status-affecting check — an explicit EXIT-CODE CONTRACT block at the top of the file records that the staleness and open-issues sections stay advisory, because whether drift is acceptable to land on is a judgment call while a reintroduced chain never is. Verified both directions: clean tree exit 0, violation exit 1 with all three report sections still printed, exit 0 again after restore.
> **(6) `doc-consistency-check.py` commentary corrected:** the scope comment claimed README carries a VERSION HISTORY section (it no longer does; `record_regions()` fails open, the safe direction) and said "eleven files" for a 13-entry `CURRENT_STATE`; the round-23 rationale block keeps its measured line numbers, entry counts and the "All 26 approved specs" quote **as a dated record**, annotated as superseded, since the rule it justifies is unchanged and still load-bearing for the five other surfaces that do carry chains. **(7) `docs/agent-guides/project-reference.md`:** "The **20** with no assembly" corrected to **19** — the list still carried **#44 Discipline**, whose `src/discipline/` assembly landed August 13, 2026 — its registry share from ~42% to **~36%** (19/53), and its README tree description from "Project overview, status, documentation hierarchy" to orientation, explicitly not a status authority.
> **Two findings recorded, not silently absorbed.** (a) **The `registry share ("N% of the registry")` agreement group lost its only surface and will go dormant.** The old README's "~36% of the registry" was that group's only `CURRENT_STATE` site; with the README rewritten the group matched nothing tree-wide and the run printed `NOTE: ... currently checking nothing` while still exiting PASS — silent loss of a check, which is the round-9 failure mode the NOTE exists to surface. It is not dormant at this commit only because **this entry** now carries the figure (~36%, = 19/53, verified correct); the next landing freezes this entry as `(prior)` and the NOTE returns. Treat that NOTE as expected, not as new drift. The surviving copies are out of scan scope: `data-contract-index.md` remains **~42%**, while `project-reference.md` had also been ~42% and is corrected to **~36%** by this PR (its assembly-less list still carried #44 Discipline, whose `src/discipline/` assembly landed August 13, 2026); this file's own history additionally carries a 43%. The percentage was deliberately **not** re-added to the README: it is a volatile count the new maintenance contract forbids and a redundant restatement of the `19 of the 53` figure that *is* checked. Reviving the group means bringing those surfaces into scope — a separate decision. (b) The two out-of-scope copies disagreed with the README for an unknown period (36% vs 42%) with no finding raised, because agreement is only checked among scanned surfaces.
> **Verification at `3bb4f44`** (the measured counts below are that commit's, not a re-run — `main` has since moved): `tools/doc-consistency-check.py` **PASS** (13 surfaces + globs; `measured: production assemblies=35, spec folders=53, design supplements=61, ERR- index rows=226, active open issues=19, resolved open issues=51`), and `check_drift.sh` clean apart from the pre-existing `UNPARSED` on root `CLAUDE.md`'s OPEN ISSUES header, which is untouched by this pass. **No gate run is owed**: no `.cs`, `.asmdef` or `tools/dotnet-ci` file moved. No `SNAPSHOT_SCHEMA_VERSION`, RNG stream, domain tag, draw site or draw-order change; no spec status, `SPEC_INDEX.md` row or open-issue state changed.
>
> **Last Updated (prior):** September 3, 2026 — **A3.1b post-merge review findings closed; KD-4 modernization deliberately deferred; documentation only.**
> **Restored after the `c5a914f` merge resolution (September 4, 2026).** Merging `main` into this branch resolved `docs/tracking/CHANGELOG.md` and `docs/tracking/file-manifest.md` by taking `main`'s side wholesale, which dropped this entry and the `CHANGELOG-readme.md` manifest row — leaving the branch shipping a **new tracking file with no manifest row** and a **landing with no changelog entry**, the exact drift this landing exists to stop. Both are restored here as the union of both sides, per the `steward` skill's conflict rules for these two files: no historical entry was edited, `main`'s own head entry is relabelled `(prior)` rather than replaced, and exactly one bare `**Last Updated:**` label survives (`check_drift.sh` verifies that count). **Corrected further in the same review chain (September 4, 2026):** `bcadfdd` fixed the `~42%` claim and the `project-reference.md` counts; this commit removes the "the group is dormant" conclusion that stood two sentences after this entry correctly says it is not (it is not — restoring this entry restored the group's only in-scope surface, and the run prints no NOTE at this head), repairs the sentence seam that edit left, and relabels the `**Verification**` line to name the commit it measured rather than refreshing its counts, so it stays a dated record.
>
> **Last Updated (prior):** September 3, 2026, later — **PR #353 pre-merge cleanup; CI hygiene regression repaired and missing A3.2a version record restored.**
> The merge resolution had restored one stale `file-manifest.md` citation to `spec-error-log.md` **v2.47** after the review-fix branch had moved both occurrences to v2.48, making `Spec hygiene checks` fail. That pointer is repaired. Testing Strategy `section-3.md` advances **v0.5 → v0.6** solely to record the already-landed PR #352 §3.11.6 SHOULD→MUST correction that had no version/history row; no new normative change is introduced here. The integration-plan v0.38 A3.2b row now records the PR #353 review corrections it previously omitted. Maintained manifest pointers move to `section-3.md` v0.6. No production code, schema, executable semantics, workflow, or enforcement changed.
>
> **Last Updated (prior):** September 3, 2026, later — **A3.2b review corrections (Codex on PR #353); one spec defect filed and resolved, one conformance gap opened.**
> Four corrections over the A3.2b head. **(1) FR-TS-093** was assigned Stage 0 by §2.2 but Stage 0+1 by §5.2's FR-TS-086 … 097 band and by the §5.6 row A3.2b added, which attached A4/A8 prerequisites a review-mechanics requirement does not have; split to its own Stage 0 row in both tables, status left AMENDMENT DRAFT / non-blocking because A3.4 has not reapproved it. **(2) The `tests/scenarios/index.<ext>` encoding decision** was stranded when A3.2b closed D1 on the test runner alone — five live references still deferred it to D1, one of them three rows above D1's own resolution — and now has its own **D9**, filed overdue rather than deferred; no extension is pinned. **(3) FR-TS-078** requires the CI-provider pin in `src/CLAUDE.md`, which carried no provider declaration, so the D4 resolution and the FR were mutually unsatisfiable; `src/CLAUDE.md` now declares GitHub Actions as a pointer to `.github/workflows/ci.yml` (`CHANGELOG-src.md` v2.126). FR-TS-078 itself is untouched — it lives in §2.2, which A3.2a owns. **(4) `ERR-019-001` filed and RESOLVED:** §5.2 published `FR-TS-075 … 080` as `Inactive` behind the criterion "CI provider pinned (D4)", which the normative core does not define, against §7.1's actual Stage 0+1 trigger (first `src/` code commit, KD-5) that this repository passed long ago — and the band swallowed FR-TS-079, a Stage 0 requirement. The false `Inactive` concealed real unmet MUSTs: **no pre-commit pipeline, no nightly pipeline, and no `tools/run-tests-local.sh`**. Table corrected in the same commit; the gap itself is **recorded, not fixed**, and open at `open-issues.md`. Deliberately not closed by gating FR-TS-075 on the three-pipeline topology it mandates — that is circular and fail-open. No production code, schema, executable semantics, workflow, or required-status change; `SPEC_INDEX.md` untouched; A3.4 reapproval and A8 enforcement still pending.
>
> **Last Updated (prior):** September 3, 2026 — **A3.2a/A3.2b Testing Strategy governance amendment draft synchronized; reapproval/enforcement still pending.**
> A3.2a added FR-TS-086…097, §3.11 architecture proof/evidence mechanics, and Appendix G against the frozen A2 schema/reference semantics. A3.2b now synchronizes §1/§4–§9, both outlines, exception/coverage-exemption boundaries, FR-to-verification coverage through 097, the four-gate CI topology, owning-runner/result binding, Governance convergence consumption, targeted mutation, and approval-checklist draft evidence. A live-repository deferral audit closes only D1 (NUnit 3.14.0 + NUnit3TestAdapter 4.6.0, executed through `tools/dotnet-ci/run-gate.sh`) and D4 (GitHub Actions at `.github/workflows/ci.yml`); D2/D3/D5–D8 remain deferred. The integration plan is v0.38; its stale §7 proof/mutation pointers are corrected and Appendix-G example IDs are reserved/example-scoped. **No architecture/evidence required status is activated, no schema/executable semantics/runtime code/workflow is changed, and `SPEC_INDEX.md` remains untouched.** A3.4 still owns coordinated #19/#20 reapproval/status synchronization; A8 owns enforcement activation.
>
> **Last Updated (prior):** September 3, 2026 — **Spec #20 KD-4 staleness filed as an open issue; tracking only.**
> `section-1.md` §1.3 KD-4 still states normatively that at Stage 0 conformance verification is manual review and "no static-analysis tooling is required", on the rationale that no source code exists — both false at `019def1`, where 35 production assemblies and 956 `.cs` files sit behind an advisory `dotnet format` check and a blocking `tools/dotnet-ci/run-gate.sh`. An automated review of PR #351 asked for the normative text to be rewritten; that was declined there, because restating a Key Design Decision is a governance-semantic change and the amendment set is under a pending A3.4 reapproval. Filed here instead so the deferral is indexed rather than living only in version-history rows. Not urgent: `section-5.md` §5.1's "Tooling status" paragraph already cites KD-4 in the past tense and records that the Stage 0+1 transition has since arrived, so the operative guidance is correct — the stale text is the KD's own statement and rationale. A3.4 discharges it, and should re-derive §5.1's own file counts rather than copy them (§5.1 publishes 947 from August 18, 2026; today it is 956). Active open issues 20 → 21. No spec, code, schema, workflow, or enforcement change.
>
> **Last Updated (prior):** September 3, 2026 — **A3.1b post-merge review findings closed; KD-4 modernization deliberately deferred; documentation only.**
> **(1) FR-CS-074 verification path restored.** §5.5's row had *replaced* "A4 canonical-selector / identity resolver" with the owner/exact-point/activation-state resolver instead of adding to it, while §5.4.8 item 1 still requires stable `component_id`, canonical selector, rename preservation and selector history, and §3.5.6 makes those identity mechanics mandatory with cross-registry resolution itself deferred to A4. Both resolvers are named again; severity stays report-only. **(2) The last two `future \`src/CLAUDE.md\`` residues swept** — `section-1.md` §1.4 and `outline.md`'s executive summary; the file exists and every sibling surface had already been corrected. **(3) KD-4 restored verbatim to its pre-A3.1b text in `outline-detailed.md`.** The post-merge correction had rewritten that Key Design Decision while the authoritative `section-1.md` §1.3 kept the original, and an automated review then asked for the normative rule to follow. Declined for this slice: A3.1b is synchronization and finding closure, and rewriting a KD's statement is a governance-semantic change; §5.1's "Tooling status" paragraph already frames KD-4 as the historical Stage 0 decision and records that the Stage 0+1 transition has since arrived. All three outline tiers and the section file agree again, and modernizing KD-4 against the live tree is tracked separately for A3.4. Every maintained pointer moved in the same commit (`file-manifest.md`'s `section-5.md` pointer to v1.7 and its outline-tier inventory row, which had gone stale for three files without the consistency check reporting it). `SPEC_INDEX.md` stays untouched pending A3.4. No runtime/code/schema/CI-enforcement change.
>
> **Last Updated (prior):** September 3, 2026 — **A3.1b post-merge Codex-review corrections; documentation only.**
> Follow-up to the five automated review findings recorded on merged PR #350: `section-5.md` §5.4 now states eight categories, §5.4.8 items 1–2 and the §5.5 rows for FR-CS-074/075 are aligned with the authoritative §2.2.9 requirement text (explicit integration owner / exact integration point / orthogonal activation state; every production host or composition root classified and mechanically accounted for), `section-8.md` §8.1 states ten sources, Spec #19 is removed from the mutually exclusive pointer-only table, and the adjacent `TBD-NORMATIVE` paragraph no longer claims root `CLAUDE.md` is the only upstream authority. `outline-detailed.md` §1.4, §2.2.9 and §8 are synchronized so authoring from the outline cannot recreate the pre-amendment authority model. The maintained `section-5.md` pointer in `file-manifest.md` moves v1.5 → v1.6 with that bump. `SPEC_INDEX.md` stays untouched pending A3.4. No runtime/code/schema/CI-enforcement change.
>
> **Last Updated (prior):** September 2, 2026 — **A3.1b review corrections; CI consistency blocker removed.**
> Follow-up to `d196324e`: repairs the stale maintained `section-5.md` pointer, C-03 arithmetic, C-06 Appendix-F expectation, duplicate §2.2.9 footer, six-tag outline residues, §8 source cadence/evidence wording, and all misplaced A3.1b version rows. `section-6.md` is restored to the approved v1.2 state because it had no A3 content change. The Roslyn `?view=visualstudio` target is retained after direct retrieval confirmed it is Microsoft's canonical target; unlike the old queryless URL, it is not matched by the repository's anchored ignore. `SPEC_INDEX.md` stays untouched pending A3.4. No runtime/code/schema/CI-enforcement change.
>
> **Last Updated (prior):** September 2, 2026 — **A3.1b Code Standards supporting surfaces synchronized; A3.4 still pending.**
> The #20 amendment's supporting surfaces now match A3.1a: all 16 live stale `73` claims move to 81 while dated May/August baseline evidence remains untouched; §5.4 gains an eighth Architecture Integration & Activation category and §5.5 grows to 83 traceability rows (81 numbered FRs + 046a/046b). §1/§4/§7/§8 and all three outlines now carry the Governance/#19 authority split, tooling-record-vs-runtime boundary, A4 report-only resolver/discovery boundary, labelled ten-tier dependency source, and separate reapproval-versus-enforcement activation sequence. `coding-reference.md` no longer reproduces the retired three-layer taxonomy/ERR-020-002/003 state. §8 re-verifies and repins its external source URLs. `SPEC_INDEX.md` is deliberately unchanged until A3.4 re-runs §9 and atomically reapproves the combined amendment. No `src/`, `.cs`, `.asmdef`, schema, executable semantics, workflow, CI job definition, or enforcement behavior changed.
>
> **Last Updated (prior):** September 2, 2026 — **A3.1a lifecycle binding clarified; draft remains unapproved.**
> Code Standards §3.5.6 no longer calls a runtime-surface field “lifecycle” without naming where that
> qualifier lives. Non-N/A activation/update/teardown values now resolve as exact runtime-surface
> `surface_id` values and as the same dependency-graph identifiers with `kind: lifecycle`. §3 → v1.12.
> No schema, executable semantics, workflow, production code, or enforcement changed. Verified: 166/166
> tooling tests, document consistency, assembly-tier checking, and recurring-defect lint at 0 ERROR.
>
> **Last Updated (prior):** September 2, 2026 — **A3.1a automated-review findings resolved; draft remains unapproved.**
> Two P1 findings exposed overclaims in the new ownership/lifecycle mechanics: schema `1.0.0` and reference
> semantics `2.1.0` validate string shape but do not resolve ownership/path bindings, and `na_fields` had no
> defined field-value representation. Code Standards §3.5.6 now defines exact registry/inventory/path targets,
> construction-path grammar, the `not-applicable` sentinel and one-to-one justification pairing, while explicitly
> keeping both surfaces non-blocking until A4 implements cross-registry resolution and discriminating fixtures.
> Appendix F.2 demonstrates the sentinel; F.4 uses the same component/surface/path identities. Plan → v0.35;
> Code Standards §2 → v1.8, §3 → v1.11, appendices → v1.6.3. No schema, executable semantics, workflow,
> production code, or enforcement changed. Verified: 166/166 tooling tests, document consistency,
> assembly-tier checking, recurring-defect lint at 0 ERROR, and all five Appendix F JSON examples with
> F.4 accepted at five nodes/four edges. A3.1b and A3.4 remain pending.
>
> **Last Updated (prior):** September 2, 2026 — **A3.1a renumbering sweep completed; draft remains unapproved.**
> The §2.2.9 → §2.2.10 footer move was repaired at `section-2.md` but not swept: `section-9-approval-checklist.md`
> C-04's live proxy comment still excluded "the §2.2.9 partition-footer rows" — in a file edited in that same pass —
> and `section-5.md` §5.5 still cited "§2.2.9's partition Count column". C-04 re-pointed to §2.2.10; §5.5 annotated
> rather than re-pointed, since that file is APPROVED and describes the baseline where the Count column IS §2.2.9.
> C-03's `[x] Verified May 8, 2026 — returns 73` line annotated in place as approved-baseline evidence, so the
> checkbox no longer reads as current against the draft-invalidation note above it; the May 8 record is preserved,
> not unchecked. C-04's dated May 11, 2026 record keeps its `§2.2.9` citation — the footer was §2.2.9 then.
> §5 → v1.4, §9 → v1.1.5. Verified: 166/166 tooling tests, document consistency, assembly-tier check, recurring-defect
> lint (0 ERROR), and executable Appendix F validation all pass. A3.1b still owns the stale-73 synchronization;
> A3.4 reapproval remains required. No production code, schema, executable semantics, workflow, or enforcement changed.
>
> **Last Updated (prior):** September 2, 2026 — **A3.1a review corrections complete; draft remains unapproved.**
> Resolved the two blocking review findings and the normative-core omissions before A3.1b: restored a
> schema-valid F.4 dependency graph, repaired the CI-breaking maintained version pointers and shifted
> §2.2.10 reference, expanded §3.5's declared FR range, restored strict classification and closed-world
> absence-proof safeguards, aligned FR-CS-078 with Governance FR-AG-025, and marked C-03 open for the
> 81-FR draft. Plan → v0.34; Code Standards §2 → v1.7, §3 → v1.10, appendices → v1.6.2, §9 → v1.1.4.
> Verified: 166/166 tooling tests, document consistency, assembly-tier check, and executable F.4
> normalization all pass. No production code, schema, executable semantics, workflow, or enforcement changed.
>
> **Last Updated (prior):** September 2, 2026 — **A3 preflight complete; implementation plan v0.33.**
> Removed a contradictory live paragraph that said A2 remained open and A3 blocked immediately after
> the same section correctly recorded A2 closed. Each specification now has a normative-core slice and
> a supporting-surface slice, followed by combined reconciliation and atomic reapproval/landing. Intermediate commits remain
> unapproved drafts and do not claim A8 enforcement. No #19/#20 normative file, schema, executable
> semantics, code, workflow, or enforcement changed.
>
> **Last Updated (prior):** September 2, 2026 — **A2 IS CLOSED. All seven conditions satisfied; A3 is unblocked.**
> Row 7 is satisfied: the approved candidate merged to `main` at `693db56`, and the landed material subject was **recomputed** — not assumed — to `4160b164…`. It is identical at `1f0e68a` (reviewed by `A2-RUN-011`), `9954e90` (approved), `0221491` (branch head), `693db56` (merge commit) and `origin/main`. **Nothing changed on the way in**, which is the check a digest-bound approval exists to make possible: a merge can reorder, drop or transform content, and "the PR merged" is not evidence that what landed is what was approved. Integration plan v0.31 → v0.32; A2 closure record v0.14 → **v1.0, `CLOSED`**.
>
> The seven conditions, and what each rests on: scope map (§2); canonical schemas with a single control source (§3); executable fixtures (§4); a fresh independent review over the pushed candidate — `A2-RUN-011` over `1f0e68a`, **no findings**, the first round in eleven after which nothing followed into the contract; every finding terminal — twenty-three, all `Blocker`/`Resolved`; project-owner approval bound to the digest; and the verified landing.
>
> **Deliberately NOT done.** No review run is marked `CONVERGED` and none carries `final_review`. FR-AG-019/020 convergence is a separate question from FR-AG-018's fresh review, the seven-condition gate never required it, and review runs are immutable snapshots that must not be retro-labelled. The test enforcing this was left in place rather than relaxed to fit the new state — relaxing a check to match a result you want is the failure mode this record spent eleven rounds documenting.
>
> **What closure does and does not mean.** It binds one artifact, named by digest. It does not put the contract beyond revision: any later change inside the material subject is a change to an **approved** contract and takes the A5/A6 schema-evolution route, and the approval does not transfer to a different digest. **A3 is unblocked** — approval, terminal finding state, matching landing and this closure update all hold — but unblocked is not started; beginning A3 remains a separate decision.
>
> Records only. No schema, executable semantics, `REFERENCE_SEMANTICS_VERSION`, fixture, finding, #19/#20 normative file, `src/`, `.cs`, `.asmdef`, gameplay, save, RNG, tuning, or CI job-definition behavior changed; discovery holds at 166 tests, 0 skipped.
>
> **Last Updated (prior):** September 1, 2026 — **A2 closure condition 6 recorded: the project owner approved the candidate. Row 7 (landing) is all that remains.**
> The owner approved the candidate at `9954e90`, material subject digest `4160b164…` — the same subject `A2-RUN-011` reviewed at `1f0e68a`, unchanged since. Integration plan v0.30 → v0.31; A2 closure record v0.13 → v0.14, whose §6 is rewritten from "no approval is recorded" to the recorded approval.
>
> **The approval is bound to that digest and does not transfer.** Any change inside the material subject returns row 6 to PENDING and requires a fresh approval against the new digest; files the subject excludes — tracking prose, the review ledger, CI configuration — may change without disturbing it. That boundary is `in_material_subject`, not a judgement call.
>
> **Row 7 is now the only outstanding condition:** merge the candidate onto the base A3 builds on, verify the landed material subject still recomputes to `4160b164…`, then mark the closure record `CLOSED`. No run is marked `CONVERGED` and none carries `final_review` — that lock is tied to row 7 and is not what owner approval releases; a test enforces it. **A2 stays OPEN until the approved candidate lands, and A3 stays BLOCKED.**
>
> Records only. No schema, executable semantics, `REFERENCE_SEMANTICS_VERSION`, fixture, finding, #19/#20 normative file, `src/`, `.cs`, `.asmdef`, gameplay, save, RNG, tuning, or CI job-definition behavior changed; discovery holds at 166 tests, 0 skipped.
>
> **Last Updated (prior):** September 1, 2026 — **A2 closure condition 4 is SATISFIED. Round 11 found nothing, and nothing followed it into the contract.**
> `A2-RUN-011` is an independent review of `1f0e68a` as pushed that returned **no findings**. That is the first time in this series a round did not move the contract, and it is exactly what row 4 has required since its v0.4 retraction: every earlier round found something real, the fix changed the material subject, and the round that reviewed the pre-fix tree no longer described the artifact. Integration plan v0.29 → v0.30; A2 closure record v0.12 → v0.13.
>
> **The claim is machine-checked, not argued.** The material subject digest `4160b164…` recomputes identically from `1f0e68a` and from the current tree, and `test_closure_condition_4_is_only_claimed_with_a_review_of_this_tree` fails the cell if that ever stops holding. Corrections made *after* the round are confined to files the material subject excludes by construction — the ledger entry recording the run (§3.8: recording a run must not recursively invalidate the subject it records), tracking prose, and stale fixture names in a CI comment and in the closure record's §1.
>
> Round 11 also verified two surfaces this record had carried as **explicitly unverified since v0.3** — Governance §3.3 property fields and §7.1 exception fields against the frozen schemas and semantics — and independently confirmed `Spec hygiene checks` at 166/166, 0 skipped, which is the CI-history hardening checked by someone other than its author.
>
> **One stale citation the round did not catch is corrected with the ones it did:** §1's row-4 retraction paragraph named `test_the_current_artifact_has_not_yet_been_reviewed`, a fixture that does not exist; the real gate is `test_closure_condition_4_is_only_claimed_with_a_review_of_this_tree` — the very test that permits the claim being made in that paragraph. **A mechanism for this class is deliberately NOT landed.** A fixture asserting every cited `test_*` name resolves would sit in the fixture suite, which is *inside* the material subject, so landing it would move the digest and re-open row 4 for a twelfth round. That trade belongs to the owner, batched with the next material change — not taken unilaterally to tidy a comment.
>
> **Rows 6 (project-owner approval) and 7 (landing on the A3 base) remain PENDING and are not agent-satisfiable.** No run is marked `CONVERGED` and none carries `final_review`; a test enforces both while the owner gate is open. **A2 stays OPEN and A3 stays BLOCKED.** Eleven rounds, twenty-three findings, all `Blocker` / `Resolved`. Discovery unchanged at 166 tests (149 governance + 9 phantom-stream context + 8 assembly-tier); `recurring-defect-lint: 0 ERROR`. No frozen schema, executable semantics, `REFERENCE_SEMANTICS_VERSION`, #19/#20 normative file, `src/`, `.cs`, `.asmdef`, gameplay, save, RNG, tuning, or CI job-definition behavior changed.
>
> **Last Updated (prior):** September 1, 2026 — **The CI provenance guard is now itself guarded. Test/workflow-adjacent; frozen semantics untouched.**
> `fetch-depth: 0` is a single line, and deleting it would silently return both history-dependent fixtures to skipping with `Spec hygiene checks` still green — the exact blind spot the previous entry closed, reachable by a one-line edit. At the round-10 reviewer's recommendation, landed before round 11.
>
> A missing-history condition is now a **failure** whenever `GITHUB_ACTIONS=true`, with `GOVERNANCE_REQUIRE_HISTORY=1` additionally arming it for CI systems that do not set the GitHub marker. **The CI marker is the trigger rather than an opt-in flag** — a guard you must remember to enable is the class of guard this exists to replace. Locally the skip is preserved: a contributor with a shallow clone gets an honest skip, not a red suite. All three skip paths route through one `unverifiable` helper — missing named revisions in *either* fixture, and incomplete ledger publication history.
>
> **The guard's own arming is pinned, because that is `A2-R10-001`'s finding applied to this guard.** That finding was never really about a version constant: it was that a fixture read as coverage while being unable to fail for the reason anyone cared about. `test_the_ci_history_guard_is_not_inert` therefore pins both directions and both triggers. Measured on a depth-1 clone: unarmed `166 tests, OK (skipped=2)`; `GITHUB_ACTIONS=true` → `FAILED (failures=2)`; `GOVERNANCE_REQUIRE_HISTORY=1` → `FAILED (failures=2)`. On full history with `GITHUB_ACTIONS=true`: `166 tests, OK`.
>
> **Recorded consequence — push the publish→bind pair, never the publishing commit alone.** At a commit that publishes a finding but before the commit binding its `at`, the ledger is genuinely inconsistent and the equality regression now *fails* rather than skipping. Verified by executing at `1635aa3`: `A2-R10-001-E1 does not equal its first publication commit time` (`01:51:50Z` recorded against `01:53:27Z` published). That is the equality rule working; it was previously masked in CI by the shallow checkout, and is stated here rather than left to be rediscovered as a mystery red.
>
> **No frozen executable semantics changed, so no `REFERENCE_SEMANTICS_VERSION` bump is owed** — this is test-harness behaviour, not `reference_semantics.py`. The reviewed candidate does change, which is why it lands before round 11. Integration plan v0.28 → v0.29; A2 closure record v0.11 → v0.12. Discovery runs 166 tests across three suites (149 governance + 9 phantom-stream context + 8 assembly-tier); `recurring-defect-lint: 0 ERROR`. Row 4 remains PENDING; **A2 stays OPEN and A3 stays BLOCKED.**
>
> **Shim-gate state, verified not assumed:** `MatchEngine.Tests` **472 passed / 1 failed / 11 skipped**. The single failure is `sim_match_engine_close_chance` at `meanCosine = −0.165` (bound −0.16) and `goalwardShare = 0.407` (bound 0.42) — *identical* to the values recorded in `close-chance-creation-design.md` §10.9 item 6, the owner-held RED by decision of August 11, 2026. Only that predicate, at those numbers, every other suite green: the expected baseline state, not a new failure and not this branch's. This branch changes no `src/` file at all. **Do not rebaseline.** No #19/#20 normative file, `src/`, `.cs`, `.asmdef`, gameplay, save, RNG, tuning, or CI job-definition behavior changed.
>
> **Last Updated (prior):** September 1, 2026 — **CI now runs the two provenance checks it had always skipped. Workflow/tracking only.**
> Acting on round 10's evidence note instead of only recording it. Both history-dependent fixtures — `test_every_recorded_digest_matches_the_revision_it_names` and `test_status_timestamps_equal_first_publication_commit_time` — **skipped in every CI run of this candidate**, because `Spec hygiene checks` checked out at the `actions/checkout` default depth of 1. The digest chain across all ten review rounds and the `at`-equals-first-publication rule are what the A2 record's provenance claims rest on, and neither had ever been verified by the gate — only on whichever contributor happened to run the suite against a full local clone. A green badge is not evidence of a check that never ran.
>
> `spec-hygiene` now sets `fetch-depth: 0`, scoped to that job alone; every other job stays shallow, so the cost is one full-history fetch on the one job that needs it. All ten revisions the ledger names were confirmed ancestors of the candidate head before landing, so the fetch reaches each of them rather than trading a skip for a failure.
>
> The skip path stays reachable and stays correct — a shallow clone still cannot verify these and still says so, naming what it could not reach, which is `A2-R5-001`'s all-or-nothing rule. It is simply no longer the CI path. Reproduce it with `git clone --depth 1 file://$PWD <dir> -b <branch>`: `165 tests, OK (skipped=2)`.
>
> Integration plan v0.27 → v0.28; A2 closure record v0.10 → v0.11, whose §4 and §8.1 said CI checks out shallow and no longer do. **No fixture, schema, executable semantics, or finding changed**; discovery holds at 148 governance + 9 phantom-stream context + 8 assembly-tier = 165, and `recurring-defect-lint: 0 ERROR`. Row 4 remains PENDING; **A2 stays OPEN and A3 stays BLOCKED.** No #19/#20 normative file, `src/`, `.cs`, `.asmdef`, gameplay, save, RNG, or tuning behavior changed. **CI job configuration did change** — that is the point of the entry.
>
> **Last Updated (prior):** September 1, 2026 — **A2 round 10: the semantics version froze while the semantics changed. Planning/tooling only.**
> An independent review of `6bce84f` — the pass round 9 owed — found one defect. The integration plan advances to v0.27 and `docs/tracking/a2-schema-semantics-closure.md` to v0.10. **A2 stays OPEN and A3 stays BLOCKED.**
>
> **A2-R10-001 (Medium).** Rounds 8 and 9 changed three admission rules — activation-baseline admission, proof execution/subject binding, and disable-anchor validation — while `REFERENCE_SEMANTICS_VERSION` stayed at `2.0.0`. That value is a field of the proof-closure subject, so it is an **input to** `subject_scope_digest`, and `assess_proof_freshness` compares it by equality to raise `proof-semantics-changed`. Two materially different semantic policies sharing one value defeats that identity contract. The module's own history sets the rule it departed from: `1.0.0 → 1.9.0` bumped once per semantic-change commit, and plan v0.19 reserved MAJOR for the import-contract break. The value advances to **2.1.0** — MINOR, covering both rounds together, restored rather than back-dated — and the versioning policy is now stated at the constant, since leaving it implicit is what let it lapse. No proof artifact exists in the repository, so nothing recorded is invalidated: the bump is mechanically inert today and made for honest signalling, the same reasoning v0.19 applied when it restored `2.0.0`.
>
> **A guard was already there and did not help.** `test_reference_semantics_version_is_pinned` forces a bump to be a deliberate edit, so the value could never drift by accident — but it asserts only that the version *is what it is*, never that it *moved when the semantics did*. Both rounds passed green with the line untouched. A pin that cannot fail for the reason you care about reads as coverage and is not. The replacement is honest about its own limit: whether a bump was owed is a judgement no fixture can settle, so the new mechanism locks only what is mechanical — the constant against every document citing it — and the judgement is written into the pin's docstring rather than implied away.
>
> **A recording correction, not a defect.** Discovery results had been reported as `0 skipped` without saying which run that describes. `Spec hygiene checks` uses `actions/checkout@v4` with no `fetch-depth`, so CI runs at depth 1, where exactly two history-dependent fixtures skip — `test_every_recorded_digest_matches_the_revision_it_names` and `test_status_timestamps_equal_first_publication_commit_time` — each naming every revision it could not reach. That is `A2-R5-001`'s all-or-nothing rule working as designed. §4 now separates the two runs and gives the command to reproduce the CI condition; a `0 skipped` claim is about full history only.
>
> Round 10's corrections are again non-independent, so a **round-11 pass is owed before row 4 is claimed.** The ledger now carries ten rounds and twenty-three findings, all `Blocker` / `Resolved`. Discovery runs 165 tests across three suites (148 governance + 9 phantom-stream context + 8 assembly-tier) on full history, 2 skipped under a shallow checkout. `recurring-defect-lint: 0 ERROR`. Approved Governance v0.10/A0 and #19/#20 normative files are unchanged. No `src/`, `.cs`, `.asmdef`, gameplay, save, RNG, tuning, or CI-status behavior changed.
>
> **Last Updated (prior):** September 1, 2026 — **A2 round 9: the round-8 anti-ratchet fix was itself a regression. Planning/tooling only.**
> A verification pass over the round-8 corrections found that one of them broke the plan it was enforcing. The integration plan advances to v0.26 and `docs/tracking/a2-schema-semantics-closure.md` to v0.9. **A2 stays OPEN and A3 stays BLOCKED.**
>
> **A2-R9-001 (Medium).** The `A2-R8-001` fix rejected **every** baseline addition measured against a trusted prior. But §3.9 declares `inactive → migration` a legal transition and an `inactive` baseline is required to be mechanically empty, so entering migration necessarily adds items — and was left unreachable. This was not abstract: the repository's own committed `temporary-activation-baseline.json` is `inactive` and empty, and its only remaining forward path was `strict`, which requires zero violations everywhere. Reproduced against that document — the transition raised at `c927a95` and was accepted at `a034fc3`, so the round-8 fix introduced it. Additions are now permitted only on the `inactive → migration` edge, the single act §3.9 requires to exist: cataloguing the pre-existing set. The exemption cannot be re-entered, because no transition returns to `inactive` — the catalogue is populated exactly once and never grows again, and growth from a migration prior stays rejected and is locked.
>
> **How it survived is the part worth keeping.** Round 8 added fixtures proving the illegitimate path now fails, and none proving the legitimate path still works. A rule that tightens needs both; asserting only the first is how a fix passes its own review. And the suite could not have caught it regardless — every `prior_baseline` fixture in the file passed a *migration* prior, so an `inactive` one was never constructed. That is round 8's own finding about fixture-bounded differentials (`A2-R8-003`) recurring **inside the commit that recorded it**, which is the strongest available argument that the lesson needs a mechanism rather than a note. Two of the three new fixtures fail at `c927a95`, and one asserts the committed baseline document rather than a constructed one.
>
> **A2-R9-002 (Low).** This plan's header read `**Version:** 0.18` while its own version history stood at v0.25 — the header last moved at `dae398a`, and seven revisions appended history rows without bumping it, so every citation of v0.19–v0.25 resolved against a document self-describing as v0.18. `doc-consistency-check` does not compare a header against its own history. The A2 closure record and the governing Governance document were checked and are not drifted.
>
> Also reconciles the two apparently opposed rationales inside `validate_proof_artifact` — an absent execution is an obligation the contract does not state, a required field's meaning is not — and resolves the normalized rather than the raw disable-anchor selector (behaviour-preserving today, since normalization is idempotent).
>
> **Round 9 is not independent.** It was performed by the same assistant that produced the round-8 remediation, in a separate session with no shared context, which is weaker than rounds 4–7 and is not put forward as equivalent. A **round-10 independent pass is owed before row 4 is claimed.** The ledger now carries nine rounds and twenty-two findings, all `Blocker` / `Resolved`. Discovery runs 164 tests across three suites (147 governance + 9 phantom-stream context + 8 assembly-tier). `recurring-defect-lint: 0 ERROR`. Approved Governance v0.10/A0 and #19/#20 normative files are unchanged. No `src/`, `.cs`, `.asmdef`, gameplay, save, RNG, tuning, or CI-status behavior changed.
>
> **Last Updated (prior):** September 1, 2026 — **A2 round 8: three contract defects from the PR #347 automated review. Planning/tooling only.**
> The automated review on pull request #347 found **three defects in the frozen contract itself** — the first round since round 3 to do so rather than in the record-keeping. All three were reproduced before being accepted, and all three are fixed and pinned. The integration plan advances to v0.25 and `docs/tracking/a2-schema-semantics-closure.md` to v0.8. **A2 stays OPEN and A3 stays BLOCKED.**
>
> **A2-R8-001 (Medium).** Activation-baseline additions were rejected only once the baseline was sealed. A newly discovered violation could be written into `items` **and** into `current_violation_ids` in the same revision, so the live-set comparison saw nothing new and the anti-ratchet never engaged — an unsealed migration baseline could absorb regressions indefinitely. §3.9 states "New violations fail" without qualification; the code had implemented only the narrower sealed-baseline sentence beside it. Additions are now measured against the trusted prior baseline whatever its seal state; shrinking and holding steady stay legal and are locked.
>
> **A2-R8-002 (Medium).** `validate_proof_artifact` checked each execution's state but never compared its `subject_scope_digest` with the artifact's, so that required field was decorative and any syntactically valid digest passed — a passing record copied from an older or unrelated subject certified the current proof. Equality is now required. Recorded as a **deliberate narrowing**: the plan defines no subsumption relation between scopes, so equality is the only mechanically defined binding available at freeze time, and widening it for a broader execution is a schema-evolution decision for A5/A6 to take explicitly rather than a gap to leave open. This reverses an earlier judgement in this series — the check was considered and declined as "inventing a rule", which was an over-correction: enforcing that a required field means what it says is not the same as adding an obligation.
>
> **A2-R8-003 (Medium).** An `intentionally-disabled` contract whose `disable_anchor` was `{}` passed `validate_integration_contracts_document`, which checked only that the anchor was a dict, while the canonical schema required `selector`, `operator` and `expected`. A live schema/semantics divergence — and **one the differential could not see**, because no fixture carried a malformed anchor. That is worth recording about the differential itself: it proves agreement only on the fixtures chosen for it. The anchor's complete typed shape now has one owner, called from both the contract validator and the evaluator so the two cannot disagree, and malformed-anchor fixtures close the coverage gap.
>
> Round 7 (`A2-R7-001`) is recorded for completeness: the owner's PR #346 landing, which replaced round 6's interval bound on status timestamps with equality against the first-publication commit time, because an interval check still admitted any invented value inside it. The ledger now carries eight rounds and twenty findings, all `Blocker` / `Resolved`. Discovery runs 160 tests across three suites (143 governance + 9 phantom-stream context + 8 assembly-tier). `recurring-defect-lint: 0 ERROR`. Approved Governance v0.10/A0 and #19/#20 normative files are unchanged. No `src/`, `.cs`, `.asmdef`, gameplay, save, RNG, tuning, or CI-status behavior changed.
>
> **Last Updated (prior):** September 1, 2026 — **A2 review-record publication provenance corrected. Planning/tooling only.**
> Follow-up review found that v0.23/v0.6 still described `at` as publication/recording provenance while the regression accepted any timestamp inside the reviewed-artifact→publication interval. `A2-R6-001` itself demonstrated the mismatch: `20:29:43Z` was accepted although the finding first appeared in committed history in `c349fb6` at `23:13:27Z`.
>
> The model is now exact: `at` equals the first-publication commit time for that finding; reviewed/resolving revisions remain separate evidence and no exact review-event time is invented. The regression requires equality and separately requires publication strictly after the reviewed artifact, with all-or-nothing behavior when publication history is incomplete. Integration plan v0.23 → v0.24; A2 closure record v0.6 → v0.7. Test cardinality is unchanged at 139 governance + 9 lint + 8 assembly-tier = 156. Row 4 remains PENDING. No frozen schema/semantics mechanism, #19/#20 normative file, `src/`, gameplay, save, RNG, or tuning behavior changed.
>
> **Last Updated (prior):** September 1, 2026 — **A2 third independent review; condition 4 still open. Planning/tooling only.**
> A third independent pass over `7d4e949` found one substantive defect, again inside the previous round's remediation. The integration plan advances v0.22 → v0.23 and `docs/tracking/a2-schema-semantics-closure.md` v0.5 → v0.6. Conditions 1, 2, 3 and 5 stand; row 4 stays **PENDING**. **A2 stays OPEN and A3 stays BLOCKED.**
>
> **A2-R6-001 (Medium).** Round 5's timestamp remediation replaced fictional *future* timestamps with fictional *earlier* ones. A finding's `Open` event was stamped at the commit time of the artifact reviewed — `11547d4` at `19:24:32Z` for round 4 — but an independent review necessarily happens **after** the artifact it reviews is pushed, so the record placed each discovery at or before the thing discovered. The resolution stamps were not commit times at all: `20:11:08Z` against `7d4e949`'s `20:13:03Z`, while the record described them as "the commit time that carried the fix". `test_status_history_is_neither_future_dated_nor_out_of_order` checked only `<=` wall clock and monotonicity, so it could see neither error — the document and its test again claimed more than they verified.
>
> **The exact review times are not recoverable, and the ledger no longer pretends otherwise.** `at` is now defined as the time a transition was **recorded** into this ledger, derived from the commit that first published the finding; the reviewed and resolving revisions are carried in `evidence`. The review happened somewhere between the artifact it reviewed and the record of it, and that interval is not invented away. The regression brackets every timestamp inside it:
>
> > commit time of the artifact reviewed **<** `at` **≤** the commit that published the record.
>
> The **strict** lower bound was not free. A first cut used `>=`, and a probe showed it did not catch the very defect it replaced, because the bad value was exactly the reviewed commit's timestamp — a green test over a known-bad ledger. All three shapes are now proven to fail: dated at the reviewed artifact, dated before it, and dated after publication.
>
> Every independent round so far has found a defect inside the preceding non-independent remediation — rounds 4, 5 and 6 in turn. Round 6's corrections are again non-independent, so a **round-7** pass over the pushed result is owed before condition 4 is claimed. Discovery runs 156 tests across three suites (139 governance + 9 phantom-stream context + 8 assembly-tier). `recurring-defect-lint: 0 ERROR`. Approved Governance v0.10/A0 and #19/#20 normative files are unchanged. No `src/`, `.cs`, `.asmdef`, gameplay, save, RNG, tuning, or CI-status behavior changed.
>
> **Last Updated (prior):** September 1, 2026 — **A2 second independent review; condition 4 still open. Planning/tooling only.**
> A second independent pass over `5ebc3f7` found three further issues, **two of them inside the round-4 remediation itself**. The integration plan advances v0.21 → v0.22 and `docs/tracking/a2-schema-semantics-closure.md` v0.4 → v0.5. Conditions 1, 2, 3 and 5 stand; row 4 stays **PENDING**. **A2 stays OPEN and A3 stays BLOCKED.**
>
> **A2-R5-001 (Medium).** The historical-digest verification could report PASS having checked only part of what its name claimed. It skipped unavailable revisions one at a time and skipped the test only when *none* resolved, so a partial-history checkout could verify one digest of five, ignore the rest, and still show green under `test_every_round_digest_recomputes_from_the_tree_it_names`. The default CI checkout is shallow, so that was the expected path rather than an edge case. Verification is now **all-or-nothing**: one missing revision skips the whole check and names what is absent. Proven both ways — it skips wholesale on partial history, and still fails loudly on a wrong digest.
>
> **A2-R5-002 (Medium).** The durable ledger recorded events that had not happened. Every `A2-R4-*` status event carried an invented `21:00:00Z`, while `5ebc3f7` — the commit asserting those events were complete — was created at `19:51:29Z`. Every timestamp now derives from a real commit: a finding is raised at the commit time of the artifact reviewed and resolved at the commit time that carried the fix. A regression rejects a future-dated or out-of-order status history.
>
> **A2-R5-003 (Low).** `A2-R4-002` attributed "required proof MUST be independently reproducible" to FR-AG-034. That sentence is **FR-AG-032**; FR-AG-034 is the rule against unsupported agent assertions. Both are now cited with their own text — FR-AG-034 independently applies to an unbacked reproducibility claim, so the Blocker basis was sound and only the citation was wrong.
>
> Two design cleanups came with them. The round-digest **distinctness** assertion is dropped: governance does not require it, and two rounds may legitimately review an unchanged material subject and correctly carry the same digest. And `test_committed_ledger_validates_against_the_frozen_contract` fed the ledger digests taken *from* the ledger — self-referential, and doubly empty since the freshness branch fires only for a `final_review` run and none carries one. Row 4's closure cell is now mechanically tied to the ledger: it may read Complete only if a recorded round's digest is the current material subject.
>
> Each independent round so far has found a defect the preceding non-independent work did not. Round 5's corrections are themselves non-independent, so a **round-6** pass over the pushed result is owed before condition 4 is claimed. Discovery runs 156 tests across three suites (139 governance + 9 phantom-stream context + 8 assembly-tier). `recurring-defect-lint: 0 ERROR`. Approved Governance v0.10/A0 and #19/#20 normative files are unchanged. No `src/`, `.cs`, `.asmdef`, gameplay, save, RNG, tuning, or CI-status behavior changed.
>
> **Last Updated (prior):** September 1, 2026 — **A2 condition 4 retracted after independent review; planning/tooling only.**
> The first **independent** review of this candidate — a different reviewer, not the assistant that wrote it — found that closure condition 4 had been claimed before the artifact naming it was ever reviewed. Row 4 returns to **PENDING**. The integration plan advances v0.20 → v0.21 and `docs/tracking/a2-schema-semantics-closure.md` v0.3 → v0.4. Conditions 1, 2, 3 and 5 stand. **A2 stays OPEN and A3 stays BLOCKED.**
>
> **A2-R4-001 (High).** Round 3 reviewed `678f0f2`. The material subject then moved by 150 lines — the `A2-R3-001` fix, its schema change, its tests — and `11547d4`, the commit that carried the completion claim, was itself never reviewed. The v0.3 record compounded this by binding round 3 to the *post-fix* digest for a review performed on the *pre-fix* tree. The A2 gate's pushed-candidate wording is deliberately stronger than FR-AG-018's bare "current artifact", and the party satisfying a condition does not get to relax it. Round 3 now binds `678f0f2`; round 4 binds `11547d4`; `test_the_current_artifact_has_not_yet_been_reviewed` fails if any round claims the working tree without a review of it. This is the argument for reviewer independence made concretely: three non-independent rounds missed it and the first independent one did not.
>
> **A2-R4-002 (Medium).** `DurableReviewLedgerTests` recomputed only the latest round's digest and merely asserted the rest were distinct, while the closure record claimed the digests were mechanically reproducible and that no trust in the document was required. Distinctness is not identity. Every recorded digest is now recomputed from the commit its scope names — the ledger is self-describing, each run's scope carrying `at <rev>`, so the check needs no second table to drift against. The bound is stated rather than glossed: CI checks out shallow, so an unavailable revision **skips explicitly** instead of passing as though it had run.
>
> **A2-R4-003 (Low).** The phantom-stream probe was re-run with its cases separated after they were seen to interfere, which demonstrated a context-bleed mode rather than testing it. New `tools/tests/test_recurring_defect_lint.py` adds nine **mixed** positive/negative fixtures. The reviewer's concern is confirmed for an adjacent unrelated negation, and that bound is now pinned by an explicit test rather than left undocumented: it pre-dates this work, and narrowing the window to zero re-raises three genuinely wrapped negations elsewhere in `docs/`, so the trade is deliberate. One part of the concern did not hold on checking — a positive-looking bullet inside a non-goals list is *correct* suppression, because such a bullet is elliptical; a neutral list lead-in is verified not to suppress.
>
> Discovery runs 155 tests across three suites (138 governance + 9 phantom-stream context + 8 assembly-tier). `recurring-defect-lint: 0 ERROR`. Approved Governance v0.10/A0 and #19/#20 normative files are unchanged. No `src/`, `.cs`, `.asmdef`, gameplay, save, RNG, tuning, or CI-status behavior changed.
>
> **Last Updated (prior):** September 1, 2026 — **A2 closure conditions 1–5 satisfied; review recorded. Planning/tooling only.**
> The five agent-satisfiable A2 closure conditions are now met and evidenced. Conditions 6 (project-owner approval) and 7 (landing) are **not agent-satisfiable and remain PENDING**; **A2 stays OPEN and A3 stays BLOCKED.** The integration plan advances v0.19 → v0.20 and `docs/tracking/a2-schema-semantics-closure.md` v0.2 → v0.3 with a new §8 carrying the review record.
>
> `review-ledger.json` stops being an empty seed and becomes the real durable record: three rounds under series `A2-SCHEMA-FREEZE`, nine findings, all `Blocker` / `Resolved`. Each run binds the material subject digest of the tree it **actually** reviewed — `e7a3ba13` for round 1, `dae398a6` for round 2, this candidate for round 3 — rather than one digest stamped across all three, which would have misreported the earlier rounds. The subject excludes the ledger itself, per §3.8's non-recursion rule. `DurableReviewLedgerTests` recomputes the latest digest and proves the three are distinct, so the closure record's digest bundle is verifiable on any checkout instead of asserted. Following the A0 record's rule, each finding cites the specific pre-existing condition it made false; per Governance §1.6 none cites a gate this review authored, and the one finding resting on project-owner designation rather than a pre-existing rule is recorded as such.
>
> Round 3 read the surfaces the earlier rounds had not — exception routing, property transitions, ledger convergence, baseline transitions, the new proof contract — probing twelve constructed violations. Eleven were correctly rejected; two apparent acceptances proved correct on checking the plan; one was real. **A2-R3-001:** `exception_route` evaluates its property branch first and unconditionally, and `property_id` carried no namespace constraint, so a property registered as `FR-CS-046` returned `governance-property` with `governance_exception_allowed` true — silently moving a Code Standards waiver into `exceptions.json`, the crossing §3.6 forbids. Both the schema and the semantics now reject a `property_id` in a #19/#20 namespace from one control-data source. §3.6's carve-out survives: an admitted property cites the FR requirement instead of taking its id.
>
> `recurring-defect-lint.py` reported three ERR-041-012 phantom-stream ERRORs that were all false positives — legitimate negations the detector could not see: a markdown-emphasised `**no** registered stream`, a `does not convert … into a registered … stream` clause, and a bullet whose negating lead-in sat seven lines above a two-line window. Per the class's own stated intent that legitimate negations must not re-raise, the detector was taught all three forms rather than the findings suppressed: negations are matched against the de-emphasised window as well as the raw one, and a list item now inherits its lead-in. Verified not to blind it — four constructed positive claims still fire. `recurring-defect-lint: 0 ERROR`.
>
> No review run is marked `CONVERGED` or `final_review`, and a test enforces that: convergence is not an agent's to declare while the owner gate is open. The review carries an explicit reviewer-independence limitation on the A0 precedent — FR-AG-018 requires a fresh review over the current artifact, not a different reviewer, and no independence is claimed. Governance fixtures grow 128 → 137; with 8 assembly-tier fixtures, `tools/tests/test_*.py` discovers 145 tooling tests. Approved Governance v0.10/A0 and #19/#20 normative files are unchanged. No `src/`, `.cs`, `.asmdef`, gameplay, save, RNG, tuning, or CI-status behavior changed.
>
> **Last Updated (prior):** September 1, 2026 — **A2 second-review remediation; planning/tooling only.**
> The second review confirmed the pushed candidate, the eight-category/ten-schema/seven-registry map, the IP-5 single enum source, and the stdlib constraint, then raised five further findings. All five land here; three were closure blockers. The integration plan advances v0.18 → v0.19 and `docs/tracking/a2-schema-semantics-closure.md` v0.1 → v0.2. **A2 remains OPEN and A3 remains BLOCKED**: this closes findings, not the gate.
>
> `proof-artifact.schema.json` was the one frozen machine contract with no executable counterpart. New `validate_proof_artifact` mirrors the shape and binds it to A2 execution truth — a `pass` result cannot outrun a non-passing execution, and a `bounded` result converts only the states `evaluate_execution_truth` permits, through a substitute #19 explicitly allows. The review-ledger and activation-baseline validators now fail closed like the property registry: an omitted trusted prior, an omitted live violation set, or a missing current digest for a `final_review` run raises typed uncertainty instead of passing silently, with `None` reserved for the positive claim that no prior existed. `strict_activation` is deliberately left defaulting off — it adds a requirement rather than relaxing one.
>
> All ten schemas now pin a canonical `$id`, so the relative `$ref`s resolve by RFC 3986 URI resolution rather than incidental filename lookup. New `tools/architecture-governance/schema_validator.py` is a bounded Draft 2020-12 validator over exactly the keyword subset these schemas use; it raises on any keyword it does not implement, so the new one-directional differential — every semantically accepted fixture must also satisfy its frozen schema — cannot pass vacuously. That implication is one-directional by design: the semantics enforce append-only history, legal transitions, closure and Disposition×Status, which JSON Schema cannot express. The differential found no live divergence and is a regression guard; it was proven live by injecting a schema-only `required` field and observing the test fail.
>
> `REFERENCE_SEMANTICS_VERSION` is restored to v2.0.0 by owner decision. v0.17 set it, v0.18 reverted it to v1.10.0, and the reversion was wrong twice over: the module now raises at import without `common.schema.json`, a mandatory external dependency that breaks any standalone import contract, and the reverted sequence published 1.9.0 → 2.0.0 → 1.10.0. `assess_proof_freshness` compares this value by equality, not ordering, so the restore is mechanically inert. Governance fixtures grow 104 → 128; with 8 assembly-tier fixtures, `tools/tests/test_*.py` discovers 136 tooling tests. Approved Governance v0.10/A0 and #19/#20 normative files are unchanged. No `src/`, `.cs`, `.asmdef`, gameplay, save, RNG, tuning, or CI-status behavior changed.
>
> **Last Updated (prior):** September 1, 2026 — **A2 review corrections; closure remains OPEN.**
> The first attempted review could not resolve local commit `b5e80832` from GitHub and correctly rejected it as unreviewable. It also exposed two real gaps: shared enum/control values were duplicated between Python and the JSON schemas, violating IP-5, and integration-plan v0.17 incorrectly equated landing with A2 closure. The integration plan advances v0.17 → v0.18 and adds a seven-condition A2 gate with fresh pushed-candidate review, terminal findings, non-delegable owner approval, approved digest binding, and landing all distinct. New `docs/tracking/a2-schema-semantics-closure.md` v0.1 owns that gate; A2 is implemented but OPEN and A3 remains BLOCKED.
>
> `reference_semantics.py` is corrected from the unpublished v2.0.0 label to v1.10.0 and now consumes `common.schema.json` as the single machine source for enums, transitions, fallback maps, and dependency-relation groups using only stdlib `json`/`pathlib`. All other schemas reference the common definitions. New regressions reject independent enum declarations, schema-reference drift, selector-discriminator mismatch, and non-stdlib imports. The explicit mapping is eight §3.1 categories → ten schemas (eight category schemas + common control + A4 bootstrap auxiliary) → seven seed state registries because proof evidence is per-proof, not an empty registry.
>
> Governance fixtures grow 102 → 104; with 8 assembly-tier fixtures, discovery runs 112 tooling tests. No #19/#20 normative file, `src/`, `.cs`, `.asmdef`, gameplay, save, RNG, tuning, or CI-status behavior changed. The branch must be pushed and freshly reviewed before any approval can be requested.
>
> **Last Updated (prior):** September 1, 2026 — **A2 canonical schema and governance-state freeze completed.**
> The final A2 slice creates `docs/tracking/architecture-governance/` with seven schema-v1.0.0 seed artifacts and canonical Draft 2020-12 schemas for classification, bootstrap intent, integration contracts, applicability, properties, Governance exceptions, reusable proof, review state, and the temporary activation baseline. The integration plan advances v0.16 → v0.17 and records A2 COMPLETE; A3 remains blocked only until this slice lands.
>
> `reference_semantics.py` advances v1.9.0 → v2.0.0. Property transitions are checked against Governance §3.1 and a trusted merge-base with append-only decision/revalidation history; an unavailable prior is explicit uncertainty. `exceptions.json` accepts only admitted APs that permit exceptions and cannot absorb FR-CS/FR-TS owner-specific waivers. Review runs and finding histories enforce the four Disposition×Status mappings, severity-independent convergence, current-subject final-review freshness, zero-finding final reviews, and round-budget NON-CONVERGED behavior. The finite activation baseline rejects new violations, cannot grow after sealing, and must be strict/sealed/mechanically empty at strict activation.
>
> Governance fixtures grow 77 → 102; with 8 assembly-tier fixtures, existing `tools/tests/test_*.py` discovery runs 110 tooling tests. Approved Governance v0.10/A0 and #19/#20 normative files are unchanged. No `src/`, `.cs`, `.asmdef`, gameplay, save, RNG, tuning, or CI-status behavior changed.
>
> **Last Updated (prior):** September 1, 2026 — **A2 selector type-ID canonicalization.**
> Codex's remaining live P2 exposed an under-specified selector producer contract: `parameter_type_ids` carried arbitrary non-empty strings, so a producer using plain `System.Int32` for both `M(int)` and legal `M(ref int)` would collapse the overloads. The integration plan advances v0.15 → v0.16 and `reference_semantics.py` v1.8.0 → v1.9.0. Selector type IDs are now normatively pinned to the C# XML documentation ID type-signature convention emitted from compiler symbols; by-reference parameters therefore use the `@` suffix.
>
> A regression proves `M(System.Int32)` and `M(System.Int32@)` have distinct selector keys and each resolves to its own symbol. No `parameter_ref_kinds` field was added: the canonical type-ID convention already carries byref identity and also fixes the previously unstated spelling contract for generic parameters/types, arrays, pointers, and nested type structure. Selector-v1 shape, execution truth, applicability/proof semantics, approved Governance v0.10/A0, and #19/#20 normative files are unchanged. Governance fixtures grow 76 → 77; with 8 assembly-tier fixtures, `tools/tests/test_*.py` discovers 85 tooling tests. No `src/`, `.cs`, `.asmdef`, gameplay, save, RNG, or tuning value changed.
>
> **Last Updated (prior):** September 1, 2026 — **A2 enum-validation and context-specificity residual hardening.**
> Verification of v1.7.0 found three residual freeze-surface issues outside the rebuilt change-context mechanism. The integration plan advances v0.14 → v0.15 and `reference_semantics.py` v1.7.0 → v1.8.0. Every enum-valued untrusted JSON boundary now uses one typed validator so arrays/objects cannot leak host-language `TypeError`; selector, activation, applicability, dependency/proof, and execution callers retain their domain-specific `SemanticsError` subclasses.
>
> Applicability context specificity now orders otherwise-identical matching rules by `change_types` set width: smaller non-empty sets outrank broader sets, every restricted set outranks generic, and the bounded rank cannot cross one surface-specificity step. Non-strict applicability without current change context now returns `context_complete: false` and `diagnostics: ["missing-change-type"]` instead of silently looking like a complete empty match; proof certification remains blocked independently. Execution truth and the v1.7 subject-side change-context model are otherwise unchanged. Governance fixtures grow 69 → 76; with 8 assembly-tier fixtures, `tools/tests/test_*.py` discovers 84 tooling tests. Approved Governance v0.10/A0 and #19/#20 normative files are unchanged. No `src/`, `.cs`, `.asmdef`, gameplay, save, RNG, or tuning value changed.
>
> **Last Updated (prior):** August 31, 2026 — **A2 applicability change-context model stabilized.**
> Verification of v1.6.0 exposed the mirror failure of the conditional persistence fix: optional rule-level `change_type` allowed an omitted change context to certify an under-scoped proof. The integration plan advances v0.13 → v0.14 and `reference_semantics.py` v1.6.0 → v1.7.0. `change_type` now belongs to the evaluated applicability subject; strict applicability requires exactly one canonical Governance §5.2 change type, and proof closure independently rejects a non-strict applicability result that omits it. Rules may optionally filter that context with `change_types`; an otherwise-identical context-specific rule mechanically outranks its generic counterpart without changing surface precedence.
>
> Persistence/resource closure activation now reads only the current subject context, never `trigger_ref` or obligation payload. Changing the context participates in the applicability digest and proof freshness, and a dedicated regression proves `pure-local-calculation → persistence-boundary` expands scope and stales the earlier proof. Execution truth is unchanged from v1.6.0. Governance fixtures grow 64 → 69; with 8 assembly-tier fixtures, `tools/tests/test_*.py` discovers 77 tooling tests. Approved Governance v0.10/A0 and #19/#20 normative files are unchanged. No `src/`, `.cs`, `.asmdef`, gameplay, save, RNG, or tuning value changed.
>
> **Last Updated (prior):** August 31, 2026 — **A2 conditional-closure and execution-truth hardening.**
> Verification of v1.5.0 exposed one closure overreach and one bounded-substitute ambiguity. The integration plan advances v0.12 → v0.13 and `reference_semantics.py` v1.5.0 → v1.6.0. Applicability rules now carry optional typed Governance §5.2 `change_type`; serializer/schema/resource relations enter any of the four proof closures only when an active matched obligation is `persistence-boundary` or `external-resource-dependency`. Ordinary structural/lifecycle/failure/mutation proofs therefore no longer inherit unrelated persistence surfaces into `subject_scope_digest`.
>
> Execution truth now has its own `ExecutionError` domain. Bounded substitutes are limited to `excluded`, `unavailable`, and `not-run` states with explicit #19 permission and complete approval metadata; `failed`, `skipped`, and `runner-failed` cannot be converted to satisfaction. Proposed FR-TS-094/096 in the Draft A3 package are aligned to that freeze. Governance fixtures grow 58 → 64; with 8 assembly-tier fixtures, `tools/tests/test_*.py` discovers 72 tooling tests. Approved Governance v0.10/A0 and #19/#20 normative files are unchanged. No `src/`, `.cs`, `.asmdef`, gameplay, save, RNG, or tuning value changed.
>
> **Last Updated (prior):** August 31, 2026 — **A2 cross-surface authority correction and execution-truth freeze.**
> A hostile sweep across approved Governance v0.10, the Draft integration plan, and A2 found four semantic conflicts plus the remaining execution-truth obligation. The integration plan advances v0.11 → v0.12 and `reference_semantics.py` v1.4.0 → v1.5.0. A2 now exposes exactly Governance's four proof classes; persistence/external-resource remains a §5.2 trigger surface whose serializer/schema/resource edges participate in applicable structural/lifecycle closures rather than a fifth proof class. Applicability now maps all six structural classifications into the three fallback scopes and derives fallback precedence explicitly. `--changed` reruns when changed material is unmapped, stale, or inside the derived closure, and skips only on proven non-impact.
>
> A2 also freezes execution truth: the seven execution states are executable semantics; only `passed` satisfies an ordinary required execution, while a non-passed state can satisfy only through an explicitly permitted, approved bounded substitute carrying authority, approval, justification, and omitted-surface/uncertainty fields. Governance fixtures grow 50 → 58; with the unchanged 8 assembly-tier fixtures, `tools/tests/test_*.py` discovers 66 tooling tests. Governance v0.10/A0 approval and #19/#20 normative files are unchanged. No `src/`, `.cs`, `.asmdef`, gameplay, save, RNG, or tuning value changed.
>
> **Last Updated (prior):** August 31, 2026 — **A2 third hostile-review hardening; finding contract made explicit.**
> Claude's verification of `bb200ec4` found one missed identity-error wrap and two output-contract ambiguities. `validate_component_identities` now converts a deleted/renamed current selector to `IdentityError`, not raw `SelectorError`. KD-W1 output is now explicitly typed: `stale-tuning-selector` records contract-integrity drift, while `inactive-tuning-change` records the actual KD-W1 tuning prohibition. Staleness is deliberately activation-independent, so active components also report stale governance selectors instead of silently discarding them.
>
> `reference_semantics.py` advances v1.3.0 → v1.4.0 and the governance suite grows 48 → 50 fixtures; total tooling discovery is now 58 tests including the unchanged 8 assembly-tier fixtures. No `src/`, `.cs`, `.asmdef`, gameplay, save, RNG, or tuning value changed.
>
> **Last Updated (prior):** August 31, 2026 — **A2 second hostile-review hardening; stale-selector fail-open closed.**
> Claude's re-review of `f954371` was revalidated against the PR head. KD-W1 now performs an unconditional resolution pass over every inactive contract's tuning selectors before changed-surface filtering, so stale/deleted/renamed governance selectors report even when the caller supplies no matching old selector. Duplicate normalized `component_id` contracts fail closed; duplicate compiler `symbol_key`→selector mappings fail when the semantic-fact universe is indexed; disable-anchor selector drift is rethrown as `ActivationError`; and callers that resolve repeatedly can construct/reuse `SemanticFactIndex` rather than rebuilding the index per lookup.
>
> `reference_semantics.py` advances v1.2.0 → v1.3.0 and its suite grows 42 → 48 fixtures, including a direct version assertion. `assembly-tier-check.py` advances v1.5 → v1.6: machine-report serialization and evidence digests share one finite-JSON helper, so NaN/Infinity are rejected consistently; its suite grows 7 → 8 fixtures. The existing `tools/tests/test_*.py` CI discovery runs all 56 tooling tests. No `src/`, `.cs`, `.asmdef`, gameplay, save, RNG, or tuning value changed.
>
> **Last Updated (prior):** August 31, 2026 — **A2 hostile-review hardening of frozen selector/activation semantics.**
> Claude's review of the original `2be53ff4` slice was revalidated against the current PR. The substantive freeze defects are fixed: KD-W1 now matches canonical selectors so deleted/renamed dormant tuning surfaces become structured violations instead of uncaught resolution errors; exception scopes reject unknown fields; contract/scope component IDs normalize identically; selector-v1 now distinguishes static constructors, indexer overloads, and events; non-finite numeric values are rejected; two stable components cannot bind one compiler `symbol_key`; selector-history entries require a supersession reason; and tooling CI now discovers `tools/tests/test_*.py` rather than hard-coding individual files.
>
> `reference_semantics.py` advances v1.1.0 → v1.2.0 and the reference suite grows 28 → 42 fixtures, now directly covering every selector kind plus `pending-integration`, enum typed values, `not-equals`, deleted tuning surfaces, exception-scope closure, whitespace normalization, NaN/Infinity rejection, duplicate symbol ownership, and selector-history rationale. The previously reported manifest table break had already been fixed in A2 slice 2. No `src/`, `.cs`, `.asmdef`, gameplay, save, RNG, or tuning value changed.
>
> **Last Updated (prior):** August 31, 2026 — **A2 slice 2: applicability + proof closure/freshness semantics.**
> `reference_semantics.py` advances v1.0.0 → v1.1.0. Applicability now evaluates every matching rule, derives precedence mechanically from selector specificity, rejects author-chosen precedence drift, fails equal-precedence conflicts, validates bounded N/A reasons/approvals, and fails unmatched strict subjects. Proof semantics now derive class-specific dependency closures from resolved obligations and typed graph edges, bind applicability + semantic-policy identity into `subject_scope_digest`, separate provenance from freshness, and conservatively fall back when `--changed` encounters an unmapped surface.
>
> The reference suite grows 10 → 28 fixtures. New cases lock deterministic rule-order behavior, specificity/fallback resolution, equal-precedence conflict/coalescing, N/A approval, strict no-match behavior, structural/lifecycle/persistence/executable closure expansion, missing requirement bindings, unrelated-vs-reachable freshness, new reachable dependencies, provenance independence, applicability-scope invalidation, and conservative changed-surface handling. No canonical JSON schemas or review-state transitions land in this slice; those remain A2 work. No `src/`, `.cs`, `.asmdef`, gameplay, save, RNG, or tuning value changed.
>
> **Last Updated (prior):** August 31, 2026 — **A2 slice 1: executable selector/identity/activation semantics.**
> Added `tools/architecture-governance/reference_semantics.py` v1.0.0 as the A2 reference implementation over typed compiler facts; it does not parse C# source. It freezes exact selector-v1 matching for namespaces/types/constructors/overloaded methods/static members and assembly identity, stable `component_id` migration via selector history, typed disable-anchor evaluation, and KD-W1 tuning-surface matching with exact approved exception scope.
>
> Added `tools/tests/test_architecture_governance_semantics.py` with ten fail-closed fixtures covering overload/static discrimination, missing/ambiguous resolution, selector-history identity, anchor drift, required disable metadata, and KD-W1 active/inactive/exception behavior. The existing `Spec hygiene checks` job now runs this suite; no new CI status was created. This is A2 foundation only: applicability, proof-closure/freshness, review-state, and canonical JSON schemas remain for later A2 slices. No `src/`, `.cs`, `.asmdef`, gameplay, save, RNG, or tuning value changed.
>
> **Last Updated (prior):** August 31, 2026 — **PR #344 Markdown-link false-negative and Codex-review corrections.**
> CI's Markdown link check failed only on two live Microsoft Learn references in Code Standards §8;
> the action returned status `0` (transport failure), not HTTP 404/410. Both URLs remain current.
> `.github/markdown-link-check.json` now exempts those two exact URLs only, preserving all other
> Microsoft Learn link checking.
>
> Codex automated review raised three P2 suggestions. Two are valid and fixed: the A0 review record
> now carries a stable reviewer identity (OpenAI ChatGPT / GPT-5.6 Sol, PR #344), and the manifest
> Tracking Documents row now reflects owner approval/A0 CLOSED rather than pending sign-off. The third
> suggestion — broadening FR-AG-020's Blocker-specific budget case — was rejected after full-context
> verification: §9.6 already requires generic budget exhaustion → NON-CONVERGED, Appendix F defines
> NON-CONVERGED as unresolved gating obligations, and §4.7 forbids convergence for every open/invalid
> finding state. Changing approved Governance v0.10 would add redundant clarity, not repair a semantic gap.
>
> Integration plan v0.10 → v0.11 and A0 review record v1.7 → v1.8. Governance v0.10, Approved status,
> canonical adoption digest, and A0 CLOSED state are unchanged. No runtime/code/spec semantics changed.
>
> **Last Updated (prior):** August 31, 2026 — **A0 CLOSED; Project Architecture Governance v0.10 approved.**
> The project owner explicitly approved Governance v0.10. The required adoption order was followed:
> Governance `Status: Draft` → `Approved` first, then the exact approved file was hashed, then the
> canonical adoption SHA-256 `aa1792bf143fb3bc1066176dedb33abc4097045e7d089844edf05ccf9961d8f6` (Git blob `76502282f205f5c4fd77c79c3309766c4dbd4498`) was recorded in
> integration-plan §11 A0. Integration plan advances v0.9 → v0.10; A0 review record v1.6 → v1.7.
>
> Governance semantics were not changed after the converged v0.10 review; its only edit is the Status
> field. The plan also removes a stale pre-A0 SPEC_INDEX-alignment prerequisite that conflicted with
> the already-adopted §9.7/A3–A9 boundary. A0 is now CLOSED and A2 is next.
>
> Planning/tracking documentation only; no `src/`, `.cs`, `.asmdef`, tool, workflow, save, RNG, tuning,
> or simulation behavior changed, so no code gate is owed.
>
> **Last Updated (prior):** August 31, 2026 — **A0 evidence-record reconciliation after Claude's pre-fix critique.**
> Tracking documentation only. `a0-governance-adoption-review.md` v1.5 → v1.6; `file-manifest.md`
> updated. Governance v0.10 and its recorded blob/SHA-256 are unchanged.
>
> Five surviving record/manifest defects were corrected: round-5 verdict text is explicitly historical;
> the current v0.10 §9.6 discharge uses exact checklist labels; the blanket A0-gate Blocker citation is
> replaced by a specific unsatisfied §9 gate condition for every corrective finding; the deleted
> historical `None is a Blocker` claim is restored and annotated rather than silently rewritten; and
> both A0 tracking files now have Tracking Documents table rows. Claude's sixth observation — no
> independent reviewer on the fresh closure — is recorded transparently as a limitation, not promoted
> into a requirement FR-AG-018 does not contain.
>
> No `src/`, planning/spec, tool, workflow, save, RNG, tuning, or simulation behavior changed; no code
> gate is owed.
>
> **Last Updated (prior):** August 31, 2026 — **Governance hostile-review closure after the systematic remediation.**
> Governance advances v0.9 → v0.10; integration plan v0.8 → v0.9; A0 consistency audit v1.0 → v1.1;
> A0 adoption review v1.4 → v1.5. Planning/tracking documentation only; Governance remains `Draft`
> pending human sign-off.
>
> Three semantic defects found in the v0.9 follow-up are fixed together. The finding state machine now
> rejects every Disposition/Status pairing except `Open` or that Disposition's mapped terminal Status,
> and convergence requires every substantive finding to be terminal; Candidate admission during the
> same review forces applicability recomputation. FR-AG-026's exclusion route now correctly means a
> surface explicitly **within** recorded Non-scope (or a §7.1 exception), not outside Non-scope. The
> pre-adoption/review-gate Blocker route now requires a durable pre-existing gate authorized by the
> project lead/owner or an existing governing authority, with scope and closure condition; the current
> reviewer cannot self-authorize or invent one retroactively.
>
> AG-A0-023–025 record those defects as Blocker/Resolved under the A0 corrective route. The prior v0.9
> zero-finding review is retained as historical and explicitly superseded. A fresh full review over
> Governance v0.10 verifies all 52 A0-scope boxes and returns zero additional findings.
>
> No `src/`, `.cs`, `.asmdef`, workflow, tool, save format, RNG, tuning, or simulation behavior changed.
>
> **Last Updated (prior):** August 31, 2026 — **Systematic Governance consistency remediation and fresh A0 adoption review.**
> Governance advances v0.8 → v0.9; the integration plan v0.7 → v0.8; the A0 review record v1.3 → v1.4;
> and new `docs/tracking/a0-governance-consistency-audit.md` v1.0 records the exhaustive audit. Planning
> and tracking documentation only. `Status:` remains `Draft`; human sign-off is still required before the
> status edit and post-edit adoption digest.
>
> **One settled model, propagated everywhere:** exactly four finding Dispositions — `Blocker`, `Accepted
> Tradeoff`, `Residual Risk`, `Candidate Property` — and five Status values. `Resolved` is a Status, not
> a Disposition; `Dispositioned` is not a Status. The High severity of AG-A0-015 did not decide its
> Disposition. Its corrective route is `Blocker → Resolved` because the explicit, already-applicable
> Governance §9 / integration-plan §11 A0 gate had to be satisfied; severity and disposition remain
> independent.
>
> **The systematic pass was exhaustive, not selective:** it built a 47-row FR-AG-to-elaboration modality
> matrix; defined `runtime-bearing component` as the sole normative term; compared every live schema,
> enum, transition, template, checklist, summary, and downstream-plan representation; corrected every
> Round 5 finding plus audit-exposed state, field-order, direct-modality, and stale-plan defects in one
> batch; and added the exact comparison evidence to the new audit record. Appendix A/B field labels now
> reproduce their canonical schemas; Appendix C uses `Teardown owner`; Appendix D keeps static,
> alternate, and bypass paths separate; and the integration plan's durable ledger/A0 state model matches.
>
> **Fresh full review result:** all 52 A0-scope boxes verify against Governance v0.9, its 47 FR rows,
> and the mechanical comparison; no new finding was returned. All twenty-two historical corrective
> findings are schema-complete with `Disposition: Blocker`, `Status: Resolved`; no Blocker remains open.
> This means the review is converged **for A0**, not that Governance is approved. §9.7 remains owned by
> A3–A9. The repository documentation-consistency checker, cross-reference inventory, and schema/enum
> audit are rerun for this landing. The CI Markdown-lint scope excludes these long-form planning and
> tracking documents.
>
> No `src/`, `.cs`, `.asmdef`, workflow, tool, save format, RNG, tuning, or simulation behavior changed,
> so no runtime/build gate is owed.
>
> **Last Updated (prior):** August 31, 2026 — **A0 adoption review run, five rounds, NOT converged, PAUSED at owner
> instruction with round 5's findings unfixed; Governance v0.4 → v0.8, still Draft. Integration
> plan v0.6 → v0.7. Awaiting human sign-off. Planning and tracking documentation only.**
> No `src/`, `.cs`, `.asmdef`, workflow, tool, save format, RNG, tuning, or simulation behavior changed, so no gate
> run is owed.
>
> **The A0 gate was circular and is now scoped.** Integration-plan non-negotiable 1 required a "completed
> self-checklist" at A0, while the plan itself assigns Governance §9.7's downstream landings to A3–A9 — so A0
> could not close until stages depending on A0 had already run. Governance §9 in fact carries two bars: its
> preamble gates becoming *authoritative* (§9.1–§9.6), and §9.7 is headed *"Before this specification is
> considered fully adopted"*. **A0 is now scoped to the first bar only**, with five explicit closure conditions —
> §9.1–§9.6 verified with cited line ranges, a fresh recorded review per FR-AG-018 carrying review-level evidence
> and Appendix B findings, no open Blocker, human sign-off, and a landing order that writes `Status: Approved`
> **before** computing the content digest, recorded outside the Governance file. A0 explicitly does **not** require
> the property registry, finding ledger, review tooling, or any #19/#20 amendment.
>
> **New `docs/tracking/a0-governance-adoption-review.md` v1.3** is the review record. **Five fresh rounds
> complete over v0.4 → v0.8; none clean, and round 5's findings are recorded but deliberately NOT fixed**, and the record says so rather than
> assuming it clean. **46 of the 52 in-scope boxes verified against cited line ranges**; five of the six §9.6
> process-state boxes are discharged by the record itself, two of those **vacuously** — no architectural property
> has been admitted anywhere in the repository — which is recorded as a limitation rather than passed silently.
> The sixth, "fresh final review completed", stays unticked until a round returns only Low findings or none.
>
> **Outcome: NOT CONVERGED, PAUSED.** Twenty-two findings across five rounds. Fourteen dispositioned and
> Resolved; **round 5's eight are open and unfixed by instruction**, and **that withdraws a §9.6 discharge**:
> "Every finding dispositioned" no longer holds, because "Open" is a Status and not a disposition under
> FR-AG-009. A0 cannot close in this state — not because a High is open, since severity does not gate approval,
> but because two of the six boxes the review record exists to discharge are no longer discharged by it. That is not a
> formality here, and the round-by-round record is the argument:
>
> — **Round 2 found a High inside the very passage round 1 had just amended.** AG-A0-002: §5.5 stated the
> failure-injection obligation as SHOULD while FR-AG-029 states it as MUST on the identical "meaningful"
> condition, and since an unmet mandatory proof trigger is a Blocker, the weaker reading left FR-AG-029
> unenforceable. *(⚠️ CORRECTED — this entry as first published attributed that Blocker rule to **FR-AG-011**,
> which is wrong: FR-AG-011 requires only that a Blocker **cite** an authority. The rule is **§4.3 item 5**. The
> misattribution came from the round-2 reader and was propagated into three documents before round 3 caught it as
> AG-A0-006. Annotated here rather than rewritten, per this file's own rule.)* §5.5 now carries the MUST and
> marks its nine failure types illustrative. AG-A0-003 (Medium): the §4.2 Status enum had no valid value for a
> Residual-Risk or Candidate-Property finding; extended. AG-A0-004 (Low): §3.3 mandated the `AP-###` format that
> FR-AG-004 calls merely recommended; hedged. AG-A0-001 (Low): §5.5 addressed test authoring rather than proof
> scope, wording §1.3 reserves to Spec #19.
>
> — **Round 3 then found that round 2's own fix was incompletely propagated, and that two claims round 2 wrote
> into the Governance version history about itself were false.** Eight findings, three Medium and five Low.
> AG-A0-005: §4.1's lifecycle line still listed three terminal states after §4.2's enum went to five — AG-A0-003's
> defect one section over, and the reason "extended to match §4.1" was untrue. AG-A0-006: the FR-AG-011
> misattribution above. AG-A0-007: FR-AG-026's "unless an approved exclusion exists" named no mechanism at all,
> leaving a MUST-level rule with an undefined escape hatch; now closed to exactly two recorded artifacts — a
> property's §3.3 Non-scope, or a §7.1 exception — with prose assertion explicitly excluded. Five Low: §5.4 had no
> modal verb where its siblings carry a MUST; §7.1 still mandated the bare `AP-###`; "Tradeoff" vs "Accepted
> Tradeoff" drift; Appendix A's paraphrased field labels; two property transitions missing from Appendix F.
>
> — **Round 4 found a fourth propagation miss, and this one had been seen and deliberately left.** Two Medium.
> AG-A0-013: §9.3's checklist still read "Tradeoff defined." The site was noticed at the 0.7 landing, classified as
> a checklist label rather than an enum site, and skipped — a judgment call that was wrong, because §9 is the
> approval gate and it named a disposition existing nowhere else in the document. AG-A0-014: §6.6 stated
> verification "SHOULD be proportionate to the consequence of tool failure" where FR-AG-036A makes it a MUST;
> since §4.3 item 1 makes a violated MUST-level property a Blocker, the SHOULD left room to treat disproportionate
> tool verification as a mere shortfall. Round 4 also independently verified all eight 0.7 changes and all twelve
> claims the 0.7 version-history row makes about the document — those held.
>
> — **Round 5 (v0.8): one High, one Medium, six Low — recorded, not fixed, per owner instruction.** The High,
> AG-A0-015, is the Disposition enum disagreeing with itself on its own size: FR-AG-009, §4.2's Disposition row
> and Appendix B's checklist list five values ending in "Resolved", while §4.1, §4.3–§4.6 and Appendix F all
> treat it as four — there is no §4.7 and no fifth Appendix F chain. "Resolved" is legitimately a *Status* value
> that leaked into the Disposition enum at three sites. Verified independently at both ends. AG-A0-016 (Medium):
> "runtime-bearing component" vs "runtime component" across four MUST-level sites with no glossary. Six Low
> covering §5.7 and §6.6 modality gaps and three appendix field-label drifts. **AG-A0-015 should not be fixed by
> reflex** — deleting the fifth value is probably right, but "Resolved is a real fifth disposition and §4.7 is
> missing" is also coherent, and choosing is a governance decision, not a typo correction.
>
> **The finding that matters most is about the process, not the document: round 5 was dispatched with explicit
> instructions to enumerate every enum site and walk every FR-AG rule against its elaborating section — and still
> found a fifth instance of each recurring class**, one of them in §6.6, the very section hardened in v0.8 for a
> sibling requirement. Five rounds of targeted fixing have not exhausted either class, which is evidence the
> document needs a systematic consistency pass rather than another round of point fixes.
>
> **Two defect classes account for nearly all of it, and each has now recurred five times.** Incomplete
> propagation: a term or enum corrected in some sites but not all (§4.1, §7.1, §9.3, the Status enum). Modality
> mismatch: an FR-AG rule stating MUST while its elaborating section says SHOULD or carries no modal verb (§5.4,
> §5.5, §6.6). Round 5 was dispatched with instructions to sweep both classes exhaustively rather than
> opportunistically. **No prediction is made about its outcome** — the severity trend is downward, but a
> diminishing count is not convergence, and this review has twice been wrong about what the next round would find.
>
> **A first reading that reported §9.1 boxes 1–3 as FAIL was rejected on verification.** Its reasoning — that the
> Authority Matrix names #19/#20 as owners of rules those specs do not yet contain — conflates *unlanded* with
> *duplicated*, and misses that §1.4's column is *Authoritative Owner* while §8.5's is *Enforcement Owner*. Those
> are different concepts, and a rule normatively owned in Governance with enforcement owned by #20 is the design.
>
> **Deliberately not done:** `Status:` remains `Draft` and no adoption digest is pinned. Both wait on human
> sign-off, which is not delegable. §9.7 remains open and owned by A3–A9.
>
> **Last Updated (prior):** August 30, 2026 — **A1c COMPLETE: merge enforcement is active and MEASURED. Integration plan
> v0.5 → v0.6. Tracking and planning documentation only.**
> No `src/`, `.cs`, `.asmdef`, workflow, tool, save format, RNG, tuning, or simulation behavior changed, so no gate
> run is owed.
>
> The owner set the `CI for Main branch` ruleset to **Active**. Enforcement was then measured by a paired two-arm
> comparison on PR #343 varying exactly one required check: **`d689f2b`** all six required checks green →
> `mergeable_state: unstable`, mergeable; **`d497a4d`** identical but for one stale `Decision Tree #7` line under
> `docs/specs/` — verified beforehand against the job's own grep as the single hit in the tree — turning
> `Spec hygiene checks` to `failure` while the other five stayed green → **`blocked`**. Control reverted
> immediately. **A red required check stops the merge; A1 has objective enforcement.**
>
> **New `docs/tracking/a1c-enforcement-evidence.md`** captures this durably, because `mergeable_state` is a
> point-in-time value GitHub does not retain: run and job ids for both arms, every conclusion, and the full
> *Require status checks to pass* list read in settings — `Markdown lint`, `YAML lint`, `Markdown link check`,
> `Spec hygiene checks`, `File manifest sanity`, `C# format check`.
>
> **The two arm commits are preserved as remote branches `evidence/a1c-green-arm` and `evidence/a1c-red-arm`.**
> The branch was squashed for landing, which orphaned them — they are NOT ancestors of this commit, and an earlier
> draft wrongly said they were. `git diff evidence/a1c-green-arm evidence/a1c-red-arm` is the whole experiment:
> one file, thirteen lines. Do not delete those branches.
>
> **One absence in that list is load-bearing**, and is why the criterion demands the whole list rather than a
> spot-check: the shim gate is not required, so the owner-held `sim_match_engine_close_chance` red gates nothing.
> `Unity tests` is also absent and resolves to `skipped` every run; that is recorded as context, not as a freeze
> risk — an earlier draft claimed a required-but-skipped context would freeze merges, which is false (GitHub
> treats `skipped` as satisfying a required check) and is withdrawn.
>
> **Method, now normative in v0.6 §11 A1c: a single `blocked` reading is not evidence about any check.** `blocked`
> is returned for an unmet approving review, an unresolved conversation, a pending required check, or a failing one.
> A first attempt at this measurement read `blocked` with the check red and called it proof while a then-standing
> approval requirement was producing that same value in *both* arms; that claim was withdrawn. Criterion 3 now
> requires paired arms and forbids the single-arm reading.
>
> **Required approving reviews were set 1 → 0 by the owner during this work, and that is recorded as a decision,
> not left implicit** — A1c would otherwise read as strengthening one gate while quietly removing another. The
> analysis is in the evidence record §4, and it inverts the obvious reading: **GitHub forbids a pull request's
> author from approving their own pull request**, so with one maintainer the 1-approval rule was not ceremony but
> **unsatisfiable** — every PR mergeable only by admin override, which trains the bypass habit. It had never
> bitten because the ruleset was disabled until August 29; PR #343 is the first it ever held. Setting it to 0 was
> necessary for normal, non-bypass merging, not the removal of a working review gate. The real cost is that **no
> human-review gate now exists to strengthen later**, on a repository where much of what lands is agent-authored.
> Revisit condition, concrete: restore to 1 if a second write-access reviewer ever exists.
>
> Classic branch protection on `main` remains unread (403 through the current integration); the ruleset layer only
> is claimed.

> **Last Updated (prior):** August 29, 2026 — **Architecture governance A1 corrected to the live repository; tooling and planning only.**
> The rejected standalone `tools/architecture-governance/asmdef_discovery.py` approach was abandoned before merge. The existing
> `tools/assembly-tier-check.py` remains the single parser/checker for Code Standards #20 §3.5.2 and gains deterministic
> `--json` complete-graph evidence, separate graph/classification/subject SHA-256 digests, and all-assembly cycle reporting
> without changing its existing production-policy verdict. New `tools/tests/test_assembly_tier_check.py` locks classification
> digest movement, heading-title independence, FR-CS-046b Infrastructure binding, test-only cycles, test-only external refs,
> JSON CLI output, and stray root-level asmdef visibility; `.github/workflows/ci.yml` now runs that suite in `Spec hygiene checks`.
>
> `project-architecture-governance-integration-plan.md` advances to v0.5: ERR-020-002/003 are recorded as already resolved,
> obsolete A1b is removed, A1a is consolidation into the existing checker, and A1c is re-scoped to activating the existing
> required `Spec hygiene checks` context after re-reading live protection state. The repository ruleset observed during review
> already names that context but is disabled; no protection setting is changed in this slice. No `src/`, `.cs`, `.asmdef`,
> save format, RNG, tuning, or simulation behavior changed.

> **Last Updated (prior):** August 29, 2026 — **CI TRIAGE on PR #341: three of the four red checks fixed;
> the fourth is the owner-held band and was not touched.** Tooling and documentation only; no `.cs`
> and no `.asmdef` changed, so no gate run is owed. The three fixed checks were all introduced by this
> branch — `doc-consistency-check.py` and its CI step do not exist on `main` at `ec555e6`, `main`'s
> `README.md` has no MD018 hit, and `spec-error-log.md` is 824,073 bytes there against 1,057,156 here.
>
> **Markdown lint (3 × MD018).** `README.md` paragraph wraps had put `#29`, `#34`, `#41` and `#44` at
> the start of a line, where markdownlint reads a spec reference as an ATX heading. Rewrapped so the
> reference ends the previous line; MD013 is off in `.markdownlint-cli2.yaml`, so nothing else moved.
> Verified with `markdownlint-cli2@0.13.0` under the CI globs: 3 errors → 0.
>
> **Unity asset hygiene — the binary guard was asking a Markdown file to go to Git LFS.**
> `check-binaries.sh` failed on `spec-error-log.md` at 1,057,156 bytes, 8,580 over its 1 MiB line. Its
> own header says it exists for "binary game assets (textures, models, audio, prebuilt libraries)", and
> the five largest tracked files are now all append-only text — `spec-error-log.md` 1,057,156,
> `CHANGELOG-src.md` 809,677, `MatchEngine.cs` 650,383, `file-manifest.md` 618,508, `CHANGELOG.md`
> 600,254 — so three more cross the same line shortly. LFS is the wrong home for every one of them: it
> would break the grep-based tools in `tools/` (`doc-consistency-check.py` reads these files directly)
> and destroy the line-level diff that makes an append-only chain auditable at all. The guard now
> classifies by git's own heuristic (a NUL byte in the first 8000 bytes) and holds text to its own
> ceiling — `TD_TEXT_THRESHOLD_BYTES`, default 4 MiB — never to the LFS requirement. Binaries are
> unchanged at 1 MiB. **Not a threshold raise:** the binary path was not weakened, and a runaway text
> file still fails. Both paths negative-tested — a planted 2 MiB non-LFS binary is still caught, a
> planted 5.4 MB text file trips the new ceiling, and the real tree passes. One latent defect fixed on
> the way in: the first form of the exit test was `[ a ] || [ b ] && exit 1`, which under this script's
> `set -euo pipefail` exits 1 on the *success* path.
>
> **Spec hygiene — 24 findings → 0, and 8 of them were a bad merge, not stale prose.** The merge at
> `d83e893`/`a605f77` resolved three `open-issues.md` entries and five `file-manifest.md` supplement
> rows by keeping **both sides as separate records** instead of unioning them. In every one of the
> eight, the shorter copy carried the currency annotation and the longer copy carried the newer body,
> so the file simultaneously published two versions of the same issue — and the duplicate entries were
> inflating this file's own active count, the exact defect a de-duplication pass had to correct here
> once already. Resolved as unions: the longer body kept, the annotation re-inserted at its original
> offset and advanced to the target's actual newest version, the duplicate dropped. Active open issues
> 22 → **19** by recount; `file-manifest.md` supplement rows lost five stale twins (`gk-conversion-at-
> contact` v1.0 under a live v1.3, `gk-rush-trigger` v1.1 under v1.5, `close-chance-creation` v1.0
> under v2.2, `match-engine-wiring-backlog` v1.0 under v1.13, `gk-contact-rate` v1.0 under v1.4).
>
> The remaining ten citations were genuinely stale and were advanced in the file's own documented
> `vOLD, now vNEW` form rather than by rewriting the dated half: `close-chance-creation-design.md`
> → v2.3 (four sites), `match-engine-wiring-backlog.md` → v1.13 (two),
> `interactive-unity-client-design.md` → v0.21 (two), `injury-aging-research-alignment-design.md`
> → v0.7 (two). Two were checker false positives rather than stale claims, and were fixed at the
> prose rather than by loosening the tool: in `spec-error-log.md` a "Files Affected" row read
> "`AbilityModel.cs` **now** hosts the curve (v0.4)", where "now" within ~30 characters of a version
> is read as a currency assertion by the checker's closed marker set — the "now" is dropped, matching
> every sibling row in that same table; and in `open-issues.md` a bare "backlog v1.9" let the version
> bind to `close-chance-creation-design.md` two clauses earlier, so the backlog is now named in
> backticks, which is what makes the binding refuse.
>
> **Cardinalities.** Design supplements 60 → **61** on two surfaces (`README.md`,
> `adversarial-review/SKILL.md`); `ls docs/tracking/*-design.md` measures 61. The assembly-less
> agreement set had seven surfaces at 19 and `orienteer.md` alone at "roughly 20" — set to 19, the
> value `README.md` carries a derivation for. The roadmap's "**eight of the 32 open findings**" was
> **not** an assembly-less figure at all: it counts football-judgment-proxy-review findings and was
> pooled into that agreement set only because "have no `src/` assembly" sits in the preceding clause.
> Reworded so the two figures are not adjacent, and its totals brought up to the record in
> `open-issues.md` — 32 open → **29**, 24 workable → **21**, since batch 1 landed August 22. The
> deferred count of eight is unchanged and still reconciles (29 − 21 = 8).
>
> **Also repaired, same merge, same class:** `CHANGELOG.md` and `CHANGELOG-src.md` each carried **two**
> bare `**Last Updated:**` labels, which makes each file self-contradictory about its own currency —
> the defect this chain's own rule exists to prevent and that has been fixed here at least three times.
> Both relabelled to `(prior)` under this entry; `CHANGELOG-src.md` keeps its v2.125 head bare and
> marks main's v2.124 `(prior)`. No historical entry was edited.
>
> **RECORDED, NOT FIXED.** `CHANGELOG.md` now holds two *concatenated* chains rather than one
> date-ordered one: this branch's August 19-and-earlier entries sit above `main`'s August 28 head, so
> the file is newest-first *within* each half and not across the seam. Every entry is present and
> dated and no entry was rewritten, so the record is intact; interleaving ~920 lines of someone else's
> chain is a restructuring, not a CI fix, and it is left for an owner call. The same seam is why
> `CHANGELOG-src.md`'s v2.125/v2.124 heads disagree between date order and version order.
>
> **Determinism:** no `SNAPSHOT_SCHEMA_VERSION` change, no new RNG stream, domain tag, draw site, or
> draw-order change. No production or test source was touched, so nothing was perturbed downstream —
> no scenario tick window, no per-90 band, no A4a round-resolution fit, no `FR-PO-052` perf baseline.
> Checked, nothing moved.
>
> **Gate:** not re-run for this change, and none is owed — no `.cs`, no `.asmdef`, and nothing under
> `tools/dotnet-ci/` moved, so the tree the gate compiles is byte-identical to the one CI already
> measured. Quoting that run rather than a fresh one: **CI run 33217732663, head `a605f77`, all 34
> suites reported against 34 `src/*/[Tt]ests/*.asmdef` — a complete sweep — 3,040 passed, 1 failed,
> 207 skipped.** The single failure is `sim_match_engine_close_chance` in `MatchEngine.Tests`
> (**472 passed / 1 failed / 11 skipped**), the band held RED by owner decision at
> `close-chance-creation-design.md` §10.9 item 6, failing at **exactly** its recorded values —
> `meanCosine` −0.165 against the −0.16 bound and `goalwardShare` 0.407 against 0.42, 2 of 3
> predicates. No new failure, no band rebaselined, and it was not touched. Per this repo's own trap
> list the gate script is `set -euo pipefail` and exits before its own verdict, so **no
> `── Gate PASSED ──` line and no quarantine report was printed** — the quarantine state below comes
> from inspecting `tools/dotnet-ci/known-failures.txt` directly, which holds no entries.

> **Last Updated (prior):** August 19, 2026, evening — **Round 14: an EXTERNAL reviewer found two exploitable
> holes in round 9's own security fix, and both were confirmed by execution before being fixed.** This is
> the first round of this series with fresh eyes on it, and it immediately found what four single-handed
> rounds did not. Tooling and documentation only; no `.cs` and no `.asmdef` changed, so no gate run is
> owed.
>
> **The finding that matters is not either bug — it is that both have one root error.** Round 9's H1 fix
> validated a *shape* rather than the argv that actually executes. Everything downstream of that was
> sound reasoning about the wrong object.
>
> **P1(a) — `awk -v` walked straight past the program.** The program was located as "the first operand
> that does not start with `-`", but `-v` and `-F` take a SEPARATE argument, so
> `awk -v x=1 'BEGIN{system("touch /tmp/pwn")}END{print 1}' CLAUDE.md` handed the escape check `x=1`,
> never examined the program, ran `system()`, and returned the claimed integer under a printed **PASS**.
> Reproduced here before fixing: the artefact was created. Fixed by scanning EVERY token rather than the
> one guessed to be the program — that needs no awk option grammar and cannot be outflanked by adding
> one, since `system`/`getline` must appear literally to be called. The same heuristic was wrong for
> `python3` (`-X foo.py bar.py` executes `bar.py`), so the script must now be `argv[1]` with no
> interpreter flags at all.
>
> **P1(b) — the flag check ran before glob expansion.** With a repo file named `--output=canary`, the
> validated command `sort * \| wc -l` expanded to `sort --output=canary …` and **wrote that file**.
> Reproduced here before fixing. Both halves closed: an expanded filename that would be read as an
> option is refused by name, and the entire escape-hatch check re-runs on the post-expansion argv.
>
> **P2 — the derived denominator was still baked into the pattern.** Round 9 derived the registry size
> instead of hard-coding 53, but the pattern still searched only for the CURRENT size, so on the day the
> registry grows the group matches nothing, prints a NOTE, and CI passes — leaving the "19 of the 53"
> prose stale and unreported, which is the exact transition that fix existed to catch. Measured: at
> `spec_folders=54` against today's prose, the old form raised **0** findings. The denominator is now
> matched and checked against the measured count, naming each stale site directly; at 54 that is **9**
> findings, at today's 53 it is clean. A stale-denominator site is no longer pooled into the agreement
> numerator either, since a figure whose denominator moved cannot be compared with one whose did not.
>
> **What this costs the record.** The standing caveat in the three entries below — that delegation was
> unavailable and the fresh-eyes property was never obtained — was not a formality. Two exploitable
> holes sat in a security fix through rounds 10, 11, 12 and 13, all of which re-read that file, and none
> of which found them, because the reviewer and the author were the same. **A delegated pass over
> `tools/doc-claim-check.py` is no longer "worth the tokens"; it is the demonstrated gap**, and it
> remains owed even after this round.
>
> **Verification:** `doc-claim-check` PASS (3 executed, unchanged; the two exploit commands are declined
> and NAMED, and neither artefact is created); `doc-consistency-check` PASS (34 excusals 23/4/7/0,
> 16 unresolvable); `recurring-defect-lint` 0 ERROR / 122 WARN / 27 INFO;
> `assembly-tier-check` PASS. Figures measured AFTER this entry was written, per the correction
> annotated on the round-13 entry below.

> **Last Updated (prior):** August 19, 2026, later still — **Round 13: `doc-claim-check.py` gains the
> dated-record model, ported from the citation checker rather than reinvented.** Tooling and
> documentation only; no `.cs` and no `.asmdef` changed over `12eba7d~1..HEAD`, so no gate run is owed.
>
> **The hazard, and why a mechanism rather than a resolution.** A claim written into an append-only
> record states what a command returned AT THE TIME. When the underlying figure later moves, the record
> is still correct — and this gate would fail CI on it. That is not hypothetical and it was not someone
> else's mistake: **at round 12 the pass writing the CHANGELOG entry quoted Spec #20's own drift-prone
> example verbatim, with its value, into the chain**, which fails the day the 36th assembly lands. It
> was caught before landing and the example is now described rather than quoted — but "remember not to
> write that" is not a mechanism, and this series exists because relying on remembering is what failed.
>
> **Ported, not reinvented.** The regions come from `doc-consistency-check.py` — its
> `record_regions` plus a new `frozen_chain_span()` extracted out of `blank_frozen_history` for the
> purpose — so the two checkers cannot disagree about which bytes are frozen history. A second copy of
> that definition would show up as one tool excusing a record the other reports, which is the
> duplicate-claim defect this repo files repeatedly. Both of the model's load-bearing properties are
> kept: the claim is still **executed** and the mismatch **excused rather than skipped**, counted and
> named so "this historical figure no longer reproduces" stays visible without gating; and an explicit
> **"now" / "currently" / "today" pierces** the excusal, because a record asserting a value is current
> is a present-tense claim wherever it sits.
>
> **0 excusals on today's tree — this is prophylactic, and is stated as such rather than dressed up as
> a fix.** Proved four ways on a scratch mirror, in both region kinds (frozen header chain and log
> body): the head entry above the marker is REPORTED, a plain record below it is EXCUSED and named, a
> reasserted record below it is REPORTED. `doc-consistency-check` output verified byte-identical either
> side of the extraction.
>
> **Verification:** `doc-claim-check` PASS (3 executed, 30 declined, 0 excused); `doc-consistency-check`
> PASS (34 excusals 23/4/7/0, 15 unresolvable *(⚠️ corrected August 19, 2026, round 14: **16**. The figure was measured before this entry was written, and WRITING it moved the number — a new head entry pushes the previous one below the `(prior)` marker, which changes what the citation scan reads. So this checker's own coverage figures cannot be measured before the entry that quotes them. Third recorded instance of "a count taken mid-pass does not survive the pass", and the first where the act of recording is itself what invalidated it.)*); `recurring-defect-lint` 0 ERROR / 122 WARN / 27 INFO;
> `assembly-tier-check` PASS. **Still owed, unchanged:** a DELEGATED review pass over
> `tools/doc-claim-check.py` — every round from 9 on ran single-handed, so the fresh-eyes property was
> never obtained on code whose author was also its reviewer.

> **Last Updated (prior):** August 19, 2026, later — **Adversarial-review rounds 10–12, and a premature
> closure withdrawn.** Documentation and tooling only; no `.cs` and no `.asmdef` changed (verified over
> `12eba7d~1..HEAD`, where the only `src/` path touched is `src/CLAUDE.md`, the coding guide), so no
> gate run is owed.
>
> **The withdrawal first, because it is the finding about the process rather than the code.** The
> round-11 commit declared the cycle converged on an L-only round. That claim does not hold: this
> skill's termination rule is a **full fresh review over the entire scope** returning only Lows, and
> round 11 read ONE FILE. An L-only sweep of the file you just edited is not a clean pass over the
> estate — it is the narrowest possible reading of the bar, made by the author of the code under
> review. Withdrawn, and the loop continued. **This is the round-8 diagnosis one level up:** rounds
> 5–8 kept finding fixes that enumerated instances where the defect was a class; round 11 declared
> convergence on a scope that was a fraction of the artifact.
>
> **Round 10 — 1 Medium, 1 Low, both in round 9's own output.** The Medium was in the RECORD: round 9's
> entry claimed "no `.cs` and no `.asmdef` changed anywhere on the branch", which is false — the branch
> carries the merged #44 discipline work from PR #322, some 60 `.cs` files. The phrasing was inherited
> verbatim from the round-7 and rounds-4–7 entries, so **three consecutive entries asserted a scope none
> of them measured**. Corrected to the range that is true, and stated AS a range. The Low was in the
> tool: the negation test ran before the is-this-a-command test, so a backticked IDENTIFIER standing
> near a negation was counted and printed as a declined CLAIM — five of eight "negated" declines were
> of that kind, overstating the coverage being given up in the one figure whose purpose is to state it
> honestly. Declines 30 → 25, every remaining line naming a real command.
>
> **Round 11 — 2 Low.** `SEP = object()` was dead the moment round 9 wrote it: a pipeline-separator
> sentinel standing beside a tokenizer that appends segments directly, with a comment describing a
> mechanism that does not exist. Plus one blank line.
>
> **Round 12 — the coverage gap round 9 named and did not close.** Round 9 filed the unrecognised
> VALUE-FIRST claim shape — a count stated before the command that checks it — as a Low and stated it
> in the header. Stating
> a gap is not closing one, and the live instances are the drift-prone kind: **Spec #20 §5.4.5 states
> the `src/` assembly count in that shape, in APPROVED text, and it goes stale the day the 36th
> assembly lands.** The pre-fix sweep found seven live instances, of which **six** quote a real command (the
> seventh, `permille/1000f > 0.6f`, is an expression that merely looks path-shaped); every one was
> evaluated by hand first and all are currently TRUE, so this adds coverage rather than findings —
> executed claims 2 → **3**, and the #20 figure is now under the checker. *(Two things the first draft
> of this entry got wrong, recorded because both are the trap the rounds-4–7 entry below already named.
> **(1)** It said "seven claims, all real": that count was taken before the discriminator was tightened,
> and it then went stale the moment this pass kept writing — a count taken mid-pass does not survive the
> pass. **(2)** The draft restated Spec #20's example HERE, verbatim with its value, which put a live
> drift-prone claim inside an append-only chronicle: `doc-claim-check` has no dated-record model, so the
> day the 36th assembly lands it would have failed CI on a correct historical record. The example is now
> DESCRIBED rather than quoted, here and in the manifest row. One authoritative site per claim — the
> spec's — is the whole point of checking it.)* **The complement test caught a defect in the addition
> before it landed**: in this shape the negator PRECEDES the number ("no longer 3 files (`cmd`)"), so
> reusing the forward shape's forward-looking gap reported a correctly-negated claim as a mismatch. The
> window now looks back, bounded to the line. That is the third time in this series that constructing
> the complement, rather than testing the motivating instance, is what found the defect.
>
> **Also checked and clean, so that the next round does not re-derive it:** the delegated version-history
> parser in `recurring-defect-lint.py` — which `doc-consistency-check.py` depends on for every citation
> it resolves — was swept for disagreement between a file's `**Version:**` header and its parsed table
> across every markdown file in `docs/`, root and `.claude/`. One disagreement exists
> (`ui-framework-t0-implementation-plan.md`, header 0.3 vs table 0.2) and it is already documented and
> handled in `open-issues.md` as an unversioned edit, with the citing text carrying a "now v0.2" pointer
> the checker accepts. No parser defect.
>
> **Standing caveat, restated because it bounds every round above:** delegation was unavailable in this
> session, so all four passes ran single-handed rather than through fresh reviewer subagents. The
> fresh-eyes property this loop normally buys was NOT obtained, and it matters most exactly where
> rounds 10–12 operated — on code whose author was the reviewer. A delegated pass over
> `tools/doc-claim-check.py` remains worth the tokens.
>
> **Verification:** `doc-claim-check` PASS (3 executed, 30 declined, each named); `doc-consistency-check`
> PASS (34 excusals 23/4/7/0, 15 unresolvable); `recurring-defect-lint` 0 ERROR / 122 WARN / 27 INFO;
> `assembly-tier-check` PASS. Value-first coverage proved in all three directions on a scratch mirror:
> a correct claim is silent, a wrong one is reported, a negated one is declined and named.

> **Last Updated (prior):** August 19, 2026 — **Adversarial-review round 9: the tool round 8 built to end the
> recurring defect class shipped WITH that class in it — three High findings in
> `tools/doc-claim-check.py`, every one proven by reproduction before the fix and re-proven in both
> directions after.** Documentation and tooling only: no `.cs` and no `.asmdef` has changed anywhere
> in this adversarial-review series — verified over `12eba7d~1..HEAD`, where the only `src/` path
> touched is `src/CLAUDE.md`, the coding guide — so no gate run is owed. *(That range is stated
> because the looser phrasing this entry first used, "anywhere on the branch", is FALSE and was
> inherited from the round-7 and rounds-4–7 entries below: the branch also carries the merged #44
> discipline work from PR #322, which is 60-odd `.cs` files. Caught in the round-10 pass over this
> entry, by re-deriving the claim instead of re-reading it — which is the whole method this series
> exists to install, applied to the entry announcing it.)*
>
> **Why this round existed, and what it confirms.** Round 8's own diagnosis was that a correction pass
> is itself a high-defect-rate activity — nine of its ten Highs had been introduced or missed by the
> previous fix pass — and its answer was to stop reviewing harder and build a checker. Round 9 is the
> fresh pass over that answer. The checker is real and it works; it also arrived with a silent decline
> path and a header asserting a safety property it did not have. **The lesson is not "the tool was
> bad" — it is that building the detector does not exempt the detector from the class.** The three
> Highs below were all invisible to reading and obvious to running.
>
> **H1 — "read-only by construction (no writing command is on the list)" was false, and the tool
> executes untrusted document text in CI.** The allow-list gates `argv[0]` only, and several genuinely
> read-only binaries carry a write or execute escape hatch behind a flag. Demonstrated: a document
> containing ``sed -i s/canary/PWNED/ victim.txt`` → 0 REWROTE the file while the tool printed **PASS**.
> `python3 -c`, `find -delete`/`-exec`, `sort -o`, `rg --pre`, `git -c`, `uniq IN OUT` and
> `awk 'BEGIN{system()}'` were reachable the same way, and `ci.yml` runs this step on `pull_request`,
> so the input is a PR's own markdown. Fixed by naming the hatches per binary rather than trusting the
> list: `DENIED_FLAGS`/`DENIED_FLAG_PREFIXES`, git globals scoped to before the subcommand, `sed`
> dropped outright (its write lives in its script, where no flag list can reach it), `awk` KEPT with
> `system`/`getline` refused — because both of the repo's only two executable claims use `awk` and
> dropping it would have made every run a vacuous pass. **The git-global scoping is the part worth
> keeping:** refusing `-c` anywhere broke `git grep -c`, i.e. both live claims, taking the tool to
> zero verified claims. Nothing in the reasoning showed that; re-measuring against the corpus did.
>
> **H2 — quoted regex patterns were glob-expanded into filenames, silently changing the command.**
> `shlex.split` discards quoting, so `find . -name '*.md' \| wc -l` ran as
> `find . -name CLAUDE.md doc.md`. All 7 glob-character tokens in the live corpus are quoted regex,
> not shell globs. Replaced with a quote-preserving tokenizer; only tokens whose glob characters are
> all unquoted expand. It also splits pipelines on unquoted `|` alone, so `grep -c 'a|b' f` stops
> being a parse failure.
>
> **H3 — pipeline exit status was never checked, so the tool fabricated a mismatch against a correct
> document.** A failing segment's empty output flowed downstream: `grep -rn 'X' nosuchdir/ | wc -l`
> printed `0`, reported as "document says 218; command returns 0". Combined with H2 that is a complete
> false-failure path on correct prose — the defect this tool exists to catch, wearing the other sign,
> in a step wired to fail CI. A non-zero exit now DECLINES the claim, with grep/rg/diff/git `1` (no
> match / files differ) as the named benign case.
>
> **M1 — the third, silent decline path.** An unlisted binary was dropped by a bare `continue`,
> counted nowhere and named nowhere, while the header, the file-manifest row and the round-8 entry all
> published "every declined claim is counted AND NAMED". 9 live instances, all real
> (`tools/recurring-defect-lint.py` at seven sites, `curl`, `ps … | grep`). Counted and named now,
> behind a command-SHAPE discriminator, because `CLAIM` also matches ~1,100 backticked IDENTIFIERS
> (`SNAPSHOT_SCHEMA_VERSION` **20 → 21**) that are not commands and would drown the signal.
> **M2** — an absolute glob (`ls /etc/*.conf`) crashed the tool with an uncaught `NotImplementedError`;
> document text must not be able to crash the checker. **M3/M4, in `doc-consistency-check.py`** — the
> historical-marker suppression was the one excusal path incrementing no counter, while every run
> printed "excusals are counted, never silent" (0 live, so latent — and it is the most heuristic of the
> four mechanisms, so the one whose silence costs most); and the oracle-less agreement group hard-coded
> the registry size 53, so on the day the registry reaches 54 it silently checks nothing, with no
> zero-self-check to catch that. Both mutation-proved in both directions. **L1** — a backticked run of
> whitespace crashed on `"".split()[0]`. **L2** — the header never stated that only the
> command-then-value claim SHAPE is recognised, so root `CLAUDE.md`'s "8 scripts (`ls tools/*.py`)" is
> invisible AND uncounted.
>
> **Two findings outside the tools.** `docs/specs/code-standards/appendices.md` carried **two rows
> numbered v1.4** — round 8's own fix pass took a number round 7's had already used — which
> `recurring-defect-lint.py` reported as the tree's ONLY ERROR while root `CLAUDE.md` still recorded
> "0 ERROR tree-wide". Renumbered ascending (round-7 L3 keeps 1.4, round-8 H1 becomes 1.5, header
> follows) → `docs/specs/code-standards/appendices.md` **v1.5**, and the lint is back to 0 ERROR /
> 122 WARN / 27 INFO. And the round-8 entry below published "7 negated-or-historical" where its own
> tool, run at its own commit, prints 8 — annotated in place rather than rewritten.
>
> **Deliberately NOT done: no `ERR-` id was filed.** The repo obligation covers a finding that
> contradicts APPROVED spec TEXT; these are tool defects, and the duplicate version row is a hygiene
> slip the lint already reports mechanically — the August 8, 2026 pass over 275 such ERRORs fixed them
> without filing ids, and that precedent is the right one. **Verification:** `doc-claim-check` PASS,
> 2 executed (unchanged), declines 21 → **25** (+9 previously-silent unlisted-binary, −5 identifier
> pseudo-claims dropped in the round-10 pass — a backticked IDENTIFIER beside a negation was being
> counted and printed as a declined CLAIM, overstating the coverage the tool was giving up in the very
> figure that exists to state it honestly — and the rest recategorised), every remaining line naming a
> real command;
> `doc-consistency-check` PASS, 34 excusals (23 region / 4 chronicle / 7 phrasing / 0 marker), 15
> unresolvable; `recurring-defect-lint` 0 ERROR; `assembly-tier-check` PASS. The security fixes were
> verified on a scratch mirror in both directions: ten hatch attempts all refused with the canary file
> intact and no file created, and the complement — the same quoted-glob command with its TRUE value
> stated — passes.

> **Last Updated (prior):** August 18, 2026, later — **Adversarial-review round 8, and the conclusion drawn from
> five rounds of it: `tools/doc-claim-check.py` CREATED, because the recurring defect could not be closed
> by reviewing harder.** Documentation only; no `src/` code changed, so no gate run is owed.
>
> **The diagnosis, which is the part worth reusing.** Rounds 5–8 kept surfacing the same three shapes, and
> round 8 found that **nine of its ten High findings were introduced or missed by the previous FIX pass**,
> most of them by mine. (1) **Fixes enumerate instances; defects are classes** — `ERR-020-001` renamed a
> constant in §4.2 but not the appendix; round 7 renamed §C.1 but not §C.2 one section below; "on every
> push" was corrected at Spec #20's four sites and left at three more. (2) **Verification prose is never
> itself verified** — root `CLAUDE.md` cites a grep that refutes its own claim, §9.2 Q-04 ratified "three
> IDs" against six, §9.1 C-02's recorded value broke *inside* the commit that claimed to re-run every
> checklist command. (3) **The detector inherits the author's blind spot** — a comment justifying the code
> beside it, a checker heuristic tuned on its motivating example. `ERR-030-048` is the pure case: a comment
> asserting a gate that did not exist, written by the pass fixing the defect whose own comment asserted a
> gate that did not exist.
>
> **Root cause: prose has no compiler.** In `src/` a missed rename is a build error and a dangling
> reference will not link. In specs and tracking documents nothing binds, so the same classes fail
> silently — which is why the defects cluster in the FIX passes rather than the original work.
>
> **So the answer was a tool, not another round.** `doc-claim-check.py` runs two oracle-free checks.
> (1) It **executes the verification commands the documents quote** and diffs the stated value — this repo
> writes claims in machine-checkable form constantly and nothing ever re-ran them. (2) It **resolves
> `Type.MEMBER` references inside csharp spec fences** against the same file's own declarations, the class
> that let `ERR-020-001` dangle in Appendix C for three months. On first run it found one live dangling
> reference (`BallPhysicsConstants.MAX_SUBSTEPS`, round 7's own rename) and one un-annotated stale proof
> (`ERR-020-006`'s live command offered as evidence for a historical 218, now 251; and 218 counts LINES,
> not occurrences — the real figure is 235). Both fixed here.
>
> **What it deliberately does not do, recorded because overclaiming is the failure mode this series is
> about:** it verifies only claims whose command prints a single integer, and resolves identifiers only
> within one file. **Every claim it declines is counted AND NAMED** — 2 unsafe, 5 not-self-contained,
> 6 not-a-single-integer, 7 negated-or-historical against 2 executed —
> *(⚠️ both halves corrected August 19, 2026, adversarial-review round 9. The FIGURE: run at
> this entry's own commit `f23f480`, the tool prints **8** negated-or-historical, not 7 — a
> count published in the entry announcing the tool built because this project kept publishing
> verification figures nobody re-ran, and one its own checker cannot catch, since the value
> comes from a multi-line report rather than a single-integer command. The CLAIM: "every
> declined claim is counted AND NAMED" was false as written — an unlisted BINARY was dropped
> by a third, silent `continue`, 9 live instances, now counted and named.)* — because rounds 5 and 6 both found
> this project's checkers hiding real defects behind silent skips. Commands are untrusted document text:
> allow-listed read-only binaries, no shell, globs expanded in-process.
>
> **Its own first run produced two false positives, both fixed and both instructive**: `grep -c '^- \*\*'`
> quoted without its file operand reads empty stdin and returns 0 (declined as not-runnable-as-written,
> not reported as a mismatch), and "the plain `grep …` **no longer returns 218**" was read as an assertion
> that it does (negation detection added). Tuning a matcher on the instances that motivated it and never
> on their complement is defect shape (3), committed by the tool written to catch it.
>
> Also landed: the three hygiene checkers now carry `if: always()` in `ci.yml`, so one failure no longer
> masks the others (round-8 tooling M3) — a reviewer had been learning one problem per push.

> **Last Updated (prior):** August 18, 2026 — **Adversarial-review rounds 4–7 over the documentation estate: two
> checker tools built and CI-wired, two APPROVED specs corrected, four ERR ids filed. No `src/` code changed,
> no `.cs` or `.asmdef` touched anywhere on the branch, so no gate run is owed.**
>
> **What the series was actually for, recorded because it is the reusable part:** this project's rules files
> and tracking documents had become large enough that their claims drifted faster than anyone re-derived them,
> and the drift was invisible because every check was a human reading prose. Rounds 4–5 built the two tools
> that make it mechanical — `tools/assembly-tier-check.py` (the §3.5.2 tier order against the real `.asmdef`
> graph) and `tools/doc-consistency-check.py` (cross-document version citations and cardinalities) — and wired
> both into `ci.yml`'s `Spec hygiene checks` job. **Both tools were found INVERTED by the review that
> followed them**, twice: round 5 found the citation checker registering this repo's phrase for stating the
> CURRENT version as a marker meaning "superseded", and round 6 found a guard meant for version-history rows
> exempting **every** markdown table row — 27% of all citations, including the manifest inventory, which is
> the exact surface round 3's headline defect lived on. A checker that reports green over the class it exists
> to catch is worse than no checker, and it took an adversarial pass to see it each time.
>
> **Round 6 — 19 High / 24 Medium / 19 Low across four lanes.** The load-bearing ones: **`ERR-030-047`**, #30
> §3.4's normative pseudocode specifying a two-argument `OnClubFixturePlayed` that `ERR-044-014` had changed
> two days earlier, together with the unfiltered-roster precondition it never propagated — an implementer
> following the spec makes **every suspension permanent, silently**. **`ERR-020-006`**, Spec #20's constant-tag
> list forbidding `[CROSS-PENDING]`, one of root `CLAUDE.md`'s six mandated tags, making all 218 uses in
> `docs/specs/` MUST-level violations of an APPROVED spec. **`ERR-020-007`**, the `[CROSS]` const-mirror
> carve-out, filed because #20 certified as compliant a declaration its own FR-CS-022 forbade. Plus README
> reporting four wired subsystems as unimplemented and running four format-versions stale on
> `SEASON_SAVE_FORMAT_VERSION`, and the A4a home/away correlation published at two mutually exclusive values
> in files that carried both at once.
>
> **Round 7 — 8 High / 17 Medium / 6 Low.** Notable because **three of its Highs were introduced or preserved
> by round 6's own fix pass**: §4.1 asserting an upward `.asmdef` reference is "a build error" (it compiles
> cleanly — that is why the drift lasted fourteen months and why the tier checker had to be written); four new
> "on every push" claims written the same day §3.5.2 corrected that exact phrase about that exact tool; and
> **`ERR-030-048`**, the loud twin of `ERR-030-047` — the serve pair left UNGATED in the very block -047
> rewrote, with a comment justifying its null-safety by citing a gate that did not exist. Implemented verbatim
> it throws on the first fixture of any career without discipline wired. **The fix pass wrote the false
> justification.** That is the finding worth keeping from this series: a correction round is itself a
> high-defect-rate activity, and the only thing that caught these was re-reviewing the fixes as hostilely as
> the original.
>
> **The checker's scope gap, closed at round 7.** With both inversions fixed the tool reported 37 findings,
> essentially all false: it could not distinguish a **currency claim** ("X is at v1.5") from a **dated record**
> ("filed against X v1.5"), and the tracking estate is mostly dated records. Closed with three mechanisms,
> structure first and phrasing last — record REGIONS (`open-issues-resolved.md` whole-file; `spec-error-log.md`
> from the Error Index to EOF), CHRONICLE targets (citations of the three append-only logs, where a version is
> a timestamp), and four narrow record phrasings — under a **currency-reassertion override that pierces all
> three**, so "now v1.6" is still reported inside a frozen region. That override is what stops the fix becoming
> the blindness it replaced. Every excusal is **counted and printed**, never silent: 37 → 2, each mechanism
> mutation-proven in both directions, and the two survivors were real. The tool then caught four stale currency
> chains created by this pass's own version bumps, which is the first time it has policed its own author.
>
> **Files:** Spec #20 (all ten files), #30 `section-3.md` v2.18, `spec-error-log.md` v2.47, root `CLAUDE.md`,
> `src/CLAUDE.md`, `README.md` v1.38, `file-manifest.md`, `open-issues.md`, `open-issues-resolved.md`,
> `path-to-playable-roadmap.md` v0.23, `league-bootstrap-design.md` v1.7, `.claude/skills/**`, and the two
> tools. **Deliberately NOT done:** the 37-residual doctrine question is answered in the tool, not by editing
> the records it excuses — a dated record that names a superseded version is correct as history, and
> "fixing" one to satisfy a checker falsifies the thing the record exists for.

> **Last Updated (prior):** August 17, 2026 — **An owner decision pass over the seven decisions this project had
> been carrying as advisory recommendations. Two shipped, five held, one found to rest on a false
> premise. No `src/` code changed and no `.asmdef` moved, so no gate run is owed.**
>
> **The unifying reasoning, recorded because it is the reusable part:** every candidate for doing-it-now
> except the two that shipped sits **upstream of a measured event** — the `GameplayConfigHolder.Bind`
> pass, W2's arming, the youth/generated-cover ladder — that would force the work to be redone. So the
> cheap, reversible items shipped and the expensive ones wait for their deciding measurement. Each hold
> below names that measurement rather than a date, which is the difference between a hold and a stall.
> ⚠️ CORRECTED August 18, 2026 (reviewed adversarial-review finding H16): the last sentence overclaims
> by one, and the `Bind` pass does not belong in the measured-event list above. **Four of the five
> holds name a deciding measurement**; the fifth — the DisciplineConfig restructure, HELD (4) — does
> not, because "the `GameplayConfigHolder.Bind` pass" is an unscheduled intention that appears on no
> roadmap and in no backlog (`open-issues.md`'s own entry, rewritten after review the same day, says
> so in terms and says it should not be counted among the measured-event holds). Its checkable release
> condition is instead **`DisciplineConfigCompletenessTests` going red** — the tripwire firing when a
> non-`int` `[GT]` is added to `DisciplineConstants`.
>
> **SHIPPED (1) — `ERR-020-002` + `ERR-020-003` ADOPTED: the assembly layer taxonomy covers the whole
> tree for the first time since it was written.** Spec #20 §3.5.2's three-gameplay-layer box placed 19
> of 35 assembly folders and carried an empty `UI (Stage 1+ — not specified yet)` row; FR-CS-046
> ("references flow one direction only") is decided relative to two layer memberships, so for ~46% of
> the tree — including **every reference into or out of the composition root** — it decided nothing.
> ⚠️ CORRECTED August 18, 2026 (reviewed finding H11; source: #20 `section-3.md` v1.2, which corrected
> the same figure in its own 1.1 row): the retired box placed **14** of 35, not 19 — 8 Physics +
> 4 Mechanics + 2 AI, the `UI` row empty — leaving **21** undecided, i.e. **60%** of the tree, not
> ~46%. The 19 was the FORMER `src/CLAUDE.md` accounting (14 layer rows + 3 infrastructure rows +
> 2 cross-cutting assemblies in prose), a way of counting #20 v1.2 explicitly rejected; v1.2
> re-derived the 14 by counting the retired box itself (`git show 0e78d381~1`).
> The August-2 ten-tier proposal is adopted, extended from the 31 folders it was drafted against to the
> **35** now in `src/`: `training-system`, `injuries-medical` and `discipline` into **Management**,
> `client-app` into **Client**, each placed from its `.asmdef` references rather than its name.
> **Re-verified against the live graph at adoption, not carried over from the draft: 35/35 folders
> placed, 0 upward references, 105 downward, 38 intra-tier, graph acyclic.** Adopting changed nothing
> that compiles — it constrains only what is written next, which is the whole value and also why the
> cost was zero. **Two rules were added that the proposal did not contain, both surfaced by the new
> placements:** (a) **a tier is a ceiling, not a licence** — `discipline` sits in Management but #44's
> own FRs forbid it `match-engine` and `season-save`, so without this sentence adoption would have read
> as *widening* a constraint #44 deliberately took on; (b) **test assemblies are not members of the
> order** — `event-system.Tests` references `decision-tree`, tier 0 to tier 4 and entirely legitimate,
> so an adopted order silent on this would have made every test assembly a violation. `ERR-020-003`
> closed with it: both files now **label** their arrow (`──►` "is available to" in #20, `←` "is
> referenced by" in `src/CLAUDE.md`), so a reader checks the label rather than the arrowhead. Files:
> #20 `section-2.md` **v1.1** *(since v1.2, the same-day reviewed-findings pass)* (FR-CS-046 restated; **FR-CS-046a** registered as a sub-clause so the
> FR-CS-046…055 span and the 73-FR count are unchanged; the published double negative "No assembly
> **MUST NOT** reference…" repaired), `section-3.md` **v1.1** *(since v1.3)* (§3.5.2 replaced), `section-5.md`
> **v1.0.2** (§5.4.5 item 1), `src/CLAUDE.md` (taxonomy + Reference Direction rewritten, the ⚠️
> 16-unlisted banner retired, the `code-standards` phantom infrastructure row struck,
> `project-constants` stated as tier 0 rather than infrastructure). Three FR-CS-056/057 header defects
> corrected in passing: those three section files each read `Status: DRAFT` against a SPEC_INDEX status
> of APPROVED, two of them behind their own version history. **Deliberately not swept, and listed so a
> later widening does not have to rediscover it:** `outline.md` / `outline-mid.md` /
> `outline-detailed.md` still describe the three-layer chain — pre-authoring artifacts, not normative
> text; every *normative* site was swept and no other hit exists in `docs/specs/`.
>
> **SHIPPED (2) — the injury/aging research-alignment supplement SIGNED OFF** (v0.5; the file is now
> at **v0.6** — see the ⚠️ correction at the end of this block). All three
> structural decisions accepted as written: the `Severe` tier + `RECOVERY_MAX` raise (R-5), the #41→#28
> aftermath seam (R-6 / KD-R4, with v0.3's consumer-owns-the-seam-type correction), and the
> deterministic-not-drawn PA reduction (KD-R4a). It was AR-converged and blocking only alignment work,
> and a later revision costs one supplement edit — against the standing cost of a live-but-unsigned
> design the next #41 landing has to read and guess at. §11 steps 1–2 complete; steps 3–5 (the
> ERR-041-013..018 / **ERR-028-020..022** back-props (re-allocated August 17, 2026 — the -002..-004 block named here was already consumed: -002 at #53's approval on July 27, -003/-004 at the #28 T1/T2a landing), the section patches, the two implementation tranches)
> are unblocked and **unscheduled**. The standing caveat is unchanged and load-bearing: R-2's
> under-exposure arm must re-fit *against* `BaselineDailyRisk` rather than beside it, and every `[GT]`
> it moves is subject to KD-W1 — which is the same W2 dependency four of these seven items share.
> ⚠️ CORRECTED August 18, 2026 (propagating the supplement's own v0.6 post-sign-off correction,
> reviewed findings H2 + H3): this block recorded the id re-allocation but not the finding that
> mattered — the sign-off's factual premise **KD-R1, "both specs are pre-code so the changes are
> free", was re-verified after the fact and found VOID.** #41 and #28 both have live save codecs
> (`MedicalSaveCodec` since August 5, `ProgressionSaveCodec` since August 8, both mandatory sub-blobs
> of #30's frame), so **R-4 and R-6 now cost a `MEDICAL_SAVE_FORMAT_VERSION` 1 → 2 and a
> `PROGRESSION_SAVE_FORMAT_VERSION` 1 → 2 bump with no migration path** (the KD-7/F3 no-migration
> refusal). The owner's sign-off of the three structural decisions (R-5, R-6/KD-R4, KD-R4a) is
> unchanged; the cost/timing argument is not.
>
> **HELD (3) — the two depleted-squad sub-questions, closed in the owning spec rather than only in a
> tracking file** (#30 `section-3.md` **v2.12** *(since v2.14)*). **(a) The back-fill trigger stays the eighteen-player
> selection walk; the short-bench posture is NOT built** — the honest fix for a depleted club is cover,
> not a shorter bench, so `ERR-044-003`'s stages 2–3 (youth call-ups, then generated low-attribute
> cover) *retire* the eleven-vs-eighteen question rather than answer it, and a short-bench mechanism
> built now would be thrown away when that ladder lands; it is also engine surgery, and narrowing the
> probe without narrowing selection reproduces the divergence `LineupSelector.TrySelect` was collapsed
> to one walk to prevent. `ERR-030-044`'s open half is CLOSED. **(b) The beyond-cap branch keeps
> degrading to greedy; refusing the fixture is REJECTED** — refusal breaks the twice-affirmed "the
> composed filter can never leave a club worse off than having no filter at all" invariant to buy a
> corner (`m > 12` concurrently suspended in one squad) that is unreachable at measured card rates.
> Both are written inline at the rules they govern, so a reader meets the decision where the question
> was, and both name what would reopen them.
>
> **HELD (4) — the DisciplineConfig restructure stays deferred to the `GameplayConfigHolder.Bind`
> composition-root pass, tree-wide.** The defect is future-conditional, the completeness tripwire
> already converts it from silent to a red test, and a #44-only restructure would create precisely the
> parallel-surface asymmetry this repo keeps filing — one subsystem on a validated readonly-struct
> config while every sibling keeps the guard-and-pre-flight shape.
>
> **HELD (5) — KD-7a: neither adopt nor reject; decide after the post-W2-arming capture, exactly as its
> own tripwire says** (`league-bootstrap-design.md` **v1.5** *(since v1.7)*, new **S9**). The corpus that would
> determine `α` predates tackle wiring, so S7 condition 4 fails on its own terms. **S7's condition 4
> was also corrected in place:** it was written on August 12 as "no player has ever made a tackle",
> which W2's landing *that same day* made false about the code while leaving it true about a shipped
> match (`TackleContactRadiusM = 0`) — it now reads as **post-arming**, or the tripwire would fire
> against a corpus statistically identical to the one it exists to reject.
>
> **HELD (6) — `pointQuality` stays parked until the close-range CONVERSION comparison on identical
> seeds exists** *(heading corrected August 18, 2026, reviewed finding H13: it read "until W1's rush
> geometry is MEASURED" — a condition satisfied August 12, 2026, five days before this hold was
> written; the rush ANATOMY is measured, and the conversion pair is what `gk-rush-trigger-design.md`
> §6 still owes — see the ⚠️ H6 correction in the body below)*
> (`gk-conversion-at-contact-design.md` **v1.1** *(since v1.3 — the currency pointer here read "since v1.2" and was itself overtaken when the KD-CC6a heading was corrected on August 18, 2026)*, new **KD-CC6a**). The §4 ladder's refusal was
> measured against a keeper who never left his line; W1 moves the contact geometry the whole ladder is
> a function of. **The unparking condition is a measurement, not a landing** — W1 landed August 4, 2026
> and has never been executed. **⚠️ CORRECTED August 17, 2026, same day (adversarial-review finding
> H6): the "never been executed" clause is FALSE and is annotated rather than deleted.** W1's rush
> anatomy WAS measured on August 12, 2026 — 23–46 rush intents committed per match against a pre-W1
> baseline of exactly 0 by construction, keepers reaching 9.1–14.1 m off their own goal line, no
> `ERR-011-009` re-stall (`gk-rush-trigger-design.md` §6 / v1.5, and `match-engine-wiring-backlog.md`
> §5 row 1: "Its owed measurement is discharged"). What remains unmeasured — and is the real
> unparking condition, now stated in KD-CC6a (v1.2) — is the close-range **conversion** comparison on
> identical seeds that §6 names as still owed. The park's conclusion survives on that ground. Until
> that pair exists the recorded refusal is a fact about the pre-rush keeper and
> must not be quoted as a fact about the mechanism.
>
> **HELD (7) — the foul/card drift stays accepted: arm W2 first, then calibrate ONCE**
> (`foul-discipline-balance-design.md` **v1.1** *(since v1.2)*, new §7 item **2a** *(renumbered to item **3** at v1.2 — `2a.` is not a valid list marker)*). That note's own item 2 said
> `FoulCallProbability` must be re-measured if the contact stream changes; C1 changed it on August 8
> and the fit drifted **~67%** unnoticed for **five days** — August 8 to the August 13 re-measurement; eighteen days from the July-26 fit itself. *(Corrected August 17, 2026: this entry first said "five weeks", wrong by ~5× under every anchoring, and load-bearing, since the interval is the counter-example the hold is justified by.)* Applying the rule literally means *not*
> landing an interim fit against a pre-tackle stream that W2's arming will change again. **Accepted
> cost, stated plainly:** the card rate — and since #44 the suspension rate derived from it — stays
> knowingly wrong meanwhile, with the acceptance bands (fouls ≤ 90, yellows ≤ 20, reds ≤ 5) reading
> green throughout.
>
> **`match-engine-wiring-backlog.md` **v1.10** *(since v1.13)* records the consequence none of these three holds could
> see individually: W2's *arming* now gates three separate decisions** — the foul/card calibration,
> KD-7a's successor distribution, and everything behind the un-isolated `sim_match_engine_inposs_gate`
> stall whose leading candidate is W6. The path **W4 → W12 → W6** therefore unblocks three decisions,
> not just the next wired subsystem, and `TackleContactRadiusM = 0` is doing far more holding-back than
> its one-constant footprint suggests.
> ⚠️ CORRECTED August 18, 2026 (propagating the backlog's own v1.11 L11 correction): "gates three" is
> FALSE — W2's arming gates **two** held decisions (the foul/card calibration and KD-7a's successor
> distribution); the third item, the un-isolated `sim_match_engine_inposs_gate` stall, is what BLOCKS
> arming, not something arming unblocks, so it does not belong beside them. The citation above is
> re-pointed: `match-engine-wiring-backlog.md` is at **v1.11** *(since v1.13)*, whose §5 note carries the corrected
> two-plus-the-blocker form; the W4 → W12 → W6 path aims to clear the blocker and thereby unblock
> the two.
>
> **THE PREMISE THAT WAS FALSE — the approval tags.** The instruction was "push them; no downside", and
> the attempt found the entry had been wrong for four months. **The eight annotated tag objects do not
> exist anywhere**: `git tag` in a fresh clone returns nothing, `git ls-remote --tags origin` returns
> nothing. They were created in the April/May 2026 authoring containers, never pushed, and are
> unrecoverable — their messages, which recorded each original sign-off date and §9 checklist status,
> are gone with them. Every session since has read "created locally, not yet pushed" as meaning they
> were sitting somewhere ready to go. **The precondition check as written cannot run**, because all
> three of its steps take a tag object as input; what it was really asking was answered without one —
> `origin/main` uses true merge commits, so it is path (3), not the squash-merge path (2). **The 403 is
> still live**, reproduced today from this session on both `--tags` and a single-tag push, with the
> proxy healthy: the refusal is GitHub-side and this session's credentials push branches but not tags.
> **Re-creation is possible and its targets are now resolved** — eight tags against four commits
> (`caaf5cf0`, `a88dba03`, `bcf94199`, `7dbcf121`), each verified an ancestor of `origin/main`; the
> recipe is recorded in the open-issues entry because tags created in this container die with it.
> **One honesty point carried into the recipe:** #1/#3/#4 were signed off in February 2026, before any
> spec text existed in git, so **no commit represents the moment of their approval** — `caaf5cf0` is
> the earliest commit that *contains* them at APPROVED, and must be tagged as a containment claim, not
> an approval claim. That is still better than what the originals did, which was to point all four
> April-26 tags at one branch HEAD with no per-spec meaning at all.
>
> **Open issues: 20 → 18 active / 44 → **47** resolved**, both re-derived by direct count
> (`grep -c '^- \*\*'`) rather than by arithmetic — this file has recorded three counts reached by
> incrementing an unverified base, so the count is measured every time.

> **Last Updated (prior):** August 16, 2026 (rounds 10-12) — **#44/#30 adversarial-review ROUNDS 10-12, a
> continuation of the rounds-8-9 chain over the SAME `ERR-030-044`/`ERR-044-019` tier-2 reinstatement
> landing.** Commits `a4a672b..d0f534a` (`ba2c574..HEAD`). **Round 10 (`a4a672b`, wave A) — M/L
> findings, visibility/locks/text residues, no High:** the `DisciplineConfigCompletenessTests`
> reflection scan widened to see `NonPublic` fields (an internal `[GT]` can no longer ship unguarded);
> the constructor's `onPitchAgentIdCount` range guard and the production `SQUAD_SIZE` cross-assembly
> occupancy-swap argument both gained mutation-verified locks; `ERR-044-013`'s owed `NO_PLAYER` spec
> note discharged; stale `SelectAvailable` references in `LineupSelector`/`SeasonSaveManager`
> re-pointed or past-tensed (the type itself was deleted at round 9, before `ba2c574`; this closes the
> citations round 9 left behind); twelve duplicated oracle comments collapsed into one `ComposedOracle`
> helper; a division-derived club-membership assertion replaced with real roster-array membership;
> `match-analytics` card routing now fails loud on an unknown kind. Text: section-2's four-vs-five
> miscount corrected, `ERR-044-014`'s stale remainder list closed, the `open-issues.md` #44 GATE
> placeholder filled with the `051c25a` verdict, Appendix C's `onPitchAgentIdCount = 22`.
> **Round 11 (`21bde36`, wave B) — 1 High, `ERR-030-046` ESCALATED under the no-third-identical-retry
> rule:** the SAME tier-2 within-tier defect survived TWO successive fixes (`ERR-030-044`'s roster-order
> key, `ERR-030-045`'s ascending-`PlayerRating` re-key) because both were an element-wise greedy
> decision of a fundamentally set-valued, per-position constraint — a global scalar cannot see that a
> squad has no room for the globally weakest banned player at HIS position, and measured on generated
> mass-suspension fixtures, **≥ 476 of 1920** had a clean completing choice the algorithm missed. The
> third attempt was ruled, not iterated: `ChooseSuspendedCandidate` becomes a capped exhaustive
> clean-completion search over subsets of the still-removed candidates (new `[FIXED]
> EXTREMIS_SEARCH_CANDIDATE_CAP = 12`, an algorithmic budget, not a `[GT]`), with the guarantee restated
> as a THEOREM — the composed squad fields the minimum achievable number of reinstated-suspended
> players, zero whenever any completing choice benches them all. Locked by three new
> `AvailabilityCompositionExtremisTests` cases (`WeakPositionExtremis`, `MinimalStallExtremis`,
> `CapFallbackExtremis`; suite 5 → 8), cross-filed at #44 as an extension of `ERR-044-019`.
> **Round 12 (`d0f534a`) — 0 High / 1 Medium / 5 Low, all fixed in this commit, over the round-11
> landing.** The Commit rule's third branch (`|R*| ≥ 2` with every member singly completing) was
> unreachable, unproven, and would have violated round 11's own theorem if it were ever live: a
> **monotonicity lemma** is now STATED and proven — `LineupSelector`'s per-position top-`k` selection
> makes `dirty(R)` monotone non-decreasing under adding candidates, so a completing singleton is
> already the global minimum — verified `thirdBranchReachable = 0` over 6,858 generated oracle cases,
> with the collapsed `order[bestSubset[0]]` form behaviour-identical over 3,966 further cases and
> 11/11 tests (an independent monotonicity proof over the code slice, not merely a restatement of
> round 11's claim). The third branch is now a fail-loud `InvalidOperationException` naming the lemma,
> its re-choice machinery deleted rather than kept dead; a fourth Commit case binds "no subset completes
> at all" for the first time, with a hoisted full-set probe skipping the whole `m·2^m` enumeration on
> that branch. Both "self-heals" beyond-cap residual statements were wrong the same way — conflating the
> SEARCH resuming with the GUARANTEE resuming — corrected at six sites (a later reviewed-findings pass
> found this same commit's own test-file comment had been missed, making the real total nine; see
> `spec-error-log.md`'s `ERR-030-046` annotation). Two new mutant-killers, `InCapBoundaryExtremis`
> (`m == CAP` exactly) and `TiedForcedStartExtremis` (a tied positive dirty count), each verified by
> actually applying its named mutant and observing the failure before reverting; the discipline slice's
> `ERR-044-022` ordering claim — previously unfalsifiable, since the constructor copies the seed and a
> fresh fold is pristine regardless of guard ordering — is now genuinely locked through a new internal
> `CardLedgerFold.OccupancyAt(int)` observation seam, with the guard-reordering mutation observed
> FAILING 145/146; a null-seed constructor lock added. Five mutations executed and observed across the
> round: the two `AvailabilityCompositionExtremisTests` mutant-killers above, the `OccupancyAt`
> guard-reorder, the null-seed guard deletion, and the collapsed-commit-rule equivalence check.
> **THE GATE (measured at `d0f534a`):** meta-integrity OK; build 0 errors; 34 suites, quarantine empty;
> `MatchEngine.Tests` **461 passed / 1 failed / 11 skipped** — the sole failure is the inherited
> owner-held-red `sim_match_engine_close_chance`, identical at baseline, no new failure;
> `Discipline.Tests` **146/146**; `SeasonSave.Tests` **447 passed / 0 failed / 3 known skips**;
> `MatchAnalytics.Tests` **59/59**. Assemblies touched: `src/discipline/`, `src/season-save/`,
> `src/match-engine/` (`SquadRating.cs` + `LineupSelector.cs`, doc/lock only), `src/match-analytics/`
> — **no `src/deterministic-sim/` change this stretch**, unlike the rounds-8-9 entry. No new files (21
> files modified across the three commits, zero added). Full account: `CHANGELOG-src.md`,
> `spec-error-log.md` v2.42 (`ERR-030-046` and its round-12 annotation).
>
> **Last Updated (prior):** August 16, 2026 (rounds 8-9) — **#44 adversarial-review ROUNDS 8-9, a fresh
> three-reviewer pass over the WHOLE branch state (AR-8 and AR-9 in the chain — the prior chain ended
> at round 7). Round 8: 3 High / 12 Medium / 7 Low, all fixed. Round 9, a fresh full re-review of
> round 8's own landing: 1 High / 10 Medium / 5 Low, all fixed.** Commits `2f4626f..051c25a`.
> **Round 8's Highs:** **`ERR-044-014`** — `DisciplineRules.OnClubFixturePlayed` decided club
> membership by `PlayerId / CLUB_SQUAD_SIZE == clubId`, a second notion of membership from
> `Availability.MarkSuspended`'s real roster walk, agreeing only while #27's id packing holds and
> validated by nothing; the derivation is DELETED, not guarded — `SeasonLoop` now supplies the
> unfiltered roster ids at the same resolve→filter→configure site the fielded XIs come from. **
> `ERR-044-015`** — #44 §4.5's composition-root MUST named `FilterAvailable`, the one method four
> other sections forbid the root to call; rewritten to the landed `MarkSuspended` →
> `AvailabilityComposition` contract. **`ERR-030-044` + `ERR-044-019`** (cross-filed at both owners)
> — the extremis back-fill fired on a BENCH shortfall rather than an inability to field an eleven, so
> a club seventeen fit and one bench short could have its best banned man reinstated into the
> rating-greedy selector's STARTING eleven; tier 2's key becomes probe-qualified (prefer the first
> candidate, in roster order, the selector would BENCH — his ban then advances normally), forced-start
> corrected to the honest two-case form (benched ⇒ ban advances; forced start ⇒ exempt, and only then
> does ERR-044-003 stage 1's serving exemption stall it). **Round 8 also:** `ERR-037-003` (M4) —
> match-analytics routed second-yellow dismissals to plain yellows (a two-way branch over a
> three-value ordinal domain), now counting one yellow AND one red per the box-score precedent;
> `ERR-044-016`/`017`/`018` — Appendix C's worked fold example (an engine-unproducible kind-2-as-
> first-booking) fixed with a preceding kind-0 card, the four `[GT]` threshold/ban constants renamed
> ALL_CAPS → PascalCase across every spec file to match the code, and §2.2 declares `DisciplineState`'s
> full landed API; `ERR-044-020`/`021` — the tap's `CurrentTick` member + `CardLedgerFold.ObserveTick`'s
> consecutive-tick refusal and partial-application poison latch synced into sections 2/3/4; both
> `DisciplineRules` card-accumulation paths (`AddYellow`/`AddBan`) now route through guarded overflow
> helpers, locked at `int.MaxValue`; `CardLedgerFold` occupancy made injective by construction
> (`ApplySubstitution` clears the vacated slot, refuses `outgoing == incoming`, the constructor refuses
> a non-injective seed). **M2, an owner (Fable) ruling rather than a fix:** the reviewer's proposed
> `DisciplineConfig` readonly-struct restructure (retiring `CommitWithExplicitConfig`, both
> `RequireCommittableConfig` forms, and the driver pre-flight/guard seams) is recorded and DEFERRED —
> it needs #44 §2.3 F6 and #30 §3.4 spec edits and belongs at (or after) the `GameplayConfigHolder.Bind`
> composition-root pass, as the tree-wide pattern rather than a #44-local fix; landed instead as the
> minimal completeness lock plus five pointer comments (see `open-issues.md`'s new head bullet).
> **Round 9's High, `ERR-030-045`** — a second adversarial pass over the `ERR-030-044` landing found
> its fix incomplete: fieldability is monotone, so while a club is short by more than one, NO candidate
> is fieldable and passes 1-2 are structurally unreachable — pass 3's OLD earliest-roster key decided
> every reinstatement but the last blindly, recreating the pre-fix defect for exactly the
> multi-reinstatement population #44 exists for. Fixed with pass 3's key as ASCENDING SELECTOR RANK
> (new `SquadRating.PlayerRating`, a delegation to the engine's own `LineupSelector.MeanAttribute` —
> one selector, four read shapes, not a second rating formula), ties on roster order; the weakest
> banned are pressed back first, so the final pick's pass 1 still finds a non-starting candidate
> whenever one exists. Stated as a minimisation, not a guarantee — the k≥2 residual (no candidate
> choice avoids starting a suspended player) and positional forcing both survive by design. **Round 9
> also:** **`ERR-044-022`** (M13) — `CardLedgerFold`'s constructor gains a required
> `onPitchAgentIdCount`; `ApplySubstitution` now refuses an on-pitch `Incoming` and a bench `Outgoing`
> — Appendix C's slot-19 malformation throws instead of silently destroying the outgoing player's
> occupancy mapping (the v1.8 row's false "impossible" claim annotated in place). **`ERR-044-023`**
> (M14) — the seed's one-to-one precondition `CardLedgerFold` assumes is violated by its only
> documented producer (`MatchEngine.PlayerIdsByAgentId` is injective only AT BOOT, never again after a
> substitution); bound normatively at #44 §4.3, doc-only at `MatchEngine.cs`, locked against the real
> engine. **M5 closed for good:** `PlayerCareerStates.SelectAvailable` — made internal at round 8,
> DELETED entirely at round 9 (its "one call site needs it" justification named a call site that goes
> straight to `Compose`) — the production-dead oracle surface that shipped the C1/C2 landing's own H2;
> all ten test oracle sites re-pointed at `AvailabilityComposition.Compose(squad, career,
> discipline: null, 0)` with explicit oracle-scope comments. **The round's final wave (text residues)**
> swept the refuted "plays only when the club cannot take the field at all" sentence to its three
> remaining live sites (root `CLAUDE.md`, `open-issues.md`, one `SeasonLoopDisciplineTests` assertion
> message), annotated the `ERR-044-020` row's own CLOSED note (it had lived only in this file's header
> chain), completed the section-1.3 reference DAG and the section-6.2 drifted line-citations → member
> names, and finished the section-3.2 ALL_CAPS → PascalCase sweep. **The two Lows round 9 left open —
> the missing landing record for rounds 8-9 in this chain, and the stale `Discipline.Tests` suite
> count carried in `file-manifest.md`/root `CLAUDE.md`/`open-issues.md` (81 → 118, never advanced past
> either round) — are the two this close-out entry itself lands**, re-derived by direct execution
> rather than incremented. **GATE at `051c25a`, August 16, 2026:** meta-integrity OK; build 0 errors;
> 34 suites, quarantine empty; `MatchEngine.Tests` **461 passed / 1 failed / 11 skipped** — the one
> failure is the inherited owner-held-red `sim_match_engine_close_chance` (also failing at the
> pre-branch baseline; owner call August 11: hold red), so the branch adds no new failure;
> `SeasonSave.Tests` **441 passed / 0 failed / 3 known skips**; `Discipline.Tests` **143 passed / 0
> failed**; `MatchAnalytics.Tests` **58 passed / 0 failed**. Assemblies touched: `src/discipline/`,
> `src/season-save/`, `src/match-engine/` (`SquadRating.cs` + `MatchEngine.cs` doc-only), `src/
> match-analytics/`, `src/deterministic-sim/` (doc-only) — full account in `CHANGELOG-src.md` and
> `spec-error-log.md` v2.39.
>
> **Last Updated (prior):** August 15, 2026, later still again (round 7) — **#44 adversarial-review ROUND 7: 0
> High / 0 Medium / 3 Low, all fixed — the first clean round in the chain, and docs-only.** Three Lows
> surfaced on the surface six prior rounds had never read — `src/season-save/PlayerCareerStates.cs` and
> `SeasonSaveManager.cs`, explicitly named unread by round 6's own reviewer. **L1** —
> `SeasonSaveManager.Load`'s doc claimed the career-coherence gate is match-only ("a save with no match
> is untouched by this"); false since the AR-pass-5-era load-side landing — `RequireCoherentCareerBlocks`
> runs unconditionally, before the `MatchBlob` branch — corrected to state the guarantee holds on every
> load rather than leaving a maintainer to read the careerless path as ungated. **L2** — three sites
> still described a five-blob frame, one of them omitting three of `Save`'s ten parameters including the
> roster and the suspension tally; fixed by pointing at `SeasonSaveBlobs`/`SeasonSaveContents` rather
> than a fourth hand-restatement, since restating the frame shape is what produced the third and fourth
> divergence (this file's own recorded omission class, filed twice before at v1.6 and v1.17). **L3** —
> `SelectAvailable`'s doc still claimed to own the depleted-squad rule and viability check that moved to
> `AvailabilityComposition` at v1.19, and described #44's filter in future tense for a filter that has
> been live since August 13. **Checked before landing, not after:** the new text says "eight-blob frame"
> while this branch's other records say "seven mandatory sub-blobs" — both correct, describing different
> sets (`SeasonSaveBlobs` carries eight: World, Season, Training, Medical, Appearance, Progression,
> Discipline, Match — of which Match is optional, leaving seven mandatory); no divergence introduced.
> **What the round did NOT find is the substantive result** — it chased this branch's own recorded
> defect classes (a fourth save/restore entry point, partial writes, the cursor-vs-clock guard family,
> aliasing, numeric edges, restore fidelity) and cleared every one by tracing rather than asserting,
> including withdrawing its own hypothesis after measurement (suspected the Save-side calendar-cursor
> invariant was unlocked; a `Load_`-prefixed test was found to assert the Save side too, so the guard is
> locked and the hypothesis was wrong). **No production behaviour changed** — `PlayerCareerStates.cs`
> and `SeasonSaveManager.cs` gained doc-comment corrections only, no logic, signature or method body
> changed; `season-save` builds 0 warnings / 0 errors. **No whole-tree gate run this round** — nothing
> outside doc comments changed, so none was needed. **Aggregate across rounds 5-7 (this entry and the
> two below):** 3 High / 25 Medium / 21 Low, all fixed (round 5: 1H/14M/10L; round 6: 2H/11M/8L; round
> 7: 0H/0M/3L); **16 new ERR ids filed** — 11 in round 5 (`ERR-017-005`, `ERR-020-004`, `ERR-030-040`,
> `ERR-030-041`, `ERR-044-006` through `ERR-044-012`), 5 in round 6 (`ERR-017-006`, `ERR-020-005`,
> `ERR-030-042`, `ERR-030-043`, `ERR-044-013`), 0 in round 7; production code changed across five
> assemblies — `src/discipline/`, `src/season-save/`, `src/event-system/`, `src/match-analytics/`,
> `src/match-engine/` — plus `src/CLAUDE.md` (round 5, the ERR-020-004 carve-out). This corrects the
> reviewed-findings brief that seeded this close-out, which had cited "2 High … 11 new ERR ids" for the
> three rounds combined; the figures above are re-derived from `git log --stat 45a29ae^..5c2e4a6`, the
> corresponding `ERR-044-0NN` body entries, and the round commit messages themselves, not carried
> forward from that brief.
>
> **Last Updated (prior):** August 15, 2026, later still (round 6) — **#44 adversarial-review ROUND 6:
> 2 High / 11 Medium / 8 Low, all fixed; gate run to completion, 34 suites, quarantine empty, and the
> FIRST gate on this branch whose verdict actually covers the CI surface.** **H1 (`ERR-030-042`)** — #30
> §3.4, the OWNING normative text for the depleted-squad back-fill, stated ONE ordering key and asserted
> #44 "inherit[s] the rule unchanged"; `AvailabilityComposition.Reinstate` has implemented TWO tiers
> since the C1/C2 landing (injured before suspended). Because `recoveryRemaining` is written only by
> `PlayerCareerStates.MarkUnavailable`, a suspended-but-uninjured player keeps the `int` default 0 — an
> implementer following §3.4 verbatim sorts him to the FRONT of the ascending-recovery order and presses
> banned players back ahead of every injured one, the exact inversion of the owner's ERR-044-003-stage-1
> decision, and silently: the never-worse-off invariant still holds, the selector still returns a
> fieldable eleven, nothing fires. Fixed spec-only — the code was already correct — by stating both
> tiers as part of the rule #30 owns, requalifying "inherit the rule unchanged" to mean the INVARIANT
> rather than the ORDERING, and writing the zero-default trap into §3.4 as an explicit MUST NOT.
> `section-3.md` v2.6, `section-2.md` v2.0. **H2** — four files under `src/` (three from the C1/C2
> landing, one from round 5) had no `.meta`, and the required `unity-meta-integrity` CI job had been RED
> on this branch since August 13 — missed by five prior review rounds and four landing records all
> reading "GATE: RUN TO COMPLETION", because `run-gate.sh` never invoked that job at all; it is a
> separate CI job (`ci.yml:201`) from the one this project's gate runs. `.meta` files generated, and
> `run-gate.sh` now runs the meta check FIRST, before the ~90-minute build/test pass. **Spec Mediums
> (M1-M5, `docs/specs/discipline-suspensions/` + cross-spec):** the shared-tap claim `ERR-044-008` had
> already refuted survived in §4.5 and `outline.md` KD-2, and had escaped the spec set entirely —
> `match-presentation-depth/section-1.md` §1 built #48's whole live-capture argument on "one shared tap"
> (cross-spec back-prop, same id); `FilterAvailable(in Squad)`'s signature was wrong in two more places
> (FR-DC-009, §1 KD-4) after an earlier fix corrected only the illustration; §1 KD-4 still asserted the
> "byte-identity-locked" claim `ERR-044-006` withdrew the same day; §6.2 priced the zero-call-site
> `FilterAvailable` while the real production surface, `MarkSuspended`, had no bullet; `CardLedgerFold.
> NO_PLAYER` — caller-facing, throws-if-violated, used normatively in Appendix C — had no declaration
> and no tag anywhere, filed **`ERR-044-013`**. **#30/#17/#20 Mediums:** `ERR-030-043` (§4.3's
> `SeasonLoop` holdings list had no #44 entry — the THIRD recurrence of that section's own recorded
> omission class), `ERR-017-006` (§3.10 claims every constant it declares appears in its own catalogue
> while omitting two of its own allocated error codes, `0x1706`/`0x1707`), `ERR-020-005` (§4.2's
> `ERR-020-004` carve-out was contradicted 25 lines later, under the heading a reader actually looks
> under). **The count correction, caught inside the round that made it:** `ERR-044-006`'s own body said
> "105/105 — the suite's measured count today" when the true figure was already 118 two commits earlier,
> and root `CLAUDE.md` said 81/81; the fixer re-derived it by RUNNING the suite (118/118) rather than
> copying the brief's figure. **`src/` Mediums/Lows:** **M1** — the AWAY club's `fieldedPlayerIds`
> argument was unlocked; swapping `awayXi` for `homeXi` in the away call still passed 22/22, because
> round 5's own new end-to-end exemption test banned home-club players only — the home-team-only trap
> (`ERR-008-002`) recurring inside that same round's new lock, in a file that cites `ERR-008-002` by name
> two tests earlier. Now `[TestCase(true/false)]`-parameterised over the club under test rather than a
> copy. **M2** — the round-level `[GT]` pre-check's "runs before anything is written at all" claim had no
> defending test; the only lock asserted `Played == False`, which stays green if the pre-check moves
> below `RunCareerDaySteps`. **M3** — #30 §5's `T-SN-DET-004` had no locking test for the back-fill TIER
> ORDER at all, which is exactly why `ERR-030-042` had no detector; added (with both an injured and a
> suspended player removed and the club unable to field a formation, the injured one is reinstated
> first) — its first version was itself wrong (failed against correct code on a seed one player short of
> position-complete) and was corrected before landing, recorded rather than silently fixed. **Four
> Lows** on stale `FoulOrdinalNone` / single-consumer-mirror claims and one misplaced comment splice;
> `LEAGUE_COMPETITION_KEY` renamed `LeagueCompetitionKey` (PascalCase, matching its `[CROSS]` tag) with
> call sites. **GATE:** `Discipline.Tests` **118**/0, `SeasonSave.Tests` **435**/0/3 known skips (+2, the
> away-club and tier-order locks), `MatchAnalytics.Tests` 57/0, `EventSystem.Tests` 54/0,
> `MatchEngine.Tests` 461/**1**/11 (37 m 43 s). Single failure tree-wide is `sim_match_engine_close_
> chance`, read by name out of the gate log — the inherited owner-held red, identical to baseline; round
> 6 adds no new failure. **All three new locks mutation-verified on uncontested trees, each restored to
> zero diff afterward:** M1 away-club exemption (clean 24/0; `awayXi`→`homeXi` fails the `_Away` case
> only), M2 write-ordering (clean 1/0; pre-check moved below `RunCareerDaySteps` fails), M3 tier order
> (clean 24/0; the tier skip deleted fails both cases). **Process error, recorded because it cost
> hours:** three overlapping verification scripts ran against one working tree, so a run written off as
> dead was still applying and restoring mutants under the others; three stated conclusions were false
> and withdrawn — that a subagent applied the mutations (it was the reviewer's own script), that a
> killed run would not notify, and that a corrupted baseline showed a real failure (the clean baseline
> is 24/0).
>
> **Last Updated (prior):** August 15, 2026 (round 5) — **#44 adversarial-review ROUND 5: 1 High / 14
> Medium / 10 Low, all fixed; gate run to completion, 34 suites, quarantine empty.** Three fresh Opus
> reviewers ran over disjoint slices (`src/discipline/`, the `season-save` composition layer, the
> card-kind chain + #44/#30 specs); the High by Opus, Medium/Low by Sonnet. **The High (`ERR-044-006`)
> is the repo's founding trap recurring inside its own fix:** #44 §5's traceability table named two
> tests that do not exist — `T-DC-VIEW-001`'s only test was deliberately deleted at round 1 as
> tautological with no replacement, and `T-DC-INT-001`'s reflection assertion was never written at all —
> while §9 ratified G6/G13/G14 on them. Round 4's own pass, which existed to fix §5's staleness, had
> verified only the four rows it added and then re-certified G14 against "the corrected table".
> Verifying every surviving row found three MORE false rows (`T-DC-VIEW-002`, `T-DC-FOLD-001`,
> `T-DC-DET-001`) and two FRs (FR-DC-002, FR-DC-022) traced by nothing; G14's wording widened to three
> dispositions (Test / Construction / Deferral), §5.6 replaced by a per-FR map so it is re-derivable by
> grep. **`src/discipline/` findings:** eight fail-loud guards survived deletion against the whole
> suite with `DisciplineEntry` carrying no test file at all — new `DisciplineEntryTests.cs` plus cases
> added to `CardLedgerFoldTests`/`DisciplineRulesTests`, `Discipline.Tests` 105 → 118;
> `LEAGUE_COMPETITION_KEY` re-tagged `[FIXED]` → `[CROSS]` as a verbatim copy of APPROVED #43's
> `LEAGUE_COMPETITION_ID`; four Lows on stale/vacuous test claims and one previously-undocumented
> ordering dependency (the fold's reliance on `RunResolvePhase` flushing substitutions before card
> issuance, now stated and locked by a Sub-then-Card test). **Season-save + #30/#17/#20 findings (landed
> in a second commit, with the gate still running at commit time — recorded as such rather than claimed
> verified early):** `ERR-030-040` — the "`OnClubFixturePlayed`'s only guard is `clubId < 0`" claim
> (both the code comment and #30 §3.4's mirror) was stale since ERR-044-003 stage 1 added a mandatory
> `fieldedPlayerIds == null` guard in the same block; both now name two guards and state why the null
> case is structurally excluded. A recorded verification (M3) was retracted as FALSE and corrected in
> place: deleting the pre-check does NOT make `Assert.Throws` see no exception (the method still
> throws) — the only discriminating assertion is `Played == False`, and the retraction preserves the
> original wrong text so the next reader cannot trim the assertion actually doing the work. **M4** — 53
> of 63 public `Save` call sites answered `disciplineWired: true` while driving no discipline: round 4's
> High deleted a forwarding overload that hardcoded `true`, and the suite then re-established `true` as
> the reflexive value at 53 hand-written sites — the hardcode merely redistributed. Now 60 `false` / 16
> `true`, each checked individually. `ERR-030-041` — #30 §3.5 justified the sweep's placement with "the
> sweep is NOT idempotent"; it is (`RollToNextSeason` sets `Yellows := 0`, so a second run finds every
> row already zero and writes nothing) — placement kept, justification corrected. **Both owner
> decisions, decided on evidence rather than preference:** the card-kind tag stays `[FIXED]` — the three
> ordinals are `public const byte` with zero `Config.Get` reads anywhere in `src/`, and a `const` inlines
> into every consumer, so the code is structurally incapable of being `[GT]`; and the `[CROSS]` routing
> rule gains an owning-catalogue carve-out (a spec-owned encoding mirrors from its owner regardless of
> consumer count), the false "#44 is the only consumer" justification deleted since three assemblies
> consume it — filed `ERR-017-005` / `ERR-020-004`. Also: #17 Appendix A's `foulOrdinal` row corrected
> `byte` → `ushort` (the struct has been `ushort` since June and the engine publishes `0xFFFF`, a value
> `byte` cannot hold), and `MatchEngineConstants.FoulOrdinalNone` gets its own catalogue home as the
> `[CROSS]` mirror of #17's new `FOUL_ORDINAL_NONE`. **Six guards mutation-verified individually** (three
> `DisciplineEntry` constructor guards, `AddBan`'s `matches < 0`, `CardLedgerFold`'s empty-seed and
> negative-seed guards) — neutering any one now fails exactly one test. **`src/CLAUDE.md` also edited
> this round** (the `ERR-020-004` carve-out) — see that file's own version chain (`CHANGELOG-src.md`).
> **GATE:** `Discipline.Tests` **118**/0, `SeasonSave.Tests` **433**/0/3, `MatchAnalytics.Tests` 57/0,
> `MatchEngine.Tests` 461/1/11 (38 m 35 s), every other suite green. Single failure tree-wide is
> `sim_match_engine_close_chance`, read by name out of the gate log — the inherited owner-held red,
> identical to baseline; round 5 adds no new failure. **11 new ERR ids filed this round:** `ERR-017-005`,
> `ERR-020-004`, `ERR-030-040`, `ERR-030-041`, `ERR-044-006` through `ERR-044-012` (seven ids).
>
> **Last Updated (prior):** August 15, 2026, later (**#44 adversarial-review round 4 CLOSED — all ten findings (0 High / 6 Medium / 4 Low) fixed, gate
> run to completion.** The round-4 findings had sat unapplied since August 13 (a session ended at a
> usage limit). Split by file domain across two delegated agents so they could not collide. **M22** —
> the round-level `[GT]` pre-check called a static nothing in `src/` binds a config for, so deleting
> it left the tree green; routed through the `IFixtureDisciplineDriver` seam. Verified by deletion,
> and the result needs stating precisely because the naive reading is wrong: deleting the call DOES
> fail a test, but not on the thrown exception — that still arrives via the per-fixture driver call —
> it fails on the assertion that no fixture was touched, the fixture being already marked `Played` by
> then under M6's ordering. That is exactly the wedge the pre-check exists to prevent. **M23** —
> claims corrected, **no lock manufactured**: an inline copy of the delegated-to body passes either
> test, so the pair structurally cannot prove delegation and now says so. **M24** — the card-kind
> encoding, which lived four times bound nowhere under two tags, is declared ONCE as `[FIXED]`/
> ALL_CAPS in `EventSystemConstants` (#17 owns the `CardIssuedEvent` payload) with `[CROSS]`/
> PascalCase mirrors in `MatchEngineConstants` and `DisciplineConstants`, `MatchEngine.cs`'s bare
> `0/1/2` literals replaced (FR-CS-016), and `MatchAnalyticsConstants` repointed at the authority;
> filed as **ERR-017-004**. **Two defects in the first draft of that fix were caught on review and
> fixed before landing** — the new constants were tagged `[GT]` (designer-tunable-via-config), which
> is M24's own defect class recurring inside M24's fix, and `MatchAnalyticsConstants` had been
> repointed at a MIRROR rather than the authority, giving the "must not diverge from its source" rule
> two hops to fail at. **M25** — #44 §5, untouched since July 24, had `T-DC-BAN-004` marked WITHDRAWN
> in place rather than deleted (the row records why the test existed and what retired it), four rows
> added for tests that existed with no §5 entry, and §9's G14 re-checked and correctly left ✅ — the
> checklist row was never wrong, only the table it cited. Every cited test name was verified to exist
> in `src/discipline/tests/` before being written into the spec; fabricated checklist values are this
> project's oldest recorded trap and a traceability table is where they hide. **M26** — FR-SN-021's
> `Save` signature corrected 9 → 10 arguments (`disciplineWired`, ERR-030-039). **M27** — §3.1's fold
> pseudocode rewritten to `CardLedgerFold`'s real buffer-then-commit-atomically shape, which an
> implementer following the old text would not have reproduced. **L18–L21** fixed; the four stale
> counts by direct measurement rather than by copying another document's figure. **The round-numbering
> divergence was recorded, not "corrected"** — file headers count review passes (to 5), `git log`
> groups the same work into 3 fix-commits; both are internally consistent and there is no single right
> number. **No production behaviour change.** **GATE: run to completion, 34 suites, quarantine empty** —
> `Discipline.Tests` 105/0, `SeasonSave.Tests` **433**/0/3 (+1, the M22 driver lock),
> `MatchAnalytics.Tests` 57/0, `MatchEngine.Tests` 461/1/11 (1 h 1 m), every other suite green. The one
> failure tree-wide is `sim_match_engine_close_chance`, **read by name out of the gate log rather than
> inferred from matching counts** — the inherited owner-held red; `MatchEngine.Tests` is identical to
> baseline, so this adds no new failure.)
>
> **Last Updated (prior):** August 15, 2026 (**Two owner decisions landed, one as code and one as sequencing.**)
>
> **(1) `ERR-044-003` stage 1 — an extremis appearance no longer serves the ban it was fielded
> through.** The #44 C1/C2 landing recorded, twice and explicitly as an owner call, that #30 §2.3 F9's
> depleted-squad back-fill can press a **suspended** player onto the pitch when a club would otherwise
> be unable to field a formation, and that the same fixture's `OnClubFixturePlayed` then decremented
> his ban anyway — so the appearance was **strictly free**, and a two-match red cost a mass-suspension
> club nothing at all. The decision is exemption. `DisciplineRules.OnClubFixturePlayed` takes a
> **required** `int[] fieldedPlayerIds` and skips the decrement for anyone in it, because the rule is
> not "the club played" but "the club played **without** him". Required rather than optional-with-a-
> default: an omitting call site would silently restore the free appearance, which is this same
> landing's own H1/H4 shape. **Fixed at the serving site, not in `AvailabilityComposition`** — the
> football rule is about who PLAYED — so the reinstatement tier order is untouched. The eleven passed
> is the array `ERR-041-010(b)`'s appearance record **already** derived at the filter+configure site,
> so no second selection walk appears (AR pass 2's parallel-surface finding); `SeasonLoop.FieldedXi`
> widens that derivation's gate from `_career` alone to the union of its two consumers, keyed on
> `_disciplineDriver` so a substituted driver cannot be handed a null eleven the production path would
> have filled. **Behaviour-identical on every fixture that does not reach the extremis tier, by
> construction** — the filter removes every suspended player before selection, so nothing but the
> back-fill can put a banned id in the eleven. No format bump, no schema bump, no draw-order change;
> #44 stays draw-free. **The better answer is agreed and NOT built, with both blockers named:** the
> Football Manager posture is a tier ladder — promote **youth**, then field **generated low-attribute
> cover**, both ahead of any suspended player — under which a banned man never takes the field at all
> and the suspended tier becomes *unreachable* rather than merely expensive, letting #30's liveness
> invariant and the Laws of the Game both hold instead of trading one against the other. Stage 2 is
> blocked on **#42 Youth having no `src/` assembly** (nothing to draw from); stage 3 on the id space —
> `PlayerId = clubId × CLUB_SQUAD_SIZE + local` is **fully packed at 25**, so a 26th player for club N
> collides with club N+1's first, and widening it touches #27 FR-SQ-010 (as amended by ERR-027-004),
> every save file, and ERR-041-019's global-uniqueness guard. `spec-error-log.md` v2.25.
>
> **(2) The foul/card calibration is sequenced behind W2 — arm the tackle first, then calibrate once.**
> The August-13 re-measurement (**fouls 35.0 / yellows 5.0 / reds 1.00 per 90** against football's
> ~22 / ~3.5 / ~0.25, fouls and yellows both ~67% above their own July-26 post-balance-pass figures)
> will **not** be fitted against today's engine. Today's foul population is **pre-tackle** —
> `TackleContactRadiusM` ships at 0 — and arming W2 routes ~47 challenges per team per 90 into the
> same single foul-candidate slot, so a fit landed now would be re-fitted immediately while its
> intermediate value sat in the tree looking calibrated. That is **KD-W1 read literally**, and the
> July-26 pass is the counter-example that makes it concrete: it fitted correctly against the contact
> stream of its day, C1's phase reclassification moved that stream in August, and nobody re-measured
> for four months. **The accepted cost is stated rather than hidden:** the drift stays live until the
> W2 calibration pass, so the card rate — and the suspension rate #44 now derives from it — is
> knowingly wrong meanwhile, with the acceptance bands reading green throughout (they cap fouls at 90,
> yellows at 20 and reds at 5; a plausibility floor, not a calibration signal). Consequence for the
> wiring backlog's own ordering: **W2 is now the precondition for the most load-bearing open realism
> item**, not merely the next wiring item. `match-engine-wiring-backlog.md` v1.9.
>
> **GATE: RUN TO COMPLETION at `eec95d0`, August 15, 2026** — quarantine empty. `Discipline.Tests` **105 / 0**, `SeasonSave.Tests` **432 / 0 / 3 known skips**, `MatchEngine.Tests` **461 / 1 / 11**. The one failure tree-wide is the close-chance scenario, identified by a name-filtered re-run rather than inferred from matching counts — the inherited owner-held red (`close-chance-creation-design.md` §10.9 item 6). `MatchEngine.Tests` is identical to the pre-change baseline, so this landing adds no new failure; nothing in it touches the match engine.
>
> **Last Updated (prior):** August 13, 2026 (**#44 Discipline & Suspensions — C1 (T0+T1) and C2 (T2) LANDED:
> `src/discipline/` is the 35th production assembly, suspensions are LIVE end to end, and the last
> gap in the season spine for PM-2 is closed.**) WHAT REMAINS: #44 T3 — the #30-owned quick-sim card
> synthesis, without which suspensions are live for one club in twenty. The subsystem is a third
> consumer of two proven seams rather than new plumbing: the #37-class per-tick ledger tap (B3) feeds
> a `CardLedgerFold`, and the ERR-030-009 resolve→filter→configure seam takes a third contributor.
> **Draw-free by construction** — no RNG stream, no domain tag, no `SubsystemOrdinals` entry, no
> `SNAPSHOT_SCHEMA_VERSION` bump (still 21). `SEASON_SAVE_FORMAT_VERSION` **5 → 6** for the seventh
> mandatory sub-blob. The pre-implementation council changed the design on two of five forks against
> `season-competition-loop/section-3.md` §3.4 (APPROVED, and later than #44): **(1)** FR-DC-010's
> engine-only filter scope contradicted FR-DC-011 and #30 §3.4 (`ERR-044-002`); **(2)** #44 §2.3 F5's
> fail-loud-below-eighteen requirement contradicted #30 §2.3 F9's back-fill rule, so
> `PlayerCareerStates.SelectAvailable` was split into `MarkUnavailable` (a removal set) + the new
> `AvailabilityComposition` (one intersection, one back-fill), letting a second contributor compose
> with #41's back-fill without either racing the other (`ERR-044-003`; recorded, not fixed, that a
> suspended player is reinstatable in extremis by owner decision — a stricter reinstatement tier, not
> a refused fixture). `ERR-044-001` (the fourth instance of the ERR-029-005/ERR-041-009 magic-header
> class) also closed a T2 verification failure: Appendix C's "slot 19" worked example was an on-pitch
> index under `SQUAD_SIZE = 22`, not the synthetic post-substitution `Incoming` id range `[22, 36)`,
> so every post-substitution card would have been misattributed as written. The occupancy seed now
> comes OUT of the engine (`MatchEngine.PlayerIdsByAgentId`) rather than a second `LineupSelector`
> walk in `season-save`. **Tests:** `Discipline.Tests` 81/81 (four mutants killed);
> `SeasonLoopDisciplineTests` +14 wiring locks, including a real 90-minute engine fixture pairing
> observer-neutrality with a positive control. **Measured, same session:** engine discipline fouls
> 35.0 / yellows 5.0 / reds 1.00 per 90 (`FoulRateDiagnosticTests`, 6 seeds × 54 000 ticks) against
> the July-26 record of 21.0 / 3.0 / 1.0 and football's ~22 / ~3.5 / ~0.25 — corrected in the OPEN
> ISSUES foul/card entry, which had gone stale. **Recorded, not fixed:** in `ManagedThroughEngine`
> only the managed fixture runs the engine and only engine fixtures generate cards, so the managed
> club accrues roughly 20× the yellows and reds of every quick-simmed rival — #44 makes suspensions
> live for one club in twenty, and #44 T3 (the #30-owned quick-sim card synthesis) is the named
> answer. Full account: `docs/tracking/spec-error-log.md` v2.17, `docs/tracking/open-issues.md`.
> **GATE: RUN TO COMPLETION at `0fb3ff0`, August 13, 2026 — 33 suites, quarantine empty. `Discipline.Tests` 101/101; `SeasonSave.Tests` 431 passed / 0 failed / 3 known skips; `MatchEngine.Tests` 461 passed / **1 failed** / 11 skipped (59 m). The gate's exit status is FAILED (the quarantine is empty, so any failure fails it), and the single failure across the ENTIRE tree is `sim_match_engine_close_chance` — the inherited owner-held red that `close-chance-creation-design.md` §10.9 item 6 rules "hold red, do not rebaseline a third time". Those counts are IDENTICAL to the pre-#44 baseline this branch was cut from (W2, `MatchEngine.Tests` 461/1/11, same single failure), so **#44 adds no new failure**: the landing is gate-clean on its own terms and the tree is red for a reason that predates it and is an owner decision, not a defect.**

> **Last Updated (prior):** August 28, 2026 — **PROJECT ARCHITECTURE GOVERNANCE INTEGRATION PLAN v0.4; documentation only.** Agreed rollout/activation revision: A1 is now an asmdef-only first slice that produces ERR-020-002/003 evidence without Roslyn/schema/#19/#20-governance dependencies; those existing #20 defects close before the coordinated governance amendment, after which the objective asmdef subset may become a required status before A8. Compiler-backed Class-A reachability moves to A4; Class-B gate-firing remains runtime/domain-owned (W12-style) evidence consumed by governance rather than implemented there. Integration contracts gain orthogonal `activation_state = active | intentionally-disabled | pending-integration | unresolved`; intentional disablement requires a machine-resolvable disable anchor and cannot act as a suppression. KD-W1 becomes a machine tuning precondition, proposed as FR-TS-097. Certifying Roslyn extraction must be built from source at the governed checkout with pinned .NET SDK/compiler/config identity. Actual #19/#20 normative files, SPEC_INDEX, and file-manifest remain intentionally untouched at this draft stage; D1–D4 remain frozen; no code, tests, CI workflow, runtime behavior, save/schema version, gameplay constant, RNG stream/domain tag/draw site, or draw order changed.

> **Last Updated (prior):** August 28, 2026 — **PROJECT ARCHITECTURE GOVERNANCE INTEGRATION PLAN v0.3; documentation only.** End-to-end implementation review hardening of `docs/planning/project-architecture-governance-integration-plan.md`: replaces self-referential commit/tree freshness with material `subject_scope_digest` + provenance separation; makes property-history validation compare against a trusted prior registry; closes the A1/A4 plain-C# root bootstrap; freezes executable selector/identity/applicability/dependency-closure semantics at A2; requires compiler-backed C# discovery (including implicit type initialization) rather than a hand-written parser; defines stable component IDs and overload-safe selector history; derives proof-class dependency closure; records explicit execution/failure-injection/mutation truth; splits durable review runs from findings; and requires an `if: always()` architecture aggregator to consume owning-runner results and reject skipped/excluded/quarantined required proof. A0–A9, the Governance/#19/#20 authority split, and ERR-020-002/003 staging remain unchanged. Actual #19/#20 files remain untouched; D1–D4 remain frozen; no `.cs`, `.asmdef`, CI workflow, runtime behavior, save/schema version, gameplay constant, RNG stream/domain tag/draw site, or draw order changed.

> **Last Updated (prior):** August 28, 2026 — **PROJECT ARCHITECTURE GOVERNANCE INTEGRATION PLAN v0.2; documentation only.** Hostile-review hardening of `docs/planning/project-architecture-governance-integration-plan.md`: implementation now requires A0 Governance adoption, A1 read-only current-tree discovery, A2 schema freeze, and A3 coordinated #19/#20 dual reapproval before governance tooling or blocking enforcement; adds closed-world runtime-surface classification, deterministic applicability resolution, typed contract edges, complete proof/tree/inventory/config/tool binding, versioned finding migration, corrected property-vs-FR exception routing, exhaustive #19/#20 file/section amendment matrices, explicit required-status CI activation, finite baseline retirement, and A0–A9 sequencing. The approved #19/#20 specification files themselves were **not** modified in this commit; D1–D4 remain frozen. Modified only the integration plan plus tracking. No `.cs`, `.asmdef`, runtime behavior, save/schema version, gameplay constant, RNG stream/domain tag/draw site, or draw order changed.

> **Last Updated (prior):** August 27, 2026 — **PROJECT ARCHITECTURE GOVERNANCE INTEGRATION PLAN; documentation only.** New `docs/planning/project-architecture-governance-integration-plan.md` v0.1 maps governance v0.4 into Code Standards #20, Testing Strategy #19, the adversarial-review ledger, CI, agent workflows, architecture inventories/contracts, evidence invalidation, and staged enforcement. The frozen D1–D4 remediation supplement is explicitly excluded from implementation authority. Modified tracking: `docs/tracking/CHANGELOG.md`, `docs/tracking/file-manifest.md`. No `.cs`, `.asmdef`, runtime behavior, save/schema version, gameplay constant, RNG stream/domain tag/draw site, or draw order changed.

> **Last Updated (prior):** August 27, 2026 — **FOLLOW-UP: load-on-demand authority boundary clarified; documentation only.** The two expanded agent guides now identify themselves as snapshots/reference material, explicitly defer to the compact guides and owning sources, and no longer instruct agents to read the root guide "completely" or treat the expanded copy as a second authority. Modified: `docs/agent-guides/project-reference.md`, `docs/agent-guides/coding-reference.md`, `docs/tracking/CHANGELOG.md`, and `docs/tracking/file-manifest.md`. No runtime artifacts changed.

> **Last Updated (prior):** August 27, 2026 — **AGENT-CONTEXT REDUCTION; documentation only.** Root `CLAUDE.md` reduced from 41,741 to 4,330 bytes and `src/CLAUDE.md` from 48,041 to 4,955 bytes. Detailed content was preserved in new load-on-demand references: `docs/agent-guides/project-reference.md` and `docs/agent-guides/coding-reference.md`. The compact guides retain non-negotiable rules and explicitly route task-specific detail. Modified: `CLAUDE.md`, `src/CLAUDE.md`, `docs/tracking/CHANGELOG.md`, `docs/tracking/file-manifest.md`. No `.cs`, `.asmdef`, runtime behavior, schema, format version, RNG draw site, stream, or domain tag changed. Markdown and link checks only; no code gate required.

> **Last Updated (prior):** August 26, 2026 — **CHEAP-TIER AGENT DELEGATION: three Sonnet agent definitions under `.claude/agents/`, all three verified BY EXECUTION rather than written and assumed; tooling only — no code, no spec, no `src/` change, no gate run required (zero `.cs` / `.asmdef` / `tools/dotnet-ci` files touched).** **New (3):** `.claude/agents/gate-runner.md` (invokes the `dotnet-gate` skill and reports it verbatim; measurement only — no edits, no quarantine additions, no commits, and exactly three legitimate results: PASSED / FAILED / COULD NOT RUN, where green is the `── Gate PASSED ──` line and nothing else), `.claude/agents/orienteer.md` (invokes the account-level `orientation` skill; read-only; stops rather than improvising a substitute sequence, since a look-around returned under that name would be trusted as the checked one), `.claude/agents/doc-scribe.md` (applies doc edits already decided and drafted; no shell, no git, no `Write`, so it structurally cannot commit, and it refuses an intent rather than inferring one). **Modified (4):** `.claude/skills/landing-close-out/SKILL.md` (new `## Delegating the mechanical half` — the sync splits by JUDGMENT, not by document: which documents a landing touches, the changelog and OPEN ISSUES narrative, the determinism declaration, the blast-radius check and the gate line stay with the caller; version bumps, manifest rows, the `(prior)` relabel and a README status line delegate, with exact strings handed over and `git diff` read before committing — and the commit itself is never delegated), `.claude/README.md` (layout tree, new `## Model tiers — what runs cheap` table, verification-status table, constraint 1 corollary), `.claude/skills/README.md` (August 26 addendum), `file-manifest.md`. **Deliberately NOT in `adversarial-review`** — it only points at `landing-close-out`, and restating the sync there is the exact composability defect the August 25 audit had just removed from three skills. **Three mechanical facts established BY EXECUTION, not assumed: (1)** `Skill` is grantable to a subagent and account-level skills resolve there — `orienteer` called it with `orientation` and it loaded; **(2)** a registered agent definition is **snapshotted and does NOT hot-reload** — an agent dispatched after an edit quoted the PRE-edit bullet verbatim and reported the new phrases absent from its own instructions, so **an edit to any `.claude/agents/*.md` cannot be validated in the session that made it** (two "failed" fixes were misdiagnosed this way before the behaviour was found; both were then re-tested in a fresh session and both hold); **(3)** the per-spawn context cost is ~26–80 K tokens, not the ~449 K the README feared. **A stale figure corrected in passing:** `.claude/README.md`'s "root `CLAUDE.md` is ~395 KB" was **exact when written** (397,972 bytes on July 31, 2026, that file's own creation date) and is now **41.5 KB** after the August 22 `landing-history.md` split — a ~9.6x change in the number that prices every subagent spawn, and the old one would have made cheap-tier dispatch look unaffordable. **The `gate-runner` test surfaced a RED TREE that is not this branch's:** `sim_match_engine_close_chance` is confirmed red on `main` itself (CI run 476 on `2092c8a`: `MatchEngine.Tests` Failed 1 / Passed 472 / Skipped 11 / Total 484, identical to the local run) and is the owner-held RED by decision of August 11. A **second** failure — `GrowthProjection_DeclineIsUnbounded_ANeverRemovedVeteranReachesEveryAttributeAtMinimum` (`GrowthProjectionTests.cs:334`, expected 0 but was −1) — is **recorded nowhere**; it was ADDED by `1a34ef4`, whose own entry above claims `PlayerProgression.Tests 149/0/0`, against a suite now totalling 152 with 1 failing. Whether that claim was wrong when written or a later merge broke it is **NOT established here**. **It IS established that the failure is not this branch's:** run 476 on `main` fails the same test with the same counts (`PlayerProgression.Tests` Failed 1 / Passed 151 / Total 152), as does PR #334's own CI. *(⚠️ CORRECTED at commit time: this entry as first drafted said the CI log was "not evidence either way" because run 476's log showed no `PlayerProgression` lines. That was an artefact of retrieving only the log **tail** — ~133 K of 436 K chars, with `MatchEngine.Tests`' 35–45 minute run filling the whole window. Pulling the full log and grepping it found both failures on both runs. **A tail is not a search**, and the same mistake would hide any suite that finishes early.)* **Flagged for an owner, deliberately not diagnosed** — it is outside the scope of the branch that found it. This landing's own tracking entries were added after a review bot on PR #334 correctly flagged their absence; the precedent reasoning that omitted them (that `.claude/` work stays inside `.claude/`) was drawn from PR #333, which was itself the deviation. Prior entry below.)

> **Last Updated (prior):** August 24, 2026 — **ADVERSARIAL REVIEW ROUND 2 — RAN PARTIALLY, and the partiality is the first thing round 3 must fix: TWO OF FOUR REVIEW LENSES NEVER RAN.** Four fresh Opus reviewers were dispatched over the ENTIRE current state (not a diff) — arithmetic, mutation, spec-governance, architecture. The governance and architecture lenses returned; **the arithmetic and mutation lenses both died on an API session limit before producing a finding.** That matters specifically: those two are the lenses that found round 1's real code defects (the `AgeRiskFor` sign inversion, the overflow-defeated ramp guard), so round 2's silence on arithmetic is ABSENCE OF EVIDENCE, not evidence of absence. **Round 2 therefore does NOT close the loop** — the termination test is a fresh FULL pass returning Low or nothing, and only half a pass ran. **Findings: 3 High, 15 Medium, 7 Low** (one dedupe — both surviving lenses independently flagged `ClassifyAgeBand`). **all three Highs fixed, and 16 of the 22 Medium/Low fixed across three fixer groups; 4 deliberately DEFERRED (D1–D4) and **`M10` NOT FIXED** — verified by grep at commit time, the config-unbound premise still stands at 2 sites in `spec-error-log.md`. *(⚠️ COUNT CORRECTED at commit time: this entry as first drafted said "13 of the M/L set fixed", which was FALSE — Group A's ten findings had not been dispatched at all when it was written, and the true figure at that moment was 6. Caught by reading the change set against the claim rather than trusting the summary. 16 + 4 + 1 = 21 against a 22 total, so **the M/L tally is still not fully reconciled and round 3 must re-derive it from the ledger** — no reconciled number is asserted here.)*** **The two governance Highs are the same defect class round 1 was fixing, recurring inside the fix:** `spec-error-log.md` — the authoritative record — still published, in two entries, the exact claims that later entries in the same file were filed to retract. `ERR-041-020` still asserted the age term sits "before the mitigation … so robustness discriminates it" while `ERR-041-021` forty lines below records that as arithmetically false (the mitigation is SUBTRACTED; addition commutes; a 956,480-input probe returns byte-identical output). `ERR-028-021` still published "the offsets sum to exactly 0 … **Locked.**" — the precise claim `ERR-028-022` retracted after the league was measured retiring ~2 months early. Round 1's sweep reached the specs and the code and stopped one paragraph short inside the log. **Both Error Index rows carried it too and were fixed with the bodies** — a body without its Index row is half a fix, which this project has shipped before. **The residual-carrier sweep that followed is this correction's SIXTH widening**, and one of the surviving carriers was the review document's own "FIXED" note for the very finding; every live carrier is now annotated in place (`MedicalStepTests.cs`, `football-judgment-proxy-review.md`, `CLAUDE.md` ×2, `open-issues.md`). The two `CHANGELOG.md` carriers are **deliberately NOT edited** — that file's own rule is that the chain is the record and historical entries are not edited; they are corrected by this entry instead. **The architecture High was a real structural defect:** the construction-day credit rule had TWO implementations (`RegenGenerator.BandStepFor` and inlined in `ProgressionEngine.SeedLifecycle`), with `BandStepFor`'s own doc declaring itself the owner of a rule it did not own — and **#28 had already paid for this exact duplication twice** (`ERR-028-018` fixed the seed site and missed the regen site; `ERR-028-020` then had to visit both). Collapsed to one owner, `AbilityModel.ConstructionDayCredit`; `BandStepFor` deleted, taking round 1's private→internal widening with it. **The two implementations were verified identical first** (probe: 0 mismatches over ages 0..200 and the `int` edges), so no larger finding. **Mutation evidence, honestly stated:** a PURE REVERT of the collapse passes 149/0 — it is behaviour-identical by construction and no behaviour test can detect it being undone; what the three new locks catch is the DIVERGENCE class, and re-introducing the historical one-site-credits defect now fails 2 tests where nothing previously compared the sites at all. **A claim of this session's own is CORRECTED here:** round 1's record described the new ramp oracle in `SeasonLoopProgressionTests` as "an INDEPENDENT reimplementation". It is not — it is a line-for-line transcription of `GrowthPhaseDays`/`DeclinePhaseDays` (same branch order, same shifted variables). It still detects the mutation it was built for, so the fix stands, but the word was false and is withdrawn. **Also fixed:** the `AgeBand` enum docs still defined the bands by the retired hard predicates in the same file round 1 was correcting; `AssemblyInfo.cs`'s FR-CS-015 rationale was false as applied (the same copied-rationale class as round 1's own `config-unbound-premise-false-28`); `MedicalStep`'s test affordance now follows the house `TestOnly_` convention and `AgeRiskFor(int)` is `internal` to match its twin term; `SPEC_INDEX`'s ordinal-92 promotion claim is re-anchored — and a full re-grep of all 251 `SubsystemOrdinals` sites in `docs/` established SPEC_INDEX was **the only surviving misstatement in the tree**, closing a sweep widened five times before. **DEFERRED, recorded, NOT silently dropped — all four widen the blast radius past the surface under review, and this project does not widen a landing unilaterally: (D1)** hoist the SplitMix64 finalizer, duplicated verbatim in **11 production files**, into `deterministic-sim` — the right answer and byte-identical by construction, but it touches `match-engine` and proving no digest or draw-order move needs a whole-tree gate including `MatchEngine.Tests` that this round cannot run; **(D2)** give the nine per-call catalogue-invariant guards a boot-time owner via `GameplayConfigHolder.Bind` — a redesign affecting every `[GT]` catalogue; **(D3)** hoist the duplicated global-id predicate to `player-database` and back-prop #27, which lands in a third spec's territory (`ERR-027-004` already open there); **(D4)** move the ramp oracle to the assembly that owns the formula and pin oracle == implementation (the false "independent" claim is corrected now; the move is round 3's). **Recorded, not fixed:** no §5 test ids allocated for H3's three new cases (the case they replace had none either); the regen CALL SITE still cannot be driven at a ramp age through the public API, so a mutation of its argument would go uncaught; and `spec-error-log.md`'s `ERR-028-020` Files Affected row now has a second stale item on this rule. **Verification at final HEAD, run rather than asserted:** whole-tree build **0 errors / 0 warnings**; `PlayerProgression.Tests` **149/0/0** (was 147); `InjuriesMedical.Tests` **76/0/0**; `SeasonSave.Tests` **402 passed / 3 known skips**; `TrainingSystem.Tests` **52/0/0**; `recurring-defect-lint.py` **0 ERROR** on a quiet tree. **NOT a whole-tree gate run** — `MatchEngine.Tests` was not executed and no verdict is claimed; nothing in this round runs on a match tick. **The football-judgment queue is untouched: still 29 open, 21 workable, batch 2 (keeper) next.**
>
> **Last Updated (prior):** August 23, 2026, latest same day — **ADVERSARIAL REVIEW ROUND 1 over the batch-1 landing — CLOSED August 23, 2026 at owner instruction: 7 High, 9 Medium, 15 Low, all 31 fixed across four commits (`9e41537`, `78b57e2`, `eb23c1c`, `9fca357`). CLOSED IS NOT CONVERGED, and the distinction is the point:** the `/adversarial-review` loop terminates only when a FRESH FULL re-review of the entire current state returns Low findings or none. No such pass ran. Round 1's fixes are verified individually — every behavioural one by actual mutation, reverted and re-run — but nothing has re-reviewed the tree those fixes produced. Recorded here so no later session reads "round 1 closed" as "this surface converged". **Why the round mattered: all seven Highs were claims that had ALREADY been verified once at the batch-1 landing.** Three worth carrying forward. **(1) A real arithmetic defect in the landing's headline fix** — `GameReadingOffsetDays` averaged three attributes with a floored `int` mean, so the offsets did NOT sum to zero over the population as the P5-exactness claim stated; the league retired ~2 months early. Fixed by carrying the undivided sum into the numerator, which sums to exactly 0 AND reproduces every diagonal value bit-for-bit, so nothing needed rebaselining. **(2) Both new production wirings were unlocked** — reverting them left 539 and 405 tests green respectively. **(3) The gate line itself was wrong** (see the entry below). **Two further real arithmetic defects came out of the Medium/Low tier, both the same shape — an `int` computation widened one step too late:** `AgeRiskFor` widened its product but not its subtraction, so at an extreme pivot the age term **changed sign** (measured +1800 with the fix, −1800 without); and `RampHalfWidthDays`'s disjointness guard was **defeated by overflow inside its own predicate**, returning `AccruedBandPoints` = 2,451,094 where it should have thrown. **The round's dominant pattern, and the one to remember: every one of the five new fail-loud config guards was deletable with the whole suite green, and two could never fire at all** — they read catalogue statics directly under a gate that binds no config. That is AR pass 14's own recorded lesson ("a guard on a branch nothing can execute ships green precisely because nothing can fire it") recurring inside the landing that cites its posture. All five now have isolating cases driven through parameterised `internal` overloads; deleting all four `AbilityModel` guards at once now fails exactly six cases (141/6) where it previously left 134/0. **One claim was WITHDRAWN rather than locked, deliberately:** `AgeRiskFor`'s position relative to the mitigation is an arithmetic identity — a 956,480-input checksum probe against the built assemblies returns byte-identical output for the moved term — so no test can distinguish it. The claim was retracted and the position restated as what is actually load-bearing (before the `OccurrenceRiskMillMult` scaling, before the clamp), with those two locked. Inventing a lock that passes vacuously is precisely what the `ERR-008-021`/`-022` chain did three times. **Three ERR ids filed and resolved:** `ERR-028-022` (the floored-mean anti-symmetry break), `ERR-028-023`, `ERR-041-021`. **Recorded, NOT fixed:** a second date inversion in `football-judgment-proxy-review.md`'s Updated chain that no finding diagnosed (an August 6 entry below August 5 ones), left rather than restructured on one reader's interpretation of ambiguous same-day timestamps; and `injuries-medical/section-9-approval-checklist.md`'s R-02 / §9.3.1 rows still saying "27 FRs", outside the three sites its finding named — the grep-boundary class this project has hit five times. **Process, recorded because it nearly cost work:** three agents shared one working tree and one ran `git stash` to measure a lint baseline; `outline.md` came back PARTIALLY restored and two edits were lost and redone. The dropped stash is pinned at tag `rescue/dropped-stash-4255331`. A concurrent-agent pass must not stash a shared tree. **Verification at final HEAD, run rather than asserted:** whole-tree build **0 errors / 0 warnings**; `PlayerProgression.Tests` **147/0/0**; `InjuriesMedical.Tests` **76/0/0**; `SeasonSave.Tests` **402 passed / 3 known skips** — the locked season-injury bands did not move; `TrainingSystem.Tests` **52/0/0**; `recurring-defect-lint.py` **0 ERROR**. **NOT a whole-tree gate run** — `MatchEngine.Tests` was not executed on this tree and no verdict is claimed for it; nothing in this round runs on a match tick. **The football-judgment queue is untouched by this round: still 29 open, 21 workable, batch 2 (keeper) next per §6.3.1.**
>
> **Last Updated (prior):** August 23, 2026 (**Documentation-only: the batch-1 landing's OVERSTATED GATE CLAIM
> corrected at all seven sites, and the three held ERR bodies landed in `spec-error-log.md` (v2.22 →
> v2.23). No `.cs` file touched, no `[GT]`, no format version, no draw.** **(1) The gate claim.** As
> published, the batch-1 entry read "31 of 32 suites fully green … quarantine empty", and both halves
> were false. The tree has **33** test suites — `ls -d src/*/[Tt]ests/*.asmdef` returns 33 and the gate
> log carries 33 suite result lines; the original count missed every suite whose folder is capitalised
> `Tests/`, which is 17 of them — so the figure is **32 of 33**. And "quarantine empty" implied a ledger
> report the run never printed: `run-gate.sh` runs under `set -euo pipefail`, so it exited non-zero on
> the inherited owner-held-red `sim_match_engine_close_chance` and never reached its quarantine-report
> section or a `Gate PASSED` line — `grep -c 'Quarantine\|Gate PASSED'` on that log returns **0**. The
> ledger IS empty, but **by inspection of `tools/dotnet-ci/known-failures.txt`** (zero non-comment
> lines), not because the gate said so. The restated form says the test sweep ran in full at final HEAD,
> names the inspection as the source of the empty ledger, and records the formal result as **FAIL on the
> held-red band — no new failure, no band rebaselined** — rather than rounding it up to PASS. Corrected
> at `CLAUDE.md`, `open-issues.md`, `spec-error-log.md`, `file-manifest.md`,
> `football-judgment-proxy-review.md`, this file and `CHANGELOG-src.md`, each with the superseded wording
> quoted in place rather than deleted. **The August 12 W2 entry further down this chain also says "31 of
> 32" and is deliberately left alone** — it is a different landing's record, and re-deriving its suite
> count is not this pass's work. **(2) The three held ERR bodies.** `ERR-028-022`, `ERR-028-023` and
> `ERR-041-021` were already cited in shipped spec text and code comments while `spec-error-log.md` held
> nothing behind them — the missing-body class that log recorded at v2.17 for `ERR-008-023`. All three
> now have both an Error Index row and a `##` body: the floored-mean retirement offset that falsified
> `ERR-028-021`'s P5 claim (and the two behaviour changes that were locked by nothing at all); the
> seed-credit MUST that still mandated the band step `ERR-028-020` retired; and `ERR-041-020`'s
> arithmetically vacuous "before the mitigation, so robustness discriminates it" position claim, whose
> lock passed against all three mutants it named. Ids re-verified free before writing. `ERR-041-020`'s
> own "Magnitude, first-guess" epidemiology sentence is corrected in place with them — the shipped
> monotone age term follows E-4 above the pivot and **inverts** its Strong-rated 16–20 arm below it, with
> `ERR-041-013` narrowed to that residual. **No gate run in this pass and none claimed:** nothing
> compilable changed, and `python3 tools/recurring-defect-lint.py --repo .` reports **0 ERROR**.)

> **Last Updated (prior):** August 22, 2026, latest same day (**BATCH 1 of the football-judgment proxy review's
> workable 24 LANDED IN FULL — three findings, three ERRs filed AND resolved in the same commit, spec +
> code together, whole-tree gate run.** `ERR-028-020`, `ERR-028-021`, `ERR-041-020`; the review's counts
> move for the first time since August 5 — **34 recorded, 5 fixed, 29 open, workable queue 24 → 21**.
> **ERR-028-020 (#28 §3.1, new §3.1.3):** the daily growth rate was `DailyPoints(ClassifyAgeBand(ageYears), …)`
> — three constants selected by a hard step at an exact integer age, so a player developed at the full
> rate on the last day of his 23rd year and at exactly zero on the first day of his 24th, with the mirror
> discontinuity at `DECLINE_AGE`; the "deep" `curveEnabled` tier called the identical classifier, and
> §1.3's promised "curves keyed to age" existed nowhere in the spec. Now a centred linear ramp of
> half-width `AGE_BAND_RAMP_HALF_WIDTH_YEARS` at each edge. **The implementation choice is the
> load-bearing one:** the accrual is the first difference of an exact integer CUMULATIVE, not a per-day
> rate — `GrowthCursor` is integer fixed-point at one-unit-per-full-growth-day, so a rate has nothing
> between 0 and 1 to return, and rescaling to milli-points (the obvious alternative) would have forced
> `PROGRESSION_SAVE_FORMAT_VERSION` 1 → 2 and a refusal of every existing save. The difference form
> keeps the per-day step in `{0, ±1}` while its DENSITY follows the continuous curve, so **no format
> moves at all**. **P5 came out exact rather than fitted:** a centred ramp has the same integral as the
> step it replaces for EVERY half-width, so no growth-rate recalibration is owed and ERR-028-018's
> no-residue traversal invariant survives by construction. Half-width 0 reproduces the §4.3 step
> byte-for-byte, exercised per-day across a 45-year sweep through a parameterised overload rather than
> asserted. `ClassifyAgeBand` demoted to a READ of the curve (two of its answers invert, which is the
> fix). **ERR-028-021 (#28 §3.4):** retirement was `rec.Age >= RETIREMENT_AGE` — one integer age for the
> whole league, so goalkeepers retired on a forward's clock and one calendar day was the difference
> between a career continuing and ending, for everyone at once. Now a per-player `RetirementAgeDays`
> compared in days: baseline + goalkeeper allowance + a full-range anti-symmetric offset over the
> Anticipation/Positioning/Composure mean. **The P3 decision is the part worth carrying forward** —
> robustness was the obvious input and is deliberately NOT used, because #29 and #41 already price
> Strength/Stamina/Balance twice over (`ERR-041-003`), so a third read would be that recorded defect a
> third time; recorded in §3.4 as a ledger entry. Zero dials reproduce the retired comparison
> identically, and the offsets sum to exactly 0 over a uniform attribute population, so the league's
> retirement RATE is unchanged and only who-retires-when moves. Still draw-free. **ERR-041-020 (#41
> §3.4):** the risk assembly presented as multi-factor and read player age nowhere — not in the formula,
> not in the signature, not in §2, so nothing in the spec would have caught it either. `AgeRiskFor` now
> sits inside the sum BEFORE the mitigation (normative, like `BASELINE_DAILY_RISK`, so robustness
> discriminates it), linear with no threshold anywhere. **P5 measured rather than argued:** the pivot is
> #27's bootstrap mean age (26 over `[17, 35]`), so `SeasonInjuryRealismTests`' league (717–816/season),
> starter (2.08), reserve (1.12) and squad-unavailability (9.4%) bands all held unmoved, and all 26
> pre-existing `AssembleRiskScore` expectations stay exact by passing the pivot age rather than being
> rebaselined. First-guess magnitude: a 34-year-old carries ≈1.37× a 20-year-old's daily risk. New
> **FR-MD-025a** carries the requirement. **Two of the three findings were mandated by their own spec's
> §2 requirements** — FR-PG-007/KD-8 made the band step the required curve-off behaviour, FR-PG-013
> required retirement "hard at `RETIREMENT_AGE`" — so both FRs are revised in place with the superseded
> requirement annotated; that is pattern (d) reaching past §3 into the FR table, and batches 5 and 6
> should expect it. **Across all three: no RNG draw, no stream registration, no domain tag, no
> `SNAPSHOT_SCHEMA_VERSION`, no `*_FORMAT_VERSION`, no draw-order change.** Suites at landing:
> `PlayerProgression.Tests` 127 → **134**, `InjuriesMedical.Tests` 70 → **74**, `SeasonSave.Tests`
> **402 passed / 3 known skips**, `TrainingSystem.Tests` 52/52. Four pre-existing locks were rebaselined
> onto the fixes and each rebaseline is annotated in the test itself with what the old assertion was
> asserting — three of them were asserting the cliff.

> **Gate: the test sweep ran in full at final HEAD; the formal result is the inherited FAIL, not a
> PASS.** Build 0 errors; **32 of 33 suites fully green** (`PlayerProgression.Tests` 134/0,
> `InjuriesMedical.Tests` 74/0,
> `SeasonSave.Tests` 402/0 with its 3 known skips, `TrainingSystem.Tests` 52/0, every other suite 0
> failed); `MatchEngine.Tests` **461 passed / 1 failed / 11 skipped**. The quarantine ledger is **empty
> by inspection of `tools/dotnet-ci/known-failures.txt`** — not because the gate reported it:
> `run-gate.sh` runs under `set -euo pipefail`, so it exited non-zero on the blocking phase and never
> reached its quarantine-report section or a `Gate PASSED` line, and **no formal verdict was ever
> printed**. Recorded as FAIL on the inherited owner-held-red band rather than rounded up to PASS.
> *(⚠️ CORRECTED August 23, 2026 — as first published this entry read "31 of 32 suites fully green …
> quarantine empty". The suite count was one low: `ls -d src/*/[Tt]ests/*.asmdef` returns **33** and the
> gate log carries 33 suite result lines, the original count having missed the suites whose folder is
> capitalised `Tests/`. And "quarantine empty" implied a ledger report the run never printed —
> `grep -c 'Quarantine\|Gate PASSED'` returns **0** on that log. The ledger IS empty, but by inspection
> of the file, not by the gate's own report. Annotated in place, never silently replaced.)* The single
> failure is `sim_match_engine_close_chance` — **owner-held RED since August 11 and failing at
> baseline** (`close-chance-creation-design.md` §10.9 item 6; §6.3.1's constraint 2 names it as the
> detector any batch now lands against). Its counts and both failing predicates come back **identical
> to the recorded baseline**: `MatchEngine.Tests` 461/1/11 exactly as recorded at the W2 landing, mean
> cosine **−0.165** against the −0.16 bound and goalward share **0.407** against 0.42 — the exact
> figures the C1 / `ERR-012-011` record carries. Nothing in this landing runs on a match tick (#28 and
> #41 are world-day subsystems; the one #30 call site is slot 4 of the day loop), so identical figures
> are the predicted result and the evidence for it, not a coincidence to be explained away. **No new
> failure, and no band was rebaselined.** An earlier run of this gate was killed and restarted
> deliberately rather than reported: its test phase had begun before the `RegenGenerator` fix and the
> `MAX_DERIVABLE_AGE_YEARS` correction below, so its verdict would not have covered the tree it was
> reported against — the invalidated-gate class this project recorded at AR pass 9.)
> 
> Prior entry below.
> **Last Updated (prior):** August 22, 2026, latest same day (**`ERR-016-011`: the replay lifecycle now
> re-derives a loaded record's OWN digest — closing the one item `ERR-016-010` had recorded as not
> fixed, and leaving nothing open on the replay-identity surface.**) **1. The digest existed and was
> correct; nothing asked it anything.** §3.2.3 defines a snapshot digest that covers the payload,
> `Encode` has computed it correctly since May, and the on-disk record has carried it — but the
> §4.2.2 lifecycle only ever validated the chain LINK (`prevSnapshotDigest`). `currentSnapshotDigest`
> was written, stored, loaded and **never recomputed**, so a record whose stored digest had been
> altered loaded clean — and so did one whose **payload** had been altered. That second case is the
> one that matters: the payload IS the Tier A/B state step 5 rehydrates, and every other check on the
> path (magic, versions, fingerprint, chain link, record trailer) validates metadata around it.
> **2. Found by a failing assertion, not by reading.** The `ERR-016-010` tampered-digest lock was
> first drafted asserting that flipping a bit in the stored digest would be refused. It was not; the
> test failed, and the reason was structural rather than incidental. **3. The fix, and why it is a
> split rather than a ninth step.** §4.2.2 step 4 becomes **4a** (chain link — is this the record that
> should follow the last one?) and **4b** (re-derive this record's own §3.2.3 digest — are these bytes
> the record they claim to be?), with a new `ERR_DS_SNAPSHOT_DIGEST_MISMATCH` (0x160F) and
> `EC-016-016`. **The lifecycle stays 8 steps**: FR-DS-012 binds an 8-step lifecycle and §4.6.2
> diagrams one, and spec renumbering cascades are this file's own KNOWN HAZARD, so a split beats a
> renumbering for an identical outcome. 4b runs **before step 5**, stated normatively — a lifecycle
> that verifies after rehydrating has already applied the bytes it was about to reject.
> **4. One preimage, one implementation.** `ComputeSnapshotDigest` is extracted as the single owner
> of the §3.2.3 preimage, shared by `Encode` and `ValidateCurrentDigest`. Not tidiness: two
> hand-written derivations of one preimage that agree only by inspection is the `ERR-010-002` class,
> and here it fails badly in **both** directions — a verifier that drifts from the recorder rejects
> every honest record, one that omits a field silently accepts tampering in it. §4.2.2 now says so.
> **5. Switching the check on immediately failed the suite's own happy-path test.**
> `ReplayEngine_PrepareReplay_WellFormedSnapshot_ReturnsZero` had never called `Encode`, so its
> "well-formed" record carried an all-zero `currentSnapshotDigest` — a value its own comment called
> "a valid digest value" and which no recording produces. It had been written that way to dodge a
> real conflation (encoding on the same codec advances that codec's chain authority and then breaks
> step 4a). The fix is the distinction the comment itself drew: **two codec instances**, a recording
> codec that encodes and a fresh replay codec that validates. The fixture had been asserting the happy
> path of a record that cannot exist. **6. Verification.** Four behaviours locked across three new
> tests and one strengthened existing test: an altered payload refused at 4b; an altered stored digest
> refused at 4b **while the loader still succeeds** — the pair marks where the boundary sits, storage
> reports the bytes it found and replay decides whether they are the record they claim to be; an
> honest round-tripped record **passing** 4b; and `ValidateCurrentDigest` not advancing the chain.
> **Two mutants executed, killing in both directions:** deleting 4b fails the two tamper locks;
> making the verifier's preimage differ slightly from the recorder's fails four honest-record locks.
> **7. No determinism impact.** No `DETERMINISM_DIGEST_VERSION` bump — the digest being checked is
> the one §3.2.3 already defined; what is new is that something reads it. No preimage, field width,
> hash-input rule, schema version, file-format version, RNG stream or golden vector moved.
> **GATE: whole tree, 33 test assemblies, 0 errors and the same 5 warnings as baseline, quarantine
> empty; `DeterministicSim.Tests` 86/0/1 (from 83/0/1), `MatchEngine.Tests` 472/1/11.** Diffed against
> the previous commit's run: **exactly one suite changed**, by exactly this landing's locks, every
> other suite byte-identical; the one failure is the inherited owner-held-red
> `sim_match_engine_close_chance`. **Nothing remains open on this surface** — the archived
> `EnvironmentFingerprint.floatModelHash` entry is annotated to say so. **Modified:**
> `src/deterministic-sim/SnapshotCodec.cs`, `ReplayEngine.cs`, `DeterministicSimConstants.cs`,
> `tests/DeterministicSimTests.cs`, `docs/specs/deterministic-sim/section-3.md` (v1.0.18),
> `section-4.md` (v1.2), `spec-error-log.md` (v2.21), `open-issues-resolved.md`, `file-manifest.md`,
> `CHANGELOG.md`, `CHANGELOG-src.md`.

> **Last Updated (prior):** August 22, 2026, latest same day (**Both `SaveManager` gaps CLOSED as
> `ERR-016-010`, and two OPEN ISSUES entries archived — the index and the record now agree for the
> first time in this file's history.**) **1. The gap was bigger than its title.** The
> `EnvironmentFingerprint.floatModelHash` entry had carried "`SaveManager` still writes
> `Fingerprint = null`" as a remainder for a month, alongside the `buildHash` sibling closed earlier
> the same day. Working the fix showed why the field had nowhere to go: **#16 §3.9.2 already
> specifies a normative on-disk record layout, and the implementation contradicted it in four
> respects at once** — no `environmentFingerprint` (which FR-DS-010 requires and §3.9.2 already
> listed), no `recordTrailer` (`grep -rn recordTrailer src/` returned zero files), a
> `currentSnapshotDigest` stored inside the header block instead of after the payload, and no format
> identifier of any kind. So this was never a missing field; it was a normative section describing no
> artifact. **The consequence that mattered:** §4.2.2 step 3 validates the fingerprint, but a disk
> load always produced `null`, so `ReplayEngine` could only ever fail closed — a step that can only
> refuse is indistinguishable from a step that is not there, and it had carried an `AR fix M-3`
> comment naming that as the normal case since June. **2. The fix, and the version that did NOT
> move.** §3.9.2 is revised to the five-section record now emitted and consumed; new **§3.9.2.1** pins
> record identity (**magic-led** — `SNAPSHOT_FILE_MAGIC` checked before any later field is
> interpreted, the ERR-029-005 "a format version is not a format identifier" rule) and separates the
> three versions that govern a record, with the reason attached. `SaveManager` replaces the fixed
> 87-byte header codec with `EncodeRecord`/`DecodeRecord`, bounds every read through
> `SaveBlobFramingHelpers.Require`, and **throws** on a malformed header instead of reporting it as
> `ERR_DS_STORAGE_ATOMICITY` — a return code from that method means the storage layer failed, and
> mapping a caller's defect onto it sends the reader to the disk. **`SNAPSHOT_SCHEMA_VERSION` is
> deliberately unmoved:** it rides in the §3.2.3 digest preimage, so bumping it moves every snapshot
> digest and invalidates the July-19-certified golden vectors. It versions the authoritative STATE
> shape; identity metadata in the file frame is not that. The frame carries its own version instead —
> the split `MATCH_SAVE_FORMAT_VERSION` already draws. **3. A cost estimate this landing corrects.**
> The entry written one commit earlier priced closing these two gaps as "the
> `SNAPSHOT_SCHEMA_VERSION` bump the digest-preimage exclusion deliberately declines to spend", at a
> golden-vector recertification on a host this project cannot currently reach. **That was wrong, and
> the correction is cheaper rather than dearer:** the right instrument was a file-frame version,
> already precedented in this tree. No digest moved, no golden vector was touched, no recertification
> is owed. The estimate stands where it was written — it was the honest reading at the time — and is
> corrected in `ERR-016-010` and at the archived entry. **4. Verification, and one lock that only
> exists because a mutant survived.** The three save/load `Assert.Ignore` stubs were **activated, not
> deleted**: their premise ("activate when Stage 1 CI infrastructure supports file I/O in EditMode
> tests") was stale, since the gate runs plain NUnit on net8.0 and the sibling `MatchSaveManagerTests`
> had been doing real file I/O on it for a month. `DeterministicSimSaveLoadTests` goes from **3 ignored
> stubs to 12 executed locks + 1 genuinely-deferred skip** (`SaveAtomicMidTick`, an API that does not
> exist). Three mutants executed: never writing the fingerprint fails three tests; accepting an empty
> build hash fails exactly one; deleting the record-trailer comparison fails exactly one — **and that
> last lock exists only because the first run of that mutant survived.** The padded-record test had
> been written as the trailer's lock; the mutant passed it; a test only the trailer can fail (a
> corrupt trailer value at unchanged file length) was written in response. That is the #29/#41 review
> loop's "which fixes have a test that fails if the fix is reverted?" applied to this landing and
> answered honestly. **5. RECORDED, not fixed — a third defect on the same surface.** Nothing in the
> §4.2.2 lifecycle recomputes `currentSnapshotDigest` from the payload it just read: step 4 validates
> the chain LINK, and the current digest is stored, loaded and never re-derived, so a record whose
> stored digest or payload was altered loads clean. Distinct from the two closed here, outside what
> closing them requires, and pinned by an explicit assertion so the day someone adds the recomputation
> the test fails and says why. **6. #29 Training / #41 Injuries & Medical — verified resolved and
> archived.** This is the discrepancy the `landing-history.md` split had recorded and left for an
> owner call. Checked against the code rather than taken on the bullet's word: `SeasonLoop`
> slot 1 is **LIVE** (`_progression.AdvanceDay(day, in growth)`), the #41 occurrence dial is **ARMED**
> (`PlayerCareerStates.InjuryOccurrenceEnabled`), and every chain `ERR-` id reads resolved. Filed
> straight to `open-issues-resolved.md`, since it never had an `open-issues.md` entry to move. **Three
> stale status markers were corrected in the same check**, and they are why it needed checking:
> `ERR-029-006`, `ERR-041-010` and `ERR-041-001` each **led with `◑` while their own cell text already
> recorded `✅`**. The narratives were not edited; each row now leads with the verdict its own body had
> been carrying. A row whose marker disagrees with its text is exactly how a closed item goes on
> reading open. Two `CLAUDE.md` assembly-map rows that still said "slot 1 stays a null seam" and "the
> occurrence dial ships OFF" were corrected with them. **7. Counts.** `open-issues.md` **15 active**,
> archive **46**, and root `CLAUDE.md`'s index **15 bullets** — re-derived by direct count, and
> **agreeing for the first time in the record**. **GATE: whole tree, 33 test assemblies, 0 errors and
> the same 5 warnings as baseline, quarantine empty; `DeterministicSim.Tests` 83/0/1 (from 72/0/4 —
> +11 executed locks, and three ignores retired), `MatchEngine.Tests` 472/1/11, byte-identical to the run one commit earlier.** The one failure is the
> inherited owner-held-red `sim_match_engine_close_chance`. **Modified:**
> `src/deterministic-sim/SaveManager.cs`, `DeterministicSimConstants.cs`, `ReplayEngine.cs`,
> `tests/DeterministicSimTests.cs`, `docs/specs/deterministic-sim/section-3.md` (v1.0.17),
> `spec-error-log.md` (v2.20), `open-issues.md`, `open-issues-resolved.md`, `CLAUDE.md`,
> `landing-history.md`, `file-manifest.md`, `CHANGELOG.md`, `CHANGELOG-src.md`.

> **Last Updated (prior):** August 22, 2026, latest same day (**`ERR-016-009`'s `buildHash` half CLOSED —
> spec + code, same commit — and the six long OPEN ISSUES landing narratives moved verbatim out of
> `CLAUDE.md`.**) **1. What build identity IS.** The previous entry recorded `buildHash` as
> deliberately not fixed because "what constitutes build identity (assembly MVIDs? a CI-stamped
> commit? the `.asmdef` closure?) is a decision, not an implementation detail." The decision is made:
> **SHA-256 over the Module Version IDs of a DECLARED authoritative assembly closure.** A **CI-stamped
> commit** was rejected because it identifies *source*, not binary — a dirty working tree, a different
> compiler and a different target framework all produce different binaries under one commit, and the
> developer builds where determinism defects actually surface carry no stamp at all. The **`.asmdef`
> closure alone** was rejected because it names *which* assemblies participate, not what is in them,
> so two builds differing only in compiled code have identical closures. The adopted answer is not a
> fourth option but the union of the two workable ones: the closure is the **scope selector**, the
> MVIDs are the **content**. **2. Two rules that are not aesthetic.** The closure is **declared, never
> discovered** — `AppDomain.GetAssemblies()` returns whatever happens to have been loaded, which
> differs between a player run, an editor run and a test run of one build, so `MatchEngineBuildIdentity`
> names its 20 modules with `typeof` expressions and a missing one is a compile error rather than a
> silently shorter hash. And `buildHash` is **outside every digest preimage**, which is the constraint
> that held this landing to one save format: `EnvironmentFingerprint.ComputeDigest()` *is* the §3.2.3
> snapshot-header preimage's envFp slot and `header.SchemaVersion` is in that preimage too, so putting
> the hash on the fingerprint — or bumping `SNAPSHOT_SCHEMA_VERSION` to widen the deterministic-sim
> header — would have moved every snapshot digest and invalidated the golden-vector corpus certified
> July 19, 2026 on a pinned host this project cannot currently re-run. `SnapshotHeader` was the right
> home for the opposite reason: it already carries `schemaVersion` and `digestVersion`, two of
> `DeterminismContext`'s four fields, so this **completes the split #16 §2.3 v1.1 documented** rather
> than creating a parallel type. **3. Landed.** New `BuildIdentity` + `BuildModule` (fail-loud on an
> empty closure, a duplicate name, a `default(BuildModule)` and a non-ASCII name the §3.2.4.1 encoder
> would silently mangle); `SnapshotHeader.BuildHash` with a REQUIRED `Initialize` parameter;
> `TickOrchestrator` takes it beside the fingerprint; `MatchEngine` stamps, copies and gates it;
> `MatchSaveCodec` carries it at `MATCH_SAVE_FORMAT_VERSION` **1 → 2** and refuses an empty value at
> **both** ends. Restore fails closed with the new **`ERR_DS_REPLAY_BUILD_MISMATCH` (0x160E)**, kept
> distinct from `ERR_DS_REPLAY_ENV_MISMATCH` because a recompiled engine on the same host passes the
> fingerprint check and must still be refused — collapsing those two axes is the reading the ERR was
> filed against. Spec: #16 §2.3 **v1.2** with new normative **§2.3.2** and **FR-DS-014** (the v1.1
> "open GAP" paragraph kept **frozen and quoted in place**, not deleted); §3.4 **v1.0.16** adds
> `DOMAIN_TAG_BUILD_IDENTITY = 0x2E` — allocated *after* the roadmap §6 reserved block `0x2B`–`0x2D`
> so no spec-pinned subsystem number moves, and with no `SubsystemOrdinals` mirror since it registers
> no stream; §3.10 adds `EC-016-015`; `match-save-file-design.md` **v0.4** adds **KD-7**.
> **No `DETERMINISM_DIGEST_VERSION` bump, no `SNAPSHOT_SCHEMA_VERSION` bump, no RNG stream, no draw
> site, no draw-order change, no golden vector moved.** **4. Verification, including one thing the
> suite caught that reasoning did not.** 27 locks — 16 on the hasher (an independently derived golden
> vector from a Python mirror of the preimage, order invariance, name/MVID/count sensitivity, every
> guard) and 11 on the composition root (closure drift, per-module sensitivity across the *real*
> closure, header carriage, the KD-7 round-trip, both empty-hash refusals, the restore gate with a
> positive control). **Three mutants were executed and each was killed by exactly one lock.** And on
> its first run the suite failed for a real reason: `CaptureDurableHeader` deep-copies the header field
> by field and dropped the new one, so every save would have carried `BuildHash = null` and the restore
> gate would have skipped itself — the entire landing green and inert, which is precisely the failure
> class this project keeps re-meeting. **GATE: whole-tree at HEAD, 33 test assemblies, build 0 errors
> and the same 5 warnings as baseline, quarantine empty; `DeterministicSim.Tests` 72/0/4 (baseline
> 56/0/4), `MatchEngine.Tests` 472/1/11 (baseline 461/1/11)** — the one failure in both runs being the
> inherited owner-held-red `sim_match_engine_close_chance`. The baseline was captured *before* any
> `src/` file was touched, and a line-by-line diff of the two runs shows **exactly two suites changed,
> by exactly this landing's 16 + 11 new locks, with every other suite byte-identical** — so "adds no
> new failure" is a measurement, not an inference.
> **5. `CLAUDE.md` compression — the owner call taken up.** The previous entry recorded this as NOT
> done because it "would mean editing historical entries, which this project's convention explicitly
> forbids", and named the cleanest form: move the long landing narratives to a `landing-history.md`
> and leave one-paragraph index bullets. That is what happened. **No text was edited, summarised,
> reordered or deleted** — six bullets' bodies (72,711 bytes; 70% of the file, one of them 38.6% on
> its own) were moved **verbatim** into the new `docs/tracking/landing-history.md`, each replaced by an
> index paragraph pointing at its section. `CLAUDE.md` goes **104,292 → 44,244 bytes (−58%)**; all 17
> bullets remain and **no item's open/closed status changed**. The new file states plainly that it
> confers no status — `open-issues.md` stays the owning record, `spec-error-log.md` the authority on
> every `ERR-` id, and a moved narrative is frozen at its move date, exactly as a pre-promotion design
> supplement is. **6. A discrepancy the move surfaced, recorded and NOT fixed.** `CLAUDE.md`'s OPEN
> ISSUES index carries **17** bullets against `open-issues.md`'s **16** entries: the **#29/#41** bullet
> has no owning entry at all, so the "16 active" figure reconciles only because it is counted from the
> record rather than from the index. That bullet's own text opens "WHAT REMAINS: nothing", which points
> at the resolved archive — but it was never in `open-issues.md` to archive, and re-classifying it is
> an owner call about what is open, not a side-effect of moving text. It is written down in three
> places and left exactly as it stood. **7. What is still open on the same contract.** The
> `SaveManager` `Fingerprint = null` write-site gap is untouched and has now gained a sibling: that
> same Stage-0 87-byte header carries no build hash either, deliberately, because widening it is
> exactly the `SNAPSHOT_SCHEMA_VERSION` bump the digest-preimage exclusion declines to spend. Closing
> both together still costs a golden-vector re-certification on the pinned host. **Modified:**
> `src/deterministic-sim/` (6 files + 2 new), `src/match-engine/` (3 files + 2 new),
> `docs/specs/deterministic-sim/section-2.md`, `section-3.md`, `docs/tracking/match-save-file-design.md`,
> `spec-error-log.md`, `open-issues.md`, `data-contract-index.md`, `file-manifest.md`, `CLAUDE.md`,
> `docs/tracking/landing-history.md` (new), `CHANGELOG.md`, `CHANGELOG-src.md`.

> **Last Updated (prior):** August 22, 2026, latest same day (**The four tracking gaps surfaced across this
> session's passes are now CLOSED — three registers reconciled, one new gap tracked.**) Doc-only; no
> `.cs` touched. **1. `open-issues.md` was a landing behind its own index.** Root `CLAUDE.md` has
> carried `ERR-008-023` since the August 7 landing and `open-issues.md` had **zero** mentions of it —
> and the `spec-error-log.md` body entry for -023 was itself missing until August 17. The surviving
> proxy-review entry now carries the full -023 record: the keeper's shot-stopping reach priced twice
> (`GK_BLOCKER_RADIUS_M` = 1.5 m went live at -022 and removed **~42% of the goal arc on every shot**,
> 1.000 → 0.584 at 16 m, keeper alone), **zero goals across four seeds**, the retirement of the
> keeper-only radius under P3, suite 15 → 16 with the near-tautology lock rewritten, and the run-419
> downstream. It also carries the **August 17 amendment**: the close-chance rebaseline was blamed on
> the wrong thing — an 18-seed paired bisect prices the whole -021/-022/-023 shot-lane chain at
> **−0.027 ± 0.039 (t = −0.70)** while **C1 / `ERR-012-011` costs −0.189 ± 0.038 (t = −5.05)**, and the
> −0.119 that drove the rebaseline sits at the **4.6th percentile** of its own two-seed estimator; the
> band is now owner-held RED. **2. A stale count inside that entry** — §3.2.10's catalogue was recorded
> as five #8 landings behind; with -023 it is **six**, the figure `CLAUDE.md` and §6.3.1 both carry. The
> historical "five … at this landing" statements in the review's §6.4 and in this changelog are
> deliberately NOT edited — they were true at their landing, and this project preserves historical rows
> verbatim. **3. The `buildHash` gap is now tracked where live blockers live.** `ERR-016-009` recorded
> it OPEN in the error log yesterday, but `open-issues.md` had no entry — so it was a filed defect
> nothing would surface. Appended to the existing `EnvironmentFingerprint.floatModelHash` entry rather
> than filed as a new one (that entry already owns the `SaveManager` writes `Fingerprint = null`
> remainder on the same contract, and after yesterday's de-duplication pass a near-duplicate entry was
> the wrong instrument). The distinction is stated: `Fingerprint = null` is a **write-site gap on a
> field that exists**; `buildHash` is a **field that does not exist** — verified by
> `git ls-files 'src/*' | xargs grep -il 'buildhash'` returning **zero files**, `EnvironmentFingerprint`
> included. Consequence recorded: the fingerprint pins the host and float mode, not the binary, so two
> builds differing only in compiled game code are indistinguishable downstream of §2.3. Deliberately
> **not** fixed — what constitutes build identity (assembly MVIDs? a CI-stamped commit? the `.asmdef`
> closure?) is a decision, not an implementation detail. **4. A consistency gap this session created.**
> `data-contract-index.md` recorded the #16 §2.3 names as *"an observation, not a defect claim"* with a
> count of **four**; a day later they became filed defect `ERR-016-009` with a count of **six** (the
> note had omitted `ToleranceRow` and `ComparatorRegistry`). The index goes **v1.1**: the note now cites
> the ERR, gives the right count, and **points at #16 §2.3 v1.1's mapping table instead of restating
> it** — which is §0 rule 2 (*the pointer targets win*) working as designed on the file that declares
> it. No table row changed. **Counts re-verified after all edits:** `open-issues.md` **16 active**,
> archive **44**, both by direct `grep -c '^- \*\*'`, matching `CLAUDE.md`'s preamble.
> `recurring-defect-lint.py` **0 ERROR tree-wide**. **NOT done, and why:** compressing `CLAUDE.md`'s
> OPEN ISSUES bullets — the measured token weight, 67% of the file with the #29/#41 bullet alone at
> 38.7% — would mean editing historical entries, which this project's convention explicitly forbids
> ("historical rows preserved verbatim"). That needs an owner decision about what history may be
> summarised, not a unilateral pass. **Modified:** `docs/tracking/open-issues.md`, `CLAUDE.md`,
> `docs/tracking/data-contract-index.md` (v1.1), `docs/tracking/file-manifest.md`,
> `docs/tracking/CHANGELOG.md`.

> **Last Updated (prior):** August 22, 2026, later same day (**The football-judgment proxy review's DUPLICATE
> entries de-duplicated — and it was not the duplicate it looked like.**) Doc-only; no `.cs` touched.
> The review had **two** active entries in `open-issues.md` and **two** bullets in root `CLAUDE.md`.
> Investigating before deleting showed they are **not two copies of one record**: the second of each is
> the record of the **concurrent `claude/football-judgment-proxy-review-pq12dz` branch (PR #305)**,
> which filed its own `ERR-008-021` against the same section with a **different fix** — ability
> weighting only, explicitly leaving the containment cliff in place — and the **August 7 merge kept the
> other branch's form, not PR #305's** (`spec-error-log.md`'s superseded `ERR-008-021` entry, annotated
> 2026-08-11, is the authoritative reconciliation). The two records also disagree on `ERR-008-021`'s
> landing date (Aug 5 vs Aug 6) and give **different gate results**, which is why a mechanical
> merge-and-delete would have been wrong. **Resolution.** `CLAUDE.md` and `open-issues.md` each keep
> ONE entry — this branch's live line, current through `ERR-008-023`. The PR #305 record moved to
> `open-issues-resolved.md` **verbatim**, under a *blockquote* annotation (deliberately not a bullet, so
> it is not counted as a resolved issue) stating that it is a superseded parallel record, not a resolved
> one, and that it describes a fix **not live in `src/decision-tree/OptionGenerator.cs`**. Precedent:
> this file's own August 2, 2026 archiving of "a duplicated pair". **Two facts were carried forward into
> both survivors rather than left only in the archive:** PR #305's form **was** genuinely gate-verified
> at **CI run 404, head `3f207ee`, Aug 7** — a *different run* from the withdrawn CI-402 claim, and
> **not evidence for the code that shipped** — and that branch's **AR-1 H-1**, selecting a SINGLE
> goal-line-nearest goalkeeper candidate for the P3 exemption instead of exempting the whole 6 m GK
> band, which the reconciliation records as *"strictly better… deliberately NOT grafted in this merge"*,
> i.e. **real, unlanded, follow-up-worthy work** on top of what `ERR-008-023` left. **Counts corrected:
> the active count had been double-counting this issue.** `open-issues.md` **17 → 16 active** and the
> archive **43 → 44**, both re-derived by direct `grep -c '^- \*\*'` after the change. **Verified no
> loss:** the archived body is byte-identical to what was removed, and every distinctive token from the
> removed `CLAUDE.md` bullet still resolves somewhere in the tree. **Honest note on token cost: this did
> NOT shrink `CLAUDE.md`** — 3,102 chars of bullet came out, ~2,400 of annotation went in, so the file
> is roughly flat at ~104k chars (~29k tokens). The win is correctness, not size. **Where the size
> actually is, measured:** OPEN ISSUES bullets are **67% of the file**, and the single
> #29-Training/#41-Injuries bullet is **40,214 chars — 38.7% of `CLAUDE.md` on its own**. Any real
> token-cost work starts there, not here. **Modified:** `CLAUDE.md`, `docs/tracking/open-issues.md`,
> `docs/tracking/open-issues-resolved.md`, `docs/tracking/CHANGELOG.md`.

> **Last Updated (prior):** August 22, 2026 (**§6.3.1: the football-judgment backlog now has a LANDING ORDER,
> not just a taxonomy.**) Doc-only; **no `.cs` touched**, so no gate run (the whole-tree gate ran
> yesterday on the last `.cs` change — 33 suites, build 0 errors, the single inherited red band).
> §6.3 classified each finding; nothing said what order to fix them in, and with 24 workable the
> default "any order" was quietly costing sequencing decisions. **Derived from three things only, and
> the subsection says outright that for roughly ten of the 24 the order does not matter.** **(A) Is the
> chain's terminal action wired** — `match-engine-wiring-backlog.md` records **W2 BUILT August 12 but
> SHIPS DISABLED pending W6**, with `MatchEngineConstants.TackleContactRadiusM` defaulting to **0.0f**
> (verified in source), and **#14 §3.6 "Tackle Intent" is the section immediately above the §3.6.5
> Tackle Outcome Resolution W2 added** (`ERR-014-006`) — so fixing the COMMIT gate now tunes the input
> to a disabled output. That is the root `CLAUDE.md` trap-table row *"tuning a machine that is missing
> pieces"* verbatim, and it defers **7** findings (#8 PRESS trigger, #13 ×4, #14 ×2). **(B) Does it need
> a design supplement first** — §6.3's mechanism class already names 4 (#8 §3.2.2.1, #15 ×2, #27); those
> are serialized on a *document*, so they are scheduled **start-first / land-last** rather than left to
> become the critical path at the end. **(C) Batch by calibration chain** — P5 calibrates once, so
> fixes sharing a measured output land together. **Six batches: off-pitch 3 → keeper 4 → contact/duel
> 4 → singletons 2 → supplement-gated 4 → pressing/defensive 7 = 24.** Batch 2 is newly unblocked: the
> conversion-at-contact residual was PARKED August 4 *because W1 changes the contact geometry the
> decision turns on*, and W1 landed that same day — recorded with the caveat that it needs a **fresh
> measurement against post-W1 geometry**, not a fix written against pre-W1 numbers. Two cross-cutting
> constraints recorded with it: **#8 §3.2.10's constant catalogue is six #8 landings behind** and must
> be discharged at the next #8 landing (batches 5 and 6 both touch #8), and **`sim_match_engine_close_chance`
> is owner-held RED** (hold red, do not rebaseline a third time), so batches 2/3/5 land against an
> already-tripped detector and need per-fix measurement rather than a band read. **What §6.3.1
> deliberately does NOT do:** re-prioritise by football impact. None of the 24 has been measured against
> the others, and ranking them by guess would be the same false precision the §5 spec-count correction
> was about — so the order comes only from wiring state, document serialization and calibration
> batching, three things the repo already knows to be true. One honest caveat is stated in the table
> rather than glossed: #14 §3.5 (threat score → marking priority) is gated by chain-coherence, not by
> W2 directly, and could move earlier. **Modified:**
> `docs/tracking/football-judgment-proxy-review.md` (new §6.3.1 + header entry),
> `docs/tracking/file-manifest.md`, `docs/tracking/CHANGELOG.md`.

> **Last Updated (prior):** August 21, 2026, later same day (**The three findings reported at the C6 landing
> are FIXED, and the whole-tree gate ran for the first time this session.**) **1. `ERR-016-009` filed
> and its spec half RESOLVED same commit** (`spec-error-log.md` v2.18): #16 §2.3's nine "Data
> Structures" include **six that name no type in `src/deterministic-sim/`** — `DeterminismContext`,
> `RngStreamKey`, `ToleranceRow` and `ComparatorRegistry` have **zero textual presence in `src/` at
> all**; `RngCursor` and `RngStreamKey`'s triple are *fields on* `RngStreamState`; `PhaseDigest` is a
> computation whose preimage is locked by golden-vector corpus D-01/D-02. §2.3 carries the weight
> because §4.2 has been explicitly non-normative since its own v0.7 and §4.4's module paths
> (`sim/tick/*` …) match **no directory** in the flat tree — and §2.3's version history revised two of
> the phantom structures as contract text (v0.7: "corrected `RngStreamKey` … extended `RngCursor`").
> **The substantive half is `buildHash`:** a declared field of the replay-identity context with no
> representation anywhere, `EnvironmentFingerprint` included, so two builds differing only in compiled
> code are indistinguishable to everything downstream of §2.3. Fixed as #16 §2.3 **v1.1** — a per-row
> implementation-mapping table (TYPE / FIELDS ON / COMPUTED / DEFERRED / SPLIT + GAP) plus a normative
> declaration that **`src/` is the surface authority and §2.3 the concept inventory**. Recorded OPEN,
> not fixed: the `buildHash` gap (sits with the existing `SaveManager` writes `Fingerprint = null`
> item) and the `ToleranceRow`/`ComparatorRegistry` Stage-1+ deferrals. **Deliberately no rename** —
> the serialized field names are correct as built, and renaming Tier-A state to match a document moves
> state for no behavioural gain. **2. The FR-CS-057 header regression fixed:**
> `src/match-viewer/tests/LiveMatchStreamerTests.cs` carried `// Modified: 2026-07-27` against a v1.2
> row dated 2026-08-15 (the Aug 15 P4b landing added the row and not the field). `recurring-defect-lint.py`
> is back to **0 ERROR tree-wide**, restoring the state last recorded August 8. **3. The proxy review's
> two SPEC counts corrected — the three FINDING counts were and are right.** §5 read 24 specs with
> findings / 29 clean, which is self-consistent (24 + 29 = 53) and matches the §4 parenthetical but
> matches neither section's content. Re-derived by direct count: **19 spec headings** in §2/§3, **34
> clean specs** in §4's list, 19 + 34 = 53. The finding counts reconcile exactly and are untouched —
> 35 `- **§` bullets minus the shot-lane bullet marked "not itemized" = 34 recorded, minus
> `ERR-008-020` and `ERR-008-019` = 32 open. Corrected in seven places with the correction annotated
> in place, never silently: the review's §4 and §5, `CLAUDE.md` ×2, `open-issues.md` ×3, `README.md`,
> `file-manifest.md` (whose "1 already fixed" was also stale). **The C6 text landed earlier today is
> unaffected** — it states only the six-assembly-less-specs figure, which is directly verifiable.
> **GATE RUN — the first of this session** (SDK 8.0.130 installed from the Ubuntu apt archive after an
> `apt-get update`; the initial install 404'd on stale index files, which is worth knowing for the next
> session). Build **succeeded, 0 errors**, 5 pre-existing CS0649 warnings. **33 suites, 3,024 tests
> passed, 1 failed, 191 skipped.** The single failure is `sim_match_engine_close_chance`
> (`MatchEngine.Tests` **461 / 1 / 11**, 1 h) — the **inherited owner-held-red band**, matching the W2
> landing's recorded baseline of 461/1/11 exactly, so this pass adds **no new failure**. The gate
> script exits non-zero on the blocking phase and therefore never reached its quarantine-report
> section: **the formal verdict is FAIL on the inherited red, not PASS**, and it is recorded that way
> rather than rounded up. Only `.cs` change in the pass is a comment header. **Modified:**
> `docs/specs/deterministic-sim/section-2.md` (v1.1), `docs/tracking/spec-error-log.md` (v2.18),
> `src/match-viewer/tests/LiveMatchStreamerTests.cs`, `docs/tracking/football-judgment-proxy-review.md`,
> `CLAUDE.md`, `README.md`, `docs/tracking/open-issues.md`, `docs/tracking/file-manifest.md`,
> `docs/tracking/CHANGELOG.md`.

> **Last Updated (prior):** August 21, 2026 (**Sequencing rule C6 recorded: spec hardening does not precede
> the assembly — plus the pointer index that replaces a rejected `DATA_SCHEMA.md`.**) Doc-only; **no
> `.cs` file touched.** Prompted by a review of an externally-suggested "upgrade every legacy spec
> with a Gen-5 model first, then implement" pipeline. **What was rejected and why:** a blanket rewrite
> of all 53 specs (this project's most recurring bug class is cross-reference cascades — `XC-`/`FM-`/
> `EC-`/`ERR-` ids and `[CROSS]` citations — and a 53-way rewrite by a model that cannot see the other
> 52 is a cascade generator; it would also re-open the ERR back-props landed since May, and its
> suggested "fixed-point float" type contracts contradict the Stage-0 `float` / Stage-5 Fixed64
> decision), and a master `DATA_SCHEMA.md` (a second authority over contracts #27/#30/#16 already own
> — the parallel-surface defect this project has hit as `LineupSelector.CanSelect`, the three copies
> of the `Save` signature, and the two hand-copied cursor walks). **What was kept:** the spec-only
> test-generation check, folded forward onto NEW specs at promotion with the pass bar changed from
> "the test file compiles" to *the test fails when the fix is reverted* — because compiling-and-green
> is exactly what this project's tautological locks already were. **NEW:** `docs/tracking/data-contract-index.md`
> v1.0 — entity → owning spec § → assembly, pointer-only, with §0's three rules (restate nothing /
> the pointer targets win / rows not columns). Two of its rows did not resolve on verification and
> were corrected before landing rather than shipped: `ScreenId` lives in `ui-framework` (it is
> `ClientScreens` that `client-app` owns), and **four `#16 §2.3` names — `DeterminismContext`,
> `PhaseDigest`, `RngStreamKey`, `RngCursor` — have no type of that name in `src/deterministic-sim/`**;
> recorded in the index as an observation, not filed as a defect. **Modified:**
> `docs/tracking/path-to-playable-roadmap.md` (v0.21 — new constraint **C6**, the normative source: a
> spec with no `src/` assembly is not hardened ahead of its own T0 landing; findings are RECORDED and
> discharged at T0, in the same commit as the code they govern. Three grounds, all already in the
> repo: §6.3 requires spec + code in one commit and a fix is not landed until a test fails when
> reverted — both unexecutable with no assembly; C5's own list shows the defects that matter here die
> on first execution, not on re-reading; and P5/KD-W1 already refuses `[GT]`s over unwired subsystems.
> **C6 is explicitly NOT "implement all 20 first"** — §6 defers fourteen past PM-3 and §10 disclaims
> finishing the spec set; the implementation order is unchanged),
> `docs/tracking/football-judgment-proxy-review.md` (new §6.3 **assembly-less class** + header entry —
> six finding-bearing specs have no assembly, #31/#34/#36/#43/#46/#54, holding **8 of the 32 open
> findings**, now deferred to their T0 landings; **the counts are unchanged — 34 recorded, 2 fixed, 32
> open** — this reclassifies *when* 8 of them are workable, not whether, and leaves 24 as the
> backlog's actual queue), `CLAUDE.md` (the C6 sentence on the live-gap paragraph + a TRACKING
> DOCUMENTS row), `docs/tracking/file-manifest.md`, `docs/tracking/CHANGELOG.md`. **Verification:**
> `tools/recurring-defect-lint.py` reports **0 findings of any class against all six changed files**.
> Tree-wide it now reports **1 ERROR**, pre-existing and unrelated —
> `src/match-viewer/tests/LiveMatchStreamerTests.cs:1` FR-CS-057 (`// Modified: 2026-07-27` vs a
> 2026-08-15 version row, from the P4b landing) — which means the "0 ERROR tree-wide" state recorded
> on August 8 has since regressed and was NOT re-derived; left unfixed as out of scope and reported.
> No gate run: no `.cs` changed.

> **Last Updated (prior):** August 17, 2026 (**The shot-lane chain is REFUTED as the cause of the held-red
> close-chance band, C1's cost is priced, and `ERR-008-023` finally has a body entry.**) A salvage
> of `claude/shot-lane-regression-bisect-vflxc2`, a branch that measured all this on August 10 and
> never merged; ported onto `main` selectively, and what was dropped in the port matters as much as
> what was kept. **Kept — the bisect (`close-chance-creation-design.md` §11, v2.3).** 18 seeds × 90
> minutes × 6 trees, 108 full matches, the tree held at `64513e4` with only the three shot-lane
> files swapped so the chain is not confounded with #41 T2, the `LineupSelector` collapse or the
> `ConfigureSquads` overload. Seed `0xD1A6D05E` is **bit-identical** before `ERR-008-021`, after it,
> and after `ERR-008-022`, moving only at `-023` — so the Acceptance-3 regression is one commit. But
> it is a **trajectory resample, not a mechanism**: no DRIBBLE path reads `GoalOpeningScore`, the
> 8-sector scan's tie resolves to `AgentFacingDirection`, and the whole −0.349 swing rides on four
> extra final-third shots. Over 18 paired seeds the chain's directional effect is **−0.027 ± 0.039**
> (t = −0.70, 8 up / 10 down) against a between-seed sd ≈ 0.17; the −0.119 that drove the August 7
> rebaseline sits at the **4.6th percentile** of that pair's own estimator across all 153 pairs, and
> 4 of the 18 seeds are already below −0.16 with none of the chain applied. **So Acceptance-3's two
> named suspects are refuted and its KD-W1 hand-off has nothing to act on.** `-021` is inert on 5 of
> 6 seeds, which confirms `ERR-008-022`'s own diagnosis by another route — the goal-centre-plane
> bound was discarding the very blockers `-021` existed to weight. **The regression that is actually
> live is `ERR-012-011` (wiring-backlog C1), and that one IS a mechanism** — **−0.189 ± 0.038,
> t = −5.05, 16 of 18 seeds down** — which is exactly the "further from goal" cost this project
> predicted from C1's `InPoss` `PullFactor` column and never measured. **This independently confirms
> §10.9**, which reached the same verdict from the post-C1 side and was owner-confirmed August 11;
> neither was derived from the other. Two bit-identical control rows fall out: everything between
> `64513e4` and `ba4e194` is inert on the engine, and the `ERR-008-024` refusal's behaviour-neutrality
> is now verified **by execution** rather than by reading its diff. **Kept — `ERR-008-023`'s body
> entry** (`spec-error-log.md` v2.17), missing since the August 7 landing while the id was cited from
> `CLAUDE.md`, `open-issues.md`, the design supplement and an `OptionGenerator.cs` comment; written
> from those existing records alone, nothing new asserted about the landing. **DROPPED in the port,
> deliberately: the branch's `ERR-008-021` (a)/(b) retitle.** `main` had already reconciled that
> double-allocation at `ERR-028-019` (v2.13) — and the branch's accompanying claim that "both
> landings are live in `ComputeGoalOpeningScore`" is **false**: `git log -S PerceivedBlockAbility`
> returns exactly one commit, `accc941`, and the merge `14d0796` states outright that PR #305's form
> was discarded. Porting it would have overwritten a correct annotation with an incorrect one. The
> ported entry therefore cites the SHIPPED `-021` throughout and says so explicitly. **Two hygiene
> catches from the port itself**, both this project's recurring classes: the branch allocated
> `close-chance-creation-design.md` **v1.6**, which `main` had independently used (renumbered to
> **v2.3** here — the same concurrent-branch collision that produced the `ERR-008-021` mess in the
> first place); and every figure in §11 **predates the W2 tackle wiring** (`ERR-014-006`, `ba40114`),
> so the controlled attributions stand but the absolute values are not today's `main` — recorded as
> a currency caveat at four sites rather than left implicit. **Code:** comments only —
> `MatchEngineCloseChanceScenarios.cs` → v1.2, the KD-W1 hand-off withdrawn and both bounds restated
> as **floors, not estimators**, with the limit written at the predicate instead of widened away. **No
> predicate, bound, seed or `[GT]` changed; nothing re-tuned (KD-W1); the `ERR-008-024` tie-break fix
> deliberately not re-attempted (§10.5).** The held-red `sim_match_engine_close_chance` stays red and
> stays C1's to answer. **GATE RUN (whole tree, locally, August 17, 2026): FAILED — and that is the baseline red state, not this landing's.** Build **0 errors**, 32 suites, quarantine unchanged; `MatchEngine.Tests` **461 passed / 1 failed / 11 skipped** (55 m 11 s). The single failure is `sim_match_engine_close_chance` (2 of 3 predicates), the owner-held-red predicate this pass re-attributes and deliberately does not fix — and the counts are **identical to main's W2 baseline of 461/1/11**, so the change adds no new failure. The `.cs` edit was independently proven comment-only by stripping comment lines from its own diff: **zero non-comment lines remain**. **RE-RUN on the merged tree (August 17, 2026, after merging main's 13 P4b commits): same verdict.** Build 0 errors, 32 suites, quarantine unchanged, `MatchEngine.Tests` **461 / 1 / 11** (40 m 58 s) — identical counts to the pre-merge run and to main's own W2 baseline, with main's P4b client suites green in the same sweep (`MatchClientCore.Tests` 168/168, `ClientApp.Tests` 15/15, `UiFramework.Tests` 50/50). Same single failure, `sim_match_engine_close_chance`.
>
> **Last Updated (prior):** August 16, 2026, later same day (**P4b AR round 4 — Medium/Low findings M19-M22/L12-L13, all fixed — and the doc-backprop finding (M13) regressed a THIRD time, now fixed with figures verified fresh rather than copied forward.** M19: round 3's own `RequireMarkingBandFitsBelowShadowLayer` (`MatchClientConstants.cs`) had no lower bound on `MarkingLayerStepM` — a zero or negative step passed the ceiling check trivially (`0 × count` is always under any positive clearance), silently reinstating the exact M16 z-fighting bug with no diagnostic — and its ceiling was the wrong SCALE: the shipped 10-microns-per-step default was 54-100× finer than the 1 mm M12 itself judged sufficient for the SAME z-fighting hazard at the SAME camera distance, with nothing explaining the gap. Fixed: the validator now composes a strictly-positive check first, `MarkingLayerStepM`'s default is raised to 0.001 m (matching M12's own judgment exactly), and `BallShadowLayerHeightM`/`PossessionRingLayerHeightM`/`AgentMarkerLayerHeightM` are raised together (0.08/0.081/0.082) so the resulting 0.027 m band still clears comfortably while the shadow/ring/marker keep the same 1 mm gaps M12 originally chose. M20: `MatchClientBehaviour.cs`'s two rotation checks used sign-sensitive `!= Quaternion.identity` — a legitimately-unrotated transform whose composed quaternion lands on the negative representative of identity was falsely rejected, by a message that printed only `eulerAngles`, itself `(0,0,0)` for that exact case (i.e. "must be identity" next to a value that already read as one). Both sites now use a new double-cover-safe `IsIdentityRotation`, and both messages print the raw `(x,y,z,w)` components too. M21: three doc comments (two in `BallRenderModel.cs`, one in `MatchClientBehaviour.cs`'s `RenderBall`) still described the ball's pre-M17 (round 3) height behaviour; corrected to state the M17 radius floor. M22: the prefab-contract clause 2a never distinguished the marking circle and possession ring (which must be STROKED outlines) from the marking spot/agent marker/ball shadow (which must be FILLED discs) — `PitchMarkingKind.Circle`'s own doc makes the distinction explicit ("a renderer that collapsed the two would draw a solid centre circle") — now named in both the type doc and the README's slot table. L12: `Update` had no H5-shaped guard, so one non-finite frame coordinate (which `MatchRenderProjection` refuses fail-loud, and nothing upstream gates) threw the same exception every frame forever, with the camera and click handler never reached again; now wrapped in a try/catch routing to `RejectWiring`, mirroring H5's own `Awake` catch. L13: the README named no pitch/ground-surface requirement at all, even though the M12/M16 ground-layer scheme's lowest layer (`MarkingLayerHeightM`, default 0) sits exactly where an "obvious" `Y = 0` turf plane would too — a new README section states the surface must sit strictly below it. **M13's third regression:** round 3's landing (`d17ab63`) touched `CLAUDE.md`/`README.md`/`file-manifest.md`/`open-issues.md`/the design supplement but never `CHANGELOG.md` or `CHANGELOG-src.md`, so this entry's predecessor still claimed "three commits, two AR rounds" and `MatchClientCore.Tests` 165/165 — both stale. Verified fresh this round: `MatchClientCore.Tests` **168/168**. *(Round 5 note: the commit count and the `git log` citation that stood here were themselves wrong, and are DELETED rather than corrected — see the round-5 entry above for why this class of claim is no longer made.)* Also fixed: `file-manifest.md`'s three self-contradictions (a stale H1-H6/M1-M13/L1-L8 finding range, "two AR rounds", a README row still describing the assembly as empty), root `CLAUDE.md`'s and `src/match-client-unity/README.md`'s matching stale status lines, and `interactive-unity-client-design.md` §5-P4b's job-list bullet, which still described markings placed "at one of four ordered `[GT]` ground-layer heights" — exactly what round 3's own M16 replaced with the band formula. **Gate: PASSED.** Full solution rebuild: 0 errors, 5 pre-existing warnings (3× CS0649 in `decision-tree/ActionSelector.cs`, 1× CS0649 in `pass-mechanics/Tests/PassMechanicsTests.cs`, 1× CS0219 in `ball-physics/tests/BallIntegrationTests.cs`, none in this landing's files); `MatchClientCore.Tests` **168/168** (0 skipped), verified by an isolated `dotnet test` run since `run-gate.sh`'s `-clp:ErrorsOnly` build step does not preserve a re-inspectable per-suite breakdown; whole-tree gate PASSED, quarantine empty; `SeasonSave.Tests` 320/0/3/323 (3 m 18 s); `MatchEngine.Tests` **436 passed / 0 failed / 10 skipped / 446**, 42 m 3 s — unchanged from round 3's own baseline, confirming no regression from this landing (all its changes are in `match-client-core`/`match-client-unity`, neither referenced by the sim). `match-client-unity` stays outside the shim gate by design; reviewed by hand. No `SNAPSHOT_SCHEMA_VERSION` change, no new RNG stream / domain tag / draw-order change.)

> **Last Updated (prior):** August 16, 2026 (**P4b — the Unity client binding, `MatchClientBehaviour.cs` — LANDED and is now GATE-VERIFIED end to end, across three commits and two AR rounds; this entry also backprops the landing into the tracking docs that had none (M13 of round 2's Medium/Low pass).** P4b is the `MonoBehaviour` host `src/CLAUDE.md` recorded as "WHAT IS NOT HERE YET": it owns a `MatchSession`, reads a frame each `Update`, and binds `AgentRenderModel`/`BallRenderModel`/`PitchMarking` onto scene objects, enforcing the prefab contract (neutral root, unit sizing, the colour-property name, no world-space `LineRenderer`) at instantiation rather than leaving it as prose. **Round 1 (commits `97bca12`/`2538147`) — 3 High + 9 Medium + 5 Low, all fixed:** H1 moved the rectangle-into-four-lines marking decomposition out of the binding into `match-client-core`'s new `PitchMarkings.BuildDrawables()` (§12 rule 1 — corner arithmetic belongs where the gate compiles it); H2 turned the prefab contract from a comment into an enforced check; H3 replaced a single "default primitive radius" divisor with a unit-radius/unit-length authoring contract. The Mediums covered wiring validation, the goalkeeper/sent-off marker tints, the possession-ring radius reading its own render model, a once-per-agent `MeshRenderer` resolve, the tick-rate source, the frame latch state machine (extracted into the new `LiveFrameLatch`), the render-count walk, and the Active Input Handling requirement. **Round 2 (commit `5c93940`) — 3 High, all fixed:** a missing `.meta` file (fresh GUID every checkout), an `Awake` exception that used to leave the component enabled with null fields (Unity delivers `Start`/`Update` after logging an `Awake` exception rather than disabling it), and a bare `Shader.PropertyToID("_Color")` literal that silently no-ops under URP — replaced with an inspector-exposed property name plus a per-marker material check. **Round 2's Medium/Low pass (this landing, M10-M13/L6-L8, all fixed):** the goal mouth gets its own prefab and `[GT] GoalMouthWidthM` instead of sharing the marking line's (M10); the prefab contract's unit-sizing clause is split into flat-ground-props-vs-volumetric-ball, with `GroundScale` renamed `FlatGroundScale` to say which rule a call site follows (M11); four new ordered `[GT]` ground-layer heights stop the four previously-coplanar ground layers (markings, ball shadow, possession ring, agent marker) from z-fighting (M12); `BuildMarkings`/`BuildAgentObjects`/`BuildBallObjects`/`BuildScene` now short-circuit on a wiring rejection instead of re-instantiating and re-logging for every remaining object (L6); `MarkingLineWidthM` and the new `GoalMouthWidthM` are validated like their render-cue siblings (L7); and a `CameraTiltDegrees`/`CameraLateralOffsetM` pairing check refuses the one combination (both zero) that degenerates `Transform.LookAt` (L8). M13 itself is this entry, plus corrected `interactive-unity-client-design.md` §5-P4b prose (it still described the pre-H1 contract) and new `file-manifest.md` rows for `MatchClientBehaviour.cs`, `LiveFrameLatch.cs`, `LiveFrameLatchTests.cs` and `PitchMarkingsDrawablesTests.cs`, none of which had one. **Gate: PASSED.** Build succeeded, 0 warnings; `MatchClientCore.Tests` **165/165** (0 skipped); whole-tree gate PASSED with the quarantine empty; `MatchEngine.Tests` **436 passed / 0 failed / 10 skipped / 446**, 28 m 13 s — unchanged by this landing (`match-client-unity` is outside the sim and stays excluded from the shim gate by design; `MatchEngine.Tests`' two previously-red scenario bands are green under the Aug 7 rebaseline, confirming that regression is unrelated to this work). `match-client-unity` itself stays outside the `dotnet-ci` gate (§12 rule 1 — it is never generated on Linux) and was reviewed by hand. No `SNAPSHOT_SCHEMA_VERSION` change, no new RNG stream / domain tag / draw site / draw-order change — nothing here reaches the sim.)
> **Last Updated (prior):** August 12, 2026, later still (**Wiring backlog W2 LANDED — a player in control
> can now be dispossessed for the first time in this engine, and `ERR-014-006` closes the tackle
> outcome model's governance question.**) New #14 §3.6.5 "Tackle Outcome Resolution" takes the
> tackle outcome decision back into the spec that owns the players, on the W1 precedent (#11 §3.7.0
> took the keeper's rush-commit distance back for the same stated reason — it is a property of the
> player, not the composition root). Neither original KD-6 delegate could accept: #8's `ActionType`
> ordinals are exhausted by the 3-bit composure-noise field, and #3 defers slide-tackle collision to
> Stage 2. A Stage-0 tackle is therefore an abstract attribute duel with **four** outcomes — `MISSED`
> / `BALL_WON` / **`BALL_LOOSE`** / `FOUL` — `BALL_LOOSE` at owner direction, because a won/missed-only
> model has no way to express the commonest result of a challenge (the ball going somewhere neither
> player controls) and folding it into `BALL_WON` would make every successful tackle a clean turnover.
> New `src/defensive-ai/TackleOutcome.cs`, `TackleDuelInputs.cs`, `TackleOutcomeResolver.cs` (ten new
> `[GT]` + one `[FIXED]` numerical ceiling, all **un-calibrated per KD-W1**). `MatchEngine.cs` wires
> the resolver at the COMMIT contact gate — radius **1.5 → 2.5 m**, re-derived (not fitted) from what
> COMMIT means, a lunge, #3 §7.2.1's own extended-leg case for its Stage-2 slide tackle — publishes
> `ContactType.SLIDE_TACKLE` for the first time anywhere in the tree, and routes a tackle foul through
> the **existing single foul-candidate slot** under KD-F4 strongest-wins rather than a second authority
> (`ApplyFoulIfCaptured` does not re-judge it — #14 §3.6.5 already priced the challenge).
> `SNAPSHOT_SCHEMA_VERSION` **20 → 21** (per-agent tackle flag + challenge cooldown; the four outcome
> counters excluded, proof at the write site). `DOMAIN_TAG_DEFENSIVE_AI` (0x1A) gets its **first draw
> site anywhere in `src/`** — keyed, not reserved, un-blocking #14's own T-DA-DET-005 — and the
> `match-flow.card-severity` draw order **moves by design** (the foul branch now draws on ticks that
> previously had none). **No digest invariance is claimed anywhere in this landing.** `Tackling` gains
> its first consumer anywhere in the tree; `Marking` still has none. Surfaced and fixed in the same
> commit: FM-08's CONTACT-time possession-loss log was `LogError`("Race condition") — accurate only
> while an ordering accident was the sole way to lose the ball mid-windup; a tackle makes it an
> ordinary football event, so it is now `LogWarning` with corrected wording. Preceded by the census
> (`TackleIntentDiagnosticTests`, 3 seeds × 90 min, both defending teams separately): **681.7 defending
> episodes, 310.2 within 3 m of the carrier, 97.2 with an intent naming him, 65.3 with a COMMIT** per
> defending team per match — against football's ~15–17 tackle attempts per team per 90, so the gate
> supplied **~4× what was needed** and W2 was a RESOLUTION problem, not a producer problem, the
> opposite of the C1 trap. Tests: 12 pure resolver locks + 7 composed engine locks, all green at the
> landing commit (`fc8f81f2`). **GATE PASSED for W2 (August 12, 2026):** whole-tree build 0 errors / 0 warnings, quarantine empty, 32 suites; `MatchEngine.Tests` **461 passed / 1 failed / 11 skipped** (38 m 2 s). The single failure is `sim_match_engine_close_chance`, the inherited owner-held-red predicate that also fails at the pre-change baseline `4b9271c` — so the branch is at its baseline red state and W2 adds no new failure. Baseline was 451/1/10; the +10 passed are W2 locks and the +1 skipped is the env-gated census instrument. — a whole-tree gate was in flight at the time
> this entry was written. ⟨PLACEHOLDER — operator to fill in build/warning counts, per-suite
> pass/fail/skip (especially `DefensiveAI.Tests` and `MatchEngine.Tests`), quarantine state, and the
> PASS/FAIL verdict once the run completes.⟩ `docs/tracking/match-engine-wiring-backlog.md` → **v1.8**
> (W2 marked WIRED; next in sequence **W4** keeper perception, then **W12**), root `CLAUDE.md` (OPEN
> ISSUES wiring-backlog bullet updated; the standing "no player has ever made a tackle" claim
> retired), `docs/tracking/open-issues.md` (mirrored), `docs/tracking/CHANGELOG-src.md` → **v2.115**,
> `docs/tracking/file-manifest.md`. Full account: `docs/tracking/tackle-wiring-design.md`, #14
> `docs/specs/defensive-ai/section-3.md` §3.6.5, `docs/tracking/spec-error-log.md` `ERR-014-006`.
> Prior entry below.

> **Last Updated (prior):** August 12, 2026 (**Roadmap A4a RAN — the round-resolution quick-sim is
> calibrated against the engine for the first time, and both KD-8 acceptance bars are recorded FAILED
> for two measured reasons that are not fit failures.**) KD-8 **Step 0 PASSED** on the current tree —
> strong-at-home mean margin **+4.000**, strong-away **−3.500**, upsets present (the July-28 record was
> +7.100 / −4.700, so the extremes have converged as the engine's goal rate fell). Corpus: **198 real
> 90-minute `MatchEngine` matches** over 11 `dSquad` buckets × 18, ~90 s/match, four processes, ~1.4 h
> — inside C1a's ~9 h budget. Least-squares fit: **`QuickSimBaseGoals` 1.35 → 1.2325,
> `QuickSimGoalRatingSlope` 0.35 → 0.2162, `QuickSimHomeAdvantageRating` 0.30 → 0.4996**; the three
> constants had carried a "provisional, not fitted" warning at their own declaration since #30 T2 and
> no longer do. **Two findings, both filed and both left as owner decisions.** **`ERR-030-033` — KD-8's
> ±0.25 per-bucket bar is below the sampling error of the corpus KD-8 itself sizes.** At ~18/bucket a
> bucket mean carries a standard error of 0.135–0.633 and **15 of 22 bucket-sides exceed the whole
> bar**, so a perfectly correct model scored against a re-run of the same corpus would also fail;
> resolving ±0.25 needs n ≈ 770/bucket ≈ 210 h against a budgeted ~9 h. The tolerance and the sample
> size were chosen independently and never checked against each other, which is why three AR passes on
> that note read the bar as a statement about the model. **`ERR-030-034` — KD-7's Poisson shape cannot
> express what the engine does.** Poisson fixes variance = mean; the engine is over-dispersed at
> **z = +5.40** (mean var/mean 1.395, 19 of 22 bucket-sides above 1), so it produces more blowouts and
> shut-outs and **far fewer draws** — 19.2% against the model's 26.8% at `dSquad ≈ 0`, most of the
> 7.6 pp W/D/L miss. A second-moment gap that no value of three mean-shaping parameters closes, and
> **the surviving half of roadmap risk row 1**. Neither was "fixed": widening a bar to fit its own
> result stops it being a bar (and there is a standing owner ruling against exactly that move, the
> August 11 close-chance call one entry below), and changing the distribution family moves persisted
> season state, since the scoreline reaches `LeagueTable` and `SeasonStateCodec` serializes it. **The
> corpus is committed, so a re-fit against a new family costs seconds rather than the run.**
> **Measured in passing: the corpus's grid-weighted goal rate is 3.09/match.** **⚠️ CORRECTED August 12, 2026 by the goal-rate match-realism pass:** 3.09 is the **grid-weighted** mean, and the grid samples `dSquad` −5…+5 uniformly while a real season clusters near 0 and mismatches score more. Re-measured: **balanced fixtures (`dSquad ≈ 0`, n=198) give 2.70 ± 0.13 vs football's ~2.7 — 0.02σ**; league-weighted 2.93 ± 0.15 (+1.47σ, not significant). **The engine did NOT overshoot football's rate; no defect, no `[GT]` moved.** The error was reading a calibration grid as a league; the fitter now emits all three figures so it cannot recur. **Also landed:** the harness gained a **sample-window** knob
> (`TD_CALIBRATION_SAMPLE_FROM`) so one bucket can be split across processes — KD-8 called the run
> parallelisable across buckets, but its acceptance bar lives at a *single* bucket, which could
> therefore only be deepened serially — with locks that a window is exactly the contiguous plan's
> slice (seeds **and** roster pairings, both keyed off the absolute index) and that adjacent windows
> tile without overlap; the fitter now emits per-bucket standard errors and the pooled dispersion test,
> so a FAIL verdict is interpretable instead of merely reported, plus `--wdl-csv`, which deepens the
> acceptance bucket **without** feeding those rows to the sample-weighted objective; and
> `RoundResolutionFitLockTests` pins the three constants, the per-bucket table over a fixed
> 4000-fixture sweep, grid-wide monotonicity, and home advantage isolated at `dSquad = 0` — the lock
> records the **achieved** 0.90 tolerance rather than the unmet ±0.25, so a future improvement tightens
> a real number instead of re-flying a claim. **Two methodology properties were verified rather than
> assumed, and both underpin the whole run:** a slice re-run in isolation reproduced its rows
> byte-for-byte (checked before committing the hours, not after), and the ±6 buckets run split across
> two processes reproduced the sanctioned `Pilot_Extreme` driver's own 20 rows **exactly**. **One
> process error, recorded:** a rebuild was run while a set of deepening slices was mid-flight, swapping
> assemblies under live test processes — this project's recorded gate-invalidation class (AR pass 9,
> `spec-error-log.md` v1.90). Those slices were **killed and re-run clean** rather than reasoned about;
> the 198-match corpus and the pilot predate every edit and were never at risk.

> **GATE (whole tree, this branch, August 12, 2026): build 0 errors; 31 of 32 suites green;
> `SeasonSave.Tests` **402 passed / 0 failed / 3 skipped** (the fitted constants moved no existing
> assertion) and `MatchEngine.Tests` **451 / 1 / 10`.** The single failure is
> `sim_match_engine_close_chance` at `meanCosine = −0.165` (bound −0.16) and `goalwardShare = 0.407`
> (bound 0.42) — **byte-identical to the values already recorded on `main`**, and the band the owner
> ruled on August 11, 2026 to **hold red rather than rebaseline a third time**
> (`close-chance-creation-design.md` §10.9 item 6). It is not reachable from this change: `match-engine`'s
> asmdef does not reference `TacticalDirector.SeasonSave` at all, so the round-resolution constants are
> structurally invisible to the engine — verified, not assumed. `python3 tools/recurring-defect-lint.py
> --repo .` reports **0 ERROR** (125 WARN / 27 INFO, the unchanged baseline).

> **Also August 12, 2026, later same day (**`ERR-030-033` RESOLVED — KD-8's per-bucket acceptance bar is
> re-specified against the corpus's measured precision, and the same A4a fit now reads mean-agreement
> PASS. Landed on the advisory recommendation; the family question stays open.**) The flat ±0.25 could
> not be met by any model at the depth KD-8 itself sizes — 15 of 22 bucket-sides carried a standard
> error larger than the whole bar — so it was not a statement about the model at all. **It is not
> widened to fit its own result**, which would stop it being a bar and would contradict the standing
> owner ruling of August 11; it is restated against the precision the corpus actually has, a priori and
> for any corpus, which is the standard construction of a test with a controlled false-alarm rate.
> KD-8 now carries **A1** per-cell `|Δ| ≤ max(0.25, 2·se)` with **±0.25 retained as a FLOOR**, so a
> corpus deep enough that `2·se < 0.25` automatically restores the original requirement rather than
> abandoning it; **A2** at most `1 + round(0.0455·cells)` exceedances and none over `max(0.40, 3·se)`,
> because a 2σ screen over N cells expects ~4.55% to exceed by chance and a zero-exceedance rule would
> fail a correct model on a large grid; **A3** a pooled `χ² ≤ χ²₀.₉₅(cells − 3)`, **which is where the
> statistical power actually lives** — A1/A2 are per-cell screens and cannot see systematic misfit that
> every individual cell passes; **A4** an 18/bucket scoreability floor, without which the se-relative
> form is gameable by shrinking n, since that widens every tolerance; and **A5** the unchanged ±5 pp
> W/D/L bar plus a pinned n ≥ 250 for a resolvable *pass* and an INCONCLUSIVE verdict when a miss is
> not distinguishable from noise. **Measured against it: worst |z| = 2.06, one exceedance of an allowed
> two, no hard exceedance, pooled χ² = 16.0 on 19 dof against a 30.1 threshold — mean agreement PASS.**
> The verdict is now reported in **two parts, mean agreement and distribution shape**, because they
> fail for unrelated reasons and the single flat verdict had the practical effect of making
> `ERR-030-034` read as a fit failure when the fit is fine. Every figure is computed and emitted by
> `tools/round-resolution-fit.py` — χ² criticals by Wilson–Hilferty, no third-party dependency, verified
> against exact values at dof 10 and 19 — so none of it is hand-copied into prose. `ERR-030-034`
> (the Poisson family) is deliberately **still open**: no candidate family is yet supported by the data,
> since independent negative-binomial closes only ~0.5 pp of the draw gap and the shared-swing family
> that would close it is refuted by the corpus's own home/away correlation. `RoundResolutionFitLockTests`
> v1.1 corrects its now-stale FAIL comments; **no assertion changed**, and its own tolerance remains a
> regression guard rather than this bar, since the suite has no standard errors.

> **Also August 12, 2026, later still (**`KD-7a` written: the round-resolution model's successor
> distribution is PRE-DECIDED and gated, and deliberately NOT adopted. Landed on the advisory
> recommendation, with one correction the act of writing it surfaced.**) `ERR-030-034` established that
> KD-7's Poisson marginal cannot express the engine's spread; KD-7a now pins **what** the successor
> would be with the same specificity KD-7 applies to `PoissonInverseCdf`, so the next capture decides
> rather than re-litigates. **S1** NB2 as `var = μ(1+αμ)` — not a constant `var/mean` ratio, because
> dispersion measurably rises with the mean and 5 of 22 bucket-sides are mildly *under*-dispersed at
> small λ, which the `1+αμ` form predicts and a ratio does not. **S2** `NegativeBinomialInverseCdf` by
> inversion, pinned by name AND by recurrence, **one uniform per side with the existing home/away
> sub-streams unchanged** — the successor changes the shape of the draw and nothing about how it is
> keyed, so KD-7's order-independent fixed-budget contract survives exactly. **S3** a new
> `[GT] QuickSimDispersion` whose **zero case routes to `PoissonInverseCdf` verbatim** — an explicit
> branch, not a limit: at α = 0 the NB2 recurrence divides by zero, and "identical in the limit" is not
> "bit-identical", so the successor is a strict superset of today. **The correction the writing
> surfaced, and it changed the deliverable: α is NOT determined by this corpus.** The advisory
> recommended initialising α ≈ 0.15–0.25 from a weighted fit; fitting it properly gives **0.0773
> weighted against 0.1552 unweighted — a factor of 2.01 — with ONE 18-sample cell carrying 36% of the
> weighted fit** (bucket −4 away, which happens to have drawn a low sample variance). A variance
> estimate at n = 18 carries ~33% relative error and inverse-variance weights go as `1/var²`, so a
> single unlucky cell dominates. Recording a number would have been false precision, so **S4 records
> the instability instead** and the fitter now emits both estimators, the max single-cell leverage and
> a determined/not-determined verdict on every run — the tripwire is evaluable rather than merely
> written. **S5** records that NB2 does **not** fix the draw deficit and must not be adopted expecting
> it to: measured **26.5%** draws against Poisson's 26.8% and the engine's 19.2%, ~0.3 pp of a 7.6 pp
> gap. **S6** pins **no** successor for the draw deficit, because its mechanism is unestablished — the
> shared-swing family that would cut draws implies negative home/away correlation and the corpus
> refutes that at **+0.044 ± 0.073** (n=198); a Dixon–Coles `ρ` remains the candidate but needs the
> joint scoreline histogram at depth, which is what the newly-committed raw rows preserve. **S7** is
> the adoption tripwire: dispersion still z > 3, α determined, the draw gap still beyond 2·se under
> KD-8's A5, and the capture taken **post-defensive-wiring** — the corpus comes from an engine in which
> *no player has ever made a tackle*, and the second moment of scorelines is exactly what that wiring
> moves. **S8** carries the corrected cost: no save-format bump. Also recorded: today's Poisson model
> draws 26.8% against real football's ~25–26%, i.e. **closer to football than the engine's 19.2% is** —
> an argument the gap may be an engine defect rather than a model defect. No `src/` file touched;
> `ERR-030-034` remains OPEN with its two findings now stated separately.

> **GATE (whole tree, August 12, 2026, post-bar-respec): build 0 errors; `SeasonSave.Tests`
> **402 passed / 0 failed / 3 skipped** and `MatchEngine.Tests` **451 / 1 / 10** — byte-identical to
> this branch's pre-change run.** The single failure is `sim_match_engine_close_chance` at
> `meanCosine = −0.165` (bound −0.16) and `goalwardShare = 0.407` (bound 0.42), the band the owner
> ruled on August 11 to hold red. This landing cannot reach it: the only `src/` edit is comment text in
> `RoundResolutionFitLockTests`, no assertion changed, and `match-engine`'s asmdef does not reference
> `TacticalDirector.SeasonSave` at all. `recurring-defect-lint`: **0 ERROR** (unchanged baseline).

> **Also August 12, 2026 (**Fable advisory review on the two A4a owner decisions — it falsified two
> of my own claims, and both corrections make the decisions cheaper and better-scoped. No decision
> taken; the corpus rows are now committed so neither can be blocked on lost data.**) Consulted on
> ERR-030-033 (the acceptance bar) and ERR-030-034 (the model family). Every load-bearing number below
> was independently reproduced against the committed corpus before being recorded.
> **Correction 1 — "a family change moves persisted season state" is OVERSTATED, verified in code.**
> `SeasonStateCodec` writes a per-fixture `Played` flag and aggregate `LeagueTableRow` totals;
> individual scorelines are folded into the table at resolution and never re-derived from their key,
> so a successor family with no layout change forces no format bump. The decisive evidence is this
> branch's own refit: it changed all three `[GT]`s — every future draw in every save — and
> `SEASON_STATE_FORMAT_VERSION` is still 1. KD-7's pin-by-name protects implementation identity, not
> save layout. **This materially lowers the cost of ERR-030-034.**
> **Correction 2 — ERR-030-034's causal sentence is WRONG and would have misdirected its own fix.**
> "More blowouts and shut-outs and correspondingly fewer draws — the whole of the 7.6 pp miss" does not
> follow: over-dispersion fattens both tails, and 0–0 is a draw. At the fitted bucket-0 lambdas,
> independent negative-binomial at the measured dispersion gives **26.3%** draws against Poisson's
> 26.8% — it closes ~0.5 pp of 7.6. So dispersion and the draw deficit are **substantially independent
> findings**, and the textbook answer to the one does nothing for the other. The only mixed-Poisson
> mechanism that cuts draws materially is a shared antithetic swing, which implies negative home/away
> correlation — and the corpus refutes it: pooled within-bucket correlation **+0.044 ± 0.073** (n=198),
> ~3σ from the ≈ −0.20 such a family predicts.
> ⚠️ CORRECTED August 18, 2026 (reviewed adversarial-review round-7 finding H1): this clause read
> **+0.004 ± 0.052 (n=378), ~4σ**. That figure is reproducible only by pooling the 180 W/D/L depth
> rows into the fit corpus — not the sanctioned invocation `round-resolution-corpus.md` §0.a records —
> and that pooling also flips the α verdict to DETERMINED, contradicting the α figures (0.0773 /
> 0.1552, 36% single-cell leverage) quoted in this same entry. It is therefore decision-relevant to
> KD-7a, not a rounding difference. The sanctioned run prints `pooled within-bucket home/away
> corr = +0.044 +/- 0.073 (n=198)`. The August-18 round-6 pass corrected the same clause in
> `league-bootstrap-design.md`, `open-issues.md`, the roadmap and root `CLAUDE.md` and missed this
> site, which left this file contradicting itself — line 745 above already carries +0.044 ± 0.073.
> The refutation itself is unaffected: both values are far from −0.20, so the shared-swing family
> stays refuted and the draw deficit's mechanism stays unestablished. **The draw deficit's mechanism is therefore NOT
> established.** The over-dispersion half stands and is NOT a pooling artifact — the hostile question
> asked at filing has a clean negative answer, within-bucket `dSquad` spread contributing ≤ 0.005 of
> the ~0.4 excess — but it is better specified as `var = μ(1+αμ)` with α ≈ 0.15–0.25 than as the
> constant 1.395 ratio the ERR quotes, since dispersion rises with the mean and 5 of 22 bucket-sides
> are mildly UNDER-dispersed at small λ.
> **Correction 3 — the deepened rows were nearly lost.** The artifact preserved only their three-number
> W/D/L summary; the 180 rows themselves (~4.5 h of engine time) lived in an ephemeral session
> scratchpad. A joint-distribution decision needs the rows, not the summary. **All 378 raw rows are now
> committed** under `docs/tracking/corpus-data/` with a README recording provenance and the re-fit
> command, so any future family decision costs seconds of compute rather than a re-capture — and, once
> the engine's scoring moves, a re-capture would not reproduce them at all.
> **Measured in passing, and it reframes the whole question:** against a properly-sized mean bar the
> current fit **PASSES** — pooled `χ² = 16.0` on 19 dof against a 30.1 threshold, worst bucket-side
> |z| = 2.06, which is the nominal rate for 22 cells. So the honest verdict composition is
> **mean-agreement PASS, distribution-shape FAIL**, not the flat "FAIL" the artifact currently reads.
> Recorded here; re-specifying the bar is still ERR-030-033's owner decision and nothing was changed.
> No `src/` file touched.

> **Also August 12, 2026 (**A goal-rate match-realism pass that ended at its own premise check: the
> engine has NOT overshot football's goal rate, and the brief claiming it had was mine.**) Run against
> the A4a corpus, so it cost no new engine time. **§0.1's premise check refuted the brief and the pass
> stopped there** — no wiring gate needed, no ladder (KD-W1 freezes it regardless), no `[GT]` moved, no
> `src/` file changed. **The measurement:** 3.09 goals/match is the **grid-weighted** corpus mean, and
> the calibration grid samples `dSquad` −5…+5 **uniformly**. A real 380-fixture season does not: under
> the shipped `StrengthDelta` ramp, |dSquad| ≤ 1 is ~39% of fixtures against the grid's 27%, and
> mismatched fixtures score far more (4.78 goals/match at +5, 3.44 at −4). Re-measured on populations a
> league actually plays: **balanced fixtures (`dSquad ≈ 0`, n=198) give 2.70 ± 0.13 against football's
> ~2.7 — 0.02σ**, and the **league-weighted** rate is **2.93 ± 0.15 (+1.47σ, not significant)**, with
> bucket-to-bucket variation marginal (χ² = 21.8 on 10 dof). So the §5.Z chain did not overshoot; it
> landed on football's rate, and the apparent overshoot was **a calibration grid read as if it were a
> league**. Every document that carried the false claim — `CLAUDE.md`, this file, `open-issues.md`,
> `path-to-playable-roadmap.md`, `file-manifest.md` — is **annotated in place** rather than rewritten,
> per this project's convention for a falsified claim. **The fix is in the tool, not in the prose:**
> `tools/round-resolution-fit.py` now emits all three figures side by side (grid / balanced /
> league-weighted, against the ~2.7 reference) and the artifact carries them as a table, so the corpus
> mean cannot be quoted as a realism number again — the misreading is now structurally hard rather than
> warned against. Two defects in that tool change were caught by running it and fixed: the regeneration
> duplicated the header blockquote inside the generated region, and the `balanced` figure read the
> shallow 18-sample grid bucket instead of the deepened 198 the run had paid for. Regeneration
> re-verified idempotent. **Residual, classified for the next pass (§7): there is no goal-rate lever to
> pull — the number is right.** What remains open on realism is unchanged and sits elsewhere: the foul
> heuristic's ~7 reds per 9 minutes (a Stage-0 placeholder for #44, which has no assembly — measure, do
> not calibrate), and the wiring backlog's 9 dormant capabilities, headline W2 "no player has ever made
> a tackle". **No gate run: no `src/` file was touched** (`git diff --stat` covers `tools/` and `docs/`
> only), and the tool change is verified by executing it.

> **Last Updated (prior):** August 11, 2026, later same day (**Owner call formally recorded: hold
> `sim_match_engine_close_chance` red, do not rebaseline a third time.**) `close-chance-creation-design.md`
> §10.9 (the `DRIBBLE_GOAL_DIR_MIN_MODIFIER` falsifier, v2.1) had already reached a disposition — hold
> red, blame the population C1 changed rather than the locked DRIBBLE mechanism, queue for the KD-W1
> calibration pass — but stated it explicitly as "an owner-facing recommendation, not a decision taken
> here." The owner has now confirmed it: §10.9 gains item 6 (v2.2), recording the confirmation and
> restating the two numbers the band still fails on (`meanCosine` −0.165 against −0.16; `goalwardShare`
> 0.407 against 0.42, both PASS at pre-C1 `ba4e194`) without moving either bound. A predicate already
> rebaselined once (§9 Acceptance-3, −0.10 → −0.16, owner call August 7, 2026) and already deleted once
> under the same rule when it stopped being a lock (§9 Acceptance-1's box predicate) does not get a
> second rebaseline to make it pass here either. This closes the close-chance half of the two-predicate
> "awaiting an owner call" note the match-engine wiring backlog's C1 gate-failure entry left open
> (`open-issues.md`); the `sim_match_engine_shot_outcomes` `fast-balls-deflect-off-bodies` reachability
> predicate in that same note is a separate lock, untouched by this call, and stays open awaiting its
> own. Synced the same commit per the standing cross-reference rule: `match-engine-wiring-backlog.md`
> (Version History → v1.6), `open-issues.md`'s wiring-backlog entry (split into the resolved close-chance
> half and the still-open shot-outcomes half), root `CLAUDE.md`'s mirrored OPEN ISSUES bullet, and
> `file-manifest.md`. **No `[GT]` moved, no code changed, no gate run — documentation only.**

**Last Updated (prior):** August 11, 2026 (**ERR-028-019 — docs-only close-out for #28 Player Progression's
> AR passes 5-8, four consecutive production landings (`39c385a`, `cf5abf0`, `8556ddd`, `b798ce2`) that
> shipped with ZERO `docs/specs/` edits between them — the ERR-028-017 class ("spec+code, same
> commit" failing) recurring twice more, this time across four commits instead of two.** Derived from
> reading each commit's own diff, not the summary that named them. Contract changes now recorded that
> had no normative text anywhere before this pass: the FR-PG-011 id-cursor rule and the M3 club-size
> rule (each enforced at three-to-four boundaries in `src/player-progression/ProgressionSaveCodec.cs`
> and `ProgressionEngine.cs`, previously undocumented in full); the `MAX_DERIVABLE_AGE_YEARS`
> representability-bound constant (Appendix A), whose own value was first set wrong — to a
> football-plausibility 1000 — and corrected same-session after it broke the `BirthWorldDay` field-width
> lock ERR-028-006 bought; the `Encode`/`FromBlocks`-vs-`Decode` exception-type split
> (`ArgumentException` vs `InvalidOperationException`, AR pass 8 M-1), which corrects a now-stale claim
> in #28 §2.3's F8 row; #30's `PlayerCareerStates.RequireBirthWorldDayWithinClock` (AR pass 6 M2(b)),
> live in `src/season-save/` since `cf5abf0` with no `docs/specs/season-competition-loop/` row at all.
> Two behaviour changes stated explicitly rather than silently overwritten: the spend/drain refusal
> clamp moved `POINT_COST - 1` (AR pass 5, itself undocumented) → `0` (AR pass 6, after execution
> falsified the "pending fraction" rationale), and `AbilityModel.DrainOnePoint` gained a failure exit
> (`void` → `bool`) that a save file wedging the day step for ~70 days of CPU with no diagnostic had
> exposed the absence of. **One OPEN hazard recorded, not resolved:** the new
> `CurrentAbility == ComputeCA(attributes, position)` save-gate is keyed on a `[GT]` bias table carrying
> a standing config-loader `TODO` — tuning one cell would make every previously-written save refuse to
> load, permanently, with no migration path under #30 Appendix B.1's F3; not triggerable today (the
> table is a compile-time constant, so stored always equals recomputed at write time), bites at the
> first tune. No tag changed. **Two unrelated hygiene items folded in from this pass's own citation
> sweep:** `CHANGELOG-src.md` v2.113's renumbering-scope claim corrected (two more rows, not just one,
> had an internal citation edited); `spec-error-log.md`'s duplicate `## ERR-008-021` heading (two
> independent write-ups from two concurrent branches, each individually true when written and jointly
> false once merged) reconciled — the entry whose form survived the August 7 merge marked authoritative,
> the other annotated superseded rather than deleted. `docs/specs/player-progression-lifecycle/{section-2.md
> v0.7, section-3.md v0.8, appendices.md v0.6}`, `docs/specs/season-competition-loop/{section-2.md v1.5,
> appendices.md v1.1}`, `spec-error-log.md` v2.13, `CHANGELOG-src.md` v2.114, `file-manifest.md`. **No
> `src/` file touched** — `git status --short` after this pass shows only `docs/` changes.
> `recurring-defect-lint.py --repo .` reports **0 ERROR**. Orientation note: the code review this pass
> was given also named `src/injuries-medical/MedicalSaveCodec.cs` as changed by these four commits;
> `git show --stat` on all four shows it was not, so #41's spec was left untouched.)
>
> **Last Updated (prior):** August 10, 2026 (**#28 Player Progression — T1 + T2a LANDED August 8, 2026, plus
> four adversarial-review passes closing August 10, 2026: ERR-029-006 CLOSED, #30's KD-2 slot 1 LIVE,
> and the career roster MOVED OFF THE WORLD SEED — but the loop found the landing genuinely broken and
> this record is that repair, not a formality.** **The landing itself (August 8):** `ProgressionEngine`
> is the KD-7 sole writer of `[1,20]` attribute growth; `SeasonLoop` gathers #29's training-input batch
> through `PlayerCareerStates.GatherTrainingInputs` and drives it through the FR-PG-021 batch
> `AdvanceDay`. **KD-4** is the load-bearing decision: `Squad` is immutable and `League` was rebuilt
> from the world seed at every load, so evolving attributes had nowhere to persist; #28's block is now
> the serialized roster and `ProgressionSquads` is the sole provider every consumer reads through — one
> authority at every moment, closing the two-authorities shape the #29/#41 T2 loop's H2 filed. This
> **retires roadmap A3's seed-rebuildable-roster property** (`SavedWorldSeed_RebuildsTheSameLeague`
> narrowed to the half that survives — generation is still seed-pure — not deleted).
> `SEASON_SAVE_FORMAT_VERSION` **4 → 5**. **No draw site at all** — no stream registration, no `0x20`
> promotion, no digest or snapshot-schema question — new-game `PotentialAbility` is a deterministic
> `[GT]` placeholder (owner's call: it is #47 authored data). Four ERRs filed AND resolved the same
> commit: **ERR-028-003** (new-game PA had no derivation anywhere; recorded-not-fixed that a whole
> youth career only moves CA ~421/10,000, so the PA ceiling is decorative regardless of PA's source —
> a growth-RATE property Stage-3 owns); **ERR-028-004** (§3.5 specified the block version-first with
> the RNG domain tag as identifier, the ERR-029-005/ERR-041-009 MUST arriving in a third spec; now
> magic-led, `ProgressionBlock` typed); **ERR-028-005** (§5.2's keystone lock was unsatisfiable as
> worded and §3.1 carried no per-day cursor while #30 runs a fixture day's slots twice, ERR-030-027 —
> a wired #28 would have double-accrued growth every fixture day; fixed with `LastAdvancedWorldDay`,
> sentinel `uint.MaxValue`, idempotent-per-day and gap-complete); **ERR-030-030** (five stale #28-null-
> seam sites + the v4 frame description). Landing suites: `PlayerProgression.Tests` 26 → 41,
> `SeasonSave.Tests` 356/0/3.
>
> **The review loop that followed is the reason this entry exists, and it found the landing broken in
> production while every test stayed green — four passes, closing 0 High by pass 4.** **AR pass 1**
> (4 High, 7 Medium, 8 Low, all fixed) found the headline defects by executing probes against the
> built assemblies: **ERR-028-006** — a new world starts on day 0, so `BirthWorldDay` (`uint`)
> underflowed for every non-zero-age player and was clamped to 0, reading the **entire league as age 0**
> after one advance (bands `growth=100 stable=0 decline=0`); both #28 fixtures used `BaseDay=100000`
> specifically to avoid the day the product actually starts on — the ERR-030-014 shape again. Fixed:
> `BirthWorldDay` → signed `long`, both clamps deleted. **ERR-028-007** — the new fourth persisted
> per-player cursor was checked at none of the three boundaries the #29/#41 loop spent passes 5/6/9
> establishing; a cursor 9,999 days ahead was accepted at composition, Save and Load, silently freezing
> growth. **ERR-028-008** — `Save`'s `?? ProgressionEngine.Empty` let a resume that dropped the store
> overwrite a populated 4-club roster with a zero-club one; fixed at the destination (`Save` now refuses
> an empty store overwriting a populated block) after the reviewer's first-choice fix broke four
> legitimate pre-#28 suites. **ERR-028-010** — a progression-wired `SeasonLoop` could not play a round
> through any public API at all (the constructor's provider was private, the public overload demanded
> reference-equality nothing exposed); fixed with a parameterless `AdvanceAndPlayNextRound()` resolving
> through the loop's own store. Plus ERR-028-009 (Medium, no F8 sentinel guard — `AdvanceDay(uint.MaxValue)`
> stored and a gap-replay loop never terminated) and ERR-028-011/-012 (Medium/Low: cross-club duplicate
> ids Encode wrote and Restore refused; a stale cursor defeating FR-PG-011; no range gates on decoded
> attributes; two records in this landing's own change history were wrong — six changed files carried
> no version row and `SeasonLoop.cs`'s claimed version chain never existed).
>
> **AR pass 2** (ERR-028-013, High+Medium; ERR-028-014, Medium; plus a 33-guard mutation audit that
> found 15 fixes with no test able to fail if reverted, all locked): the `SeasonLoop` constructor had
> conflated "a progression store was supplied" with "#28 is the roster authority" — an empty store
> (the honest pre-#28 composition `SeasonSaveManager.Save` itself documents) could be composed but not
> resumed through any documented path, because nothing anywhere had ever reconstructed a `SeasonLoop`
> from `Load` output. Fixed with one `progressionIsRoster` predicate driving every gate. **ERR-028-014**
> is the diagnosis worth keeping: the never-advanced cursor sentinel was exempted from the cursor-vs-
> clock rule as a **sibling-copy error** — the exemption is sound for #29/#41, whose fresh states carry
> no clock-anchored quantity, and wrong for #28, the only one of the four whose fresh state (age, from
> `BirthWorldDay`) means something different at every clock value; a day-0 store composed against a
> day-3650 clock banked one day of growth for a decade, silently. Fixed by deleting the special case
> (`SeedFrom` anchors the cursor at the seed day) rather than adding a gate; two tests that had been
> locking the defect as intended behaviour were **inverted**, not adjusted. Also fixed
> `tools/recurring-defect-lint.py`, which had `TODAY` hardcoded to its own authoring date and was
> producing false ERRORs every day since.
>
> **AR pass 3** (ERR-030-031, High — doc sweep; ERR-028-015, High×2 — code, both introduced by pass 2):
> the ERR-028-014 sweep had stopped at its own spec folder — the **fifth** recurrence of the grep-
> boundary widening class — leaving #30 §2.3's F8 row and Appendix B.1 asserting "all three" cursors
> and a blanket sentinel exemption ERR-028-014 had made false the same day, and #28 §5.1 still
> documenting the retired behaviour for a test that had been renamed and inverted. **ERR-028-015** is
> the sharper finding: anchoring the cursor at the seed day made `AdvanceDay(seedDay, …)` a total no-op,
> and mutation testing — not the static audit — proved three locks had gone silently unguarded (deleting
> the idempotency guard outright left **all 469 tests green**; deleting the retirement-age comparison
> left 85/85 green); and the ERR-028-013 relaxation had reopened the ERR-028-010 gate by breaking the
> biconditional it was keyed on (`_career` vs `_careerSquads`), letting a progression-only loop skip the
> two-provider refusal entirely — verbatim the ERR-028-010 shape, in the fix that cites it. Rekeyed to
> the authority the loop owns.
>
> **AR pass 4** (ERR-028-016, 0 High, Medium×4 — the loop's first pass with no High): the headline is a
> correction to **pass 3's own comment** — pass 3 attributed a guard's load-bearing property to backward-
> call cursor regression when the `if` condition already prevents that; the guard actually prevents the
> §3.4 retirement evaluation from running on a no-op call, and pass 3's rewrite had also discarded the
> original comment's correct half. Plus three decode range checks tested only one side of a two-sided OR
> (the same half-guard shape pass 2's sweep found), and five more guards with no isolating test at all.
> Suite: `PlayerProgression.Tests` 89 → **100**.
>
> **What remains open of #28, unchanged by the loop:** the season boundary (retiree removal + 1:1
> regen) and the `player-progression.regen` stream it needs, plus T3's deep growth curve — the loop
> found and fixed defects in what landed; it did not extend the landing's scope.
>
> **Verified 2026-08-10 at this branch's HEAD** (this session's own runs): whole tree builds **0 errors**;
> `PlayerProgression.Tests` **100 passed / 0 failed / 0 skipped**; `SeasonSave.Tests` **385 passed / 0
> failed / 3 skipped**; `tools/recurring-defect-lint.py --repo .` reports **0 ERROR** tree-wide. **Status
> honestly: this branch is mid-review-loop, not closed out** — nothing here has been squared against a
> whole-tree `MatchEngine.Tests` run on this exact tree. The last whole-tree gate this branch's own AR
> passes report is pass-3's PASS at `9392839` (`MatchEngine.Tests` 436/0/10, quarantine empty; pass 4
> did not touch the match engine). Separately, **CI is currently RED on `sim_match_engine_close_chance`**
> — this reproduces identically on `main` at the same predicate values (the ERR-008-021/-022/-023 chain's
> recorded, not-yet-recalibrated residual, per the entries above) and is **not caused by this branch**,
> which touches no match-engine or decision-tree file.)
>
> **Last Updated (prior):** August 9, 2026, later still (**AR over the ERR-010-002 landing — 1 High, 6 Medium, 4 Low found, all fixed across two commits (`48977fa` doc half, `d93e0c8` code half), and the whole-tree gate result for both.** Doc half: §3.5.1's stale "bounded to the hemisphere the ball can physically reach" spec text corrected to match the code, which provably never enters that branch; `GkHeadingIntentSource.HeaderAimTarget`'s phantom `§4.2a` citation resolved to `gk-heading-engine-integration-design.md`, recording two measured limitations (the ballistic solve's ≈15 m range ceiling, since the target is always pinned to the goal line; the wide-clearance lateral bias, weak and inverted — a team-0 header at (10,10) aims 4.1° off straight upfield, one at (10,34) aims 17.9°, the wrong direction); version rows added to three production files that had shipped substantive changes with none. Code half: **(1)** the out-of-range fallback in the ballistic aim solve returned a flat 45° "maximum-range launch," which is only correct when the target sits at contact height — a header contacts near 2.3 m aiming at ground-level targets, so this was the production path (measured 9.98° error at the boundary, 4.38° at the production nominal speed), fixed to the true `tan(θ) = v / √(v² − 2·g·dz)` solve with a reachability guard, retiring the `[DERIVED]` `MaxRangeLaunchComponent` constant whose name asserted what it was not. **(2)** `ComputeAimNormal` did not propagate a degenerate desired direction, so a zero `aimDir` reflected the ball straight back at full power — the maximum possible deflection, arrived at through the branch documented as the natural rebound, and it made the zero-aim fallback's own lock pass against a branch production could never enter. **(3)** Four missing ERR-008-002 home/away locks added for `HeaderAimTarget`, the landing's only team-branching geometry — a mislabelled existing test ran team-agnostic code and could not have caught an asymmetry; the mirror itself was already correct. **One bug was introduced by the fix and caught by its own new lock before landing**: the unreachable-height guard first returned `Vector3.up` (Unity's +Y) instead of this project's +Z up axis (Ball Physics #1 §1.2) — the coordinate-axis trap in this file's own hazard table, and it still nearly shipped. **Whole-tree gate, local run, head `d93e0c8`: build 0 errors, 3 warnings; `GATE_EXIT=1` — did NOT print "Gate PASSED".** Sole failure: `sim_match_engine_close_chance`, 2 of 3 predicates (`final-third-dribbles-are-not-goal-averse` meanCosine −0.165 vs bound −0.16; `goalward-dribbles-are-not-a-minority-of-one-in-three` goalwardShare 0.407 vs bound 0.42) — the inherited C1 failure, identical to three decimals against the `589a011` baseline, so this landing moved nothing. `MatchEngine.Tests` **451 passed / 1 failed / 10 skipped / 462 total** (447/1/10/458 baseline, the +4 exactly the new `HeaderAimTarget` locks); `HeadingMechanics.Tests` **63 passed / 15 skipped / 0 failed** (60 → 63); all 31 other suites green, quarantine empty; `python3 tools/recurring-defect-lint.py --repo .`: 0 ERRORs. `spec-error-log.md` v2.04, `file-manifest.md`. Also landed: `close-chance-creation-design.md` §10.8 — the §10.7-corrected instrument's first execution (Report C5b, 6 seeds × 90 min, 1,081 aerial final-third episodes) finds headers failing horizontally (nearest attacker 5.88–5.93 m, nearest defender 4.28–4.93 m in pure XY, no height term at all) and RE-RANKS §10.6's withdrawn "attack the ball" lever back to first, on a corrected instrument reaching the same conclusion (0% within contact distance in true 3-D) by a route that does not carry §10.7's height-floor artifact. Prior entry below.)
>
> **Last Updated (prior):** August 9, 2026, later same day (**ERR-010-002 — Heading Mechanics #10 §3.5 delegated header aim to Decision Tree #8, which cannot emit a header at all.** `ActionType` ordinal 8 overflows the 3-bit composure-noise field (wiring backlog W9), so the aim decision had no owner, `HeaderIntent.TargetIntent` reached no formula anywhere, and every header was a **passive specular mirror**: the ball left the head along the reflection of its own incoming path with zero player influence on direction — the `ERR-011-010` shape, a decision delegated to a system that structurally cannot make it. Two further defects in the same chain: the contact point had **two independent derivations** (`HeadingMechanics.Update` Pass 1 and Pass 2, agreeing only by coincidence — the parallel-surface trap), and Pass 2 rebuilt the world-space point from its **2-D** head-local projection, pinning `contactPointActual.z` to the head centre, so the §3.5 reflection normal was permanently horizontal and `reflected.z = v̂_in.z` — **a descending ball was headed further down and no header could lift the ball**. **Resolved by new #10 §3.5.1 + `src/heading-mechanics/HeadingAim.cs`** (+ `Tests/HeadingAimTests.cs`): (1) a ballistic launch-direction solve to `TargetIntent` at the perfect-contact speed, low root, with a continuous 45° maximum-range fallback when the target is out of range (P1); (2) the reflecting half-vector normal — with a recorded note that NO geometric hemisphere bound is applied because it provably can never fire, this project's "guard on an unreachable branch" defect class; (3) an achieved normal blended from the geometric normal by normalised Heading, where **steer authority 0 is exactly the pre-fix behaviour** and the ramp spans the whole attribute range with no plateau (the `ERR-008-019` FULL-RANGE shape, P2). One `ResolveContactGeometry` owner now read by both passes; the 3-D contact point carried directly instead of round-tripped. Producer half: new `GkHeadingIntentSource.HeaderAimTarget` (§4.2a) — clear wide when deep, aim at goal when advanced, continuous lerp in the taker's advancement, constant-free. **No new `[GT]`** (the Heading attribute is the dial, so inside KD-W1), **no `SNAPSHOT_SCHEMA_VERSION` bump** (both intent fields were already serialized), **no new RNG stream / domain tag / draw site / draw-order change**. Digests DO move for any match containing a header because contact counts change and `HeadingContactQuality` draws twice per contact. Also landed: `close-chance-creation-design.md` **§10.7**, which corrects §10.6 twice — item 3's consequence was wrong (the aim was inert, not merely fixed), and **§10.6's proximity census is an instrument artifact**: `BallToAgentDistance3D` measured ball-to-agent-GROUND distance including the ball's full height while the episode gate requires ball z > 0.5 m, so its two smallest buckets were structurally unreachable and the published "0%" in both was the instrument reporting its own gate. §10.6's ranking of "attack the ball" as the first lever is **withdrawn, not replaced**. The instrument was corrected in `src/match-engine/tests/CloseChanceDiagnosticTests.cs` (v1.4): three separate series now — horizontal-only separation, 3-D distance to the agent's head point, and the retained ball-to-ground measure explicitly relabelled as carrying a ≥ ball-height floor — plus the ball height at the moment of minimum horizontal separation. **New files (2 + metas):** `src/heading-mechanics/HeadingAim.cs`, `src/heading-mechanics/Tests/HeadingAimTests.cs`. **Modified:** `src/heading-mechanics/HeadingMechanics.cs`, `src/heading-mechanics/HeadingMechanicsConstants.cs` (+ `KINEMATIC_TWO_COEFF`/`PERFECT_CONTACT_QUALITY` [FIXED], `SurfaceNormalEpsilon`/`MaxRangeLaunchComponent` [DERIVED]), `src/match-engine/GkHeadingIntentSource.cs`, `src/match-engine/MatchEngine.cs`, `src/match-engine/tests/CloseChanceDiagnosticTests.cs` (v1.4). **Specs:** `docs/specs/heading-mechanics/section-2.md` v0.4 (new FR-HE-036/037/038), `docs/specs/heading-mechanics/section-3.md` v0.4 (new §3.5.1). **Tracking:** `spec-error-log.md` v2.00 (ERR-010-002), `close-chance-creation-design.md` v1.6, `match-engine-wiring-backlog.md` v1.4, both changelogs, `file-manifest.md`. ****GATE-VERIFIED** (local whole-tree run, head `c89c838`): build 0 errors; `HeadingMechanics.Tests` **60 / 15 skipped / 0 failed** (47 → 60, the +13 being this landing's `HeadingAimTests` locks, all executed); `MatchEngine.Tests` **447 / 1 / 10 — byte-identical to the pre-fix baseline**, the one failure being the inherited C1 `sim_match_engine_close_chance` (meanCosine −0.165, goalwardShare 0.407, unchanged to three decimals) that predates this branch and awaits an owner call; all 33 suites otherwise unchanged, quarantine empty. **The "digests DO move" claim written at landing is WITHDRAWN as stated** — no measured digest movement anywhere. A match containing an executed header would digest differently; **no scenario in this tree contains one**, which is the 0.2% contact ratio (2 executed / 963 failed over 6 seeds × 90 min) showing up exactly where the evidence advisor predicted it would. The aim is locked by unit geometry and by nothing else.** — a whole-tree gate is running elsewhere; result to be recorded separately. Prior entry below.)
>
> **Last Updated (prior):** August 8, 2026, later same day (**WIRING BACKLOG C1 — the #12 `InPoss` gate. `ERR-012-011` filed and RESOLVED, spec + code, same commit.** #12 §3.0.2 classified the tactical phase from the ON-BALL CARRIER. The engine clears `_possessingAgentId` at every `ApplyKick` and re-acquires it only on physical receipt, so for the entire flight of every pass the snapshot read "loose ball" and §3.0.2's ball-velocity branch classified a team knocking the ball around as being in **transition**. Spec and code were each self-consistent; "who is on the ball" and "which team has the ball" are different questions and only the first had ever been asked. Phase now classifies from **TEAM possession** — the football possession-sequence convention, made normative in FR-PA-022: a team is in possession while a player is on the ball AND while a ball it played is travelling to a team-mate; a ball played to no one (a shot, a ball no longer going to its intended receiver) is not. The orchestrator composes it over a new `_passInFlightReceiverId` latch, armed at the CONTACT kick from the `PassRequest.TargetAgentId` the executor already holds and expiring on possession, any ball strike, any restart, receiver inactivity, or the ball ceasing to approach him — that last rule **reuses `RunFirstTouch`'s own receding predicate**, hoisted to one shared `BallApproaching`, so there is **no new `[GT]` and no timeout** and the landing stays inside the KD-W1 freeze. Snapshot fields were **ADDED, not redefined**: redefining `PossessionOwnerEntityId` would have excluded the intended RECEIVER from #23's dismark nudge (FR-DM-007) for the whole flight of every pass — the one player who most needs to move to receive. New §3.0.5 worked example walks a pass tick by tick: the settled-possession-with-a-moving-ball case the section had no example of, which is how this survived. `SNAPSHOT_SCHEMA_VERSION` **19 → 20** (the latch is cross-tick and NOT reconstructible — `PassExecutor` never clears its `_request` on the return to Idle, so all 22 serialized executor states carry a stale last-pass target that nothing dates); no new RNG stream / domain tag / draw site / draw-order change. **Measured, 6 seeds × 90 min: final-third `InPoss` 7.5% → 40.8%, possession-phase share 24.2% → 96.8%**, TransToAtk 58.9% → 3.1%, away mirror clean (41/56/3/0 against 56/41/0/3). Two facts measured for the first time: the engine holds **no on-ball possessor on 86.0%** of final-third samples and a pass is in flight on **77.0%**. **THE RESULT WORTH KEEPING IS NOT THE GATE — IT IS THAT THE BACKLOG'S OWN RATIONALE FOR C1 WAS WRONG, AND THE COUNCIL CAUGHT IT BEFORE A LINE WAS WRITTEN.** "Unblocks phase-gated behaviour across #13/#14/#15" is false: `TacticalContext.HasAttackIntent` is written by the engine every stride and **read by no production code anywhere**, so #15 is inert independent of its gate; #13's press target positions have no consumer outside `pressing-ai`, so more-correct pressing still steers nobody. The one large lever C1 does pull is #12's own `PullFactor` table, whose `InPoss` column is **less advanced** than the `TransToAtk` column it replaces for every attacking role (ST 0.60 vs 0.75) — and the predicted regression **measured**: deepest composed slot **23.0 → 25.7 m** from goal, mean attackers in the box **0.04 → 0.02**, slot-in-box 3% → 1%, dribble cosine −0.03 → −0.180; shots 16 → 19/match, goals 2.83 → 3.33. C1's value is a **correct phase label** plus an `InPoss` column exercisable for the first time — a precondition for the KD-W1 calibration pass, which now inherits a differently-shaped table. It is **not** the creation fix; that remains C4. Two new Class-A wiring-backlog items fell out of the investigation: **C5** (`HasAttackIntent` has no reader — the second and larger lock on #15's door, missed by the v1.0 audit because its method counts methods with no caller, not fields with no reader) and **C6** (`GkHeadingWorldAdapter.ApplyKick` is unreachable from any test). Acceptance: `match-engine-inposs-gate` (#19 ScenarioRunner, Tier B, 2 seeds × 90 min), **proven to FAIL at the pre-fix commit `ba4e194` by executing it in a worktree** — both predicates read 0.256 against a 0.70 bound. It pins **no** goal, shot, box-occupancy or dribble figure, deliberately: every shape metric moved the wrong way and pinning one would encode a regression as a contract. 12 new `PassInFlightPossessionTests` locks, mutation-tested — reverting the classifier kills both mirrored cases, deleting the v20 field kills the round trip, deleting the shot adapter's clear kills its isolating case. **Recorded, not fixed:** the GK/heading and `ApplyRestart` clears have no isolating lock; the restart one is benign, the GK/heading one is C6. **GATE: FAILED — executed locally, whole tree, and the failure is C1's own doing.** Build 0 errors, quarantine empty, every suite green except `MatchEngine.Tests` **446 passed / 2 failed / 10 skipped** (53 m 26 s; 446 → 458 total with this landing's 12 locks plus the new scenario, which PASSED). `sim_match_engine_close_chance` fails both its ERR-008-018 predicates (`meanCosine` **-0.165** vs the -0.16 bound; `goalwardShare` **0.407** vs 0.42) and `sim_match_engine_shot_outcomes` fails `fast-balls-deflect-off-bodies` (`totalDeflections > 0`, read **0**). **Both PASS at the pre-fix commit `ba4e194`, verified by executing them in a worktree - so C1 caused both, and that is measured, not inferred.** **Neither bound has been moved.** The close-chance pair misses by 0.005 and 0.013 on a band already rebaselined twice this fortnight, and the council's standing note applies: a predicate rebaselined a third time has stopped being a lock. The shot-outcomes one is not a band at all - it is ERR-003-007's REACHABILITY lock, and C1 drove it to exactly zero, meaning no fast ball found a body anywhere in the corpus. That is consistent with the measured shape compression (deepest slot 23.0 -> 25.7 m) and is a new fact about the engine, not a threshold to retune. Both are filed for the owner with the mechanism named. Prior entry below.)
>
> **Last Updated (prior):** August 8, 2026, convergence entry (**THE BALANCE-PASS ADVERSARIAL REVIEW LOOP CONVERGED — pass 16: "no new High or Medium findings."** Three mechanical Lows fixed at the convergence commit (the future dates breaking the lint baseline; the assignment ceiling's first lock — a mutant erasing it had left the suite green; the last "floored at 1" clause). Final ledger: 16 passes, 13 consecutive whole-tree gate PASSES, 9 ERR ids filed AND resolved, the FR-MD-027 dial ARMED and measured in the football band, the recurring-defect lint mechanizing four defect classes, the 275-error tree-wide backlog filed for the owner. `spec-error-log.md` v1.96. **Pass-15 gate: PASSED — thirteenth consecutive.** Final gate over the convergence commit: **PASSED** (the fourteenth consecutive whole-tree verdict, closing the chain) — quarantine empty; `MatchEngine.Tests` 436/0/10 (27 m 46 s), `SeasonSave.Tests` 356/0/3, `InjuriesMedical.Tests` 67/67 (the ceiling assert live in T-MD-MOD-002), `TrainingSystem.Tests` 52/52 over the convergence commit; on its PASS the #29/#41 balance-pass chain is complete, with #30's slot-1 (ERR-029-006, blocked on #28) the one remaining seam.)
>
> **Last Updated (prior):** August 8, 2026, second final entry (**Balance-pass adversarial review pass 15: 0 High, 2 Medium, 2 Low, all fixed — both Mediums inside the pass-14 fix.** The moved guard fired AFTER Severity was written (the step's one partial-write throw site — the refusal itself wrote the breach it named; branch now atomic, prevention claims corrected); §3.1's assignment never had the RECOVERY_MAX ceiling the code applies (spec-only). Lows: a nested `<para>`; two src-tree annotations. Pass 14's dead-branch claim CONFIRMED by exhaustive enumeration; AppearanceWindow 0 mismatches over ~6.3M modelled reads; all four hunting sweeps clean. Verified: build 0 errors, 356/0/3, 67/67, 52/52. `spec-error-log.md` v1.95. **Pass-14 gate: PASSED — twelfth consecutive verdict.** Pass-15 gate: **PASSED** (thirteenth consecutive verdict) — whole tree, quarantine empty; `MatchEngine.Tests` 436/0/10 (28 m 5 s), `SeasonSave.Tests` 356/0/3, `InjuriesMedical.Tests` 67/67, `TrainingSystem.Tests` 52/52. AR pass 16 reviews a two-file delta; the loop ends only when a pass returns no new High/Medium.)
>
> **Last Updated (prior):** August 8, 2026, past midnight (**Balance-pass adversarial review pass 14 — the lint-armed pass: 0 High, 1 Medium, 4 Low, all fixed.** The Medium: pass 13's `RecoveryMax` guard sat on a branch `ValidateState` makes provably unreachable under any config, while the breach it names happens on the mutually exclusive draw branch — demonstrated by model, moved to `AssignRecoveryDays`, the falsified claims annotated. Lows: the codec's non-gate rationale inherited #29's clamp claim (#41 refuses — asymmetry stated); ERR-029-008's sweep was three of seven (completed); the last unsanctioned key spelling in #27's row (annotated; tool-scope decision recorded); pass 13 M3's own leftover 1 → 2 line. The negative results now matter: tautology, unexecuted-branch and parallel-surface sweeps all CLEAN; lint surface clean. Verified: build 0 errors, 356/0/3, 67/67, 52/52. `spec-error-log.md` v1.94. Pass-14 gate: **PASSED** (twelfth consecutive verdict) — whole tree, quarantine empty; `MatchEngine.Tests` 436/0/10 (27 m 51 s), `SeasonSave.Tests` 356/0/3, `InjuriesMedical.Tests` 67/67, `TrainingSystem.Tests` 52/52. AR pass 15 launches with it; the loop ends only when a pass returns no new High/Medium.)
>
> **Last Updated (prior):** August 8, 2026, final entry of the day (**Balance-pass adversarial review pass 13: 0 High, 4 Medium, 6 Low, all fixed — three Mediums pass-12 fixes incomplete at their own class boundary, the fourth (#30 §4) an axis untouched in twelve passes.** Pass 12's "the one unmirrored `[GT]`" was false twice (`RecoveryMax`, `InjuryRiskMax`'s zero side — both guarded, claims annotated); ERR-029-008 — #29 still published the pre-T0-AR `SetFocus` shape the T0 High deleted; #30 §4's THIRD signature copy deleted in favour of Appendix B; #30's catalogue said frame v2 beside an appendix describing v4, and the bitmask bound was in no spec. Lows: the 4-tuple spelling sanctioned; stale counts; the rowless class inside its own fix (sixth); #30's outline; T-SN-DET-004. Verified: build 0 errors, 356/0/3, 67/67, 52/52. `spec-error-log.md` v1.93 (+ ERR-029-008). **Pass-12 gate: PASSED — tenth consecutive verdict.** Pass-13 gate: **PASSED** (eleventh consecutive verdict) — whole tree, quarantine empty; `MatchEngine.Tests` 436/0/10 (28 m 10 s), `SeasonSave.Tests` 356/0/3, `InjuriesMedical.Tests` 67/67, `TrainingSystem.Tests` 52/52. AR pass 14 launches with it, consuming the new `tools/recurring-defect-lint.py` sweep; the loop ends only when a pass returns no new High/Medium.)
>
> **Last Updated (prior):** August 8, 2026, last entry of the day (**Balance-pass adversarial review pass 12: 0 High, 4 Medium, 5 Low, all fixed — run with the generator-class brief; every Medium a class-mate of a pass-11 fix.** FR-SN-034 still mandated #29/#41 as null seams; §2.2 knew nothing of the career/appearance types; `RecoveryDaysPerTickBase` was the one `[GT]` with a lock and no runtime guard (at 0 every injury is permanent, silently — guarded, fourth instance of the posture); and **ERR-030-029**: the depleted-squad back-fill rule existed in no spec while #36 explicitly waited on the seam to settle it — #30 §3.4 owns it now, terminal refusal F9, #36 pointed at it. Lows: the framing helper's third caller; the phantom-stream sweep's fifth widening (into `src/`); the draw key's canonical spelling pinned with sanctioned abbreviations; two missing `Modified:` headers; `AdvanceDays` + the roll refusal into FR-SN-032/F5. Verified: build 0 errors, 356/0/3, 67/67, 52/52. `spec-error-log.md` v1.92 (+ the ERR-030-029 row). **Pass-11 gate: PASSED — ninth consecutive verdict.** Pass-12 gate: **PASSED** (tenth consecutive verdict) — whole tree, quarantine empty; `MatchEngine.Tests` 436/0/10 (29 m), `SeasonSave.Tests` 356/0/3, `InjuriesMedical.Tests` 67/67, `TrainingSystem.Tests` 52/52. AR pass 13 launches with it; the loop ends only when a pass returns no new High/Medium.)
>
> **Last Updated (prior):** August 8, 2026, even later same day (**Balance-pass adversarial review pass 11: 0 High, 2 Medium, 4 Low, all fixed — both Mediums stale-spec debt on #30 §2.** FR-SN-013 still declared the availability seam "empty until #44 T2" (LIVE since T2 — the pass-5 §3.4 correction stopped one section short of the FR an implementer reads first; rewritten, §2 v1.0 with FR-SN-021 refreshed three landings forward). The composition-pairing and cursor-vs-clock refusals — three boundaries, two directions, three cursor kinds — had one appendix sentence as their entire normative source: new F7/F8 rows + Appendix B in full (v0.7). Lows: pass 10's renumber had itself reused v0.4 (#41 §9 → v0.5; #29 §2's rowless reorder → v0.7); the citation contract widened to FR-or-section; the severity guard now mirrors ALL three lock predicates (a negative numerator silently deleted its tier — non-negativity added, zero legal); the draw key spelled one way. Verified: build 0 errors, 356/0/3, 67/67, 52/52. `spec-error-log.md` v1.91. **Pass-10 gate: PASSED — the eighth consecutive verdict** (32 suites, quarantine empty, 436/0/10), superseding the invalidated pass-9 run. Pass-11 gate: **PASSED** (ninth consecutive verdict) — whole tree, quarantine empty; `MatchEngine.Tests` 436/0/10 (28 m 31 s), `SeasonSave.Tests` 356/0/3, `InjuriesMedical.Tests` 67/67, `TrainingSystem.Tests` 52/52. AR pass 12 launches with it; the loop ends only when a pass returns no new High/Medium.)
>
> **Last Updated (prior):** August 8, 2026, still later same day (**Balance-pass adversarial review pass 10: 0 High, 1 Medium, 6 Low, all fixed — the Medium is ERR-041-003's High recurring, measured.** The severity-split invariant was the only #41 catalogue invariant with no production guard — its `[GT]` numerators are config keys, the only lock runs config-unbound, and a config at 600+400 silently deletes the `Serious` tier (measured: Serious = 0 over the whole range; ~10% of injuries become 21-day Moderates, invisible to the instrument's severity-blind bands). Fixed as the third instance of the draw-site guard posture: `ClassifySeverityFromDraw` fail-louds at the classifying site; §3.2 + Appendix A carry the enforcement; the lock's comment records the two-layer split. Lows: pass 9's dangling header fragment; the v2.93 row inserted against the add-at-top rule; both outlines' stale ranges (#29's two landings behind); T-MD-DET-010/T-TR-DET-006 assigned to the existing F8 locks; #41 §9's duplicate 0.2 rows renumbered + #29 §2's table reordered; `AppearanceSaveCodec`'s refusals cite #30 Appendix B.1 instead of a sibling spec's FR. Suites: 356/0/3, 67/67, 52/52, build 0 errors. `spec-error-log.md` v1.90. **The pass-9 gate was INVALIDATED** — a pass-10 build ran under its test phase (process error, recorded); superseded by the pass-10 gate at HEAD. Pass-10 gate: **PASSED** (eighth consecutive verdict) — whole tree, 32 suites, quarantine empty; `MatchEngine.Tests` 436/0/10 (29 m 3 s), `SeasonSave.Tests` 356/0/3, `InjuriesMedical.Tests` 67/67, `TrainingSystem.Tests` 52/52 — the marker in the pass-9 entry below refers to this same run. AR pass 11 launches with it; the loop ends only when a pass returns no new High/Medium.)
>
> **Last Updated (prior):** August 8, 2026, later same day (**Balance-pass adversarial review pass 9: 0 High, 2 Medium, 5 Low, all fixed — both Mediums the loop's own recurring classes.** The cursor-vs-clock invariant was TWO hand-copied walks (the parallel-surface class pass 4 M1 resolved for the sibling gate one pass before this duplicate was written), and the file-boundary copy's medical-lag predicate had NO isolating case — deleting it left the suite green; collapsed to one owner on `PlayerCareerStates` with both boundaries delegating, plus the medical-only-lag Save case, and the post-fix mutant now fails BOTH boundaries' locks. The ERR-041-012 sweep's FOURTH residue was inside files the previous widening bumped: #41 §9.1's gate, §9.6's Decision (still ratifying "stream registration" at T2), §8.2 citing the anti-phantom FR as authority FOR the phantom, the LIVE research supplement twice — and the mandated repo-wide grep caught a fifth site (the 0x2A catalogue doc itself); frozen-by-design exclusions now recorded in the ERR row. Lows: pass 8's #16/SPEC_INDEX back-props shipped rowless against an explicit "with version rows" claim (fifth consecutive FR-CS-057 recurrence — v1.0.15 + the header bump added, the claim corrected); four tracking headers dated tomorrow; the frame doc's missing sixth cref; new F8 (sentinel-as-worldDay) specced at BOTH #41 and the #29 sibling in one commit; the severity-split invariant strict. Suites: 356/0/3, 67/67, 52/52. `spec-error-log.md` v1.89. **Pass-8 gate: PASSED — the seventh consecutive** (`MatchEngine.Tests` 436/0/10 in 30 m 36 s, quarantine empty). Pass-9 gate: **INVALIDATED, not failed** — a pass-10 fix build was run while the gate's test phase was still executing (a process error against this loop's own hold-builds rule), swapping binaries under the unfinished suites, so the run would have mixed two trees; stopped at 31 suites (all green to that point, `SeasonSave.Tests` 356/0/3 among them) and superseded by the pass-10 gate at HEAD, whose tree strictly contains pass 9's. Pass-10 gate: **PASSED** (eighth consecutive verdict) — whole tree, 32 suites, quarantine empty; `MatchEngine.Tests` 436/0/10 (29 m 3 s), `SeasonSave.Tests` 356/0/3, `InjuriesMedical.Tests` 67/67, `TrainingSystem.Tests` 52/52. AR pass 10 launches with the gate; the loop ends only when a pass returns no new High/Medium.)
>
> **Last Updated (prior):** August 8, 2026 (**Balance-pass adversarial review pass 8: 0 High, 2 Medium, 6 Low, all fixed — both Mediums again completions-at-a-boundary, demonstrated.** Pass 7's predicate-isolation claim was false for the two FIRST-evaluated predicates (both-cursors-together shadowed the training branch in both directions; deleting it left the suite green) — five-for-five now, with (a2)/(c3). And the ERR-041-012 sweep was folder-scoped: the phantom stream survived in #16 §3.4's own allocation row ("code const + registration land at T2" — both halves false), three #40 lines including a factual "does register a stream" comparator, and SPEC_INDEX — the sweep's third widening, each stopped at a grep boundary. Lows: the rowless v0.6; two tables reordered (one reading DESCENDING); two stale parentheticals; two phantom-name residuals; a paragraph un-fused; the instrument's log constant. Suites: 67/67, 52/52, 356/359. `spec-error-log.md` v1.88. Pass-8 gate: **PASSED** (seventh consecutive) — whole tree, quarantine empty; `MatchEngine.Tests` 436/0/10 (30 m 36 s), `SeasonSave.Tests` 356/0/3, `InjuriesMedical.Tests` 67/67, `TrainingSystem.Tests` 52/52, `DecisionTree.Tests` 129/0/4. AR pass 9 next; the loop ends only when a pass returns no new High/Medium.)

> **Last Updated (prior):** August 8, 2026, last entry of the day (**Balance-pass adversarial review pass 7: 0 High, 2 Medium, 4 Low, all fixed — both Mediums completions of pass-6 fixes, every pass-6 verification held.** The ERR-041-012 sweep reached its last six #41 files (§7.1's T2 instruction had ORDERED the registration §4.5 forbids; §9 R-02 signed the phantom stream off as verified; Appendix C and §5.5 defined T-MD-NEU-003 in two contradictory ways under one id); and pass 6's composition gate got its four unlocked predicates locked — the pass-6 test had driven both cursors ahead together so the training branch shadowed the rest, and two mutants deleted the medical/appearance/lagging checks with the suite green. Lows: stale Modified: parentheticals; §3.4's v1.5 lines reordered below the F5 guards (they contradicted §3.3.2 and the code); SeasonSaveContents' Purpose; the duplicate-ClubId lock. Suites: 67/67, 52/52, `SeasonSave.Tests` 356/359 (+2). `spec-error-log.md` v1.87. **Gate: PASSED — executed locally at the pass-7 head `38a03c0`, whole tree, quarantine empty**: `MatchEngine.Tests` **436 / 0 / 10** in 31 m 5 s; `SeasonSave.Tests` 356 / 0 / 3. AR pass 8 followed.)

> **Last Updated (prior):** August 8, 2026, latest same day (**Balance-pass adversarial review pass 6: 0 High, 4 Medium, 5 Low, all fixed — two demonstrated by probe, every pass-5 fix verified sound. All four Mediums are one lesson from different angles: a rule enforced at one boundary and stated as if it held at all of them.** The KD-4 calendar invariant was Load-only three lines above the gate whose own doc states never-write-what-Load-refuses (and a passing test recorded the asymmetry as intended — Save now refuses it); the pass-5 cursor gate was ahead-only while a cursor LAGGING by ≥ 2 wedges the career permanently via F7 (demonstrated; both directions now refused, the legitimate lag-of-one locked as a PASS case); the same rule missed the COMPOSITION boundary entirely — a career driven eleven days ahead through public API composed into a loop that accepted it and silently skipped seven days of conditioning and seven armed draws (the constructor now pairs career against clock, and the three career writers went `internal`); and ERR-041-012's de-phantoming had stopped at §4.5/§3.1, leaving the registered stream alive in five places across #41 §§1/2/5/6 including the headline KD-1 and a normative signature that could not express the required dial (swept; four section bumps). Lows: step-9 prose → step 12; the unreachable WindowMask branch; SeasonSaveManager's appearance-block doc mentions; §3.4's pseudocode gains its pre-round line and clock guard; duplicate ClubIds refused by name. Suites: 67/67, 52/52, `SeasonSave.Tests` 354/357 (+2). `spec-error-log.md` v1.86. **Gate: PASSED — executed locally at the pass-6 head `d891aad`, whole tree, quarantine empty**: `MatchEngine.Tests` **436 / 0 / 10** in 30 m 58 s; `SeasonSave.Tests` 354 / 0 / 3 (the pass-6 binaries — counts confirm). AR pass 7 followed.)

> **Last Updated (prior):** August 8, 2026, still later same day (**Balance-pass adversarial review pass 5: 0 High, 7 Medium, 10 Low, all fixed — three demonstrated by probe execution, and the new cross-blob gate's first suite run caught the round-trip fixture itself writing incoherent cursors.** The Mediums: the `APPR` sub-blob's byte layout was specified in NO spec while F3 makes the first written layout the format permanently (**ERR-030-028** — the ERR-029-004 class, on the block created one landing after that ERR was filed; #30 Appendix B.1 pins it field by field); #30 §3.4 — the owning spec's algorithm for the round loop — contradicted the code on three counts (the seam marked "empty until #44 T2" has been LIVE since T2, `PlayThroughEngine` showed raw two-argument configuration, and the loop had NO appearance-record step; untouched since v0.8 while three landings changed the code it specifies — now v1.4); the pass-4 default-block lock killed no mutant (rebuilt with empty siblings + message/paramName pins); a per-player world-day cursor ahead of the restored clock WEDGED the career permanently once the dial armed (the day steps run before the clock increment, so it can never catch up; save/reload clean) — `SeasonSaveManager` gains the cursor-vs-clock rule as its second cross-blob check at Save AND Load; five doc sites still described the pre-arming world, one deferring a consequence to "the balance pass that arms the dial" which armed it and did not — **fielding the injured is strictly FREE** (unmodified attributes, cannot be re-injured, recovery not extended), now a RECORDED-NOT-FIXED block at `SelectAvailable`; the manifest's per-file inventory omitted eleven balance-pass files and carried six wrong rows; and AR pass 1's record had been written by EDITING the published changelog entries in place — both originals restored verbatim and pass 1 split into its own entries (v2.88). Lows: the log's stale footer retired; table columns; KD-3's ERR-027-004 pointer; the `[FIXED]` bitmask window bound catalogued; two stale headers; `PlayerRecord` cites #27; the scenario's injury-changed-the-eleven precondition; the instrument reads its constants; the congestion rows named FORMULA PROBES; offending-set `ParamName`s. Suites: 67/67, 52/52, `SeasonSave.Tests` 352/355 (+3). `spec-error-log.md` v1.85. **Gate: PASSED — executed locally at the pass-5 head `b76cbd3`, whole tree, quarantine empty**: build 0 errors, 0 failures in every suite; `MatchEngine.Tests` **436 / 0 / 10 skipped** in 31 m 5 s; `SeasonSave.Tests` 352 / 0 / 3 inside the gate (the pass-5 binaries — the counts confirm it). AR pass 6 followed; the loop ends only when a pass returns no new High/Medium.)

> **Last Updated (prior):** August 8, 2026, even later same day (**Balance-pass adversarial review pass 4: 0 High, 6 Medium, 10 Low, all fixed — two findings demonstrated by executing a probe against the built assemblies.** The Mediums: `Save`'s pass-3 coherence gate still wrote the one file `FromBlocks` refuses — a cross-club duplicate id, the very predicate the same commit added to `FromBlocks` — and now runs the same walk; the appearance record's ENGINE branch had never executed anywhere (every career suite is QuickSimAll, every engine-mode career test stops at the boot — the T2 pass-5 High's shape recurring in a landing that cites it), so the season-loop scenario wires a career with an injured managed-club starter and asserts the engine fixture records the FILTERED eleven on the real match it already pays for; ERR-041-019's contract reached the OWNING spec (**ERR-027-004** — FR-SQ-010 still said club-scoped full stop, and #42/#31's allocators read #27, not #41); the per-club roster reconciliation's transfer-reset residual is RECORDED at the code site and #41 §3.1.1 (a moved player arrives fit — worse than the luck-change the club-term refusal prices; #31's arrival obligation); two APPROVED spec headers misstated their own currency on the amended sections (bumped, with demotions); and two pass-3 test edits shipped ROWLESS — the fourth consecutive FR-CS-057 recurrence — with `file-manifest.md` asserting versions the tree did not have. Lows: default-block refused by name not NRE (demonstrated); every gate branch + the permuted-order PASS case locked; the `AppearanceWindowDays` [1,31] invariant; the shifting-provider lock proves its budget was consumed; `SeasonSaveCodec` doc drift; 1.6×10⁷; the log's own rows' 1% annotated; a third out-of-order version table; the stale T2 MatchLoad residual marked CLOSED; the F6-guard's cannot-be-locked note. Suites: 67/67 (+1), 52/52, `SeasonSave.Tests` 349/352 (+3). `spec-error-log.md` v1.84. **Gate: PASSED — executed locally at the pass-4 head `92af584`, whole tree, quarantine empty**: build 0 errors, 0 failures in every suite; `MatchEngine.Tests` **436 / 0 / 10 skipped** in 30 m 36 s. AR pass 5 running; the loop ends only when a pass returns no new High/Medium.)

> **Last Updated (prior):** August 8, 2026, later same day (**Balance-pass adversarial review pass 3: 1 High, 4 Medium, 8 Low, all fixed — and the High's guard caught a real cross-club id collision in the project's own test fixture on its first execution.** **The High (ERR-041-019):** the armed occurrence draw is keyed `(worldSeed, playerId, worldDay)` with no club term, but #27 promises `PlayerId` uniqueness only WITHIN a club — the career layer itself is keyed `(ClubId, PlayerId)` on that premise — and nothing checked the difference. Two clubs sharing an id would draw bit-identical injury luck forever, silently; it was safe only by accident of today's one id allocator. Now enforced fail-loud at all three id entry points (`ForLeague`, `FromBlocks`, the roster sync's validating half — the path #42 intake and #31 transfers actually arrive through), with the key deliberately unchanged: a club term re-rolls every career (the ERR-041-011 argument) and would change a transferred player's luck with his club. **The first suite run under the guard failed the roll test**: its regen fixture's suffix mapped into club 2's id range — the project's first post-generator "allocator" was its own test, and it collided, invisibly, since T2. **Mediums:** `FromBlocks` shared the training block's id arrays while copying the states (the back door reopened through the binary-search keys — now copied); the appearance state's carry through the roster sync had no test at all (revert it and every suite stayed green — now locked in the roll test); pass 2's "locked by" claim was false — both recorded locks compute their expectation through the same walk the deleted code used, so both passed against the pre-fix loop; the discriminating lock now shifts the provider's roster mid-round and fails pre-fix by construction; and #41 §3.1's normative pseudocode still took an `rng` its body never used and never named the FR-MD-027 dial — the ERR-041-012 class one section from the section D4 rewrote. **Lows:** the recording became pair-atomic (`RecordFixtureAppearances`: both clubs validated before either written); `Save` now refuses an incoherent career-block triple (it could write a file its own restore path refuses — and the round-trip suite WAS writing one); #30's slot list un-marked the LIVE slots 2/4 "NULL SEAM today"; two out-of-order version tables swapped; the null-appearance refusal and empty-set assertions joined their siblings; assorted stale headers/comments corrected. Suites after the fixes: `InjuriesMedical.Tests` 66/66, `TrainingSystem.Tests` 52/52, `SeasonSave.Tests` 346/349 (3 known skips; +3 = this pass's locks). `spec-error-log.md` v1.83. **Gate: PASSED — executed locally at the pass-3 head `4a5ab5e`, whole tree, quarantine empty**: build 0 errors, 0 failures in every suite; `MatchEngine.Tests` **436 / 0 / 10 skipped** in 30 m 25 s; `SeasonSave.Tests` 346 / 0 / 3 inside the gate. AR pass 4 running; the loop ends only when a pass returns no new High/Medium.)

> **Last Updated (prior):** August 8, 2026 (**Balance-pass adversarial review pass 2: 0 High, 1 Medium, 9 Low, all fixed** — `spec-error-log.md` v1.82. **The Medium is the T2-H3 shape recurring one pass after it was cited:** `AdvanceAndPlayNextRound` re-derived the fielded XIs by a SECOND `SelectAvailable` + selector walk — an unenforced agreement with the engine's configuration that would diverge silently the day a manager-chosen XI lands (#38 Wave-7), recording eleven men who did not play. Fixed structurally: the XIs come OUT of `ResolveFixture` — the engine branch derives them inside `BootFixtureEngine`'s new id-producing overload, one statement from the `ConfigureSquads` that consumes the same filtered squad instances; the quick-sim branch from the very squads its rating reads — and the loop's second walk is deleted. Locked by `EnginePath_HandsBackTheFilteredElevensIds` (both sides; injured starters absent from the ids the record consumes) and an injured-starter round case that forces the availability filter to PARTICIPATE in the XI-identity assertion — with every player fit, both sides could read the unfiltered squad and still agree. **Lows:** the ERR-030-027 re-entered day no longer computes occurrence inputs `MedicalStep`'s F6 cursor will discard (and no longer reads the appearance window with today's match already in the bits — correct before only by the shift-0 exclusion); direct `RecordAppearances` refusal locks — nothing-written on a failed validate (fresh-listed-first is the interleaved-write mutant-killer) and a recording throw leaves the fixture UNPLAYED, the property the pass-1 ordering fix exists for; delta-form perturbation asserts (the starter band edge holds by ~0.03 BY DESIGN — the effect-size claim moved onto halving-costs-≥0.5/0.3-injuries margins no band refit erodes); `MedicalStep`'s worst-case-below-clamp/1% tuning note corrected to saturates/1.6% plus three test-comment residues and `training-system-design.md`'s duplicated malformed "v0.5" row (renumbered v0.8); slots 9–11 joined `RunCareerDaySteps`' seam list; three pass-1 one-line doc edits that shipped ROWLESS got version rows (the FR-CS-057 class recurring one pass after its own hygiene sweep — `InjuriesMedicalConstants`, `MedicalStep`, `InjuriesMedicalConstantsTests`); `SeasonSaveManager`'s orphaned header fragment + `SeasonSaveManagerTests`' stale T1 annotation; the three-parallel-state-sets ceiling note on `PlayerCareerStates` (a FOURTH per-player set — #44 suspensions is the candidate — collapses the shape into a per-player career struct); substitution-dependency notes on `AppearanceState`/`SquadRating.StartingElevenPlayerIds`. No new ERR ids. Suites after the fixes: `InjuriesMedical.Tests` 66/66, `TrainingSystem.Tests` 52/52, `SeasonSave.Tests` 343/346 (3 known skips; the +5 are this pass's locks). **Gate: PASSED — executed locally at the pass-2 head `c63bb38`, whole tree, quarantine empty**: build 0 errors, 0 failures in every suite; `MatchEngine.Tests` **436 / 0 / 10 skipped** in 30 m 42 s — the pass-2 fixes moved no acceptance band; `SeasonSave.Tests` 343 / 0 / 3 inside the gate too. AR pass 3 launched over the fixed tree; the loop ends only when a pass returns no new High/Medium.)

> **Last Updated (prior):** August 7, 2026, still later same day (**Balance-pass AR pass 1 [RESTORED ENTRY — AR pass 5 M7: originally injected into the already-published balance-pass entry in place (commit 3549ab1), against this file's own no-edit rule; split back out verbatim, dated to the work it records.] **Adversarial review over the landing: 0H + 3M + 8L, all fixed** (`spec-error-log.md` v1.81) — headline M: baseline + one appearance consumed 9,600 of the 10,000 ceiling, compressing the #29/robustness terms into ≤4% of the range for every player who played; fixed as headroom (`InjuryRiskMax` 10000 → 16000, a 1.6%/day cap that still binds below #29's ~19,960 producer max; congestion now prices at 1.49%); the other two Ms were recorded tautology classes recurring (the D4 restore lock passed disarmed — now a growth assertion; the HardContacts lock clamp-saturated — now sub-clamp with a precondition). Re-measured post-fix: pooled 783 injuries/season, starters 2.08, reserves 1.13, unavailability 9.5%. **Gate: PASSED — executed locally, whole tree** (the Ubuntu-archive SDK; baseline on the pre-change tree also PASSED): build 0 errors, 0 failures in every suite, quarantine empty; `MatchEngine.Tests` **436 passed / 0 failed / 10 skipped** in 31 m 13 s — the arming moved no acceptance band; `SeasonSave.Tests` **338 / 0 / 3 skipped**; `InjuriesMedical.Tests` 66/66; `TrainingSystem.Tests` 52/52)

> **Last Updated (prior):** August 7, 2026, even later same day (**The #29/#41 balance pass LANDED (owner-directed): the occurrence dial is ARMED — FR-MD-027 — at rates measured in the football band.** Council-shaped: the integrity and evidence advisors were convened on the plan pre-implementation and independently converged on the same cheaper shape for the deferred ERR-030-026 split — don't split the step, move the day. **D1 (ERR-030-027):** the fixture day's own KD-2 slots run pre-round at the top of `AdvanceAndPlayNextRound` (idempotent; the next advance's re-run is a cursor no-op; #30 §3.3.2 pins the convention). Recovery lands before selection, so tiers mean what they say; the occurrence draw moves to matchday morning, fed by the appearance window, which never contains the current day. FR-MD-022, KD-6 and the medical format survive verbatim — the split would have cost a second persisted cursor, `MEDICAL_SAVE_FORMAT_VERSION` 1→2 and a KD-6 revision for the same ordering. **D2 (ERR-041-010(b) closed):** the per-player appearance record — a lazily-shifted day-bitmask, shifted at READ time so no new KD-2 slot, no third idempotency cursor and no split-relative ordering question exists; written per fixture for both clubs' fielded XIs via the new `SquadRating.StartingElevenPlayerIds` (one selector, three read shapes); persisted as the mandatory `APPR` v1 sub-blob (`SEASON_SAVE_FORMAT_VERSION` 3 → 4, typed `AppearanceBlock`); read into FR-MD-010's `MatchLoad` at slot 4. **D3 (ERR-041-011 + ERR-029-007):** `OCCURRENCE_DRAW_DENOM` → `[FIXED]` 1,000,000, DECOUPLED from the `[GT]` ceiling — the draw is `hash % denom`, so the old `[GT]`-derived denominator let one config edit re-roll every career's injury luck; `BaselineDailyRisk` 4000 before the mitigation (position normative) kills the injury-proof-forever default; `AppearanceLoadWeight` 150 → 5600. **D4 (ERR-041-012):** the dial argument is REQUIRED (no default in either position), production posture ON, OFF locked both ways plus a restored-career-still-injures lock; #41 §4.5's phantom registered stream rewritten to the keyed derivation, ordinal 92 deliberately unallocated. **Measured by the new season-scale instrument** (8 seeds × full 20-club quick-sim seasons — the ERR-030-014 lesson at season scale): league 717–816 injuries/season (~39/club vs the E-1 band 30–55), starters 2.08, reserves 1.12, unavailability 9.4%; bands league-wide and perturbation-proof. Characterization AFTER numbers at per-100k resolution replace the 23.1%/0/43.1% BEFORE absurdities; certainty is structurally unreachable at the per-million denominator, so the forced-occurrence pattern became deterministic hot-day scans. ERR-041-002/-003's deferred back-props discharged; the research-alignment supplement's stale id-map re-based to ERR-041-013..018. Deliberately NOT done: the research supplement's R-1/R-2 structural changes (awaiting owner sign-off), the quick-sim condition question (#30 line comment, still open), slot 1 (#28/D1). **Gate:** the full whole-tree gate is EXECUTING LOCALLY at this commit (the Ubuntu-archive SDK; baseline run on the pre-change tree PASSED) — its verdict line lands in the close-out commit that follows, never written ahead of execution (the v1.75 lesson))

> **Last Updated (prior):** August 7, 2026, night (**Merge of origin/main `80d97c8` into the B9c branch — two sessions raced on the same discovery, and the two records below reconcile as follows.** This branch's gate run (the "evening" entry) and main's run-419 diagnosis (`b162a00`, the "latest same day" entry) independently found the same two `MatchEngine.Tests` band failures AND the same environment fact (Ubuntu-archive `dotnet-sdk-8.0` installs where the dot.net installer is 403-blocked). Main went further: it diagnosed both failures per-seed and **rebaselined both bands by owner call** (`no-deep-dive-early-miss` `== 0` → `<= 1`; the cosine bound −0.10 → −0.16), recording the regressions for the KD-W1 calibration pass. That is exactly the "fixed or re-banded with owner sign-off" exit this branch's OPEN ISSUES entry named, so **the two-red-locks entry filed this evening is RESOLVED at this merge** (15 → 14 active; moved to `open-issues-resolved.md`) — it lived for roughly one hour, which is the system working. `CHANGELOG-src.md` numbering: main's concurrent entry keeps **v2.80**, this branch's client-app landing renumbers **v2.80 → v2.81** and its gate follow-up **v2.81 → v2.82** (the `0d96670` collision precedent). No code conflict — the branches touched disjoint `.cs` files; main's rebaseline makes the full gate green for this branch's next run. Prior entries below, both sides preserved verbatim beyond the renumber.)

> **Last Updated (prior):** August 7, 2026, evening (**The B9c landing is GATE-VERIFIED — the first full local gate run in months — and the run surfaced two scenario locks RED on origin/main plus an environment fact that retires a standing assumption.** **The environment fact first, because it changes every future session's posture: `dotnet-sdk-8.0` installs cleanly from the Ubuntu archive via `apt-get update && apt-get install dotnet-sdk-8.0` (8.0.129), even though every dot.net SDK host is 403 at the agent proxy.** Weeks of landings recorded "no .NET SDK in the authoring environment; CI on push is the only compiler" — true of the installer, never checked against the distro archive (the owner pointed this out). `src/CLAUDE.md` now carries the note beside the gate command. **The gate itself:** build **0 errors** / 5 pre-existing CS0649 warnings; **`ClientApp.Tests` 15/15 on first execution**; every suite green except two — `UiFramework.Tests` 49/50, because the FR-UI-001 reverse-reference scan **fired on the new assembly exactly as designed** (`client-app` references `ui-framework` and was not in the sanctioned-renderers list; sanctioned with its tests asmdef, `MatchViewObserverNeutralityTests.cs` v1.1, suite re-run **50/50**), and **`MatchEngine.Tests` 434 passed / 2 failed / 10 skipped (48 m 8 s)**. **The two failures are main's, not this branch's — verified by execution, not inference:** a worktree at `origin/main` `9b8a7b4` ran exactly those two tests and both failed (2/2, 11 m 4 s). `sim_match_engine_close_chance` fails `final-third-dribbles-are-not-goal-averse` at **meanCosine −0.119 vs the −0.10 bound** (pre-ERR-008-018 these seeds ≈ −0.29 — marginally out of band, not collapsed); `sim_match_engine_keeper_contact` fails `no-deep-dive-early-miss` at **deepDiveEarly = 1**. Both predicates sit downstream of shot-option generation, so the -021/-022/-023 chain (landed with KD-W1 recalibration deferred) is the likely source, but no bisection has been run and the -023 entry shows `MatchEngine.Tests` was almost never executed — filed as a new OPEN ISSUES entry at the head (15 active), owned by the realism track, explicitly NOT quarantined: both are marginal band misses carrying real post--023 baseline information, and hiding them would repeat the never-executed-suite trap. Prior entry below.)

> **Last Updated (prior):** August 7, 2026, later same day (**The screen catalogue's home — the P5a layering question resolved by owner decision and landed as a NEW ASSEMBLY, `src/client-app/`.** The August 7 P5a landing had recorded, deliberately unbuilt, the four screens' `ScreenId` catalogue and navigation graph: FR-UI-010 forbids `ui-framework` hard-coding a screen, `ui-framework` sits above `match-client-core` so the core cannot hold one, and `match-client-unity` is gate-invisible (§12 rule 1). The owner chose the remaining candidate — a new gate-compiled assembly above `ui-framework` — on the `match-engine` composition-root precedent: composition lives above what it wires. `TacticalDirector.ClientApp` references only `ui-framework` and holds three types: `ClientAppConstants` (four `[FIXED]` screen ids 1–4, 0 deliberately never allocated per the `ManagerCommandKind.None` zero-value-safety convention), `ClientScreens` (the ids as typed values), and `ClientScreenFlow` (the five-edge navigation graph — Main Menu → Tactics Setup → Match View → Post-Match Report — as guarded moves over a **privately-owned** `NavigationShell`, so the graph is enforced by encapsulation and the P5b binding forwards clicks, deciding nothing). The two Replace edges are the design content, both locked by where a later Pop lands: TacticsSetup → MatchView (a running match must not sit above a setup screen "back" could return to) and MatchView → PostMatchReport (the match freezes at full time; a Pop from the report lands on Main Menu, never a dead match view). Registration transposition is refused at construction (the ERR-029-005 silent-transposition class); an abandon-match edge is deliberately absent (§5-P5b specifies no quit control — the FR-CS-049 phantom rule). 15 tests (3 catalogue + 12 flow). Assembly count 33 → 34; `client-app` is unplaced in ERR-020-002's proposal like the other 15. Roadmap B9c ✅; B9b now has nothing ahead of it but the host. C3's management screens inherit the home by precedent. **No `SNAPSHOT_SCHEMA_VERSION` change, no new RNG stream / domain tag / draw site / draw-order change — nothing reaches the sim**; blast radius checked: nothing existing references the new assembly, no scenario window, rate band, corpus fit or perf baseline moves. **Dotnet gate NOT runnable in this environment** (no .NET SDK; CI on push is the compiler); verified by manual compile-risk review + `generate_projects.py` clean at 66 csproj with both ClientApp projects present, and meta-integrity green.)

> **Last Updated (prior):** August 7, 2026, latest same day (**Main run 419 diagnosed — two acceptance bands rebaselined to the post-ERR-008-023 baseline (owner call), and the full gate now runs in the Claude remote environment.** After the -021/-022/-023 chain merged to main (`9b8a7b4`), CI run 419 failed `MatchEngine.Tests` 434/2/10 on two scenario bands — and the second failure had been **invisible to every session before this one**: both CI log tails cap at 5,000 lines, `sim_match_engine_close_chance` prints early, and the PR #303 session that fixed the entry-fatigue interleaving (`a55244c`, verified correct — all 13 pass at the merge head) could only see 2 of its run's actual 3 failures. Diagnosed by reproducing locally: **the Ubuntu-archive `dotnet-sdk-8.0` installs in Claude remote sessions** (`apt-get install dotnet-sdk-8.0`; the 403 that founded "no SDK, CI is the only compiler" was `dot.net`, not `archive.ubuntu.com`) — full suite verdicts matched run 419 exactly, per-predicate values included; `tools/dotnet-ci/README.md` v1.2 records the recipe. **The failures, measured:** (1) `sim_match_engine_keeper_contact` `no-deep-dive-early-miss` — one crossed episode's dive resolved **616.7 ms** early (seed 0xD1A6D05E; the other crossed episode −16.7 ms). Inside the pre-fix 456–2000 ms class, so the miss class has genuinely recurred at 1 per match-equivalent vs the pre-fix 9-of-15; band `== 0` → `<= 1`, the ms bound deliberately not widened past the episode. (2) `sim_match_engine_close_chance` `final-third-dribbles-are-not-goal-averse` — pooled cosine **−0.119**, and per-seed the regression is **one seed entirely**: 0x0F1E…78 held its ERR-008-018 gain (+0.078) while 0xD1A6D05E gave all of it back (**−0.232** vs −0.221 pre-fix / +0.091 post-fix); goalward share held at 0.450 only because the healthy seed carries it (0.564 / 0.385). Bound −0.10 → −0.16, still refusing the pre-fix pooled ≈ −0.29; share bound unchanged. **Both regressions are RECORDED for the KD-W1 calibration pass, not re-tuned** — the chain's own P5 residuals (the withdrawn -021 population-preserving claim; -022's blockers added with no recalibration) are the suspects, and quiet re-tuning here is the mistake -023 exists to record. Test-only changes; no schema change, no new RNG stream / domain tag / draw site, no draw-order change. Scenario files v1.1 both; `gk-contact-rate-design.md` v1.4 (AR-6); `close-chance-creation-design.md` v1.1 (Acceptance-3). Gate: executed locally in the remote session (the first landing in this file able to say that) — full `MatchEngine.Tests` at the unmodified merge head `9b8a7b4`: **446 = 434 passed / 2 failed / 10 skipped**, the two bands above and nothing else, matching CI run 419's verdicts and per-predicate values exactly; both scenarios re-run **green** after the rebaseline. Whole-tree gate: CI on this push.)

> **Last Updated (prior):** August 7, 2026 (**ERR-008-023 — the ERR-008-022 landing scored ZERO GOALS; the acceptance scenario caught it.** CI run `31188688249` (PR #303, head `a2987be`) is the first run ever to reach `MatchEngine.Tests` on this branch — that suite takes **22 m 55 s**, against the 3 minutes run 402 survived before cancellation, so nothing had exercised the match engine here at all. It failed `sim_match_engine_shot_outcomes` on `goals-still-scored = 0`: four seeds x 18 minutes, 72 minutes of football, no goal. Cause: -022's own headline fix. The retired goal-centre-plane bound had discarded a goal-line keeper for **every** shooter position, so the keeper-only `GK_BLOCKER_RADIUS_M` = 1.5 m disc had never been exercised; it went live at -022 and removed **~42% of the goal arc on every shot** (1.000 -> 0.584 at 16 m, keeper alone). Fixed by retiring the disc — every blocker occludes with `BLOCKER_RADIUS_M`, keeper included, because reach beyond the body is shot-stopping and P3 assigns that to #11, which prices it at contact. `gkness` survives, lerping the P3 exemption alone. Suite 15 -> 16; a GK-read continuity lock that was about to become this file's third tautology of its class now carries live attributes. This is the P5 residual -022 recorded as *not fixed* under KD-W1.)

> **Last Updated (prior):** August 6, 2026, latest same day (**ERR-008-022 far-post lock corrected — the
> first gate run.** **CI run 402 (PR #302, head `301c634`) — the first execution of any of this work.** Build succeeded **0 errors** (5 warnings); `DecisionTree.Tests` **127 passed / 1 failed / 4 skipped / 132**, every other suite green — but **not a gate pass**: the `Compile + test` job was cancelled at 16:59:45, before `run-gate.sh` reached `Gate PASSED`, and four hygiene checks were cancelled without ever being assigned a runner (`spec-error-log.md` v1.75). The failure was `ShotLane_FarPostBlocker_OccludesTheGoal` (expected 0.782157, got **0.728880**) and it was the TEST: it read `ctx.OpponentGoalPostL`, y = **30.34** in the home fixture — the post *nearer* the (90, 24) shooter. The pre-fix bound kept the near post and discarded only the far one, so the lock named for ERR-008-022's headline finding would have **passed against the broken model**. Now selected by geometry (`FarPostFrom`), not by the `PostL`/`PostR` label, which carries opposite sides in this file's two fixtures; expected value unchanged and **not** compiler-confirmed — the run evaluated the old test and returned the near post's 0.728880, and `0612bcc` has never been compiled since. The recorded 12-of-12 mutant kill overstates the far-bound mutant accordingly — the harness killed it, the committed test did not. Prior entry below.)

> **Last Updated (prior):** August 6, 2026, latest same day (**ERR-008-022 — the shot lane threw away the far post
> before the occlusion model ever ran.** From the adversarial review over the ERR-008-021
> landing, one day old, in three hostile passes. #8 §3.1.4.3's lane test bounded the shooting
> lane by the distance to the goal **centre** — a plane that, for any shooter not on the goal's
> centre line, cuts diagonally across the goal mouth. Measured: the **far-post** blocker was
> discarded and the near-post one kept on **20,213 of 20,213** sampled in-range off-centre
> shooters; a keeper standing on his line at goal centre gave `proj == distToGoal` exactly and
> was dropped for *every* shooter position, so shooter (95,20) with a keeper in front read
> **1.000, a completely open goal**; and the mirror case admitted an opponent standing *behind*
> the goal line, in the net, at the keeper's 1.5 m radius. The far post is half of what a
> shooter aims at, so ERR-008-021's whole reason for existing — pricing partial occlusion at
> the posts — was being denied its geometry upstream. Two further hard predicates in the same
> derivation turned out to be **larger cliffs than the one -021 removed**: `GOAL_MIN_SHOT_DIST`
> stepped `GoalOpeningScore` 1.000 → 0.050 across one centimetre of lane depth, and since 0.050
> sits below `MIN_GOAL_VISIBILITY` it deleted the SHOOT option with it; and the goalkeeper
> classification stepped it 0.768 → 0.311 across 2 cm — a step -021 had *widened* to 0.551 by
> making it attribute-dependent, three lines from the code it rewrote, unrecorded. All three
> fixed: goal-line-plane bound, plus two new `[GT]` ramp widths (`SHOT_BLOCKER_NEAR_FADE_M` =
> 1.0 m, `GK_PROXIMITY_FADE_M` = 2.0 m) turning the remaining predicates into slopes, with
> `gkness` lerping the blocking radius and the P3 ability exemption together.
>
> **Three of the -021 landing's own verification claims were false** and are corrected in the
> record rather than left standing. (1) The **P5 exactness** argument — "the old rectangle and
> the new trapezoid integrate to `4h·halfArc` for every `h`, including `h > halfArc`" — is wrong
> above `h = halfArc`, where the old model's per-opponent clamp saturates and the trapezoid does
> not: measured **1.198×** at `h`=10°/`halfArc`=8.35° and **2.000×** at `h`=16.7°, reachable for
> any blocker within ~2.7 m of a 20 m shooter. That claim was the stated reason no recalibration
> was needed; the reason is withdrawn and the residual left for the balance pass (KD-W1).
> (2) The **test count**: **10** locks, 9 evaluable, 5 fail / 4 pass — not "9 locks, 5 of 8",
> published in six documents. (3) The **§3.2.3.2 worked example**: its opponent sat 4.5 m from
> the goal line, so the algorithm classified him a **goalkeeper** and exempted him from the very
> ability term the example demonstrated — every number in it, including the two new derived ones,
> was unreachable. Re-derived with a genuine outfielder and re-expressed in corner-origin
> coordinates (it was written in the abolished centre-origin frame).
>
> **The suite was inadequate to its own claim.** The over-blocking half of ERR-008-021 had **no
> lock at all** — the only test reaching a partial overlap asserted `< 1.0` rather than the
> value, so a mutant restoring the pre-fix full-width contribution **passed all ten locks**; 8 of
> 12 plausible mutants survived. Every fixture put the shooter on the goal's centre line, which
> made `bisector` and the post clipping untestable and the away "mirror" bit-identical to the
> home case. And `ShotLane_NullAttributeView_IsAbilityNeutral` was a **tautology**: the helper's
> own `if (attrs != null)` guard discards the differing arguments, so it asserted `f(x) == f(x)`
> — the exact shape the ERR-008-020 review caught one landing earlier, in a commit that claimed
> to have avoided it "at authoring time rather than at review". The pass-lane twin had it too.
> Suite 10 → **15** locks, all six new ones asserting values or continuity rather than
> inequalities.
>
> Spec §3.1.4.3 v1.5 + §3.2 v1.13 (which also writes the **v1.12 row ERR-008-021 never wrote** —
> §3.2.3.2 was rewritten inside that section with no version-history entry) + `OptionGenerator.cs`
> v1.8 / `UtilityWeights.cs` v1.12 / `DecisionTreeConstants.cs` v1.5 / `OptionGeneratorTests.cs`
> v1.8, same commit. Three §8 documentation defects cleared alongside: §3.1.4.3 delegated
> `GoalOpeningScore` to **§3.2.2, the PASS formula** (ERR-008-018 verbatim, in text written one
> day after it); the field was named `GoalVisibilityScore` there and `GoalOpeningScore` everywhere
> downstream; and a stale duplicate of the §3.2.13 version history, eleven revisions behind, was
> removed in favour of a pointer. **Digest invariance NOT claimed** — every change is live on
> generated shots. **Gate NOT run — no .NET SDK in the authoring environment**; every number above
> is closed-form derivation cross-checked against a Python reference implementation of both models.
>
> **AR-2, same day.** A second hostile pass over this fix found it committing the same class of
> error it criticises: both new ramps ran entirely on ONE side of the predicate they replace
> (1.0 → 2.0 m and 6 → 8 m), which is a systematic one-sided reduction in occlusion dressed as a
> continuity fix. ERR-008-019 and ERR-008-020 both explicitly centred their ramps on the old cliff
> so the population integral is preserved — the P5 pivot. Corrected to half-width either side
> (0.5 → 1.5 m and 5 → 7 m), so a blocker at exactly `GOAL_MIN_SHOT_DIST` contributes half his
> occlusion and one at exactly `GK_PROXIMITY_TO_GOAL` reads half keeper; both constants are now
> ramp *centres* and their catalogue comments say so. Every value lock is unchanged — all sit
> outside the ramp bands — and both continuity sweeps were re-ranged to span them.)

> **Last Updated (prior):** August 5, 2026, latest same day (**ERR-008-021 — the shot-lane occlusion
> test told a defender across the near post from an open goal.** The §6.4 follow-up the
> ERR-008-020 template fix deliberately deferred, now discharged; third fix under the
> football-judgment proxy review's remediation doctrine. #8 §3.1.4.3 / §3.2.3.2's
> `ComputeGoalOpeningScore` carried **both** of the pass lane's defects, and the containment one is
> the more damaging: step 4 counted an opponent's occlusion only when his angular *centre* lay
> inside the goal arc, and then counted his **entire** angular width. So a defender whose centre
> sat a hair the wrong side of the post direction contributed **exactly nothing** — the shooter
> read a *fully open goal* with a man standing squarely across his near post — while one a
> centimetre the other side contributed a full width, half of it behind the post and blocking
> nothing at all. On the fixture the suite now uses (shooter 15 m out on the centre line, one
> blocker 5 m in front), **4 cm of lateral defender position stepped `GoalOpeningScore` from 0.595
> to 1.000**. That score prices the SHOOT candidate (§3.2.3.1), gates its existence (§3.1.4.1) and
> drives `PowerIntent` (§3.5.3), so the discontinuity reached shot selection, shot value and shot
> speed alike. The second defect is §2's pattern-(a) finding transposed to the goal: the width was
> `2·atan(radius/distance)` — body radius alone — so a defender who neither reads the shot nor gets
> his body into its line shut the goal off exactly as hard as one who does. **Fixed:** the
> contribution is now the true angular **overlap** of the blocking disc with the goal arc. Unlike
> -019 and -020 this required **no ramp constant, no half-width `[GT]` and no tolerance epsilon** —
> an interval intersection is continuous by construction (P1) *and* is the geometrically honest
> answer, so the over-blocking and the under-blocking fall out with the cliff rather than needing
> separate fixes; the 0.01° epsilon the containment test needed is deleted. The overlap is scaled
> by the blocker's **Anticipation + Positioning** ability (`SHOT_BLOCKER_ABILITY_MIN/MAX` =
> 0.6/1.4 `[GT]`, league-average exactly 1.0) read through the **shooter's Vision** as
> discrimination fidelity (P2) — reusing `LANE_VISION_FIDELITY_FLOOR` rather than declaring a
> second one, because fidelity is a property of the assessor and a duplicate would be a parallel
> surface, not a parameter. The **goalkeeper is exempt from the ability term** and occludes on
> geometry alone (P3): #11 §3.5's save model and §3.7.0's rush — which *sets* the geometry this
> function measures — own his shot-stopping, so pricing it here as well would charge the shooter
> twice for one keeper. **P5 holds exactly rather than approximately:** over a uniformly-placed
> blocker the old rule integrates a rectangle of area `4h·halfArc` and the overlap integrates a
> trapezoid of area `4h·halfArc`, for every disc width and every arc — so the fix redistributes
> occlusion from a step to a slope and from anonymous bodies to identified ones without opening or
> closing the goal on average; the ability midpoint of 1.0 leaves the attribute axis neutral too.
> **No schema change, no new RNG stream / domain tag / draw site, no draw-order change.** **Digest
> invariance is NOT claimed and is false** — the -019 lesson applied at authoring time rather than
> at review: this model is live on every SHOOT candidate the generator produces and moves for any
> blocker who is not both exactly average and wholly inside the arc. The behaviour change is the
> point. **Blast radius — stated, not cleared, because nothing here can be executed.** The change is
> inside one pure function and adds no per-tick work, so `FR-PO-052` is not in question, and no
> `[GT]` governing an already-calibrated chain moved (the two new dials are first-guess values on a
> surface KD-W1 leaves uncalibrated until the complete-engine pass). But `GoalOpeningScore` gates
> the SHOOT candidate through `MIN_GOAL_VISIBILITY` and multiplies `U_SHOOT`, so **shot and goal
> production move on every seed**, and the acceptance scenarios with rate bands downstream of that
> — `match-engine-shot-outcomes` (`MaxMeanGoalsPerWindow` = 2.4, `shots-are-taken`), the
> shot-speed and keeper-save scenarios, and the goal-rate-sensitive diagnostics — must be re-checked
> at the first gate run. They are deliberately loose over-correction guards rather than calibration
> claims, and the P5 integration argument says the *mean* should not move, so a trip is not expected;
> it is also not ruled out, and a trip should be read against this landing before being read as a
> regression.
> Locked by **10 `OptionGeneratorTests`** (v1.7), including the P5 pivot on the *computed* path as
> well as the null-view path (Anticipation 10 / Positioning 11 ⇒ mean01 = 0.5 exactly — the
> ERR-008-020 AR-1 M-1 lesson, applied while authoring instead of at review), the GK exemption
> proved by moving the keeper's attributes between the extremes, and the away mirror. A reference
> implementation of both models, run over all ten locks, confirms **5 of the 9 that can be
> evaluated against the old model fail on it** — continuity (step 0.405 vs the asserted < 0.05),
> the straddling blocker (1.000, not < 1.0), home discrimination, the low-Vision separation (the
> pre-fix gap is exactly zero) and the away mirror. The remaining three — both P5 pivot rows and
> null-view neutrality — pass pre-fix by construction, which is the point of a pivot row; the
> ninth, the MIN/MAX-midpoint invariant, cannot be evaluated pre-fix because the constants are new. **Recorded, not fixed:**
> `IsInShotPath`'s corridor end-bounds are still hard (a near-end step at 1.0 m, and an exclusive
> far bound that drops a keeper standing exactly on his line) — front-of versus behind the goal
> line is a physical fact rather than a football judgment, so P1 does not obviously reach it; and
> §3.2.10's constant catalogue, which **five consecutive #8 landings** have now left behind, so its
> "Total constants: 58" summary is wrong by at least nine and wants a reconciliation pass of its
> own. The 34-finding tally is unchanged — the shot lane was never itemized as its own §2/§3
> finding, so **32 itemized findings remain open**. Surfaces synced: #8 `section-3-1.md` v1.4 +
> `section-3-2-3-to-3-2-9.md`, `spec-error-log.md` v1.67 (head + entry + index row),
> `football-judgment-proxy-review.md` (header, §2, new §6.4.1), `open-issues.md`, `CLAUDE.md`,
> `CHANGELOG-src.md` v2.76, `file-manifest.md`, `README.md`. **Gate NOT run — no .NET SDK in the
> authoring environment; CI compiles on push.** Prior entry below.)
> **Last Updated (prior):** August 7, 2026 (**The #29/#41 T2 branch reached a compiler for the first time and
> failed to build.** CI run 405 on PR #304 — the first run this branch has ever had — reported
> `Build FAILED, 5 Error(s)`, all in `PlayerCareerStates.FromBlocks`. The adversarial-review pass-3 fix
> that made `FromBlocks` copy its two state arrays rather than borrow them declared those locals
> `training` and `injury`, inside a method whose own parameters are `training` and `medical`. C# does
> not read that as shadowing: once a local of that name is declared anywhere in a block, the whole block
> resolves the name to the local, so the four earlier uses of the *parameter* became CS0841
> use-before-declaration errors and the declaration itself CS0136. Renamed to `trainingStates` /
> `injuryStates`. The behaviour is exactly what six review passes described; the code had simply never
> been compiled. **That is worth recording plainly**: the failure every "NO GATE RUN" caveat on this
> landing anticipated arrived not as a wrong algorithm or a bad assumption but as a name collision — the
> single cheapest class of defect a compiler catches, and one that careful reading demonstrably does not.
> What run 405 did establish: `match-engine`, `training-system` and `injuries-medical` compiled, so the
> four-argument `ConfigureSquads` fatigue seam, `SquadRating.CanFieldStartingEleven` and
> `LineupSelector.TrySelect` are real. `season-save` failed, so its test project never built and both new
> suites — including the pass-6 `FromBlocks` copy lock — are still unexecuted. Every symbol they touch has
> now been hand-checked against its declaration for name, signature, accessibility and type, which is the
> ceiling of what this environment can verify. The next run is the real answer.)

> **Last Updated (prior):** August 6, 2026, latest same day (**Adversarial review pass 6 over the #29/#41 T2
> landing — 0 High, 1 Medium, 2 Low, all fixed.** After three consecutive passes that each found
> something only by changing axis, this one picked four the earlier passes had not used, and the one that
> paid was asking a different question entirely: *does each of the twelve fixes landed in passes 3–5 have
> a test that fails without it?* Eleven do. The twelfth — pass 3's change making
> `PlayerCareerStates.FromBlocks` **copy** the two state arrays instead of borrowing them — has none, so
> reverting the `Array.Copy` leaves every suite green while reopening a silent-overwrite hole:
> `ClubTrainingStates.States` is a public array field and `SeasonSaveContents` is a public struct, so a
> caller holding a loaded save needs no internals access to rewrite a running career's conditioning and
> injury state, straight past both day steps and both subsystems' declared single writers. That is the
> same defect class as pass 1's ascending-ids High, on the same type, one route further in. Now locked by
> a test that mutates through exactly that public surface. **The two Lows:** the load-time
> `AvailabilityFilteredSquads` decorator still justified itself with "can safely share arrays with the
> blocks it hands back" — true when written, false since pass 3, and a reader trusting it would conclude
> `FromBlocks` borrows; and the five files this landing created carried no `// Modified:` header field
> despite two or three version-history rows each (FR-CS-056/057 requires one, matching the latest row).
> **Also closed a gap in the pass-3–5 commit itself:** `92baaa3` never updated `file-manifest.md`, so
> this landing carries one combined passes-3–6 manifest entry rather than leaving a landing unrecorded.
> **What the pass cleared, since three "clean" calls have now been wrong:** every new symbol's
> compile-resolvability — including the `MatchEngine.MatchEngine` qualification, which I expected to be
> CS0426 and is not, because the `using` directives sit at compilation-unit level rather than inside the
> namespace declaration, so the enclosing-namespace walk reaches the namespace before the imported type;
> hand-execution of the three new `EnginePath_*` cases against F442 and `CareerTestRoster`'s position
> layout (ten of eleven home starters are drawn from locals ≥ 11, so the entry-fatigue assertion is not a
> coin toss, and the filtered squad is exactly 18 and fieldable, so the back-fill never fires and the
> divergence precondition is real); and repo hygiene — all five new files tracked with `.meta` siblings,
> and no caller outside `season-save` touching the three surfaces that went `internal`. Nothing has been
> compiled or executed at any point in this round: still no .NET SDK in the authoring environment, the
> installer still 403 at the proxy, so CI on push remains the only compiler for all of it.)

> **Last Updated (prior):** August 6, 2026, latest same day (**Adversarial review passes 3–5 over the #29/#41 T2
> landing — 1 High, 4 Medium, 7 Low, all fixed.** Three further passes after the pass-2 "clean" call, each
> going at an axis the earlier ones had not: the unread spans of the four new suites, then the
> resolution-mode axis across every test in the assembly, then the day-step producer read against #30's
> fixture interleave. The second and third each found something the first two passes could not have seen
> by reading harder — which is the honest lesson of this round: "converged" meant "converged on the axes
> I had looked at".
>
> **H — the career-wired match boot had never executed anywhere.** Every test that wires a
> `PlayerCareerStates` runs `QuickSimAll`; every `ManagedThroughEngine`/`FullEngine` test in `season-save`
> builds the loop through the careerless three-argument constructor. So `SeasonLoop.PlayThroughEngine`'s
> boot — the **sole production call site** of #29's match-entry-fatigue seam, and the only place the
> ERR-030-009 availability filter meets a real `MatchEngine` — shipped with zero execution, which is
> ERR-030-014's shape one layer up: a composition that runs green without doing the thing it exists to do.
> Found by sweeping resolution modes, not by reading the code again. Fixed structurally rather than by
> bolting on a slow test: `SeasonLoop.BootFixtureEngine` is extracted `internal`, which is exactly what
> was done to `ShouldPlayThroughEngine` and for the reason that method's own comment already gave —
> inline, the branch is reachable only by playing a full 90-minute match, and no suite in this assembly
> pays that cost. Three `EnginePath_*` cases now cover the filter, the fatigue projection and the unwired
> floor in milliseconds.
>
> **M1 — the entry-fatigue tests could not fail.** `MatchEngineEntryFatigueTests` probed indices where
> squad-local and starter-slot coincide, because `CoherentSquad` lays positions out in slot order. Swap
> `entryFatigue[local]` for `entryFatigue[k]` in `ApplySquad` and every assertion in the file still passes.
> That is the one property the seam turns on, and the one #30's filter breaks deliberately — filtering
> renumbers the locals. The new case puts the only goalkeeper at the last local, so slot 0 must map to
> local 17 whatever the ratings say.
>
> **M2 — the single-writer fix was closed on one route and open on the other.** `FromBlocks` borrowed the
> two state arrays, and the documented restore path hands it the very arrays `SeasonSaveManager.Load`
> returns inside `SeasonSaveContents`; every holder of those contents was therefore writing into the
> running career's `Condition`, `Severity` and idempotency cursors, past both day steps. Pass 1 had made
> `TrainingBlocks`/`MedicalBlocks` internal for precisely this reason and stopped at the save side. Now
> copied.
>
> **M3 — detectable is not prevented.** Pass 1 answered the stale-`ScheduleFor`-handle defect with a
> `RosterGeneration` counter the caller is asked to compare. `ScheduleFor` is now `internal` and the
> public focus surface is `PlayerCareerStates.TrySetFocus`, resolving the club fresh per call, because
> this file argues two paragraphs elsewhere that binding must be structural — `TrainingSchedule` exists
> for that reason. The counter stays; `CommitRosterSync` refuses a stale plan on it.
>
> **M4 — ERR-030-026, and the one worth remembering.** #30's KD-2 tick order pins nine day-slots and has
> **no slot for playing the round**, because a round is a separate command. So where a fixture sits
> relative to slot 2 (#29) and slot 4 (#41) is specified nowhere and, in the code, falls out of
> `AdvanceToNextFixtureDay`'s loop condition — it stops on *reaching* the fixture day, so matchday's own
> steps run after the round. That is right for #41's occurrence draw and **wrong for the recovery
> countdown sharing the same atomic step**: a player whose recovery expires on matchday misses a fixture
> he had served his time for, so every injury runs one matchday longer than its tier. Inert today (the
> dial is off) and invisible to the suites either way. The cost was never today's behaviour — it is that
> the balance pass would fit `RecoveryDaysPerTickBase` and every tier-day constant straight through an
> unstated convention and absorb the bias permanently. Adopted rather than changed (splitting the halves
> alters #41's step contract), documented at all three sites that determine it, and locked by a test.
> Whether #41 should expose recovery and occurrence separately is deferred to the balance pass with owner
> sign-off.
>
> **L (7):** `SeasonSaveManager`'s version rows ran 1.7-then-1.6 with two `Modified:` headers; `SeasonLoop`
> v1.7 recorded a "public World accessor" that landed `internal`; both block types' docs disclaimed the
> decode ordering `FromBlocks` now requires, so acting on them would make it refuse valid saves;
> `CareerTestRoster.Build`'s summary named the wrong index; the quick-sim's omission of match-entry fatigue
> is argued rather than silent; the `Save(SeasonLoop, …)` overload no longer recommends a cross-thread
> `ActiveMatch` read; `Blocks_RoundTripThroughBothCodecs` asserted nothing about the medical block and held
> for two sets of identical fresh states.
>
> **Deliberately not done:** no `SNAPSHOT_SCHEMA_VERSION` change, no format bump, no RNG stream / domain
> tag / draw-site / draw-order change, and no behaviour change of any kind — the ERR-030-026 convention is
> adopted as-is precisely so this pass stays behaviour-neutral. **NO GATE RUN** — still no .NET SDK in the
> authoring environment (installer 403 at the proxy), so every claim here is static; CI on push is the
> gate. Prior entry below.)

> **Last Updated (prior):** August 6, 2026, latest same day (**Adversarial review over the #29/#41 T2 landing —
> 3 High, 4 Medium, 4 Low, all fixed; pass 2 clean.**
>
> **H1 — `PlayerCareerStates.FromBlocks` trusted an ordering invariant it never checked, and the failure
> was silent state loss.** Every lookup in that type is a binary search over the per-club player ids.
> `ForLeague` sorts; `FromBlocks` only checked that the two blocks agreed *with each other*, and
> `ClubTrainingStates`' constructor imposes no order at all — so an unordered block (which the codecs
> never produce but a public caller trivially can) made `IndexOfPlayer` miss a player who WAS carried,
> and `SyncToRoster` then read that miss as "new" and overwrote his season of conditioning and injury
> history with `Create()`. No exception, no assertion, indistinguishable from a fresh career. Now
> refused, in the loop that was already walking the ids.
>
> **H2 — a mid-match save restored the wrong starting eleven.** The match is configured with the
> availability-FILTERED squad; the snapshot records only each team's `ClubId`, so it cannot record
> *which eighteen of the twenty-five*. Restoring through the raw provider handed
> `ReprojectDistinctSquads` the full roster, it re-ran `LineupSelector` over a different candidate set,
> and a different eleven's canonical attribute records landed on the pitch — ClubId matching, size gate
> passing, every guard green, and the match silently diverging from the pre-save run. `SeasonSaveManager.Load`
> now rebuilds the career from the medical block in the *same file* and re-applies the filter through an
> `ISquadProvider` decorator, so restore re-selects from exactly the squad the match was configured with.
> Latent today (needs the occurrence dial armed AND a mid-match save) but armed for the interactive
> client, which is the current critical path. Locked by a 60-tick digest-chain continuation across the
> save — the only way to see WHICH eleven came back, since the attribute records are re-derived rather
> than serialized.
>
> **H3 — `LineupSelector.CanSelect` was a hand-copied second implementation of the selection walk.**
> Two answers to "which eleven does this squad field", nothing keeping them in step, and no equivalence
> test. The first rule added to `Select` — #44's ban filter is the near one — would have left
> `CanSelect` answering the old question, and the availability filter's press-back-in loop would then
> exit on a squad `ConfigureSquads` refuses. That is the parallel-surface trap `SquadRating`'s own doc
> says it exists to prevent, reintroduced one file below it. Collapsed to one walk (`TrySelect`), with
> `Select` and `CanSelect` as its two wrappers, plus an equivalence lock across five squad shapes and
> all three formations — including the case a player-count rule cannot see: eighteen fit outfielders
> and no goalkeeper.
>
> **The four Mediums.** (1) `RollToNextSeason` wrote the roster reconciliation *before*
> `BeginNextSeason` — the one commit this method's own docs call fallible — so a refused roll left a
> career reconciled against a season that never began, flatly contradicting the comment that claimed
> the opposite. `SyncToRoster` now splits into `PrepareRosterSync` (pure, throws) + `CommitRosterSync`
> (cannot fail), staged at (d′) and installed after (e); "refused ⇒ nothing moved" is now true of both
> sides, which no single placement could achieve. (2) Nothing checked that the career covered the
> season's clubs — a subset career constructed happily, advanced days happily, then threw from the
> filter on fixture 3 of 10 with two results already applied to the table. The constructor now refuses
> the pairing, the same argument that puts the KD-4 invariant there. (3) `ScheduleFor` handed out a
> handle bound to arrays `SyncToRoster` replaces, so a screen caching it across a boundary lost every
> focus change with `TrySetFocus` still returning `true`; a `RosterGeneration` counter makes that
> detectable. (4) `TrainingBlocks()`/`MedicalBlocks()` returned live mutable state arrays through the
> public `Career` property, making any holder a second writer of #29/#41 state — the single-writer
> property `SeasonState` enforces with `internal` mutators, dropped. Both accessors are now `internal`,
> with a `SeasonSaveManager.Save(SeasonLoop, match, path)` overload for external callers, and
> `SeasonLoop.World` went `internal` for the same reason rather than widening the surface to serve it.
>
> **Lows:** the risk read is skipped when the occurrence dial is off (nothing consumes it); the
> `SelectAvailable` summary no longer contradicts its own back-fill paragraph; the day steps now record
> *why* they are not validate-all-then-write while the sync is (their per-day idempotency makes a retry
> safe); and the boundary test covers insertion as well as removal.
>
> **Determinism unchanged by the pass**: no `SNAPSHOT_SCHEMA_VERSION` change, no format bump, no new RNG
> stream / domain tag / draw site / draw-order change. **Still no gate run** — no .NET SDK, installer
> still 403 at the proxy. Every finding above is static reasoning; the two things static reasoning is
> worst at remain open, and both are now the first thing CI will answer: whether the six suites compile,
> and whether the digest-continuation lock behaves as predicted.)

> **Last Updated (prior):** August 6, 2026, later same day (**#29/#41 T2 LANDED — the two subsystems now
> PRODUCE state. `PlayerCareerStates` is the #30-side owner T1 was missing: at T1 both codecs existed
> and nothing constructed a state set for them to encode, so every save carried two empty blocks.**
>
> **What is live.** `src/season-save/PlayerCareerStates.cs` holds both per-club sets keyed by
> `(ClubId, PlayerId)` and is the single place #30 calls either subsystem from. `SeasonLoop` takes it
> and its squad provider as an optional PAIR and drives: **slot 2** (`TrainingStep.AdvanceTrainingDay`)
> and **slot 4** (`MedicalStep.AdvanceMedicalDay`) in the KD-2 order, both taking the world day BEFORE
> step 9's increment; the **FR-MD-023 availability filter** at the pre-declared ERR-030-009
> resolve→filter→configure position, on the quick-sim path as well as the engine one (#44's suspension
> view joins the same call); **#29's §3.3 match-entry fatigue** into a new four-argument
> `MatchEngine.ConfigureSquads`, seeded onto each starter's `AerobicPool` as `1 − fatigue`; and the
> **FR-TR-025 / FR-MD-025 roster reconciliation** at a new (d′) position in `RollToNextSeason`, before
> the commits so a refused roll leaves the career untouched too.
>
> **Behaviour-neutral on the defaults, and that is a property rather than a hope.** Every player starts
> on `Balanced`, whose daily load equals `FatigueDailyRecovery` **exactly**, so the training-fatigue
> accumulator never leaves 0, the projection hands the engine an all-rested array, and a match booted
> through the wiring is digest-identical to one booted without it — locked both ways
> (`AllZeroFatigue_IsDigestIdenticalToTheTwoArgumentOverload` and its counterpart
> `NonZeroFatigue_ReachesTheSimulation`, which asserts on POSITION because the reservoir is itself
> serialized and a digest would move even for a seam that were written and never read). The world tick
> stays byte-identical to a bare `WorldStore.AdvanceDay` (FR-SN-026): neither day step touches the
> world.
>
> **#41's occurrence draw ships DISARMED (FR-MD-027), on measurement rather than caution.** The fifth
> AR pass over T0 measured the daily probability through the real producer chain: ~23% for a freshly
> inserted player on his first day, ~43% half-fatigued, and exactly 0 forever on the default focus —
> two to three orders of magnitude out in both directions. KD-W1 forbids re-tuning ahead of the balance
> pass, so T2 wires the path and leaves the dial off; everything downstream of an injury (the filter,
> the depleted-squad press-back-in, the views) is live and tested against directly-constructed injured
> states, so arming it is a one-argument change. Both dial positions are locked — "off injures nobody"
> is satisfiable by a step that is never called, so the armed path is proven to reach the draw.
>
> **Two ERRs filed: ERR-029-006 and ERR-041-010** — the same finding in both siblings. The T2 seam text
> names #28 APIs and types `TacticalDirector.PlayerProgression` does not expose: §3.5/§4.3's batch
> `#28.AdvanceDay(worldDay, in trainingInputs)` (only the per-player `AdvanceDayForPlayer` exists, and
> #28's own slot-1 wiring is roadmap D1), and both FR-TR-025 and FR-MD-025's `RegenResult` /
> `RetirementResult`. The handoff half is resolved in substance by reconciling against the roster #30
> already holds — the same contract, keyed the same way, which starts inserting exactly the regens the
> moment #28 T2 produces them. **Slot 1 stays a null seam deliberately**: gathering a batch for a
> consumer with neither the API nor a call site is the phantom class this project refuses, and
> `ComputeTrainingInput` returns `Neutral` on both branches regardless.
>
> **Recorded, not fixed (the next pass starts here).** #41 §3.5 sources `MatchLoad` from "#30's fixture
> result"; #30 has no per-player appearance record, `AppearanceLoadWeight` is a non-zero `[GT]` (150),
> and neither sub-blob may carry a counter for it. `MatchLoad.None` is passed — inert while the dial is
> off, since `AssembleRiskScore` sits inside the `occurrenceEnabled` branch. Recomputing appearances
> from the fixture list is **not** equivalent: the availability filter changes who actually played, so
> a recompute diverges precisely in the seasons injuries matter. It needs a persisted home and a format
> decision, and it is due with the balance pass.
>
> **One design decision worth naming.** The availability filter needed a depleted-squad rule, and the
> obvious one — back-fill to a player count — is wrong: selection refuses a *position-incomplete*
> squad outright (KD-L3), so eighteen fit outfielders and no goalkeeper stops the season. The rule is
> instead "press the least-injured back in until the club can field the formation", asked of the
> engine's own selector through a new `SquadRating.CanFieldStartingEleven` / `LineupSelector.CanSelect`
> probe rather than answered by a second selection rule in `season-save` (the parallel-surface trap
> `SquadRating` exists to avoid). In the limit that is the whole squad — exactly the unfiltered
> behaviour — so the filter can never leave a club worse off than having no filter at all.
>
> **Determinism.** No `SNAPSHOT_SCHEMA_VERSION` change (the aerobic reservoir was already serialized —
> proven by a save/restore round-trip rather than asserted). No `SEASON_SAVE_FORMAT_VERSION` change and
> no sub-blob format bump: T2 fills blocks whose layout T1 pinned. **No new RNG stream, no new domain
> tag, no new draw site, no draw-order change** — #41's keyed occurrence draw is the one T0 already
> allocated, and it is reached only when the dial is armed.
>
> **Blast radius, checked.** No scenario with a hardcoded tick window or per-90 band is touched: with
> no career wired every path is unchanged, and with one wired on the defaults the match digest chain is
> identical. No A4a round-resolution re-fit is implied — the quick-sim's rating input only moves once a
> club actually has injured players, which requires the dial. No `FR-PO-052` question: the two day
> steps run on the world tick, not the 60 Hz path, and `ConfigureSquads` gained one array read per
> starter at boot.
>
> **NO GATE RUN.** The authoring environment still has no .NET SDK and the installer is still 403 at
> the agent proxy, so `PlayerCareerStates`, the two new probes, the `ConfigureSquads` overload and all
> four new suites are written and unexecuted. CI on push is the gate — the same posture as T0 and T1,
> both of which then came back green on the first run.)
> **Last Updated (prior):** August 7, 2026 (**Unity client P5a — the UGUI shell's decisions extracted
> host-free, and P5 split into P5a / P5b to make that a phase.** The split is §12 rule 1 applied to a
> phase that had never had it applied: P4 was split on the argument that every decision the render
> skin makes belongs in a gate-compiled assembly, and "the UGUI shell" as one host-only phase would
> have put *when a control is available* and *what the speed buttons offer* inside `MonoBehaviour`s
> the CI gate can never compile — the exact leak AR-P4a2-H1 found sitting inside the deliverable built
> to close that leak. Landed in `match-client-core`: **`PlaybackSpeedLadder`**, the four `[GT]`
> playback multipliers as an *ordered* ladder with the opening rung named and the end behaviour
> decided — the catalogue held four independent dials and said nothing about which a match opens at or
> what "faster" does at 10×; stepping **clamps rather than wraps**, because a faster-click at the top
> that dropped the viewer to 1× reads as a fault rather than a limit, and pause stays off the ladder
> because it is a streamer state, not a multiplier. And **`MatchControlAvailability`** +
> **`MatchControlLockReason`**, which resolve §5-P5's standing requirement — "the UI gates tactical
> input at full time so a click does not silently no-op" — into three states each carrying *why* it is
> locked. **Two decisions inside that type are the kind a later tidy-up reverses, so both are
> test-locked:** saving stays enabled at full time (§6.3 — a finished match is precisely when a viewer
> wants to save, and the `ServiceOnce()` seam exists so the capture needs no tick; locking save
> alongside the tactical controls would make a completed match unsaveable), and a frameless streamer
> does **not** resolve to `Live` — `TryGetLatestFrame`'s out-parameter on a false return is
> `default(LiveMatchFrame)`, whose `MatchEnded` is *false*, so a resolver reading the frame
> unconditionally would report a match that has not started as fully interactive. The type also
> documents at length that it is §6.2's **best-effort early-out and not the guarantee** — the sim side
> reads the engine's live `_matchEnded` and is the authority — so that nobody later deletes a sim-side
> guard on the grounds that the UI checks it, which would leave the trailing half holding the
> invariant. **The one finding is the §5-P0 cap note turned from prose into an assertion:** that note
> required `MatchViewerConstants.MaxLiveSpeedMultiplier ≥ 10` so 10× is not refused, and nothing
> enforced it. Because `SetSpeedMultiplier` fail-louds rather than clamping, a cap configured below a
> step would have surfaced as *one speed button throwing mid-match while the other three worked* — a
> partial failure, which is harder to spot than a total one. `RequireStreamerAcceptsSpeed` now pairs
> each speed against the streamer's `[Min, Max]` at load, in the shape of the existing
> `RequireFarRayMeetsGround` cross-dial check, so the process refuses to start instead; the tests
> express the bounds relative to the cap rather than as literals, so a retune keeps them meaningful.
> **Recorded, not built — and it is a layering decision, not an omission:** the four screens'
> `ScreenId` catalogue and navigation graph has no correct home today. FR-UI-010 is explicit that the
> framework hard-codes no screen, so it does not belong in `ui-framework`; and `ui-framework` sits
> *above* `match-client-core`, so the core cannot hold a `ScreenId` either. The remaining candidates
> are `match-client-unity` (gate-invisible — wrong by rule 1) or a new assembly above `ui-framework`.
> That is the same question roadmap §6 item 2 already flags for C3's management screens, and it wants
> owner sign-off rather than an implementation-pass guess. **Determinism: no `SNAPSHOT_SCHEMA_VERSION`
> change, no new RNG stream, domain tag, draw site or draw-order change — nothing in this landing
> reaches the simulation.** No ERR filed: the cap gap was a missing *enforcement* of an existing design
> note, not a contradiction in one. **Blast radius checked and nothing moved** — no behaviour change
> reaches the engine, so no scenario tick window, no per-90 rate band, no A4a corpus fit and no
> FR-PO-052 perf baseline is perturbed. **Full dotnet gate NOT RUNNABLE in this environment, and this
> time the block was re-tested rather than assumed:** there is no .NET SDK, and every SDK binary host
> — `dot.net`, `builds.dotnet.microsoft.com`, `dotnetcli.azureedge.net` — returns 403 at the agent
> proxy, though the install script itself is reachable from GitHub raw, so the block is on the
> binaries and not the script. CI on push is therefore the only compiler for this landing, exactly as
> for #29/#41 T0 and T1. In place of the gate: type-name/filename match and brace balance on all five
> new files, a CS0104 collision sweep over the newly-imported `TacticalDirector.MatchViewer`
> namespace (12 public types, none colliding — this repo has paid for that one before with five
> `TacticTranslation` types in scope at once), confirmation that `MatchViewerConstants` is public and
> that `Scoreline`/`RestartBanner`/`LiveAgentCue`/`MatchPeriod` are `default`-constructible value
> types, `using`-group order, the FR-CS-002 `s_` private-static-field rename, and
> `generate_projects.py` regenerated clean at 64 csproj. `match-client-core` 135 → ~157 expected.)

> **Last Updated (prior):** August 7, 2026 (**ERR-008-021 gate run — PASSED. The shot-lane weighting, the
> AR-1 fixes and all 7 new locks compiled and executed for the first time.** PR #305, CI run 404,
> head `3f207ee`. Build 0 errors (5 warnings, the known count); `DecisionTree.Tests` **120 passed /
> 0 failed / 4 skipped / 124 total**, carrying the 7 `ShotLane_*` locks — including the H-1
> regression lock (an in-band defender who is not the GK candidate IS weighted) and the exact
> GK-arc pin. Whole-tree gate PASSED with the quarantine empty; **`MatchEngine.Tests` 420/430
> unchanged**, so the intended digest movement tripped no goal-rate band or tick-window scenario —
> the blast-radius caution in the two entries below resolves as "checked by execution, nothing
> moved". Retires the "gate NOT runnable" caveats on both ERR-008-021 entries; CI on push remains
> the only compiler for this work. Prior entry below.)

> **Last Updated (prior):** August 6, 2026, latest (**ERR-008-021 AR-1 — the same-day adversarial review over
> the shot-lane landing: 1 High, 7 Medium, 5 Low, all fixed.** The High: the landed P3 exemption
> keyed on the 6 m GK band, not the goalkeeper — so EVERY near-goal defender escaped the new ability
> weighting, leaving it inert precisely where shots are blocked (for a 10 m shot, most of the usable
> path), and all six of the landing's fixtures sat 8 m off the goal line so no lock registered it.
> Fixed to a single **GK candidate** (goal-line-nearest visible opponent within the band,
> snapshot-order tie-break, independent of the shot-path filter); every other blocker — in the band
> or not — is now weighted, while the radius stays per-band (the recorded §3.2.3.2 Stage-0
> limitation), so neutral-case arcs are unchanged. Also corrected: the P5 "today's arcs bit-for-bit"
> overclaim (exact only at the ability midpoint raw 10/11 or under a null view; the all-default
> 10/10 squad reads ≈ 0.979 — the same overclaim shape retracted for ERR-008-019 a day earlier);
> margin-less discrimination locks; three vacuously-passable equality locks; the Vision-fidelity
> expression duplicated across both lanes (hoisted to `VisionFidelity`); both away mirrors running a
> goal-post L/R assignment production never builds; the Known-limitation paragraph's inverted radius
> consequence; and the §3.1.4.1 gate boundary (code generated at exactly `MIN_GOAL_VISIBILITY`
> against the spec's strict ">"). New H-1 regression lock: an in-band defender who is not the
> candidate IS weighted (keeper claims the slot from wide of the shot wedge). Surfaces:
> `OptionGenerator.cs` v1.8, `OptionGeneratorTests.cs` v1.8 (7 shot-lane locks),
> `UtilityWeights.cs` v1.11 (doc only), `section-3-1.md` v1.5, `section-3-2.md` v1.13,
> `spec-error-log.md` v1.68. **Gate NOT runnable in the authoring environment; CI on push is the
> gate.** Prior entry below.)

> **Last Updated (prior):** August 6, 2026, later again (**ERR-008-021 — the shot-lane follow-up deferred at
> the ERR-008-020 landing, closed.** #8 §3.1.4.3/§3.2.3.2's goal-occlusion sum was attribute-blind:
> every outfield blocker in the shot path occluded the same geometric arc whoever he was, so a
> Pace/Anticipation 1/1 defender walled off the goal exactly as hard as a 20/20 one, and no shooter
> attribute entered the read (pattern (a); already continuous in position, so no P1 cliff to kill).
> Fixed per judgment-proxy doctrine P2/P3/P5 as §3.2.3.2 **step 3a**: each OUTFIELD blocker's arc ×
> §3.1.3.3's `perceived_ability` (Anticipation/Pace → 0.6..1.4, read through the SHOOTER's Vision
> fidelity) — **no new constants**, the ERR-008-020 `[GT]`s reused verbatim so one lever calibrates
> both lanes at the eventual KD-W1 pass; the GOALKEEPER's arc stays purely geometric (P3 — keeper
> quality is priced once, at the #11 save; `GK_BLOCKER_RADIUS` is an abstraction of coverage, not a
> body). League-average / null-view ability = 1.0 reproduces today's arcs exactly (P5 pivot).
> Spec + code same commit: `section-3-1.md` v1.4, `section-3-2.md` v1.12 (+ step 3a and its worked
> example in `section-3-2-3-to-3-2-9.md`), `OptionGenerator.cs` v1.7, 6 new `OptionGeneratorTests`
> locks (computed-average pivot = null-view arc exactly, Vision-20 vs Vision-1 discrimination,
> null-view neutrality, GK-arc invariance, away mirror). Adjacent defect recorded-not-fixed:
> §3.2.3.2's numerical example is in a legacy centre-origin frame and its blocker classifies as GK
> under the section's own heuristic yet uses the outfield radius (annotated in place; its 0.757
> feeds the §3.2.3.3 chain). No schema change, no new RNG stream / domain tag / draw site, no
> draw-order change; digests move where a generated SHOOT has a non-neutral outfield blocker in the
> path, as intended. Blast radius checked: no scenario band or tick-window instrument reads
> `GoalOpeningScore` directly; goal-rate-sensitive locks may shift on affected seeds — CI will show
> it. **Gate NOT runnable in the authoring environment (no .NET SDK; installer 403 at the proxy);
> CI on push is the gate.** Prior entry below.)

> **Last Updated (prior):** August 6, 2026, later same day (**#29/#41 T1 gate run — PASSED. The two save
> codecs, the three new types and the frame change compiled for the first time; all 58 new tests
> executed and passed.** PR #300, CI run 397, head `9a7f703`. Build 0 errors (5 warnings, not shown to
> be new); `TrainingSystem.Tests` **52/52**, `InjuriesMedical.Tests` **66/66**, 0 skipped in either;
> `SeasonSave.Tests` **267 passed / 3 skipped / 270**, carrying the 7 new `SeasonSaveManagerTests`.
> Whole-tree gate PASSED with the quarantine empty, `MatchEngine.Tests` 420/430 unchanged.
>
> **Nothing needed a fix.** Zero compile errors and zero test failures on the first run, exactly as at
> the T0 gate — and that is the whole result for three things the last two landings changed and had no
> way to check: the `in TrainingBlock` / `in MedicalBlock` signature change at every
> `SeasonSaveCodec.Encode` call site, the byte offsets throughout both codec suites after the leading
> `*_SAVE_MAGIC` shifted every one of them, and `SaveBlobFramingHelpers` under `TreatWarningsAsErrors`.
>
> **What the run retires:** the "no gate run" caveat on both T1 entries below. The adversarial review
> in particular was written end to end against code that had never been compiled, including the
> two-layer fix for the mutual-decode defect — whose load-time half, the `*_SAVE_MAGIC` gate, had been
> exercised only by a byte-exact Python model and never by a compiler refusing a foreign block. Those
> claims now hold by execution.
>
> **Worth recording about the run itself:** it took 36 minutes with the other ten checks long green,
> which reads as a hung job from outside. It was not — `MatchEngine.Tests` alone runs **35 m 30 s**,
> and the quarantine being empty means the full suite was enforced rather than a report-only subset.
> The authoring environment still has no .NET SDK, the installer still 403 at the proxy, so CI remains
> the only compiler available for this work.)

> **Last Updated (prior):** August 6, 2026, later same day (**Adversarial review over the #29/#41 T1 landing —
> 2 High, 2 Medium, 3 Low, all fixed.** The headline is a defect that exists *because* ERR-029-004
> succeeded. Pinning #29's byte layout to match #41's made the two blocks byte-for-byte the same shape,
> and **every sub-blob format in the save stack sits at version 1** — `TRAINING_SAVE_FORMAT_VERSION`,
> `MEDICAL_SAVE_FORMAT_VERSION`, `SEASON_STATE_FORMAT_VERSION`, `MATCH_SAVE_FORMAT_VERSION`,
> `PROGRESSION_SAVE_FORMAT_VERSION`. A version gate therefore separates one *generation* of a format
> from the next and **never one format from another**, so each codec decoded the other's bytes cleanly,
> completely and silently: severity tiers arrived as training focuses, recovery counters as conditioning
> cursors, injury counts as training fatigue, every gate green and no trailing byte. Proven by executing
> a byte-exact model of both formats in **both directions** before the fix — the reverse case is a squad
> on Fitness/Technical focus with a healthy `Condition` reading back as a squad carrying
> Moderate/Serious injuries with thousands of recovery days, F1 coherence satisfied throughout. The
> trigger, transposing two arguments in `SeasonSaveCodec.Encode`'s list of five consecutive `byte[]`,
> had no compile-time signal either. **Fixed in two layers** (`ERR-029-005` / `ERR-041-009`): each block
> now writes a self-identifying `*_SAVE_MAGIC` first and refuses a foreign one on decode, and the
> frame's two confusable parameters become the typed `TrainingBlock` / `MedicalBlock`, making the
> transposition a build error. Deliberately **not** an RNG domain tag — those name draw domains and must
> stay free to change independently of a save format. The general rule is now a MUST in both §4.4
> sections: **a format version is not a format identifier.** Second High, same shape one layer up:
> `SeasonSaveManager.Save`'s `trainingClubs`/`medicalClubs` defaulted to null-meaning-empty, so at T2 a
> call site omitting them would compile, save, and load back empty arrays indistinguishable from an
> unwired game — a season of conditioning and injury history gone with nothing thrown and no assertion
> able to fire. Both are now required and reject null; `Array.Empty` is how a caller *says* "no training
> state". **Mediums:** `TrainingSaveCodec.Encode` had no encode-side value gates while its sibling did
> and documented why, so it could write a file its own `Decode` refuses (an unloadable save is data
> loss); and the two codecs' framing helpers — `CanonicalOrder`, `RequireAscending`, `ReadCount`,
> `Require` — were duplicated verbatim on day one, which is how the first Medium arose in the first
> place, so they are hoisted to `SaveBlobFramingHelpers` in `deterministic-sim`. The three older codecs
> keep their own copies: retrofitting them is scope this pass did not take. **Lows:** a `Value: 2` in a
> constant whose value is 3, a "three sub-blobs" that is now five, two stale file-header Purpose blocks,
> a `KD-7 blob independence` citation that should be `KD-2` (KD-7 is the codec/disk-I/O split), and each
> sibling suite testing only half the ordering gate the other tested. **No format-version bump and no
> `SEASON_SAVE_FORMAT_VERSION` bump:** neither sub-blob format has ever been written to a real save
> (nothing constructs either state set until T2), and the *frame* layout is untouched — only the
> contents of two blocks the frame treats as opaque. **STILL NO GATE RUN** — no .NET SDK in the
> authoring environment, installer still 403 at the proxy. Everything above, including the fixes, is
> written and unexecuted apart from the format cross-decode, which was proven outside C#.)

> **Last Updated (prior):** August 6, 2026 (**#29 / #41 T1 — the two save codecs, composed into #30's season
> save.** `TrainingSaveCodec` (`TRAINING_SAVE_FORMAT_VERSION` = 1) and `MedicalSaveCodec`
> (`MEDICAL_SAVE_FORMAT_VERSION` = 1), each an opaque independently version-gated sub-blob, now ride in
> the season frame between the season block and the optional match block —
> `SEASON_SAVE_FORMAT_VERSION` **2 → 3**. Both are **mandatory** rather than presence-flagged: unlike a
> match, training and medical state has no "absent" case, only an empty one, which each codec writes as
> a well-formed zero-club block. That choice is what stops T2 needing a second frame bump the day the
> producers are wired.
>
> **Order is not state.** The blocks are maps keyed by `(ClubId, PlayerId)`, so encode canonicalizes to
> ascending keys and decode requires them. Two equal state sets therefore produce identical bytes
> whatever roster order the caller holds them in — a save written after a squad-list reshuffle is
> byte-identical to one written before — and a duplicate key fails loud at encode rather than reaching
> the file with no defined winner.
>
> **Two ERRs filed and resolved, spec + code same commit.** **ERR-029-004:** #29 §4.4 described the
> sub-blob's posture and never a field of its layout, while #41 §4.4 pinned its own in full — and F3
> refuses every cross-version migration, so the first written layout is the format permanently. New
> §4.4.1 pins it. **ERR-041-008:** #41 §4.4's layout groups blocks by club without naming one, so club
> identity would cross a save boundary by list order alone — an implicit agreement with a sibling
> sub-blob its own KD-7 forbids this codec to read. `ClubId` is now written in both specs and both
> codecs. Both entries also fix their §2.3 **F3** row, which named `ArgumentException` while citing the
> `MatchSaveCodec` posture — and that codec throws `InvalidOperationException`.
>
> **Deliberately not done:** T2. Nothing constructs either state set yet, so `SeasonSaveManager.Save`
> substitutes the empty set and every save written today carries two empty blocks. The #30 tick-order
> slots, the `IsAvailable` read into squad selection and the FR-TR-025 / FR-MD-025 roster handoff stay
> open. Also unchanged: `[GT]` bands are gated on neither codec's decode — enforcing `ConditionMax` or
> `RecoveryMax` at load would turn a designer's ceiling edit into data loss across every existing save.
>
> **NO GATE RUN.** The authoring environment still has no .NET SDK (the installer is still 403 at the
> proxy), so two production codecs, two container types, the frame change and ~40 new tests are written
> and never compiled — the same posture the T0 landing shipped under, and the same one CI retired for it
> on push. `tools/dotnet-ci/generate_projects.py` runs clean (64 projects, every asmdef reference
> resolved) and `tools/unity-ci/check-meta-integrity.sh` passes.)

> **Last Updated (prior):** August 5, 2026, end of same day (**ERR-008-019 — the full-range
> digest-invariance claim is RETRACTED (adversarial review over the landing).** Documentation
> only: no formula, constant, test or behaviour change, and the code is byte-identical to the
> entry below. The argument recorded in five places — "any generator-reachable MIDFIELD SHOOT
> needs ≥ ~34.5 m of range, and only raw 20's gate reaches it, where ramp = step" — rested on the
> shooter sitting within Ball Physics #1 §3.1.11.1 `CheckPossession`'s **0.5 m** `ControlRadius`.
> That is not a production possession-granting path in this engine. The two that are:
> `MatchEngine.RunLooseBallPickup` (§5.Z Phase H, KD-H3), which grants possession to the nearest
> eligible agent within `MatchEngineConstants.LooseBallPickupRadiusM` = **1.0 m** of a loose ball
> at rest and **leaves the ball where it lies**, and the first-touch path
> (`FIRST_TOUCH_ACCEPTANCE_RADIUS_M` = 1.0 m). After the grant **nothing re-anchors the ball to
> the holder or releases possession on separation** — the holder moves freely under dispatched
> `MoveTo` commands and the executors' only entry check is the possession id — so separation at a
> decision tick reaches 1.0 m. Corrected: a MIDFIELD ball at x → 70⁻ with the holder goal-side
> puts the shooter just above **34.0 m**, **inside** raw 19's range gate (20 + (18/19) × 15 =
> 34.21 m), where the full-range ramp gives ≈ **0.524** against the old step's 0.55. A generated
> option can score differently, so invariance is **not established** and is likely false on seeds
> realizing that state. **The behaviour change itself is owner-directed and intended** — this
> retracts a claim, not a decision; P5 (uniform-population mean 0.30), all four test locks and the
> worked examples stand. The superseded narrow ramp (half-width 0.05) **survives** the corrected
> premise: its band caps at 29.0 m, still disjoint from > 34.0 m — the 0.5 m premise error is
> smaller than that margin and larger than the full-range form's 0.3 m one, which is exactly why
> one claim holds and the other fails. Also fixed (Low): `LONG_SHOT_RAMP_HALF_WIDTH`'s XML doc
> advertised a (0, 0.25] range the suite forbids below 0.25 — that range is the formula's validity
> domain, not a free dial (`UtilityWeights.cs` v1.10). Surfaces synced: #8 §3.2.3.1 +
> `section-3-2.md` v1.11, `spec-error-log.md` v1.63 (head + entry + index row),
> `football-judgment-proxy-review.md`, `open-issues.md`, `CLAUDE.md`, `CHANGELOG-src.md` v2.72,
> `file-manifest.md`, `README.md`. **Gate NOT run — no .NET SDK in the authoring environment; CI
> compiles on push.** Prior entry below.)

> **Last Updated (prior):** August 5, 2026, even later same day (**ERR-008-019 owner revision — the
> long-shot ramp widened to the FULL attribute range.** The owner directed the scaling to run
> over the whole LongShots range, not the initial 8–13 band; the metres-based §3.1.4.2 range
> gate already scales raw 1–20, so the instruction lands on the §3.2.3.1 zone-modifier ramp.
> One `[GT]` value: `LONG_SHOT_RAMP_HALF_WIDTH` 0.05 → **0.25**, its maximum valid value — the
> ramp spans the whole shifted domain [0.5, 1.0] and `t` reduces to `A_LongShots` exactly.
> Raw 1 is exactly 0.05, raw 20 exactly 0.55, and every raw point between moves the modifier
> ≈ 0.026: **no plateau anywhere**. P5 survives the revision (the midpoint stays at the old
> cliff and the uniform-population mean is 0.30 under the step, the narrow ramp, and the full
> ramp alike), and so does digest invariance, in tighter form: the full ramp differs from the
> step at every rating except raw 20 — and raw 20 (range 35.0 m) is the only rating whose
> range gate reaches the ≥ ~34.5 m a MIDFIELD SHOOT requires (raw 19 caps at 34.2 m), where
> the ramp equals the step. **Still no digest moves.** Spec: §3.2.3.1 constants + note,
> §3.2.3.4 re-derived, Case B recomputed 0.200 → 0.162, `section-3-2.md` v1.10 footnote.
> Code: `UtilityWeights.cs` v1.9 (the value), `UtilityScorer.cs` v1.15 (comment only — the
> formula was built for this). Tests v1.9: the M-4 lock is now
> `ShootMidfield_RampRunsInShiftedForm` (raw 10 computed ratio; the raw form suppresses raw 10
> to SHORT) and `ShootMidfield_FullRangeRamp_EndpointsExact_AndStrictlyMonotone` replaces the
> plateau-equality assertions, which were the exact opposite of the owner's instruction.
> `spec-error-log.md` v1.62. **Gate NOT run — no .NET SDK in the authoring environment; CI
> compiles on push.** Prior entry below.)

> **Last Updated (prior):** August 5, 2026, latest same day (**ERR-008-019 LANDED — the second fix under the
> football-judgment remediation doctrine, closing the review's founding finding.** #8 §3.2.3.1's
> midfield `ZoneModifier_SHOOT` was a hard step on shifted LongShots — 0.55 strictly above
> `LONG_SHOT_THRESHOLD`, 0.05 at or below, an **11× jump across one raw attribute point** — the
> original pattern-(b) cliff the whole judgment-proxy review was named after, and the finding whose
> earlier "FIXED … gate green" record was verified false at the ERR-008-020 landing. Landed now
> under the id soft-reserved for it, re-verified free as required. Fix per doctrine P1/P5: a linear
> ramp in the unchanged shifted form, centred on the old threshold with new
> `[GT] LONG_SHOT_RAMP_HALF_WIDTH` = 0.05 — full suppression at raw ≤ 8, full long-shot modifier at
> raw ≥ 13, the exact SHORT/LONG midpoint at the old cliff, so endpoints and the
> population-integrated modifier reproduce the old behaviour (the -020 centred-ramp precedent,
> locked by test). P2/P3 deliberately out of scope: long-shot inclination is the shooter's own
> execution capability, not a recognition judgment — no fidelity term, no new attribute. **The
> branch is production-unreachable in the only band the fix changes** (the ramp differs from the old step only at A_LongShots ≤ 0.6, whose §3.1.4.2 range gate caps at 29.0 m, while a generator-reachable MIDFIELD SHOOT needs ≥ ~34.5 m of range — disjoint bands, so no generated option ever scores differently; ERR-008-017's stale "≥ 40 m" figure — written after the ERR-008-016 equal-thirds correction — fixed in passing), so the cliff was latent and **no digest moves on any seed** — no
> scenario re-anchoring, unlike -020's blast-radius note; landed anyway per the standing
> wrong-shaped-model posture. Spec (`section-3-2-3-to-3-2-9.md` §3.2.3.1 + §3.2.3.4 re-derived as
> ramp bands; `section-3-2.md` v1.9 footnote) + code (`UtilityScorer.cs` v1.14,
> `UtilityWeights.cs` v1.8) + 4 new `UtilityScorerTests` locks (no-cliff, exact midpoint pivot,
> endpoint clamps, monotonicity) + the AR-2 M-4 lock refitted raw 12 → 14, same commit;
> `spec-error-log.md` v1.61. Review tally: **2 fixed, 32 open.** No schema change, no new RNG
> stream / domain tag / draw site, no draw-order change. **Gate NOT run — no .NET SDK in the
> authoring environment; CI runs it on push.** Prior entry below.)

> **Last Updated (prior):** August 5, 2026, later same day (**#29/#41 gate run — PASSED. Both assemblies
> compiled for the first time; all 67 of their tests executed and passed.** PR #299, CI run 394, head
> `ddbbe58`. Build 0 errors; `TrainingSystem.Tests` 27/27, `InjuriesMedical.Tests` 40/40, 0 skipped in
> either; whole-tree gate PASSED with the quarantine empty, `MatchEngine.Tests` 420/430 unchanged.
>
> **The PR had to be unblocked first, and the reason is worth recording:** #299 was conflicted against
> `main`, and GitHub cannot construct the merge ref for a conflicted PR, so the `pull_request` workflow
> never fired at all. The gate was not slow or flaky — it had never been *asked* to run. Merging `main`
> resolved five chain-append conflicts (both branches prepending to "Last Updated" chains; both sides
> kept everywhere) plus two genuine collisions, since the branches forked at `2.60`/`v1.56` and then
> allocated the same numbers independently: `CHANGELOG-src` 2.61–2.64 (main's kept, this branch's
> renumbered 2.65–2.68) and `spec-error-log` v1.57 (main's ERR-008-020 kept; ERR-041-002/003 became
> v1.58/v1.59).
>
> **What the run retires:** every "the suite locks X" claim across the T0 landing and five adversarial
> review passes was, until now, a claim about code that had never been compiled — the never-compiled
> surface trap this file's own history records. No fix was needed to get green. Beyond compilation it
> confirms Appendix B day by day, #41 §3.6 term by term, the keyed-draw separation, and AR pass 5's
> hand-computed occurrence-probability baseline (231/0/431 per-mille), which had been derived by
> mirroring the C# in Python against a tree that could not be built.
>
> The authoring environment still has no .NET SDK — the installer is still 403 at the agent proxy,
> re-checked here — so CI remains the only compiler for this work.)

> **Last Updated (prior):** August 5, 2026, later same day (**Adversarial review over the #29/#41 T0 landing —
> 2 High, 4 Medium, 4 Low, all fixed; converged on pass 2.** Both Highs were the same shape: a design
> that made a silent wrong answer reachable, guarded by a test that could not fail.
>
> **H-1 — one contract value, two config keys, and a lock wired to nothing.** `InjuryRiskMax` was
> declared `[GT]` in BOTH catalogues, under `[training-system]` and `[injuries-medical]`. #41 §3.4
> passes #29's `RiskScore` through with weight 1 and compares it against a draw whose denominator is
> derived from that ceiling, so setting one key without the other rescales every occurrence probability
> and #29's clamped maximum stops meaning "certain". The equality test written to catch exactly that
> passed unconditionally, because the gate leaves `GameplayConfigHolder` unbound and both sides return
> their fallback. Fixed by re-tagging #41's row `[CROSS]` and mirroring #29's — **ERR-041-003**.
>
> **H-2 — a focus command that could write another club's player.** `TrainingStep.SetFocus(int[] ids,
> TrainingState[] states, …)` took the pair as separate arguments and checked only that the lengths
> matched — and every club in a generated league has the same squad size, so passing club A's ids with
> club B's states resolved the player against A and wrote B. No exception, wrong player, wrong club,
> and it would have persisted at T1. The command moves onto `TrainingSchedule.TrySetFocus`, which binds
> the pair once at construction, so there is no argument a caller can supply to reach it. Locked by a
> test that fails against the old signature.
>
> **The Mediums:** a `MedicalModifier` gate that rejected zero but not negative (a negative recovery
> speed one-days a Serious injury; a negative occurrence multiplier clamps risk to zero and silently
> ends injuries forever — and #34 is the declared future producer of both); an F1 coherence check that
> structurally could not see a negative `RecoveryRemaining`, because "not recovering" and "healthy"
> look identical to an iff; **four tests that could not fail** (asserting the identity function is the
> identity, asserting a pure function is pure, and comparing two values that are equal by construction
> — the documented repo trap, with FR ids on them claiming coverage they did not provide); and the one
> cross-assembly contract in the whole landing — #29's `ComputeInjuryRisk` feeding #41's
> `AssembleRiskScore` — having **no test at all**.
>
> **Pass 2 caught two regressions in pass 1's own fixes**, which is the reason the loop re-reads
> everything rather than the diff: the replacement for one tautological test was *itself* tautological
> (`in` parameters cannot be mutated, so "this read does not mutate" is a compile-time guarantee), and
> the new seam test asserted something **false** — that #29's saturated maximum reaches #41's ceiling.
> It does not, and finding out why is the more useful half: **both specs mitigate on the same three
> physical attributes**, so a robust player is priced down twice and #41 always subtracts again on top
> of #29's already-mitigated value. Spec-faithful, since each spec mandates its own term, but it
> entangles the two `[GT]` tables and it means "maximum risk" never means certain occurrence. Recorded
> as an explicit assertion so the balance pass inherits the fact instead of rediscovering it.
>
> Pass 3 over the full surface of both assemblies surfaced no new High or Medium. **Still no gate run**
> — no .NET SDK, installer blocked by network policy — so every fix above is reviewed and unexecuted.)

> **Last Updated (prior):** August 5, 2026 (**#41 Injuries & Medical T0 landed — and #29 Training System T0
> with it, because #41 could not be built without it.** The task was the next spec after #29 in code
> implementation order; the roadmap's Phase D orders that as D2 #29 → D3 #41, and #41 §4.1 requires
> a reference to `TacticalDirector.TrainingSystem` for the one type it reads — `InjuryRiskContribution`,
> #29's already-published risk scalar (FR-TR-017 / FR-MD-009). That assembly did not exist. So #29 T0
> landed as the declared prerequisite rather than as a half-built stub, and the pair went in together:
> **two new host-free assemblies, `src/training-system/` and `src/injuries-medical/`, taking `src/` from
> 31 to 33** and the assembly-less-APPROVED-spec count from 22 down to 20.
>
> **#29 T0** — `TrainingFocus`, `TrainingState` (+ the `Create`-not-`default` sentinel discipline),
> `TrainingSchedule` as a genuine read-only VIEW over per-player focus rather than a stored copy
> (FR-TR-003), `CoachingModifier`, `InjuryRiskContribution`, `TrainingViewModel`, the four `TrainingStep`
> entry points (§3.1–§3.4) and the FR-TR-023 `SetFocus` command, plus the Appendix A catalogue. Appendix
> B's Fitness week is reproduced day by day as a test, including its `ProjectMatchEntryFatigue = 0.23`.
> No RNG anywhere: `_RESERVED_0x21_` / ordinal 83 stay reserved (KD-6).
>
> **#41 T0** — `InjurySeverity`, `InjuryState`, `MatchLoad`, `MedicalModifier` (explicit `Identity`, and
> `default` fails loud — its zero is ×0 risk and a divide-by-zero recovery scale), `MedicalViewModel`,
> `MedicalStep` (§3.1–§3.4: the recovery-then-draw day step, the keyed occurrence draw, the same-draw
> severity bucketing, the risk assembly), and the Appendix A catalogue. §3.6's worked example is pinned
> term by term — the risk assembly's 2900, the `draw 1500 ⇒ Minor` bucketing, the 7-day Minor tier — and
> the robustness table is calibrated so `mean 14 ⇒ 400` is exact rather than approximately reproduced.
>
> **Two findings, both filed** (`spec-error-log.md` v1.58). **ERR-041-002** is the consequential one and
> it is ERR-030-012's twin, reached independently from the same constraint: **#41 §2.2/§3.1 call
> `rng.DrawKeyed(...)` on `DeterministicRngService`, and no such method exists.** #16 exposes only the
> branch-safe reservation trio, whose draw value is keyed on an `ActionOrdinal` the service increments
> inside `Reserve` — nothing accepts a caller-supplied ordinal. The one shape that *is* implementable
> against today's API is cursor-positioned, which KD-1 of the same spec forbids: FR-MD-007 serializes no
> cursor precisely because every draw must be reproducible from `(playerId, worldDay, purpose)` alone.
> Resolved the way #30 resolved it — a local keyed SplitMix64 derivation, the
> `RoundResolutionModel.FixtureKey` precedent — so `AdvanceMedicalDay` takes `ulong worldSeed` in place
> of the service and registers no stream. **ERR-041-001 closes with it**: `DOMAIN_TAG_INJURIES_MEDICAL =
> 0x2A` lands in `DeterministicSimConstants` at that first draw site; `SubsystemOrdinals.InjuriesMedical
> = 92` is deliberately **not** allocated, because an ordinal with no registered stream behind it is the
> zero-consumer phantom FR-LW-031 forbids.
>
> **Not done, and named rather than implied:** T1 (both save codecs and the `SeasonSaveCodec`
> composition) and T2 (the #30 tick-order wiring, the availability read into squad selection, the
> FR-MD-025 / FR-TR-025 roster-membership handoff) are untouched. Both assemblies are inert — nothing
> constructs them, so the season loop is byte-identical to before this landing. #29's `ComputeTrainingInput`
> returns `TrainingInput.Neutral` on both branches, because #28's type still has no fields to populate;
> the deep branch is a marked seam, not a magnitude invented ahead of its consumer.
>
> **NO GATE RUN.** The authoring environment has no .NET SDK and the network policy blocks the
> installer (`builds.dotnet.microsoft.com` → 403 at the proxy), so 17 production files and 5 test files
> across two new assemblies are **written and never compiled** — precisely the defect class
> `tools/dotnet-ci` exists to catch. Every "the suite locks X" claim in this entry is a claim about test
> code that has not executed. First CI run on push is the real gate.)
> **Last Updated (prior):** August 5, 2026, later same day (**PR #298's first gate run: one failure — the
> snapshot-coverage guard, correctly — and two execution-verified confirmations.** The failure:
> `DecisionTree_InstanceFieldCount_MatchesCapturedSet`, the reflection lock that pins DecisionTree's
> field count so cross-tick state cannot silently skip the snapshot. ERR-008-020's
> `_allAgentAttributes` made it 11; the landing had made (and documented) the exclusion decision —
> injected dependency, host re-wires at boot/restore, the `_saveDispatch` class — but never updated
> the guard's ledger. Fixed: count 10 → 11 + the field recorded in the excluded class; no production
> change. **The confirmations, both by execution for the first time:** (1) `MatchEngine.Tests` 420
> passed / 0 failed — `RoundTrip_KeeperSubstitutedOntoOutfieldSlot_IsDeterministic`, red on `main`
> since the W1 merge, passes under the restore-resync fix; (2) all nine ERR-008-020 lane-model locks
> and the engine wiring lock pass on their first-ever compile. Once this push goes green, the PR
> carries a gate strictly better than `main`'s (which remains red until merged). Prior entry below.)

> **Last Updated (prior):** August 5, 2026 (**CI fix — main went red at the W1 merge, and the cause was the
> W1 AR-2 fix's own restore claim being false.** `RoundTrip_KeeperSubstitutedOntoOutfieldSlot_IsDeterministic`
> failed on `main` at `ba04d49` (and on both prior W1-branch runs): digest diverged at tick 151, the
> first post-restore tick. The v1.60 occupant-change fix argued `_gkAgentIds` needs no schema bump
> because it is "reconstructed rather than serialized, so restore re-derives it and sees no change" —
> half true. The boot-time derivation runs against the DEFAULT goalkeeper-flag layout;
> `DeserializeWorldState` then overwrites the flags with the SAVED layout; and whenever the two
> differ (this test substitutes a bench keeper onto an outfield slot at tick 50, saves at 150), the
> first post-restore `RefreshGkAgentIds` misreads the flag delta as a live occupant change and
> `ResetSlot`s #11 keeper state that was itself just restored — a wipe the uninterrupted run does
> not perform at that tick, because its reset fired back at the substitution and its state evolved
> since. Fixed in `MatchEngine.cs` v1.63: the keeper resolution extracted to `ResolveGkAgentId`,
> and `RestoreFromSnapshot` gains **step 3b — `ResyncGkAgentIdsAfterRestore`**, re-deriving the map
> from the restored flags **without** reset, since restored #11 per-slot state already belongs to
> the restored occupant. The live-path reset — the actual substitute-inheritance fix — is unchanged;
> this restores exactly the restore-transparency that existed before the reset was introduced. All
> restore paths route through the one factory (`MatchSession.RestoreFrom` → `MatchSaveManager` →
> `RestoreFromSnapshot`), so one fix covers all. Verification is the already-failing CI test; not
> runnable locally (no .NET SDK). `gk-rush-trigger-design.md` v1.4 supersedes the v1.3 claim.
> Prior entry below.)

> **Last Updated (prior):** August 4, 2026, latest same day (**ERR-008-020 adversarial review — 2 Medium,
> 1 Low, all fixed; pass 2 clean.** Both Mediums are lessons in what a lock is worth when it doesn't
> execute the thing it claims to lock. **M-1:** the landing's P5-pivot test asserted "an average
> defender counts exactly 1.0" through the *null-attribute-view guard* — the ability computation it
> exists to pin was never run for an average defender anywhere in the suite, so the spec's "MIN/MAX
> midpoint MUST equal 1.0" invariant was enforced by nothing and a `[GT]` retune could break the
> whole pivot-on-baseline argument silently. Now locked twice: a computed-path pivot (Anticipation
> 10 + Pace 11, whose normalised mean is 0.5 *exactly*) and a constants midpoint invariant. **M-2:**
> the engine wiring had no detector, and the model's null fallback is silent *by design* — dropping
> the one `SetAllAgentAttributes` boot call would revert every match to attribute-blind lane pricing
> with every test green, the wiring-backlog gate-level-dormancy class this repo documents as its top
> defect shape. Now `DecisionTree.HasSquadAttributeView` + an engine `TestOnly` sweep +
> `MatchEngineSquadTests` construction lock. **L:** the elite-vs-poor discrimination margins were a
> hardcoded 0.15; now derived from the constants (half the true `(MAX−MIN)/DIVISOR` gap), so a
> legitimate retune shrinks the margin instead of false-failing the suite. Production delta is two
> read-only accessors — no digest, schema, RNG, or draw-order surface. Nine locks now cover the
> model across two suites. **Gate still NOT runnable here (no .NET SDK); CI on this push is the
> first compile.** Prior entry below.)

> **Last Updated (prior):** August 4, 2026, latest same day (**ERR-008-020 — the doctrine's template fix
> landed: the pass lane learns who the defender is, and a false "FIXED" claim is corrected.** First
> fix under `football-judgment-proxy-review.md` §6, exactly as converged: #8 §3.1.3.3's binary 0.8 m
> `is_interceptor` corridor — 2 cm of defender position stepped `PassLaneScore` by 0.33, and no
> defender attribute entered the judgment, so a Pace/Anticipation 1/1 defender priced a lane
> identically to a 20/20 one — becomes a continuous per-opponent threat weight: linear falloff
> (core 0.4 m [GT], zero at 1.2 m [GT], **ramp centred on the old cliff so integrated threat is
> preserved and the neutral verification rows reproduce exactly** — doctrine P5, locked by test) ×
> defender Anticipation+Pace ability (0.6–1.4 [GT], average ⇒ exactly 1.0) read through the passer's
> **Vision as discrimination fidelity** (P2: `perceived = 1 + fidelity × (true − 1)`, floor 0.2 [GT]
> — a Vision-1 passer reads everyone as near-average, which IS the pre-fix engine; §3.2.2's Vision
> term untouched, P3 no double-count). Plumbing: `DecisionTree.SetAllAgentAttributes` boot seam (the
> `SetMatchSeed` pattern) carries the engine's live `_dtAttrs` reference into `DecisionContext` —
> substitutions visible through it; null view ⇒ ability-neutral, never an exception. Spec §3.1.3.3
> rewritten (v1.3, worked example + verification table), shot lane §3.1.4.3 deferred with a scope
> note (owner call), `spec-error-log.md` → v1.57, 6 `OptionGeneratorTests` locks incl. the away-side
> mirror. No `SNAPSHOT_SCHEMA_VERSION` change (the view is an injected dependency, excluded from
> `CaptureState`), no new RNG stream / domain tag / draw site, no draw-order change; digests move
> for any match with a PASS candidate near a defender, as intended. **Blast radius recorded:** every
> tick-window/rate-band instrument may shift on its seeds and cannot be checked here; the A4a
> round-resolution fit needs its Step-0 re-check after the first measured corpus; FR-PO-052 adds no
> allocation, only a few float ops per candidate. **Gate NOT run — no .NET SDK in this environment;
> nothing compiled or executed; CI's dotnet gate on this push is the first compile.** **Separately,
> a record correction:** the review file's §2 claim that ERR-008-019 (the long-shot cliff) was
> "FIXED … gate green" is **false against both branches** — no log entry exists, the cliff is live
> in `UtilityWeights.cs`/`UtilityScorer.cs` and the spec, and no branch carries a fix; the prior
> session recorded a landing that never happened (the fabricated-claims trap). Review §2/§5
> corrected, the finding re-opened (33 open again), the id soft-reserved. Prior entry below.)

> **Last Updated (prior):** August 4, 2026, latest same day (**Football-judgment proxy review — the remediation
> doctrine (§6) landed, doc-only.** The review file stops being identification-only: an owner session
> converged the general approach before any of the 33 open findings gets a fix, and §6 records it so
> each fix cites a principle instead of re-arguing the method up to 33 times. The frame is the owner's
> **recognition → decision → execution** pattern — which is already the #7 → #8 → Mechanics/Physics
> pipeline — with its five failure modes made into binding mitigations (stages degrade assessment
> quality, never delete options; attributes enter a judgment once; decisions commit intent, not a
> frozen coordinate — a spot where a teammate *will arrive* is a legitimate target, a lock on his
> current position is not; coordination is signalled, not mind-read; calibration targets the chain).
> Five principles: **P1** continuous-never-cliff (the ERR-008-019 shape, covering the pattern-(b)
> findings), **P2** skill as *discrimination fidelity* — `perceived = neutral + fidelity × (true −
> neutral)`, so a low-skill assessor sees everyone as average, which IS today's attribute-blind engine
> (graceful degradation, no RNG in assessment), **P3** the attribute ownership ledger (Vision owns
> on-ball recognition, Anticipation off-ball/predictive; **no new "play recognition" attribute** —
> owner call), **P4** intent as a first-class object (pass-to-space, run-intent signals on the event
> bus, set-piece routine targets — mechanism-class, design supplement first), **P5** calibrate
> end-to-end, pivot on today's baseline, defer real `[GT]` tuning per KD-W1. The **template fix is
> chosen but NOT implemented**: #8 §3.1.3.3 pass-lane interceptors become `distance_falloff ×
> perceived(Anticipation+Pace)` through the passer's Vision fidelity; §3.2.2's Vision term is
> untouched (it rewards vision generally, fidelity owns risk discrimination — no double-count); the
> §3.1.4 shot lane deliberately deferred. Also recorded: the **pairwise playing-familiarity gap** —
> #33's social graph and #2's per-player Stage-4 hooks exist, but nothing pairwise-on-pitch; the
> natural third input to the run-signal handshake; candidate supplement. The review file also finally
> enters `file-manifest.md` — it was never recorded at creation.)

> **Last Updated (prior):** August 4, 2026, latest same day (**W1 adversarial review pass 2 — 1 High, 1
> Medium, 3 Low.** The High is a seam defect, and it is the other half of pass 1's own fix rather than
> a new subject. #11 indexes every per-keeper array by `gkIndex`, which is the **team** (KD-1); this
> engine keys identity by **roster slot**. Those agree right up until the occupant of the keeper slot
> changes — a keeper is sent off, and the reserve keeper comes on in a *different* slot, which is the
> only shape the sequence can take because `SubstitutePlayer` refuses the dismissed slot itself. The
> path is live from `ManagerCommand`, not hypothetical. Nothing inside #11 can observe the handover,
> so the substitute inherited the slot whole: state, dive scratch, hold stamps, and a `RushIntent`
> whose target was **locked at commit (KD-15) for a player who has left the pitch** — which the
> `Set → Rushing` row then launched him at, making his first act on the field a sprint to a point
> nobody chose for him. Pass 1's sent-off filter is what made this reachable: it changed the dismissed
> keeper's slot from self-resolving (#11 kept ticking him to the end of his run) to frozen
> indefinitely, and frozen state is precisely what gets inherited — a ghost sprint traded for a stale
> one. Fixed by giving #11 a `ResetSlot` and having `RefreshGkAgentIds` detect a change of occupant,
> so the slot's state belongs to whoever holds it. **No new engine state and no
> `SNAPSHOT_SCHEMA_VERSION` bump**: `_gkAgentIds` *is* the previous value, and it is reconstructed
> rather than serialized, so a restore re-derives it and sees no change. The constructor's sentinel
> loop now runs through `ResetSlot` too — a fresh slot defined once instead of in a pair that must
> agree (§5.Z.12). The Medium is a gap in my own last pass: the sent-off fix shipped with **nothing
> asserting it**, so its return would have been silent; both locks are now in, mirrored home and away.
> Three Lows recorded not fixed in `gk-rush-trigger-design.md` §7 — a comment in #11 that W1 falsified,
> a redundant `_attrs` write on the rush path, and a state-machine comment that states the opposite of
> the row order it describes. **Gate still NOT run — no .NET SDK in this environment, so none of this
> has been compiled or executed.**)

> **Last Updated (prior):** August 4, 2026, same day (**W1 adversarial review pass 1 — 1 High, 4
> Medium, 4 Low, all fixed.** The High is the one worth naming: `RushArmed` bounded how LONG a run the
> keeper would commit to and never how SHORT, so a keeper standing on the ball he had just swept
> re-armed — and that is the *ordinary* end state of a sweep, not an edge case, because §5.Z.15/16
> bars the keeper from collecting the loose ball he ran to. Traced through the real call order the
> result is a zero-length rush every third tactical tick (`Set` → commit → `Anticipate` → `Rushing` →
> target reached → `Recovering` → `Set`, the cooldown bypassed because `UpdateBaselineSlot` feeds the
> keeper his own position), a keeper pinned to a dead ball, a `RushPhase.Reached` published every
> cycle, and — the part that bites — never enough `Anticipate` dwell for §3.3.6's dive gate, so the
> save path is suppressed while it runs. **`ERR-011-009` ended the stall; without this guard it became
> a churn.** The fix reuses #11's own arrival radius rather than minting a twelfth `[GT]`: the commit
> test and the arrival test must agree, and §5.Z.12's rule is that a pair has two places that must
> agree where a mirror has one. Mediums: a keeper **sent off mid-rush kept sprinting** (the engine's
> freeze is `_commands = Stop`, which governs the movement integration only, while #11's `Rushing`
> branch writes position *after* it — `RefreshGkAgentIds` now filters `_isSentOff`, which is what
> `NotifyKeeperOfShot`'s own comment already assumed); `RushCommitFatiguePenaltyM` is **structurally
> dead**, since all four `ToGoalkeeper` call sites hardcode `fatigue: 0f`, so it is recorded
> do-not-calibrate in both the spec and the design note rather than entering the calibration pass
> looking live; **no test proved the keeper physically leaves his line** through a real engine (the
> composed locks stopped at `GkState == Rushing`, which is equally true of an engine whose rush
> position write-back is dropped — the #11 v1.4 H-2 defect), now fixed by a displacement test plus the
> re-arm lock; and `GkHeadingIntentSource`'s v1.1 history row still documented the **rejected**
> last-man model as current. Lows: the epsilon renamed `GK_RUSH_DEGENERACY_EPSILON` because it guards
> three dimensionally different quantities, a `+4 [GT]` header corrected to 5, an orphaned header
> continuation folded back, and the cross-catalogue `GkRushCommitment > RushCommitThreshold` invariant
> — which keeps the whole trigger from going silently dead — now **asserted** instead of merely
> commented. **Still not measured: no .NET SDK in this environment, so no gate run and no numbers.**)

> **Last Updated (prior):** August 4, 2026, latest same day (**WIRING BACKLOG W1 LANDED — the goalkeeper
> comes off his line for the first time, and the spec defect that discovery surfaced
> (`ERR-011-009`).** `GoalkeeperMechanics.CommitRushIntent` had **zero production callers** since it
> was written, so every one-on-one this engine has ever played was a stationary keeper on his line —
> the whole rush subsystem below the trigger (dispatch, `Rushing → OneOnOne → Smothered`, abort
> reasons, telemetry, snapshot serialization) was built, tested and dead. `MatchEngine.TryCommitRushIntents`
> is that caller, over a new pure `GkHeadingIntentSource.RushArmed`. **The predicate is built from one
> sentence: a keeper comes out to REDUCE THE SHOOTING ANGLE.** So the only thing that keeps him home is
> a team-mate already **goal-side** of the ball, inside the corridor the shot would travel down — a
> defender merely *chasing* the carrier, or wrestling him for the ball, narrows nothing and does not
> stop him. And **how far** he comes out is not an engine constant but the keeper's own attributes,
> #11 §3.7.0's `ComputeRushCommitDistanceM` over `OneVsOne` / `Composure` / fatigue: ~9 m for a timid
> keeper, ~16 m for an aggressive one at 20% fatigue. (This is the corrected model — the first cut used
> a last-man test, refusing the rush whenever any team-mate was nearer the ball, which keeps the keeper
> home in exactly the situation he exists for. Caught at owner review, before any measurement.) For a
> loose ball the locked target is an **intercept-race solve** rather than the ball's current position,
> because KD-15 locks the target at commit and a rolling ball is not where it was; the solve
> self-guards, since a clearance outrunning the keeper has no positive root. Skipped whenever
> `SaveArmed` holds for the same keeper — **a ball driving at the goal is a save, not a rush** — or a
> shot would send the keeper charging out while the ERR-011-007 commit-lead gate still held the dive,
> regressing the whole §5.Z.17–§5.Z.22 save pipeline. Deliberately **not** routed through the Decision
> Tree: `ActionType.SAVE = 7` is the last ordinal that fits the 3-bit composure-noise field, so a RUSH
> action would force the same digest rebaseline that defers W9, turning the board's cheapest large
> lever into its most expensive item. **No new engine state** — #11's own already-serialized
> `_rushIntentActive` is the per-episode latch, read through new `GetState`/`HasActiveRushIntent`
> accessors rather than duplicated (two latches with different lifetimes for one episode is precisely
> ERR-011-002's dive-at-nothing) — so **no `SNAPSHOT_SCHEMA_VERSION` change**. **What the wiring
> surfaced, first: `ERR-011-010`.** §3.7's state entry delegated the rush DECISION to Decision Tree #8,
> which has no goalkeeper model and structurally cannot acquire one — so the condition belonged to
> nobody, which is the whole reason the method sat uncalled for ten weeks while everything below it was
> built, reviewed and tested. And because the "when" was delegated, the spec never said what a keeper is
> *deciding* either, a gap no call site can fill by guessing. New §3.7.0 takes the decision back (the
> §3.3.6 move) and states it normatively on both halves: only a goal-side body is cover, and the
> distance is his own attributes. `OneVsOne` is consumed for the commit DECISION only — FR-GK-024's
> closed-form constraint on the 1v1 SAVE formulas is untouched. **And second: `ERR-011-009`.**
> #11 §3.1.1 gives `Rushing` three exits and `OneOnOne` two, and for a
> LOOSE ball **none of them can fire** — the 1v1 and smother triggers are false by construction with
> no ball possessor, F-08 needs one, and §3.7.2's update converges on the locked target and stops
> without overshooting — so a keeper who swept a loose ball would have stood over it in `Rushing` for
> the remainder of the match. Everything else anticipated the completion (`RushPhase.Reached` has been
> in the enum since v0.1, never published; §3.7.3 reserves `AbortReason.AttackerBeatGK`, also
> unreachable); only the table that adjudicates state had no row. Fixed spec-and-code in the same
> commit: two §3.1.1 rows, the §3.7.2 terminating check, `[GT] RUSH_TARGET_REACHED_RADIUS_M`, and the
> `Reached` event finally emitted — a **completion, not an abort**, ranked below contact, F-08 and the
> 1v1 trigger, so FR-GK-018 / KD-15 are untouched. **The honest headline, and a deliberate break with
> every §5.Z entry above: NOTHING HERE HAS BEEN EXECUTED.** The authoring environment has no .NET SDK
> and the agent proxy denies `builds.dotnet.microsoft.com`, so `tools/dotnet-ci/run-gate.sh` did not
> run and the new `GkRushDiagnosticTests` instrument is written-and-unrun. There are **no pre/post
> numbers**, and none were invented; the gate result for this landing is whatever the GitHub
> `dotnet-compile-test` job reports, and no claim that a suite enforces anything may be cited before
> then — that is this project's own never-compiled-surfaces hazard, and it is being named rather than
> stepped in. Eleven new `[GT]`s — six in #11's catalogue (the §3.7.0 commit-distance model plus
> `RushTargetReachedRadiusM`) and five in the engine's (`GkRushMaxInterceptS`, `GkRushMaxBallHeightM`,
> `GkRushCommitment`, and the two cover-geometry dials) — are all first plausible numbers, not fitted
> ones; under KD-W1 they are **new dials for a dead surface**, not retunes, and they are the
> calibration pass's input. Note where they live: how far the keeper comes out is **#11's**, because it
> is a property of the keeper; the cover geometry and the guards are the **engine's**. Files: `MatchEngine.cs` v1.58,
> `GkHeadingIntentSource.cs` v1.1, `MatchEngineConstants.cs` v1.28, `GoalkeeperMechanics.cs` v1.11,
> `GoalkeeperStateMachine.cs` v1.7, `GoalkeeperConstants.cs` v1.5, `GoalkeeperRushDispatch.cs` v1.1,
> new `GoalkeeperRushTests.cs` /
> `GkRushTriggerTests.cs` / `GkRushDiagnosticTests.cs`, new owner doc `gk-rush-trigger-design.md`,
> spec #11 §3 v0.7, `spec-error-log.md` v1.56, `match-engine-wiring-backlog.md` v1.1. Next in the
> backlog sequence: **C1**, the `InPoss` gate — the largest starvation on the board. Prior entry
> below.)

> **Last Updated (prior):** August 4, 2026, latest same day (**MERGE — `main` into the P4a branch, and a version-number collision resolved.** PR #295 was un-mergeable. Three conflicts, **all in the "newest entry at top" chains** — this file, `CHANGELOG-src.md`, `file-manifest.md` — which is the expected class when two branches each prepend, and **no source conflict at all**: `main` had moved on in `decision-tree` and `match-engine` only, which the client assemblies do not touch. Resolved by **interleaving both sides chronologically by commit time** rather than picking a winner, so every entry from both branches survives verbatim, one `Last Updated:` per chain, everything below it `(prior)`. **The collision worth knowing about:** both branches independently allocated `CHANGELOG-src.md` **v2.53**. `main` owns it (close-chance §5.Z.24, already in trunk), so this branch's four entries renumbered up by one — P4a landing 2.53→**2.54**, AR pass 1 →**2.55**, tilted-view →**2.56**, AR pass 2/3 →**2.57** — in both the header chain and the VERSION HISTORY table. Nothing outside that file cited them (grep over `docs/`, `src/`, `README.md`). **A consequence to leave alone rather than "fix":** 2.54 is dated August 3 and sits above 2.53 dated August 4. The table is keyed on version, and version numbers record the order things land in trunk, which is not the order they were written — renumbering by date would mean renumbering an entry already merged. **One edit to content that arrived from `main`, made deliberately and recorded here rather than silently:** its chain tagged close-chance `v2.52` while its own VERSION HISTORY table gives that entry 2.53; left alone the merge would have put two `v2.52` tags in one chain — the duplicated-version hazard this project has a standing trap entry for — so the tag now agrees with the table it contradicted. **Deliberately NOT touched:** `main` carries a repeated §5.Z.23 entry in both changelog chains (both tagged v2.51), and the VERSION HISTORY table has six long-standing duplicate version ids (2.9, 2.10, 2.34, 2.36, 2.42, 2.44) — identical on both branches, so neither was introduced here. Deleting a historical entry during a conflict resolution is what these files forbid, and renumbering 150 rows of merged history is not a merge's job. **Verified mechanically, not by reading:** every line of both parents is accounted for in all three merged files — the only absences are the label relabels, the four renumbers, one deduplicated header, and five manifest rows where this branch's text is a superset of `main`'s (the `LiveAgentCue` row gained `IsGoalkeeper`; the client section headers moved P3→P4a and P4→P4b). Every P4a source file is byte-identical pre- and post-merge. No `src/` change in this merge.)
>
> **Last Updated (prior):** August 4, 2026, latest same day (**P4a ADVERSARIAL-REVIEW PASS 2 — 1 High, 4 Medium fixed; run over the tilted-view revision's own output.** **H-1, and it is the pointed one:** `PitchCameraRig` decided where the camera goes and how it is angled, but said nothing about **how much of the pitch it sees** — so P4b would have chosen a field of view inside the `MonoBehaviour`. A framing decision, in the one place the CI gate cannot compile, sitting inside the deliverable whose entire purpose is keeping decisions out of there (§12 rule 1, the P4a/P4b split). `PitchCameraPose` gains `FieldOfViewDegrees` — the binding now assigns position, look-at and field of view, and picks nothing — `MatchClientConstants` gains `CameraVerticalFovDegrees`, and because two individually-legal dials can pair into a camera whose lowest ray never meets the ground, the bound is `tilt + fov/2 < 90` rather than two range checks. `PitchCameraRig.GroundExtentAlongTilt` attaches a number to the framing: near and far reach of visible ground, **deliberately asymmetric**, since a tilted camera sees a trapezoid and asserting symmetry is the mistake the test guards. **M-1:** §5-P4b instructed *both* cameras in a single bullet — the new rig placement and, in the same sentence, the deleted orthographic one — while the very next bullet said the orthographic assumption was wrong; the roadmap's B8 row carried only the stale half. The live instruction sheet for the next phase on the critical path contradicted itself. **M-2:** `PitchMarking`'s doc still sent the render skin to `ToView`, which would stand every marking upright in the world XY plane instead of laying it on the turf — and `ToView`/`ToPitch` turned out to have no production caller left at all after the revision (`ToView` was `ToWorld` with the height dropped, and the inverse a click needs is a ray intersection), so both are deleted and their tests re-anchored. **M-3:** `CameraLateralOffsetM` was the only camera dial with no validation, and it lands directly in the camera's world position — a non-finite value put the camera nowhere while every assertion about the aim point still passed. **M-4:** the tilted-view revision never appended version-history rows to `MatchClientConstants.cs` (v1.4) or `MatchRenderProjection.cs` (v1.2), so each file's newest row described constants and a `HeightScale` it no longer had, and three tracking documents cited versions the files themselves did not claim. The `// Modified:` date check did not catch it, because the previous row carried the same date. `match-client-core` 129 → 135; the two new locks verified non-vacuous by breaking them (symmetric ground extent fails 2, a fov dropped from the pose fails 1). **Full dotnet gate: PASSED, 0 failures** (whole tree green, 30 suites; match-client-core 129 → 135, match-engine 368 unchanged). **The sweep after the fixes found one more Medium, so this pass is NOT converged** — `PitchMarkingKind.Rectangle` still documented corner ordering as *not* guaranteed and told consumers to re-normalise, which is the exact contract pass 1's H-1 reversed: `PitchMarking.cs` was fixed then and the enum sitting beside it was not, so two files stated opposite contracts for one field, and the enum is the one a renderer switching on `Kind` reads first. Fixed; the guarantee is test-locked by `EveryRectangleArrivesWithItsCornersNormalised`, so the docs cannot silently drift from the code again. **Pass 3 then re-read the whole P4a surface and surfaced no High and no Medium — the loop is converged.** Two Lows fixed: `PitchCameraPose`'s header and class summary still described it as two values, and a test comment credited the wrong assertion with guarding the static-init-order defect. That second one is worth stating plainly, because the correction is counter-intuitive: asserting `CameraTiltDegrees > 0` does **not** catch a declaration reorder. By the time any test reads the field, static init has finished and it holds its real value whichever order it ran in. What catches it is re-evaluating the invariant itself on the finished values — a pair that is genuinely invalid fails there regardless of what the boot check saw. The guard was already present and correct; only the comment beside it was wrong. **Full dotnet gate on the converged tree: PASSED, 0 failures** (30 suites; match-client-core 135, match-engine 368 unchanged).)
>
> **Last Updated (prior):** August 4, 2026, latest same day (**`match-realism-pass` SKILL RE-CUT FOR WIRE-FIRST
> — the calibration ladder moves behind a wiring gate, and the gate now defers to the wiring backlog
> and KD-W1.** Tooling-only; no `.cs`, no spec, no assembly, no gate run. The skill encoded
> measure → localize → ladder → land, which is the right shape only when the chain under the dial is
> complete. Twice in the §5.Z chain a brief arrived asking for a *quality* that turned out to be
> **undefined** because a stage was missing — **§5.Z.17** ("the quality of the save, not its
> existence"; measured zero hand contacts across six keeper-matches, one cause being
> `OnShotExecutedEvent` with zero callers anywhere) and **§5.Z.23 / ERR-011-008** (#11's catch coded to
> one of its two spec statements, so a claimed ball flew on into the net).
>
> **New `## 0. The wiring gate`, ahead of the premise check (now §0.1).** It opens by requiring the
> chain to be **enumerated from the observable backwards to the dial** out of the owning spec's §3 —
> building that list is the hard part, since nobody had "the catch parks the ball" on a stage list
> until §5.Z.23's instrument followed the ball after the contact — and falls back to §1's funnel when
> the list cannot be written from source. Then six source-read checks, **all six run, every failure
> reported**: multi-gap chains are not rare (§5.Z.15 found #11 switched off AND keepers skipped by the
> physics phase; §5.Z.17 found three independently sufficient defects). Checks 1–5 are assembly
> existence, composition-root construction + phase reach + **the flag state inside your own
> instrument** (`DisableGkHeading()` is called in five places and §1 tells you to copy an exemplar),
> live **read**-side consumer, spec §3 **body** vs Outputs summary, and Stage-0 placeholder. Checks 1
> and 5 split on whether the brief names a spec or a symptom.
>
> **Merged with `main` across the wiring audit, which changed this skill rather than merely colliding
> with it.** Three integrations: (a) **check 0 is now `match-engine-wiring-backlog.md`** — the audit
> enumerated **10 Class-A dormant capabilities** by three systematic sweeps, so the gate reads that
> board before re-deriving anything, and W1/W2 (the keeper never leaves his line, no player has ever
> made a tackle) are cited as the standing examples; (b) **new check 6, gate-level dormancy**, which
> the audit names as the explicit blind spot of exactly the static checks §0 had listed — a call site
> that runs but whose condition is almost never true is invisible to all of checks 1–5, and C1 (#12
> commits `InPoss` on **9.5%** of final-third samples) was found only by runtime instrumentation;
> (c) **§3 now opens with KD-W1's `[GT]` freeze**, since the project-wide rule — no `[GT]` change
> governing an unwired subsystem until the post-backlog calibration pass — is strictly stronger than
> the per-chain conditionality this pass had written, and a skill that told the reader to calibrate
> once *its* gate passed would have contradicted standing policy. §5.Z.24 also refutes a claim in the
> skill's own opening — it is "the first premise in this chain that survived its own check" — so
> "every one produced a partly-wrong brief" is corrected to seven of eight, and its **ERR-008-018**
> joins ERR-008-017 as §2's second cause-1 instance.
>
> **The gate is a filter, not a verdict on calibration.** §5.Z.20 is cited in both §0 and §3 as the
> standing counterexample: a `[GT]` recalibration inside #11's own §3.4.3/§3.4.5 ranges produced **the
> largest single movement this chain has measured, goals per match 14.7 → 8.0**. It fixed two timing
> defects in the same pass — so the gate would have had work to do there too — and its owner document
> states those fixes alone were not sufficient, the old values "could not reach the catch band … even
> with a perfect window", which is precisely the point: the dial was load-bearing independently of the
> wiring. The stated rationale for wiring first is therefore **not** that it moves the number more, but
> that a missing stage bounds the outcome at a level no dial can reach.
>
> **Two further edits.** §2's cause 3 (structurally unreachable / vacuous gate) is labelled **§0
> failing late**. §7 requires the recorded residual to be **classified — missing stage or mis-set
> dial** — because the next pass runs §0 against that sentence.
>
> **Adversarially reviewed before landing; the review is why this entry reads as it does.** Pass 1
> raised 4 High: a superlative ("the largest movements came from a missing stage") that the chain's own
> record **refutes** via §5.Z.20; "§3 is the step most passes should skip", contradicted by load-bearing
> `[GT]` work in at least §5.Z.18/.19/.20/.21; "stop at the first check that fails", contradicted by the
> two-gaps example the gate itself cites; and a **misattribution of the motivating evidence** — §5.Z.15
> and §5.Z.16 were cited as calibration briefs that turned out to be wiring, when §5.Z.11 item 2 had
> named that wiring in advance ("opt-in and default-off (`EnableGkHeading`) … plus GK locomotion") and
> §5.Z.16 was never a brief at all. Passes 2–3 caught 3 more Medium, two of them introduced by the
> pass-1 fixes; pass 4 was clean.
>
> **Chain repair, recorded rather than absorbed.** This merge's conflict region contained a
> pre-existing defect on `main`: an **orphaned `**Last Updated:**` header** for §5.Z.23 with no body
> and an unclosed parenthesis (the real §5.Z.23 entry survives intact below as `(prior)`), plus the B6
> entry left bare when the audit entries were inserted above it — three bare labels where the chain
> permits exactly one. The orphan is deleted and B6 relabelled `(prior)`; no entry body was edited.
> This is the fourth time this chain has needed the same correction.
>
> Modified: `.claude/skills/match-realism-pass/SKILL.md` (frontmatter description + §0/§0.1/§2/§3/§7),
> `.claude/skills/README.md` (derivation row), `file-manifest.md`, and this file. Prior entry below.)

> **Last Updated (prior):** August 4, 2026, later same day (**MATCH-ENGINE WIRING AUDIT — the code that
> exists and never runs, and the `[GT]` freeze that follows from it.** Seven consecutive §5.Z passes
> fitted constants against the composed engine. This audit asks what was *in* that engine, and the
> answer is: less than the assembly graph suggests. Three passes over the 18 assemblies the match
> engine references — a comment sweep for self-declared deferrals, a whole-tree production-caller
> count over every `public` method, and manual triage of every candidate in source — found **10
> Class-A dormant capabilities**. The two largest were invisible to the project's own tracking. **The
> keeper never comes off his line**: `GoalkeeperMechanics.CommitRushIntent` has no production caller,
> though everything downstream of it works (`GoalkeeperRushDispatch.UpdateRushFrame` moves the keeper
> and writes back to the movement array; `Rushing → OneOnOne → Smothered` exists with abort reasons
> and telemetry; the `RushIntent` is even serialized) — only the trigger is missing, so every 1v1 in
> the game is a stationary keeper waiting to dive. **No player has ever made a tackle**: three
> independent dormant links in one chain — `DefensiveAITick.GetTackleIntentRequests` is populated
> every tick and read by nobody (its own class doc says integration is Stage 1, KD-16),
> `GetAndClearTackleFlag` is hardcoded `=> false` in **both** engine collision adapters
> (`MatchEngine.cs:6721`, `:6789`), and consequently `PassExecutor`'s §3.8.5 tackle-interrupt branch
> and its `CancelReason.TackleInterrupt` outcome are unreachable code. No comment anywhere records
> this one; only the call-graph pass found it. Also dormant: cross claims (`ResolveHandContactDuel`
> intentionally not called, blocked on the same multi-agent contact feed as the already-filed
> AGENT_BALL fan-out), the keeper's vision (`SaveArmed` is four lines of pure geometry while a
> tested `OcclusionFilter` runs for every outfielder), the #13 BackwardPass press trigger
> (`PassEventRing.Push` has no producer, so the ring the engine builds per team is permanently
> empty), `BallStateType.Controlled` (no producer — possession is a flag, never a kinematic
> constraint), and #26's kickoff preset selection (`ManagerAdaptation.ApplyKickoff` uncalled, so an
> AI manager starts every match on the human baseline; only the mid-match ladder is wired).
> **The method's blind spot is stated rather than hidden:** it detects *method-level* dormancy and is
> structurally unable to see *gate-level* dormancy — a call site that runs but whose condition is
> almost never true. One such is already measured (#12 commits `InPoss` on **9.5%** of final-third
> samples, starving every phase-gated mechanism in #13/#14/#15), found by runtime instrumentation in
> §5.Z.24 and by no static analysis, so the backlog carries four Class-B entries from that pass and
> books a gate-firing instrument as its own item. **This backlog is a floor, not a ceiling.**
> **KD-W1, the `[GT]` freeze:** do not land a constant governing an unwired subsystem. The hazard is
> diagnostic, not just arithmetic — measured conversion of ~18% against football's ~11% reads as "the
> shot model is too generous" when part of it is "no keeper has ever narrowed an angle and no
> defender has ever tackled", and a pass aimed at the shot model would have left behind a `[GT]` that
> later has to be un-tuned. Defect fixes, instruments and measurement continue freely; constants wait
> for one calibration pass against the complete engine. **KD-W2** scopes this to the match engine —
> the 22 approved specs with no assembly remain `path-to-playable-roadmap.md`'s problem. The §5.Z.23
> `pointQuality` owner decision is **parked, not resolved**: the rush trigger changes the contact
> geometry that decision turns on. New `docs/tracking/match-engine-wiring-backlog.md` v1.0. Read-only
> audit — no `src/` change, no spec change, no gate run. **Prior entry below.**)

> **Last Updated (prior):** August 4, 2026 (**Tilted-view revision — KD-P4a-2 (owner call).** P4a first shipped a flat top-down view with ball height faked by a sprite lift and a capped size ramp. The owner reversed it to an FM-style view — from above, **tilted back from vertical, slightly off centre** — since the ball only needs to be visible on and above the pitch, not scaled. The revision **deletes more than it adds**: with a tilted camera height is a real world axis, so `BallHeightViewOffsetPerMetre`, `BallHeightScalePerMetre` and `BallMaxHeightScale` are gone, along with `BallRenderModel.SpritePosition`/`SpriteRadius` and `MatchRenderProjection.HeightScale` — and with them the AR pass's M-5 finding and its 10 m saturation limitation, which stop existing rather than needing a retune. New: `PitchCameraRig`/`PitchCameraPose` (height, tilt-from-vertical, lateral offset — a placement is a decision, so it is gate-compiled, and the pose is two world points because `Quaternion` is not in the shim) and `PitchViewProjection.ToWorld`/`ToWorldGround`/`TryGroundHit`. **The one real cost is the click inverse:** screen position is no longer affine in pitch position, so `TryGroundHit` does a ray/ground-plane intersection; `Camera` is not in the shim, so the Unity side supplies the ray and the math stays gate-tested. Survivors, each for a reason — the **shadow** (under any tilt a lofted ball separates from the pitch point it is over, the one cue perspective cannot supply), the corner→centre re-origin (it is the ground plane), and `FollowBallCamera` (it decides *where* the camera looks). Two things recorded rather than left implicit: the engine's Y becomes the world's **Z** and its Z the world's **Y** — an axis swap, the same trap class as the corner origin, locked by its own test (seven tests fail if it is inverted) — and `FollowBallCamera`'s pitch clamp is now **approximate**, since it describes a rectangle of visible ground where a tilted view sees a trapezoid; kept deliberately, as its job is keeping the target near the pitch rather than exact framing. **Full dotnet gate: PASSED, 0 failures** (whole tree green; match-client-core 112 → 129, match-engine 368 unchanged — no sim source was touched). The entry was first written while the run was still in flight and recorded as *not yet reported*; this line replaces that provisional wording with the run's actual result.)
>
> **Last Updated (prior):** August 4, 2026 (**§5.Z.24 — CLOSE-CHANCE CREATION: the first premise in this
> chain that survived its own check, one formula defect fixed, and the creation gap deliberately NOT
> claimed.** §5.Z.23 §7 item 4 re-localized the creation residual to the final-third → penalty-area
> stage (6.5% against football's ~40%) and named it a #8/#15 surface. Two premises were checked.
> **The first SURVIVED — a first for this seven-pass chain**: the "306.7 final-third entries" figure
> is a raw boundary-crossing count that a ball rattling across x = 35 would have inflated, but
> re-counted with a 1 s exit dwell over six full matches it reads **311 episodes against 312 raw
> crossings**, each averaging 5.1 s. The denominator was sound. The second premise located the stage
> without naming a mechanism, and the instrument (`CloseChanceDiagnosticTests`, env-gated
> `TD_CREATION_DIAGNOSTIC=1`) found two, both real: **nobody is in the box** — mean attacking
> outfielders inside the penalty area while the ball is in the final third is **0.11**, with 92% of
> samples at zero, and the deepest *composed target slot* is **22.8 m** from goal against a deepest
> *attacker* at 22.2 m, so the players sit within 0.6 m of where they are told to be and are simply
> never asked into the area — and **the carrier walks the ball back out**: DRIBBLE is the modal
> attacking-third action at **40%** of heartbeat decisions with a mean cosine to the opponent goal of
> **−0.302** and only 31% pointing goalward. **ERR-008-018** is the second half: #8 §3.1.5.2 picks the
> dribble direction by free space alone and closes by delegating the correction to *"the scoring stage
> (§3.2.2)"* — but §3.2.4.1, DRIBBLE's actual formula, has no directional factor and **§3.2.2 is the
> PASS formula**, so the promised term was delegated to a section that does not own DRIBBLE and never
> had a home. Same class as ERR-008-017. Fixed with `DirectionQuality_DRIBBLE`; measured cosine
> **−0.302 → +0.006** and goalward share **31% → 49%**, moving on **all six seeds with no overlap**
> between the pre- and post-fix distributions. The `[GT]` floor lands at **0.80** rather than the 0.50
> that maximises the effect, because suppressing the dribble pushes the carrier onto HOLD — which has
> no timeout — and at floors 0.65 and 0.50 one seed in six stalled outright (mean final-third episode
> 5.1 s → 17.5 s / 28.6 s). **The creation funnel itself did not move and is not claimed**: box
> occupancy 0.11 → 0.10, ball into the box 6% → 5% of episodes, passes into the box 1% → 0%, shots
> 19.3 → 19.5, goals 3.67 → 3.50 — **the residual shot-count gap is NOT closed**. #15 §4.5.2's
> run-target overlay was implemented, measured and **REFUSED**: it moves the committed RUNNER's target
> from 80.9 m to 14.7 m from the attacked goal and moves box occupancy **down**, 0.11 → 0.08, because
> a RUNNER's target is `carrier + 12 m` and the carrier is usually still in midfield. A pooled number
> nearly carried a false creation claim — at floor 0.50 the corpus reads box occupancy 0.11 → 0.59,
> but five of six seeds are flat and the whole movement is **one stalled match** contributing 32% of
> samples; the acceptance scenario's box predicate failed post-fix, forced the per-seed breakdown, and
> the claim was withdrawn and the predicate deleted rather than re-tuned. The residual is re-localized
> and sharper than what it replaces: **#8 cannot pass to a place, only to a player** — §3.1.3 generates
> one PASS candidate per visible teammate *at that teammate's current position*, so passes into the box
> measured 1% at every rung of the ladder, including rungs where players did reach the box. Owner doc:
> `docs/tracking/close-chance-creation-design.md`; match-engine §5.Z.24; `spec-error-log.md` v1.56.
> Acceptance `match-engine-close-chance` — **2 of 3 predicates fail at `7fcd897` by execution**. No
> schema / RNG / domain-tag / draw-site / draw-order change. Prior entry below.)

> **Last Updated (prior):** August 4, 2026 (**P4a ADVERSARIAL-REVIEW PASS — 1 High, 5 Medium, 3 Low fixed;
> the pass then re-run clean.** **H-1, and the one that would have shipped:** `PitchMarking.Rectangle`
> took its two corners in whatever order it was given, and `PitchMarkings` builds the end boxes from
> their goal line *inwards* — so the away penalty area and away goal area arrived with **descending
> X** while the home pair ascended. A P4b binding doing the obvious `B − A` would have drawn those two
> inverted or invisible: the #8 ERR-008-002 home/away asymmetry class, landing in a `MonoBehaviour`
> where the gate can never see it, in the very type whose purpose is to leave the skin nothing to
> decide. Worse, the fixture *laundered* it — `AssertAreaBox` normalised with `Mathf.Min`/`Max` before
> asserting, so any corner order passed. `Rectangle` now normalises (A = min, B = max), the helper
> reads `A`/`B` directly, the mirror test states the rectangle pairing explicitly, and two new tests
> pin it; verified non-vacuous by un-normalising the factory, which fails four tests.
> **M-2:** the render path had **no non-finite gate** while its sibling `MatchFrameView` refuses one
> fail-loud — and `ProjectBall`'s doc excused the omission with "the producers upstream refuse to
> publish a non-finite coordinate at all", which is false: `LiveMatchStreamer` does not check, and
> `FrameInterpolator` *deliberately propagates* a non-finite position (it reads as a discontinuity and
> snaps to it). A NaN would have reached `transform.position`. Agent and ball **ground** positions are
> now refused; ball **height** keeps its graceful degradation, because a bad height still leaves a
> true ground position to draw at. **M-3:** `HasBall` was derived from `PossessionRingRadius > 0`, so
> a `[GT]` config setting the ring radius to zero would have answered "nobody has the ball" for a
> whole match — a fact about the simulation riding a presentation size. Inverted: `HasBall` is stored,
> the radius derives. **M-4:** a `BallMaxHeightScale` below 1 was silently repaired into "no cap",
> contradicting the `[GT]` loader's fail-loud contract in an untestable branch; it is now refused at
> boot, along with the previously documented-only "the ring must exceed the marker" invariant, and the
> repair branch is deleted. **M-5:** two `[GT]` rationales carried **fabricated figures** — an uncapped
> 20 m ball is 2.8 m across, not "wider than the penalty area"/"the six-yard box", and a 0.25 m marker
> is ~9 px at the default camera, not "a pixel". Replaced with checked numbers, plus the cap's real
> 10 m saturation point, now pinned by a test. **M-6:** the shirt-numbering rule was **duplicated, not
> moved** — the browser viewer's inline `computeJersey` was still there while the class doc, the
> version history and the commit message all said it had moved into `MatchRoster`. New
> `match-viewer/RosterShirtNumbers.cs` is now the one implementation; `LiveMatchStreamer` caches its
> output, `LiveMatchServer` serves a `"shirt"` key, `computeJersey` is deleted, and the rule's tests
> move down with the rule. **Lows:** a tautological marker-radius test replaced with one that can
> fail, `MatchRoster.FromStreamer`'s happy path covered (it had only its null guard, so the only
> production path into the type never ran), and the ring/marker invariant now enforced rather than
> merely asserted against the fallbacks.
> Two further defects surfaced while re-reviewing the fixes and were closed in the same pass: the M-2 gate initially ran *inside* the write loop, which would have left the destination half this frame and half the last behind a thrown exception (it now validates in a pass of its own, so the method stays all-or-nothing), and M-4's new validators were themselves unreachable from any test — replacing an untestable repair branch with an untestable guard would have moved the problem, so `MatchClientConstantsTests.cs` v1.0 drives them directly.
> **Full dotnet gate: PASSED, 0 failures** (whole tree green; all 30 suites reported, quarantine empty) — `match-client-core` 103 → 112, `match-viewer` 41 → 48, `ui-framework` 50 (unchanged), `match-engine` 368 passed / 8 skipped (unchanged; no `match-engine` source is touched by this pass), every other suite unchanged. No new compiler warnings — the five the tree reports are pre-existing CS0649s in `decision-tree`. No `SNAPSHOT_SCHEMA_VERSION` change, no new RNG stream /
> domain tag / draw site, no draw-order change, no engine-behaviour change.)
>
> **Last Updated (prior):** August 3, 2026, latest same day (**INTERACTIVE UNITY CLIENT P4a LANDED — the
> host-free render model.** P4 is split into **P4a, every render *decision*, and P4b, the binding.**
> That split is the August-3 owner-decision rule ("keep logic out of `MonoBehaviour`s") turned from a
> discipline into a phase boundary, and the ordering argument is the one that already put P6's
> head-less scenario ahead of P4: land the decisions where `tools/dotnet-ci` can compile and test them,
> and what is left for the pinned host is binding — which a cert run genuinely verifies — rather than
> behaviour, which it verifies only along the paths someone thought to click.
>
> **What landed** (all in gate-compiled `src/match-client-core/`): `PitchViewProjection`, the single
> documented coordinate adapter §7 requires — engine **corner-origin** metres ⇄ a **centre-origin**
> view plane at 1 unit per metre, plus the inverse a pointer click needs. Centring is not cosmetic:
> it makes a home-end position and its away-end mirror differ only in sign, which is what turns the
> mirrored assertions this repo's #8 ERR-008-002 history demands into one line each.
> `PitchMarkings`/`PitchMarking`/`PitchMarkingKind`, the IFAB catalogue as shapes, read from the
> **existing** `MatchViewerConstants` `[FIXED]` values rather than restated (§7's one-source-of-truth
> rule across both Views), with both ends emitted from one loop over a sign so a marking cannot be
> right at one end and wrong at the other. `MatchRoster`, the match-constant per-slot data — and the
> shirt-numbering rule finally out of the browser viewer's inline JavaScript and into gate-tested C#.
> `MatchRenderProjection` → `AgentRenderModel`/`BallRenderModel`: positions from the P3 interpolator's
> buffer because that is what is actually being drawn, every discrete cue from the newest captured
> frame because cues do not interpolate, the possession ring, and the ball's shadow / height-lift /
> capped-scale cues. Colour-free by design — a palette has no correct answer a test could assert.
>
> **Deliberately not built:** the D-arc and the corner arcs. Neither has a `[FIXED]` constant and the
> browser viewer draws neither, so adding them would mean inventing geometry here and diverging the two
> Views. Recorded rather than silently dropped.
>
> **The finding, KD-P4a-1 — a stale cache older than this pass.** `LiveMatchStreamer` cached team ids
> *and* goalkeeper flags at construction under "roster metadata never changes across a match". True of
> team ids; **false of goalkeeper flags**, which `MatchEngine.SubstitutePlayer` rewrites — so a keeper
> substituted for an outfield player moves which slot is the goalkeeper and the cache has silently
> disagreed with the engine ever since, drawing the keeper ring on the wrong player in the browser
> viewer since P1. A Unity roster type built on the same accessor would have inherited it wholesale,
> which is the argument for doing the render model before the skin rather than after. `LiveAgentCue`
> gains `IsGoalkeeper` — the first cue added through the extension mechanism KD-P1-6 created the struct
> for — sampled every tick; `MatchRoster` holds no goalkeeper flag at all so the stale copy cannot come
> back; `LiveMatchServer` reads the frame cue, fixing the harness with no JSON key and no viewer-script
> change. Re-reading the engine from the accessor was rejected: that is the off-sim-thread tear-read the
> streamer's single-writer invariant exists to prevent, and the reason it was cached to begin with.
>
> **No `SNAPSHOT_SCHEMA_VERSION` change, no new RNG stream / domain tag / draw site, no draw-order
> change, no engine-behaviour change** — the new cue is sampled from an existing read-only accessor.
> **Full dotnet gate: PASSED, 0 failures** (whole tree green; all 30 suites reported) — `match-client-core` 65 → 103, `match-viewer` 39 → 41, `ui-framework` 50 (unchanged), `match-engine` 368 passed / 8 skipped (unchanged; no `match-engine` source is touched by this landing), every other suite unchanged. **Next: P4b on the pinned host** (roadmap row B8), which now binds a render model that is
> already decided and already tested.)

> **Last Updated (prior):** August 3, 2026, later same day (**§5.Z.23 — CONVERSION AT CONTACT: the recorded

> **Last Updated (prior):** August 3, 2026, latest same day (**OWNER DECISION — ROADMAP B6 REVERSED: the
> product ships the FULL UNITY UI, not the web-hosted viewer.** Doc-only; no `.cs` changed. Recorded in
> `path-to-playable-roadmap.md` v0.11 (§7 supersede note, C2 amended, risk register re-cut),
> `interactive-unity-client-design.md` v0.11 (§12 status-change block), `browser-match-client-design.md`
> v1.3 (standing status block), and this file's assembly map + OPEN ISSUES.
>
> **The July 25 B6 entry is preserved verbatim and is not wrong** — it decided *time to a playable
> loop*, and it delivered that: PM-1 was reached July 27 on the browser surface. This decision is about
> *what the game ships as*, which the B6 table never weighed. That distinction matters for reading the
> record: the reversal is not a correction of a bad call.
>
> **Nothing is discarded, and nothing blocks P4 starting.** `src/match-client-unity/` is an asmdef and a
> README — P4 was never begun, so there is no unwind. The entire substrate a UGUI skin binds is already
> gate-compiled and needs no change: #38's view models and dispatchers, `MatchFrameView`,
> `MatchViewModelSource`, `MatchTacticsDispatcher`, `NavigationShell`, `MatchSession`, the command
> channel, `FrameInterpolator`, `FollowBallCamera`, and the P6 determinism locks. This is the
> "renderer is a leaf" property #38's contract was written for, finally used in the direction it was
> designed for. No art prerequisite either — §5-P4 is 2D-first, the pitch renders from the IFAB
> `[FIXED]` geometry already in `MatchViewerConstants`, agents are primitives, sprites are polish.
>
> **`src/match-client-web/` (34 tests) is retained and reclassified: shipping surface → host-free
> reference harness.** It is the only surface in the repo that exercises the whole read / playback /
> intent loop in CI on every push, which `match-client-unity` structurally never can. That makes it the
> regression net under the substrate the skin binds. Rule: **keep it green, do not extend it.** If it
> ever becomes expensive to keep green, delete it deliberately — do not quarantine it into
> `known-failures.txt`, which would leave a harness reporting green while proving nothing.
>
> **The one real cost is coverage, and the rule that bounds it is the entry worth carrying.** The CI
> gate cannot compile a line of `match-client-unity` and never will — the Unity shim covers `Vector2`,
> `Vector3`, `Mathf`, `Debug` and `Profiling`, value types and statics that can be reimplemented
> honestly, and there is no honest head-less `MonoBehaviour`, `GameObject` or `Camera`. **Extending the
> shim to fake them is explicitly REFUSED:** a lifecycle-free stand-in would let a render loop that never
> runs report green, which is ERR-030-014's failure mode transplanted one layer up, and this project has
> already paid for that lesson once at the cost of months of 0–0 matches. The mitigation is
> architectural instead: **keep logic out of `MonoBehaviour`s** — every decision (what to draw, where the
> camera goes, what a click means, which intent an input maps to) lives in gate-compiled
> `match-client-core` / `ui-framework`, and the Unity types assign transforms and forward input with no
> branch a test would want to reach. P3 already demonstrates the pattern. Then the uncovered surface is
> *binding*, which a cert run genuinely verifies, rather than *behaviour*, which a cert run verifies only
> along the paths someone thought to click. Second rule: budget a cert-host run **per P4/P5 landing**,
> not one at the end — the host block cleared July 19, 2026, so that is scheduling, not access, and a
> skin first exercised at the end is the never-compiled-surface trap this repo has hit seven times.
>
> **`PM-1` is now a split claim, and the roadmap says so rather than leaving the flag to be misread.**
> Its determinism exit criterion is met head-lessly and stays met. Its other three criteria are
> statements about a *screen*, and were demonstrated on a surface that is no longer the product — so
> they are open again against the Unity client. PM-1-the-capability holds; "the Unity client plays a
> match" is not yet true.
>
> **Also fixed, pre-existing:** `path-to-playable-roadmap.md`'s Version History had its header and
> delimiter rows separated by a data row, so it did not render as a table at all, and its rows were out
> of version order. Both corrected. The duplicated `v0.9` version number — two separate July 27 landings
> — is left as found, since historical entries are not rewritten.)
>
> **Last Updated (prior):** August 3, 2026, latest same day (**INTERACTIVE UNITY CLIENT P6 — the head-less
> closed-loop scenario LANDED, ahead of P4, and the ordering is the point.** `interactive-unity-client-design.md`
> §12 recommended P6 before the render skin for one reason: `match-client-unity` is in
> `generate_projects.py`'s `SHIM_EXCLUDED_ASMDEFS`, so **every P4/P5 line is invisible to
> `tools/dotnet-ci`**, while §5-P6's scenario is head-less and checked on every push. Landing it first
> means the render skin arrives against an existing determinism lock rather than ahead of one.
>
> **What §5-P6 asks for, and what it needed first.** The scenario is specified as "boot via
> `MatchSession`, inject a scripted tick-stamped command sequence through the queue, assert (a) two runs
> with the same `MatchSetup` + same sequence are digest-identical and (b) save@N → restore →
> tick-to-N+K replaying the same post-N commands == uninterrupted run." Three of those verbs had no
> composition-level surface: **`MatchSession` could not be advanced head-lessly** (`LiveMatchStreamer.TickOnce()`
> is `internal` to `match-viewer` and the only public advance is the background pacing thread), **could
> not be saved** (the P0 pass deferred "the durable save-capture body that rides the `ServiceOnce`
> seam"), and **could not be restored** (the constructor always boot-configures a fresh engine). P6 is
> therefore three small production additions plus the scenario, not the scenario alone.
>
> **`MatchSession` v1.2.** `TickOnce()` — the head-less deterministic advance — drives the **real**
> streamer seam (`match-viewer/AssemblyInfo.cs` v1.1 grants `InternalsVisibleTo` `MatchClientCore`;
> the seam stays internal to `match-viewer`, so nothing widens for the browser viewer). Routing through
> the real seam rather than a parallel client-side tick path is what makes the scenario a proof about
> the shipping composition: the pre-tick hook fires, the frame is captured, and the full-time auto-pause
> applies exactly as under paced playback. It refuses fail-loud once `Start()` has been called — two
> threads ticking one engine is a data race, and the streamer's own "never concurrently with the pacing
> loop" contract was a comment until now. `CaptureSave()` rides the `ServiceOnce` seam, so it works
> while running, paused and at full time; **§6.3's drained-empty-before-capture invariant is now held by
> ORDERING** — one sim-thread pass under the tick gate drains and applies the queue and only then
> encodes — rather than being asserted after the fact. An `Encode` fault is latched and rethrown to the
> `CaptureSave` caller instead of escaping the pre-tick hook and killing the pacing thread (the
> isolation posture `MatchClientDriver` already takes for a refused command); the handshake is
> `Interlocked`/`Volatile` rather than a lock held across `ServiceOnce`, which would have set up the
> opposite lock order against the tick gate. `RestoreFrom(blob, squads)` splits the constructor into a
> static `BootEngine` plus an engine-agnostic wiring ctor, so a restored session re-applies **no** boot
> mutator — `ConfigureSquads` throws on a ticked engine and re-staging tactics would overwrite restored
> state.
>
> **`TickStampedCommandReplay` v1.0** is the mechanism §6.1's reproducibility invariant is defined
> against. It enqueues each log entry immediately before the tick whose pre-tick `CurrentTick` equals
> its `AppliedTick` — exactly where the original drain read the clock — so a replayed run re-stamps
> identically and **the log is a fixed point of its own replay** (asserted). An out-of-order log and an
> entry whose application point has already passed are both refused fail-loud, because silently skipping
> either yields a run that is not the log's run while still reporting success.
>
> **The load-bearing predicate is the control run.** Both scenarios would pass on a command channel
> that did nothing at all: a run reproducing itself is not evidence that the commands are in the loop.
> So `match-client-command-log-replay` runs a **third** session with the same `MatchSetup` and **no
> commands**, and requires it to DIVERGE, in a bounded window around the first command (min = the tick
> after it is drained, max = two AI strides later) rather than merely "eventually". This is the direct
> lesson of the 600-tick capstone that asserted tick count, cadence, finiteness and digest advance while
> every match was a 90-minute 0–0 deadlock (ERR-030-014). The script is ten commands across **all three**
> live mutators and **both teams**, straddling the save tick — a home-only script would have repeated
> the #8 ERR-008-002 asymmetry mistake one layer up. `match-client-save-restore-replay` saves at tick 90
> (deliberately command-free, and the scenario checks that emptiness rather than assuming it, because a
> command at the save tick is in or out of the snapshot depending on drain order while carrying the same
> stamp either way), restores, and replays the post-90 log to tick 180 against the uninterrupted run.
>
> **One predicate was deliberately not written.** A "queue is drained at capture" check inside the
> scenario would be true there no matter which order the capture pass ran in — a vacuous pass dressed as
> a guarantee. The §6.3 invariant is locked instead by a unit test that enqueues a command immediately
> before `CaptureSave` and requires it back applied and logged.
>
> **Blast radius: nothing moved.** No engine behaviour changed — the client observes and drives
> pre-existing public mutators only — so no tick-window instrument, per-90 rate band, or round-resolution
> fit is perturbed, and the FR-PO-052 baseline is untouched (no per-tick work added on any existing
> path). No `SNAPSHOT_SCHEMA_VERSION` change, no new RNG stream, domain tag or draw site, no draw-order
> change. No ERR filed: nothing here contradicts an APPROVED spec.
>
> **Gate: NOT RUNNABLE in this environment.** The network policy blocks the .NET SDK download —
> `curl https://dot.net/v1/dotnet-install.sh` returns a proxy 403 — the same constraint
> `interactive-unity-client-design.md`'s own header records for the P0 landing. Verified instead by
> exhaustive manual review against source, plus a `generate_projects.py` run confirming the new
> `TacticalDirector.TestingStrategy` reference resolves and the test project is generated with it. The
> gate runs in CI on the PR; the per-suite counts are not restated here because they were not measured.
> **Prior entry below.**)
>
> **Last Updated (prior):** August 3, 2026, later same day (**§5.Z.23 — CONVERSION AT CONTACT: the recorded
> premise was refuted, and the real defect is that a keeper's CATCH never stopped the ball.**
> `gk-contact-rate-design.md` §7 item 1 recorded the goal-rate residual as *"marginal, end-of-envelope
> touches whose parries and spills keep the ball alive in the box"*, naming the Stage-0 `pointQuality`
> lottery and parry placement as the levers. That premise had never been measured. The new per-contact
> instrument (`GoalConversionDiagnosticTests`, env-gated `TD_CONVERSION_DIAGNOSTIC=1`, 3 full matches on
> the §5.Z.20–§5.Z.22 seeds) measures ball speed the tick before each contact and at the end of it:
> **parried 10.8 → 0.0, deflected 10.3 → 4.2, spilled 13.9 → 9.0, missed 9.5 → 9.5 — and caught
> 11.1 → 10.8**, one tick of drag. **The parries and spills work; the catch does nothing to the ball**,
> and **7 of 10 catches were followed by a goal within 5 s** (parries and spills: zero), with 14 of 15
> goals following a keeper contact within 10 s. **ERR-011-008**: #11 §3.5.2's catch branch is TWO
> statements — `Ball.SetPossessor(gkId)` **and** `ball.velocity = gkHandVelocity` ("parked at hand
> position") — and only the first was implemented, at both the catch and the Stage-0 smother claim.
> Possession is a FLAG in this engine, not a kinematic constraint (`RunPhysicsPhase` integrates the ball
> unconditionally; `CheckRestartAndApply` adjudicates a goal on ball POSITION), so a claimed shot flew
> on into the net with the keeper recorded as holding it. **§3.5.2's pseudocode body was correct** — the
> contributing spec defect is §3.5's **Outputs** summary, which named `SetPossessor` alone for the catch,
> and `IGoalkeeperBallSystem`, which exposed no seam for a park at all, so the omission was invisible
> from both the summary and the interface. Fixed with `ParkBall()` at both claim sites; summary restated,
> pseudocode untouched. **Measured (3 full matches, same seeds pre/post): caught-band exit speed
> 10.8 → 0.0 m/s, goals from caught contacts 7 of 10 → 0 of 11, goals over the corpus 15 → 11
> (5.0 → 3.7/match — the closest this engine has measured to football's ~2.7), scorelines
> 2-2/2-0/6-3 → 1-0/2-2/4-2.** At n=3 a 1.3 goals/match delta sits only just above this chain's noise
> bar; what carries it is that the mechanism's own signature (exit speed 10.8 → 0.0, band goals 7 → 0)
> does not depend on the goal count at all. **No `SNAPSHOT_SCHEMA_VERSION` change, no new RNG stream /
> domain tag / draw site, no draw-order change** — the park is a pure write to current-tick ball state.
> Locked by `match-engine-keeper-claim` (#19 ScenarioRunner, Tier B, 2 seeds × 90 min — full-match
> windows because a claim is rarer than a contact): **2 of 3 predicates fail on the pre-fix engine,
> verified by executing the scenario in a worktree at `4b12954` — `travellingAfterClaim = 6 of 6` and
> `concededWhileHolding = 5 of 6`** — plus `GoalkeeperClaimTests` (3: park XOR kick, per band).
> **Both levers §5.Z.22 named are recorded NOT fixed, with evidence rather than intent.** The
> `pointQuality` lottery is confirmed and quantified: quality 0.559 / 0.564 / 0.590 across *rising*
> contact marginality with catch rate 43% / 38% / **50%** — blind AND inverted — and
> `HandlingPointErrorSigmaM` provably cancels out of its own formula, a `[GT]` dial whose value cannot
> matter at any setting. The geometry-aware form was **implemented and measured rather than argued
> about**: it fixes the direction (0.261 / 0.255 / 0.150) and collapses the level to **zero catches and
> zero parries** (goals 3.7 → 4.3/match), because mean contact marginality is 0.68 and no `[GT]` inside
> #11's own ranges lifts the blend back over `CatchThreshold`'s 0.65 floor. **The ladder refuses; the
> next action is a design decision, not a calibration run.** Parry placement stays out on evidence — it
> produced zero goals in either corpus. **The creation residual is re-localized and is now a measured
> stage rather than "possession churn":** 306.7 final-third entries per match (football ~110) but only
> **20.0 penalty-box entries** (football ~45), and **0.68 shots per BOX entry, ABOVE football's ~0.55** —
> so neither shot selection nor the box is the bound. The bottleneck is the single transition
> **final third → penalty area, converting at 6.5% against football's ~40%**. Owner:
> `docs/tracking/gk-conversion-at-contact-design.md`; match-engine §5.Z.23. **AR-1 (full-gate
> fallout — one instrument, not the mechanism; the fourth instance of the §5.Z.22 AR-4 class):**
> `match-engine-shot-speed`'s `mean-shot-distance` predicate failed at 29.77 vs its 24.0 m ceiling.
> Three measurements settled it — the scenario **passes at the pre-fix commit**; the full-match
> diagnostic reads 29.5 / 12.9 / 19.5 m across the standing seeds (**21.7 m pooled over 41 strikes**,
> inside §5.Z.21's landed 16.5–27.1 m band, so no regression); and with everything else fixed the
> same corpus reads **27.11 m at 18 min, 24.71 m at 45 min, inside the ceiling over full matches**.
> **The shot-distance distribution is not stationary within a match** — early play is long-shot
> dominated and close-range strikes accumulate as box penetration develops — and this pass amplified
> that bias by removing a population of very-close-range REBOUND shots. Fixed in the ESTIMATOR
> (corpus 2 → 4 seeds, windows 18 min → full matches); **predicates and bounds UNCHANGED**, since a
> ceiling raised past the current reading would discriminate nothing. Full dotnet gate: **PASSED, 0 failures** (whole tree green, 30 assemblies; match-engine 376 → 376 with the failing shot-speed scenario now green (368 passed / 8 env-gated diagnostics skipped, up from 367 passed / 1 failed), goalkeeper-mechanics 78 passed; quarantine empty, so the full suite is enforced. Match-engine duration 26 m 6 s — up ~9 min, the cost of the new keeper-claim scenario (2 seeds × 90 min) and the shot-speed resize (4 seeds × 90 min); SDK 8.0.129 via apt))

> **Last Updated (prior):** August 3, 2026 (**PROJECT SKILLS LANDED — six workflow skills under
> `.claude/skills/`; tooling only: no code, no spec, no `src/` change, no gate run.** The recurring
> workflows this repo runs by hand are now Claude Code skills, checked into the repo rather than a
> personal skills directory, because each encodes conventions that live here and version with them:
> **`match-realism-pass`** (the §5.Z measure → localize → fix → calibrate → re-measure → lock loop, run
> 6 times in the §5.Z.17–§5.Z.22 chain), **`snapshot-schema-bump`** (the cross-tick decision plus the
> serializer/reader/pin/probe/round-trip checklist, over 19 bumps — two of which exist only to fix an
> earlier omission), **`err-file-and-backprop`** (id allocation against the live log, the entry
> template, spec-patch-in-the-same-commit), **`landing-close-out`** (the tracking-document sync),
> **`spec-promotion`** (supplement → 11-file set → the G1/G2/G3 gates, with G3 flagged
> non-self-grantable), and **`dotnet-gate`**. Each is derived from measured repetition in the last 200
> commits, and each carries the traps this project has actually hit — the id collisions
> (`ERR-030-015`, and the branch-vs-main class a check at authoring time cannot catch), the v17
> RNG-cursor hole, the instruments that broke because a pass moved the tick windows they hardcoded
> (three in the keeper-contact pass alone, one of which escaped to CI), the `[GT]` §6.3 → Appendix A
> gap that recurred in **all ten** promotions of the last wave, and the "offline sweep gives the shape,
> never the value" calibration lesson. **Deliberately NOT duplicated:** `adversarial-review` and
> `orientation` are invoked by the two skills that need a review step, never restated.
> **Merged with `main` twice while this branch was open**, and the second merge crossed main's
> documentation restructure — the `**Last Updated:**` chain moved out of `CLAUDE.md` into this file and
> OPEN ISSUES into `open-issues.md`, so this branch's `CLAUDE.md` edits were **redistributed into the
> new structure rather than merged textually**. In parallel main landed its own `.claude/` work (PR
> #283 `adversarial-review`, #284 the advisor council + orchestrator, #285/#287 `chat-review` and the
> SessionStart hook), so the directory now holds two kinds of thing — **agent patterns** that change
> who does the work, and the six **workflow encodings** above that change how a recurring job is done
> correctly. Only `orientation` remains account-level. `.gitignore` resolved to main's negation set (a
> strict superset), and `.claude/README.md` is the single index of the directory.
> **THREE DEFECTS FIXED IN THE SAME PASS, all found by auditing the docs against the tree rather than
> reading them:** (1) this chain carried **five** bare `**Last Updated:**` labels — the July-27 Track C
> Phase B, July-27 doc-sync, July-27 season-roll and July-26 root-doc entries each kept the current
> label instead of `(prior)`, leaving the file self-contradictory about its own currency (the defect
> `CLAUDE.md` had corrected three times before, which the split then carried across verbatim, and which
> `file-manifest.md` reproduced independently); all four relabelled, entry text untouched. (2) The
> `src/` assembly map in `CLAUDE.md` listed **`match-analytics` twice** with different Notes — one row
> from the July-27 doc-sync pass and a second from the Track C B6 landing; merged into one row carrying
> both facts (T0-only **and** the no-sim-assembly-may-reference-it layer guard). (3) The production
> assembly count read **30** in both PROJECT IDENTITY and the REPO STRUCTURE tree; disk has **31** —
> never updated when `match-client-web` landed in B6, and that assembly *is* in the map table, so the
> table and the prose disagreed. **Verified unchanged:** the `53 APPROVED / 0 IN REVIEW / 0 NOT
> STARTED` and `22 with no assembly` claims both re-derived from `SPEC_INDEX.md` registry rows and the
> assemblies on disk — correct as written. `landing-close-out` now encodes the one-bare-label
> convention so (1) stops recurring. See `.claude/skills/README.md`. Prior entry below.)
>
> **Last Updated (prior):** August 2, 2026, later same day (**Intra-layer acyclicity landed in `src/CLAUDE.md`;
> `ERR-020-002`'s one open question closed; open-issues re-filing pass — 18 → 10 active.**) Two follow-ups
> to the taxonomy filing. **(1) Acyclicity.** The proposal left one question for the owner: a flat tier
> permits intra-tier cycles, and two tiers now carry a real internal order (`match-client-core` →
> `ui-framework` → `match-client-web`; `season-save` → `living-world`). Sub-ranking Client and Management
> was the alternative and was rejected as brittle — it would need re-cutting every time a client assembly
> is added. The sentence is taken: *intra-layer references are permitted; intra-layer cycles are not*
> (proposed as `FR-CS-046a`, a sub-clause of FR-CS-046 rather than a new FR, so nothing renumbers). It
> documents an invariant **already enforced mechanically** — verified, not assumed: Unity rejects circular
> `.asmdef` references, and `tools/dotnet-ci/generate_projects.py` emits one `<ProjectReference>` per
> `.asmdef` reference (line 157), so a cycle fails the Linux compile gate too. It landed **now** in
> `src/CLAUDE.md` `### Reference Direction`, where it binds under today's three-layer taxonomy and does not
> wait on sign-off; §3.5.2 gains the same sentence when the tier order is signed off. `ERR-020-003` also
> sharpened: `src/CLAUDE.md` is the only one of the three renderings that labels its arrow, so it is the
> model for the fix rather than a fourth problem. **(2) Re-filing pass over `open-issues.md`** — the
> second flagged item from the `CLAUDE.md` split, where a deliberately conservative classifier left
> everything ambiguous in the active file. Eight entries archived: **six closed but never moved** (#18 and
> #19 both APPROVED May 15, 2026 and stale by fourteen months, their own text already reading "superseded
> above; entry retained for history"; ERR-030-014, resolved at §5.Z Phase H; the A4a blocker, superseded
> by its own July-28 UPDATE four days after opening; the Fixed64 scope decision, a decision record rather
> than an issue; the naming-convention reconciliation, complete May 6, 2026) **plus a duplicated pair** —
> the tactical-theory entry appears twice, and diffing them showed the copies are not equivalent: one
> predates the same-day CORRECTED/REVERTED review pass and still lists a test seam the item-(3) revert
> removed. Both are archived, canonical first, superseded second, so the correction history survives.
> **Three titles amended** to lead with what remains open rather than what has landed (`floatModelHash`,
> GK/Heading Phase 1, and the #23–#26 supplements — all four of which are now approved specs, leaving only
> #26's §9.2 `[GT]` balance review). **Bodies are preserved byte-for-byte** and asserted so before write;
> the only additions are a dated status clause inside each bold title and one italic *Re-filed* note.
> Where a title contradicted its own body — two did — the body wins and the note says which. Root
> `CLAUDE.md`'s index regenerated from the active set: **10 active / 41 resolved**. Prior entry below.)

> **Last Updated (prior):** August 2, 2026 (**ERR-020-002 + ERR-020-003 filed, both OPEN — the assembly layer
> taxonomy back-prop the `src/CLAUDE.md` split surfaced.**) Spec #20 §3.5.2 places **19 of the 31 assembly
> folders now in `src/`**; the twelve unplaced are `living-world`, `match-analytics`, `match-client-core`,
> `match-client-unity`, `match-client-web`, `match-engine`, `match-viewer`, `player-database`,
> `player-progression`, `season-save`, `tactical-instructions`, `ui-framework`. FR-CS-046 is decided
> relative to two layer memberships, so it currently decides nothing about ~39% of the tree — including
> every reference into or out of the composition root, which is precisely the part still being built.
> **ERR-020-002** proposes a ten-tier order (Foundation / Physics / Configuration / Mechanics / AI / Data /
> Composition / Management / Presentation / Client, with Infrastructure out-of-band) covering all 31
> folders, **derived from the `.asmdef` reference graph rather than folder names** and verified against the
> whole graph before proposing: zero upward references, 29 intra-tier references all pre-existing and
> acyclic. Adopting it therefore changes nothing that exists and constrains only future code, which is
> both its value and why its cost is zero. It also retires §3.5.2's stale empty `UI (Stage 1+ — not
> specified yet)` row (four UI/client assemblies exist; #38 is APPROVED) and strikes `code-standards` from
> `src/CLAUDE.md`'s infrastructure table (no such folder; #20 is a style guide). **Spec #20 is deliberately
> untouched** — layer membership is its authority and wants owner sign-off, and a wrong answer written into
> the authority file is worse than a documented gap; the ⚠️ note in `src/CLAUDE.md` names the gap and now
> cites the filing. The one call worth arguing with is `player-database` at tier 5 (above AI, below
> Composition): no gameplay-layer assembly references it today, and seating it there is what keeps physics
> and AI operating on struct parameters rather than squad rows. **ERR-020-003** (Low) came out of the same
> verification: §3.5.2 draws `Physics ──► Mechanics ──► AI ──► UI` while the root `CLAUDE.md` states `AI →
> Mechanics → Physics, never the reverse` — the same rule with opposite arrows and neither notation
> labelled. The code follows the `CLAUDE.md` reading; no violation exists, so this is a notation fix, not a
> behaviour one. `spec-error-log.md` → v1.54. Prior entry below.)

> **Last Updated (prior):** July 28, 2026, latest entry of the day (**KEEPER CONTACT RATE — §5.Z.20 §7.1's
> residual, BOTH NAMED LEVERS LANDED, MEASURED; the goal-rate residual moves to conversion AT
> contact.** Measured per episode at the ball's goal-plane crossing (new env-gated
> `GkContactRateDiagnosticTests` — a frame aggregate cannot attribute position vs timing): of 15
> crossed un-contacted threat episodes at baseline, **9 were dive-early with the dive over 456–2000 ms
> before the ball arrived and dive-late exactly 0** — the commit was never slow, always too eager —
> plus 3 no-dive, 3 lateral-miss, with the lateral need at the crossing (1.91–3.83 m) at or beyond the
> dive's ~3.55 m total coverage. **ERR-011-007** — #11's `Anticipate → Diving` row was unconditional on
> `SaveIntent`, so the fixed 600 ms dive envelope opened and closed during the ball's 925–2006 ms
> flight; new #11 §3.3.6 commit-to-arrival gate (hold the coiled keeper until predicted time-to-plane ≤
> a lateral-need-scaled commit lead, `[GT] DIVE_COMMIT_MIN_LEAD_FRAC`; ONE crossing predictor shared
> with the ERR-011-003 dive direction so timing and direction cannot drift). The §3.2.3 window anchor
> refined to the keeper's first decision opportunity at/after the live stamp — the first full-corpus
> run measured the window §5.Z.20 fixed collapsing back to ~0 under the hold (the shot is usually
> struck AFTER the intent commit and re-stamps the episode), the pass's one calibration iteration.
> **ERR-012-010** — #12 §3.3.3's GK-slot lateral term (`GK_LATERAL_FACTOR × basisY` over the pitch
> width: ±2 m of travel over 68 m) becomes the BALL-LINE point clamped inside the goal mouth
> (`[GT] GK_LATERAL_CLAMP_M` = 3.0 replaces the factor, retired not retuned — no value of a
> pitch-anchored gain expresses goal-anchored tracking; central ball is the exact pre-fix identity).
> **Measured over 3 full matches, same seeds pre/post: contacted episodes 8 → 23, crossed
> un-contacted 15 → 9 (contact rate ~35% → ~72%), deep dive-early GONE (residue 83–183 ms = the 10 Hz
> grid), catches 6 → 10, window at contact 0.34–0.44 — and goals 14 → 15 over the corpus, UNCHANGED at
> n=3.** The §5.Z.17 shape again: "contact rate → goals/shot" assumed a contact stops the shot, and
> that premise does not survive tripling the contact count — the added contacts are marginal
> end-of-envelope touches whose parries and spills keep the ball alive in the box (one match ran 6-3
> on such chains). **The honest next lever is conversion AT contact: the Stage-0 pointQuality lottery
> (E ≈ 0.68, attribute-blind) and parry placement (nothing steers a parry away from the goal mouth).**
> **No `SNAPSHOT_SCHEMA_VERSION` change, no new RNG stream / domain tag / draw site, no draw-order
> change** — both mechanisms are pure functions of the current tick's ball state and keeper position.
> Locked by `match-engine-keeper-contact` (#19 ScenarioRunner, Tier B, 2 seeds × 45 min) — **3 of 4
> predicates fail on the pre-fix engine, verified by executing the scenario in a worktree at the
> pre-fix commit** (`heldCommits = 0` — the hold is structurally impossible pre-fix; contacts 3 vs 4
> crossings, inverted; one deep dive-early) — plus `GoalkeeperCommitGateTests` (11), four ball-line
> GK-slot locks, and the `GoalkeeperConversionTests`/save-launch-scenario re-anchor (a parked ball now
> correctly HOLDS the dive — the Phase-H "tests encoded the old contract" class, intent preserved).
> **The full gate then failed two INSTRUMENTS — neither a defect in the landed mechanisms (AR-4):**
> the shot instruments sampled the strike from `BallView` at END of the strike tick with the attacked
> goal named by the sampled velocity's x-sign, and this pass made same-tick post-strike touches common
> enough to break that — a measured 13 m strike read as **92.3 m** (velocity reversed by a touch ⇒
> wrong goal), driving `match-engine-shot-speed`'s distance mean to 51.80 vs its 24.0 ceiling, with
> the same dilution having left the speed-mean floor a 0.08 margin; fixed at the root with the
> strike-TIME `TestOnly_LastShotStrikePosition/Velocity` seam (captured beside the `_shotContacts`
> increment — post-ApplyKick, before anything else can move the ball) consumed by the scenario AND
> `ShotOutcomeDiagnosticTests`, plus 9 → 18 min/seed windows (this pass thinned 9-min windows to 3
> strikes — a per-sample lottery; predicates/bounds UNCHANGED, measured clean distMean 22.7). And the
> P1 observer-neutrality test's non-vacuity guard tripped because this pass moved its seed's first
> restart ~3 900 → 7 270 ticks; window re-measured 6 000 → 8 000, guard intact. **A THIRD instrument
> of the same class then surfaced on the PR's Linux CI gate (AR-5):** the #37 MatchAnalytics liveness
> test measured away possession at exactly 0 because this pass moved its seed's away-possession onset
> past the 30 s window (first accrual measured between ticks 1 800 and 2 400); window re-measured
> 1 800 → 3 600 ticks, assertions unchanged.
> **Full dotnet gate: PASSED, 0 failures.** See `docs/tracking/gk-contact-rate-design.md` +
> `match-engine-design.md` §5.Z.22 + `spec-error-log.md` v1.53 + src/CLAUDE.md v2.50. Prior entry below.)
> **Last Updated (prior):** July 28, 2026, last entry of the day (**A4a STEP 0 PASSED — the round-resolution
> calibration corpus is worth fitting for the first time.** Re-run after the §5.Z.17–§5.Z.21
> match-realism chain: over the same 20 keyed matches (dSquad ±6.0), **strong-at-home mean margin
> +7.100, strong-away mean margin −4.700** — the ramp extremes separate IN BOTH DIRECTIONS (the strong
> away side wins 9 of 10 at 5.8 goals/match where the July-26 runs had it scoring 0–2 across every
> match), upsets exist (the strong side loses 3–4 in one row), and the §5.Z.11 venue asymmetry is down
> from ~15× to **~1.5× on margin** — a modifier on the strength signal rather than the signal itself
> (recorded as a fit caveat: the model's home term absorbs it, KD-8's re-capture rule applies if a later
> pass shrinks it). **One instrument fix (found by execution):** the first post-play pilot FAILED at
> teardown with every assertion green — a PLAYING match emits FM-08/FM-03 possession-race errors as
> ordinary match events (§5.Z.7), and the calibration drivers predate play developing; both env-gated
> drivers now carry the standard `LogAssert.ignoreFailingMessages` wrapper
> (`RoundResolutionCalibrationHarnessTests` v1.1), and the re-run with the fixed instrument reproduced
> the identical 20 rows (deterministic keyed seeds — verified byte-identical) and PASSED. **Next A4a
> action: the corpus slices + `tools/round-resolution-fit.py` (~1.4 h across four processes), its own
> roadmap item.** Docs: `round-resolution-corpus.md` v0.3 (§1.b full CSV) + the §5.Z.11 and
> path-to-playable OPEN ISSUES updates. **Full dotnet gate: PASSED, 0 failures.** Prior entry below.)
> **Last Updated (prior):** July 28, 2026, latest same day (**SHOT VOLUME — §5.Z.19's remaining lever (a), FIXED,
> CALIBRATED AND MEASURED — and the calibration ladder REFUSED half the design target, which is the
> finding worth keeping.** Measured first (`ShotOutcomeDiagnosticTests` v1.3 gains per-shot distance +
> possession-churn context): the finding is the DISTRIBUTION, not the count — mean shot distance ran
> **30–34 m** against football's ~17, ~60% of shots beyond 22 m, clustered AT the §3.1.4.2 range-gate
> boundary. Cause (**ERR-008-017**, verified against source): #8 §3.2.3.1's `U_SHOOT` has **no distance
> term**, and `GoalOpeningScore` is scale-free by construction (goal arc and near-goal-blocker occlusion
> both shrink ~1/d) — within range a 34 m shot scored identically to a 10 m one, while football's
> P(goal|shot) falls ~tenfold over that span; the ERR-008-016 class, the formula omitted the strongest
> single predictor of shot value in the game it models. Patched (spec + code same commit): §3.2.3.1 gains
> `DistanceQuality_SHOOT` — 1.0 inside `[GT] SHOOT_SWEET_RANGE_M` = 12 m (every close-range utility
> BITWISE untouched, so the §5.Z.17–§5.Z.20 calibrations stand), hyperbolic decay
> `FALLOFF/(FALLOFF + (d−SWEET))` beyond; the range gate stays the hard cap (a preference, not a cliff —
> the ±0.15 composure-noise band still lets an adventurous agent take the occasional 30 m shot, which is
> football). **The four-rung falloff ladder (3 full matches per rung, same seeds) showed count ≈ 25 AND
> mean ≤ 22 m are NOT jointly reachable by this lever:** FALLOFF 9 hits 24.0 shots but keeps 39% long
> shots and goals at 7.7; once long shots correctly lose to passes, volume is bounded by close-chance
> CREATION, and at ~3× football's final-third churn almost no possession penetrates the box (0.05
> shots/entry vs football's ~0.2). **`[GT] SHOOT_DIST_FALLOFF_M` = 8 chosen — the distribution + goal-rate
> landing: shots 31/35/38 → 17/19/17, long-shot share 60% → 30%, goals 8.0 → 4.7/match (the closest this
> engine has ever measured to football's ~2.7), scorelines 2-2 / 3-2 / 5-0** — the roadmap chain wants a
> goal rate that makes the A4a corpus worth fitting, and a football-shaped distribution at 18 shots serves
> that strictly better than a football-count 24 still dominated by range-boundary strikes. Speed floors
> unaffected (the decay changes which shots are TAKEN, not how they are struck). **No
> `SNAPSHOT_SCHEMA_VERSION` change, no new RNG stream / domain tag / draw site, no draw-order change.**
> Locked by the `match-engine-shot-speed` scenario's new mean-shot-distance ceiling (**fails on the
> pre-fix engine at exactly 30.0 vs 24.0, verified by execution before the scorer change landed**) + 5
> `UtilityScorerTests` locks; one existing lock re-anchored with intent preserved (a zone-ratio test at
> 28 m where the decay pushes the suppressed branch into the UTILITY_FLOOR clamp — found by execution,
> AR-3), and the `match-engine-shot-outcomes` corpus resized 9 → 18 min/seed after the full gate caught
> its goals-still-scored reachability predicate at zero goals on the calibrated neutral path (the
> keeper-conversion corpus-sizing lesson, AR-4). Recorded, not fixed: the churn/creation residual (now also owning the count gap) and the midfield
> long-shot machinery being production-unreachable dead surface (zone minimum 40 m vs range-gate maximum
> 35 m). **Full dotnet gate: PASSED, 0 failures.** See `docs/tracking/shot-volume-design.md` +
> `match-engine-design.md` §5.Z.21 + `spec-error-log.md` v1.52 + src/CLAUDE.md v2.48. Prior entry below.)
> **Last Updated (prior):** July 28, 2026, later same day (**KEEPER CATCH/PARRY CONVERSION — §5.Z.19's residual
> lever (c), the dominant goal-rate term, FIXED, CALIBRATED AND MEASURED.** The §3.2.3 reaction window —
> 30% of #11 §3.5.1's handling-quality blend — was structurally dead: re-evaluated every frame, so the
> value the contact consumed was dated by the ball's whole FLIGHT time (**ERR-011-005** — the spec's own
> §3.2.5 worked example scores the dive COMMIT; now computed once at the dive-launch frame and FROZEN),
> and the detection stamp was never cleared, so dives were dated against shots struck **85–349 seconds**
> earlier, with rebound/deflection episodes having no anchor at all (**ERR-011-006** — the stamp now dies
> with its episode via `ClearSaveIntent`/save resolution, and the new `OnThreatArmed` seeds it at episode
> onset through the same §3.2.1/§3.2.2 formulas; a live stamp always wins, so the stamp itself is the
> latch — already serialized in the v19 GK block, **no new engine state**). Baseline windows at contact:
> 0.000/0.000/0.199, one catch in three full matches. Plus the KD-C3 `[GT]` recalibration, all inside the
> #11 §3.4.3/§3.4.5 spec ranges, over two measured full-match iterations: `ReactionBaseMs` 350 → 220,
> `ReactionBallSpeedCoeff` 8 → 3, tolerances 120/80 → 200/140 (the engine's discrete ~100–300 ms commit
> grid scored as deep-early against human-continuous-time values ⇒ window ≈ 0 for every producible dive),
> and `HandlingBase`/`HandlingKAttr` 0.45 → 0.60 + `CatchThreshold` 0.78 → 0.74 (the Stage-0 pointQuality
> term is a fixed noise lottery — E ≈ 0.68, invariant under every `[GT]`, blind to attributes, recorded
> not fixed). **Measured over 3 full matches, same seeds pre/post: window at contact 0.000 → 0.30–0.67,
> elapsed-when-airborne 85–349 s → ~0.3 s, quality at contact 0.36–0.50 → 0.41–0.79, catches 1 → 6 of 15
> contacts, goals per match 14.7 → 8.0 (13/13/18 → 6/9/9), goals per shot 0.38–0.42 → 0.19–0.26 at 31–38
> genuine strikes/match; scorelines 8-5/7-6/13-5 → 3-3/6-3/8-1 — the engine's first football-plausible
> scorelines.** The measurement also BOUNDS what remains of lever (c), and it is not conversion: a contact
> almost always stops the shot, and the keeper meets only ~¼ of on-target shots — the CONTACT RATE (#12
> GK-slot lateral positioning + commit-to-arrival timing, mean lateral offset 1.7–4.6 m while airborne) is
> the residual, a behaviour change to APPROVED specs rather than a `[GT]` dial, recorded with shot volume
> (lever (a)) as what bounds the remaining ~3× gap to football's ~2.7. **No `SNAPSHOT_SCHEMA_VERSION`
> change, no new RNG stream / domain tag / draw site, no draw-order change.** Locked by the new
> `match-engine-keeper-conversion` acceptance scenario (`ConfigureSquads` path — the neutral-path draft
> failed its own hold predicate because the conversion did not transfer across shot populations, the
> §5.Z.19 AR-4 class reproduced) + the 7-lock `GoalkeeperConversionTests` fixture driven through the real
> orchestrator. **Instrument fallout caught before any gate run:** `match-engine-shot-speed` and
> `ShotOutcomeDiagnosticTests` counted "shots" off `ShotDetectedTickMs` edges, which the arming stamps
> redefine as threat episodes (≥ 3 m/s rollers included) — both re-anchored to the new
> `MatchEngine.TestOnly_ShotContacts` genuine-strike counter. **Full dotnet gate: PASSED, 0 failures (whole tree green — 30 suites; match-engine 360 → 366, goalkeeper-mechanics 55 → 62).**
> See `docs/tracking/gk-catch-parry-conversion-design.md` + `match-engine-design.md` §5.Z.20 +
> `spec-error-log.md` v1.51 + #11 `section-3.md` v0.4 + src/CLAUDE.md v2.47. Prior entry below.)
> **Last Updated (prior):** July 28, 2026 (**SHOT SPEED + THE PHYSICAL GOAL FRAME — §5.Z.18's residual lever (b),
> FIXED, CALIBRATED AND MEASURED.** The engine's strikers were tapping the ball at 10–30% power: #8
> §3.5.3's `PowerIntent = clamp(goalOpening × A_Finishing, 0.1, 1.0)` is a product of two [0,1]
> fractions that pinned nearly every shot at its own 0.1 clamp floor (**ERR-008-016** — patched to
> floor-plus-modulation with `[GT] POWER_INTENT_FLOOR` = 0.65; the spec's "low opening ⇒ reduce power"
> rationale inverted the game it models), and #6's `VFloor = 10` anchored a neutral FULL-power vBase at
> ~16 m/s before reducers (**ERR-006-004** — retuned 10 → 24 over two measured calibration iterations;
> the formula multiplies the ceiling span by attrFraction AND powerIntent, so the anchor must carry the
> base pace). Composed, measured shot-tick means ran 6.9–10.3 m/s against football's ~20–25. And because
> a football-pace ball moves **~0.42 m per 60 Hz tick**, fixing the speed made the goal frame's absence
> load-bearing (**ERR-001-005**): a discrete per-tick test TUNNELS through a 0.12 m post, and boundary
> adjudication at the detected position (up to 0.42 m past the plane) misread a rising ball crossing
> UNDER the bar as over it. New `BallCollision.ApplySweptGoalFrameCollision` — the tick's movement
> segment against six capped cylinders (post axes half a diameter OUTWARD of the 7.32 m inner-edge box,
> bar axis half a diameter ABOVE the 2.44 m lower edge — the same IFAB datums the box test uses),
> earliest hit wins, response is the existing restitution model, **`ApplyGoalPostCollision`'s first
> production caller** — plus a `CheckBoundaries` prevPosition overload adjudicating at the interpolated
> plane crossing. Engine wiring is capture-before-integrate / collide-after-integrate;
> `_prevTickBallPosition` is WITHIN-tick (the `RestartAppliedThisTick` class) — **no
> `SNAPSHOT_SCHEMA_VERSION` change, no new RNG stream / domain tag / draw site, no draw-order change.**
> **Measured over 3 full matches, same seeds pre/post: shot-tick means 6.9–10.3 → 14.7–16.1 m/s, maxima
> 15.3–18.9 → 23.3–27.6; shots per match 59–70 → 31–45 (football ~25 — pace ends possession episodes
> decisively, so lever (a) shot volume is ~half discharged as a side effect); woodwork 0 → 1/0/5
> strikes/match; and goals per shot ROSE 0.14–0.25 → 0.38–0.42 (goals 12.3 → 14.7/match) — a
> football-pace shot beats this keeper far more often than a roller, so the catch/parry conversion
> (§5.Z.17 §7.5, lever (c)) is now unambiguously the dominant term in the goal rate, measured against
> real pace for the first time.** Locked by `match-engine-shot-speed` (#19 ScenarioRunner, Tier B, 2
> seeds × 9 min + scripted front-face frame probes, ~46 s) — **5 of 7 predicates fail on the pre-fix
> engine, verified by executing the scenario in a worktree at the pre-fix commit** (speed floors
> unreachable at mean 8.90 / max 17.59 on the calibrated `ConfigureSquads` path; both frame probes
> adjudicated as exits; the rising crossing misread as a goal kick — and the scenario's first draft
> sampled the NEUTRAL path, whose floors did not transfer: the full gate caught it, AR-4) — plus
> `SweptGoalFrameTests` (11, headlined by the tunneling
> discriminator) and 3 PowerIntent locks. Design AR-3 recorded a probe-geometry finding worth keeping:
> an UNDERSIDE bar strike reflects down-and-in and legitimately scores (football's in-off-the-bar), so
> a no-goal rebound probe must strike the frame front face. **Full dotnet gate: PASSED, 0 failures.**
> **A4a's realism gate advances again; the named next levers are the keeper's catch/parry conversion
> and the remaining half of shot volume.** See `docs/tracking/shot-speed-woodwork-design.md` +
> `match-engine-design.md` §5.Z.19 + `spec-error-log.md` v1.50 + src/CLAUDE.md v2.46. Prior entry below.)
> **Last Updated (prior):** July 27, 2026, latest same day (**SHOT-OUTCOME DISTRIBUTION — §5.Z.17's residual, the
> named A4a blocker, FIXED AND MEASURED.** The four defects that made every outcome class except "goal"
> structurally unreachable are closed, each with its ERR filed and the spec patched where the spec was the
> defect: **ERR-006-002** — `ShotExecutor` discarded `finalDirection.z` and rebuilt the vertical from
> `sin(launchAngle)`, leaving the whole vertical half of the placement/error model inert *against the
> spec's own* `finalVelocity = finalDirection × kickSpeed` (§3.5.7); conformed, with the §3.5.6
> launch-tilt aim composition. **ERR-006-003** — the error cone was not a cone: angular error mapped to a
> **fixed 0.128 m/° at every range** (the spec's own reference-anchored form was 0.35 m/°, correct only at
> exactly 20 m); now `tan(err) × distance` at the goal plane, which reproduces the spec's 20 m value
> exactly and misses wide from range. **ERR-001-004** — the spec's own §3.1.10.3 pseudocode gated EVERY
> boundary test behind `z < 0.22 m`; gate removed from `CheckBoundaries` AND `IsOutOfBounds` (Law 9/10) —
> **the goal has a crossbar**, an airborne crossing adjudicates at the crossing (goal under the bar,
> out above/wide, throw-in in the air). **ERR-003-007** — the empty-TODO `OnAgentCollision` is live:
> `BallCollision.ApplyAgentDeflection` (#1 §3.1.10.1 `BodyPartCoefficients`, first consumer), gated
> Controlled-out / sub-`[GT] AgentDeflection.MinBallSpeedMps`-out, with the approaching-only response as a
> **stateless** self-block guard — no cooldown, no schema bump. Plus the `ShotWorldAdapter` pressure query
> live (was hardcoded `0f`; reuses the first-touch `PressureEvaluator` with the §5.Z.14 un-mirror) and
> `MIN_GOAL_VISIBILITY` 0.05 → 0.12 (it equalled the `GOAL_OPENING_MIN` floor, so the SHOOT gate could
> never fire). **Measurement drove two design reversals (AR-3):** the deflection gate was designed at 18 m/s
> against an assumed 20–35 m/s shot band — measured shots run **12–21 m/s**, so 18 would have made almost
> every shot unblockable (re-anchored to 10, with reception protected by GEOMETRY: the 1.0 m first-touch
> trigger reach sits well outside the ~0.4 m hitbox and a ball cannot jump the gap in one 60 Hz tick below
> ~35 m/s — pass speeds reach 28, so no speed gate can separate pass from shot); and the acceptance
> scenario's first draft failed its own determinism predicate by interleaving its two engines — the
> documented §5.Z.7 process-static-EventBus property, reproduced before the scenario ever tested the fix.
> **Measured over 3 full matches, same seeds pre/post: goals 15.3 → 12.3 per match, goals/shot 0.24–0.29 →
> 0.14–0.25, fast-ball body deflections 0 → 560–612 per match.** Every mechanism is now real; the remaining
> mass is NOT these mechanisms and is recorded, not fixed: **shot volume** (59–70/match, ~2.5× football — a
> DT-selection/possession-churn property `MIN_GOAL_VISIBILITY` barely dents) and **shot speed** (means
> 7–10 m/s vs football's ~25 — #6 `VFloor`/`VCeiling` × #8 `PowerIntent` shaping), which keeps shots on the
> ground (the new crossbar rarely bites) and hands keepers easy contacts they still rarely hold (§7.5).
> Locked by `match-engine-shot-outcomes` (#19 ScenarioRunner, Tier B, 4 seeds × 9 min, ~59 s) — **3 of 8
> predicates fail on the pre-fix engine, verified by executing the scenario in a worktree at the pre-fix
> commit** (the over-bar crossing adjudicated as *nothing* — `cue=None` — the under-bar crossing scoring
> nothing, deflections exactly zero); two airborne-adjudication predicates are scripted-stimulus probes
> because natural airborne line-crossings above 1 m are rare in 36 min of play (a natural floor would be
> flaky for the wrong reason — recorded un-asserted instead). Plus 17 unit locks, two tests inverted with
> intent preserved (they encoded the old z-gate contract — the Phase-H class), and the env-gated
> `ShotOutcomeDiagnosticTests` instrument (`TD_SHOT_DIAGNOSTIC=1`). **Full dotnet gate: PASSED, 0
> failures; no `SNAPSHOT_SCHEMA_VERSION` change, no new RNG stream / domain tag / draw site, no
> draw-order change** — digests move for any match containing a shot or an airborne crossing, as intended.
> **A4a remains gated on match realism, but its named blocker is discharged; the next levers are shot
> volume, shot speed, and the keeper's catch/parry conversion.** See
> `docs/tracking/shot-outcome-distribution-design.md` + `match-engine-design.md` §5.Z.18 +
> `spec-error-log.md` v1.49 + src/CLAUDE.md v2.45. Prior entry below.)
> **Last Updated (prior):** July 27, 2026, latest same day (**GOALKEEPER SAVE PIPELINE — §5.Z.15's named lever,
> measured and discharged. Three correctness defects fixed; the goal rate barely moved, and that is the
> result.** §5.Z.15 recorded the next lever on the engine's ~4.7×-football goal rate as *"the quality of
> the goalkeeper's save, not further shot or finishing tuning"*. That framing carries a premise — that
> saves happen and are merely poor. **They did not happen.** Measured over three full 90-minute matches,
> the keepers made **zero** hand contacts with the ball across all six keeper-matches. "Save quality" was
> not a low number; it was undefined. **Nothing in the tree could have said so, because no instrument had
> ever reported a goalkeeper statistic of any kind** — the ERR-030-014 class again, one level further in.
> New env-gated `GkSaveDiagnosticTests` reports the pipeline as a **funnel** (`armed → SAVE committed →
> Anticipate → Diving → Airborne → contact → caught`) because a funnel localises WHERE a chain breaks
> instead of only reporting its end empty; every stage up to and including the dive fired healthily
> (14–41 commits, 13–31 dives a match) and the chain ended at **contact, at exactly zero**. Three defects,
> each independently sufficient: **ERR-011-003** — the dive had **no direction**
> (`ComputeDiveDirectionLateral`'s only non-zero branch is gated on `SaveIntent.DeflectionTarget`, which
> the engine's sole producer sets `null`; measured mean `|diveDirectionLateral|` = **0.000** across every
> dive ever launched, with the envelope's closest approach to the ball **2.75 m short** over a whole
> match — not a near miss, the keeper dived straight up on the spot. The cause is a conflation:
> `DeflectionTarget` is where the keeper wants to PUT the ball, not where it should DIVE); **ERR-011-004**
> — a catch was **arithmetically impossible**, since `OnShotExecutedEvent` had zero callers in production
> *or tests*, pinning `reactionWindowAchieved` at 0 and capping §3.5.1's blend at a **measured 0.630** for
> a PERFECT keeper against `CatchThreshold` 0.78; **ERR-011-002** — the keeper **woke for the wrong end of
> the pitch** and never stood down (the orchestrator computed the third the keeper's own team ATTACKS and
> passed it to a state-machine parameter documented as the OPPOSING team's — the §5.Z.12 per-side-pair
> class — while `Anticipate` had no exit but a dive, so keepers held it **76–92% of every match**).
> **Measured effect: dive direction 0.000 → 1.000, best miss 2.75 m → −0.07 m, contacts 0 → 15, Anticipate
> share 76–92% → 11–18% — and goals per match 15.3 → **15.3**, i.e. UNCHANGED, against football's ~2.7.** Three genuine
> defects, each of which had to be fixed before a save was possible at all, are worth about **one goal a
> match**. The named lever was real and is now spent; **it was not where the mass is** — the same shape as
> §5.Z.9 and §5.Z.11, where the measurement refuted its own brief. Locked by the new
> `match-engine-goalkeeper-saves` acceptance scenario (#19 ScenarioRunner, Tier B, 4 seeds × 15 min, 56 s),
> which asserts **reachability** stage by stage and deliberately pins **no** save percentage and **no**
> goal rate — a band here would pin a number this pass did not earn. **11 of its 12 predicates fail on the
> pre-fix engine, verified by executing it against reverted production files rather than inferred**, three
> at exactly zero. **Full dotnet gate: PASSED, 0 failures** (match-engine 358 → 360 passed); **no
> `SNAPSHOT_SCHEMA_VERSION` change, no new RNG stream / domain tag / draw site, and no change to the draw
> order.** **RECORDED, NOT FIXED — and this is now the honest next lever, each verified against source:**
> a shot **essentially cannot miss the goal** (aim is hardcoded to `u ∈ {0.1, 0.9}`, i.e. **0.732 m inside
> the post**, against ~2.25° of typical angular error where >5.73° is needed — and `ShotExecutor` never
> reads `finalDirection.z`, so the entire vertical half of the placement and error model is inert); there
> is **no crossbar** (`BallCollision.CheckBoundaries` gates EVERY boundary test, goals included, behind
> `z < Ball.Diameter` = 0.22 m, so a ball crossing the line airborne is neither a goal nor out of play —
> the goal is 7.32 m wide and of unbounded height); and there are **no blocked shots**
> (`BallCollisionHandler.OnAgentCollision` is called in production and its body is an empty `TODO`; posts
> are non-physical). In football roughly **30% of shots are blocked and 30% miss the target**; here both
> are approximately zero, which is a larger multiplier on the goal rate than anything a goalkeeper does.
> **A4a remains blocked — but the reason is now specific: the shot-outcome distribution, not the keeper.**
> See `docs/tracking/goalkeeper-save-pipeline-design.md` + `match-engine-design.md` §5.Z.17 +
> `spec-error-log.md` v1.48 + src/CLAUDE.md v2.44. Prior entry below.)
> **Last Updated (prior):** July 27, 2026, latest same day (**TRACK C PHASE B IS COMPLETE — B3, B4 and B6
> landed and `PM-1` (a playable match) is REACHED.** A person can now open a browser on the running
> client and watch a real match — live pitch, clock, score, period, restart captions — change a team's
> mentality / pressing / passing and see it queued and applied on a tick boundary, substitute, pause /
> resume / run at 1–10×, and read live statistics that keep serving after full time. **B3 (#37 T1)** is
> the read-only per-tick ledger tap: a `TickLedgerSnapshot` the engine fills in the Snapshot phase,
> **after `SerializeLedger` and before the bus resets the tick — the only moment the records both exist
> and are identified with a tick** — copying rather than indexing the process-static ring, so
> "current-tick scoped" is structural rather than documentary, and sized from `EventQueueCapacity` so
> overflow is impossible by construction. It reuses `SerializeLedger`'s own canonical-order walk
> (extracted to `EventLedger.BuildCanonicalOrder`), so the digest bytes and the observer cannot drift
> apart; and the §3.2 routing table branches on `EventRegistry.GetOrdinal<T>()` rather than a local
> ordinal table, so it cannot fall out of step with Appendix A. **`GetOrdinal` now calls
> `EnsureInitialized()` first** — `EventOrdinalCache<T>` is a separate static-generic type, so a first
> caller would otherwise read 0 for every type and silently match nothing: the static-init-order trap
> this project has now hit three times. Surfaced **ERR-037-002** — §3.4 states the territorial split as
> two strict inequalities and then requires it to be **total**; both cannot hold at exactly `x == L/2`,
> which is not a limit case but where a kickoff parks the ball for many consecutive ticks. **B4 built
> two of its three items and refused the third:** `FrameInterpolator` (speed-aware alpha, because at 3×
> the same wall-clock covers three times the simulated time; and **snap-not-smooth across a
> discontinuity** — a restart teleports the ball, a substitution swaps who occupies a roster slot, and
> blending either draws a glide where the truth is a jump) and `FollowBallCamera` (dead-zone trailing,
> `1 − e^(−rate·dt)` **proven** frame-rate-independent by step subdivision rather than asserted, and a
> clamp that CENTRES when the view is wider than the pitch instead of returning whichever crossed bound
> compared last). The third, a live-stats accumulator, **is #37's aggregator** — a second one would be
> the parallel-surface trap. **B6's finding is that the obvious implementation was the wrong one:**
> extending `LiveMatchServer` would have given the spectator surface a mutation channel, which is
> exactly what ERR-038-001 and the interactive-client AR-1 H-2 rejected — the streamer holds the engine
> and that server holds no engine reference *by construction*. So the mutating surface is a new
> host-free assembly `src/match-client-web/` **above** `match-client-core`, with three routes carrying
> three privileges (reads change nothing; `/playback` changes *when* ticks happen, never what is in
> them, so it never enters the replay log; `/intent` alone mutates, and only through the tick-stamped
> `ManagerCommandQueue`) — each asserted against the command queue rather than by inspection. Router
> and transport are separate types, so every routing decision is a pure function under test and the
> socket code decides nothing. It also needed a genuinely **new seam**: #37's every-tick contract cannot
> ride the pre-tick hook, which is set-once, already taken by the command drain, and also fires from
> `ServiceOnce()` where no tick advances — so `LiveMatchStreamer` gains a read-only
> `SetPostTickObserver` that **disarms and latches** its first exception rather than killing the sim
> thread (the pacing loop does not guard `TickOnce`, and a derived statistic must not be able to end a
> match — nor be swallowed, since a frozen report reads as merely stale). Governed by the new
> `docs/tracking/browser-match-client-design.md`. **Then the two mechanical layer guards failed, and
> correctly:** both `NoOtherAssemblyReferencesMatchAnalytics` and `NoOtherAssemblyReferencesTheUiFramework`
> were written as *"nothing references me"* while the invariant each names is *"no **sim** assembly
> references me"* — the first legitimate consumer exposed the gap. Narrowed to a sanctioned-consumer
> allow-list **plus** an explicit never-reference list naming every sim assembly, so growing the
> allow-list to quiet a red test still fails: **stricter than before, not looser.** **Full dotnet gate:
> PASSED, 0 failures** (match-analytics 24 → 54, match-client-core 22 → 45, new match-client-web 34).
> **What PM-1 does NOT claim:** it is a statement about the client, not about the match it shows — the
> engine's goal rate still runs ~4.7× football's and its home/away asymmetry ~50× football's home
> advantage (§5.Z.11/§5.Z.15), both unchanged and neither blocking. Three PM-1 surfaces are
> deliberately thin and recorded rather than dropped: team selection is `MatchSetup` in code (a
> new-game screen is roadmap C4), `SetPlayerTactic` returns **501** rather than assembling a per-agent
> tactic from ten defaults the manager never chose, and the post-match report is the live statistics
> panel continuing after full time rather than a dedicated screen. **Next: Phase C — #44 discipline,
> then the season and new-game screens; the objective is PM-2.** Prior entry below.)
> **Last Updated (prior):** July 27, 2026, later same day (**Documentation sync pass — no code, no spec, no gate
> run.** Reconciled the root docs against two code landings that shipped earlier the same day (both on
> `path-to-playable-roadmap.md` Track C/S and already recorded there and in `spec-error-log.md`, but never
> folded into this file or `README.md`): **Match Analytics #37 T0** (roadmap item B2) gave that spec a
> `src/match-analytics/` assembly for the first time — value types (`MatchStatline`/`AdvancedStatline`/
> `StatPoint`/`MatchAnalyticsResult`, all copy-not-wrap and gated at construction) plus the pure, stateless
> `XgLocationModel`; it surfaced and resolved **ERR-037-001** (§4.1's reference list omitted the
> Ball-Physics `[CROSS]` reference Appendix A's `GOAL_WIDTH_M` tag requires — Appendix A won, so the
> asmdef references `TacticalDirector.BallPhysics` directly rather than re-declaring a third copy of
> 7.32 m). This moves the "APPROVED specs with no assembly" count from 23 to **22** and the assembly
> count from 29 to **30** — both were stale in the PROJECT IDENTITY section and the assembly map table
> below (which was missing a `match-analytics` row entirely). Also landed: **Track C B1**, a richer
> `LiveMatchFrame`/`MatchFrameView` observation frame (per-agent booking/sent-off/substitute state,
> per-team substitutions used, derived match period, last restart) for the interactive Unity client —
> no `SNAPSHOT_SCHEMA_VERSION` change (the new engine fields are either read-only copies of existing
> serialized state or within-tick fields reset every `RunInputPhase`, so no new cross-tick surface was
> added). Neither landing touched this file, `README.md`, or `docs/tracking/file-manifest.md` at the
> time, which is what this pass corrects; `file-manifest.md`'s "Current Specification Folders" table was
> found separately stale (stuck at 26 rows / "All 26 spec folders now exist", predating the #27–#54 wave)
> and is fixed in the same pass. Prior entry below.)
> **Last Updated (prior):** July 27, 2026, later same day (**ALL TEN APPROVED — the specification phase is
> CLOSED. `SPEC_INDEX.md`: 53 APPROVED / 0 IN REVIEW / 0 NOT STARTED.** Lead-developer R-01..R-05 sign-off
> granted on #53, #35, #46, #36, #54, #47, #48, #50, #51 and #39, with the **23 back-props filed and
> RESOLVED atomically with the flips** (`spec-error-log.md` v1.47) per each spec's own pipeline step 6.
> **Docs only: no code, no `src/` change, no gate run, and no format version bumped today.**
> **Landing the back-props together is what exposed the wave's most consequential defect, and filing them
> one spec at a time never could have:** **#30's pinned day-advance tick order was not implementable as
> written.** `ERR-030-007` had been filed **twice** — for #42's academy step and #32's scouting step, at
> two separate approvals — leaving **two step 7s, two step 8s and an orphaned `AdvanceDay` line** in a
> sequence **six approved specs cite by number**. Neither approval could have seen it alone. Reconciled
> under **ERR-030-022** in a new §3.3.1 (#32 → 9, #35 media expiry → 10, #54 tenure → 11, `AdvanceDay` →
> 12), which also had to resolve a **conflict between two of this wave's own back-props**: ERR-030-020
> (#53) requires its step to precede its same-day consumers and says to renumber below it, while
> ERR-030-022 requires the cited slots not to move — jointly unsatisfiable by inserting a new step 1.
> **Resolved by numbering the facility step 0**; a step numbered zero is unusual, but a renumber that
> silently invalidates six approved specs' citations is worse, and patching all six would edit approved
> text for a numbering preference rather than a design need. **`ERR-030-009` is a duplicate too** (#45's
> `JobSecurity` band; #44's availability filter) — both duplications preserved verbatim as frozen records
> and documented as errata. **Three entries change approved contracts rather than pointers:**
> **ERR-048-001** corrects a **contradiction between two MUSTs inside APPROVED #48** (FR-MP-025 forbids
> `#51 → #48`; FR-MP-027 required #51's catalogue to be keyed on #48's `CueId` — jointly impossible, and
> an assembly cycle waiting to happen); **ERR-045-002** re-points `FR-BD-012` from #30 to #54, closing a
> MUST that delegated the sacking decision to a spec containing no such rule; **ERR-033-003** replaces a
> per-producer morale field with a producer-agnostic one, **filed jointly by #35 and #46**. Three entries
> are ◑ spec-text-first with a named future bump, and **#54's `SEASON_STATE_FORMAT_VERSION` bump is
> decided to combine with #45's queued one** so saves face one refusal boundary rather than two. **Also
> fixed in passing:** #30's `section-2.md` and `section-3.md` each carried **two bare `**Last Updated:**`
> labels** with different content. **The consequence to carry forward:** with the spec phase closed,
> **23 of 53 APPROVED specs have no `src/` assembly** — *"the spec is APPROVED"* now says nothing about
> whether code exists, and that is true of 43% of the registry. Prior entry below.)
> **Last Updated (prior):** July 27, 2026 (**TEN DESIGN SUPPLEMENTS PROMOTED TO FULL SECTION FILES — the
> pre-promotion backlog is empty. Docs only: no code, no `src/` change, no gate run.** Every converged
> `docs/tracking/*-design.md` supplement that lacked a spec folder now has an 11-file set at
> `Status: IN REVIEW`: **#53** Club Infrastructure (`FR-IN`), **#35** Media & Press (`FR-ME`), **#46**
> News/Inbox & Man-Management (`FR-NW`), **#36** National Teams (`FR-NT`), **#54** Manager Career &
> Reputation (`FR-MC`), **#47** New-Game Setup & DB Editor (`FR-ED`), **#48** Match Presentation Depth
> (`FR-MP`), **#50** Save Migration & Versioning (`FR-MG`), **#51** Audio & Sound Design (`FR-AU`),
> **#39** Steam Packaging & Release (`FR-PK`). `SPEC_INDEX.md` gains ten registry rows: **43 APPROVED /
> 10 IN REVIEW / 0 NOT STARTED**. **Each carries a recorded section-file PASS-1 adversarial review + fix
> pass and an AR-2 sweep to CONVERGENCE (§9.4.1), and each stops at `IN REVIEW` deliberately** — G1 is
> closed, G2 (back-props) lands atomically at approval, and **G3, lead-developer R-01..R-05 sign-off, is
> a human authority and is not self-grantable**, exactly as every supplement's own §12 pipeline states.
> **The finding that generalises is an id-collision class, not a per-spec defect:** three supplements
> (#35, #46, #53) proposed `ERR-` ids that had **already been filed** — #30's T2 landing filed rows the
> same day those supplements were written, and nothing cross-checks a *proposed* id against
> `spec-error-log.md` — so a supplement's id is a suggestion to re-verify at promotion, not a
> reservation; reassigned to ERR-030-022/023, ERR-030-024 and ERR-029-003, each recorded as an M finding.
> The other seven verified their ids free against the log **and** every spec folder, and say so.
> **A second cross-wave pattern, recorded because ten repetitions is a process signal rather than ten
> slips:** in all ten, the `[GT]` budget ceilings declared in §6.3 were missing from the Appendix A
> catalogue — the #45 PASS-1 M-2 defect, reproduced independently each time, an artifact of §6 being
> authored before the appendices with nothing walking back. **Findings worth carrying forward:** #51's
> KD-1 resolves a genuine contradiction in **APPROVED** text (#48 forbids `#51 → #48` while FR-MP-027
> requires #51's catalogue to be keyed on #48's `CueId` — jointly impossible, and it would have surfaced
> as an assembly cycle after both were approved; ERR-048-001 corrects it, changes no code, and is
> therefore the back-prop most likely to be deferred at the price of the next implementer building the
> forbidden reference in good faith); #39's KD-2 inverts the release gate because **this repo's CI is
> skip-open** — `unity-tests` is gated on a secret and reports success when it is absent, so a green
> pipeline is compatible with nothing having been built or tested; and #50's KD-2 records that **rosters
> are regenerated rather than saved**, so a format-only migrator would migrate 25 versions perfectly and
> still hand the player a different squad. **Three specs file no back-props at all** (#48, #39, and the
> #37/#44/#46 class), stated as evidence of correct layering rather than left as an empty table.
> **Two numbers outside the roadmap's original #27–#51 range are promoted here for the first time:**
> #53, because four APPROVED specs consume a facility model they all attribute to #40 whose scope
> excludes it; and #54, because #45's `FR-BD-012` MUST names #30 as deciding a sacking and #30 contains
> no such rule. **Deliberately NOT done:** no sign-off claimed, no back-prop filed, no `src/` touched,
> no dotnet gate run (nothing compiled changed), and `management-layer-spec-roadmap.md`'s wave blocks
> left intact — they are the reasoning that produced the order, and rewriting them in the past tense
> would destroy the record of why each spec sits where it does. See `SPEC_INDEX.md` NOTES and the
> roadmap v0.7 header note.)
> **Last Updated (prior):** July 27, 2026 (**SEASON-BOUNDARY ROLL LANDED — #30 T3 / path-to-playable A5.
> Phase A is complete and PM-2-sim — a playable season, the objective — is REACHED.** A career no longer
> ends after one season: `SeasonLoop.RollToNextSeason()` finalizes the table, evaluates the board,
> derives the next seed, regenerates the schedule and calendar, and resets — and two careers from one
> seed now agree on **both** seasons' final tables, with a save taken at the boundary restoring to the
> same continuation. **The transform is pure in the prior `SeasonState`** — no clock read, no draw —
> which is what makes FR-SN-029's restartability claim non-trivial rather than incidental: deriving the
> next calendar from the world clock instead of from the old calendar would have made the roll depend on
> *when the client happened to call it*. `SeasonRollOutcome` is the producer record a career screen needs
> ("you finished 14th, the board wanted 10th, your job security fell"); job security is gained flat when
> the objective is met and lost **per league position short** when it is not, because a flat penalty would
> make missing by one place identical to finishing bottom. The (a') #43 promotion/relegation and (b') #40
> finance insertion points, and (d) #28's age advance, are **declared positions, not interfaces**
> (FR-SN-034 / FR-LW-031). New `[FIXED] SEASON_ROLL_SEED_DOMAIN` + three `[GT]` rows; no
> `SEASON_STATE_FORMAT_VERSION` change (the calendar was already serialized). **Full dotnet gate: PASSED,
> 0 failures (whole tree green; season-save 240 → 261, SDK 8.0.129 via apt).**
> **An adversarial review over the landing then found 1H+3M+2L, all fixed.** **H:** `AdvanceDays` bounded
> the world clock only while a season was IN PROGRESS. Once complete it was unbounded — so a client
> walking the close season past the day the next season opens reached a career with **no way forward**:
> the season cannot be played (it is complete) and cannot be rolled (the derived calendar now opens in
> the past), the world clock only moves forward, and the stuck state **saves and reloads cleanly**.
> Reproduced: complete on day 42, `AdvanceDays(57)`, both routes refuse, save/load round-trips the
> wreck. Fixed by generalising the existing KD-4 guard — post-season the bound is the day the next
> season opens, derived through the same `ShiftCalendarToNextSeason` the roll uses, so there is one
> derivation and two readers. **M-1:** the step (b) job-security arithmetic re-derived the pass/fail
> rule as `finalPosition <= targetPosition` instead of calling `BoardObjective.IsMetBy` — a second copy
> of board policy, sitting on the composition root rather than on `BoardState` (whose own doc already
> anticipated "the season-boundary pass/fail evaluation"). When #45 extends the objective model, the
> reported verdict and `IsOnTrack` would have moved while the job-security consequence silently stayed
> on the old rule. Moved to `BoardState.EvaluateAtSeasonEnd`, so one predicate drives verdict, running
> read and penalty. **M-2:** `SecondSeason_DiffersFromTheFirst` asserted a **disjunction** (table
> differs OR schedule differs) whose table half is always true — season 2 quick-sims against a
> different seed — so the schedule half, the thing the test is named for, was unreachable. Proven by
> perturbation: making the roll reuse the OLD seed for `FixtureScheduler` (every season replaying the
> identical fixture list) left the whole suite green. Now asserted separately, and the perturbation
> fails it. **M-3:** a season saved AFTER the roll had zero coverage — the shipped restartability test
> saves BEFORE it — while the roll installs a schedule and calendar the codec has never been shown,
> and "a roll installs a state Encode writes but Decode refuses" is a defect this exact path produced
> once already at T1. **L:** `EnginePlayedFixtures` / `MatchOutcomes` silently span the boundary, and
> the former's doc still claimed the per-season semantics T3 took away. Three new locks, each proven
> non-vacuous by perturbing its fix; season-save 258 → 261, full gate re-run green. **The second L —
> `ShiftCalendarToNextSeason` sitting on the composition root — was then fixed too, as
> `SeasonCalendar.ShiftedToNextSeason`:** pure calendar arithmetic now lives on the type that owns
> calendars, which also drops two array copies and a re-validation of an ordering that adding one
> constant to a strictly-ascending sequence provably preserves. What stays on the loop is
> `NextSeasonCalendar()` — the choice of the `[GT]` close season, bound in one place and read by both
> `AdvanceDays` and `RollToNextSeason`. Two new gates with it (a single-round calendar still moves
> forward; a `breakDays` of zero, and a shift that would carry the final round past `uint.MaxValue`,
> both refused). Season-save 261 → 263, full gate re-run green.**
> **The landing's finding is the sixth consecutive C5 hit, and the sharpest illustration yet of the
> project's own "tests that verify the composition runs, not that it works" trap — ERR-030-015.** §3.5's
> `RollToNextSeason` pseudocode regenerates `Fixtures`, resets `Table`, and advances `SeasonNumber`/`Seed`
> — but **never rebuilds `Calendar`**, whose cursor sits at `RoundCount` precisely *because* the season
> just ended. A roll implemented from the spec verbatim therefore produces a season that is
> **permanently unplayable**: `AdvanceToNextFixtureDay` throws F5 and `AdvanceAndPlayNextRound` throws, on
> every call, for the rest of the career — so the transform could not deliver FR-SN-029's multi-season
> continuity at all. **And no assertion over the rolled state's *fields* would have caught it**: the
> schedule, the table, the seed and the season number are all exactly right. It took an acceptance test
> that plays a **second** season to completion. Measured: **9 of the suite's 18 predicates fail** against
> the spec-as-written. Fixed as step **(c′)**, which shifts the OLD calendar's day mapping forward by one
> season length plus a `[GT] SeasonBreakDays` close season — chosen over rebuilding a linear calendar
> because it keeps the transform pure AND preserves a non-uniform schedule (a mid-season gap survives the
> roll instead of being silently flattened). `section-3.md` → v1.0, which also consolidates the **two
> stale `Version` header fields** that file carried. **What A5 does NOT claim:** PM-2-sim is a statement
> about the loop, not about the quality of what it simulates. **A4a remains gated — and not on compute.**
> Its Step 0 pilot and full corpus are ~33 min and ~1.4 h, both affordable; the blocker is that the
> engine's goal rate still runs **~4.7× football's** (§5.Z.15), so a corpus fitted today would calibrate
> the quick-sim to reproduce that faithfully across a whole 380-fixture league. Step 0 will not catch it
> on its own — it asks *"do the strength extremes separate?"*, and it passed at 25–0. The honest next
> lever is the quality of the goalkeeper's save, not further shot tuning. See
> `docs/tracking/path-to-playable-roadmap.md` v0.8 + src/CLAUDE.md v2.42.)
> **Last Updated (prior):** July 26, 2026, later same day (**FOUL & DISCIPLINE BALANCE PASS LANDED — §5.Z.9,
> closing the §5.Z.7 item 1 finding that Phase H recorded as the most visible remaining unrealism in a
> played match.** A match no longer empties itself of players: measured over one match-equivalent of
> composed play, **480 → 21.0 fouls, 147 → 3.0 yellows and 75 → 1.0 red cards per 90 minutes** against a
> football reference of ~22 / ~3.5 / ~0.25, with no team dropping below eleven where the pre-fix engine
> reduced teams to five to seven inside nine minutes. **The headline is that the measurement refuted the
> finding's own diagnosis.** §5.Z.7 framed this as a `[GT]` threshold question; the peak qualifying-force
> distribution turns out to be bounded and narrow (p99 = 1175 N, **max 2362 N** — a collision impulse over
> `ContactDurationS` cannot exceed it), so replaying the production gate across a threshold ladder gives
> 480 fouls at 1200 N, 90 at 2000 N and **0 at 3000 N**. The threshold is a cliff, not a dial, and the only
> values in between sit on the last thirty samples of a 130 000-tick run — a setting that would read as
> calibrated while being pure noise. No cooldown rescues it either. **The real gap was the referee:** the
> model called *every* hard cross-team from-behind contact a foul, and the engine produces **seventeen of
> those per second**, so what was missing is judgement — a probability. Fixed with a force-scaled call
> probability `p(F) = min(1, FoulCallProbability × F / FoulImpactForceThresholdN)` (a harder challenge is
> likelier to be given; a hard contact is never automatically a foul), whose **single draw** also selects
> the card severity from the rescaled remainder `v = u / p` — ordinary inverse-transform partitioning, so
> there is **no new RNG stream and no `SNAPSHOT_SCHEMA_VERSION` change**. A wave-on arms no cooldown
> (arming it would silently swallow the genuine foul two ticks later), and the consumer now keeps the
> **strongest** contact of a tick rather than the first, since force now decides the call and first-wins
> would systematically under-call the hardest fouls. New `[GT] FoulCallProbability` = 0.015;
> `YellowCardProbability` 0.35 → 0.16, `RedCardProbability` 0.05 → 0.011, `FoulCooldownTicks` 60 → 180.
> **Calibration required a live run, not the offline sweep, and that generalises:** the sweep pointed at
> 0.025, where a real match measured 37.5 fouls per 90 min — giving 20× fewer fouls means 20× fewer
> restarts, so play runs on and the qualifying-contact count *rose* from 36 000 to 129 000 over a
> comparable corpus. An offline gate replay finds the right shape cheaply; it never gives the value.
> **Acceptance is the test the tree did not have:** `match-engine-discipline-plausible` (#19 ScenarioRunner,
> Tier B, 6 seeds × 9 min, ~52 s) asserts foul/yellow/red rates in plausibility bands, that **no team is
> reduced below nine players** (per seed, never aggregated — one abandoned match must not average away),
> and that cards stay a minority of fouls; **9 of its 10 predicates fail on the pre-fix engine**, each by
> more than an order of magnitude. Plus 8 unit locks in `MatchEngineFoulCardTests` (probability shape,
> wave-on leaving no trace, strongest-wins capture driven through the real consumer), the env-gated
> `FoulRateDiagnosticTests` instrument (replays the gate offline across a ladder so one composed run
> yields the whole curve), and `MatchEngine.TestOnly_SetCollisionObserver` — the seam that made the force
> distribution observable at all, since the collision system takes exactly one consumer and it is private.
> **Full dotnet gate: PASSED, 0 failures (whole tree green; match-engine 333 → 342, SDK 8.0.129 via apt).**
> **One finding recorded and deliberately NOT fixed** (new OPEN ISSUES entry): the **contact rate itself**
> — 17 hard cross-team from-behind contacts per second, on 20% of all ticks, is not football. The
> refereeing model now sits plausibly on top of it, but the stream underneath is wrong, and it is the next
> thing to look at for match realism (most likely #12 agent spacing or #3's 60° `BehindDotThreshold`
> cone). See `docs/tracking/foul-discipline-balance-design.md` + `match-engine-design.md` §5.Z.9 +
> src/CLAUDE.md v2.41.)
> **Last Updated (prior):** July 26, 2026, latest same day (**ROOT-DOC RECONCILIATION — `CLAUDE.md` + `README.md`
> re-based on the actual repo state; no code, no spec, no tracking-doc change.** The two root documents had
> drifted badly behind the tree they describe: this file's body still said *"All 20 Stage-0 specifications
> are APPROVED, plus the first Stage-1 forward spec #21"* and *"Ball Physics (#1) and Agent Movement (#2)
> have initial implementations"* — against a real state of **43 APPROVED specs (0 IN REVIEW / 0 NOT
> STARTED)** and **29 production assemblies**; its REPO STRUCTURE tree listed 8 spec folders and 2 `src/`
> assemblies out of 43 and 29, and named none of `tools/`, `docs/design/`, or the Unity project shell.
> `README.md` was pinned at **July 14, 2026 / 26 specs**, twelve days and seventeen approved specs stale,
> and its status text still described `SNAPSHOT_SCHEMA_VERSION` 15 (actual: **18**). **Corrections that
> change what an agent would do:** (1) a new **`src/` assembly map** — the folder-name→spec mapping is
> *not* inferable, since #27 lives in `player-database`, #28 in `player-progression`, #30 in `season-save`,
> #38 in `ui-framework`, #23/#24/#25 inside `positioning-ai`, and #26 inside `tactical-instructions`, while
> `match-engine` / `match-viewer` / `match-client-*` / `project-constants` are not numbered specs at all;
> (2) the **13 APPROVED-but-unimplemented specs** (#29, #31–#34, #37, #40–#45, #49) are now stated in both
> files, because "approved" had become a misleading proxy for "a consumer exists" — the single most
> load-bearing fact about the current state, and the premise of `path-to-playable-roadmap.md`;
> (3) the **design-supplement governance class** (42 `docs/tracking/*-design.md`) is documented for the
> first time — it appears in no root doc, yet it is where `match-engine-design.md` and every pre-promotion
> spec note live; (4) the two **roadmaps** (`management-layer-spec-roadmap.md` — which specs to author;
> `path-to-playable-roadmap.md` — which code to land) added to TRACKING DOCUMENTS; (5) three new rows in
> *Things That Have Gone Wrong Before*, each earned: never-compiled surfaces, **tests that verify the
> composition runs rather than that it works** (the ERR-030-014 class — the capstone asserted tick count,
> cadence, finiteness, bounds and digest advance, every one of which holds for a match in which nothing
> happens), and home-team-only worked examples; (6) the *"When Writing Code (Future — after all 20 specs
> approved)"* heading de-tensed — that future arrived on May 19, 2026. Also fixed: a **second bare
> `**Last Updated:**` label** at the June-10 entry deep in this header chain, which made the block
> self-contradictory about its own currency (now `(prior)`). **Deliberately NOT touched:** the historical
> header entries and OPEN ISSUES bodies (frozen records per this project's own "historical rows preserved
> verbatim" convention — they are re-dated by nothing here), and `src/CLAUDE.md`, whose **Assembly Layer
> Taxonomy is itself now stale** (it lists `UI | (Stage 1+ — not yet specified)` while #38 is APPROVED and
> `src/ui-framework/` exists, and omits `match-engine`, `season-save`, `player-database`,
> `player-progression`, `match-viewer`, and `match-client-core`) — recorded here as a follow-up rather than
> edited, since it is the authoritative coding guide and its taxonomy is a Spec #20 §3.5.2 reproduction
> that should be corrected against that spec, not against a folder listing. **The dotnet gate was not
> re-run in the authoring environment** (no SDK), so the gate claims restated here were quoted from the
> last landing's record — but CI subsequently ran the full Linux shim gate green on this branch
> (10 checks pass, Unity tests skipped for want of a license), which re-verifies them independently.)
> **Last Updated (prior):** July 26, 2026, later same day (**MATCH-ENGINE POSSESSION BOOTSTRAP LANDED — §5.Z Phase H,
> roadmap item A4b. ERR-030-014 is CLOSED: a production match now plays.** The engine that had never in its
> history put the ball in motion now kicks it, contests it, works it into both penalty areas and scores.
> Measured over six seeds × 9 minutes: peak ball speed **16.2–17.2 m/s** (was 0.00), peak height **2.45–2.91 m**
> (was 0.11 = the resting centre height), possession held **10.5–20.9%** of ticks (was 0%) and changing hands
> **262–298 times** (was 0). **The fix is five seams, not the one the finding anticipated — and four of the
> five were found by RUNNING the composed engine, each invisible until the previous fix let play run
> further.** (1) **KD-H1 restart taker award:** `ApplyRestart` now takes an `awardedTeam` and every call site
> declares one, so no restart can silently grant the ball to nobody — kickoff to the home side, the second
> half to the other (Law 8, `[DERIVED]` from the first so they cannot drift together), a goal to the
> conceding team, throw-in/corner/goal-kick to `RestartResolver`'s already-computed award, offside to the
> defenders, a foul to the victim's team; the taker is that team's nearest **non-sent-off** agent.
> (2) **KD-H2:** possession assignment, NOT imparted velocity — `ApplyKick` stays the sole producer of ball
> motion. (3) **KD-H3 loose-ball pickup:** `RunFirstTouch` correctly refuses a ball that is not moving (a
> still ball is not an incoming receive, and #4's control-quality model is a function of incoming velocity),
> so a separate `RunLooseBallPickup` claims a ball that has come to REST — gated on the exact complement of
> first touch's speed gate, so the two can never both fire, and #4's contract is untouched.
> (4) **KD-H5 / ERR-008-014:** the Decision Tree had **no action at all that fetches a stationary loose
> ball** — PRESS targets an opponent, MOVE targets the formation slot, INTERCEPT bailed at its
> minimum-ball-speed gate — so play died the first time a pass ran out of momentum beyond INTERCEPT's ~10 m
> reach, with all 22 agents circling their slots around it; fixed by emitting a loose-ball **collect** as the
> SOLE off-ball option for one **host-designated** collector per team (host-designated because only the host
> knows who is sent off — a perception-derived "nearest teammate" rule deadlocked on a frozen red-carded
> agent eleven teammates were deferring to; and sole-option per ERR-008-013's AR-4, since the collect scores
> ~0.35 against MOVE's ~0.21, a gap **inside** the ±0.15 composure-noise band, so as a competitor the
> collector visibly dithered and never arrived). (5) **KD-H4 / ERR-008-015:** `NotifyActionComplete` had
> **zero production callers**, so every agent that passed or shot was frozen in EXECUTING for the rest of
> the match — no decisions, no movement commands, and no way to release the ball it was still holding; the
> composition root now closes the lifecycle (it is the only layer that sees both the trees and their
> executors), and `OnPossessionChanged` no longer interrupts a holder whose executor is still in flight.
> **Acceptance is the test the tree did not have:** `match-engine-play-develops` (#19 ScenarioRunner, Tier B,
> 6 seeds × 32 400 ticks, ~90 s) asserts the ball is kicked and airborne, possession is held and contested,
> **play is still alive at the final tick**, and across the spread the ball reaches both penalty areas and
> goals are scored — **every predicate fails on the pre-Phase-H engine**, and `play-still-alive-at-final-tick`
> caught two of the four stalls, both of which let play run for eight or nine minutes before dying. Plus a
> two-run byte-identical digest chain over 6 000 ticks of LIVE play (the Phase F capstone matched two
> 600-tick chains, but 600 ticks of the old engine were 600 ticks of nothing). New
> `MatchEnginePossessionBootstrapTests` (11) + `OptionGeneratorTests` (+3). **21 existing tests were updated
> — most of them encoded the old "a restart clears possession" contract, which is precisely the contract
> that made the deadlock possible.** **Full dotnet gate: PASSED, 0 failures (whole tree green; match-engine
> 322 → 333, SDK 8.0.129 via apt).** No `SNAPSHOT_SCHEMA_VERSION` change (nothing new is serialized).
> **Two findings recorded and deliberately NOT fixed** (design note §5.Z.7): the foul heuristic issues
> **~7 red cards per 9 minutes** — consistently, across seeds, i.e. every player would be dismissed inside a
> full match — which is a `[GT]` threshold question (`FOUL_MIN_FORCE_N` / `FoulCooldownTicks` /
> `RedCardProbability`) needing a foul-rate target rather than a guess folded into a correctness fix, and is
> now **the most visible remaining unrealism in a played match**; and the process-static EventBus makes
> **interleaved** engines diverge at tick 1 (sequential runs are byte-identical — verified both ways), a
> latent property of #17 §3.2.1 that was invisible only because no production event had ever been published.
> **This unblocks PM-1 and roadmap A4a** — re-run #30's KD-8 Step 0 pilot (~33 min); note it may still
> refuse, since Phase H makes matches *play*, not necessarily *discriminate by squad strength*, which is
> exactly what Step 0 exists to ask. See `docs/tracking/match-engine-design.md` §5.Z + src/CLAUDE.md v2.40.)
> **Last Updated (prior):** July 26, 2026 (**Season & Competition Loop #30 T2 LANDED — the day-advance loop + the
> round-resolution model; path-to-playable roadmap item A4 — and the same landing surfaced the most
> consequential finding on the playability track: ERR-030-014, a production match cannot develop play at
> all.** Four new files in the existing `TacticalDirector.SeasonSave` assembly (`RoundResolutionMode`,
> `RoundResolutionModel`, `SeasonLoop`) plus `src/match-engine/SquadRating.cs` — the narrow PUBLIC rating
> seam over the internal `LineupSelector` that league-bootstrap AR-4 M-1 recorded as A4's named prerequisite
> (re-implementing selection inside `season-save` was explicitly refused as the parallel-surface trap).
> `SeasonLoop` is the KD-7 **sole writer** of `SeasonState`: `AdvanceToNextFixtureDay` / `AdvanceDays` walk
> the world one calendar day at a time in the **KD-2 fixed order** (only step 9, `WorldStore.AdvanceDay`, is
> live — steps 1–8 remain documented null seams per FR-SN-034, so a no-fixture day is byte-identical to a
> bare `AdvanceDay`, FR-SN-026), and `AdvanceAndPlayNextRound(ISquadProvider)` resolves the **whole** round
> (FR-SN-012), routing the managed club's fixture through a real `MatchEngine` and every other through the
> model, applying each result in FR-SN-013's pinned table → event → mark order, and advancing the cursor.
> `RoundResolutionModel` is **keyed, not cursor-positioned** (§3.4.1): `FixtureKey(seasonSeed, seasonNumber,
> roundIndex, home, away)` folds in `DOMAIN_TAG_SEASON_LOOP` — **the tag's first draw site, discharging
> ERR-030-001** — and feeds an exp-shaped lambda pair through a *named* **inverse-CDF** Poisson quantile (one
> uniform per side, `MAX_GOALS_PER_SIDE` cap), so permuting a round's resolution order yields the
> byte-identical table (T-SN-CAL-003c). That is roadmap C1's whole point realised: a 20-club / 38-round /
> **380-fixture season resolves in milliseconds** against the ≥ 16 hours the real engine would need. New
> `SeasonLoopTests` + `RoundResolutionModelTests` + the **`season-multi-fixture` capstone** on the #19
> ScenarioRunner (season-save 179 → 240 tests (237 passed + 3 env-gated drivers skipped), incl. the capstone scenario; the capstone runs one real ~3.6-minute engine
> match — the deliberate Simulation-layer home for that cost). **Full dotnet gate: PASSED, 0 failures (whole
> tree green; SDK 8.0.129 via apt).** **Three ERRs filed. Two are the familiar shape** — a §4 architecture
> sketch another section of the same spec forbids: **ERR-030-012** (§4.5 specifies a REGISTERED
> cursor-positioned season stream, but §3.4.1 requires keyed draws for order-independence; realized as the
> keyed derivation above, and `SubsystemOrdinals.SeasonLoop = 84` deliberately **not** allocated in code,
> because an ordinal with no stream behind it is the zero-consumer phantom FR-LW-031 forbids) and
> **ERR-030-013** (§4.6's "records the `MatchResult` in `SeasonState`" is unimplementable — §2.2/Appendix B
> give `SeasonState` no outcome collection, and adding one would bump `SEASON_STATE_FORMAT_VERSION` for a
> payload FR-SN-017 forbids a consumer for; the producer record is loop-scoped, the durable record is the
> serialized table). **The third changes the plan. ERR-030-014, found by actually RUNNING A4a's KD-8 Step 0
> pilot:** all 20 full 90-minute engine matches finished **0–0** at a measured squad-rating differential of
> **±6** on a `[1,20]` scale. Characterisation over 60 000 ticks — in both a distinct-squad and a plain
> neutral configuration — found the ball's velocity **identically zero for the entire match**, never
> airborne, and **never possessed by any agent**. The cause is a closed loop, half of it already stated in
> the engine's own comment: `InitializeKickoffState` places the ball at rest (*"a kick would set it in
> motion; none at Stage 0"*), `RunFirstTouch` gate 3 refuses a touch unless the ball is ALREADY moving,
> production possession is granted only by that path (`TestOnly_SetPossessor` is documented "Not called by
> production"), and only a possessing agent can kick. No motion ⇒ no reception ⇒ no possession ⇒ no kick ⇒
> no motion. **A production match has always been a 90-minute 0–0 deadlock** — and it was invisible because
> the 321 match-engine tests each drive their own inputs per subsystem, while the one composed test (the
> 600-tick kickoff capstone) asserts tick count, AI-stride cadence, finiteness, on-pitch bounds and
> digest-chain advance: every one of which holds for a match in which nothing happens. **It verified that
> the composition runs, never that it plays** — precisely the gap the path-to-playable roadmap opened with.
> Consequences: **A4a is blocked upstream of itself** (not by its compute — measured at ~98 s/match, so the
> full corpus is ~1.4 h across four processes, well inside C1a's 9 h budget); the three round-resolution
> `[GT]` parameters ship **provisional and explicitly not fitted**, football-plausible rather than
> engine-matched; **PM-1 ("watch a match") is blocked by the same gap**, PM-2-sim is not. Owner is
> `match-engine-design.md` (new **§5.Z Phase H**), not #30, and roadmap item **A4b** (a kickoff/restart
> possession grant) now precedes A4a on the critical path — deliberately not attempted inside A4, since it is
> a behaviour change to the most safety-critical assembly, activates a large amount of never-composed code
> (C5 at its strongest), and moves every engine digest. Committed alongside: the A4a harness, the fitter
> `tools/round-resolution-fit.py`, the env-gated Step 0 and characterisation drivers (neither asserting
> current behaviour — pinning a defect would make it a contract), and the evidence record
> `docs/tracking/round-resolution-corpus.md`. Self-review over the landing found **2 M + 3 L**: no gate
> enforced "the world is ON the round's fixture day", so a client could skip the day-advance for a whole
> career and get a plausible-looking table stamped with wrong world days; and the `FullEngine` routing branch
> was reachable only by running two real matches, so a typo there would have shipped as "FullEngine quietly
> behaves like ManagedThroughEngine" (extracted to the pure `ShouldPlayThroughEngine`, all six combinations
> locked). See src/CLAUDE.md v2.39 + the path-to-playable and new match-engine-playability OPEN ISSUES entries.)
> **Last Updated (prior):** July 25, 2026, latest same day (**League bootstrap LANDED — path-to-playable
> roadmap item A3, the #47-minimal substitute (C3): a playable league now EXISTS, generated, with no
> authored data and no database editor.** `LeagueBootstrap.Generate(worldSeed, clubCount)` turns one
> seed into an N-club league — five new files in the existing `TacticalDirector.SeasonSave` assembly
> (KD-1, no new assembly; it gains a `TacticalDirector.PlayerDatabase` asmdef reference):
> `LeagueBootstrapConstants`, `ClubNameCatalogue`, `Club`, `League`, `LeagueBootstrap`. Three
> domain-separated derivations from the one world seed (KD-4 — roster / strength / season), one
> registered roster stream per club under `SubsystemOrdinals.PlayerDatabase` with `entityId = clubId`
> (so a club's BASE roster — identity + pre-strength attributes — is a function of `(worldSeed, clubId)`
> alone, independent of league size; the SHIPPED attributes are not, because the strength ramp is over
> league size, and both halves are test-locked), a seeded Fisher–Yates **strength rank** ramped into a per-club `[1,20]`
> attribute delta so the table is not 20 statistically identical teams (KD-5; `WeakFootRating`
> deliberately excluded — a `[1,5]` scale would saturate), `League` **is** the `ISquadProvider` (no
> adapter for the engine or for #30 T2), and `League.CreateSeason(managedClubId)` hands #30 a startable
> `SeasonState` through the existing `SeasonState.CreateNew`. **No new #16 domain tag or subsystem
> ordinal is allocated** — the strength permutation uses a LOCAL SplitMix64 exactly as
> `FixtureScheduler` does, so `DOMAIN_TAG_SEASON_LOOP` / ordinal 84 stay pinned to #30 T2's first draw
> site per ERR-030-001. **The load-bearing finding, caught at design time (KD-6):** `RosterGenerator`
> draws positions uniformly over four, so a 25-player squad lacks the four defenders a back four needs
> ~3% of the time per line — and `LineupSelector` refuses such a squad fail-loud, so a 20-club league
> would have failed to start **by seed**, the worst failure shape available. Fixed at the root: a `[GT]`
> position template (3 GK / 8 DF / 8 MF / 6 FW, sized against the worst case across all three shipped
> formation families) fed to a new **additive** `RosterGenerator.Generate(rng, streamIndex, clubId,
> PlayerPosition[])` overload — the position draw still runs and is discarded, so the per-player RNG
> budget, the stream layout, and the drawn-position path stay **byte-identical** (`RosterGenerator.cs`
> v1.4). Governed by the new converged supplement `docs/tracking/league-bootstrap-design.md` (v1.1 —
> AR-1 1H+2M+2L → AR-2 1M+1L → AR-3 CONVERGENCE → **AR-4 over the shipped code, 0H+2M+4L**). AR-4's two
> M findings are **forward gaps A4 would have walked into**: `LineupSelector` is `internal` to
> match-engine, so KD-7's quick-sim `Rating(club)` is unreachable from `SeasonLoop` (recorded as a named
> A4 prerequisite, with re-implementing selection inside season-save explicitly refused as the
> parallel-surface trap), and A4a's calibration harness had been placed in an assembly that cannot reach
> the `internal ApplyStrength` (corrected to `src/season-save/tests/`). AR-4 L: `MaxClubCount` 64 → 32
> plus an explicit `MaxRngStreams` coherence gate (one stream per club — at 64 it exactly filled the
> registry, and any raise would have failed *mid-generation* with a generic "registry full");
> `POSITION_COUNT` hoisted to `PlayerDatabaseConstants` so two assemblies stop carrying private copies of
> the enum's member count (the PM AR-7 M-1 parallel-surface class), locked against `Enum.GetValues`; and
> negative world-day `[GT]` values refused at read rather than wrapping to ~4.29e9. New
> `tests/LeagueBootstrapTests.cs` (27 — determinism, seed divergence, league-size independence,
> contiguous ids + globally unique `PlayerId`s, catalogue coverage/uniqueness, strength-ramp
> endpoints/symmetry/permutation, position coherence for every shipped formation **plus** an end-to-end
> `ConfigureSquads` acceptance run through the real engine, every F1–F6 gate, and the `CreateSeason`
> handoff round-tripping through `SeasonStateCodec`) + `RosterGeneratorTests` +3 + `PlayerAttributesTests`
> +1. **Full dotnet gate: PASSED, 0 failures (whole tree green; season-save 141 → 177, player-database
> 42 → 46; SDK 8.0.129 via apt).** **A4a is designed but NOT executed** — its ~9 h corpus run is its own
> roadmap item, and A4 (#30 T2) is the next item on the critical path. **AR-5 (a hostile whole-file
> re-read, not a diff pass) then found 1H+4M+3L, all fixed:** **H-1** — because rosters are REGENERATED
> from the world seed rather than saved, the generation path is persistence-equivalent, and every
> determinism test on it was self-referential ("generate twice, compare"), so a draw-order change, a
> catalogue reorder, or a one-line `[GT]` tweak would silently rewrite every club in every existing save
> with the whole suite green; closed by new **KD-10** + a pinned golden vector
> (`LeagueBootstrapGoldenVectorTests` — the #16 HKDF/SipHash precedent), proven non-vacuous by
> perturbing `AttributeBaseMean` 10 → 11 and watching it fire. **M-1** — the world seed was WRITE-ONLY
> (`SeasonState.Seed` holds the derived season seed and `Mix` has no inverse; `WorldStore._worldSeed`
> had no accessor), so a saved career could not rebuild its `ISquadProvider` at all; closed by a
> read-only `WorldStore.WorldSeed` + the KD-9 resume recipe + a round-trip lock. **M-2** — the
> league-size-independence claim above was true only of the base roster (narrowed everywhere; the #43
> promotion/relegation consequence named). **M-3** — `SquadPositionCounts` was a public mutable `int[]`
> whose mutation still passes the sum check while voiding the KD-6 fieldable-squad guarantee (now
> `ReadOnlyCollection` over a private backing array). **M-4** — the strength spread's *sufficiency* was
> unverified while being the feature's stated purpose (discharged as KD-8 **Step 0**: a ~20-match pilot
> at the ramp extremes runs BEFORE the 9 h corpus, so A4a cannot fit three parameters to noise). Plus 3
> L. **AR-6 over those fixes then found 1M** — the new golden vector pinned only a 4-club league, leaving everything that varies with league size (the permutation length, the ramp denominator, name indexing, and the `delta == 0` branch that never occurs at N=4) unguarded; a second digest + delta row is now pinned at `DefaultClubCount` behind a guard that fails if the default is retuned. **Gate re-run: PASSED, 0 failures (season-save 141 → 177, living-world 119).** See src/CLAUDE.md
> v2.38 + the path-to-playable OPEN ISSUES entry.)
> **Last Updated (prior):** July 25, 2026 (**Season & Competition Loop #30 T1 LANDED — the season save/restore path;
> path-to-playable roadmap item A2.** A season is now part of the save file, not just the world and an optional
> match. New `src/season-save/SeasonStateCodec.cs` is a pure byte codec for the season-state sub-blob over the
> #30 Appendix B layout (version gate first; seed / seasonNumber / **managedClubId**; the club set; the
> CONCRETE schedule — serialized, never regenerated, per KD-5; the calendar cursor per KD-4; the league table
> in ClubId order; the board), carrying the `MatchSaveCodec`/`WorldStateSerializer` fail-loud posture:
> overflow-safe length bounds, a trailing-byte guard, and **decode-through-the-validating-constructors**, so a
> corrupt blob throws rather than materializing a structurally impossible season. The outer frame gains a
> **third** opaque sub-blob between the world and match blocks and **`SEASON_SAVE_FORMAT_VERSION` bumps 1 → 2**
> (FR-SN-020 — the world and match blobs are byte-untouched; only the frame around them moved, and a v1 file is
> rejected fail-loud, no Stage-0 migration). `SeasonSaveManager.Save(world, season, matchOrNull, path)` /
> `Load(...) → { World, Season, Match }` per FR-SN-021, with all three blobs captured before the file is opened;
> unlike the match, the season is **never optional**. **Implementation surfaced ERR-030-011** (filed + patched
> same commit): §3.6's `EncodeSeason` pseudocode omitted `ManagedClubId` — which Appendix B row 3a lists and
> `SeasonState` requires, so a codec written to §3.6 verbatim emits a blob no season can be reconstructed from —
> and Appendix B row 11 left job security as `f32/u8`, neither matching the integer per-mille `BoardState`
> carries. **Appendix B is the byte-layout authority**; §3.6 gains the missing line, row 11 is pinned
> `jobSecurityPerMille i32` (ratifying what #30 T0 adopted and flagged as a back-prop candidate). No
> `SEASON_STATE_FORMAT_VERSION` change — T1 is that version's first use, so the correction lands before any
> file exists. **Two code self-AR findings fixed:** the per-array length bound moved from a `count * width`
> byte product (overflowable for a large blob and a crafted count) to a provably overflow-free element-wise
> `remaining / width`; and `SeasonState`'s constructor now requires a calendar mapping at least one round,
> closing an encode/decode asymmetry where an EMPTY schedule with a `default(SeasonCalendar)` was constructible
> but not decodable. New `SeasonStateCodecTests` (round-trip field identity for fresh / mid-season / completed
> seasons, per-column and scalar locks, encode determinism + a non-vacuity control, a pinned-offset layout lock,
> and every FR-SN-023 fail-loud gate) + `SeasonSaveManagerTests` v1.3. **Full dotnet gate: PASSED, 0 failures
> (whole tree green; season-save 112 → 135 tests; SDK 8.0.129 via apt).** **Adversarial review over the landing (3 passes, converged): 1M+4L / 1M+2L / 0H+0M+3L.** Pass 1: the T1 self-AR's zero-round calendar guard was MOVED, not resolved — `BeginNextSeason` carries the identical vacuous coverage check unguarded (its `maxRound >= RoundCount` is false at `maxRound = -1`) and the ctor still took an empty fixture array, so a roll could install a state `Encode` writes and `Decode` refuses (reproduced by an executed probe); fixed at the root (the ctor now refuses an EMPTY schedule) and mirrored onto the roll. Pass 2: **FR-SN-011 (MUST) / F4 were unimplemented** — `SatisfiesCursorInvariant` had ZERO production callers while its own doc claimed `SeasonLoop.Restore` invoked it, so a save whose world clock had passed the pending round loaded silently and would surface at T2 as a stuck or skipped round; `SeasonSaveManager.Load` now enforces it (the one cross-blob coherence rule, checkable only at this root, which is the layering argument for the root existing), with the completed-season vacuous case locked so the gate cannot become a spurious refusal. Pass 3: three `Modified` headers stale against their history rows (FR-CS-056). L also: `<exception>` docs on the `Decode` seam, an outer-frame pinned-order lock, a mis-naming test rename, offset-helper widths named + a coherence guard, and two docs naming a T2 type / an already-closed back-prop. **Gate re-run: PASSED, 0 failures (season-save 135 → 141).** Remaining #30: T2 the day-advance
> loop + round resolution, T3 the boundary roll. See src/CLAUDE.md v2.37 + the path-to-playable OPEN ISSUES
> entry.)
> **Last Updated (prior):** July 22, 2026 (**Goalkeeper Mechanics #11 + Heading Mechanics #10 WIRED into the match
> engine, and the GK/Heading attribute projections LANDED — Phase 1 (opt-in).** The `ToGoalkeeper` /
> `ToHeading` projections that `player-attribute-projection-design.md` deferred under KD-P8 (phantom
> consumers — `MatchEngine` built neither struct) are now non-phantom: `MatchEngine.cs` v1.44 constructs
> both sealed orchestrators + four stateless ball/RNG adapters at boot and registers `heading.mechanics` +
> `goalkeeper.mechanics` RNG streams (the card-severity precedent). A new public `EnableGkHeading()` opts
> in (default OFF): while off the engine is **byte-identical to pre-wiring** (no `SNAPSHOT_SCHEMA_VERSION`
> change — the 279-test existing snapshot/determinism/restore suite is unchanged); while on, a 10 Hz
> tactical + 60 Hz physics drive runs both orchestrators and conservative Stage-0 world-state triggers
> (the `MatchFlowCollisionConsumer` heuristic-foul precedent) commit a `SaveIntent` seeded from
> `PlayerAttributeProjection.ToGoalkeeper` (loose on-target ball near the defended goal) and a
> `HeaderIntent` seeded from `ToHeading` (nearest agent to a loose airborne ball) — the projections' live
> consumer. A flag-on engine is deterministic FORWARD but not yet snapshot-safe, so the durable-capture
> seams fail loud (`NotSupportedException`); the per-tick digest is untouched. New
> `PlayerAttributeProjection.cs` v1.2 (`ToGoalkeeper` int→float widen of the ten GK fields; `ToHeading`
> raw copy of Heading/Strength/Balance) + `MatchEngineConstants.cs` v1.25 (+6 `[GT]` trigger constants).
> Governed by the new converged supplement `docs/tracking/gk-heading-engine-integration-design.md` (AR-1
> 1M+2L → AR-2 CONVERGENCE → AR-3 opt-in scope revision; code self-AR folded CS0118 fully-qualification,
> `_gkAgentIds` refresh across `ConfigureSquads`/subs, and the guard placement on the durable-capture
> seams — not the per-tick `SerializeWorldState`). New `MatchEngineGkHeadingTests` (8) +
> `PlayerAttributeProjectionTests` +2. **Full dotnet gate: PASSED, 0 failures (whole tree green; 290
> match-engine tests; SDK 8.0.129 via apt).** **Phase 2 (deferred):** serialize the RNG cursors + both
> orchestrators' in-flight state (`SNAPSHOT_SCHEMA_VERSION` 17 → 18), flip the default to on, take the
> digest rebaseline; plus a DT-driven producer, the `CollisionConsumer` duel fan-out, and the closed-loop
> scenario. See the new GK/Heading engine-integration OPEN ISSUES entry + src/CLAUDE.md v2.32.)
> **Last Updated (prior):** July 22, 2026 (**Unified season save LANDED — snapshot-deserialize N2 / match-engine
> Phase G-Phase 3 season save-file root; Phase 3 is now COMPLETE.** A whole season is now one **file**:
> `SeasonSaveManager.Save(world, matchOrNull, path)` bundles the living-world `WorldStore.Snapshot()`
> composite together with an **optional** in-progress `MatchEngine` (a `matchPresent` flag byte — a season
> between fixtures has a world but no match), and `SeasonSaveManager.Load(path, ISquadProvider squads =
> null)` reconstructs both (`SeasonSaveContents { WorldStore World; MatchEngine Match /* null if none */
> }`). The file is a **thin frame over two self-contained, independently version-gated byte blobs**
> (`SeasonSaveCodec`) — the codec never parses either sub-blob's internals, so all four inner versions
> stay untouched and the season file only adds a **fourth** format version
> (`SeasonSaveConstants.SEASON_SAVE_FORMAT_VERSION` [FIXED] = 1). **This is the N2 blocker the notes
> deferred**, and its resolution is the whole point: `FR-LW-003` bars the match engine from referencing
> the living-world assembly (and vice-versa), so **neither save could host the other**; the new
> `TacticalDirector.SeasonSave` assembly (`src/season-save/`) sits **above both** and is the only assembly
> that may see both — the same layer class as `match-viewer` over `match-engine` — so it composes them
> without either referencing the other (verified: `match-engine.asmdef` and `living-world.asmdef`
> reference neither each other). **Decisions made at this season root:** the file carries the world blob
> (always) then the match blob (only when `matchPresent`); the match blob is reused through a new public
> `MatchSaveManager.Encode(engine) → byte[]` / `Restore(blob, squads) → MatchEngine` "match save as a
> value" API (the internal capture seams stay internal — `Save`/`Load` refactored to delegate,
> behaviour-identical, all 279 match-engine tests still green); the `ISquadProvider` is a Load-time
> parameter, never persisted (threaded into the match restore only when a match blob is present); the
> match restore's fingerprint + MXCSR float-mode gates run on season `Load` unchanged. Governed by the new
> converged supplement `docs/tracking/unified-season-save-design.md` (v0.5, AR-1 2M+3L → AR-2
> CONVERGENCE; a follow-up code AR over the shipped diff found 0H+0M+2L, both fixed — L-1 restored
> `MatchSaveManager.Save`'s engine-before-path guard order (kept behaviour-identical); L-2 added the R4
> no-match-with-provider test lock). New `SeasonSaveManagerTests` (19 — disk round-trip determinism for a
> no-match season (world field-identical + `world.text` resumes) and a season with a neutral /
> distinct-squad match via `ISquadProvider` (match digest chain byte-identical AND world field-identical,
> both through one file), `SeasonSaveCodec` round-trip + all fail-loud gates, manager
> missing/corrupt/no-provider/null-world/overwrite). No `SNAPSHOT_SCHEMA_VERSION` /
> `WORLD_STORE_FORMAT_VERSION` / `MATCH_SAVE_FORMAT_VERSION` change (a file frame around unchanged blobs);
> `MatchSaveManager.cs` v1.2. **Full dotnet gate: PASSED, 0 failures (whole tree green; 19 new season-save
> tests; SDK installed via apt).** See src/CLAUDE.md v2.31 + the snapshot-deserialize OPEN ISSUES entry.
> **With this, snapshot-deserialize Phase 3 is complete** (the native MXCSR live-mode query was certified
> July 22; N2 lands here) — nothing further open on that track.)
> **Last Updated (prior):** July 21, 2026 (**On-disk match save format LANDED — snapshot-deserialize Phase 3
> `SaveManager` fold (N1).** A running match is now a **file**: `MatchSaveManager.Save(engine, path)`
> captures a durable snapshot and writes it atomically (the §4.6.1.1 temp→fsync→rename contract), and
> `MatchSaveManager.Load(path, ISquadProvider squads = null)` reads it back into a ready-to-tick
> `MatchEngine` via the Phase-1/2 `RestoreFromSnapshot` reader. The on-disk blob (`MatchSaveCodec`, a
> pure version-gated codec) packs the three things restore needs: the KD-7 boot `matchSeed` (the payload
> does not carry it — the file is the boot-header root the deserialize note deferred to N1), the
> `SnapshotHeader` incl. its `EnvironmentFingerprint` + digest chain, and the `SnapshotPayload`;
> fail-loud on a `MATCH_SAVE_FORMAT_VERSION` mismatch, an out-of-bounds length prefix, or trailing bytes
> (overflow-safe bound guard). `MatchEngine` gains a public `MatchSeed` property + the durable-capture
> seams promoted `TestOnly_` → production internal (`CaptureDurableHeader`/`CaptureDurablePayload`).
> **Decisions made at this N1 root:** the file carries the boot seed; the `EnvironmentFingerprint` is
> serialized so the KD-6 float-mode gate runs **end-to-end through disk** (a save under `CreateStage0Dev`
> validates; a tampered/foreign fingerprint is rejected — closing O3 for the on-disk path, so the
> on-disk header no longer writes `Fingerprint = null`); the `ISquadProvider` is a Load-time parameter,
> never persisted (the file references rosters by ClubId, the caller owns the roster store). Governed by
> the new converged supplement `docs/tracking/match-save-file-design.md` (v0.3, AR-1 3M+2L → AR-2
> CONVERGENCE); code self-AR folded one overflow-safe-bound hardening. New `MatchSaveManagerTests` (16 —
> disk round-trip determinism for neutral / booking-before-save / distinct-squad-via-provider, codec
> round-trip + all fail-loud gates, manager missing-file/corrupt-file/no-provider/overwrite paths). No
> `SNAPSHOT_SCHEMA_VERSION` change (a file frame around the unchanged reader/writer). `MatchEngine.cs`
> v1.43 + `MatchEngineConstants.cs` (MATCH_SAVE_FORMAT_VERSION [FIXED] = 1). **Full dotnet gate: PASSED,
> 0 failures (279 match-engine tests; whole tree green — SDK installed via apt).** See src/CLAUDE.md
> v2.30 + the snapshot-deserialize OPEN ISSUES entry. **Still open in Phase 3:** the native MXCSR
> live-mode query (host-blocked) + the N2 unified season save (FR-LW-003 + season save-file root).)
> **Last Updated (prior):** July 20, 2026 (**Snapshot-deserialize Phase 2 LANDED — distinct-squad restore
> re-projection (#27 T3), the last T3 data-side item, CLOSED.** A match booted through `ConfigureSquads`
> with real club squads can now be saved and restored byte-deterministically, not just refused. New
> `ISquadProvider` seam (`src/match-engine/ISquadProvider.cs`) threaded into
> `MatchEngine.RestoreFromSnapshot(…, ISquadProvider squads = null)`; the new
> `MatchEngine.ReprojectDistinctSquads` replaces the Phase-1 fail-loud — the neutral path returns
> immediately, and each team with a non-sentinel `_rosterClubId` (v16 identity) has its roster resolved
> (ClubId-checked + size/record validated, both teams before any apply — the `ConfigureSquads`
> validate-both-before-write discipline), its base lineup re-projected via `LineupSelector` +
> `PlayerAttributeProjection` (`ReprojectBaseLineup` — attribute arrays + the bench GK flags
> `_benchIsGoalkeeper`, a boot-constant NOT serialized; the on-pitch `_isGoalkeeper` stays the restored
> serialized value), and the substitutions the serialized `_activeBenchSlot` records replayed
> (`ReprojectSubstitutions`, the attribute half of `SubstitutePlayer`). Fail-loud on absent provider /
> unresolvable ClubId / mismatched returned ClubId (R4). Determinism rests on the provider returning the
> SAME roster the saved match loaded (`LineupSelector` + `PlayerAttributeProjection` are pure). Acceptance:
> `MatchEngineSnapshotRestoreTests` v1.1 proves G3 round-trip determinism for a distinct (varied-attribute)
> squad, a mid-match substitution, a post-restore substitution, and a post-restore keeper-for-keeper
> substitution, plus fail-loud on no provider / unknown ClubId / mismatched roster. No
> `SNAPSHOT_SCHEMA_VERSION` change. `MatchEngine.cs` v1.42. **Full dotnet gate: PASSED, 0 failures (263
> match-engine tests; whole tree green — SDK installed via apt).** **Discovered during Phase 2 (out of
> scope; a Phase-1 snapshot-completeness follow-up, NOT a Phase-2 defect):** a post-restore substitution
> that FLIPS a pitch slot's goalkeeper status — subbing a keeper onto an OUTFIELD slot, which realistic play
> never does — diverges via a Positioning-AI (#12) formation-slot interaction with the GK-flag flip (two
> fresh engines with the same substitution are deterministic; the base distinct-squad round-trip + realistic
> keeper-for-keeper and outfielder substitutions all round-trip). See
> `docs/tracking/snapshot-deserialize-design.md` v0.8 + src/CLAUDE.md v2.29 + the OPEN ISSUES entry. **Still
> open:** Phase 3 (native MXCSR query + on-disk `SaveManager` fold, host/upstream-gated) + the Phase-1
> Positioning GK-flag-flip edge above.)
> **Last Updated (prior):** July 20, 2026 (**Snapshot-deserialize Phase 1 COMPLETE — save/load/replay reader LANDED,
> G3 round-trip determinism GREEN.** The keystone the next tier of MVP work sits behind: the match engine
> can now be reconstructed from a snapshot, not just run forward once. New `MatchEngine.DeserializeWorldState`
> (the symmetric line-for-line mirror of `SerializeWorldState`, reconstructing every subsystem's cross-tick
> state through its `RestoreState` seam) + the static `MatchEngine.RestoreFromSnapshot(in SnapshotHeader,
> SnapshotPayload, ulong matchSeed)` factory (fingerprint gate → boot + `EventBus.ResetForNewMatch` →
> deserialize → KD-3 distinct-squad fail-loud → digest-chain `CommitLoadedDigest` + clock restore). New
> `RestoreState` counterparts on Pressing/Defensive/Attacking/Perception/Positioning + `MovementCommand.
> ReconstructFromSnapshot` (RotationController / executors / DecisionTree / OscillationGuard / MatchClock /
> RNG restore seams pre-existed). Acceptance: `MatchEngineSnapshotRestoreTests` proves **save@N → restore →
> tick to N+K == an uninterrupted run** byte-for-byte (KD-5) across neutral kickoff, a mid-match tactics
> change, and the KD-8 booking-cursor regression, plus version-gate / trailing-byte / distinct-squad
> fail-loud. Two findings folded in during landing: the excluded `_possessingAgentId`/`_prevPossessingAgentId`
> are reconstructed from the restored `MatchContext.PossessingAgentId` (the `_prev == _poss ==
> MatchContext.PossessingAgentId` snapshot-time invariant), and the trailing-byte guard is now event-ledger-
> aware (`RunSnapshotPhase` appends the digest-load-bearing ledger after the world state; the reader validates
> the world-state read ended at the ledger domain-tag boundary rather than restoring the ledger, which is
> replayed forward). No `SNAPSHOT_SCHEMA_VERSION` change (a pure reader over the v17 writer). `MatchEngine.cs`
> v1.41. **Full dotnet gate: PASSED, 0 failures (257 match-engine tests; whole tree green).** This unblocks
> save/load of an in-progress match, replay/rewind, and — via Phase 2 — distinct-squad restore (#27 T3). See
> `docs/tracking/snapshot-deserialize-design.md` v0.7 + src/CLAUDE.md v2.27 + the OPEN ISSUES entry. **Still
> open:** Phase 2 (#27 T3 distinct-squad re-projection via the `ISquadProvider` seam — Phase 1 refuses a
> non-sentinel roster reference) and Phase 3 (native MXCSR query + on-disk `SaveManager` fold, host/upstream-
> gated).)
> **Last Updated (prior):** July 18, 2026 (**Squad/Player Data Layer #27 T3 LANDED** — the snapshot roster-reference
> field for distinct-squad save/restore fidelity, per the new converged
> `docs/tracking/squad-roster-reference-design.md` (v0.2, AR-1..AR-2 CONVERGED). New per-team
> `MatchEngine._rosterClubId[TEAM_COUNT]` (the loaded `Squad.ClubId`, or `[FIXED] NO_ROSTER_CLUB_ID = -1`
> when no squad is configured), set by `ConfigureSquads` **after** both squads validate-and-apply (so a
> refused call leaves the sentinel), serialized at **`SNAPSHOT_SCHEMA_VERSION` 15 → 16** (`MatchEngine.cs`
> v1.39 / `MatchEngineConstants.cs` v1.23). Boot-constant identity — the same lifecycle class as the
> already-serialized `_teamIds`/`_isGoalkeeper`, which is what makes it non-phantom despite no restore
> consumer: a save now records **which squad each team loaded** — the identity half of restore fidelity;
> the per-slot attribute VALUES stay excluded (re-projectable from the roster, keyed by the serialized
> `_activeBenchSlot` for substitution bench-swaps). **KD-T3-2 design decision:** a configured squad —
> even all-`CreateDefault` — is now digest-distinguishable from an unconfigured one **by design** (the
> reference is identity, not attributes: club 7 all-neutral ≠ frozen neutral, because club 7 is a
> persistent roster to reload on restore). This **supersedes** the T1 KD-P7 all-default byte-identity
> lock (a T1-only property — T1 added no serialized field); behavioural neutrality still holds and is
> re-locked as "a config-default run diverges from unconfigured **at tick 1**, before any behavioural
> divergence could exist, so the roster field is the sole difference." A non-digest "header" alternative
> was rejected (KD-T3-4 — the match engine has no save/restore surface distinct from the digest payload,
> so a header field would be a zero-consumer phantom that also would not do the job; the payload is the
> project's established boot-constant-identity surface). **KD-T3-3:** the restore re-projection itself is
> future work — the match engine has **no snapshot-deserialize path** (verified: no `Read`/`Deserialize`
> in `MatchEngine.cs`), so building the consumer now would be a phantom; T3 lands the reference and
> unblocks that work on the data side. New `TestOnly_RosterClubId` seam; exclusion-proof +
> `ConfigureSquads`/substitution restore-scope docs updated. Tests: `MatchEngineSnapshotSchemaTests` v1.13
> (pin 15 → 16 + `RosterReference_FeedsSnapshotDigest` single-field probe), `MatchEngineSquadTests`
> v1.2 (T1 neutrality lock replaced with the KD-T3-2 identity-capture / same-config-determinism /
> distinct-ClubId / sentinel-seam locks). **Post-landing code AR (fresh-eyes over the shipped diff):
> 0H+0M+1L — L: replacing the T1 byte-identity lock dropped the direct match-level proof that a
> config-default match is *behaviourally* identical to unconfigured (the new tests prove the roster
> field feeds the digest, not that the divergence is non-behavioural); fixed by adding
> `ConfiguredDefaultSquad_IsBehaviourNeutral_ObservableStateMatchesUnconfigured` (ball + every agent
> position match tick-for-tick — the observable level a digest can no longer isolate). Re-verified
> clean: field appended last (no offset move), no snapshot decoder reads the payload by offset (only
> the opaque digest), CROSS-TICK-COVERAGE excluded-set claim survives.** **Full dotnet gate re-run:
> PASSED, 0 failures (237 match-engine tests).** See src/CLAUDE.md v2.26. **Remaining #27:** lineup selection proper (Plan-3 —
> the Stage-0 mapping is roster-order), the per-spec GK (#11)/Heading (#10) projections (deferred until
> those specs are engine-wired, KD-P8), the distinct-squad restore re-projection (gated on a
> snapshot-deserialize path existing), and on-disk persistence / transfers / aging (Stage 1+).)
> **Last Updated (prior):** July 17, 2026, latest same day (**Repeat adversarial review of the T1/T2 landing
> (AR-4 of its cycle, run at the user's request) — 1 M + 3 L, all doc-only, all fixed; then AR-5
> sweep 0H+0M+1L (doc) — CONVERGENCE, cycle CLOSED** per the L-only-round convention. The pass
> re-walked the full touched surface against source: writer-completeness sweep of every projected
> array (`_canonicalAttrs`/`_attrs`/`_dtAttrs`/`_perceptionAttrs`/bench — exactly boot seed +
> `ConfigureSquads` + `SubstitutePlayer`, no stray writer), the FirstTouchAbility site inventory
> (exactly 3), Perception-side mutation of `_perceptionAttrs` (none — the IsHalfTurned preserve is
> defensive-only), and the downstream #13 WeakReceiver/threat-score consumers (T1 activates the
> previously-dormant WeakReceiver press trigger for genuinely below-average receivers under a
> distinct squad — designed behaviour, default path unchanged at 10 ≥ threshold). **M-1 (doc,
> cross-assembly contract):** `AttackingAgentSnapshot.Pace/Dribbling` XML still documented the
> `(raw−1)/19` normalization while the T1 writer populates them live ÷`ATTRIBUTE_MAX` (KD-P3) —
> pre-T1 the mismatch was against an unconsumed 0.5 placeholder (flagged in the projection design
> §2); post-T1 it misdescribed real data a consumer could mis-derive raw values from. Docs aligned
> to the live ÷20 convention (`AttackingAgentSnapshot.cs` v1.1); switching the MATH stays a
> recorded deferred design question (it moves the neutral off 0.5). **L:** three `MatchEngine.cs`
> comments the T1 code edits outdated ("Stage-0 neutral placeholder" claims at the
> CoverShadowCurve fill / FillAttackingSnapshot summary / BuildFirstTouchContext summary —
> v1.38, doc-only); the three `STAGE0_NEUTRAL_*` constants' stale "TODO: replace when ERR-007
> attribute split lands" markers retired (`MatchEngineConstants.cs` v1.22 — the split landed;
> production-unconsumed since T1, retained as the KD-P7 neutral-equivalence references); AR-5's
> L — `ConfigureSquads` doc now states players beyond the consumed 18 are ignored. The
> decision-tree `(raw−1)/19` hits are #8's own spec-pinned INTERNAL normalization of the raw
> values T1 feeds it — KD-P2-consistent, not a finding. Full dotnet gate re-run: PASSED, 0
> failures. See src/CLAUDE.md v2.25.)
> **Last Updated (prior):** July 17, 2026, latest same day (**Squad/Player Data Layer T1/T2 LANDED** — `MatchEngine`
> attribute seeding now sources from canonical player records per the converged
> `docs/tracking/player-attribute-projection-design.md` (v0.3, AR-1..AR-3 CONVERGED; PR #225). New
> `src/match-engine/PlayerAttributeProjection.cs` (pure per-target projections: #2/#8/#7 raw copies;
> #5/#6 with the KD-P1 derived KickPower — `(Passing+Technique)×.5` / `RoundToInt((Finishing+LongShots)×.5)`,
> the ERR-007 proxies now computed from real attributes; the three `FirstTouchAbility` sites #13/#14/#4
> per KD-P9; the sole normalized target — Attacking pace/dribbling — `÷ATTRIBUTE_MAX` per KD-P3 so
> neutral = 0.5). `MatchEngine.cs` v1.37: canonical `_canonicalAttrs`/`_benchCanonicalAttrs` records
> (default `CreateDefault()`, NOT serialized — same B3 exclusion class, proof updated), every seeding
> site converted (zero production `STAGE0_NEUTRAL_*` consumers remain), new public `ConfigureSquads`
> (pre-kickoff, Stage-0 roster-order lineup — player 0 → the GK slot; lineup selection proper stays
> deferred; fail-loud [1,20]/[1,5] bounds gate at the consuming seam, both squads validated before ANY
> write), and `SubstitutePlayer` now copies the canonical bench record + re-projects `_dtAttrs`/
> `_perceptionAttrs` (the v2.20 substitution-attrs hazard's on-pitch half). **Default path proven
> byte-identical (KD-P7, digest-locked — no schema change, no rebaseline); a distinct squad diverges
> by design and deterministically. Distinct-squad restore stays a T3 deliverable (KD-P10, documented —
> no restore path exists today).** Implementation-time corrections recorded in the design docs'
> version histories: the §1 inventory under-reported `FirstTouchContext.Technique` (same site, now
> projected, neutral-preserving; projection doc v0.4), and #27's reserved-list mis-classified
> `FirstTouchAbility` (KD-P9 correction, squad-player doc v0.5 + `PlayerAttributes.cs` v1.1).
> Self-adversarial review of the landing: **AR-1 1 M** (per-team validate-then-apply let an invalid
> AWAY squad refuse only after the HOME squad had landed — validation hoisted for both squads before
> any write, + regression lock) **+ AR-2/AR-3 sweeps clean** (residual-seed grep empty; KD-P8 honoured —
> no GK/Heading phantom projections). New suites: `PlayerAttributeProjectionTests` (scale/derivation/
> neutral-equivalence locks) + `MatchEngineSquadTests` (digest neutrality/divergence/determinism +
> substitution + fail-loud gates). Full dotnet gate: PASSED, 0 failures (232 match-engine tests).
> See src/CLAUDE.md v2.24. **Remaining #27 work:** T3 snapshot roster reference (distinct-squad
> restore fidelity), lineup selection, Stage-1+ persistence/transfers/aging.)
> **Last Updated (prior):** July 17, 2026, later same day (**Fourth repeat adversarial review (AR-4 of the cycle) —
> 0 H + 0 M + 1 L (doc-only), fixed. CONVERGENCE — the review cycle over the July 14–15 landings is
> CLOSED** per the project convention (an L-only round ends the cycle; match-viewer AR-4 precedent).
> Instead of another piecemeal sweep, the pass walked the COMPLETE sent-off participation matrix —
> AI dispatch skip / all four Mechanics-AI `IsActive` snapshot fills / physics forced-stop / offside
> line / first-touch receiver scan (AR-2's fix) / foul-card-restart interpretation (AR-3's fix) /
> substitution refusal / half+full-time one-shots — plus the in-flight-state interactions the
> earlier rounds never composed: a card's `ApplyRestart` clears possession BEFORE the Resolve-phase
> executor advance, and the executor adapters' `IsBallPossessedBy` reads the live
> `_possessingAgentId`, so a just-sent-off agent's mid-windup pass/shot self-cancels at CONTACT via
> the FM-08/FM-05 possession recheck (no participation leak through in-flight executors). **L
> (doc):** the `_lastHolderAgentId` writer comment claimed the `GoalAwardedEvent` credit "names the
> agent whose kick scored" — deflections never update the tracker (the approximation already
> documented at the `RestartResolver` seam by AR-1), so a deflection-chain goal credits the last
> SETTLED holder, possibly not the kicker and possibly sent off since; comment aligned
> (`MatchEngine.cs` v1.36, doc-only — scoring-TEAM classification is pure geometry and unaffected).
> Full dotnet gate re-run: PASSED, 0 failures. See src/CLAUDE.md v2.23.)
> **Last Updated (prior):** July 17, 2026 (**Third repeat adversarial review (AR-3 of the cycle) — 1 M found,
> fixed.** The pass re-verified all six AR-1/AR-2 fixes and swept the card/restart/possession
> interaction paths the earlier rounds had cleared piecemeal. **M-1:** foul candidates involving a
> sent-off participant were still applied — `ApplyFoulIfCaptured` checked contact type, force, and
> opposite teams but not `_isSentOff`, and sent-off agents deliberately remain collision bodies, so
> a frozen red-carded agent standing in the path of play repeatedly WON free kicks (`ApplyRestart`
> teleported the ball to their feet) and drew cards against opponents who ran into their back, for
> the rest of the match — the foul/card/restart interpretation was the remaining participation
> surface without the exclusion. Fixed (`MatchEngine.cs` v1.35 — candidate discarded at the
> application site: no event, no cooldown, no restart; physical collision response unchanged) + 2
> regression locks (`MatchEngineFoulCardTests` v1.1 — sent-off victim in the exact positive
> free-kick geometry, and sent-off offender). Verified clean: every card path clears possession via
> `ApplyRestart` (no sent-off-possessor deadlock vector); the Interception case maps the Stage-0
> unresolved interceptor to NO_POSSESSION. Full dotnet gate re-run: PASSED, 0 failures. See
> src/CLAUDE.md v2.22.)
> **Last Updated (prior):** July 16, 2026, later same day (**Repeat adversarial review (AR-2 of the cycle) — 1 M + 1 L
> found, both fixed; the pass otherwise re-verified the first round's fixes and swept the
> surfaces the first round had only skimmed** (LiveMatchFrame, AttrIdx/NameCatalogue, the four
> live-viewer/player-database test suites, RunPhysicsPhase freeze, RunFirstTouch gates). **M-1:**
> sent-off agents could still RECEIVE the ball — `RunFirstTouch`'s gate-4 receiver scan was the one
> participation surface without the `_isSentOff` exclusion (AI dispatch, all four Mechanics-AI
> `IsActive` snapshot fills, the physics forced-stop, and the offside line all have it), so a ball
> rolling past a frozen red-carded agent handed them possession they could never release (no AI
> dispatch ⇒ no kick), deadlocking play until the next half/full-time ball reset. Fixed
> (`MatchEngine.cs` v1.34) + regression lock (`MatchEngineFirstTouchTests` v1.1 — the exact
> CONTROLLED-receive geometry with the agent sent off stays loose). Physical presence
> (collision/perception/pressure) deliberately unchanged. **L (doc):** `AttrIdx`'s "Technical (8)"
> group comment lists 7 members (totals were correct). Full dotnet gate re-run: PASSED, 0 failures.
> See src/CLAUDE.md v2.21.)
> **Last Updated (prior):** July 16, 2026 (**Adversarial-review fix pass over the last three landings** —
> match-flow completion (July 14) / interactive match view (July 15) / squad-player data layer
> (July 15) were re-reviewed fresh-eyes at the user's request; findings 2 M + 4 L, all fixed same
> day. M-1: `MatchEngine.SubstitutePlayer` never reset the outgoing slot's yellow-card count —
> discipline was slot-keyed, so a substitute replacing a booked player was sent off on their own
> first yellow via the second-yellow promotion (`MatchEngine.cs` v1.33 resets it; no schema bump —
> v15 already serializes the count; +regression locks in `MatchEngineSubstitutionTests` v1.1).
> M-2: `SquadFileLoader` bounded every numeric key except `age` (silently accepted any int against
> its own "out-of-range int all throw" contract) — now [AgeMin, AgeMax] (+2 locks). L: post-full-time
> `SubstitutePlayer` refused (state mutated a frozen match while the queued SubstitutionEvent could
> never flush past the `_matchEnded` Resolve guard); `RestartResolver`'s "touched last" param doc
> aligned to the actual caller input (the last settled HOLDER — deflections never update the
> tracker, −1 ⇒ team 0); the live viewer's HUD clock reintroduced the `m:60` rounding bug the HTML
> replay's AR-1 had fixed (now rounds before the minute split; node-verified); `LiveMatchServer`
> connection threads that outlive `Stop()` now answer 503 instead of still driving /control;
> `RosterGenerator` modulo-bias doc note. Also flagged forward: the substitution attrs-swap ×
> player-database T1 interaction (see the updated squad/player OPEN ISSUES entry). See src/CLAUDE.md
> v2.20.)
> **Last Updated (prior):** July 15, 2026, later same day (**Squad/Player Data Layer T0 LANDED** — the match
> engine currently seeds all 22 agents with identical mid-range (10) attributes
> (`PlayerAttributes.CreateDefault()`, `STAGE0_NEUTRAL_ATTRIBUTE`); this is a Stage-1-forward pull
> (master plan §4.2 places a player database at Stage 2) providing the canonical data layer that
> gap needs, mirroring the #21/#22 design-supplement-first precedent. Design doc
> `docs/tracking/squad-player-data-design.md` (candidate spec #27, not yet reserved in
> `SPEC_INDEX.md` — registry rows land at promotion per the #23–26 precedent) went through 2
> self-adversarial-review rounds to convergence (AR-1: club-identity vs match-`teamId` conflation in
> the original `PlayerId`/RNG-stream keying draft — corrected via KD-3; trimmed the canonical
> attribute table to "consumed by an existing spec" ∪ "reserved, master-plan-only"; WeakFootRating
> scale isolation. AR-2: position-bias-table test strategy switched to direct constant assertions,
> not statistical sampling over generated squads). New `src/player-database/` assembly
> (`TacticalDirector.PlayerDatabase`, references only `DeterministicSim`): canonical
> `PlayerAttributes` (31 `[1,20]` fields reconciling all 7 existing per-spec attribute structs +
> `WeakFootRating` on its own `[1,5]` scale — closes the long-open `ERR-007` gap where the spec text
> was patched in 2026 but `AgentMovement.PlayerAttributes` never actually gained the fields;
> `PassAgentAttributes` still carries `[TEMPORARY-PROXY-ERR-007]` tags today), `PlayerRecord` /
> `Squad` (club-scoped roster container, `CLUB_SQUAD_SIZE`=25 per master plan §4.2 — deliberately
> not `MatchEngineConstants.SQUAD_SIZE`, which is the unrelated match-scoped 22-on-pitch-agent
> concept), `RosterGenerator` (deterministic — new `DOMAIN_TAG_PLAYER_DATABASE`=0x1F +
> `SubsystemOrdinals.PlayerDatabase`=81 back-propped into `deterministic-sim`, off-pitch band
> alongside Living World), `SquadFileLoader` (Stage-0 human-authoring text import, mirrors
> `TeamTacticFileLoader`'s grammar exactly). Code adversarial review (2 passes) caught three real
> defects before landing: `PlayerRecord.Position` had no RNG draw at all in the first pass
> (`FIELDS_PER_PLAYER` undercounted 35→36); `WeakFootRating`'s jitter reused the much-wider
> attribute spread against its own `[1,5]` range, clamping most draws to the boundary (now its own
> `WeakFootSpread`); `SquadFileLoader`'s identity default computed `PlayerId` from the raw
> section-local index instead of the club-scoped formula `RosterGenerator` uses, caught by a
> round-trip test that would have failed against the bug. Also flagged and documented (not yet
> hit): `PlayerDatabase.PlayerAttributes` shares its bare name with the pre-existing, unrelated
> `AgentMovement.PlayerAttributes` — no collision today since nothing references this new assembly
> yet, but the CS0104 class the project hit at `src/CLAUDE.md` v1.73 (`TacticTranslation`) will
> recur the moment a future T-phase wires both into `MatchEngine`. **Deliberately NOT built in this
> pass** (see the design doc §4/§5 T-phase plan): wiring into `MatchEngine` (replacing
> `CreateDefault()` seeding — intentionally NOT behaviour-neutral, unlike a typical T0, since the
> entire point is giving agents distinct attributes, so it needs its own reviewed change); per-spec
> projection updates that would close `ERR-007` for real; a snapshot roster-reference field; the
> on-disk save-format squad persistence / transfer market / aging (master plan §4.3/§4.4, explicitly
> out of scope). Full dotnet gate not runnable in this environment (mirror 404s on
> `dotnet-sdk-8.0`, consistent with prior entries) — verified by exhaustive manual review in place
> of `dotnet test`. See src/CLAUDE.md v2.19.)
> **Last Updated (prior):** July 15, 2026 (**Interactive match view LANDED** — upgrades the passive post-hoc
> HTML replay (`src/match-viewer/`) into a live-updating viewer watched *during* a real match: a
> background thread paces a real `MatchEngine` at wall-clock speed (`LiveMatchStreamer.cs`, new)
> and a minimal loopback-only HTTP server (`LiveMatchServer.cs`, new — hand-rolled over
> `TcpListener`, no package dependency) serves a browser page that polls `/frame` and redraws, plus
> a playback-only `/control` endpoint (pause/resume/speed — deliberately never a gameplay-mutation
> channel). `MatchEngine.cs` v1.32 gains 3 trivial read-only properties (`HomeScore`/`AwayScore`/
> `MatchEnded`), same section as the existing `BallView`/`AgentView` observation surface. Full
> in-Unity rendering remains blocked on Unity host access (existing OPEN ISSUE) — this is the
> "at minimum a live-updating viewer" floor. Per the user's process instructions: a design doc
> (`docs/tracking/interactive-match-view-design.md`) went through 2 self-adversarial-review rounds
> to convergence before implementation, then the code itself went through 2 adversarial-review
> passes, catching and fixing (among other things) an identical `Start()`/`Stop()` race condition
> in both new classes — the running-state flag flipped true inside the lifecycle lock before the
> background thread was actually assigned, so a `Stop()` racing into that narrow window could join
> a null thread while a fresh thread got spawned against an already-stopped listener. Full dotnet
> gate not runnable in this environment (no SDK reachable) — verified by exhaustive manual review
> in place of `dotnet test`. See src/CLAUDE.md v2.18 for the full file-by-file description.)
> **Last Updated (prior):** July 14, 2026 (**Match-flow completion LANDED** — throw-ins, corners, goal kicks,
> fouls/cards, offside, substitutions, half-time break, and full-time end (previously only kickoff +
> goal-restart existed; see `docs/tracking/match-flow-completion-design.md` for the full plan +
> adversarial-review history). Per the user's process instructions: a design doc was written first,
> adversarially reviewed to convergence (AR-1 through AR-6, each documented in the design note's own
> version history — including AR-4's rejection of a full ends-swap at half-time, since `team 0
> attacks +X` is hardcoded across goal detection/offside/Mechanics-AI and a real ends-swap is a
> Stage-1+ deferral, and AR-5's fix for `SubstitutePlayer` being callable between ticks when
> `EventBus.CurrentPhase` is not a valid producer phase — now a pending-event queue flushed at the
> top of the next Resolve phase), then implemented, then the CODE was itself adversarially reviewed
> to convergence (catching, among other things, an `OffsideEvaluator` bug where fewer than two active
> defenders left the accumulator at an `Infinity` sentinel instead of `NaN`, which made `IsOffside`
> return true for every finite attacker position — the exact opposite of the intended "too few
> defenders to be offside" rule). **New:** `src/match-engine/RestartResolver.cs` (pure
> position/awarded-team resolution for `RestartType.ThrowIn`/`Corner`/`GoalKick`, unified
> `awardedTeam = 1 − lastTouchTeam`), `OffsideEvaluator.cs` (pure second-nearest-to-goal-line
> geometry + reception-time offside check — a documented Stage-0 approximation, not the full
> freeze-at-the-pass Law), `SubstitutionReason.cs`; three new Tier A events
> (`OffsideCalledEvent` 0x18, `RestartAwardedEvent` 0x19, `MatchPhaseChangedEvent` 0x1A, all
> registered in `EventRegistry` v1.8). `MatchEngine.cs` v1.31: `CheckRestartAndApply` (renamed/
> extended from `CheckGoalAndRestart`) routes non-goal exits through `RestartResolver` +
> a shared `ApplyRestart` primitive; a per-tick foul-detection consumer (`MatchFlowCollisionConsumer`,
> replacing the former no-op `NullCollisionEventConsumer`) captures at most one FROM_BEHIND
> high-force cross-team collision per tick, drawn against a new `match-flow.card-severity` RNG
> stream for card severity (yellow/red bands), with second-yellow promotion and sent-off tracking
> (`_yellowCards`/`_isSentOff`) feeding a forced-stop in the Physics phase and an `IsActive = false`
> exclusion in all four Mechanics-AI snapshot fill sites (#12/#13/#14/#15); `EvaluateAndApplyOffside`
> hooked into `RunFirstTouch`'s Controlled case for genuine same-team pass receptions; a public
> `SubstitutePlayer` (bench-roster swap, cap-enforced at `MAX_SUBSTITUTIONS_PER_TEAM`, queued
> `SubstitutionEvent` publish); `CheckMatchFlowTransitions` (called every Input phase, not
> stride-gated) fires the half-time ball-reset-only transition once at `HALF_TIME_BOUNDARY_TICK` and
> the full-time gameplay-freeze once at `MATCH_TICKS_TOTAL` (both guarded by one-shot flags;
> `_matchEnded` freezes AI/Physics/Resolve while the tick/snapshot loop keeps advancing).
> **`SNAPSHOT_SCHEMA_VERSION` 14 → 15** (per-agent yellow-card count + sent-off flag, the global foul
> cooldown, per-agent active bench slot, per-team substitutions-used count, half-time/full-time
> fired flags — all cross-tick and now digest-load-bearing). New tests:
> `MatchEngineRestartTests`/`MatchEngineOffsideTests`/`MatchEngineFoulCardTests`/
> `MatchEngineSubstitutionTests`/`MatchEngineMatchFlowTests` (pure-function locks + MatchEngine
> integration + two-run determinism each); `MatchEngineSnapshotSchemaTests` v1.12 (pin 15 + two new
> preimage probes). Full dotnet gate not runnable in this environment (no SDK access) — verified by
> exhaustive manual code review (multiple adversarial-review rounds reading the entire touched
> surface, not just the diff) in place of `dotnet test`. See src/CLAUDE.md v2.17 and
> `docs/tracking/match-engine-design.md` v2.0.)
> **Last Updated (prior):** July 13, 2026 (**Unity engine version bumped: 2022.3.62f1 → Unity 6000.4.9f1,
> graphics API pinned DX11 — documentation-only pass, no recertification performed.**
> `ProjectSettings/ProjectVersion.txt` updated to `6000.4.9f1`. `docs/tracking/certification-platform.md`
> → v1.3: Unity-version and new Graphics-API rows updated to the target tuple; per that file's own
> Maintenance Rule this is a MAJOR version bump, so Status flips from `✅ PINNED` back to
> `⏳ RECERT REQUIRED` and every downstream unblocker it previously closed (`FR-DS-009-GATE` Stage 0
> activation, `FR-PO-052` perf-gate, §7.5 D1 test-runner pin, `EnvironmentFingerprint`) is blocked
> again until a real certification run executes against the new tuple — the June 7, 2026 run only
> certified the superseded 2022.3.62f1 tuple. `docs/tracking/cert-run-runbook.md` → v1.1 (Step 0
> pre-flight table updated; flags that `CertifiedPerfBaseline.Stage0CertPlatformPin`
> (`src/performance-optimization/CertifiedPerfBaseline.cs`) still hardcodes the old
> `win11-unity2022.3.62f1-...` pin string as a follow-up CODE change, deliberately out of scope for
> this docs-only pass). This root `CLAUDE.md`'s own "Unity 2022 LTS conventions" coding-convention
> line updated to Unity 6. **Deliberately NOT touched** (per this project's own "historical rows
> preserved verbatim" convention): dated version-history rows inside already-`APPROVED` spec section
> files that cite Unity 2022.3/2022 LTS as reference hardware or citation text (e.g.
> `positioning-ai/section-6.md`, `defensive-ai/section-6.md`, `attacking-ai/section-6.md`,
> `pressing-ai/section-6.md`, and citation blocks in `agent-movement`/`ball-physics`/`first-touch`/
> `collision-system`/`pass-mechanics` §8) — these are frozen approval-time records, not living
> config, and per Spec #16 §1.7 a version bump of this kind requires Platform Certification owner
> sign-off before it can be certified, which has not been sought here. Also not touched: the
> `tools/dotnet-ci` build-shim's technical claims about Unity's actual BCL/TFM/LangVersion surface
> (`netstandard2.1`, `LangVersion 9.0`) — verifying those against real Unity 6000.4.9f1 behavior is
> an engineering task, not a documentation edit, and is called out as a new OPEN ISSUE below. See the
> new OPEN ISSUES entry.)
> **Last Updated (prior):** July 11, 2026, latest same day (**Engine substrate LANDED — goal detection + score
> state + match-length/halves model (the #26 §9.3 upstream deliverables) — and the #26 half-time
> trigger + live ladder inputs ACTIVATED** (the §3.4/§1.6 PASS-1 M-1 gates CLOSED). **(a)
> Match-length model:** `MatchEngineConstants` v1.20 — `[FIXED] MATCH_LENGTH_MINUTES` (90) +
> `[DERIVED] MATCH_TICKS_TOTAL` (= 324 000; the #26 §3.5 `[CROSS-PENDING]` row promoted `[CROSS]`,
> §3.5 v0.3) + `[DERIVED] HALF_TIME_BOUNDARY_TICK` (162 000 — the FR-TP-019 Stage-0 halves model:
> boundary only, no break/end-swap/match-end). **(b) Goal detection:** `MatchEngine.cs` v1.30 —
> Resolve-phase `CheckGoalAndRestart` (executor advance → goal check → first touch):
> `BallCollision.CheckBoundaries` ⇒ KickOff = goal; scoring TEAM by exit half-space geometry (own
> goals credit the right side); per-team score + the FIRST-EVER Tier A `GoalAwardedEvent` (0x07;
> Scorer = the new last-holder tracker) + centre-spot restart (agents keep positions; possession
> cleared); non-goal exits untouched (no throw-in/corner model). **`SNAPSHOT_SCHEMA_VERSION` 13 →
> 14** (goals + last-holder serialized). **(c) #26 activation:** `RunManagerDecisionPoints` passes
> LIVE goalDiff + `ticksRemaining`/`MATCH_TICKS_TOTAL`; `ManagerDecisionGate` v1.1 fires the
> half-time decision (once, first stride at/after the boundary, regardless of interval position —
> the §3.2 worked example). Tests: new `MatchEngineGoalTests` (6) + `ManagerAITests` v1.1 (+4) +
> schema pin 14 + ScoreState probe. Spec docs: #26 section-1 v0.3 / section-2 v0.4 / section-3
> v0.3 / section-9 v0.5 (§9.1 engine-substrate gates CLOSED); `match-engine-design.md` v1.4.
> **Full dotnet gate: PASSED, 0 failures.** See src/CLAUDE.md v2.15. Remaining #26 follow-up:
> only the §9.2 own-`[GT]` balance review — the KD-6 on-disk preset format stays deferred BY SPEC
> (FR-TP-002/017: no disk format at Stage 0+1). Not built (Stage-1+ restart model): throw-ins /
> corners / goal kicks, the half-time break / end swap, match-end.)
> **Last Updated (prior):** July 11, 2026, later same day (**#26 T1–T4 manager-AI wiring LANDED** — the last
> item on the July-10 T-phase plans; default-behaviour-neutral (`ManagerMode.Human = 0` zero-init =
> the inert identity per KD-4 — no gate fire, no adaptation, no engine calls; a default match is
> byte-identical to pre-#26). **T1:** `tactical-instructions/TacticalPresetsConstants.cs` (§3.5
> scalars + the A.2 archetype / A.3 affinity `[GT]` tables; `MATCH_TICKS_TOTAL` deliberately
> absent — `[CROSS-PENDING]`) + `match-engine/TacticPresetProjection.cs` (FM-TP-01; the FR-TP-014
> roster gate at the consuming seam). **T2:** `ManagerDecisionGate` (FM-TP-02, KD-3 — kickoff +
> fixed interval; the half-time trigger stays gated on the engine halves model per §1.6/PASS-1
> M-1), evaluated only in RunAiPhase's stride branch BEFORE the FR-TI-027 commit (FR-TP-018;
> off-stride firing impossible, F5). **T3:** `ManagerProfile` (F4 NaN-gated, A.2 factory) +
> `ManagerAdaptation` kickoff scoring (Appendix B.1 exact: Aggressive → Gegenpress 0.66,
> Pragmatic → Balanced 0.50; tie → lowest ordinal, KD-8) + `ApplyKickoff` (the FR-TP-004 boot
> path via the EXISTING appliers; seeds `LastDecisionTick = 0` so the first stride gate never
> double-fires). **T4:** `StepToward`/`EvaluateLadder` (FM-TP-04, B.2 exact — 0.622 steps / 0.233
> holds; `URGENCY_DIFF_CAP`) + `RunDecisionPoint` (the FR-TP-005 mid-match path via
> `SetTeamTactic`/`SetPlayerTactic`, never the appliers — F3; decrement-then-check hold per the
> B.2 70′→80′ cadence). The live engine call passes goalDiff = 0 — engine-TRUE (no goal producer
> exists) — so both ladder terms are identically zero for any clock inputs and the T4 prerequisite
> gate is honoured with a single code path; the ladder body is unit-locked through explicit
> parameters. `MatchEngine.cs` v1.29 (public `ConfigureManager`, internal boot seams, `TestOnly_
> ManagerState`), **`SNAPSHOT_SCHEMA_VERSION` 13** (per-team `ManagerState` in pinned Appendix C
> order — mid-match manager decisions restore-deterministic, FR-TP-012). Tests: new
> `ManagerAITests` (21) + `MatchEngineSnapshotSchemaTests` v1.10 (pin 13 + ManagerState probe).
> **Full dotnet gate: PASSED, 0 failures.** See src/CLAUDE.md v2.14. Remaining #26 follow-ups are
> the spec's own engine-substrate gates (half-time trigger; live goalDiff/`MATCH_TICKS_TOTAL` —
> upstream match-engine deliverables per §9.3) + the KD-6 on-disk preset format (parser swap).)
> **Last Updated (prior):** July 11, 2026 (**Specs #23/#24/#25 wiring LANDED** — the T-phase step after the
> July-10 T0 scaffolding; all default-behaviour-neutral (Balanced ⇒ Off/None/Off = the exact
> identities, byte-identical default match). **(a)** `SlotComposer` v1.2 gains the #24 build-up
> overlay stage (Step 3b, FM-BU-02 — after ContextModifier, before spacing) and the #23 dismark
> offset stage (Step 4b, FM-DM-02 — after spacing, before the pitch clamp), per ERR-012-007/008
> and the #24 §4.2 combined order; `PositioningPerceptionSnapshot` v1.1 carries the routing dials +
> per-agent pressure/marker carriers (zero defaults = identities). **(b)** New
> `positioning-ai/RotationController.cs` (#25 §3.1–§3.4: FM-RO-01 predicate on the
> controller-owned SERIALIZED `LastComposedTarget` cache per PASS-1 H-1, FM-RO-02 dwell/commit +
> hold/revert, atomic pairwise `SlotIndex` swap + partner lock, phase-exit freeze, FR-RO-009
> per-tick cap, F2/F5/F6 validating restore seams) wired into `PositioningAITick` v1.3 per
> §4.2/ERR-012-009 (sole post-seed `SlotIndex` writer; identity binding never rewrites a row).
> **(c)** #23 §3.4 marked-pass-target penalty in #8 `UtilityScorer` v1.10 (passer-view proximity ×
> passer awareness per FR-DM-010/011; Off ⇒ exact ×1.0); `TacticalContext` v1.7 +
> `DismarkIntensity`; `TacticalWeights` v1.5 + `TargetMarkedUtilityMult` [GT] /
> `MarkedPassRadiusM` [CROSS]. **(d)** `MatchEngine.cs` v1.28, **`SNAPSHOT_SCHEMA_VERSION` 11 →
> 12**: Phase-D dial writers + one-stride-stale dismark carriers (§3.2 M-1 contract), per-agent
> dwell update in the perception pass (FR-DM-003, runs regardless of dial), #24
> classify/check-then-decrement pre-pass + FM-BU-03 TEAM-LEVEL regain arming in
> `OnPossessionChanged` (settledTeam diff; Balanced carries HoldShape so a default match never
> opens a window), v12 serializes dwell / zone+settledTeam / rotation binding+cache+pairs + the
> three dials appended to `WriteTeamTactic` in pinned #21 Appendix B order; 9 TestOnly seams.
> Tests: +`SlotComposerStageTests` (7) + `RotationControllerTests` (12); `UtilityScorerTests`
> v1.5 (+4 incl. the exact 0.832 worked example), `MatchEngineTacticTests` v1.5 (+5),
> `MatchEngineSnapshotSchemaTests` v1.9 (pin 12 + 2 probes). **Full dotnet gate: PASSED, 0
> failures.** See src/CLAUDE.md v2.13. Next per the T-phase plans: #26 T1 preset→config
> projection, T2 decision gate, T3 kickoff scoring, T4 adaptation.)
> **Last Updated (prior):** July 10, 2026, later same day (**Specs #23–#26 all `IN REVIEW → APPROVED`; steps
> completed: sign-off + back-props + the last citation** — (1) lead-developer R-01..R-05 sign-off
> granted on all four (each `section-9-approval-checklist.md` → v0.4 with the §9.5 gate table +
> §9.6 decision, per the #22 template; all 44 spec-folder files flip `Status: APPROVED`;
> `SPEC_INDEX.md` **26 APPROVED / 0 IN REVIEW**). (2) The seven cross-spec back-props FILED and
> landed atomically with the flips (`spec-error-log.md` v1.30): ERR-021-005/006/007 — #21
> `TeamTactic` gains `DismarkIntensity`/`BuildUpStructure`/`RotationFreedom` + Appendix B appends
> in pinned approval order #23 → #24 → #25 after `MarkingOrientation` (`tactical-instructions/
> section-2.md` + `appendices.md` → v0.5; serialization enters `WriteTeamTactic` + schema bump
> only at each spec's wiring); ERR-012-007/008/009 — new `positioning-ai/section-3.md` §3.7.1
> (v0.6) pins the build-up overlay stage (ContextModifier → spacing), the dismark offset stage
> (spacing → pitch clamp, FR-DM-008), the `RotationController` pre-composition position, and the
> `AgentPositioningData.SlotIndex` single-writer contract amendment (numbers 004–006 deliberately
> skipped — soft-reserved by the June-13 quarantine cluster whose ERR-012-003 citation is already
> live); ERR-008-012 — `decision-tree/section-3-2.md` §3.2.2.1 (v1.5) anchors the FM-DM-03
> marked-pass-target multiplier in the pre-clamp tactical product. (3) The #26 Bradley row
> VERIFIED: Bradley, P. S. & Noakes, T. D. (2013), *Match running performance fluctuations in
> elite soccer: indicative of fatigue, pacing or situational influences?*, **J Sports Sci
> 31(15):1627–1638, DOI 10.1080/02640414.2013.796062, PMID 23808376** (index-level corroboration
> across PubMed + independent indexes; publisher/Crossref direct resolution still blocked by the
> environment's network policy — same evidence class as the accepted Wilson rows). §8 citation
> rows now closed across ALL specs. Carried forward post-APPROVED, non-blocking: the `[GT]`
> balance passes (#21 G2 precedent) and the #26 engine-substrate gates (T2 halves/
> `MATCH_TICKS_TOTAL`, T4 goal-detection — upstream match-engine deliverables). Implementation of
> #23–#26 per each spec's §6 T-phase plan is the next body of work.)
> **Last Updated (prior):** July 10, 2026 (**#23–#26 post-PASS-1 open gates closed where closable** — §8
> `[CITATION-PENDING]` rows: #23 both VERIFIED (Wilson Orion 2008 ISBN 978-0-7528-8995-5; Low et
> al. 2020 *Sports Medicine* 50:343–385 DOI 10.1007/s40279-019-01194-7); #24 Wilson VERIFIED +
> Spielverlagerung reclassified informal background per its own resolution path; #25 Wilson
> VERIFIED + the Memmert & Raabe book row REPLACED with the verified Low et al. 2020 review per
> the #10/#11 OI-003 replace-with-verifiable precedent; #26 Wilson VERIFIED, the Bradley
> score-line row stays `[CITATION-PENDING]` with a recorded July-10 environment-blocked
> verification attempt (search quota + Crossref/publisher access unavailable — not fabricated,
> per the "never fabricate" rule). **#25 Appendix A completed**: A.2 (4-3-3, 5 rows — single
> pivot deliberately excluded, rest-defence anchor) + A.3 (4-2-3-1, 6 rows — double pivot rotates
> as a pair) authored against the verified `Family433`/`Family4231` slot rosters (F442/F433/F4231
> = the complete `FormationFamily` enum), F1 hand-audits recorded. **#26 A.1 preset compositions
> pinned** against the actual #21 enum member names (PASS-1 L-2 close-out; all names verified
> present, full rosters recorded). Checklists at v0.3. Remaining open gates: the one #26 Bradley
> citation row; back-prop ERRs at `APPROVED`; #26 engine-substrate gates (upstream-owned);
> R-01..R-05 sign-off.)
> **Last Updated (prior):** July 8, 2026, later same day (**Section-file PASS-1 adversarial reviews run on
> all four IN-REVIEW specs #23–#26, all findings resolved in same-day v0.2 fix passes** — #23
> Dismarking 0H+1M+3L (M-1: the dwell-update-inside-#12-tick claim was impossible — `FilteredView`
> is built in the per-agent pass AFTER Positioning in the stride order; now a pinned one-stride-
> stale consumption contract); #24 Build-Up 0H+3M+2L (M-1: the post-regain suppression window
> armed on EVERY teammate reception — `PossessionChangedEvent` carries per-agent holder ids and
> fires on intra-team transfers, verified against the payload; now team-level-regain arming. M-2:
> zone hysteresis reformulated as committed-zone expansion, well-defined for long-ball jumps. M-3:
> catalogue lane keys corrected — fullbacks occupy wide L/R lanes, not LH/RH); #25 Rotations
> **1H**+1M+3L (H-1: §4.2's "previous-tick composed targets on `AgentPositioningData`" did not
> exist — the struct has no such field — and the restore re-seed broke FR-RO-013/T-RO-DET-003
> byte-identity; now a controller-owned SERIALIZED `LastComposedTarget` cache. M-1: phase exit
> reset dwell in the pseudocode while FR-RO-010 mandated freeze — the test plan contradicted the
> pseudocode, caught at spec stage. PASS-2 re-read clean at H/M per the High-found rule. L-3:
> `LINE_DWELL_TICKS = 5` verified, 30 ≥ 5 with 6× margin); #26 Presets 0H+1M+2L (M-1: §3.2/§3.4
> consumed engine score/halves state that does not exist — no goal producer, no halves model, and
> `MATCH_TICKS_TOTAL` was an untagged phantom; now explicit T2/T4 prerequisite gates +
> `[CROSS-PENDING]` row. L-1: Appendix E sensitivity values re-derived — ~39.4′/~52.5′, not
> ~35′/~85′). Four `adversarial-review-section-files-v1.md` files filed; §9.3 gates updated.
> Remaining open gates: `[CITATION-PENDING]` §8 rows, back-prop ERRs at `APPROVED`, #25 Appendix-A
> family completeness, R-01..R-05 sign-off.)
> **Last Updated (prior):** July 8, 2026 (**Candidates #23–#26 promoted to section files at `IN REVIEW`** —
> all four authored as full 11-file spec sets (v0.1) from the two July 7 design supplements, per
> each supplement's own §6 promotion pipeline (steps 1–3): `docs/specs/dismarking-ai/` (#23,
> FR-DM), `docs/specs/build-up-structures/` (#24, FR-BU), `docs/specs/positional-rotations/`
> (#25, FR-RO), `docs/specs/tactical-presets/` (#26, FR-TP). `SPEC_INDEX.md` registry rows added
> (**22 APPROVED / 4 IN REVIEW**); RESERVED entries retired; supplements bumped to v0.4/v0.5 with
> promotion notes + the "Specification Before Code" citation fix (a README.md heading, not
> CLAUDE.md — both §6 sections cited it wrongly). Section-file PASS-1 adversarial reviews NOT yet
> run — each spec's §9.3 records its open gates (`[CITATION-PENDING]` §8 rows; back-prop ERR
> filing at `APPROVED`; R-01..R-05 sign-off). See the updated OPEN ISSUES entry below.)
> **Last Updated (prior):** July 7, 2026, later same day (**Two design supplements opened, AR cycle
> converged same day, §6 Implementation Plans added** — for the items the same day's
> tactical-theory cross-reference flagged as too large for a cheap routing-seam reuse — see the
> new OPEN ISSUES entry below. `docs/tracking/advanced-positional-behaviors-design.md` v0.1 → v0.3
> (dismarking, scripted build-up structures, positional rotations — candidate specs #23–#25) and
> `docs/tracking/game-model-ai-manager-design.md` v0.1 → v0.4 (tactical preset library +
> AI-manager selection/adaptation — candidate spec #26). AR-1 (0H+0M+2L) + AR-2 (0H+0M+1L) +
> AR-3 (clean, CONVERGENCE). Both DESIGN SUPPLEMENT stage only (pre-promotion, no code, no
> section files) — parallel to the #21/#22 pre-approval precedent.)
> **Last Updated (prior):** July 7, 2026 (**Four cheap-item tactical additions landed** — a `MarkingOrientation` dial (#14 MAN_MARK radius scalar), a Positioning AI #12 rest-defense coverage check (dampens risky PASS/SHOOT/DRIBBLE), a half-spaces PASS bonus (routes each agent's existing #12 lane into #8's utility scorer), and a curving-press blind-side bias (#13). All default-behaviour-neutral; `SNAPSHOT_SCHEMA_VERSION` 10 → 11. See the new OPEN ISSUES entry + src/CLAUDE.md v2.9. AR-1 (0H+0M+1L, resolved: §7.x citation-collision renumbering) + AR-2 (clean, CONVERGENCE).)
> **Last Updated (prior):** July 2, 2026, latest same day (**Living World #22 slice 2 landed + AR-1 resolved** — ArcEngine (§3.4 spawn/atomic-pin/resolve/§6.2-expiry; `world.arcs` trigger draws stay the KD-10 seam per FR-LW-020/031) + ActiveSetMembership (§3.5 entry/LRU-at-cap/own-club Depart, FR-LW-023/025) wired into WorldLoop phases 4/6; AR-1: 0H+2M+4L resolved (pin-array snapshot; promotion mask check via new `ColdStore.TryPeek` verify-before-take; overflow gate; 2 doc); AR-2 full-surface: 0H+1M+2L resolved (Add mask gate + upfront entity validation close the residual FR-LW-025 strand vectors; scope docs); AR-3: 0H+0M+2L doc-only — **CONVERGENCE, slice-2 AR cycle closed** — 24-test suite. See the updated OPEN ISSUES entry + src/CLAUDE.md v1.94–v1.97.)
> **Last Updated (prior):** July 2, 2026, later same day (**Living World #22 season/world loop slice 1 landed** — the first KD-10 prerequisite (#22 §7.1 "persistent world store + season-calendar loop"). New `src/living-world/` services on the T0 data types: `WorldClock` (KD-4 — worldTick = calendar day, never the match loops), `WorldLoop` (§4.2 phase order; phase-3 decay live, phases 1/2/4/5/6 documented seams — no phantom interfaces per FR-LW-031), `MemoryStore` (canonical-order edges; §3.2 evict-before-append + FR-LW-018 pins; §3.1 owned-layer ApplyEvent, PlayerEdge refused), `ColdStore` (§3.5 Compress/Rehydrate; Residue-A v1 schema recorded; FR-LW-009 episodeId resume). 20-test suite. See the new OPEN ISSUES entry + src/CLAUDE.md v1.90.)
> **Last Updated (prior):** July 2, 2026 (**Minimal match viewer landed** — first presentation-layer surface. New `src/match-viewer/` assembly (`TacticalDirector.MatchViewer`; presentation tooling, not a numbered spec): `MatchReplayRecorder` ticks a real `MatchEngine` and samples world state between ticks through a new public read-only observation surface (`MatchEngine.cs` v1.24: `BallView`/`AgentView(i)`/`AgentTeamId(i)`/`AgentIsGoalkeeper(i)`/`PossessingAgentId` — value-type copies, no behaviour change); `HtmlReplayExporter` emits a single self-contained HTML canvas replay (pitch markings, home/away/GK/possession/ball-height cues, play/pause/scrub/speed; NOT a determinism-pinned wire format). Observer-neutrality digest-locked by `MatchViewerTests` (recorded run == unobserved same-seed run). See the new OPEN ISSUES entry.)
> **Last Updated (prior):** June 28, 2026 (Status check confirms `tools/dotnet-ci/known-failures.txt` quarantine is empty — the June 12 burn-down (see OPEN ISSUES "Dotnet CI gate quarantine burn-down — RESOLVED") holds. Also surfaced: this file's history had not mentioned the **Match Engine** integration layer (`src/match-engine/`, governed by `docs/tracking/match-engine-design.md`, NOT a numbered spec) — the composition root wiring all 20 approved subsystems into the `deterministic-sim` 7-phase tick pipeline. Phases A–E are complete as of June 27, 2026 (full canonical world-state snapshot serialization through `SNAPSHOT_SCHEMA_VERSION` 8; Physics/Resolve/AI-phase wiring through Positioning→Pressing→Defensive→Attacking→DecisionTree; Events-phase possession-changed producer/consumer). **Phase F (capstone closed-loop scenario on the #19 `ScenarioRunner`) is the only remaining phase** — see new OPEN ISSUES entry below. README.md and file-manifest.md status sections updated to match.)
> **Last Updated (prior):** June 12, 2026 (Non-certifying Linux compile/test CI gate landed (`tools/dotnet-ci/` + `dotnet-compile-test` job in ci.yml): asmdef→csproj generator + ~6-type UnityEngine shim compiles the ENTIRE src/ tree (production netstandard2.1 = Unity 2022.3 BCL surface) and runs every NUnit suite under `dotnet test` on ubuntu — closing the verification gap behind the seven consecutive structurally-dead build surfaces. First-ever full-tree compile found EIGHT more never-compiled surfaces, headlined by ERR-017-002 (H): #17 §3.2.1/§3.2.2 specified Publish/Subscribe overloads distinguished ONLY by generic constraint — illegal C# (CS0111) — implemented verbatim in EventBus + five spec EventBusStub files, so the event-system PRODUCTION assembly never compiled; spec patched same commit (section-3.md v1.0.2), code now single `where T : struct` methods with cached EventTierCache<T> marker dispatch, call sites unchanged. Also: ProfilerMarker imported from the wrong namespace ×18 files; File.Move(overwrite:) absent from netstandard2.1 (SaveManager); ShotExecutor PascalCase vs ALL_CAPS enum members; missing usings (CoverShadowSelector Span<T>, UtilityScorer FilteredView — decision-tree was STILL dead post-June-11); GoalkeeperMechanics int?→int; the SIXTH stray-brace dead test suite (ShotMechanicsTests §5.12 fixture); DefensiveAITests' 51 internal [Test] methods (NUnit requires public — suite could never run); NUnit API misuse in two suites; EventRegistry static-init order fragility (EnsureInitialized() fix); SipHash old-fixture vectors 4–7 FABRICATED (production correct per independent mirror). Then 1,165 tests executed for the first time in project history; 30 genuine model/expectation failures quarantined shrinking-only (tools/dotnet-ci/known-failures.txt + docs/tracking/dotnet-ci-quarantine.md, per-test hypotheses filed) — any NEW failure or compile error fails CI. Gate is explicitly NON-CERTIFYING (certification-platform.md v1.2): determinism certification stays on the pinned Windows/Unity tuple. See src/CLAUDE.md v1.66 and the new OPEN ISSUES entry.)
> **Last Updated (prior):** June 11, 2026, later same day (Decision Tree #8 comprehensive audit (AR-2) completed — 3H+11M+9L over spec + implementation; assembly had never compiled (static calls to instance executors, missing asmdef ref); away-team zone modifiers/press urgency/line-depth all home/away-asymmetric (every prior example and fixture was home-team); §3.7.2 state machine implemented (PASS/SHOOT hold EXECUTING; forced-refresh same-type suppression); ERR-008-002..011 filed, spec patched same commit; see the resolved OPEN ISSUES entry below and `docs/specs/decision-tree/audit-report.md`.)
> **Last Updated (prior):** June 11, 2026 (Pass Mechanics #5 AR-9 fix pass: 1H+3M+5L, then AR-10 sweep: 2L (resolved same commit; no functional findings) — the FIFTH consecutive spec whose test suite was structurally incapable of catching its defects. H-1: `src/pass-mechanics/Tests/PassMechanicsTests.cs` has NEVER compiled since v1.1 (2026-06-01) — namespace closed before the appended IT- integration fixture, stray `}` at EOF (CS1022), fixture stranded in the global namespace; identical defect class to First Touch ERR-004 (170/171 braces there, 161/162 here). All AR-2..AR-8 "the test suite enforces X" claims were unverifiable while the suite was dead. M-1: PassExecutor Idle-guard rejection stomped `_lastResult` — an Execute() during FollowThrough/Complete destroyed the committed Completed record (ContactFrame replay-sync data) and surfaced Invalid at the next IsIdle; rejection now reported via return value only. M-2: FM-07 distance gate `d <= 0f` passed NaN (compares false) and Mathf.Max argument ordering silently sanitised it to a 0.001 m pass; gate now `!(d > 0f) || IsInfinity(d)` per the project NaN-gate pattern (FT AR-8 M-1 / AM AR-10 / CS AR-7). M-3: stale tackle flag — cleared only by WINDUP polling, so a tackle registered during FollowThrough/Idle (even while not in possession) spuriously cancelled the agent's NEXT pass on its first WINDUP frame; drained (discarded) at INITIATING per §3.8.5 freshness. L: CONTACT pressure re-sample now queries passer position fresh (INITIATING cache was up to ~15 frames stale on a pass on the run); ComputeErrorAngle NaN fallback flipped MinErrorAngle → MaxErrorAngle (failed OPEN — corrupted input produced a 0.1° laser pass); declared-but-unconsumed doc-notes (PhysicalProfile.DistMin/DominantSpin/IsAerial incl. the IsAerialFormula parallel-surface hazard with 9-profile agreement verified, PassAgentAttributes.Crossing); PassOutcome.Cancelled / PassAgentState.Position doc corrections; through-ball SPEC-DEVIATION NOTE (kickSpeed derived from IntendedDistance BEFORE the lead projection extends the aim point ⇒ led passes systematically underhit; joins KD-4 / §7.1 Stage 1 upgrade). New PassExecutorGuardTests fixture PX-001..004 locks M-1/M-2/M-3 via pure stub seams (no EventBus boot — all paths terminate pre-publish). Files: PassExecutor.cs v1.12, PassErrorCalculator.cs v1.8, PassTargetResolver.cs v1.8, PhysicalProfile.cs v1.2, PassAgentAttributes.cs v1.1, PassAgentState.cs v1.1, PassOutcome.cs v1.3, Tests/PassMechanicsTests.cs v1.2, src/CLAUDE.md v1.63, file-manifest.md header.)
> **Last Updated (prior):** June 10, 2026 (Collision System #3 AR-7 fix pass: 1H+3M+3L, then AR-8 sweep: 1H+1M+1L, then AR-9 sweep: 2L doc-only — Agility unconsumed-field pointer, AGENT_BALL ContactPoint Z claim, then AR-10 sweep: 2L, no functional findings — dead MaxIterations doc-noted, tracking-row tally corrected; mechanical verification of bitfield uniqueness (253 pairs), 3×3 broad-phase coverage, and emit gating (all resolved; ERR-003-001..006 filed and closed same day). Both H findings were closed-loop model defects the test suite ENCODED rather than caught. AR-7 H-1/ERR-003-001: F = j × 60 Hz assumed the whole impulse acts in one 16.7 ms frame — the entire stochastic fall/stumble band (500–1500 N literature values) sat below walking pace (P(fall)=1 at ~0.5 m/s closing; knockdownForceOut pinned at 1.0); new [GT] ContactDurationS (0.15 s), F = j / ContactDurationS, PHYSICS_TICK_HZ removed (sole consumer). AR-8 H-1/ERR-003-005: impulse approach gate INVERTED — with the a1→a2 manifold normal, vRel=(v1−v2)·n>0 is approaching, but the gate returned separation-only for vRel>0, so genuine closing collisions exchanged no momentum and EvaluateFallOrStumble was unreachable for real contacts, while overlapped pairs already moving apart were velocity-reversed back inward (CR-001 rationalised this as a 'passed-through state'); gate + impulse signs corrected (j>0 invariant preserved), restitution verified e·v. M: FROM_BEHIND broken on three surfaces — formula sign (ERR-003-002), unflipped instigator→victim normal at the call site (same ERR), and shadowing by the velocity-only shoulder predicate (ERR-003-006, AR-8); same-team hits above fallThreshold escaped both fall and stumble branches (ERR-003-003); MaxCollisionPairs valve counted broad-phase candidates and aborted the whole frame in goalmouth densities (ERR-003-004 — now counts narrow-phase CONFIRMED collisions, cap = event-buffer capacity). L: non-finite velocity sanitised to zero at the snapshot gate (NaN previously published into CollisionEvent.ImpactForce); both-grounded overlaps no longer emit 60 zero-force events/s; RecordEvent drop warning; CellX/CellY FloorToInt (cell 0 was double-width for negative coords). Spec §3.3/§3.4 pseudocode patched in the same commit (6 ERR anchors); CONTACT_DURATION_S added to the §3.3 catalogue. All test expectations re-derived for the corrected model and verified by a numerical mirror including the xorshift128+ RNG (FL-002 5210/10000 stumbles vs 0.5175 predicted; FL-003 5073 falls; FL-004 90/0; CR-001 ∓1.5 m/s). Files: CollisionResponse.cs v1.6, CollisionSystem.cs v1.6, ContactTypeClassifier.cs v1.3, CollisionSystemConstants.cs v1.5, SpatialHashGrid.cs v1.4, CollisionEvent.cs v1.2, ContactForceData.cs v1.1, AgentAgentCollisionResult.cs v1.2, tests/CollisionSystemTests.cs v1.3, spec-error-log.md v1.24, docs/specs/collision-system/section-3-3.md + section-3-4.md.) Prior June 9, 2026 (Agent Movement #2 AR-12 fix pass: 3H+1M+3L, then AR-13 sweep 2M (both resolved). The three H findings were closed-loop speed-control defects invisible to pure-function/mid-flight tests: H-1 agents at rest could never start moving (IDLE branch only decayed speed while EvaluateFromIdle required speed > IdleExit — IDLE now accelerates toward the command-capped topSpeed on moving intent); H-2 commandSpeed never capped the speed-integration target (jog commands auto-promoted to SPRINTING and drained the reservoir; walk commands flapped WALKING→JOGGING→DECELERATING — Step 4–5 now applies topSpeed = min(topSpeed, commandSpeed) and ApplyAcceleration gains an asymptote ceiling); H-3 Zeno deceleration (per-frame a = v²/(2d) against the fixed total d → ~78 s / ~32 m to stop from 6 m/s; new MinDecelerationFloor [GT] bounds the tail; §3.2.5 constant-rate spec-deviation note filed). M-1 LeanAngle now reflects velocity-direction path curvature, not facing rotation. AR-13: exhausted agents (AerobicPool < AerobicJogFloor) with jog commands clamp commandSpeed to JogEnter (kills a ~3 Hz aerobic-gate flap); IDLE launch additionally gated on a non-degenerate target offset so the Decision Tree HOLD shape (StrafeWhileWatching at own position) keeps resting agents at rest. New MovementCommand.WalkTo factory; closed-loop regression fixture T-AM-110..115 + decel-floor units T-AM-108..109. Files: AgentMovementSystem.cs v1.15, AgentLocomotion.cs v1.5, AgentDirectionalMovement.cs v1.7, AgentMovementConstants.cs v1.9, AgentState.cs v1.5, GroundedReason.cs v1.1, MovementCommand.cs v1.3, tests v2.2/v1.1, test-plan.md v0.3. Prior June 9, 2026 (Ball Physics #1 AR-7 fix pass: 2H+4M+3L, then AR-8 sweep 2L (resolved) — clean. H-1/ERR-001-001: bounce ground normal was Unity Y-up `Vector3.up` in the Z-up coordinate system — a falling ball never rebounded; fixed in `BallGroundInteraction.cs` AND in the §3.1.8.1 spec pseudocode that sourced it. H-2: ValidatePhysicsState ground clamp zeroed Velocity.z before the state machine could see vz<0, trapping fast descents in a permanent Airborne ground-hover; Airborne now keeps vz through the clamp. M-1/ERR-001-002: friction stick impulse gains the 1+m·r²/I=2.5 coupling divisor. M-2..M-4 + L: test spin-sign convention fixed, gravity added to the Bouncing branch, LongBall test windows re-derived from the model (verified numerically), magic literals catalogued, MomentOfInertia retagged [DERIVED], ERR-001-003 [EST] inventory filed. Prior June 8, 2026 (Pass Mechanics #5 AR-8 fix pass: 0M + 3L. L-1: AR-7's CrossSubType-ignore warning brace-add left an empty `if (cond) { }` in production builds; gate hoisted to wrap the entire if-statement since the diagnostic has no functional follow-up (the other 7 AR-7-gated emits MUST keep the body-gate form because their if-bodies contain `_lastResult` + `return`). L-2: `ExecuteContact` state transition (`_state = FollowThrough`) hoisted above Step 8 `EventBusStub.Publish` — if Publish throws, the ball was already kicked at Step 6 and the executor must not stay in `Contact` (re-entry would re-run `ApplyKick`); the FM-08 possession recheck currently guards against the double-kick, but defensive ordering removes the dependence on the recovery seam. L-3: forward-reference notes inserted next to the AR-2 M-2 v1.6 / v1.3 history rows in `PassExecutor.cs` and `PassTargetResolver.cs` — the "[-1, +1]" characterisation there is the AR-2-era contract, superseded by AR-6 L-1 to "[-1, +1)"; historical rows preserved verbatim. Files: `PassExecutor.cs` v1.11, `PassTargetResolver.cs` v1.7. AR-8 sweep clean — no further high or medium issues. Prior June 8, 2026 (Pass Mechanics #5 AR-7 fix pass: 1M+3L all resolved on a fresh-eyes full-surface sweep over all 24 files in `src/pass-mechanics/`. M-1: FR-CS-031 gating drift fixed across sibling files — `PassMechanicsConstants` v1.2 (AR-2 L-13) gated its FM-01 `Debug.LogError` emits but the parallel cold-path emits in `PassExecutor.cs` (8 emits), `PassTypeProfiles.cs` (2 emits), and `PassVelocityCalculator.cs` (2 emits) never got the same `#if UNITY_EDITOR || DEVELOPMENT_BUILD` gating. All 12 emits now gated. L-1: `[-1, +1]` → `[-1, +1)` propagation from AR-6 producer-side correction to the two consumer-side surfaces — `PassTargetResolver.ApplyErrorToDirection` `<summary>` and `<param>` for `errorDirectionFraction`, plus the `PassExecutor.ExecuteContact` Step 3 callsite comment. L-2: `EventBusRegistrar.cs` v1.3 history row's "no `InternalsVisibleTo` on this assembly" rationale was already stale at AR-3 time — `AssemblyInfo.cs` created 2026-06-01 with `[InternalsVisibleTo("TacticalDirector.PassMechanics.Tests")]`; corrected to the boundary-mocking rationale alone. L-3: `CrossSubType` and `PassType` enums gained the ORDINAL STABILITY paragraph parallel to `CancelReason` v1.4. `PassType` carries a stronger contract — beyond being embedded in both `PassAttemptEvent` (0x0C) and `PassCancelledEvent` (0x0D) payloads, `(int)_request.PassType` is the third hash input to `ComputeErrorDirection`, so reordering would break deterministic error-direction parity even before the event digest catches the drift. Files: `PassExecutor.cs` v1.10, `PassTypeProfiles.cs` v1.4, `PassVelocityCalculator.cs` v1.4, `PassTargetResolver.cs` v1.6, `EventBusRegistrar.cs` v1.4, `CrossSubType.cs` v1.1, `PassType.cs` v1.2. AR-7 sweep clean. Prior June 8, 2026 (Pass Mechanics #5 AR-6 fix pass: 1M+3L all resolved — converts the AR-5 cycle-stop. M-1 finished what AR-5 started: input-mix primes for `frameNumber` / `passTypeIndex` in `PassErrorCalculator.ComputeErrorDirection` replaced with xxHash64 PRIME64_3 (`0x165667B19E3779F9`) and PRIME64_5 (`0x27D4EB2F165667C5`) so the Stafford Mix13 finalizer no longer multiplies through the same primes the input-mix already used (`0xBF58476D1CE4E5B9` and `0x94D049BB133111EB` remain as finalizer multipliers only). Input-mix primes are now disjoint from finalizer primes on all three axes, completing the AR-5 M-1 invariant. L-1: `<returns>` upper bound corrected `[-1, +1]` → `[-1, +1)` to match the 24-bit mantissa quantisation (EC-010 already enforces `Assert.Less(dir, 1.0f)`). L-2: comment block rewritten to call out the additive-vs-XOR asymmetry on `agentId` (AR-5 intent) and record the AR-6 input-mix/finalizer prime disjointness invariant. L-3: bit-extraction literals `0x00FFFFFFu` / `0x01000000u` promoted to named local consts `Mantissa24Mask` / `Mantissa24Scale` with a comment noting the 24-bit window matches float mantissa precision. Files: `src/pass-mechanics/PassErrorCalculator.cs` v1.7. Closes the long-standing cycle-stop carve-out. Prior June 8, 2026 (cross-spec routing close-out: `Possession.ControlHeight` ↔ `GroundControlHeight` resolved — Ball Physics #1 §3.1.11 is the authority, First Touch #4 `GroundControlHeight` is now a `[CROSS]` mirror; sibling-hazard sweep (`ControlRadius` / `ControlVelocity` / `ChallengeRadius`) returns no other parallel declarations. Prior June 7, 2026 (AR-hardening sweep complete: every coded section's last adversarial round now yields no findings or L-only — except Pass Mechanics #5 AR-5 (1M+3L) which carried an explicit "cycle stop" (converted to AR-6 above on June 8, 2026). Final AR by spec: #1 AR-8 (2L resolved; AR-7 2H+4M+3L fixed June 9, 2026) ✓; #2 AR-11 (2L) ✓; #3 AR-6 (3L) ✓; #4 AR-6 (3L) ✓; #5 AR-10 (2L, no functional findings; AR-9 1H+3M+5L fixed June 11, 2026) ✓; #6 AR-4 (3L) ✓; #7 AR-2 (3L) ✓; #8 AR-2 (clean) ✓; #10 AR-2 (clean) ✓; #11 AR-3 (clean) ✓; #12 AR-3 (clean) ✓; #13 AR-2 (clean) ✓; #14 AR-2 (clean) ✓; #15 AR-3 (clean) ✓; #16 AR-3 (1L) ✓; #17 AR-11 (no findings) ✓; #18 AR-4 (2L) ✓; #19 AR-5 (2L) + PR #132 Codex P2 follow-up ✓. Significant test scaffolding landed: Ball Physics enum-ordinal-stability + body-part-coefficients + surface-properties tests; Agent Movement T-AM-001..107 regression + unit roster (18 + 59 NUnit tests across 11 fixtures); `docs/specs/agent-movement/test-plan.md` v0.2. The PR #132 Codex P2 follow-up to `PerfGateRunner.Run` rejects mismatched perf-baseline pairs via `ArgumentException` before delegating to `RegressionGate.Evaluate` — FR-PO-031 requires same scenario, seed, platform pin, and loop; runner validates `baseline.Loop == current.Loop` unconditionally and `ScenarioManifestId` / `Seed` / `PlatformPin` when both records carry a non-null `SessionManifest`. Stage 0 host platform pin landed same day in `docs/tracking/certification-platform.md` v1.1: Windows 11 / Unity 2022.3.62f1 / Mono / x64 / SSE4.2 / 1 worker / DAZ+FTZ+fp-contract+FMA all off. Closes the long-standing OPEN ISSUE; unblocks FR-DS-009-GATE Stage 0 activation, FR-PO-052 perf-gate, #19 §7.5 D1 test-runner pin, #18 §3.9.4 warmup-measurement path, and #16 §4.8 EnvironmentFingerprint digest semantics.)
> **Last Updated (prior):** June 10, 2026, latest same day (First Touch #4 scenario corpus: `heavy-touch-runs-on` (ERR-004-003 displacement-velocity coherence lock) + `interception-chain-anchors-at-displaced-ball` (ERR-004-004 ball-anchored gate via real PressureEvaluator, §3.4.5 redirect + Frame N+1 CONTROLLED chain) on the #19 ScenarioRunner; new AssemblyInfo.cs InternalsVisibleTo; envelope windows mirror-derived. Prior same day: First Touch #4 AR-7 fix pass: 1H+3M+3L, then AR-8 sweep: 2M — ERR-004-003..006 filed; ERR-004-003/004/006 closed same day, ERR-004-005 documented-open. H-1/ERR-004-003: §3.3.2 direction-blend sign inverted — heavy touches displaced the ball back toward the passer against their own §3.3.5 retained momentum; spec pseudocode patched same commit; the test suite encoded BOTH sign conventions at once and had never compiled (unbalanced brace since v1.1). M/ERR-004-004: interception proximity re-anchored from the agent to the displaced ball per §3.4.2 (PressureEvaluator now supplies the global-nearest opponent position). §3.4.5 interception velocity redirect implemented (was specified, never coded). AR-8: EvaluateFirstTouch non-finite input sanitise gate (Clamp01 passes NaN); §5.10 VS-001 hand-calc used a non-§3.2.3 additive velocity modifier (ERR-004-006, spec + test re-derived to r≈0.195 m). All expectations verified by a full-pipeline numerical mirror. Files: 8 src files + tests v1.2 + 3 spec section files + spec-error-log v1.25.) Prior same day (Scenario-corpus expansion on the #19 ScenarioRunner. Spec #1 per-spec corpus: `drop-and-rebound` (AR-7 H-1 / ERR-001-001 lock — load-bearing predicates are the 1.0–1.45 m first-rebound-peak window and exact X/Y purity of a spinless vertical drop) + `fast-descent-grounds-out` (AR-7 H-2 hover-deadlock lock, extended to the full composed settle) in `src/ball-physics/tests/BallPhysicsScenarios.cs` + `BallPhysicsScenarioTests.cs`; envelope windows derived from a numerical mirror of the fixed model. First cross-spec corpus per KD-8: `lofted-pass-kick-bounce-roll` under `tests/scenarios/cross-spec/` (owning specs {1, 5}) chains the real `PassExecutor` (#5) WINDUP→CONTACT lifecycle into the real `BallPhysicsCore` loop (#1) through the `IPassBallSystem` seam, with #17 EventBus boot wiring + one-tick Resolve-phase lifecycle around the CONTACT publish — the composition surface where per-spec suites passed while the chain died at first touch-down pre-AR-7. New files: CrossSpecScenarios.cs / CrossSpecScenarioTests.cs in `src/testing-strategy/Tests/`; asmdef reference updates (ball-physics-tests + testing-strategy-tests); file-manifest reconciled (incl. three June-7 ball-physics test rows missing from its per-file table). src/CLAUDE.md v1.60.) Prior June 10, 2026, still later same day (Testing Strategy #19 ScenarioRunner AR-2 sweep: 0H+0M+2L, both resolved — NaN in_range bounds now throw as authoring errors instead of masquerading as failing predicates; min>max exception message InvariantCulture. ScenarioEnvelope v1.2, ScenarioRunnerTests v1.2 (19 tests), src/CLAUDE.md v1.59. Otherwise clean.) Prior June 10, 2026, later same day (Testing Strategy #19 ScenarioRunner AR-1 fix pass: 0H+4M+6L, all resolved. M-1 entry/scenario manifest-coherence guard (a ClosedLoopScenario registered under a different manifest instance than it executes would pass load-time validation against a manifest the run never uses); M-2 non-empty `fixture_refs` refused at Stage 0 (no fixture loader exists until the Stage 0+1 KD-10 deliverable; §3.3.4 forbids silent acceptance); M-3 diagnostics hardening (CR/LF sanitized out of predicate IDs / details / exception messages so the line-oriented key=value encoding cannot be corrupted; `exception_stack=` line added — a thrown body previously dropped its stack); M-4 A.1 name-uniqueness now actually enforced (v1.0 doc claimed it via path-uniqueness), plus §3.3.5 path↔name coherence and cross-spec ≥2 owning-spec arity (`SCENARIO_PATH_CROSS_SPEC_PREFIX` [FIXED] added). L: FR-TS-070 format-version check hoisted before field interpretation; ReadOnlyCollection wrappers on manifest lists (castable-array seam, parallels #18 AR-1 L-3); InvariantCulture detail strings; T-AM-115 position-unchanged restored to exact equality (migration had silently weakened it to Vector2's approximate ==); ScenarioIndexEntry split to its own file per FILE NAMING precedent; IScenario KD-7 wording clarified as implementation obligation. ScenarioRunnerTests 12→18. src/CLAUDE.md v1.58; file-manifest reconciled.) Prior June 10, 2026 (Stage 0 closed-loop scenario harness: Spec #19 §3.3.3 `ScenarioRunner` implemented now rather than at Stage 0+1, motivated by the third consecutive spec — Ball Physics AR-7, Agent Movement AR-12/AR-13 — where H/M-class closed-loop defects were *encoded by* pure-function unit suites rather than caught by them; per-function tests verify the spec as written, only a closed-loop run verifies the spec as composed. Contract honored: single entry point `Run(manifestPath, seed)`; manifest as sole input (in-memory Appendix A.1 manifests — the on-disk `index.<ext>` encoding remains D1-pinned at Stage 0+1, so the index is injected as an immutable in-code value and the Stage 0+1 file loader is a parser swap); KD-7 verbatim seeding of `DeterministicRngService` before any subsystem init; refusal of unindexed scenarios (FR-TS-028) and unknown `format_version` (FR-TS-070) as load-time `ArgumentException`s per §3.3.4; implicit pass forbidden — zero recorded envelope predicates ⇒ Failed (FR-TS-030). New: 9 harness files + Tests in `src/testing-strategy/` (ScenarioStatus/Result/Manifest/Envelope/Context/IScenario/ClosedLoopScenario/ScenarioIndex/ScenarioRunner; ScenarioRunnerTests.cs 12 contract tests); `TestingStrategyConstants.cs` v1.3 (`SCENARIO_MANIFEST_FORMAT_VERSION`). First fixture corpus: T-AM-110..115 migrated from `AgentMovementTests.cs` (v2.3) to `AgentMovementScenarios.cs` (bodies + A.1 manifests) + `AgentMovementScenarioTests.cs` — the project's first Simulation-layer tests (`sim_<scenario>` per #19 §3.1.4); requirement IDs and assertion substance unchanged; `agent-movement-tests.asmdef` gains the testing-strategy reference; `test-plan.md` v0.4; `file-manifest.md` gains a per-file `src/testing-strategy/` section. src/CLAUDE.md v1.57. Prior June 9, 2026 (Agent Movement #2 AR-12 fix pass: 3H+1M+3L, then AR-13 sweep 2M (both resolved). The three H findings were closed-loop speed-control defects invisible to pure-function/mid-flight tests: H-1 agents at rest could never start moving (IDLE branch only decayed speed while EvaluateFromIdle required speed > IdleExit — IDLE now accelerates toward the command-capped topSpeed on moving intent); H-2 commandSpeed never capped the speed-integration target (jog commands auto-promoted to SPRINTING and drained the reservoir; walk commands flapped WALKING→JOGGING→DECELERATING — Step 4–5 now applies topSpeed = min(topSpeed, commandSpeed) and ApplyAcceleration gains an asymptote ceiling); H-3 Zeno deceleration (per-frame a = v²/(2d) against the fixed total d → ~78 s / ~32 m to stop from 6 m/s; new MinDecelerationFloor [GT] bounds the tail; §3.2.5 constant-rate spec-deviation note filed). M-1 LeanAngle now reflects velocity-direction path curvature, not facing rotation. AR-13: exhausted agents (AerobicPool < AerobicJogFloor) with jog commands clamp commandSpeed to JogEnter (kills a ~3 Hz aerobic-gate flap); IDLE launch additionally gated on a non-degenerate target offset so the Decision Tree HOLD shape (StrafeWhileWatching at own position) keeps resting agents at rest. New MovementCommand.WalkTo factory; closed-loop regression fixture T-AM-110..115 + decel-floor units T-AM-108..109. Files: AgentMovementSystem.cs v1.15, AgentLocomotion.cs v1.5, AgentDirectionalMovement.cs v1.7, AgentMovementConstants.cs v1.9, AgentState.cs v1.5, GroundedReason.cs v1.1, MovementCommand.cs v1.3, tests v2.2/v1.1, test-plan.md v0.3. Prior June 9, 2026 (Ball Physics #1 AR-7 fix pass: 2H+4M+3L, then AR-8 sweep 2L (resolved) — clean. H-1/ERR-001-001: bounce ground normal was Unity Y-up `Vector3.up` in the Z-up coordinate system — a falling ball never rebounded; fixed in `BallGroundInteraction.cs` AND in the §3.1.8.1 spec pseudocode that sourced it. H-2: ValidatePhysicsState ground clamp zeroed Velocity.z before the state machine could see vz<0, trapping fast descents in a permanent Airborne ground-hover; Airborne now keeps vz through the clamp. M-1/ERR-001-002: friction stick impulse gains the 1+m·r²/I=2.5 coupling divisor. M-2..M-4 + L: test spin-sign convention fixed, gravity added to the Bouncing branch, LongBall test windows re-derived from the model (verified numerically), magic literals catalogued, MomentOfInertia retagged [DERIVED], ERR-001-003 [EST] inventory filed. Prior June 8, 2026 (Pass Mechanics #5 AR-8 fix pass: 0M + 3L. L-1: AR-7's CrossSubType-ignore warning brace-add left an empty `if (cond) { }` in production builds; gate hoisted to wrap the entire if-statement since the diagnostic has no functional follow-up (the other 7 AR-7-gated emits MUST keep the body-gate form because their if-bodies contain `_lastResult` + `return`). L-2: `ExecuteContact` state transition (`_state = FollowThrough`) hoisted above Step 8 `EventBusStub.Publish` — if Publish throws, the ball was already kicked at Step 6 and the executor must not stay in `Contact` (re-entry would re-run `ApplyKick`); the FM-08 possession recheck currently guards against the double-kick, but defensive ordering removes the dependence on the recovery seam. L-3: forward-reference notes inserted next to the AR-2 M-2 v1.6 / v1.3 history rows in `PassExecutor.cs` and `PassTargetResolver.cs` — the "[-1, +1]" characterisation there is the AR-2-era contract, superseded by AR-6 L-1 to "[-1, +1)"; historical rows preserved verbatim. Files: `PassExecutor.cs` v1.11, `PassTargetResolver.cs` v1.7. AR-8 sweep clean — no further high or medium issues. Prior June 8, 2026 (Pass Mechanics #5 AR-7 fix pass: 1M+3L all resolved on a fresh-eyes full-surface sweep over all 24 files in `src/pass-mechanics/`. M-1: FR-CS-031 gating drift fixed across sibling files — `PassMechanicsConstants` v1.2 (AR-2 L-13) gated its FM-01 `Debug.LogError` emits but the parallel cold-path emits in `PassExecutor.cs` (8 emits), `PassTypeProfiles.cs` (2 emits), and `PassVelocityCalculator.cs` (2 emits) never got the same `#if UNITY_EDITOR || DEVELOPMENT_BUILD` gating. All 12 emits now gated. L-1: `[-1, +1]` → `[-1, +1)` propagation from AR-6 producer-side correction to the two consumer-side surfaces — `PassTargetResolver.ApplyErrorToDirection` `<summary>` and `<param>` for `errorDirectionFraction`, plus the `PassExecutor.ExecuteContact` Step 3 callsite comment. L-2: `EventBusRegistrar.cs` v1.3 history row's "no `InternalsVisibleTo` on this assembly" rationale was already stale at AR-3 time — `AssemblyInfo.cs` created 2026-06-01 with `[InternalsVisibleTo("TacticalDirector.PassMechanics.Tests")]`; corrected to the boundary-mocking rationale alone. L-3: `CrossSubType` and `PassType` enums gained the ORDINAL STABILITY paragraph parallel to `CancelReason` v1.4. `PassType` carries a stronger contract — beyond being embedded in both `PassAttemptEvent` (0x0C) and `PassCancelledEvent` (0x0D) payloads, `(int)_request.PassType` is the third hash input to `ComputeErrorDirection`, so reordering would break deterministic error-direction parity even before the event digest catches the drift. Files: `PassExecutor.cs` v1.10, `PassTypeProfiles.cs` v1.4, `PassVelocityCalculator.cs` v1.4, `PassTargetResolver.cs` v1.6, `EventBusRegistrar.cs` v1.4, `CrossSubType.cs` v1.1, `PassType.cs` v1.2. AR-7 sweep clean. Prior June 8, 2026 (Pass Mechanics #5 AR-6 fix pass: 1M+3L all resolved — converts the AR-5 cycle-stop. M-1 finished what AR-5 started: input-mix primes for `frameNumber` / `passTypeIndex` in `PassErrorCalculator.ComputeErrorDirection` replaced with xxHash64 PRIME64_3 (`0x165667B19E3779F9`) and PRIME64_5 (`0x27D4EB2F165667C5`) so the Stafford Mix13 finalizer no longer multiplies through the same primes the input-mix already used (`0xBF58476D1CE4E5B9` and `0x94D049BB133111EB` remain as finalizer multipliers only). Input-mix primes are now disjoint from finalizer primes on all three axes, completing the AR-5 M-1 invariant. L-1: `<returns>` upper bound corrected `[-1, +1]` → `[-1, +1)` to match the 24-bit mantissa quantisation (EC-010 already enforces `Assert.Less(dir, 1.0f)`). L-2: comment block rewritten to call out the additive-vs-XOR asymmetry on `agentId` (AR-5 intent) and record the AR-6 input-mix/finalizer prime disjointness invariant. L-3: bit-extraction literals `0x00FFFFFFu` / `0x01000000u` promoted to named local consts `Mantissa24Mask` / `Mantissa24Scale` with a comment noting the 24-bit window matches float mantissa precision. Files: `src/pass-mechanics/PassErrorCalculator.cs` v1.7. Closes the long-standing cycle-stop carve-out. Prior June 8, 2026 (cross-spec routing close-out: `Possession.ControlHeight` ↔ `GroundControlHeight` resolved — Ball Physics #1 §3.1.11 is the authority, First Touch #4 `GroundControlHeight` is now a `[CROSS]` mirror; sibling-hazard sweep (`ControlRadius` / `ControlVelocity` / `ChallengeRadius`) returns no other parallel declarations. Prior June 7, 2026 (AR-hardening sweep complete: every coded section's last adversarial round now yields no findings or L-only — except Pass Mechanics #5 AR-5 (1M+3L) which carried an explicit "cycle stop" (converted to AR-6 above on June 8, 2026). Final AR by spec: #1 AR-8 (2L resolved; AR-7 2H+4M+3L fixed June 9, 2026) ✓; #2 AR-11 (2L) ✓; #3 AR-6 (3L) ✓; #4 AR-6 (3L) ✓; #5 AR-10 (2L, no functional findings; AR-9 1H+3M+5L fixed June 11, 2026) ✓; #6 AR-4 (3L) ✓; #7 AR-2 (3L) ✓; #8 AR-2 (clean) ✓; #10 AR-2 (clean) ✓; #11 AR-3 (clean) ✓; #12 AR-3 (clean) ✓; #13 AR-2 (clean) ✓; #14 AR-2 (clean) ✓; #15 AR-3 (clean) ✓; #16 AR-3 (1L) ✓; #17 AR-11 (no findings) ✓; #18 AR-4 (2L) ✓; #19 AR-5 (2L) + PR #132 Codex P2 follow-up ✓. Significant test scaffolding landed: Ball Physics enum-ordinal-stability + body-part-coefficients + surface-properties tests; Agent Movement T-AM-001..107 regression + unit roster (18 + 59 NUnit tests across 11 fixtures); `docs/specs/agent-movement/test-plan.md` v0.2. The PR #132 Codex P2 follow-up to `PerfGateRunner.Run` rejects mismatched perf-baseline pairs via `ArgumentException` before delegating to `RegressionGate.Evaluate` — FR-PO-031 requires same scenario, seed, platform pin, and loop; runner validates `baseline.Loop == current.Loop` unconditionally and `ScenarioManifestId` / `Seed` / `PlatformPin` when both records carry a non-null `SessionManifest`. Stage 0 host platform pin landed same day in `docs/tracking/certification-platform.md` v1.1: Windows 11 / Unity 2022.3.62f1 / Mono / x64 / SSE4.2 / 1 worker / DAZ+FTZ+fp-contract+FMA all off. Closes the long-standing OPEN ISSUE; unblocks FR-DS-009-GATE Stage 0 activation, FR-PO-052 perf-gate, #19 §7.5 D1 test-runner pin, #18 §3.9.4 warmup-measurement path, and #16 §4.8 EnvironmentFingerprint digest semantics.)
