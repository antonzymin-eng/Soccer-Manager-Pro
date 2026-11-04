# D-P7-6 — Interactive career matches: amendment draft to Season & Competition Loop #30

**Created:** November 2, 2026\
**Last Updated:** November 4, 2026\
**Version:** 0.7\
**Status:** PROPOSED technical contract; product decisions recorded in roadmap D-11. Not implemented
or a normative spec. Owning-spec amendments and final technical approval remain due.\
**Purpose:** define interactive managed fixtures, selected-coach delegated completion and no in-match
saving in one contract for #30, with #26/#34/#41/#44/#37/#38 and client consequences. This revision
replaces v0.6's ordinary live abandon/restart route. The automatic starting eleven is retained.

## 1. Source baseline and authority

The season/match implementation inspected at `1d3cebf3` still resolves a round synchronously through
`SeasonLoop.PlayNextRound`. P7b adds opt-in career UI bindings but no interactive career-match seam.
`SeasonLoop.BootFixtureEngine` supplies availability-filtered squads and #29 entry fatigue, but no
adaptive opponent configuration. `MatchSession` owns paced playback/commands; `TickOnce` refuses after
`Start`, even after `Stop`. The event bus is process-static: only one engine may be in flight.
`CardLedgerFold` consumes each tick's transient ledger; it does not serialize its pending records.
`CareerSaveSlots` refuses an active match block. These are integration constraints, not promised APIs.

Roadmap [D-11](path-to-playable-roadmap.md) records the owner's product
choices. Individual choices do not approve the complete technical contract. Before implementation,
amend #30, #26, #34, #41, #44, #37 where needed, #38 and the client design with ERR entries and their
version histories. No match-save block, enum allocation, format bump or tuning change is granted here.
Staff persistence may independently require its owning format work. Recruitment remains deferred.

## 2. Decision register

| ID | Topic | Status | Decision / remaining boundary |
|---|---|---|---|
| DC-01 | In-match saving | OWNER-CONFIRMED | No saving an active match, including a paused one. This is not a ban on saving for the whole fixture calendar day. Home-only manual save before/after the match remains valid. |
| DC-02 | Selected delegate | OWNER-CONFIRMED | Simulate rest of match lets the user choose the assistant manager or another eligible coach employed by their club. No silent default, fictional staff identity or arbitrary archetype labelled as a real coach. |
| DC-03 | Continuation | OWNER-CONFIRMED | The selected person manages the remainder of the same match: retain time, score, rosters, fatigue, injuries, tactics, cards, substitutions and already accepted inputs. No new fixture or round-model replacement result. |
| DC-04 | Ordinary leave action | OWNER-CONFIRMED | Replace the normal abandon/restart route with delegated completion. Normal Exit offers coach selection, completes the match/round, saves, then exits (DC-Q3); no forfeit or forced manual play. |
| DC-05 | Medical eligibility | OWNER-CONFIRMED PRINCIPLE | Ability to start or enter depends on the injury and the owning medical rule. This is not blanket permission for all injured players; the exact #41 eligibility mapping is still due. |
| DC-06 | Disciplinary records | OWNER-CLARIFIED | Supply the session with starting disciplinary records, let it update private match-local records from cards/participation, and return one completed record with the result. No career-side live polling/writes and no mid-match serialization. Finalization applies the returned effects once through #44 rules. |
| DC-07 | Starting eleven | RETAINED OWNER DECISION | Automatic starting XI; team-tactic preparation remains. Delegation does not introduce starting-XI selection. |
| DC-Q1 | Opponent manager | OWNER-CONFIRMED | Adaptive AI opponent in career matches. Use the recommended fixed initial archetype 0 for the opponent as the authorized default. This is not a fabricated real coach identity or the selected delegate's mapping. |
| DC-Q2 | Return to player control | OWNER-CONFIRMED | Delegation is final for the current match; finish it and show the report. No switch back to player control. Cancelling the coach picker before confirmation is still allowed. |
| DC-Q3 | Normal application exit | OWNER-CONFIRMED | Offer coach selection, complete the same match and round in the background, save successfully, then exit. A failed save keeps the completed in-memory state and exit pending for retry. Forced termination can recover only the last durable save. |
| DC-Q4 | Pre-kickoff delegation | OWNER-CONFIRMED | Tactics Setup offers Play or delegate the whole match, selecting the assistant manager or another eligible coach. Both routes use the same match engine; delegation is accelerated engine simulation, not an instant round-model result. |
| DC-Q5 | Staff behavior mapping / eligibility | TECHNICAL CONTRACT WORK | Use the recommended bounded real-staff slice, stable IDs and role/availability checks; define the selected person's tactical/substitution policy through #34/#26. No numeric mapping was previously recommended, so do not invent one or treat a generic opponent archetype as the chosen coach. |
| DC-08 | Remaining recommended choices | OWNER-AUTHORIZED DEFAULTS | For matters not explicitly overridden, use the current proposal's recommended choices. Explicit injury/save/disciplinary-record/control restrictions take precedence. Withdrawn C-01–C-12 save/resume choices remain withdrawn. |
| DC-09 | User entry to delegated simulation | OWNER-CONFIRMED | No joining, spectating-as-a-route-back-to-control, or taking over an ongoing delegated match. After delegation confirmation, complete the match and show its report. Background progress is status, not an entry action. |

The owner authorized the recommended defaults for items not explicitly overridden. That authorization
settles already recommended choices (including opponent archetype 0); it does not approve nonexistent
APIs, unproposed numeric staff/injury formulas or the withdrawn resume design.
The role/availability checks in DC-Q5 must use real owning-system data.

## 3. Proposed player flow

**Before kickoff:** Tactics Setup offers **Play** and **Delegate whole match**. Play uses player control.
Delegating opens the same real-staff picker described below, then boots once with the chosen delegate
and the player's admitted setup tactic as the initial baseline. The delegate may change tactics later
through their defined policy; choosing a coach does not select a new starting XI or run a pre-kickoff
substitution. Cancelling this picker returns to Tactics Setup without booting an engine or committing
anything. The simulation result is shown in the normal report, not silently consumed on Home.

**After kickoff:**

1. During a live or paused match, choose **Simulate rest of match**.
2. A picker lists the actual assistant manager and eligible club coaches, identifying their roles and
   supported decision information. Opening/cancelling the picker alone must not transfer control.
3. Select a person and confirm. Revalidate employment, eligibility and current match/session identity.
4. Acknowledge the handoff at a safe between-ticks boundary. Show who is managing and that the same
   match is completing in the background. Do not navigate to a finished report early.
