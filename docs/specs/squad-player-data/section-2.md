# Squad / Player Data Layer Specification #27 — Section 2: Functional Requirements, Data Structures, Failure Modes

**Created:** July 22, 2026
**Last Updated:** July 22, 2026 (v0.1)
**Version:** 0.1
**Status:** IN REVIEW

---

## 2.1 Functional Requirements

Conformance per RFC 2119. Citations resolve to a KD in §1.4, a §3 algorithm, or a failure mode.
All requirements describe the **landed** implementation (present tense).

### Canonical record (FR-SQ-001..006)

| FR | Subject | Conformance | Source |
|---|---|---|---|
| FR-SQ-001 | The canonical `PlayerAttributes` struct is the single source of truth; every per-spec attribute struct is a projection of it, never an independent proxy. | MUST | KD-1 |
| FR-SQ-002 | `PlayerAttributes` carries 31 `int [1,20]` fields grouped Physical(6)/Technical(7)/Mental(7)/Goalkeeping(6)/Reserved(5); each field's doc cites its real consumer spec or is marked `RESERVED`. | MUST | §2.2.1 |
| FR-SQ-003 | `WeakFootRating` is `[1,5]`, a separate field on a distinct scale, excluded from the `[1,20]` array (`ToArray`/`FromArray`) and the `[1,20]` clamp helpers. | MUST | KD-2 |
| FR-SQ-004 | All 31 `[1,20]` attributes are `int`; a clamp helper enforces the `[ATTRIBUTE_MIN, ATTRIBUTE_MAX]` range at generation. | MUST | KD-2 / §3 |
| FR-SQ-005 | `CreateDefault()` sets every `[1,20]` field to `10` (`ATTRIBUTE_BASE_MEAN`) and `WeakFootRating` to `3` (`WEAK_FOOT_BASE`). | MUST | §2.2.1 |
| FR-SQ-006 | `ToArray()`/`FromArray(int[31])` round-trip the 31 `[1,20]` fields through a single named `AttrIdx` ordinal map shared by the generator and the loader (no duplicated 31-way switch); `FromArray` fails loud on a non-31 length. | MUST | §3 / F1 |

### Identity and containers (FR-SQ-007..011)

| FR | Subject | Conformance | Source |
|---|---|---|---|
| FR-SQ-007 | `PlayerPosition` is `{Goalkeeper=0, Defender, Midfielder, Forward}`, byte-stable/APPEND-only; it is NOT positioning-ai's `RoleId`. | MUST | KD-4 |
| FR-SQ-008 | `PlayerRecord` carries `{PlayerId, FirstName, LastName, Age, Position, Attributes}`. | MUST | §2.2.3 |
| FR-SQ-009 | `Squad` carries `{ClubId, PlayerRecord[] (1..CLUB_SQUAD_SIZE), Count, GetPlayer(i)}`; the ctor snapshot-copies the caller's array and `GetPlayer` bounds-checks the index. | MUST | §2.2.4 / F3 |
| FR-SQ-010 | `PlayerId = clubId * CLUB_SQUAD_SIZE + localIndex`; club identity is distinct from match `teamId`. | MUST | KD-3 |
| FR-SQ-011 | `PlayerDatabaseConstants` is the constant catalogue; every constant carries exactly one `[FIXED]/[DERIVED]/[GT]/[CROSS]` tag. | MUST | KD-5 / #20 |

### Generation (FR-SQ-012..017)

| FR | Subject | Conformance | Source |
|---|---|---|---|
| FR-SQ-012 | `RosterGenerator.Generate(rng, streamIndex, clubId, count)` draws exactly `FIELDS_PER_PLAYER = 36` values per player via `Reserve(FIELDS_PER_PLAYER)` → 36× `DrawReserved` → `CloseReservation`. | MUST | §3 / F4 |
| FR-SQ-013 | The **caller** registers the RNG stream (`RosterGenerator` is stateless): siteId `"player-database.roster-generation"`, `SubsystemOrdinals.PlayerDatabase`, `entityId = clubId` — so generation is unit-testable without booting a match. | MUST | KD-5 |
| FR-SQ-014 | A `[4][31]` position-bias table (array-valued `[GT]`) adds a per-attribute bias, non-zero only at each position's signature attributes; applied inside the per-attribute clamp. | MUST | §3 / KD-5 |
| FR-SQ-015 | `WeakFootRating` is drawn with its own `WEAK_FOOT_SPREAD = 2` jitter (base 3 ± 2 spans `[1,5]` with no clamp), NOT `ATTRIBUTE_SPREAD`. | MUST | KD-2 / F2 |
| FR-SQ-016 | Two-run determinism: the same seed + `streamIndex` + `clubId` + `count` yields a byte-identical `Squad`. | MUST | KD-5 |
| FR-SQ-017 | `NameCatalogue` provides Stage-0 in-code first/last-name arrays (32 each), APPEND-only for ordinal stability. | MUST | KD-8 |

