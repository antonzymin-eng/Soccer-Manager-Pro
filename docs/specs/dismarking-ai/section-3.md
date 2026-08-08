# Dismarking & Marker-Awareness AI Specification #23 — Section 3: Formulas and Algorithms

**Created:** July 8, 2026
**Last Updated:** July 8, 2026 (v0.2 — PASS-1 M-1: dwell-update site pinned to the per-agent perception pass; #12 consumption is one-stride-stale by design (the RestDefense same-tick analogy does not transfer to `FilteredView`-derived signals).)
**Version:** 0.2
**Status:** APPROVED

---

## 3.1 MarkingPressure (FM-DM-01)

For agent `a` with `FilteredView` `V` and dwell state `S`:

```
d_marker   = min distance from a.Position to V.VisibleOpponents[i].PerceivedPosition
             over i in [0, VisibleOpponentsCount), considering only entries with
             finite positions and distance ≤ MARKING_RADIUS_M          [m]
proximity01 = d_marker exists ? clamp01(1 − d_marker / MARKING_RADIUS_M) : 0   [dimensionless]
dwell01     = min(1, S.DwellTicks / MARKING_DWELL_FULL_TICKS)                  [dimensionless]
MarkingPressure = proximity01 × dwell01                                        ∈ [0, 1]
```

- **Units / ranges:** distances in metres; `MARKING_RADIUS_M > 0` `[GT]`; ticks are 10 Hz heartbeats.
- **Why the product:** proximity alone spikes on a defender running past; dwell alone lingers after
  a marker leaves. The product requires *sustained* proximity — the tactical meaning of "marked".
- **Worked example:** marker perceived at 1.2 m, `MARKING_RADIUS_M = 3.0`, `DwellTicks = 7`,
  `MARKING_DWELL_FULL_TICKS = 10` → proximity01 = 1 − 1.2/3.0 = 0.6; dwell01 = 0.7;
  `MarkingPressure = 0.42`.

## 3.2 Dwell state machine (per agent, per heartbeat)

```
if phase != InPoss:
    S.DwellTicks = max(0, S.DwellTicks − MARKING_DWELL_DECAY_PER_TICK)   # decay-only
    S.LastMarkerId stays                                                  # cheap resume
elif a qualifying marker m exists (per §3.1 d_marker test):
    S.DwellTicks  = min(MARKING_DWELL_FULL_TICKS, S.DwellTicks + 1)
    S.LastMarkerId = m.AgentId
else:
    S.DwellTicks  = max(0, S.DwellTicks − MARKING_DWELL_DECAY_PER_TICK)
    if S.DwellTicks == 0: S.LastMarkerId = -1
```

Update order (PASS-1 M-1): dwell/pressure updates run in ascending agent index inside the
**per-agent perception pass** at stride N — the pass where `FilteredView` is built. The #12 offset
stage (§3.3) therefore consumes the value computed at stride **N−1** (Positioning runs before the
per-agent pass in the stride order), a documented one-stride latency in the conservative direction:
a newly acquired marker starts influencing the offset one stride late, which the dwell ramp absorbs.
The `RestDefenseEvaluator` same-tick pattern does NOT apply here — it consumes
`PositioningPerceptionSnapshot`, not `FilteredView`. The §3.4 passer-side penalty runs in the same
per-agent pass as its `FilteredView` and is always fresh. Restore determinism is unaffected: the
consumed value is a pure function of serialized dwell state + #16-serialized perception state.
Marker identity switches (a *different* opponent takes over inside the radius)
do **not** reset dwell — being handed from marker to marker is still being marked; `LastMarkerId`
simply tracks the current nearest.

- **Worked example (decay):** `DwellTicks = 10`, marker steps out of radius,
  `MARKING_DWELL_DECAY_PER_TICK = 2` → 10 → 8 → 6 → … reaches 0 after 5 unmarked heartbeats (0.5 s).

## 3.3 Dismark offset stage (FM-DM-02; #12 `SlotComposer`)

Runs after `SpacingResolver`, before the pitch clamp (FR-DM-008). For each eligible agent
(outfield, not carrier, `MarkingPressure > DISMARK_PRESSURE_FLOOR`):

```
markerPos = V.VisibleOpponents[nearest qualifying].PerceivedPosition
u         = (a.Position − markerPos)
if |u| < DISMARK_MIN_MARKER_DIST_EPS: skip (F3)
û         = u / |u|                                    # away from the marker
mag       = DISMARK_OFFSET_MAX_M × MarkingPressure × DismarkIntensityScalar[dial]   [m]
target'   = composedTarget + û × mag
```

The pitch clamp then applies as it does to every composed target, so the offset can never push a
target off-pitch. `DismarkIntensityScalar`: `Off → 0.0` (identity), `Conservative → 0.6`,
`Aggressive → 1.0` — all `[GT]`.

- **Range:** `mag ∈ [0, DISMARK_OFFSET_MAX_M]`; with the catalogue value 2.5 m and the §3.1 example
  (pressure 0.42) at `Aggressive`: `mag = 2.5 × 0.42 × 1.0 = 1.05 m` away from the marker.
- **Direction choice rationale:** directly-away is the cheapest deterministic evasion and composes
  with the existing spacing stage (which already prevents bunching into teammates). A
  side-preferring variant (toward open space) is a §7.3 deferral — it needs a second-opponent scan
  whose cost/benefit belongs to a balance pass.

## 3.4 Marked-pass-target penalty (FM-DM-03; #8 `UtilityScorer`)

For each PASS option from passer `p` to perceived teammate `t` (both read from **p's**
`FilteredView`, FR-DM-011):

```
d_t          = min distance from t.PerceivedPosition to any p-perceived opponent   [m]
targetProx01 = clamp01(1 − d_t / MARKING_RADIUS_M)          # no dwell term — p has no dwell state for t
awareness01  = clamp01((A_Decisions + A_Anticipation) / 2)  # passer's own attributes
mult         = Lerp(1.0, TARGET_MARKED_UTILITY_MULT, targetProx01 × awareness01)
utility(PASS→t) ×= (dial == Off ? 1.0 : mult)
```

Applied next to the existing Mentality / rest-defense multipliers, before the
`[UTILITY_FLOOR, UTILITY_CEILING]` clamp (#8 owns the product, boundary row 4 in §1.5).

- **Range:** `mult ∈ [TARGET_MARKED_UTILITY_MULT, 1.0]`; with the catalogue value 0.7, a fully
  aware passer (`awareness01 = 1`) sees ×0.7 on a teammate with an opponent at 0 m, ×1.0 on a free
  teammate. **Worked example:** opponent perceived 0.9 m from the teammate → targetProx01 = 0.7;
  awareness01 = 0.8 → `mult = Lerp(1.0, 0.7, 0.56) = 1.0 − 0.3×0.56 = 0.832`.
- The dwell-free proximity term is deliberate: the passer judges a snapshot ("is he tight right
  now?"), the marked agent's own dwell state models *sustained* attention — two different epistemic
  positions, both KD-1-compliant.

## 3.5 Constant catalogue (authoritative table in Appendix A)

| Constant | Tag | Value | Units |
|---|---|---|---|
| `MARKING_RADIUS_M` | `[GT]` | 3.0 | m |
| `MARKING_DWELL_FULL_TICKS` | `[GT]` | 10 | heartbeats |
| `MARKING_DWELL_DECAY_PER_TICK` | `[GT]` | 2 | heartbeats |
| `DISMARK_PRESSURE_FLOOR` | `[GT]` | 0.15 | — |
| `DISMARK_OFFSET_MAX_M` | `[GT]` | 2.5 | m |
| `DISMARK_INTENSITY_SCALAR` (Off/Cons/Aggr) | `[GT]` | 0.0 / 0.6 / 1.0 | — |
| `TARGET_MARKED_UTILITY_MULT` | `[GT]` | 0.7 | — |
| `DISMARK_MIN_MARKER_DIST_EPS` | `[FIXED]` | 1e-3 | m |

`[GT]` magnitudes are shapes-reviewed here and pinned at the implementation balance pass, the #21
G2 precedent (§9.2 note).

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-08 | — | Initial formulas FM-DM-01..03 with units, ranges, worked examples; constant table. |
| 0.2 | 2026-07-08 | — | PASS-1 M-1: dwell-update site pinned to the per-agent perception pass; #12 consumption is one-stride-stale by design (the RestDefense same-tick analogy does not transfer to `FilteredView`-derived signals). |
#endregion
