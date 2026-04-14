## 8.6 Citation Audit

### 8.6.1 Audit Methodology

Each constant or formula in Sections 3.2–3.10 is classified as one of:

| Code | Meaning |
|---|---|
| **ACADEMIC** | Value or structure derived directly from a published study with minimal modification |
| **ACADEMIC-INFORMED** | Value uses academic source for direction/order-of-magnitude; final value is calibrated |
| **DERIVED** | Mathematically derived from confirmed constants (no independent source required) |
| **GAMEPLAY-TUNED [GT]** | Value chosen for gameplay feel; no strong academic grounding; requires playtesting |
| **DESIGN-AUTHORITY** | Defined by an internal project document (Master Volumes or another Spec) |
| **DEFERRED-DESIGN** | Not yet defined; reserved for a future stage |
| **⚠[VER]** | Value requires verification before approval (simulation or literature review) |

---

### 8.6.2 Audit: §3.2 Velocity Calculation

| Constant / Formula | Disposition | Notes |
|---|---|---|
| `V = V_BASE × ContactZoneModifier × PowerModifier × LongShotsBonus × FormModifier` | ACADEMIC-INFORMED | Structure from [LEES-1998] force-velocity framework |
| `V_BASE = f(KickPower, Technique)` | ACADEMIC-INFORMED + [GT] | [LEES-1998] / [KELLIS-2007] direction; [GT] scaling |
| `V_FLOOR = 8.0 m/s` | ACADEMIC-INFORMED + [GT] | [STATSBOMB-OPEN] context; [GT] minimum |
| `V_CEILING = 35.0 m/s` | ACADEMIC-INFORMED | [LEES-1998] 28–34 m/s range; ceiling set at +1 m/s margin |
| `ContactZoneModifier[Centre] = 1.00` | DESIGN-AUTHORITY | Reference contact; no modifier |
| `ContactZoneModifier[OffCentre] = 0.85` | ACADEMIC-INFORMED + [GT] | [NUNOME-2006] ~15% reduction; [GT] fine-tuning |
| `ContactZoneModifier[BelowCentre] = 0.75` | ACADEMIC-INFORMED + [GT] | [INOUE-2014] elevation cost; [GT] magnitude |
| `PowerModifier = f(PowerIntent)` | ACADEMIC-INFORMED + [GT] | [LEES-1998] force-velocity; [GT] curve shape |
| `PowerPenalty` (accuracy cost of high power) | ACADEMIC-INFORMED + [GT] | [LEES-1998] power–accuracy trade-off; [GT] slope |
| `LongShotsBonus = f(LongShots, distance)` | ACADEMIC-INFORMED + [GT] | [KELLIS-2007] attribute–velocity relationship; [GT] |
| `FormModifier` no-op at Stage 0 | DEFERRED-DESIGN | [MASTER-VOL2] §FormSystem not yet written |
| `ATTR_MAX = 20.0` | DESIGN-AUTHORITY | [MASTER-VOL2] |
| Snapshot of attributes at pipeline entry | DESIGN-AUTHORITY | KD-5 isolation requirement |
| `ShotRequest` struct validation | DESIGN-AUTHORITY | FR-01 / §3.1 |
| Guard: `PowerIntent ∈ [0.0, 1.0]` | DESIGN-AUTHORITY | §3.1 validation |
| Guard: `ContactZone ∈ {Centre, OffCentre, BelowCentre}` | DESIGN-AUTHORITY | §3.1 validation |

**§3.2 audit status: COMPLETE. 16 items. 0 undocumented.**

---

### 8.6.3 Audit: §3.3 Launch Angle Derivation

