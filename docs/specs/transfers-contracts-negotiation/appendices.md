# Transfers, Contracts & Negotiation #31 — Appendices

**Created:** July 23, 2026
**Last Updated:** September 14, 2026 (v0.4 — T0 football-judgment constants + worked example)
**Last Updated (prior):** July 23, 2026 (v0.3 — AR-3 fix pass; prior v0.2 AR-1, v0.1 initial)
**Version:** 0.4
**Status:** APPROVED

---

## Appendix A — Constant catalogue

| Constant | Tag | Value | Notes |
|---|---|---|---|
| `TRANSFERS_SAVE_FORMAT_VERSION` | `[FIXED]` | 1 | the transfers sub-blob's own version gate (KD-4; lands at T1). |
| `PERMILLE_DENOM` | `[FIXED]` | 1000 | integer per-mille denominator. |
| `VALUE_PER_RATING_POINT` | `[GT]` | illustrative | integer currency per mean-attribute point (§3.1); loaded through `GameplayConfig`. |
| `AgeCurvePermille[age]` | `[GT]` | illustrative | age→value shape: neutral peak band, older decline, young-unproven discount. |
| `NEGOTIATION_COUNTER_BAND_PERMILLE` | `[GT]` | illustrative | symmetric synchronous T0 counter-offer half-band around counterparty value (§3.2); `> 0`, draw-free. |
| `CLUB_NEED_PER_PLAYER_PERMILLE` | `[GT]` | illustrative | always-on Stage-2 positional-stock step around the #27-derived neutral count (§3.1); bounded so the multiplier stays positive for every legal squad count. |
| `SUMMER_WINDOW_LENGTH_DAYS` | `[GT]` | illustrative | length of the single minimal transfer window (KD-6). |
| `DEFAULT_CONTRACT_SEASONS` | `[GT]` | illustrative | career-start seeded contract length (§3.8); `> 0` (F7-valid). |
| `DEFAULT_WAGE_*` (`DefaultWageFor`) | `[GT]` | illustrative | career-start seeded wage (§3.8); `≥ 0` (F6-valid). |
| `SEASON_START_WORLD_DAY` | `[CROSS]` | #30 | season-start world day; read-only from #30 calendar at T2. |
| `TRANSFERS_STAFF_MULT_IDENTITY` | `[FIXED]` | 1000 | deferred #34 staff multiplier identity. |
| `TRANSFERS_PERSONALITY_MULT_IDENTITY` | `[FIXED]` | 1000 | deferred #33 personality multiplier identity. |

**Tag note:** all `[GT]` values are loaded through the Code Standards #20 `GameplayConfig.Get*` mechanism at
T0; their fallback magnitudes are not architectural constants. The normative contract is direction/shape:
integer monotonic valuation identity, positive positional-need multiplier, a non-zero near-value counter band,
one minimal window, and F6/F7-valid genesis terms. `SEASON_START_WORLD_DAY` remains `[CROSS]` from #30.

## Appendix B — Transfers sub-blob layout (KD-4)

Composed into #30's `SeasonSaveCodec` frame as an opaque, independently version-gated block (T1):

| Field | Type | Notes |
|---|---|---|
| version | u32 | `TRANSFERS_SAVE_FORMAT_VERSION`; gate first (F3) |
| clubCount | u32 | `Require`-bounded count |
| per club: ClubId | i32 | |
| per club: CommittedSpendThisWindow | i64 | season-scoped |
| per club: ActiveWindow.OpenWorldDay / CloseWorldDay | u32 / u32 | season-scoped |
| contractCount | u32 | managed-club contracts only |
| per contract: PlayerId | i32 | |
| per contract: WagePerPeriod | i64 | `≥ 0` |
| per contract: LengthSeasons | i32 | `> 0` (F7) |
| trailing-byte guard | — | `if (o != len) throw` |

No RNG cursor/action ordinal is serialized. Deep clause/loan/wage-structure fields append after the minimal
contract fields behind the appropriate version bump.

## Appendix C — Worked valuation / negotiation example

A 24-year-old outfielder with #27 attribute mean `14` first resolves the pure identity:
- `base = 14 * VALUE_PER_RATING_POINT`;
- `ageMult = AgeCurvePermille[24] = 1000‰` in the peak band;
- `identity = base`.

The **counterparty** then applies its positional stock. At the #27-derived neutral count the need multiplier is
`1000‰`, so `cv == identity`; below-neutral stock raises `cv`, while overstock lowers it. Deep #33 personality
and #34 staff refinements may later multiply that Stage-2 value but do not remove the positional term.

For a neutral-stock `cv = 100,000` and a `5%` illustrative counter band: a manager buy at `100,000` is
`Accepted`; `99,999` through `95,000` is `CounterOffered`; `94,999` is `Rejected`. The mirror applies to a sell
above the buyer's valuation. No draw occurs; equal inputs produce equal outcomes.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-23 | — | Initial appendices (constant catalogue, sub-blob layout, worked valuation example). Status IN REVIEW. |
| 0.2 | 2026-07-23 | — | AR-1: dropped `CONTRACT_NO_EXPIRY` + `ExpiryWorldDay` from the layout (single contract-end truth `LengthSeasons`, M3); contract count noted managed-club-only (M2). |
| 0.3 | 2026-07-23 | — | AR-3: Appendix C worked example is attributes+age only at minimal (needMult moved to the deep tier); added `TRANSFERS_NEED_MULT_IDENTITY` constant. AR-4: added `DEFAULT_CONTRACT_SEASONS` / `DEFAULT_WAGE_*` for §3.8 career-start seeding. |
| 0.4 | 2026-09-14 | — | T0 football-judgment close-out supersedes the AR-3 deep-only need decision: positional need is now always-on Stage 2; adds the deterministic counter-offer-band `[GT]`; removes the obsolete minimal need-identity constant; Appendix C demonstrates the new three-way outcome. |
#endregion
