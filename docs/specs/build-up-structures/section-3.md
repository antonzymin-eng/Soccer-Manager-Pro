# Scripted Build-Up Structures Specification #24 — Section 3: Formulas and Algorithms

**Created:** July 8, 2026
**Last Updated:** July 8, 2026 (v0.1)
**Version:** 0.1
**Status:** IN REVIEW

---

## 3.1 Zone classifier with hysteresis (FM-BU-01)

Thresholds are thirds of the pitch, team-relative X (from own goal line, attack toward +X):

```
BUILDUP_ZONE_THRESHOLD_1_M = PITCH_LENGTH_M / 3      # 35.0 m   [DERIVED]
BUILDUP_ZONE_THRESHOLD_2_M = 2 × PITCH_LENGTH_M / 3  # 70.0 m   [DERIVED]

rawZone(x) = x < T1 ? OwnThird : x < T2 ? MiddleThird : FinalThird

commit rule (H = BUILDUP_ZONE_HYSTERESIS_M [GT] = 2.0 m; PASS-1 M-2 formulation):
    the COMMITTED zone's own interval expands by H at each of its boundaries
    (OwnThird committed ⇒ it claims [0, T1+H); MiddleThird ⇒ [T1−H, T2+H);
    FinalThird ⇒ [T2−H, 105]);
    if x lies inside the expanded committed interval → CommittedZone holds;
    otherwise CommittedZone = rawZone(x) (raw thresholds — well-defined for
    non-adjacent jumps)
```

- **Units/ranges:** x ∈ [0, 105] m team-relative; H ≥ 0 (and H < (T2−T1)/2 so expanded intervals
  stay proper).
- **Worked example:** committed = OwnThird (claims [0, 37.0)), ball at x = 35.5 m → holds. Ball
  reaches 37.2 m → outside → rawZone commits MiddleThird (now claims [33.0, 72.0)). Ball drops to
  34.1 m → holds MiddleThird; at 32.9 m → commits OwnThird. A ball dithering ±1.9 m around 35 m
  never flaps the zone. **Long-ball example (non-adjacent):** committed OwnThird, ball cleared to
  x = 80 m → outside [0, 37.0) → rawZone(80) = FinalThird directly — no intermediate
  MiddleThird tick.
- The classifier runs once per team per heartbeat, before the overlay stage (same
  current-tick-ordering rule as `RestDefenseEvaluator` consumption).

## 3.2 Overlay stage (FM-BU-02; #12 `SlotComposer`)

For each active outfield slot when FR-BU-004's gate passes:

```
Δ = OverlayTable[structure][CommittedZone][slot.DefaultLine][slot.DefaultLane]   # Appendix A
composedTarget += (Δ.Dx, Δ.Dy)      # team-relative; mirrored to world frame by #12's existing transform
```

Every Δ satisfies |Dx|,|Dy| ≤ `BUILDUP_OFFSET_MAX_M` `[GT]` = 8.0 m (FR-BU-008). Zones
`FinalThird` and structures' unlisted rows are implicit (0,0) — build-up structure is an
own/middle-third concept (FR-BU-004); in the final third the existing #12/#15 attacking shapes own
positioning.

- **Worked example (BackThree, OwnThird; lane keys per PASS-1 M-3):** the DEF-line wide **L/R**
  lane slots get Δ = (−4.0, 6.0 toward pitch centre) — fullbacks tuck deep and narrow beside the
  centre-backs; the MID-line half-space **LH/RH** slots get Δ = (−4.0, 0.0) — the central pair
  drops toward the back line. A 4-4-2's left back at composed target (18.0, 12.0) team-relative
  moves toward the y = 34 centreline: (18.0 − 4.0, 12.0 + 6.0) = (14.0, 18.0). The catalogue in
  Appendix A stores lane-symmetric magnitudes; the lateral sign resolves toward pitch centre per
  lane side (y < 34 ⇒ +, y > 34 ⇒ −, **0 at exactly y = 34** — PASS-1 L-1), which keeps one table
  row serving both flanks.

## 3.3 Post-regain suppression window (FM-BU-03)

```
team-level regain (PASS-1 M-1):
    settledTeam = team of the current settled possessor (per-agent holder ids
                  mapped to teams; loose-ball −1 does not change settledTeam)
    on settledTeam transition opponent → this team:
        SuppressTicksRemaining = TransitionWon ∈ {CounterAttack, CounterPress}
                                 ? REGAIN_SUPPRESS_TICKS : 0
    (intra-team possessor changes never re-arm — the raw PossessionChangedEvent
     fires on teammate receptions too, PreviousHolder/NewHolder being agent ids)
per heartbeat:
    SuppressTicksRemaining = max(0, SuppressTicksRemaining − 1)
overlay gate: … AND SuppressTicksRemaining == 0     (FR-BU-004)
```

`REGAIN_SUPPRESS_TICKS` `[GT]` = 30 (3.0 s at 10 Hz — the counter-attack exploitation window
before a team settles into structure).

- **Worked example:** `TransitionWon = CounterAttack`, regain at heartbeat 100 → overlay suppressed
  through heartbeat 129, active again from 130 if still `InPoss`. With `HoldShape`: active from the
  first `InPoss` heartbeat.

## 3.4 Constants

| Constant | Tag | Value | Units |
|---|---|---|---|
| `BUILDUP_ZONE_THRESHOLD_1_M` / `_2_M` | `[DERIVED]` | 35.0 / 70.0 | m (= PITCH_LENGTH_M/3, 2·PITCH_LENGTH_M/3) |
| `BUILDUP_ZONE_HYSTERESIS_M` | `[GT]` | 2.0 | m |
| `BUILDUP_OFFSET_MAX_M` | `[GT]` | 8.0 | m |
| `REGAIN_SUPPRESS_TICKS` | `[GT]` | 30 | heartbeats |
| Overlay tables (Appendix A) | `[GT]` | per-row | m |

`[GT]` magnitudes are shapes-reviewed; pinned at the balance pass (#21 G2 precedent, §9.2).

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-08 | — | Initial FM-BU-01..03 with units, ranges, worked examples. |
| 0.2 | 2026-07-08 | — | PASS-1 fixes: M-2 committed-zone-expansion hysteresis formulation (+ long-ball example); M-1 team-level-regain arming; M-3 lane keys in the §3.2 example; L-1 centreline sign pin. |
#endregion
