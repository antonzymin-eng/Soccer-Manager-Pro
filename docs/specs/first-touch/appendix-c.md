## Appendix C: Sensitivity Analysis

This appendix quantifies how each input variable affects the output (q, r, possession
outcome) across the realistic range of match inputs. Sensitivity analysis serves two
purposes: (1) identifying which inputs most strongly affect outcomes, enabling focused
tuning; (2) documenting expected output distributions to detect formula regression.

All sensitivity calculations hold all other inputs at representative baseline values:
- Technique = 13, FirstTouch = 12 (mid-tier player: WeightedAttr = 12.70, NormAttr = 0.635)
- ballSpeed = 15.0 m/s (reference pass speed)
- agentSpeed = 2.0 m/s (light jog)
- pressureScalar = 0.0 (no pressure)
- IsHalfTurnOriented = false

**Baseline calculation:**
```
AttrWithBonus = 0.635
VelDifficulty = 1.00
MoveDifficulty = 1.0 + (2.0/7.0) × 0.5 = 1.0 + 0.143 = 1.143
RawQuality = 0.635 / 1.143 = 0.555
q_baseline = 0.555
```

---

### C.1 Control Quality vs. Technique Attribute

Holding FirstTouch = 12, all other inputs at baseline. Technique varies 1–20.

| Technique | WeightedAttr | NormAttr | AttrWithBonus | q | Band |
|-----------|-------------|---------|--------------|---|------|
| 1 | 12×0.30 + 1×0.70 = 4.30 | 0.215 | 0.215 | 0.188 | Heavy |
| 5 | 3.60+3.50=7.10 | 0.355 | 0.355 | 0.311 | Heavy |
| 8 | 3.60+5.60=9.20 | 0.460 | 0.460 | 0.403 | Poor |
| 10 | 3.60+7.00=10.60 | 0.530 | 0.530 | 0.464 | Poor |
| 12 | 3.60+8.40=12.00 | 0.600 | 0.600 | 0.525 | Poor |
| 13 (baseline) | 3.60+9.10=12.70 | 0.635 | 0.635 | 0.555 | Poor |
| 15 | 3.60+10.50=14.10 | 0.705 | 0.705 | 0.617 | Good |
| 17 | 3.60+11.90=15.50 | 0.775 | 0.775 | 0.678 | Good |
| 20 | 3.60+14.00=17.60 | 0.880 | 0.880 | 0.770 | Good |

**Sensitivity:** ∂q/∂Technique ≈ +0.030 per Technique point (0.70 × (1/20) / 1.143 ≈ 0.031).
Technique improvement from 10 to 15 (+5 points) produces Δq ≈ +0.153, enough to shift
from Poor to Good band. From 15 to 20 (+5 points) produces Δq ≈ +0.153 again, moving
deeper into Good band.

**Key insight:** Every 5 Technique points shifts q by approximately 0.15 at baseline
conditions. This validates the [1–20] attribute scale as providing meaningful differentiation
without excessive sensitivity.

---

### C.2 Control Quality vs. Ball Velocity

Holding all inputs at baseline (mid-tier player). Ball velocity varies 5–35 m/s.

| Ball Speed (m/s) | VelDifficulty | q | Band | Outcome (no pressure, no opponents) |
|---|---|---|---|---|
| 5.0 | 0.333 | 1.0 (clamped) | Perfect | CONTROLLED |
| 8.0 | 0.533 | 0.989 | Perfect | CONTROLLED |
| 10.0 | 0.667 | 0.726 | Good | CONTROLLED (if r ≤ 0.60m) |
| 12.0 | 0.800 | 0.605 | Good | CONTROLLED (if r ≤ 0.60m) |
| 15.0 | 1.000 | 0.555 (baseline) | Poor | LOOSE_BALL |
| 18.0 | 1.200 | 0.463 | Poor | LOOSE_BALL |
| 20.0 | 1.333 | 0.416 | Poor | LOOSE_BALL |
| 24.0 | 1.600 | 0.347 | Poor | LOOSE_BALL |
| 28.0 | 1.867 | 0.297 | → 0.30 (thunderbolt cap) | INTERCEPTION/DEFLECTION |
| 32.0 | 2.133 | → 0.30 (thunderbolt cap) | capped | INTERCEPTION/DEFLECTION |
| 35.0 | 2.333 | → 0.30 (thunderbolt cap) | capped | INTERCEPTION/DEFLECTION |

