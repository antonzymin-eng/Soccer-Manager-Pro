# Personalities, Morale & Squad Dynamics #33 — Section 6: Performance

**Created:** July 23, 2026
**Last Updated:** July 23, 2026 (v0.1 — initial authoring)
**Version:** 0.1
**Status:** APPROVED

---

## 6.1 Cadence

#33 runs on the **world tick** (one `worldTick` = one calendar day) at #30's slot 3 — **not** the 10 Hz/60 Hz
match loops (FR-HS-001). Per world day it performs, per player, one `AdvanceHumanSystemsDay` (a handful of
integer per-mille operations + the F6 guard) and, per club, one `AdvanceRelationshipDay` pass over its
club-scoped pairwise set. There is **no** per-frame or per-match-tick cost; the off-pitch cadence is the
`WorldStore.AdvanceDay()` cadence, orders of magnitude below the match loop.

## 6.2 Cost shape

- **Per-player daily:** O(1) integer arithmetic. No allocation in the daily step (structs passed `ref`/`in`).
- **Per-club pairwise:** the store is **club-scoped** at the minimal tier — O(squad²) pairs bounded by
  `CLUB_SQUAD_SIZE²` (≤ 25² = 625 per club), touched once per day; a deterministic pass, no draw. Cross-club
  relationships (the O(global²) case) are a **recorded deep-tier extension**, deliberately not built (§7), so
  the minimal cost stays per-club-bounded.
- **Clique/chemistry derivation:** computed **on demand** for a UI/consumer read, not every tick (KD-4 — it
  is a derived read, persisted nowhere), so it adds no daily cost.
- **The #22 route:** at the minimal tier the view is **empty** (KD-8), so `RouteIntoLivingWorld` makes zero
  `SetPlayerEdgeMirror` calls; when real canon flows, it is one O(1) mirror write per active club-scoped pair
  per day, bounded as above.

## 6.3 Save cost

The `HUMAN_SYSTEMS_SAVE_FORMAT_VERSION` sub-blob is per-player fixed-width state + the per-club pairwise set —
serialized once per save alongside the other season sub-blobs (no RNG cursor, KD-7). No per-tick serialization.

## 6.4 Determinism / perf gate

#33 is deterministic and draw-free; its per-day work is negligible against the season loop's fixture
resolution. No dedicated perf baseline is required at the minimal tier (the FR-PO-052 match-tick budget is
untouched — #33 never runs on the match tick). A deep-tier cross-club relationship model, if built, would
carry its own perf note.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-23 | — | Initial §6 (world-tick cadence, per-club-bounded cost, save cost). Status IN REVIEW. |
#endregion
