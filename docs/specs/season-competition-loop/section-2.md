# Season & Competition Loop Specification #30 — Section 2: Functional Requirements, Data Structures, Failure Modes

**Created:** July 22, 2026
**Last Updated:** July 22, 2026 (v0.1)
**Version:** 0.1
**Status:** IN REVIEW
**Source:** `docs/tracking/season-competition-loop-design.md` v0.2

---

## 2.1 Functional requirements

FR prefix **FR-SN**. MUST/SHOULD per RFC-2119. All requirements describe intended behaviour of the
forward design (nothing is built yet).

### Fixture generation

| ID | Requirement | Level | KD |
|---|---|---|---|
| FR-SN-001 | Fixture generation MUST be a pure, deterministic function of `(clubIds, seed)` — same inputs ⇒ byte-identical `Fixture[]`, with no `System.Random` / `DateTime` / ambient state. | MUST | KD-5 |
| FR-SN-002 | For `N` clubs the schedule MUST be a **double** round-robin: every ordered pair `(home, away)` with `home ≠ away` appears exactly once (`N·(N−1)` fixtures over `2·(N−1)` rounds). | MUST | KD-5 |
| FR-SN-003 | Within any one round, each club MUST appear in at most one fixture (no club plays twice in a round). | MUST | — |
| FR-SN-004 | Generation MUST reject `N < 2` (fail-loud) and MUST handle odd `N` via the standard bye rotation (circle method with a phantom club). | MUST | F1 |

### League table

| ID | Requirement | Level | KD |
|---|---|---|---|
| FR-SN-005 | The table MUST hold, per club: Played, Won, Drawn, Lost, GoalsFor, GoalsAgainst, GoalDifference (= GF − GA), Points. | MUST | — |
| FR-SN-006 | `ApplyResult(home, away, homeGoals, awayGoals)` MUST update both clubs' rows: +3 Pts to the winner (or +1 each on a draw), P/W/D/L, and GF/GA/GD, with points values in the `[GT]` catalogue (App. A). | MUST | — |
| FR-SN-007 | The ordered table view MUST apply the pinned Stage-0 tie-break order **Points → GoalDifference → GoalsFor → ClubId** (ascending ClubId as the final deterministic tiebreak). The order MUST be a total order (no two clubs ever compare equal). | MUST | KD-6 |
| FR-SN-008 | `ApplyResult` MUST reject a result for a club not in the table, a self-fixture (`home == away`), or a negative goal count (fail-loud). | MUST | F2 |

### Calendar & match-day flow

| ID | Requirement | Level | KD |
|---|---|---|---|
| FR-SN-009 | `SeasonCalendar` MUST hold a cursor over the fixture rounds (which round is next) and the mapping from round → calendar day. | MUST | KD-4 |
| FR-SN-010 | `AdvanceToNextFixtureDay()` MUST advance the world one `WorldStore.AdvanceDay()` per intervening calendar day up to (and including) the next fixture day, in the fixed KD-2 tick order. | MUST | KD-2 |
| FR-SN-011 | The calendar cursor's "next fixture day" MUST always be `≥` the current `WorldClock` day; a restore MUST re-validate this invariant and fail-loud on violation. | MUST | KD-4 |
| FR-SN-012 | `PlayNextFixture(ISquadProvider)` MUST play the fixture at the cursor through a real `MatchEngine`, resolve each club's `Squad` via the provider (`ConfigureSquads`), and advance the cursor by one. | MUST | KD-2 |
| FR-SN-013 | Playing a fixture MUST derive a `MatchResult` (scoreline + per-club goals) from the match engine's authoritative score state / event ledger, `ApplyResult` it to the table, and emit the FR-SN-018 match-outcome event — in that order. | MUST | — |

### Board objectives & job-security

| ID | Requirement | Level | KD |
|---|---|---|---|
| FR-SN-014 | `BoardState` MUST hold the literal Stage-0 objective (`FinishAtOrAbove(position P)`) and a job-security scalar / state. | MUST | KD-6 |
| FR-SN-015 | Board evaluation MUST run at the season boundary (pass/fail against final position) and MUST expose a running "on track?" read from the current table position (a projection, not a mutation of the objective). | MUST | KD-6 |

