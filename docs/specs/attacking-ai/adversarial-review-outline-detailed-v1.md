# Adversarial Review — Attacking AI #15 outline-detailed.md v1.0

**Created:** May 17, 2026
**Reviewer:** AI agent (adversarial pass)
**Scope:** `attacking-ai/outline-detailed.md` v1.0
**Measured against:** CLAUDE.md, 9-section template, adjacent approved
specs (#8 Decision Tree, #12 Positioning AI, #13 Pressing AI, #14
Defensive AI, #16 Deterministic Simulation), and `SPEC_INDEX.md`.
**Severity legend:** H = blocks section-file authoring; M = must fix in
v1.1; L = follow-up acceptable.

---

## VERIFIED PREMISES

- Spec #15 status in `SPEC_INDEX.md`: NOT STARTED. Correct.
- Domain tag block ERR-012-001: `0x17`=#12, `0x18`=#11, `0x19`=#13,
  `0x1A`=#14, `0x1B`=#15. Confirmed in `spec-error-log.md` L1158.
- Event System #17 Appendix byte range `0x18…0x1B` reserved for #14
  **event channels**. Confirmed in `event-system/appendices.md` L76.
  These are event-channel IDs (in #17's namespace), distinct from domain
  tags (in #16's namespace). No collision with `DOMAIN_TAG_ATTACKING_AI = 0x1B`.
- `LaneAssignment` in #12: a 5-bin lateral classification enum
  ("Five lateral bins" per `positioning-ai/section-3.md`). Fields
  confirmed: `lane` (LaneAssignment enum) and `lateralPct` (float 0–1)
  per `positioning-ai/section-2.md`. There is NO `.lateralBias` float field.
- #8 §1.3.2 "Multi-agent coordination" statement: names #12 and #13 only.
  #14 Defensive AI and #15 Attacking AI are NOT explicitly listed.
- `lineMembership` and `laneAssignment` in #12 are DISTINCT concepts:
  `lineMembership` = forward/backward position (DEFENSE/MIDFIELD/ATTACK);
  `laneAssignment` = lateral bin (5-bin: LEFT_WIDE through RIGHT_WIDE).

---

## FINDINGS

### [H-1] `relativeAngle_rad` declared as a RunParameters field but derived from other fields

**Location:** FR-AT-011; §3.4 formula; §2.2 RunParameters struct.

**Problem:** FR-AT-011 (normative) declares `RunParameters` has "exactly
four fields: `relativeAngle_rad`, `depthOffset_m`, `lateralOffset_m`,
`runTriggerTick`." Yet §3.4 shows:
```
relativeAngle_rad = atan2(lateralOffset_m, depthOffset_m)    // derived
```
`relativeAngle_rad` is fully determined by `depthOffset_m` and `lateralOffset_m`.
Storing a derived field as normative introduces a consistency constraint
that has failed before (ERR pattern: stored constant vs. derived constant).
FR-AT-011 is normative; the formula is inconsistent with it.

**Fix options:**
A. Remove `relativeAngle_rad` from `RunParameters`. Struct has 3 fields:
   `depthOffset_m`, `lateralOffset_m`, `runTriggerTick`. Angle is a
   computed property at use-site (no persistent storage).
B. Keep all 4 fields; tag `relativeAngle_rad` as `[DERIVED]` in the struct
   doc comment; make the formula normative; update FR-AT-011 to say
   "four fields (one derived)."

**Recommended:** Option A — simpler, avoids the invariant maintenance
problem. Recount: 3 fields, not 4.

---

### [H-2] `laneAssignment.lateralBias` references a non-existent struct field

**Location:** §3.4 RunParameters generation formula; §3.3 role
assignment algorithm; §6.1 constant catalogue.

**Problem:** §3.4 computes:
```
lateralOffset_m = laneAssignment.lateralBias × BASE_LATERAL_OFFSET_M
```
`LaneAssignment` in #12 is a 5-bin lateral **enum** (`positioning-ai/section-3.md`).
It has a `lane` field (enum) and a `lateralPct` float (0–1; multiplied
by 68m for anchor.y), but no `.lateralBias` field. This field does not
exist in the confirmed #12 data model. Calling a non-existent property
on an approved spec's struct is an Interface Design Principle violation
and a phantom-interface class hazard (ERR-001 / ERR-004 pattern).

Also in §3.3 step 2a: "if agent is in a forward lane (#12 `laneAssignment`
is ATTACK or MIDFIELD_ATTACK)" — these are `lineMembership` values
(DEFENSE/MIDFIELD/ATTACK), NOT `laneAssignment` values. This conflates
two distinct #12 fields.

**Fix:** Rewrite §3.4 to derive lateral offset from confirmed #12 fields:
```
// lateralPct is float 0–1 from formationSlot (confirmed in #12 §2.2)
centeredPct = formationSlot.lateralPct - 0.5      // −0.5 to +0.5
lateralOffset_m = centeredPct × PITCH_WIDTH_M × LATERAL_SCALE [GT]
```
Rewrite §3.3 step 2a to use confirmed field: "if `formationSlot.lineMembership`
is ATTACK or MIDFIELD" (not laneAssignment). Add Q2 as a section-file
grep verification of `formationSlot.lateralPct` and `lineMembership`
exact field names.

---

### [H-3] #8 §1.3.2 stage-binding citation is inaccurate

**Location:** Metadata header (Stage Binding); §1.8; §9.3 precondition (f).

**Problem:** The metadata header states the stage binding is "per #8
§1.3.2 Stage 1+ deferral table." The actual #8 §1.3.2 text says:
"Multi-agent coordination — Positioning AI (#12), Pressing AI (#13),
and coordinated pressing triggers are Stage 1+." Attacking AI (#15) is
not explicitly named. This is the same citation-accuracy hazard that
generated KNOWN HAZARD entries about stale spec numbers — citing a
section that doesn't explicitly say what the citation claims.

Both #13 and #14 made the same claim in their outline-detailed.md
("Verified facts grepped from #8 §1.3.2") and the actual text only names
#12 and #13 explicitly.

**Fix:** (a) Correct the citation to be accurate: "#8 §1.3.2 multi-agent
coordination deferral (Positioning AI #12 and Pressing AI #13 named;
Attacking AI #15 covered by implication)." (b) File ERR-015-005 as a
back-prop amendment to #8 §1.3.2 to add Attacking AI #15 explicitly to
the deferral row (analogous to ERR-012-002 / ERR-013-004 that both filed
back-prop amendments against #8 for similar accuracy issues). File at
section-file draft; listed here so it is not forgotten.

---

### [M-4] `TOUCHLINE_HOLD_Y_M` constant naming implies absolute Y, not distance

**Location:** §3.6 width-holding formula; §6.1 constant catalogue.

**Problem:** §3.6 states:
```
targetPosition.y = TOUCHLINE_HOLD_Y_M [GT]
```
then immediately corrects: "for the team attacking x=105 and near y=68:
`targetPosition.y = 68 - TOUCHLINE_HOLD_Y_M`."
The suffix `_Y_M` implies metres on the Y-axis (absolute coordinate).
But the formula shows it is a **distance from the touchline**, not an
absolute Y. This naming ambiguity is exactly the "coordinate origin"
KNOWN HAZARD class (CLAUDE.md). A reader implementing this will set
`targetPosition.y = 4.0` for one team and `targetPosition.y = 64.0`
for the other — but only the text correction reveals this; the constant
name contradicts it.

**Fix:** Rename to `TOUCHLINE_HOLD_DIST_M [GT]`. Update §3.6 formula
explicitly:
```
// near-touchline Y derived per team orientation:
// if ball.y > 34: nearTouchlineY = PITCH_WIDTH_M - TOUCHLINE_HOLD_DIST_M
// else:           nearTouchlineY = TOUCHLINE_HOLD_DIST_M
targetPosition.y = nearTouchlineY
```
This makes the coordinate convention explicit rather than hiding it in
a parenthetical.

---

### [M-5] §3.4 `rotate()` call undefined — coordinate frame ambiguous

**Location:** §3.4 RunParameters generation formula.

**Problem:** The formula:
```
runTargetPosition = ballCarrier.position
    + rotate(Vector2(depthOffset_m, lateralOffset_m), ballCarrier.forwardAngle)
```
uses `rotate()` without defining:
(a) What is `ballCarrier.forwardAngle`? Is it the velocity vector's angle?
    The team's attack direction (fixed per half)? A combination?
    If it's the velocity vector, a stationary ball carrier has angle = 0
    (or undefined), producing degenerate runs.
(b) In `Vector2(depthOffset_m, lateralOffset_m)`, depth is the X
    component and lateral is the Y component. After rotation, what
    coordinate frame is the output in? Pitch-frame (X = goal-to-goal)?
(c) The worked example claims "forward angle = 0° (toward x=105 goal)"
    — but a ball carrier not facing the goal would produce off-pitch run
    targets without a clamp. The clamp is declared but not positioned
    in the formula flow.

**Fix:** Define `ballCarrier.forwardAngle` as **the team's current
attack direction** (fixed per match-half: angle=0 for team attacking
x=105, angle=π for team attacking x=0), not the velocity vector.
This avoids degenerate runs and is consistent with using a normalised
"distance-to-opponent-goal" scalar for all directional computations.
Explicitly state the Vector2 convention and that the output is in
pitch-frame (X, Y in metres from corner origin).

---

### [M-6] Transition-to-defense logic split between §3.1 and §3.9 creates ambiguity

**Location:** §3.1 phase gating; §3.9 transition-to-defense behavior.

**Problem:** §3.1 says "decrement `transitionHoldTick`" but §3.9 (later
in the document) says "set `transitionHoldTick = TRANSITION_HOLD_TICKS`."
The SET operation must logically precede the DECREMENT operation, but the
document presents them in reverse order (§3.1 before §3.9). A reader
implementing the algorithm top-to-bottom will try to decrement a counter
that hasn't been set yet.

The pseudocode in §3.13 steps 1–2 gates on phase then returns, which
implies §3.9 logic is embedded in §3.1. But §3.9 appears much later
as if it is a separate algorithmic step. This ordering confusion is the
same class of error that caused multiple PASS-1 findings in #13 and #14.

**Fix:** Restructure so that §3.1 calls §3.9 (the transition controller)
when phase is TRANSITION or changes from IN_POSSESSION. Move the set/
decrement logic to §3.9; §3.1 becomes a pure gate that dispatches to
§3.9 for non-IN_POSSESSION states. §3.9 then becomes:
1. If phase changed from IN_POSSESSION to TRANSITION: SET counter.
2. Decrement counter; return appropriate directive.
3. Clear counter on return to IN_POSSESSION.

---

### [L-7] `DANGER_ZONE_CORRIDOR_HW_M` derivation references unexplained factor

**Location:** §6.1 constant catalogue; Appendix A derivations.

**Problem:** §6.1 states `DANGER_ZONE_CORRIDOR_HW_M = 8.16 m` with the
description "half-width of dangerous zone (half of goal width 7.32m ×
2.23× factor)." The 2.23× factor is unexplained. No citation, no
derivation, no sports-science basis. This is the same fabricated-value
risk that caused ERR-005 in Heading #10 v0.1.

**Fix:** Either: (a) derive from the penalty area width (40.32m →
half = 20.16m) — but then `CORRIDOR_HW_M = 20.16m`, not 8.16m; or
(b) define it as the goal width extended by `GOAL_ANGLE_FACTOR [GT]`
— a gameplay-tunable angle factor — and move the exact value to the
`[GT]` category with a comment "set to 8.16m to align with the 6-yard-box
width plus goalkeeper diving reach; adjustable"; or (c) simplify to
`DANGER_ZONE_CORRIDOR_HW_M = PENALTY_BOX_HW_M / 2 = 10.08m [DERIVED]`.
The 8.16m value may be valid but the justification must be traceable.
Appendix A must provide the derivation — the unexplained "2.23× factor"
is not a derivation.

---

### [L-8] KD-8 enum prohibition scope unclear — `overloadFlank` discriminator left unresolved

**Location:** Q6; KD-8; §2.2 AttackDirective struct.

**Problem:** Q6 asks "Confirm at section-file authoring that
`overloadFlank (LEFT/RIGHT)` is acceptable per KD-8 and Parameter-Based
Physics principles — it is analogous to `sameTeam: bool`." But the
answer is needed NOW to ensure no inconsistency in the outline. Leaving
this open risks a PASS-1 finding at section-file level for the exact
same question.

**Fix:** Resolve Q6 in the outline itself. KD-8 states: "No PatternType,
OverlapType, RunType enum anywhere in the algorithm or data structures."
The scope of KD-8 (and the Parameter-Based Physics CLAUDE.md principle)
is **tactical movement pattern taxonomy** — the enumeration of *how a
player moves* (ShotType=Volley, PassType=ThroughBall, etc.). A LEFT/RIGHT
spatial discriminator in `AttackDirective` is a **positional indicator**,
not a movement pattern type. Compare with #14's `MarkAssignment.mode`
enum (ZONAL/MAN_MARK/INTERCEPT_RUNNER), which is similarly a tactical-AI
discriminator. `overloadFlank` is acceptable. Add this resolution to KD-8
as a clarifying note.

---

### [L-9] §3.3 role assignment conflates lineMembership and laneAssignment

**Location:** §3.3 role assignment algorithm step 2a.

**Problem:** Step 2a reads: "if agent is in a forward lane (#12
`laneAssignment` is ATTACK or MIDFIELD_ATTACK)." ATTACK and MIDFIELD are
`lineMembership` values (forward/backward classification). `laneAssignment`
holds lateral-bin values (LEFT_WIDE through RIGHT_WIDE). These are
separate fields — using `laneAssignment` to check ATTACK/MIDFIELD is
a category error.

**Fix:** §3.3 step 2a should read: "if `formationSlot.lineMembership`
is ATTACK or MIDFIELD (forward lines only) — these agents make runs into
the final third." Width-holding (`laneAssignment` = LEFT_WIDE or
RIGHT_WIDE) is a separate criterion used in §3.6. Add a note: "RUNNER
eligibility is controlled by `lineMembership`; touchline holding is
controlled by `laneAssignment` — these are independent #12 fields."

---

## SUMMARY

| Sev | ID | Location | One-line description |
|---|---|---|---|
| H | H-1 | FR-AT-011; §3.4; §2.2 | `relativeAngle_rad` declared in struct but fully derived; FR inconsistent with formula |
| H | H-2 | §3.3; §3.4; §6.1 | `.lateralBias` field doesn't exist in #12; `lineMembership` vs. `laneAssignment` conflation |
| H | H-3 | Metadata; §1.8; §9.3(f) | #8 §1.3.2 citation inaccurate — #15 not explicitly named; ERR-015-005 needed |
| M | M-4 | §3.6; §6.1 | `TOUCHLINE_HOLD_Y_M` name implies absolute Y but means distance; rename |
| M | M-5 | §3.4 | `rotate()` undefined — forwardAngle convention and coordinate frame ambiguous |
| M | M-6 | §3.1; §3.9 | Transition SET (§3.9) / DECREMENT (§3.1) presented in wrong order |
| L | L-7 | §6.1; Appendix A | `DANGER_ZONE_CORRIDOR_HW_M` "2.23× factor" unexplained |
| L | L-8 | Q6; KD-8; §2.2 | `overloadFlank` LEFT/RIGHT acceptability left unresolved in outline |
| L | L-9 | §3.3 step 2a | `laneAssignment` used where `lineMembership` is correct |

**Total: 3 H / 3 M / 3 L. All must be resolved in v1.1 before section-file authoring begins.**

---

## VERSION HISTORY

| Version | Date | Author | Summary |
|---|---|---|---|
| 1.0 | May 17, 2026 | AI agent | Initial adversarial review of outline-detailed.md v1.0. 9 findings (3H/3M/3L). |
