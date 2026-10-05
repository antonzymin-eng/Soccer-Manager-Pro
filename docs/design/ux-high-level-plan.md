# System XI — UX High-Level Plan

**Created:** September 4, 2026  
**Last Updated:** October 5, 2026\
**Version:** 1.21\
**Status:** PLAN — CONVERGED AFTER EXTERNAL DEPENDENCY REVIEW\
**Scope:** Player-facing UX planning from the current PM-1 presentation surface through the PM-2 Early Access loop\
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
- P4b's `MatchClientBehaviour.cs` Unity binding is **landed and partially host-verified**: pinned-editor compilation succeeded September 12 and the tracked scene booted/rendered in Play mode September 13; click-to-command and the cert-host render-loop/performance capture remain open;
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
- owner image-review mechanism for S0; independent participant mechanism for S1;
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
| F1 evidence/capability baseline | 3–4 working days | starts immediately after plan/tracking close-out |
| F2 current-vs-target architecture | 0.5–1 day | uses existing PM-1 graph; no re-authoring |
| F3 shared S0/S1 interaction audit | 1–2 days | only primitives actually needed |
| F4 validation setup | 0.5–1 day | includes the S0 owner image-review record |
| S0 A–E | 2–4 days | host-free |
| S0 F–G | 2–4 days | prototype + owner image review |
| S0 H–I | 2–3 days | produces P5b implementation handoff |
| S0 J | external dependency | requires remaining Unity host/binding/cert work |
| S1 A–E | 3–5 days | may overlap Unity S0 work; classification-driven |
| S1 F–G | 2–4 days | one capped participant round |
| S1 H–I | 2–4 days | only current/designable scope handed off |
| S1 J | implementation-dependent | career-shell/Unity surfaces must exist |

`ux-detailed-plan.md` §10.2 is the authority for these bands and this table mirrors it; where the two disagree the detailed plan wins and this table is the defect. That is the same single-definition rule §0 applies to Gates A–J.

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

After those conditions are landed, F1 is the next UX action. No additional high-fidelity screen production precedes it. *Status note, September 30, 2026: F0–F4 and S0 A–F are complete. The owner's pinned v0.4 approval remains in validation protocol §9.1; v0.5 delta is owner-approved at `0e8bd2b` (§9.1.1), with the other 18 carried forward apart from version text. H is PASS for the separately owner-approved v0.1 reference at `4a6220c` (23 image pairs, packet §13 / protocol §9.1.2); I is OPEN for the implementation handoff. Execution status is tracked in `ux-detailed-plan.md` and `docs/tracking/open-issues.md`, not here.*

*Superseded status update, October 1, 2026: the actual H v0.1 approval remains preserved in protocol §9.1.2. H is reopened for separate review of corrected v0.2 (§9.1.3); I is on hold for this revision and #470 remains blocked on completed I.*

*Prior status correction, October 1, 2026: the preceding reopening/hold was an author interpretation, not a new owner decision. H remains PASS for approved v0.1 and I remains OPEN using its sources/images pinned to `4a6220c`. Corrected v0.2 awaits separate approval before adoption (§9.1.3); if it is not approved, v0.1 remains the handoff baseline. #470 remains blocked on completed I.*

*Status update after owner approval, October 1, 2026: Anton Zymin replied “images approved” at 12:35:32 America/Los_Angeles to all 23 H v0.2 views at `a1044d5` (protocol §9.1.3). H PASS / I OPEN now use approved v0.2; v0.1 approval remains preserved. #470 remains blocked on completed I.*


*Status update, October 1, 2026: S0 Gate I is IN PROGRESS through journey packet §14, the P5b handoff draft. Identity reconciliation (S0-I-001), shipping text-scale allocation (S0-I-002) and dynamic UI formatting allocation (S0-I-003) remain open. H stays PASS for approved v0.2; #470 remains blocked on completed I. Execution and live blockers remain in the detailed plan and open issues.*
*Status update, October 3, 2026: Anton Zymin approved the recommended B/C/A choices: distinct demo squads, 100–200% text scaling and the client formatter. Journey §14.10 records the actual instruction and workstream ownership. Product directions are settled; binding/dependency contracts and affected reference reconciliation remain open. I stays IN PROGRESS and #470 blocked until final handoff completion. Earlier status updates remain dated history.*

*Status update, October 3, 2026 — contract-completion draft: [S0 binding contracts](ux-s0-binding-contracts.md) §§2–5 define the proposed identity/fixture, scale/reflow, copy/provider/dependency and reference inputs against main `3429fafd`. Final review of the complete mapping and recorded verdict are next. I remains IN PROGRESS and #470 blocked; earlier approvals and status updates remain history.*

*Status update, October 4, 2026: S0 Gate I passed the technical implementation-handoff review at e8ffd89 (journey §14.11). Identity/scale/copy decisions and reference reconciliation are complete; actual client/localization/host verification remains due. Main's #470 Gate-I block clears only after #478 lands; refresh, fresh CI and exact-head pinned Unity compile precede its merge. Earlier dated updates and owner approvals remain history.*

---

*Status update, October 5, 2026: #470 foundation landed at f276f700 after green exact-head CI and recorded pinned Unity compile/ClientApp evidence. The next consumed lifecycle/identity slice is planned in [P5b implementation plan](../tracking/p5b-lifecycle-identity-plan.md). #49 L2 precedes localized screens. Strategy, S0 scope, Gate-I approval and Gate-J/B8/B9b/B10 acceptance requirements are unchanged.*

