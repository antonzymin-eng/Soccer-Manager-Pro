# System XI — Shared S0/S1 UX System

**Created:** September 7, 2026  
**Last Updated:** September 11, 2026  
**Version:** 0.4  
**Status:** F3 COMPLETE — current workstream phase is defined by [`ux-detailed-plan.md`](ux-detailed-plan.md)  
**Parent execution authority:** [`ux-detailed-plan.md`](ux-detailed-plan.md)  
**Experience architecture:** [`ux-experience-architecture.md`](ux-experience-architecture.md) v0.1  
**Evidence parent:** [`ux-baseline-evidence.md`](ux-baseline-evidence.md) v0.3

---

## 0. Purpose and authority

This is UX-B: the smallest shared interaction system required by S0/S1. It defines reusable presentation behavior,
not a speculative component library and not a runtime API.

Authority remains: APPROVED specification → verified production/client behavior → F1/F2 UX evidence → this shared
UX contract → visual mockups.

Nothing here creates a `ScreenId`, navigation edge, `ManagerIntent`, command seam, view model, settings schema, locale,
audio cue, asset contract, or save behavior. Missing implementation remains `TARGET`/`FUTURE-BLOCKED`.

The `touchline` direction remains the visual reference: dense analyst tool, quiet chrome, tabular numbers and restrained
accent. HTML token values are not shipping constants.

---

# 1. Cross-cutting invariants

1. **Presentation does not compute domain truth.** Legality, status, analytics and urgency come from owning projections
   or explicit presentation state.
2. **Mutation uses typed owning commands only.** A live control needs a verified public seam through the client
   dispatcher/composition path.
3. **Navigation is named and typed.** UI does not poke the shell or rely on incidental stack behavior.
4. **Color is supplementary.** Required meaning survives without hue.
5. **Keyboard and pointer are peers.** Critical/high-frequency S0 actions cannot require hover or a mouse.
6. **Focus is visible and recoverable** after transitions, refresh, modal/drawer close and failure.
7. **Localization is a layout input.** Expanded text, maximum supported text scale and glyph fallback must not hide
   critical actions/data.
8. **Missing art degrades safely.** It does not fabricate identity or block an otherwise-valid task.
9. **Transient feedback is not durable recovery.** Blockers/failures stay visible until resolved.
10. **Future capability is omitted, not cosmetically simulated.** `FUTURE-BLOCKED`, `SPEC-ONLY`, `UNKNOWN` and
    out-of-release features do not appear as normal permanently-disabled shipping controls.

---

# 2. Actions and state

## 2.1 Hierarchy

| Class | Use | Constraint |
|---|---|---|
| Primary | dominant task commit/progression action | normally one visually dominant action per task region |
| Secondary | supporting/reversible action | must not compete with primary |
| Destructive | material non-obvious loss/context replacement | needs owning lifecycle evidence before final confirmation behavior |
| Quiet/tertiary | low-risk utility/disclosure | never hides critical action |

Prominence never establishes legality; owning state does.

## 2.2 Shared states

| State | Required behavior |
|---|---|
| Default | critical actions use an explicit label |
| Hover | optional affordance; never sole critical information |
| Focus | unmistakable and distinct from hover/selection |
| Pressed | local acknowledgement without predicting command success |
| Disabled | non-invokable; critical/high-frequency actions expose a useful reason |
| In progress | suppress duplicate invocation only when owner exposes unresolved work |
| Success | reflect new projected state; toast may acknowledge non-blocking completion |
| Failure/refusal | preserve recoverable context; actionable reason is persistent |

UI does not invent progress/success/failure lifecycles absent from the owner.

Confirmation is reserved for material, non-obvious loss where safer undo/recovery is absent. Routine reversible actions
must not accumulate confirmation friction.

---

# 3. Navigation, tabs and focus

F2 owns page/modal/drawer choice; F3 owns interaction behavior inside them.

- Shipping navigation exposes only admitted/live destinations.
- Current PM-1 retains its exact four screens/five named moves. F3 adds no abandon-match or synthetic Back edge.
- Current location has structural/text indication, not color alone.
- Non-actionable nav is not in normal tab order.
- Tabs represent alternate views of one owning context; tab changes do not silently mutate domain state.
- Focus can enter/leave a tab set and remains logical after localization/reflow.

