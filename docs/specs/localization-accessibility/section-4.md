# Localization & Accessibility #49 — Section 4: Architecture

**Created:** July 23, 2026
**Last Updated:** July 23, 2026 (v0.2 — section-file PASS-1 (1H+1M+1L) → AR-2 convergence; APPROVED)
**Version:** 0.2
**Status:** APPROVED

---

## 4.1 Assembly placement & reference direction (KD-6)

New presentation/content assembly **`TacticalDirector.Localization`** (`src/localization/`).

**Reference direction (the load-bearing KD-6 invariant) — one-way, the #38 rule (generic core references
nothing sim-side; only a concrete per-producer boundary adapter references a built producer):**
- **No sim/loop assembly references `TacticalDirector.Localization`** (enforced by the absence of the
  reverse reference — the `match-viewer`/`ui-framework` no-reverse-reference lock; §5 asserts it, FR-LC-012).
  A sim-side producer like #22 (`living-world`, references only `DeterministicSim`) MUST NOT gain a
  reference to the presentation layer.
- **The generic core seam references *nothing* sim-side** (the #38 "generic substrate references nothing
  sim-side" rule). `ILocalizer`/`LocalizationKey`/`TextTemplateId` (a generic `(producerTag, localOrdinal)`
  value)/`LocalizedTextRequest` (a producer-agnostic named-slot payload) name **no** `living-world` type.
- **A per-producer boundary adapter references only a *built* producer** — the #38 "concrete surface
  references only assemblies that already exist" rule (FR-LC-013). At Stage 2 that is exactly **one**
  adapter, `LivingWorldTextBoundary` (§2.2.1), referencing `living-world` (to read `InteractionIntent` +
  the slot facts + the `EventKind` clause key) and mapping them into the generic `LocalizedTextRequest`.
  This adapter — **not** the core seam — is the only sim-side reference; it lives at the composition layer
  that already references both. Adding #35/#46 adds a **sibling adapter**, never a change to the core.
- A producer never references a #49 type: it emits its own native values, and the boundary adapter builds
  the `TextTemplateId`/`LocalizedTextRequest` (FR-LC-014).

## 4.2 File layout (proposed)

```
src/localization/                  // CORE assembly — references NO sim assembly (FR-LC-012)
├── localization.asmdef            // references: (none sim-side); referenced by no sim assembly
├── ILocalizer.cs                  // the single seam: Resolve(key) + Render(req) — generic input only
├── LocalizationKey.cs             // stable identity for a static string
├── TextTemplateId.cs              // GENERIC (producerTag, localOrdinal) identity — no producer enum
├── LocalizedTextRequest.cs        // template id + ulong draw + producer-agnostic NamedSlotSet + (hasCitedEpisode, citationKind)
├── NamedSlotSet.cs                // producer-agnostic named-value slot payload
├── LocaleId.cs                    // base + (Wave-8) locale identities; BaseLocale constant
├── TemplateCatalogue.cs           // per-locale static strings + per-(Id, variant) templates + variantCount + per-clause-key clauses; FR-LC-008a coverage assert at construction
├── TemplateExpander.cs            // named-placeholder substitution + bounded plural/gender selector (KD-3)
├── Localizer.cs                   // ILocalizer impl: KD-5 fallback precedence + the §3.2 Render algorithm
├── A11yBoundary.cs                // KD-4 boundary marker (read-only presentation settings shape; content/store = Wave 8)
├── LocalizationConstants.cs       // [FIXED] BaseLocale etc.; no sim constants
└── Tests/ …                       // BaseLocaleIdentity / Fallback / LocalizeAfterGenerate / Coverage(F5) tests

src/localization-boundary/         // per-producer BOUNDARY adapters — the ONLY sim-side references
├── localization-boundary.asmdef   // references: Localization + living-world (composition layer)
└── LivingWorldTextBoundary.cs     // §2.2.1: maps InteractionIntent + InteractionSlots + draw -> LocalizedTextRequest
                                   //   (#35/#46 add SIBLING adapters here — never a core change)
```

**Producer-side (T-phase, `living-world`):** `InteractionTextGenerator.Generate` returns native values
`(intent, selectionDraw, slots incl. hasCitedEpisode + citationKind)` instead of a baked string;
`InteractionTextCorpus` (template rows **and** the per-`EventKind` clause table) migrates to the base-locale
`TemplateCatalogue`. **`living-world` gains no reference to `Localization`** — it emits its own types
(FR-LC-014); the `LivingWorldTextBoundary` adapter (a separate composition-layer assembly) is what maps
those into the generic request. The `world.text` draw and the pre-draw validation (the reconstructed
intent-value roster gate) stay in `living-world` (§3.4 / FR-LC-015).

## 4.3 Determinism & naming (KD-7 / FR-LC-017)

#49 registers **no** RNG stream, **no** `DOMAIN_TAG_*`, **no** `SubsystemOrdinal`, holds no persistent sim
state, and bumps no save format — it draws nothing (the one relevant draw is #22's existing `world.text`,
unchanged and owned by `living-world`), advances nothing, and persists nothing to the determinism save
(locale + a11y selections are client-local settings outside it). It appears nowhere in the #16 §3.4
catalogue; there is nothing to reserve and no `_RESERVED_` placeholder is warranted (the #37/#38 posture).

## 4.4 CS0104 hazard (name collision)

`LocalizationKey` / `TextTemplateId` / `LocalizedTextRequest` / `Localizer` / `TemplateCatalogue` are new
names; a grep of `docs/specs/**` + `src/**` at T0 MUST confirm no existing type shares them before the
assembly is wired (the `TacticTranslation` / `PlayerAttributes` CS0104 precedent). If a future spec brings a
same-named type into a shared scope, fully-qualify from line one (the KD-P6 discipline).

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-23 | — | Initial architecture: assembly placement + one-way reference direction, file layout, the #22 retrofit, no RNG/tag/ordinal, CS0104 note. Status IN REVIEW. |
| 0.2 | 2026-07-23 | — | Section-file PASS-1 (1H+1M+1L; H-1 generic-core / per-producer boundary-adapter split, M-1 FR-LC-008a construction-time roster-coverage invariant, L-1 `{score}` derived) → AR-2 convergence; APPROVED. See section-9 §9.3.1. |
#endregion
