# System XI — Detailed UX Execution Plan

**Created:** September 4, 2026  
**Last Updated:** September 4, 2026  
**Version:** 1.1  
**Status:** PLAN — F0 CLOSED September 6, 2026; F1 MAY BEGIN  
**Parent:** [`ux-high-level-plan.md`](ux-high-level-plan.md)  
**Release target:** Early Access centered on PM-2, with PM-1 as prerequisite

---

## 0. Purpose

This is the single execution authority for UX work. It defines work packages, Gates A–J, dependencies, validation mechanics, handoffs, ownership, and the exact next sequence.

It does not redefine domain behavior. Authority remains:

1. APPROVED specification;
2. governing client implementation/design documents;
3. verified production behavior;
4. explicitly labelled future UX intent;
5. visual reference.

The current client documents that must be read before PM-1 work are:

- `docs/tracking/interactive-unity-client-design.md`;
- `docs/tracking/ui-client-framework-design.md`;
- `docs/tracking/ui-framework-t0-implementation-plan.md`;
- `docs/tracking/path-to-playable-roadmap.md`;
- `src/client-app/ClientScreens.cs`;
- `src/client-app/ClientScreenFlow.cs`;
- `src/match-client-unity/README.md`.

The current four-screen/five-edge PM-1 graph is **input**, not a design question.

---

# 1. Plan convergence and external critique

The detailed plan went through three internal critique/revision rounds before v1.0. A subsequent repository-grounded review found additional defects.

## 1.1 Findings accepted

- S0 incorrectly implied the shipping Unity client could currently be launched.
- S0 did not name B8/P4b host verification, B9b/P5b or B10/P6 as implementation dependencies.
- Gate G's 3–5-person aspiration plus a provisional escape hatch could become a permanent no-op.
- The plan omitted the documents that already own screen identities, navigation, P4/P5 splits and the `MonoBehaviour` layering rule.
- The package was not routed through normal tracking/discovery surfaces.
- Gates were duplicated between high-level and detailed plans.
- No effort bands or role ownership were stated.
- The existing browser/replay surfaces were not used as the obvious host-free real-data reference vehicle.

## 1.2 Status claims corrected rather than accepted

Two review claims were stale against the current tree:

- **P4b is landed as code.** `src/match-client-unity/README.md` records `MatchClientBehaviour.cs` as LANDED after five AR rounds. The real gap is that this assembly is excluded from the shim and the P4b binding has never compiled/run on the pinned Unity host; P5b and on-host P6 remain open.
- **#30 is implemented.** Its implementation is in `src/season-save/`: `SeasonLoop`, season save state, league bootstrap and T2 round/day progression exist, including `AdvanceAndPlayNextRound`. The meaningful S1 question is which player-facing projection, dispatcher, screen and navigation surfaces exist.

The revised plan below incorporates the structural concerns while using the verified current state.

---

# 2. Durable UX artifacts

Keep documentation lean. UX uses four artifact classes.

## UX-A — Baseline & Evidence Pack

Contains:

- current capability matrix;
- current verification qualifiers;
- existing mockup reconciliation;
- player/task assumptions;
- PM-1/PM-2 task hierarchy;
- current-vs-target navigation map;
- release-scope matrix;
- open UX decisions.

## UX-B — Shared System Pack

Contains only cross-journey rules actually required by S0/S1:

- component/state rules;
- keyboard/focus behavior;
- data-density/table behavior;
- localization/reflow;
- accessibility application expectations;
- art/fallback slots;
- audio/caption placement;
- desktop layout/resolution behavior.

## UX-C — Validation Protocol

Contains:

- scripted walkthroughs;
- participant recruitment/test procedure;
- severity/disposition rules;
- test-data profiles;
- retest rules;
- Unity implementation-verification checklist.

## UX-D — Journey Packet

One packet per journey:

- Gate A audit;
- flow;
- low-fidelity wireframes;
- state matrix;
- cross-stream findings;
- interactive prototype;
- usability evidence;
- high-fidelity references;
- implementation mapping;
- QA cases;
- blockers/deferred items;
- version/change record.

No separate document is required for every subsection.

---

