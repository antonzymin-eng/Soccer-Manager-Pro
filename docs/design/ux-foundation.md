# System XI — UX Foundation and Early-Access Screen Map

**Created:** September 4, 2026  
**Status:** DESIGN REFERENCE (non-normative)  
**Working title:** System XI  
**Related:** UI / Client Framework #38; `docs/design/ui-mockups/`; `docs/tracking/path-to-playable-roadmap.md`

---

## 1. Purpose and boundary

This document starts the UX/design workstream that runs in parallel with simulation, Unity UI implementation, art production, localization infrastructure, QA, and commercial preparation.

It does **not** create new simulation contracts, screen APIs, view-model contracts, or implementation obligations. Where an APPROVED specification disagrees with this document, the specification wins. A screen that depends on an unbuilt owning seam remains design-only until that seam exists, consistent with UI Framework #38 §7.1.

The objective is narrower: establish the player-facing information architecture, screen priorities, interaction rules, and art integration slots early enough that UI implementation and asset production can proceed without waiting for the backend to be complete.

---

## 2. Existing baseline — preserve, do not restart

The project already has a real visual foundation in `docs/design/ui-mockups/`.

The UX workstream inherits these decisions:

- **Visual direction:** `touchline` — an analyst tool, not broadcast graphics.
- **Reference stage:** desktop-first, 1920×1080 reference layout with density guardrails.
- **Existing foundations:** design system, typography, color tokens, spacing/radii, buttons, inputs, tabs/chips, tables, cards/modals, stat tiles, attribute displays, formation pitch, match-day HUD, command palette.
- **Existing management mockups:** Squad, Tactics, Training, Scouting, Transfers, Club, Club Finances, Club Staff, Club Board Room, Club History, World.
- **Implementation boundary:** these HTML mockups are visual references, not shipped code and not a new contract surface.

The current UX gap is therefore **not** “invent a visual style.” It is to turn the existing style into a coherent end-to-end game experience and fill the screens/states that are still absent.

---

## 3. Current UX gap against the playable roadmap

The implemented PM-1 client catalogue currently contains four screens:

1. Main Menu
2. Tactics Setup
3. Match View
4. Post-Match Report

The current visual mockup set covers Tactics, but does not yet provide dedicated reference mockups for Main Menu, Match View, or Post-Match Report. PM-2 additionally needs the player to understand and control the season loop: fixtures, league state, advancing time/rounds, and returning naturally into match preparation.

That makes the first UX priority the **play loop**, not additional deep-management screens.

### Priority order

| UX priority | Surface | Why it is first |
|---|---|---|
| UX-P0 | Main Menu | Entry point and root of the existing client navigation graph. |
| UX-P0 | Tactics Setup refinement | Existing mockup exists; must be reconciled with the actual PM-1 interaction flow rather than redesigned. |
| UX-P0 | Match View | Critical missing visual reference for the shipping Unity client. |
| UX-P0 | Post-Match Report | Completes the current four-screen PM-1 loop. |
| UX-P1 | Season / League hub | Core PM-2 surface: table, fixtures/calendar, next match, advance action. |
| UX-P1 | New-game / career start flow | Required for a player-facing season/career entry rather than developer bootstrap. |
| UX-P1 | Save / Continue / Load surfaces | Required before Early Access saves become a player promise. |
| UX-P2 | Squad / Transfers / Training / Scouting / Club refinement | Existing references already exist; iterate when their production seams are ready. |
| UX-P2 | Settings / accessibility / controls | Needed before commercial release; design can begin before final implementation. |

---

## 4. Early-Access information architecture

The target experience should feel like one management workspace rather than a collection of disconnected feature screens.

### 4.1 Launch layer

**Main Menu**

- Continue
- New Game
- Load Game
- Settings
- Credits
- Exit

Only actions supported by the current build should be enabled. Unsupported actions should not route to placeholder gameplay.

### 4.2 Career shell

