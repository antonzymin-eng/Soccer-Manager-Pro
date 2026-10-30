# Goalkeeper Mechanics Specification #11 — Section 7: Future Extensions & Stage 1+ Deferrals

**Created:** May 16, 2026
**Last Updated:** October 29, 2026 (v0.2 — ERR-011-021: §7.3 discipline deferral updated for D-07 and the eight-second rule)
**Version:** 0.2
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

**Statement.** GK fouls outside the box, cards for outside-area
handling (the DFK itself is modelled by §3.1.4), and time-wasting
bookings for repeated eight-second hand-control offences (§3.8.5; a
single offence carries no card) are not modelled at Stage 0. *(Updated
October 29, 2026, ERR-011-021: the former text cited the retired
six-second rule and `GK_HOLD_MAX_TICKS`.)*

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
distribution choices at Stage 0 come from the #21 `GkDistributionPolicy`
through the Match Engine producer (#11 §3.8, ERR-011-016); a risk
model would be a later #21 / engine extension. The *physics* of
distribution remains owned here.

**Candidate Stage.** Stage 1+ #21 / Match Engine extension.

**Rationale.** KD-6: distribution-side weighting is a policy concern
outside #11; the geometry remains #11-owned (KD-16).

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
| 0.2 | October 29, 2026 | W8 C spec application / ERR-011-021 | §7.3 no longer cites the retired six-second rule; outside-area handling is modelled (D-07, §3.1.4) and only its cards remain deferred; repeated eight-second offences' bookings deferred. Review round 4: §7.9 no longer says Decision Tree #8 decides distribution; at Stage 0 the choice comes from the #21 `GkDistributionPolicy` through the Match Engine producer (ERR-011-016). | W8 C contract v0.5 |
