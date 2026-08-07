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

This repo has hit collisions repeatedly, and every time the proposed id had been written down in
advance and claimed by someone else in between:

- three design supplements proposed ids that had already been filed the same day the supplements were
  written, and had to be reassigned at promotion;
- `ERR-030-015` was verified free on a branch, then claimed on `main` by #30's own T3 landing while
  that branch was still open — a **branch-vs-main** collision, which a check at authoring time cannot
  catch;
- the injury/aging note proposed `ERR-028-002..004` on July 26; `ERR-028-002` was filed at #53's
  approval on July 27, so that whole range is stale and nothing has re-pointed it.

Two consequences. Treat an id written in a design note as a *suggestion to re-verify*, never a
reservation. And **re-verify at merge, not only at authoring** — the branch-vs-main case is invisible
until you rebase. Also check the spec folders themselves (`grep -rn "ERR-030-0" docs/specs/`), since
a citation can exist in an approved spec before the log entry lands.

**Since August 7, 2026 the merge-time half is enforced mechanically.**
`tools/spec-ci/check-id-collisions.sh` runs in CI's `spec-hygiene` job on every `pull_request` — the
first moment both sides of a branch-vs-main race are visible — and fails on a duplicate ERR detail
entry or Error Index row, along with the `DOMAIN_TAG` / `SubsystemOrdinals` / FR-id / FR-prefix and
version-history namespaces. Run it locally before pushing:

```bash
bash tools/spec-ci/check-id-collisions.sh
```

That does not retire the manual check at Step 1 — the gate sees the id you *wrote*, not the one you
are about to write, and it cannot tell you which free id to pick. It closes the case where you picked
correctly and the log moved anyway.

Two id conventions worth knowing: numbers are sometimes deliberately **skipped** to soft-reserve them
for an in-flight cluster, and a duplicate that has already shipped in approved text stays **preserved
verbatim as errata** rather than being silently renumbered — six approved specs citing a step number
is a stronger constraint than tidiness.

## Step 2 — Write the entry in the log's shape

**The log has two surfaces per entry, and both must be updated.** Missing the first is easy, because
the file is long enough that you land in the detail section and never scroll back:

1. **A summary row in the `## Error Index` table near the top** — `| ID | Title | Severity | Files
   Affected | Status |`. Severity is Major / Moderate / Medium; Status is a short phrase
   (`Closed — fixed in …`, `Open — low priority`, or `◑` for spec-text-first entries whose code half
   is deferred).
2. **The full entry further down.** Append it and follow the existing template exactly (copy
   `ERR-008-017` as the model):

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
