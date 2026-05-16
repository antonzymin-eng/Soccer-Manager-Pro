# Goalkeeper Mechanics #11 — `outline-detailed.md` v1.0 → Pass-1 Adversarial Review

**Created:** May 16, 2026, late evening (between `outline-detailed.md`
v1.0 and v1.1).
**Reviewer:** AI agent
(`claude/goalkeeper-mechanics-specs-pM9hR`, pass-1 critique role).
**Scope:** `outline-detailed.md` v1.0 only. Findings recorded here
for tracking; resolution required before v1.1 supersedes v1.0.
**Method:** Section-by-section read against CLAUDE.md project rules,
the 9-section spec template, and the most recent precedent set by
Heading Mechanics #10 (APPROVED May 16, 2026, same day) — which
itself codifies the lessons of Pass Mechanics #5's 19-finding audit
and Shot Mechanics #6's KD-6 routing.

**Severity legend:** **H** = blocks v1.1 promotion (architectural
or invariant violation); **M** = must resolve in v1.1 to pass
approval gate; **L** = follow-up worth tracking but not v1.1-
blocking.

**Totals:** 4 H / 8 M / 6 L = 18 findings.

---

## HIGH-SEVERITY FINDINGS

### H-1 — Symmetric reaction tolerance is wrong.

**Location:** v1.0 KD-2 / §3.2 pseudocode.

**Problem:** v1.0 used a single `REACTION_TOLERANCE_MS` constant for
both "reacted too early" and "reacted too late". This mirrors
Heading #10's pass-1 H-1 finding (which split early / late
tolerance into two `[GT]` constants). For goalkeepers the
asymmetry is even more pronounced than for headers: early commit
is penalised by misdirection risk, late commit by reduced reach;
the two failure modes have different time-scale signatures.

**Fix locus:** new KD-18; rewrite §3.2 with piecewise formula;
add `REACTION_EARLY_TOLERANCE_MS` and
`REACTION_LATE_TOLERANCE_MS` as distinct `[GT]` rows in §3.4.

### H-2 — KD-3 Positioning AI #12 boundary is a hand-wave.

**Location:** v1.0 KD-3.

