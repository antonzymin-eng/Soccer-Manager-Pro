# Goalkeeper Mechanics Specification #11 — Section 7: Future Extensions & Stage 1+ Deferrals

**Created:** May 16, 2026
**Version:** 0.1
**Status:** DRAFT
**Purpose:** Enumerate the Stage 1+ deferrals and future-extension
candidates that #11 carves out of Stage 0 scope. Each deferral
identifies the rationale and the candidate Stage.

---

## 7.1 Concussion / injury accumulation (KD-17)

**Statement.** No injury accumulation model exists in the 20-spec
set. Saves involving direct head contact, body collisions during
dives, or 1v1 smothers do not contribute to an injury counter at
Stage 0.

**Candidate Stage.** Stage 1+ Medical/Injury spec (no current slot).

**Rationale.** Out of Stage 0 scope; same posture as Heading #10
KD-15.

## 7.2 Substitution dynamics

**Statement.** When a GK is replaced (red card; injury; tactical),
the replacement agent role assignment is out of scope.

**Candidate Stage.** Stage 1+ match-management spec.

**Rationale.** Roster-level concern; not a goalkeeper mechanic.

## 7.3 Yellow / red card discipline

**Statement.** GK fouls outside the box, deliberate handball
outside the box, and time-wasting bookings (related to the 6-second
rule per `GK_HOLD_MAX_TICKS`) are not modelled at Stage 0.

**Candidate Stage.** Stage 1+ Discipline spec.

**Rationale.** Out of Stage 0 scope; KD-17.

## 7.4 Dive-attribute scaling of `DIVE_PHASE_DURATION_MS`

**Statement.** `DIVE_PHASE_DURATION_MS` is a flat `[GT]` at Stage 0
per §3.3.3; attribute-driven duration (e.g. shorter dives for
high-`Aerial` keepers due to faster launch acceleration) is
deferred.

**Candidate Stage.** Stage 1+ when validation data justifies
attribute-driven duration.

**Rationale.** Attribute scaling without empirical anchor introduces
tuning surface that cannot be validated at Stage 0.

## 7.5 AM #2 native Z kinematics

**Statement.** When AM #2 publishes a vertical-axis kinematic
surface in §3.6 (Stage 1+ extension), Spec #11 §3.3 retires the
synthetic dive trajectory and reads apex-frame `agentZ` from AM #2
instead. A `GroundedReason.DIVING_SAVE` enum value lands in AM #2
at that time as a non-behavioral patch.

**Candidate Stage.** Stage 1+ AM #2 §3.6 native Z extension.

**Rationale.** KD-12; mirrors Heading #10 KD-18 and §7.8 retirement
schedule. The Stage 0 synthetic trajectory has no `[FIXED]`
constants and no special-case API surface; retirement is a
substitution of read source.

## 7.6 GK-specific footwork (set-position shuffle animation granularity)

**Statement.** Sub-60 Hz footwork granularity (small lateral
adjustments during the set-piece preparation window; finer
ball-position-aware shuffle than the 10 Hz tactical loop currently
specifies) is not modelled.

**Candidate Stage.** Stage 2+ animation spec.

**Rationale.** Animation-driven; not a physics or AI concern at
Stage 0.

## 7.7 Penalty-saving specialism

**Statement.** "Save-side" diving cues (shooter-eye-tracking analog
that biases the dive direction commit) are not modelled. The
penalty-shot pipeline at Stage 0 uses the same §3.2 reaction
formula as open-play shots with the early-tolerance edge already
modelling early commits.

**Candidate Stage.** Stage 1+ when Perception System #7 grows
finer-grain attention modelling.

**Rationale.** Requires #7 extension; not unilaterally addable in
#11.

## 7.8 Sweeper-keeper tactical role

**Statement.** Extreme outfield-style positioning under a high
defensive line (sweeper-keeper) is not modelled at Stage 0; the
GK's reactive radius is bounded by `GK_REACTIVE_RADIUS_M = 1.5 m`
around the #12 baseline.

**Candidate Stage.** Stage 1+ Tactical-Identity spec.

**Rationale.** Tactical-identity feature; requires #12 extension to
support sweeper-keeper baselines.

## 7.9 Distribution-side risk model

**Statement.** Short-pass-under-press vs. long-clear-to-channel
distribution choices are decided by Decision Tree #8 GK branches
at Stage 0; the *physics* of distribution remains owned here even
when the risk model elaborates in #8.

**Candidate Stage.** Stage 1+ Decision Tree #8 extension.

**Rationale.** KD-6: distribution-side weighting is a #8 concern;
the geometry remains #11-owned (KD-16).

## 7.10 Multi-attacker 1v1 (2v1 break)

**Statement.** A 2v1 break (two attackers vs. one defender + GK)
uses the §3.6 cross-claim duel mechanism as a Stage 0 approximation;
a dedicated 2v1 decision branch is not modelled.

**Candidate Stage.** Stage 1+ tactical AI extension.

**Rationale.** §3.6 approximation is functionally adequate at
Stage 0; deferred elaboration when validation data justifies a
dedicated branch.

---

## 7.11 Version History

| Version | Date | Author | Notes | Reviewer |
|---------|------|--------|-------|----------|
| 0.1 | May 16, 2026 | initial draft | First v0.1 from outline v1.2; 10 deferrals catalogued with rationale and candidate Stage | self-pass-1 in `adversarial-review-section-files-v1.md` |
