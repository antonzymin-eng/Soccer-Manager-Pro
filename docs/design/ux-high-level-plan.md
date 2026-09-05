# System XI — UX High-Level Plan

**Created:** September 4, 2026  
**Status:** PLAN — CONVERGED HIGH-LEVEL DIRECTION  
**Scope:** Player-facing UX from current PM-1 client through Early Access and subsequent management depth  
**Related:** `ux-foundation.md`, UI / Client Framework #38, Localization & Accessibility #49, Match Presentation Depth #48, `path-to-playable-roadmap.md`, `ui-mockups/`

---

## 0. Purpose

This plan defines **how UX work is sequenced, validated, and handed to implementation before substantial additional screen production begins**.

It is deliberately separate from implementation. It creates no new simulation API, view-model contract, navigation edge, save format, or owning-spec behavior. APPROVED specs and production code remain authoritative.

The immediate rule is:

> **No additional high-fidelity UX screen should be treated as implementation-ready until it has passed the planning and validation gates defined here.**

The existing `Main Menu.html` mockup predates this plan and is therefore **provisional reference work**, not an approved implementation handoff.

---

# 1. Convergence record

The plan was deliberately developed through critique/revision rather than accepted at first draft.

## 1.1 High-level draft v0.1

Initial phase model:

1. establish UX principles;
2. map screens and navigation;
3. extend the design system;
4. mock up PM-1;
5. mock up PM-2;
6. refine management screens;
7. hand off to Unity;
8. iterate after Early Access.

### Critique v0.1

This was **not adequate** for implementation planning.

Problems:

- Too **screen-centric**; it treated screens as the unit of UX rather than player tasks and journeys.
- Too close to visual production; it allowed high-fidelity work before low-fidelity flow validation.
- No explicit player/task model or usability evidence.
- Accessibility and localization appeared as checks rather than design inputs.
- No dependency gate against missing backend seams, inviting phantom controls.
- No full-state design requirement for loading, empty, error, disabled, stale, save/load, and destructive actions.
- No explicit content/copy discipline, onboarding strategy, keyboard/focus model, or resolution validation.
- No measurable definition of when a design is ready for implementation.
- No mechanism to stop PM-3/deep-management design from consuming effort before PM-2 proves the core career loop.

**Revision consequence:** move from “screen production phases” to “experience architecture + validation gates.”

---

## 1.2 High-level draft v0.2

Revised phase model:

1. evidence baseline and player/task model;
2. information architecture and primary journeys;
3. shared interaction system including a11y/localization;
4. low-fidelity PM-1 prototype;
5. validate PM-1;
6. high-fidelity PM-1 + handoff;
7. repeat for PM-2;
8. repeat for later management depth.

### Critique v0.2

Substantially better, but still too **waterfall-like**.

Problems:

- A single giant “architecture” phase could delay learning from real flows.
- Global design-system work could become speculative and overbuild primitives never used.
- PM-1 and PM-2 would each pass through large monolithic batches rather than small end-to-end slices.
- Cross-stream dependencies—art, audio, localization, QA, Unity bindings—needed to be reviewed **inside each slice**, not after all design work.
- No explicit release cut line existed: “later management depth” could expand indefinitely.
- No formal rule existed for design decisions that conflict with current implementation versus future target UX.

**Revision consequence:** retain a small global foundation, then use a repeatable vertical-slice loop with explicit milestone cut lines.

---

## 1.3 High-level draft v0.3

Revised model:

- Foundation stage for evidence, task hierarchy, navigation rules, design-system audit, accessibility/localization constraints, resolution/input rules, and UX governance.
- Then each player journey becomes a vertical UX slice:
  1. contract/dependency audit;
  2. task flow;
  3. low-fidelity wireframe;
  4. full state matrix;
  5. accessibility/localization/art/audio review;
  6. interactive prototype;
  7. usability validation;
  8. high-fidelity reference;
  9. implementation handoff;
  10. implementation validation.
- Slice order follows PM-1 → PM-2 → PM-3/depth.

### Critique v0.3

This is structurally sound. Three residual gaps remained:

