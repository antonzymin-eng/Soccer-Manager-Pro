# Defensive AI Specification #14 — Detailed Outline

**Created:** May 17, 2026
**Last Updated:** May 17, 2026
**Version:** 1.0
**Status:** DRAFT — expansion of `outline.md` into section-by-section plan.
Resolves all 14 findings (6 H / 6 M / 2 L) from the May 6, 2026 adversarial
review at the bottom of `outline.md`.
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
| Spec # | 14 |
| Title | Defensive AI |
| Folder | `docs/specs/defensive-ai/` |
| Priority | 4 (Phase C tactical-AI chain — depends on #12 and #13) |
| Status | NOT STARTED (outline phase) |
| Owner | Lead developer (gameplay-AI domain) |
| Approved Dependencies | #1 Ball Physics; #2 Agent Movement; #3 Collision System; #4 First Touch; #5 Pass Mechanics; #6 Shot Mechanics; #7 Perception; #8 Decision Tree; #13 Pressing AI (APPROVED May 17, 2026); #16 Deterministic Simulation; #17 Event System; #18 Performance; #19 Testing; #20 Code Standards |
| Pending Dependencies | #11 Goalkeeper Mechanics (IN REVIEW) — consumed as boundary reference for wall positioning and out-of-pos GK coverage; #12 Positioning AI (IN REVIEW) — consumed as baseline shape source and line/lane membership provider; ERR-014-001 (domain-tag allocation in #16 §3.4) opens at section-file draft |
| Downstream Consumers | #15 Attacking AI (Phase C linear chain) |
| Stage Binding | **Spec drafted at Stage 0. Runtime activation is Stage 1** per #8 §1.3.2 ("Stage 1+: Defensive AI #14 introduces coordinated mark assignments") and the Phase C tactical-AI activation sequence. Stage 0 deliverable from #14 = published interface schema + algorithm specification only; no runtime code emitted at Stage 0. |
| Estimated Effort | 6–8 working days |

---

## STAGE-BINDING CLARIFICATION (resolves outline.md H-2 partial)

**Spec authored at Stage 0; runtime activates at Stage 1.**

At Stage 0, #8 Decision Tree handles all defensive individual behavior via:
- `INTERCEPT` action: agent moves to intercept the ball trajectory.
- `PRESS` action: agent moves toward ball-carrier (uncoordinated).
- `MOVE_TO_POSITION` action: agent moves to its #12-baseline slot.

These produce credible Stage-0 defensive behavior without coordination. #14
introduces **coordinated** defensive assignment — zonal and man-marking
designations that bias which agents target which opponents, when to step the
offside line, and how to manage last-man/emergency scenarios. This is the
same pattern as #13's Stage-1 binding: spec text exists at Stage 0 to name
contracts; code activates at Stage 1.

**Stage 0 deliverable from #14:** published specification (this document and
the section files). No runtime code at Stage 0.

**Why authoring at Stage 0 has value:**
1. Names the cross-spec contracts so #15 Attacking AI can reference a stable
   surface.
2. Files the back-prop amendments #14 will need from #8 (as ERR-014-NNN)
   ahead of Stage 1.
3. Declares the handoff protocol with #13 (KD-4), resolving the lingering
   §4 "handoff rules" gap in both specs' outlines.
4. Preserves the "write all 20 specs before any code" discipline.

---

## CROSS-CUTTING DESIGN DECISIONS

