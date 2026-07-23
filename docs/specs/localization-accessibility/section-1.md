# Localization & Accessibility #49 — Section 1: Introduction, Scope, Dependencies, Key Decisions

**Created:** July 23, 2026
**Last Updated:** July 23, 2026 (v0.2 — section-file PASS-1 (1H+1M+1L) → AR-2 convergence; APPROVED)
**Version:** 0.2
**Status:** APPROVED
**Source:** `docs/tracking/localization-seam-template-design.md` v0.2

---

## 1.1 Introduction

Localization & Accessibility (#49) supplies the **one seam** every user-facing string routes through and
the **template/slot contract** procedural producers emit through. It is **presentation/content layer**: it
turns sim-produced *identities and facts* into localized *surface strings*, and it computes no game state.
The rule that shapes the spec is settled before any requirement: **the localization transform runs
display-side, strictly after deterministic generation** — so a producer's serialized state and its
deterministic draw stay locale-independent and a save round-trips byte-identically across display locales
(§1.6 KD-2).

This spec is the **seam + template-contract slice** (KD-6 scope). It defines the seam, the template model,
the reference direction, the fallback policy, and the **retrofit of the one built text producer** (#22
`InteractionTextGenerator`). Translated **locales** and the **accessibility content surface** are Wave 8.
It is authored as a **forward design** (nothing built yet — the #21–#38 posture); T-phase code + the #22
retrofit + Wave-8 content are post-APPROVED follow-ups.

## 1.2 Cadence and layer

Localization is a **display-time lookup** — it advances no sim tick and draws from no stream; the only
relevant draw is #22's existing `world.text` reservation, which stays sim-side and unchanged (§1.6 KD-2/
KD-7). It is presentation/content infrastructure (Code Standards #20 §3.5.2 layer taxonomy), **not**
zero-allocation game-loop code.

## 1.3 Scope

**In scope (Wave 1 — the seam + contract):**
- The **seam** — `ILocalizer` with `Resolve(LocalizationKey)` (static) + `Render(LocalizedTextRequest)`
  (procedural). All user-facing text routes through it (KD-1).
- The **template model** — named-placeholder substitution + a bounded plural/gender selector (KD-3).
- The **localize-after-generate boundary** — the determinism contract (KD-2).
- The **reference direction / seam placement** — the `TacticalDirector.Localization` assembly and its
  one-way references (KD-6).
- The **fallback policy** for a missing key/locale/variant (KD-5).
- The **#22 retrofit** — how the one built producer emits through the seam without a sim→presentation
  reference, base-locale-identity-neutral (KD-6 / §3 / Appendix C).

**Out of scope — deferred to a named later wave/spec:**
- Any **translated locale** beyond the base locale (Wave 8 content). The base locale = today's English
  strings, byte-for-byte.
- The **accessibility content surface** proper — the option catalogue, its client-settings store, the
  colourblind-safe palette content (Wave 8). This slice records only the a11y **boundary** (KD-4).
- The **text producers themselves** — #35 media, #46 news/inbox, #38 static UI strings are authored in
  their own waves and bind to this seam as they land (KD-1; §7.2). #49 supplies the routing seam; it never
  generates content.

## 1.4 The producer reality (verified against source)

- **Exactly one user-facing text producer exists today, and it bakes English strings.**
  `src/living-world/InteractionTextGenerator.cs` — `Generate(InteractionIntent intent, in InteractionSlots
  slots) → string`: (1) `templates = InteractionTextCorpus.TemplatesFor(intent)` (an **in-code English
  corpus**), (2) one reserved **`world.text`** draw selects `templates[draw % templates.Length]`, (3)
  `Expand` does `.Replace("{subject}", …).Replace("{opponent}", …).Replace("{score}", …)`, optionally
  appending `EpisodeClause(slots.CitedEpisode.Kind)`. So the project **already** has a template + named-slot
  model — it just bakes the *English* surface inline. This is the seam #49 formalizes and the one producer
  it retrofits.
- **`InteractionSlots`** (verified): `{ string SubjectName; string OpponentName; int HomeGoals; int
  AwayGoals; bool HasCitedEpisode; MemoryEpisode CitedEpisode }`. Slot tokens: `{subject}`, `{opponent}`,
  `{score}`.
- **Pre-draw validation is a documented determinism property.** `Generate` validates intent (`TemplatesFor`
  throws on `None`/out-of-roster), slots, and the §3.2 salience gate **before** the `world.text`
  reservation — a refused call consumes **no** cursor (slice-3 AR-1 L-3; the file comment "All refusals
  above run pre-draw so a failed call leaves the cursor untouched"). §3.4 preserves this after the corpus
  migrates.
- **The UI already declares it owns no strings.** #38 FR-UI-004: "The UI MUST compute no game state,
  analytics, or **localized text**." #38 §7.3 / KD-5: "**#49 localization** — localized strings the UI
  renders through #49's seam; **the UI holds no string catalogue (that is #49)**." #49 supplies exactly the
  contract #38 promised.

## 1.5 Dependencies

| Direction | Spec / surface | Nature |
|---|---|---|
| Upstream (needs) | #22 `living-world` — `InteractionIntent` + `InteractionSlots` facts + the `world.text` draw; the migrated `InteractionTextCorpus` content | renderer references the one built producer (KD-6) |
| Upstream (composes, as they land) | #38 static UI keys; #35 media; #46 news/inbox — each binds to the seam as it is authored | producers emit through the seam (KD-1) |
| Downstream (consumers) | UI screens (#38 Wave-7) render localized strings through the seam | read-only |
| Downstream (references #49) | **no sim/loop assembly** — top of the presentation graph (KD-6) |

## 1.6 Key decisions

**KD-1 — One seam, two emission shapes (load-bearing).** All user-facing text routes through `ILocalizer`.
A producer emits either a `LocalizationKey` (static) or its **native procedural values** — a
template-family identity + slot facts + the raw selection draw (the `LocalizedTextRequest` is assembled from
those at the #49 boundary, never by the producer — KD-6). **A producer that emits a baked, human-readable
localized string fails the seam** — the coverage lock (§5) is a routing check enforced at each producer's
own spec. #49 provides the *contract* and the *base-locale catalogue*; it never generates content.

**KD-2 — Localize-after-generate; the determinism boundary (load-bearing).** The transform is display-side,
strictly **after** deterministic generation. A producer's deterministic draw (#22's `world.text`
reservation) and its serialized state (memory episodes, the RNG cursor) are **locale-independent** — they
carry identities and facts, never localized strings. The renderer consults the locale only to turn an
already-decided `(TextTemplateId, variant, slots)` into a surface string. Therefore **a save round-trips
byte-identically across display locales** (`WorldStore.Snapshot`/`Restore`, the `world.text` cursor, and
the season save are untouched). Localizing *before/inside* generation would make `world.text` output
locale-dependent and break save round-trip; the boundary forbids it structurally — the producer has no
locale to localize with. Changing the base-locale content re-selects display text but does **not** alter
what is serialized (a display transform), so **no** `SNAPSHOT_SCHEMA_VERSION` / `WORLD_STORE_FORMAT_VERSION`
/ `SEASON_SAVE_FORMAT_VERSION` impact.

**KD-3 — Template model: named-placeholder + bounded plural/gender.** The model is named-placeholder
substitution (`{subject}`, `{opponent}`, `{score}`, …) **plus a bounded grammatical selector**: a template
MAY declare a plural/gender category keyed on a slot (CLDR-style `one`/`few`/`many`/`other` + a small
gender set) to choose among sub-forms of the same variant; it MUST NOT require arbitrary runtime morphology
(case-declension synthesis, agreement engines). Base-locale English declares no categories → identity with
today's `.Replace` behaviour. Deeper grammar is a Stage-3+ deferral (§7).

**KD-4 — Accessibility: record the boundary only (content is Wave 8).** A11y is a presentation-side,
client-local concern (text scale, high-contrast + colourblind-safe palette reusing the `dataviz` colour
discipline, input assist) exposed to the UI as a read-only settings value with **no sim reference and no
determinism-save impact** (the #38 client-preferences class). The option catalogue + settings store +
palette content are Wave 8. This slice pins only that a11y, like localization, is display-time and never
touches serialized state.

**KD-5 — Fallback: stable default, never crash, never mutate.** A missing key, locale, `(Id, variant)`, or
clause renders the **base-locale identity**, never a crash and never a state mutation. Precedence:
`currentLocale` → `BaseLocale` → (dev builds only) a visible `‹key›` marker; **production always falls
through to the base locale**. Because a missing translation resolves to base-locale English — today's exact
strings — **base-locale identity is the correctness anchor**: with only the base locale loaded, every
rendered string is byte-identical to today's output (§3 / Appendix C).

**KD-6 — Seam placement / one-way reference direction (load-bearing for layering).** The renderer
(`ILocalizer` + the base-locale catalogue + all locale data) lives high, in a new presentation/content
assembly `TacticalDirector.Localization` (`src/localization/`). One-way, exactly the #38 rule: **no
sim/loop assembly references it** (a sim-side producer like #22 must not gain a presentation reference); the
renderer references only **built** producers (at Stage 2: `Localization → living-world`, one reference — a
reference to an unbuilt producer is the FR-LW-031 phantom-dependency class). A producer emits only its own
native values; `LocalizedTextRequest`/`TextTemplateId` are #49 types assembled at the #49 boundary
(`TextTemplateId.ForInteraction(intent)`), so a producer never references a #49 type. The **pre-draw
validation stays sim-side** (§1.4 / §3.4).

**KD-7 — No determinism identifiers (the #37/#38 posture).** #49 registers no RNG stream, no `DOMAIN_TAG_*`,
no `SubsystemOrdinal`, holds no persistent sim state, and bumps no save format. It appears nowhere in the
#16 §3.4 catalogue and warrants no `_RESERVED_` placeholder — a positive property.

## 1.7 Boundary matrix

| Concern | Owner | #49's relationship |
|---|---|---|
| Game state / sim facts / the `world.text` draw | the sim (#22 etc.) | **reads** identities/facts (produces none) |
| The surface-string content (templates, clauses, UI strings) | #49 (base-locale) + Wave-8 locales | **owns** the catalogue |
| The deterministic selection (the draw) | the producer (sim-side) | **reproduces** it display-side (`% variantCount`) |
| Translated locale content / a11y option content | Wave 8 | **out of scope** (this slice = seam + contract) |
| The text producers (#35/#46/#38-static) | their own specs | **out of scope** — they bind to the seam (KD-1) |

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-23 | — | Initial section from the converged supplement. Scope/deps/KD-1..7/boundary matrix, grounded in `InteractionTextGenerator`/`InteractionSlots`/`InteractionTextCorpus` + #38 FR-UI-004/KD-5. Status IN REVIEW. |
| 0.2 | 2026-07-23 | — | Section-file PASS-1 (1H+1M+1L; H-1 generic-core / per-producer boundary-adapter split, M-1 FR-LC-008a construction-time roster-coverage invariant, L-1 `{score}` derived) → AR-2 convergence; APPROVED. See section-9 §9.3.1. |
#endregion
