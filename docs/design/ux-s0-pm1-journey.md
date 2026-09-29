# System XI — S0 PM-1 Journey Packet

**Created:** September 12, 2026  
**Last Updated:** September 28, 2026  
**Version:** 0.5  
**Status:** S0 GATES A–B COMPLETE — GATE C NEXT (Gate B closes on owner merge; see §7.10)  
**Execution authority:** [`ux-detailed-plan.md`](ux-detailed-plan.md) v1.9 §5–§6  
**Validation task authority:** [`ux-validation-protocol.md`](ux-validation-protocol.md) v0.12  
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

The Gate-A verdict survives the correction. A-10 moves from a partly overstated `DESIGNABLE` to an explicit
split, and no row becomes `UNKNOWN`.

## 7.2 Flow decisions

Gate B may choose ordering and wording within the audited surfaces. These are the choices that shape the flow.
B-DEC-1, B-DEC-2 and B-DEC-5 are product-visible and are put to the owner at merge (§7.10).

| ID | Decision | Rationale | Seam |
|---|---|---|---|
| B-DEC-1 | The player manages the **home** team (team 0). Every manager intent carries `TeamId = 0`. | One controlled side keeps S0 to "your team vs an opponent"; home is team 0 in every existing seam. | A-09, A-23, A-25 |
| B-DEC-2 | The opponent is **AI-managed** (`AwayManagerMode = AI`), using the default manager profile. | A static opponent that never reacts misrepresents a management match. If the owner declines, the fallback is `Human`/no input, and the flow is otherwise unchanged. | A-39 |
| B-DEC-3 | The pre-match choice is **Mentality**, seven ordered values from Very Defensive to Very Attacking, defaulting to Balanced. All other team-tactic axes stay at their `Balanced` defaults and are not shown in S0. | One consequential, ordered, explainable choice satisfies S0-T2. The other axes are real but would widen S0 past "one understandable choice"; S1 revisits them. | A-10 (corrected) |
| B-DEC-4 | The in-match tactical adjustment is the **same Mentality control**, sent as a team-tactic change. Per-player tactic change stays available as a seam (A-24) but is **not** in the S0 required path. | Re-using the pre-match concept makes S0-T4 learnable and testable. The per-player surface needs player identity, which A-40 limits to shirt numbers. | A-23, A-26 |
| B-DEC-5 | Full time does **not** auto-navigate. Match View enters a full-time state on the frame where `MatchEnded` is true, and a single primary action, "View match report", invokes `ShowPostMatchReport()`. | S0-T6 tests recognition of the frozen full-time state. An automatic `Replace` would remove that moment. The action uses the existing edge, so no edge is invented. | A-05, A-15, A-30, A-31 |
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
  - **Live statistics:** open/close (A-28). Contents in §7.7.
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
- **Actions:** "View match report" → `ShowPostMatchReport()` (A-31) → PR. This is the only available action.
- **Back/cancel:** none. There is nothing to return to (A-07).

### B-7 Post-Match Report (PR)

- **Entry trigger:** B-6.
- **Player goal:** understand the result and core statistics, then leave (S0-T6).
- **Required information:** final score and result (win/draw/loss from the home side's view), and both teams'
  #37 statlines (§7.7). No causal analysis or ratings that #37 does not supply (A-29).
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

Disabled states appear only where the capability is real and the current state blocks it (F2 §6). Capabilities
that are future-blocked, such as Formation, pre-match Role/Duty/Instructions, save, quit, rematch and abandon, are
**omitted**, not shown disabled.

## 7.7 Information the flow requires from #37

Live statistics (B-4) and the report (B-7) use the same `MatchAnalyticsResult` owner (A-27, A-29, S0-A-004).
Both show, for home and away, only what `MatchStatline` / `AdvancedStatline` carry:

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
| S0-B-002 | Major | No frame read-back of the active tactic; outcome evidence exists only in the command logs (A-38), which no #38 adapter projects. | `ACCEPT FOR CURRENT GATE`. §7.5 defines the states from A-38. | Gate I must name the adapter that projects `Log`/`FailedCommands` to the Match View; Gate J verifies it. |
| S0-B-003 | Minor (S0) | Substitutions apply immediately; the owner's stoppage rule is not implemented. | `BLOCKED BY DOMAIN/CLIENT IMPLEMENTATION` for the future state; today's behavior designed. | When the owning engine change lands, §7.5 returns to Gate B (§13). |
| S0-B-004 | Major (usability risk) | The neutral demo has no player names. Substitution relies on shirt numbers and bench slots (A-40). | `ACCEPT FOR CURRENT GATE`; measured at Gate G. | Before Gate H: either choose an S0 squad source (Gate-A addendum) or accept shirt-number identity on Gate G evidence. |
| S0-B-005 | Minor | B-DEC-5 chooses a player-invoked full-time → report action. PR #361's closing plan note said the transition should be driven by `FullTime` rather than "an invented button". | `ACCEPT FOR CURRENT GATE`, pending owner confirmation at merge. The action uses the existing `ShowPostMatchReport` edge and is enabled only by the `FullTime` state, so it honors the note's intent (no new edge, no UI-derived end detection). | Owner confirms or reverses at merge. If reversed, B-6 becomes a timed or automatic transition and Gate C retests S0-T6. |
| S0-B-006 | Minor | No quit/exit control exists in the Gate-A inventory. The desktop window close is the only exit. | `DEFER TO P2/P3` — requires a Gate-A addendum naming the host quit seam. | Before Gate I. |
| S0-B-007 | Minor | No half-time interval exists in the engine. The second half follows directly. | `ACCEPT FOR CURRENT GATE`. | Re-evaluate at Gate G if participants expect a break. |
| S0-B-008 | Minor | Whether requests made while paused apply before resume depends on whether the P5b binding services the command queue off-tick (`ServiceOnce`). | `ACCEPT FOR CURRENT GATE`. §7.5 shows Pending either way, so the flow is correct under both. | Gate I states the binding's paused-servicing behavior. |

## 7.10 Gate B verdict

**PASS on owner merge.** The S0 flow is coherent without visual styling:

- every state has an entry trigger, a goal, required information with a cited owner, actions bound to existing
  seams, explicit alternate/blocked behavior, cancel/back behavior and a completion/return destination;
- the only navigation used is the five existing `ClientScreenFlow` moves;
- no screen, edge, simulation command, statistic, setting or production capability is added beyond the Gate-A
  inventory as amended in §7.1.

Merging this revision is the owner's confirmation of **B-DEC-1** (player manages home), **B-DEC-2** (AI-managed
opponent) and **B-DEC-5** (player-invoked full-time → report). If any is declined in review, Gate B stays open on
that item alone.