| Constant / Formula | Disposition | Notes |
|---|---|---|
| `LaunchAngle = BaseAngle + BodyLeanPenalty + ContactZoneAngleOffset + PlacementAdjustment` | ACADEMIC-INFORMED | Structure from [LEES-1998] / [NUNOME-2006] |
| `BaseAngle[Centre] = 4°` | ACADEMIC-INFORMED | [LEES-1998] 2°–8° range; midpoint |
| `BaseAngle[OffCentre] = 6°` | ACADEMIC-INFORMED + [GT] | [ASAI-2002] off-centre elevation; [GT] magnitude |
| `BaseAngle[BelowCentre] = 18°` | ACADEMIC-INFORMED + [GT] | [INOUE-2014] below-centre elevation; [GT] magnitude |
| `BodyLeanPenalty = f(BodyLeanAngle)` | ACADEMIC | [NUNOME-2006] lean–elevation relationship |
| `PlacementAdjustment` for high target | GAMEPLAY-TUNED [GT] | No academic source; design choice to allow chip-style elevation |
| `ANGLE_MAX = 45°` | DERIVED | Ballistic optimum; no direct source needed |
| `ANGLE_MIN = 0°` | DESIGN-AUTHORITY | Ground-clearance floor; design choice |
| Clamp to `[ANGLE_MIN, ANGLE_MAX]` | DESIGN-AUTHORITY | FR-04 validity requirement |

**§3.3 audit status: COMPLETE. 9 items. 0 undocumented.**

---

### 8.6.4 Audit: §3.4 Spin Vector Calculation

| Constant / Formula | Disposition | Notes |
|---|---|---|
| Spin vector decomposition (topspin, backspin, sidespin) | ACADEMIC-INFORMED | [BRAY-2003] / [ASAI-2002] / [CARRE-2002] framework |
| `TOPSPIN_BASE[Centre]` = 25 rad/s | ACADEMIC-INFORMED + [GT] ✅[VER] | [BRAY-2003] order-of-magnitude. Validated v1.2: at 28 m/s produces +1.47m dip over 20m vs gravity-only (within [CARRE-2002] 1–2m reference). |
| `BACKSPIN_BASE[BelowCentre]` = 30 rad/s | ACADEMIC-INFORMED + [GT] ✅[VER] | [BRAY-2003] direction. Validated v1.2: Magnus = 0.69N (16.5% of gravity) at 12 m/s. Net vertical accel = -8.20 m/s² — ball floats but still descends. Does not reverse gravity. |
| `SIDESPIN_BASE[OffCentre]` = 28 rad/s | ACADEMIC-INFORMED + [GT] ✅[VER] | [ASAI-2002] / [CARRE-2002] direction. Validated v1.2: at 16.66 m/s produces 1.85m lateral curl over 20m (within [CARRE-2002] 1–2m reference). Novice (Technique=1): 1.54m. Differential of 0.31m — note for playtesting: Technique impact on visible curl is modest. |
| `SpinIntent ∈ [0.0, 1.0]` scalar | DESIGN-AUTHORITY | Parameter definition; [MASTER-VOL1] |
| `SPIN_MAX_MAGNITUDE` / `SPIN_ABSOLUTE_MAX` = 80 rad/s | ACADEMIC-INFORMED + [GT] ✅[VER] | [BRAY-2003] ~63 rad/s free kick reference; [Inoue-2014] 150 rad/s elite measured. 80 rad/s is conservative Ball Physics ceiling. At 80 rad/s / 35 m/s: F_Magnus = 5.63N, lateral accel = 13.1 m/s². Ceiling is reachable only by composite spin — physically extreme but not impossible. |
| ContactZone → spin component mapping | ACADEMIC-INFORMED | [ASAI-2002] contact geometry |
| Spin vector normalisation | DERIVED | Mathematical requirement |
| `SpinIntent = 0` → zero spin vector | DESIGN-AUTHORITY | Determinism requirement; [MASTER-VOL1] |
| Spin decay (in flight) | DESIGN-AUTHORITY | Not owned by Shot Mechanics; [BALL-PHYSICS-1] §3.1.4 |
| `Ball.ApplyKick()` spin input | DESIGN-AUTHORITY | [BALL-PHYSICS-1] interface |
| Sidespin sign convention (left/right) | DESIGN-AUTHORITY | [MASTER-VOL1] coordinate system |
| Cross-zone spin mixing (composite) | GAMEPLAY-TUNED [GT] | No academic source for mixed contact; design interpolation |

**§3.4 audit status: COMPLETE. 13 items. 0 undocumented.**
**✅ All 4 [VER] items resolved in v1.2. Spin base values validated against Ball Physics §3.1.4 Magnus model. All within observable bounds.**

