# Squad / Player Data Layer Specification #27 — Section 1: Introduction, Scope, Dependencies

**Created:** July 22, 2026
**Last Updated:** July 22, 2026 (v0.1)
**Version:** 0.1
**Status:** IN REVIEW
**Source:** `docs/tracking/squad-player-data-design.md` v0.6

---

## 1.1 Purpose and scope

This spec defines the **canonical player-data layer** — the source of truth for player attributes and
rosters that the match engine seeds agents from. **In scope:** the canonical `PlayerAttributes` record
(31 `int [1,20]` fields + `WeakFootRating [1,5]`) and its array/default helpers; the coarse
`PlayerPosition` classification; the `PlayerRecord` identity + `Squad` club-roster container;
deterministic `RosterGenerator`; the Stage-0 `SquadFileLoader` text import; and the `MatchEngine`
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
  `entityId = team * PLAYERS_PER_TEAM + slot` to an unbounded club count).
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
#endregion
