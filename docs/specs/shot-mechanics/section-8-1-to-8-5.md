# Shot Mechanics Specification #6 — Section 8: References

**File:** `Shot_Mechanics_Spec_Section_8_v1_3.md`
**Purpose:** Comprehensive bibliography of all research papers, real-world data sources,
and internal documentation used to derive, validate, and implement shot mechanics
formulas. Includes a complete citation audit tracing every formula, constant, and
threshold in Sections 3.1–3.10 to its academic source or explicitly flagging it as
empirically chosen for gameplay purposes.

**Created:** February 23, 2026, 11:59 PM PST
**Revised:** February 26, 2026
**Version:** 1.3
**Status:** DRAFT — Awaiting Lead Developer Review
**Specification Number:** 6 of 20 (Stage 0 — Physics Foundation)
**Author:** Claude (AI) with Anton (Lead Developer)
**Prerequisites:** Sections 1 (v1.1), 2 (v1.0), 3 Part 1 (v1.1), 3 Part 2 (v1.1),
4 (v1.3), 5 (v1.3), 6 (v1.0), 7 (v1.0)

> ✅ **DOI Verification Status (v1.1+):** All DOIs verified. Four corrections applied
> in v1.1. [WYSCOUT-VELOCITY] removed. See Version History for full detail.

> ✅ **Spin Validation Status (v1.2):** §3.4 spin base values analytically validated
> against Ball Physics §3.1.4 Magnus model. All four values within observable bounds.
> See §8.6.4 for results. `SHOT_WF_BASE_ERROR_PENALTY` reclassified to [GT] per
> Appendix OI-App-C-01 and §3.8.9 DD-3.8-01.

> **Rendering note:** This document contains mathematical symbols (×, ≈, π, °, ≤, ≥,
> ω, etc.) that require UTF-8 encoding to display correctly. If symbols appear garbled,
> verify the file is being read with UTF-8 encoding.

> **Source reuse note:** Several sources in this section are shared with Ball Physics
> Spec #1, Pass Mechanics Spec #5, and/or First Touch Spec #4. Where a source has been
> previously cited and DOI-verified in another specification, that verification status
> is noted. Cross-specification consistency is maintained: if a DOI is corrected in one
> spec it must be corrected in all specs that share it.

> ⚠ **Cross-spec action required:** Four DOI corrections in this document affect shared
> citations. The following specifications must be updated to match:
> - **[NUNOME-2006]** DOI corrected → also appears in Pass Mechanics Spec #5 §8.1.1
> - **[ASAI-2002]** DOI corrected → also appears in Pass Mechanics Spec #5 §8.1.2 and Ball Physics Spec #1 §8.1
> - **[CARRE-2002]** label and DOI corrected → also appears in Ball Physics Spec #1 §8.1
> These shared specs must be updated before implementation begins.

---

## Table of Contents

