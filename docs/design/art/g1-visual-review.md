# AP-02 — G1 Visual Review Evidence

**Status:** ROUND 4 READY FOR OWNER VISUAL REVIEW — G1 NOT YET ACCEPTED  
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

### Round 3 — superseded by font trial

Round 3 addressed the density, overflow, stadium, club-separation, and player-reference findings and introduced Fira Sans Condensed as a replacement display-face candidate. The layout corrections remain in Round 4; the font trial does not.

---

## Round 4 candidate

Round 4 retains the accepted-in-principle layout corrections from Round 3 and changes only the display-face candidate:

- retains the preferred blue/navy/white palette with restrained yellow emphasis;
- trials **PT Sans Narrow** in place of Fira Sans Condensed for a more editorial, less synthetic display character;
- keeps tool tiles as a large left-hand label/sub-label plus a large right-hand icon;
- keeps the narrower portrait, centred player statistics, unclipped `Key passes`, filled lower profile area, and highlighted key player attributes/information;
- keeps larger, denser match-stat values and explicit Kovač/Mensah pitch/event references;
- keeps the distinct team-color underlay/accent beneath each club-kit panel;
- keeps the complete field and both stands visible in the stadium sample;
- keeps accessibility/selection states inside squad and match contexts with named player references;
- keeps the composed squad screen at **16 visible data columns**, with larger in-cell values, fuller footer/rail information, and a vertically/horizontally centred enlarged Role Fit block.

PT Sans Narrow is a **visual candidate only**. The Google Fonts metadata records OFL licensing plus `cyrillic` and `cyrillic-ext` subsets, making it suitable for the Ukrainian-capable visual trial. AP-03 still owns exact shipping binary/version, redistribution verification, Ukrainian glyph-corpus proof, offline packaging, fallback, and Unity import. The canonical `touchline` token/art-direction sources are not changed to this candidate until owner visual acceptance; otherwise the review board would prematurely become production authority.

---

## How to review Round 4

Open:

- `docs/design/art/g1-visual-review.html`

Review Sections 01–08 as one system. In particular:

- **01:** palette and PT Sans Narrow display-face feel;
- **02:** whether tool labels/icons use their tiles efficiently;
- **03:** clipping, centering, player-information hierarchy, key-player highlighting, and lower-area utilization;
- **04:** match-stat density and explicit Kovač/Mensah references;
- **05:** club/kit separation from the panel background;
- **06:** whether the full field and both stands make the stadium complete enough to judge;
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

If Round 4 and the later dense-screen typography regression checks are acceptable, respond:

> **G1 accepted.**

If not, identify the numbered section and remaining issue. Another visual-review round is preferred to locking a direction the owner does not want.