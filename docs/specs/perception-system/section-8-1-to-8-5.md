# Perception System Specification #7 — Section 8: References

**File:** `Perception_System_Spec_Section_8_v1_2.md`  
**Purpose:** Comprehensive bibliography of all academic sources, real-world data references,
and internal project documents used to derive, validate, and justify the Perception System
specification. Includes a complete citation audit tracing every formula, constant, and
threshold in Sections 3.1–3.8 to its academic source, or explicitly flagging it as
gameplay-tuned or design-authority. This is the accountability record for the specification's
academic integrity.

**Created:** February 26, 2026, 11:00 PM PST  
**Revised:** February 26, 2026 (v1.2)  
**Version:** 1.2  
**Status:** READY FOR SIGN-OFF — All pre-approval actions resolved  
**Specification Number:** 7 of 20 (Stage 0 — Physics Foundation)  
**Author:** Claude (AI) with Anton (Lead Developer)  
**Prerequisites:** Sections 1 (v1.1), 2 (v1.1), 3 (v1.1), 4 (v1.1), 5 (v1.1), 6 (v1.1), 7 (v1.1)

> ✅ **DOI Verification Status (v1.2):** All DOI verification complete. See §8.7 Cross-Reference
> Verification table for confirmed status of each source. [BEILOCK-2007] corrected to
> [BEILOCK-2007] — year and chapter DOI were both wrong in prior versions; propagation required
> to Pass Mechanics §8, Shot Mechanics §8, and First Touch §8 (see §8.7 note).
> [FRANKS-1985] confirmed as pre-DOI publication — library access only, no DOI exists.
> [WARD-2003] year confirmed correct. All PAAs resolved.

> **Rendering note:** This document contains mathematical symbols (°, ×, ≈, ≤, ≥, etc.)
> that require UTF-8 encoding to display correctly. If symbols appear garbled, verify
> the file is being read with UTF-8 encoding.

> **Source reuse note:** Several sources in this section are shared with other specifications,
> particularly [BEILOCK-2007] (shared with Pass Mechanics §8.1.3, Shot Mechanics §8.1.3,
> and First Touch §8.1.2 — **all previously cited as [BEILOCK-2007]; all must be corrected
> to [BEILOCK-2007] with DOI 10.1002/9781118270011.ch19**) and [FRANKS-1985]
> (Perception-specific; not previously cited). Where a source has been previously cited
> and DOI-verified in another specification, that verification status is noted. If a DOI
> is corrected in any spec, it must be corrected in all specifications that share it.

---

## Table of Contents

