# Transfers, Contracts & Negotiation #31 — Appendices

**Created:** July 23, 2026
**Last Updated:** October 20, 2026 (v0.12 — AR round 4, ERR-031-014: Appendix A regains the TRANSFERS_STAFF_MULT_IDENTITY row #34 cites)
**Last Updated (prior):** October 20, 2026 (v0.11 — AR round 3, ERR-031-013: Appendix A matches TransfersConstants.cs exactly)
**Last Updated (prior):** October 20, 2026 (v0.10 — ERR-031-011: `SUMMER_WINDOW_IN_SEASON_DAYS` replaces `SUMMER_WINDOW_LENGTH_DAYS`)
**Last Updated (prior):** October 20, 2026 (v0.9 — T2: ERR-031-008 window constants)
**Last Updated (prior):** October 19, 2026 (v0.8 — ERR-031-005: contract membership is a roster check)
**Last Updated (prior):** October 18, 2026 (v0.7 — ERR-031-003: Appendix B gains the leading `XFER` magic and pins `clubCount ∈ {0,1}` as a wiring fact; Appendix A gains `TRANSFERS_SAVE_MAGIC`; T1 live)
**Last Updated (prior):** September 14, 2026 (v0.6 — require a non-empty young-age band)
**Last Updated (prior):** September 14, 2026 (v0.5 — PR #407 review correction; prior v0.4 T0 football-judgment constants, v0.3 AR-3, v0.2 AR-1, v0.1 initial)
**Version:** 0.12
**Status:** APPROVED

---

## Appendix A — Constant catalogue

| Constant | Tag | Value | Notes |
|---|---|---|---|
| `TRANSFERS_SAVE_MAGIC` | `[FIXED]` | `"XFER"` (`0x58464552`) | transfers sub-blob self-identifying tag, written BEFORE the version (ERR-031-003). Live at T1. |
| `TRANSFERS_SAVE_FORMAT_VERSION` | `[FIXED]` | 1 | transfers sub-blob version gate; live at T1. |
| `TRANSFERS_SAVE_HEADER_BYTES` | `[DERIVED]` | 12 | fixed header: magic + version + clubCount (three u32). |
| `TRANSFERS_SAVE_CLUB_RECORD_BYTES` | `[DERIVED]` | 20 | one club record: ClubId i32 + CommittedSpendThisWindow i64 + Open u32 + Close u32. |
| `TRANSFERS_SAVE_CONTRACT_RECORD_BYTES` | `[DERIVED]` | 16 | one contract record: PlayerId i32 + WagePerPeriod i64 + LengthSeasons i32. |
| `TRANSFERS_SAVE_MAX_CLUBS` | `[FIXED]` | 1 | most club records the block may carry: `TransfersState` is scoped to the ONE managed club (§2.2), so a block carries 0 (not wired) or 1. |
| `PERMILLE_DENOM` | `[FIXED]` | 1000 | shared integer per-mille denominator. |
| `IDENTITY_MULTIPLIER_PERMILLE` | `[FIXED]` | 1000 | the identity multiplier for the deferred #34 staff and #33 personality modulation (one constant: the two deep-tier multipliers share the same identity). #34 cites it by the name in the `TRANSFERS_STAFF_MULT_IDENTITY` row below; a separate `TRANSFERS_PERSONALITY_MULT_IDENTITY` was never declared in code and nothing cites it (AR round 3, ERR-031-013). |
| `TRANSFERS_STAFF_MULT_IDENTITY` | `[DERIVED]` | `IDENTITY_MULTIPLIER_PERMILLE` (1000) | the `staffMult` identity #34 produces by default and cites as `[CROSS]` from this appendix (#34 `appendices.md`, FR-TX-011). A spec-level name for the one shared constant above: the code declares only `IDENTITY_MULTIPLIER_PERMILLE`, so there is no second symbol to drift (AR round 4, ERR-031-014 — AR round 3 had dropped this row, leaving #34's citation dangling). |
| `VALUE_PER_RATING_POINT` | `[GT]` | illustrative | integer currency per one full canonical mean-attribute point; base computation keeps `sum/ATTRIBUTE_COUNT` exact until final division. |
| `PEAK_AGE_MIN` / `PEAK_AGE_MAX` | `[GT]` | illustrative | inclusive neutral-age band; `PEAK_AGE_MIN > 0` and `PEAK_AGE_MAX >= PEAK_AGE_MIN`, preserving a non-empty young-discount region. |
| `YOUNG_DISCOUNT_PERMILLE` | `[GT]` | illustrative | `[0,1000)`; must be a real discount below the peak. |
| `DECLINE_PER_YEAR_PERMILLE` | `[GT]` | illustrative | positive annual decline after `PEAK_AGE_MAX`. |
| `MINIMUM_AGE_MULTIPLIER_PERMILLE` | `[GT]` | illustrative | `[0,1000)` floor for old-age decline. |
| `NEGOTIATION_COUNTER_BAND_PERMILLE` | `[GT]` | illustrative | strict `(0,1000)` synchronous counter-offer half-band; positive valuation retains both counter and reject regions. |
| `CLUB_NEED_PER_PLAYER_PERMILLE` | `[GT]` | illustrative | upper bound is **derived** as `floor((PERMILLE_DENOM-1)/(CLUB_SQUAD_SIZE-neutralCount))`, keeping the multiplier positive for every legal stock count even if #27 cardinalities change. |
| `SUMMER_WINDOW_IN_SEASON_DAYS` | `[GT]` | illustrative (fallback 38) | world days, `≥ 1`: how long the single minimal window stays open from the season's opening fixture day on, counting that day — `Close = openingFixtureDay + IN_SEASON_DAYS − 1` (§3.5, ERR-031-011). Config key `[transfers] SummerWindowInSeasonDays`. With `FirstRoundDay` 7 the genesis window is `[0, 44]`. |
| ~~`SUMMER_WINDOW_LENGTH_DAYS`~~ | ~~`[GT]`~~ | ~~45~~ | **Withdrawn at AR round 2 (ERR-031-011):** a length counted from the roll day made the close depend on when the client rolled (with prompt rolls it shut 11 days before the first match). Replaced by `SUMMER_WINDOW_IN_SEASON_DAYS`. |
| `DEFAULT_CONTRACT_SEASONS` | `[GT]` | illustrative (fallback 3) | genesis contract length, also the terms of a regen joining the managed club at the boundary (§3.7); `> 0`. Config key `[transfers] DefaultContractSeasons`. |
| `DEFAULT_WAGE_PER_PERIOD` | `[GT]` | illustrative (fallback 1000) | flat genesis wage — every seeded contract, genesis and boundary regen alike; there is no per-player wage model and no `DefaultWageFor` (§3.8); `≥ 0`. Config key `[transfers] DefaultWagePerPeriod`. |
| ~~`SEASON_START_WORLD_DAY`~~ | ~~`[CROSS]`~~ | ~~#30~~ | **Withdrawn at T2 (ERR-031-008):** #30 never defined it, and one constant cannot place season N > 1. The window starts on the world day #30 begins the season (genesis day / roll day), passed to `TransferWindow.SummerWindowFor` (as its open day) and persisted as `ActiveWindow`. |

**Tag note:** `[GT]` values are loaded through Code Standards #20 `GameplayConfig.Get*` from `[transfers]`; in
`TransfersConstants.cs` they are PascalCase (`ValuePerRatingPoint`, `PeakAgeMin`, `PeakAgeMax`,
`YoungDiscountPermille`, `DeclinePerYearPermille`, `MinimumAgeMultiplierPermille`, `NegotiationCounterBandPermille`,
`ClubNeedPerPlayerPermille`, `SummerWindowInSeasonDays`, `DefaultContractSeasons`, `DefaultWagePerPeriod`), the
config key being the code name. Fallback magnitudes are
not architectural constants. The normative safety/shape bounds above are architectural.

## Appendix B — Transfers sub-blob layout (KD-4)

Composed into #30's `SeasonSaveCodec` frame as an opaque, independently version-gated block (T1):

| Field | Type | Notes |
|---|---|---|
| magic | u32 | `TRANSFERS_SAVE_MAGIC` (`"XFER"`); gate FIRST (F3, ERR-031-003) |
| version | u32 | `TRANSFERS_SAVE_FORMAT_VERSION`; gate second (F3) |
| clubCount | u32 | `0` (no #31 subsystem wired) or `1` (the managed club); `> 1` fails loud — `TransfersState` is managed-club scoped (§2.2) |
| per club: ClubId | i32 | |
| per club: CommittedSpendThisWindow | i64 | season-scoped |
| per club: ActiveWindow.OpenWorldDay / CloseWorldDay | u32 / u32 | season-scoped |
| contractCount | u32 | managed-club contracts only; MUST be `0` when `clubCount = 0`; `≤ CLUB_SQUAD_SIZE` |
| per contract: PlayerId | i32 | strictly ascending; membership in the managed club is a ROSTER fact the season root checks (ERR-031-005), never derived from the id |
| per contract: WagePerPeriod | i64 | `≥ 0` |
| per contract: LengthSeasons | i32 | `> 0` (F7) |
| trailing-byte guard | — | `if (o != len) throw` |

No RNG cursor/action ordinal is serialized. Deep clause/loan/wage-structure fields append after the minimal
contract fields behind the appropriate version bump.

**Corrected at T1 (ERR-031-003).** This table originally began with `version` and carried no magic — the
exact defect ERR-029-005 / ERR-041-009 / ERR-028-004 / ERR-044-001 filed against every sibling block. Every
sub-blob in the season frame sits at version 1, so a version gate separates generations of ONE format and
cannot tell this block from another: an empty `FNCE` finance block (magic, version 1, count 0) is
byte-shape-compatible with a transfers header, and a transposed frame slot would have decoded cleanly. The
shipped block leads with the magic, checked before the version.

**`clubCount` is a wiring fact.** A wired career always writes its one club record, including after every
contract has expired or been sold; `0` therefore means only "no #31 subsystem behind this save" and decodes
to an absent state, never to a request to seed (FR-TX-028: a load never seeds). This is what lets #30's
destination guard key on the count without repeating ERR-030-038 (#30 Appendix B.1). Every decoded value
passes the same validation a live state applies (window `Close ≥ Open`, spend `≥ 0`, F6/F7 contracts);
any refusal is a corrupt-file `InvalidOperationException`.

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
| 0.6 | 2026-09-14 | — | Require `PEAK_AGE_MIN > 0` so the young-discount region and strict boundary regression are well-defined. |
| 0.7 | 2026-10-18 | — | **ERR-031-003** (#31 T1): Appendix B gains the leading `TRANSFERS_SAVE_MAGIC` (`"XFER"`) before the version — the table had specified the ERR-029-005-class version-first, magic-less layout — and pins `clubCount ∈ {0,1}` as a wiring fact, `contractCount = 0` without a club, ascending contract ids and `≤ CLUB_SQUAD_SIZE`; Appendix A gains the magic row and marks both rows live. |
| 0.8 | 2026-10-19 | — | ERR-031-005: Appendix B — contract membership is checked by #30 against the roster, not derived from the id. |
| 0.9 | 2026-10-20 | — | #31 T2 (ERR-031-008): `SUMMER_WINDOW_LENGTH_DAYS` is an inclusive length; `SEASON_START_WORLD_DAY` withdrawn (never defined by #30; the start is the season-begin day). |
| 0.10 | 2026-10-20 | — | ERR-031-011 (AR round 2): `SUMMER_WINDOW_IN_SEASON_DAYS` (`[GT]`, fallback 38) replaces the roll-day-anchored `SUMMER_WINDOW_LENGTH_DAYS` (withdrawn row kept); the `SEASON_START_WORLD_DAY` withdrawal row names `SummerWindowFor`. |
| 0.11 | 2026-10-20 | — | ERR-031-013 (AR round 3): Appendix A brought to `TransfersConstants.cs`: gains `TRANSFERS_SAVE_HEADER_BYTES` (12), `_CLUB_RECORD_BYTES` (20), `_CONTRACT_RECORD_BYTES` (16) and `_MAX_CLUBS` (1), which the T1 codec declares; `DEFAULT_WAGE_*` (`DefaultWageFor`) becomes the flat `DEFAULT_WAGE_PER_PERIOD`; the never-declared `TRANSFERS_STAFF_MULT_IDENTITY` / `TRANSFERS_PERSONALITY_MULT_IDENTITY` rows are replaced by the single shipped `IDENTITY_MULTIPLIER_PERMILLE` (#34's cited name recorded as an alias); the tag note maps the PascalCase code names and config keys. |
| 0.12 | 2026-10-20 | — | ERR-031-014 (AR round 4, R4-L6): AR round 3 removed `TRANSFERS_STAFF_MULT_IDENTITY` from Appendix A while #34 `appendices.md` (and its §3/§7/§8) still cite it as a `[CROSS]` constant sourced here. Re-added as a `[DERIVED]` alias of `IDENTITY_MULTIPLIER_PERMILLE` (one source tag; no second code symbol), so #34 stays correct untouched. |
#endregion
