# AP-02 — G1 Visual Review Evidence

**Status:** READY FOR OWNER VISUAL REVIEW — G1 NOT YET ACCEPTED  
**Date:** September 9, 2026  
**Art direction:** `docs/design/art/art-direction-v1.md` (`art-direction-v1`)  
**Visual board:** `docs/design/art/g1-visual-review.html`  
**Inherited baseline:** `docs/design/ui-mockups/`, chosen direction `touchline`

---

## Purpose

The existing management mockups are intentionally dense HTML/CSS information layouts. They are useful evidence for layout, hierarchy, density, and typography, but by themselves they do not show enough representative artwork to support the full AP-02 G1 visual judgment.

This evidence surface closes that review gap without pulling AP-03 Unity import work or AP-05 runtime integration forward.

`g1-visual-review.html` is a **reference review board**, not a production asset package. Its CSS/SVG examples are disposable visual evidence used to judge whether the proposed rules form one coherent System XI art language. G1 acceptance does not approve these exact drawings as shipping assets and does not authorize bulk production.

---

## How to review

Open:

- `docs/design/art/g1-visual-review.html`

The page is self-contained except for the same shared `touchline` token/font reference already used by the UI mockups. It remains legible with browser fallback fonts if the network font request is unavailable.

Review the numbered sections as one visual system:

1. identity / wordmark language;
2. analyst-tool icon family;
3. editorial portrait treatment;
4. match-instrument language;
5. fictional clubs and kit identities;
6. stadium / atmosphere treatment;
7. accessibility and motion grammar;
8. composed management-screen sample.

Reject G1 if those sections look like unrelated styles, if the board drifts into the rejected `stadium` broadcast-package direction, or if the match presentation still reads as placeholder-quality rather than a plausible professional Stage-1 target.

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

Agent/mechanical review remains responsible for the non-subjective parts of the same gate: `touchline` inheritance, #38 ownership compatibility, single-source token discipline, and preserving AP-03 rights/font/import work as pending.

G1 does **not** close:

- font redistribution, exact binary/version, offline packaging, glyph-corpus, or Unity-import evidence (AP-03 / G2);
- source→export→Unity technical recipe proof (AP-03 / G2);
- actual Unity match integration (AP-05 / G3-MATCH);
- management runtime integration before its real P5b/Wave-7 consumer exists (AP-05 / G3-UI runtime subresult);
- family-scale production.

---

## Owner response

If the board and the three dense-screen typography checks are acceptable, the required owner response is simply:

> **G1 accepted.**

If not, identify the numbered board section or dense mockup screen and the visual defect. A screenshot is useful but not required.