1. The plan needed an explicit **Early Access release cut** so “useful future UX” cannot silently become required pre-EA work.
2. The plan needed a **decision hierarchy** for current code vs future UX intent to prevent mockups from silently redefining navigation or behavior.
3. It needed a **design-change rule after handoff** so implementation is not destabilized by continuous visual churn.

These are addressed in the final plan below.

---

# 2. Settled high-level plan

The UX program uses **two layers**:

1. a one-time **UX Foundation**;
2. a repeatable **Vertical Slice Cycle** for each player journey.

The release roadmap determines slice order and the Early Access cut line.

---

## HL-0 — Freeze substantial UX implementation until planning is complete

### Goal
Prevent additional mockup/UI production from outrunning the experience architecture.

### Actions

- Treat existing UX mockups as references, not implementation-ready specifications.
- Treat `Main Menu.html` as provisional until its journey slice passes the final design gate.
- Do not add new runtime UI contracts merely to satisfy a mockup.
- Continue only planning, inventory, audit, research, and low-risk validation work until the detailed plan is approved.

### Exit
The detailed implementation plan exists, has passed critique/revision, and defines the first executable UX slice.

---

## HL-1 — Build the UX evidence and player-task foundation

### Goal
Define who the client is serving and what the player is trying to accomplish before defining screens.

### Outputs

- target player/archetype assumptions;
- primary player goals;
- task hierarchy;
- frequency/importance ranking of tasks;
- current UX asset and implementation inventory;
- competitor/reference benchmark plan;
- known constraints and non-goals;
- Early Access UX success criteria.

### Core principle
The basic unit of UX planning is a **player task / journey**, not a backend subsystem and not a screen.

Examples:

- start or continue a career;
- understand what needs attention today;
- prepare for the next match;
- choose a lineup and tactics;
- watch and influence a match;
- understand why the match ended as it did;
- progress to the next fixture;
- inspect and improve the squad.

### Exit
Every Early Access-critical task has an owner, priority, entry point, successful completion state, and failure/blocked state.

---

## HL-2 — Lock global experience architecture

### Goal
Create the smallest stable global UX architecture that individual slices can extend without redesigning navigation each time.

### Outputs

- launch-layer structure;
- career-shell structure;
- primary navigation model;
- secondary navigation / command-palette role;
- back-navigation rules;
- modal vs page vs drawer rules;
- notification/attention model;
- save/continue/load placement;
- settings/accessibility entry points;
- onboarding/help model;
- current-navigation vs target-navigation mapping.

### Decision hierarchy
When UX intent and current implementation differ:

1. **APPROVED normative spec wins.**
2. **Current production behavior is recorded accurately.**
3. A future UX target may be designed, but must be explicitly labeled **future / blocked / requires owning-spec or implementation change**.
4. A mockup alone never creates a runtime requirement.

### Exit
All P0/P1 journeys can be mapped through the shell without inventing unknown navigation semantics.

---

## HL-3 — Validate and complete the shared UX system

### Goal
Make the existing `touchline` system implementation-safe before multiplying screens.

### Preserve

- `touchline` visual direction;
- data-dense analyst-tool posture;
- existing design tokens where viable;
- 1920×1080 reference stage and desktop-first intent;
- command palette as accelerator, not sole navigation.

### Audit/complete

- typography hierarchy;
- density levels;
- spacing and layout rules;
- buttons and action hierarchy;
- tables, sorting, filtering, selection;
- tabs and navigation states;
- forms and selectors;
- tooltips/help;
- modals and confirmation;
- toasts/banners/errors;
- loading/empty/stale/partial states;
- keyboard focus/navigation;
- mouse behavior;
- text scaling and reflow;
- contrast and color-independent meaning;
- colorblind theme modes;
- font/glyph fallback;
- pseudo-localization behavior;
- localization expansion tolerance;
- icon semantics;
- art asset slots/fallbacks;
- audio/caption hooks where applicable.

### Rule
Do not create speculative components. A new primitive must be justified by a validated journey slice or a clearly universal requirement.

### Exit
A shared-component/state checklist exists and each component used by the first P0 slice is defined across relevant states.

