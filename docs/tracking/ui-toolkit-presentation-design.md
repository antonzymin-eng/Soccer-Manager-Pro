# UI Toolkit presentation track — design supplement

Created November 20, 2026. Purpose: record owner decision D-16 (UI Toolkit for menus and management
screens) and lay out the questions, phases and governance changes needed before any migration code lands.
Version 0.2.
Status: **DRAFT.** D-16 and its timing (Q1: start now, alongside PM-2) are decided; nothing else here is
approved. This document amends no spec and authorizes no implementation beyond starting T0.

Decision register: [roadmap §5.1, D-16](path-to-playable-roadmap.md) ·
Client plan: [interactive Unity client](interactive-unity-client-design.md) ·
UI contract: [#38 UI / Client Framework](../specs/ui-client-framework/) ·
UX authority: [`ux-detailed-plan.md`](../design/ux-detailed-plan.md) ·
Visual references: [`ui-mockups/`](../design/ui-mockups/README.md), [S0 H v0.2](../design/s0-high-fidelity/README.md)

## 1. Owner decision D-16 — November 20, 2026

Anton Zymin decided:

- **UI Toolkit** (UXML layout, USS style sheets) becomes the rendering technology for menus and
  management screens: Main Menu, New Career, Load Career, Career Home, the season screens and all later
  management screens.
- **UGUI stays** for the match pitch and effects-heavy match-day pieces. Both systems run in the same
  scene.

Reasons, as discussed:

1. The owner judged the current screens well below modern-game quality. They use legacy
   `UnityEngine.UI.Text` with one font and flat code-built rectangles, and have no art or motion.
2. A management game is mostly tables, forms and menus. UI Toolkit has style sheets, flexbox reflow,
   virtualized `ListView`/`MultiColumnListView` and runtime data binding for exactly that kind of
   screen.
3. The July mock-ups are HTML/CSS. Their tokens (`assets/tokens.css`) and component styles map closely
   to USS variables and selectors.
4. UGUI keeps its advantages (custom materials, particles, precise per-control navigation) where they
   matter most: on the pitch.

D-16 does **not** by itself:

- amend #38 (FR-UI-021 still names UGUI; see §5);
- schedule the migration against PM-2 (Q1, decided the same day: start now; see §3);
- adopt the July mock-ups as the visual target (open question Q2);
- withdraw any S0 acceptance. The accepted S0 screens stay as they are until a rebuilt screen is
  accepted in their place.

## 2. What does not change

- **View models and coordinators.** `ClientApp`, `UiFramework`, `MatchClientCore` and the career
  coordinator are renderer-independent (#38 §4, roadmap §7). UI Toolkit binds the same view models
  that UGUI binds today. Only the rendering leaf is rebuilt.
- **The roadmap §7 rule.** No decision logic goes in a Unity-side adapter. Everything that decides what
  to show, or what an input means, stays in gate-compiled plain C#.
- **Accessibility requirements.** Full keyboard operation, 200% text scale with reflow, and the contrast
  evidence rules (`ux-shared-system.md` F3-001) apply unchanged. A new look does not lower them.
- **Determinism.** Presentation reads view models only. Nothing in this track enters a snapshot,
  a digest or a save.

## 3. Open owner questions

| ID | Question | Recommendation |
|---|---|---|
| Q1 | Timing: start now alongside PM-2, or after PM-2 is accepted? | **DECIDED November 20, 2026 by the owner: start now, alongside PM-2.** Otherwise every season screen built in UGUI for PM-2 is built twice. Host work uses the client-only slots D-12 already allows. |
| Q2 | Visual target: the July mock-ups' design system, the S0 H v0.2 look, or a new art direction? | Use the **July mock-ups' design system** (tokens, type, components) as the starting direction, then finalize it per screen through S1 Gate H image approval. |
| Q3 | Rebuilt S0 match screens: re-accept them through a short visual review, or rerun S0 Gate J in full? | Short visual and keyboard review per rebuilt screen. Gate J's behaviour evidence does not change, because the view models do not. |
| Q4 | Fonts: the mock-ups use Barlow Condensed (display), Barlow or IBM Plex Sans (body) and JetBrains Mono. Adopt these? | Adopt them. All are under the SIL Open Font License; record each font's license in the art asset register when it is added. |

## 4. Proposed phases (not approved)

Each phase is one bounded landing with host evidence on the pinned Unity 6000.4.9f1 install, unless noted.

| Phase | Content | Exit |
|---|---|---|
| **T0 — Foundation spike** | Runtime UI Toolkit panel alongside the existing UGUI canvas; focus and keyboard navigation across both; 200% text scale via `PanelSettings` and USS variables; font assets; how much of a UI Toolkit tree EditMode tests can build and check. Land the #38 amendment (§5). | Host receipt for each item. A written go/no-go on keyboard focus parity with the current screens. |
| **T1 — Tokens and components** | USS tokens from the Q2 target; core components: button, tab, chip, card, data table, stat tile, attribute bar, dialog. Contrast evidence for every text/background pair at 100% and 200%. | A component gallery scene captured on the host; contrast table recorded. |
| **T2 — Front door** | Main Menu, New Career, Load Career rebuilt; the UGUI versions removed. | Keyboard and 200% acceptance as P7b's; owner image approval. |
| **T3 — Career Home and season screens** | Career Home (fixtures, calendar, table, results) and the PM-2 season screens still to come, built once in UI Toolkit. | PM-2 career-lane acceptance runs on these screens. |
| **T4 — Match-day overlay** | S0 pre-match screens and the Match View side rail in UI Toolkit; the pitch stays UGUI. | Q3 re-acceptance. |
| **T5 — Motion and polish** | Transitions, hover and press feedback, loading states, art from `docs/design/art/`. | Owner play session. |

Q1 is decided (start now): T0–T2 run alongside PM-2 career-lane item 4 (season screens). Season screens
not yet built wait for T1's components rather than landing in UGUI first. Work already merged or in an
open PR (for example the Career Home standings paging) is not held back; T3 rebuilds it.

## 5. Governance changes this track needs

- **#38 UI / Client Framework.** FR-UI-021 says "The UGUI rendering binding MUST be Unity-host-gated."
  §1, §4.3, §5, §7.2 and §8 say UGUI in the same sense. The intent (the rendering binding is host-gated
  and outside the pure substrate) still holds. Only the toolkit name changes, to "the Unity rendering
  binding (UI Toolkit for screens; UGUI for the match pitch)". Land this in T0 after reading every file
  in the spec folder, with version-history rows in each file touched.
- **`ux-shared-system.md` F3-003** (which owner applies theme, palette and fonts) is answered by T1's
  token sheet. Record the answer there.
- **`ux-detailed-plan.md`.** S1 Gate H produces UI Toolkit screen images, not UGUI ones. Add a
  pointer to this document; the gate definitions do not change.
- **`interactive-unity-client-design.md` §5-P7.** Add a note that later P7 screens bind through
  UI Toolkit from T2 onward.
- **Packages.** UI Toolkit ships as a built-in Unity module. `com.unity.ugui` 2.0.0 (which now
  contains TextMeshPro) stays for the pitch.

## 6. Risks

1. **Keyboard focus parity.** S0 Gate J's acceptance relied on UGUI's per-control navigation.
   UI Toolkit's focus model is less granular. T0's go/no-go exists for this risk. If parity costs too
   much, the fallback is UGUI with TextMeshPro, sprites and a component library, which is slower but
   keeps the current focus code.
2. **Host-only verification.** Like the current UGUI code, UI Toolkit adapters are not compiled by the
   Linux gate. Keep adapters thin and put logic in tested plain C#.
3. **Double work during the transition.** Any UGUI screen landed after D-16 is rebuilt later. Q1
   decides how much of that to accept.
4. **Density against accessibility.** The mock-ups use 12 px tables. At 200% text these must reflow
   (wrap, stack or page), not clip. T1's data table component must show this before T3 depends on it.

## Version History

| Version | Date | Change |
|---|---|---|
| 0.2 | November 20, 2026 | Owner answered Q1: start now, alongside PM-2. Status, §1, §3 and §4 updated. |
| 0.1 | November 20, 2026 | Created. Records owner decision D-16; lists open questions Q1–Q4, proposed phases T0–T5, governance changes and risks. Draft; nothing beyond D-16 approved. |