# 3. Capability labels

Every relevant capability receives one label plus a verification qualifier when material.

- `LIVE` — production behavior exists.
- `DESIGNABLE` — owning read/action behavior exists, but player-facing presentation does not.
- `FUTURE-BLOCKED` — implementation needs an owning-system/client change that is absent.
- `OUT-OF-EA` — deliberately outside the Early Access cut.
- `UNKNOWN` — evidence insufficient; cannot be used as a design premise.

Qualifiers include:

- `HOST-UNVERIFIED`;
- `HOST-VERIFIED`;
- `GATE-COMPILED`;
- `SPEC-ONLY`;
- `UNWIRED`;
- `REFERENCE-HARNESS`.

Example: P4b is `LIVE / HOST-UNVERIFIED`, not "unimplemented" and not "verified".

---

# 4. Foundation work

## F0 — Planning/tracking close-out

Before F1 begins:

- `ux-high-level-plan.md` is the strategy authority;
- this file is the execution authority;
- `ux-foundation.md` is bannered as superseded historical context;
- existing `Main Menu.html` is marked provisional;
- repository tracking routes agents to these plans;
- PR #362 remains draft until the planning/tracking corrections are reviewed.

No new high-fidelity UX production occurs during F0.

## F1 — Baseline & Evidence Pack

### F1.1 Current capability matrix

Audit at minimum:

- `src/ui-framework/`;
- `src/client-app/`;
- `src/match-client-core/`;
- `src/match-client-unity/`;
- `src/match-viewer/`;
- `src/match-client-web/`;
- `src/match-analytics/`;
- `src/season-save/`;
- current #38/#48/#49/#51 implementation surfaces;
- later management assemblies relevant to S2.

For each capability record separately:

- domain/read seam;
- command seam;
- UI projection/adapter;
- screen identity;
- navigation edge;
- rendering/binding;
- test/compile state;
- host-cert state;
- capability label.

**Do not infer absence from folder naming.** #30 is the precedent: its implementation is in `src/season-save/`, not `src/season-competition-loop/`.

**Do not trust stale comments as current status.** A source comment saying a spec has no assembly is evidence to investigate, not authority when production code now exists.

### F1.2 Existing reference audit

Review every mockup for:

- task served;
- navigation assumptions;
- displayed data;
- implied controls/actions;
- owning seams;
- localization/a11y risks;
- duplicated primitives;
- status against the capability matrix.

Output columns:

`Reference | Keep | Revise | Retire | Capability gaps | Gate implications`

### F1.3 Player/task assumptions

Use three lightweight hypotheses:

- experienced management-sim player;
- football-literate newcomer;
- efficiency/power user.

No elaborate fictional personas.

### F1.4 PM-1/PM-2 task hierarchy

For each task:

- goal;
- frequency;
- consequence of failure;
- required information;
- required action;
- owning system;
- milestone priority.

Priority: `Critical`, `High`, `Medium`, `Low`.

### F1.5 Early Access success floor

A player must be able to:

- enter/start/resume the supported mode;
- know what needs attention next;
- prepare and start a match;
- read live match state;
- use supported match interventions;
- understand result/core statistics;
- return to season context;
- advance correctly;
- save/quit/resume according to the actual product promise;
- find release-critical settings/accessibility controls.

### F1 exit

No `UNKNOWN` capability may support an S0/S1 design premise. Contradictions become named findings, not assumptions.

---

## F2 — Current-vs-target architecture

### F2.1 Current PM-1 graph — record, do not redesign

`ClientScreenFlow` already owns the legal graph. Record it exactly and cite its five named moves.

The UX workstream may propose a future flow only by labelling the required new `ScreenId`/move/registration as future work. A mockup cannot add an edge.

### F2.2 Future career shell

Map target locations for:

- Home/Season;
- Squad;
- Tactics;
- Training;
- Scouting;
- Transfers;
- Club;
- World;
- settings/help;
- later inbox/news where relevant.

Every destination carries its capability/milestone label.

### F2.3 Interaction architecture

Define only what is not already owned by current navigation code:

- page vs modal vs drawer use;
- subnavigation/tabs;
- attention/badge/banner/toast rules;
- save/load transition blocking;
- contextual help/why-disabled pattern;
- current-vs-target mapping.

### F2 exit

Every S0/S1 task has a place in either the **current** graph or an explicitly **future** target graph. Nothing is ambiguously half-current.

---

## F3 — Shared S0/S1 interaction system

Audit/define only needed primitives:

- primary/secondary/destructive actions;
- nav/tab/focus states;
- dense tables, sort/filter/selection;
- inputs/selectors/toggles;
- tooltips/help;
- modals/confirmation;
- toast/banner/inline feedback;
- loading/empty/partial/stale/error states;
- keyboard traversal and no-trap rules;
- pseudo-locale expansion/reflow;
- maximum supported text scale;
- contrast/colorblind/color-independent semantics;
- font/glyph fallback assumptions;
- missing badge/portrait/stadium/key-art fallbacks;
- caption/HUD coexistence and muted-audio behavior;
- smallest/reference/high-resolution desktop behavior.

### F3 exit

Every shared primitive needed by S0 has defined state, focus, a11y/localization and fallback behavior. No speculative component library is required.

---

## F4 — Validation setup

### F4.1 Methods

Use four layers:

1. contract review;
2. scripted heuristic/self-walkthrough;
3. independent task-based participant test;
4. implementation verification.

### F4.2 Binding participant mechanism

For **S0 and S1 only**, Gate G requires:

- **2 independent participants** in one formative round;
- the designer/implementer does not count;
- where practical, one experienced management-sim player and one football-literate newcomer;
- participants may be recruited from the owner's/team's personal or community network; paid recruitment is optional, not assumed;
- F4 records the intended participants/recruiting channel before the journey reaches Gate F;
- the test round is scheduled for the first practical session after the prototype passes Gate F;
- if either participant is unavailable, Gate G simply does not pass — there is **no provisional bypass to Gate H/I**.

One round is the default cap. A second round is required only if:

- a Blocker/Major causes a material flow redesign; or
- the owner explicitly requests another validation pass.

This is intentionally small enough to bind in a solo/small-team project while still providing evidence independent of the author.

### F4.3 Scripted self-walkthrough

Before independent testing, the author executes the same tasks using:

- baseline data;
- long names/pseudo-locale;
- max text scale;
- keyboard only;
- missing art;
- disabled/error states;
- smallest supported desktop layout.

This is a binding Gate E input, but **never substitutes for Gate G**.

### F4.4 Severity

**Blocker** — critical task cannot be completed; inaccessible critical path; destructive ambiguity; false capability presented as live.

**Major** — repeated critical-info miss; frequent navigation failure; common action misunderstood; severe repeated friction; material localization/a11y failure.

**Moderate** — meaningful confusion/inefficiency with workaround.

**Minor** — polish/microcopy/low-impact consistency.

### F4.5 Disposition

S0/S1 Gate G cannot pass with:

- any unresolved Blocker;
- any unresolved Major unless the project owner explicitly accepts it with rationale and target disposition.

Moderate may be accepted if documented/non-compounding. Minor enters backlog.

### F4.6 Test-data profiles

Minimum profiles:

- ordinary case;
- long player/club/competition names;
- empty/large lists;
- many status indicators;
- pseudo-locale;
- alternate date/currency formatting;
- no save;
- save/load failure where relevant;
- no match frame yet;
- disabled action with reason;
- missing art;
- event-heavy match;
- unusual scoreline;
- full-time/frozen state;
- smallest desktop;
- 1920×1080;
- high-resolution/ultrawide behavior;
- max text scale;
- keyboard-only and mouse-only.

### F4 exit

The protocol is repeatable by someone other than its author, and two independent participants are identified/recruitable for S0.

---

# 5. Gates A–J — single authoritative definition

These gates apply to every journey. High-level documents refer here rather than duplicating them.

## Gate A — Contract/dependency truth

Before controls are designed:

- identify owning spec/system;
- verify read/data seam;
- verify action/command seam;
- verify UI adapter/projection status;
- verify screen/navigation status;
- verify rendering/host status;
- classify missing surfaces.