---

## HL-4 — Establish the milestone UX backlog and release cut

### Goal
Tie UX effort to the playable roadmap and Early Access scope.

### P0 — PM-1 / immediate playable-match loop

Required first:

- Main Menu;
- match setup / Tactics Setup;
- Match View;
- Post-Match Report;
- all transitions and failure/empty states for that loop.

### P1 — PM-2 / Early Access core career loop

Required for Early Access target:

- New Game / career start;
- Continue / Load / Save flows appropriate to the shipped save promise;
- Career Home / Season hub;
- next fixture and fixture/calendar context;
- league table / competition context;
- advance/progress action;
- route into match preparation;
- post-match return into the career loop;
- settings/accessibility surface needed for release;
- minimum onboarding/help necessary to understand the loop.

### P2 — PM-3 / management depth

Designed/implemented only as owning systems become ready and after P1 core-loop validation:

- Squad depth;
- Training;
- Injuries/medical presentation;
- Transfers/contracts;
- Scouting;
- staff;
- finances;
- board;
- world/competition depth;
- history/statistics;
- inbox/news/man-management as the relevant systems land.

### P3 — post-EA/deep refinement

- advanced personalization;
- deep onboarding/tutorialization;
- advanced accessibility beyond the EA floor;
- lower-frequency workflow optimization;
- broad visual polish and delight layers;
- optional layout personalization if justified by evidence.

### Early Access cut rule
P2/P3 work may proceed only when it is clearly non-blocking and cannot delay unresolved P0/P1 usability or implementation work.

---

## HL-5 — Run every journey through the Vertical Slice Cycle

Every UX journey uses the same cycle.

### Step A — Contract and dependency audit

Before drawing controls:

- identify the owning spec/data source;
- identify existing read surfaces;
- identify existing command seams;
- identify current screen/navigation registration;
- identify localization, a11y, art, audio, analytics dependencies;
- mark any missing seam as a blocker owned outside UX.

**Gate A:** no phantom actions.

### Step B — Task flow

Define:

- trigger/entry point;
- player goal;
- required information;
- decisions/actions;
- alternate paths;
- cancellation/back behavior;
- completion state;
- blocked/error states;
- return destination.

**Gate B:** flow is coherent without visual styling.

### Step C — Low-fidelity information design

Produce wireframes that establish:

- information hierarchy;
- primary action;
- density;
- navigation;
- progressive disclosure;
- comparison patterns;
- keyboard/focus sequence;
- responsive/reflow expectations.

**Gate C:** no high-fidelity styling yet.

### Step D — Full state matrix

Design relevant states:

- default;
- hover/focus/pressed;
- selected;
- disabled + reason;
- loading;
- empty;
- partial;
- stale;
- error;
- offline/unavailable where relevant;
- saving/progress;
- destructive confirmation;
- success/failure feedback.

**Gate D:** the UX is defined outside the happy path.

### Step E — Cross-stream design review

Evaluate the slice with:

- localization/pseudo-locale;
- max supported text scale;
- keyboard-only operation;
- color-independent meaning;
- art removed / fallback assets;
- audio muted / caption path where relevant;
- small supported desktop layout;
- reference desktop layout;
- likely high-resolution/ultrawide behavior.

**Gate E:** the slice does not depend on ideal English copy, perfect art, color, or one screen size.

### Step F — Interactive prototype

Prototype the full task, not isolated screens.

**Gate F:** actions, back paths, focus movement, and critical transitions are testable before Unity implementation.

### Step G — Usability validation

Run task-based validation against defined success criteria.

Measure at minimum:

- task completion;
- navigation errors;
- missed critical information;
- time/steps for frequent actions;
- misunderstanding of disabled/error states;
- player confidence about “what happens next.”

**Gate G:** critical usability findings are resolved or explicitly accepted.

### Step H — High-fidelity reference

Only after the prior gates:

- apply `touchline` visual system;
- finalize component usage;
- apply art slots/fallbacks;
- finalize copy roles/keys;
- document transitions and states;
- note any future-only behavior clearly.