Gate B does **not** release P5b (Gate I), pass any later gate, or change any `src/` surface.

---

# 8. Gate C inputs — next work

Gate C produces low-fidelity information design for the seven states in §7.3: hierarchy, density, primary action,
navigation, comparison, progressive disclosure, focus sequence and reflow. Carry forward:

1. MV-L's always-visible set (§7.4 B-4) and its placement against the pitch;
2. the §7.5 request-state treatment at the intervention controls, which must stay readable without color;
3. the Mentality scale's ordered presentation and consequence text (B-2), within localization/reflow limits;
4. the substitution chooser built on shirt number and bench slot (A-40, S0-B-004);
5. the MV-FT frozen state and its single primary action;
6. the §7.7 statistic set with no shots row, and xG omitted when unavailable;
7. everything omitted in §7.6 stays omitted. Gate C may not reintroduce it as disabled chrome.

---

# 9. Version history

| Version | Date | Notes |
|---|---|---|
| 0.1 | September 12, 2026 | Created S0 UX-D packet and completed Gate A against `main` `ddd221c9`; revalidated PR #404 Unity compile evidence, P5b absence, repeated-match lifecycle ownership, live/control/stat seams and cross-stream constraints. Gate B next. |
| 0.2 | September 21, 2026 | Reconciled PR #406 onto current `main` `ad7e0d75` after #407. Re-ran the Gate-A current-state claims: P5b remains absent; the four-screen/five-edge client graph, lifecycle, match projection/dispatch, playback/control and #37 analytics ownership remain valid; P4b advances from compiler-only evidence to partial host verification (tracked-scene Play-mode boot/render smoke) without overstating click/perf/Gate-J acceptance. Gate A remains PASS; Gate B is next. |
| 0.3 | September 21, 2026 | Review correction: A-11 no longer treats the `PlayerTactic` value type as proof of a pre-match action/state seam. `MatchSetup` has no per-player tactic holder/builder and `MatchSession.BootEngine` applies no per-player setup state, so Role/Duty/Instructions are explicitly `FUTURE-BLOCKED` for pre-match editing until a setup persistence/handoff contract exists. The existing live `SetPlayerTactic` dispatcher remains valid for in-match intervention. This converts an overstated seam into a named blocker; the Gate-A PASS and zero-`UNKNOWN` result remain valid, and Gate B is constrained to a verified team-tactic pre-match choice. |
| 0.4 | September 21, 2026 | Review closeout: repins the maintained execution/validation authority headers to `ux-detailed-plan.md` v1.8 / `ux-validation-protocol.md` v0.11 and makes the Gate-A verdict taxonomy explicit. `UNWIRED` means an existing contract lacks production presentation/binding; `FUTURE-BLOCKED` means the required contract/state/runtime capability itself is absent. This prevents the A-07/A-11 contract gaps from being laundered later as P5b-only binding work. Gate A remains PASS; Gate B remains next. |
| 0.5 | September 28, 2026 | **S0 Gate B complete (on owner merge).** Adds §7 Gate B: seven journey states over the four typed screens using only the five existing `ClientScreenFlow` moves; per-state entry/goal/information/actions/alternate/back/completion; the requested/applied/refused/not-applied intervention feedback model over `MatchSession.Driver.Log`/`FailedCommands`; blocked-path table with owner-sourced reasons; #37 statistic set (no shots row; xG only when available); S0-T1–T7 + PM-1 substitution coverage; findings S0-B-001–008. Gate-A re-check at `main` `ee37aa60` **corrects A-10**: `TeamTactic.Formation` has no simulation consumer, so S0's pre-match choice is Mentality; adds A-38–A-41 (command-outcome logs, AI opponent mode, shirt-number identity, substitution legality inputs). Owner confirmation requested at merge for B-DEC-1/2/5. Adds §8 Gate C inputs. No `src/` change; P5b remains gated on Gate I. |
