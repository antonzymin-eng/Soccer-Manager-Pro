# PR #439 Evidence-Ref Archive

> **Created:** September 23, 2026
> **Purpose:** Preserve every branch-exclusive changed-file blob state and cited workflow-run head needed to make the PR #439 evidence refs disposable without losing the evidence record.
> **Status:** **PRE-DELETE ARCHIVE COMPLETE CANDIDATE — NO DELETION AUTHORIZED IN PR #447.**

## Archived scope

This archive records:

- all **16** live `evidence/pr439-*` refs and their exact observed remote heads in `ref-heads.tsv`;
- all **39** branch-exclusive changed-file blob states across those refs in `MANIFEST.tsv`, each copied byte-for-byte under `history/**.txt`;
- all **26** GitHub Actions run ids cited on PR #439 in `run-heads.tsv`, including exact run branch, run head SHA, event, conclusion and workflow name;
- an explicit `ref-disposition.tsv` in which every ref is classified `deletable` but every `delete_now` remains `false`.

The archive includes intermediate states, not only branch tips. In particular it preserves the pre-W3 instrument/parser/workflow lineage `6f2ed037…` → `ccb7bf67…` → `cf36526c…`, the W6 pre-fix discriminator source, all three W2 diagnostic arms, the M7 mutation proof, and the full #441 header-reachability evidence lineage.

The result-bearing six-seed W3 tables are preserved separately in `../pr439-w3/`.

## Verification

`tools/dotnet-ci/check_pr439_evidence_refs.py` owns archive integrity and pre-delete live-ref completeness. `.github/workflows/pr439-evidence-ref-gate.yml`:

1. fetches full history plus all live `evidence/pr439-*` refs;
2. verifies every archived `.txt` blob equals the recorded original Git blob;
3. requires the exact 16-ref topology and exact recorded heads;
4. re-derives every branch-exclusive changed-file blob state and requires the live 39-state set to match the manifest exactly;
5. verifies that the run ledger exactly covers the workflow-run ids cited on PR #439 and cross-checks each run against the GitHub Actions API.

This follows the PR #416 archive precedent while keeping PR #439's archive bounded to its own evidence refs.

## Deletion boundary

PR #447 does **not** delete evidence branches and does **not** authorize deletion. The archive must first land on `main` and pass its pre-delete gate from the durable tree. Any later cleanup must be an explicit follow-up that changes the disposition/authorization state and performs expected-head-protected deletion only after the live-ref completeness gate is green.

Until then, all 16 refs remain live.
