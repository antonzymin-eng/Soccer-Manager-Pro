# Goalkeeper Mechanics Specification #11 — Detailed Outline

**Purpose:** Section-by-section draft plan for Goalkeeper Mechanics #11.
For every subsection: the rules / formulas / data it will publish, the
upstream citations it will carry, and the cross-references it will
emit. Detailed enough that section files (`section-1.md` …
`section-9-approval-checklist.md`, `appendices.md`) can be drafted
mechanically from this document, with no further outlining work
required.

This document is **not normative** — FR text, formulas, and constant
values land in the section files. Detailed outline records intent,
provenance, and structural mapping only.

**Created:** May 16, 2026, late evening
**Version:** 1.2
**Status:** DRAFT — supersedes `outline.md` v0.1 (May 6, 2026); resolves
all 13 findings of the May 6, 2026 adversarial review attached to that
file. v1.1 additionally resolves all 18 findings of
`outline-detailed-pass-1-review.md` (4 H / 8 M / 6 L). v1.2 resolves
the 5 follow-up findings of `outline-detailed-pass-2-review.md`
(0 H / 2 M / 3 L) — pass-2 returns no remaining issues.
**Specification Number:** 11 of 20 (Stage 0, Priority 3)
**Estimated Effort:** ~32 hours (section files), ~6 hours (pass-1
adversarial critique), ~4 hours (pass-1 fix cycle), ~2 hours
(pass-2 critique + fix).
**Companion documents:** `outline.md` (high-level v0.1 with
adversarial-review appendix — retained for history; do not edit).

---

## Dependencies (direct, all APPROVED unless tagged)

- **Ball Physics #1** — incoming `BallState` (post-shot trajectory);
  output surface `Ball.ApplyKick(velocity, spin, agentId, matchTime)`
  for parries / punches / distribution kicks (§3.1.11.2); coordinate-
  system authority (§1.2).
- **Agent Movement #2** — GK XY kinematics (`Agent` instance per
  §3.5.1); `PlayerAttributes` field reads
  (`Reflexes`, `Handling`, `Aerial`, `OneVsOne`, `Throwing`,
  `Kicking`, `Strength`, `Balance`, `Composure`) via §3.5.6 struct
  surface; `AgentMovementState` / `GroundedReason` enums (§3.1.2).
  AM #2 §3.6 defers Z>0 to Stage 1+; dive vertical-axis kinematics
  at Stage 0 are owned by Spec #11 (KD-12, mirrors Heading #10
  KD-18).
- **Collision System #3** — hand-on-ball contact resolution
  (contact normal, relative velocity, impulse budget); GK-body
  collisions during dives and 1v1 sweeps; `ICollisionEventConsumer`
  pattern per #3 §3.4.2.
- **First Touch #4** — boundary partner: head contacts by the GK
  route to Heading #10 per #10 KD-7 (not #4); foot/leg/torso GK
  contacts that are NOT save attempts (e.g. an outfield-style
  touch when the GK is outside the box) route to #4. Save-attempt
  contacts on the body route to #11 §3 regardless of body part.
- **Pass Mechanics #5** — consumed on the OUTPUT side of distribution
  (§3.8); GK distribution kicks produce a `PassIntent`-equivalent
  payload that Pass Mechanics consumes via its existing intent
  surface. No #5 amendment required (KD-9).
- **Shot Mechanics #6** — `ShotExecutedEvent` (§4.5 / §2.4.3) is the
  sole upstream interface for shot data; KD-6 body-part routing
  authority establishing that ALL head saves route to Heading #10.
- **Perception System #7** — visibility-cone latency model
  (`PERCEPTION_BASE_LATENCY_MS` + GK-specific `Reflexes` modifier);
  ball trajectory observation accuracy ('seeing' the shot) governs
  the reaction-time floor used in §3.2.
- **Decision Tree #8** — intent surface: `SaveIntent`,
  `ClaimIntent`, `RushIntent`, `DistributeIntent`; GK-specific
  Decision Tree branches supply the physical intent parameters
  (target hand, target body-part, throw / roll / kick selection,
  receiver tactical anchor). #8 §1.7 intent-surface anchor pinned
  during `section-1.md` authoring.
- **Heading Mechanics #10** — GK head-contact ownership inversion:
  GK head contacts execute the #10 pipeline (KD-7 there), NOT a
  Spec #11 head-specific code path. Spec #11 supplies the
  GK-specific intent parameters via Decision Tree #8 branches and
  inherits #10's physics layer unchanged (KD-4 here).
- **Positioning AI #12 (IN REVIEW)** — produces the GK *resting*
  baseline slot (§3.3.3 in #12: `GK_DEPTH_M`, `GK_ADVANCE_FACTOR`,
  `GK_LATERAL_FACTOR` currently `[EST]` pending this spec).
  Spec #11 publishes the consumer contract that promotes these to
  `[GT]` per #12 AR-S1-11 fix policy. Boundary: #12 owns resting
  position only; Spec #11 owns shot-reactive / set / cross / 1v1 /
  recovery micro-position (KD-3).
- **Deterministic Simulation #16** — RNG governance
  (`DeterministicRngService`, §4.1 / §4.5); iteration-order
  discipline (§3.2); `DOMAIN_TAG` allocation
  (`DOMAIN_TAG_GOALKEEPER = 0x17` requested per ERR-011-001 back-
  prop; collision-management policy with open ERR-012-001 block
  proposal recorded in OPEN-ITEMS).
- **Event System #17** — publish API for `SaveAttemptedEvent`,
  `BallClaimedEvent`, `DistributionExecutedEvent`,
  `GoalkeeperRushEvent`, plus #6's `ShotExecutedEvent` consumption.

**Tractability cites (not direct dependencies; named here for
context only, not consumed at any interface):**
- Performance Optimization #18 — §3.10 trace channel registry and
  §6 ratify-not-override authority; consumed for performance
  surfaces in §6, not for behavioral specification.
- Testing Strategy #19, Code Standards #20 — consumed by §5 / §9
  for test framework and constant-tag verification gates.

**Downstream (consumers; specs NOT STARTED or IN REVIEW —
interface declared here, not negotiated):**
- Positioning AI #12 (IN REVIEW) — GK constants `[EST]` → `[GT]`
  promotion governed by interface here (KD-3).
- Defensive AI #14, Attacking AI #15 (NOT STARTED) — consume
  aggregate save / claim statistics; no per-call interface.
- Pressing AI #13 (NOT STARTED) — consumes
  `DistributionExecutedEvent` to trigger counter-press; no per-call
  interface beyond the event.

---

## EXECUTIVE SUMMARY

Goalkeeper Mechanics governs every behavior unique to the
goalkeeper: shot reactions (dives, parries, catches, deflections),
high-ball claims (catches, punches), 1v1 confrontations
(narrowing the angle, smother), area sweeping (rushes), recovery
to set position, and post-possession distribution (throw / roll /
kick). The single GK on each side runs this pipeline on top of the
normal agent pipeline; nothing in #1–#10 / #12 is overridden.

The core model:

```
SaveResult = f(SaveIntent, IncomingBallState, GKHandReachGeometry)
           × ReactionWindowAchieved
           × HandlingQualityScalar
           × DiveBodyMechanics
           × Fatigue
```

Where:
- `SaveIntent ∈ {SaveIntent | ClaimIntent | DeflectIntent}` — intent
  vocabulary supplied by Decision Tree #8 GK branches; carries
  continuous physical intent fields (target hand, target deflection
  vector, target catch-clutch firmness ∈ [0,1]), NOT a discrete
  save-type enum.
- `IncomingBallState` — `BallState` snapshot at predicted
  hand-contact frame (post-shot trajectory from Ball Physics #1).
- `GKHandReachGeometry` — `[DERIVED]` reach envelope derived from
  attributes (`Aerial`, `Handling`, `Strength`) and dive direction;
  Stage 0 synthetic dive trajectory per KD-12.
- `ReactionWindowAchieved ∈ [0,1]` — continuous scalar derived from
  signed `(elapsedSinceShotMs − requiredReactionMs)`; named labels
  (`Reflexive`, `Standard`, `Sluggish`) are telemetry, NOT gates on
  the formula.
- `HandlingQualityScalar ∈ [0,1]` — continuous quality derived from
  hand-contact-point error (m on the hand surface), incoming
  ball speed, and `Handling` attribute. Named outcome labels
  (`Caught`, `Parried`, `Deflected`, `Spilled`) are telemetry
  emitted FROM the scalar (banded by configurable thresholds),
  NEVER inputs to physics.
- `DiveBodyMechanics` — dive launch impulse, body orientation, hand
  reach at apex, fatigue penalty.
- `Fatigue ∈ [0,1]` — `0 = rested, 1 = fatigued` (CLAUDE.md
  convention; pre-committed in KD-10).

Physical output — post-contact ball velocity, post-contact spin
adjustment, ball-final-state (caught: ball owned by GK; not caught:
new free-ball trajectory) — emerges from these parameters.
Catch / parry / deflect / spill are downstream telemetry labels, NOT
inputs.

**Output interface surface:**

```
Ball.ApplyKick(velocity, spin, agentId, matchTime)        // parry / punch / deflect / distribution kick
Ball.SetPossessor(agentId)                                  // catch (ball-owned by GK)
SaveAttemptedEvent { agentId, matchTime, saveIntent,
                     reactionWindowAchieved, handlingQuality,
                     handlingQualityLabel, incomingBallState,
                     outgoingBallState, contestedDuelId?, ... }
BallClaimedEvent { agentId, matchTime, claimType,
                    contestedDuelId?, releaseSchedule, ... }
DistributionExecutedEvent { agentId, matchTime, deliveryKind,
                             targetReceiverId?, targetPoint,
                             passIntent, ... }
GoalkeeperRushEvent { agentId, matchTime, rushTarget,
                       rushPhase, abortReason? }
```

A save attempt that misses the ball entirely (dive late, dive in
wrong direction) emits `SaveAttemptedEvent` with
`failureCause = MissedContact` and does NOT invoke
`Ball.ApplyKick` or `Ball.SetPossessor`. Ball trajectory is
unchanged.

---

## KEY DESIGN DECISIONS (pre-committed)

Each KD is restated at the top of `section-1.md` §1.3 with rationale
and consequence-if-violated. Pre-committing here removes ambiguity
during drafting.

- **KD-1 — Parameter-based save model (no save-type enum).** No
  `SaveType` / `SaveClass` / `SaveOutcome` enum at any layer.
  Decision Tree #8 GK branches supply continuous physical intent
  (`targetHand`, `deflectionVector`, `clutchFirmness`); physics
  produces vectors; named outcome labels (`Caught`, `Parried`,
  `Deflected`, `Spilled`) are post-computation telemetry emitted
  from `HandlingQualityScalar` bands. Same trap closed for #5
  (`KickType`), #6 (`ShotType`), #10 (`HeaderType`). **Resolves
  adversarial findings 3 (v0.1) and pass-1 H-1.**

- **KD-2 — Continuous reaction-window scalar.** Reaction quality is
  a continuous scalar ∈ [0,1] derived from a signed offset
  `elapsedSinceShotMs - requiredReactionMs`, where
  `requiredReactionMs` is computed per §3.2 from Perception #7
  base latency, `Reflexes` attribute, ball-speed factor, and the
  GK's pre-shot anticipation score from Decision Tree #8. Named
  labels (`Reflexive` / `Standard` / `Sluggish`) are post-
  computation telemetry, NEVER branched on by the physics formula.
  **Resolves adversarial finding 5 (v0.1) and pass-1 M-2.**

- **KD-3 — Boundary with Positioning AI #12.** #12 owns the GK
  *resting* baseline slot (formation-driven; ball-position-aware
  but reactive only to slow ball motion in open play). Spec #11
  owns everything reactive: set-position micro-shuffle when a
  shooter winds up; near/far-post selection during set-piece
  preparation; angle-narrowing during 1v1; cross-claim
  positioning; sweep/rush dispatch; recovery-to-line after a save.
  The boundary is the `GKResting` state in §3.1 — leaving that
  state hands position authority to Spec #11; re-entering it
  returns authority to Spec #12. The `GK_DEPTH_M`,
  `GK_ADVANCE_FACTOR`, `GK_LATERAL_FACTOR` constants currently
  `[EST]` in #12 §3.3.3 are promoted to `[GT]` *after* this spec
  publishes the consumer contract in §3.3.0 (see KD-13 for the
  exact ratification mechanism). **Resolves adversarial finding 6
  (v0.1) and pass-1 H-2.**

- **KD-4 — GK head contacts execute the #10 pipeline.** This spec
  does NOT redefine head-ball physics. Per Heading Mechanics #10
  KD-7, GK head contacts (punching with the head — rare but
  possible — or any other head save) route through #10. Spec #11
  supplies the `HeaderIntent` payload (via #8 GK branches) and
  consumes the resulting `HeaderExecutedEvent`. **Resolves
  adversarial finding 4 (v0.1) on cross-spec ownership.**

- **KD-5 — Save physics consumes Collision System #3 contact data;
  does not redefine it.** Hand-ball, body-ball, and chest-ball
  contacts read contact normal, relative velocity, and impulse
  budget from #3 contact events. Spec #11 layers a GK-specific
  resolution on top (`HandlingQualityScalar` modulates the
  effective restitution and grip; `clutchFirmness` from #8 caps
  the bounce energy retained). No #3 revision required.
  **Resolves adversarial finding 7 (v0.1) on contact-model
  coupling.**

- **KD-6 — Distribution emits Pass Mechanics #5 intent (no #5
  amendment).** Throw / roll / kick distribution choices produce
  a Pass Mechanics #5 `PassIntent`-equivalent payload (target
  receiver / target point, power intent, spin intent, delivery
  kind). Pass Mechanics #5 consumes this via its existing intent
  surface; no Pass-side spec change. Distribution-specific
  weighting (e.g. risk model — short to centre-back vs. long to
  striker) lives in Decision Tree #8 GK branches, NOT in #11.
  Spec #11 owns the *kinematic* surface of the distribution kick
  (windup time, release point geometry); the trajectory generation
  itself is #5's responsibility. **Resolves adversarial finding 7
  (v0.1) on distribution-model coupling and pass-1 H-3.**

