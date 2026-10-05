# System XI — P5b Lifecycle and Identity Implementation Plan

**Created:** October 5, 2026\
**Last Updated:** October 5, 2026\
**Version:** 0.1\
**Status:** IMPLEMENTATION PLAN — source audited; implementation and validation pending\
**Purpose:** consume the existing match lifecycle in the Unity shell and renderer, with the approved S0 demo identity and real match/report data.\
**Baseline:** `main` at `f276f700acf534f470a3a2abc9649f88b55fdbda` (#470 merge).

## 1. Authority and completed foundation

[S0 journey §14](../design/ux-s0-pm1-journey.md#14-gate-i--p5b-implementation-handoff)
and [binding contracts §§2–5](../design/ux-s0-binding-contracts.md) govern behavior.
The [detailed UX plan](../design/ux-detailed-plan.md) defines Gates I/J;
[client design](interactive-unity-client-design.md) §12 governs decision placement.
This plan chooses implementation boundaries within those contracts. Proposed types and
members are TO BUILD unless the source-audit table says they exist. It grants no new
product scope, host acceptance, Gate-J pass or permission to merge an implementation.

#470 merged October 5 at `f276f700`, from head
`509a9f5dc43878d134d4356146bc313ebe8062b5`. PR CI run
[37264147436](https://github.com/antonzymin-eng/Soccer-Manager-Pro/actions/runs/37264147436)
succeeded, including the Linux functional gate. Unity CI tests were skipped for lack
of a configured license. The recorded owner-host evidence covers Unity 6000.4.9f1
compile with zero errors and ClientApp EditMode 26/26. GitHub comparison reports no
file differences between that head and the merge. This is foundation evidence,
not acceptance of the complete journey or proof of the code planned below.

## 2. Scope and delivery boundary

The next implementation PR consumes `MatchSessionLifecycle` and removes the renderer's
internal demo session. It includes the approved distinct demo fixture, frame-owned
occupant identity, renderer attachment/cleanup, analytics publication, and host-free
setup/full-time/report lifecycle decisions. Every new producer has a consumer and
outcome tests in that PR; no second unconsumed lifecycle prerequisite is introduced.

The later copy/scale/screens PR consumes #49 L2 and completes the shipping UGUI
presenters, localized copy, 100–200% text reflow, command-outcome presentation,
choosers and focus behavior. The two implementation PRs may be combined if L2 is
available; localized screens cannot land ahead of it. A separate lifecycle/identity
landing must expose no incomplete player-facing Start/report journey. Exercise its
real composition through tests and a development host fixture until the complete
screen consumer lands. Existing menu/setup navigation remains the foundation surface.

In particular, **full time does not automatically navigate to the report**. The
accepted ended frame enters frozen Match View (MV-FT). The player activates
View match report to invoke `ShowPostMatchReport()`; Return then clears the match
and invokes `ReturnToMainMenu()`. Formation, individual tactics, save/load, rematch,
abandon, settings, career, audio and additional analytics remain outside S0.

## 3. Audited source and environment

| Existing source | Confirmed boundary at the baseline | Required change |
| --- | --- | --- |
| `match-client-core/MatchSessionLifecycle.cs` | `CreateSession(setup)`, `HasCurrent`, `Current`, `ClearSession()`; creation does not start playback; replacement stops the running predecessor | Shell coordinator consumes this single lifecycle; do not add another session owner |
| `match-client-core/MatchSession.cs` | Configured-squad boot; `AttachTickObserver` before Start; streamer, driver and enqueue-only commands; `Start`, `Stop`, `ServiceOnce`; direct engine-derived time/end reads are unsuitable during playback | Copy a boot descriptor before playback; running presentation reads accepted frames |
| `client-app/ClientScreenFlow.cs` | Five named guarded moves; private navigation shell | Retain one flow for shell lifetime; do not recreate history to change a session |
| `ui-framework/ScreenId.cs`, `NavigationShell.cs` | Readonly source/dispatcher registration; registering the same id again throws | Register stable session-aware handles once; change their privately held match context at lifecycle boundaries |
| `match-client-unity/ClientShellBehaviour.cs` | Validates roots, applies visibility and forwards only Open/Cancel; null registrations are stubs | Bind the host-free coordinator; replace stubs with real typed handles as their consumers land |
| `match-client-unity/MatchClientBehaviour.cs` | Validates wiring in Awake; opt-in constructs NeutralDemo, builds visuals, starts in Start, stops in OnDestroy/rejection; latch is currently lifetime readonly | Add external Attach/detach; initialize safely before Awake; fresh latch and visuals per attachment; remove playback ownership and demo fields |
| `match-engine/MatchEngine.cs` | `PlayerIdsByAgentId()` copies boot/current identity; substitution changes `_slotPlayerIds`; no scalar `AgentPlayerId` getter | Add the reviewed read-only scalar observation with its frame consumer; preserve boot-copy cadence |
| `match-viewer/LiveMatchStreamer.cs`, `LiveAgentCue.cs` | Capture under tick gate samples discipline, bench-origin and keeper cues; no player id | Add current PlayerId in the same frame, and update every constructor/test producer |
| `match-client-core/MatchRoster.cs`, `MatchRenderProjection.cs`, `LiveFrameLatch.cs` | Team metadata, slot shirts and interpolation exist | Resolve S0 authored shirts from accepted frame identity; snap positions on occupant change; retain explicit neutral-client policy |
| `ui-framework/MatchViewModelSource.cs` | Existing frame-backed screen source | A fresh concrete source per match behind the stable shell handle |

The checked-in project root is the repository root (`Assets/`, `Packages/`,
`ProjectSettings/ProjectVersion.txt`). Unity is pinned to **6000.4.9f1
(f7258d6eebbe)**. The package manifest confirms Unity Test Framework 1.6.0,
IDE integrations and AI Assistant; no Unity MCP package is declared there.
The Unity README records Built-in rendering and legacy Input Manager for the host;
these are documented host settings, not independently inspected live settings in this
planning session. `Assets/Scripts` → `src` junction is recorded owner-host setup.
No Unity Editor connection, Console, Build Settings or profiler capability is available
in this session. Current live host configuration and player-build readiness are unverified.

Production boundaries remain engine → captured observations → host-free client models
→ thin Unity bindings. `client-app` currently references only `ui-framework`; the
coordinator will need explicit downward references to its consumed lifecycle and
fixture types. `match-client-core` owns generic session/roster/analytics adapters;
`client-app` owns the concrete S0 fixture, screen decisions and request presentation.
Add only actually consumed asmdef references, update structural dependency locks, and
keep simulation assemblies free of client/UI dependencies. Do not reference
`match-client-web`; its observer locking is precedent, not a production dependency.
Follow `src/CLAUDE.md`: constructor injection, one primary public type per file,
XML contracts, file headers and append-only version histories. No mutable global context.

## 4. Lifetime and attachment design

### 4.1 Shell context and stable registrations

One shell-scoped coordinator owns the existing lifecycle and navigation flow. Stable
typed source/dispatcher handles are registered once; each reads its current privately
installed match context. The context holds the concrete frame source, dispatcher,
identity resolver, analytics adapter and later outcome presenter. Before attachment,
match reads return the defined waiting/unavailable model and match dispatch is refused.
After clear, no handle can reach the previous concrete dispatcher. This keeps the
framework's registration contract and five navigation moves intact.

Use shell-local, monotonically advancing attachment identity to invalidate queued
callbacks. It is presentation state, never a simulation input, save field or RNG draw.
Callbacks carry their captured context and must be ignored when it is no longer current.
Dispose/unsubscribe owned callbacks on clear; never resolve an old callback through
the replacement's `Current`. UI/lifecycle operations are serialized on the client thread.

### 4.2 Start transaction

1. Accept a Start only from setup with no start transaction or current running match.
   Validate admitted fixture and draft; create from Balanced and replace only home
   Mentality, preserving all other tactic axes. Use seed 1, home Human, away AI/default
   profile and heading disabled. Cancel/re-entry resets the draft without a session.
2. Call `CreateSession(setup)` once. Capture the configured engine boot descriptor
   before any tick, and install exactly one analytics observer through
   `AttachTickObserver`. Create fresh match-local sources, identity resolver, latch,
   report state and later outcome/disclosure state.
3. Attach the external session and identity context to the pitch renderer and screen
   handles. Validate all attachment results synchronously; no live engine getter polling.
   Then call `ClientScreenFlow.StartMatch()` and begin playback once, after attachment.
4. Repeated activation during the transaction produces no additional session, observer,
   navigation move or playback start. Failure before playback invalidates the context,
   detaches partial visual construction and clears the newly created session. Treat
   invalid wiring/content as developer integration failure, not a new player retry UI.
   Handle failure after navigation as terminal shell rejection with the same cleanup;
   do not invent a Back/abandon edge to roll the graph back.

### 4.3 Renderer ownership and Unity lifecycle

`Attach(MatchSession)` is the proposed external-session seam on the **pitch renderer**,
not a method invented on `MatchSessionLifecycle`. Supply immutable identity context
alongside attachment using an explicitly consumed typed binding input. The renderer
owns its generated visuals only; the shell coordinator owns playback and session clear.

Attachment must work while Match View's root is inactive and before Unity invokes that
component's Awake/Start. Use idempotent explicit initialization shared by Awake and
Attach, keep unconditional wiring validation, and ensure later Awake/Start cannot
reinitialize a valid attachment or start playback. A rejected renderer refuses Attach
synchronously. An asynchronous render failure reports to the coordinator, which stops
the owned session; do not lose the existing no-background-playback-on-rejection guarantee.

Build generated objects under an owned container per attachment; detach nulls held
session/context references and clears latch/arrays/camera state immediately, hides the
container, then destroys only generated objects. Deferred Unity Destroy must not leave
old visuals visible during same-frame reattachment. Detach is idempotent and safe after
partial build. Preserve authored prefabs, scene children, camera and materials.
Turning Match View off to show the report changes visibility, not match ownership.
Component disable/enable must neither stop/start the match nor rebuild an attachment.
Destruction of required renderer wiring triggers coordinator teardown; destruction
ordering and shell destruction must also work when objects are already gone.

Remove `_autoBootDemoMatch`, `_demoSeedText`, demo parsing and NeutralDemo construction
from this renderer, its scene opt-in and documentation together. Update the isolated
demo scene to an explicit external host composition that consumes the same fixture and
lifecycle; do not restore an implicit boot in another MonoBehaviour. Retain its existing
prefab contract and verify the scene can still exercise the renderer.

## 5. Identity and analytics consumed in the same landing

Use the exact approved 36-name, 18-player-per-team fixture in binding contracts §2.1,
including authored shirts and its stated existing numeric defaults. Do not use fixture
order as the engine's lineup order. Admission checks cover unique ids across teams,
complete names, valid positions/attributes and unique positive shirts within each team.
The real engine must produce each XI and seven bench entries. No extra RNG or tuning.

Copy the boot slot/bench identity once into a privately owned descriptor. Add scalar
`AgentPlayerId(agentId)` for valid on-pitch slots and capture it in `LiveAgentCue` with
the other cues under `_tickGate`. Zero is valid; neutral matches use `NO_PLAYER_ID`.
Keep every cue constructor producer explicit, including browser/test fixtures. This
adds observation data, not serialized simulation state; verify schema/RNG neutrality.

Acquire one accepted frame for a complete screen/pitch refresh. Resolve name/shirt,
keeper/substitute and outgoing choices from it; bench-origin entries come from the
descriptor. Exclude used bench indices by current BenchSlot cues, not player-id
deduplication. When current/previous player ids differ, use current identity and snap
that slot's position; unchanged identities retain interpolation. Keep the browser's
neutral slot-shirt policy explicitly supported. Retained command/report identities
must be immutable snapshots, never re-resolved against a later occupant.

The consumed analytics adapter attaches before any tick and uses #37's read-only
observation and aggregator. Serialize ObserveTick and result/cutoff publication under
one adapter lock. On failure, mark publication incomplete before releasing that lock,
then rethrow so the streamer disarms/latches its observer fault. Publish immutable
snapshots; track attempted tick, completed observation and observed-tick count. Never
hold the analytics lock while Stop/ServiceOnce or a streamer gate is acquired.
Consume the publication in the host-free report context in this landing and the final
screen binding later. Score/end state always comes from the frame; partial analytics
cannot freeze playback, redefine the result or add a retry. Presentation row/copy
rules remain journey §14.6 and the later screens implementation.

## 6. Full-time and teardown barrier

On the first accepted ended frame, lock further input and close unsubmitted staging.
Stop/join paced playback; perform **one** post-end `ServiceOnce()` to drain/drop queue
residue under the existing engine end guard. Read the now-stable logs, preserve any
evidenced Applied/Refused outcome and settle only unmatched Pending as Not applied.
Freeze the final frame/report context and enter MV-FT once. This barrier has a consumed
host-free controller test in this landing; the full command adapter and player copy
must land before feedback/chooser controls ship in the later screens slice.

Do not service gameplay requests while paused, advance a tick from projection, or mix
paced Start with TickOnce. At report acknowledgement invoke the named report move.
On Return or shell/application destruction: stop/quiesce any owned driving first,
invalidate context/callbacks, detach all bindings, then `ClearSession()` and release
match-local caches, sources and disclosures. Return uses the named Pop to Main Menu;
new setup starts Balanced. Stop-before-Start does not revoke externally held session
references, so clearing every owned reference and callback is an explicit obligation.

## 7. Implementation order and checks

| Step | Implement and consume together | Evidence required before this implementation merges |
| --- | --- | --- |
| A | Approved fixture + setup builder + boot descriptor; scalar engine identity → frame cue → S0 identity/render projection | Real boot XI/bench, home/away and substitution mapping, sentinel/zero-id, observer neutrality, unchanged neutral reference behavior |
| B | Analytics publication + final report context; coordinator start/full-time/clear + stable registration handles | Observer installed before first tick; forced mid-tick failure atomically incomplete; single Start, real end barrier, explicit report action and repeat-session isolation |
| C | Unity external Attach/detach + shell binding + isolated scene conversion; remove all internal demo ownership | Pinned exact-head compile; attachment before Awake on inactive root, rejection/partial-build cleanup, no hidden session, repeated detach and two matches without duplicate objects |
| D, after #49 L2 | S0 formatter/admitted content + scale + four presenters + outcomes/choosers/focus | Journey I-Q01–19 across assigned lanes, shipped catalogue identity and invalid-pattern admission, real 200% layout, host/cert evidence |

A–C are implementation steps within one consumed lifecycle/identity PR, not separately
mergeable unconsumed prerequisites. Linux gate is the host-free test authority; Unity
compile covers the excluded binding. Fixture setup can change match outcomes: record
fixture revision/content SHA-256, setup and exact head, prove same-setup repeatability,
and compare with the former no-squad setup at otherwise identical inputs. Report the
differences; no calibration or replacement of the frozen engine corpus is authorized.

Meaningful regression coverage includes: all seven Mentalities preserve other axes;
cancel has no session; actual source/dispatcher handles follow the second session;
frame identity remains old until the replacing frame despite an earlier log; keeper
replacement and used bench-origin duplication; skipped/repeated frames; immutable old
request/report labels; late callbacks from the old context; full-time queue residue
without extra ticks; incomplete analytics while score/time continue; attachment and
teardown failures. Use real existing seams and test outcomes rather than parallel fake
selection/identity algorithms. Add controlled fault injection only at the narrow seam
needed to test cleanup, without changing normal gameplay.

For each implementation PR, run relevant structural/dependency tests, C# format and
documentation checks, then its fresh Linux functional gate. On the pinned Windows 11
host compile the exact PR head before merge and record Unity revision, source tree,
warnings, relevant EditMode counts and the PlayMode attachment/teardown scenarios.
Carry known warnings/root-discovery limitations honestly. Record merge head afterward.
Gate-J/B8/B9b/B10 performance and complete journey acceptance remain separate until
their actual cases run; editor FPS is never cert evidence.

## 8. Planning result and outstanding evidence

Source inspection confirms the implementation seams and dependency boundaries above.
No production source, Unity asset, package or approved reference image is changed by
this plan. No implementation test or Unity runtime case has been executed for it.
All planned QA retains its original PLANNED status; #49 L2, full screens and B8/B9b/B10
acceptance remain open. Next action is implementation A–C against the then-current
main, followed by exact-head Linux/Unity validation. Detailed class signatures and
actual build/scene results belong to that implementation review.

## Version History

| Version | Date | Notes |
| --- | --- | --- |
| 0.1 | October 5, 2026 | Source-audited plan after #470 merged: consumed lifecycle/identity/analytics boundaries, stable registrations, external renderer attachment, explicit report acknowledgement, teardown and pre-merge evidence. Implementation pending. |
