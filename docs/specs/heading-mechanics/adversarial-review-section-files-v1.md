# Heading Mechanics #10 — Section Files v0.1 Adversarial Review (Pass 1)

**Created:** May 16, 2026
**Reviewer:** Adversarial pass against `section-1.md` … `appendices.md`
at v0.1 (May 16, 2026), authored from `outline-detailed.md` v1.1.
**Scope:** All section files + appendices. The prior pass-1 review
(`outline-detailed-pass-1-review.md`, May 15, 2026) targeted the
detailed outline; this pass targets the section-file realization
of that outline.
**Method:** Read each section end-to-end; cross-checked claims in
`section-N.md` against KDs in `section-1.md`, the master constants
table in `section-3.md` §3.1, FR rows in `section-2.md` §2.1, and
the failure-mode table in §2.3; verified back-prop entries against
`docs/tracking/spec-error-log.md`; verified domain-tag slot against
`docs/specs/deterministic-sim/section-3.md` §3.4; ran arithmetic
checks on every worked example.
**Headline:** v0.1 cleanly inherits the outline-detailed v1.1
structure and the eighteen KDs are reproduced faithfully. However,
**§3.6 contains a spin double-reversal bug in the closed-form
formula**, **§3.4 inverts the stated semantics of
`headingAttrScale`**, the **§3.2 worked example contains an
off-by-one comparison**, and the **`ERR-010-001` back-propagation
entry that KD-10 / OI-001 / Appendix G claim was filed is not
present in `spec-error-log.md`**. A scattering of medium-grade
inconsistencies between §3 algorithms, §2.3 failure modes, and §5
test descriptions also need cleanup before v0.2.

| Severity | Count |
|----------|-------|
| HIGH     | 5     |
| MEDIUM   | 9     |
| LOW      | 7     |
| Total    | 21    |

Recommend a v0.2 fix pass before `SPEC_INDEX.md` row 10 flips
`NOT STARTED → IN REVIEW`. None of the findings invalidate the
KD set; all are concentrated in §3 internal consistency, §5 test
descriptions, and one process item (back-prop not actually filed).

---

## HIGH

### H-1 — §3.6 / Appendix A.3: outgoing-spin formula double-counts the reversal

§3.6 publishes the closed form:

```
spinPreservationFactor = SPIN_PRESERVATION_BASE
                       · (1 - contactPointAxialOffset_m
                              / SPIN_TRANSFER_REVERSAL_THRESHOLD)
reversalTerm           = max(0, -spinPreservationFactor) · incomingSpin
outgoingSpin           = SPIN_TRANSFER_COEFF · headAngularVelocity
                       + (incomingSpin · spinPreservationFactor)
                       - reversalTerm
```

When `contactPointAxialOffset > SPIN_TRANSFER_REVERSAL_THRESHOLD`,
`spinPreservationFactor` is negative, so `(incomingSpin ·
spinPreservationFactor)` is **already** the reversed-sign
contribution. Subtracting an additional `reversalTerm` (which is
`|spinPreservationFactor| · incomingSpin`) then reverses the spin
a second time. The §3.6 worked example demonstrates the bug:

```
factor                 = 0.6 · (1 - 0.02/0.015) = -0.2
incomingSpinTerm       = 8 · (-0.2)             = -1.6 rad/s   (already reversed)
reversalTerm           = 0.2 · 8                =  1.6 rad/s
incomingContribution   = -1.6 - 1.6             = -3.2 rad/s   (DOUBLED)
```

