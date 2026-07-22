# Season & Competition Loop Specification #30 — Section 6: Performance

**Created:** July 22, 2026
**Last Updated:** July 22, 2026 (v0.2 — section-file PASS-1 fixes, §9.3)
**Version:** 0.2
**Status:** APPROVED
**Source:** `docs/tracking/season-competition-loop-design.md` v0.2

---

## 6.1 Cadence — world tick, not the hot path

The entire loop runs at **world-tick cadence** (`WorldClock`: one `worldTick` = one calendar day) or
on explicit host commands (advance, play, save, roll) — **never** the 10 Hz tactical / 60 Hz physics
match loops (§1.2 / FR-SN-025). The CLAUDE.md zero-allocation, struct-only, `ProfilerMarker` game-loop
rules govern the 60 Hz path and **do not apply** here, exactly as they do not for `WorldStore`,
`SeasonSaveManager`, `TeamTacticFileLoader`, or `RosterGenerator`. `SeasonLoop` may allocate, use
`new`, throw, and hold plain classes.

The 60 Hz cost appears *inside* `AdvanceAndPlayNextRound` (KD-9), which resolves a round's `N/2`
fixtures. This is the load that motivates the quick-sim seam (§3.4.1): the **quick-sim** identity runs
exactly **one** full `MatchEngine` match (the managed club's) per round + `N/2−1` cheap deterministic
draws, so a 20-club season is ~380 cheap resolutions + ~38 full matches; the **full-sim** minimal
identity runs all `N/2` matches per round (~380 full matches/season — correct but expensive, the
reason quick-sim exists). Each full match's cost is the match engine's, governed by its own FR-PO-052
budget; the season loop's own per-fixture work (`ApplyResult`, one event record) is trivially bounded.

## 6.2 Sizing

- **Fixtures:** `N·(N−1)` for `N` clubs — a 20-club league is 380 fixtures, a few KB serialized. The
  fixture list is generated once per season and serialized (KD-5), read on load; no per-day
  regeneration.
- **Table:** `N` rows; `OrderedView()` is an `O(N log N)` sort of a copied `N`-row array per call —
  called on demand (a view request), not per tick. For `N ≤ 32` (a comfortable Stage-2 ceiling) this
  is immaterial.
- **Day advance:** `AdvanceToNextFixtureDay` runs one `WorldStore.AdvanceDay()` per intervening day —
  the world loop's own bounded per-day cost (living-world §6); the season loop adds only the loop and
  a cursor bump.
- **Save:** three blob captures + one atomic write, off the hot path (the `SeasonSaveManager`
  precedent). The season block is small (fixtures + `N` table rows + calendar + board).

## 6.3 No perf gate at this spec

There is no per-tick perf budget to certify here — the season loop is not on a real-time loop. The
FR-PO-052 certified perf baseline covers the match engine the loop *invokes*, not the loop itself.
The §5.7 capstone scenario, once wired, measures nothing beyond determinism (it is a correctness /
determinism lock, not a perf gate).

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-22 | — | Initial performance analysis: world-tick cadence, sizing, no per-tick perf gate. |
| 0.2 | 2026-07-22 | — | Section-file PASS-1: whole-round resolution (KD-9 / FR-SN-012/013a/013b / §3.4 / ManagedClubId), API-name corrections (`RunTick`→`MatchEnded`, `ResolveByClubId`), `uint` world-day, KD-collision + label reconciliation. See section-9 §9.3. |
#endregion
