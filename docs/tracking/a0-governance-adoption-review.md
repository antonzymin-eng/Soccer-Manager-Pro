# A0 — Governance adoption review record

> **Created:** August 31, 2026
> **Purpose:** The review-level evidence for stage A0 of
> [`docs/planning/project-architecture-governance-integration-plan.md`](../planning/project-architecture-governance-integration-plan.md).
> Governance FR-AG-018 requires a fresh review over the current artifact before final convergence.
> Appendix B of the Governance specification is a *finding*-record template; it does not by itself
> evidence that a review occurred. This file supplies the review-level record — subject identity and
> digest, scope, method, rounds, and outcome — and carries each finding in the Appendix B field set.
> **Owning plan:** integration plan §11 A0.
> **This review itself does not approve anything.** Approval is a human act. The project owner's August 31, 2026 approval and A0 closure are recorded in §6.

---

## 1. Review subject

| Field | Value |
|---|---|
| Artifact | `docs/planning/project-architecture-governance.md` |
| Version at review open | 0.4 |
| Version at latest revision of this record | 0.10 — hostile-review follow-up fixed; owner-approved and authoritative under closed A0 |
| Status during review | `Draft`; changed to `Approved` only after the fresh review converged and the project owner signed off |
| Blob digest, v0.4 (as reviewed, round 1) | `f00032cf2f16971ffbef51f6bbe307fac51a31d3` |
| SHA-256, v0.4 | `412c38eceba7a00d67ee7eb7631863bd550e29fed3e033fef75c55e7690ba316` |
| Blob digest, v0.5 (as reviewed, round 2) | `f5d0c487f14c525fa75f038cb3254c3e4bdd9417` |
| SHA-256, v0.5 | `14b940f29a4fdac867ae329ce02bf21fa257ec408c1b676bbfacd545ea22bfde` |
| Blob digest, v0.6 (as reviewed, round 3) | `e8ebad7443f8484df4355bc2355a6988df2570bb` |
| SHA-256, v0.6 | `3d66c5bd8cc87901bbdf07245ca9a7d1e3e686989641b85731cc2753ff4dadb2` |
| Blob digest, v0.7 (as reviewed, round 4) | `f32649a66f01db4606c7212c1a3c93ecf5e089f3` |
| SHA-256, v0.7 | `6364689fd4c436bbf9787d259206f2c9ebb25d7d51e79eb231862d07b7223dc0` |
| Blob digest, v0.8 (as reviewed, round 5) | `929815352f497d7e21e0e6eb063025f89acaf576` |
| SHA-256, v0.8 | `224c9aff822258bf139f1d089c442d1b0625b39d9bde81ab102ce9ad9555cd5b` |
| Blob digest, v0.9 (fresh post-remediation review) | `bd709b2eccd765a319c861d6d9081f30a552922f` |
| SHA-256, v0.9 | `b25f4d4ea138ab20854d1f2d2340c0ed0e9c81ea13cac4c55e9b3581f94c671e` |
| Blob digest, v0.10 (fresh hostile-review closure) | `ad261addb906e5ca5e8d8c4b39711abe93e7774d` |
| SHA-256, v0.10 | `f40be7aef9c505677f1120e366246a82c3c542db0a742e5a34ab981a55e26bf0` |

⚠️ **The round-5 pause is historical.** At owner direction, it was followed by one systematic
consistency-remediation pass rather than another point-fix round. That pass produced Governance v0.9,
explicitly dispositioned and resolved round 5's eight findings, and then performed the fresh full
adoption review recorded in §4.4. At that point the document remained `Draft`; convergence was not
approval. The later owner approval and approved-file digest are recorded in §6.

**These digests are review-subject identities, not the adoption pin.** The A0 adoption digest is
computed *after* `Status: Draft` → `Approved` is written, over that resulting file, and recorded in
the integration plan. See §6 and integration plan §11 A0 condition 5.

---

## 2. Scope

**In scope:** Governance §9.1 through §9.6 — 52 of the 59 checklist boxes — plus an adversarial
search for defects anywhere in the document.

**Explicitly out of scope: Governance §9.7 (7 boxes).** §9.7 is headed *"Before this specification is
considered **fully adopted**"*, while the §9 preamble gates becoming *"authoritative"*. Those are two
different bars. §9.7 asks for the Spec #19 and #20 amendments, the Master Development Plan pointer,
adversarial-review reconciliation, the property registry, finding-schema tooling, and repository
inventory tooling — all of which the integration plan assigns to stages A3–A9, which cannot start
until Governance has authority. Requiring §9.7 at A0 is circular. That circularity was a defect in
the *plan*, not the Governance document, and was fixed in plan v0.7.

**§9.7 status, recorded for completeness:** all seven remain unlanded, correctly unticked. Verified
by search — no `property-registry*` or `*architecture-governance*` file exists outside the two
planning documents; `docs/specs/testing-strategy/` and `docs/specs/code-standards/` contain zero
references to the Governance specification; `docs/planning/master-development-plan.md` contains no
pointer. `tools/assembly-tier-check.py` is the one partial: it is identified as applicable to the
dependency-direction slice of FR-AG-026, but only inside the still-Draft integration plan.

---

## 3. Method

Each checklist box was checked against the document's own text and required to resolve to a cited
line range. A box asserting that something is "defined" passes only where the definition was located
and quoted; inference was not accepted as evidence. This follows the root `CLAUDE.md` rule that
approval-checklist evidence is never fabricated and is verified against source files.

Reading was delegated across five independent passes, each given the document cold. Findings were
then verified by the orchestrating agent against the cited line ranges before being accepted.

**Honest statement of verification depth.** Roughly 30 of the cited line ranges were independently
re-read and confirmed by the orchestrator, chosen for being load-bearing or contested; the remainder
rest on a single reader's citation. Every citation in §4 below names a line range, so any claim here
is cheap for a later reviewer to re-check. Two readers' conclusions were overturned on verification
(§5), which is the evidence that the verification step was real rather than nominal.

Every round after the first is a fresh review over the *amended* artifact rather than a re-read of
the previous round's output, because FR-AG-018 requires convergence to include a fresh review over
the current artifact. Rounds 1–5 are complete over v0.4 through v0.8. Round 5 was paused before its
findings were fixed.

