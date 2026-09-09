# AP-02 — G1 Visual Review Evidence

**Status:** ROUND 3 READY FOR OWNER VISUAL REVIEW — G1 NOT YET ACCEPTED  
**Date:** September 9, 2026  
**Art direction:** `docs/design/art/art-direction-v1.md` (`art-direction-v1`, still PROPOSED pending G1)  
**Visual board:** `docs/design/art/g1-visual-review.html`  
**Inherited structural baseline:** `docs/design/ui-mockups/`, chosen direction `touchline`

---

## Purpose

The existing management mockups remain useful evidence for layout, hierarchy, density, and typography, but they do not provide enough representative artwork for the full AP-02 G1 visual judgment. `g1-visual-review.html` is therefore the dedicated **reference review board**.

The board is visual evidence only. Its CSS/SVG examples are not approved production assets, and G1 does not authorize bulk production or pull AP-03/AP-05 runtime work forward.

---

## Review history

### Round 1 — rejected

The first board established the necessary review surface but was rejected for its black/electric-green emphasis and repeated under-use of available panel space.

### Round 2 — rejected

The blue-first Dynamo Kyiv / Ukraine-adjacent palette was substantially preferred, but the owner identified these remaining issues:

1. IBM Plex Sans Condensed felt too synthetic / “AI-like”;
2. tool tiles still under-used their footprint — labels and icons needed to become substantially larger;
3. Section 03 had text clipping, an over-wide portrait, poorly centred statistics, unused lower space, and no clear visual emphasis on the player’s key information;
4. Section 04 match-stat cards still carried too little information for their footprint;
5. Section 05 needed stronger separation between kit colors and the underlying panel;
6. Section 06 still did not show the stadium/pitch completely enough to judge;
7. Section 07 did not make it clear which match marker represented Kovač versus Mensah;
8. Section 08 still needed more squad columns and materially larger, better-centred in-box typography — specifically including Role Fit.

The owner also promoted the density preference into the main visual-review principle:

> **Do not waste screen space. Every box must earn its footprint.**

This does not mean indiscriminate clutter. Empty space is justified when it improves hierarchy, scanning, interaction targets, or preserves room for a real state/content requirement. Decorative emptiness is not a default System XI aesthetic.

---

## Round 3 candidate

Round 3 directly addresses every Round 2 finding:

- retains the preferred blue/navy/white palette with restrained yellow emphasis;
- replaces the Round 2 display-face trial with **Fira Sans Condensed** for visual evaluation;
- changes tool tiles to a large left-hand label/sub-label plus a large right-hand icon;
- narrows the portrait, centres player statistics, removes the clipped `Key passes` treatment, fills the bottom of the profile area, and highlights the most important player attributes/information;
- increases the size and density of match-stat values and makes the Kovač/Mensah relationship explicit in both pitch labels and event text;
- adds a distinct team-color underlay/accent beneath each club-kit panel so shirt colors do not disappear into the surrounding surface;
- recomposes the stadium sample so the complete field and both stands are visible;
- shows accessibility/selection states inside squad and match contexts with named player references;
- expands the composed squad screen to **16 visible data columns**, larger in-cell values, more useful footer/rail information, and a vertically/horizontally centred, enlarged Role Fit block.

The Round 3 font remains a **visual candidate only**. Fira Sans Condensed has Cyrillic support suitable for this review path, but AP-03 still owns exact shipping binary/version, redistribution, Ukrainian glyph-corpus proof, offline packaging, fallback, and Unity import. The canonical `touchline` token/art-direction sources are not changed to this candidate until owner visual acceptance; otherwise the review board would prematurely become production authority.

---

## How to review Round 3

Open:

- `docs/design/art/g1-visual-review.html`

Review Sections 01–08 as one system. In particular:

- **01:** palette and new display-face feel;
- **02:** whether tool labels/icons finally use their tiles efficiently;
- **03:** clipping, centering, player-information hierarchy, and bottom-area utilization;
- **04:** match-stat density and explicit Kovač/Mensah references;
- **05:** club/kit separation from the panel background;
- **06:** whether the stadium is now complete enough to judge;
- **07:** whether state cues are understandable in real context;
- **08:** whether the squad screen is busy enough for a normal monitor without sacrificing readability.

Reject G1 if a section still wastes obvious space, if the visual language feels inconsistent, or if the presentation still reads as placeholder-quality rather than a plausible professional Stage-1 target.

---

## Separate dense-screen typography check

The review board does not replace the 1920×1080 regression check. Before final G1 acceptance, the chosen display face must also be tested on:

- `docs/design/ui-mockups/Squad Screen.html`;
- `docs/design/ui-mockups/Tactics.html`;
- `docs/design/ui-mockups/Club Finances.html`.

Reject the chosen face if it introduces unacceptable clipping, wrapping, hierarchy loss, density regression, or awkward display headings/labels/numerics.

---

## G1 decision boundary

Owner visual acceptance covers whether the chosen palette, typography direction, density rule, and visual families can produce a coherent professional System XI presentation.

Agent/mechanical review remains responsible for `touchline` inheritance, #38 ownership compatibility, single-source token discipline at landing, and preserving AP-03 rights/font/import work as pending.

G1 does **not** close:

- font redistribution/exact binary/offline packaging/glyph-corpus/Unity-import evidence (AP-03 / G2);
- source→export→Unity recipe proof (AP-03 / G2);
- actual Unity match integration (AP-05 / G3-MATCH);
- management runtime integration before its real P5b/Wave-7 consumer exists;
- family-scale production.

---

## Owner response

If Round 3 and the later dense-screen typography regression checks are acceptable, respond:

> **G1 accepted.**

If not, identify the numbered section and remaining issue. Another visual-review round is preferred to locking a direction the owner does not want.