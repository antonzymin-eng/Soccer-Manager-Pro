# Localization & Accessibility #49 — Section 2: Functional Requirements, Data Structures, Failure Modes

**Created:** July 23, 2026
**Last Updated:** July 23, 2026 (v0.2 — section-file PASS-1 (1H+1M+1L) → AR-2 convergence; APPROVED)
**Version:** 0.2
**Status:** APPROVED

---

## 2.1 Functional requirements

**The seam (KD-1)**
- **FR-LC-001** — All user-facing text (static UI strings and procedurally generated text alike) MUST route
  through the single `ILocalizer` seam. No other code path may produce a surface string shown to the player.
- **FR-LC-002** — A producer MUST NOT emit a baked, human-readable localized string. It emits an identity
  (`LocalizationKey`) or its native procedural values (§2.2). Violation is a **coverage-lock** failure (F3),
  enforced at the producer's own spec.
- **FR-LC-003** — Static strings MUST resolve via `string Resolve(LocalizationKey key)`.
- **FR-LC-004** — Procedural text MUST render via `string Render(in LocalizedTextRequest req)`, where the
  request carries a `TextTemplateId`, the `ulong` selection draw, the slot facts, and the citation
  `(hasCitedEpisode, citationKind)`.

**Localize-after-generate (KD-2)**
- **FR-LC-005** — The localization transform MUST run display-side, strictly **after** deterministic
  generation. The renderer MUST NOT advance any sim tick or draw from any RNG stream.
- **FR-LC-006** — A producer's serialized state and its deterministic draw MUST be locale-independent. A
  save (`WorldStore` / season save) MUST round-trip **byte-identically** across display locales; only the
  rendered strings differ.
- **FR-LC-007** — The variant selection MUST be `variant = draw % variantCount(BaseLocale, Id)`, computed in
  the renderer, where `variantCount` is **locale-independent** (the base-locale count). The modulo MUST be
  computed in `ulong` (FR-LC-020) then narrowed.
- **FR-LC-008** — Every locale MUST supply `variantCount(Id)` templates for the base count; a missing
  `(Id, variant)` MUST fall back to the base-locale template (KD-5 / FR-LC-011).

**Template model (KD-3)**
- **FR-LC-009** — The template model MUST be named-placeholder substitution plus a bounded plural/gender
  category selector (CLDR-style categories + a small gender set). A template MUST NOT require arbitrary
  runtime morphology. Base-locale English declares no categories (identity with `.Replace`).
- **FR-LC-010** — The episode citation clause MUST be a per-`EventKind` localizable string, selected by
  `EventKind` (a sim fact), **not** by the draw, and appended when `hasCitedEpisode` — matching
  `InteractionTextGenerator`'s `text + " " + clause`. The clause table migrates to the base-locale catalogue.

**Fallback (KD-5)**
- **FR-LC-011** — A missing key, locale, `(Id, variant)`, or clause MUST render the base-locale identity;
  it MUST NOT crash and MUST NOT mutate any state. A visible `‹key›` marker is permitted **only** in dev
  builds; production MUST fall through to the base locale.

**Seam placement / reference direction (KD-6)**
- **FR-LC-012** — No sim/loop assembly may reference `TacticalDirector.Localization` (the no-reverse-
  reference lock; F6 is a build error). The **core seam** (`ILocalizer`/`LocalizationKey`/`TextTemplateId`/
  `LocalizedTextRequest`, §2.2) MUST reference no sim assembly; only a **per-producer boundary adapter**
  (§2.2.1) references a producer.
- **FR-LC-013** — A boundary adapter MUST reference only a **built** producer; an adapter/reference for an
  unbuilt producer is forbidden (the FR-LW-031 phantom-dependency rule). Adding producer #2 (#35/#46) adds
  a sibling adapter, never a change to the core seam.
