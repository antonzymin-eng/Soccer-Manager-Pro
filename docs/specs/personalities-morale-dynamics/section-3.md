# Personalities, Morale & Squad Dynamics #33 — Section 3: Algorithms

**Created:** July 23, 2026
**Last Updated:** July 27, 2026 (v0.3 — back-prop landed atomically with the ten-spec approval wave; see the version-history row)
**Last Updated (prior):** July 23, 2026 (v0.2 — AR-1 fix pass; prior v0.1 initial)
**Version:** 0.3
**Status:** APPROVED

---

All arithmetic is **integer per-mille** (FR-HS-004). The only float in the whole spec is the single
`StrengthPermille / 1000f` conversion at the #22 route boundary (§3.4). No stochastic draw occurs at the
minimal tier (FR-HS-009).

## 3.1 The world-day step — `AdvanceHumanSystemsDay` (FR-HS-009..012)

Invoked once per player per world day at #30's pre-declared tick-order slot 3 (§4). `committedInputs` are
**values** #30 routes in (recent match result, minutes played, board objective state) — #33 references neither
#30 nor the match engine.

```
AdvanceHumanSystemsDay(ref MoraleState m, playerId, in PersonalityProfile p,
                       in HumanSystemsDayInput committedInputs, worldDay):
    # F6 idempotency / gap guard
    if m.LastAdvancedWorldDay != HS_NOT_ADVANCED_SENTINEL:
        if worldDay == m.LastAdvancedWorldDay:  return          # no-op (already advanced)
        if worldDay != m.LastAdvancedWorldDay + 1:  throw       # day gap — #30 advances one day at a time (F6)

    # 1. Morale target from committed inputs (deterministic; integer per-mille).
    target := ComputeMoraleTarget(m.EquilibriumPermille, committedInputs, p)   # clamped [0,1000]

    # 2. Drift morale toward the target by a bounded integer step (no float, no draw).
    m.MoralePermille := DriftPermille(m.MoralePermille, target, MORALE_DRIFT_STEP_PERMILLE)   # clamp [0,1000]

    m.LastAdvancedWorldDay := worldDay
```

- `ComputeMoraleTarget` consumes `ExternalDeltaPermille` as an **additive term alongside**
  `BoardObjectiveDeltaPermille` (ERR-033-003). `0` ⇒ the target is unchanged, so the field is
  **behaviour-neutral until a non-zero delta is actually delivered** — and because it lives on a
  **transient input struct**, it carries **no `HUMAN_SYSTEMS_SAVE_FORMAT_VERSION` bump**.