Once a career is active, the primary navigation model should group information by the manager's decision context rather than by backend subsystem name.

**Core destinations**

- **Home / Season** — next fixture, league position, recent result, immediate decisions, advance action.
- **Squad** — roster, selection status, player inspection.
- **Tactics** — formation and instructions.
- **Training** — gated on #29 production support.
- **Scouting** — gated on #32 production support.
- **Transfers** — gated on #31 production support.
- **Club** — finances, board, staff, history as sub-areas where appropriate.
- **World** — competitions, tables, fixtures and broader football context.

The existing command-palette concept remains a secondary fast-navigation path; it should not replace visible primary navigation for new players.

### 4.3 Match journey

The shortest PM-1/PM-2 interaction loop is:

`Season/Home → Match preparation/Tactics → Match View → Post-Match Report → Season/Home`

For the current PM-1 implementation, the existing code graph remains authoritative:

`Main Menu → Tactics Setup → Match View → Post-Match Report → Main Menu`

UX work may illustrate the future season-shell loop, but it must not silently change the currently implemented navigation graph.

---

## 5. Interaction principles

### 5.1 Dense, not cramped

Football management is information-heavy. System XI should optimize for scanning and comparison rather than oversized presentation cards. Density is acceptable when hierarchy remains obvious.

### 5.2 Decisions before decoration

Every major screen should answer, in order:

1. What changed?
2. What matters now?
3. What can I do about it?
4. What happens if I do nothing?

Decorative art should support those answers, not compete with them.

### 5.3 Stable navigation

Primary navigation should remain spatially stable across career screens. A player should not need to relearn where Squad, Tactics, Club, or World live from screen to screen.

### 5.4 Progressive disclosure

High-frequency information stays visible. Secondary detail belongs in expandable rows, side panels, tabs, drawers, tooltips, or drill-down screens rather than competing with the primary task.

### 5.5 Explicit system state

The UX must distinguish:

- actionable vs read-only;
- unavailable vs not yet discovered;
- disabled by rules vs disabled because implementation is absent;
- live match vs paused/frozen full-time state;
- save/load progress, success and failure;
- empty data vs loading vs error.

### 5.6 Keyboard and mouse parity for management work

The desktop client should support fast keyboard-driven navigation for high-frequency actions while remaining fully usable with a mouse. The existing command palette is a useful accelerator, not the sole accessibility path.

---

## 6. Shared component/state inventory

The existing design-system page already establishes many components. Production UX work should ensure each of these has the full state set before screens multiply.

| Component | Required UX states |
|---|---|
| Primary/secondary button | default, hover, pressed, disabled, busy |
| Tab / nav item | default, hover, active, disabled, attention indicator |
| Table row | default, hover, selected, focused, unavailable, warning |
| Input / selector | empty, populated, focused, invalid, disabled |
| Card / panel | normal, selected, stale/outdated, warning, critical |
| Modal | confirm, destructive confirm, progress, failure |
| Tooltip | concise explanation, rule reason, disabled-action reason |
| Toast/banner | success, warning, non-blocking error, blocking error |
| Data surface | loading, empty, partial, error, stale |

No screen should invent a one-off visual language for these states.

---

## 7. Art ↔ UX integration contract

The art pipeline and UX stream can work independently only if the UI defines stable **slots** rather than depending on final assets.

### 7.1 Asset slots to define early

| Asset family | UX role | Required fallback |
|---|---|---|
| Club badge | identity in tables, headers, fixtures, match HUD | generated/default crest mark |
| Player portrait | player detail and selective high-value surfaces | silhouette / generated placeholder |
| Staff portrait | staff detail where used | silhouette / generated placeholder |
| Stadium / venue image | atmosphere on match/club surfaces | neutral venue background |
| Competition mark | tables, fixtures, world navigation | text + generic competition symbol |
| Country / region mark | nationality and world context | text code or generic flag treatment |
| UI icons | navigation, actions, status | text label must remain understandable without icon |
| Background / key art | launch and high-level atmosphere | flat design-system background |
| Match presentation assets | HUD markers, event icons, overlays | minimal geometric/token-based fallback |

