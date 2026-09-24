# PR #439 Evidence-Ref Archive

> **Created:** September 23, 2026
> **Purpose:** Preserve branch-only PR #439 evidence and cited-run provenance before any disposable `evidence/pr439-*` ref is deleted.
> **Status:** Archive built; **no deletion authorized**. All 16 refs remain live with `delete_now=false`.

## Archived material

The archive contains **39 exact branch-history blob snapshots** under `history/`, covering every non-deletion file state introduced by the exclusive commits of all **16/16** live `evidence/pr439-*` refs.

Each snapshot carries a terminal `.txt` suffix but preserves the original Git blob bytes. Historical C#, workflow YAML, scripts, and tests therefore remain auditable without entering ordinary source or workflow discovery.

`MANIFEST.tsv` records, for every archived state:

- source ref and merge base;
- exact source commit, subject, and committer date;
- original path and quarantined archive path;
- original Git blob SHA.

`run-heads.tsv` records **26 workflow runs cited by PR #439's durable record/review history**, including exact head branch/SHA, commit subject/date, event, conclusion, and workflow name.

`ref-heads.tsv` records all **16** live `evidence/pr439-*` refs and exact heads. `ref-disposition.tsv` classifies all 16 as `deletable`, but every row remains `delete_now=false`; archival completeness and deletion authorization are deliberately separate states.

The result-bearing W3 six-seed tables and artifact provenance are committed separately under `../pr439-w3/`.

## Verification

`tools/dotnet-ci/check_pr439_evidence_refs.py` owns this directory's integrity contract.

Archive mode verifies that:

1. all ledgers exist and the 16-ref disposition set matches the recorded ref set;
2. every snapshot path is unique, quarantined as `.txt`, and hashes to the recorded original Git blob SHA in the committed tree;
3. the cited-run ledger has no duplicate run IDs.

Live mode additionally requires a full-history checkout with all `evidence/pr439-*` remote-tracking refs fetched. It verifies:

1. the live remote-ref set is exactly the 16 recorded refs;
2. every live head equals the recorded head SHA;
3. every non-deletion file/blob state introduced by every branch-exclusive commit is represented exactly in the durable manifest.

Before any deletion, run the live check against freshly fetched remote refs. Deletion remains blocked if the live set/head mapping drifts or if any branch-history state is missing from the archive.

## Deletion boundary

This archive does **not** delete branches and does not silently authorize cleanup. A later explicit cleanup change may flip the 16 `delete_now` values only after the live verifier passes on the then-current remote topology. Remote deletion should then use the recorded expected head SHAs as leases.

Until that explicit authorization exists, every PR #439 evidence ref remains retained.
