# Goalkeeper Mechanics Specification #11 — Section 2: Functional Requirements, Data Structures & Failure Modes

**Created:** May 16, 2026
**Version:** 0.1
**Status:** DRAFT
**Purpose:** Enumerate the functional requirements (FRs), publish the
data structures, catalogue the failure modes, and declare the
telemetry surface for Goalkeeper Mechanics #11. Section numbering
follows the CLAUDE.md spec template (§2 = FRs / data / failure /
telemetry).

---

## 2.1 Functional Requirements Catalogue

Conformance levels follow RFC 2119 (`MUST` / `SHOULD` / `MAY`).
Source KDs are declared in §1.3.

| FR | Conformance | Statement | Source KD | Target subsection |
|----|-------------|-----------|-----------|-------------------|
| FR-GK-001 | MUST | A save is eligible iff the GK state machine is in {`Set`, `Anticipate`, `Diving`, `Airborne`, `OneOnOne`} AND the ball is within `GK_SAVE_VOLUME_RADIUS_M` of any predicted hand position within the dive-reach envelope at any frame in the next `REACTION_LATE_TOLERANCE_MS` window. | KD-1, KD-12 | §3.1 / §3.5 |
| FR-GK-002 | MUST | `HandlingQualityScalar` is computed as a continuous scalar in `[0, 1]`; physics code does NOT branch on `Caught` / `Parried` / `Deflected` / `Spilled` labels. | KD-1, KD-21 | §3.5 |
| FR-GK-003 | MUST | No `SaveType` / `SaveClass` / `SaveOutcome` enum exists at any layer (formula, data, public API, telemetry). The label fields on telemetry events are post-computation tags, NOT inputs to physics. | KD-1 | §3.5 / §4 |
| FR-GK-004 | MUST | GK head contacts route to Heading Mechanics #10 §3.7; no #11-local head-physics path exists. | KD-4 | §3.6 / §3.10 |
| FR-GK-005 | MUST | GK resting baseline position is consumed read-only from Positioning AI #12 §3.3.3 via `PositioningAI.GetGKBaselineSlot(matchTime) → Vector2`. | KD-3 | §3.3.0 / §4.2 |
| FR-GK-006 | MUST | GK reactive position (set / shuffle / narrow / cross-claim / sweep / recovery) is owned by Spec #11. Reactive radius is bounded by `GK_REACTIVE_RADIUS_M = 1.5 m` `[GT]` around the #12-supplied baseline while in `Resting` / `Set`. | KD-3, KD-13 | §3.3.0 |
| FR-GK-007 | MUST | Distribution emits a Pass Mechanics #5 `PassIntent`-equivalent payload via the existing #5 intent surface; no #5 amendment is required. | KD-6 | §3.8 |
| FR-GK-008 | MUST | Distribution release-point geometry (release height, launch angle range, windup duration) is owned by Spec #11 §3.8. | KD-16 | §3.8 |
| FR-GK-009 | MUST | A failed save (`failureCause ∈ {MissedContact, MistimedDive, WrongDirection, OutOfReach, DisturbedInDuel}`) produces NO `Ball.ApplyKick` and NO `Ball.SetPossessor`; ball trajectory is unchanged; `SaveAttemptedEvent` is emitted with `failureCause` populated. | KD-11 | §3.9 |
| FR-GK-010 | MUST | All randomness routes through `DeterministicRngService` with registered draw-site IDs (`DRAW_SITE_HANDLING_NOISE`, `DRAW_SITE_HANDLING_POINT_NOISE`, `DRAW_SITE_DIVE_TIMING_JITTER`, `DRAW_SITE_CROSS_CLAIM_TIEBREAK`). | KD-7 | §3.3 / §3.5 / §3.6 / §4.4 |
| FR-GK-011 | MUST | Save physics consumes Collision System #3 contact data via `ICollisionEventConsumer` (#3 §3.4.2); contact normal, relative velocity, and impulse budget are NOT redefined locally. | KD-5 | §3.5 / §3.6 |
| FR-GK-012 | MUST | Fatigue convention `0.0 = rested, 1.0 = fatigued`. | KD-8, KD-10 | §3.3 / §3.5 / §3.7 |
| FR-GK-013 | MUST | Spec #11 uses corner-origin coordinates per Ball Physics #1 §1.2 (`0 ≤ x ≤ 105 m`, `0 ≤ y ≤ 68 m`, `z ≥ 0`). | KD-10 | §3 |
| FR-GK-014 | MUST | Tick-rate split: state-machine transitions and intent selection at 10 Hz tactical loop; dive kinematics, hand-ball contact resolution, ball-velocity emission at 60 Hz physics loop. | KD-10 | §3.1 / §3.3 / §4.6 |
| FR-GK-015 | MUST | Every published constant carries exactly one source tag in {`[GT]`, `[EST]`, `[FIXED]`, `[DERIVED]`, `[CROSS]`, `[CROSS-PENDING]`}. | KD-9 | §3.4 / §9.1 |
| FR-GK-016 | MUST | Set-piece saves (free-kicks, penalties) are processed by the same shot-reaction pipeline as open-play saves; the defensive wall is out of scope (Defensive AI #14). | KD-19 | §3.2 / §5.2 |
| FR-GK-017 | MUST | `ReactionWindowAchieved` uses asymmetric tolerance constants: `REACTION_EARLY_TOLERANCE_MS` and `REACTION_LATE_TOLERANCE_MS`, with `REACTION_LATE_TOLERANCE_MS` numerically smaller. | KD-18 | §3.2 |
| FR-GK-018 | MUST | A committed rush (`commitmentLevel > RUSH_COMMIT_THRESHOLD`) is not abortable on the basis of ball-trajectory changes; the sole abort condition is `BallIntercepted` (a non-GK agent becomes `BallState.PossessorId`). | KD-15 | §3.7 |
| FR-GK-019 | MUST | Telemetry labels (`Caught` / `Parried` / `Deflected` / `Spilled` / `Missed` / `Reflexive` / `Standard` / `Sluggish`) are emitted exclusively post-formula and are never read by physics. | KD-1, KD-2, KD-21 | §2.4 / §3.5 |
| FR-GK-020 | MUST | Stage 0 dive kinematics (XY launch + synthetic Z arc + ground recovery) are owned by Spec #11 §3.3 per KD-12; AM #2 §3.6 is not amended. | KD-12 | §3.3 |
| FR-GK-021 | MUST | §3.3.0 publishes the Positioning AI #12 consumer contract; on #11's `IN REVIEW` transition, #12 §3.3.3 GK constants promote `[EST]` → `[GT]` via a #12 patch revision. | KD-13 | §3.3.0 / §9.4 |
| FR-GK-022 | MUST | Cross-claim contact routing is determined by Collision System #3 contact-event body part (head → #10 §3.7; hand → §3.6 here), NOT by intent. | KD-14 | §3.6 |
| FR-GK-023 | MUST | The `Caught` vs. `Parried` band boundary toggles between `Ball.SetPossessor` (above `CATCH_THRESHOLD`) and `Ball.ApplyKick` (between `PARRY_THRESHOLD` and `CATCH_THRESHOLD`); the `Parried` / `Deflected` / `Spilled` boundaries all resolve via `Ball.ApplyKick` and differ only in outgoing-velocity magnitude and angle. | KD-21 | §3.5 |
| FR-GK-024 | MUST | The `OneVsOne` attribute participates in closed-form coefficients in §3.5 `attrFactor` and §3.2 `requiredReactionMs` gated on state `OneOnOne`; no alternative formula path exists for 1v1 saves. | KD-20 | §3.2 / §3.5 |
| FR-GK-025 | MUST | Iteration over multi-attacker cross-claim duels follows #16 §3.2 entity order. | KD-7 | §3.6 |
| FR-GK-026 | MUST | `DOMAIN_TAG_GOALKEEPER` is allocated `[CROSS-PENDING]` until #16 §3.4 ratifies the value. Promotion to `[CROSS]` is atomic with #16's back-prop landing (ERR-011-001) and #11's `IN REVIEW → APPROVED` transition. The literal value is `0x17` if ERR-011-001 lands before ERR-012-001; `0x1D` if ERR-012-001 lands first. | KD-7 | §3.4 / §4.4 / §9.4 |
| FR-GK-027 | MUST | State-machine transitions are deterministic; no `System.Random` or `DateTime.Now` paths exist in any GK code path. | KD-7, CLAUDE.md | §3.1 |
| FR-GK-028 | MUST | The `GK_HOLD_MAX_TICKS` constant enforces the 6-second hand-hold rule (60 ticks at 10 Hz); tagged `[FIXED]` as a Laws-of-the-Game constant, not a designer tuning. | KD-9, §3.4 | §3.4 / §3.8 |
| FR-GK-029 | MUST | `Ball.SetPossessor(gkId)` invocation parks the ball at the GK's hand position with zero velocity until `releaseTickEarliest` and a subsequent `DistributeIntent` are received. | KD-21 | §3.5 / §3.8 |
| FR-GK-030 | MUST | A non-eligible state (e.g. `Distributing` windup; `Recovering` from a missed dive) when a shot arrives produces NO `SaveAttemptedEvent`; ball-vs-GK contact (if any) is resolved by Collision System #3 standard rebound physics. | KD-11, F-07 | §3.5 / §3.9 |
| FR-GK-031 | MUST | `clutchFirmness` and other `[0, 1]` intent fields are clamped on consumption; out-of-range values emit a telemetry warning but do NOT crash the pipeline. | F-10 | §2.3 / §3.5 |
| FR-GK-032 | MUST | Distribution targeting outside pitch bounds is clamped to the nearest in-bounds point; telemetry warning emitted; NOT a hard failure. | F-09 | §3.8 |
| FR-GK-033 | MUST | A `targetReceiverId` that has left the pitch (substituted between commit and release) falls back to `targetPoint`-based distribution. | F-05 | §3.8 |
| FR-GK-034 | SHOULD | `BallState` snapshots older than one physics frame must be re-queried, not extrapolated. | F-06 | §3.2 / §3.5 |
| FR-GK-035 | MUST | The four publish surfaces (`SaveAttemptedEvent`, `BallClaimedEvent`, `DistributionExecutedEvent`, `GoalkeeperRushEvent`) all route through Event System #17 §3.2.1. | KD-10 | §4.3 |
| FR-GK-036 | MUST | The GK state machine entry into `GROUNDED` after a dive uses `GroundedReason.DIVING_HEADER` at Stage 0 per KD-12; Stage 1+ migrates to `GroundedReason.DIVING_SAVE` as an AM #2 non-behavioral patch (§7.5). | KD-12 | §3.1 / §3.3 |
| FR-GK-037 | MUST | The dive launch impulse formula consumes `Strength_norm` and `Aerial_norm` as primary attribute inputs; both attributes are normalised to `[0, 1]` per AM #2 §3.5.6 attribute conventions. | KD-9 | §3.3 |
| FR-GK-038 | MUST | The handling-quality scalar is convex-blended with `reactionWindowAchieved` via `HANDLING_REACTION_BLEND_ALPHA` `[GT]`; weight sums to 1.0 by construction. | KD-2, KD-9 | §3.5 |
| FR-GK-039 | MUST | The cross-claim duel score uses three weighted attributes (`Balance`, `Strength`, `Aerial`) with `[GT]` weights summing to 1.0 by construction. | KD-14 | §3.6 |
| FR-GK-040 | MUST | Near-tie tiebreak Gaussian noise (`CROSS_CLAIM_TIEBREAK_NOISE_AMPLITUDE` `[GT]`) is applied only when `|scoreA − scoreB| < CROSS_CLAIM_TIEBREAK_EPSILON` `[GT]`. | KD-7, KD-14 | §3.6 |
| FR-GK-041 | MUST | The dive timing jitter Gaussian (`DIVE_TIMING_JITTER_SIGMA_MS` `[GT]`) modulates `peakHandZ_m` linearly via `DIVE_JITTER_PEAK_Z_COEFF` `[GT]`; no other dive-kinematics term is randomly perturbed. | KD-7 | §3.3 |
| FR-GK-042 | MUST | Every constant in §3.4 has a row in §3.4's Master Physical Profile Table with source tag, unit, valid-range, and citation columns populated. | KD-9 | §3.4 / §9.1 |
| FR-GK-043 | MUST | After `GK_HOLD_MAX_TICKS` (6-second rule; 60 ticks at 10 Hz) elapsed without a `DistributeIntent`, the GK forces a default ROLL distribution to the nearest own-team agent within the penalty area. | KD-9 / Laws of the Game | §3.1 / §3.8 |
| FR-GK-044 | MUST | Distribution `powerIntent` is multiplied by `THROW_ACCURACY_COEFF · Throwing_norm` for Throw delivery and by `KICK_ACCURACY_COEFF · Kicking_norm` for Kick delivery; Roll delivery does NOT consume `Throwing` or `Kicking` (low-skill action). | KD-9 / §3.8.1 | §3.8 |

---

## 2.2 Data Structures

All data structures are struct-based and zero-allocation in the
game loop per CLAUDE.md "Struct-based, zero-allocation architecture
in the game loop." All `Vector2` and `Vector3` are in
corner-origin pitch coordinates per #1 §1.2.

### 2.2.1 Intent payloads (consumed from Decision Tree #8 GK branches)

```
struct SaveIntent {
    HandEnum   targetHand;             // Left / Right / Either — parameterises a per-hand reach-geometry lookup, NOT a physics input
    float      clutchFirmness;         // [0, 1]
    Vector3?   deflectionTarget;       // optional target deflection point
    int        attemptCommittedTick;   // 10 Hz tactical tick at which commit occurred
}

struct ClaimIntent {
    Vector3    targetContactPoint;
    float      clutchFirmness;         // [0, 1]
    int        attemptCommittedTick;
}

struct DistributeIntent {
    DeliveryKindEnum  deliveryKind;    // Throw / Roll / Kick — parameterises kinematic profile lookup, NOT physics input
    int?              targetReceiverId;
    Vector3           targetPoint;
    float             powerIntent;     // [0, 1]
    Vector3           spinIntent;
}

struct RushIntent {
    Vector3  rushTarget;
    float    commitmentLevel;          // [0, 1]
    int      attemptCommittedTick;
}
```

**Enum exception note (KD-1 carve-out).** `HandEnum` and
`DeliveryKindEnum` are anatomy / kinematic profile lookups, not
physics-input enums. KD-1 prohibits enums that gate physics
formulas; these two parameterise table-lookups for geometry and
windup duration, neither of which is a physics output. The
prohibition is preserved.

### 2.2.2 State machine

```
enum GoalkeeperState {
    Resting,
    Set,
    Anticipate,
    Diving,
    Airborne,
    HandsOnBall,
    Recovering,
    Distributing,
    Rushing,
    OneOnOne,
    Smothered
}
```

State semantics summarised in §3.1; the full transition table is in
§3.1 with trigger conditions, source spec, target state, and
tick-rate (10 Hz tactical or 60 Hz physics event-driven).

### 2.2.3 Per-frame contact state

```
struct GKContactState {
    int       predictedContactFrame;
    int       actualContactFrame;
    float     reactionWindowAchieved;   // [0, 1]
    float     handlingQualityScalar;    // [0, 1]
    Vector2   contactPointError;        // metres, hand-local coordinates
    HandEnum  handChoice;
    float     clutchFirmness;           // [0, 1]
}
```

### 2.2.4 Emitted events (Event System #17 §3.2.1 publish surface)

```
struct SaveAttemptedEvent {
    int                    agentId;
    long                   matchTime;
    SaveIntent             saveIntent;
    float                  reactionWindowAchieved;
    float                  handlingQualityScalar;
    HandlingQualityLabel   handlingQualityLabel;  // Caught / Parried / Deflected / Spilled / Missed — EMITTED, NOT CONSUMED
    ReactionLabel          reactionLabel;         // Reflexive / Standard / Sluggish — EMITTED, NOT CONSUMED
    Vector3                contactPoint;
    BallState              incomingBallState;
    Vector3                outgoingBallVelocity;
    Vector3                outgoingBallSpin;
    int?                   contestedDuelId;
    FailureCause?          failureCause;          // populated only when handlingQualityLabel == Missed
    BodyPartEnum           contactBodyPart;       // KD-12 telemetry disambiguation
}

struct BallClaimedEvent {
    int          agentId;
    long         matchTime;
    ClaimType    claimType;             // Cross / Aerial / OneOnOne / ShotCatch — telemetry label
    int?         contestedDuelId;
    int          releaseTickEarliest;   // 10 Hz tick at which distribution may begin (honours GK_HOLD_MAX_TICKS)
}

struct DistributionExecutedEvent {
    int                agentId;
    long               matchTime;
    DeliveryKindEnum   deliveryKind;
    int?               targetReceiverId;
    Vector3            targetPoint;
    PassIntent         passIntent;        // payload emitted to Pass Mechanics #5
    Vector3            releasePoint;
    int                windupDurationMs;
}

struct GoalkeeperRushEvent {
    int          agentId;
    long         matchTime;
    Vector3      rushTarget;
    RushPhase    rushPhase;       // Launched / InFlight / Reached / Aborted
    AbortReason? abortReason;     // BallIntercepted / BallCleared / AttackerBeatGK
}
```

### 2.2.5 Cross-claim duel context

```
struct CrossClaimDuelContext {
    int                       duelId;
    ReadOnlySpan<int>         participantAgentIds;
    int                       winnerAgentId;
    BodyPartEnum              contactBodyPart;   // Head → routes to #10 §3.7; Hand → §3.6 resolution here
}
```

### 2.2.6 Enums (telemetry-only)

The following enums are TELEMETRY LABELS emitted on events; they
are NOT inputs to any physics formula (KD-1, KD-2, KD-21):

```
enum HandlingQualityLabel { Caught, Parried, Deflected, Spilled, Missed }
enum ReactionLabel        { Reflexive, Standard, Sluggish }
enum FailureCause         { MissedContact, MistimedDive, WrongDirection, OutOfReach, DisturbedInDuel }
enum ClaimType            { Cross, Aerial, OneOnOne, ShotCatch }
enum RushPhase            { Launched, InFlight, Reached, Aborted }
enum AbortReason          { BallIntercepted, BallCleared, AttackerBeatGK }
enum BodyPartEnum         { Hand, Head, Body, Foot }
```

`HandEnum` and `DeliveryKindEnum` (defined in §2.2.1) are the
KD-1 carve-outs (anatomy / kinematic-profile lookups, not physics
inputs).

---

## 2.3 Failure Modes

Each failure mode has detection conditions, recovery action, and a
source FR / KD. Cross-referenced as `EC-011-NNN` in §8.4.

| ID | Failure mode | Detection | Recovery |
|----|--------------|-----------|----------|
| F-01 | Mistimed dive (ball passed save volume before dive apex) | `actualContactFrame > predictedContactFrame + REACTION_LATE_TOLERANCE_MS / FRAME_MS` | Emit `SaveAttemptedEvent` with `failureCause = MistimedDive`; state machine `Diving → Recovering`. |
| F-02 | Wrong-direction dive | Predicted-contact hand position more than `WRONG_DIRECTION_THRESHOLD_M` from ball trajectory at GK depth | Emit `SaveAttemptedEvent` with `failureCause = WrongDirection`; state machine `Diving → Recovering`. |
| F-03 | Out-of-reach (ball outside GK hand-reach envelope at all candidate frames) | Reach-envelope check fails for every candidate frame in `[currentFrame, currentFrame + REACTION_LATE_TOLERANCE_MS / FRAME_MS]` | Emit `SaveAttemptedEvent` with `failureCause = OutOfReach`; state machine `Anticipate → Recovering`. |
| F-04 | Cross-claim disturbed by another agent | Duel-loser identification per §3.6 step 6 | Losers emit `SaveAttemptedEvent` (or `HeaderAttemptFailedEvent` if head route) with `failureCause = DisturbedInDuel`. NOT a hard failure — the winning agent's outcome event is the canonical record. |
| F-05 | `DistributeIntent.targetReceiverId` no longer on the pitch | Receiver lookup returns null at release frame | Fall back to `targetPoint`-based distribution; emit telemetry warning `gk.distribution.target_receiver_missing`; NOT a hard failure. |
| F-06 | `BallState` snapshot stale (>1 physics frame old) | `BallState.frameStamp < currentFrame` | Re-query Ball Physics #1; do not extrapolate. SHOULD (FR-GK-034). |
| F-07 | GK in non-eligible state when shot arrives (e.g. `Distributing` windup) | Eligibility predicate (FR-GK-001) fails | NO `SaveAttemptedEvent` emitted; ball-vs-GK contact (if any) resolved by Collision System #3 standard rebound physics. State-machine sequencing issue, NOT save quality issue. |
| F-08 | Rush aborted mid-flight by ball interception | `BallState.PossessorId` becomes a non-GK agent during `Rushing` | Emit `GoalkeeperRushEvent` with `abortReason = BallIntercepted`; state machine `Rushing → Recovering`. KD-15. |
| F-09 | Distribution kick targeted outside pitch bounds | `targetPoint.x ∉ [0, 105]` OR `targetPoint.y ∉ [0, 68]` | Clamp `targetPoint` to nearest in-bounds point; emit telemetry warning `gk.distribution.target_out_of_bounds`; NOT a hard failure. |
| F-10 | `clutchFirmness` or other `[0, 1]` intent field out of range | Range check on consumption | Clamp to `[0, 1]`; emit telemetry warning `gk.intent.range_clamp`; NOT a hard failure. |

---

## 2.4 Telemetry Surface

Counters, gauges, and histograms routed to the trace pipeline per
Performance Optimization #18 Appendix F.0 channel-registry schema.
Channel rows are back-propagated to #18 Appendix F.0 at Stage 0+1
per the schedule established by Heading #10 OI-002 closure
precedent.

| Channel | Kind | Buckets / scale | Purpose |
|---------|------|-----------------|---------|
| `gk.save.reaction.window` | Histogram | `[0, 1]`, 10 bins | Distribution of `reactionWindowAchieved` |
| `gk.save.reaction.label` | Counter | 3: `Reflexive` / `Standard` / `Sluggish` | Telemetry-label band counts |
| `gk.save.handling.quality` | Histogram | `[0, 1]`, 10 bins | Distribution of `handlingQualityScalar` |
| `gk.save.handling.label` | Counter | 5: `Caught` / `Parried` / `Deflected` / `Spilled` / `Missed` | Telemetry-label band counts |
| `gk.save.failure.cause` | Counter | 5: `MissedContact` / `MistimedDive` / `WrongDirection` / `OutOfReach` / `DisturbedInDuel` | Failure-mode mix |
| `gk.cross_claim.outcome` | Counter | 3: `Win` / `Loss` / `Disturbed` | Cross-claim resolution mix |
| `gk.cross_claim.body_part` | Counter | 2: `Hand` / `Head` | KD-14 routing distribution |
| `gk.rush.outcome` | Counter | 4: `Reached` / `Aborted` / `Intercepted` / `AttackerBeatGK` | Rush dispatch outcomes |
| `gk.distribution.kind` | Counter | 3: `Throw` / `Roll` / `Kick` | Distribution-mix distribution |
| `gk.distribution.windup_ms` | Histogram | `0–2000 ms`, 20 bins | Windup-duration distribution |
| `gk.state.transition` | Counter | tagged on `(from, to)` pair | State-machine transition audit |
| `gk.dive.peak_z` | Histogram | `0–3 m`, 15 bins | Apex hand-Z distribution |

The full channel-registry rows (subsystem-channel-owner mapping
per #18 Appendix F.0 13-field schema) are populated at first
`src/Gameplay/Goalkeeper/` commit per #18 Appendix F.0 / §7.2
Stage 0+1 deliverable schedule.

---

## 2.5 Version History

| Version | Date | Author | Notes | Reviewer |
|---------|------|--------|-------|----------|
| 0.1 | May 16, 2026 | initial draft | First v0.1 from outline v1.2; 42 FRs catalogued; data structures published; 10 failure modes catalogued; 12 telemetry channels declared | self-pass-1 in `adversarial-review-section-files-v1.md` |
| 0.2 | May 16, 2026 | pass-1 fix pass | AR-S1-M2 (FR-GK-043 forced-release added); AR-S1-M3 (FR-GK-044 `Throwing`/`Kicking` consumption added) — FR count 42 → 44 | self-pass-2 self-critique on v0.2 yields no further findings |
