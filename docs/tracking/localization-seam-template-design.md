# Localization Seam + Template Contract #49 — Design Supplement

> **Created:** July 23, 2026
> **Status:** DESIGN SUPPLEMENT (pre-promotion; same governance class as `match-analytics-statistics-design.md` / `ui-client-framework-design.md`).
> **Candidate spec:** #49 (Localization & Accessibility). **This supplement scopes the Wave-1 tier only** — the localization **seam + template contract**. Locales, translated content, and the a11y content surface are **Wave 8** (out of scope here; §0).
> **Wave:** 1 (final item). **FR prefix (proposed):** FR-LC.
> **Determinism:** none — read-only display transform. No RNG stream, no `DOMAIN_TAG_*`, no `SubsystemOrdinal` (the #37 / #38 posture).

---

## 0. Scope (what this supplement designs, and what it does not)

**In scope (Wave 1 — seam + contract):**
- The **one localization seam** every user-facing string routes through: a keyed lookup for static strings + a template/slot model for procedurally generated text (KD-1).
- The **localize-after-generate boundary** that keeps procedural determinism (#22's `world.text` draw + serialized memory) locale-independent (KD-2).
- The **template model** the seam adopts (named-placeholder substitution + a bounded plural/gender selector) (KD-3).
- The **seam placement / reference direction** — how a sim-side producer (#22) emits through the seam without a sim→presentation reference (KD-6).
- The **fallback policy** for a missing key/locale (KD-5).
- The **retrofit of the one built producer** (#22 `InteractionTextGenerator`) as a T-phase (KD-2 / KD-6).

**Explicitly OUT of scope (deferred to Wave 8 — content tier):**
- Any **translated locale** beyond the base locale (the base locale = today's English strings, byte-for-byte).
- The **accessibility content surface** proper — text-scale/contrast/colourblind option *content* and its client settings store. The Wave-1 tier records only the a11y *boundary* (KD-4); the option catalogue + persistence are Wave 8.
- The **text producers themselves** (#35 media, #46 news/inbox, #38 screens) — they are authored in their own waves and bind to this seam as they land. #49 supplies the routing seam; it never generates content.

This mirrors #38 exactly (framework early, screens/content late) and #37 (read-only surface now, producer-gated depth later). Approving #49's Wave-1 tier as a **forward design** (T-phase code deferred) follows the #21–#38 precedent.

---

## 1. The problem, grounded in real source

Two facts from the codebase set every decision below.

**(a) Exactly one user-facing text producer exists today, and it bakes English strings.**
`src/living-world/InteractionTextGenerator.cs` (`Generate(InteractionIntent intent, in InteractionSlots slots) → string`):
1. `templates = InteractionTextCorpus.TemplatesFor(intent)` — an **in-code English corpus** (`src/living-world/InteractionTextCorpus.cs`).
2. one reserved **`world.text`** draw (`_rng.Reserve/DrawReserved/CloseReservation`) selects `template = templates[draw % templates.Length]`.
3. `Expand(template, slots)` does `.Replace("{subject}", slots.SubjectName).Replace("{opponent}", …).Replace("{score}", …)`, optionally appending an `EpisodeClause`.

So the project **already** has a template + named-slot model (`{subject}`/`{opponent}`/`{score}` over `InteractionSlots { SubjectName, OpponentName, HomeGoals, AwayGoals, HasCitedEpisode, CitedEpisode }`). It just bakes the *English* surface string inline. **This is the seam #49 formalizes and the one producer it retrofits.**

**(b) The UI already declares it owns no strings.**
`docs/specs/ui-client-framework/` #38 FR-UI-004: "The UI MUST compute no game state, analytics, or **localized text**; it composes the read-only inputs." #38 §7.3 / KD-5: "**#49 localization** — localized strings the UI renders through #49's seam; **the UI holds no string catalogue (that is #49)**." So #38 is already written *against* this seam existing. #49 must supply exactly the contract #38 promised.

**The load-bearing invariant (from the plan):** *all* user-facing text — static UI strings **and** procedurally generated text — routes through **one** seam, and the localization transform runs **after** deterministic generation, so a save round-trips identically regardless of display locale.

---

## 2. The seam (KD-1)

Two emission shapes cover every producer; one renderer resolves both.

**Static strings** (UI labels, menu items, fixed notifications):
- A producer emits a **`LocalizationKey`** — a stable identity for one string (a keyed lookup, no slots).
- Renderer: `string Resolve(LocalizationKey key)`.

**Procedural text** (#22 interactions today; #35/#46 later):
- A producer emits **only values it already owns** — never a baked string, and never a `TacticalDirector.Localization` type (KD-6):
  - its native **template-family identity** (e.g. #22's `InteractionIntent`),
  - a **slot carrier** of pre-computed sim facts (the `InteractionSlots` generalization — named placeholders → values, **plus** the citation `(hasCitedEpisode, EventKind citationKind)` — see §2.1),
  - the **raw `ulong` selection draw** the producer already made deterministically (#22's `world.text` value, verbatim).
- The **#49 boundary** (the composition/UI layer that references both the producer and `Localization`) assembles those native values into a `LocalizedTextRequest = { TextTemplateId Id; ulong SelectionDraw; <slot facts + citationKind> }` via a boundary factory (`TextTemplateId.ForInteraction(intent)`); the producer never constructs a `LocalizedTextRequest` or a `TextTemplateId` (both are #49-owned types — KD-6).
- Renderer: `string Render(in LocalizedTextRequest req)`, plus a convenience boundary overload `Render(InteractionIntent intent, ulong draw, in <slot facts>)` so a `living-world` result feeds the seam with no reverse reference.

**The renderer (`ILocalizer`) is the single seam** — the one place a locale is consulted and a surface string is produced. Its Stage-2 body:
```
Resolve(key):
    template = catalogue[currentLocale].static[key]   ?? catalogue[BaseLocale].static[key]   # KD-5 fallback
    return template                                    # static keys have no slots

Render(req):
    n       = catalogue[BaseLocale].variantCount(req.Id)          # locale-INDEPENDENT count (KD-2); n >= 1 (precondition, §2.1)
    variant = (int)(req.SelectionDraw % (ulong)n)                # the selection, reproduced display-side (ulong modulo, then narrow)
    template = catalogue[currentLocale].template(req.Id, variant) # KD-5 fallback to BaseLocale per (Id,variant)
    text     = Expand(template, req.slots)                        # named-placeholder + plural/gender (KD-3)
    if req.hasCitedEpisode:                                       # the §3.2 citation clause — selected by EventKind, NOT the draw
        clause = catalogue[currentLocale].clause(req.citationKind) ?? catalogue[BaseLocale].clause(req.citationKind)  # KD-5
        text   = text + " " + clause                             # matches InteractionTextGenerator's `text + " " + clause`
    return text
```

**Why the draw is emitted raw and the modulo happens in the renderer:** the *selection* must be reproducible from serialized state, and the variant **count** is locale-independent content owned by #49. The producer advances its `world.text` cursor by exactly one draw regardless of locale or catalogue size (the cursor is the serialized state — see KD-2); the renderer maps that draw to a variant against the **base-locale** count. Every locale MUST supply `variantCount(Id)` templates for the base count (missing ones fall back per KD-5), so the variant index is stable across locales and the draw space never depends on which locale is displayed.

### 2.1 The citation clause + the pre-draw validation split (grounded in `InteractionTextGenerator`)

Two properties of the one built producer bind the contract:

- **The episode citation clause is a *second* localizable string, selected by `EventKind`, not by the draw.** `InteractionTextGenerator.Generate` optionally appends `InteractionTextCorpus.EpisodeClause(slots.CitedEpisode.Kind)` — a full authored sentence chosen by the cited episode's `EventKind` (six defined kinds), appended as `text + " " + clause`. This clause is content that **also migrates to #49** (a per-`EventKind` clause table in the base-locale catalogue) and localizes independently of the main template. The emission carries `(hasCitedEpisode, citationKind)`; the renderer appends `clause(citationKind)` (base-locale fallback, KD-5). Because the clause is keyed by `EventKind` (a sim fact, not the draw), it is locale-independent selection — KD-2-safe.

- **Intent-roster + slot + salience validation stays pre-draw in `living-world`.** Today `Generate` validates the intent (`TemplatesFor` throws on `None`/out-of-roster), the slots, and the §3.2 salience gate **before** the `world.text` reservation, so a refused call consumes **no** cursor (the documented replay-parity invariant — slice-3 AR-1 L-3; the file comment "All refusals above run pre-draw so a failed call leaves the cursor untouched"). The retrofit (KD-6) migrates the template *content* + counts to #49, but these gates are checks on the **intent value and sim facts** — locale-independent, not catalogue content — so they **stay sim-side, pre-draw**, preserving the no-cursor-on-refusal invariant exactly. The renderer therefore only ever receives a request for a **defined** intent, and by the APPEND-only ≥1-template contract every defined intent has `variantCount(Id) ≥ 1` — so `draw % variantCount` never divides by zero (the `None` row, `Array.Empty`, is refused pre-draw and never reaches the renderer). A request for an undefined/`None` id is a caller error the producer already refused, not a renderer path.

---

## 3. Key decisions

### KD-1 — One seam, two emission shapes (LOAD-BEARING)
All user-facing text routes through `ILocalizer` (§2). A producer emits either a `LocalizationKey` (static) or its **native procedural values** — a template-family identity + slot facts + the raw selection draw (the `LocalizedTextRequest` is assembled from those at the #49 boundary, never by the producer — §2/KD-6). **A producer that emits a baked, human-readable localized string fails the seam** — the coverage lock (§8) is a routing check, enforced at each producer's own spec. #49 provides the *contract* and the *base-locale catalogue*; it never generates content.

### KD-2 — Localize-after-generate; determinism boundary (LOAD-BEARING)
The localization transform is **display-side** and runs strictly **after** deterministic generation. Concretely:
- A producer's **deterministic draw** (#22's `world.text` reservation) and its **serialized state** (the memory episodes, the RNG cursor) are **locale-independent** — they carry *identities and facts*, never localized strings.
- The renderer consults the locale only to turn an already-decided `(TextTemplateId, variant, slots)` into a surface string.
- Therefore **a save round-trips byte-identically across display locales** (`WorldStore.Snapshot`/`Restore`, the `world.text` cursor, and the season save are untouched). This is the trap the plan's §5/§9 name: localizing *before/inside* generation would make `world.text` output locale-dependent and break save round-trip. The boundary forbids it structurally — the producer has no locale to localize with.

The variant **count** used by the modulo (§2) is base-locale content. Changing the base-locale corpus is the same class of change as changing any content artifact — it re-selects display text but **does not** alter what is serialized (the plan's KD-2: "localizing it is a display transform — it does not change what is serialized"). No `SNAPSHOT_SCHEMA_VERSION` / `WORLD_STORE_FORMAT_VERSION` / `SEASON_SAVE_FORMAT_VERSION` impact.

### KD-3 — Template model: named-placeholder + bounded plural/gender
The Stage-2 template model is: **named-placeholder substitution** (`{subject}`, `{opponent}`, `{score}`, … — a superset of #22's current slots) **plus a bounded grammatical selector**. A template MAY declare a **plural/gender category keyed on a slot** (CLDR-style plural categories — `one`/`few`/`many`/`other`, and a small gender set) so a locale can choose among sub-forms of the same template variant; it MUST NOT require arbitrary runtime morphology (case declension synthesis, agreement engines). This bounds the "grammar depth can balloon" risk (plan §9): the placeholder set + a fixed category selector is the whole model; anything deeper is a Stage-3+ deferral recorded in §7 of the eventual spec. #22's `{subject}`/`{opponent}`/`{score}` are the initial placeholder set; the selector is unused until a locale needs it (base-locale English declares no categories → identity with today's `.Replace` behaviour).

### KD-4 — Accessibility: record the boundary only (content is Wave 8)
The Wave-1 tier records the a11y **boundary**, not the option content: accessibility is a **presentation-side, client-local** concern (text scale, high-contrast + colourblind-safe palette reusing the `dataviz` colour discipline, input assist) exposed to the UI as a read-only settings value with **no sim reference and no determinism-save impact** (the #38 "UI preferences/layout are client-local settings outside the determinism save" class). The option **catalogue + its client-settings store + the palette content** are Wave 8. This keeps KD-4 from pulling content scope into the seam tier while pinning that a11y, like localization, is a display-time concern that never touches serialized state.

### KD-5 — Fallback policy: stable default, never crash, never mutate
A missing key, missing locale, or missing `(Id, variant)` template renders the **base-locale identity** (the plan's "stable default"), never a crash and never a state mutation. Precedence: `currentLocale` entry → `BaseLocale` entry → (dev builds only) a visible `‹key›` marker to surface untranslated strings during authoring; **production always falls through to the base locale** (a visible marker would ship a broken string). Because a missing translation resolves to base-locale English — today's exact strings — **base-locale identity is the correctness anchor**: with only the base locale loaded, every rendered string is byte-identical to today's output (§8).

### KD-6 — Seam placement / reference direction (LOAD-BEARING for layering)
The renderer (`ILocalizer` + the base-locale catalogue + all locale data) lives **high** in the graph, in a new **presentation/content assembly `TacticalDirector.Localization` (`src/localization/`)**. The **reference direction is one-way**, exactly the #38 rule:
- **No sim/loop assembly references `TacticalDirector.Localization`** (enforced by the absence of the reverse reference — the `match-viewer`/`ui-framework` no-reverse-reference lock). A sim-side producer like #22 (`living-world`, references only `DeterministicSim`) must **not** gain a reference to the presentation layer.
- **The renderer references each *built* producer** to consume the producer's native identity/slot types — the #38 "concrete surface references only assemblies that already exist" rule. At Stage 2 that is exactly **one** reference: `Localization → living-world` (to read `InteractionIntent` + the interaction slot facts). A reference to #35/#46/#38-static is added **only when that producer is built** — never speculatively (the FR-LW-031 phantom-dependency rule).
- The generic seam surface (`ILocalizer`, `LocalizationKey`, `TextTemplateId`, `LocalizedTextRequest`) lives **in `TacticalDirector.Localization`**. `TextTemplateId` is constructed **at the renderer boundary** from the producer's emitted values (e.g. `TextTemplateId.ForInteraction(intent)`), so a producer never needs to reference a `TextTemplateId` type — it emits only types it already owns (its intent enum + its slot facts + its draw). This is what lets #22 stay free of any presentation reference while still routing through the seam.

**Consequence for #22's retrofit (T-phase):** `InteractionTextGenerator.Generate` stops returning a baked English string and instead **returns the deterministic native values `(intent, selectionDraw, slots incl. hasCitedEpisode + citationKind)`** — all types `living-world` already owns (it never references `TacticalDirector.Localization`, KD-6). The English corpus `InteractionTextCorpus` (both the per-intent template rows **and** the per-`EventKind` clause table) **migrates into `TacticalDirector.Localization` as the base-locale catalogue** (content is #49's, per #38 KD-5). The `world.text` **draw stays in `living-world`** (it advances the serialized cursor — it must stay sim-side for determinism), and — critically — **the intent-roster + slot + salience validation stays pre-draw in `living-world`** (§2.1), so the documented "refused call consumes no cursor" replay-parity invariant is preserved after the corpus leaves (only the *content* migrates; the *gates* on intent value + sim facts are locale-independent and stay sim-side). A caller wanting the surface string builds a `LocalizedTextRequest` at the #49 boundary and calls `ILocalizer.Render` (or the `Render(intent, draw, facts)` convenience overload). Because the draw, the serialized memory, and the base-locale content (template rows + clauses + counts) are all preserved, the rendered output is **identical** to today's (KD-2 / §8) — the retrofit is behaviour-neutral at the base locale despite changing #22's public return type.

### KD-7 — No determinism identifiers (the #37/#38 posture)
#49 registers **no** RNG stream, **no** `DOMAIN_TAG_*`, **no** `SubsystemOrdinal`, holds **no** persistent sim state, and bumps **no** save format. It is a display transform over sim-produced identities/facts; it draws nothing (the one relevant draw is #22's existing `world.text`, unchanged and owned by `living-world`) and advances nothing. It appears nowhere in the #16 §3.4 catalogue and warrants no `_RESERVED_` placeholder (a positive property, like #37/#38).

---

## 4. Persistent state & save impact
Locale selection and a11y options are **client-local settings outside the determinism save** (the #38 client-preferences class). The string/template catalogue is a **content artifact**, not live game state. **No** `SNAPSHOT_SCHEMA_VERSION` / `WORLD_STORE_FORMAT_VERSION` / `SEASON_SAVE_FORMAT_VERSION` impact — KD-2 guarantees the serialized surface is locale-independent. A season save produced under one display locale loads and renders correctly under any other.

---

## 5. Primary surfaces (proposed — the eventual §4)

New assembly **`TacticalDirector.Localization`** (`src/localization/`). References at Stage 2: `living-world` (the one built producer) — nothing else sim-side, and referenced by no sim assembly.

| File (proposed) | Role |
|---|---|
| `ILocalizer.cs` | the single seam: `Resolve(LocalizationKey)` + `Render(in LocalizedTextRequest)` |
| `LocalizationKey.cs` | stable identity for a static string |
| `TextTemplateId.cs` | stable identity for a template family + boundary factories (`ForInteraction(InteractionIntent)`) — constructed at the #49 boundary from a producer's native id, never by the producer (KD-6) |
| `LocalizedTextRequest.cs` | template id + `ulong` selection draw + slot facts + `(hasCitedEpisode, citationKind)` (the renderer input, assembled at the #49 boundary — §2/§2.1) |
| `LocaleId.cs` | base + (Wave-8) locale identities; `BaseLocale` constant |
| `TemplateCatalogue.cs` | per-locale keyed static strings + per-`(Id, variant)` templates + `variantCount(Id)` + per-`EventKind` citation clauses; base-locale catalogue = the migrated `InteractionTextCorpus` content (template rows **and** clause table) |
| `TemplateExpander.cs` | KD-3 named-placeholder substitution + bounded plural/gender selector |
| `Localizer.cs` | `ILocalizer` impl: fallback precedence (KD-5) + the §2 `Render` algorithm |
| `A11yBoundary.cs` | KD-4 boundary marker (read-only presentation settings shape; **content/store deferred to Wave 8**) |
| `LocalizationConstants.cs` | `[FIXED] BaseLocale` etc.; no sim constants |

**Producer-side (T-phase, `living-world`):** `InteractionTextGenerator.Generate` returns a `(intent, selectionDraw, slots)` request instead of a baked string; `InteractionTextCorpus` content migrates to the base-locale `TemplateCatalogue`. No new `living-world` reference (it emits its own types).

**CS0104 note:** `LocalizationKey` / `TextTemplateId` / `LocalizedTextRequest` / `Localizer` are new names — a `docs/specs/**` + `src/**` grep at T0 must confirm no collision before wiring (the `TacticTranslation` / `PlayerAttributes` precedent); fully-qualify from line one if a future spec shares a name.

---

## 6. Test focus (the eventual §5)

- **Coverage lock (routing):** no user-facing string bypasses the seam — a producer emitting a baked, human-readable localized string fails a routing check (enforced at each producer's spec; #49's own test asserts the `ILocalizer` entry points are the sole surface-string source it exposes).
- **Base-locale identity (KD-5 anchor):** with only the base locale loaded, `Resolve`/`Render` produce **byte-identical** strings to today's output — the migrated corpus + the reproduced `draw % variantCount` selection reproduce `InteractionTextGenerator`'s exact result for the same `(intent, draw, slots)`.
- **Localize-after-generate (KD-2):** #22's `world.text` generation + serialized memory are **locale-independent** — a `WorldStore` / season save round-trips byte-identically across two display locales; the rendered strings differ, the serialized bytes do not.
- **Fallback fail-safe (KD-5):** a missing key / locale / `(Id, variant)` renders the base-locale default with no crash and no state mutation; a dev-marker surfaces only in dev builds.
- **Template model (KD-3):** named-placeholder substitution + a plural/gender selector select the correct sub-form; base-locale English (no categories declared) is identity with `.Replace`.
- **Determinism posture (KD-7):** #49 registers no stream/tag/ordinal and holds no persistent state (nothing to serialize to assert).

---

## 7. Risks & open questions (the eventual §7 / §9)

- **Producer discipline is the whole invariant (plan §9):** the single-seam guarantee is only as strong as each producer's emit-through-seam discipline. Enforced by the coverage lock at each producer's spec, not centrally — the eventual §1 of #49 cites KD-1 verbatim for #35/#46/#38 to bind to.
- **The retrofit changes #22's public return type** (baked string → structured request). Behaviour-neutral at the base locale (§8/KD-6) but a real API change — sequenced as a T-phase, forward-designed here like #37/#38 code.
- **Grammar depth (KD-3)** is deliberately bounded to named-placeholder + a fixed category selector; deeper morphology is a Stage-3+ deferral, recorded so a locale author cannot silently expand the model.
- **A11y content (KD-4)** is a boundary here, content in Wave 8 — the split keeps the seam tier from absorbing option-catalogue + settings-store scope.
- **Variant-count contract:** every locale must supply `variantCount(Id)` templates for the base count; a locale with fewer falls back per `(Id, variant)` to base (KD-5) — so a partial translation degrades gracefully, string-by-string, never crashing and never shifting the draw space.

---

## 8. The correctness anchor (why base-locale identity is provable)

The retrofit is behaviour-neutral because, for a fixed `(intent, selectionDraw, slots)`:
- today: `InteractionTextGenerator` validates the intent/slots/salience pre-draw, draws once, computes `variant = draw % (ulong)TemplatesFor(intent).Length`, picks the English template, `.Replace`-expands the slots, and — when `HasCitedEpisode` — appends `EpisodeClause(CitedEpisode.Kind)` as `text + " " + clause`.
- after: `living-world` runs the **same** pre-draw validation and the **same** single draw (§2.1), then emits `(intent, draw, slots incl. hasCitedEpisode + citationKind)`; the #49 boundary builds the request and `ILocalizer.Render` computes `variant = draw % (ulong)catalogue[BaseLocale].variantCount(intent)`, picks the **same** migrated English template (base-locale content == the migrated corpus), expands with the **same** placeholder set, and appends the **same** migrated clause by `citationKind`.

`variantCount(intent) == TemplatesFor(intent).Length` (the migrated corpus preserves counts), the clause table is migrated verbatim, and the draw is unchanged, so `variant`, the expanded slots, and the appended clause are identical → the surface string is byte-for-byte identical. The `world.text` cursor advances by exactly one reservation either way (pre-draw validation preserved, §2.1), so serialized state is untouched (KD-2). **Base-locale identity is thus a mechanical property of preserving the corpus + the clause table + the draw + the pre-draw gates**, and the acceptance test (§6) asserts it directly.

---

## 9. Promotion pipeline (the eventual §6 step 1)
1. This supplement → self-adversarial review to convergence.
2. Promote to the 11-file section set (`docs/specs/localization-accessibility/`, `IN REVIEW`), FR-LC-NNN.
3. Section-file PASS-1 adversarial review → AR-2 convergence.
4. R-01..R-05 lead-developer sign-off → APPROVED (forward design; T-phase code + the #22 retrofit + Wave-8 locale/a11y content are post-APPROVED follow-ups).
5. `SPEC_INDEX.md` registry row + header entry at promotion (the #37/#38 precedent — registry row lands at promotion, not at supplement stage).

---

## Version History
| Version | Date | Change |
|---|---|---|
| v0.1 | 2026-07-23 | Initial design supplement — Wave-1 localization seam + template contract (KD-1..KD-7), grounded in `InteractionTextGenerator`/`InteractionSlots` (the one built producer) + #38 FR-UI-004/KD-5. Scope: seam + contract + #22 retrofit as forward design; locales + a11y content deferred to Wave 8. |
| v0.2 | 2026-07-23 | Self-adversarial AR-1 (2H+1M+1L, all fixed). H-1: pinned the pre-draw validation split — intent-roster/slot/salience gates stay sim-side in `living-world` after the corpus migrates, preserving the no-cursor-on-refusal replay invariant + `variantCount ≥ 1` (no div-by-zero); new §2.1. H-2: resolved the §2↔KD-6 contradiction — the producer emits only its own native values; `LocalizedTextRequest`/`TextTemplateId` are assembled at the #49 boundary, not by the producer. M-1: the per-`EventKind` episode citation clause added to the contract (a second localizable string, selected by `EventKind` not the draw; migrates to #49, appended in `Render`). L-1: `ulong` draw width pinned to `DrawReserved`. §2/§5/§8 + KD-6 updated. |
