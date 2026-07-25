# Competition Structure #43 — Section 6: Performance

**Created:** July 24, 2026
**Last Updated:** July 24, 2026 (v0.1 — initial)
**Version:** 0.1
**Status:** APPROVED

---

## 6.1 Cadence

#43 has **no per-tick match-loop cost** — it is off-pitch, driven at fixture-day / boundary-roll
cadence. At minimal, #43 executes **no code on the season path** (the instance-0 binding is inert —
§4.3), so a season costs zero #43 work. At deep, the costs are per-event: a draw at round
completion, the transform once per season boundary, the merged-view query once per fixture-day
advance.

## 6.2 Per-operation cost

- **`DrawRound`** — O(n) keyed draws + swaps for n entrants (a 64-entrant cup round 0 = 63 keyed
  computations); stateless, allocation = the one drawn array.
- **Round-robin instance generation** — delegates to #30's `FixtureScheduler.Generate` (its
  documented cost, per instance, once per season).
- **`ApplyPromotionRelegation`** — O(divisions × swap count) integer membership updates, once per
  season boundary.
- **Merged fixture-day view** — O(instances) min-scan over precomputed per-instance mappings; the
  slotting itself is computed once at season scheduling time, not per query (§3.5).
- **Bracket coherence gates** — O(round size) at mutation/decode.

## 6.3 Save cost

Encoding/decoding the competition sub-blob is O(instances × entrants + bracket rounds) integer
serialization, once per save/load — the sibling sub-blob cost class. Nearly empty at minimal (a
version tag + one binding row).

## 6.4 Budget

Off-pitch and event-driven — nowhere near a per-tick budget concern. **No RNG stream registered at
minimal**; deep keyed draws are bounded per round completion. Bracket view-model construction for
#38 (future) is O(rounds × round size) value copies at presentation cadence.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-24 | — | Initial §6 (cadence, per-operation cost, save cost, budget), promoted from design supplement v0.3. Status IN REVIEW. |
#endregion
