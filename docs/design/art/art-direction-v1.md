# System XI — Art Direction v1

**Status:** PROPOSED — G1 PENDING  
**Created:** September 6, 2026  
**Last Updated:** September 6, 2026  
**Document version:** 1.1  
**Style version:** `art-direction-v1`  
**Parent plan:** `docs/planning/art-pipeline-foundation.md` v0.8+  
**Visual baseline:** `docs/design/ui-mockups/` v1.1, chosen direction `touchline`  
**Architecture authority:** UI / Client Framework #38

---

## 1. Purpose and precedence

This document turns the already-chosen `touchline` design reference into a production-art direction. It does **not** reopen the July 25, 2026 `stadium` versus `touchline` decision.

Precedence remains:

1. APPROVED specifications govern behavior, ownership, and runtime boundaries.
2. `master-development-plan.md` governs stage quality gates.
3. The chosen `touchline` mockup/tokens are the visual baseline where specs do not pin a value.
4. This document extends that baseline into art families the mockups do not define.
5. Family recipes may specialize this document without silently contradicting it.

No value here is simulation truth, determinism input, or a runtime wire-format constant.

---

## 2. Direction in one sentence

**System XI is a dense football-analysis tool with restrained broadcast energy: dark technical surfaces, precise information hierarchy, electric-green focus, and visual character that comes from systems thinking rather than spectacle.**

### 2.1 What it should feel like

- precise;
- analytical;
- modern but not glossy;
- football-specific without imitating a TV graphics package;
- information-dense without looking like a spreadsheet skin;
- confident and authored, not generic SaaS;
- alive on match day without becoming arcade-like.

### 2.2 What it should not become

- a broadcast lower-third package (`stadium` is the rejected comparison);
- neon cyberpunk;
- skeuomorphic leather/wood “manager office” decoration;
- glossy mobile-game cards;
- photorealism used as a substitute for hierarchy;
- a collection of unrelated AI-generated styles;
- a national-symbol theme park.

---

## 3. Inherited `touchline` foundation

The following values are inherited from `docs/design/ui-mockups/assets/tokens.css`. They are repeated here only as a mapping for art production; `tokens.css` remains the current source reference for these values until a deliberate token change is accepted.

### 3.1 Surfaces and ink

| Role | Inherited token/value | Art use |
|---|---|---|
| Deep background | `--bg-0 #07090c` | outer canvas, deepest negative space |
| Primary surface | `--bg-1 #0d1015` | base application/match surround |
| Touchline card | `--bg-card #11151b` | normal panels / art-safe UI surface |
| Strong card | `--bg-card-strong #181d25` | raised/selected containers |
| Stripe | `--bg-stripe #0e1218` | alternating dense rows / subtle segmentation |
| Primary ink | `--ink-1 #f4f6fa` | highest-emphasis text/symbol details |
| Secondary ink | `--ink-2 #c1c7d2` | secondary labels/details |
| Tertiary ink | `--ink-3 #8a93a3` | metadata/quiet annotation |
| Disabled ink | `--ink-4 #5a6371` | inactive/hint content |

Art should normally sit close to these surfaces rather than introduce new large-area background colors.

### 3.2 Brand and semantic color

Primary brand is **electric green `#00ff88`** (`--brand-400`). Use it for focus, active identity, and selected-state accents—not as a flood fill across large surfaces.

Inherited semantic colors:

- positive: `#2ee572`;
- caution/neutral: `#f5b942`;
- negative: `#ff4d5a`;
- information: `#4aa8ff`;
- trophy/highlight: `#ffc933`.

Inherited categorical data-viz sequence:

`#00ff88`, `#4aa8ff`, `#ffc933`, `#ff4d5a`, `#b066ff`, `#ff8a3c`.

**Rule:** brand green is not a synonym for “good.” Semantic meaning uses the semantic tokens; brand green indicates System XI identity/focus unless the UI context already assigns it a data role.

### 3.3 Density, spacing, and shape

The mockups use a 4px-derived dense spacing system and restrained radii. `touchline` specifically inherits:

- HUD radius: `4px` (`--r-2`);
- panel radius: `6px` (`--r-3`);
- button radius: `4px` (`--r-2`);
- no skew (`--skew: 0deg`).

