# Attacking AI Specification #15 — Detailed Outline

**Created:** May 17, 2026
**Last Updated:** May 17, 2026
**Version:** 1.1
**Status:** DRAFT — expansion of `outline.md` into section-by-section plan.
Resolves all 13 findings (5 H / 6 M / 2 L) from the May 6, 2026 adversarial
review at the bottom of `outline.md`. v1.1 resolves all 9 findings (3 H /
3 M / 3 L) from the self-adversarial review (`adversarial-review-outline-detailed-v1.md`).
**Companion documents:** `outline.md` (high-level + first review).
**Unblocks:** section-file authoring (`section-1.md` … `appendices.md`).

---

## PURPOSE OF THIS DOCUMENT

Expansion of `outline.md` into a section-by-section subsection plan that
resolves every adversarial-review finding. For each subsection: the FRs it
will publish, the boundary declarations it will hold, and the cross-references
it will emit. Detailed enough that `section-1.md` … `section-9-approval-checklist.md`
and `appendices.md` can be drafted mechanically.

This document does **not** publish FR text in normative form — that text lands
in `section-2.md`. The detailed outline records every FR's intended rule,
conformance level, and source spec/section so the FR table can be authored
without re-deriving rules.

---

## METADATA HEADER (resolves outline.md H-1)

| Field | Value |
|-------|-------|
| Spec # | 15 |
| Title | Attacking AI |
| Folder | `docs/specs/attacking-ai/` |
| Priority | 4 (Phase C tactical-AI chain — depends on #12, #13, #14) |
| Status | NOT STARTED (outline phase) |
| Owner | Lead developer (gameplay-AI domain) |
| Approved Dependencies | #1 Ball Physics; #2 Agent Movement; #3 Collision System; #4 First Touch; #5 Pass Mechanics; #6 Shot Mechanics; #7 Perception; #8 Decision Tree; #13 Pressing AI (APPROVED May 17, 2026); #16 Deterministic Simulation; #17 Event System; #18 Performance; #19 Testing; #20 Code Standards |
| Pending Dependencies | #11 Goalkeeper Mechanics (IN REVIEW) — GK excluded from pool; #12 Positioning AI (IN REVIEW) — `RunIntent` writer layer declared in #12 §4.5; #14 Defensive AI (IN REVIEW) — mutual-exclusion phase contract; ERR-015-001 (domain-tag allocation in #16 §3.4) opens at section-file draft |
| Downstream Consumers | None (final link in the Phase C tactical-AI chain) |
| Stage Binding | **Spec drafted at Stage 0. Runtime activation is Stage 1** per #8 §1.3.2 "Multi-agent coordination" deferral (names #12 / #13; #15 covered by implication — ERR-015-005 filed to add #15 explicitly) and #12 §4.5 `RunIntent` writer-layer Stage 1+ declaration. Stage 0 deliverable = published interface schema + algorithm specification only; no runtime code emitted at Stage 0. |
| Estimated Effort | 6–8 working days |

---

## STAGE-BINDING CLARIFICATION (resolves outline.md H-2 partial)

**Spec authored at Stage 0; runtime activates at Stage 1.**

At Stage 0, #8 Decision Tree handles all in-possession behavior via:
- `PASS` action: agent selects a receiver and passes.
- `SHOOT` action: agent shoots at goal.
- `DRIBBLE` action: agent advances with ball.
- `HOLD` action: agent shields ball and waits.
- `MOVE_TO_POSITION` action: off-ball agents move to their #12 baseline slot.

These produce credible Stage-0 attacking behavior without coordination. Off-ball
agents drift toward their formation slot; on-ball agents independently select
pass, shoot, or dribble. There is no run-timing, no overload creation, no
weak-side pulling, and no team-style differentiation between teams at Stage 0.

#15 introduces **coordinated off-ball movement** — timed runs that create
space, width holders that stretch the defense, overload zones that concentrate
attackers, and team-style profiles that distinguish possession-based from
counter-attacking teams. This is the same pattern as #13 (pressing coordination)
and #14 (marking coordination): spec text exists at Stage 0 to name contracts;
code activates at Stage 1.

**Stage 0 deliverable from #15:** published specification (this document and
the section files). No runtime code at Stage 0.

**Why authoring at Stage 0 has value:**
1. Names the cross-spec contracts so #12's `RunIntent` writer-layer slot
   (declared in #12 §4.5.2 as a Stage 1+ struct) has a stable consumer.
2. Files the back-prop amendments #15 will need from #8 (as ERR-015-NNN)
   ahead of Stage 1.
3. Declares the mutual-exclusion contract with #14 Defensive AI (KD-6),
   resolving the open coupling question in both specs.
4. Preserves the "write all 20 specs before any code" discipline.

---

## CRITICAL BOUNDARY DECISION: #15 DOES NOT OWN ACTION SELECTION
## (resolves outline.md H-3 — highest-priority finding)

**Finding H-3 (outline.md May 6 review):** "§3 collides with Decision Tree
#8. 'Final-third preferences (cross, pass, shoot, recycle)' is exactly the
action-selection territory already approved in Spec #8."

**Resolution:**

Spec #15 does **not** own final-third action selection. That territory is
approved in Spec #8 and is frozen. Any redefinition would trigger a
renumbering-cascade-class change to a frozen spec — exactly the trap the
project's error history warns against (KNOWN HAZARD — Spec Renumbering
Cascades in CLAUDE.md).

**What #15 owns:**
Off-ball movement coordination only — the timed runs, width positions, and
support angles that other agents adopt while the ball carrier's action is
resolved by #8. #15 never selects pass / shoot / dribble. Those actions
are owned entirely by #8.

**How #15 interacts with #8 without owning actions:**
- #15 publishes, per tick, an `AttackIntent` struct for each off-ball agent.
  `AttackIntent` declares: (a) the agent's current attacking role
  (RUNNER | SUPPORT_BALL | HOLD_WIDTH | WEAK_SIDE), and (b) the agent's
  run-target `Vector2` if role is RUNNER.
- The orchestrator exposes this as part of the composed tactical context
  that #8 reads. #8's PASS utility weights a pass toward a RUNNER higher
  than a pass toward a HOLD_WIDTH agent (the receiver's attacking role is
  visible to the perception snapshot that #8 already consumes per #7 §3.7).
- **#15 does NOT instruct the ball carrier.** The ball carrier is handled
  by #8 exclusively.

**Implication for §1–§9 template mapping:** The old outline's "§3 Final-third
preferences (cross, pass, shoot, recycle)" is replaced by "§3 Off-ball
movement coordination algorithms." The final-third decision (which action
to take) remains in #8; #15 influences that decision by shaping the
off-ball landscape.

---

## PARAMETERIZED MOVEMENT — NO PATTERN ENUM
## (resolves outline.md H-4)

**Finding H-4 (outline.md May 6 review):** "§1 'overlaps, underlaps, cutbacks,
third-man runs' reads like a discrete enum. CLAUDE.md eliminated
KickType/ShotType/PassType for this exact pattern."

**Resolution:**

Vocabulary labels (overlap, underlap, third-man run) are **gameplay-vocabulary
only** — they appear only in the glossary (Appendix F) and the human-readable
team-style documentation. No `PatternType`, `RunType`, or `OverlapType` enum
exists in the algorithm or data structures.

All off-ball movement is described by three continuous parameters emitted
in `RunParameters` (a sub-struct of `AttackIntent`):

| Parameter | Type | Description |
|---|---|---|
| `depthOffset_m` | float | How far ahead of the ball carrier the run targets in the team-attack direction (m). Positive = toward opponent goal. Range: [5.0, 40.0]. |
| `lateralOffset_m` | float | Left/right offset from the ball carrier's corridor in the team-attack-perpendicular direction (m). Signed: positive = toward the Y=68 touchline. Range: [−34.0, 34.0]. |
| `runTriggerTick` | int | The tick index at which the runner commits (starts moving). Enables timing-staggered runs. Always ≥ currentTick. |

These three numbers fully parameterise any named attacking pattern
(angle is a derived quantity = `atan2(lateralOffset_m, depthOffset_m)`,
computed at use-site only — not stored):
- "Overlap" = depthOffset ≈ 15–25m, lateralOffset beyond ball carrier's lane (large positive or negative).
- "Underlap" = depthOffset ≈ 8–15m, lateralOffset toward the center (signed toward 0).
- "Third-man run" = depthOffset ≈ 20–30m, lateralOffset ≈ 0, triggered `N` ticks after the first pass.

The physics layer consumes `RunParameters` as target-position offsets from ball
carrier position (via #2 Agent Movement MOVE_TO_POSITION dispatch). No enum
crosses the boundary. This is analogous to Shot Mechanics #6's elimination of
`ShotType` via parameter-based physics (KD-3 / OI-006 in #6).

---

## CROSS-CUTTING DESIGN DECISIONS

### KD-1 — Cite-not-redefine
Spec #15 never restates a CLAUDE.md invariant or a rule from another approved
spec. Cited: corner-origin coordinates (#1 §1.2); fatigue `0 = rested,
1 = fatigued` (CLAUDE.md); 10 Hz tactical / 60 Hz physics (CLAUDE.md);
EntityId no-reuse (#2 §2.5 XC-002-001 + #8 §1.7.3 XC-008-001); perception
schema (#7 §3.7–§3.10); #12 baseline `formationSlot` + `RunIntent` writer-layer
(#12 §4.5); #8 action selection pipeline (#8 §3.1.1–§3.1.9); #13 press role
partition (#13 §2.2 FR-PR-014); #14 mutual-exclusion phase contract (#14
KD-8 / FR-DA-013).

### KD-2 — 10 Hz tactical, no 60 Hz work
Attacking AI runs on the 10 Hz tactical loop. Output is one `AttackDirective`
per team per tick and one `AttackIntent` per off-ball agent per tick. No 60 Hz
steering work — physical movement is owned by #2 via #8's resolved
`Action.TargetPosition` (same path as #12, #13, #14).

### KD-3 — Boundary with Decision Tree #8 (resolves outline.md H-3)

**Verified facts** (grepped from `decision-tree/section-1.md`,
`decision-tree/section-3-1.md`):
- #8 Stage-0 action set: PASS, SHOOT, DRIBBLE, HOLD, MOVE_TO_POSITION,
  PRESS, INTERCEPT. No per-team off-ball coordination at Stage 0.
- #8 §1.3.2 defers coordinated attacking movement to Stage 1+ context.
- #8 §3.1.7: MOVE_TO_POSITION utility uses perception snapshot for target.
  At Stage 1, #15's `AttackIntent.runTargetPosition` becomes an additional
  input to the MOVE_TO_POSITION utility for off-ball runners.

**#15's role:**
- #15 publishes `AttackIntent` per off-ball agent. This informs #8's PASS
  utility (which receiver to target) and the MOVE_TO_POSITION utility
  (where the off-ball agent should go).
- #15 does NOT produce `ActionType` values. All action selection is in #8.
- Stage 1 coupling: orchestrator exposes `AttackIntent` to the composed
  tactical context that #8 reads. The specific mechanism (a new
  `TacticalContext.AttackIntent[]` field added via #8 §2.2.6 amendment, or
  an accessor `AttackingAI.GetIntent(EntityId)`) is selected at section-file
  authoring and tracked as ERR-015-002 against #8 §2.2.6 / §3.1.7.

**Stage 0:** no coupling. #8 §3.1.7 MOVE_TO_POSITION uses the #12 baseline
slot only. #15 ships as inert specification.

### KD-4 — Boundary with Positioning AI #12 (resolves outline.md M-7)

**Verified facts** (grepped from `positioning-ai/section-2.md`,
`positioning-ai/section-4.md`):
- #12 §2.2 struct `FormationSlot` carries: `baselinePosition Vector2`,
  `lineMembership`, `laneAssignment`, `defensiveLineDepth`.
- #12 §4.5.2: "Stage 1+ #14 (Defensive AI) and #15 (Attacking AI) may
  consume BaselineDefensiveShape and RunIntent writer layer."
- #12 §2.2 FR-PA-048: "No interface produced against unspecified consumers
  (#13/#14/#15) at Stage 0."
- #12 §4.5 data structure table row: `RunIntent | #15 Attacking AI writer layer | 1+`

**#15's role:**
- **#12 owns:** per-agent baseline `formationSlot` (in-possession shape),
  `lineMembership`, `laneAssignment`.
- **#15 writes:** `RunIntent` — the Stage 1+ writer-layer struct declared in
  #12 §4.5.2. `RunIntent` is a temporary deviation from the baseline slot
  that the orchestrator composes before #8 reads target positions.
- **When #15 emits no run intent for an agent:** the agent defaults to its
  #12 baseline slot. MOVE_TO_POSITION at Stage 1 = baseline slot unless
  #15 has a live `RunIntent` for that agent.
- **#15 does NOT:** modify the baseline slot directly. #15 only writes the
  overlay `RunIntent`. This is the exact writer-layer pattern #12 §4.5
  declares.

**Vocabulary boundary:** #12 owns *passive support positions* (agents moving
to formation shape). #15 owns *active off-ball runs* (agents making timed
deviations from shape to create space).

**Stage 0 at spec text:** #12 accessor names are declared here as boundary
hints. No code references exist yet (consistent with #12 FR-PA-048 and the
Interface Design Principle).

### KD-5 — Boundary with Pressing AI #13

**Verified facts** (grepped from `pressing-ai/section-2.md` FR-PR-014,
`pressing-ai/section-4.md`):
- #13 is triggered when the team is OUT_OF_POSSESSION or TRANSITION.
- #13 emits `PressAssignment` per agent (role: PRIMARY_PRESS /
  COVER_SHADOW / HOLD_SHAPE).
