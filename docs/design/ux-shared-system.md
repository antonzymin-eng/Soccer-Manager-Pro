# System XI — Shared S0/S1 UX System

**Created:** September 7, 2026  
**Last Updated:** September 7, 2026  
**Version:** 0.1  
**Status:** F3 CANDIDATE — contract/state audit complete; adversarial pass pending  
**Parent execution authority:** [`ux-detailed-plan.md`](ux-detailed-plan.md)  
**Experience architecture:** [`ux-experience-architecture.md`](ux-experience-architecture.md) v0.1  
**Evidence parent:** [`ux-baseline-evidence.md`](ux-baseline-evidence.md) v0.3

---

## 0. Purpose and authority

This is UX-B: the smallest shared interaction system required by S0/S1. It defines reusable presentation behavior,
not a speculative component library and not a runtime API.

Authority remains:

1. APPROVED specification;
2. verified production/client behavior;
3. F1 evidence and F2 current/target architecture;
4. this shared UX contract;
5. visual mockups.

Nothing here creates a `ScreenId`, navigation edge, `ManagerIntent`, command seam, view model, settings schema, locale,
audio cue, asset contract, or save behavior. Where a required implementation surface does not exist, the rule is
labelled `TARGET` or `FUTURE-BLOCKED` rather than presented as current behavior.

The `touchline` visual direction remains the reference style: dense analyst tool, quiet chrome, tabular numbers and
restrained accent. Token values in the HTML references are not shipping constants.

---

# 1. Cross-cutting invariants

These rules apply to every S0/S1 primitive.

1. **Presentation does not compute domain truth.** Labels, status, legality, analytics and urgency come from owning
   projections or explicit presentation state; UI does not invent football logic.
2. **Mutation uses typed owning commands only.** A control is not admitted as live unless its action maps to a verified
   public seam through the client dispatcher/composition path.
3. **Navigation is named and typed.** Buttons do not poke the shell directly or rely on incidental stack behavior.
4. **Color is supplementary.** Meaning needed to act must survive grayscale/color-vision changes through text, icon,
   shape, position or another non-color carrier.
5. **Keyboard and pointer are peers.** Every critical/high-frequency S0 action is reachable, understandable and
   executable without a mouse; pointer-only hover is never the sole information path.
6. **Focus is visible and recoverable.** Focus does not disappear after transitions, list updates, modal/drawer close,
   disabled-state changes or failed commands.
7. **Localization is a layout input, not polish.** Components must tolerate pseudo-locale expansion, supported maximum
   text scale and glyph fallback without hiding critical actions or data.
8. **Missing presentation assets degrade safely.** Missing art must not fabricate identity, alter domain meaning or
   block a task that can otherwise complete.
9. **Transient feedback never carries durable recovery information.** Toasts acknowledge; persistent inline/banner
   states explain blockers, failures and required recovery.
10. **Future capability is omitted, not cosmetically simulated.** `FUTURE-BLOCKED`, `SPEC-ONLY`, `UNKNOWN` and
    out-of-release capabilities do not appear as normal permanently-disabled production controls.

---

# 2. Action hierarchy and states

## 2.1 Action classes

| Class | Use | S0 examples | Constraint |
|---|---|---|---|
| **Primary** | the task's next dominant commit/progression action | Start Match; close report/continue through the current legal return | normally one visually dominant primary action per task region |
| **Secondary** | valid supporting or reversible action | Cancel setup; open detail; change selection | must not compete visually with the primary |
| **Destructive** | irreversible or context-replacing action with material loss risk | future load-over-current-context; future quit with unsaved work if such state exists | requires owning lifecycle evidence before confirmation rules can be finalized |
| **Quiet/tertiary** | low-risk utility or disclosure | contextual help; close drawer | never used to hide a critical action |

Visual prominence does not establish legality. Enabled/disabled state comes from the owning client/domain state.

## 2.2 Shared action state matrix

Every admitted action supports the applicable states below.

