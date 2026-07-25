# Season & Competition Loop Specification #30 — Appendices

**Created:** July 22, 2026
**Last Updated:** July 25, 2026 (v0.3 — ERR-030-010 Appendix C venue correction, found at #30 T0)
**Version:** 0.3
**Status:** APPROVED
**Source:** `docs/tracking/season-competition-loop-design.md` v0.2

---

## Appendix A — Constant catalogue (`SeasonLoopConstants`)

All values proposed; magnitudes are illustrative pending a Stage-2 balance pass (the #21 §9.2
precedent — the spec's contract is the shapes/directions, the `[GT]` numbers are tunable).

| Constant | Tag | Value | Meaning |
|---|---|---|---|
| `SEASON_SAVE_FORMAT_VERSION` | `[FIXED]` | 2 | outer season-frame version (bumped 1 → 2; owned by `SeasonSaveConstants`) |
| `SEASON_STATE_FORMAT_VERSION` | `[FIXED]` | 1 | the season sub-blob's own version (new) |
| `WIN_POINTS` | `[GT]` | 3 | points for a win |
| `DRAW_POINTS` | `[GT]` | 1 | points for a draw |
| `LOSS_POINTS` | `[GT]` | 0 | points for a loss |
| `DEFAULT_OBJECTIVE_POSITION` | `[GT]` | (per-club) | the board's "finish at or above" target |
| `DOMAIN_TAG_SEASON_LOOP` | `[CROSS]` | `0x22` | mirror of `DeterministicSimConstants.DOMAIN_TAG_SEASON_LOOP` (#16 §3.4; allocated at approval) |
| `SUBSYSTEM_ORDINAL_SEASON_LOOP` | `[CROSS]` | 84 | mirror of `SubsystemOrdinals.SeasonLoop` (#16; allocated at approval) |

`WIN/DRAW/LOSS_POINTS` are the association-football 3/1/0 convention (§8.2); a `[GT]` catalogue value,
not a physical constant, so a rules variant (e.g. 2/1/0) is a config change.

## Appendix B — Season-state sub-blob byte layout (KD-1 / §3.6)

The season block, in order (all via `CanonicalSerializer`; every length prefix via an overflow-safe
`ReadCount`, `0 ≤ n ≤ remaining`):

| # | Field | Type | Notes |
|---|---|---|---|
| 1 | version | u32 | `SEASON_STATE_FORMAT_VERSION`; gate first (F3) |
| 2 | seed | u64 | the season seed |
| 3 | seasonNumber | i32 | multi-season counter |
| 3a | managedClubId | i32 | the human manager's club (KD-9 / FR-SN-013b) |
| 4 | clubCount | count | `ReadCount` |
| 5 | clubIds[] | i32 × clubCount | the roster world |
| 6 | fixtureCount | count | `N·(N−1)` |
| 7 | fixtures[] | (round i32, home i32, away i32, played u8) × fixtureCount | the serialized schedule (KD-5) |
| 8 | calendar | (nextRoundIndex i32, roundCount count, roundToDay[] u32) | the cursor (KD-4); day values are `uint` to match `WorldStore.CurrentWorldTick` |
| 9 | tableRowCount | count | = clubCount |
| 10 | tableRows[] | (clubId, P, W, D, L, GF, GA, GD, Pts) i32 × 9 × rows | ClubId order |
| 11 | board | (targetPosition i32, jobSecurityPerMille i32) | the objective + security |

> **Row 11 pinned at #30 T1 (ERR-030-011).** The v0.1 row left the representation open as
> `jobSecurity f32/u8` — neither of which the implementation uses. #30 T0 resolved `BoardState` to an
> integer per-mille in `[0, JobSecurityScale]`, following the integer-arithmetic convention every later
> management spec standardized on (#41's AR-1 moved that spec's whole model float → integer per-mille;
> #40 uses integer currency; #33 uses per-mille scalars), and recorded the row as a back-prop
> candidate. T1 is where it became a real byte layout, so the row is now pinned to `i32`. Integers also
> make the sub-blob round-trip exact with no NaN gate.


The outer `SeasonSaveCodec` frame nesting this block:
`SEASON_SAVE_FORMAT_VERSION (u32) → matchPresent flag (u8) → [len u32]world → [len u32]season →
([len u32]match iff matchPresent)`. Trailing bytes after the declared content ⇒ throw (F3).

## Appendix C — Worked 4-club round-robin schedule

`clubIds = [10, 11, 12, 13]`, identity permutation, circle method (M = 4, index 0 pinned):

| Round | Fixture 1 | Fixture 2 |
|---|---|---|
| 0 | 10 v 13 | 11 v 12 |
| 1 | 12 v 10 | 11 v 13 |
| 2 | 10 v 11 | 12 v 13 |
| 3 | 13 v 10 | 12 v 11 |
| 4 | 10 v 12 | 13 v 11 |
| 5 | 11 v 10 | 13 v 12 |

> **Corrected at #30 T0 (ERR-030-010).** Rounds 1 and 4 previously read `10 v 12 / 13 v 11` and
> `12 v 10 / 11 v 13` — the venues were inverted because this table (and the identical §3.7 one) was
> hand-derived without applying §3.1's round-parity venue rule. The **pairings were always right**;
> only the home/away side of the odd first-leg round (and its second-leg mirror) changed, so the set
> of 12 ordered pairs below is unchanged. Measured at the Stage-2 target size of 20 clubs, the
> unparried form gives the pinned club **all 19** first-leg fixtures at home; with parity every club
> lands in 8–10 of an ideal 9–10.

- 12 fixtures = `N·(N−1) = 4·3` (FR-SN-002).
- Every ordered pair appears once: `{10v13, 10v12, 10v11, 13v10, 12v10, 11v10, 11v12, 13v11, 12v13,
  12v11, 11v13, 13v12}` — all 12 ordered pairs of distinct clubs.
- Each club appears exactly once per round (FR-SN-003): round 0 = {10,13,11,12}, etc.
- Rounds 0–2 are the first leg, 3–5 the second leg with venues reversed (round offset `M−1 = 3`).

## Appendix D — Table tie-break worked example

Two clubs tied through the first three keys:

| Club | P | W | D | L | GF | GA | GD | Pts |
|---|---|---|---|---|---|---|---|---|
| 10 | 3 | 2 | 0 | 1 | 5 | 3 | +2 | 6 |
| 11 | 3 | 2 | 0 | 1 | 5 | 3 | +2 | 6 |

Points equal (6=6) → GD equal (+2=+2) → GF equal (5=5) → **ClubId ascending**: 10 orders above 11.
The final key is `ClubId`, and clubIds are unique (F2 keeps each club to one row), so the comparator
is a **total order** — no two rows ever compare equal (FR-SN-007).

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-22 | — | Initial appendices: constant catalogue, season-state byte layout, worked 4-club schedule, tie-break worked example. |
| 0.2 | 2026-07-22 | — | Section-file PASS-1: whole-round resolution (KD-9 / FR-SN-012/013a/013b / §3.4 / ManagedClubId), API-name corrections (`RunTick`→`MatchEnded`, `ResolveByClubId`), `uint` world-day, KD-collision + label reconciliation. See section-9 §9.3. |
| 0.3 | 2026-07-25 | — | **ERR-030-010** (found at #30 T0 implementation): Appendix C rounds 1 and 4 venue-corrected — the table was hand-derived without §3.1's round-parity venue rule. Pairings unchanged, so the 12-ordered-pair completeness bullet is unaffected; justification (20-club venue distribution) recorded inline. |
#endregion
