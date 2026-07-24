# Discipline & Suspensions #44 — Section 6: Performance

**Created:** July 24, 2026
**Last Updated:** July 24, 2026 (v0.1 — initial)
**Version:** 0.1
**Status:** APPROVED

---

## 6.1 Cadence

#44 adds **no engine-side work**: the fold is fed by the same per-tick tap read the root already
performs for #37's class of consumer, and its per-record work is a switch + at most two integer
updates. Off-match, #44 runs per-event only (a filter at selection, a decrement per played
fixture, a boundary sweep per season).

## 6.2 Per-operation cost

- **`OnTapRecord`** — O(1): an ordinal switch, an occupancy lookup, one or two integer adds. Card
  and substitution events are rare (a handful per match), so the per-tick cost is effectively the
  ignore branch.
- **`FilterAvailable`** — O(squad size) value copy, once per managed-fixture selection.
- **`OnClubFixturePlayed`** — O(active-ban entries for the club), typically 0–2.
- **Boundary sweep** — O(entries), once per season.

## 6.3 Save cost

O(entries) integer serialization, once per save/load — the smallest sub-blob in the family
(three ints per carded player). Empty at genesis.

## 6.4 Budget

Negligible on every axis; no RNG stream, no allocation in the fold's steady state beyond the
filter's value copy. Not a per-tick budget concern.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-24 | — | Initial §6 (cadence, per-operation cost, save cost, budget), promoted from design supplement v0.3. Status IN REVIEW. |
#endregion
