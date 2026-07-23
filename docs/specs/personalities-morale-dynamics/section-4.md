# Personalities, Morale & Squad Dynamics #33 — Section 4: Architecture

**Created:** July 23, 2026
**Last Updated:** July 23, 2026 (v0.3 — AR-2 fix pass; prior v0.2 AR-1, v0.1 initial)
**Version:** 0.3
**Status:** APPROVED

---

## 4.1 Assembly & file layout

New assembly `TacticalDirector.HumanSystems` (`src/human-systems/`), referencing **only**
`TacticalDirector.PlayerDatabase` (#27) and `TacticalDirector.DeterministicSim` (#16). It references neither
`match-engine` nor `living-world` nor the season loop (FR-HS-028).

```
src/human-systems/
├── MoraleState.cs                 # struct + Create()
├── PersonalityProfile.cs          # struct + Create()
├── PairwiseRelationship.cs        # struct
├── PairwiseRelationshipStore.cs   # club-scoped edge store (insert / remove / drift / lookup)
├── HumanSystemsDayInput.cs        # committed-values input to the daily step (no #30 reference)
├── HumanSystemsDay.cs             # AdvanceHumanSystemsDay + AdvanceRelationshipDay (§3.1)
├── CliqueDeriver.cs               # DeriveCliques / chemistry (pure read, §3.2)
├── HumanSystemsView.cs            # the KD-1 committed read view (primitive arrays, §3.4)
├── MentoringPlan.cs               # identity seam (KD-5)
├── HumanSystemsSaveCodec.cs       # HUMAN_SYSTEMS_SAVE_FORMAT_VERSION sub-blob (KD-7)
├── HumanSystemsConstants.cs       # the [GT]/[FIXED] catalogue (Appendix A)
└── tests/…                        # T-HS-*
```

## 4.2 Reference direction (acyclic, one-way)

```
   TacticalDirector.SeasonSave  (the composition root — references BOTH sides)
        │  invokes slot 3            │  routes the view
        ▼                            ▼
   #30 SeasonLoop  ──(values)──►  #33 HumanSystems        #22 LivingWorld
        (producer-only,              │  reads                (mirrors via
         FR-SN-017)                  ▼                        SetPlayerEdgeMirror;
                                #27, #16                      refuses via ApplyEvent)
```

- **#30 → #33:** the day-advance loop *invokes* `AdvanceHumanSystemsDay` at slot 3 (a **fill** of the
  pre-declared `RunWorldTickInFixedOrder` null seam, FR-SN-034 — **not** a new step, contrast #41's
  ERR-030-002), passing committed values, and hands #33's committed view back to the root. #30 is
  **producer-only** and is **not** the #22 router (FR-SN-017); its sole #22 surface stays the outcome event.
- **root → #22 (the KD-1 route):** the `SeasonSave` root — the only assembly referencing both `match-engine`
  and `living-world` — routes #33's pairwise scalar into #22 phase-2 as **primitive arrays** via the new
  `MemoryStore.SetPlayerEdgeMirror` seam. **Neither `living-world` nor `#33` references the other.**
- **#33 → {#27, #16}** only. **Acyclic.** #22's assembly stays reference-untouched (it gains a public method,
  a #22-internal code change).

## 4.3 The #22 mirror seam (`SetPlayerEdgeMirror`) — the one new #22 method

`MemoryStore` today has **no** way to set `PlayerEdge` on a live edge (`GetOrCreateEdge` no-ops on an existing
edge, `InsertEdge` throws on one, `ApplyEvent` refuses `PlayerEdge`). #33's T-phase adds a method written in
`MemoryStore`'s **own copy-out / mutate / write-back idiom** (`_edges` is a `List<RelationshipEdge>`; the store
has no `ref`-returning accessor, and `CollectionsMarshal.AsSpan` is unavailable on the pinned netstandard2.1
surface):