**Note on possession outcomes:** Radius calculation (modifier included) determines CONTROLLED
vs. LOOSE_BALL more than q threshold alone. At 10 m/s: r = 0.10 + (10/15)×0.25 = 0.10+0.167
= 0.267m → CONTROLLED. At 12 m/s: r_good at q=0.605 → base 0.50m + modifier 0.20m = 0.70m
→ LOOSE_BALL (r > 0.60m).

**Key insight:** For a mid-tier player, the practical CONTROLLED/LOOSE_BALL threshold is
around 10–11 m/s. At 15 m/s (reference speed), mid-tier players reliably produce LOOSE_BALL.
This matches the design intent: CONTROLLED outcomes at reference speed require above-average
players (Technique ≥ 15).

---

### C.3 Touch Radius Distribution at Typical Match Inputs

Holding q constant, ball speed varies 5–30 m/s to show velocity modifier effect.

| q | Band | r_base | +0.25m (15m/s) | +0.47m (28m/s) | +0.50m (30m/s) |
|---|------|--------|-----------------|-----------------|-----------------|
| 1.00 | Perfect | 0.10m | 0.35m | 0.57m | 0.60m |
| 0.90 | Perfect | 0.20m | 0.45m | 0.67m | 0.70m |
| 0.85 | Good→ | 0.30m | 0.55m | 0.77m | 0.80m |
| 0.70 | Good | 0.42m | 0.67m | 0.89m | 0.92m |
| 0.60 | Poor→ | 0.60m | 0.85m | 1.07m | 1.10m |
| 0.50 | Poor | 0.72m | 0.97m | 1.19m | 1.22m |
| 0.35 | Heavy→ | 1.20m | 1.45m | 1.67m | 1.70m |
| 0.20 | Heavy | 1.63m | 1.88m | capped 2.00m | capped 2.00m |
| 0.00 | Heavy | 2.00m | capped 2.00m | capped 2.00m | capped 2.00m |

**Key insight:** The velocity modifier is significant — +0.25m at reference speed and
+0.47m at thunderbolt speed. A player with q = 1.00 receiving a thunderbolt (28 m/s) has
r = 0.57m, which exceeds CONTROLLED_RADIUS (0.60m). Combined with the thunderbolt quality
cap (q → 0.30), thunderbolt balls are practically impossible to control, consistent with
design intent.

**The velocity modifier makes CONTROLLED much harder at high ball speeds.** Even perfect
quality (q = 1.0) requires ball speeds below ~20 m/s to stay within CONTROLLED_RADIUS.

---

### C.4 Pressure Degradation Sensitivity

Holding baseline inputs, pressureScalar varies 0.0–1.0.

| pressureScalar | Penalty (×0.40) | Multiplier | q (from baseline RawQ = 0.555) | Δq from no-pressure |
|---|---|---|---|---|
| 0.0 | 0.0 | 1.000 | 0.555 | 0 |
| 0.1 | 0.04 | 0.960 | 0.533 | -0.022 |
| 0.2 | 0.08 | 0.920 | 0.511 | -0.044 |
| 0.3 | 0.12 | 0.880 | 0.488 | -0.067 |
| 0.5 | 0.20 | 0.800 | 0.444 | -0.111 |
| 0.7 | 0.28 | 0.720 | 0.400 | -0.155 |
| 1.0 | 0.40 | 0.600 | 0.333 | -0.222 |

**Scenario mapping:**
- Opponent at 3.0m (PRESSURE_RADIUS boundary): contribution ≈ 0 → pressureScalar ≈ 0
- Opponent at 2.0m: contribution = 0.333 → pressureScalar = 0.222 → Δq = -0.049
- Opponent at 1.0m: contribution = 0.667 → pressureScalar = 0.444 → Δq = -0.098
- Opponent at 0.5m: contribution = 0.833 → pressureScalar = 0.556 → Δq = -0.123
- Two opponents at 1.5m: pressureScalar = 0.667 → Δq = -0.148

**Key insight:** A single opponent at 1.0m reduces q by approximately 0.10 — enough to
push a mid-tier player from Poor band into Heavy band at that distance. Two opponents at
1.5m reduce q by ~0.15. The pressure model is meaningful but not punishingly strong.

---

### C.5 Half-Turn Bonus Impact Across Attribute Tiers

Half-turn bonus applied to players of different attribute tiers at baseline speed (15 m/s).