- **KD-7 — All randomness via `DeterministicRngService`.** Three
  draw sites at Stage 0: `DRAW_SITE_HANDLING_NOISE` (§3.4 — handling
  scalar Gaussian perturbation), `DRAW_SITE_DIVE_TIMING_JITTER`
  (§3.3 — dive launch timing jitter), `DRAW_SITE_DEFLECT_DIRECTION`
  (§3.5 — deflection-angle perturbation when handling quality is
  low). Iteration order over multi-attacker scenarios (cross
  claims, 1v1) follows #16 §3.2 entity ordering. New `DOMAIN_TAG`
  allocation (`DOMAIN_TAG_GOALKEEPER = 0x17`) requested from #16
  §3.4 via back-propagation entry filed under `ERR-011-001`. The
  allocation is a pure namespace amendment to APPROVED #16 (no
  `DETERMINISM_DIGEST_VERSION` bump), following the precedent set
  by Heading #10's `0x16` patch and Event System #17's `0x15`
  patch. **Collision-management policy with ERR-012-001 (open;
  Positioning AI #12 proposes block `0x17…0x1C`):** whichever spec
  reaches `APPROVED` first takes `0x17`. If #11 reaches APPROVED
  before #12, the #12 block re-shifts to `0x18…0x1D` (mirrors the
  May 16 #10 / #12 shift). If #12 lands first, #11 shifts to
  `0x1D`. The `[CROSS-PENDING]` tag accommodates either outcome.
  **Resolves adversarial finding 7 (v0.1) on RNG and pass-1 M-1.**

- **KD-8 — Fatigue convention.** `0.0 = rested, 1.0 = fatigued`
  (CLAUDE.md). Pre-committed because Pass Mechanics #5 FR-02 had
  this inverted at one point. **Resolves adversarial finding 8
  (v0.1).**

- **KD-9 — Constant-tag policy.** Every numeric constant published
  by Spec #11 carries exactly one of `[GT] / [EST] / [FIXED] /
  [DERIVED] / [CROSS] / [CROSS-PENDING]`. §9 Approval Checklist
  programmatically verifies every constant in `section-3.md` and
  `appendices.md` against this rule before approval. No magic
  numbers in formula code. **Resolves adversarial finding 9
  (v0.1).**

- **KD-10 — Project-invariants citation block.** Spec #11 cites
  and does not restate: corner-origin coordinates (Ball Physics
  #1 §1.2); fatigue `0=rested, 1=fatigued` (CLAUDE.md); tick-rate
  split (10 Hz tactical for state-machine transitions and intent
  selection; 60 Hz physics for dive kinematics, hand-ball contact
  resolution, ball-velocity emission; CLAUDE.md). Pre-committing
  this block here prevents drift. **Resolves adversarial findings
  8, 11, 12 (v0.1).**

- **KD-11 — Failed-save physics is well-defined.** A save attempt
  that misses the ball entirely (mistimed dive; wrong-direction
  dive; hand outside reach envelope) produces: NO
  `Ball.ApplyKick`; NO `Ball.SetPossessor`; ball trajectory
  unchanged; `SaveAttemptedEvent` published with `failureCause`
  ∈ {`MissedContact`, `MistimedDive`, `WrongDirection`,
  `OutOfReach`}. The GK's state machine transitions to `Recover`
  via §3.1. **Resolves adversarial finding 7 (v0.1) — outcome
  enum trap — by making non-contact a structural state, not a
  save-type label.**

- **KD-12 — Stage 0 dive kinematics owned by Spec #11.** Agent
  Movement #2 §3.6 defers Z>0 movement to Stage 1+; dives at
  Stage 0 traverse both XY (horizontal launch) and Z (vertical
  rise / fall during airborne phase). Spec #11 owns the synthetic
  dive trajectory: launch velocity from `Strength` and
  `DiveLaunchImpulseProfile`; parabolic vertical arc; recovery to
  ground state (entering `GROUNDED` with `GroundedReason.DIVING_SAVE`
  per AM #2 §3.1.2 — note: this enum value will need to be added
  to AM #2 or, alternatively, Spec #11 uses the existing
  `GroundedReason.DIVING_HEADER` value because the AM #2 §3.1.2
  enum already exposes it via Heading #10's KD-18 reuse; see KD-12
  amendment in §1.3). At Stage 1+ when AM #2 grows native Z
  kinematics, Spec #11's synthetic dive trajectory retires; see
  §7.5 deferral. Mirrors Heading #10 KD-18 — no AM #2 amendment
  required at Stage 0. **Resolves pass-1 H-4.**

- **KD-13 — Positioning AI #12 ratification protocol.** §3.3.0 of
  this spec publishes a *Consumer Contract for GK Baseline
  Position*: a one-paragraph normative statement specifying the
  exact set of inputs Spec #11 expects from #12 (current ball
  position; current possession; pitch coordinates; bound of the
  6-yard / 18-yard boxes) and the exact computational shape
  Spec #11 reserves for itself (any micro-adjustment within
  `GK_REACTIVE_RADIUS_M = 1.5 m` of the #12-supplied baseline).
  This contract is the explicit `[GT]` ratification event for
  `GK_DEPTH_M`, `GK_ADVANCE_FACTOR`, `GK_LATERAL_FACTOR` — i.e.
  when #11 reaches `IN REVIEW`, these constants in #12 §3.3.3
  promote `[EST]` → `[GT]` atomically with #11's status flip (a
  patch revision to #12 §3.3.3 / §6 v1.0.x). #11 does NOT
  redefine the three constants; it only publishes the contract
  that authorises their promotion. **Resolves pass-1 H-2 and
  M-3.**

- **KD-14 — Cross-claim / aerial duel routing.** Aerial cross
  claims by the GK are head contacts iff the contact body part is
  head (route to Heading #10 per KD-4); hand contacts iff the
  contact body part is hand (route through Spec #11 §3.4). The
  contact body part is determined by Collision System #3
  contact-event data (Stage 0 approximation: ball-vs-agent-hand
  capsule vs. ball-vs-agent-head sphere intersection priority),
  NOT by intent. Contested aerial duels with multiple attackers
  use the duel mechanism of Heading #10 §3.7 for head contacts and
  Spec #11 §3.6 for hand contacts; both consume Collision System
  #3 contact-event lists. **Resolves pass-1 M-4 (cross-claim
  routing ambiguity).**

- **KD-15 — Rush / sweep abort policy.** A goalkeeper rush (sweep
  to clear a through-ball or close down a 1v1) is committed at the
  10 Hz tactical tick by Decision Tree #8. Once committed, the
  rush is NOT abortable on the basis of ball-trajectory changes
  *during* the rush (analog of Heading #10 KD-17 intent-staleness
  policy): the GK chose to rush; deviation is what the failure
  modes catalogue captures. EXCEPTION: if the ball is intercepted
  by another agent before the GK reaches it, the rush aborts via
  `GoalkeeperRushEvent.abortReason = BallIntercepted` and the GK
  recovers (state machine `Rush → Recover` per §3.1). No mid-rush
  re-targeting. **Resolves pass-1 M-5.**

- **KD-16 — Distribution release-point geometry is #11-owned.** The
  geometry of where the ball leaves the GK during distribution
  (release height above ground, launch angle range, windup
  duration) is owned by Spec #11 §3.8. Pass Mechanics #5 consumes
  the resulting `PassIntent` and produces the trajectory. Rationale
  (parallel to Heading #10 KD-16 spin-transfer ownership): the
  geometry depends on GK posture, dive recovery state, and chosen
  distribution kind (throw / roll / kick), which only #11 knows.
  **Resolves pass-1 M-6.**

- **KD-17 — Concussion / injury / yellow-card disciplinary modeling
  deferred to Stage 1+.** No injury or card-state system exists in
  the 20-spec set. §7 records the deferral pointing forward to a
  future Medical/Discipline spec. Same posture as Heading #10
  KD-15. **Resolves pass-1 L-1.**

- **KD-18 — `ReactionWindowAchieved` is asymmetric.** The reaction
  scalar uses different `[GT]` tolerance constants for "reacted
  too early" (a GK can technically commit before ball flight is
  legible — anticipation) and "reacted too late". Early-commit is
  penalised by misdirection risk (modeled via §3.3 dive direction
  selection at the 10 Hz tactical tick that pre-commits before
  full trajectory legibility); late-commit is penalised by
  reduced reach. Two distinct `[GT]` constants:
  `REACTION_EARLY_TOLERANCE_MS` and `REACTION_LATE_TOLERANCE_MS`.
  Mirrors Heading #10 KD-2 pass-1 H-1 fix for the same trap.
  **Resolves pass-1 H-1 (asymmetry).**

- **KD-19 — Set-piece saves IN scope at Stage 0; defensive wall is
  NOT.** Saves from free-kicks and penalties are mechanically
  identical to open-play saves because incoming `BallState` is
  read uniformly from Ball Physics #1 regardless of how it
  originated. The defensive wall (positioning of outfielders) is
  Defensive AI #14's concern (NOT STARTED); #11 sees only the
  resulting shot. Penalty taker selection / dynamics are Decision
  Tree #8 / Shot Mechanics #6 concerns. Mirrors Heading #10 KD-13
  for the cross-vs-header boundary. **Resolves pass-1 M-7.**

- **KD-20 — `OneVsOne` attribute behavior is closed-form, not
  branched.** The `OneVsOne` attribute modulates the
  `HandlingQualityScalar` and `ReactionWindowAchieved` formulas
  via a continuous coefficient (`ONE_VS_ONE_HANDLING_COEFF`,
  `ONE_VS_ONE_REACTION_COEFF`) gated on the GK's state machine
  being in `OneOnOne` (§3.1). No alternative formula path for
  1v1 saves; the same physics produces a different result
  because the inputs change continuously. **Resolves pass-1 L-2.**

- **KD-21 — Catch-vs-parry banding thresholds are `[GT]`.** The
  thresholds at which `HandlingQualityScalar` bands into
  `Caught` / `Parried` / `Deflected` / `Spilled` telemetry
  labels are `[GT]` constants (`CATCH_THRESHOLD`,
  `PARRY_THRESHOLD`, `DEFLECT_THRESHOLD`), tunable at design
  time. The bands govern TELEMETRY ONLY — they do not branch
  physics. Specifically: when `HandlingQualityScalar ≥
  CATCH_THRESHOLD`, `Ball.SetPossessor(gkId)` is invoked AND the
  ball is parked at the GK's hand position with zero velocity;
  when below, the ball receives a parry impulse via
  `Ball.ApplyKick` with magnitude scaling inversely with the
  scalar (lower handling → higher bounce energy retained from
  incoming speed, per §3.5). The `Caught` vs. `Parried` boundary
  is the ONLY band that toggles between two `Ball.*` API calls;
  the other bands (`Parried` vs. `Deflected` vs. `Spilled`) all
  resolve via `Ball.ApplyKick` and differ only in outgoing-
  velocity magnitude and angle. **Resolves pass-1 L-3 (band
  semantics ambiguity).**

---

## SECTION 1 — PURPOSE & SCOPE (`section-1.md`)

### 1.1 What This Specification Covers

**Subsection target length:** ~55 lines.

**Content:**
- Opening declarative scope statement.
- Bullet list of governance areas (10 items): GK state machine,
  shot reaction pipeline, dive kinematics integration, hand-ball
  contact resolution, cross-claim and high-ball, 1v1 confrontation,
  area sweep / rush dispatch, post-save recovery, distribution
  (throw / roll / kick) intent generation, failure-mode handling
  and telemetry surface.
- Applicability block: every gameplay action initiated by the GK
  agent role (one agent per side with `PlayerRole.Goalkeeper`);
  every ball contact in which the GK is the contacting agent
  AND the contact occurs inside the penalty area OR is a save
  attempt initiated from inside the penalty area (boundary
  per Laws of the Game; outside-box GK contacts route to outfield
  pipelines unless KD-14 hand-contact routing applies).
- Closing pointer to §1.2 (out-of-scope), §1.3 (KDs), §1.4
  (dependencies).

### 1.2 What Is Out of Scope

**Subsection target length:** ~35 lines.

**Content (one-line entries with owning document):**
- Head-ball contacts by the GK → Heading Mechanics #10 (KD-4).
- GK *resting* baseline position (formation slot) → Positioning AI
  #12 §3.3.3 (KD-3).
- Pass / kick trajectory generation (the ball-flight physics of
  distribution) → Pass Mechanics #5 (KD-6, KD-16). #11 owns the
  release-point geometry and intent payload; #5 owns the
  trajectory.
- Defensive wall positioning at free-kicks → Defensive AI #14
  (KD-19).
- Penalty kick taker dynamics → Shot Mechanics #6 + Decision Tree
  #8.
- Yellow / red card discipline, injury accumulation → Stage 1+
  (KD-17).
- Substitution logic (when a GK is replaced) → Stage 1+ match-
  management spec.

### 1.3 Key Design Decisions

**Subsection target length:** ~220 lines.

Twenty-one KDs (KD-1 … KD-21) reproduced from the KEY DESIGN
DECISIONS block above, each formatted as: statement (1 sentence),
rationale (2–3 sentences), consequence-if-violated (1 sentence).
KD numbering is canonical for the spec and cited by FR rows in §2.

### 1.4 Dependencies and Integration Contracts

**Subsection target length:** ~70 lines.

**Content:**
- Upstream table (10 rows: #1, #2, #3, #4, #5, #6, #7, #8, #10,
  #16, #17), each row naming the consuming subsection of #11 and
  the **exact verified** section-level citation in the upstream
  spec. Anchor cheatsheet:
  - #1 §1.2 (coordinate origin), §3.1.11.2 (`Ball.ApplyKick`),
    and the `Ball.SetPossessor` surface (anchor pinned during
    `section-1.md` authoring — current Ball Physics §3.1 publishes
    possession handling via `BallState.PossessorId` per ERR-008
    resolution; the setter site is at `ball-physics/section-3-1.md`
    around the `ApplyKick` block).
  - #2 §3.1.2 (`AgentMovementState`, `GroundedReason`), §3.5.1
    (`Agent` class), §3.5.6 (`PlayerAttributes` struct — Reflexes,
    Handling, Aerial, OneVsOne, Throwing, Kicking, Composure
    field declarations).
  - #3 §3.4.2 `ICollisionEventConsumer` consumer pattern (anchor
    confirmed against Heading #10 OI-005 pinning); per-contact
    data per #3 §3.x.
  - #4 §1.2 boundary statement (head exception per #10 KD-7; foot
    save-attempts are #11-owned regardless of body part).
  - #5 §3.x `PassIntent` consumer surface (anchor pinned during
    `section-4.md` authoring — Pass Mechanics #5 publishes intent
    consumption at its §1.7 / §3 surface).
  - #6 §4.5 (`ShotExecutedEvent`); §1.3 KD-6 body-part discriminator
    authority.
  - #7 §3.x perception-latency surface (anchor pinned during
    drafting — current Perception System §3 publishes
    visibility-cone latency in milliseconds).
  - #8 §1.7.x intent surface (anchor pinned during drafting); the
    GK-specific Decision Tree branches are NOT a #8 amendment —
    they extend #8's existing intent vocabulary by adding
    `SaveIntent` / `ClaimIntent` / `DistributeIntent` / `RushIntent`
    types that #8 already accommodates per its parameter-based
    intent design.
  - #10 §3.7 contested-duel mechanism (consumed for cross-claim
    head contacts per KD-14).
  - #16 §3.2 (entity ordering), §3.4 (`DOMAIN_TAG` catalogue —
    pending `0x17` allocation per KD-7), §4.1 (RNG service),
    §4.5 (draw-site registry).
  - #17 §3.2.1 `Publish API surface` (anchor confirmed against
    Heading #10 OI-005 pinning).
- Downstream table (4 rows: #12, #13, #14, #15) — interface
  surface is the four published events
  (`SaveAttemptedEvent`, `BallClaimedEvent`,
  `DistributionExecutedEvent`, `GoalkeeperRushEvent`) plus the
  KD-13 consumer-contract ratification for #12 GK constants.
- Positioning AI #12 IN-REVIEW-status note: this spec's `IN REVIEW`
  transition is the ratification event for #12's GK constants
  (KD-13); the inverse dependency is acknowledged but does NOT
  gate #11 (interface is declared here, not negotiated back).
- Pass Mechanics #5 amendment-insulation note (KD-6).
- Goalkeeper-vs-Heading boundary note (KD-4, KD-14).

### 1.5 Version History

Standard 5-column table (Version | Date | Author | Notes |
Reviewer).

---

## SECTION 2 — FUNCTIONAL REQUIREMENTS, DATA STRUCTURES & FAILURE MODES (`section-2.md`)

### 2.1 Functional Requirements Catalogue

**Subsection target length:** ~160 lines.

`FR-GK-001` … `FR-GK-NN` table. Each row: ID, statement,
conformance level (MUST / SHOULD / MAY), source KD, target
subsection. Projected count: ~42 FRs.

Anchor FRs to write first:
- `FR-GK-001` (MUST) — Save eligibility: a save is eligible iff
  GK state-machine is in {`Set`, `Anticipate`, `Diving`,
  `OneOnOne`} AND ball is within `GK_SAVE_VOLUME_RADIUS_M` of any
  predicted hand position within the dive-reach envelope.
  Source: KD-1, KD-12.
- `FR-GK-002` (MUST) — `HandlingQualityScalar` is computed as a
  continuous scalar; no branching on `Caught`/`Parried`/`Deflected`/
  `Spilled` labels in physics. Source: KD-1, KD-21.
- `FR-GK-003` (MUST) — No `SaveType` / `SaveClass` / `SaveOutcome`
  enum at any layer. Source: KD-1.
- `FR-GK-004` (MUST) — GK head contacts route to Heading #10.
  Source: KD-4.
- `FR-GK-005` (MUST) — GK resting baseline position consumed
  read-only from Positioning AI #12. Source: KD-3.
- `FR-GK-006` (MUST) — GK reactive position (set / shuffle /
  narrow / cross-claim / sweep / recovery) owned by Spec #11.
  Source: KD-3.
- `FR-GK-007` (MUST) — Distribution emits Pass Mechanics #5
  `PassIntent`-equivalent; no Pass-side amendment. Source: KD-6.
- `FR-GK-008` (MUST) — Distribution release-point geometry owned
  by Spec #11 §3.8. Source: KD-16.
- `FR-GK-009` (MUST) — Failed save → no `Ball.ApplyKick`, no
  `Ball.SetPossessor`; emit `SaveAttemptedEvent` with
  `failureCause`. Source: KD-11.
- `FR-GK-010` (MUST) — All randomness via `DeterministicRngService`
  with registered draw-site IDs. Source: KD-7.
- `FR-GK-011` (MUST) — Save physics consumes #3 contact data; no
  redefinition. Source: KD-5.
- `FR-GK-012` (MUST) — Fatigue convention `0=rested, 1=fatigued`.
  Source: KD-8, KD-10.
- `FR-GK-013` (MUST) — Corner-origin coordinates. Source: KD-10.
- `FR-GK-014` (MUST) — Tick-rate split: 10 Hz state-machine /
  intent / 60 Hz physics. Source: KD-10.
- `FR-GK-015` (MUST) — Every published constant carries a source
  tag. Source: KD-9.
- `FR-GK-016` (MUST) — Set-piece saves covered at Stage 0; the
  wall and the kick are not. Source: KD-19.
- `FR-GK-017` (MUST) — `ReactionWindowAchieved` uses asymmetric
  early/late tolerance constants. Source: KD-18.
- `FR-GK-018` (MUST) — Rush abort policy: in-flight rush is not
  abortable except `BallIntercepted`. Source: KD-15.
- `FR-GK-019` (MUST) — Telemetry labels (`Caught` / `Parried` /
  `Deflected` / `Spilled` / `Reflexive` / `Standard` / `Sluggish`)
  emitted ONLY post-formula; never branched on. Source: KD-1,
  KD-2, KD-21.
- `FR-GK-020` (MUST) — Stage 0 dive kinematics owned by Spec #11
  per KD-12. Source: KD-12.
- `FR-GK-021` (MUST) — Positioning AI #12 consumer contract
  published in §3.3.0; ratifies `[EST]` → `[GT]` promotion for
  three #12 GK constants when #11 reaches IN REVIEW. Source: KD-13.
- `FR-GK-022` (MUST) — Cross-claim contact routing: head→#10;
  hand→#11; determined by #3 contact-event body-part, NOT intent.
  Source: KD-14.
- `FR-GK-023` (MUST) — Catch-vs-parry boundary toggles between
  `Ball.SetPossessor` and `Ball.ApplyKick`; other band boundaries
  resolve via `Ball.ApplyKick` only. Source: KD-21.
- `FR-GK-024` (MUST) — `OneVsOne` attribute participates in
  closed-form coefficients; no 1v1-specific physics branch.
  Source: KD-20.
- `FR-GK-025` (MUST) — Iteration over multi-attacker cross-claim
  duels follows #16 §3.2 entity order.
- `FR-GK-026` (MUST) — `DOMAIN_TAG_GOALKEEPER` allocated `[CROSS-
  PENDING]`; resolved atomically on #16 back-prop or on
  `IN REVIEW → APPROVED` transition for this spec. Source: KD-7.
- `FR-GK-027` (MUST) — State-machine transitions deterministic;
  no `System.Random` / `DateTime.Now` paths. Source: KD-7,
  CLAUDE.md.
- (~15 more FRs covering specific formula behaviors, telemetry
  contracts, edge cases, and Stage 1+ deferrals.)

### 2.2 Data Structures

**Subsection target length:** ~130 lines.

Structs to define (struct-based, zero-allocation per CLAUDE.md):
- `SaveIntent` — Decision Tree #8 GK-branch output consumed by
  #11. Fields: `targetHand: HandEnum` (Left / Right / Either —
  this IS an enum because hand choice is discrete anatomy, not a
  physics parameter; KD-1's no-enum rule applies to PHYSICS-input
  enums, and `HandEnum` does not gate any physics formula — it
  parameterises a per-hand reach-geometry lookup), `clutchFirmness:
  float ∈ [0,1]`, `deflectionTarget: Vector3?`,
  `attemptCommittedTick: int`.
- `ClaimIntent` — for cross / aerial claims. Fields:
  `targetContactPoint: Vector3`, `clutchFirmness: float`,
  `attemptCommittedTick: int`.
- `DistributeIntent` — for throw / roll / kick. Fields:
  `deliveryKind: DeliveryKindEnum` (Throw / Roll / Kick — same
  exception as `HandEnum`: parameterises kinematic profile lookup,
  not physics), `targetReceiverId: int?`, `targetPoint: Vector3`,
  `powerIntent: float`, `spinIntent: Vector3`.
- `RushIntent` — for sweep / 1v1 close-down. Fields:
  `rushTarget: Vector3`, `commitmentLevel: float ∈ [0,1]`,
  `attemptCommittedTick: int`.
- `GoalkeeperState` — state-machine state ∈ {`Resting`, `Set`,
  `Anticipate`, `Diving`, `Airborne`, `HandsOnBall`, `Recovering`,
  `Distributing`, `Rushing`, `OneOnOne`, `Smothered`}.
- `GKContactState` — internal per-frame structure during 60 Hz
  contact resolution. Fields: `predictedContactFrame: int`,
  `actualContactFrame: int`, `reactionWindowAchieved: float`,
  `handlingQualityScalar: float`, `contactPointError: Vector2`
  (m, in hand-local coordinates), `handChoice: HandEnum`,
  `clutchFirmness: float`.
- `SaveAttemptedEvent` — published on every save attempt
  (successful or failed). Fields: `agentId`, `matchTime`,
  `saveIntent: SaveIntent`, `reactionWindowAchieved`,
  `handlingQualityScalar`, `handlingQualityLabel: enum`
  (`Caught` / `Parried` / `Deflected` / `Spilled` / `Missed` —
  emitted, not consumed), `reactionLabel: enum` (`Reflexive` /
  `Standard` / `Sluggish`), `contactPoint`, `incomingBallState`,
  `outgoingBallVelocity`, `outgoingBallSpin`, `contestedDuelId:
  int?`, `failureCause: enum?` (only when `handlingQualityLabel
  == Missed`).
- `BallClaimedEvent` — published on cross / aerial / 1v1 claim
  (catch path). Fields: `agentId`, `matchTime`, `claimType: enum`
  (`Cross` / `Aerial` / `OneOnOne` / `ShotCatch` — telemetry),
  `contestedDuelId: int?`, `releaseTickEarliest: int` (tactical-
  loop tick when distribution may begin; honours the 6-second
  rule via `GK_HOLD_MAX_TICKS = 60` `[FIXED]` at 10 Hz).
- `DistributionExecutedEvent` — published when distribution kick /
  throw / roll releases. Fields: `agentId`, `matchTime`,
  `deliveryKind`, `targetReceiverId: int?`, `targetPoint`,
  `passIntent`, `releasePoint: Vector3`, `windupDurationMs: int`.
- `GoalkeeperRushEvent` — published on rush state entry / exit /
  abort. Fields: `agentId`, `matchTime`, `rushTarget`,
  `rushPhase: enum` (`Launched` / `InFlight` / `Reached` /
  `Aborted`), `abortReason: enum?` (`BallIntercepted` /
  `BallCleared` / `AttackerBeatGK`).
- `CrossClaimDuelContext` — populated when ≥2 agents are within
  cross-claim contact volume simultaneously. Fields: `duelId`,
  `participantAgentIds: ReadOnlySpan<int>`, `winnerAgentId`,
  `contactBodyPart: enum` (`Head` → routes to #10; `Hand` → §3.6
  resolution here).

### 2.3 Failure Modes

**Subsection target length:** ~90 lines.

Catalogue of expected failure modes:
- F-01: Mistimed dive (ball passed save volume before dive apex).
  Detection: `actualContactFrame > predictedContactFrame +
  REACTION_LATE_TOLERANCE_MS / FRAME_MS`. Recovery:
  `SaveAttemptedEvent` with `failureCause = MistimedDive`. KD-11.
- F-02: Wrong-direction dive. Detection: predicted-contact hand
  position more than `WRONG_DIRECTION_THRESHOLD_M` from ball
  trajectory at GK depth. Recovery:
  `SaveAttemptedEvent` with `failureCause = WrongDirection`.
- F-03: Out-of-reach (ball outside GK hand-reach envelope at all
  candidate frames). Recovery: `SaveAttemptedEvent` with
  `failureCause = OutOfReach`.
- F-04: Cross-claim contested by an attacker — resolved per §3.6
  (hand) or routed to Heading #10 §3.7 (head). NOT a failure;
  winner-only emits the relevant event, losers emit
  `SaveAttemptedEvent` / `HeaderAttemptFailedEvent` with
  `failureCause = DisturbedInDuel`.
- F-05: Decision Tree #8 supplied `targetReceiverId` who is no
  longer on the pitch (substituted between commit and release).
  Recovery: fall back to `targetPoint`-based distribution; emit
  telemetry warning; NOT a hard failure.
- F-06: `BallState` snapshot stale (>1 physics frame old).
  Recovery: re-query Ball Physics #1; do not extrapolate.
- F-07: GK in non-eligible state when shot arrives (e.g. still in
  `Distributing` windup). Recovery: physics treats GK as obstacle
  (#3 contact pipeline); no `SaveAttemptedEvent` emitted; ball
  may rebound off the GK body via #3-standard rebound physics.
  (Tracked separately from F-01..F-04 because it is a state-
  machine sequencing issue, not a save quality issue.)
- F-08: Rush aborted mid-flight by ball interception. Detection:
  `BallState.possessorId` becomes a non-GK agent during rush.
  Recovery: `GoalkeeperRushEvent` with `abortReason =
  BallIntercepted`; state machine `Rush → Recover`. KD-15.
- F-09: Distribution kick targeted outside pitch bounds.
  Recovery: clamp `targetPoint` to nearest in-bounds point;
  telemetry warning; NOT a hard failure.
- F-10: `clutchFirmness` outside [0,1]. Recovery: clamp to bound;
  emit telemetry warning.

### 2.4 Telemetry Surface

**Subsection target length:** ~50 lines.

Counters and gauges emitted on the trace pipeline (Performance
Optimization #18 Appendix F.0 channel-registry schema; channel
rows allocated via #18 Appendix F.0 back-prop at Stage 0+1 per
the schedule established by Heading #10 OI-002 closure pattern):
- `gk.save.reaction.window` (histogram, scalar 0..1).
- `gk.save.reaction.label` (counter, 3 buckets: `Reflexive` /
  `Standard` / `Sluggish`).
- `gk.save.handling.quality` (histogram, scalar 0..1).
- `gk.save.handling.label` (counter, 5 buckets: `Caught` /
  `Parried` / `Deflected` / `Spilled` / `Missed`).
- `gk.cross_claim.outcome` (counter: win/loss/disturbed).
- `gk.rush.outcome` (counter: reached/aborted/intercepted).
- `gk.distribution.kind` (counter: throw/roll/kick).
- `gk.state.transition` (counter, transition-pair tagged).

---

## SECTION 3 — CORE FORMULAS, ALGORITHMS, PSEUDOCODE (`section-3.md`)

**Subsection target length:** ~700 lines (largest section).

### 3.1 GK State Machine

States: `Resting`, `Set`, `Anticipate`, `Diving`, `Airborne`,
`HandsOnBall`, `Recovering`, `Distributing`, `Rushing`,
`OneOnOne`, `Smothered`.

Transition table (~25 transitions) with trigger condition, source
spec, target state, and tick-rate (10 Hz tactical or 60 Hz physics
event-driven). Examples:
- `Resting → Set` on `BallState.position` entering attacking
  third (10 Hz; ball-position trigger).
- `Set → Anticipate` on `ShotExecutedEvent` consumed (60 Hz event).
- `Anticipate → Diving` on Decision Tree #8 commit (10 Hz).
- `Diving → Airborne` on dive-launch impulse applied (60 Hz).
- `Airborne → HandsOnBall` on #3 hand-ball contact event (60 Hz).
- `Airborne → Recovering` on ground re-entry without contact
  (60 Hz).
- `HandsOnBall → Distributing` on Decision Tree #8 commit
  (10 Hz).
- `Set → Rushing` on rush commit (10 Hz; Decision Tree #8).
- `Rushing → Smothered` on hand-ball contact during rush.
- `Rushing → Recovering` on rush abort (KD-15).

Pseudocode for state-eval at each tick frequency. Iteration order
deterministic per #16 §3.2 (single GK per side eliminates the
iteration-order concern for state-machine evaluation, but
multi-attacker scenarios in §3.6 still require explicit
iteration order).

### 3.2 Shot Reaction Pipeline

**Inputs:** `ShotExecutedEvent` (Shot Mechanics #6 §4.5), GK
attributes via `PlayerAttributes` (AM #2 §3.5.6), Perception
System #7 visibility-cone latency.
**Outputs:** `requiredReactionMs`, `reactionWindowAchieved`,
`predictedContactFrame`, `dive direction commit` (entered at the
10 Hz tactical tick following shot detection).

```
shotDetectedTickMs   = shotExecutedEvent.matchTimeMs
                     + PERCEPTION_BASE_LATENCY_MS · perceptionLatencyScale(GK)
requiredReactionMs   = REACTION_BASE_MS
                     - REACTION_REFLEXES_COEFF · Reflexes_norm
                     + REACTION_BALL_SPEED_COEFF
                       · max(0, ballSpeed - REACTION_BALL_SPEED_REF_MPS)
elapsedSinceShotMs   = currentMatchTimeMs - shotDetectedTickMs
reactionOffsetMs     = elapsedSinceShotMs - requiredReactionMs

if reactionOffsetMs <= 0:
    reactionWindowAchieved = 1 - clamp01(-reactionOffsetMs /
                                          REACTION_EARLY_TOLERANCE_MS)
else:
    reactionWindowAchieved = 1 - clamp01( reactionOffsetMs /
                                          REACTION_LATE_TOLERANCE_MS)
```

`perceptionLatencyScale(GK)` is `[DERIVED]` from #7 §3.x scaled
by `Reflexes`. Stage 0 approximation: `1 - 0.3 · Reflexes_norm`
(formula and exact constants pinned in `section-3.md` §3.2).

Telemetry-label assignment (banded post-computation):
- `Reflexive` if `reactionWindowAchieved >
  REFLEXIVE_LABEL_THRESHOLD`.
- `Sluggish` if `reactionWindowAchieved <
  SLUGGISH_LABEL_THRESHOLD`.
- `Standard` otherwise.

Worked example: 25 m/s shot from 18 m out (flight time ≈ 720 ms);
GK with `Reflexes = 0.8`; `requiredReactionMs` ≈ 280 ms;
elapsed = 250 ms at 60 Hz tick following shot detection;
`reactionOffsetMs = -30 ms`; with
`REACTION_EARLY_TOLERANCE_MS = 100`, `reactionWindowAchieved =
0.7`; label = `Standard`.

### 3.3 Dive Kinematics (KD-12)

**Stage 0 synthetic dive trajectory (KD-12):**

```
diveDirection         = sign(targetHandY - gkY)        // -1 / 0 / +1  (lateral = Y, across goal mouth; §1.2)
diveLaunchImpulse_mps = DIVE_LAUNCH_BASE_MPS
                      + DIVE_LAUNCH_K_STRENGTH · Strength_norm
                      + DIVE_LAUNCH_K_AERIAL   · Aerial_norm
diveDurationMs        = DIVE_PHASE_DURATION_MS
                       (does NOT scale with attributes at Stage 0;
                        attribute-scaling deferred to Stage 1+
                        per §7)
peakHandZ_m           = DIVE_PEAK_Z_BASE_M
                      + DIVE_PEAK_Z_K_AERIAL · Aerial_norm
                      + DIVE_PEAK_Z_K_STRENGTH · Strength_norm
                      - DIVE_FATIGUE_PEAK_Z_COEFF · fatigue
                      + diveTimingJitterMs · DIVE_JITTER_PEAK_Z_COEFF
diveTimingJitterMs    = DIVE_TIMING_JITTER_SIGMA_MS
                      · rng.NextGaussian(DRAW_SITE_DIVE_TIMING_JITTER)
handPathZ(frame)      = parabolic interpolation peaking at apex
                        frame with peak value peakHandZ_m
```

Hand reach envelope at apex frame is computed from `Aerial`,
`Handling`, `Strength`, dive direction, and a `[GT]` body
articulation budget. Worked example with sensitivity (Appendix B).

State-machine effect: dive launch enters `Airborne`; recovery to
`Recovering` enters `GROUNDED` with `GroundedReason.DIVING_HEADER`
re-use OR a new `GroundedReason.DIVING_SAVE` enum value to AM #2
— **resolved in v1.1 / v1.2 per KD-12 amendment**: §3.3 uses
`GroundedReason.DIVING_HEADER` (no AM #2 amendment), and the
GROUNDED reason is RE-LABELLED in telemetry via the
`SaveAttemptedEvent.contactBodyPart` field for diagnostic
disambiguation. AM #2 amendment is recorded in §7 as a Stage 1+
cleanup item.

### 3.3.0 Positioning AI #12 Consumer Contract (KD-13)

**Subsection target length:** ~50 lines.

Normative statement specifying:
1. **Inputs Spec #11 expects from #12** (read-only at every
   10 Hz tactical tick): `gkBaselineSlot: Vector2` (computed by
   #12 §3.3.3 using `GK_DEPTH_M`, `GK_ADVANCE_FACTOR`,
   `GK_LATERAL_FACTOR` and current ball position via #12's
   `basisX`/`basisY` functions).
2. **What Spec #11 reserves for itself**: any micro-adjustment
   within `GK_REACTIVE_RADIUS_M = 1.5 m` `[GT]` of
   `gkBaselineSlot` while the state machine is in `Resting` or
   `Set`; full XY freedom during `Anticipate`, `Diving`,
   `Airborne`, `HandsOnBall`, `Recovering`, `Rushing`, `OneOnOne`,
   `Smothered`.
3. **Ratification mechanism**: when Spec #11 reaches `IN REVIEW`,
   the three #12 GK constants in `positioning-ai/section-3.md`
   §3.3.3 and `section-6.md` row entries promote `[EST]` → `[GT]`
   via a #12 patch revision (v1.0.x); coordinated atomically.
4. **Forward-binding constraint**: any future #12 amendment to
   `GK_DEPTH_M` / `GK_ADVANCE_FACTOR` / `GK_LATERAL_FACTOR` is
   subject to Spec #11 §9.5 cross-spec re-audit at Stage 1+
   tunings.

### 3.4 Master Physical Profile Table

Per-row constants table (mirrors Heading #10 §3.1 / Pass Mechanics
#5 §3.1.4 structure): one column per tunable constant, source-tag
column, unit column, valid-range column, citation column.

**Inventory discipline (pass-1 M-8 closure):** every symbol that
appears in §3.2–§3.8 pseudocode bodies MUST be a row in this
table with a source tag, OR be a per-call output / local variable
explicitly named as such in the relevant §3.X subsection.

Constants to enumerate (~50 rows):
- `GK_SAVE_VOLUME_RADIUS_M` `[GT]`.
- `GK_REACTIVE_RADIUS_M` `[GT]` (KD-13).
- `GK_HOLD_MAX_TICKS` `[FIXED]` (6-second rule; 60 ticks at 10 Hz;
  derived from the Laws of the Game and tagged `[FIXED]` not
  `[GT]` because it is a rule constant, not a designer tuning).
- `REACTION_BASE_MS` `[GT]`.
- `REACTION_REFLEXES_COEFF` `[GT]`.
- `REACTION_BALL_SPEED_COEFF` `[GT]`.
- `REACTION_BALL_SPEED_REF_MPS` `[GT]`.
- `REACTION_EARLY_TOLERANCE_MS` `[GT]` (KD-18).
- `REACTION_LATE_TOLERANCE_MS` `[GT]` (KD-18; numerically smaller
  than `REACTION_EARLY_TOLERANCE_MS` per analogous Heading #10
  pass-1 H-1 fix).
- `REFLEXIVE_LABEL_THRESHOLD` `[GT]` (telemetry-band boundary).
- `SLUGGISH_LABEL_THRESHOLD` `[GT]` (telemetry-band boundary).
- `PERCEPTION_BASE_LATENCY_MS` `[CROSS]` (Perception System #7
  §3.x).
- `DIVE_LAUNCH_BASE_MPS` `[GT]`.
- `DIVE_LAUNCH_K_STRENGTH` `[GT]`.
- `DIVE_LAUNCH_K_AERIAL` `[GT]`.
- `DIVE_PHASE_DURATION_MS` `[GT]`.
- `DIVE_PEAK_Z_BASE_M` `[GT]`.
- `DIVE_PEAK_Z_K_AERIAL` `[GT]`.
- `DIVE_PEAK_Z_K_STRENGTH` `[GT]`.
- `DIVE_FATIGUE_PEAK_Z_COEFF` `[GT]`.
- `DIVE_TIMING_JITTER_SIGMA_MS` `[GT]`.
- `DIVE_JITTER_PEAK_Z_COEFF` `[GT]`.
- `WRONG_DIRECTION_THRESHOLD_M` `[GT]`.
- `HANDLING_BASE` `[GT]`.
- `HANDLING_K_ATTR` `[GT]` (multiplier on `Handling_norm`).
- `HANDLING_K_BALL_SPEED` `[GT]` (negative; faster ball → harder
  to handle).
- `HANDLING_BALL_SPEED_REF_MPS` `[GT]`.
- `HANDLING_FATIGUE_COEFF` `[GT]`.
- `HANDLING_NOISE_SIGMA` `[GT]`.
- `HANDLING_POINT_ERROR_SIGMA_M` `[GT]`.
- `HANDLING_REACTION_BLEND_ALPHA` `[GT]` (convex weight; see
  §3.4 formula).
- `CATCH_THRESHOLD` `[GT]` (KD-21).
- `PARRY_THRESHOLD` `[GT]` (KD-21).
- `DEFLECT_THRESHOLD` `[GT]` (KD-21).
- `MIN_HANDLING_QUALITY` `[GT]` (below this → `Missed`).
- `PARRY_VELOCITY_RETAIN_BASE` `[GT]` (fraction of incoming
  speed retained when parrying at perfect quality).
- `PARRY_VELOCITY_RETAIN_K_QUALITY` `[GT]` (sensitivity to
  `HandlingQualityScalar`).
- `PARRY_DEFLECT_ANGLE_SIGMA_RAD` `[GT]` (deflection angle
  Gaussian sigma when quality is mid-range).
- `CLUTCH_FIRMNESS_K_RETAIN` `[GT]` (`clutchFirmness` modulation
  of parry retain).
- `ONE_VS_ONE_HANDLING_COEFF` `[GT]` (KD-20).
- `ONE_VS_ONE_REACTION_COEFF` `[GT]` (KD-20).
- `RUSH_LAUNCH_BASE_MPS` `[GT]`.
- `RUSH_LAUNCH_K_PACE` `[GT]` (uses AM #2 `Pace` attribute).
- `RUSH_COMMIT_FATIGUE_COEFF` `[GT]`.
- `THROW_RELEASE_HEIGHT_M` `[GT]`.
- `ROLL_RELEASE_HEIGHT_M` `[GT]`.
- `KICK_RELEASE_HEIGHT_M` `[GT]`.
- `THROW_WINDUP_MS` `[GT]`.
- `ROLL_WINDUP_MS` `[GT]`.
- `KICK_WINDUP_MS` `[GT]`.
- `CROSS_CLAIM_VOLUME_RADIUS_M` `[GT]` (used by §3.6).
- `CROSS_CLAIM_DUEL_BALANCE_W` `[GT]` (§3.6 weight).
- `CROSS_CLAIM_DUEL_STRENGTH_W` `[GT]`.
- `CROSS_CLAIM_DUEL_AERIAL_W` `[GT]`.
- `CROSS_CLAIM_TIEBREAK_EPSILON` `[GT]`.
- `CROSS_CLAIM_TIEBREAK_NOISE_AMPLITUDE` `[GT]`.
- `FRAME_MS` `[DERIVED]` from `TICK_RATE_PHYSICS_HZ`.
- `GRAVITY_MPS2` `[CROSS]` (Ball Physics #1).
- `PITCH_LENGTH_M` `[CROSS]` (Ball Physics #1 §1.2).
- `PITCH_WIDTH_M` `[CROSS]` (Ball Physics #1 §1.2).
- `PENALTY_AREA_DEPTH_M` `[CROSS]` (Ball Physics #1 — exact
  anchor pinned during drafting).
- `DOMAIN_TAG_GOALKEEPER = 0x17` `[CROSS-PENDING]`
  (Deterministic Simulation #16 §3.4 — back-prop ERR-011-001).
- `TICK_RATE_TACTICAL_HZ` `[CROSS]` (CLAUDE.md).
- `TICK_RATE_PHYSICS_HZ` `[CROSS]` (CLAUDE.md).

### 3.5 Handling-Quality Scalar (KD-1, KD-2, KD-21)

```
handlingNoise        = HANDLING_NOISE_SIGMA
                     · rng.NextGaussian(DRAW_SITE_HANDLING_NOISE)
contactPointError    = ||handContactActual - targetHandContact||
                       + HANDLING_POINT_ERROR_SIGMA_M
                         · rng.NextGaussian(DRAW_SITE_HANDLING_NOISE)
                                                  // shares the same draw site
                                                  // is INTENTIONALLY false:
                                                  // see L-5 fix — use a
                                                  // distinct draw site below
pointQuality         = 1 - clamp01(contactPointError /
                                    HANDLING_POINT_ERROR_SIGMA_M)
speedFactor          = clamp01(1 - HANDLING_K_BALL_SPEED ·
                                    max(0, ballSpeed -
                                            HANDLING_BALL_SPEED_REF_MPS) /
                                            HANDLING_BALL_SPEED_REF_MPS)
attrFactor           = HANDLING_BASE
                     + HANDLING_K_ATTR · Handling_norm
                     - HANDLING_FATIGUE_COEFF · fatigue
                     + (state == OneOnOne ?
                          ONE_VS_ONE_HANDLING_COEFF · OneVsOne_norm : 0)
rawHandling          = attrFactor · speedFactor · pointQuality
                     + handlingNoise
handlingQualityScalar = clamp01(HANDLING_REACTION_BLEND_ALPHA · rawHandling
                              + (1 - HANDLING_REACTION_BLEND_ALPHA)
                                · reactionWindowAchieved)
```

**Note (v1.2 L-5 fix):** the `contactPointError` Gaussian noise
above is sampled from `DRAW_SITE_HANDLING_POINT_NOISE`, a SEPARATE
draw site from `DRAW_SITE_HANDLING_NOISE`. The original v1.0
listed only three draw sites; v1.2 adds a fourth and the §4.4
list is updated accordingly. Sharing a draw site would entangle
two independent error sources (handling-scale noise vs. point-
error noise), violating the §16 §4.5 draw-site registry's
single-purpose-per-site rule.

Band-to-action mapping (post-formula, KD-21 explicit):
```
if handlingQualityScalar >= CATCH_THRESHOLD:
    Ball.SetPossessor(gkId)
    Ball velocity set to GK hand velocity; ball "owned"
    label = Caught
elif handlingQualityScalar >= PARRY_THRESHOLD:
    Ball.ApplyKick(parryVelocity(quality, clutchFirmness), spin, gkId, t)
    label = Parried
elif handlingQualityScalar >= DEFLECT_THRESHOLD:
    Ball.ApplyKick(deflectVelocity(quality, deflectionTarget), spin, gkId, t)
    label = Deflected
elif handlingQualityScalar >= MIN_HANDLING_QUALITY:
    Ball.ApplyKick(spillVelocity(quality), spin, gkId, t)
    label = Spilled
else:
    // no contact achieved despite eligibility — F-01..F-03
    SaveAttemptedEvent with failureCause; ball unchanged
    label = Missed
```

`parryVelocity`, `deflectVelocity`, `spillVelocity` are
closed-form helpers defined in §3.5.x with worked examples in
Appendix A.2.

### 3.6 Cross-Claim & Aerial Duel Resolution

Cross / aerial / 1v1 duels among ≥2 agents within
`CROSS_CLAIM_VOLUME_RADIUS_M`. Algorithm mirrors Heading #10
§3.7 structure (so the duel arithmetic is consistent across
specs):

1. Determine **contact body part** per agent at the candidate
   contact frame from Collision System #3 (hand capsule vs.
   head sphere intersection priority — Stage 0 approximation;
   the priority rule is published in §3.6 with pseudocode).
2. **Routing (KD-14):** if winning contact body part is `Head`,
   defer to Heading #10 §3.7 duel mechanism; if `Hand`, resolve
   here:
3. Iterate participants in #16 §3.2 entity order.
4. Compute base score:
   `baseScore = CROSS_CLAIM_DUEL_BALANCE_W  · Balance_norm
              + CROSS_CLAIM_DUEL_STRENGTH_W · Strength_norm
              + CROSS_CLAIM_DUEL_AERIAL_W   · Aerial_norm`.
5. Rank; near-tie tiebreak via
   `DRAW_SITE_CROSS_CLAIM_TIEBREAK` (Gaussian noise of amplitude
   `CROSS_CLAIM_TIEBREAK_NOISE_AMPLITUDE`) applied ONLY when
   `|scoreA - scoreB| < CROSS_CLAIM_TIEBREAK_EPSILON`.
6. Winner emits `BallClaimedEvent` (catch) or
   `SaveAttemptedEvent` (parry/deflect/spill) per §3.5
   handling-quality outcome.
7. Losers emit `SaveAttemptedEvent` with
   `failureCause = DisturbedInDuel`.

Worked example: GK + striker contesting a corner; striker wins
with `baseScore = 0.74` vs. GK 0.68; routes to Heading #10 §3.7
for outgoing-vector physics.

### 3.7 Rush / Sweep Dispatch (KD-15)

State entry condition: Decision Tree #8 `RushIntent.commitmentLevel
> RUSH_COMMIT_THRESHOLD` at the 10 Hz tactical tick.

Launch impulse:
```
rushLaunchMps = RUSH_LAUNCH_BASE_MPS
              + RUSH_LAUNCH_K_PACE · Pace_norm
              - RUSH_COMMIT_FATIGUE_COEFF · fatigue
```

Per-frame update in `Rushing` state: GK XY position advances
toward `rushTarget` at `rushLaunchMps`; ball-interception check
on every 60 Hz tick; abort policy per KD-15.

`Rushing → Smothered` on hand-ball contact during rush (executes
§3.5 handling-quality pipeline with state-specific coefficients
honoring `OneVsOne` per KD-20 if the GK is also in `OneOnOne`
phase).

### 3.8 Distribution Generation (KD-6, KD-16)

Decision Tree #8 supplies `DistributeIntent` at the 10 Hz tactical
tick once the GK enters `HandsOnBall` state and
`releaseTickEarliest` has passed.

```
releaseHeight   = match deliveryKind:
                    Throw → THROW_RELEASE_HEIGHT_M
                    Roll  → ROLL_RELEASE_HEIGHT_M
                    Kick  → KICK_RELEASE_HEIGHT_M
windupMs        = match deliveryKind:
                    Throw → THROW_WINDUP_MS
                    Roll  → ROLL_WINDUP_MS
                    Kick  → KICK_WINDUP_MS
releasePoint    = gkPosition + Vector3(0, 0, releaseHeight)
passIntent      = PassIntent {
                    sourceAgentId = gkId,
                    sourcePoint   = releasePoint,
                    targetPoint   = distributeIntent.targetPoint,
                    targetReceiverId = distributeIntent.targetReceiverId,
                    powerIntent   = distributeIntent.powerIntent,
                    spinIntent    = distributeIntent.spinIntent,
                    deliveryKind  = mapToPassMechanicsDelivery(deliveryKind)
                  }
emit DistributionExecutedEvent
publish passIntent to Pass Mechanics #5 via #5 §3.x intent surface
state machine: Distributing → Recovering or Distributing → Set
                (whichever appropriate; per state-machine §3.1)
```

`mapToPassMechanicsDelivery` is a one-to-one structural mapping
(throw → `LowDriven`-equivalent at sub-cross height; roll →
ground roll; kick → `Lofted`-equivalent) documented in §3.8.

### 3.9 Failed-Save Pipeline

Pseudocode for failure emission (F-01..F-04 from §2.3): no
`Ball.ApplyKick`, no `Ball.SetPossessor`; ball state unchanged;
`SaveAttemptedEvent` published with `failureCause`.

### 3.10 Boundary Algorithms

- Boundary with Heading #10 (KD-4, KD-14): single-paragraph
  statement + pseudocode predicate `if contactBodyPart == Head →
  Spec #10`.
- Boundary with Positioning AI #12 (KD-3, KD-13): §3.3.0 consumer
  contract.
- Boundary with Pass Mechanics #5 (KD-6, KD-16): release-point
  here; trajectory there.
- Boundary with Collision System #3 (KD-5): contact-event API
  consumed read-only.

---

## SECTION 4 — ARCHITECTURE, FILE LAYOUT, INTERFACE CONTRACTS (`section-4.md`)

**Subsection target length:** ~280 lines.

### 4.1 File Layout (under `src/Gameplay/Goalkeeper/`)

- `GoalkeeperMechanics.cs` — orchestrator; consumed by simulation
  scheduler at both 10 Hz (state transitions, intent commits) and
  60 Hz (physics phase).
- `GoalkeeperConstants.cs` — every constant from §3.4 with its
  source tag; no magic numbers in formula files (KD-9).
- `GoalkeeperStateMachine.cs` — §3.1.
- `GoalkeeperReactionPipeline.cs` — §3.2.
- `GoalkeeperDiveKinematics.cs` — §3.3.
- `GoalkeeperPositioningContract.cs` — §3.3.0 (KD-13).
- `GoalkeeperHandlingQuality.cs` — §3.5.
- `GoalkeeperCrossClaimDuel.cs` — §3.6.
- `GoalkeeperRushDispatch.cs` — §3.7.
- `GoalkeeperDistribution.cs` — §3.8.
- `GoalkeeperTelemetry.cs` — §2.4 surface emission.
- Test layout: `tests/Gameplay/Goalkeeper/` with one file per
  source file (Spec #19 §3.x).

### 4.2 Input Interface Contracts

Method signatures (consumed):
- `BallPhysics.GetBallState(matchTime) → BallState` — Spec #1.
- `BallState.PossessorId` — Spec #1 (used by rush-abort detection
  per F-08).
- `Agent` instance access for kinematics + attribute reads —
  Spec #2 §3.5.1 / §3.5.6.
- `AgentMovementState` + `GroundedReason` enums — Spec #2 §3.1.2.
- `CollisionSystem` — Spec #3 §3.4.2 `ICollisionEventConsumer`
  pattern.
- `ShotExecutedEvent` subscription — Spec #6 §4.5 / Event System
  #17 subscribe API.
- `Perception.GetVisibilityLatency(agentId, target) → ms` —
  Spec #7 §3.x (anchor pinned during drafting).
- `DecisionTree.GetGKIntent(agentId, tick) →
  SaveIntent | ClaimIntent | RushIntent | DistributeIntent | None`
  — Spec #8 §1.7.x (anchor pinned during drafting).
- `PositioningAI.GetGKBaselineSlot(matchTime) → Vector2` —
  Spec #12 §3.3.3 (consumed read-only per KD-3 / §3.3.0).
- `DeterministicRng.NextFloat(drawSiteId) → float`,
  `DeterministicRng.NextGaussian(drawSiteId) → float` — Spec #16
  §4.1 / §4.5.

### 4.3 Output Interface Contracts

Method signatures (emitted):
- `Ball.ApplyKick(velocity, spin, agentId, matchTime)` — Spec #1
  §3.1.11.2.
- `Ball.SetPossessor(agentId)` — Spec #1 (anchor pinned during
  drafting; this surface is presumed published per ERR-008
  resolution; if absent, a back-prop entry filed to add it as a
  pure namespace amendment to APPROVED #1).
- `EventBus.Publish<SaveAttemptedEvent>(evt)` — Spec #17 §3.2.1.
- `EventBus.Publish<BallClaimedEvent>(evt)` — Spec #17.
- `EventBus.Publish<DistributionExecutedEvent>(evt)` — Spec #17.
- `EventBus.Publish<GoalkeeperRushEvent>(evt)` — Spec #17.
- `PassMechanics.ConsumePassIntent(passIntent)` — Spec #5 §3.x
  (anchor pinned during drafting).
- `Heading.SubmitGKIntent(headerIntent)` — Spec #10 (used when a
  GK head save occurs; routed via Decision Tree #8 GK branches
  per KD-4).