### Import (FR-SQ-018..019)

| FR | Subject | Conformance | Source |
|---|---|---|---|
| FR-SQ-018 | `SquadFileLoader.Parse(text, clubId)` reads a `[player N]` `key = value` grammar (`#` comments, InvariantCulture numeric parsing); an omitted key inherits the mid-range default (`CreateDefault` value / `"Player N"` / `Midfielder` / age 25). | MUST | KD-8 |
| FR-SQ-019 | The text import is not a determinism-pinned wire format — only the resulting `PlayerRecord` values feed the sim, never the grammar; a future richer/binary encoding is a pure parser swap. | MUST | KD-8 |

### Integration and exclusion (FR-SQ-020..021)

| FR | Subject | Conformance | Source |
|---|---|---|---|
| FR-SQ-020 | A `Squad` is excluded from `SNAPSHOT_SCHEMA_VERSION` (boot-constant, never-mutated-mid-match — the `_attrs`/`_perfs` exclusion class). | MUST | KD-7 |
| FR-SQ-021 | Generation and parsing run at club-setup time, not per-tick, and are not governed by the zero-alloc game-loop rule. | MUST | KD-6 |

### Landed T-phase wiring (FR-SQ-022..026)

| FR | Subject | Conformance | Source |
|---|---|---|---|
| FR-SQ-022 | `[T1, LANDED]` `MatchEngine` kickoff seeding sources attributes from a configured `Squad` (`ConfigureSquads`); this is intentionally NOT behaviour-neutral (distinct players is the point). | MUST | §7 |
| FR-SQ-023 | `[T2, LANDED]` per-spec projections (`PlayerAttributeProjection`) read real `Crossing`/derived `KickPower`/`WeakFootRating`/etc. from the canonical record — closing `ERR-007`. | MUST | §7 |
| FR-SQ-024 | `[T3, LANDED]` a per-team roster reference (`_rosterClubId = Squad.ClubId`, sentinel `NO_ROSTER_CLUB_ID = -1`) is serialized in the snapshot header at `SNAPSHOT_SCHEMA_VERSION` 16 — identity, not values (KD-7). | MUST | §7 / KD-7 |
| FR-SQ-025 | `[Phase-2, LANDED]` distinct-squad restore re-projection re-derives per-slot attributes from the resolved roster via an `ISquadProvider`, keyed by the serialized `_activeBenchSlot`. | MUST | §7 |
| FR-SQ-026 | `[LANDED]` `LineupSelector` performs proper per-line selection (mean-attribute greedy, `PlayerId` tie-break) replacing the roster-order trust mapping. | MUST | §7 |

## 2.2 Data structures

Field lists confirmed against `src/player-database/`.

### 2.2.1 `PlayerAttributes` (struct)

31 `int [1,20]` fields + 1 `int WeakFootRating [1,5]`. Each `[1,20]` field's doc cites its real consumer
or `RESERVED`.

