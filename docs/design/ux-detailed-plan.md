# System XI — Detailed UX Implementation Plan

**Created:** September 4, 2026  
**Status:** PLAN — READY FOR IMPLEMENTATION AFTER OWNER REVIEW  
**Parent:** `docs/design/ux-high-level-plan.md`  
**Scope:** Planning, validation, design production, handoff, and implementation verification for the player-facing UX  
**Release target:** Early Access centered on the PM-2 playable-season loop, with PM-1 as prerequisite and PM-3/deeper management gated behind it

---

# 0. Purpose and operating rule

This document converts the converged high-level UX plan into concrete work packages, artifacts, gates, dependencies, acceptance criteria, and execution order.

It does **not** redefine domain behavior. The authoritative order remains:

1. APPROVED specification;
2. production code/current behavior;
3. explicitly labeled future UX intent;
4. visual reference/mockup.

A mockup can expose a missing requirement. It cannot silently create one.

**Implementation freeze:** no additional high-fidelity screen should be treated as implementation-ready until its journey slice reaches **Gate I — Implementation Handoff** below.

The existing `docs/design/ui-mockups/Main Menu.html` remains provisional pre-plan work until the PM-1 journey passes the same gates.

---

# 1. Detailed-plan convergence record

## 1.1 Detailed draft v0.1

The first detailed draft expanded each high-level stage into separate documents and then scheduled screen-by-screen work:

- inventory document;
- persona document;
- task document;
- navigation document;
- component document;
- a11y document;
- validation document;
- Main Menu slice;
- Tactics slice;
- Match slice;
- Report slice;
- PM-2 screens individually.

### Critique v0.1

Rejected as too bureaucratic and internally inconsistent with the task-first high-level plan.

Problems:

- The plan created too many documents whose synchronization would become work of its own.
- PM-1 had again been decomposed into individual screens rather than the actual task: **play one match end-to-end**.
- It did not define realistic test-data extremes.
- It did not define severity/disposition rules for usability findings.
- It lacked a lean validation method appropriate to a small project.
- It did not define how existing mockups are reconciled against real contracts.
- It did not expose which work can proceed in parallel with art/audio/localization/backend work.

**Revision:** replace document proliferation with a few durable evidence packs; make a journey the unit of work.

---

## 1.2 Detailed draft v0.2

Revised structure:

- one UX baseline/evidence pack;
- one shared-system pack;
- one validation protocol;
- one design packet per journey;
- PM-1 journey = launch → prepare → match → report;
- PM-2 journey = start/continue career → understand season → advance → play → return;
- later management journeys gated by system readiness.

### Critique v0.2

Structurally strong, but not yet implementation-ready.

Remaining gaps:

- no explicit severity threshold for passing usability review;
- no contract for test-data extremes;
- no parallel-work matrix;
- no design-change protocol after handoff;
- no branch/versioning convention for UX references;
- no explicit “blocked by backend vs design-only future” labels;
- no definition of what QA receives from UX;
- no exact first executable task after planning.

These are resolved in the final plan below.

---

## 1.3 Detailed draft v0.3 — final critique

The revised final structure was checked against:

- UI / Client Framework #38's one-way presentation contract and no-phantom-seam rule;
- the current four-screen `client-app` graph;
- the PM-1/PM-2/PM-3 roadmap;
- the existing `touchline` design system and management mockups;
- #49's pseudo-locale, text-scale, contrast, colorblind, font-fallback, and renderer-application requirements;
- #48/#51 presentation/audio composition boundaries;
- the requirement that Unity-host-only rendering not become the source of domain behavior.

No structural blocker remains. The remaining unknowns—specific usability findings, exact final copy, final art, and later management-system readiness—are inputs the plan is designed to discover, not missing pieces of the plan itself.

---

# 2. Deliverable model

To avoid documentation sprawl, UX work uses **four durable artifact types**.

## UX-A — Baseline & Evidence Pack

One evolving file/set covering:

- player/task assumptions;
- task hierarchy and priority;
- existing implementation inventory;
- existing mockup inventory;
- current-vs-target navigation map;
- reference/competitor observations;
- constraints;
- release-scope matrix;
- open UX decisions.