**Pass:** every proposed control/data element is backed by a verified owner or explicitly `FUTURE-BLOCKED`; no phantom action.

## Gate B — Task flow

Define:

- entry trigger;
- player goal;
- required information;
- actions/decisions;
- alternate/blocked paths;
- cancellation/back behavior;
- completion state;
- return destination.

**Pass:** flow is coherent without visual styling.

## Gate C — Low-fidelity information design

Define hierarchy, density, primary action, navigation, comparison, progressive disclosure, focus sequence and reflow expectations.

**Pass:** interaction can be evaluated without color, art or polish.

## Gate D — Full state matrix

Cover relevant default/focus/selected/disabled/loading/empty/partial/stale/error/progress/confirmation/success/failure states.

**Pass:** no happy-path-only design.

## Gate E — Cross-stream resilience

Execute/review with:

- pseudo-locale;
- max text scale;
- keyboard only;
- color-independent meaning;
- missing/fallback art;
- audio muted/caption path where relevant;
- smallest/reference/high-resolution layouts;
- representative data extremes.

**Pass:** Blocker/Major findings resolved or recorded before prototype validation.

## Gate F — Interactive prototype

Prototype the complete task including back/cancel and critical failure/disabled states.

The prototype is not required to be Unity. It must accurately label real versus future behavior.

**Pass:** a participant can attempt the complete task without instructions about which control to click.

## Gate G — Independent usability validation

Run the F4 participant round.

Record:

- version tested;
- participant profile;
- task completion;
- wrong turns/hesitation;
- missed information;
- misunderstood controls/states;
- confidence about what happens next;
- findings/severity/disposition/retest.

**Pass:** two independent participants completed the round; no unresolved Blocker; no unaccepted Major.

## Gate H — High-fidelity reference

Only after Gate G:

- apply `touchline`;
- finalize shared-component usage;
- show full states;
- apply art slots/fallbacks;
- finalize copy roles/localization keys as appropriate;
- clearly mark future behavior.

**Pass:** design-ready.

## Gate I — Implementation handoff

Provide:

- journey/screen-state map;
- component mapping;
- focus order;
- data/read mapping;
- action/command mapping;
- navigation mapping;
- localization roles;
- asset slots;
- a11y behavior;
- QA Given/When/Then cases;
- blockers/deferred items.

**Pass:** an implementer can build the supported scope without inventing product behavior.

## Gate J — Implementation verification

Against the real implementation:

- verify hierarchy/states/transitions;
- verify public command paths only;
- verify presentation logic remains outside gate-invisible `MonoBehaviour`s;
- verify focus/keyboard;
- pseudo-locale/max text scale;
- fallback art;
- supported resolutions;
- real data extremes;
- required Unity-host/cert behavior.

**Pass:** implementation matches the validated UX and all named host/roadmap dependencies are closed for that journey.

---

# 6. S0 — PM-1 journey: play one match end-to-end

**Priority:** P0  
**Current graph owner:** `src/client-app/ClientScreenFlow.cs`  
**Current screen identities:** Main Menu, Tactics Setup, Match View, Post-Match Report

## 6.1 Current dependency state

Known before F1:

- P4b Unity render/camera/click binding code is landed but host-unverified;
- P5b UGUI shell is open;
- on-host P6 is open;
- therefore the shipping Unity PM-1 client cannot currently be launched;
- host-free command/session/render-decision logic exists;
- #37 match analytics exists;
- `match-viewer` and `match-client-web` provide real match presentation/reference behavior host-free.

## 6.2 Roadmap coupling

S0 does **not** wait for all Unity work before design, but the dependencies are explicit:

- **S0 Gates A–E:** host-free; proceed from code/spec/reference evidence.
- **S0 Gate F:** use a design prototype for the full flow; use `match-viewer`/`match-client-web` and captured real outputs to validate Match View data/behavior. Do not extend the browser client into a second shipping implementation.
- **S0 Gate G:** validate the design prototype with two independent participants.
- **S0 Gates H–I:** produce the approved visual reference and the implementation packet that directly feeds **B9b/P5b**.
- **Roadmap B8/P4b:** its code is landed, but pinned-host compile/runtime/cert evidence is still required. Follow the roadmap/interactive-client ordering for the host landing.
- **Roadmap B9b/P5b:** consumes the S0 Gate-I packet for the four-screen UGUI binding.
- **Roadmap B10/on-host P6:** verifies scene boot, 60 FPS rendering, live tactical input and render-loop certification.
- **S0 Gate J:** cannot pass until the relevant B8 host verification, B9b and B10 evidence exists.

