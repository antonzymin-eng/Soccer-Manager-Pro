# AP-02 — G1 Visual Review Evidence

**Status:** ROUND 2 READY FOR OWNER VISUAL REVIEW — G1 NOT YET ACCEPTED  
**Date:** September 9, 2026  
**Art direction:** `docs/design/art/art-direction-v1.md` (`art-direction-v1`, still PROPOSED pending G1)  
**Visual board:** `docs/design/art/g1-visual-review.html`  
**Inherited structural baseline:** `docs/design/ui-mockups/`, chosen direction `touchline`

---

## Purpose

The existing management mockups are intentionally dense HTML/CSS information layouts. They are useful evidence for layout, hierarchy, density, and typography, but by themselves do not show enough representative artwork to support the full AP-02 G1 visual judgment.

`g1-visual-review.html` is therefore a **reference review board**, not a production asset package. Its CSS/SVG examples are disposable visual evidence used to judge whether the proposed rules form one coherent System XI art language. G1 acceptance does not approve these exact drawings as shipping assets and does not authorize bulk production.

This evidence surface does not pull AP-03 Unity import work or AP-05 runtime integration forward.

---

## Round 1 owner disposition — REJECTED

The September 9 first review did **not** accept G1. The owner gave eight concrete findings:

1. reject the black/electric-green scheme and test a Dynamo Kyiv / Ukrainian-inspired color family, allowing nearby colors where contrast/readability require it;
2. use UI space efficiently — Section 02 tool tiles and their contents were under-sized relative to their containers;
3. Section 03 player information boxes were under-filled — increase information size and/or count for an average monitor;
4. Section 04 match-information boxes wasted space;
5. Section 05 club/kit examples did not fill their section;
6. Section 06 stadium/environment evidence lacked enough detail to judge;
7. Section 07 accessibility/state symbols needed to be shown **in use**, not isolated;
8. Section 08 management composition needed substantially higher information density, more columns, and less dead space.

Round 2 exists specifically to test those corrections. G1 remains open.

---

## Round 2 color candidate

The review candidate deliberately does **not** use a literal 50/50 Ukrainian-flag treatment. Instead it takes the more usable overlap between Dynamo Kyiv tradition and Ukrainian identity:

- deep navy `#071426` — application ground;
- technical navy-blue `#0D223B` — panels;
- royal blue `#1E65B7` — football/identity layer;
- sky blue `#5BB8FF` — active/focus layer;
- near-white `#F7FBFF` — primary information;
- yellow `#FFD84D` — high-value emphasis and selective decision accent.

The intent is **blue/white first, yellow restrained**. Yellow is not used as a large-area reading surface and does not replace semantic positive/warning/error meaning. The candidate keeps independent positive, warning, negative, and information states so brand identity does not erase status meaning.

This palette is intentionally scoped to the Round 2 board until the owner accepts it. If G1 accepts the direction, the accepted palette/token change must be promoted into the canonical `touchline` token/art-direction sources in the same landing; the review board must not become a second production source of truth.

---

## Density rule introduced by Round 1

The review establishes a stronger space-use principle for the proposed direction:

> **Empty space must earn its footprint.** On normal management screens, unused area is justified only when it creates hierarchy, preserves scanability, protects interaction targets, or reserves a real content/state need. Decorative emptiness is not a default System XI aesthetic.

Practical consequences for Round 2:

- tools/icons scale to their tiles rather than floating inside them;
- information cards either enlarge their values or carry more useful information;
- match sidebars use realistic metrics/events instead of sparse showcase cards;
- family-review grids show enough members to judge repetition and variation;
- state/accessibility treatments are demonstrated inside actual UI/match contexts;
- dense management screens should resemble a professional information workstation on an average monitor.

This does **not** mean filling every pixel. Scanability and hierarchy still win over indiscriminate clutter.

---

## How to review Round 2

Open:

- `docs/design/art/g1-visual-review.html`

Review the numbered sections as one visual system:

1. identity / blue-white-yellow palette;
2. analyst-tool icon family at working density;
3. editorial portrait plus dense player information;
4. match-instrument language with a realistically occupied information rail;
5. fictional club and kit family at useful sample volume;
6. stadium / atmosphere treatment with concrete environmental detail;
7. accessibility, selection, warning, keeper, and action cues **in context**;
8. high-density composed management-screen sample.

Reject G1 if those sections look like unrelated styles, if the palette does not feel appropriate, if density still looks wasteful, or if the match presentation still reads as placeholder-quality rather than a plausible professional Stage-1 target.

---

## Separate T-01 dense-screen typography check

The visual board does **not** replace the existing 1920×1080 font-regression requirement. Review these existing pages separately:

- `docs/design/ui-mockups/Squad Screen.html`;
- `docs/design/ui-mockups/Tactics.html`;
- `docs/design/ui-mockups/Club Finances.html`.

For the proposed `touchline` display role using IBM Plex Sans Condensed, reject if there is unacceptable:

- clipping;
- unexpected wrapping;
- hierarchy loss;
- density regression;
- visibly awkward display headings, labels, or numerics compared with the chosen `touchline` baseline.

These pages are expected to be mostly text, tables, panels, controls, and simple primitives. Their purpose in G1 is typography/density regression, not representative art-family review.

---

## G1 decision boundary

Owner visual acceptance covers the visual judgments in `art-direction-v1.md` §15, including T-01's appearance and whether the proposed families can produce a coherent professional presentation.

Agent/mechanical review remains responsible for the non-subjective parts of the same gate: `touchline` inheritance, #38 ownership compatibility, single-source token discipline at landing, and preserving AP-03 rights/font/import work as pending.

G1 does **not** close:

- font redistribution, exact binary/version, offline packaging, glyph-corpus, or Unity-import evidence (AP-03 / G2);
- source→export→Unity technical recipe proof (AP-03 / G2);
- actual Unity match integration (AP-05 / G3-MATCH);
- management runtime integration before its real P5b/Wave-7 consumer exists (AP-05 / G3-UI runtime subresult);
- family-scale production.

---

## Owner response

If Round 2 and the three dense-screen typography checks are acceptable, the required owner response is simply:

> **G1 accepted.**

If not, identify the numbered board section or dense mockup screen and the visual defect. Another visual-review round is preferred to locking a direction the owner does not want.