## UX-B — Shared System Pack

One evolving file/set covering:

- navigation rules;
- interaction patterns;
- component/state matrix;
- typography/density/layout rules;
- input/focus behavior;
- accessibility rules;
- localization/reflow rules;
- icon rules;
- art/audio/caption slot rules;
- desktop resolution behavior.

## UX-C — Validation Protocol

One stable protocol covering:

- heuristic review;
- task-based prototype testing;
- participant criteria;
- finding severity;
- pass/fail rules;
- pseudo-locale/a11y checks;
- test-data profiles;
- design verification after Unity implementation.

## UX-D — Journey Design Packet

One packet per journey slice containing:

- contract/dependency audit;
- current and target flow;
- low-fidelity wireframes;
- state matrix;
- prototype;
- usability findings/dispositions;
- high-fidelity references;
- component mapping;
- localization/copy roles;
- art/audio slots;
- implementation acceptance criteria;
- blocked/deferred items;
- version/change history.

These may be implemented as Markdown + HTML prototypes/mockups rather than forcing a new file for every subsection.

---

# 3. Work package F0 — Planning freeze and repository cleanup

**Objective:** establish planning authority before more design production.

## F0.1 Mark the current relationship between documents

- `ux-high-level-plan.md` = strategic UX authority.
- `ux-detailed-plan.md` = execution authority.
- `ux-foundation.md` = earlier direction memo; retain as historical/reference material or revise later to point to the plans.
- `ui-mockups/` = visual references, not runtime contracts.

## F0.2 Mark existing Main Menu mockup provisional

Do not delete it. Record that it was created before the plan and must pass the PM-1 journey gates before implementation handoff.

## F0.3 Stop new high-fidelity screen production

Allowed during freeze:

- audit;
- inventory;
- research;
- flow diagrams;
- low-fidelity wireframes;
- test protocol work;
- dependency analysis.

Not allowed as “approved UX” during freeze:

- new production-ready screen mockups;
- final visual polish;
- UI-side invention of missing backend actions.

### F0 exit gate

- high-level plan exists;
- detailed plan exists;
- first journey is named;
- branch/PR states that substantial UX implementation is paused pending plan acceptance.

---

# 4. Work package F1 — Baseline & Evidence Pack

**Objective:** establish the factual starting point and task model.

## F1.1 Inventory current UX implementation

Audit at minimum:

- `src/ui-framework/`;
- `src/client-app/`;
- `src/match-client-core/`;
- `src/match-client-unity/`;
- relevant web-client precedents;
- #30/#37/#38/#48/#49/#51 public presentation seams;
- save/load surfaces relevant to the release target.

Record:

- existing screen identities;
- existing navigation edges;
- existing read surfaces;
- existing command seams;
- currently missing but required seams;
- Unity-host-gated behavior;
- future-only intended surfaces.

### Output
A **current capability matrix** with labels:

- `LIVE` — production behavior exists;
- `DESIGNABLE` — read/action seam exists but screen does not;
- `FUTURE-BLOCKED` — UX can be explored but implementation requires an owning-system change;
- `OUT-OF-EA` — deliberately outside the Early Access cut;
- `UNKNOWN` — requires decision/investigation.

## F1.2 Audit existing UX references

Review every existing mockup for:

- task served;
- navigation assumptions;
- controls shown;
- data shown;
- action seams implied;
- stale branding;
- desktop-layout assumptions;
- localization hazards;
- accessibility hazards;
- duplicated component patterns;
- visual-only controls with no current/future owner.

Do **not** treat “looks good” as approval.

### Output
Mockup reconciliation table:

| Reference | Keep | Revise | Retire | Contract gaps | Notes |
|---|---|---|---|---|---|

## F1.3 Define target player assumptions

Use a small number of explicit assumptions rather than fictional detailed personas.

Recommended initial archetypes:

1. **Experienced football-management player** — expects dense information and fast navigation.
2. **Football-literate newcomer to management sims** — understands football terms but not interface conventions.
3. **Efficiency/power user** — heavy keyboard use, repeated high-frequency actions, values comparison speed.

These are hypotheses to test, not immutable personas.

## F1.4 Build task hierarchy