Production art should reinforce that geometry: crisp rectangular compositions, modest rounding, no gratuitous pills, bevels, chrome, or diagonal broadcast slashes.

### 3.4 Typography roles

The July 25 mockup baseline nominated:

- display: **Barlow Condensed**;
- body: **IBM Plex Sans**;
- mono/data: **JetBrains Mono**.

AP-03's rights/script audit found a shipping blocker in that inherited stack: current upstream Barlow/Barlow Condensed does not provide the required Ukrainian/Cyrillic coverage. AP-02 therefore proposes **IBM Plex Sans Condensed** as the display replacement while retaining IBM Plex Sans for body and JetBrains Mono for mono/data. This keeps the condensed analyst/broadcast-accent role but aligns the display/body system within the IBM Plex family and removes the known script hole.

Touchline label behavior remains uppercase, weight 600, tracking `0.08em`; display weight remains 700 with `-0.01em` tracking. The role changes font family, not hierarchy.

These remain **visual candidates, not yet shipping font dependencies**. AP-03 owns redistribution/offline packaging, exact binary/version pinning, and Ukrainian/Cyrillic/fallback evidence. G1 acceptance would approve the visual substitution; it would not by itself close G2 or authorize font binaries.

---

## 4. Global production-art rules

### 4.1 Hierarchy before decoration

A production asset must improve at least one of:

- recognition;
- hierarchy;
- football context;
- state readability;
- identity;
- atmosphere.

If it only fills empty space, omit it.

### 4.2 Flat first, depth only when useful

Use surface contrast, line weight, scale, and spacing before shadow/glow. Existing shadows may support true elevation such as modal/overlay separation. Brand glow is an accent, not a default treatment.

### 4.3 Live text stays live

Reusable art contains no baked user-facing text by default. Numbers, names, competition labels, dates, tactical instructions, and localized copy remain UI text.

### 4.4 One coherent family language

Within a family, lock geometry, crop, light, texture level, and edge treatment before volume. A technically clean asset in the wrong family style is a failed asset.

### 4.5 Fictional-first identity

Until rights are explicit, clubs, people, sponsors, competitions, kits, and venue marks are fictional. Avoid accidental one-to-one resemblance to real crests, sponsors, kit patterns, or recognizable people.

---

## 5. System XI identity and wordmark extension

The working title should express **system, structure, and football intelligence**, not a generic sports badge.

### 5.1 Wordmark direction

- primary construction: condensed uppercase display typography;
- `SYSTEM` carries the analytical/system identity; `XI` is the football signifier and may receive stronger brand emphasis;
- use modular spacing/grid alignment rather than broadcast slashes or shield framing;
- favor one-color/flat versions first; electric green on dark and light/white on dark must both work;
- the mark must survive small header use without a separate micro-detail crest.

### 5.2 Ukrainian / Eastern-European reference language

If used, keep the connection **structural rather than literal**:

- football-school analytical rigor;
- technical notebook / tactical-diagram discipline;
- modernist grid and engineered proportion;
- restrained archival-football cues.

Do **not** make flag blue/yellow, the trident, folk ornament, Cyrillic decoration, or Soviet-era visual tropes the default brand shorthand. Any explicit national symbol requires a separate intentional decision.

### 5.3 Identity rejection tests

Reject a concept if it reads primarily as:

- esports team branding;
- television network graphics;
- betting/crypto branding;
- generic football crest;
- military/tactical software;
- national-team merchandise.

---

## 6. Icon family extension

Icons should read like tools in an analyst workstation.

### 6.1 Geometry

AP-02 production extension:

- author on a **24×24 reference grid**;
- default optical stroke: **2px** at 24px reference size;
- use simple geometric silhouettes and consistent optical weight;
- default to outline/line construction; filled mass is reserved for states where stronger emphasis is needed;
- corners should follow the touchline system's modest rounding rather than soft consumer-app blobs;
- do not encode a word/letter when a language-neutral football symbol works.

These are art-family rules, not replacements for UI layout tokens.

### 6.2 States

Icons should be designed to accept UI-driven state styling rather than bake separate color variants into files. Normal, hover, selected, disabled, warning, and destructive color come from the UI/theme when technically practical.

### 6.3 Small-size test