### KD-1 — Cite-not-redefine
Spec #14 never restates a CLAUDE.md invariant or a rule from another approved
spec. Cited: corner-origin coordinates (#1 §1.2); fatigue `0 = rested,
1 = fatigued` (CLAUDE.md); 10 Hz tactical / 60 Hz physics (CLAUDE.md);
EntityId no-reuse (#2 §2.5 XC-002-001 + #8 §1.7.3 XC-008-001); perception
schema (#7 §3.7–§3.10); #12 baseline formation slot and line membership
(#12 §3.7, §4.5.2); #13 role partition (#13 §2.2, FR-PR-014); #3 tackle
contact physics (#3 §3.x).

### KD-2 — 10 Hz tactical, no 60 Hz work
Defensive AI runs on the 10 Hz tactical loop. Output is a per-team
`MarkDirective` (one struct per team per tick) and a per-agent
`MarkAssignment` (one struct per agent per tick). No 60 Hz steering work —
physical pursuit is owned by #2 via #8's resolved `Action.TargetPosition`
(same path as #12 and #13).

### KD-3 — Boundary with Positioning AI #12 (resolves outline.md H-5 partial)

**Verified facts** (grepped from `positioning-ai/section-2.md`,
`section-4.md`):
- #12 §2.2 struct `FormationSlot` carries: `baselinePosition Vector2`,
  `lineMembership`, `laneAssignment`, `defensiveLineDepth`.
- #12 §4.5.2 declares `BaselineDefensiveShape` read-only view for #14
  (Stage 1+); `PositioningAI.GetLine(EntityId)` exposed at Stage 1+.
- #12 §2.2 FR-PA-048: "No interface produced against unspecified consumers
  (#13/#14/#15) at Stage 0."

**Therefore #14's role at Stage 1:**
- **#12 owns:** per-agent baseline `formationSlot` (out-of-possession shape),
  `DefensiveLineDepth` (tactical depth parameter), phase enum.
- **#14 reads:** `BaselineDefensiveShape`, `LineMembership`, `LaneAssignment`
  via Stage 1+ accessors on the #12 subsystem.
- **#14 does NOT:** modify the baseline slot. #14 issues override layers
  (`MarkAssignment`) on top of #12 anchors, just as #13 issues
  `PressOverride` on top of #12 anchors. The orchestrator composes.

**#12 `DefensiveLineDepth` field:** set by team-instruction infrastructure
(Stage 1+ coach-UI system). #14 **reads** this field to know the target
line position for offside trap execution; #14 does not write it.

**Stage 0 at spec text:** #12 accessor names are declared here as boundary
hints. No code references exist yet (consistent with #12 FR-PA-048 and the
"Interface Design Principle" — no interface against unspecified consumer).

### KD-4 — Boundary with Pressing AI #13 (resolves outline.md H-5)

**Verified facts** (grepped from `pressing-ai/section-2.md` FR-PR-014,
FR-PR-017, `section-4.md` §4.5):
- #13 FR-PR-014: "Roles are a disjoint partition per agent per tick
  (PRIMARY_PRESS ⊕ COVER_SHADOW ⊕ HOLD_SHAPE)."
- #13 §4.5: `PressDirective` / `PressAssignment` consumed by orchestrator.
- #13 §7.4 (Future Extensions): "#14 handoff — mark/cover for HOLD_SHAPE
  agents is owned by #14."

**#13 owns:** agents with role `PRIMARY_PRESS` or `COVER_SHADOW` on any
given tick. Their target position is #13-derived.

**#14 owns:** agents with role `HOLD_SHAPE` on any given tick. Within this
subset, #14 assigns `ZONAL`, `MAN_MARK`, or `INTERCEPT_RUNNER` modes.

**Disjoint enforcement:** at Stage 1 the orchestrator reads #13's
`PressAssignment` first. Any agent with role ≠ `HOLD_SHAPE` in #13's
directive is **excluded** from #14's assignment pool entirely. #14 MUST
NOT assign a mark to an agent currently in a press role (FR-DA-010).

**Handoff sequence (Stage 1 per tick):**
1. #12 publishes per-agent baseline `formationSlot`.
2. #13 publishes per-agent `PressAssignment` (role).
3. #14 reads #13's roles; filters its assignment pool to HOLD_SHAPE agents.
4. #14 issues per-agent `MarkAssignment` for the HOLD_SHAPE subset.
5. Orchestrator composes: PRESS/COVER_SHADOW agents use #13 target;
   HOLD_SHAPE agents use #14 mark target; #8 reads the composed slot.

**Stage 0:** no coupling. Both specs ship as inert text at Stage 0.

### KD-5 — Boundary with Decision Tree #8 (resolves outline.md H-5 partial)

**Verified facts** (grepped from `decision-tree/section-1.md` §1.3.2,
§1.5; `decision-tree/section-3-1.md` §3.1.7–§3.1.9):
- #8 Stage-0 action set: PASS, SHOOT, DRIBBLE, HOLD, MOVE_TO_POSITION,
  PRESS, INTERCEPT. No MARK or CONTAIN action type at Stage 0.
- #8 §1.3.2 defers coordinated defensive assignments to Stage 1+ (#14).

**Stage 1 role:**
- #14 publishes a `MarkAssignment` per HOLD_SHAPE agent per tick.
- #8 §3.1.7 (MOVE_TO_POSITION) and §3.1.9 (INTERCEPT) are extended at
  Stage 1 to consult #14's `MarkAssignment` for target position selection.
- **Coupling direction:** one-way. #14 → #8 read-only. Mechanism: accessor
  `DefensiveAI.GetMarkAssignment(EntityId)` OR a `TacticalContext.MarkDirective`
  field added via the #8 §2.2.6 amendment process. Mechanism selected at
  section-file draft; tracked as ERR-014-001 against #8 §2.2.6 / §3.1.7.
- **Stage 0:** no coupling. #8 §3.1.7 / §3.1.9 remain unchanged.

### KD-6 — Boundary with Collision System #3 (resolves outline.md H-3)

**Verified facts** (grepped from `collision-system/section-1.md`; from
CLAUDE.md KD "Parameter-Based Physics"):
- #3 owns: contact physics, tackle force model, slide-tackle geometry,
  foul detection, agent-agent impulse response.
- #14 owns: tackle *intent* — the per-tick decision of whether to commit
  (lunge), jockey (shadow without lunging), or hold position.

**Boundary:**
- **#14 produces:** `TackleIntentRequest` — a per-agent per-tick struct
  carrying: target opponent `EntityId`, mode (`COMMIT` | `JOCKEY` |
  `HOLD`), approach angle (float), coverage depth (count of defenders
  behind the tackling agent).
- **#8 reads** the `TackleIntentRequest` and translates it into an
  `AgentAction` dispatched to #3 (consistent with the parameter-based
  physics principle — #14 supplies intent parameters, #3 translates
  them into contact physics).
- **#3 produces:** contact outcome (tackle result, foul flag, possession
  change) consumed by #1/#2 as always. #3 does not read #14 directly.
- At Stage 0: `TackleIntentRequest` is a spec-level declaration only;
  no runtime code produced.

### KD-7 — Boundary with Goalkeeper Mechanics #11 (resolves outline.md L-14)

**Verified facts** (grepped from `goalkeeper-mechanics/section-1.md`
§1.4, §1.5; FR-GK-016; §3.3.3):
- #11 owns: GK save pipeline, claim/dive/distribution decisions, GK
  positioning within goal zone.
- #11 FR-GK-016: "Defensive wall (outfielder positioning at free-kicks)
  is out of scope — Defensive AI #14."
- #11 §1.4: "Defensive wall positioning at free-kicks → Defensive AI #14."

**#14 owns:**
1. **Defensive wall formation** at free-kicks: outfield player selection,
   wall depth (distance from ball), spacing, and the instruction to hold
   position. Deferred to Stage 2+ set-piece system; declared here as
   #14's responsibility boundary.
2. **Coverage for out-of-position GK:** when the last-man predicate (KD-12)
   fires AND the GK's x-position is outside its expected zone (read from
   the #7 perception snapshot), #14 issues a retreat instruction to the
   nearest outfield defender in the backline as an emergency `MarkAssignment`
   override (mode `COVER_GK_ZONE`).

**Stage 0 boundary:** the `COVER_GK_ZONE` mode and wall formation are
declared in spec text; no Stage-0 interfaces written against #11 (consistent
with Interface Design Principle — both #11 and #14 are still in outline/review
stage; binding is a Stage 1 concern). The boundary is a *hint* here;
once both reach APPROVED, the full coupling surface is declared.

### KD-8 — Boundary with Attacking AI #15

At Stage 0, #15 is NOT STARTED. #14 is out-of-possession behavior; #15 is
in-possession behavior. The two are mutually exclusive at the team level
(gated by #12's phase enum). No further coupling at Stage 1 beyond phase
enumeration agreement. Each team's `MarkDirective` is suppressed when the
team holds possession (FR-DA-013).

### KD-9 — Offside-line ownership (resolves outline.md H-4)

**Two responsibilities that must NOT be conflated:**

| Responsibility | Owner | Notes |
|---|---|---|
| Defensive line step-up decision | **#14** | Timing, trigger, depth target (x-coordinate) |
| Offside adjudication (goal validity) | **Future referee/rules spec** | Not in the 20-spec set; out of scope for #14 |

**#14's offside-line model:**
- The "offside line" is the x-coordinate of the last outfield defender
  (corner-origin, own-half direction) at any given tick.
- #14 owns the decision to *step up* this line (offside trap execution):
  it computes a target step-depth, issues a mark-assignment override to
  all backline defenders to advance simultaneously, and tracks line
  stability via hysteresis.
- #14 **does NOT** adjudicate whether a striker is offside — that is a
  match-rules concern. #14 merely places defenders; the rules system
  (future spec) reads agent positions independently.

**Consequence:** the spec section on offside trap must NOT contain any
offside-rule logic, VAR decisions, or goal-line distance checks. Any such
logic is filed as out-of-scope in §1.2.

### KD-10 — Determinism binding (#16) (resolves outline.md M-7)

All `MarkDirective` writes, `MarkAssignment` writes, the internal assignment
hysteresis state, line-step state, and tackle-intent state are authoritative
simulation state per #16 §3.2 and appear in the per-tick digest at the scope
#16 §6.2 defines for tactical-AI outputs. Agent iteration uses the canonical
EntityId sort from #16 §3.2.5. Assignment evaluation order across agents on
the same tick is EntityId-ascending. Any stochastic tie-breaking (e.g., two
defenders equidistant from the same opponent) uses `DeterministicRngService`
with domain tag `DOMAIN_TAG_DEFENSIVE_AI` — value `[CROSS-PENDING]` until
lead-developer ratifies the allocation via ERR-014-001 (proposed `0x1A`,
next available in the ERR-012-001 Phase B/C block after `0x19` for #13).

### KD-11 — Hysteresis pattern reuse (#2 §3.1 binding)
Mark assignment transitions and line-step hysteresis use the dwell-time +
dead-zone hysteresis pattern from #2 §3.1. #14 does NOT define a new
algorithm — it parameterises the #2 pattern. All hysteresis constants are
`[EST]` at outline stage; promotion to `[GT]` happens at section-file
authoring when each value gains a worked-example justification (Appendix A).

### KD-12 — Last-man predicate (resolves outline.md H-6)

**Formal definition (deterministic):**

```
IsLastManCandidate(a) :=
    a ∈ OutfieldDefenders                         // excludes GK
    AND a.position.x == min{b.position.x :
        b ∈ OutfieldDefenders}                    // most-rearward outfield
    (ties broken: lowest EntityId wins)

IsLastManThreat(ballPos, lastMan) :=
    ballPos.x < lastMan.position.x
        + LAST_MAN_BALL_BUFFER_M [GT]            // ball ahead of last man
    AND ballPos.x > LAST_MAN_OWN_HALF_MIN_X [GT] // ball not trivially in own third
```

Coordinate convention: x-axis is goal-to-goal (0 = own goal line,
105 = opponent goal line) per Ball Physics #1 §1.2. The "own-half" direction
for the team defending the x=0 goal means smaller x = closer to own goal.
For the team defending x=105, the perspective inverts — the spec normalises
all last-man computations to a "distance to own goal" scalar so the same
formula works for both teams.

**Anti-GK contamination:** the GK's position is excluded even if it ventures
further forward than outfield defenders (e.g., during a corner), because #11
owns GK positioning decisions.

**Performance:** `min` over ≤11 outfield defenders. O(N) per tick, N ≤ 11.

### KD-13 — Constant-tag discipline (resolves outline.md M-12)
Every constant carries exactly one of `[GT]`, `[EST]`, `[FIXED]`,
`[DERIVED]`, `[CROSS]`, `[CROSS-PENDING]`. All assignment thresholds and
hysteresis values start `[EST]` at outline stage; promotion to `[GT]` happens
at section-file authoring per CLAUDE.md.

### KD-14 — Single constant catalogue per #20 §4.2 (FR-CS-025)
ALL constants live in one file `DefensiveAIConstants.cs`, organised into
`#region` blocks per #20 §4.2.

### KD-15 — Event System binding (#17) (resolves outline.md M-11)
At Stage 1 runtime, #14 emits two event channels for telemetry and #15
handoff:
- `MARK_ASSIGNED` — fired when a `MarkAssignment` changes mode or target
  (not on every tick — only on transitions).
- `LINE_STEPPED` — fired when a line step-up is executed (offside trap).

Both channels require atomic back-prop into #17 §3.10 channel registry —
filed as ERR-014-002 / ERR-014-003 at section-file draft. At Stage 0, no
channels are produced or consumed.

**Event System Appendix** (`event-system/appendices.md`) reserves byte range
`0x18…0x1B` for #14. Channel IDs will be allocated within this range at
Stage 1 first-commit per #17's §3.10 schema.

### KD-16 — Stage-0/Stage-1 scope discipline
Out of Stage 0 scope (deferred to §7): runtime code, #8 back-prop amendment,
#17 channel registration, #15 handoff implementation, defensive wall
formation (Stage 2+ set-piece system), ML-tuned `[GT]` parameter fitting,
coach UI, save-game persistence, goalkeeper-as-last-man specialized handling
(KD-7), authoring tools, per-player man-marking instructions from team
tactics screen.

### KD-17 — Anti-chaos invariants (resolves outline.md M-7 partial)
Three measurable invariants, enforced as FRs:

1. **Min backline agents:** `MIN_BACKLINE_AGENTS [GT]` (Stage 1 default: 3).
   Agents whose #12 `LineMembership` is `DEFENSE` cannot be promoted to
   `MAN_MARK` mode if doing so drops the count below the floor. Note: this
   invariant is independently enforced by both #14 and #13; #14's check
   applies only to its HOLD_SHAPE subset, which is non-overlapping with
   #13's assignments.
2. **Max man-mark assignments:** `MAX_MAN_MARK_ASSIGNMENTS [GT]` (Stage 1
   default: 4). Caps the total number of agents with `MAN_MARK` mode
   simultaneously, preventing whole-team man-marking.
3. **Max mark displacement from #12 anchor:** `MAX_MARK_DISPLACEMENT_M [GT]`
   (Stage 1 default: 20m). A `MarkAssignment` that would move an agent
   further than this from its #12 baseline anchor is demoted to `ZONAL`
   for this tick.

Invariants are checked BEFORE the directive is published (FR-DA-024).

### KD-18 — Exploit-resistance test corpus (resolves outline.md L-13 partial)
Section 5 MUST include integration tests for the canonical defensive-AI
exploit set:
- `EXPLOIT_OFFSIDE_TRAP_SPRUNG_EARLY` — a striker's early run triggers the
  step-up, leaving the striker through on goal; test asserts the step-up
  only fires when the hysteresis condition is fully met.
- `EXPLOIT_SWITCH_THROUGH_HOLE` — ball switched to a zonal gap not covered
  by any mark assignment; test asserts the nearest HOLD_SHAPE agent
  transitions within `REASSIGN_LATENCY_TICKS [GT]`.
- `EXPLOIT_LAST_MAN_ONE_ON_ONE` — attacker beats last outfield defender;
  test asserts the emergency override fires and the correct `TackleIntentRequest`
  mode (`COMMIT` vs `JOCKEY`) is selected based on approach angle.
- `EXPLOIT_GK_OUT_OF_POSITION` — GK advances for a cross and misses; test
  asserts #14 issues a `COVER_GK_ZONE` assignment to the nearest backline
  defender within one tick.

### KD-19 — Phase enumeration (binding to #12 §3.0)
#14 reads #12's per-team phase enum at tick start. If the team holds
possession (`IN_POSSESSION`), the `MarkDirective` is all-`ZONAL` with no
overrides (pure shape-keeping via #12 baseline). If `OUT_OF_POSSESSION` or
`TRANSITION`, #14's full assignment algorithm fires (FR-DA-013).

---

## BOUNDARY MATRIX (resolves outline.md H-2 / H-5)

| Boundary | #14 owns | Other owns | Direction | Mechanism | Stage 0? |
|---|---|---|---|---|---|
| #8 Decision Tree | `MarkDirective` per team + per-agent `MarkAssignment` for HOLD_SHAPE agents | Per-agent action loop (MOVE_TO_POSITION / INTERCEPT scoring) | #8 reads #14 (Stage 1) | Accessor or `TacticalContext.MarkDirective` extension; ERR-014-001 | No (Stage 1) |
| #12 Positioning AI | Mark-mode overrides for HOLD_SHAPE agents | Baseline out-of-poss `formationSlot`; `LineMembership`; `DefensiveLineDepth` | Orchestrator composes; both read by #8 | `BaselineDefensiveShape` + `GetLine` accessor (Stage 1+) | No (Stage 1) |
| #13 Pressing AI | HOLD_SHAPE agent assignments | PRIMARY_PRESS / COVER_SHADOW agent assignments | #14 reads #13 role partition | Per-tick `PressAssignment` role filter (KD-4 handoff) | No (Stage 1) |
| #3 Collision System | Tackle *intent* (`TackleIntentRequest`) | Contact physics, foul detection, impulse response | #8 reads #14 intent, dispatches to #3 | Parameter-based physics pipeline | No (Stage 1 spec; declared at Stage 0) |
| #11 Goalkeeper | Outfield defensive wall; COVER_GK_ZONE assignment | GK positioning/saves/distribution | #14 reads GK position via #7 perception | KD-7 boundary; Interface Design Principle (Stage 1) | No (spec text only) |
| #2 Agent Movement | (none direct — via #8 action output) | 60 Hz steering | #2 reads #8 | Same path as #12/#13 | No |
| #7 Perception | (none — read consumer) | Filtered world model | #14 reads #7 | Snapshot read at tick start | Yes |
| #15 Attacking AI | (mutually exclusive by possession phase) | In-possession behavior | independent | KD-8 phase gating | No |
| #16 Determinism | `MarkDirective` / `MarkAssignment` / hysteresis state / line-step state / tackle-intent state | Digest format + iteration rule | #14 conforms | EntityId iteration + domain-tagged RNG (`[CROSS-PENDING]` `0x1A`) | Yes (spec text) |
| #17 Event System | `MARK_ASSIGNED` / `LINE_STEPPED` channel definitions | Channel registry | (deferred) | ERR-014-002 / ERR-014-003 at Stage 1 | No (Stage 1) |
| #18 Performance | (conformance only) | Per-tick budget framework | #14 conforms | §6 budget against named host | Yes (spec text) |
| #19 Testing | (conformance only) | Test-framework conventions | #14 conforms | §5 plan | Yes (spec text) |
| #20 Code Standards | (conformance only) | File / catalogue / naming rules | #14 conforms | `DefensiveAIConstants.cs` per FR-CS-025 | Yes (spec text) |

---

## SECTION 1 — INTRODUCTION, SCOPE, DEPENDENCIES, KEY DECISIONS

### 1.1 Purpose
One-paragraph problem statement: #14 specifies coordinated defensive
assignment behavior for HOLD_SHAPE agents — the outfield players out of
possession who are not currently assigned to a press role by Pressing AI #13.
The spec exists to (a) define the mark-mode catalog (ZONAL, MAN_MARK,
INTERCEPT_RUNNER), (b) define the assignment algorithm, (c) implement the
offside trap execution protocol, (d) define the last-man emergency protocol,
(e) declare the tackle-intent surface, (f) enforce anti-chaos invariants,
and (g) declare the integration surface with #8 Decision Tree (where runtime
activation lands at Stage 1).

### 1.2 Scope (in / out)
**In:** HOLD_SHAPE agent mark assignments, mark-mode catalog and algorithm,
offside trap execution (step-up decision and timing), last-man predicate
(KD-12), emergency clearance/cover-GK override, tackle-intent production
(KD-6), anti-chaos invariants (KD-17), exploit-resistance test corpus
(KD-18), parameter catalogue.

**Out (per KD-16):** runtime code, #8 back-prop amendment text (filed as
ERR-014-001), authoring tools, coach UI, set-piece defensive wall formation
(Stage 2+ set-piece system), offside adjudication (future referee spec),
per-player tactical instructions screen, ML tuning, save-game persistence,
Fixed64 migration.