| Player Tier | Technique | FirstTouch | q (no HT) | q (HT) | Δq | Outcome change? |
|-------------|-----------|-----------|-----------|---------|-----|-----------------|
| Poor | 5 | 5 | 0.266 | 0.306 | +0.040 | No (both Heavy) |
| Below avg | 8 | 8 | 0.383 | 0.440 | +0.057 | No (both Poor) |
| Average | 12 | 11 | 0.555 | 0.638 | +0.083 | No (Poor→Good, but r pushes to LOOSE_BALL) |
| Above avg | 15 | 14 | 0.673 | 0.774 | +0.101 | Possible (r improvement) |
| Elite | 18 | 17 | 0.764 | 0.879 | +0.115 | Yes (Good→Perfect band) |
| Peak | 20 | 20 | 0.880 | 1.012→1.0 | +0.120→capped | Minimal (both at ceiling) |

**Key insight:** Half-turn bonus provides the largest *absolute* benefit to above-average
and elite players. For poor players, it moves them further within their band but rarely
across a band boundary. This is the intended design: half-turn orientation rewards skilled
players disproportionately, reflecting the real principle that technique amplifies the
benefits of good body positioning.

---

### C.6 Sensitivity Summary and Tuning Guidance

| Variable | Sensitivity per Unit | Typical Match Range | Δq at extremes | Primary Tuning Lever |
|---|---|---|---|---|
| Technique | +0.031/point | 5–20 | +0.46 | ATTR_MAX, TECHNIQUE_WEIGHT |
| FirstTouch | +0.013/point | 5–18 | +0.17 | FIRST_TOUCH_WEIGHT |
| Ball Speed | –0.021/(m/s) | 8–28 m/s | –0.42 | VELOCITY_REFERENCE, VELOCITY_MAX_FACTOR |
| Agent Speed | –0.040/(m/s) | 0–7 m/s | –0.17 | MOVEMENT_PENALTY |
| Pressure scalar | –0.222 at max | 0–1.0 | –0.22 | PRESSURE_WEIGHT |
| Half-turn | +0.10 to +0.12 | Binary | +0.10 avg | HALF_TURN_BONUS |

**Tuning guidance by symptom:**

| Symptom | Diagnosis | Recommended Adjustment |
|---|---|---|
| Too many CONTROLLED outcomes | q floor too high | Reduce VELOCITY_REFERENCE to 12.0 or increase CONTROLLED_THRESHOLD to 0.60 |
| Too many INTERCEPTION outcomes | Touch radii too large | Reduce VELOCITY_RADIUS_FACTOR from 0.25 to 0.15 |
| Pressure feels too punishing | PRESSURE_WEIGHT too high | Reduce from 0.40 to 0.30 |
| Pressure feels irrelevant | PRESSURE_WEIGHT too low | Increase from 0.40 to 0.50 |
| Elite players indistinguishable from good | Attribute sensitivity too low | Increase TECHNIQUE_WEIGHT from 0.70 to 0.75, reduce FIRST_TOUCH_WEIGHT to 0.25 |
| Sprint reception too punishing | MOVEMENT_PENALTY too high | Reduce from 0.50 to 0.35 |
| Half-turn too impactful | HALF_TURN_BONUS too large | Reduce from 0.15 to 0.10 |
| Thunderbolt outcomes too common | THUNDERBOLT_SPEED too low | Increase from 28 to 32 m/s |

**Constants interaction warning:** VELOCITY_REFERENCE and VELOCITY_RADIUS_FACTOR interact.
Changing VELOCITY_REFERENCE affects both q (via VelDifficulty) and r (via the radius
modifier). Changing one without the other creates asymmetric effects. If VELOCITY_REFERENCE
is adjusted, re-run Appendix B.1 and B.2 to verify expected values.

---

**End of Appendices**

**Appendix A:** Formula Derivations — 6 subsystems, complete mathematical derivation chain
**Appendix B:** Numerical Verification — 7 hand calculations including discovered VS-001 discrepancy
**Appendix C:** Sensitivity Analysis — 5 variable sweeps, full tuning guidance table

**Blocker 1 from §9 Approval Checklist:** RESOLVED — Appendices A, B, C all present.

**Outstanding item from Appendix B.1:** VS-001 ball speed (currently 18 m/s) produces
LOOSE_BALL, not CONTROLLED as specified. Recommend revising VS-001 to ball speed 12 m/s
before approval sign-off. This is an additional pre-approval item requiring lead developer
decision.

---

**Page Count:** ~35 pages
**Version:** 1.0
**Dependencies completed:** Sections 1–8 of First Touch Mechanics Specification #4