- #13 §7.4 defers HOLD_SHAPE mark/cover to #14.

**#15's role:**
- #15 is active when the team is IN_POSSESSION (phase = IN_POSSESSION).
- On the same tick that the team holds possession, #13 produces empty
  directives (HOLD_SHAPE only — all agents at baseline). #15 then produces
  the attacking off-ball overlay.
- No direct data coupling between #15 and #13 within a tick. Phase
  discrimination is sufficient: #12 phase enum gates both specs
  independently. If IN_POSSESSION → #15 fires, #13 at minimum output.
  If OUT_OF_POSSESSION → #13 fires, #15 at empty output.

### KD-6 — Boundary with Defensive AI #14 (resolves outline.md M-6)

**Verified facts** (grepped from `defensive-ai/outline-detailed.md` KD-8,
FR-DA-013, §7.4):
- #14 FR-DA-013: "If #12 phase is IN_POSSESSION for this team, emit
  all-ZONAL directive (no override)."
- #14 KD-8: "#15 is in-possession behavior; #14 is out-of-possession
  behavior. The two are mutually exclusive at the team level."
- #14 §7.4: "#15 may consume `MarkDirective.emergencyFlag` as a signal
  that a goal-risk situation exists, informing any transition-phase
  recovery behavior."

**#15's role:**
- #15 and #14 are **mutually exclusive per team per tick**. No tick can
  simultaneously have both specs active for the same team (enforced by #12
  phase enum gating: KD-19 in #14 and §3.1 gating in #15).
- **Transition-phase handling:** When #12 phase flips from IN_POSSESSION to
  OUT_OF_POSSESSION (turnover), #15 emits an empty `AttackDirective` for
  `TRANSITION_HOLD_TICKS [GT]` then ceases. This covers the brief window
  while agents recover defensive shape. #13 takes over pressing coordination
  independently.
- **Emergency signal consumer (Stage 1+):** #15 may read
  `MarkDirective.emergencyFlag` from #14 to accelerate the transition
  (emit empty immediately instead of holding for TRANSITION_HOLD_TICKS).
  This coupling is declared here as a boundary hint; no interface is
  authored at Stage 0 (Interface Design Principle — #14 is IN REVIEW).

**Handoff protocol (Stage 1 per tick, for the team currently holding possession):**
1. #12 publishes per-agent baseline `formationSlot` and phase.
2. #15 reads phase; if IN_POSSESSION → runs attack-intent algorithm.
3. #15 publishes per-agent `AttackIntent` + team `AttackDirective`.
4. Orchestrator composes: #12 baseline slot overridden by #15 `RunIntent`
   where present; #8 reads the composed target.
5. If phase is OUT_OF_POSSESSION: #14 takes over; #15 emits empty directive.

### KD-7 — Boundary with Goalkeeper Mechanics #11

**Verified facts** (grepped from `goalkeeper-mechanics/section-1.md` §1.4):
- #11 owns: GK save pipeline, claim/dive/distribution decisions, GK
  positioning within goal zone.
- GK is excluded from all off-ball attacking movement coordination.

**#15's role:**
- GK is excluded from the attacking movement pool entirely (FR-AT-006).
- #15 does NOT produce `AttackIntent` for the GK entity.
- At Stage 0: no coupling. #11 and #15 are in outline/review stage
  simultaneously; no interface is authored (Interface Design Principle).

### KD-8 — No PatternType enum (resolves outline.md H-4)
Vocabulary labels (overlap, underlap, third-man run, cutback) appear only
in the glossary (Appendix F) and team-style documentation. No `PatternType`,
`RunType`, or `OverlapType` enum exists anywhere in the algorithm or data
structures. All movement is fully described by the three `RunParameters` fields
(see "Parameterized Movement" section above). This is the same principle that
eliminated `ShotType` in Shot Mechanics #6 (KD-3 / OI-006).

**Scope clarification (resolves adversarial finding L-8):** KD-8 prohibits
**movement-pattern taxonomy enums** (types of how a player moves — the class
eliminated in Shot Mechanics / Pass Mechanics). It does NOT prohibit spatial
discriminators in AI-layer output structs. `AttackDirective.overloadFlank`
is a **positional indicator** (which side of the pitch has an overload),
not a movement-pattern type. It is analogous to `MarkAssignment.mode`
(ZONAL/MAN_MARK etc.) in Defensive AI #14 — an AI-layer role discriminator,
not a physics-layer pattern enum. `overloadFlank` is acceptable. Q6 is
resolved; no further follow-up needed.

### KD-9 — Stage binding (resolves outline.md H-2)
**Spec authored at Stage 0; runtime activates at Stage 1.** See Stage-Binding
Clarification above. Stage 0 deliverable = published specification only; no
runtime code at Stage 0. This is the same pattern as #13 and #14.

### KD-10 — Stage-0-feasible acceptance criteria for chance quality
(resolves outline.md H-5)

**Finding H-5:** "xG-bound acceptance criteria infeasible at Stage 0. Shot
Mechanics #6 §7 explicitly defers xG to Stage 1+."

**Resolution:** Stage-0 surrogate metric for chance quality:
1. **Shots in dangerous zone per match:** count of SHOOT actions (per #8)
   where ball position satisfies: distance to opponent goal center ≤
   `DANGER_ZONE_MAX_DIST_M [GT]` AND |Y − goalCenter.Y| ≤
   `DANGER_ZONE_CORRIDOR_HW_M [GT]`. These are computable from #1 ball
   position + #8 SHOOT action events without any xG model.
2. **Average shot distance from goal:** mean distance of all SHOOT actions
   to the opponent goal center over a simulated match.

These two metrics are measurable at Stage 1 (first runtime), not at Stage 0.
The spec text declares the measurement method now so it is unambiguous when
first implemented. The actual numerical acceptance thresholds (e.g., "≥ 3
dangerous-zone shots per 90 min per team at medium engagement") are
team-style-profile dependent and live in Appendix D.

**Tactical identity measurability** (resolves outline.md L-13):
"Tactical identity" is measured as the difference in chance-creation
distribution across style presets:
- Comparing POSSESSION vs. DIRECT profiles: the DIRECT profile MUST produce
  ≥ `DIRECT_RUN_COUNT_DELTA [GT]` more runner assignments per match than the
  POSSESSION profile (measurable from `AttackIntent` role histograms).
- Comparing COUNTER_ATTACK vs. others: the counter profile MUST produce
  ≥ `COUNTER_TRANSITION_SPEED_DELTA_TICKS [GT]` fewer TRANSITION_HOLD_TICKS
  on average (measurable from directive emission log).
These are falsifiable acceptance criteria — not a subjective sign-off.

### KD-11 — Determinism binding (#16) (resolves outline.md M-8)

All `AttackDirective` writes, `AttackIntent` writes, `RunParameters` writes,
hysteresis state, and transition-hold counters are authoritative simulation
state per #16 §3.2 and appear in the per-tick digest at the scope #16 §6.2
defines for tactical-AI outputs. Agent iteration uses the canonical EntityId
sort from #16 §3.2.5. Assignment evaluation order across agents on the same
tick is EntityId-ascending.

Any stochastic tie-breaking (e.g., two agents equidistant as width-holder
candidates) uses `DeterministicRngService` with domain tag
`DOMAIN_TAG_ATTACKING_AI = 0x1B [CROSS-PENDING]` — proposed as `0x1B` per
the ERR-012-001 Phase B/C block (`0x17`=#12, `0x18`=#11, `0x19`=#13,
`0x1A`=#14, `0x1B`=#15) within the `0x17…0x1C` reserved range. Filed as
ERR-015-001 against #16 §3.4; `[CROSS-PENDING]` until that patch lands.

### KD-12 — Team-style profiles

Three named tactical profiles. A profile is a named cluster of multiplier
constants — NOT a `TeamStyleType` enum in the physics layer (same principle
as KD-8). Profiles are `[GT]` tunable parameters set in the constant
catalogue; the algorithm is identical across profiles.

| Profile name | Effect on algorithm |
|---|---|
| `POSSESSION` | `SUPPORT_RADIUS_MULT > 1`, more SUPPORT_BALL, slower `runTriggerTick`, lower `MAX_RUNNERS` |
| `DIRECT` | deeper `depthOffset_m`, faster `runTriggerTick`, higher `MAX_RUNNERS` |
| `COUNTER_ATTACK` | minimal SUPPORT_BALL, maximum RUNNER roles, zero `TRANSITION_HOLD_TICKS` |

At Stage 0, all teams use the `POSSESSION` default constants. Stage 1 wires
real team-style selection via the team-instruction infrastructure. Profiles
are not encoded as an enum in any struct — the active profile is selected
by which constant-catalogue values are loaded at match initialisation.

### KD-13 — Anti-chaos invariants
Three measurable invariants, checked before the directive is published:

1. **Max runners:** simultaneous RUNNER assignments ≤ `MAX_RUNNERS [GT]`.
   Excess runners are demoted to SUPPORT_BALL (lowest-threat runner first,
   EntityId tie-break).
2. **Min support:** at least `MIN_SUPPORT_AGENTS [GT]` agents in
   SUPPORT_BALL or HOLD_WIDTH mode at all times (ensures there is always a
   short-pass option for the ball carrier).
3. **Own-half runner block:** no RUNNER `runTargetPosition` assigned whose
   x-coordinate, in the normalised distance-to-opponent-goal frame, exceeds
   `OWN_HALF_RUN_BLOCK_M [GT]` past the half-line. Running behind one's
   own half-line is never a meaningful attacking action.

All invariants are checked POST-assignment, PRE-publication (FR-AT-021).
Violation handling: demote the costliest rule-breaking assignment until clean.
If still unresolvable after `MAX_INVARIANT_PASSES [GT]` iterations: emit
all-default (all agents HOLD_WIDTH / SUPPORT_BALL) directive for this tick.

### KD-14 — Single constant catalogue per #20 §4.2 (FR-CS-025)
ALL constants live in one file `AttackingAIConstants.cs`, organised into
`#region` blocks per #20 §4.2.

### KD-15 — Event System binding (#17) (resolves outline.md L-12)
At Stage 1 runtime, #15 emits two event channels:
- `ATTACK_RUN_STARTED` — fired when an agent's `AttackIntent` role
  transitions to RUNNER (not on every tick — only on the transition tick).
- `OVERLOAD_DECLARED` — fired when the `AttackDirective.overloadActive`
  flag transitions to true.

Both channels require atomic back-prop into #17 §3.10 channel registry —
filed as ERR-015-003 / ERR-015-004 at section-file draft. At Stage 0, no
channels are produced or consumed.

**Event System Appendix** (`event-system/appendices.md`) reserves byte range
`0x18…0x1B` for #14 (per #14 KD-15). If #14 occupies `0x1B`, #15 channels
will be allocated in the next available block at Stage 1 first-commit per
#17's §3.10 schema. The exact byte values are Stage-1 deliverables, not
Stage-0 blockers.

### KD-16 — Coordinate conventions (resolves outline.md M-9)
- Corner-origin coordinate system: X = goal-to-goal (0–105m), Y =
  touchline-to-touchline (0–68m), Z = height. Per #1 §1.2 and Appendix C.
- "Final third" for the team attacking the x=105 goal: x ≥ `FINAL_THIRD_X_M [DERIVED]`
  = `PITCH_LENGTH_M × 2/3` ≈ 70m. For the team attacking the x=0 goal:
  x ≤ `PITCH_LENGTH_M / 3` ≈ 35m. All formulas use a normalised
  "distance-to-opponent-goal" scalar to avoid per-team branching.
- "Weak side" = the half of the Y-axis opposite to the ball's current Y
  position. Formally: if ball.y > PITCH_WIDTH_M / 2, weak side is y < 30m;
  else weak side is y > 38m. (Asymmetric thresholds prevent flicker at
  midfield Y.)
- Fatigue convention: `0.0 = fully rested`, `1.0 = fully fatigued`
  (CLAUDE.md). Any inversion is a critical error.
- Tactical loop: 10 Hz (100 ms/tick). Physics/render loop: 60 Hz (~16.67 ms).
  #15 produces outputs only on the 10 Hz loop (KD-2; resolves M-10).

### KD-17 — Stage-0/Stage-1 scope discipline
Out of Stage 0 scope (deferred to §7): runtime code, #8 back-prop amendment
text, #17 channel registration, #12 `RunIntent` struct implementation,
team-style instruction wiring from coach UI, ML-tuned `[GT]` parameter
fitting, set-piece attacking positioning (corner/free-kick runners), per-player
run instructions from tactics screen, save-game persistence, Fixed64 migration.

---

## BOUNDARY MATRIX (resolves outline.md H-2 / M-6 / M-7)

| Boundary | #15 owns | Other owns | Direction | Mechanism | Stage 0? |
|---|---|---|---|---|---|
| #8 Decision Tree | `AttackIntent` per off-ball agent (run target, role) | Per-agent action loop (PASS/SHOOT/DRIBBLE/HOLD scoring) | #8 reads #15 (Stage 1) | `TacticalContext.AttackIntent[]` extension or accessor; ERR-015-002 | No (Stage 1) |
| #12 Positioning AI | `RunIntent` write layer (temporary slot deviation) | Baseline `formationSlot`; `lineMembership`; `laneAssignment`; phase enum | Orchestrator composes; #8 reads composed slot | `RunIntent` writer-layer per #12 §4.5 (Stage 1+) | No (Stage 1) |
| #13 Pressing AI | (no direct coupling; phase-gated) | PRIMARY_PRESS / COVER_SHADOW roles when OUT_OF_POSSESSION | Independent phase gating via #12 phase enum | Phase enum read from #12 (same source) | No |
| #14 Defensive AI | (mutually exclusive by possession phase) | `MarkDirective` (all-ZONAL when IN_POSSESSION per FR-DA-013) | Independent phase gating; emergencyFlag optional consumer at Stage 1+ | KD-6 mutual exclusion; `MarkDirective.emergencyFlag` (Stage 1+ boundary hint) | No (spec text only) |
| #11 Goalkeeper | (none — GK excluded from pool entirely) | GK positioning/saves/distribution | GK excluded | KD-7 exclusion rule | Yes (spec text) |
| #2 Agent Movement | (none direct — via #8 action output) | 60 Hz steering | #2 reads #8 | Same path as #12/#13/#14 | No |
| #7 Perception | (none — read consumer) | Filtered world model | #15 reads #7 | Snapshot read at tick start | Yes |
| #16 Determinism | `AttackDirective` / `AttackIntent[]` / `RunParameters` / hysteresis state / transition-hold counter | Digest format + iteration rule | #15 conforms | EntityId iteration + domain-tagged RNG `DOMAIN_TAG_ATTACKING_AI = 0x1B [CROSS-PENDING]` | Yes (spec text) |
| #17 Event System | `ATTACK_RUN_STARTED` / `OVERLOAD_DECLARED` channel definitions | Channel registry | (deferred Stage 1) | ERR-015-003 / ERR-015-004 at Stage 1 | No (Stage 1) |
| #18 Performance | (conformance only) | Per-tick budget framework | #15 conforms | §6 budget against named host | Yes (spec text) |
| #19 Testing | (conformance only) | Test-framework conventions | #15 conforms | §5 plan | Yes (spec text) |
| #20 Code Standards | (conformance only) | File / catalogue / naming rules | #15 conforms | `AttackingAIConstants.cs` per FR-CS-025 | Yes (spec text) |

---

## SECTION 1 — INTRODUCTION, SCOPE, DEPENDENCIES, KEY DECISIONS

### 1.1 Purpose
One-paragraph problem statement: #15 specifies coordinated off-ball movement
for agents when their team holds possession. The spec (a) defines the
attacking-role catalog (RUNNER / SUPPORT_BALL / HOLD_WIDTH / WEAK_SIDE),
(b) defines run-parameter generation (parameterized — no PatternType enum),
(c) defines the support-angle and width-holding heuristics, (d) implements
overload-zone detection and weak-side pulling, (e) declares team-style
profile modifiers, (f) specifies the transition-to-defense behavior on
possession loss, (g) enforces anti-chaos invariants, and (h) declares the
integration surface with #8 Decision Tree and #12 Positioning AI (where
runtime activation lands at Stage 1).

### 1.2 Scope (in / out)
**In:** off-ball attacking-role assignment, RunParameters generation
(parameterized movement), support heuristics, width-holding protocol,
overload detection, weak-side pulling, team-style profile multipliers,
transition-to-defense behavior, anti-chaos invariants, Stage-0-feasible
chance-quality acceptance criteria, constant catalogue.

**Out (per KD-17):** action selection (PASS/SHOOT/DRIBBLE — owned by #8),
runtime code, #8 back-prop amendment text (filed as ERR-015-002), #17
channel registration, #12 RunIntent implementation, team-style instruction
wiring from coach UI, set-piece attacking positioning (Stage 2+ set-piece
system), xG modeling (Stage 1+ per #6 §7), per-player run instructions
from tactics screen, ML tuning, save-game persistence, Fixed64 migration.

### 1.3 Dependencies
Upstream APPROVED: #1, #2, #3, #4, #5, #6, #7, #8, #13, #16, #17, #18, #19, #20.
Upstream IN REVIEW: #11, #12, #14.
(Section-file draft re-verifies status via `SPEC_INDEX.md` grep before
submitting for sign-off.)

### 1.4 Key Domain Concepts
Attacking role (RUNNER / SUPPORT_BALL / HOLD_WIDTH / WEAK_SIDE), attack
directive, attack intent, RunParameters, off-ball movement pool, support
radius, overload zone, weak-side puller, width-holder, team-style profile
(POSSESSION / DIRECT / COUNTER_ATTACK), transition-hold, anti-chaos
invariant, assignment hysteresis, dangerous zone (Stage-0 surrogate).

### 1.5 Key Design Decisions
Cross-reference KD-1..KD-17.

### 1.6 Interface Boundaries
The Boundary Matrix above.

### 1.7 Coordinate & Convention Bindings
- Corner-origin coordinate system per #1 §1.2 and Appendix C.
- "Final third" and "own half" normalised to distance-to-opponent-goal scalar.
- "Weak side" defined via midfield Y asymmetric threshold (KD-16).
- Fatigue convention `0.0 = rested`, `1.0 = fatigued` (CLAUDE.md).
- Tactical loop 10 Hz; physics 60 Hz. #15 outputs only on 10 Hz.
- EntityId no-reuse per #2 §2.5 (XC-002-001) and #8 §1.7.3 (XC-008-001).
- No PatternType / RunType enum anywhere in spec text (KD-8).

### 1.8 Stage-Binding Statement
**Spec drafted at Stage 0; runtime activates at Stage 1.** See Stage-Binding
Clarification above. Stage 0 deliverable = published specification only.

### 1.9 Version History

---

## SECTION 2 — FUNCTIONAL REQUIREMENTS, DATA STRUCTURES, FAILURE MODES

### 2.1 Functional Requirements Table (provisional 36-entry enumeration)

Conformance: `MUST` unless noted.

| FR | Subject | Conf. | Source |
|---|---|---|---|
| FR-AT-001 | Tactical tick rate is 10 Hz | MUST | CLAUDE.md / KD-2 |
| FR-AT-002 | Output is one `AttackDirective` per team per tick + one `AttackIntent` per off-ball agent per tick | MUST | KD-2 |
| FR-AT-003 | Agent iteration order is EntityId-sorted ascending | MUST | #16 §3.2.5 / KD-11 |
| FR-AT-004 | `AttackDirective` and `AttackIntent[]` contribute to per-tick digest | MUST | #16 §6.2 / KD-11 |
| FR-AT-005 | RNG calls use `DOMAIN_TAG_ATTACKING_AI` (`[CROSS-PENDING]` `0x1B` until ERR-015-001 ratified) | MUST | #16 §3.4 / KD-11 |
| FR-AT-006 | GK is excluded from the attacking movement pool | MUST | KD-7 |
| FR-AT-007 | Ball carrier is excluded from the attacking movement pool | MUST | KD-3 |
| FR-AT-008 | Phase gating: if #12 phase is OUT_OF_POSSESSION for this team, emit empty directive; cease algorithm | MUST | KD-5 / §3.1 |
| FR-AT-009 | Transition phase: if phase is TRANSITION, emit empty directive for `TRANSITION_HOLD_TICKS [GT]` then cease | MUST | KD-6 / §3.9 |
| FR-AT-010 | Off-ball movement is fully parameterized by `RunParameters`; no PatternType / RunType enum | MUST | KD-8 |
| FR-AT-011 | `RunParameters` contains exactly three fields: `depthOffset_m`, `lateralOffset_m`, `runTriggerTick`; angle is a derived quantity computed at use-site only | MUST | KD-8 / §3.4 |
| FR-AT-012 | Attacking role catalog is exactly: RUNNER, SUPPORT_BALL, HOLD_WIDTH, WEAK_SIDE | MUST | §3.3 |
| FR-AT-013 | Agents within `SUPPORT_RADIUS_M [GT]` of ball carrier default to SUPPORT_BALL unless assigned RUNNER | MUST | §3.5 |
| FR-AT-014 | At least `MIN_WIDTH_HOLDERS [GT]` agents hold width on the near-touchline side at all times | MUST | §3.6 |
| FR-AT-015 | At least one agent holds the WEAK_SIDE position unless agent count < `MIN_WEAK_SIDE_AGENT_THRESHOLD [GT]` | MUST | §3.7 |
| FR-AT-016 | Overload is declared when ≥ `OVERLOAD_COUNT [GT]` agents (excluding WEAK_SIDE) are in `OVERLOAD_ZONE_WIDTH_M [GT]` Y-corridor on the same flank | MUST | §3.8 |
| FR-AT-017 | Team-style profile modifies `depthOffset_m`, `runTriggerTick`, `MAX_RUNNERS`, `SUPPORT_RADIUS_M` via constant multipliers; no algorithm branching | MUST | KD-12 / §3.10 |
| FR-AT-018 | Anti-chaos: simultaneous RUNNER roles ≤ `MAX_RUNNERS [GT]` | MUST | KD-13 |
| FR-AT-019 | Anti-chaos: at least `MIN_SUPPORT_AGENTS [GT]` agents in SUPPORT_BALL or HOLD_WIDTH | MUST | KD-13 |
| FR-AT-020 | Anti-chaos: no RUNNER `runTargetPosition.x` (normalised to distance-to-opponent-goal) assigned beyond `OWN_HALF_RUN_BLOCK_M [GT]` from the half-line | MUST | KD-13 |
| FR-AT-021 | Anti-chaos invariants checked POST-assignment, PRE-publication | MUST | KD-13 / §3.11 |
| FR-AT-022 | Assignment transitions use dwell-time hysteresis binding to #2 §3.1 | MUST | KD-11 / §3.12 |
| FR-AT-023 | Hysteresis: role/target transition fires only after new candidate preferred for `ATTACK_DWELL_TICKS [EST]` consecutive ticks | MUST | §3.12 |
| FR-AT-024 | Failure mode F1: stale perception → freeze previous tick directive | MUST | §2.4 |
| FR-AT-025 | Failure mode F2: #12 slot unavailable → emit empty directive | MUST | §2.4 |
| FR-AT-026 | Failure mode F3: anti-chaos unresolvable after `MAX_INVARIANT_PASSES [GT]` → emit all-default directive | MUST | §2.4 / KD-13 |
| FR-AT-027 | Failure mode F4: phase unavailable from #12 → treat as OUT_OF_POSSESSION (safe fallback) | MUST | §2.4 |
| FR-AT-028 | All formulas have units, valid input ranges, and at least one worked example in §3 or Appendix | MUST | CLAUDE.md |
| FR-AT-029 | All constants tagged per KD-13 in CLAUDE.md | MUST | CLAUDE.md |
| FR-AT-030 | Single constant catalogue `AttackingAIConstants.cs` per #20 §4.2 | MUST | #20 FR-CS-025 / KD-14 |
| FR-AT-031 | No interface produced against any consumer spec at Stage 0 | MUST | CLAUDE.md Interface Design Principle |
| FR-AT-032 | Fatigue input convention `0 = rested` | MUST | CLAUDE.md / KD-1 |
| FR-AT-033 | Stage-1 activation gated on: (a) #8 ERR-015-002 amendment ratified; (b) #12 APPROVED; (c) ERR-015-003/004 #17 channel rows landed | MUST | KD-17 / §7 |
| FR-AT-034 | Dangerous-zone surrogate metric declared and measurable per KD-10 | MUST | KD-10 / §5.7 |
| FR-AT-035 | Tactical-identity acceptance criteria measurable per KD-10 | MUST | KD-10 / §5.8 |
| FR-AT-036 | `AttackDirective.overloadActive` flag emitted when overload condition met | MUST | §3.8 / FR-AT-016 |

### 2.2 Data Structures

| Struct | Purpose | Stage |
|---|---|---|
| `AttackDirective` | Per-team per-tick: team `EntityId`, `overloadActive` bool, `overloadFlank` (LEFT/RIGHT), `transitionHoldTick` countdown | 1 (spec'd at 0) |
| `AttackIntent` | Per-off-ball-agent per-tick: `role` enum (RUNNER/SUPPORT_BALL/HOLD_WIDTH/WEAK_SIDE), `runParameters RunParameters?` (null unless RUNNER), `validThroughTick` | 1 (spec'd at 0) |
| `RunParameters` | Sub-struct of `AttackIntent`: `depthOffset_m f32`, `lateralOffset_m f32`, `runTriggerTick i32` — three fields; angle is derived at use-site | 1 (spec'd at 0) |
| `AttackHysteresisState` | Per-agent: dwell counter for role transitions; authoritative state digested per KD-11 | 1 |
| `TransitionHoldState` | Per-team: countdown ticks remaining after possession loss | 1 |
| `AttackIntentSnapshot` | Read-only view for #17 channel emission and tests | 1 |

### 2.3 Inputs (read-only at tick start)
- Perception snapshot (#7 §3.7): agent positions, ball position, ball carrier
  EntityId, possession state, attribute lookups (`Pace`, `Stamina`, `Dribbling`).
- #12 baseline `formationSlot` per agent (via `BaselineShape` read-only view),
  including `formationSlot.lineMembership` (DEFENSE/MIDFIELD/ATTACK — used for
  RUNNER eligibility in §3.3) and `formationSlot.lateralPct` (float 0–1 — used
  to derive `lateralOffset_m` in §3.4).
- #12 phase enum for this team (IN_POSSESSION / OUT_OF_POSSESSION / TRANSITION).
- #12 `laneAssignment` per agent (5-bin lateral enum — LEFT_WIDE/LEFT/CENTRE/
  RIGHT/RIGHT_WIDE; used only in §3.6 to identify which agents hold the touchline).
- `teamAttackAngle`: match-half constant derived from team goal assignment —
  `0.0 rad` for the team attacking toward x=105; `π rad` for the team attacking
  toward x=0. Source: match configuration record, not the perception snapshot.
  Used in §3.4 to decompose run offsets into pitch-frame vectors.

### 2.4 Failure Modes (F1–F4 above)
Section files enumerate each with: detection condition, recovery action,
test reference.

### 2.5 Version History

---

## SECTION 3 — CORE FORMULAS AND ALGORITHMS

### 3.1 Phase gating (binding to #12 §3.0 per KD-5 / KD-6)
Read #12 phase for this team.
- If OUT_OF_POSSESSION: emit empty directive; return immediately.
- If TRANSITION or phase just changed from IN_POSSESSION → TRANSITION:
  dispatch to §3.9 (TransitionController); return the directive §3.9 emits.
- If IN_POSSESSION: reset `transitionHoldTick` counter to 0; proceed to §3.2.

Note (resolves adversarial finding M-6): The TRANSITION counter is SET in §3.9
when the phase first changes. §3.1 is a pure gate that dispatches to §3.9 for
non-IN_POSSESSION states. §3.9 owns the set/decrement/emit logic so there is
no ambiguity about which step comes first in algorithm execution order.

### 3.2 Attacking pool construction (binding to KD-3 / KD-7)
Attacking pool = all agents on this team EXCLUDING:
- GK EntityId (KD-7; GK exclusion is permanent and unconditional).
- Ball carrier EntityId (KD-3; resolved from perception snapshot).
Result: up to 10 off-ball agents (11 outfield − ball carrier; GK already excluded).

Worked example: 11-agent team, 1 GK + 1 ball carrier = 9 off-ball agents
in the attacking pool.

### 3.3 Role assignment algorithm
For each agent in pool (EntityId-ascending order):
1. Check hysteresis (§3.12) — if current role is stable for ≥ `ATTACK_DWELL_TICKS`,
   retain current role.
2. Else, compute candidate roles in priority order:
   a. RUNNER — if `formationSlot.lineMembership` is ATTACK or MIDFIELD (forward
      lines; controls which agents make deep runs toward goal) AND fewer than
      `MAX_RUNNERS` runners already assigned. NOTE: controlled by `lineMembership`,
      NOT `laneAssignment` — these are distinct #12 fields. `laneAssignment`
      controls lateral bin (5-bin classification); `lineMembership` controls
      forward/backward line position.
   b. SUPPORT_BALL — if agent is within `SUPPORT_RADIUS_M` of ball carrier.
   c. WEAK_SIDE — if agent is in the weak-side Y-corridor (KD-16) and fewer
      than `MIN_WEAK_SIDE_AGENT_THRESHOLD` weak-side agents already assigned.
   d. HOLD_WIDTH — default role for all other agents.
3. Assign the highest-priority eligible role.
4. If RUNNER: generate `RunParameters` (§3.4).

### 3.4 Run parameter generation
For an agent assigned RUNNER role.

**Coordinate-frame definition (resolves adversarial finding M-5):**
`teamAttackAngle` = the team's current attack direction in pitch-frame:
- `0.0 rad` for the team attacking the x=105 goal (positive-X direction).
- `π rad` for the team attacking the x=0 goal (negative-X direction).
This is a match-half constant, NOT the ball-carrier's velocity vector.
Using velocity would produce degenerate runs from a stationary carrier.

`depthOffset_m` is the forward component (in the `teamAttackAngle` direction).
`lateralOffset_m` is the perpendicular component (positive = toward y=68 touchline
in the x=105 attack direction; positive = toward y=0 touchline in the x=0
attack direction, due to π rotation). This preserves "positive = right side
from attacking perspective" regardless of which goal the team attacks.

```
// Step 1: compute raw offsets
depthOffset_m   = Clamp(BASE_RUN_DEPTH_M [GT] × styleProfile.depthMult, 5.0, 40.0)

// lateralPct ∈ [0, 1] from formationSlot (confirmed field in #12 §2.2)
// centeredPct ∈ [−0.5, +0.5]; positive = toward y=68 side in pitch-frame
centeredPct     = formationSlot.lateralPct − 0.5
lateralOffset_m = Clamp(centeredPct × PITCH_WIDTH_M × LATERAL_SCALE [GT], −34.0, 34.0)

runTriggerTick  = currentTick + max(1, round(BASE_RUN_TRIGGER_DELAY_TICKS [GT]
                                             × styleProfile.timingMult))

// Step 2: compute run target in pitch-frame
depthVec    = Vector2(cos(teamAttackAngle), sin(teamAttackAngle)) × depthOffset_m
lateralVec  = Vector2(−sin(teamAttackAngle), cos(teamAttackAngle)) × lateralOffset_m
runTargetPosition = ballCarrier.position + depthVec + lateralVec

// Clamp to pitch boundary
runTargetPosition.x = Clamp(runTargetPosition.x, 0, PITCH_LENGTH_M)
runTargetPosition.y = Clamp(runTargetPosition.y, 0, PITCH_WIDTH_M)
```

`styleProfile.depthMult`, `styleProfile.timingMult`, and `styleProfile.supportMult`
are constant-catalogue multipliers loaded at match initialisation (KD-12).
`LATERAL_SCALE [GT]` controls how much of the agent's lane offset translates
into a lateral run deviation; proposed 0.8 (agents run slightly narrower than
their baseline lane position).

**Valid input ranges:**
- `depthOffset_m` ∈ [5.0, 40.0] m (Clamp applied)
- `lateralOffset_m` ∈ [−34.0, 34.0] m (Clamp applied; half pitch width)
- `runTriggerTick` ≥ currentTick + 1 (always future; minimum 1 tick delay)

**Worked example (DIRECT profile, team attacking x=105):**
Ball carrier at (70, 34), `teamAttackAngle = 0`. Agent's `formationSlot.lateralPct = 0.75`
(right-side channel). `BASE_RUN_DEPTH_M = 15.0`, `depthMult (DIRECT) = 1.4`,
`LATERAL_SCALE = 0.8`, `BASE_RUN_TRIGGER_DELAY_TICKS = 3`, `timingMult (DIRECT) = 0.7`.
- `depthOffset_m = Clamp(15.0 × 1.4, 5, 40) = 21.0m`
- `centeredPct = 0.75 − 0.5 = 0.25`
- `lateralOffset_m = Clamp(0.25 × 68 × 0.8, −34, 34) = Clamp(13.6, …) = 13.6m`
- `depthVec = Vector2(1, 0) × 21.0 = (21, 0)` (attack direction = +X)
- `lateralVec = Vector2(0, 1) × 13.6 = (0, 13.6)` (perpendicular = +Y)
- `runTargetPosition = (70, 34) + (21, 0) + (0, 13.6) = (91, 47.6)` — inside
  final third, right channel → "overlap" geometry in gameplay vocabulary.
- `runTriggerTick = currentTick + max(1, round(3 × 0.7)) = currentTick + 2`.

### 3.5 Support radius heuristic
For each pool agent not assigned RUNNER on this tick:
- Compute `distanceToBallCarrier` (Euclidean, using #7 perception positions).
- If `distanceToBallCarrier ≤ SUPPORT_RADIUS_M × styleProfile.supportMult`:
  candidate for SUPPORT_BALL (subject to role priority in §3.3).

Units: m. Clamped minimum: 5.0m (below this, agent is in physical contact range
and should be treated as on-ball).

### 3.6 Width-holding protocol
After role assignment (§3.3), check: count of HOLD_WIDTH + WEAK_SIDE agents
on the near-touchline side.
If < `MIN_WIDTH_HOLDERS`: the nearest HOLD_WIDTH or non-RUNNER agent (by
absolute Y-deviation from the near touchline, ascending sort) is promoted to
HOLD_WIDTH.

HOLD_WIDTH target position (resolves adversarial finding M-4 — explicit
per-team derivation, no ambiguous constant naming):
```
// TOUCHLINE_HOLD_DIST_M [GT] = distance from the touchline (NOT absolute Y)
// "Near touchline" = the touchline on the same side as the ball
if ball.y >= PITCH_WIDTH_M / 2:
    // ball on the y=68 side
    nearTouchlineY = PITCH_WIDTH_M - TOUCHLINE_HOLD_DIST_M   // e.g. 64.0 m
else:
    // ball on the y=0 side
    nearTouchlineY = TOUCHLINE_HOLD_DIST_M                   // e.g. 4.0 m

targetPosition.x = ballCarrier.position.x    // tracks ball x (same depth)
targetPosition.y = nearTouchlineY
```

Units: all in m. The derivation is the same regardless of which goal each
team attacks — the formula depends only on ball.y, not team orientation.
Worked example: ball.y = 50 (y=68 side). `TOUCHLINE_HOLD_DIST_M = 4.0m`.
`nearTouchlineY = 68 − 4 = 64.0m`. Width-holder goes to (ballCarrier.x, 64).

### 3.7 Weak-side positioning
A weak-side agent holds the opposite half of the Y-axis from the ball. Formal
definition of weak-side Y-corridor (KD-16):
```
if ball.y > PITCH_WIDTH_M / 2:
    weakSideTarget.y = WEAK_SIDE_FAR_Y_M [GT]      // near y=0 touchline
else:
    weakSideTarget.y = PITCH_WIDTH_M - WEAK_SIDE_FAR_Y_M [GT]  // near y=68 touchline
weakSideTarget.x = ballCarrier.position.x + WEAK_SIDE_DEPTH_OFFSET_M [GT]
```

Assign the agent with the greatest Y-deviation from the ball to WEAK_SIDE.
EntityId tie-break. This is a O(N) scan over pool agents, N ≤ 9.

### 3.8 Overload detection
After all roles are assigned, scan the pool for overload:
```
nearSideAgents = [a for a in pool
                  if sameFlank(a.position.y, ball.y, OVERLOAD_ZONE_WIDTH_M) 
                  and a.role != WEAK_SIDE]
if len(nearSideAgents) >= OVERLOAD_COUNT:
    AttackDirective.overloadActive = true
    AttackDirective.overloadFlank  = (ball.y > PITCH_WIDTH_M/2) ? RIGHT : LEFT
else:
    AttackDirective.overloadActive = false
```

`sameFlank(agentY, ballY, zoneWidth)` := |agentY − ballY| ≤ zoneWidth.

Worked example: ball at y=50, `OVERLOAD_ZONE_WIDTH_M = 20.0`. Agents at y = 45,
52, 60 are within 20m of ball.y=50 (not WEAK_SIDE). Count = 3 ≥ `OVERLOAD_COUNT = 3`
→ overload declared, RIGHT flank.

### 3.9 Transition-to-defense behavior (TransitionController — resolves M-6)

This section is the authoritative transition logic. §3.1 dispatches here;
§3.1 does not duplicate this logic.

```
// Called from §3.1 when phase is TRANSITION
// or when phase just changed from IN_POSSESSION to TRANSITION / OUT_OF_POSSESSION

function TransitionController(prevPhase, currentPhase, state):

    // Step 1: detect phase change and SET counter (must come before DECREMENT)
    if prevPhase == IN_POSSESSION and currentPhase != IN_POSSESSION:
        state.transitionHoldTick = TRANSITION_HOLD_TICKS   // SET on first transition tick

    // Step 2: decrement and emit
    if state.transitionHoldTick > 0:
        state.transitionHoldTick -= 1
        return frozenLastDirective         // frozen: no new runs triggered
    else:
        return emptyDirective              // countdown expired

    // Step 3: reset on return to IN_POSSESSION (handled in §3.1 gate)
```

`TRANSITION_HOLD_TICKS [GT]` for POSSESSION/DIRECT profiles: 5 ticks.
`TRANSITION_HOLD_TICKS [GT]` for COUNTER_ATTACK profile: 0 (instant recovery;
agents immediately default to #12 baseline on possession loss).

Note: the trigger is purely phase detection from #12. No direct call from #14.
If #14's `MarkDirective.emergencyFlag` is consumed at Stage 1+ to override
`transitionHoldTick` to 0 (immediate empty), that coupling is a Stage 1+
boundary hint declared in KD-6 and not implemented at Stage 0.

### 3.10 Team-style profile application
Profile multipliers are applied as scale factors in §3.4 (run depth and timing)
and §3.5 (support radius). The multipliers are `[GT]` constants in the
catalogue. The algorithm code is identical regardless of profile; only the
constant values differ. This enforces KD-8 (no enum branching in physics/AI
algorithm code).

Profile-multiplier catalogue (provisional; promoted to `[GT]` at section-file
authoring with derivations in Appendix A):

| Multiplier | POSSESSION | DIRECT | COUNTER_ATTACK |
|---|---|---|---|
| `DEPTH_MULT` | 0.8 | 1.4 | 1.6 |
| `TIMING_MULT` | 1.2 | 0.7 | 0.5 |
| `SUPPORT_MULT` | 1.3 | 0.8 | 0.5 |
| `MAX_RUNNERS_OVERRIDE` | 1 | 3 | 4 |

These four constants × 3 profiles = 12 `[GT]` constants in the catalogue.

### 3.11 Anti-chaos invariant enforcement (KD-13)
Apply invariants in order (post-role-assignment, pre-publication):
1. Count RUNNER roles; if > `MAX_RUNNERS`, demote excess runners to
   SUPPORT_BALL (lowest `depthOffset_m` runner first; EntityId tie-break).
   Re-check.
2. Count SUPPORT_BALL + HOLD_WIDTH roles; if < `MIN_SUPPORT_AGENTS`, the
   shallowest RUNNER (smallest `depthOffset_m`) is demoted to SUPPORT_BALL.
   Re-check.
3. For each RUNNER: if `runTargetPosition.x` (normalised) is in own half
   beyond `OWN_HALF_RUN_BLOCK_M` of half-line → demote to HOLD_WIDTH.
   Re-check.
4. If any invariant still violated after `MAX_INVARIANT_PASSES [GT]`
   iterations: emit all-default directive (all agents HOLD_WIDTH or
   SUPPORT_BALL; no runners) for this tick (FR-AT-026).

### 3.12 Assignment hysteresis (KD-11, binding to #2 §3.1)
A role/target transition fires only after the new candidate has been preferred
for `ATTACK_DWELL_TICKS [EST]` consecutive ticks. Prevents role-thrash when
boundary conditions oscillate between ticks (e.g., agent at exactly
SUPPORT_RADIUS_M boundary).

### 3.13 Pseudocode — per-tick main loop
```
1.  Read perception snapshot (#7), #12 slots/phase/lineMembership/lateralPct.
2.  Gate on phase (§3.1 — pure gate; dispatches to §3.9 for non-IN_POSSESSION):
        if OUT_OF_POSSESSION → emit empty directive; return.
        if TRANSITION → call TransitionController(§3.9); return its directive.
        if IN_POSSESSION → reset transitionHoldTick to 0; continue.
3.  Build attacking pool: all agents − GK − ball carrier (§3.2).
4.  For each agent in pool (EntityId-ascending):
        Check hysteresis (§3.12) — retain if dwell valid.
        Else: compute candidate role using lineMembership/lateralPct (§3.3); assign.
        If RUNNER: generate RunParameters using teamAttackAngle + lateralPct (§3.4).
5.  Validate support radius for SUPPORT_BALL candidates (§3.5).
6.  Enforce width-holding: promote agent(s) using TOUCHLINE_HOLD_DIST_M if
    MIN_WIDTH_HOLDERS not met (§3.6).
7.  Assign WEAK_SIDE agent (§3.7).
8.  Compute overload flag (§3.8).
9.  Apply anti-chaos invariants (§3.11); demote / fallback if needed.
10. Publish AttackDirective + per-agent AttackIntent.
```

### 3.14 Constants catalogue (forward ref to §6.1)
All thresholds enumerated in §6.1. All `[EST]` at outline stage; promoted
to `[GT]` at section-file authoring with Appendix A derivations.

### 3.15 Version History

---

## SECTION 4 — ARCHITECTURE, FILE LAYOUT, INTERFACE CONTRACTS

### 4.1 Architecture Overview
Single subsystem `AttackingAI` on the 10 Hz scheduler. Pure-function design
except for hysteresis state and transition-hold state (authoritative;
digested). **Runtime activates at Stage 1** — Stage 0 ships the spec, not
the code.

### 4.2 File Structure (#20 §4.2 compliant — single catalogue)
```
src/AttackingAI/                                     (Stage 1+)
├── AttackingAITick.cs           (10 Hz entry point)
├── AttackingPoolBuilder.cs      (§3.2 — GK + ball-carrier exclusion)
├── RoleAssigner.cs              (§3.3 role assignment; §3.4 RunParameters)
├── SupportHeuristic.cs          (§3.5 support radius computation)
├── WidthHolder.cs               (§3.6 width-holding protocol)
├── WeakSideController.cs        (§3.7 weak-side positioning)
├── OverloadDetector.cs          (§3.8 overload zone detection)
├── TransitionController.cs      (§3.9 transition-to-defense hold)
├── InvariantEnforcer.cs         (§3.11 anti-chaos invariants)
├── AttackHysteresis.cs          (§3.12 dwell-time state; authoritative)
└── AttackingAIConstants.cs      (SINGLE catalogue per FR-CS-025 / KD-14)
```

### 4.3 Internal Module Contracts
Module-by-module input/output declared as `readonly struct` parameters.
No `class` types on hot path per #18 §3.7 zero-alloc rule.

### 4.4 Upstream Integration Contracts
- Perception snapshot (#7 §3.7) at tick start.
- #12 baseline `formationSlot` per agent (read-only view).
- #12 `laneAssignment` per agent.
- #12 phase enum (`PositioningAI.GetPhase` — Stage 1+).

### 4.5 Downstream Integration Contracts
- `AttackDirective` + `AttackIntent[]` consumed by orchestrator, which
  (a) composes with #12 `RunIntent` writer-layer before #8 reads target
  positions, and (b) makes `AttackIntent.role` visible to #8's PASS utility
  via the KD-3 mechanism (ERR-015-002 choice: `TacticalContext` extension
  or accessor).
- `AttackIntentSnapshot` read-only view for #17 event emission and tests.

#### 4.5.1 To #8 Decision Tree (Stage 1+ — declared, not implemented)
`AttackIntent` published by #15 informs #8's PASS utility: a RUNNER is a
higher-priority pass target than a HOLD_WIDTH agent. The specific mechanism
is ERR-015-002; no interface is authored at Stage 0 (Interface Design
Principle).

#### 4.5.2 To #12 Positioning AI (Stage 1+ — declared, not implemented)
`RunParameters.runTargetPosition` feeds into the `RunIntent` writer-layer
declared in #12 §4.5. The orchestrator writes #15's computed position into
#12's `RunIntent` slot before #8's MOVE_TO_POSITION utility reads target
positions. No runtime code at Stage 0.

### 4.6 Determinism & Safety Boundaries (binding to #16)
Iteration order: EntityId-ascending per #16 §3.2.5. RNG domain tag
`DOMAIN_TAG_ATTACKING_AI = 0x1B [CROSS-PENDING]`. Digest scope: attack
directive, attack intents, run parameters, hysteresis dwell counters,
transition-hold countdown. All are authoritative simulation state per #16 §3.2.

### 4.7 Cross-Specification Validation Checks
- No GK in pool (KD-7; build-time grep).
- Ball carrier excluded from pool (KD-3).
- No PatternType / RunType enum anywhere in `src/AttackingAI/`
  (build-time grep; same protocol as Shot Mechanics #6 §9 enum-grep).
- Fatigue convention (KD-1).
- EntityId no-reuse cited (#2 §2.5 / #8 §1.7.3).
- Action selection absent from all #15 code (no PASS/SHOOT/DRIBBLE calls).

### 4.8 Version History

---

## SECTION 5 — TEST PLAN

### 5.1 Test Counts (verifiable target)

| Category | Target | Source |
|---|---|---|
| Unit (pool construction, role assignment, RunParameters, support heuristic, width-holding, weak-side, overload, transition, hysteresis, invariant enforcement) | ≥52 | §3.2–§3.12 |
| Integration (full-team possession phase under each profile, possession→transition→defense sequence) | ≥12 | §3.13 |
| Determinism regression | ≥6 | #16 §5 |
| Performance | ≥3 | §6 |
| Anti-chaos invariant tests | ≥6 | KD-13 |
| Style-profile tactical-identity tests | ≥6 | KD-10 / KD-12 |
| **Total** | **≥85** | — |

### 5.2 Unit Test List (representative)
- Pool construction: GK excluded regardless of position.
- Pool construction: ball carrier excluded.
- Phase gate: OUT_OF_POSSESSION → empty directive emitted.
- Phase gate: TRANSITION → frozen directive emitted for N ticks, then empty.
- Role assignment: RUNNER assigned only to agents where `lineMembership` is ATTACK or MIDFIELD.
- Role assignment: SUPPORT_BALL assigned when within SUPPORT_RADIUS_M.
- RunParameters: depthOffset_m clamped to [5.0, 40.0].
- RunParameters: runTriggerTick is never in the past.
- RunParameters: run target position computed correctly from depthOffset, lateralOffset, and teamAttackAngle.
- Width-holding: min width holders enforced by promotion.
- Weak-side: correct agent selected; EntityId tie-break.
- Overload: fires when ≥ OVERLOAD_COUNT agents in corridor.
- Overload: WEAK_SIDE agent excluded from overload count.
- Anti-chaos MAX_RUNNERS: excess runners demoted to SUPPORT_BALL.
- Anti-chaos MIN_SUPPORT_AGENTS: shallowest runner demoted.
- Anti-chaos own-half block: runner in own half demoted.
- Anti-chaos unresolvable: all-default fallback emitted.
- Hysteresis: role oscillation suppressed for ATTACK_DWELL_TICKS.
- Failure F1: stale perception → frozen directive.
- Failure F2: #12 unavailable → empty directive.
- No PatternType / RunType enum present (grep-based test).

### 5.3 Integration Test List
- Full 9-agent IN_POSSESSION tick with POSSESSION profile: correct distribution
  of roles (≤1 RUNNER, ≥1 SUPPORT_BALL, ≥2 HOLD_WIDTH, ≥1 WEAK_SIDE).
- Full 9-agent IN_POSSESSION tick with DIRECT profile: ≤3 RUNNERS, faster
  run timing.
- Full 9-agent IN_POSSESSION tick with COUNTER_ATTACK profile: ≤4 RUNNERS,
  zero TRANSITION_HOLD_TICKS.
- Possession → TRANSITION → OUT_OF_POSSESSION sequence over 15 ticks:
  directive correctly frozen then emptied.
- Overload declared on correct flank for ball on right side.
- Anti-chaos invariant combination: MAX_RUNNERS + MIN_SUPPORT violation
  resolved in < MAX_INVARIANT_PASSES.

### 5.4 Determinism Regression (binding to #16 §5)
- 90-minute match replay produces bit-identical per-tick digest on reference
  host.

### 5.5 Performance Validation (binding to §6)
Per-tick budget measured against named host (§6.3).

### 5.6 Anti-Chaos & Profile Tests
- One test per anti-chaos invariant asserting demotion cascade and
  eventual all-default fallback.
- Three style-profile tests: DIRECT must produce more runners per match than
  POSSESSION by ≥ `DIRECT_RUN_COUNT_DELTA [GT]` (measurable from intent log).
- Three counter-attack tests: COUNTER_ATTACK must produce zero transition-hold
  ticks on possession loss.

### 5.7 Dangerous-Zone Surrogate Validation (resolves outline.md H-5)
Stage-0 surrogate metric (KD-10):
- Shots in dangerous zone: count of SHOOT actions (per #8) where
  `distanceToGoalCenter ≤ DANGER_ZONE_MAX_DIST_M [GT]` AND
  `|Y − goalCenter.Y| ≤ DANGER_ZONE_CORRIDOR_HW_M [GT]`.
- Average shot distance: mean of `distanceToGoalCenter` over all SHOOT actions.
Test declares the measurement method. Actual threshold validation is Stage 1+
(no simulation code at Stage 0). The spec makes the method unambiguous now.

### 5.8 Tactical Identity Tests (resolves outline.md L-13)
Per KD-10:
- DIRECT vs. POSSESSION: DIRECT profile produces ≥ `DIRECT_RUN_COUNT_DELTA [GT]`
  more RUNNER assignments per simulated match (measurable from `AttackIntent`
  role histograms).
- COUNTER_ATTACK: produces ≤ `COUNTER_MAX_HOLD_TICKS [GT]` avg transition-hold
  ticks per possession loss (measurable from `TransitionHoldState` log).

### 5.9 Version History

---

## SECTION 6 — PERFORMANCE ANALYSIS AND BUDGETS

### 6.1 Constant Catalogue
Full enumeration with tags. All `[EST]` at outline stage unless noted;
promoted to `[GT]` at section-file authoring with Appendix A derivations.

| Constant | Tag | Proposed value | Purpose |
|---|---|---|---|
| `SUPPORT_RADIUS_M` | `[GT]` | 12.0 m | Radius within which agents are SUPPORT_BALL candidates |
| `MIN_WIDTH_HOLDERS` | `[GT]` | 2 | Minimum agents holding near-touchline width |
| `MIN_WEAK_SIDE_AGENT_THRESHOLD` | `[GT]` | 4 | Minimum pool size before WEAK_SIDE is assigned |
| `OVERLOAD_COUNT` | `[GT]` | 3 | Agents in flank corridor required to declare overload |
| `OVERLOAD_ZONE_WIDTH_M` | `[GT]` | 20.0 m | Y-half-width of overload detection corridor |
| `TOUCHLINE_HOLD_DIST_M` | `[GT]` | 4.0 m | Distance from nearest touchline for HOLD_WIDTH target (NOT absolute Y; see §3.6 formula) |
| `WEAK_SIDE_FAR_Y_M` | `[GT]` | 8.0 m | Distance from weak-side touchline for WEAK_SIDE target |
| `WEAK_SIDE_DEPTH_OFFSET_M` | `[GT]` | 5.0 m | X-offset (toward goal) for weak-side position |
| `MAX_RUNNERS` | `[GT]` | 2 | Anti-chaos: max simultaneous RUNNER roles (POSSESSION profile; overridden by style profile) |
| `MIN_SUPPORT_AGENTS` | `[GT]` | 1 | Anti-chaos: min SUPPORT_BALL + HOLD_WIDTH agents |
| `OWN_HALF_RUN_BLOCK_M` | `[GT]` | 5.0 m | Anti-chaos: run targets must not be >5m past own half-line into own half |
| `MAX_INVARIANT_PASSES` | `[GT]` | 3 | Max demotion iterations before all-default fallback |
| `ATTACK_DWELL_TICKS` | `[EST]` | 3 | Dwell ticks before role/target transition fires |
| `TRANSITION_HOLD_TICKS` | `[GT]` | 5 | Ticks to hold attack directive after possession loss (POSSESSION profile; 0 for COUNTER) |
| `BASE_RUN_DEPTH_M` | `[GT]` | 15.0 m | Base run target depth (before profile multiplier) |
| `LATERAL_SCALE` | `[GT]` | 0.8 | Scale factor converting `lateralPct` deviation to lateral run offset; < 1 so runs stay slightly narrower than baseline lane |
| `BASE_RUN_TRIGGER_DELAY_TICKS` | `[GT]` | 3 | Base run trigger delay in ticks (before profile multiplier) |
| `DANGER_ZONE_MAX_DIST_M` | `[GT]` | 20.0 m | Max distance to goal center for dangerous-zone surrogate |
| `DANGER_ZONE_CORRIDOR_HW_M` | `[GT]` | 10.16 m | Half-width of dangerous zone; derived as PENALTY_BOX_HW_M (20.16m) / 2 = 10.08m ≈ 10.16m (rounded to align with 6-yard-box edge plus goalkeeper diving reach; derivation in Appendix A) |
| `FINAL_THIRD_X_M` | `[DERIVED]` | 70.0 m | X threshold for final third; `PITCH_LENGTH_M × 2/3` |
| `PITCH_LENGTH_M` | `[CROSS: #1 §1.2]` | 105.0 m | X-axis pitch length |
| `PITCH_WIDTH_M` | `[CROSS: #1 §1.2]` | 68.0 m | Y-axis pitch width |
| `HALF_LINE_X` | `[CROSS: #1 §1.2]` | 52.5 m | Midfield line x-coordinate |
| `DEPTH_MULT_POSSESSION` | `[GT]` | 0.8 | Run depth multiplier — POSSESSION style |
| `DEPTH_MULT_DIRECT` | `[GT]` | 1.4 | Run depth multiplier — DIRECT style |
| `DEPTH_MULT_COUNTER` | `[GT]` | 1.6 | Run depth multiplier — COUNTER_ATTACK style |
| `TIMING_MULT_POSSESSION` | `[GT]` | 1.2 | Run timing multiplier — POSSESSION style |
| `TIMING_MULT_DIRECT` | `[GT]` | 0.7 | Run timing multiplier — DIRECT style |
| `TIMING_MULT_COUNTER` | `[GT]` | 0.5 | Run timing multiplier — COUNTER_ATTACK style |
| `SUPPORT_MULT_POSSESSION` | `[GT]` | 1.3 | Support radius multiplier — POSSESSION style |
| `SUPPORT_MULT_DIRECT` | `[GT]` | 0.8 | Support radius multiplier — DIRECT style |
| `SUPPORT_MULT_COUNTER` | `[GT]` | 0.5 | Support radius multiplier — COUNTER_ATTACK style |
| `MAX_RUNNERS_POSSESSION` | `[GT]` | 1 | Max runners override — POSSESSION style |
| `MAX_RUNNERS_DIRECT` | `[GT]` | 3 | Max runners override — DIRECT style |
| `MAX_RUNNERS_COUNTER` | `[GT]` | 4 | Max runners override — COUNTER_ATTACK style |
| `DIRECT_RUN_COUNT_DELTA` | `[GT]` | 15 | Min additional runner assignments per match in DIRECT vs. POSSESSION (tactical identity acceptance criterion) |
| `COUNTER_MAX_HOLD_TICKS` | `[GT]` | 0 | Max avg transition-hold ticks for COUNTER_ATTACK (acceptance criterion) |

Total constants: ~36 (all `[GT]` or `[CROSS]` or `[DERIVED]`; no `[EST]`
except `ATTACK_DWELL_TICKS` — to be promoted at section-file authoring).

### 6.2 Hot Path Enumeration (#18 KD-10 binding)
Main per-tick loop:
- Pool construction: O(N), N ≤ 11.
- Role assignment: O(N), N ≤ 10 (off-ball agents; inner check is O(1) per agent).
- Support radius scan: O(N).
- Width-holding: O(N) sort by |Y − touchline|, at most once per tick.
- Weak-side: O(N) scan.
- Overload detection: O(N).
- Anti-chaos enforcement: O(N) up to 3 passes; worst case 30 iterations.

**Total worst-case evaluations per tick: ≈120 floating-point ops + 30 comparison ops.
Bounded by O(N) with constant factors.** Substantially cheaper than #14 (no
opponent scoring, no tackle intent evaluation).

### 6.3 Per-Tick Budget (reference host per #18 KD-9)
Target: ≤0.10 ms per 10 Hz tick on the named reference host (Ryzen 7 5800X
@ 4.5 GHz, single thread, Mono backend, Unity 2022.3 LTS — same as #13 and
#14 per-tick budget anchor). Caveat: cert host budget supersedes once
`certification-platform.md` is pinned by lead developer.

### 6.4 Per-Frame Budget
N/A — no per-frame work.

### 6.5 Memory Footprint
`AttackDirective` + `AttackIntent[10]` + `RunParameters[10]` +
`AttackHysteresisState[10]` + `TransitionHoldState` ≈ < 2 KB.

### 6.6 Version History

---

## SECTION 7 — FUTURE EXTENSIONS

### 7.1 Stage 1 — Runtime activation (KD-17)
Implementation lands once: (a) #8 ratifies ERR-015-002 amendment for the #15
read surface; (b) #12 reaches APPROVED; (c) #17 channel rows land via
ERR-015-003 / ERR-015-004.

### 7.2 Stage 1+ — `ATTACK_RUN_STARTED` / `OVERLOAD_DECLARED` channels
Atomic back-prop into #17 §3.10 via ERR-015-003 / ERR-015-004.

### 7.3 Stage 1+ — #14 emergency-flag consumer
When #14's `MarkDirective.emergencyFlag` is exposed by the orchestrator,
#15 may consume it to zero `TRANSITION_HOLD_TICKS` immediately on a goal-risk
turnover (KD-6 boundary hint; not implemented at Stage 0 per Interface Design
Principle).

### 7.4 Stage 1+ — xG-model integration
Replace the dangerous-zone surrogate (§5.7) with a proper xG model once
Shot Mechanics #6 Stage 1+ xG surface is available. The surrogate metric
continues in parallel for regression detection.

### 7.5 Stage 2+ — Set-piece attacking positioning
Corner and free-kick attacking runs (runners to the penalty spot, far-post
runners, decoy runs) are owned by #15 at Stage 2+ when the set-piece event
infrastructure (planned Stage 2+) is available.

### 7.6 Stage 2+ — Per-archetype attacking profiles
e.g., 4-3-3 vs. 4-2-3-1 default run-geometry presets based on team formation.
Formation-specific lane bias for the ATTACK_LEFT / ATTACK_RIGHT / ATTACK_CENTER
lane designations in #12.

### 7.7 Stage 2+ — Tactical instruction overlay
Per-match manager instructions wiring into style profile selection and
individual run instruction overrides. Requires coach-UI infrastructure
(Stage 2+).

### 7.8 Stage 2+ — ML-tuned `[GT]` parameter fitting
Run depth, timing, support radius, and style-profile multipliers as ML-fit
parameters tuned against a large simulated-match corpus.

### 7.9 Stage 5+ — Fixed64 migration per #9.
### 7.10 Stage 5+ — Cross-platform determinism per #9.

---

## SECTION 8 — REFERENCES AND CITATIONS

### 8.1 Cross-Spec References (grep-verified at section-file draft time)
- #1 §1.2, Appendix C (`PITCH_LENGTH_M`; `PITCH_WIDTH_M`; `HALF_LINE_X`;
  corner-origin coordinate system)
- #2 §2.5 (XC-002-001 — EntityId no-reuse); §3.1 (hysteresis pattern)
- #6 §7 (xG deferred to Stage 1+)
- #7 §3.7–§3.10 (perception schema; ball carrier resolution; attribute lookups)
- #8 §1.3.2 "Multi-agent coordination" deferral (names #12 / #13; #15 covered
  by implication — ERR-015-005 filed to add #15 explicitly at section-file
  draft); §1.7.3 (XC-008-001); §2.2.6 (amendment process — ERR-015-002);
  §3.1.7 (MOVE_TO_POSITION utility; #15 advises at Stage 1)
- #12 §4.5 (`RunIntent` writer-layer declaration; Stage 1+); §4.5.2
  (to-#15 boundary hint row); §2.2 (`laneAssignment` and `lineMembership`
  fields); FR-PA-048 (no interface against #15 at Stage 0)
- #13 §2.2 (FR-PR-014 role partition — confirms no RUNNER/SUPPORT_BALL
  overlap with PRIMARY_PRESS / COVER_SHADOW at Stage 1)
- #14 FR-DA-013 (all-ZONAL when IN_POSSESSION — mutual exclusion with #15);
  KD-8 (mutual-exclusion declaration); §7.4 (emergencyFlag boundary hint)
- #16 §3.2, §3.2.5, §3.4 (ERR-015-001 domain-tag allocation `0x1B`); §5; §6.2
- #17 §3.10 channel registry (Stage 1 back-prop via ERR-015-003 / ERR-015-004)
- #18 §3.7 (zero-alloc hot path); §6
- #19 §3, §4
- #20 §4.2 (FR-CS-025)

### 8.2 CLAUDE.md Invariants Bound
Corner-origin coordinate system; fatigue convention; tick rates; zero-alloc
hot path; constant-tag policy; Interface Design Principle (explains why no
#8/#12/#14 amendment text lives in this spec at Stage 0); Parameter-Based
Physics principle (no PatternType / RunType enum).

### 8.3 Typed Cross-Reference IDs
- `XC-015-NNN` — allocated at section-file draft for each upstream citation
  crossing a boundary.
- `ERR-015-001` — back-prop to #16 §3.4 to allocate
  `DOMAIN_TAG_ATTACKING_AI = 0x1B` within ERR-012-001 Phase B/C block.
  Filed at section-file draft.
- `ERR-015-002` — back-prop to #8 §2.2.6 / §3.1.7 to add a read of #15's
  `AttackIntent`. Mechanism chosen at section-file draft (accessor vs.
  `TacticalContext` extension). Filed at section-file draft.
- `ERR-015-003` — back-prop to #17 §3.10 to register `ATTACK_RUN_STARTED`
  channel. Filed at Stage 1.
- `ERR-015-004` — back-prop to #17 §3.10 to register `OVERLOAD_DECLARED`
  channel. Filed at Stage 1.
- `ERR-015-005` — back-prop to #8 §1.3.2 "Multi-agent coordination" paragraph
  to add "Attacking AI #15" alongside #12 and #13 as an explicitly named
  Stage 1+ deferral. Filed at section-file draft. Follows ERR-012-002
  (one-token patch precedent for #8 §3.1.7) and ERR-013-004 (one-token
  patch to #8 §3.1 correcting "Fatigue System #13" → "Pressing AI #13").
- Inherits `0x1B` slot from ERR-012-001 Phase B/C block
  (`0x17`=#12, `0x18`=#11, `0x19`=#13, `0x1A`=#14, `0x1B`=#15).

### 8.4 Academic / External References
No direct academic references required for the Stage-0 algorithm specification.
If published research on attacking movement coordination models or overload
geometry is cited in section files, DOIs must be verified before sign-off
(per ERR-005 fabricated-reference precedent from Heading #10 v0.1).

### 8.5 Version History

---

## SECTION 9 — APPROVAL CHECKLIST

### 9.1 Self-Contained Spec Content
- All 13 outline.md findings resolved (mapping in §9.4).
- All 36 FRs cross-referenced.
- All constants tagged (`[EST]` for outline-stage placeholders; promoted to
  `[GT]` at section-file authoring per CLAUDE.md).
- All cross-spec citations grep-verified at section-file draft time.
- Stage-binding statement (§1.8) makes Stage-0/Stage-1 split unambiguous.
- No PatternType / RunType enum in any spec text or code stub.
- No action-selection logic (PASS / SHOOT / DRIBBLE) in #15 scope.

### 9.2 Cross-Spec Sign-Offs Required
- #16 lead-developer approval of `DOMAIN_TAG_ATTACKING_AI = 0x1B` (via
  ERR-015-001 back-prop to #16 §3.4).
- #8 owner ack of read-surface mechanism (ERR-015-002 amendment text).
- #12 owner ack of `RunIntent` writer-layer contract per #12 §4.5.

### 9.3 KD-Sequencing Preconditions
- (a) `ERR-015-001` domain-tag `0x1B` ratified via #16 §3.4 patch.
- (b) `ERR-015-002` mechanism choice ratified by lead developer.
- (c) All `[CROSS-PENDING]` tags promoted to `[CROSS]`.
- (d) `ATTACK_DWELL_TICKS` `[EST]` → `[GT]` with Appendix A derivation.
- (e) #12 `RunIntent` writer-layer accessor name confirmed at section-file draft
  (grep `positioning-ai/section-4.md`).
- (f) ERR-015-005 back-prop amendment to #8 §1.3.2 ratified (adds #15 explicitly
  to the multi-agent-coordination deferral paragraph).
- (g) Lead-developer R-01..R-05 review pass.

### 9.4 Finding-to-Resolution Map

| Review | Finding | Sev | Resolved by |
|---|---|---|---|
| outline.md | 1. Missing metadata header | H | "Metadata Header" section |
| outline.md | 2. Section plan misaligned with CLAUDE.md template | H | §1–§9 mapping per template; Stage-Binding Clarification |
| outline.md | 3. §3 collides with Decision Tree #8 | H | KD-3: #15 does not own action selection; #15 owns off-ball movement; "Critical Boundary Decision" section |
| outline.md | 4. Pattern-template enum risk | H | KD-8: no PatternType / RunType enum; "Parameterized Movement" section; RunParameters four-field struct |
| outline.md | 5. xG acceptance criteria infeasible at Stage 0 | H | KD-10: Stage-0 surrogate (dangerous-zone shots + avg distance); §5.7; acceptance thresholds in Appendix D |
| outline.md | 6. Boundary with Defensive AI #14 §4 unstated | M | KD-6: mutual exclusion by phase; emergencyFlag Stage 1+ boundary hint; Boundary Matrix |
| outline.md | 7. Boundary with Positioning AI #12 unstated | M | KD-4: #12 owns baseline; #15 writes RunIntent writer-layer per #12 §4.5; Boundary Matrix |
| outline.md | 8. Determinism plan absent | M | KD-11: EntityId iteration; DOMAIN_TAG_ATTACKING_AI 0x1B; §4.6 digest scope; ERR-015-001 |
| outline.md | 9. Coordinate convention unmentioned | M | KD-16: full coordinate binding; "final third" and "weak side" formal definitions; §1.7 |
| outline.md | 10. Tick-rate split unstated | M | KD-2 + §1.7 (10 Hz tactical; 60 Hz physics) |
| outline.md | 11. Constant-tag policy not invoked | M | KD-14 + §6.1 full catalogue with tags |
| outline.md | 12. No event production declared | L | KD-15 (ATTACK_RUN_STARTED / OVERLOAD_DECLARED deferred Stage 1; ERR-015-003/004) |
| outline.md | 13. "Tactical identity" unmeasurable | L | KD-10 + §5.8 measurable criteria (runner-count delta; transition-hold-tick delta) |
| adversarial-review v1 | H-1. `relativeAngle_rad` redundancy | H | Removed from RunParameters struct; FR-AT-011 updated to 3 fields; "Parameterized Movement" section revised |
| adversarial-review v1 | H-2. `laneAssignment.lateralBias` non-existent field + lineMembership/laneAssignment conflation | H | §3.4 rewritten using `formationSlot.lateralPct`; §3.3 step 2a corrected to `lineMembership`; Q2 retained for grep confirmation |
| adversarial-review v1 | H-3. #8 §1.3.2 citation inaccurate | H | Metadata + §1.8 + §8.1 corrected to "by implication"; ERR-015-005 filed |
| adversarial-review v1 | M-4. `TOUCHLINE_HOLD_Y_M` naming ambiguity | M | Renamed to `TOUCHLINE_HOLD_DIST_M`; §3.6 formula rewritten with explicit per-side derivation |
| adversarial-review v1 | M-5. `rotate()` call undefined | M | §3.4 defines `teamAttackAngle`, coordinate-frame convention, and uses explicit depthVec+lateralVec decomposition |
| adversarial-review v1 | M-6. Transition SET/DECREMENT order inverted | M | §3.1 is now a gate only; §3.9 is the authoritative TransitionController with SET-then-DECREMENT ordering |
| adversarial-review v1 | L-7. `DANGER_ZONE_CORRIDOR_HW_M` derivation unexplained | L | Value updated to 10.16m; derived from PENALTY_BOX_HW_M/2; Appendix A derivation entry added |
| adversarial-review v1 | L-8. `overloadFlank` LEFT/RIGHT acceptability unresolved | L | KD-8 extended with scope-clarification note; `overloadFlank` confirmed acceptable (spatial discriminator, not movement-pattern enum) |
| adversarial-review v1 | L-9. §3.3 uses `laneAssignment` for lineMembership check | L | §3.3 step 2a corrected to `lineMembership` (same fix as H-2) |

### 9.5 Lead-Developer Sign-Off Lines (R-01..R-05)

### 9.6 Version History

---

## APPENDICES

### Appendix A — Derivations
Style-profile multiplier derivation and rationale (why DIRECT_DEPTH_MULT = 1.4,
why COUNTER_TIMING_MULT = 0.5). Hysteresis dwell-time derivation (binding to
#2 §3.1 pattern). `ATTACK_DWELL_TICKS [EST] → [GT]` promotion entry.
`FINAL_THIRD_X_M` derivation: `PITCH_LENGTH_M × 2/3 = 105 × 2/3 = 70.0m [DERIVED]`.
`DANGER_ZONE_CORRIDOR_HW_M` derivation: FIFA penalty area width = 40.32m
(distance from centre to each post = 20.16m). Half of penalty-area half-width
= 10.08m. Rounded up to `10.16m [GT]` to include the 6-yard-box width
(18.32m → 9.16m half-width) plus a goalkeeper diving-reach margin (~1m).
This is a `[GT]` constant — the exact value is gameplay-tunable; the
derivation establishes the design intent. If pitch dimensions change per #1 §1.2
update, this constant must be re-derived.

### Appendix B — RunParameters Verification Table
Canonical input/output table for the run parameter generator (§3.4). Four
scenarios: (1) POSSESSION profile, central lane; (2) DIRECT profile, right
lane (overlap geometry); (3) COUNTER_ATTACK profile, left lane (underlap
geometry); (4) depthOffset_m clamped at 40.0m. Each with full worked
calculation and final `runTargetPosition` coordinates.

### Appendix C — Width-Holding and Weak-Side Reference Card
Three canonical scenarios: (1) 9 agents, ball at center — correct width
allocation; (2) pool of 3 agents, WEAK_SIDE not assigned (below
MIN_WEAK_SIDE_AGENT_THRESHOLD); (3) agent at exactly SUPPORT_RADIUS_M
boundary — hysteresis preserves role. Worked tick-by-tick trace.

### Appendix D — Acceptance Criteria and Style-Profile Validation
Per KD-10:
- Dangerous-zone shot surrogate: measurement protocol, comparison baseline,
  stage-of-availability (Stage 1 first-runtime).
- DIRECT vs. POSSESSION tactical-identity test: `DIRECT_RUN_COUNT_DELTA`
  threshold justification.
- COUNTER_ATTACK transition-speed test: `COUNTER_MAX_HOLD_TICKS = 0`
  justification.

### Appendix E — Anti-Chaos Sensitivity Analysis
Per-invariant sensitivity sweeps for `MAX_RUNNERS`, `MIN_SUPPORT_AGENTS`,
and `OWN_HALF_RUN_BLOCK_M`. Shows that tightening MAX_RUNNERS to 1 reduces
overload frequency by ~X%; loosening to 5 risks defensive exposure on
counter (measured via §5.7 dangerous-zone concession surrogate).

### Appendix F — Glossary
Attacking role (RUNNER / SUPPORT_BALL / HOLD_WIDTH / WEAK_SIDE), attack
directive, attack intent, run parameters, off-ball movement pool, support
radius, overload zone, weak-side puller, width-holder, team-style profile
(POSSESSION / DIRECT / COUNTER_ATTACK), transition-hold, anti-chaos invariant,
assignment hysteresis, dangerous zone, final third (in coordinate terms), weak
side (formal Y-threshold definition). Gameplay vocabulary names for
parameterized patterns: "overlap" = RunParameters geometry, "underlap" =
RunParameters geometry, "third-man run" = RunParameters geometry, "cutback"
= Stage 2+ set-piece deferral.

### Appendix G — Telemetry & Troubleshooting Playbook
Stage 1+ debug overlays: per-agent role label over pitch map, overload zone
highlight, run-target position trail, weak-side corridor shading,
transition-hold countdown. Stage 0 placeholder only.

---

## OUTSTANDING OUTLINE-PHASE QUESTIONS

### Q1 — ERR-015-002 mechanism selection
Accessor (`AttackingAI.GetIntent(EntityId)`) vs. `TacticalContext.AttackIntent[]`
field extension via #8 §2.2.6 amendment. Same decision pattern as KD-3 in
#13 outline (resolved as Option B `PressDirective?` nullable field in #13 v0.3)
and KD-5 in #14 (resolved as Option B `TacticalContext.MarkDirective?`). The
precedent strongly favors the `TacticalContext` extension pattern. Decision
deferred to section-file draft; tracked as ERR-015-002.

### Q2 — #12 `laneAssignment` accessor name
`laneAssignment` field appears in `positioning-ai/section-2.md` §2.2 struct
table. Exact accessor or field name for Stage 1+ read by #15 must be
grep-confirmed at section-file draft against the `positioning-ai/section-4.md`
published interface surface.

### Q3 — Style-profile constant-loading mechanism
How are profile multipliers loaded at match initialisation? Options: (a) a
per-team constant struct loaded from a `TeamTacticsProfile` asset (Stage 1+
coach-UI concern); (b) a `[GT]` named constant set selected by an index
in `TacticalContext`. Decision deferred to section-file draft. No enum in
the physics/AI layer (KD-8 / KD-12).

### Q4 — GK expected zone source
`AttackingPoolBuilder.cs` needs to identify the GK EntityId. Source: #7
Perception snapshot should tag the GK. Confirm that #7 §3.7 exposes a
`isGoalkeeper: bool` field or equivalent tag in the perception snapshot.
Grep `perception-system/section-3.md` at section-file draft.

### Q5 — Domain-tag number confirmation
`DOMAIN_TAG_ATTACKING_AI = 0x1B` is proposed per ERR-012-001 Phase B/C block.
If #11, #12, or #14 reach APPROVED before #15's section-file draft and claim
a different slot, the block may shift. Verify against
`deterministic-sim/section-3.md` at section-file draft; update ERR-015-001
accordingly.

---

## NEXT STEPS

1. (Optional) Self-adversarial pass against this v1.0 outline.
2. Draft `section-1.md`.
3. Draft `section-2.md` (FR table — already enumerated above; 36 FRs).
4. Draft `section-3.md` (algorithms §3.1–§3.15).
5. Draft `section-4.md`.
6. Draft `section-5.md`.
7. Draft `section-6.md` (promote `[EST]` → `[GT]` with derivations in
   Appendix A; `ATTACK_DWELL_TICKS` is the sole `[EST]` constant).
8. Draft `section-7.md`.
9. Draft `section-8.md` (grep-verify every citation).
10. Draft `section-9-approval-checklist.md`.
11. Draft `appendices.md`.
12. Adversarial review PASS-1 on section files.
13. v0.2 fix pass.
14. Flip `SPEC_INDEX.md` row 15 `NOT STARTED → IN REVIEW`.
15. Lead-developer R-01..R-05 sign-off.

---

## VERSION HISTORY

| Version | Date | Author | Summary |
|---|---|---|---|
| 1.1 | May 17, 2026 | AI agent | v1.1 resolves all 9 adversarial-review findings (3H/3M/3L) from `adversarial-review-outline-detailed-v1.md`. H-1: `relativeAngle_rad` removed from `RunParameters` (now 3 fields); FR-AT-011 updated. H-2: `laneAssignment.lateralBias` replaced by `formationSlot.lateralPct`-based formula; §3.3 corrected to use `lineMembership` for RUNNER eligibility. H-3: #8 §1.3.2 citation corrected; ERR-015-005 filed as back-prop. M-4: `TOUCHLINE_HOLD_Y_M` renamed `TOUCHLINE_HOLD_DIST_M`; §3.6 explicit per-side formula. M-5: §3.4 defines `teamAttackAngle` (team attack direction constant), replaces `rotate()` with explicit `depthVec + lateralVec` decomposition. M-6: §3.1 is now a gate only; §3.9 is the authoritative TransitionController with SET-then-DECREMENT ordering. L-7: `DANGER_ZONE_CORRIDOR_HW_M` updated to 10.16m with Appendix A derivation from penalty-area dimensions. L-8: KD-8 clarified — scope is movement-pattern enums only; `overloadFlank` confirmed acceptable. L-9: §3.3 step 2a corrected (covered by H-2 fix). §2.3 Inputs expanded: `teamAttackAngle` added as explicit input (match-half constant; source = match config); `formationSlot.lineMembership` and `formationSlot.lateralPct` fields called out separately; `laneAssignment` role clarified (width-holding §3.6 only). §5.2 unit-test description corrected from "MIDFIELD_ATTACK lanes" to "`lineMembership` ATTACK or MIDFIELD". Q6 resolved; now 5 outstanding questions (Q1–Q5). |
| 1.0 | May 17, 2026 | AI agent | Initial detailed outline. Resolves all 13 findings from the May 6, 2026 adversarial review at the bottom of `outline.md` (5 H / 6 M / 2 L). Establishes 17 cross-cutting design decisions (KD-1..KD-17). Stage-binding clarification anchors #15 runtime to Stage 1 — Stage 0 deliverable is spec text only. 36 FRs enumerated. 4 attacking roles (RUNNER / SUPPORT_BALL / HOLD_WIDTH / WEAK_SIDE). KD-3 boundary with #8 (no action selection by #15). KD-4 boundary with #12 (RunIntent writer-layer). KD-6 boundary with #14 (mutual-exclusion by phase). KD-8 no PatternType / RunType enum — fully parameterized RunParameters (3 fields; angle derived). KD-10 Stage-0-feasible chance-quality surrogate. KD-12 three team-style profiles as constant-multiplier sets (no style enum). KD-13 three anti-chaos invariants. ERR-015-001..005 back-prop requests pre-filed. Domain-tag `DOMAIN_TAG_ATTACKING_AI = 0x1B [CROSS-PENDING]`. 5 Outstanding Outline-Phase Questions flagged. |
