# Localization & Accessibility Specification #49 (seam + template contract slice) — Outline

**Created:** July 23, 2026
**Last Updated:** September 12, 2026 (v0.3 — L1 dependency direction and typed-selector architecture synchronized)
**Version:** 0.3
**Status:** APPROVED
**Source:** `docs/tracking/localization-seam-template-design.md` v0.2 (July 23, 2026), AR-1 (2H+1M+1L) → AR-2 converged

---

## Purpose

Defines the **one localization seam** every user-facing string routes through — static UI strings **and**
procedurally generated text alike — plus the **template/slot/selector contract** procedural producers map
into at the localization boundary. The load-bearing invariant: a single routing point (`ILocalizer`), and a
**localize-after-generate** boundary that keeps procedural determinism (#22's `world.text` draw + serialized
memory) locale-independent, so a save round-trips byte-identically regardless of display locale. It is
presentation/content layer: **no sim assembly may reference it**.

The L1 core contract is now implemented in `TacticalDirector.Localization`; catalogue lookup, template
expansion/rendering, producer adapters, translated **locales**, and the **accessibility content surface**
remain later slices. Wave 8 still owns locale/a11y content. The #22 retrofit remains a later integration
through a sibling boundary adapter rather than a dependency from the core localization assembly.

## Section map

| Section | Content |
|---|---|
| 1 | Introduction, scope (the seam+contract slice; the locale/a11y-content + producer deferrals), dependencies, key decisions (KD-1..KD-7), boundary matrix |
| 2 | Functional requirements (FR-LC-001..020 + FR-LC-008a), data structures (generic core `ILocalizer`/`LocalizationKey`/`TextTemplateId`/`LocalizedTextRequest`/`NamedSlotSet`/typed selector contracts + the per-producer boundary adapter), failure modes F1–F6 |
| 3 | The seam (static `Resolve` + procedural `Render`); the localize-after-generate boundary; the template model; the pre-draw validation split + citation clause; worked render |
| 4 | Architecture: the `TacticalDirector.Localization` generic core, its no-sim-reference rule, sibling per-producer boundary adapters that reference both sides, the #22 retrofit, no RNG/tag/ordinal |
| 5 | Test plan (coverage lock / base-locale identity / localize-after-generate save round-trip / fallback fail-safe / template model / no-reverse-reference) + FR traceability |
| 6 | Performance: display-time transform (off the sim loops); zero persistent sim state |
| 7 | Forward extensions: Wave-8 locale content + the a11y content surface; #35/#46/#38-static producer bindings; grammar-depth Stage-3+ deferral |
| 8 | References (the layer taxonomy #20 §3.5.2; `InteractionTextGenerator`/`InteractionSlots`/`InteractionTextCorpus`; #38 FR-UI-004/KD-5) |
| 9 | Approval checklist + R-01..R-05 lead-developer gates |
| Appendices | Constant catalogue (`LocalizationConstants`); the producer-emission → renderer-input mapping table; the #22-retrofit before/after byte-identity table; a worked render transition |

## Key decisions (detailed in §1)

- **KD-1** One seam, two emission shapes: static `LocalizationKey` + procedural producer-native facts mapped
  to a generic request. A baked localized string fails the seam.
- **KD-2** Localize-after-generate: the transform is display-side, after deterministic generation; the
  serialized surface is locale-independent → save round-trips across locales.
- **KD-3** Template model: named-placeholder substitution + bounded plural/gender selection from typed,
  locale-neutral selector operands; no arbitrary morphology.
- **KD-4** A11y: record the boundary only (client-local, no sim reference, no save impact); option content
  is Wave 8.
- **KD-5** Fallback: a missing key/locale/variant/clause renders the base-locale identity; never crash,
  never mutate; dev marker only in dev builds.
- **KD-6** Seam placement / one-way reference: `TacticalDirector.Localization` is producer-agnostic and
  references nothing sim-side; a sibling boundary adapter introduced for each producer references both the
  localization core and that built producer, constructs the generic request, and preserves the rule that
  producers themselves never reference #49. The pre-draw validation stays sim-side.
- **KD-7** No determinism identifiers (no RNG stream / domain tag / ordinal / save-format bump).

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-23 | — | Initial outline authored from the converged design supplement (v0.2). Status IN REVIEW. |
| 0.2 | 2026-07-23 | — | Section-file PASS-1 (1H+1M+1L; H-1 generic-core / per-producer boundary-adapter split, M-1 FR-LC-008a construction-time roster-coverage invariant, L-1 `{score}` derived) → AR-2 convergence; APPROVED. See section-9 §9.3.1. |
| 0.3 | 2026-09-12 | GPT-5.6 Sol | Synchronizes the outline with the approved generic-core/sibling-boundary dependency direction and the L1 typed-selector request contract; records that the L1 core exists while renderer/catalogue/content and producer integration remain later slices. |
#endregion
