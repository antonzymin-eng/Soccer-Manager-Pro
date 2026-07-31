---
name: err-file-and-backprop
description: >-
  File a spec-error-log entry (ERR-NNN-NNN) and land the back-propagation into the owning spec
  correctly — allocating a non-colliding id, writing the entry in the log's fixed shape, patching the
  spec section in the same commit when the spec text is itself the defect, and bumping every version
  header the change touches. Use this skill whenever a defect is found in an APPROVED spec or in code
  that contradicts one, whenever an ERR id needs allocating or citing, whenever a cross-spec
  back-prop is landed at approval, and whenever a fix note says "spec + code, same commit". Trigger
  it even when the fix looks purely like code — if the spec's own pseudocode or formula sourced the
  bug, the spec is the defect and skipping the log leaves the next implementer to rebuild it.
---

# File an ERR and Back-Prop

`docs/tracking/spec-error-log.md` is at v1.53 — roughly fifty revisions. It is the authoritative
remediation backlog, and its value depends on two things being reliably true: ids are unique, and an
entry describes the *spec* defect rather than only the code fix.

## Step 1 — Allocate the id against the live log

Ids are `ERR-<owning spec number, 3 digits>-<sequence, 3 digits>`, e.g. `ERR-011-007` for
Goalkeeper Mechanics #11. The owning spec is the one whose text is wrong, not the one where the
symptom appeared — the keeper-contact pass filed against #11 *and* #12 because both specs' formulas
were defective.

**Check the id is free against the log as it exists right now**, not against a plan written earlier:

```bash
grep -n "ERR-011-" docs/tracking/spec-error-log.md | tail
```

This repo has hit collisions twice, and both times the proposed id had been written down in advance
and filed by someone else in between:

- three design supplements proposed ids that had already been filed the same day the supplements were
  written, and had to be reassigned at promotion;
- `ERR-030-015` collided live and became `ERR-030-025` during a merge.

So treat an id written in a design note as a *suggestion to re-verify*, never a reservation. Also
check the spec folders themselves (`grep -rn "ERR-030-0" docs/specs/`) — a citation can exist in an
approved spec before the log entry lands.

Two id conventions worth knowing: numbers are sometimes deliberately **skipped** to soft-reserve them
for an in-flight cluster, and a duplicate that has already shipped in approved text stays **preserved
verbatim as errata** rather than being silently renumbered — six approved specs citing a step number
is a stronger constraint than tidiness.

## Step 2 — Write the entry in the log's shape

Append the entry and follow the existing template exactly (copy `ERR-008-017` as the model):

```markdown
## ERR-NNN-NNN: <Spec name> #N §X.Y — <one-line statement of the defect>

**Filed:** <date> — <what pass surfaced it>. **Status: RESOLVED** (same commit) | **OPEN**.
Owner design supplement: `docs/tracking/<topic>-design.md`.

**How found.** <the measurement or review that surfaced it, with numbers; then the cause verified
against source — name the file and the expression>

**Fix (spec + code, same commit).** <what the spec section now says, what stays unchanged and why,
what the worked examples become>

**Determinism impact:** <schema / RNG stream / domain tag / draw site / draw order — usually "none";
then what locks it, with the pre-fix failure verified by execution>

---
```

The "How found" section is the part that pays off later. An entry that says *what* was wrong without
*how it was measured* cannot be re-checked, and several entries here were only believable because they
carried the numbers (dive-early 456–2000 ms with dive-late exactly zero; mean shot distance 30–34 m
against football's ~17).

Then update the file header: bump `**Version:**`, rewrite `**Updated:**` with a prose summary of this
revision, and relabel the previous summary so the chain reads `Prior update below.`

## Step 3 — Patch the owning spec in the same commit

When the spec text sourced the defect, the spec is the deliverable. Landing the code fix alone leaves
an approved document that still instructs the next implementer to rebuild the bug — which is exactly
how the Ball Physics ground-normal bug and the `z < 0.22 m` boundary gate survived as long as they did.

- Edit the section file (`docs/specs/<folder>/section-N.md`), including the pseudocode and any worked
  example whose arithmetic the change moves. If an example is now unreachable in production, annotate
  it rather than deleting it.
- Append a version-history row to every section file you touched, and bump its `Version:` /
  `Modified:` headers — stale headers against real history are a recurring low finding here.
- When two documents disagree, **Appendix B is the byte-layout authority** over §3 pseudocode; that
  precedent settled `ERR-030-011`.

## Step 4 — Decide the back-prop's timing

Not every ERR lands immediately. Three timings are in use, and stating which one applies avoids a
back-prop quietly evaporating:

- **Same commit** — the default when the fix is being landed now.
- **Atomically at approval** — cross-spec back-props filed when a spec flips to APPROVED. Landing
  twenty-three of them together is what exposed the duplicated `#30` day-advance step numbers; filing
  one at a time could not have.
- **Deferred to a named stage** — when the consumer does not exist yet. Say which stage, and why
  building it now would be the phantom-consumer trap.

Also record when an entry changes an approved **contract** rather than a pointer, since that needs
owner awareness: `ERR-048-001` resolved two MUSTs inside one approved spec that were jointly
impossible and would have surfaced as an assembly cycle.

## Step 5 — Close out

Cite the ERR id in the code comment at the fix site and in the commit message, so the code points back
at its rationale. Then run the `landing-close-out` skill — the root `CLAUDE.md` entry and the OPEN
ISSUES section both need the ERR referenced, and the log version bump belongs in the same commit as
everything else it describes.
