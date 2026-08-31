# A0 — Governance adoption review record

> **Created:** August 31, 2026
> **Purpose:** The review-level evidence for stage A0 of
> [`docs/planning/project-architecture-governance-integration-plan.md`](../planning/project-architecture-governance-integration-plan.md).
> Governance FR-AG-018 requires a fresh review over the current artifact before final convergence.
> Appendix B of the Governance specification is a *finding*-record template; it does not by itself
> evidence that a review occurred. This file supplies the review-level record — subject identity and
> digest, scope, method, rounds, and outcome — and carries each finding in the Appendix B field set.
> **Owning plan:** integration plan §11 A0.
> **This review does not approve anything.** Approval is a human act; see §6.

---

## 1. Review subject

| Field | Value |
|---|---|
| Artifact | `docs/planning/project-architecture-governance.md` |
| Version at review open | 0.4 |
| Version at review close | 0.6 |
| Status throughout | `Draft` — unchanged by this review |
| Blob digest, v0.4 (as reviewed, round 1) | `f00032cf2f16971ffbef51f6bbe307fac51a31d3` |
| SHA-256, v0.4 | `412c38eceba7a00d67ee7eb7631863bd550e29fed3e033fef75c55e7690ba316` |
| Blob digest, v0.5 (as reviewed, round 2) | `f5d0c487f14c525fa75f038cb3254c3e4bdd9417` |
| SHA-256, v0.5 | `14b940f29a4fdac867ae329ce02bf21fa257ec408c1b676bbfacd545ea22bfde` |
| Blob digest, v0.6 (as reviewed, round 3) | `e8ebad7443f8484df4355bc2355a6988df2570bb` |
| SHA-256, v0.6 | `3d66c5bd8cc87901bbdf07245ca9a7d1e3e686989641b85731cc2753ff4dadb2` |
| Blob digest, v0.7 (round 4 subject) | `f32649a66f01db4606c7212c1a3c93ecf5e089f3` |
| SHA-256, v0.7 | `6364689fd4c436bbf9787d259206f2c9ebb25d7d51e79eb231862d07b7223dc0` |

⚠️ **Round 4 is dispatched but has not reported as of this revision.** Rounds 1–3 are complete.
Round 3 returned three Medium findings, so the review has not converged and a fourth round is owed;
this file records that state rather than one it cannot yet evidence. §6 states the outcome
accordingly.

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
the current artifact — and each round of fixes produces a new current artifact. Rounds 1–3 are
complete over v0.4, v0.5 and v0.6 respectively; round 4 was dispatched against v0.7 and is
outstanding at this revision.

**Convergence rule applied.** A round returning only Low findings, or none, converges. Round 3
returned three Medium, so it did not, and a fourth round is owed. This is the same rule the
repository's `adversarial-review` skill uses.

**What the rounds have actually demonstrated.** Round 2 found a High inside the passage round 1 had
just amended. Round 3 then found that round 2's own fix was incompletely propagated — §4.2's enum was
extended while §4.1's lifecycle line was not — and that two claims round 2 wrote into the document's
version history about itself were false. Each round has found defects introduced or missed by the
previous one. That is the argument for not treating the fresh-review condition as a formality, and
for not predicting a round's outcome before it reports.

---

## 4. Result — §9.1 to §9.6

**46 of 52 boxes verified. 6 are not self-verifiable and are discharged by this record itself.**

| Section | Boxes | Verified | Line-range evidence |
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

### 4.2 The six §9.6 boxes

Boxes 1–6 assert that a review *occurred*: applicable properties identified, MUST-level properties
satisfied, required proof complete, no Blockers open, every finding dispositioned, fresh final review
completed. No amount of re-reading the document settles these. **This record is the external evidence
that discharges them**, as follows:

| Box | Discharged by |
|---|---|
| Applicable admitted properties identified | §4.3 below — none admitted; the registry is an A6 artifact and no property has been admitted through it, so the applicable set is empty and the obligation is vacuous, not skipped |
| Every MUST-level property satisfied | Vacuous for the same reason. The document's own FR-AG requirements were verified in §4 above; those are requirements, not admitted properties |
| Required proof complete | §5.2's Trigger Matrix maps proof obligations to *code* change types. This artifact is a document, and no row applies. Recorded rather than silently passed |
| No Blockers open | §5 — twelve findings, none dispositioned Blocker |
| Every finding dispositioned | §5 — all twelve carry an explicit disposition |
| Fresh final review completed | **Not yet.** Rounds 1–3 are complete but round 3 returned three Medium findings, so the review has not converged; round 4 over the current artifact is outstanding. This box stays unticked until a round returns only Low findings or none, per FR-AG-018 |

### 4.3 A recorded limitation

