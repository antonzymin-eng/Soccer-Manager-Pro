# Season & Competition Loop Specification #30 — Section 2: Functional Requirements, Data Structures, Failure Modes

**Created:** July 22, 2026
**Last Updated:** July 27, 2026 (v0.9 — back-props ERR-030-016 / -020 / -021 / -022 landed atomically with the ten-spec approval wave: the tick order reconciled after the duplicate `ERR-030-007` filing, FR-SN-013b's `ManagedClubId` made an explicit optional, FR-SN-034 extended to #32/#35/#53/#54)
**Last Updated (prior):** July 25, 2026 (v0.8 — back-props ERR-030-008 board tick-order + ERR-030-009 JobSecurity derived band; prior v0.7 ERR-030-007 academy, v0.6 ERR-030-006 staff, v0.5 ERR-030-004, v0.4 ERR-030-003, v0.3 ERR-030-002, v0.2 PASS-1)
**Last Updated (prior):** July 24, 2026 (v0.8 — back-prop ERR-030-009 #44 availability-filter null seam in FR-SN-013; prior v0.7 ERR-030-007, v0.6 ERR-030-006, v0.5 ERR-030-004, v0.4 ERR-030-003, v0.3 ERR-030-002, v0.2 PASS-1)
**Version:** 0.9
**Status:** APPROVED
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
| FR-SN-012 | `AdvanceAndPlayNextRound(ISquadProvider)` MUST resolve **every** fixture in the round at the cursor (all `N/2` of them) and apply **all** their results to the table, then advance the cursor by one round. Resolving a strict subset of a round's fixtures is forbidden — the table would be undefined for the unplayed clubs. | MUST | KD-2 / KD-9 |
| FR-SN-013 | Each fixture in the round MUST be resolved to a `MatchResult` (scoreline + per-club goals), `ApplyResult`-ed to the table, and emit the FR-SN-016 match-outcome event — the managed club's fixture (`SeasonState.ManagedClubId`) through the real `MatchEngine` (squads via `ISquadProvider.ResolveByClubId` → `ConfigureSquads`), the others through the round-resolution model (FR-SN-013a). **Null seam (ERR-030-009, at #44's approval):** the resolved squad MAY be filtered through the #44 suspension-availability view (a value-copy reduction) **between resolve and configure** — empty until #44 T2 wires it; the flow is then resolve → *filter* → configure. | MUST | KD-9 |
| FR-SN-013a | Non-managed fixtures MUST be resolved by a **deterministic** round-resolution model. The Stage-2 minimal identity MAY resolve every fixture (managed and non-managed) through the full `MatchEngine`; the **quick-sim** deepening resolves non-managed fixtures via a deterministic result model drawing from the `DOMAIN_TAG_SEASON_LOOP` sub-stream (FR-SN-027) — a documented Stage-2+ seam, not a rewrite. Either way, all `N/2` results apply to the table (FR-SN-012). | MUST | KD-9 |
| FR-SN-013b | `SeasonState` MUST carry a `ManagedClubId` (the human manager's club); it selects which of the round's fixtures runs through the full `MatchEngine` under the human's tactical influence (`SetTeamTactic`, #21), the rest through the round-resolution model. `ManagedClubId` MUST be serialized in the season blob. **Amended by ERR-030-021 (at #54's approval): `ManagedClubId` MUST become an explicit OPTIONAL**, because an **unemployed** manager is otherwise structurally unrepresentable — today's constructor throws when the id is not in the club set, so a career between jobs cannot be saved at all. When absent, **every** fixture in the round resolves through the round-resolution model and no `MatchEngine` runs. ◑ Spec-text-first: the text lands at approval, the representation change and its **`SEASON_STATE_FORMAT_VERSION` bump** at #54 T2 — **to be combined with `ERR-030-009`'s queued bump on the same block** so existing saves face **one** refusal boundary rather than two. | MUST | KD-9 |

### Board objectives & job-security

| ID | Requirement | Level | KD |
|---|---|---|---|
| FR-SN-014 | `BoardState` MUST hold the literal Stage-0 objective (`FinishAtOrAbove(position P)`) and a job-security scalar / state. **Amended at #45's approval (ERR-030-009):** #30 remains sole owner of the **objective**, but from **#45 T2** the job-security half MUST be a **derived band** (`JobSecurityBand`, a `u8` enum) projected on read from #45's per-club board confidence — **not** independent state. Holding an independent scalar alongside #45's confidence would be two truths for one quantity, diverging at the first restore with nothing to detect it. Consequences: the season block loses its last `float`, and the representation change is a `SEASON_STATE_FORMAT_VERSION` bump (pre-T2 saves rejected fail-loud, no migration — #50's subject). | MUST | KD-6 |
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
| FR-SN-025 | The whole loop MUST run on the world tick (`WorldClock`), never the 10 Hz / 60 Hz match loops (the world-tick-cadence convention, §1.2). | MUST | §1.2 |
| FR-SN-026 | A no-fixture day MUST advance the world **byte-identically** to a bare `WorldStore.AdvanceDay()` (behaviour-neutral world floor). | MUST | KD-8 |
| FR-SN-027 | Any genuinely stochastic season event MUST draw through a dedicated season RNG sub-stream (`DOMAIN_TAG_SEASON_LOOP = 0x22`, `SubsystemOrdinals.SeasonLoop = 84`); fixture generation is deterministic-from-seed and needs no draw for the single-league case. | MUST | KD-5 |
| FR-SN-028 | The concrete fixture list MUST be serialized in the season blob (not regenerated on load), so a loaded season is independent of generator-version drift. | MUST | KD-5 |

### Multi-season continuity

| ID | Requirement | Level | KD |
|---|---|---|---|
| FR-SN-029 | The season-boundary roll MUST be a single restartable, round-trip-deterministic transform (finalize table → evaluate board → regenerate fixtures for the next season → advance ages [null seam] → reset table). | MUST | KD-6 |
| FR-SN-030 | A two-run simulated season from the same seed MUST reach a byte-identical final table (end-to-end determinism). | MUST | — |
| FR-SN-031 | The boundary roll MUST preserve well-defined insertion points between "finalize table" and "regenerate fixtures" without changing the surrounding steps: (a') #43's promotion/relegation transform, and (b') #40's finance-settlement step (appended by ERR-030-003 at #40's approval, positioned after (a') so budgets reflect the post-promotion division). | SHOULD | KD-6 |

### Command surface & view model

| ID | Requirement | Level | KD |
|---|---|---|---|
| FR-SN-032 | `SeasonLoop` MUST be the sole writer of season state; season state MUST be mutable only through the public command API (`AdvanceToNextFixtureDay`, `AdvanceAndPlayNextRound`, the boundary roll), never by field access. | MUST | KD-7 |
| FR-SN-033 | `SeasonViewModel` MUST expose the table + fixture list + calendar position as **read-only value copies** for #37/#38; reading MUST NOT mutate season state or affect the save digest (observer-neutral). | MUST | KD-7 |
| FR-SN-034 | Every world-tick spec #30 must tick that does not exist yet (#28/#29/#33/#41/#31/#34/#42/#45) MUST be a **documented null seam** in the KD-2 tick order, never an invented interface (FR-LW-031). The injuries seam (#41) was appended as step 4 by ERR-030-002 at #41's approval; the transfers seam (#31) was appended as step 5 by ERR-030-004 at #31's approval; the staff seam (#34) was appended as step 6 by ERR-030-006 at #34's approval (both deep-tier position reservations — empty at minimal); the academy seam (#42) was appended as step 7 by ERR-030-007 at #42's approval (a latched one-shot that goes live at #42's own T-phase); the board seam (#45) was appended as step 8 by ERR-030-008 at #45's approval (one bounded integer drift per **modelled** club, also live at #45's own T-phase). **Extended July 27, 2026 (the ten-spec approval wave) to #32/#35/#53/#54, and reconciled:** `ERR-030-007` had been filed **twice** (#42 academy *and* #32 scouting), leaving two step 7s and two step 8s — §3.3.1 records the reconciliation. #32 scouting is **step 9**; the #35 media-expiry seam is **step 10** (ERR-030-022); the #54 tenure seam is **step 11** (ERR-030-021, after board because it reads that day's confidence); `AdvanceDay` is now **step 12**. The #53 facilities seam is **step 0** (ERR-030-020) — numbered zero rather than inserted as a new 1 because it must precede its same-day consumers (steps 2/4/7) *and* the slots six approved specs cite by number must not move. | MUST | KD-2 |

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
- **`BoardState`** (value type): `Objective (BoardObjective)`, `JobSecurity`. **From #45 T2 (ERR-030-009)** `JobSecurity` is a **derived `JobSecurityBand` enum** over #45's per-mille board confidence — projected on read, never stored as independent truth. Until #45 T2 it remains #30's own scalar.
- **`MatchResult`** (readonly struct): `HomeClubId`, `AwayClubId`, `HomeGoals`, `AwayGoals`,
  `RoundIndex`, `WorldDay` — the match-outcome producer payload (KD-3).
- **`SeasonState`** (sealed class): `Seed (ulong)`, `ManagedClubId (int)`, `ClubIds (int[])`,
  `Fixtures (Fixture[])`, `Table (LeagueTable)`, `Calendar (SeasonCalendar)`, `Board (BoardState)`,
  `SeasonNumber (int)` — the season sub-blob's serialized surface.
- **`SeasonLoop`** (sealed class, the composition root): owns `SeasonState`; holds references to the
  `WorldStore` and the active-or-null `MatchEngine` (the managed club's in-progress fixture); exposes
  the command API (`AdvanceToNextFixtureDay`, `AdvanceAndPlayNextRound(ISquadProvider)`,
  `RollToNextSeason`, `View`) + `Snapshot()`/`Restore()` for the season sub-blob.
- **`SeasonViewModel`** (readonly struct): read-only value copies of the table view, fixture list, and
  calendar position for #37/#38.

## 2.3 Failure modes

| ID | Trigger | Response |
|---|---|---|
| F1 | `FixtureScheduler.Generate` with `N < 2` | throw (fail-loud); no partial schedule |
| F2 | `ApplyResult` with unknown club / self-fixture / negative goals | throw; table unchanged |
| F3 | Season codec: bad format version / out-of-bounds length prefix / trailing bytes | throw from `Decode`; no partial restore (the `MatchSaveCodec` posture) |
| F4 | Restore with "next fixture day < current WorldClock day" (KD-4 invariant violated) | throw; corrupt/inconsistent save rejected |
| F5 | `AdvanceAndPlayNextRound` when the cursor is past the last round (season already complete) | throw / documented no-op per §3 — the caller must run the boundary roll first |
| F6 | `AdvanceAndPlayNextRound` with an `ISquadProvider.ResolveByClubId` that cannot resolve the managed fixture's `ClubId` (or a non-managed fixture, when the minimal identity full-sims every fixture) | throw from the config/resolve path (the #27 `ISquadProvider` fail-loud contract) |

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-22 | — | Initial FR set FR-SN-001..034, data structures, failure modes F1–F6, from supplement v0.2. |
| 0.2 | 2026-07-22 | — | Section-file PASS-1: whole-round resolution (KD-9 / FR-SN-012/013a/013b / §3.4 / ManagedClubId), API-name corrections (`RunTick`→`MatchEnded`, `ResolveByClubId`), `uint` world-day, KD-collision + label reconciliation. See section-9 §9.3. |
| 0.3 | 2026-07-23 | — | Back-prop ERR-030-002 (at #41 approval): FR-SN-034 tick-order null-seam enumeration extended to include Injuries #41 (appended as step 4). |
| 0.4 | 2026-07-23 | — | Back-prop ERR-030-003 (at #40 approval): FR-SN-031 now enumerates two insertion points — (a') #43 promo/rel and (b') #40 finance settlement (after (a')). |
| 0.5 | 2026-07-23 | — | Back-prop ERR-030-004 (at #31 approval): FR-SN-034 tick-order null-seam enumeration extended to include Transfers #31 (appended as step 5, a deep-tier position reservation). |
| 0.6 | 2026-07-23 | — | Back-prop ERR-030-006 (at #34 approval): FR-SN-034 tick-order null-seam enumeration extended to include Staff #34 (appended as step 6, a deep-tier position reservation; `AdvanceDay` → step 7). |
| 0.7 | 2026-07-24 | — | Back-prop ERR-030-007 (at #42 approval): FR-SN-034 tick-order null-seam enumeration extended to include Youth Academy #42 (appended as step 7; `AdvanceDay` → step 8). |
| 0.9 | 2026-07-27 | — | Back-props landed atomically with the ten-spec approval wave: **ERR-030-020** (#53 facilities seam, numbered **step 0** — see §3.3.1 for why it is 0 and not a new 1), **ERR-030-021** (#54 tenure seam at step 11 + `ManagedClubId` becomes an explicit OPTIONAL so an unemployed career is representable, ◑ bump at T2, to be combined with ERR-030-009's), **ERR-030-022** (tick-order reconciliation — `ERR-030-007` had been filed twice, leaving two step 7s and two step 8s; #32 scouting → 9, #35 media expiry → 10, `AdvanceDay` → 12), **ERR-030-016** (the resolve→filter→configure seam admits more than one consumer; the current pair composes order-independently **because both are removals**, stated as a property to preserve). FR-SN-013b + FR-SN-034 amended. |
| 0.8 | 2026-07-25 | — | Back-props ERR-030-008 + ERR-030-009 (at #45 approval): FR-SN-034 enumeration + `AdvanceDay` → step 9 for the new board seam (step 8); FR-SN-014 and the §2.2 `BoardState` entry amended so that from #45 T2 `JobSecurity` is a **derived band** over #45's confidence rather than independent state — #30 keeps the objective and its evaluation; only the job-security half becomes a projection. |
#endregion
