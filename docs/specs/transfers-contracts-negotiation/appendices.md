# Transfers, Contracts & Negotiation #31 — Appendices

**Created:** July 23, 2026
**Last Updated:** July 23, 2026 (v0.3 — AR-3 fix pass; prior v0.2 AR-1, v0.1 initial)
**Version:** 0.3
**Status:** APPROVED

---

## Appendix A — Constant catalogue

| Constant | Tag | Value | Notes |
|---|---|---|---|
| `TRANSFERS_SAVE_FORMAT_VERSION` | `[FIXED]` | 1 | the transfers sub-blob's own version gate (KD-4). |
| `PERMILLE_DENOM` | `[FIXED]` | 1000 | integer per-mille denominator (the #33/#40 posture). |
| `VALUE_PER_RATING_POINT` | `[GT]` | illustrative | integer currency per mean-attribute point (§3.1); pinned at the Stage-2/3 balance pass (#21 G2). |
| `AgeCurvePermille[age]` | `[GT]` | illustrative | age→value multiplier table: neutral peak band (`1000‰`), decline past ~30, young-unproven discount (master plan §4.3 shape); balance-pass-pinned. |
| `SUMMER_WINDOW_LENGTH_DAYS` | `[GT]` | illustrative | length of the single minimal transfer window (KD-6); balance-pass-pinned. |
| `DEFAULT_CONTRACT_SEASONS` | `[GT]` | illustrative | length of a career-start seeded contract (§3.8); `> 0` (F7-valid); balance-pass-pinned. |
| `DEFAULT_WAGE_*` (`DefaultWageFor`) | `[GT]` | illustrative | the career-start seeded wage, a `[GT]` function of the seeded player's #27 record (§3.8); `≥ 0` (F6-valid); balance-pass-pinned. |
| `SEASON_START_WORLD_DAY` | `[CROSS]` | #30 | the season-start world day the window opens on; sourced from #30's `SeasonCalendar` (read-only, KD-6). |
| `TRANSFERS_STAFF_MULT_IDENTITY` | `[FIXED]` | 1000 | the `staffMult` identity (`×1000‰`) until #34 produces a value (KD-3). |
| `TRANSFERS_PERSONALITY_MULT_IDENTITY` | `[FIXED]` | 1000 | the `personalityMult` identity until #33 is read (deep, KD-1). |
| `TRANSFERS_NEED_MULT_IDENTITY` | `[FIXED]` | 1000 | the `needMult` identity (`×1000‰`) at minimal; the deep tier's club-need bias (KD-1). |

**Tag note:** the `[GT]` valuation/window magnitudes are **illustrative pending a Stage-2/3 balance pass** —
the reviewed contract is the shapes/directions (monotone age curve, ceiling-gated affordability, one-window
minimal), not the numbers (the #21 G2 / #40 / #41 / #33 precedent). `SEASON_START_WORLD_DAY` is `[CROSS]` from
#30 (copied verbatim, read-only) — the window is derived, not independently set.

## Appendix B — Transfers sub-blob layout (KD-4)

Composed into #30's `SeasonSaveCodec` frame as an opaque, independently version-gated block (mirrors the
`SeasonSaveCodec` / `MatchSaveCodec` posture; every length-prefixed read preceded by an overflow-safe
`Require(offset, need, total)` bound against `total − offset`):

| Field | Type | Notes |
|---|---|---|
| version | u32 | `TRANSFERS_SAVE_FORMAT_VERSION`; **gate first** (F3) |
| clubCount | u32 | `Require`-bounded count |
| per club: ClubId | i32 | |
| per club: CommittedSpendThisWindow | i64 | resets at the season boundary (season-scoped) |
| per club: ActiveWindow.OpenWorldDay / CloseWorldDay | u32 / u32 | season-scoped |
| contractCount | u32 | `Require`-bounded count (managed-club contracts only, §2.2) |
| per contract: PlayerId | i32 | |
| per contract: WagePerPeriod | i64 | `≥ 0` |
| per contract: LengthSeasons | i32 | `> 0` (F7) — the sole contract-end field |
| (trailing-byte guard) | — | `if (o != len) throw` (F3) |

**No `RngCursor`/`actionOrdinal` field** — minimal is draw-free (FR-TX-018); deep draws are keyed
(position-independent), so no cursor is ever serialized. Deep clause/loan/wage-structure + precise-expiry
fields **append** after `LengthSeasons` behind `deepTransfersEnabled` (FR-TX-015), guarded by the same
version bump — the minimal layout above is never reordered.

## Appendix C — Worked valuation example

A 24-year-old outfielder, #27 attribute mean `14`:
- `base = 14 * VALUE_PER_RATING_POINT`.
- `ageMult = AgeCurvePermille[24]` — inside the peak band ⇒ `1000‰` (neutral).
- `value = base * 1000/1000 = base` (integer) — **attributes + age only** at minimal (FR-TX-001).

A manager buy at `fee = value` is **accepted** (`fee ≥ cv`); a buy at `fee = value − 1` is **rejected**. With
`deepTransfersEnabled` on, the deep tier multiplies `needMult` (the valuing club's scarcity) and
`personalityMult`: a low-loyalty seller (`personalityMult > 1000`) raises `cv`, so the same `fee = value` now
**rejects** — the deep tier *scales* the identity, never replaces it (KD-1). All integer; two runs identical.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-23 | — | Initial appendices (constant catalogue, sub-blob layout, worked valuation example). Status IN REVIEW. |
| 0.2 | 2026-07-23 | — | AR-1: dropped `CONTRACT_NO_EXPIRY` + `ExpiryWorldDay` from the layout (single contract-end truth `LengthSeasons`, M3); contract count noted managed-club-only (M2). |
| 0.3 | 2026-07-23 | — | AR-3: Appendix C worked example is attributes+age only at minimal (needMult moved to the deep tier); added `TRANSFERS_NEED_MULT_IDENTITY` constant. AR-4: added `DEFAULT_CONTRACT_SEASONS` / `DEFAULT_WAGE_*` for §3.8 career-start seeding. |
#endregion
