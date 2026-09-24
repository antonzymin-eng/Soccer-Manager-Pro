# PR #439 Evidence-Ref Archive — Phase 1

> **Created:** September 23, 2026
> **Purpose:** Record the live `evidence/pr439-*` ref topology before any branch cleanup.
> **Status:** **NO DELETION AUTHORIZED.** This is the first archival phase only.

`ref-heads.tsv` records all 16 live `evidence/pr439-*` refs and their exact remote head SHAs, with the UTC observation time of the live-remote inventory.

This is a recorded snapshot, not protection against later ref deletion. It deliberately does **not** yet claim PR #416-style branch-exclusive blob completeness. Before any `evidence/pr439-*` ref is deleted, Phase 2 must:

1. enumerate every branch-exclusive commit and changed-path blob state from each live ref;
2. archive any blob or evidence-only source state that would otherwise become unreachable, including the pre-W3 instrument/parser/workflow lineage;
3. add a `run-heads.tsv` covering every workflow run cited by PR #439 and its durable evidence/diagnosis records;
4. add an explicit ref disposition and a live-ref completeness verifier equivalent in purpose to the PR #416 archive guard;
5. pass that verifier against the then-current remote refs immediately before any deletion.

Until those gates are committed and passed, every PR #439 evidence ref remains retained.

The durable result-bearing six-seed W3 data is stored separately in `../pr439-w3/` so the expiring Actions artifacts are no longer the only copy of the per-seed result table.
