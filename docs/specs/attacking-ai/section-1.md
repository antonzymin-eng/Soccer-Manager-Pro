# Attacking AI Specification #15 — Section 1: Introduction, Scope, Dependencies, Key Decisions

**Created:** May 17, 2026
**Last Updated:** May 17, 2026 (v0.1 — initial draft from `outline-detailed.md` v1.1)
**Version:** 0.1
**Status:** DRAFT
**Source:** `outline-detailed.md` v1.1 (May 17, 2026)

---

## 1.1 Purpose

Attacking AI (#15) specifies **coordinated off-ball movement** for outfield
agents when their team holds possession of the ball. The spec applies
exclusively to the off-ball movement pool — all outfield agents minus the
goalkeeper and the current ball carrier — and produces one `AttackDirective`
per team per tick plus one `AttackIntent` per off-ball agent per tick on the
10 Hz tactical loop.

The spec exists to (a) define the canonical **attacking-role catalog** (§3.3,
KD-12): `RUNNER`, `SUPPORT_BALL`, `HOLD_WIDTH`, `WEAK_SIDE`; (b) define
**run-parameter generation** (§3.4, KD-8) using three continuous parameters
(`depthOffset_m`, `lateralOffset_m`, `runTriggerTick`) — no `PatternType` or
`RunType` enum anywhere; (c) define the **support-angle heuristic** (§3.5)
and **width-holding protocol** (§3.6); (d) specify **weak-side pulling**
(§3.7) and **overload-zone detection** (§3.8); (e) declare **team-style
profile** modifiers (§3.10, KD-12) — three named constant-multiplier clusters
(`POSSESSION` / `DIRECT` / `COUNTER_ATTACK`) with no algorithm branching;
(f) specify the **transition-to-defense behaviour** on possession loss (§3.9,
KD-6); (g) enforce three measurable **anti-chaos invariants** before
directive publication (§3.11, KD-13); and (h) declare the integration surface
with Decision Tree #8 and Positioning AI #12 where runtime activation lands
at **Stage 1** per KD-17.

This specification is a producer of one `AttackDirective` per team per tick
and one `AttackIntent` per off-ball agent per tick. It does **not** own the
per-agent action loop (that is #8); does **not** own the action-selection
choice of PASS, SHOOT, DRIBBLE, or HOLD (that is #8 exclusively); does
**not** steer agents at 60 Hz (that is #2); does **not** redefine perception,
fatigue, or coordinate conventions (cite-not-redefine, KD-1); and does
**not** manage goalkeeper positioning (GK is always excluded from the
off-ball pool, KD-7).

It is bound by CLAUDE.md "Project Identity" (Stage 0 Physics Foundation — no
code until all 20 specs approved) and the "Interface Design Principle" (no
interfaces against unspecified consumers at Stage 0 — #15 is the final link
in the Phase C tactical-AI chain).

## 1.2 Scope

### 1.2.1 In Scope (specification text — Stage 0 deliverable)

- Off-ball attacking-role assignment from the four-role catalog (RUNNER /
  SUPPORT_BALL / HOLD_WIDTH / WEAK_SIDE) for all eligible agents (§3.3,
  KD-12).
- `RunParameters` generation for RUNNER-assigned agents: `depthOffset_m`,
  `lateralOffset_m`, `runTriggerTick` fully parameterised; no PatternType
  enum (§3.4, KD-8).
- Support-radius heuristic: agents within `SUPPORT_RADIUS_M [GT]` of ball
  carrier default to SUPPORT_BALL (§3.5, FR-AT-013).
- Width-holding protocol: at least `MIN_WIDTH_HOLDERS [GT]` agents hold
  the near-touchline corridor at all times (§3.6, FR-AT-014).
- Weak-side positioning: at least one agent holds the opposite Y-corridor
  from the ball when pool size ≥ `MIN_WEAK_SIDE_AGENT_THRESHOLD [GT]`
  (§3.7, FR-AT-015).
- Overload detection: `AttackDirective.overloadActive` flag and
  `overloadFlank` (LEFT/RIGHT) emitted when ≥ `OVERLOAD_COUNT [GT]`
  agents on the near-touchline side (§3.8, FR-AT-016).
- Team-style profile multipliers (`POSSESSION` / `DIRECT` /
  `COUNTER_ATTACK`) applied via `[GT]` constant scaling — no algorithm
  branching (§3.10, KD-12, FR-AT-017).
- Transition-to-defense behaviour on possession loss: frozen directive
  emitted for `TRANSITION_HOLD_TICKS [GT]` ticks, then empty directive
  (§3.9, FR-AT-009).
- Anti-chaos invariant enforcement: MAX_RUNNERS cap, MIN_SUPPORT_AGENTS
  floor, OWN_HALF_RUN_BLOCK_M runner-position guard — all checked
  POST-assignment, PRE-publication (§3.11, KD-13, FR-AT-021).
- Assignment-transition dwell-time hysteresis bound to #2 §3.1 pattern
  (§3.12, FR-AT-022).
- Phase gating via #12 phase enum: `OUT_OF_POSSESSION` suppresses all
  computation (§3.1, FR-AT-008).
- Stage-0-feasible chance-quality acceptance criteria: dangerous-zone
  shot surrogate and tactical-identity measurability (§5, KD-10).
- Constant catalogue declaration (§6, KD-14).

### 1.2.2 Out of Scope (deferred to §7 per KD-17)

Action selection (PASS / SHOOT / DRIBBLE / HOLD — owned by #8 and frozen);
runtime code (Stage 1 deliverable); the #8 §2.2.6 / §3.1.7 back-prop
amendment text itself (filed as `ERR-015-002` separately); #17 channel
registration (`ATTACK_RUN_STARTED` / `OVERLOAD_DECLARED` — `ERR-015-003` /
`ERR-015-004` at Stage 1); #12 `RunIntent` writer-layer struct implementation;
team-style instruction wiring from the coach UI; set-piece attacking
positioning (Stage 2+ set-piece system); xG modeling (Stage 1+ per Shot
Mechanics #6 §7); per-player run instructions from the tactics screen; ML-
tuned `[GT]` parameter fitting; save-game persistence; Fixed64 migration
(Stage 5+); goalkeeper-position management (owned by #11 per KD-7).

## 1.3 Dependencies

### 1.3.1 Approved Upstream

| Spec | Sections Bound | Use |
|---|---|---|
| #1 Ball Physics | §1.2, Appendix C | Corner-origin coordinate system; `PITCH_LENGTH_M` = 105 m, `PITCH_WIDTH_M` = 68 m; ball-state schema (`XC-015-001`) |
| #2 Agent Movement | §2.5 (`XC-002-001`), §3.1 | EntityId no-reuse (`XC-015-002`); dwell-time hysteresis pattern bound in §3.12 |
| #3 Collision System | §3.x | Boundary reference: #15 never produces contact physics; #3 owns that domain |
| #4 First Touch | §3.1 | Not read directly — consumed via #7 perception snapshot |
| #5 Pass Mechanics | §2 FR-10 (`PassAttemptEvent`) | Not read directly — ball state consumed via #7 snapshot |
| #6 Shot Mechanics | §2 | Boundary awareness for dangerous-zone metric (§5, KD-10); consumed via #7 snapshot only |
| #7 Perception System | §3.7–§3.10 | Filtered world model: agent positions, ball position, ball carrier `EntityId`, possession state, attribute lookups (`Pace`, `Stamina`, `Dribbling`), `isActive` flag; `PlayerRole.Goalkeeper` for GK exclusion |
| #8 Decision Tree | §1.3.2, §2.2.6, §3.1.7 | Stage-1 binding row (§1.3.2 defers coordinated attacking movement to Stage 1+); `TacticalContext.AttackIntent[]?` field (ERR-015-002, Option B — mirrors `PressDirective?` / `MarkDirective?` precedent); EntityId no-reuse (`XC-008-001`, referenced as `XC-015-003`) |
| #13 Pressing AI | §2.2 (`PressAssignment`), §3.4, §4.5 | Phase context: #13 is active when OUT_OF_POSSESSION; #15 is active when IN_POSSESSION — mutually exclusive via #12 phase enum. No direct data coupling within a tick. |
| #16 Deterministic Simulation | §3.2, §3.2.5, §3.4, §5, §6.2 | EntityId iteration order; domain-tag registry (`DOMAIN_TAG_ATTACKING_AI = 0x1B [CROSS-PENDING]`); per-tick digest scope for `AttackDirective` / `AttackIntent[]` / `RunParameters` / hysteresis state / transition-hold counter |
| #17 Event System | §3.10 (channel registry — Stage 1 back-prop) | No channels produced or consumed at Stage 0; `ATTACK_RUN_STARTED` / `OVERLOAD_DECLARED` deferred to ERR-015-003/004 |
| #18 Performance | §3.7, §6 | Zero-allocation hot-path discipline; per-tick budget framework |
| #19 Testing | §3, §4 | Test taxonomy and FR-traceability framework |
| #20 Code Standards | §4.2 (FR-CS-025) | Single constant-catalogue file `AttackingAIConstants.cs`; `#region` block organisation |

### 1.3.2 Pending Upstream

- **#11 Goalkeeper Mechanics** — `IN REVIEW` (May 16, 2026). Consumed as a
  boundary reference for the GK-exclusion rule (FR-AT-006, KD-7). The GK's
  `EntityId` and position are read via the #7 perception snapshot
  (`PlayerRole.Goalkeeper` flag); #15 never calls a direct #11 accessor at
  Stage 0 or Stage 1. No interface exists between #11 and #15 at any stage
  (Interface Design Principle — GK is simply excluded from the attacking pool
  before any computation begins).

- **#12 Positioning AI** — `IN REVIEW` (May 16, 2026). Consumed as the
  **baseline formation source** for all off-ball agents: per-agent
  `formationSlot` (`Vector2` baseline position), `lineMembership`
  (`DEFENSE` / `MIDFIELD` / `ATTACK` — used for RUNNER eligibility in §3.3),
  `lateralPct` (float 0–1, used to derive `lateralOffset_m` in §3.4),
  and `laneAssignment` (5-bin: LEFT_WIDE / LEFT / CENTRE / RIGHT /
  RIGHT_WIDE — used for width-holding identification in §3.6). Also provides
  the per-team `phase` enum (`IN_POSSESSION` / `OUT_OF_POSSESSION` /
  `TRANSITION`) that gates all of #15's computation (FR-AT-008, KD-5).
  Stage 1 runtime accesses these via `BaselineShape` read-only view and
  `PositioningAI.GetPhase(TeamId)` per #12 §4.5 (`XC-015-004`).
  No interface produced against #12 at Stage 0 (FR-AT-031, `XC-015-005`
  binding #12 FR-PA-048).

- **#14 Defensive AI** — `IN REVIEW` (May 17, 2026). Consumed as a mutual-
  exclusion boundary reference. #14 is active when the team is
  OUT_OF_POSSESSION; #15 is active when the team is IN_POSSESSION. They are
  mutually exclusive per team per tick via the #12 phase enum (KD-6 in both
  specs). A Stage 1+ boundary hint exists: #15 may consume
  `MarkDirective.emergencyFlag` from #14 to accelerate the
  `TransitionHoldState` countdown to zero. No interface produced against #14
  at Stage 0 (Interface Design Principle — #14 is IN REVIEW at draft time).

### 1.3.3 Cross-Spec Issues Filed at Section-File Draft

- **`ERR-015-001`** — `DOMAIN_TAG_ATTACKING_AI = 0x1B` allocation in #16
  §3.4. Proposed value `0x1B` per the ERR-012-001 Phase B/C block layout:
  `0x17` = #12 (ERR-012-001 proposed), `0x18` or `0x1D` = #11
  (ERR-011-001 proposed — whichever of #11/#12 reaches `APPROVED` first
  takes `0x17`/`0x18`; if #12 reaches `APPROVED` first, #11 shifts to
  `0x1D`), `0x19` = #13 (resolved), `0x1A` = #14 (ERR-014-004 proposed),
  `0x1B` = #15 (this entry). #15's `0x1B` slot is stable regardless of the
  #11/#12 domain-tag race because the shift only affects the `0x17`/`0x18`
  allocation. **Status:** OPEN — `DOMAIN_TAG_ATTACKING_AI` is
  `[CROSS-PENDING]` throughout this spec until ERR-015-001 is ratified in
  #16 §3.4. The same shift-right collision policy as the May 16, 2026
  #10/#12 domain-tag shift applies if a conflict emerges before ratification.

- **`ERR-015-002`** — `TacticalContext.AttackIntent[]?` nullable field
  addition to #8 §2.2.6. **Mechanism:** Option B, mirroring the
  `PressDirective?` precedent established by #13 ERR-013-001 and the
  `MarkDirective?` precedent established by #14 ERR-014-001. #15 writes
  `AttackIntent[]?` per-team per-tick at Stage 1+; #8 §3.1.7
  (`MOVE_TO_POSITION`) reads it when constructing off-ball agent target
  positions for RUNNER-assigned agents. The `AttackIntent[]?` field is
  nullable so that a `null` value at Stage 0 / before #15 activates is a
  well-formed signal to #8 that no coordinated off-ball running is in
  effect. **Status:** OPEN — back-prop to #8 §2.2.6 / §3.1.7 to be
  ratified before Stage 1 activation (FR-AT-033 gate (a)).

- **`ERR-015-003`** — `ATTACK_RUN_STARTED` channel registration in #17
  §3.10 (Stage 1). Fired when an agent's `AttackIntent` role transitions to
  RUNNER (transition tick only — not every tick that the agent is a RUNNER).
  Exact byte range: next available after #14's Stage 1 allocation per #17
  §3.10 schema; deferred to Stage 1 first commit per KD-15.

- **`ERR-015-004`** — `OVERLOAD_DECLARED` channel registration in #17 §3.10
  (Stage 1). Fired when `AttackDirective.overloadActive` transitions to
  `true` (not every tick that an overload is active). Deferred to Stage 1
  first commit per KD-15.

- **`ERR-015-005`** — Back-prop to #8 §1.3.2 to add "Attacking AI #15" to
  the multi-agent coordination deferral row (currently names #12/#13 by
  implication; explicit citation of #15 removes ambiguity). One-token patch;
  no formula change. Filed at section-file draft.

### 1.3.4 Downstream (declared, not implemented)

#15 is the final link in the Phase C tactical-AI chain. No specification
depends on #15 as an upstream. No Stage 0 or Stage 1 interface is produced
against any downstream consumer (CLAUDE.md "Interface Design Principle" —
#15 has no downstream consumers that are authored specs requiring a boundary
declaration).

## 1.4 Key Domain Concepts

| Term | Definition |
|---|---|
| **Off-ball movement pool** | The set of outfield agents for which #15 produces `AttackIntent` on a given tick: all agents on the possessing team excluding the GK (KD-7) and the current ball carrier (KD-3). Maximum size is 9 agents (11 players − 1 GK − 1 ball carrier). |
| **Attacking role** | One of four discrete per-agent designations assigned each tick from the catalog (FR-AT-012): `RUNNER`, `SUPPORT_BALL`, `HOLD_WIDTH`, `WEAK_SIDE`. |
| **RUNNER** | Attacking role: agent makes a timed forward run described by `RunParameters`. Eligible only for agents with `formationSlot.lineMembership` in {ATTACK, MIDFIELD}. Count limited by `MAX_RUNNERS [GT]`. |
| **SUPPORT_BALL** | Attacking role: agent positions near the ball carrier to provide a close passing option. Triggered when the agent is within `SUPPORT_RADIUS_M [GT]` of the ball carrier. |
| **HOLD_WIDTH** | Attacking role: agent holds the near-touchline corridor to stretch the defensive shape. Default role for agents that do not qualify for the other three roles. |
| **WEAK_SIDE** | Attacking role: agent holds the Y-corridor opposite the ball, providing a switch-of-play option. At most one agent is assigned WEAK_SIDE per tick. |
| **AttackDirective** | Per-team per-tick output struct: `overloadActive` bool, `overloadFlank` (LEFT/RIGHT), `transitionHoldTick` countdown. Written by #15; read by the orchestrator. |
| **AttackIntent** | Per-off-ball-agent per-tick output struct: `role` (RUNNER/SUPPORT_BALL/HOLD_WIDTH/WEAK_SIDE), `runParameters RunParameters?` (non-null only when role is RUNNER), `validThroughTick`. Written by #15; read by the orchestrator and (at Stage 1) by #8. |
| **RunParameters** | Sub-struct of `AttackIntent` for RUNNER agents. Exactly three fields: `depthOffset_m f32`, `lateralOffset_m f32`, `runTriggerTick i32`. The run angle is a derived quantity (`atan2(lateralOffset_m, depthOffset_m)`) computed at use-site only — never stored in this struct (FR-AT-011, KD-8). |
| **Support radius** | The distance threshold (constant `SUPPORT_RADIUS_M [GT]`) within which an agent defaults to SUPPORT_BALL. Scaled by `styleProfile.supportMult` for the active team-style profile. |
| **Overload zone** | A Y-corridor of width `OVERLOAD_ZONE_WIDTH_M [GT]` centred on the ball's Y-coordinate. When ≥ `OVERLOAD_COUNT [GT]` non-WEAK_SIDE agents occupy this corridor on the same flank, an overload is declared and `AttackDirective.overloadActive` is set. |
| **Weak-side puller** | The off-ball agent assigned WEAK_SIDE role: the agent with the greatest Y-deviation from the ball, holding a position in the opposite Y-corridor to stretch the defence. EntityId ascending is the tie-break when two agents share equal Y-deviation. |
| **Width-holder** | An off-ball agent assigned HOLD_WIDTH role and positioned near the near touchline (`TOUCHLINE_HOLD_DIST_M [GT]` from the boundary) to maintain lateral pitch coverage. |
| **Team-style profile** | A named cluster of constant multipliers (`POSSESSION` / `DIRECT` / `COUNTER_ATTACK`) that scale `depthOffset_m`, `runTriggerTick`, `MAX_RUNNERS`, and `SUPPORT_RADIUS_M`. The algorithm is identical across all three profiles; only the constant values change (KD-12). Not an enum anywhere in the algorithm or data structures. |
| **Transition-hold** | A per-team tick countdown (`TransitionHoldState`) that fires when the team loses possession. During the countdown, #15 emits a frozen version of the last `AttackDirective` before ceasing; this covers the brief window while agents recover defensive shape. `TRANSITION_HOLD_TICKS [GT]` is 0 for the COUNTER_ATTACK profile. |
| **Anti-chaos invariant** | One of three measurable constraints enforced before `AttackDirective` publication (KD-13, §3.11): (1) simultaneous RUNNER count ≤ `MAX_RUNNERS [GT]`; (2) at least `MIN_SUPPORT_AGENTS [GT]` agents in SUPPORT_BALL or HOLD_WIDTH; (3) no RUNNER `runTargetPosition` assigned beyond `OWN_HALF_RUN_BLOCK_M [GT]` past the half-line into own territory. |
| **Assignment hysteresis** | Per-agent dwell counter (`AttackHysteresisState`) that gates role transitions. A role change is committed only after the new candidate role has been continuously preferred for `ATTACK_DWELL_TICKS [EST]` consecutive ticks, preventing role-thrash at boundary conditions (FR-AT-022, FR-AT-023). |
| **Dangerous zone** | Stage-0-feasible chance-quality surrogate metric: the region within `DANGER_ZONE_MAX_DIST_M [GT]` of the opponent goal centre AND within `DANGER_ZONE_CORRIDOR_HW_M [GT]` of the goal-centre Y. SHOOT actions inside this zone count as "dangerous-zone shots" — no xG model required (KD-10, FR-AT-034). |
| **Final third** | The attacking team's forward 35 m of the pitch. For a team attacking the x=105 goal: x ≥ `FINAL_THIRD_X_M [DERIVED]` = `PITCH_LENGTH_M × 2/3` ≈ 70 m. For the team attacking the x=0 goal: x ≤ `PITCH_LENGTH_M / 3` ≈ 35 m. All algorithms use a normalised distance-to-opponent-goal scalar to avoid per-team branching (KD-16). |
| **teamAttackAngle** | A match-half constant (not the ball carrier's velocity vector). `0.0 rad` for the team attacking toward x=105; `π rad` for the team attacking toward x=0. Used in §3.4 to decompose run offsets into pitch-frame vectors. |

## 1.5 Key Design Decisions

Cross-reference to the 17 KDs catalogued in `outline-detailed.md` v1.1:

| KD | Subject | Resolution Locus |
|---|---|---|
| KD-1 | Cite-not-redefine of CLAUDE.md invariants and upstream-spec rules; no re-statement of coordinates, fatigue convention, tick rates, or EntityId rules | §1.7 |
| KD-2 | 10 Hz tactical loop; no 60 Hz work in #15; all steering via #8 → #2 path | §1.7, §4.1 |
| KD-3 | Boundary with Decision Tree #8 — #15 never selects PASS/SHOOT/DRIBBLE/HOLD; #15 produces `AttackIntent` that #8 reads at Stage 1; ball carrier excluded from pool | §1.6, §3.2 |
| KD-4 | Boundary with Positioning AI #12 — #12 owns baseline `formationSlot`; #15 writes `RunIntent` writer-layer overlay (Stage 1+); #15 reads `lineMembership`, `lateralPct`, `laneAssignment`, `phase` from #12 | §1.6, §4.4 |
| KD-5 | Boundary with Pressing AI #13 — mutually exclusive by possession phase; no direct data coupling within a tick | §1.6, §3.1 |
| KD-6 | Boundary with Defensive AI #14 — mutually exclusive per team per tick by #12 phase enum; `TransitionHoldState` covers possession-loss window; `MarkDirective.emergencyFlag` is a Stage 1+ boundary hint only | §1.6, §3.9 |
| KD-7 | Boundary with Goalkeeper Mechanics #11 — GK unconditionally excluded from attacking pool; GK position read via #7 snapshot; no direct #11 accessor at any stage | §1.6, §3.2 |
| KD-8 | No PatternType / RunType / OverlapType enum — all movement fully described by three `RunParameters` fields; `overloadFlank` (LEFT/RIGHT) is a spatial discriminator, not a movement-pattern enum | §3.4, §2.2 |
| KD-9 | Stage binding — spec at Stage 0; runtime at Stage 1; same pattern as #13 and #14 | §1.8, §7 |
| KD-10 | Stage-0-feasible acceptance criteria — dangerous-zone shot surrogate (§5.7) and tactical-identity measurability via role histograms (§5.8) | §5, FR-AT-034, FR-AT-035 |
| KD-11 | Determinism binding (#16) — EntityId-sort, RNG domain tag `0x1B [CROSS-PENDING]`, all output structs in per-tick digest | §3, §4.6 |
| KD-12 | Team-style profiles (POSSESSION / DIRECT / COUNTER_ATTACK) — named constant-multiplier clusters, not enums; algorithm is identical across all three | §3.10 |
| KD-13 | Anti-chaos invariants: MAX_RUNNERS cap, MIN_SUPPORT_AGENTS floor, OWN_HALF_RUN_BLOCK_M guard — enforced POST-assignment, PRE-publication; fallback to all-default on unresolvable violation | §3.11 |
| KD-14 | Single constant catalogue `AttackingAIConstants.cs` per #20 §4.2 | §4.2 |
| KD-15 | Event System binding (#17) — `ATTACK_RUN_STARTED` / `OVERLOAD_DECLARED` channels at Stage 1 via ERR-015-003/004; no channels at Stage 0 | §7.5 |
| KD-16 | Coordinate and convention bindings — corner-origin (#1 §1.2); final-third/own-half normalised to distance-to-opponent-goal scalar; weak-side Y-corridor via asymmetric midfield threshold; fatigue 0=rested; 10 Hz tactical / 60 Hz physics | §1.7 |
| KD-17 | Stage-0/Stage-1 scope discipline — action selection, runtime code, #8 amendment text, #17 channel reg, set-piece positioning, ML tuning all deferred | §1.2.2, §7 |

## 1.6 Interface Boundaries

Authoritative Boundary Matrix (mirrors `outline-detailed.md` v1.1):

| Boundary | #15 owns | Counterparty owns | Direction | Mechanism | Stage 0? |
|---|---|---|---|---|---|
| #8 Decision Tree | `AttackIntent` per off-ball agent (run target, role); `AttackDirective` per team | Per-agent action loop (PASS / SHOOT / DRIBBLE / HOLD / MOVE_TO_POSITION scoring) | #8 reads #15 (Stage 1) | `TacticalContext.AttackIntent[]?` extension via ERR-015-002, Option B (mirrors `PressDirective?` / `MarkDirective?` precedent — KD-3) | No (Stage 1 runtime) |
| #12 Positioning AI | `RunIntent` write layer (temporary slot deviation for RUNNER agents) | Baseline `formationSlot`; `lineMembership`; `lateralPct`; `laneAssignment`; phase enum | Orchestrator composes; #8 reads composed slot | `RunIntent` writer-layer per #12 §4.5 (`XC-015-004`); `BaselineShape` read-only view; `PositioningAI.GetPhase(TeamId)` accessor (Stage 1+) | No (Stage 1) |
| #13 Pressing AI | (no direct coupling; phase-gated) | PRIMARY_PRESS / COVER_SHADOW roles when OUT_OF_POSSESSION | Independent phase gating via #12 phase enum | Same #12 phase enum read by both specs independently (KD-5) | No |
| #14 Defensive AI | (mutually exclusive by possession phase) | `MarkDirective` (all-ZONAL when team is IN_POSSESSION per FR-DA-013) | Independent phase gating; `MarkDirective.emergencyFlag` is a Stage 1+ boundary hint | KD-6 mutual exclusion; `emergencyFlag` hint (Stage 1+ only; Interface Design Principle — no interface at Stage 0) | No (spec text only) |
| #11 Goalkeeper | (none — GK unconditionally excluded from pool) | GK positioning, saves, distribution | GK excluded from pool before any computation | KD-7 exclusion rule; GK `EntityId` identified via `PlayerRole.Goalkeeper` in #7 snapshot | Yes (spec text) |
| #2 Agent Movement | (none direct — via #8 action output) | 60 Hz steering toward `Action.TargetPosition` | #2 reads #8 | Same composition path as #12 / #13 / #14 | No |
| #7 Perception | (none — read consumer only) | Filtered world model: agent positions, ball position, ball carrier EntityId, possession state, attributes, `isActive` | #15 reads #7 | Snapshot captured at tick start; no mid-tick re-reads | Yes (spec text) |
| #16 Determinism | `AttackDirective` / `AttackIntent[]` / `RunParameters` / `AttackHysteresisState` / `TransitionHoldState` digest scope | Digest format + iteration rule | #15 conforms | EntityId iteration + domain-tagged RNG (`DOMAIN_TAG_ATTACKING_AI = 0x1B [CROSS-PENDING]` until ERR-015-001 ratified) | Yes (spec text) |
| #17 Event System | `ATTACK_RUN_STARTED` / `OVERLOAD_DECLARED` channel definitions | Channel registry | (deferred Stage 1) | ERR-015-003 / ERR-015-004 at Stage 1 | No (Stage 1) |
| #18 Performance | (conformance only) | Per-tick budget framework | #15 conforms | §6 budget against named host | Yes (spec text) |
| #19 Testing | (conformance only) | Test-framework conventions | #15 conforms | §5 plan | Yes (spec text) |
| #20 Code Standards | (conformance only) | File / catalogue / naming rules | #15 conforms | `AttackingAIConstants.cs` per FR-CS-025 | Yes (spec text) |

## 1.7 Coordinate and Convention Bindings (KD-1 cite-not-redefine)

- **Coordinate origin:** corner of pitch at (0, 0, 0); X = 0–105 m
  goal-to-goal; Y = 0–68 m touchline-to-touchline; Z = height (ground
  at Z = 0 m). Cited from Ball Physics #1 §1.2 and Appendix C
  (`XC-015-001`). Ball center rests at Z = 0.11 m when grounded.

- **Attack-direction normalisation:** `teamAttackAngle` is a match-half
  constant. For the team attacking the x=105 goal: `teamAttackAngle = 0.0 rad`
  (positive-X direction). For the team attacking the x=0 goal:
  `teamAttackAngle = π rad` (negative-X direction). All run-offset
  formulas in §3.4 decompose into pitch-frame vectors using this angle,
  eliminating per-team branching. The GK's goal-anchor x-coordinate
  follows the same normalisation.

- **Final-third and own-half bounds:** `FINAL_THIRD_X_M [DERIVED]` =
  `PITCH_LENGTH_M × 2/3` ≈ 70 m (for team attacking x=105) or
  `PITCH_LENGTH_M / 3` ≈ 35 m (for team attacking x=0). All algorithms
  use a normalised `distanceToOpponentGoal` scalar computed once per tick
  per agent so the same formula body applies to both teams.

- **Weak-side Y-corridor:** Formal definition (KD-16): if `ball.y > 34 m`,
  the weak side is the y < 30 m half; if `ball.y ≤ 34 m`, the weak side
  is the y > 38 m half. The 4 m asymmetric threshold prevents role-flicker
  when the ball crosses the Y mid-line. See §3.7 for the full definition
  with `WEAK_SIDE_FAR_Y_M [GT]`.

- **Fatigue convention:** `0.0 = fully rested`, `1.0 = fully fatigued`.
  Cited from CLAUDE.md. Any inversion is a critical error (KD-1,
  FR-AT-032).

- **Tick rates:** 10 Hz tactical (this spec, KD-2); 60 Hz physics (#1, #2,
  #3). Cited from CLAUDE.md. #15 produces no per-frame work; all 60 Hz
  steering is owned by #2 via the #8 action pipeline.

- **EntityId no-reuse:** Bound from #2 §2.5 (`XC-002-001`, referenced
  here as `XC-015-002`) and #8 §1.7.3 (`XC-008-001`, referenced here
  as `XC-015-003`). Required by EntityId-sorted iteration (#16 §3.2.5,
  KD-11) and by EntityId ascending terminal tie-breaks throughout the
  assignment algorithm (§3.3, §3.7, §3.11).

- **Attribute range:** Player attributes (`Pace`, `Stamina`, `Dribbling`,
  etc.) are on the integer scale [1–20] per Perception System #7 and the
  master planning volumes. All formulas in §3 normalise these to the
  continuous range [0, 1] before use by the transformation
  `normalised = (attribute − 1) / 19`.

- **No PatternType / RunType / OverlapType enum (KD-8):** All off-ball
  movement is fully described by three continuous `RunParameters` fields.
  Gameplay vocabulary labels (overlap, underlap, third-man run, cutback)
  appear only in the §8 glossary (Appendix F of this spec) and team-style
  documentation. No pattern-type discriminator enters the algorithm, the
  data structures, or the physics pipeline at any stage.

- **Parameter-based physics:** #15 produces `Vector2` target positions and
  `EntityId` identifiers, not enum types propagated into the physics layer.
  This is consistent with CLAUDE.md "Parameter-Based Physics (No Type Enums)".
  `AttackDirective.overloadFlank` (LEFT/RIGHT) is a spatial discriminator
  in the AI layer only — it never enters the physics pipeline.

## 1.8 Stage-Binding Statement

**Spec drafted at Stage 0; runtime activates at Stage 1.** Authoritative
basis: Decision Tree #8 §1.3.2 (features deferred to Stage 1+:
"Stage 1 — multi-agent coordination" — covers #12, #13, and by implication
#15; ERR-015-005 filed to add #15 explicitly) and Positioning AI #12 §4.5
(`RunIntent` writer-layer declared as a Stage 1+ struct, naming #15 as the
consumer).

At Stage 0, #8 handles all in-possession individual behaviour via its
existing action set (PASS, SHOOT, DRIBBLE, HOLD, MOVE_TO_POSITION) without
off-ball coordination. Off-ball agents drift toward their #12 baseline
formation slot; on-ball agents independently select pass, shoot, or dribble.
There is no run-timing, no overload creation, no weak-side pulling, and no
team-style differentiation at Stage 0.

#15 introduces the coordinated off-ball movement layer — timed runs that
create space behind the defensive line, width holders that stretch the
shape, overload zones that concentrate attackers on one flank, and team-style
profiles that produce measurably different chance-creation distributions
across `POSSESSION` / `DIRECT` / `COUNTER_ATTACK` presets. This is the same
Stage-0-spec / Stage-1-runtime pattern as Pressing AI #13 and Defensive
AI #14.

Stage 0 deliverable from #15 = published specification only (this document,
the section files, and the appendices). **No runtime code at Stage 0.**

Stage 1 activation requires three preconditions (FR-AT-033):

1. `ERR-015-002` resolved — `TacticalContext.AttackIntent[]?` field ratified
   in #8 §2.2.6 (Option B selected; back-prop pending).
2. #12 Positioning AI reaches `APPROVED` (consumed as baseline shape source,
   `lineMembership` / `lateralPct` / `laneAssignment` provider, and phase
   enum gate).
3. `ERR-015-003` / `ERR-015-004` — #17 channel rows for
   `ATTACK_RUN_STARTED` and `OVERLOAD_DECLARED` landed.

Until all three clear, #15 ships as an inert specification — exactly the
pattern established by #13 §1.8 and #14 §1.8.

## 1.9 Version History

| Version | Date | Author | Summary |
|---|---|---|---|
| 0.1 | May 17, 2026 | AI agent (claude-sonnet-4-6) | Initial draft from `outline-detailed.md` v1.1. §1.1–§1.9 authored. Boundary matrix confirmed against `defensive-ai/section-1.md` v0.3 and `outline-detailed.md` v1.1. ERR-015-001..005 declared. KD-1..KD-17 tabulated. Coordinate and convention bindings cited. Stage-binding statement mirrors #13 §1.8 and #14 §1.8 pattern. |
