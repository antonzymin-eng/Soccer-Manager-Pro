# System XI — Art Direction v1

**Status:** ACCEPTED — G1 CLOSED  
**Created:** September 6, 2026  
**Last Updated:** September 10, 2026  
**Document version:** 1.4  
**Style version:** `art-direction-v1`  
**Parent plan:** `docs/planning/art-pipeline-foundation.md` v0.8+  
**Visual baseline:** `docs/design/ui-mockups/`, inherited direction `touchline`  
**G1 evidence:** `docs/design/art/g1-visual-review.md` and `docs/design/art/g1-visual-review.html`  
**Architecture authority:** UI / Client Framework #38

---

## 1. Purpose and precedence

This document converts the already-selected `touchline` reference into the accepted production art direction for System XI. It does not reopen the July 25, 2026 `stadium` versus `touchline` decision.

Precedence remains:

1. APPROVED specifications govern behavior, ownership, runtime boundaries, and simulation truth.
2. `master-development-plan.md` governs stage-level quality gates.
3. The chosen `touchline` design reference governs the visual baseline where specifications are silent.
4. This document governs the accepted production-art language and its skins.
5. Family recipes may specialize this document but may not silently contradict it.

No art-direction value is simulation truth, a determinism input, or a runtime wire-format constant.

---

## 2. Direction in one sentence

**System XI is a dense football-analysis tool with a restrained retro football character: deep blue technical surfaces, precise information hierarchy, yellow/gold emphasis, archival texture, and visual identity that comes from football systems thinking rather than spectacle.**

### 2.1 Primary skin — Retro Dynamo Blue

The primary accepted skin is internally named **Retro Dynamo Blue**:

- deep navy / midnight blue base;
- royal/Dynamo-adjacent blue for structure and active identity;
- lighter sky blue for secondary information and focus support;
- cream/near-white text;
- yellow/gold for selection, stars, high-priority emphasis, and active accents;
- restrained aged-paper/paint/print texture rather than sterile perfect surfaces.

The label describes palette inspiration only. It does not authorize copying Dynamo Kyiv trademarks, crests, kits, sponsor marks, or protected artwork.

### 2.2 Alternate skin — Retro Bronze

The earlier bronze treatment is preserved as an alternate skin, **Retro Bronze**. It uses the same layout, semantic states, icons, density rules, and interaction behavior. Only presentation tokens change.

The alternate skin is not a second design system. It must be implemented as a skin/theme over the same structural UI.

### 2.3 What System XI should feel like

- precise and analytical;
- information-dense without becoming visually chaotic;
- football-specific without imitating a television package;
- modern in function but slightly archival/retro in finish;
- authored and human rather than generic SaaS or overly polished AI-style concept art;
- alive on match day without becoming arcade-like.

### 2.4 What it should not become

- neon cyberpunk;
- glossy mobile-game cards;
- a broadcast lower-third package;
- skeuomorphic leather/wood manager-office decoration;
- excessive chrome or bronze framing on the primary skin;
- photorealism used as a substitute for hierarchy;
- a collection of unrelated generated styles;
- a national-symbol theme park.

---

## 3. Core visual system

### 3.1 Surfaces and hierarchy

Primary screens use layered blue-black surfaces rather than true black. Panels are visually **elevated** through surface/value contrast and restrained shadow; internal sub-boxes should not default to heavy outlines.

Rules:

- use outlines when they communicate focus, danger, selection, or a true boundary;
- use elevation/value contrast for ordinary grouping;
- keep panel radii restrained and geometry predominantly rectangular;
- texture may soften digital perfection but may not interfere with text, charts, or exact reading.

### 3.2 Density rule

The accepted governing principle is:

> **Do not waste screen space. Every box must earn its footprint.**

This does not mean indiscriminate clutter. Empty space is justified only when it materially improves hierarchy, scanning, interaction targets, or state comprehension.

For an average desktop monitor:

- enlarge useful content before preserving decorative emptiness;
- prefer additional useful columns/rows over oversized empty cards;
- center compact values appropriately within their cells;
- do not create a large panel for a single short label when the same information can coexist with related data;
- preserve enough spacing to separate information groups and maintain click targets.

### 3.3 Brand and semantic color

In the primary skin:

- blue is the dominant structural/identity family;
- yellow/gold is the primary selection and rating emphasis;
- green remains semantic positive/available;
- red remains semantic danger/unavailable/critical risk;
- warning/risk must not rely on the same gold used for ordinary selection if the two can appear together.

**Selection is not status.** Clicking/selecting a player highlights the row or marker; it does not overwrite availability, injury, suspension, workload, or squad-status text.

### 3.4 Typography

The accepted visual role is a **condensed, humanist/editorial football-analysis display face**, paired with a highly readable body face and monospaced/data face.

Current reference stack:

- display reference: **PT Sans Narrow** / equivalent Cyrillic-capable condensed face;
- body reference: **IBM Plex Sans**;
- mono/data reference: **JetBrains Mono**.

The earlier IBM Plex Sans Condensed trial was rejected visually as too synthetic. G1 accepts the current typography character and hierarchy. AP-03 owns the exact shipping binary/version, redistribution rights, offline packaging, Ukrainian glyph-corpus proof, and fallback behavior. If AP-03 must substitute another Cyrillic-capable condensed face for technical/legal reasons, it may do so without reopening G1 only if the accepted visual role and dense-screen behavior are preserved.

---

## 4. Global production-art rules

### 4.1 Hierarchy before decoration

A production asset must materially improve at least one of recognition, hierarchy, football context, state readability, identity, or atmosphere. If it only fills empty space, omit it.

### 4.2 Retro texture with restraint

The accepted retro character may use:

- subtle print/grain/paint variation;
- slightly imperfect edge texture;
- modest archival-football cues;
- restrained warm highlights.

It must not reduce small-text clarity or turn the UI into a distressed poster.

### 4.3 Live text stays live

Player names, numbers, competition labels, dates, tactical instructions, ratings, localized strings, and other dynamic data remain live UI text by default.

### 4.4 One coherent family language

Within each family, lock geometry, crop, texture level, light direction, edge treatment, and information role before scaling volume.

### 4.5 Fictional-first identity

Until rights are explicit, clubs, people, sponsors, competitions, kits, venue marks, and stadium branding are fictional. Avoid accidental one-to-one resemblance to real protected assets.

---

## 5. Product identity and wordmark

- `SYSTEM` carries the analytical/system identity; `XI` is the football signifier and may receive stronger yellow/gold emphasis;
- use condensed uppercase typography and engineered spacing;
- keep the mark readable at small header sizes;
- prefer modular grid alignment to broadcast slashes/shields;
- primary treatment is blue/cream with restrained yellow emphasis;
- the brand may reference Ukrainian/Eastern-European football-school rigor structurally, not through default use of flags, tridents, folk ornament, Soviet motifs, or real-club marks.

Reject identity concepts that primarily read as esports, betting/crypto, military software, generic football crest, national-team merchandise, or television-network graphics.

---

## 6. Icon family

Icons should read as tools in an analyst workstation.

- author on a 24×24 reference grid;
- default optical stroke around 2px at reference size;
- use simple geometric silhouettes with consistent weight;
- yellow/gold icon treatment is permitted where it improves contrast/pop against dark blue surfaces;
- selected module state is applied to the containing control, not baked into a separate icon asset;
- normal/hover/selected/disabled/warning/destructive states should come from the UI/theme where practical;
- every core icon must remain identifiable at the smallest actual shipping size.

---

## 7. Player data language

### 7.1 Individual attributes

Individual attributes remain **numeric**. Examples: Vision 15, Dribbling 14, Finishing 12.

### 7.2 Composite player-game areas

Broader areas that summarize multiple attributes use a **five-star gauge** with half-star increments available. Examples include:

- Passing;
- Ball Carrying;
- Press Resistance;
- Build Up;
- Chance Creation;
- Scoring;
- defensive/goalkeeping equivalents for other player functions.