**Problem:** v1.0 said "fine-grain micro-adjustments only" without
defining the boundary computationally. Positioning AI #12 is IN
REVIEW *right now*; its §3.3.3 publishes three GK constants
currently `[EST]` pending this spec (per #12 AR-S1-11 fix policy).
v1.0 has no mechanism by which those `[EST]` constants ever
become `[GT]`. The result is a forward-coupling dead-end: #12
cannot reach APPROVED until #11 declares the contract; #11
declares the contract only abstractly; the `[EST]` tag never
clears.

**Fix locus:** sharpen KD-3 to specify the boundary as a state-
machine condition (`Resting` / `Set` states yield position
authority to #12; all other states retain it here). Add new
KD-13 with explicit ratification protocol: when #11 reaches IN
REVIEW, a patch revision to #12 §3.3.3 atomically promotes the
three constants. Add §3.3.0 (`Positioning AI #12 Consumer
Contract`) as a normative subsection.

### H-3 — Distribution-to-Pass-Mechanics coupling unspecified.

**Location:** v1.0 KD-6 / §3.8.

**Problem:** v1.0 said "GK distribution emits a `PassIntent`-
equivalent" without specifying which surface of Pass Mechanics #5
consumes it, whether #5 needs amendment to accept GK-originated
intents, or which spec owns the release-point geometry (release
height, windup duration). Pass Mechanics #5 is APPROVED and its
intent surface was not designed with GK distributions in mind;
silent re-use risks ERR-001-class (phantom interface) faults.

**Fix locus:** sharpen KD-6 (no #5 amendment; existing intent
surface is parameter-shaped enough). Add KD-16 (release-point
geometry owned here; mirrors Heading #10 KD-16 spin-transfer
ownership). Detail `mapToPassMechanicsDelivery` mapping in §3.8.

### H-4 — Dive Z-kinematics missing AM #2 boundary discussion.

**Location:** v1.0 §3.3 dive kinematics, §3 generally.

**Problem:** v1.0 modeled dive kinematics with Z (vertical) motion
without addressing the AM #2 §3.6 explicit Stage 1+ deferral of
Z>0 movement. This is the exact gap that Heading #10 discovered
during its own pass-1 review (the AM #2 jump-surface absence,
fixed via KD-18 there). The pattern is identical for GK dives:
AM #2 owns ground XY; #11 must own synthetic Z at Stage 0 OR
amend AM #2.

**Fix locus:** new KD-12 (mirrors Heading #10 KD-18). Stage 0
dive trajectory synthesized inside #11 (parabolic Z arc from
launch to landing). State-machine ground re-entry uses
`GroundedReason.DIVING_HEADER` (already in AM #2 §3.1.2 per
Heading #10's KD-18 work) or adds a `DIVING_SAVE` value — the
choice is itself a v1.1 design decision (resolved in v1.2 to
re-use `DIVING_HEADER` and defer the rename). Stage 1+ retire
target documented in §7.5. No AM #2 amendment at Stage 0.

---

## MEDIUM-SEVERITY FINDINGS

### M-1 — RNG draw sites not enumerated.

**Location:** v1.0 KD-7 / §4.4.

**Problem:** v1.0 mentioned `DeterministicRngService` but did not
enumerate the specific draw sites the spec will register against
#16 §4.5. Heading #10 (pass-1 M-4) had the same gap and resolved
it by listing each draw site with its specific caller. Without
explicit enumeration, the §16 §4.5 registry cannot be back-prop'd
and the determinism-owner sign-off (§9.3) cannot be granted.

**Fix locus:** §4.4 lists draw sites at v1.1 — initially three
(`DRAW_SITE_HANDLING_NOISE`, `DRAW_SITE_DIVE_TIMING_JITTER`,
`DRAW_SITE_CROSS_CLAIM_TIEBREAK`); pass-2 surfaced a fourth
(L-5). Each wired to the specific §3.X caller.

### M-2 — Perception System #7 citation absent from reaction model.

**Location:** v1.0 §3.2 reaction-time pseudocode.

**Problem:** v1.0 §3.2 invented its own reaction-latency model
without citing Perception System #7. Same trap as v0.1 finding #5
on this spec — the reaction-time model "must be derived from
Perception #7 visibility-cone latency plus a GK-specific attribute
(`Reflexes`)". Re-invention is ERR-001 / phantom-interface class.

**Fix locus:** KD-2 explicitly cites #7 §3.x; §3.2 consumes
`PERCEPTION_BASE_LATENCY_MS` `[CROSS]`; the §3.4 constant table
tags `PERCEPTION_BASE_LATENCY_MS` `[CROSS]` to Perception #7.

### M-3 — Ratification mechanism for #12 GK constants unspecified.

**Location:** v1.0 KD-3.

**Problem:** Distinct from H-2: H-2 is the boundary; M-3 is the
*mechanism*. Even if H-2 is fixed by specifying the state-machine
boundary, v1.0 does not say *what action* causes the `[EST]` →
`[GT]` promotion in #12 §3.3.3. Heading #10's `[CROSS-PENDING]`
→ `[CROSS]` precedent for `DOMAIN_TAG_HEADING` is the structural
analog: a coordinated atomic patch.

**Fix locus:** KD-13 specifies "atomic patch revision to #12
§3.3.3 / §6 promoting `[EST]` → `[GT]` coordinated with #11
status flip to IN REVIEW".

### M-4 — Cross-claim head-vs-hand routing ambiguity.

**Location:** v1.0 §3 generally; aerial duels under-specified.

**Problem:** v1.0 said GK head contacts route to Heading #10 (good
— KD-4) and hand contacts stay here (good). But during a cross
claim, the GK *may* contact the ball with either body part
depending on micro-positioning. v1.0 has no decision rule for
which body part wins when both are within contact volume
simultaneously.

**Fix locus:** new KD-14 — contact body part is decided by
Collision System #3 contact-event data (hand capsule vs. head
sphere intersection priority at Stage 0); NOT by intent. §3.6
step 1 implements; step 2 routes per body part.

### M-5 — Rush abort policy undefined.

**Location:** v1.0 §3.7 (rush dispatch).

**Problem:** v1.0 did not specify whether a committed rush is
abortable mid-flight. This is the GK-version of Heading #10's
intent-staleness problem (KD-17 there). Without a policy, the
implementation will quietly choose one — possibly differently
from the design intent.

**Fix locus:** new KD-15 — non-abortable EXCEPT on ball
interception by another agent. F-08 in §2.3.
`GoalkeeperRushEvent.abortReason` field added to §2.2.

### M-6 — Distribution release-point geometry ownership unclear.

**Location:** v1.0 KD-6 / §3.8.

**Problem:** v1.0 said "release point at GK hand position" without
specifying that release HEIGHT, launch angle range, and windup
duration are #11-owned (versus owned by Pass Mechanics #5).
Without ownership, the implementation may consult #5 for a
geometry that #5 doesn't publish.

**Fix locus:** new KD-16 (release-point geometry here); §3.8
parameterised by `THROW_RELEASE_HEIGHT_M`, `ROLL_RELEASE_HEIGHT_M`,
`KICK_RELEASE_HEIGHT_M`, `THROW_WINDUP_MS`, `ROLL_WINDUP_MS`,
`KICK_WINDUP_MS`.

### M-7 — Set-piece scope unclear.

**Location:** v1.0 §1.2 (out-of-scope) and §3 generally.

**Problem:** v1.0 did not explicitly state whether free-kick saves
and penalty saves are in Stage 0 scope. Mirror of Heading #10's
M-7 (KD-13 there: set-piece headers ARE in Stage 0; the kick
itself is not).

**Fix locus:** new KD-19 — set-piece saves IN scope at Stage 0;
defensive wall positioning NOT in scope (owned by #14).

### M-8 — Inventory: §3 constants not fully tabled in v1.0 §3.4.

**Location:** v1.0 §3.4 master constants table.

**Problem:** v1.0 §3.4 listed ~30 constants but several appeared
in §3.2 / §3.3 / §3.5 / §3.6 pseudocode without a §3.4 entry
(e.g. cross-claim duel weights, handling band thresholds). Same
shape of finding as Heading #10 pass-1 M-1.

**Fix locus:** §3.4 expanded to ~50 rows; every §3.2–§3.8 symbol
either tabled or named per-call output.

---

## LOW-SEVERITY FINDINGS

### L-1 — Concussion / discipline absence.

**Location:** v1.0 §7 implicit.

**Problem:** No deferral for injury accumulation or yellow-card /
red-card discipline. Heading #10 KD-15 / KD-17 deferred the
analogous medical concern; GK has additional discipline surface
(handling outside box; foul on attacker during sweep).

**Fix locus:** new KD-17; §7.1, §7.3.

### L-2 — `OneVsOne` attribute use unspecified.

**Location:** v1.0 §3.5 implicit.

**Problem:** AM #2 publishes `OneVsOne` attribute but v1.0 did
not say how it participates in formulas. The risk is that the
implementation introduces a 1v1-specific physics branch (ERR-001
class).

**Fix locus:** new KD-20 — closed-form coefficient
`ONE_VS_ONE_HANDLING_COEFF` gated on state-machine being
`OneOnOne`; no physics branch.

### L-3 — Band-to-action mapping ambiguity at boundaries.

**Location:** v1.0 §3.5 (catch/parry/deflect/spill).

**Problem:** v1.0 said catch / parry / deflect / spill are
telemetry labels (good — KD-1) but did not specify what API call
distinguishes `Caught` (ball owned) from `Parried` (ball rebounds)
at the band boundary.

**Fix locus:** new KD-21 — `Caught` invokes
`Ball.SetPossessor(gkId)`; `Parried`/`Deflected`/`Spilled` all
invoke `Ball.ApplyKick` with different magnitudes. The only
API-toggling band is `Caught` vs. `Parried`; other transitions
are continuous in `Ball.ApplyKick` parameters.

### L-4 — 6-second-rule constant not classified.

**Location:** v1.0 §3.4 implicit.

**Problem:** The Laws of the Game 6-second hold rule on the GK is
a fixed rule constant, not a designer tuning. v1.0 omitted it
from §3.4 entirely.

**Fix locus:** `GK_HOLD_MAX_TICKS = 60` `[FIXED]` at 10 Hz; §3.4
new row; §2.2 `BallClaimedEvent.releaseTickEarliest` consumes.

### L-5 — `DRAW_SITE_HANDLING_NOISE` shared between two error sources.

**Location:** v1.0 §3.5 / §4.4 (raised during pass-2, but the
trap was authored in v1.0 — moved here for chronological
attribution).

**Problem:** v1.0 §3.5 had `handlingNoise` and `contactPointError`
both sampled from `DRAW_SITE_HANDLING_NOISE`. Two independent
error sources sharing a draw site violates the #16 §4.5 single-
purpose-per-site rule (which exists precisely so that a single
draw-site digest captures one perturbation source).

**Fix locus (deferred to v1.2):** split into
`DRAW_SITE_HANDLING_NOISE` (handling-scale Gaussian) and
`DRAW_SITE_HANDLING_POINT_NOISE` (contact-point Gaussian); §4.4
draw-site count 3 → 4. v1.1 carries the v1.0 wording forward; v1.2
fixes it (acknowledged in pass-2 review as P2-M-1).

### L-6 — §8.3 academic anchor sparseness.

**Location:** v1.0 §8.3.

**Problem:** v1.0 §8.3 listed three external references. Heading
#10's pass-1 L-6 used "six anchors named at outline stage" as
the bar (so §9 audit does not surface a sparseness finding when
section files are authored). GK literature is at least as rich as
heading literature.

**Fix locus:** six external references named at outline stage
(Dicks et al. 2010, Savelsbergh et al. 2002, Spratford et al.
2009, Suzuki et al. 1988, Williams & Burwitz 1993, Opta/StatsBomb
commercial baseline). DOI verification deferred to `section-8.md`
authoring per OI-003.

---

## RECOMMENDATIONS BEYOND v1.1 (for pass-2)

- Re-audit the `Ball.SetPossessor` surface against Ball Physics
  #1 §3.1 — v1.0 presumed it exists per ERR-008 resolution but
  did not verify. (Pass-2 picks this up as P2-M-2.)
- Re-audit KD-12's dual mention of `GroundedReason.DIVING_HEADER`
  re-use AND new `DIVING_SAVE` value — pick one. (Pass-2 picks
  this up as P2-L-1; v1.2 resolves by choosing `DIVING_HEADER`
  re-use at Stage 0 and deferring the rename.)
- §6.3 cross-claim duel-rate cites "per Opta baselines" without
  a specific cross-ref — pass-2 picks this up as P2-L-3.
- `FR-GK-026` mechanism for atomic resolution unclear — pass-2
  picks up as P2-L-2.
- `DRAW_SITE_HANDLING_NOISE` shared between two error sources
  (L-5 above) — pass-2 elevates and fixes in v1.2.

---

## RESOLUTION SUMMARY

All 18 findings (4 H / 8 M / 6 L) addressed in
`outline-detailed.md` v1.1 (May 16, 2026, later same evening).
Resolution mapping table is Appendix F of `outline-detailed.md`.
v1.2 (also May 16, 2026, latest) additionally resolves the 5
pass-2 findings — mapping table is Appendix G.
