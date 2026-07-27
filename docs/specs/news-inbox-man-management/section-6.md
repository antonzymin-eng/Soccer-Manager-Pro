# News, Inbox & Man-Management #46 — Section 6: Performance

**Created:** July 27, 2026
**Last Updated:** July 27, 2026 (v0.1 — initial section-file set)
**Version:** 0.1
**Status:** IN REVIEW

---

## 6.1 Loop classification

#46 is **command-driven and root-projected**, with **no position in `RunWorldTickInFixedOrder`** at all
(FR-NW-001). It has no hot path: nothing executes on the 10 Hz tactical or 60 Hz physics loops, no #46
type is reachable from `MatchEngine.RunTick`, and #46 feeds no digest.

That makes #46 the cheapest spec in the wave by construction — but it moves the cost somewhere unusual,
so the classification is worth stating precisely:

- **Appends are per producer-event**, not per day or per frame. A season produces on the order of a few
  hundred (one per fixture, plus discipline, board and transfer items).
- **Queries are per user action**, and are the only O(items) operation. They run when a human opens a
  screen — off any loop, at human cadence.
- **The drain is per player per day**, at #30's step 3, and returns `false` for the overwhelming
  majority. Absence must be cheap and must not be exceptional, which is why F7 classifies it as a
  non-failure.

## 6.2 Cost profile

| Operation | Cadence | Work |
|---|---|---|
| `Append` — common | per producer-event (~hundreds/season) | 2 enum checks, 1 arity check, 1 cursor increment, 1 insert, 1 read-key compaction pass |
| `Append` — log full | rare | + 1 eviction (`INBOX_MAX_ITEMS`) |
| `Query` | per user action | O(`INBOX_MAX_ITEMS`) filter + value copies; **allocates the result** — the one allocating path, and it is off any loop |
| `IsRead` | per item rendered | 1 comparison, then a scan of the bounded exception set |
| `MarkRead` / `MarkAllReadBefore` | per user action | 1 lookup / 1 `Max` + a bounded prune |
| `TryTalkToPlayer` | per user command | 2 range checks, 2 predicate checks, 1 append |
| `TryTakePendingDelta` — **no row** | once per player per day | 1 lookup over a bounded list; **the common case** |
| `Encode` / `Decode` | once per save / load | O(items + read keys + pending deltas), all bounded |

**Every collection #46 holds is bounded by a `[GT]` constant**, which is the structural reason it has no
scaling risk: the log by `INBOX_MAX_ITEMS` **and** `INBOX_RETENTION_DAYS`, the exception set by the log
(FR-NW-018), and pending deltas by clear-on-delivery and drop-on-departure. There is **no unbounded
collection anywhere in the spec**, and nothing grows with career length.

**That last property is maintained deliberately, not accidentally.** Remove any one of four rules — the
retention window, the item cap, the log-bounded exception set, or never-writing-a-zero-delta — and #46's
APPEND-only blob grows without bound across a twenty-season career. Each is a MUST rather than an
optimisation for exactly that reason.

**Allocation: zero on the append and drain paths.** `Query` allocates its result, by design: it is a
presentation call at human cadence, not a loop step. `Append` copies the caller's payload array (§3.1),
which is a bounded copy and the price of not retaining a live handle.

## 6.3 Budget ceilings

| Budget | Value | Tag |
|---|---|---|
| `INBOX_BUDGET_APPEND_US` — one `Append` | 10 µs | `[GT]` |
| `INBOX_BUDGET_QUERY_MS` — one full-log `Query` | 2 ms | `[GT]` |
| `INBOX_BUDGET_DRAIN_US` — one `TryTakePendingDelta` | 2 µs | `[GT]` |

**These are ceilings, not measurements.** No certified number exists for #46 and none is invented here: a
certified figure must come from the pinned Windows 11 / Unity 6000.4.9f1 / DX11 / Mono host per
`certification-platform.md`, and #46 has no implementation to measure. The ceilings are set generously so
a first measurement either passes comfortably or reveals something genuinely wrong — the
`CertifiedPerfBaseline` PENDING posture applied to a spec that has not been built.

**`INBOX_BUDGET_QUERY_MS` is in milliseconds, not microseconds, and that is deliberate.** A query is a
screen-open operation at human cadence, and holding it to a loop-step budget would be a false constraint.
**`INBOX_BUDGET_DRAIN_US` is the one to measure first**: it is multiplied by every player #30 iterates,
every day, for the whole career — the same reasoning #35 records for its own drain.

## 6.4 Memory

Per item: two bytes of tags, three ints, and a bounded payload — on the order of **40 bytes**. At
`INBOX_MAX_ITEMS = 200` the log is ~8 KB, the exception set is bounded by it, and pending deltas are a
handful of 12-byte records. #46's total resident footprint is **well under 100 KB**, and the save sub-blob
is the same order (Appendix B).

That is why #46 does not need — and deliberately does not have — the `SAVE_SIZE_BUDGET` compression or
cold-store machinery #22 carries. Recorded so a later reviewer does not read its absence as an omission;
§7.4 R-1 records the one scenario (career-long archival history) that would change the answer, and why the
right response there is a compact aggregate rather than a raised bound.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-27 | — | Initial §6 (no-tick-slot classification with the three unusual cadences named, cost profile, the four rules that keep the footprint bounded, `[GT]` ceilings with the deliberate ms-vs-µs distinction for the query budget and a no-certified-number caveat, memory). Status IN REVIEW. |
#endregion