5. After confirmation there is no return to player control for this match. At full time, quiesce the
   tick owner, finalize the round once, and run the normal post-round autosave. Ordinary delegation
   shows the report/return to Home; an Exit request waits for that successful durable save, then closes
   the application. Cancelling the picker before confirmation leaves the current match under player
   control and cancels the Exit request; it does not abandon/restart the fixture.

If no eligible delegate exists, do not fabricate one: show the owning-system reason. Handling the
absence of eligible staff without trapping the player is a release dependency: the staff slice must
provide a legitimate available assistant/coach for the promised flow, or the owner must choose a
separate fallback before this flow is declared complete. No forfeit or discard fallback is assumed.

## 4. Save and exit boundaries

| Context | Required behavior |
|---|---|
| Career Home before kickoff on a fixture day | Existing manual saves allowed; do not disable by calendar date. |
| Tactics/preparation, no match started | No active-match bytes may be saved. Existing Home-only save UI can remain; any save exposed here must encode a coherent prematch career with transient plans discardable/rebuildable on load. |
| Player-controlled, paused or delegated active match | No save control and no career match-block write. |
| Full time while finalization is in progress | Wait for coherent completion before a save/exit acknowledgement; do not expose a half-committed round. |
| Career Home after completed round | Existing post-round autosave/manual save behavior. |
| Simulation or finalization failure | Preserve the owning fault state and persistent reason; never report success, auto-restart the fixture, or offer unsafe abandon from a partly applied round. Keep normal Exit pending; expose the actual recovery state. Remote Faulted/reload recovery and any inability to complete must be specified before implementation, not silently converted into a saved result. |
| Completed round but autosave fails | Keep the completed in-memory result. Offer retry of the write; never rerun the match. Normal requested exit must wait for successful saving; retain the result and allow retry. A forced process close is outside that acknowledged flow. |
| Forced termination/crash | Last successfully written career save only. No hidden mid-match save is introduced. |

This refines the save-enforcement contract's wording: transient unsafe state can still cause a precise refusal, but
"it is matchday" alone is never a save-refusal condition.

## 5. Technical work by owner

| Work | Owner and requirement |
|---|---|
| Staff foundation | #34: a bounded actual staff roster and assistant/coach assignments, stable staff IDs, club membership and availability. `src/` has no staff/backroom production assembly; the minimal specification has Coach/Scout/Physio, not a completed assistant-manager picker. This dependency does not automatically authorize the entire deep staff/hiring system. |
| Decision mapping | #34 + #26: immutable resolved decision configuration for the chosen person, including supported tactical tendencies and a substitution policy. Existing `ManagerProfile` is an archetype of aggression/caution/patience; it is not a StaffRecord adapter. Current tactical adaptation is not a complete coach substitution controller. |
| Live manager takeover | #26 / MatchEngine public contract: initialize the selected delegate consistently with the current tactic/preset and match clock. `ConfigureManager` is deterministic mid-match but resets metadata to Balanced and an unfired decision clock; do not blindly use it or rerun kickoff adaptation. Review a proper takeover seam and any necessary digest/schema implications against the actual landing. |
| Tick ownership | MatchClientCore: freeze player command acceptance, settle prior accepted input once, then transfer exclusive ownership at an acknowledged boundary. `TickOnce` currently refuses after paced `Start`, even after Stop; add a supported accelerated/unpaced mode rather than bypassing the guard. Keep one engine on the process-static event bus. |
| Match-local records and observers | MatchSession + #44/#37: collect match-local card/participation/statistics information while the engine runs, preserving substitution attribution and fault/coverage checks; return the completed disciplinary/participation record once. #30 does not poll or update career discipline each tick. Reuse the shared rules and a single composed session observation path; no duplicate consumer or silent reset on takeover. |
| Medical/substitution eligibility | #41/#30/#44: define injury-dependent start/entry eligibility and preserve suspension, keeper, window, cap, used-bench and sent-off constraints. Revalidate automated substitutions at the same authoritative boundary as player requests. |
| Commit/navigation | #30 + ClientApp: retain BeginRound/CompleteRound and joined completion; only a valid owned full-time result can commit. Add a named delegation transition, prevent duplicate requests/stale callbacks, and show report/Home only after completion. Keep PM-1 and non-career playback behavior separate. |
| Verification | Owning suites plus pinned Unity host: prove watched/delegated parity for the same actual decision/input sequence, correct staff/team ownership, observer coverage, eligible substitutions, single commitment and responsive background progress. No claim that different decision-making staff must produce identical results. |

Delegation identity/configuration and the handoff boundary belong in the in-memory deterministic test/
command evidence needed to reproduce that run. This does not reintroduce a durable full command journal,
rewind or a match-continuation save block.

### Proposed client transition table

These are client control states layered over the domain Closed/Open/Live/Faulted phases;
no new persisted mid-match state or ordinal allocation is implied.

| From | Request / guard | Next / effect |
|---|---|---|
| Preparation (Open, no engine) | Play | One kickoff into player-controlled Live; automatic XI and chosen initial team tactic. |
| Preparation | Delegate whole match | Coach picker, retaining the setup; no engine yet. |
| Coach picker before kickoff | Cancel | Preparation, same setup; no staff/controller/engine change. |
| Coach picker before kickoff | Confirm real eligible coach | One kickoff into delegated Live; freeze the staff configuration used for this run. |
| Player Live, including pause | Simulate rest | Coach picker; preserve and quiesce the same engine at a safe boundary while making the selection. No further ticks or player commands until cancel/confirm. |
| Coach picker during play | Cancel | Restore the previous player-control/pause state exactly; no delegation or abandoned fixture. |
| Coach picker during play | Confirm | Settle earlier accepted commands, install delegate via reviewed live takeover, then continue the same engine under one accelerated tick owner. |
| Delegated Live | Return to control | Refuse: user confirmed delegation is final for the match. |
| Player Live | Normal Exit | Same coach picker with an Exit-after-success flag; cancelling also cancels Exit. |
| Delegated Live | Normal Exit | Keep the existing confirmed delegate, flag Exit-after-success; do not require a second selection or restart simulation. |
| Any completion-in-progress | Duplicate request/callback | Refuse or return the existing operation receipt; no additional kickoff/tick owner/commit. |
| Full time | Quiesce/join; validate observer health and owned result | Common finalization; domain Closed only after successful complete. |
| Finalized, save acknowledged | Exit flag set | Exit; otherwise normal report then Home. |
| Finalized, autosave failed | Retry save | Keep result and Exit pending; write the same completed career, never rerun the match. |
| Any observer/simulation/finalization fault | Recovery required | Do not publish success; apply the specified fault protocol. Never discard a partly applied round or label an unfinished match saved. |

