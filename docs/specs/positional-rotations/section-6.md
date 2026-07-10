# Positional Rotations Specification #25 — Section 6: Performance Budget

**Created:** July 8, 2026
**Last Updated:** July 8, 2026 (v0.1)
**Version:** 0.1
**Status:** APPROVED

---

Per 10 Hz heartbeat per team: ≤ `ROTATION_MAX_PAIRS_PER_FAMILY` (8) pair evaluations, each four
distance computations (four sqrt worst-case; square-compare optimisation is valid for the
predicate's ≥ form and left to implementation) + counter arithmetic. With `Off`, zero evaluations
(dial gate first). No allocation: pair state is a fixed per-team array sized by the family's table;
the catalogue is static readonly. Comfortably inside #12's existing budget — the supplement's
O(n²)-at-10 Hz concern is retired by KD-1's static table, not by micro-optimisation. Ratified per
#18 KD-2 at implementation; FR-PO-052 covers regressions.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-08 | — | Initial budget analysis. |
#endregion
