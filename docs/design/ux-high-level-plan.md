# System XI — UX High-Level Plan

**Created:** September 4, 2026  
**Last Updated:** September 4, 2026  
**Version:** 1.1  
**Status:** PLAN — CONVERGED AFTER EXTERNAL DEPENDENCY REVIEW  
**Scope:** Player-facing UX planning from the current PM-1 presentation surface through the PM-2 Early Access loop  
**Execution plan:** [`ux-detailed-plan.md`](ux-detailed-plan.md)

---

## 0. Purpose

This document defines the **strategy, scope, ordering, ownership, and release cut** for UX work. It deliberately does not repeat the execution gates; Gates A–J are defined once, in `ux-detailed-plan.md`.

No UX document creates a simulation API, view model, command seam, navigation edge, save promise, or domain rule. Authority remains:

1. APPROVED specifications;
2. the implementation/design documents that govern the existing client surface;
3. production code and verified current behavior;
4. explicitly labelled future UX intent;
5. visual references/mockups.

A polished mockup is not evidence that a capability exists.

---

## 1. Existing authority this plan extends

UX work must begin from the client architecture already in the repository, not re-derive it.

Primary governing references:

- `docs/specs/ui-client-framework/` — UI / Client Framework #38;
- `docs/tracking/ui-client-framework-design.md` — #38 design supplement;
- `docs/tracking/ui-framework-t0-implementation-plan.md` — landed host-free framework substrate;
- `docs/tracking/interactive-unity-client-design.md` — PM-1 client phases P0–P6, P4a/P4b and P5a/P5b splits, and §12's logic-out-of-`MonoBehaviour` rule;
- `docs/tracking/path-to-playable-roadmap.md` — B-series client sequencing and PM-1/PM-2 milestone definitions;
- `src/client-app/ClientScreens.cs` and `ClientScreenFlow.cs` — the current four screen identities and five-edge graph;
- `src/match-client-unity/README.md` — Unity binding status and cert-host contract;
- `src/match-viewer/` and `src/match-client-web/` — host-free real-match presentation/reference harnesses, retained as reference surfaces rather than the shipping UI;
- `docs/specs/localization-accessibility/` and its content-tier design — localization/a11y boundaries;
- `docs/specs/match-presentation-depth/` and `docs/specs/audio-sound-design/` — presentation/audio composition boundaries.

The existing `touchline` visual direction and `docs/design/ui-mockups/` remain useful visual references, not runtime authority.

---

## 2. Verified starting-state corrections

The first planning draft contained assumptions that were too coarse. The UX plan now distinguishes **landed code**, **host verification**, **screen binding**, and **domain implementation** separately.

### 2.1 PM-1 Unity client

Current state:

- host-free P0–P3, P4a, P5a and the head-less half of P6 are landed;
- P4b's `MatchClientBehaviour.cs` Unity binding is **landed as code**, but `src/match-client-unity/` is excluded from the Linux shim and that binding has **never compiled or run on the pinned Unity host**;
- P5b, the UGUI shell/binding for Main Menu, Tactics Setup, Match View and Post-Match Report, remains open;
- the on-host half of P6 remains open;
- therefore there is **no launchable shipping PM-1 Unity client today**.

Consequence: S0 design and prototype work may proceed host-free, but final implementation acceptance cannot occur until the roadmap's remaining Unity work closes. `ux-detailed-plan.md` maps the relevant gates directly to B8/P4b host verification, B9b/P5b and B10/P6.

### 2.2 PM-2 season loop

The earlier blanket assumption that #30 had no implementation is also incorrect.

Current state:

- #30 implementation lives in `src/season-save/`, including `SeasonLoop`, season save state, league bootstrap, and T2 round/day progression behavior;
- `SeasonLoop.AdvanceAndPlayNextRound` exists;
- the host-free UI framework does **not** therefore automatically have a season screen, season view-model source, `AdvanceRound` dispatcher, or career-shell navigation identity;
- some older UI comments still describe #30 as assembly-less and must not be treated as current-state evidence.

Consequence: S1 is not globally "blocked on #30". Gate A must classify each needed PM-2 presentation capability independently: domain seam present, UI projection present/absent, command adapter present/absent, screen identity present/absent, and Unity binding present/absent.

### 2.3 Deeper management systems

Several P2 systems remain specification-only. UX may study their information architecture, but implementation-bound design is capability-gated. A missing `src/` assembly or action seam is `FUTURE-BLOCKED`, not a reason to fabricate a UI contract.

---

## 3. Settled UX operating model

The UX program uses two layers:

1. a small **Foundation** that establishes evidence, tasks, current-vs-target architecture, shared interaction constraints, and validation mechanics;
2. repeatable **Journey Slices** that take one complete player task from contract audit through implementation verification.