The displayed composites may differ by position/function. A goalkeeper, centre-back, striker, and winger do not need the same set of composite categories.

### 7.3 Ability, potential, and role fit

- current ability: five-star gauge;
- potential: five-star gauge;
- role fit: five-star gauge;
- half-stars are valid;
- filled stars use yellow/gold in the primary skin;
- unfilled stars remain clearly visible but subordinate.

### 7.4 Form

Where the UI uses `Form (Last 5)`, the displayed value is the average of the player's last five match ratings. This art document records the accepted presentation convention; authoritative runtime calculation remains owned by the appropriate gameplay/data specification.

### 7.5 Preferred-position diagram

The position diagram shows the full supported position map. Preferred positions are indicated by highlighting the **position circle**, not the abbreviation text. The abbreviation sits beneath its circle consistently. The accepted example highlights `CM`, `CAM`, and `RM`.

---

## 8. Match-view art

The 2D match view is a **live tactical instrument**, not a television broadcast or arcade scene.

### 8.1 Pitch

- top-down readability is primary;
- grass texture is restrained and low-noise;
- field geometry remains crisp;
- tactical overlays remain separable layers;
- pitch aspect ratio should read like a football field, not a square diagram.

### 8.2 Players and officials

- outfield players use compact circular markers with strong edges;
- goalkeeper uses a redundant visual distinction and team-specific keeper treatment;
- selected player gets a separate yellow/gold outer emphasis while retaining team identity;
- player name/number labels remain live text;
- referee and assistant referees use distinct marker shapes from players;
- the visual reference must contain exactly **11 players per team** plus **1 referee and 2 assistant referees** unless the depicted game state explicitly represents a dismissal/substitution transition.

The deterministic G1 reference encodes that 11-v-11 + 3-official invariant in data and checks it at runtime. Generated concept images are not authoritative for countable match geometry.

### 8.3 Event-linked labels

Only the selected player is labeled by default. A second player may be labeled when a user-selected event explicitly references that player. The event itself must be visibly selected so the extra label has clear context.

### 8.4 Team/semantic color discipline

Team colors, goalkeeper colors, selected-state yellow, semantic events, and data overlays must remain distinguishable simultaneously. Team identity may not be replaced by the selection color.

---

## 9. Portrait family

Portraits should read as a coherent editorial database, not collectible cards.

- head-and-shoulders / upper-chest crop;
- consistent eye line and head scale;
- simple dark/neutral background compatible with blue surfaces;
- controlled soft directional light;
- moderate detail, natural expressions, no heroic poster pose;
- consistent camera/crop/light/texture across the family;
- reject malformed anatomy, embedded text, visible watermarks, celebrity/real-player resemblance, and real club/sponsor marks.

The deliberate fallback portrait must look authored and shippable rather than like a missing-image placeholder.

---

## 10. Fictional clubs and kits

### 10.1 Badges

- readable silhouette first;
- simple geometry that survives small sizes;
- generally 2–4 principal colors;
- no microtext or fake decorative detail unless target-size testing justifies it;
- avoid close resemblance to real clubs;
- family quality should be consistent without forcing identical badge shapes.

### 10.2 Kits

- establish home/away contrast before decoration;
- use controlled solids, stripes, sashes, hoops, blocks, or restrained patterns;
- each kit should have its own vertically stacked presentation box in dense club-summary contexts where that improves separation;
- use panel/background contrast so shirt colors do not disappear into the card;
- no sponsor marks until rights/product policy explicitly allows them.

---

## 11. Stadium and environment

Stadium imagery supplies football atmosphere but must not overpower the information layer.

- the field is the visual emphasis; stands should frame it rather than dominate it;
- use real-looking venue detail in representative review imagery rather than bare placeholder rectangles;
- center venue name/location information above capacity/weather/pitch-information boxes when using the accepted stadium-summary composition;
- reserve low-detail regions behind live UI;
- avoid recognizable landmark imitation and real sponsor signage;
- day/night/weather variants are created only when actual client states require them.

