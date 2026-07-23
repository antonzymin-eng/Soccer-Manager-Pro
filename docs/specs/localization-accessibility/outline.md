# Localization & Accessibility Specification #49 (seam + template contract slice) — Outline

**Created:** July 23, 2026
**Last Updated:** July 23, 2026 (v0.2 — section-file PASS-1 (1H+1M+1L) → AR-2 convergence; APPROVED)
**Version:** 0.2
**Status:** APPROVED
**Source:** `docs/tracking/localization-seam-template-design.md` v0.2 (July 23, 2026), AR-1 (2H+1M+1L) → AR-2 converged

---

## Purpose

Defines the **one localization seam** every user-facing string routes through — static UI strings **and**
procedurally generated text alike — plus the **template/slot contract** procedural producers emit through.
The load-bearing invariant: a single routing point (`ILocalizer`), and a **localize-after-generate**
boundary that keeps procedural determinism (#22's `world.text` draw + serialized memory) locale-independent,
so a save round-trips byte-identically regardless of display locale. It is presentation/content layer:
**no sim assembly may reference it** (the `match-viewer` lock).

Authored **seam + contract slice only** (the #38 framework/screens precedent): translated **locales** and
the **accessibility content surface** are **Wave 8** — this slice pins the seam, the template model, the
reference direction, the fallback policy, and the **retrofit of the one built producer** (#22
`InteractionTextGenerator`). This is a **forward design** (the #21–#38 pre-code posture); T-phase code +
the #22 retrofit + Wave-8 locale/a11y content are post-APPROVED follow-ups.

## Section map

| Section | Content |
|---|---|
| 1 | Introduction, scope (the seam+contract slice; the locale/a11y-content + producer deferrals), dependencies, key decisions (KD-1..KD-7), boundary matrix |
| 2 | Functional requirements (FR-LC-001..020 + FR-LC-008a), data structures (generic core `ILocalizer`/`LocalizationKey`/`TextTemplateId`/`LocalizedTextRequest`/`NamedSlotSet`/`TemplateCatalogue` + the per-producer boundary adapter), failure modes F1–F6 |
| 3 | The seam (static `Resolve` + procedural `Render`); the localize-after-generate boundary; the template model; the pre-draw validation split + citation clause; worked render |
| 4 | Architecture: the `TacticalDirector.Localization` assembly, the one-way reference direction (renderer references built producers; no sim assembly references it), the #22 retrofit, no RNG/tag/ordinal |
| 5 | Test plan (coverage lock / base-locale identity / localize-after-generate save round-trip / fallback fail-safe / template model / no-reverse-reference) + FR traceability |
| 6 | Performance: display-time transform (off the sim loops); zero persistent sim state |
| 7 | Forward extensions: Wave-8 locale content + the a11y content surface; #35/#46/#38-static producer bindings; grammar-depth Stage-3+ deferral |
| 8 | References (the layer taxonomy #20 §3.5.2; `InteractionTextGenerator`/`InteractionSlots`/`InteractionTextCorpus`; #38 FR-UI-004/KD-5) |
| 9 | Approval checklist + R-01..R-05 lead-developer gates |
| Appendices | Constant catalogue (`LocalizationConstants`); the producer-emission → renderer-input mapping table; the #22-retrofit before/after byte-identity table; a worked render transition |

## Key decisions (detailed in §1)

- **KD-1** One seam, two emission shapes: static `LocalizationKey` + procedural (native id + slots +
  draw). A baked localized string fails the seam.
- **KD-2** Localize-after-generate: the transform is display-side, after deterministic generation; the
  serialized surface is locale-independent → save round-trips across locales.
- **KD-3** Template model: named-placeholder substitution + a bounded plural/gender category selector; no
  arbitrary morphology.
- **KD-4** A11y: record the boundary only (client-local, no sim reference, no save impact); option content
  is Wave 8.
- **KD-5** Fallback: a missing key/locale/variant/clause renders the base-locale identity; never crash,
  never mutate; dev marker only in dev builds.
- **KD-6** Seam placement / one-way reference: the renderer lives high (`TacticalDirector.Localization`),
  references only built producers; no sim assembly references it; producers emit native values, the #49
  boundary assembles the request; the pre-draw validation stays sim-side.
- **KD-7** No determinism identifiers (no RNG stream / domain tag / ordinal / save-format bump).

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-23 | — | Initial outline authored from the converged design supplement (v0.2). Status IN REVIEW. |
| 0.2 | 2026-07-23 | — | Section-file PASS-1 (1H+1M+1L; H-1 generic-core / per-producer boundary-adapter split, M-1 FR-LC-008a construction-time roster-coverage invariant, L-1 `{score}` derived) → AR-2 convergence; APPROVED. See section-9 §9.3.1. |
#endregion