The owner then directed one systematic consistency-remediation pass, documented in
[`a0-governance-consistency-audit.md`](a0-governance-consistency-audit.md): four disposition values,
one canonical runtime-bearing term, an exhaustive 47-row FR-to-elaboration modality matrix, and a
schema/template/transition comparison. The fresh full review over v0.9 follows that remediation; it
is not an incremental “round 6” point-fix cycle.

**Convergence rule applied.** The fresh review checks the actual FR-AG-015–020 conditions. Its
zero-finding result is recorded in §4.4; no forecast was used to reach it.

**What the rounds have actually demonstrated.** Round 2 found a High inside the passage round 1 had
just amended. Round 3 then found that round 2's own fix was incompletely propagated — §4.2's enum was
extended while §4.1's lifecycle line was not — and that two claims round 2 wrote into the document's
version history about itself were false. Each round has found defects introduced or missed by the
previous one. That is the argument for not treating the fresh-review condition as a formality, and
for not predicting a round's outcome before it reports.

---

## 4. Historical verification record and current closure — §9.1 to §9.6

**Historical state through round 5 / Governance v0.8:** 46 of 52 boxes were verified; 6 were not
self-verifiable and were to be discharged by this record itself. §§4.1–4.3 preserve that pre-remediation
state. They are not the current A0 verdict; the current closure is §4.5.

⚠️ **The line numbers in the table below were correct at the version each box was verified against
(v0.4 for §9.1–§9.6, re-confirmed at v0.6), and have since drifted** — the document has grown by
roughly forty lines across rounds 2–4. **The section and requirement identifiers are the durable
anchors; treat the line numbers as approximate.** They are recorded rather than deleted because they
show which passage was actually read, and they are not re-derived at every round because doing so
each time would cost more than it is worth for a citation whose §-anchor is stable. A later reviewer
re-verifying these boxes should locate by §-id and FR-id.

| Section | Boxes | Verified | Evidence — §-anchors durable, line numbers as at v0.4/v0.6 |
|---|---|---|---|
| 9.1 Authority | 5 | 5 | Matrix 89–108; §8.3 1077–1084; §8.4 1087–1104 |
| 9.2 Property governance | 10 | 10 | §3.1 400–406; §3.2 421–483; §3.3 487–508; §3.4 511–529; §7.1 939–957; §7.3 973–984; §7.4 987–994; §7.5 997–1008; §7.6 1011–1038 |
| 9.3 Finding governance | 8 | 8 | §4.2 561–576; §4.3 579–588; §4.4 592–606; §4.5 610–623; §4.6 627–635; KD-AG-2 122–128; FR-AG-011 218–224; FR-AG-016/017 246–252 |
| 9.4 Proof | 15 | 15 | §5.2 656–674; §5.3 677–701; §5.4 704–720; §5.5 724–746; §5.6 746–780; §5.7 787–804; §6.6 907–933; AC-8 468–483; FR-AG-026–032A 294–326; FR-AG-036B 352–356; FR-AG-040B/C/D 382–392 |
| 9.5 Agentic development | 7 | 7 | §6.1 819–838; §6.2 841–856; §6.3 859–868; §6.4 871–891; §6.5 894–903; FR-AG-033/034 331–337 |
| 9.6 Review termination | 7 | 1 | FR-AG-019/020 258–264 (box 7 only) |

### 4.1 The §9.1 result, stated carefully

Boxes 1–3 were initially reported FAIL on the reasoning that the Authority Matrix names Spec #19 and
#20 as owners of failure injection, mutation, integration ownership and reachability, while §8.1/§8.2
concede those specs do not yet contain that material — so the rules currently live only here.

**That reasoning was rejected on verification, and the boxes pass.** Two reasons:

1. *Unlanded is not duplicated.* Whether an owner has yet written a rule down is a different question
   from whether this document duplicates their detail. The first is sequencing; only the second is
   what boxes 2–3 assert.
2. *Authority and enforcement are different columns.* §1.4 is the **Authority Matrix**, column
   *Authoritative Owner*. §8.5 is **FR-to-Enforcement Traceability**, column *Enforcement Owner*.
   FR-AG-021–025 being normatively owned here while #20 owns their enforcement is the design, not
   dual ownership.

On the substance, nothing in §2.4, §2.5 or §5.3–5.7 prescribes a test framework, coverage threshold,
CI mechanic or code pattern — the things §1.3 (lines 68–82) actually reserves. §5.6 line 766 goes out
of its way to disclaim mutation-score maximization, which is the tell that implementation detail was
deliberately kept out.

### 4.2 Historical §9.6 state at round 5

Boxes 1–6 assert that a review *occurred*: applicable properties identified, MUST-level properties
satisfied, required proof complete, no Blockers open, every finding dispositioned, fresh final review
completed. No amount of re-reading the document settles these. **This record is the external evidence
that discharges them**, as follows:

| Box | Discharged by |
|---|---|
| Applicable admitted properties identified | §4.3 below — none admitted; the registry is an A6 artifact and no property has been admitted through it, so the applicable set is empty and the obligation is vacuous, not skipped |
| Every MUST-level property satisfied | Vacuous for the same reason. The document's own FR-AG requirements were verified in §4 above; those are requirements, not admitted properties |
| Required proof complete | §5.2's Trigger Matrix maps proof obligations to *code* change types. This artifact is a document, and no row applies. Recorded rather than silently passed |
| No Blockers open | ⚠️ **Discharge weakened at round 5.** Fourteen findings dispositioned, none a Blocker; round 5's eight are **assessed** no-Blocker by the reviewer and by my own reading against §4.3's six conditions, but not formally dispositioned. See §5.2 |
| Every finding dispositioned | ⚠️ **DISCHARGE WITHDRAWN at round 5.** Fourteen carry an explicit disposition; round 5's eight do not. "Open" is a Status, not a disposition (FR-AG-009). See §5.2 |
| Fresh final review completed | **Not yet.** Five rounds complete, none clean. Round 5 over v0.8 returned one High and seven Medium/Low. This box stays unticked until a round returns only Low findings or none, per FR-AG-018 |

### 4.3 A recorded limitation

The first two boxes are discharged as *vacuous*, not as *satisfied by evidence*. No architectural
property has been admitted anywhere in this repository, because the admission machinery is itself
downstream work. This is an honest reading of an empty set, but it means those two boxes carry no
assurance. They will acquire real content the first time a property is admitted, at which point this
review's conclusion on them does not transfer. Recorded here rather than left implicit.

### 4.4 Fresh full post-remediation review — Governance v0.9

