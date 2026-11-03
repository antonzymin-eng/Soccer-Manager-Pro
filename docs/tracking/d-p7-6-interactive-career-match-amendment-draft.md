# D-P7-6 — Interactive career matches: amendment draft to Season & Competition Loop #30

**Created:** November 2, 2026\
**Last Updated:** November 3, 2026\
**Version:** 0.6\
**Status:** DRAFT — PROPOSED, NOT APPROVED. Not a normative spec. No work is authorized by this file;
it takes effect only when approved and applied to the owning specs with their ERR entries (§8).\
**Purpose:** turn the owner's D-11 product decision (roadmap §5.1 register, as amended November 2,
2026: PM-2 managed fixtures are played interactively; there are no mid-match saves; quitting a match
abandons it and returns to Career Home with the round unplayed) into a reviewable technical contract
for #30, with the cross-spec consequences for #41, #44, #26, #21, #38 and the match client named. It
replaces the bullet-list contract first recorded in
[`interactive-unity-client-design.md`](interactive-unity-client-design.md) §5-P7 *Remaining* item 2.\
**Scope:** documentation only. No source, test, schema or format change in this file.\
**Owning specs:** #30 (`docs/specs/season-competition-loop/`); consequences for #41
(`injuries-medical/`), #44 (`discipline-suspensions/`), #26 (`tactical-presets/`), #21
(`tactical-instructions/`), #38 (`ui-client-framework/`) and the client design supplement. Roadmap
ordering: [§5.1a](path-to-playable-roadmap.md#51a-working-sequence-to-early-access--november-2-2026)
career lane item 2.

---

## 1. What exists today (verified against `main` `4bcc2ae`; `src/` is unchanged at the branch head)

Two thread names are used throughout, because the code's own remarks say "sim thread" for both. The
**season thread** is the thread that calls `SeasonLoop` commands; the class declares no thread safety
(`SeasonLoop.cs` l.132–135), so there is one at a time. The **tick thread** is the thread that calls
`RunTick`: the streamer's pacing thread under `MatchSession.Start`, the caller under `TickOnce`.

| Fact | Source |
|---|---|
| A round resolves inside one synchronous call. `PlayNextRound` runs the matchday-morning career day-steps, then resolves fixtures **in fixture-index order**, applying each one immediately (appearances → table → outcome event → mark played → ban serving → card commit), then advances the cursor. | `src/season-save/SeasonLoop.cs` `PlayNextRound` (~l.1033–1180); #30 §3.4 |
| KD-9 / FR-SN-012: a fixture-day resolves the **whole** round; resolving a strict subset is forbidden. | #30 §1.4 KD-9, §2.1 FR-SN-012 |
| Which fixtures reach an engine depends on `RoundResolutionMode`. Under `ManagedThroughEngine` only the managed fixture does and the rest go through `RoundResolutionModel.Resolve(fixture, seed, seasonNumber, homeMean, awayMean, worldDay)` (keyed per fixture, no shared cursor). Under `QuickSimAll` none does, the managed fixture included. Under `FullEngine` every fixture does. The per-fixture work that can throw — resolve (F6), the availability filter (F9 can throw), `FieldedXi`, `RosterIds` — runs **inside the apply loop**, one fixture at a time, ahead of that fixture's table write. | `SeasonLoop.ResolveFixture` (~l.1804), `ShouldPlayThroughEngine` (l.1867); `RoundResolutionMode.cs` |
| The matchday-morning career day-steps are slot 1 (#28 progression), slot 2 (#29 training) and slot 4 (#41 medical, including the injury occurrence draw). They run after every guard and before any fixture, and are idempotent per day through per-player cursors. Those cursors must sit within one day of the clock (`[clock − 1, clock]`), so a state with the fixture day's steps run and the round unplayed is a coherent state; the D-P7-2 post-round autosave already writes the same cursor shape with the round played. | `SeasonLoop.cs` `RunCareerDaySteps` (l.1737), called at l.1103 and l.1708; `PlayerCareerStates.RequireTrainingCursorWithinClock` (l.594); ERR-030-027 |
| The managed engine is booted by `BootFixtureEngine` (availability filter, #29 entry fatigue, `ConfigureSquads`) from a fixture-keyed seed. `PlayThroughEngine` seeds a `CardLedgerFold` from `PlayerIdsByAgentId()` before the first tick and pumps `fold.ObserveTick(tap)` after **every** `RunTick`; a skipped pump loses that tick's cards. It increments `EnginePlayedFixtures` after the loop, and `EmitMatchOutcome` appends to `MatchOutcomes`; neither counter is serialized. | `SeasonLoop.PlayThroughEngine` (~l.1905–1960); `EnginePlayedFixtures` (l.625), `MatchOutcomes` (l.654) |
| `BootFixtureEngine` configures squads only: no `ConfigureManager`, no tactic, no `ManagerAdaptation.ApplyKickoff`. Both teams therefore run at the engine defaults, `ManagerMode.Human` (inert, #26 KD-4) and `TeamTactic.Balanced`, so the career opponent never selects a kickoff preset or adapts in-match: wiring-backlog W7 is dead on this path. | `SeasonLoop.BootFixtureEngine` (~l.2016–2045); `MatchEngine.cs` ~l.334, ~l.870, ~l.885 |
| PM-1's `MatchSession.BootEngine` applies `ConfigureSquads` → `ConfigureManager` (AI side) → `ManagerAdaptation.ApplyKickoff(engine, catalogue, new TeamTacticConfig(home, away))` before any tick: the human's setup tactic is a **boot baseline** on the #26 FR-TP-004/010 path, not a command. The command queue drains at the top of the tick after enqueue and carries only live mutators. | `src/match-client-core/MatchSession.cs` l.114–155; client design §6.2 |
| `ManagerAdaptation` and `TeamTacticConfig` are in `match-engine`; `TeamTactic`, `ITacticPresetCatalogue` and `InCodeTacticPresetCatalogue` are in `tactical-instructions`, which `season-save.asmdef` does not list. | the `.asmdef` files; `src/tactical-instructions/` |
| The event bus is process-static: `EventLedger` is an `internal static` class with static buffers, dispatch table and phase state, and every engine boot calls `EventBus.ResetForNewMatch()`. Engines therefore run one at a time. Booting or ticking a second engine while another is live shares and resets that bus; this is inferred from the static design, not demonstrated, and §2.9 treats it as binding. | `src/event-system/EventLedger.cs` l.130; `MatchEngine.cs` l.1098–1107 |
| `MatchEnded` flips **inside** the tick (`CheckMatchFlowTransitions` from the input phase, l.3467 and l.3507), before the streamer's post-tick observer runs for that tick. Full time only auto-pauses the pacing loop (`ApplyCapturedFrame`, `LiveMatchStreamer.cs` l.336–345); the pacing thread stays alive until `Stop()`, which joins it (l.436–450). `MatchSession.MatchEnded` is documented as a racing read while the pacing thread runs. | `LiveMatchStreamer.cs`; `MatchSession.cs` l.205–218, l.273 |
| The fold tracks slot **occupancy** through `SubstitutionEvent`s, so cards are attributed correctly across substitutions. Cards sit in `_pending` until `Commit`. Its lossless-consumption check anchors on its first `ObserveTick` call, refuses a non-consecutive tick after that, and latches `_faulted` if a tick throws part-way. It can therefore see an interior gap and a latched fault; it cannot see a missed first tick or missed trailing ticks. It is not serialized, which is safe only while no save can be taken mid-fixture. | `src/discipline/CardLedgerFold.cs` type remarks (L2, L19), l.111–124, l.336–361 |
| `FieldedXi` returns the **starting** eleven, and is non-null exactly when a career or discipline is wired. Both #41's appearance record and #44's ban-serving exemption consume it; the code records (M5) that they are correct only because no production path substitutes. | `SeasonLoop.FieldedXi` remarks (~l.2105–2122) |
| The depleted-squad back-fill reinstates removed players when a club cannot satisfy the full eighteen-player selection walk: tier 1 the injured, tier 2 the suspended. Reinstated players can be composed onto the **bench**, and the ERR-030-046 search exists to put reinstated-suspended players there rather than in the eleven. | #30 §3.4 (ERR-030-042, ERR-030-044, ERR-030-046); `src/season-save/tests/AvailabilityCompositionExtremisTests.cs` header |
| `RequestSubstitution` checks indices, match end, sent-off / already-replaced / already-pending slots, bench-slot reuse, the substitution cap, windows and the keeper role. It checks no player eligibility. At `CurrentTick == 0` an accepted request is executed at once as a free stoppage (`SubstitutePlayer`) instead of being queued, and the client's drain runs ahead of tick 1, so a substitution enqueued before kickoff is an immediate starting-XI change. Its only production caller is the client's `ManagerCommand.Substitute` through `MatchEngineMutations`; inside the engine only its own pre-kickoff branch calls `SubstitutePlayer`. | `src/match-engine/MatchEngine.cs` l.1613–1680, l.1766–1775; `src/match-client-core/ManagerCommand.cs`, `MatchEngineMutations.cs` l.49 |
| `SeasonSaveManager.Save(SeasonLoop, MatchEngine matchOrNull, path)` writes whatever engine it is handed; its remarks say `ActiveMatch` is not a safe source and ask the caller to pass an engine it owns. `CareerSaveSlots.Write` calls it with `null` (l.217; the comment at l.75 records that the client never writes a match block), and `CareerSession` refuses to load a slot holding one. `MatchSession.CaptureSave` is public. | `SeasonSaveManager.cs` l.440–461; `CareerSaveSlots.cs` l.75, l.217; `CareerSession.cs` l.185; `MatchSession.cs` l.326 |
| `CareerProgress.NextAction` derives its one action from calendar and clock only: on the fixture day it returns `PlayMatchday` whatever any round state, so that one value means both "play my match" and "sim the round". `CareerSession.Execute` checks the action is that legal one, runs it (`PlayMatchday` calls `AdvanceAndPlayNextRound()`, which plays a managed fixture through the engine headlessly under the production default `ManagedThroughEngine`; the client tests use `QuickSimAll`), throws "Unreachable" for a value its `switch` does not name, and autosaves after every action except `AdvanceDay`. The season dispatcher maps `AdvanceRound` onto `Execute(PlayMatchday)` and cannot tell the two meanings apart. `CareerHomeSource` feeds `session.IsBusy` into `IsProgressBlocked`, which is false between operations. | `CareerProgress.cs` l.40–55; `CareerSession.cs` l.252–290; `SeasonCommandDispatcher.cs`; `CareerHomeSource.cs` l.136; `CareerSession.cs` l.114 |
| The public members that change state a round reads or a save persists are: on `SeasonLoop`, `AdvanceToNextFixtureDay`, `AdvanceDays`, both `AdvanceAndPlayNextRound` overloads, `RollToNextSeason`, `ApplyTransaction`, `SubmitTransferBid`; on `PlayerCareerStates` (reached through `SeasonLoop.Career`), `TrySetFocus`; on `DisciplineRules` (a public type that wraps the `DisciplineState` which `SeasonLoop.Discipline` exposes), `ApplyCard`, `AddYellow`, `AddBan`, `OnClubFixturePlayed`, `RollToNextSeason`, `MigratePlayerId`, `DropPlayer`. `SeasonState`'s mutators and its `Table` accessor are `internal` (`LeagueTable` is a public type); `ProgressionEngine`, `DisciplineState` and `TransfersSnapshot()` (a clone) expose reads only; `PlayerCareerStates.MarkUnavailable` writes only the caller's arrays. No source in `client-app` or `match-client-core` constructs `DisciplineRules` or calls `TrySetFocus`. | `SeasonLoop.cs` (public members); `PlayerCareerStates.cs` l.1356, l.1473; `DisciplineRules.cs` l.81–486; `SeasonState.cs` l.63, l.309–378 |
| The career client never reads a match block, and `season-save` does not reference `match-client-core`; `client-app` references both. | `CareerSession.cs:185`; the three `.asmdef` files |
| `MatchSession` has a private engine-wrapping constructor, a public `AttachTickObserver` (runs after every tick on the **tick thread**) and `TickOnce`. `AttachTickObserver` forwards to `LiveMatchStreamer.SetPostTickObserver`, which is **set-once**; PM-1's `SessionMatchAnalytics.Attach` takes that slot on every match, and a second attach throws. `InvokePostTickObserver` disarms the whole observer on its first exception and latches `PostTickObserverFault`. Two production readers key on that property: the web host's `MatchClientHost.AnalyticsFault` (its own aggregator has no other fault latch) and `S0ScreenPresenter`'s statistics refresh, which republishes at once when the streamer holds a fault the cached publication does not yet show. `SessionMatchAnalytics.ObserveTick` latches its own `_incomplete` before it rethrows, and the PM-1 Post-Match report reads that publication. Tests pin the set-once slot and the streamer fault on `MatchSession(MatchSetup)` sessions. | `src/match-client-core/MatchSession.cs` l.72–107, 247, 287; `src/match-viewer/LiveMatchStreamer.cs` l.255–307; `SessionMatchAnalytics.cs:39`, l.69–70; `ClientMatchCoordinator.cs:104`; `MatchClientHost.cs:63`, `:126`; `S0ScreenPresenter.cs:504`; `ClientMatchContext.cs:63`; `ClientMatchCoordinatorTests.cs:94`; `MatchClientHostTests.cs:181–190`; `SessionMatchAnalyticsTests.cs:68`; `S0DemoFixtureComparisonTests.cs:95` |

## 2. Contract (proposed)

### 2.1 Owner decisions this contract implements

- **Interactive managed fixtures** (D-11).
- **No mid-match saves** (D-11 as amended November 2, 2026). Saving is unavailable from entering the
  matchday until the player is back on Career Home. Read precisely: from `BeginManagedFixture` until
  `CompleteManagedFixture` or `AbandonManagedFixture` returns, the season side refuses every save
  (§2.10); the player's manual save is offered on Career Home only; and the D-P7-2 post-round autosave
  is written by the client when `CompleteManagedFixture` has committed the round, as D-P7-2 already
  writes it after every played round. Mid-match saving was costed in v0.1 of this draft (a new
  managed-fixture sub-blob, season-save format 8 → 9, a restore path, and every engine snapshot schema
  change invalidating mid-match saves) and withdrawn.
- **Quitting a match abandons it** and returns to Career Home with the round unplayed. No result,
  card, appearance or table change from the abandoned match is kept. No forfeit, no instant
  finish, no kickoff autosave: a player who wants to replay a matchday can already reload a manual
  save, so blocking replays would prevent nothing.
- **No starting-XI changes in PM-2** (owner answer to v0.1 Q1, November 2, 2026): D-P7-4's automatic
  selection stands; Tactics Setup changes team tactics only.

A crash or an application exit during a match loses the match and everything since the last save, as
an exit anywhere else in the career does today.

### 2.2 Round order and one implementation

Fixtures keep resolving in fixture-index order and are applied by the existing per-fixture block. The
managed engine is booted at `Kickoff`, after Tactics Setup and before any fixture is applied; its
result is folded in at its own index position during `CompleteManagedFixture`.

**One implementation, every mode.** `PlayNextRound` splits in two internal halves. `BeginRound(squads)`
runs for every `RoundResolutionMode`, with or without a managed fixture, and does what `PlayNextRound`
does before its apply loop: the guards, `RequireCommittableConfig`, `RunCareerDaySteps`, and the round
plan (§2.4.1). The plan holds each fixture's inputs that can fail (F6, F9, `FieldedXi`, `RosterIds`) and,
where the mode resolves through the round model, the result: every non-managed fixture under
`ManagedThroughEngine`, every fixture under `QuickSimAll`. Under `FullEngine` the plan holds the inputs
only and each engine match still plays inside `CompleteRound`, as today. `CompleteRound(plan)` is the
apply loop. The two public `AdvanceAndPlayNextRound` overloads keep their provider checks (F7) and run
`BeginRound` → [a managed fixture that runs through an engine: boot with `PreKickoffConfig.Default`, tick
to full time pumping the same per-tick observation as §2.4] → `CompleteRound` inside one call. **They do
not use `RoundState`:** it stays Closed, there is no Open, Live or Faulted on this path, and its
retry-after-throw posture is unchanged (§2.4.1). `BeginManagedFixture` is the public wrapper over
`BeginRound(_careerSquads)` for the interactive route and accepts fewer cases than the synchronous call
(§2.4); a case it refuses keeps the synchronous implementation.

**The failure point moves on the synchronous path.** Today an F6/F9 throw for fixture *k* leaves fixtures
0..*k*−1 applied and marked played; after the split it throws in `BeginRound` with nothing applied. Both
are retryable, the new one earlier. A grep of `season-save/tests` for partial-apply wording found no test
that pins the old partial state (the comments at `SeasonLoopCareerTests.cs:93` and
`SeasonLoopProgressionTests.cs:304` guard constructor refusals). `AppearanceRecordTests`' recording-throw
test (l.~409) is expected to keep its assertions, since the validation moves earlier and still leaves the
fixture unplayed; the landing confirms it. T-IM-16 carries a clause for this (§5).

Booting the managed engine before the earlier-indexed fixtures are applied is expected to give the same
engine: the managed squad's availability, entry fatigue and seed do not depend on other clubs'
fixtures, and nothing is applied between `Begin`, `Kickoff` and the first tick. Lock T-IM-01 proves
interactive and headless runs agree; **it compares new code with new code**, so the claim that the
synchronous path is unchanged from today rests on T-IM-16, which compares it with values pinned from the
pre-change head (§5). Whether the headless digest itself is unchanged from today depends on Q1 (§3):
under Q1 (b) it is expected unchanged but **not yet verified** (Q1 (b) says why) and the landing shows it
on the existing headless `ManagedThroughEngine` fixtures; under Q1 (a) the managed fixture's digest
changes, the landing carries digest-change evidence, and T-IM-16 still holds for everything that does
not depend on the managed engine (non-managed results, their table rows, the day-step state).

### 2.3 Round states

| State | Entered by | Season state | Season-side `Save` |
|---|---|---|---|
| **Closed** | cursor advance; `AbandonManagedFixture`; any load; construction | pre-round, **or** fixture day with the matchday-morning steps run and the round unplayed (the post-abandon shape, §1; its mutators are refused, below) | yes, as today |
| **Open** | `BeginManagedFixture` | matchday-morning steps done; round plan computed (§2.4.1); pre-kickoff preview computed; **no engine**; nothing applied | refused |
| **Live** | `Kickoff` (engine booted; the first tick follows); stays Live through full time until `Complete` or `Abandon` | as Open | refused; the client calls `CompleteMatchday` before Post-Match |
| **Faulted** | an exception from `CompleteManagedFixture` **after its first write** (§2.4.1) | partly applied; undefined | refused |
| **Committed → Closed** | `CompleteManagedFixture` | round fully applied, cursor advanced | yes; the D-P7-2 post-round autosave follows |

There is no stored "Ended" state. Full time is read from the engine by `Complete`, after the tick thread
has stopped (§2.9); the season thread cannot observe that join, and a state entered by reading
`MatchEnded` before it would be the racing read §2.9 forbids.

`AbandonManagedFixture` from Open discards the preview and the round plan. From Live it discards the
engine, the fold and the participation record as well. Either way it returns to Closed. **It is refused
in Faulted**, where "nothing was applied" is no longer true. A `CompleteManagedFixture` that is refused
before its first write (any precondition in §2.4, full time not yet reached included) leaves the round
Live and Abandon is permitted. The matchday-morning steps that `Begin` ran stay run; they are idempotent
for the day, so the next `Begin` repeats them as no-ops and, because nothing else may run in between
(below), computes the same plan and preview, and `Kickoff` with the same `PreKickoffConfig` boots the
same engine. The post-abandon Closed state is saveable and a state the career can be loaded into
(T-IM-15).

**Refusals while the round is not Closed.** Rule: every public member of `SeasonLoop`, and of each
object it hands out, that changes state a round reads or a save persists is refused with a named
failure mode (new F-row). Enumerated from the real surface (§1), not from memory: `AdvanceDays`,
`AdvanceToNextFixtureDay`, both `AdvanceAndPlayNextRound` overloads, `RollToNextSeason`,
`SubmitTransferBid`, `ApplyTransaction`, a second `BeginManagedFixture`, `SeasonSaveManager.Save(loop,
…)`, and `PlayerCareerStates.TrySetFocus` (reached through `SeasonLoop.Career`; `PlayerCareerStates`
holds no loop reference, so the loop sets an `internal` flag on it that `TrySetFocus` checks, spanning
`Begin` to the round's application, an abandon and a load of that state included). `DisciplineRules`'
public writers over `loop.Discipline` cannot be refused by the loop, which never sees the call; that is
a caller contract, recorded at #30 §4.3, and the landing verifies by grep that no client assembly
constructs `DisciplineRules`. `Kickoff` is accepted only in Open. Read-only views stay available. T-IM-06
carries the list and fails when a new public mutator is on neither it nor the read-only allowlist.

**Refusals after an abandon.** The post-abandon state is `clock == the pending round's fixture day` with
the career's day-step cursors at the clock (§1); it ends when `Complete` applies the round. While it
holds, `ApplyTransaction`, `SubmitTransferBid` and `TrySetFocus` are refused with their own named F-row;
`BeginManagedFixture` and the synchronous `AdvanceAndPlayNextRound` overloads stay legal. Why refuse
rather than order: the steps ran before the command, so a focus change would miss the day's training
slot and a transfer would change the squad the next `Begin` composes, and "the next `Begin` computes the
same plan" would be false. Allowing them would need an ordering rule per mutator; refusing keeps an
abandoned-and-replayed matchday equal to an uninterrupted one (T-IM-15). The visible cost: after an
abandon the player cannot change focus or transfer until the round is played, and Career Home shows the
reason.

### 2.4 Seams and ownership

**`season-save` (#30) owns the round, the engine, the fold and the participation record.** New public
surface on `SeasonLoop` (names indicative):

- `RoundState RoundState` and `bool CanBeginManagedFixture` (the round is Closed, the mode is
  `ManagedThroughEngine`, the loop owns a provider, and the pending round has a managed fixture; `Mode`
  is already public) — read-only views the client and `CareerProgress` read (§2.10, §2.11).
- `ManagedFixturePreview BeginManagedFixture()` — Closed only. Runs `BeginRound(_careerSquads)` (§2.2):
  the existing guards, `RequireCommittableConfig` in the position it has today (with the other guards,
  before the day-steps; `Complete` does not repeat it), the matchday-morning day-steps (slots 1, 2 and
  4), the per-fixture resolution of §2.4.1, and the **compose** half of `BootFixtureEngine` (below) for
  both managed clubs. Returns the preview: the filtered squads, the automatic starting elevens, the
  per-club ineligible-to-enter set (§2.7) and `ManagedTeamIndex` (0 when the managed club is the
  fixture's home club, else 1; the managed club may be away). Constructs no engine. Enters Open.
  Refused, each with a named F-row, when the round is not Closed; when the mode is not
  `ManagedThroughEngine` (`QuickSimAll` has no managed engine, and `FullEngine` would tick other engines
  in `Complete`, which §2.4.1 and §2.9 rule out); when the round has no managed fixture; or when the loop
  owns no squad provider (the refusal the no-argument `AdvanceAndPlayNextRound` raises). Those cases are
  served by the synchronous path (§2.2, §2.11).
- `ManagedFixtureHandle Kickoff(in PreKickoffConfig config)` — Open only. Runs the **configure** half of
  `BootFixtureEngine` on the preview's own squad instances, so the eleven Tactics Setup showed is the
  eleven configured (the ERR-041-010(b) colocation rule; no second availability walk). Boot order is
  PM-1's: `ConfigureSquads` → the opponent's manager configuration as Q1 (§3) decides →
  `ManagerAdaptation.ApplyKickoff(engine, catalogue, baseline)`, where `baseline` holds
  `config.TeamTactic` in the managed club's team slot and `TeamTactic.Balanced` in the other. That is
  the #26 FR-TP-004/010 boot path. Then seeds the fold and the `ParticipationRecord` (§2.5) from
  `PlayerIdsByAgentId()` immediately after boot (the ERR-044-023 boot-time precondition) and enters Live.
  **`Kickoff` is atomic:** on any throw no handle exists, the round stays Open and Abandon is permitted.
- `PreKickoffConfig` — PM-2 carries one field, the managed club's `TeamTactic`.
  `PreKickoffConfig.Default` holds `TeamTactic.Balanced`, the tactic the engine starts both teams at
  today. A later starting-XI choice (roadmap §5.1a career item 4 lists squad/selection) cannot be added
  as a further field: `IneligibleToEnter` and the preview are computed at `Begin` from the automatic XI,
  so a chosen XI at `Kickoff` needs both recomputed there, and a #30 §3.4 / #44 §2.3 rule on selecting
  reinstated players. Both are out of PM-2 (§6).
- **`BootFixtureEngine` splits in two.** *Compose*: resolve → availability filter → `FieldedXi` →
  `RosterIds` → ineligible-to-enter set. *Configure*: fixture-keyed seed → engine → `ConfigureSquads`
  with entry fatigue → manager configuration → `ApplyKickoff`. The synchronous path calls both back to
  back, with `PreKickoffConfig.Default` for the managed fixture, so the headless and interactive paths
  share one boot. `FullEngine` mode's non-managed engine fixtures keep both teams at Balanced, as today.
  Naming `TeamTactic` and the preset catalogue makes `season-save.asmdef` list
  `TacticalDirector.TacticalInstructions` directly (Unity's direct-reference rule, `src/CLAUDE.md`).
- `ManagedFixtureHandle.TakeEngineForSession()` — returns the booted engine **once**; a second call
  throws. It replaces a public `Engine` property: a standing public handle on a mid-fixture engine is
  both a save source (§2.10) and a mutation path around the command queue. The client passes the engine
  straight to `MatchSession.Attach`, which keeps it private. The handle keeps its own reference for the
  tap and the tick count.
- `ManagedFixtureHandle.ManagedTeamIndex` and `IneligibleToEnter` — the preview's team index and per-club set (§2.7), as player ids.
- `ManagedFixtureHandle.ObserveTick()` — one pass per engine tick: the participation record takes the
  tick's `SubstitutionEvent`s, then the fold takes the same tap (§2.5). MUST run exactly once after every
  engine tick. The handle counts observations against the engine's completed ticks and `Complete` fails
  loud on a mismatch. **That counter is new**: the fold's own consecutiveness check cannot see a missed
  first tick or missed trailing ticks (§1), and a missed tick would also be missing from the
  participation record.
- `ManagedFixtureHandle.IsObservationFaulted` / `ObservationFault` — latched by `ObserveTick`'s own
  `catch` (which rethrows to the §2.8 composite). Volatile, readable from any thread (§2.8).
- `MatchResult[] CompleteManagedFixture()` — season thread, after the tick thread has stopped (§2.9).
  Preconditions, all checked **before the first write**: the handle is valid; `Engine.MatchEnded`
  (read after the join); observations equal completed ticks; the handle is not observation-faulted and
  the fold is not faulted; no ineligible-to-enter player is in the participation set (§2.7). A refusal
  leaves the round Live. It then applies the round (§2.4.1) in index order and advances the cursor.
- `void AbandonManagedFixture()` — §2.3; season thread, after the tick thread has stopped (§2.9).

`SeasonLoop.ActiveMatch` stays the synchronous-path accessor.

**`match-client-core` gains one entry point and, on the sessions it builds, the per-tick composite (§2.8):**
`MatchSession.Attach(MatchEngine engine, int managedTeamIndex, IReadOnlyList<int> ineligibleToEnter)` wraps
an engine it did not boot (the existing private constructor), applying no boot mutator, and carries the
managed team and the §2.7 set as plain values. A session built this way is a **career-attached** session.
It refuses `CaptureSave` (§2.10). Its drain refuses, before the engine is called, a command for the other
team (`SetTeamTactic` or `Substitute` with a `teamId` other than `managedTeamIndex`, `SetPlayerTactic` for
an agent outside the managed team's slots), a substitution at `CurrentTick == 0` (§2.6) and a bench slot
in the ineligible set (§2.7). Without the team check the human could command the opponent, the AI side
under Q1 (a) included: `MatchEngineMutations` passes `teamId` straight to the engine today. That refusal
has no typed status to reuse (`SubstitutionRequestStatus` is substitution-only), so it throws a named
exception, which the driver records in `FailedCommands` as it records any throwing mutator
(`MatchClientDriver.cs` l.141). The session alone carries the §2.8 composite. The route also needs an
attached path beside PM-1's: `MatchSessionLifecycle` gains the counterpart of `CreateSession(MatchSetup)`
(same stop-the-predecessor rule), `ClientMatchCoordinator` an attached start, the match identity context is
built from the preview's squads instead of a `MatchSetup`, and the presenter's hard-coded team 0
(`S0ScreenPresenter.cs:365`, `:385`) becomes the managed team index. PM-1's `MatchSession(MatchSetup)` and
`RestoreFrom` sessions and its start are unchanged, including their single set-once post-tick slot. The
client registers `handle.ObserveTick` as the session's `First` per-tick consumer and then attaches
analytics as PM-1 does (§2.8), so the observation runs after every tick on the tick thread at any
playback speed. `match-client-core` takes no reference to `season-save`.

**`client-app` composes them** through new `CareerSession` operations and screen moves (§2.10, §2.11).

#### 2.4.1 `CompleteManagedFixture` is apply-only

Everything in the round that can fail on its inputs is computed at `BeginRound`, before `Kickoff`, and
held in the round plan:

- for the managed fixture, the compose half (§2.4);
- for every non-managed fixture under `ManagedThroughEngine`: `ResolveSquad` (F6), `SelectAvailable`
  (F9), `FieldedXi`, `RosterIds` and `RoundResolutionModel.Resolve`, so a roster-integrity throw
  happens at Tactics Setup, where the player has lost nothing, and not after a 90-minute match;
- the validating half of each appearance write (`ValidateAppearanceWrite`, run over each round club's
  whole roster, which bounds any participation set);
- `RequireCommittableConfig`;
- one fixture per club: the plan reads every club's squad once, at `Begin`, which is exact only because
  each club plays at most once per round (FR-SN-003). `BeginRound` refuses a round in which a club
  appears in two fixtures, with a named F-row, rather than trust the scheduler.

`Complete` then applies in the existing order (appearances → table → outcome event → mark played →
ban serving → card commit) from the plan, the finished engine's score and the participation record.
`EnginePlayedFixtures` and `MatchOutcomes` change only here, so Abandon leaves both untouched. The
apply-only guarantee is for the interactive route, which refuses `FullEngine` (§2.4).

What can still throw inside the apply is the residual `CardLedgerFold.CommitWithExplicitConfig` records
(its L2 note: an accumulator at `int.MaxValue`, reachable only from a corrupt decoded row). If anything
throws after the first write, `CompleteManagedFixture` latches **Faulted**: Abandon, every command and
every save are refused, and the cause is readable (`FaultCause`). The client shows it and offers only
Main Menu / Load; the recovery is the last save, as after a crash. Faulted is terminal for that loop
instance. A precondition refusal is not a fault.

**Faulted belongs to the interactive route only.** The synchronous overloads share `CompleteRound` but do
not use `RoundState` (§2.2): a throw after the first write propagates as today, the fixtures already
applied stay marked played, and a retry skips them. That is the posture
`SeasonLoopDisciplineTests`' M12 test (`AThrowInsideTheServeAndCommitBlock_…OnRetry`) and the loop's
M6/M12/M17 comments lock, and it is unchanged. The two postures live in the two wrappers, not in
`CompleteRound`. They differ because a synchronous retry replays the unplayed fixtures from their
fixture-keyed seeds, while an interactive retry has no finished match to re-apply and Abandon is unsound
once a write has landed.

### 2.5 Participation: who played

**Definition.** A club's **participation set** for the managed fixture is the players on pitch slots at
boot (the starting eleven) together with every player named as incoming by a `SubstitutionEvent` in the
fixture. A player substituted off, or sent off, stays in it. Substitutions are not accepted before
kickoff in PM-2 (§2.6), so the boot occupants are exactly the starting eleven and the set cannot hold a
starter who never played a tick. On the headless path (no substitutions) it equals the starting eleven,
so headless behaviour is unchanged. Non-managed fixtures keep `FieldedXi`.

**Owner and feed.** #30 owns a `ParticipationRecord` in `season-save`, created in `Kickoff` under the
condition `FieldedXi` already uses (a career **or** discipline is wired), independent of whether the
discipline fold exists: the fold is null with discipline unwired, but appearances need participation
whenever a career is. It is seeded at boot from `PlayerIdsByAgentId()` and takes, per tick and from the
tap's `SubstitutionEvent` records, the incoming agent's player. It tracks no occupancy; it needs only
the incoming id. The tap (today built only when discipline is wired) is built whenever the record or the
fold exists. `ObserveTick` makes one pass: record first, then `fold?.ObserveTick(tap)`. This is a second,
smaller consumer of the same per-tick stream, not a replacement for the fold's occupancy map, which
stays private to #44 (§4.1 of #44 forbids it the engine). v0.3's "beside the fold, not a second walker"
is withdrawn because it could not be implemented without a #44 API change.

- **#41 appearance record** (`RecordFixtureAppearances`, FR-MD-010 window): proposed rule — any minutes
  on the pitch is an appearance, so the record takes the participation set. **#41 owner sign-off
  needed** (ERR on #41).
- **#44 ban serving** (`OnClubFixturePlayed`, ERR-044-003 stage 1 exemption): this is **conformance,
  not a new rule.** FR-DC-011 already says the "fielded eleven" it requires is "the eleven that actually
  took part, not merely the eleven that started" (ERR-044-010), and #44 §4.5 already makes it a
  composition-root MUST to widen the array "the moment a substitution seam gets a production caller"
  ("today that eleven is the STARTING eleven only" is §4.5's sentence). The interactive route is that
  caller on the season path. The application passes the participation set where `homeXi`/`awayXi` are
  passed today. **The #44 text lands with the code** (§8): until `OnClubFixturePlayed` receives the
  participation set, wording that says the MUST is satisfied would be false. It then updates the existing
  ERR-044-010 row (recorded, not fixed → closed) and refreshes FR-DC-011's "`SeasonLoop.FieldedXi`
  supplies the STARTING eleven today" sentence, §3.3's SUBSTITUTION DEPENDENCY paragraph and §4.5's
  fielded-eleven bullet. No #44 owner sign-off is needed for this item; the only new ERR-044 is the §2.7
  bench mirror. A suspended player **can** be on the bench: the extremis back-fill (#30 §3.4,
  ERR-030-044..046) deliberately reinstates suspended players there. §2.7 makes a back-filled suspended
  bench player ineligible to enter, so under this rule the only suspended players in the participation
  set are back-filled starters, which is exactly #30 §3.4's residual, and a benched reinstatee's ban
  advances. The §2.7 mirror in #44 §2.3/§7.2 is a separate cross-file and stays as §2.7 states it.

### 2.6 Pre-kickoff choices

Team tactics chosen on Tactics Setup reach the engine as `PreKickoffConfig.TeamTactic` and are applied
at `Kickoff` through `ManagerAdaptation.ApplyKickoff`, the #26 FR-TP-004 boot path PM-1 uses.
`ApplyKickoff` itself stages the baseline with `SetTeamTactic` (through `TeamTacticConfigApplier`), so it
commits at the first AI stride (`MatchEngine.cs` ~l.3590–3597) exactly as a drained command would; there
is no "active before the first tick" difference. The real differences are two. The baseline is not a
command, so it stays out of the tick-stamped command log that T-IM-02 replays. And it goes through the one
call on which an AI-managed side's selected kickoff preset replaces that side's baseline
(`ApplyKickoff`, `ManagerMode.AI`), which matters for the opponent under Q1 (a). In-match tactic changes
stay commands, unchanged. The starting eleven is not editable (§2.1).

**Pre-kickoff substitutions are refused.** A substitution accepted at `CurrentTick == 0` executes at
once as a free stoppage (§1), which is a starting-XI change by another route and would also put an
outgoing starter who never played a tick into the boot occupants. The career-attached session's drain
(`MatchEngineMutations.RequestSubstitution`, §2.7's refusal site) refuses a request while
`CurrentTick == 0`, before the engine is called, and the driver records the refusal as it records any
typed refusal (Gate J J-02). It reuses the existing `SubstitutionRequestStatus.BenchPlayerUnavailable`;
a dedicated status would append an ordinal to the engine's enum and is not proposed.

### 2.7 Bench eligibility (the ERR-030-044..046 guarantee)

The availability filter does not keep every removed player off the bench. When a club cannot satisfy
selection (eleven position-matched starters plus the seven-man bench, the ERR-030-044 trigger),
`AvailabilityComposition` reinstates removed players, injured first (tier 1) and suspended second
(tier 2). Within the search bound the composed eleven contains the minimum achievable number of
reinstated-suspended players, and a ban stalls only in a probe-verified forced start or the beyond-cap
corner (#30 §3.4 residual (i)/(ii); #44 §2.3 and §7.2). `RequestSubstitution` checks no player
eligibility (§1). Without a rule here, a reinstated-suspended bench player brought on would join the
participation set, be exempted by ERR-044-003 stage 1, and have his ban stall in a fixture where the
search proved a clean completion. That is a route outside the residual #30 §3.4 and #44's mirrors state
as exhaustive.

**Rule for the suspended (option (a), proposed).** A player the back-fill reinstated onto the **bench**
at tier 2 (suspended, whether or not also injured) is ineligible to enter the match. Back-filled starters
are unaffected and play as they do today. Tier-1 (injured-only) bench reinstatees are a separate
question, Q3 (§3): every reason above concerns the suspended, and none is recorded for the injured.

**What (a) costs, and the August 17 decision.** #30 §3.4 records the owner's decision of August 17, 2026
to *keep the eighteen* and not build a short-bench posture. Under (a) the bench still has seven slots, but
a reinstated-suspended player on one cannot be used, so the club has fewer usable substitutes than bench
slots: functionally the short bench that decision declined to build, reached by eligibility instead of by
narrowing selection. It exists only while the back-fill fires, and that decision expects the generated
cover of ERR-044-003 stages 2–3 (not built) to retire the reinstatement path. The owner accepts it
knowingly (§3, §8) or chooses (b).

- **The set.** The compose half of `BootFixtureEngine` derives, per club, `IneligibleToEnter`: the
  reinstated-suspended players not in the starting eleven (tier-1 players too if Q3 answers
  "ineligible"). It is taken from `AvailabilityComposition.Compose`'s own reinstatement record, exposed
  as an output, not from a second walk (the parallel-surface rule). It is empty on the FR-DC-018 identity
  path, fixed at `Begin`, and carried on the preview and the handle. It covers both clubs. It can name
  players with no bench slot, since `Compose` can reinstate more players than the eighteen hold; `Attach`
  ignores ids it cannot map to a bench slot, and the season-side backstop is unaffected because a player
  outside the eighteen cannot be in the participation set.
- **Refusal site: `match-client-core`, at the drain.** `MatchSession.Attach` maps each id of the managed
  club's set to its bench index once, through `MatchBootRoster.BenchPlayerId`.
  `MatchEngineMutations.RequestSubstitution` refuses a request whose bench slot is in the set with the
  existing `SubstitutionRequestStatus.BenchPlayerUnavailable` before it calls the engine. The opponent's
  set needs no drain check, because the drain refuses any command for the opponent (§2.4). The driver
  records the refusal as it records any typed refusal (Gate J J-02). The substitution chooser greys the
  player out with a reason, beside the existing keeper-role greying
  (`S0ScreenPresenter.UpdateIncomingAvailability`); the greying is presentation, and the drain is the
  guarantee.
- **Why the drain and not the engine.** This keeps the layering as it is: `season-save` computes the set
  and already references `match-engine`; `match-client-core` receives plain ids and takes no
  `season-save` reference; `match-engine` is unchanged. An engine-side bench-ineligibility flag would be
  cross-tick engine state, so a `SNAPSHOT_SCHEMA_VERSION` bump and digest evidence inside the W8 B→C
  window (§7), for a rule only the career route needs. The drain sees every production substitution
  today, because it is the engine's only production substitution caller (the `SubstitutePlayer` census,
  `MatchEngine.cs` l.1766–1774). **Reopening trigger:** an engine-internal substitution caller (the
  census names AI-manager and injury substitutions) moves this check into the engine, because the drain
  would no longer see every substitution.
- **Season-side backstop.** `CompleteManagedFixture` fails loud (new F-row) if the participation set
  holds a player in `IneligibleToEnter`, so the #30 guarantee does not rest on the client alone.

**ERR-030 obligation (filed at application).** #30 §3.4's guarantee and its "tier order is not
cosmetic" paragraph speak only of the composed eleven. With substitutions possible they must also state
that a reinstated-suspended player on the bench cannot enter, citing this rule, so that residual (i)/(ii)
stays exhaustive. The same statement is mirrored at #44 §2.3 (the "Benched" bullet) and §7.2 as an
ERR-044 cross-file.

**#41 note.** Under Q3 (a) a tier-1 bench reinstatee who comes on is credited with an appearance like any
substitute; under Q3 (b) #41's appearance record never credits one. ERR-041 carries the Q3 answer.

**Option (b), considered and rejected.** Accept that the manager may bring a reinstated-suspended
player on, and add a third residual item to #30 §3.4 and to #44 §2.3/§7.2. Rejected because it widens
the residual that the ERR-030-044 → ERR-030-045 → ERR-030-046 chain was escalated to minimise, and it
makes a stalled ban a manager's choice rather than a forced compromise.

### 2.8 Per-tick observers

The streamer has one post-tick slot and PM-1's analytics already takes it (§1). The career route needs
two per-tick consumers: the season observation (`handle.ObserveTick`, which the match needs in order to
commit) and analytics (which feeds Post-Match). Attaching both throws. Wrapping both in one action
puts them behind the streamer's disarm-on-first-exception rule, so an analytics fault would silently
stop `ObserveTick` and make the match uncommittable, and a fold fault would freeze analytics.

**Owner: `MatchSession`, as a composite per-tick observer on career-attached sessions only.**
`MatchSession.Attach` (§2.4), and nothing else, installs one composite action into the streamer's slot
before it returns, and exposes ordered registration,
`AddTickConsumer(Func<MatchEngine, Action> factory, TickConsumerOrder order)`. A session built by
`MatchSession(MatchSetup)` or `RestoreFrom` (PM-1's coordinator, the web host, every existing test)
installs no composite and keeps today's single set-once slot and fault surface, unchanged.

- `First` takes at most one consumer and runs before every other consumer on each tick. The career route
  registers the season observation there. `After` consumers run next, in registration order.
  Registration is refused once `Start` or the first `TickOnce` has run. `AddTickConsumer` is refused,
  with a named exception, on a session not built by `Attach`.
- Each consumer runs in its own `try`/`catch`. A consumer that throws is disarmed alone, its fault is
  latched on a per-consumer surface (`MatchSession.TickConsumerFault(order)`, indicative), and the other
  consumers run on that tick and every later one. The composite never throws to the streamer, so on a
  career-attached session `InvokePostTickObserver`'s disarm-all path is not reached and
  `Streamer.PostTickObserverFault` is set only if the composite's own code fails. The catch is on the
  observation path, not the 60 Hz inner loop, the same carve-out `LiveMatchStreamer` already cites. The
  consumer list is fixed before playback, so the composite allocates nothing per tick.
- `AttachTickObserver` branches on the session kind. On a career-attached session it registers an
  `After` consumer, so `SessionMatchAnalytics.Attach` registers through it unchanged. On any other
  session it forwards to `SetPostTickObserver` exactly as today: set-once, disarm-all on the first
  exception, the cause on `Streamer.PostTickObserverFault`. `SessionMatchAnalytics.Attach`'s "attaches
  exactly once … later per-tick consumers must compose here" remark is rewritten to name both forms.
  **No existing test is rewritten.** `ClientMatchCoordinatorTests.cs:94` (a second attach throws),
  `MatchClientHostTests.cs:181–190` (set-once; a null factory is refused),
  `SessionMatchAnalyticsTests.cs:68` (a failing adapter latches `PostTickObserverFault`) and
  `S0DemoFixtureComparisonTests.cs:95` (no fault after a clean match) all run on `MatchSession(MatchSetup)`
  sessions and keep their contract; `MatchClientHostTests.cs:128–136` drives a bare `LiveMatchStreamer`
  and is outside `MatchSession` altogether. They are T-IM-11's PM-1 regression half.
- **Season-consumer fault:** that consumer is disarmed and the handle has latched the exception itself
  (`IsObservationFaulted`, §2.4). The client polls `IsObservationFaulted` every frame (and
  `LiveMatchStreamer.PostTickObserverFault`, which on this session stays null unless the composite
  itself fails), pauses playback at once, shows the reason and offers only abandon; it does not wait for
  full time. `CompleteManagedFixture` refuses a faulted handle with the round left Live and Abandon
  the only exit (§2.3), and the observation-gap check (§2.4) catches the same fault if the client missed
  it.
- **Analytics fault:** the season observation continues and the match commits. The fault signal is not
  lost. `SessionMatchAnalytics.ObserveTick` latches its own `_incomplete` before it rethrows
  (`SessionMatchAnalytics.cs` l.69–70), so `Publish().IsIncomplete` reads true on the live screen and in
  Post-Match, whose report reads that publication (`ClientMatchContext.cs:63`), exactly as on PM-1; and
  the composite latches the exception at `TickConsumerFault(After)`. The one PM-1 reader that keys on
  the streamer's fault for this, `S0ScreenPresenter.cs:504`, would see null on a career-attached
  session and show the incomplete notice up to one simulated second late, at its next periodic refresh.
  The career route's match screen therefore reads `TickConsumerFault(After)` (with
  `Streamer.PostTickObserverFault` for a composite failure) at that condition. Whether it does so by
  a session-kind branch in the shared presenter or in its own presenter is the client landing's choice;
  PM-1's reading is unchanged either way. `MatchClientHost.AnalyticsFault` (`MatchClientHost.cs:126`)
  belongs to the web host, which builds its session from a `MatchSetup` and never wraps a career engine.

**Why career-attached only (option (a)), not a composite on every session (option (b)).** Option (b)
installs the composite on every session and latches the first `After` fault onto a session-level
surface that both production readers migrate to. It works, but it changes the fault surface of PM-1
and the web host, which no owner decision touches: `MatchClientHost.AnalyticsFault` and
`S0ScreenPresenter.cs:504` migrate; `ClientMatchCoordinatorTests.cs:94`, `MatchClientHostTests.cs:181–190`
and `SessionMatchAnalyticsTests.cs:68` are rewritten; and `S0DemoFixtureComparisonTests.cs:95`'s null
check would pass vacuously unless pointed at the new surface. v0.4 installed the composite on every
session without that migration, which would have left `PostTickObserverFault` null for good on PM-1
and the web host: the web host's statistics, whose aggregator has no other fault latch, would freeze
with no fault shown, and PM-1's immediate fault refresh would be dead. Option (a) leaves those paths,
their two readers and their tests as they are, and confines the composite to the one session kind that
needs two consumers. Its cost is a session-kind branch in `AttachTickObserver` and a career screen that
reads its analytics fault from the session rather than from the streamer.

**Why `MatchSession` and not `SessionMatchAnalytics.Attach`.** `MatchSession` already owns the streamer
and the observer seam, and fault isolation between consumers is session plumbing. Composing inside
`SessionMatchAnalytics.Attach` would make an analytics adapter own the season commit's ordering and fault
isolation. That adapter's own fault posture (latch `_incomplete` and rethrow) is the opposite of what
the season consumer needs, and every later consumer would also have to compose inside analytics.

### 2.9 Thread contract and quiescence

`MatchEnded` reads true from inside the final tick, before that tick's post-tick observer has run, and
the pacing thread stays alive after full time (§1). The season thread can therefore see `MatchEnded`
while the tick thread is still inside the final `fold.ObserveTick`, which appends to the fold's
`_pending` list that `Commit` then iterates, and a second engine booted meanwhile would reset the
process-static bus under it.

- **Rule.** `CompleteManagedFixture` and `AbandonManagedFixture` run on the season thread, after
  `MatchSession.Stop()` has returned (it joins the pacing thread), or, for a head-less `TickOnce`
  session, with no tick in flight. `MatchEnded` is read for `Complete` **after** that join; a read
  before it is a racing read and is not used (§2.3). `season-save` does not reference
  `match-client-core`, so it cannot verify the join. **The client enforces it in one place:**
  `CareerSession.CompleteMatchday(session)` and `AbandonMatchday(session)` take the `MatchSession`, call
  `Stop()`, and only then call the season seam, so no screen carries the duty (T-IM-12 (b)).
- **What the seam enforces itself.** One lock on the handle serialises `ObserveTick`, `Complete` and
  `Abandon`; the fault read (`IsObservationFaulted`, `ObservationFault`) is a lock-free volatile read, so
  the client's per-frame poll never waits on the tick thread. After `Complete` or `Abandon` the handle is
  **invalidated**: every member but the two fault reads throws a named F-row, so a late `ObserveTick`
  cannot write into a committed or discarded fold. The §2.8 composite catches that throw on the tick
  thread, so it cannot reach the streamer. The observation count (§2.4) is the backstop for a `Complete`
  issued before the final observation lands: `ObserveTick` takes the lock only after its tick has run, so
  `Complete` refuses on the count mismatch instead of blocking. It is not the guarantee, the join is.
- **One engine in flight.** From `Kickoff` until `Complete`/`Abandon` returns, no other engine is booted
  or ticked in the process: the round state allows one handle, the interactive route refuses
  `FullEngine` so `Complete` boots none, and the client runs no PM-1 session while a round is not Closed.

### 2.10 Save enforcement and the `CareerSession` operations

- **`SeasonSaveManager.Save(SeasonLoop, …)` refuses** (new F-row) unless `RoundState` is Closed,
  whatever `matchOrNull` holds. That overload is the one `CareerSaveSlots.Write` uses (l.217). The
  long-form overload takes the loop's parts, not the loop, cannot see round state, and is not a
  career-route entry. A career-attached `MatchSession` refuses `CaptureSave`, so the match-blob path is
  closed too. `TakeEngineForSession()` (§2.4) leaves the season side with no standing handle on the
  engine; the session's tick-consumer factories receive the engine and `MatchEngineMutations` is a public
  type over it, so the client can still reach it, as PM-1 does.
- **`CareerProgress.NextAction` gains two values.** `PlayManagedMatchday` is returned on a fixture day
  when `CanBeginManagedFixture` (§2.4), and `PlayMatchday` only when it is not (a bye round,
  `QuickSimAll`, `FullEngine`, no owned provider). `MatchdayInProgress` is returned whenever `RoundState`
  is not Closed. Neither is executable through `Execute`: each is refused before its `switch` with a named
  error (today a value that passed the legality check but has no case would throw "Unreachable"), and
  `PlayManagedMatchday`'s names the Tactics Setup move. `SeasonCommandDispatcher` refuses `AdvanceRound` on
  a managed fixture day with the same error: it cannot otherwise tell "play my match" from "sim the
  round", which is what let `PlayMatchday` run a managed match headlessly (§1). `CareerHomeSource` sets
  `IsProgressBlocked` from `NextAction == MatchdayInProgress` as well as `IsBusy` (it passes only
  `IsBusy` today, false between operations), so the control is blocked while a round is open.
  `CareerSession.SaveTo`, the manual save, refuses with a named error while `MatchdayInProgress`; Career
  Home offers it only when Closed.
- **`CareerSession` operations** (names indicative), each taking `BeginOperation` and bumping
  `Revision` as `Execute` does: `BeginMatchday()` → `BeginManagedFixture`; `StartMatch(config)` →
  `Kickoff`, returning the handle; `CompleteMatchday(session)` → stops the session, then
  `CompleteManagedFixture`, then builds `CareerChange.RoundPlayed(...)` as `Execute(PlayMatchday)` does
  and calls `Autosave()`; `AbandonMatchday(session)` → stops the session, then `AbandonManagedFixture`.
  The D-P7-2 autosave therefore follows `Complete`, on the season thread, as it follows the synchronous
  round. `LastAutosaveError` behaves as today.
- **Post-abandon Closed.** A save taken after an abandon is a fixture day with the steps run and the
  round unplayed (§2.3). Loading it gives `NextAction == PlayManagedMatchday` (or `PlayMatchday` where
  `CanBeginManagedFixture` is false), and `Begin` repeats the steps as no-ops. T-IM-15 locks that this state commits the same
  round as an uninterrupted run and that the mutators §2.3 names are refused in it.

### 2.11 Client routes and intent routing

Route table (screen-move names indicative; the client owner confirms them in the client design
supplement at application). Each row is a **new named move**, never a reuse of `ReturnToMainMenu` or
PM-1's `CancelTacticsSetup` (ux-experience-architecture §3.1). The career root sits exactly one screen
above Main Menu (client design §5-P7), and the Tactics-cancel junction that ux §3 leaves open is a named
move returning to Career Home.

| From → to | Move | Season seam | Round state after |
|---|---|---|---|
| Career Home → Tactics Setup | play the match (`PlayManagedMatchday`) | `BeginManagedFixture` (via `BeginMatchday`) | Open |
| Tactics Setup → Match | start match | `Kickoff(config)` (via `StartMatch`) | Live |
| Tactics Setup → Career Home | cancel | `AbandonManagedFixture` from Open | Closed |
| Match → Career Home | quit (confirmed) | `AbandonMatchday(session)`: stop the session, then `AbandonManagedFixture` from Live | Closed |
| Match → Post-Match | full time | `CompleteMatchday(session)`: stop the session, then `CompleteManagedFixture`; the autosave follows | Closed |
| Post-Match → Career Home | return | none | Closed |
| any → Main Menu / Load (Faulted only) | leave | none | (loop discarded) |
| Tactics Setup → Match, quick-resolve | per Q2 | per Q2 | per Q2 |

**D-P7-3 restated for the interactive route.** The client still offers exactly one progress action, now
derived from `NextAction`:

| State | Offered | Seam |
|---|---|---|
| clock before the pending fixture day | `AdvanceDay` | `AdvanceDays(1)` |
| fixture day, round Closed, `CanBeginManagedFixture` | `PlayManagedMatchday` → Tactics Setup | `BeginManagedFixture`, not `Execute` |
| fixture day, **no managed fixture** (the bye round of an odd-N league, FR-SN-004) or a mode/provider case `BeginManagedFixture` refuses | `PlayMatchday`, no match screen | `Execute(PlayMatchday)` → synchronous `AdvanceAndPlayNextRound()`: a quick-simmed or all-quick-sim round, as today |
| round Open, Live or Faulted | `MatchdayInProgress` (no action) | — |
| season complete, final fixture day not yet ticked | `AdvanceDay` | `AdvanceDays(1)` |
| season complete, final fixture day ticked | `StartNextSeason` | `RollToNextSeason()` |

On a managed fixture day `PlayMatchday` and `AdvanceRound` are refused (§2.10): until Q2 is answered no
Career Home route runs the managed fixture without Tactics Setup.

**Unemployed manager.** FR-SN-013b (ERR-030-021) makes `ManagedClubId` optional so that a manager
between jobs is representable; when absent every fixture is quick-simmed and no engine runs. That is
spec text only: `SeasonState.ManagedClubId` is a plain `int` today and the representation lands at #54
T2. When it does, an unemployed round has no managed fixture and takes the `PlayMatchday` row, so this
contract needs no further rule for it.

**#38 Appendix C.** FR-UI-003 routes UI-originated sim mutation through an existing public seam, and
Begin, Kickoff, Complete and Abandon will be such seams once they exist. **The rows land with the code,
not before** (§8): Appendix C's column is "Public seam (verified)", so rows naming seams and `IntentKind`
ordinals that do not exist yet would be unverified claims, and ERR-038-005 landed its rows and its
`IntentKind` appends in one commit. The entry then adds, in the form ERR-038-005 used, season intent
kinds appended (ordinals after 6, names indicative): `BeginManagedMatch` → `CareerSession.BeginMatchday()`
→ `BeginManagedFixture()`, `StartManagedMatch` → `Kickoff(config)` (payload: the `TeamTactic`),
`CompleteManagedMatch` → `CompleteManagedFixture()` and `AbandonManagedMatch` →
`AbandonManagedFixture()`; the last two carry no payload and the dispatcher supplies the lifecycle's
current session (§2.9). `AdvanceRound` keeps its row for rounds with no managed fixture and is refused on
a managed fixture day (§2.10). `IntentRouting` classifies the new kinds as season intents, so the KD-U2
drift guard is narrowed by classification, not weakened. The same entry corrects the `Substitute` row,
which names `MatchEngine.SubstitutePlayer`: that method is `internal` (`MatchEngine.cs` l.1775), and the
production path is `RequestSubstitution` through `MatchEngineMutations`.

## 3. Open questions

Three product questions are open (Q1–Q3). Separately, the §2.5 appearance rule needs the #41 owner's
acceptance (the #44 half is conformance and needs none), and the §2.7 suspended-bench rule needs the #30,
#44 and #41 owners' acceptance and the owner's, because it is a functionally short bench (§2.7).

**Q1 — Does the career opponent have an AI manager in PM-2?** Today `BootFixtureEngine` configures no
manager, so the opponent is inert: `ManagerMode.Human`, Balanced all match, no kickoff selection, no
adaptation (§1). PM-1's demo opponent is AI-managed.

- **(a) AI manager.** The configure half of `BootFixtureEngine` calls
  `ConfigureManager(opponentTeam, ManagerMode.AI, archetype)` before `ApplyKickoff`, on both the headless
  `ManagedThroughEngine` path and the interactive path, as `MatchSession.BootEngine` does for PM-1's AI
  side. The managed club stays `ManagerMode.Human` on both paths, so T-IM-01 parity holds. As drafted,
  this covers the managed fixture only: `FullEngine` mode's non-managed engine fixtures keep today's inert
  configuration unless the owner's answer extends it. Consequences:
  - The headless managed fixture's digest changes. The landing carries digest-change evidence on the
    existing headless `ManagedThroughEngine` fixtures (results, cards, appearance records, post-round
    save bytes) instead of claiming neutrality.
  - The non-managed round-resolution model was fitted to engine matches. That corpus
    (`league-bootstrap-design.md` KD-7/KD-7a/KD-8) names no manager configuration, so the managed fixture
    would be played under a configuration the quick-sim fit may not reflect. Whether the capture must
    be repeated with an AI-managed side is a D-08 scope item to record; this draft changes neither the
    model nor its calibration.
  - Archetype sub-choice: a fixed ordinal (PM-1 passes 0) or a deterministic per-club derivation. The
    career has no per-club manager identity today.
- **(b) Inert opponent for PM-2.** Keep `ManagerMode.Human` and Balanced. The headless digest is **not
  yet verified** unchanged: `ApplyKickoff` still stages identity team and player tactics for a
  Human/Human pair, and `BootFixtureEngine` calls neither applier today, so whether staging them leaves
  the digest unchanged is measured at the landing (T-IM-16 gives the fallback). This is a stated product
  limitation: the career opponent never changes its tactic during a match, and W7 kickoff adaptation
  stays dead on the career path.

**Recommendation:** (a), with archetype 0, so the career opponent behaves as the PM-1 opponent the owner
has already played. This is not decided here. Until Q1 is answered, §2.4's `Kickoff` and T-IM-10 are
written to apply either way.

**Q2 — Is there a quick-resolve option for the managed fixture?** The PM-2 roadmap criterion reads
"play (or quick-resolve) each round" (roadmap §1 PM-2; §5 step table, row 11). D-11 decided that managed
fixtures are played interactively and rejected an instant finish **on quit**. It did not say whether the
player may choose, before kickoff, to resolve the managed fixture without watching. That is a different
feature from the rejected one: the player asks for it, the result is committed through the normal path,
and leaving a match is not what triggers it. Today `PlayMatchday` on Career Home does it: `Execute` runs
`AdvanceAndPlayNextRound()` headlessly at default tactics, with no Tactics Setup (§1). **Under this draft
`PlayMatchday` and `AdvanceRound` no longer run a managed fixture** (§2.10), so until Q2 is answered no
Career Home route resolves it without watching.

- **(a) No quick-resolve for the managed fixture.** Career Home's matchday action goes to Tactics Setup
  and the match. The synchronous call stays for rounds with no managed fixture and for tests. Playback
  speed is the only way to shorten a match, and the roadmap's "(or quick-resolve)" is amended or read as
  the other clubs' fixtures.
- **(b) Quick-resolve from Tactics Setup.** After choosing a tactic the player picks Play or
  Quick-resolve. Quick-resolve is `Kickoff(config)` + the head-less tick loop with `ObserveTick` +
  `CompleteManagedFixture`, the sequence the synchronous call becomes (§2.2): no match screen, no
  mid-match input, about two minutes of compute off the UI thread (roadmap C1; client design §5-P7), the
  result committed and Post-Match shown as usual. No new season seam; the managed tactic still applies.
- **(c) Keep today's one-click `PlayMatchday`** for the managed fixture on Career Home beside Play. Two
  routes to the same engine run, `NextAction` would have to offer two actions against D-P7-3's one, and
  the shortcut ignores any tactic the player wanted.
- **(d) Round-model resolution of the managed fixture.** Instant, no engine, no tactic: the managed
  fixture resolves through `RoundResolutionModel` like the others, which is what the code's
  `RoundResolutionMode.QuickSimAll` doc calls the "quick-resolve the round" path. It needs a new season
  seam (the mode is fixed at construction, so today the whole loop, not one round, carries it), produces
  no cards (the quick-sim synthesizes none, #44 §1.1/§7.2) and gives up the player's tactic.

**Asymmetry with Q1 (a).** A headless engine run of the managed fixture, (b) and (c), pits a managed side
that never adapts (`ManagerMode.Human`, no commands) against an AI opponent that does, so quick-resolve
is systematically worse for the player than the same match watched with input. Under Q1 (b) both sides
are inert and the asymmetry does not arise; (d) has none but no engine either.

**Recommendation:** (b), with the asymmetry above shown to the owner. It meets the roadmap's quick-resolve
wording, applies the tactic the player chose, needs no seam of its own, and T-IM-01 already proves it
equals the interactive no-input run. (a) is the fallback if the owner wants watched matches only; (d) if
the asymmetry is not acceptable. This is not decided here. Until Q2 is answered, the §2.11 route table
carries the quick-resolve row as "per Q2" and nothing else depends on it.

**Q3 — May a tier-1 (injured-only) bench reinstatee enter the match?** For the #41 owner (§2.7). A tier-1
player the back-fill puts in the eleven plays today; one on the bench has no rule yet.

- **(a) Eligible, like a starter.** `IneligibleToEnter` holds the suspended only, and #41's appearance
  record credits an injured substitute like any other. The functionally short bench of §2.7 stays limited
  to the suspended.
- **(b) Ineligible.** Needs a stated #41 reason, which this draft does not have. It widens the short
  bench to tier 1 and is inconsistent with the starter case unless the reason distinguishes them.

**Recommendation:** (a): no recorded reason makes a tier-1 player fit to start and unfit to come on, and
(b) shrinks the usable bench further against the August 17 decision. This is not decided here; ERR-041
carries the answer.

## 4. Normative edits proposed for #30 (applied only after approval)

**One ERR-030 entry**, allocated at application against that head, carries every #30 edit in this
section. The #44, #41, #26, #21, #38 and client-design items are cross-file consequences, each filed as
its own entry. Each site below names the replacement it gets. Section numbers refer to
`docs/specs/season-competition-loop/`.

1. **`section-1.md` KD-9 and `section-2.md` FR-SN-012.** "A round may span `BeginManagedFixture` …
   `CompleteManagedFixture` (or end at `AbandonManagedFixture`); no fixture of the round is applied
   before `Complete`, and `Complete` applies all of them."
2. **`section-1.md` KD-7 and `section-2.md` FR-SN-032 (command API enumerations).** Add
   `BeginManagedFixture`, `Kickoff`, `CompleteManagedFixture`, `AbandonManagedFixture`, and the existing commands the lists omit
   (KD-7 omits `AdvanceDays`; both omit `ApplyTransaction` and `SubmitTransferBid`). Add: "While a round is Open, Live,
   or Faulted every other mutating command is refused."
3. **`section-2.md` §2.2 data structures.** In the `SeasonLoop` bullet, replace "holds … the
   active-or-null `MatchEngine` (the managed club's in-progress fixture)" with "holds a round state and,
   from `Kickoff` to `Complete`/`Abandon`, a `ManagedFixtureHandle` that owns the managed engine, the
   fold and the `ParticipationRecord`; `ActiveMatch` remains the synchronous path's accessor". Extend
   the command-API list at the end of that bullet as in item 2. Add `RoundState`, `ManagedFixturePreview`,
   `ManagedFixtureHandle`, `PreKickoffConfig`, `ParticipationRecord`.
4. **`section-2.md` §2.3 failure modes.** New rows: a mutating command, `Save` or `TrySetFocus` while
   not Closed; `ApplyTransaction`, `SubmitTransferBid` or `TrySetFocus` after an abandon, until the round
   is applied (§2.3); `Begin` outside Closed, in `QuickSimAll`/`FullEngine`, with no managed fixture or no
   owned provider, or for a round with a club in two fixtures; `Kickoff` outside Open; an observation gap
   or fault; `Complete` before full time; `Complete` with an ineligible-to-enter player in the
   participation set; `Complete` or `Abandon` with no open round, `Abandon` in Faulted, any handle member
   after invalidation; a Faulted round (§2.4.1).
5. **`section-2.md` FR-SN-013 and new FR-SN-035..** (numbers allocated at application). FR-SN-013 gains:
   "The managed fixture's appearance record and ban-serving input are its participation set (§2.5),
   not its starting eleven." New rows: round states and refusals (§2.3); `BeginRound`/`CompleteRound` and
   the seams (§2.2, §2.4); apply-only `Complete` and Faulted (§2.4.1); the participation record (§2.5);
   bench eligibility and its backstop (§2.7); thread contract (§2.9); save refusal (§2.10).
6. **`section-2.md` FR-SN-021 and `section-1.md` KD-1.** "A career save is never taken while a round is
   not Closed; `SeasonSaveManager.Save(SeasonLoop, …)` refuses. The frame's `matchPresent` slot stays as
   specified but the career route never writes it." **No `SEASON_SAVE_FORMAT_VERSION` change.**
7. **`section-3.md` §3.3.2.** Replace "the fixture day's own slots 0–11 run at the top of
   `AdvanceAndPlayNextRound`" and "`AdvanceAndPlayNextRound` runs the slots only after every one of its
   guards" with the same statements about `BeginRound`, which both `AdvanceAndPlayNextRound` and
   `BeginManagedFixture` call; add "`AbandonManagedFixture` leaves them run, and the next `Begin`
   repeats them as no-ops; until the round is applied `ApplyTransaction`, `SubmitTransferBid` and
   `TrySetFocus` are refused, so nothing runs between the steps and the next `Begin`" (ERR-030-027's
   ordering).
8. **`section-3.md` §3.4 pseudocode and the FR-SN-025 paragraph.** The `AdvanceAndPlayNextRound` block
   becomes `BeginRound` and `CompleteRound`; `PlayThroughEngine` becomes the compose/configure halves
   plus the per-tick `ObserveTick`. The paragraph "`AdvanceAndPlayNextRound` is invoked *from* the
   world-tick loop, so the two clocks stay disjoint (FR-SN-025)" becomes: "The match runs on the 10 Hz /
   60 Hz loops on the tick thread between `BeginManagedFixture` and `Complete`. No world-day command is
   accepted in that interval (§2.3), so the clocks stay disjoint by refusal, not by call nesting." The
   M17/ERR-030-039 paragraph (what M6's ordering costs) gains: the cost stated there is the synchronous
   call's and is unchanged (it does not use `RoundState`); the interactive route adds Faulted (§2.4.1)
   for the same residual; F6/F9 resolution now fails at `BeginRound`, before any fixture is applied
   (§2.2).
9. **`section-3.md` §3.4, depleted-squad guarantee and "tier order is not cosmetic"** (ERR-030, §2.7):
   extended to the bench — "a back-filled bench player cannot enter, so residual (i)/(ii) stays
   exhaustive". #44 §2.3/§7.2 mirror it (ERR-044 cross-file).
10. **`section-3.md` §3.4.1.** "both output the fielded XIs and the UNFILTERED roster ids" gains "(the
    managed fixture outputs its participation set)"; the determinism note gains "and the managed
    engine's boot time, before or after earlier-indexed fixtures are applied, does not change any
    result", and a note that the round plan reads every club's squad once at `Begin`, which is exact
    because each club plays at most once per round (FR-SN-003).
11. **`section-4.md` §4.3.** The `_activeMatch` bullet gains "the handle is the interactive owner;
    `_activeMatch` remains the synchronous-path accessor". The command-API paragraph gains the new
    seams and the `DisciplineRules` caller contract (§2.3). The hot-path sentence "It is **not** on the
    60 Hz hot path … allocation / `new` / exceptions are permitted" becomes: "…except
    `ManagedFixtureHandle.ObserveTick`, which runs once per engine tick on the tick thread. It MUST NOT
    allocate on a tick that carries no card and no substitution, and takes no lock but the handle's
    own; its `catch` (§2.4) is on the observation path, not the simulation inner loop." (The fold's loop
    reads the tap and appends to `_pending` only on a card; whether it allocates nothing on an empty
    tick is measured by T-IM-17, not assumed.)
12. **`appendices.md` Appendix B (the paragraph describing the appearance block) and B.1.** "the
    per-player fielded-XI record" becomes "the per-player participation record: the starting eleven and
    every player who came on"; B.1's `recentBits` comment "fielded on anchor − k" becomes "took part on
    anchor − k". **Byte layout unchanged.**
13. **`section-5.md` new §5.9 and §5.8.** §5.9 holds the locks marked **[#30]** in §5 below as
    `T-SN-IM-0nn` under the same numbers (`T-IM-nn` is `T-SN-IM-0nn`; the [client] numbers stay unused there). They are the season-side locks, which `season-save`
    tests can reach, and the cross-assembly integration locks, which stay in #30 because their subject is
    a #30 contract and are labelled *hosted in `TacticalDirector.ClientApp.Tests`*, the one test assembly
    that references both `SeasonSave` and `MatchClientCore`. Locks marked **[client]** have client-only
    subjects (the composite, drain refusals, `CareerSession`, the route and dispatcher): #30 §5.9 does not
    own them. They belong in the client design supplement's test plan, which this draft does not edit and
    the application names. §5.8's "Every FR-SN-001..034 + 013a/013b maps to at least one test above"
    extends to the new FR rows, each traced to a **[#30]** lock. The §9 approval checklist's FR and test
    counts are refreshed in the application sweep.
14. **`section-2.md` FR-SN-025 and `section-1.md` §1.2.** Both say the loop MUST run on the world tick and
    never on the 10 Hz / 60 Hz match loops, without exception; `ManagedFixtureHandle.ObserveTick` is
    #30-owned code that runs once per engine tick on the tick thread. Add: "except
    `ManagedFixtureHandle.ObserveTick`, which the client's tick consumer calls on the tick thread; it
    mutates no season state until `CompleteManagedFixture` (§2.9), and every world-day command is refused
    while a round is open (§2.3)". §1.5 row 3 ("orchestrates via `MatchEngine`/`MatchSaveManager`") gains
    "the interactive fixture's tick loop is driven by the client".
15. **`section-2.md` FR-SN-013b.** It names one influence path, `SetTeamTactic`. Becomes: "under the
    human's tactical influence: the pre-kickoff baseline (`ManagerAdaptation.ApplyKickoff`, #26
    FR-TP-004) and live `SetTeamTactic` commands (#21)"; "the rest through the round-resolution model"
    is unchanged.
16. **`section-4.md` §4.1 and §4.2.** §4.1's reference list gains `TacticalDirector.TacticalInstructions`
    (for `TeamTactic` and `ITacticPresetCatalogue`) beside `MatchEngine`, `LivingWorld`,
    `DeterministicSim` and `PlayerDatabase`. §4.2's file layout gains `ManagedFixtureHandle.cs`,
    `ManagedFixturePreview.cs`, `RoundState.cs`, `PreKickoffConfig.cs`, `ParticipationRecord.cs` and the
    round-plan type, and its `season-save-tests.asmdef` line gains `TacticalInstructions` (asmdef
    references are not transitive in Unity, that line's own remark).
17. **`section-7.md` §7.1 and §7.3, and `outline.md`.** §7.1 gains a T-phase entry for the interactive
    managed fixture (no format change: `SEASON_SAVE_FORMAT_VERSION` stays 8); §7.3 gains a seam row for
    #38 and the client (`BeginManagedFixture`, `Kickoff`, `CompleteManagedFixture`,
    `AbandonManagedFixture`); `outline.md`'s section-2 line "FR-SN-001..034" becomes the new FR range.

Cross-file consequences (each its own entry at application):

- **#44 (conformance, §2.5):** at the code landing, updates the existing ERR-044-010 row (closed, not a
  new entry) and refreshes FR-DC-011, §3.3's SUBSTITUTION DEPENDENCY paragraph and §4.5's fielded-eleven
  bullet. The only new ERR-044 is the §2.7 bench mirror at §2.3/§7.2, whose rule text lands with approval.
- **#41:** FR-MD-010's "fielded" is "took part" (§2.5); ERR-041 carries the Q3 answer (§2.7).
- **#26 (Tactical Presets & AI-Manager Selection), affected.** The configure half becomes a second
  production caller of the FR-TP-004/010 kickoff boot path (`ManagerAdaptation.ApplyKickoff`), beside
  `MatchSession.BootEngine`; under Q1 (a) it is also a caller of `ConfigureManager` (FR-TP-007) for the
  career opponent. No FR-TP rule changes. Whether #26 records the career caller in its own text or only
  #30 cites it is settled at application.
- **#21 (Tactical Instructions), affected.** The Tactics Setup tactic is a pre-kickoff boot baseline,
  not an in-match change, so FR-TI-027's stride-boundary rule governs only in-match commands, as it
  does today. No #21 edit is expected; it is named so the application checks it.
- **#38 Appendix C:** the rows of §2.11, the `IntentKind` appends and the `Substitute` row correction,
  under one ERR-038 entry, at the code landing (§8).
- **Client design supplement (`interactive-unity-client-design.md` §5-P7):** the D-P7-3 table and the
  routes of §2.11, the `CareerSession` operations, `PlayManagedMatchday` and `MatchdayInProgress` of
  §2.10, and the **[client]** locks of §5 as its test plan.

## 5. Locks (proposed §5 additions)

Working labels `T-IM-nn` map one to one to `T-SN-IM-0nn` for the locks marked **[#30]** (§4 item 13).
Locks marked **[client]** have client-only subjects and go to the client design supplement's test plan,
not to #30 §5.9. Every lock is mirrored for the managed club at home and away (its team index differs,
§2.4). **Host** says where it runs: a lock that needs both `SeasonLoop` and `MatchSession` lives in
`TacticalDirector.ClientApp.Tests`, which references both (`season-save` tests cannot reach
`match-client-core`). Three access changes let each host see its subject (§7): `season-save` grants
`InternalsVisibleTo(TacticalDirector.ClientApp.Tests)`, `season-save-tests.asmdef` gains
`TacticalInstructions`, and the synchronous path gains an `internal` end seam (T-IM-03). **Cost** says
whether it plays a real 90-minute match (about two minutes each, roadmap C1) or runs on a fresh or
hand-ended engine, using the existing `MatchEngine.TestOnly_CheckMatchFlowTransitions(MATCH_TICKS_TOTAL)`
end seam (internal, visible to both test assemblies). The match counts in the table are **per mirror**;
§7 totals them.

| ID | Host / cost | Lock |
|---|---|---|
| T-IM-01 | **[#30]** hosted in ClientApp.Tests; 2 real matches | **No-input parity.** "No input" means `Kickoff(PreKickoffConfig.Default)` and no in-match manager commands. Same seed, rosters and configuration (including the opponent's manager configuration per Q1): an interactive managed match driven through `MatchSession.Attach` + `TickOnce` equals the headless `ManagedThroughEngine` round, which boots through the same configure half with the same default. Compared: final engine digest, every `MatchResult`, committed card tally, appearance records, bans, **the full save bytes** (`SeasonSaveManager.Save` to a temp file, byte-compared), `MatchOutcomes.Count` and `EnginePlayedFixtures`. `SeasonLoop.Snapshot()` alone is not enough: it encodes `SeasonState` only (`SeasonLoop.cs` l.1599) and none of the career blocks or counters. |
| T-IM-02 | **[#30]** hosted in ClientApp.Tests; 2 real matches | **Input replay.** Same initial state, the same `PreKickoffConfig` and the same tick-stamped command log give the same final digest and the same committed state, compared as in T-IM-01. |
| T-IM-03 | **[#30]** SeasonSave.Tests; hand-ended engine | **Season-commit equivalence.** Given the same managed result and participation set, `CompleteManagedFixture` and the synchronous path produce identical full save bytes, `MatchOutcomes.Count` and `EnginePlayedFixtures`. The synchronous side is ended through the `internal` end seam on its tick-to-full-time loop (§7), which ends the engine with `TestOnly_CheckMatchFlowTransitions` instead of ticking, because the real loop plays a full match. Both sides are new code, so this lock alone proves nothing about change from today; T-IM-16 anchors that. |
| T-IM-04 | **[#30]** SeasonSave.Tests (Open, and Live on raw `RunTick`); hosted in ClientApp.Tests (Live with a substitution through `MatchSession`, and Live at full time on the end seam) | **Abandon.** After `Abandon` from Open (plan and preview only, no engine constructed) or from Live (including after a substitution and a card, and at full time): no result, card, appearance, ban or table change is applied; the loop's full save bytes, `MatchOutcomes.Count` and `EnginePlayedFixtures` equal those of a control loop that ran `Begin` then `Abandon` and nothing else; a new `Begin` gives the same preview, and `Kickoff` with the same config and the same command log gives the same engine digest **after N ticks** (an equal initial digest is a weak check); a full replay then commits normally. |
| T-IM-05 | **[#30]** SeasonSave.Tests; hand-ended engine | **Exactly once.** `Complete` before full time is refused; a second `Complete` is refused; no card or ban is applied twice. |
| T-IM-06 | **[#30]** SeasonSave.Tests; no match | **Command refusal.** Every operation in §2.3's enumerated list is attempted while Open, Live and Faulted and refused with its named failure mode; after an `Abandon` the loop's full save bytes equal the control twin of T-IM-04. `Kickoff` outside Open is refused. A reflection check lists the public instance methods of `SeasonLoop` and `PlayerCareerStates` and fails if one is on neither the refused list nor a read-only allowlist, so a new public mutator cannot slip past the rule. `DisciplineRules` writers are outside the lock (§2.3). |
| T-IM-07 | **[#30]** SeasonSave.Tests; the last-tick case hosted in ClientApp.Tests (one real match) | **Observation gap and fault.** (a) Ticking the engine without `ObserveTick` makes `Complete` fail loud. (b) The same for a gap at the **first** tick and at the **last** tick, which the fold's own check cannot see (§1). (c) A fault injected into the season consumer at tick N: `IsObservationFaulted` reads true before tick N+1, `Complete` is refused with the round left Live, `Abandon` succeeds, and the state equals the control twin of T-IM-04. |
| T-IM-08 | (a) **[#30]** hosted in ClientApp.Tests; (b) **[client]** MatchClientCore.Tests; no full match | **Participation and drain.** (a) With a substitution after kickoff, the substituted-on player gets an appearance and counts as fielded for #44, and a player substituted off or sent off stays in the set; with none, the set equals the starting eleven. The record exists with a career wired and discipline unwired (fold null). (b) On a career-attached session, a `Substitute` at `CurrentTick == 0`, and a `SetTeamTactic`, `SetPlayerTactic` or `Substitute` for the opponent's team, are each refused at the drain and logged in `FailedCommands`, and leave the engine digest equal to the no-command run. |
| T-IM-09 | (a) **[client]** MatchClientCore.Tests, hand-built engine and id set; (b) **[#30]** SeasonSave.Tests; no match | **Bench eligibility.** (a) On a career-attached session over a hand-built engine and a hand-built `IneligibleToEnter` set naming two of the managed club's bench players (no `season-save` types: the `AvailabilityCompositionExtremisTests` fixtures are `internal` to SeasonSave.Tests): a `Substitute` command naming either is refused at the drain with `BenchPlayerUnavailable`, is logged, and leaves the engine digest equal to the no-command run; an id with no bench slot is ignored. (b) On a managed club whose composed bench holds a reinstated-suspended and a reinstated-injured player (built as in `AvailabilityCompositionExtremisTests`): after `Complete` the suspended player's ban has advanced, and `Complete` fails loud when the participation set is made to contain an ineligible player. The set is empty when no back-fill fires, and holds the injured player only under Q3 (b). |
| T-IM-10 | **[#30]** hosted in ClientApp.Tests; engine ticked past its first AI stride | **Kickoff boot path.** `Kickoff` with a non-default managed tactic logs no command for it. After the first AI stride (the tactic's commit point, §2.6; no earlier reader exists), the engine digest equals that of a PM-1 `MatchSession(MatchSetup)` boot with the same squads, tactic and manager configuration, ticked the same count (managed club home, Q1 (a)); the away mirror and Q1 (b) compare with a bare engine booted by the same `ConfigureSquads` → manager configuration → `ApplyKickoff` calls. Under Q1 (a), `GetManagerState` (internal, visible to ClientApp.Tests) reads the opponent as `ManagerMode.AI` with its kickoff preset seeded, on both the headless and interactive paths; under Q1 (b), as `ManagerMode.Human` at Balanced. |
| T-IM-11 | **[client]** MatchClientCore.Tests (isolation, session kinds; a few hundred ticks); ClientApp.Tests (the `Complete` outcomes, which need `SeasonLoop`); the existing PM-1 tests in MatchClientCore.Tests, MatchClientWeb.Tests and ClientApp.Tests, unmodified | **Observer isolation on career-attached sessions only.** (a) On a session built by `MatchSession.Attach`: an `After` consumer that throws at tick N leaves the `First` (season) consumer observing every tick and `Complete` succeeds; the analytics adapter's `Publish().IsIncomplete` reads true and `TickConsumerFault(After)` holds the exception. A `First` consumer that throws at tick N leaves analytics observing every later tick, latches that consumer's fault, and `Complete` fails loud on the observation gap. In neither case is `LiveMatchStreamer.PostTickObserverFault` set. A second `First` registration and any registration after start are refused; a second `AttachTickObserver` is accepted as a further `After` consumer. (b) On a `MatchSession(MatchSetup)` session: `AddTickConsumer` is refused; `AttachTickObserver` stays set-once; a throwing observer latches `Streamer.PostTickObserverFault`. `ClientMatchCoordinatorTests.cs:94`, `MatchClientHostTests.cs:128–136` and `:181–190`, `SessionMatchAnalyticsTests.cs:68` and `S0DemoFixtureComparisonTests.cs:95` pass without edits. |
| T-IM-12 | (a) **[#30]** hosted in ClientApp.Tests; (b) **[client]** ClientApp.Tests; 1 real paced match | **Quiescence.** A real `Start()` session runs to full time. (a) A test hook at the entry of the handle's `ObserveTick`, after `RunTick` and before the handle's lock is taken, holds the final tick's observation open while the season thread calls `Complete`: it is refused on the observation count (it neither blocks nor deadlocks) and loses no card; after the hook is released and `Stop()` has returned, `Complete` succeeds. After `Complete` or `Abandon`, a late `ObserveTick` throws the named F-row, the composite catches it, and no unhandled exception reaches the pacing thread. (b) `CareerSession.CompleteMatchday(session)` and `AbandonMatchday(session)`, called on a session that is still running, have stopped it (joined the pacing thread) before the season seam runs, so a caller cannot reach the seam first. |
| T-IM-13 | **[#30]** SeasonSave.Tests; hand-ended engine | **Apply-only and Faulted.** (a) A roster-integrity fault (F6 or F9) in a non-managed club's squad is raised at `Begin`, with nothing applied and the round Closed. (b) A fault injected after the first write (the `IFixtureDisciplineDriver` seam, as `SeasonLoopDisciplineTests` does) leaves the round Faulted: `Abandon`, commands and `Save` are refused, `FaultCause` is set, and `EnginePlayedFixtures`/`MatchOutcomes` are as the partial apply left them. (c) A precondition refusal is not Faulted and `Abandon` then succeeds. |
| T-IM-14 | (a) **[#30]** SeasonSave.Tests; (b) **[client]** ClientApp.Tests; no match | **No save while a round is open; one progress action.** (a) `SeasonSaveManager.Save(loop, …)` refuses in Open, Live and Faulted, whatever `matchOrNull` holds. (b) In those states `CareerSession.SaveTo` refuses, `NextAction` is `MatchdayInProgress`, `IsProgressBlocked` reads true between operations, and `Execute(MatchdayInProgress)` is refused with its named error; a career-attached `MatchSession.CaptureSave` throws, and the engine is reachable only once through `TakeEngineForSession()`. On a managed fixture day (Closed, `ManagedThroughEngine`, provider owned) `NextAction` is `PlayManagedMatchday`; `Execute(PlayManagedMatchday)`, `Execute(PlayMatchday)` and `AdvanceRound` through the dispatcher are refused with the named error and the loop's full save bytes equal a control's. On a bye round, or under `QuickSimAll` or `FullEngine`, `NextAction` stays `PlayMatchday` and plays as today. |
| T-IM-15 | **[#30]** hosted in ClientApp.Tests; 2 real matches | **Abandon → save → load → replay.** `Begin`, `Kickoff`, some ticks, `Abandon`, `CareerSession.SaveTo`, load, `Begin`, `Kickoff` with the same config: the engine boots byte-identically to an uninterrupted `Begin`/`Kickoff`, and the committed round and full save bytes equal the uninterrupted run's. The loaded state's cursors are within the clock band (§1). Negative case: in the post-abandon state `ApplyTransaction`, `SubmitTransferBid` and `TrySetFocus` are refused with their named F-row and leave the full save bytes unchanged, and a following `Begin` gives the same preview as without the attempts (§2.3). |
| T-IM-16 | **[#30]** SeasonSave.Tests; 1 real match | **Pre-change anchor.** For fixed seeds, values captured by running the synchronous `AdvanceAndPlayNextRound` at the head immediately **before** the code landing (the `LeagueBootstrapGoldenVectorTests` convention; the head SHA is recorded in the landing) are pinned: the managed engine's final digest, every `MatchResult`, the full save bytes' SHA-256, `MatchOutcomes.Count`, `EnginePlayedFixtures`, ban and appearance state. The refactored synchronous path must reproduce them under Q1 (b); if staging identity tactics through `ApplyKickoff` changes the managed digest (Q1 (b)), either the Default config skips `ApplyKickoff` on both paths or the change is accepted with digest-change evidence as under Q1 (a). Under Q1 (a) the managed-engine-dependent values change and are re-pinned with evidence, and the rest still match. **Moved failure point (§2.2):** a roster-integrity (F6/F9) fault injected for fixture *k* throws at `BeginRound` with nothing applied, and the round then retries; the synchronous call never enters Faulted, and `SeasonLoopDisciplineTests`' M12 retry test and `AppearanceRecordTests`' recording-throw test pass unmodified. |
| T-IM-17 | **[#30]** SeasonSave.Tests; a few thousand ticks on a raw engine | **Hot-path allocation.** Over a window of ticks with no card and no substitution, the handle's `ObserveTick` pass allocates 0 bytes, measured with `GC.GetAllocatedBytesForCurrentThread` against a control, the form `MatchClientDriverTests` already uses. If it does not hold, the §4 item 11 sentence is withdrawn rather than the lock loosened. |

## 6. What this does not do

- No mid-match save, no persisted command log, no change to the season save format.
- No change to round order, non-managed results, the quick-sim model, engine source or any `[GT]`.
  Engine *configuration* on the career path changes only as Q1 decides (§3).
- No forfeit, instant finish or kickoff autosave. Quick-resolve for the managed fixture is Q2, not
  decided here.
- No starting-XI selection. A later XI choice has two prerequisites, recorded as a future ERR dependency: `IneligibleToEnter` and the preview recomputed at `Kickoff` (both are computed at `Begin` from the automatic XI), and a #30 §3.4 / #44 §2.3 rule on selecting reinstated players (a chosen XI could start a reinstated-suspended player and break §3.4's minimum-achievable guarantee, the ERR-030-046 class).
- No engine-side substitution eligibility (§2.7) and no change to the streamer's single post-tick slot
  (§2.8).
- No interactive route for `QuickSimAll` or `FullEngine` (§2.4): both keep the synchronous path.
- No change to PM-1's or the web host's sessions (`MatchSession(MatchSetup)`, `RestoreFrom`). The §2.8
  composite is installed only by `MatchSession.Attach`, so on those sessions the set-once slot,
  `Streamer.PostTickObserverFault`, its two production readers (`MatchClientHost.AnalyticsFault`,
  `S0ScreenPresenter`'s statistics refresh) and the tests that pin them are unchanged; T-IM-11 checks
  it.
- No #21 or #26 rule change; §4 names them as affected callers only.
- No #38 screen-spec change beyond the client-design navigation moves and the Appendix C rows of §2.11.

## 7. Cost and sequencing

About "L" for career item 2, at the upper end: one #30 amendment without a format change, the
`BeginRound`/`CompleteRound` split and `BootFixtureEngine` compose/configure split, a `season-save.asmdef`
reference to `TacticalInstructions`, the participation record and round plan, a `match-client-core`
entry point with the per-tick composite and the managed-team drain checks, an attached-session path through
`MatchSessionLifecycle` and `ClientMatchCoordinator` with career identity binding and a managed team index
in the presenter (§2.4), the pre-change golden capture, #41 and #44 errata, the `CareerSession` operations
and the `NextAction` values, the #38 Appendix C entry, and the client route. Test-side access: `season-save`
`AssemblyInfo` gains `InternalsVisibleTo(TacticalDirector.ClientApp.Tests)` (the grant `match-engine`
already makes, `AssemblyInfo.cs` l.15) for the T-IM-12 hook, `season-save-tests.asmdef` gains
`TacticalInstructions`, and the synchronous path gains an `internal` end seam for T-IM-03. It runs in the
career lane; `src/match-engine/` should need no change (if it does during the W8 B→C window, it carries
digest-neutrality evidence or waits, per the roadmap). Host: compile plus filtered EditMode for the
`SeasonSave`, `MatchClientCore`, `ClientApp`, `Discipline` and `InjuriesMedical` test assemblies (D-01a
items 1–2); no whole-MatchEngine run unless engine source changes.

**Lock cost.** Counting each lock home and away, §5 needs 18 real 90-minute matches (T-IM-01: 4, 02: 4,
15: 4, 16: 2, 12: 2, 07's last-tick case: 2), about 36 minutes of compute at the roadmap's two minutes
a match. Every other lock runs on a fresh or hand-ended engine in milliseconds to seconds. Whether the
real-match locks sit in the per-PR gate or a declared compute slot is a code-landing decision recorded
with the landing; no suite in `season-save` plays a full match today (its tests say so), so these are
new cost and are placed in `ClientApp.Tests` except T-IM-16 and T-IM-07's last-tick case.

## 8. Path to approval

1. Owner answers Q1 and Q2 (§3); the #41 owner answers Q3.
2. #41 owner accepts or amends §2.5's appearance rule; #30, #44 and #41 owners accept or amend §2.7, and
   the owner accepts knowingly that §2.7 (a) is a functionally short bench.
3. Adversarial review of this draft to convergence (the spec-promotion / W8 C precedent).
4. Owner approval of the wording.
5. Application, in two parts. **With approval, ahead of code:** #30 sections 1–5 and appendices as
   itemised in §4 under one ERR-030 entry; the #41 appearance rule and the §2.7 #44/#41 mirror text; the
   #26/#21 check; client-design §5-P7 updated; ERR entries allocated against that head
   (`spec-error-log.md`). **With the code landing, same commit** (the ERR-038-005 precedent; CLAUDE.md:
   a specification defect and its back-propagation land together), preceded by the T-IM-16 capture: the
   #38 Appendix C rows, `IntentKind` appends and `Substitute` row correction (Appendix C lists only seams
   and ordinals that exist); the #44 update of ERR-044-010 and of FR-DC-011, §3.3 and §4.5
   (`OnClubFixturePlayed` receives the starting eleven until then); and the `match-engine-wiring-backlog.md`
   W7 entry, which records the career path's status per Q1 (inert under (b), AI-managed under (a))
   instead of "WIRED" alone.

## Review history

| Version | Date | Change |
|---|---|---|
| 0.6 | 2026-11-03 | AR-2 Medium/Low fixes (M7, M11–M21, L4–L10). **Interactive route (M11–M13, M17):** new `PlayManagedMatchday` action routed to Tactics Setup, `PlayMatchday`/`AdvanceRound` refused on managed fixture days, Q2 gains option (d) and the Q1 (a) × Q2 asymmetry; `ManagedTeamIndex` on preview and handle, with the career-attached drain refusing the other team's commands; `Ended` dropped as a stored state; post-abandon mutators refused until the round is played. **Round integrity (M14, M21, L8, L10):** the synchronous path stays stateless and Faulted is interactive-only, per-mode resolution placement and the moved F6/F9 failure point stated, `Kickoff` atomic, FR-SN-003 plan check, lock-free fault read, join enforced in `CareerSession.CompleteMatchday`/`AbandonMatchday`. **Specs, ownership, locks (M7, M15, M16, M18, M19, L4–L7):** §4 gains the FR-SN-025/§1.2, FR-SN-013b, §4.1/§4.2, §7 and outline sites; locks split by owner and host; #38 Appendix C and the ERR-044-010 closure move to the code landing; §2.7 split with new Q3 for tier 1 and the August 17 decision cited; XI-selection claim replaced; tactic-commit rationale, T-IM-10, the #44 quote, W7 and `IneligibleToEnter` corrected. **Client facts (L9):** `Execute` refuses `MatchdayInProgress`, `IsProgressBlocked` source change, autosave rationale dropped. |
| 0.5 | 2026-11-03 | AR-2 High fix (H4). v0.4's §2.8 installed the composite observer on every `MatchSession`, which would have left `Streamer.PostTickObserverFault` null for good on PM-1 and the web host (its readers `MatchClientHost.AnalyticsFault` and `S0ScreenPresenter.cs:504` would go blind) and broken more tests than the one it named, while §6 said PM-1 was unchanged. Option (a) adopted: `MatchSession.Attach` alone installs the composite; `AttachTickObserver` registers an `After` consumer on a career-attached session and stays set-once elsewhere; `AddTickConsumer` is refused on other sessions. The career route keeps a fault signal (the analytics adapter's own `_incomplete` latch, `TickConsumerFault(After)`, `IsObservationFaulted`), and its match screen reads the session surface where `S0ScreenPresenter.cs:504` reads the streamer. Option (b) recorded and rejected. §1 names the readers and tests; §2.4, §2.8, §6 and T-IM-11 aligned; T-IM-11's `Complete` outcomes move to ClientApp.Tests, the host that reaches both `SeasonLoop` and `MatchSession` (§5). |
| 0.4 | 2026-11-03 | AR-1 Medium/Low fixes (M1–M10, L1–L3). **M1–M2 (round integrity):** new §2.9 thread contract (Complete/Abandon on the season thread after `MatchSession.Stop()` joins, `MatchEnded` read after the join, handle lock and invalidation, one engine in flight on the process-static bus); new §2.4.1 makes Complete apply-only, with every fallible per-fixture resolution precomputed at `BeginRound`, a Faulted state (Abandon refused, cause surfaced), and `FullEngine` refused for the interactive route; T-IM-12, T-IM-13. **M3–M4 (one implementation, participation):** §2.2 splits `PlayNextRound` into internal `BeginRound`/`CompleteRound` for every mode, with `BeginManagedFixture` as a narrower wrapper; T-IM-16 pins pre-change values; §2.5 defines participation (boot occupants plus incoming substitutes), moves it to a #30-owned `ParticipationRecord` fed by the handle's single pass, and §2.6 refuses pre-kickoff substitutions at the drain; "not a second walker" withdrawn. **M5–M6 (saves, #44):** §2.10 makes `Save(SeasonLoop, …)` refuse while a round is open, replaces `handle.Engine` with one-shot `TakeEngineForSession()`, refuses `CaptureSave` on a career-attached session, specifies the `CareerSession` operations, `MatchdayInProgress` and the post-abandon Closed state (T-IM-14, T-IM-15); §2.5's #44 item restated as conformance to FR-DC-011 / #44 §4.5 that closes ERR-044-010, with only #41 sign-off kept. **M7–M8 (normative sites, routes):** §4 names each #30 site with its replacement sentence under one ERR-030 entry and maps the locks to a T-SN-IM block in §5.9/§5.8; new §2.11 route table, D-P7-3 restated with the bye and unemployed fallback, #38 Appendix C rows; new owner question Q2 (quick-resolve for the managed fixture) with a recommendation. **M9–M10 (faults, refusals):** the handle exposes `IsObservationFaulted`, the client polls it and offers only abandon at once (§2.8); T-IM-07 extended to injected faults and first/last-tick gaps; §2.3 derives the refused-operation list from the real public surface, with a reflection check in T-IM-06. **L1–L3 (precision):** §1 corrected (day-steps include slot 1, the quick-sim row is mode-specific, "sim thread" split into season thread and tick thread, `CareerSaveSlots.cs:217` is the operative site, the fold's check scope, the new tick counter); locks compare full save bytes and counters, state their host assembly and real-match cost (§5, §7); §4 item 11 amends the §4.3 hot-path note with T-IM-17, and §2.1 aligns save wording (manual save on Career Home, autosave after Complete). |
| 0.3 | 2026-11-03 | AR-1 High fixes (H1–H3). **H1:** §2.5's "a suspended player is never on the bench" was false (the ERR-030-044..046 back-fill reinstates suspended and injured players onto the bench); new §2.7 makes back-filled bench players ineligible to enter (option (a); (b) recorded and rejected), refused at the `match-client-core` drain with a season-side `Complete` backstop and an ERR-030 obligation; T-IM-09. **H2:** `Begin` no longer boots the engine; it computes a pre-kickoff preview, and `Kickoff(PreKickoffConfig)` boots through a compose/configure split of `BootFixtureEngine`, applying the managed tactic on the #26 FR-TP-004 boot path instead of a "tick-0" command; §2.3 states and T-IM-01/04/06 aligned; #26/#21 named in §4/§6; new owner question Q1 (career opponent AI manager); T-IM-10. **H3:** new §2.8 composite per-tick observer owned by `MatchSession` with per-consumer fault isolation, season observation first; T-IM-11. §1 gains the facts each fix rests on. |
| 0.2 | 2026-11-02 | Owner decisions applied: no starting-XI changes (v0.1 Q1, answer (a)); no mid-match saves; quitting a match abandons it and returns to Career Home with the round unplayed; no kickoff autosave (v0.1 Q2/Q3 withdrawn). Removed: the managed-fixture sub-blob, season-save format 8 → 9, restore path and live-save locks, and the managed-first round order (index order kept; headless path unchanged). Added: `AbandonManagedFixture`, the abandon lock, save unavailability while a round is open. Size back to about "L". Not yet adversarially reviewed. |
| 0.1 | 2026-11-02 | Initial draft, from the November 2 review of the client-design bullet contract: substitutions vs starting-XI consumers, unserialized card fold, engine/observer ownership across assemblies, round order and states, save-policy owner questions. |
