# Data-Contract Index — where each entity is defined

> **Created:** August 21, 2026
> **Purpose:** Answer one question fast — *"where is `<entity>` defined, and where is it implemented?"*
> — for the data that crosses assembly boundaries. It is a **map**, not a definition.
> **Governance:** not a spec, not a design supplement. It confers no authority and settles no
> question. It is a lookup table over facts that already exist elsewhere.

---

## 0. What this file is NOT — read this before adding to it

This index exists **instead of** a `DATA_SCHEMA.md`-style master document, and the distinction is
the whole point. This project's recurring structural defect is the **parallel surface**: a second
place that says the same thing, drifts, and then two documents disagree with no rule about which
wins. It has been found here as `LineupSelector.CanSelect` (a hand-copied second selection walk,
#29/#41 T2 AR pass 1 H3), as the **three** copies of `SeasonSaveManager.Save`'s signature (#30 §4 /
Appendix B / the catalogue, AR pass 13), and as the two hand-copied five-predicate cursor walks
collapsed at AR pass 9. A master schema document would be the same defect at the widest possible
scope.

So, three hard rules:

1. **Restate nothing.** No field lists, no C# types, no ranges, no units, no formulas, no version
   *values*. A row may name a type and point at its owner. The moment a row tells you what is *in*
   a type, it has become a second source of truth and must be cut back to a pointer.
2. **The pointer targets win, always.** If this file disagrees with a spec section, with a file's
   own `// Spec:` header, or with `src/`, **they are right and this file is stale**. Fix the row;
   never "reconcile" by editing the spec to match the index.
3. **Rows, not columns.** Adding a row for a new entity is routine. Adding a *column* that carries
   semantics (a type, a size, a default, a range) turns the index into a schema — refused.

**Verify any row in one command.** Every production `.cs` file in this tree carries a `// Spec:`
header naming its owning spec and section — **682 of 682 non-test `src/*.cs` files, measured
August 21, 2026** — and that header, not this file, is the authority:

```bash
grep -m1 '^// Spec:' src/<assembly>/<Type>.cs
```

`docs/specs/SPEC_INDEX.md` maps spec number → folder. The root `CLAUDE.md` assembly map is the
reliable index of which assemblies exist. `docs/tracking/file-manifest.md` is the authoritative
file inventory. This file adds only the entity → (spec §, assembly) hop those three do not make.

---

## 1. Player and squad

| Entity | Owning spec § | Assembly | Type(s) |
|---|---|---|---|
| Canonical player record | #27 §2.2.3 | `player-database` | `PlayerRecord` |
| Canonical player attributes | #27 §2.2.1 | `player-database` | `PlayerAttributes` (`TacticalDirector.PlayerDatabase`) — **see §9 hazard 1** |
| Player position | #27 §2.2.2 | `player-database` | `PlayerPosition` |
| Squad (roster container) | #27 §2.2.4 | `player-database` | `Squad` (sealed, immutable) |
| Player-database constants | #27 §2.2.5 | `player-database` | `PlayerDatabaseConstants` |
| Per-layer attribute projections | `player-attribute-projection-design.md` | consuming assembly, one each | `DtAgentAttributes`, `GoalkeeperAgentAttributes`, `HeadingAgentAttributes`, `PassAgentAttributes`, `PerceptionAgentAttributes`, `ShotAgentAttributes`, `AgentMovement.PlayerAttributes` |
| Roster resolution at the match boundary | `match-engine-design.md` | `match-engine` | `ISquadProvider` |
| **Career roster authority** (evolving attributes) | #28 KD-4 | `player-progression` | `ProgressionEngine` — the sole writer |
| Career roster → `ISquadProvider` projection | #30 §2.2 | `season-save` | `ProgressionSquads` (lives here because #28 §4.1 forbids #28 to reference a `match-engine` type) |