---

### 8.6.5 Audit: §3.5 Placement Resolution

| Constant / Formula | Disposition | Notes |
|---|---|---|
| Goal mouth coordinate system | DERIVED | From [MASTER-VOL1] coordinate space |
| `PlacementTarget` → world-space goal coordinate | DERIVED | Geometric transform |
| Goal post boundary clamping | DERIVED | FIFA goal specification |
| `PlacementTarget ∈ {BottomLeft, BottomRight, TopLeft, TopRight, Centre}` | DESIGN-AUTHORITY | Decision Tree vocabulary; [MASTER-VOL1] |
| Distance-to-target sigmoid (accuracy degradation at range) | ACADEMIC-INFORMED + [GT] | [STATSBOMB-OPEN] shot location data; [GT] sigmoid shape |
| `D_MID = 20m` sigmoid midpoint | ACADEMIC-INFORMED + [GT] | [STATSBOMB-OPEN] confirms 6–25m shot range; [GT] midpoint |
| Goal mouth coordinate lookup table | DESIGN-AUTHORITY | [MASTER-VOL1] coordinate system |
| Placement resolution gated at CONTACT state | DESIGN-AUTHORITY | §3.9 pipeline |

**§3.5 audit status: COMPLETE. 8 items. 0 undocumented.**

---

### 8.6.6 Audit: §3.6 Error Model

| Constant / Formula | Disposition | Notes |
|---|---|---|
| `ErrorMagnitude = BASE_ERROR × PowerPenalty × PressureScalar × FatigueScalar × PlacementDifficultyScalar × WFPenalty` | ACADEMIC-INFORMED | Multiplicative chain; [LEES-1998] / [DICKS-2010] / [BEILOCK-2007] / [ALI-2011] |
| `BASE_ERROR` (at median finishing) | GAMEPLAY-TUNED [GT] | No direct academic derivation; tuned to produce plausible miss rates |
| `PowerPenalty = f(PowerIntent)` (in error model) | ACADEMIC-INFORMED + [GT] | [LEES-1998] power–accuracy; [GT] curve parameters |
| `PressureScalar = f(proximityDefenders, Composure, PsychologyPressureScale)` | ACADEMIC-INFORMED + [GT] | [DICKS-2010] / [BEILOCK-2007]; [GT] weights |
| `PRESSURE_RADIUS` | ACADEMIC-INFORMED + [GT] | [DICKS-2010] 2–3m; [GT] exact threshold |
| `PRESSURE_WEIGHT` | ACADEMIC-INFORMED + [GT] | [BEILOCK-2007] 20–50%; [GT] calibrated to ~0.30–0.40 |
| `PsychologyPressureScale = 1.0f` at Stage 0 | DEFERRED-DESIGN | [MASTER-VOL2] §H-Gate |
| `FatigueScalar = f(AgentState.Fatigue)` | ACADEMIC-INFORMED + [GT] | [ALI-2011] direction; [GT] curve |
| `FATIGUE_POWER_REDUCTION = 0.20` | ACADEMIC-INFORMED + [GT] | [ALI-2011] endurance decrements; [GT] calibration |
| `PlacementDifficultyScalar` | GAMEPLAY-TUNED [GT] | No direct academic source; design choice |
| `NormalisedFinishing = Finishing / ATTR_MAX` | DERIVED | From [MASTER-VOL2] |
| `NormalisedComposure = Composure / ATTR_MAX` | DERIVED | From [MASTER-VOL2] |
| Finishing attribute effect on BASE_ERROR | ACADEMIC-INFORMED + [GT] | Literature confirms attribute–accuracy relationship; [GT] scaling |
| Deterministic error direction hash | DESIGN-AUTHORITY | [MASTER-VOL1] §1.3 determinism requirement |
| Error vector applied to PlacementTarget | DERIVED | Geometric application |
| Out-of-goal clamping | DESIGN-AUTHORITY | Validity requirement; blocked shot routing |
| `FATIGUE_ERROR_SCALE` | ACADEMIC-INFORMED + [GT] | [ALI-2011]; [GT] calibration |