| State | Required behavior |
|---|---|
| Default | label states the action, not merely an icon for critical actions |
| Hover | optional visual affordance only; no hover-exclusive critical information |
| Focus | unmistakable focus indicator independent of hover/accent fill |
| Pressed/active | immediate local acknowledgement without predicting command success |
| Disabled | remains non-invokable; critical/high-frequency actions expose why when the reason helps the player recover |
| In progress | suppress duplicate invocation when owning lifecycle exposes unresolved work; preserve context |
| Success | reflect new projected state; optional toast only for non-blocking acknowledgement |
| Failure/refusal | keep the player in a recoverable context and surface owning reason persistently where action is required |

A control may not invent an `in progress`, success or failure lifecycle that the owning client does not expose. Until
such lifecycle exists, that behavior remains `TARGET`.

## 2.3 Destructive confirmation

Confirmation is required only when the player could otherwise cause material, non-obvious loss and there is no safer
undo/recovery. Confirmation must state the consequence in plain language and name the destructive action explicitly.
Do not add confirmation to routine reversible actions merely because they are important.

---

# 3. Navigation, tabs and focus

F2 owns page/modal/drawer choice. F3 owns the shared interaction behavior inside those choices.

## 3.1 Top-level navigation

- Only live/admitted destinations appear in shipping navigation.
- Current PM-1 screens retain the current five named moves; F3 adds no back/abandon path.
- Target career navigation is visually distinguishable in design evidence but is not represented as a current edge.
- Current location has text/structural indication, not accent color alone.
- A nav item that is not actionable is not put into the normal tab order.

## 3.2 Tabs/subnavigation

Tabs are alternate views of one owning context, per F2. Required states:

- default;
- focus;
- selected/current;
- disabled only where a real live view is temporarily unavailable.

Keyboard behavior:

- focus can enter and leave the tab set without trapping;
- changing selected tab never silently commits a domain mutation;
- focus order remains logical if labels expand/reflow;
- selected state has text/shape/position semantics in addition to color.

## 3.3 Focus policy

On entry to a page, focus goes to the first meaningful task target, not decorative chrome. On state-changing refresh:

1. preserve focus on the same logical item where it still exists;
2. if that item disappears, move to the nearest meaningful surviving target in the same task region;
3. if the region disappears, move to the page's next safe task anchor;
4. never dump focus invisibly to the application root.

Closing a modal/drawer restores focus to its invoker unless that invoker no longer exists, in which case the same
fallback rule applies.

`Escape` closes only the interaction layer whose contract permits cancellation. It does not imply browser-style Back
and must never create an abandon-match path that is absent from `ClientScreenFlow`.

No custom keyboard shortcut may shadow text entry, operating-system conventions or another S0 critical action. F3
pins behavior classes, not final key bindings; journey packets record actual bindings at Gate C/D.

---

# 4. Dense data tables and list selection

Dense tables are a core `touchline` pattern, but density never takes priority over scanability, focus visibility or
critical information.

## 4.1 Table semantics

Every S0/S1 table/list must identify:

- stable row identity supplied by the owning projection;
- primary identifying column;
- which columns are sortable/filterable;
- current sort/filter state;
- selected row when selection exists;
- empty/loading/partial/stale/error behavior;
- which columns are essential at the smallest supported desktop.

The UI must not synthesize a stable player/match identity from display text such as a name.

## 4.2 Sorting

- sort affordance is keyboard reachable;
- current sort key and direction are visible without color alone;
- activating the same sort toggles direction only if the owning interaction design declares both directions useful;
- sorting does not change domain state;
- selection follows the same logical row after sort if that row still exists.

## 4.3 Filtering/search

- active filters remain visible while applied;
- zero-result state distinguishes “no data exists” from “filters hide all results”;
- clearing filters is a visible/reachable recovery action when filters produce zero results;
- filtering does not silently reset unrelated task state unless the journey explicitly requires it.

## 4.4 Selection/detail

