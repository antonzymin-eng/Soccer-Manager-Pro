## Appendix C: Sensitivity Analysis

These tables show how key outputs vary across the full input range. They serve two
purposes: (1) confirming monotonicity properties required by several PE- tests, and
(2) providing playtesting calibration reference — if these ranges feel wrong in-game,
the relevant [GT] constant is the calibration target.

### C.1 Velocity vs. KickPower and Distance

**Pass type: Ground. Fatigue = 0. V_OFFSET = 8.0.**

| KickPower \ Distance | 5m    | 15m   | 25m   | 35m (D_MAX) |
|---------------------|-------|-------|-------|-------------|
| 1                   | 8.07  | 8.21  | 8.36  | 8.50        |
| 5                   | 8.36  | 9.07  | 9.79  | 10.50       |
| 10                  | 8.71  | 10.14 | 11.57 | 13.00       |
| 15                  | 9.07  | 11.21 | 13.36 | 15.50       |
| 20 (clamp likely)   | 9.43  | 12.29 | 15.14 | 18.00 (cap) |

All values in m/s. Values at V_MAX_Ground cap (18.0) will be clamped.
Strictly increasing in both KickPower and Distance ✓ — confirms PV-008 (monotone) will pass.

---

### C.2 ErrorAngle vs. Passing Attribute

**Pass type: Ground. All other modifiers at neutral (1.0). BASE_ERROR = 1.5°.**

| Passing | PassingModifier | ErrorAngle (°) | Lateral miss at 20m |
|---------|----------------|----------------|---------------------|
| 1       | 2.800          | 4.20           | 1.47 m              |
| 3       | 2.552          | 3.83           | 1.34 m              |
| 5       | 2.305          | 3.46           | 1.21 m              |
| 8       | 1.934          | 2.90           | 1.01 m              |
| 10      | 1.687          | 2.53           | 0.88 m              |
| 12      | 1.440          | 2.16           | 0.75 m              |
| 15      | 1.068          | 1.60           | 0.56 m              |
| 18      | 0.695          | 1.04           | 0.36 m              |
| 20      | 0.450          | 0.675          | 0.24 m              |

Strictly monotone decreasing ✓. Lateral miss column provides football-grounded
calibration reference: a Passing=10 player missing by 0.88m at 20m is within a
comfortable reception range; Passing=1 missing by 1.47m at 20m is difficult to
control but not completely erratic.

---

### C.3 ErrorAngle vs. Pressure Scalar

**Pass type: Ground. Passing=10 (neutral), all other modifiers neutral.**

| PressureScalar | PressureModifier | ErrorAngle (°) | Lateral miss at 20m |
|----------------|-----------------|----------------|---------------------|
| 0.0 (none)     | 1.000           | 2.53           | 0.88 m              |
| 0.2 (light)    | 1.100           | 2.78           | 0.97 m              |
| 0.4 (moderate) | 1.200           | 3.04           | 1.06 m              |
| 0.6 (heavy)    | 1.300           | 3.29           | 1.15 m              |
| 0.8 (intense)  | 1.400           | 3.54           | 1.24 m              |
| 1.0 (maximum)  | 1.500           | 3.80           | 1.33 m              |

Strictly monotone increasing ✓. The difference between no pressure (0.88m miss) and
maximum pressure (1.33m miss) is 0.45m — a meaningful but not extreme degradation
for a mid-range passer. Under pressure at PE-003, this confirms monotone increase.

---

### C.4 Combined Error: Pressure + Fatigue

**Pass type: Ground. Passing=10, BodyAngle=0°, Urgency=0, IsWeakFoot=false.**

| Fatigue \ Pressure | 0.0     | 0.25    | 0.50    | 0.75    | 1.0     |
|--------------------|---------|---------|---------|---------|---------|
| 0.0                | 2.53°   | 2.85°   | 3.16°   | 3.48°   | 3.80°   |
| 0.25               | 2.66°   | 2.99°   | 3.32°   | 3.65°   | 3.98°   |
| 0.50               | 2.78°   | 3.13°   | 3.47°   | 3.82°   | 4.16°   |
| 0.75               | 2.91°   | 3.27°   | 3.63°   | 3.99°   | 4.35°   |
| 1.0                | 3.04°   | 3.42°   | 3.79°   | 4.17°   | 4.55°   |

All values increase both moving right (more pressure) and moving down (more fatigue) ✓.
The combined worst case (Fatigue=1.0, Pressure=1.0) at mid-range passing = 4.55° — roughly
double the fresh, no-pressure baseline. This is in a plausible range before urgency and
body angle penalties compound further.

---

### C.5 Spin Magnitude vs. Technique Attribute

**SPIN_BASE = 8.0 rad/s (Ground), 15.0 rad/s (Cross), 12.0 rad/s (Chip).**

| Technique | TechniqueScale | Ground (rad/s) | Cross (rad/s) | Chip (rad/s) |
|-----------|---------------|----------------|---------------|--------------|
| 1         | 0.500         | 4.00           | 7.50          | 6.00         |
| 5         | 0.711         | 5.69           | 10.66         | 8.53         |
| 10        | 0.974         | 7.79           | 14.61         | 11.68        |
| 15        | 1.237         | 9.89           | 18.55         | 14.84        |
| 20        | 1.500         | 12.00          | 22.50         | 18.00        |

