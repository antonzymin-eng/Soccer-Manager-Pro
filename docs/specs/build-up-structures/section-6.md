# Scripted Build-Up Structures Specification #24 — Section 6: Performance Budget

**Created:** July 8, 2026
**Last Updated:** July 8, 2026 (v0.1)
**Version:** 0.1
**Status:** IN REVIEW

---

Per 10 Hz heartbeat per team: one zone classification (a handful of float compares), one window
decrement, and — when active — one table lookup + vector add per outfield slot (≤10). No sqrt, no
allocation (tables are static readonly arrays; state is a per-team struct). Estimated well under a
microsecond per team-tick on the pinned Stage-0 host class; absorbed by #12's existing budget with
no line-item change. Ratified per #18 KD-2 at implementation; FR-PO-052 gate covers regressions.
With `None` the stage exits on the dial check — default-cheap as well as default-neutral.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-08 | — | Initial budget analysis. |
#endregion