For each task record:

- player goal;
- frequency;
- consequence of failure;
- required information;
- required action;
- owning backend system;
- milestone priority.

### Priority formula
Use qualitative ranking rather than fabricated numeric precision:

- **Critical:** cannot complete PM-1/PM-2 without it.
- **High:** frequent or high-consequence Early Access task.
- **Medium:** useful management depth but not loop-blocking.
- **Low:** polish or rare workflow.

## F1.5 Reference/competitor benchmark

Benchmark comparable management/simulation interfaces for specific questions, not imitation:

- global navigation;
- squad/table density;
- match-preparation flow;
- match-day information hierarchy;
- post-match explanation;
- save/continue/new-game clarity;
- keyboard efficiency;
- onboarding/help;
- handling of unavailable systems.

Capture principles and failure patterns; do not copy protected visual assets or distinctive trade dress.

## F1.6 Early Access success criteria

At minimum, a new player should be able to:

- start or continue the available game mode;
- understand the next required action;
- prepare for a match;
- begin the match without hidden mandatory steps;
- read the live match state;
- make supported tactical/substitution changes;
- understand the result and core statistics;
- return to the career/season context;
- progress to the next fixture;
- save/quit/resume according to the release's actual save promise;
- discover settings/accessibility controls.

### F1 exit gate

No unresolved contradiction between the task hierarchy, Early Access cut, current code, and authoritative specs.

---

# 5. Work package F2 — Experience architecture

**Objective:** lock the shell before individual journey styling.

## F2.1 Launch architecture

Define behavior/placement for:

- Continue;
- New Game;
- Load Game;
- Settings;
- Credits;
- Exit.

For each, define:

- enabled criteria;
- empty/no-save state;
- progress state;
- failure state;
- confirmation behavior;
- return/back behavior.

## F2.2 Career shell architecture

Target information architecture:

- Home / Season;
- Squad;
- Tactics;
- Training;
- Scouting;
- Transfers;
- Club;
- World;
- later Inbox/News where appropriate.

Each destination receives a milestone/dependency label. The existence of a nav label does not imply implementation readiness.

## F2.3 Navigation rules

Define once:

- top-level navigation;
- sub-navigation;
- tab use;
- breadcrumb use;
- back behavior;
- modal use;
- drawer/side-panel use;
- deep-link/command-palette behavior;
- what state is preserved when moving between screens;
- when navigation is blocked during save/load/live transitions.

## F2.4 Attention model

Define how the player learns “something needs action” without turning the interface into notification noise.

Classes:

- blocking action required;
- important but non-blocking;
- informational change;
- background completion;
- error/failure.

Define badge/banner/toast/modal use for each class.

## F2.5 Onboarding/help architecture

Early Access minimum should favor contextual explanation over a long forced tutorial.

Define:

- first-run orientation;
- glossary/football-rule help where genuinely needed;
- tooltip standard;
- empty-state teaching;
- “why disabled?” explanation;
- optional contextual hints;
- how hints can be dismissed/disabled.

### F2 exit gate

Every P0/P1 journey has an unambiguous place in the architecture and back-navigation can be described without referring to a mockup screenshot.

---

# 6. Work package F3 — Shared UX system audit

**Objective:** turn the existing visual system into a complete interaction system.

## F3.1 Design-token audit

Retain `touchline` unless evidence gives a reason to reopen it.

Audit:

- semantic color tokens;
- text colors/contrast;
- surface levels;
- typography sizes/weights;
- spacing scale;
- radii;
- data-viz palette;
- status colors;
- selected/focus states.

## F3.2 Density tiers

Define at least:

- standard dense desktop;
- compact dense desktop if supported;
- enlarged-text/reflow mode.

Avoid arbitrary per-screen density changes.

## F3.3 Core component/state matrix

For each component used by P0/P1, define relevant states.

### Actions

- primary button;
- secondary button;
- destructive button;
- icon button;
- menu item;
- context action.

### Navigation

- global nav item;
- subnav/tab;
- breadcrumb;
- command-palette result.

### Data

- table;
- sortable header;
- filter;
- row selection;
- stat tile;
- badge/chip;
- progress/form indicator;
- chart/legend.