Every core icon must remain identifiable at the smallest actual shipping size used by the consuming screen. Thin interior details that disappear before the outer silhouette are rejected.

---

## 7. Data-visualization art extension

Most charts should remain primitives/text. Create art assets only where a primitive cannot express the intended football concept.

Rules:

- inherit `--viz-1` through `--viz-6` for categorical identity;
- no 3D charts, glass effects, textured bars, or decorative gradients that alter perceived magnitude;
- data marks must remain visually subordinate to labels/values when exact reading matters;
- textures/patterns may reinforce color differentiation for accessibility but may not encode hidden values;
- heat-map or pitch overlays use transparent layers over a quiet pitch base rather than a second decorative field illustration.

---

## 8. Match-view art extension

The 2D match view should feel like **a live tactical instrument**, not a miniature television broadcast or arcade game.

### 8.1 Pitch

- top-down / tactical readability is primary;
- use a dark, restrained, low-noise grass/pitch surface rather than photoreal blades, mowing spectacle, or high-frequency texture;
- field markings remain clear across zoom levels but should not compete with players/ball;
- pitch character may include subtle material variation, but gameplay geometry remains owned by the client, not painted into the texture;
- overlays such as zones, heat, passing lanes, or tactical annotations must remain separable layers.

### 8.2 Player markers

- outfield marker base: compact circular/near-circular form with a strong edge;
- goalkeeper must have a redundant non-color distinction (shape, inset, or glyph treatment);
- selected/controlled/focused state uses a separate outer ring/halo layer rather than replacing team identity;
- home/away distinction cannot depend on hue alone;
- player number/name remains live UI text if shown.

### 8.3 Ball and event marks

- ball is the highest-priority tiny moving object and must remain visible against every pitch region;
- any ball trail is restrained and functional, never a permanent arcade streak;
- goal/card/substitution/event marks use the same icon geometry as the UI family where possible;
- feedback effects use brief, low-area emphasis and do not obscure nearby markers or tactical overlays.

### 8.4 Match color discipline

The pitch, team colors, semantic events, selected-state brand green, and data overlays can coexist. Do not assume brand green is always available for the home team or for positive data. Final marker palette is resolved with actual match-client integration in AP-05.

---

## 9. Portrait family extension

Portraits should look like a coherent editorial database, not collectible cards.

### 9.1 Treatment

- restrained semi-realistic/editorial rendering;
- square master composition;
- head-and-shoulders / upper-chest crop;
- consistent eye line and head scale;
- simple dark/neutral background compatible with touchline surfaces;
- controlled soft directional light with readable facial structure;
- moderate texture/detail: enough individuality at 512px runtime target, no pore-level hyperrealism requirement;
- neutral-to-natural expression; no heroic poster pose.

### 9.2 Consistency over novelty

Across the family, preserve:

- camera angle;
- crop;
- focal length impression;
- background value range;
- light direction/contrast;
- edge treatment;
- realism/stylization level.

### 9.3 Generated-person safeguards

Reject visible watermarks, malformed anatomy, embedded text, celebrity/real-player resemblance, real club/sponsor marks, and inconsistent age/ethnicity cues relative to the fictional record being depicted.

The deliberate fallback portrait must look authored and shippable—not like a missing-image placeholder.

---

## 10. Fictional club badge and kit extension

### 10.1 Badges

- readable silhouette first;
- simple geometry that survives small navigation/table sizes;
- generally 2–4 principal colors;
- avoid microtext, fake founding dates, and tiny interior illustration unless the actual target size proves them useful;
- avoid shield templates or motif/color combinations that closely reproduce a real club;
- a league of fictional clubs should feel related in production quality, not identical in shape.

### 10.2 Kits / color identities

- establish clear home/away contrast before decorative pattern;
- use simple controlled families: solid, stripe, sash, hoop, block, or restrained geometric pattern;
- no sponsor marks until explicit rights/product policy says otherwise;
- kit design must still function when reduced to marker/thumbnail color identity;
- accessibility review must check likely home/away pairings rather than individual kits in isolation.

---

## 11. Stadium and environment extension

Stadium/background imagery supplies football atmosphere behind an information-heavy interface.