Selected-row state must survive color-independent viewing. If a drawer/detail rail follows selection, keyboard focus and
selection are related but not conflated: moving focus does not automatically trigger a domain mutation.

## 4.5 Responsive column behavior

The HTML guardrail is a reference, not a renderer contract. F3 adopts three validation bands for S0/S1:

- **small desktop:** 1366-wide reference case from the existing guardrail audit;
- **reference:** 1920×1080;
- **expanded:** 2560-wide/high-resolution or ultrawide behavior.

At smaller width, non-essential columns may fold behind a detail disclosure, but the row identity, task-critical state
and primary action must remain visible. At wider width, the UI may reveal useful columns or whitespace; it must not
stretch reading lines and controls simply to consume space.

The exact shipping minimum resolution remains a product/implementation decision. `1366` is an F3 validation case, not
a platform-support promise.

---

# 5. Inputs, selectors and toggles

## 5.1 Shared rules

- Every input has a persistent label or an equally durable programmatic/visible association; placeholder text is not a
  label.
- Current value is distinguishable from help/explanation text.
- Validation errors identify the affected field and recovery, not merely “invalid”.
- Changing a selector/toggle does not imply immediate domain commit unless its owning command contract explicitly does
  so.
- Controls that stage a multi-field configuration make the commit/cancel boundary obvious.

## 5.2 Selectors

Selectors used for tactics/options must tolerate long localized values and keyboard navigation. Truncation may occur in
a compact closed control only if the full selected value is available on focus/inspection and ambiguity cannot affect
the decision.

## 5.3 Toggles

A toggle is used only for a binary state with an unambiguous positive/negative meaning. State must be visible through
label/text/shape, not switch color alone. Destructive or multi-state choices are not represented as toggles.

---

# 6. Tooltips and contextual help

F2's help priority remains binding: clear label/state → inline reason → tooltip/popover → dedicated reference.

Shared tooltip/popover rules:

- never carry the only critical instruction, blocker reason or recovery action;
- available from keyboard focus as well as pointer hover;
- dismissal does not move or lose task focus;
- no football causality/recommendation claim is invented by UX;
- may define unfamiliar football/management terminology using approved/static copy, but may not imply simulation depth
  the owning model does not provide;
- content reflows for pseudo-locale/max text scale rather than overflowing a fixed pixel box.

---

# 7. Modal and drawer behavior

## 7.1 Modal

A modal intentionally traps focus while open. Required behavior:

- initial focus lands on the safest meaningful action or first required field, not automatically on a destructive
  button;
- Tab/Shift-Tab cycles inside the modal;
- Escape/Cancel exists unless the owning operation is genuinely non-cancellable;
- close restores parent focus;
- background controls cannot receive input;
- critical title/consequence remains readable under pseudo-locale/max text scale;
- a progress modal is used only when the owning lifecycle provides progress/blocking semantics.

## 7.2 Drawer/detail rail

A drawer preserves the parent task. It may contain inspection or secondary editing but cannot hide the page's required
primary action or blocker. Focus may enter the drawer and must have a deterministic path back to the page. A drawer does
not create shell navigation history by itself.

---

# 8. Feedback and asynchronous/partial states

## 8.1 Feedback channels

| Pattern | Durable? | Use |
|---|---:|---|
| Inline | yes while condition exists | field/row/action-specific state, refusal, validation |
| Banner | yes while condition exists | page-level blocker/high-priority warning |
| Toast | no | non-blocking acknowledgement after completed action |
| Badge | summary only | count/attention on a live destination; explanation lives inside destination |

## 8.2 Loading

Loading is admitted only when the client can distinguish “not yet available” from empty/failure. Preserve page geometry
where practical; do not flash false zeros/empty tables while data is unresolved.

For Match View, #38's current failure contract is specific: before a first streamer frame exists, render the documented
last-known/empty frame behavior and never advance the simulation to force a frame.

## 8.3 Empty

Empty state says what is absent and, where actionable, what the player can do next. “No save exists” and “filter
returned zero rows” are distinct states.