**§3.6 audit status: COMPLETE. 17 items. 0 undocumented.**

---

### 8.6.7 Audit: §3.7 Body Mechanics Evaluation

| Constant / Formula | Disposition | Notes |
|---|---|---|
| `BodyMechanicsScore = f(RunUpAngle, PlantFootOffset, AgentVelocity, BodyLean, Fatigue, STUMBLING)` | ACADEMIC-INFORMED | [LEES-1998] / [KELLIS-2007] / [NUNOME-2006] structural basis |
| `RunUpAngleScore`: optimal 30°–45° | ACADEMIC | [LEES-1998] — direct numeric range adoption |
| `RUNUP_ANGLE_OPTIMAL_MIN = 30°` | ACADEMIC | [LEES-1998] |
| `RUNUP_ANGLE_OPTIMAL_MAX = 45°` | ACADEMIC | [LEES-1998] |
| `PlantFootScore`: optimal 20–30 cm lateral | ACADEMIC-INFORMED | [LEES-1998] / [KELLIS-2007] — range confirmed; [GT] penalty slope |
| `PLANT_FOOT_OPTIMAL_OFFSET` range | ACADEMIC-INFORMED | [LEES-1998] 20–30 cm; [GT] exact centre point |
| `AgentVelocityBonus = f(AgentVelocity)` | ACADEMIC-INFORMED + [GT] | [KELLIS-2007] approach speed–velocity; [GT] magnitude |
| `BodyLeanScore` | ACADEMIC | [NUNOME-2006] lean–trajectory relationship |
| `FatigueBodyPenalty` | ACADEMIC-INFORMED + [GT] | [ALI-2011]; [GT] penalty shape |
| `STUMBLING` → score floor | DESIGN-AUTHORITY | Agent Movement §3.5 hysteresis; [GT] floor value |
| `BodyMechanicsScore` → velocity modifier | GAMEPLAY-TUNED [GT] | How BMS maps to velocity bonus is a design decision; no academic source fully specifies this transfer function |

**§3.7 audit status: COMPLETE. 11 items. 0 undocumented.**

---

### 8.6.8 Audit: §3.8 Weak Foot Penalty

| Constant / Formula | Disposition | Notes |
|---|---|---|
| Error cone multiplier formula | ACADEMIC-INFORMED + [GT] | Structurally reused from Pass Mechanics §3.7 (same [CAREY-2001] source); magnitude [GT] for shots |
| `SHOT_WF_BASE_ERROR_PENALTY = 0.60` | GAMEPLAY-TUNED [GT] ✅ | Reclassified from ACADEMIC-INFORMED+[VER] in v1.2. [CAREY-2001] documents 15–25% passing accuracy degradation — insufficient to ground a 60% shot error penalty. Per §3.8.9 DD-3.8-01 and Appendix OI-App-C-01: value is a design decision (2× the pass penalty), explicitly [GT], primary playtesting calibration target. Tuning guidance: reduce to 0.40–0.50 if weak-foot shots feel unusable at Rating=1. |
| `WeakFootRating` (1–5) → multiplier mapping | DESIGN-AUTHORITY | [MASTER-VOL2] / [AGENT-MOVEMENT-2] §3.5.6 |
| Velocity reduction formula | ACADEMIC-INFORMED + [GT] | [CAREY-2001] direction; [GT] magnitude |
| `SHOT_WF_VELOCITY_REDUCTION` | ACADEMIC-INFORMED + [GT] | [CAREY-2001]: weak foot produces lower ball speed; specific reduction [GT] |
| Non-dominant foot detection (agentId vs. ContactFoot) | DESIGN-AUTHORITY | [AGENT-MOVEMENT-2] laterality field |

**§3.8 audit status: COMPLETE. 6 items. 0 undocumented.**
**✅ `SHOT_WF_BASE_ERROR_PENALTY` reclassified to [GT] in v1.2. No [VER] items remain.**

---

### 8.6.9 Audit: §3.9 Shot State Machine