This lets UX unblock P5b without pretending the client already exists.

## 6.3 S0 Gate-A audit specifics

Verify:

- five legal `ClientScreenFlow` moves;
- tactics/substitution command seams;
- live command marshaling path;
- match frame/read surfaces;
- P5a playback/control-state decisions;
- #37 report inputs;
- current save/new-game limitations for this PM-1-only flow;
- #48/#49/#51 current availability;
- P4b host verification status;
- P5b implementation status.

Output: desired-control table with `LIVE`, `DESIGNABLE`, `FUTURE-BLOCKED`, decorative, or removed.

## 6.4 S0 Gate-B flow

Current production target flow remains:

`Main Menu → Tactics Setup → Match View → Post-Match Report → Main Menu`

plus the existing Tactics Setup cancel/back edge.

UX may not add an abandon-match/back edge unless the current owning design/spec is changed through its own process.

## 6.5 S0 Gate-C/D priorities

### Main Menu

- action hierarchy based on actual supported state;
- unavailable action reason;
- settings access only if real/allocated;
- no key-art dependency.

### Tactics Setup

- reconcile `Tactics.html` with actual command/setup capabilities;
- eliminate or label controls with no seam;
- make readiness/start/cancel state explicit.

### Match View

- score/clock/match state;
- pitch/agent/ball readability;
- real playback state semantics;
- supported tactical/substitution intervention;
- event/full-time feedback;
- no UI-derived analytics or domain causality.

### Post-Match Report

- score/result;
- #37-backed core stats;
- explanatory hierarchy without claiming causal analysis not exposed by #37;
- return action.

## 6.6 S0 prototype vehicle

Use two layers:

1. **Design prototype** under `docs/design/` for the complete four-context journey and all test states.
2. **Real-data/reference check** using existing `match-viewer` / `match-client-web` output or captured real run data.

Do not modify those reference harnesses merely to make the prototype easier. Their role is to prevent a synthetic Match View from drifting away from actual match output.

## 6.7 S0 usability task

Participant goal:

> Prepare the team, start the match, identify what is happening, make one supported intervention, understand the final result, and return successfully.

No step-by-step instruction.

## 6.8 S0 Gate-I handoff target

The implementation packet explicitly targets P5b and names:

- existing `ClientScreens`/`ClientScreenFlow` mapping;
- view-model source per screen;
- dispatcher per actionable screen;
- P5a state decisions already owned outside the binding;
- visual/component/focus/a11y/localization requirements;
- host-only wiring that must decide nothing.

## 6.9 S0 Gate-J host acceptance

Gate J requires evidence that:

- P4b binding compiles/runs on the pinned host and its existing cert obligations pass;
- P5b four-screen UGUI binding is present and follows `ClientScreenFlow` rather than recreating navigation logic;
- on-host P6 scene/live-input/performance checks pass;
- the implemented UX passes pseudo-locale/text-scale/focus/fallback/resolution checks;
- no domain or navigation decision was moved into `MonoBehaviour` to make the design work.

---

# 7. S1 — PM-2 / Early Access journey: run a season loop

**Priority:** P1 / Early Access core

Target journey:

`Launch → New/Continue/Load → Career Home/Season → inspect/prepare/advance → Match → Report → Career Home/Season → Save/Continue`

## 7.1 Known starting state

Do not begin with the false assumption that #30 is absent.

F1 must verify the current state, starting from:

- `src/season-save/SeasonLoop.cs`;
- league/bootstrap types in `src/season-save/`;
- season save manager/codecs;
- `AdvanceAndPlayNextRound` and day/fixture progression;
- any current #30/player-facing projection/adapters;
- `ui-framework` intent catalogue/dispatchers;
- `client-app` screen catalogue and graph.