**Gate H:** design-ready.

### Step I — Implementation handoff

Provide:

- screen/journey map;
- state matrix;
- component mapping;
- data/read dependency map;
- action/command dependency map;
- localization key roles;
- asset slots/specs;
- accessibility behavior;
- acceptance criteria;
- known blocked/deferred behaviors.

**Gate I:** implementation-ready.

### Step J — Implementation validation

After Unity implementation:

- verify against the approved prototype/reference;
- verify interaction states;
- verify keyboard/focus;
- run pseudo-locale + max text-scale checks;
- test fallbacks with art unavailable;
- test supported resolutions;
- test real data extremes;
- validate on the Unity certification host where required.

**Gate J:** implementation matches the UX contract without introducing presentation-owned domain logic.

---

## HL-6 — Control post-handoff design churn

### Goal
Prevent implementation from chasing a moving target.

Once a slice passes Gate H/I:

- changes that fix a demonstrated usability/accessibility defect are allowed;
- cosmetic preference changes are deferred unless low-cost and non-disruptive;
- navigation or action-semantics changes must re-enter at the appropriate earlier gate;
- any change requiring a new backend mutation seam returns to Gate A and is owned by the relevant domain spec;
- the approved reference receives a version/change note.

---

## HL-7 — Early Access UX feedback loop

### Goal
Use Early Access to improve evidence, not to replace pre-release usability work.

Prepare before EA:

- feedback taxonomy by journey/task;
- bug vs usability vs missing-feature distinction;
- issue severity rules;
- high-frequency-friction watch list;
- optional telemetry requirements where justified and privacy-appropriate;
- periodic UX review cadence;
- rule for promoting P2/P3 work ahead of polish based on evidence.

Early Access feedback should primarily answer:

- where players get lost;
- what information they cannot find;
- which frequent workflows take too many actions;
- which states are misread;
- which management decisions lack understandable consequences;
- which planned depth matters enough to prioritize next.

---

# 3. UX principles that remain fixed unless evidence overturns them

1. **Task-first, not subsystem-first.** Organize around what the manager is trying to accomplish.
2. **Dense, not cramped.** Optimize for scanning and comparison rather than oversized presentation cards.
3. **Decisions before decoration.** Important information and action hierarchy precede visual flourish.
4. **Stable navigation.** Frequent destinations should not move between contexts.
5. **Progressive disclosure.** Keep frequent information visible; reveal lower-frequency detail on demand.
6. **Explicit state.** The player must distinguish unavailable, loading, empty, disabled-by-rule, failed, stale, and unsupported.
7. **No phantom controls.** A mockup cannot invent a command seam.
8. **Art-independent comprehension.** Core flows must remain understandable with fallback assets.
9. **Localization/a11y by construction.** Pseudo-locale, text scale, focus, contrast and glyph coverage are design inputs, not final audits.
10. **Keyboard + mouse efficiency.** Frequent management actions should be fast without making keyboard shortcuts mandatory.
11. **Current reality and future intent are labeled separately.** Design references must never imply that future behavior already exists.
12. **Validate before polish.** High fidelity follows a coherent, tested flow.

---

# 4. High-level definition of ready

The high-level UX program is ready to proceed to substantial design/implementation only when:

- the detailed plan expands every HL stage and Vertical Slice gate into concrete tasks/artifacts;
- the first P0 slice is named and scoped;
- its contract/dependency audit is complete;
- the global navigation and state rules are documented;
- a11y/localization requirements are embedded in its acceptance criteria;
- supported desktop layout test cases are defined;
- usability-validation method and pass/fail handling are defined;
- design handoff/change-control rules are defined;
- P0/P1/P2 cut lines are explicit.

The companion detailed plan is the implementation of this high-level structure.

---

## Version History

| Version | Date | Change |
|---|---|---|
| 1.0 | September 4, 2026 | Converged high-level UX plan after three critique/revision rounds. Replaces screen-first sequencing with a foundation + repeatable vertical-slice cycle, explicit PM/Early-Access cut lines, design gates, dependency discipline, a11y/localization integration, usability validation, and post-handoff change control. |