Only the controlled pause is used during the picker; cancelling preserves whether the player had
already paused. Confirmed delegation consumes that pause and runs the remainder. This is proposed
UI/lifecycle detail consistent with the confirmed choices, not a new grant of engine threading APIs.

### Before-code decisions and review artifacts still needed

1. **#34 roster/roles:** real assistant assignment and coach list, stable IDs, employed/available rule,
   and an unavailable-staff case that does not trap the player without a legitimate delegate.
2. **#34/#26 policy:** staff-to-tactics/substitution mapping and a versioned in-memory configuration
   identity. Adaptive opponent with fixed archetype 0 is the authorized recommended default; it does
   not replace the separate chosen assistant/coach policy. Staff persistence may require its own owning-format work even though no match-save block
   is needed; do not claim the absence of all save-format impact from an unimplemented staff subsystem.
3. **#41 rule:** injury-dependent start/entry eligibility supplied by the medical owner; exact condition/
   severity thresholds still unchosen. Do not reuse the withdrawn blanket injured-bench option as unrestricted injured-bench permission.
4. **#30/#26/client signatures:** canonical boot, live delegation, accepted-input fence, exclusive
   accelerated execution, managed-team restrictions and observer ordering. Preserve no tick-0 XI
   mutation and do not add direct UI-thread engine writes.
5. **Fault/exit protocol:** decide the safe recoverable behavior when completion cannot succeed. The
   confirmed successful path is finish/save/exit; this does not authorize freezing the application,
   retrying a partially applied finalizer, or fabricating a result to satisfy an Exit request.
6. **Verification plan:** pin the old synchronous behavior, attribute deliberate AI changes, then
   compare watched/delegated runs with equivalent actual controllers/inputs. Retain W8 B→C isolation
   and actual pinned-editor/interactive evidence requirements.

### Session disciplinary-record boundary — revised requirement

Use a pre-match record-in / completed record-out model. Use this boundary for both watched
and delegated matches; do not turn it into a career-level live card tracker merely because the existing
earlier draft places a fold at #30.

1. **Input:** #30/#44 supply a copied, validated disciplinary baseline scoped to the fixture's clubs,
   competition and player IDs, plus eligibility/rule context. Bind it to fixture identity and a baseline
   revision/fingerprint. Passing mutable career records to a running session is prohibited. Keep
   pre-existing season/competition accumulation separate from this match's yellow/red counters: a
   previous match's yellow is not this match's first yellow and cannot trigger an immediate second-yellow
   dismissal. Existing suspensions inform eligibility; they are not served or erased at kickoff.
2. **During play:** the session updates a private match disciplinary record as the engine issues cards
   and substitutions. Retain PlayerId attribution at the time of each event, participation and event
   ordering sufficient for #44's result rules. This is ordinary match bookkeeping, not live career
   persistence. The engine's event ledger is per-tick; current CardLedgerFold consumes it after each
   tick. A replacement must still capture the necessary information before it disappears—reading only
   the final engine's occupants/yellow flags would lose substituted players' cards and ordering.
3. **Handoff:** whole-match and remainder simulation retain that same private record, observers and
   coverage/fault state. Do not initialize an empty record when the selected coach takes over.
4. **Output:** after full time and internal playback-thread termination, the owned session returns one
   immutable completed fixture result with its updated match disciplinary record and participation
   summary. No intermediate career update or live save is required. Return source identity/fingerprint
   and completion identity with it, so another fixture's or stale baseline's record cannot be applied.
5. **Commit:** #30 validates that owned result and applies it once through the common #44 finalization
   rules: existing-ban serving with actual participation, then this match's new cards. The session's
   local updates must not already have mutated career bans; never both apply an updated tally and add
   the same card deltas again. Replace only the validated fixture scope, not an unrelated global tally.
   Shared #44 logic remains the authority; no duplicated simplified suspension algorithm in the UI.
6. **Failure:** a missing, incomplete, faulted, wrong-fixture or wrong-baseline match record is not a
   valid completed result. Do not silently clear it or commit truncated discipline. Recover through
   the agreed fault protocol, retaining no false success or unsafe abandon of partial career effects.

This needs a reviewed session/result integration seam: the current source does not provide this full
pre-match-copy/final-record contract. Its internal event capture can be reused or reorganized; the
product requirement is the input/output boundary and correct final records, not a specific per-tick
season observer API.

**Additional acceptance:** hash/compare career discipline throughout watched/delegated execution to
prove it remains unchanged until finalization; compare the returned record to the expected cards and
participants; test substituted-off card recipients, second yellow/red, multiple same-tick events,
duplicate completion, stale baseline, handoff with pending substitution and save-write retry. Career
ban/appearance/medical-load effects must agree after one commit, with no mid-match serialization.

### Thread termination is not a player feature

"Stop/join playback" is internal concurrency terminology: request that the background tick thread stop,
then wait for its termination before reading the final result, changing its owner or releasing resources.
It does **not** mean joining a game as a player. No Join match / Resume control / enter-running-sim action
is provided after confirmed delegation, whether chosen before kickoff, mid-match or on Exit. Tests must
refuse stale UI commands and any control-entry attempt for that delegated match; only progress status,
completion/report and the already defined exit flow are exposed.

## 6. Acceptance and decision completion

- Both pre-kickoff actions exist: Play or select a delegate; cancelling selection before confirmation
  creates no engine or result. Whole-match delegation uses the chosen setup baseline and automatic XI,
  then the selected person's defined policy. Its result/digest equals a watched control driven by the
  same delegate decisions. Do not compare it against a Human-controlled no-input run as if identical.
- Same tick/score/player state immediately across takeover, apart from acknowledged prior commands
  and deliberate manager/control metadata. No new engine, kickoff reset or random whole-match result.
- Valid selection of the real club assistant/coach; stale, unavailable or foreign staff rejected
  before mutation. The opponent never receives the selected delegate's commands.
- Identical pre-handoff state plus selected staff/configuration and accepted-input sequence gives an
  identical final digest/result across acceleration/display rates, for home and away managed clubs.
- Tactical and substitution decisions follow the specified selected-staff policy. Cover injury,
  suspension, already-used bench, goalkeeper, sent-off, window/cap and pending-substitution cases.
