---
name: spec-promotion
description: >-
  Promote a converged design supplement to a full numbered specification — authoring the 11-file
  section set at IN REVIEW, allocating a collision-free FR- prefix and spec number, running the PASS-1
  adversarial review and AR sweep to convergence, registering the SPEC_INDEX row, and stopping at the
  human sign-off gate. Use this skill whenever a docs/tracking/*-design.md supplement is being turned
  into docs/specs/<folder>/, whenever a new spec number or FR- prefix is being allocated, whenever a
  spec's status moves NOT STARTED → IN REVIEW → APPROVED, and whenever back-props are being filed at
  approval. Trigger it even when the request is just "write up spec #N" or "approve this spec" —
  the gate order and the id-collision checks are where this goes wrong.
---

# Spec Promotion

Eleven promotions have run through this repo, ten of them in a single day. The mechanics are
well-worn; what makes them worth a skill is that the same three defects recurred across the wave,
one of them in **all ten** promotions.

The governing rule: a **design supplement** (`docs/tracking/*-design.md`) is frozen at its
convergence and confers no approval status. The spec folder is the contract. Read the supplement for
the *reasoning* and the spec for the *rules* — where they disagree after promotion, the spec wins.

## Step 1 — Allocate the number and the prefix, against live state

**Spec number:** take it from `docs/specs/SPEC_INDEX.md`, which is canonical. A number reserved in a
design note's own table is a reservation against *renumbering collisions*, not a registry entry —
the row lands in `SPEC_INDEX.md` at promotion, never at design-note stage.

**FR- prefix:** two letters (occasionally three), and collisions are easy. `FR-PR-` was proposed for
Positional Rotations and is already Pressing AI's. Check before committing to it:

```bash
grep -rhoE "\bFR-[A-Z]{2,3}-" docs/specs/ docs/tracking/ src/ | sort -u
```

`docs/tracking/` is included deliberately, not just `docs/specs/` — an unpromoted design supplement
can already hold a prefix (today `FR-DT-` appears there and in no spec), and checking only the spec
folders would miss exactly the collision this step exists to catch.

**ERR ids:** any `ERR-` id the supplement proposes must be re-verified free against
`docs/tracking/spec-error-log.md` *and* the spec folders. Three supplements in the last wave proposed
ids that had already been filed. See the `err-file-and-backprop` skill.

## Step 2 — Author the 11-file set

Mirror the layout of an existing spec folder (`docs/specs/tactical-presets/` is a clean recent one):

```
outline.md
section-1.md   Introduction, scope, dependencies, key decisions (KD-N)
section-2.md   Functional requirements (FR-XX-NNN), data structures, failure modes
section-3.md   Formulas, algorithms, pseudocode  (split section-3-1.md … when large)
section-4.md   Architecture, file layout, interface contracts
section-5.md   Test plan
section-6.md   Performance analysis and budgets
section-7.md   Future extensions and Stage 1+ deferrals
section-8.md   References and citations
section-9-approval-checklist.md
appendices.md
```

Everything starts at `Status: IN REVIEW`, v0.1.

Points where promotions here have gone wrong:

- **Constant tags.** Every constant carries exactly one of `[GT]` `[EST]` `[FIXED]` `[DERIVED]`
  `[CROSS]` `[CROSS-PENDING]`. A `[CROSS-PENDING]` is an outstanding dependency that gates this
  spec's own approval, so use it sparingly and cite the tracking ERR.
- **The §6.3 → Appendix A gap.** In all ten promotions of the last wave, the `[GT]` budget ceilings
  declared in §6.3 were missing from the Appendix A catalogue — an artifact of §6 being authored
  before the appendices with nothing walking back. Reconcile the two before calling the draft done.
- **Citations.** Mark unverified references `[CITATION-PENDING]` and resolve them with real DOIs.
  Never fabricate a citation or a verification value; if the environment blocks lookup, record the
  blocked attempt. Two fabricated references had to be replaced with real equivalents in #10, and a
  fabricated set of KAT vectors was found in #16.
- **Worked examples.** Mirror any team-relative geometry to the away side. Three home/away asymmetry
  defects shipped in #8 because every example and every fixture used the home team.
- **Interfaces.** Do not specify an interface against an unspecified consumer. A registered stream,
  ordinal, or interface with no draw site is the phantom-surface trap — the repo deliberately leaves
  ordinals unallocated until their first real draw site.

## Step 3 — PASS-1 adversarial review, then sweep to convergence

Run the `adversarial-review` skill over the section files. File the findings as
`adversarial-review-section-files-v1.md` in the spec folder, fix them in a same-day v0.2 pass, and
sweep again until a round produces no High or Medium findings (§9.4.1). Record the counts —
`PASS-1 1H+3M+3L → PASS-2 clean` — in §9.3 and the version history.

If a PASS finds a High, re-read the whole spec rather than only the touched section: #25's H-1
(a referenced field that did not exist on the struct) was found that way, and its PASS-2 re-read was
what confirmed nothing else depended on the same phantom.

## Step 4 — The three gates, in order

- **G1 — spec content complete.** Sections written, reviews converged, citations resolved, appendices
  reconciled.
- **G2 — back-props filed.** Cross-spec ERRs land **atomically with the approval flip**, not before.
  Landing twenty-three together is what exposed `#30`'s duplicated day-advance step numbers — two
  step 7s and two step 8s in a sequence six approved specs cite by number. No single approval could
  have seen it.
- **G3 — lead-developer R-01..R-05 sign-off.** This is a **human authority and is not
  self-grantable.** Stop at IN REVIEW and say so. Every promotion in the last wave stopped here
  deliberately, exactly as each supplement's own §12 pipeline states.

## Step 5 — Register and close out

At promotion to IN REVIEW: add the `SPEC_INDEX.md` registry row, retire any RESERVED note, and bump
the supplement to note the promotion.

At approval (once G3 is granted): flip every file in the folder to `Status: APPROVED`, complete
`section-9-approval-checklist.md` with the §9.5 gate table and §9.6 decision, update the
`SPEC_INDEX.md` counts, and land the G2 back-props in the same commit.

Then run `landing-close-out`. One thing to carry into the root `CLAUDE.md` entry: state whether the
newly approved spec has a `src/` assembly. "APPROVED" says nothing about whether code exists — it is
currently untrue of roughly 42% of the registry, and that gap is the single most misread fact about
this project's state.