The unit of work is a **journey**, not an individual screen and not a backend subsystem.

Examples:

- play one match end-to-end;
- start/resume a career and progress through a league round;
- inspect and select a squad;
- recruit a player.

The detailed cycle and pass criteria live only in `ux-detailed-plan.md`.

---

## 4. Foundation scope

The Foundation is intentionally small. It must answer only the cross-cutting questions that would otherwise be re-litigated in every journey.

### F1 — Evidence and capability baseline

Produce:

- current capability matrix using the labels below;
- existing mockup reconciliation;
- PM-1/PM-2 player-task hierarchy;
- milestone/release priority;
- known UX constraints and open decisions.

Capability labels:

- `LIVE` — production behavior exists and its relevant verification state is known;
- `DESIGNABLE` — the owning read/action seam exists, but the player-facing surface does not;
- `FUTURE-BLOCKED` — implementation needs an owning-system/client change that does not exist yet;
- `OUT-OF-EA` — deliberately outside the Early Access cut;
- `UNKNOWN` — evidence is insufficient and must be resolved before the capability is used.

A status must include its **verification qualifier** where material, e.g. `LIVE / HOST-UNVERIFIED`.

### F2 — Current-vs-target experience architecture

Do **not** redesign the current PM-1 graph. Record it exactly:

`Main Menu → Tactics Setup → Match View → Post-Match Report → Main Menu`, with the existing cancel edge from Tactics Setup.

Future career-shell navigation is a separate target map. Any new `ScreenId`, edge, dispatcher, or view-model source is explicitly future until allocated by its owning implementation/spec work.

### F3 — Shared interaction constraints

Audit and define only the primitives required by S0/S1:

- action hierarchy;
- dense tables/data comparison;
- focus and keyboard behavior;
- loading/empty/error/disabled/stale states;
- localization expansion/reflow;
- text scaling/contrast/color-independent meaning;
- asset slots and missing-art fallbacks;
- caption/audio coexistence where applicable;
- supported desktop layout behavior.

No speculative component catalogue.

### F4 — Validation protocol

Define:

- scripted heuristic/self-walkthrough checks;
- independent participant test mechanism for S0/S1;
- severity and disposition rules;
- test-data extremes;
- implementation verification procedure.

Gate G is binding, not aspirational; the concrete mechanism is in the detailed plan.

---

## 5. Milestone backlog and Early Access cut

### S0 — PM-1: play one match end-to-end

Journey:

`launch/entry → tactics setup → live match → post-match report → return`

Design scope:

- Main Menu;
- Tactics Setup reconciliation;
- Match View;
- Post-Match Report;
- transition, focus, disabled, loading/error and fallback states.

Dependency posture:

- design through handoff may proceed while remaining Unity host/binding work is open;
- the browser/replay surfaces may supply real-match data/reference behavior for prototype validation, but **must not be extended into a second shipping UI**;
- Gate J is blocked until the remaining Unity roadmap work is actually verifiable on-host.

### S1 — PM-2: run the Early Access season loop

Journey:

`launch → new/continue/load → season context → prepare/advance → match → report → season context → save/resume`

S1 begins with a capability audit, not an assumption that either everything exists or nothing exists.

Known starting fact: #30's season loop/save/league bootstrap is implemented in `src/season-save/`. Unknown/missing player-facing adapters and navigation are classified individually at Gate A.

Low-fidelity design may proceed against a specified future surface when clearly labelled `FUTURE-BLOCKED`. High-fidelity handoff may not present a future capability as current.

### S2 — PM-3 / management depth

Capability-gated journeys only after S1's core loop is validated sufficiently that they cannot displace Early Access work.

Examples:

- squad inspection/selection;
- training/progression;
- injuries/availability;
- transfers/contracts;
- scouting;
- staff/finances/board/world depth.

Each starts with Gate A. Specification-only systems remain design research, not implementation handoffs.

### Early Access cut

S0 + S1 define the UX release path. S2/P3 work may run in parallel only when it does not delay unresolved S0/S1 design, client implementation, host verification, accessibility/localization, or release-critical QA.

---

## 6. Prototype strategy

The project already has two useful host-free presentation surfaces:

- `src/match-viewer/` — replay/live streaming foundation;
- `src/match-client-web/` — a browser match-client reference harness over real match data.

They are **reference/test vehicles, not the shipping UI**.

For S0:

- use them to observe real engine behavior and data density before Unity is available;
- use captured/representative real outputs in the UX prototype where useful;
- validate Match View information hierarchy against real runs;
- do not add a second product feature path or new domain logic to the browser client merely to satisfy UX prototyping.

The end-to-end UX prototype may remain a design artifact under `docs/design/`; it is validated against real data/reference behavior rather than pretending to be the shipping client.

---

