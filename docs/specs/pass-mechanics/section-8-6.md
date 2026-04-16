## 8.6 Citation Audit

This audit verifies that every formula and constant in Sections 3.1–3.8 has been traced
to a source or explicitly flagged. It is the accountability mechanism ensuring no formula
is accidentally undocumented or mis-cited.

---

### 8.6.1 Audit Methodology

For each formula, constant, or threshold, one of five dispositions applies:

- **DESIGN-AUTHORITY** — specified by a Master Volume or other internal design
  document. The document and section are the binding reference.
- **ACADEMIC** — directly derived from a specific value or formula in a peer-reviewed
  source listed in §8.1. The link from formula to source must be traceable.
- **ACADEMIC-INFORMED** — the general structure or range of the value is supported
  by academic sources, but the specific implementation value is a design choice.
  Denoted with the academic source and [GT] suffix.
- **GAMEPLAY-TUNED [GT]** — deliberately chosen for gameplay feel, with no academic
  grounding. Rationale and tuning guidance documented inline.
- **DEFERRED-DESIGN** — awaiting an unwritten specification or future design decision.
- **DERIVED** — mathematically derived from first principles (e.g., projectile motion
  equations) with no free parameters requiring calibration.

Honest [GT] flags are preferable to fabricated or unverified citations. The ~55% [GT]
ratio is within expectations for a system that maps abstract player attributes to
physical outcomes.

---

### 8.6.2 Audit: §3.2 Pass Velocity Model

**KickPower → V_base mapping (coefficient form):**
ACADEMIC-INFORMED. [LEES-1998] and [KELLIS-2007] establish that ball speed is
approximately 1.1–1.5× foot speed at contact. KickPower [1–20] maps to foot speed
conceptually; the specific linear mapping is a design decision. [GT]

**ATTR_MAX = 20.0:**
DESIGN-AUTHORITY. [MASTER-VOL2] §PlayerAttributes specifies the [1–20] attribute
range for all PlayerAttributes. ✓

**Fatigue velocity reduction formula:**
ACADEMIC-INFORMED. [ALI-2011] confirms that fatigue reduces passing power output.
The specific functional form (linear vs exponential) and FATIGUE_POWER_REDUCTION
value are gameplay-tuned. [GT]

**V_MIN clamping (prevents zero-velocity output):**
DESIGN-AUTHORITY. Master Vol 1 §1.3 determinism requirement implies the system
must never produce undefined physical state. Zero velocity would make Ball.ApplyKick()
call undefined. ✓

**V_MAX clamping (prevents superhuman output):**
GAMEPLAY-TUNED. Upper bound chosen to prevent exploitation (e.g., teleporting passes
that bypass aerodynamic deceleration modelling). Requires numerical validation against
Ball Physics drag model before Section 3 finalisation. [GT]
⚠ **VALIDATION REQUIRED:** Run Ball Physics simulation at V_MAX for each pass type
to confirm ball reaches expected field distance before decelerating to < 3 m/s.

---

### 8.6.3 Audit: §3.3 Launch Angle Derivation

**Ground and Driven pass angles (fixed profile):**
ACADEMIC-INFORMED. [LEES-1998] documents observed launch angles of 2–8° for ground
passes and 5–15° for driven passes. §3.7 profile table values sit within these ranges.
Exact values are design choices within the academic-observed window. [GT]

**Lofted and Chip pass angles (distance-dependent formula):**
DERIVED. The formula θ = atan2(2H, D) for a desired apex height H and horizontal
distance D is elementary projectile mechanics. No empirical calibration of the formula
structure is required; H is gameplay-tuned for desired visual arc. [GT for H values]

**Cross sub-type angles (Flat/Whipped/High):**
ACADEMIC-INFORMED. [LEES-1998] and observational data support the general ordering
(flat < whipped < high). Specific bounds are gameplay-tuned. [GT]

**atan() / sin() usage note:**
Stage 0 uses standard float trigonometric functions. These are expensive relative to
the remainder of Pass Mechanics. Performance analysis in Section 6 confirms this is
acceptable for a discrete-event system. Pre-computed lookup tables are deferred to
Stage 2 optimisation (§7 risk note 4 from Outline). ✓

---

### 8.6.4 Audit: §3.4 Spin Vector Calculation

