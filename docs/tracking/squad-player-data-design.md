# Squad / Player Data Layer — Design Supplement

> **Created:** July 15, 2026
> **Status:** DESIGN SUPPLEMENT (pre-promotion — no section files, no `SPEC_INDEX.md` row yet).
> Candidate spec number **#27** (next free per `SPEC_INDEX.md`), reserved informally in this
> doc only; not added to the registry until/if this promotes to section files, per the #21–#26
> precedent (registry rows land at promotion, not at design-note stage).
> **Purpose:** Scope and design a Squad/Player data layer — a canonical player attribute record,
> deterministic roster generation, and a Stage-0 text-import format — to replace the match
> engine's current all-synthetic, all-neutral agent seeding.

---

## 0. Scope and governance

This is a **Stage-1-forward pull**, exactly like #21/#22: the master development plan
(`docs/planning/master-development-plan.md` §4.2, "Squad Management") places a player database
at Stage 2 (Year 3), but the match engine already has a concrete, present-day gap it creates:
every one of the 22 match agents is seeded with identical mid-range (`10`) attributes
(`PlayerAttributes.CreateDefault()`, `DtAgentAttributes.CreateDefault()`,
`STAGE0_NEUTRAL_ATTRIBUTE = 10f`), so there is no way to test or tune how the engine behaves
with genuinely varied players. `DtAgentAttributes.cs`'s own header comment has anticipated this
since May 29: *"Populated by the simulation orchestrator each heartbeat from the player data
store"* — that store has never existed. This doc scopes the minimum layer that makes that true.

**Explicitly out of scope for this pass:** season progression / aging / retirement / training
(`master-development-plan.md` §4.4, §4.3 transfer system) — all Stage-2 UI/economy features.
This is a **data layer** only: a canonical attribute record, deterministic generation, and
import — not squad management, not a UI, not a transfer market.

---

## 1. What exists vs. what this adds

Seven independent, drifting attribute structs exist today, each a narrow per-spec projection,
all populated with identical neutral defaults, never sourced from real data:

| Struct | Spec | Fields | Populated from |
|---|---|---|---|
| `PlayerAttributes` | Agent Movement #2 | Pace, Acceleration, Agility, Balance, Strength, Stamina | `CreateDefault()` (all 10) |
| `DtAgentAttributes` | Decision Tree #8 | 15 attrs incl. Decisions, Vision, Passing, Finishing... | `CreateDefault()` (all 10) |
| `PerceptionAgentAttributes` | Perception #7 | Decisions, Anticipation | `CreateDefault()` |
| `PassAgentAttributes` | Pass Mechanics #5 | Passing, Technique, KickPower, WeakFootRating, Crossing | `STAGE0_NEUTRAL_ATTRIBUTE`, proxied (`[ERR-007-PENDING]`) |
| `ShotAgentAttributes` | Shot Mechanics #6 | Finishing, LongShots, Composure, KickPower, Technique, WeakFootRating | `STAGE0_NEUTRAL_ATTRIBUTE` |
| `HeadingAgentAttributes` | Heading #10 | Heading, Strength, Balance | `CreateDefault()` |
| `GoalkeeperAgentAttributes` | Goalkeeper #11 | Reflexes, Handling, Aerial, OneVsOne, Throwing, Kicking, Composure, Strength, Balance, Pace | `CreateDefault()` |

**`ERR-007`** (`spec-error-log.md:31`) is marked *Closed — resolved in
Agent_Movement_Spec_Section_3_5_v1_3.md*, but `src/agent-movement/PlayerAttributes.cs` never
actually gained `KickPower`/`WeakFootRating`/`Crossing` — only the spec text was patched.
`PassAgentAttributes.cs` still carries the `[TEMPORARY-PROXY-ERR-007]` tags today. This layer's
canonical record is the natural place to close that gap for real: every per-spec struct becomes
a genuine **projection** of one master record instead of an independently-drifting proxy.

**This adds:** a bottom-of-graph `src/player-database/` assembly owning (a) a canonical
`PlayerAttributes` record reconciling all seven structs above plus the master plan's V1 list,
(b) a deterministic `RosterGenerator`, (c) a Stage-0 human-authoring text import format, and
(d) a `Squad` container. **Wiring these into `MatchEngine`** (replacing `CreateDefault()`
seeding) is explicitly a later T-phase (§5) — this pass is data types + generation + import only,
matching the project's established T0-scaffolding-first precedent (#21, #22, #23–26).