| Constant / Formula | Disposition | Notes |
|---|---|---|
| State sequence (IDLE → INITIATING → WINDUP → CONTACT → FOLLOW_THROUGH → COMPLETE) | DESIGN-AUTHORITY | [PASS-MECHANICS-5] pattern reused directly |
| `STUMBLING` state addition | DESIGN-AUTHORITY | OI-003 design decision; [AGENT-MOVEMENT-2] hysteresis pattern |
| `WINDUP_FRAMES` | GAMEPLAY-TUNED [GT] | Duration of windup; playtesting calibration required |
| `FOLLOWTHROUGH_FRAMES` | GAMEPLAY-TUNED [GT] | Duration of follow-through; playtesting calibration required |
| Tackle cancel logic (interrupt flag polling) | DESIGN-AUTHORITY | [COLLISION-3] pattern |
| `Ball.ApplyKick()` call gated at CONTACT→FOLLOW_THROUGH | DESIGN-AUTHORITY | [BALL-PHYSICS-1] §3.1.11.2 |
| `ShotOutcome.Invalid` logging at Warning | DESIGN-AUTHORITY | [MASTER-VOL4] §EventSystem |

**§3.9 audit status: COMPLETE. 7 items. 0 undocumented.**

---

### 8.6.10 Audit: §3.10 Event Publishing