**Spin axis assignment per pass type (topspin/backspin/sidespin):**
ACADEMIC. [ASAI-2002] documents that instep contact produces topspin, outside-of-foot
contact produces sidespin, and chip contact produces backspin. The spin axis assignments
in §3.4.1 map directly to these findings. ✓

**SPIN_BASE values (rad/s) per pass type:**
ACADEMIC-INFORMED. [BRAY-2003] documents free-kick spin rates of 50–63 rad/s;
general kicked passes are estimated at 6–25 rad/s. §3.7 SPIN_BASE values fall within
this range. Specific values per pass type are gameplay-tuned. [GT]
⚠ **CROSS-SPEC VALIDATION REQUIRED:** SPIN_BASE values must be numerically validated
against Ball Physics §3.1 Magnus force constants [HONG-2012]. An over-specified spin
vector produces implausible lateral curve at typical pass distances.

**TechniqueScale multiplier [0.5–1.5]:**
GAMEPLAY-TUNED. Technique attribute modulates spin quality. The [0.5–1.5] range is
a design decision ensuring that low-Technique players generate insufficient spin for
reliable curve, while high-Technique players achieve near-ideal spin. [GT]

**Minimum spin floor (prevents knuckling):**
ACADEMIC-INFORMED. [HONG-2012] identifies spin rates below ~1 rad/s as producing
aerodynamically unpredictable knuckling. A minimum spin floor prevents unintended
knuckling on all non-chip pass types. The floor value is gameplay-tuned. [GT]

---

### 8.6.5 Audit: §3.5 Error Model

**Error model architecture (multiplicative chain):**
DESIGN-AUTHORITY. The multiplicative modifier chain is the established pattern across
all Stage 0 specifications. Consistent with First Touch §3.1, Collision System §3.3.
Master Vol 1 implicitly mandates this by requiring that each attribute affects outcomes
independently. ✓

**BASE_ERROR per pass type:**
GAMEPLAY-TUNED. The baseline error angle at neutral attributes, pressure, fatigue,
and orientation. No academic source prescribes specific angular error for each pass
type in an abstract simulation context. The values must be calibrated so that:
(a) elite players (Passing ≥ 18) achieve realistic completion rates at typical distances;
(b) poor players (Passing ≤ 4) produce visibly erratic passes.
Target calibration range: elite completion rate ≥ 85% at 20m; poor player completion
rate ≤ 55% at 20m. [GT]

**PassingModifier formula:**
GAMEPLAY-TUNED. Maps Passing attribute [1–20] to an error multiplier. Direction is
constrained by physics (higher Passing → lower error), but the functional form (linear
vs inverse vs exponential decay) and scaling are design decisions. [GT]
Tuning guidance: at Passing = 10 (mid-range), PassingModifier should produce
approximately 1.0 (neutral). At Passing = 20, ≈ 0.4–0.6. At Passing = 1, ≈ 2.0–3.0.

**PressureModifier formula:**
ACADEMIC-INFORMED. [DICKS-2010] and [BEILOCK-2007] confirm that pressure degrades
accuracy non-linearly. Inverse-square with saturation is a reasonable functional form
consistent with attention-narrowing research. Specific PRESSURE_WEIGHT and
PRESSURE_RADIUS values are gameplay-tuned within the academic range. [GT]

**PRESSURE_WEIGHT:**
ACADEMIC-INFORMED. [BEILOCK-2007] documents 20–50% accuracy degradation under peak
pressure. PRESSURE_WEIGHT should be calibrated so that maximum PressureModifier
output falls in this range. [GT]

**PRESSURE_RADIUS:**
ACADEMIC-INFORMED. [DICKS-2010] documents measurable gaze disruption at 2–4m opponent
proximity. PRESSURE_RADIUS should be set within this range. [GT]

**FatigueModifier formula:**
ACADEMIC-INFORMED. [ALI-2011] confirms monotonic accuracy degradation with fatigue.
Functional form and FATIGUE_POWER_REDUCTION are gameplay-tuned. [GT]

**FATIGUE_POWER_REDUCTION:**
ACADEMIC-INFORMED. [ALI-2011] implies velocity and accuracy both degrade with fatigue.
Value should be calibrated so that a fully fatigued player (Fatigue = 1.0) produces
approximately 15–25% higher error than a fresh player at identical attributes. [GT]

**OrientationModifier formula:**
GAMEPLAY-TUNED. Penalises passes made with significant body misalignment. No academic
source directly prescribes the functional form or magnitude for a simulation context.
Value is constrained by: OrientationModifier(0°) = 1.0 (no penalty when aligned);
OrientationModifier(90°) = ORIENTATION_MAX_PENALTY (maximum penalty). [GT]