## 8.4 Partial

A partial state is used only if the owning projection explicitly supports partial availability. Missing fields must not
be silently replaced with invented zeros, ratings or recommendations.

## 8.5 Stale

A stale indicator is used only if the owning client/projection exposes freshness or a known transition creates stale
context. UX does not infer staleness from wall-clock time on its own. When load/context replacement completes, F2's rule
applies: pre-load career state may not remain presented as current.

## 8.6 Error/refusal

- recoverable error preserves useful entered/selected context unless unsafe;
- refusal reason supplied by an owner is shown in plain player-facing language/localized copy;
- failure requiring player action is persistent, never toast-only;
- retries are offered only where the owning command/lifecycle permits them;
- internal exception/spec/assembly terminology is not player-facing copy.

---

# 9. Localization, text scale and glyph resilience

## 9.1 Ownership

Current status is split deliberately:

- #49 owns localization/a11y option **values/contracts**;
- #38 rendering/theme owns applying text scale, contrast/colorblind presentation, reflow and font/glyph fallback;
- the content-tier implementation is not currently a live S0/S1 runtime surface, so settings UI remains
  `FUTURE-BLOCKED` until its owning client implementation lands.

F3 therefore defines the layout expectation without inventing option values or a settings store.

## 9.2 Pseudo-locale gate

Every S0 journey must be exercised with the #49 pseudo-locale behavior once available. Until runtime #49 exists, the
prototype/reference test substitutes equivalent expanded/bracketed content and labels that evidence as prototype-side,
not production localization proof.

Required outcomes:

- critical labels/actions remain visible and distinguishable;
- layouts reflow rather than overlap;
- text does not cover score/time, selection state or required controls;
- tables preserve critical columns and disclose folded detail;
- modal/drawer content remains operable;
- catalogue-path text can be distinguished from pure data in the eventual pseudo-locale contract.

## 9.3 Maximum text scale

The maximum supported text-scale value is owned outside UX and is not invented here. F3 requires all S0 shared
primitives to pass at the maximum value the shipping #49/#38 contract eventually exposes. Prototype validation uses the
largest agreed test scale available at that stage and records the value tested.

No component may satisfy scaling by clipping a critical label or making the primary action unreachable.

## 9.4 Glyph fallback

Shipping fonts/fallback chains are #38 theme assets, not the Google Fonts imported by HTML mockups. Missing-glyph
behavior must not substitute blank space for a critical label. F3 assumes a verified fallback chain will be required
before a real locale is offered; the HTML font stack is reference-only.

---

# 10. Contrast, colorblind and color-independent semantics

The theme owns actual palettes. F3 owns semantic resilience.

For every critical/high state that currently uses color (positive/negative, selected, warning, role/position, form,
attribute band, result):

- add text, icon/shape, sign, position or pattern so the distinction remains usable without hue;
- focus state remains distinct from selection, warning and positive/negative state;
- disabled state is not conveyed only by lower opacity when that would make text unreadable;
- charts/heatmaps used later require a non-color reading path (labels, legend values, patterns or equivalent), but F3
  does not invent #37 chart behavior not required by S0.

The current mockup colors are references, not proof of contrast compliance.

---

# 11. Art and identity fallbacks

F3 defines four required fallback classes because S0/S1 must remain usable before the production art pipeline is
complete.

| Slot | Fallback rule |
|---|---|
| Club badge/crest | neutral geometric/initial-safe placeholder supplied by presentation; never borrow another club's identity |
| Player portrait | neutral silhouette/identity placeholder; name/role remains the primary identity |
| Stadium/background | neutral environmental field/texture; no invented venue detail |
| Key art/menu hero | restrained branded/system background; primary navigation remains fully readable without art |

Rules:

1. missing art never removes the text identity required to make a decision;
2. fallback occupies a stable layout slot to avoid large reflow when real art loads;
3. no status/ability/injury/morale meaning is encoded only in portrait treatment;
4. asset failure is presentation degradation, not a reason to fabricate data;
5. alt/accessibility semantics describe the useful identity where the runtime surface supports them; decorative art is
   not repeated as noise.

Exact asset formats, loading, provenance and production pipeline remain owned by the art/client workstreams.

---

# 12. Audio, captions and HUD coexistence

#51 runtime is currently `FUTURE-BLOCKED / SPEC-ONLY` for the UX evidence baseline, so this section records an S0/S1
future constraint rather than claiming live audio behavior.

When #51 is admitted:

- muted audio is a valid normal state; no task may depend on hearing a cue;
- an informational cue's required caption decision must have a presentation region that does not cover score/time,
  critical match controls, primary alerts or the current focus target;
- caption and commentary text must remain readable at maximum text scale and pseudo-locale expansion;
- captions are not duplicated into multiple competing HUD regions;
- a `NoCaption` ambience cue does not create a UX error by itself;
- audio settings are not persisted by UX; #51 contributes its schema fragment and the client settings owner persists it.

Until runtime audio/captions exist, prototypes may reserve/test the region but must label it `FUTURE-BLOCKED`.

---

# 13. Desktop layout behavior

## 13.1 Validation bands

F3 uses three design-validation bands, not platform promises:

| Band | Reference | Required behavior |
|---|---|---|
| Small | 1366-wide desktop case | preserve critical task, identity, state and primary action; fold secondary columns/details |
| Reference | 1920×1080 | baseline density/hierarchy from the existing mockup stage |
| Expanded | 2560-wide/high-resolution/ultrawide case | reveal useful secondary information or increase safe breathing room; do not simply stretch controls/text |

Vertical constraints must also be exercised: a primary action cannot fall below an unreachable fixed panel because text
or status content expands.

## 13.2 Reflow priority

When space becomes constrained:

1. preserve task identity/context;
2. preserve blocker/current state;
3. preserve primary action;
4. preserve primary identifiers and decision-critical values;
5. fold contextual detail into drawer/disclosure;
6. drop decorative art/chrome before decision information.

Horizontal scrolling is acceptable for genuinely wide analytical tables only when the frozen/anchored identity and
current selection remain understandable. It is not the default response to every narrow layout.

---

# 14. S0 primitive admission matrix

This matrix is the F3 exit checklist for primitives required by the current four-screen PM-1 journey.

| Primitive | Needed S0? | State rules | Focus/keyboard | Localization/a11y | Fallback | Status |
|---|---:|---:|---:|---:|---:|---|
| Primary/secondary action | yes | §2 | §3 | §§9–10 | n/a | DEFINED |
| Destructive action/confirmation | conditional | §2.3 | §§2–3 | §§9–10 | n/a | DEFINED; live use requires owning lifecycle |
| Navigation/current location | yes | §3.1 | §3 | §§9–10 | n/a | DEFINED; current graph remains authority |
| Tabs/subnav | conditional | §3.2 | §3.2 | §§9–10 | n/a | DEFINED; no speculative career chrome |
| Dense table/list | yes for report/setup/data views | §4 | §4 | §§9–10,13 | empty/partial §8 | DEFINED |
| Sort/filter | where admitted | §4 | §4 | §§9–10 | zero-result §8 | DEFINED |
| Selection/detail rail | yes where row inspection is needed | §§4,7 | §§3–4,7 | §§9–10 | art §11 where used | DEFINED |
| Input/selector/toggle | yes for tactics/setup | §5 | §§3,5 | §§9–10 | n/a | DEFINED |
| Tooltip/context help | yes | §6 | §6 | §9 | n/a | DEFINED |
| Modal/confirmation | conditional | §7 | §7 | §§9–10,13 | n/a | DEFINED |
| Toast/banner/inline | yes | §8 | §3 where interactive | §§9–10 | n/a | DEFINED |
| Loading/empty/partial/stale/error | yes as applicable | §8 | §3 | §§9–10 | art §11 | DEFINED with owner-evidence constraints |
| Keyboard traversal/no trap | yes | n/a | §3 | §9 | n/a | DEFINED |
| Pseudo-locale/reflow | yes | n/a | n/a | §9 | n/a | DEFINED; runtime proof future until #49 lands |
| Text scale | yes | n/a | n/a | §9.3 | n/a | DEFINED without inventing numeric value |
| Contrast/color-independent semantics | yes | n/a | focus §3 | §10 | n/a | DEFINED |
| Glyph fallback | yes | n/a | n/a | §9.4 | system font chain future | DEFINED as #38 theme obligation |
| Badge/portrait/stadium/key-art fallback | yes where slot exists | n/a | n/a | §11 | §11 | DEFINED |
| Caption/HUD coexistence | future S0 dependency | §12 | n/a | §§9,12 | muted audio valid | DEFINED / FUTURE-BLOCKED runtime |
| Desktop layout bands | yes | §13 | n/a | §§9–10 | §11 | DEFINED |