Intent (per §3.1 row note for `SPIN_TRANSFER_REVERSAL_THRESHOLD`
"contact-point axial offset beyond which `spinPreservationFactor`
goes negative (spin reverses)") is that crossing the threshold
flips the sign once. The current formula flips it twice. Appendix
A.3 reproduces the same buggy derivation verbatim.

**Fix.** Drop the explicit `- reversalTerm` subtraction — the
`(incomingSpin · spinPreservationFactor)` term already carries the
sign flip — and remove the redundant `reversalTerm` definition.
Re-derive the §3.6 worked example: `incomingContribution = 8 ·
(-0.2) = -1.6 rad/s`. Update the §5.1.5 reversal-boundary test
expectation accordingly. Update Appendix A.3.

Severity HIGH because the formula is published as `FM-010-004`
and Appendix A.3 explicitly treats it as a derivation — the bug
will propagate into `HeadingSpinTransfer.cs` if not caught.

### H-2 — §3.4 `headingAttrScale` inverts the stated semantics

§3.4 publishes:

```
pointQuality       = 1 - clamp01(pointError /
                                  (CONTACT_POINT_ERROR_SIGMA_M
                                   · headingAttrScale(agent)))

headingAttrScale(agent) = 1 + CONTACT_POINT_HEADING_ATTR_COEFF
                            · (Heading_norm - 0.5)
```

The accompanying prose: *"Higher Heading attribute tightens the
point-error distribution (centred on 0.5 → unit scale)."*

The math does the opposite. At `Heading_norm = 1.0`,
`headingAttrScale = 1 + 0.4·0.5 = 1.2`; denominator becomes
`0.03·1.2 = 0.036` (larger); ratio `pointError / 0.036` is
**smaller**; `pointQuality` is **higher** for any given physical
`pointError`. Higher-`Heading` players are therefore **more
forgiving of error**, not tighter-distributed — i.e. a worse
contact yields more quality for high-`Heading` agents than for
low-`Heading` agents, which is the *opposite* of "tightens".

Either (a) the prose is wrong and the math intends "more
forgiveness", or (b) the math is wrong and should be inverted
(`headingAttrScale = 1 - COEFF · (Heading_norm - 0.5)`, or apply
`/ headingAttrScale` somewhere it currently multiplies). Note that
the `pointNoiseM` term (`CONTACT_POINT_NOISE_SIGMA_M ·
NextGaussian`) is **not** scaled by `Heading`, so the "tighter
physical distribution" reading is incompatible with the formula
as published — high-`Heading` players have the same physical
jitter but a more forgiving quality function.

Direct precedent for this class of error: CLAUDE.md "Things That
Have Gone Wrong Before" cites the inverted fatigue convention in
Pass Mechanics #5 FR-02. Same trap, different attribute.

**Fix.** Pick the intended semantics (probably "high-Heading
players generate less physical error", i.e. scale
`pointNoiseM` by `1 / headingAttrScale` and remove the scaling
from the denominator), restate prose to match, and update the
§5.1.3 expected-monotonicity assertion to reflect the corrected
direction. Add a dedicated unit test verifying the sign of the
`Heading_norm` derivative of `pointQuality`.

Severity HIGH because the formula is `FM-010-002` and the
inversion is exactly the historical bug class CLAUDE.md flags.

### H-3 — §3.2 worked example has an off-by-one comparison

§3.2 "Worked Example — Corner Cross":

> the re-prediction returns `T+14`, which exceeds `T+9 +
> framesLateTolerance` (= `T+9 + 5` at `MAX_LATE_TOLERANCE_MS
> = 90`).

`T+14` does **not** exceed `T+9 + 5 = T+14`; it equals. The §3.2
pseudocode uses strict `>`:

```
if predictedContactFrame > idealContactFrame + framesLateTolerance:
    emitFailedAttempt(agent, MistimedLate)
```

so the example as written **passes** the check and emits no failure
— contradicting the example's own conclusion that
`HeaderAttemptFailedEvent { failureCause: MistimedLate }` is
emitted.

Compounding the issue, **the rounding policy for
`framesLateTolerance = MAX_LATE_TOLERANCE_MS / FRAME_MS = 90 /
16.67 ≈ 5.4` is not specified anywhere in §3.1, §3.2, or §3.4.**
Floor → 5; round-nearest → 5; ceil → 6. The choice matters for
the boundary frame and will produce inter-implementation drift.

**Fix.** (a) Change the worked example to `T+15` (clearly
exceeds 5-frame tolerance) and update the surrounding prose; or
change the pseudocode comparison to `>=`. (b) Pin the rounding
policy as part of the §3.2 `framesEarlyTolerance` /
`framesLateTolerance` derivation — `round-to-nearest` is the
common choice but state it.

Severity HIGH because the example appears in `section-3.md` and
will be copy-pasted into test fixtures; the off-by-one will
silently propagate.

### H-4 — §3.7 step 4: `disturbanceFactor` formula is missing

§3.7 step 4:

> Losers receive `disturbanceFactor ∈ [0, DUEL_DISTURBANCE_MAX]`
> (scaled by `baseScore` gap), applied multiplicatively to their
> `contactQualityScalar` (`q' = q · (1 - disturbanceFactor)`).

"Scaled by `baseScore` gap" is the only specification. The actual
mapping from `(baseScore[winner] − baseScore[loser])` to
`[0, DUEL_DISTURBANCE_MAX]` is undefined: is it linear? Saturating?
Sigmoid? What's the input gap range — `[0, 1]` (since weights sum
to 1.0)? What's the value at gap `= 0`? At gap `= 1`?