### Input

- text input;
- numeric input where required;
- select/dropdown;
- segmented selector;
- toggle;
- slider only where semantically appropriate.

### Feedback

- tooltip;
- modal;
- confirmation;
- toast;
- banner;
- inline validation;
- loading indicator;
- empty state;
- partial/stale state;
- blocking error.

## F3.4 Keyboard and focus model

Define:

- visible focus indicator;
- logical tab order;
- arrow-key patterns for lists/tabs where expected;
- Enter/Space activation;
- Escape behavior;
- command palette shortcut;
- whether high-frequency actions get dedicated shortcuts;
- prevention of keyboard traps.

Mouse remains fully supported; shortcuts are accelerators.

## F3.5 Localization/reflow rules

Design for:

- pseudo-locale expansion;
- longer button labels;
- variable date/currency formatting;
- player/club names longer than English mock data;
- font fallback/glyph coverage;
- no concatenated sentence fragments where localization owns the string;
- no meaning stored only in word order assumptions.

## F3.6 Accessibility rules

Embed #49's application boundary into rendering design:

- text scale;
- reflow;
- contrast mode;
- colorblind-safe palette;
- input assist where applicable;
- captions/subtitles where audio content is meaningful;
- no color-only communication;
- adequate focus visibility;
- motion reduction for optional UI animation where relevant.

## F3.7 Art-independent layout rules

For each asset family, define:

- aspect ratio;
- crop/safe area;
- min/max display size;
- placeholder/fallback;
- whether it is decorative or informational;
- behavior when missing.

Core comprehension must survive:

- no portrait;
- no badge;
- no stadium image;
- no competition art;
- no key art.

## F3.8 Audio/caption integration

UX owns placement/behavior, not audio logic.

Define:

- UI feedback cue opportunities;
- muted state behavior;
- caption location and collision rules;
- match commentary/caption coexistence with HUD;
- volume/settings entry points;
- no essential information available only through audio.

### F3 exit gate

All shared primitives required by the first journey have defined state, focus, localization, accessibility, and fallback behavior.

---

# 7. Work package F4 — Validation protocol

**Objective:** define what evidence is sufficient before visual lock and before Unity acceptance.

## F4.1 Review methods

Use four layers:

1. **Contract review** — no phantom data/action/navigation semantics.
2. **Heuristic review** — hierarchy, consistency, error prevention, visibility, efficiency.
3. **Task-based usability testing** — independent people attempt the actual journey.
4. **Implementation verification** — Unity build matches the validated design under real constraints.

## F4.2 Participant strategy

For P0/P1 critical flows:

- target **3–5 independent representative participants per formative round**;
- include at least one experienced management-sim player and one football-literate newcomer where possible;
- do not count the designer/implementer as an independent participant;
- if independent participants are temporarily unavailable, the slice may continue as **provisional**, but it does not receive full usability validation status.

Testing is iterative; five people once is less useful than small rounds with fixes between them.

## F4.3 Task-test format

Give a goal, not step-by-step instructions.

Example:

> “You are about to play your next league match. Set up the team the way you want, start the match, make one tactical change, and then tell me what you think caused the result.”

Capture:

- success/failure;
- completion path;
- wrong turns;
- hesitation points;
- questions asked;
- misunderstood labels;
- ignored information;
- confidence at completion.

## F4.4 Finding severity

### Blocker

- cannot complete critical task;
- data/action meaning is dangerously wrong;
- inaccessible critical path;
- destructive behavior is ambiguous;
- design requires a nonexistent backend capability but presents it as real.

### Major

- frequent navigation failure;
- critical information repeatedly missed;
- common action consistently misunderstood;
- high-friction repeated workflow;
- localization/a11y failure affecting normal use.

### Moderate

- noticeable inefficiency/confusion with workaround;
- secondary information hierarchy problem;
- lower-frequency state poorly explained.

### Minor

- polish, consistency, microcopy, cosmetic issue with low functional impact.

## F4.5 Gate disposition rule

A critical P0/P1 journey cannot pass usability Gate G with:

- any unresolved **Blocker**;
- any unresolved **Major** unless explicitly accepted by the owner with rationale and planned disposition.