⚠️ **Historical result, superseded by §4.5.** The two shorthand row labels `No Blockers open` and
`Every finding dispositioned` below were stale v0.8 wording when this v0.9 result was written. The
v0.9 checklist labels were `No finding with Disposition Blocker remains Status Open` and `Every
substantive finding has exactly one valid Disposition`. The stale labels are retained here as
historical evidence rather than silently rewritten. A hostile follow-up over this exact v0.9 artifact found
AG-A0-023 through AG-A0-025. The zero-new-findings conclusion below is retained as the published
review result, not treated as the current conclusion.

**Result as recorded at v0.9: 52 of 52 A0-scope boxes verified; no new findings.** The review re-read the whole current
Governance artifact rather than only Round 5's cited passages. It also re-ran the mechanical inventory
recorded in `a0-governance-consistency-audit.md`: 47 FR rows; matching four-value Disposition and
five-value Status sets; exact property/finding template field order; six matching property transitions;
separate static, alternate, and bypass proof dependencies; no live noncanonical normative runtime
term; and no unresolved numeric-section reference.

| Section | Boxes | Current result | Evidence |
|---|---:|---:|---|
| 9.1 Authority | 5 | 5 | §1.4; §8.1–§8.5; no owner overlap introduced. |
| 9.2 Property governance | 10 | 10 | §3.1–§3.5; §7.1–§7.6; Appendix A. |
| 9.3 Finding governance | 8 | 8 | KD-AG-2; FR-AG-009–014; §4.1–§4.6; Appendix B and F. |
| 9.4 Proof | 15 | 15 | FR-AG-026–032A and 040B–040D; §5.2–§5.7; §6.6; Appendix D. |
| 9.5 Agentic development | 7 | 7 | FR-AG-033–036B; §3.2 AC-8; §6.1–§6.6; Appendix E. |
| 9.6 Review termination | 7 | 7 | FR-AG-015–020; §4.7; §5.3 below; this fresh review. |

The six review-state boxes discharge as follows:

| Box | Current discharge |
|---|---|
| Applicable admitted properties identified | No property has been admitted; the applicable set remains empty and the result is vacuous, as §4.3 records. |
| Every MUST-level property satisfied | Vacuous for the same empty admitted-property set. |
| Required proof complete | The §5.2 Trigger Matrix applies to code/change classes; this document-only remediation triggers none. |
| No Blockers open | Every recorded corrective finding now has `Disposition: Blocker` and `Status: Resolved`; none has the `Blocker`/`Open` combination defined by §4.7. |
| Every finding dispositioned | The Round 5 records are complete in §5.3. Historical records are schema-completed there without inferring tradeoff/risk approval. |
| Fresh final review completed | This whole-artifact v0.9 review, performed after the systematic remediation, found no new finding. |

The High severity of AG-A0-015 was **not** treated as a reason by itself to choose `Blocker`. The
current `Blocker` disposition records the A0 corrective route: its self-governance contradiction had
to be corrected before the candidate could satisfy the authorized Governance §9 / integration-plan §11
A0 gate. Severity and Disposition remain independent.

---

### 4.5 Fresh full hostile-review closure — Governance v0.10

**Result: 52 of 52 A0-scope boxes verified; zero new findings after AG-A0-023–025 were fixed.** This
review re-read the full current Governance artifact after the three hostile-review repairs and reran
the same mechanical inventories used by the systematic audit.

The closure specifically verified the three previously missed failure modes:

- for each Disposition, only `Open` or its mapped terminal Status is legal, and every substantive finding
  must be terminal before convergence;
- FR-AG-026 excludes a surface only when it is explicitly **within** recorded Non-scope or covered by a
  §7.1 exception; and
- a pre-adoption/review gate can ground a Blocker only under the §1.6 authorization contract and cannot
  be invented or self-authorized by the current reviewer.

The 47 FR headings remain unique; the four-Disposition/five-Status sets remain aligned; Appendix B/F
and integration-plan §8 carry the same transition semantics; all §9.1–§9.6 boxes remain satisfied.
No additional inconsistency was found in the current v0.10 artifact.

The current §9.6 discharge is:

| Exact v0.10 §9.6 box | Current discharge |
|---|---|
| Applicable admitted properties identified | No property has been admitted; the applicable set is empty and the result remains vacuous, not skipped. |
| Every MUST-level property satisfied | Vacuous for the same empty admitted-property set; Governance's own FR-AG requirements were separately checked as document requirements. |
| Required proof complete | This documentation-only change triggers no §5.2 code/change proof row. |
| No finding with Disposition `Blocker` remains Status `Open` | Every recorded corrective Governance finding is `Blocker / Resolved`. |
| Every substantive finding has exactly one valid Disposition and is in its §4.1-mapped terminal Status | Every recorded corrective Governance finding has exactly one Disposition and its mapped terminal Status. |
| Fresh final review completed | This v0.10 whole-artifact review was performed after AG-A0-023–025 were fixed and returned no additional Governance finding. |
| Round-budget exhaustion produces NON-CONVERGED, not APPROVED | Verified directly in FR-AG-019/020 and §4.7; no round-budget shortcut was used. |

**Reviewer-independence limitation:** the v0.10 closure review was performed by the same assistant that
applied AG-A0-023–025. FR-AG-018 requires a fresh review over the current artifact; it does not require
a different reviewer. No independence is claimed here. Human sign-off was a separate A0 condition and is recorded as completed in §6.

---

## 5. Findings

Twenty-five findings were recorded across five historical rounds, the systematic remediation, and one hostile follow-up. The Round 5 entries and the
previously compressed Round 3–4 entries are schema-completed in §5.3 so every live record can be read
under Governance v0.9's Appendix B field set. `Resolved` is recorded as Status, never Disposition.

The following table preserves the original short historical summaries for Rounds 3–4. Its historical
outcome text is not the authoritative current schema record; §5.3 supplies the missing fields.