This matters substantially for the FR-HE-026 cutoff: whether a
duel loser emits a poor-quality `HeaderExecutedEvent` or a
`HeaderAttemptFailedEvent` (`q' < MIN_CONTACT_QUALITY`) is
entirely determined by this missing formula. §5.1.6 / §5.2.5
integration tests will be untestable until it's pinned.

**Fix.** Add an explicit `disturbanceFactor(gap)` formula to §3.7
step 4 — likely `DUEL_DISTURBANCE_MAX · clamp01(gap /
DUEL_DISTURBANCE_GAP_SATURATION)` with a new `[GT]` constant for
the saturation gap added to §3.1. Add a worked example for the
2-way duel showing the loser's `q'` computation. Verify §6.3.2
"disturbance-factor application to losers ≤10 µs" remains
credible against the chosen formula's per-call cost.

### H-5 — §3.5 `headerLaunchAngle` uses an undeclared `ANGULAR_COEFF` (KD-11 violation)

§3.5 `headerLaunchAngle` pseudocode:

```
adjustedDir = rotate(reflectedDir, ω_head · ANGULAR_COEFF)
```

Followed by prose: *"`ANGULAR_COEFF` is absorbed into the
head-velocity contribution; no new constant is published here
(covered by `SPIN_TRANSFER_COEFF` semantically; the geometric
coupling on launch angle is implicit in `reflectedDir`)."*

This violates KD-11 / FR-HE-014 ("Every numeric constant published
by #10 carries exactly one of `[GT]` / …"). `ANGULAR_COEFF` is a
named symbol in pseudocode that drives an algorithmic computation
(`rotate(...)`); it is not "absorbed" into `SPIN_TRANSFER_COEFF`
because that constant governs spin, not launch-angle rotation —
the two operate on different output channels. The "no new constant
is published" prose is a magic-number-by-narrative — exactly the
trap KD-11 was written to close.

**Fix.** Either (a) remove `ANGULAR_COEFF` from the pseudocode if
the head-velocity contribution to launch angle is intended to be
zero at Stage 0 (and state that explicitly), or (b) add a new
`LAUNCH_ANGLE_HEAD_VELOCITY_COEFF [GT]` row to §3.1 and reference
it. Update §9.1 constant-tag checklist and Appendix D glossary.

Severity HIGH because §9.1 verifies §3.1 against
`HeadingConstants.cs` at implementation time — if `ANGULAR_COEFF`
appears in `HeadingPowerAngle.cs` without a §3.1 row, the
constant-tag gate fails after the file is already written.

---

## MEDIUM

### M-1 — `ERR-010-001` back-prop entry is not in `spec-error-log.md`

KD-10 (`section-1.md` line 245-253) claims the back-prop was
"created during section authoring":

> A new domain-tag allocation `DOMAIN_TAG_HEADING = 0x16` is
> requested from #16 §3.4 via back-propagation entry `ERR-010-001`
> (created during section authoring).

Appendix G "Open-Items Tracker" reports OI-001 status as
"pending — to file when section-3 lands"; §9.4 OI-001 says
"pending — atomic with §3.1 row promotion `[CROSS-PENDING] →
[CROSS]`". `grep ERR-010 docs/tracking/spec-error-log.md` returns
only the long-closed ERR-010 (Shot Mechanics renumbering; March 6,
2026). No `ERR-010-001` row exists.

Either the section-text claims are aspirational (and KD-10's "was
created" wording is misleading), or the entry was written but not
landed. Either way, the v0.1 section files publish a
`[CROSS-PENDING]` constant whose tracking row doesn't exist —
which is the precise situation `[CROSS-PENDING]` was introduced
to avoid (CLAUDE.md "Constant Tags" requires the citation to
"name … the `spec-error-log.md` back-prop ID tracking the
allocation").

**Fix.** File the actual `ERR-010-001` entry in
`docs/tracking/spec-error-log.md` (status: open) modelled on the
`ERR-017-001` precedent. Adjust KD-10 wording to "is filed as
`ERR-010-001`" or "will be filed atomically with v0.2 landing"
to match reality.

### M-2 — `EligibilityPredicate` emits failed-attempt events from inside a "predicate"

§3.2 pseudocode:

```
if predictedContactFrame < idealContactFrame - framesEarlyTolerance:
    emitFailedAttempt(agent, MistimedEarly); return (false, …)
