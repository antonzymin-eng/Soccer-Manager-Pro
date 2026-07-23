# Localization & Accessibility #49 — Section 3: The Seam, the Determinism Boundary, the Template Model

**Created:** July 23, 2026
**Last Updated:** July 23, 2026 (v0.3 — repeat AR-3 (1H+1L) fix pass; APPROVED)
**Version:** 0.3
**Status:** APPROVED

---

The "algorithms" here are the one seam (`Resolve` + `Render`), the localize-after-generate boundary, and
the template model. All are testable without Unity (KD-4 / §5).

## 3.1 The seam (KD-1)

Two emission shapes, one renderer.

**Static strings:** a producer emits a `LocalizationKey`; the renderer returns a keyed string (no slots).

**Procedural text:** a producer emits **only its own native values** (never a #49 type — KD-6):
- its native template-family identity (e.g. #22's `InteractionIntent`),
- the slot facts (the `InteractionSlots` generalization) **plus** the citation `(hasCitedEpisode,
  EventKind citationKind)`,
- the raw `ulong` selection draw (#22's `world.text` value, verbatim).

The **per-producer boundary adapter** (`LivingWorldTextBoundary`, §2.2.1 — a composition-layer assembly that
references both the producer and `Localization`; the generic core references neither producer) assembles
those into a generic `LocalizedTextRequest` via `LivingWorldTextBoundary.ForInteraction(intent)`. The
producer never constructs a `LocalizedTextRequest` or `TextTemplateId`, and the core seam names no
producer type (§4.1).

## 3.2 The renderer (`ILocalizer`) — the single seam

```
Resolve(key):
    template = catalogue[currentLocale].static[key] ?? catalogue[BaseLocale].static[key]   # KD-5 fallback
    return template                                                                        # no slots

Render(req):
    n        = catalogue[BaseLocale].variantCount(req.Id)          # locale-INDEPENDENT count (KD-2); n >= 1 (§3.4)
    variant  = (int)(req.SelectionDraw % (ulong)n)                # the selection, reproduced display-side
    template = catalogue[currentLocale].template(req.Id, variant) # KD-5 fallback to BaseLocale per (Id, variant)
    text     = Expand(template, req.Slots)                        # pure string placeholder substitution over the NamedSlotSet (§3.5)
    if req.HasCitedEpisode:                                       # citation clause — selected by EventKind, NOT the draw
        # producer-scoped by req.Id.ProducerTag so two producers' clause keys never collide:
        clause = catalogue[currentLocale].clause(req.Id.ProducerTag, req.CitationKind)
              ?? catalogue[BaseLocale].clause(req.Id.ProducerTag, req.CitationKind)
        text   = text + " " + clause                             # matches InteractionTextGenerator's `text + " " + clause`
    return text
```

The renderer is the **one place a locale is consulted and a surface string is produced** (FR-LC-001). The
draw is emitted raw and the modulo happens here because the *selection* must be reproducible from serialized
state while the variant **count** is locale-independent content owned by #49.

## 3.3 The localize-after-generate boundary (KD-2)

The transform is display-side and runs strictly **after** generation. The producer's deterministic draw and
serialized state carry identities and facts, never localized strings, so:
- the `world.text` cursor advances by exactly one reservation per interaction regardless of locale (the
  serialized state is locale-independent), and
- a save round-trips byte-identically across display locales (FR-LC-006); only the rendered surface differs.

The variant **count** used by the modulo is base-locale content. Changing the base-locale corpus re-selects
display text but does **not** alter what is serialized — a display transform (KD-2). Localizing
before/inside generation would make `world.text` output locale-dependent and break save round-trip; the
boundary forbids it structurally (the producer has no locale).

## 3.4 The pre-draw validation split + the citation clause (grounded in `InteractionTextGenerator`)

Two properties of the one built producer bind the contract:

- **Slot / salience validation stays pre-draw, sim-side; the intent gate is reconstructed as a
  roster-value check.** Today `Generate` validates the intent (`TemplatesFor` throws on `None`/out-of-roster),
  the slots, and the §3.2 salience gate **before** the `world.text` reservation, so a refused call consumes
  **no** cursor (the documented replay-parity invariant — slice-3 AR-1 L-3). The retrofit (§4) migrates the
  template *content* + counts to #49 — so `TemplatesFor` (a **corpus-coverage** check, `ordinal <
  corpus.Length`) leaves `living-world`. The slot + salience gates are checks on **sim facts** —
  locale-independent — so they stay sim-side, pre-draw, unchanged. The intent gate is **reconstructed** as
  an **intent-VALUE roster check** (`None` + `Enum.IsDefined`/max-defined-ordinal on the `living-world`-owned
  `InteractionIntent` enum) — locale-independent, needs no corpus, and stays sim-side, pre-draw (FR-LC-015).
  This does **not** by itself guarantee a template exists (an enum member could lack a #49 row); that gap is
  closed by the **construction-time coverage invariant** (FR-LC-008a / F5): the base-locale catalogue MUST
  cover every defined `InteractionIntent` (≥1 template) and every defined citable `EventKind` (a clause),
  asserted fail-loud at catalogue construction. Together: the sim-side gate admits only defined intents, and
  every defined intent has `variantCount(Id) ≥ 1` **by construction**, so `draw % variantCount` never
  divides by zero and the no-cursor-on-refusal invariant is preserved (a `None`/undefined intent is refused
  pre-draw and never reaches the renderer; F1). Without FR-LC-008a the split would allow a consumed-cursor-
  then-fail on an enum member the catalogue forgot — the coverage invariant is what makes the split safe.

- **The citation clause is a second localizable string, selected by `EventKind`, not the draw.** `Generate`
  optionally appends `EpisodeClause(slots.CitedEpisode.Kind)` — a full authored sentence per `EventKind`
  (six defined kinds). This clause content **also migrates to #49** (a per-`EventKind` clause table) and
  localizes independently of the main template. The emission carries `(hasCitedEpisode, citationKind)`; the
  renderer appends `clause(Id.ProducerTag, citationKind)` — **producer-scoped** (§3.2), so a second
  producer's clause keys never collide with #22's, base-locale fallback KD-5. Because the clause is keyed by
  `EventKind` (a sim fact, not the draw), it is locale-independent selection — KD-2-safe (FR-LC-010).

## 3.5 The template model (KD-3)

`Expand(template, slots)` performs **pure string** named-placeholder substitution over the
producer-agnostic `NamedSlotSet` (an immutable `name → string` map) — the `{subject}`/`{opponent}`/`{score}`
set today, extensible with new placeholders. The generic expander does **no** producer-specific formatting:
each placeholder is replaced by its already-formatted string slot value. **`{score}` is a *derived*
placeholder, derived in the boundary adapter, not the renderer** — `LivingWorldTextBoundary` (§2.2.1), which
holds the typed `InteractionSlots` and owns the #22 score-format knowledge, computes `score =
HomeGoals.ToString(InvariantCulture) + "-" + AwayGoals.ToString(InvariantCulture)` and puts a plain `score`
string slot into the `NamedSlotSet`. So the generic `Expand` substitutes `{score}` like any other string
slot, base-locale expansion is byte-identical to today's `Expand` (FR-LC-016 — the boundary formats exactly
as `InteractionTextGenerator.Expand` did), and the numeric-score formatting stays with the producer that
owns the concept (not leaked into the generic core — the KD-6 / #38 boundary this spec pins). Locales
localize the surrounding template text, **not** the numeric score glyph. Beyond substitution, `Expand`
applies an optional bounded grammatical selector: a template
MAY declare a plural/gender category keyed on a slot (CLDR-style `one`/`few`/`many`/`other` + a small gender
set) so a locale chooses among sub-forms of the same variant. It MUST NOT require arbitrary runtime
morphology. Base-locale English declares no categories, so `Expand` reduces to today's `.Replace` behaviour
(identity). Deeper grammar (case declension synthesis, agreement engines) is a Stage-3+ deferral (§7),
recorded so a locale author cannot silently expand the model.

## 3.6 Worked render (base locale, matching today's output)

`InteractionIntent.PlayerQuestionsMinutes` (ordinal 2; base-locale has 2 templates), draw `= 5`, slots
`{ SubjectName="Rooney", OpponentName="Everton", HomeGoals=2, AwayGoals=1, HasCitedEpisode=false }`:
- `n = variantCount(BaseLocale, Id) = 2`; `variant = 5 % 2 = 1`.
- base-locale template[1] = `"{subject} wants a word about playing time after the {score} against {opponent}."`
- `Expand` → `"Rooney wants a word about playing time after the 2-1 against Everton."`
- no cited episode → no clause appended.

This is byte-identical to `InteractionTextGenerator.Generate` for the same `(intent, draw, slots)` because
the migrated corpus preserves the template row and count and the draw is unchanged (Appendix C).

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-23 | — | Initial contracts: the seam (Resolve/Render), localize-after-generate boundary, pre-draw validation split + citation clause, template model, worked render. Status IN REVIEW. |
| 0.2 | 2026-07-23 | — | Section-file PASS-1 (1H+1M+1L; H-1 generic-core / per-producer boundary-adapter split, M-1 FR-LC-008a construction-time roster-coverage invariant, L-1 `{score}` derived) → AR-2 convergence; APPROVED. See section-9 §9.3.1. |
| 0.3 | 2026-07-23 | — | Repeat AR-3 (1H+1L): H — `{score}` derivation moved to the boundary adapter (was leaking #22 formatting into the generic renderer); `NamedSlotSet` defined as immutable name→string; generic `Expand` is pure string substitution. L — clause lookup producer-scoped by `(Id.ProducerTag, CitationKind)`. See section-9 §9.3.1. |
#endregion