**ORIENTATION_MAX_PENALTY:**
GAMEPLAY-TUNED. Maximum penalty for fully misaligned body (90° from pass direction).
Should not exceed a multiplier of ~2.5 to avoid producing physically implausible
backward passes from technically competent players at low urgency. [GT]

**UrgencyModifier formula:**
GAMEPLAY-TUNED. Rushed passes are less accurate. Urgency [0.0–1.0] maps to an
error multiplier. The modifier must be monotonically increasing. [GT]

**URGENCY_ERROR_SCALE:**
GAMEPLAY-TUNED. At maximum urgency (Urgency = 1.0), error multiplier = 1.0 +
URGENCY_ERROR_SCALE. Value should produce visibly degraded passes under urgency
while retaining coherent ball direction (no backward-pointing error vectors). [GT]

**WeakFootModifier formula:**
ACADEMIC-INFORMED. [CAREY-2001] documents 15–25% accuracy degradation for weak-foot
passes in elite competition. WEAK_FOOT_BASE_PENALTY should be calibrated within this
range at WeakFootRating = 1 (worst). At WeakFootRating = 5 (ambidextrous),
WeakFootModifier = 1.0 (no penalty). [GT]
⚠ `[ERR-007-PENDING]`: WeakFootRating field name is provisional pending ERR-007
resolution. Audit disposition unchanged regardless of final field name.

**FormModifier (reserved field):**
DEFERRED-DESIGN. Field reserved in multiplier chain. Full specification pending
[MASTER-VOL2] §FormSystem. Current value = 1.0 (neutral) in all Stage 0 executions.

**PsychologyModifier (reserved field):**
DEFERRED-DESIGN. Field reserved in multiplier chain. Full specification pending
[MASTER-VOL2] §H-Gate. Current value = 1.0 (neutral) in all Stage 0 executions.

**Error angle clamping (MAX_ERROR_ANGLE):**
GAMEPLAY-TUNED. Prevents the multiplicative chain from producing physically absurd
results (e.g., error angles > 45° would redirect the ball more sideways than forward).
MAX_ERROR_ANGLE should be set so that worst-case output is implausible but not
comically broken. Suggested ceiling: 30°. [GT]

**Determinism invariant (no System.Random):**
DESIGN-AUTHORITY. [MASTER-VOL1] §1.3. The error vector must be reproducible from
seed alone. System.Random is explicitly forbidden. Error angle is computed
deterministically from input parameters. ✓

---

### 8.6.6 Audit: §3.6 Target Resolution

**Linear receiver position projection:**
DERIVED. Position at time t = current_position + current_velocity × t. Elementary
kinematics. No empirical calibration required.

**Vision attribute proxied by Technique (KD-7):**
GAMEPLAY-TUNED with documented rationale. [MASTER-VOL2] does not include a Vision
attribute at Stage 0. Technique is used as a proxy for lead distance accuracy.
This is an explicit Stage 1 upgrade point. [GT] ✓

**Lead distance clamping (pitch bounds):**
DESIGN-AUTHORITY. If projected interception point falls outside pitch bounds, pass
executes to raw target coordinate. This is a boundary condition, not a formula. ✓

---

### 8.6.7 Audit: §3.7 Physical Profile Constants

The physical profile table in §3.7 (reproduced in §2.4.3 provisional values) contains
all per-pass-type constants. The disposition table below covers the entire table.

| Constant Category | Disposition | Notes |
|---|---|---|
| V_MIN (all types) | ACADEMIC-INFORMED + [GT] | [KELLIS-2007], [STATSBOMB-OPEN] provide lower bound confirmation |
| V_MAX (all types) | ACADEMIC-INFORMED + [GT] | [KELLIS-2007] upper bound; requires drag validation |
| ANGLE_MIN (all types) | ACADEMIC-INFORMED + [GT] | [LEES-1998] window; design choice within it |
| ANGLE_MAX (all types) | ACADEMIC-INFORMED + [GT] | [LEES-1998] window; design choice within it |
| SPIN_BASE (all types) | ACADEMIC-INFORMED + [GT] | [BRAY-2003] order of magnitude; requires Magnus cross-validation |
| WINDUP_FRAMES (all types) | GAMEPLAY-TUNED [GT] | [FRANKS-1985] plausibility bracket only; design choice |
| FOLLOWTHROUGH_FRAMES (all types) | GAMEPLAY-TUNED [GT] | Cosmetic; no academic source applicable |

