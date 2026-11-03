# D-P7-6 — Interactive career matchday contract

**Created:** November 3, 2026\
**Version:** 0.4\
**Status:** SUPERSEDED — the v0.1–v0.3 resume proposal below is withdrawn\
**Purpose:** make roadmap D-11's interactive managed fixture and unfinished-matchday resume concrete.\
**Authority:** [client design §5-P7](interactive-unity-client-design.md#p7--career-management-screens-roadmap-c3c4--host-free-decisions-landed-2026-10-18), Remaining item 2; #30 and its collaborators retain domain authority.

**Current session proposal:** [Delegated match completion and decision register](d-p7-6-delegated-completion-proposal.md).
The historical body below is not the active requirement set.

## Remote-main correction — governing scope

Live GitHub main was checked on November 3, 2026 at
`1d3cebf3b8d874715158067d9deb5fa48b309a30` (PR #536), 21 commits ahead of this working tree's
`32ff375f` baseline. Earlier checks used the stale local checkout and missed the owner's amendment.

Use [the remote D-P7-6 amendment draft v0.6](https://github.com/antonzymin-eng/Soccer-Manager-Pro/blob/1d3cebf3b8d874715158067d9deb5fa48b309a30/docs/tracking/d-p7-6-interactive-career-match-amendment-draft.md).
It is PROPOSED, not approved. Its §2.1 and §6 explicitly remove mid-match saves, a persisted command
log, a managed-fixture save block, a season-save format bump, a restore path and live-save locks.
Quitting abandons the match and returns to Career Home with the round unplayed. Matchday-morning
steps remain run and are cursor-idempotent; abandonment is not a rollback of every pre-matchday block.
The automatic starting XI stands.

**The earlier C-01–C-12 choices below are historical and MUST NOT be implemented as the current contract.**
The local code work is preserved; no merge, rebase, reset or runtime change is performed by this correction.

| Earlier item | Current remote-main disposition |
|---|---|
| C-01 shared match setup | Still relevant; align fatigue/tactics through season-owned boot and attached session. Opponent AI is remote Q1, not decided here. |
| C-02 total preparation rollback | Replace with remote Open/Live/Closed/ Faulted and abandon semantics; morning steps stay run. |
| C-03 persisted suspended round | Remove; round state is in-memory and unsaveable while open/live/faulted. |
| C-04 save/input capture fence | Remove from career scope; retain normal live command marshaling and quiescent Complete/Abandon. |
| C-05 persisted card/substitution fold | Remove persistence; retain live season-owned per-tick observation, participation and complete coverage/fault checks. |
| C-06 restored boot identities | Remove restore requirement; retain correct live boot/current identity and managed-team command ownership. |
| C-07 resumed report accumulators/history | Remove; retain live analytics/report health and per-consumer observer fault isolation. |
| C-08 staged commit/automatic rollback | Replace with remote BeginRound/CompleteRound plan and Faulted-after-write handling; not the generic rollback policy previously proposed here. |
| C-09 new continuation/save layout | Remove new blocks/version bump; retain save refusal while a round is open and existing Home-only save compatibility checks. |
| C-10 mid-match save-and-quit | Replace with AbandonManagedFixture, joined playback, discard uncommitted match effects, return Home. No forfeit or instant finish. |
| C-11 composed resume locks | Remove live-save/resume cases; use remote T-IM locks for parity, replay, participation, observer faults, abandonment, no-save enforcement and pre-change anchors. |
| C-12 parallel approval plan | Superseded by remote §8, including Q1 opponent AI, Q2 quick-resolve, Q3 injured bench eligibility and owner acceptance of appearance/suspended-bench rules. |

The remote appearance/ban-serving proposal now includes incoming substitutes in participation.
The earlier requirement to keep starting-XI-only appearance inputs must not be carried forward.
Remote interactive scope is ManagedThroughEngine; QuickSimAll/FullEngine keep their synchronous paths.

## User clarifications after the remote-main correction

These session instructions supersede conflicting local proposal wording. They are not a claim that
remote main has already been amended or that runtime support exists.

- **Injury eligibility:** starting or entering must depend on the injury and the owning medical rule;
  do not interpret remote Q3(a) as blanket permission for all injured players. Today
  `MedicalStep.IsAvailable` admits only `Severity.None`; the depleted-squad back-fill is an exception,
  not a complete injury-dependent eligibility policy. The exact injury/eligibility mapping still needs
  the #41 contract; the UI must consume it rather than invent thresholds.
- **Save scope:** no saving within an active match. Do not describe this as a whole-calendar-matchday
  prohibition. Home-only manual saving before/after a match on the same world day remains supported.
  Internal save refusals for transient Open/Live/Faulted round states require precise lifecycle reasons;
  they must not be inferred merely from the calendar being a fixture day.
- **Live records versus persistence:** remove mid-match serialization requirements, not the live
  discipline and participation tracking needed to commit suspensions, appearances and medical match
  load correctly. Observers still need to run in both interactive play and accelerated continuation.
- **Simulate remainder (proposal to develop):** at an acknowledged between-ticks boundary, stop
  accepting player commands, resolve already accepted inputs exactly once, transfer exclusive tick
  ownership, and continue the same engine from its current tick/score/cards/substitutions to full time
  off the UI thread. Preserve the fold, participation and analytics consumers and commit through the
  normal round finalizer once. Do not restart the fixture or substitute a fresh round-model result.
  The same match may continue at accelerated pacing or through a new reviewed unpaced handoff;
  `MatchSession.TickOnce` currently refuses once `Start` has ever run, even after `Stop`, so a naive
  Stop-then-TickOnce loop is not a supported implementation. The user has now chosen delegated control: select the assistant manager or another eligible club
  coach to manage the remaining match (see the requirement below). Keeping current tactics without a
  selected delegate is not a substitute for that requested behavior.
- **Exit and controller decisions confirmed:** the user chose adaptive AI opponents, delegation final
  for the match, and normal Exit offering coach selection then completing/saving before the process
  exits. Picker cancellation before confirmation keeps player control. Forced termination still
  recovers only the previous durable save. See the active delegated-completion decision register.

## Delegated simulate-remainder requirement — user direction, November 3

**Required product behavior:** while a match is live or paused, the player can choose **Simulate rest
of match**, select their assistant manager or another eligible coach employed by their club, and
confirm handing control to that person. Continue the same match from the current tick/score with all
existing player state, tactics, fatigue, cards and substitutions intact. The chosen staff member then
manages the remainder under their defined decision policy. Finish off the UI thread, publish the normal
report and commit the round exactly once. This is accelerated continuation of the existing engine,
not the round-model quick result, a new fixture, a forfeit or abandonment.

This is a user scope instruction, not evidence that the APIs exist. It supersedes the local suggestion
of merely retaining tactics without a chosen delegate. Remote main's abandon-on-quit design and Q2's
pre-kickoff quick-resolve choices must be revised separately; this instruction does not decide whether
pre-kickoff quick-resolve is offered. Normal application exit during background completion is still an
explicit policy question; forced process termination does not gain resume support.

### Required contracts and owners

| Area | Required change / evidence |
|---|---|
| Staff selection — #34 / ClientApp | Supply real eligible club staff identities and assistant/coach assignments. Show the assistant manager and other eligible coaches; validate club membership, role and availability again on confirmation. No arbitrary archetype may be presented as a named staff member. If no eligible delegate exists, explain why the action is unavailable. |
| Staff decision policy — #34 / #26 | Define a deterministic, reviewed mapping from the selected person's supported attributes/tactical tendencies to takeover behavior, including tactical decisions and substitutions under the injury/suspension/window/cap rules. Do not invent attribute effects or numeric mapping in the binding. Record staff identity and resolved configuration at the handoff for replay verification. |
| Safe takeover — MatchClientCore / #30 / #26 | Stop accepting player commands at an acknowledged between-ticks fence; settle already accepted commands exactly once. Install the selected delegate on the managed team only and transfer exclusive tick ownership to the supported accelerated/unpaced continuation. Preserve current engine state and the existing tactic until the delegate legitimately changes it. Do not rerun kickoff adaptation or reset score, fatigue, lineup, cards, used substitutions or match time. |
| Same observer chain — #30 / #44 / #37 | Continue season participation/card observation and analytics for every tick exactly as during watched play; retain health/fault handling. Acceleration must not skip observers or use a new engine. No mid-match serialization is required. |
| Completion — ClientApp / #30 | Show background progress; suppress duplicate takeover requests and concurrent player changes; retain the selected delegate identity for the running operation. Complete through the common season finalizer once, then show the report/Home route. |

### Repository gaps to implement before exposing the picker

- No staff/backroom production assembly or real assistant/coach roster exists in the inspected `src/`.
  The #34 minimal specification has Coach/Scout/Physio roles and role slots, not an implemented
  assistant-manager selection surface. Add the owning staff slice and role/assignment contract first;
  do not create a UI-only fabricated staff roster or silently treat the whole #34 deep tier as required.
- Current `ManagerProfile` is an aggression/caution/patience archetype, not a real StaffRecord mapping.
  `ManagerAdaptation` supplies tactical adaptation, not a complete assistant/coach substitution policy.
- `MatchEngine.ConfigureManager` is intended for kickoff. A live call is deterministic but resets manager
  state to a Balanced preset marker and an unstarted decision clock. A reviewed takeover seam must
  initialize that metadata consistently with the actual current tactics/time; blindly calling it and
  `ApplyKickoff` is not a valid continuation contract.
- `MatchSession.TickOnce` refuses after paced `Start`, even after Stop. Provide a supported handoff or
  accelerated execution mode, rather than bypassing that ownership guard.

### Acceptance evidence

1. The chosen assistant/coach is a real eligible member of the managed club; invalid/stale/foreign staff
   selection is refused before any handoff or simulation mutation.
2. At handoff, current tick, score, rosters, injuries/fatigue, tactics, cards, queued changes and used
   substitutions match the pre-handoff state except for explicitly settled accepted commands and the
   new delegation metadata. No kickoff reset or second engine is created.
3. Identical pre-handoff state, staff identity/configuration and accepted-input boundary yield identical
   remainder result/digest regardless of display cadence or acceleration. Cover both home/away teams,
   a paused match, a pending substitution, and takeover near full time.
4. Delegate tactical/substitution decisions conform to their defined policy and existing eligibility
   rules, with no commands applied to the opponent. Selection affects only supported documented inputs;
   no test may claim differing staff behavior until the mapping supplies a real difference.
5. Cards, participation and final report equal a watched control using the same delegated decision/input
   sequence. The result, table, appearances and discipline commit once; double clicks, stale callbacks,
   teardown and observer faults do not create a second commit or silently truncate records.

## Historical local proposal — withdrawn

## 1. Evidence and boundaries

Inspected baseline: `32ff375f7b2b74ff5e2c95e4c19aedb1d737efce` (November 3, 2026).

- `SeasonLoop.PlayNextRound` runs fixture-day career steps before resolving fixtures in
  `UnplayedFixtureIndicesInRound` order, then records appearances, applies/emits/marks each result,
  serves bans/commits the card fold, and advances the calendar. Preserve the actual source order.
- `SeasonLoop.ActiveMatch` is sim-thread-only and exists inside synchronous resolution. It cannot
  be captured safely by a concurrent UI read.
- `SeasonLoop.PlayThroughEngine` seeds `CardLedgerFold` before kickoff and observes every completed
  tick. Its buffered cards are **not persisted**; the source explicitly warns that mid-match restore
  would lose them. Engine bytes alone cannot meet D-11.
- `SeasonSaveCodec` already frames an optional match blob, while `CareerSession.Resume` refuses it.
  Presence of that blob does not persist the suspended round continuation.
- `MatchSession` owns the existing command queue, tick-boundary servicing and replay log. Its
  normal boot and #30's `BootFixtureEngine` must not construct two independent managed matches.
- `season-save.asmdef` references MatchEngine, never MatchClientCore/ClientApp/UiFramework.
  Domain-to-presentation dependencies are forbidden; presentation may depend on public domain seams.

**Current save scope versus proposal.** No player-facing in-match career saving exists:
`CareerSaveSlots.Write` passes `null` as the match to `SeasonSaveManager.Save`,
`CareerSession.Resume` refuses `contents.Match != null`, and the current UGUI only offers manual saves
on Career Home. Roadmap D-11 and the committed client design still request unfinished-matchday resume;
this draft describes the missing future capability, not current behavior or an approved save control.
No removal of that requested scope was found in the inspected checkout. If the intended product rule
is now "save only between matches", D-11/§5-P7 must change first; that decision cannot be inferred from
the current implementation's inability to save during a match.

This draft changes no approved specification, format constant, engine behavior or navigation edge.
API names below describe responsibilities, not granted public symbols.

## 2. Ownership and state transitions (proposed)

#30 owns one round continuation and authoritative engine boot/configuration; MatchClientCore owns
interactive playback/commands over that same engine through a reviewed composition seam.
**Existing boot paths are not interchangeable:** #30's `BootFixtureEngine` filters availability and
passes match-entry fatigue into four-argument `ConfigureSquads`. `MatchSession.BootEngine` uses the
two-argument overload, configures optional managers and invokes `ManagerAdaptation.ApplyKickoff`.
Simply constructing a new `MatchSession(MatchSetup)` from the league squads can lose fatigue or change
kickoff behavior. A common boot/composition seam must preserve these inputs exactly. Wrapping #30's
engine is not publicly supported today (`MatchSession(engine)` is private); it requires reviewed API
work, not a call to an existing public constructor. The binding
owns neither domain state nor a second tick loop. One owner executes mutations at a time.

| State | Permitted action and next state | Refusal / invariant |
|---|---|---|
| Between rounds | On the fixture day, prepare the managed fixture → Preparation | Validate all round/config guards before mutation; no new matchday begins while another is pending |
| Preparation | Confirm setup → Playing; cancel → Career Home | Preparation is a draft; cancel discards it without applying day steps, consuming fixture draws or committing a result |
| Playing | Pause/resume; capture; reach full time → Awaiting commit | One engine/tick owner; no AdvanceDay, second round, roll, roster mutation or second playback owner |
| Awaiting commit | Commit authoritative result and remaining round work → Report | Full-time save must retain an uncommitted result; a second completion request cannot apply it twice |
| Report | Explicit acknowledgement → Career Home | Result already committed; acknowledgement only navigates and cannot apply effects again |
| Suspended save | Load/Continue → restored Playing or Awaiting commit | Restore exact pending identity and phase; never create a fresh fixture or replay committed effects |
| Failed | Recover from last coherent checkpoint | No blind retry over partially applied effects; keep prior durable save and show a persistent error |

Preparation must account for matchday-morning availability before confirming the XI. The current
`RunCareerDaySteps` is already cursor-idempotent, but it mutates progression/training/medical state;
that is not a transactional preview or cancellation rollback. The no-mutation cancellation row above
is a **proposed constraint**, not current behavior. This revision proposes full private staging under C-02 below; there is no established cloning API
being claimed. The implementation must prove the staged copy includes every affected collaborator
before preparation is exposed. A different cancellation policy requires an explicit contract revision.
Auto-lineup and the existing admitted team-tactic choices remain the baseline; this contract adds no
new squad-selection or pre-match player-tactic promise.

## 3. Fixture order, result commitment and failure

1. Freeze the round's fixture order, world day, managed fixture index, resolution mode and configuration.
   Resolve any prefix before the managed fixture in exactly the existing order. Do not move the managed
   fixture to the front for convenience. Do not resolve the suffix until managed completion.
2. Persist the committed prefix plus the pending managed fixture and continuation cursor together.
   Pausing, saving and resuming consume no additional fixture or day-step draws. On resume skip the
   prefix and retain its resulting domain state; do not regenerate its outcomes. Do not invent a new
   shared fixture RNG cursor: `RoundResolutionModel.MatchSeedFor` derives a seed from fixture identity,
   season seed and season number. Preserve existing deterministic derivation and each owning system's
   persisted state; ordering still matters for side effects and publication.
3. Route synchronous and interactive completion through one #30 finalization implementation.
   Preserve appearance recording, table → event → played marker, ban serving/card commitment,
   remaining fixtures and the final `AdvanceCursorOneRound`. Daily finance and progression run at
   their existing day/boundary seams; they are not extra effects to replay on a report acknowledgement.
   This shared finalizer is a required extraction: today the block is embedded in `PlayNextRound`,
   with no public submit-result or commit-once API. The UI submits intent to
   finish, never a caller-supplied score or a second hand-authored finalizer.
4. Bind completion to season, round, fixture and operation identity. Stale/wrong-result callbacks
   refuse; repeated acknowledgement of a completed identity returns the existing outcome without
   side effects. Lifetime callback identity is separate from durable continuation identity.
5. Stage fallible work before publishing, or checkpoint enough state to restore every collaborator
   before retry. A prefix or partial finalizer may never become externally visible as a complete round.
   Do not claim atomicity merely because a method is guarded by `IsBusy`.
6. If completion succeeds but autosave fails, retain the committed in-memory career, show the failure,
   and retry saving those bytes; never rerun the fixture to retry a write.

## 4. Safe capture and persistence review

For the proposed mid-match career path, reuse `MatchSession.CaptureSave`/`ServiceOnce` for the engine
side of capture: a serviced boundary already exists for a standalone match, including paused/full-time
states. It does not currently capture the surrounding season continuation or observer state atomically.
The existing servicing routine drains commands **before** engine capture. A new coordinated barrier must
also capture the season continuation and required observers without another tick intervening.

**Command history correction.** `MatchSession.RestoreFrom` deliberately starts with an empty queue and
an empty in-memory log; pre-save applied effects are in the engine snapshot. A full pre-save command
journal on disk is not required for continuation, and journal/rewind remains deferred by §11. Preserve
that policy unless separately approved. Freeze command acceptance at the capture fence or explicitly
account for post-drain arrivals: the same tick number alone cannot distinguish a command already in
snapshot state from one arriving later at that tick (`CaptureSave` remarks / `TickStampedCommandReplay`).
Replay verification can supply recorded test inputs independently of the save-file layout. No Unity API or season live-state read runs on the UI thread while the
worker is mutating it. Publish immutable projections only after completion.

The persistence design must account for all of the following, not merely the optional match blob:

| Continuation data | Why it is needed |
|---|---|
| Season/round/fixture identity, phase, resolution mode and remaining cursor | Prevent duplicate prefix/suffix resolution and wrong-fixture completion |
| Day-step completion and the post-prefix world/domain state | Prevent duplicate training/medical/day accounting and random draws |
| Canonical engine save plus setup/manager/tactic/roster identity | Resume the actual played fixture, preserving no-input parity |
| Command acceptance/capture fence and unconsumed-input policy | Applied effects are already in the engine save; close or account for later arrivals without requiring a full persisted command history |
| Card fold pending attributions, occupancy/bench-origin mapping and observation/commit status | Preserve pre-save cards/substitutions and lossless tick consumption; review `_hasObservedTick`, `_lastObservedTick`, `_faulted`, `_committed` as well as `_pending`; engine bytes do not serialize this observer |
| Appearance identity and original fielded XI/unfiltered roster context required by finalization | Current #30 records the starting XI from its configuration site, not final occupants; do not silently change appearance/ban-serving semantics to include substitutes |
| Boot roster/presentation identity and report-observer continuity policy | `MatchSession.BootRoster` is unavailable after restoring a substituted match; the current report also depends on session analytics and command-feedback history, not just engine bytes |
| Result/commit phase and coherent completed-fixture markers | Full-time-before-commit and post-commit capture remain distinguishable |

**Layout disposition:** reuse the optional match block where it fits, but do not assert the existing
layout is sufficient. Choose and review a versioned home for the continuation and fold, and amend the
owning #30/#44 contracts atomically with implementation. Bump `SEASON_SAVE_FORMAT_VERSION` only when
its persisted layout changes; independently version nested layouts when changed. Allocate no numeric
version here. Preserve D-10: older incompatible slots are listed and never loaded; no migration promise.

The card fold must continue observing every tick once and in order, including after restoration.
Restoring only `_pending` cannot establish that invariant. Review capture immediately after a command
has changed an occupant but before its queued substitution event is observed; retain a coherent engine,
fold and pending-event boundary rather than re-seeding from current occupants.

`SeasonLoop.MatchOutcomes` is also an in-memory list; `SeasonLoop.Restore` constructs a fresh loop
without an outcome-history input. Define which current-round prefix outcomes the continuation needs
for completion/reporting; do not claim the existing save already restores that list or require an
unrelated full historical-outcomes archive.

A resumed report must not present post-resume-only analytics or feedback as complete match history.
C-07 below requires normal resume to preserve aggregate report continuity, while preserving an actual
observer failure's partial/unavailable status and cutoff. It deliberately distinguishes aggregate
statistics from optional historical command feedback; neither raw-event archival nor a new whole-history
command journal is required.

A **proposed** Save-and-quit action would wait for durable save success and worker termination before
navigation/disposal. It does not exist in the current match UI and is not the existing Home action
"Return to Main Menu (without saving)". The exact player-facing affordance still needs review.
A failed write keeps the session available for retry. Forced process termination recovers only the last
successful save; D-11 does not promise persistence of unsaved ticks. Invalid pending identity, missing
fold/continuation, unsupported versions or incoherent engine/fixture state refuse before replacing the
active career. Remove the current match-block refusal only with complete validated restore support.

## 5. Navigation and P7b separation

After approval **and implementation**, add named career preparation/return moves to `ClientScreenFlow`.
They must retain career provenance through Tactics → Match → Report; the PM-1 report still returns to
Main Menu. Do not reuse `ReturnToMainMenu` for a career return. Continue of an unfinished matchday routes
to the restored match state, rather than calling `AdvanceAndPlayNextRound` anew.

Independent P7b proceeds now with the existing eight career moves and sources. Seven-screen visibility
and seven-root validation now accompany a worker-backed coordinator and optional shared-shell
composition. An opt-in UGUI adapter now consumes those snapshots; actual scene activation, pinned
Unity compile/runtime and copy/scale/focus acceptance remain due. Until this contract lands, current matchday resolution stays on
Career Home and both new matchday edges remain absent.

## 6. Acceptance locks to implement

| Lock | Required comparison / adverse case |
|---|---|
| No-input parity | Same runtime, configuration, seed, rosters, tactics and managers: interactive vs synchronous ManagedThroughEngine result **and digest** equal; managed club both home/away and early/late in fixture order |
| Input replay | Same initial state + identical ordered tick-stamped command log → same result/digest, including rejected commands and same-tick ordering |
| Season commit equivalence | Same authoritative managed result → identical complete post-round season state/snapshot; appearances, discipline, finance/progression, outcomes and cursor applied once |
| Resume | Uninterrupted vs save/load with identical accepted inputs, at kickoff, paused tick, after substitution/card, before full-time commit and after commit; domain bytes, final result/digest and pending state agree |
| Failure/retry | Inject boot, capture, decode, finalize and disk-write failures; no lost card, duplicated ban serving, consumed prefix draw, partial replacement or duplicate commit |
| Lifecycle | Double click, stale completion callback, navigation during work, teardown with work pending, repeated report acknowledgement and loaded incompatible/malformed continuation |
| Engine isolation | No match-engine edit during W8 B→C unless digest-neutrality is demonstrated; otherwise defer it |

## 7. Review and implementation sequence

- Review this lifecycle, preparation staging and persistence layout with #30/#44/client owners;
  settle composition without a reverse assembly edge. Record approval explicitly in §5-P7.
- Back-propagate owning spec amendments, then implement the suspended-round seam and shared finalizer.
- Add continuation/fold capture/restore, input boundary locks and failure tests, then remove match refusal.
- Add career navigation and interactive bindings; run the acceptance matrix and pinned Unity validation.
- Apply the prepared S1 Gate-A delta after approval; design/validate the interactive route under S1's
  own two-participant rules. No source inspection or prototype substitutes for host evidence.

## 8. Repository cross-check — November 3, 2026

Read-only source audit against HEAD `32ff375f7b2b74ff5e2c95e4c19aedb1d737efce` plus the current
uncommitted P7b working tree. This is a contract consistency review, not runtime validation or approval.

| Technical item | Repository evidence | Disposition |
|---|---|---|
| One matchday owner / ordered fixtures | `SeasonLoop.PlayNextRound`, `UnplayedFixtureIndicesInRound`; `ActiveMatch` is synchronous/sim-thread-only | KEEP; suspended ownership/continuation API is absent |
| Interactive managed fixture | `BootFixtureEngine` versus `MatchSession.BootEngine` use different fatigue/manager setup; no public adopt-engine session constructor | KEEP; require common boot parity before wiring, not a new independent match |
| Safe capture | `MatchSession.CaptureSave`, `ServiceOnSimThread`, `RestoreFrom` already support standalone match state | REUSE; extend the boundary to season/fold/observers rather than claim the existing seam is wholly absent |
| Mid-match career save/resume | `CareerSaveSlots.Write` saves a null match; `CareerSession.Resume` rejects a match; `SeasonLoop.Restore` has no pending-round input | FUTURE ONLY; still requested by D-11, not current functionality or a reviewed UI action |
| Command persistence | `MatchSession.RestoreFrom` documents a fresh queue/log; client design §6.3/§11 defer journal/rewind | CORRECT; retain effects + command fence, not mandatory full-history serialization |
| Cards/substitutions/identity | `CardLedgerFold` has pending/occupancy/tick/fault/commit state but no save API; `BootRoster` refuses after substituted restore | KEEP; expand observer/identity inventory and settle partial/full report behavior |
| Commit each result once | `PlayNextRound` contains appearance → table → outcome → played → serve bans → cards, then cursor advance | KEEP; extract common code, preserve order, and design recovery; no existing general atomic transaction/commit-id API |
| Preparation/cancel | `RunCareerDaySteps` is cursor-idempotent but mutates availability-related state before fixture boot | QUALIFY; staged cancellation is a design choice to prove, not an existing guarantee |
| Return to Career Home | `ClientScreenFlow` deliberately omits both career matchday edges; `ClientMatchCoordinator` owns PM-1 return | KEEP; add named provenance-aware moves only with the owning seam |
| Determinism tests / engine isolation | `MatchSession`/driver replay surfaces; keyed `MatchSeedFor`; roadmap W8 B→C restriction | KEEP; compare identical boot configurations and inputs; add resume/commit locks without claiming they already run |

**Result at v0.2:** the overall proposed work remained relevant but needed explicit choices for
preparation rollback, boot integration, versioned continuation, capture fences, report completeness
and the future save/quit affordance. Revision v0.3 proposes those choices as C-01–C-12 below; they
remain unimplemented and subject to contract approval. The implemented inability to save in-match does not itself remove D-11's proposed
future scope. No implementation or product decision was changed by this audit.

## 9. Revised implementation obligations — proposed v0.3

These requirements replace the earlier "choose a policy" gaps where stated. **Every item is OPEN**:
they define required work and its completion evidence, not existing capabilities. Ownership names refer
to repository systems, not new human assignments. Exact API signatures, layout fields and numeric
version allocations must be reviewed in the owning contracts before implementation.

### C-01 — One canonical match setup and engine

**Owners:** SeasonSave #30 + MatchClientCore; existing #29/#41/#44 availability/fatigue providers and
#26/#21 remain authoritative for manager/tactic behavior.

The season owner must produce one frozen fixture configuration after availability and matchday
preparation: fixture/club identity, `MatchSeedFor` seed, actual filtered roster and selected XI/bench,
original unfiltered roster IDs, per-player entry fatigue, initial team tactics, manager modes/profiles
and existing engine feature flags. Fatigue is `0 = rested, 1 = exhausted`; preserve the existing values
and player mapping, including any reindexing caused by filtering. Never substitute a zero-fatigue
`ConfigureSquads` overload or regenerate rosters from the original league seed.

Both synchronous and interactive consumers must use that same configuration and boot implementation.
The session must adopt the resulting engine through a new reviewed composition boundary; it must not
boot a second one. Domain code must not reference presentation assemblies. Apply kickoff tactics and
manager adaptation once, in the established order. No-input parity compares identical manager modes,
profiles, tactics, flags and runtime. It does not compare a newly AI-configured interactive opponent
against today's differently configured headless baseline. Any intended manager/tactic change must be
explicitly applied to both comparison paths and isolated from the W8 B→C baseline.

**Done when:** tests inspect both teams' boot rosters, fatigue mappings and configuration with injured/
suspended players, nonzero/unequal fatigue, distinct tactics and admitted manager modes; then compare
no-input final result and digest. Repeat with the managed club home/away and early/late in fixture order.

### C-02 — Staged preparation with lossless cancellation

**Owner:** #30, composing its existing progression/training/medical/discipline/finance/transfer blocks.

Validate round/day/config guards before work. Build preparation against a complete private staged
career state; run the fixture-day steps and required prefix there, not in the publicly active career.
Show availability and auto-lineup from that same stage. Re-entering or cancelling preparation discards
the stage; the active career, durable saves, day cursors, outcomes and owning-system random state remain
unchanged. At kickoff install the validated stage once, freezing its configuration for C-01. No
parallel transfer, roster, day-advance or season-roll mutation is accepted while preparation is active.
Cursor-idempotence alone is insufficient proof of staging or rollback.

**Done when:** capture all owner blocks before entry; enter/cancel/re-enter repeatedly and compare bytes,
cursors, outcomes and subsequent deterministic progress against a never-entered control. The displayed
XI/fatigue must equal the engine's eventual boot inputs, including matchday-morning injury recovery.

### C-03 — Explicit suspended-round continuation

**Owner:** #30 / season-save persistence.

Add a versioned continuation carrying season/round/fixture identity, phase, world day, frozen mode/
configuration identity, completed prefix markers, current-round results required for completion, and
remaining fixture cursor. Preserve canonical fixture order and keyed seed derivation. On resume retain
the post-prefix state and never resolve an already completed fixture again. Represent Playing,
Awaiting commit and completed-round/report-ready states unambiguously. A save containing a match
without its required coherent continuation must refuse rather than guess a route.

**Done when:** resume with the managed fixture first, middle and last; compare remaining fixture seeds,
results, appearances, discipline, table, day state and round cursor with uninterrupted controls. Include
saves after managed full time but before commit and after complete round commit.

### C-04 — Coherent capture and input acceptance fence

**Owners:** MatchClientCore + #30; Unity only requests operations and receives immutable status.

For a career capture, close gameplay-command acceptance at a defined sequence fence, acknowledge/drain
all accepted requests to a terminal applied/refused/not-applied outcome, then service capture without
advancing an extra tick. Capture engine, continuation, identity, fold and report-observer state under
one exclusive ownership barrier. Associate every captured part with the same fixture, phase and tick.
A post-fence input must be refused as not accepted or wait until the capture completes; it cannot be
acknowledged and silently disappear. No UI-thread read of the live engine or season state is allowed.
Reopen input only after capture completes and only if the match is still eligible for input.

Restore with a fresh command queue/log as today; already-applied effects are in engine state. Full
pre-save command history and rewind remain out of scope. Test inputs may be retained by the test runner.

**Done when:** race input against capture before/after the fence and at the same tick, including pause
and full time. Each acknowledged command has exactly one terminal outcome and contributes at most once.
Captured bytes remain coherent; a paused capture neither waits for the next tick nor advances one.

### C-05 — Cards, substitutions and lossless fold continuation

**Owners:** Discipline #44 for fold state/validation; #30 for persistence and final commit; MatchClientCore
for composing all per-tick observers rather than attaching competing sole observers.

Define capture/restore for the fold's ordered pending attributions, current occupancy and bench-origin
mapping, on-pitch boundary, observation watermark, fault latch and committed status. Preserve or
canonically reconstruct each needed value with an equivalence test. Restore must not treat current
occupants as the original boot map or silently clear `_faulted`/`_committed`. A fold that cannot safely
continue must refuse the resume; never silently omit discipline effects.

Coordinate engine pending substitution events and fold observation. A substitution applied during the
capture service can have a queued event not yet observed by the fold: engine and fold must restore to
that same relative state and consume the event once on the proper next tick. The next observation must
be consecutive; skipped/duplicate ticks and double card commitment must fail. Ban serving remains before
this fixture's card commitment, in the existing finalization order.

**Done when:** save/resume before and after a yellow, second-yellow/red, applied substitution and its
pending event; compare PlayerId card attribution, occupancy, bans and tally bytes with uninterrupted
controls. Cover multiple events at one tick, repeated saves/restores, substituted players receiving
cards, and corrupt/missing/faulted/already-committed fold state.

### C-06 — Stable player identity and existing appearance semantics

**Owners:** #30 configuration; MatchClientCore presentation identity; #41/#44 finalization consumers.

Persist the original starters, bench origins and admitted player/team identity needed by
`MatchIdentityContext`, alongside current engine occupant state. Validate membership, uniqueness and
roster/configuration identity before publishing a restored career. Rebuild presentation identity from
this continuation, not a fresh `MatchSession.BootRoster` query after substitution. Display/control
current occupants using accepted frames; preserve original bench identities and used-bench state.
Keep #30's current starting-XI appearance/ban-serving semantics; expanding appearances to substitutes
requires an explicit owning-spec amendment, not an incidental UI change.

**Done when:** after substituted restore, names/shirt/team/bench identity and legal substitution choices
match the uninterrupted session; original and current occupants remain distinct. Appearance and ban
serving use the original configured inputs once. A wrong-roster or duplicate-identity save refuses.

### C-07 — Reliable report continuity with honest failure status

**Owners:** MatchAnalytics #37 owns aggregate state; MatchClientCore owns session publication and identity;
ClientApp owns the report binding and disclosures.

Normal save/resume must retain the sufficient aggregate/accumulator state and observation watermark
needed for the final report's statistics to equal uninterrupted observation. An owner-validated compact
state is sufficient; no full raw-event archive is required. Compose analytics and discipline after each
completed tick so neither steals the other's observer or misses an event. Restore a previously faulted
observer's trustworthy result, incomplete status and cutoff; never label it healthy because a new adapter
was constructed. Engine final score/end state remains authoritative even when statistics are unavailable.

If observer data is corrupt or cannot be validated, refuse that continuation instead of silently
publishing post-resume-only totals as a complete report. A genuine recorded observer failure may still
publish the existing partial/unavailable report treatment with its real coverage/cutoff. Do not add
pre-save and post-save cumulative totals unless the aggregate owner specifies an exact merge law.

Historical command feedback is separate from match statistics: pre-save command history need not be
persisted, but the resumed report must explicitly disclose its absence and present only the new session's
feedback as such. Applied/pending/refused status needed for the current state must not be fabricated.
Persist the frozen report/coverage state needed if a report-ready career save resumes without replaying
a completed match or committing it again.

**Done when:** save/resume around goals, substitutions and cards, then compare every supported final
statistic and score against uninterrupted controls. Separately test an observer fault before save,
restore after that fault, unavailable results, repeated resume, historical-feedback disclosure and a
report-ready save. A partial result must preserve its actual cutoff and never claim full-match coverage.

### C-08 — Shared, exactly-once finalization and bounded recovery

**Owner:** #30 with existing #41/#44 collaborators; ClientApp only requests finish/acknowledgement.

Extract one common finalization path for both execution modes. Use the owned completed engine result,
not a UI-supplied score. Preserve appearance → table → outcome → played marker → ban serving → cards,
then remaining fixtures and one final round-cursor advance. Daily finance/progression and season
settlement stay at their current day/boundary seams.

Before any fallible finalization block, stage the complete affected state and validate the inputs.
Publish a successful commit only after all required effects are valid. On failure retain/restore the
last coherent continuation; do not leave a partly committed state retryable through the normal progress
button. Record durable phase/identity and an idempotent commit receipt in the same coherent save.
Duplicate completion, report acknowledgement or stale callbacks return the already committed outcome
or refuse without effects. An acknowledged report navigation is not a second commit.

**Done when:** compare post-round owner-block bytes and current-round outcomes for equivalent managed
results. Inject failure before/after each effect, before/after cursor advance and across save/restore;
retry only from the coherent checkpoint and prove no double appearances, points, serving, cards or
fixture resolution. Refuse receipts belonging to another season/round/fixture.

### C-09 — Versioned envelope, atomic slot write and validated restore

**Owners:** season-save envelope and owner-specific codecs; ClientApp slot metadata and replacement.

Specify the continuation/fold/identity/aggregate/receipt layouts and cross-identity validation together.
Use the existing optional engine block where appropriate. Allocate version bumps against the actual
landing tree; update the slot sidecar's nested-format stamps so D-10 classifies incompatible saves.
Validate lengths, versions, identities, phase/tick coherence and owning-system invariants before swapping
the active career. Reuse the existing staged save/sidecar pair recovery; preserve the previous durable
pair when capture, encode, flush or swap fails. Do not weaken independent sub-blob version checks or
promise migration. Allow removal of the current match refusal only after all required blocks restore.

**Done when:** corrupt/truncate each block, mismatch nested versions or fixture identities, and interrupt
each slot-write stage. Load must preserve the previous active career on failure; reopening must recover
one coherent save/sidecar pair and Continue must never select an incompatible or invalidated save.

### C-10 — Lifecycle, routing and proposed save/quit behavior

**Owners:** ClientApp coordinator/flow + MatchClientCore worker lifetime; Unity binds their status.

Add named career preparation/return/resume moves that retain career provenance. Start restores into
Playing, Awaiting commit or Report according to the validated phase; no route reboots or reruns a saved
fixture. Retain PM-1's separate report-to-Main-Menu move. Disable competing operations and navigation
from acceptance through completion; callbacks carry lifetime identity and cannot navigate a newer session.

For the future career save/quit action, request the C-04 capture, wait for durable write acknowledgement,
then stop/join playback and navigate. Failure keeps the live session and a persistent reason for retry.
Closing the process forcibly recovers only the last successful save. Existing Home "without saving"
semantics remain explicit; no hidden save is implied. Exact controls/copy remain S1 handoff work and
must be backed by these real operations before they are presented as functioning runtime behavior.

**Done when:** exercise double clicks, Back/cancel, paused/full-time save, disk failure/retry, scene
teardown, late completion and repeated report acknowledgement. No orphan worker, second tick owner,
wrong return destination or command accepted after teardown is permitted.

### C-11 — Determinism and engine-lane isolation

**Owners:** #30 + MatchClientCore test owners; pinned-host owner for actual Unity evidence.

Keep no-input parity, recorded-input replay, season-commit equivalence and resume equivalence as
separate locks. Resume compares the same initial configuration and accepted input sequence, with an
explicit capture fence; it is not compared against a command-free run. Add all C-01–C-10 failure and
boundary cases to the owning suites and mirrored home/away cases where team identity matters.
Do not alter engine gameplay or its current schema merely to bypass a missing presentation/season seam.
Any necessary match-engine change during W8 B→C still requires digest-neutrality evidence or must wait.

**Done when:** applicable supplemental Linux checks pass, actual pinned Unity compilation/EditMode and
interactive lifecycle checks are recorded against the landing identity, and no unmeasured runtime,
performance or determinism certification is claimed.

### C-12 — Contract and UX handoff completion gate

**Owners:** #30/#44/#37 and client contract owners; UX workstream for S1 artifacts.

Before implementation, record reviewed signatures, field/version layouts and owner-spec amendments for
C-01–C-10; none is granted merely by describing it here. Review and approve this revision as a whole,
then back-propagate normative amendments atomically with their implementation. S1 Gate-A adoption must
classify each actual landing separately from its proposed design; Gate F/G must cover the complete
interactive/resume route and truthful report/error states. Recruitment remains deferred by the user;
this revision assigns no participants or sessions and grants no S1 validation waiver.

**Done when:** an implementer can trace every state, capture field, command, read model, navigation move,
error and acceptance case to its owning requirement and verified landing, without inventing product
behavior or treating a proposed seam as an existing API.

### Implementation order and evidence ledger

| Step | Requirements | Required output before advancing |
|---|---|---|
| 1 | C-01, C-02, C-03, C-12 | Reviewed boot/staging/continuation signatures and phase/identity rules |
| 2 | C-04, C-05, C-06, C-07, C-09 | Reviewed complete persistence/barrier layout and owner-codec validation rules |
| 3 | C-08 plus C-01–C-09 | Shared execution/finalization implementation, failure/resume/determinism tests |
| 4 | C-10, C-11 | Navigation/UI integration and exact-identity pinned-host evidence |
| 5 | C-12 / S1 | Adopted capability delta and independently satisfied UX gates |

All evidence entries are currently **NOT IMPLEMENTED / NOT VERIFIED for D-P7-6**. The standalone match
save/replay and independent P7b foundations identified in §8 may be reused, but their tests are not
substitutes for the composed career requirements above. C-01–C-12 completion requires an actual evidence
reference; a checkbox or contract approval alone is insufficient.

## Version History

| Version | Date | Notes |
|---|---|---|
| 0.1 | 2026-11-03 | Proposed lifecycle, deterministic fixture ordering, exactly-once finalization, persistence inventory, failure handling and acceptance locks; no approval or implementation inferred. |
| 0.2 | 2026-11-03 | Repository cross-check: distinguish current Home-only saves from D-11 future scope; remove mandatory persisted command history; correct dependency direction; expose boot/fatigue, fold/identity/report, staging and transaction gaps with per-item evidence. |
| 0.3 | 2026-11-03 | Turn cross-check gaps into C-01–C-12 proposed obligations with owners, chosen staging/capture/report policies, failure cases, implementation order and completion evidence; no implementation or approval inferred. |
| 0.4 | 2026-11-03 | WITHDRAW v0.1–v0.3 after live remote-main check at 1d3cebf3: no mid-match saves, no new save format/restore path; map earlier requirements to remote amendment v0.6. |
