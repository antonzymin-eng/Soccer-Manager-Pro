# AP-02 — G1 Visual Review Evidence

**Status:** G1 ACCEPTED BY OWNER  
**Acceptance date:** September 10, 2026  
**Art direction:** `docs/design/art/art-direction-v1.md` (`art-direction-v1`)  
**Canonical visual review surface:** `docs/design/art/g1-visual-review.html`  
**Primary skin:** `retro-blue` — Retro Dynamo Blue  
**Alternate skin:** `retro-bronze` — Retro Bronze

---

## Acceptance record

The owner accepted G1 after the visual direction was rebuilt as deterministic HTML/CSS/SVG/JavaScript rather than treated as an image-generation artifact.

The accepted visual direction is:

- **Retro Dynamo Blue** as the primary skin: deep/navy blue technical surfaces, brighter Dynamo/Ukraine-adjacent blues, cream/white text, and yellow/gold emphasis;
- **Retro Bronze** preserved as a second skin option rather than discarded;
- dense, information-forward screens where **every box must earn its footprint**;
- restrained retro/archival texture and less pristine geometry so the UI feels authored rather than generic/AI-clean;
- raised/elevated panels rather than heavy outlines around every sub-box;
- panel lock + dropdown affordances in the upper-right; panels are locked by default, with unlock enabling drag/reorder and resize behavior;
- 5-star gauges, including half-stars, for player ability, potential, role fit, and broader player-game composites;
- individual player attributes remain numeric;
- broad player-game composites are function/position-relevant groupings such as Passing, Ball Carrying, Press Resistance, Chance Creation, and Scoring;
- player-position diagrams show the full position set and highlight the preferred-position **circle**, with the label beneath the circle;
- `Form (Last 5)` represents the average of the last five match ratings;
- selected/clicked state is visual emphasis and does not overwrite availability/status data;
- risk states such as heavy workload use an explicit danger treatment, not merely a gold highlight;
- squad sorting is initiated by column-header interaction rather than a separate sort control.

The internal working label **Retro Dynamo Blue** describes palette inspiration only. It does not authorize copying Dynamo Kyiv trademarks, crests, kits, or protected artwork.

---

## Deterministic invariants

The canonical review surface encodes countable UI facts instead of asking an image model to approximate them:

- match pitch = **11 Riverside players + 11 North Vale players + 1 referee + 2 assistant referees**;
- the page performs an in-page roster count and throws if the 11-v-11 + 3-official invariant fails;
- Riverside and North Vale player arrays are separate deterministic data sources;
- goalkeeper styling is distinct from outfield styling and the two teams' goalkeeper treatments are also distinct;
- player labels are live UI text, not baked into pitch art;
- preferred positions are data-driven (`CM`, `CAM`, `RM` in the accepted example);
- star ratings are data-driven and support half-star increments;
- squad columns are sortable from their headers;
- the primary/alternate skin switch is implemented from one shared structural surface rather than two divergent mockups.

Generated images remain useful as exploratory visual references, but they are no longer authoritative for exact counts, positions, labels, state semantics, or interaction behavior.

---

## G1 boundary

G1 is **closed**. Owner acceptance covers visual direction, density, palette/skin direction, typography character, information hierarchy, family coherence, and the ability of the direction to reach professional Stage-1 presentation quality.

G1 does **not** close AP-03/G2. AP-03 still owns:

- exact shipping font selection/binary/version and Ukrainian glyph proof;
- font redistribution/offline packaging;
- source → export → Unity recipe proof;
- provenance/rights for production portraits, stadium imagery, badges, kits, and other assets;
- importer/meta/GUID/LFS evidence;
- runtime integration proof.

The current review portrait/stadium treatments are illustrative references only, not shipping assets.

---

## Next step

Proceed to **AP-03 / G2 technical pipeline proof**. G1 should be reopened only for a deliberate material art-direction change, not for ordinary implementation polish.