| ID | Round introduced | Severity | Summary | Historical outcome / evidence |
|---|---|---|---|---|
| AG-A0-005 | 3 | Medium | §4.1's finding-lifecycle line still listed three terminal states after v0.6 extended §4.2's enum to five — AG-A0-003's defect one section over, and the reason v0.6's "extended to match §4.1" was false | Resolved. §4.1 now lists all four terminal states and maps each to its disposition; the false claim is annotated in the v0.6 row, not deleted |
| AG-A0-006 | 3 | Medium | The v0.6 version-history row attributed the Blocker trigger to FR-AG-011, which requires only that a Blocker *cite* an authority | Resolved. The rule is §4.3 item 5; the row is corrected in place with a ⚠️ annotation |
| AG-A0-007 | 3 | Medium | FR-AG-026's "unless an approved exclusion exists" named no mechanism — a MUST-level rule with an undefined escape hatch | Resolved. Closed to exactly two recorded artifacts: a property's §3.3 Non-scope, or a §7.1 exception. Prose assertion explicitly excluded |
| AG-A0-008 | 3 | Low | §5.4 stated its evidence list with no modal verb while §5.3 and the fixed §5.5 anchor theirs to a MUST | Resolved. Anchored to FR-AG-028 in the same shape |
| AG-A0-009 | 3 | Low | §7.1's exception schema still mandated the bare `AP-###` that FR-AG-004 calls recommended | Resolved. Hedged — the third and last site of AG-A0-004's defect |
| AG-A0-010 | 3 | Low | §4.2 and Appendix F said "Tradeoff" where FR-AG-009 and Appendix B say "Accepted Tradeoff" | Resolved. Unified |
| AG-A0-011 | 3 | Low | Appendix A paraphrases five of §3.3's field labels, working against the machine-checkable-schema ambition in §6.1 | Resolved. Appendix A now states §3.3 governs and that a schema is generated from §3.3, not the template |
| AG-A0-012 | 3 | Low | Appendix F's property summary omitted §3.1's `Rejected → Candidate` and `Superseded → Retired` edges | Resolved. Both added |
| AG-A0-013 | 4 | Medium | §9.3's checklist still read "Tradeoff defined." — the bare term AG-A0-010 replaced everywhere else. Fourth instance of the propagation class, and it sat inside §9, the self-check gate meant to catch exactly this | Resolved. **This one was a judgment call made and got wrong at the 0.7 landing:** the site was seen, classified as a checklist label rather than an enum site, and deliberately left. §9 is the approval gate; naming a disposition that exists nowhere else makes it an enum site |
| AG-A0-014 | 4 | Medium | §6.6 said verification "SHOULD be proportionate to the consequence of tool failure" while FR-AG-036A requires it as a MUST. Since §4.3 item 1 makes a violated MUST-level property a Blocker, the SHOULD left room to treat disproportionate tool verification as a mere shortfall | Resolved. §6.6 now carries the MUST anchored to FR-AG-036A, with depth framed as judgment *within* the obligation — the shape §5.4 and §5.5 already use |

### 5.2 Round 5 — historical open record at Governance v0.8

**At v0.8, Round 5's eight findings were open.** The review was paused at owner instruction
immediately after it reported, with the explicit instruction not to fix anything. They were recorded
without a disposition — and that distinction was load-bearing, not bookkeeping. Governance FR-AG-009
requires every substantive finding to end in exactly one disposition drawn from a fixed set, and
"Open" is a *Status* value, not a disposition.

**Two consequences, stated plainly rather than left implicit:**

1. **§9.6's "Every finding dispositioned" box can no longer be discharged.** §4.2 above discharged it
   on the basis that all findings to that point carried an explicit disposition. Eight now do not.
   That discharge is withdrawn until these are dispositioned.
2. **§9.6's "No Blockers open" box is discharged only on the reviewer's severity assessment**, which
   is not the same thing as a disposition. Round 5 assessed no finding as a Blocker, and finding 1's
   subject matter — an enum size mismatch — does not meet any of §4.3's six Blocker conditions on my
   reading. But no one has formally dispositioned them, so this rests on assessment, not record.

**A0 cannot close in this state.** Not because a High is open — severity does not gate approval — but
because two of the six §9.6 boxes this record exists to discharge are no longer discharged by it.