### 1.3 Dependencies
Upstream APPROVED: #1, #2, #3, #4, #5, #6, #7, #8, #13, #16, #17, #18, #19, #20.
Upstream IN REVIEW: #11, #12.
(Section-file draft re-verifies status via `SPEC_INDEX.md` grep before
submitting for sign-off.)

### 1.4 Key Domain Concepts
Mark mode (ZONAL / MAN_MARK / INTERCEPT_RUNNER / COVER_GK_ZONE), mark
directive, mark assignment, HOLD_SHAPE pool, offside trap, last-man predicate,
tackle intent (COMMIT / JOCKEY / HOLD), anti-chaos invariants, assignment
hysteresis, line step-up, emergency override.

### 1.5 Key Design Decisions
Cross-reference KD-1..KD-19.

### 1.6 Interface Boundaries
The Boundary Matrix above.

### 1.7 Coordinate & Convention Bindings (resolves outline.md M-8)
- Corner-origin coordinate system: X = goal-to-goal (0–105m), Y =
  touchline-to-touchline (0–68m), Z = height. Per #1 §1.2 and Appendix C.
- "Own half" for a team defending x=0 goal: x ∈ [0, 52.5]. For team
  defending x=105: x ∈ [52.5, 105]. All last-man and line-depth formulas
  use a normalised "distance-to-own-goal" scalar to avoid per-team branching.
