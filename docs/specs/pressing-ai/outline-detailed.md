# Pressing AI Specification #13 — Detailed Outline

**Created:** May 16, 2026
**Last Updated:** May 16, 2026
**Version:** 1.0
**Status:** DRAFT — expansion of `outline.md` into section-by-section
plan. Resolves all 13 findings (5 H / 6 M / 2 L) from the May 6, 2026
adversarial review at the bottom of `outline.md`.
**Companion documents:** `outline.md` (high-level + first review).
**Unblocks:** section-file authoring (`section-1.md` … `appendices.md`).

---

## PURPOSE OF THIS DOCUMENT

Expansion of `outline.md` into a section-by-section subsection plan
that resolves every adversarial-review finding. For each subsection:
the FRs it will publish, the boundary declarations it will hold, and
the cross-references it will emit. Detailed enough that
`section-1.md` … `section-9-approval-checklist.md` and `appendices.md`
can be drafted mechanically.

This document does **not** publish FR text in normative form — that
text lands in `section-2.md`. The detailed outline records every FR's
intended rule, conformance level, and source spec/section so the FR
table can be authored without re-deriving rules.

---

## METADATA HEADER (resolves outline.md H-1)

| Field | Value |
|-------|-------|
| Spec # | 13 |
| Title | Pressing AI |
| Folder | `docs/specs/pressing-ai/` |
| Priority | 4 (Phase C tactical-AI chain — depends on #12) |
| Status | NOT STARTED (outline phase) |
| Owner | Lead developer (gameplay-AI domain) |
| Approved Dependencies | #1 Ball Physics; #2 Agent Movement; #4 First Touch; #5 Pass Mechanics (re-approved May 6, 2026); #7 Perception; #8 Decision Tree; #16 Deterministic Simulation; #17 Event System; #18 Performance; #19 Testing; #20 Code Standards |
| Pending Dependencies | #12 Positioning AI (IN REVIEW) — consumed as baseline shape source; ERR-013-001 (domain-tag allocation in #16 §3.4) opens at section-file draft |
| Downstream Consumers | #14 Defensive AI; #15 Attacking AI (Phase C linear chain) |
| Stage Binding | **Spec drafted at Stage 0. Runtime activation is Stage 1** per #8 §1.4.21 + §1.3.2 ("Stage 1 — Pressing AI #13 introduces coordinated press triggers") and #8 §1.5 row "Pressing AI #13 (Stage 1)". Stage 0 deliverable from #13 = published interface schema + algorithm specification only; no runtime code emitted at Stage 0. |
| Estimated Effort | 5–7 working days |

---

## STAGE-BINDING CLARIFICATION (resolves outline.md H-4)

This is the **most important** decision in this outline and deserves
its own subsection because it dissolves the H-4 finding entirely.

**Finding H-4 (outline.md May 6 review):** "Pass Mechanics #5
SUSPENDED-status risk unacknowledged. Backward-pass detection is a
primary trigger; the spec it depends on is currently suspended..."

**Resolution path 1 (status update):** #5 was re-approved May 6, 2026
(see CLAUDE.md OPEN ISSUES, "Pass Mechanics (#5) — RESOLVED"). The
SUSPENDED status no longer applies. The finding is half-stale.

**Resolution path 2 (stage binding):** Even with #5 APPROVED, #13's
**runtime** is Stage 1 per #8 §1.4.21. That is the canonical anchor:
#8 itself declares "No coordinated pressing... Stage 1 — Pressing AI
#13 introduces coordinated press triggers." Therefore:

- #13 spec is **authored** in Stage 0 alongside the other 19 specs.
- #13 runtime **activates** at Stage 1 when (a) #8 publishes the
  back-prop amendment ratifying its read of #13 outputs, and (b) the
  `TacticalContext` schema either grows a `PressDirective` field (per
  #8 §2.2.6 amendment process) or #13 publishes a read-only accessor
  on its own subsystem surface that #8's PRESS utility consumes.
- At Stage 0, #8's existing **uncoordinated** PRESS utility (§3.1.8)
  remains the sole press behavior. #13 ships as inert specification.

This is exactly the pattern #12 §7.3 declared for the #13 binding
slot ("`PressOverride` displacement layer that mutates
`TacticalContext.FormationSlot[]` BEFORE #8 reads it... declared here
only as a boundary hint; #12 does not implement the override at
Stage 0").

**Why authoring the spec at Stage 0 still has value:** it (a) names
the cross-spec contracts so #14 / #15 specs (also Phase C, also
NOT STARTED) can refer to a stable surface, (b) lets the Stage 1
implementation start from approved text instead of from scratch, (c)
exposes the back-prop amendments #13 will need from #8 (filed as
ERR-013-NNN well in advance), and (d) preserves the project's
"write all 20 specs before any code" discipline.

---

## CROSS-CUTTING DESIGN DECISIONS

### KD-1 — Cite-not-redefine
Spec #13 never restates a CLAUDE.md invariant or a rule from another
approved spec. Cited: corner-origin coordinates (#1 §1.2); fatigue
`0 = rested, 1 = fatigued` (CLAUDE.md); 10 Hz tactical / 60 Hz
physics (CLAUDE.md); EntityId no-reuse (#2 §2.5 XC-002-001 + #8
§1.7.3 XC-008-001); perception schema (#7 §3.7–§3.10); decision-tree
PRESS action schema (#8 §3.1.8, §3.2.7); First Touch quality output
(#4 §3.5 pressure scalar); Pass Mechanics directional-event surface
(#5 §3.x — exact subsection grep-verified at section-file draft).

### KD-2 — 10 Hz tactical, no 60 Hz work
Pressing AI runs on the 10 Hz tactical loop. Output is a per-team
`PressDirective` (one struct per team per tick) and a per-agent
`PressAssignment` (one struct per agent per tick). No 60 Hz steering
work — physical pursuit is owned by #2 via #8's resolved
`Action.TargetPosition` (same path as #12's slot output, just biased
by the PRESS assignment).

### KD-3 — Boundary with Decision Tree #8 (Stage-1 runtime binding)

**Verified facts** (grep against `decision-tree/section-3-1.md`
§3.1.8 and `section-1.md` §1.4.21 + §1.5):
- #8 §3.1.8 evaluates PRESS independently per agent at Stage 0 (gates
  on stamina ≥ `PRESS_STAMINA_MINIMUM = 0.20 [GT]` and target within
  `PRESS_TRIGGER_DISTANCE = 8.0m [GT]`).
- #8 §1.4.21 explicitly defers coordinated pressing to "Stage 1 —
  Pressing AI #13".
- #8 §1.5 row labels Pressing AI #13 as "Stage 1" and the
  interaction is "Coordinated press state — DT will consult before
  scoring PRESS".

**Therefore #13's Stage-1 role is to PUBLISH a coordinated
`PressAssignment` per agent that #8 §3.1.8 consults during PRESS
target selection.** It is an upstream advisor to #8, not a competitor.

**Boundary at Stage 1 (anticipated, normative for the spec text):**
- **#13 owns:** per-team trigger evaluation, role assignment
  (primary presser / cover-shadow / hold-shape), cover-shadow lane
  computation, trap timing, disengage/reset logic, anti-chaos
  invariants.
- **#8 owns:** the per-agent PRESS utility scoring and the final
  PRESS-vs-MOVE_TO_POSITION-vs-INTERCEPT decision. #8 reads #13's
  `PressAssignment` as a bias / target lock; #8 may still decline to
  PRESS if its utility math says so (e.g., stamina too low).
- **#13 → #8 coupling:** one direction, read-only. Mechanism is
  either (a) a read-only accessor `PressingAI.GetAssignment(EntityId)`
  invoked from #8 §3.1.8.2 target selection, OR (b) a new
  `TacticalContext.PressDirective` field added via #8 §2.2.6
  amendment (requires the amendment to be drafted as part of this
  spec; tracked as ERR-013-001 against #8 §2.2.6 / §3.1.8.2).
  **The outline does not pre-pick the mechanism — section-file draft
  will choose** based on whether the orchestrator-tier wiring is
  cleaner with a direct accessor or a TacticalContext extension.
- **#8 → #13 coupling:** none at Stage 1 either. #13 does not read
  per-agent #8 action selections.
- **Stage 0:** no coupling at all. #13 ships as inert specification.

### KD-4 — Boundary with Positioning AI #12
#13's output **biases** but **does not replace** #12's
`formationSlot`. The flow at Stage 1:

1. #12 publishes per-agent baseline `formationSlot` (out-of-possession
   shape).
2. #13 publishes per-agent `PressAssignment`.
3. Orchestrator composes: agents with `Role = PRIMARY_PRESS` or
   `Role = COVER_SHADOW` get a #13-derived target position that
   overrides their #12 slot for that tick; all other agents keep #12's
   slot.
4. #8 reads the composed slot via `TacticalContext.FormationSlot` and
   the new `PressDirective` (or accessor) per KD-3.

This is the "`PressOverride` displacement layer" that #12 §7.3
reserves. #13 owns the override; #12 owns the baseline.

### KD-5 — Boundary with Defensive AI #14
At Stage 0, #14 is NOT STARTED. At Stage 1+, #14 owns zonal / man
assignments for the non-pressing agents (those whose #13 role is
`HOLD_SHAPE`). #14's outline declares a §4 "handoff rules between
defensive and pressing systems" — this spec mirrors that handoff:
**#13 owns the pressing agents; #14 owns the cover/zonal agents; the
roles are mutually exclusive per agent per tick.** Boundary specified
here as a hint, not implemented.

### KD-6 — Boundary with Attacking AI #15
At Stage 0, #15 is NOT STARTED. #13 is out-of-possession behavior;
#15 is in-possession behavior. The two are mutually exclusive at the
team level (gated by possession phase). No further coupling at Stage 1
beyond phase enumeration agreement (KD-10).

### KD-7 — Trigger catalog (resolves outline.md H-3)
Four canonical triggers, each cited to an authoritative upstream:

| Trigger | Source | Specific surface |
|---|---|---|
| `BAD_TOUCH` | First Touch #4 §3.5 | First-touch quality scalar exceeding `BAD_TOUCH_THRESHOLD [GT]` and ball-velocity escape > `BAD_TOUCH_VELOCITY_M_S [GT]` |
| `BACKWARD_PASS` | Pass Mechanics #5 (subsection grep-verified at section-file draft) | Pass directional event where `dot(passVelocity, attackingDirection) < BACKWARD_PASS_THRESHOLD [GT]` |
| `SIDELINE_TRAP` | Ball Physics #1 §1.2 (corner-origin geometry) + #7 Perception (ball position) | Ball within `SIDELINE_TRAP_DISTANCE_M [GT]` of either touchline AND ball-carrier facing toward sideline |
| `WEAK_RECEIVER` | Perception #7 §3.7–§3.10 (visibility/attribute lookup) | Receiver's `FirstTouch` attribute below `WEAK_RECEIVER_THRESHOLD [GT]` AND receiver's perceived pressure ≥ `WEAK_RECEIVER_PRESSURE [GT]` |

Triggers fire on the tick AFTER the originating event is visible in
the #7 perception snapshot (one-tick latency by design — perception
filtering already enforces this for opponent-side events).

All four trigger constants are `[GT]` (designer-tuned). The triggers
themselves are not magic numbers — they are derivations from upstream
surfaces. The thresholds are tunable.

### KD-8 — Coordinated role catalog (resolves outline.md H-4)

Three roles, per-agent, per tick:

| Role | Count per team | Behavior |
|---|---|---|
| `PRIMARY_PRESS` | 0 or 1 | Closes ball-carrier directly. Target = ball-carrier `EntityId`. |
| `COVER_SHADOW` | 0..`MAX_COVER_SHADOWS [GT]` (Stage 1 default: 2) | Closes a passing lane between ball-carrier and a candidate receiver. Target = (receiver `EntityId`, shadow-lane `Vector2`). |
| `HOLD_SHAPE` | All others | Defaults to #12's `formationSlot`. No override applied. |

Role assignment is computed by the algorithm in §3.4. Role transitions
use dwell-time hysteresis (KD-9). The disjoint partition is enforced
as an invariant (FR-PR-014).

### KD-9 — Hysteresis pattern reuse (#2 §3.1 binding)
Trigger debounce, role transitions, and disengage decisions use the
dwell-time + dead-zone hysteresis pattern from #2 §3.1. #13 does NOT
define a new algorithm — it parameterises the #2 pattern. All
hysteresis constants are `[EST]` at outline stage; promotion to `[GT]`
happens during section-file authoring when each value gains a
worked-example justification (Appendix A).

### KD-10 — Determinism binding (#16)
All `PressDirective` writes, `PressAssignment` writes, and the
internal trigger debounce + role hysteresis state are authoritative
simulation state per #16 §3.2 and appear in the per-tick digest at
the scope #16 §6.2 defines for tactical-AI outputs. Agent iteration
uses the canonical EntityId sort from #16 §3.2.5. Trigger evaluation
order across agents on the same tick uses EntityId-ascending. Any
stochastic micro-jitter (e.g., tie-breaking when two cover-shadow
candidates have equal cost) uses `DeterministicRngService` with
domain tag `DOMAIN_TAG_PRESSING_AI` — value `[CROSS-PENDING]` until
lead-developer ratifies the Phase B/C block per ERR-012-001 (proposed
`0x19` in the #12 outline's KD-9 table; this spec inherits that
proposal and files ERR-013-001 as the back-prop request if the block
has not yet been allocated when section-file draft begins).

### KD-11 — Event System binding (#17)
At Stage 1 runtime, #13 emits two event channels for telemetry and
#14 handoff:
- `PRESS_TRIGGERED` — fired when a `PressDirective` is published with
  non-empty role assignments.
- `PRESS_DISENGAGED` — fired when a `PressDirective` returns to
  all-`HOLD_SHAPE` after a non-trivial press.

Both channels require atomic back-prop into #17 §3.10 channel
registry — filed as ERR-013-002 / ERR-013-003 at section-file draft.
At Stage 0, no channels are produced or consumed (consistent with
runtime activation deferral).

**Phase enumeration** (in-possession / out-of-possession /
transition) is consumed from #12 §3.0 (per #12's local-phase
computation). #13 does NOT compute phase independently; it reads
#12's phase output. If #12's phase is `IN_POSSESSION` for the team
that owns this `PressDirective`, the directive is all-`HOLD_SHAPE`
(no press from a team in possession).

### KD-12 — Stage-0/Stage-1 scope discipline
Out of Stage 0 scope (deferred to §7): runtime code, #8 back-prop
amendment, #17 channel registration, #14 handoff implementation,
authoring tools, coach UI, save-game persistence, ML-tuned `[GT]`
parameter fitting, set-piece pressing, custom press-style editor,
goalkeeper-as-pivot specialized handling (KD-13).

### KD-13 — Goalkeeper handling (binding to #11)
The goalkeeper is **never** assigned `PRIMARY_PRESS` or
`COVER_SHADOW`. The GK is always `HOLD_SHAPE` from #13's perspective
and its slot is owned by #11 Goalkeeper Mechanics (IN REVIEW). #13's
trigger #4 (`WEAK_RECEIVER`) does not consider the GK as a candidate
receiver — i.e., a pass back to the GK is handled by the
`BACKWARD_PASS` trigger only, with the GK explicitly excluded from
"weak-receiver" classification.

### KD-14 — Constant-tag discipline
Every constant carries exactly one of `[GT]`, `[EST]`, `[FIXED]`,
`[DERIVED]`, `[CROSS]`, `[CROSS-PENDING]`. All trigger thresholds
and hysteresis values start `[EST]` at outline stage; promotion to
`[GT]` happens at section-file authoring per CLAUDE.md.

### KD-15 — Single constant catalogue per #20 §4.2 (FR-CS-025)
ALL constants live in one file `PressingAIConstants.cs`, organised
into `#region` blocks per #20 §4.2.

### KD-16 — Anti-chaos invariants (resolves outline.md M-3)
Three measurable invariants enforced as FRs:
- **Max pressers per ball-side third:** `MAX_PRESSERS_BALL_THIRD [GT]`
  (Stage 1 default: 3). Counted as `PRIMARY_PRESS` +
  `COVER_SHADOW`.
- **Min back-line agents:** `MIN_BACKLINE_AGENTS [GT]` (Stage 1
  default: 3). Agents in defensive third whose #12 line membership
  is `DEFENSE` cannot be promoted to `PRIMARY_PRESS` if doing so
  drops the count below the floor.
- **Max distance-from-#12-anchor:** `MAX_PRESS_DISPLACEMENT_M [GT]`
  (Stage 1 default: 25m). A `COVER_SHADOW` assignment that would
  move an agent further than this from its #12 baseline anchor is
  rejected and the agent stays `HOLD_SHAPE`.

These invariants are enforced *before* the directive is published,
so #8 never sees a chaos-violating directive.

### KD-17 — Exploit-resistance test corpus (resolves outline.md M-4)
Section 5 (test plan) MUST include integration tests for the
canonical pressing-AI exploit set:
- `EXPLOIT_LONG_BALL_OVER_PRESSERS` — long ball over a high press
  must not collapse the entire defensive line.
- `EXPLOIT_SWITCH_OF_PLAY` — switch to weak-side isolated zone must
  trigger disengage and reset within `RESET_LATENCY_TICKS [GT]`.
- `EXPLOIT_ONE_TWO_BOUNCE` — drag-and-bounce one-twos through the
  press must not deterministically beat it (i.e., must still leave
  a defender behind the bounce).
- `EXPLOIT_GK_PIVOT` — backward pass to GK must trigger press but
  not commit beyond the halfway line (see KD-13).

Coverage is mandatory; tests live in §5.6. This forecloses
ERR-005-style fabricated verification.

---

## BOUNDARY MATRIX (resolves outline.md H-2 / H-3 / M-2)

| Boundary | #13 owns | Other owns | Direction | Mechanism | Stage 0? |
|---|---|---|---|---|---|
| #8 Decision Tree | `PressDirective` per team + per-agent `PressAssignment` | Per-agent action loop including PRESS utility scoring | #8 reads #13 (at Stage 1) | Accessor or `TacticalContext.PressDirective` extension; selected at section-file draft | No (Stage 1 runtime) |
| #12 Positioning AI | `PressOverride` displacement consumed by orchestrator | Baseline out-of-poss `formationSlot` | Orchestrator composes; both read by #8 | Per-agent slot override pre-#8 read | No (Stage 1) |
| #2 Agent Movement | (none direct — via #8 action output) | 60 Hz steering | #2 reads #8 | Same path as #12 | No |
| #4 First Touch | (none — read consumer) | First-touch quality / pressure scalar | #13 reads #4 | Perception snapshot field at tick start | Yes (schema only) |
| #5 Pass Mechanics | (none — read consumer) | Pass directional event | #13 reads #5 | Per-tick event (subsection grep at section-file draft) | Yes (schema only) |
| #7 Perception | (none — read consumer) | Filtered world model | #13 reads #7 | Snapshot read at tick start | Yes |
| #11 Goalkeeper | (KD-13: GK excluded from press roles) | GK slot ownership | independent | KD-13 invariant | n/a |
| #14 Defensive | Pressing-role-owned agents | Cover/zonal-role-owned agents | Disjoint partition per tick | KD-5 handoff rule | No (Stage 1+) |
| #15 Attacking | (mutually exclusive by possession phase) | In-possession behavior | independent | KD-6 phase gating | No |
| #16 Determinism | `PressDirective` / `PressAssignment` / debounce + hysteresis state digest scope | Digest format + iteration rule | #13 conforms | EntityId iteration + domain-tagged RNG (`[CROSS-PENDING]`) | Yes (spec text) |
| #17 Event System | `PRESS_TRIGGERED` / `PRESS_DISENGAGED` channel definitions | Channel registry | (deferred) | ERR-013-002 / ERR-013-003 at Stage 1 | No (Stage 1) |
| #18 Performance | (conformance only) | Per-tick budget framework | #13 conforms | §6 budget against named host (KD-15 in #12) | Yes (spec text) |
| #19 Testing | (conformance only) | Test-framework conventions | #13 conforms | §5 plan | Yes (spec text) |
| #20 Code Standards | (conformance only) | File / catalogue / naming rules | #13 conforms | `PressingAIConstants.cs` per FR-CS-025 | Yes (spec text) |

---

## SECTION 1 — INTRODUCTION, SCOPE, DEPENDENCIES, KEY DECISIONS

### 1.1 Purpose
One-paragraph problem statement: #13 specifies coordinated pressing
behavior — when a team out of possession deliberately commits 1–3
agents to closing the ball-carrier and shadowing nearby passing lanes.
The spec exists to (a) name the trigger catalog, (b) define role
assignment, (c) enforce anti-chaos invariants, (d) provide reset/
disengage logic, and (e) declare the integration surface with #8
Decision Tree (where the runtime activation lands at Stage 1).

### 1.2 Scope (in / out)
- **In:** trigger detection from upstream events, per-team directive
  computation, per-agent role assignment, cover-shadow lane geometry,
  trap timing, stamina/discipline costs, disengage/reset logic,
  anti-chaos invariants (KD-16), exploit-resistance test corpus
  (KD-17), parameter catalogue.
- **Out (per KD-12):** runtime code, #8 back-prop amendment text
  (filed as ERR-013-001 separately), authoring tools, coach UI,
  set-piece pressing, ML tuning, save-game.

### 1.3 Dependencies
Upstream APPROVED: #1, #2, #4, #5, #7, #8, #16, #17, #18, #19, #20.
Upstream IN REVIEW: #11, #12. (Section-file draft re-verifies status
via `SPEC_INDEX.md` grep before submitting for sign-off.)

### 1.4 Key Domain Concepts
Trigger, directive, assignment, primary press, cover shadow, cover-
shadow lane, hold shape, trap, disengage, reset latency, anti-chaos
invariants.

### 1.5 Key Design Decisions
Cross-reference KD-1..KD-17.

### 1.6 Interface Boundaries
The Boundary Matrix above.

### 1.7 Coordinate & Convention Bindings
Corner origin (#1 §1.2); fatigue `0 = rested, 1 = fatigued`; 10 Hz
tactical; EntityId no-reuse (#2 §2.5 / #8 §1.7.3); ball geometry from
#1.

### 1.8 Stage-Binding Statement
**Spec drafted at Stage 0; runtime activates at Stage 1.** Authoritative
basis: #8 §1.4.21 + §1.5. Stage 0 deliverable from #13 = published
specification (this document and the section files). No runtime code
at Stage 0.

### 1.9 Version History

---

## SECTION 2 — FUNCTIONAL REQUIREMENTS, DATA STRUCTURES, FAILURE MODES

### 2.1 Functional Requirements Table (provisional 44-entry enumeration)

Conformance: `MUST` unless noted. Citations point to KDs or source
sections.

| FR | Subject | Conf. | Source |
|---|---|---|---|
| FR-PR-001 | Tactical tick rate is 10 Hz | MUST | CLAUDE.md / KD-2 |
| FR-PR-002 | Output is one `PressDirective` per team per tick + one `PressAssignment` per agent per tick | MUST | KD-2 / KD-3 |
| FR-PR-003 | Agent iteration order is EntityId-sorted ascending | MUST | #16 §3.2.5 / KD-10 |
| FR-PR-004 | `PressDirective` and `PressAssignment` contribute to per-tick digest | MUST | #16 §6.2 / KD-10 |
| FR-PR-005 | RNG calls use `DOMAIN_TAG_PRESSING_AI` (`[CROSS-PENDING]` until ERR-013-001 ratified) | MUST | #16 §3.4 / KD-10 |
| FR-PR-006 | No allocation on hot path | MUST | #18 §3.7 |
| FR-PR-007 | Single constant catalogue `PressingAIConstants.cs` per #20 §4.2 | MUST | #20 FR-CS-025 / KD-15 |
| FR-PR-008 | Fatigue input convention `0 = rested` | MUST | CLAUDE.md / KD-1 |
| FR-PR-009 | Trigger `BAD_TOUCH` fires when #4 first-touch quality and ball-velocity escape exceed thresholds | MUST | KD-7 / §3.1 |
| FR-PR-010 | Trigger `BACKWARD_PASS` fires when #5 pass directional event indicates dot(pass, attack-dir) < threshold | MUST | KD-7 / §3.1 |
| FR-PR-011 | Trigger `SIDELINE_TRAP` fires when ball is within sideline distance AND carrier faces toward sideline | MUST | KD-7 / §3.1 |
| FR-PR-012 | Trigger `WEAK_RECEIVER` fires when perceived receiver's FirstTouch attribute is below threshold AND perceived pressure exceeds threshold | MUST | KD-7 / §3.1 |
| FR-PR-013 | Triggers debounce via dwell-time hysteresis | MUST | KD-9 / §3.2 |
| FR-PR-014 | Roles are a disjoint partition per agent per tick (PRIMARY_PRESS ⊕ COVER_SHADOW ⊕ HOLD_SHAPE) | MUST | KD-8 |
| FR-PR-015 | At most one `PRIMARY_PRESS` per team per tick | MUST | KD-8 |
| FR-PR-016 | At most `MAX_COVER_SHADOWS [GT]` (default 2) cover-shadow assignments per team per tick | MUST | KD-8 |
| FR-PR-017 | Goalkeeper is always `HOLD_SHAPE` from #13's perspective | MUST | KD-13 |
| FR-PR-018 | Anti-chaos: max pressers per ball-side third = `MAX_PRESSERS_BALL_THIRD [GT]` | MUST | KD-16 |
| FR-PR-019 | Anti-chaos: min backline agents = `MIN_BACKLINE_AGENTS [GT]` | MUST | KD-16 |
| FR-PR-020 | Anti-chaos: max press displacement from #12 anchor = `MAX_PRESS_DISPLACEMENT_M [GT]` | MUST | KD-16 |
| FR-PR-021 | Anti-chaos invariants are checked BEFORE directive is published | MUST | KD-16 |
| FR-PR-022 | Primary-press target is the ball-carrier `EntityId` | MUST | §3.3 |
| FR-PR-023 | Cover-shadow target is the highest-cost candidate-receiver `EntityId` not pressed by the primary | MUST | §3.4 |
| FR-PR-024 | Cover-shadow lane position lies on the geometric segment between ball-carrier and target receiver at offset `COVER_SHADOW_LANE_FRACTION [GT]` | MUST | §3.5 |
| FR-PR-025 | Role assignment uses cost-based selection (smallest required displacement wins) with EntityId terminal tie-break | MUST | §3.4 / KD-9 |
| FR-PR-026 | Role transitions use dwell-time hysteresis `ROLE_DWELL_TICKS [EST]` | MUST | KD-9 / §3.6 |
| FR-PR-027 | Stamina cost: `PRIMARY_PRESS` adds `STAMINA_COST_PRIMARY_PER_TICK [GT]` to the assigned agent's fatigue accumulator | MUST | §3.7 |
| FR-PR-028 | Stamina cost: `COVER_SHADOW` adds `STAMINA_COST_SHADOW_PER_TICK [GT]` | MUST | §3.7 |
| FR-PR-029 | Stamina gate: an agent is excluded from press roles if its fatigue ≥ `PRESS_FATIGUE_CEILING [GT]` (cite-not-redefine #8 §3.1.8.1) | MUST | §3.7 / KD-1 |
| FR-PR-030 | Disengage: directive returns to all-`HOLD_SHAPE` if no trigger has fired for `DISENGAGE_TIMEOUT_TICKS [GT]` | MUST | §3.8 |
| FR-PR-031 | Disengage: directive returns to all-`HOLD_SHAPE` immediately if ball leaves the press-eligible zone (cited as `PRESS_ELIGIBLE_ZONE` polygon) | MUST | §3.8 |
| FR-PR-032 | Reset: after disengage, no new press fires for `RESET_LATENCY_TICKS [GT]` | MUST | §3.8 |
| FR-PR-033 | Phase gating: directive is all-`HOLD_SHAPE` if #12 phase is `IN_POSSESSION` for this team | MUST | KD-11 |
| FR-PR-034 | All formulas have a worked example in §3 or Appendix E | MUST | CLAUDE.md |
| FR-PR-035 | Failure mode F1: stale perception → freeze previous tick directive | MUST | §2.4 |
| FR-PR-036 | Failure mode F2: invalid trigger source (NaN quality scalar) → suppress trigger | MUST | §2.4 |
| FR-PR-037 | Failure mode F3: mid-tick possession change → defer trigger evaluation to next tick boundary | MUST | §2.4 |
| FR-PR-038 | Failure mode F4: cover-shadow candidate set empty → demote that slot to `HOLD_SHAPE` | MUST | §2.4 |
| FR-PR-039 | Failure mode F5: invariant violation detected at publication time → fall back to all-`HOLD_SHAPE` for this tick | MUST | §2.4 / KD-16 |
| FR-PR-040 | Failure mode F6: #12 baseline slot unavailable (e.g., #12 disabled) → no overrides emitted | MUST | §2.4 |
| FR-PR-041 | All tunable constants tagged per KD-14 | MUST | KD-14 |
| FR-PR-042 | No interface produced against unspecified consumer specs (#14/#15 at Stage 0/1) | MUST | CLAUDE.md / KD-5/KD-6 |
| FR-PR-043 | Stage-0 deliverable is spec text only; no runtime code | MUST | KD-12 / §1.8 |
| FR-PR-044 | Stage-1 activation gated on (a) #8 ERR-013-001 amendment ratified, (b) #12 APPROVED, (c) ERR-013-002/003 #17 channel rows landed | MUST | KD-12 / §7 |

### 2.2 Data Structures

| Struct | Purpose | Stage |
|---|---|---|
| `PressDirective` | Per-team per-tick: team `EntityId`, primary-press agent `EntityId?`, cover-shadow agent list, disengage flag, reset-cooldown counter | 1 (spec'd at 0) |
| `PressAssignment` | Per-agent per-tick: role enum, target `EntityId?`, target position `Vector2?`, valid-through-tick | 1 (spec'd at 0) |
| `PressTrigger` | Per-team per-tick: trigger-flags bitmask + per-flag dwell counter | 1 |
| `RoleHysteresisState` | Per-agent: dwell counters for role transitions (digested per KD-10) | 1 |
| `PressOverride` view | Read-only view exposed to orchestrator for #12 slot composition | 1 |
| `PressDirectiveSnapshot` | Read-only view exposed for #17 channel emission and tests | 1 |

### 2.3 Inputs (read-only at tick start)
- Perception snapshot (#7 §3.7) — agent positions, ball position,
  possession state, attribute lookups.
- First-touch quality scalar (#4 §3.5) — propagated through perception
  or read directly (decided at section-file draft).
- Pass directional event (#5 §3.x — subsection grep-verified at
  section-file draft) — per-tick event ring read.
- #12 baseline `formationSlot` per agent.
- #12 phase enum for this team.

### 2.4 Failure Modes (F1–F6 above)
Section files enumerate each with detection condition, recovery
action, and test reference.

### 2.5 Version History

---

## SECTION 3 — CORE FORMULAS AND ALGORITHMS

### 3.1 Trigger detection
Four sub-algorithms, one per trigger. Each produces a boolean flag
plus a "trigger origin" `EntityId` (ball-carrier or candidate
receiver). Pseudocode in §3.11.

### 3.2 Trigger debounce (hysteresis)
Each trigger flag is held for `TRIGGER_DWELL_TICKS [EST]` before
firing the press; held for `TRIGGER_RELEASE_TICKS [EST]` after the
upstream condition clears. Binding to #2 §3.1 (KD-9).

### 3.3 Primary-press selection
Of all eligible agents (stamina ≥ `PRESS_STAMINA_MINIMUM` from #8;
within `PRESS_TRIGGER_DISTANCE` from #8 §3.1.8.2 of the ball-carrier;
not GK), select the agent whose post-displacement cost (squared
distance from current position to ball-carrier projected interception
point) is lowest. EntityId terminal tie-break.

### 3.4 Cover-shadow selection
Candidate receivers = opponents within `COVER_SHADOW_CANDIDATE_RADIUS_M [GT]`
of the ball-carrier and visible per #7 perception. For each candidate,
compute the shadow-lane position (§3.5). Assign cover-shadow agents
greedily by cost, up to `MAX_COVER_SHADOWS`. Respect KD-16 invariants;
reject assignments that violate them and demote to `HOLD_SHAPE`.

### 3.5 Cover-shadow lane geometry
`shadowPos = lerp(ballCarrierPos, receiverPos, COVER_SHADOW_LANE_FRACTION)`.
`COVER_SHADOW_LANE_FRACTION = 0.55 [GT]` — slightly past midpoint
toward the receiver, biasing for interception over angle denial.
Worked example: ball at (60, 30), receiver at (75, 40) → shadow at
(68.25, 35.5).

### 3.6 Role transitions (hysteresis)
A role transition fires only after the new candidate has been
preferred for `ROLE_DWELL_TICKS [EST]` consecutive ticks. Prevents
role-thrash when two candidates are near-equal cost.

### 3.7 Stamina costs
Per-tick fatigue accumulation: `PRIMARY_PRESS` adds
`STAMINA_COST_PRIMARY_PER_TICK [GT]`; `COVER_SHADOW` adds
`STAMINA_COST_SHADOW_PER_TICK [GT]`. Fatigue ceiling
`PRESS_FATIGUE_CEILING [GT]` excludes the agent from press roles;
cite-not-redefine #8 §3.1.8.1 stamina-gate logic.

### 3.8 Disengage and reset
Disengage when: (a) all triggers cleared for `DISENGAGE_TIMEOUT_TICKS [GT]`,
OR (b) ball leaves press-eligible zone (defined by #1 corner-origin
coordinates: pitch x ∈ [`PRESS_ZONE_X_MIN`, `PRESS_ZONE_X_MAX`] in
this team's attacking direction). After disengage, no new press
fires for `RESET_LATENCY_TICKS [GT]`.

### 3.9 Anti-chaos invariant enforcement
Apply KD-16 invariants in order: (1) check ball-side-third presser
count; (2) check backline floor; (3) check displacement cap. Any
violation demotes the lowest-priority assignment to `HOLD_SHAPE` and
re-checks until clean.

### 3.10 Constants catalogue (forward ref to §6.1)
All thresholds and counts. Tagged per KD-14.

### 3.11 Pseudocode (per-tick main loop)
1. Read perception snapshot, #12 phase, #12 baseline slots.
2. Gate on phase (KD-11): if `IN_POSSESSION`, emit empty directive,
   return.
3. Update trigger flags (§3.1) and debounce (§3.2).
4. If no trigger active and not in reset latency, emit empty directive.
5. Else: select primary press (§3.3); select cover shadows (§3.4);
   apply role hysteresis (§3.6).
6. Enforce anti-chaos invariants (§3.9). On violation: fall back to
   all-`HOLD_SHAPE` per FR-PR-039.
7. Accumulate stamina costs (§3.7).
8. Check disengage / reset (§3.8).
9. Publish `PressDirective` + per-agent `PressAssignment`.

### 3.12 Version History

---

## SECTION 4 — ARCHITECTURE, FILE LAYOUT, INTERFACE CONTRACTS

### 4.1 Architecture Overview
Single subsystem `PressingAI` on the 10 Hz scheduler. Pure-function
design except for hysteresis state (authoritative; digested).
**Runtime activates at Stage 1** — Stage 0 ships the spec, not the
code.

### 4.2 File Structure (#20 §4.2 compliant — single catalogue)
```
src/PressingAI/                                  (Stage 1)
├── PressingAITick.cs           (10 Hz entry point)
├── TriggerEvaluator.cs         (§3.1 + §3.2)
├── PrimaryPressSelector.cs     (§3.3)
├── CoverShadowSelector.cs      (§3.4 + §3.5)
├── RoleHysteresis.cs           (§3.6; authoritative state)
├── StaminaAccumulator.cs       (§3.7)
├── DisengageResolver.cs        (§3.8)
├── InvariantEnforcer.cs        (§3.9 + KD-16)
└── PressingAIConstants.cs      (SINGLE catalogue per FR-CS-025 / KD-15)
```

### 4.3 Internal Module Contracts
Module-by-module input/output declared as `readonly struct`
parameters. No `class` types on hot path.

### 4.4 Upstream Integration Contracts
- Perception snapshot read (#7 §3.7) at tick start.
- First-touch quality scalar (#4 §3.5) — surface chosen at section
  draft.
- Pass event ring (#5) — subsection grep-verified at section-file
  draft.
- #12 baseline slot accessor (`PositioningAI.GetFormationSlot`).
- #12 phase accessor (`PositioningAI.GetPhase`).

### 4.5 Downstream Integration Contracts
- `PressDirective` / `PressAssignment` consumed by orchestrator,
  which (a) forwards to #12 slot composition for override-eligible
  agents, and (b) makes available to #8 §3.1.8 via the chosen
  mechanism in KD-3.
- `PressDirectiveSnapshot` read-only view for #17 event emission and
  tests.

### 4.6 Determinism & Safety Boundaries (binding to #16)
Iteration order; RNG domain tag (`[CROSS-PENDING]`); digest scope
(directives, assignments, role hysteresis state, trigger debounce
state, stamina accumulators).

### 4.7 Cross-Specification Validation Checks
Section-file draft enumerates: invariant respects #12 line membership;
GK never assigned (KD-13); fatigue convention (KD-1); EntityId
no-reuse cited (#2 §2.5 / #8 §1.7.3).

### 4.8 Version History

---

## SECTION 5 — TEST PLAN

### 5.1 Test Counts (verifiable target)

| Category | Target | Source |
|---|---|---|
| Unit (trigger detection, debounce, selection, lane geometry, hysteresis, stamina, disengage) | ≥40 | §3.1–§3.8 |
| Integration (full-team press under each trigger × each phase) | ≥10 | §3.11 |
| Determinism regression | ≥6 | #16 §5 |
| Performance | ≥3 | §6 |
| Anti-chaos invariant tests | ≥6 | KD-16 |
| Exploit-resistance (KD-17 corpus) | ≥4 | KD-17 |
| **Total** | **≥69** | — |

### 5.2 Unit Test List (representative)
- Each of the four triggers fires under its canonical input.
- Each trigger debounce holds for `TRIGGER_DWELL_TICKS` before firing.
- Primary-press selection picks the lowest-cost agent.
- Cover-shadow lane position matches `COVER_SHADOW_LANE_FRACTION`.
- Role hysteresis: oscillating cost stays in original role for
  ≥`ROLE_DWELL_TICKS`.
- Stamina accumulation matches `STAMINA_COST_*` constants.
- Disengage timeout fires at `DISENGAGE_TIMEOUT_TICKS`.
- Reset latency suppresses re-press for `RESET_LATENCY_TICKS`.
- Each of F1–F6 has a dedicated test.

### 5.3 Integration Test List
- Each trigger × out-of-possession phase produces a valid directive.
- Phase boundary (out→in) immediately empties the directive.
- Possession turnover sequence (in→trans→out) produces correct
  per-tick directive evolution.

### 5.4 Determinism Regression (binding to #16 §5)
- 90-min match replay produces bit-identical per-tick digest on
  reference host.

### 5.5 Performance Validation (binding to §6)
Per-tick budget measured against named host (KD-15 from #12 — pinned
in §6.3).

### 5.6 Anti-Chaos & Exploit-Resistance Scenarios (resolves outline.md M-3 / M-4)
- KD-16 invariants: one test per invariant, asserting violation
  causes fallback to all-`HOLD_SHAPE`.
- KD-17 corpus: `EXPLOIT_LONG_BALL_OVER_PRESSERS`,
  `EXPLOIT_SWITCH_OF_PLAY`, `EXPLOIT_ONE_TWO_BOUNCE`,
  `EXPLOIT_GK_PIVOT`.

### 5.7 Version History

---

## SECTION 6 — PERFORMANCE ANALYSIS AND BUDGETS

### 6.1 Constant Catalogue
Full enumeration with tags. Trigger thresholds, hysteresis counts,
role caps, stamina costs, disengage/reset timing, anti-chaos floors,
zone polygons. All `[EST]` at outline stage per KD-14.

### 6.2 Hot Path Enumeration (#18 KD-10 binding)
Main per-tick loop: 22 agents iterated O(1) for assignment + O(N×M)
for cover-shadow candidate scan where N = nearby opponents,
M = receivers (bounded by `COVER_SHADOW_CANDIDATE_RADIUS_M`). Worst
case ≈ 50 pairwise checks.

### 6.3 Per-Tick Budget (reference host per #12 KD-15)
Target: ≤0.10 ms per 10 Hz tick on the named reference host
(Ryzen 7 5800X @ 4.5 GHz, single thread, Mono backend, Unity 2022.3
LTS). Caveat: cert host budget supersedes once
`certification-platform.md` is pinned by lead developer; ±30%
variance possible.

### 6.4 Per-Frame Budget
N/A — no per-frame work.

### 6.5 Memory Footprint
`PressDirective` + `PressAssignment[22]` + `RoleHysteresisState[22]`
+ trigger state ≈ <1 KB.

### 6.6 Version History

---

## SECTION 7 — FUTURE EXTENSIONS

### 7.1 Stage 1 — Runtime activation (KD-12)
Implementation lands once: (a) #8 ratifies ERR-013-001 amendment for
the #13 read surface; (b) #12 reaches APPROVED; (c) #17 channel rows
land via ERR-013-002 / ERR-013-003.

### 7.2 Stage 1+ — Coordinated press patterns
High press / mid block / low block as named press styles, selectable
per match via team-instruction infrastructure (planning doc Month 5–6
"Team Instructions").

### 7.3 Stage 1+ — Trap-zone authoring
Custom trap-zone polygons selectable per opponent.

### 7.4 Stage 1+ — #14 handoff
Mark/cover hand-off rule between `HOLD_SHAPE` agents (owned by #14)
and pressing agents (owned by #13).

### 7.5 Stage 1+ — `PRESS_TRIGGERED` / `PRESS_DISENGAGED` channels
Atomic back-prop into #17 §3.10 via ERR-013-002 / ERR-013-003.

### 7.6 Stage 1+ — Stamina-fatigue model integration
Per the planning doc reference to Fatigue System (Stage 1; called
"#13" in old #8 §3.1.8.1 prose — note the stale ref, file as
ERR-013-004 if still present at section-file draft).

### 7.7 Stage 2+ — ML-tuned `[GT]` parameter fitting
Trigger thresholds, role caps, and hysteresis counts as ML-fit
parameters.

### 7.8 Stage 2+ — Per-archetype press preferences
e.g., 4-3-3 high-press default, 5-3-2 mid-block default.

### 7.9 Stage 5+ — Fixed64 migration per #9.

### 7.10 Stage 5+ — Cross-platform determinism per #9.

---

## SECTION 8 — REFERENCES AND CITATIONS

### 8.1 Cross-Spec References (grep-verified at section-file draft time)
- #1 §1.2, §3.x (sideline geometry)
- #2 §2.5 (XC-002-001 — EntityId no-reuse), §3.1 (hysteresis pattern)
- #4 §3.5 (pressure / first-touch quality)
- #5 §3.x (pass directional event — subsection grep at section-file
  draft; #5 re-approved May 6, 2026)
- #7 §3.7–§3.10 (perception schema)
- #8 §1.4.21 (Stage-1 deferral text), §1.5 (interaction row), §1.7.3
  (XC-008-001), §3.1.8 + §3.1.8.1 + §3.1.8.2 (PRESS utility surface
  this spec advises), §3.2.7 (PRESS utility scoring)
- #11 §7.x (GK ownership; KD-13)
- #12 §3.0 (phase enum), §3.7 (formation slot output), §7.3
  (`PressOverride` binding slot)
- #16 §3.2, §3.2.5, §3.4 (ERR-013-001 or inherits ERR-012-001 block),
  §5, §6.2
- #17 §3.10 channel registry (Stage 1 back-prop via ERR-013-002 /
  ERR-013-003)
- #18 §3.7, §6
- #19 §3, §4
- #20 §4.2 (FR-CS-025)

### 8.2 CLAUDE.md Invariants Bound
Corner origin, fatigue convention, tick rates, zero-alloc hot path,
constant-tag policy, Interface Design Principle (cited heavily —
explains why #14/#15 handoffs are deferred and why no #8 amendment
text lives in this spec).

### 8.3 Typed Cross-Reference IDs
- `XC-013-NNN` — allocated at section-file draft for each upstream
  citation that crosses a boundary.
- `ERR-013-001` — back-prop to #8 §3.1.8.2 (or §2.2.6 if mechanism
  chosen at section-file draft is a `TacticalContext` extension) to
  add a read of #13's `PressAssignment`. Filed at section-file draft.
- `ERR-013-002` — back-prop to #17 §3.10 to register
  `PRESS_TRIGGERED` channel. Filed at Stage 1.
- `ERR-013-003` — back-prop to #17 §3.10 to register
  `PRESS_DISENGAGED` channel. Filed at Stage 1.
- `ERR-013-004` (conditional) — stale "Fatigue System #13" ref in #8
  §3.1.8.1 L753; current #13 is Pressing AI. One-line patch request
  if the ref is still present at section-file draft.
- Inherits `DOMAIN_TAG_PRESSING_AI = 0x19` from ERR-012-001 block
  proposal. If block not yet ratified at section-file draft, files
  its own ERR-013-005 mirror request.

### 8.4 Version History

---

## SECTION 9 — APPROVAL CHECKLIST

### 9.1 Self-Contained Spec Content
- All 13 outline.md findings resolved (mapping in §9.4).
- All 44 FRs cross-referenced.
- All constants tagged (`[EST]` for outline-stage placeholders).
- All cross-spec citations grep-verified at section-file draft time.
- Stage-binding statement (§1.8) makes Stage-0/Stage-1 split
  unambiguous.

### 9.2 Cross-Spec Sign-Offs Required
- #16 lead-developer approval of `DOMAIN_TAG_PRESSING_AI` value (via
  ERR-012-001 inheritance OR fresh ERR-013-005).
- #8 owner ack of the read-surface mechanism chosen in KD-3 (accessor
  vs `TacticalContext` extension) — ratification of ERR-013-001
  amendment text.
- #12 owner ack of the `PressOverride` slot-composition contract per
  #12 §7.3.

### 9.3 KD-Sequencing Preconditions
- (a) `ERR-013-001` mechanism choice ratified by lead developer.
- (b) Domain-tag allocation resolved (inherits ERR-012-001 or fresh).
- (c) All `[CROSS-PENDING]` tags promoted to `[CROSS]`.
- (d) Hysteresis & trigger `[EST]` constants promoted to `[GT]` with
  Appendix A derivations.
- (e) #5 subsection for pass-directional event grep-verified (§8.1).
- (f) #4 first-touch quality surface confirmed (perception-propagated
  or direct).
- (g) Lead-developer R-01..R-05 review pass.

### 9.4 Finding-to-Resolution Map

| Review | Finding | Sev | Resolved by |
|---|---|---|---|
| outline.md | 1. Missing metadata header | H | "Metadata Header" |
| outline.md | 2. Section plan misaligned with template | H | §1–§9 mapping in this outline |
| outline.md | 3. Trigger-detection upstream sources undeclared | H | KD-7 trigger catalog + Boundary Matrix + §8.1 citations |
| outline.md | 4. Pass Mechanics #5 SUSPENDED-status risk | H | Stage-binding clarification §1.8 + status update (#5 re-approved May 6) + grep at section-file draft |
| outline.md | 5. Cover-shadow / lane denial requires Perception #7 | H | KD-7 (WEAK_RECEIVER) + §3.5 cites #7 + Boundary Matrix |
| outline.md | 6. Determinism plan absent | M | KD-10 + §3.9 / §3.11 + §4.6 |
| outline.md | 7. Stamina/fatigue convention not pre-committed | M | KD-1 + FR-PR-008 + §3.7 (cite-not-redefine #8 §3.1.8.1) |
| outline.md | 8. Boundary with #14/#12 unstated | M | KD-4 / KD-5 + Boundary Matrix |
| outline.md | 9. Tick-rate split unstated | M | KD-2 + §1.7 |
| outline.md | 10. Anti-chaos guardrails undefined | M | KD-16 (three measurable invariants) + FR-PR-018..021 + §5.6 |
| outline.md | 11. Exploit-resistance tests undefined | M | KD-17 (canonical exploit corpus) + §5.6 |
| outline.md | 12. Constant-tag policy not invoked | L | KD-14 + §6.1 |
| outline.md | 13. No event production declared | L | KD-11 + §7.5 (channels deferred to Stage 1 with ERR-013-002/003) |

### 9.5 Lead-Developer Sign-Off Lines (R-01..R-05)

### 9.6 Version History

---

## APPENDICES

### Appendix A — Derivations
Trigger-threshold derivations, hysteresis dwell-time selection
(binding to #2 §3.1 proof), cover-shadow lane fraction, anti-chaos
invariant floors/ceilings. Each `[EST]` constant gets one entry here
when promoted to `[GT]`.

### Appendix B — Trigger Catalog Reference Cards
One per trigger (BAD_TOUCH, BACKWARD_PASS, SIDELINE_TRAP,
WEAK_RECEIVER): input surface, threshold, debounce, worked example,
test reference.

### Appendix C — Cover-Shadow Lane Geometry
Diagrams + worked examples for `COVER_SHADOW_LANE_FRACTION` at the
three canonical configurations (carrier+receiver vertical, diagonal,
horizontal).

### Appendix D — Anti-Chaos Sensitivity Analysis
Per-invariant sensitivity sweeps for `MAX_PRESSERS_BALL_THIRD`,
`MIN_BACKLINE_AGENTS`, `MAX_PRESS_DISPLACEMENT_M`.

### Appendix E — Exploit-Resistance Playbook (KD-17 corpus)
Long ball over pressers, switch of play, one-two bounce, GK pivot.
Each with input scenario, expected directive evolution, and pass
criterion.

### Appendix F — Glossary
Trigger, directive, assignment, primary press, cover shadow, shadow
lane, trap zone, disengage, reset latency, anti-chaos invariant.

### Appendix G — Telemetry & Troubleshooting Playbook
Stage 1+ debug overlays; Stage 0 placeholder only.

---

## OUTSTANDING OUTLINE-PHASE QUESTIONS

The following will be resolved during section-file authoring; flagged
here so the section-file PASS-1 self-adversarial review can target
them.

### Q1 — KD-3 mechanism selection
Accessor (`PressingAI.GetAssignment(EntityId)`) vs.
`TacticalContext.PressDirective` field extension via #8 §2.2.6
amendment. The latter requires unfreezing the Stage-0-frozen
`TacticalContext` schema — but #13 is a Stage-1 binding, so the
freeze argument may not apply. Decision deferred to section-file
draft; both options preserved in KD-3 prose.

### Q2 — #4 first-touch quality surface
Whether #4's pressure scalar reaches #13 via perception snapshot
(propagated by #7) or via a direct read. Grep #7 §3.7–§3.10 at
section-file draft; default assumption is perception-propagated.

### Q3 — #5 pass directional event subsection
Exact subsection of #5 publishing the directional event. Grep
required at section-file draft.

### Q4 — Domain-tag inheritance vs. fresh allocation
ERR-012-001 proposes `DOMAIN_TAG_PRESSING_AI = 0x19` as part of the
Phase B/C block. If `0x19` is taken by another spec when section-file
draft happens, file ERR-013-005 for re-allocation; otherwise inherit.

### Q5 — `PRESS_ELIGIBLE_ZONE` polygon definition
§3.8 references a polygon for ball-in-zone disengage. Concrete
coordinates (per-team-attacking-direction) defined at section-file
draft; default proposal is x ∈ [`THIRD_LINE_X`, opponent goal-line]
for high-press style, extensible by team-instruction parameters at
Stage 1+.

---

## NEXT STEPS

1. (Optional) Self-adversarial pass against this v1.0 outline.
2. Draft `section-1.md`.
3. Draft `section-2.md` (FR table — already enumerated above).
4. Draft `section-3.md`.
5. Draft `section-4.md`.
6. Draft `section-5.md`.
7. Draft `section-6.md` (promote `[EST]` → `[GT]` with derivations
   in Appendix A).
8. Draft `section-7.md`.
9. Draft `section-8.md` (grep-verify every citation).
10. Draft `section-9-approval-checklist.md`.
11. Draft `appendices.md`.
12. Adversarial review pass PASS-1 on section files.
13. v0.2 fix pass.
14. Flip `SPEC_INDEX.md` row 13 `NOT STARTED → IN REVIEW`.
15. Lead-developer R-01..R-05 sign-off.

---

## VERSION HISTORY

| Version | Date | Author | Summary |
|---|---|---|---|
| 1.0 | May 16, 2026 | AI agent (claude/draft-ai-specs-outline-I1RSX) | Initial detailed outline. Resolves all 13 findings from the May 6, 2026 review at the bottom of `outline.md` (5 H / 6 M / 2 L). Establishes 17 cross-cutting design decisions (KD-1..KD-17). Stage-binding clarification §1.8 anchors #13 runtime to Stage 1 per #8 §1.4.21 / §1.5 — Stage 0 deliverable is spec text only. 44 FRs enumerated. 4 canonical triggers (BAD_TOUCH / BACKWARD_PASS / SIDELINE_TRAP / WEAK_RECEIVER) each cited to upstream surface. 3 roles (PRIMARY_PRESS / COVER_SHADOW / HOLD_SHAPE) as disjoint partition. 3 anti-chaos invariants (KD-16) + 4-exploit test corpus (KD-17). Five Outstanding Outline-Phase Questions flagged for section-file PASS-1. |