### Match-outcome producer (NOT ingest)

| ID | Requirement | Level | KD |
|---|---|---|---|
| FR-SN-016 | Each played fixture MUST emit exactly one structured `MatchResult` match-outcome event, deterministic in the result it summarizes. | MUST | KD-3 |
| FR-SN-017 | #30 MUST NOT wire the match-outcome event into #22's phase-1 ingest, and MUST NOT add an ingest entry point to #22. #30 is the **producer only**; ingest activation is gated on #33 (`FR-LW-032`). | MUST | KD-3 |
| FR-SN-018 | When #30 references the world layer it MUST reference only `WorldStore`'s public surface, never `living-world` internals (FR-LW-003). | MUST | KD-3 |

### Save / restore

| ID | Requirement | Level | KD |
|---|---|---|---|
| FR-SN-019 | Season state MUST persist as a **third opaque sub-blob** in `SeasonSaveCodec`, with its own `SEASON_STATE_FORMAT_VERSION`; the codec MUST NOT parse the world or match sub-blobs. | MUST | KD-1 |
| FR-SN-020 | The outer `SEASON_SAVE_FORMAT_VERSION` MUST bump **1 → 2**; the world blob (`WORLD_STORE_FORMAT_VERSION`) and match blob (`MATCH_SAVE_FORMAT_VERSION`) MUST stay byte-untouched. | MUST | KD-1 |
| FR-SN-021 | `SeasonSaveManager.Save`/`Load` MUST gain a season parameter (`Save(world, season, matchOrNull, path)` / `Load(...) → (world, season, matchOrNull)`); capture MUST complete before the file is opened (the blob-before-file precedent). | MUST | KD-1 |
| FR-SN-022 | Save→restore of the full season state (table + fixtures + calendar + board) MUST be byte-identical through one file (round-trip determinism). | MUST | — |
| FR-SN-023 | The season codec MUST fail-loud on: a `SEASON_STATE_FORMAT_VERSION` / `SEASON_SAVE_FORMAT_VERSION` mismatch, an out-of-bounds length prefix (overflow-safe bound), or trailing bytes — the `MatchSaveCodec`/`WorldStateSerializer` posture. | MUST | F3 |
| FR-SN-024 | A save may land **mid-sequence** in the KD-2 day-advance; a restore MUST equal an uninterrupted advance (`save@day-N mid-advance → restore → advance to N+K == an uninterrupted run`). | MUST | KD-2 |

### Determinism & neutrality

| ID | Requirement | Level | KD |
|---|---|---|---|
| FR-SN-025 | The whole loop MUST run on the world tick (`WorldClock`), never the 10 Hz / 60 Hz match loops. | MUST | KD-8 |
| FR-SN-026 | A no-fixture day MUST advance the world **byte-identically** to a bare `WorldStore.AdvanceDay()` (behaviour-neutral world floor). | MUST | KD-8 |
| FR-SN-027 | Any genuinely stochastic season event MUST draw through a dedicated season RNG sub-stream (`DOMAIN_TAG_SEASON_LOOP = 0x22`, `SubsystemOrdinals.SeasonLoop = 84`); fixture generation is deterministic-from-seed and needs no draw for the single-league case. | MUST | KD-5 |
| FR-SN-028 | The concrete fixture list MUST be serialized in the season blob (not regenerated on load), so a loaded season is independent of generator-version drift. | MUST | KD-5 |

### Multi-season continuity

| ID | Requirement | Level | KD |
|---|---|---|---|
| FR-SN-029 | The season-boundary roll MUST be a single restartable, round-trip-deterministic transform (finalize table → evaluate board → regenerate fixtures for the next season → advance ages [null seam] → reset table). | MUST | KD-6 |
| FR-SN-030 | A two-run simulated season from the same seed MUST reach a byte-identical final table (end-to-end determinism). | MUST | — |
| FR-SN-031 | The boundary roll MUST preserve a well-defined insertion point for #43's promotion/relegation transform (between "finalize table" and "regenerate fixtures") without changing the surrounding steps. | SHOULD | KD-6 |

