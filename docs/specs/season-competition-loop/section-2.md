# Season & Competition Loop Specification #30 — Section 2: Functional Requirements, Data Structures, Failure Modes

**Created:** July 22, 2026
**Last Updated:** August 9, 2026 (v1.3 — ERR-028-014: F8 grows a fourth persisted per-player cursor (#28's progression `LastAdvancedWorldDay`) and states the #28 exception to the sentinel exemption its #29/#41 siblings carry — the sweep-stopped-at-a-grep-boundary class, corrected here in the same pass as `appendices.md` Appendix B.1)
**Last Updated (prior):** August 8, 2026, later still (v1.2 — ERR-030-030: FR-SN-034 stops mandating #28 as a null seam — #28 T2a made slot 1 LIVE, same as #29/#41's slots 2/4)
**Last Updated (prior):** August 8, 2026, later same day (v1.1 — balance-pass AR pass 12 M1+M2+L5: FR-SN-034 stops mandating #29/#41 as null seams, §2.2 learns the career/appearance types exist, FR-SN-032/F5 cover AdvanceDays and the roll's unplayed-fixture refusal)
**Last Updated (prior):** August 8, 2026 (v1.0 — balance-pass AR pass 11 M1+M2: FR-SN-013's availability seam corrected to LIVE (the §3.4 v1.4 correction had stopped one section short of the FR an implementer reads first), FR-SN-021's signature refreshed three landings forward, + F7/F8 — the composition-pairing and cursor-vs-clock refusals five landings of code had enforced with no normative source)
**Last Updated (prior):** July 27, 2026 (v0.9 — back-props ERR-030-016 / -020 / -021 / -022 landed atomically with the ten-spec approval wave: the tick order reconciled after the duplicate `ERR-030-007` filing, FR-SN-013b's `ManagedClubId` made an explicit optional, FR-SN-034 extended to #32/#35/#53/#54)
**Last Updated (prior):** July 25, 2026 (v0.8 — back-props ERR-030-008 board tick-order + ERR-030-009 JobSecurity derived band; prior v0.7 ERR-030-007 academy, v0.6 ERR-030-006 staff, v0.5 ERR-030-004, v0.4 ERR-030-003, v0.3 ERR-030-002, v0.2 PASS-1)
**Last Updated (prior):** July 24, 2026 (v0.8 — back-prop ERR-030-009 #44 availability-filter null seam in FR-SN-013; prior v0.7 ERR-030-007, v0.6 ERR-030-006, v0.5 ERR-030-004, v0.4 ERR-030-003, v0.3 ERR-030-002, v0.2 PASS-1)
**Version:** 1.3
**Status:** APPROVED
**Source:** `docs/tracking/season-competition-loop-design.md` v0.2

---

## 2.1 Functional requirements

FR prefix **FR-SN**. MUST/SHOULD per RFC-2119. The requirements were authored as a forward design
(the #21–#26 posture); the implementation has since landed — T0–T3, the #29/#41 T2 wiring and the
balance pass all live in `src/season-save/` (currency corrected at the lint sweep, August 8, 2026 —
the outline's pass-13 L4 class, in the FR preamble).

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
| FR-SN-013 | Each fixture in the round MUST be resolved to a `MatchResult` (scoreline + per-club goals), `ApplyResult`-ed to the table, and emit the FR-SN-016 match-outcome event — the managed club's fixture (`SeasonState.ManagedClubId`) through the real `MatchEngine` (squads via `ISquadProvider.ResolveByClubId` → `ConfigureSquads`), the others through the round-resolution model (FR-SN-013a). **Availability-filter seam (ERR-030-009, at #44's approval; LIVE since #29/#41 T2 — corrected at the balance-pass AR pass 11, M1, after §3.4's v1.4 correction stopped one section short of this FR):** the resolved squad MUST be filtered through the composed availability view (a value-copy reduction) **between resolve and configure**, on **both** clubs of **every** fixture on **both** resolution paths — a club missing four first-choice players must be rated as such whether or not a human is watching (§3.4). The seam is currently occupied by #41's FR-MD-023 medical-availability view; #44 suspensions and #36 call-ups join it at their own T-phases, composing order-independently because every consumer is a removal (ERR-030-016 — a property to preserve). The flow is resolve → *filter* → configure. | MUST | KD-9 |
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
| FR-SN-021 | `SeasonSaveManager.Save`/`Load` MUST carry the season alongside the world and optional match; capture MUST complete before the file is opened (the blob-before-file precedent). *(Signature as amended through #29/#41 T1–T2 and the balance pass — refreshed at AR pass 11 after three landings had left the original `Save(world, season, matchOrNull, path)` form here while Appendix B moved:)* `Save(world, season, matchOrNull, path, trainingClubs, medicalClubs, appearanceClubs)` — the three career-block sets REQUIRED and null-rejecting (T1 AR: an omitted set must not compile into an empty save) — plus the `Save(loop, matchOrNull, path)` overload for external callers; `Load(path, …) → SeasonSaveContents` (world, season, the three career-block sets, optional match). | MUST | KD-1 |
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
| FR-SN-032 | `SeasonLoop` MUST be the sole writer of season state; season state MUST be mutable only through the public command API (`AdvanceToNextFixtureDay`, `AdvanceDays` — the bounded free-advance, refused past the season's last fixture day and past the next season's opening day, KD-4 — `AdvanceAndPlayNextRound`, the boundary roll), never by field access. | MUST | KD-7 |
| FR-SN-033 | `SeasonViewModel` MUST expose the table + fixture list + calendar position as **read-only value copies** for #37/#38; reading MUST NOT mutate season state or affect the save digest (observer-neutral). | MUST | KD-7 |
| FR-SN-034 | Every world-tick spec #30 must tick **whose own T-phase has not yet landed** (#33/#31/#34/#42/#45/#32/#35/#53/#54 today) MUST be a **documented null seam** in the KD-2 tick order, never an invented interface (FR-LW-031). **#29 and #41 are no longer in that set** *(amended at the balance-pass AR pass 12, M1 — this row had continued to mandate them as null seams after #29/#41 T2 made slots 2 and 4 LIVE, contradicting §3.3's slot list)*: `SeasonLoop.RunCareerDaySteps` drives `AdvanceTrainingDay` at slot 2 and `AdvanceMedicalDay` at slot 4 (§3.3.2). **#28 is no longer in that set either** *(amended August 8, 2026 by ERR-030-030 — #28 T2a made slot 1 LIVE the same day)*: `RunCareerDaySteps` drives `ProgressionEngine.AdvanceDay` at slot 1, ahead of #29's slot 2, and the null-seam MUST applies only to the specs still unlanded. The injuries seam (#41) was appended as step 4 by ERR-030-002 at #41's approval; the transfers seam (#31) was appended as step 5 by ERR-030-004 at #31's approval; the staff seam (#34) was appended as step 6 by ERR-030-006 at #34's approval (both deep-tier position reservations — empty at minimal); the academy seam (#42) was appended as step 7 by ERR-030-007 at #42's approval (a latched one-shot that goes live at #42's own T-phase); the board seam (#45) was appended as step 8 by ERR-030-008 at #45's approval (one bounded integer drift per **modelled** club, also live at #45's own T-phase). **Extended July 27, 2026 (the ten-spec approval wave) to #32/#35/#53/#54, and reconciled:** `ERR-030-007` had been filed **twice** (#42 academy *and* #32 scouting), leaving two step 7s and two step 8s — §3.3.1 records the reconciliation. #32 scouting is **step 9**; the #35 media-expiry seam is **step 10** (ERR-030-022); the #54 tenure seam is **step 11** (ERR-030-021, after board because it reads that day's confidence); `AdvanceDay` is now **step 12**. The #53 facilities seam is **step 0** (ERR-030-020) — numbered zero rather than inserted as a new 1 because it must precede its same-day consumers (steps 2/4/7) *and* the slots six approved specs cite by number must not move. | MUST | KD-2 |

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
  `WorldStore` and the active-or-null `MatchEngine` (the managed club's in-progress fixture); since
  #29/#41 T2 also holds the optional **career PAIR** — `PlayerCareerStates` + the `ISquadProvider` it
  was bound to (half-supplying or later swapping the provider is refused, F7); exposes
  the command API (`AdvanceToNextFixtureDay`, `AdvanceDays`, `AdvanceAndPlayNextRound(ISquadProvider)`,
  `RollToNextSeason`, `View`) + `Snapshot()`/`Restore()` for the season sub-blob.
- **`PlayerCareerStates`** (sealed class, #30-owned since #29/#41 T2): the three parallel per-club
  career state sets — #29 training, #41 medical, #30 appearance — keyed `(ClubId, PlayerId)` with
  globally-unique player ids (ERR-041-019), the single place #30 calls either sibling subsystem from,
  and the owner of the shared per-cursor clock predicates (F8). *(Added at AR pass 12 M2 — this
  section declared no career and no appearance state three landings after both became load-bearing,
  which is how the APPR layout came to ship unspecified, ERR-030-028.)*
- **`AppearanceState`** (struct, per player): `RecentBits (u32)` — the lazily-shifted appearance
  day-bitmask, shifted at READ time — and `BitsAsOfWorldDay (u32)` — the day the bits are anchored to
  (never ahead of the clock, F8). Byte layout pinned in Appendix B.1 (ERR-030-028).
- **`ClubAppearanceStates`** (sealed class): one club's `PlayerIds`/`AppearanceState[]` pair — the
  appearance third of the career triple, serialized as the `APPR` sub-blob.
- **`SeasonViewModel`** (readonly struct): read-only value copies of the table view, fixture list, and
  calendar position for #37/#38.

## 2.3 Failure modes

| ID | Trigger | Response |
|---|---|---|
| F1 | `FixtureScheduler.Generate` with `N < 2` | throw (fail-loud); no partial schedule |
| F2 | `ApplyResult` with unknown club / self-fixture / negative goals | throw; table unchanged |
| F3 | Season codec: bad format version / out-of-bounds length prefix / trailing bytes | throw from `Decode`; no partial restore (the `MatchSaveCodec` posture) |
| F4 | Restore with "next fixture day < current WorldClock day" (KD-4 invariant violated) | throw; corrupt/inconsistent save rejected |
| F5 | `AdvanceAndPlayNextRound` when the cursor is past the last round (season already complete); or the boundary roll (`RollToNextSeason`) invoked while any fixture of a resolved round was never played | throw / documented no-op per §3 — the caller must run the boundary roll first; the roll itself refuses an incomplete season (`RequireEveryFixturePlayed`, F5's second half) |
| F6 | `AdvanceAndPlayNextRound` with an `ISquadProvider.ResolveByClubId` that cannot resolve the managed fixture's `ClubId` (or a non-managed fixture, when the minimal identity full-sims every fixture) | throw from the config/resolve path (the #27 `ISquadProvider` fail-loud contract) |
| F7 | A career paired wrong at composition: `SeasonLoop` constructed with a career but no provider (or vice versa — two providers would train one league and play another), with a career that does not cover the season's clubs, or `AdvanceAndPlayNextRound` invoked with a DIFFERENT provider than the pair bound at construction | throw at composition / at the call (`ArgumentException`) — every symptom of a mispair downstream is a plausible table rather than a crash (the #29/#41 T2 rule) |
| F8 | Any persisted per-player career cursor outside the coherent band relative to the world clock — the **four** persisted per-player cursors are #29/#41's `LastAdvancedWorldDay`, #28's progression `LastAdvancedWorldDay` (ERR-028-007 — the fourth cursor, added at #28 T1/T2a and enforced on the same terms as its siblings; `src/season-save/SeasonSaveManager.cs`'s own comment labels it exactly that), and the appearance anchor's `BitsAsOfWorldDay`. #29/#41/#28 cursors AHEAD of the clock or LAGGING it by ≥ 2 (ahead = the F6-idempotency silently skips the day step; lag ≥ 2 = the sibling specs' F7 gap refusal fires on every later advance and, the day steps running before the clock increment, can never close — the career wedges permanently while saving cleanly; **#28's lag case is worse still**, because `ProgressionEngine.AdvanceDay` REPLAYS a gap rather than banking one day, so a mispaired file would bank N days of growth in one call from a single day's inputs, invisibly); the appearance anchor AHEAD only (it has no gap contract). **#28 is the one exception to the sentinel exemption its siblings carry (ERR-028-014):** #29/#41's fresh state is exempt from this band check at their own never-advanced sentinel (a fresh state with no clock-anchored quantity is coherent at any clock), but the sentinel is not a legal #28 store state at all — `ProgressionEngine.SeedFrom` anchors the cursor at the seed day, never the sentinel, and `FromBlocks` refuses a lifecycle carrying it — so #28's cursor is checked against the coherent band unconditionally, with no exempted value. | throw at Save, at Load AND at composition (`InvalidOperationException`) — the save root and `SeasonLoop`'s constructor share one predicate set (`PlayerCareerStates`' per-cursor owners), so the two gates cannot drift |
| F9 | The composed availability filters leave a club unable to field the formation even with EVERY squad member pressed back in (ERR-030-029 — the §3.4 depleted-squad rule's terminal case) | throw (`InvalidOperationException`) at selection — a roster-integrity bug, not a football outcome; the back-fill itself (least-injured first, selector-probed) is the defined non-throwing path |

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
| 0.8 | 2026-07-25 | — | Back-props ERR-030-008 + ERR-030-009 (at #45 approval): FR-SN-034 enumeration + `AdvanceDay` → step 9 for the new board seam (step 8); FR-SN-014 and the §2.2 `BoardState` entry amended so that from #45 T2 `JobSecurity` is a **derived band** over #45's confidence rather than independent state — #30 keeps the objective and its evaluation; only the job-security half becomes a projection. |
| 0.9 | 2026-07-27 | — | Back-props landed atomically with the ten-spec approval wave: **ERR-030-020** (#53 facilities seam, numbered **step 0** — see §3.3.1 for why it is 0 and not a new 1), **ERR-030-021** (#54 tenure seam at step 11 + `ManagedClubId` becomes an explicit OPTIONAL so an unemployed career is representable, ◑ bump at T2, to be combined with ERR-030-009's), **ERR-030-022** (tick-order reconciliation — `ERR-030-007` had been filed twice, leaving two step 7s and two step 8s; #32 scouting → 9, #35 media expiry → 10, `AdvanceDay` → 12), **ERR-030-016** (the resolve→filter→configure seam admits more than one consumer; the current pair composes order-independently **because both are removals**, stated as a property to preserve). FR-SN-013b + FR-SN-034 amended. |
| 1.0 | 2026-08-08 | — | **Balance-pass AR pass 11 (M1 + M2)**: FR-SN-013's ERR-030-009 clause still read "MAY be filtered … empty until #44 T2" while the seam has been LIVE, unconditional, both-clubs-both-paths since #29/#41 T2 — §3.4 was corrected at AR pass 5 and the sweep stopped there, leaving the requirements section an implementer reads first contradicting the section that describes the loop (three false statements in one MUST). FR-SN-021 refreshed (the four-argument signature was three landings and three parameters stale; Appendix B had moved at v0.5/v0.6, §2 had not). **F7/F8 added**: the composition-pairing refusals and the cross-blob cursor-vs-clock rule — enforced at three boundaries in two directions over three cursor kinds since the T2/pass-5/pass-6 landings — had ONE appendix sentence (one cursor kind, one direction, one boundary) as their entire normative source; the pass-9-L4 class ("a production fail-loud with no spec row"), six refusals wide at #30. |
| 1.1 | 2026-08-08 | — | **Balance-pass AR pass 12 (M1 + M2 + L5)** — pass 11's own fixes completed to their class: **FR-SN-034** still MANDATED #29/#41 as null seams one row below the FR-SN-013 pass 11 corrected (amended to landed-live at slots 2/4, the MUST restricted to unlanded specs); **§2.2** declared a `SeasonLoop` with no career pair and knew nothing of `PlayerCareerStates`/`AppearanceState`/`ClubAppearanceStates` three landings after all became load-bearing — the gap that let the APPR layout ship unspecified (ERR-030-028) and forced F7/F8 to cite undeclared members; **FR-SN-032/F5** gain `AdvanceDays` (a public command with three fail-loud refusals, absent from every list) and the roll's `RequireEveryFixturePlayed` half.**F9** (M4, ERR-030-029): the depleted-squad terminal refusal — see §3.4's new rule. |
| 1.2 | 2026-08-08 | — | **ERR-030-030** (found at #28 T2a implementation): **FR-SN-034** dropped #28 from the null-seam enumeration — #28's `AdvanceDay` went LIVE at slot 1 the same day, the identical stale-seam-text class corrected for #29/#41 at AR passes 11/12, recurring on the next subsystem to wire. See also §3.3, §3.5 step (d), and `appendices.md` Appendix A/B, corrected in the same commit. |
| 1.3 | 2026-08-09 | — | **ERR-028-014** (found at #28 implementation, August 8–9, 2026): #28's `ProgressionEngine.SeedFrom` now anchors `LastAdvancedWorldDay` at the seed day and `FromBlocks` refuses a lifecycle carrying the never-advanced sentinel, retiring that sentinel from #28's legal store states — but `AdvanceDay_FirstCall_AdvancesExactlyOneDay`'s reasoning survived nowhere else that mattered until this row: **F8** still enumerated only three cursor kinds (#29, #41, the appearance anchor) with no mention of #28's, and carried no statement of the exception. Corrected: F8 now names #28's progression `LastAdvancedWorldDay` as the fourth persisted per-player cursor (ERR-028-007), states its worse-case lag consequence (`AdvanceDay` replays a gap rather than banking one day), and states the #28-only exception to the sentinel exemption — #29/#41's fresh state carries no clock-anchored quantity so "never advanced" is exempt at any clock, while #28's fresh state derives age from `BirthWorldDay` so the same exemption would have meant something different at every clock value, which is why #28 has none. `appendices.md` Appendix B.1 corrected in the same commit (the identical duplicated-description class F8's own row exists to prevent — one paragraph, two homes). |
#endregion