- composition should reserve low-detail UI-safe zones;
- use subdued contrast and saturation behind live UI;
- favor coherent architectural atmosphere over recognizable landmark imitation;
- no large sponsor/competition signage unless fictional and intentionally designed;
- background detail should fall away before foreground UI detail;
- day/night/weather variants are created only when the actual client exposes those states;
- do not pre-generate combinatorial venue libraries.

A stadium image is optional until a real consuming screen/state exists.

---

## 12. Motion and feedback principles

This document does not set UI timing or interaction behavior; those remain screen/framework concerns.

For art/effect assets:

- motion should explain state change or match action, not decorate idle screens;
- prefer short, contained emphasis over loops and perpetual glow;
- avoid camera-shake, lens-flare, confetti, or particle excess as default feedback;
- preserve text/data legibility during any effect;
- reduced-motion support must remain possible because essential meaning is never encoded only in animation.

---

## 13. Accessibility and readability

- critical state must use at least one cue beyond hue when practical: shape, outline, glyph, pattern, number, or text;
- do not use brand green and semantic positive green as the only distinction between two simultaneous meanings;
- evaluate contrast on the **final composed screen**, not just isolated swatches;
- preserve a strong marker/ball silhouette at actual match zoom;
- categorical data should remain traceable when color perception is reduced;
- avoid high-frequency textures behind small text or icons;
- no rasterized text as a script-coverage workaround.

AP-04/AP-05 must test representative assets under common color-vision-deficiency simulations and at actual intended sizes.

---

## 14. Token-change register

### T-01 — Display font family (PROPOSED with G1)

AP-03 identified a localization blocker in the inherited display face. AP-02 therefore proposes:

- **from:** `--font-display: "Barlow Condensed", ...`
- **to:** `--font-display: "IBM Plex Sans Condensed", "IBM Plex Sans", ...`
- **reason:** preserve a condensed display role while providing a technically viable Ukrainian/Cyrillic path and reducing cross-family typography complexity;
- **scope:** visual reference/theme token only; no runtime font binary is adopted in AP-02;
- **evidence owner:** AP-03 for license, exact binary/version, glyph corpus, offline packaging, and Unity import;
- **rollback:** if G1 visual review rejects IBM Plex Sans Condensed, choose another Cyrillic-capable condensed display face through the same explicit token-change process rather than reverting to an uncovered shipping face.

The AP-02 branch updates `docs/design/ui-mockups/assets/tokens.css` and the mockup README in the same proposed landing so there is no second typography source of truth. No palette, spacing, radius, body-face, mono-face, or `touchline` direction token changes are proposed.

Any future change to an inherited palette, type role, spacing value, radius, or `touchline` direction value must be recorded here as a proposed change and update the UI design reference in the same landing. Family-specific values in this document (for example the icon reference grid/stroke) are extensions, not silent token replacements.

---

## 15. G1 acceptance criteria

G1 is ready for owner acceptance when review confirms:

1. `touchline` is visibly inherited, not reselected;
2. no rule contradicts APPROVED #38 ownership/behavior;
3. the identity, icon, match, portrait, fictional-club, stadium, motion, and accessibility families are specific enough to reject off-style samples;
4. the proposed T-01 display-font substitution is visually accepted or explicitly replaced by another Cyrillic-capable condensed face, with the shared mockup token updated in the same landing;
5. no second palette/type/spacing source has been created, and AP-03 rights/font/import work remains separate and explicitly pending;
6. the direction can produce professional Stage-1 match presentation rather than placeholder-quality visuals.

Until that acceptance, `style_version = art-direction-v1` is a proposed family target, not permission for bulk production.

---

## 16. Version History

| Version | Date | Change |
|---|---|---|
| 1.0 | 2026-09-06 | Initial AP-02 derivation from chosen `touchline`: maps inherited tokens and extends unresolved identity, icon, data-viz, match, portrait, fictional-club, stadium, motion, and accessibility rules. No inherited token changes proposed. |
| 1.1 | 2026-09-06 | AP-03 cross-stream typography correction: records the inherited Barlow Condensed Ukrainian/Cyrillic gap and proposes IBM Plex Sans Condensed as T-01 display replacement; shared mockup token/reference is updated in the same proposed G1 landing. Runtime font adoption remains AP-03/G2 work. |
