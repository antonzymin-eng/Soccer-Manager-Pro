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

commit rule (H = BUILDUP_ZONE_HYSTERESIS_M [GT] = 2.0 m):
    newZone = rawZone(x) computed with each boundary shifted H **away from**
              the committed zone (i.e. leaving OwnThird upward requires x ≥ T1 + H;
              re-entering it requires x < T1 − H)
    CommittedZone = newZone
```

- **Units/ranges:** x ∈ [0, 105] m team-relative; H ≥ 0.
- **Worked example:** committed = OwnThird, ball at x = 35.5 m: leaving requires ≥ 37.0 m → holds
  OwnThird. Ball reaches 37.2 m → commits MiddleThird. Ball drops to 34.1 m: re-entry requires
  < 33.0 m → holds MiddleThird; at 32.9 m → commits OwnThird. A ball dithering ±1.9 m around 35 m
  never flaps the zone.
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

- **Worked example (BackThree, OwnThird):** the DEF-line LH/RH lane slots get Δ = (−4.0, 6.0 toward
  pitch centre) — fullbacks tuck deep and narrow beside the centre-backs; the MID-line central slot
  gets Δ = (−6.0, 0.0) — the pivot drops between the lines. A 4-4-2's left back at composed target
  (18.0, 12.0) team-relative moves toward the y = 34 centreline: (18.0 − 4.0, 12.0 + 6.0) =
  (14.0, 18.0). The catalogue in Appendix A stores lane-symmetric magnitudes; the lateral sign
  resolves toward pitch centre per lane side (y < 34 ⇒ +, y > 34 ⇒ −), which keeps one table row
  serving both flanks.

## 3.3 Post-regain suppression window (FM-BU-03)

```
on possession regained (possession-changed signal, new possessor = this team):
    SuppressTicksRemaining = TransitionWon ∈ {CounterAttack, CounterPress}
                             ? REGAIN_SUPPRESS_TICKS : 0
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
#endregion