> **The one thing to know here.** Since #28 T1/T2a a career's roster is a function of the **save**,
> not of the world seed (#28 KD-4). If you need "the current squad", ask `ProgressionSquads` when a
> career is populated — not `League`, and not a seed rebuild.

---

## 2. Club, league, season

| Entity | Owning spec § | Assembly | Type(s) |
|---|---|---|---|
| Club | `league-bootstrap-design.md` KD-2/KD-3 | `season-save` | `Club` |
| League (also the default squad provider) | `league-bootstrap-design.md` KD-9 | `season-save` | `League : ISquadProvider`, `LeagueBootstrap`, `ClubNameCatalogue` |
| Fixture | #30 §2.2 | `season-save` | `Fixture` |
| League table | #30 §2.2 | `season-save` | `LeagueTable`, `LeagueTableRow` |
| Season calendar / day cursor | #30 §2.2 | `season-save` | `SeasonCalendar` |
| Board objective & job security | #30 §2.2 | `season-save` | `BoardObjective`, `BoardState` (job security becomes a derived read over #45 at its T2) |
| Match outcome payload | #30 §2.2 | `season-save` | `MatchResult` |
| Season state (serialized surface) | #30 §2.2, Appendix B | `season-save` | `SeasonState` |
| Season composition root | #30 §2.2 | `season-save` | `SeasonLoop` |
| Season read model for UI/analytics | #30 §2.2 | `season-save` | `SeasonViewModel` |

---

## 3. Per-player career state (three parallel sets, one owner)

| Entity | Owning spec § | Assembly | Type(s) |
|---|---|---|---|
| **Owner of all three sets** | #30 §2.2 | `season-save` | `PlayerCareerStates` — keyed `(ClubId, PlayerId)`; the single place #30 calls either sibling from |
| Training state | #29 §2.2 | `training-system` | `ClubTrainingStates`, `TrainingSchedule` |
| Medical / injury state | #41 §2.2 | `injuries-medical` | `ClubInjuryStates` |
| Appearance record | #30 Appendix B.1 | `season-save` | `ClubAppearanceStates`, `AppearanceState` |
| Progression / career state | #28 §2.2, §3.5 | `player-progression` | `ClubCareerStates` |

> Player ids are **globally unique**, not club-scoped — `ERR-041-019` / `ERR-027-004`. #27 FR-SQ-010
> was amended to say so; the keyed injury draw depends on it.

---

## 4. Match runtime and determinism

| Entity | Owning spec § | Assembly | Type(s) |
|---|---|---|---|
| RNG service and per-stream state | #16 §2.3, §3.2 | `deterministic-sim` | `DeterministicRngService`, `RngStreamState`, `SubsystemOrdinals` |
| Replay position / phase identity | #16 §2.3, §5.10 | `deterministic-sim` | `ReplayCursor`, `PhaseId`, `ReplayEngine`, `TickOrchestrator` |
| Despawn ledger (Tier A state) | #16 §2.3, §3.2.5.3 | `deterministic-sim` | `DespawnLog`, `DespawnEntry` |
| Environment fingerprint / divergence | #16 §4.8 | `deterministic-sim` | `EnvironmentFingerprint`, `DivergenceDetector`, `DivergenceClass`, `DeterminismTier` |
| Snapshot header | #16 §2.3, §4.8 | `deterministic-sim` | `SnapshotHeader`, `SnapshotPayload`, `SnapshotCodec` |
| Match world-state snapshot (the live one) | `match-engine-design.md` | `match-engine` | `SNAPSHOT_SCHEMA_VERSION` in `MatchEngineConstants` — **see §9 hazard 2** |
| Match clock / phase | #16, #8 | `deterministic-sim`, `decision-tree` | `MatchClock`, `MatchPhase`, `MatchContext` |
| Per-layer AI snapshots | #14, #15 | `defensive-ai`, `attacking-ai` | `DefensiveSnapshot`, `AttackingSnapshot`, and their `*AgentSnapshot` rows |
| Match statistics | #37 §2.2 | `match-analytics` | `MatchAnalyticsResult`, `MatchAnalyticsAggregator`, `MatchEngineObservation` |

> **Four `#16 §2.3` names do not resolve to types.** That section lists `DeterminismContext`,
> `PhaseDigest`, `RngStreamKey` and `RngCursor` as data structures; none exists under those names in
> `src/deterministic-sim/`, whose closest surfaces are the four rows above. This is recorded as an
> observation, not a defect claim — the concepts are implemented, the *names* differ — but it is why
> this index points at `src/` for the type and at the spec for the contract, and never pretends the
> two vocabularies are one. Verify with `grep -rn 'struct \|class ' src/deterministic-sim/`.

> `match-analytics` is presentation-layer derivation: **no sim assembly may reference it**, and that
> is enforced mechanically.

---

## 5. Save formats — where the version constants live

Deliberately **no version values here** — a table of numbers goes stale the first time anyone bumps
one, and a stale version number is worse than no table. The constant name and its file are stable;
read the value from the file.

| Format | Constant | File |
|---|---|---|
| Season save frame (the root) | `SEASON_SAVE_FORMAT_VERSION` | `src/season-save/SeasonSaveConstants.cs` |
| Season sub-blob | `SEASON_STATE_FORMAT_VERSION` | `src/season-save/SeasonSaveConstants.cs` |
| Appearance sub-blob (`APPR`) | `APPEARANCE_SAVE_FORMAT_VERSION` | `src/season-save/SeasonSaveConstants.cs` |
| Progression sub-blob (`PROG`) | `PROGRESSION_SAVE_FORMAT_VERSION` | `src/player-progression/PlayerProgressionConstants.cs` |
| Training sub-blob | `TRAINING_SAVE_FORMAT_VERSION` | `src/training-system/TrainingSystemConstants.cs` |
| Medical sub-blob | `MEDICAL_SAVE_FORMAT_VERSION` | `src/injuries-medical/InjuriesMedicalConstants.cs` |
| Match save | `MATCH_SAVE_FORMAT_VERSION` | `src/match-engine/MatchEngineConstants.cs` |
| World store / world snapshot (#22) | `WORLD_STORE_FORMAT_VERSION`, `WORLD_SNAPSHOT_FORMAT_VERSION` | `src/living-world/LivingWorldConstants.cs` |
| Scenario manifest (#19) | `SCENARIO_MANIFEST_FORMAT_VERSION` | `src/testing-strategy/TestingStrategyConstants.cs` |

> **The rule these blocks were bought with:** *a format version is not a format identifier*
> (`ERR-029-005` / `ERR-041-009` / `ERR-028-004`). Every sub-blob is magic-led and typed at the frame
> seam, because four same-shaped `byte[]` payloads at one call site decoded each other's bytes
> cleanly, completely and silently. Byte layouts are pinned in **#30 Appendix B**; do not restate one
> here. Bumping any of these is governed by the `snapshot-schema-bump` skill.

---

## 6. Tactics

| Entity | Owning spec § | Assembly | Type(s) |
|---|---|---|---|
| Team tactic | #21 §2.2.1 | `tactical-instructions` | `TeamTactic` |
| Player instructions | #21 §2.2.2 | `tactical-instructions` | `PlayerInstructions` |
| Per-agent resolved tactic | #21 §2.2.3 | `tactical-instructions` | `PlayerTactic` |
| Tactic enums (APPEND-only) | #21 §2.2.4, Appendix A | `tactical-instructions` | see §2.2.4 |
| Tactic presets | #26 | `tactical-instructions` | `ITacticPresetCatalogue` + implementations |
| Engine-side tactic config | `match-engine-design.md` | `match-engine` | `TeamTacticConfig`, `PlayerTacticConfig` |
| AI-side tactical context | #8 | `decision-tree` | `TacticalContext` |

---

## 7. Presentation

| Entity | Owning doc | Assembly | Type(s) |
|---|---|---|---|
| Render models, pitch projection, markings | `interactive-unity-client-design.md` §5-P4a | `match-client-core` | `MatchRenderProjection`, `AgentRenderModel`, `BallRenderModel`, `PitchViewProjection`, `PitchMarkings`, `MatchRoster` |
| Shell decisions (speed ladder, control gating) | `interactive-unity-client-design.md` §5-P5a | `match-client-core` | `PlaybackSpeedLadder`, `MatchControlAvailability`, `MatchControlLockReason` |
| Screen **identity type** | #38 | `ui-framework` | `ScreenId` — the type; it carries no screen list |
| The four screens + navigation graph | `interactive-unity-client-design.md` §5-P5a / v0.17 | `client-app` | `ClientScreens` (the catalogue), `ClientScreenFlow`, `ClientAppConstants` |
| UI framework substrate | #38 | `ui-framework` | T0 substrate only — no screens, no UGUI binding |

---

## 8. Specified but NOT implemented — no assembly exists

These entities have an **APPROVED spec and no code**. There is nothing in `src/` to point at, and
that is the single most important thing this index can tell you: an APPROVED spec says nothing
about whether an implementation exists, and it is true of roughly 42% of the registry.

| Spec | Folder | Entities it will own |
|---|---|---|
| #31 | `transfers-contracts-negotiation/` | transfer offers, contracts, negotiation state |
| #32 | `scouting-player-knowledge/` | scout reports, knowledge/uncertainty over `PlayerRecord` |
| #33 | `personalities-morale-dynamics/` | personality, morale, the pairwise social graph |
| #34 | `staff-backroom/` | staff records, roles, staff contracts |
| #35 | `media-press-interactions/` | press events, interview state |
| #36 | `national-teams-international/` | national squads, call-ups |
| #39 | `steam-packaging-release/` | build/release artefacts |
| #40 | `club-finances-economy/` | budgets, wage bill, transactions |
| #42 | `youth-academy-intake/` | intake cohorts, youth candidates |
| #43 | `competition-structure/` | cups, knockouts, multi-competition calendar |
| #44 | `discipline-suspensions/` | cards, bans, suspension state |
| #45 | `board-ownership-dynamics/` | board confidence, ownership |
| #46 | `news-inbox-man-management/` | inbox items, man-management interactions |
| #47 | `new-game-setup-db-editor/` | **authored** starting data (incl. new-game `PotentialAbility`) |
| #48 | `match-presentation-depth/` | commentary/presentation depth |
| #49 | `localization-accessibility/` | localization keys, template contracts |
| #50 | `save-migration-versioning/` | cross-version save migration |
| #51 | `audio-sound-design/` | audio events and mixing |
| #53 | `club-infrastructure-facilities/` | facilities, infrastructure levels |
| #54 | `manager-career-reputation/` | manager tenure, reputation, job market |

> **Do not write an interface against a row in this table.** "Write interfaces only when both sides
> are specified" (root `CLAUDE.md`; `ERR-001`/`ERR-004`), and **`path-to-playable-roadmap.md` C6**:
> a spec with no assembly is not hardened, extended or wired ahead of its own T0 landing. Findings
> against these specs are recorded and discharged at T0.

---

## 9. Name hazards — same word, different thing

**1. There are two `PlayerAttributes` types, and they are not interchangeable.**

| | Canonical | Locomotion |
|---|---|---|
| Namespace | `TacticalDirector.PlayerDatabase` | `TacticalDirector.AgentMovement` |
| Owner | #27 §2.2.1 | #2 §3.5.1, §4.5.1 |
| Role | the career-authoritative record | one of ~7 per-layer projections fed *from* the canonical record |

The other projections (`DtAgentAttributes`, `PassAgentAttributes`, `ShotAgentAttributes`,
`GoalkeeperAgentAttributes`, `HeadingAgentAttributes`, `PerceptionAgentAttributes`) are named
distinctly and are less likely to be confused; `AgentMovement.PlayerAttributes` is the one that
shares its name with the canonical type. The field-by-field mapping and its scale semantics live in
`player-attribute-projection-design.md` — **not here**.

**2. There are two `SNAPSHOT_SCHEMA_VERSION` constants.**

| File | Governs |
|---|---|
| `src/match-engine/MatchEngineConstants.cs` | the live match world-state snapshot — **this is the one the `snapshot-schema-bump` skill means** |
| `src/deterministic-sim/DeterministicSimConstants.cs` | #16's own snapshot contract |

Bumping the wrong one is silent.

**3. "Squad" is overloaded three ways** — `PlayerDatabase.Squad` (the roster container, #27 §2.2.4),
`ISquadProvider` (the match-boundary resolution seam, `match-engine`), and `ProgressionSquads` (the
career-roster projection, `season-save`). They are three different layers of the same word.

---

## Version History

| Version | Date | Change |
|---------|------|--------|
| v1.0 | August 21, 2026 | Initial index. Created after a proposed `DATA_SCHEMA.md` master-schema document was rejected as a parallel surface over #27/#30/#16, which already own these contracts — this file is the pointer-only alternative that was adopted instead. Covers player/squad, club/league/season, the three career-state sets, match runtime and determinism, save-format constant locations, tactics, presentation, the 20 specified-but-unimplemented entity sets, and three name hazards (two `PlayerAttributes` types, two `SNAPSHOT_SCHEMA_VERSION` constants, three senses of "Squad"). §0's three rules — restate nothing, the pointer targets win, rows not columns — are what keep it from becoming the document it replaced. |
