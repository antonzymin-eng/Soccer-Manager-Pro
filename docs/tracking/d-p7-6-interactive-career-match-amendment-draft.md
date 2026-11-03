# D-P7-6 — Interactive career matches: amendment draft to Season & Competition Loop #30

**Created:** November 2, 2026\
**Last Updated:** November 3, 2026\
**Version:** 0.4\
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
| `CareerProgress.NextAction` derives its one action from calendar and clock only: on the fixture day it returns `PlayMatchday` whatever any round state. `CareerSession.Execute` checks the action is that legal one, runs it, and autosaves after every action except `AdvanceDay`. The season dispatcher maps `AdvanceRound` onto `Execute(PlayMatchday)`. | `CareerProgress.cs` l.40–55; `CareerSession.cs` l.252–290; `SeasonCommandDispatcher.cs` |
| The public members that change state a round reads or a save persists are: on `SeasonLoop`, `AdvanceToNextFixtureDay`, `AdvanceDays`, both `AdvanceAndPlayNextRound` overloads, `RollToNextSeason`, `ApplyTransaction`, `SubmitTransferBid`; on `PlayerCareerStates` (reached through `SeasonLoop.Career`), `TrySetFocus`; on `DisciplineRules` (a public type that wraps the `DisciplineState` which `SeasonLoop.Discipline` exposes), `ApplyCard`, `AddYellow`, `AddBan`, `OnClubFixturePlayed`, `RollToNextSeason`, `MigratePlayerId`, `DropPlayer`. `SeasonState`'s mutators and `LeagueTable` are `internal`; `ProgressionEngine`, `DisciplineState` and `TransfersSnapshot()` (a clone) expose reads only; `PlayerCareerStates.MarkUnavailable` writes only the caller's arrays. No source in `client-app` or `match-client-core` constructs `DisciplineRules` or calls `TrySetFocus`. | `SeasonLoop.cs` (public members); `PlayerCareerStates.cs` l.1356, l.1473; `DisciplineRules.cs` l.81–486; `SeasonState.cs` l.63, l.309–378 |
| The career client never reads a match block, and `season-save` does not reference `match-client-core`; `client-app` references both. | `CareerSession.cs:185`; the three `.asmdef` files |
| `MatchSession` has a private engine-wrapping constructor, a public `AttachTickObserver` (runs after every tick on the **tick thread**) and `TickOnce`. `AttachTickObserver` forwards to `LiveMatchStreamer.SetPostTickObserver`, which is **set-once**; PM-1's `SessionMatchAnalytics.Attach` takes that slot on every match, and a second attach throws. `InvokePostTickObserver` disarms the whole observer on its first exception and latches `PostTickObserverFault`. | `src/match-client-core/MatchSession.cs` l.72–107, 247, 287; `src/match-viewer/LiveMatchStreamer.cs` l.255–307; `SessionMatchAnalytics.cs:39`; `ClientMatchCoordinator.cs:104`; `ClientMatchCoordinatorTests.cs:94` |

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
does before its apply loop: the guards, `RequireCommittableConfig`, `RunCareerDaySteps`, and (§2.4.1) the
per-fixture resolution that can fail. `CompleteRound(plan)` is the apply loop. The two public
`AdvanceAndPlayNextRound` overloads keep their provider checks (F7) and then run `BeginRound` →
[for a round whose managed fixture runs through an engine: boot with `PreKickoffConfig.Default`, tick to
full time pumping the same per-tick observation as §2.4] → `CompleteRound`. `BeginManagedFixture` is the
public wrapper over `BeginRound(_careerSquads)` for the interactive route and accepts fewer cases than
the synchronous call (§2.4), so a mode the interactive route refuses is not left without an
implementation: it keeps the synchronous one. Under `FullEngine` the synchronous path's non-managed
engine matches still run in place in the apply loop, as today.

Booting the managed engine before the earlier-indexed fixtures are applied is expected to give the same
engine: the managed squad's availability, entry fatigue and seed do not depend on other clubs'
fixtures, and nothing is applied between `Begin`, `Kickoff` and the first tick. Lock T-IM-01 proves
interactive and headless runs agree; **it compares new code with new code**, so the claim that the
synchronous path is unchanged from today rests on T-IM-16, which compares it with values pinned from the
pre-change head (§5). Whether the headless digest itself is unchanged from today depends on Q1 (§3):
under Q1 (b) it is expected unchanged and the landing shows it on the existing headless
`ManagedThroughEngine` fixtures; under Q1 (a) the managed fixture's digest changes, the landing carries
digest-change evidence, and T-IM-16 still holds for everything that does not depend on the managed
engine (non-managed results, their table rows, the day-step state).

### 2.3 Round states

| State | Entered by | Season state | Season-side `Save` |
|---|---|---|---|
| **Closed** | cursor advance; `AbandonManagedFixture`; any load; construction | pre-round, **or** fixture day with the matchday-morning steps run and the round unplayed (the post-abandon shape, §1) | yes, as today |
| **Open** | `BeginManagedFixture` | matchday-morning steps done; round plan computed (§2.4.1); pre-kickoff preview computed; **no engine**; nothing applied | refused |
| **Live** | `Kickoff` (engine booted; the first tick follows) | as Open | refused |
| **Ended** | full time, read by the season thread after the tick thread has stopped (§2.9) | as Open | refused; the client calls `CompleteManagedFixture` before Post-Match |
| **Faulted** | an exception from `CompleteManagedFixture` **after its first write** (§2.4.1) | partly applied; undefined | refused |
| **Committed → Closed** | `CompleteManagedFixture` | round fully applied, cursor advanced | yes; the D-P7-2 post-round autosave follows |

`AbandonManagedFixture` from Open discards the preview and the round plan. From Live or Ended it
discards the engine, the fold and the participation record as well. Either way it returns to Closed.
**It is refused in Faulted**, where "nothing was applied" is no longer true. A `CompleteManagedFixture`
that is refused before its first write (any precondition in §2.4) leaves the round in Ended and
Abandon is permitted. The matchday-morning steps that `Begin` ran stay run; they are idempotent for the
day, so the next `Begin` repeats them as no-ops and computes the same plan and preview, and `Kickoff`
with the same `PreKickoffConfig` boots the same engine. The post-abandon Closed state is saveable and is
a state the career can be loaded into (T-IM-15).