---

## 12. Panel and interaction affordances

Every customizable dashboard panel uses a consistent upper-right affordance:

- lock/unlock control;
- dropdown/menu control;
- locked by default to prevent accidental movement;
- unlocked panels may be drag-reordered and resized;
- menu may expose swap/move/size/reset actions as supported by the owning UI specification.

This section defines the accepted visual affordance and interaction expectation. Final runtime behavior/ownership remains subject to UI / Client Framework #38 and later screen specifications.

---

## 13. Accessibility and readability

- critical state should have a cue beyond hue when practical;
- danger/unavailability cannot be communicated only by yellow/gold;
- selected/clicked state must remain distinguishable from positive/available state;
- goalkeeper and officials use redundant non-color distinctions;
- evaluate contrast on composed screens, not isolated swatches;
- preserve marker/ball silhouettes at actual match zoom;
- avoid high-frequency texture behind small text;
- no rasterized text as a script-coverage workaround.

AP-04/AP-05 should test representative assets under common color-vision-deficiency simulations and actual intended sizes.

---

## 14. Accepted token/skin changes

### T-01 — Display typography role — ACCEPTED AT G1

The inherited Barlow Condensed shipping path is unsuitable because Ukrainian/Cyrillic coverage must be proven. The IBM Plex Sans Condensed visual trial was rejected. G1 accepts PT Sans Narrow / equivalent humanist condensed character as the current visual direction, subject to AP-03 exact-font validation.

### T-02 — Primary color identity — ACCEPTED AT G1

The earlier electric-green-first treatment is superseded for `touchline` by the accepted **Retro Dynamo Blue** skin:

- blue/navy structural identity;
- cream/white ink;
- yellow/gold selection and star emphasis;
- semantic green/red retained for state meaning.

### T-03 — Alternate Retro Bronze skin — ACCEPTED AT G1

The bronze concept is retained as a second skin over the same structural UI. It may not fork layout, state semantics, or interaction behavior.

The shared mockup token source must be updated in the same AP-02 landing so these accepted values do not exist only in the review board.

---

## 15. G1 acceptance record

**G1 accepted by owner on September 10, 2026.**

Acceptance confirms:

1. `touchline` remains the inherited structural direction;
2. Retro Dynamo Blue is the accepted primary visual skin;
3. Retro Bronze is retained as the alternate skin;
4. the density/no-wasted-space principle is accepted;
5. composite star gauges, numeric individual attributes, preferred-position highlighting, match marker language, club/kit treatment, stadium treatment, and panel-control affordances are sufficiently defined to judge later samples;
6. the deterministic reference demonstrates a professional Stage-1 target rather than placeholder-quality presentation;
7. AP-03 technical/font/rights/import proof remains separate and pending.

`style_version = art-direction-v1` is now an accepted family target. G1 should be reopened only for a deliberate material art-direction change, not ordinary implementation polish.

---

## 16. Version History

| Version | Date | Change |
|---|---|---|
| 1.0 | 2026-09-06 | Initial AP-02 derivation from chosen `touchline`; extended unresolved identity, icon, match, portrait, fictional-club, stadium, motion, and accessibility families. |
| 1.1 | 2026-09-06 | Recorded the inherited Barlow Condensed Ukrainian/Cyrillic gap and initial IBM Plex Sans Condensed replacement proposal. |
| 1.2 | 2026-09-06 | Required representative dense-screen visual regression evidence before G1. |
| 1.3 | 2026-09-06 | Corrected unsupported font-weight assumptions in the visual proof. |
| 1.4 | 2026-09-10 | **G1 accepted.** Freezes Retro Dynamo Blue as primary skin, preserves Retro Bronze as alternate skin, records the no-wasted-space rule, humanist condensed typography role, deterministic 11-v-11 match invariant, star-gauge conventions, preferred-position behavior, retro texture, elevated panel language, and lock/dropdown/drag/resize panel affordances. AP-03/G2 technical proof remains pending. |