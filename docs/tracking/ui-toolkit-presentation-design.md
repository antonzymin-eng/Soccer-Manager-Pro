# UI Toolkit presentation track — design supplement

Created November 20, 2026. Purpose: record owner decision D-16 (UI Toolkit for menus and management
screens) and lay out the questions, phases and governance changes needed before any migration code lands.
Version 0.4.
Status: **DRAFT.** D-16, its timing (Q1: start now, alongside PM-2) and the PM-2 fallback rules (Q5, §4.1)
are decided; nothing else here is approved. This document amends no spec. It authorizes only the T0a feasibility spike, which changes no
spec, plan or shipped screen.

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

- amend #38 or the master plan (both still name UGUI; see §5). Those amendments wait for T0a's result;
- adopt the July mock-ups as the visual target (open question Q2);
- withdraw any S0 acceptance. The accepted S0 screens stay as they are until a rebuilt screen passes
  the re-verification in Q3.

The owner also decided Q1 the same day: the migration starts now, alongside PM-2. §4.1 states what that
makes PM-2 depend on, and what happens if the track stalls.

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

## 3. Owner questions

| ID | Question | Status or recommendation |
|---|---|---|
| Q1 | Timing: start now alongside PM-2, or after PM-2 is accepted? | **DECIDED November 20, 2026 by the owner: start now, alongside PM-2.** Otherwise every season screen built in UGUI for PM-2 is built twice. Host work uses the client-only slots D-12 already allows. §4.1 records the resulting PM-2 dependency. |
| Q2 | Visual target: the July mock-ups' design system, the S0 H v0.2 look, or a new art direction? | Recommend the **July mock-ups' design system** (tokens, type, components) as the starting direction, finalized per screen through S1 Gate H image approval. **Q2 does not block T1 or PM-2** (§4.1): T1 can ship on provisional token values. |
| Q3 | How is a rebuilt screen re-verified against S0 Gate J? | Recommend a **targeted Gate J rerun**, not a visual review alone. The view models do not change, but they can be bound wrongly, so each rebuilt screen reruns the Gate J checks the renderer can break: live command routing through public command paths, focus and keyboard, pseudo-locale and maximum text scale, fallback art, supported resolutions, and the required host behaviour. Match View's mixed UGUI and UI Toolkit layout reruns all of them. Evidence for paths the rebuild does not touch (engine, session, save) is reused by citation. |
| Q4 | Fonts: the mock-ups use Barlow Condensed (display), Barlow or IBM Plex Sans (body) and JetBrains Mono. Adopt these? | Recommend adopting them. All are under the SIL Open Font License; record each font's license in the art asset register when it is added. |
| Q5 | Confirm the PM-2 fallback rules in §4.1. | **DECIDED November 20, 2026 by the owner: all three rules confirmed as written.** |

## 4. Proposed phases (not approved)

Each phase is one bounded landing with host evidence on the pinned Unity 6000.4.9f1 install, unless noted.

| Phase | Content | Exit |
|---|---|---|
| **T0a — Feasibility spike** | Runtime UI Toolkit panel alongside the existing UGUI canvas; focus and keyboard navigation across both; 200% text scale via `PanelSettings` and USS variables; font assets; how much of a UI Toolkit tree EditMode tests can build and check. Throwaway code on a branch; **no spec, plan or shipped-screen change.** | Host receipt for each item and a written **go/no-go** on keyboard focus parity with the current screens. |
| **T0b — Binding amendments** (only after a T0a go) | Amend #38 and supersede master plan §3.4 (§5). | Owner approval of the amendment text. |
| **T1 — Tokens and components** | USS tokens; core components: button, tab, chip, card, data table, stat tile, attribute bar, dialog. Contrast evidence for every text/background pair at 100% and 200%. If Q2 is still open, tokens carry provisional values (§4.1). | A component gallery scene captured on the host; contrast table recorded. |
| **T2 — Front door** | Main Menu, New Career, Load Career rebuilt; the UGUI versions removed. | Q3 targeted Gate J rerun; owner image approval. |
| **T3 — Career Home and season screens** | Career Home (fixtures, calendar, table, results) and the PM-2 season screens still to come, built once in UI Toolkit. | Q3 targeted Gate J rerun; PM-2 career-lane acceptance runs on these screens. |
| **T4 — Match-day overlay** | S0 pre-match screens and the Match View side rail in UI Toolkit; the pitch stays UGUI. | Q3 full Gate J rerun for the mixed-renderer Match View. |
| **T5 — Motion and polish** | Transitions, hover and press feedback, loading states, art from `docs/design/art/`. | Owner play session. |