---

## 2. Key decisions

- **KD-1 (canonical record).** One `PlayerAttributes` struct in the new assembly is the single
  source of truth. Every existing per-spec struct (`PassAgentAttributes`, `ShotAgentAttributes`,
  etc.) stays where it is — this doc does not touch those specs' code — but a later T-phase
  projects each from the canonical record instead of `STAGE0_NEUTRAL_ATTRIBUTE`, closing `ERR-007`
  for real. Field set (§3) = every attribute actually consumed by existing code today, reconciled
  by name, plus a small set of master-plan-only fields kept `RESERVED` (declared-but-unconsumed,
  same documented pattern as `DtAgentAttributes.Crossing` / `ERR-008-006`).
- **KD-2 (int [1,20] convention).** All attributes are `int` in `[1,20]`, matching the stated
  convention in `PlayerAttributes.cs`/`DtAgentAttributes.cs` doc comments. `GoalkeeperAgentAttributes`
  today uses `float` — pre-existing drift in that spec, not fixed here; the eventual GK projection
  casts from the canonical `int`. `WeakFootRating` is `[1,5]`, a different scale — kept as a
  separate field, never folded into the `[1,20]` attribute set or its clamp/array helpers.
- **KD-3 (club vs. match-team identity).** A **club** (up to 25 players, `CLUB_SQUAD_SIZE`) is a
  league-wide entity, independent of any match. `MatchEngineConstants.SQUAD_SIZE = 22` /
  `PLAYERS_PER_TEAM = 11` / `_teamIds[i] ∈ {0,1}` are match-scoped concepts (which 2 clubs are
  playing, and which is home/away) — this layer never uses `teamId` as an identifier. Roster
  generation and `PlayerRecord` are keyed by a caller-assigned `clubId`, deliberately distinct
  from match `teamId`, to avoid conflating "home/away slot in this match" with "which of the
  league's clubs." `PlayerId = clubId * CLUB_SQUAD_SIZE + localIndex` (mirrors the existing
  `entityId = team * PLAYERS_PER_TEAM + slot` convention in `MatchEngine.cs`, generalized to an
  unbounded club count instead of a fixed 2).
