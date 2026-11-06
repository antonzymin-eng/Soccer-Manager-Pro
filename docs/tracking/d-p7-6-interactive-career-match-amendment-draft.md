# D-P7-6 — Interactive career matches: amendment draft to Season & Competition Loop #30

**Created:** November 2, 2026\
**Last Updated:** November 4, 2026\
**Version:** 0.11\
**Status:** PARKED (owner instruction, November 4, 2026). Technical content is v0.9's: a PROPOSED,
unapproved contract for the feature roadmap D-11 defers until #34 staff ships. §11 (v0.11) adds an
unreviewed carry-forward list from a parallel draft; nothing else changed after v0.9. AR-3
FAILED v0.8 (4 High / 7 Medium / 9 Low); v0.9's proposed fixes are unreviewed. No further review or
revision until #34's T0 landing is scheduled (roadmap C6: do not harden ahead of an assembly). On
unparking, re-verify the source baseline, then run an independent re-review before owning amendments
or technical approval. Not implemented or a normative spec.\
**Purpose:** define interactive managed fixtures, selected-coach delegated completion and no in-match
saving in one contract for #30, with #26/#34/#41/#44/#37/#38 and client consequences. This revision
replaces v0.6's ordinary live abandon/restart route. The automatic starting eleven is retained.

## 1. Source baseline and authority

The source baseline is `7f81e48a` (P7b code), retained unchanged through documentation head
`4162e6d4`. Season/match code inherited from `1d3cebf3` still resolves a round synchronously through
`SeasonLoop.PlayNextRound`. P7b adds opt-in career UI bindings but no interactive career-match seam.
`SeasonLoop.BootFixtureEngine` supplies availability-filtered squads and #29 entry fatigue, but no
adaptive opponent configuration. `MatchSession` owns paced playback/commands; `TickOnce` refuses after
`Start`, even after `Stop`. The event bus is process-static: only one engine may be in flight.
`CardLedgerFold` consumes each tick's transient ledger; it does not serialize its pending records.
`CareerSession.Resume` refuses loaded match bytes and marks that slot Damaged; slot storage
itself is not the match-resume policy owner. These are integration constraints, not promised APIs.

Roadmap [D-11](path-to-playable-roadmap.md) records the owner's product
choices. Individual choices do not approve the complete technical contract. Before implementation,
amend #30, #26, #34, #41, #44, #37 where needed, #38 and the client design with ERR entries and their
version histories. New operation/data/refusal types are proposed below. Their symbolic names grant no existing API,
wire ordinal, enum number, match-save block, format bump or tuning change.
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
| DC-Q1 | Opponent manager | OWNER-CONFIRMED; REVISED NOVEMBER 4 | Adaptive AI with deterministic variety across Aggressive 0 / Pragmatic 1 / Balanced 2. This explicit answer replaces the earlier fixed-Aggressive recommended default. Freeze the actual club/profile mapping and policy version in fixture/test identity; never present an archetype as a real employee. |
| DC-Q2 | Return to player control | OWNER-CONFIRMED | Delegation is final for the current match; finish it and show the report. No switch back to player control. Cancelling the coach picker before confirmation is still allowed. |
| DC-Q3 | Normal application exit | OWNER-CONFIRMED | Offer coach selection, complete the same match and round in the background, save successfully, then exit. A failed save keeps the completed in-memory state and exit pending for retry. Forced termination can recover only the last durable save. |
| DC-Q4 | Pre-kickoff delegation | OWNER-CONFIRMED | Tactics Setup offers Play or delegate the whole match, selecting the assistant manager or another eligible coach. Both routes use the same match engine; delegation is accelerated engine simulation, not an instant round-model result. |
| DC-Q5 | Staff scope / policy | OWNER STAGING DECISION NOVEMBER 4; POLICY WORK DUE | Defer delegation until the staff system ships; do not pull a small #34 slice into PM-2. At that later landing, amend FR-ST-003 for actual assistant/coach assignments and freeze #34/#26 tactical/substitution policy. Interactive career matches also wait for staff; PM-2 retains existing Career Home resolution. |
| DC-08 | Remaining recommended choices | OWNER-AUTHORIZED DEFAULTS | For matters not explicitly overridden, use the current proposal's recommended choices. Explicit injury/save/disciplinary-record/control restrictions take precedence. Mid-match save/resume is excluded. |
| DC-09 | User entry to delegated simulation | OWNER-CONFIRMED | No joining, spectating-as-a-route-back-to-control, or taking over an ongoing delegated match. After delegation confirmation, complete the match and show its report. Background progress is status, not an entry action. |

The owner authorized the recommended defaults for items not explicitly overridden. That authorization
settles earlier recommended choices except where later explicit answers replace them (the fixed
opponent archetype 0 was replaced by the November 4 variety answer); it does not approve nonexistent
APIs, unproposed numeric staff/injury formulas or the withdrawn resume design.
The role/availability checks in DC-Q5 must use real owning-system data.

### 2.1 Product-decision provenance

These choices came from the owner's earlier replies in the project conversation. They revise the
older D-11 product choice; they do not constitute sign-off on this complete technical draft. The
quotations below preserve the distinction between a direct answer and a recommended default.