**Focus recovery:** preserve the same logical target if it survives refresh; otherwise move to the nearest meaningful
surviving target in the same task region, then the next safe page anchor. Never drop focus invisibly to app root.
Closing modal/drawer restores its invoker when possible.

`Escape` closes only a layer whose contract permits cancellation. It is not browser-history Back.

Final shortcut bindings belong to journey Gate C/D; shared shortcuts must not shadow text entry, OS conventions or
another S0 critical action.

---

# 4. Dense tables, sorting, filtering and selection

Every admitted table/list identifies:

- stable row identity from the owning projection;
- primary identifying column;
- sortable/filterable columns and current sort/filter state;
- selected row where selection exists;
- empty/loading/partial/stale/error behavior;
- critical columns for the small-desktop validation case.

Display text such as a player name is not synthesized into a stable identity.

Sorting/filtering is keyboard reachable and does not mutate domain state. Sort key/direction and selection survive
without color. Selection follows the same logical row after sorting where it still exists. Zero-result filtering is
distinct from genuinely empty data and exposes a clear-filter recovery path.

Focus and selection are related but not conflated: moving focus does not itself create a domain mutation.

At constrained width, secondary columns may fold into detail disclosure while identity, task-critical state and primary
action remain visible. Wider layouts may reveal useful secondary columns or breathing room; controls/text do not simply
stretch to consume space.

---

# 5. Inputs, selectors and toggles

- Inputs keep a persistent label; placeholder text is not the label.
- Current value is distinguishable from help text.
- Validation identifies the affected field and recovery, not merely “invalid”.
- Selector/toggle changes do not imply domain commit unless the owning command contract says so.
- Multi-field staging makes commit/cancel boundaries explicit.
- Selectors tolerate long localized values and keyboard navigation.
- A toggle represents only an unambiguous binary state; state is not conveyed by color alone.

---

# 6. Tooltips and contextual help

F2's priority remains binding: clear label/state → inline reason → tooltip/popover → dedicated reference.

Tooltips/popovers:

- never carry the only critical instruction, blocker reason or recovery action;
- are available from keyboard focus as well as pointer hover;
- dismiss without losing task focus;
- may explain approved/static terminology but do not invent football causality, recommendations or simulation depth;
- reflow for expanded text/max-scale testing rather than requiring a fixed pixel box.

---

# 7. Modal and drawer behavior

**Modal:** intentionally traps focus; initial focus goes to the safest meaningful action/first required field rather
than automatically to destructive action. Tab/Shift-Tab stay inside. Escape/Cancel exists unless the owning operation
is genuinely non-cancellable. Close restores parent focus. Background cannot receive input. A progress modal is valid
only when the owner exposes progress/blocking semantics.

**Drawer/detail rail:** preserves parent task and does not create shell history by itself. It may inspect/secondarily
edit, but cannot hide the page's required primary action or blocker. Focus has a deterministic path in and back out.

---

# 8. Feedback and unresolved-data states

| Pattern | Use |
|---|---|
| Inline | field/row/action-specific state, refusal, validation |
| Banner | page-level blocker/high-priority warning |
| Toast | transient non-blocking acknowledgement |
| Badge | summary/count on a live destination; explanation exists inside |

**Loading** is shown only when the client can distinguish unresolved from empty/failure; do not flash false zeros.
For Match View, #38's approved contract applies: before the first streamer frame, use documented last-known/empty-frame
behavior and never advance simulation to force a frame.

**Empty** distinguishes no data from filtered-to-zero. **Partial** is used only when the owner supports partial
availability; missing values are not invented. **Stale** appears only with owner/freshness evidence or a known context
replacement; UX does not infer it from wall time. **Error/refusal** preserves safe context, uses player-facing copy,
remains persistent when action is required, and offers retry only where the owning lifecycle permits it.

---

# 9. Localization, text scale and glyph resilience

## 9.1 Authority split — corrected in AR-1

