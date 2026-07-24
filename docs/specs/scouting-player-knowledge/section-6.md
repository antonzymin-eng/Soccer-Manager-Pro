# Scouting & Player Knowledge #32 — Section 6: Performance

**Created:** July 24, 2026
**Last Updated:** July 24, 2026 (v0.1 — initial)
**Version:** 0.1
**Status:** APPROVED

---

## 6.1 Cadence

#32 has **no per-tick match-loop cost** — it is off-pitch, on the world tick only (KD-7). At minimal
the world-tick scouting slot is a **null seam**, so a day costs **zero** #32 tick work. At deep,
`AdvanceScoutingDay` is one integer increment + at most one band-up per day (a single map write) —
bounded by `MAX_ACTIVE_ASSIGNMENTS = 1`.

## 6.2 Per-operation cost

- **`ResolveBand`** — one dial check, one integer division (own-squad), one map lookup.
- **`EstimateFor`** — 31 × (one table read + at `w > 0` one stateless keyed draw + a handful of
  integer ops); no allocation beyond the returned value type. At `w == 0` (every minimal read) the
  per-attribute cost is a table read and two assignments — no draw (FR-SC-012).
- **`AssignScout` / `CancelAssignment`** (deep) — a fixed gate chain + one field write.
- **`RankByEstimate`** (deep) — O(candidates × position-relevant attributes) integer sums + one
  sort; bounded by the caller-supplied candidate list; on demand, off the match tick.

## 6.3 Save cost

Encoding/decoding the scouting sub-blob is O(scouted players) integer serialization, once per
save/load — the #41/#33/#34 sub-blob-cost class. Empty (a version tag + zero counts) at minimal.
Because estimates are derived, the block never scales with `ATTRIBUTE_COUNT` (KD-1).

## 6.4 Budget

Off-pitch, at most once-per-day for the assignment step and once-per-view for `EstimateFor` — nowhere
near a per-tick budget concern. **No RNG stream registered at minimal** (KD-3), so no stream-advance
cost; the deep keyed draws are stateless computations bounded per view (31 per full `KnownPlayer`).
View-model construction for a whole shortlist screen (#38, future) is O(players shown × 31) integer
work — presentation-cadence, not sim-cadence.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-24 | — | Initial §6 (cadence, per-operation cost, save cost, budget), promoted from design supplement v0.3. Status IN REVIEW. |
#endregion
