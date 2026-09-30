# System XI — S0 PM-1 Journey Packet

**Created:** September 12, 2026  
**Last Updated:** September 30, 2026\
**Version:** 0.15\
**Status:** S0 A–F COMPLETE — G PASS FOR v0.5; H OPEN; I BLOCKED (§12.3)\
**Execution authority:** [`ux-detailed-plan.md`](ux-detailed-plan.md) v1.19 §5–§6\
**Validation task authority:** [`ux-validation-protocol.md`](ux-validation-protocol.md) v0.22\
**Evidence snapshot:** Gate A — `main` at `ad7e0d751f978c8785e7bab2024b99ff5a8da26d` (PR #406 reconciliation base); Gate B — `main` at `ee37aa60` (September 28, 2026)

---

## 0. Purpose

This is UX-D, the journey packet for S0: PM-1, play one match end-to-end. It starts the packet with the
binding Gate A contract/dependency audit. Later revisions add Gates B–J to this same artifact rather than
creating parallel journey documents.

S0 remains the current four-screen product graph:

`launch → Main Menu → Tactics Setup → Match View → Post-Match Report → Main Menu`

This packet does not redesign that graph, authorize P5b implementation, add a career shell, or treat a
prototype as production capability. Gate A answers only whether every S0 control/data requirement has a
verified owner or an explicit missing-surface classification.

### Gate A pass rule

Per `ux-detailed-plan.md` §5, Gate A passes only when the owning system, read/data seam, action/command seam,
UI adapter/projection, screen/navigation state, rendering/host state and missing surfaces are explicit for
every proposed control/data element. No unsupported control may become real by appearing in a mockup.

---

# 1. Journey and validation-task coverage

The binding S0 task set is unchanged:

| Task | Player outcome | Gate-A contract area |
|---|---|---|
| S0-T1 | Reach Tactics Setup from launch/Main Menu using the supported match-entry path | root screen, match-entry edge, launch/shell binding |
| S0-T2 | Make one understandable pre-match tactical choice and start | `MatchSetup`, tactic vocabulary, session creation, start edge |
| S0-T3 | Read score, match time/state and current playback speed | live frame projection, clock presentation, playback state |
| S0-T4 | Change playback speed and make one in-match tactical adjustment | speed ladder/streamer controls, manager-intent dispatcher |
| S0-T5 | Open/close live statistics and identify a useful statistic | #37 analytics owner, stats presentation state |
| S0-T6 | Recognize full time, read result/summary and return to Main Menu | full-time state/gating, analytics result, report/return edges |
| S0-T7 | Cancel Tactics Setup without starting, then re-enter | guarded cancel/back and re-entry edges |

PM-1's roadmap exit criterion requiring a live substitution is also audited even though S0-T4 asks the
participant for one tactical adjustment. Gate A cannot omit a shipping PM-1 control merely because the
formative task set exercises a smaller representative action.

---

# 2. Evidence correction since the F1 baseline and original #406 snapshot

The F1 baseline correctly classified P4b as `HOST-UNVERIFIED`, but both that baseline and the original
September 12 Gate-A snapshot now understate the available host evidence. PR #404 first proved the repository
compiled in Unity 6000.4.9f1 after a forced `Assets/Scripts` reimport with **0 compile errors** and successful
assembly reload. The September 13 B8 follow-up then advanced P4b to **partial host verification**: the tracked
`Assets/Scenes/Scene.unity` wires `MatchClientBehaviour` to the eight placeholder prefabs and pitch surface,
and Play mode boots without wiring rejection with 22 markers, 22 possession rings, ball + shadow, 27 marking
drawables and a follow camera; the editor observation was about 220 FPS.

That still does **not** make P4b `HOST-VERIFIED`. Click-to-select is not yet a shipping interaction because
`HandleClick` only logs the ground point until P5b routes it to a command, and the budgeted cert-host
render-loop/performance capture remains open. This packet therefore uses the current evidence statement:

**P4b = landed + Unity-compiler-verified + scene-boot/render-smoke-verified / cert-host acceptance incomplete.**

The existing `HOST-UNVERIFIED` qualifier remains until the full host certificate closes. Gate A may rely on
the real render/runtime smoke evidence, but it may not promote that evidence into Gate-J acceptance.

---

# 3. Gate A — contract/dependency/control audit

## 3.1 Navigation and screen ownership

| ID | Proposed S0 surface | Owner / verified seam | Adapter / screen / host state | Gate-A classification | Constraint carried forward |
|---|---|---|---|---|---|
| A-01 | Main Menu identity/root | `ClientScreens.MainMenu`; `ClientScreenFlow` roots `NavigationShell` at Main Menu | typed identity and root behavior live; P5b UGUI screen absent | `DESIGNABLE / UNWIRED` | S0 may expose only the supported PM-1 entry path; do not make career/Continue/Load/Settings controls look live |
| A-02 | Main Menu → Tactics Setup | `ClientScreenFlow.OpenTacticsSetup()` (`Push`) | legal edge live; no Main-Menu-specific source/binding | `DESIGNABLE / UNWIRED` | binding forwards to the named move; no shell poke or duplicate nav rule |
| A-03 | Tactics Setup cancel/back | `ClientScreenFlow.CancelTacticsSetup()` (`Pop`) | legal edge live; P5b control absent | `DESIGNABLE / UNWIRED` | this is S0-T7's only current cancel/back behavior |
| A-04 | Tactics Setup → Match View | `ClientScreenFlow.StartMatch()` (`Replace`) | edge live; concrete setup/session handoff binding absent | `DESIGNABLE / UNWIRED` | the match replaces setup; no Back path to stale setup once started |
| A-05 | Match View → Post-Match Report | `ClientScreenFlow.ShowPostMatchReport()` (`Replace`) | edge live; full-time UI trigger/binding absent | `DESIGNABLE / UNWIRED` | finished match is not returnable through Back |
| A-06 | Post-Match Report → Main Menu | `ClientScreenFlow.ReturnToMainMenu()` (`Pop`) | edge live; report button/binding absent | `DESIGNABLE / UNWIRED` | current S0 return destination is Main Menu, not future Career Home |
| A-07 | Exit/abandon from Match View | no current `ClientScreenFlow` edge | deliberately absent | `FUTURE-BLOCKED` | S0 must not invent an abandon-match control |
| A-08 | Shipping four-screen shell | P5b in interactive-client plan | `ClientShellBehaviour`/equivalent P5b binding is absent on `main` | `FUTURE-BLOCKED` | production binding remains a Gate-I handoff; Gates B–H may use design/reference prototypes |

The current graph remains five named moves over four typed screen identities. S0-B must model those exact
semantics rather than a visually convenient alternative.

## 3.2 Pre-match setup and kickoff

| ID | Proposed S0 control/data | Owner / verified seam | Adapter / screen state | Gate-A classification | Constraint carried forward |
|---|---|---|---|---|---|
| A-09 | Home/away match configuration | `MatchSetup` owns squads, initial team tactics, manager modes/profiles and GK-heading boot flag | immutable boot value exists; no Tactics-Setup-specific view model/builder | `DESIGNABLE / UNWIRED` | S0 prototype may expose only fields backed by the actual setup/tactic types |
| A-10 | Formation / team tactic choice | `TeamTactic` is the initial tactic carried by `MatchSetup`; F1 semantic audit already verified Formation/Mentality and the supported team-tactic axes | no P5b pre-match adapter/control | `DESIGNABLE / UNWIRED` | Gate B chooses the smallest understandable verified tactic choice; no speculative FM-style semantics. **Corrected at Gate B (§7.1):** Formation has no simulation consumer; only Mentality is a consequential pre-match choice. |
| A-11 | Player Role/Duty/Instructions | `PlayerTactic` vocabulary exists, and live mutation exists through `ILiveMatchMutations.SetPlayerTactic` / `MatchTacticsDispatcher`; **`MatchSetup` carries no per-player tactic state/builder** | no pre-match state holder and no P5b pre-match adapter/control | `FUTURE-BLOCKED` pre-match | Gate B must not present Role/Duty/Instructions as pre-match-editable; a setup persistence/handoff contract must land first. A later live-match control may use the existing live dispatcher without implying a boot seam. |
| A-12 | Construct current match session | `MatchSessionLifecycle.CreateSession(MatchSetup)` installs a fresh, not-yet-started `MatchSession` and stops a prior current session before replacement | host-free lifecycle landed September 11; concrete Unity consumer intentionally absent | `DESIGNABLE / UNWIRED` | lifecycle stays unconsumed by production Unity code until Gate I; prototype may model the transition but must not wire it |
| A-13 | Start paced playback after attachment | `MatchSession` / live streamer own playback start | shipping attach/start ordering belongs to P5b | `FUTURE-BLOCKED` at binding | construction, host attachment and playback start remain separate; Gate I must preserve that order |

`MatchSessionLifecycle` closes the repeated-match ownership prerequisite but does not authorize an early
Unity shell implementation. Its own contract explicitly leaves attachment to P5b so the host can finish
wiring Match View before simulation playback starts.

## 3.3 Live match read model

| ID | Proposed S0 data | Owner / verified seam | Adapter / screen / host state | Gate-A classification | Constraint carried forward |
|---|---|---|---|---|---|
| A-14 | Score | `LiveMatchStreamer` → `LiveMatchStreamerFrameSource` → `MatchViewModelSource` → `MatchFrameView.Score` | concrete host-free projection live; P5b display absent | `DESIGNABLE / UNWIRED` | bind the projection; do not read the engine directly |
| A-15 | Match period / ended state | `MatchFrameView.Period`, `MatchFrameView.MatchEnded` | concrete projection live; P5b display absent | `DESIGNABLE / UNWIRED` | full-time presentation must agree with `MatchEnded` and the control-availability state |
| A-16 | Match time | `MatchFrameView.Tick` plus the match tick-rate contract; browser/reference client demonstrates existing tick→clock formatting | no dedicated shipping clock presenter in #38/P5b | `DESIGNABLE` data / `FUTURE-BLOCKED` presenter | Gate B may require a clock readout; Gate I must place formatting in a proper presentation helper/binding without inventing a second time source |
| A-17 | Field/agent state | `MatchFrameView` ball position, possession, agent positions/cues, substitutions used, restart banner | projection live; P4a landed; P4b now has pinned-host compile + scene-boot/render-smoke evidence, while click-to-command and cert-host perf acceptance remain open | `DESIGNABLE / HOST-UNVERIFIED` | UX may prototype from real captured output; Gate J still needs the remaining host/P5b/P6 evidence |
| A-18 | Pre-first-frame state | `MatchFrameView.Empty`; `MatchControlAvailability.AwaitingFirstFrame` | host-free state exists; P5b presentation absent | `DESIGNABLE / UNWIRED` | no blank/happy-path-only Match View; controls are visibly unavailable before first frame |

The S0 clock is a presentation gap, not a missing simulation fact. Gate A does not authorize a second clock
state or browser-code copy; it records the missing shipping presenter so the later implementation handoff has
one explicit owner decision to close.

## 3.4 Playback controls

| ID | Proposed S0 control/data | Owner / verified seam | Adapter / screen state | Gate-A classification | Constraint carried forward |
|---|---|---|---|---|---|
| A-19 | Current playback-speed choice | `PlaybackSpeedLadder` owns the ordered 1×/3×/5×/10× rungs and real-time opening index; streamer owns applied multiplier | P5b visual state absent | `DESIGNABLE / UNWIRED` | selected speed must be explicit enough for S0-T3; do not derive a private speed catalogue |
| A-20 | Faster/slower | `PlaybackSpeedLadder.StepFaster/StepSlower`; streamer applies `MultiplierAt(index)` | P5b buttons absent | `DESIGNABLE / UNWIRED` | stepping clamps at ends; binding forwards the decision |
| A-21 | Pause/resume | streamer `Pause`/`Resume`; pause is explicitly not a ladder rung | P5b control absent | `DESIGNABLE / UNWIRED` | pause must not masquerade as 0× or reset selected speed |
| A-22 | Playback enablement | `MatchControlAvailability.PlaybackControlsEnabled` + lock reason | host-free decision live; P5b interactable/why-disabled treatment absent | `DESIGNABLE / UNWIRED` | controls lock before first frame and at full time |

## 3.5 Live tactical intervention and substitution

| ID | Proposed S0 control | Owner / verified seam | Adapter / screen state | Gate-A classification | Constraint carried forward |
|---|---|---|---|---|---|
| A-23 | Team tactic change | engine public team-tactic seam via `MatchTacticsDispatcher` and the tick-stamped manager-intent command path | dispatcher live; P5b controls absent | `DESIGNABLE / UNWIRED` | UI never mutates engine/tactic state directly |
| A-24 | Player tactic change | engine public player-tactic seam via `MatchTacticsDispatcher` | dispatcher live; P5b controls absent | `DESIGNABLE / UNWIRED` | use only implemented `PlayerTactic` semantics |
| A-25 | Substitution | public substitution seam via `MatchTacticsDispatcher` → tick-stamped command queue | dispatcher live; P5b substitution UI absent | `DESIGNABLE / UNWIRED` | PM-1 exit criterion stays in scope even if the formative task uses a tactic change |
| A-26 | Tactical/substitution enablement | `MatchControlAvailability.TacticalInputEnabled` / `SubstitutionEnabled` + `MatchControlLockReason` | decision live; P5b presentation absent | `DESIGNABLE / UNWIRED` | disabled state is informative, not a clickable no-op; sim-side full-time guard remains authoritative |

## 3.6 Live statistics and post-match report

| ID | Proposed S0 data/control | Owner / verified seam | Adapter / screen state | Gate-A classification | Constraint carried forward |
|---|---|---|---|---|---|
| A-27 | Live statistics data | #37 `MatchAnalyticsAggregator`; repeated `Build()` is the existing analytics owner | real data exists in reference/browser composition; no P5b stats adapter/panel | `DESIGNABLE / UNWIRED` | do not add a second live-stats accumulator in client code |
| A-28 | Open/close live stats area | presentation-only state over A-27 data | P5b collapsible panel absent | `DESIGNABLE / UNWIRED` | collapsing changes presentation, not analytics ownership or sampling |
| A-29 | Final analytics result | #37 `MatchAnalyticsAggregator.Build()` → `MatchAnalyticsResult` | result type live; no Post-Match-specific #38 source/binding | `DESIGNABLE / UNWIRED` | report must render the real #37 result rather than a synthetic parallel summary |
| A-30 | Final score / full-time state | `MatchFrameView.Score` + `MatchEnded`; #37 result supplies statistics | data live; report composition absent | `DESIGNABLE / UNWIRED` | Gate B must make the frozen/end state unambiguous before return action |
| A-31 | Proceed to report | `ClientScreenFlow.ShowPostMatchReport()` | edge live; automatic/manual trigger presentation not bound | `DESIGNABLE / UNWIRED` | Gate B resolves the user-facing transition without inventing a new navigation edge |
| A-32 | Return to Main Menu | `ClientScreenFlow.ReturnToMainMenu()` | edge live; P5b control absent | `DESIGNABLE / UNWIRED` | current PM-1 loop ends at Main Menu; future Career Home remains separate S1/client work |

## 3.7 Cross-stream constraints that S0 must reserve without pretending they are live

| ID | Concern | Current owner/state | Gate-A classification | S0 rule |
|---|---|---|---|---|
| A-33 | Localization | #49 contract/implementation plan constrains text ownership and pseudo-locale testing; shipping client localization runtime is not an S0 screen capability on this snapshot | `FUTURE-BLOCKED` runtime | prototype may exercise pseudo-locale/reflow; no locale/settings control is presented as live without its runtime owner |
| A-34 | Accessibility/focus/text scale | `ux-shared-system.md` owns S0/S1 interaction requirements; production focus/settings behavior is not supplied by P5b yet | `DESIGNABLE` UX / `FUTURE-BLOCKED` binding where needed | Gate C–E must specify/test behavior; Gate I maps it to the actual client surfaces |
| A-35 | Match-presentation depth | #48 constrains later presentation depth; core PM-1 frame/render substrate already exists | `FUTURE-BLOCKED` for #48-only depth | S0 cannot require commentary/presentation features that #48 has not supplied |
| A-36 | Audio/captions | #51 constrains later cue/settings behavior; no S0 production audio binding is established here | `FUTURE-BLOCKED` runtime | Gate E still tests muted/caption coexistence where relevant; no phantom sound/settings control |
| A-37 | Missing art | UX shared fallback rules | `DESIGNABLE` | prototype and later binding must remain usable without final art |

---

# 4. Gate-A findings and dispositions

### S0-A-001 — P5b is the dominant missing shipping layer

**Finding:** The four typed screens and navigation graph are live, but the four-screen UGUI composition and the
Main Menu/Tactics/Post-Match adapters are not. `Match View` has the strongest concrete projection/dispatcher
substrate; the other three screens require client-specific presentation work.

**Disposition:** `FUTURE-BLOCKED` at production binding. This does not block S0 Gates B–H. Production P5b work
remains the Gate-I handoff.

### S0-A-002 — P4b evidence advanced again after the original Gate-A snapshot

**Finding:** The older UX text saying P4b had never compiled on the pinned Unity host was already stale after
PR #404, and the original #406 wording saying scene/runtime proof was wholly open is now stale too. Current
main carries pinned-editor compilation plus a real tracked-scene Play-mode boot/render smoke result; the
shipping click path and cert-host render-loop capture remain open.

**Disposition:** retain `HOST-UNVERIFIED`, with the explicit qualifier **compiler + scene/render smoke verified / full host acceptance incomplete**. Do not claim Gate J from the partial host evidence.

### S0-A-003 — repeated-match lifecycle is real but deliberately unconsumed

**Finding:** `MatchSessionLifecycle` now owns current-session replacement and clear semantics. Its contract keeps
construction separate from host attachment and playback start.

**Disposition:** S0-B/F may model this lifecycle. Production consumption stays blocked until Gate I; do not land a
second unconsumed shell/lifecycle prerequisite ahead of the actual consumer.

### S0-A-004 — live and post-match statistics have one owner

**Finding:** #37's `MatchAnalyticsAggregator` is already the statistics accumulator and can build the result used
by both live/reference presentation and the final report. The interactive-client plan explicitly rejected a
second client-side live-stats accumulator.

**Disposition:** all S0 live/report statistics consume #37. Any proposed statistic not present in
`MatchAnalyticsResult` or its live aggregation inputs is excluded or explicitly future-blocked.

### S0-A-005 — the shipping match clock lacks a dedicated presentation seam

**Finding:** `MatchFrameView` provides `Tick`, `Period` and `MatchEnded`; the reference/browser client already
formats tick into a player clock. There is no dedicated shipping clock presenter in the current #38/P5b surface.

**Disposition:** S0-B includes a readable match clock because the underlying time fact is real. Gate I must decide
where the tested tick→clock presentation helper lives; it must not create a second clock state/source.

### S0-A-006 — no Match View abandon edge exists

**Finding:** `ClientScreenFlow` deliberately omits an abandon-match transition.

**Disposition:** no S0 wireframe/prototype control may invent one. If product scope later requires abandon/quit,
that change returns to Gate A after a real client-flow contract exists.

### S0-A-007 — current Main Menu scope is intentionally narrow

**Finding:** current typed navigation supports the PM-1 entry path, not career Continue/New Game/Load/Settings
flows. Visual references that show those destinations overstate the current client.

**Disposition:** S0-B/C use a PM-1/demo entry path and visually distinguish any future-only reference material;
career-shell navigation remains S1/client work.

### S0-A-008 — full-time UI gating is honesty, not the safety invariant

**Finding:** `MatchControlAvailability.FullTime` disables tactical, substitution and playback controls, but its
own contract says the sim-side match-ended guard remains authoritative.

**Disposition:** S0-D must show locked controls and a reason at full time; implementation may not remove or replace
the sim-side guard because the UI also checks the frame.

### S0-A-009 — pre-match per-player tactics have vocabulary but no setup handoff

**Finding:** `PlayerTactic` defines Role, Duty and Instructions, and the live command path can stage a
`SetPlayerTactic` mutation after a session exists. But `MatchSetup` carries only team tactics plus the other
boot configuration; it has no per-player tactic collection/builder, and `MatchSession.BootEngine` therefore has
no pre-match per-player tactic state to apply. Treating the value type alone as a Tactics Setup seam would make
Gate B invent persistence or command timing.

**Disposition:** classify Role/Duty/Instructions as `FUTURE-BLOCKED` **for pre-match editing** until a concrete
setup persistence/handoff contract exists. Gate B may use the verified team-tactic choice for Tactics Setup and
may separately use the existing live dispatcher for an in-match per-player tactic change; neither substitutes
for the missing boot seam.

---

# 5. Gate A verdict

**PASS — S0 Gate A is complete on reconciled evidence snapshot `ad7e0d751f978c8785e7bab2024b99ff5a8da26d`.**

There are **zero `UNKNOWN` capability dependencies** supporting the S0 design premise. Every S0 task/control is
one of:

- backed by an implemented owner/read/action/navigation contract and therefore `DESIGNABLE`;
- `UNWIRED` when that product contract exists but the production presentation/binding is not yet connected; or
- `FUTURE-BLOCKED` when the required product contract/state/runtime capability itself is absent (for example,
  A-07's missing Match View abandon edge and A-11's missing pre-match per-player tactic setup handoff).

`UNWIRED` and `FUTURE-BLOCKED` are therefore not interchangeable: a later gate may not relabel a missing contract
as merely a P5b binding gap.

The pass does **not** mean P5b, full P4b host acceptance, on-host P6, #48, #49 or #51 runtime work is complete.
It means S0 can proceed to Gate B without inventing product behavior.

---

# 6. Gate B inputs — as recorded at Gate A

Gate B must now define one coherent S0 task flow using only the audited surfaces above. It must include:

1. launch/root state and PM-1/demo match entry;
2. Tactics Setup entry, one clear verified **team-tactic** pre-match choice, cancel/re-entry, and start; per-player Role/Duty/Instructions remain excluded until a setup handoff exists;
3. Match View pre-first-frame, live and full-time states;
4. visible score, readable match time/state and explicit selected playback speed;
5. playback change, one tactical change and a substitution path;
6. collapsible live stats consuming #37;
7. full-time transition into the #37-backed Post-Match Report;
8. return to Main Menu through the existing named edge;
9. alternate/disabled paths and explicit reasons where the owner contract supplies one.

Gate B may select interaction wording and ordering, but it may not add a screen, navigation edge, simulation
command, statistic, setting or production capability not present in this Gate-A inventory.

---

# 7. Gate B — S0 task flow

Gate B defines the S0 journey without visual styling: entry trigger, player goal, required information,
actions/decisions, alternate and blocked paths, cancellation/back behavior, completion state and return
destination (`ux-detailed-plan.md` §5). It uses only the Gate-A inventory in §3, as amended by §7.1. Every
information element and action below cites its Gate-A row. Layout, density, component choice, focus order and
copy are Gate C–E work and are deliberately not decided here; the wording in quotation marks is placeholder
intent, not final copy.

## 7.1 Gate-A re-check, addenda and one correction

Gate B re-ran the Gate-A claims it depends on against `main` `ee37aa60`. The four-screen / five-edge graph
(`ClientScreenFlow`), `MatchSessionLifecycle`, `MatchControlAvailability` (three states, three lock reasons),
`PlaybackSpeedLadder` (1×/3×/5×/10×, real-time opening rung), streamer `Pause`/`Resume`, `MatchTacticsDispatcher`
(three intent kinds: team tactic, player tactic, substitution) and `MatchAnalyticsResult` are unchanged. P5b is
still absent from `main`; the refreshed branch is held as draft PR #470 pending Gate I.

Designing the flow exposed one overstatement in the Gate-A inventory and four existing surfaces the inventory
did not list. Per `ux-detailed-plan.md` §13, action/data changes return to Gate A, so they are recorded here as
Gate-A amendments with their evidence rather than used silently.

| ID | Surface | Evidence at `ee37aa60` | Classification | Effect on the flow |
|---|---|---|---|---|
| A-10 **correction** | Formation as the pre-match choice | `TeamTactic.Formation` (`F442`/`F433`/`F4231`) is written by the snapshot serializer (`MatchEngine.cs` canonical tactic write), parsed by `TacticFileGrammar` and passed through by `MatchClientRouter`; **no simulation system reads it**. `Mentality` is read by `UtilityScorer` (`MentalityRiskMultiplier`) and by the engine's defensive-line depth (`MentalityLineBias`) | Formation: `FUTURE-BLOCKED` as a player-facing choice (no football consequence); Mentality: `DESIGNABLE / UNWIRED` | S0's one pre-match choice is **Mentality**. A Formation control would promise a consequence the match cannot deliver. |
| A-38 **addendum** | Command outcome | `MatchSession.Driver.Log` (applied, tick-stamped) and `MatchSession.Driver.FailedCommands` (refused by the live mutator, tick-stamped). Commands drained after `MatchEnded` are dropped sim-side and appear in **neither** list | `DESIGNABLE / UNWIRED` (no #38 adapter) | The only real source for S0-T4's "can tell whether each action took effect". `LiveMatchFrame` carries no active-tactic read-back. |
| A-39 **addendum** | Opponent manager | `MatchSetup.AwayManagerMode = ManagerMode.AI` opts the away team into #26 kickoff selection and in-match adaptation; `MatchSetup.NeutralDemo` leaves both teams `Human` (no adaptation) | `DESIGNABLE / UNWIRED` (no client setup builder) | Lets the opponent react during the match without a new capability; see B-DEC-2. |
| A-40 **addendum** | Player identity | `NeutralDemo` passes no `Squad`s. Agents are identified only by team, shirt number (`MatchRoster.ShirtNumber`), goalkeeper flag and cues; bench players only by bench slot 0–6 (`LiveAgentCue.BenchSlot`). `MatchSetup` accepts real `Squad`s, but no S0 source for a squad pair exists in client code | shirt-number identity `DESIGNABLE / UNWIRED`; named players `FUTURE-BLOCKED` for S0 | Substitution selection uses shirt number and bench slot; names are not shown. See S0-B-004. |
| A-41 **addendum** | Substitution legality inputs | Sim-side refusals: match ended, outgoing slot sent off or already substituted, bench slot already used, team at `MAX_SUBSTITUTIONS_PER_TEAM` (5); bench size `SUBSTITUTES_PER_TEAM` (7). Frame exposes `SubstitutionsUsed` per team and per-agent `IsSentOff` / `BenchSlot` | `DESIGNABLE / UNWIRED` | The flow can disable illegal choices before submission; the sim-side refusal (A-38) remains authoritative for races. |
| A-42 **addendum** | Statistics health | #37 statistics are fed by the post-tick observer attached through `MatchSession.AttachTickObserver`. On its first exception the streamer disarms it and latches the cause on `MatchSession.Streamer.PostTickObserverFault` (null while healthy), and the match keeps running. After that, every `MatchAnalyticsResult` built is frozen at the failing tick. The reference web host's `AnalyticsFault` wraps the same seam and states that a screen **must** surface it rather than show frozen numbers as current; its `/report` route emits `healthy`/`fault` beside the numbers. The aggregator's observed-tick count locates where the numbers stopped. | `DESIGNABLE / UNWIRED` (no #38 projection) | Live statistics and the report need an explicit failure state (§7.4 B-4, B-7; §7.6). The score and full-time state come from the frame (A-14, A-15), not from #37, so they stay correct. |

*A-42 was added on September 30, 2026 in response to automated review of PR #472 (S0-B-009).*

The Gate-A verdict survives the correction. A-10 moves from a partly overstated `DESIGNABLE` to an explicit
split, and no row becomes `UNKNOWN`.

## 7.2 Flow decisions

Gate B may choose ordering and wording within the audited surfaces. These are the choices that shape the flow.
B-DEC-1, B-DEC-2 and B-DEC-5 are product-visible and were explicitly confirmed by the project owner on September 28, 2026 (§7.10).

| ID | Decision | Rationale | Seam |
|---|---|---|---|
| B-DEC-1 | The player manages the **home** team (team 0). Every manager intent carries `TeamId = 0`. | One controlled side keeps S0 to "your team vs an opponent"; home is team 0 in every existing seam. | A-09, A-23, A-25 |
| B-DEC-2 | The opponent is **AI-managed** (`AwayManagerMode = AI`), using the default manager profile. | A static opponent that never reacts misrepresents a management match. If the owner declines, the fallback is `Human`/no input, and the flow is otherwise unchanged. | A-39 |
| B-DEC-3 | The pre-match choice is **Mentality**, seven ordered values from Very Defensive to Very Attacking, defaulting to Balanced. All other team-tactic axes stay at their `Balanced` defaults and are not shown in S0. | One consequential, ordered, explainable choice satisfies S0-T2. The other axes are real but would widen S0 past "one understandable choice"; S1 revisits them. | A-10 (corrected) |
| B-DEC-4 | The in-match tactical adjustment is the **same Mentality control**, sent as a team-tactic change. Per-player tactic change stays available as a seam (A-24) but is **not** in the S0 required path. | Re-using the pre-match concept makes S0-T4 learnable and testable. The per-player surface needs player identity, which A-40 limits to shirt numbers. | A-23, A-26 |
| B-DEC-5 | **Lifecycle contract:** the full-time state is the authoritative transition. The first frame with `MatchEnded` true puts Match View into MV-FT, and `MatchControlAvailability.FullTime` is the only thing that makes the report reachable. The binding never detects the end itself and never enables the report from any other signal. **Presentation:** in MV-FT the already-available report is exposed through a "View match report" control that invokes `ShowPostMatchReport()`. That control is acknowledgement and navigation only. It does not decide whether the match is over or whether the report exists, and it cannot appear or act outside MV-FT. | S0-T6 tests recognition of the frozen full-time state, and an immediate automatic `Replace` would remove that moment. Whether the report opens on acknowledgement or on a timed advance after MV-FT is a presentation choice that Gate G can test. Neither choice changes the contract above. | A-05, A-15, A-22, A-30, A-31 |
| B-DEC-6 | Pause is a separate toggle from the speed rungs. Pausing keeps the selected rung, and resuming returns to it. | `PlaybackSpeedLadder` has no 0× rung, and A-21 forbids pause masquerading as a speed. | A-19–A-21 |
| B-DEC-7 | A setup choice is not kept when the player cancels Tactics Setup. Re-entry starts from Balanced. | No pre-match state holder exists (A-11's missing setup handoff). A remembered draft would invent persistence. | A-03, A-09 |
| B-DEC-8 | Live statistics start **closed**. Opening them does not pause the match. | The pitch is the primary information during play, and statistics are secondary (S0-T5 asks for open/close). | A-27, A-28 |

## 7.3 Journey states

Seven presentation states over the four typed screens. State names are Gate-B labels, not new screen identities:
all Match View states are `ClientScreens.MatchView`.

| State | Screen | Entered when | Leaves by |
|---|---|---|---|
| **MM** Main Menu | `MainMenu` | launch (root), or return from Tactics Setup / Post-Match Report | "Play a demo match" → TS |
| **TS** Tactics Setup | `TacticsSetup` | `OpenTacticsSetup()` (`Push`) | "Start match" → MV-0 (`StartMatch()`, `Replace`); "Back" → MM (`CancelTacticsSetup()`, `Pop`) |
| **MV-0** Match starting | `MatchView` | session created and attached; no frame published yet (`AwaitingFirstFrame`) | first frame → MV-L |
| **MV-L** Live | `MatchView` | a frame with `MatchEnded == false` | pause → MV-P; `MatchEnded` frame → MV-FT |
| **MV-P** Paused | `MatchView` | player pauses from MV-L | resume → MV-L |
| **MV-FT** Full time | `MatchView` | first frame with `MatchEnded == true` (`FullTime`) | "View match report" → PR (`ShowPostMatchReport()`, `Replace`) |
| **PR** Post-Match Report | `PostMatchReport` | from MV-FT | "Return to main menu" → MM (`ReturnToMainMenu()`, `Pop`) |

The only backward edges are TS → MM and PR → MM, exactly the two `Pop` moves. There is no edge out of any
Match View state except MV-FT → PR (A-07: no abandon).

## 7.4 Step-by-step flow

### B-1 Main Menu (MM)

- **Entry trigger:** application launch roots the shell at Main Menu (A-01), or a return from TS/PR.
- **Player goal:** start a match.
- **Required information:** product identity, and one available action that names what it does ("Play a demo
  match"). No career, continue, load, settings or quit entries (S0-A-007; S0-B-006).
- **Actions:** "Play a demo match" → `OpenTacticsSetup()` (A-02).
- **Alternate/blocked:** none. The single action is always available in MM, so there is no disabled state to explain.
- **Back/cancel:** not applicable at the root.
- **Completion:** TS is shown.

### B-2 Tactics Setup (TS)

- **Entry trigger:** B-1.
- **Player goal:** make one understandable tactical choice and start (S0-T2), or leave without starting (S0-T7).
- **Required information:**
  - which side the player manages (home) and that the opponent is AI-managed (B-DEC-1/2), identified as home/away,
    not by club name (A-40);
  - the current Mentality value, with Balanced pre-selected (B-DEC-3);
  - for each Mentality value, one short consequence statement tied to what the engine actually changes: more or
    less risk in on-ball choices, and a higher or deeper defensive line (A-10). No statement may promise effects
    the engine does not model.
- **Actions:**
  - choose a Mentality value (presentation state only until start);
  - "Start match" → build `MatchSetup` (home tactic = `Balanced` with the chosen Mentality, away mode `AI`) →
    `MatchSessionLifecycle.CreateSession(setup)` (A-12) → `StartMatch()` (A-04) → MV-0;
  - "Back" → `CancelTacticsSetup()` (A-03) → MM.
- **Alternate/blocked:** "Start match" is always enabled, because Balanced is a valid choice. Nothing on this screen
  can be incomplete. Role/Duty/Instructions are absent, not disabled (A-11; F2 §6: future capability is omitted).
- **Back/cancel:** "Back" discards the choice (B-DEC-7). Re-entry from MM shows Balanced again.
- **Completion:** the match view opens in MV-0. Setup is gone from history (`Replace`), so there is no Back to
  setup after starting.

### B-3 Match starting (MV-0)

- **Entry trigger:** B-2 "Start match".
- **Player goal:** understand that the match is about to begin.
- **Required information:** a transient "match starting" state (`AwaitingFirstFrame`, A-18). Playback, tactical
  and substitution controls are visible but unavailable, with that reason (A-22, A-26).
- **Actions:** none.
- **Ordering constraint:** session construction, host attachment and playback start remain three separate steps,
  in that order (A-13). The flow shows MV-0 from attachment until the first frame.
- **Completion:** first frame → MV-L.

### B-4 Live match (MV-L)

- **Entry trigger:** first frame, or resume from MV-P.
- **Player goal:** follow the match, and intervene when they choose to (S0-T3/T4/T5, PM-1 substitution).
- **Required information (always visible):**
  - score (A-14);
  - match clock as minutes elapsed, derived from `Tick` against the 90-minute match length, and the period
    (first half / second half) (A-15, A-16);
  - current playback speed rung, and whether paused (A-19);
  - the pitch: agents, ball, possession cue, restart banner when present (A-17);
  - the player's current Mentality and the state of any pending request (§7.5).
- **Actions:**
  - **Speed:** faster/slower step one rung (`StepFaster`/`StepSlower`, A-20); pause → MV-P (A-21).
  - **Tactical adjustment:** choose a new Mentality value and confirm → `SetTeamTactic(0, …)` through the
    dispatcher (A-23). The request then shows as pending until A-38 reports it applied or refused (§7.5).
  - **Substitution:** open the substitution choice → pick an on-pitch home player (shirt number) and an unused
    bench slot → confirm → `Substitute(0, outSlot, benchSlot, Tactical)` (A-25). The reason is always `Tactical`, because S0 has no injury surface. Pending/applied/refused as §7.5.
    Selection can be abandoned without effect until confirm.
  - **Live statistics:** open/close (A-28). Contents in §7.7. If A-42 reports a fault, the area keeps its values
    but shows a persistent "statistics stopped at minute N" state in place of presenting them as current. N comes
    from the aggregator's observed-tick count. The match itself continues normally.
- **Half-time:** the engine has no half-time interval state. The period label changes to second half, and play
  continues with a kickoff restart banner. The flow adds no break screen (S0-B-007).
- **Completion:** a `MatchEnded` frame → MV-FT.

### B-5 Paused (MV-P)

- Same information as MV-L, with an explicit paused indicator. The selected speed rung stays visible and
  unchanged (B-DEC-6).
- Actions: resume → MV-L. Tactical and substitution requests may be made. They show as pending, and the flow does
  not assume they apply before resume (S0-B-008).
- Speed-rung changes while paused change the selected rung and take effect on resume.

### B-6 Full time (MV-FT)

- **Entry trigger:** the first frame with `MatchEnded == true` (A-15, `MatchControlAvailability.FullTime`).
- **Player goal:** recognize that the match is over and see the result (S0-T6).
- **Required information:** a persistent "full time" state and the final score. The pitch stays frozen on the final
  frame. Playback, tactical and substitution controls remain visible but unavailable, with reason "match ended"
  (A-22, A-26, S0-A-008). Any request still pending resolves as "not applied — the match ended" (§7.5).
- **Transition contract:** MV-FT exists only because `FullTime` was reported. Report availability derives from that state alone (B-DEC-5).
- **Actions:** "View match report" → `ShowPostMatchReport()` (A-31) → PR. This is acknowledgement/navigation over an already-available report, and the only action in MV-FT.
- **Back/cancel:** none. There is nothing to return to (A-07).

### B-7 Post-Match Report (PR)

- **Entry trigger:** B-6.
- **Player goal:** understand the result and core statistics, then leave (S0-T6).
- **Required information:** final score and result (win/draw/loss from the home side's view), both from the frame
  (A-30), and both teams' #37 statlines (§7.7), together with their health (A-42). No causal analysis or ratings
  that #37 does not supply (A-29).
- **Statistics failure path:** if A-42 reports a fault, the score and result still show, because they don't
  depend on #37. The statistics carry a persistent "statistics incomplete — stopped at minute N" notice and are
  never presented as full-match totals. The #37 goal count may then disagree with the score, and the score wins.
  The return action stays available. Whether the partial figures are shown at all under that notice is a Gate C
  choice. Showing them unlabelled is not allowed.
- **Actions:** "Return to main menu" → `ReturnToMainMenu()` (A-32) → MM. No rematch, replay or save
  (no edge, no client lifecycle).
- **Completion / return destination:** MM. A new match from MM goes through B-2 again, and
  `MatchSessionLifecycle` stops and replaces the finished session (A-12).

## 7.5 Intervention feedback — requested, applied, refused, not applied

This closes S0-T4's "can tell whether each action took effect" using only A-38. The flow never shows an action as
applied on the strength of the click.

| Request state | Determined by | Shown to the player |
|---|---|---|
| **Pending** | enqueued; not yet in `Driver.Log` or `Driver.FailedCommands` | the requested change, marked as waiting |
| **Applied** | the command appears in `Driver.Log` | the change as current, with the match minute it applied at (from its tick stamp) |
| **Refused** | the command appears in `Driver.FailedCommands` | a persistent inline message at the intervention control. The displayed value reverts to what was last applied. The refusal is not toast-only (F2 §5). |
| **Not applied — match ended** | still pending when the MV-FT frame arrives (sim-side post-end drop is recorded in neither list) | resolved in MV-FT with that wording |

Current substitution semantics are **immediate**: the swap applies at the tick the command is drained. The owner
rule of September 25, 2026, "request any time, execute at the next stoppage" (`open-issues.md`), is recorded but
not implemented. The flow designs today's behavior. When that rule lands, the substitution row gains a
"requested — waiting for a stoppage" state, and this section returns to Gate B under §13 change control
(S0-B-003).

## 7.6 Alternate and blocked paths

| Where | Condition | Behavior | Reason source |
|---|---|---|---|
| MV-0 | no frame yet | all match controls unavailable; transient "match starting" | `AwaitingFirstFrame` (A-18, A-22, A-26) |
| MV-L/P | at 10× | "faster" unavailable ("already at the fastest speed") | `PlaybackSpeedLadder.FastestIndex` (A-20) |
| MV-L/P | at 1× | "slower" unavailable ("already at real time") | `PlaybackSpeedLadder.SlowestIndex` (A-20) |
| MV-L/P | home team has used 5 substitutions | substitution entry unavailable, with the count shown | `SubstitutionsUsed` vs `MAX_SUBSTITUTIONS_PER_TEAM` (A-41) |
| substitution choice | outgoing player sent off, or already substituted this match | not offered as an outgoing choice | `IsSentOff`, `BenchSlot` / sim refusal rule (A-41) |
| substitution choice | bench slot already used | not offered as an incoming choice | sim refusal rule (A-41) |
| MV-L/P | a request races past these checks and is refused | "refused" state, §7.5 | `FailedCommands` (A-38) |
| MV-FT | match ended | every match control unavailable ("match ended"); pending requests resolve as not applied | `FullTime` / `MatchEnded` (A-15, A-22, A-26); sim-side drop |
| TS | — | no blocked state; "Start match" always enabled | B-DEC-3 |
| MV-L/P, MV-FT, PR | statistics observer faulted | statistics marked "stopped at minute N" / "incomplete"; never shown as current or as full-match totals; score, clock and navigation unaffected | `PostTickObserverFault` + observed-tick count (A-42) |

Disabled states appear only where the capability is real and the current state blocks it (F2 §6). Capabilities
that are future-blocked, such as Formation, pre-match Role/Duty/Instructions, save, quit, rematch and abandon, are
**omitted**, not shown disabled.

## 7.7 Information the flow requires from #37

Live statistics (B-4) and the report (B-7) use the same `MatchAnalyticsResult` owner (A-27, A-29, S0-A-004).
Both show, for home and away, only what `MatchStatline` / `AdvancedStatline` carry, and both carry the A-42 health
state. A faulted result is labelled incomplete, never shown as current:

- goals, possession share %, fouls, yellow cards, red cards, offsides, corners, throw-ins, goal kicks, substitutions;
- territorial %;
- xG **only when** `LiveXgAvailable` is true, and omitted otherwise, never shown as 0.

#37 carries **no shots or shots-on-target statistic**. Gate C must not lay out the familiar "shots" row. The goal,
foul and offside location maps (`GoalMap`/`FoulMap`/`OffsideMap`) exist and are left to Gate C as optional report
detail. They are not required by any S0 task.

## 7.8 Validation-task coverage

| Task | Flow steps | Completion signal |
|---|---|---|
| S0-T1 | B-1 → B-2 | TS shown |
| S0-T2 | B-2 (Mentality) → B-3 | MV-0/MV-L shown with the chosen Mentality displayed |
| S0-T3 | B-4 | score, minute/period, speed rung all readable in MV-L |
| S0-T4 | B-4 speed + Mentality change, §7.5 | speed rung changed; Mentality request reaches Applied |
| S0-T5 | B-4 statistics | stats opened, a requested statistic located (§7.7), stats closed |
| S0-T6 | B-6 → B-7 → B-1 | full time recognized, report read, MM reached |
| S0-T7 | B-2 Back → B-1 → B-2 | MM reached without starting; TS re-entered at Balanced |
| PM-1 exit (substitution) | B-4 substitution, §7.5, §7.6 | a substitution reaches Applied |

## 7.9 Gate-B findings and dispositions

Ledger fields per `ux-detailed-plan.md` §12. Owner is the UX workstream owner (Anton Zymin) unless named.

| ID | Severity | Finding | Disposition | Release condition / retest |
|---|---|---|---|---|
| S0-B-001 | Major | Gate A's A-10 treated Formation as a verified pre-match choice; no simulation system reads it (§7.1). | `FIX NOW` — Formation removed from S0; Mentality chosen (B-DEC-3). | Done in this revision. If a formation consumer lands, Formation re-enters at Gate A. |
| S0-B-002 | Major | No frame read-back of the active tactic; outcome evidence exists only in the command logs (A-38), which no #38 adapter projects. | `ACCEPT FOR CURRENT GATE`; owner accepted for G September 30, 2026. Reason and acceptance statement: validation protocol §9.1. §7.5 defines the states from A-38. | Gate I specifies the production adapter reading `Driver.Log`/`FailedCommands` for Pending/Applied/Refused/Not applied; Gate J verifies it. Feedback does not ship without the adapter. The finding remains open through that verification. |
| S0-B-003 | Minor (S0) | Substitutions apply immediately; the owner's stoppage rule is not implemented. | `BLOCKED BY DOMAIN/CLIENT IMPLEMENTATION` for the future state; today's behavior designed. | When the owning engine change lands, §7.5 returns to Gate B (§13). |
| S0-B-004 | Major (usability risk) | The neutral demo has no player names. Substitution relies on shirt numbers and bench slots (A-40). | `ACCEPT FOR CURRENT GATE`; owner accepted for S0 September 30, 2026. Reason and acceptance statement: validation protocol §9.1. | Recheck identification at H. Closes when player names are available through S1/client work. If H shows players cannot be identified, reopens as a Blocker. |
| S0-B-005 | Minor | PR #361's closing plan note said Match View → Post-Match Report should be driven by `FullTime`, "not an invented button". A player-facing "View match report" control is in tension with that note. Reusing the existing `ShowPostMatchReport` edge does **not** by itself resolve the tension, because the note concerns what drives the transition, not whether the edge is new. | `ACCEPT FOR CURRENT GATE`; B-DEC-5 owner-confirmed September 28, 2026. B-DEC-5 separates the two concerns: `FullTime` remains the authoritative lifecycle trigger and sole source of report availability, and the control is limited to acknowledgement/navigation within MV-FT. | Confirmed. Gate I must carry the contract into the binding. If Gate G shows participants missing or stalling on the full-time moment, only MV-FT's presentation returns to Gate C; the lifecycle contract stands. |
| S0-B-006 | Minor | No quit/exit control exists in the Gate-A inventory. The desktop window close is the only exit. | `DEFER TO P2/P3` — requires a Gate-A addendum naming the host quit seam. | Before Gate I. |
| S0-B-007 | Minor | No half-time interval exists in the engine. The second half follows directly. | `ACCEPT FOR CURRENT GATE`. | Re-evaluate at Gate G if participants expect a break. |
| S0-B-008 | Minor | Whether requests made while paused apply before resume depends on whether the P5b binding services the command queue off-tick (`ServiceOnce`). | `ACCEPT FOR CURRENT GATE`. §7.5 shows Pending either way, so the flow is correct under both. | Gate I states the binding's paused-servicing behavior. |
| S0-B-009 | Major | The flow presented #37 statistics unconditionally, with no path for an analytics-observer failure. After a fault, statistics freeze at the failing tick while the match continues, so the report would look plausible but be incomplete. The owning contract (A-42) requires the fault to be surfaced. Raised by automated review of PR #472. | `FIX NOW` — A-42 added; B-4, B-7, §7.6 and §7.7 now carry the failure state; the score and result stay frame-sourced. | Done in this revision. Gate I must name where the Unity binding reads `PostTickObserverFault`; Gate J verifies the failure presentation with a forced observer fault. |

## 7.10 Gate B verdict

**PASS.** The S0 flow is coherent without visual styling:

- every state has an entry trigger, a goal, required information with a cited owner, actions bound to existing
  seams, explicit alternate/blocked behavior, cancel/back behavior and a completion/return destination;
- the only navigation used is the five existing `ClientScreenFlow` moves;
- no screen, edge, simulation command, statistic, setting or production capability is added beyond the Gate-A
  inventory as amended in §7.1.

**✅ OWNER DECISION — September 28, 2026.** The project owner explicitly confirmed **B-DEC-1** (player manages
home), **B-DEC-2** (existing AI manager for the opponent) and **B-DEC-5** (full time is the authoritative transition
and sole source of report availability; the report control is acknowledgement/navigation only). **Gate B is
closed.**

Gate B does **not** release P5b (Gate I), pass any later gate, or change any `src/` surface.

---

# 8. Gate C inputs

Gate C produces low-fidelity information design for the seven states in §7.3: hierarchy, density, primary action,
navigation, comparison, progressive disclosure, focus sequence and reflow. Carry forward:

1. MV-L's always-visible set (§7.4 B-4) and its placement against the pitch;
2. the §7.5 request-state treatment at the intervention controls, which must stay readable without color;
3. the Mentality scale's ordered presentation and consequence text (B-2), within localization/reflow limits;
4. the substitution chooser built on shirt number and bench slot (A-40, S0-B-004);
5. the MV-FT frozen state and its single primary action;
6. the §7.7 statistic set with no shots row, and xG omitted when unavailable;
7. everything omitted in §7.6 stays omitted. Gate C may not reintroduce it as disabled chrome;
8. the A-42 statistics-failure presentation for the live stats area and the report, including whether partial
   figures appear under the incomplete notice.

---

# 9. Gate C — low-fidelity information design

**Vehicle:** [interactive prototype v0.5](s0-prototype/index.html), [review/reproduction guide](s0-prototype/README.md).
Seven monochrome wireframes and the executable vehicle cover §8's eight inputs. They use system text,
boxes, spacing, checked selection and solid/dashed marker outlines; no `touchline` polish or final art.

## 9.1 Screen hierarchy, action and density

| State / wireframe | Information order and density | Primary action / navigation | Disclosure / comparison |
|---|---|---|---|
| [MM](s0-prototype/evidence/v0.5/mm.pdf) | product identity → home/AI opponent context → one supported action; low density | Play a demo match → `OpenTacticsSetup()` | no career, settings, save or other future chrome |
| [TS](s0-prototype/evidence/v0.5/ts.pdf) | home/away responsibility → seven ordered Mentality choices with consequences → ready state → start/back; one choice region | Start match → `StartMatch()`; Back → `CancelTacticsSetup()` | all seven choices visible; Balanced checked on entry; no Formation, player tactics or squad-name promise |
| [MV-0](s0-prototype/evidence/v0.5/mv-0.pdf) | waiting heading → withheld score/clock → selected speed → locked controls/reason → pitch placeholder | no player action | never renders empty frame as 0–0/live; no fake progress percentage or retry seam |
| [MV-L](s0-prototype/evidence/v0.5/mv-l.pdf) | score/period/minute/speed band → playback controls → captured pitch; Home Mentality and request feedback in adjacent rail | task-region actions: Pause / speed / Change Mentality / Make substitution | statistics closed initially; Home/Away columns compare the same snapshot; opening does not pause |
| [MV-P](s0-prototype/evidence/v0.5/mv-p.pdf) | same structure; Paused is text next to unchanged speed | Resume | requests may be submitted; Pending stays distinct from current/applied; no claim of pre-resume application |
| [MV-FT](s0-prototype/evidence/v0.5/mv-ft.pdf) | Full time + final score → report acknowledgement → frozen pitch → locked controls/reason | View match report → `ShowPostMatchReport()` | request still pending resolves as Not applied — match ended; report cannot be reached from another state |
| [PR](s0-prototype/evidence/v0.5/pr.pdf) | Full time/home result + frame score → statistics health → core comparison → return | Return to main menu → `ReturnToMainMenu()` | no ratings, causality, shots, replay, rematch or save; optional maps omitted from this S0 cut |

The final score always comes from the selected fixture frame; Goals recorded in #37 is labelled separately. Possession
shares include loose-ball time: Home + Away need not equal 100%, and the prototype does not normalize
them. xG has no row when unavailable, rather than an invented zero. Live and final statistics use the
same selected fixture. Ordinary sessions use a synthetic 2–1 scenario, labelled in the banner and table.
The `scoreline` fixture uses unmodified captured #37 output. The prototype omits the captured
Substitutions row so it cannot contradict the simulated request count; the captured pitch is labelled
as unchanged by simulated swaps. This is a vehicle limitation, not removal of the shipping row in §7.7.
No production accumulator or causal explanation is introduced.

## 9.2 Progressive disclosure and statistics failure decision

**C-DEC-1 (owner-accepted September 30, 2026; protocol §9.1):** retain partial figures in the open live statistics area, under the persistent
“Statistics stopped at minute N” notice. Keep that notice visible even when statistics are closed.
The report first shows “Statistics incomplete — stopped at minute N”; partial figures are hidden
behind **Show partial statistics — incomplete**, with a table caption repeating the cutoff and
“not full-match totals”. Score/result and Return stay outside that disclosure. This is the Gate-C
choice left open by §7.4 B-7. See [incomplete report](s0-prototype/evidence/v0.5/report-incomplete.pdf).

The reference capture is healthy; the fault fixture deliberately freezes its synthetic statistics snapshot at
minute 18 while score/time continue. Its cutoff models A-42's observed-tick count. It is simulated
fault evidence, not a claim that the captured run faulted. No retry is offered because A-42's
observer is disarmed, not restartable through an audited S0 UI seam.

Mentality and substitution use cancellable staging dialogs. Mentality shows the full requested value
and the same consequence as setup. Substitution labels outgoing Home shirt number (including goalkeeper)
and incoming **Bench slot 0–6**, never invented names. Sent-off/already-substituted outgoing slots and
used bench slots are excluded; the sim-side refusal remains authoritative. Submit queues a request,
not success. Current value/count changes only after simulated Applied feedback. No extra confirmation
step is added to routine submission. Only the last three feedback records are expanded; earlier records
remain in labelled disclosure, preserving the active task's space under many indicators.

## 9.3 Focus, cancellation and reflow

| Region | Keyboard sequence / recovery |
|---|---|
| MM | Play a demo match; root has no Back action |
| TS | checked Mentality radio group (arrows traverse seven values) → Start match → Back; cancellation discards draft |
| MV-L/P | available Slower → Pause/Resume → available Faster → Change Mentality → Make substitution → Open/Close statistics → any earlier-feedback disclosure; disabled controls skipped, reasons stay visible |
| Mentality dialog | requested Mentality selector → Submit change → Cancel; full selected label and consequence remain inline |
| Substitution dialog | outgoing shirt selector → incoming bench selector → Submit change → Cancel |
| MV-FT | report acknowledgement receives focus on full-time transition; disabled playback/team actions do not trap focus |
| PR | partial-statistics disclosure if present → Return to main menu |

Each named screen transition focuses its heading. Dialog Tab/Shift-Tab wrap inside; Escape/Cancel
closes staging and restores its enabled invoker. Submit disables the pending request’s invoker and
focuses the labelled request-feedback region. At 1×/10×, newly disabled Slower/Faster instead focus
Pause/Resume. These destinations keep the user in the active task region. Full time cancels an open, unsubmitted chooser and focuses
the report action. It does not discard a queued command silently: pending feedback becomes not applied.
Live refresh preserves earlier-feedback disclosure state and restores its summary focus, or the same
logical focus target when it remains; it does not advance selection.
No global gameplay shortcut or Escape-as-history-Back is introduced.

At 1366-wide, preserve the scoreboard, playback/current tactic and blockers; keep secondary statistics
closed until requested. At 1920×1080, pitch and the compact intervention/statistics rail are adjacent.
At 2560-wide, cap the content width instead of stretching text/controls across the display. At narrower
available width or text scale of 150% and above, the rail stacks; the pitch keeps full available width. A deterministic nearest-clear placement rule separates
marker labels without promising visible leader lines, and current Mentality stays in the context band. Expanded labels wrap; 200% text may require vertical scrolling but no
critical control or label is clipped. Dialogs scroll vertically and keep their fields/commit/cancel
reachable. These remain design-validation cases, not shipping resolution/minimum/scale promises.

## 9.4 Complete image-review coverage

Every PDF below is an exported exercised DOM state in prototype v0.5. Dialogs are opened through
actual controls; outcomes follow queued simulated requests. The Not-applied image comes from the
normal MM→TS→MV minute-89 pause/request/resume journey, not a directly loaded result. None is
independent tester evidence. The owner approved the `mv-ft-statistics-fault.pdf` delta at `0e8bd2b` on September 30, 2026 (protocol §9.1.1). Text extraction confirms
the other 18 match approved v0.4 apart from the version label; they are carried forward, not submitted
for a full re-review. Those 18 v0.5 PDFs are unchanged by this correction. Original approved v0.4
PDFs remain unchanged in the parent evidence directory and in protocol §9.1.

| Review surface | Image |
|---|---|
| Seven journey states | [MM](s0-prototype/evidence/v0.5/mm.pdf), [TS](s0-prototype/evidence/v0.5/ts.pdf), [MV-0](s0-prototype/evidence/v0.5/mv-0.pdf), [MV-L](s0-prototype/evidence/v0.5/mv-l.pdf), [MV-P](s0-prototype/evidence/v0.5/mv-p.pdf), [MV-FT](s0-prototype/evidence/v0.5/mv-ft.pdf), [PR](s0-prototype/evidence/v0.5/pr.pdf) |
| Mentality staging | [Requested value, consequence, Submit and Cancel](s0-prototype/evidence/v0.5/mentality-dialog.pdf) |
| Substitution staging | [Outgoing shirt and incoming bench selection](s0-prototype/evidence/v0.5/substitution-dialog.pdf) |
| Live request Pending | [Pending beside unchanged current value](s0-prototype/evidence/v0.5/mv-live-pending.pdf) |
| Paused request Pending | [Waiting for resume](s0-prototype/evidence/v0.5/mv-paused-pending.pdf) |
| Applied requests | [Mentality and substitution feedback/count](s0-prototype/evidence/v0.5/mv-live-applied.pdf) |
| Refused request | [Refusal beside unchanged current value](s0-prototype/evidence/v0.5/mv-live-refused.pdf) |
| Healthy live statistics | [Open panel during play](s0-prototype/evidence/v0.5/mv-live-statistics.pdf) |
| Whistle beats request | [Full time with Not applied record](s0-prototype/evidence/v0.5/mv-ft-not-applied.pdf) |
| Incomplete report, disclosure closed | [Score, incomplete notice and return](s0-prototype/evidence/v0.5/report-incomplete.pdf) |
| Incomplete report, disclosure open | [Minute-18 partial statistics](s0-prototype/evidence/v0.5/report-partial-open.pdf) |
| Full-time statistics fault | [Ended-state notice and retained partial figures](s0-prototype/evidence/v0.5/mv-ft-statistics-fault.pdf) |
| Expanded-text fault stress | [1366-wide, pseudo-locale, 200% base font](s0-prototype/evidence/v0.5/stress-fault-1366.pdf) |

The player-facing pitch omits the raw restart and possession-holder captions. It shows goals,
penalty/goal areas and **Home attacks right / Away attacks left**, grounded in `MatchEngine`'s
`MirrorPitchIfAway` and `CheckMatchFlowTransitions`: Stage 0 retains that convention across halves.
The data itself is unchanged. Labels may move slightly to avoid overlap; the caption and accessible
description no longer promise visible leader lines. This is presentation deconfliction, not changed player positions. Synthetic possession/territory now vary
by minute, so the minute-18 cutoff differs visibly from final figures. Numeric column headers and
values share right alignment. The waiting surface has one lock explanation and a separate blank
pitch placeholder. Full-time statistics use the owner-accepted disabled-reopening choice S0-G-008; an already-open frozen panel remains visible.

**Gate C: PASS.** Hierarchy, primary actions, comparisons, density, disclosure, focus and reflow are
specified and inspectable without color/art/polish. The lack of player names remains S0-B-004's owner-accepted S0 risk, with recheck at H (protocol §9.1).

---

# 10. Gate D — full state matrix

State is scoped to a real owner or explicit prototype presentation state; irrelevant states are not
invented to populate a checklist. The seven page states remain §7.3's existing screen graph.

| Surface / state | Presentation, action availability and next safe step | Owner / fixture |
|---|---|---|
| MM default / focus | one supported entry; persistent visible label; outlined keyboard focus | A-01/02; `MM` |
| TS default / selected | Balanced checked; seven ordered values and consequences; valid selection always ready | A-09/10; `TS` |
| TS focus / staged / cancel | arrow selection stages only; Start enabled; Back discards draft, re-entry Balanced | B-DEC-3/7; complete walkthrough |
| Buttons hover / pressed | optional hover, native pressed acknowledgement; no command-success claim from click | shared §2; all actionable controls |
| Match loading / empty | waiting for first frame; score/clock withheld; controls locked with reason | A-18/22/26; `waiting`, `MV-0` |
| Live default / progress | score/minute/period/selected speed visible; sparse pitch snapshots; no percent-progress invention | A-14–20; `MV-L` |
| Playback selected / boundary disabled | 1×/3×/5×/10× text; Slower disabled at 1×, Faster at 10×, reason inline | A-19/20; walkthrough |
| Paused | Paused + selected rung; Resume; permitted staged requests can remain Pending | A-21; `MV-P` |
| Statistics healthy / open / closed | disclosure changes only presentation; #37 comparison; no shots; unavailable xG omitted | A-27/28; ordinary capture |
| Statistics partial / stale / error | A-42 alone establishes stopped data; persistent cutoff notice; no wall-time staleness guess or false-current figures | `fault`; cutoff minute 18 |
| Report statistics failure | frame result intact; incomplete notice; optional clearly captioned partial figures; Return enabled | A-29/30/42; `PR` + `fault` |
| Mentality draft / confirmation | selector + consequence; Submit requests change; Cancel/Escape no mutation | A-23; staging dialog |
| Request in progress | Pending text at action; current last-applied value remains; duplicate same-kind request suppressed | A-38; paused request |
| Request success | Applied + applied minute; current value/count reflects outcome | A-38; resumed request |
| Request refusal / failure | persistent Refused; last-applied value remains; stage a new request; no toast-only recovery | A-38; `refusal` |
| Substitution selected / cancelled | outgoing shirt and unused bench slot staged; Cancel restores invoker without count change | A-25/40/41; chooser |
| Substitution unavailable | five used → entry disabled with count/reason; illegal choices excluded; server may still refuse races | A-41; `limit` |
| Full time / frozen / end race | match actions disabled with ended reason; unresolved request Not applied — match ended; report becomes available only from MatchEnded | A-15/22/26/38; `MV-FT`, `pending-end` |
| Post-match success / return | Home win/draw/loss and score; core comparison; return via named edge; next setup starts fresh | A-29–32; `PR` / repeated loop |
| Many feedback records | latest three expanded; earlier records inspectable without burying current change controls | presentation only; `events` |
| Missing art / expanded text | neutral pitch/identity/text, no borrowed identity; wrapping labels and reachable controls | shared §§9–13; all fixtures |

An all-zero statistic from a real captured snapshot is not an empty/error state. Before any frame,
statistics are unavailable; there is no synthetic loading-to-success timer in production. Save/load,
formation, pre-match per-player tactics and abandon remain omitted. A start-error/retry control has no
audited owner and is not invented: TS's Balanced/default choice is always valid. The general failure
case is the actual command-refusal seam, separate from A-42's statistics fault.

**Gate D: PASS.** Relevant default/focus/selected/disabled/loading/empty/partial/stale/error/progress/
confirmation/success/failure states have a disposition; the design is not happy-path-only.

---

# 11. Gate E — scripted resilience evidence

## 11.1 Run identity and reference truth

The initial v0.1 Gate-E PASS is **withdrawn**: its 71 checks missed focus loss after submit/speed
boundaries and earlier-feedback collapse during live ticks. [Archived original evidence](s0-prototype/evidence/walkthrough-v0.1.json)
is retained unchanged; the old source hashes identify the superseded PR commit. The successful [74-check run 02](s0-prototype/evidence/walkthrough-v0.2.json) and [79-check run 03](s0-prototype/evidence/walkthrough-v0.3.json) are retained unchanged. The current verdict
uses **85 checks** in run `UX-GE-S0-20260930-05`, including direct focus assertions that fail on body
focus and a live-timer disclosure regression. No participant results are claimed. The prior [v0.4 run 04](s0-prototype/evidence/walkthrough.json)
is preserved; its normal-text and fault-timeline coverage claims are superseded by run 05.
Run 05 is preserved unchanged at `a859ea1`; its fingerprints describe that baseline, not the small copy correction.
[Focused delta evidence](s0-prototype/evidence/v0.5/full-time-fault-delta.json), run `UX-GE-S0-20260930-05-DELTA-01`,
records three passing checks, current seven-source/19-image hashes, and only one regenerated PDF.
Healthy full time retains its report note; faulted full time suppresses the final-statistics promise.
Run 05 tests minute 0/17/18/21 in both ordinary and direct-first-frame entry, cutoff crossing at
10× speed, full-time fault wording, and actual normal/pseudo label text and font sizes.

| Run field | Value |
|---|---|
| Gate-E run ID | `UX-GE-S0-20260930-05` |
| Journey | S0 |
| Prototype/version | `s0-prototype v0.5` |
| Date | September 30, 2026 (UTC and America/Los_Angeles) |
| Runner | Codex scripted/self-walkthrough; no independent participant claim |
| Evidence | [baseline 85 checks + hashes at a859ea1](s0-prototype/evidence/v0.5/walkthrough.json); [focused delta/current hashes](s0-prototype/evidence/v0.5/full-time-fault-delta.json); [reproducible runner](s0-prototype/verify.cjs) |
| Real-data source | `main` `c37213abf2b2b8f6ded68b125bd7c0cc524927fa`; existing `MatchClientHost`, no harness/source modification |
| Ordinary session | Explicitly synthetic 2–1 score/statistics fixture, with captured reference pitch positions; no seed/realism claim |
| Capture / unusual-score fixture | seed `0x00C11E7B6D0C`; neutral agents; home Human / away AI; 91 snapshots, ticks 1–324000, MatchEnded true, statistics observer healthy; frame score **19–9**, xG unavailable |

The unusually high recorded score is evidence as captured, not a tuned/football-realism claim. The
capture checked the two reference surfaces actually consumed: frame projection and coherent result +
observed-tick snapshot. The simulated choices do not rerun it. Prototype minute progression is compressed;
it does not demonstrate shipping playback pace or smooth rendering. Reference capture compilation
succeeded on .NET 8; existing ActionSelector CS0649 warnings were emitted. No Unity compile was run.

## 11.2 Complete protocol matrix

Every row below is for **s0-prototype v0.5** and the identified run. Results distinguish executed
prototype evidence from future shipping obligations. Relevant task traversal uses S0-T1–T7 plus the
PM-1 substitution path; additional condition fixtures inspect failures/limits without claiming they
are participant task completions.

| Condition | Result | Prototype/version | Evidence / reason |
|---|---|---|---|
| Ordinary case | PASS | v0.5 | mouse journeys at all three widths and keyboard journey: cancel/re-enter, choose/start, read state, speed/tactic/substitution, stats open/close, full time/report/return; synthetic 2–1 ordinary scenario and captured pitch; raw #37 values exercised by unusual-score journey |
| Color-independent meaning | PASS | v0.5 | monochrome complete journeys; checked radio selection, literal Paused/Pending/Applied/Refused/Full time text, Home H/Away A marker identity, dashed disabled controls and persistent reasons |
| Contrast | PASS | v0.5 | actual computed foreground/background pairs and ratios in `computed contrast`; each ≥4.5:1; 3px #161616 focus outline against white; disabled text #444 on #eee, not low opacity |
| Long player/club/competition names | PASS | v0.5 | labelled long-identity stress fixture complete journey and 1366/1920/2560 + pseudo/200% geometry; actual S0 uses shirt/slot identity, not named squads |
| Empty/large lists | N/A | v0.5 | no variable catalogue, sort/filter or career lists; S0's bounded chooser has 11 starting slots / 7 bench slots and fixed stat rows. Ordinary and limit checks exercise those bounds; genuinely empty frame is checked separately |
| Many status indicators | PASS | v0.5 | `events` fixture, 15 feedback records; latest three visible; earlier disclosure remains open with summary focus over five live ticks and pause/resume; all-width geometry and complete journey; every Applied timestamp ≤ clock |
| Pseudo-locale | PASS | v0.5 | approximately 40% bracketed expansion; complete fault/pseudo/200% journey and every applicable all-width fixture; equivalent stress content, not #49 localization runtime proof |
| Alternate date/currency formatting | N/A | v0.5 | S0 contains no dates/currencies or management account surfaces |
| No save | PASS | v0.5 | complete MM→TS→MV→PR→MM journey has no save/Continue/Load control or persistence promise |
| Save/load failure where relevant | N/A | v0.5 | no save/load action or contract is admitted in S0; general failure remains separately tested |
| No match frame yet | PASS | v0.5 | `no match frame`: waiting fixture stays non-live after clock advance, score/clock withheld, actions disabled with reason |
| Disabled action with reason | PASS | v0.5 | waiting/full-time lock reasons, real-time/fastest boundary reasons, five-substitution limit; geometry at all widths |
| Error/failure state (general) | PASS | v0.5 | `refused command`: last applied Mentality remains Balanced, persistent refusal, retry through ordinary staging; statistics failure preserves score/Return |
| Missing art | PASS | v0.5 | every complete journey uses no external art/fonts; neutral pitch and explicit Home/Away/shirt identity remain usable |
| Event-heavy match | PASS | v0.5 | `events` complete journey plus dense feedback/restart context with stats open; fixed scoreboard/action regions survive all-width pseudo/200% fixture |
| Unusual scoreline | PASS | v0.5 | unmodified real 19–9 capture complete journey, separate from ordinary 2–1 scenario; each goals table agrees with score; score identity/columns preserved at all widths |
| Full-time/frozen state | PASS | v0.5 | all complete journeys lock controls; `end race` starts at MM, reaches minute 89, pauses/submits/resumes, then resolves Pending as not applied on the live whistle tick; no direct-state load is claimed as a journey; report only available from MV-FT |
| Small desktop — 1366-wide | PASS | v0.5 | 1366×768 full mouse/keyboard journeys; all stress fixtures; [expanded-text fault view](s0-prototype/evidence/v0.5/stress-fault-1366.pdf); no horizontal overflow/clipped critical labels |
| Reference — 1920×1080 | PASS | v0.5 | complete journey, all seven wireframes, and stress fixtures; playback region above pitch keeps high-frequency controls early |
| Expanded — 2560-wide | PASS | v0.5 | complete journey/stress fixtures; capped content and compact rail, no extreme stretched control widths |
| Max supported text scale | PASS (prototype value) | v0.5 | explicitly tested **200% base font** (16px → 32px; browser zoom stays 100%) with pseudo-locale and all-width failure/chooser fixtures plus complete journey. Shipping maximum remains unallocated; Gate I must name it and Gate J must retest that actual value |
| Keyboard only | PASS | v0.5 | complete 1366 journey by Tab/arrows/Enter/Escape; cancel restores invoker; both submits immediately focus request feedback; 1×/10× boundaries focus Pause/Resume; live earlier-feedback focus survives refresh; `keyTo` rejects unexpected body focus; each chooser's Tab wrap inspected through 12 advances; headings/full-time focus deterministic |
| Mouse only | PASS | v0.5 | complete mouse journey at every width, including selectors, chooser cancel/submit, disclosures and return |
| Audio muted/caption path | PASS (prototype scope) | v0.5 | all journeys are silent; no information depends on sound. Future-caption reservation combined with every all-width pseudo/200% fixture; runtime #51 remains future-blocked, not a working caption consumer |

## 11.3 Findings, disposition and retest

Owner for each row is **Anton Zymin, UX workstream**. All findings concern **S0**.

| ID / gate | Severity | Evidence / finding | Disposition | Release condition / retest |
|---|---|---|---|---|
| S0-E-001 / E | Major | first 1366 pseudo/200% traversal found unbroken expanded text causing horizontal overflow | `FIX NOW` | fixed paragraph/flex-child wrap; final complete journey and all stress geometry PASS |
| S0-E-002 / E | Major | initial native-modal traversal let focus leave the dialog cycle | `FIX NOW` | explicit first/last Tab/Shift-Tab wrap; complete keyboard journey and 12-cycle chooser probes PASS |
| S0-E-003 / E | Minor | first reference wireframe placed playback controls after the tall pitch, below the initial viewport | `FIX NOW` | playback region moved above pitch; final screenshots and complete journeys PASS |
| S0-E-004 / E | Minor | shipping maximum text scale is not yet allocated by the owning client/a11y contract | `BLOCKED BY DOMAIN/CLIENT IMPLEMENTATION` | Gate I names actual maximum; Gate J repeats these checks at that value; current prototype evidence is 200% only |
| S0-E-005 / E | Major | v0.1 submit disabled its restored invoker; speed extremes disabled their focused control, leaving body focus | `FIX NOW` | v0.2 focuses request feedback / Pause-Resume; immediate submit and both speed-boundary assertions PASS, without Tab recovery from body |
| S0-E-006 / E | Major | v0.1 rebuilt earlier-feedback disclosure closed every live tick and lost summary focus | `FIX NOW` | v0.2 preserves open state and logical summary ID; live five-tick and pause/resume regression PASS |

S0-B-002's command-outcome adapter, S0-B-004's shirt-number usability risk, S0-B-003's future
stoppage timing, and A-42's production health projection remain the existing later-gate obligations.
They are not closed by prototype success. No new shipping Blocker/Major is concealed as visual polish.

**Gate E: PASS for this prototype.** The complete matrix has auditable results; reproduced design
Majors are fixed and retested. Numeric shipping scale, font/runtime, smooth pitch rendering, caption
runtime and pinned-host acceptance remain explicit implementation/Gate-J work, not prototype claims.

---

# 12. Gate F — complete vehicle; Gate G owner image review

## 12.1 Complete task vehicle

The [prototype](s0-prototype/index.html) supplies the four-context journey through all seven states,
cancel/back before start, repeated entry/return, current speed and pause, Mentality staging/outcomes,
substitution selection/cancel/outcomes, live statistics open/close, full time and report. Its visible
banner states that all actions are simulated, ordinary score/statistics are synthetic, and captured
pitch positions are not changed by choices. The unusual-score fixture consumes the unmodified capture.
Reviewer-only URL fixtures expose waiting, refusal, statistics failure, substitution limit, end-race,
expanded text, dense feedback and resolution/scale cases; no debug controls enter the product surface.

| Task | Prototype outcome evidenced |
|---|---|
| S0-T1 | reach Tactics Setup from the sole supported MM entry |
| S0-T2 | choose Mentality with a bounded consequence; start; current choice displayed |
| S0-T3 | score, minute/period and selected speed readable, separate Paused state |
| S0-T4 | change rung; submit Mentality; Pending distinct from Applied/Refused/Not applied |
| S0-T5 | open statistics, find Possession %, close without pausing |
| S0-T6 | Full time recognized; controls lock; report/result/stats; return MM |
| S0-T7 | change draft, Back without start, re-enter at Balanced |
| PM-1 substitution | cancel a selection without effect, submit shirt/bench choice, inspect Applied count |

These are scripted/self-walkthrough outcomes. The complete task remains available for inspection;
the prototype supplies no click-by-click instruction. The owner's September 30 decision replaces
the S0 participant round with the image review in protocol §9.1.

## 12.2 What remains real versus simulated/future

Real reference evidence: existing frame and coherent #37 snapshot ownership, roster cues, ended state,
and the actual healthy 90-minute capture used by the unusual-score fixture. Synthetic ordinary-session
score/statistics are an explicit UX fixture, not #37 validation or a tuned engine result. Simulated
prototype behavior: compressed timeline, staged
choices, queue/outcome feedback, substitution count/legality presentation, observer-fault injection
and caption-region fixture. Future production work: all P5b adapters/UGUI binding, font/localization/
a11y application, actual maximum scale and runtime audio/captions. No prototype operation writes a
`MatchSession`, command queue, `ClientScreenFlow`, engine, Unity scene, save or analytics accumulator.

## 12.3 Assessment and next gate

| Gate record | Owner | Status / evidence |
|---|---|---|
| S0 reviewer assignment | Anton Zymin | Owner-directed September 30, 2026: testers not required; owner conducts image reviews |
| Gate F complete task | UX workstream | PASS — §12.1 complete vehicle, v0.5 run 05; former tester prerequisite superseded |
| Gate G image approval | Anton Zymin | v0.4 PASS at `13c2c09`; v0.5 PASS at `0e8bd2b`, single-image delta plus 18 carried images, protocol §9.1.1 |
| Gate H high-fidelity image review | Anton Zymin | OPEN — reference v0.1 / 23 image pairs ready; separate owner approval pending (§13); H is not passed |
| Gate I implementation handoff | UX / Unity client | BLOCKED — awaits separately owner-approved H images; #470 remains blocked on I |

**Owner decision, September 30, 2026:** the owner instructed, "don't worry about testers. I will be
conducting all reviews of images". This changes S0's review method under detailed-plan F4.2;
the former S0-F-001 prerequisite is superseded. No tester availability, independence or session
completion is claimed. The later explicit owner acceptance/approval is recorded in protocol §9.1.

**Gate F: PASS.** The executable complete-task and critical-state requirements are satisfied by
the vehicle/evidence above; the owner has removed the remaining tester prerequisite.

**Gate G: PASS, September 30, 2026.** Anton Zymin explicitly accepted the proposed owner
statements and approved prototype v0.4 at `13c2c09`, the original 18 images retained in protocol §9.1. The complete record is
[validation protocol §9.1](ux-validation-protocol.md#91-s0-owner-image-review): image/source pin,
C-DEC-1 and S0-G-008 decisions, every finding disposition, and written rationale/release condition
for carried Majors S0-B-002/S0-B-004. The original 18 approved PDFs and run-04 record stay unchanged; their source version is pinned to `13c2c09`.
Image review supplies no runtime or independent usability evidence; the existing 80-check run remains
supporting scripted interaction evidence. **For current v0.5, Gate G is PASS and H is OPEN.** On September 30, 2026, Anton Zymin replied
“approved” to the corrected full-time fault image at `0e8bd2b`; protocol §9.1.1 records the delta
approval. The other 18 carry forward from approved v0.4, apart from version text. The next
high-fidelity images still need separate owner approval before I. **PR #470 remains draft/blocked on S0 Gate I.**

## 12.4 Image-review findings and required owner decisions

Claude's September 30 review covered the earlier nine PDFs. The resulting findings below are
recorded against S0, owned by the UX workstream (Anton Zymin). The owner approved the replacement images on September 30, 2026. Carried Major acceptances
are the owner's explicit decisions in protocol §9.1; the production findings remain open.

| ID | Severity | Finding | Disposition | Release condition / retest |
|---|---|---|---|---|
| S0-G-001 | Major (review coverage) | Earlier nine PDFs omitted dialogs, request outcomes and healthy live statistics | `FIX NOW` | Fixed for v0.4: original 18 images/run 04 approved at `13c2c09`, protocol §9.1. Current §9.4 adds the nineteenth fault-at-full-time view for v0.5 re-review. |
| S0-G-002 | Moderate | Raw Restart: KickOff and loose-holder captions misrepresented open play | `FIX NOW` | Fixed: both captions omitted from the player-facing pitch; source capture unchanged; absence asserted at full time and inspected across regenerated views. |
| S0-G-003 | Moderate | Missing goals, penalty areas and attacking direction | `FIX NOW` | Fixed: pitch geometry and fixed Stage-0 direction added; source grounding in §9.4; rendered live/full-time views inspected. |
| S0-G-004 | Minor | A10/H4 marker-label overlap in the 200% stress view | `FIX NOW` | Fixed: stable nearest-clear label layout; pairwise bounding-box separation asserted in all geometry checks, including every 200% fixture. |
| S0-G-005 | Minor | Minute-18 partial possession/territory identical to full-match fixture | `FIX NOW` | Fixed synthetic temporal fixture: possession 47/47 and territory 50/50 at minute 18 versus final 51/43 and 54/46. Partial/full difference asserted; raw capture unchanged. |
| S0-G-006 | Minor | Numeric report columns misaligned with team headers | `FIX NOW` | Fixed: Home/Away headers right-aligned with numeric values; computed alignment asserted and PDFs inspected. |
| S0-G-007 | Minor | Repeated starting lock explanation and placeholder crossing pitch markings | `FIX NOW` | Fixed: one control-lock reason and unmarked pitch placeholder; copy/marking assertions and MV-0 image inspected. |
| S0-G-009 | Minor | Focus outlines touched dropdown labels in both dialogs | `FIX NOW` | Fixed: .75rem label/field gap; visible focus and at least 4px outline/label clearance measured for every dropdown at normal and pseudo/200% text at all three widths. Replacement dialog PDFs inspected. |
| S0-G-010 | Minor | Caption promised leader lines that were not visibly useful | `FIX NOW` | Fixed: removes that promise from visible and accessible descriptions; overlap separation remains asserted. Expanded-text image inspected. |
| S0-G-011 | Minor | Circular refusal wording implied an explanation the engine does not supply | `FIX NOW` | Fixed: “Refused. Current Mentality unchanged.”; status and requested value retained, unchanged applied value asserted. No reason invented. |
| S0-G-012 | Minor | Substitution help used developer-facing engine language | `FIX NOW` | Fixed: describes when the substitution takes effect in player language; the current no-stoppage behavior remains explicit. |

**Codex correction findings, September 30, 2026** (new revision v0.5):

| ID | Severity | Finding / source review | Disposition | Fix / retest |
|---|---|---|---|---|
| S0-G-013 | Moderate | Fault seeded too early for direct MV-0; normal MM entry clears it but the timer then activates at minute 19. [Codex thread](https://github.com/antonzymin-eng/Soccer-Manager-Pro/pull/473#discussion_r4148489967) | `FIX NOW` | Fixed: fixture fault applied after progression reaches/crosses cutoff 18; direct states at/after cutoff initialize it separately. Both entry paths assert minute 0/17 healthy, 18 faulted, 21 frozen; 10× crossing also checked. |
| S0-G-014 | Moderate | Full-time fault notice claimed play continues. [Codex thread](https://github.com/antonzymin-eng/Soccer-Manager-Pro/pull/473#discussion_r4148489976) | `FIX NOW` | Fixed: Match View at full time uses incomplete/final-score wording. Live-through-whistle test and the new fault-at-full-time PDF show frozen partial values, final score and report access. |
| S0-G-015 | Minor (evidence) | Normal-text clearance branch used pseudo=0, which still enables pseudo labels. [Codex thread](https://github.com/antonzymin-eng/Soccer-Manager-Pro/pull/473#discussion_r4148489984) | `FIX NOW` | Fixed: normal query omits pseudo; asserts actual plain versus expanded labels and 16/32px root fonts before measuring focus clearance. Prior run-04 normal-text claim superseded. |

**S0-G-008 — `ACCEPT FOR CURRENT GATE`, owner-accepted September 30, 2026.** Statistics
cannot be reopened at full time. Retain an already-open frozen panel; the note points to the match
report, and View match report remains primary. Acceptance statement: protocol §9.1.

| Gate-G decision record | Current value / required owner action |
|---|---|
| Review ID | `UX-GG-S0-OWNER-20260930-03` |
| Reviewer | Anton Zymin |
| Review version | APPROVED v0.4 at `13c2c09`; original 18 image paths/hashes and run-04 source fingerprints in protocol §9.1 |
| Owner review date / statement | September 30, 2026; verbatim owner confirmation and accepted decisions in protocol §9.1 |
| Image approval | APPROVED by Anton Zymin — prototype v0.4 at `13c2c095bea6876ffd010dfad1df23fb4f137868`, original 18 PDFs listed in protocol §9.1 |
| Finding dispositions | S0-G-001–007 and S0-G-009–012 fixed/retested above; S0-G-008 `ACCEPT FOR CURRENT GATE`; earlier B/E ledgers still apply |
| C-DEC-1 | ACCEPTED — labelled partial live figures; report partial figures behind incomplete disclosure, score/result/Return outside; protocol §9.1 |
| Carried Majors | S0-B-002 accepted for G and S0-B-004 accepted for S0; verbatim owner reasons/release conditions in protocol §9.1 and §7.9 ledger references; findings remain open for H/I/J follow-up |
| Full-time statistics access | S0-G-008 `ACCEPT FOR CURRENT GATE` — no reopening; retained-open panel and report note; protocol §9.1 |
| Gate G | PASS — owner approval/decisions recorded; no unresolved Blocker or unaccepted Major |
| Current revision | v0.5 G PASS at `0e8bd2b`, protocol §9.1.1; H OPEN, with separate H image approval before I. #470 remains blocked on I. |

---

**Additional v0.5 delta finding:** S0-G-016 (Minor), `FIX NOW`: full-time fault view also promised
final report statistics. Fixed by showing that note only for healthy statistics; three focused
checks pass and only the new full-time fault PDF is regenerated. See the delta evidence in §11.1.

## 12.5 H/I follow-ups from image review

These obligations remain open after the pinned v0.4 approval and v0.5 single-image delta approval.
H is OPEN for high-fidelity work and separate owner image review; no H/I approval is supplied.

| Gate | Follow-up | Closure evidence |
|---|---|---|
| H | Review Mentality choice presentation: the live dropdown shows one selected option while setup exposes all seven choices and effects. | Owner-approved high-fidelity images show a deliberate, understandable choice/consequence presentation in both contexts. |
| H | Applied substitution feedback must agree with the pitch. Current captured markers do not update, despite the simulated count/feedback. | Replacement images show a coherent post-swap projection, or a clearly labelled illustrative scenario with consistent identity; unchanged captured markers may not imply a real applied swap. Recheck S0-B-004 identity. |
| H / S0-H-001 (Minor) | Remove the stray dot from the near-zero-length displaced-label leader in the 200% stress view. `FIX NOW` at H. | H stress images retain separated markers without an unexplained leader dot. |
| H / S0-H-002 (Minor) | Display bench slots as 1–7, keeping engine indices 0–6 internal. `FIX NOW` at H. | H substitution images use 1-based player-facing labels; I maps display labels to unchanged engine indices. |
| H / S0-H-003 (Minor) | Polish “Submission requests the change; feedback confirms the outcome” into player language. `FIX NOW` at H. | Owner-approved H dialog copy preserves Submit/Cancel and requested-versus-applied meaning without developer phrasing. |
| H / S0-H-004 (Minor) | Review disabled “Close statistics” on an open full-time panel. Preserve the accepted no-reopening rule, but choose a clearer presentation. `FIX NOW` at H. | Owner-approved H full-time images avoid an apparently actionable disabled close control; any change to S0-G-008 is explicitly decided. |
| I | The prototype direction label is fixed to the verified Stage-0 home +X convention. Stage 0 deliberately does not swap ends. | The handoff identifies the engine-owned direction projection required when Stage-1 ends-swap lands; shipping text follows that projection and is not inferred from minute or hardcoded. |

---

# 13. Gate H — high-fidelity reference

**Status:** reference v0.1 ready for separate owner image review; H OPEN, I/#470 BLOCKED.
**Base:** PR #473 head `b6c9c6a41be01fd3950f8e74755eda4672f124a2`, containing the owner-approved
v0.5 delta record. The owner authorized starting H on that base before all #473 checks completed.
That instruction authorizes H work, not H image approval or either PR's merge.

The [H reference and complete 23-image index](s0-high-fidelity/README.md) applies the accepted
touchline / Retro Dynamo Blue style. It reads the same immutable G model/capture/scenario, with a
separate renderer, stylesheet and verification record. G's complete source/evidence tree remains
byte-identical to the base. The current four screens/five moves and owner-approved C-DEC-1/S0-G-008
behavior remain. No production, scene, schema, RNG, spec or client binding is changed.

## 13.1 Carried findings and identity recheck

| Finding / obligation | H v0.1 implementation and author verification | Owner review / production boundary |
|---|---|---|
| S0-H-001 stray leader dot | Leader endpoints have no circles; only materially displaced labels have lines. Stress geometry retains non-overlapping markers. | FIX implemented; confirm stress image; owner closure pending. |
| S0-H-002 bench indices | Display Home shirt 12–18 and bench slot 1–7; seven option values stay 0–6. | FIX implemented; confirm substitution image; I maps labels to existing indices. |
| S0-H-003 submit copy | “Choose a change, then submit it. Your team changes only when you see Applied.” Submit/Cancel preserved. | FIX implemented; confirm both staged dialogs; no application predicted on selection. |
| S0-H-004 full-time statistics | Replace disabled statistics button with static retained/closed panel status. No reopen/close action at full time; report stays primary. | FIX implemented; confirm both retained panels and closed panel; S0-G-008 unchanged. |
| Mentality choice consistency | Live requested choice/effect plus comparison of all seven identical setup labels/effects. | Confirm both contexts and expanded comparison; no new tactical semantics. |
| Applied substitution / S0-B-004 | Explicit illustrative overlay changes H4 to H14 ↔ after Applied, matching shirt 4→14 feedback; Pending/Refused retain H4. 22 markers and used-choice exclusion checked. | Shirt-only identity rechecked in author images; owner must decide whether it identifies players sufficiently. S0-B-004 remains OPEN for production names. If owner cannot identify players at H, reopen as Blocker. |

These are implementation/retest dispositions, not owner acceptance or H PASS. S0-B-002 still requires
the I feedback adapter and J runtime verification. Engine-owned attack direction for future Stage-1
ends-swap remains the I obligation in §12.5. This reference retains verified fixed Stage-0 direction.

## 13.2 Components, art and copy

The [reference README](s0-high-fidelity/README.md#design-and-boundaries) records shared component
usage, states, copy roles and fallbacks. Text wordmark/menu treatment replaces unavailable key art;
Home/Away text substitutes for crests; visible shirt markers identify players without portraits or
invented names. Navy/blue structure, cream text, gold selection, distinct warning treatment and
textual outcomes follow the accepted art direction. Offline system-font fallback is exercised;
AP-03 shipping font/glyph/packaging proof remains separate. No asset or locale catalog is invented.

## 13.3 Evidence and approval requirement

`UX-H-S0-20260930-01`: 90 PASS checks, 23 PNG/PDF image pairs and source/image hashes in
[s0-high-fidelity/evidence/v0.1/walkthrough.json](s0-high-fidelity/evidence/v0.1/walkthrough.json).
All PDFs were rendered through Poppler and inspected. Coverage includes complete tasks, request
states, both incomplete disclosures, fault timing, normal/expanded text, three desktop widths,
keyboard recovery, substitution identity and undistorted pitch proportions. See the README for
exact evidence limits; this supplies neither runtime nor independent usability evidence.

**Owner H review is pending.** Approval must identify the reviewed H commit and the complete image
set/hashes. Record the actual confirmation in validation protocol; do not infer it from G approval,
starting H, passing checks or opening a PR. After that separate approval, I may begin. #470 remains
blocked until the Gate-I handoff. Merge remains dependent on #473 and the applicable CI checks.

---

# 14. Version history

| Version | Date | Notes |
|---|---|---|
| 0.1 | September 12, 2026 | Created S0 UX-D packet and completed Gate A against `main` `ddd221c9`; revalidated PR #404 Unity compile evidence, P5b absence, repeated-match lifecycle ownership, live/control/stat seams and cross-stream constraints. Gate B next. |
| 0.2 | September 21, 2026 | Reconciled PR #406 onto current `main` `ad7e0d75` after #407. Re-ran the Gate-A current-state claims: P5b remains absent; the four-screen/five-edge client graph, lifecycle, match projection/dispatch, playback/control and #37 analytics ownership remain valid; P4b advances from compiler-only evidence to partial host verification (tracked-scene Play-mode boot/render smoke) without overstating click/perf/Gate-J acceptance. Gate A remains PASS; Gate B is next. |
| 0.3 | September 21, 2026 | Review correction: A-11 no longer treats the `PlayerTactic` value type as proof of a pre-match action/state seam. `MatchSetup` has no per-player tactic holder/builder and `MatchSession.BootEngine` applies no per-player setup state, so Role/Duty/Instructions are explicitly `FUTURE-BLOCKED` for pre-match editing until a setup persistence/handoff contract exists. The existing live `SetPlayerTactic` dispatcher remains valid for in-match intervention. This converts an overstated seam into a named blocker; the Gate-A PASS and zero-`UNKNOWN` result remain valid, and Gate B is constrained to a verified team-tactic pre-match choice. |
| 0.4 | September 21, 2026 | Review closeout: repins the maintained execution/validation authority headers to `ux-detailed-plan.md` v1.8 / `ux-validation-protocol.md` v0.11 and makes the Gate-A verdict taxonomy explicit. `UNWIRED` means an existing contract lacks production presentation/binding; `FUTURE-BLOCKED` means the required contract/state/runtime capability itself is absent. This prevents the A-07/A-11 contract gaps from being laundered later as P5b-only binding work. Gate A remains PASS; Gate B remains next. |
| 0.5 | September 28, 2026 | **S0 Gate B complete; B-DEC-1/2/5 owner-confirmed September 28, 2026.** Adds §7 Gate B: seven journey states over the four typed screens using only the five existing `ClientScreenFlow` moves; per-state entry/goal/information/actions/alternate/back/completion; the requested/applied/refused/not-applied intervention feedback model over `MatchSession.Driver.Log`/`FailedCommands`; blocked-path table with owner-sourced reasons; #37 statistic set (no shots row; xG only when available); S0-T1–T7 + PM-1 substitution coverage; findings S0-B-001–009. Gate-A re-check at `main` `ee37aa60` **corrects A-10**: `TeamTactic.Formation` has no simulation consumer, so S0's pre-match choice is Mentality; adds A-38–A-42 (command-outcome logs, AI opponent mode, shirt-number identity, substitution legality inputs, statistics health). A-42 / S0-B-009 were added before merge in response to automated review of PR #472: the flow now surfaces an analytics-observer fault in place of presenting frozen statistics as current. B-DEC-1/2/5 explicitly owner-confirmed September 28, 2026; B-DEC-5 states full time as the authoritative transition and the report control as acknowledgement only. Adds §8 Gate C inputs. No `src/` change; P5b remains gated on Gate I. |
| 0.6 | September 30, 2026 | Adds Gates C/D information design and state matrix; seven monochrome wireframes and a complete interactive prototype under `s0-prototype/`; real 91-snapshot MatchClientHost capture; executed Gate-E resilience matrix with fixed/retested overflow and modal-focus findings. C–E PASS; F deliverable READY but formal opening/pass awaits both pre-F participant attestations (S0-F-001). Statistics-failure decision retains labelled live partial figures and hides report partial figures behind disclosure. No production/spec/Unity changes; G requires real independent sessions, H/I unopened, #470 remains blocked. |
| 0.7 | September 30, 2026 | PR #473 review correction: withdraws initial E pass; fixes submit/speed focus and live feedback disclosure Majors; 74-check run 02 passes with direct focus and live-timer assertions. Coherent synthetic 2–1 ordinary session; raw 19–9 capture reserved for unusual-score checks; normal-play whistle race; removes contradictory captured substitution row; distinguishes base-font scaling from zoom. Moves tester attestations from findings to formal F prerequisites. C-DEC-1 remains a Gate-C design choice, not an owner-confirmed decision; no gate rule requires its confirmation. F/G/H/I remain unpassed, #470 blocked. |
| 0.8 | September 30, 2026 | Records explicit owner instruction to conduct image reviews instead of testers. S0-F-001 prerequisite superseded; F passes existing complete vehicle. G awaits actual owner image approval, including failure/stress views and carried design findings. H/I remain unopened; #470 blocked. |
| 0.9 | September 30, 2026 | Image-review correction: 18 PDFs cover dialogs, queued/applied/refused/not-applied requests and healthy/partial statistics. Removes raw pitch restart/holder captions, adds grounded pitch/direction markings and collision-free label placement with leaders, fixes temporal statistics/header alignment/waiting copy. Records S0-G-001–008 dispositions and explicit pending C-DEC-1/full-time-statistics/carried-Major/image decisions. Run 03 has 79 checks; successful run 02 archived. G opens H only; H approval separately precedes I. |
| 0.10 | September 30, 2026 | Second image-review cleanup: S0-G-008 moved to pending owner decisions; dropdown focus spacing measured; leader-line promise removed; concise refusal and player-facing substitution copy. Records H choice/identity consistency and I engine-direction obligations. Prototype v0.4 run 04 has 80 checks; run 03 retained. Owner choices and G/H/I approval remain pending. |
| 0.11 | September 30, 2026 | Records actual owner acceptance of C-DEC-1/S0-G-008 and carried Majors, with durable rationale/release-condition references, and approval of v0.4 / 13c2c09 / all 18 PDFs. G PASS; H OPEN; I/#470 blocked. Carries stray dot, 1-based bench labels and dialog copy into H. Prototype/evidence unchanged. |
| 0.12 | September 30, 2026 | Fixes Codex S0-G-013–015: fault-cutoff timing, full-time notice, actual normal-text clearance. Current v0.5 has 85 checks and 19 PDFs under evidence/v0.5; original 18 PDFs/run04 unchanged. G reopened for current-revision owner review; pinned v0.4 approval/decisions preserved; H paused, I/#470 blocked. |
| 0.13 | September 30, 2026 | Fixes the full-time fault report-note contradiction with three focused checks and one regenerated image. Narrows pending v0.5 review to that image; carries the other 18 from approved v0.4 apart from version text. Adds disabled Close statistics to H backlog. Original approval and accepted decisions preserved; no new approval inferred. |
| 0.14 | September 30, 2026 | Records actual owner approval of the corrected v0.5 full-time fault image at 0e8bd2b, carrying the other 18 forward from approved v0.4 apart from version text. G PASS; H OPEN; I/#470 blocked on separately approved H images. Documents-only recording preserves reviewed source, PDFs and evidence. |
| 0.15 | September 30, 2026 | Starts separate H v0.1 on PR #473 / b6c9c6a: touchline, carried fixes, coherent illustrative substitution shirts, 90 checks and 23 image pairs. H owner review pending; G preserved, I/#470 blocked. |