### 4.1 PM-2 dependency and fallbacks

Because the owner chose to start now (Q1), roadmap career-lane item 4 makes season screens not yet
built wait for T1. **T0a, T0b and T1 are therefore on the PM-2 path.** Screens already merged, or in an
open PR, are not held back; T3 rebuilds them.

To keep that dependency from stalling the career lane, the owner confirmed three rules on November 20, 2026 (Q5):

1. **Q2 never blocks PM-2.** Components read every color, font and spacing value from USS variables.
   If Q2 is undecided when T1 starts, T1 uses the current H v0.2 values as provisional tokens and
   passes contrast on those. Deciding Q2 later re-values the variables; it does not rebuild screens.
2. **A T0a no-go releases the career lane at once.** Unbuilt season screens resume in UGUI with no
   further wait, and D-16 returns to the owner. The fallback this document recommends then is UGUI
   with TextMeshPro, sprites and a shared component library (§6 risk 1).
3. **T0a is timeboxed.** If T0a has not produced a go/no-go after two host sessions, the career lane
   proceeds in UGUI as under rule 2, while T0a continues off the PM-2 path. The two-session figure is
   a planning limit the owner accepted, not a measured estimate.

## 5. Governance changes this track needs

Both binding changes below land in **T0b, only after a T0a go**. A T0a no-go leaves them unmade.

- **#38 UI / Client Framework.** FR-UI-021 says "The UGUI rendering binding MUST be Unity-host-gated."
  §1, §4.3, §5, §7.2 and §8 say UGUI in the same sense. The intent (the rendering binding is host-gated
  and outside the pure substrate) still holds. Only the toolkit name changes, to "the Unity rendering
  binding (UI Toolkit for screens; UGUI for the match pitch)". Read every file in the spec folder first
  and add version-history rows to each file touched.
- **Master development plan §3.4.** It says "UI Framework: Unity UGUI (simpler than UI Toolkit for
  now)". The base text is preserved verbatim by convention, so supersede it with a new master-plan
  amendment file, following the pattern of
  [`master-plan-amendment-01-audio-multiplayer-transport.md`](../planning/master-plan-amendment-01-audio-multiplayer-transport.md),
  and add it to the plan's **Amendments** header line.
- **`ux-shared-system.md` F3-003** (which owner applies theme, palette and fonts) is answered by T1's
  token sheet. Record the answer there.
- **`ux-detailed-plan.md`.** S1 Gate H produces UI Toolkit screen images, not UGUI ones, and rebuilt
  S0 screens are re-verified under Q3. Add a pointer to this document; the gate definitions do not
  change.
- **`interactive-unity-client-design.md` §5-P7.** Add a note that later P7 screens bind through
  UI Toolkit from T2 onward.
- **Packages.** UI Toolkit ships as a built-in Unity module. `com.unity.ugui` 2.0.0 (which now
  contains TextMeshPro) stays for the pitch.

## 6. Risks

1. **Keyboard focus parity.** S0 Gate J's acceptance relied on UGUI's per-control navigation.
   UI Toolkit's focus model is less granular. T0a's go/no-go exists for this risk. If parity costs too
   much, the fallback is UGUI with TextMeshPro, sprites and a component library, which is slower but
   keeps the current focus code.
2. **Host-only verification.** Like the current UGUI code, UI Toolkit adapters are not compiled by the
   Linux gate. Keep adapters thin and put logic in tested plain C#.
3. **PM-2 delay.** T0a, T0b and T1 now sit on the PM-2 path. §4.1's confirmed rules bound the delay.
4. **Double work during the transition.** UGUI screens already merged are rebuilt in T3.
5. **Density against accessibility.** The mock-ups use 12 px tables. At 200% text these must reflow
   (wrap, stack or page), not clip. T1's data table component must show this before T3 depends on it.

## Version History

| Version | Date | Change |
|---|---|---|
| 0.4 | November 20, 2026 | Owner confirmed the three §4.1 PM-2 fallback rules as written (Q5). Status, §3, §4.1 and §6 updated. |
| 0.3 | November 20, 2026 | PR #560 review: §4.1 records the PM-2 dependency and proposed fallback rules (Q5); T0 split into a spec-free T0a spike and a conditional T0b; master plan §3.4 added to §5; Q3 changed to a targeted Gate J rerun; risk 3 added. |
| 0.2 | November 20, 2026 | Owner answered Q1: start now, alongside PM-2. Status, §1, §3 and §4 updated. |
| 0.1 | November 20, 2026 | Created. Records owner decision D-16; lists open questions Q1–Q4, proposed phases T0–T5, governance changes and risks. Draft; nothing beyond D-16 approved. |
