# PR #416 Evidence-Ref Archive

> **Created:** September 18, 2026
> **Purpose:** Make the branch-only evidence carried by the 24 `evidence/pr416-*` refs durable on
> `main` before any disposable ref is deleted.
> **Status:** Step 5 archival candidate. This archive does **not** itself authorize ref deletion.

## What is archived

The archive contains **85 exact Git blob snapshots**:

- **72 current-tip files** covering the directional branch-side file set for all **24/24** evidence refs;
- **13 historical run-time files** for six runs whose evidence branch later advanced:
  `35224582571`, `35284452456`, `35285144898`, `35293636125`,
  `35301715589`, and `35305911122`.

Every snapshot is stored beneath this directory with its original repository path preserved beneath
the ref/run directory and a terminal `.txt` suffix. The suffix is quarantine only: the stored blob
bytes are unchanged. This prevents archived C#, workflow YAML, and historical Markdown from entering
normal source, Actions, YAML, or Markdown discovery.

`MANIFEST.tsv` is the byte-identity authority. It records the source ref, exact source head,
historical run id where applicable, original path, archive path, and Git blob SHA for every snapshot.

## Inventory method

The uniqueness audit is directional. For each live evidence ref, the branch-side file set was derived
from **merge-base → evidence ref**, not from `main → evidence ref`. Comparing current `main` directly
to an old evidence ref mixes later mainline changes into the result and can falsely attribute those
changes to the evidence branch.

The current-tip archive therefore preserves every file in that directional branch-side set, including
workflow wrappers, injected evidence helpers, preregistration records, experimental source forms,
historical spec/test/source states, and result-enforcement logic. Where a branch had advanced after a
cited evidence run, the relevant run-time files are additionally captured under `runs/<run-id>/`.

This archive complements, rather than replaces,
`docs/tracking/pr416-evidence-provenance.md`, which records the causal interpretation, run outcomes,
material embedded deltas, and the three pre-existing policy-retained refs.

## 24-ref disposition

The machine-readable authority is `ref-disposition.tsv`.

| Disposition | Count | Meaning |
| --- | ---: | --- |
| `deletable` | 21 | No unique branch-only file evidence remains after this archive is on `main`; deletion still waits for the main-only verification step. |
| `retain` | 0 | No ref requires ordinary retention solely because archival is incomplete. |
| `policy-retained` | 3 | Existing provenance explicitly retains the ref; copying its bytes does not silently revoke that policy. |

The three `policy-retained` refs are:

- `evidence/pr416-close-chance-retirement`
- `evidence/pr416-narrow-rolling-candidate`
- `evidence/pr416-state-only-preforce-candidate`

The other 21 refs are classified `deletable` **subject to Step 5 main-only verification after this
archive lands**. In particular, every row currently has `delete_now=false`.

## Main-only deletion verification

After this archive is merged, the deletion verifier must use `main` plus this directory and the
durable provenance/diagnosis only. For every `deletable` row it must establish:

1. the row exists in `ref-disposition.tsv`;
2. all current-tip files attributed to that ref exist in `MANIFEST.tsv`;
3. each archived file's Git blob SHA matches the manifest;
4. any separately required historical run-time snapshot for an advanced ref exists and matches;
5. the causal/result interpretation remains recoverable from
   `pr416-evidence-provenance.md` / `w6-elevated-stationary-ball-fix.md`;
6. no policy record still marks that ref retained.

Only after all six are true from `main` alone is deletion authorized for that individual
`deletable` ref. A failed or ambiguous check leaves the ref intact.

The three `policy-retained` refs are excluded from deletion even if all byte/archive checks pass.
Changing that disposition requires an explicit later policy decision, not inference from the existence
of this archive.