- The session receives a consistent starting disciplinary snapshot, updates private match-local
  records and returns the final record once. Cards, PlayerId attribution, participation and supported
  statistics match a watched control driven by the same decisions. No mutation of authoritative career
  discipline occurs while the session runs; preserve real observer fault/coverage status. No mid-match
  persistence is needed.
- Finalize/autosave once; duplicate handoff, late callback, observer failure, finalizer failure and
  write retry cannot add duplicate points/appearances/bans or silently abandon committed effects.
- Save availability before/after the match on the same calendar day; save refusal while live/paused/
  delegated. No format bump solely for this no-mid-match-save route.
- Prove a confirmed delegation cannot switch back to player control; cancelling its picker before
  confirmation makes no delegation/match-state change. Normal Exit remains pending until the completed
  round's save is durably acknowledged, including save retry and a blocked/failed write. Forced
  termination does not imply completed/saved work.
- Implementing staff eligibility and takeover APIs requires their owning contracts and validation;
  no UI-only invented roster or privately bypassed engine guard. Recruitment remains deferred.

### Adaptive-opponent implementation consequence

DC-Q1 requires AI configuration on both watched and headless managed-fixture paths, in the same boot
order as the selected tactic and entry fatigue. Current headless career opponents are inert, so this
is a deliberate behavior/digest change: retain T-IM-16 pre-change evidence and record the new
managed-fixture baseline rather than claim neutrality. The effect on the existing round-model fit must
be assessed under the current roadmap's governed quick-sim check; no automatic gameplay retune or
unrelated FullEngine expansion is authorized. After delegation, compare equivalent runs with the same
chosen staff/controller decisions; do not require equivalence with an unattended human side that makes
no tactical/substitution decisions.

Product choices are recorded in roadmap D-11 and the register above. Technical operation names,
staff/medical mappings and normative back-propagation remain proposals. No D-P7-6 implementation or
contract-wide approval is claimed.

## 7. Proposed integration contract — reviewable API/data boundary

This section defines the required semantics more precisely. Type/operation names are **proposed**,
not existing symbols or granted assembly references. Existing owner IDs and types remain authoritative;
no persisted enum ordinal, schema version, random domain or tuning constant is allocated here.

### 7.1 Immutable input and completed-result shapes

| Proposed record | Owner → consumer | Required contents and validation |
|---|---|---|
| `ManagedFixtureContext` | #30 → session composition | Fixture/season/round identity, managed team index, world day, frozen configuration identity, seed, actual roster and automatic XI/bench, entry fatigue, baseline tactics and eligibility context. MatchSession must attach to its one engine rather than recreate these inputs. |
| `FixtureDisciplineInput` | #44/#30 → session record collector | Copied competition/player-scoped `DisciplineEntry` baseline, unfiltered membership and eligible participants, applicable rule/config identity and baseline fingerprint. Season accumulation and current-match caution/red state are distinct. Never pass the mutable career tally. |
| `EligibleDelegate` | #34 → client picker | Real stable staff ID, employer club, assistant/coach assignment, available/eligible flag and reason, display identity and decision-profile identity. It must not expose an invented staff record merely to populate the picker. |
| `DelegateDecisionConfig` | #34/#26 → session controller | Frozen staff/club/team identity, tactical profile reference, substitution-policy reference, policy revision and resolved supported inputs. Revalidate against the actual roster/medical/disciplinary context; never resolve the opponent's configuration from the user's selected coach. |
| `MedicalParticipationDecision` | #41 → automatic XI/substitution validators | `CanStart`, `CanEnter` and a reason sourced from the actual injury/clinical participation rule. The same underlying injury facts must produce the same decision for player and coach requests. No UI-created severity threshold or blanket injured-player exception. |
| `MatchDisciplinaryRecord` | session collector → completed result | Source fixture/baseline identities; complete ordered player-attributed new-card record; original roster identity and actual participation; collection health/coverage and completion identity. Each card keeps the kind emitted by the engine, not a guessed conversion from final counts. |
| `CompletedManagedFixture` | owned session/handle → #30 finalizer | Immutable owned score/end tick, fixture/config identities, final match disciplinary/participation record and report publication with real completeness/cutoff. Valid only after exclusive ticking has stopped and the final observation has completed. The UI cannot supply or edit these values. |
| `MatchCompletionReceipt` | #30 → ClientApp | Committed fixture/round identity and resulting career revision, with outcome and committed/not-committed status. Duplicate completion returns the existing receipt or a refusal without changing state. |
| `CareerSaveReceipt` | slot writer → exit coordinator | Committed career revision, chosen slot/write sequence and successful durable-write acknowledgement. Exit must be tied to the completed round's revision, not an older successful save. |

Every returned collection must be an immutable copy/frozen owner buffer with a defined lifetime.
Cross-record identity checks include fixture, managed club/team, competition, roster/configuration and
baseline revision. A failed check refuses before replacing career state or installing a delegate.
These records are transient operation contracts; they do not imply mid-match serialization.

### 7.2 Proposed operations and allowed callers

| Operation | Caller / execution owner | Guarded result |
|---|---|---|
| `PrepareManagedFixture` | client requests; #30 season owner | Validate guards and compute BeginRound/Open preview with automatic XI, initial discipline and eligible delegates. No engine yet, no applied fixture results. Morning-step idempotence is retained; cancellation does not promise a full day rollback. |
| `StartPlayerMatch(configIdentity)` | client requests; #30 + MatchClientCore | Validate preview/config identity and run one canonical kickoff with entry fatigue and tactics; managed side player-controlled, opponent adaptive AI with initial archetype 0. Refuse a second kickoff. |
| `StartDelegatedMatch(delegateIdentity, configIdentity)` | client requests; same composition | Revalidate staff, medical/disciplinary eligibility and decision profile; boot once and enter delegated control. Preserve automatic XI and the chosen team-tactic baseline; no pre-kickoff substitution or instant round-model score. |
| `RequestRemainderDelegation(delegateIdentity, sessionIdentity)` | UI requests; session control service | Validate and stage all delegate inputs before changing ownership; then perform the one-way boundary sequence below. Return an accepted/rejected operation identity, never a second engine. |
| `RequestExitAfterCompletion` | UI / lifetime coordinator | During player control, use the same picker and handoff. During delegated control, retain that delegate and attach the exit flag to the existing operation. No abandon/restart, user Join or second delegate request. |
| `CompleteManagedFixture(ownedCompletion)` | client coordinates only after stop/wait; #30 applies | Check full-time state, session/record identity and coverage, then run the common finalizer once and return its receipt. No caller-provided score/card tally and no second report-triggered commit. |
| `SaveCompletedCareer(commitReceipt)` | completion coordinator; slot worker | Save that exact committed revision through existing pair-atomic writing. A retry repeats the write from the retained career state and never simulates/finalizes again. |
| `ExitAfterSave(commitReceipt, saveReceipt)` | application lifetime owner | Require matching completed revision and successful durable save receipt; release owned views/resources and exit. A pending/failed write cannot satisfy this operation. |