if predictedContactFrame > idealContactFrame + framesLateTolerance:
    emitFailedAttempt(agent, MistimedLate);  return (false, …)
```

The function named `EligibilityPredicate` has side effects: it
publishes events to the `EventBus`. A predicate that mutates
event state is a separation-of-concerns violation and produces
two risks:

1. **Double emission.** §4.6 pseudocode calls
   `EligibilityPredicate(...)` once per tick per agent. If the
   predicate is also called from any diagnostic / lookahead path
   (e.g., the §6.4 `heading.duel.entry` trace, or
   `BallState.snapshotFrame > 1` re-query in step 2), each call
   will emit another failed event.
2. **Test isolation.** §5.1.1 truth-table tests of the eligibility
   predicate now need EventBus stubs. The §5.1.7 failed-attempt
   tests overlap.

**Fix.** Split `EligibilityPredicate` into a pure predicate
returning `(bool, predictedContactFrame, idealContactFrame,
mistimedDirection?)` and a caller in §4.6 that, on
`mistimedDirection != None`, invokes `emitFailedAttempt(...)`
once. Update §5.1.1 to assert no event emission from the
predicate path.

### M-3 — `jumpStartFrame` is referenced everywhere but sourced nowhere

§3.3 references `jumpStartFrame` as the anchor of the synthetic
trajectory:

```
phase_t         = (currentFrame - jumpStartFrame) · FRAME_MS
apexFrame       = jumpStartFrame + round(...)
u               = (currentFrame - jumpStartFrame) / totalPhaseFrames
agentHeadZ(frame) = JumpReach_m · 4 · u · (1 - u)
```

It is also implicit in `computeJumpApexFrame(agent, currentFrame)`
in §3.2. But no section defines where `jumpStartFrame` is set or
how it relates to `HeaderIntent.attemptCommittedTick` (10 Hz) or
the 60 Hz tick on which the jump physically begins. Options
include:

- `jumpStartFrame = ceil(attemptCommittedTick · 6)` (commit-tick
  translated, no ramp).
- `jumpStartFrame = attemptCommittedTick · 6 + JUMP_RAMP_FRAMES`
  (commit-tick + reaction-latency ramp).
- `jumpStartFrame` set by an Agent Movement #2 transition event
  that doesn't exist (KD-18 confirms it doesn't).

Without pinning the source, two implementations will produce
different `apexFrame` values for the same `HeaderIntent` — and
the §3.4 `timingOffsetMs` will diverge.

**Fix.** Add a §3.3 paragraph or new sub-subsection defining
`jumpStartFrame` explicitly. Likely: `jumpStartFrame = first
60 Hz tick on which §3.2 sees `agent.movementState ∉ {GROUNDED,
STUMBLING}` after `HeaderIntent` commit`. Reflect in §2.2
`HeaderContactState` or a new `JumpPhaseState` struct.

### M-4 — `actualContactFrame` (used in §3.4) is set nowhere

§3.4 references `actualContactFrame` in the timing-offset formula:

```
timingOffsetMs = (actualContactFrame - idealContactFrame) · FRAME_MS
                 + timingJitterMs
