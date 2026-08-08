# Goalkeeper Mechanics Specification #11 — Section 1: Purpose & Scope

**Created:** May 16, 2026
**Version:** 0.3
**Status:** DRAFT
**Purpose:** Establish the scope of the Goalkeeper Mechanics
specification, the out-of-scope items, the twenty-one pre-committed
Key Design Decisions (KDs) that govern the entire spec, and the
upstream/downstream integration contracts. Subsequent section files
(`section-2.md` through `appendices.md`) build on the KDs declared
here.

---

## 1.1 What This Specification Covers

Goalkeeper Mechanics #11 governs every behavior unique to the
goalkeeper agent role (`PlayerRole.Goalkeeper`; one agent per side).
From the instant Decision Tree #8 commits the GK to a save, claim,
rush, or distribution intent until the ball leaves the GK's
possession, this spec owns the state machine, the reaction pipeline,
the dive kinematics, the hand-ball contact-quality computation, the
contested-duel resolution for hand contacts, the failed-attempt
pipeline, the rush dispatch logic, the distribution release
geometry, and the telemetry surface.

Governance areas:

- **GK state machine** (§3.1) — eleven states governing every
  transition from resting through dive, claim, rush, distribution,
  recovery.
- **Shot reaction pipeline** (§3.2) — consumes `ShotExecutedEvent`
  from Shot Mechanics #6 and produces `requiredReactionMs`,
  `reactionWindowAchieved`, and a dive-direction commit at the 10 Hz
  tactical tick following shot detection.
- **Dive kinematics** (§3.3) — at Stage 0, owns the synthetic
  XY+Z dive trajectory per KD-12. AM #2 ground kinematics consumed
  read-only.
- **Positioning AI #12 consumer contract** (§3.3.0) — KD-13.
- **Handling-quality scalar computation** (§3.5) — produces a
  continuous scalar in `[0, 1]` from contact-point error, ball
  speed, attributes, fatigue, and reaction quality.
- **Cross-claim & aerial duel resolution** (§3.6) — consumes
  Collision System #3 contact data; resolves multi-agent contests
  with deterministic tie-break; head-vs-hand routing per KD-14.
- **Rush / sweep dispatch** (§3.7) — KD-15 abort policy.
- **Distribution generation** (§3.8) — KD-6 / KD-16; emits Pass
  Mechanics #5 `PassIntent`-equivalent.
- **Failed-save handling** (§3.9) — emits a structured
  `SaveAttemptedEvent` with `failureCause` without modifying ball
  state.
- **Telemetry surface** (§2.4) — counters / gauges / histograms
  routed to the trace pipeline.

Applicability: every gameplay action initiated by the GK agent role
(one agent per side with `PlayerRole.Goalkeeper`); every ball
contact in which the GK is the contacting agent AND the contact
occurs inside the penalty area OR is a save attempt initiated from
inside the penalty area (boundary per Laws of the Game; outside-box
GK contacts route to outfield pipelines unless KD-14 hand-contact
routing applies).

Pointer to §1.2 (out-of-scope items), §1.3 (KDs), §1.4 (dependency
and integration contract tables).

---

## 1.2 What Is Out of Scope

- **Head-ball contacts by the GK** → Heading Mechanics #10 (KD-4).
  Spec #11 supplies the `HeaderIntent` payload via Decision Tree #8
  GK branches and consumes the resulting `HeaderExecutedEvent`; it
  does not redefine head-ball physics.
- **GK *resting* baseline position** (formation slot) → Positioning
  AI #12 §3.3.3 (KD-3). Spec #11 owns only reactive position.
- **Pass / kick trajectory generation** (the ball-flight physics of
  distribution) → Pass Mechanics #5 (KD-6, KD-16). Spec #11 owns
  the release-point geometry and intent payload; #5 owns the
  trajectory.
- **Defensive wall positioning at free-kicks** → Defensive AI #14
  (NOT STARTED) per KD-19.
- **Penalty kick taker dynamics** → Shot Mechanics #6 + Decision
  Tree #8.
- **Yellow / red card discipline, injury accumulation** → Stage 1+
  (KD-17; no current spec slot).