These operations need owning-spec/API back-propagation before code. MatchClientCore owns the session
controller/collector glue; #44 owns disciplinary rules and result validation; #30 owns career mutation.
No domain assembly may gain a dependency on ClientApp or MatchClientCore for convenience.

### 7.3 One-way handoff transaction

1. Pause at the acknowledged between-ticks boundary when opening the staff picker; remember the
   previous player pause state. Resolve the staff/configuration before accepting confirmation.
   Cancelling restores that pause/control state and makes no delegate change.
2. On confirmation, verify session/fixture and staff identity again, close player command acceptance,
   and settle all commands already accepted by the session. Reject late/stale player submissions
   explicitly; no command can be acknowledged and then silently lost during the transition.
3. Validate the takeover and stage its controller metadata before installing it. Preserve the actual
   current team/player tactics, pending accepted changes, fatigue, time, score, occupants, cards and
   substitution windows/counts. Opponent state is not reset.
4. Install the chosen delegate on the managed team only and consume the prior player pause. Advance
   the same engine with the supported accelerated/unpaced owner; run private match records/analytics
   through the same observation chain on every relevant tick. Do not call the currently forbidden
   Start-then-TickOnce combination or open another match on the process-static event bus.
5. Once accepted, player input/control-entry remains closed through full time. Repeated requests refer
   to the existing operation. An already accepted handoff failure is surfaced as a session fault;
   rejection before installation leaves player control coherent and recoverable.
6. At full time, request internal tick-thread termination and wait for it, then freeze the completed
   record/report and finalize once. A report action cannot race a trailing card/participation update.

**Tactic initialization rule:** `ConfigureManager` currently resets its preset marker to Balanced and
its decision clock to unfired. Neither that call nor `ApplyKickoff` may silently reset live tactics.
The takeover API must seed state from the actual current tactic and current decision time. If that
tactic has no exact catalogue preset, retain it as an explicit custom baseline until the next legitimate
AI decision; do not invent a nearest-preset conversion in the UI. #26 must specify the custom-baseline
transition before implementation, since today's ladder expects a valid current preset ordinal.

### 7.4 Disciplinary merge semantics

The returned session record describes the completed match, not an already installed replacement for
all career discipline. Finalization must:

- Validate the input baseline against the current fixture-scoped career baseline before the first
  write. Refuse missing players/competition, stale identity or incomplete collection.
- Apply existing #44 ban-serving rules using the actual participation set (original players plus
  incoming substitutes), then apply the returned ordered match cards through existing `ApplyCard`.
- Preserve #44's exact kinds: yellow adds one caution; second-yellow kind adds one caution plus its
  second-yellow ban; straight red adds its ban without an extra caution. Do not synthesize an additional
  red event from a second-yellow event or deduplicate legitimately additive bans.
- Keep prior season caution totals separate from the match's first/second caution logic. A returning
  yellow from a prior match is never interpreted as an on-field caution in this match.
- Change only the validated competition/player scope, retain unrelated entries, and publish a receipt
  only with the complete per-fixture effect block. Do not apply both a computed final tally and those
  same card deltas. Card collection/working records never mutate the authoritative season tally.

This uses shared #44 behavior, not a second suspension algorithm. Concrete `ApplyCard`/ban-serving API
amendments must accompany the actual participant-set integration. A record collector may reuse the
current fold internally, but its final immutable record must be exportable without committing to career
state on the tick thread.

### 7.5 Refusal and recovery contract

| Condition | Required behavior |
|---|---|
| Stale session/fixture/config/staff identity | Refuse before takeover or kickoff; retain the valid current state. |
| No eligible delegate | Show a real reason; a legitimate staff/fallback policy must exist before shipping the exit promise. Never fabricate a coach or silently abandon. |
| Prohibited user command after delegation | Refuse with delegated-control reason; no engine mutation and no return-to-control route. |
| Injury/suspension/keeper/window/cap rejection | Use the same authoritative refusal as a player substitution; the coach cannot bypass it. |
| Failed/incomplete disciplinary record | Refuse completion; never install truncated cards, inferred participants or a false completed round. |
| Analytics observer failure with healthy disciplinary completion | Preserve actual partial/unavailable statistics and cutoff. Do not invent full-match totals; distinguish this from a discipline-integrity failure that prevents commit. |
| Error before finalizer's first write | Retain a coherent live/completion state; use the guarded recovery restrictions below. No implicit restart. |
| Error after finalizer writes | Enter Faulted; refuse saves and normal mutation. No unsafe abandon, fabricated success or automatic partial retry. Keep the previous durable save. |
| Completed round with failed save | Retain the completed revision and pending Exit, allow write retry, and keep failure visible. No duplicate match/commit. |

**Recommended fault-exit resolution:** when a match cannot complete because of a genuine unrecoverable
integrity fault, stop and release the failed engine safely, preserve diagnostics and the previous
coherent durable career, and offer an explicit error-recovery exit. Do not label it a successful
finish/save exit or overwrite the last good save with faulty state. On next launch only that prior
save loads. This exceptional failure recovery is not an ordinary abandon/restart button and is not
available on a healthy match. It makes the user's successful finish/save/exit promise precise without
trapping the application indefinitely or inventing an outcome. Crash/forced termination remains the
same last-durable-save limitation. This proposed technical default is covered by the user's recommended-
choices instruction; the owning recovery contract still needs review and implementation evidence.

### 7.6 Traceable acceptance cases for the next implementation slice