- Fatigue convention: `0.0 = fully rested`, `1.0 = fully fatigued`
  (CLAUDE.md). Any inversion is a critical error.
- Tactical loop: 10 Hz (100 ms/tick). Physics/render loop: 60 Hz (~16.67 ms).
  #14 produces outputs only on the 10 Hz loop (KD-2, resolves M-9).
- EntityId no-reuse per #2 §2.5 (XC-002-001) and #8 §1.7.3 (XC-008-001).

### 1.8 Stage-Binding Statement
**Spec drafted at Stage 0; runtime activates at Stage 1.** Authoritative
basis: #8 §1.3.2 Stage 1+ deferral of coordinated defensive assignment.
Stage 0 deliverable = published specification only. No runtime code at
Stage 0.

### 1.9 Version History

---

## SECTION 2 — FUNCTIONAL REQUIREMENTS, DATA STRUCTURES, FAILURE MODES

### 2.1 Functional Requirements Table (provisional 37-entry enumeration)

Conformance: `MUST` unless noted.

| FR | Subject | Conf. | Source |
|---|---|---|---|
| FR-DA-001 | Tactical tick rate is 10 Hz | MUST | CLAUDE.md / KD-2 |
| FR-DA-002 | Output is one `MarkDirective` per team per tick + one `MarkAssignment` per agent per tick | MUST | KD-2 |
| FR-DA-003 | Agent iteration order is EntityId-sorted ascending | MUST | #16 §3.2.5 / KD-10 |
| FR-DA-004 | `MarkDirective` and `MarkAssignment` contribute to per-tick digest | MUST | #16 §6.2 / KD-10 |
| FR-DA-005 | RNG calls use `DOMAIN_TAG_DEFENSIVE_AI` (`[CROSS-PENDING]` `0x1A` until ERR-014-001 ratified) | MUST | #16 §3.4 / KD-10 |
| FR-DA-006 | No allocation on hot path | MUST | #18 §3.7 |
| FR-DA-007 | Single constant catalogue `DefensiveAIConstants.cs` per #20 §4.2 | MUST | #20 FR-CS-025 / KD-14 |
| FR-DA-008 | Fatigue input convention `0 = rested` | MUST | CLAUDE.md / KD-1 |
| FR-DA-009 | GK is excluded from #14's assignment pool entirely | MUST | KD-7 |
| FR-DA-010 | Agents with #13 role PRIMARY_PRESS or COVER_SHADOW are excluded from #14's assignment pool | MUST | KD-4 |
| FR-DA-011 | Mark modes are: ZONAL, MAN_MARK, INTERCEPT_RUNNER, COVER_GK_ZONE | MUST | §3.3 |
| FR-DA-012 | `DefensiveLineDepth` is read from #12's field; #14 does not write it | MUST | KD-3 |
| FR-DA-013 | Phase gating: if #12 phase is IN_POSSESSION for this team, emit all-ZONAL directive (no override) | MUST | KD-19 |
| FR-DA-014 | Mark assignment algorithm uses displacement-based cost selection with EntityId terminal tie-break | MUST | §3.4 / KD-11 |
| FR-DA-015 | Assignment transitions use dwell-time hysteresis binding to #2 §3.1 | MUST | KD-11 / §3.11 |
| FR-DA-016 | Man-mark target is the opponent within `MAN_MARK_CANDIDATE_RADIUS_M [GT]` with highest threat score | MUST | §3.3 |
| FR-DA-017 | Threat score is `perceivedGoalProximity × receiverAttribute` (cite-not-redefine #7 §3.7–§3.10) | MUST | §3.5 / KD-1 |
| FR-DA-018 | Offside trap fires when ball velocity below `OFFSIDE_BALL_SPEED_THRESHOLD_M_S [GT]` AND all triggers met | MUST | §3.7 / KD-9 |
| FR-DA-019 | Offside trap: all DEFENSE-line agents advance to `offsideStepDepth` simultaneously on the same tick | MUST | §3.7 |
| FR-DA-020 | Offside-line adjudication is out of scope; #14 places defenders only | MUST | KD-9 |
| FR-DA-021 | Last-man predicate is computed deterministically per KD-12 | MUST | KD-12 / §3.8 |
| FR-DA-022 | Emergency override: when last-man threat fires, override that agent to INTERCEPT_RUNNER | MUST | §3.9 |
| FR-DA-023 | Tackle intent: #14 evaluates COMMIT / JOCKEY / HOLD per eligible agent; `TackleIntentRequest` produced | MUST | §3.6 / KD-6 |
| FR-DA-024 | Anti-chaos invariants are checked BEFORE directive is published | MUST | KD-17 |
| FR-DA-025 | Anti-chaos: min backline agents (LineMembership = DEFENSE) = `MIN_BACKLINE_AGENTS [GT]` | MUST | KD-17 |
| FR-DA-026 | Anti-chaos: max MAN_MARK assignments = `MAX_MAN_MARK_ASSIGNMENTS [GT]` | MUST | KD-17 |
| FR-DA-027 | Anti-chaos: max mark displacement from #12 anchor = `MAX_MARK_DISPLACEMENT_M [GT]` | MUST | KD-17 |
| FR-DA-028 | Anti-chaos violation demotes the costliest override to ZONAL and re-checks until clean | MUST | KD-17 / §3.10 |
| FR-DA-029 | Failure mode F1: stale perception → freeze previous tick directive | MUST | §2.4 |
| FR-DA-030 | Failure mode F2: #12 slot unavailable → emit all-ZONAL directive | MUST | §2.4 |
| FR-DA-031 | Failure mode F3: #13 directive unavailable → treat all agents as HOLD_SHAPE | MUST | §2.4 |
| FR-DA-032 | Failure mode F4: anti-chaos invariant violation at publication → fall back to all-ZONAL for this tick | MUST | §2.4 / KD-17 |
| FR-DA-033 | Failure mode F5: last-man predicate produces tie with no EntityId ordering → use ascending EntityId | MUST | §2.4 / KD-12 |
| FR-DA-034 | All formulas have units, valid input ranges, and at least one worked example in §3 or Appendix | MUST | CLAUDE.md |
| FR-DA-035 | All constants tagged per KD-13 | MUST | KD-13 |
| FR-DA-036 | No interface produced against unspecified consumer spec #15 at Stage 0 | MUST | CLAUDE.md / KD-8 |
| FR-DA-037 | Stage-1 activation gated on: (a) #8 ERR-014-001 amendment ratified; (b) #12 APPROVED; (c) ERR-014-002/003 #17 channel rows landed | MUST | KD-16 / §7 |

### 2.2 Data Structures

| Struct | Purpose | Stage |
|---|---|---|
| `MarkDirective` | Per-team per-tick: team `EntityId`, offensive-line depth (float), offside-trap active flag, step-up target depth, emergency flag | 1 (spec'd at 0) |
| `MarkAssignment` | Per-agent per-tick: mode enum (ZONAL / MAN_MARK / INTERCEPT_RUNNER / COVER_GK_ZONE), target `EntityId?`, target position `Vector2?`, valid-through-tick | 1 (spec'd at 0) |
| `TackleIntentRequest` | Per-agent: intent mode (COMMIT / JOCKEY / HOLD), target `EntityId`, approach angle (float), coverage depth (u8) | 1 (spec'd at 0) |
| `MarkHysteresisState` | Per-agent: dwell counter for assignment transitions; authoritative state digested per KD-10 | 1 |
| `OffsideLineState` | Per-team: current line depth, step-up dwell counter, cooldown ticks remaining | 1 |
| `BaselineDefensiveShapeView` | Read-only view of #12 `BaselineDefensiveShape` used at tick start | 1 |
| `MarkDirectiveSnapshot` | Read-only view for #17 channel emission and tests | 1 |

### 2.3 Inputs (read-only at tick start)
- Perception snapshot (#7 §3.7): agent positions, ball position, possession
  state, attribute lookups (`Vision`, `Anticipation`, `Tackling`).
- #12 baseline `formationSlot` per agent (via `BaselineDefensiveShapeView`).
- #12 `DefensiveLineDepth` (team-instruction-set value).
- #12 phase enum for this team.
- #13 `PressAssignment` per agent (role partition — reads HOLD_SHAPE pool).

### 2.4 Failure Modes (F1–F5 above)
Section files enumerate each with: detection condition, recovery action,
test reference.

### 2.5 Version History

---

## SECTION 3 — CORE FORMULAS AND ALGORITHMS

### 3.1 Phase gating (binding to #12 §3.0 per KD-19)
Read #12 phase for this team. If `IN_POSSESSION`: emit all-ZONAL directive;
return. If `OUT_OF_POSSESSION` or `TRANSITION`: proceed to §3.2.

### 3.2 HOLD_SHAPE pool filtering (binding to #13 KD-4)
Read #13 `PressAssignment` per agent. Build HOLD_SHAPE pool = {agents where
role == HOLD_SHAPE AND EntityId ≠ GK}. This is the eligible pool for §3.3.
Worked example: 11-agent team, 1 GK + 1 PRIMARY_PRESS + 2 COVER_SHADOW =
7 agents in HOLD_SHAPE pool.

### 3.3 Mark-mode assignment algorithm
For each agent in HOLD_SHAPE pool (EntityId-ascending order):
1. Check if agent is already in a non-ZONAL mode with a live target —
   if dwell counter not expired, retain current assignment (§3.11).
2. Else, evaluate candidate mark targets:
   - MAN_MARK candidates: opponents within `MAN_MARK_CANDIDATE_RADIUS_M [GT]`
     and visible per #7 perception.
   - INTERCEPT_RUNNER candidates: opponents on a run trajectory (velocity
     magnitude > `RUNNER_VELOCITY_THRESHOLD_M_S [GT]`) pointing toward
     own-half.
3. Score candidates via threat function (§3.5).
4. Assign the agent the highest-threat candidate mode and target.
5. Apply anti-chaos checks (§3.10) before emitting.

### 3.4 Displacement cost function
`cost(agent, targetPos) = |agent.position - targetPos|²`

Used for tie-breaking within a given mode. Lower cost preferred. EntityId
tie-break if costs equal.

Worked example: agent at (30, 25), target at (40, 30) → cost =
(10² + 5²) = 125. Units: m².

### 3.5 Threat score
`threat(opponent) = perceivedGoalProximity(opponent) × opponentFirstTouch(opponent)`

Where:
- `perceivedGoalProximity` = 1.0 − (distance(opponent, goalCenter) /
  `PITCH_LENGTH_M [CROSS: #1 §1.2]`). Clamped to [0, 1].
- `opponentFirstTouch` = #7-perceived `FirstTouch` attribute of the
  opponent, normalised to [0, 1] from the player attribute scale.

All inputs from perception snapshot (#7 §3.7–§3.10) — no direct attribute
reads by #14 (cite-not-redefine #7). Worked example provided at section-file
authoring (requires concrete attribute scale from #2 §3.x).

### 3.6 Tackle intent evaluation
For each HOLD_SHAPE agent whose assigned opponent is within
`TACKLE_ELIGIBLE_RADIUS_M [GT]`:

```
approachAngle = angle between (agent→opponent vector) and (agent velocity)
coverageDepth = count(teammates between agent and own goal, x within COVERAGE_DEPTH_CORRIDOR_M [GT])

if coverageDepth >= TACKLE_COMMIT_COVERAGE_FLOOR [GT]:
    mode = COMMIT        // other defenders can recover; risk acceptable
elif approachAngle < TACKLE_JOCKEY_ANGLE_RAD [GT]:
    mode = JOCKEY        // poor angle; shadow without committing
else:
    mode = HOLD          // insufficient coverage and poor angle
```

Emit `TackleIntentRequest` for the agent. Worked example at section-file
authoring.

### 3.7 Offside trap algorithm (resolves outline.md H-4 KD-9)

**Trigger conditions (all must hold for `OFFSIDE_TRAP_DWELL_TICKS [GT]`
consecutive ticks):**
1. Ball velocity < `OFFSIDE_BALL_SPEED_THRESHOLD_M_S [GT]` (ball is slow
   or stopped — reduces risk of through-ball catching step-up).
2. Ball-carrier position in opponent half (ball.x > `HALF_LINE_X [CROSS: #1 §1.2]`).
3. All DEFENSE-line agents are within `LINE_COHERENCE_THRESHOLD_M [GT]`
   x-spread of each other (line is compact enough to step as a unit).
4. #13 is not in an active press with PRIMARY_PRESS assigned (stepping
   the line behind an active press creates exposure — blocked by
   `OFFSIDE_PRESS_BLOCK_FLAG`).

**Execution (when trigger fires):**
1. Compute `offsideStepDepth`:
   `offsideStepDepth = max(currentLineDepth + OFFSIDE_STEP_SIZE_M [GT],`
   `    #12.DefensiveLineDepth)`
   Never steps past `OFFSIDE_MAX_DEPTH_M [GT]` (safety ceiling).
2. Issue `MarkAssignment` with mode ZONAL + `targetPosition.x = offsideStepDepth`
   for all DEFENSE-line agents simultaneously on the same tick (FR-DA-019).
3. Set `offsideTrapActive = true` in `MarkDirective`.
4. Start `OFFSIDE_RESET_COOLDOWN_TICKS [GT]` cooldown after step fires.
   Re-fire blocked during cooldown.

Worked example at section-file authoring.

### 3.8 Last-man predicate (KD-12)
Compute `lastManAgent` per KD-12 definition. If `IsLastManThreat(ballPos, lastManAgent)`:
1. Set `emergencyFlag = true` in `MarkDirective`.
2. Override `lastManAgent.MarkAssignment` to `INTERCEPT_RUNNER` targeting
   the advancing attacker.
3. If `IsLastManThreat` AND `coverageDepth == 0` (no teammate behind):
   `TackleIntentRequest.mode = JOCKEY` (never commit when truly last man
   with no cover — prevents "last man sent off" scenario). Cite: KD-6.

### 3.9 Emergency COVER_GK_ZONE override
If GK x-position is outside `[GK_EXPECTED_ZONE_MIN_X [GT], GK_EXPECTED_ZONE_MAX_X [GT]]`
(from #7 perception) AND `IsLastManThreat` is active:
1. Find agent in DEFENSE-line with minimum displacement cost to the GK's
   abandoned zone.
2. Issue `COVER_GK_ZONE` assignment override for that agent.
3. Retain this override until GK returns to expected zone OR `COVER_GK_ZONE_MAX_TICKS [GT]`
   ticks elapse (hysteresis prevents flicker).

### 3.10 Anti-chaos invariant enforcement (KD-17)
Apply invariants in order (post-assignment, pre-publication):
1. Count DEFENSE-line agents; if < `MIN_BACKLINE_AGENTS`, demote the most
   recently assigned non-backline MAN_MARK to ZONAL. Re-check.
2. Count MAN_MARK assignments; if > `MAX_MAN_MARK_ASSIGNMENTS`, demote the
   lowest-threat MAN_MARK to ZONAL. Re-check.
3. For each non-ZONAL assignment: if displacement > `MAX_MARK_DISPLACEMENT_M`,
   demote to ZONAL. Re-check.
4. If any invariant still violated after 3 passes: fall back to all-ZONAL
   (FR-DA-032).

### 3.11 Assignment hysteresis (KD-11, binding to #2 §3.1)
A mode/target transition fires only after the new candidate has been preferred
for `MARK_DWELL_TICKS [EST]` consecutive ticks. Prevents assignment-thrash
when two candidates swap cost ranking between ticks.

### 3.12 Constants catalogue (forward ref to §6.1)
All thresholds, counts, depths. Tagged per KD-13 (all `[EST]` at outline
stage; promoted to `[GT]` at section-file authoring with Appendix A
derivations).

### 3.13 Pseudocode (per-tick main loop)
```
1.  Read perception snapshot, #12 slots/phase/lineDepth, #13 PressAssignment.
2.  Gate on phase (§3.1): if IN_POSSESSION → emit all-ZONAL; return.
3.  Build HOLD_SHAPE pool (§3.2).
4.  Compute last-man predicate (§3.8) — done FIRST; emergency takes priority.
5.  If emergency: override lastMan assignment → INTERCEPT_RUNNER (§3.8).
    If GK out-of-pos AND emergency: add COVER_GK_ZONE (§3.9).
6.  For each remaining HOLD_SHAPE agent (EntityId-ascending):
        Check hysteresis (§3.11) — retain if dwell valid.
        Else: run mark assignment (§3.3), score threats (§3.5).
7.  Evaluate tackle intent for eligible agents (§3.6).
8.  Check offside trap trigger (§3.7); if trigger met and not on cooldown:
        Execute step-up; set offsideTrapActive.
9.  Enforce anti-chaos invariants (§3.10); fall back if needed (FR-DA-032).
10. Publish MarkDirective + per-agent MarkAssignment + TackleIntentRequests.
```

### 3.14 Version History

---

## SECTION 4 — ARCHITECTURE, FILE LAYOUT, INTERFACE CONTRACTS

### 4.1 Architecture Overview
Single subsystem `DefensiveAI` on the 10 Hz scheduler. Pure-function design
except for hysteresis state, offside-line state, and tackle-intent state
(authoritative; digested). **Runtime activates at Stage 1** — Stage 0 ships
the spec, not the code.

### 4.2 File Structure (#20 §4.2 compliant — single catalogue)
```
src/DefensiveAI/                                     (Stage 1+)
├── DefensiveAITick.cs           (10 Hz entry point)
├── HoldShapePoolFilter.cs       (§3.2 — reads #13 roles)
├── MarkAssigner.cs              (§3.3 + §3.4 + §3.5 assignment algorithm)
├── TackleIntentEvaluator.cs     (§3.6)
├── OffsideTrapController.cs     (§3.7; line-state is authoritative)
├── LastManDetector.cs           (§3.8 + §3.9)
├── InvariantEnforcer.cs         (§3.10 + KD-17)
├── MarkHysteresis.cs            (§3.11; dwell state is authoritative)
└── DefensiveAIConstants.cs      (SINGLE catalogue per FR-CS-025 / KD-14)
```

### 4.3 Internal Module Contracts
Module-by-module input/output declared as `readonly struct` parameters.
No `class` types on hot path per #18 §3.7 zero-alloc rule.

### 4.4 Upstream Integration Contracts
- Perception snapshot read (#7 §3.7) at tick start.
- #12 `BaselineDefensiveShape` view (Stage 1+).
- #12 `DefensiveLineDepth` field (Stage 1+).
- #12 phase accessor (`PositioningAI.GetPhase`).
- #13 `PressAssignment` per agent (`PressingAI.GetAssignment(EntityId)`).

### 4.5 Downstream Integration Contracts
- `MarkDirective` / `MarkAssignment` consumed by orchestrator, which
  (a) composes with #12 baseline and #13 press overrides before #8 reads,
  and (b) makes available to #8 §3.1.7 via KD-5 mechanism.
- `TackleIntentRequest` per eligible agent consumed by #8 for dispatch to #3.
- `MarkDirectiveSnapshot` read-only view for #17 event emission and tests.

#### 4.5.1 To #15 Attacking AI (Stage 1+ — declared, not implemented)
#15 may consume the `MarkDirective.emergencyFlag` as a signal that a
goal-risk situation exists, informing any transition-phase recovery behavior.
No interface authored at Stage 0 (Interface Design Principle).

### 4.6 Determinism & Safety Boundaries (binding to #16)
Iteration order: EntityId-ascending per #16 §3.2.5. RNG domain tag
`DOMAIN_TAG_DEFENSIVE_AI = 0x1A [CROSS-PENDING]`. Digest scope: directives,
assignments, hysteresis dwell counters, offside-line state, tackle-intent
state. All are authoritative simulation state per #16 §3.2.

### 4.7 Cross-Specification Validation Checks
- No GK in assignment pool (KD-7).
- No overlap with #13-assigned agents (KD-4; enforced per FR-DA-010).
- Fatigue convention (KD-1).
- EntityId no-reuse cited (#2 §2.5 / #8 §1.7.3).
- Offside adjudication excluded from spec text (KD-9 compliance grep).

### 4.8 Version History

---

## SECTION 5 — TEST PLAN

### 5.1 Test Counts (verifiable target)

| Category | Target | Source |
|---|---|---|
| Unit (pool filter, assignment algorithm, threat scoring, tackle intent, offside trap, last-man, hysteresis, invariant enforcement) | ≥48 | §3.2–§3.11 |
| Integration (full-team defense under each phase × mode combination) | ≥12 | §3.13 |
| Determinism regression | ≥6 | #16 §5 |
| Performance | ≥3 | §6 |
| Anti-chaos invariant tests | ≥6 | KD-17 |
| Exploit-resistance (KD-18 corpus) | ≥4 | KD-18 |
| **Total** | **≥79** | — |

### 5.2 Unit Test List (representative)
- HOLD_SHAPE pool excludes GK and all non-HOLD_SHAPE #13 roles.
- Phase gate: IN_POSSESSION produces all-ZONAL directive.
- Mark assignment: highest-threat candidate is selected.
- Threat score: perceivedGoalProximity and attribute factors combine correctly.
- Displacement cost tie-break uses EntityId-ascending.
- Tackle intent: COMMIT fires when coverageDepth ≥ floor.
- Tackle intent: JOCKEY fires when approach angle < threshold, coverage low.
- Offside trap: fires only when all four trigger conditions hold for
  `OFFSIDE_TRAP_DWELL_TICKS` consecutive ticks.
- Offside trap: all DEFENSE-line agents receive step-up assignment in same tick.
- Offside trap: blocked during cooldown ticks.
- Last-man: correct agent identified; tie broken by EntityId-ascending.
- Last-man: emergency override issues INTERCEPT_RUNNER.
- COVER_GK_ZONE: issued when GK out-of-position AND emergency active.
- Each of F1–F5 has a dedicated test.
- Hysteresis: oscillating candidate stays in original assignment for
  ≥ `MARK_DWELL_TICKS`.

### 5.3 Integration Test List
- Full-team press + defend scenario: #13-assigned agents absent from #14
  pool; #14 correctly manages the remaining HOLD_SHAPE agents.
- Phase transition out→in→out: directive correctly suppresses on IN_POSSESSION
  and resumes on OUT_OF_POSSESSION.
- Possession turnover (transition phase): correct emergency/normal behavior
  per tick.

### 5.4 Determinism Regression (binding to #16 §5)
- 90-minute match replay produces bit-identical per-tick digest on reference
  host.

### 5.5 Performance Validation (binding to §6)
Per-tick budget measured against named host (§6.3).

### 5.6 Anti-Chaos & Exploit-Resistance Scenarios (resolves outline.md L-13)
- KD-17 invariants: one test per invariant asserting violation causes
  demotion cascade and eventual all-ZONAL fallback.
- KD-18 corpus: `EXPLOIT_OFFSIDE_TRAP_SPRUNG_EARLY`,
  `EXPLOIT_SWITCH_THROUGH_HOLE`, `EXPLOIT_LAST_MAN_ONE_ON_ONE`,
  `EXPLOIT_GK_OUT_OF_POSITION`.

### 5.7 xG-Surrogate Validation (resolves outline.md L-13)
xG modeling is Stage 1+ per Shot Mechanics #6 §7. Stage-0 surrogate metric:
**shots-in-box conceded per match and average shot distance from goal**.
Both are computable from #1 ball position + #3 contact events without a
full xG model. Acceptance criterion: the surrogate metrics must be
measurable from the simulation trace (not validated in Stage 0 code, but
the spec must declare the measurement method so it is unambiguous at Stage 1).

### 5.8 Version History

---

## SECTION 6 — PERFORMANCE ANALYSIS AND BUDGETS

### 6.1 Constant Catalogue
Full enumeration with tags. All `[EST]` at outline stage; promoted to `[GT]`
at section-file authoring with Appendix A derivations.

| Constant | Tag | Proposed value | Purpose |
|---|---|---|---|
| `MAN_MARK_CANDIDATE_RADIUS_M` | `[GT]` | 15.0 m | Radius within which opponents are candidate man-mark targets |
| `RUNNER_VELOCITY_THRESHOLD_M_S` | `[GT]` | 3.0 m/s | Minimum opponent velocity to be classified as INTERCEPT_RUNNER target |
| `LAST_MAN_BALL_BUFFER_M` | `[GT]` | 5.0 m | Ball-ahead buffer for last-man threat firing |
| `LAST_MAN_OWN_HALF_MIN_X` | `[GT]` | 5.0 m | Minimum distance from own goal for last-man threat |
| `OFFSIDE_BALL_SPEED_THRESHOLD_M_S` | `[GT]` | 4.0 m/s | Ball speed ceiling for offside trap trigger |
| `OFFSIDE_STEP_SIZE_M` | `[GT]` | 3.0 m | Forward advancement per trap execution |
| `OFFSIDE_MAX_DEPTH_M` | `[GT]` | 45.0 m | Maximum depth the line may advance via trap |
| `OFFSIDE_TRAP_DWELL_TICKS` | `[EST]` | 3 | Dwell ticks before trap fires |
| `OFFSIDE_RESET_COOLDOWN_TICKS` | `[GT]` | 10 | Cooldown ticks after trap fires |
| `LINE_COHERENCE_THRESHOLD_M` | `[GT]` | 8.0 m | Max x-spread of DEFENSE-line for trap eligibility |
| `MARK_DWELL_TICKS` | `[EST]` | 4 | Dwell ticks before mark mode/target transition |
| `TACKLE_ELIGIBLE_RADIUS_M` | `[GT]` | 3.0 m | Radius within which tackle intent is evaluated |
| `TACKLE_COMMIT_COVERAGE_FLOOR` | `[GT]` | 1 | Min teammates behind before COMMIT allowed |
| `TACKLE_JOCKEY_ANGLE_RAD` | `[GT]` | 0.35 rad (~20°) | Angle below which JOCKEY is preferred over HOLD |
| `COVERAGE_DEPTH_CORRIDOR_M` | `[GT]` | 5.0 m | Y-corridor half-width for counting coverage teammates |
| `MIN_BACKLINE_AGENTS` | `[GT]` | 3 | Anti-chaos: min DEFENSE-line agents |
| `MAX_MAN_MARK_ASSIGNMENTS` | `[GT]` | 4 | Anti-chaos: max simultaneous MAN_MARK |
| `MAX_MARK_DISPLACEMENT_M` | `[GT]` | 20.0 m | Anti-chaos: max displacement from #12 anchor |
| `GK_EXPECTED_ZONE_MIN_X` | `[GT]` | −2.0 m (relative to goal line) | GK expected zone lower bound |
| `GK_EXPECTED_ZONE_MAX_X` | `[GT]` | 15.0 m (relative to goal line) | GK expected zone upper bound |
| `COVER_GK_ZONE_MAX_TICKS` | `[GT]` | 20 | Max ticks of COVER_GK_ZONE before forced release |
| `REASSIGN_LATENCY_TICKS` | `[GT]` | 2 | Max ticks before unassigned zone is covered |
| `PITCH_LENGTH_M` | `[CROSS: #1 §1.2]` | 105.0 m | X-axis pitch length |
| `HALF_LINE_X` | `[CROSS: #1 §1.2]` | 52.5 m | Midfield line x-coordinate |

### 6.2 Hot Path Enumeration (#18 KD-10 binding)
Main per-tick loop:
- HOLD_SHAPE pool filter: O(N), N ≤ 22.
- Mark assignment: O(N × M), N = HOLD_SHAPE agents (≤11), M = candidate
  opponents (bounded by `MAN_MARK_CANDIDATE_RADIUS_M`). Worst case:
  10 agents × 10 candidates = 100 score evaluations.
- Offside trap trigger: O(1) aggregate check over DEFENSE-line (≤5 agents).
- Anti-chaos enforcement: O(N) up to 3 passes; worst case 33 iterations.
- Tackle intent: O(K), K = agents within `TACKLE_ELIGIBLE_RADIUS_M` (≤4
  expected in normal play).

**Total worst-case evaluations per tick: ≈150. Bounded by O(N × M).**

### 6.3 Per-Tick Budget (reference host per #12 KD-15)
Target: ≤0.12 ms per 10 Hz tick on the named reference host (Ryzen 7 5800X
@ 4.5 GHz, single thread, Mono backend, Unity 2022.3 LTS). Budget is
slightly higher than #13 (≤0.10 ms) due to the additional offside-trap
computation and tackle-intent evaluation pass. Caveat: cert host budget
supersedes once `certification-platform.md` is pinned by lead developer.

### 6.4 Per-Frame Budget
N/A — no per-frame work.

### 6.5 Memory Footprint
`MarkDirective` + `MarkAssignment[22]` + `MarkHysteresisState[22]` +
`TackleIntentRequest[22]` + `OffsideLineState` ≈ <2 KB.

### 6.6 Version History

---

## SECTION 7 — FUTURE EXTENSIONS

### 7.1 Stage 1 — Runtime activation (KD-16)
Implementation lands once: (a) #8 ratifies ERR-014-001 amendment for the #14
read surface; (b) #12 reaches APPROVED; (c) #17 channel rows land via
ERR-014-002 / ERR-014-003.

### 7.2 Stage 1+ — Mark mode: MAN_MARKING individual instructions
Per-opponent man-marking instructions from team tactics screen; overrides
the default threat-based assignment.

### 7.3 Stage 1+ — `MARK_ASSIGNED` / `LINE_STEPPED` channels
Atomic back-prop into #17 §3.10 via ERR-014-002 / ERR-014-003.

### 7.4 Stage 1+ — #15 Attacking AI emergency signal
Expose `MarkDirective.emergencyFlag` to orchestrator for #15's
transition-recovery behavior.

### 7.5 Stage 2+ — Set-piece defensive wall formation
When team is defending a free-kick: #14 selects wall members, computes wall
depth and spacing. Requires set-piece event infrastructure (planned Stage 2+;
binding to #11 GK wall-request surface).

### 7.6 Stage 2+ — Tactical instructions overlay
High-line, low-block, mid-block as named defensive styles selectable per
match via team-instruction infrastructure.

### 7.7 Stage 2+ — ML-tuned `[GT]` parameter fitting
Threat-score weights, hysteresis constants, offside-trap thresholds as
ML-fit parameters.

### 7.8 Stage 2+ — Per-archetype defensive profiles
e.g., 4-3-3 pressing-high default, 5-4-1 deep block default.

### 7.9 Stage 5+ — Fixed64 migration per #9.

### 7.10 Stage 5+ — Cross-platform determinism per #9.

---

## SECTION 8 — REFERENCES AND CITATIONS

### 8.1 Cross-Spec References (grep-verified at section-file draft time)
- #1 §1.2, Appendix C (corner-origin coordinates; `PITCH_LENGTH_M`;
  `HALF_LINE_X`)
- #2 §2.5 (XC-002-001 — EntityId no-reuse); §3.1 (hysteresis pattern)
- #3 §3.x (tackle contact physics surface; exact subsection grep at
  section-file draft)
- #7 §3.7–§3.10 (perception schema; attribute lookups)
- #8 §1.3.2 (Stage 1+ deferral text for #14); §1.7.3 (XC-008-001);
  §2.2.6 (amendment process — ERR-014-001); §3.1.7 (MOVE_TO_POSITION
  utility; #14 advises at Stage 1); §3.1.9 (INTERCEPT utility)
- #11 §1.4 (defensive wall ownership table); FR-GK-016 (wall out of scope
  for #11)
- #12 §3.7 (formation slot output); §4.5.2 (`BaselineDefensiveShape`
  read-only view; `GetLine` accessor); §2.2 (`DefensiveLineDepth` field)
- #13 §2.2 (FR-PR-014 disjoint partition); §4.5 (`PressAssignment` accessor);
  §7.4 (#14 handoff binding slot)
- #16 §3.2, §3.2.5, §3.4 (ERR-014-001 domain-tag allocation); §5; §6.2
- #17 §3.10 channel registry (Stage 1 back-prop via ERR-014-002 /
  ERR-014-003); Appendix (`0x18…0x1B` reserved block for #14)
- #18 §3.7 (zero-alloc hot path); §6
- #19 §3, §4
- #20 §4.2 (FR-CS-025)

### 8.2 CLAUDE.md Invariants Bound
Corner-origin coordinate system, fatigue convention, tick rates, zero-alloc
hot path, constant-tag policy, Interface Design Principle (explains why #15
handoff is deferred and why no #8 amendment text lives in this spec).

### 8.3 Typed Cross-Reference IDs
- `XC-014-NNN` — allocated at section-file draft for each upstream citation
  that crosses a boundary.
- `ERR-014-001` — back-prop to #8 §3.1.7 and/or §2.2.6 to add a read of
  #14's `MarkAssignment`. Mechanism chosen at section-file draft (accessor
  vs. `TacticalContext` extension). Filed at section-file draft.
- `ERR-014-002` — back-prop to #17 §3.10 to register `MARK_ASSIGNED`
  channel within `0x18…0x1B` block. Filed at Stage 1.
- `ERR-014-003` — back-prop to #17 §3.10 to register `LINE_STEPPED`
  channel within `0x18…0x1B` block. Filed at Stage 1.
- `ERR-014-004` — back-prop to #16 §3.4 to allocate
  `DOMAIN_TAG_DEFENSIVE_AI = 0x1A` within the ERR-012-001 Phase B/C block.
  Filed at section-file draft. (Note: uses ERR-014-004 not ERR-014-001 to
  avoid collision with the #8 back-prop number. Domain tag back-prop to #16
  follows the precedent of ERR-010-001 / ERR-013-005.)
- Inherits `0x1A` slot from ERR-012-001 Phase B/C block proposal
  (`0x17`=#12, `0x18`=#11, `0x19`=#13, `0x1A`=#14, `0x1B`=#15).

### 8.4 Academic / External References
No direct academic references expected in §8.3 at Stage 0 (defensive AI
algorithms are self-contained from the spec's upstream surfaces). If
published research on defensive shape models is cited in section files,
DOIs must be verified before sign-off (per ERR-005 fabricated-reference
precedent from Heading #10 v0.1).

### 8.5 Version History

---

## SECTION 9 — APPROVAL CHECKLIST

### 9.1 Self-Contained Spec Content
- All 14 outline.md findings resolved (mapping in §9.4).
- All 37 FRs cross-referenced.
- All constants tagged (`[EST]` for outline-stage placeholders; promoted
  to `[GT]` at section-file authoring per KD-13).
- All cross-spec citations grep-verified at section-file draft time.
- Stage-binding statement (§1.8) makes Stage-0/Stage-1 split unambiguous.

### 9.2 Cross-Spec Sign-Offs Required
- #16 lead-developer approval of `DOMAIN_TAG_DEFENSIVE_AI = 0x1A` (via
  ERR-014-004 back-prop to #16 §3.4).
- #8 owner ack of the read-surface mechanism chosen in KD-5 (accessor vs.
  `TacticalContext` extension) — ratification of ERR-014-001 amendment text.
- #12 owner ack of `BaselineDefensiveShape` / `GetLine` accessor contract
  per #12 §4.5.2.
- #13 owner ack of HOLD_SHAPE handoff protocol per KD-4.

### 9.3 KD-Sequencing Preconditions
- (a) `ERR-014-001` mechanism choice ratified by lead developer.
- (b) Domain-tag allocation `0x1A` resolved via ERR-014-004.
- (c) All `[CROSS-PENDING]` tags promoted to `[CROSS]`.
- (d) Hysteresis and assignment `[EST]` constants promoted to `[GT]` with
  Appendix A derivations.
- (e) #3 tackle-physics subsection grep-verified (§8.1).
- (f) #12 `BaselineDefensiveShape` and `GetLine` accessor confirmed at
  section-file draft.
- (g) Lead-developer R-01..R-05 review pass.

### 9.4 Finding-to-Resolution Map

| Review | Finding | Sev | Resolved by |
|---|---|---|---|
| outline.md | 1. Missing metadata header | H | "Metadata Header" section |
| outline.md | 2. Section plan misaligned with CLAUDE.md template | H | §1–§9 mapping per template; "Stage-Binding Clarification" |
| outline.md | 3. Tackle ownership ambiguity | H | KD-6 (intent vs. physics split) + §3.6 `TackleIntentRequest` + §4.5 |
| outline.md | 4. Offside-line ownership undefined | H | KD-9 (step-up = #14; adjudication = future referee spec) + §3.7 + §1.2 |
| outline.md | 5. Boundary with #13 and #12 unstated | H | KD-3 (#12 boundary) + KD-4 (#13 handoff disjoint partition) + Boundary Matrix |
| outline.md | 6. Last-man computation undefined | H | KD-12 (formal predicate with formula, tie-break, GK exclusion) + §3.8 |
| outline.md | 7. Determinism plan absent | M | KD-10 + §3.13 EntityId iteration + §4.6 digest scope |
| outline.md | 8. Coordinate convention unmentioned | M | KD-1 cite-not-redefine + §1.7 full coordinate binding |
| outline.md | 9. Tick-rate split unstated | M | KD-2 + §1.7 (10 Hz tactical; 60 Hz physics) |
| outline.md | 10. Fatigue convention not pre-committed | M | KD-1 + FR-DA-008 + §1.7 explicit binding |
| outline.md | 11. No event production declared | M | KD-15 (`MARK_ASSIGNED` / `LINE_STEPPED` deferred to Stage 1; ERR-014-002/003) |
| outline.md | 12. Constant-tag policy not invoked | M | KD-13 + §6.1 full catalogue with tags |
| outline.md | 13. Chance-prevention lacks acceptance criteria | L | §5.7 Stage-0 xG surrogate (shots-in-box + avg shot distance) |
| outline.md | 14. No GK interaction model | L | KD-7 (wall ownership + COVER_GK_ZONE protocol) + §3.9 |

### 9.5 Lead-Developer Sign-Off Lines (R-01..R-05)

### 9.6 Version History

---

## APPENDICES

### Appendix A — Derivations
Hysteresis dwell-time derivations (binding to #2 §3.1), threat-score formula
derivation, offside-step-size justification, anti-chaos invariant
floor/ceiling rationale. Each `[EST]` constant gets one entry here when
promoted to `[GT]`.

### Appendix B — Last-Man Predicate Reference Card
Formal definition, normalisation to "distance-to-own-goal" scalar, worked
example for each team orientation (defending x=0 vs. x=105), GK-exclusion
rule, deterministic tie-break. Three canonical test inputs: (1) single clear
last man; (2) two defenders equidistant (EntityId tie-break); (3) GK
further forward than all outfield defenders (GK excluded; correct last man
identified).

### Appendix C — Offside Trap Algorithm Verification
Four canonical trigger inputs: (1) all conditions met → trap fires; (2) ball
too fast → trap blocked; (3) line not coherent → trap blocked; (4) active
press → trap blocked. Each with worked tick-by-tick trace.

### Appendix D — Anti-Chaos Sensitivity Analysis
Per-invariant sensitivity sweeps for `MIN_BACKLINE_AGENTS`,
`MAX_MAN_MARK_ASSIGNMENTS`, `MAX_MARK_DISPLACEMENT_M`.

### Appendix E — Exploit-Resistance Playbook (KD-18 corpus)
Offside trap sprung early, switch through hole, last-man one-on-one, GK
out-of-position. Each with input scenario, expected directive evolution, and
pass criterion.

### Appendix F — Glossary
Mark mode (ZONAL / MAN_MARK / INTERCEPT_RUNNER / COVER_GK_ZONE), mark
directive, mark assignment, HOLD_SHAPE pool, offside trap, last-man predicate,
tackle intent (COMMIT / JOCKEY / HOLD), anti-chaos invariant, assignment
hysteresis, line step-up, emergency override, coverage depth.

### Appendix G — Telemetry & Troubleshooting Playbook
Stage 1+ debug overlays for mark assignments, offside-line position, and
tackle-intent state. Stage 0 placeholder only.

---

## OUTSTANDING OUTLINE-PHASE QUESTIONS

The following will be resolved during section-file authoring:

### Q1 — KD-5 mechanism selection
Accessor (`DefensiveAI.GetMarkAssignment(EntityId)`) vs.
`TacticalContext.MarkDirective` field extension via #8 §2.2.6 amendment.
Same decision pattern as KD-3 in #13 outline. Decision deferred to
section-file draft.

### Q2 — #3 tackle-physics subsection
Exact subsection of #3 where `TackleIntentRequest` / contact-physics surface
lives. Grep required at section-file draft.

### Q3 — Threat score attribute normalisation
The `opponentFirstTouch` attribute normalisation range depends on #2 §3.x
player attribute scale. Grep `agent-movement/section-3.md` for the attribute
range definition at section-file draft.

### Q4 — GK expected zone coordinates
`GK_EXPECTED_ZONE_MIN_X` and `GK_EXPECTED_ZONE_MAX_X` are in goal-relative
coordinates. Need to confirm how #7 Perception exposes the GK's expected
position (may be implicit from #11's `GK_STARTING_X` or equivalent
constant). Grep `goalkeeper-mechanics/section-3.md` at section-file draft.

### Q5 — Domain-tag number confirmation
`DOMAIN_TAG_DEFENSIVE_AI = 0x1A` is proposed based on the ERR-012-001 Phase
B/C block (`0x17=#12, 0x18=#11, 0x19=#13, 0x1A=#14, 0x1B=#15`). If either
#11 or #12 reaches APPROVED before #14's section-file draft and claims a
different slot, the block may shift. Verify against `deterministic-sim/section-3.md`
at section-file draft; update ERR-014-004 accordingly.

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
14. Flip `SPEC_INDEX.md` row 14 `NOT STARTED → IN REVIEW`.
15. Lead-developer R-01..R-05 sign-off.

---

## VERSION HISTORY

| Version | Date | Author | Summary |
|---|---|---|---|
| 1.0 | May 17, 2026 | AI agent (claude/defensive-ai-specs-outline-w8nP3) | Initial detailed outline. Resolves all 14 findings from the May 6, 2026 adversarial review at the bottom of `outline.md` (6 H / 6 M / 2 L). Establishes 19 cross-cutting design decisions (KD-1..KD-19). Stage-binding clarification anchors #14 runtime to Stage 1 per #8 §1.3.2 — Stage 0 deliverable is spec text only. 37 FRs enumerated. 4 mark modes (ZONAL / MAN_MARK / INTERCEPT_RUNNER / COVER_GK_ZONE). KD-4 disjoint handoff contract with #13 Pressing AI. KD-6 tackle-intent split with #3 Collision System. KD-9 offside-line ownership: step-up decision owned by #14; adjudication out of scope. KD-12 deterministic last-man predicate with EntityId tie-break and GK exclusion. 3 anti-chaos invariants (KD-17) + 4-exploit test corpus (KD-18). ERR-014-001..004 back-prop requests pre-filed. Domain-tag `DOMAIN_TAG_DEFENSIVE_AI = 0x1A [CROSS-PENDING]`. 5 Outstanding Outline-Phase Questions flagged. |