```

`actualContactFrame` is declared in §2.2 `HeaderContactState`
("populated at contact"), but no algorithm in §3 specifies when
or how it is populated. The §4.6 60 Hz pseudocode dispatches into
§3.5 / §3.6 / §3.7 on `currentFrame == predictedContactFrame`,
which implies `actualContactFrame = currentFrame` at that point —
but this is never stated.

Closely related: the §3.7 duel resolution may delay actual
contact for losers (`disturbanceFactor` reduces quality but
contact still occurs at `predictedContactFrame`?). Underspecified.

**Fix.** Add to §4.6 60 Hz pseudocode: *"On the
`currentFrame == predictedContactFrame` branch, set
`HeaderContactState.actualContactFrame = currentFrame` before
invoking §3.4."* Or pin in §3.4's input list.

### M-5 — §3.7 step 4 vs. step 5 inconsistency: 2-way vs. 3-way duel loser semantics diverge

§3.7 step 4 (2-way framing):

> Losers receive `disturbanceFactor … applied multiplicatively to
> their `contactQualityScalar` …`. If `q' < MIN_CONTACT_QUALITY`,
> the loser emits `HeaderAttemptFailedEvent` instead of a poor-
> quality `HeaderExecutedEvent` (FR-HE-026).

§3.7 step 5 (3-way+ framing):

> Winner-only emits `HeaderExecutedEvent`; all losers emit
> `HeaderAttemptFailedEvent` with `failureCause = DisturbedInDuel`
> (FR-HE-027).

These are mutually inconsistent. In a 2-way duel, the loser may
emit a `HeaderExecutedEvent` with disturbance-adjusted quality
(if `q' ≥ MIN_CONTACT_QUALITY`). In a 3-way duel, all losers
always emit `HeaderAttemptFailedEvent` regardless of their `q'`.
Why does adding a third participant categorically change loser
semantics?

If the intent is "in any contested duel, only the winner emits
an executed event", then FR-HE-026 (`q' < MIN_CONTACT_QUALITY`
gate) is dead code in 2-way duels too. If the intent is
"disturbance-adjusted executed events are allowed for losers in
any size duel", then FR-HE-027 / §3.7 step 5 are wrong.

**Fix.** Pick one semantics. Recommendation: align 3-way with
2-way — losers emit failed events only when `q' <
MIN_CONTACT_QUALITY`, otherwise emit disturbance-adjusted
executed events. (This makes the "winner-only" wording in §2.3
F-04 and §3.7 step 5 incorrect — adjust the prose. FR-HE-027
needs a rewrite to match.)

### M-6 — §5.1.6 tiebreak test assertion contradicts §3.7 step 3 multi-way semantics

§5.1.6 unit test:

> Tiebreak-invocation count: 1000 deterministic-replay iterations
> on a near-tie configuration … — verify `DRAW_SITE_DUEL_TIEBREAK`
> is called **exactly once per duel** and never on non-tie
> configurations.

§3.7 step 3:

> each participant `i` within `DUEL_TIEBREAK_EPSILON` of
> `baseScore[rank0]` receives an additive
> `DUEL_TIEBREAK_NOISE_AMPLITUDE · rng.NextFloat(...)`.

If three participants are all within `DUEL_TIEBREAK_EPSILON` of
`baseScore[rank0]` (a 3-way near-tie), the RNG is called three
times. The "exactly once per duel" assertion in §5.1.6 is wrong
for 3+ way ties.

**Fix.** Restate the test as "exactly `N` calls per duel, where
`N` is the count of participants within `DUEL_TIEBREAK_EPSILON`
of `baseScore[rank0]`". Update the test to cover the 3-way
near-tie case.

### M-7 — §5.1.7 test description contradicts §2.3 for F-05 / F-06 / F-07

§5.1.7 prescribes for each failure mode F-01..F-07:

> Assert: no `Ball.ApplyKick` invocation; `BallState` unchanged
> after the tick; `HeaderAttemptFailedEvent` published with
> `failureCause` matching the expected enum value.

But per §2.3:

- **F-05** (off-pitch `targetIntent`) clamps and emits a
  *warning* — no `HeaderAttemptFailedEvent`. The attempt
  continues.