- **Substitution logic** (when a GK is replaced) → Stage 1+
  match-management spec.
- **Sweeper-keeper extreme outfield-style positioning** → Stage 1+
  Tactical-Identity spec (§7.8).

---

## 1.3 Key Design Decisions

Twenty-one Key Design Decisions are pre-committed. Each is cited by
FR rows in §2 and by the relevant formula subsections of §3.

### KD-1 — Parameter-based save model (no save-type enum)

**Statement.** No `SaveType` / `SaveClass` / `SaveOutcome` enum at
any layer. Decision Tree #8 GK branches supply continuous physical
intent (`targetHand`, `deflectionVector`, `clutchFirmness`); physics
produces vectors; named outcome labels (`Caught` / `Parried` /
`Deflected` / `Spilled`) are post-computation telemetry emitted from
`HandlingQualityScalar` bands.

**Rationale.** Same trap closed for #5 (`KickType`), #6 (`ShotType`),
#10 (`HeaderType`). An outcome enum surfaces caller intent into
physics and prevents continuous tuning.

**Consequence if violated.** Re-introduces phantom enum proliferation
(ERR-005 class) and prevents Stage 1+ tuning of outcome thresholds
without touching physics.

### KD-2 — Continuous reaction-window scalar

**Statement.** Reaction quality is a continuous scalar in `[0, 1]`
derived from a signed offset `elapsedSinceShotMs - requiredReactionMs`,
where `requiredReactionMs` is computed per §3.2 from Perception
System #7 base latency, the `Reflexes` attribute, a ball-speed
factor, and the GK's pre-shot anticipation score from Decision Tree
#8. Named labels (`Reflexive` / `Standard` / `Sluggish`) are
post-computation telemetry, NEVER branched on by the physics
formula.

**Rationale.** Mirrors the Heading #10 KD-2 fix for the same trap.

**Consequence if violated.** Step discontinuities in save quality
near label boundaries; tuning failures at the band edges.

### KD-3 — Boundary with Positioning AI #12

**Statement.** Spec #12 owns the GK *resting* baseline slot
(formation-driven; reactive only to slow ball motion in open play).
Spec #11 owns everything reactive: set-position micro-shuffle when
a shooter winds up; near/far-post selection during set-piece
preparation; angle-narrowing during 1v1; cross-claim positioning;
sweep/rush dispatch; recovery-to-line after a save. The boundary is
the `Resting` state in §3.1.

**Rationale.** Avoids dual ownership of GK position. KD-13 spells out
the ratification protocol for the three `[EST]` constants in #12.

**Consequence if violated.** #12 GK constants oscillate between
`[EST]` and `[GT]` as ownership shifts; phantom-interface risk
(ERR-001 / ERR-004 class).

### KD-4 — GK head contacts execute the #10 pipeline

**Statement.** Spec #11 does NOT redefine head-ball physics. Per
Heading Mechanics #10 KD-7, GK head contacts route through #10.
Spec #11 supplies the `HeaderIntent` payload via #8 GK branches and
consumes the resulting `HeaderExecutedEvent`.

**Rationale.** Single-spec ownership of head physics is a
project-wide invariant.

**Consequence if violated.** Two parallel head-physics
implementations; tuning drift.

### KD-5 — Save physics consumes Collision System #3 contact data; does not redefine it