- **FR-LC-014** — A producer MUST emit only its own native values; `LocalizedTextRequest` and
  `TextTemplateId` (both #49-core types) MUST be assembled by the **boundary adapter** (§2.2.1), never by
  the producer.
- **FR-LC-015** — The producer's **pre-draw gate is an intent-VALUE roster check** (`None` + enum-defined /
  max-defined-ordinal — locale-independent, sim-side), *replacing* today's `TemplatesFor` corpus-length
  throw (which becomes #49 catalogue content after the retrofit). Combined with the FR-LC-008a
  **construction-time roster coverage** invariant below, every intent the sim-side gate admits has
  `variantCount(Id) ≥ 1`, so the renderer never divides by zero (F1) and a refused call consumes no
  `world.text` cursor (the no-cursor-on-refusal invariant is preserved).
- **FR-LC-008a** — **(coverage invariant, load-bearing for the split)** The base-locale catalogue MUST
  cover the producer's **entire defined roster**: every defined `InteractionIntent` (excl. `None`) has ≥1
  base-locale template, and every defined citable `EventKind` has a base-locale clause. This is asserted
  **fail-loud at catalogue construction** (F5), so a defined id **missing** a row is caught at the authoring
  boundary — not merely an explicit 0-count row, and never a consumed-cursor-then-fail at render.
- **FR-LC-016** — The #22 retrofit MUST be base-locale-identity-neutral: `InteractionTextGenerator.Generate`
  returns native values, the corpus (template rows **and** the per-`EventKind` clause table) migrates to
  #49, and the rendered base-locale output is byte-identical to today's (§3 / Appendix C).

**Determinism / persistence (KD-7 / KD-4)**
- **FR-LC-017** — #49 MUST register no RNG stream, no `DOMAIN_TAG_*`, no `SubsystemOrdinal`, hold no
  persistent sim state, and bump no save format.
- **FR-LC-018** — Locale selection and a11y options MUST be client-local settings outside the determinism
  save.
- **FR-LC-019** — The a11y boundary MUST be a read-only presentation settings value (text scale / contrast
  / colourblind-safe palette / input assist) with no sim reference and no save impact. The option content +
  settings store are deferred to Wave 8.
- **FR-LC-020** — `LocalizedTextRequest.SelectionDraw` MUST be the `ulong` value returned by
  `DeterministicRngService.DrawReserved` (the `world.text` reservation), carried verbatim.

## 2.2 Data structures

**The core seam is producer-agnostic (references nothing sim-side — the #38 rule, §4.1). The #22 coupling
lives in a per-producer boundary adapter (§2.2.1), the only thing that references `living-world`.**

```csharp
// --- CORE seam (in TacticalDirector.Localization) — references NO sim assembly ---
public interface ILocalizer
{
    string Resolve(LocalizationKey key);                 // static string
    string Render(in LocalizedTextRequest req);          // procedural text — generic input only
}

public readonly struct LocalizationKey { /* stable identity for one static string */ }

public readonly struct TextTemplateId
{
    // GENERIC identity: (producerTag, localOrdinal) — NOT modeled on any one producer's enum.
    public readonly int ProducerTag;      // which producer family (e.g. living-world interactions)
    public readonly int LocalOrdinal;     // the producer's own id within that family
}

public readonly struct LocalizedTextRequest
{
    public readonly TextTemplateId Id;
    public readonly ulong SelectionDraw;      // the world.text draw, verbatim (FR-LC-020)
    // producer-AGNOSTIC slot representation (a small named-value set) — NOT fixed subject/opponent/score
    // fields (those are #22-specific; #35/#46 carry disjoint slots). {score} is DERIVED at render from
    // the numeric slots via InvariantCulture (§3.5), not a pre-formatted string.
    public readonly NamedSlotSet Slots;
    public readonly bool HasCitedEpisode;
    public readonly int CitationKind;         // the producer's clause key (e.g. #22 EventKind ordinal); selects the clause (FR-LC-010)
}

// content (in TacticalDirector.Localization): per-locale keyed static strings + per-(Id, variant) templates
// + variantCount(Id) + per-clause-key clauses. Base-locale = the migrated InteractionTextCorpus content.
public sealed class TemplateCatalogue { /* ... */ }
```

### 2.2.1 Per-producer boundary adapter (the ONLY sim-side reference)

```csharp
// --- BOUNDARY adapter (composition layer; references BOTH living-world AND Localization) ---
// The one place a #22 native value maps into the generic request. #35/#46 add SIBLING adapters
// without touching the core seam above (FR-LC-013/014; §7.3).
public static class LivingWorldTextBoundary
{
    public static TextTemplateId ForInteraction(InteractionIntent intent);          // maps enum -> (tag, ordinal)
    public static LocalizedTextRequest BuildRequest(InteractionIntent intent, ulong draw, in InteractionSlots slots);
    // convenience: BuildRequest(...) then ILocalizer.Render(req)
    public static string Render(ILocalizer loc, InteractionIntent intent, ulong draw, in InteractionSlots slots);
}
```

## 2.3 Failure modes

| ID | Condition | Handling |
|---|---|---|
| **F1** | An undefined / `None` `TextTemplateId` reaches the renderer (`variantCount == 0`) | **Cannot occur** — the producer refuses it pre-draw (FR-LC-015 intent-value gate), and FR-LC-008a construction coverage guarantees every admitted defined id has `variantCount ≥ 1`. |
| **F2** | Missing locale / `(Id, variant)` **translation** / **non-base** clause | Render the base-locale identity (FR-LC-011); no throw, no mutation. (A missing **base-locale** row/clause for a defined id is F5, caught at construction — not this graceful path.) |
| **F3** | A producer emits a baked, human-readable localized string | **Coverage-lock failure** — a routing check at the producer's spec (FR-LC-002); not a runtime path in #49. |
| **F4** | `currentLocale` set to an unknown locale | Falls back entirely to the base locale (KD-5); no throw. |
| **F5** | Catalogue fails **roster coverage** at construction (FR-LC-008a: a defined `InteractionIntent` with no base-locale template row, or a defined citable `EventKind` with no base-locale clause; a defined id with an explicit 0-count row) | **Fail loud** at catalogue construction (the authoring boundary), never a silent render-time default or a consumed-cursor-then-fail. |
| **F6** | A sim/loop assembly references `TacticalDirector.Localization` | **Build error** (asmdef direction, FR-LC-012). |

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-23 | — | Initial FR set (FR-LC-001..020), data structures, failure modes F1–F6. Status IN REVIEW. |
| 0.2 | 2026-07-23 | — | Section-file PASS-1 fixes: H-1 generic core / per-producer boundary-adapter split (§2.2 core references nothing sim-side; §2.2.1 `LivingWorldTextBoundary`); M-1 FR-LC-008a construction-time roster-coverage invariant + F1/F5 rewrite + FR-LC-015 intent-value gate; L-1 `{score}` derived → AR-2 convergence; APPROVED. See section-9 §9.3.1. |
#endregion
