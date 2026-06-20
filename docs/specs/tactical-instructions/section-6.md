# Tactical Instructions Specification #21 — Section 6: Performance

**Created:** June 20, 2026
**Last Updated:** June 20, 2026 (v0.3 — PASS-2 fix pass)
**Version:** 0.3
**Status:** APPROVED (June 20, 2026)

---

## 6.1 Loop placement

This layer has **no per-tick hot path of its own**. The translation maps (§3.1) run **once per
tactic-change** (boot + occasional touchline shout), not per tick (FR-TI-025). The only per-tick cost is
the multiplier insertion in #8 `UtilityScorer`/`OptionGenerator`, which is accounted to #8's budget, not
here.

## 6.2 Per-tactic-change cost (cold path)

| Operation | Cost |
|---|---|
| Materialize `TeamTactic` + `PlayerTactic[22]` | one-time / on-change; pre-allocated, no per-tick alloc |
| Run translation maps (≤ 4 team maps + per-agent role/duty resolve) | O(22), bounded, off the hot path |
| Write routing fields on the 5 snapshots | O(22), plain field writes |

Zero managed allocation on any path (FR-TI-002, #18 §3.7). Structs are value types passed by `ref`/`in`.

## 6.3 Per-tick added cost (charged to #8)

The `UtilityScorer` product gains **five** scalar multiplies per scored option
(`role × mentality × duty × instruction × tempo`), all array/lookup reads of pre-resolved values.
Two **new option-generation branches** also run per tick in `OptionGenerator`: the `FocusPlay`
lateral-preference term and the `Tempo` breadth widening (PASS-2 M-3 — these were not in the v0.1
estimate). Both are bounded per-agent passes over the already-generated candidate set (no new
allocation, no new outer loop). Combined estimate still < 0.02 ms added to #8's existing per-tick budget
at 22 agents — to be measured against the pinned host once Phase D composes (#18 / FR-PO-052).

## 6.4 Memory

`TeamTactic` ≈ 16 bytes ×2 teams; `PlayerTactic` (incl. `PlayerInstructions`) ≈ 24 bytes ×22 ×2 ≈ ~1 KB
working set; `RoleWeightModifiers` table = `|PlayerRole| × |ActionType|` floats (curated subset × 7 ≈
< 1 KB static). Total < 3 KB. No growth on the hot path.

## 6.5 Budget statement

This spec asserts **no independent per-tick budget** (it has no per-tick loop). It asserts a cold-path
bound (O(22) per tactic-change, zero-alloc) and a ≤ 0.01 ms contribution to #8's per-tick budget,
verified at Stage 1 against the pinned host.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-06-20 | — | Cold-path-only cost model; ≤0.01 ms charged to #8; <3 KB working set. |
| 0.3 | 2026-06-20 | — | PASS-2 fix pass (M-3): §6.3 now accounts for the five utility multiplies + the two new `OptionGenerator` branches (Tempo breadth, FocusPlay); estimate widened to <0.02 ms. |
#endregion