### 4.4 Determinism Compliance Surface

Listing of all #11 → #16 touchpoints:
- `DOMAIN_TAG_GOALKEEPER = 0x17` allocation request (back-prop
  ERR-011-001; pure namespace amendment per #10 `0x16` / #17
  `0x15` precedent; collision-management policy with ERR-012-001
  per KD-7).
- Registered draw sites (4 — v1.2 expanded from 3 per L-5):
  `DRAW_SITE_HANDLING_NOISE` (§3.5 handling-scale Gaussian),
  `DRAW_SITE_HANDLING_POINT_NOISE` (§3.5 contact-point Gaussian
  — v1.2 added per L-5),
  `DRAW_SITE_DIVE_TIMING_JITTER` (§3.3 dive timing Gaussian),
  `DRAW_SITE_CROSS_CLAIM_TIEBREAK` (§3.6 near-tie perturbation).
- Entity-iteration order in §3.6 (cross-claim duel).

### 4.5 Performance Compliance Surface

Pre-commitments referenced from #18 §6 ratify-not-override (KD-2
of #18). Budget framing (steady-state vs. p99 tail, mirroring
Heading #10 H-4 fix):
- 0-byte hot-path allocation budget (#18 §3.10 `[FIXED]`).
- **Steady-state per-tick cost budget**: ≤30 µs (`[EST]`) at
  22-agent match (only 2 GKs — the steady-state cost is
  dominated by state-machine evaluation, not contact resolution).
- **p99 save-frame tail budget**: ≤220 µs (`[EST]`) at save-
  resolution frames (dive launch + contact resolution +
  handling-quality computation + cross-claim duel arithmetic).
  Justified by §6.3 component-cost breakdown.
- **p99 cross-claim duel-frame tail budget**: ≤280 µs (`[EST]`)
  at 3-way duel frames (GK + 2 attackers, near-tie tiebreak
  invocation, head-vs-hand routing decision).
- All `[EST]` budgets are not credible until
  `certification-platform.md` Stage-0 host pin lands; the
  `FR-PO-052` Stage 0+1 perf-gate activation is gated on that
  pin and not on #11 sign-off.
- No `HotPathAllocExempt` attribute uses required (struct-based
  data flow).

### 4.6 Tick-Scheduling Surface

- 10 Hz tactical loop: Decision Tree #8 GK branches produce
  `SaveIntent` / `ClaimIntent` / `RushIntent` / `DistributeIntent`;
  state-machine transitions evaluated; Positioning AI #12
  baseline slot read.
- 60 Hz physics loop: shot-detection latency progress; dive
  kinematics integration; hand-ball contact resolution;
  handling-quality computation; cross-claim duel arithmetic;
  output emission. Sequence diagram (ASCII) in §4.6.

---

## SECTION 5 — TEST PLAN (`section-5.md`)

**Subsection target length:** ~320 lines.

### 5.1 Unit Tests

One sub-section per §3 algorithm; ~6–10 test cases each. Examples:
- 5.1.1 State machine (every transition exercised; cycle
  detection; deterministic ordering under sequential events).
- 5.1.2 Reaction pipeline (signed `reactionOffsetMs` sweep
  -300 ms…+300 ms; `Reflexes` sweep 0…1; ball-speed sweep
  10…40 m/s).
- 5.1.3 Dive kinematics (`Strength`/`Aerial` sweep ±10%; fatigue
  sweep 0…1; apex-Z sensitivity).
- 5.1.4 Handling-quality scalar (point-error sweep 0…0.1 m;
  ball-speed sweep; `Handling` sweep; `OneVsOne` state on/off).
- 5.1.5 Band-to-action mapping (each band boundary exercised;
  `Ball.SetPossessor` invoked iff `Caught`; `Ball.ApplyKick`
  invoked iff `Parried`/`Deflected`/`Spilled`; F-01..F-03 paths
  exercise `failureCause` enum).
- 5.1.6 Cross-claim duel (2-way GK vs. striker; 3-way GK + 2
  strikers; tiebreaker invocation count; iteration-order
  determinism; head-vs-hand routing per KD-14).
- 5.1.7 Rush dispatch (commit threshold; abort on
  `BallIntercepted`; per KD-15 non-abort under ball-trajectory
  change).
- 5.1.8 Distribution generation (each `deliveryKind`; release-
  point geometry; `PassIntent` mapping correctness).
- 5.1.9 Failed-save emission (each F-01..F-10 cause).

### 5.2 Integration Tests

- 5.2.1 Open-play save from a Shot Mechanics #6 shot (consumes
  `ShotExecutedEvent`).