The expected gap is **presentation composition**, not necessarily domain behavior.

## 7.2 S1 entry conditions

### Design-ahead entry

S1 Gates A–C may begin after:

- F1 has classified the required PM-2 capabilities;
- F2 has separated current versus future career navigation;
- S0 work remains the higher-priority blocker path.

A `FUTURE-BLOCKED` capability may appear in a low-fidelity future-flow design only when labelled.

### Validation/high-fidelity priority entry

S1 Gates D–I become priority work after S0 has passed Gate G or the project owner explicitly determines that parallel S1 work cannot delay an unresolved S0 Blocker/Major.

### Gate-I condition

Full S1 implementation handoff requires every EA-critical control to have either:

- a real owning seam plus a named UI implementation path; or
- an explicit dependency item/owner that is part of the implementation plan.

It may not silently treat a spec-only or missing screen surface as live.

## 7.3 S1 Gate-A audit

Classify separately:

- league table read model;
- fixture/calendar read model;
- next-fixture/progression state;
- advance command seam;
- UI dispatcher/intent for advance;
- generated new-game bootstrap;
- new-game configuration actually required for EA;
- save capture/load/resume domain support;
- player-facing save browser/continue selection;
- career Home/Season `ScreenId` and navigation;
- post-match return target;
- settings/a11y persistence;
- #37 season/statistic inputs used by the surface.

This prevents both errors: "#30 is missing" and "#30 exists, therefore the UI exists."

## 7.4 Product decisions that UX cannot invent

Escalate if unresolved:

- exact EA new-game options;
- autosave/manual-save promise;
- incompatible-save promise;
- whether Continue auto-selects or opens a picker;
- exact semantic of advance-to-next-day/event/match;
- mandatory blockers before advance.

UX designs the interaction after the product/domain decision; it does not decide the domain rule via a button label.

## 7.5 Career Home information hierarchy

The Home/Season surface should answer:

1. What changed?
2. What needs attention?
3. What is next?
4. What can I do before then?
5. How do I progress?

Candidate modules are admitted only when their data is `LIVE`/`DESIGNABLE` or explicitly future-labelled.

## 7.6 S1 usability tasks

Two participants attempt, at minimum:

- start or resume the supported career mode;
- identify next match and league position;
- reach preparation;
- progress correctly;
- complete a round through the match/report loop;
- understand the changed table/result;
- save/quit/resume according to the product promise;
- find settings/accessibility.

## 7.7 S1 acceptance

A player can complete repeated league rounds without developer knowledge of #30 internals and without guessing which action progresses time.

---

# 8. S2 — PM-3/deeper management journeys

S2 is never one mega-slice.

Each capability starts at Gate A and activates only when its owning system has a real implementation/read/action surface appropriate to the journey.

Candidate journeys:

- squad inspection/selection — #27 + real selection seam;
- training/progression — #28/#29 wired behavior/action;
- injuries/availability — #41 presentation surface;
- transfers/contracts — #31 implementation plus finance constraints;
- scouting — #32 implementation/read/action surface;
- staff/finances/board/world — owning assemblies/surfaces as they land.

A specification is enough for conceptual research but not an implementation handoff.

No S2 high-fidelity work may delay unresolved S0/S1 Early Access work.

## 8.1 Provisional implementation state — to be confirmed, not assumed, at F1.1

Recorded September 6, 2026 by symbol search over `src/`, **not** by folder naming — the #30 lesson in §2 applies to this table as much as to S1, and this is a starting hypothesis for F1.1 to verify, not a finding:

