# Squad / Player Data Layer Specification #27 — Section 3: Formulas and Algorithms

**Created:** July 22, 2026
**Last Updated:** July 22, 2026 (v0.1)
**Version:** 0.1
**Status:** APPROVED

---

This layer is already built (`src/player-database/`); this section describes the shipped code in
present tense. Every constant is tagged; the authoritative catalogue is Appendix A.

## 3.1 Canonical `PlayerAttributes` and the `AttrIdx` ordinal map (FR-SQ-001/002/003/006)

`PlayerAttributes` is the single source of truth (KD-1); every per-spec attribute struct is a
projection of it (T2, §7). It holds **31 `int [1,20]` fields** + one special-scale field:

| Group | Fields (AttrIdx ordinal) |
|---|---|
| Physical (6) | Pace 0, Acceleration 1, Agility 2, Balance 3, Strength 4, Stamina 5 |
| Technical (7) | Passing 6, Technique 7, Finishing 8, LongShots 9, Dribbling 10, Crossing 11, Heading 12 |
| Mental (7) | Decisions 13, Vision 14, Composure 15, Anticipation 16, WorkRate 17, Aggression 18, Positioning 19 |
| Goalkeeping (6) | Reflexes 20, Handling 21, Aerial 22, OneVsOne 23, Throwing 24, Kicking 25 |
| Reserved (5) | Tackling 26, Marking 27, Concentration 28, Teamwork 29, FirstTouchAbility 30 |
| Special-scale (1) | `WeakFootRating` `[1,5]` — **not** in the array/clamp helpers |

`AttrIdx` (static class) declares the 31 ordinals once (`AttrIdx.Count = 31 = ATTRIBUTE_COUNT`),
shared by `ToArray`/`FromArray`, `RosterGenerator`, and `SquadFileLoader` — no duplicated 31-way
switch. Ordinals are **append-only**: reordering breaks the generator's fixed RNG draw order and any
serialized attribute array. The full index table is Appendix B. Every `[1,20]` row cites its real
consumer spec; the four still-unconsumed rows (Tackling/Marking/Concentration/Teamwork) are
`RESERVED` per master plan §4.2 (`FirstTouchAbility` is consumed since T1 — three `MatchEngine`
sites, projection KD-P9, so only four remain reserved).

- **`CreateDefault()`** (FR-SQ-005): every `[1,20]` field `= AttributeBaseMean` (10);
  `WeakFootRating = WeakFootBase` (3).
- **`ToArray()` / `FromArray(int[31])`** (FR-SQ-006): copy the 31 fields in `AttrIdx` order.
  `FromArray` throws `ArgumentException` unless `values.Length == AttrIdx.Count` (F1). **Neither
  touches `WeakFootRating`** — the `[1,5]` scale is isolated from the `[1,20]` array so a 5-max value
  can never be array-copied into a 20-max slot, and vice versa (KD-2, FR-SQ-003).

## 3.2 `RosterGenerator.Generate` draw sequence (FR-SQ-012/013/016)

`Squad Generate(DeterministicRngService rng, int streamIndex, int clubId, int count)` — stateless;
the **caller** registers the RNG stream (siteId `"player-database.roster-generation"`,
`SubsystemOrdinals.PlayerDatabase`, `entityId: clubId`), so it is unit-testable without booting a
match (FR-SQ-013). `count` outside `[1, CLUB_SQUAD_SIZE]` throws `ArgumentException`.

Per player, one `Reserve`/draw/`CloseReservation` cycle consumes **exactly** `FIELDS_PER_PLAYER = 36`
draws (F4 — a fixed budget; a future field add fails the locked count assertion rather than silently
desyncing the stream):

```
Reserve(streamIndex, FIELDS_PER_PLAYER = 36)
  draw 0  firstName : DrawBounded(FirstNames.Length = 32)      -> NameCatalogue.FirstNames[i]
  draw 1  lastName  : DrawBounded(LastNames.Length = 32)       -> NameCatalogue.LastNames[i]
  draw 2  age       : AgeMin + DrawBounded(AgeMax-AgeMin+1 = 19)         in [17, 35]
  draw 3  position  : (PlayerPosition) DrawBounded(4)          uniform over the 4 values
  draw 4  weakFoot  : Clamp(WeakFootBase + (DrawBounded(2*WeakFootSpread+1 = 5) - WeakFootSpread),
                            WEAK_FOOT_MIN, WEAK_FOOT_MAX)                 in [1, 5]
  draw 5+i (i in [0,31)) attribute AttrIdx i, in ordinal order:
            jitter = DrawBounded(2*AttributeSpread+1 = 9) - AttributeSpread          in [-4, +4]
            attrs[i] = Clamp(AttributeBaseMean + PositionBias[position][i] + jitter,
                             ATTRIBUTE_MIN, ATTRIBUTE_MAX)
CloseReservation(streamIndex)
PlayerId = clubId * CLUB_SQUAD_SIZE + localIndex
```

`DrawBounded(bound) = (int)(u64Draw % bound)` — a plain modulo map. The bias (< 2⁻⁵⁹ for every
`bound ≤ 32`) is deliberately accepted: generation is not a statistically load-bearing surface, and
rejection sampling would break the fixed 36-draw budget. Position is uniform over the 4 values — a
documented Stage-0 simplification (a realistic few-GK distribution is future work). Same seed ⇒
byte-identical `Squad` (FR-SQ-016).