Approved #49 currently establishes the localization seam plus a **read-only presentation a11y settings boundary**
(text scale / contrast / colourblind-safe palette / input assist), and explicitly defers option content + settings
store to a later tier.

The later #49 content-tier design proposes that #38 rendering/theme apply scale/reflow/contrast and own palette/font
fallback content. That assignment is **design/back-prop intent, not yet an approved #38/runtime fact**. F3 therefore:

- requires the UX behavior as a `TARGET` release constraint;
- does not claim the #38 application/palette/font owner is already landed;
- keeps release settings UI `FUTURE-BLOCKED` until the owning client/content work lands.

## 9.2 Pseudo-locale/reflow

The detailed UX plan requires pseudo-locale stress testing. When the #49 content-tier pseudo-locale is available, use
that contract. Before then, prototypes use equivalent expanded/bracketed test content and label it prototype evidence,
not production-localization proof.

Critical labels/actions, score/time, selection, tables, modal/drawer controls and required recovery must remain visible
and operable. Layout reflows rather than overlaps.

## 9.3 Maximum text scale

F3 does not invent a numeric maximum. Every S0 primitive must pass at the maximum value eventually admitted by the
shipping a11y/client contract. Prototype evidence records the explicit test value used. Scaling cannot clip a critical
label or make the primary action unreachable.

## 9.4 Glyph fallback

The HTML Google Fonts are reference-only. A verified shipping font/glyph-fallback chain is required before a real
locale can be honestly offered, but the final owner remains a pending implementation/back-prop matter. Missing glyphs
must never become blank critical labels.

---

# 10. Contrast/colorblind/color-independent semantics

Palette ownership/application is not asserted as currently landed; see §9.1. Independently of palette choice, every
critical/high state that uses color (positive/negative, selected, warning, role/position, form, attribute band, result)
needs a non-hue carrier: text, icon/shape, sign, position, pattern or equivalent.

Focus remains distinct from selection/warning/status. Disabled state cannot rely on low opacity that destroys
readability. Later charts/heatmaps need a non-color reading path, but F3 does not invent #37 behavior outside S0.
Existing mockup colors are references, not contrast evidence.

---

# 11. Art and identity fallbacks

| Slot | Fallback rule |
|---|---|
| Club badge/crest | neutral identity placeholder; club text identity remains authoritative; never borrow another club's crest |
| Player portrait | neutral silhouette; name/role remains authoritative |
| Stadium/background | neutral environment/texture; no invented venue detail |
| Key art/menu hero | restrained branded/system fallback; navigation remains fully readable without art |

Fallbacks keep stable layout slots, carry no hidden status/ability meaning, and do not fabricate data. Exact asset
formats/loading/provenance remain art/client workstream concerns.

---

# 12. Audio, captions and HUD coexistence

#51 is approved as a specification but remains `FUTURE-BLOCKED / SPEC-ONLY` in the F1 runtime baseline.

When admitted:

- muted audio is a valid state; required task information cannot depend on hearing alone;
- informational cues requiring caption coverage need a presentation region that avoids score/time, critical controls,
  primary alerts and current focus;
- captions survive expanded text/max-scale testing and are not duplicated into competing HUD regions;
- an explicitly justified `NoCaption` ambience/texture cue is not a UX error;
- UX does not persist audio settings itself; persistence follows the owning client/settings contract.

Before runtime admission, prototypes may reserve/test this region only as `FUTURE-BLOCKED`.

---

# 13. Desktop layout behavior

F3 uses design-validation cases, not shipping platform promises:

| Case | Reference | Required behavior |
|---|---|---|
| Small desktop | 1366-wide case already identified by the HTML guardrail audit | preserve critical context/state/action; fold secondary detail |
| Reference | 1920×1080 | baseline `touchline` density/hierarchy |
| Expanded | 2560-wide/high-resolution/ultrawide case | reveal useful detail or safe breathing room; do not merely stretch |

`1366` is not declared as the supported minimum. The shipping minimum remains a product/implementation decision.

Reflow priority: task identity/context → blocker/current state → primary action → primary identifiers/decision-critical
values → contextual detail → decorative art/chrome. Wide analytical tables may scroll horizontally only when identity
and current selection remain understandable.

