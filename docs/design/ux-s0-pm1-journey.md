# System XI — S0 PM-1 Journey Packet

**Created:** September 12, 2026  
**Last Updated:** September 21, 2026  
**Version:** 0.2  
**Status:** S0 GATE A COMPLETE — GATE B NEXT  
**Execution authority:** [`ux-detailed-plan.md`](ux-detailed-plan.md) v1.5 §5–§6  
**Validation task authority:** [`ux-validation-protocol.md`](ux-validation-protocol.md) v0.8  
**Evidence snapshot:** `main` at `ad7e0d751f978c8785e7bab2024b99ff5a8da26d` (PR #406 reconciliation base)

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
| A-10 | Formation / team tactic choice | `TeamTactic` is the initial tactic carried by `MatchSetup`; F1 semantic audit already verified Formation/Mentality and the supported team-tactic axes | no P5b pre-match adapter/control | `DESIGNABLE / UNWIRED` | Gate B chooses the smallest understandable verified tactic choice; no speculative FM-style semantics |
| A-11 | Player Role/Duty/Instructions | `PlayerTactic` vocabulary is implemented and bounded by the F1 semantic audit | no P5b pre-match adapter/control | `DESIGNABLE / UNWIRED` | retain only exact implemented values; no arbitrary instruction lists |
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

---

# 5. Gate A verdict

**PASS — S0 Gate A is complete on reconciled evidence snapshot `ad7e0d751f978c8785e7bab2024b99ff5a8da26d`.**

There are **zero `UNKNOWN` capability dependencies** supporting the S0 design premise. Every S0 task/control is
one of:

- backed by an implemented owner/read/action/navigation contract and therefore `DESIGNABLE`; or
- explicitly `FUTURE-BLOCKED`/`UNWIRED` at the production presentation/binding layer.

The pass does **not** mean P5b, full P4b host acceptance, on-host P6, #48, #49 or #51 runtime work is complete.
It means S0 can proceed to Gate B without inventing product behavior.

---

# 6. Gate B inputs — next work

Gate B must now define one coherent S0 task flow using only the audited surfaces above. It must include:

1. launch/root state and PM-1/demo match entry;
2. Tactics Setup entry, one clear verified pre-match choice, cancel/re-entry, and start;
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

# 7. Version history

| Version | Date | Notes |
|---|---|---|
| 0.1 | September 12, 2026 | Created S0 UX-D packet and completed Gate A against `main` `ddd221c9`; revalidated PR #404 Unity compile evidence, P5b absence, repeated-match lifecycle ownership, live/control/stat seams and cross-stream constraints. Gate B next. |
| 0.2 | September 21, 2026 | Reconciled PR #406 onto current `main` `ad7e0d75` after #407. Re-ran the Gate-A current-state claims: P5b remains absent; the four-screen/five-edge client graph, lifecycle, match projection/dispatch, playback/control and #37 analytics ownership remain valid; P4b advances from compiler-only evidence to partial host verification (tracked-scene Play-mode boot/render smoke) without overstating click/perf/Gate-J acceptance. Gate A remains PASS; Gate B is next. |
