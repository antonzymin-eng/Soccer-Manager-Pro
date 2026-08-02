# File Manifest (Post-Migration Baseline)

**Created:** April 30, 2026  
**Last Updated:** August 2, 2026 (**ERR-020-002 + ERR-020-003 filed — assembly layer taxonomy back-prop.**
No new files and no file removed; this entry records tracking-document version movement only.
**Modified:** `docs/tracking/spec-error-log.md` → **v1.54** (two OPEN entries + two Error Index rows);
`docs/tracking/open-issues.md` (one new active entry, 17 → **18 active** / 33 resolved);
`docs/tracking/CHANGELOG.md` (new head entry); `CLAUDE.md` (OPEN ISSUES index line + count);
`src/CLAUDE.md` (the ⚠️ taxonomy staleness note now cites `ERR-020-002`, distinguishes the verbatim
#20 §3.5.2 layer table from the `src/CLAUDE.md` infrastructure-table extension that carries the
`code-standards` phantom, and records `ERR-020-003`'s arrow-notation conflict with the binding reading).
**No code, no spec text, no assembly change** — `docs/specs/code-standards/section-3.md` is deliberately
untouched pending owner sign-off on the proposed ten-tier order. Prior entry below.)

**Last Updated (prior):** July 27, 2026 (**Path-to-playable Track C: B1 richer observation frame + B2 Match Analytics
#37 T0 LANDED, then an adversarial-review fix pass over both (0H + 6M + 3L, all fixed).** **New assembly:**
`src/match-analytics/` (`TacticalDirector.MatchAnalytics`) — `match-analytics.asmdef`,
`MatchAnalyticsConstants.cs`, `XgLocationModel.cs`, `StatPoint.cs`, `MatchStatline.cs`,
`AdvancedStatline.cs`, `MatchAnalyticsResult.cs` + `Tests/` (asmdef, `XgLocationModelTests.cs`,
`MatchAnalyticsValueTypeTests.cs`). Nothing is engine-wired: the T1 ledger tap is roadmap B3, and the xG
model has no live consumer because the ledger carries no shot origin (**ERR-037-001** filed rather than
worked around). **New engine/viewer files:** `src/match-engine/MatchPeriod.cs` + `RestartCue.cs`
(deliberately NOT a widening of Ball Physics' ordinal-stable `RestartType`), `src/match-viewer/
LiveAgentCue.cs` + `Scoreline.cs` + `RestartBanner.cs`. **Modified:** `src/match-engine/MatchEngine.cs`
v1.50 (discipline / period / restart accessors; `ApplyRestart` declares its cue at all five call sites; the
two restart fields are WITHIN-tick, reset beside `_aiPhaseRanThisTick`, so there is **no
`SNAPSHOT_SCHEMA_VERSION` change** and the exclusion proof needs no new class; three inline teamId guards
collapsed into `GuardTeamId`); `LiveMatchFrame.cs` v1.2 / `LiveMatchStreamer.cs` v1.5 /
`LiveMatchServer.cs` v1.2 / `ui-framework/MatchFrameView.cs` v1.2. **The AR's structural finding (M-6):**
the frame constructor had reached 13 positional parameters with four adjacent `int`s, so a transposed
call site would compile silently — the score and restart triples collapse into `Scoreline` and
`RestartBanner`, leaving **no two parameters sharing a type**. That also closed M-3 structurally: because
the banner DERIVES its team and tick from its own cue, `default(RestartBanner)` reports the no-restart
sentinel instead of "home team, tick 0" (the zero-value trap, one layer up from where `MatchFrameView`
had already been bitten by it once). Other findings: the statlines gained an unset-vs-zero discriminator
(M-1); the observer-neutrality run went 400 → 6000 ticks **and now asserts a restart was actually
observed**, since at 400 ticks it proved neutrality only for the accessors that never fired (M-2);
`TEAM_COUNT` became a `[CROSS]` mirror (M-4); the xG team gate reached all three public entry points, not
just `Evaluate` (M-5); and a tautological `COLS * ROWS` assertion the compiler folds away was re-anchored
to a literal (L-1). **Full dotnet gate: PASSED, 0 failures, quarantine empty** (match-analytics 24,
match-viewer 39, ui-framework 50, match-engine 358 + 3 env-gated skips). **Prior:** July 26, 2026
(**Season & Competition Loop #30 T2 LANDED** — path-to-playable roadmap item
**Last Updated:** July 27, 2026, later same day (**Documentation sync pass — no code, no spec change.**
Two gaps found and fixed: (1) the "Current Specification Folders" table below had not been updated since
July 8, 2026 and was stuck at 26 rows / "All 26 spec folders now exist," predating the entire #27–#54
management-layer promotion wave (27 more spec folders now exist, all APPROVED per `SPEC_INDEX.md`) —
rows added at folder+status granularity, pointing to `SPEC_INDEX.md` for full per-spec detail rather
than duplicating it. (2) Two same-day code landings — **Match Analytics #37 T0** (`src/match-analytics/`,
value types + `XgLocationModel`, ERR-037-001 resolved) and **Track C B1** (the interactive-Unity-client
richer observation frame extending `LiveMatchFrame`/`MatchFrameView`, no `SNAPSHOT_SCHEMA_VERSION`
change) — had landed without a corresponding manifest entry; both are recorded in
`path-to-playable-roadmap.md` (items B1/B2, marked LANDED) and this pass folds them in here too. Root
`CLAUDE.md` and `README.md` were reconciled in the same pass (assembly count 29 → 30; "APPROVED with no
assembly" count 23 → 22). Prior entry below.)
**Last Updated (prior):** July 27, 2026 (**Season & Competition Loop #30 T3 LANDED** — path-to-playable roadmap
item **A5**, the season-boundary roll; with it Phase A is complete and **PM-2-sim is reached**.
**New files:** `src/season-save/SeasonRollOutcome.cs` (the boundary-roll producer record — board verdict,
job security before/after, what the next season starts from; session-scoped, deliberately not serialized
per the ERR-030-013 posture); `src/season-save/tests/SeasonRollTests.cs` (18 tests). **Modified:**
`src/season-save/SeasonLoop.cs` v1.1 (`RollToNextSeason` + the pure `EvaluateJobSecurity` /
`ShiftCalendarToNextSeason` / `DeriveNextSeasonSeed` helpers); `src/season-save/SeasonLoopConstants.cs` v1.3
(`[FIXED] SEASON_ROLL_SEED_DOMAIN`; `[GT] SeasonBreakDays` + the two board job-security deltas; the
`PositiveDayValue` read guard); `src/season-save/RoundResolutionModel.cs` (`Mix` private → internal, so the
seed derivation reuses one finalizer instead of carrying a second copy in the same assembly);
`docs/specs/season-competition-loop/section-3.md` **v1.0** (ERR-030-015 — §3.5 gains step (c′), and the two
stale `Version` header fields are consolidated); `docs/tracking/spec-error-log.md` v1.46;
`docs/tracking/path-to-playable-roadmap.md` v0.8. **Full dotnet gate: PASSED, 0 failures (whole tree green;
season-save 240 → 258 tests — 255 passed + the 3 env-gated calibration/diagnostic drivers skipped).**
The landing filed **ERR-030-015**: §3.5's `RollToNextSeason` pseudocode regenerated `Fixtures` but never
rebuilt `Calendar`, whose cursor sits at `RoundCount` because the season just ended — so a season rolled
from the spec as written is *permanently unplayable*, and no assertion over the rolled state's fields would
have noticed. Caught by the acceptance test playing a **second** season to completion; 9 of the suite's 18
predicates fail against the pre-fix form. No `SEASON_STATE_FORMAT_VERSION` change (the calendar was already
serialized). Prior entry below.)

**Last Updated (prior):** July 26, 2026 (**Season & Competition Loop #30 T2 LANDED** — path-to-playable roadmap item
A4, the day-advance loop + the round-resolution model. **New files:** `src/season-save/RoundResolutionMode.cs`,
`RoundResolutionModel.cs`, `SeasonLoop.cs`; `src/match-engine/SquadRating.cs` (the public XI-mean rating seam
over the internal `LineupSelector` — league-bootstrap AR-4 M-1's named A4 prerequisite);
`src/season-save/tests/SeasonLoopTests.cs`, `RoundResolutionModelTests.cs`, `SeasonLoopScenarios.cs`,
`SeasonLoopScenarioTests.cs`, `RoundResolutionCalibrationHarness.cs`,
`RoundResolutionCalibrationHarnessTests.cs`, `EngineScoringDiagnosticTests.cs`; `tools/round-resolution-fit.py`;
`docs/tracking/round-resolution-corpus.md` (the A4a evidence record). **Modified:**
`src/deterministic-sim/DeterministicSimConstants.cs` v1.5 (`DOMAIN_TAG_SEASON_LOOP = 0x22` at its first draw
site, ERR-030-001); `src/season-save/SeasonLoopConstants.cs` v1.2 (the `[CROSS]` tag mirror, the `[FIXED]`
sub-stream / match-seed domains + `MAX_GOALS_PER_SIDE`, and the five `[GT]` round-resolution rows);
`src/match-engine/LineupSelector.cs` v1.1; `src/season-save/tests/season-save-tests.asmdef`
(+ `TacticalDirector.TestingStrategy`); `docs/specs/season-competition-loop/section-4.md` v0.3;
`docs/tracking/spec-error-log.md` v1.44; `docs/tracking/match-engine-design.md` v2.1 (new §5.Z Phase H);
`docs/tracking/path-to-playable-roadmap.md` v0.6. **Full dotnet gate: PASSED, 0 failures (whole tree green;
season-save 179 → 240 tests (237 passed + 3 env-gated drivers skipped), incl. the capstone scenario).** The landing filed **ERR-030-012 / ERR-030-013** (two §4
architecture sketches another section of the same spec forbids) and — by running A4a's KD-8 Step 0 pilot —
**ERR-030-014**: a production match never puts the ball in motion, so every engine match is 0–0. A4a is
blocked upstream; new roadmap item A4b owns the fix. Prior entry below.)

**Last Updated (prior):** July 25, 2026, latest same day (**League bootstrap LANDED** — path-to-playable roadmap
item A3, the #47-minimal substitute (roadmap C3). **New files:** `src/season-save/LeagueBootstrapConstants.cs`,
`ClubNameCatalogue.cs`, `Club.cs`, `League.cs`, `LeagueBootstrap.cs` + `tests/LeagueBootstrapTests.cs` (and
their `.meta` files); new design supplement `docs/tracking/league-bootstrap-design.md` v1.1. **Modified:**
`src/season-save/season-save.asmdef` (+ `TacticalDirector.PlayerDatabase`), `src/player-database/`
`RosterGenerator.cs` v1.4 (an additive supplied-position `Generate` overload — the drawn-position path stays
byte-identical) + `PlayerDatabaseConstants.cs` (`POSITION_COUNT` hoisted so two assemblies stop carrying
private copies) + `tests/RosterGeneratorTests.cs` / `tests/PlayerAttributesTests.cs`, plus the season-save
manifest section below (which had never listed the #30 T0 value types). **Full dotnet gate: PASSED, 0
failures (whole tree green; season-save 141 → 177, player-database 42 → 46, living-world 119).** A follow-up
hostile whole-file review (AR-5, 1H+4M+3L) added the golden vector, a read-only `WorldStore.WorldSeed`
accessor (`src/living-world/WorldStore.cs` v1.7) so a saved career can rebuild its `ISquadProvider`, and
read-only wrappers on the two catalogue arrays.)
**Last Updated (prior):** July 25, 2026 (**Season & Competition Loop #30 T1 LANDED** — path-to-playable
roadmap item A2, the season save/restore path. **New files:** `src/season-save/SeasonStateCodec.cs` (the #30
Appendix B season sub-blob codec) + `src/season-save/tests/SeasonStateCodecTests.cs` (and their `.meta` files).
**Modified:** `SeasonSaveCodec.cs` v1.1 + `SeasonSaveBlobs.cs` v1.1 (the frame gains a THIRD opaque sub-blob
between the world and match blocks), `SeasonSaveConstants.cs` v1.1 (**`SEASON_SAVE_FORMAT_VERSION` 1 → 2**,
FR-SN-020 — the world and match blobs stay byte-untouched), `SeasonSaveManager.cs` v1.2 +
`SeasonSaveContents.cs` v1.1 (`Save(world, season, matchOrNull, path)` / `Load → { World, Season, Match }`,
FR-SN-021), `SeasonState.cs` v1.3 (code self-AR: the ctor now requires a calendar mapping ≥ 1 round — an empty
schedule with a `default(SeasonCalendar)` was constructible but not decodable, an FR-SN-022 round-trip
asymmetry), `tests/SeasonSaveManagerTests.cs` v1.3. Spec: `season-competition-loop/section-3.md` §3.6 +
`appendices.md` Appendix B row 11 (**ERR-030-011** — §3.6's `EncodeSeason` omitted `ManagedClubId` which
Appendix B row 3a requires; row 11's `f32/u8` job security pinned to `i32` per-mille), `spec-error-log.md` →
v1.42, `path-to-playable-roadmap.md` → v0.4, `src/CLAUDE.md` → v2.37. Full dotnet gate PASSED, 0 failures
(whole tree green; season-save 112 → 135 tests).)
**Last Updated (prior):** July 25, 2026, latest same day (**#30 T0 adversarial-review fix pass — 1H+1M+6L, all fixed;
re-review pass 2 clean.** **Modified:** `src/season-save/SeasonState.cs` (H-1 — `Table` public→`internal`
+ public read-only projections `TableOrdered`/`TableRowsInClubIdOrder`/`TableRow`/`PositionOf` + internal
`ApplyResult(in MatchResult)` + ctor `table.Clone()`; the KD-7/FR-SN-032 single-writer contract was
unenforced for the table), `src/season-save/LeagueTableRow.cs` (M-1 — `Create` gains the F3 fail-loud gate
for #30 T1's decode path: non-negative counts + `won+drawn+lost == played`), `tests/SeasonStateTests.cs` +
`tests/LeagueTableTests.cs` (call sites routed through the new seams; +9 tests — ctor table snapshot-copy,
public projections, `ApplyResult(in MatchResult)`, `FromRows` failure paths, `Create` validation),
`docs/specs/season-competition-loop/section-3.md` → v0.9 (§3.1 pseudocode binds `ring := ids`),
`docs/tracking/path-to-playable-roadmap.md` → v0.3 (engine test count 306 → 321; C1 relabelled a lower
bound — p50×ticks understates wall-clock, which tracks the mean), `src/CLAUDE.md` → v2.34. Full dotnet gate
PASSED, 0 failures (whole tree green; season-save 97 → 106 tests).)
**Last Updated (prior):** July 25, 2026, later same day (**Season & Competition Loop #30 T0 LANDED** — path-to-playable
roadmap item A1, the first code on that track. **New files** (all in the existing `src/season-save/`, per #30
§4.1 — no new assembly): `SeasonLoopConstants.cs`, `Fixture.cs`, `FixtureScheduler.cs`, `LeagueTableRow.cs`,
`LeagueTable.cs`, `SeasonCalendar.cs`, `BoardObjective.cs`, `BoardState.cs`, `MatchResult.cs`, `SeasonState.cs`,
`SeasonViewModel.cs`, `AssemblyInfo.cs` (KD-7 `InternalsVisibleTo`), + `tests/FixtureSchedulerTests.cs`,
`tests/LeagueTableTests.cs`, `tests/SeasonStateTests.cs` (and their `.meta` files). Behaviour-neutral: no
`MatchEngine`/`WorldStore` wiring (T2), no codec change (T1), `SEASON_SAVE_FORMAT_VERSION` still 1. **Modified:**
`docs/specs/season-competition-loop/section-3.md` → v0.9, `appendices.md` → v0.3, `section-5.md` → v0.3 (all
**ERR-030-010** — §3.1's round-parity venue rule is authoritative; the §3.7 / Appendix C worked tables were
hand-derived without it and are corrected at rounds 1/4, T-SN-FIX-001 re-anchored, new T-SN-FIX-008 venue-balance
lock); `docs/tracking/spec-error-log.md` → v1.41 (ERR-030-010 filed + RESOLVED — the first #30 error found by code
rather than by a downstream spec's approval); `docs/tracking/path-to-playable-roadmap.md` → v0.2 (B6 renderer
decision taken — browser client first, Unity P4–P6 after; A1 marked landed); `src/CLAUDE.md` → v2.33. Full dotnet
gate PASSED, 0 failures (whole tree green; season-save 20 → 97 tests).)
**Last Updated (prior):** July 25, 2026 (**Path-to-playable implementation roadmap authored.** **New file:**
`docs/tracking/path-to-playable-roadmap.md` v0.1 — ROADMAP governance class (the same level as
`management-layer-spec-roadmap.md`; designs no system, opens no numbered spec, changes no `SPEC_INDEX.md`
row). The companion to the spec roadmap: that file sequences *which specs to author*, this one sequences
*which code to land* to reach a playable build. Pins a PM-1/PM-2/PM-3 milestone ladder with testable exit
criteria; inventories the existing floor; splits the work into Track S (host-free season spine, no external
blocker) and Track C (client, host-gated only at Unity P4–P6). **Five quantified constraints:** C1 — full-
fidelity season simulation is infeasible (certified p50 0.4768 ms/tick × `MATCH_TICKS_TOTAL` 324,000 ≈ 154 s
per match ⇒ ~16.3 h for a 380-fixture season), so #30 KD-9's round-resolution model is critical path with a
≲10 ms/match budget; C1a — calibrating it needs ~200 engine-simulated matches ≈ 9 h of compute, budgeted as
its own item; C2 — the Unity host block, with the existing `LiveMatchServer` browser surface as a real
fallback; C3 — `RosterGenerator` already produces deterministic club squads, so a thin league bootstrap
defers #47 entirely; C4 — #50 save-migration debt activates at the PM-2 exit; C5 — spec-defect latency
(ERR-024-001 / ERR-017-002 precedent, 1–3 findings expected per T-phase landing). Phase A–D work breakdown
anchored to each spec's own §7 T-phase plan (#30 T0–T3, #37 T0/T1, #44 T0–T2, #28/#29/#40/#41/#31 minimal)
plus the `interactive-unity-client-design.md` P1–P6 phases. **Headline finding: zero new numbered specs are
required to reach PM-2** — the three governance gaps (league bootstrap, season/squad screens, the #50
decision) each close with a design note under the `lineup-selection-design.md` precedent. Records the B6
renderer decision point (browser-first recommended; UGUI binds the same #38 view models later) and a risk
register. No spec, code, or `SPEC_INDEX.md` change.)
**Last Updated (prior):** July 24, 2026, latest same day (**AR-3 fresh-eyes pass over the Amendment-01 surface (repeat
review, user-requested): 0H+2M+1L, all fixed.** The pass read roadmap §2–§6 in full for the first time —
both M findings came from there. M-1: `spec-plans/spec-48-match-presentation-depth.md` v0.3 — the one file
a Wave-7 #48 supplement author starts from carried no pointer to the Amendment-01 audio split; §1 now
records the boundary (#48 = event→cue mapping only; the playback framework — mixer/buses/catalogue/
settings — is #51, with the stub-bus/rehoming option per spec-51 KD-1). M-2: `management-layer-spec-roadmap.md`
→ **v0.5** — v0.4 had added §1 rows without §3 scope sketches (breaking the v0.2 rows+sketches precedent;
§1 claimed #27–#52 while §3 stopped at #50); §3 sketches added for #51/#52. L: roadmap §6 no-RNG
parenthetical extended to include #51/#52 (footnote ³ covered them, §6 read alone undercounted). AR-4
sweep over the changed surface: 0H+0M — CONVERGENCE, cycle closed.)
**Last Updated (prior):** July 24, 2026, latest same day (**AR-1 over Amendment 01 + the #51/#52 plans: 0H+5M+3L, all
fixed.** M-1 seam-commit contract corrected against source — `SubstitutePlayer` applies immediately
(`MatchEngine.cs` queues only the notification event), Set\*Tactic are the stride-committed pair, and
remote-intents-via-tick-scheduled-command-layer is now an explicit guardrail (amendment §3.2/§3.3 + spec-52
§3/§5); M-2 #48 wave mislabel (amendment §2.4, Wave 8 → "one wave after #48's Wave-7 slice"); M-3 the
"Sound effects" master-plan anchor corrected §3.4 → §3 Month-11–12 "UI & Polish" across amendment /
base-plan pointer / spec-51; M-4 `management-layer-spec-roadmap.md` → **v0.4** (the v0.2 gap-fill
precedent): §1 heading #27–#52 + rows #51/#52 + footnote ³, §7 Wave-8 #51 entry + new Wave-9 block; M-5
`spec-plans/README.md` "Next step" de-staled (promoted-through list per `SPEC_INDEX.md`; next = #32, then
Wave 5) + governance header aligned to roadmap v0.4. L: README footnote ² → ¹; spec-52 §8 guardrails no
longer overstated as automated locks; spec-51 §3 marks #48's mapper "(proposed)". Amendment v0.2,
spec-51/52 plans v0.2.)
**Last Updated (prior):** July 24, 2026, later same day (**#51/#52 spec-plan files authored** — the Amendment-01 §5
next step. **New files:** `docs/tracking/spec-plans/spec-51-audio-sound-design.md` v0.1 (Wave 8, FR-AU,
presentation — none; game-wide audio framework, boundary with #48's match-audio slice pinned as KD-1) +
`docs/tracking/spec-plans/spec-52-multiplayer-transport-netcode.md` v0.1 (Wave 9 post-roadmap, FR-NET,
transport — none; lockstep intent-replication surfaces + Stage-6 gate + pre-Stage-5 guardrails recorded).
**Modified:** `docs/tracking/spec-plans/README.md` (title range #27–#52, index rows, Wave-9 footnote,
determinism-headroom note), this file. No `SPEC_INDEX.md` change — numbers proposed, not reserved.)
**Last Updated (prior):** July 24, 2026, later same day (**Master Plan Amendment 01 — audio + multiplayer transport.**
**New file:** `docs/planning/master-plan-amendment-01-audio-multiplayer-transport.md` v0.1 — planning-level
amendment covering the two feature areas the July-2026 coverage review found named in the master plan but
scoped nowhere (audio/sound design → candidate #48 slice + proposed #51; Stage-6 multiplayer transport /
deterministic netcode → proposed #52, lockstep intent-replication model pinned, no pull-forward before
Stage 5). **Modified:** `docs/planning/master-development-plan.md` (header Amendments pointer only — base
text verbatim), this file (Planning Documents table row). No spec, registry, or code change; #51/#52 are
proposed, not reserved in `SPEC_INDEX.md` (the `spec-plans/README.md` precedent).)
**Last Updated (prior):** July 24, 2026 (**#28 T0 adversarial-review follow-ups + CI `.meta` fix.** **New files:**
`src/player-database/PlayerGenerationRng.cs` v1.0 (+ `.meta`) — shared `DrawBounded` (the biased-but-accepted
generation modulo mapping + rationale) + `Clamp`, extracted from the duplicated copies in `RosterGenerator`
(#27) / `RegenGenerator` (#28); plus the 15 Unity `.meta` sidecars for the new `player-progression/`
assembly + `PlayerGenerationRng.cs` (deterministic `md5(path)` GUIDs via `tools/unity-ci/generate-missing-metas.sh`
— the "Unity .meta integrity" PR check on #250 had failed with 14 missing metas; all other checks passed).
**Modified:** `src/player-database/RosterGenerator.cs` v1.3 + `src/player-progression/RegenGenerator.cs` v1.2
(delegate to `PlayerGenerationRng`; byte-identical — verified by the unchanged PlayerDatabase/PlayerProgression/
MatchEngine suites), `src/player-progression/RegenGenerator.cs` also v1.1 (clubId `<param>` doc corrected —
inert at T0), `src/CLAUDE.md` v2.35. **Full dotnet gate: PASSED, 0 failures (whole tree green).**)
**Last Updated (prior):** July 24, 2026 (**Player Progression & Lifecycle #28 T0 landed** — the new
`src/player-progression/` assembly (`TacticalDirector.PlayerProgression`; references `PlayerDatabase` +
`DeterministicSim` only), the draw-free aging core + the pure single-player regen generator, per
`docs/tracking/progression-t0-implementation-plan.md`. **New files:**
`src/player-progression/player-progression.asmdef`; `src/player-progression/PlayerProgressionConstants.cs`
v1.0 (Appendix A catalogue, region order Fixed→Derived→Cross→GT; the `0x20`/82 RNG mirrors deferred to
T2, KD-B); `src/player-progression/PlayerLifecycle.cs` v1.0 (the §2.2 overlay — PA / CA-cache / `long`
GrowthCursor / `BirthWorldDay` KD-A anchor / retirement); `src/player-progression/TrainingInput.cs` v1.0
(the #29 seam value type, `Neutral` identity — no phantom interface); `src/player-progression/AbilityModel.cs`
v1.0 (integer `ComputeCA` + `ClassifyAgeBand` + weighted `TrySpendOnePoint`/`DrainOnePoint` + the `AgeBand`
enum); `src/player-progression/GrowthProjection.cs` v1.0 (the §3.1 daily step, the sole attribute-mutation
path, curve-off KD-8 identity); `src/player-progression/RegenGenerator.cs` v1.0 (the §3.3 pure regen,
fixed `PROGRESSION_REGEN_FIELDS`=37 budget, returns `(PlayerRecord, PlayerLifecycle)` carrying the drawn
PA); `src/player-progression/tests/player-progression-tests.asmdef` + `PlayerProgressionConstantsTests.cs`
/ `AbilityModelTests.cs` (T-PG-CA-*) / `GrowthProjectionTests.cs` (T-PG-DET-*/ID-*) / `RegenGeneratorTests.cs`
(T-PG-REG-*), 24 tests total. **Modified:** `docs/tracking/progression-t0-implementation-plan.md`
(→ IMPLEMENTED), `docs/tracking/squad-player-stage1-plan.md` v0.5 (A.1 LANDED), `src/CLAUDE.md` v2.34,
root `CLAUDE.md` (Squad/Player OPEN ISSUES — aging #28 T0). Behaviour-neutral (nothing wired into
`MatchEngine`); no `SNAPSHOT_SCHEMA_VERSION` change. **Full dotnet gate: PASSED, 0 failures (whole tree
green; 24 new player-progression tests; SDK 8.0.129 via apt).**)
**Last Updated (prior):** July 22, 2026 (**Goalkeeper #11 + Heading #10 engine integration, Phase 1 (opt-in)** —
the GK/Heading attribute projections landed with a live consumer, per the new supplement
`docs/tracking/gk-heading-engine-integration-design.md` (new). **New files:**
`docs/tracking/gk-heading-engine-integration-design.md` (converged design supplement — AR-1/AR-2/AR-3 +
code-AR + Phase-1 landing note); `src/match-engine/tests/MatchEngineGkHeadingTests.cs` v1.0 (+ `.meta`) —
8 Phase-1 integration locks (flag semantics; flag-off default determinism + commits-nothing; save/header
commit the projection; distinct-squad roster GK Pace flows through; flag-on forward determinism;
durable-capture fails-loud-on / succeeds-off). **Modified:** `src/match-engine/MatchEngine.cs` v1.44
(construct + drive both orchestrators + 4 stateless adapters + 2 RNG streams; `EnableGkHeading()` opt-in;
`DriveGkHeadingTactical`/`DriveGkHeadingPhysics` + §4 save/header triggers seeded from the projections;
`RefreshGkAgentIds`; durable-capture fail-loud guard; `TestOnly_` seams), `src/match-engine/PlayerAttributeProjection.cs`
v1.2 (`ToGoalkeeper` + `ToHeading` added, KD-P8 note removed), `src/match-engine/MatchEngineConstants.cs`
v1.25 (+6 `[GT]` trigger constants), `src/match-engine/match-engine.asmdef` + `src/match-engine/tests/match-engine-tests.asmdef`
(+ HeadingMechanics + GoalkeeperMechanics refs), `src/match-engine/tests/PlayerAttributeProjectionTests.cs`
v1.1 (+2 ToHeading/ToGoalkeeper field-scale locks). No `SNAPSHOT_SCHEMA_VERSION` change (default engine
byte-identical). **Full dotnet gate: PASSED, 0 failures (whole tree green; 290 match-engine tests).**)
**Last Updated:** July 22, 2026 (**GK/Heading cleaner-architecture pass — behaviour-identical.** **New
files:** `src/match-engine/GkHeadingIntentSource.cs` v1.0 (pure static §4 save/header trigger geometry —
`SaveArmed` / `NearestHeaderCandidate` — extracted out of `MatchEngine` so the "when" heuristic is
unit-testable, the `MatchFlowCollisionConsumer` precedent); `src/match-engine/tests/GkHeadingIntentSourceTests.cs`
v1.0 (10 pure-function locks). **Modified:** `src/match-engine/MatchEngine.cs` v1.45 — the four nested
ball/RNG adapters collapsed into ONE `GkHeadingWorldAdapter` (both ball systems share `ApplyKick`; the
two RNG services disambiguate by arity), and `TryCommitSaveIntents`/`TryCommitHeaderIntents` delegate
their geometry to `GkHeadingIntentSource` (keeping only latch + projection + commit).
`gk-heading-engine-integration-design.md` §9b/§9c (cleaner-architecture pass + the deferred Phase-2
flag-removal epic). No `SNAPSHOT_SCHEMA_VERSION` change. **Full dotnet gate: PASSED, 0 failures (whole
tree green; 300 match-engine tests).**)
**Last Updated (prior):** July 21, 2026 (**On-disk match save format landed — snapshot-deserialize Phase 3
`SaveManager` fold (N1)**, per `docs/tracking/match-save-file-design.md` v0.3. **New files:**
`src/match-engine/MatchSaveCodec.cs` v1.0 (+ `.meta`) — pure static `Encode`/`Decode` of the on-disk
save blob (KD-7 boot-`matchSeed` boot-header + `SnapshotHeader` incl. `EnvironmentFingerprint` +
`SnapshotPayload`; `MATCH_SAVE_FORMAT_VERSION`-gated; fail-loud decode with an overflow-safe bound
guard), `src/match-engine/MatchSaveContents.cs` v1.0 (+ `.meta`) — the decode-result readonly struct,
`src/match-engine/MatchSaveManager.cs` v1.0 (+ `.meta`) — static atomic `Save(engine, path)` /
`Load(path, ISquadProvider squads = null) → MatchEngine`, `src/match-engine/tests/MatchSaveManagerTests.cs`
v1.0 (+ `.meta`) — 16 tests (disk round-trip determinism neutral/booking/distinct-squad, codec
round-trip + fail-loud gates, manager fail-loud + overwrite). **Modified:** `src/match-engine/MatchEngine.cs`
v1.43 (public `MatchSeed` property; `TestOnly_CaptureDurableHeader/Payload` → production internal
`CaptureDurableHeader/Payload`), `src/match-engine/MatchEngineConstants.cs` (`[FIXED]
MATCH_SAVE_FORMAT_VERSION = 1`), `src/match-engine/tests/MatchEngineSnapshotRestoreTests.cs` (capture-seam
call sites repointed to the production names). No `SNAPSHOT_SCHEMA_VERSION` change; no asmdef change. Full
dotnet gate: PASSED, 0 failures (279 match-engine tests; whole tree green). Remaining Phase 3: native MXCSR
query (host-blocked) + N2 unified season save.)
**Last Updated (prior):** July 20, 2026 (**Snapshot-deserialize Phase 2 landed — distinct-squad restore
re-projection (#27 T3 / KD-3).** **New file:** `src/match-engine/ISquadProvider.cs` v1.0 (+ `.meta`) — the
public `ClubId → Squad` resolver the `RestoreFromSnapshot` factory threads into re-projection. **Modified:**
`src/match-engine/MatchEngine.cs` v1.42 (`RestoreFromSnapshot(…, ISquadProvider squads = null)`;
`ReprojectDistinctSquads` / `ReprojectBaseLineup` / `ReprojectSubstitutions` replacing the Phase-1
distinct-squad fail-loud; re-projects `_benchIsGoalkeeper`, a boot-constant NOT serialized;
`TestOnly_BenchIsGoalkeeper` seam); `src/match-engine/tests/MatchEngineSnapshotRestoreTests.cs` v1.1
(distinct-squad G3 round-trip — base / mid-match sub / post-restore sub / post-restore keeper-for-keeper
sub — + three provider fail-loud gates). No `SNAPSHOT_SCHEMA_VERSION` change. Full dotnet gate: PASSED, 0
failures (263 match-engine tests; whole tree green). Discovered out-of-scope (Phase-1 completeness
follow-up, root `CLAUDE.md` OPEN ISSUES): a keeper-onto-outfield-slot substitution post-restore diverges
via a Positioning-AI GK-flag-flip interaction.)
**Last Updated (prior):** July 20, 2026 (**Snapshot-deserialize Phase 1 reader landed** — the read half of the
save/load/replay path (`docs/tracking/snapshot-deserialize-design.md` v0.7). **New file:**
`src/match-engine/tests/MatchEngineSnapshotRestoreTests.cs` v1.0 (+ `.meta`) — G3 round-trip determinism
(neutral kickoff / mid-match tactics changed / KD-8 booking-cursor regression) + version-gate /
trailing-byte / distinct-squad fail-loud. **Modified:** `src/match-engine/MatchEngine.cs` v1.41
(`DeserializeWorldState` + `Read*` helpers, the static `RestoreFromSnapshot` factory,
`TestOnly_CaptureDurableHeader/Payload` seams, `_possessingAgentId`/`_prevPossessingAgentId`
reconstruction, event-ledger-boundary trailing guard); `src/pressing-ai/PressingAITick.cs` v1.6 /
`src/defensive-ai/DefensiveAITick.cs` v1.4 / `src/attacking-ai/AttackingAITick.cs` v1.4 /
`src/perception-system/PerceptionSystem.cs` v1.6 / `src/positioning-ai/PositioningAITick.cs` v1.4
(new `RestoreState` counterparts to their CaptureState seams); `src/agent-movement/MovementCommand.cs`
v1.5 (`ReconstructFromSnapshot` factory). No `SNAPSHOT_SCHEMA_VERSION` change (a pure reader over the
v17 writer). Full dotnet gate: PASSED, 0 failures (257 match-engine tests; whole tree green).)
**Last Updated (prior):** July 19, 2026 (**Squad/Player Data Layer #27 lineup selection Plan-3 landed** — proper
lineup selection replaces the roster-order trust mapping in `ConfigureSquads`. **New files:**
`src/match-engine/LineupSelector.cs` v1.0 (pure `Select(Squad, FormationFamily) → LineupPlan`: KD-L1
`DefaultLine → PlayerPosition` bridge, KD-L2 per-line greedy by mean-attribute rating + `PlayerId`
tie-break (no RNG), KD-L3 fail-loud on a short starter line + best-remaining bench, KD-L4 GK flags from
the selection), `src/match-engine/tests/LineupSelectorTests.cs` v1.0 (11 locks),
`docs/tracking/lineup-selection-design.md` v1.0 (design + §5 implementation/code-review). **Modified:**
`src/match-engine/MatchEngine.cs` (`ConfigureSquads` size-gate → `Select` → bounds-gate-selected → apply,
all fail-loud before any write; `ApplySquad`/`ValidateSelectedRecords`/`ValidateSquadSize` index through
the plan + write `_isGoalkeeper`/`_benchIsGoalkeeper` from it; **no `SNAPSHOT_SCHEMA_VERSION` bump**),
`src/match-engine/tests/MatchEngineSquadTests.cs` v1.3 (position-coherent fixtures, KD-L5; distinct-player
routing follows selection; substitution forces the distinct record onto the bench;
`MisOrderedSquad_SelectsGoalkeeperForGkSlot` KD-L4), `src/match-engine/tests/MatchEngineSnapshotSchemaTests.cs`
(`NeutralSquad` made position-coherent). `CLAUDE.md` OPEN ISSUES #27 updated. — prior:
**Squad/Player Data Layer #27 T3 landed** — the snapshot roster-reference
field. **New file:** `docs/tracking/squad-roster-reference-design.md` v0.2 (T3 design supplement, AR-1..AR-2
CONVERGED). **Modified:** `src/match-engine/MatchEngine.cs` v1.39 (per-team `_rosterClubId[TEAM_COUNT]` —
the loaded `Squad.ClubId` or `NO_ROSTER_CLUB_ID`; set by `ConfigureSquads` after validate-and-apply;
serialized at v16; `TestOnly_RosterClubId` seam; exclusion-proof + restore-scope docs updated),
`src/match-engine/MatchEngineConstants.cs` v1.23 (`[FIXED] NO_ROSTER_CLUB_ID = -1`; **`SNAPSHOT_SCHEMA_VERSION`
15 → 16** + v16 doc paragraph), `src/match-engine/tests/MatchEngineSnapshotSchemaTests.cs` v1.13 (pin
15 → 16 + `RosterReference_FeedsSnapshotDigest` probe), `src/match-engine/tests/MatchEngineSquadTests.cs`
v1.2 (T1 neutrality lock superseded by the KD-T3-2 identity-capture / same-config-determinism /
distinct-ClubId / sentinel-seam locks; post-landing code AR 0H+0M+1L added
`ConfiguredDefaultSquad_IsBehaviourNeutral_ObservableStateMatchesUnconfigured`), `docs/tracking/
squad-roster-reference-design.md` v0.3 (code-AR round), `docs/tracking/squad-player-data-design.md`
v0.6 + `docs/tracking/player-attribute-projection-design.md` (T3-landed notes), root + src `CLAUDE.md`.
Full dotnet gate re-run: PASSED, 0 failures (237 match-engine tests).)
**Last Updated (prior):** July 17, 2026, latest same day (**T1/T2 repeat adversarial review (AR-4 + AR-5 sweep):
1M+4L, all doc-only, all fixed — CONVERGENCE, cycle closed. No new files, modified only:**
`src/attacking-ai/AttackingAgentSnapshot.cs` v1.1 (M-1 — Pace/Dribbling docs aligned to the live
KD-P3 ÷ATTRIBUTE_MAX convention the T1 writer actually supplies; the (raw−1)/19 math switch stays a
recorded deferred question), `src/match-engine/MatchEngine.cs` v1.38 (three stale neutral-placeholder
comments aligned + the ConfigureSquads extra-players note), `src/match-engine/MatchEngineConstants.cs`
v1.22 (STAGE0_NEUTRAL_* stale ERR-007 TODOs retired — production-unconsumed since T1, retained as the
KD-P7 neutral-equivalence references), `src/match-engine/PlayerAttributeProjection.cs` v1.1
(ToNormalized note), root + src `CLAUDE.md`. Full dotnet gate re-run: PASSED, 0 failures.)
**Last Updated (prior):** July 17, 2026, latest same day (**Squad/Player Data Layer T1/T2 landed** — `MatchEngine`
attribute seeding sourced from canonical player records per `player-attribute-projection-design.md`
v0.3. **New files:** `src/match-engine/PlayerAttributeProjection.cs` v1.0 (pure canonical→per-spec
projections; KD-P1 derived KickPower; KD-P3 normalization; KD-P8 no GK/Heading targets),
`src/match-engine/tests/PlayerAttributeProjectionTests.cs` v1.0 (scale/derivation/neutral-equivalence
locks), `src/match-engine/tests/MatchEngineSquadTests.cs` v1.0 (digest neutrality/divergence/
determinism + substitution canonical swap + fail-loud gates incl. the self-AR-1 M-1
both-squads-validate-before-write lock). **Modified:** `src/match-engine/MatchEngine.cs` v1.37
(`_canonicalAttrs`/`_benchCanonicalAttrs` + all seeding sites converted + `ConfigureSquads` +
`SubstitutePlayer` canonical swap/re-projection + 6 TestOnly seams; no schema change),
`src/match-engine/match-engine.asmdef` + `tests/match-engine-tests.asmdef` (+`TacticalDirector.PlayerDatabase`),
`src/player-database/PlayerAttributes.cs` v1.1 (KD-P9 FirstTouchAbility reserved→consumed, doc-only),
`docs/tracking/player-attribute-projection-design.md` v0.4, `docs/tracking/squad-player-data-design.md`
v0.5, root + src `CLAUDE.md`. Full dotnet gate: PASSED, 0 failures.)
**Last Updated (prior):** July 17, 2026, later same day (**Fourth repeat adversarial review (AR-4): 0H+0M+1L
doc-only — CONVERGENCE, cycle closed. No new files, modified only:** `src/match-engine/MatchEngine.cs`
v1.36 (L — `_lastHolderAgentId` writer comment aligned to the last-settled-holder approximation;
no code change), root + src `CLAUDE.md`. Full dotnet gate re-run: PASSED, 0 failures.)
**Last Updated (prior):** July 17, 2026 (**Third repeat adversarial review (AR-3): 1M, fixed — no new files,
modified only:** `src/match-engine/MatchEngine.cs` v1.35 (M-1 — foul candidates involving a
sent-off participant discarded at `ApplyFoulIfCaptured`; pre-fix a frozen red-carded agent
repeatedly won free kicks and drew cards against opponents running into them),
`src/match-engine/tests/MatchEngineFoulCardTests.cs` v1.1 (+2 regression locks), root + src
`CLAUDE.md`. Full dotnet gate re-run: PASSED, 0 failures.)
**Last Updated (prior):** July 16, 2026, later same day (**Repeat adversarial review (AR-2): 1M+1L, both fixed —
no new files, modified only:** `src/match-engine/MatchEngine.cs` v1.34 (M-1 — sent-off agents
excluded from the first-touch receiver scan; pre-fix a red-carded agent could receive into
un-releasable possession, deadlocking play), `src/match-engine/tests/MatchEngineFirstTouchTests.cs`
v1.1 (+1 regression lock), `src/player-database/AttrIdx.cs` v1.1 (L — group-count doc comment),
root + src `CLAUDE.md`. Full dotnet gate re-run: PASSED, 0 failures.)
**Last Updated (prior):** July 16, 2026 (**Adversarial-review fix pass over the July 14–15 landings (match-flow
completion / interactive match view / squad-player data layer) — no new files, modified only:**
`src/match-engine/MatchEngine.cs` v1.33 (M-1 substitution yellow-card reset + L-2 post-full-time
`SubstitutePlayer` refusal + L-1 last-holder-approximation doc at the restart seam),
`src/match-engine/RestartResolver.cs` v1.1 (L-1 doc), `src/match-engine/tests/MatchEngineSubstitutionTests.cs`
v1.1 (+2 regression locks), `src/match-viewer/LiveMatchServer.cs` v1.1 (L-3 viewer clock rounds
before the minute split + L-4 post-Stop connection threads answer 503),
`src/player-database/SquadFileLoader.cs` v1.2 (M-2 age range-checked to [AgeMin, AgeMax] + L-5
gap-fill doc), `src/player-database/tests/SquadFileLoaderTests.cs` v1.1 (+2 age-bounds locks),
`src/player-database/RosterGenerator.cs` v1.2 (L-6 modulo-bias doc note), root + src `CLAUDE.md`.
dotnet gate runs in CI on push.)
**Last Updated (prior):** July 13, 2026 (**P1 real perf harness LANDED (cert-run-runbook.md P1 Tier A) —
replaces the synthetic `tools/perf-harness/run.sh` `p50=0.000` stub with a harness that boots the
real `MatchEngine` capstone.** New files: `src/performance-optimization/StopwatchPerfHarness.cs`
(concrete `IPerfHarness`, §3.3.5 manual Stopwatch capture; nearest-rank p50/p99),
`src/performance-optimization/tests/performance-optimization-tests.asmdef` +
`src/performance-optimization/tests/StopwatchPerfHarnessTests.cs` (new perf-opt test assembly),
`src/match-engine/tests/MatchEngineCapstonePerfHarness.cs` (Stopwatch-times each `RunTick` of the
capstone; non-cert `LinuxNonCertPlatformPin` stamp) + `src/match-engine/tests/MatchEngineCapstonePerfHarnessTests.cs`.
Modified: `src/match-engine/tests/MatchEngineCapstoneScenarios.cs` (`NumTicks` → public
`KickoffMultiSecondTicks`), `src/CLAUDE.md` (BUILD AND TEST COMMANDS batch-mode command +
WHAT IS NOT HERE YET row), `docs/tracking/cert-run-runbook.md` v1.3 (Step 2 concrete command +
P1 row), `tools/perf-harness/run.sh` (header note), root `CLAUDE.md` OPEN ISSUES. Linux run is
NON-certifying; the certified capture stays gated on P2 (pinned host) + Steps 2–4. dotnet gate
runs in CI on push.)
**Last Updated (prior):** July 11, 2026, latest same day (**Engine substrate landed — goal detection +
score state + match-length/halves model; #26 half-time trigger + live ladder inputs activated.**
New file: `src/match-engine/tests/MatchEngineGoalTests.cs` (6 tests). Modified:
`match-engine/{MatchEngine.cs v1.30 (Resolve-phase CheckGoalAndRestart + _goals/_lastHolderAgentId +
v14 serialization + RunManagerDecisionPoints live inputs + 4 TestOnly seams),
MatchEngineConstants.cs v1.20 (MATCH_LENGTH_MINUTES / MATCH_TICKS_TOTAL / HALF_TIME_BOUNDARY_TICK;
SNAPSHOT_SCHEMA_VERSION 13 → 14), ManagerDecisionGate.cs v1.1 (half-time trigger active),
ManagerAdaptation.cs (docs), tests/ManagerAITests.cs v1.1 (+4),
tests/MatchEngineSnapshotSchemaTests.cs v1.11 (pin 14 + ScoreState probe)},
`tactical-instructions/TacticalPresetsConstants.cs` v1.1 (doc — MATCH_TICKS_TOTAL allocated
engine-side, [CROSS]), #26 spec section-1/2/3/9 (gate closures + [CROSS] promotion),
`match-engine-design.md` v1.4, root + src CLAUDE.md. Full dotnet gate: PASSED, 0 failures.)
**Last Updated (prior):** July 11, 2026, later same day (**#26 T1–T4 manager-AI wiring landed** —
preset→config projection, decision gate, kickoff scoring, adaptation ladder + `ManagerState`
serialization (`SNAPSHOT_SCHEMA_VERSION` 12 → 13); default-behaviour-neutral (Human zero-init
identity, KD-4). New files: `src/tactical-instructions/TacticalPresetsConstants.cs` (#26 §3.5 +
A.2/A.3 catalogue), `src/match-engine/{ManagerMode.cs, ManagerProfile.cs, ManagerState.cs,
ManagerDecisionGate.cs, ManagerAdaptation.cs, TacticPresetProjection.cs,
tests/ManagerAITests.cs}`. Modified: `match-engine/{MatchEngine.cs v1.29 (ConfigureManager +
stride decision gate before the FR-TI-027 commit + v13 serialization + boot/TestOnly seams),
MatchEngineConstants.cs v1.19 (schema 12 → 13), tests/MatchEngineSnapshotSchemaTests.cs v1.10
(pin 13 + ManagerState probe)}`. Per-file table reconciliation: the July-10 #26/back-prop T0
rows in `src/tactical-instructions/` (three dial enums, `TacticPreset`, `TacticPresetLibrary`,
`Tests/TacticPresetLibraryTests`) and the June-30 `Tests/BalancePassInvariantsTests` row had been
recorded only in this header note — rows added now. Full dotnet gate run locally: PASSED, 0
failures.)  
**Last Updated (prior):** July 11, 2026 (**Specs #23/#24/#25 wiring landed** — the SlotComposer stage
insertions, the #25 RotationController, the #8 marked-pass-target penalty, and the match-engine
Phase-D writers + serialization (`SNAPSHOT_SCHEMA_VERSION` 11 → 12). New files:
`src/positioning-ai/RotationController.cs` + `Tests/SlotComposerStageTests.cs` +
`Tests/RotationControllerTests.cs`. Modified: `positioning-ai/{SlotComposer.cs v1.2,
PositioningAITick.cs v1.3, PositioningPerceptionSnapshot.cs v1.1}`, `decision-tree/{UtilityScorer.cs
v1.10, TacticalContext.cs v1.7, TacticalWeights.cs v1.5, decision-tree(.Tests).asmdef (+PositioningAI
ref for the MARKING_RADIUS_M [CROSS] mirror), Tests/UtilityScorerTests.cs v1.5}`,
`match-engine/{MatchEngine.cs v1.28, MatchEngineConstants.cs v1.18,
tests/MatchEngineTacticTests.cs v1.5, tests/MatchEngineSnapshotSchemaTests.cs v1.9}`.
Per-file table reconciliation: the July-7 cheap-item `RestDefenseEvaluator(.Tests)` rows and the
July-10 #23/#24/#25 T0 rows had been recorded only in this header note, never in the
positioning-ai per-file table — rows added now (24 → 41 files incl. tests). Full dotnet gate run
locally: PASSED, 0 failures.)  
**Last Updated (prior):** July 10, 2026, latest same day (**T0 AR-1 fix pass: 0H+1M+3L, all resolved** — findings
confined to the #26 preset surface + one header defect; no new files. M-1: `TacticPreset.cs` v1.1
ctor snapshot-copies `Players` (a retained live caller array bypassed the FR-TP-014 gate — the
living-world slice-2 AR-1 M-1 / match-viewer AR-3 M-1 class); L-1: `TacticPresetLibraryTests.cs`
v1.1 composition tests gain inherited-dial == Balanced locks (the `Compose`-defaults coherence was
claimed but unlocked for dials some presets touch) + the new M-1 snapshot regression;
`TacticPresetLibrary.cs` v1.1 doc de-overclaimed; L-2: `TacticPreset` FR-TP-014 docs re-anchored to
the consuming applier seam (the library performs no validation call and cannot know roster size —
vacuously satisfied at Stage 0, test-locked); L-3: `TeamTacticFileLoader.cs` header gains its
missing `// Modified:` field (FR-CS-056). Everything else verified clean against the specs:
#23 §3.1/§3.2/§3.3 + #24 §3.1/§3.2 worked examples spec-exact, #25 Appendix A/D row-for-row,
#26 A.1 composition-for-composition, NaN gates correct under Unity `Clamp01` semantics, family row
keys hit real slots, ordinal locks extended. Full dotnet gate re-run: PASSED, 0 failures.)  
**Last Updated (prior):** July 10, 2026, latest same day (**#23–#26 T0 scaffolding landed** — 14 new `src/`
files + 4 test files + 5 edits, all behaviour-neutral. tactical-instructions: +
`DismarkIntensity.cs` / `BuildUpStructure.cs` / `RotationFreedom.cs` / `TacticPreset.cs` /
`TacticPresetLibrary.cs` + `Tests/TacticPresetLibraryTests.cs`; `TeamTactic.cs` v1.3 (ERR-021
field appends), `Tests/EnumOrdinalStabilityTests.cs` v1.3, `Tests/FactoryIdentityTests.cs` v1.3.
match-engine: `TeamTacticFileLoader.cs` v1.2 (+3 keys). positioning-ai: + `MarkingDwellState.cs` /
`MarkingPressureEvaluator.cs` (#23), `BuildUpZone.cs` / `BuildUpZoneState.cs` /
`BuildUpZoneClassifier.cs` / `BuildUpOverlayCatalogue.cs` (#24), `RotationPair.cs` /
`RotationPairState.cs` / `RotationAdjacencyCatalogue.cs` (#25) + `Tests/
MarkingPressureEvaluatorTests.cs` / `Tests/BuildUpStructureTests.cs` /
`Tests/RotationCatalogueTests.cs`; `PositioningAIConstants.cs` v1.2. Spec-side: ERR-024-001 (H,
resolved — #24 catalogue row keys matched no slot; `build-up-structures/appendices.md` v0.3 +
`section-3.md` v0.3), #26 `section-2.md` v0.3 (stale §2.2.2 ordinals), `spec-error-log.md` v1.31.
Full dotnet gate run locally: PASSED, 0 failures.)  
**Last Updated (prior):** July 10, 2026, later same day (**Specs #23–#26 `IN REVIEW → APPROVED` + the seven
back-prop amendments; no new files, no `src/` change** — all 44 files across the four spec folders
flip `Status: APPROVED`; the four `section-9-approval-checklist.md` files → v0.4 (R-01..R-05
sign-off tables + decisions); `tactical-presets/section-8.md` → v0.3 (Bradley & Noakes 2013 DOI
10.1080/02640414.2013.796062 VERIFIED); the three owning `section-2.md` files → v0.3 (back-prop
tables record filed ERRs + pinned append order). Back-prop targets: `tactical-instructions/
section-2.md` → v0.5 + `appendices.md` → v0.5 (ERR-021-005/006/007 `TeamTactic` field + Appendix B
appends); `positioning-ai/section-3.md` → v0.6 (new §3.7.1, ERR-012-007/008/009 incl. the
`SlotIndex` single-writer amendment); `decision-tree/section-3-2.md` → v1.5 (ERR-008-012 §3.2.2.1
anchor note). `spec-error-log.md` → v1.30 (seven entries filed and resolved); `SPEC_INDEX.md`
count **26 APPROVED / 0 IN REVIEW**.)  
**Last Updated (prior):** July 10, 2026 (**#23–#26 gate close-out touches 10 spec files, no new files, no
`src/` change** — the four `section-8.md` files → v0.2 (citations verified / replaced /
reclassified; #26 Bradley row pending with a recorded attempt); the four
`section-9-approval-checklist.md` files → v0.3 (gates ticked; headers de-drifted from the v0.2
pass); `positional-rotations/appendices.md` → v0.3 (A.2 4-3-3 + A.3 4-2-3-1 adjacency tables);
`tactical-presets/appendices.md` → v0.3 (A.1 member names pinned against the #21 enums).)  
**Last Updated (prior):** July 8, 2026, later same day (**PASS-1 adversarial reviews on #23–#26** — one new
`adversarial-review-section-files-v1.md` per spec folder (4 files); findings resolved in same-day
v0.2 fix passes touching section files across all four folders (#23 0H+1M+3L; #24 0H+3M+2L; #25
1H+1M+3L + PASS-2 clean at H/M; #26 0H+1M+2L). No `src/` change.)  
**Last Updated (prior):** July 8, 2026 (**Candidates #23–#26 promoted to section files at `IN REVIEW`** —
four new spec folders, 11 files each (outline + section-1..8 + section-9-approval-checklist +
appendices, all v0.1): `docs/specs/dismarking-ai/` (#23), `docs/specs/build-up-structures/` (#24),
`docs/specs/positional-rotations/` (#25), `docs/specs/tactical-presets/` (#26). Design supplements
bumped v0.3 → v0.4 / v0.4 → v0.5 (promotion notes + "Specification Before Code" citation fix);
`SPEC_INDEX.md` registry rows added, RESERVED entries retired. Also reconciled: the "Current
Specification Folders" table below was missing rows 21/22 since their June promotions — rows
21–26 added now. No `src/` change.)  
**Last Updated (prior):** July 7, 2026, later same day (**Two design-supplement tracking docs added** —
`docs/tracking/advanced-positional-behaviors-design.md` and
`docs/tracking/game-model-ai-manager-design.md`; see the new Tracking Documents rows. No `src/`
change.)  
**Last Updated (prior):** July 7, 2026 (**Tactical-theory cross-reference: four cheap-item additions landed** — (1) `MarkingOrientation` dial (new `src/tactical-instructions/MarkingOrientation.cs`; `TeamTactic` v1.2 appends the field; `SNAPSHOT_SCHEMA_VERSION` 10 → 11) scales the #14 MAN_MARK candidate radius via new `defensive-ai/TacticTranslation.MarkRadiusScalar` + `DefensiveSnapshot.MarkingOrientation` routing field + `MarkAssigner` consumption; (2) Positioning AI #12 rest-defense coverage check (new `src/positioning-ai/RestDefenseEvaluator.cs` + `Tests/RestDefenseEvaluatorTests.cs`; `PositioningAITick.GetRestDefenseSufficient()`) dampens PASS/SHOOT/DRIBBLE via new `TacticalContext.RestDefenseSufficient` + `TacticalWeights.RestDefenseRiskMult` in `UtilityScorer`; (3) half-spaces PASS bonus (`TacticalContext.AgentLane` routes each agent's existing Positioning AI `LaneId`; `decision-tree.asmdef` gains the `PositioningAI` reference; `TacticalWeights.LaneMult[5]`) boosts PASS utility in LH/RH lanes; (4) curving-press blind-side bias (new `src/pressing-ai/BlindSideApproach.cs` + `Tests/BlindSideApproachTests.cs`; `PressingAIConstants.BlindSideApproachBiasM`) nudges the primary presser's approach target toward the ball carrier's blind side in `PressingAITick`. All four default/Balanced/C ⇒ identity, byte-identical to pre-addition. New `MatchEngine.TestOnly_MarkingOrientation`/`_RestDefenseSufficient`/`_AgentLane` seams + `MatchEngineTacticTests` cases + `MatchEngineSnapshotSchemaTests.MarkingOrientation_FeedsSnapshotDigest`. dotnet gate not runnable in this environment; compile-checked by hand (brace/paren balance).)  
**Last Updated:** July 7, 2026, later same day (**Tactical-theory cross-reference items (2)/(3)/(4) corrected/reverted after user review.** (1) `MarkingOrientation` dial stands unchanged (new `src/tactical-instructions/MarkingOrientation.cs`; `TeamTactic` v1.2 appends the field; `SNAPSHOT_SCHEMA_VERSION` 10 → 11) scaling the #14 MAN_MARK candidate radius via `defensive-ai/TacticTranslation.MarkRadiusScalar` + `DefensiveSnapshot.MarkingOrientation` routing field + `MarkAssigner` consumption. (2) Positioning AI #12 rest-defense coverage check redesigned: the dampener no longer applies as a flat team-wide penalty — `UtilityScorer.ComputeUtility` now scales `TacticalWeights.RestDefenseRiskMult` by the ball carrier's own tactical awareness (`Mathf.Lerp(1.0f, RestDefenseRiskMult, (A_Decisions + A_Anticipation) * 0.5f)`) so an oblivious carrier takes no dampening at all — the insufficient coverage is a manager-facing tactical flaw, not something the AI silently corrects. (3) half-spaces PASS bonus REVERTED entirely — `TacticalContext.AgentLane`, `TacticalWeights.LaneMult`, and `decision-tree.asmdef`'s `PositioningAI` reference are all removed; half-spaces are an exploitable spatial gap requiring tactical/player instructions, not a flat bonus. (4) curving-press mechanic redesigned: `src/pressing-ai/BlindSideApproach.cs` + `Tests/BlindSideApproachTests.cs` DELETED, replaced by new `src/pressing-ai/CoverShadowCurve.cs` + `Tests/CoverShadowCurveTests.cs` — bends the primary presser's approach target toward the cover-shadow lane point between the ball carrier and the nearest eligible opponent receiver, blended by `PressingAIConstants.CoverCurveBlendWeightMax` scaled by the presser's own attribute average (new `PressingAgentSnapshot.DefensivePositioningAttribute`/`PhysicalEffortAttribute`/`MentalSharpnessAttribute`, sourced by `MatchEngine.FillPressingSnapshot` from `_dtAttrs`) — a poor, low-effort defender curves almost none. Removed: `MatchEngine.TestOnly_AgentLane` seam + its test cases; `decision-tree-tests.asmdef`'s `PositioningAI` reference. Spec docs updated: `decision-tree/section-7.md` §7.7 (redesigned) + §7.8 (marked REVERTED) + `pressing-ai/section-7.md` §7.12 (redesigned). dotnet gate not runnable in this environment; compile-checked by hand (brace/paren balance).)  
**Last Updated (prior):** July 2, 2026 (**Minimal match viewer — first presentation-layer surface.** New assembly `src/match-viewer/` (`TacticalDirector.MatchViewer`; tooling, not a numbered spec): `match-viewer.asmdef`, `MatchViewerConstants.cs`, `ReplayFrame.cs`, `MatchReplay.cs`, `MatchReplayRecorder.cs`, `HtmlReplayExporter.cs` + `tests/match-viewer-tests.asmdef`, `tests/MatchViewerTests.cs` — see the new `src/match-viewer/` section. Modified: `src/match-engine/MatchEngine.cs` v1.24 (public read-only observation surface: `BallView`/`AgentView(i)`/`AgentTeamId(i)`/`AgentIsGoalkeeper(i)`/`PossessingAgentId` — value-type copies, no behaviour change; observer-neutrality digest-locked in `MatchViewerTests`). dotnet gate runs in CI on push.)  
**Last Updated (prior):** June 30, 2026 (**Tactical Instructions #21 — Stage-1 per-agent on-disk tactic-file loader.** New: `src/match-engine/PlayerTacticFileLoader.cs` (`Parse(text) → PlayerTacticConfig` over the `[agent N]` `key = value` text grammar — the per-agent sibling of `TeamTacticFileLoader`; omitted key/section ⇒ identity ⇒ behaviour-neutral; fail-loud on every malformation) + `src/match-engine/tests/PlayerTacticFileLoaderTests.cs`. The team + per-agent in-code config sources, boot appliers, and text loaders now all exist; only the pinned `[GT]` disk-encoding swap (FR-CS-019, Stage-1, outside #21) is outstanding. No production-behaviour change at the default. dotnet gate runs in CI on push. Prior June 30, 2026 (**Tactical Instructions #21 — Stage-1 leftovers (per-agent tactic config + G2 balance pass + §3.4 depth recompute).** New: `src/match-engine/PlayerTacticConfig.cs` + `src/match-engine/PlayerTacticConfigApplier.cs` (in-code per-agent `PlayerTactic` config source + boot applier; mirrors `TeamTacticConfig`; `Identity` = behaviour-neutral), `src/match-engine/tests/PlayerTacticConfigTests.cs`, `src/tactical-instructions/Tests/BalancePassInvariantsTests.cs` (pins identity-row exactness + strict monotonicity + `RoleWeightModifiers` ∈ [0.5,2.0] shapes). Modified: `src/match-engine/MatchEngine.cs` v1.23 (per-agent `_active`/`_pendingPlayerTactics[SQUAD_SIZE]` + public `SetPlayerTactic` + stride-commit + `ctx.PlayerTactic` route + `WritePlayerTactic` serialization + `TestOnly_PlayerTactic`; §3.4 `FillDefensiveSnapshot.DefensiveLineDepth = Clamp01(DefensiveLine + MentalityLineBias)`), `src/match-engine/MatchEngineConstants.cs` v1.17 (`SNAPSHOT_SCHEMA_VERSION` 9 → 10 + v10 doc), `src/match-engine/tests/MatchEngineTacticTests.cs` v1.4 + `MatchEngineSnapshotSchemaTests.cs` v1.7 (pin 9→10 + `PlayerTactic_FeedsSnapshotDigest`), `src/tactical-instructions/TacticalInstructionsConstants.cs` v1.1 (G2 magnitudes pinned — values unchanged; framing illustrative → pinned), `src/decision-tree/TacticTranslation.cs` + `UtilityScorer.cs` + `TacticalContext.cs` (doc reframes), `docs/specs/tactical-instructions/section-3.md` + `section-5.md` (G2 balance pass DONE). No production-behaviour change at the default (byte-identical). dotnet gate runs in CI on push.) Prior June 28, 2026 (**FR-PO-052 certified perf baseline corpus machinery.** New: `src/performance-optimization/CertificationStatus.cs` (Pending/Certified) + `CertifiedPerfBaseline.cs` (a certification-tagged baseline corpus entry — Pending carries NO metric and refuses to build a record because the Linux gate is NON-certifying; Certified validates a complete manifest + finite positive metrics and projects to a corpus BaselineRecord; platform-pin tokens Stage0CertPlatformPin/LinuxNonCertPlatformPin), `src/match-engine/tests/CertifiedPerfBaselineTests.cs` (PENDING lock + certified projection through PerfGateRunner + fail-closed invariants), and the first corpus artifact `docs/specs/performance-optimization/baselines/match-engine/kickoff-multi-second.cert.md` (PENDING_CERT_RUN — runbook to promote on the pinned platform). Modified: `src/match-engine/tests/MatchEngineCapstoneTests.cs` v1.1 (non-cert anchor pin → the named LinuxNonCertPlatformPin constant; behaviour-neutral). No production behaviour change; the authoritative per-tick budget still requires a run on the pinned Windows/Unity tuple. Prior June 28, 2026: Match Engine design note **Phase F — capstone closed-loop scenario; Match Engine integration (Phases A–F) complete.** New: `src/match-engine/tests/MatchEngineCapstoneScenarios.cs` (`match-engine-kickoff-multi-second` on the #19 ScenarioRunner — owning specs {1,2,3,4,5,6,7,8,12,13,14,15,16,17,19}, Tier B; boots a real MatchEngine, ticks it 600× = 10 s @ 60 Hz, records gameplay-invariant predicates [tick-count; ai-stride-cadence = NumTicks/AI_PHASE_STRIDE = 100; ball + agents finite and on-pitch every tick; chained snapshot digest advances] + a two-run same-seed determinism digest match) and `MatchEngineCapstoneTests.cs` (runs the scenario through ScenarioRunner.Run → Passed; direct two-run digest-chain equality test; FR-PO-052 per-tick perf-gate activation via PerfGateRunner.Run against a generous Stage-0 anchor BaselineRecord — NON-certifying Linux gate, authoritative budget stays on the pinned Windows/Unity tuple). Modified: `src/match-engine/tests/match-engine-tests.asmdef` (+`TacticalDirector.TestingStrategy` + `TacticalDirector.PerformanceOptimization`). No production `MatchEngine.cs` change — the scenario reads world state through existing internal `TestOnly_*` seams + public `CurrentTick`/`AiPhaseRunCount`/`CurrentSnapshotDigest`. `docs/tracking/match-engine-design.md` v1.0 (Phase F implemented; status line + §5 Phase F + Version History updated). CI gate (`bash tools/dotnet-ci/run-gate.sh`) verifies compile + suite on push — dotnet is unavailable in the authoring environment. Prior June 27, 2026 (Match Engine design note Phase E — events-phase consumers. New: `src/match-engine/tests/MatchEngineEventsTests.cs` (publish-on-change interrupts only the new holder; no-change publishes nothing; two same-seed runs with a possession transition produce byte-identical ledger-backed digest chains + lock the per-match reset seam; transition-vs-baseline effect; Tier A boot-phase Subscribe guard). Modified: `src/event-system/EventBus.cs` v2.1 (new public `ResetForNewMatch()` — clears the Tier A/B subscriber Dispatchers table + Tier C channel and reopens the boot phase, leaving the EventRegistry row schema intact; closes match-engine Risk #4 — the process-static bus could not be re-subscribed by a second match), `src/match-engine/MatchEngine.cs` v1.15 (RunResolvePhase calls `PublishPossessionChangeIfChanged` after C4 — diffs the settled holder vs the new `_prevPossessingAgentId` and publishes a Tier A PossessionChangedEvent #17 0x04 on a net change; Boot calls `EventBus.ResetForNewMatch()` then `EventBus.Subscribe<PossessionChangedEvent>(OnPossessionChanged)` which NotifyInterrupt()s the new holder's DecisionTree; new `TestOnly_DtState` seam), `src/match-engine/MatchEngineConstants.cs` v1.15 (`POSSESSION_CHANGE_REASON_UNSPECIFIED` [FIXED] byte 0), `src/match-engine/tests/match-engine-tests.asmdef` (+`TacticalDirector.EventSystem`). No `SNAPSHOT_SCHEMA_VERSION` bump — world-state body unchanged; only the serialized ledger digest now carries the event. `docs/tracking/match-engine-design.md` v0.9.13 (Phase E implemented; Risk #4 RESOLVED; §4 boot-step-2 + §5 Phase E + status line updated). CI gate (`bash tools/dotnet-ci/run-gate.sh`) verifies compile + suite on push — dotnet is unavailable in the authoring environment. Phase F pending.) Prior June 22, 2026, later same day (Match Engine design note Phase D step D3 — first-touch wiring. Modified: `src/match-engine/MatchEngine.cs` v1.8 (RunResolvePhase calls RunFirstTouch after the C3 executor Update and before the C4 UpdateMatchContext — a loose, ground-level, moving ball arriving within FIRST_TOUCH_ACCEPTANCE_RADIUS_M of the nearest APPROACHING agent triggers BuildFirstTouchContext via the real internal PressureEvaluator + OrientationDetector seams, EvaluateFirstTouch/ApplyTouchResult through the new FirstTouchWorldAdapter, and the outcome maps onto possession; CONTROLLED → toucher, INTERCEPTION → interceptor (AGENT_ID_NONE at Stage 0 → loose), LOOSE_BALL/DEFLECTION → loose), `src/match-engine/MatchEngineConstants.cs` v1.8 (FIRST_TOUCH_ACCEPTANCE_RADIUS_M + FIRST_TOUCH_MIN_BALL_SPEED_M_S [GT]; SNAPSHOT_SCHEMA_VERSION unchanged — FirstTouchSystem is stateless, writing only _ball + _possessingAgentId), `src/match-engine/match-engine.asmdef` (+FirstTouch), `src/first-touch/AssemblyInfo.cs` v1.1 (+InternalsVisibleTo("TacticalDirector.MatchEngine") so the host runs the internal PressureEvaluator / OrientationDetector context-assembly seams). New: `src/match-engine/tests/MatchEngineFirstTouchTests.cs` (CONTROLLED receive → possession, home + away frame-agnostic; receding / high / possessed not touched; same-seed digest determinism). `docs/tracking/match-engine-design.md` v0.9.6 (D3 implemented). CI gate (`bash tools/dotnet-ci/run-gate.sh`) verifies compile + suite on push — dotnet is unavailable in the authoring environment. D2b + D4–D5 + E–F pending.) Prior June 22, 2026, later same day (Match Engine design note Phase D step D2a — mechanics-AI wiring (Positioning AI #12). Modified: `src/match-engine/MatchEngine.cs` v1.7 (RunAiPhase runs RunPositioningAI before the perception/DT loop — one PositioningAITick + reused PositioningPerceptionSnapshot per team seeded at boot from STAGE0_FORMATION, filled from world state + ticked, GetFormationSlot folded into each agent's TacticalContext; away team mapped through the canonical attack-+X frame via the self-inverse 180° MirrorPitchIfAway — ERR-008-002 guard; new helpers RunPositioningAI/FillPositioningSnapshot/ComputeTeamMeanFatigue/MirrorPitchIfAway + TestOnly_FormationSlot), `src/match-engine/MatchEngineConstants.cs` v1.7 (MaxEntityId [DERIVED] + STAGE0_FORMATION/STAGE0_TACTICAL_INTENSITY [GT]; SNAPSHOT_SCHEMA_VERSION unchanged — positioning hysteresis serialization is the D4 step), `src/match-engine/match-engine.asmdef` (+PositioningAI). New: `src/match-engine/tests/MatchEngineMechanicsTests.cs` (formation slots feed the decision context; away-team mirror; same-seed slot determinism). `docs/tracking/match-engine-design.md` v0.9.5 (D2a implemented; §5 split into D2a/D2b; Pressing #13 / Defensive #14 / Attacking #15 remain as D2b). CI gate (`bash tools/dotnet-ci/run-gate.sh`) verifies compile + suite on push — dotnet is unavailable in the authoring environment. D2b + D3–D5 + E–F pending.) Prior June 22, 2026, later same day (Match Engine design note Phase D step D1 — AI-phase wiring (perception → decision → movement). Modified: `src/match-engine/MatchEngine.cs` v1.6 (RunAiPhase drives a host-owned perception SpatialHashGrid + PerceptionSystem.OnHeartbeat ×22 → DecisionTree.ReceiveSnapshot ×22; new HostMovementController adapter writes MovementCommands into _commands; InitializeAiSnapshots assembles the Stage-0 static §2.5 AI input snapshots; DecisionTree EventBusRegistrar booted; PerceptionSubsystem/DecisionTreeAI aliases), `src/match-engine/MatchEngineConstants.cs` v1.6 (PERCEPTION_GRID_POINT_INSERT_RADIUS [FIXED]; SNAPSHOT_SCHEMA_VERSION unchanged — DT/perception cross-tick state serialization is D4), `src/match-engine/match-engine.asmdef` (+PerceptionSystem), `src/match-engine/tests/MatchEnginePhysicsTests.cs` v1.1 (OutfieldAgent_MovesTowardTarget... replaced by AiPhase_DrivesChain_GoalkeepersSkipped — AI now owns _commands; +DeterministicSim using). `docs/tracking/match-engine-design.md` v0.9.5 (D1 implemented; §5 D1 + §6.5 perception/DT cross-tick-state D4 follow-up). No files added or removed. CI gate (`bash tools/dotnet-ci/run-gate.sh`) verifies compile + suite on push — dotnet is unavailable in the authoring environment. D2–D5 + E–F pending.) Prior June 19, 2026, later same day (Match Engine design note Phase C steps C1/C1a/C2/C3 — Resolve-phase wiring. Modified: `src/match-engine/MatchEngine.cs` v1.4 (collision + per-agent executor lifecycle in RunResolvePhase; PassWorldAdapter/ShotWorldAdapter nested adapters; BuildPass*/BuildShot* world-state mappers; possession field + TestOnly_ seams), `src/match-engine/MatchEngineConstants.cs` v1.4 (NO_POSSESSION + STAGE0_NEUTRAL_* GT region), `src/match-engine/match-engine.asmdef` (+CollisionSystem/PassMechanics/ShotMechanics), `src/match-engine/tests/match-engine-tests.asmdef` (+PassMechanics/ShotMechanics). New: `src/match-engine/tests/MatchEngineResolveTests.cs` (collision separation in Resolve, same-seed determinism with a live collision, scripted pass/shot initiation through the adapters). `docs/tracking/match-engine-design.md` v0.9.2 (C1–C3 marked implemented; C4 absorbs the registry boot + possession-flip test). C4–C6 pending.) Prior June 19, 2026 (Match Engine design note Phase C step C0 — executor snapshot get/restore seams. New source files: `src/pass-mechanics/PassExecutorState.cs` (PassExecutor cross-tick state DTO; PhysicalProfile excluded — recomputed on restore) + `src/shot-mechanics/ShotExecutorState.cs` (ShotExecutor cross-tick state DTO). Modified: `src/pass-mechanics/PassExecutor.cs` v1.14 + `src/shot-mechanics/ShotExecutor.cs` v1.9 (CaptureState/RestoreState seams, parallel to the B0 OscillationGuard seam). New test files: `src/pass-mechanics/Tests/PassExecutorStateTests.cs` + `src/shot-mechanics/Tests/ShotExecutorStateTests.cs` (CanonicalSerializer round-trip + Capture/Restore identity locks). Test asmdefs `pass-mechanics-tests` / `shot-mechanics-tests` gain the `TacticalDirector.DeterministicSim` reference (CanonicalSerializer). `docs/tracking/match-engine-design.md` v0.9.1 (C0 marked implemented). C1–C6 pending.) Prior June 11, 2026, later same day (Decision Tree #8 comprehensive audit (AR-2): 3H+11M+9L. New files: `docs/specs/decision-tree/audit-report.md` (canonical audit deliverable), `src/decision-tree/Tests/DecisionContextAssemblerTests.cs` (H-2/M-1 locks). Modified: 18 decision-tree source files + 4 test files (see audit-report.md Files-changed list), `src/agent-movement/MovementCommand.cs` v1.4 (PressSprint + SprintWhileWatching factories), 5 asmdefs (DeterministicSim reference: decision-tree, pass-mechanics, perception-system, heading-mechanics, goalkeeper-mechanics), 7 decision-tree spec section files (ERR-008-002..011 patches), spec-error-log.md v1.26.) Prior June 11, 2026 (Pass Mechanics #5 AR-9 fix pass (1H+3M+5L) + AR-10 sweep (2L, same commit). No files added or removed — H-1 repaired the never-compiling `src/pass-mechanics/Tests/PassMechanicsTests.cs` (namespace closed before the v1.1 IT- fixture; stray `}` at EOF) and added the PassExecutorGuardTests fixture (PX-001..004) inside it; source fixes in PassExecutor.cs v1.12, PassErrorCalculator.cs v1.8, PassTargetResolver.cs v1.8, PhysicalProfile.cs v1.2, PassAgentAttributes.cs v1.1, PassAgentState.cs v1.1, PassOutcome.cs v1.3, tests v1.2.) Prior June 10, 2026, still later same day (Scenario-corpus expansion onto the #19 ScenarioRunner. New Spec #1 per-spec corpus: `src/ball-physics/tests/BallPhysicsScenarios.cs` (drop-and-rebound — AR-7 H-1 / ERR-001-001 lock; fast-descent-grounds-out — AR-7 H-2 hover-deadlock lock; envelope windows derived from a numerical mirror of the fixed model) + `src/ball-physics/tests/BallPhysicsScenarioTests.cs` (sim_<scenario> Simulation-layer tests); `ball-physics-tests.asmdef` gains the testing-strategy reference. First cross-spec corpus (KD-8, paths under `tests/scenarios/cross-spec/`): `src/testing-strategy/Tests/CrossSpecScenarios.cs` (lofted-pass-kick-bounce-roll — real PassExecutor (#5) kicks a real BallState through the real BallPhysicsCore loop (#1) via the IPassBallSystem seam, with #17 boot wiring + tick lifecycle around the CONTACT publish; owning specs {1, 5}) + `CrossSpecScenarioTests.cs`; `testing-strategy-tests.asmdef` gains ball-physics / pass-mechanics / event-system references. Manifest reconciliation: the three June-7 ball-physics test files (EnumOrdinalStabilityTests, BodyPartCoefficientsTests, SurfacePropertiesTests) were recorded only in this header note and never added to the Spec #1 per-file table — rows added now. Prior June 10, 2026, still later same day (Spec #19 ScenarioRunner AR-2 sweep: 0H+0M+2L, both resolved — ScenarioEnvelope.cs v1.2 (NaN in_range bounds throw as authoring error; min>max exception message InvariantCulture); ScenarioRunnerTests.cs v1.2 (18→19 tests). Prior June 10, 2026, later same day (Spec #19 ScenarioRunner AR-1 fix pass: 0H+4M+6L, all resolved. New file: `src/testing-strategy/ScenarioIndexEntry.cs` (extracted from ScenarioIndex.cs per FILE NAMING precedent + AR-1 M-1 manifest-coherence guard). Modified: ScenarioRunner.cs v1.1 (M-2 fixture_refs refusal, M-4 path↔name + cross-spec arity, L-6 format-version-first), ScenarioIndex.cs v1.1 (M-4 duplicate-name rejection), ScenarioEnvelope.cs v1.1 + ClosedLoopScenario.cs v1.1 (M-3 CR/LF sanitization + exception_stack line), ScenarioManifest.cs v1.1 (L-1 ReadOnlyCollection wrappers), ScenarioResult.cs / IScenario.cs / ScenarioContext.cs v1.1 (doc), TestingStrategyConstants.cs v1.4 (SCENARIO_PATH_CROSS_SPEC_PREFIX), ScenarioRunnerTests.cs v1.1 (12→18 tests), AgentMovementScenarios.cs v1.1 (L-2 InvariantCulture details, L-3 exact position equality for T-AM-115). Prior June 10, 2026 (Stage 0 closed-loop scenario harness landed — Spec #19 §3.3.3 ScenarioRunner pulled forward from the Stage 0+1 schedule after the third consecutive spec (Ball Physics AR-7, Agent Movement AR-12/AR-13) where H/M-class closed-loop defects were encoded by pure-function unit suites rather than caught by them. New `src/testing-strategy/` files (9 .cs): ScenarioStatus, ScenarioResult, ScenarioManifest, ScenarioEnvelope, ScenarioContext, IScenario, ClosedLoopScenario, ScenarioIndex, ScenarioRunner; new `src/testing-strategy/Tests/` (testing-strategy-tests.asmdef + ScenarioRunnerTests.cs, 12 contract tests); TestingStrategyConstants.cs v1.3 adds SCENARIO_MANIFEST_FORMAT_VERSION. First fixture corpus: T-AM-110..115 migrated out of AgentMovementTests.cs (v2.3) into AgentMovementScenarios.cs (bodies + A.1 manifests) + AgentMovementScenarioTests.cs (`sim_<scenario>` Simulation-layer tests); agent-movement-tests.asmdef gains the testing-strategy reference; docs/specs/agent-movement/test-plan.md v0.4. A `src/testing-strategy/` per-file section was added to this manifest (the June 7 scaffold had been recorded only in this header note). Prior June 9, 2026 (Ball Physics #1 AR-7/AR-8 fix pass: BallGroundInteraction.cs v1.3.1, BallPhysicsCore.cs v1.4, BallPhysicsConstants.cs v1.8, tests/BallPhysicsCoreTests.cs v1.5, tests/BallIntegrationTests.cs v1.4 (new Airborne_FastDescent_Bounces_NoHoverDeadlock test); docs/specs/ball-physics/section-3-1-8-to-3-1-14.md row 2.8; spec-error-log.md v1.23 (ERR-001-001..003). No files added or removed. Prior June 8, 2026 (Event System #17 boot-wiring smoke test landed: new `src/event-system/tests/EventBusWiringSmokeTests.cs` v0.4 — SMOKE-EVT-WIRING-001 drives boot → publish-one-per-spec → DrainTick → SerializeLedger and asserts SHA-256 digest stability across the 6 currently-wired EventBusRegistrar.Initialize() call sites (Pass / Shot / Perception / Decision / Heading / Goalkeeper); Agent Movement (#2) is a `[CROSS-PENDING]` slot pending the AM-side registrar; golden digest pinned via Assert.Inconclusive until that lands. AR-1 (1H+1M+4L) + AR-2 (0H+1M+4L) + AR-3 (0H+0M+2L cycle-stop) adversarial review cycles complete. `event-system-tests.asmdef` extended with 6 production spec references for the smoke test (Editor-only, no production layering impact — test assemblies are infrastructure per src/CLAUDE.md). Prior June 7, 2026 (AR-hardening sweep complete + test scaffolding landed. New source files: `src/agent-movement/AssemblyInfo.cs` (InternalsVisibleTo for tooling-override factory access). New test files: `src/ball-physics/tests/EnumOrdinalStabilityTests.cs` (AR-6 L-3 — locks int ordinals for all 6 public enums), `src/ball-physics/tests/BodyPartCoefficientsTests.cs` (AR-4 L-2 — throw-on-unknown + catalogue round-trip), `src/ball-physics/tests/SurfacePropertiesTests.cs` (AR-4 L-2 — throw-on-unknown across all 4 Get* methods), `src/agent-movement/Tests/AgentMovementTests.cs` v2.0 (T-AM-001..018, T-AM-030..033, T-AM-040..043 — 18 NUnit tests across 4 fixtures), `src/agent-movement/Tests/AgentMovementUnitTests.cs` (T-AM-007..107 — 59 NUnit tests across 7 fixtures). New tracking doc: `docs/specs/agent-movement/test-plan.md` v0.2 (T-AM-NNN catalogue). File splits: Ball Physics — `BallCollision.cs` split into `BodyPart.cs` + `RestartType.cs` + `KickResult.cs` + `BodyPartCoefficients.cs` + `BallCollision.cs` per FILE NAMING (AR-2 L-2). Performance Optimization — `TraceChannel.cs` split into `ChannelVerbosity.cs` + `ChannelSamplingRule.cs` + `ChannelDeterminismClass.cs` + `TraceChannelDescriptor.cs` + `TraceChannelRegistry.cs` (AR-1 H-1). New Testing Strategy assembly: `src/testing-strategy/` with 14 files (TestingStrategyConstants, TestTier, TestLayer, GoldenVectorKind/Entry/Result/Runner, DeterminismTierKind/Result, DeterminismSuiteResult, DeterminismGate, PerfGateReport, PerfGateRunner, testing-strategy.asmdef). Prior May 31: AR-4 fixes in event-system.))
**Last Updated:** June 22, 2026, later same day (Match Engine design note Phase C steps C4/C5/C6 — Resolve-phase completion. Modified: `src/match-engine/MatchEngine.cs` v1.5 (MatchContext authored each Resolve via UpdateMatchContext; Pass/Shot EventBusRegistrar boot; C5 SerializeWorldState adds per-agent executor C0 capture + MatchContext; WritePassExecutorState/WriteShotExecutorState/WriteMatchContext helpers; TestOnly_MatchContext seam), `src/match-engine/MatchEngineConstants.cs` v1.5 (SNAPSHOT_SCHEMA_VERSION 1 → 2 + v1/v2 doc), `src/match-engine/match-engine.asmdef` + `src/match-engine/tests/match-engine-tests.asmdef` (+DecisionTree), `src/match-engine/tests/MatchEngineSnapshotSchemaTests.cs` (pin 1 → 2). New: `src/match-engine/tests/MatchEngineMatchContextTests.cs` (ball-zone authoring, possession derivation, scripted pass reaches CONTACT + releases possession, same-seed determinism with a live CONTACT publish, C5 digest-preimage probes). `docs/tracking/match-engine-design.md` v0.9.4 (C4–C6 implemented, Phase C complete). Match-engine manifest section reconciled — missing MatchEngineResolveTests.cs row added; header/refs/MatchEngine.cs row refreshed for C0–C6. Phase D D1 unblocked.) Prior June 22, 2026 (Match Engine design note Phase D step D0 — DecisionTree snapshot get/restore seam, the gating sub-step. New source file: `src/decision-tree/DecisionTreeState.cs` (cross-tick state-machine DTO: DtState ordinal + last AgentAction + _hasDispatchedAction; _matchSeed/_optionBuffer excluded per §2.6). Modified: `src/decision-tree/DecisionTree.cs` v1.2 (CaptureState/RestoreState seams, parallel to the Pass/Shot executor C0 seams), `src/decision-tree/Tests/decision-tree-tests.asmdef` (+`TacticalDirector.DeterministicSim` for CanonicalSerializer). New test file: `src/decision-tree/Tests/DecisionTreeStateTests.cs` (CanonicalSerializer round-trip + Capture/Restore identity + fresh-IDLE default + reflection field-count guard). `docs/tracking/match-engine-design.md` v0.9.3 (D0 marked implemented; §5 Phase D expanded into ordered sub-steps D0–D5). Decision Tree section count 36 → 38 files. C4–C6 + D1–D5 + E–F pending.) Prior June 20, 2026 (Tactical Instructions #21 APPROVED — new spec folder `docs/specs/tactical-instructions/` (11 section files: outline + section-1..8 + section-9-approval-checklist + appendices, at v0.3/v0.4) + `adversarial-review-section-files-v1.md` (PASS-1) + `adversarial-review-section-files-v2.md` (PASS-2). **No `src/` source files** — the `src/tactical-instructions/` assembly (T0 scaffolding) is not yet authored, so no per-file source rows are added here; this manifest tracks `src/` inventory. `docs/tracking/tactical-instruction-layer-design.md` marked superseded by the spec. Prior June 11, 2026, later same day (Decision Tree #8 comprehensive audit (AR-2): 3H+11M+9L. New files: `docs/specs/decision-tree/audit-report.md` (canonical audit deliverable), `src/decision-tree/Tests/DecisionContextAssemblerTests.cs` (H-2/M-1 locks). Modified: 18 decision-tree source files + 4 test files (see audit-report.md Files-changed list), `src/agent-movement/MovementCommand.cs` v1.4 (PressSprint + SprintWhileWatching factories), 5 asmdefs (DeterministicSim reference: decision-tree, pass-mechanics, perception-system, heading-mechanics, goalkeeper-mechanics), 7 decision-tree spec section files (ERR-008-002..011 patches), spec-error-log.md v1.26.) Prior June 11, 2026 (Pass Mechanics #5 AR-9 fix pass (1H+3M+5L) + AR-10 sweep (2L, same commit). No files added or removed — H-1 repaired the never-compiling `src/pass-mechanics/Tests/PassMechanicsTests.cs` (namespace closed before the v1.1 IT- fixture; stray `}` at EOF) and added the PassExecutorGuardTests fixture (PX-001..004) inside it; source fixes in PassExecutor.cs v1.12, PassErrorCalculator.cs v1.8, PassTargetResolver.cs v1.8, PhysicalProfile.cs v1.2, PassAgentAttributes.cs v1.1, PassAgentState.cs v1.1, PassOutcome.cs v1.3, tests v1.2.) Prior June 10, 2026, still later same day (Scenario-corpus expansion onto the #19 ScenarioRunner. New Spec #1 per-spec corpus: `src/ball-physics/tests/BallPhysicsScenarios.cs` (drop-and-rebound — AR-7 H-1 / ERR-001-001 lock; fast-descent-grounds-out — AR-7 H-2 hover-deadlock lock; envelope windows derived from a numerical mirror of the fixed model) + `src/ball-physics/tests/BallPhysicsScenarioTests.cs` (sim_<scenario> Simulation-layer tests); `ball-physics-tests.asmdef` gains the testing-strategy reference. First cross-spec corpus (KD-8, paths under `tests/scenarios/cross-spec/`): `src/testing-strategy/Tests/CrossSpecScenarios.cs` (lofted-pass-kick-bounce-roll — real PassExecutor (#5) kicks a real BallState through the real BallPhysicsCore loop (#1) via the IPassBallSystem seam, with #17 boot wiring + tick lifecycle around the CONTACT publish; owning specs {1, 5}) + `CrossSpecScenarioTests.cs`; `testing-strategy-tests.asmdef` gains ball-physics / pass-mechanics / event-system references. Manifest reconciliation: the three June-7 ball-physics test files (EnumOrdinalStabilityTests, BodyPartCoefficientsTests, SurfacePropertiesTests) were recorded only in this header note and never added to the Spec #1 per-file table — rows added now. Prior June 10, 2026, still later same day (Spec #19 ScenarioRunner AR-2 sweep: 0H+0M+2L, both resolved — ScenarioEnvelope.cs v1.2 (NaN in_range bounds throw as authoring error; min>max exception message InvariantCulture); ScenarioRunnerTests.cs v1.2 (18→19 tests). Prior June 10, 2026, later same day (Spec #19 ScenarioRunner AR-1 fix pass: 0H+4M+6L, all resolved. New file: `src/testing-strategy/ScenarioIndexEntry.cs` (extracted from ScenarioIndex.cs per FILE NAMING precedent + AR-1 M-1 manifest-coherence guard). Modified: ScenarioRunner.cs v1.1 (M-2 fixture_refs refusal, M-4 path↔name + cross-spec arity, L-6 format-version-first), ScenarioIndex.cs v1.1 (M-4 duplicate-name rejection), ScenarioEnvelope.cs v1.1 + ClosedLoopScenario.cs v1.1 (M-3 CR/LF sanitization + exception_stack line), ScenarioManifest.cs v1.1 (L-1 ReadOnlyCollection wrappers), ScenarioResult.cs / IScenario.cs / ScenarioContext.cs v1.1 (doc), TestingStrategyConstants.cs v1.4 (SCENARIO_PATH_CROSS_SPEC_PREFIX), ScenarioRunnerTests.cs v1.1 (12→18 tests), AgentMovementScenarios.cs v1.1 (L-2 InvariantCulture details, L-3 exact position equality for T-AM-115). Prior June 10, 2026 (Stage 0 closed-loop scenario harness landed — Spec #19 §3.3.3 ScenarioRunner pulled forward from the Stage 0+1 schedule after the third consecutive spec (Ball Physics AR-7, Agent Movement AR-12/AR-13) where H/M-class closed-loop defects were encoded by pure-function unit suites rather than caught by them. New `src/testing-strategy/` files (9 .cs): ScenarioStatus, ScenarioResult, ScenarioManifest, ScenarioEnvelope, ScenarioContext, IScenario, ClosedLoopScenario, ScenarioIndex, ScenarioRunner; new `src/testing-strategy/Tests/` (testing-strategy-tests.asmdef + ScenarioRunnerTests.cs, 12 contract tests); TestingStrategyConstants.cs v1.3 adds SCENARIO_MANIFEST_FORMAT_VERSION. First fixture corpus: T-AM-110..115 migrated out of AgentMovementTests.cs (v2.3) into AgentMovementScenarios.cs (bodies + A.1 manifests) + AgentMovementScenarioTests.cs (`sim_<scenario>` Simulation-layer tests); agent-movement-tests.asmdef gains the testing-strategy reference; docs/specs/agent-movement/test-plan.md v0.4. A `src/testing-strategy/` per-file section was added to this manifest (the June 7 scaffold had been recorded only in this header note). Prior June 9, 2026 (Ball Physics #1 AR-7/AR-8 fix pass: BallGroundInteraction.cs v1.3.1, BallPhysicsCore.cs v1.4, BallPhysicsConstants.cs v1.8, tests/BallPhysicsCoreTests.cs v1.5, tests/BallIntegrationTests.cs v1.4 (new Airborne_FastDescent_Bounces_NoHoverDeadlock test); docs/specs/ball-physics/section-3-1-8-to-3-1-14.md row 2.8; spec-error-log.md v1.23 (ERR-001-001..003). No files added or removed. Prior June 8, 2026 (Event System #17 boot-wiring smoke test landed: new `src/event-system/tests/EventBusWiringSmokeTests.cs` v0.4 — SMOKE-EVT-WIRING-001 drives boot → publish-one-per-spec → DrainTick → SerializeLedger and asserts SHA-256 digest stability across the 6 currently-wired EventBusRegistrar.Initialize() call sites (Pass / Shot / Perception / Decision / Heading / Goalkeeper); Agent Movement (#2) is a `[CROSS-PENDING]` slot pending the AM-side registrar; golden digest pinned via Assert.Inconclusive until that lands. AR-1 (1H+1M+4L) + AR-2 (0H+1M+4L) + AR-3 (0H+0M+2L cycle-stop) adversarial review cycles complete. `event-system-tests.asmdef` extended with 6 production spec references for the smoke test (Editor-only, no production layering impact — test assemblies are infrastructure per src/CLAUDE.md). Prior June 7, 2026 (AR-hardening sweep complete + test scaffolding landed. New source files: `src/agent-movement/AssemblyInfo.cs` (InternalsVisibleTo for tooling-override factory access). New test files: `src/ball-physics/tests/EnumOrdinalStabilityTests.cs` (AR-6 L-3 — locks int ordinals for all 6 public enums), `src/ball-physics/tests/BodyPartCoefficientsTests.cs` (AR-4 L-2 — throw-on-unknown + catalogue round-trip), `src/ball-physics/tests/SurfacePropertiesTests.cs` (AR-4 L-2 — throw-on-unknown across all 4 Get* methods), `src/agent-movement/Tests/AgentMovementTests.cs` v2.0 (T-AM-001..018, T-AM-030..033, T-AM-040..043 — 18 NUnit tests across 4 fixtures), `src/agent-movement/Tests/AgentMovementUnitTests.cs` (T-AM-007..107 — 59 NUnit tests across 7 fixtures). New tracking doc: `docs/specs/agent-movement/test-plan.md` v0.2 (T-AM-NNN catalogue). File splits: Ball Physics — `BallCollision.cs` split into `BodyPart.cs` + `RestartType.cs` + `KickResult.cs` + `BodyPartCoefficients.cs` + `BallCollision.cs` per FILE NAMING (AR-2 L-2). Performance Optimization — `TraceChannel.cs` split into `ChannelVerbosity.cs` + `ChannelSamplingRule.cs` + `ChannelDeterminismClass.cs` + `TraceChannelDescriptor.cs` + `TraceChannelRegistry.cs` (AR-1 H-1). New Testing Strategy assembly: `src/testing-strategy/` with 14 files (TestingStrategyConstants, TestTier, TestLayer, GoldenVectorKind/Entry/Result/Runner, DeterminismTierKind/Result, DeterminismSuiteResult, DeterminismGate, PerfGateReport, PerfGateRunner, testing-strategy.asmdef). Prior May 31: AR-4 fixes in event-system.))
**Purpose:** Canonical inventory aligned with the current folder-based spec layout in `docs/specs/`.

---

## Scope and Authority

This manifest supersedes the legacy flat-file inventory that referenced historical filenames such as `*_v1_0.md`.

- **Canonical spec status source:** `docs/specs/SPEC_INDEX.md`
- **Canonical schedule source:** `docs/tracking/PROGRESS.md`
- **Canonical cross-spec issue source:** `docs/tracking/spec-error-log.md`

Use this file to track the **current folder structure**, not legacy per-version filenames.

---

## Source Files

| File | Purpose |
|------|---------|
| `src/CLAUDE.md` | Coding guide: C# naming, constant catalogues, Unity project structure, build/test commands. Created May 19, 2026 when coding began. At v1.23 as of May 30, 2026. |

### Spec #1 — Ball Physics (`src/ball-physics/`)

> Relocated May 30, 2026 from `src/Core/Physics/Ball/` to spec-canonical `src/ball-physics/` via `git mv`; history preserved. Two asmdef files added.

| File | Purpose |
|------|---------|
| `src/ball-physics/ball-physics.asmdef` | Assembly definition (references TacticalDirector.DeterministicSim; autoReferenced true) |
| `src/ball-physics/BallPhysicsConstants.cs` | `[FIXED]` / `[GT]` / `[DERIVED]` / `[CROSS]` constant catalogue for Ball Physics |
| `src/ball-physics/BallState.cs` | Mutable value-type game state for the ball (position, velocity, spin, ground contact) |
| `src/ball-physics/BallPhysicsCore.cs` | Core physics calculations: gravity, drag, Magnus effect |
| `src/ball-physics/BallStateMachine.cs` | State machine: CONTROLLED ↔ AIRBORNE ↔ ROLLING transitions |
| `src/ball-physics/BallGroundInteraction.cs` | Ground friction and rolling dynamics |
| `src/ball-physics/BallCollision.cs` | Ball-specific collision response (detection geometry lives in `collision-system/`) |
| `src/ball-physics/BallEventLogger.cs` | Event/logging infrastructure for ball physics events |
| `src/ball-physics/SurfaceProperties.cs` | Surface-specific physics parameters (grass, artificial turf, etc.) |
| `src/ball-physics/tests/ball-physics-tests.asmdef` | Test assembly definition (EditMode; references ball-physics.asmdef + testing-strategy.asmdef; autoReferenced false) |
| `src/ball-physics/tests/BallPhysicsCoreTests.cs` | Unit tests for core physics calculations |
| `src/ball-physics/tests/BallIntegrationTests.cs` | Integration tests for full ball physics pipeline |
| `src/ball-physics/tests/BallStateMachineTests.cs` | Unit tests for ball state machine transitions |
| `src/ball-physics/tests/BodyPartCoefficientsTests.cs` | AR-4 L-2 — throw-on-unknown + catalogue round-trip for the per-body-part coefficients |
| `src/ball-physics/tests/SurfacePropertiesTests.cs` | AR-4 L-2 — throw-on-unknown + catalogue round-trip across all 4 SurfaceProperties Get* methods |
| `src/ball-physics/tests/EnumOrdinalStabilityTests.cs` | AR-6 L-3 — locks int ordinals for all 6 public ball-physics enums |
| `src/ball-physics/tests/BallPhysicsScenarios.cs` | Per-spec closed-loop scenario corpus (#19 §3.3.1): drop-and-rebound (AR-7 H-1 / ERR-001-001 lock) + fast-descent-grounds-out (AR-7 H-2 hover-deadlock lock); bodies + Appendix A.1 manifests, envelope windows from a numerical mirror of the fixed model |
| `src/ball-physics/tests/BallPhysicsScenarioTests.cs` | sim_<scenario> Simulation-layer tests running the Spec #1 corpus through the #19 ScenarioRunner |
| `src/ball-physics/tests/ShotOutcomeBallPhysicsTests.cs` | Shot-outcome pass unit locks (ERR-001-004 / ERR-003-007): `ApplyAgentDeflection` reflect + retention, separating/degenerate no-ops (the stateless self-block guard), and the Law 9/10 airborne boundary adjudication (goal under the bar, goal kick over it, throw-in in the air, interior height never out) |
| `src/ball-physics/tests/SweptGoalFrameTests.cs` | Shot-speed pass unit locks (ERR-001-005): swept post/crossbar strikes incl. the tunneling discriminator (a segment fully crossing a post in one tick), restitution/spin retention, mouth/over-frame non-clips, Controlled/degenerate/starts-inside gates, crossing-point adjudication both ends + the position-only overload's retained semantics |

### Spec #2 — Agent Movement (`src/agent-movement/`)

| File | Purpose |
|------|---------|
| `src/agent-movement/AgentMovementConstants.cs` | All movement constants: `MovementThresholds` / `FatigueRates` / `LocomotionConstants` / `DirectionalConstants` / `TurnConstants` / `OscillationGuardConstants` / `SafetyConstants` / `PlayerAttributeConstants` (8 nested classes) |
| `src/agent-movement/AgentMovementState.cs` | Enum: `AgentMovementState` (7 locomotion states) |
| `src/agent-movement/GroundedReason.cs` | Enum: `GroundedReason` (NONE / COLLISION / SLIDING_TACKLE / DIVING_HEADER) |
| `src/agent-movement/FacingMode.cs` | Enum: `FacingMode` (AUTO_ALIGN / TARGET_LOCK) |
| `src/agent-movement/DecelerationMode.cs` | Enum: `DecelerationMode` (CONTROLLED / EMERGENCY) |
| `src/agent-movement/AgentState.cs` | Mutable value-type game state for an agent (ref-mutated, not readonly — pending C# version pin) |
| `src/agent-movement/PlayerAttributes.cs` | Player skill ratings (1–20 scale) used by movement formulas |
| `src/agent-movement/PerformanceContext.cs` | Performance modifier gateway (fatigue, injury, surface) |
| `src/agent-movement/MovementCommand.cs` | Input command structure from Decision Tree / tactical layer |
| `src/agent-movement/AgentMovementSystem.cs` | 12-step 60 Hz pipeline orchestrator |
| `src/agent-movement/AgentStateMachine.cs` | Pure state evaluator (no side effects) |
| `src/agent-movement/OscillationGuard.cs` | Ring-buffer anti-oscillation guard; v1.5 adds the GetState/RestoreState serialization seam (Match Engine Phase B step B0) |
| `src/agent-movement/OscillationGuardState.cs` | Plain-data snapshot DTO of OscillationGuard's ring-buffer state (Phase B step B0 serialization seam; parallel to DeterministicSim RngStreamState) |
| `src/agent-movement/AgentLocomotion.cs` | Acceleration / deceleration formulas |
| `src/agent-movement/AgentTurning.cs` | Turn rate / lean angle / stumble probability |
| `src/agent-movement/AgentDirectionalMovement.cs` | Directional multipliers / facing update |
| `src/agent-movement/AgentSafetySystem.cs` | NaN detection / speed clamp / pitch boundary enforcement |
| `src/agent-movement/agent-movement.asmdef` | Assembly definition (no references; autoReferenced true) |
| `src/agent-movement/AssemblyInfo.cs` | `[InternalsVisibleTo("TacticalDirector.AgentMovement.Tests")]` (added June 4, 2026 for T-AM-030..032 seam) |
| `src/agent-movement/Tests/AgentMovementTests.cs` | v2.3 — Regression-anchored integration roster T-AM-001..018 / 030..033 / 040..043 (closed-loop fixture T-AM-110..115 migrated to the #19 scenario harness June 10, 2026) |
| `src/agent-movement/Tests/AgentMovementUnitTests.cs` | Pure-function unit coverage T-AM-007..009 / 019..023 / 034..039 / 044..047 / 050..052 / 070..109 (T-AM-108..109 added in AR-12 fix pass, June 9, 2026) |
| `src/agent-movement/Tests/AgentMovementScenarios.cs` | Per-spec closed-loop scenario corpus T-AM-110..115 (#19 §3.3.1): scenario bodies + Appendix A.1 manifests; first fixture corpus on the #19 ScenarioRunner (June 10, 2026) |
| `src/agent-movement/Tests/AgentMovementScenarioTests.cs` | Simulation-layer executable tests (`sim_<scenario>` per #19 §3.1.4) running the T-AM-110..115 corpus through ScenarioRunner.Run |
| `src/agent-movement/Tests/OscillationGuardSerializationTests.cs` | B0-001..004 — OscillationGuard GetState/RestoreState CanonicalSerializer round-trip + behavioural-equivalence locks (Match Engine Phase B step B0) |
| `src/agent-movement/Tests/agent-movement-tests.asmdef` | Test assembly definition (EditMode; references agent-movement.asmdef + testing-strategy.asmdef + deterministic-sim.asmdef for the B0 CanonicalSerializer round-trip) |

### Spec #3 — Collision System (`src/collision-system/`)

| File | Purpose |
|------|---------|
| `src/collision-system/CollisionSystemConstants.cs` | All constant catalogue: `[FIXED]` / `[GT]` / `[DERIVED]` / `[CROSS]` constants for the collision system |
| `src/collision-system/CollisionDetection.cs` | Broad-phase and narrow-phase collision detection logic |
| `src/collision-system/SpatialHashGrid.cs` | Spatial hash grid for broad-phase agent and ball proximity queries |
| `src/collision-system/CollisionManifold.cs` | Contact manifold: penetration depth, contact normal, contact point |
| `src/collision-system/CollisionEvent.cs` | Struct event published to the event bus on confirmed collision |
| `src/collision-system/CollisionResponse.cs` | Impulse-based collision response calculations |
| `src/collision-system/CollisionSystem.cs` | Main orchestrator: 60 Hz pipeline for all agent–agent and agent–ball collisions |
| `src/collision-system/CollisionPairBitfield.cs` | Bitfield tracking already-processed pairs within a tick (prevents double-processing) |
| `src/collision-system/AgentAgentCollisionResult.cs` | Result struct for an agent–agent collision resolution |
| `src/collision-system/AgentBallCollisionData.cs` | Data struct describing an agent–ball contact |
| `src/collision-system/AgentPhysicalProperties.cs` | Physical properties (mass, radius, restitution) per agent |
| `src/collision-system/BallCollisionHandler.cs` | Ball-specific collision response (delegates geometry to `ball-physics/BallCollision.cs`) |
| `src/collision-system/ContactForceData.cs` | Contact force magnitude and direction for logging/events |
| `src/collision-system/ContactType.cs` | Enum: contact classification (SLIDE, SHOULDER, BLOCK, FOUL) |
| `src/collision-system/ContactTypeClassifier.cs` | Classifies a collision manifold into a `ContactType` |
| `src/collision-system/CollisionType.cs` | Enum: collision kind (AGENT_AGENT, AGENT_BALL, AGENT_POST, AGENT_BOUNDARY) |
| `src/collision-system/DeterministicRNG.cs` | Thin wrapper around SplitMix64 for foul-roll and stumble-roll RNG draws |
| `src/collision-system/ICollisionEventConsumer.cs` | Interface for systems that consume collision events (Spec #3 §3.4.2 consumer pattern) |
| `src/collision-system/collision-system.asmdef` | Assembly definition for the collision-system assembly |
| `src/collision-system/tests/collision-system-tests.asmdef` | Test assembly definition (EditMode; references collision-system.asmdef) |
| `src/collision-system/tests/BallCollisionHandlerTests.cs` | ERR-003-007 detection-side gate locks: Controlled ball never deflects, sub-gate-speed ball left to the control model, fast approaching ball deflects |

### Spec #4 — First Touch Mechanics (`src/first-touch/`)

| File | Purpose |
|------|---------|
| `src/first-touch/FirstTouchConstants.cs` | All constant catalogue for First Touch |
| `src/first-touch/FirstTouchContext.cs` | Input context struct: incoming ball state, agent state, env conditions |
| `src/first-touch/FirstTouchResult.cs` | Final output struct: displaced ball state + possession outcome |
| `src/first-touch/TouchResult.cs` | Intermediate result of touch quality evaluation |
| `src/first-touch/FirstTouchSystem.cs` | Main orchestrator: entry point for a first-touch resolution |
| `src/first-touch/BallDisplacementProcessor.cs` | Computes post-touch ball displacement vector from error and control quality |
| `src/first-touch/ControlQualityCalculator.cs` | Calculates control quality score (0–1) from player attributes and context |
| `src/first-touch/OrientationDetector.cs` | Detects player body orientation relative to the incoming ball direction |
| `src/first-touch/PossessionStateMachine.cs` | Manages possession state transitions (LOOSE → CONTROLLED → POSSESSED) |
| `src/first-touch/PressureEvaluator.cs` | Evaluates nearby-defender pressure scalar from agent positions |
| `src/first-touch/PressureResult.cs` | Pressure evaluation output struct |
| `src/first-touch/TouchRadiusCalculator.cs` | Computes the acceptance radius for a first-touch attempt |
| `src/first-touch/IAgentMovementSystem.cs` | Interface boundary to Agent Movement (#2) (read-only query surface) |
| `src/first-touch/IBallPhysicsSystem.cs` | Interface boundary to Ball Physics (#1) (read-only query surface) |
| `src/first-touch/IFirstTouchSystem.cs` | Public interface for consumers of the First Touch system |
| `src/first-touch/first-touch.asmdef` | Assembly definition (references BallPhysics, AgentMovement, CollisionSystem) |
| `src/first-touch/Tests/first-touch-tests.asmdef` | EditMode test assembly definition |
| `src/first-touch/Tests/FirstTouchTests.cs` | CQ/TR/PR/OR/PO/EC/BD/VS + invariant suite + IT-001..008 stubs (v1.2 AR-7 re-derivation) |
| `src/first-touch/AssemblyInfo.cs` | [InternalsVisibleTo("TacticalDirector.FirstTouch.Tests")] for the scenario corpus |
| `src/first-touch/Tests/FirstTouchScenarios.cs` | Closed-loop scenario corpus on the #19 ScenarioRunner: heavy-touch-runs-on (ERR-004-003 lock) + interception-chain-anchors-at-displaced-ball (ERR-004-004 / §3.4.5 lock) |
| `src/first-touch/Tests/FirstTouchScenarioTests.cs` | sim_<scenario> Simulation-layer tests running the corpus through ScenarioRunner |

### Spec #5 — Pass Mechanics (`src/pass-mechanics/`)

| File | Purpose |
|------|---------|
| `src/pass-mechanics/PassMechanicsConstants.cs` | All constant catalogue: physical profiles, error model, timing constants |
| `src/pass-mechanics/PassRequest.cs` | Input struct: passer, target agent/position, requested pass type |
| `src/pass-mechanics/PassResult.cs` | Output struct: actual ball velocity applied + outcome classification |
| `src/pass-mechanics/PassAttemptEvent.cs` | Tier A struct event (IEventA; ordinal 0x0C; 12-byte header): published when a pass is initiated |
| `src/pass-mechanics/PassCancelledEvent.cs` | Tier A struct event (IEventA; ordinal 0x0D; 12-byte header): published when a pass is cancelled |
| `src/pass-mechanics/CancelReason.cs` | Enum: TackleInterrupt — reason a pass was cancelled |
| `src/pass-mechanics/PassExecutor.cs` | Main orchestrator: executes the full pass pipeline |
| `src/pass-mechanics/PassVelocityCalculator.cs` | Calculates launch velocity from physical profile and player attributes |
| `src/pass-mechanics/PassErrorCalculator.cs` | Error / accuracy model: direction and speed deviation |
| `src/pass-mechanics/PassTargetResolver.cs` | Resolves intended target position from agent reference |
| `src/pass-mechanics/PassTypeProfiles.cs` | Factory: returns the `PhysicalProfile` for a given `PassType` |
| `src/pass-mechanics/PhysicalProfile.cs` | Struct: physical parameters (speed range, spin, launch angle) per pass type |
| `src/pass-mechanics/PassExecutorState.cs` | Plain-data snapshot DTO of PassExecutor's cross-tick state-machine + in-flight fields (Match Engine Phase C C0 seam; PhysicalProfile recomputed on restore) |
| `src/pass-mechanics/PassType.cs` | Enum: GROUND, DRIVEN, LOB, CHIP, CROSS |
| `src/pass-mechanics/CrossSubType.cs` | Enum: cross sub-type (LOW, DRIVEN, FLOATED, CUTBACK) |
| `src/pass-mechanics/SpinType.cs` | Enum: spin type (TOPSPIN, BACKSPIN, SIDESPIN, NONE) |
| `src/pass-mechanics/PassOutcome.cs` | Enum/struct: outcome classification (ACCURATE, MISPLACED, INTERCEPTED, OUT) |
| `src/pass-mechanics/PassAgentAttributes.cs` | Agent skill attributes consumed by the pass system (passing, vision, weak-foot) |
| `src/pass-mechanics/PassAgentState.cs` | Agent state consumed by the pass system (fatigue, stamina, body orientation) |
| `src/pass-mechanics/IPassAgentQuery.cs` | Interface to query agent attributes and state |
| `src/pass-mechanics/IPassBallSystem.cs` | Interface to Ball Physics (#1) for applying kick velocity |
| `src/pass-mechanics/IPassCollisionQuery.cs` | Interface to Collision System (#3) for interception queries |
| `src/pass-mechanics/EventBusStub.cs` | Wired to EventBus.Publish; 3-tier generic IEventA/B/C overloads |
| `src/pass-mechanics/EventBusRegistrar.cs` | Boot-time RegisterExternalRow<T>() for PassAttemptEvent (0x0C) + PassCancelledEvent (0x0D) |

### Spec #6 — Shot Mechanics (`src/shot-mechanics/`)

| File | Purpose |
|------|---------|
| `src/shot-mechanics/ShotMechanicsConstants.cs` | All GT/Fixed/Cross constants (velocity, angle, spin, error, body mechanics, weak-foot, timing) |
| `src/shot-mechanics/ContactZone.cs` | Enum: Centre / BelowCentre / OffCentre — where on the ball the foot contacts |
| `src/shot-mechanics/ShotOutcome.cs` | Enum: Completed / Cancelled / Invalid / Initiated |
| `src/shot-mechanics/ShotCancelReason.cs` | Enum: TackleInterrupt — reason a shot was cancelled |
| `src/shot-mechanics/ShotRequest.cs` | Input struct from Decision Tree (#8) to ShotExecutor |
| `src/shot-mechanics/ShotResult.cs` | Output struct returned by ShotExecutor (velocity, spin, error offset, BMS, outcome) |
| `src/shot-mechanics/ShotExecutorState.cs` | Plain-data snapshot DTO of ShotExecutor's cross-tick state-machine + in-flight fields (Match Engine Phase C C0 seam) |
| `src/shot-mechanics/ShotAgentAttributes.cs` | Agent attribute snapshot (Finishing, LongShots, Composure, KickPower, Technique, WeakFootRating, Fatigue) |
| `src/shot-mechanics/ShotAgentState.cs` | Agent physical state snapshot (Position, Velocity, FacingDirection, CurrentState) |
| `src/shot-mechanics/ShotExecutedEvent.cs` | Tier A struct event (IEventA; ordinal 0x01; 12-byte header): published at CONTACT completion after Ball.ApplyKick() |
| `src/shot-mechanics/ShotCancelledEvent.cs` | Tier A struct event (IEventA; ordinal 0x0E; 12-byte header): published when a tackle interrupt fires during WINDUP |
| `src/shot-mechanics/ShotAnimationData.cs` | Tier C struct event (IEventC; ordinal 0x0F): animation data stub for Animation System (unconsumed at Stage 0) |
| `src/shot-mechanics/BodyMechanicsResult.cs` | Output struct from BodyMechanicsEvaluator (Score, CQM, StumbleTriggered) |
| `src/shot-mechanics/GoalGeometry.cs` | Value struct: goal width, height, goal-line X, post Y coords, crossbar Z |
| `src/shot-mechanics/IShotVelocityCalculator.cs` | Interface enabling EC-008 NaN injection seam only (ShotVelocityCalculator + NaNVelocityStub) |
| `src/shot-mechanics/IShotBallSystem.cs` | Interface: IsBallPossessedBy() + ApplyKick() to Ball Physics |
| `src/shot-mechanics/IShotAgentQuery.cs` | Interface: GetAttributes() + GetState() to Agent Movement |
| `src/shot-mechanics/IShotCollisionQuery.cs` | Interface: tackle flag poll + pressure scalar to Collision System |
| `src/shot-mechanics/GoalGeometryProvider.cs` | Static access point for goal geometry; test override seam for SP-009 |
| `src/shot-mechanics/ShotVelocityCalculator.cs` | §3.2 velocity formula; stateless singleton; implements IShotVelocityCalculator |
| `src/shot-mechanics/ShotLaunchAngleCalculator.cs` | §3.3 launch angle formula (base angle + power/spin lift + body lean + body shape); pure static |
| `src/shot-mechanics/ShotSpinCalculator.cs` | §3.4 spin vector assembly (topspin / backspin / sidespin); pure static |
| `src/shot-mechanics/ShotPlacementResolver.cs` | §3.5 goal-relative placement → world-space aim direction; also applies error offset |
| `src/shot-mechanics/BodyMechanicsEvaluator.cs` | §3.7 body mechanics score (run-up angle, plant foot, velocity, body lean); pure static |
| `src/shot-mechanics/WeakFootPenaltyApplier.cs` | §3.8 weak-foot error multiplier and velocity multiplier; pure static |
| `src/shot-mechanics/ShotErrorCalculator.cs` | §3.6 deterministic angular error (magnitude, direction hash, offset); pure static |
| `src/shot-mechanics/ShotEventEmitter.cs` | Publishes ShotExecutedEvent, ShotCancelledEvent, ShotAnimationData via EventBusStub |
| `src/shot-mechanics/EventBusStub.cs` | Wired to EventBus.Publish; 3-tier generic IEventA/B/C overloads |
| `src/shot-mechanics/EventBusRegistrar.cs` | Boot-time RegisterExternalRow<T>() for ShotExecutedEvent (0x01) + ShotCancelledEvent (0x0E) + ShotAnimationData (0x0F) |
| `src/shot-mechanics/ShotExecutor.cs` | Sealed orchestrator: 5-state machine (Idle→Windup→Contact→FollowThrough→Complete) |
| `src/shot-mechanics/Tests/NaNVelocityStub.cs` | #if UNITY_EDITOR\|\|DEVELOPMENT_BUILD; returns float.NaN for EC-008 FM-05 recovery test |
| `src/shot-mechanics/Tests/ShotPlacementResolverShotOutcomeTests.cs` | ERR-006-002/003 locks: §3.5.6 launch-tilt aim (v does not drive vertical), distance-scaled error cone (wide-of-post reachable from 20 m at 5°), vertical clamp preserves lofted launches |

### Spec #10 — Heading Mechanics (`src/heading-mechanics/`)

| File | Purpose |
|------|---------|
| `src/heading-mechanics/heading-mechanics.asmdef` | Assembly definition (references agent-movement, ball-physics, collision-system, event-system; added event-system ref May 30, 2026) |
| `src/heading-mechanics/HeadingMechanicsConstants.cs` | All GT/Fixed/Cross/Derived constants (§3.1); region order Fixed→Derived→Cross→GT |
| `src/heading-mechanics/ContactQualityLabel.cs` | Enum: Early / OnTime / Late — telemetry only; KD-2 |
| `src/heading-mechanics/MistimedDirection.cs` | Enum: None / Early / Late — eligibility output |
| `src/heading-mechanics/FailureCause.cs` | Enum: MistimedEarly / MistimedLate / PositionedPoorly / DisturbedInDuel |
| `src/heading-mechanics/SetPieceContext.cs` | Enum: OpenPlay / Corner / FreeKick — telemetry only |
| `src/heading-mechanics/HeadingAgentAttributes.cs` | Struct: Heading/Strength/Balance [1-20], Fatigue [0,1], TeamId |
| `src/heading-mechanics/HeaderIntent.cs` | Struct: PowerIntent/ContactPointIntent/TargetIntent/AttemptCommittedTick/SetPieceContext (locked at commit; KD-17) |
| `src/heading-mechanics/HeaderContactState.cs` | Struct: per-attempt mutable state (JumpStartFrame, quality, disturbance, etc.) |
| `src/heading-mechanics/EligibilityResult.cs` | Struct: IsEligible, PredictedContactFrame, IdealContactFrame, MistimedDirection |
| `src/heading-mechanics/HeaderExecutedEvent.cs` | Tier B struct event (IEventB; ordinal 0x12; 12-byte header): published on successful contact |
| `src/heading-mechanics/HeaderAttemptFailedEvent.cs` | Tier C struct event (IEventC; ordinal 0x13): published on failure; no ball-state modification |
| `src/heading-mechanics/ContestedDuelContext.cs` | Struct: DuelId, ParticipantCount, WinnerAgentId, BufferStartIndex |
| `src/heading-mechanics/IHeadingBallSystem.cs` | Interface: GetBallState + ApplyKick |
| `src/heading-mechanics/IHeadingRngService.cs` | Interface: NextFloat + NextGaussian |
| `src/heading-mechanics/HeadingRngServiceStub.cs` | Stage 0 SplitMix64 stub; replace at Stage 1 with #16 wiring |
| `src/heading-mechanics/EventBusStub.cs` | Wired to EventBus.Publish; 3-tier generic IEventA/B/C overloads |
| `src/heading-mechanics/EventBusRegistrar.cs` | Boot-time RegisterExternalRow<T>() for HeaderExecutedEvent (0x12 Tier B) + HeaderAttemptFailedEvent (0x13 Tier C) |
| `src/heading-mechanics/HeadingEligibility.cs` | Pure eligibility predicate (§3.2); no side effects |
| `src/heading-mechanics/HeadingJumpKinematics.cs` | FM-010-001 JumpReach + Stage 0 synthetic parabolic Z (KD-18) |
| `src/heading-mechanics/HeadingContactQuality.cs` | FM-010-002 contact-quality scalar (asymmetric timing + point error) |
| `src/heading-mechanics/HeadingPowerAngle.cs` | FM-010-003 outgoing speed + reflection geometry + own-goal flag (§3.8) |
| `src/heading-mechanics/HeadingSpinTransfer.cs` | FM-010-004 head angular-velocity derivation + outgoing spin (§3.6) |
| `src/heading-mechanics/HeadingDuelResolution.cs` | FM-010-005 duel scoring; ICollisionEventConsumer; pre-allocated buffers |
| `src/heading-mechanics/HeadingTelemetry.cs` | Stage 0 stub; emits §2.4 heading.* trace-pipeline channels at Stage 0+1 |
| `src/heading-mechanics/HeadingMechanics.cs` | 60 Hz orchestrator; two-pass per-frame loop (§4.6) |

### Spec #11 — Goalkeeper Mechanics (`src/goalkeeper-mechanics/`)

| File | Purpose |
|------|---------|
| `src/goalkeeper-mechanics/goalkeeper-mechanics.asmdef` | Assembly definition (references agent-movement, ball-physics, collision-system, event-system; added event-system ref May 30, 2026) |
| `src/goalkeeper-mechanics/GoalkeeperConstants.cs` | All GT/Fixed/Cross/Derived constants (§3.4); region order Fixed→Derived→Cross→GT; ~79 constants; 4 draw-site IDs |
| `src/goalkeeper-mechanics/GoalkeeperState.cs` | Enum: Resting/Set/Anticipate/Diving/Airborne/HandsOnBall/Recovering/Distributing/Rushing/OneOnOne/Smothered |
| `src/goalkeeper-mechanics/HandlingQualityLabel.cs` | Enum: Caught/Parried/Deflected/Spilled/Missed — telemetry only (KD-2) |
| `src/goalkeeper-mechanics/ReactionLabel.cs` | Enum: Reflexive/Standard/Sluggish — telemetry only (KD-2) |
| `src/goalkeeper-mechanics/FailureCause.cs` | Enum: MissedContact/MistimedDive/WrongDirection/OutOfReach/DisturbedInDuel |
| `src/goalkeeper-mechanics/ClaimType.cs` | Enum: Cross/Aerial/OneOnOne/ShotCatch — telemetry only |
| `src/goalkeeper-mechanics/RushPhase.cs` | Enum: Launched/InFlight/Reached/Aborted |
| `src/goalkeeper-mechanics/AbortReason.cs` | Enum: BallIntercepted/BallCleared/AttackerBeatGK |
| `src/goalkeeper-mechanics/BodyPartEnum.cs` | Enum: Hand/Head/Body/Foot — collision routing (KD-14) |
| `src/goalkeeper-mechanics/HandEnum.cs` | Enum: Left/Right/Either — anatomy lookup KD-1 carve-out (not physics input) |
| `src/goalkeeper-mechanics/DeliveryKind.cs` | Enum: Throw/Roll/Kick — kinematic profile lookup KD-1 carve-out (not physics input) |
| `src/goalkeeper-mechanics/SaveIntent.cs` | Struct: TargetHand/ClutchFirmness/DeflectionTarget/AttemptCommittedTick (from Decision Tree #8) |
| `src/goalkeeper-mechanics/ClaimIntent.cs` | Struct: TargetContactPoint/ClutchFirmness/AttemptCommittedTick |
| `src/goalkeeper-mechanics/DistributeIntent.cs` | Struct: DeliveryKind/TargetReceiverId/TargetPoint/PowerIntent/SpinIntent |
| `src/goalkeeper-mechanics/RushIntent.cs` | Struct: RushTarget/CommitmentLevel/AttemptCommittedTick |
| `src/goalkeeper-mechanics/GkContactState.cs` | Struct: per-attempt mutable state (PredictedContactFrame, ReactionWindowAchieved, HandlingQualityScalar, etc.) |
| `src/goalkeeper-mechanics/CrossClaimDuelContext.cs` | Struct: DuelId, ParticipantCount, WinnerAgentId, ContactBodyPart, BufferStartIndex |
| `src/goalkeeper-mechanics/GoalkeeperAgentAttributes.cs` | Struct: all GK attributes [1-20] + Fatigue [0,1] + normalised accessors |
| `src/goalkeeper-mechanics/GoalkeeperPositioningContract.cs` | Struct: KD-13 consumer contract — holds gkBaselineSlot + reactive-radius bounds logic |
| `src/goalkeeper-mechanics/SaveAttemptedEvent.cs` | Tier A struct event (IEventA; ordinal 0x14; 12-byte header): published on every save attempt; includes telemetry labels |
| `src/goalkeeper-mechanics/BallClaimedEvent.cs` | Tier A struct event (IEventA; ordinal 0x15; 12-byte header): published on Caught save; includes releaseTickEarliest (6-second rule) |
| `src/goalkeeper-mechanics/DistributionExecutedEvent.cs` | Tier A struct event (IEventA; ordinal 0x16; 12-byte header): published when distribution passIntent is emitted to Pass Mechanics #5. v1.2: AR-2 fix — int? TargetReceiverId → int (sentinel -1); nullable padding bytes are non-deterministic in null case. |
| `src/goalkeeper-mechanics/GoalkeeperRushEvent.cs` | Tier C struct event (IEventC; ordinal 0x17): published on rush launch, update, and abort |
| `src/goalkeeper-mechanics/IGoalkeeperBallSystem.cs` | Interface: GetBallState + ApplyKick + SetPossessor |
| `src/goalkeeper-mechanics/IGoalkeeperRngService.cs` | Interface: NextFloat + NextGaussian (4 registered draw sites) |
| `src/goalkeeper-mechanics/GoalkeeperStateMachine.cs` | Pure state evaluator: EvaluateTacticalTransition + EvaluatePhysicsTransition; no side effects |
| `src/goalkeeper-mechanics/GoalkeeperReactionPipeline.cs` | §3.2 formulas: ComputeShotDetectedTickMs / ComputeRequiredReactionMs / ComputeReactionWindowAchieved / ComputeReactionLabel; pure static |
| `src/goalkeeper-mechanics/GoalkeeperDiveKinematics.cs` | §3.3 Stage 0 synthetic dive trajectory: launch impulse, timing jitter, parabolic Z, reach envelope; pure static |
| `src/goalkeeper-mechanics/GoalkeeperHandlingQuality.cs` | §3.5 handling-quality scalar + band-to-action velocity helpers (parry/deflect/spill); pure static |
| `src/goalkeeper-mechanics/GoalkeeperCrossClaimDuel.cs` | §3.6 body-part determination + duel-score arithmetic + tiebreak; implements ICollisionEventConsumer |
| `src/goalkeeper-mechanics/GoalkeeperRushDispatch.cs` | §3.7 rush launch impulse + per-frame update; pure static |
| `src/goalkeeper-mechanics/GoalkeeperDistribution.cs` | §3.8 release-point geometry, windup duration, accuracy coefficient, F-05/F-09 target validation; pure static |
| `src/goalkeeper-mechanics/GoalkeeperTelemetry.cs` | Stage 0 stub; emits §2.4 gk.* trace-pipeline channels at Stage 0+1 (12 channels) |
| `src/goalkeeper-mechanics/EventBusStub.cs` | Wired to EventBus.Publish; 3-tier generic IEventA/B/C overloads |
| `src/goalkeeper-mechanics/EventBusRegistrar.cs` | Boot-time RegisterExternalRow<T>() for SaveAttemptedEvent (0x14) + BallClaimedEvent (0x15) + DistributionExecutedEvent (0x16) + GoalkeeperRushEvent (0x17) |
| `src/goalkeeper-mechanics/GoalkeeperMechanics.cs` | Main 10 Hz + 60 Hz orchestrator: state machine, dive kinematics, handling quality, cross-claim duels, rush, distribution; constructor-injected |

### Perception System (#7) — 14 files

| File | Role |
|------|------|
| `src/perception-system/perception-system.asmdef` | Assembly definition (AI layer; references AgentMovement, BallPhysics, CollisionSystem, FirstTouch, EventSystem; added event-system ref May 30, 2026) |
| `src/perception-system/PerceptionConstants.cs` | All GT/Fixed/Derived/Cross constants (§3.10): 18 spec constants + system-sizing constants |
| `src/perception-system/PerceptionAgentAttributes.cs` | Struct: Decisions, Anticipation, TeamId, IsHalfTurned snapshot (§4.2.2) |
| `src/perception-system/FilteredView.cs` | FilteredView, PerceptionDiagnostics, PerceivedAgent, ShoulderCheckAnimData, OcclusionDebugRecord, PerceivedAgentDebug struct definitions (§3.7) |
| `src/perception-system/PerceptionEvents.cs` | Tier C struct event PerceptionRefreshEvent (IEventC; ordinal 0x10) + RefreshTrigger enum (§4.6.3) |
| `src/perception-system/EventBusStub.cs` | Wired to EventBus.Publish; 3-tier generic IEventA/B/C overloads |
| `src/perception-system/EventBusRegistrar.cs` | Boot-time RegisterExternalRow<T>() for PerceptionRefreshEvent (0x10) |
| `src/perception-system/FovCalculator.cs` | FoV formula (§3.1) + angular candidacy test + blind-side and peripheral arc predicates; static, no side effects |
| `src/perception-system/OcclusionFilter.cs` | Shadow cone geometry (§3.2.3) + opponent occlusion test; Stage 0: opponents only (OQ-1); static, no side effects |
| `src/perception-system/PressureEvaluator.cs` | PressureScalar formula (§3.6); reused verbatim from First Touch #4 §3.5; static, no side effects |
| `src/perception-system/BallPerceptionEvaluator.cs` | Ball range/FoV/occlusion tests + BallStalenessFrames tracking (§3.5); no L_rec (OQ-2); static, no side effects. v1.2: AR-2 L-1 removed unused prevBallVisible parameter. |
| `src/perception-system/RecognitionLatencyTracker.cs` | Per-(observer,target) latency counters, L_rec formula, half-turn peripheral bonus, Wang/Jenkins deterministic hash (§3.3); INV-10 pre-allocated int[22×22] arrays. v1.2: AR-2 L-3 DeterministicHash literals cast to unchecked int for true 32-bit wrapping. |
| `src/perception-system/ShoulderCheckScheduler.cs` | Autonomous shoulder check scheduling, window management, blind-side entity L_rec (§3.4); INV-10 pre-allocated arrays |
| `src/perception-system/ViewBuilder.cs` | Pure field-assembly step: sets scalar/count fields on pre-allocated FilteredView + PerceptionDiagnostics without overwriting PerceivedAgent[] references (§3.7); static, no computation |
| `src/perception-system/RecognitionLatencyState.cs` | Readonly struct: D4 snapshot view over the recognition-latency tracker pair arrays (latency/confirmed/expiry); returned by RecognitionLatencyTracker.CaptureState |
| `src/perception-system/ShoulderCheckState.cs` | Readonly struct: D4 snapshot view over the shoulder-check scheduler per-agent arrays (next-check/window-expiry/active/anim) + per-pair blind-side arrays; returned by ShoulderCheckScheduler.CaptureState |
| `src/perception-system/PerceptionTickState.cs` | Readonly struct: D4 snapshot bundle (RecognitionLatencyState + ShoulderCheckState + per-agent ball-perception carry-over); returned by PerceptionSystem.CaptureState for the Match Engine snapshot layer |
| `src/perception-system/PerceptionSystem.cs` | 10Hz orchestrator; 7-step pipeline for all 22 agents; forced-refresh handler; zero heap allocation on hot path (§3.0–§3.8, §4.1, §4.6). v1.2: AR-2 L-1/L-2 — removed prevBallVisible argument; added length guards to HandleForcedRefresh; added agentHasPossession length guard. |

### Decision Tree (#8) — 38 files

| File | Description |
|------|-------------|
| `src/decision-tree/decision-tree.asmdef` | Assembly definition (AI layer; references agent-movement, perception-system, pass-mechanics, shot-mechanics, heading-mechanics, goalkeeper-mechanics, collision-system, event-system, deterministic-sim, tactical-instructions; June 11, 2026 audit added the deterministic-sim ref EventBusRegistrar requires — asmdef refs are not transitive; June 28, 2026 added tactical-instructions for the #21 T2 seam) |
| `src/decision-tree/AssemblyInfo.cs` | [assembly: InternalsVisibleTo("TacticalDirector.DecisionTree.Tests")] |
| `src/decision-tree/DecisionTree.cs` | Public sealed class: 6-step pipeline orchestrator + state machine (§3.6, §3.7, §4.1) |
| `src/decision-tree/DecisionTreeStateMachine.cs` | Pure state evaluator: IDLE/EVALUATING/EXECUTING/INTERRUPTED transitions (§3.7.2) |
| `src/decision-tree/SnapshotValidator.cs` | Step 1: validates FilteredView — phase gate, agent identity, ball state (§3.1.1) |
| `src/decision-tree/DecisionContextAssembler.cs` | Step 2: assembles DecisionContext from all pipeline inputs (§2.2.4, §3.1.1) |
| `src/decision-tree/OptionGenerator.cs` | Step 3: generates all eligible ActionOption candidates (§3.1) |
| `src/decision-tree/UtilityScorer.cs` | Step 4: scores ActionOptions with §3.2 formulas; #21 §3.2 Mentality risk mult + §3.3 per-agent PlayerTactic product applied per option before the clamp (identity ⇒ ×1.0); #23 §3.4 marked-pass-target multiplier on PASS options (target proximity to passer-perceived opponents × passer awareness; Off ⇒ exact ×1.0) |
| `src/decision-tree/TacticalModifierResolver.cs` | Step 4 helper: resolves tactical multipliers per action type (§3.4) |
| `src/decision-tree/TacticTranslation.cs` | #21 T2 consumer seam: TacticPressing/TacticPassing → #8 enums (rank-mapped, F5 clamp) + Mentality risk/line resolvers + §3.3 PlayerTacticActionMultiplier (per-agent role/duty/instr × team tempo product; identity ⇒ ×1.0) (#21 §3.1/§3.2/§3.3; pure, translate-once) |
| `src/decision-tree/ActionSelector.cs` | Step 5: composure noise injection + highest-EffectiveUtility winner (§3.3) |
| `src/decision-tree/ActionDispatcher.cs` | Step 6: routes selected action to movement controller or physics executor (§3.5) |
| `src/decision-tree/DecisionContext.cs` | Internal struct: all assembled pipeline inputs for one agent-tick (§2.2.4) |
| `src/decision-tree/ActionOption.cs` | Internal struct: one scored candidate (§3.1.0) |
| `src/decision-tree/AgentAction.cs` | Public readonly struct: pipeline output (type, target, params, utility) (§2.2.3) |
| `src/decision-tree/DecisionTreeState.cs` | Public readonly struct: Match Engine Phase D D0 snapshot DTO — cross-tick state machine (DtState ordinal + last AgentAction + _hasDispatchedAction); _matchSeed/_optionBuffer excluded (§2.6) |
| `src/decision-tree/DecisionMadeEvent.cs` | Tier C struct event (IEventC; ordinal 0x11): published after each decision (§2.2.7) |
| `src/decision-tree/DtAgentAttributes.cs` | Struct: all DT-consumed player attributes [1–20] + CreateDefault factory (§3.1) |
| `src/decision-tree/MatchContext.cs` | Struct: authoritative match state per heartbeat (§2.2.5) |
| `src/decision-tree/TacticalContext.cs` | Struct: pressing mode, passing style, formation slots, #21 Mentality + Tempo + per-agent PlayerTactic routing fields (Stage0Default seeds Balanced/Standard/identity); Stage0Default factory (§2.2.6) |
| `src/decision-tree/DecisionTreeConstants.cs` | Constants: capacity limits / timing budgets / pipeline invariants (§4.2, §3.7) |
| `src/decision-tree/UtilityWeights.cs` | Constants: all 58+ utility scoring constants (§3.2.11) |
| `src/decision-tree/ComposureWeights.cs` | Constants: NOISE_MAX / COMPOSURE_SUPPRESSION / TIEBREAK_EPSILON (§3.3.3–3.3.5) |
| `src/decision-tree/TacticalWeights.cs` | Constants: tactical multipliers for all action types (§3.4) |
| `src/decision-tree/PitchGeometry.cs` | Static helpers: field zone classification, goal post positions, centre (§3.1.1) |
| `src/decision-tree/IDtMovementController.cs` | Public interface: dispatch boundary to Agent Movement #2 (§3.5) |
| `src/decision-tree/EventBusStub.cs` | Wired to EventBus.Publish (internal; single-sig for DecisionMadeEvent) |
| `src/decision-tree/EventBusRegistrar.cs` | Boot-time RegisterExternalRow<T>() for DecisionMadeEvent (0x11) |
| `src/decision-tree/ActionType.cs` | Enum: PASS/SHOOT/DRIBBLE/HOLD/MOVE_TO_POSITION/PRESS/INTERCEPT |
| `src/decision-tree/DtState.cs` | Enum: IDLE/EVALUATING/EXECUTING/INTERRUPTED (§3.7.1) |
| `src/decision-tree/FieldZone.cs` | Enum: DEFENSIVE/MIDFIELD/ATTACKING |
| `src/decision-tree/MatchPhase.cs` | Enum: OPEN_PLAY/SET_PIECE_HOME/SET_PIECE_AWAY/KICK_OFF |
| `src/decision-tree/PassingStyle.cs` | Enum: DIRECT/MIXED/SHORT |
| `src/decision-tree/PressingMode.cs` | Enum: HIGH/MEDIUM/LOW |
| `src/decision-tree/PossessionState.cs` | Enum: HOME_TEAM/AWAY_TEAM/CONTESTED |
| `src/decision-tree/Tests/decision-tree-tests.asmdef` | Test assembly (EditMode; references decision-tree.asmdef) |
| `src/decision-tree/Tests/OptionGeneratorTests.cs` | UT-01..07: OptionGenerator generation gates and candidate logic |
| `src/decision-tree/Tests/UtilityScorerTests.cs` | UT-08..09: UtilityScorer per-action-type utility formulas |
| `src/decision-tree/Tests/ActionSelectorTests.cs` | UT-10..15: ActionSelector composure noise + winner selection |
| `src/decision-tree/Tests/DispatcherTests.cs` | UT-16..23: ActionDispatcher movement routing |
| `src/decision-tree/Tests/DecisionTreeIntegrationTests.cs` | UT-24..35: full pipeline state machine + output (UT-33..35 are June 11 audit H-3 locks) |
| `src/decision-tree/Tests/DecisionContextAssemblerTests.cs` | June 11, 2026 audit locks: H-2 team-relative BallZone + M-1 OpponentHasBall derivation |
| `src/decision-tree/Tests/DecisionTreeStateTests.cs` | Match Engine Phase D D0 locks: DecisionTreeState CanonicalSerializer round-trip + Capture/Restore identity + fresh-IDLE default + reflection field-count guard (asmdef gains DeterministicSim ref) |
| `src/decision-tree/Tests/TacticTranslationTests.cs` | #21 T2 seam locks: enum-translation validity + non-inversion + F5 clamp; Mentality Balanced identity (FR-TI-031) + Stage0Default no-op + monotone risk/line shape (asmdef gains TacticalInstructions ref) |

### Positioning AI (#12) — 41 files (incl. the #23/#24/#25 files this assembly hosts)

| File | Description |
|------|-------------|
| `src/positioning-ai/positioning-ai.asmdef` | Assembly definition (Mechanics layer; references tactical-instructions for the #21 T2 width seam) |
| `src/positioning-ai/PositioningAIConstants.cs` | Single constant catalogue (FR-PA-011/KD-17): pitch/spacing/hysteresis/GK/phase constants + 3 formation tables + pull-factor 13×4 table + lane edges |
| `src/positioning-ai/Phase.cs` | Enum: InPoss/OutOfPoss/TransToAtk/TransToDef (byte) |
| `src/positioning-ai/LineId.cs` | Enum: Defense/Midfield/Attack (byte) |
| `src/positioning-ai/LaneId.cs` | Enum: LW/LH/C/RH/RW — five 13.6 m bins (byte) |
| `src/positioning-ai/RoleId.cs` | Enum: 13 roles GK..ST — row index in 13×4 pull-factor table (byte) |
| `src/positioning-ai/FormationFamily.cs` | Enum: F442/F433/F4231 (byte) |
| `src/positioning-ai/FormationSlotRecord.cs` | Readonly struct: LongPct/LateralPct/Role/DefaultLine/DefaultLane/IsGoalkeeper |
| `src/positioning-ai/ContextModifierInputs.cs` | Readonly struct: ScoreDiff/TeamMeanFatigue/TacticalIntensity; #21 T2 Width/DefensiveWidth routing fields (3-arg ctor seeds Standard = ×1.00 identity; 5-arg ctor for the Phase-D writer) |
| `src/positioning-ai/AgentPositioningData.cs` | Readonly struct: EntityId/SlotIndex/Position/IsActive/Role/IsGoalkeeper |
| `src/positioning-ai/AgentHysteresisState.cs` | Struct: CurrentLine/CandidateLine/LineDwellCount/CurrentLane/CandidateLane/LaneDwellCount |
| `src/positioning-ai/HysteresisState.cs` | Sealed class: team phase state + AgentHysteresisState[] Agents; SeedFromFormation() |
| `src/positioning-ai/PositioningPerceptionSnapshot.cs` | Sealed class: pre-allocated tick input (TickIndex/BallPosition/BallVxFiltered/Agents[]); v1.1 #23/#24/#25 wiring — DismarkIntensity + per-agent pressure/marker carriers (one-stride-stale per §3.2 M-1), BuildUpStructure + committed zone + suppression flag, RotationFreedom (zero defaults = identities) |
| `src/positioning-ai/PhaseClassifier.cs` | Pure static: ClassifyAndCommit() PHASE_HYSTERESIS_TICKS dwell; indeterminate → lastCommitted |
| `src/positioning-ai/AnchorCalculator.cs` | Pure static: ComputeAnchor/ComputeBallRelativeOffset/ComputeGkSlot (own-half ball.x clamp) |
| `src/positioning-ai/ContextModifier.cs` | Pure static: ApplyToAll() — lateral + vertical compactness scaling relative to centroid (§3.5); #21 T2 — lateralScale ×= phase-selected width scalar via TacticTranslation (in-poss Width / OOP DefensiveWidth; Standard ⇒ ×1.00 exact) |
| `src/positioning-ai/SpacingResolver.cs` | Pure static: EnforceHardSpacing() cost-based displacement up to SPACING_MAX_PASSES (§3.6) |
| `src/positioning-ai/ShapeAnalyzer.cs` | Pure static: ResolveAllLines() insertion-sort + LINE_DWELL_TICKS; ResolveAllLanes() LANE_DWELL_TICKS; called AFTER spacing+clamp (AR-S1-03) |
| `src/positioning-ai/SlotComposer.cs` | Pure static: Compose() pipeline (anchor→offset→modifiers→**#24 build-up overlay**→spacing→**#23 dismark offset**→clamp→lines→lanes; the ERR-012-007/008 stage insertions in the #24 §4.2 combined order; both stages exact no-ops at the zero dials) |
| `src/positioning-ai/PositioningAITick.cs` | Sealed class: 10 Hz orchestrator; zero-alloc hot path; F1 stale detection; GetFormationSlot/GetLine/GetLane/GetPhase; v1.3 #25 wiring — RotationController runs after phase classification, before compose (§4.2/ERR-012-009); post-compose LastComposedTarget write-back; CaptureRotationState() seam |
| `src/positioning-ai/RestDefenseEvaluator.cs` | Cheap-item addition (§3.5/§7.13): pure static rest-defense coverage check (outfield agents behind REST_DEFENSE_DEPTH_M while IN_POSSESSION) |
| `src/positioning-ai/MarkingDwellState.cs` | #23 §2.2.1 per-agent dwell state (DwellTicks + LastMarkerId; Unmarked factory — default carries marker id 0); serialized at v12 |
| `src/positioning-ai/MarkingPressureEvaluator.cs` | #23 pure static: marker search + §3.2 dwell machine + FM-DM-01 pressure + FM-DM-02 offset (primitive-span inputs per the Mechanics-cannot-import-AI layering note; F1/F3/F4 gates) |
| `src/positioning-ai/BuildUpZone.cs` | #24 enum: OwnThird/MiddleThird/FinalThird (byte) |
| `src/positioning-ai/BuildUpZoneState.cs` | #24 §2.2.2 per-team committed zone + suppression countdown; serialized at v12 |
| `src/positioning-ai/BuildUpZoneClassifier.cs` | #24 pure static: FM-BU-01 committed-zone-expansion hysteresis + FM-BU-03 arming/decrement arithmetic |
| `src/positioning-ai/BuildUpOverlayCatalogue.cs` | #24 Appendix A [GT] overlay tables (row keys per ERR-024-001; centreline lateral-sign resolution) |
| `src/positioning-ai/RotationPair.cs` | #25 §2.2.3 normalized adjacency pair (GK-refusing ctor) |
| `src/positioning-ai/RotationPairState.cs` | #25 §2.2.2 per-pair dwell/rotated/hold state; serialized at v12 |
| `src/positioning-ai/RotationAdjacencyCatalogue.cs` | #25 Appendix A [GT] adjacency tables (F442/F433/F4231, commit-priority row order) |
| `src/positioning-ai/RotationController.cs` | #25 §3.1–§3.4 controller: FM-RO-01 predicate on the serialized LastComposedTarget cache, FM-RO-02 dwell/commit/revert/hold, atomic SlotIndex swap + partner lock, phase-exit freeze, F2/F5/F6 validating restore seams; sole post-seed SlotIndex writer (ERR-012-009) |
| `src/positioning-ai/TacticTranslation.cs` | #21 T2 consumer seam: TacticWidth/TacticDefWidth → lateral-compactness scalar (direct ordinal lookup over WidthScalar/DefWidthScalar, §3.1 F5 clamp; Standard ⇒ ×1.00); pure, translate-once (FR-TI-025) |
| `src/positioning-ai/Tests/positioning-ai-tests.asmdef` | Test assembly (EditMode; references positioning-ai.asmdef + tactical-instructions) |
| `src/positioning-ai/Tests/PositioningAITests.cs` | T-U-001..021 (unit) + T-D-001..002 (determinism) + T-I-001..004 (integration) + T-P-001 (perf) + T-T-001 (tactical) |
| `src/positioning-ai/Tests/TacticTranslationTests.cs` | #21 T2 seam locks: TacticWidth/TacticDefWidth → compactness scalar validity + Standard identity (FR-TI-031) + ContextModifierInputs Standard-seed neutrality + monotone shape + F5 clamp |
| `src/positioning-ai/Tests/RestDefenseEvaluatorTests.cs` | §3.5/§7.13 rest-defense coverage locks |
| `src/positioning-ai/Tests/MarkingPressureEvaluatorTests.cs` | #23 T0: FM-DM-01/02 worked examples + dwell machine + F1/F3/F4 gates |
| `src/positioning-ai/Tests/BuildUpStructureTests.cs` | #24 T0: FM-BU-01/03 + catalogue bound/identities + ERR-024-001 regression |
| `src/positioning-ai/Tests/RotationCatalogueTests.cs` | #25 T0: F1 invariants + pinned Appendix A rows + FR-RO-007 bound |
| `src/positioning-ai/Tests/SlotComposerStageTests.cs` | #23/#24 stage-insertion locks: exact catalogue Δ + None identity + phase/suppression/final-third gates + post-overlay spacing invariant (#24); exact FM-DM-02 offset + Off identity + carrier/phase exclusion + clamp bounds (#23) |
| `src/positioning-ai/Tests/RotationControllerTests.cs` | #25 controller locks: 5-tick trigger dwell + atomic swap, Conservative advantage bar, hold-blocked revert + mirrored dwell, partner lock, per-tick commit cap, phase-exit freeze, Off identity, F2/F5/F6 restore gates |
| `src/pressing-ai/pressing-ai.asmdef` | Assembly definition (Mechanics layer; references positioning-ai, pass-mechanics, tactical-instructions) |
| `src/pressing-ai/PressingAIConstants.cs` | Single constant catalogue: trigger distances/durations, cover-shadow geometry, stamina costs, pitch constants (GT/Fixed/Derived/Cross regions) |
| `src/pressing-ai/AssemblyInfo.cs` | `[InternalsVisibleTo("TacticalDirector.PressingAI.Tests")]` — created June 12, 2026 (dotnet CI gate; test suite was uncompilable without it) |
| `src/pressing-ai/TriggerFlags.cs` | [Flags] enum: None / BadTouch / BackwardPass / SidelineTrap / WeakReceiver (byte) |
| `src/pressing-ai/PressRole.cs` | Enum: HoldShape / PrimaryPress / CoverShadow (byte) |
| `src/pressing-ai/CoverShadow.cs` | Struct: DefenderId, ReceiverId, TargetPosition |
| `src/pressing-ai/PressDirective.cs` | Struct: per-tick output (PrimaryPresserId, PrimaryTargetPosition, Shadow0, Shadow1, CoverShadowCount, ActiveTriggers); static Inactive; IsActive property |
| `src/pressing-ai/PressAssignment.cs` | Struct: per-agent output (EntityId, Role, TargetPosition) |
| `src/pressing-ai/PressTrigger.cs` | Struct: 8 dwell/release counters (4 dwell + 4 release; zero allocation, no arrays) |
| `src/pressing-ai/RoleHysteresisState.cs` | Sealed class: LastRole[], PendingRole[], RoleDwell[] arrays keyed by EntityId (AR-2 M-2/M-3); Reset() |
| `src/pressing-ai/PressingTickState.cs` | Readonly struct: D4 snapshot view bundling the cross-tick state (RoleHysteresisState, PressTrigger, disengage/cooldown dwell, press-fatigue array); returned by PressingAITick.CaptureState for the Match Engine snapshot layer |
| `src/pressing-ai/PressingAgentSnapshot.cs` | Struct: per-agent tick input (EntityId, TeamId, Position, BaselineSlot, Fatigue, FirstTouchAttribute, Line, IsGoalkeeper, HasBall, IsActive) |
| `src/pressing-ai/PressingSnapshot.cs` | Sealed class: tick input container (TickIndex, BallPosition, BallVelocity, BallCarrierEntityId, AttackingDirection, PossessionTeamId, PressingTeamId, Agents[22]); #21 T2 `LineOfEngagement` routing field (ctor-seeded `Standard` = identity — zero-value default is VeryLow) |
| `src/pressing-ai/PassEventRing.cs` | Sealed class: ring buffer for BackwardPass trigger (Push, TryGetLatest, Clear) |
| `src/pressing-ai/PositioningAIView.cs` | Readonly struct: facade over PositioningAITick (GetFormationSlot, GetLine, GetPhase, IsSentinelSlot) |
| `src/pressing-ai/TriggerEvaluator.cs` | Pure static: Evaluate() debounce pipeline for 4 triggers + ComputeGeometricPressure helper |
| `src/pressing-ai/PrimaryPressSelector.cs` | Pure static: Select() best presser by cost; ComputeInterceptionPoint(); GetCarrierPosition() helper; #21 T2 — eligibility radius scaled by TacticTranslation.PressTriggerRadiusScalar(snapshot.LineOfEngagement) (Standard ⇒ ×1.0) |
| `src/pressing-ai/TacticTranslation.cs` | #21 T2 consumer seam: LineOfEngagement → #13 press-trigger-radius scalar (direct ordinal lookup over LineOfEngagementScalar, §3.1 F5 clamp; Standard ⇒ ×1.0); pure, translate-once (FR-TI-025) |
| `src/pressing-ai/CoverShadowSelector.cs` | Pure static: Select() up to 2 cover shadows; threat score + greedy defender assignment |
| `src/pressing-ai/RoleHysteresis.cs` | Pure static: Commit() dwell guard; ForceAllHoldShape() |
| `src/pressing-ai/StaminaAccumulator.cs` | Pure static: Apply() per-role fatigue cost; ApplyAll() batch apply |
| `src/pressing-ai/DisengageResolver.cs` | Pure static: Evaluate() disengage conditions (zone exit + timeout); IsInCooldown() |
| `src/pressing-ai/InvariantEnforcer.cs` | Pure static: Enforce() three anti-chaos invariants (MaxPressersBallThird, MinBacklineAgents, MaxPressDisplacementM) |
| `src/pressing-ai/PitchOrientation.cs` | Pure static: AttackRelativeX() — attack-direction-relative pitch X for §3.8 zone / §3.9 own-third checks (AR-2 H-1) |
| `src/pressing-ai/PressingAITick.cs` | Sealed class: 10 Hz orchestrator; 8-step pipeline; pre-allocated buffers; zero-alloc hot path; persistent press-fatigue ledger (AR-2 M-1) |
| `src/pressing-ai/Tests/TacticTranslationTests.cs` | #21 T2 seam locks: LineOfEngagement → press-trigger-radius scalar validity + Standard identity (FR-TI-031) + PressingSnapshot ctor-seed behaviour-neutrality + monotone shape + F5 clamp (tests asmdef gains TacticalInstructions ref) |

### `src/defensive-ai/` — Spec #14 (20 files: 19 .cs + 1 asmdef)

| File | Role |
|------|------|
| `src/defensive-ai/defensive-ai.asmdef` | Assembly definition (Mechanics layer; references positioning-ai, pressing-ai, tactical-instructions) |
| `src/defensive-ai/DefensiveAIConstants.cs` | Single constant catalogue: 22 [GT] + 4 [CROSS] constants (assignment, hysteresis, offside-trap, tackle, anti-chaos, GK-zone bounds) |
| `src/defensive-ai/MarkMode.cs` | Enum: Zonal / ManMark / InterceptRunner / CoverGkZone (byte; FR-DA-011) |
| `src/defensive-ai/TackleMode.cs` | Enum: Hold / Jockey / Commit (byte) |
| `src/defensive-ai/MarkDirective.cs` | Struct: team-level tick output (TeamId, OffensiveLineDepth, OffsideTrapActive, StepUpTargetDepth, EmergencyFlag); Inactive() factory |
| `src/defensive-ai/MarkAssignment.cs` | Struct: per-agent assignment (Mode, TargetEntityId, TargetPosition, ValidThroughTick, OverriddenThisTick, IsManuallyAssigned); MakeZonal() factory |
| `src/defensive-ai/TackleIntentRequest.cs` | Struct: per-agent tackle intent (AgentEntityId, Mode, TargetEntityId, ApproachAngle, CoverageDepth) |
| `src/defensive-ai/MarkHysteresisState.cs` | Struct: per-agent dwell-lock state (DwellCounter, CandidateMode, CandidateTargetEntityId, HoldTicks); Default() factory |
| `src/defensive-ai/DefensiveTickState.cs` | Readonly struct: D4 snapshot view bundling cross-tick state (per-entity MarkHysteresisState[], per-entity last MarkAssignment[], OffsideLineState); returned by DefensiveAITick.CaptureState for the Match Engine snapshot layer |
| `src/defensive-ai/OffsideLineState.cs` | Struct: per-team offside state (CurrentLineDepth, StepUpDwellCounter, CooldownTicksRemaining, CoverGkZoneActiveTicks); Default() factory |
| `src/defensive-ai/DefensiveAgentSnapshot.cs` | Struct: per-agent tick input (EntityId, TeamId, Position, Velocity, IsActive, IsGoalkeeper, HasBall, BaselineSlot, Line, PressRole, PerceivedFirstTouch) |
| `src/defensive-ai/DefensiveSnapshot.cs` | Sealed class: tick input container (TickIndex, DefensiveTeamId, BallPosition, BallVelocity, TeamPhase, DefensiveLineDepth, GkEntityId, GkPosition, Agents[22], HasActivePrimaryPress); #21 T2 OffsideTrapRequested routing field (false identity; arming-gate consumption deferred per KD-9); + MarkingOrientation routing field (ctor-seeded Balanced, cheap-item addition FR-TI-033) |
| `src/defensive-ai/HoldShapePoolFilter.cs` | Pure static: BuildPool() filters GK + PrimaryPress/CoverShadow; SnapshotIndexOf(); IndexOf() |
| `src/defensive-ai/LastManDetector.cs` | Pure static: Evaluate() last-man predicate (§3.8) + COVER_GK_ZONE trigger (§3.9); DefendsX0/DistToOwnGoal/DisplacementCost/ComputeAbandonedZoneCenter helpers; LastManResult struct |
| `src/defensive-ai/MarkHysteresis.cs` | Pure static: PreCheck() dwell-lock gate; ApplyGate() transition accumulator; Reset() for emergency overrides |
| `src/defensive-ai/MarkAssigner.cs` | Pure static: Assign() regular assignment loop (§3.3); ThreatScore() (§3.5); SelectBestCandidate(); IsBetter() tie-break comparator; MAN_MARK candidate radius scaled by TacticTranslation.MarkRadiusScalar (cheap-item addition FR-TI-033) |
| `src/defensive-ai/TackleIntentEvaluator.cs` | Pure static: Evaluate() tackle intent (§3.6); ComputeCoverageDepth(); SelectMode() |
| `src/defensive-ai/OffsideTrapController.cs` | Pure static: Update() dwell counter + fire trigger (§3.7); ExecuteStepUp(); ComputeDefenseLineSpread(). #21 FR-TI-022/KD-9 (v1.2): consumes OffsideTrapRequested as an additive request — requested ⇒ reduced OffsideTrapRequestedDwellTicks; false ⇒ baseline (neutral) |
| `src/defensive-ai/InvariantEnforcer.cs` | Pure static: Enforce() 3 anti-chaos invariants (§3.10); 3-pass demotion loop; AreAllSatisfied() post-loop check; F4 hard-fallback detection |
| `src/defensive-ai/DefensiveAITick.cs` | Sealed class: 10 Hz orchestrator; 9-step §3.13 pipeline; pre-allocated buffers; GetMarkDirective/GetAssignment/GetTackleIntentRequests public API |
| `src/defensive-ai/TacticTranslation.cs` | #21 T2 consumer seam: OffsideTrap → #14 trap-request bool passthrough (false identity; KD-9 request-not-guarantee); + MarkRadiusScalar(MarkingOrientation) → MAN_MARK radius scalar (cheap-item addition FR-TI-033); pure, translate-once (FR-TI-025) |
| `src/defensive-ai/Tests/TacticTranslationTests.cs` | #21 T2 seam locks: OffsideTrap passthrough + DefensiveSnapshot false-seed identity (FR-TI-031); tests asmdef gains tactical-instructions ref |

### `src/attacking-ai/` — Spec #15 (26 files: 24 .cs + 1 asmdef + 1 test)

| File | Description |
|------|-------------|
| `src/attacking-ai/attacking-ai.asmdef` | Assembly definition (Mechanics layer; references positioning-ai, pressing-ai, tactical-instructions) |
| `src/attacking-ai/AttackingAIConstants.cs` | Single constant catalogue: GT/Derived/Cross constants (run-params bounds, support radius, width, weak-side, overload, invariants, hysteresis, test criteria, angle epsilon) |
| `src/attacking-ai/AttackRole.cs` | Enum: HoldWidth / SupportBall / Runner / WeakSide (byte; FR-AT-012) |
| `src/attacking-ai/Flank.cs` | Enum: Left / Right — overload lateral discriminator (§3.8) |
| `src/attacking-ai/RunParameters.cs` | Readonly struct: DepthOffsetM / LateralOffsetM / RunTriggerTick — exactly 3 fields (FR-AT-011) |
| `src/attacking-ai/AttackHysteresisState.cs` | Struct: per-agent dwell state (CurrentRole, DwellCounter, CandidateRole, CandidateDwell) |
| `src/attacking-ai/AttackingTickState.cs` | Readonly struct: D4 snapshot view bundling cross-tick state (per-agent AttackHysteresisState[], TransitionHoldState, frozen in-possession AttackDirective); returned by AttackingAITick.CaptureState for the Match Engine snapshot layer |
| `src/attacking-ai/TransitionHoldState.cs` | Struct: per-team possession-loss countdown + PrevPhase |
| `src/attacking-ai/AttackDirective.cs` | Readonly struct: team-level tick output (TeamId, OverloadActive, OverloadFlank, TransitionHoldTick); static Empty |
| `src/attacking-ai/AttackIntent.cs` | Readonly struct: per-agent tick output (AgentEntityId, Role, RunParameters?, ValidThroughTick) |
| `src/attacking-ai/StyleProfile.cs` | Readonly struct: 5 profile multipliers + static factories Possession/Direct/Counter |
| `src/attacking-ai/AttackIntentSnapshot.cs` | Readonly struct: read-only zero-copy view over tick output (Directive, Intents[], IntentCount, TickIndex) |
| `src/attacking-ai/AttackingAgentSnapshot.cs` | Readonly struct: per-agent tick input (EntityId, TeamId, Position, BaselineSlot, Line, IsGoalkeeper, HasBall, IsActive, Pace, Stamina, Dribbling) |
| `src/attacking-ai/AttackingSnapshot.cs` | Sealed class: pre-allocated tick input container (TickIndex, AttackingTeamId, BallPosition, BallCarrierEntityId, BallCarrierPosition, TeamAttackAngle, Agents[22]); #21 T2 FocusPlay routing field (Mixed zero-value identity; OverloadDetector consumption deferred) |
| `src/attacking-ai/AttackPoolEntry.cs` | Internal struct: per-agent scratch entry during pipeline (EntityId, Position, LateralPct, Line, AssignedRole, HasRunParams, run-param fields, RunTargetPosition, TargetPosition) |
| `src/attacking-ai/AttackingPoolBuilder.cs` | Pure static: Build() filters snapshot→pool, EntityId-ascending insertion sort; −1 on F2 sentinel |
| `src/attacking-ai/AttackHysteresis.cs` | Pure static: IsStable() / Update() (with CandidateDwell reset on current-role re-preference) / Reset() — increment-based dwell |
| `src/attacking-ai/SupportHeuristic.cs` | Pure static: IsWithinSupportRadius() / ComputeEffectiveRadius() — floor = MinEffectiveRadiusM |
| `src/attacking-ai/RoleAssigner.cs` | Pure static: Assign() two-pass (pass 1 counts stable, pass 2 evaluates non-stable); GenerateRunParams() §3.4 with Mathf.RoundToInt |
| `src/attacking-ai/WidthHolder.cs` | Pure static: Enforce() near-touchline width-holding; skips near-side HoldWidth+WeakSide in promotion loop |
| `src/attacking-ai/WeakSideController.cs` | Pure static: EnsureWeakSide() post-check; selects max-|Y-ballY| non-RUNNER agent |
| `src/attacking-ai/OverloadDetector.cs` | Pure static: Evaluate() counts non-WEAK_SIDE agents in Y-corridor; fires at ≥OverloadCount. #21 FR-TI-021 (v1.1): 5-arg Evaluate overload (4-arg delegates null) — a FocusPlay-preferred ball-side flank lowers the trigger count by OverloadFocusCountBias (bias, not gate; null ⇒ unchanged) |
| `src/attacking-ai/TransitionController.cs` | Pure static: Evaluate() SET-then-DECREMENT transition hold; COUNTER (0 ticks) → instant empty |
| `src/attacking-ai/InvariantEnforcer.cs` | Pure static: Apply() 3 anti-chaos invariants (max runners, min support, no own-half runs); ApplyFallback() all-HoldWidth |
| `src/attacking-ai/AttackingAITick.cs` | Sealed class: 10 Hz orchestrator; §3.13 pipeline; pre-allocated zero-alloc buffers; LastDirective/GetIntent/GetSnapshot public API |
| `src/attacking-ai/TacticTranslation.cs` | #21 T2 consumer seam: FocusPlay → preferred Flank? (Mixed/ThroughMiddle → null identity; FR-TI-021); pure, translate-once (FR-TI-025) |
| `src/attacking-ai/Tests/TacticTranslationTests.cs` | #21 T2 seam locks: FocusPlay → preferred-flank mapping + AttackingSnapshot Mixed-zero-value identity (FR-TI-031); tests asmdef gains tactical-instructions ref |

### `src/deterministic-sim/` — Spec #16 (30 files: 27 .cs + 2 asmdef + 1 native; native/ C shim)

> Cross-cutting foundation assembly; all gameplay layers reference it; it references no other gameplay assembly.
> AR-1 (4H+4M) + AR-2 (1L) + AR-3 (1L) adversarial review cycles complete (AR-3 clean). Implementation date: May 29, 2026.
> Golden-vector pass June 12, 2026: full §9.5 #4(a)/(b)/(c) KAT suites landed (3 new test fixtures); HKDF upgraded to full RFC 5869 multi-block Expand; WriteF64TierB added; AssemblyInfo.cs InternalsVisibleTo added (the test assembly's internal HkdfSha256/SipHash24_64 calls had never been compilable without it); DeterministicSimTests.cs stray namespace closure fixed (save/load fixture was stranded in the global namespace).

| File | Purpose |
|------|---------|
| `src/deterministic-sim/deterministic-sim.asmdef` | Assembly definition (no references — cross-cutting foundation) |
| `src/deterministic-sim/DeterministicSimConstants.cs` | All [FIXED]/[DERIVED]/[GT] constants: tick rates, error codes (0x1601–0x160D), domain tags (0x10–0x1D), field widths, RNG params, digest/schema versions, END_OF_SNAPSHOT_PHASE_ORDINAL=6, FrameMs / FrameSeconds (B1 per-tick dt) |
| `src/deterministic-sim/PhaseId.cs` | Enum: Input=0 / Intent=1 / AI=2 / Physics=3 / Resolve=4 / Events=5 / Snapshot=6 (byte; AR-1 H-4: AI_NoOp removed; Events=5 added) |
| `src/deterministic-sim/DeterminismTier.cs` | Enum: TierA=0 / TierB=1 / TierC=2 (byte) |
| `src/deterministic-sim/DivergenceClass.cs` | Enum: None / HardDesync / SoftDrift / Cosmetic (byte) |
| `src/deterministic-sim/SubsystemOrdinals.cs` | Compile-time const ints for deterministic intra-phase ordering: BallPhysics=0..GoalkeeperMechanics=7 (Physics 0–19), PositioningAI=20..AttackingAI=23 (Mechanics 20–39), PerceptionSystem=40, DecisionTree=41 (AI 40–59), EventSystem=60 |
| `src/deterministic-sim/ReplayCursor.cs` | Readonly struct: Tick (ulong), PhaseOrdinal (byte), IsAtEndOfSnapshot property, EndOfSnapshot(tick) factory — step-7 boundary assertion in ReplayEngine |
| `src/deterministic-sim/DespawnEntry.cs` | Readonly struct: EntityId (int), FinalActionOrdinal (ulong), FinalRngCursor (ulong), DespawnTick (ulong) — Tier A tombstone written by Resolve phase |
| `src/deterministic-sim/DespawnLog.cs` | Pre-allocated tombstone list: Append / ContainsEntity / GetEntry / Clear; capacity = MaxDespawnEntries (512) |
| `src/deterministic-sim/EnvironmentFingerprint.cs` | Sealed class: 6 readonly fields (WorkerCount, SchedulerPolicy, ReductionTopology, SimdFeatureLevel, FloatModelHash, UnicodeNormalizationVersion); Lock(); ValidateAgainst() → ERR_DS_REPLAY_ENV_MISMATCH; CreateStage0Dev() placeholder factory + IsDevPlaceholder gate; CreateStage0MonoCertified() real-hash factory (ERR-016-006 Option A) + Stage0Mono*/Stage0SimdLevel consts |
| `src/deterministic-sim/FloatFlagTuple.cs` | Readonly struct: the §4.8.3 11-field float-flag tuple + ComputeHash() = SHA-256(SerializeCanonical(0x14 ‖ tuple)) — the live-host floatModelHash hasher (ERR-016-006 Option A) |
| `src/deterministic-sim/MxcsrNative.cs` | Internal static: P/Invoke boundary (`[DllImport("td_mxcsr")]`) to the native MXCSR shim; TryQuery(out uint) reads the calling thread's SSE control/status register, returns false (probe unavailable) when the library is absent (§4.8.2) |
| `src/deterministic-sim/MxcsrValidator.cs` | Public static: §4.8.2 runtime float-mode gate — decode DAZ (bit 6) / FTZ (bit 15) / RC (bits 13–14), MatchesStage0Pin(uint) pure check, ValidateStage0FloatMode() → ProbeStatus (Unavailable off-host / Validated / throws on divergence); mirrors FloatFlagTuple's Stage-0 pinned fields 5/6/7 |
| `src/deterministic-sim/native/mxcsr_query.c` | Native C shim: exports `td_get_mxcsr()` = `_mm_getcsr()` (single STMXCSR). Built to td_mxcsr.dll / libtd_mxcsr.so; the managed intrinsic .NET/Mono lacks. Build instructions in native/README.md |
| `src/deterministic-sim/native/README.md` | Build + availability-policy doc for the td_mxcsr shim (MSVC/GCC build lines, Assets/Plugins/x86_64 placement, off-host no-op semantics, certified-capture host-block note) |
| `src/deterministic-sim/RngStreamState.cs` | Mutable struct: StreamKey/RngCursor/ActionOrdinal (ulong), BudgetRemaining/DeclaredBudget/DrawIndex (int), SiteId (string), StreamVersion (ushort), SubsystemOrdinal (int), EntityId (int); ClearReservation() |
| `src/deterministic-sim/MatchClock.cs` | Sealed class: CurrentTick / CurrentTacticalTick (÷AI_PHASE_STRIDE) / CurrentMatchTimeMs (×FrameMs) / CurrentMatchTimeSeconds (×FrameSeconds; B1 seconds-clock) / IsAiStrideTick; Advance(); RestoreFromSnapshot(tick) for replay step 5 — no System.DateTime (FR-CS-042) |
| `src/deterministic-sim/DeterministicRngService.cs` | Sealed class: HKDF-SHA256 key derivation at construction; SipHash-2-4-64 per-draw hash; RegisterStream / Reserve / DrawReserved / CloseReservation / Skip / RestoreStream; zero-alloc hot path (stackalloc Span<byte>[21]; AR-1 H-3) |
| `src/deterministic-sim/CanonicalSerializer.cs` | Static class: §3.2.4.1 Write/Read for bool, u8/i8, u16/i16, u32/i32, u64/i64, f32 (−0.0→+0.0), f32TierB (NaN→0x7FC00000), f64, f64TierB (NaN→0x7FF8000000000000; corpus F-09), strings, bytes, optional tags; FloatUintUnion explicit-layout struct (AR-1 H-1/H-2: eliminates BitConverter.GetBytes heap alloc) |
| `src/deterministic-sim/SnapshotHeader.cs` | Sealed class: SchemaVersion (u32) / DigestVersion (u16) / Tick (u64) / PrevSnapshotDigest[32] / CurrentSnapshotDigest[32] / Fingerprint / Cursor; Initialize(tick, prevDigest, fingerprint) |
| `src/deterministic-sim/SnapshotPayload.cs` | Sealed class: pre-allocated PayloadBytes[MaxSnapshotBytes] / BytesWritten; Reset() |
| `src/deterministic-sim/SnapshotCodec.cs` | Sealed class: Encode() — SHA-256 over payload bytes, digest chain advance; ValidateHeader() → ERR_DS_SCHEMA_INCOMPATIBLE; ValidatePrevDigest() → ERR_DS_DIGEST_CHAIN_BREAK; CommitLoadedDigest() for replay load |
| `src/deterministic-sim/ReplayEngine.cs` | Sealed class: PrepareReplay() executes §4.2.2 steps 1–7; step 6 (RNG restoration) is Stage 0 stub comment (AR-3 L-1: empty loop replaced); step 8 (ReapplyInputsFromT+1) delegated to TickOrchestrator |
| `src/deterministic-sim/SaveManager.cs` | Sealed class: CommitAtomic() implements §4.6.1.1 five-step atomic save (temp write → fsync → rename-with-overwrite → dir fsync); File.Move(overwrite:true) (AR-1 M-2: IOException fix) |
| `src/deterministic-sim/TickOrchestrator.cs` | Sealed class: RunTick() 7-phase 60 Hz pipeline (Input→Intent→AI/AI_NoOp→Physics→Resolve→Events→Snapshot); AI stride-gated on IsAiStrideTick; System.Action phase callbacks; 9 ProfilerMarkers; zero-alloc hot path |
| `src/deterministic-sim/DivergenceDetector.cs` | Static class: CompareDigests / CompareTierAFloat / CompareTierBFloat (AR-1 M-3: one-canonical-NaN case returns SoftDrift) / CompareTierAInt / CompareTierAUlong / Worst(DivergenceClass, DivergenceClass) |
| `src/deterministic-sim/AssemblyInfo.cs` | Assembly-level attributes: InternalsVisibleTo("TacticalDirector.DeterministicSim.Tests") so the KAT suites can drive internal HkdfSha256/HkdfExtract/HkdfExpand/SipHash24_64 (first-touch/pass-mechanics precedent) |
| `src/deterministic-sim/tests/deterministic-sim-tests.asmdef` | Test assembly definition (EditMode; references deterministic-sim.asmdef) |
| `src/deterministic-sim/tests/DeterministicSimTests.cs` | HKDF RFC 5869 Appendix A.1 KAT; SipHash-2-4-64 ref vectors 0–7; canonical serialization (bool, u32/u64 LE, −0.0, PHYSICS_DT bits); T-DS-ORDER-001 clock sequence; T-DS-RNG-002 branch cursor parity; T-DS-SNAP-003 u64 round-trip; T-DS-FAULT-009..014 (budget mismatch, Tier A NaN, Tier B non-canonical NaN, digest chain break, env mismatch, replay boundary); AI stride; DespawnLog; v1.2 fixed the v1.1 stray namespace closure that stranded the save/load fixture in the global namespace |
| `src/deterministic-sim/tests/HkdfSha256KatTests.cs` | Full HKDF-SHA256 KAT suite (§9.5 #4(a)): RFC 5869 A.1–A.3 PRK + full OKM byte-exact (L=42/82 locks multi-block Expand) + pinned project Test Case 4 (RNG_KDF invocation pattern → (k0,k1)) per hkdf-sha256-kat.md v1.2 |
| `src/deterministic-sim/tests/SipHash24KatTests.cs` | Full SipHash-2-4-64 KAT suite (§9.5 #4(b)): all 64 Aumasson & Bernstein 2012 Appendix A vectors + pinned project RNG_STREAM_HASH 21-byte draw-preimage case per siphash-2-4-kat.md v1.2 |
| `src/deterministic-sim/tests/SerializeCanonicalCorpusTests.cs` | Full canonical-serialization corpus suite (§9.5 #4(c)): all 41 serialize-canonical-corpus.md entries (P/F/S/B/O/E/A/ST/D incl. chained SnapshotDigest D-07), encoded bytes + SHA-256 asserted per entry |
| `src/deterministic-sim/tests/MxcsrValidatorTests.cs` | Pure-decode locks for MxcsrValidator (§4.8.2): DAZ/FTZ/RC extraction from synthetic MXCSR values, Stage-0 pin match, exception-flag independence, ValidateStage0FloatMode non-throwing off-host (Unavailable). Native-shim-free — runs on the Linux gate |

### `src/event-system/` — Spec #17 (21 files: 19 .cs + 2 asmdef)

> Cross-cutting foundation assembly; autoReferenced true so Assembly-CSharp assemblies get it automatically; spec assemblies with their own .asmdef need explicit references.
> AR-1 (3H+3M+2L) + AR-2 (1L) + AR-3 clean adversarial review cycles complete. Implementation date: May 30, 2026.

| File | Purpose |
|------|---------|
| `src/event-system/event-system.asmdef` | Assembly definition (references TacticalDirector.DeterministicSim; autoReferenced true) |
| `src/event-system/EventSystemConstants.cs` | All [GT]/[CROSS] constants: queue/dispatch/handler/slot capacities + error codes (0x1701–0x1706) + DomainTagEventLedger (0x15). v1.1: AR-2 fix — MaxEventSlotBytes 128→160; ErrEvtUnregisteredOrdinal (0x1706) added. |
| `src/event-system/IEventA.cs` | Marker interface: Tier A (authoritative, ring-buffered, digest-included) |
| `src/event-system/IEventB.cs` | Marker interface: Tier B (bounded-authoritative; Stage 5+ tolerance path) |
| `src/event-system/IEventC.cs` | Marker interface: Tier C (cosmetic; immediate CosmeticChannel dispatch; excluded from digest) |
| `src/event-system/EventHandler.cs` | Delegate: `void EventHandler<T>(in T evt) where T : struct` |
| `src/event-system/SubscriptionToken.cs` | Readonly struct: EventTypeOrdinal + SubscriberIndex; zero allocation (FR-EVT-073) |
| `src/event-system/EventRegistry.cs` | Appendix A registry: 11 seeded rows (0x01–0x0B) + placeholder rows 0x0C–0x17 (updated by owning spec's EventBusRegistrar.Initialize()) + 3 match-flow-completion rows 0x18–0x1A (OffsideCalledEvent/RestartAwardedEvent/MatchPhaseChangedEvent, subsystemOrdinal = EventSystem, registered directly via RegisterRow<T> since match-engine has no domain-tag/subsystem-ordinal of its own — v1.8); RegisterRow<T> / RegisterRowRaw / RegisterExternalRow<T>; EventOrdinalCache<T> O(1) static-field lookup. v1.3: AR-3 fix — IsRegistered now requires StructSize > 0 (placeholder RegisterRowRaw rows return false until Initialize() sets struct size). |
| `src/event-system/EventLedger.cs` | Ring buffer + typed BFS dispatch; EventSlotMeta (FM-017-002 sort key); EventTypeDispatchBase / EventTypeDispatcher<T>; DrainTick; InsertionSort; SerializeLedger; Subscribe. v1.3: AR-4 H-1: EventTypeOrdinal removed from CompareKey (not in FM-017-002); AR-4 H-2: SerializeLedger now sorts by FM-017-002 key (was insertion order). |
| `src/event-system/CosmeticChannel.cs` | Tier C immediate dispatch: per-ordinal pub-count table; ≥ maxPerTick drop predicate; stackalloc span dispatch (zero-alloc FR-EVT-048). v1.5: AR-3 fix — structSize <= 0 guard promoted from silent return to throw; added upper-bound guard structSize > MaxEventSlotBytes preventing ArgumentOutOfRangeException crash on oversized Tier C structs. |
| `src/event-system/EventBus.cs` | Public static API: BeginTick / BeginPhase / DrainTick / SerializeLedger / OnTickBoundary; Publish / Subscribe overloads per tier. v1.3: AR-2 fix — unconditional if/throw guard in PublishAuthoritative; Subscribe<IEventA/B> guard. v1.4: AR-4 M-1: upper-bound structSize > MaxEventSlotBytes guard added to PublishAuthoritative Tier A/B path; AR-4 L-1: structSize<=0 fallback promoted to throw. |
| `src/event-system/EventTierCache.cs` | internal static generic: cached tier-marker flags (IsTierA/B/C/IsValid) backing EventBus single-method dispatch (ERR-017-002); type-init reflection only |
| `src/event-system/PossessionChangedEvent.cs` | Tier A 0x04: PreviousHolder / NewHolder / Reason |
| `src/event-system/FoulCommittedEvent.cs` | Tier A 0x05: Offender / Victim / Location (Vector3) / FoulKind |
| `src/event-system/CardIssuedEvent.cs` | Tier A 0x06: Recipient / CardKind / FoulOrdinal (byte; 0xFF = procedural) |
| `src/event-system/GoalAwardedEvent.cs` | Tier A 0x07: Scorer / Assister / ScoringTeam / BallPosition (Vector3) |
| `src/event-system/SubstitutionEvent.cs` | Tier A 0x08: Outgoing / Incoming / Team / SubstitutionReason |
| `src/event-system/TickHeartbeatEvent.cs` | Tier C 0x09: empty payload (CLR min size 1 byte); MaxPerTick=1 |
| `src/event-system/VfxImpactCue.cs` | Tier C 0x0A: ImpactPoint (Vector3) / ImpactKind / Intensity; MaxPerTick=64 |
| `src/event-system/UiNotificationCue.cs` | Tier C 0x0B: NotificationKind / SubjectEntity; MaxPerTick=32 |
| `src/event-system/OffsideCalledEvent.cs` | Match-flow completion (design note §4/§8): Tier A 0x18: OffendingAgentId / Team / Location (Vector3). Producer phase Resolve |
| `src/event-system/RestartAwardedEvent.cs` | Match-flow completion (design note §5/§8): Tier A 0x19: RestartKind (mirrors BallPhysics.RestartType) / AwardedTeam / Location (Vector3). Producer phase Resolve |
| `src/event-system/MatchPhaseChangedEvent.cs` | Match-flow completion (design note §7/§8): Tier A 0x1A: NewPhase (0=SecondHalf, 1=FullTime) / HomeScore / AwayScore. Producer phase Input (where CheckMatchFlowTransitions runs) |
| `src/event-system/tests/event-system-tests.asmdef` | Test assembly (EditMode; autoReferenced false; references TacticalDirector.EventSystem + TacticalDirector.DeterministicSim + 6 production spec assemblies for the boot-wiring smoke test: PassMechanics / ShotMechanics / PerceptionSystem / DecisionTree / HeadingMechanics / GoalkeeperMechanics — added 2026-06-08 with EventBusWiringSmokeTests.cs landing). |
| `src/event-system/tests/EventBusWiringSmokeTests.cs` | SMOKE-EVT-WIRING-001 boot-wiring smoke test (added 2026-06-08). Drives boot → publish-one-per-spec → DrainTick → SerializeLedger and asserts SHA-256 digest stability across runs. Catches regressions in ordinal allocation, version byte, producer-phase wiring, subsystem-ordinal embedding, struct-size registration, and canonical byte layout. Covers 6 currently-wired registrars (Pass / Shot / Perception / Decision / Heading / Goalkeeper); Agent Movement (#2) is a `[CROSS-PENDING]` slot. Golden digest pinned via Assert.Inconclusive (surfaces unfilled pin to CI as yellow) until the AM-side EventBusRegistrar lands. AR-1 (1H+1M+4L) + AR-2 (0H+1M+4L) + AR-3 (0H+0M+2L cycle-stop) adversarial review cycles complete. |

### `src/performance-optimization/` — Spec #18 (17 files: 16 .cs + 1 asmdef)

> Infrastructure-only assembly (Spec #18). autoReferenced false; references TacticalDirector.DeterministicSim (for EnvironmentFingerprint #16 §4.8). Game-layer assemblies (Physics / Mechanics / AI) MUST NOT import this assembly at runtime. Scaffold added May 30, 2026; promoted to real implementation June 1, 2026.

| File | Purpose |
|------|---------|
| `src/performance-optimization/performance-optimization.asmdef` | Assembly definition; references TacticalDirector.DeterministicSim; autoReferenced false |
| `src/performance-optimization/HotPathAllocExemptAttribute.cs` | Governance attribute (§3.7.5): `[HotPathAllocExempt(justification)]` with optional `SignOffRef`; `AttributeTargets.Method\|Class\|Struct`; `Inherited=false; AllowMultiple=false` |
| `src/performance-optimization/ChannelVerbosity.cs` | F.0 schema enum: Minimal / Standard / Debug / Exhaustive (FR-PO-055). Extracted from TraceChannel.cs in AR-1 H-1 |
| `src/performance-optimization/ChannelSamplingRule.cs` | F.0 schema enum: EveryTick / PerNTicks / EventDriven (FR-PO-056). Extracted from TraceChannel.cs in AR-1 H-1 |
| `src/performance-optimization/ChannelDeterminismClass.cs` | F.0 schema enum: TierA / TierB / TierC (FR-PO-058a). Extracted from TraceChannel.cs in AR-1 H-1 |
| `src/performance-optimization/TraceChannelDescriptor.cs` | v1.2 — F.0 sealed descriptor (11 fields); constructor enforces both structural F.0 invariants (SamplingRule=PerNTicks ⇒ SampleN>0 per AR-1 L-1; InsideTickPipeline ⇒ SignOffLogRef non-empty per AR-2 L-2) and carries XML <summary> per AR-2 M-2. Extracted from TraceChannel.cs in AR-1 H-1 |
| `src/performance-optimization/TraceChannelRegistry.cs` | Stage 0 anchor rows: PerfBudget / PerfAlloc / PerfTrace (perf.* channels owned by Spec #18). Extracted from TraceChannel.cs in AR-1 H-1 |
| `src/performance-optimization/PerformanceOptimizationConstants.cs` | v1.2 — Fixed: HOT_PATH_ALLOC_BUDGET_BYTES=0 / LOOP_TAG_TACTICAL_10HZ / LOOP_TAG_PHYSICS_60HZ; GT: PerPrRegressionFraction=0.05 / AbsoluteDriftFraction=0.10 / BaselineSampleCount=100 / MaxFlakeRate=0.01 / HeadroomMultiplierMin=1.2 / HeadroomMultiplierMax=1.5 / PromotionToleranceFraction=0.20 / ReproducibilityToleranceFraction=0.20; EST: SamplerDefaultHz=1000 / StatisticalSignificanceN=30 / FirstTickWarmupCount=0 |
| `src/performance-optimization/LoopTag.cs` | enum: TacticalTenHz / PhysicsSixtyHz — loop discriminator per KD-8 / §3.2.2; on-disk string keys are LOOP_TAG_* constants |
| `src/performance-optimization/BaselinePassFail.cs` | enum: Pass / Fail / Advisory — capture-time pass/fail verdict per Appendix A (advisory at capture; authoritative at CI gate time) |
| `src/performance-optimization/HardwareCounterSnapshot.cs` | readonly struct: CpuModel / CoreCount / ThermalState — §3.3.2 session manifest hardware field |
| `src/performance-optimization/SessionManifest.cs` | v1.2 — sealed class: all §3.3.2 required fields (GitSha / Seed / EnvironmentFingerprint #16 §4.8 / PlatformPin / ScenarioManifestId / SessionStartUtc / SessionEndUtc / HardwareCounters / HarnessVersion); `IsComplete()` validator used by §3.4.4 baseline validator (AR-2 L-1: validates HardwareCounters.CpuModel/ThermalState; PR #129 Codex P2: also requires HardwareCounters.CoreCount > 0 — completes the §3.3.2 hardware-snapshot triple) |
| `src/performance-optimization/BaselineRecord.cs` | sealed class: SessionManifest + Loop + P50Ms + P99Ms + PerMethodAllocBytes + PassFail + ThresholdCited — immutable baseline record per Appendix A / §4.3.2 |
| `src/performance-optimization/BudgetRollupEntry.cs` | readonly struct: SpecId / SubroutineName / Loop / BudgetMs / AllocBudgetBytes / Citation — one row in the §3.1.3 Appendix C roll-up table; produced by IBudgetSource |
| `src/performance-optimization/HotPathEntry.cs` | readonly struct: SpecId / MethodName / Loop / BudgetMs / HasAllocExemption — one entry in the §3.7.2 hot-path union set (materialised into tools/hot-path-union.json at Stage 0+1) |
| `src/performance-optimization/IPerfHarness.cs` | interface: BeginSession(manifest) / RecordTickSample(declared, actualMs, allocBytes) / FinalizeSession() → BaselineRecord; both producer (§3.3 harness) and consumer (#19 ScenarioRunner) specified per §4.3.1 / §4.4 |
| `src/performance-optimization/IBudgetSource.cs` | v1.1 — interface: SpecId property + GetEntries() → IReadOnlyList<BudgetRollupEntry> (AR-1 L-3 read-only return type); both producer (per-spec §6 extractor) and consumer (budget-auditor.py) specified per §4.4 |
| `src/performance-optimization/RegressionResult.cs` | readonly struct: PerPrPassed / AbsoluteDriftPassed / DeltaFraction / MilestoneDriftFraction / AllPassed — output of RegressionGate.Evaluate (FR-PO-031) |
| `src/performance-optimization/RegressionGate.cs` | v1.2 — static class: PassesPerPrCheck(baselineMs, currentMs) / PassesAbsoluteDriftCheck(milestoneMs, currentMs) / Evaluate(baseline, current, milestoneMs) → RegressionResult; implements FR-PO-031 §3.5.2 + §3.5.6. AR-1 M-1 Evaluate reuses helpers; AR-1 M-2 degenerate baseline (≤0/NaN) fails closed; AR-2 M-1 PassesAbsoluteDriftCheck NaN milestone returns true (skip-drift signal) aligning helper with Evaluate; non-NaN ≤0 milestone still fails closed |
| `src/performance-optimization/ReproducibilityResult.cs` | readonly struct: IsReproducible / OriginalP50Ms / RecapturedP50Ms / AbsDeltaFraction / ScenarioMatched / SeedMatched — output of BaselineReproducibilityAuditor.Validate (FR-PO-067) |
| `src/performance-optimization/BaselineReproducibilityAuditor.cs` | v1.1 — static class (AR-1 L-2 sealed→static): Validate(original, recaptured) → ReproducibilityResult; implements §3.4.4 / §5.4 / FR-PO-067 reproducibility check; AR-1 M-3 degenerate origP50 (≤0/NaN) fails closed; Stage 0 carve-out per §3.4.4 (MUST activates at Stage 0+1) |
| `src/performance-optimization/CertificationStatus.cs` | v1.0 — enum Pending/Certified (append-only ordinal stability; Pending=0 = safe default) for a perf baseline corpus entry's certification state (FR-PO-052 certified baseline machinery) |
| `src/performance-optimization/CertifiedPerfBaseline.cs` | v1.0 — certification-tagged perf baseline corpus entry: Pending(scenario, loop, platformPin, threshold) carries NO metric (NaN) and refuses TryBuildBaselineRecord (the Linux gate is NON-certifying — no fabricated number); Certified(manifest, loop, p50, p99, threshold) validates a complete manifest + finite positive metrics (p99≥p50, fail-closed) and projects to a corpus BaselineRecord. Platform-pin tokens Stage0CertPlatformPin (certification-platform.md v1.2 tuple) + LinuxNonCertPlatformPin |

### `src/testing-strategy/` — Spec #19 (28 files: 26 .cs + 2 asmdef)

> Infrastructure-only assembly (Spec #19). autoReferenced false; references TacticalDirector.DeterministicSim + TacticalDirector.PerformanceOptimization. Game-layer assemblies MUST NOT import this assembly at runtime. Scaffold added June 2, 2026; Stage 0 ScenarioRunner (closed-loop scenario harness, §3.3.3) added June 10, 2026.

| File | Purpose |
|------|---------|
| `src/testing-strategy/testing-strategy.asmdef` | Assembly definition; references TacticalDirector.DeterministicSim + TacticalDirector.PerformanceOptimization; autoReferenced false |
| `src/testing-strategy/TestingStrategyConstants.cs` | v1.4 — §3.10 governance constant catalogue (pyramid bounds, coverage thresholds, flake windows, pre-commit budget) + SCENARIO_MANIFEST_FORMAT_VERSION (FR-TS-070) + SCENARIO_PATH_CROSS_SPEC_PREFIX (§3.3.5 layout, AR-1 M-4) |
| `src/testing-strategy/TestLayer.cs` | enum: five-layer test taxonomy per §3.1.1 (Unit / Integration / Simulation / Determinism / EndToEndSoak) |
| `src/testing-strategy/TestTier.cs` | enum: Tier A/B/C classification mirror of #16 §1.1.1 (KD-1) |
| `src/testing-strategy/GoldenVectorKind.cs` | enum: discriminates the three golden-vector corpora pinned by #16 §9.5 #4 (a/b/c) |
| `src/testing-strategy/GoldenVectorEntry.cs` | Catalogue entry for one golden-vector corpus (kind + source path + citation) |
| `src/testing-strategy/GoldenVectorResult.cs` | Per-entry result from GoldenVectorRunner; one failed entry blocks FR-DS-009-GATE |
| `src/testing-strategy/GoldenVectorRunner.cs` | Catalogues the #16 §9.5 #4 corpora; per-entry runner surface for the CI determinism gate |
| `src/testing-strategy/DeterminismTierKind.cs` | enum: canonical tier order of #16 §5's regression suite (FR-TS-011 / FR-TS-018) |
| `src/testing-strategy/DeterminismTierResult.cs` | Per-tier outcome from DeterminismGate; failures block merges (KD-2 / FR-TS-012) |
| `src/testing-strategy/DeterminismSuiteResult.cs` | Aggregated DeterminismGate result; carried verbatim by ITestHarness.RunDeterminismTiers (FR-TS-016) |
| `src/testing-strategy/DeterminismGate.cs` | Single integration point invoking #16 §5's regression suite from Spec #19 (FR-TS-016) |
| `src/testing-strategy/PerfGateReport.cs` | PerfGateRunner result: wraps #18 RegressionResult with spec / loop / scenario context |
| `src/testing-strategy/PerfGateRunner.cs` | v1.2 — CI-side wrapper around #18 RegressionGate; rejects mismatched baseline pairs per FR-PO-031 (PR #132 Codex P2) |
| `src/testing-strategy/ScenarioStatus.cs` | enum: Passed / Failed / Quarantined per §3.3.3 (Quarantined is Stage 0+1 flake-layer only) |
| `src/testing-strategy/ScenarioResult.cs` | §3.3.3 result value: status + machine-readable diagnostics + durationMs + #16 §4.8 fingerprint |
| `src/testing-strategy/ScenarioManifest.cs` | In-memory Appendix A.1 manifest entry (name / owning_spec_ids / seed / tier / fixture_refs / format_version) + load-time field validation; Stage 0 in-code authoring (on-disk encoding pinned at Stage 0+1, D1) |
| `src/testing-strategy/ScenarioEnvelope.cs` | Executable expected_outcome_envelope: bodies record bounded predicate outcomes (CheckTrue / CheckEquals / CheckInRange); zero predicates ⇒ Failed (FR-TS-030); NaN values fail in_range, NaN bounds throw (AR-2 L-1) |
| `src/testing-strategy/ScenarioContext.cs` | Per-invocation body input: manifest + verbatim run seed + KD-7-seeded DeterministicRngService + envelope; declares ScenarioBody delegate |
| `src/testing-strategy/IScenario.cs` | §4.4.1 interface: single method ScenarioResult Run(ulong seed); both sides specified in #19 |
| `src/testing-strategy/ClosedLoopScenario.cs` | Standard IScenario for closed-loop scenarios: fresh RNG + context per run (hermetic, FR-TS-023), body drives a real subsystem loop, envelope evaluated (implicit pass forbidden), exceptions → Failed with diagnostic |
| `src/testing-strategy/ScenarioIndex.cs` | v1.1 — immutable in-memory root manifest; duplicate paths AND duplicate names rejected (AR-1 M-4: A.1 name uniqueness); the runner refuses unindexed scenarios (§3.3.6 / FR-TS-028) |
| `src/testing-strategy/ScenarioIndexEntry.cs` | One index row (path + manifest + scenario); extracted from ScenarioIndex.cs (AR-1 L-4); rejects a ClosedLoopScenario registered under a different manifest instance than it executes (AR-1 M-1) |
| `src/testing-strategy/ScenarioRunner.cs` | v1.1 — §3.3.3 single entry point Run(manifestPath, seed): index resolution + load-time validation (FR-TS-070 format version first, then A.1 fields, §3.3.5 path↔name coherence, cross-spec ≥2 owning-spec arity, non-empty fixture_refs refusal per §3.3.4 — AR-1 M-2/M-4/L-6) → delegates to IScenario.Run; Stage 0 index injected in code, Stage 0+1 adds the index.<ext> file loader (D1) |
| `src/testing-strategy/Tests/testing-strategy-tests.asmdef` | Test assembly definition (EditMode; references testing-strategy + deterministic-sim + ball-physics + pass-mechanics + event-system — the cross-spec corpus drives real #1/#5/#17 surfaces) |
| `src/testing-strategy/Tests/CrossSpecScenarios.cs` | Cross-spec closed-loop scenario corpus (KD-8; paths under SCENARIO_PATH_CROSS_SPEC_PREFIX, ≥ 2 owning specs per A.1): lofted-pass-kick-bounce-roll chains PassExecutor (#5) into BallPhysicsCore (#1) through the IPassBallSystem seam with #17 boot wiring + tick lifecycle around the CONTACT publish; owning specs {1, 5} |
| `src/testing-strategy/Tests/CrossSpecScenarioTests.cs` | sim_<scenario> Simulation-layer tests running the cross-spec corpus through ScenarioRunner |
| `src/testing-strategy/Tests/ScenarioRunnerTests.cs` | v1.2 — 19 ScenarioRunner contract tests: index refusal, format-version rejection, kebab-case validation, implicit-pass rejection, failure diagnostics, NaN in_range, exception + stack capture, seed plumbing (KD-7), per-invocation hermeticity, AR-1 locks (manifest coherence, fixture-refs refusal, newline flattening, duplicate-name / path↔name / cross-spec arity) |

### `src/tactical-instructions/` — Tactical Instructions input layer (Spec #21 T0, June 21, 2026)

> T0 scaffolding (the first landable slice of #21 §7.2): the bottom-of-graph data assembly — 16 enums + 3 aggregate structs + identity factories + 1 constant catalogue + 2 test files. Behaviour-neutral (KD-10): no consumer is wired and the identity factories reproduce today's no-instruction baseline. References only `TacticalDirector.ProjectConstants` per FR-TI-002, but that assembly does not exist yet and T0 consumes nothing from it, so the asmdef `references` array is empty until project-constants lands. Seams into #8/#11–#15 + the consumer-side `TacticTranslation` maps are T2–T3 (gated on match-engine Phase C/D + the `[GT]` config-loader).

| File | Purpose |
|---|---|
| `src/tactical-instructions/tactical-instructions.asmdef` | Assembly `TacticalDirector.TacticalInstructions`; empty `references` (project-constants not yet created); autoReferenced true |
| `src/tactical-instructions/Mentality.cs` | enum (byte, 7): VeryDefensive…VeryAttacking; indexes MentalityRiskMult/LineBias (§3.2) |
| `src/tactical-instructions/Tempo.cs` | enum (byte, 5): VerySlow…VeryFast; Standard (2) identity; NEW #8 branch (§3.3) |
| `src/tactical-instructions/TacticWidth.cs` | enum (byte, 5): VeryNarrow…VeryWide; Standard (2) identity; → #12 compactness |
| `src/tactical-instructions/TacticDefWidth.cs` | enum (byte, 3): Narrow/Standard/Wide; → #12 OOP compactness |
| `src/tactical-instructions/LineOfEngagement.cs` | enum (byte, 5): VeryLow…VeryHigh; → #13 trigger distances |
| `src/tactical-instructions/TransitionPlan.cs` | enum (byte, 4): CounterAttack/HoldShape/CounterPress/Regroup; overrides only the transition dimension (§3.2) |
| `src/tactical-instructions/GkDistributionPolicy.cs` | enum (byte, 6): SlowDown…ThrowOut; → #11 DistributeIntent defaults |
| `src/tactical-instructions/FocusPlay.cs` | enum (byte, 4): Mixed/LeftFlank/RightFlank/ThroughMiddle; NEW #8/#15 branch |
| `src/tactical-instructions/TacticPassing.cs` | enum (byte, 3): Short/Mixed/Direct; translated → #8 PassingStyle |
| `src/tactical-instructions/TacticPressing.cs` | enum (byte, 3): Low/Medium/High; translated → #8 PressingMode |
| `src/tactical-instructions/TacticTriggerMask.cs` | [Flags] enum (byte): None/BadTouch/BackwardPass/SidelineTrap/WeakReceiver; translated → #13 TriggerFlags |
| `src/tactical-instructions/TacticFormation.cs` | enum (byte, 3): F442/F433/F4231 (ordinals match #12 FormationFamily); translated → #12 |
| `src/tactical-instructions/Duty.cs` | enum (byte, 3): Defend/Support/Attack; indexes DutyForeOffsetM/DutyAggressionBias |
| `src/tactical-instructions/PlayerRole.cs` | enum (byte, 6): Default/Poacher/DeepLyingPlaymaker/BallWinningMid/InsideForward/TargetMan; behavioural role (KD-3, ≠ RoleId); indexes RoleWeightModifiers |
| `src/tactical-instructions/InstrBias.cs` | enum (byte, 3): Less/Default/More; indexes InstrBiasMult |
| `src/tactical-instructions/SetPieceDutyFlags.cs` | [Flags] enum (byte): None/FreeKickTaker/CornerTaker/PenaltyTaker (Stage 1+) |
| `src/tactical-instructions/MarkingOrientation.cs` | enum (byte, 3): BallOriented/Balanced/ManOriented; Balanced (1) identity; → #14 MAN_MARK candidate radius (cheap-item addition, FR-TI-033, July 7 2026) |
| `src/tactical-instructions/DismarkIntensity.cs` | #23 dial enum (byte, 3): Off(0, identity)/Conservative/Aggressive (ERR-021-005 back-prop) |
| `src/tactical-instructions/BuildUpStructure.cs` | #24 dial enum (byte): None(0, identity) + structure members (ERR-021-006 back-prop) |
| `src/tactical-instructions/RotationFreedom.cs` | #25 dial enum (byte, 3): Off(0, identity)/Conservative/Free (ERR-021-007 back-prop) |
| `src/tactical-instructions/TeamTactic.cs` | readonly struct (canonical Appendix B order: 17 fields + appended MarkingOrientation + the three #23/#24/#25 dials in pinned approval order) + `Balanced` identity factory (reproduces Stage0Default; FR-TI-031) |
| `src/tactical-instructions/TacticPreset.cs` | #26 §2.2.1 (FR-TP-001/014): immutable named tactic bundle (Name metadata-only + TeamTactic + optional roster-indexed PlayerTactic[], snapshot-copied at ctor); ValidatePlayers roster gate run by the consuming applier seam |
| `src/tactical-instructions/TacticPresetLibrary.cs` | #26 §2.2.2 (FR-TP-002/013): static APPEND-only catalogue — the five A.1 presets in pinned ladder order (ParkTheBus 0 … Gegenpress 4; ordinal = serialized identity = StepToward ladder position); Compose defaults = the Balanced identity values (KD-7) |
| `src/tactical-instructions/TacticalPresetsConstants.cs` | #26 §3.5 + Appendix A.2/A.3 catalogue: MANAGER_ARCHETYPE_COUNT + archetype ordinals [FIXED]; ManagerDecisionIntervalTicks/ManagerSwitchHoldIntervals/AdaptStepThreshold/UrgencyDiffCap [GT] via GameplayConfig; BaseFit/AggrAffinity/CautAffinity + archetype Aggression/Caution/Patience [GT] tables (literal per the array carve-out); MATCH_TICKS_TOTAL deliberately absent ([CROSS-PENDING], PASS-1 M-1) |
| `src/tactical-instructions/PlayerInstructions.cs` | readonly struct (per-agent individual instructions) + `Default` identity factory |
| `src/tactical-instructions/PlayerTactic.cs` | readonly struct (Role + Duty + Instructions) + `Default(role)` identity factory |
| `src/tactical-instructions/TacticalInstructionsConstants.cs` | single catalogue (Appendix A): Fixed (cardinalities + MARK_TARGET_NONE) / Derived (identity-row properties — expression-bodied to dodge static-init order) / GT (all [GT] tables, illustrative pending T2 balance pass) |
| `src/tactical-instructions/Tests/tactical-instructions-tests.asmdef` | Test assembly (EditMode; references the production assembly) |
| `src/tactical-instructions/Tests/EnumOrdinalStabilityTests.cs` | Locks all 16 enums' ordinals / bit-positions + byte-backing + 8-flag ceiling (FR-TI-007) |
| `src/tactical-instructions/Tests/FactoryIdentityTests.cs` | Locks the identity factories + catalogue identity rows + RoleWeightModifiers [0.5,2.0] (T-TI-U-029) + table dimensions (FR-TI-031) |
| `src/tactical-instructions/Tests/BalancePassInvariantsTests.cs` | §5.6/G2 balance-pass locks: identity-row exactness, strict monotonicity of the risk/line/width/engagement tables, RoleWeightModifiers ∈ [0.5,2.0] with the §3.3 directional shapes |
| `src/tactical-instructions/Tests/TacticPresetLibraryTests.cs` | #26 T0: pinned ladder order + A.1 composition locks (incl. inherited-dial == Balanced per preset) + KD-7 identity discipline + Players snapshot-copy regression |

### `tools/` — Stage 0 perf-gate tooling (Spec #18 Appendix E / FR-PO-070)

> Added June 1, 2026. All tools are Stage 0 deliverables per Appendix E. Stage 0+1 upgrades the harness from manual Stopwatch to automated benchmark per §3.3.5.

| File | Purpose |
|------|---------|
| `tools/run-perf-local.sh` | Stage 0 local pre-commit perf-gate runbook (Appendix E / FR-PO-070): runs budget-auditor.py schema + loop-tag passes, then invokes perf-harness/run.sh for anchor baselines; reviewer pastes output into PR description (FR-PO-071) |
| `tools/budget-auditor.py` | §5.3 schema-conformance auditor + §5.5 loop-tag auditor (FR-PO-070): walks every approved spec §6 against Appendix B template; reports missing sections and untagged ms values as ERR-018-NNN candidates; `--mode schema\|loop-tag\|all` |
| `tools/select-seed.py` | KD-6 deterministic seed selector: Stage 0 returns fixed dev seed; Stage 0+1 derives seed from git SHA + scenario ID via SHA-256 (8-byte truncation) |
| `tools/perf-harness/run.sh` | Stage 0 synthetic harness runner: parses scenario manifest, captures Stage 0 Stopwatch stub metrics, writes JSON baseline record under `docs/specs/performance-optimization/baselines/` per Appendix A §A.3 |
| `tools/perf-harness/scenarios/anchor-baseline.manifest.json` | Stage 0 anchor scenario manifest: PERF-ANCHOR-S0-001; validates JSON projection schema end-to-end; no gameplay code exercised |
| `docs/specs/performance-optimization/baselines/.gitkeep` | Stage 0 baseline storage root (Appendix A §A.3): JSON records land at baselines/\<spec-N\>/\<scenario\>-\<seed\>-\<sha8\>.json; migrates to tests/data/baselines/ at first src/ commit (FR-PO-074) |

---

### `tools/dotnet-ci/` — Non-certifying Linux compile/test gate (June 12, 2026)

| File | Purpose |
|---|---|
| `tools/dotnet-ci/README.md` | Gate rationale, first-run findings (8 never-compiled surfaces), shim fidelity rules, NOT-a-certification caveat |
| `tools/dotnet-ci/generate_projects.py` | asmdef → `*.gen.csproj` + `TacticalDirector.gen.sln` generator (generated files gitignored; asmdefs remain single source of truth; production netstandard2.1 / tests net8.0; LangVersion 9.0; AssemblyName = asmdef name; DEVELOPMENT_BUILD defined) |
| `tools/dotnet-ci/run-gate.sh` | Gate runner: generate → restore → build (errors block) → `dotnet test` excluding quarantine (any failure blocks) → report-only quarantined run |
| `tools/dotnet-ci/known-failures.txt` | Machine-readable quarantine ledger (30 entries from the first-ever suite execution; shrinking-only; mirrored in `docs/tracking/dotnet-ci-quarantine.md`) |
| `tools/dotnet-ci/LogAssertVerifyAssemblyInfo.cs` | Linked into every generated test project; applies the assembly-level LogAssert reset/verify action |
| `tools/dotnet-ci/UnityShim/UnityShim.csproj` | Shim assembly project (netstandard2.1) |
| `tools/dotnet-ci/UnityShim/Vector2.cs` | UnityEngine.Vector2 shim — Unity-exact approximate `==`, exact `Equals`, 1e-5 Normalize threshold |
| `tools/dotnet-ci/UnityShim/Vector3.cs` | UnityEngine.Vector3 shim — same semantics notes as Vector2 |
| `tools/dotnet-ci/UnityShim/Mathf.cs` | UnityEngine.Mathf shim — Unity NaN-propagation semantics (NaN-gate pattern depends on them), round-half-to-even RoundToInt |
| `tools/dotnet-ci/UnityShim/Debug.cs` | UnityEngine.Debug + LogType + ShimLog event spine (LogAssert observation seam) |
| `tools/dotnet-ci/UnityShim/Profiling.cs` | No-op UnityEngine.Profiling.Profiler + Unity.Profiling.ProfilerMarker |
| `tools/dotnet-ci/UnityShim.TestTools/UnityShim.TestTools.csproj` | TestTools shim project (separate so production assemblies gain no NUnit reference) |
| `tools/dotnet-ci/UnityShim.TestTools/LogAssert.cs` | UnityEngine.TestTools.LogAssert with UTF parity (unmet expectation / unexpected failing log fails the test) |
| `tools/dotnet-ci/UnityShim.TestTools/LogAssertVerifyAttribute.cs` | Assembly-level NUnit ITestAction applying the log contract per test |

---

### `src/match-engine/` — Match Engine composition root (Phase A + B June 16, 2026; **Phase C complete** C0–C3 June 19 / C4–C6 June 22, 2026; **Phase D steps D0–D1 + D2a + D3** June 22, 2026)

> Infrastructure/composition assembly — NOT a member of any gameplay layer; NOT covered by a formal spec (governance anchor: `docs/tracking/match-engine-design.md`). Drives the deterministic-sim `TickOrchestrator` 7-phase pipeline. References `TacticalDirector.DeterministicSim` + `TacticalDirector.EventSystem` + `BallPhysics` + `AgentMovement` (B2) + `CollisionSystem` + `FirstTouch` (D3) + `PassMechanics` + `ShotMechanics` (C1) + `DecisionTree` (C4, for `MatchContext` / `PitchGeometry`) + `PositioningAI` (D2a, for the per-team formation tick); remaining game-layer references (Pressing #13 / Defensive #14 / Attacking #15 at D2b) land with Phases D–F. autoReferenced true. Game-layer assemblies MUST NOT reference match-engine back. (The D0 DecisionTree snapshot seam lives in `src/decision-tree/`; the match-engine consumes it at the Phase D snapshot extension. First touch grants the host `InternalsVisibleTo` so it can run the internal `PressureEvaluator` / `OrientationDetector` context-assembly seams — D3.)

| File | Purpose |
|------|---------|
| `src/match-engine/match-engine.asmdef` | Assembly definition; references DeterministicSim + EventSystem + BallPhysics + AgentMovement + CollisionSystem + FirstTouch (D3) + PassMechanics + ShotMechanics + PerceptionSystem + DecisionTree + PositioningAI (D2a) + PressingAI/DefensiveAI/AttackingAI (D2b) + TacticalInstructions (#21) + ProjectConstants + PlayerDatabase (#27 T1) |
| `src/match-engine/AssemblyInfo.cs` | InternalsVisibleTo("TacticalDirector.MatchEngine.Tests") |
| `src/match-engine/MatchEngineConstants.cs` | [FIXED]/[DERIVED]/[GT] catalogue: SQUAD_SIZE / TEAM_COUNT / PLAYERS_PER_TEAM, kickoff coordinate constants (Ball Physics #1 §1.2 corner-origin), NO_POSSESSION sentinel, STAGE0_NEUTRAL_* executor-adapter proxies, PERCEPTION_GRID_POINT_INSERT_RADIUS (D1 broad-phase point insert), MaxEntityId + STAGE0_FORMATION + STAGE0_TACTICAL_INTENSITY (D2a Positioning AI inputs), FIRST_TOUCH_ACCEPTANCE_RADIUS_M + FIRST_TOUCH_MIN_BALL_SPEED_M_S (D3 first-touch trigger gates), SNAPSHOT_SCHEMA_VERSION (u32 = 15; world-state field-set pin — distinct from the #16 SnapshotHeader schema version), MATCH_LENGTH_MINUTES [FIXED] + MATCH_TICKS_TOTAL (= 324 000; the #26 §3.5 [CROSS] authority) + HALF_TIME_BOUNDARY_TICK [DERIVED] (the FR-TP-019 Stage-0 halves model — boundary only), GOAL_AREA_DEPTH_M [FIXED] + SUBSTITUTES_PER_TEAM/MAX_SUBSTITUTIONS_PER_TEAM [FIXED] (match-flow completion §5/§6) + FoulImpactForceThresholdN/RedCardProbability/YellowCardProbability/FoulCooldownTicks [GT] (§3) |
| `src/match-engine/MatchEngine.cs` | Sealed composition root: boot (seed → DeterministicRngService, clock/codec/fingerprint, AgentMovementSystem, CollisionSystem + per-agent PassExecutor[22]/ShotExecutor[22] + adapters, Pass/Shot EventBusRegistrar boot, real BallState + AgentState[] kickoff world state + buffers + MatchContext), 7 method-group phase callbacks driving the EventBus lifecycle + digest-load-bearing snapshot serialization. B2: Physics drives BallPhysicsCore + AgentMovementSystem.UpdateAllAgents (skips GKs). B3: full §2.6 AgentState/Ball field set incl. OscillationGuard. C2/C3: Resolve drives CollisionSystem.UpdateCollisions + the 22 pass + 22 shot executor lifecycles via the PassWorldAdapter/ShotWorldAdapter. C4: UpdateMatchContext authors MatchContext (possession state, home-perspective BallZone) at the end of Resolve. C5: SerializeWorldState adds the per-agent C0 executor capture + MatchContext (schema v2). D1: RunAiPhase drives a host-owned perception SpatialHashGrid + PerceptionSystem.OnHeartbeat ×22 → 22 per-agent DecisionTree.ReceiveSnapshot, dispatching MovementCommands into _commands (HostMovementController) / PASS-SHOOT into the executors; Stage-0 static AI input snapshots assembled at boot (InitializeAiSnapshots); DecisionTree EventBusRegistrar booted (DecisionMadeEvent Tier C, excluded from digest). D2a: RunAiPhase runs RunPositioningAI before the DT loop — one PositioningAITick + reused PositioningPerceptionSnapshot per team (seeded at boot from STAGE0_FORMATION), filled from world state and ticked, with GetFormationSlot folded back into each agent's TacticalContext (the DT MOVE_TO_POSITION / HOLD anchor); the away team is mapped through the canonical attack-+X frame and back via the self-inverse 180° MirrorPitchIfAway (ERR-008-002 guard). D3: RunResolvePhase calls RunFirstTouch after the executor Update (C3) and before UpdateMatchContext (C4) — a loose, ground-level, moving ball arriving within FIRST_TOUCH_ACCEPTANCE_RADIUS_M of the nearest APPROACHING agent triggers BuildFirstTouchContext (real PressureEvaluator pass over the opposing team via _opponentScratch + OrientationDetector half-turn flag; ERR-007 neutral touch attributes) → FirstTouchSystem.EvaluateFirstTouch/ApplyTouchResult through the FirstTouchWorldAdapter (IBallPhysicsSystem → _ball; IAgentMovementSystem → Stage-0 dribbling no-op); the outcome maps onto possession (CONTROLLED → toucher, INTERCEPTION → interceptor id (AGENT_ID_NONE at Stage 0 → loose), LOOSE_BALL/DEFLECTION → loose). Snapshot schema unchanged (FirstTouchSystem stateless). #21 T2 runtime activation: per-team `_active`/`_pendingTeamTactics` (default `TeamTactic.Balanced`); public `SetTeamTactic(teamId, in TeamTactic)` stages pending; RunAiPhase commits pending→active at the stride boundary (FR-TI-027); RunMechanicsAI overlays the active tactic's Mentality (→ #8 UtilityScorer risk mult) + translated Pressing/Passing (TacticTranslation) into each TacticalContext. Balanced = MEDIUM/MIXED/×1.0 = Stage0Default (behaviour-neutral; tactic arrays NOT serialized → no schema bump; mid-match change not yet restore-deterministic, ERR-021-002). TestOnly_Mentality/Pressing/Passing seams added. #13 Phase-D writer (v1.18): FillPressingSnapshot routes the pressing team's active TeamTactic.LineOfEngagement → PressingSnapshot.LineOfEngagement (overwriting the ctor Standard seed; PrimaryPressSelector scales its trigger radius by PressingAI.TacticTranslation; Balanced ⇒ Standard ⇒ ×1.0 byte-identical). TestOnly_PressLineOfEngagement seam added. #14/#15 Phase-D writers (v1.19): FillDefensiveSnapshot routes the active TeamTactic.OffsideTrap → DefensiveSnapshot.OffsideTrapRequested via fully-qualified DefensiveAI.TacticTranslation (CS0104 — five TacticTranslation types in scope); FillAttackingSnapshot routes the active TeamTactic.FocusPlay → AttackingSnapshot.FocusPlay (enum passthrough). Balanced ⇒ false / Mixed = routing identities (byte-identical); active consumption deferred (#14 KD-9, #15 §5.6/G2). TestOnly_OffsideTrapRequested / TestOnly_FocusPlay seams added. #12 Phase-D writer (v1.20, last of the three Mechanics writers): RunMechanicsAI builds ContextModifierInputs via the 5-arg ctor, routing the active TeamTactic.Width / DefensiveWidth (ContextModifier translates to the lateral-compactness scalar). Balanced ⇒ Standard / Standard ⇒ ×1.00 byte-identical (5-arg both-Standard ≡ 3-arg identity ctor). The modifier struct is a per-tick input captured per-team in _posModifiers only for the TestOnly_PositioningWidth / TestOnly_PositioningDefWidth seams; no schema bump. #21 §3.3 (v1.21): RunMechanicsAI routes the active team Tempo into TacticalContext (per-option §3.3 UtilityScorer product); per-agent PlayerTactic stays the Stage0Default identity. ERR-021-002 resolved (v1.22): SerializeWorldState writes both the active+pending per-team TeamTactic via WriteTeamTactic (Appendix B order); SNAPSHOT_SCHEMA_VERSION 8 → 9 — a mid-match tactic change is now restore-deterministic. Public observation surface (v1.24): BallView / AgentView(i) / AgentTeamId(i) / AgentIsGoalkeeper(i) / PossessingAgentId — read-only value-type COPIES for the presentation layer (`src/match-viewer/` recorder); no live-buffer reference escapes, no behaviour change. Engine substrate (v1.30): Resolve-phase CheckGoalAndRestart (BallCollision.CheckBoundaries ⇒ KickOff = goal; scoring team by exit half-space geometry; per-team _goals++ + Tier A GoalAwardedEvent 0x07 with the _lastHolderAgentId scorer credit + centre-spot restart; non-goal exits untouched — no throw-in/corner model); RunManagerDecisionPoints passes the #26 ladder LIVE goalDiff (v14 score) + ticksRemaining/MATCH_TICKS_TOTAL; SNAPSHOT_SCHEMA_VERSION 13 → 14 (goals + last-holder serialized); TestOnly_Goals/SetGoals/LastHolderAgentId/RunManagerDecisionPoints seams. Match-flow completion (v1.31): CheckRestartAndApply (renamed/extended from CheckGoalAndRestart) routes non-goal exits through RestartResolver + a shared ApplyRestart primitive, publishing a Tier A RestartAwardedEvent; MatchFlowCollisionConsumer (replaces the former no-op NullCollisionEventConsumer) captures at most one FROM_BEHIND high-force cross-team foul candidate per tick against a new `match-flow.card-severity` RNG stream, via DetermineCardKind (pure band lookup) + ApplyCardAndCheckSentOff (promotion/sent-off logic) + ApplyFoulIfCaptured (publishes FoulCommittedEvent/CardIssuedEvent, arms the cooldown, awards a free kick); `_yellowCards`/`_isSentOff` discipline state forces a Stop command every Physics tick and excludes the agent (`IsActive = false`) from all four Mechanics-AI snapshot fill sites (#12/#13/#14/#15); EvaluateAndApplyOffside (OffsideEvaluator-backed) hooks into RunFirstTouch's Controlled case for genuine same-team pass receptions, publishing OffsideCalledEvent on a violation; public SubstitutePlayer (bench-roster identity swap, cap-enforced at MAX_SUBSTITUTIONS_PER_TEAM, queues a SubstitutionEvent flushed at the top of the next RunResolvePhase — AR-5, since SubstitutePlayer may be called between ticks when EventBus.CurrentPhase is not a valid producer phase) + PublishPendingSubstitutions; CheckMatchFlowTransitions (called every RunInputPhase, not stride-gated) fires the half-time transition once at HALF_TIME_BOUNDARY_TICK (ball reset to centre spot only — no ends-swap; `team 0 attacks +X` is hardcoded across goal detection/offside/Mechanics-AI, so a full ends-swap is a documented Stage-1+ deferral, AR-4) and the full-time transition once at MATCH_TICKS_TOTAL (`_matchEnded` freezes RunAiPhase/RunPhysicsPhase/RunResolvePhase while the tick/snapshot loop keeps advancing), both publishing MatchPhaseChangedEvent. SNAPSHOT_SCHEMA_VERSION 14 → 15 (per-agent yellow-card count + sent-off flag, the global foul cooldown, per-agent active bench slot, per-team substitutions-used count, half-time/full-time fired flags). New TestOnly_* seams: YellowCards/IsSentOff/FoulCooldownRemaining/ActiveBenchSlot/SubstitutionsUsed/SecondHalfStarted/MatchEnded/SetTeamId/SetIsSentOff/SetBenchSlot/EvaluateAndApplyOffside/InjectFoulCandidate/DetermineCardKind/ApplyCardAndCheckSentOff/CheckMatchFlowTransitions. #27 T1/T2 (v1.37): canonical `_canonicalAttrs`/`_benchCanonicalAttrs` player records (default CreateDefault; NOT serialized — B3 exclusion proof extended with the KD-P10 distinct-squad restore scope); every attribute-seeding site converted to PlayerAttributeProjection reads (starter/bench #2 attrs, #8/#7 AI snapshots, Pass/Shot builders, the three FirstTouchAbility sites, FirstTouchContext.Technique, Attacking pace/dribbling); public ConfigureSquads (pre-kickoff gate, Stage-0 roster-order lineup, both squads validated before any write); SubstitutePlayer copies the canonical bench record + re-projects _dtAttrs/_perceptionAttrs; +6 TestOnly attribute seams (Canonical/Movement/Dt/Perception/Pass/ShotAttributes). Default path byte-identical (KD-P7); no schema change. |
| `src/match-engine/TeamTacticConfig.cs` | #21 T2 manager-tactic config source: immutable per-team TeamTactic (index = teamId 0 home / 1 away); `Default` = Balanced for every team (FR-TI-031 behaviour-neutral); ForTeam(teamId) with bounds guard. Authored in code (Default/ctor) or from the on-disk Stage-0 text format via TeamTacticFileLoader.Parse (the parser swap, #19 ScenarioIndex precedent); the Stage-1 [GT] loader (FR-CS-019) may replace the grammar leaving Apply untouched |
| `src/match-engine/TeamTacticConfigApplier.cs` | #21 T2 boot applier: static Apply(engine, config) stages every team's tactic into MatchEngine.SetTeamTactic once per team before kickoff (committed at the first AI-stride boundary, FR-TI-027); null-guards both args; applying TeamTacticConfig.Default is behaviour-neutral. The boot-time seam TeamTacticFileLoader feeds (parses a file → TeamTacticConfig → Apply unchanged) |
| `src/match-engine/TeamTacticFileLoader.cs` | #21 on-disk tactic-file loader: Parse(text) → TeamTacticConfig over a line-oriented case-insensitive `key = value` grammar under [home]/[away] headers + `#` comments; omitted key inherits the Balanced identity (empty/null ⇒ Default ⇒ behaviour-neutral); unknown key/section, unparsable value, duplicate key, out-of-range TimeWasting all throw FormatException (fail loud). Stage-0 human-authoring text format (NOT a determinism-pinned wire format — only the resulting TeamTactic values enter the digest via v9); the parser swap TeamTacticConfig/Applier were authored to receive |
| `src/match-engine/PlayerAttributeProjection.cs` | #27 T1/T2 (player-attribute-projection-design.md §3): pure static canonical→per-spec projections — ToAgentMovement/ToDecisionTree/ToPerception raw copies with caller-supplied runtime TeamId/IsHalfTurned (KD-P4); ToPass/ToShot with the KD-P1 derived KickPower ((Passing+Technique)×.5 / Mathf.RoundToInt((Finishing+LongShots)×.5)); FirstTouchAbility for the three #13/#14/#4 sites (KD-P9); ToNormalized = ÷ATTRIBUTE_MAX for the sole pre-normalized target (KD-P3). Canonical type fully qualified throughout (KD-P6 CS0104 discipline); no GK/Heading projections (KD-P8 — MatchEngine builds neither; forward-compat mappings stay in the design doc). Neutral record projects to every pre-T1 STAGE0 seed exactly (KD-P7) |
| `src/match-engine/PlayerTacticFileLoader.cs` | #21 §3.3 per-agent on-disk tactic-file loader (sibling of TeamTacticFileLoader): Parse(text) → PlayerTacticConfig over a line-oriented case-insensitive `key = value` grammar under [agent N] headers (N = roster index 0..SQUAD_SIZE−1) + `#` comments; every PlayerTactic/PlayerInstructions field has a key (role/duty/riskyPasses/shootTendency/dribbleTendency/crossTendency/positioningFreedom/closeDown/tightMarking/markTarget/setPieceRoles); omitted key/section inherits the PlayerTactic.Default(PlayerRole.Default) identity (empty/null ⇒ PlayerTacticConfig.Identity ⇒ behaviour-neutral); unknown key/section, out-of-range or non-numeric agent index, unparsable value, duplicate key, duplicate section all throw FormatException (fail loud). Stage-0 human-authoring text format (only the resulting PlayerTactic values enter the v10 digest); the parser swap PlayerTacticConfig/Applier were authored to receive |
| `src/match-engine/ManagerMode.cs` | #26 §2.2.4 enum (byte, APPEND-only, serialized at v13): Human(0, zero-value inert identity — KD-4) / AI(1, opts a team into the manager-AI decision loop) |
| `src/match-engine/ManagerProfile.cs` | #26 §2.2.3 readonly struct: Aggression/Caution [0,1] (F4 NaN-gated at ctor) + PatienceIntervals ≥ 1; FromArchetype(ordinal) reads the A.2 tables with the F2 out-of-range gate |
| `src/match-engine/ManagerState.cs` | #26 §2.2.4 per-team persistent manager state (Mode/ProfileOrdinal/CurrentPresetOrdinal/HoldIntervalsRemaining/LastDecisionTick); zero-init = Human = inert; serialized per team in Appendix C order at v13 (FR-TP-012); LastDecisionTick < 0 = kickoff decision not yet fired |
| `src/match-engine/ManagerDecisionGate.cs` | #26 §3.2 FM-TP-02 pure tick-predicate (KD-3 — a gate, not a clock file): Mode == AI AND (first-ever evaluation OR interval elapsed); evaluated only inside RunAiPhase's stride branch before the FR-TI-027 commit (FR-TP-018; off-stride firing impossible, F5); half-time trigger ACTIVE as of v1.1 (fires once at the first stride evaluation at/after MatchEngineConstants.HALF_TIME_BOUNDARY_TICK, regardless of interval position — FR-TP-019; no clock state beyond LastDecisionTick) |
| `src/match-engine/TacticPresetProjection.cs` | #26 §3.1 FM-TP-01 pure projection: preset → (TeamTacticConfig, PlayerTacticConfig) for ONE managed team (other team keeps its own value; Players fills the managed roster block, else identity); the FR-TP-014 roster gate runs here (the consuming applier seam); no engine call |
| `src/match-engine/ManagerAdaptation.cs` | #26 §3.3/§3.4 manager logic: KickoffScore/SelectKickoffPreset (FM-TP-03, argmax + tie → lowest ordinal per KD-8; B.1 exact), StepToward + EvaluateLadder (FM-TP-04 one-rung saturating ladder, URGENCY_DIFF_CAP; B.2 exact; matchTicksTotal an explicit param, supplied live from MatchEngineConstants.MATCH_TICKS_TOTAL by the engine's decision-point seam), RunDecisionPoint (FR-TP-005 mid-match path via SetTeamTactic/SetPlayerTactic — never the appliers, F3; decrement-then-check hold per the B.2 cadence), ApplyKickoff (FR-TP-004 boot path via the existing appliers; seeds LastDecisionTick = 0) — signatures admit no opponent input (KD-5, FR-TP-008) |
| `src/match-engine/tests/ManagerAITests.cs` | #26 T1–T4 suite (21 tests): B.1/B.2 worked examples exact, gate arithmetic (kickoff-fires-once + interval), exact-float tie → lowest ordinal, FM-TP-01 projection + F1/F2/F4 fail-loud gates, hold cadence + Patience multiplier through the real mid-match path, boot-path routing (single AI / two AI / AI-vs-Human baseline preservation), in-engine first-stride kickoff fire, Human-identity digest lock (T-TP-DET-002), two-AI two-run bitwise determinism (T-TP-DET-001); v1.1 +4: half-time gate arithmetic (fires once, regardless of interval position) + half-time through the engine's live seam + live-goalDiff urgency (trailing → Possession) and protect (leading → CounterAttack) steps committed at the stride |
| `src/match-engine/tests/match-engine-tests.asmdef` | Test assembly definition (EditMode; references match-engine + deterministic-sim + event-system + ball-physics + agent-movement + pass-mechanics + shot-mechanics + decision-tree + positioning/pressing/defensive/attacking AI + perception-system + testing-strategy + performance-optimization (Phase F) + tactical-instructions (#21 T2)) |
| `src/match-engine/tests/MatchEngineDeterminismTests.cs` | Phase A capstone: two same-seed runs → byte-identical snapshot digest chains; chain non-degenerate + advances; AI phase fires only on AI_PHASE_STRIDE ticks; first processed tick is 1 / first AI tick is stride |
| `src/match-engine/tests/MatchEnginePhysicsTests.cs` | Phase B step B2 + Phase D D1: dropped-ball integration through the real loop; same-seed determinism with live ball + agent + AI dynamics; AiPhase_DrivesChain_GoalkeepersSkipped (D1 — the AI chain runs ×22/stride over a 2 s run without throwing and both goalkeepers stay byte-exact; supersedes the B2 injected-WalkTo test now that the AI owns _commands) |
| `src/match-engine/tests/MatchEngineSnapshotSchemaTests.cs` | Phase B step B3 + Phase D D4 + #21 + #23/#24/#25 + #26 + engine substrate + match-flow completion: SNAPSHOT_SCHEMA_VERSION pin (15); OscillationGuard-state + ball-spin + DT/positioning/pressing/defensive/attacking/perception + TeamTactic/PlayerTactic/MarkingOrientation + DismarkBuildUpRotationDials + BuildUpSettledTeamAndSuppression + ManagerState + ScoreState + MatchFlowCompletionState (sent-off flag) + SubstitutionState (bench-slot/count bookkeeping) preimage probes; locked-guard same-seed determinism |
| `src/match-engine/tests/MatchEngineGoalTests.cs` | Engine-substrate goal-detection suite (6 tests): both goal mouths score the correct team (exit-geometry classification — own goals credit the right side) + centre-spot restart; non-goal (outside posts) and airborne (z-gate) exits leave score + ball untouched; last-holder scorer credit; two-run bitwise determinism with a goal in the run |
| `src/match-engine/tests/TeamTacticFileLoaderTests.cs` | #21 loader tests: round-trips the text grammar onto TeamTactic fields, empty/comment-only/null ⇒ Balanced identity (behaviour-neutral), fail-loud cases (unknown key/section, bad enum, duplicate key, key-before-section, out-of-range TimeWasting), parsed config fed through Apply and routed per team |
| `src/match-engine/tests/PlayerTacticFileLoaderTests.cs` | #21 §3.3 per-agent loader tests: round-trips the [agent N] grammar onto PlayerTactic/PlayerInstructions fields (omitted key/section ⇒ identity), empty/comment-only/null ⇒ PlayerTacticConfig.Identity (digest-chain behaviour-neutral when applied), fail-loud cases (key-before-section, unknown/out-of-range/non-numeric section, unknown key, bad enum, bad markTarget, duplicate key/section, no-`=` line), parsed config fed through Apply and routed per agent |
| `src/match-engine/tests/MatchEngineResolveTests.cs` | Phase C C1/C1a/C2/C3: collision separates an overlapping pair in Resolve; same-seed determinism with a live collision; scripted pass/shot initiates through the executor adapters and advances one tick (below CONTACT) |
| `src/match-engine/tests/MatchEngineMatchContextTests.cs` | Phase C C4/C5: home-perspective ball-zone authoring; loose=CONTESTED + possessing-agent-team derivation; scripted ground pass reaches CONTACT, releases possession, kicks the ball; same-seed determinism with a live CONTACT publish; C5 digest-preimage probes for MatchContext + executor state |
| `src/match-engine/tests/MatchEngineMechanicsTests.cs` | Phase D D2a (Positioning AI #12): formation slots feed the decision context (home defender deep / striker advanced, on-pitch); away-team slots mirror the home team (exact GK pitch-mirror — ERR-008-002 guard); same-seed determinism of the slot output |
| `src/match-engine/tests/MatchEngineFirstTouchTests.cs` | Phase D D3 (first touch): a loose, ground-level, approaching ball is received → CONTROLLED gains possession (home + away, proving first-touch is frame-agnostic); receding / above-control-height / already-possessed balls are not touched; a scripted receive is byte-identical across two same-seed runs |
| `src/match-engine/tests/MatchEnginePlayDevelopmentScenarios.cs` | §5.Z Phase H acceptance scenario (#19 ScenarioRunner): `match-engine-play-develops` (owning specs {1,2,3,4,5,6,7,8,12,13,14,15,16,17,19}, Tier B) runs 6 seeds × 32 400 ticks (9 min each) and asserts what no prior test did — the ball is KICKED and goes airborne, possession is HELD (≥5% of ticks) and CHANGES HANDS (≥50 times), **play is still alive at the final tick**, and across the seed spread the ball reaches BOTH penalty areas and a non-zero scoreline is produced; plus a two-run byte-identical digest chain over 6 000 ticks of live play. Every predicate fails on the pre-Phase-H engine (ERR-030-014) |
| `src/match-engine/tests/MatchEnginePlayDevelopmentTests.cs` | Runs the Phase H acceptance scenario through `ScenarioRunner.Run` (`sim_match_engine_play_develops`, Simulation layer) |
| `src/match-engine/tests/MatchEnginePossessionBootstrapTests.cs` | §5.Z Phase H per-seam unit locks (11): the kickoff/restart taker award incl. nearest-eligible selection and sent-off exclusion (KD-H1); the loose-ball pickup incl. reach gate, sent-off exclusion and disjointness from first touch (KD-H3); the loose-ball collector designation incl. sent-off exclusion, one-per-team and the possessed-ball identity (KD-H5); and the DecisionTree PASS/SHOOT completion sweep, which releases a blocking action and leaves a continuous one alone (KD-H4) |
| `src/match-engine/tests/GkSaveDiagnosticTests.cs` | §5.Z.17 goalkeeper save-pipeline diagnostic (env-gated `TD_GK_DIAGNOSTIC=1`, assertion-free). The first instrument in the tree to report a goalkeeper statistic of any kind. Walks the save pipeline as a FUNNEL (`armed → SAVE committed → Anticipate → Diving → Airborne → contact → caught`) over full matches so the stage that collapses localises the defect, plus the per-state tick histogram, the dive miss-distance / dive-direction probe, and the reaction-window and handling-quality distributions at contact. A second test reports the ARITHMETIC ceiling on handling quality, which decides whether the residual is `[GT]` tuning or a fix |
| `src/match-engine/tests/MatchEngineGoalkeeperSaveScenarios.cs` | §5.Z.17 goalkeeper save acceptance scenario (#19 ScenarioRunner): `match-engine-goalkeeper-saves` (owning specs {2,6,11,12,16,19}, Tier B) runs 4 seeds × 54 000 ticks and asserts the save pipeline's REACHABILITY stage by stage — keeper notified of shots, does not live in Anticipate (per keeper, since the defect it locks was per-side), dives, dives DIRECTED, and makes hand contact. 11 of its 12 predicates fail on the pre-fix engine, three at exactly zero. Deliberately pins no save percentage and no goal rate |
| `src/match-engine/tests/ShotOutcomeDiagnosticTests.cs` | §5.Z.18 shot-outcome-distribution diagnostic (env-gated `TD_SHOT_DIAGNOSTIC=1`, assertion-free). Reports per full match: shots (keeper shot-notification rising edges), goals and goals/shot, on-target goal-mouth crossings, off-target exits, fast-ball body contacts against the KD-6 deflection gate, and the shot-tick ball-speed range. Measured the pre-fix baseline (15.3 goals/match, 0 fast contacts) and the post-fix distribution (12.3, 560–612). v1.4: strike-time sampling via `TestOnly_LastShotStrike*` (keeper-contact AR-4) |
| `src/match-engine/tests/MatchEngineShotOutcomeScenarios.cs` | Shot-outcome acceptance scenario (#19 ScenarioRunner): `match-engine-shot-outcomes` (owning specs {1,3,6,8,16,19}, Tier B, 4 seeds × 32 400 ticks) — scripted-stimulus airborne-adjudication probes (over-bar = out not a goal; under-bar airborne = goal), natural-play deflection reachability, goals under a loose sanity ceiling, sequential two-run digest determinism. 3 of 8 predicates fail on the pre-fix engine, verified by execution |
| `src/match-engine/tests/MatchEngineShotSpeedScenarios.cs` | Shot-speed/woodwork acceptance scenario (#19 ScenarioRunner): `match-engine-shot-speed` (owning specs {1,6,8,16,19}, Tier B, 2 seeds × 64 800 ticks + scripted frame probes) — natural-play speed floors (mean ≥ 14 / max ≥ 20 m/s) + mean-shot-distance ceiling (24 m), front-face post + crossbar rebound probes, the rising crossing-point adjudication probe, sequential two-run digest determinism. 5 of 7 predicates fail on the pre-fix engine, verified by execution against the unmodified tree. v1.3: strike-time sampling via `TestOnly_LastShotStrike*` + 18 min/seed windows (keeper-contact AR-4) |
| `src/match-engine/tests/MatchEngineShotSpeedTests.cs` | Runs the shot-speed acceptance scenario through the #19 ScenarioRunner (sim_ layer) |
| `src/match-engine/tests/MatchEngineShotOutcomeTests.cs` | Runs `match-engine-shot-outcomes` through the #19 ScenarioRunner (`sim_` layer) |
| `src/match-engine/tests/MatchEngineGoalkeeperSaveTests.cs` | Runs the §5.Z.17 goalkeeper save acceptance scenario through the #19 ScenarioRunner (Simulation layer, `sim_<scenario>`) |
| `src/match-engine/tests/MatchEngineKeeperConversionScenarios.cs` | §5.Z.20 keeper catch/parry-conversion acceptance scenario (#19 ScenarioRunner): `match-engine-keeper-conversion` (owning specs {2,6,11,12,16,19}, Tier B, 2 seeds × 45 min on the `ConfigureSquads` path — the neutral-path draft's hold predicate failed (the §5.Z.19 AR-4 population-transfer class) and the corpus is sized from the funnel's measured per-contact tick positions) — the frozen dive reaction window is alive (ERR-011-005/006), a contact converts to the parry band, the keeper holds a ball. No rate pins |
| `src/match-engine/tests/MatchEngineKeeperContactScenarios.cs` | gk-contact-rate acceptance scenario `match-engine-keeper-contact` (Tier B, 2 seeds × 45 min): the ERR-011-007 hold is alive, contacts outnumber un-contacted crossings, no deep dive-early miss — 3 of 4 predicates fail pre-fix, verified by execution |
| `src/match-engine/tests/MatchEngineKeeperContactTests.cs` | Runs the gk-contact-rate acceptance scenario through the #19 ScenarioRunner |
| `src/match-engine/tests/GkContactRateDiagnosticTests.cs` | Env-gated (TD_GK_DIAGNOSTIC=1) per-episode contact-rate anatomy instrument: classifies every goalward threat episode at the goal-plane crossing (contact / no-dive / dive-early / dive-late / lateral-miss / faded) — the measurement that attributed §5.Z.22's two levers |
| `src/match-engine/tests/MatchEngineKeeperConversionTests.cs` | Runs `match-engine-keeper-conversion` through the #19 ScenarioRunner (`sim_` layer) |
| `src/match-engine/tests/MatchEngineDisciplineScenarios.cs` | §5.Z.9 foul/discipline acceptance scenario (#19 ScenarioRunner): `match-engine-discipline-plausible` (owning specs {1,2,3,16,17,19}, Tier B) runs 6 seeds × 32 400 ticks and asserts what no prior test did — foul / yellow / red rates in football-plausibility BANDS (not pins), **no team reduced below nine players** asserted PER SEED (one abandoned match must not average away), and cards a minority of fouls. 9 of its 10 predicates fail on the pre-balance engine, each by more than an order of magnitude |
| `src/match-engine/tests/MatchEngineDisciplineTests.cs` | Runs the §5.Z.9 discipline acceptance scenario through `ScenarioRunner` (Simulation layer, `sim_<scenario>` per #19 §3.1.4) |
| `src/match-engine/tests/FoulRateDiagnosticTests.cs` | The §5.Z.9 MEASUREMENT instrument (env-gated `TD_FOUL_DIAGNOSTIC=1`; asserts nothing about the rate — pinning it is the acceptance scenario's job). Attaches an observer to every collision event, records the per-tick peak cross-team FROM_BEHIND force, and replays the foul gate OFFLINE across a (threshold, cooldown, call-probability) ladder, so one composed run yields the whole rate curve — necessary because the `[GT]` constants are `static readonly` and an in-process sweep of the real gate is impossible |
| `src/match-engine/tests/MatchBalanceDiagnosticTests.cs` | Env-gated (`TD_BALANCE_DIAGNOSTIC=1`) characterisation of the §5.Z.10 scoreline asymmetry the KD-8 Step 0 pilot exposed (away team scoring 0 in twenty full matches, home totals an order of magnitude above football). Reports per-team possession share against where the ball spends its time, which separates "the away team never reaches the goal it attacks" from "it reaches it and the goal is not credited". Asserts nothing |
| `src/match-engine/tests/MatchEngineCapstoneScenarios.cs` | Phase F capstone scenario corpus (#19 ScenarioRunner): `match-engine-kickoff-multi-second` (owning specs {1,2,3,4,5,6,7,8,12,13,14,15,16,17,19}, Tier B) boots a real MatchEngine and ticks it 600× (10 s @ 60 Hz); records gameplay-invariant predicates (tick-count; ai-stride-cadence = NumTicks/AI_PHASE_STRIDE = 100; ball + agents finite and on-pitch every tick; chained snapshot digest advances) + a two-run same-seed determinism digest match. Reads world state via the existing internal TestOnly_* seams + public CurrentTick/AiPhaseRunCount/CurrentSnapshotDigest (no production change) |
| `src/match-engine/tests/MatchEngineCapstoneTests.cs` | Phase F capstone tests: runs the kickoff scenario through ScenarioRunner.Run → Passed; a direct two-run same-seed digest-chain equality test (re-locks EventBus.ResetForNewMatch across two in-process matches); FR-PO-052 per-tick perf-gate activation — a real per-tick measurement flows through PerfGateRunner.Run (#18 RegressionGate) against a generous Stage-0 anchor BaselineRecord (loop PhysicsSixtyHz; NON-certifying Linux gate) |
| `src/match-engine/tests/MatchEngineTacticTests.cs` | #21 T2 runtime-activation: SetTeamTactic routes a live per-team TeamTactic into each agent's TacticalContext at the AI-stride boundary (per-team translation Pressing.High→HIGH / Passing.Direct→DIRECT etc.); FR-TI-027 pending takes effect only at the stride; default/explicit Balanced is behaviour-neutral (digest chain identical to the untouched run); same non-Balanced tactic is deterministic across two runs; invalid teamId throws; #13 Phase-D writer — LineOfEngagement routes per team into the Pressing AI snapshot (VeryHigh/VeryLow) and the Balanced default routes Standard (v1.1); #14/#15 Phase-D writers — OffsideTrap routes per team into the Defensive AI snapshot + FocusPlay (LeftFlank/RightFlank) into the Attacking AI snapshot, with false/Mixed identity defaults (v1.2); #12 Phase-D writer — Width/DefWidth (VeryWide/Wide vs VeryNarrow/Narrow) route per team into the Positioning modifiers and the Balanced default routes Standard/Standard (v1.3); #23/#24/#25 Phase-D writers — dial routing per team, identity defaults + identity slot bindings, FM-BU-03 team-regain arming/decrement, marking-dwell coherence, non-identity-dial determinism (v1.5) |
| `src/match-engine/tests/MatchEngineAwayTeamScenarios.cs` | Decision Tree #8 audit deferred away-team closed-loop scenario on the #19 ScenarioRunner (`away-team-tactic-mirror`, Tier B, owning specs {2,8,16,19,21}, cross-spec path): boots a real MatchEngine, sets home=defending / away=attacking, ticks 300× (5 s), and locks that every away agent carries the away (attacking) routed tactic, every home agent the home (defending) one, the partitions distinct (composition-level inverse of the ERR-008-002 home/away root cause), away agents in bounds, two-run determinism digest match |
| `src/match-engine/tests/MatchEngineAwayTeamTests.cs` | Runs the away-team tactic-mirror scenario through ScenarioRunner.Run → Passed (DT #8 deferred away-team closed-loop follow-up, enabled by #21 runtime activation) |
| `src/match-engine/tests/CertifiedPerfBaselineTests.cs` | v1.0 — locks the FR-PO-052 certified perf baseline for the kickoff scenario: Stage-0 corpus entry is PENDING (no metric, refuses to build a record — no fabricated certification); certified projection builds a complete BaselineRecord that self-compares through PerfGateRunner (0% → pass); fail-closed invariants (degenerate metrics, incomplete manifest, empty args); platform-pin tokens match the documented tuple |
| `src/match-engine/tests/TeamTacticConfigTests.cs` | #21 T2 TeamTacticConfig + applier tests: Default Balanced-for-every-team, ForTeam per-team mapping + bounds throw, applier null-guards, Apply routes each team's tactic through SetTeamTactic at the stride boundary (Attacking/Defending translated per team), and applying the Default config is behaviour-neutral (digest chain identical to the unconfigured run) |
| `src/match-engine/RestartResolver.cs` | Match-flow completion (design note §5): pure static Resolve(RestartType, ballPosition, lastTouchTeam) → (position, awardedTeam) for ThrowIn/Corner/GoalKick — awardedTeam is uniformly `1 − lastTouchTeam` for all three (verified against BallCollision.CheckBoundaries's actual branches); position resolved per type (throw-in: X clamped, Y snapped to nearer touchline; corner: nearest corner inset by the ball radius; goal kick: six-yard-box centre on the exited goal line). KickOff/None are the caller's responsibility |
| `src/match-engine/OffsideEvaluator.cs` | Match-flow completion (design note §4): pure static ComputeOffsideLineX (second-nearest-to-own-goal X among the defending team's active non-sent-off agents; NaN if fewer than two — the AR-6 fix, see version history) + IsOffside (own-half exemption + NaN-line-never-offside guard). A documented Stage-0 reception-time approximation, not the Laws' freeze-at-the-pass model |
| `src/match-engine/SubstitutionReason.cs` | Match-flow completion (design note §6): enum Tactical(0)/Injury(1), embedded in the Tier A SubstitutionEvent payload byte field (ORDINAL STABILITY — append-only) |
| `src/match-engine/tests/MatchEngineRestartTests.cs` | Match-flow completion (design note §5) suite: RestartResolver pure-function locks (award-team unification across all three types; per-type position resolution incl. touchline snap / corner inset / six-yard-box centring) + MatchEngine integration (ball placement + possession clear for throw-in/corner/goal-kick; the pre-existing goal/centre-spot path is unaffected) + two-run bitwise determinism |
| `src/match-engine/tests/MatchEngineOffsideTests.cs` | Match-flow completion (design note §4) suite: OffsideEvaluator pure-function locks (second-nearest-to-goal-line both attack directions; sent-off/opponent exclusion from the active-defender count; the AR-6 NaN-degenerate-input regression — fewer than two active defenders must not make IsOffside always true; own-half exemption both directions) + MatchEngine integration via the direct TestOnly_EvaluateAndApplyOffside seam (violation applies the free kick and clears possession; an onside receiver is a no-op) |
| `src/match-engine/tests/MatchEngineFoulCardTests.cs` | Match-flow completion (design note §3) suite: pure DetermineCardKind band-boundary locks (red/yellow/no-card) + ApplyCardAndCheckSentOff promotion logic (first yellow, second-yellow → SecondYellow + sent off, straight red) + MatchEngine integration (an injected foul candidate awards a free kick at the victim's position and arms the cooldown; the cooldown decrements each tick; a sent-off agent decelerates to rest under the forced-Stop command) + two-run bitwise determinism with a foul in the run |
| `src/match-engine/tests/MatchEngineSubstitutionTests.cs` | Match-flow completion (design note §6) suite: the AR-5 regression lock (SubstitutePlayer is safe to call immediately after construction, before EventBus has ever entered a phase-valid state — the pending-event-queue fix) + applied-effect/bookkeeping (bench identity swap, per-team substitutions-used count) + every guard rejection (invalid team/slot/bench index, wrong-team slot, sent-off slot, already-substituted slot, reused bench index, cap reached) + the queued SubstitutionEvent flushing without throwing on the next RunTick + two-run bitwise determinism |
| `src/match-engine/tests/PlayerAttributeProjectionTests.cs` | #27 T1/T2 pure locks: per-field scale/copy behaviour with distinct inputs for every live projection, the KD-P1 KickPower derivations (float exact; RoundToInt integer + pinned half-to-even case using Mathf.RoundToInt itself as oracle), WeakFoot [1,5] round-trip, ToNormalized ÷20 endpoints, runtime-field passthrough (KD-P4), and the KD-P7 neutral-equivalence sweep (projection(CreateDefault) == every pre-T1 STAGE0/CreateDefault seed incl. the 0.5 normalized pair) |
| `src/match-engine/tests/MatchEngineSquadTests.cs` | #27 T1 engine-integration locks: all-CreateDefault-squad digest chains byte-identical to the unconfigured run (KD-P7); a distinct squad routes into every per-slot surface via the TestOnly seams (canonical/#2/#8/#7/#5/#6, per-team slot mapping, no GK gate), diverges the digest by design, and is two-run deterministic; SubstitutePlayer copies the canonical bench record + re-projects _dtAttrs/_perceptionAttrs (the v2.20 hazard lock); every fail-loud gate (null, too-small, out-of-range attr, WeakFoot out-of-range, post-tick call) incl. the self-AR-1 M-1 lock — an invalid AWAY squad refuses with the valid HOME squad left unapplied (both squads validated before any write) |
| `src/match-engine/tests/MatchEngineMatchFlowTests.cs` | Match-flow completion (design note §7) suite: the explicit-tick TestOnly_CheckMatchFlowTransitions seam (mirrors the TestOnly_RunManagerDecisionPoints pattern) locks pre-boundary no-op, half-time ball-reset-to-centre + fires-once guard (no ends-swap — AR-4), full-time _matchEnded flag + fires-once guard, post-full-time gameplay freeze (ball/agents unchanged while the tick/snapshot loop keeps advancing), and two-run bitwise determinism past full time |

### `src/project-constants/` — Project Constants & `[GT]` config loader (FR-CS-019, June 30, 2026)

> Infrastructure assembly at the bottom of the reference graph (read-only by all; `references: []`, `autoReferenced`). The documented home for the `[GT]` config-loading mechanism and (when one exists) multi-consumer `[CROSS]` constants. The mechanism landed June 30, 2026; the per-catalogue migration of the 520 existing `[GT]` literals landed the same day (509/520 — see `GameplayConfigHolder.cs` + the 17 migrated `<Spec>Constants.cs` catalogues listed in their own per-spec sections; 11 array-table constants in `tactical-instructions` + 4 untagged catalogues are explicit carve-outs, see `src/CLAUDE.md` "Migration status").

| File | Purpose |
|---|---|
| `src/project-constants/project-constants.asmdef` | Assembly definition `TacticalDirector.ProjectConstants`; `references: []`; `autoReferenced` |
| `src/project-constants/GameplayConfig.cs` | FR-CS-019 immutable boot-time `[GT]` key/value store keyed `[section] key`; `GetFloat/GetInt/GetBool/GetString(section, key, fallback)` — absent ⇒ fallback (behaviour-neutral), present-but-malformed ⇒ `FormatException`; immutable + constructor-injected (not a static singleton); boot-time only |
| `src/project-constants/GameplayConfigFileLoader.cs` | FR-CS-019 `Parse(text) → GameplayConfig` over `[section]` `key = value` + `#` comments; `null`/empty ⇒ `Empty`; key-before-section / empty key / duplicate `section.key` / malformed header / no-`=` line all throw `FormatException`; parser-swap seam (grammar not determinism-pinned) |
| `src/project-constants/GameplayConfigHolder.cs` | Resolves the boot-sequencing design point: single `Bind(config)` call point a composition root uses before any `[GT]` catalogue is referenced; `Config` resolves to `GameplayConfig.Empty` until bound (behaviour-neutral default); first `Config` read locks the binding, so a late `Bind` throws `InvalidOperationException` instead of silently losing the override |
| `src/project-constants/AssemblyInfo.cs` | `[InternalsVisibleTo("TacticalDirector.ProjectConstants.Tests")]` for `GameplayConfigHolder.ResetForTests` |
| `src/project-constants/tests/project-constants-tests.asmdef` | Test assembly definition (EditMode; references project-constants) |
| `src/project-constants/tests/GameplayConfigTests.cs` | Getter / fallback / fail-loud / case-insensitive / ctor-guard locks |
| `src/project-constants/tests/GameplayConfigFileLoaderTests.cs` | Grammar round-trip + comments/blanks + empty→Empty + every fail-loud case |
| `src/project-constants/tests/GameplayConfigHolderTests.cs` | Empty-default / Bind-before-lock takes effect / Bind(null) throws / Bind-after-lock throws / ResetForTests clears the lock |

### `src/living-world/` — Living World System #22 T0 scaffolding + season/world-loop slices 1–4 (June 21 / July 2–3, 2026; spec APPROVED June 22, 2026)

> Engine-free (`noEngineReferences`); references only `TacticalDirector.DeterministicSim` (slice 3 — the world.text RNG stream + the §4.6 canonical serializer). The spec's vol-2/vol-3 human-systems upstreams do not exist in `src/` yet. **Season/world-loop slice 1 landed July 2, 2026** — the KD-10 "persistent world store + season-calendar loop" prerequisite: `WorldClock` / `WorldLoop` / `MemoryStore` / `ColdStore` (§4.2/§4.3). **Slice 2 landed July 2, 2026 (same day)** — `ArcEngine` (§3.4 spawn/pin/lifecycle/expiry; trigger evaluation + its `world.arcs` RNG draws stay the documented KD-10 seam, FR-LW-020/031) + `ActiveSetMembership` (§3.5 entry/LRU-demotion/promotion, FR-LW-023/025), wired into WorldLoop phases 4/6. **Slice 3 landed July 2, 2026 (same day)** — `InteractionTextGenerator` (§3.3, over the new aperiodic `world.text` sub-stream — ERR-022-001 `DOMAIN_TAG_LIVING_WORLD=0x1E` + `SubsystemOrdinals.LivingWorld=80`) + `WorldStateSerializer` (§4.6 canonical block). **BackgroundTierSim (§3.5) stays the documented WorldLoop phase-5 seam, deliberately not built** — it would summarise club-AI/transfer/sacking outcomes that do not exist (a consumer would be the FR-LW-031 phantom-interface class), and §3.5 specifies no background-tier update formula. **Slice 4 landed July 3, 2026** — `WorldStore` (§7.1 KD-10 season composition root: the persistent world store + season-calendar loop; owns/wires the six services, drives `AdvanceDay`, round-trips a composite Snapshot = §4.6 four-store block + managerId + FR-LW-022 membership roster) + the `ActiveSetMembership` roster-serialization seam. Remaining: arc *trigger evaluators* + `world.arcs` registration, BackgroundTierSim, the `world.text` cursor in the composite save, and the composite `SNAPSHOT_SCHEMA_VERSION` fold into the *unified match/season* save — all land as their KD-10 upstreams (vol-2/vol-3, match-outcome events, an attached text generator, the unified season save root) are wired.

| File | Purpose |
|---|---|
| `src/living-world/living-world.asmdef` | Assembly definition `TacticalDirector.LivingWorld`; references `TacticalDirector.DeterministicSim` (slice 3); `noEngineReferences` (off-pitch layer touches no physics) |
| `src/living-world/AssemblyInfo.cs` | `InternalsVisibleTo("TacticalDirector.LivingWorld.Tests")` |
| `src/living-world/RelationshipLayer.cs` | `enum : byte {PlayerEdge,Affinity,Trust}` — ordinals = ActiveLayers bit positions (FR-LW-028) |
| `src/living-world/EventKind.cs` | `enum : byte` — open roster (vol-2 §7), APPEND-only seed members |
| `src/living-world/ArcKind.cs` | `enum : byte` — ordinal order = non-entity arc evaluation order (FR-LW-017) |
| `src/living-world/InteractionIntent.cs` | `enum : byte` — named to avoid existing Intent/AttackIntent/DistributeIntent collisions |
| `src/living-world/MemoryEpisode.cs` | `readonly struct` — episodeId/Kind/Salience/WorldTick/ManagerChoiceId + WithDecayedSalience |
| `src/living-world/SpawnCause.cs` | `readonly struct` provenance (KD-8) with nested Input |
| `src/living-world/Arc.cs` | `struct` arc state machine + PinnedEpisode refs + IsExpired liveness |
| `src/living-world/RelationshipEdge.cs` | `struct` — ActiveLayers mask, read-only PlayerEdge mirror, owned Affinity/Trust, Memory[], NextEpisodeId; IsLayerActive |
| `src/living-world/ColdSummary.cs` | `struct` — departed-contact compression incl. NextEpisodeId high-water mark (FR-LW-009) |
| `src/living-world/LivingWorldConstants.cs` | Appendix A catalogue — Fixed (`WORLD_TEXT_STREAM_SITE_ID`/`_VERSION`, `WORLD_SNAPSHOT_FORMAT_VERSION`) / Cross (`DomainTagLivingWorld` [CROSS #16 §3.4] + `CLIQUE_THRESHOLD` [CROSS vol-2 §2.1]) / GT (illustrative, pending §7 G2 balance pass; `SALIENCE_REF_THRESHOLD` now consumed by InteractionTextGenerator) |
| `src/living-world/LivingWorldMath.cs` | Pure deterministic helpers: §3.1 ApplyEvent/ApplyDecay/Clamp01 + FR-LW-021 CompareEvictability tiebreak |
| `src/living-world/WorldClock.cs` | Season-calendar clock (KD-4/FR-LW-019): one worldTick = one calendar day; Advance/RestoreFromSnapshot; distinct from MatchClock, never advanced by the match loops |
| `src/living-world/WorldLoop.cs` | §4.2 per-tick orchestrator: clock advance + phase-3 salience decay + phase-4 ArcEngine expiry sweep + phase-6 membership cap enforcement (null-injectable seams); phases 1/2/5 documented seams (producers not yet built; no phantom interfaces per FR-LW-031) |
| `src/living-world/MemoryStore.cs` | Live deep-tier store: edges sorted on the canonical (FromId,ToId) key (FR-LW-021); §3.2 evict-before-append (lowest-salience unpinned pre-existing episode; all-pinned ⇒ transient growth, shrink on unpin); FR-LW-018 **reference-counted** pins (AR-1 M-1); RemoveEdge refuses pinned edges (AR-1 M-2); §3.1 owned-layer ApplyEvent (PlayerEdge refused, FR-LW-004); InsertEdge F6 gate |
| `src/living-world/ColdStore.cs` | Cold tier sorted by EntityId + §3.5 Compress/Rehydrate transforms; Residue-A v1 schema recorded (NetRelationship = mean of active owned layers); episodeId resumes from NextEpisodeId (FR-LW-009); TryPeek = non-destructive verify-before-take companion to TryTake (slice-2 AR-1 M-2); Add mask coherence gate + single-manager scope doc (slice-2 AR-2) |
| `src/living-world/ArcEngine.cs` | §3.4 emergent-arc lifecycle: SpawnArc (steps 1–3, atomic FR-LW-018 pinning with F1 rollback; **AR-1 M-1** spawn-time pin-array snapshot so post-spawn caller mutation cannot desync resolve; **AR-1 L-1** spawnTick+lifetime uint-overflow gate), AdvanceState, ResolveArc/unpin, §6.2 per-tick expiry sweep in deterministic spawn order; trigger evaluation + `world.arcs` RNG sub-stream documented as the KD-10 seam (no draw site ⇒ no stream registered, FR-LW-020/031) |
| `src/living-world/ActiveSetMembership.cs` | §3.5 active-set membership (FR-LW-023/025): entry on first interaction (upfront entity validation, **AR-2 M-1**); cold-store promotion honouring the verify-live-edge-first TryTake ordering with the **AR-1 M-2** mask check against a TryPeek BEFORE the destructive take; deterministic LRU demotion at the external cap (max episode worldTick, ties → lowest EntityId, arc-pinned edges skipped); own-club at-club exemption + Depart path (pinned departure defers as external); **v1.3** roster-serialization seam for the KD-10 root (`MemberCount`/`GetMemberAt` canonical read + `RestoreMember` live-edge-validated rebuild, FR-LW-022) |
| `src/living-world/InteractionSlots.cs` | Slice 3 — §3.3 slot-fact carrier (`readonly struct`): SubjectName/OpponentName/HomeGoals/AwayGoals match-engine facts + optional cited `MemoryEpisode` (FR-LW-013 — no derived stats); fail-loud ctors; default-value re-gated at Generate |
| `src/living-world/InteractionTextCorpus.cs` | Slice 3 — §3.3 Stage-0 in-code authored corpus (`internal static`): per-`InteractionIntent` template rows + per-`EventKind` episode clauses; APPEND-only order (draw selects by index); None/out-of-roster refused (KD-6 offline authoring, FR-LW-012; the §7.2 AI-authored corpus is a pure data swap) |
| `src/living-world/InteractionTextGenerator.cs` | Slice 3 — §3.3 deterministic surface text: registers + draws the aperiodic **world.text** sub-stream (first #22 draw site; one Reserve/DrawReserved per interaction, FR-LW-020, separate from world.arcs) then slot-expands; §3.2 citation gated at SALIENCE_REF_THRESHOLD (NaN-closed); ALL validation runs pre-draw so a refused call consumes no cursor (replay parity, T-LW-DET-003/004); no model inference (FR-LW-012); provenance implicit (§3.6) |
| `src/living-world/WorldStateSerializer.cs` | Slice 3 — §4.6 canonical living-world block (Appendix B order) via the #16 CanonicalSerializer (bitwise round-trip); Serialize(clock/memory/arcs/cold) → payload; Deserialize rebuilds through the validating store seams (SpawnArc re-takes pins ⇒ FR-LW-018 refcounts reconstructed); fail-loud version/domain-tag/trailing-byte/out-of-roster-EventKind/lifetime gates; composite SNAPSHOT_SCHEMA_VERSION bump deferred to the KD-10 season root (FR-LW-003) |
| `src/living-world/WorldStore.cs` | Slice 4 — KD-10 season composition root (`sealed`): the persistent world store + season-calendar loop (§7.1). Owns/wires the six services (WorldClock+MemoryStore+ColdStore+ArcEngine+ActiveSetMembership+WorldLoop) for one manager; `AdvanceDay` drives the §4.2 phases with producers (3 decay / 4 arc expiry / 6 external-cap LRU; phases 1/2/5 stay the WorldLoop null seams, FR-LW-031); `RecordInteraction` stamps the current day; `Snapshot`/`Restore` round-trip a composite payload = fail-loud header (WORLD_STORE_FORMAT_VERSION + DomainTagLivingWorld + managerId) + the §4.6 four-store block + the FR-LW-022 membership roster; fail-loud version/tag/managerId/length-count/flag/trailing gates + a `ReadCount` bound |
| `src/living-world/Tests/living-world-tests.asmdef` | Test assembly definition (EditMode; references living-world + DeterministicSim) |
| `src/living-world/Tests/LivingWorldTests.cs` | T0 units: enum ordinals (T-LW-U-001..004); §3.1 worked examples (0.56, ~0.016, no-overshoot, no-op); eviction tiebreak; ActiveLayers masking; episodeId-resume |
| `src/living-world/Tests/SeasonWorldLoopTests.cs` | Slice-1 suite (28 tests): clock calendar semantics + T-LW-DET-006; memory T-LW-U-011..018 (monotonic ids, eviction + tiebreak + pin exemption + transient growth, decay, F1 guard, PlayerEdge/F6 refusal, NaN-gates); T-LW-DET-002 canonical order; LOD T-LW-I-011..014 (top-N retention, F5 retained-fields round-trip / T-LW-FAIL-005, episodeId resume, duplicate demote/promote fail-loud); loop phase order + T-LW-DET-007 additive identity; two-run field-identity determinism; AR-1 regression locks (AR-1: ref-counted pins, pinned-edge removal refusal, mask conflict, F6 insert gate, cold-summary coherence; AR-2: out-of-roster layer/mask-bit refusal, episodeId + salience coherence at both seams) |
| `src/living-world/Tests/ArcMembershipTests.cs` | Slice-2 suite (24 tests incl. AR-1 + AR-2 locks — Add undefined-mask refusal, self-contact upfront refusal without stranding — M-1 post-spawn pin-array mutation cannot desync resolve, M-2 promotion mask conflict fails loud without stranding the summary, L-1 overflow refusal): ArcEngine spawn/pin + provenance, F1 pin rollback, validations, resolve/unpin, shared-pin refcount integration, §6.2 expiry boundary, state advance; membership entry/repeat/class-flip + mask-conflict fail-loud, LRU cap demotion to cold, worldTick-tie → lowest EntityId, arc-pinned skip, own-club exemption, Depart→cold + FR-LW-009 re-entry resume, pinned-Depart deferral as external, non-own-club Depart refusal; WorldLoop phase-4/6 wiring; two-run field-identity determinism |
| `src/living-world/Tests/WorldTextSnapshotTests.cs` | Slice-3 suite (~20 tests): InteractionTextGenerator determinism (T-LW-DET-003 same-seed identical string, exact slot expansion, eligible-citation clause), no-draw-on-refusal replay-parity lock (below-threshold + NaN salience + None/out-of-roster intent + kindless citation + default slots all refuse pre-draw; cursor untouched), single-draw cursor advance, world.text↔sibling-stream non-interference (T-LW-DET-004); WorldStateSerializer §4.6 field-identity round-trip incl. NextEpisodeId + shared-pin refcount reconstruction, bitwise determinism + serialize→deserialize→serialize stability, fail-loud unknown-version / wrong-domain-tag / trailing-byte / null-store gates |
| `src/living-world/Tests/WorldStoreTests.cs` | Slice-4 suite (14 tests): construction/wiring + negative-manager guard; AdvanceDay advances the calendar + decays salience (phase 3) + expires arcs across the lifetime boundary (phase 4, pin released); RecordInteraction stamps the store's current day; Snapshot/Restore field-identical + idempotent round-trip (arc pin re-take, own-club/external roster classes, departed→cold contact) + empty-store round-trip; two-run byte determinism; six fail-loud restore gates (null / wrong version / wrong domain tag / corrupt store-length / trailing bytes / bad membership flag) |

### `src/match-viewer/` — Minimal match viewer (July 2, 2026; presentation tooling — not a numbered spec)

> First presentation-layer surface: records a `MatchEngine` run through its public observation surface (v1.24) and exports a self-contained HTML canvas replay (ball + 22 agents, play/pause/scrub/speed). Pure observer — recording is digest-identical to an unobserved run (locked by test). The HTML output is NOT a determinism-pinned wire format (rounded display coordinates only; same contract class as the Stage-0 tactic text grammar). The UI layer proper remains Stage 1+ (`src/CLAUDE.md` layer taxonomy).

| File | Purpose |
|---|---|
| `src/match-viewer/match-viewer.asmdef` | Assembly definition `TacticalDirector.MatchViewer`; references MatchEngine + DeterministicSim + BallPhysics + AgentMovement + ProjectConstants |
| `src/match-viewer/MatchViewerConstants.cs` | Catalogue: IFAB pitch-marking geometry `[FIXED]` (const) + canvas/recording presentation `[GT]` (config-resolved via `GameplayConfig`, `"match-viewer"` section; presentation-only — nothing feeds the sim or digest) |
| `src/match-viewer/ReplayFrame.cs` | One sampled frame: tick / ball position / possessing agent / agent positions (value copies, never aliasing live buffers) |
| `src/match-viewer/MatchReplay.cs` | Immutable frame sequence + roster (teamIds, GK flags) / pitch / cadence metadata; ReadOnlyCollection + cloned arrays; fail-loud ctor (frame coherence, strictly-increasing ticks, non-empty, metadata NaN-gates) + roster-index guards |
| `src/match-viewer/MatchReplayRecorder.cs` | Ticks an engine, sampling between ticks; seed + pre-configured-engine overloads; fail-loud guards; kickoff frame + final tick always captured |
| `src/match-viewer/HtmlReplayExporter.cs` | Self-contained HTML canvas replay: pitch markings, home/away/GK/possession/ball-height cues, play/pause/scrub/speed + space toggle; InvariantCulture; fail-loud non-finite gate |
| `src/match-viewer/tests/match-viewer-tests.asmdef` | Test assembly definition (EditMode; references match-viewer + match-engine + deterministic-sim + ball-physics + agent-movement) |
| `src/match-viewer/tests/MatchViewerTests.cs` | Frame cadence; on-pitch finiteness; bitwise two-run determinism; observer-neutrality digest lock; fail-loud guards; exporter structure/no-NaN locks |
| `src/match-viewer/AssemblyInfo.cs` | `InternalsVisibleTo` the test assembly (the `LiveMatchStreamer.TickOnce` / `ApplyCapturedFrame` seams are internal) |
| `src/match-viewer/LiveMatchFrame.cs` | One live-captured frame (July 15, 2026): tick / ball / possession / positions / `Scoreline` / `MatchEnded` — plus, since P1, per-agent `LiveAgentCue[]`, per-team substitution counts, the derived `MatchPeriod` and the latched `RestartBanner` |
| `src/match-viewer/LiveAgentCue.cs` | P1: the per-agent HUD cue — yellow cards, sent-off, active bench slot (`IsSubstitute` derived from the slot, so the two can never disagree) |
| `src/match-viewer/Scoreline.cs` | P1 AR-1 M-6: the home/away score pair as one carrier; owns the non-negative gate, so a negative score is refused before it can reach a frame |
| `src/match-viewer/RestartBanner.cs` | P1 AR-1 M-6: the latched restart (cue + awarded team + tick). Team and tick are DERIVED from the cue, so `default(RestartBanner)` reports `NO_RESTART_TEAM` / 0 rather than "home team, tick 0" |
| `src/match-viewer/LiveMatchStreamer.cs` | Real-time-paced `MatchEngine` tick loop (July 15, 2026); lock-protected latest-frame handoff; pause/resume/speed; full-time auto-pause; optional sim-thread pre-tick hook. Owns the P1 cross-tick restart latch — deliberately here, not in the engine, so nothing about it reaches the snapshot (KD-P1-3) |
| `src/match-viewer/LiveMatchServer.cs` | Loopback-only hand-rolled HTTP server (July 15, 2026): `GET /` viewer page, `/frame` polled JSON, `/control` playback-only. Holds a streamer, never an engine. The `/frame` payload still carries only the pre-P1 fields — rendering the P1 additions is roadmap B6 |
| `src/match-viewer/tests/LiveMatchStreamerTests.cs` | Latest-frame handoff; observer-neutrality digest; full-time auto-pause; speed/lifecycle guards; roster accessors; threaded start/stop smoke test |
| `src/match-viewer/tests/LiveMatchServerTests.cs` | Real-loopback-socket routing / control / error-path / abuse-guard / shutdown locks |
| `src/match-viewer/tests/LiveMatchFrameCueTests.cs` | P1: per-agent cue lockstep with positions, per-team substitution counts, derived period, and the restart LATCH (with a non-vacuity guard — restarts are sparse, so a bound that stopped working must fail rather than skip the property) |

### Season Save (`src/season-save/`) — unified season save-file root + Season & Competition Loop #30 + the league bootstrap

> Not a single numbered spec. The assembly hosts three related bodies of work at the same layer position
> (above both match-engine and living-world): the save-file root (`unified-season-save-design.md`), the
> #30 season loop value types + codec, and the league bootstrap (`league-bootstrap-design.md`).
### `src/match-analytics/` — Match Analytics & Statistics #37 T0 (July 27, 2026; spec APPROVED)

> The value types and the pure xG location model, per `docs/tracking/path-to-playable-roadmap.md` item B2.
> **Nothing is wired into the engine yet** — the T1 ledger tap + aggregator is roadmap item B3, and the xG
> model has no live consumer at Stage 1 because the event ledger carries no shot origin (recorded as
> ERR-037-001, not worked around). Assembly references EventSystem + MatchEngine + BallPhysics.

| File | Purpose |
|------|---------|
| `match-analytics.asmdef` | `TacticalDirector.MatchAnalytics`; references EventSystem + MatchEngine + BallPhysics |
| `MatchAnalyticsConstants.cs` | Appendix A catalogue: xG `[GT]` coefficients, pitch `[CROSS]` mirrors from Ball Physics #1, `TEAM_COUNT` `[CROSS]` from the engine (AR-1 M-4 — it indexes the same per-team arrays, so a local copy would be the parallel-surface trap), the heatmap grid and the territorial sample stride |
| `XgLocationModel.cs` | The §3.3 pure xG location model: distance + subtended-goal-angle logit, overflow-safe logistic, `[0,1]` clamp, F2 non-finite gate, and a team gate on all three public entry points (AR-1 M-5) |
| `StatPoint.cs` | One recorded event location + team (the aggregator's input unit) |
| `MatchStatline.cs` | The immutable per-team basic statline; carries an explicit `_hasValue` discriminator so `default(MatchStatline)` is recognisable as unset rather than reading as a real 0-0 line (AR-1 M-1) |
| `AdvancedStatline.cs` | Territorial share + the copied heatmap bins; same `_hasValue` discriminator |
| `MatchAnalyticsResult.cs` | The per-match result — both teams' statlines; refuses a default-constructed statline at construction |
| `Tests/match-analytics-tests.asmdef` | `TacticalDirector.MatchAnalytics.Tests` (Editor-only) |
| `Tests/XgLocationModelTests.cs` | The three §3.3 worked examples pinned, plus the shape a Stage-2 refit must preserve — distance/angle monotonicity, home/away mirror symmetry (the ERR-008-002 lesson), totality over and beyond the pitch, purity, and the F1/F2 gates |
| `Tests/MatchAnalyticsValueTypeTests.cs` | Value-type contracts: copy-not-wrap on the heatmap bins, the unset-vs-zero discriminator, and every fail-loud gate |

---

### `src/ui-framework/` — UI / Client Framework #38 T0 substrate (July 25, 2026; spec APPROVED July 22, 2026)

Presentation layer. Host-free and CI-gated; the UGUI rendering binding is Unity-host-gated (#38 §4.3/§7.2).
Governed by `docs/tracking/ui-framework-t0-implementation-plan.md`.

| File | Purpose |
|------|---------|
| `ui-framework.asmdef` | `TacticalDirector.UiFramework`; references MatchEngine + MatchViewer + MatchClientCore + TacticalInstructions + ProjectConstants (all built — no speculative reference) |
| `AssemblyInfo.cs` | `InternalsVisibleTo` the test assembly (the intent→command translation is internal) |
| `IViewModelSource.cs` | The KD-1 projection contract `IViewModelSource<T> where T : struct` + the non-generic marker the screen registry stores |
| `ScreenId.cs` | `ScreenId` value-type identity (value equality) + `ScreenRegistration` { id, source, dispatcher } |
| `NavigationShell.cs` | The §3.2 deterministic stack machine; fail-loud on unregistered navigation (F2), root `Pop`, un-rooted `Current`, duplicate `Register` (ERR-038-003) |
| `IntentKind.cs` | The closed intent set + `None = 0` zero-value sentinel; `AdvanceRound` deliberately absent until #30 is built |
| `ManagerIntent.cs` | The typed manager intent payload (one factory per kind); carries no channel dependency |
| `ICommandDispatcher.cs` | The dispatch contract — route to an existing public seam; throw on unmapped (F3) |
| `MatchTacticsDispatcher.cs` | The one concrete dispatcher: live mode marshals via the `MatchSession` command channel (KD-U1/FR-UI-023), single-threaded mode applies directly; internal intent→command translation |
| `ILiveFrameSource.cs` | The KD-U7 one-method frame read seam (makes FR-UI-005 structural + the match view thread-free testable) |
| `LiveMatchStreamerFrameSource.cs` | Production adapter over `LiveMatchStreamer` (pure pass-through; exposes only the read capability) |
| `MatchFrameView.cs` | The immutable match view model — arrays copied never wrapped; SQUAD_SIZE / possession-id / finite gates (F1). P1 adds `AgentCues` / `SubstitutionsUsed` (both copied, both length-gated), `Period` and the latched `Restart`; the score gate lives in `Scoreline` since AR-1 M-6 |
| `MatchViewModelSource.cs` | `IViewModelSource<MatchFrameView>` over the frame seam; F5 last-known/empty; holds no engine |
| `UiFrameworkConstants.cs` | `[GT]` match-view refresh cadence (declared, consumed by the §7.2 UGUI binding) |
| `Tests/ui-framework-tests.asmdef` | `TacticalDirector.UiFramework.Tests` (Editor-only, autoReferenced false) |
| `Tests/NavigationShellTests.cs` | T-UI-NAV-001/002/003 — the §3.5 worked transition + every fail-loud edge |
| `Tests/CommandDispatchTests.cs` | T-UI-DISPATCH-001..004 — per-seam routing, F3, the intent/command drift guard, the FR-UI-023 marshaling lock |
| `Tests/MatchViewProjectionTests.cs` | T-UI-MATCHVIEW-001/002, T-UI-FAIL-001/002, T-UI-LAYER-002 |
| `Tests/MatchViewObserverNeutralityTests.cs` | T-UI-NEU-001 digest-chain neutrality + T-UI-LAYER-001 reverse-reference scan |
| `Tests/MatchViewCueProjectionTests.cs` | P1: F4 copy locks for the cue / substitution arrays, their F1 gates, period + restart pass-through, and the AR-1 M-3 empty-view sentinel / banner-construction locks |

---

### `src/match-analytics/` — Match Analytics & Statistics #37 (T0 July 27, 2026; T1 same day, roadmap B3)

Presentation-layer derivation. Read-only over two taps (FR-AN-002); no sim assembly may reference it
(KD-4, scanned mechanically). Registers no RNG stream, domain tag or `SubsystemOrdinal` (KD-5).

| File | Purpose |
|------|---------|
| `match-analytics.asmdef` | `TacticalDirector.MatchAnalytics`; references EventSystem + MatchEngine + BallPhysics (the BallPhysics ref is ERR-037-001's resolution — Appendix A's `[CROSS]` tags require it) |
| `MatchAnalyticsConstants.cs` | Appendix A: xG `[GT]` coefficients, pitch `[CROSS]` mirrors, heatmap grid, sample stride, and the card / restart record encodings mirrored from their producers |
| `MatchStatline.cs` / `AdvancedStatline.cs` / `StatPoint.cs` / `MatchAnalyticsResult.cs` | The four immutable #38 view models; arrays copied, never wrapped; F1/F2/F4 gated at construction |
| `XgLocationModel.cs` | The KD-2 pure two-term geometric model (shape is the contract, coefficients are `[GT]`) |
| `ITickLedgerTap.cs` | **T1** — the KD-7 per-tick ledger seam (both sides specified, so the §3.2 routing table is drivable from authored records) |
| `IWorldStateSample.cs` | **T1** — the §3.4 positional sample seam; narrower than the engine's observation surface on purpose |
| `MatchEngineObservation.cs` | **T1** — the live-engine adapter implementing both seams (read-only forwards) |
| `MatchAnalyticsAggregator.cs` | **T1** — the KD-3 core: §3.1 tick-weighted possession with an explicit loose bucket, the §3.2 routing table keyed on `EventRegistry.GetOrdinal<T>()`, §3.4 territorial + heatmap binning, F1/F2/F3/F4/F5/F6 |
| `Tests/match-analytics-tests.asmdef` | `TacticalDirector.MatchAnalytics.Tests` (Editor-only) |
| `Tests/XgLocationModelTests.cs` | T-AN-XG-* — the three §3.3 worked examples + the shape properties a Stage-2 refit must preserve |
| `Tests/MatchAnalyticsValueTypeTests.cs` | View-model gates + the KD-4 reverse-reference scan (narrowed at B6 to a sanctioned-consumer allow-list **plus** an explicit never-reference list) |
| `Tests/MatchAnalyticsAggregatorTests.cs` | **T1** — the §3.2 routing table row by row, possession weighting, §3.4 totality incl. the halfway-line boundary (ERR-037-002), Build idempotence/snapshot semantics, every failure mode |
| `Tests/MatchAnalyticsObserverNeutralityTests.cs` | **T1** — T-AN-NEU-001 (digest unchanged, with a non-vacuity guard) + T-AN-DET-001 two-run determinism over a real match; liveness window re-measured 1 800 → 3 600 ticks (v1.1, gk-contact-rate AR-5) |

---

### `src/match-client-web/` — the PM-1 browser match client (roadmap B6, July 27, 2026)

Not a numbered spec. Governed by `docs/tracking/browser-match-client-design.md`. The only assembly
above BOTH `ui-framework` and `match-analytics`; host-free and CI-gated. Deliberately separate from
`match-viewer`'s `LiveMatchServer`, whose playback-only invariant it must not weaken (ERR-038-001).

| File | Purpose |
|------|---------|
| `match-client-web.asmdef` | `TacticalDirector.MatchClientWeb`; references MatchClientCore + UiFramework + MatchAnalytics + MatchViewer + MatchEngine + TacticalInstructions + ProjectConstants + DeterministicSim |
| `MatchClientWebConstants.cs` | `[GT]` port, poll cadences, canvas scale, restart-caption window; `[FIXED]` request-line bound |
| `MatchClientHost.cs` | KD-W1/W4 composition: `MatchSession` + the #38 projection + the intent dispatcher + the live #37 aggregator, pumped by a sim-thread post-tick observer under an analytics lock |
| `MatchClientResponse.cs` | KD-W6 — the router/transport seam value |
| `MatchClientRouter.cs` | KD-W2/W3/W7 — the four routes and their privilege split; #38 frame + #37 report serialization; fail-loud parsing incl. the `Enum.IsDefined` guard |
| `MatchClientPage.cs` | KD-W9 — the self-contained page (pitch, HUD, playback, tactics, statistics); renders the view model, reads pitch geometry from the streamer |
| `MatchClientServer.cs` | KD-W8 — loopback-only transport; decides nothing, delegates every route |
| `tests/match-client-web-tests.asmdef` | `TacticalDirector.MatchClientWeb.Tests` (Editor-only) |
| `tests/MatchClientRouterTests.cs` | Routing table + the privilege split asserted against the command queue + fail-loud parsing |
| `tests/MatchClientHostTests.cs` | The every-tick pump over a really-running match (F6 self-checks it), `ServiceOnce` not advancing it, the disarm-and-latch fault path, intent delivery |
| `tests/MatchClientServerTests.cs` | Real-loopback framing, routing, request-line bound, post-`Stop` refusal, rebind |

---

### Season Save (`src/season-save/`) — unified season save-file root (not a numbered spec; `unified-season-save-design.md`)

| File | Purpose |
|------|---------|
| `src/season-save/season-save.asmdef` | `TacticalDirector.SeasonSave` — the composition/persistence root ABOVE both match-engine and living-world (references MatchEngine + LivingWorld + DeterministicSim); the only assembly that may see both blobs, resolving FR-LW-003 |
| `src/season-save/SeasonSaveConstants.cs` | `[FIXED] SEASON_SAVE_FORMAT_VERSION = 2` — the outermost format version, distinct from the snapshot schema versions + MATCH_SAVE_FORMAT_VERSION + WORLD_STORE_FORMAT_VERSION + SEASON_STATE_FORMAT_VERSION (KD-4); bumped 1 → 2 at #30 T1 when the frame gained the season sub-blob (FR-SN-020) |
| `src/season-save/SeasonSaveBlobs.cs` | Deframe result: `WorldBlob` + `SeasonBlob` (both always) + `MatchBlob` (null if no in-progress match) — three opaque byte sub-blobs (KD-2/KD-3, FR-SN-019) |
| `src/season-save/SeasonSaveCodec.cs` | Pure static frame codec: `Encode(worldBlob, seasonBlob, matchBlobOrNull)` / `Decode(byte[]) → SeasonSaveBlobs` — a SEASON_SAVE_FORMAT_VERSION-gated frame + matchPresent flag + three length-prefixed opaque sub-blobs (each keeps its own version gate); overflow-safe `Require` bound + fail-loud on null/version/flag/length/trailing (KD-7/KD-8) |
| `src/season-save/SeasonStateCodec.cs` | #30 T1: pure static season-state sub-blob codec — `Encode(SeasonState)` / `Decode(byte[]) → SeasonState` over the #30 Appendix B layout (version gate; seed/seasonNumber/managedClubId; club set; the serialized schedule per KD-5; calendar cursor per KD-4; table in ClubId order; board), SEASON_STATE_FORMAT_VERSION-gated; overflow-safe element-wise length bounds, trailing-byte guard, serialized-vs-derived goal-difference coherence check, and decode-through-the-validating-constructors (FR-SN-019/022/023) |
| `src/season-save/SeasonSaveContents.cs` | `Load` result: reconstructed `WorldStore` + `SeasonState` (both never null) + nullable `MatchEngine` |
| `src/season-save/SeasonSaveManager.cs` | Static: `Save(world, season, matchOrNull, path)` (capture all three → Encode → atomic temp→fsync→rename) / `Load(path, ISquadProvider = null, ArcCanonSource = null) → SeasonSaveContents` (Decode → WorldStore.Restore + SeasonStateCodec.Decode +, when present, MatchSaveManager.Restore) — KD-1/KD-5/KD-6/KD-8, FR-SN-021 |
| `src/season-save/SeasonLoopConstants.cs` | #30 Appendix A: `[FIXED] SEASON_STATE_FORMAT_VERSION` + `IDENTITY_PERMUTATION_SEED`; `[GT]` points scheme (Win/Draw/Loss) + `JobSecurityScale`, off `GameplayConfig` |
| `src/season-save/Fixture.cs` | #30 T0: one scheduled fixture (round index, home/away ClubId, played flag); `MarkPlayed` returns a new value |
| `src/season-save/FixtureScheduler.cs` | #30 T0: pure `Generate(clubIds, seed)` double round-robin with the §3.1 round-parity venue rule (ERR-030-010) + a local SplitMix64 club-label permutation (FR-CS-044 `unchecked`) |
| `src/season-save/LeagueTableRow.cs` | #30 T0: one club's table row; `Create` fail-loud on negative counts / `won+drawn+lost != played` (F3) |
| `src/season-save/LeagueTable.cs` | #30 T0: `ApplyResult` (both rows resolved before either is written — F2) + the FR-SN-007 tie-break `OrderedView` + `RowsInClubIdOrder` serialization order |
| `src/season-save/SeasonCalendar.cs` | #30 T0: the KD-4 round→world-day mapping + the cursor; `Linear` guards zero cadence and uint overflow |
| `src/season-save/BoardObjective.cs` | #30 T0: the Stage-0 literal objective "finish at or above position P" (FR-SN-014) |
| `src/season-save/BoardState.cs` | #30 T0: the objective + integer per-mille job security (Appendix B row 11) |
| `src/season-save/MatchResult.cs` | #30 T0: one fixture's outcome payload — the table write and the FR-SN-016 producer event |
| `src/season-save/SeasonState.cs` | #30 T0: the whole serialized season surface (seed / season number / managed club / club set / the CONCRETE schedule per KD-5 / table / calendar / board); copy-then-validate coherence gates; KD-7 internal mutators (SeasonLoop is the only production writer) |
| `src/season-save/SeasonRollOutcome.cs` | #30 T3: the boundary-roll producer record — completed/next season number, final vs target position, objective met, job security before/after, next seed + first fixture day. Session-scoped, not serialized (ERR-030-013 posture) |
| `src/season-save/SeasonViewModel.cs` | #30 T0: the read-only observation surface for #37/#38 (FR-SN-033) — value copies |
| `src/season-save/LeagueBootstrapConstants.cs` | A3: `[FIXED]` roster/strength/season seed domain separators + the roster stream identity; `[GT]` DefaultClubCount / MaxClubCount / LeagueStrengthSpread / calendar cadence (negative world-days refused at read) / the array-valued squad position template; `BuildSquadPositionTemplate()` expands it fail-loud |
| `src/season-save/ClubNameCatalogue.cs` | A3: Stage-0 APPEND-only club names assigned by `ClubId` index (KD-3 — drawn from no stream, in no digest); ≥ `MaxClubCount` entries, test-locked for coverage + uniqueness |
| `src/season-save/Club.cs` | A3: one club's bootstrap identity — `ClubId` / `Name` / `StrengthDelta`; not serialized (re-derivable from the world seed) |
| `src/season-save/League.cs` | A3: the immutable bootstrap product; implements `ISquadProvider` (so one instance serves the engine, `SeasonSaveManager.Load` and A4's round loop); `CreateSeason(managedClubId)` → `SeasonState` via `SeasonState.CreateNew`; default objective = top half (KD-9) |
| `src/season-save/LeagueBootstrap.cs` | A3: `Generate(worldSeed, clubCount)` → `League` — three domain-separated seed derivations (KD-4), one roster stream per club (`entityId = clubId`), a seeded Fisher–Yates strength rank ramped to a per-club `[1,20]` delta (KD-5, `WeakFootRating` excluded), position-template roster generation (KD-6), F1–F3 gates; local SplitMix64, so **nothing** is allocated in #16 |
| `src/season-save/tests/season-save-tests.asmdef` | Test assembly (EditMode; references season-save + match-engine + living-world + deterministic-sim + player-database) |
| `src/season-save/tests/SeasonSaveManagerTests.cs` | Disk round-trip determinism (no-match season; season with neutral / distinct-squad match via ISquadProvider), each asserting the season resumes field-identical (FR-SN-022) + SeasonSaveCodec round-trip/fail-loud incl. the v1-frame rejection + manager fail-loud paths incl. the R4 no-match-with-provider and null-season locks |
| `src/season-save/tests/SeasonStateCodecTests.cs` | #30 T1: season sub-blob round-trip field identity (fresh / mid-season / completed), per-column and scalar locks, encode determinism + non-vacuity control, the pinned-offset layout lock (Appendix B field order incl. row 3a), and every FR-SN-023 fail-loud gate |
| `src/season-save/tests/SeasonStateTests.cs` | #30 T0: value-type contracts + the instance-field-count coupling guards across `SeasonState` and its five aggregates (a field added but omitted from the codec would otherwise pass the round-trip vacuously) |
| `src/season-save/tests/LeagueBootstrapGoldenVectorTests.cs` | A3 AR-5 H-1: the PINNED golden vector for league generation — absolute expected season seed, strength deltas, spot identity/attribute values and an FNV-1a-64 digest over every field of every club and player. Rosters are regenerated from the world seed rather than persisted, so this is the only test that fails when generation moves; verified non-vacuous by perturbing `AttributeBaseMean` |
| `src/season-save/tests/LeagueBootstrapTests.cs` | A3: two-run field-identical determinism, seed divergence, roster independence from league size, contiguous ids + globally unique `PlayerId`s, catalogue coverage/uniqueness, `MaxClubCount` vs `MaxRngStreams` coupling, strength-ramp endpoints/symmetry/permutation and its reach into the rosters, position coherence for every shipped formation **plus** an end-to-end `ConfigureSquads` run through the real engine (F6), every F1–F5 gate, and the `CreateSeason` handoff round-tripping through `SeasonStateCodec` |

## Tracking Documents

| File | Purpose |
|------|---------|
| `docs/tracking/PROGRESS.md` | Stage progress, milestones, and current status notes |
| `docs/tracking/spec-error-log.md` | Cross-spec error tracking (`ERR-*`) |
| `docs/tracking/spec-error-log-err012-addendum.md` | ERR-012 addendum details |
| `docs/tracking/fix-manifest-pass-mechanics.md` | Pass Mechanics audit/fix closure tracking |
| `docs/tracking/certification-platform.md` | Stage 0 host platform version pin (required before first Spec #16 certification run) |
| `docs/tracking/file-manifest.md` | This manifest |
| `docs/tracking/advanced-positional-behaviors-design.md` | Design supplement (v0.4, Jul 8, 2026 — PROMOTED) — dismarking, scripted build-up structures, positional rotations; promoted to specs #23–#25 (`dismarking-ai/`, `build-up-structures/`, `positional-rotations/`, all APPROVED Jul 10, 2026); superseded by the specs on deviation |
| `docs/tracking/game-model-ai-manager-design.md` | Design supplement (v0.5, Jul 8, 2026 — PROMOTED) — tactical preset library + AI-manager selection/adaptation; promoted to spec #26 (`tactical-presets/`, APPROVED Jul 10, 2026); superseded by the spec on deviation |
| `docs/tracking/league-bootstrap-design.md` | Design supplement (v1.1, Jul 25, 2026 — **A3 LANDED**) — the league bootstrap (club identity/naming, strength distribution, world-seed derivation) plus the roadmap-A4a round-resolution model shape and calibration methodology. Closes §6 item 1 of the path-to-playable roadmap; explicitly not #47 (no editor, no new data format) |
| `docs/tracking/injury-aging-research-alignment-design.md` | Design supplement (v0.3, Jul 26, 2026 — **AR-CONVERGED, awaiting owner sign-off**) — reconciles the APPROVED text of #41 Injuries & Medical and #28 Player Progression against the sports-science literature. 12 findings (5H+5M+2L): no age term in the occurrence model, monotone-not-U-shaped match load, no recovery-interval input, recurrence deferred with its input already serialized, a severity model that cannot represent an ACL, no post-injury consequence, position-blind age bands, and an inverted decline order. Proposes ERR-041-002..007 + ERR-028-002..004 back-props (FR-MD-028..034 / FR-PG-025..028); zero #30/#29/#27/#16 changes, no RNG/determinism impact, no save-format bump (both owning specs are pre-first-byte — the timing argument, KD-R1). Form / congestion-coordination / contract-year recorded but deliberately NOT designed |
| `docs/tracking/shot-outcome-distribution-design.md` | Design supplement (v1.1, Jul 27, 2026 — **LANDED, AR-1..AR-3 converged**) — the shot-outcome distribution pass (§5.Z.18): KD-1..KD-8, the measured pre/post table (goals 15.3 → 12.3, deflections 0 → 560–612/match), ERR-006-002/003 + ERR-001-004 + ERR-003-007 back-props, the `match-engine-shot-outcomes` acceptance evidence (3 of 8 predicates fail pre-fix by execution), and the recorded residual levers (shot volume, shot speed, keeper conversion) |
| `docs/tracking/shot-speed-woodwork-design.md` | Design supplement (v1.1, Jul 28, 2026 — **LANDED, AR-1..AR-3 converged**) — the shot-speed & woodwork pass (§5.Z.19): KD-1..KD-7, the two-iteration calibration table (means 6.9–10.3 → 14.7–16.1 m/s; VFloor 10 → 24; goals/shot ROSE 0.14–0.25 → 0.38–0.42 — the keeper's conversion now dominant), ERR-008-016 + ERR-006-004 + ERR-001-005 back-props, and the `match-engine-shot-speed` acceptance evidence (5 of 7 predicates fail pre-fix by execution) |
| `docs/tracking/gk-catch-parry-conversion-design.md` | Design supplement (v1.0, Jul 28, 2026 — **LANDED, AR converged**) — the keeper catch/parry conversion pass (§5.Z.20): the measured funnel (window at contact 0.000 → 0.30–0.67, catches 1 → 6 of 15 contacts, goals 14.7 → 8.0/match), ERR-011-005/006 back-props, the KD-C3 recalibration inside #11 spec ranges, and the recorded contact-rate + pointQuality residuals |
| `docs/tracking/gk-contact-rate-design.md` | Design supplement (v1.0, Jul 28, 2026 — **LANDED, AR converged**) — the keeper contact-rate pass (§5.Z.22): the per-episode anatomy (baseline 9 of 15 crossed episodes dive-early by 456–2000 ms), ERR-011-007 (#11 §3.3.6 commit-to-arrival gate + the first-decision-opportunity window anchor) + ERR-012-010 (#12 §3.3.3 ball-line GK slot) back-props, the measured result (contact rate 35% → 72%, catches 6 → 10, goals 14 → 15 unchanged at n=3), and the conversion-at-contact residual |
| `docs/tracking/shot-volume-design.md` | Design supplement (v1.0, Jul 28, 2026 — **LANDED, AR-1..AR-4 converged**) — the shot-volume pass (§5.Z.21): the baseline distance/churn measurement (means 30–34 m, ~60% beyond 22 m), ERR-008-017 (`DistanceQuality_SHOOT`), the four-rung falloff ladder that refused half the design target (count ≈ 25 AND mean ≤ 22 m not jointly reachable — close-chance creation is churn-bounded), the FALLOFF = 8 distribution/goal-rate landing (shots 34.7 → 17.7, goals 8.0 → 4.7), and the recorded churn/creation + dead-midfield-branch residuals |
| `docs/tracking/env-fingerprint-float-model-hash-mono-mapping.md` | Proposal (v0.2, Jul 19, 2026 — **APPROVED, Option A**) — resolved the #16 §4.8.3 `floatModelHash` tuple vs. the Stage-0 Mono pin (ERR-016-006); options A/B/C + owner sign-off. §4.8.3/§5.5 edits + live-host hasher landed same day; §4.8.2 runtime MXCSR validation + certified capture stay host-blocked |
| `docs/tracking/stress-test-strategy.md` | Tier A/B/C spec stress-test probe strategy (v1.0, May 18, 2026) |
| `docs/tracking/stress-reports/INDEX.md` | Index of all stress-test run reports |
| `docs/tracking/stress-reports/2026-05-18-tier-a-run-1.md` | Tier A Run 1 report (May 18, 2026) — 3 FAIL, 2 WARN; all 3 FAILs resolved before Run 2 |
| `docs/tracking/stress-reports/2026-05-18-tier-a-run-2.md` | Tier A Run 2 report (May 18, 2026) — 2 FAIL, 1 WARN (×147); FAIL-4/FAIL-5 fixed in this commit; OBS-1 closed |
| `docs/tracking/stress-reports/2026-05-18-tier-a-run-3.md` | Tier A Run 3 report (May 18, 2026) — 1 FAIL (FAIL-6: `[EST]` body-text in #12 §3 + §6.1) fixed in this pass; zero open FAILs after run |
| `docs/tracking/stress-reports/2026-05-18-tier-a-run-4.md` | Tier A Run 4 report (May 18, 2026) — 1 FAIL (FAIL-7: `ATTACK_DWELL_TICKS [EST]` in #15 §1.4) + FIND-12 (headers) + FIND-13 (checklist evidence); all fixed; A-16 triage inaugurated (10/147) |
| `docs/tracking/stress-reports/2026-05-19-tier-a-run-5.md` | Tier A Run 5 report (May 19, 2026) — A-16 triage full corpus sweep; 167/167 entries, all XC- confirmed, 0 open; 0 new FAILs |
| `tools/spec-stress/reports/a16-triage.json` | A-16 normative-constraint-audit triage state — 167 entries (all XC- confirmed), 0 open; COMPLETE |

---


*(June 12, 2026: `docs/tracking/dotnet-ci-quarantine.md` added — human-readable quarantine ledger for the dotnet CI gate; machine mirror at `tools/dotnet-ci/known-failures.txt`.)*

## Design References

Non-normative visual references. Nothing here is on a build path, read by the sim, or part of any
snapshot/digest; where a reference and an APPROVED spec disagree, the spec wins.

| File / folder | Purpose |
|---------------|---------|
| `docs/design/ui-mockups/README.md` | Index + scope contract for the UI mockups (v1.0, Jul 25, 2026) |
| `docs/design/ui-mockups/Soccer Manager Pro - Design System.html` | Design-system page: color, type, spacing, components, data-viz, match-day HUD; two visual directions (`stadium` / `touchline`, neither chosen yet) |
| `docs/design/ui-mockups/Desktop Guardrails.html` | Desktop layout/resolution guardrails (1920×1080 reference stage) |
| `docs/design/ui-mockups/Command Palette.html` | Global command-palette / navigation pattern |
| `docs/design/ui-mockups/*.html` (11 screens) | Screen mockups — Squad, Tactics, Training, Scouting, Transfers, Club, Club Finances, Club Staff, Club Board Room, Club History, World |
| `docs/design/ui-mockups/assets/` | Shared mockup assets — 8 `.css` (incl. `tokens.css`), 3 `.js`, 4 `.jsx` tweak panels |
| `docs/design/ui-mockups/screenshots/squad-check.png` | Reference capture of the squad screen |

Landed July 25, 2026 as the visual reference for UI / Client Framework **#38** (framework slice,
APPROVED Jul 22, 2026) and the Wave-7 screen specs it defers to (#38 §7.1). All mockup data is
hardcoded and illustrative.

---

## Planning Documents

| File |
|------|
| `docs/planning/master-development-plan.md` |
| `docs/planning/master-plan-amendment-01-audio-multiplayer-transport.md` |
| `docs/planning/master-vol-1-physics-core.md` |
| `docs/planning/master-vol-2-human-systems.md` |
| `docs/planning/master-vol-3-club-operations.md` |
| `docs/planning/master-vol-4-tech-implementation.md` |
| `docs/planning/development-best-practices.md` |

---

## Current Specification Folders

All 53 spec folders now exist in `docs/specs/` (20 Stage-0 + 33 Stage-1-forward/management-layer
specs #21–#54, spanning the full #21–#26 tactical wave, the #27–#49 management-layer waves, and the
ten promoted July 27, 2026: #53, #35, #46, #36, #54, #47, #48, #50, #51, #39). This table was last
reconciled at row 26 (July 8, 2026) and had not been updated through the #27–#54 promotion waves
until this pass (July 27, 2026) — rows #27–54 below are added at folder+status granularity only;
`SPEC_INDEX.md` is the authoritative source for full per-spec detail (FR prefix, approval date,
back-prop history) and should be read alongside this table rather than have its content duplicated
here. Status reflects authoritative classification in `SPEC_INDEX.md`.

| # | Folder | Status |
|---|--------|--------|
| 1 | `docs/specs/ball-physics/` | APPROVED |
| 2 | `docs/specs/agent-movement/` | APPROVED |
| 3 | `docs/specs/collision-system/` | APPROVED |
| 4 | `docs/specs/first-touch/` | APPROVED |
| 5 | `docs/specs/pass-mechanics/` | APPROVED (re-approved May 6, 2026) |
| 6 | `docs/specs/shot-mechanics/` | APPROVED |
| 7 | `docs/specs/perception-system/` | APPROVED |
| 8 | `docs/specs/decision-tree/` | APPROVED (draft-level) |
| 9 | `docs/specs/fixed64-math/` | APPROVED (May 15, 2026) — §9 v1.0 lead-developer sign-off; §9.2 engine + gameplay owner sign-offs granted; §9.7 reciprocal `XC-016-NNN` resolved via #16 §8.3.2 documented comparator-glossary deferral (CLAUDE.md "Interface Design Principle"). §9.8 implementation-time deliverables (golden-vector corpus, CI bench, harness digest, owning-team ledger) remain post-APPROVED follow-ups. Implementation deferred to Stage 5 per §8.1 v0.2. |
| 10 | `docs/specs/heading-mechanics/` | APPROVED May 16, 2026 (v0.3; section files 1–9 + appendices + outline + outline-PASS-1 + section-files-PASS-1-adversarial-review; ERR-010-001 RESOLVED) |
| 11 | `docs/specs/goalkeeper-mechanics/` | APPROVED (May 18, 2026) — section files v0.2; PASS-1 adversarial review (11 findings: 3 H / 5 M / 3 L) resolved same day; ERR-011-001 CLOSED (`DOMAIN_TAG_GOALKEEPER = 0x1D [CROSS: #16 §3.4]` in #16 §3.4 v1.0.5); GK constants `GK_DEPTH_M` / `GK_ADVANCE_FACTOR` / `GK_LATERAL_FACTOR` promoted `[EST]` → `[GT]`; lead-developer R-01..R-05 signed. 44 FRs; ~79 constants; 4 RNG draw sites. |
| 12 | `docs/specs/positioning-ai/` | APPROVED (May 18, 2026) — section files v0.3 (FAIL-4 fix pass); PASS-1 adversarial review (21 findings: 7 H / 9 M / 5 L) filed and resolved in v0.2 (May 16); ERR-012-001 CLOSED (`DOMAIN_TAG_POSITIONING_AI = 0x17 [CROSS: #16 §3.4]` in #16 §3.4 v1.0.5); Appendix A.1–A.8 derivations confirmed; GK constants promoted `[EST]` → `[GT]`; lead-developer R-01..R-05 signed. 47 active FRs; 18 `[GT]` + 7 other constants; no `[EST]` remain. |
| 13 | `docs/specs/pressing-ai/` | APPROVED (May 17, 2026) — v0.3. All gate items resolved: ERR-013-001 Option B (`TacticalContext.PressDirective?` nullable field added to DT #8 §2.2.6); ERR-013-004 (`"Fatigue System #13"` → `"Pressing AI #13"` in DT #8 §3.1.8.1); ERR-013-005 (`DOMAIN_TAG_PRESSING_AI = 0x19 [CROSS]` in #16 §3.4 v1.0.3); ERR-013-007 / ERR-013-008 (`GetPhase` / `GetLine` Stage 1 accessor declarations in #12 §4.5.1); Appendix A derivations for TRIGGER_DWELL_TICKS / TRIGGER_RELEASE_TICKS / ROLE_DWELL_TICKS / INTERCEPT_LOOKAHEAD_TICKS all `[EST]` → `[GT]`; §4.4.3/§4.4.4/§4.5.2/§4.6 updated (Option B mechanism); §1.6 boundary table `[CROSS-PENDING]` → `[CROSS]`; T-C-/T-X- test-prefix table added to #19 §3.1.4; lead-developer R-01..R-05 signed 2026-05-17. OI-002 (Stage 1 channel-registry rows) open non-blocking per §9.6. |
| 14 | `docs/specs/defensive-ai/` | APPROVED (May 18, 2026) — section files v0.4 (FAIL-4 fix pass); PASS-1 adversarial review (17 findings: 6 H / 7 M / 4 L) filed and all resolved in v0.2 fix pass same day; ERR-014-001 CLOSED (`TacticalContext.MarkDirective?` nullable field added to #8 §2.2.6 v1.1.3); ERR-014-004 CLOSED (`DOMAIN_TAG_DEFENSIVE_AI = 0x1A [CROSS: #16 §3.4]` in #16 §3.4 v1.0.5); lead-developer R-01..R-05 signed. 37 FRs; 26 constants (22 `[GT]` + 4 `[CROSS]`); ≥85 tests; ≤0.12 ms per-tick budget. ERR-014-002/003 Stage 1 non-blocking open. |
| 15 | `docs/specs/attacking-ai/` | APPROVED (May 18, 2026) — section files v0.2 (section-1.md through section-9-approval-checklist.md + appendices.md); PASS-1 adversarial review (7 findings: 1 H / 5 M / 1 L) filed and all resolved in v0.2 fix pass; lead-developer R-01..R-05 signed; ERR-015-001 CLOSED (DOMAIN_TAG_ATTACKING_AI = 0x1B allocated in #16 §3.4 v1.0.4); ERR-015-002 CLOSED (AttackIntent[]? added to #8 §2.2.6 v1.1.2; §3.1.7 RUNNER override note added); ERR-015-005 CLOSED ("Attacking AI (#15)" added to #8 §1.3.2 v1.1.2); ERR-015-003/004 Stage 1 non-blocking open. 36 FRs; 38 constants (33 [GT] + 4 [CROSS] + 1 [DERIVED]); 85 tests. |
| 16 | `docs/specs/deterministic-sim/` | APPROVED (May 14, 2026, later same day) — Tier 2 Final Approval. §9 v1.7. All §9.4.2 gates cleared: §9.5 #4(a)/(b)/(c) spec-level sub-conditions SATISFIED (golden-vector files `hkdf-sha256-kat.md` v1.1, `siphash-2-4-kat.md` v1.1, `serialize-canonical-corpus.md` v1.0); §8.3.1 cross-spec re-audit COMPLETE (§8 v1.2, all four upstream rows promoted to `complete`); §9.3 sign-offs (lead-developer Tier 2, QA-automation, platform-certification) granted, platform-certification with explicit Stage-0 host-platform-pin caveat. ERR-017-001 closed atomically via `DOMAIN_TAG_EVENT_LEDGER = 0x15` allocation in §3.4 v1.0.1 (no `DETERMINISM_DIGEST_VERSION` bump). |
| 17 | `docs/specs/event-system/` | APPROVED (May 13, 2026) — 10 section files + appendices; section-files PASS 1 + PASS 2 adversarial review applied; lead-developer sign-off complete |
| 18 | `docs/specs/performance-optimization/` | APPROVED (May 15, 2026, later same day still) — §9 v1.0 lead-developer sign-off. v1.0 `[TBD-NORMATIVE]` sweep landed across §1 / §2 / §3 / §4 / §5 / §6 / §8 / appendices (60 citation-qualifier instances resolved against #16 APPROVED text + #19 v1.0.1 patch-revised text). §9.4.1 blocker list all RESOLVED. KD-2 sequencing satisfied. FR count 82 (FR-PO-019 split into 019 MAY + 019a MUST; FR-PO-058a emission constraints). KD-3 inverted: #18 owns trace pipeline; #16 retains §3.2.4.1 / §3.1 / §5. |
| 19 | `docs/specs/testing-strategy/` | APPROVED (May 15, 2026, later same day) — §9 v1.0 lead-developer sign-off; v1.0.1 `[TBD-NORMATIVE]` sweep landed across §1 / §3 / §4 / §5 / §6 / §8 / appendices against #16's APPROVED text and #18's IN REVIEW v0.3 surface (51 citation-qualifier instances resolved); KD-2 sequencing satisfied. Clears #18's last KD-2 gate. |
| 20 | `docs/specs/code-standards/` | APPROVED (May 11, 2026) — 10 section files + appendices; adversarial review pass-1 applied; lead-developer R-01..R-05 sign-off complete |
| 21 | `docs/specs/tactical-instructions/` | APPROVED (Jun 20, 2026) — 11 section files + 2 adversarial-review files; promoted from `tactical-instruction-layer-design.md` v0.3 |
| 22 | `docs/specs/living-world/` | APPROVED (Jun 22, 2026) — 11 section files + 10 adversarial-review files; promoted from `living-world-system-design.md` v0.7 |
| 23 | `docs/specs/dismarking-ai/` | APPROVED (Jul 10, 2026) — 12 files; PASS-1 0H+1M+3L resolved Jul 8; §8.2 both rows VERIFIED; back-props ERR-021-005/012-007/008-012 filed + landed at approval; FR-DM-001..018 |
| 24 | `docs/specs/build-up-structures/` | APPROVED (Jul 10, 2026) — 12 files; PASS-1 0H+3M+2L resolved Jul 8; back-props ERR-021-006/012-008 filed + landed at approval; append order pinned #23 → #24 → #25; FR-BU-001..016; KD-3 records the deliberate TransitionWon-gating refinement vs the supplement |
| 25 | `docs/specs/positional-rotations/` | APPROVED (Jul 10, 2026) — 12 files; PASS-1 1H+1M+3L resolved Jul 8 + PASS-2 clean at H/M; Appendix A complete for all three `FormationFamily` members; back-props ERR-021-007/012-009 (incl. the #12 `SlotIndex` single-writer amendment) filed + landed at approval; FR-RO-001..018 |
| 26 | `docs/specs/tactical-presets/` | APPROVED (Jul 10, 2026) — 12 files; PASS-1 0H+1M+2L resolved Jul 8; §8.2 fully closed (Bradley & Noakes 2013 verified Jul 10); no back-props (§2.3); engine-substrate gates carried forward upstream-owned; FR-TP-001..020 |
| 27 | `docs/specs/squad-player-data/` | APPROVED (Jul 22, 2026) — implemented at `src/player-database/` |
| 28 | `docs/specs/player-progression-lifecycle/` | APPROVED (Jul 23, 2026) — implemented at `src/player-progression/` (T0 only) |
| 29 | `docs/specs/training-system/` | APPROVED (Jul 23, 2026) — no assembly |
| 30 | `docs/specs/season-competition-loop/` | APPROVED (Jul 22, 2026) — implemented at `src/season-save/` (T0–T3; also hosts the league bootstrap + unified season save-file root) |
| 31 | `docs/specs/transfers-contracts-negotiation/` | APPROVED (Jul 23, 2026) — no assembly |
| 32 | `docs/specs/scouting-player-knowledge/` | APPROVED (Jul 24, 2026) — no assembly |
| 33 | `docs/specs/personalities-morale-dynamics/` | APPROVED (Jul 23, 2026) — no assembly |
| 34 | `docs/specs/staff-backroom/` | APPROVED (Jul 23, 2026) — no assembly |
| 35 | `docs/specs/media-press-interactions/` | APPROVED (Jul 27, 2026) — no assembly |
| 36 | `docs/specs/national-teams-international/` | APPROVED (Jul 27, 2026) — no assembly |
| 37 | `docs/specs/match-analytics-statistics/` | APPROVED (Jul 22, 2026) — implemented at `src/match-analytics/` (T0 only, landed Jul 27, 2026) |
| 38 | `docs/specs/ui-client-framework/` | APPROVED (Jul 22, 2026) — implemented at `src/ui-framework/` (T0 substrate only) |
| 39 | `docs/specs/steam-packaging-release/` | APPROVED (Jul 27, 2026) — no assembly |
| 40 | `docs/specs/club-finances-economy/` | APPROVED (Jul 23, 2026) — no assembly |
| 41 | `docs/specs/injuries-medical/` | APPROVED (Jul 23, 2026) — no assembly |
| 42 | `docs/specs/youth-academy-intake/` | APPROVED (Jul 24, 2026) — no assembly |
| 43 | `docs/specs/competition-structure/` | APPROVED (Jul 24, 2026) — no assembly |
| 44 | `docs/specs/discipline-suspensions/` | APPROVED (Jul 24, 2026) — no assembly |
| 45 | `docs/specs/board-ownership-dynamics/` | APPROVED (Jul 25, 2026) — no assembly |
| 46 | `docs/specs/news-inbox-man-management/` | APPROVED (Jul 27, 2026) — no assembly |
| 47 | `docs/specs/new-game-setup-db-editor/` | APPROVED (Jul 27, 2026) — no assembly |
| 48 | `docs/specs/match-presentation-depth/` | APPROVED (Jul 27, 2026) — no assembly |
| 49 | `docs/specs/localization-accessibility/` | APPROVED (Jul 23, 2026) — no assembly |
| 50 | `docs/specs/save-migration-versioning/` | APPROVED (Jul 27, 2026) — no assembly |
| 51 | `docs/specs/audio-sound-design/` | APPROVED (Jul 27, 2026) — no assembly |
| 53 | `docs/specs/club-infrastructure-facilities/` | APPROVED (Jul 27, 2026) — no assembly |
| 54 | `docs/specs/manager-career-reputation/` | APPROVED (Jul 27, 2026) — no assembly |

**Notes:**
- Attacking AI (#15) files (May 17–18, 2026): `outline.md` (high-level v1.0), `outline-detailed.md` (v1.1), `adversarial-review-outline-detailed-v1.md`, `section-1.md` through `section-9-approval-checklist.md` + `appendices.md` (all at v0.2). `DOMAIN_TAG_ATTACKING_AI = 0x1B [CROSS: #16 §3.4]` (ERR-015-001 CLOSED May 18, 2026). Lead-developer R-01..R-05 signed May 18, 2026. Status: APPROVED.
- Deterministic Simulation (#16) files: `outline.md`, `section-1.md` through `section-9-approval-checklist.md`, `appendices.md`, `critique-log.md` (consolidated review history; supersedes the former `adversarial-review.md` and `third-pass-fix-log.md`, both removed May 3, 2026).
- Fixed64 Math (#9) files include `adversarial-review.md` and `adversarial-critique-pass-2.md` alongside the standard section files.
- Code Standards (#20) outline-tier files: `outline.md` (high-level v1.0), `outline-mid.md` (mid-level v1.3), `outline-detailed.md` (detailed v1.3). Section files authored from the detailed outline May 7–8, 2026; adversarial review pass-1 applied May 11, 2026; lead-developer R-01..R-05 sign-off completed May 11, 2026. Current set: `section-1.md` v1.0.1, `section-2.md` v1.0.1, `section-3.md` v1.0.1, `section-4.md` v1.0, `section-5.md` v1.0.1, `section-6.md` v1.0.1, `section-7.md` v1.0.1, `section-8.md` v1.0, `section-9-approval-checklist.md` v1.1 (APPROVED), `appendices.md` v1.1. SPEC_INDEX.md line 40 reflects APPROVED status.
- Testing Strategy (#19) files: `outline.md` (high-level v1.0 + first adversarial review), `outline-detailed.md` (v1.1 — second adversarial review applied), section-1 through section-9-approval-checklist, and `appendices.md`. Initial section-file draft authored May 12, 2026 from `outline-detailed.md` v1.1; v0.2 self-critique sweep applied same day (3 H / 6 M / 8 L findings, all resolved); status `IN REVIEW`. v0.2 corrected #16 section-number citations against current `deterministic-sim/` text (§7 → §5 regression suite, §1.3.1 → §1.1.1 tier classification, §5 → §3.2.4.1 canonical schema, deleted §8 "trace channels" — no such section). Per KD-2 sequencing in the spec, advancement to `APPROVED` is gated on (a) Spec #16 reaching Tier 2 `APPROVED`, (b) Spec #18 having at least an outline-level draft, and (c) all `TBD-NORMATIVE` tags resolved.
- Performance Optimization Strategy (#18) files: `outline.md` (high-level v1.0 with embedded adversarial review of May 6, 2026), `outline-detailed.md` (v1.1 — May 13, 2026), `section-1.md` through `section-9-approval-checklist.md` + `appendices.md`, `pass-2-adversarial-review.md`. Section files authored May 13, 2026 (v0.1) from `outline-detailed.md` v1.1; PASS-1 adversarial review (4 H / 6 M / 13 L findings, 23 total) resolved in v0.2 (May 14, 2026). PASS-2 adversarial review (2 H / 5 M / 8 L findings, 15 total) filed May 14, 2026 against v0.2; resolved in v0.3 fix pass same day. ERR-018-002..018 all resolved. PASS-2 H-1 / H-2 / M-1 traced to PR #59 + PR #60 parallel-branch merge collision on the v0.2 fix pass. Status `IN REVIEW`; lead-developer sign-off pending. KD-3 inverted: #18 owns trace pipeline; #16 retains §3.2.4.1/§5/§3.1.2. `TBD-NORMATIVE` tags throughout for #16 (IN PROGRESS) and #19 (IN REVIEW) citations. FR count is 82 (FR-PO-001 … 080 + FR-PO-019a + FR-PO-058a — FR-PO-019a split from FR-PO-019 per ERR-018-017).
- Event System (#17) files: `outline.md` (high-level v1.0), `outline-detailed.md` (v1.1), `section-files-critique-pass-1.md`, `section-files-critique-pass-2.md`, `section-1.md` through `section-8.md`, `section-9-approval-checklist.md`, and `appendices.md`. All section files authored May 13, 2026 from `outline-detailed.md` v1.1. Section-files PASS 1 adversarial critique (3 H / 5 M / 12 L findings) resolved in v0.2; section-files PASS 2 adversarial critique (2 H / 6 M / 7 L findings) resolved in v0.3. All section files at v0.3; lead-developer sign-off granted May 13, 2026. ERR-017-001 FULLY RESOLVED — #16-side May 14, 2026 (`DOMAIN_TAG_EVENT_LEDGER = 0x15` allocated in #16 §3.4 v1.0.1); #17-side May 15, 2026 (§1.0.1 patch revision across §1 / §2 / §3 / §7 / §8 / §9 / appendices — `[CROSS-PENDING]` → `[CROSS]` promotion, literal value `0x15` inlined).

---

## Naming Convention (Current)

- Spec files are stored inside per-spec folders.
- Filenames do **not** carry version suffixes.
- Git history is the version record.

Examples:
- `section-1.md`
- `section-3-1-to-3-2.md`
- `section-9-approval-checklist.md`
- `appendix-a.md`
- `audit-report.md`

---

## Maintenance Rule

Update this manifest when:
- a new spec folder is added,
- a tracking/planning file is added, removed, or renamed,
- project-level documentation paths change.