| ID | Severity | Summary | Status |
|---|---|---|---|
| AG-A0-015 | **High** | The Disposition enum contradicts itself on its own size. FR-AG-009, §4.2's Disposition row and Appendix B's checklist list **five** values ending in "Resolved"; §4.1, §4.3–§4.6 and Appendix F all treat it as **four**. There is no §4.7 and no fifth Appendix F chain. "Resolved" is legitimately a *Status* value (§4.2's Status row) that leaked into the Disposition enum at three sites. FR-AG-009 says a finding "MUST end in exactly one disposition" — and a reader cannot tell whether the universe is four values or five | Open. **Independently verified** by the orchestrator: FR-AG-009 does list five; §4.3–§4.6 define exactly four |
| AG-A0-016 | Medium | "runtime-bearing component" (FR-AG-021, Appendix C) vs "runtime component" (FR-AG-023, FR-AG-027) — four MUST-level sites, two spellings, no glossary saying whether they are synonyms or a deliberate narrow/broad distinction | Open. **Independently verified**: all four sites confirmed |
| AG-A0-017 | Low–Medium | §5.7 states FR-AG-032A's core invalidation trigger with no modal verb, while the *negative* case one line below carries an explicit MUST NOT — so the "does invalidate" half is soft prose and the "does not invalidate" half is hard. §5.7 never cites FR-AG-032A by id | Open |
| AG-A0-018 | Low | §6.6 under-anchors FR-AG-040C with no modal verb — in the very section v0.8 hardened for FR-AG-036A. Low rather than Medium because the prohibition half three lines later does carry MUST NOT | Open |
| AG-A0-019 | Low | Three softer no-modal-verb echoes in §6.2, §6.4, §6.5 against FR-AG-036, FR-AG-033, FR-AG-034. Round 5 did not recommend individual fixes; surrounding MUST-level text already carries each obligation | Open |
| AG-A0-020 | Low | Appendix B says "Round" where §4.2 says "Round introduced", and unlike Appendix A it carries no disclaimer naming the governing section | Open |
| AG-A0-021 | Low | Appendix D merges §5.7's separate "static initialization paths" and "alternate or bypass paths" bullets into one field, with no note that §5.7 governs | Open |
| AG-A0-022 | Low | Appendix C paraphrases FR-AG-023's "teardown" as "Shutdown/disposal owner" — the same unflagged-paraphrase class Appendix A was given a disclaimer for at v0.7 | Open |

### 5.3 Current schema-completion ledger

AG-A0-001 through AG-A0-004 already carry complete field records below. Their prior
`Disposition: Resolved` values are normalized to `Disposition: Blocker`, `Status: Resolved`; this is
an A0 corrective-route classification, not a severity conversion. The preceding tables provide each
remaining record's Finding ID, Round introduced, Severity, and Summary. The table below supplies its
remaining required Appendix B fields.

**The A0 gate is not a blanket Blocker citation.** Governance §9 plus integration-plan §11 A0 supplies
the authorized pre-adoption gate wrapper; each Blocker must also identify the specific §9 checklist
condition that the defect made false or non-verifiable. The `Requirement/Property` cells below carry
that row-specific linkage. A finding that cannot identify such a concrete unsatisfied gate condition
does not become a Blocker merely because it was found during A0.

| ID | Evidence | Requirement/Property | Required action | Owner | Disposition | Status | Resolution evidence |
|---|---|---|---|---|---|---|---|
| AG-A0-005 | §4.1 omitted two terminal mappings that §4.2/Appendix F required. | §9.3 `Finding record schema defined` + `Review termination requires complete dispositions`; §4.1; §4.2; Appendix F | Fix | A0 review | Blocker | Resolved | Governance v0.7 completed the mappings; v0.9 retains their exact Status semantics. |
| AG-A0-006 | The v0.6 history named FR-AG-011 instead of §4.3 item 5 for the Blocker trigger. | §9.3 `Requirement linkage mandatory for blockers`; §4.3 item 5 | Fix | A0 review | Blocker | Resolved | v0.7 annotates the false historical attribution rather than deleting it. |
| AG-A0-007 | FR-AG-026's approved-exclusion route was unspecified. | §9.4 `Repository-wide inventory requirement present`; FR-AG-026; §3.3; §7.1 | Fix | A0 review | Blocker | Resolved | v0.7 restricts it to Non-scope or a §7.1 exception. |
| AG-A0-008 | §5.4 lacked FR-AG-028's required modality. | §9.4 `Lifecycle/order defined`; FR-AG-028; §5.4 | Fix | A0 review | Blocker | Resolved | v0.7 added the explicit lifecycle/order obligation. |
| AG-A0-009 | §7.1 made the recommended `AP-###` form mandatory. | §9.2 `Stable property ID defined` + `Exception mechanism defined`; FR-AG-004; §7.1 | Fix | A0 review | Blocker | Resolved | v0.7 makes the form recommended. |
| AG-A0-010 | §4.2/Appendix F used bare Tradeoff against the defined term. | §9.3 `Accepted Tradeoff defined` + `Finding record schema defined`; FR-AG-009; §4.2; Appendix F | Fix | A0 review | Blocker | Resolved | v0.7 uses Accepted Tradeoff consistently. |
| AG-A0-011 | Appendix A paraphrased §3.3 field labels. | §9.2 `Admission record schema defined`; §3.3; Appendix A | Fix | A0 review | Blocker | Resolved | v0.9 replaces the remaining aliases with exact field labels. |
| AG-A0-012 | Appendix F omitted two §3.1 transitions. | §9.2 `Rejection defined` + `Supersession defined` + `Retirement defined`; §3.1; Appendix F | Fix | A0 review | Blocker | Resolved | v0.9 reproduces all six transitions as a table. |
| AG-A0-013 | §9.3 named a non-existent bare Tradeoff disposition. | §9.3 `Accepted Tradeoff defined`; FR-AG-009 | Fix | A0 review | Blocker | Resolved | v0.8 changed the checklist label to Accepted Tradeoff. |
| AG-A0-014 | §6.6 weakened FR-AG-036A from MUST to SHOULD. | §9.4 `Merge-critical governance tooling is itself verified`; FR-AG-036A; §6.6 | Fix | A0 review | Blocker | Resolved | v0.8 restores the MUST; v0.9 retains it. |
| AG-A0-015 | FR-AG-009, §4.2, and Appendix B had five Dispositions while all lifecycle treatment had four. | §9.3 `Finding record schema defined` + `Review termination requires complete dispositions`; FR-AG-009; §4.1–§4.2; Appendix B; Appendix F | Fix | A0 review | Blocker | Resolved | v0.9 settles four Dispositions and five Statuses; `Resolved` is Status only. |
| AG-A0-016 | Four MUST-level sites used two undefined runtime component terms. | §9.4 `Structural reachability defined` + `Lifecycle/order defined`; FR-AG-021; FR-AG-023; FR-AG-027; Appendix C | Fix | A0 review | Blocker | Resolved | v0.9 defines and uses runtime-bearing component canonically. |
| AG-A0-017 | §5.7 did not state FR-AG-032A's positive invalidation trigger normatively. | §9.4 `Affected proof is revalidated after material changes to that dependency surface`; FR-AG-032A; §5.7 | Fix | A0 review | Blocker | Resolved | v0.9 requires regeneration or revalidation after material dependency change. |
| AG-A0-018 | §6.6 did not state FR-AG-040C's terminal boundary normatively. | §9.4 `Governance-tool verification terminates without recursive checker chains`; FR-AG-040C; §6.6 | Fix | A0 review | Blocker | Resolved | v0.9 makes ordinary-verification termination an explicit MUST. |
| AG-A0-019 | §§6.2, 6.4, and 6.5 elided the FR-AG-036/033/034 modalities. | §9.5 `Exhaustive mechanical-work assumption explicit` + `Unsupported agent assertions prohibited` + `Judgment domain explicit`; FR-AG-033; FR-AG-034; FR-AG-036 | Fix | A0 review | Blocker | Resolved | v0.9 restores each direct governing modality. |
| AG-A0-020 | Appendix B used `Round` instead of §4.2's `Round introduced`. | §9.3 `Finding record schema defined`; §4.2; Appendix B | Fix | A0 review | Blocker | Resolved | v0.9 reproduces the exact name and field order. |
| AG-A0-021 | Appendix D merged static, alternate, and bypass dependencies. | §9.4 `Reusable proof declares a precise dependency surface`; §5.7; Appendix D | Fix | A0 review | Blocker | Resolved | v0.9 keeps all three categories separate. |
| AG-A0-022 | Appendix C used Shutdown/disposal rather than the mandated teardown term. | §9.4 `Lifecycle/order defined`; FR-AG-023; Appendix C | Fix | A0 review | Blocker | Resolved | v0.9 uses `Teardown owner` exactly. |

**Two judgment calls round 5 made that I checked and agree with**, recorded because a later reviewer
will otherwise re-raise them: FR-AG-012's heading "Tradeoff integrity" is **not** a propagation site
— FR-AG-010, FR-AG-013 and FR-AG-014 all use short glosses rather than the full defined term, so
flagging it would demand flagging them. And KD-AG-2's lowercase "accepted tradeoff" is prose style,
matching §1.1 and §8.4, not the term-drift pattern.

**What round 5 confirmed rather than found:** both v0.8 changes landed correctly; all eight v0.7
fixes verified against live text; the twelve claims the v0.7 version-history row makes about the
document all hold; every `§` and `FR-AG-###` cross-reference resolves, including the §8.5 range
table; no unsatisfiable or circular MUST rule.

---

The four findings from rounds 1 and 2 follow in full Appendix B form.

### AG-A0-001

- **Round introduced:** 1
- **Severity:** Low
- **Summary:** §5.5 was the only place in the document that addressed test authoring directly rather
  than proof scope, wording §1.3 reserves to Spec #19.
- **Evidence:** §5.5 line 730 read *"Where meaningful, tests SHOULD intentionally cause:"*. Every
  sibling subsection (§5.3, §5.4, §5.6) phrases its obligation as what the *proof* or *evidence* must
  do. §1.3 line 73 disclaims "detailed test framework implementation".
- **Requirement/Property:** §9.1 `No detailed #19 responsibility is duplicated here`; §1.3; §1.4 line 102
- **Disposition:** Blocker
- **Required action:** Fix
- **Owner:** A0 review
- **Status:** Resolved
- **Resolution evidence:** Reworded in v0.5. Superseded by AG-A0-002, which reworked the same passage
  again; see below.

### AG-A0-002

- **Round introduced:** 2
- **Severity:** High
- **Summary:** §5.5 stated the failure-injection obligation as SHOULD while FR-AG-029 states it as
  MUST, gated on the identical "meaningful" condition — making a mandatory proof trigger read as
  optional.
- **Evidence:** FR-AG-029 (line 307): *"Applicable meaningful failure paths MUST be deliberately
  exercised."* §5.5 (line 730, as amended by AG-A0-001): *"Where meaningful, the proof scope SHOULD
  include deliberately causing:"*. **§4.3 item 5** (line 587) makes an unmet mandatory proof trigger
  grounds for a Blocker, and FR-AG-016 forbids convergence with Blockers open — so under the weaker
  reading FR-AG-029 could not be enforced as written. §5.3 (lines 684, 694) and §5.6 (line 753) use
  MUST for their own obligations; §5.5 was the outlier.
  *(⚠️ CORRECTED by AG-A0-006 — this finding as first written cited **FR-AG-011** for the Blocker
  trigger. FR-AG-011 requires only that a Blocker cite an authority; it says nothing about proof
  triggers. The misattribution came from the round-2 reader and was propagated here, into the
  Governance version history, and into the CHANGELOG before round 3 caught it. The finding itself
  stands — only its cited authority was wrong.)*
- **Requirement/Property:** §9.4 `Failure injection defined`; FR-AG-029; §4.3 item 5; FR-AG-016
- **Disposition:** Blocker
- **Required action:** Fix
- **Owner:** A0 review
- **Status:** Resolved
- **Resolution evidence:** §5.5 now opens *"Applicable meaningful failure paths MUST be deliberately
  exercised. FR-AG-029 carries this obligation; this section does not weaken it."*, and marks the nine
  failure types *"illustrative rather than exhaustive"* under the SHOULD — which is what the SHOULD was
  for. Landed in v0.6; re-reviewed in round 3.
- **Note:** AG-A0-001's fix preserved this pre-existing tension and its version-history note asserted
  "no normative obligation changed" without reconciling the two modalities. The tension predates this
  review; the unreconciled assertion did not.

### AG-A0-003

- **Round introduced:** 2
- **Severity:** Medium
- **Summary:** The §4.2 Finding Record Schema's Status enum offered no valid value for a
  Residual-Risk or Candidate-Property finding.
- **Evidence:** §4.2 line 573 read *"Open / Resolved / Accepted"*. §4.1 line 555 gives
  *"Open → Dispositioned → Resolved/Accepted/Recorded"*, and Appendix F (lines 1446, 1448) adds
  *"Open → Residual Risk → Recorded"* and *"Open → Candidate Property → Property process"*. The
  analogous Property State field (§3.3 line 495) correctly mirrors all five states from §3.1, so the
  asymmetry was an oversight rather than a design choice.
- **Requirement/Property:** §9.3 `Finding record schema defined`; §4.1; Appendix F
- **Disposition:** Blocker
- **Required action:** Fix
- **Owner:** A0 review
- **Status:** Resolved
- **Resolution evidence:** Enum extended to *"Open / Resolved / Accepted / Recorded / In property
  process"* in v0.6.

### AG-A0-004

- **Round introduced:** 2
- **Severity:** Low
- **Summary:** §3.3 stated the `AP-###` identifier format as a MUST-level schema requirement while
  FR-AG-004 calls the same format merely "Recommended".
- **Evidence:** FR-AG-004 (lines 175–178): *"Every admitted property MUST receive a stable
  identifier. Recommended form: `AP-###`."* §3.3 line 489 introduces its table with *"Every admitted
  property MUST record:"*, and line 493 read *"| Property ID | Stable `AP-###` |"* — mandating the
  exact format FR-AG-004 hedges.
- **Requirement/Property:** §9.2 `Stable property ID defined` + `Admission record schema defined`; FR-AG-004
- **Disposition:** Blocker
- **Required action:** Fix
- **Owner:** A0 review
- **Status:** Resolved
- **Resolution evidence:** Schema cell now reads *"Stable identifier; recommended form `AP-###`
  (FR-AG-004)"* in v0.6.

### 5.4 Hostile follow-up findings over Governance v0.9

| ID | Round introduced | Severity | Summary | Requirement/Property | Required action | Owner | Disposition | Status | Resolution evidence |
|---|---:|---|---|---|---|---|---|---|---|
| AG-A0-023 | Hostile follow-up | High | The four terminal mappings were descriptive but invalid Disposition/Status pairs were not prohibited, and §4.7 allowed non-Blocker findings to remain `Open` at convergence. | §9.3 `Finding record schema defined` + `Review termination requires complete dispositions`; FR-AG-017; §4.1–§4.2; §4.7; Appendix F | Fix | A0 review | Blocker | Resolved | Governance v0.10 makes only `Open` or the mapped terminal Status legal and requires every finding to be terminal before convergence; same-review Candidate admission recomputes applicability. |
| AG-A0-024 | Hostile follow-up | High | FR-AG-026 inverted its Non-scope route by saying a surface outside Non-scope was excluded, which is the in-scope side of the boundary. | §9.4 `Repository-wide inventory requirement present`; FR-AG-026; §3.3; §5.3; §7.1 | Fix | A0 review | Blocker | Resolved | Governance v0.10 states that an excluded surface must be explicitly included within recorded Non-scope or covered by a §7.1 exception. |
| AG-A0-025 | Hostile follow-up | Medium | The newly added review-gate Blocker basis did not define who authorizes a gate, when authorization exists, or how retroactive reviewer-created gates are prevented. | §9.3 `Requirement linkage mandatory for blockers`; FR-AG-011; §1.6; §4.3 | Fix | A0 review | Blocker | Resolved | Governance v0.10 requires a durable pre-existing gate record authorized by the project lead/owner or existing governing authority, scoped to the artifact with a closure condition, and prohibits reviewer self-authorization/retroactive invention. |

All three were classified Blocker because they contradicted the already-applicable A0 self-governance
and review gate, not because of severity. Their current Status is the §4.1-mapped terminal Status
`Resolved`.

### 5.5 Historical-record preservation correction

A prior revision of this record stated: *“Fourteen findings across four rounds… None is a Blocker.”*
When v1.4 normalized the historical findings to the settled Disposition/Status model, that sentence was
deleted rather than preserved and corrected. That deletion was inconsistent with this record's own
practice of annotating false historical claims instead of silently removing them.

The historical claim is therefore restored here as a correction record: **it was true only of the
pre-v0.9 provisional classification.** Under the settled A0 model, the corrective findings are Blockers
only where their `Requirement/Property` field identifies the specific §9 gate condition they made false
or non-verifiable. Severity did not drive the reclassification.
### 5.6 Reconciliation of Claude's pre-fix critique

Claude's review of the v0.9/v1.4 landing found six issues. Five required record/manifest correction and
are fixed in this revision: stale present-tense round-5 verdicts, blanket A0-gate Blocker linkage, stale
v0.8 checklist labels inside the v0.9 review, deletion rather than annotation of a reversed historical
claim, and missing Tracking Documents rows. The sixth — no independent reviewer claim on the fresh
review — is recorded in §4.5 as a limitation, not converted into a new requirement: FR-AG-018 requires
freshness, not reviewer independence.

These are defects/limitations in the A0 evidence record and repository manifest, not new defects in the
Governance v0.10 subject. The v0.10 Governance blob/SHA-256 therefore remains unchanged.
### Not filed as findings

One round-2 observation is deliberately not filed: that the v0.5 version history cited this file
before it existed. That was a same-branch sequencing artifact of splitting the work across two
commits, not a document defect, and it resolves on this file landing.

---

## 6. Outcome and what remains

**Review outcome: CONVERGED FOR A0; OWNER APPROVED; A0 CLOSED.** The systematic remediation resolved the
Round 5 and audit-exposed inconsistencies, but the subsequent hostile follow-up found AG-A0-023–025
in Governance v0.9. Governance v0.10 resolves those three findings, and the fresh full closure review
in §4.5 found no additional finding. Every recorded corrective finding is now `Disposition: Blocker`,
`Status: Resolved`; every finding is in its Disposition-mapped terminal Status. This is an A0 gate result, not a claim that a High
severity always selects `Blocker`.

| Review activity | Subject | Result |
|---|---|---|
| 1 | v0.4 | 1 Low |
| 2 | v0.5 | 1 High, 1 Medium, 1 Low — the High inside the passage round 1 had just amended |
| 3 | v0.6 | 3 Medium, 5 Low — including that round 2's fix was incompletely propagated, and that two claims round 2 wrote into the document's own version history were false |
| 4 | v0.7 | 2 Medium — a fourth propagation miss, this one a site seen and deliberately left on a judgment call that was wrong |
| 5 | v0.8 | **1 High, 1 Medium, 6 Low** — historical pause record; all eight are dispositioned and resolved in §5.3 |
| Systematic remediation | v0.8 → v0.9 | One exhaustive FR/modality/schema/terminology pass; all listed corrections applied together |
| Fresh full adoption review | v0.9 | Historical zero-finding result; superseded when hostile follow-up found AG-A0-023–025 |
| Hostile follow-up | v0.9 | 2 High, 1 Medium — all fixed in v0.10 |
| Fresh hostile-review closure | v0.10 | 52/52 A0 boxes verified; zero new findings |

**Two defect classes account for nearly all of the first twenty-two findings, and round 5 found a fifth instance
of each even after being told to sweep them exhaustively.** Incomplete propagation — a term or enum
corrected in some sites but not all: §4.1, §7.1, §9.3, the Status enum, and now the Disposition enum
itself. Modality mismatch — an FR-AG rule stating MUST while its elaborating section says SHOULD or
carries no modal verb: §5.4, §5.5, §6.6, and now §5.7 and §6.6 again against a *different* FR.

**That last point is the one worth carrying.** Round 5 was dispatched with explicit instructions to
enumerate every enum site and walk every FR-AG rule against its elaborating section — and still found
new instances of both classes, including one in §6.6, the very section hardened in v0.8 for a sibling
requirement. Five rounds of targeted fixing have not exhausted either class. That is evidence the
document needed a systematic consistency pass rather than another round of point fixes. That pass is the v0.9 remediation documented in the companion audit; the v0.10 hostile-review closure
then fixes the three semantic defects the systematic matrix itself did not detect.

**AG-A0-015 was not fixed by reflex.** The remediation settled the model explicitly: four
Dispositions select handling; five Status values represent lifecycle. That makes `Resolved` a Status,
not an unmodelled fifth Disposition, and makes `Dispositioned` non-status prose rather than a missing
enum member.

**The review did not itself approve Governance; the project owner did.** On August 31, 2026 the owner
explicitly approved Governance v0.10. The required landing sequence then completed in order:

1. Human sign-off recorded.
2. Governance `Status: Draft` → `Approved`.
3. The exact resulting approved file was hashed and its canonical adoption pin recorded in integration
   plan §11 A0.

**A0 is CLOSED.** The canonical adoption pin is owned by integration plan §11 A0; the digests in §1
remain review-subject identities and are not substituted for that pin.

A2 is the next stage. Governance §9.7 remains open and is owned by A3–A9; the
specification is authoritative at A0 but not *fully adopted* until then.

---

## Version History

| Version | Date | Author | Notes |
|---|---|---|---|
| 1.7 | August 31, 2026 | — | **A0 closure.** Records explicit project-owner approval of Governance v0.10, confirms the required landing order was followed (`Draft → Approved` first, hash second), and points to integration plan §11 A0 as the owner of the canonical approved-file adoption pin `aa1792bf143fb3bc1066176dedb33abc4097045e7d089844edf05ccf9961d8f6`. Review outcome advances from converged/not-yet-approved to **OWNER APPROVED; A0 CLOSED**. Governance semantics were not changed after review; the Governance file itself changed only in its Status field. |
| 1.6 | August 31, 2026 | — | **Claude pre-fix critique reconciliation.** Marks §§4.1–4.3 explicitly historical, preserves the stale v0.8 box labels in §4.4 with an annotation, adds the exact current v0.10 §9.6 discharge table, and records that the v0.10 fresh review was not reviewer-independent because FR-AG-018 does not require independence. Replaces the blanket A0-gate Blocker citation with row-specific §9 gate linkage for every corrective finding, restores/annotates the deleted historical `None is a Blocker` claim, and records the companion manifest-table repair. Governance v0.10 content and digests are unchanged. |
| 1.5 | August 31, 2026 | — | **Hostile-review closure.** Records AG-A0-023–025 against Governance v0.9: invalid cross-axis finding states/open non-Blocker convergence, inverted FR-AG-026 Non-scope semantics, and under-specified review-gate authorization. Governance v0.10 resolves all three, the companion audit advances to v1.1, and a fresh full v0.10 review returns zero new findings with all 52 A0-scope boxes verified. The v0.9 zero-finding review is retained but explicitly marked superseded. Outcome remains **CONVERGED FOR A0; NOT YET APPROVED** pending human sign-off. |
| 1.4 | August 31, 2026 | — | **Systematic remediation and fresh full adoption review.** Governance v0.8 → v0.9 settles four Dispositions and five Statuses, canonically defines `runtime-bearing component`, and reconciles the full FR modality/schema/template surface in one batch; the companion audit carries the 47-row matrix. Round 5's eight historical open findings are now complete `Blocker` / `Resolved` records, and the earlier compressed record fields are schema-completed without inferring tradeoff/risk approval. The A0 corrective route is explicitly grounded in the authorized Governance §9 / plan §11 pre-adoption gate, never in severity. The fresh whole-artifact v0.9 review verifies all 52 A0-scope boxes and returns zero new findings. Review outcome: **CONVERGED FOR A0; NOT YET APPROVED** — human sign-off, then the status edit and post-edit adoption digest, remain. |
| 1.3 | August 31, 2026 | — | **Round 5 recorded; review PAUSED at owner instruction with its findings deliberately unfixed.** One High, one Medium, six Low — AG-A0-015 to AG-A0-022 — bringing the total to twenty-two across five rounds, none clean. The High (AG-A0-015) is the Disposition enum disagreeing with itself on its own size: FR-AG-009, §4.2 and Appendix B list five values ending in "Resolved" while §4.1, §4.3–§4.6 and Appendix F treat it as four; independently verified at both ends. Governance stays at v0.8 with these findings open against it. **Two §9.6 discharges change as a direct result:** "Every finding dispositioned" is **withdrawn**, because "Open" is a Status and not a disposition under FR-AG-009, and "No Blockers open" is weakened to rest on severity assessment rather than formal disposition. A0 therefore cannot close in this state — not because a High is open, since severity does not gate approval, but because two of the six boxes this record exists to discharge are no longer discharged by it. Also recorded: round 5 found a fifth instance of each of the two recurring defect classes *despite* being dispatched with instructions to sweep both exhaustively, which is the argument that a systematic consistency pass is now owed rather than another round of point fixes. |
| 1.2 | August 31, 2026 | — | Round 4 recorded; Governance v0.7 → v0.8. Two Medium findings, fourteen in total, none a Blocker. **AG-A0-013** is the fourth instance of the incomplete-propagation class and the first that was seen and deliberately left: §9.3's checklist item was classified at the 0.7 landing as a label rather than an enum site, which was wrong — §9 is the approval gate, and it named a disposition that existed nowhere else. **AG-A0-014**: §6.6 stated FR-AG-036A's MUST as a SHOULD. Round 4 independently verified all eight of the 0.7 changes and all twelve claims the 0.7 version-history row makes about the document. Round 4 did not converge, so round 5 over v0.8 is dispatched, with instructions to sweep the two recurring defect classes exhaustively rather than opportunistically. Also corrected in this record: the review-subject table said the review closed at v0.6, which was false — the review is open; and §4's cited line numbers, correct when written, have drifted as the document grew, which is now stated with the §-anchors named as the durable reference. |
| 1.1 | August 31, 2026 | — | Round 3 recorded; Governance v0.6 → v0.7. Eight further findings, three Medium and five Low, none a Blocker — twelve in total. Two of them (AG-A0-005, AG-A0-006) falsify claims round 2 wrote into the Governance version history about its own fix, and one of those, the FR-AG-011 misattribution, had been propagated into this record, the Governance history and the CHANGELOG before round 3 caught it; all three sites are corrected by annotation rather than rewrite. AG-A0-007 closes FR-AG-026's previously undefined "approved exclusion" to two recorded artifacts. Round 3 did not converge — three Medium — so round 4 over v0.7 is dispatched and outstanding, and the convergence rule now applied is the repository's own: a round returning only Low findings or none. Recorded shortfall: round 3's eight findings are tabulated rather than written in full Appendix B field form, and the record says so. |
| 1.0 | August 31, 2026 | — | Initial record. Two review rounds complete over Governance v0.4 → v0.6, §9.1–§9.6 in scope; round 3 over v0.6 dispatched and outstanding at this revision. 46 of 52 boxes verified against cited line ranges; five of the six §9.6 process-state boxes discharged by this record, two of those vacuously and recorded as such, and "fresh final review completed" left unticked pending round 3. Four findings, all Resolved, none a Blocker. Outcome recorded as NOT YET CONVERGED rather than rounded up — round 2 found a High defect inside the passage round 1 had just amended, which is the whole reason FR-AG-018 requires a fresh pass over the current artifact. §9.7 confirmed unlanded and scoped out to A3–A9. Status and adoption digest deliberately not written. |