| ID | Scenario / expected evidence |
|---|---|
| DCT-01 | Validate copied pre-match discipline/medical inputs and real staff identities; immutable input/output cannot alias career state. Refuse stale or foreign selections before mutation. |
| DCT-02 | Both kickoff actions share fixture/seed/roster/fatigue/tactic configuration; named coach controls delegated path, adaptive archetype-0 opponent remains the opponent. |
| DCT-03 | Pause/open picker/cancel restores exact player state. Confirm at running/paused/near-full-time boundaries preserves current match state, installs one controller, and closes player entry permanently. |
| DCT-04 | Settle accepted pre-fence commands once; refuse late player commands. Pending substitutions carry across takeover without double application or changed recipient attribution. |
| DCT-05 | Final returned record includes substituted-off and incoming players, yellow/second-yellow/red cases, season threshold crossings and same-tick ordering. Career discipline remains unchanged throughout play. |
| DCT-06 | Finalize each valid record once with correct serving-before-card order; duplicate/stale result and wrong baseline cannot change table, appearances, discipline or cursor. |
| DCT-07 | Watched and accelerated runs with the same controller decisions/input boundary yield the same result/digest/participants/statistics across managed home/away and before-/after-kickoff delegation. |
| DCT-08 | Internal thread termination completes before final record read; no lost last-tick card, second tick owner, observer reset, or user Join/Resume-control action. |
| DCT-09 | Save allowed on Home before/after a match on the same day; refused during watched/paused/delegated play and unsafe partial completion. No new persisted match block. |
| DCT-10 | Repeated Exit during delegated work uses the same operation/delegate. Exit waits for the completed revision's save receipt; failed-write retry never reruns match or finalizer. |
| DCT-11 | Missing/faulted disciplinary output blocks commit; a statistics-only fault retains truthful partial reporting. Inject before-/after-write finalizer failures and verify distinct recovery states. |
| DCT-12 | Genuine unrecoverable-fault exit preserves the old durable save and does not claim successful completion; healthy matches cannot invoke fault recovery as an abandon shortcut. |

These are planned tests, not executed results. Existing P7b source tests and standalone match replay
checks are reusable foundations only; they do not prove delegated staff control or the composed
record-in/record-out career contract. Implementation remains gated on the canonical owner amendments,
bounded #34/#26/#41 dependencies, appropriate Linux checks and exact-source pinned Unity validation.

## 8. Retained round, ownership and verification obligations

### 8.1 One round implementation and fixture order

Split `PlayNextRound` into internal `BeginRound` and `CompleteRound` shared by synchronous and
interactive routes. `BeginRound` performs existing guards, `RequireCommittableConfig`, idempotent
career day-steps (progression, training, medical), then stages all fallible fixture inputs: availability,
`FieldedXi`/actual participation inputs, `RosterIds` and round-model results where applicable.
`CompleteRound` applies fixtures in existing fixture-index order through one common effect block
(appearances, table, outcome event, played state, ban serving, cards), then advances the round cursor.
KD-9/FR-SN-012 still require the whole round; no strict subset may be committed.

Managed fixture kickoff occurs before any fixture result is applied; its owned completed result is
inserted at its fixture index. Other fixtures use their existing resolution mode and fixture-keyed
seeds. Do not boot another engine while the managed session owns the process-static event bus.
The synchronous overloads retain their provider guards and use the same halves within one call;
they do not acquire the interactive `RoundState` machine. `FullEngine` and `QuickSimAll` remain on
that synchronous route; the proposed interactive wrapper admits only `ManagedThroughEngine`, a
pending managed fixture and an owned squad provider. Refuse unsupported modes precisely.

Pre-stage F6/F9 validation for all fixtures before the first result write. This deliberately moves
those failures earlier than today's per-fixture apply loop; record and test the changed failure
boundary, including the existing recording-throw appearance test. Preserve pre-change synchronous
anchors (T-IM-16) separately from new watched/delegated parity: comparing two new paths cannot prove
neutrality against old code. Adaptive-opponent behavior is an intentional managed-engine digest
change and requires attributed before/after evidence and the governed quick-sim assessment.

### 8.2 Domain phases, cancellation and mutation guards

| Phase | Owned state | Saving / allowed completion |
|---|---|---|
| Closed | Coherent career before or after a round | Home saves allowed, including the same fixture calendar day. |
| Open | Day-steps and staged preview, no engine, no applied result | Transient plan is not saved. Cancel preparation may discard it and return Home. |
| Live | One owned engine, including paused/delegated and full-time awaiting finalization | No saving or ordinary abandonment; delegate/complete the same match. |
| Faulted | Finalizer threw after its first career write | No saving, mutation, ordinary cancellation or unsafe partial retry. Preserve diagnostics and previous durable save. |
| Committed → Closed | Whole round applied and cursor advanced | Post-round autosave; failed write retries the same completed revision. |

There is no stored Ended phase: read full time only after internal tick-thread termination and final
observation. Closed may include a cancelled *pre-kickoff* preparation whose day-steps ran; those cursors
are coherent within `[clock - 1, clock]`. Day-steps remain run and are idempotent on the next preparation.
Do not conflate pre-kickoff cancellation with abandoning an active match.

While Open/Live/Faulted, refuse all season/career mutations that affect staged inputs or saved state:
`AdvanceDays`, `AdvanceToNextFixtureDay`, both `AdvanceAndPlayNextRound` overloads, `RollToNextSeason`,
`SubmitTransferBid`, `ApplyTransaction`, a second preparation, `SeasonSaveManager.Save` and
`PlayerCareerStates.TrySetFocus` via the exposed Career object. Propagate the owning guard rather than
letting callers bypass it through a child object. After cancelled preparation on the pending fixture
day, keep focus/transfer/transaction changes refused until the round is played: the day's training and
medical steps already consumed their inputs. Permit coherent Home saves and another preparation.
Direct `DisciplineRules` writers are a composition-owner restriction, since the loop cannot intercept
them; verify no client constructs those writers. Read-only projections stay available. Every refusal
needs a named failure row in the owning contract; no new public mutator may escape an exhaustive
allowlist/refusal audit.

Kickoff is atomic: validate/configure fully before exposing a handle; on failure retain coherent Open
state with no live engine. Completion preconditions are checked before any career write. A failure
before writes retains a coherent retryable state; after the first write latch Faulted. Never silently
retry a partially applied round or infer a rollback from fixture-keyed random seeds.

### 8.3 Engine and observation ownership

#30 owns fixture preparation, the engine/handle lifetime and career finalization. MatchClientCore
attaches the playback/session controller to that engine and owns session-local collection; it must
not construct a second engine. #44 remains the disciplinary-rule authority. Domain assemblies must
not reference presentation/client assemblies: owner contracts carry data across this boundary.