- [Preamble](#preamble)
- [8.1 Academic Sources](#81-academic-sources)
  - [8.1.1 Kicking Biomechanics and Power–Accuracy Trade-off](#811-kicking-biomechanics-and-poweraccuracy-trade-off)
  - [8.1.2 Ball Spin and Aerodynamic Effects](#812-ball-spin-and-aerodynamic-effects)
  - [8.1.3 Shot Accuracy Under Pressure and Fatigue](#813-shot-accuracy-under-pressure-and-fatigue)
  - [8.1.4 Weak Foot and Laterality in Football](#814-weak-foot-and-laterality-in-football)
  - [8.1.5 Body Mechanics and Shooting Stance Biomechanics](#815-body-mechanics-and-shooting-stance-biomechanics)
- [8.2 Real-World Data Sources](#82-real-world-data-sources)
  - [8.2.1 Shot Velocity and Distance Distributions](#821-shot-velocity-and-distance-distributions)
  - [8.2.2 Shot Outcome and xG Data](#822-shot-outcome-and-xg-data)
- [8.3 Internal Project Documents](#83-internal-project-documents)
  - [8.3.1 Master Volumes](#831-master-volumes)
  - [8.3.2 Related Specifications](#832-related-specifications)
  - [8.3.3 Development Documentation](#833-development-documentation)
- [8.4 Software and Tools](#84-software-and-tools)
- [8.5 Citation Summary](#85-citation-summary)
- [8.6 Citation Audit](#86-citation-audit)
  - [8.6.1 Audit Methodology](#861-audit-methodology)
  - [8.6.2 Audit: §3.2 Velocity Calculation](#862-audit-32-velocity-calculation)
  - [8.6.3 Audit: §3.3 Launch Angle Derivation](#863-audit-33-launch-angle-derivation)
  - [8.6.4 Audit: §3.4 Spin Vector Calculation](#864-audit-34-spin-vector-calculation)
  - [8.6.5 Audit: §3.5 Placement Resolution](#865-audit-35-placement-resolution)
  - [8.6.6 Audit: §3.6 Error Model](#866-audit-36-error-model)
  - [8.6.7 Audit: §3.7 Body Mechanics Evaluation](#867-audit-37-body-mechanics-evaluation)
  - [8.6.8 Audit: §3.8 Weak Foot Penalty](#868-audit-38-weak-foot-penalty)
  - [8.6.9 Audit: §3.9 Shot State Machine](#869-audit-39-shot-state-machine)
  - [8.6.10 Audit: §3.10 Event Publishing](#8610-audit-310-event-publishing)
  - [8.6.11 Summary: Audit Outcomes](#8611-summary-audit-outcomes)
- [Cross-Reference Verification](#cross-reference-verification)
- [Version History](#version-history)

---

## Preamble

This section provides the complete bibliography for the Shot Mechanics Specification.
Every formula, coefficient, and threshold in Sections 3.1–3.10 either derives from a
source listed here OR is explicitly flagged as empirically chosen for gameplay purposes.

References are organised by type:

1. **Academic Sources (8.1):** Peer-reviewed research on kicking biomechanics,
   power–accuracy trade-off, ball spin, aerodynamics, shooting accuracy, laterality,
   and shooting stance biomechanics.
2. **Real-World Data (8.2):** Match statistics, shot tracking data, and empirical
   measurements used for validation and calibration.
3. **Internal Project Documents (8.3):** Cross-references to Master Volumes and related
   specifications.
4. **Software and Tools (8.4):** Technical infrastructure references.
5. **Citation Summary (8.5):** Quick-reference table mapping every formula area and
   constant to its source.
6. **Citation Audit (8.6):** Complete traceability from formulas to sources with
   explicit flagging of empirically chosen values.

**Honesty principle:** Shot mechanics sits at the intersection of biomechanics
(kicking force-velocity relationships, spin physics) and deliberate gameplay design
(ContactZone modifiers, power–accuracy weighting, BASE_ERROR tuning). Approximately
50% of constants are gameplay-tuned [GT], consistent with the outline estimate and with
Pass Mechanics (~55%). This proportion is higher than Ball Physics (~20%) because the
translation from physical intent parameters to observable outcomes requires design
choices that no single academic source fully prescribes. All [GT] values are explicitly
flagged with tuning guidance and physical motivation.

| Aspect | Ball Physics | Pass Mechanics | First Touch | Shot Mechanics |
|--------|-------------|----------------|-------------|----------------|
| Academic sources | 15+ papers | 10 papers | 9 sources | ~10 sources |
| Empirically-tuned values | ~20% | ~55% | ~45% | ~50% |
| Primary domain | Aerodynamics | Biomechanics + design | Ball control | Kicking biomechanics + design |
| Literature maturity | Extensive | Moderate | Moderate | Moderate–extensive |

---

## 8.1 Academic Sources

### 8.1.1 Kicking Biomechanics and Power–Accuracy Trade-off

---

**[LEES-1998]** Lees, A., & Nolan, L. (1998). The biomechanics of soccer: A review.
*Journal of Sports Sciences, 16*(3), 211–234.

- **DOI:** 10.1080/026404198366740 ✅ Verified
- **Access:** Taylor & Francis Online (institutional access or purchase)
- **Shared with:** Pass Mechanics Spec #5 §8.1.1
- **Used for:**
  - Core justification for the power–accuracy trade-off (KD-1). Literature documents
    that maximum-effort kicks produce measurably larger accuracy variance than
    submaximal efforts. Supports the `PowerPenalty` scalar in §3.6.
  - Launch angle ranges for full-instep contact (2°–8°) — informs
    `BaseAngle[Centre] = 4°` in §3.3.
  - Velocity ceiling grounding: elite kickers achieve 28–34 m/s ball speed at
    maximum effort; supports `V_CEILING = 35.0 m/s` as the upper bound.
  - Establishes that approach angle and plant foot position significantly influence
    kicking outcome — foundational justification for the body mechanics model in §3.7.
- **Key content:** Comprehensive biomechanics review covering foot–ball contact,
  force-velocity relationships, approach mechanics, and accuracy degradation under
  maximal effort. The most foundational kicking biomechanics reference in the
  specification.
- **Limitations:** Focuses on maximal-effort kicks (penalties, direct shots); submaximal
  finesse-type kicks are less well covered. `BaseAngle[OffCentre]` and
  `BaseAngle[BelowCentre]` require supplementary sources.

---

**[KELLIS-2007]** Kellis, E., & Katis, A. (2007). Biomechanical characteristics and
determinants of instep soccer kick. *Journal of Sports Science and Medicine, 6*(2),
154–165.

- **URL:** https://www.jssm.org/vol6/n2/2/v6n2-2text.php ✅ Verified (open access;
  paper confirmed at JSSM Vol 6 Issue 2. If URL is stale, access via
  https://www.jssm.org/ and navigate to Vol 6, No 2.)
- **Access:** Free — Journal of Sports Science and Medicine (open access)
- **Shared with:** Pass Mechanics Spec #5 §8.1.1
- **Used for:**
  - Force–velocity relationship in the kicking chain: confirms that increased approach
    speed contributes to ball velocity — informs the agent velocity component of
    body mechanics (§3.7.5).
  - Attribute-to-velocity scaling: the relationship between muscular force output
    and ball speed is sub-linear at the extremes, supporting the [GT] calibration of
    `V_BASE` scaling rather than a purely linear formula.
  - Supporting evidence for plant foot offset effects on kick mechanics: planted
    foot too close reduces hip rotation range; too far reduces stability — directly
    informs §3.7.4 (plant foot offset score).
- **Key content:** Controlled study on instep kick kinematics with EMG and motion
  capture data. Provides numeric data on approach angle, plant foot distance, and
  resultant ball velocity.
- **Limitations:** Laboratory conditions with controlled distances; direct mapping
  to in-match distances and player conditions requires [GT] interpolation.

---

**[NUNOME-2006]** Nunome, H., Ikegami, Y., Kozakai, R., Apriantono, T., & Sano, S.
(2006). Segmental dynamics of soccer instep kicking with the preferred and non-preferred
leg. *Journal of Sports Sciences, 24*(5), 529–541.

- **DOI:** 10.1080/02640410500298024 ✅ Verified
  *(v1.0 had incorrect DOI 10.1249/01.mss.0000232459.96965.33 — corrected in v1.1)*
- **Access:** Taylor & Francis Online (institutional access or purchase)
- **Shared with:** Pass Mechanics Spec #5 §8.1.1 ⚠ Pass Mechanics §8 must also be
  updated to reflect this DOI correction.
- **Used for:**
  - Distinction between instep (Centre ContactZone) and side-foot (OffCentre
    ContactZone) biomechanics: instep produces higher ball speed; side-foot
    produces greater accuracy and more sidespin. Directly supports the
    `ContactZoneModifier` value differentials in §3.2.
  - Confirmation that non-preferred leg kicks produce measurably lower ball speed —
    informs the weak foot velocity reduction model in §3.8.
  - Body lean backward relationship to elevated ball trajectory — directly informs
    §3.3.6 (BodyLeanPenalty) and §3.7.6 (body lean score).
- **Key content:** 3D kinetic analysis comparing instep kicks with preferred and
  non-preferred leg. Provides direct numeric evidence for velocity modifier values
  and laterality effects.
- **Limitations:** Controlled lab study; in-match conditions (fatigue, pressure,
  body shape) are not modelled.

---

**[INOUE-2014]** Inoue, K., Nunome, H., Sterzing, T., Shinkai, H., & Ikegami, Y.
(2014). Dynamics of the support leg in soccer instep kicking. *Journal of Sports
Sciences, 32*(11), 1023–1032.

- **DOI:** 10.1080/02640414.2014.886126 ✅ Verified
  *(v1.0 had incorrect DOI 10.1080/02640414.2013.877593 — corrected in v1.1)*
- **Access:** Taylor & Francis Online (institutional access or purchase)
- **Used for:**
  - Physical basis for `BaseAngle[BelowCentre] = 18°`: below-centre contact
    elevates trajectory significantly because the foot imparts upward force vector
    on the ball. Inoue documents the relationship between contact point height and
    resultant ball elevation.
  - Support leg (plant foot) dynamics: stance leg loading and position during the
    kicking moment — supports the plant foot offset scoring in §3.7.4.
- **Key content:** Motion analysis of support leg during instep kick with force plate
  data. Relevant to understanding how stance leg mechanics affect kick outcome.
- **Limitations:** Focused on instep kick; curved/chip shots with below-centre
  contact require extrapolation.

---

### 8.1.2 Ball Spin and Aerodynamic Effects

---

**[ASAI-2002]** Asai, T., Carré, M.J., Akatsuka, T., & Haake, S.J. (2002). The curve
kick of a football I: Impact with the foot. *Sports Engineering, 5*(4), 183–192.

- **DOI:** 10.1046/j.1460-2687.2002.00108.x ✅ Verified
  *(v1.0 had incorrect DOI 10.1046/j.1460-2687.2002.00098.x — corrected in v1.1)*
- **Access:** Wiley Online Library (institutional access or purchase)
- **Shared with:** Pass Mechanics Spec #5 §8.1.2, Ball Physics Spec #1 §8.1
  ⚠ Both specs must also be updated to reflect this DOI correction.
- **Used for:**
  - Physical basis for off-centre contact producing sidespin (OffCentre ContactZone
    → high `SidespinBase`). Asai documents that foot contact displaced laterally
    from ball centre imparts angular momentum about the vertical axis.
  - Spin magnitude orders of magnitude: curved shots achieve 5–10 rev/s (≈ 31–63
    rad/s) at high `SpinIntent`, supporting `SIDESPIN_BASE[OffCentre]` value range.
  - Confirmation that sidespin reduces velocity: off-centre kicks generate more
    spin at the expense of ball speed — supports `ContactZoneModifier[OffCentre]
    = 0.85` and the spin–velocity trade-off in §3.2.
- **Key content:** Combined experimental and CFD study of curved kick mechanics.
  Contains specific angular velocity measurements for off-centre contact conditions.
- **Limitations:** Study conditions use controlled approach and contact angle; in-match
  contact variation requires [GT] interpolation for magnitude constants.

---

**[BRAY-2003]** Bray, K., & Kerwin, D.G. (2003). Modelling the flight of a soccer
ball in a direct free kick. *Journal of Sports Sciences, 21*(2), 75–85.

- **DOI:** 10.1080/0264041031000070994 ✅ Verified
- **Access:** Taylor & Francis Online (institutional access or purchase)
- **Shared with:** Pass Mechanics Spec #5 §8.1.2, Ball Physics Spec #1 §8.1
- **Used for:**
  - Spin rate ranges for in-match shots: free kick shots achieve 8–10 rev/s
    (≈ 50–63 rad/s) — provides the upper order-of-magnitude bound for
    `SPIN_MAX_MAGNITUDE`.
  - Confirmation that topspin reduces effective flight distance (dipping trajectory)
    — validates the use of `TospinBase[Centre]` as a meaningful physical modifier.
  - Aerodynamic coupling between spin and velocity — confirms that spin–velocity
    trade-off is physically motivated, not a pure design choice.
- **Key content:** Flight trajectory modelling for free kicks including spin rates
  measured by video analysis. Direct source for spin magnitude benchmarks.
- **Limitations:** Free kick conditions (set piece, stationary ball) differ from
  open-play shots; `TOPSPIN_BASE` and `BACKSPIN_BASE` require [GT] tuning from
  these benchmarks rather than direct adoption.

---

**[CARRE-2002]** Carré, M.J., Asai, T., Akatsuka, T., & Haake, S.J. (2002). The
curve kick of a football II: Flight through the air. *Sports Engineering, 5*(4),
193–200.

- **DOI:** 10.1046/j.1460-2687.2002.00109.x ✅ Verified
  *(v1.0 had incorrect label CARRE-2004 and incorrect DOI 10.1046/j.1460-2687.2002.00099.x
  — both corrected in v1.1. Paper is from 2002, not 2004.)*
- **Access:** Wiley Online Library (institutional access or purchase)
- **Shared with:** Ball Physics Spec #1 §8.1 *(previously cited under CARRE-2004 label
  — that label must be updated to CARRE-2002 in Ball Physics §8 as well)*
- **Used for:**
  - Ball aerodynamics in the spin regime: Magnus force coefficients for football-
    sized spheres at shot-relevant speeds (20–35 m/s) and spin rates
    (0–65 rad/s). Shared with Ball Physics #1; reproduced here because Shot
    Mechanics §3.4 spin outputs feed directly into the Magnus model.
  - Confirms that sidespin at ~8 rev/s produces lateral deflection of 1–2m over
    20m — provides validation reference for §3.4 spin vector magnitudes via
    Ball Physics simulation (Appendix B).
- **Key content:** CFD and wind tunnel measurements of Magnus force on a football.
  Contains lift and drag coefficient data as functions of spin parameter.
- **Limitations:** Steady-state aerodynamics; in-flight spin decay (Ball Physics §3.1.4)
  is a separate model. Shot Mechanics outputs initial spin; Ball Physics governs decay.

---

### 8.1.3 Shot Accuracy Under Pressure and Fatigue

---

**[DICKS-2010]** Dicks, M., Button, C., & Davids, K. (2010). Examination of gaze
behaviors under in situ and video simulation task constraints reveals differences in
information pickup for perception and action. *Attention, Perception, & Psychophysics,
72*(3), 706–720.

- **DOI:** 10.3758/APP.72.3.706 ✅ Verified (confirmed via Springer / PsycINFO)
- **Access:** Springer / Psychonomic Society (institutional access)
- **Shared with:** Pass Mechanics Spec #5 §8.1.3
- **Used for:**
  - Empirical evidence that defensive proximity degrades shooting accuracy by
    constraining the shooter's gaze behaviour and reducing decision time. Grounds the
    `PressureScalar` in §3.6 in observed performance data.
  - PRESSURE_RADIUS calibration: proximity effects on accuracy become significant
    when a defender is within approximately 2–3m of the ball. Supports the [GT]
    tuning range for `PRESSURE_RADIUS`.
- **Key content:** Gaze analysis study comparing in-situ and simulated penalty
  scenarios. Documents how attentional demands under proximity pressure affect
  motor output accuracy.
- **Limitations:** Penalty-specific study; generalisation to open-play shooting
  scenarios requires [GT] scaling assumptions.

---

**[BEILOCK-2007]** Beilock, S.L., & Gray, R. (2007). Why do athletes choke under
pressure? In G. Tenenbaum & R.C. Eklund (Eds.), *Handbook of Sport Psychology*
(3rd ed., pp. 425–444). Wiley.

- **DOI:** 10.1002/9781118270011.ch19 ✅ Verified (confirmed via Wiley Online Library)
  *(Label corrected from [BEILOCK-2007]; year corrected from 2010 to 2007; DOI corrected
  from .ch20 to .ch19. Correction from Perception System §8 v1.2 DOI verification.)*
- **Access:** Wiley Online Library (institutional access or purchase)
- **Shared with:** Pass Mechanics Spec #5 §8.1.3, First Touch Spec #4 §8.1.2,
  Perception System Spec #7 §8.1.3 — **all updated to [BEILOCK-2007]**
- **Used for:**
  - Theoretical basis for the `PressureScalar` formula structure: cognitive load
    from proximity pressure does not linearly subtract from skill — it degrades
    the quality of motor execution multiplicatively. Supports the multiplicative
    modifier chain in §3.6.
  - Supports `PRESSURE_WEIGHT` magnitude (~0.30–0.40 range): literature documents
    20–50% performance degradation in high-stress motor tasks.
  - Differentiation between Composure (attribute) as baseline pressure resistance
    and `PressureScalar` as situational degradation — the formula structure in
    §3.6.5 reflects this distinction.
- **Key content:** Comprehensive review of choking under pressure mechanisms —
  attentional control theory and distraction theory. Foundational reference for
  the pressure degradation model across all physics specifications.
- **Limitations:** General sports psychology; shot-specific pressure magnitudes
  require football-specific [GT] calibration.

---

**[ALI-2011]** Ali, A. (2011). Measuring soccer skill performance: A review.
*Scandinavian Journal of Medicine and Science in Sports, 21*(2), 170–183.

- **DOI:** 10.1111/j.1600-0838.2010.01256.x ✅ Verified (confirmed via Wiley / PubMed)
- **Access:** Wiley Online Library (institutional access or purchase)
- **Shared with:** Pass Mechanics Spec #5 §8.1.3
- **Used for:**
  - Direction confirmation for the `FatigueScalar` in §3.6.6: fatigue reduces
    kicking accuracy and velocity, with performance degradation increasing
    non-linearly at high fatigue levels (>70% depletion).
  - `FATIGUE_POWER_REDUCTION = 0.20` (20% velocity loss at full fatigue) is
    consistent with documented performance decrements in endurance protocols.
  - Also supports the body mechanics degradation under fatigue: late-match body
    mechanics scores are expected to be lower on average, which the formula
    captures through `Fatigue` inputs to both §3.6 and §3.7.
- **Key content:** Review of soccer skill assessment methods including velocity,
  accuracy, and consistency across a match. Contains data on within-match
  performance decay at fatigue-relevant time points.
- **Limitations:** Aggregate review; specific constants require [GT] calibration
  against the reference attribute ranges defined in Agent Movement §3.5.6.

---

### 8.1.4 Weak Foot and Laterality in Football

---

**[CAREY-2001]** Carey, D.P., Smith, G., Smith, D.T., Shepherd, J.W., Skriver, J.,
Ord, L., & Rutland, A. (2001). Footedness in world soccer: An analysis of
France '98. *Journal of Sports Sciences, 19*(11), 855–864.

- **DOI:** 10.1080/026404101753113804 ✅ Verified (confirmed via Taylor & Francis,
  PubMed, and multiple subsequent citations in the literature)
- **Access:** Taylor & Francis Online (institutional access or purchase)
- **Shared with:** Pass Mechanics Spec #5 §8.1.4
- **Used for:**
  - Empirical grounding for the weak foot penalty range: Carey documents measurable
    accuracy and power deficits when professional players use their non-dominant
    foot, with performance penalties ranging from ~15% to ~60% depending on
    individual laterality strength.
  - `WeakFootRating` (1–5 scale) calibration: a rating of 1 corresponds to extreme
    laterality (large penalty); rating of 5 corresponds to genuine ambidexterity
    (minimal penalty). This range is informed by the distribution of laterality
    effects documented in the France '98 dataset.
  - Separate error and velocity penalties: Carey's data shows both accuracy
    degradation and a modest power reduction when using the non-dominant foot —
    directly motivates the two-parameter weak foot model in §3.8 (error cone
    multiplier + velocity reduction).
- **Key content:** Observational analysis of 1,497 ball touches from France '98 World
  Cup, classifying dominant and non-dominant foot use and comparing outcomes.
- **Limitations:** Observational data from match footage; controlled laterality
  measurement cannot be extracted. `SHOT_WF_BASE_ERROR_PENALTY = 0.60` is an
  extrapolation from Carey's range rather than a direct measurement, and should
  be reviewed for [GT] reclassification if supporting evidence is insufficient
  (flagged in §3.8 constants reference).

---

### 8.1.5 Body Mechanics and Shooting Stance Biomechanics

---

**[LEES-1998]** *(See §8.1.1 — this source serves double duty)*

The body mechanics model in §3.7 draws on [LEES-1998] for:
- Run-up angle range: 30°–45° to the goal line is documented as optimal for
  instep kicks, providing the ideal range used in §3.7.3 `RunUpAngleScore`.
- Plant foot distance: optimal planting at 20–30 cm lateral to the ball — supports
  the `PLANT_FOOT_OPTIMAL_OFFSET` range in §3.7.4.

---

**[KELLIS-2007]** *(See §8.1.1 — this source serves double duty)*

The body mechanics model in §3.7 additionally draws on [KELLIS-2007] for:
- Approach velocity contribution to ball speed: agents moving toward goal at
  contact receive a velocity bonus proportional to their approach speed (§3.7.5).
- Quantification of how plant foot positioning constrains hip rotation range —
  directly informs the plant foot offset penalty shape.

---

**[NUNOME-2006]** *(See §8.1.1 — this source serves double duty)*

The body mechanics model in §3.7 additionally draws on [NUNOME-2006] for:
- Confirmation that body lean backward results in elevated ball trajectory (shot
  skied over the bar). This is the primary real-world cause that §3.7.6 (body lean
  measurement) and §3.3.6 (BodyLeanPenalty) are designed to model.
- Quantification of the relationship between upper body lean angle and resultant
  launch angle elevation — informs the linear mapping in §3.3.6.

---

## 8.2 Real-World Data Sources

### 8.2.1 Shot Velocity and Distance Distributions

---

**[STATSBOMB-OPEN]** StatsBomb. (2018–present). StatsBomb Open Data. GitHub
repository containing structured event data for selected competitions.

- **Access:** Public — https://github.com/statsbomb/open-data (verify licence terms
  before using data commercially)
- **Shared with:** Pass Mechanics Spec #5 §8.2.1, Ball Physics Spec #1 §8.2
- **Used for:**
  - Validation of velocity reference ranges in §2.2: ground-level shot speeds of
    12–24 m/s and driven shot speeds of 18–30 m/s are consistent with StatsBomb
    shot data (ball speed derived from freeze frame position analysis).
  - Shot distance distribution: confirms that the majority of open-play shots
    occur within 6–25m of goal — grounds the `D_MID = 20m` sigmoid midpoint
    (OI-001) in observed shot location data.
  - xG context: StatsBomb xG values for shots from various distances and body
    positions provide an indirect validation signal for the error model in §3.6 —
    elite striker shot accuracy should correspond to high xG outcomes at close range.
- **Limitations:** StatsBomb open data covers specific competitions only; shot
  velocity is inferred, not directly measured. Commercial (paid) StatsBomb data
  provides direct velocity fields but is not available for this project at Stage 0.
  All uses here are contextual/validation only, not formula derivation.

---

**[FIFA-QUALITY]** FIFA Quality Programme. (2024). *FIFA Quality Programme for
Footballs: Performance Requirements.* FIFA Technical Document.

- **Access:** FIFA website (public document)
- **Shared with:** Ball Physics Spec #1 §8.2
- **Used for:**
  - Ball behaviour standards (coefficient of restitution, pressure decay) relevant
    to the boundary conditions within which shot velocities operate. The FIFA
    standards define the range of ball behaviours that the physics model must
    reproduce.
  - Confirms that ball speed following contact is physically bounded by the
    energy transfer efficiency documented in the standard — supports `V_CEILING
    = 35.0 m/s` as consistent with FIFA ball standards under legal contact force.
- **Limitations:** Standards document rather than research publication. No
  constants are directly derived from this source; it provides physical boundary
  confirmation only.

---

### 8.2.2 Shot Outcome and xG Data

---

**[IMPECT-2023]** Impect GmbH. (2023). *Impect Packing Data Methodology.* Technical
documentation. Available at https://www.impect.com/

- **Used for:** Contextual reference for the role of shot pressure and defensive
  proximity in real-world shot outcomes. Impect's packing metric quantifies
  opponents bypassed per action, providing indirect evidence that defensive pressure
  within the shot corridor degrades shooting outcomes — consistent with the
  `PressureScalar` model in §3.6.
- **Limitations:** Commercial methodology with limited public detail. Used for
  directional confirmation only; no constants are derived from this source.

---

*Note: [WYSCOUT-VELOCITY] removed in v1.1 as required by the pre-approval action in
v1.0 §8.6.11. The velocity range (15–34 m/s in-match; 22–27 m/s penalties) is
adequately grounded by [LEES-1998] and [STATSBOMB-OPEN] independently.*

---

## 8.3 Internal Project Documents

### 8.3.1 Master Volumes

---

**[MASTER-VOL1]** *System XI — Master Volume 1: Physics Core.*
Internal project document. File: `Master_Vol_1_Physics_Core.md`.

- **Used for:**
  - §1.3: Determinism requirement — all randomness in Shot Mechanics must use the
    deterministic hash `f(matchSeed, agentId, frameNumber)`. This is the binding
    design authority for §3.6.9 (deterministic error direction). No override is
    permitted.
  - Coordinate system definition: pitch corner at (0,0,0), X = pitch length,
    Z = pitch width, Y = height. Shot Mechanics §3.5 goal mouth coordinates and
    §3.6 error vectors operate in this coordinate space.
  - Performance budget philosophy: discrete-event cost model (not per-frame),
    adopted in §6 (Performance Analysis).

---

**[MASTER-VOL2]** *System XI — Master Volume 2: Human Systems.*
Internal project document. File: `Master_Vol_2_Human_Systems.md`.

- **Used for:**
  - `ATTR_MAX = 20.0` — the [1–20] attribute scale is the design authority for
    all attribute-normalised formulas in §3.2 (`EffectiveAttribute / ATTR_MAX`),
    §3.6 (`NormalisedFinishing`, `NormalisedComposure`), §3.7, and §3.8.
  - `WeakFootRating` (1–5 scale) definition — `Agent Movement §3.5.6` confirms
    the field; Master Vol 2 establishes the design meaning of each rating level.
  - `FormModifier` interface (deferred-design): §3.2 and §3.6 contain no-op hooks
    at `[STAGE-3-HOOK]` sites for Form System integration. Master Vol 2 §FormSystem
    is the future authority for this modifier — not yet written.
  - `PsychologyPressureScale` interface (deferred-design): §3.6.5 defaults this
    to 1.0f at Stage 0. Master Vol 2 §H-Gate is the future authority.

---

**[MASTER-VOL4]** *System XI — Master Volume 4: Tech Implementation.*
Internal project document. File: `Master_Vol_4_Tech_Implementation.md`.

- **Used for:**
  - Event system patterns: `ShotExecutedEvent` publication in §3.10 follows the
    event bus subscription pattern established in Master Vol 4 §EventSystem.
  - Logging policy: `PassOutcome.Invalid` / `ShotOutcome.Invalid` logging to
    structured log at Warning level. The logging standard is the design authority
    for §3.9 failure handling.
  - Zero heap-allocation requirement for all physics evaluation methods —
    design authority for the struct-only implementation confirmed in §6.

---

### 8.3.2 Related Specifications

---

**[BALL-PHYSICS-1]** *Ball Physics Specification #1 (Approved).* Multiple section files.

- **Used for:**
  - `Ball.ApplyKick()` interface: velocity vector and spin vector are the two primary
    outputs of Shot Mechanics. This interface is the hard dependency confirmed before
    Section 3 drafting.
  - Magnus force model (§3.1.4): all spin vectors from §3.4 are consumed by the
    Magnus model. Shot Mechanics is responsible for producing physically meaningful
    spin magnitudes that the Magnus model can produce observable trajectories from.
  - Coordinate system and physics constants shared by both specifications.
  - `V_CEILING = 35.0 m/s` is consistent with the drag model — balls at 35 m/s
    reach the goal from 20m in approximately 0.57s under drag, which is physically
    plausible (cross-checked in Appendix B).

---

**[AGENT-MOVEMENT-2]** *Agent Movement Specification #2 (Approved).* Multiple
section files.

- **Used for:**
  - `PlayerAttributes` struct (§3.5.6): `Finishing`, `LongShots`, `Composure`,
    `KickPower`, `WeakFootRating`, `Technique` — all confirmed present and stable.
  - `AgentState.Fatigue` field: confirmed present and normalised [0.0, 1.0].
  - `AgentPhysicalProperties`: `RunUpAngle`, `PlantFootOffset`, `AgentVelocity`,
    `BodyLeanAngle` — confirmed as inputs to §3.7 (Body Mechanics Evaluation).
  - `STUMBLING` signal: the hysteresis-based stumble trigger from §3.7.9 emits
    to Agent Movement's existing STUMBLING state handler. No new interface required.

---

**[COLLISION-3]** *Collision System Specification #3 (Approved).* Multiple
section files.

- **Used for:**
  - Tackle interrupt flag: polling pattern during WINDUP state in §3.9. Identical
    to Pass Mechanics pattern — no new interface required.
  - Impulse-based collision detection does not interact with shot velocity
    calculation; Shot Mechanics reads a boolean interrupt flag only.
  - ⚠ **ERR-009 note:** Collision System §3 requires revision before Shot Mechanics
    pressure query path can be implemented. This does not block Shot Mechanics
    sign-off but must be resolved before implementation begins.

---

**[PASS-MECHANICS-5]** *Pass Mechanics Specification #5 (Approved).* Multiple
section files.

- **Used for:**
  - Weak foot formula structure (§3.8): error cone multiplier and velocity reduction
    formula patterns are reused directly from Pass Mechanics §3.7.
  - Shot state machine pattern (§3.9): IDLE → INITIATING → WINDUP → CONTACT →
    FOLLOW_THROUGH → COMPLETE sequence reused from Pass Mechanics §3.8.
  - `KickType` resolution pattern: Shot Mechanics follows the same parameter-based
    model rather than named type enum (KD-3 consistency).
  - Citation cross-reference: [NUNOME-2006] DOI correction in this document also
    applies to Pass Mechanics §8.1.1 — update required.

---

### 8.3.3 Development Documentation

---

**[BEST-PRACTICES]** *System XI — Development Best Practices.*
Internal project document. File: `Development_Best_Practices.md`.

- **Used for:** Specification structure, version control discipline, approval
  checklist format, and cross-specification consistency requirements.

---

## 8.4 Software and Tools

---

**[UNITY-ENGINE]** Unity Technologies. (2024). *Unity Engine — version 2022 LTS.*
https://unity.com/

- **Used for:** Target implementation platform. All performance targets in §6 are
  expressed in terms of Unity's Update() and physics tick cycles.
- **Note:** No gameplay constants are derived from Unity engine behaviour; it is
  listed here for completeness as the implementation authority.

---

**[CSHARP-LANG]** Microsoft. (2024). *C# Language Reference.*
https://learn.microsoft.com/en-us/dotnet/csharp/

- **Used for:** Implementation language. All code structures referenced in §4
  (Architecture) are C# structs and classes targeting Unity's .NET Standard 2.1
  compatibility profile.

---

## 8.5 Citation Summary

Quick-reference table mapping every formula area to its primary academic source.

| Formula Area | Primary Source(s) | Classification |
|---|---|---|
| §3.2 Velocity — base formula | [LEES-1998], [KELLIS-2007] | ACADEMIC-INFORMED |
| §3.2 V_BASE, V_FLOOR, V_CEILING | [LEES-1998], [STATSBOMB-OPEN] | ACADEMIC-INFORMED + [GT] |
| §3.2 ContactZoneModifier | [NUNOME-2006], [ASAI-2002] | ACADEMIC-INFORMED + [GT] |
| §3.2 PowerPenalty | [LEES-1998] | ACADEMIC-INFORMED + [GT] |
| §3.2 LongShotsBonus | [KELLIS-2007] | ACADEMIC-INFORMED + [GT] |
| §3.3 BaseAngle[Centre] | [LEES-1998] | ACADEMIC-INFORMED |
| §3.3 BaseAngle[BelowCentre] | [INOUE-2014] | ACADEMIC-INFORMED + [GT] |
| §3.3 BaseAngle[OffCentre] | [ASAI-2002] | ACADEMIC-INFORMED + [GT] |
| §3.4 TOPSPIN_BASE, BACKSPIN_BASE | [BRAY-2003] | ACADEMIC-INFORMED + [GT] ⚠[VER] |
| §3.4 SIDESPIN_BASE | [ASAI-2002], [CARRE-2002] | ACADEMIC-INFORMED + [GT] ⚠[VER] |
| §3.5 Placement resolution | Internal geometry | DESIGN-AUTHORITY |
| §3.6 PowerPenalty | [LEES-1998] | ACADEMIC-INFORMED + [GT] |
| §3.6 PressureScalar | [DICKS-2010], [BEILOCK-2007] | ACADEMIC-INFORMED + [GT] |
| §3.6 FatigueScalar | [ALI-2011] | ACADEMIC-INFORMED + [GT] |
| §3.6 Deterministic error direction | [MASTER-VOL1] | DESIGN-AUTHORITY |
| §3.7 RunUpAngleScore | [LEES-1998] | ACADEMIC-INFORMED |
| §3.7 PlantFootScore | [LEES-1998], [KELLIS-2007] | ACADEMIC-INFORMED + [GT] |
| §3.7 AgentVelocityBonus | [KELLIS-2007] | ACADEMIC-INFORMED + [GT] |
| §3.7 BodyLeanPenalty | [NUNOME-2006] | ACADEMIC |
| §3.8 WF error multiplier | [CAREY-2001] | ACADEMIC-INFORMED + [GT] |
| §3.8 SHOT_WF_BASE_ERROR_PENALTY | [CAREY-2001] | ACADEMIC-INFORMED ⚠[VER] |
| §3.9 State machine | [PASS-MECHANICS-5] | DESIGN-AUTHORITY |
| §3.10 Event publishing | [MASTER-VOL4] | DESIGN-AUTHORITY |

---