- `ComputeMoraleTarget` blends the internal `EquilibriumPermille` set-point with committed-input deltas
  (a win nudges the target up, a benching nudges it down), scaled by the player's `Temperament` trait
  (a steadier temperament dampens the swing) — all integer per-mille, clamped `[0,1000]`. Its exact
  `[GT]` coefficients are illustrative pending a Stage-2/3 balance pass (the #21 G2 precedent; Appendix A).
- `DriftPermille(cur, tgt, step)` moves `cur` toward `tgt` by at most `step` per-mille: `cur + sign(tgt−cur)
  · min(step, |tgt−cur|)`. Deterministic, monotone, idempotent at `cur == tgt`.
- The **pairwise relationship** projection (`AdvanceRelationshipDay`, the same F6 guard) drifts each
  club-scoped pair's `StrengthPermille` toward a co-appearance/result-derived target by
  `RELATIONSHIP_DRIFT_STEP_PERMILLE`, or holds at the neutral baseline when the minimal dial keeps drift off
  (FR-HS-011). No draw either way.

## 3.2 Clique / chemistry derivation (FR-HS-020/021)

Cliques are a **pure derived read** over the pairwise store — never persisted (KD-4):

```
DeriveCliques(clubPairwise):                 # returns groups; no state written
    # A clique is a connected component whose edges are MUTUALLY-strong pairs (matching #22's "mutual > 0.6").
    build undirected graph G on club players;
        add edge {a,b} iff pairwise(a,b).StrengthPermille > CLIQUE_THRESHOLD_PERMILLE
                        AND pairwise(b,a).StrengthPermille > CLIQUE_THRESHOLD_PERMILLE      # BOTH directions
    return connected components of G with >= 2 members
```

The **mutuality** rule (both ordered directions must exceed the threshold) matches #22's clique definition
verbatim (living-world §1 "clique at **mutual** > 0.6" / §3.1 "cliques form at mutual > 0.6") — a one-sided
strong tie (a→b = 700, b→a = 500) is **not** a clique edge on either side, so #33's derived cliques never
diverge from #22's own clique consumers (e.g. `DressingRoomSplit`). `CLIQUE_THRESHOLD_PERMILLE = 600`
corresponds **exactly** to #22's `0.6` float threshold: a pair at permille `600` is **not** a clique edge
(`600 > 600` is false; `600/1000f > 0.6f` is false), and permille `601` **is** (`601/1000f > 0.6f`) — the
cross-representation is proven, not assumed (§5, Appendix B). **Chemistry** (a squad-level scalar, e.g. mean
intra-squad `StrengthPermille`) is derived the same way and stored nowhere.

## 3.3 Morale-value read accessors (FR-HS-023..025)

`MoraleOf(in MoraleState) → int` is the read-only projection OUT (match via the #27 seam, #31/#35/#45 reads).
No accessor mutates #33 state. **There is no write path INTO #33 morale at all** — this keeps the coupling
one-directional and the determinism ordering simple.

**ERR-033-004 (at #46's approval) — what "#46's man-management seam" is, stated so the wrong reading is
unavailable.** Earlier text called it a *future write path*, which invited an implementation in which #46
assigns `MoralePermille` directly. That would **contradict FR-HS-002**, which makes #33 the sole writer of
its own state. The seam **is the routed `ExternalDeltaPermille`** (ERR-033-003): #46 produces a bounded
delta, the root sums and clamps it, and #33 applies it through `ComputeMoraleTarget` like any other
committed input. **No behaviour change — this makes the only coherent reading the only available one.**

**ERR-033-002 (at #35's approval) — roster-lifecycle lockstep extends to routed inputs.** FR-HS-027 drops a
player's #33 entries at the season-boundary churn; a **producer-side pending delta** for a player who has
just retired or been transferred must be dropped with them. Otherwise an undelivered delta outlives its
subject and lands on whoever next holds that `PlayerId`. **The rule is what must exist, not where it is
filed** — it may equivalently be stated producer-side.

## 3.4 The #22 read-surface assembly (KD-1 — the load-bearing path)

Per world day, after slot 3 has produced the committed pairwise store, the `TacticalDirector.SeasonSave` root
assembles the `HumanSystemsView` and routes it into #22:

```
BuildHumanSystemsView(clubPairwise):         # ONLY the pairwise scalar (FR-HS-015) — no baseline
    for each pair (a,b) with a live club-scoped relationship:
        emit fromPlayerId=a, toPlayerId=b, edge = pair.StrengthPermille / 1000f      # the sole float
    # (ordered pairs: a→b and b→a are distinct entries, matching #22's per-ordered-pair Trust storage)

RouteIntoLivingWorld(view, worldStore):      # owned by the SeasonSave root, NOT #30, NOT living-world
    for each (from,to,edge) in view:
        worldStore.Memory.SetPlayerEdgeMirror(from, to, edge)    # NEW #22 seam (FR-HS-018); [0,1] gated
    # #22 then runs its WorldLoop; phase-2 consumes the refreshed PlayerEdge mirror (arc reads are #22's own).
```

`SetPlayerEdgeMirror` is the **one new** `MemoryStore` method #33's T-phase adds (none sets `PlayerEdge` on a
live edge today). It sets the `PlayerEdge` field + the `PlayerEdge` `ActiveLayers` bit on the (existing or
freshly created) edge, gated finite in `[0,1]` (NaN fails closed — the `InsertEdge` posture); it does **not**
touch `Affinity`/`Trust`, episodes, pins, or arcs, so `ApplyEvent`'s refusal and `T-LW-U-035` are unaffected.
At the minimal tier the route runs with an **empty** view (KD-8) ⇒ no `SetPlayerEdgeMirror` calls ⇒ #22
byte-identical.

## 3.5 Worked example (behaviour-neutral minimal)

Club 7, world day 0 → 1. Every player: `MoraleState.Create()` (morale 500, equilibrium 500), all traits 10,
every club-scoped pairwise `StrengthPermille = 500` (neutral). After one `AdvanceHumanSystemsDay` per player
with a **neutral** committed input (no match that day): target `= 500`, `DriftPermille(500, 500, step) = 500`
— morale unchanged; every pairwise holds at 500. `DeriveCliques` returns **no** clique (no pair `> 600`).
`BuildHumanSystemsView` at the minimal empty-view setting emits nothing ⇒ #22 byte-identical (T-HS-NEU-001).
A save→restore here is field-identical (T-HS-DET-001). This is the KD-8 identity the deep tier modulates.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-23 | — | Initial §3 (`AdvanceHumanSystemsDay`, clique derivation, the #22 read-surface assembly + `SetPlayerEdgeMirror`, worked example). Status IN REVIEW. |
| 0.2 | 2026-07-23 | — | AR-1 (M): clique rule made **mutual** (both directions > 600, matching #22). |
| 0.3 | 2026-07-27 | — | **ERR-033-003 / -004 / -002**: §3.1 consumes `ExternalDeltaPermille` as an additive term (0 ⇒ unchanged, so behaviour-neutral until delivered); §3.3 states that there is **no write path into #33 morale at all** and that #46's man-management seam is the routed delta, closing the reading under which #46 would assign `MoralePermille` directly and contradict FR-HS-002; and records the roster-lifecycle drop rule for pending deltas. |
#endregion