The first two boxes are discharged as *vacuous*, not as *satisfied by evidence*. No architectural
property has been admitted anywhere in this repository, because the admission machinery is itself
downstream work. This is an honest reading of an empty set, but it means those two boxes carry no
assurance. They will acquire real content the first time a property is admitted, at which point this
review's conclusion on them does not transfer. Recorded here rather than left implicit.

---

## 5. Findings

Twelve findings across three rounds, in Governance Appendix B field order. None is a Blocker.

**Round 3's eight findings are summarised in the table below rather than written out in full field
form, and that is a deliberate, recorded shortfall.** Governance FR-AG-009 requires every substantive
finding to end in exactly one disposition, which they do, and each row carries the Appendix B fields
that bear on disposition. But five of the Appendix B fields — Round, Requirement/Property, Required
action, Owner, Resolution evidence — are collapsed into the row text rather than listed. If this
record is ever cited as an example of Appendix B compliance, cite AG-A0-001 to AG-A0-004 below, not
the table.

| ID | Round | Severity | Summary | Disposition / evidence |
|---|---|---|---|---|
| AG-A0-005 | 3 | Medium | §4.1's finding-lifecycle line still listed three terminal states after v0.6 extended §4.2's enum to five — AG-A0-003's defect one section over, and the reason v0.6's "extended to match §4.1" was false | Resolved. §4.1 now lists all four terminal states and maps each to its disposition; the false claim is annotated in the v0.6 row, not deleted |
| AG-A0-006 | 3 | Medium | The v0.6 version-history row attributed the Blocker trigger to FR-AG-011, which requires only that a Blocker *cite* an authority | Resolved. The rule is §4.3 item 5; the row is corrected in place with a ⚠️ annotation |
| AG-A0-007 | 3 | Medium | FR-AG-026's "unless an approved exclusion exists" named no mechanism — a MUST-level rule with an undefined escape hatch | Resolved. Closed to exactly two recorded artifacts: a property's §3.3 Non-scope, or a §7.1 exception. Prose assertion explicitly excluded |
| AG-A0-008 | 3 | Low | §5.4 stated its evidence list with no modal verb while §5.3 and the fixed §5.5 anchor theirs to a MUST | Resolved. Anchored to FR-AG-028 in the same shape |
| AG-A0-009 | 3 | Low | §7.1's exception schema still mandated the bare `AP-###` that FR-AG-004 calls recommended | Resolved. Hedged — the third and last site of AG-A0-004's defect |
| AG-A0-010 | 3 | Low | §4.2 and Appendix F said "Tradeoff" where FR-AG-009 and Appendix B say "Accepted Tradeoff" | Resolved. Unified |
| AG-A0-011 | 3 | Low | Appendix A paraphrases five of §3.3's field labels, working against the machine-checkable-schema ambition in §6.1 | Resolved. Appendix A now states §3.3 governs and that a schema is generated from §3.3, not the template |
| AG-A0-012 | 3 | Low | Appendix F's property summary omitted §3.1's `Rejected → Candidate` and `Superseded → Retired` edges | Resolved. Both added |

The four findings from rounds 1 and 2 follow in full Appendix B form.

### AG-A0-001

- **Round:** 1
- **Severity:** Low
- **Summary:** §5.5 was the only place in the document that addressed test authoring directly rather
  than proof scope, wording §1.3 reserves to Spec #19.
- **Evidence:** §5.5 line 730 read *"Where meaningful, tests SHOULD intentionally cause:"*. Every
  sibling subsection (§5.3, §5.4, §5.6) phrases its obligation as what the *proof* or *evidence* must
  do. §1.3 line 73 disclaims "detailed test framework implementation".
- **Requirement / Property:** §9.1 box 2; §1.3; §1.4 line 102
- **Disposition:** Resolved
- **Required action:** Fix
- **Owner:** A0 review
- **Status:** Resolved
- **Resolution evidence:** Reworded in v0.5. Superseded by AG-A0-002, which reworked the same passage
  again; see below.

### AG-A0-002

- **Round:** 2
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
- **Requirement / Property:** FR-AG-029; §4.3 item 5; FR-AG-016
- **Disposition:** Resolved
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

- **Round:** 2
- **Severity:** Medium
- **Summary:** The §4.2 Finding Record Schema's Status enum offered no valid value for a
  Residual-Risk or Candidate-Property finding.
- **Evidence:** §4.2 line 573 read *"Open / Resolved / Accepted"*. §4.1 line 555 gives
  *"Open → Dispositioned → Resolved/Accepted/Recorded"*, and Appendix F (lines 1446, 1448) adds
  *"Open → Residual Risk → Recorded"* and *"Open → Candidate Property → Property process"*. The
  analogous Property State field (§3.3 line 495) correctly mirrors all five states from §3.1, so the
  asymmetry was an oversight rather than a design choice.