| Register items | Evidence from the owner's replies |
|---|---|
| DC-01 | “no saving during the match” and “Saving on Career Home before or after a match on the same day remains available.” |
| DC-02 / DC-03 | “I'd want the 'simulate rest of match' to allow for user to choose assistant manager or one of the other coaches to manage the rest of the match in quick sim.” |
| DC-Q2 / DC-09 | Asked whether choosing a coach should be final for that match, the owner answered “Yes—finish the match and show the report (recommended)”; later clarified “We shouldn't have an ability for user to join an ongoing match if it's being simmed (if that's a part of this)”. |
| DC-04 / DC-Q3 | Asked whether Exit should offer coach selection, finish the match, save the completed round and then exit, the owner answered “Yes—finish, save, then exit (recommended)”. |
| DC-Q4 | Asked whether Tactics Setup should offer Play or selected-coach delegation before kickoff using the same engine, the owner answered “Yes—offer Play or delegate the whole match (recommended)”. |
| DC-Q1 / DC-08 | “Yes—adaptive AI opponent (recommended)” is the direct product answer. Archetype 0 is a recommended implementation default covered by “for items i didn't mention use recommended choices.” It was not a separate explicit numeric answer. |
| DC-05 | “Injuries: eligibility should depend on the injury.” Exact #41 thresholds remain technical work. |
| DC-06 | “A match sessions receives disciplinary records before starting. Then it updates the record based on the match cards and sends it back.” No live career-side mutation or mid-match serialization is implied. |
| DC-07 | Automatic XI is the retained owner decision already recorded in v0.6 / D-P7-4, rather than a new decision attributed to these replies. |

### 2.2 November 4 answers after AR-3

| Question | Exact owner answer / consequence |
|---|---|
| Bring a minimal #34 slice forward? | “Defer delegation until the staff system ships.” The earlier small-slice recommendation is rejected. No early staff implementation or roadmap advance is authorized. |
| Coach decision execution? | “Simulation thread and shared command queue (recommended).” Adopt external policy evaluation on the single simulation thread and the authoritative shared command service. |
| Opponent disposition? | “Deterministic variety across existing profiles (recommended).” Replace fixed Aggressive 0 with stable variety; exact algorithm below remains part of the technical proposal. |
| Defer interactive career matches as well? | “Defer interactive career matches too (recommended).” PM-2 retains existing Career Home match resolution; interactive career play and delegation arrive together after staff ships. |

**Resolved staging:** PM-2 keeps existing Career Home engine-backed round resolution, result/table
feedback and between-round saving. Interactive career matches, pre-kickoff/remainder coach delegation
and the finish/save/exit match flow ship together only after the real staff system and reviewed
D-P7-6 contract. No interim in-match Exit policy, fake coach or early staff slice is needed. PM-1's
standalone interactive match is separate and unchanged. Sections 3–8 describe that deferred future
contract; they are not PM-2 blockers or a claim of implemented functionality. The independent P7b
bindings, season screens, #44 T3 and current S1 Home-resolution journey continue.

## 3. Proposed staff-enabled player flow (delegation deferred until #34 ships)

**Staff prerequisite:** this target route is enabled only after the actual staff system and its
assistant/coach contract ship. The independent P7b Home resolution route remains available today.

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
| Tactics/preparation, no match started | No save action while Open. CancelPreparation discards the transient plan, returns to coherent Closed/Home and allows saving on that same calendar day. No preparation save format is introduced. |
| Player-controlled, paused or delegated active match | No save control and no career match-block write. |
| Full time while finalization is in progress | Wait for coherent completion before a save/exit acknowledgement; do not expose a half-committed round. |
| Career Home after completed round | Existing post-round autosave/manual save behavior. |
| Simulation or finalization failure | Enter the specific session/round fault state in §7.5. Quiesce asynchronously, preserve diagnostics and prior durable save; offer explicit recovery exit/reload. Never fabricate a saved result or trap an Exit waiting for impossible completion. |
| Completed round but autosave fails | Retain the completed revision. Offer write retry/another admitted slot, Cancel Exit (keep the result on Home), or explicit storage-error Exit without saving with a lost-progress warning. The last is a proposed exceptional recovery action, not successful finish/save/exit; see §7.5. |
| Forced termination/crash | Last successfully written career save only. No hidden mid-match save is introduced. |

This refines the save-enforcement contract's wording: transient unsafe state can still cause a precise refusal, but
"it is matchday" alone is never a save-refusal condition.

## 5. Technical work by owner

| Work | Owner and requirement |
|---|---|
| Staff foundation | #34 remains in its existing later roadmap tranche. Delegation waits for that staff system, including actual assistant/coach assignments, stable IDs and availability. Current FR-ST-003 cannot supply the requested picker unchanged; amend it at that owning landing. No early bounded slice is authorized. |
| Decision mapping | #34 + #26: immutable resolved decision configuration for the chosen person, including supported tactical tendencies and a substitution policy. Existing `ManagerProfile` is an archetype of aggression/caution/patience; it is not a StaffRecord adapter. Current tactical adaptation is not a complete coach substitution controller. |
| Live manager takeover | Owner-selected external coach controller (§7.7): retain managed engine mode Human; install frozen #34/#26 policy on the session simulation thread and submit through the authoritative queue. Do not call ConfigureManager/ApplyKickoff on takeover. Engine-policy alternative would need an explicit schema/digest and eligibility design; it is not the recommended path. |
| Tick ownership | MatchClientCore: freeze player command acceptance, settle prior accepted input once, then transfer exclusive ownership at an acknowledged boundary. `TickOnce` currently refuses after paced `Start`, even after Stop; add a supported accelerated/unpaced mode rather than bypassing the guard. Keep one engine on the process-static event bus. |
| Match-local records and observers | #30 owns the engine handle, private participation collector and #44 fold; its domain ObserveTick runs on the tick thread. MatchSession only pumps that observer. #37 separately owns analytics. No tick-time career tally mutation, client-built discipline/result record, duplicate consumer or reset on takeover. Explicitly amend #44 §4.5 / §3.1 / §4.3 for frozen output and finalizer authority. |
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
| Player Live, including pause | Simulate rest | Request pause/input fence, finish any in-flight tick/observation, then acknowledge and enable the picker. No further ticks after acknowledgement until cancel/confirm. |
| Coach picker during play | Cancel | Restore the state at the acknowledged fence and prior pause flag; no delegation, rollback of the in-flight tick or abandoned fixture. |
| Coach picker during play | Confirm | Verify the once-only input settlement receipt, install the external coach controller, then continue the same engine under one accelerated tick owner. |
| Delegated Live | Return to control | Refuse: user confirmed delegation is final for the match. |
| Player Live | Normal Exit | Same coach picker with an Exit-after-success flag; cancelling also cancels Exit. |
| Delegated Live | Normal Exit | Keep the existing confirmed delegate, flag Exit-after-success; do not require a second selection or restart simulation. |
| Full time reached before picker/handoff acceptance | Delegation or Exit | Quiesce and complete the existing match; install no new controller. Exit attaches to that completion and waits for its save. |
| Any completion-in-progress | Duplicate request/callback | Refuse or return the existing operation receipt; no additional kickoff/tick owner/commit. |
| Full time | Quiesce/join; validate observer health and owned result | Common finalization; domain Closed only after successful complete. |
| Finalized, save acknowledged | Exit flag set | Exit; otherwise normal report then Home. |
| Finalized, autosave failed | Retry save | Keep result and Exit pending; write the same completed career, never rerun the match. |
| Critical observer/controller/simulation/finalization fault | Recovery required | Do not publish success; apply the specified fault protocol. Never discard a partly applied round or label an unfinished match saved. |

Only the controlled pause is used during the picker; cancelling preserves whether the player had
already paused. Confirmed delegation consumes that pause and runs the remainder. This is proposed
UI/lifecycle detail consistent with the confirmed choices, not a new grant of engine threading APIs.

### Before-code decisions and review artifacts still needed

1. **#34 roster/roles:** real assistant assignment and coach list, stable IDs, employed/available rule,
   and an unavailable-staff case that does not trap the player without a legitimate delegate.
2. **#34/#26 policy:** staff-to-tactics/substitution mapping and a versioned in-memory configuration
   identity. Adaptive opponent with deterministic variety across Aggressive/Pragmatic/Balanced is the November 4 decision; it does
   not replace the separate chosen assistant/coach policy. Staff persistence may require its own owning-format work even though no match-save block
   is needed; do not claim the absence of all save-format impact from an unimplemented staff subsystem.
3. **#41 rule:** injury-dependent start/entry eligibility supplied by the medical owner; exact condition/
   severity thresholds still unchosen. Do not reuse the withdrawn blanket injured-bench option as unrestricted injured-bench permission.
4. **#30/#26/client/#match-viewer signatures:** canonical boot, live delegation, acknowledged pause/accepted-input fence, exclusive
   accelerated execution, managed-team restrictions and observer ordering. Preserve no tick-0 XI
   mutation and do not add direct UI-thread engine writes.
5. **Fault/exit protocol:** review the concrete proposed exceptional recovery actions in §7.5. The
   confirmed successful path is finish/save/exit; this does not authorize freezing the application,
   retrying a partially applied finalizer, or fabricating a result to satisfy an Exit request.
6. **Verification plan:** pin the old synchronous behavior, attribute deliberate AI changes, then
   compare watched/delegated runs with equivalent actual controllers/inputs. Retain W8 B→C isolation
   and actual pinned-editor/interactive evidence requirements.

### Session disciplinary-record boundary — revised requirement

Use a pre-match record-in / completed record-out model for both watched and delegated sessions.
#30/#44 own the private match record inside the domain handle; MatchSession pumps its observer but
never constructs the record. Authoritative career records change only at finalization.

1. **Input:** #30/#44 supply a copied, validated disciplinary baseline scoped to the fixture's clubs,
   competition and player IDs, plus eligibility/rule context. Bind it to fixture identity and a baseline
   revision/fingerprint. Passing mutable career records to a running session is prohibited. Keep
   pre-existing season/competition accumulation separate from this match's yellow/red counters: a
   previous match's yellow is not this match's first yellow and cannot trigger an immediate second-yellow
   dismissal. Existing suspensions inform eligibility; they are not served or erased at kickoff.
2. **During play:** the #30-owned observer updates a private #44 match disciplinary record as the engine issues cards
   and substitutions. Retain PlayerId attribution at the time of each event, participation and event
   ordering sufficient for #44's result rules. This is ordinary match bookkeeping, not live career
   persistence. The engine's event ledger is per-tick; current CardLedgerFold consumes it after each
   tick. A replacement must still capture the necessary information before it disappears—reading only
   the final engine's occupants/yellow flags would lose substituted players' cards and ordering.
3. **Handoff:** whole-match and remainder simulation retain that same private record, observers and
   coverage/fault state. Do not initialize an empty record when the selected coach takes over.
4. **Output:** after full time and internal playback-thread termination, the #30-owned handle freezes one
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
| `FixtureDisciplineInput` | #44/#30 → #30-owned domain collector | Copied competition/player-scoped `DisciplineEntry` baseline, unfiltered membership and eligible participants, applicable rule/config identity and baseline fingerprint. Season accumulation and current-match caution/red state are distinct. Never pass the mutable career tally. |
| `EligibleDelegate` | #34 → client picker | Real stable staff ID, employer club, assistant/coach assignment, available/eligible flag and reason, display identity and decision-profile identity. It must not expose an invented staff record merely to populate the picker. |
| `DelegateDecisionConfig` | #34/#26 → session controller | Frozen staff/club/team identity, tactical profile reference, substitution-policy reference, policy revision and resolved supported inputs. Revalidate against the actual roster/medical/disciplinary context; never resolve the opponent's configuration from the user's selected coach. |
| `MedicalParticipationDecision` | #41 → automatic XI/substitution validators | `CanStart`, `CanEnter` and a reason sourced from the actual injury/clinical participation rule. The same underlying injury facts must produce the same decision for player and coach requests. No UI-created severity threshold or blanket injured-player exception. |
| `MatchDisciplinaryRecord` | #44 fold / #30 handle → domain-owned completed result | Source fixture/baseline identities; complete ordered player-attributed new-card record; original roster identity and actual participation; collection health/coverage and completion identity. Each card keeps the kind emitted by the engine, not a guessed conversion from final counts. |
| `CompletedManagedFixture` | private #30 handle → #30 finalizer; read-only client projection | Immutable owned score/end tick, fixture/config identities, final match disciplinary/participation record. #37 separately publishes report completeness/cutoff; no client report DTO enters this domain record. Valid only after exclusive ticking has stopped and the final observation has completed. The UI cannot supply or edit these values. |
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
| `StartPlayerMatch(configIdentity)` | client requests; #30 + MatchClientCore | Validate preview/config identity and run one canonical kickoff with entry fatigue and tactics; managed side player-controlled, opponent adaptive AI with its frozen deterministically selected profile. Refuse a second kickoff. |
| `StartDelegatedMatch(delegateIdentity, configIdentity)` | client requests; same composition | Revalidate staff, medical/disciplinary eligibility and decision profile; boot once and enter delegated control. Preserve automatic XI and the chosen team-tactic baseline; no pre-kickoff substitution or instant round-model score. |
| `CancelPreparation(previewIdentity)` | UI requests; #30 owner, Open only | Discard preview/plan with no engine/results; keep idempotent day-steps and guarded focus/transfer state; Closed/Home permits save. Refuse during Live/fault. |
| `RequestRemainderDelegation(delegateIdentity, sessionIdentity)` | UI requests; session control service | Validate and stage all delegate inputs before changing ownership; then perform the one-way boundary sequence below. Return an accepted/rejected operation identity, never a second engine. |
| `RequestExitAfterCompletion` | UI / lifetime coordinator | During player control, use the same picker and handoff. During delegated control, retain that delegate and attach the exit flag to the existing operation. No abandon/restart, user Join or second delegate request. |
| `CompleteManagedFixture(ownedHandle)` | client coordinates only after stop/wait; #30 applies | Check full-time state, session/record identity and coverage, then run the common finalizer once and return its receipt. No caller-provided score/card tally and no second report-triggered commit. |
| `SaveCompletedCareer(commitReceipt)` | completion coordinator; slot worker | Save that exact committed revision through existing pair-atomic writing. A retry repeats the write from the retained career state and never simulates/finalizes again. |
| `ExitAfterSave(commitReceipt, saveReceipt)` | application lifetime owner | Require matching completed revision and successful durable save receipt; release owned views/resources and exit. A pending/failed write cannot satisfy this operation. |

These operations need owning-spec/API back-propagation before code. MatchClientCore owns session control and callback pumping. #30 owns the engine handle,
record construction and career mutation; #44 owns the fold/rules and record validation.
No domain assembly may gain a dependency on ClientApp or MatchClientCore for convenience.

### 7.3 One-way handoff transaction

1. Request a pause/input fence and await its receipt before opening an interactive coach picker.
   A tick already in flight may finish, including all observation. No tick may start after the
   acknowledgement until cancel/confirm. Remember the prior pause flag and the state at that receipt;
   cancel restores that acknowledged state/flag, not an imaginary rollback of the in-flight tick.
2. On confirmation, verify session/fixture and staff identity and the input-fence receipt again.
   Earlier commands have been settled once at that boundary; do not replay them on confirmation. Reject late/stale player submissions
   explicitly; no command can be acknowledged and then silently lost during the transition.
3. Validate the takeover and stage its controller metadata before installing it. Preserve the actual
   current team/player tactics, pending accepted changes, fatigue, time, score, occupants, cards and
   substitution windows/counts. Opponent state is not reset.
4. Install the chosen external coach controller on the managed team only and consume the prior player pause. Advance
   the same engine with the supported accelerated/unpaced owner; run private match records/analytics
   through the same observation chain on every relevant tick. Do not call the currently forbidden
   Start-then-TickOnce combination or open another match on the process-static event bus.
5. Once accepted, player input/control-entry remains closed through full time. Repeated requests refer
   to the existing operation. An already accepted handoff failure is surfaced as a session fault;
   rejection before installation leaves player control coherent and recoverable.
6. At full time, request internal tick-thread termination and wait for it, then freeze the completed
   record/report and finalize once. A report action cannot race a trailing card/participation update.

**Tactic initialization and takeover:** under the owner-selected external-controller option the managed
engine side stays ManagerMode.Human, even when a coach controls its command queue. Configure squads,
the opponent's AI profile and all feature flags, then ApplyKickoff with the user's admitted baseline.
Human mode prevents FR-TP-004 from replacing that baseline. Assert the nondefault team/player tactic
at the first AI stride. During takeover, do not rerun ApplyKickoff or ConfigureManager; seed the
external controller from actual current tactics/time. #26 owns its custom-baseline transition and
substitution policy; this is not an invented nearest-preset conversion in the UI.

**Full-time boundary:** after quiescence, the owner checks whether full time already occurred before
accepting a handoff. If so, skip delegate installation and complete the same match. An Exit request
attaches to the existing completion; it does not open a second picker, replay or finalizer. Pending
commands that can no longer apply must receive an explicit ended-match refusal/outcome in the command
record. Do not replay them after full time or silently erase an acknowledged request. This requires
an owner/drain amendment: the current `MatchClientDriver.Service` clears its batch when `MatchEnded`
is true without appending those commands to `FailedCommands`.

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
| Error after finalizer writes | Enter RoundFaulted (the proposed RoundState.Faulted phase); refuse saves and normal mutation. No unsafe abandon, fabricated success or automatic partial retry. Keep the previous durable save. |
| Completed round with failed save | Retain the completed revision and pending Exit, allow write retry, and keep failure visible. No duplicate match/commit. |

**Fault/recovery contract (PROPOSED; technical approval still required):** distinguish domain round
state from session execution state. A critical observation/controller/tick fault before fixture-finalizer writes
latches SessionFaulted with the round still Live. Stop advancing and asynchronously quiesce/join the
owner. Morning day-steps may already have run; no rollback of those steps is promised. No Save or Complete can succeed from that partial session. Offer Recovery Exit or Reload Last
Durable Career after resource release, with explicit loss of the unfinished match. This is available
only for a genuine latched fault; it is not a healthy-match abandon/restart action.

If finalization throws before its first write, a validated retry of that operation may be offered;
otherwise use recovery exit/reload. After the first write, latch RoundFaulted, never retry partial
application, and offer the same last-durable recovery actions after quiescence. Neither fault state
waits indefinitely for a normal-success receipt, overwrites the good save or claims completion.

A storage failure after successful round completion is SaveFailed, not either integrity fault. Retain
the completed revision and offer Retry Write, another admitted slot, Cancel Exit (stay on Home with
that in-memory result), or **Exit Without Saving** after a clear lost-progress warning. The last action
is an explicit exceptional storage-error exit; it is not a successful saved exit and cannot be offered
during healthy unfinished play. No retry re-simulates or re-applies the match. UI remains responsive
while any write is pending; close requests re-use that operation, not an unbounded main-thread Wait.

These exceptional exits are proposed recovery semantics, not already owner-confirmed normal paths.
The confirmed successful path remains finish/save/exit. Forced termination always retains only the
last durable save. DCT-11/12 and DCT-17 test each fault stage, cancelled exit and repeated write failure.

### 7.6 Traceable acceptance cases for the next implementation slice

| ID | Scenario / expected evidence |
|---|---|
| DCT-01 | Validate copied pre-match discipline/medical inputs and real staff identities; immutable input/output cannot alias career state. Refuse stale or foreign selections before mutation. |
| DCT-02 | Both kickoff actions share fixture/seed/roster/fatigue/tactic configuration; named coach controls delegated path, adaptive opponent retains its explicitly frozen profile from the varied club mapping. |
| DCT-03 | Pause/open picker/cancel restores the acknowledged boundary state and prior pause flag; an in-flight tick may finish before acknowledgement. Confirm at running/paused/near-full-time boundaries preserves current match state, installs one controller, and closes player entry permanently. |
| DCT-04 | Settle accepted pre-fence commands once; refuse late player commands. Pending substitutions carry across takeover without double application or changed recipient attribution. |
| DCT-05 | Final returned record includes substituted-off and incoming players, yellow/second-yellow/red cases, season threshold crossings and same-tick ordering. Career discipline remains unchanged throughout play. |
| DCT-06 | Refuse completion before full time with full career-save bytes/counters unchanged; finalize each valid record once with correct serving-before-card order; duplicate/stale result and wrong baseline cannot change table, appearances, discipline or cursor. |
| DCT-07 / T-IM-01 | Compare the interactive attached run to the existing synchronous AdvanceAndPlayNextRound ManagedThroughEngine route through the canonical configure/finalizer halves, with the same actual controllers, seed, inputs, flags and opponent profile. Require equal digest, every result, participants, statistics, bans/appearances, full SeasonSaveManager.Save bytes, MatchOutcomes.Count and EnginePlayedFixtures. Also compare equivalent watched/accelerated delegation before kickoff and for the remainder; mirror managed home/away. |
| DCT-08 | Internal thread termination completes before final record read; no lost last-tick card, second tick owner, observer reset, or user Join/Resume-control action. |
| DCT-09 | Prove TakeEngineForSession (working name) succeeds once only; a second attachment/access and stale handle fail without mutation. Save allowed on Home before/after a match on the same day; refused during watched/paused/delegated play and unsafe partial completion. No new persisted match block. |
| DCT-10 | Repeated Exit during delegated work uses the same operation/delegate. Exit waits for the completed revision's save receipt; failed-write retry never reruns match or finalizer. |
| DCT-11 | Missing/faulted disciplinary output blocks commit; a statistics-only fault retains truthful partial reporting. Inject before-/after-write finalizer failures and verify distinct recovery states. |
| DCT-12 | Genuine unrecoverable-fault exit preserves the old durable save and does not claim successful completion; healthy matches cannot invoke fault recovery as an abandon shortcut. |
| DCT-13 / T-IM-02 | **Input replay, ClientApp.Tests; two real matches per home/away mirror.** Repeat from identical initial career/engine state, seed, PreKickoffConfig and tick-stamped accepted command sequence, including within-tick order. Freeze the same delegate identity/policy and handoff tick where applicable. Require equal final digest, every MatchResult, participation, cards/bans/appearances, MatchOutcomes.Count, EnginePlayedFixtures and full completed career-save bytes. Replay is in-memory test evidence; no mid-match save or durable command journal is introduced. |
| DCT-14 / T-IM-03 | **Season-commit equivalence, SeasonSave.Tests; controlled end seam.** From identical staged round inputs, use internal owner-only test seams to seal each route's own result with the same managed score, ordered disciplinary record and actual participation facts, then run interactive and synchronous finalization. Require byte-identical SeasonSaveManager.Save output after completion, equal MatchOutcomes.Count and EnginePlayedFixtures, and expected appearances/ban/card effects. Include nonempty card and incoming-substitute positive controls. Use a reviewed internal test-only end seam; expose no public result injection or new production dependency on MatchClientCore. This proves commit equivalence; DCT-07 proves real-run parity and T-IM-16 separately anchors old behavior. |
| DCT-15 / T-IM-08(b) | **Authoritative command-drain refusal, MatchClientCore.Tests; no full match.** On a career-attached session, refuse Substitute at CurrentTick == 0 and opponent-team SetTeamTactic, SetPlayerTactic and Substitute at the drain. Each refusal appears in FailedCommands with its owning reason, never in the applied log, and leaves the digest equal to a no-command control at the same tick. Include an accepted managed-side command as a positive control after kickoff. Mirror managed home/away; enforce eligible-bench/window/cap/keeper rules and refuse player commands after delegation. UI-only rejection cannot satisfy this lock. |
| DCT-16 | **Full-time request race, ClientApp.Tests plus MatchClientCore.Tests.** End the final tick while opening the picker, confirming delegation or requesting Exit; also hold final observation open. No delegate is installed after full time, queued commands receive applied-or-explicit-refused outcomes, only one completed result/finalizer exists, and Exit waits for that completed revision's save. Inject a late queued command to exercise the existing ended-batch drop hazard. |
| DCT-17 | Exercise Exit/window-close from preparation, each picker, player Live, delegated Live, completion, SessionFaulted, RoundFaulted and SaveFailed. Verify cancel semantics, exceptional loss warnings, responsive UI, single ownership/write receipt and no healthy abandon shortcut. |
| DCT-18 / T-IM-09(b) | Put a suspended reinstatee on the bench, keep them unfielded and verify their ban is served at finalization; retain a suspended extremis starter in participation and verify exemption. Refuse an ineligible incoming player both at the authoritative drain and finalization backstop. Mirror home/away and injury-dependent start/entry cases. |
| DCT-19 / T-IM-04/15 preparation subset | Against a control that only prepares/cancels, compare actual full save bytes and both nonserialized counters before/after cancellation; no result/card/appearance effect. Save/load/reprepare and play with the same inputs; the final full save bytes equal uninterrupted preparation/play. Test guarded focus/transfer/transaction attempts and unchanged bytes on each refusal. Live abandonment stays withdrawn. |
| DCT-20 | Verify FR-SN-003 one-fixture-per-club preflight, both-club appearance prevalidation, bounded record overflow, no client-created completion payload and exact feature-flag/config identity. Prove critical failure happens before a career write or enters the specified fault state. |

These are planned tests, not executed results. Existing P7b source tests and standalone match replay
checks are reusable foundations only; they do not prove delegated staff control or the composed
record-in/record-out career contract. Implementation remains gated on the canonical owner amendments,
bounded #34/#26/#41 dependencies, appropriate Linux checks and exact-source pinned Unity validation.

### 7.7 Proposed staff/controller slice and authoritative result ownership

**H1 owner decision — delegation waits for staff:** keep #34 in its existing later roadmap tranche.
Do not bring forward the small staff slice. Before delegation can ship at that later point, amend
FR-ST-003 to distinguish an actual roster from the designated HeadCoach/HeadPhysio/ChiefScout
consumer slots and support assistant/coach assignments. Preserve existing StaffRole ordinals;
allocate new job/assignment encoding only in the owning approved amendment. The real staff system
must supply stable identities, availability and persistence; the picker cannot precede it. No hiring,
wage or other staff implementation is authorized by this document. Interactive career play is deferred alongside delegation; PM-2 keeps Home round resolution (§2.2).

`NeutralHouseStaff` is explicitly a REAL generated entity under FR-ST-004/KD-5. An actual assigned,
named, stable-ID, persisted record from that factory may qualify; a missing record, default struct,
placeholder UI label or arbitrary archetype presented as an employee may not. The current factory
alone does not create an assistant assignment or override FR-ST-003. Loaded careers do not silently
reseed staff. Missing/incompatible staff data follows the owning save-compatibility rule; absent
eligible staff blocks preparation before kickoff with a precise repair/load reason. A corrupt staff
dependency detected after kickoff is a latched session fault with recovery exit, never a fabricated
coach or healthy abandon. New #34 save data may require owning format work, independently of the
prohibition on match saves. The owner explicitly deferred delegation until this dependency ships; interactive career play is deferred too, with no interim match Exit required in PM-2.

**H2 owner-selected architecture — simulation thread/shared queue:** coach decisions execute on the
session's single simulation thread, immediately before its authoritative pre-tick command service.
#34 produces an immutable real-person configuration; #26 owns BOTH tactical and substitution policy,
its deterministic engine-tick cadence/hold rules and versioned attribute mapping (never wall-clock or playback-speed timing); MatchClientCore evaluates that
policy from sim-thread reads and enqueues typed commands through the same queue as player input.
Every accepted/refused coach command is tick-stamped and subject to managed-team, suspension,
injury, window/cap, keeper and used-player guards. There is no direct coach call to engine mutators,
UI-thread policy evaluation or second tactical writer for the managed team. Resolve policy inputs
before installing the controller; numeric mapping/formulas remain required owning-contract artifacts.

The managed engine side stays Human-mode under the selected option, because the external controller supplies
its commands; the opponent uses the existing adaptive engine manager. The session controller has
explicit transient policy/handoff state included in in-memory replay evidence, not an engine snapshot
field or hidden durable command journal. This option does not require a snapshot-schema bump solely
for external coach identity. Any actual engine-state change still requires schema/digest review.
The in-engine controller alternative was not selected. Any later move into engine manager state
requires a separate snapshot/digest/eligibility amendment; it is not part of this proposal.

**H3 domain ownership rule:** #30 constructs and exclusively owns ManagedFixtureHandle, its participation
collector and the #44 CardLedgerFold seeded from copied pre-match baseline/boot occupants. The client
receives only an attachment capability and read-only projections; it cannot construct/replace the
completed score, discipline or participants. ObserveTick is domain code called by the session after
each tick; it updates private match data only. At joined full time, #30 freezes its OWN result and
CompleteManagedFixture takes that same owned handle, not a caller-supplied result DTO. Result/record
constructors and sealing are private/internal to the owning domain assemblies; identity checks bind
the handle to its loop, fixture and baseline. Reject foreign, forged, reused and stale handles before
any write. An opaque completion receipt is not a public result-injection API. Internal test end seams
remain test-only and cannot grant a production caller that ability.

#44 §4.5 / §3.1 / §4.3 MUST be amended explicitly: #30 seeds/pumps the fold, exports/finalizes its
private fixture record without writing season discipline on the tick thread, then applies serving
and ordered card effects once in the common finalizer. Existing root ownership and lossless-tap rules
are retained. #37 separately owns analytics buffers/publication; a partial analytics report is not
an incomplete disciplinary record, and no client analytics type enters #30's authority boundary.

### 7.8 Pause, accelerated playback, navigation and application close

LiveMatchStreamer.Pause currently sets a flag; PacingLoop reads shouldTick before TickOnce, so that
call alone is NOT a quiescence receipt. Add a career-scoped RequestPauseAndFence operation in the
streamer/session contract: close input admission, finish any in-flight RunTick plus every observer,
service or explicitly refuse accepted queued inputs at the recorded boundary, then publish an ack
with session identity/tick and prior pause state. Only then show an enabled picker. Cancel restores
the acknowledged state/pause flag. Tests hold a tick/observer open to prove no premature ack and no
ticks after ack. Registration/stop locks must not be held while waiting for worker termination.

Add an acknowledged paced/paused/accelerated mode on the SAME exclusive execution owner. Accelerated
mode removes wall-clock pacing but retains the exact command/observer order; it is not a larger
speed multiplier and does not call today's forbidden Start-then-TickOnce sequence. A switch does
not recreate the engine, install a second tick thread or reset the static event bus. Waits/joins
occur off the Unity main thread; UI consumes published progress/receipts. Amend match-viewer playback
contract, MatchClientCore session contract and client design §6, with PM-1/browser compatibility tests.

| Exit/navigation context | Proposed behavior |
|---|---|
| Preparation, no engine | CancelPreparation → coherent Closed/Home → save → exit; no automatic match is played. Cancel Exit leaves preparation unchanged before cancellation is committed. |
| Pre-kickoff coach picker | Back returns to preparation. Exit discards the picker/preview, performs the same preparation cancellation/save/exit; no coach or engine is committed. |
| Player-match picker | An Exit request sets the existing picker's Exit-after-success flag. Confirm delegates/finishes/saves/exits. Cancel cancels Exit and restores the acknowledged prior player/pause state. |
| Player Live / paused | Normal Exit uses that picker after pause/fence acknowledgement. Closing the window requests the same flow. |
| Delegated Live | Remain on completion progress; no Home/new fixture/demo/match re-entry. Read-only settings/accessibility overlays may return to progress without changing ownership. Exit attaches to the existing delegate/operation. |
| Completion or pending save | Repeated close/Exit reuses the current operation; show responsive progress. Only the matching saved revision satisfies normal Exit. |
| SessionFaulted / RoundFaulted | After asynchronous quiescence, offer diagnostic recovery exit or reload of last durable career; no successful-save claim or healthy abandon. |
| SaveFailed after completion | Retry/another slot, Cancel Exit to Home, or explicit warned storage-error exit without saving; preserve the completed in-memory revision until that choice. |

Unity window-close must be intercepted BEFORE teardown using the reviewed Application.wantsToQuit /
OnApplicationWantsToQuit lifetime route; OnApplicationQuit and OnDestroy are too late for an async
coach picker. Return/defer quitting while the operation is pending, then permit one final quit after
its receipt. Engine/IO faults use the recovery choices above. OS kill/crash remains outside this
acknowledged flow. The existing P7b Dispose wait is not an implementation of this route.

### 7.9 Record bounds, replay types and feature configuration

Propose #44-owned boot-allocated fixture storage with explicit derived bounds:

| Buffer | Proposed admitted bound / overflow action |
|---|---|
| Occupancy and participant set | MatchEngineConstants.AgentIdSpace = 22 + 2×7 = 36 entries [DERIVED]; seed once before substitutions and retain original PlayerIds. |
| Substitution records | TEAM_COUNT × MAX_SUBSTITUTIONS_PER_TEAM = 2×5 = 10 [DERIVED] for the currently supported rules; rejected requests do not consume this event buffer. |
| Player-card records | 3 × AgentIdSpace = 108 [DERIVED], a conservative two-cautions-plus-one-dismissal bound per distinct admitted player. Kind-2 still has its exact one-caution/ban semantics, not an invented extra red. Staff/off-field cards are outside the current engine event contract. |

At admission validate the actual match rules/roster and the derivation. A future rule/capacity change
requires updating the owning constant and tests before admission. Current CardLedgerFold._pending
is a growing List, so reserving/bounding it is required implementation work, not existing behavior.
No silent eviction/truncation; excess/duplicate-invalid events latch integrity failure before another
tick and block commit. Test each boundary and boundary+1. Per-tick ledger capacity remains owned by
#17, not reallocated here. Collection has no card/substitution allocation after its boot reservation;
measure T-IM-17 against control, including event ticks, rather than assuming List growth is free.

New transient working shapes ARE required: ControllerHandoffRecord (identity, policy version, fence
and effective tick), CommandOutcome (Applied/Refused with request/tick/team), and symbolic refusal
reasons such as MatchEnded, DelegatedControl, WrongManagedTeam, BeforeKickoff and BenchUnavailable.
Assign these to MatchClientCore/client command ownership and domain eligibility ownership in their
amendments; preserve old wire/enum ordinals and allocate additions at that reviewed application.
DCT-13 replays the frozen policy and ordered applied/handoff records; DCT-15 checks explicit typed
outcomes. This is not a claim that these types already exist or a durable command-save requirement.

Freeze feature configuration with the fixture identity, including goalkeeper heading DISABLED for
career matches (today BootFixtureEngine never calls EnableGkHeading), the admitted catalogue, seed,
rosters/fatigue/tactics, managed team and actual opponent profile. No hidden default differs between
watched, accelerated and synchronous controls. Adaptive AI is confirmed. Archetype 0 is Aggressive;
that is the superseded fixed default, retained here only as decision history. The November 4 answer replaces that fixed default with deterministic variety across Aggressive 0 /
Pragmatic 1 / Balanced 2. Proposed mapping: validated nonnegative opponentClubId modulo the frozen
ManagerArchetypeCount (currently 3), with no new RNG draw. Club IDs 0/1/2/3 map to 0/1/2/0; the same
club keeps its profile at home/away and across fixture order. Freeze mapping/catalogue version in
configuration identity and re-pin affected AI anchors. No real opponent employee is fabricated.

### 7.10 Exact owning-spec amendments proposed for application

One allocated ERR-030 coordinates #30 changes; cross-owner amendments receive their own ERR entries.
These are proposed replacement obligations, not edits to approved text in this documentation pass.

| Owner/site | Required replacement / addition |
|---|---|
| #30 section-1 KD-9 / section-2 FR-SN-012 | “A round may span preparation/kickoff/completion; no fixture effect is applied before Complete, which applies the whole round in fixture-index order. Only preparation with no engine may be cancelled.” |
| #30 section-1 KD-7 / section-2 FR-SN-032 | Add preparation, cancellation, owned kickoff/completion and read-only round/fault state; include previously omitted AdvanceDays, ApplyTransaction, SubmitTransferBid. Refuse other mutations while Open/Live/Faulted and specified post-cancel pending-day mutations. |
| #30 section-1 §1.2, §1.5 / section-2 FR-SN-025 | Permit the domain handle's read-only-season ObserveTick on the match tick owner; world-day commands stay refused while the round is open. The client drives ticking but owns no result authority. |
| #30 section-1 KD-1 / section-2 FR-SN-021 | “SeasonSaveManager.Save refuses non-Closed round/session fault states; no active-match block is written by the career route. Cancel preparation returns to Closed/Home before saving.” No match-save-format bump; staff persistence is separately costed. |
| #30 section-2 §2.2 | Define RoundState, SessionExecutionState, preview, opaque owned handle and private completed record/participation; retain ActiveMatch only on the synchronous route. One-shot session attachment exposes no reusable engine getter or public result constructor. |
| #30 section-2 §2.3 | Add named refusals for mode/provider/fixture absence, duplicate club in a round, non-Open kickoff, wrong/stale handle, premature/duplicate completion, observer gap/fault, ineligible participation, repeat attachment, prohibited save/mutation and both fault stages. No retries after partial writes. |
| #30 section-2 FR-SN-013 / 013b + new FR rows | Actual participants replace XI-only managed appearance/serving inputs. Managed influence includes admitted kickoff baseline and authoritative logged live player/coach commands; non-managed results retain their existing model. Add staged round, collection/finalization/guard/eligibility/threading requirements; allocate FR numbers at application. |
| #30 section-3 §3.3.2 | BeginRound owns guarded idempotent day-steps. Preparation cancellation leaves them run; until the pending round is played, focus/transfer/transaction mutations remain refused. |
| #30 section-3 §3.4 | Replace duplicated resolve/apply with BeginRound staging and CompleteRound common finalization. Require FR-SN-003 preflight, paired appearance validation before writes, configure/observe owned handle, explicit critical session fault and post-write RoundFaulted. Retain existing synchronous retry posture and attribute earlier F6/F9 failures. |
| #30 section-3 §3.4 depleted-squad text / §3.4.1 | Specify ineligible suspended bench and injury-dependent admission, finalization backstop, actual participation and unfiltered rosters. Read squads once per club; reject >1 fixture per club before staging. No second availability walk between preview and kickoff. |
| #30 section-4 §4.1–4.3 | Record direct TacticalInstructions/other actual type references, owned handle/collector placement, new command/caller contracts, and the ObserveTick hot-path exception with measured bounded allocation behavior. No dependency on client or analytics DTOs. |
| #30 section-4 §4.4 / appendices B, B.1 | Save refusal at the lowest loop save entry; participation means “took part on anchor−k”. Existing appearance byte layout stays unchanged; any #34 block integration is separately specified. |
| #30 section-5 §5.8/new integration subsection; section-7 §7.1/7.3; section-9; outline | Apply all season-owned T-IM/DCT locks with ClientApp host labels where cross-assembly; retain exact full-byte/counter comparisons, FR mapping and costs. Add lifecycle seam/T-phase rows and refresh approval counts/FR ranges without claiming tests exist before code. |
| #44 section-2 FR-DC-011/eligibility failures; section-3 §3.1/§3.3; section-4 §4.3/§4.5; §5/§7 | #30 retains fold seeding/ownership/lossless pumping; add bounded frozen record extraction, no career write on tick, once-only serving-before-cards with actual participants. Amend the root interface explicitly; close ERR-044-010 only with implementing code. Preserve exact card kinds and unfiltered roster membership. |
| #41 FR-MD-010 / appearance-write contract and tests | Define injury-dependent CanStart/CanEnter, “any participation” appearance/load rule and both-club nonmutating appearance prevalidation. No client severity guesses or blanket injured-bench permission. |
| #34 FR-ST-003/004/011/012/014; §2 data, §3 genesis/projections, §4 ownership, appendices/save, tests/§7 | At the later staff landing, before delegation: roster plus designated slots, real assistant/coach assignments/availability, NeutralHouseStaff eligibility, stable identity/persistence/genesis and absent-staff refusal. Keep existing modifier reads on designated holders; do not falsely promise byte neutrality for new delegated decisions. |
| #26 FR-TP-004/007/010 + policy/hold/substitution tests; #21 FR-TI-027 | Selected external policy owns tactical AND substitution decisions through the queue; managed mode Human preserves user kickoff tactic; opponent remains engine AI. Specify current/custom baseline and deterministic cadence. Preserve #21 stride application for live commands. The unselected in-engine alternative would require a separate amendment. |
| #37 section-3 per-tick observation/publication contract (§3.5), client SessionMatchAnalytics integration and tests | Keep analytics separately owned and isolated from critical domain collection. Preserve final-tick coverage and true cutoff/incomplete status across pause/mode handoff; no client report DTO becomes authoritative career input. |
| #38 Appendix C / intent-routing and failure records | Name distinct preparation/cancel/report/Home/delegation/exit moves and new symbolic outcomes; refuse late/opponent/delegated commands authoritatively. Allocate added ordinals at code landing, never repurpose PM-1 ReturnToMainMenu. |
| match-viewer/LiveMatchStreamer contract; MatchClientCore MatchSession; client design §6/§5-P7/test plan | Acknowledged pause/fence and one-owner paced↔accelerated control; full-time queued-command outcomes in MatchClientDriver.Service; PM-1/browser compatibility; Unity close interception and asynchronous finalization/save/recovery UI. |
| #19 test strategy / CI application and deferred-interactive UX authority | Add the full-match integration gate/costs below with real runner configuration before code merge. The later interactive/delegation UX slice follows contract approval/source evidence. Current S1 targets PM-2 Home resolution, keeps all eight prescribed tasks and does not wait for D-P7-6; recruitment remains deferred. |

### 7.11 Verification placement and time budget (proposed, not measured)

The integration subject owns real-match evidence. Run pre-commit unit/property/short-tick/refusal and
controlled-end cases in the existing fast gate. Full-match cases require the source PR's named
career-integration job plus nightly and pinned-host validation; they are not silently skipped by a
new opt-in environment variable. Apply the actual test categories, filters, timeout and required-job
configuration with #19/CI approval before implementation lands. Existing mandatory gates stay intact.
One engine per process; no in-process parallel fixture engines. Shared run artifacts may be reused
only when fixture/config/controller/input identities and all required assertions match exactly.

| Planned evidence | Full-match runs (both managed-home/away mirrors included) |
|---|---:|
| Pre-change T-IM-16 synchronous anchors | 2 unpaced |
| Post-change attributed T-IM-16 anchors | 2 unpaced |
| T-IM-01 synchronous versus interactive attached | 4 unpaced |
| T-IM-02 repeated input replay | 4 unpaced |
| DCT-07 whole-match delegated equivalent decisions | 2 paced at 10× + 2 unpaced |
| DCT-07 remainder handoff equivalent decisions | 2 paced at 10× + 2 unpaced |
| DCT-08/16 real paced final-observer/quiescence race | 2 paced at 10× |
| **Conservative total, no reuse assumed** | **16 unpaced + 6 paced = 22 runs** |

A 90-minute match at 10× takes at least 540 seconds [DERIVED] of pacing; six such runs require at least 54
minutes even before overhead. Using the roadmap's ~120-second unpaced compute estimate, sixteen
unpaced runs add ~32 minutes: ~86 minutes serial plus compile/assertion overhead. Proposed integration
job budget is 120 minutes [EST]; measure on the pinned runner and revise the budget/job split openly if it
cannot finish, without weakening oracle assertions. This does not fit the 60-second pre-commit budget.
Controlled-end tests do not replace real replay/parity. Record source/runtime/config hashes, elapsed
per case and true throughput; do not claim any of these proposed runs already passed. Pre-change
anchors are captured before implementation; later PRs consume their immutable source-tagged artifacts
rather than silently rebuild an old runtime. The 22-run budget conservatively includes that capture.

## 8. Retained round, ownership and verification obligations

### 8.1 One round implementation and fixture order

Split `PlayNextRound` into internal `BeginRound` and `CompleteRound` shared by synchronous and
interactive routes. `BeginRound` performs existing guards, `RequireCommittableConfig`, idempotent
career day-steps (progression, training, medical), then stages all fallible fixture inputs: availability,
`FieldedXi`/actual participation inputs, `RosterIds` and round-model results where applicable.
Before any apply, validate FR-SN-003 (each club appears at most once in the round); refuse duplicate
club fixtures before day-step/result mutation. Prevalidate the two clubs' appearance writes as a pair
using the same identity/membership/day checks as PlayerCareerStates.ValidateAppearanceWrite, for all
planned fixtures before the first apply. Revalidate final actual-participation inputs for both clubs
before the first fixture apply in CompleteRound, as well as before either club's appearance write. Expose the check in the owning #41/#30 contract; do not call today's
private helper from a client. A refused away side must not leave a home appearance.
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
| RoundFaulted (RoundState.Faulted) | Finalizer threw after its first fixture-effect write | No saving, mutation, ordinary cancellation or unsafe partial retry. Preserve diagnostics and previous durable save. |
| Committed → Closed | Whole round applied and cursor advanced | Post-round autosave; failed write retries the same completed revision. |

SessionFaulted is a separate domain-handle integrity latch while the round remains Live; controller
faults are reported to that owner rather than silently producing results. SaveFailed is a failed
write after the round is Closed, not a partially applied round. “Faulted” in round guards names
RoundState.Faulted; all three failure contexts use the concrete §7.5 recovery actions. Running/paused/
delegated execution modes remain session/client control state, never durable mid-match save state.

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

#30 owns fixture preparation, engine/handle lifetime, private participation/card collection and
career finalization. MatchClientCore attaches playback/controller and pumps the domain observer; it must
not construct a second engine. #44 remains the disciplinary-rule authority. Domain assemblies must
not reference presentation/client assemblies: owner contracts carry data across this boundary.
Lend the #30-owned engine to its sole session driver once through an owned attachment; do not expose a reusable public
engine getter. Invalidate the handle after completion or preparation cancellation, reject stale
callbacks, and permit only one season command owner and one exclusive tick owner at a time.

Compose the admitted squads once and configure those exact instances at kickoff; no second
availability walk may change the XI shown in preparation. Boot order: squads and entry fatigue,
manager configuration, then kickoff tactic application in the managed team's home/away slot.
Use the player's chosen baseline for the Human-mode managed side and the explicitly frozen opponent
profile from the owner-selected deterministic variety mapping. Freeze goalkeeper-heading disabled for
career kickoff, matching current BootFixtureEngine; equivalent-run tests use that exact same flag. Automatic
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

DCT-01–20 in §7.6 are planned, not executed. Retain these v0.6 integration obligations alongside them:

| Lock | Required evidence |
|---|---|
| Shared round implementation | Every mode uses one apply block; fixture order, outcome count, engine count, table, finance/progression and cursor effects occur once. |
| T-IM-16 pre-change anchor | Pin old synchronous digest, all results, full save-byte hash, bans/appearances and counters before code. Attribute intended AI and earlier-validation changes. |
| T-IM-01 / DCT-07 full-save parity | Equivalent watched/headless controllers yield equal final digest, all results, participation, bans/appearances, counters and actual full save bytes, including managed-home/away mirrors. |
| Exhaustive mutation audit | Reflect over public loop/career mutators; every one belongs to the guarded list or explicit read-only allowlist. Test Open, Live, Faulted and cancelled-preparation restrictions. |
| Preparation cancellation / DCT-19 | No engine/result is committed; compare full save bytes and counters to a prepare/cancel control, then save/load/reprepare/play against uninterrupted play; day-steps/cursors and guarded mutations remain coherent. Healthy Live cannot cancel. |
| Atomic boot / complete | Inject roster/configuration failure before kickoff and result-write failures before/after the first write; verify distinct coherent/Faulted states and no unsafe retry. |
| Observation coverage | Miss first, middle and last ticks; hold the final observer open while requesting completion; refuse until it finishes and the tick thread has stopped, without deadlock or lost cards. |
| Eligibility and commands | Suspended back-fill bench refusal, injury-dependent rules, no pre-kickoff substitution, opponent-team refusal, home/away and eligible/used/sent-off/window/cap cases. |
| Source wiring / Unity | Direct asmdef references, no reverse domain/client dependency, source-backed routing tests and exact-source pinned Unity compile/EditMode plus actual UI checks. |
| T-IM-17 hot path | After buffer warmup, measure zero managed allocations in the command drain, record/participation collection and composed per-tick observers; bounded buffers must fail explicitly rather than truncate records. |

### 8.4a v0.6 lock disposition

All entries below are **planned obligations**, not implemented tests. Old labels retain their review
meaning; working operation names still require owning-contract approval. Locks needing SeasonLoop
and MatchSession run in ClientApp.Tests; client-only drain checks run in MatchClientCore.Tests.
Every team-sensitive case includes the managed-home and managed-away mirror. Controlled end seams
supplement real-match cases; they cannot replace the real-run replay/parity evidence.

| v0.6 lock | Current requirement / disposition |
|---|---|
| T-IM-01 | DCT-07 explicitly compares existing synchronous round resolution against interactive play; §8.4 full-save parity: equivalent-input watched/headless runs, full bytes and nonserialized counters. |
| T-IM-02 | Restored explicitly as DCT-13: repeated tick-stamped input replay. |
| T-IM-03 | Explicit DCT-14: interactive/synchronous finalizer equivalence; stronger positive controls. |
| T-IM-04 | Ordinary **live** abandonment withdrawn by D-11; preparation subset retained with explicit full-save/counter comparisons in DCT-19. |
| T-IM-05 | DCT-06 explicitly refuses premature/duplicate completion with unchanged full save bytes/counters; no duplicate career effects. |
| T-IM-06 | §8.2 / §8.4 exhaustive public-mutator audit, named refusals and guarded cancellation state. |
| T-IM-07 | DCT-11 / §8.3–8.4: first/middle/last observation gaps and faults block completion; healthy-live abandon recovery is replaced by the specified fault protocol. |
| T-IM-08(a) | DCT-05 / §8.3: starters plus incoming, off/sent-off players retained, no-substitution equals starters. Collection must work with career wired and discipline unwired; no dependency on a nonnull card fold. |
| T-IM-08(b) | Restored explicitly as DCT-15: tick-0 substitution/opponent-team drain rejection, logs and unchanged digest. |
| T-IM-09 | DCT-18 explicitly proves unfielded suspended-reinstatee ban serving, fielded-starter exemption and refusal; DCT-01 / DCT-15 and §8.3–8.4: suspended bench and injury-dependent admission; completion rejects ineligible participants and ban serving uses actual participation. |
| T-IM-10 | DCT-02 / §8.3: same admitted squads, entry fatigue and nondefault baseline on canonical kickoff, correct managed team, AI opponent with the same frozen profile from the varied club mapping. Compare after the first AI stride and prove setup adds no live command. |
| T-IM-11 | DCT-11 / §8.3 observer isolation. PM-1/browser keep set-once observer semantics and PostTickObserverFault; career composition isolates consumers and preserves actual analytics fault/cutoff. Reject duplicate integrity-consumer or post-start registrations. Existing PM-1 fault-reader tests must pass unmodified. |
| T-IM-12 | DCT-08 / DCT-16 and §8.3–8.4: final observation completes and tick owner terminates before result reads; stale observer callbacks fail safely without killing playback. |
| T-IM-13 | DCT-11 / §8.1–8.2: pre-stage roster failures and distinguish pre-write refusal from post-write Faulted. No automatic retry of partial application. |
| T-IM-14 | DCT-09 explicitly tests one-shot engine attachment; §8.5: refuse every save bypass and direct managed-day synchronous progress; retain bye/unmanaged/supported synchronous fallbacks. |
| T-IM-15 | Live abandon/save/load/replay withdrawn; DCT-19 retains full-byte prepare/cancel/save/load/reprepare/play comparison. §8.2 / §8.4 retain preparation-cancel/save/load/reprepare, idempotent day-steps, cursor checks and guarded focus/transfer/transaction mutations. |
| T-IM-16 | §8.1 / §8.4: old synchronous anchors, attributed AI/failure-boundary changes and existing recording-throw/retry tests. |
| T-IM-17 | §8.4: measured zero-allocation observation/drain after warmup, explicit bounded-buffer failure. |

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

### 8.6 Owning amendment application map

AR-3 replacement text and exact sites are specified in §7.10 above. No approved spec is changed by
this draft. Allocate ERR/FR/enum IDs against the application head; update outline, test traceability,
section-7 seam records, section-9 approval counts and version histories in that same application.

## 9. November 4 author review record (historical v0.8)

AR-3 independently reviewed v0.8 and failed it; see §10. The self-review below is retained as that
revision's record, not a current pass or an unfulfilled claim that no independent review occurred.

**Scope:** source-backed adversarial self-review of v0.7 against v0.6's 17 test locks and current
MatchClientCore/ClientApp behavior. This is not an independent reviewer verdict or technical approval.
v0.6's AR-1/AR-2 conclusions apply to that revision; they do not transfer automatically to this rewrite.

| Finding | Severity | v0.8 disposition / evidence |
|---|---|---|
| R-01: product confirmation versus technical approval was easy to conflate | Medium | §2.1 records exact earlier owner replies. DC-Q1 separates explicit adaptive AI from authorized archetype-0 default; automatic XI is retained v0.6 authority. Full technical approval remains due. |
| R-02: input replay lost an explicit lock | Medium | DCT-13 restores T-IM-02 with complete committed-state/digest/save comparisons and frozen handoff/controller evidence. |
| R-03: save parity retained but finalizer equivalence lost its old label and isolated subject | Medium | §8.4 already required full-save parity; DCT-14 separately restores T-IM-03's synchronous/interactive finalizer comparison and counters, with nonempty card/substitute controls. |
| R-04: drain refusal reduced to prose, losing log/digest assertions | Medium | DCT-15 restores exact T-IM-08(b) tick-0/opponent-team cases, FailedCommands checks and a positive control. §8.4a maps every old lock. |
| R-05: final-tick requests could attempt takeover after full time or silently lose commands | Medium | §7.3 / DCT-16 specify existing-operation completion and explicit pending-command outcomes. Current MatchClientDriver.Drain's ended-batch clear is source evidence of work required, not a completed fix. |
| R-06: shutdown limitation only in README | Medium | [Open issue](open-issues.md#career-shutdown-main-thread-wait) names Dispose/OnDestroy, owner, safe closure criteria and opt-in boundary. |

**Still open:** independent adversarial review of this revised contract; final owner technical sign-off;
#34 real-staff/eligibility/policy, #26 takeover, #41 medical mapping and no-eligible-staff release policy;
owner API/spec back-propagation; implementation of the planned locks and exact-source pinned Unity
compile/EditMode/runtime validation. A clean document check or existing P7b tests close none of these.

## 10. AR-3 independent review and v0.9 proposed disposition

**Received:** November 4, 2026. **Reviewed revision:** v0.8 at `4162e6d4`. Claude reported two fresh
reviewers plus source cross-checks: **FAIL, 4 High / 7 Medium / 9 Low**. This record preserves that
verdict. The rows below are author-proposed fixes for re-review, not independent closure or technical
sign-off. No runtime code changed. The owner answered H1/H2/Low-8 on November 4: defer delegation until staff, use the simulation
thread/shared queue, and vary opponent profiles deterministically. The follow-up answer explicitly defers interactive career matches too; PM-2 keeps Home resolution.
All four scope/architecture/profile answers are recorded in §2.2; technical approval is still separate.

| Finding | Proposed v0.9 disposition / remaining gate |
|---|---|
| H1 staff prerequisite | §7.7 names FR-ST-003 amendment, real roster versus designated slots, deferred staff prerequisite and NeutralHouseStaff semantics. **Owner chose to defer delegation until staff ships; #34 stays in its existing tranche. Interactive career matches are also explicitly deferred; current PM-2 Home resolution remains the route.** |
| H2 controller execution | §7.7 records the owner-selected simulation-thread/shared-queue option; #26 owns tactical AND substitution policy. Numeric policy amendment and technical review remain due. |
| H3 client result authority | §7.7 restores #30/#44-owned collection, private completion sealing and owned-handle finalization. §7.10 explicitly amends #44 §3.1/§4.3/§4.5. Client-built result/discipline payloads cannot commit. |
| H4 fault states/exit | §7.5 distinguishes critical SessionFaulted, post-write RoundFaulted and SaveFailed. Explicit exceptional recovery exit/reload breaks impossible-success waits; the recovery policy is PROPOSED rather than falsely owner-confirmed. |
| M1 missing lifecycle routes | §7.8 covers preparation/picker/window close, CancelPreparation, failed-save alternatives and delegated progress-only navigation. DCT-17 covers all contexts. |
| M2 pause/accelerated support | §7.8 requires a real pause/fence receipt after in-flight tick/observation and one-owner mode switching; §7.10 names match-viewer and client §6 amendments. Existing Pause flag is insufficient. |
| M3 weakened locks | DCT-07 explicitly compares existing synchronous round resolution; DCT-19 compares full save bytes/counters for preparation cancellation and save/load/reprepare; DCT-06 refuses premature completion; DCT-18 proves unfielded suspended-reinstatee ban serving; DCT-09 proves one-shot attachment. |
| M4 amendment specificity | §7.10 restores section/FR/KD/replacement obligations across #30 and cross-owners, including #37 §3.5. Normative application/ERR allocation remains separate. |
| M5 delegated kickoff tactic | §7.3 / §7.7 keep managed engine mode Human under owner-selected H2 option; only the external coach emits commands. ApplyKickoff therefore retains the admitted baseline. Test first-stride state and both team mirrors. The in-engine alternative was not selected; any later change requires another review. |
| M6 missing command/refusal shapes | §7.9 explicitly proposes transient handoff/outcome and typed refusal records, identifies owners and defers ordinal allocation. Earlier “no enum allocation” meant no numeric grant, not no new types. |
| M7 full-match gate cost | §7.11 budgets 22 conservative runs, including six ≥540s paced runs; required PR integration/nightly placement and proposed 120-minute budget need real runner measurements/CI application. No pre-commit claim. |
| L1 dropped preflight checks | §8.1 requires FR-SN-003 before staging and nonmutating paired appearance validation before any fixture apply; DCT-20 and the #30/#41 application rows cover both. |
| L2 preparation saving | §4 consistently refuses Open/preparation saves; CancelPreparation returns Closed/Home before saving. No preparation format or direct save action. |
| L3 source symbol / garbled row | Active source references use MatchClientDriver.Service; §4's failure row is rewritten completely. The prior §9 R-05 “Drain” name is a historical naming error corrected here. |
| L4 buffer bounds | §7.9 proposes 36 occupancy/participant entries, 10 substitution events and conservative 108 player-card events from existing owner limits; boundary/overflow tests and no truncation. #44 implementation/constant review still due. |
| L5 roadmap pointers/history | Roadmap maintained D-11/career-lane pointers advance to v0.9. The November 2 historical note is restored to its recorded v0.6 wording, explicitly labelled historical; a separate new dated row owns this revision. |
| L6 source baseline | §1 names actual P7b source `7f81e48a` and documentation head `4162e6d4`; `1d3cebf3` identifies inherited season code only. |
| L7 save refusal owner | §1 identifies CareerSession.Resume and its MarkDamaged path; storage alone does not impose the match policy. |
| L8 Aggressive default | §2 / §7.9 name Aggressive 0, Pragmatic 1 and Balanced 2. **Owner chose deterministic variety on November 4.** The proposed stable club-ID mapping and changed digest anchors still require technical review. |
| L9 goalkeeper heading | §7.9 freezes career goalkeeper heading disabled, as current BootFixtureEngine does, and compares exactly the same flag/config identity in parity/replay. |

**Next gate for the deferred feature:** independent re-review of this resulting revision;
resolve findings before owner technical sign-off and normative application. P7b code remains
unchanged and still owes pinned Unity 6000.4.9f1 validation. Existing P7b tests do not execute these
planned D-P7-6 cases or establish an AR pass. Recruitment remains deferred.

## 11. Carried forward from the parallel v0.8 draft (`148a01a`)

**Added:** November 4, 2026, at the owner's request, while this draft is PARKED. **Source:** commit
`148a01a` on branch `claude/eloquent-dijkstra-o8q4gh`, a separate November 3 revision of v0.6 that
kept the abandon-on-quit design. Its abandon routes, Q1 (opponent manager), Q2 (quick-resolve) and Q4
(player-facing resolution modes) are superseded by D-11 and §2 and are **not** carried. The items below
are the parts not already present in v0.9, re-checked against source at `73dfb63`. None has been
reviewed or approved; evaluate each on unparking, alongside §10.

| # | Item | Why it matters / where it would land |
|---|---|---|
| P-01 | **Substitution census lock and reopening trigger.** `MatchEngine.RequestSubstitution` (public, `MatchEngine.cs:1613`) is today the only caller of internal `SubstitutePlayer` (`:1672`, declared `:1775`). Add a source-scan lock (the `ClubFinancesCritiqueTests` folder-scan pattern) that fails if `SubstitutePlayer(` gains another call site in `src/match-engine/`, or `RequestSubstitution(` gains any call site inside `match-engine`. | Drain-side eligibility (DCT-15, DCT-18) holds only while every substitution into a career engine passes the drain, which the queued coach policy (§2.2) relies on. **Reopening trigger:** an engine-internal substitution caller (AI-manager or injury substitutions) moves the eligibility check into the engine, with snapshot and digest consequences. AR-3 noted this trigger was lost after v0.6. |
| P-02 | **#41 owns no selection logic.** Approved #41 FR-MD-023 defines `IsAvailable` as `Severity == None` and states "#30's squad selection reads it; #41 MUST own no selection logic". | §7.10's #41 row assigns injury-dependent CanStart/CanEnter to #41, which contradicts FR-MD-023. DC-05 needs either a #30 selection rule that reads #41 state, or an #41 amendment of FR-MD-023 (both the binary availability and the no-selection MUST), filed as an ERR. The parallel draft framed injured-only bench reinstatees ("tier 1") as a #30 rule for this reason. |
| P-03 | **Reuse an existing refusal status where it fits.** Pre-kickoff and ineligible-bench substitution refusals can return the existing `SubstitutionRequestStatus.BenchPlayerUnavailable` through `MatchEngineMutations.RequestSubstitution`, recorded as typed refusals (Gate J J-02). | Narrows §7.9's new typed-refusal record to the cases with no existing status (opponent-team commands, delegated-play refusals). Note: a throwing refusal is recorded as `Accepted` today (`MatchClientDriver.cs:141`), so those cases still need the typed record. |
| P-04 | **Cheap test hosting.** #30-owned locks drive a raw-tick handle in `SeasonSave.Tests` (engine from the one-shot handoff, `RunTick`, then `ObserveTick`), so `season-save` grants no new `InternalsVisibleTo`. Most locks use a stated tick window or a hand-ended engine via internal `MatchEngine.TestOnly_CheckMatchFlowTransitions(MATCH_TICKS_TOTAL)` (`MatchEngine.cs:2329`; `match-engine` already grants `SeasonSave.Tests` and `ClientApp.Tests` internals). Real full matches only for parity and the pre-change anchor. Client bench-refusal tests use hand-built engines and ids, because `AvailabilityCompositionExtremisTests` fixtures are internal to `SeasonSave.Tests`. | Directly reduces §7.11's proposed 22-full-match budget (AR-3 M7). |
| P-05 | **Pre-change anchor convention.** Capture the synchronous `ManagedThroughEngine` values at the head immediately before the code landing using the `LeagueBootstrapGoldenVectorTests` convention, with the SHA recorded. Existing `SeasonLoopTests`, `SeasonLoopCareerTests`, `SeasonLoopDisciplineTests` (including its M12 retry test) and `AppearanceRecordTests`' recording-throw test must pass unmodified. | Makes T-IM-16 (§8.4a) concrete and names the regression set. |
| P-06 | **Kickoff comparison conditions.** When comparing a career boot with a PM-1 `MatchSession(MatchSetup)` boot: use the fixture seed as the setup seed, `GkHeadingEnabled` false, and zero entry fatigue (PM-1 boots with the two-argument `ConfigureSquads`; `BootFixtureEngine` passes the training projection). | v0.9 §7.9 pins GK heading only; the entry-fatigue mismatch would make DCT-02 / T-IM-10 compare unlike boots. |
| P-07 | **Reflection mutator audit.** T-IM-06's refusal lock fails if a public instance method of `SeasonLoop` or `PlayerCareerStates` is on neither the refused list nor the read-only allowlist (which holds `TrySetFocus`). | Keeps §8.2's guard list exhaustive as those types grow. |
| P-08 | **Further #30 amendment sites** missing from §7.10: §1.3 interface rows (#26 kickoff and `ConfigureManager` caller, #21 `SetTeamTactic`, client driver calling tick-thread `ObserveTick`; `TacticalInstructions` among `season-save`'s references); §1.5 row 3 (the interactive tick loop is client-driven); §5.1 and §6.1 (the `ObserveTick` exception to "never on the match loops", and its allocation lock); §7.3's #43 row (P-09). | Completes the site-level list AR-3 M5 asked for. |
| P-09 | **Competitions (#43) generalisation.** The seam covers one league round per loop. #43's merged fixture-day driver (KD-5, FR-CP-019) is where it generalises to "a managed fixture day is open", carrying fixture identity and competition key. Record this as a #30 §7.3 row rather than widening the preview now. | Prevents a league-only seam from blocking cups (D-13). |
| P-10 | **Away-managed client sites.** These assume team 0 and must take the managed team index, or an away-managed fixture commands and labels the opponent: `ClientChangeRequests` (refuses `TeamId != 0`, `ClientChangeRequests.cs:56,107`), `S0ScreenPresenter`'s tactic and substitution commands, bench identity and used-bench checks, choice lists, keeper greying and marker side, `ClientMatchContext`'s baseline tactic, and the demo-only `S0DemoFixture.Bind`. Lock: on an away-managed session, presenter-driven commands reach the managed team, never the opponent. | Concrete checklist for DCT-07's home/away mirror on the client side. |
| P-11 | **Coordinator teardown hazard.** Every post-start failure path in `ClientMatchCoordinator` (`StartMatchCore`'s catch, `Refresh`'s catch, `RejectAttachment`, `Dispose`) ends in `ClearOwnedMatch`, which stops the session but tells the season side nothing. A career-attached coordinator must route each path to the round's defined terminal or fault transition (§7.5), never leave it Live. Lock: an injected throw at each post-kickoff step leaves a defined round state. | v0.9 defines fault states but not this client-side source of silent exits. |
| P-12 | **Future starting-XI choice.** DC-07 keeps the automatic XI. A later manual XI would need the ineligible set and preview recomputed at kickoff, and a #30 §3.4 / #44 §2.3 rule on selecting reinstated players (the ERR-030-046 class). | Records the dependency so a future XI feature does not reopen the back-fill guarantee unnoticed. |
| P-13 | **Wiring-backlog record.** At the code landing, the `match-engine-wiring-backlog.md` W7 entry should record the career path's opponent-manager configuration rather than "WIRED" alone. | Keeps KD-W1 tracking accurate. |
| P-14 | **Independent of D-P7-6, fileable now.** #38 Appendix C's `Substitute` row (`ui-client-framework/appendices.md:54`) cites internal `MatchEngine.SubstitutePlayer(...)`; the public seam is `RequestSubstitution` through `MatchEngineMutations` (`match-client-core/MatchEngineMutations.cs:49`). | A spec-text defect in approved #38; candidate ERR-038 entry. Not filed here. |

## Review history

| Version | Date | Change |
|---|---|---|
| 0.11 | 2026-11-04 | Still PARKED. New §11 carries forward 14 items from the parallel v0.8 draft `148a01a` that v0.9 lacked (census lock, #41 FR-MD-023 selection ownership, refusal-status reuse, test hosting and anchors, boot comparison conditions, mutator audit, #30/#43 amendment sites, away-managed client sites, coordinator teardown, future XI, W7 record, an independent #38 Appendix C defect). Re-checked against source at `73dfb63`; unreviewed. Superseded abandon/Q1/Q2/Q4 content not carried. |
| 0.10 | 2026-11-04 | **PARKED by owner instruction.** Status only; technical content identical to v0.9. The owner confirmed the §2.1/§2.2 quotations as their own answers. AR-3's findings against v0.8 stay recorded in §10; v0.9's proposed dispositions have not been independently reviewed. Resume (re-verify baseline, then independent re-review) when #34 T0 is scheduled. |
| 0.9 | 2026-11-04 | Record failed independent AR-3 and propose domain-owned results, explicit fault/exit/pause contracts, restored stronger locks, precise amendment map, bounded records and full-match gate budget. Record owner answers: delegation deferred until staff, external queued policy and deterministic profile variety; interactive career play is also deferred and PM-2 retains Home resolution; re-review/sign-off required. |
| 0.8 | 2026-11-04 | Trace product answers, restore explicit replay/finalizer/drain locks, map every v0.6 lock, specify full-time request outcomes and record author self-review. Independent review and technical approval remain open. |
| 0.7 | 2026-11-04 | Consolidate confirmed selected-coach delegation, no in-match saving, injury-dependent eligibility and session records into the canonical draft and roadmap D-11; replace ordinary live abandonment, retain round/observer/guard obligations and identify owning amendments. Technical approval remains due. |
| 0.6 | 2026-11-03 | AR-2 Medium/Low fixes (M7, M11–M21, L4–L10). **Interactive route (M11–M13, M17):** new `PlayManagedMatchday` action routed to Tactics Setup, `PlayMatchday`/`AdvanceRound` refused on managed fixture days, Q2 gains option (d) and the Q1 (a) × Q2 asymmetry; `ManagedTeamIndex` on preview and handle, with the career-attached drain refusing the other team's commands; `Ended` dropped as a stored state; post-abandon mutators refused until the round is played. **Round integrity (M14, M21, L8, L10):** the synchronous path stays stateless and Faulted is interactive-only, per-mode resolution placement and the moved F6/F9 failure point stated, `Kickoff` atomic, FR-SN-003 plan check, lock-free fault read, join enforced in `CareerSession.CompleteMatchday`/`AbandonMatchday`. **Specs, ownership, locks (M7, M15, M16, M18, M19, L4–L7):** §4 gains the FR-SN-025/§1.2, FR-SN-013b, §4.1/§4.2, §7 and outline sites; locks split by owner and host; #38 Appendix C and the ERR-044-010 closure move to the code landing; §2.7 split with new Q3 for tier 1 and the August 17 decision cited; XI-selection claim replaced; tactic-commit rationale, T-IM-10, the #44 quote, W7 and `IneligibleToEnter` corrected. **Client facts (L9):** `Execute` refuses `MatchdayInProgress`, `IsProgressBlocked` source change, autosave rationale dropped. |
| 0.5 | 2026-11-03 | AR-2 High fix (H4). v0.4's §2.8 installed the composite observer on every `MatchSession`, which would have left `Streamer.PostTickObserverFault` null for good on PM-1 and the web host (its readers `MatchClientHost.AnalyticsFault` and `S0ScreenPresenter.cs:504` would go blind) and broken more tests than the one it named, while §6 said PM-1 was unchanged. Option (a) adopted: `MatchSession.Attach` alone installs the composite; `AttachTickObserver` registers an `After` consumer on a career-attached session and stays set-once elsewhere; `AddTickConsumer` is refused on other sessions. The career route keeps a fault signal (the analytics adapter's own `_incomplete` latch, `TickConsumerFault(After)`, `IsObservationFaulted`), and its match screen reads the session surface where `S0ScreenPresenter.cs:504` reads the streamer. Option (b) recorded and rejected. §1 names the readers and tests; §2.4, §2.8, §6 and T-IM-11 aligned; T-IM-11's `Complete` outcomes move to ClientApp.Tests, the host that reaches both `SeasonLoop` and `MatchSession` (§5). |
| 0.4 | 2026-11-03 | AR-1 Medium/Low fixes (M1–M10, L1–L3). **M1–M2 (round integrity):** new §2.9 thread contract (Complete/Abandon on the season thread after `MatchSession.Stop()` joins, `MatchEnded` read after the join, handle lock and invalidation, one engine in flight on the process-static bus); new §2.4.1 makes Complete apply-only, with every fallible per-fixture resolution precomputed at `BeginRound`, a Faulted state (Abandon refused, cause surfaced), and `FullEngine` refused for the interactive route; T-IM-12, T-IM-13. **M3–M4 (one implementation, participation):** §2.2 splits `PlayNextRound` into internal `BeginRound`/`CompleteRound` for every mode, with `BeginManagedFixture` as a narrower wrapper; T-IM-16 pins pre-change values; §2.5 defines participation (boot occupants plus incoming substitutes), moves it to a #30-owned `ParticipationRecord` fed by the handle's single pass, and §2.6 refuses pre-kickoff substitutions at the drain; "not a second walker" withdrawn. **M5–M6 (saves, #44):** §2.10 makes `Save(SeasonLoop, …)` refuse while a round is open, replaces `handle.Engine` with one-shot `TakeEngineForSession()`, refuses `CaptureSave` on a career-attached session, specifies the `CareerSession` operations, `MatchdayInProgress` and the post-abandon Closed state (T-IM-14, T-IM-15); §2.5's #44 item restated as conformance to FR-DC-011 / #44 §4.5 that closes ERR-044-010, with only #41 sign-off kept. **M7–M8 (normative sites, routes):** §4 names each #30 site with its replacement sentence under one ERR-030 entry and maps the locks to a T-SN-IM block in §5.9/§5.8; new §2.11 route table, D-P7-3 restated with the bye and unemployed fallback, #38 Appendix C rows; new owner question Q2 (quick-resolve for the managed fixture) with a recommendation. **M9–M10 (faults, refusals):** the handle exposes `IsObservationFaulted`, the client polls it and offers only abandon at once (§2.8); T-IM-07 extended to injected faults and first/last-tick gaps; §2.3 derives the refused-operation list from the real public surface, with a reflection check in T-IM-06. **L1–L3 (precision):** §1 corrected (day-steps include slot 1, the quick-sim row is mode-specific, "sim thread" split into season thread and tick thread, `CareerSaveSlots.cs:217` is the operative site, the fold's check scope, the new tick counter); locks compare full save bytes and counters, state their host assembly and real-match cost (§5, §7); §4 item 11 amends the §4.3 hot-path note with T-IM-17, and §2.1 aligns save wording (manual save on Career Home, autosave after Complete). |
| 0.3 | 2026-11-03 | AR-1 High fixes (H1–H3). **H1:** §2.5's "a suspended player is never on the bench" was false (the ERR-030-044..046 back-fill reinstates suspended and injured players onto the bench); new §2.7 makes back-filled bench players ineligible to enter (option (a); (b) recorded and rejected), refused at the `match-client-core` drain with a season-side `Complete` backstop and an ERR-030 obligation; T-IM-09. **H2:** `Begin` no longer boots the engine; it computes a pre-kickoff preview, and `Kickoff(PreKickoffConfig)` boots through a compose/configure split of `BootFixtureEngine`, applying the managed tactic on the #26 FR-TP-004 boot path instead of a "tick-0" command; §2.3 states and T-IM-01/04/06 aligned; #26/#21 named in §4/§6; new owner question Q1 (career opponent AI manager); T-IM-10. **H3:** new §2.8 composite per-tick observer owned by `MatchSession` with per-consumer fault isolation, season observation first; T-IM-11. §1 gains the facts each fix rests on. |
| 0.2 | 2026-11-02 | Owner decisions applied: no starting-XI changes (v0.1 Q1, answer (a)); no mid-match saves; quitting a match abandons it and returns to Career Home with the round unplayed; no kickoff autosave (v0.1 Q2/Q3 withdrawn). Removed: the managed-fixture sub-blob, season-save format 8 → 9, restore path and live-save locks, and the managed-first round order (index order kept; headless path unchanged). Added: `AbandonManagedFixture`, the abandon lock, save unavailability while a round is open. Size back to about "L". Not yet adversarially reviewed. |
| 0.1 | 2026-11-02 | Initial draft, from the November 2 review of the client-design bullet contract: substitutions vs starting-XI consumers, unserialized card fold, engine/observer ownership across assemblies, round order and states, save-policy owner questions. |
