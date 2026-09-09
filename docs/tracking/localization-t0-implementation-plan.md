# Localization & Accessibility #49 — T0 Infrastructure Implementation Plan

**Created:** September 4, 2026
**Last Updated:** September 4, 2026
**Version:** 0.1
**Status:** IMPLEMENTATION PLAN
**Governs:** `src/localization/` (`TacticalDirector.Localization`) and its T0 test assembly
**Spec:** `docs/specs/localization-accessibility/` #49 (APPROVED)

---

## 1. Objective

Land the producer-agnostic localization substrate required by #49 without touching any simulation producer.
T0 establishes the single localization seam and generic request/value contracts. T1 will migrate the
`living-world` English corpus and add the separate `localization-boundary` adapter.

T0 must remain safe to develop in parallel with backend, UI, art and audio work:

- no `living-world` source edits;
- no sim/loop assembly reference to localization;
- no RNG, deterministic identifier, tick ownership or save-format change;
- no translated locale content beyond the fixed base-locale identity (`en`);
- no Wave-8 accessibility option catalogue or settings persistence.

## 2. Authoritative architecture

The approved post-review contract in #49 §2.2, §3.1, §4.1 and §9.3.1 is authoritative:

- `TacticalDirector.Localization` is a generic core assembly and references **no sim assembly**;
- producer-specific mapping belongs in a separate `localization-boundary` assembly added at T1;
- `ILocalizer` exposes exactly the two surface-string operations: `Resolve(LocalizationKey)` and
  `Render(in LocalizedTextRequest)`;
- `TextTemplateId` is generic `(producerTag, localOrdinal)` identity;
- `NamedSlotSet` is an immutable producer-agnostic `name -> string` collection;
- `LocalizedTextRequest.SelectionDraw` is the producer's locale-independent `ulong` value carried verbatim.

### 2.1 Stale §1.6 wording

`section-1.md` §1.6 still contains older wording saying the renderer references built producers. That conflicts
with the later PASS-1/AR-3 result recorded in §9.3.1 and the explicit §2.2/§4.1 core/boundary split. T0 will
not reproduce the stale dependency. The documentation cleanup is tracked as a same-workstream follow-up and
must be resolved before T1 changes producer code.

## 3. T0 slices

### L0.1 — assembly + core value contracts

Create `src/localization/` and its Editor-only test assembly. Land:

- `ILocalizer`
- `LocalizationKey`
- `LocaleId`
- `LocalizationConstants`
- `TextTemplateId`
- `NamedSlot`
- `NamedSlotSet`
- `LocalizedTextRequest`

This slice proves the producer-agnostic type boundary before any catalogue or rendering behavior is added.

### L0.2 — catalogue + fallback

Land the in-memory catalogue structures and `Localizer` implementation:

- base-locale static strings;
- base-locale template variants;
- producer-scoped clause keys;
- current-locale -> base-locale fallback;
- base-locale variant-count selection using `ulong` modulo;
- no runtime state mutation during `Resolve`/`Render`.

### L0.3 — template expansion

Land pure named-slot substitution and the bounded grammatical selector. The selector accepts only authored
categories; it does not synthesize morphology. Base-locale templates with no selector reduce to pure string
replacement, matching the approved identity contract.

### L0.4 — construction-time coverage contract

The core cannot inspect producer enums without violating FR-LC-012. Therefore `TemplateCatalogue`
construction will accept generic required template IDs and producer-scoped clause keys supplied by the
composition layer. Construction fails if any required base-locale template row is absent/empty or any
required base-locale clause is absent.

At T1, `localization-boundary` derives that generic requirement set from the complete defined
`InteractionIntent`/citable `EventKind` rosters. This preserves FR-LC-008a's fail-loud construction-time
coverage without introducing a core -> `living-world` reference.

### L0.5 — T1 handoff

Only after L0.1-L0.4 are green:

1. add `src/localization-boundary/` referencing localization + `living-world`;
2. add `LivingWorldTextBoundary`;
3. change `InteractionTextGenerator.Generate` to return native deterministic values;
4. migrate `InteractionTextCorpus` templates and clauses into the base-locale catalogue;
5. prove byte-identical base-locale rendering and unchanged cursor/save behavior;
6. remove the old baked-string path only after identity tests pass.

## 4. Assembly placement

`localization` is presentation/client infrastructure and is seated in Code Standards #20 §3.5.2 tier 9
(Client). Its production asmdef has no references at T0. The test asmdef references only the production
assembly plus the repository's standard Editor/NUnit test dependencies.

No sim/loop tier may reference `TacticalDirector.Localization`.

## 5. Verification matrix

| Slice | Required checks |
|---|---|
| L0.1 | compile; value equality/default safety; `NamedSlotSet` defensive-copy/immutability tests; public seam shape |
| L0.2 | static resolve; procedural render; unknown locale fallback; missing translation fallback; base variant-count modulo |
| L0.3 | named substitution; bounded category selection; base-locale no-selector identity |
| L0.4 | missing/empty required base row fails at construction; missing required clause fails at construction |
| L0.5 | #22 base-locale byte identity; refused call consumes no cursor; locale-neutral save/digest; no reverse asmdef reference |

Repository-level checks after each merge-ready slice:

- `python3 tools/assembly-tier-check.py --repo .`
- `python3 tools/doc-consistency-check.py`
- `python3 tools/recurring-defect-lint.py`
- Linux shim compile/test gate for all generated asmdef projects

## 6. Explicit non-goals

T0 does **not** add Ukrainian or any other translated strings, locale-selection UI, text-scale/contrast
content, a settings store, media/news localization adapters, or UI screen copy. Those are later content or
producer-binding slices on top of this seam.

## 7. Done condition for T0

T0 is complete when the generic localization assembly compiles/tests independently, implements the approved
Resolve/Render/fallback/template/coverage contracts, has no sim-side reference, and is ready for T1 to bind
`living-world` without changing the core seam.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-09-04 | — | Initial T0 implementation plan; records the strict generic-core/boundary split, staged slices and generic construction-time coverage mechanism. |
#endregion