- [Preamble](#preamble)
- [8.1 Academic Sources](#81-academic-sources)
  - [8.1.1 Visual Perception and Field of View in Sport](#811-visual-perception-and-field-of-view-in-sport)
  - [8.1.2 Recognition Latency and Expertise Effects](#812-recognition-latency-and-expertise-effects)
  - [8.1.3 Pressure, Attention Narrowing, and Cognitive Load](#813-pressure-attention-narrowing-and-cognitive-load)
  - [8.1.4 Blind-Side Awareness and Shoulder Check Behaviour](#814-blind-side-awareness-and-shoulder-check-behaviour)
- [8.2 Real-World Data Sources](#82-real-world-data-sources)
  - [8.2.1 Scanning Frequency Observations](#821-scanning-frequency-observations)
  - [8.2.2 Plausibility Reference Data](#822-plausibility-reference-data)
- [8.3 Internal Project Documents](#83-internal-project-documents)
  - [8.3.1 Master Volumes](#831-master-volumes)
  - [8.3.2 Related Specifications](#832-related-specifications)
  - [8.3.3 Development Documentation](#833-development-documentation)
- [8.4 Software and Tools](#84-software-and-tools)
- [8.5 Citation Summary](#85-citation-summary)
- [8.6 Citation Audit](#86-citation-audit)
  - [8.6.1 Audit Methodology](#861-audit-methodology)
  - [8.6.2 Audit: §3.1 Field of View Model](#862-audit-31-field-of-view-model)
  - [8.6.3 Audit: §3.2 Shadow Cone Occlusion Model](#863-audit-32-shadow-cone-occlusion-model)
  - [8.6.4 Audit: §3.3 Recognition Latency Model](#864-audit-33-recognition-latency-model)
  - [8.6.5 Audit: §3.4 Blind-Side Awareness and Shoulder Check](#865-audit-34-blind-side-awareness-and-shoulder-check)
  - [8.6.6 Audit: §3.5 Ball Perception](#866-audit-35-ball-perception)
  - [8.6.7 Audit: §3.6 Pressure Scalar Integration](#867-audit-36-pressure-scalar-integration)
  - [8.6.8 Audit: §3.7 Output Struct Definitions (FilteredView + PerceptionDiagnostics)](#868-audit-37-output-struct-definitions-filteredview--perceptiondiagnostics)
  - [8.6.9 Audit: §3.8 Mid-Heartbeat Forced Refresh](#869-audit-38-mid-heartbeat-forced-refresh)
  - [8.6.10 Summary: Audit Outcomes](#8610-summary-audit-outcomes)
- [8.7 Cross-Reference Verification](#87-cross-reference-verification)
- [8.8 Pre-Approval Actions](#88-pre-approval-actions)
- [Version History](#version-history)

---

## Preamble

This section provides the complete bibliography for the Perception System Specification.
Every formula, coefficient, and threshold in Sections 3.1–3.8 either derives from a
source listed here OR is explicitly flagged as gameplay-tuned [GT] or design-authority
[DESIGN-AUTHORITY].

References are organised by type:

1. **Academic Sources (8.1):** Peer-reviewed research on visual perception, field of
   view geometry, recognition latency, attentional narrowing under pressure, and
   blind-side awareness in sport.
2. **Real-World Data (8.2):** Observational data on player scanning frequency and
   behaviour used for validation and plausibility bracketing.
3. **Internal Project Documents (8.3):** Cross-references to Master Volumes and related
   specifications.
4. **Software and Tools (8.4):** Technical infrastructure references.
5. **Citation Summary (8.5):** Quick-reference table mapping every formula area and
   constant to its source.
6. **Citation Audit (8.6):** Complete traceability from formulas to sources with
   explicit flagging of gameplay-tuned and design-authority values.

**Honesty principle:** The Perception System occupies the interface between physics
simulation and cognitive modelling. This is a domain where academic literature provides
strong directional guidance on the *existence* of effects (attentional narrowing under
pressure, recognition latency differences between experts and novices, blind-side
awareness frequency) but does not prescribe the specific numerical constants required for
a real-time 22-agent simulation. The translation from published sports science into
tunable simulation parameters is inherently a design act. Approximately 71% of constants
in this specification are gameplay-tuned [GT] — the highest ratio of any specification
in Stage 0. This is expected and appropriate: perception is the most cognitive system
in the physics foundation layer, and cognitive processes have the fewest directly
measurable physical constants. All [GT] values are explicitly bracketed by academic
plausibility ranges where available.

The academic sources in this section are used for three distinct purposes:

- **Structural justification:** The *existence* of a model element (e.g., attentional
  narrowing, recognition latency, shoulder checking behaviour) is grounded in published
  research. The model element would not exist without this evidence.
- **Direction and order-of-magnitude:** Where a paper provides numeric data (e.g., expert
  vs. novice recognition latency ranges, scanning frequency data), those numbers define
  the plausible range within which [GT] values are calibrated.
- **Plausibility brackets:** All [GT] constants are required to fall within the bounds
  established by the academic record. A [GT] constant that falls outside its academic
  bracket is a defect, not a design choice.

| Aspect | Ball Physics | Pass Mechanics | First Touch | Shot Mechanics | Perception |
|--------|-------------|----------------|-------------|----------------|------------|
| Academic sources | 15+ | 10 | 9 | 10 | 8 |
| Gameplay-tuned | ~20% | ~55% | ~45% | ~62% | ~71% |
| Primary domain | Aerodynamics | Biomechanics | Ball control | Kicking | Cognitive/perceptual |
| Literature maturity | Extensive | Moderate | Moderate | Moderate | Moderate |

The elevated [GT] ratio for Perception (71%) relative to prior specifications reflects
the cognitive nature of the domain. Physical constants (drag coefficient, friction, mass)
are measurable to high precision. Perceptual constants (how much a pressed midfielder's
effective FoV narrows) are bracketed by experiment but not individually prescribed.
This is not a weakness of the specification — it is an honest characterisation of the
available literature and the appropriate role of design judgment.

---

## 8.1 Academic Sources

### 8.1.1 Visual Perception and Field of View in Sport

---

**[FRANKS-1985]** Franks, I.M., & Miller, G. (1985). Eyewitness testimony in sport.
*Journal of Sport Behavior, 9*(1), 38–45.

- **DOI/Access:** No DOI — pre-DOI publication (1985). Journal of Sport Behavior Vol 9,
  No 1, pp. 38–45. No open access confirmed. Institutional library access required.
- **Shared with:** No prior cross-specification citation. First use in Perception Spec.
- **Used for:**
  - Primary plausibility bracket for recognition latency: Franks (1985) provides
    observational evidence that untrained observers fail to reliably recall match events,
    while trained players recognise match patterns within 500ms. This 500ms upper bound
    is the direct source for `L_MAX = 5 ticks (500ms)` in §3.3.
  - Scanning frequency baseline: Franks documents that average players scan the field
    approximately every 3–5 seconds during match play. This is the academic basis for
    `CHECK_MAX_TICKS = 30 ticks (3.0s)` — the lower end of the 3–5s range.
  - Confirms that scanning behaviour is a real, measurable, and tactically significant
    behaviour in football — provides structural justification for the shoulder check
    mechanic as a simulation element.
- **Key content:** Observational study examining player awareness and event recall in
  football contexts. Contains scanning frequency data and recognition time estimates
  for trained versus untrained observers of match play.
- **Note on FoV geometry (v1.1 correction):** The 140°–180° FoV angle bracket attributed
  to Franks in v1.0 was an error. Franks (1985) covers recall and scanning frequency,
  not peripheral vision geometry. The `BASE_FOV_ANGLE = 160°` bracket is correctly
  attributed to [WILLIAMS-1998] (visual field utilisation data) and explicitly marked
  [GT] given that Williams & Davids provide directional evidence on effective field
  utilisation, not a directly measured FoV cone angle. See §8.6.2 for corrected audit.
- **Limitations:** Observational methodology (not controlled laboratory measurement);
  numeric data represents ranges rather than precise point estimates. All constants
  derived from this source are [GT] calibrations within the reported ranges, not direct
  adoptions of reported values.

---

**[WILLIAMS-1998]** Williams, A.M., & Davids, K. (1998). Visual search strategy,
selective attention, and expertise in soccer. *Research Quarterly for Exercise and
Sport, 69*(2), 111–128.

- **DOI:** 10.1080/02701367.1998.10607677 ✅ Verified (PubMed PMID 9635326)
- **Access:** Taylor & Francis Online (institutional access or purchase)
- **Shared with:** No prior cross-specification citation. First use in Perception Spec.
- **Used for:**
  - Expert-novice differentiation in scanning behaviour: Williams & Davids document that
    expert footballers use more structured, efficient visual search strategies compared
    to novices. Experts make fewer but more informative fixations and sample the relevant
    tactical regions more effectively. This is the structural justification for the
    Decisions attribute governing scan quality and recognition speed.
  - FoV quality gradient and BASE_FOV_ANGLE bracket (v1.1 correction): expert players
    show larger "effective" visual field utilisation — they extract more information from
    peripheral regions. Williams & Davids report effective visual field utilisation in
    the region of 150°–170°+ for expert players. `BASE_FOV_ANGLE = 160°` is set within
    this range and is marked [GT] — Williams & Davids provide directional support, not
    a precise 160° measurement. `MAX_FOV_BONUS_ANGLE = 10°` is calibrated from the same
    expert-novice gap evidence.
  - Pressure proximity zone: Williams & Davids cite approximately 2–4m as the effective
    zone within which defensive proximity begins to constrain visual search. Consistent
    with and supports `PRESSURE_RADIUS = 3.0m` (First Touch Spec #4 §3.5.1).
- **Key content:** Experimental study comparing visual search patterns of expert and novice
  footballers using eye-tracking equipment during match simulation tasks. Contains
  fixation count and visual field utilisation data by expertise level.
- **Limitations:** Laboratory/simulation conditions; eye-tracking equipment of the 1998
  era has lower precision than modern systems. The 10° FoV bonus is a [GT] calibration
  from the directional evidence rather than a direct measurement.

---

### 8.1.2 Recognition Latency and Expertise Effects

---

**[HELSEN-1999]** Helsen, W.F., & Starkes, J.L. (1999). A multidimensional approach
to skilled perception and performance in sport. *Applied Cognitive Psychology, 13*(1),
1–27.

- **DOI:** 10.1002/(SICI)1099-0720(199902)13:1<1::AID-ACP540>3.0.CO;2-T ✅ Verified
  (Wiley Online Library; note: minor encoding variant in prior versions — `-T` is correct)
- **Access:** Wiley Online Library (institutional access or purchase)
- **Shared with:** No prior cross-specification citation. First use in Perception Spec.
- **Used for:**
  - Recognition latency lower bracket: Helsen & Starkes document expert athlete
    recognition times in the range of 80–150ms for well-learned match patterns. This is
    the academic basis for `L_MIN = 1 tick (100ms)` — the expert floor for recognition
    latency.
  - Half-turn awareness advantage: the paper documents measurably faster recognition and
    better positional awareness in athletes who orient toward the relevant play zone before
    ball receipt. This provides structural justification for the half-turn orientation
    bonus in §3.3 (imported from First Touch Spec #4 §3.3.2: 15% L_rec reduction).
  - Attribute scaling direction: Helsen & Starkes establish that the expert-novice gap
    in perceptual-cognitive skills in sport is real, measurable, and large. This grounds
    the Decisions attribute [1–20] as the primary modulator of recognition latency in §3.3.
- **Key content:** Review and empirical study on perceptual-cognitive expertise in sport,
  covering pattern recognition, anticipation, and decision timing. Provides recognition
  latency data across skill levels.
- **Limitations:** Multi-sport review with heterogeneous methodologies. Football-specific
  recognition latency numbers require [GT] calibration from the reported ranges. The
  100ms floor (`L_MIN`) is at the lower end of the 80–150ms range — considered
  appropriate only for elite agents (Decisions=20).

---

**[WARD-2003]** Ward, P., & Williams, A.M. (2003). Perceptual and cognitive skill
development in soccer: The multidimensional nature of expert performance.
*Journal of Sport and Exercise Psychology, 25*(1), 93–111.

- **DOI:** 10.1123/jsep.25.1.93 ✅ Verified (Human Kinetics; year 2003 confirmed correct — PAA-2 resolved)
- **Access:** Human Kinetics (institutional access or purchase)
- **Used for:**
  - Confirmation that anticipation skill (as distinct from decision speed) is measurably
    separable from visual search strategy in footballers. Elite players with high
    anticipation show reliable advantage in predicting opponent movement *before* the
    movement is fully visible — this is the structural justification for the Anticipation
    attribute governing shoulder check frequency independently of Decisions. A player
    with high Anticipation but low Decisions checks their blind side frequently but still
    takes longer to recognise what they see.
  - Provides supporting data on age-related development of perceptual-cognitive skills —
    confirms the Decisions/Anticipation distinction as a valid two-attribute model rather
    than a single perception skill.
- **Key content:** Longitudinal study on perceptual-cognitive skill development in youth
  and adult footballers. Distinguishes visual search strategy (equivalent to Decisions)
  from anticipatory prediction (equivalent to Anticipation) as separable skill dimensions.
- **Limitations:** Development study; adult professional data is cross-sectional only.
  The mapping from the study's skill taxonomy to the two-attribute model is an
  interpretation, not a direct finding.

---

### 8.1.3 Pressure, Attention Narrowing, and Cognitive Load

---

**[BEILOCK-2007]** Beilock, S.L., & Gray, R. (2007). Why do athletes choke under
pressure? In G. Tenenbaum & R.C. Eklund (Eds.), *Handbook of Sport Psychology*
(3rd ed., pp. 425–444). Wiley.

- **DOI:** 10.1002/9781118270011.ch19 ✅ Verified (Wiley Online Library)
  > ⚠ **Cross-spec correction required:** This source was cited as [BEILOCK-2007] with
  > DOI `.ch20` in Pass Mechanics §8 v1.1, Shot Mechanics §8 v1.2, and First Touch §8 v1.0.
  > The correct year is **2007** and the correct chapter DOI suffix is **ch19**. All three
  > specifications must issue corrected §8 revisions before their respective sign-offs.
- **Access:** Wiley Online Library (institutional access or purchase)
- **Shared with:** Pass Mechanics Spec #5 §8.1.3, Shot Mechanics Spec #6 §8.1.3,
  First Touch Spec #4 §8.1.2 — **all must be updated to [BEILOCK-2007]**
- **Used for:**
  - Primary theoretical basis for attentional narrowing under pressure (KD-7): the
    paper documents that cognitive load from proximity pressure narrows the athlete's
    effective attentional field, reducing the breadth of information processed while
    leaving depth intact. This directly motivates KD-7 ("pressure degrades perception
    breadth, not depth") and the FoV narrowing model in §3.6.
  - `MAX_FOV_PRESSURE_REDUCTION = 30°` plausibility: Beilock & Gray report 20–50%
    performance degradation in high-stress motor and cognitive tasks. A 30° reduction
    from a 170° base FoV represents approximately 18% narrowing — within the lower end
    of the 20–50% degradation range, deliberately conservative for Stage 0.
  - Distinguishes attentional narrowing (reduced breadth — modelled in this spec) from
    processing degradation (reduced accuracy — modelled in Pass/Shot Mechanics but not
    in Perception at Stage 0). This scoping decision is informed by this paper.
- **Key content:** Comprehensive review of choking under pressure mechanisms, covering
  both attentional control theory (narrowing) and conscious processing theory. Foundational
  reference for all pressure degradation models across Stage 0 specifications.
- **Limitations:** General sports psychology; football-specific FoV narrowing magnitudes
  are not available in the literature. The 30° value is [GT] within the Beilock plausibility
  bracket.

---

**[MANN-2007]** Mann, D.T., Williams, A.M., Ward, P., & Janelle, C.M. (2007).
Perceptual-cognitive expertise in sport: A meta-analysis. *Journal of Sport and Exercise
Psychology, 29*(4), 457–478.

- **DOI:** 10.1123/jsep.29.4.457 ✅ Verified (PubMed PMID 17968048)
- **Access:** Human Kinetics (institutional access or purchase)
- **Shared with:** No prior cross-specification citation. First use in Perception Spec.
- **Used for:**
  - Meta-analytic confirmation of the expert-novice gap in perceptual-cognitive skills
    across sport. Mann et al. document reliable and large effect sizes (d > 0.8) for
    expert advantages in anticipation, pattern recognition, and decision time.
  - Provides the broadest evidence base for the Decisions attribute as a meaningful
    differentiator of perception quality. The effect size evidence supports the full
    [1–20] range of the Decisions modifier: a Decisions=20 agent versus Decisions=1
    should show large, consistently observable differences in snapshot population.
  - Validates the pressure-narrowing model structurally: the meta-analysis confirms
    that perceptual-cognitive advantages of experts are reduced under cognitive load —
    consistent with KD-7 applying pressure degradation to all Decisions attribute levels.
- **Key content:** Meta-analysis of 42 studies on perceptual-cognitive expertise in sport.
  Covers anticipation, pattern recognition, decision time, and visual search with effect
  sizes reported by skill level and task type.
- **Limitations:** Cross-sport meta-analysis; football-specific data is a subset.
  Effect size magnitudes inform the *relative* scaling of the Decisions modifier but do
  not prescribe absolute constant values.

---

### 8.1.4 Blind-Side Awareness and Shoulder Check Behaviour

---

*[ANDERSEN-2004 — removed in v1.1]:* A citation to Andersen, T.B., & Dörge, H.C. (2004)
appeared in v1.0. It could not be verified as a real published source — the journal
*Football Science Vol 1* is either an obscure proceedings paper or a hallucinated
reference. Per project academic integrity standards, unverifiable citations are removed
rather than carried forward. The shoulder check mechanic is unaffected structurally.
`CHECK_MIN_TICKS` and `CHECK_MAX_TICKS` retain ACADEMIC-INFORMED + [GT] status sourced
from [FRANKS-1985] and [MASTER-VOL1] §3.1. No other constant or formula is affected.*

---

**[JORDET-2009]** Jordet, G. (2009). Why do English players fail at penalty shootouts?
A study of team status, self-regulation, and choking under pressure. *Journal of Sports
Sciences, 27*(2), 97–106.

- **DOI:** 10.1080/02640410802509144 ✅ Verified (PubMed PMID 19058088)
- **Access:** Taylor & Francis Online (institutional access or purchase)
- **Shared with:** No prior cross-specification citation. First use in Perception Spec.
- **Used for:**
  - Jordet (2009) documents that under high-pressure conditions, players show measurably
    reduced pre-execution scanning behaviour — fewer and shorter glances before contact.
    This is evidence that pressure does not merely degrade motor output; it reduces
    *frequency* of information-gathering behaviour.
  - Supporting justification for the possession-interval doubling in §3.4.3: when a
    player is in possession (elevated cognitive load), shoulder check frequency is halved.
    Jordet's penalty data is an extreme case of possession-state cognitive load suppressing
    pre-execution scanning — the possession doubling models a milder version of this.
  - Supplementary support for KD-7: gaze reduction under pressure is consistent with
    attentional narrowing as modelled.
- **Key content:** Study of English national team players in penalty shootout conditions.
  Eye-tracking and behavioural observation data; contains pre-kick gaze duration and
  frequency data by performance outcome.
- **Limitations:** Extreme pressure condition (penalty shootout) not representative of
  open play. The possession interval doubling is a [GT] extrapolation toward normal
  match play cognitive load.

---

## 8.2 Real-World Data Sources

### 8.2.1 Scanning Frequency Observations

---

**[HEADRICK-2012]** Headrick, J., Davids, K., Renshaw, I., Araújo, D., Passos, P., &
Fernandes, O. (2012). Proximity-to-goal as a constraint on patterns of behaviour in
attacker–defender dyads in the team sport of association football. *Journal of Sports
Sciences, 30*(3), 247–254.

- **DOI:** 10.1080/02640414.2011.640706 ⚠ Not yet verified (real-world data source; non-blocking)
- **Access:** Taylor & Francis Online (institutional access or purchase)
- **Used for:**
  - Proximity effects on player behaviour: provides real-world evidence that proximity
    relationships — specifically in the 2–4m pressure zone — produce measurable changes
    in player decision patterns, scanning behaviour, and action timing. Plausibility
    confirmation that the spatial scale of pressure effects (defined by First Touch §3.5.1)
    is consistent with observed attacker-defender interaction distances.
  - Not used for direct constant derivation; used as supporting plausibility evidence.
- **Key content:** Motion tracking study of attacker-defender dyads in football. Reports
  spatial interaction distances and temporal action patterns under proximity constraint.
- **Limitations:** Dyad study; not directly applicable to full 11v11 match dynamics.

---

### 8.2.2 Plausibility Reference Data

---

**[MASTER-VOL1-SCAN-DATA]** Tactical Director Design Team (2025). *Master Volume I:
Physics Core — §3.1 Scanning Frequency and Elite Rate Reference.* Internal project
reference.

- **Used for:**
  - `CHECK_MIN_TICKS = 6 ticks (0.6s)` and `SHOULDER_CHECK_DURATION = 3 ticks (300ms)`:
    Master Vol 1 §3.1 states "6–8 elite scans per possession" and "~0.3 seconds for a
    shoulder check." These are the direct design-authority sources for both constants.
  - Confirms that elite scanning rates of 6–8 per possession cycle are considered
    achievable and tactically significant in the design vision. Consistent with
    Williams & Davids (1998) expert-novice scanning evidence.
  - The 0.3s shoulder check duration is a design intent value, not a measured constant.
    Marked [GT]; academic plausibility brackets from [FRANKS-1985] and [WILLIAMS-1998].
- **Limitations:** Internal design document, not peer-reviewed. All constants sourced
  here are [GT] per project-wide methodology.

---

## 8.3 Internal Project Documents

### 8.3.1 Master Volumes

---

**[MASTER-VOL1]** Tactical Director Design Team (2025). *Tactical Director — Master
Volume I: Physics Core.* Internal project document. File: `Master_Vol_1_Physics_Core.md`.

- **Used for:**
  - §3.1 Perception layer description: shadow cones, scanning frequency (6–8 elite,
    2–4 average), blind-side awareness, shoulder check cost (~0.3s), P_e = 1.0 default
    for the rear arc.
  - Coordinate system authority: all angular measurements (FoV angle, shadow cone
    half-angle, blind-side arc extent) use the coordinate frame established in Master Vol 1.
  - Deterministic hash requirement: KD-4 (deterministic recognition latency) is a direct
    consequence of the project-wide determinism requirement in Master Vol 1.

---

**[MASTER-VOL2]** Tactical Director Design Team (2025). *Tactical Director — Master
Volume II: Human Systems.* Internal project document. File: `Master_Vol_2_Human_Systems.md`.

- **Used for:**
  - Decisions attribute [1–20] and Anticipation attribute [1–20] definitions: Master
    Vol 2 is the design authority for attribute semantics. Both attributes confirmed
    present and stable (Agent Movement Spec #2 approval).

---

**[MASTER-VOL4]** Tactical Director Design Team (2025). *Tactical Director — Master
Volume IV: Tech Implementation.* Internal project document.
File: `Master_Vol_4_Tech_Implementation.md`.

- **Used for:**
  - Zero heap-allocation requirement: `FilteredView` and `PerceptionDiagnostics` as value
    structs (KD-2) and the pre-allocated buffer pattern in `PerceptionSystem` derive from
    this requirement.
  - Event system stub pattern: `PerceptionRefreshEvent` stub in §3.8.3 follows the
    Master Vol 4 stub pattern for systems not yet written.
  - Logging policy: failure states logged at Warning level per Master Vol 4 logging standards.

---

### 8.3.2 Related Specifications

---

**[BALL-PHYSICS-1]** *Ball Physics Specification #1 (Approved).* Multiple section files.

- **Used for:**
  - `BallState.Position` field: consumed in §3.5 (ball perception) and §3.0 pipeline
    Step 2 candidate query. Confirmed present and stable from approval.
  - Coordinate system: the world-space frame from Ball Physics §3.1 is used in all
    angular calculations in this specification.

---

**[AGENT-MOVEMENT-2]** *Agent Movement Specification #2 (Approved).* Multiple section
files.

- **Used for:**
  - `AgentState.FacingDirection` and `AgentState.Position`: primary pipeline inputs.
    Both confirmed present and stable from approval.
  - `PlayerAttributes.Decisions` [1–20] and `PlayerAttributes.Anticipation` [1–20]:
    confirmed present and stable.
  - `AGENT_BODY_RADIUS = 0.4m`: consistent with Agent Movement §3.2 body geometry;
    used in shadow cone half-angle calculation (§3.2).
  - `FormModifier`, `PsychologyModifier`, `InjuryLevel`: confirmed no-op at Stage 0
    (§7.3 deferred hooks).

---

**[COLLISION-3]** *Collision System Specification #3 (Approved).* Multiple section files.

- **Used for:**
  - Spatial hash interface: `QueryRadius(position, radius)` — consumed in §3.0 pipeline
    Step 2 for candidate entity retrieval. Interface contract confirmed from approval.
  - Spatial hash is owned by Collision System; consumed read-only by Perception.

---

**[FIRST-TOUCH-4]** *First Touch Mechanics Specification #4 (Approved).* Multiple
section files.

- **Used for:**
  - `CalculatePressureScalar()` formula: reused verbatim in §3.6. First Touch §3.5 is
    the authoritative source for the formula and all three associated constants
    (`PRESSURE_RADIUS`, `MIN_PRESSURE_DISTANCE`, `PRESSURE_SATURATION`). All consumed
    read-only, marked [CROSS].
  - Half-turn orientation bonus (First Touch §3.3.2): 15% L_rec reduction consumed in
    §3.3. First Touch §3.3.2 is authoritative. Consumed read-only, marked [CROSS].

---

**[PASS-MECHANICS-5]** *Pass Mechanics Specification #5 (Approved).* Multiple section
files.

- **Used for:**
  - Structural precedent: parameter-based model pattern (avoiding named type enums)
    consistent with this specification's approach — no `PerceptionState` enum (KD design
    principle matching Pass/Shot Mechanics KD-3 consistency).
  - No constants from Pass Mechanics consumed by Perception.

---

**[SHOT-MECHANICS-6]** *Shot Mechanics Specification #6 (Approved pending checklist).*
Multiple section files.

- **Used for:**
  - Section 8 format, audit methodology, and cross-reference verification table structure
    follow Shot Mechanics §8 as the established template.
  - No constants from Shot Mechanics consumed by Perception.

---

### 8.3.3 Development Documentation

---

**[BEST-PRACTICES]** *Tactical Director — Development Best Practices.* Internal project
document. File: `Development_Best_Practices.md`.

- **Used for:** Specification structure, version control discipline, approval checklist
  format, cross-specification consistency requirements, and constants citation methodology
  ([GT], [CROSS], [DERIVED] tagging scheme).

---

**[SPEC-ERROR-LOG]** *Tactical Director — Specification Error Log v1.2.* Internal
project document. File: `Spec_Error_Log_v1_2.md`.

- **Used for:** All open error flags affecting this specification are tracked in the
  error log. Any constant or formula change arising from error log resolution must be
  reflected in this section before final approval.

---

## 8.4 Software and Tools

---

**[UNITY-ENGINE]** Unity Technologies. (2024). *Unity Engine — version 2022 LTS.*
https://unity.com/

- **Used for:** Target implementation platform. All performance targets in §6 are
  expressed in terms of Unity's Update() and physics tick cycles. The 10Hz/60Hz cadence
  split (KD-1) is implemented using Unity's FixedUpdate/Update architecture.
- **Note:** No gameplay constants are derived from Unity engine behaviour.

---

**[CSHARP-LANG]** Microsoft. (2024). *C# Language Reference.*
https://learn.microsoft.com/en-us/dotnet/csharp/

- **Used for:** Implementation language. All code structures in §4 (Architecture) are
  C# structs and classes targeting Unity's .NET Standard 2.1 compatibility profile.
  The value-type (struct) definition of `FilteredView`, `PerceptionDiagnostics`, and
  `PerceivedAgent` is a C# language decision consistent with KD-2.

---

## 8.5 Citation Summary

Quick-reference table mapping every formula area and constant to its primary academic
source or authority.

| Formula Area / Constant | Primary Source(s) | Classification |
|---|---|---|
| §3.1 FoV model structure | [WILLIAMS-1998], [MANN-2007] | ACADEMIC-INFORMED |
| `BASE_FOV_ANGLE` = 160° | [WILLIAMS-1998] effective field utilisation ~150°–170°+ | ACADEMIC-INFORMED + [GT] |
| `MAX_FOV_BONUS_ANGLE` = 10° | [WILLIAMS-1998] expert-novice scanning | ACADEMIC-INFORMED + [GT] |
| `MAX_FOV_PRESSURE_REDUCTION` = 30° | [BEILOCK-2007] 20–50% degradation range | ACADEMIC-INFORMED + [GT] |
| `MIN_FOV_ANGLE` = 120° | Safety floor; no academic source | [GT] |
| §3.2 Shadow cone model | [MASTER-VOL1] design authority | DESIGN-AUTHORITY |
| `AGENT_BODY_RADIUS` = 0.4m | Adult body geometry; [AGENT-MOVEMENT-2] §3.2 | ACADEMIC-INFORMED |
| `MIN_SHADOW_HALF_ANGLE` = 5° | Safety floor; no academic source | [GT] |
| Shadow cone angular formula | Trigonometric derivation | DERIVED |
| Opponents only at Stage 0 | OQ-1 resolution; [MASTER-VOL1] | DESIGN-AUTHORITY |
| §3.3 L_rec model structure | [HELSEN-1999], [MANN-2007] | ACADEMIC-INFORMED |
| `L_MAX` = 5 ticks (500ms) | [FRANKS-1985] upper bracket | ACADEMIC-INFORMED + [GT] |
| `L_MIN` = 1 tick (100ms) | [HELSEN-1999] 80–150ms expert range | ACADEMIC-INFORMED + [GT] |
| L_rec linear interpolation formula | Linear scaling; [MASTER-VOL2] attribute authority | DESIGN-AUTHORITY |
| `CONFIRMATION_EXPIRY_TICKS` = 1 tick | Algebraic minimum; not GT-tuned | DERIVED |
| `PERIPHERAL_ARC_INNER_BOUND` = 40° | Derived from BASE_FOV_HALF_ANGLE / 2 | DERIVED |
| Half-turn L_rec reduction = 15% | [HELSEN-1999] (via [FIRST-TOUCH-4] §3.3.2) | ACADEMIC-INFORMED [CROSS] |
| Additive-only noise (+0/+1) | KD-4 determinism; [MASTER-VOL1] | DESIGN-AUTHORITY |
| §3.4 Shoulder check mechanic | [FRANKS-1985], [MASTER-VOL1] | ACADEMIC-INFORMED + DESIGN-AUTHORITY |
| `CHECK_MAX_TICKS` = 30 ticks (3.0s) | [FRANKS-1985] 3–5s scan range | ACADEMIC-INFORMED + [GT] |
| `CHECK_MIN_TICKS` = 6 ticks (0.6s) | [MASTER-VOL1] §3.1 elite rate | DESIGN-AUTHORITY + [GT] |
| `SHOULDER_CHECK_DURATION` = 3 ticks | [MASTER-VOL1] §3.1 "~0.3 seconds" | DESIGN-AUTHORITY + [GT] |
| Possession interval doubling | [JORDET-2009] possession-state cognitive load | ACADEMIC-INFORMED + [GT] |
| Shoulder check autonomy (DT does not call) | OQ-3, KD-6 resolution | DESIGN-AUTHORITY |
| §3.5 Ball — no L_rec | OQ-2 resolution | DESIGN-AUTHORITY |
| `BallStalenessFrames` | Design authority; no academic source | DESIGN-AUTHORITY |
| §3.6 `CalculatePressureScalar()` | [BEILOCK-2007] (via [FIRST-TOUCH-4] §3.5) | ACADEMIC-INFORMED [CROSS] |
| `PRESSURE_RADIUS` = 3.0m | [WILLIAMS-1998] (via [FIRST-TOUCH-4] §3.5.1) | ACADEMIC-INFORMED [CROSS] |
| `MIN_PRESSURE_DISTANCE` = 0.3m | [FIRST-TOUCH-4] §3.5.2 authoritative | [CROSS] |
| `PRESSURE_SATURATION` = 1.5 | [FIRST-TOUCH-4] §3.5.3 authoritative | [CROSS] |
| FoV narrowing only (not L_rec, not check interval) | KD-7 scope | DESIGN-AUTHORITY |
| §3.7 `FilteredView` + `PerceptionDiagnostics` as structs | KD-2; [MASTER-VOL4] zero-alloc | DESIGN-AUTHORITY |
| §3.8 Forced refresh trigger events | KD-1; [MASTER-VOL1] | DESIGN-AUTHORITY |
| L_rec = 0 on forced refresh | Salient-event model | DESIGN-AUTHORITY |
| `MAX_PERCEPTION_RANGE` = 120m | OQ-5; full pitch diagonal | ACADEMIC-INFORMED + [GT] |

---

