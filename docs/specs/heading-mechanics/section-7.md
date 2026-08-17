# Heading Mechanics Specification #10 — Section 7: Future Extensions & Stage 1+ Deferrals

**Created:** May 16, 2026
**Version:** 0.2
**Status:** DRAFT
**Purpose:** Catalogue Stage 1+ deferrals tied to specific KDs, plus
forward-looking interface migrations that activate when upstream
specs grow surfaces #10 currently synthesises locally.

Each deferral lists: ID, statement, rationale, candidate stage.

---

## 7.1 Weak-Aerial-Side Asymmetry — Stage 1+

**Statement:** No `WeakAerialSide` attribute or asymmetric
left-vs-right aerial penalty is introduced at Stage 0. KD-14.

**Rationale:** Validation data establishing the magnitude (and
even the existence) of a left-vs-right aerial asymmetry per player
is unavailable to the project at Stage 0. Introducing the attribute
pre-data would force a `[EST]` value with no upgrade path. Once
validation data exists, an analogous `WeakAerialSide` attribute
mirroring `WeakFootRating` (AM #2 §3.5.6) can be added; #10 §3.4
`headingAttrScale` is the natural integration point.

---

## 7.2 Concussion / Injury Accumulation — Stage 1+ (Medical Spec)

**Statement:** No injury, concussion, or cumulative-impact modelling
is included. KD-15.

**Rationale:** No injury / medical spec exists in the 20-spec set;
when a future Medical spec is authored at Stage 1+, the
`HeaderExecutedEvent` payload provides the per-contact impulse data
that a cumulative-impact model would consume.

---

## 7.3 Bicycle-Kick / Overhead-Kick Distinct Kinematics — Stage 1+

**Statement:** Stage 0 routes overhead-kick head contacts through
the #10 pipeline using posture data from Agent Movement #2, but
does not introduce a distinct overhead-kick formula branch.

**Rationale:** KD-1 prohibits new kinematic enums in the physics
layer. Overhead-kick head contacts emerge from the existing
parameter-based contact model (`contactPointIntent`,
`headVelocityVector`). A Stage 1+ refinement may introduce
posture-aware contact-quality modifiers if validation shows
significantly different feel.

---

## 7.4 Headed-Pass Intent Classification — Stage 1+

**Statement:** Telemetry classification of header outcomes into
"clearance / flick-on / knock-down" labels is not a #10 publication
at Stage 0.

**Rationale:** Per KD-1 and #6 KD-6, named outcome labels are
downstream telemetry, not physics inputs. A Stage 1+ telemetry
classifier consuming `HeaderExecutedEvent` can attach labels
without modifying #10.

---

## 7.5 Set-Piece Kick Generation — Stage 1+

**Statement:** Free-kick / corner-kick *delivery* (the kick itself)
remains deferred to a future set-piece spec at Stage 1+ per Shot
Mechanics #6 §1.2.

**Rationale:** KD-13 establishes that the *header off* a set-piece
delivery is in #10 scope at Stage 0 (mechanically identical to an
open-play header); the kick itself is not.

---

## 7.6 Aerial-Attribute Introduction to AM #2 — Stage 1+

**Statement:** `JumpReach` is `[DERIVED]` from existing AM #2
attributes (Strength, Balance, Heading) per KD-4 at Stage 0. A
dedicated `JumpReach` or `Aerial` `PlayerAttribute` may be added
to AM #2 at Stage 1+.

**Rationale:** The `[DERIVED]` formula was chosen to preserve AM
#2 APPROVED status. If Stage 0 / Stage 1 validation shows that the
derived formula cannot match observed match-level aerial
distributions, an explicit attribute would be the natural upgrade
path. Until then, the derivation is sufficient.

---

## 7.7 Concession-Time / Pressure / Referee-Decision Interaction — Stage 2+

**Statement:** Match-state coupling (e.g., headers under pressure
late in the match, referee-decision modifiers) is deferred to a
Stage 2+ match-state spec.

**Rationale:** No match-state spec exists in the 20-spec set; #10
publishes raw physics + skill outputs at Stage 0. Match-state
modifiers, if introduced later, can be applied as input modulations
to `PowerIntent` / `Heading_norm` upstream of #10's formula path.