- **F-06** (stale `BallState`) re-queries and emits a
  *diagnostic* — no `HeaderAttemptFailedEvent`. The attempt
  continues.
- **F-07** (envelope clamp on `contactPointIntent`) absorbs the
  clamp delta into `pointError` — no standalone telemetry, no
  failed event.

So three of the seven failure modes do NOT match the assertion
template.

**Fix.** Split §5.1.7 into two test groups: (a) F-01..F-04 with
the failed-event assertion; (b) F-05..F-07 with mode-specific
assertions (clamp-and-continue, re-query-and-continue,
absorbed-into-pointError). Cite FR-HE-029, FR-HE-033, FR-HE-030
respectively.

### M-8 — `framesEarlyTolerance` / `framesLateTolerance` rounding policy unspecified

(Companion to H-3.) §3.2 step 5:

```
framesEarlyTolerance = MAX_EARLY_TOLERANCE_MS / FRAME_MS
framesLateTolerance  = MAX_LATE_TOLERANCE_MS  / FRAME_MS
```

`140 / 16.67 ≈ 8.4`; `90 / 16.67 ≈ 5.4`. No rounding policy is
named. The comparison `predictedContactFrame > idealContactFrame
+ framesLateTolerance` then depends on whether
`framesLateTolerance` is `5`, `5.4`, or `6`. If kept as a `float`,
the integer-vs-float comparison silently passes or fails on
boundary frames. If implementations differ, deterministic-replay
across builds will drift.

**Fix.** Define explicitly: `framesEarlyTolerance =
(int) ceil(MAX_EARLY_TOLERANCE_MS / FRAME_MS)`, similarly for
late. Or keep them as `float` and use `>=` with strict-greater
documented. Pin in §3.2.

### M-9 — `timingJitterMs` placement: telemetry vs. execution noise semantics unclear

§3.4 computes:

```
timingJitterMs = TIMING_JITTER_SIGMA_MS · NextGaussian(...)
timingOffsetMs = (actualContactFrame - idealContactFrame) · FRAME_MS
                 + timingJitterMs
```

This adds the jitter to the timing offset *post-contact*, after
§3.2 has already adjudicated eligibility on the un-jittered
`predictedContactFrame`. Two contradictory interpretations:

- **(A) Execution noise.** Jitter models the player's actual
  timing variation. Then jitter should perturb `actualContactFrame`
  itself (and thus also the eligibility-window check in §3.2). The
  current placement is wrong — quality degrades but eligibility
  doesn't react.
- **(B) Perception/measurement noise.** Jitter models how the
  player perceives their own timing error, affecting only the
  derived quality scalar. Then the current placement is correct,
  but the semantics needs to be stated explicitly because it's
  unusual.

