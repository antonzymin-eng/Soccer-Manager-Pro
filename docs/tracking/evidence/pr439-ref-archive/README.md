# PR #439 Evidence Ref Archive

> **Created:** September 23, 2026
> **Purpose:** Preserve disposable `evidence/pr439-*` provenance before branch cleanup.
> **Production anchor:** PR #439 merged as `e62f2fd215b03419664e2405b7f8f99354257498`.

This archive covers **16** PR439 evidence refs and **54** source snapshots: **31** current-tip paths plus **23** branch-history blob states. Current snapshots are the complete non-deleted tip diff against each ref's merge base. History snapshots preserve every non-deleted blob state introduced by earlier branch-exclusive commits, including superseded Actions run heads.

Every snapshot is stored under `current/` or `history/` as a quarantined `.txt` path while reusing the exact historical Git blob SHA. `MANIFEST.tsv` binds each snapshot to source ref, source commit, commit metadata, original path, optional run ID, archive path and blob SHA. `run-heads.tsv` records all 21 Actions runs observed on these refs. `ref-disposition.tsv` pins all 16 current branch heads.

The `meta/` TSV files are construction ledgers retained so the final manifest can be independently reconciled to the per-ref capture. They are not evidence substitutes.

All refs remain `delete_now=false`. This PR archives evidence only; it does not delete any `evidence/pr439-*` branch. Deletion becomes eligible only after the archive is merged to `main` and `tools/dotnet-ci/check_pr439_evidence_refs.py` passes.