Strictly monotone increasing in Technique for all pass types ✓ (SV-004 will pass).
Cross spin at Technique=20 (22.50 rad/s) is below the 25 rad/s upper range documented
in [BRAY-2003] for general passes — within plausible bounds pending Ball Physics Magnus
cross-spec validation.

---

### C.6 WeakFootModifier vs. WeakFootRating

**WEAK_FOOT_BASE_PENALTY = 0.30. IsWeakFoot = true for all rows.**

| WeakFootRating | PenaltyFraction | WeakFootModifier | Error Multiplier Effect |
|---------------|----------------|-----------------|------------------------|
| 1 (worst)     | 1.00           | 1.300           | +30% error             |
| 2             | 0.75           | 1.225           | +22.5% error           |
| 3             | 0.50           | 1.150           | +15% error             |
| 4             | 0.25           | 1.075           | +7.5% error            |
| 5 (ambi)      | 0.00           | 1.000           | No penalty             |

Strictly monotone decreasing as WeakFootRating improves ✓.
At Rating=1, the 30% error multiplier sits at the upper bound of [CAREY-2001]'s
documented 15–25% range. Recommend reducing WEAK_FOOT_BASE_PENALTY to 0.25 to
bring the maximum penalty within the academic-informed range. Flag as OI-App-C-01.

> ⚠ **OI-App-C-01:** WEAK_FOOT_BASE_PENALTY = 0.30 produces a maximum penalty of
> 30%, slightly exceeding [CAREY-2001]'s documented 25% maximum. Reduce to 0.25 for
> tighter academic grounding, or retain at 0.30 and explicitly reclassify as [GT].

---

## Open Issues Summary (Appendix-Identified)

All issues identified in v1.0 have been resolved in v1.1.

| ID          | Location  | Description                                             | Severity | Status |
|-------------|-----------|--------------------------------------------------------|----------|--------|
| OI-App-B-01 | B.1.1    | V_base formula needed V_OFFSET term                    | HIGH     | ✅ RESOLVED — AM-003-001 corrects §3.2 formula; A.1.2 updated |
| OI-App-B-02 | B.1.3    | PV-004 test expectation misaligned with corrected formula | MODERATE | ✅ RESOLVED — Section 5 v1.1 PV-004 updated to range check |
| OI-App-B-03 | B.1.4    | PV-005 condition did not produce V_MAX clamp           | MODERATE | ✅ RESOLVED — Section 5 v1.1 PV-005 uses Distance=D_MAX_Chip |
| OI-App-B-04 | B.2.4    | LA-006 expectation INVERTED — physics was correct      | CRITICAL | ✅ RESOLVED — Section 5 v1.1 LA-006 corrected; test name updated |
| OI-App-B-05 | B.3.1    | PE-001 referenced undefined constant "MIN_ERROR"       | MODERATE | ✅ RESOLVED — Section 5 v1.1 PE-001 uses explicit ELITE_ERROR constant |
| OI-App-C-01 | C.6      | WEAK_FOOT_BASE_PENALTY = 0.30 exceeds [CAREY-2001] 25% max | LOW  | ⚠ CARRIED — design decision deferred to playtesting; constant reclassified as [GT] in §8.6.5 |

**OI-App-C-01 rationale:** Reducing WEAK_FOOT_BASE_PENALTY to 0.25 would bring the
maximum penalty within the [CAREY-2001] documented range. However, the current 0.30
value was chosen as a gameplay-tuned starting point. It is more conservative to leave
it at 0.30 with an explicit [GT] flag and calibrate during playtesting, rather than
forcing it to an academic bound for a constant that is inherently a gameplay tuning
parameter. The §8.6.5 citation audit already flags WeakFootModifier formula as
ACADEMIC-INFORMED + [GT]. No further action required before approval.

---

## Version History

| Version | Date | Author | Notes |
|---------|------|--------|-------|
| 1.0 | February 20, 2026, 11:59 PM PST | Claude (AI) / Anton | Initial draft. 7 formula derivations (A.1–A.7). Numerical verification for 12 key tests. 6 sensitivity tables. 6 open issues identified during hand-calculation — including 1 CRITICAL (OI-App-B-04: LA-006 inverted expectation) and 1 HIGH (OI-App-B-01: V_base formula correction required). |
| 1.1 | February 21, 2026 | Claude (AI) / Anton | All 5 blocking/high/moderate open issues resolved. A.1.2 updated with corrected V_OFFSET formula per AM-003-001. A.1.5 constants table adds V_OFFSET column. B.1.1 recalculated with corrected formula — PV-001 now passes. B.1.3 updated — PV-004 test expectation corrected to range check. B.1.4 updated — PV-005 condition corrected to D_MAX. B.2.4 updated — LA-006 expectation corrected, physics explanation added. B.3.1 updated — PE-001 uses explicit ELITE_ERROR constant. OI-App-C-01 (WEAK_FOOT_BASE_PENALTY) carried as deferred [GT] design decision. |

---

*End of Pass Mechanics Specification #5 — Appendices A, B, C*

*Next: Section 9 — Approval Checklist*