## Version History

| Version | Date | Change |
|---|---|---|
| 1.0 | September 4, 2026 | Initial converged high-level plan after three internal critique/revision rounds. |
| 1.1 | September 4, 2026 | External dependency-review revision: inherited existing client-plan authority; corrected P4b/#30 status; tied S0 to B8/B9b/B10; made Gate G binding; added prototype vehicle, role ownership, effort bands, and tracking/discoverability exit condition; removed duplicated gate definitions from the high-level plan. |
| 1.2 | September 9, 2026 | Corrects §8's F1 effort band 1–2 → **3–4 working days**, matching the revision `ux-detailed-plan.md` made at its own v1.2 on September 6, 2026. This table had duplicated the band rather than citing it, was not updated with the detailed plan, and so contradicted the execution authority for three days — found by an external pre-merge review of PR #362. §8 now names `ux-detailed-plan.md` §10.2 as the authority it mirrors, so the next band revision has one place to land. §11's "F1 is the next UX action" carries a dated status note: F0–F3 are complete and F4 is next. `**Version:**` and `**Last Updated:**` are re-derived in the same commit — omitting that is exactly the desync this workstream had to correct in the detailed plan at v1.3. No band other than F1 differs between the two documents; all twelve were compared. |
| 1.3 | September 21, 2026 | Reconciles current client evidence and UX phase status after PR #406 Gate A. P4b is no longer described as never having run: pinned-editor compilation and tracked-scene Play-mode boot/render smoke are recorded while click-to-command and cert-host performance evidence remain open. The dated §11 note advances to F0–F4 + S0 Gate A complete / Gate B next. Strategy, release cut, gate definitions and effort bands are unchanged. |
| 1.4 | September 28, 2026 | Status-note sync only: S0 Gate B completes in `ux-s0-pm1-journey.md` v0.5 §7 (owner-confirmed September 28, 2026) and Gate C is next. No plan content changes. |
| 1.5 | September 30, 2026 | Status-note sync only: S0 C–E complete and F prototype ready; formal F pending both pre-F participant attestations. Strategy and gate definitions unchanged. |
| 1.6 | September 30, 2026 | Mirrors the owner-directed S0 image-review method and A–F complete / G pending image approval status; S0 tester prerequisite removed in execution authority. |
| 1.7 | September 30, 2026 | Status sync after explicit owner G approval of v0.4 at 13c2c09: S0 A–G complete; H open; I blocked on separately approved H images. Strategy/gate definitions unchanged. |
| 1.8 | September 30, 2026 | Status sync after Codex corrections: pinned v0.4 approval preserved; corrected v0.5 awaits G re-review, H paused for that revision, I/#470 blocked. |
| 1.9 | September 30, 2026 | Fixes the full-time fault report-note contradiction with three focused checks and one regenerated image. Narrows pending v0.5 review to that image; carries the other 18 from approved v0.4 apart from version text. Adds disabled Close statistics to H backlog. Original approval and accepted decisions preserved; no new approval inferred. |
| 1.10 | September 30, 2026 | Records actual owner approval of the corrected v0.5 full-time fault image at 0e8bd2b, carrying the other 18 forward from approved v0.4 apart from version text. G PASS; H OPEN; I/#470 blocked on separately approved H images. Documents-only recording preserves reviewed source, PDFs and evidence. |
| 1.11 | September 30, 2026 | H v0.1 reference ready for separate owner review, 23 image pairs; G evidence preserved and I remains blocked. |
| 1.12 | September 30, 2026 | Records separate H image approval at 4a6220c; H PASS and I OPEN for handoff, with production binding still gated on I. |
| 1.13 | October 1, 2026 | Adds current H v0.2 review-pending / I-on-hold note while preserving the dated v0.1 approval and its history row. No strategy or gate definition change. |
| 1.14 | October 1, 2026 | Supersedes the author-imposed reopening/hold note; preserves H PASS / I OPEN for approved v0.1 while v0.2 adoption awaits separate owner approval. No strategy or gate definition change. |
| 1.15 | October 1, 2026 | Records separate owner image approval of H v0.2 at a1044d5; I OPEN using v0.2, prior approvals unchanged. No strategy/gate definition change. |
| 1.16 | October 1, 2026 | Status sync for S0 Gate-I handoff draft, IN PROGRESS with identity/scale-allocation blockers. H remains approved; #470 remains blocked on completed I. Strategy/scope and gate definitions unchanged. |
| 1.17 | October 1, 2026 | PR #476 review status sync: identity, scale and dynamic UI formatting remain Gate-I allocation blockers. Strategy, scope and gate definitions unchanged. |
| 1.18 | October 3, 2026 | Records actual B/C/A owner choices and remaining contract/reference work through journey §14.10. I remains IN PROGRESS; #470 blocked. Strategy, gate definitions and prior approvals unchanged. |
| 1.19 | October 3, 2026 | Routes the October 3 contract-completion proposals to final Gate-I review; direction approval remains distinct from handoff acceptance and runtime proof. Previous approval/status history preserved; I IN PROGRESS, #470 blocked. |
| 1.20 | October 4, 2026 | Records technical Gate-I handoff PASS at e8ffd89; main release follows #478 landing. Consumed implementation and Gate-J/client/host evidence remain due. Strategy, gate definitions and prior approvals preserved. |
| 1.21 | October 5, 2026 | Records #470 foundation landing and routes the next consumed lifecycle/identity implementation; strategy, approved scope and gates unchanged. |
