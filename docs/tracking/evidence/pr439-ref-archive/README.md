# PR #439 Evidence-Ref Archive — Phase 1

> **Created:** September 23, 2026
> **Purpose:** Freeze the live `evidence/pr439-*` ref topology before any branch cleanup.
> **Status:** **NO DELETION AUTHORIZED.** This is the first archival phase only.

`ref-heads.tsv` records all 16 live `evidence/pr439-*` refs and their exact remote head SHAs as observed after PR #439 merged.

This directory deliberately does **not** yet claim PR #416-style branch-exclusive blob completeness. Before any `evidence/pr439-*` ref is deleted, a follow-up live-ref completeness pass must enumerate branch-exclusive commits and changed-path blob states, archive any otherwise-unreachable material, and record an explicit deletion disposition. Until that gate is committed and passed, every PR #439 evidence ref remains retained.

The durable result-bearing six-seed W3 data is stored separately in `../pr439-w3/` so the expiring Actions artifacts are no longer the only copy of the per-seed result table.