### Command surface & view model

| ID | Requirement | Level | KD |
|---|---|---|---|
| FR-SN-032 | `SeasonLoop` MUST be the sole writer of season state; season state MUST be mutable only through the public command API (`AdvanceToNextFixtureDay`, `PlayNextFixture`, the boundary roll), never by field access. | MUST | KD-7 |
| FR-SN-033 | `SeasonViewModel` MUST expose the table + fixture list + calendar position as **read-only value copies** for #37/#38; reading MUST NOT mutate season state or affect the save digest (observer-neutral). | MUST | KD-7 |
| FR-SN-034 | Every world-tick spec #30 must tick that does not exist yet (#28/#29/#33) MUST be a **documented null seam** in the KD-2 tick order, never an invented interface (FR-LW-031). | MUST | KD-2 |

## 2.2 Data structures

- **`Fixture`** (readonly struct): `RoundIndex (int)`, `HomeClubId (int)`, `AwayClubId (int)`,
  `Played (bool)` — plus the resolved result once played is recorded on the table, not the fixture
  (the fixture list is the immutable schedule; `Played` is the only mutable-on-play field).
- **`LeagueTableRow`** (value type): `ClubId (int)`, `Played/Won/Drawn/Lost (int)`,
  `GoalsFor/GoalsAgainst (int)`, `GoalDifference (int, = GF−GA)`, `Points (int)`.
- **`LeagueTable`** (sealed class over `LeagueTableRow[]`): `ApplyResult(...)`, `OrderedView()`
  (returns rows in the FR-SN-007 tie-break order — read-only copy).
- **`SeasonCalendar`** (value type): `NextRoundIndex (int)`, `RoundToDay (int[])` (round → world-day),
  the KD-4 cursor.
- **`BoardObjective`** (readonly struct): `TargetPositionOrBetter (int)`.
- **`BoardState`** (value type): `Objective (BoardObjective)`, `JobSecurity (float/enum)`.
- **`MatchResult`** (readonly struct): `HomeClubId`, `AwayClubId`, `HomeGoals`, `AwayGoals`,
  `RoundIndex`, `WorldDay` — the match-outcome producer payload (KD-3).
- **`SeasonState`** (sealed class): `Seed (ulong)`, `ClubIds (int[])`, `Fixtures (Fixture[])`,
  `Table (LeagueTable)`, `Calendar (SeasonCalendar)`, `Board (BoardState)`, `SeasonNumber (int)` —
  the season sub-blob's serialized surface.
- **`SeasonLoop`** (sealed class, the composition root): owns `SeasonState`; holds references to the
  `WorldStore` and the active-or-null `MatchEngine`; exposes the command API + `Snapshot()`/`Restore()`
  for the season sub-blob.
- **`SeasonViewModel`** (readonly struct): read-only value copies of the table view, fixture list, and
  calendar position for #37/#38.

## 2.3 Failure modes

| ID | Trigger | Response |
|---|---|---|
| F1 | `FixtureScheduler.Generate` with `N < 2` | throw (fail-loud); no partial schedule |
| F2 | `ApplyResult` with unknown club / self-fixture / negative goals | throw; table unchanged |
| F3 | Season codec: bad format version / out-of-bounds length prefix / trailing bytes | throw from `Decode`; no partial restore (the `MatchSaveCodec` posture) |
| F4 | Restore with "next fixture day < current WorldClock day" (KD-4 invariant violated) | throw; corrupt/inconsistent save rejected |
| F5 | `PlayNextFixture` when the cursor is past the last fixture (season already complete) | throw / documented no-op per §3 — the caller must run the boundary roll first |
| F6 | `PlayNextFixture` with an `ISquadProvider` that cannot resolve a fixture's `ClubId` | throw from the match restore/config path (the #27 `ISquadProvider` fail-loud contract) |

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-22 | — | Initial FR set FR-SN-001..034, data structures, failure modes F1–F6, from supplement v0.2. |
#endregion
