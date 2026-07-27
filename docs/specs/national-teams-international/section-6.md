# National Teams & International Management #36 — Section 6: Performance

**Created:** July 27, 2026
**Last Updated:** July 27, 2026 (v0.1 — initial section-file set)
**Version:** 0.1
**Status:** IN REVIEW

---

## 6.1 Loop classification

#36 runs on the **world tick** and at **#30's resolve→configure seam** only. It has **no hot path**:
nothing executes on the 10 Hz tactical or 60 Hz physics loops, no #36 type is reachable from
`MatchEngine.RunTick`, and #36 feeds no digest (FR-NT-003, asserted structurally by T-NT-BOUND-001).

Three cadences matter, and one of them is unusual enough to call out:

- **The window advance is per world day** — a single cursor comparison on the overwhelming majority of
  days, since most days are not window transitions.
- **The availability filter runs per resolved fixture**, on both clubs. It is O(squad) and executes on
  every fixture whether or not a window is open, so its **outside-a-window fast path matters more than
  its inside-a-window cost**.
- **`NationOf` is called per eligibility evaluation**, which at selection time is once per player in the
  pool. It is a pure four-round mix plus one dictionary probe — but it is the only #36 operation whose
  call count scales with **pool size**, and selection touches the whole pool.

## 6.2 Cost profile

| Operation | Cadence | Work |
|---|---|---|
| `AdvanceWindowDay` — **no transition** | per world day | 1 cursor comparison, then return; the common case |
| `AdvanceWindowDay` — transition | ~2–6× per season | + the window open/close bookkeeping |
| `FilterAvailable` — **outside a window** | per resolved fixture, per club | **1 comparison, then return the squad unchanged** — the fast path that dominates |
| `FilterAvailable` — inside a window | per fixture during ~2–6 windows | O(squad) with a call-up set probe per player |
| `NationOf` — unpinned | per eligibility evaluation | 4 SplitMix64 rounds + a ≤ `NATION_COUNT` walk; **no allocation, no state** |
| `NationOf` — pinned | rare | 1 dictionary probe |
| `SelectCallUps` | once per window open | O(pool log pool) for the sort, then a capped greedy walk |
| `OnPlayerReKeyed` | per transfer | 1 derivation, 2 table writes, 2 migrations |
| `TryResolveNationSquad` *(deep)* | per international fixture | O(squad) resolution from #27's pool |
| `Encode` / `Decode` | once per save / load | O(call-ups + pins + minutes), all bounded |

**The one cost worth designing around is `SelectCallUps`**, and it is bounded by its cadence rather than
its complexity: a sort over the managed pool, **twice to six times a season**. At a 20-club league of 25
players that is ~500 records sorted a handful of times per year — irrelevant. It would only matter if
selection ran per day, which is precisely why it is gated on a window **open transition** rather than
evaluated continuously.

**`NationOf` has no cache, deliberately.** Caching a derived value is how the second-truth problem KD-1
avoids gets re-introduced through the back door: a cache keyed by `PlayerId` would go stale at exactly the
event — a re-key — that the pin table exists to handle, and it would go stale **silently**. The
derivation is four integer rounds; it does not need one.

**Allocation: zero** on the window-advance and unpinned-`NationOf` paths. `FilterAvailable` allocates its
reduced squad (it is a value-copy reduction by contract, FR-NT-016), and `SelectCallUps` allocates its
sort buffer once per window — both off any loop.

## 6.3 Budget ceilings

| Budget | Value | Tag |
|---|---|---|
| `NT_BUDGET_NATION_OF_US` — one `NationOf` call | 1 µs | `[GT]` |
| `NT_BUDGET_FILTER_US` — one `FilterAvailable` call | 20 µs | `[GT]` |
| `NT_BUDGET_SELECT_MS` — one `SelectCallUps` over the managed pool | 5 ms | `[GT]` |

**These are ceilings, not measurements.** No certified number exists for #36 and none is invented here: a
certified figure must come from the pinned Windows 11 / Unity 6000.4.9f1 / DX11 / Mono host per
`certification-platform.md`, and #36 has no implementation to measure. The ceilings are generous so a
first measurement either passes comfortably or reveals something genuinely wrong — the
`CertifiedPerfBaseline` PENDING posture applied to a spec that has not been built.

**`NT_BUDGET_SELECT_MS` is in milliseconds deliberately**, for the same reason #46's query budget is: a
few-times-a-season operation should not carry a loop-step budget. **`NT_BUDGET_NATION_OF_US` is the one
to measure first**, because it is the only figure multiplied by pool size.

## 6.4 Memory

- **`NationPin`**: 8 bytes per entry, and — the property worth stating — **bounded by transfer volume,
  not by pool size** (FR-NT-010). A career with a thousand transfers holds ~8 KB; an untransferred world
  holds **nothing at all**.
- **`CallUp`**: 12 bytes per entry, bounded by `NT_SQUAD_SIZE` × active national teams.
- **`IntlMinutes`**: 8 bytes per entry, **empty at the minimal tier** and dropped at zero.
- **Nationality for every unpinned player**: **zero bytes**. This is KD-1's payoff stated as a number —
  the alternative (a `PlayerRecord` field) would have cost a field per player *and* a rewrite of every
  existing save.

Total resident footprint is **well under 100 KB** in any realistic career, and the save sub-blob is the
same order (Appendix B). #36 therefore needs none of the `SAVE_SIZE_BUDGET` compression machinery #22
carries — recorded so its absence is not read as an omission.

**Nothing in #36 grows with career length except the pin table**, and that grows with **transfers**, which
are themselves bounded per window by #31. FR-NT-013's drop-on-retire is what keeps even that from
outliving its pool — remove it and the pin table is the one #36 collection that would grow monotonically
across a twenty-season career.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-27 | — | Initial §6 (world-tick + seam classification with the three cadences named, cost profile with the outside-a-window fast path called out as the dominant one, the deliberate absence of a `NationOf` cache and why it would re-introduce the second-truth problem, `[GT]` ceilings with a no-certified-number caveat and the ms-vs-µs distinction, memory with KD-1's payoff stated as *zero bytes for every unpinned player*). Status IN REVIEW. |
#endregion
