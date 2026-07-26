# Localization Content & Accessibility #49 (Wave-8 content tier) — Design Supplement

> **Created:** July 26, 2026
> **Last Updated:** July 26, 2026 (v0.4 — AR-3 sweep: 0H+0M+2L, **CONVERGENCE**; prior v0.3 AR-2, v0.2 AR-1, v0.1 initial)
> **Version:** 0.4
> **Status:** DESIGN SUPPLEMENT (pre-promotion) — **for the second tier of an already-APPROVED spec**
> **Candidate spec:** **#49**, Wave-8 **content tier** · **FR prefix:** `FR-LC` (new requirements begin at
> **FR-LC-021** — `FR-LC-001..020` + `FR-LC-008a` are APPROVED and unchanged)
> **Companion:** `docs/tracking/localization-seam-template-design.md` (the Wave-1 **seam** tier, APPROVED)
> **Promoted from:** `docs/tracking/spec-plans/spec-49-localization-accessibility.md` v0.2 (content-tier row)

---

## 0. Purpose and posture

The Wave-1 tier deliberately shipped a seam with **one locale and no a11y content**, deferring both to
Wave 8 (#49 §7.1 / §7.2). This supplement designs that content tier: what a locale *is*, when one may
ship, and what the accessibility surface actually contains. Design only — no code, no new spec number.

**This tier extends an APPROVED spec rather than opening one**, which changes the pipeline (§12) and is
the reason every decision below is expressed as an addition (`FR-LC-021+`) rather than an amendment.

Verification against the approved seam and its now-larger producer set produces three findings:

- **A shipped locale can be arbitrarily incomplete and nothing will say so** (§2(b)). The fallback that
  makes the seam robust — missing `(Id, variant)` renders base-locale English — also makes a 5%-translated
  locale look like a working one. The seam tier's coverage invariant (`FR-LC-008a`) protects **only the
  base locale**, by construction, and correctly so. **KD-1** supplies the missing gate.
- **The producer roster has nearly tripled since §7.3 was written** — three named, **eight** real (§2(c)).
  Coverage is per-producer, so the content tier's obligation grew with it.
- **Nobody owns applying an a11y option.** #38 lists a11y among the things it does *not* own (§2(d)), and
  #49 owns a *settings value*. Between them sits the actual work — scaling a label, honouring a contrast
  mode — which is a renderer obligation by nature. **KD-4** assigns it and files the back-prop.

## 1. Scope

**This tier owns:** the **definition of a locale** as data, the **completeness measurement and ship gate**
for one, the **pseudo-locale** that makes layout testable without translators, the **a11y option
catalogue**, and the **boundaries** of both (what is content vs. model depth; what is option state vs.
rendering).

**This tier does not own:**

| Not owned | Owner | How this tier relates |
|---|---|---|
| The seam, template model, fallback, and determinism posture | #49 **Wave-1 tier (APPROVED)** | Content is added *on* it; `FR-LC-001..020` are untouched (§12) |
| **Applying** text scale / contrast when rendering | **#38** (proposed — §8.1) | #49 owns the option *value*; the renderer honours it (KD-4) |
| The client-settings **store** | **#38** (proposed — ERR-038-004, filed by #51) | Locale + a11y selection are fragments in it, not a private file (KD-5) |
| The **copy** and the **translations** | writing / translation production | #49 specifies the catalogue's shape, coverage rule, and gate — not the words (§11 R-1) |
| Each producer's own coverage assertion | that producer's spec | `FR-LC-008a` is extended *by* each producer, per §7.3 (KD-2) |
| Save/format concerns | #30 / #50 | A locale is not save-visible at all (KD-7) — the point is that this is *provable*, not merely intended |

## 2. What already exists (verified)

**(a) The seam tier is APPROVED and complete for one locale.** `docs/specs/localization-accessibility/`
carries `FR-LC-001..020` plus `FR-LC-008a` at `Status: APPROVED`, with the renderer, the boundary-adapter
split, the base-locale identity anchor, and `FR-LC-017`'s no-stream/no-tag/no-save posture all pinned.

**Consequence:** this tier adds **data and gates**, never plumbing — exactly as §7.1 promises (*"No new
plumbing — the renderer, the boundary, and the producers are unchanged"*). Any decision below that would
require a core seam change is out of scope by construction.

**(b) The coverage invariant protects the base locale only — by design, and it is the right design.**
`FR-LC-008a` requires the **base-locale** catalogue to cover the producer's entire defined roster,
*"asserted fail-loud at catalogue construction"*. For any other locale, `FR-LC-008` says a missing template
*"falls back per `(Id, variant)`"*, and `FR-LC-011` extends that to keys, locales and clauses.

That asymmetry is correct: a partial translation must **degrade gracefully, string by string**, never
crash. But it has a consequence the seam tier had no reason to address, because it had no second locale:

> **A locale that is 5% translated behaves identically to one that is 100% translated, except that the
> player reads English.** There is no error, no warning, and no count.

**Consequence:** completeness for a non-base locale cannot be a *construction* invariant without
destroying the fallback that makes partial translation viable. It must be a **measurement plus a ship
gate** (KD-1) — a different mechanism for a different tier, which is why the seam tier was right to leave
it out.

**(c) Eight producers now bind to the seam; §7.3 names three.** #49 §7.3 anticipates *"#35 / #46 /
#38-static"*. Since it was written (July 23), the wave has added:

| Producer | Emits | Status |
|---|---|---|
| #22 interactions | intent + slots + `world.text` draw | **built**; retrofit specified (`FR-LC-016`) |
| #38 static UI | `LocalizationKey`s | approved |
| #35 media/press | question + **answer-option** rosters | supplement AR-converged |
| #46 news/inbox | inbox item identities | supplement AR-converged |
| #48 commentary | commentary intent via `MatchTextBoundary` | supplement AR-converged |
| #50 migration refusals | refusal class + version slots | supplement AR-converged |
| #39 Cloud conflict notices | conflict identity + slots | supplement AR-converged (this wave) |
| #51 audio captions | `CaptionId` per cue | supplement AR-converged (this wave) |

**Consequence:** the content tier's obligation is **per-producer** (`FR-LC-008a` is extended by each), so
"a complete locale" is a statement about eight rosters, not one catalogue. KD-2 defines the measurement
accordingly, and §7.3's list is updated as part of this tier's own promotion (§8.3 — not a back-prop,
since it is the same spec's forward-extension section).

**(d) A11y has an owner for the option and no owner for the effect.** #38's `section-1.md` out-of-scope
list reads *"localization/a11y (#49)"* — a clean disclaimer. #49's `FR-LC-019` defines the a11y boundary as
*"a read-only presentation settings value (text scale / contrast / colourblind-safe palette / input
assist)"* and defers *"the option content + settings store"* to Wave 8.

So #49 owns a **number**, and #38 has disclaimed the **behaviour**. Nobody owns "the label is 1.5× larger".

**Consequence:** KD-4 splits it explicitly — and the split is not arbitrary: a settings value cannot apply
itself, and the only layer holding a label is the renderer. Left unassigned, text scale would be
implemented per-screen by whoever noticed, which is precisely how a11y becomes 80%-honoured.

**(e) The colourblind palette has no home and one likely consumer nobody has connected.** `FR-LC-019` names
a *"colourblind-safe palette reusing the `dataviz` colour discipline"*, and §7.2 repeats it. But #37
(Match Analytics — heatmaps, xG, PPDA) contains **no** colour or palette text at all, and #38 holds no
theme surface in its approved files.

**Consequence:** if #49 shipped the palette as content, `Localization` would become the project's theme
authority and every chart-drawing surface would reference it — the inversion `FR-LC-012`'s reference
discipline exists to prevent, arriving from the opposite direction. KD-4 keeps #49 to the *mode selection*
and puts palette content with the client theme.

## 3. Staging (minimal-first → deep)

| Tier | Content |
|---|---|
| **Minimal (the identity)** | **Base locale only, a11y options all at their neutral values.** Identical to the APPROVED Wave-1 state — `FR-LC-011`'s base-locale identity anchor already proves it. |
| **Content tier (this one)** | The pseudo-locale (KD-3) + the completeness measurement and ship gate (KD-1/KD-2) + the a11y option catalogue and its application contract (KD-4/KD-5). **The first real locale is data on top of all three**, not part of them. |
| **Deep (Stage 3+)** | RTL/bidi layout, deeper morphology (`FR-LC-009`'s bound), region-specific formatting beyond the base set (KD-6). |

**The ordering inside this tier is load-bearing:** the pseudo-locale and the gate land *before* the first
translation, because both exist to catch what a first translation would otherwise reveal in production.

## 4. Key decisions

### KD-1 — A locale is **offered or not offered**; there is no partial locale in the picker

Completeness is measured (KD-2) and compared against a threshold. Below it, the locale is **not offered in
the UI at all**. Above it, it is offered, and per-string fallback continues to work exactly as
`FR-LC-008`/`FR-LC-011` specify.

**Why a binary gate rather than a quality indicator:** a picker entry is a promise. §2(b) shows the
fallback makes a 5% locale indistinguishable from a finished one *from inside the game*, so the player
discovers the truth only by playing in it. A "72% translated" label would be honest and would still leave
the player to find out which 28% — and no player can act on that. The binary gate moves the decision to
the only party who can make it informedly.

**Where the threshold binds:** the offered-locale list is **compliance-class** on **#39's release
checklist** (KD-4 there), because "which locales does this build claim to support" is a store-page and
release claim, not a runtime property. #39's gate is fail-closed, which is the right posture for it.

**Coverage is necessary and not sufficient — the gate has a second condition.** A locale can be 100%
translated and still unreadable if the shipped font cannot render its script: the player gets tofu boxes,
which is strictly worse than English, and no string-counting metric detects it. So a locale is offered only
when its script is **covered by a shipped font** as well as above the coverage threshold. Font assets and
their fallback chain are **#38 theme** content (KD-4's boundary — the same reasoning that puts the palette
there), and the check belongs beside the coverage report because both answer one question: *can we honestly
offer this?*

**Two carve-outs, stated so they are not discovered as loopholes:**

- **The base locale is never gated** — it is the identity and is complete by `FR-LC-008a`'s construction
  check.
- **The pseudo-locale (KD-3) is never *offered*** — it is a test artifact, available only in dev builds,
  and must not appear in a shipped picker.

### KD-2 — Completeness is a **build-time report over the same rosters** `FR-LC-008a` already walks

For each locale and each producer roster, the report counts required entries (static keys; per
`TextTemplateId`, `variantCount(BaseLocale, Id)` templates; per-`EventKind` clauses; per-producer
identities per §2(c)) against those present. It reuses `FR-LC-008a`'s construction-time walk — the same
rosters, the same fail-loud enumeration — and differs in what it does with a gap: **the base locale throws;
a non-base locale is counted.**

Three properties this buys, in ascending importance:

1. **No new machinery** — the walk exists; the tier adds a counting mode.
2. **It cannot silently under-report**, because a *missing roster entry* (not merely an empty row) is
   already what `FR-LC-008a` was strengthened to detect. A measurement built on a weaker enumeration would
   report 100% for a locale missing an entire producer.
3. **It grows automatically with §2(c)'s roster.** When #51's captions or #39's conflict notices are added,
   they enter the denominator without anyone remembering to extend the report — which is the only way a
   coverage number survives a growing producer set.

**Runtime counting is explicitly rejected**: it would mean either walking every catalogue at boot (a cost
paid by every player for a developer's benefit) or counting fallbacks as they happen (which reports
coverage of *what was displayed this session*, a number that varies per playthrough and is therefore
useless as a gate).

### KD-3 — A **pseudo-locale** is the layout gate, and it ships in the tier that precedes translation

A generated, non-translated locale that mechanically transforms every base string:

- **expansion** to a pinned factor (the eventual `[GT]`, sized for the worst realistic case — German/Finnish
  run long),
- **accented/wide substitution** so missing glyph coverage and font fallback show up,
- **bracketing** (`⟦…⟧`) so truncation and clipping are visible at a glance, and an unbracketed string is a
  candidate **baked string bypassing the seam** — a near-free `FR-LC-002` coverage check no static analysis
  gives you.

**The bracketing check needs one exemption, or it will be switched off within a week.** Plenty of
legitimate on-screen text is *not* catalogue content and correctly renders unbracketed: proper nouns
supplied as slot values (player and club names), numbers, scores, dates and currency rendered through
culture formatting. Slot values sit *inside* a bracketed template, so a template-rendered string still
brackets as a whole — but a label that is **only** a datum has nothing to bracket. So the check is stated
as: any string produced by the **catalogue path** must bracket, and a UI element rendering pure data must
declare itself as such. Without the exemption the test fires on correct code, and a test that cries wolf
is a test that gets disabled — which is worse than not having it.

**Why it lands before the first translation:** every failure it catches — clipped labels, fixed-width
layouts, missing glyphs, concatenated strings, bypassed seams — is a **#38 screen** defect, not a
translation defect. Discovering them via the first translated locale means paying for translation, then
rebuilding the screens, then re-testing the translation. The pseudo-locale finds them with zero translated
words and runs in CI, where no real locale ever can (no translator in the loop).

It also composes with KD-4: pseudo-locale **at maximum text scale** is the actual worst case, and it is the
configuration the layout tests should pin.

### KD-4 — #49 owns the **option**; #38 owns the **application**; the theme owns the **palette**

The split §2(d)/(e) forces:

| Concern | Owner | Rationale |
|---|---|---|
| Option catalogue + current values (text scale, contrast mode, colourblind mode, input assist, subtitles on/off) | **#49** | It is the a11y boundary `FR-LC-019` already defines — a value, no rendering |
| **Applying** the values when rendering (scaling type, honouring contrast, reflowing) | **#38** | Only the renderer holds a label; a value cannot apply itself (§2(d)) |
| **Palette content** for each colourblind/contrast mode | **#38's client theme** | Keeps `Localization` from becoming the theme authority that every chart surface must reference (§2(e)) |
| **Fonts** and the glyph-fallback chain | **#38's client theme** | Same boundary: a font is a rendering asset, not catalogue data — and it gates whether a locale can be offered at all (KD-1) |
| Consuming the palette for data visualization | **#37**'s surfaces via the theme | The `dataviz` discipline applies at the drawing site, not at the string seam |

**Subtitles/captions are an a11y option that binds two specs at once**, so the tier states it once: #51
declares a `CaptionId` per cue (its KD-4), #49 renders that identity like any other producer's, and the
"subtitles on" **option** lives here. No new seam — it is exactly the §7.3 producer-binding pattern with an
audio producer.

### KD-5 — Locale + a11y selection are **fragments** in the one client-settings store

They join the store proposed in **ERR-038-004** (filed by #51 this wave — §2 of that supplement records
five specs naming this store and none owning it). This tier does **not** file a duplicate back-prop; it
adds two fragments to the same proposal, and §11 R-3 carries the same fallback if #38's owner declines.

**Failure policy matches #51's and, deliberately, not #50's:** an unreadable settings fragment **resets to
defaults** (base locale, neutral a11y) and continues. `FR-LC-011` already makes base-locale fallback the
never-crash path, so this is the same principle one level up. Applying save-grade refusal to a preferences
file would block launch over a corrupt byte describing a font size.

### KD-6 — What is **content** and what is **model depth** (the boundary that keeps this tier data-only)

`FR-LC-009` bounds the template model to named placeholders plus a bounded plural/gender selector, and
§7.4 defers deeper morphology to Stage 3+. This tier holds that line and adds the corollary that makes it
enforceable:

> **A locale that cannot be expressed in the approved model is not a content problem — it is a Stage-3+
> model change, and it does not ship as a partial hack.**

Concretely: **RTL/bidi** (a layout-engine concern in #38, not a catalogue concern), **case declension
synthesis**, and **agreement engines** are all Stage-3+ (KD-6 extends §7.4's list rather than reopening
it). What *is* in scope as content: plural/gender **category declarations** per locale, number/date/
currency **formatting** via the platform's culture data, and the string data itself.

This matters because the pressure at content time is always to add "just one" template feature for one
language — and the seam's bounded model is what makes eight producers' rosters mechanically checkable.

### KD-7 — Determinism: adding a locale changes **no serialized byte**, and this is provable

`FR-LC-005`/`FR-LC-006`/`FR-LC-017` already establish it: localization runs display-side, after generation;
producer state and draws are locale-independent; #49 registers no stream, tag, or ordinal and bumps no
format. The content tier changes none of that — it adds catalogue rows.

**Two corollaries worth stating explicitly, because both are natural mistakes:**

- **Catalogues are outside `WORLD_GENERATION_VERSION`** (#50 KD-2). That version covers *derived-not-stored
  data that the sim regenerates* — rosters, knowledge bands, nationality. Localized text is derived at
  **display** from state that is already saved, so changing a catalogue changes what a player *reads*, never
  what a save *contains*, and #50 must not be extended to migrate it.
- **The base-locale corpus is a different case, and it is already handled.** Changing base-locale
  `variantCount(Id)` shifts which variant a given draw selects (`FR-LC-007`) — a *display* re-selection,
  still with no serialized change. Adding a **non-base** locale cannot even do that: the modulo is always
  taken against the base count (`FR-LC-007`), which is exactly why that requirement is written that way.

## 5. Persistent state (shape)

**No sim persistent state; no format-version bump** (`FR-LC-017`, unchanged). Two client-local fragments,
persisted by the store of KD-5:

```
LocaleSettings : { selectedLocale : LocaleId }                       # defaults to BaseLocale
A11ySettings   : { textScale, contrastMode, colourblindMode,
                   inputAssist, subtitlesEnabled }                   # all default to neutral
```

Catalogues, the pseudo-locale generator, and the palette definitions are **content/config artifacts**, not
live state.

## 6. Determinism posture

- Display-side only; no stream, tag, ordinal, or save impact (`FR-LC-017`, KD-7).
- A save round-trips byte-identically across display locales (`FR-LC-006`) — the acceptance test the seam
  tier already specifies, re-run here with a *second* locale present, which is the first time it is
  non-vacuous.
- Catalogues sit outside #50's migration and outside `WORLD_GENERATION_VERSION` (KD-7).

## 7. Primary surfaces (proposed)

| Surface | Direction | Notes |
|---|---|---|
| `LocaleCatalogue` (per-locale data) | content artifact | rows only; no seam change (§2(a)) |
| `CoverageReport(locale) → per-producer counts` | build-time tool | reuses `FR-LC-008a`'s walk (KD-2) |
| `OfferedLocales` (gate output) | build → UI picker + #39 checklist | binary offer/not-offer on **both** conditions — coverage threshold **and** font/script support (KD-1) |
| `PseudoLocale` generator | dev builds only | expansion + accents + bracketing (KD-3); never offered |
| `A11yOptions` (values) | #49 → #38 | option state; application is #38's (KD-4) |
| `LocaleSettings` / `A11ySettings` fragments | #49 → client-settings store | KD-5 |
| Theme palettes per contrast/colourblind mode | **#38 theme** | not a #49 surface (KD-4/§2(e)) |

## 8. Cross-spec back-props

### 8.1 At approval

| ID | Target | Change |
|---|---|---|
| **ERR-038-005** | #38 (`ui-client-framework`, APPROVED) | `section-1.md` lists *"localization/a11y (#49)"* as out of scope, which is right about the **catalogue** and wrong about the **effect**: #49 owns a settings value, and only the renderer can apply it. Record #38's obligation to consume `A11yOptions` when rendering — text scale, contrast mode, reflow — and to own the **theme palettes** per contrast/colourblind mode **and the fonts / glyph-fallback chain** (§2(d)/(e), KD-4; the font chain also gates KD-1's locale offer, so its owner must be named). Without it, a11y has an owner for the number and none for the behaviour, and text scale gets implemented per-screen by whoever notices. (`ERR-038-004` is proposed by #51 this wave; `-005` is the next free number — verified.) |

### 8.2 Deferred (land at the named tier)

- **The locale-completeness threshold** as a compliance row on **#39's release checklist** (KD-1) — lands
  when #39 promotes; #39 is pre-promotion, so this is a coordination note, not an ERR against an approved
  spec.
- **Per-producer coverage assertions** for #48/#50/#39/#51 — each lands in *its own* spec, per §7.3's
  existing rule (KD-2).
- **The first translated locale** — data, after the pseudo-locale and the gate (§3).

### 8.3 Explicitly **not** back-props

- **#49 §7.1/§7.2/§7.3** — updating them (including §2(c)'s three-to-eight producer list) is **this tier's
  own promotion**, not an amendment from outside: they are the forward-extension sections of the same
  spec, and this is the extension they forward-referenced.
- **`FR-LC-001..020` + `FR-LC-008a`** — untouched. Every decision here adds `FR-LC-021+` (§12).
- **ERR-038-004** — already filed by #51; this tier adds fragments to that proposal rather than duplicating
  it (KD-5).
- **#50 / #16** — no migration scope, no stream, no tag (KD-7).

## 9. Test focus

**The gate's non-vacuity, which is the whole tier** (KD-1/KD-2): a deliberately incomplete locale must
measure below threshold and **not be offered** — constructed by omitting one producer's roster entirely,
which is the case a weaker enumeration would score as complete (§2(b)/KD-2(2)). **And the second
condition:** a fully-translated locale whose script no shipped font covers is also **not offered** —
the case coverage counting scores as perfect and the player experiences as tofu (KD-1).

**Pseudo-locale as a seam check** (KD-3): every string produced by the **catalogue path** renders bracketed;
an unbracketed string from that path is a test failure (`FR-LC-002`), while an element declared pure-data
(names, numbers, dates) is exempt — the exemption is part of the assertion, not a caveat on it. Plus the
layout pass at **pseudo-locale × maximum text scale**, the real worst case.

**Locale-independence, now non-vacuously** (KD-7/`FR-LC-006`): a `WorldStore` / season save round-trips
byte-identically with a *second* locale selected — the same assertion the seam tier makes, run for the
first time with something to vary. **Base-locale identity preserved** (`FR-LC-011`): with a partial locale
selected, every missing string renders the exact base-locale text, and a missing `(Id, variant)` does not
shift the variant selection (`FR-LC-007`'s base-count modulo).

**A11y application** (KD-4): a text-scale change is honoured by rendered output (a #38-side test, per
ERR-038-005) while `A11yOptions` itself remains a value with no rendering; the palette resolves from the
theme, not from `Localization`. **Settings** (KD-5): fragments round-trip; a corrupt fragment resets to
base locale + neutral options and continues.

## 10. Reference DAG

```
shell → {#49, #38, producers}     #49 → {built producers}     #38 → {#49 values}     sim → { }
```

Unchanged from the seam tier: `Localization` references **built producers** (`FR-LC-013`) and is referenced
by no sim assembly (`FR-LC-012`). The content tier adds **no edge** — its additions are catalogue rows, a
build-time report, and settings fragments.

**The one edge it deliberately does not add** is `#37 → Localization` (or any chart surface → Localization)
for the colourblind palette. KD-4 routes it through #38's theme instead, which is the difference between
a11y content living in a display-string library and living in the theme layer that already owns colour.

## 11. Risks and standing options

- **R-1 — translation is content and it dwarfs the spec.** The tier specifies shape, coverage and gate; the
  words are production. The spec text must not drift into copy (the #48 §11 R-3 / #51 §11 R-1 class).
- **R-2 — the threshold is a product decision with a real cost** (KD-1). Set it high and few locales ship;
  set it low and the picker promises what the game does not deliver. #49 defines the mechanism and leaves
  the number a policy constant, exactly as #50 does with its supported floor.
- **R-3 — ERR-038-004/005 may be declined.** If the settings store is declined, the fallback is in-memory
  with persistence deferred, never a private file (#51 R-3, inherited). If the **a11y application**
  back-prop is declined, a11y has no owner at all — and unlike the settings store, there is no viable
  fallback inside #49, because #49 renders nothing. That makes ERR-038-005 the harder of the two.
- **R-4 — the pseudo-locale must stay unshippable** (KD-1 carve-out). A dev-only artifact that reaches a
  release picker is a visible-garbage bug in a shipped build; the offer gate should exclude it structurally
  rather than by remembering to.
- **R-5 — model-depth pressure arrives with the first hard language** (KD-6). The bounded model is what
  makes eight rosters checkable; the moment one locale gets a bespoke feature, the coverage report's
  denominator stops meaning the same thing across locales.

## 12. Promotion pipeline

**This tier extends an APPROVED spec, so it does not follow the eleven-new-files path.**

1. **This supplement, AR-converged** — **DONE at v0.4.** AR-1 (0H+2M) → v0.2, AR-2 (0H+1M) → v0.3,
   AR-3 (0H+0M+2L) → v0.4 = **CONVERGENCE** (an L-only round closes the cycle, per the project
   convention).
2. **Extend the existing section set** at `docs/specs/localization-accessibility/` — new requirements
   `FR-LC-021+` in `section-2.md`; content/gate/pseudo-locale detail in `section-3.md`; the a11y catalogue
   and the KD-4 split in `section-4.md`; the §9 test additions in `section-5.md`; and **§7.1/§7.2 rewritten
   from deferrals into delivered scope** with §7.3's producer list corrected to §2(c)'s eight (§8.3).
   `FR-LC-001..020` + `FR-LC-008a` are **not edited**.
3. **PASS-1 adversarial review over the extended sections** + a fix pass.
4. **No new `SPEC_INDEX.md` row** — row 49 exists; its entry gains the content-tier note (the #38
   screens-tier precedent: one file, two wave rows).
5. **Lead-developer R-01..R-05 sign-off for the extension** — a human authority, not self-grantable.
6. **Land `ERR-038-005`** atomically with the flip.

## Version History

| Version | Date | Change |
|---|---|---|
| v0.1 | July 26, 2026 | Initial supplement for #49's Wave-8 content tier, promoted from the plan's content-tier row. Three findings from verification against the APPROVED seam. **(1) KD-1** — `FR-LC-008a` is a construction-time coverage check on the **base locale only**, while `FR-LC-008`/`FR-LC-011` make every other locale fall back per string; correct for robustness, but it means a 5%-translated locale is indistinguishable in-game from a finished one, with no error, warning, or count. Completeness for a non-base locale therefore cannot be a construction invariant without destroying the fallback — it must be a **measurement plus a binary ship gate** (offered / not offered, no partial locale in the picker), binding as a compliance row on #39's fail-closed release checklist. **(2) §2(c)** — #49 §7.3 anticipates three producers; there are now **eight** (#22, #38-static, #35, #46, #48, #50, #39, #51), so "a complete locale" is a claim about eight rosters, and KD-2's report reuses `FR-LC-008a`'s walk precisely so new producers enter the denominator automatically. **(3) KD-4** — #38's `section-1.md` disclaims a11y to #49 while `FR-LC-019` makes #49's a11y surface a *settings value*: nobody owns applying it, and a value cannot apply itself. Split as option (#49) / application (#38) / palette (#38's theme), filed as **ERR-038-005** — with §2(e) noting that had #49 shipped the palette, `Localization` would have become the theme authority every chart surface references. **Also:** KD-3 puts a **pseudo-locale** (expansion + accents + bracketing) *before* the first translation, since every defect it catches is a #38 screen defect that would otherwise be found by paying for a translation first — and its bracketing doubles as an `FR-LC-002` bypass check no static analysis provides; KD-7 states the two natural mistakes explicitly (catalogues are outside `WORLD_GENERATION_VERSION`; a non-base locale cannot shift variant selection because `FR-LC-007` takes the modulo against the base count). |
| v0.2 | July 26, 2026 | **AR-1 fix pass: 0H + 2M, both resolved.** **M-1** — §0 and §2(c) said *"seven"* producers while §2(c)'s own table listed **eight** (#22, #38-static, #35, #46, #48, #50, #39, #51), and §12/§0's own argument depends on the count being right, since it is the denominator KD-2's coverage report walks. Corrected throughout. **M-2** — KD-3 claimed an unbracketed string is *"provably"* a seam bypass, which is false for the large class of on-screen text that is legitimately not catalogue content: proper nouns arriving as slot values, numbers, scores, dates and currency. Slot values sit inside a bracketed template so template output still brackets, but a label that is only a datum has nothing to bracket, so the check as written fires on correct code — and a test that cries wolf gets disabled, which is worse than not having it. Restated with the pure-data exemption as part of the assertion, and §9 aligned. |
| v0.3 | July 26, 2026 | **AR-2 fix pass: 0H + 1M, resolved.** **M-1** — KD-1's gate measured *translation coverage* alone, but a locale can be 100% translated and still unreadable if no shipped font covers its script: the player gets tofu boxes, which is worse than English, and **no string-counting metric detects it**. The gate would have passed exactly the case it exists to prevent. Added font/script support as a second, independent condition, with fonts and the glyph-fallback chain assigned to **#38's theme** on the same boundary KD-4 uses for the palette (a font is a rendering asset, not catalogue data) — which also means the ownership must be named in ERR-038-005, since the font chain now gates whether a locale may be offered. |
| v0.4 | July 26, 2026 | **AR-3 sweep: 0H + 0M + 2L, both resolved — CONVERGENCE** (an L-only round closes the cycle). The sweep traced AR-2's second gate condition through every place the gate is described. **L-1** — §7's `OfferedLocales` row still described a single-condition gate, and the surface table is what an implementer reads first (the #48 AR-4 / #51 AR-3 lesson). **L-2** — §8.1's `ERR-038-005` body still listed only `A11yOptions` and palettes, omitting the font chain that KD-1 now depends on #38 owning; a back-prop that omits half its content lands as half a fix. |