---

## 7.8 AM #2 Native Z Kinematics Retirement — Stage 1+

**Statement:** When Agent Movement #2 grows native Z (vertical)
kinematics at Stage 1+, retire the #10-owned synthetic jump
trajectory (§3.3 / KD-18) and read apex-frame `agentZ` from AM #2
instead, adding the anatomical head-above-COM offset.

**Rationale:** KD-18 made #10 the temporary owner of Stage 0
vertical kinematics because AM #2 §3.6 explicitly defers Z>0
motion to Stage 1+. Once AM #2 publishes a Z-axis surface, #10's
synthetic trajectory becomes redundant and should retire to avoid
two competing sources of truth.

**Migration interface:** Replace the parabolic interpolation in
§3.3 with a `BallPhysics`-style positional query against the AM
#2 vertical state; the surrounding §3 algorithms remain unchanged
because they consume `agentHeadZ(frame)` as a scalar input.

---

## 7.9 AM #2 Head-Segment Skeletal API — Stage 1+

**Statement:** When AM #2 publishes per-segment skeletal data
(specifically, head-segment angular velocity), retire the
#10-owned `headAngularVelocity` derivation in §3.6 and read the
quantity directly.

**Rationale:** Pass-1 H-3 resolved the absence of a head-segment
API in AM #2 by deriving `headAngularVelocity` from
`agent.facing` finite-difference + projected neck rotation. This
is a Stage 0 approximation. A skeletal API would yield more
accurate spin-transfer outputs.

---

## 7.10 Dedicated Jump-Timing Attribute — Stage 1+

**Statement:** Introduce a new `PlayerAttribute` separating jump
*reach* from jump *timing* (anticipation of apex alignment) when
validation data warrants the split.

**Rationale:** KD-4 currently folds jump-timing skill into the
`Heading` term of the `JumpReach` formula (`JUMP_REACH_K_HEADING`
coefficient). If Stage 1+ validation shows that high-`Heading`
players cluster their apex-alignment but not their reach, or vice
versa, separating the attributes will improve fidelity. Until then,
the combined term is parsimonious.

---

## 7.11 Glancing / Direct Telemetry Classifier — Stage 1+

**Statement:** A downstream telemetry classifier may distinguish
glancing headers from direct headers using a contact-angle
threshold. The dead `GLANCING_ANGLE_THRESHOLD_RAD` constant
removed from §3.1 in v1.1 (pass-1 L-3) becomes relevant here.

**Rationale:** Per KD-1, header outcomes are emergent from the
parameter-based contact model; named outcome labels are downstream
telemetry. The classifier consumes `HeaderExecutedEvent` fields
(`contactPoint`, `outgoingVelocity`, incoming geometry) and emits
a label; no #10 physics-layer change is required.

---

## 7.12 Head-Velocity Launch-Angle Modulation — Stage 1+

**Statement:** Stage 0 `headerLaunchAngle` is pure reflection
geometry off the head contact point (§3.5). A
`LAUNCH_ANGLE_HEAD_VELOCITY_COEFF [GT]` term coupling head angular
velocity to launch-angle deflection may be added at Stage 1+ when
biomechanical validation data warrants.

**Rationale:** v0.2 H-5 removed an untagged `ANGULAR_COEFF` from
the §3.5 pseudocode that violated KD-11. The geometric effect of
head angular velocity on launch direction (as distinct from
outgoing spin, which §3.6 already captures) is plausible but
uncalibrated; introducing a free coefficient pre-data would force
an `[EST]` value with no defensible upgrade path. Once validation
data exists, the natural integration point is the §3.5
`reflectedDir` → `adjustedDir` rotation.

---

## 7.13 Version History

| Version | Date         | Author  | Notes                                                  | Reviewer |
|---------|--------------|---------|--------------------------------------------------------|----------|
| 0.1     | May 16, 2026 | drafter | Initial section draft from outline-detailed v1.1       | pending  |
| 0.2     | May 16, 2026 | drafter | v0.2 PASS-1 fix pass: added §7.12 head-velocity launch-angle modulation deferral (H-5 rationale).                                               | pending  |