Compose the admitted squads once and configure those exact instances at kickoff; no second
availability walk may change the XI shown in preparation. Boot order: squads and entry fatigue,
manager configuration, then kickoff tactic application in the managed team's home/away slot.
Use the player's chosen baseline for the managed side and AI archetype 0 for the opponent. Automatic
XI is retained; refuse pre-kickoff substitutes rather than applying a tick-0 mutation. All player and
coach commands enforce managed-team ownership and the same eligibility/window/cap/keeper rules.
Back-filled suspended bench players cannot enter. Injured players start/enter only under #41's
injury-dependent rule; an unconditional injured-bench permission is withdrawn.

After each engine tick capture participation/substitutions and card events with their original
PlayerIds before the ledger resets. Seed participation with starters, add incoming substitutes;
substituted-off players retain their cards and participation. Do not approximate participation by
the final occupants or use starting XI alone for appearances, medical load or ban serving. The #41
any-time participation appearance rule requires its owning amendment; widening #44's serving input
fulfills its existing actual-participant obligation (ERR-044-010), not a new suspension algorithm.

Use one composite observation chain with per-consumer fault isolation so an analytics fault does not
skip discipline/participation or another reader. Preserve PM-1/browser callback behavior. Track an
explicit observation count against completed engine ticks: a consecutive-ledger check alone misses
a skipped first or final tick. Latch integrity faults with a safe readable reason before another tick;
incomplete/faulted discipline blocks commit. Analytics-only faults preserve honest partial statistics
and cutoff. No reset of collection/health on delegation and no fabricated full-match report.

The UI never reads the live engine. Completion and record freeze run only after Stop requests tick
termination and Join has returned; never hold an observer lock while joining. Poll progress/faults
through published snapshots or safe status fields. Join is internal thread synchronization, with no
user entry to an ongoing delegated match. Quiescence must also close the final-tick observation race.

### 8.4 Additional retained acceptance locks

DCT-01–12 in §7.6 are planned, not executed. Retain these v0.6 integration obligations alongside them:

| Lock | Required evidence |
|---|---|
| Shared round implementation | Every mode uses one apply block; fixture order, outcome count, engine count, table, finance/progression and cursor effects occur once. |
| T-IM-16 pre-change anchor | Pin old synchronous digest, all results, full save-byte hash, bans/appearances and counters before code. Attribute intended AI and earlier-validation changes. |
| Full-save parity | Equivalent watched/headless controllers yield equal final digest, all results, participation, bans/appearances, counters and actual full save bytes, including managed-home/away mirrors. |
| Exhaustive mutation audit | Reflect over public loop/career mutators; every one belongs to the guarded list or explicit read-only allowlist. Test Open, Live, Faulted and cancelled-preparation restrictions. |
| Preparation cancellation | No engine or result is committed; day-steps stay idempotent; Home save/load/reprepare yields coherent preview, cursor and final round. Healthy Live cannot invoke cancellation. |
| Atomic boot / complete | Inject roster/configuration failure before kickoff and result-write failures before/after the first write; verify distinct coherent/Faulted states and no unsafe retry. |
| Observation coverage | Miss first, middle and last ticks; hold the final observer open while requesting completion; refuse until it finishes and the tick thread has stopped, without deadlock or lost cards. |
| Eligibility and commands | Suspended back-fill bench refusal, injury-dependent rules, no pre-kickoff substitution, opponent-team refusal, home/away and eligible/used/sent-off/window/cap cases. |
| Source wiring / Unity | Direct asmdef references, no reverse domain/client dependency, source-backed routing tests and exact-source pinned Unity compile/EditMode plus actual UI checks. |

### 8.5 Career routing and source projection

For `CanBeginManagedFixture`, Career Home offers a named preparation action rather than running
`PlayMatchday` synchronously. Refuse direct `PlayMatchday`/`AdvanceRound` bypasses on that managed
fixture day. On a bye, an unmanaged career or an unsupported synchronous mode, retain the existing
progress action. Project `IsProgressBlocked` from real operation/round state and bump cached-view
revision on lifecycle changes. Reject `Execute` while matchday work is in progress. Preserve normal
PM-1 routing and provenance: report returns to Career Home only for a career-owned match.

Refuse `CaptureSave` on a career-attached session and every season/save-slot entry while Open/Live/
Faulted; a caller must not bypass the rule via low-level `SeasonSaveManager.Save`. Home before or
after the match remains saveable on the same day. `CareerSession` wrappers stop/wait for the tick
owner before calling completion, then autosave only the closed coherent round. Preparation
cancellation returns Home without constructing an engine. No report-open callback can commit twice.

### 8.6 Owning amendment checklist

- #30: shared round halves/guards, immutable preview/owned handle/result, no-match-save enforcement,
  complete/cancel-preparation/fault states, common finalizer and named failures; update §1/§2/§3/§4,
  test contracts, reference algorithms, appendices, outline and approval records together.
- #44: match-record input/output and actual-participant serving; update ERR-044-010 with the code,
  and bench-eligibility mirrors. Preserve serving-before-card order and exact ApplyCard kinds.
- #41/#34/#26: medical participation policy, real delegate roster/persistence/eligibility and versioned
  staff tactical/substitution mapping/live takeover; no invented thresholds or fake identities.
- #21/#26: kickoff baseline versus live command semantics, custom takeover baseline, opponent AI
  and managed-team restrictions, including direct assembly references.
- #38/client design: distinct career preparation/report/Home moves, one-way delegation, pending exit,
  save retry and truthful report/fault states. Add lifecycle IntentRouting rows only with real seams;
  do not repurpose PM-1 ReturnToMainMenu or ship phantom public enum members.
- S1: adopt the Gate-A delta only after technical contract approval and source/host verification;
  preserve all prescribed tasks, including delegated finish/report and failure/cancellation states.

## Review history