- **KD-4 (position ≠ formation role).** `PlayerPosition` (Goalkeeper/Defender/Midfielder/Forward,
  4 values) is a coarse squad-management classification for roster generation/display — it is
  **not** `positioning-ai`'s `RoleId` (13-value granular formation-slot role). No shared type,
  no cross-reference; a future T-phase may define a `PlayerPosition → RoleId` mapping when squads
  are wired into formation seeding, but that mapping does not exist yet and is not invented here
  (Interface Design Principle — don't write it against an unspecified consumer).
- **KD-5 (determinism).** Roster generation is the only place this layer draws randomness, and it
  goes through `DeterministicRngService` exactly like every other subsystem — no `System.Random`.
  New allocations (back-propped into Deterministic Simulation #16, mirroring the `ERR-022-001`
  precedent): `DOMAIN_TAG_PLAYER_DATABASE = 0x1F` (next free after `0x1E`) and
  `SubsystemOrdinals.PlayerDatabase = 81` (next free in the off-pitch 80–99 band, alongside
  `LivingWorld = 80` — roster generation is boot/off-match-tick, the same category).
- **KD-6 (not zero-alloc, not a hot path).** Roster generation and text-file parsing run at
  club-setup time, never per-tick — the CLAUDE.md struct/zero-allocation rule governs the game
  loop and does not apply here, exactly as it doesn't for `TeamTacticFileLoader`/
  `PlayerTacticFileLoader`. `Squad` is a plain class holding a `PlayerRecord[]`.
- **KD-7 (not in the match snapshot).** Per the existing `_attrs`/`_perfs` EXCLUSION PROOF
  (`MatchEngine.cs:2782`), boot-deterministic, never-mutated-mid-match data is correctly excluded
  from `SNAPSHOT_SCHEMA_VERSION`. A loaded/generated `Squad` is exactly that class of data — it
  does not enter the per-tick snapshot in this pass. (If a future T-phase adds save/restore
  fidelity for "which roster was loaded," that is a roster-reference id in the snapshot *header*,
  not per-player attribute values — noted as a T-phase item, not designed here.)
- **KD-8 (import is Stage-0 text, not a wire format).** `SquadFileLoader` mirrors
  `TeamTacticFileLoader`/`PlayerTacticFileLoader` exactly: a human-authoring `key = value` grammar
  under `[player N]` sections, fail-loud on anything unrecognised, omitted key ⇒ mid-range default
  (`10`, matching every existing `CreateDefault()`). This is **not** a determinism-pinned format —
  only the resulting `PlayerRecord` values matter, never the grammar. The real on-disk save format
  is explicitly Stage-1+/Stage-2 (master plan §4.6, "JSON-based for V1"), out of scope here.

---

## 3. New types

`src/player-database/` (namespace `TacticalDirector.PlayerDatabase`; `references:
["TacticalDirector.DeterministicSim"]` — the one dependency, for `DeterministicRngService`).

### `PlayerAttributes` (struct)

31 `int [1,20]` fields + 1 `WeakFootRating [1,5]`. Grouped, each tagged with its real consumer or
`RESERVED`:

| Group | Fields (consumer) |
|---|---|
| Physical (6) | Pace, Acceleration, Agility, Balance, Strength, Stamina (Agent Movement #2) |
| Technical (8) | Passing, Technique (Pass #5); Finishing, LongShots (Shot #6); Dribbling, Crossing (Decision Tree #8, `Crossing` unconsumed per `ERR-008-006`); Heading (Heading #10) |
| Mental (8) | Decisions, Vision, Composure, Anticipation, WorkRate, Aggression, Positioning (Decision Tree #8); Composure also consumed by Shot #6/GK #11 |
| Goalkeeping (6) | Reflexes, Handling, Aerial, OneVsOne, Throwing, Kicking (Goalkeeper #11) |
| Reserved (5) | Tackling, Marking, Concentration, Teamwork, FirstTouchAbility — master plan §4.2 attributes; declared for forward compatibility, same pattern as `DtAgentAttributes.Crossing`. **Correction (T1, per projection-design KD-P9):** `FirstTouchAbility` is in fact CONSUMED — three live `MatchEngine` sites (#13 `FirstTouchAttribute`, #14 `PerceivedFirstTouch`, #4 `FirstTouchContext.FirstTouchAttribute`) read it (as a neutral placeholder pre-T1, as the real value since T1); only the other four rows remain reserved. |
| Special-scale (1) | WeakFootRating `[1,5]` (Pass #5 / Shot #6) |

`CreateDefault()` = every `[1,20]` field `10`, `WeakFootRating = 3` (matches every existing
struct's mid-range convention). `ToArray()`/`FromArray(int[31])` + a named `AttrIdx` constants
class give the 31 fields a single ordinal mapping shared by the generator and the loader
(avoids a duplicated 31-way switch in two places).

### `PlayerPosition` (enum): `Goalkeeper, Defender, Midfielder, Forward`

### `PlayerRecord` (struct): `PlayerId (int), FirstName, LastName (string), Age (int), Position (PlayerPosition), Attributes (PlayerAttributes)`

### `Squad` (sealed class): `ClubId (int), PlayerRecord[] Players` (≤ `CLUB_SQUAD_SIZE` = 25), `Count`, `GetPlayer(i)`.

### `PlayerDatabaseConstants`

`[FIXED]` `ATTRIBUTE_MIN=1/MAX=20`, `WEAK_FOOT_MIN=1/MAX=5`, `CLUB_SQUAD_SIZE=25` (master plan
§4.2 — deliberately named to not collide with `MatchEngineConstants.SQUAD_SIZE`). `[DERIVED]`
`ATTRIBUTE_COUNT=31`, `IDENTITY_DRAWS_PER_PLAYER=5`, `FIELDS_PER_PLAYER=36` (5 identity draws —
first name, last name, age, **position**, weak foot — + 31 attribute draws; implementation caught
the position draw missing from this v0.3 count, corrected here). `[GT]` `ATTRIBUTE_BASE_MEAN=10`,
`ATTRIBUTE_SPREAD=4` (jitter ±4), `AGE_MIN=17/MAX=35`, `WEAK_FOOT_BASE=3`,
`WEAK_FOOT_SPREAD=2` (its own — narrower — jitter half-width: base 3 ± 2 exactly spans the valid
[1,5] range with no clamping; reusing `ATTRIBUTE_SPREAD`=4 here would clamp ~6 of every 9 draws to
the boundary, another implementation-time catch), and a `[4][31]` position-bias table (array-valued
`[GT]`, the established carve-out per `TacticalInstructionsConstants`) — nonzero only at each
position's signature attributes (e.g. Goalkeeper: +4 on the 6 goalkeeping fields; Forward: +3
Finishing/Pace/Dribbling; Midfielder: +3 Passing/Vision/Stamina; Defender: +3
Tackling/Marking/Strength).

### `RosterGenerator` (static)

`Squad Generate(DeterministicRngService rng, int streamIndex, int clubId, int count)`. Per
player: `Reserve(streamIndex, FIELDS_PER_PLAYER)` → 36× `DrawReserved` (name-first index,
name-last index, age, position [uniform over the 4 values — a documented Stage-0 simplification,
not weighted toward realistic squad composition], weak foot, then 31 attributes in `AttrIdx`
order, each `Clamp(BASE_MEAN + PositionBias + jitter, 1, 20)`) → `CloseReservation`. `PlayerId =
clubId * CLUB_SQUAD_SIZE + localIndex`. Caller registers the stream (siteId
`"player-database.roster-generation"`, `SubsystemOrdinals.PlayerDatabase`, `entityId: clubId`) —
`RosterGenerator` itself is stateless, so it's testable without booting a match.

### `NameCatalogue` (static): Stage-0 in-code first/last name arrays (32 each), APPEND-only, same pattern as `InteractionTextCorpus`.

### `SquadFileLoader` (static): `Squad Parse(string text, int clubId)` — `[player N]` sections, `key = value`, `#` comments, InvariantCulture numeric parsing, fail-loud (`FormatException`) on unknown section/key, duplicate, unparsable or out-of-range value; omitted key ⇒ `PlayerAttributes.CreateDefault()` field value / `"Player N"` name / `Midfielder` / age 25.

---

## 4. Wiring changes (deferred — not built in this pass)

**Known hazard for T1:** `PlayerDatabase.PlayerAttributes` shares its bare type name with the
existing `AgentMovement.PlayerAttributes` (`src/agent-movement/PlayerAttributes.cs`, a completely
different, narrower struct). No collision today — `match-engine` does not yet reference
`player-database` — but the moment a T-phase adds that reference, every bare `PlayerAttributes` in
`MatchEngine.cs` becomes ambiguous (CS0104). This is the exact defect class the project hit at
`src/CLAUDE.md` v1.73 (`PressingAI.TacticTranslation` vs `DecisionTree.TacticTranslation` — "five
`TacticTranslation` types now in match-engine scope"). T1's writer must fully-qualify one or both
(`TacticalDirector.PlayerDatabase.PlayerAttributes`) from the first line that needs it, not
discover the error via a failed build.

Listed for sequencing only; none of this is implemented here:

- **T1** — `MatchEngine` kickoff seeding sources `_attrs`/bench attrs from a loaded/generated
  `Squad` instead of `PlayerAttributes.CreateDefault()`. Unlike typical T0→T1 steps in this
  project, this is **intentionally not behaviour-neutral** — the entire point is giving agents
  distinct attributes — so it needs its own reviewed change, not a silent default-identity landing.
- **T2** — per-spec projections (`BuildPassAttributes`, `BuildShotAttributes`, `_dtAttrs`, GK
  attrs) read real `Crossing`/`KickPower`(derived)/`WeakFootRating`/etc. from the canonical record
  instead of `STAGE0_NEUTRAL_ATTRIBUTE` — closes `ERR-007` for real.
- **T3** — snapshot header roster-reference field for save/restore fidelity (KD-7).
- **Stage-1+** — on-disk save-format squad persistence, transfer market, aging/training (master
  plan §4.3/§4.4, explicitly out of scope per §0).

---

## 5. Test plan (T0)

Unit: `PlayerAttributes` clamp/array round-trip, `CreateDefault()` values; ordinal stability of
`AttrIdx`; position-bias table exact values (not statistical — direct constant checks); generator
exact-value locks (given a fixed drawn sequence, exact expected `PlayerRecord`, incl. the
clamp boundary at extreme draws); `PlayerId` uniqueness across two `clubId`s; two-run
determinism (same seed ⇒ byte-identical `Squad`); `Reserve` budget matches
`FIELDS_PER_PLAYER` exactly (a locked count assertion, so a future field addition can't silently
desync). `SquadFileLoader`: round-trip, every fail-loud gate (unknown key/section, duplicate,
out-of-range, unparsable), empty-file ⇒ all-default squad. No closed-loop `ScenarioRunner`
scenario yet — nothing is wired into an orchestrator to exercise end-to-end (deferred to T1, once
`MatchEngine` actually consumes a `Squad`).

---

## 6. Adversarial review (self-review, converged)

**AR-1 (v0.1 → v0.2):** four findings, all fixed before this doc was finalized (folded in above,
not left as prose findings, per the project's fix-in-place convention for design-note self-review):
(1) v0.1 conflated `clubId` with match `teamId` for RNG stream keying and `PlayerId` derivation —
would have collided player IDs across two different clubs that both happened to be assigned match
slot 0 in different matches; fixed by KD-3's explicit club/match-identity separation. (2) v0.1's
canonical attribute list was an unreconciled superset of all mentions anywhere (including
speculative additions) rather than "what's actually consumed today ∪ a small documented-reserved
set" — trimmed to the 26-consumed + 5-reserved list in §3, each row citing its real consumer.
(3) v0.1 had no explicit statement that `WeakFootRating`'s `[1,5]` scale must stay out of the
`[1,20]` array/clamp helpers — added to KD-2 and called out again in `ToArray`/`FromArray`'s
description. (4) v0.1 didn't say who calls `RegisterStream` — left ambiguous whether the generator
owns registration (which would make it un-unit-testable without a full match boot); resolved by
making `RosterGenerator` stateless and pushing registration to the caller, matching the existing
card-severity-stream pattern in `MatchEngine.cs`.

**AR-2 (v0.2 → v0.3):** one finding — the position-bias table description didn't say how a test
could catch a "phantom" all-zero table (a bias table that compiles but never actually biases
anything, silently making generation position-blind); §5 now specifies **direct constant-value
assertions on the bias table itself**, not statistical sampling over generated squads, so the
test fails immediately and deterministically rather than probabilistically. No further findings —
**CONVERGED**.

---

#### Version History
| Version | Date | Notes |
|---|---|---|
| 0.1 | 2026-07-15 | Initial draft. |
| 0.2 | 2026-07-15 | AR-1: club/match identity separation (KD-3), trimmed attribute list to consumed+reserved, WeakFootRating scale isolation, stream-registration ownership. |
| 0.3 | 2026-07-15 | AR-2: position-bias table test strategy (direct constant assertions, not statistical). Converged. |
| 0.4 | 2026-07-15 | Implementation-time corrections (T0 code review, not a design-stage AR round — caught while writing `RosterGenerator`/tests): (1) `PlayerRecord.Position` had no generation input at all in v0.3 — `FIELDS_PER_PLAYER` undercounted by one draw (35 → 36; `IDENTITY_DRAWS_PER_PLAYER` 4 → 5). (2) `WeakFootRating`'s jitter reused `ATTRIBUTE_SPREAD` (±4) against its own much narrower [1,5] range, clamping most draws to the boundary instead of spreading around `WeakFootBase`; given its own `WeakFootSpread`=2 (exactly spans [1,5], no clamp). (3) `SquadFileLoader`'s identity default computed `PlayerId` from the raw section-local index instead of the club-scoped `clubId * CLUB_SQUAD_SIZE + localIndex` formula RosterGenerator uses (KD-3) — caught by a round-trip test that would have failed against the bug. All three fixed in code before this pass's own review closed. |
| 0.5 | 2026-07-17 | T1/T2 LANDED (see `player-attribute-projection-design.md` + `MatchEngine.cs` v1.37): §3's reserved-list row corrected per projection-design KD-P9 — `FirstTouchAbility` is consumed by three live `MatchEngine` sites, not reserved. §4's T1/T2 rows are now implemented (`PlayerAttributeProjection` + `ConfigureSquads`); T3 (snapshot roster reference) and Stage-1+ remain open. |