### 7.2 Rules

- Core navigation and decision comprehension must survive with **no final art installed**.
- Asset absence must degrade to an intentional fallback, not a broken layout.
- Portraits/badges must not determine row height or critical information density.
- Text labels remain authoritative for actions; icons supplement them.
- The UI should reserve final aspect ratios and safe areas before final art is produced.
- Localization expansion must be considered before finalizing fixed-width labels or buttons.

---

## 8. First mockup slices

### UX-S1 — Complete the existing PM-1 loop

Produce visual references, using the existing `touchline` system, for:

1. Main Menu
2. Match View
3. Post-Match Report
4. Reconcile the existing Tactics mockup with the actual Tactics Setup role in the current client graph

**Exit condition:** all four currently implemented `ClientScreens` have a coherent visual reference and their transitions make sense as one journey.

### UX-S2 — Design the PM-2 season loop

Produce a Season / League hub reference with:

- league table;
- next fixture;
- fixture/calendar view;
- latest result;
- advance-to-next-round action;
- clear route into match preparation;
- clear return path after the post-match report.

**Exit condition:** the player can understand how one league round becomes the next without knowing the underlying #30 API.

### UX-S3 — Shared interaction states

Extend the design reference with:

- loading / empty / error / disabled states;
- modal patterns;
- save/load feedback;
- destructive confirmation;
- keyboard focus treatment;
- tooltip treatment for football rules and unavailable actions.

### UX-S4 — Art-slot wireframes

Add placeholder slots for badges, portraits, stadium imagery, competition marks and match-presentation art to the priority screens. This becomes the handshake with the parallel art pipeline.

---

## 9. Early-access UX acceptance criteria

A design slice is ready to hand to implementation when:

- the primary player goal is obvious within one screen scan;
- every visible action has a defined result or is clearly unavailable;
- navigation into and out of the screen is explicit;
- loading, empty, disabled and error states are defined where relevant;
- the screen works with placeholder art;
- the screen works without relying on color alone for meaning;
- critical numeric data can be compared quickly;
- likely localization expansion does not destroy the layout;
- the design uses existing System XI tokens/components unless a documented new primitive is genuinely required;
- no design-only control invents a backend mutation path that the owning spec has not provided.

---

## 10. Parallel-work boundaries

| Workstream | UX can proceed now | UX must wait for |
|---|---|---|
| Unity UI implementation | screen layouts, navigation concepts, component states, placeholders | final binding details where no owning seam exists |
| Art/assets | slot definitions, aspect ratios, fallback behavior, usage hierarchy | final art production does not need backend completion |
| Localization | flexible layout rules, text expansion allowance, string-role inventory | final translated copy |
| Simulation/backend | UX can use representative mock data | authoritative behavior remains owned by specs/code |
| QA/playtesting | task flows, usability scenarios, screen acceptance criteria | final interaction instrumentation can land later |

The guiding rule is: **design the experience early; do not fabricate runtime contracts to make the mockup look complete.**

---

## 11. Immediate next work

1. Build the **Main Menu** visual reference in the existing mockup system.
2. Build the **Match View** visual reference, reusing the established match-day HUD language and the actual Unity client responsibilities.
3. Build the **Post-Match Report** visual reference against #37 analytics output.
4. Review the existing **Tactics** mockup against the current Tactics Setup screen contract and remove any interaction that assumes a nonexistent seam.
5. Then design the **Season / League hub** as the first PM-2 UX surface.

This order deliberately follows the playable ladder rather than polishing lower-priority management surfaces first.

---

## Version History

| Version | Date | Change |
|---|---|---|
| 1.0 | September 4, 2026 | Initial UX foundation: preserved `touchline`, mapped current gaps against PM-1/PM-2, established information architecture, interaction principles, art-slot contract, and first UX slices. |