- 5.2.2 Free-kick save (set-piece pathway, KD-19).
- 5.2.3 Penalty save.
- 5.2.4 Corner cross-claim — hand-contact path (cross-claim duel,
  §3.6).
- 5.2.5 Corner cross-claim — head-contact path (routes to Heading
  #10, KD-14).
- 5.2.6 1v1 confrontation (`OneOnOne` state; `OneVsOne` attribute
  effects per KD-20).
- 5.2.7 Mistimed dive → failed save → no ball state change.
- 5.2.8 GK rush + abort on interception.
- 5.2.9 Save + distribution: full cycle from
  `ShotExecutedEvent` to `DistributionExecutedEvent`; verify
  Pass Mechanics #5 receives valid `PassIntent`.
- 5.2.10 Deterministic replay: 1000-tick scenario producing
  identical `SaveAttemptedEvent` /
  `DistributionExecutedEvent` sequence across runs.
- 5.2.11 #12 baseline-slot ratification: with #12 v1.0.x patch
  applied (post-`IN REVIEW` transition), GK reads baseline from
  #12 and applies KD-13 reactive radius correctly.

### 5.3 Validation Scenarios (match-feel)

- 5.3.1 22-agent match peak: 90-minute simulation with ~4 shots
  on target per side (~8 total per match per published Opta /
  StatsBomb baselines); verify handling-label distribution
  (`Caught` / `Parried` / `Deflected` / `Spilled` / `Missed`
  ratios match a designer-set target).
- 5.3.2 Reflex A/B: same shot trajectory, two GK profiles
  (`Reflexes` 60 vs. 90) → measurable
  `reactionWindowAchieved` divergence.
- 5.3.3 Fatigue gradient: dive peak-Z at fatigue=0.0 vs. 1.0 →
  ~15% reach reduction (validation against KD-8 plus
  `DIVE_FATIGUE_PEAK_Z_COEFF`).
- 5.3.4 1v1 conversion rate: striker through-ball into 1v1; verify
  conversion rate of ~30% matches published 1v1 baseline (Opta;
  cited in §8).

### 5.4 Cross-Spec Conformance Tests

- 5.4.1 No `SaveType` / `SaveClass` / `SaveOutcome` symbol exists
  in `src/` (grep gate; KD-1). This gate is #11-specific.
- 5.4.2 Every constant in `GoalkeeperConstants.cs` has a source
  tag comment (KD-9; programmatic verification per #20 §3.4).
- 5.4.3 Every RNG call uses
  `DeterministicRng.NextFloat(drawSiteId)` or
  `DeterministicRng.NextGaussian(drawSiteId)` (KD-7).
- 5.4.4 #12 GK constant promotion atomic with #11 IN REVIEW: a
  scripted gate verifies that at the commit moment of the #11
  status flip, `positioning-ai/section-3.md` GK constants carry
  `[GT]` and `section-6.md` rows agree. (KD-13.)

---

## SECTION 6 — PERFORMANCE ANALYSIS & BUDGETS (`section-6.md`)

**Subsection target length:** ~150 lines.

### 6.1 Per-Tick Budget

**Steady-state budget**: ≤30 µs per 60 Hz physics tick at 22-agent
match peak under non-save-frame load.
**p99 save-frame tail budget**: ≤220 µs at save-resolution frames.
**p99 cross-claim duel-frame tail budget**: ≤280 µs at 3-way duel
frames.

Justification (component decomposition in §6.3; matches §4.5
exactly; mirrors Heading #10 H-4 reconciliation): only 2 GK agents
per match (vs. 22 outfielders), so steady-state cost is dominated
by state-machine evaluation (per 10 Hz tick at amortised 60 Hz
cost ≈ 5 µs/GK) + reactive-position micro-update (≈10 µs/GK at
60 Hz). p99 tail covers dive kinematics, hand-ball contact
resolution, handling-quality computation, and (for the duel
case) the multi-attacker tiebreak arithmetic.

### 6.2 Hot-Path Allocation Discipline

- No `new` in formula files.
- `ReadOnlySpan<>` for #3 contact-event consumption.
- Struct return types for all intent payloads.
- Cite #18 Appendix F.0 channel-registry and 0-byte budget.

### 6.3 Scaling Analysis

- 22-agent match peak: 2 GK steady-state evaluations + per-shot
  reaction-pipeline trigger (~8 shots-on-target per match per
  validation §5.3.1).
- p99 save frames: ≤8 per match (each 'on target' shot triggers
  full pipeline); estimated worst-case cost ~220 µs at the
  save-resolution frame.
- p99 cross-claim duel frames: estimated ≤0.5 per match minute
  (~15 crosses per match per Opta baselines; ~30% contested by
  GK; ≤4–5 duels per 90 min; ≈0.05/min — much less frequent than
  Heading #10's duel rate per agent, but each is more expensive
  because of the head-vs-hand routing decision and the
  GK-specific handling pipeline). Cost ~280 µs.
- Folds back into §6.1 p99 budgets.

### 6.4 Profiling Compliance (KD-6 of #18)

- Determinism-aware profiling hooks at §3.5 entry,
  §3.6 entry, §4.3 emission.
- Trace channel allocations declared in §2.4 (channel rows
  back-propped to #18 Appendix F.0 at Stage 0+1 per Heading #10
  OI-002 closure precedent).

### 6.5 Stage 0 → Stage 1 Performance Migration Notes

Single paragraph: Fixed64 binding deferred to Stage 5+ (Spec #9
§8.1); `float` is canonical at Stage 0. Dive Z kinematics retire
to AM #2 §3.6 native Z kinematics at Stage 1+ (KD-12 / §7.5).

---

## SECTION 7 — FUTURE EXTENSIONS & STAGE 1+ DEFERRALS (`section-7.md`)

**Subsection target length:** ~110 lines.

Each deferral has: ID, statement, rationale, candidate Stage.

- 7.1 Concussion / injury accumulation (KD-17) — gated on Medical
  spec.
- 7.2 Substitution dynamics (which GK comes off, when) — Stage 1+
  match-management spec.
- 7.3 Yellow / red card discipline (handling outside box; foul
  dynamics during sweep) — Stage 1+ Discipline spec.
- 7.4 Dive-attribute scaling of `DIVE_PHASE_DURATION_MS` —
  Stage 1+ when validation data justifies attribute-driven
  duration (currently flat `[GT]` per §3.3).
- 7.5 AM #2 native Z kinematics — Stage 1+ (KD-12). When AM #2
  publishes a vertical-axis kinematic surface, retire the
  #11-owned synthetic dive trajectory and read apex-frame
  `agentZ` from AM #2 instead; introduce
  `GroundedReason.DIVING_SAVE` as a non-behavioral patch at that
  time.
- 7.6 GK-specific footwork (set-position shuffle animation
  granularity below the 60 Hz physics tick) — Stage 2+ animation
  spec.
- 7.7 Penalty-saving specialism (the "save side" diving cue;
  shooter-eye-tracking analog) — Stage 1+ when Perception System
  #7 grows finer-grain attention modeling.
- 7.8 Sweeper-keeper tactical role (extreme outfield-style
  positioning under high defensive line) — Stage 1+ Tactical-
  Identity spec.
- 7.9 Distribution-side risk model (short-pass-under-press vs.
  long-clear-to-channel) — Stage 1+ Decision Tree extension; the
  *physics* of distribution remains owned here even when the
  risk model elaborates.
- 7.10 Multi-attacker 1v1 (e.g. 2v1 break) — Stage 1+; at Stage 0
  the §3.6 cross-claim duel mechanism is reused as an
  approximation.

---

## SECTION 8 — REFERENCES, CITATIONS, DOI VERIFICATION (`section-8.md`)

**Subsection target length:** ~85 lines.

### 8.1 Project Documents Cited

- `CLAUDE.md` (coordinate, fatigue, tick-rate, constant-tag
  invariants).
- `SPEC_INDEX.md` (numbering authority).
- `docs/tracking/spec-error-log.md` (ERR-011-001 back-prop entry;
  ERR-012-001 collision-management policy reference).

### 8.2 Upstream Specs Cited (section-level)

Table: spec #, section/subsection, citation purpose. ~22 rows
covering #1 §1.2, §3.1.11.2, possession surface; #2 §3.1.2 /
§3.5.1 / §3.5.6; #3 §3.4.2 `ICollisionEventConsumer`; #4 §1.2
(boundary statement); #5 §3.x `PassIntent` surface; #6 §4.5
`ShotExecutedEvent` / §1.3 KD-6; #7 §3.x perception latency; #8
§1.7.x intent surface; #10 §3.7 duel mechanism / KD-7 GK head
ownership; #12 §3.3.3 GK baseline; #16 §3.2 / §3.4 / §4.1 / §4.5;
#17 §3.2.1; #18 Appendix F.0 / §6; #19 §3.x test framework APIs;
#20 §3.x constant-tag verification.

### 8.3 External References (Academic / Empirical)

Pre-identified anchor set (DOI verification during `section-8.md`
authoring; named at outline stage so §9 audit does not surface a
sparseness finding, mirroring Heading #10 pass-1 L-6):

- Dicks, Davids & Button (2010) — visual perception and action in
  one-on-one goalkeeping (relevant for §3.2 reaction model;
  *Journal of Sport & Exercise Psychology*).
- Savelsbergh, Williams, van der Kamp & Ward (2002) —
  anticipation skill in penalty saves (relevant for §3.2
  `Reflexes` modulation; *Journal of Sports Sciences*, DOI
  [10.1080/026404102320183319](https://doi.org/10.1080/026404102320183319)).
- Spratford, Mellifont & Burkett (2009) — biomechanics of
  goalkeeper diving (relevant for §3.3 dive launch impulse and
  peak reach; *Journal of Sports Sciences*).
- Suzuki, Togari, Isokawa, Ohashi & Ohgushi (1988) — analysis of
  goalkeeping motion (relevant for §3.5 hand-ball contact
  geometry baselines).
- Opta / StatsBomb shots-on-target / saves-per-match baseline
  (commercial-data baseline class per Heading #10 §9.6 retention
  pattern; modern empirical baseline for §5.3.1 / §6.3
  save-frequency and cross-claim-frequency targets).
- Williams & Burwitz (1993) — advance cue utilization in
  goalkeeping (relevant for §3.2 anticipation modeling).

### 8.4 Typed Cross-References

Allocated IDs (`XC-011-NNN`, `FM-011-NNN`, `EC-011-NNN`):

- `XC-011-001` — to Ball Physics #1 §1.2 coordinate origin.
- `XC-011-002` — to Ball Physics #1 §3.1.11.2 `Ball.ApplyKick`
  surface.
- `XC-011-003` — to Shot Mechanics #6 §4.5 `ShotExecutedEvent`.
- `XC-011-004` — to Heading #10 KD-7 / §3.7 (GK head-contact
  ownership inversion).
- `XC-011-005` — to Positioning AI #12 §3.3.3 (GK baseline
  consumer contract; ratification of three GK constants).
- `XC-011-006` — to Determinism #16 §3.4 `DOMAIN_TAG_GOALKEEPER`
  catalogue row (`0x17` per KD-7).
- `XC-011-007` — to Event System #17 §3.2.1 publish API.
- `XC-011-008` — to Pass Mechanics #5 §3.x `PassIntent` surface
  (KD-6).
- `XC-011-009` — to Collision System #3 §3.4.2
  `ICollisionEventConsumer` pattern (KD-5).
- `XC-011-010` — to Perception System #7 §3.x visibility latency.
- `FM-011-001` — `requiredReactionMs` (§3.2).
- `FM-011-002` — `reactionWindowAchieved` (§3.2).
- `FM-011-003` — `handlingQualityScalar` (§3.5).
- `FM-011-004` — `diveLaunchMps` / `peakHandZ` (§3.3).
- `FM-011-005` — `crossClaimDuelScore` (§3.6).
- `EC-011-001..010` — F-01…F-10 from §2.3.

---

## SECTION 9 — APPROVAL CHECKLIST (`section-9-approval-checklist.md`)

**Subsection target length:** ~160 lines.

### 9.1 Constant-Tag Verification (KD-9)

Per-constant programmatic check: every entry in
`GoalkeeperConstants.cs` mirror (or §3.4 master table) has
exactly one tag in `{[GT], [EST], [FIXED], [DERIVED], [CROSS],
[CROSS-PENDING]}`. Verification is grep-based; no fabricated
checklist entries.

### 9.2 Cross-Spec Reference Verification

Every `XC-011-NNN` resolves to a specific section in the named
spec. Verification: grep target spec; section must exist.

### 9.3 Sign-Off Requirements

- Lead-developer sign-off.
- Physics-owner sign-off (hand-ball contact geometry, dive
  kinematics).
- Determinism-owner sign-off (KD-7 governance: 4 draw-site IDs,
  iteration order, `DOMAIN_TAG_GOALKEEPER` allocation +
  collision-management with ERR-012-001).
- Positioning AI #12 owner co-sign for §3.3.0 consumer contract
  and the atomic `[EST]` → `[GT]` promotion in #12.

### 9.4 Outstanding Items at Approval Time

- `DOMAIN_TAG_GOALKEEPER` `[CROSS-PENDING]` → `[CROSS]` atomic
  with #16 back-prop ERR-011-001; collision-management per KD-7.
- Trace channel rows allocated in #18 Appendix F.0 at Stage 0+1.
- Atomic patch revision to Positioning AI #12 §3.3.3 / §6
  promoting three GK constants `[EST]` → `[GT]` (KD-13).
- AM #2 `GroundedReason.DIVING_SAVE` enum addition — Stage 1+
  cleanup item (§7.5); not blocking #11 approval since §3.3
  re-uses `DIVING_HEADER` at Stage 0.

### 9.5 Cross-Spec Re-Audit (pre-`APPROVED`)

Verify against APPROVED versions of #1, #2, #3, #4, #5, #6, #7,
#8, #10, #16, #17 — and IN REVIEW version of #12 — that no
upstream surface cited has shifted between draft start and
approval. Particular attention to #12 §3.3.3 (the GK baseline
formula) because the §3.3.0 consumer contract depends on its
exact computational shape.

### 9.6 Post-Approval Follow-ups (not gating)

- Comprehensive audit (per Decision Tree #8 precedent); not
  required for APPROVED transition.
- AM #2 `GroundedReason.DIVING_SAVE` enum addition at Stage 1+
  (KD-12 / §7.5).
- DOI verification for §8.3 external references — six anchors
  named in v1.0; verification pending `section-8.md` authoring.
- Defensive AI #14 / Attacking AI #15 integration verification
  once those specs reach IN REVIEW.

---

## APPENDICES (`appendices.md`)

**Target length:** ~300 lines total.

### Appendix A — Derivations

- A.1 `requiredReactionMs` derivation from Perception #7 base
  latency + GK reflex modulation; sensitivity to ball-speed term.
- A.2 `handlingQualityScalar` linearity proof; band-to-action
  mapping closed-form proofs of velocity-retention monotonicity.
- A.3 Dive launch impulse derivation (work-energy from
  `Strength` to launch velocity; first-principles ablation).
- A.4 Cross-claim duel score sensitivity (analog of Heading #10
  Appendix A duel derivation).

### Appendix B — Sensitivity Tables

- B.1 `requiredReactionMs` over `Reflexes` × ball-speed grid
  (11 × 11).
- B.2 `peakHandZ` over `Aerial` × `Strength` × fatigue grid.
- B.3 `handlingQualityScalar` over `Handling` × ball-speed ×
  point-error grid (3-D heatmap).
- B.4 Cross-claim duel-score `Balance × Strength × Aerial`
  ranking sensitivity.

### Appendix C — Exemplar GK Tuning Profiles

Three preset GK style profiles (sweeper-keeper high-aerial
specialist, classic reactive shot-stopper, balanced modern
keeper). Each profile sets the `[GT]` constants in §3.4 to
feel-target values. Profiles are illustrative; designer-authored
values supersede at Stage 1+. Mirrors Heading #10 Appendix C
pattern.

### Appendix D — Glossary

`GK_SAVE_VOLUME`, `SaveIntent`, `ClaimIntent`, `DistributeIntent`,
`RushIntent`, `HandlingQualityScalar`, `ReactionWindowAchieved`,
`SaveAttemptedEvent`, `BallClaimedEvent`,
`DistributionExecutedEvent`, `GoalkeeperRushEvent`,
`CrossClaimDuelContext`, `Resting` / `Set` / `Anticipate` /
`Diving` / `Airborne` / `HandsOnBall` / `Recovering` /
`Distributing` / `Rushing` / `OneOnOne` / `Smothered` states.

### Appendix E — Mapping Table to v0.1 Adversarial Review Findings

Two-column table: finding number (1–13 from `outline.md`
adversarial-review appendix) → resolution location in this
detailed outline (KD-N or section ID). Used by §9 to
programmatically confirm every finding is addressed.

### Appendix F — Mapping Table to Pass-1 Review Findings (v1.0 → v1.1)

Two-column table covering the 18 findings of
`outline-detailed-pass-1-review.md` (4 H / 8 M / 6 L):

| Finding | Severity | Resolution location |
|---------|----------|---------------------|
| H-1 asymmetric reaction tolerance | HIGH | KD-18; §3.2 piecewise formula; §3.4 keeps `REACTION_EARLY_TOLERANCE_MS` and `REACTION_LATE_TOLERANCE_MS` as distinct `[GT]` rows |
| H-2 KD-3 boundary with #12 was a hand-wave | HIGH | KD-3 sharpened with state-machine-defined boundary; KD-13 added with explicit ratification protocol; §3.3.0 consumer contract added |
| H-3 distribution kick coupling to Pass Mechanics #5 unclear | HIGH | KD-6 sharpened (release geometry here; trajectory there); KD-16 added; §3.8 `mapToPassMechanicsDelivery` specified |
| H-4 dive Z-kinematics missing AM #2 boundary | HIGH | New KD-12 (mirrors Heading #10 KD-18); §3.3 owns synthetic dive trajectory at Stage 0; §7.5 deferral when AM #2 grows Z kinematics; `GroundedReason.DIVING_HEADER` re-use at Stage 0 with telemetry disambiguation; AM #2 amendment Stage 1+ |
| M-1 RNG draw sites not enumerated | MEDIUM | §4.4 lists 3 draw sites at v1.1 (expanded to 4 at v1.2 per L-5); each wired to specific §3.X caller |
| M-2 reaction-time model citation to Perception #7 absent | MEDIUM | KD-2 cites #7 §3.x; §3.2 formula consumes `PERCEPTION_BASE_LATENCY_MS` `[CROSS]` |
| M-3 Positioning AI #12 ratification mechanism unspecified | MEDIUM | KD-13; §3.3.0 consumer contract; §9.4 lists atomic patch revision as outstanding item |
| M-4 cross-claim head-vs-hand routing ambiguity | MEDIUM | KD-14; §3.6 step 1 (body-part determination); step 2 routing via #10 §3.7 for head, §3.6 here for hand |
| M-5 rush abort policy undefined | MEDIUM | KD-15; §3.7 implements; F-08 in §2.3; `GoalkeeperRushEvent.abortReason` field added |
| M-6 distribution release geometry ownership | MEDIUM | KD-16; §3.8; `mapToPassMechanicsDelivery` documented |
| M-7 set-piece scope unclear | MEDIUM | KD-19 (set-piece saves IN scope; wall NOT — owned by #14) |
| M-8 §3 constants not inventoried against §3.4 | MEDIUM | §3.4 expanded to ~50 rows; every §3.2–§3.8 symbol either tabled or named per-call output |
| L-1 concussion / discipline absence | LOW | KD-17 explicit deferral with rationale; §7.1 / §7.3 captures |
| L-2 `OneVsOne` attribute use unspecified | LOW | KD-20; closed-form coefficient in §3.5 `attrFactor`; no 1v1 physics branch |
| L-3 band-to-action mapping ambiguity at boundaries | LOW | KD-21 explicit `Caught` vs. `Parried` toggle between two `Ball.*` APIs; other bands all `Ball.ApplyKick` |
| L-4 6-second-rule constant not classified | LOW | `GK_HOLD_MAX_TICKS` tagged `[FIXED]` (rule constant, not designer tuning) per §3.4 |
| L-5 `DRAW_SITE_HANDLING_NOISE` shared between two error sources | LOW | v1.2 splits into `DRAW_SITE_HANDLING_NOISE` and `DRAW_SITE_HANDLING_POINT_NOISE` per §16 §4.5 single-purpose-per-site rule; §3.5 and §4.4 updated |
| L-6 §8.3 anchor sparseness | LOW | Six external references named at outline stage (Dicks et al., Savelsbergh et al., Spratford et al., Suzuki et al., Williams & Burwitz, Opta/StatsBomb) per Heading #10 L-6 precedent |

### Appendix G — Mapping Table to Pass-2 Review Findings (v1.1 → v1.2)

Two-column table covering the 5 findings of
`outline-detailed-pass-2-review.md` (0 H / 2 M / 3 L):

| Finding | Severity | Resolution location |
|---------|----------|---------------------|
| P2-M-1 §3.5 contact-point noise shares `DRAW_SITE_HANDLING_NOISE` with handling-scale noise | MEDIUM | Promoted to L-5 fix in v1.2: separate `DRAW_SITE_HANDLING_POINT_NOISE` added; §4.4 draw-site count 3 → 4 |
| P2-M-2 `Ball.SetPossessor` surface presumed but not verified against #1 | MEDIUM | §4.3 explicit note: presumed published per ERR-008 resolution; back-prop entry filed if absent; OPEN-ITEMS row added |
| P2-L-1 KD-12 dual reference to `GroundedReason.DIVING_HEADER` re-use AND new `DIVING_SAVE` value | LOW | KD-12 simplified: Stage 0 re-uses `DIVING_HEADER` (no AM #2 amendment); Stage 1+ cleanup adds `DIVING_SAVE` via §7.5 deferral |
| P2-L-2 `FR-GK-026` references atomic resolution but mechanism unclear | LOW | FR text refined: resolution is atomic with #16 back-prop landing; #11 status flip is separately atomic for #12 GK constants |
| P2-L-3 §6.3 cross-claim duel-rate cited "per Opta baselines" without specific cross-ref | LOW | §8.3 cross-references the Opta/StatsBomb commercial-data class explicitly; §6.3 anchors to §8.3 |

---

## OPEN-ITEMS TRACKER

Status at outline-detailed v1.2:

| ID | Item | Owner | Status |
|----|------|-------|--------|
| OI-001 | `DOMAIN_TAG_GOALKEEPER = 0x17` allocation in #16 §3.4 | back-prop ERR-011-001 | pending — to be filed when `section-3.md` lands; precedent: #10 `DOMAIN_TAG_HEADING = 0x16` patch; collision-management with ERR-012-001 per KD-7 |
| OI-002 | `#18 Appendix F.0` trace channel rows for `gk.*` channels | back-prop | pending — to be filed when `section-2.md` §2.4 lands; Stage 0+1 delivery schedule per Heading #10 OI-002 precedent |
| OI-003 | DOI verification for §8.3 external references (six anchors named in v1.0) | drafter | pending |
| OI-004 | Pin #3 `ICollisionEventConsumer` exact reference in §3.4.2, #5 `PassIntent` exact §3.x anchor, #7 perception-latency exact §3.x anchor, #8 GK-branch intent surface exact §1.7.x anchor | drafter | pending — pin during `section-1.md` authoring |
| OI-005 | Atomic patch revision to Positioning AI #12 §3.3.3 / §6 promoting three GK constants `[EST]` → `[GT]` | coordinated with #12 owner | gated on #11 IN REVIEW transition; KD-13 |
| OI-006 | Verify `Ball.SetPossessor` surface exists in Ball Physics #1 §3.1; if absent, file ERR-011-002 for non-behavioral patch | drafter | pending — pin during `section-1.md` / `section-4.md` authoring |
| OI-007 | AM #2 `GroundedReason.DIVING_SAVE` enum value addition | Stage 1+ cleanup | not blocking #11 sign-off; §7.5 deferral; KD-12 |
| OI-008 | `certification-platform.md` Stage-0 host pin | lead developer | not blocking #11 spec sign-off; blocks `FR-PO-052` perf-gate activation only (shared with #10 OI-006) |

---

## VERSION HISTORY

| Version | Date | Author | Notes | Reviewer |
|---------|------|--------|-------|----------|
| 0.1 | May 6, 2026 | initial | `outline.md` high-level; 13-finding adversarial review attached | (review applied in this doc) |
| 1.0 | May 16, 2026 (late evening) | this document | Detailed outline supersedes v0.1; 13 v0.1 findings all resolved via KD-1…KD-11 + section-plan remap; dependencies fully enumerated; output interfaces defined; ready for section-file authoring | self-pass-1 review in `outline-detailed-pass-1-review.md` |
| 1.1 | May 16, 2026 (late evening, later) | this document | Resolves all 18 pass-1 adversarial review findings (4 H / 8 M / 6 L) per `outline-detailed-pass-1-review.md`; adds KD-12 (Stage 0 dive kinematics), KD-13 (Positioning AI #12 ratification protocol via §3.3.0 consumer contract), KD-14 (cross-claim head/hand routing), KD-15 (rush abort policy), KD-16 (distribution release-point ownership), KD-17 (concussion/discipline deferral), KD-18 (asymmetric reaction tolerance), KD-19 (set-piece scope), KD-20 (`OneVsOne` closed-form), KD-21 (band-to-action mapping); §3.4 expanded ~30 → ~50 rows with full M-8 inventory closure; §3.3 owns Stage 0 dive trajectory (KD-12 mirrors Heading #10 KD-18); §3.3.0 added (consumer contract); §3.6 head/hand routing tagged and consumed; §3.8 distribution-release geometry detailed; F-08..F-10 added; six §8.3 academic anchors named; Appendix F mapping table added | self-pass-2 review in `outline-detailed-pass-2-review.md` |
| 1.2 | May 16, 2026 (late evening, latest) | this document | Resolves all 5 pass-2 review findings (0 H / 2 M / 3 L); promotes P2-M-1 to L-5 (new fourth draw site `DRAW_SITE_HANDLING_POINT_NOISE`); KD-12 simplified to single Stage 0 path (re-use `DIVING_HEADER`); §4.3 `Ball.SetPossessor` verification posture explicit; Appendix G mapping table added; OI-006 added | pending |