| Group | Fields (consumer) |
|---|---|
| Physical (6) | Pace, Acceleration, Agility, Balance, Strength, Stamina (Agent Movement #2) |
| Technical (7) | Passing, Technique (Pass #5); Finishing, LongShots (Shot #6); Dribbling, Crossing (Decision Tree #8 — `Crossing` declared-but-unconsumed, `ERR-008-006`); Heading (Heading #10) |
| Mental (7) | Decisions, Vision, Composure, Anticipation, WorkRate, Aggression, Positioning (Decision Tree #8; Composure also Shot #6 / GK #11) |
| Goalkeeping (6) | Reflexes, Handling, Aerial, OneVsOne, Throwing, Kicking (Goalkeeper #11) |
| Reserved (5) | Tackling, Marking, Concentration, Teamwork (master-plan §4.2, no Stage-0 consumer); FirstTouchAbility (CONSUMED since T1 — three live sites: #13/#14/#4 — per projection-design KD-P9) |
| Special-scale (1) | WeakFootRating `[1,5]` (Pass #5 / Shot #6) |

Helpers: `CreateDefault()` (all `[1,20]` = 10, WeakFootRating = 3, FR-SQ-005); `ToArray()` /
`FromArray(int[31])` over the 31 fields in `AttrIdx` order, `WeakFootRating` excluded (FR-SQ-003);
`FromArray` throws `ArgumentException` on a length ≠ `AttrIdx.Count` (F1). `AttrIdx` is the named
ordinal map (`AttrIdx.Count = 31`) shared by the array helpers, the generator, and the loader.

### 2.2.2 `PlayerPosition` (enum)

`Goalkeeper = 0, Defender = 1, Midfielder = 2, Forward = 3` — coarse squad-management position,
APPEND-only (ordinal indexes the position-bias table). NOT positioning-ai's `RoleId` (KD-4).

### 2.2.3 `PlayerRecord` (struct)

`{ int PlayerId, string FirstName, string LastName, int Age, PlayerPosition Position,
PlayerAttributes Attributes }`. `PlayerId` is club-scoped (KD-3). `CreateDefault(playerId)` yields the
identity record (`"Player"` / `playerId` / age 25 / `Midfielder` / `CreateDefault()` attributes).

### 2.2.4 `Squad` (sealed class)

`{ int ClubId, int Count, PlayerRecord GetPlayer(int index) }` over a private `PlayerRecord[]`. The ctor
snapshot-copies the caller's array (post-construction caller mutation cannot reach the instance) and
refuses a null / empty / `> CLUB_SQUAD_SIZE` roster (F3). `GetPlayer` throws
`ArgumentOutOfRangeException` outside `[0, Count)`. `ClubId` is NOT a match `teamId` (KD-3).

### 2.2.5 `PlayerDatabaseConstants`

The constant catalogue (Appendix). `[FIXED]` `ATTRIBUTE_MIN=1` / `ATTRIBUTE_MAX=20` /
`WEAK_FOOT_MIN=1` / `WEAK_FOOT_MAX=5` / `CLUB_SQUAD_SIZE=25`. `[DERIVED]` `ATTRIBUTE_COUNT=31` /
`IDENTITY_DRAWS_PER_PLAYER=5` / `FIELDS_PER_PLAYER=36`. `[GT]` `ATTRIBUTE_BASE_MEAN=10` /
`ATTRIBUTE_SPREAD=4` / `AGE_MIN=17` / `AGE_MAX=35` / `WEAK_FOOT_BASE=3` / `WEAK_FOOT_SPREAD=2` + the
`[4][31]` position-bias table. `[CROSS]` `DOMAIN_TAG_PLAYER_DATABASE=0x1F` /
`SubsystemOrdinals.PlayerDatabase=81` (mirrored from Deterministic Simulation #16 §3.4).

## 2.3 Serialization

A `Squad` is **not serialized** into the match snapshot (KD-7 / FR-SQ-020) — it is boot-deterministic,
never-mutated-mid-match data. The only snapshot surface this layer feeds is the T3 per-team roster
reference (`_rosterClubId`, a club **id** in the snapshot header, `SNAPSHOT_SCHEMA_VERSION` 16), which
records *which* squad each team loaded (identity), not per-player attribute values; those values are
re-projectable from the roster keyed by the serialized `_activeBenchSlot` (§7).

## 2.4 Failure modes

| F | Mode | Handling |
|---|---|---|
| F1 | `FromArray` receives a non-31-length array, or an attribute value reaches a consumer outside `[1,20]` | fail loud (`ArgumentException`) at the array seam; generation clamps to `[ATTRIBUTE_MIN, ATTRIBUTE_MAX]` before assignment |
| F2 | `WeakFootRating` outside `[1,5]` | fail loud; the `WEAK_FOOT_SPREAD = 2` jitter around base 3 spans `[1,5]` exactly so a correct draw never clamps (KD-2) |
| F3 | A `Squad` is built with a null / empty / `> CLUB_SQUAD_SIZE` roster | ctor throws (`ArgumentNullException` / `ArgumentException`) — never truncates silently |
| F4 | `RosterGenerator`'s per-player `Reserve` budget ≠ `FIELDS_PER_PLAYER` | locked count assertion (a future field addition that desyncs the budget fails the test, not the run) |
| F5 | `SquadFileLoader` meets an unknown section/key, a duplicate key/section, an out-of-range or unparsable value | `FormatException` (fail loud, never silent fallback); an omitted key inherits the mid-range default |

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-22 | — | Initial FR set (FR-SQ-001..026), data structures (confirmed against `src/player-database/`), failure modes F1–F5. |
#endregion
