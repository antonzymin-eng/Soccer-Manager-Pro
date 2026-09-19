# PR #416 Evidence-Ref Archive

> **Created:** September 18, 2026
> **Purpose:** Preserve branch-only PR #416 evidence before any disposable `evidence/pr416-*` ref is deleted.
> **Status:** Step 5 deletion authorization candidate. The 21 refs classified `deletable` are marked `delete_now=true` as one atomic set; the three policy-retained refs remain `false`. This transition becomes effective only after this exact revision passes both deletion gates and lands.

## Archived material

The archive contains **100 quarantined snapshot paths**:

- **72 current-tip files** covering the directional merge-base→tip file set for all **24/24** evidence refs;
- **13 run-time files** for six cited runs whose evidence branch later advanced;
- **15 intermediate-history paths**, representing **13 previously unarchived blob identities**, recovered from branch-exclusive commits that were no longer visible at ref tips.

Every snapshot carries a terminal `.txt` suffix but preserves the original Git blob bytes. Historical
C#, workflow YAML, scripts, and Markdown therefore remain auditable without entering ordinary source,
Actions, YAML, Python, or Markdown discovery.

`MANIFEST.tsv` is the snapshot byte/provenance authority. Its `kind` values are `current`, `run`,
and `history`. Each row now also preserves the selected source commit's subject and Git committer
date so those two context fields remain available even if the source ref later disappears.

`run-heads.tsv` separately records the authoritative GitHub Actions metadata for **all 31 workflow
run ids cited** across the durable PR #416 provenance and diagnosis: run id, head branch, exact
`head_sha`, commit subject/date, event, conclusion, and workflow name.

A `source_head` is exact provenance for that archived row, but it is not asserted to be the first or
only commit that ever contained the blob. Identical blob content can appear at multiple commits.

## Why the original tip-only audit was insufficient

A merge-base→tip diff is correct for the **tip file set**, but it is not a complete deletion audit.
An evidence branch can create or modify a file on an intermediate commit and later replace or remove
that state before the tip. Deleting the ref can then make that intermediate blob unreachable even
though a tip-only manifest is perfect.

The corrected audit therefore treats tip coverage and history coverage as separate properties.
The 13 newly recovered blob identities include intermediate test-source revisions, workflow revisions,
and the ApplyKick ablation generator/workflow that were absent from the original tip inventory.

## 24-ref disposition

`ref-disposition.tsv` remains the machine-readable disposition authority:

| Disposition | Count | Meaning |
| --- | ---: | --- |
| `deletable` | 21 | Candidate for deletion only after both verification stages below pass. |
| `retain` | 0 | No ref is presently retained solely because known archival work remains incomplete. |
| `policy-retained` | 3 | Existing provenance explicitly retains the ref; archival does not override that policy. |

The three policy-retained refs remain:

- `evidence/pr416-close-chance-retirement`
- `evidence/pr416-narrow-rolling-candidate`
- `evidence/pr416-state-only-preforce-candidate`

All **21** `deletable` rows are `delete_now=true`; the **3** `policy-retained` rows remain `false`. Mixed or partial authorization is invalid.

## Verification before deletion

Deletion has **two different gates**. They prove different things and neither substitutes for the other.

### A. Live-ref completeness — must run while the refs still exist

For every ref proposed for deletion:

1. derive its merge base against current `main`;
2. enumerate every branch-exclusive commit from merge base through the ref tip;
3. enumerate every changed-path blob state introduced by that history, including states later replaced
   or removed before the tip;
4. compare those blob identities against the durable set that will survive deletion: current `main`,
   this archive, and refs explicitly classified `policy-retained`;
5. require every blob that would otherwise become deletion-set-only to have an exact archived blob
   entry in `MANIFEST.tsv`;
6. require the current-tip directional file set to match the `current` manifest rows exactly;
7. require all cited workflow runs for that ref to resolve to the exact `head_sha` recorded in
   `run-heads.tsv`, with any run-specific state needed for interpretation represented by the archive
   or durable mainline records.

This gate must fail closed on any missing ref, commit, path, blob, or run mapping. It cannot be
reconstructed from `main` alone after the refs are deleted.

### B. Mainline archive integrity and interpretive reconstruction — after this archive lands

From `main` alone, verify that:

1. every `MANIFEST.tsv` archive path exists;
2. every archived file hashes to its recorded Git blob SHA;
3. every disposition row and every run-head row is present and parseable, including preserved
   commit subject/date fields;
4. the causal/result interpretation remains recoverable from
   `pr416-evidence-provenance.md` and `w6-elevated-stationary-ball-fix.md`;
5. the three policy-retained refs remain excluded from deletion unless an explicit later policy
   decision changes them.

This second gate proves archive integrity and enough preserved context for **interpretive**
reconstruction. It does **not** reconstruct full Git commit identity such as author, parentage, or
complete co-change topology, and it does **not** prove historical completeness; only the live-ref
gate can do that.

Only a ref that passes both gates, remains classified `deletable`, and has `delete_now=true` may be deleted. The 21 deletion candidates transition as one atomic authorization set; partial authorization is invalid.
