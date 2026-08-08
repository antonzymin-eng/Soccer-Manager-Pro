# Season & Competition Loop Specification #30 — Section 4: Architecture

**Created:** July 22, 2026
**Last Updated:** August 8, 2026, later same day (v0.5 — AR pass 14 L4: §4.2's leftover 1 → 2 delta line corrected to 1 → 4; the tests list marked illustrative)
**Last Updated (prior):** August 8, 2026 (v0.4 — balance-pass AR pass 13 M3: §4 was three landings stale — §4.4's third signature copy deleted in favour of Appendix B, §4.3 gains the career pair + AdvanceDays, §4.2 the eight T1/T2/D2 files)
**Last Updated (prior):** July 26, 2026 (v0.3 — ERR-030-012 §4.5 keyed-not-cursor correction + ERR-030-013 §4.6 producer-record location, both found at #30 T2 implementation; prior v0.2 section-file PASS-1 reconciliation, §9.3)
**Version:** 0.5
**Status:** APPROVED
**Source:** `docs/tracking/season-competition-loop-design.md` v0.2

---

## 4.1 Assembly placement

The season loop lives in the existing **`TacticalDirector.SeasonSave`** assembly (`src/season-save/`)
— the composition/persistence layer that already sits **above both** `match-engine` and
`living-world` and is the only assembly permitted to reference both (FR-LW-003 keeps those two
independent; the season root is their `match-viewer`-over-`match-engine` layer class). #30 adds no new
cross-layer reference direction: it extends a root that already references
`TacticalDirector.MatchEngine` + `TacticalDirector.LivingWorld` + `TacticalDirector.DeterministicSim`,
and (for FR-SN-012's `ConfigureSquads` / `ISquadProvider`) the `TacticalDirector.PlayerDatabase`
reference the season-save tests already carry.

```
                 TacticalDirector.SeasonSave   (season loop lives here — above both)
                    │            │
        ┌───────────┘            └───────────┐
   match-engine                         living-world
   (#1..#17 substrate)                  (#22 — never references match-engine, FR-LW-003)
        │                                    │
        └──────────── deterministic-sim ─────┘   (#16 — referenced by all)
```

**No new assembly.** `SeasonLoop`, `FixtureScheduler`, `LeagueTable`, `SeasonCalendar`, `BoardState`,
`MatchResult`, `SeasonState`, `SeasonViewModel` are new files under `src/season-save/`, alongside
`SeasonSaveManager`/`SeasonSaveCodec`/`SeasonSaveConstants`.

## 4.2 File layout (proposed — pinned at T-phase)

```
src/season-save/
├── season-save.asmdef            ← existing; gains PlayerDatabase ref (already in the tests asmdef)
├── SeasonSaveConstants.cs        ← existing; SEASON_SAVE_FORMAT_VERSION 1 → 4 across #30 T1 / #29-#41 T1 / D2; + SEASON_STATE_FORMAT_VERSION
├── SeasonSaveCodec.cs            ← existing; Encode/Decode gain the season block (§4.4)
├── SeasonSaveContents.cs         ← existing; gains the reconstructed SeasonState
├── SeasonSaveManager.cs          ← existing; Save/Load gain the season parameter (§4.4)
├── SeasonState.cs                ← NEW: the serialized season surface (§2.2)
├── Fixture.cs                    ← NEW
├── FixtureScheduler.cs           ← NEW: static, pure Generate(clubIds, seed) → Fixture[]
├── LeagueTableRow.cs             ← NEW
├── LeagueTable.cs                ← NEW: ApplyResult + OrderedView (tie-break)
├── SeasonCalendar.cs             ← NEW
├── BoardObjective.cs             ← NEW
├── BoardState.cs                 ← NEW
├── MatchResult.cs                ← NEW: the match-outcome producer payload
├── SeasonViewModel.cs            ← NEW: read-only surface for #37/#38
├── SeasonLoop.cs                 ← NEW: the composition root (sole writer; command API; Snapshot/Restore)
├── SeasonStateCodec.cs           ← NEW: the season sub-blob codec (§3.6) — pure, testable in memory
├── PlayerCareerStates.cs         ← #29/#41 T2 (§2.2): the three parallel per-club career sets — the
│                                    #30-side owner of both subsystems' state + the F8 cursor predicates
├── AppearanceState.cs            ← balance pass D2 (§2.2 / Appendix B.1): the lazily-shifted day-bitmask
├── ClubAppearanceStates.cs       ← balance pass D2: one club's appearance third of the career triple
├── AppearanceWindow.cs           ← balance pass D2: the windowed read + the [1,31] runtime guard
├── AppearanceSaveCodec.cs        ← balance pass D2: the APPR sub-blob codec (Appendix B.1)
├── AppearanceBlock.cs            ← balance pass D2: the typed frame block (ERR-029-005's compile-time half)
├── TrainingBlock.cs              ← #29/#41 T1: typed frame block for the #29 sub-blob
├── MedicalBlock.cs               ← #29/#41 T1: typed frame block for the #41 sub-blob
└── tests/                        (illustrative — `file-manifest.md` is the authoritative inventory)
    ├── season-save-tests.asmdef  ← existing
    ├── FixtureSchedulerTests.cs  ← NEW
    ├── LeagueTableTests.cs       ← NEW
    ├── SeasonStateCodecTests.cs  ← NEW
    ├── SeasonLoopTests.cs        ← NEW: day-advance / play-fixture / boundary-roll / round-trip
    └── SeasonSaveManagerTests.cs ← existing; extended for the season parameter
```

## 4.3 The `SeasonLoop` composition root

`SeasonLoop` (sealed) is the **sole writer** of `SeasonState` (KD-7 / FR-SN-032). It holds:

- `SeasonState _state` — the season sub-blob's surface (owned; never handed out by reference).
- `WorldStore _world` — the day-advance substrate (referenced, not owned; #22 owns its lifecycle).
- `MatchEngine _activeMatch` — the in-progress fixture, or null between fixtures (KD-3 / KD-1
  matchPresent flag).
- the optional **career PAIR** (since #29/#41 T2, §2.2): `PlayerCareerStates _career` + the
  `ISquadProvider` it was bound to — half-supplied or later swapped is refused (§2.3 F7), and the
  career's cursors are clock-checked at construction (F8).

Public command API (the only mutation path, FR-SN-032): `AdvanceToNextFixtureDay()`, `AdvanceDays(n)`
(the bounded free-advance — refused past the season's last fixture day and past the next season's
opening day, KD-4), `AdvanceAndPlayNextRound(ISquadProvider)` (resolves the whole round, KD-9),
`RollToNextSeason()`, plus read-only `View()` → `SeasonViewModel`
(FR-SN-033) and `Snapshot()` / `Restore(...)` for the season sub-blob. It is **not** on the 60 Hz hot
path (§1.2 world-tick cadence), so allocation / `new` / exceptions are permitted — the
`SeasonSaveManager` / `WorldStore` precedent.

## 4.4 The `SeasonSaveManager` / `SeasonSaveCodec` surface (FR-SN-019..021)

**The authoritative signature and frame layout live in FR-SN-021 (§2.1) and Appendix B / B.1 — this
section deliberately no longer restates them.** *(AR pass 13 M3: this section held the THIRD copy of the
signature — pass 11 corrected the §2 copy after "three landings and three parameters stale", and this
copy plus its v1→2 frame text had drifted identically. A third copy is not re-synchronised; it is
deleted — the parallel-surface rule applied to spec text.)* What this section keeps is the one
architecture-level rule the frame does not state: `Save` captures every blob **before** opening the
file — the blob-before-file / atomic temp→fsync→rename contract (§4.6.1.1) — and the world/match
sub-blobs stay opaque and byte-untouched at every frame change (FR-SN-020).

## 4.5 RNG-stream registration (FR-SN-027 / KD-5)

> **Corrected at #30 T2 implementation (ERR-030-012).** The paragraph below described a registered,
> cursor-positioned `DeterministicRngService` stream. That is **incompatible with §3.4.1's own
> requirement** that the round-resolution model's draws be *keyed on the fixture* rather than
> cursor-positioned, so that resolving a round's fixtures in any order yields the same table
> (T-SN-CAL-003c) — a cursor makes each scoreline depend on how many fixtures were drawn before it.
> T2 therefore realizes the season sub-stream as a **keyed derivation**: `DOMAIN_TAG_SEASON_LOOP` is
> folded into `FixtureKey(seasonSeed, seasonNumber, roundIndex, homeClubId, awayClubId)`, which is that
> tag's first consumer and satisfies ERR-030-001's "code const at T2's first draw site".
> `SubsystemOrdinals.SeasonLoop = 84` is **not** allocated in code at T2: a subsystem ordinal exists
> only to key a *registered* stream, so a code const with no stream behind it would be the zero-consumer
> phantom FR-LW-031 forbids. Ordinal 84 stays reserved in #16 §3.4's spec text for the first genuinely
> cursor-positioned season event (a #43 cup draw is the likely first), which will register
> `season-loop.season-events` as described below.

**Superseded description, retained for the reservation it records:** `SeasonLoop` would register a
`DeterministicRngService` stream at construction (or reuse the `WorldStore`'s service) with siteId
`"season-loop.season-events"`, `SubsystemOrdinals.SeasonLoop = 84`, `entityId: SeasonNumber`. The
generator is a pure static that takes an already-drawn permutation (or the identity), so
`FixtureScheduler` is testable without a season boot — the #27 `RosterGenerator` stateless posture.
Back-prop (§7): `DOMAIN_TAG_SEASON_LOOP = 0x22` + `SubsystemOrdinals.SeasonLoop = 84` land in #16 §3.4
at approval (only #30's row; `0x20`/`0x21` stay gaps for #28/#29 per KD-5's honesty note).

## 4.6 The #22 producer-not-consumer boundary (KD-3 / FR-SN-016..018)

`SeasonLoop.EmitMatchOutcome(result)` records the `MatchResult` and is the phase-1
**producer**. It **does not** call any #22 ingest method — none exists (`WorldLoop` phase-1 has no
interface, FR-LW-031), and #30 **must not add one** (FR-SN-017). The only #22 surface #30 touches is
`WorldStore`'s public API (`AdvanceDay`/`Snapshot`/`Restore`/`CurrentWorldTick`), never `living-world`
internals (FR-SN-018 / FR-LW-003).

> **Where the record lives — corrected at #30 T2 (ERR-030-013).** This section originally said
> `EmitMatchOutcome` records the result "in `SeasonState`". It cannot: §2.2 and Appendix B give
> `SeasonState` no outcome collection, and adding one would be a `SEASON_STATE_FORMAT_VERSION` bump
> carrying a payload with **no consumer** — #22 ingest does not exist and FR-SN-017 forbids #30 from
> creating it. T2 therefore keeps the producer record **loop-scoped and transient**
> (`SeasonLoop.MatchOutcomes`, a read-only value-copy collection); the *durable* record of what happened
> is the league table, which is serialized. FR-SN-016's requirement is unchanged and satisfied — exactly
> one structured, deterministic `MatchResult` is emitted per played fixture — and #33 subscribes to this
> surface when it lands, at which point whether the payload also needs persisting is a #33-side decision
> co-defined with `FR-LW-027`/`FR-LW-032`.

The eventual ingest is a #22 wiring change at #33's landing
(`FR-LW-032`), co-defining the payload against `FR-LW-027`/`FR-LW-032`/living-world KD-9/KD-10 — a #22 edit, not a
#30 one.

## 4.7 The CS0104 hazard note (carried from #27 T1)

`SeasonState` / `MatchResult` are new names in `SeasonSave`, so no collision today. But `SeasonLoop`
already sees `MatchEngine.MatchEngine` (namespace == class, requiring the `TacticalDirector.MatchEngine.MatchEngine`
fully-qualified form the existing `SeasonSaveManager` uses) and, via `ConfigureSquads`,
`PlayerDatabase.PlayerAttributes` (the #27 v1.73 CS0104 class). The T-phase implementer must
fully-qualify `MatchEngine` and any `player-database` type that shares a bare name with a
`match-engine` type **from the first line that needs it**, not discover it via a failed build (the
#27 §4 / `src/CLAUDE.md` v1.73 lesson).

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-22 | — | Initial architecture: SeasonSave-assembly extension, file layout, SeasonLoop root, the codec/manager signature change, RNG registration, the #22 producer boundary, CS0104 hazard. |
| 0.2 | 2026-07-22 | — | Section-file PASS-1 reconciliation (whole-round KD-9 command/API rename, living-world-KD disambiguation, KD/FR label fixes). See section-9 §9.3. |
| 0.3 | 2026-07-26 | — | **ERR-030-012** (found at T2 implementation): §4.5's registered cursor-positioned season stream contradicts §3.4.1's keyed-draw requirement (T-SN-CAL-003c order-independence). T2 realizes the sub-stream as a keyed derivation folding `DOMAIN_TAG_SEASON_LOOP` into the fixture key — that tag's first consumer, satisfying ERR-030-001 — and does NOT allocate `SubsystemOrdinals.SeasonLoop = 84` in code, since an ordinal with no registered stream is the FR-LW-031 phantom; ordinal 84 stays spec-reserved for the first cursor-positioned season event. **ERR-030-013** (same landing): §4.6's "records the `MatchResult` in `SeasonState`" is not implementable — §2.2 / Appendix B give `SeasonState` no outcome collection, and adding one would bump `SEASON_STATE_FORMAT_VERSION` for a payload FR-SN-017 forbids a consumer for; the producer record is loop-scoped and transient, the durable record is the serialized table. |
| 0.4 | 2026-08-08 | — | **Balance-pass AR pass 13 (M3)**: §4 had been untouched through T1/T2/D2 and every AR pass while `src/CLAUDE.md` orders implementers to read it before coding — §4.4 held the THIRD copy of the Save/Encode signature (the class pass 11 fixed in §2), still showing four arguments, the v1→2 bump and a five-field frame against today's seven-argument Save, version 4 and eight-field frame; §4.3's state list had no career pair and no `AdvanceDays`; §4.2's layout missed all eight T1/T2/D2 files. §4.4's copy DELETED in favour of a pointer to Appendix B (a third copy is not re-synchronised); the rest brought current. |
| 0.5 | 2026-08-08 | — | **Balance-pass AR pass 14 (L4)**: the pass-13 M3 rewrite left `SEASON_SAVE_FORMAT_VERSION 1 → 2` five lines above the D2 files it added — the contradiction the fix was closing, re-introduced one block apart; corrected to 1 → 4, tests list marked illustrative. |
#endregion
