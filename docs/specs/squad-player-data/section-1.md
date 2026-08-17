# Squad / Player Data Layer Specification #27 — Section 1: Introduction, Scope, Dependencies

**Created:** July 22, 2026
**Last Updated:** August 8, 2026 (v0.3 — ERR-027-004 pointer on KD-3: the career-level global-uniqueness requirement lives on FR-SQ-010; KD-3 is the first thing a #42/#31 allocator author reads, so it points there — balance-pass AR pass 5 L3)
**Last Updated (prior):** July 27, 2026 (v0.2 — back-prop landed atomically with the ten-spec approval wave; see the version-history row)
**Last Updated (prior):** July 22, 2026 (v0.1)
**Version:** 0.3
**Status:** APPROVED
**Source:** `docs/tracking/squad-player-data-design.md` v0.6

---

## 1.1 Purpose and scope

This spec defines the **canonical player-data layer** — the source of truth for player attributes and
rosters that the match engine seeds agents from. **In scope:** the canonical `PlayerAttributes` record
(31 `int [1,20]` fields + `WeakFootRating [1,5]`) and its array/default helpers; the coarse
`PlayerPosition` classification; the `PlayerRecord` identity + `Squad` club-roster container;
deterministic `RosterGenerator` — whose **draw contract is save-visible without being saved**, see the
note below; the Stage-0 `SquadFileLoader` text import; and the `MatchEngine`
integration that consumes all of the above (the landed T1–T3 wiring, §7).

**Out of scope (explicitly, per design supplement §0):** season progression, aging, retirement,
training, the transfer market, on-disk save-format squad persistence, and any UI. This is a **data
layer**, a Stage-1-forward pull (master plan §4.2 places the player database at Stage 2) motivated by
the present-day gap it fills — every one of the 22 match agents was seeded with identical mid-range
(`10`) attributes, so nothing could test or tune how the engine behaves with genuinely varied players.

**Promotion posture (unusual).** The layer is **already built and wired** — the code preceded this
numbered spec (the reverse of the #21/#22 design-supplement-first order). Section text therefore
documents the existing `src/player-database/` + `src/match-engine/` implementation **in present
tense**; §7 records the landed T-phase status rather than a forward plan.

## 1.2 Convention inheritance

Inherits the project conventions verbatim: corner-origin pitch (Ball Physics #1 §1.2); fatigue
`0.0 = rested, 1.0 = fatigued`; every constant carries exactly one `[FIXED]/[DERIVED]/[GT]/[CROSS]`
tag; deterministic RNG only (SplitMix64/HKDF-SipHash via #16 — no `System.Random`). Attributes use the
`int [1,20]` scale stated in the pre-existing `PlayerAttributes`/`DtAgentAttributes` doc comments;
`WeakFootRating` is a separate `[1,5]` scale (KD-2).

### 1.2.1 The generation contract is save-visible without being saved (ERR-027-003, at #50's approval)

**Rosters are regenerated from the world seed, not persisted.** `WorldStore.WorldSeed`'s own doc comment
states it: *"Squads are not persisted, so resuming a career means calling
`LeagueBootstrap.Generate(world.WorldSeed, season.ClubCount)`."* A career's entire playing population is
therefore a function of **two saved integers and the generator's current code**.

**The consequence, recorded here because it constrains what may be changed after ship:**
`RosterGenerator`'s **draw order and per-player field budget**, `LeagueBootstrap`'s **club-name
catalogue**, and its **strength ramp** are covered by **`WORLD_GENERATION_VERSION`** (#50 KD-2). Changing
any of them post-ship requires a **version bump plus a generation migration** — because the same seed will
otherwise silently produce a different league in every existing save.

`LeagueBootstrapGoldenVectorTests` remains the **CI** guard, and it is the right guard for an *accidental*
change: it fires when the output moves unintentionally. It says nothing about a **deliberate** change
shipped in an update, which is precisely #50's domain. **This back-prop adds the runtime guard the golden
vector never was**, and changes no #27 code, type or requirement.

## 1.3 Dependencies

| Dep | Direction | Nature |
|---|---|---|
| Deterministic Sim #16 | this → it | sole reference: `DeterministicRngService` (roster-generation draws); `DOMAIN_TAG_PLAYER_DATABASE = 0x1F` + `SubsystemOrdinals.PlayerDatabase = 81` allocated there (KD-5) |
| Match Engine (design note) | it → this | consumes a `Squad` at kickoff (`ConfigureSquads`) and projects the canonical record into every per-spec attribute struct (`PlayerAttributeProjection`); §7 |
| Agent Movement #2 / Decision Tree #8 / Pass #5 / Shot #6 / Heading #10 / Goalkeeper #11 / Perception #7 | it → this (via match-engine) | each per-spec attribute struct becomes a **projection** of the canonical record (KD-1) — this spec touches none of their code |
| positioning-ai #12 (`RoleId`) | none | explicitly **not** a dependency — `PlayerPosition` ≠ `RoleId` (KD-4) |
| Code Standards #20 | governs | layering, naming, constant tags |

`TacticalDirector.PlayerDatabase` references **only** `TacticalDirector.DeterministicSim` — it sits at
the bottom of the reference graph, off-pitch band (§4).

## 1.4 Key decisions

- **KD-1 — Canonical record is the single source of truth.** One `PlayerAttributes` struct in the new
  assembly is authoritative. Every existing per-spec attribute struct (`PassAgentAttributes`,
  `ShotAgentAttributes`, `DtAgentAttributes`, …) stays where it is; a match-engine projection derives
  each from the canonical record instead of `STAGE0_NEUTRAL_ATTRIBUTE`, closing the long-open `ERR-007`
  gap (the spec text was patched in 2026 but `AgentMovement.PlayerAttributes` never gained the fields).
- **KD-2 — `int [1,20]`; `WeakFootRating [1,5]` on a separate scale.** All 31 canonical attributes are
  `int` in `[1,20]`. `WeakFootRating` is a different `[1,5]` scale, kept as a distinct field and
  **never** folded into the `[1,20]` array (`ToArray`/`FromArray`) or the `[1,20]` clamp helpers.
- **KD-3 — Club identity ≠ match-team identity.** A **club** (up to `CLUB_SQUAD_SIZE = 25` players) is a
  league-wide entity, independent of any match; match concepts (`SQUAD_SIZE = 22`,
  `PLAYERS_PER_TEAM = 11`, `teamId ∈ {0,1}`) are match-scoped. This layer keys on a caller-assigned
  `clubId`, never `teamId`. `PlayerId = clubId * CLUB_SQUAD_SIZE + localIndex` (generalises the engine's
  `entityId = team * PLAYERS_PER_TEAM + slot` to an unbounded club count). *A career carrying more than
  one club additionally requires ids to be GLOBALLY unique across its clubs — see FR-SQ-010 as amended
  by ERR-027-004 (#41's occurrence-draw key has no club term); today's formula satisfies it, and any
  future allocator MUST preserve it.*
- **KD-4 — `PlayerPosition` ≠ `RoleId`.** `PlayerPosition` (4 coarse values) is a squad-management
  classification for generation/display; positioning-ai's `RoleId` (13 granular formation-slot roles)
  is a different concept. No shared type, no cross-reference. A future `PlayerPosition → RoleId` mapping
  is **not invented here** (Interface Design Principle — don't write it against an unspecified consumer).
- **KD-5 — Determinism.** Roster generation is the layer's only randomness source and draws exclusively
  through `DeterministicRngService`. New #16 allocations (back-prop, the `ERR-022-001` precedent):
  `DOMAIN_TAG_PLAYER_DATABASE = 0x1F`, `SubsystemOrdinals.PlayerDatabase = 81` (off-pitch 80–99 band,
  alongside `LivingWorld = 80` — roster generation is boot/off-match-tick).
- **KD-6 — Not zero-alloc, not a hot path.** Generation and text parsing run at **club-setup time**,
  never per-tick — the CLAUDE.md struct/zero-allocation game-loop rule does not apply here (exactly as
  for `TeamTacticFileLoader`/`PlayerTacticFileLoader`). `Squad` is a plain sealed class over an array.
- **KD-7 — Not in the match snapshot.** A generated/loaded `Squad` is boot-deterministic,
  never-mutated-mid-match data — the `_attrs`/`_perfs` exclusion class — so it is excluded from
  `SNAPSHOT_SCHEMA_VERSION`. The T3 roster reference that does enter the snapshot is a **club-id
  in the snapshot header** (identity), not per-player attribute values (§7).
- **KD-8 — Import is Stage-0 text, not a wire format.** `SquadFileLoader` mirrors the tactic-file
  loaders: a human-authoring `[player N]` `key = value` grammar, fail-loud on anything unrecognised,
  omitted key ⇒ mid-range default. Only the resulting `PlayerRecord` **values** matter, never the
  grammar — it is not a determinism-pinned format; the real on-disk save format is Stage-1+.

## 1.5 Boundary matrix

| # | Boundary | This spec | Other side |
|---|---|---|---|
| 1 | Club roster identity | owns `Squad`/`PlayerRecord`, keyed by `clubId` | — |
| 2 | Match-team assignment (home/away, `teamId`) | never used as an identifier | Match Engine owns `_teamIds` |
| 3 | Canonical attribute values | owns `PlayerAttributes` (single source of truth) | per-spec structs are projections (match-engine) |
| 4 | Coarse position | owns `PlayerPosition` (4 values) | positioning-ai #12 owns `RoleId` (13 values) — no shared type (KD-4) |
| 5 | Determinism substrate | consumes only | #16 owns `DeterministicRngService` + the domain tag / ordinal |
| 6 | Match snapshot framing | excluded (boot-constant, KD-7) | #16 owns the codec; the T3 header roster-id is match-engine's |

## 1.6 Stage binding

Stage-1-forward pull. Types + generation + import are the T0 slice; the `MatchEngine` seeding
(`ConfigureSquads`), the per-spec projections (`PlayerAttributeProjection`, closing `ERR-007`), the
snapshot roster reference, the distinct-squad restore re-projection, and proper lineup selection are
**all landed** (§7). Stage-1+ (persistence, transfers, aging, training) is deferred (§7, §1.1).

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-22 | — | Initial section from supplement v0.6; documents the built-and-wired layer in present tense; KD-1..KD-8 carried from supplement §2. |
| 0.2 | 2026-07-27 | — | **ERR-027-003** (at #50's approval): new **§1.2.1** records that the generation contract is **save-visible without being saved** — rosters are regenerated from the world seed, so `RosterGenerator`'s draw order and field budget, `LeagueBootstrap`'s catalogue and its strength ramp are covered by `WORLD_GENERATION_VERSION`, and changing any post-ship needs a version bump plus a generation migration. The golden vector stays the CI guard against an *accidental* change; this is the **runtime** guard it never was. No #27 code, type or requirement change. |
| 0.3 | 2026-08-08 | — | **ERR-027-004** (balance-pass AR pass 5, L3): KD-3 gains the one-sentence pointer to FR-SQ-010's career-level GLOBAL-uniqueness amendment — the FR carried the contract for one pass while the key decision every allocator author reads first said nothing. |
#endregion
