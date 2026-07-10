# Tactical Presets & AI-Manager Selection Specification #26 — Section 6: Performance Budget

**Created:** July 8, 2026
**Last Updated:** July 8, 2026 (v0.1)
**Version:** 0.1
**Status:** APPROVED

---

The gate predicate is a handful of integer compares per stride tick per team (~10 Hz × 2). The
scoring/ladder functions run only at fired decision points — roughly 20–30 times per 90-minute
match — and are a few dozen float ops each. Application costs are the existing `SetTeamTactic`
staging + stride commit, already budgeted by #21's machinery. Zero per-tick allocation (profiles,
state, catalogue all pre-built). This is the cheapest subsystem in the project by orders of
magnitude; no budget line item. Ratified per #18 KD-2 at implementation.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-08 | — | Initial budget analysis. |
#endregion