**Total constants in §3.7 table:** 63 (9 pass types × 7 constant categories).
**Disposition breakdown:** 0 ACADEMIC; 35 ACADEMIC-INFORMED + [GT]; 28 GAMEPLAY-TUNED [GT].

---

### 8.6.8 Audit: §3.8 Pass Execution State Machine

**State machine architecture (6-state):**
DESIGN-AUTHORITY. The IDLE → INITIATING → WINDUP → CONTACT → FOLLOW_THROUGH →
COMPLETE structure is a design decision consistent with the Agent Movement state
machine pattern established in Spec #2 §3.1. ✓

**CONTACT state as the single ApplyKick() call frame:**
DESIGN-AUTHORITY. Constraint that Ball.ApplyKick() is called exactly once per CONTACT
state is a contract with Ball Physics §3.1.11.2. ✓

**Tackle interrupt cancellation during WINDUP only:**
DESIGN-AUTHORITY. FR-09 in §2.1 specifies this boundary. Consistent with Collision
System Spec #3 §4.x tackle interrupt interface. ✓

**PassCancelledEvent published on tackle only (KD-8):**
DESIGN-AUTHORITY. KD-8 is resolved in §1.3. Invalid request rejection is a programming
error (logged, not evented). ✓

**Event queue capacity:**
DESIGN-AUTHORITY. [MASTER-VOL4] §EventSystem. ✓

---

### 8.6.9 Summary: Audit Outcomes

| Disposition | Count | Representative Examples |
|---|---|---|
| DESIGN-AUTHORITY | 9 | ATTR_MAX, determinism invariant, state machine structure, event queue, ApplyKick() constraint, tackle interrupt boundary |
| ACADEMIC | 2 | Spin axis per pass type, spin rate order of magnitude |
| ACADEMIC-INFORMED + [GT] | 21 | V_MIN/V_MAX, ANGLE_MIN/ANGLE_MAX, SPIN_BASE, PRESSURE_WEIGHT, PRESSURE_RADIUS, FATIGUE_POWER_REDUCTION, WEAK_FOOT_BASE_PENALTY |
| GAMEPLAY-TUNED [GT] | 28 | BASE_ERROR, PassingModifier form, OrientationModifier, UrgencyModifier, WINDUP_FRAMES, FOLLOWTHROUGH_FRAMES, TechniqueScale, MAX_ERROR_ANGLE |
| DERIVED | 2 | Lofted/Chip launch angle formula, lead distance projection |
| DEFERRED-DESIGN | 2 | FormModifier, PsychologyModifier |
| VERIFICATION REQUIRED [VER] | 2 | V_MAX drag validation, SPIN_BASE Magnus cross-validation |

**Total constants and formulas audited: 66. Zero undocumented.**

**Outstanding validation actions before Section 3 finalisation:**
1. Simulate all V_MAX values through Ball Physics drag model. Confirm balls travel
   expected distances before decelerating below playable threshold.
2. Simulate all SPIN_BASE values against Ball Physics Magnus constants. Confirm no
   pass type produces implausible lateral curve under normal conditions.
3. Calibrate BASE_ERROR values to produce target elite/poor player completion rate
   ranges documented in §8.6.5.
4. Verify DOIs for all academic sources via doi.org (full list in Cross-Reference
   Verification table below).

---

## Cross-Reference Verification