---

# 15. Findings and implementation dependencies

**F3-001 — mockup tokens are references, not compliance evidence.** Existing color/type/spacing tokens remain useful
for `touchline`, but contrast, text-scale and pseudo-locale behavior need explicit Gate E proof.

**F3-002 — keyboard/focus semantics are UX requirements not currently supplied by #38's pure navigation substrate.**
They belong in the rendering/binding/journey implementation and must be verified at Gate J; F3 does not alter
`NavigationShell` to encode focus.

**F3-003 — accessibility ownership is split correctly but runtime application is still absent.** #49 owns values; #38
must apply scale/reflow/theme/glyph behavior. Settings UI remains future until that client path exists.

**F3-004 — missing art must be deliberately testable.** S0 cannot wait for production badges/portraits/stadium/key art;
journey packets need missing-art states as normal Gate E fixtures.

**F3-005 — audio/caption coexistence is a design constraint without a live runtime consumer.** Reserve/test the space in
S0 prototypes where relevant, but do not present audio settings/captions as live until #51 composition exists.

**F3-006 — `1366 / 1920 / 2560` are validation cases, not a shipping resolution declaration.** Platform minimum support
must be decided and verified elsewhere; UX uses these cases to prevent a 1920-only design.

**F3-007 — “disabled” and “future” are different states.** A real temporarily-illegal action may be disabled with a
reason; an unimplemented module/action is omitted. This prevents the management mockups from turning the EA shell into
a wall of fake controls.

---

# 16. F3 exit assessment

Against `ux-detailed-plan.md` F3 exit:

- shared S0 action/state behavior: defined;
- nav/tab/focus behavior: defined without changing current graph;
- tables/sort/filter/selection: defined;
- inputs/selectors/toggles: defined;
- tooltips/help: defined;
- modal/confirmation: defined;
- toast/banner/inline feedback: defined;
- loading/empty/partial/stale/error: defined with owner-evidence limits;
- keyboard traversal/no-trap: defined;
- pseudo-locale/reflow/max text scale: defined without inventing #49 values;
- contrast/colorblind/color-independent semantics: defined;
- font/glyph fallback: assigned to #38 theme/application, not HTML fonts;
- missing art fallbacks: defined;
- caption/HUD/muted-audio behavior: defined and labelled future where runtime is absent;
- small/reference/expanded desktop behavior: defined as validation bands.

**Candidate conclusion:** F3 content is complete. It must receive one adversarial consistency pass against F1/F2 and
#38/#49/#51 before its status changes to `F3 COMPLETE — F4 NEXT`.

#region VersionHistory
| Version | Date | Change |
|---|---|---|
| 0.1 | 2026-09-07 | Initial UX-B shared system. Bounded to S0/S1 primitives required by the detailed plan; reconciled with F2 interaction architecture, #38 presentation-only contracts, #49 localization/a11y ownership, #51 caption/audio constraints and existing desktop/mockup references. |
#endregion