```csharp
// NEW public MemoryStore method (added at #33 T2). Sets ONLY the PlayerEdge mirror; touches nothing else.
public void SetPlayerEdgeMirror(int fromId, int toId, float value)
{
    // Caller (the SeasonSave root) guarantees fromId/toId are a player↔player pair (FR-HS-017 / §7.3 L);
    // PlayerEdge is valid only on player↔player edges (RelationshipLayer.cs).
    if (!(value >= 0f && value <= 1f))                                 // finite [0,1]; NaN fails closed
        throw new ArgumentOutOfRangeException(nameof(value));
    int idx = FindEdgeIndex(fromId, toId, out bool found);            // idx = the canonical-sort insertion index
    RelationshipEdge edge = found ? _edges[idx]
                                  : NewStrangersEdge(fromId, toId);    // fresh edge (Memory = Array.Empty<>())
    edge.PlayerEdge = value;
    edge.ActiveLayers |= (byte)(1 << (int)RelationshipLayer.PlayerEdge);   // additive — ORs in the bit only
    if (found) _edges[idx] = edge; else _edges.Insert(idx, edge);     // write-back / ordered insert (FR-LW-021)
    // Affinity / Trust / Memory / pins / arcs untouched → ApplyEvent refusal + T-LW-U-035 unaffected.
}
```

It uses the store's real lookup (`FindEdgeIndex(from, to, out bool found)`, which returns the canonical
`(FromId, ToId)`-sort insertion index) and, on the absent branch, an **ordered** `_edges.Insert(idx, edge)`
(never `.Add()` at the tail — the binary-search `FindEdgeIndex` depends on the sort order, FR-LW-021); the
fresh edge is a strangers-baseline edge with `Memory = Array.Empty<MemoryEpisode>()` (as `GetOrCreateEdge`
builds one). It ORs in the `PlayerEdge` bit (additive — it does **not** replace an existing edge's
`ActiveLayers`, and it never calls `GetOrCreateEdge`, so its AR-1 L-1 mask-conflict guard is not in play) and
leaves every other field alone. This is a
#22 **code** addition with **no schema change and no arc-logic change** (KD-1), recorded in #22 at the T-phase
wire-up (a T-phase ERR-022-NNN), **not** at #33's approval (§8 / §7-T2).

## 4.4 Provenance at the consuming seam

`AdvanceHumanSystemsDay` takes primitive/value inputs (a `HumanSystemsDayInput` of committed values), so #33
cannot import #30's or the match engine's types — provenance (that the committed inputs are the day's true
results) is the caller's contract, enforced at the #30 call seam (the #23 `MarkingPressureEvaluator` /
#41 `InjuryRiskContribution` primitive-input precedent). Symmetrically, the `HumanSystemsView` is primitive
arrays so the `SeasonSave` root can route it into #22 without either assembly referencing the other.

## 4.5 Persistence (KD-7)

`HumanSystemsSaveCodec` writes the #33 sub-blob (`HUMAN_SYSTEMS_SAVE_FORMAT_VERSION` [FIXED] = 1): per club,
each player's `MoraleState` (morale + equilibrium + `LastAdvancedWorldDay`) + `PersonalityProfile` (5 traits) +
the club-scoped pairwise `StrengthPermille` set + mentoring pairings. Composed into `SeasonSaveCodec` as an
opaque sub-blob (codec never parses it); the outer `SEASON_SAVE_FORMAT_VERSION` bump is coordinated with #30 at
the T-phase (as #28/#29/#40/#41 defer). **No `WORLD_STORE_FORMAT_VERSION` bump** — the #22 `PlayerEdge` mirror
stays #22's own serialized state. Fail-loud gates (F3/F5); no RNG cursor serialized (draw-free).

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-23 | — | Initial §4 (assembly/layout, acyclic reference direction, the `SetPlayerEdgeMirror` seam, provenance, persistence). Status IN REVIEW. |
| 0.2 | 2026-07-23 | — | AR-1 (M): `SetPlayerEdgeMirror` rewritten in `MemoryStore`'s copy-out/write-back idiom (no `GetOrCreateEdgeRef` — no ref accessor on netstandard2.1); additive `ActiveLayers` OR; player↔player caller contract noted. |
| 0.3 | 2026-07-23 | — | AR-2 (M): `SetPlayerEdgeMirror` corrected to the real `FindEdgeIndex(.., out found)` + ordered `_edges.Insert(idx, edge)` (not `.Add()`), preserving the canonical-sort invariant (FR-LW-021). |
#endregion