| Version | Date | Change |
|---|---|---|
| 0.7 | 2026-11-04 | Consolidate confirmed selected-coach delegation, no in-match saving, injury-dependent eligibility and session records into the canonical draft and roadmap D-11; replace ordinary live abandonment, retain round/observer/guard obligations and identify owning amendments. Technical approval remains due. |
| 0.6 | 2026-11-03 | AR-2 Medium/Low fixes (M7, M11–M21, L4–L10). **Interactive route (M11–M13, M17):** new `PlayManagedMatchday` action routed to Tactics Setup, `PlayMatchday`/`AdvanceRound` refused on managed fixture days, Q2 gains option (d) and the Q1 (a) × Q2 asymmetry; `ManagedTeamIndex` on preview and handle, with the career-attached drain refusing the other team's commands; `Ended` dropped as a stored state; post-abandon mutators refused until the round is played. **Round integrity (M14, M21, L8, L10):** the synchronous path stays stateless and Faulted is interactive-only, per-mode resolution placement and the moved F6/F9 failure point stated, `Kickoff` atomic, FR-SN-003 plan check, lock-free fault read, join enforced in `CareerSession.CompleteMatchday`/`AbandonMatchday`. **Specs, ownership, locks (M7, M15, M16, M18, M19, L4–L7):** §4 gains the FR-SN-025/§1.2, FR-SN-013b, §4.1/§4.2, §7 and outline sites; locks split by owner and host; #38 Appendix C and the ERR-044-010 closure move to the code landing; §2.7 split with new Q3 for tier 1 and the August 17 decision cited; XI-selection claim replaced; tactic-commit rationale, T-IM-10, the #44 quote, W7 and `IneligibleToEnter` corrected. **Client facts (L9):** `Execute` refuses `MatchdayInProgress`, `IsProgressBlocked` source change, autosave rationale dropped. |
| 0.5 | 2026-11-03 | AR-2 High fix (H4). v0.4's §2.8 installed the composite observer on every `MatchSession`, which would have left `Streamer.PostTickObserverFault` null for good on PM-1 and the web host (its readers `MatchClientHost.AnalyticsFault` and `S0ScreenPresenter.cs:504` would go blind) and broken more tests than the one it named, while §6 said PM-1 was unchanged. Option (a) adopted: `MatchSession.Attach` alone installs the composite; `AttachTickObserver` registers an `After` consumer on a career-attached session and stays set-once elsewhere; `AddTickConsumer` is refused on other sessions. The career route keeps a fault signal (the analytics adapter's own `_incomplete` latch, `TickConsumerFault(After)`, `IsObservationFaulted`), and its match screen reads the session surface where `S0ScreenPresenter.cs:504` reads the streamer. Option (b) recorded and rejected. §1 names the readers and tests; §2.4, §2.8, §6 and T-IM-11 aligned; T-IM-11's `Complete` outcomes move to ClientApp.Tests, the host that reaches both `SeasonLoop` and `MatchSession` (§5). |
| 0.4 | 2026-11-03 | AR-1 Medium/Low fixes (M1–M10, L1–L3). **M1–M2 (round integrity):** new §2.9 thread contract (Complete/Abandon on the season thread after `MatchSession.Stop()` joins, `MatchEnded` read after the join, handle lock and invalidation, one engine in flight on the process-static bus); new §2.4.1 makes Complete apply-only, with every fallible per-fixture resolution precomputed at `BeginRound`, a Faulted state (Abandon refused, cause surfaced), and `FullEngine` refused for the interactive route; T-IM-12, T-IM-13. **M3–M4 (one implementation, participation):** §2.2 splits `PlayNextRound` into internal `BeginRound`/`CompleteRound` for every mode, with `BeginManagedFixture` as a narrower wrapper; T-IM-16 pins pre-change values; §2.5 defines participation (boot occupants plus incoming substitutes), moves it to a #30-owned `ParticipationRecord` fed by the handle's single pass, and §2.6 refuses pre-kickoff substitutions at the drain; "not a second walker" withdrawn. **M5–M6 (saves, #44):** §2.10 makes `Save(SeasonLoop, …)` refuse while a round is open, replaces `handle.Engine` with one-shot `TakeEngineForSession()`, refuses `CaptureSave` on a career-attached session, specifies the `CareerSession` operations, `MatchdayInProgress` and the post-abandon Closed state (T-IM-14, T-IM-15); §2.5's #44 item restated as conformance to FR-DC-011 / #44 §4.5 that closes ERR-044-010, with only #41 sign-off kept. **M7–M8 (normative sites, routes):** §4 names each #30 site with its replacement sentence under one ERR-030 entry and maps the locks to a T-SN-IM block in §5.9/§5.8; new §2.11 route table, D-P7-3 restated with the bye and unemployed fallback, #38 Appendix C rows; new owner question Q2 (quick-resolve for the managed fixture) with a recommendation. **M9–M10 (faults, refusals):** the handle exposes `IsObservationFaulted`, the client polls it and offers only abandon at once (§2.8); T-IM-07 extended to injected faults and first/last-tick gaps; §2.3 derives the refused-operation list from the real public surface, with a reflection check in T-IM-06. **L1–L3 (precision):** §1 corrected (day-steps include slot 1, the quick-sim row is mode-specific, "sim thread" split into season thread and tick thread, `CareerSaveSlots.cs:217` is the operative site, the fold's check scope, the new tick counter); locks compare full save bytes and counters, state their host assembly and real-match cost (§5, §7); §4 item 11 amends the §4.3 hot-path note with T-IM-17, and §2.1 aligns save wording (manual save on Career Home, autosave after Complete). |
| 0.3 | 2026-11-03 | AR-1 High fixes (H1–H3). **H1:** §2.5's "a suspended player is never on the bench" was false (the ERR-030-044..046 back-fill reinstates suspended and injured players onto the bench); new §2.7 makes back-filled bench players ineligible to enter (option (a); (b) recorded and rejected), refused at the `match-client-core` drain with a season-side `Complete` backstop and an ERR-030 obligation; T-IM-09. **H2:** `Begin` no longer boots the engine; it computes a pre-kickoff preview, and `Kickoff(PreKickoffConfig)` boots through a compose/configure split of `BootFixtureEngine`, applying the managed tactic on the #26 FR-TP-004 boot path instead of a "tick-0" command; §2.3 states and T-IM-01/04/06 aligned; #26/#21 named in §4/§6; new owner question Q1 (career opponent AI manager); T-IM-10. **H3:** new §2.8 composite per-tick observer owned by `MatchSession` with per-consumer fault isolation, season observation first; T-IM-11. §1 gains the facts each fix rests on. |
| 0.2 | 2026-11-02 | Owner decisions applied: no starting-XI changes (v0.1 Q1, answer (a)); no mid-match saves; quitting a match abandons it and returns to Career Home with the round unplayed; no kickoff autosave (v0.1 Q2/Q3 withdrawn). Removed: the managed-fixture sub-blob, season-save format 8 → 9, restore path and live-save locks, and the managed-first round order (index order kept; headless path unchanged). Added: `AbandonManagedFixture`, the abandon lock, save unavailability while a round is open. Size back to about "L". Not yet adversarially reviewed. |
| 0.1 | 2026-11-02 | Initial draft, from the November 2 review of the client-design bullet contract: substitutions vs starting-XI consumers, unserialized card fold, engine/observer ownership across assemblies, round order and states, save-policy owner questions. |
