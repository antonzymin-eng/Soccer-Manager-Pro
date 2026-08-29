# Season & Competition Loop Specification #30 — Section 5: Test Plan

**Created:** July 22, 2026
**Last Updated:** August 16, 2026, round 5 (v0.6 — L-t1: T-SN-DET-005's "names all five within-tier-search
locks" was wrong — the suite has always had TEN cases, all scoped to the tier-2 choice. New
**T-SN-DET-006** names the five it missed (`BenchDepthExtremis` ×2, `ForcedStartExtremis`,
`MultiReinstatementExtremis` ×2); T-SN-DET-005's claim annotated in place, not rewritten. Pure
traceability — no code or suite change)
**Last Updated (prior):** August 16, 2026 (v0.5 — round-4 reviewed-findings pass over the `ERR-030-046` landing: new **T-SN-DET-005** names the ERR-030-046 within-tier search suite, `AvailabilityCompositionExtremisTests` — the id had been missing since that suite's own creation, the F8 §5-id precedent recurring)
**Last Updated (prior):** August 8, 2026 (v0.4 — balance-pass AR pass 13 L5: T-SN-DET-004 names the depleted-squad locks; the ERR-030-029 back-prop reaches #30's own test plan)
**Last Updated (prior):** July 25, 2026 (v0.3 — ERR-030-010: T-SN-FIX-001 re-anchored + new T-SN-FIX-008)
**Version:** 0.6
**Status:** APPROVED
**Source:** `docs/tracking/season-competition-loop-design.md` v0.2

---

## 5.1 Test layers

The layer runs on the world tick (§1.2 / FR-SN-025), so its tests are Unit + Determinism + (at T2+)
a Simulation-layer `#19 ScenarioRunner` capstone. No 60 Hz hot-path perf gate applies here (§6).

## 5.2 Fixture generation (FR-SN-001..004)

| ID | Test |
|---|---|
| T-SN-FIX-001 | `Generate([10,11,12,13], seed)` matches the App. C worked schedule exactly (the ERR-030-010-corrected table — rounds 1/4 venues). |
| T-SN-FIX-002 | Two-run determinism: `Generate(ids, seed)` twice ⇒ byte-identical `Fixture[]`. |
| T-SN-FIX-003 | Double round-robin completeness: every ordered pair `(a,b), a≠b` appears exactly once (`N·(N−1)` fixtures). |
| T-SN-FIX-004 | No club appears twice in any one round. |
| T-SN-FIX-005 | Odd `N` (bye rotation): every real club plays `2·(N−1)` fixtures, none against a phantom. |
| T-SN-FIX-006 | `Generate` with `N < 2` throws (F1). |
| T-SN-FIX-007 | Seed sensitivity: a distinct permutation seed yields a distinct fixture order over the same club set. |
| T-SN-FIX-008 | **Venue balance (ERR-030-010):** over a 20-club league every club's first-leg home count is within one of the 9/10 ideal, and no club takes more than 3 consecutive home fixtures. Fails under the pre-correction unparried rule (pinned club = 19). |

## 5.3 League table (FR-SN-005..008)

| ID | Test |
|---|---|
| T-SN-TAB-001 | `ApplyResult` win/draw/loss arithmetic (P/W/D/L/GF/GA/GD/Pts) exact for each outcome. |
| T-SN-TAB-002 | `GoalDifference` = GF − GA after a sequence of results (recomputed, not drifted). |
| T-SN-TAB-003 | Tie-break order Pts→GD→GF→ClubId exercised at an **exact** three-key tie (only ClubId separates). |
| T-SN-TAB-004 | `OrderedView()` is a read-only copy — the stored rows are unchanged after ordering (observer-neutral, FR-SN-033). |
| T-SN-TAB-005 | `ApplyResult` fail-loud: unknown club, self-fixture, negative goals (F2). |

## 5.4 Calendar & day-advance (FR-SN-009..013 / KD-2)

| ID | Test |
|---|---|
| T-SN-CAL-001 | `AdvanceToNextFixtureDay` advances the world exactly `(targetDay − currentDay)` times. |
| T-SN-CAL-002 | **Behaviour-neutral floor (KD-8 / FR-SN-026):** a no-fixture day's advance produces a world snapshot byte-identical to a bare `WorldStore.AdvanceDay()` on an equal starting world. |
| T-SN-CAL-003 | `AdvanceAndPlayNextRound` resolves **all** `N/2` fixtures in the round, applies every result to the table, advances the cursor one round, and emits one match-outcome event per fixture (FR-SN-012/016). |
| T-SN-CAL-003a | **Round completeness (KD-9):** after a full round, every club **that has a fixture in that round** has `Played` incremented by exactly 1 (for odd `N`, the one bye club that round does not — the N>2 broken-table regression lock, bye-aware). |
| T-SN-CAL-003b | **Managed-club routing (FR-SN-013b):** the `ManagedClubId` fixture runs through the `MatchEngine` (its result reflects the engine score); non-managed fixtures resolve through the round-resolution model. |
| T-SN-CAL-003c | **Order-independence (§3.4.1):** resolving a round's fixtures in a permuted order yields the byte-identical final table (non-managed quick-sim draws by key, not cursor). |
| T-SN-CAL-004 | KD-4 invariant: next-fixture-day ≥ current WorldClock day after each advance. |
| T-SN-CAL-005 | `AdvanceAndPlayNextRound` past the last round throws / documented no-op (F5). |
| T-SN-CAL-006 | `AdvanceAndPlayNextRound` with an unresolvable `ClubId` fails loud (F6). |

## 5.5 Save / restore round-trip (FR-SN-019..024)

| ID | Test |
|---|---|
| T-SN-SAVE-001 | `SeasonStateCodec` round-trip: encode → decode ⇒ field-identical `SeasonState`. |
| T-SN-SAVE-002 | Full-file round-trip through `SeasonSaveManager` (world + season + optional match) ⇒ table + fixtures + calendar + board byte-identical, world field-identical, match digest chain byte-identical. |
| T-SN-SAVE-003 | **No-match season** (matchPresent = 0): world + season restore; `Contents.Match == null`. |
| T-SN-SAVE-004 | Fail-loud gates: bad `SEASON_STATE_FORMAT_VERSION`, bad `SEASON_SAVE_FORMAT_VERSION`, out-of-bounds season length prefix, trailing bytes (F3). |
| T-SN-SAVE-005 | KD-4 restore invariant: a season blob with next-fixture-day < current-world-day is rejected (F4). |
| T-SN-SAVE-006 | World / match blob untouched: a pre-#30 world/match blob byte-for-byte equals the block the season frame nests (no inner-version change, FR-SN-020). |

## 5.6 Mid-sequence & end-to-end determinism (FR-SN-024 / FR-SN-030)

| ID | Test |
|---|---|
| T-SN-DET-001 | **Mid-sequence restore (KD-2):** save@day-N mid-advance → restore → advance to N+K == an uninterrupted advance (world + season byte-identical). |
| T-SN-DET-002 | **Two-run season:** the same seed + `ManagedClubId` drives a full simulated season (every round resolved via a fixed `ISquadProvider`, non-managed fixtures via the round-resolution model) to a byte-identical final table — every club's row populated (the KD-9 completeness lock at season scale). |
| T-SN-DET-003 | Season-boundary roll (KD-6): `RollToNextSeason` is two-run deterministic and restartable (save mid-roll → restore → same continuation). |
| T-SN-DET-004 | **The depleted-squad rule (ERR-030-029 / §3.4 / F9):** the composed availability filter presses the least-injured back in until the engine's own selector can field the formation (locked by `PlayerCareerStatesTests.SelectAvailable_BackfillsTheLeastInjuredRatherThanRefusingToFieldATeam`), and the terminal case — even the whole squad cannot field it — fails loud (`…WhenTheRosterItselfCannotFieldATeam_FailsLoud`). Ids assigned at AR pass 13 (L5): the locks predate their row — the F8 §5-id precedent (pass 10 L4), two passes late for #30's own F9. |
| T-SN-DET-005 | **The ERR-030-046 within-tier search (§3.4, tier 2's suspended reinstatement choice):** locked by `AvailabilityCompositionExtremisTests` — `WeakPositionExtremis` (the per-position mutant-killer: a global rating key cannot see that the squad has no room for the globally weakest banned player at his position), `MinimalStallExtremis` (the minimisation guarantee — dirty count exactly the forced minimum, driven through a really played round so the forced starter's ban stalls while the benched one's advances), `CapFallbackExtremis` (the beyond-cap residual — termination, fieldability and repeat-call determinism only), and the two round-4 mutant-killers `InCapBoundaryExtremis` (`m == EXTREMIS_SEARCH_CANDIDATE_CAP` exactly) and `TiedForcedStartExtremis` (two candidates tied at the same positive dirty count, the canonical-first preference). Ids assigned at the round-4 pass: the suite predates its row by one same-day landing, the F8/T-SN-DET-004 precedent recurring. **This row names five of the suite's ten cases — see T-SN-DET-006 for the remaining five (round-5 correction, L-t1).** |
| T-SN-DET-006 | **The remaining five `AvailabilityCompositionExtremisTests` cases (round-5, L-t1):** T-SN-DET-005 named only five of the suite's ten cases despite claiming "all five" — the suite has always had ten. The other five: `BenchDepthExtremis_ReinstatesACandidateTheSelectorBenches_NotTheEarliestOnTheRoster` and `BenchDepthExtremis_TheReinstatedSuspendedPlayer_StillServesHisBan` (ERR-030-044's within-tier key — the reinstated player is the one the selector would BENCH, not the earliest on the roster, and a benched reinstatee's ban still advances); `ForcedStartExtremis_TheOnlyGoalkeeper_IsReinstatedIntoTheXi_AndHisBanStalls` (the canonical forced-start residual — the sole banned goalkeeper); and `MultiReinstatementExtremis_PressesTheWeakestBannedPlayersBack_AndTerminatesDeterministically` + `MultiReinstatementExtremis_KeepsEveryBannedPlayerOutOfTheXi_WhenACompletingChoiceExists` (ERR-030-045's `k >= 2` case — the weakest banned players pressed back first, and a clean completing choice kept entirely out of the XI). |

## 5.7 Capstone scenario (T2+, `#19 ScenarioRunner`)

`season-multi-fixture` (owning specs `{16,19,22,27,30}` + the match-engine spec set it drives, Tier
B; path under `SCENARIO_PATH_CROSS_SPEC_PREFIX`): boot a `SeasonLoop`, play K fixtures across a
schedule (advancing the world between them), and assert (a) the table reflects the played results,
(b) a two-run same-seed digest match over the full season state, and (c) the KD-8 floor holds for
every no-fixture day. The match-engine capstone (`match-engine-kickoff-multi-second`) precedent. Not
required at the design stage; T2 has since wired the loop, so this is now simply the outstanding §5 addition (noted at the lint sweep).

## 5.8 FR traceability

Every FR-SN-001..034 + 013a/013b maps to at least one test above (fixture: 001–007; table: TAB-001..005; calendar
/ flow / producer: CAL-001..006; save: SAVE-001..006; determinism / continuity / neutrality:
DET-001..003 + CAL-002; view-model / command discipline: TAB-004 + CAL-003). The Wave-2+ null-seam
FR (FR-SN-034) is verified structurally (the KD-2 order has documented empty slots, no interface),
not by an execution test — nothing ticks there yet.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-22 | — | Initial test plan: fixture / table / calendar / save / determinism / capstone + FR traceability. |
| 0.2 | 2026-07-22 | — | Section-file PASS-1: whole-round resolution (KD-9 / FR-SN-012/013a/013b / §3.4 / ManagedClubId), API-name corrections (`RunTick`→`MatchEnded`, `ResolveByClubId`), `uint` world-day, KD-collision + label reconciliation. See section-9 §9.3. |
| 0.3 | 2026-07-25 | — | **ERR-030-010**: T-SN-FIX-001 re-anchored to the corrected Appendix C table; new **T-SN-FIX-008** venue-balance regression lock (fails under the pre-correction rule). |
| 0.4 | 2026-08-08 | — | **Balance-pass AR pass 13 (L5) + the lint sweep**: *(lint: §5.7's "once T2 wires the loop" tensed — T2 wired it.)* ERR-030-029's back-prop had reached #36's test plan and not #30's own — **T-SN-DET-004** names the two existing depleted-squad locks (back-fill + terminal refusal). |
| 0.5 | 2026-08-16 | — | **Round-4 reviewed-findings pass over the `ERR-030-046` landing (Low)**: no test-plan id traced `AvailabilityCompositionExtremisTests` at all, the ERR-030-029/T-SN-DET-004 gap recurring one ERR later. New **T-SN-DET-005** names all five within-tier-search locks — `WeakPositionExtremis`, `MinimalStallExtremis`, `CapFallbackExtremis`, and the round-4 additions `InCapBoundaryExtremis` + `TiedForcedStartExtremis`. **ANNOTATED 2026-08-16, round 5 (L-t1) — the "all five" claim is WRONG:** the suite has always had TEN cases, all scoped to the tier-2 choice, and this row named only five of them — `BenchDepthExtremis` ×2, `ForcedStartExtremis`, and `MultiReinstatementExtremis` ×2 were left untraced. Named at the new **T-SN-DET-006** (v0.6). |
| 0.6 | 2026-08-16, round 5 | — | **L-t1**: new **T-SN-DET-006** names the five `AvailabilityCompositionExtremisTests` cases v0.5's T-SN-DET-005 missed — `BenchDepthExtremis_ReinstatesACandidateTheSelectorBenches_NotTheEarliestOnTheRoster`, `BenchDepthExtremis_TheReinstatedSuspendedPlayer_StillServesHisBan`, `ForcedStartExtremis_TheOnlyGoalkeeper_IsReinstatedIntoTheXi_AndHisBanStalls`, `MultiReinstatementExtremis_PressesTheWeakestBannedPlayersBack_AndTerminatesDeterministically`, `MultiReinstatementExtremis_KeepsEveryBannedPlayerOutOfTheXi_WhenACompletingChoiceExists`. T-SN-DET-005's "all five" claim annotated in place, not rewritten. No code or suite change — both rows are pure traceability. |
#endregion