The §3.1 row note ("Amplitude of per-attempt timing-noise
Gaussian") doesn't disambiguate.

**Fix.** State the semantics in §3.1 / §3.4 explicitly. If (A),
move the jitter into §3.2 (perturb `predictedContactFrame` or
`actualContactFrame`). If (B), justify why the player's quality
degrades on a perception that doesn't shift the actual contact —
likely framing is "execution micro-variations within a single
physics frame that don't shift the frame index but shift the
contact moment within the frame".

---

## LOW

### L-1 — `HeaderIntent.attemptCommittedTick` is declared but never consumed

§2.2 `HeaderIntent` carries `attemptCommittedTick: int`. The §3.2
eligibility predicate body never reads it. The §3.3 jump
kinematics references `jumpStartFrame`, which is presumably derived
from `attemptCommittedTick` (see M-3), but the derivation is not
shown. If `attemptCommittedTick` is unused, drop it from the
struct; if it's used by §3.3's missing-`jumpStartFrame` derivation,
make the connection explicit.

### L-2 — `DUEL_DISTURBANCE_MAX` range stated, formula gap (companion to H-4)

§3.1 row for `DUEL_DISTURBANCE_MAX`: *"Maximum disturbance factor
applied to a duel loser's `contactQualityScalar`."* The range
`(0, 1]` and value `0.5` are pinned, but how `disturbanceFactor`
actually varies within that range is left to §3.7 step 4, which
itself defers to "scaled by `baseScore` gap" (see H-4). Either
state the disturbance formula here or move the constant's
semantic note to §3.7 alongside the formula when H-4 is fixed.

### L-3 — `JUMP_APEX_FRACTION` `[GT]` justification reads like a defence, not a description

§3.1 row note for `JUMP_APEX_FRACTION`: *"`[GT]` not `[FIXED]`
because the Stage 0 trajectory is synthetic per KD-18, not
physical."* This is an explanation of the tag choice but reads
slightly defensively. Most other rows use the note column for
*what the constant does*, not *why it has the tag it has*. Move
the tag-rationale into a separate "tag-rationale" footnote, or
keep the convention loose but flag this is reading like a comment
in the audit log.

### L-4 — `XC-010-005` (Event System #17 own-goal adjudication) has no §17 anchor pinned

§8.2 / §8.4 / §9.2 list `XC-010-005 → Event System #17` for
own-goal-shape trajectory adjudication, but no subsection
anchor is supplied. KD-6 says the adjudication is owned by #17
/ Match Referee — but #17 (APPROVED May 13, 2026) doesn't
define a Match Referee surface; that's a downstream concern.
The cross-reference effectively points at an unspecified surface
of an APPROVED spec — risks a stale `XC-` row after #17 ratifies
its own goal-adjudication API (or doesn't, because Match Referee
isn't in the 20-spec set).

**Fix.** Either (a) anchor `XC-010-005` to a concrete §17
subsection that exists today (e.g., the `EventBus.Publish` surface
in §3.x), with prose clarifying that the adjudication itself is
"future" and routes via the published event; or (b) drop
`XC-010-005` until a Match Referee spec exists.

### L-5 — §6.3.1 eligibility-predicate frequency upper bound is loose

§6.3.1 "Eligibility predicate (§3.2): ≤22 / frame". This implies
every agent gets the predicate every frame. §4.6 pseudocode is
gated on "if agent has a HeaderIntent latched and aerial-phase
active" — typically far fewer than 22 agents. The 22-cap binds
worst-case for budget framing, fine; but the row note in §6.3.1
should make clear this is upper-bound pessimism, not steady-state
expectation. Otherwise the §6.3.1 ≈78 µs total reads as a
steady-state, which it isn't.

### L-6 — §5.3.1 expected telemetry shares (`55/20/25 OnTime/Early/Late`) need a noise-model citation

With `±40 ms` label thresholds and `TIMING_JITTER_SIGMA_MS = 8`,
a pure-noise model would put >99% of attempts into `OnTime`. To
get 45% non-`OnTime`, you'd need substantial *systematic*
mistiming — i.e. Decision Tree #8 committing slightly too early
or too late on most attempts. §5.3.1 doesn't say where the
distribution model comes from. Either cite an empirical source
(pass-1 L-6 brought in Opta / StatsBomb for header counts but
not for timing labels), or recast the row as designer-target with
that framing.

### L-7 — Glossary `ContactPointIntent` semantics drift from §2.2 struct

Appendix D glossary: *"`ContactPointIntent` — Decision Tree #8
output specifying the intended contact location on the head
surface (2-D head-local coordinates: forehead-centre /
forehead-edge / temple as a continuous parameter)."*

§2.2 struct: `Vector2 contactPointIntent; // head-local
coordinates (m)`.

The glossary suggests a named-region continuous parameter (which
is a bit odd — what's the axis convention from "forehead-centre"
to "temple"?). The struct is `Vector2` in metres. §3.1
`CONTACT_POINT_ERROR_SIGMA_M` is in metres. The glossary's
named-region prose conflicts with the metre-based struct: state
the head-local axis convention (origin at head centre? `+x` =
forward? units consistent with `Vector2.Distance(...)` in metres?)
in §2.2 or in a new §3.1 sub-paragraph, and align the glossary.

---

## CROSS-CUTTING OBSERVATIONS

### C-1 — Section-file authoring preceded `SPEC_INDEX.md` status flip (same pattern as #12, #16)

`SPEC_INDEX.md` row 10 still reads `NOT STARTED` (verified May 16,
2026). Section files v0.1 exist. Project precedent for this gap:
Positioning AI #12 (status flipped atomically with v0.2 PASS-1
fix landing, per CLAUDE.md OPEN ISSUES); Deterministic Sim #16
(v0.7 at status flip, per `SPEC_INDEX.md` row 16 history).

Recommendation: flip row 10 `NOT STARTED → IN REVIEW` atomically
with the v0.2 fix pass that closes the H-1..H-5 + M-1
back-prop-filing items. Don't flip until then — flipping at v0.1
publishes the H-1 spin-double-reversal bug as "in review".

### C-2 — KD set holds up under v0.1 realization

The eighteen KDs declared in `section-1.md` §1.3 cleanly map onto
section-file content; the §3 → §3.1 reconciliation finally
catches up to the §3.1 inventory growth from outline v1.0 → v1.1;
all `[CROSS-PENDING]` / `[CROSS]` / `[DERIVED]` tags are used
correctly. The findings above are concentrated in **algorithm
internals** and the **§5 test-plan ↔ §2.3 failure-mode**
alignment — not in the KD layer or the dependency closure.

### C-3 — Appendix F adversarial-review traceability is solid

The Appendix F mapping table from the 21 pass-1 outline findings
to v1.1 outline resolutions is well-formed and verifiable. Pass-2
should add an Appendix H mirror for these v0.1 section-file
findings once v0.2 lands.

---

## RECOMMENDED V0.2 FIX PASS SCOPE

1. **H-1** — Drop the redundant `reversalTerm` subtraction in
   §3.6; re-derive the worked example; mirror in Appendix A.3;
   update §5.1.5 reversal-boundary test expectation.
2. **H-2** — Resolve the `headingAttrScale` semantic inversion;
   pick "less physical error" or "more forgiveness" semantics
   and align math + prose + §5.1.3 test direction.
3. **H-3** — Fix §3.2 worked example off-by-one (change `T+14` →
   `T+15`) and pin the rounding policy for `framesEarlyTolerance`
   / `framesLateTolerance` (also closes M-8).
4. **H-4** — Add explicit `disturbanceFactor(gap)` formula in
   §3.7 step 4; introduce `DUEL_DISTURBANCE_GAP_SATURATION [GT]`
   in §3.1 if needed; add worked example (closes L-2).
5. **H-5** — Either remove `ANGULAR_COEFF` from §3.5 (state
   Stage 0 launch-angle contribution from head velocity is zero)
   or add `LAUNCH_ANGLE_HEAD_VELOCITY_COEFF [GT]` to §3.1.
6. **M-1** — File the actual `ERR-010-001` entry in
   `spec-error-log.md`; adjust KD-10 wording to reality.
7. **M-2** — Split `EligibilityPredicate` into a pure predicate
   + a caller that emits failed events.
8. **M-3** — Define `jumpStartFrame` source in §3.3 (or new
   `JumpPhaseState` struct in §2.2).
9. **M-4** — Pin where `actualContactFrame` is set (likely §4.6
   60 Hz pseudocode).
10. **M-5** — Align 2-way vs. 3-way duel loser semantics;
    rewrite FR-HE-027 or §3.7 step 5 to match.
11. **M-6** — Fix §5.1.6 "exactly once per duel" → "exactly N
    calls, where N is the near-tie cohort size".
12. **M-7** — Split §5.1.7 into F-01..F-04 (failed-event
    assertion) and F-05..F-07 (mode-specific assertions).
13. **M-9** — Disambiguate `timingJitterMs` semantics
    (execution vs. perception noise).
14. **L-1..L-7** — Quick-pass; each is a single-paragraph or
    single-row edit.

Estimated v0.2 effort: 4–6 hours single-author. No KD-set
changes required. After v0.2 lands, flip `SPEC_INDEX.md` row 10
`NOT STARTED → IN REVIEW` atomically, and add Appendix H
traceability mapping the 21 findings above to v0.2 resolutions.

---

## VERSION HISTORY

| Version | Date         | Author              | Notes                                                                  |
|---------|--------------|---------------------|------------------------------------------------------------------------|
| 1.0     | May 16, 2026 | adversarial reviewer | Pass-1 adversarial review of `section-1.md`..`appendices.md` v0.1     |
