# Living World #22 — Section-File Adversarial Review PASS-3

**Created:** June 21, 2026
**Reviewer:** fresh-eyes pass over `docs/specs/living-world/` section files v0.3
**Result:** 2 M + 2 L (no High). All resolved in the v0.4 fix pass (same day).

---

## Medium

**M-1 — `PlayerEdge` double-authority (contradicts KD-1/KD-9).** §2.2.1 stores `PlayerEdge` as an owned
`float` field, and the §3.1 update formula is "applied only to layers **active** for the edge's
node-type" — for a player↔player pair the active layer **is** `PlayerEdge`, so the formula would *mutate*
the vol-2 §2.1 relationship strength. That makes this layer a **second authority** over the social-graph
edge it is supposed to consume read-only (KD-1 "no redesign," KD-9 "never a second authority"), and risks
desyncing from vol-2's own edge evolution (§2.2 propagation). **Fix:** `PlayerEdge` is a **read-only
projection** of vol-2's authoritative edge; the §3.1 update writes only this layer's **owned** layers
(`Affinity`, `Trust`) and never `PlayerEdge`. Strengthen FR-LW-004 accordingly.

**M-2 — §4.5 edge bound imprecise (PASS-2 M-3 fix).** The fix asserts "live edges are bounded by
`ACTIVE_SET_EXTERNAL_CONTACTS_MAX`," but intra-club edges are **pairwise — O(active-set²)** and bounded by
the squad/staff/board size, not the external-contacts constant (which bounds only the external slice).
**Fix:** restate the edge bound as O(active-set²), governed by the whole (finite) active-set size, and
note O(active-set²) edge×memory is the budget's dominant driver.

## Low

**L-1 — `ActiveLayers` bit-position stability not stated.** The bitmask is persisted but its bit→layer
mapping is not tied to an ordinal contract. Note `ActiveLayers` bit positions = `RelationshipLayer`
ordinals (already covered by FR-LW-028, but should be explicit).

**L-2 — `ColdSummary` cannot reconstruct `ActiveLayers` on rehydration.** It stores `NetRelationship` +
retained episodes but neither the node-type nor the mask, so promotion (§3.5) cannot know which layers to
populate. Note the mask/node-type must survive compression (within the §7 deferred schema).