- **Requirement / Property:** §4.1; Appendix F; §9.3 box 6
- **Disposition:** Resolved
- **Required action:** Fix
- **Owner:** A0 review
- **Status:** Resolved
- **Resolution evidence:** Enum extended to *"Open / Resolved / Accepted / Recorded / In property
  process"* in v0.6.

### AG-A0-004

- **Round:** 2
- **Severity:** Low
- **Summary:** §3.3 stated the `AP-###` identifier format as a MUST-level schema requirement while
  FR-AG-004 calls the same format merely "Recommended".
- **Evidence:** FR-AG-004 (lines 175–178): *"Every admitted property MUST receive a stable
  identifier. Recommended form: `AP-###`."* §3.3 line 489 introduces its table with *"Every admitted
  property MUST record:"*, and line 493 read *"| Property ID | Stable `AP-###` |"* — mandating the
  exact format FR-AG-004 hedges.
- **Requirement / Property:** FR-AG-004; §9.2 box 3
- **Disposition:** Resolved
- **Required action:** Fix
- **Owner:** A0 review
- **Status:** Resolved
- **Resolution evidence:** Schema cell now reads *"Stable identifier; recommended form `AP-###`
  (FR-AG-004)"* in v0.6.

### Not filed as findings

One round-2 observation is deliberately not filed: that the v0.5 version history cited this file
before it existed. That was a same-branch sequencing artifact of splitting the work across two
commits, not a document defect, and it resolves on this file landing.

---

## 6. Outcome and what remains

**Review outcome: NOT YET CONVERGED — round 4 outstanding.** All twelve findings to date are
dispositioned and none is a Blocker, which is what convergence needs on the findings side. What is
missing is a round that comes back clean: round 3 returned three Medium findings, and its fixes
produced v0.7, which has not yet been reviewed. Round 4 is dispatched.

This is deliberately not rounded up. The rounds have not been a formality: round 2 found a High in
the passage round 1 had just amended, and round 3 found that round 2's fix was incompletely
propagated *and* that two things round 2 wrote into the document's own version history were false.
On that record, predicting round 4 clean would be unfounded.

**Expect this to take another round or two.** Each pass has surfaced fewer and lighter findings —
High, then Medium, and the trend is the useful signal — but the propagation defects in particular
have recurred three times across different sections, so a clean round is the only thing that should
close this.

**This review does not approve the Governance specification, and nothing in this file should be read
as approval.** `Status:` remains `Draft`. The remaining A0 conditions are, in order:

1. **A review round returns only Low findings, or none.** Outstanding — round 4 is dispatched
   against v0.7. Rounds continue until one comes back clean.
2. **Human sign-off.** Not delegable to an agent.
3. **Then** write `Status: Draft` → `Approved` in the Governance file.
4. **Then** compute the SHA-256 of that resulting file and record it in integration plan §11 A0.

Steps 3 and 4 are ordered and the order matters: a digest computed before the status edit pins a
superseded artifact, and a digest written into the file it covers invalidates itself. The digests in
§1 above are review-subject identities and are **not** the adoption pin.

Once A0 closes, A2 is the next stage. Governance §9.7 remains open and is owned by A3–A9; the
specification is authoritative at A0 but not *fully adopted* until then.

---

## Version History

| Version | Date | Author | Notes |
|---|---|---|---|
| 1.1 | August 31, 2026 | — | Round 3 recorded; Governance v0.6 → v0.7. Eight further findings, three Medium and five Low, none a Blocker — twelve in total. Two of them (AG-A0-005, AG-A0-006) falsify claims round 2 wrote into the Governance version history about its own fix, and one of those, the FR-AG-011 misattribution, had been propagated into this record, the Governance history and the CHANGELOG before round 3 caught it; all three sites are corrected by annotation rather than rewrite. AG-A0-007 closes FR-AG-026's previously undefined "approved exclusion" to two recorded artifacts. Round 3 did not converge — three Medium — so round 4 over v0.7 is dispatched and outstanding, and the convergence rule now applied is the repository's own: a round returning only Low findings or none. Recorded shortfall: round 3's eight findings are tabulated rather than written in full Appendix B field form, and the record says so. |
| 1.0 | August 31, 2026 | — | Initial record. Two review rounds complete over Governance v0.4 → v0.6, §9.1–§9.6 in scope; round 3 over v0.6 dispatched and outstanding at this revision. 46 of 52 boxes verified against cited line ranges; five of the six §9.6 process-state boxes discharged by this record, two of those vacuously and recorded as such, and "fresh final review completed" left unticked pending round 3. Four findings, all Resolved, none a Blocker. Outcome recorded as NOT YET CONVERGED rather than rounded up — round 2 found a High defect inside the passage round 1 had just amended, which is the whole reason FR-AG-018 requires a fresh pass over the current artifact. §9.7 confirmed unlanded and scoped out to A3–A9. Status and adoption digest deliberately not written. |