---

# 14. S0 primitive admission matrix

| Primitive | S0 need | Shared rule | Status |
|---|---:|---|---|
| Primary/secondary/destructive action | yes/conditional | §2 | DEFINED; destructive lifecycle owner required |
| Navigation/current location/tabs | yes/conditional | §3 | DEFINED; current graph remains authority |
| Dense table/list + sort/filter/selection | where journey needs it | §4 | DEFINED |
| Input/selector/toggle | yes for setup/tactics | §5 | DEFINED |
| Tooltip/context help | yes | §6 | DEFINED |
| Modal/drawer | conditional | §7 | DEFINED |
| Toast/banner/inline | yes | §8 | DEFINED |
| Loading/empty/partial/stale/error | as owner exposes them | §8 | DEFINED with evidence constraint |
| Keyboard traversal/no trap | yes | §§3,7 | DEFINED |
| Pseudo-locale/reflow | yes | §9 | DEFINED; production mechanism future |
| Maximum text scale | yes | §9 | DEFINED without invented value |
| Contrast/color-independent semantics | yes | §10 | DEFINED; palette/application ownership pending |
| Glyph fallback | yes | §9 | DEFINED behavior; implementation owner pending |
| Missing art fallbacks | where slot exists | §11 | DEFINED |
| Caption/HUD coexistence | future dependency | §12 | DEFINED / FUTURE-BLOCKED runtime |
| Small/reference/expanded desktop | yes | §13 | DEFINED as validation cases |

---

# 15. Findings and dependencies

**F3-001 — mockup tokens are references, not compliance evidence.** Gate E must prove contrast, expanded text and scale.

**F3-002 — keyboard/focus semantics are UX/rendering requirements, not current `NavigationShell` behavior.** They must
be implemented in binding/journeys and verified at Gate J; F3 does not modify #38's pure shell.

**F3-003 — approved #49 does not yet settle the application/theme owner.** The content-tier design proposes #38
application/palette/font responsibilities, but that back-prop/runtime work remains future. AR-1 corrected the earlier
v0.1 overclaim.

**F3-004 — missing art is a normal Gate E fixture.** S0 cannot depend on production art availability.

**F3-005 — audio/caption coexistence is a valid future constraint without a live #51 consumer.** Do not present it as
current runtime behavior.

**F3-006 — `1366 / 1920 / 2560` are validation cases, not a platform-support declaration.**

**F3-007 — disabled and future are different.** A real temporarily-illegal action may be disabled with reason; an
unimplemented action/module is omitted.

---

# 16. F3 exit assessment

The detailed-plan F3 primitive set is covered: actions; nav/tab/focus; tables/sort/filter/selection; inputs/selectors/
toggles; help; modal/confirmation/drawer; feedback; loading/empty/partial/stale/error; keyboard/no-trap;
pseudo-locale/reflow/max scale; color-independent semantics; glyph fallback expectation; missing-art fallback;
caption/HUD/muted-audio behavior; and desktop layout cases.

**F3 COMPLETE.** AR-2 found no new ownership, capability or status drift after the AR-1 correction. The current
workstream phase is defined only by [`ux-detailed-plan.md`](ux-detailed-plan.md); this F3 artifact does not duplicate that moving status. No high-fidelity journey production is authorized by F3 alone.

#region VersionHistory
| Version | Date | Change |
|---|---|---|
| 0.1 | 2026-09-07 | Initial UX-B shared system. |
| 0.2 | 2026-09-07 | AR-1: corrected the substantive ownership overclaim. Approved #49 defines the read-only a11y boundary and defers option content/store; #38 application/palette/font assignment remains content-tier design/back-prop intent, not a landed approved/runtime fact. Compacted wording while preserving F3 primitive/state coverage. |
| 0.3 | 2026-09-07 | AR-2: rechecked against F1/F2 plus approved #38/#49/#51. No new substantive findings. Marked F3 complete and F4 next. |
| 0.4 | 2026-09-11 | Review correction: removes the stale moving-phase claim from this completed F3 artifact and points current workstream status to `ux-detailed-plan.md`. No F3 interaction-system semantics change. |
#endregion