**Clamp is defensive under the pinned constants.** Attribute pre-clamp values span
`[10 + 0 − 4, 10 + 4 + 4] = [6, 18]` (max bias `+4`, GK fields) — strictly inside `[1,20]`, so the
`[1,20]` clamp never fires today; it guards a future `[GT]` retune. `WeakFootRating`'s own narrower
`WeakFootSpread = 2` makes `WeakFootBase(3) ± 2` span **exactly** `[1,5]` (FR-SQ-015) — the reason it
has its own spread rather than reusing `AttributeSpread = 4`, which would clamp ~6 of every 9 draws
to the boundary.

## 3.3 Position-bias table (FR-SQ-014) and WeakFoot jitter (FR-SQ-015)

`PositionAttributeBias` is a `[4][31]` `[GT]` table (array-valued carve-out, `TacticalInstructionsConstants`
precedent), indexed `[(int)PlayerPosition][AttrIdx.*]`, **nonzero only at each position's signature
attributes**:

| Position (ordinal) | Signature attributes (additive bias) |
|---|---|
| Goalkeeper (0) | Reflexes, Handling, Aerial, OneVsOne, Throwing, Kicking — each **+4** |
| Defender (1) | Tackling, Marking, Strength — each **+3** |
| Midfielder (2) | Passing, Vision, Stamina — each **+3** |
| Forward (3) | Finishing, Pace, Dribbling — each **+3** |

Every other cell is 0 (no bias). Direct constant-value assertions in the test suite catch a
"phantom" all-zero table that would silently make generation position-blind (design AR-2).

`WeakFootRating` draws through its own `WeakFootSpread = 2` (§3.2, draw 4) — never `AttributeSpread`.
Base `3 ± 2` spans `[1,5]` with no clamping (FR-SQ-015).

## 3.4 `SquadFileLoader` grammar (FR-SQ-018/019)

`Squad Parse(string text, int clubId)` — the Stage-0 human-authoring text import (KD-8). NOT a
determinism-pinned wire format: only the resulting `PlayerRecord` values matter, never the grammar
(FR-SQ-019). Line-oriented, case-insensitive `key = value` under `[player N]` section headers
(N = 0-based roster index); `#` starts a comment; blank lines ignored; `InvariantCulture` numeric
parsing.

Keys: `firstName`/`lastName` (string), `age` (int `[AgeMin, AgeMax]`), `position` (`PlayerPosition`
member name), `weakFoot` (int `[1,5]`), and one key per `AttrIdx` field name (int `[1,20]`, e.g.
`pace = 14`).

- **Omitted key ⇒ mid-range identity** (KD-8): the `CreateDefault` / `PlayerRecord.CreateDefault`
  field value. An omitted/empty file ⇒ an all-identity squad of `CLUB_SQUAD_SIZE` players; squad
  length = highest authored index + 1, and an **index gap** parses as a full-identity player (the
  per-section analogue of the omitted-key rule).
- **`PlayerId` is club-scoped** even for identity-filled players: `clubId * CLUB_SQUAD_SIZE +
  localIndex` (KD-3), matching `RosterGenerator` — not the raw local index.
- **Fail-loud (`FormatException`, F5):** unknown section/key, duplicate section, duplicate key,
  malformed header, a key before any section, unparsable value, out-of-range int (incl. `age`), and
  a highest index ≥ `CLUB_SQUAD_SIZE`. Every consumed key is removed from the section map; any
  leftover key is unknown and throws.

## 3.5 Worked example — one generated `PlayerRecord`

`Generate(rng, s, clubId = 7, count = 3)`, player `localIndex = 2` ⇒ `PlayerId = 7×25 + 2 = 177`.
Given a reservation whose 36 draws produce the bounded values below:

| Draw | Field | Bounded value | Result |
|---|---|---|---|
| 0 | firstName | `% 32 = 4` | `FirstNames[4]` = **"William"** |
| 1 | lastName | `% 32 = 13` | `LastNames[13]` = **"Wilson"** |
| 2 | age | `% 19 = 4` | `17 + 4` = **21** |
| 3 | position | `% 4 = 3` | **Forward** ⇒ bias +3 at Pace/Finishing/Dribbling |
| 4 | weakFoot | `% 5 = 4` | jitter `4−2 = +2`; `Clamp(3+2,1,5)` = **5** (== `WEAK_FOOT_MAX`, at the boundary) |
| 5 (Pace, i=0) | attr | `% 9 = 8` | jitter `+4`; `Clamp(10 + 3 + 4, 1, 20)` = `Clamp(17)` = **17** |
| 6 (Acceleration, i=1) | attr | `% 9 = 4` | jitter `0`; `Clamp(10 + 0 + 0)` = **10** |
| 13 (Finishing, i=8) | attr | `% 9 = 7` | jitter `+3`; `Clamp(10 + 3 + 3)` = **16** |
| 15 (Dribbling, i=10) | attr | `% 9 = 6` | jitter `+2`; `Clamp(10 + 3 + 2)` = **15** |
| … | 27 remaining attrs | — | `10 + 0 + jitter`, all in `[6, 18]` |

Weak foot lands **at** the `[1,5]` upper bound (5) — the clamp is an identity here because
`WeakFootSpread` was chosen to span the scale exactly. The signature attributes reach 15–17 (bias
`+3` + jitter); the `[1,20]` clamp is a no-op at every draw (§3.2). `PlayerId` uses the club-scoped
formula, distinct from any match `teamId`. The full numeric expansion is Appendix D.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-22 | — | Initial algorithms: canonical record + AttrIdx map, CreateDefault/ToArray/FromArray, RosterGenerator 36-draw sequence, position-bias + WeakFoot jitter, SquadFileLoader grammar, worked example. Documents the shipped `src/player-database/` code (values verified against source). |
#endregion