**Refusals while the round is not Closed.** Rule: every public member of `SeasonLoop`, and of each
object it hands out, that changes state a round reads or a save persists is refused with a named
failure mode (new F-row). Enumerated from the real surface (§1), not from memory: `AdvanceDays`,
`AdvanceToNextFixtureDay`, both `AdvanceAndPlayNextRound` overloads, `RollToNextSeason`,
`SubmitTransferBid`, `ApplyTransaction`, a second `BeginManagedFixture`, `SeasonSaveManager.Save(loop,
…)`, and `PlayerCareerStates.TrySetFocus` (reached through `SeasonLoop.Career`; `PlayerCareerStates`
holds no loop reference, so the loop sets an `internal` round-open flag on it that `TrySetFocus`
checks). `DisciplineRules`' public writers over `loop.Discipline` cannot be refused by the loop, which
never sees the call; that is a caller contract, recorded at #30 §4.3, and the landing verifies by grep
that no client assembly constructs `DisciplineRules`. `Kickoff` is accepted only in Open. Read-only
views stay available. T-IM-06 carries the list and fails when a new public mutator is on neither it nor
the read-only allowlist.

### 2.4 Seams and ownership

**`season-save` (#30) owns the round, the engine, the fold and the participation record.** New public
surface on `SeasonLoop` (names indicative):

- `RoundState RoundState` and `bool NextRoundHasManagedFixture` — read-only views the client and
  `CareerProgress` read (§2.10, §2.11).
- `ManagedFixturePreview BeginManagedFixture()` — Closed only. Runs `BeginRound(_careerSquads)` (§2.2):
  the existing guards, `RequireCommittableConfig` in the position it has today (with the other guards,
  before the day-steps; `Complete` does not repeat it), the matchday-morning day-steps (slots 1, 2 and
  4), the per-fixture resolution of §2.4.1, and the **compose** half of `BootFixtureEngine` (below) for
  both managed clubs. Returns the preview: the filtered squads, the automatic starting elevens and the
  per-club ineligible-to-enter set (§2.7). Constructs no engine. Enters Open. Refused, each with a
  named F-row, when the round is not Closed; when the mode is not `ManagedThroughEngine` (`QuickSimAll`
  has no managed engine, and `FullEngine` would tick other engines in `Complete`, which §2.4.1 and §2.9
  rule out); when the round has no managed fixture; or when the loop owns no squad provider (the
  refusal the no-argument `AdvanceAndPlayNextRound` raises). Those cases are served by the synchronous
  path (§2.2, §2.11).
- `ManagedFixtureHandle Kickoff(in PreKickoffConfig config)` — Open only. Runs the **configure** half of
  `BootFixtureEngine` on the preview's own squad instances, so the eleven Tactics Setup showed is the
  eleven configured (the ERR-041-010(b) colocation rule; no second availability walk). Boot order is
  PM-1's: `ConfigureSquads` → the opponent's manager configuration as Q1 (§3) decides →
  `ManagerAdaptation.ApplyKickoff(engine, catalogue, baseline)`, where `baseline` holds
  `config.TeamTactic` in the managed club's team slot and `TeamTactic.Balanced` in the other. That is
  the #26 FR-TP-004/010 boot path. Then seeds the fold and the `ParticipationRecord` (§2.5) from
  `PlayerIdsByAgentId()` immediately after boot (the ERR-044-023 boot-time precondition) and enters Live.
- `PreKickoffConfig` — PM-2 carries one field, the managed club's `TeamTactic`.
  `PreKickoffConfig.Default` holds `TeamTactic.Balanced`, the tactic the engine starts both teams at
  today. A later starting-XI choice (roadmap §5.1a career item 4 lists squad/selection) is a further
  field read by the compose/configure split, with no change to `Begin`'s or `Kickoff`'s signature. PM-2
  adds no such field (§2.1).
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
- `ManagedFixtureHandle.IneligibleToEnter` — the preview's per-club set (§2.7), as player ids.
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
  leaves the round in Ended. It then applies the round (§2.4.1) in index order and advances the cursor.
- `void AbandonManagedFixture()` — §2.3; season thread, after the tick thread has stopped (§2.9).

`SeasonLoop.ActiveMatch` stays the synchronous-path accessor.

**`match-client-core` gains one entry point and the per-tick composite:** `MatchSession.Attach(MatchEngine
engine, IReadOnlyList<int> ineligibleToEnter)` wraps an engine it did not boot (the existing private
constructor), applying no boot mutator, and carries the §2.7 set as plain player ids. A session built
this way is a **career-attached** session: it refuses `CaptureSave` (§2.10) and its drain refuses
pre-kickoff substitutions (§2.6); PM-1's `MatchSession(MatchSetup)` is unchanged.
The client registers `handle.ObserveTick` as the session's `First` per-tick consumer and then attaches
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
- `RequireCommittableConfig`.

`Complete` then applies in the existing order (appearances → table → outcome event → mark played →
ban serving → card commit) from the plan, the finished engine's score and the participation record.
`EnginePlayedFixtures` and `MatchOutcomes` change only here, so Abandon leaves both untouched. The
synchronous path shares `CompleteRound` but, under `FullEngine`, still plays the non-managed engine
fixtures inside it as today; the apply-only guarantee is stated for the interactive route, which refuses
`FullEngine` (§2.4).

What can still throw inside the apply is the residual `CardLedgerFold.CommitWithExplicitConfig` records
(its L2 note: an accumulator at `int.MaxValue`, reachable only from a corrupt decoded row). If anything
throws after the first write the round enters **Faulted**: Abandon, every command and every save are
refused, and the cause is readable (`FaultCause`). The client shows it and offers only Main Menu / Load;
the recovery is the last save, as after a crash. Faulted is terminal for that loop instance. A
precondition refusal is not a fault.

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
  composition-root MUST to widen the array "the moment a substitution seam gets a production caller".
  The interactive route is that caller on the season path. The application passes the participation set
  where `homeXi`/`awayXi` are passed today, **closes ERR-044-010** (recorded, not fixed), and refreshes
  #44 §3.3's SUBSTITUTION DEPENDENCY paragraph, §4.5's fielded-eleven bullet and FR-DC-011's "today that
  eleven is the STARTING eleven only" sentence to say they are satisfied. No #44 owner sign-off and no
  new #44 ERR is needed for this item. A suspended player **can** be on the bench: the extremis
  back-fill (#30 §3.4, ERR-030-044..046) deliberately reinstates suspended players there. §2.7 makes a
  back-filled bench player ineligible to enter, so under this rule the only suspended players in the
  participation set are back-filled starters, which is exactly #30 §3.4's residual, and a benched
  reinstatee's ban advances. The §2.7 mirror in #44 §2.3/§7.2 is a separate cross-file and stays as
  §2.7 states it.

### 2.6 Pre-kickoff choices

Team tactics chosen on Tactics Setup reach the engine as `PreKickoffConfig.TeamTactic` and are applied
at `Kickoff` as the managed club's boot baseline through `ManagerAdaptation.ApplyKickoff`, the #26
FR-TP-004 boot path PM-1 uses. They are not a command: the queue drains at the top of the tick after
enqueue (client design §6.2), carries only live mutators, and a live `SetTeamTactic` commits at a
stride boundary (#21 FR-TI-027), so there is no "tick 0" drain point at which a command would act as a
boot input. In-match tactic changes stay commands, unchanged. The starting eleven is not editable
(§2.1).

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

**Rule (option (a), adopted).** A player the back-fill reinstated onto the **bench** (tier 2, suspended
whether or not also injured, or tier 1, injured) is ineligible to enter the match. Back-filled
starters are unaffected and play as they do today.

- **The set.** The compose half of `BootFixtureEngine` derives, per club, `IneligibleToEnter`: the
  reinstated players not in the starting eleven. It is taken from `AvailabilityComposition.Compose`'s
  own reinstatement record, exposed as an output, not from a second walk (the parallel-surface rule). It
  is empty on the FR-DC-018 identity path, fixed at `Begin`, and carried on the preview and the handle.
  It covers both clubs.
- **Refusal site: `match-client-core`, at the drain.** `MatchSession.Attach` maps each id to its bench
  index once, through `MatchBootRoster.BenchPlayerId`. `MatchEngineMutations.RequestSubstitution` refuses
  a request whose bench slot is in the set with the existing `SubstitutionRequestStatus.BenchPlayerUnavailable`
  before it calls the engine. The driver records the refusal as it records any typed refusal (Gate J
  J-02). The substitution chooser greys the player out with a reason, beside the existing keeper-role
  greying (`S0ScreenPresenter.UpdateIncomingAvailability`); the greying is presentation, and the drain is
  the guarantee.
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
that a reinstated player on the bench cannot enter, citing this rule, so that residual (i)/(ii) stays
exhaustive. The same statement is mirrored at #44 §2.3 (the "Benched" bullet) and §7.2 as an ERR-044
cross-file.

**#41 note.** Under (a) an injured player the back-fill put on the bench may not be brought on, so #41's
appearance record never credits a tier-1 bench reinstatee. A tier-1 reinstatee who starts plays as
today.

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

**Owner: `MatchSession`, as a composite per-tick observer.** At construction `MatchSession` installs one composite
action into the streamer's slot and exposes ordered registration,
`AddTickConsumer(Func<MatchEngine, Action> factory, TickConsumerOrder order)`:

- `First` takes at most one consumer and runs before every other consumer on each tick. The career route
  registers the season observation there. `After` consumers run next, in registration order.
  Registration is refused once `Start` or the first `TickOnce` has run.
- Each consumer runs in its own `try`/`catch`. A consumer that throws is disarmed alone, its fault is
  latched on a per-consumer surface (`MatchSession.TickConsumerFault(order)`, indicative), and the other
  consumers run on that tick and every later one. The composite never throws to the streamer, so
  `InvokePostTickObserver`'s disarm-all path is not reached. The catch is on the observation path, not
  the 60 Hz inner loop, the same carve-out `LiveMatchStreamer` already cites. The consumer list is fixed
  before playback, so the composite allocates nothing per tick.
- `AttachTickObserver` becomes the `After` form. `SessionMatchAnalytics.Attach` registers through it
  unchanged; its "attaches exactly once … later per-tick consumers must compose here" remark is
  rewritten to point at the composite. `ClientMatchCoordinatorTests.cs:94`, which asserts that a second
  attach throws, is rewritten to the new contract: a second `After` consumer is accepted, a second
  `First` is refused, and registration after start is refused.
- **Season-consumer fault:** that consumer is disarmed and the handle has latched the exception itself
  (`IsObservationFaulted`, §2.4). The client polls `IsObservationFaulted` every frame (and
  `LiveMatchStreamer.PostTickObserverFault`, which stays null unless the composite itself fails),
  pauses playback at once, shows the reason and offers only abandon; it does not wait for full time.
  `CompleteManagedFixture` refuses a faulted handle with the round left in Ended and Abandon the only
  exit (§2.3), and the observation-gap check (§2.4) catches the same fault if the client missed it.
  **Analytics fault:** analytics reports incomplete, as it does today; the season observation
  continues and the match commits.

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
  before it is a racing read and is not used. `season-save` does not reference `match-client-core`, so
  it cannot verify the join; the rule is on the caller and the lock (T-IM-12) drives a real paced
  session through it.
- **What the seam enforces itself.** One lock on the handle serialises `ObserveTick`, `Complete`,
  `Abandon` and the fault read. After `Complete` or `Abandon` the handle is **invalidated**: every
  member throws a named F-row, so a late `ObserveTick` cannot write into a committed or discarded fold.
  The §2.8 composite catches that throw on the tick thread, so it cannot reach the streamer. The
  observation count (§2.4) is a backstop for a `Complete` issued before the final observation lands; it
  is not the guarantee, the join is.
- **One engine in flight.** From `Kickoff` until `Complete`/`Abandon` returns, no other engine is booted
  or ticked in the process: the round state allows one handle, the interactive route refuses
  `FullEngine` so `Complete` boots none, and the client runs no PM-1 session while a round is not Closed.

### 2.10 Save enforcement and the `CareerSession` operations

- **`SeasonSaveManager.Save(SeasonLoop, …)` refuses** (new F-row) unless `RoundState` is Closed,
  whatever `matchOrNull` holds. That overload is the one `CareerSaveSlots.Write` uses (l.217). The
  long-form overload takes the loop's parts, not the loop, cannot see round state, and is not a
  career-route entry. A career-attached `MatchSession` refuses `CaptureSave`, so the match-blob path is
  closed too. `TakeEngineForSession()` (§2.4) leaves no public handle on the engine.
- **`CareerProgress.NextAction` gains `MatchdayInProgress`**, returned whenever `RoundState` is not
  Closed. It is not executable: `CareerSession.Execute` already refuses any action but the legal one, so
  the synchronous `PlayMatchday` can no longer be offered over an open round, and
  `CareerHomeViewModel.IsProgressBlocked` reads true. `CareerSession.SaveTo` refuses while
  `MatchdayInProgress`, before it reaches the season guard, so an `InvalidOperationException` does not
  escape the autosave path that catches only I/O errors.
- **`CareerSession` operations** (names indicative), each taking `BeginOperation` and bumping
  `Revision` as `Execute` does: `BeginMatchday()` → `BeginManagedFixture`; `StartMatch(config)` →
  `Kickoff`, returning the handle; `CompleteMatchday()` → `CompleteManagedFixture`, then builds
  `CareerChange.RoundPlayed(...)` as `Execute(PlayMatchday)` does and calls `Autosave()`;
  `AbandonMatchday()` → `AbandonManagedFixture`. The D-P7-2 autosave therefore follows `Complete`, on
  the season thread, as it follows the synchronous round. `LastAutosaveError` behaves as today.
- **Post-abandon Closed.** A save taken after an abandon is a fixture day with the steps run and the
  round unplayed (§2.3). Loading it gives `NextAction == PlayMatchday`, and `Begin` repeats the steps as
  no-ops. T-IM-15 locks that this state commits the same round as an uninterrupted run.

### 2.11 Client routes and intent routing

Route table (screen-move names indicative; the client owner confirms them in the client design
supplement at application). Each row is a **new named move**, never a reuse of `ReturnToMainMenu`
(ux-experience-architecture §3.1); the career stack is rooted at Career Home, so the Tactics-cancel
junction ux §3 row "Tactics cancel → Career Home" leaves open is a pop onto that root, distinct from
PM-1's `CancelTacticsSetup`, which pops to Main Menu.

| From → to | Move | Season seam | Round state after |
|---|---|---|---|
| Career Home → Tactics Setup | play the match | `BeginManagedFixture` (via `BeginMatchday`) | Open |
| Tactics Setup → Match | start match | `Kickoff(config)` (via `StartMatch`) | Live |
| Tactics Setup → Career Home | cancel | `AbandonManagedFixture` from Open | Closed |
| Match → Career Home | quit (confirmed) | stop the session, then `AbandonManagedFixture` from Live or Ended | Closed |
| Match → Post-Match | full time | stop the session, then `CompleteManagedFixture`; the autosave follows | Closed |
| Post-Match → Career Home | return | none | Closed |
| any → Main Menu / Load (Faulted only) | leave | none | (loop discarded) |
| Tactics Setup → Match, quick-resolve | per Q2 | per Q2 | per Q2 |

**D-P7-3 restated for the interactive route.** The client still offers exactly one progress action, now
derived from `NextAction`:

| State | Offered | Seam |
|---|---|---|
| clock before the pending fixture day | `AdvanceDay` | `AdvanceDays(1)` |
| fixture day, `NextRoundHasManagedFixture`, mode `ManagedThroughEngine`, loop owns a provider, round Closed | `PlayMatchday` → Tactics Setup | `BeginManagedFixture` |
| fixture day, **no managed fixture** (the bye round of an odd-N league, FR-SN-004) or a mode/provider case `BeginManagedFixture` refuses | `PlayMatchday`, no match screen | synchronous `AdvanceAndPlayNextRound()` — a quick-simmed or all-quick-sim round, as today |
| round Open, Live, Ended or Faulted | `MatchdayInProgress` (no action) | — |
| season complete, final fixture day not yet ticked | `AdvanceDay` | `AdvanceDays(1)` |
| season complete, final fixture day ticked | `StartNextSeason` | `RollToNextSeason()` |

**Unemployed manager.** FR-SN-013b (ERR-030-021) makes `ManagedClubId` optional so that a manager
between jobs is representable; when absent every fixture is quick-simmed and no engine runs. That is
spec text only: `SeasonState.ManagedClubId` is a plain `int` today and the representation lands at #54
T2. When it does, an unemployed round has no managed fixture and takes the row above, so this contract
needs no further rule for it.

**#38 Appendix C.** FR-UI-003 routes UI-originated sim mutation through an existing public seam, and
Begin, Kickoff, Complete and Abandon are such seams, so they bypass nothing: the application adds
Appendix C rows under one ERR-038 entry, in the form ERR-038-005 used. `AdvanceRound` keeps its row for
the synchronous case. Season intent kinds are appended (ordinals after 6, names indicative):
`BeginManagedMatch` → `BeginManagedFixture()`, `StartManagedMatch` → `Kickoff(config)` (payload: the
`TeamTactic`), `CompleteManagedMatch` → `CompleteManagedFixture()`, `AbandonManagedMatch` →
`AbandonManagedFixture()`. `IntentRouting` classifies them as season intents, so the KD-U2 drift guard is
narrowed by classification as it was for ERR-038-005, not weakened.

## 3. Open questions

Two product questions are open (Q1, Q2). Separately, the §2.5 appearance rule needs the #41 owner's
acceptance (the #44 half is conformance and needs none), and the §2.7 bench-eligibility rule needs the
#30, #44 and #41 owners' acceptance.

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
- **(b) Inert opponent for PM-2.** Keep `ManagerMode.Human` and Balanced. The headless digest is expected
  unchanged. This is a stated product limitation: the career opponent never changes its tactic during a
  match, and W7 kickoff adaptation stays dead on the career path.

**Recommendation:** (a), with archetype 0, so the career opponent behaves as the PM-1 opponent the owner
has already played. This is not decided here. Until Q1 is answered, §2.4's `Kickoff` and T-IM-10 are
written to apply either way.

**Q2 — Is there a quick-resolve option for the managed fixture?** The PM-2 roadmap criterion reads
"play (or quick-resolve) each round" (roadmap §1 PM-2; §5 step table, row 11). D-11 decided that managed fixtures are
played interactively and rejected an instant finish **on quit**. It did not say whether the player may
choose, before kickoff, to resolve the managed fixture without watching. That is a different feature
from the rejected one: the player asks for it, the result is committed through the normal path, and
leaving a match is not what triggers it. Today the player-reachable route to a synchronous round is
`PlayMatchday` on Career Home: `AdvanceAndPlayNextRound()` at default tactics, with no Tactics Setup
(§1).

- **(a) No quick-resolve for the managed fixture.** Career Home's matchday action goes to Tactics Setup
  and the match. The synchronous call stays for rounds with no managed fixture and for tests. Playback
  speed is the only way to shorten a match, and the roadmap's "(or quick-resolve)" is amended or read as
  the other clubs' fixtures.
- **(b) Quick-resolve from Tactics Setup.** After choosing a tactic the player picks Play or
  Quick-resolve. Quick-resolve is `Kickoff(config)` + the head-less tick loop with `ObserveTick` +
  `CompleteManagedFixture`, the sequence the synchronous call becomes (§2.2): no match screen, no
  mid-match input, about two minutes of compute off the UI thread (roadmap C1; client design §5-P7), the
  result committed and Post-Match shown as usual. No new season seam; the managed tactic still applies.
- **(c) Keep today's one-click `PlayMatchday`** on Career Home beside Play. Two routes to the same engine
  run, and the shortcut ignores any tactic the player wanted.

**Recommendation:** (b). It meets the roadmap's quick-resolve wording, applies the tactic the player
chose, needs no seam of its own, and T-IM-01 already proves it equals the interactive no-input run.
(a) is the fallback if the owner wants watched matches only. This is not decided here. Until Q2 is
answered, the §2.11 route table carries the quick-resolve row as "per Q2" and nothing else depends on it.

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
   Ended or Faulted every other mutating command is refused."
3. **`section-2.md` §2.2 data structures.** In the `SeasonLoop` bullet, replace "holds … the
   active-or-null `MatchEngine` (the managed club's in-progress fixture)" with "holds a round state and,
   from `Kickoff` to `Complete`/`Abandon`, a `ManagedFixtureHandle` that owns the managed engine, the
   fold and the `ParticipationRecord`; `ActiveMatch` remains the synchronous path's accessor". Extend
   the command-API list at the end of that bullet as in item 2. Add `RoundState`, `ManagedFixturePreview`,
   `ManagedFixtureHandle`, `PreKickoffConfig`, `ParticipationRecord`.
4. **`section-2.md` §2.3 failure modes.** New rows: a mutating command, `Save` or `TrySetFocus` while
   not Closed; `Begin` outside Closed, in `QuickSimAll`/`FullEngine`, with no managed fixture or no
   owned provider; `Kickoff` outside Open; an observation gap or fault; `Complete` before full time;
   `Complete` with an ineligible-to-enter player in the participation set; `Complete` or `Abandon` with
   no open round, `Abandon` in Faulted, any handle member after invalidation; a Faulted round (§2.4.1).
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
   repeats them as no-ops".
8. **`section-3.md` §3.4 pseudocode and the FR-SN-025 paragraph.** The `AdvanceAndPlayNextRound` block
   becomes `BeginRound` and `CompleteRound`; `PlayThroughEngine` becomes the compose/configure halves
   plus the per-tick `ObserveTick`. The paragraph "`AdvanceAndPlayNextRound` is invoked *from* the
   world-tick loop, so the two clocks stay disjoint (FR-SN-025)" becomes: "The match runs on the 10 Hz /
   60 Hz loops on the tick thread between `BeginManagedFixture` and `Complete`. No world-day command is
   accepted in that interval (§2.3), so the clocks stay disjoint by refusal, not by call nesting."
9. **`section-3.md` §3.4, depleted-squad guarantee and "tier order is not cosmetic"** (ERR-030, §2.7):
   extended to the bench — "a back-filled bench player cannot enter, so residual (i)/(ii) stays
   exhaustive". #44 §2.3/§7.2 mirror it (ERR-044 cross-file).
10. **`section-3.md` §3.4.1.** "both output the fielded XIs and the UNFILTERED roster ids" gains "(the
    managed fixture outputs its participation set)"; the determinism note gains "and the managed
    engine's boot time, before or after earlier-indexed fixtures are applied, does not change any
    result".
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
13. **`section-5.md` new §5.9 and §5.8.** §5.9 holds the locks of §5 below as `T-SN-IM-001..017`
    (working labels `T-IM-01..17` map one to one: `T-IM-nn` is `T-SN-IM-0nn`). §5.8's "Every
    FR-SN-001..034 + 013a/013b maps to at least one test above" extends to the new FR rows, each traced to
    its `T-SN-IM` lock. The §9 approval checklist's FR and test counts are refreshed in the application
    sweep.

Cross-file consequences (each its own entry at application):

- **#44 (conformance, §2.5):** closes ERR-044-010; refreshes FR-DC-011, §3.3's SUBSTITUTION DEPENDENCY
  paragraph and §4.5's fielded-eleven bullet. Separately, the §2.7 bench mirror at §2.3/§7.2.
- **#41:** FR-MD-010's "fielded" is "took part"; a back-filled injured bench player cannot be brought
  on (§2.5, §2.7).
- **#26 (Tactical Presets & AI-Manager Selection), affected.** The configure half becomes a second
  production caller of the FR-TP-004/010 kickoff boot path (`ManagerAdaptation.ApplyKickoff`), beside
  `MatchSession.BootEngine`; under Q1 (a) it is also a caller of `ConfigureManager` (FR-TP-007) for the
  career opponent. No FR-TP rule changes. Whether #26 records the career caller in its own text or only
  #30 cites it is settled at application.
- **#21 (Tactical Instructions), affected.** The Tactics Setup tactic is a pre-kickoff boot baseline,
  not an in-match change, so FR-TI-027's stride-boundary rule governs only in-match commands, as it
  does today. No #21 edit is expected; it is named so the application checks it.
- **#38 Appendix C:** the rows of §2.11 under one ERR-038 entry.
- **Client design supplement (`interactive-unity-client-design.md` §5-P7):** the D-P7-3 table and the
  routes of §2.11, the `CareerSession` operations and `MatchdayInProgress` of §2.10.

## 5. Locks (proposed §5 additions)

Working labels `T-IM-nn` map one to one to `T-SN-IM-0nn` in #30 §5.9 (§4 item 13). Every lock is
mirrored for the managed club at home and away. **Host** says where it runs: a lock that needs both
`SeasonLoop` and `MatchSession` lives in `TacticalDirector.ClientApp.Tests`, which references both
(`season-save` tests cannot reach `match-client-core`). **Cost** says whether it plays a real
90-minute match (about two minutes each, roadmap C1) or runs on a fresh or hand-ended engine, using the
existing `MatchEngine.TestOnly_CheckMatchFlowTransitions(MATCH_TICKS_TOTAL)` end seam (internal, visible
to both test assemblies). The match counts in the table are **per mirror**; §7 totals them.

| ID | Host / cost | Lock |
|---|---|---|
| T-IM-01 | ClientApp.Tests; 2 real matches | **No-input parity.** "No input" means `Kickoff(PreKickoffConfig.Default)` and no in-match manager commands. Same seed, rosters and configuration (including the opponent's manager configuration per Q1): an interactive managed match driven through `MatchSession.Attach` + `TickOnce` equals the headless `ManagedThroughEngine` round, which boots through the same configure half with the same default. Compared: final engine digest, every `MatchResult`, committed card tally, appearance records, bans, **the full save bytes** (`SeasonSaveManager.Save` to a temp file, byte-compared), `MatchOutcomes.Count` and `EnginePlayedFixtures`. `SeasonLoop.Snapshot()` alone is not enough: it encodes `SeasonState` only (`SeasonLoop.cs` l.1599) and none of the career blocks or counters. |
| T-IM-02 | ClientApp.Tests; 2 real matches | **Input replay.** Same initial state, the same `PreKickoffConfig` and the same tick-stamped command log give the same final digest and the same committed state, compared as in T-IM-01. |
| T-IM-03 | SeasonSave.Tests; hand-ended engine | **Season-commit equivalence.** Given the same managed result and participation set, `CompleteManagedFixture` and the synchronous path produce identical full save bytes, `MatchOutcomes.Count` and `EnginePlayedFixtures`. Both sides are new code, so this lock alone proves nothing about change from today; T-IM-16 anchors that. |
| T-IM-04 | SeasonSave.Tests (Open, and Live on raw `RunTick`); ClientApp.Tests (Live with a substitution through `MatchSession`, Ended on the end seam) | **Abandon.** After `Abandon` from Open (plan and preview only, no engine constructed), Live (including after a substitution and a card) or Ended: no result, card, appearance, ban or table change is applied; the loop's full save bytes, `MatchOutcomes.Count` and `EnginePlayedFixtures` equal those of a control loop that ran `Begin` then `Abandon` and nothing else; a new `Begin` gives the same preview, and `Kickoff` with the same config and the same command log gives the same engine digest **after N ticks** (an equal initial digest is a weak check); a full replay then commits normally. |
| T-IM-05 | SeasonSave.Tests; hand-ended engine | **Exactly once.** `Complete` before full time is refused; a second `Complete` is refused; no card or ban is applied twice. |
| T-IM-06 | SeasonSave.Tests; no match | **Command refusal.** Every operation in §2.3's enumerated list is attempted while Open, Live, Ended and Faulted and refused with its named failure mode; after an `Abandon` the loop's full save bytes equal the control twin of T-IM-04. `Kickoff` outside Open is refused. A reflection check lists the public instance methods of `SeasonLoop` and `PlayerCareerStates` and fails if one is on neither the refused list nor a read-only allowlist, so a new public mutator cannot slip past the rule. `DisciplineRules` writers are outside the lock (§2.3). |
| T-IM-07 | SeasonSave.Tests / ClientApp.Tests; one real match for the last-tick case | **Observation gap and fault.** (a) Ticking the engine without `ObserveTick` makes `Complete` fail loud. (b) The same for a gap at the **first** tick and at the **last** tick, which the fold's own check cannot see (§1). (c) A fault injected into the season consumer at tick N: `IsObservationFaulted` reads true before tick N+1, `Complete` is refused with the round left Ended, `Abandon` succeeds, and the state equals the control twin of T-IM-04. |
| T-IM-08 | ClientApp.Tests; no full match | **Participation.** With a substitution after kickoff, the substituted-on player gets an appearance and counts as fielded for #44, and a player substituted off or sent off stays in the set; with none, the set equals the starting eleven. A `Substitute` command at `CurrentTick == 0` is refused at the drain and logged, and leaves the engine digest equal to the no-command run. The record exists with a career wired and discipline unwired (fold null). |
| T-IM-09 | (a) MatchClientCore.Tests, (b) SeasonSave.Tests; no match | **Bench eligibility.** On a managed club whose composed bench holds a reinstated-suspended and a reinstated-injured player (built as in `AvailabilityCompositionExtremisTests`): (a) a `Substitute` command naming either is refused at the drain with `BenchPlayerUnavailable`, is logged, and leaves the engine digest equal to the no-command run; (b) after `Complete` the suspended player's ban has advanced, and `Complete` fails loud when the participation set is made to contain an ineligible player. The set is empty when no back-fill fires. |
| T-IM-10 | ClientApp.Tests; engine at tick 0 | **Kickoff boot path.** `Kickoff` with a non-default managed tactic: that tactic is the managed team's active tactic before the first tick, equal to PM-1's `MatchSession` boot with the same squads, tactic and manager configuration, and no command is logged for it. Under Q1 (a), the opponent is `ManagerMode.AI` with its kickoff preset applied before the first tick on both the headless and interactive paths; under Q1 (b), it is `ManagerMode.Human` at Balanced. |
| T-IM-11 | MatchClientCore.Tests; a few hundred ticks | **Observer isolation, both directions.** An `After` consumer that throws at tick N leaves the `First` (season) consumer observing every tick, and `Complete` succeeds. A `First` consumer that throws at tick N leaves analytics observing every later tick, latches that consumer's fault, and `Complete` fails loud on the observation gap. In neither case is `LiveMatchStreamer.PostTickObserverFault` set. A second `First` registration and any registration after start are refused. |
| T-IM-12 | ClientApp.Tests; 1 real paced match | **Quiescence.** A real `Start()` session runs to full time. `Complete` is attempted by the season thread (a) while the final tick's observation is held open by a test hook in the handle, and is refused, losing no card; (b) before `Stop()` has returned, per the contract; and succeeds only after `Stop()`. After `Complete` or `Abandon`, a late `ObserveTick` throws the named F-row, the composite catches it, and no unhandled exception reaches the pacing thread. |
| T-IM-13 | SeasonSave.Tests; hand-ended engine | **Apply-only and Faulted.** (a) A roster-integrity fault (F6 or F9) in a non-managed club's squad is raised at `Begin`, with nothing applied and the round Closed. (b) A fault injected after the first write (the `IFixtureDisciplineDriver` seam, as `SeasonLoopDisciplineTests` does) leaves the round Faulted: `Abandon`, commands and `Save` are refused, `FaultCause` is set, and `EnginePlayedFixtures`/`MatchOutcomes` are as the partial apply left them. (c) A precondition refusal is not Faulted and `Abandon` then succeeds. |
| T-IM-14 | SeasonSave.Tests, ClientApp.Tests; no match | **No save while a round is open.** `SeasonSaveManager.Save(loop, …)` refuses in Open, Live, Ended and Faulted, whatever `matchOrNull` holds. `CareerSession.SaveTo` refuses and `NextAction` is `MatchdayInProgress` in those states, so `PlayMatchday` is not offered; `AdvanceRound` through the season dispatcher is refused. A career-attached `MatchSession.CaptureSave` throws, and the engine is reachable only once through `TakeEngineForSession()`. |
| T-IM-15 | ClientApp.Tests; 2 real matches | **Abandon → save → load → replay.** `Begin`, `Kickoff`, some ticks, `Abandon`, `CareerSession.SaveTo`, load, `Begin`, `Kickoff` with the same config: the engine boots byte-identically to an uninterrupted `Begin`/`Kickoff`, and the committed round and full save bytes equal the uninterrupted run's. The loaded state's cursors are within the clock band (§1). |
| T-IM-16 | SeasonSave.Tests; 1 real match | **Pre-change anchor.** For fixed seeds, values captured by running the synchronous `AdvanceAndPlayNextRound` at the head immediately **before** the code landing (the `LeagueBootstrapGoldenVectorTests` convention; the head SHA is recorded in the landing) are pinned: the managed engine's final digest, every `MatchResult`, the full save bytes' SHA-256, `MatchOutcomes.Count`, `EnginePlayedFixtures`, ban and appearance state. The refactored synchronous path must reproduce them under Q1 (b); under Q1 (a) the managed-engine-dependent values change and are re-pinned with evidence, and the rest still match. |
| T-IM-17 | ClientApp.Tests; a few thousand ticks | **Hot-path allocation.** Over a window of ticks with no card and no substitution, the handle's `ObserveTick` pass allocates 0 bytes, measured with `GC.GetAllocatedBytesForCurrentThread` against a control, the form `MatchClientDriverTests` already uses. If it does not hold, the §4 item 11 sentence is withdrawn rather than the lock loosened. |

## 6. What this does not do

- No mid-match save, no persisted command log, no change to the season save format.
- No change to round order, non-managed results, the quick-sim model, engine source or any `[GT]`.
  Engine *configuration* on the career path changes only as Q1 decides (§3).
- No forfeit, instant finish or kickoff autosave. Quick-resolve for the managed fixture is Q2, not
  decided here.
- No starting-XI selection.
- No engine-side substitution eligibility (§2.7) and no change to the streamer's single post-tick slot
  (§2.8).
- No interactive route for `QuickSimAll` or `FullEngine` (§2.4): both keep the synchronous path.
- No change to PM-1's `MatchSession(MatchSetup)` path.
- No #21 or #26 rule change; §4 names them as affected callers only.
- No #38 screen-spec change beyond the client-design navigation moves and the Appendix C rows of §2.11.

## 7. Cost and sequencing

About "L" for career item 2, at the upper end: one #30 amendment without a format change, the
`BeginRound`/`CompleteRound` split and `BootFixtureEngine` compose/configure split, a `season-save.asmdef`
reference to `TacticalInstructions`, the participation record and round plan, a `match-client-core`
entry point with the per-tick composite, the drain-side checks and the pre-change golden capture, #41
and #44 errata, the `CareerSession` operations and `NextAction` value, the #38 Appendix C entry, and the
client route. It runs in the career lane; `src/match-engine/` should need no change (if it does during
the W8 B→C window, it carries digest-neutrality evidence or waits, per the roadmap). Host: compile plus
filtered EditMode for the `SeasonSave`, `MatchClientCore`, `ClientApp`, `Discipline` and
`InjuriesMedical` test assemblies (D-01a items 1–2); no whole-MatchEngine run unless engine source
changes.

**Lock cost.** Counting each lock home and away, §5 needs 18 real 90-minute matches (T-IM-01: 4, 02: 4,
15: 4, 16: 2, 12: 2, 07's last-tick case: 2), about 36 minutes of compute at the roadmap's two minutes
a match. Every other lock runs on a fresh or hand-ended engine in milliseconds to seconds. Whether the
real-match locks sit in the per-PR gate or a declared compute slot is a code-landing decision recorded
with the landing; no suite in `season-save` plays a full match today (its tests say so), so these are
new cost and are placed in `ClientApp.Tests` except T-IM-16 and T-IM-07's last-tick case.

## 8. Path to approval

1. Owner answers Q1 and Q2 (§3).
2. #41 owner accepts or amends §2.5's appearance rule; #30, #44 and #41 owners accept or amend §2.7.
3. Adversarial review of this draft to convergence (the spec-promotion / W8 C precedent).
4. Owner approval of the wording.
5. Application landing: #30 sections 1–5 and appendices as itemised in §4 under one ERR-030 entry, #41
   and #44 errata (including the closure of ERR-044-010), the #26/#21 check, the #38 Appendix C entry,
   ERR entries allocated against that head (`spec-error-log.md`), client-design §5-P7 updated. Code
   follows in its own landing, preceded by the T-IM-16 capture.

## Review history

| Version | Date | Change |
|---|---|---|
| 0.4 | 2026-11-03 | AR-1 Medium/Low fixes (M1–M10, L1–L3). **M1–M2 (round integrity):** new §2.9 thread contract (Complete/Abandon on the season thread after `MatchSession.Stop()` joins, `MatchEnded` read after the join, handle lock and invalidation, one engine in flight on the process-static bus); new §2.4.1 makes Complete apply-only, with every fallible per-fixture resolution precomputed at `BeginRound`, a Faulted state (Abandon refused, cause surfaced), and `FullEngine` refused for the interactive route; T-IM-12, T-IM-13. **M3–M4 (one implementation, participation):** §2.2 splits `PlayNextRound` into internal `BeginRound`/`CompleteRound` for every mode, with `BeginManagedFixture` as a narrower wrapper; T-IM-16 pins pre-change values; §2.5 defines participation (boot occupants plus incoming substitutes), moves it to a #30-owned `ParticipationRecord` fed by the handle's single pass, and §2.6 refuses pre-kickoff substitutions at the drain; "not a second walker" withdrawn. **M5–M6 (saves, #44):** §2.10 makes `Save(SeasonLoop, …)` refuse while a round is open, replaces `handle.Engine` with one-shot `TakeEngineForSession()`, refuses `CaptureSave` on a career-attached session, specifies the `CareerSession` operations, `MatchdayInProgress` and the post-abandon Closed state (T-IM-14, T-IM-15); §2.5's #44 item restated as conformance to FR-DC-011 / #44 §4.5 that closes ERR-044-010, with only #41 sign-off kept. **M7–M8 (normative sites, routes):** §4 names each #30 site with its replacement sentence under one ERR-030 entry and maps the locks to a T-SN-IM block in §5.9/§5.8; new §2.11 route table, D-P7-3 restated with the bye and unemployed fallback, #38 Appendix C rows; new owner question Q2 (quick-resolve for the managed fixture) with a recommendation. **M9–M10 (faults, refusals):** the handle exposes `IsObservationFaulted`, the client polls it and offers only abandon at once (§2.8); T-IM-07 extended to injected faults and first/last-tick gaps; §2.3 derives the refused-operation list from the real public surface, with a reflection check in T-IM-06. **L1–L3 (precision):** §1 corrected (day-steps include slot 1, the quick-sim row is mode-specific, "sim thread" split into season thread and tick thread, `CareerSaveSlots.cs:217` is the operative site, the fold's check scope, the new tick counter); locks compare full save bytes and counters, state their host assembly and real-match cost (§5, §7); §4 item 11 amends the §4.3 hot-path note with T-IM-17, and §2.1 aligns save wording (manual save on Career Home, autosave after Complete). |
| 0.3 | 2026-11-03 | AR-1 High fixes (H1–H3). **H1:** §2.5's "a suspended player is never on the bench" was false (the ERR-030-044..046 back-fill reinstates suspended and injured players onto the bench); new §2.7 makes back-filled bench players ineligible to enter (option (a); (b) recorded and rejected), refused at the `match-client-core` drain with a season-side `Complete` backstop and an ERR-030 obligation; T-IM-09. **H2:** `Begin` no longer boots the engine; it computes a pre-kickoff preview, and `Kickoff(PreKickoffConfig)` boots through a compose/configure split of `BootFixtureEngine`, applying the managed tactic on the #26 FR-TP-004 boot path instead of a "tick-0" command; §2.3 states and T-IM-01/04/06 aligned; #26/#21 named in §4/§6; new owner question Q1 (career opponent AI manager); T-IM-10. **H3:** new §2.8 composite per-tick observer owned by `MatchSession` with per-consumer fault isolation, season observation first; T-IM-11. §1 gains the facts each fix rests on. |
| 0.2 | 2026-11-02 | Owner decisions applied: no starting-XI changes (v0.1 Q1, answer (a)); no mid-match saves; quitting a match abandons it and returns to Career Home with the round unplayed; no kickoff autosave (v0.1 Q2/Q3 withdrawn). Removed: the managed-fixture sub-blob, season-save format 8 → 9, restore path and live-save locks, and the managed-first round order (index order kept; headless path unchanged). Added: `AbandonManagedFixture`, the abandon lock, save unavailability while a round is open. Size back to about "L". Not yet adversarially reviewed. |
| 0.1 | 2026-11-02 | Initial draft, from the November 2 review of the client-design bullet contract: substitutions vs starting-XI consumers, unserialized card fold, engine/observer ownership across assemblies, round order and states, save-policy owner questions. |