**Statement.** Hand-ball, body-ball, and chest-ball contacts read
contact normal, relative velocity, and impulse budget from #3
contact events via `ICollisionEventConsumer` (#3 §3.4.2). Spec #11
layers a GK-specific resolution on top: `HandlingQualityScalar`
modulates the effective restitution and grip; `clutchFirmness` from
#8 caps the bounce energy retained.

**Rationale.** No #3 revision required; mirrors #10 KD-5 precedent.

**Consequence if violated.** Re-derives contact normals incorrectly;
diverges from outfield contact resolution.

### KD-6 — Distribution emits Pass Mechanics #5 intent (no #5 amendment)

**Statement.** Throw / roll / kick distribution choices produce a
Pass Mechanics #5 `PassIntent`-equivalent payload. Pass Mechanics
#5 consumes this via its existing intent surface; no Pass-side spec
change. Distribution weighting (short to centre-back vs. long to
striker) lives in Decision Tree #8 GK branches, NOT in #11.

**Rationale.** Insulates #5 from amendment; preserves the
parameter-based parameter-passing protocol.

**Consequence if violated.** #5 grows a GK-specific intent type;
re-review needed.

### KD-7 — All randomness via `DeterministicRngService`

**Statement.** Four draw sites at Stage 0:
`DRAW_SITE_HANDLING_NOISE` (§3.5 handling-scale Gaussian),
`DRAW_SITE_HANDLING_POINT_NOISE` (§3.5 contact-point Gaussian),
`DRAW_SITE_DIVE_TIMING_JITTER` (§3.3 dive launch timing jitter),
`DRAW_SITE_CROSS_CLAIM_TIEBREAK` (§3.6 near-tie perturbation).
Iteration order over multi-attacker scenarios follows #16 §3.2
entity ordering. `DOMAIN_TAG_GOALKEEPER = 0x1D` `[CROSS: #16 §3.4]`
— ERR-011-001 resolved May 18, 2026. Positioning AI #12 reached
`APPROVED` first and claimed `0x17` per first-to-`APPROVED` precedent
(KD-7 collision-management policy); #11 was reallocated to `0x1D`
in #16 §3.4 v1.0.5. The allocation is a pure namespace amendment to
APPROVED #16 (no `DETERMINISM_DIGEST_VERSION` bump), following the
precedent set by Heading #10's `0x16` patch (ERR-010-001) and
Event System #17's `0x15` patch (ERR-017-001).

**Rationale.** Single source of randomness ensures deterministic
replay. Domain-tag separation protects digest independence.

**Consequence if violated.** Non-deterministic replay; Stage 5+
cross-platform parity violations.

### KD-8 — Fatigue convention

**Statement.** `0.0 = rested, 1.0 = fatigued` (CLAUDE.md).

**Rationale.** Project-wide invariant; recurring bug class
(Pass Mechanics #5 FR-02 had this inverted at one point).

**Consequence if violated.** Reach reduction inverted across §3.3 /
§3.5 / §3.7.

### KD-9 — Constant-tag policy

**Statement.** Every numeric constant published by Spec #11 carries
exactly one of `[GT] / [EST] / [FIXED] / [DERIVED] / [CROSS] /
[CROSS-PENDING]`. §9 Approval Checklist programmatically verifies
every constant in `section-3.md` and `appendices.md` against this
rule before approval. No magic numbers in formula code.

**Rationale.** CLAUDE.md mandate; recurring audit class.

**Consequence if violated.** Tunings collide with physics-derived
constants; designer cannot trust the catalogue.

### KD-10 — Project-invariants citation block

**Statement.** Spec #11 cites and does not restate the project-wide
invariants: corner-origin coordinates (Ball Physics #1 §1.2);
fatigue `0=rested, 1=fatigued` (CLAUDE.md); tick-rate split (10 Hz
tactical for state-machine transitions and intent selection; 60 Hz
physics for dive kinematics, hand-ball contact resolution,
ball-velocity emission; CLAUDE.md).

**Rationale.** Prevents drift from project invariants by citing,
not duplicating.

**Consequence if violated.** Local restatements diverge over time
from CLAUDE.md.

### KD-11 — Failed-save physics is well-defined

**Statement.** A save attempt that misses the ball entirely
produces: NO `Ball.ApplyKick`; NO `Ball.SetPossessor`; ball
trajectory unchanged; `SaveAttemptedEvent` published with
`failureCause` ∈ {`MissedContact`, `MistimedDive`, `WrongDirection`,
`OutOfReach`, `DisturbedInDuel`}. The GK's state machine transitions
to `Recovering` via §3.1.

**Rationale.** Non-contact is a structural state, not a save-type
label; resolves the outcome-enum trap by structure.

**Consequence if violated.** Ghost saves spuriously perturb ball
state.

### KD-12 — Stage 0 dive kinematics owned by Spec #11

**Statement.** Agent Movement #2 §3.6 defers Z>0 movement to
Stage 1+; dives at Stage 0 traverse both XY (horizontal launch) and
Z (vertical rise / fall during airborne phase). Spec #11 owns the
synthetic dive trajectory: launch velocity from `Strength` and
`DiveLaunchImpulseProfile`; parabolic vertical arc; recovery to
ground state entering `GROUNDED` with
`GroundedReason.DIVING_HEADER` (re-used from Heading #10 KD-18 at
Stage 0; no AM #2 amendment required). Telemetry disambiguation
via `SaveAttemptedEvent.contactBodyPart`. At Stage 1+ when AM #2
grows native Z kinematics, Spec #11's synthetic dive trajectory
retires per §7.5; a `GroundedReason.DIVING_SAVE` enum value lands
in AM #2 at that time as a non-behavioral patch.

**Rationale.** Mirrors Heading #10 KD-18; avoids AM #2 amendment
during Stage 0.

**Consequence if violated.** AM #2 re-review required; cross-spec
cascade.

### KD-13 — Positioning AI #12 ratification protocol

**Statement.** §3.3.0 of this spec publishes a *Consumer Contract
for GK Baseline Position*. This contract is the explicit `[GT]`
ratification event for `GK_DEPTH_M`, `GK_ADVANCE_FACTOR`,
`GK_LATERAL_FACTOR` — when #11 reaches `IN REVIEW`, these constants
in #12 §3.3.3 promote `[EST]` → `[GT]` atomically with #11's status
flip, via a patch revision to #12 §3.3.3 / §6 (v1.0.x). #11 does
NOT redefine the three constants; it only publishes the contract
that authorises their promotion.

**Rationale.** Resolves the chicken-and-egg blocker that prevented
#12 from promoting its GK constants to `[GT]` (see #12 AR-S1-11
fix policy).

**Consequence if violated.** #12 GK constants stay `[EST]`
indefinitely; #12 approval blocked.

### KD-14 — Cross-claim / aerial duel routing

**Statement.** Aerial cross claims by the GK are head contacts iff
the contact body part is head (route to Heading #10 §3.7 per KD-4);
hand contacts iff the contact body part is hand (route through
Spec #11 §3.6). The contact body part is determined by Collision
System #3 contact-event data (Stage 0 approximation: ball-vs-agent-
hand capsule vs. ball-vs-agent-head sphere intersection priority),
NOT by intent.

**Rationale.** Body-part is a physical fact, not an intent choice;
matches the parameter-based-physics invariant.

**Consequence if violated.** Routing depends on intent; #10 / #11
both touch the contact; double-emission.

### KD-15 — Rush / sweep abort policy

**Statement.** A goalkeeper rush is committed at the 10 Hz tactical
tick by Decision Tree #8. Once committed, the rush is NOT abortable
on the basis of ball-trajectory changes *during* the rush. EXCEPTION:
if the ball is intercepted by another agent before the GK reaches
it, the rush aborts via
`GoalkeeperRushEvent.abortReason = BallIntercepted` and the GK
recovers. No mid-rush re-targeting.

**Rationale.** Analog of Heading #10 KD-17 intent-staleness policy.

**Consequence if violated.** Visible flicker in rush trajectories;
non-deterministic re-evaluation order.

### KD-16 — Distribution release-point geometry is #11-owned

**Statement.** The geometry of where the ball leaves the GK during
distribution (release height above ground, launch angle range,
windup duration) is owned by Spec #11 §3.8. Pass Mechanics #5
consumes the resulting `PassIntent` and produces the trajectory.

**Rationale.** Geometry depends on GK posture, dive recovery state,
and chosen distribution kind, which only #11 knows. Parallel to
Heading #10 KD-16 spin-transfer ownership.

**Consequence if violated.** #5 grows GK-specific posture knowledge;
re-review needed.

### KD-17 — Concussion / injury / disciplinary modeling deferred to Stage 1+

**Statement.** No injury or card-state system exists in the 20-spec
set. §7 records the deferral pointing forward to a future
Medical/Discipline spec.

**Rationale.** Same posture as Heading #10 KD-15; out of Stage 0
scope.

**Consequence if violated.** Out-of-scope content gates Stage 0
sign-off.

### KD-18 — `ReactionWindowAchieved` is asymmetric

**Statement.** The reaction scalar uses different `[GT]` tolerance
constants for "reacted too early" (anticipation; misdirection risk)
and "reacted too late" (reduced reach). Two distinct `[GT]`
constants: `REACTION_EARLY_TOLERANCE_MS` and
`REACTION_LATE_TOLERANCE_MS` (the latter numerically smaller).

**Rationale.** Real-world reaction asymmetry: early-commit is
penalised through misdirection (the 10 Hz pre-commit happens before
trajectory legibility), late-commit through reach. Mirrors Heading
#10 KD-2 pass-1 H-1 fix.

**Consequence if violated.** Symmetric tolerance produces a
single-degree-of-freedom tuning surface that masks reach loss.

### KD-19 — Set-piece saves IN scope at Stage 0; defensive wall is NOT

**Statement.** Saves from free-kicks and penalties are mechanically
identical to open-play saves because incoming `BallState` is read
uniformly from Ball Physics #1 regardless of how it originated. The
defensive wall (positioning of outfielders) is Defensive AI #14's
concern (NOT STARTED); #11 sees only the resulting shot.

**Rationale.** Mirrors Heading #10 KD-13 cross-vs-header boundary.

**Consequence if violated.** Out-of-scope wall logic creeps into
#11; #14 re-review on every set-piece tuning change.

### KD-20 — `OneVsOne` attribute behavior is closed-form

**Statement.** The `OneVsOne` attribute modulates the
`HandlingQualityScalar` and `ReactionWindowAchieved` formulas via
continuous coefficients (`ONE_VS_ONE_HANDLING_COEFF`,
`ONE_VS_ONE_REACTION_COEFF`) gated on the GK's state machine being
in `OneOnOne` (§3.1). No alternative formula path for 1v1 saves.

**Rationale.** Avoids the branched-physics trap (KD-1 same shape).

**Consequence if violated.** Two parallel save pipelines; doubled
tuning surface.

### KD-21 — Catch-vs-parry banding thresholds are `[GT]`

**Statement.** The thresholds at which `HandlingQualityScalar`
bands into `Caught` / `Parried` / `Deflected` / `Spilled`
telemetry labels are `[GT]` constants (`CATCH_THRESHOLD`,
`PARRY_THRESHOLD`, `DEFLECT_THRESHOLD`). The bands govern TELEMETRY
ONLY — they do not branch physics. The `Caught` vs. `Parried`
boundary is the ONLY band that toggles between two `Ball.*` API
calls (`Ball.SetPossessor` vs. `Ball.ApplyKick`); the other bands
all resolve via `Ball.ApplyKick` and differ only in outgoing-velocity
magnitude and angle.

**Rationale.** Tunable bands separate emission from physics.

**Consequence if violated.** Band shifts require physics re-review.

---

## 1.4 Dependencies and Integration Contracts

### Upstream specs (consumed)

| Spec | Section | Consumed by #11 | Purpose |
|------|---------|-----------------|---------|
| Ball Physics #1 | §1.2 | §1.1 / §3.4 | Coordinate-system origin |
| Ball Physics #1 | §3.1.11.2 | §3.5 / §3.8 / §3.9 | `Ball.ApplyKick(velocity, spin, agentId, matchTime)` |
| Ball Physics #1 | §3.1 possession surface | §3.5 / §3.7 | `Ball.SetPossessor(agentId)`; `BallState.PossessorId` (read in F-08 rush abort) |
| Agent Movement #2 | §3.1.2 | §3.1 / §3.3 | `AgentMovementState`, `GroundedReason` enums |
| Agent Movement #2 | §3.5.1 | §3.1 / §3.3 / §3.7 | `Agent` class XY kinematics |
| Agent Movement #2 | §3.5.6 | §3.2 / §3.5 / §3.7 | `PlayerAttributes` field reads (`Reflexes`, `Handling`, `Aerial`, `OneVsOne`, `Throwing`, `Kicking`, `Strength`, `Balance`, `Composure`, `Pace`) |
| Collision System #3 | §3.4.2 + agent collider geometry | §3.5 / §3.6 / §3.7 | `ICollisionEventConsumer` pattern (KD-5); agent `handCapsule` / `headSphere` colliders consumed by §3.6.1 body-part determination (v0.2 AR-S1-M4) |
| First Touch #4 | §1.2 | §1.2 boundary | Head-exception per #10 KD-7; foot save-attempts #11-owned |
| Pass Mechanics #5 | §1.7 / §3 intent surface | §3.8 | `PassIntent` consumer surface (KD-6) |
| Shot Mechanics #6 | §4.5 | §3.2 | `ShotExecutedEvent` |
| Shot Mechanics #6 | §1.3 KD-6 | §3.6 / KD-4 | Body-part discriminator authority |
| Perception System #7 | §3 visibility latency | §3.2 | `PERCEPTION_BASE_LATENCY_MS` consumption |
| Decision Tree #8 | §1.7 intent surface | §3.1 / §3.2 / §3.7 / §3.8 | GK-branch intent vocabulary extension (`SaveIntent`, `ClaimIntent`, `DistributeIntent`, `RushIntent`) |
| Heading Mechanics #10 | §3.7 | §3.6 | Contested-duel mechanism for head contacts (KD-14) |
| Heading Mechanics #10 | KD-7 | §1.1 / KD-4 | GK head-contact ownership inversion |
| Deterministic Simulation #16 | §3.2 | §3.6 | Entity iteration order |
| Deterministic Simulation #16 | §3.4 | §3.4 / §4.4 | `DOMAIN_TAG` catalogue (pending `0x17` allocation per KD-7 / ERR-011-001) |
| Deterministic Simulation #16 | §4.1 / §4.5 | §3.3 / §3.5 / §3.6 / §4.4 | RNG service + draw-site registry |
| Event System #17 | §3.2.1 | §3.9 / §4.3 | Publish API surface |

**Tractability cites (not direct dependencies):** Performance
Optimization #18 (§6 ratify-not-override, Appendix F.0 channel
registry — consumed by §6 of this spec); Testing Strategy #19 /
Code Standards #20 (§5 / §9).

### Downstream specs (consumers; interface declared here, not negotiated)

| Spec | Status | Consumer surface |
|------|--------|------------------|
| Positioning AI #12 | IN REVIEW | KD-13 ratification of `GK_DEPTH_M` / `GK_ADVANCE_FACTOR` / `GK_LATERAL_FACTOR` (`[EST]` → `[GT]` atomic with #11 IN REVIEW) |
| Pressing AI #13 | NOT STARTED | Consumes `DistributionExecutedEvent`; no per-call interface |
| Defensive AI #14 | NOT STARTED | Consumes aggregate save / claim telemetry; no per-call interface |
| Attacking AI #15 | NOT STARTED | Consumes aggregate save / claim telemetry; no per-call interface |

### Boundary notes

- **Goalkeeper-vs-Heading boundary** (KD-4 / KD-14): head contacts
  route to #10 unchanged; #11 supplies intent payloads via #8.
- **Pass Mechanics #5 amendment-insulation** (KD-6): #11 emits a
  `PassIntent`-shaped payload; no #5 amendment.
- **#12 inverse-dependency note**: #12's `IN REVIEW` status is not a
  gate on #11 drafting; the ratification per KD-13 fires on #11's
  `IN REVIEW` transition.

---

## 1.5 Version History

| Version | Date | Author | Notes | Reviewer |
|---------|------|--------|-------|----------|
| 0.1 | May 16, 2026 | initial draft | First v0.1 from `outline-detailed.md` v1.2; KDs 1–21 reproduced; dependency + downstream tables populated; out-of-scope catalogued | self-pass-1 in `adversarial-review-section-files-v1.md` |
| 0.2 | May 16, 2026 | pass-1 fix pass | AR-S1-M4 (#3 dependency row amended to include agent collider geometry) | self-pass-2 self-critique on v0.2 yields no further findings |
| 0.3 | May 18, 2026 | AI agent (adversarial-specs-review-run2-AFrm4) | FAIL-4 fix (A-03): KD-7 block updated — `DOMAIN_TAG_GOALKEEPER = 0x1D [CROSS: #16 §3.4]`; ERR-011-001 resolved; collision-management policy prose updated to reflect final allocation outcome. |