| Capability | Provisional state | Evidence |
|---|---|---|
| Squad inspection/selection (#27) | implementation present | `player-database`, `LineupSelector`, `PlayerAttributeProjection` |
| Training/progression (#28/#29) | implementation present (T0) | `src/training-system/`, `src/player-progression/` |
| Injuries/availability (#41) | implementation present (T0) | `src/injuries-medical/` |
| **Board objectives** | **partially present** | `BoardState`, `BoardObjective` in `src/season-save/` — another capability living under a differently-named assembly |
| Transfers/contracts (#31) | **implementation-absent** | no type matches; `HeadingSpinTransfer` and `GoalkeeperPositioningContract` are physics false positives |
| Scouting (#32) | **implementation-absent** | no matching type in `src/` |
| Finances / wages | **implementation-absent** | no matching type; `BudgetRollupEntry` is a `performance-optimization` perf budget, not club finance |
| Staff | **implementation-absent** | no matching type in `src/` |

The four marked implementation-absent are `FUTURE-BLOCKED` for UX purposes: design research is permitted, an implementation handoff is not. **The false positives are the point of recording the evidence column** — a folder-name or bare-keyword search would have reported transfers, contracts and budgets as present, and reported board objectives as absent. Every row is re-derived at F1.1 against the tree of the day.

---

# 9. Parallel-work matrix

| Stream | Can proceed before UX Gate I | UX handoff | UX must not assume |
|---|---|---|---|
| Backend/sim | yes | Gate A exposes missing/available seams | mockup creates domain behavior |
| Unity client | P4b host work and host-free client work continue | S0 Gate I feeds P5b | design screenshot is runtime authority |
| Art | pipeline/style/fallback work | Gates C/E slots; H final usage | final art exists |
| Localization | infrastructure/pseudo-locale | C/D string roles; E validation | English widths are fixed |
| Accessibility | settings/infrastructure | F3/E/J renderer expectations | ad-hoc per-screen application |
| Audio | infrastructure/cue language | E/H caption/feedback placement | audio carries essential state |
| QA | general infrastructure | D/F/I behavior cases | screenshot comparison is sufficient |

---

# 10. Ownership and effort

## 10.1 Role owners

- UX workstream: artifacts, flows, prototypes, findings, handoffs.
- Project owner: release cut, accepted Major findings, unresolved product decisions.
- Owning domain/client subsystem: truth of read/action seams.
- Unity client workstream: P5b implementation.
- Certification/Unity-host workstream: B8/B10 on-host verification.
- QA: implementation acceptance execution with UX cases.

## 10.2 Active UX effort bands

Assuming one primary UX contributor; excludes external waiting:

- F1: 3–4 working days (revised up from 1–2 at v1.2 — F1.1 spans `ui-framework`, `client-app`, `match-client-core`, `match-client-unity`, `season-save`, `match-analytics` and five spec surfaces, and the dependency review that produced v1.1 demonstrated that establishing what actually exists here is slower than it looks: two of its nine findings were wrong about implementation state);
- F2: 0.5–1 day;
- F3: 1–2 days;
- F4: 0.5–1 day;
- S0 A–E: 2–4 days;
- S0 F–G: 2–4 days;
- S0 H–I: 2–3 days;
- S0 J: external implementation/host dependency;
- S1 A–E: 3–5 days;
- S1 F–G: 2–4 days;
- S1 H–I: 2–4 days;
- S1 J: implementation-dependent.

These are planning estimates, not release commitments.

---

# 11. QA handoff

For every journey, QA receives behavior cases using:

- **Given:** starting state/data/resolution/input mode;
- **When:** player attempts task/action;
- **Then:** visible state, navigation, focus, feedback and command/read class;
- **And:** forbidden/phantom behavior does not occur.

Required classes:

- happy path;
- invalid/disabled;
- loading/empty/error;
- keyboard;
- pseudo-locale/max text scale;
- missing-art fallback;
- smallest supported layout;
- relevant real-data extreme.

Visual regression may supplement, never replace, behavior checks.

---

# 12. UX finding ledger

Record:

- journey;
- gate;
- severity;
- evidence;
- owner;
- disposition;
- release condition;
- retest result.

Disposition:

- `FIX NOW`;
- `ACCEPT FOR CURRENT GATE`;
- `DEFER TO P2/P3`;
- `BLOCKED BY DOMAIN/CLIENT IMPLEMENTATION`;
- `INVALID / NOT REPRODUCED`.

A deferred Major must name a concrete milestone/condition; "later polish" is invalid.

---

# 13. Change control after handoff

After Gate H/I:

- usability/accessibility defect fixes reopen the relevant gate;
- cosmetic preference changes normally defer;
- navigation/action changes return to A/B;
- new domain mutation needs return to Gate A and the owning system;
- journey packet version records the change.

---

# 14. Exact first sequence after F0 closes

No additional polished screen comes next.

1. **F1.1 — current capability matrix.** First explicitly resolve the P4b/P5b/P6 host/client state and #30 `season-save` state that triggered this review.
2. **F1.2 — mockup reconciliation.** Begin with `Tactics.html` and provisional `Main Menu.html`.
3. **F1.4 — PM-1/PM-2 task hierarchy and EA priority.**
4. **F2 — record the existing `ClientScreenFlow`; produce separate future career-shell map.**
5. **F3 — audit only S0-required component/state/a11y/localization/fallback primitives.**
6. **F4 — write scripts, severity ledger and participant mechanism; identify two S0 testers.**
7. **S0 Gate A — full PM-1 dependency/control audit.**
8. **S0 Gate B — complete task flow.**
9. **S0 Gate C — low-fidelity wireframes.**
10. Continue through D–G; no high fidelity before Gate G passes.

The existing Main Menu visual is revisited at Gate H unless earlier low-fidelity findings show it should be retired.

---

# 15. Definition of implementation-ready

A journey is implementation-ready only at Gate I.

For S0 specifically, Gate I means **ready for P5b implementation**, not "PM-1 shipped." Shipping/acceptance still requires Gate J and the named B8/B9b/B10 evidence.

For S1, Gate I cannot contain an unlabeled future control. Every EA-critical dependency has a real surface or a named owner/implementation dependency.

---

# 16. Final readiness critique

After the external dependency review, the plan now has no remaining structural escape hatch:

- current client navigation is inherited, not re-designed;
- landed-code versus host-verification state is explicit;
- S0 has a named consumer and cert path;
- #30 status is measured correctly rather than inferred from a folder name;
- S1 can design ahead without pretending future UI surfaces are live;
- Gate G requires two real independent participants and cannot be bypassed into H/I;
- host-free prototype validation uses the existing real-match reference surfaces without turning them into a second shipping client;
- ownership and effort are bounded;
- gates are defined once;
- F1 remains the correct first substantive action.

That prerequisite — repository tracking/discoverability close-out under F0 — **landed September 6, 2026** (`CHANGELOG.md`, `open-issues.md` 21 → 22 active, `file-manifest.md`, `project-reference.md`), so **F1 may begin now**.

Three things are deliberately **not** F1 gates, having been mis-stated as such in the close-out's first draft and corrected at v1.2:

- **PR draft status.** #362 remaining draft is a review-state fact, not a technical dependency. F1 is evidence-gathering against the tree and needs no acceptance decision to proceed.
- **B8 → B9b → B10 host ordering.** It is the recommended implementation sequence and a real dependency for S0 Gates I/J, but it constrains nothing in the F1 capability audit.
- **Gate-G tester recruitment.** §4 already places participant identification in F4, before usability testing. The two-participant requirement stays binding at Gate G; who the participants are is settled at F4, not now.

---

## Version History

| Version | Date | Change |
|---|---|---|
| 1.0 | September 4, 2026 | Detailed plan after three internal critique/revision rounds. |
| 1.1 | September 4, 2026 | External dependency-review revision: corrected P4b/#30 state; inherited current client-plan authority; tied S0 Gate I to P5b and Gate J to B8/B9b/B10; made Gate G binding at two independent participants; added real-data prototype strategy, role ownership, effort bands, S1 design-ahead conditions, and tracking close-out as the only prerequisite to F1. |
| 1.2 | September 6, 2026 | F0 closed and F1 unblocked. F1's effort band revised 1–2 → **3–4 working days**. New §8.1 records the provisional S2 implementation state by symbol search with its evidence column, including two findings a folder-name search would have got backwards: board objectives are **present** in `src/season-save/`, while transfers, scouting, finances and staff are implementation-absent. Reaffirms that the tracking close-out was F1's only prerequisite — PR draft status, B8/B9b/B10 host ordering and Gate-G tester recruitment (which §4 already places in F4) are **not** F1 gates. |