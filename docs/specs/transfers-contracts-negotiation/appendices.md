# Transfers, Contracts & Negotiation #31 — Appendices

**Created:** July 23, 2026
**Last Updated:** September 14, 2026 (v0.5 — PR #407 review correction: tuning bounds + precise valuation example)
**Last Updated (prior):** September 14, 2026 (v0.4 — T0 football-judgment constants + worked example; prior v0.3 AR-3, v0.2 AR-1, v0.1 initial)
**Version:** 0.5
**Status:** APPROVED

---

## Appendix A — Constant catalogue

| Constant | Tag | Value | Notes |
|---|---|---|---|
| `TRANSFERS_SAVE_FORMAT_VERSION` | `[FIXED]` | 1 | transfers sub-blob version gate; lands at T1. |
| `PERMILLE_DENOM` | `[FIXED]` | 1000 | integer per-mille denominator. |
| `VALUE_PER_RATING_POINT` | `[GT]` | illustrative | integer currency per one full canonical mean-attribute point; base computation keeps `sum/ATTRIBUTE_COUNT` exact until final division. |
| `PEAK_AGE_MIN` / `PEAK_AGE_MAX` | `[GT]` | illustrative | inclusive neutral-age band. |
| `YOUNG_DISCOUNT_PERMILLE` | `[GT]` | illustrative | `[0,1000)`; must be a real discount below the peak. |
| `DECLINE_PER_YEAR_PERMILLE` | `[GT]` | illustrative | positive annual decline after `PEAK_AGE_MAX`. |
| `MINIMUM_AGE_MULTIPLIER_PERMILLE` | `[GT]` | illustrative | `[0,1000)` floor for old-age decline. |
| `NEGOTIATION_COUNTER_BAND_PERMILLE` | `[GT]` | illustrative | strict `(0,1000)` synchronous counter-offer half-band; positive valuation retains both counter and reject regions. |
| `CLUB_NEED_PER_PLAYER_PERMILLE` | `[GT]` | illustrative | upper bound is **derived** as `floor((PERMILLE_DENOM-1)/(CLUB_SQUAD_SIZE-neutralCount))`, keeping the multiplier positive for every legal stock count even if #27 cardinalities change. |
| `SUMMER_WINDOW_LENGTH_DAYS` | `[GT]` | illustrative | length of the single minimal transfer window. |
| `DEFAULT_CONTRACT_SEASONS` | `[GT]` | illustrative | genesis contract length; `> 0`. |
| `DEFAULT_WAGE_*` (`DefaultWageFor`) | `[GT]` | illustrative | genesis wage; `≥ 0`. |
| `SEASON_START_WORLD_DAY` | `[CROSS]` | #30 | season-start world day; read-only at T2. |
| `TRANSFERS_STAFF_MULT_IDENTITY` | `[FIXED]` | 1000 | deferred #34 staff multiplier identity. |
| `TRANSFERS_PERSONALITY_MULT_IDENTITY` | `[FIXED]` | 1000 | deferred #33 personality multiplier identity. |

**Tag note:** `[GT]` values are loaded through Code Standards #20 `GameplayConfig.Get*`; fallback magnitudes are
not architectural constants. The normative safety/shape bounds above are architectural.

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

With the default T0 fallback tuning, a 25-year-old player whose 31 canonical attributes are all `10` has:

- attribute sum `310`;
- `ageMult = 1000‰` in the peak band;
- `ValuePlayer = 310 × 10000 × 1000 / (31 × 1000) = 100000` currency units.

Raising exactly one canonical attribute from `10` to `11` changes the sum to `311` and the value to
`floor(311 × 10000 / 31) = 100322`; the minimal model therefore does **not** collapse players to 20 integer-
mean price buckets.

The counterparty then applies positional stock **excluding the negotiated player**. At the #27-derived neutral
count the multiplier is `1000‰`; below-neutral stock raises value, overstock lowers it. A selling club subtracts
the negotiated player from its raw current count; a buying club does not, because he is not yet on that roster.

For `cv = 100000` and an illustrative `5%` counter band: a buy at `100000` is `Accepted`; `99999` through
`95000` is `CounterOffered`; `94999` is `Rejected`. If an accepted bid exceeds remaining manager budget,
`SubmitBid` returns `InsufficientBudget`; if the destination has no slot it returns `SquadFull`. Neither is a
`NegotiationOutcome`, and neither mutates local finance/transfer state.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-23 | — | Initial appendices; status IN REVIEW. |
| 0.2 | 2026-07-23 | — | AR-1 contract-layout corrections. |
| 0.3 | 2026-07-23 | — | AR-3/AR-4 attributes+age minimal baseline and genesis constants. |
| 0.4 | 2026-09-14 | — | T0 football-judgment close-out: always-on positional need + deterministic counter band. |
| 0.5 | 2026-09-14 | — | PR #407 review correction: precise rational-mean valuation example, prospective exclude-player stock, derived club-need safety bound, sub-1000 age/counter bounds, and typed submission outcomes. |
#endregion