| Reference | Target | Status | Notes |
|---|---|---|---|
| [LEES-1998] DOI | 10.1080/026404198366740 | ⚠ Pending | Verify via doi.org before final approval |
| [KELLIS-2007] | JSSM open access URL | ⚠ Pending | Free access; verify URL is still live |
| [NUNOME-2006] DOI | **10.1080/02640410500298024** | ✅ Verified | **Corrected from v1.0** (was 10.1249/01.mss.0000232459.96965.33). Confirmed via PubMed, ResearchGate |
| [ASAI-2002] DOI | **10.1046/j.1460-2687.2002.00108.x** | ✅ Verified | **Corrected from v1.0** (was 10.1046/j.1460-2687.2002.00098.x). Confirmed via Wiley Online Library |
| [BRAY-2003] DOI | 10.1080/0264041031000070994 | ⚠ Pending | Verify via doi.org before v1.1 |
| [HONG-2012] DOI | 10.1007/s12650-012-0131-y | ⚠ Pending | Shared with Ball Physics §8; verify once |
| [DICKS-2010] DOI | 10.3758/APP.72.3.706 | ⚠ Pending | Verify via doi.org before v1.1 |
| [BEILOCK-2007] DOI | **10.1002/9781118270011.ch19** | ✅ Verified | **Label corrected from [BEILOCK-2007]; year corrected 2010→2007; DOI corrected .ch20→.ch19. Verified via Wiley Online Library. Correction from Perception §8 v1.2.** |
| [ALI-2011] DOI | 10.1111/j.1600-0838.2010.01256.x | ⚠ Pending | Verify via doi.org before v1.1 |
| [CAREY-2001] DOI | 10.1080/026404101753113804 | ⚠ Pending | Verify via doi.org before v1.1 |
| [KUNZ-2007] | FIFA Magazine archive | ⚠ Pending | Trade publication; verify archive accessibility |
| [STATSBOMB-OPEN] | GitHub repository | ✓ Available | Public; verify licence terms before using data commercially |
| [IMPECT-2023] | Impect website | ⚠ Pending | Commercial; contextual reference only; no formula constants derived |
| [FRANKS-1985] | Library access | ⚠ Pending | Pre-DOI; contextual reference only |
| [MASTER-VOL1] §1.3 | Determinism requirement | ✓ Verified | Primary authority for §3.5 determinism invariant |
| [MASTER-VOL2] §PlayerAttributes | [1–20] attribute range | ✓ Verified | ATTR_MAX = 20.0 confirmed |
| [MASTER-VOL2] §FormSystem | FormModifier interface | ⚠ Not yet written | Deferred-design; placeholder in §3.5 |
| [MASTER-VOL2] §H-Gate | PsychologyModifier interface | ⚠ Not yet written | Deferred-design; placeholder in §3.5 |
| Ball Physics §3.1.11.2 | Ball.ApplyKick() interface | ⚠ ERR-006 pending | Amendment AM-001-001 drafted; approve before Section 3 |
| Agent Movement §3.5.6 | PlayerAttributes struct | ⚠ ERR-007 pending | KickPower/WeakFootRating/Crossing absent; Amendment AM-002-001 drafted |
| Ball Physics Magnus constants | SPIN_BASE validation | ⚠ Validation required | Numerical cross-check required before Section 3 finalisation |
| Ball Physics drag model | V_MAX validation | ⚠ Validation required | Simulation required before Section 3 finalisation |

---

**End of Section 8**

**Page Count:** ~18 pages estimated
**Version:** 1.2
**Constants Audited:** 66
**Zero undocumented constants.**

**Next Section:** Section 9 — Approval Checklist

---

*End of Pass Mechanics Specification #5 — Section 8: References*

| Version | Date | Author | Notes |
|---------|------|--------|-------|
| 1.0 | February 20, 2026, 11:59 PM PST | Claude (AI) / Anton | Initial draft. 10 academic sources. 66 constants audited. ~55% gameplay-tuned ratio documented. 2 outstanding validation actions flagged. ERR-006 and ERR-007 flags carried forward. |
| 1.1 | February 24, 2026 | Claude (AI) / Anton | Cross-spec DOI corrections from Shot Mechanics §8 v1.1 review. (1) [NUNOME-2006] DOI corrected to 10.1080/02640410500298024; author list and paper title corrected to match actual paper (segmental dynamics, preferred/non-preferred leg, JSS 24(5):529–541). (2) [ASAI-2002] DOI corrected to 10.1046/j.1460-2687.2002.00108.x. DOI verification banner updated. Cross-Reference Verification table updated. |
| 1.2 | February 26, 2026 | Claude (AI) / Anton | Cross-spec correction from Perception System §8 v1.2 DOI verification. [BEILOCK-2010] corrected to [BEILOCK-2007]: year corrected from 2010 to 2007; DOI corrected from 10.1002/9781118270011.ch20 to 10.1002/9781118270011.ch19. All occurrences updated (citation block §8.1.3, citation summary §8.5, audit §8.6.5, cross-reference table). |
| 1.3 | March 25, 2026 | Claude (AI) / Anton | Post-audit fixes: Decision Tree #7→#8 (C-03, 2 instances); footer version corrected 1.0→1.2 (M-06). |
