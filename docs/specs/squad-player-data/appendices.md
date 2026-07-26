# Squad / Player Data Layer Specification #27 — Appendices

**Created:** July 22, 2026
**Last Updated:** July 22, 2026 (v0.1)
**Version:** 0.1
**Status:** APPROVED

---

## Appendix A — Constant catalogue (authoritative)

Verified against `src/player-database/PlayerDatabaseConstants.cs` and (for `[CROSS]`)
`src/deterministic-sim/`. `[FIXED]`/`[DERIVED]` are `const` (ALL_CAPS); `[GT]` are `static readonly`
(PascalCase per Spec #20 §3.2.3 — the identifiers below are the real code names).

| Constant | Tag | Value | Meaning |
|---|---|---|---|
| `ATTRIBUTE_MIN` | `[FIXED]` | 1 | Lower bound of any `[1,20]` attribute. |
| `ATTRIBUTE_MAX` | `[FIXED]` | 20 | Upper bound of any `[1,20]` attribute. |
| `WEAK_FOOT_MIN` | `[FIXED]` | 1 | Lower bound of `WeakFootRating` (`[1,5]`). |
| `WEAK_FOOT_MAX` | `[FIXED]` | 5 | Upper bound of `WeakFootRating`. |
| `CLUB_SQUAD_SIZE` | `[FIXED]` | 25 | Max players per club roster (master plan §4.2). Distinct from match-scoped `MatchEngineConstants.SQUAD_SIZE = 22` (KD-3). |
| `ATTRIBUTE_COUNT` | `[DERIVED]` | 31 | `int[1,20]` field count = `AttrIdx` member count. |
| `POSITION_COUNT` | `[DERIVED]` | 4 | `PlayerPosition` member count; also the row count of the position-bias table. Declared once in the catalogue because two assemblies consume it (`RosterGenerator`'s draw bound and the league bootstrap's squad template) — ERR-027-002. |
| `IDENTITY_DRAWS_PER_PLAYER` | `[DERIVED]` | 5 | Identity draws: first name, last name, age, position, weak foot. |
| `FIELDS_PER_PLAYER` | `[DERIVED]` | 36 | `IDENTITY_DRAWS_PER_PLAYER + ATTRIBUTE_COUNT` (5 + 31). RNG draws reserved per player. |
| `AttributeBaseMean` | `[GT]` | 10 | Mean each attribute generates around, before bias + jitter. |
| `AttributeSpread` | `[GT]` | 4 | Symmetric jitter half-width around `(BaseMean + bias)`. |
| `AgeMin` | `[GT]` | 17 | Minimum generated age (years). |
| `AgeMax` | `[GT]` | 35 | Maximum generated age (years). |
| `WeakFootBase` | `[GT]` | 3 | Mean `WeakFootRating` generates around. |
| `WeakFootSpread` | `[GT]` | 2 | Its own narrower jitter half-width: `3 ± 2` spans `[1,5]` exactly, no clamping (FR-SQ-015). |
| `PositionAttributeBias` | `[GT]` | `[4][31]` table | Additive per-position signature bias (Appendix C). |
| `DOMAIN_TAG_PLAYER_DATABASE` | `[CROSS]` | `0x1F` | RNG domain tag. Authoritative source: `DeterministicSimConstants` (#16 §3.4). |
| `SubsystemOrdinals.PlayerDatabase` | `[CROSS]` | 81 | Off-pitch (80–99) subsystem ordinal. Authoritative source: `SubsystemOrdinals` (#16 §3.4). |

## Appendix B — `AttrIdx` ordinal table

Verified against `src/player-database/AttrIdx.cs`. Append-only; `AttrIdx.Count = 31`.

| Idx | Field | Idx | Field | Idx | Field |
|---|---|---|---|---|---|
| 0 | Pace | 11 | Crossing | 22 | Aerial |
| 1 | Acceleration | 12 | Heading | 23 | OneVsOne |
| 2 | Agility | 13 | Decisions | 24 | Throwing |
| 3 | Balance | 14 | Vision | 25 | Kicking |
| 4 | Strength | 15 | Composure | 26 | Tackling |
| 5 | Stamina | 16 | Anticipation | 27 | Marking |
| 6 | Passing | 17 | WorkRate | 28 | Concentration |
| 7 | Technique | 18 | Aggression | 29 | Teamwork |
| 8 | Finishing | 19 | Positioning | 30 | FirstTouchAbility |
| 9 | LongShots | 20 | Reflexes | | |
| 10 | Dribbling | 21 | Handling | | |

`WeakFootRating` is **not** in this map (separate `[1,5]` scale, KD-2).

## Appendix C — Position-bias table (`PositionAttributeBias`)

Verified against `BuildPositionAttributeBias()`. Every unlisted cell is 0.

| Position (ordinal) | Attribute (AttrIdx) | Bias |
|---|---|---|
| Goalkeeper (0) | Reflexes 20, Handling 21, Aerial 22, OneVsOne 23, Throwing 24, Kicking 25 | +4 each |
| Defender (1) | Tackling 26, Marking 27, Strength 4 | +3 each |
| Midfielder (2) | Passing 6, Vision 14, Stamina 5 | +3 each |
| Forward (3) | Finishing 8, Pace 0, Dribbling 10 | +3 each |

## Appendix D — Worked generation example (numeric expansion of §3.5)

`Generate(rng, s, clubId = 7, count = 3)`, `localIndex = 2` ⇒
`PlayerId = 7 × CLUB_SQUAD_SIZE(25) + 2 = 177`. Reservation of `FIELDS_PER_PLAYER = 36` draws;
bounded values as tabled in §3.5.

```
draw 0  firstName % 32 = 4   -> FirstNames[4]  = "William"
draw 1  lastName  % 32 = 13  -> LastNames[13]  = "Wilson"
draw 2  age       % 19 = 4   -> AgeMin(17) + 4                          = 21
draw 3  position  % 4  = 3   -> (PlayerPosition)3                       = Forward
draw 4  weakFoot  % 5  = 4   -> Clamp(WeakFootBase(3) + (4 - WeakFootSpread(2)), 1, 5)
                             -> Clamp(3 + 2, 1, 5)                       = 5   (== WEAK_FOOT_MAX)
Forward bias: Pace(0)=+3, Finishing(8)=+3, Dribbling(10)=+3; all others 0.
draw 5  Pace      % 9  = 8   -> Clamp(10 + 3 + (8 - AttributeSpread(4)), 1, 20)
                             -> Clamp(10 + 3 + 4, 1, 20)                 = 17
draw 6  Acceler.  % 9  = 4   -> Clamp(10 + 0 + (4 - 4), 1, 20)          = 10
draw 13 Finishing % 9  = 7   -> Clamp(10 + 3 + (7 - 4), 1, 20)          = 16
draw 15 Dribbling % 9  = 6   -> Clamp(10 + 3 + (6 - 4), 1, 20)          = 15
draws 7..12, 14, 16..35  -> Clamp(10 + 0 + jitter, 1, 20), jitter in [-4,+4], each in [6,18]
CloseReservation
```

Resulting `PlayerRecord`: `{ PlayerId 177, "William Wilson", Age 21, Position Forward,
Attributes{ Pace 17, Finishing 16, Dribbling 15, others 6..18, WeakFootRating 5 } }`. The `[1,20]`
clamp is a no-op at every draw (pre-clamp range `[6,18]`); weak foot lands **at** its `[1,5]` upper
bound (clamp identity by `WeakFootSpread` design).

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-22 | — | Initial appendices: constant catalogue, AttrIdx ordinal table, position-bias table, worked generation numeric expansion. Values verified against `src/player-database/`. |
#endregion