Moderate findings may be accepted if documented and non-compounding. Minor findings enter polish backlog.

## F4.6 Test-data profiles

Every critical journey should be checked with relevant profiles, not only the ideal mock data.

### Baseline

- ordinary names;
- normal table size;
- ordinary fixture/result;
- complete data.

### Content extremes

- long player names;
- long club/competition names;
- zero/empty list;
- very large list within supported domain range;
- many status badges;
- long localized copy/pseudo-locale;
- dates/currencies with expanded formatting.

### State extremes

- no save exists;
- save/load failure;
- no published live match frame yet;
- disabled action with domain reason;
- missing art;
- partial/stale data if possible;
- match with many events/cards/substitutions;
- unusual scoreline;
- full-time/frozen state;
- unavailable future feature.

### Display/input extremes

- smallest supported desktop resolution;
- 1920×1080 reference;
- higher-resolution/ultrawide behavior;
- max supported text scale;
- pseudo-locale;
- keyboard-only;
- mouse-only;
- reduced motion if implemented.

## F4.7 Acceptance evidence

Each journey packet records:

- test date/version;
- prototype/build tested;
- participants/test mode;
- findings;
- severity;
- disposition;
- retest result.

### F4 exit gate

Validation can be repeated by someone other than the original designer from the written protocol.

---

# 8. Journey Slice S0 — PM-1: Play one match end-to-end

**Priority:** P0 / first executable UX slice  
**Current code anchor:** `ClientScreens.MainMenu`, `TacticsSetup`, `MatchView`, `PostMatchReport` and `ClientScreenFlow`  
**Goal:** a player can launch the current client, prepare a match, watch/influence it, understand the result, and exit the loop without confusion.

This is one journey containing four screen contexts, not four independent projects.

## S0-A — Contract/dependency audit

Verify:

- current `ClientScreenFlow` transitions;
- tactics command seams actually available;
- live match read/frame source;
- live command marshaling behavior;
- post-match #37 analytics available;
- current limitations around team selection/new game/save state;
- #48 presentation inputs currently available or absent;
- #49 string/a11y rendering path status;
- #51 audio/caption status.

### Output
Journey capability table with each desired control marked:

- current;
- future-blocked;
- visual-only/decorative;
- removed from EA flow.

### Gate A
No unlabeled phantom action.

## S0-B — Task flow

Define exact current and target flow:

`Launch → Main Menu → Tactics Setup → Start Match → Live Match → Full Time → Post-Match Report → Main Menu`

For each transition define:

- trigger;
- confirmation;
- loading/progress state;
- failure path;
- keyboard focus destination;
- back/cancel behavior.

### Gate B
Flow works on paper without visual styling.

## S0-C — Low-fidelity wireframes

Create low-fidelity references for the full journey.

### Main Menu

Focus:

- supported action hierarchy;
- unavailable continue/load state;
- settings access;
- no reliance on final key art.

### Tactics Setup

Reconcile existing `Tactics.html` against actual PM-1 setup requirements.

Audit every visible control for an owning seam.

Focus:

- selected XI/formation visibility;
- team/player instruction editing actually supported;
- readiness/start-match action;
- cancel path;
- validation if setup cannot start.

### Match View

Focus:

- score/time/state;
- pitch readability;
- selected/focused player state if needed;
- playback speed/pause semantics vs actual supported behavior;
- supported tactical/substitution interaction;
- event/state feedback;
- transition to full time;
- no data invented in UI.

### Post-Match Report

Focus:

- score/result;
- key statistics from #37;
- enough explanation to understand the result without pretending the UI knows causality the analytics do not expose;
- return action.

### Gate C
Hierarchy and interaction can be evaluated without colors/graphics.

## S0-D — State matrix

At minimum:

- Main Menu: no save / save available if current build supports it / unavailable action reason.
- Tactics: valid / invalid setup / selection changed / unsupported control hidden or disabled appropriately.
- Match: no frame yet / live / paused if applicable / tactical change pending/applied feedback / full time.
- Report: analytics available / analytics failure or unavailable handling defined.

### Gate D
No happy-path-only design.

## S0-E — Cross-stream review

Check entire journey with:

- pseudo-locale;
- max text scale;
- keyboard only;
- missing art;
- color-independent state;
- no audio;
- caption/HUD collision assumptions;
- smallest supported desktop layout.

### Gate E
Pass or record findings before prototype.

## S0-F — Interactive prototype

Prototype the entire flow, including:

- back/cancel;
- transitions;
- disabled/error states;
- at least one tactical change;
- post-match return.

The prototype may use representative data, but every action must be labeled according to real/future status.

### Gate F
The complete task is testable without Unity implementation changes.

## S0-G — Usability round

Primary task:

- prepare and start match;
- identify live state;
- make a supported intervention;
- understand final result;
- return successfully.

Apply F4 severity/disposition rules.

### Gate G
No unresolved Blocker; no unresolved Major without owner acceptance.

## S0-H — High-fidelity reference

Only now:

- reconcile/update provisional Main Menu;
- finalize Tactics reference;
- create Match View reference;
- create Post-Match reference;
- show complete states, not only hero screenshots.

### Gate H
Design-ready.

## S0-I — Implementation packet

Provide Unity/UI implementer:

- screen-state diagrams;
- component mapping;
- focus order;
- transition behavior;
- data source mapping;
- action seam mapping;
- art slots;
- localization roles;
- a11y behavior;
- test-data cases;
- acceptance scenarios.

### Gate I
Implementation-ready.

## S0-J — Unity verification

After implementation:

- verify visual hierarchy;
- verify state behavior;
- verify only existing public command seams are used;
- verify no logic moved into `MonoBehaviour` to satisfy presentation behavior;
- verify keyboard/focus;
- verify pseudo-locale/max text scale;
- verify missing-art fallback;
- verify supported resolutions;
- certify host-only binding behavior.

### Gate J
PM-1 UX slice accepted.

---

# 9. Journey Slice S1 — PM-2 / Early Access: Run a season loop

**Priority:** P1 / Early Access core  
**Goal:** start or resume a career, understand the season context, progress to the next match, play it, understand the result, and continue.

Target journey:

`Launch → New/Continue/Load → Career Home/Season → inspect fixture/table as needed → advance/prepare → match → report → Career Home/Season → save/continue`

## S1-A — Contract/dependency audit

Audit:

- #30 season loop APIs/view models;
- league table/calendar surface;
- round advance seam;
- `LeagueBootstrap`/current generated-league capability;
- save/resume behavior actually promised for EA;
- new-game capability available without #47 editor;
- current screen registration/navigation gaps;
- #37 season/statistics inputs relevant to Home/World;
- localization/a11y/settings store status.

Any new navigation identities or commands are **future target** until their owning implementation lands.

## S1-B — Product/task decisions

Settle before high fidelity:

- what “New Game” config exists in EA;
- what “Continue” chooses when multiple saves exist;
- autosave/manual-save policy from the player's perspective;
- whether advancing progresses one day, to next event, or next match in the EA UX, consistent with #30 behavior;
- what requires acknowledgement before advance;
- what the Career Home must show to prevent blind progression.

If a choice changes domain behavior, it is escalated outside UX rather than decided by the mockup.

## S1-C — Low-fidelity journey

### Launch/save layer

Design:

- Continue;
- New Game;
- Load;
- save selection metadata;
- save failure and incompatible/broken state as supported by the actual save policy.

### New Game minimum

Design only the setup actually needed for generated Early Access play. Do not recreate the deferred #47 database editor.

### Career Home / Season hub

Primary questions:

1. What happened?
2. What needs my attention?
3. Who/what is next?
4. What can I do before then?
5. How do I progress?

Likely core modules:

- next fixture;
- current league position;
- latest result;
- immediate blockers/actions;
- advance/continue control;
- compact recent/upcoming calendar;
- relevant squad/tactical warning only if backed by real data.

### Competition context

Design table/fixture drill-down without forcing the player into a separate “World” screen for every common question.

## S1-D through S1-J

Run the same state → cross-stream → prototype → usability → high-fidelity → handoff → Unity-validation cycle as S0.

### S1 usability tasks

At minimum:

- start a generated career;
- identify next match and current league position;
- navigate to relevant preparation;
- advance correctly;
- play/resolve through the loop;
- understand updated table/result;
- save/quit/resume according to product promise;
- locate settings/accessibility.

### S1 acceptance

A player can complete multiple league rounds without needing developer knowledge of #30's internal API or guessing which screen advances time.

---

# 10. Journey Slice S2 — PM-3: Manage and improve the squad

**Priority:** P2  
**Activation:** only after S1's core loop is validated and the owning systems' implementation seams are ready.

This is not one simultaneous mega-slice. It is a family of capability-gated journeys.

## S2.1 Squad inspection and selection

Inputs:

- #27 roster/player data;
- actual selection seam.

Tasks:

- understand available squad;
- compare players;
- understand role/position status;
- select/adjust lineup as supported;
- inspect player detail.

## S2.2 Training/progression

Activation gate:

- #29/#28 behavior and action seam actually implemented/wired.

Tasks:

- understand development state;
- choose supported focus;
- understand expected vs actual outcome without false certainty.

## S2.3 Injuries/availability

Activation gate:

- #41 presentation surface available.

Tasks:

- see unavailable/limited players;
- understand reason/duration where exposed;
- make lineup decisions accordingly.

## S2.4 Transfers/contracts

Activation gate:

- #31 action seam and relevant finance constraints implemented.

Tasks:

- search/browse candidates as supported;
- understand knowledge uncertainty from #32;
- submit/adjust offers through real seams;
- understand budget/contract consequence.

## S2.5 Scouting

Activation gate:

- #32 read/action surface exists.

Tasks:

- distinguish known vs unknown information;
- compare candidates without presenting fogged values as truth;
- issue supported scouting action.

## S2.6 Club / staff / finances / board / world

Each becomes its own journey packet when the relevant owning systems are ready. Existing mockups are starting references only.

### S2 rule

No S2 high-fidelity work is allowed to delay unresolved S0/S1 usability or implementation work for Early Access.

---

# 11. Cross-stream parallelization matrix

| Stream | Can proceed during UX planning | Handoff point from UX | What UX must not assume |
|---|---|---|---|
| Backend/simulation | continue independently | Gate A dependency map identifies needed existing seams | UX cannot invent domain actions |
| Unity UI implementation | framework/core work may continue | Gate I per journey | high-fidelity mockup is not a runtime contract before Gate I |
| Art | style exploration, asset pipeline, fallback assets | Gate C/E defines slots; Gate H finalizes usage | final art availability |
| Localization | infrastructure, pseudo-locale, catalogue rules | Gate C/D defines string roles; Gate E validates expansion | English dimensions are stable |
| Accessibility | option/infrastructure work | F3 + Gate E/J define renderer behavior | per-screen ad hoc application |
| Audio | bus/cue infrastructure, general UI audio language | Gate E/H defines presentation opportunities/caption placement | audio is required to understand state |
| QA | general test infrastructure | Gate D/F/I provide state + journey acceptance cases | screenshots alone are sufficient tests |
| Analytics/telemetry | event design may proceed if desired | pre-EA feedback plan defines justified UX events | telemetry replaces usability testing |

---

# 12. Branching, versioning, and review convention

## 12.1 Planning branch

Current planning work remains on `design/ux-foundation` until the plan itself is accepted.

## 12.2 Journey branches

Recommended convention after acceptance:

- `design/ux-s0-pm1-loop`
- `design/ux-s1-pm2-season-loop`
- capability-specific S2 branches later.

Avoid a long-lived branch containing every future screen.

## 12.3 Reference versioning

Each approved journey packet/reference records:

- version;
- date;
- gate reached;
- significant behavior/layout change;
- blocked/future items.

After Gate H/I, a significant navigation/action change increments the journey reference and re-enters the appropriate earlier validation gate.

---

# 13. UX → QA handoff

For each journey, QA receives behavior-oriented cases, not just screenshot comparison.

Minimum acceptance case template:

- **Given:** starting state/data profile/resolution/input mode;
- **When:** player attempts task/action;
- **Then:** expected visible state, navigation result, focus result, feedback, and underlying command/read interaction class;
- **And:** no forbidden/phantom behavior occurs.

Required classes:

- happy path;
- invalid/disabled path;
- loading/empty/error path;
- keyboard path;
- pseudo-locale/max text scale;
- missing-art fallback;
- smallest supported desktop layout;
- real-data extreme relevant to the screen.

Visual regression can supplement these cases but does not replace behavior checks.

---

# 14. UX debt and finding backlog

Use one UX finding ledger or issue label set with:

- journey;
- severity;
- evidence/source;
- current gate;
- disposition;
- owner;
- target milestone.

Disposition values:

- FIX NOW;
- ACCEPT FOR CURRENT GATE;
- DEFER TO P2/P3;
- BLOCKED BY DOMAIN/IMPLEMENTATION;
- INVALID / NOT REPRODUCED.

A deferred finding must name the milestone/condition that releases it; “later polish” is not sufficient for a Major issue.

---

# 15. First executable sequence after plan approval

Substantial UX implementation should **not** resume with another high-fidelity screen.

The next work is:

1. **F1.1 — current capability matrix** for `ui-framework`, `client-app`, match client, #37, #38, #48, #49, #51, and the save/season surfaces needed by S0/S1.
2. **F1.2 — existing mockup reconciliation**, especially `Tactics.html` and the provisional `Main Menu.html`.
3. **F1.4 — PM-1/PM-2 task hierarchy** and release priority matrix.
4. **F2 — global navigation/current-vs-target map**.
5. **F3 — component/state + a11y/localization gap audit** for only the primitives needed by S0.
6. **F4 — validation protocol fixture**: test scripts, severity ledger, test-data profiles.
7. **S0-A — PM-1 contract/dependency audit**.
8. **S0-B — complete PM-1 task flow**.
9. Only then begin **S0-C low-fidelity wireframes**.

The provisional Main Menu mockup is revisited only at S0-H unless low-fidelity work reveals a reason to retire it earlier.

---

# 16. Definition of implementation-ready

A journey is ready for substantial Unity implementation only if all are true:

- Gate A: every control/data element has a real owner or is explicitly future-blocked;
- Gate B: task flow is complete, including back/cancel/error paths;
- Gate C: low-fidelity hierarchy is accepted;
- Gate D: relevant states are defined;
- Gate E: pseudo-locale, max text scale, keyboard, color independence, missing art, and layout constraints have been reviewed;
- Gate F: the end-to-end journey is prototyped;
- Gate G: no unresolved Blocker, and no unaccepted Major usability finding;
- Gate H: high-fidelity reference uses the shared system and records all states;
- Gate I: implementation packet maps components, data, commands, strings, assets, a11y, and QA acceptance criteria;
- any future-only behavior is visibly separated from current implementation scope.

A visually polished screenshot without these conditions is **not implementation-ready**.

---

# 17. Final readiness critique

This plan is ready to use because it now answers the questions the earlier drafts did not:

- **What is the unit of work?** A player journey.
- **What happens before drawing?** Contract/dependency audit and task flow.
- **When is high fidelity allowed?** After low-fidelity, state, cross-stream, prototype and usability gates.
- **How are a11y/localization handled?** As design inputs and test configurations, not final audits.
- **How is backend drift prevented?** Every action/data element is mapped at Gate A; missing seams stay outside UX.
- **How is Early Access scope controlled?** S0 + S1 are the cut; S2 cannot delay them.
- **How are existing mockups treated?** Reconciled references, not authority.
- **How are findings handled?** Severity + disposition + retest rules.
- **How is implementation protected from churn?** Gate H/I version lock and re-entry rules.
- **What happens next?** F1 audit work, not more high-fidelity design.

No further plan-level structural revision is required before beginning F1. Findings discovered during F1 may change **content or priority**, but should not require redesigning the planning framework unless they invalidate the PM-2 release target or #38/#49 architecture.

---

## Version History

| Version | Date | Change |
|---|---|---|
| 1.0 | September 4, 2026 | Detailed implementation plan after three critique/revision rounds. Defines durable artifacts, foundation work packages F0–F4, journey slices S0–S2, gates A–J, validation/severity rules, extreme-data testing, cross-stream parallelization, version/change control, QA handoff, UX debt handling, and the exact first sequence after plan approval. |