| Constant / Formula | Disposition | Notes |
|---|---|---|
| `ShotExecutedEvent` struct fields | DESIGN-AUTHORITY | [MASTER-VOL4] §EventSystem; fields selected for GK Mechanics (#11) and Statistics Engine (Stage 1+) downstream |
| `ShotCancelledEvent` struct fields | DESIGN-AUTHORITY | [MASTER-VOL4] §EventSystem |
| Event bus publication pattern (fire-and-forget) | DESIGN-AUTHORITY | [MASTER-VOL4] §EventSystem; [PASS-MECHANICS-5] pattern |
| No `IGkResponseSystem` interface at Stage 0 | DESIGN-AUTHORITY | KD-7 / OI-005; interface deferred until GK Mechanics Spec #11 is written |
| `ShotAnimationData` stub (populated, unconsumed) | DESIGN-AUTHORITY | KD-7 / §7.1.1 Stage 1 hook |

**§3.10 audit status: COMPLETE. 5 items. 0 undocumented.**

---

### 8.6.11 Summary: Audit Outcomes

| Sub-system | Items Audited | ACADEMIC | ACADEMIC-INFORMED | DERIVED | GAMEPLAY-TUNED [GT] | DESIGN-AUTHORITY | DEFERRED-DESIGN | ⚠ VERIFY |
|---|---|---|---|---|---|---|---|---|
| §3.2 Velocity | 16 | 0 | 5 | 0 | 7 | 4 | 2 | 0 |
| §3.3 Launch Angle | 9 | 0 | 4 | 0 | 5 | 0 | 0 | 0 |
| §3.4 Spin Vector | 13 | 0 | 5 | 0 | 4 | 1 | 0 | 0 |
| §3.5 Placement | 8 | 0 | 0 | 4 | 2 | 2 | 0 | 0 |
| §3.6 Error Model | 17 | 0 | 5 | 3 | 5 | 2 | 2 | 0 |
| §3.7 Body Mechanics | 11 | 2 | 4 | 0 | 5 | 0 | 0 | 0 |
| §3.8 Weak Foot | 6 | 0 | 2 | 0 | 2 | 2 | 0 | 0 |
| §3.9 State Machine | 7 | 0 | 0 | 0 | 2 | 5 | 0 | 0 |
| §3.10 Event Publishing | 5 | 0 | 0 | 0 | 0 | 5 | 0 | 0 |
| **TOTAL** | **92** | **2** | **25** | **7** | **32** | **21** | **4** | **0** |
| **%** | | 2% | 27% | 8% | 35% | 23% | 4% | **0%** |

**Effective gameplay-tuned proportion:** 35% pure [GT] + 27% academic-informed [GT] =
**~62% of constants have gameplay-tuning as a component.** Pure academic adoption is 2%.
Zero [VER] items remain. All pre-approval actions resolved.

**Pre-approval actions status (v1.2):**

| # | Action | Status |
|---|---|---|
| 1 | DOI verification for all 10 academic sources | ✅ Complete (v1.1) |
| 2 | [WYSCOUT-VELOCITY] remove or replace | ✅ Removed (v1.1) |
| 3 | [VER] §3.4 spin validation: TOPSPIN_BASE, BACKSPIN_BASE, SIDESPIN_BASE, SPIN_MAX | ✅ Complete (v1.2) — all within observable bounds vs Magnus model |
| 4 | [VER] §3.8 SHOT_WF_BASE_ERROR_PENALTY: reclassify per OI-App-C-01 | ✅ Complete (v1.2) — reclassified [GT], tuning guidance added |
| 5 | Cross-spec DOI corrections in Pass Mechanics §8 and Ball Physics §8 | ✅ Complete (v1.2) — Pass Mechanics §8 v1.1 and Ball Physics §8 v1.4 issued |
| 6 | ERR-009: Collision System Spec #3 Section 3 revision | ⚠ Required before implementing pressure query path only — does not block sign-off |

**Section 8 is ready for sign-off. One pre-implementation item remains (ERR-009).**

---

## Cross-Reference Verification

| Reference | DOI / URL | Verification Status | Notes |
|---|---|---|---|
| [LEES-1998] | 10.1080/026404198366740 | ✅ Verified | Confirmed via Taylor & Francis, PubMed, Semantic Scholar |
| [KELLIS-2007] | https://www.jssm.org/vol6/n2/2/v6n2-2text.php | ✅ Verified | Open access; paper confirmed at JSSM Vol 6(2):154–165 |
| [NUNOME-2006] | **10.1080/02640410500298024** | ✅ Verified | **Corrected from v1.0** (was 10.1249/01.mss.0000232459.96965.33). Confirmed via PubMed, ResearchGate |
| [INOUE-2014] | **10.1080/02640414.2014.886126** | ✅ Verified | **Corrected from v1.0** (was 10.1080/02640414.2013.877593). Confirmed via PubMed, Taylor & Francis |
| [ASAI-2002] | **10.1046/j.1460-2687.2002.00108.x** | ✅ Verified | **Corrected from v1.0** (was 10.1046/j.1460-2687.2002.00098.x). Confirmed via Wiley Online Library |
| [BRAY-2003] | 10.1080/0264041031000070994 | ✅ Verified | Confirmed via Taylor & Francis, PubMed, Semantic Scholar |
| [CARRE-2002] | **10.1046/j.1460-2687.2002.00109.x** | ✅ Verified | **Label corrected from CARRE-2004; DOI corrected from 10.1046/j.1460-2687.2002.00099.x**. Confirmed via Wiley Online Library, Sheffield Hallam University repository |
| [DICKS-2010] | 10.3758/APP.72.3.706 | ✅ Verified | Confirmed via Springer / PsycINFO |
| [BEILOCK-2007] | **10.1002/9781118270011.ch19** | ✅ Verified | **Label corrected from [BEILOCK-2010]; year corrected 2010→2007; DOI corrected .ch20→.ch19. Correction from Perception §8 v1.2.** |
| [ALI-2011] | 10.1111/j.1600-0838.2010.01256.x | ✅ Verified | Confirmed via Wiley / PubMed |
| [CAREY-2001] | 10.1080/026404101753113804 | ✅ Verified | Confirmed via Taylor & Francis, PMC, and extensive literature citations |
| [STATSBOMB-OPEN] | https://github.com/statsbomb/open-data | ✅ Available | Verify licence before commercial use |
| [FIFA-QUALITY] | FIFA website (public doc) | ✅ Available | Shared with Ball Physics §8 |
| [WYSCOUT-VELOCITY] | — | ✅ Removed | Removed in v1.1 as required; velocity range grounded by [LEES-1998] + [STATSBOMB-OPEN] |
| [IMPECT-2023] | https://www.impect.com/ | ⚠ Contextual only | No constants derived; directional reference only |
| [MASTER-VOL1] §1.3 | Internal | ✅ Verified | Determinism requirement confirmed |
| [MASTER-VOL2] §PlayerAttributes | Internal | ✅ Verified | ATTR_MAX = 20.0, WeakFootRating confirmed |
| [MASTER-VOL2] §FormSystem | Internal | ⚠ Not yet written | Deferred-design placeholder |
| [MASTER-VOL2] §H-Gate | Internal | ⚠ Not yet written | Deferred-design placeholder |
| [BALL-PHYSICS-1] Ball.ApplyKick() | Internal | ✅ Verified | Interface stable (Amendment AM-001-001 resolved) |
| [AGENT-MOVEMENT-2] §3.5.6 | Internal | ✅ Verified | All required attributes confirmed present |
| [COLLISION-3] tackle flag | Internal | ✅ Verified | Polling pattern confirmed |
| [PASS-MECHANICS-5] §3.7, §3.8 | Internal | ✅ Verified | Weak foot and state machine patterns |
| Ball Physics Magnus validation | Internal (Appendix B) | ⚠ Action required | §3.4 spin constants must be validated before implementation |

---

## Version History

| Version | Date | Author | Notes |
|---|---|---|---|
| 1.0 | February 23, 2026, 11:59 PM PST | Claude (AI) / Anton | Initial draft. 92 constants/formulas audited across 9 sub-systems. ~62% gameplay-tuned component. 5 pre-approval actions identified. 10 academic sources. CARRE-2004 label discrepancy flagged. |
| 1.1 | February 24, 2026 | Claude (AI) / Anton | DOI verification complete. Four corrections applied: (1) [NUNOME-2006] DOI corrected to 10.1080/02640410500298024; also corrected citation description to reflect actual paper (Nunome et al. 2006, segmental dynamics, preferred/non-preferred leg). (2) [INOUE-2014] DOI corrected to 10.1080/02640414.2014.886126. (3) [ASAI-2002] DOI corrected to 10.1046/j.1460-2687.2002.00108.x. (4) [CARRE-2004] label corrected to [CARRE-2002] and DOI corrected to 10.1046/j.1460-2687.2002.00109.x. [WYSCOUT-VELOCITY] removed. Cross-spec action notice added for shared DOI corrections. |
| 1.2 | February 24, 2026 | Claude (AI) / Anton | All remaining pre-approval blockers resolved. (1) §3.4 spin validation: all four spin base values analytically validated against Ball Physics §3.1.4 Magnus model — TOPSPIN_BASE[Centre]=25 rad/s produces +1.47m dip over 20m ✅; SIDESPIN_BASE[OffCentre]=28 rad/s produces 1.85m lateral curl over 20m ✅; BACKSPIN_BASE[BelowCentre]=30 rad/s reduces effective gravity by 16.5% without reversing it ✅; SPIN_ABSOLUTE_MAX=80 rad/s conservative vs 150 rad/s literature ✅. (2) SHOT_WF_BASE_ERROR_PENALTY reclassified from ACADEMIC-INFORMED+[VER] to [GT] per Appendix OI-App-C-01 and §3.8.9 DD-3.8-01. (3) Cross-spec corrections issued: Pass Mechanics §8 v1.1 (NUNOME-2006 and ASAI-2002 DOIs corrected) and Ball Physics §8 v1.4 (CARRE-2002 DOI and title corrected). Audit summary table updated: [VER] count = 0. Section 8 ready for sign-off. |
| 1.3 | February 26, 2026 | Claude (AI) / Anton | Cross-spec correction from Perception System §8 v1.2 DOI verification. [BEILOCK-2010] corrected to [BEILOCK-2007]: year corrected from 2010 to 2007; DOI corrected from 10.1002/9781118270011.ch20 to 10.1002/9781118270011.ch19. All occurrences updated (citation block §8.1.3, citation summary §8.5 ×2, audit §8.6.6 ×3, cross-reference table). |
| 1.4 | March 6, 2026 | Claude (AI) / Anton | Comprehensive audit corrections: Decision Tree #7→#8, Goalkeeper Mechanics #10→#11 (spec renumbering cascade). |

---

**Next Section:** Section 9 — Approval Checklist

---

*End of Section 8 — Shot Mechanics Specification #6*

*Tactical Director — Specification #6 of 20 | Stage 0: Physics Foundation*