## 7. Ownership and decision rights

Role ownership is explicit even where no individual is named.

| Concern | Accountable role |
|---|---|
| UX plan, task flows, wireframes, prototypes, findings ledger | UX workstream |
| Release scope and accepted Major findings | Project owner |
| Domain/read/action seam truth | Owning subsystem/spec implementation |
| Current PM-1 navigation truth | `client-app` / interactive Unity client plan |
| P5b/Unity binding implementation | Unity client workstream |
| On-host verification | Certification/Unity-host workstream |
| Localization/a11y contract | #49 workstream + renderer owner |
| Art slots/assets | Art + UX workstreams |
| QA acceptance cases | UX + QA workstreams |

No UX author may accept a missing domain seam on behalf of its owner.

---

## 8. Timebox and sequencing estimate

These are **effort bands, not release dates**. They assume one primary UX contributor and exclude waiting for external code/host availability.

| Work | Active UX effort | Dependency note |
|---|---:|---|
| F1 evidence/capability baseline | 1–2 working days | starts immediately after plan/tracking close-out |
| F2 current-vs-target architecture | 0.5–1 day | uses existing PM-1 graph; no re-authoring |
| F3 shared S0/S1 interaction audit | 1–2 days | only primitives actually needed |
| F4 validation setup | 0.5–1 day | includes participant booking before S0-G |
| S0 A–E | 2–4 days | host-free |
| S0 F–G | 2–4 days | prototype + one capped participant round |
| S0 H–I | 2–3 days | produces P5b implementation handoff |
| S0 J | external dependency | requires remaining Unity host/binding/cert work |
| S1 A–E | 3–5 days | may overlap Unity S0 work; classification-driven |
| S1 F–G | 2–4 days | one capped participant round |
| S1 H–I | 2–4 days | only current/designable scope handed off |
| S1 J | implementation-dependent | career-shell/Unity surfaces must exist |

Planning therefore does not place a quarter-long design phase in front of PM-2. The foundation is intentionally measured in days, then work proceeds in vertical slices.

---

## 9. Change control

Once a journey passes detailed-plan Gate H/I:

- demonstrated usability/accessibility fixes may reopen the relevant earlier gate;
- cosmetic preference changes are deferred unless low-cost and non-disruptive;
- navigation/action-semantics changes return to the appropriate contract/flow gate;
- a change needing a new domain seam returns to Gate A;
- the journey packet records the revision and affected gate.

---

## 10. Convergence record

### Draft 0.1 critique

Rejected for being screen-centric, allowing visual work before flow validation, and lacking dependency/a11y/localization/state gates.

### Draft 0.2 critique

Rejected for becoming waterfall-like and for treating shared-system work as a speculative up-front build.

### Draft 0.3 critique

Accepted structurally after adding vertical journey slices, an Early Access cut, current-vs-future decision hierarchy, and change control.

### External dependency critique — September 4, 2026

A subsequent repository-grounded review identified four valid structural corrections and two status corrections:

**Valid corrections incorporated:**

1. S0 must name its relationship to P4b/P5b/P6 and cannot claim a launchable Unity client before host/binding completion.
2. Gate G needed a real participant mechanism rather than an unlimited provisional escape hatch.
3. The plan must cite and inherit the existing UI/client implementation documents and current graph.
4. Planning needs explicit ownership, effort bands, and repository tracking/discoverability.

**Status claims corrected rather than adopted:**

- P4b is not "unlanded": its Unity binding code is landed, but host compilation/runtime verification is outstanding.
- #30 is not assembly-less: its T1/T2 implementation is in `src/season-save/`; the meaningful PM-2 gap is the player-facing UI projection/dispatch/navigation surface, which Gate A must measure rather than assume.

This revision preserves the review's underlying dependency concern while grounding the plan in the current tree.

---

## 11. High-level exit

The high-level plan is settled when:

- this document contains strategy/scope only;
- execution gates exist once, in `ux-detailed-plan.md`;
- `ux-foundation.md` is explicitly historical/superseded;
- current client authority is linked;
- S0 and S1 dependency posture is explicit;
- Gate G has a binding mechanism;
- effort/ownership are explicit;
- repository tracking surfaces route agents to the plan.

After those conditions are landed, F1 is the next UX action. No additional high-fidelity screen production precedes it.

---

## Version History

| Version | Date | Change |
|---|---|---|
| 1.0 | September 4, 2026 | Initial converged high-level plan after three internal critique/revision rounds. |
| 1.1 | September 4, 2026 | External dependency-review revision: inherited existing client-plan authority; corrected P4b/#30 status; tied S0 to B8/B9b/B10; made Gate G binding; added prototype vehicle, role ownership, effort bands, and tracking/discoverability exit condition; removed duplicated gate definitions from the high-level plan. |