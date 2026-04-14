## 2.3 Functional Requirements

Functional requirements are grouped by pipeline stage. Each FR has an ID, a SHALL
statement, acceptance criteria, and a cross-reference to the testing section (Section 5)
where it is verified.

### 2.3.1 Pipeline Integrity Requirements (FR-01 – FR-04)

---

**FR-01 — Single snapshot per agent per tick**

> The Decision Tree SHALL process exactly one `PerceptionSnapshot` per agent per
> heartbeat tick, delivered via `ReceiveSnapshot()`.

**Rationale:** Multiple deliveries per tick would allow an agent to re-evaluate with
updated information mid-heartbeat, breaking the strict Perception-first evaluation order
(KD-1) and creating non-deterministic replay behaviour.

**Acceptance criteria:**
- `ReceiveSnapshot()` is called exactly once per agent per tick by the orchestrator.
- If called a second time within the same tick, the DT logs an error and ignores the
  second delivery (does not re-evaluate).
- Integration test IT-01 verifies single delivery across 100 simulated ticks with 22
  agents (2,200 calls, zero duplicate deliveries).

**Test reference:** Unit test UT-01 (Section 5).

---

**FR-02 — Single action per agent per tick**

> The Decision Tree SHALL produce exactly one `AgentAction` per agent per heartbeat tick.

**Rationale:** Multiple actions per tick are architecturally undefined. Execution systems
expect exactly one action per agent per tick. Zero actions leave an agent in an undefined
state.

**Acceptance criteria:**
- `DispatchAction()` is called exactly once per pipeline execution.
- In all failure modes (§2.4), the pipeline still produces exactly one `AgentAction`
  (fallback to HOLD; see FM-04 and FM-05).
- Integration test IT-02 verifies exactly 22 actions dispatched per heartbeat tick
  across 1,000 simulated ticks.

**Test reference:** Unit test UT-02 (Section 5).

---

**FR-03 — No world state reads**

> The Decision Tree SHALL NOT read from the `WorldState` namespace. All data consumed
> by the DT pipeline SHALL be sourced from `PerceptionSnapshot`, `MatchContext`,
> `TacticalContext`, or `AgentState` (self), assembled into `DecisionContext` in Step 2.

**Rationale:** World state reads violate the epistemic model. An agent acting on
information it cannot perceive produces non-physical, omniscient behaviour — the
antithesis of the simulation's design intent (§1.3, AR-2).

**Acceptance criteria:**
- `DecisionTree.cs` and all classes in the Decision Tree namespace have no `using` or
  `import` references to the `WorldState` namespace.
- A static analysis rule (enforced in CI pipeline) flags any such reference as a build
  error.
- Code review checklist item in §9 Approval Checklist: "WorldState namespace is not
  referenced anywhere in the Decision Tree namespace."

**Test reference:** Static analysis rule SA-01 (Section 5); documented in §9 checklist.

---

**FR-04 — Deterministic output**

> Given identical inputs (`PerceptionSnapshot`, `MatchContext`, `TacticalContext`,
> `AgentState`, `matchSeed`, `agentId`, `heartbeatTick`), the Decision Tree SHALL
> always produce an identical `AgentAction`.

**Rationale:** Determinism is required for replay integrity. The match replay system
must be able to reproduce any match exactly from the initial seed and the sequence of
inputs. Any non-determinism in the DT breaks replay correctness.

**Acceptance criteria:**
- Two DT instances initialised with the same `matchSeed` and fed identical inputs in
  identical order produce byte-identical `AgentAction` outputs across a full 90-minute
  simulated match.
- The Composure noise formula uses `noise_seed = matchSeed XOR (agentId << 16) XOR
  heartbeatTick` exclusively — no `System.Random`, no `UnityEngine.Random`, no
  `DateTime.Now`, no GUID.
- Unit test UT-04 verifies determinism by running the same 10-tick sequence twice with
  the same seed and asserting `AgentAction` equality for all 22 agents.

**Test reference:** Unit test UT-04, integration test IT-03 (Section 5).

---

### 2.3.2 Action Generation Requirements (FR-05 – FR-06)

---

**FR-05 — Possession-gated generation**

> The Decision Tree SHALL NOT generate action candidates that require capabilities
> unavailable to the agent given its possession state.

**Rationale:** An agent without possession cannot pass or shoot. An agent with possession
is not available for interception. Generating invalid candidates would require scoring
to handle undefined inputs — a silent failure risk (AR-5).

**Acceptance criteria:**
- If `HasPossession = true`: only PASS, SHOOT, DRIBBLE, HOLD candidates are generated.
- If `HasPossession = false`: only MOVE_TO_POSITION, PRESS, INTERCEPT candidates are
  generated.
- HOLD is always generated when `HasPossession = true` regardless of other conditions.
- Unit test UT-05 verifies both possession branches across 50 randomised `DecisionContext`
  inputs.

**Test reference:** Unit test UT-05 (Section 5).

---

**FR-06 — Attribute-influenced option breadth**

> The number of PASS candidates evaluated SHALL be influenced by the agent's `Decisions`
> attribute, such that low-`Decisions` agents consider fewer pass options and
> high-`Decisions` agents consider more.

**Rationale:** `Decisions` models cognitive bandwidth — a less decisive player does not
fully survey available options before acting. This creates measurable attribute-to-
behaviour differentiation consistent with the FM-killer design goal.

**Acceptance criteria:**
- `Decisions` attribute [1–20] maps to a maximum PASS candidate count
  `MaxPassCandidates` via a formula defined in Section 3.
- An agent with `Decisions = 1` evaluates at most `MIN_PASS_CANDIDATES` teammates
  (lower bound defined in Section 3).
- An agent with `Decisions = 20` evaluates all visible teammates (up to
  `MAX_VISIBLE_TEAMMATES` from Perception System).
- The mapping is monotonically non-decreasing: higher `Decisions` → equal or greater
  candidate count.
- Unit test UT-06 verifies the mapping at `Decisions` = 1, 5, 10, 15, 20 against
  expected candidate counts from Section 3.

**Test reference:** Unit test UT-06 (Section 5).

---

### 2.3.3 Scoring and Selection Requirements (FR-07 – FR-09)

---

**FR-07 — Utility formula coverage**

> Every generated `ActionOption` SHALL receive a utility score via the formula
> `U(option) = BaseUtility × AttributeMultiplier × ContextModifier × RiskPenalty`,
> with all four components computed and with the result clamped to [0.01, 1.0].

**Rationale:** The utility formula is the core scoring mechanism. All four components
must be present; missing any component degrades the quality of the decision model.
The clamp floor (0.01) prevents complete suppression of any candidate.

**Acceptance criteria:**
- All 7 action type formulas (Section 3) are implemented with all four components.
- No formula returns 0.0 or below (floor = 0.01).
- No formula returns above 1.0 (ceiling = 1.0).
- Unit tests UT-07a through UT-07g verify each action type formula independently at
  attribute extremes (`A_x = 0.0` and `A_x = 1.0`) and at `P = 0.0` and `P = 1.0`.

**Test reference:** Unit tests UT-07a – UT-07g (Section 5).

---

**FR-08 — HOLD fallback**

> The Decision Tree SHALL select HOLD if `GenerateOptions()` produces no viable
> candidates with `BaseUtility > 0` other than HOLD itself, without raising an
> exception.

**Rationale:** There must always be a valid action. An exception or null action at
this stage leaves the agent undefined for the remainder of the tick — an unrecoverable
simulation error. HOLD is always generated when the agent has possession, ensuring
this fallback is always available.

**Acceptance criteria:**
- Unit test UT-08 simulates a `DecisionContext` where all non-HOLD candidates score
  at or below the HOLD floor (e.g., zero visible teammates, no shooting range, no
  open space). Asserts `AgentAction.Type == HOLD`.
- `DecisionMadeEvent.FallbackToHold` is set to `true` in this case.
- No exception is raised in any configuration of `DecisionContext`.

**Test reference:** Unit test UT-08 (Section 5).

---

**FR-09 — Composure-differentiated selection**

> The `Composure` attribute [1–20] SHALL produce measurably different action selection
> distributions. An agent with `Composure = 20` SHALL select the highest-utility action
> significantly more often than an agent with `Composure = 1` when presented with the
> same scored candidate list.

**Rationale:** Composure is a primary differentiator between player quality tiers. If
Composure has no measurable selection effect, the attribute is non-functional — a
design failure inconsistent with the FM-killer specification goal.

**Acceptance criteria:**
- Unit test UT-09 runs `SelectAction()` 1,000 times on a fixed scored candidate list
  with `Composure = 1` and 1,000 times with `Composure = 20` (varying only `heartbeatTick`
  to vary noise seed).
- With `Composure = 20`: max-utility action selected ≥ 95% of trials.
- With `Composure = 1`: max-utility action selected ≤ 60% of trials.

⚠ **PROVISIONAL THRESHOLDS — MUST BE UPDATED BEFORE SECTION 2 CAN BE APPROVED:**
The 95% / 60% acceptance thresholds above are forward estimates based on the outline's
noise clamping guidance (±0.20 utility, AR-4). They will be superseded by mathematically
derived values once the Section 3 Composure noise model is finalised. Section 2 approval
is **blocked** until Section 3 §3.3 delivers the noise model, at which point:
  1. The derived noise range at `Composure = 1` and `Composure = 20` must be computed.
  2. The UT-09 thresholds must be updated to match the Section 3 derivation.
  3. This FR-09 acceptance criteria block must be revised to v1.1 with the correct values.

Do NOT lock UT-09 test values against these provisional figures. The Section 3 derivation
is authoritative; this section documents intent only.

- Results are deterministic: same tick sequence → same selection sequence (FR-04).

**Test reference:** Unit test UT-09 (Section 5).

---

### 2.3.4 Dispatch and Event Requirements (FR-10 – FR-11)

---

**FR-10 — Execution system routing**

> The Decision Tree SHALL route each `AgentAction` to the correct execution system in
> the same heartbeat tick it is selected.

**Rationale:** Deferred dispatch would allow the match state to change between selection
and execution, causing the agent to act on a stale decision. Synchronous same-tick
dispatch is required for simulation consistency.

**Acceptance criteria:**
- Each `ActionType` routes to exactly one execution system as defined in §2.1.2 Step 6
  dispatch table.
- Dispatch occurs before the orchestrator advances to the next agent.
- Integration test IT-04 verifies routing by injecting a mock execution system and
  asserting `MockPassMechanics.Receive()` is called with the correct `PassRequest` when
  `ActionType == PASS`, etc., for all 7 action types.

**Test reference:** Integration test IT-04 (Section 5).

---

**FR-11 — DecisionMadeEvent publication**

> The Decision Tree SHALL publish a `DecisionMadeEvent` for each dispatched `AgentAction`,
> regardless of action type or whether fallback-to-HOLD occurred.

**Rationale:** The event stub is the foundation for the replay and statistics pipeline.
If the event is not published in edge cases (fallback, tiebreak, error recovery), the
replay system will have gaps. Publication must be unconditional.

**Acceptance criteria:**
- `DecisionMadeEvent` is published for every `DispatchAction()` call.
- `DecisionMadeEvent.FallbackToHold` is `true` only when FR-08 fallback is triggered.
- `DecisionMadeEvent.TiebreakerApplied` is `true` only when the §2.1.2 tiebreak rule
  was invoked.
- Unit test UT-11 verifies publication in normal, fallback, tiebreak, and interrupt
  scenarios.

**Test reference:** Unit test UT-11 (Section 5).

---

### 2.3.5 Performance Requirement (FR-12)

---

**FR-12 — 4ms heartbeat budget**

> The Decision Tree pipeline for all 22 agents SHALL complete within 4ms per heartbeat
> tick, leaving 6ms of the 10ms budget for Perception System and execution system
> overhead.

**Rationale:** The tactical heartbeat runs at 10Hz (100ms game time per tick), mapped
to a real-time budget allocation. The simulation orchestrator allocates 10ms to the
combined Perception + DT pipeline. 4ms is the DT's share, consistent with the Master
Development Plan performance budget (Master Vol 4).

**Acceptance criteria:**
- Performance test PT-01 measures end-to-end DT pipeline time for all 22 agents across
  100 consecutive heartbeat ticks on the target hardware specification (Master Vol 4).
- Mean pipeline time ≤ 4ms per tick.
- 99th-percentile pipeline time ≤ 6ms per tick (spike allowance).
- Operation count analysis in Section 6 (Performance) derives the theoretical budget
  allocation from step-level operation counts before PT-01 is run.

**Test reference:** Performance test PT-01 (Section 5); operation count analysis Section 6.

---

### 2.3.6 Functional Requirements Summary Table

| ID | Statement (abbreviated) | Pipeline Step | Priority | Test Ref |
|---|---|---|---|---|
| FR-01 | One snapshot per agent per tick | Step 1 | Must Have | UT-01, IT-01 |
| FR-02 | One action per agent per tick | Step 6 | Must Have | UT-02, IT-02 |
| FR-03 | No world state reads | All steps | Must Have | SA-01 |
| FR-04 | Deterministic output | All steps | Must Have | UT-04, IT-03 |
| FR-05 | Possession-gated generation | Step 3 | Must Have | UT-05 |
| FR-06 | Decisions-influenced option breadth | Step 3 | Should Have | UT-06 |
| FR-07 | Utility formula coverage, all 7 types | Step 4 | Must Have | UT-07a–g |
| FR-08 | HOLD fallback, no exception | Step 5 | Must Have | UT-08 |
| FR-09 | Composure-differentiated selection | Step 5 | Must Have | UT-09 |
| FR-10 | Correct execution system routing | Step 6 | Must Have | IT-04 |
| FR-11 | DecisionMadeEvent published unconditionally | Step 6 | Should Have | UT-11 |
| FR-12 | 4ms budget for 22 agents | All steps | Must Have | PT-01 |

**Priority classification:** "Must Have" — failure to meet this requirement constitutes
a defect that blocks specification approval. "Should Have" — failure degrades quality
but does not block approval pending a documented remediation plan.

---

## 2.4 Failure Modes

Failure modes are conditions where the DT receives invalid, absent, or unexpected input,
or encounters internal state inconsistencies. Each failure mode has a defined resolution
behaviour. Failure modes must never produce unhandled exceptions or undefined `AgentAction`
outputs.

### 2.4.1 Failure Mode Catalogue

---

**FM-01 — No visible teammates (PASS unavailable)**

| Field | Value |
|---|---|
| Condition | `PerceptionSnapshot.VisibleTeammates` is empty (length 0) |
| Impact | PASS candidate generation produces zero candidates |
| Resolution | PASS is not generated. Remaining with-ball candidates (SHOOT, DRIBBLE, HOLD) are evaluated normally. HOLD is always available as fallback. |
| Invariant maintained | INV-01, INV-06 |
| Logged | No — this is a normal game state (agent isolated, heavy press) |

---

**FM-02 — Ball not visible to agent**

| Field | Value |
|---|---|
| Condition | `PerceptionSnapshot.BallVisible = false` |
| Impact | Agent does not know where the ball is |
| Resolution | All with-ball candidates suppressed. If `HasPossession = true` (agent has ball but cannot see it — physically implausible at Stage 0; treated as `HasPossession = false`). If `HasPossession = false`: MOVE_TO_POSITION is generated to formation slot; PRESS and INTERCEPT are suppressed (require ball position knowledge). `DecisionContext.BallVisible = false` gates PRESS and INTERCEPT generation in Step 3. |
| Invariant maintained | INV-01, INV-04, INV-05 |
| Logged | Yes — `DecisionMadeEvent.FallbackToHold = false`; `SelectedAction = MOVE_TO_POSITION` |

---

**FM-03 — No viable options above HOLD floor**

| Field | Value |
|---|---|
| Condition | All generated candidates (excluding HOLD) score at or below the HOLD base utility (0.25 [GT]) after scoring in Step 4 |
| Impact | HOLD would not be selected by utility alone — but it always wins this scenario by design |
| Resolution | HOLD is selected. `DecisionMadeEvent.FallbackToHold = true`. No exception. |
| Invariant maintained | INV-01, INV-06 |
| Logged | Yes — `FallbackToHold = true` in `DecisionMadeEvent` |

**Note:** HOLD has `U_base_HOLD = 0.25` [GT]. Any candidate whose scored utility exceeds
0.25 will beat HOLD in normal circumstances. If no candidate exceeds 0.25 (e.g., no
viable pass lanes, no shot opportunity, no open space, extreme pressure), HOLD wins by
its base value. This is intentional — the agent does the sensible thing and holds when
nothing better is available.

---

**FM-04 — PerceptionSnapshot null or malformed**

| Field | Value |
|---|---|
| Condition | `ReceiveSnapshot()` is called with a null snapshot, or the snapshot contains fields in an invalid state (e.g., `HasPossession = true` but no ball position) |
| Impact | Pipeline cannot assemble a valid `DecisionContext` |
| Resolution | Log error with agentId and heartbeatTick. Produce `AgentAction { Type = HOLD }` as a safe default. Publish `DecisionMadeEvent` with `FallbackToHold = true`. Do not throw. |
| Invariant maintained | INV-01, INV-06 |
| Logged | Yes — error log at ERROR level: "Invalid PerceptionSnapshot for agent {id} at tick {tick}" |

**Important:** A null or malformed snapshot is a programming error in the Perception
System or orchestrator. It must be visible in logs so it can be corrected. It must not
crash the simulation.

---

**FM-05 — Equal-utility candidates (tiebreak)**

| Field | Value |
|---|---|
| Condition | Two or more `ActionOption` candidates have identical `EffectiveUtility` after Composure noise injection |
| Impact | `SelectAction()` cannot deterministically pick a winner by utility score alone |
| Resolution | Select the candidate with the lower `ActionType` enum ordinal. E.g., PASS (0) beats SHOOT (1) in a tie. Log `TiebreakerApplied = true` in `DecisionMadeEvent`. |
| Invariant maintained | INV-01, FR-04 (determinism) |
| Logged | Yes — `DecisionMadeEvent.TiebreakerApplied = true` |

**Design note:** Ties are uncommon in practice because the Composure noise formula
produces distinct float values for distinct `(matchSeed, agentId, tick)` triplets.
However, floating-point equality is possible (e.g., two candidates that are both HOLD
with identical scores in a pathological state). The tiebreak rule handles this.

---

**FM-06 — Mid-execution interrupt (tackle)**

| Field | Value |
|---|---|
| Condition | The Collision System delivers a tackle interrupt signal to a PASS or SHOOT action that is currently in its windup phase |
| Impact | The execution system (Pass Mechanics or Shot Mechanics) transitions to INTERRUPTED state. The DT's current heartbeat evaluation may be in progress for other agents. |
| Resolution | The DT does not manage windup state (KD-5). The interrupt is handled entirely by the execution system. At the next heartbeat tick, this agent's `PerceptionSnapshot` will reflect the new state (no ball, likely). The DT evaluates fresh. `DecisionMadeEvent` for the interrupted action was already published at dispatch; no retraction event is required at Stage 0. |
| Invariant maintained | INV-01 (next tick), KD-5 |
| Logged | Execution system logs interrupt; DT does not log separately |

---

### 2.4.2 Failure Mode Severity Classification

| ID | Failure Mode | Severity | Response | Exception Thrown? |
|---|---|---|---|---|
| FM-01 | No visible teammates | Low | Normal scoring, HOLD available | No |
| FM-02 | Ball not visible | Low | MOVE_TO_POSITION fallback | No |
| FM-03 | No viable options above HOLD | Low | HOLD selected, logged | No |
| FM-04 | Null/malformed snapshot | High | HOLD default, error logged | No |
| FM-05 | Equal-utility tiebreak | Low | Enum ordinal tiebreak, logged | No |
| FM-06 | Mid-execution tackle interrupt | Medium | Handled by execution system; DT evaluates fresh next tick | No |

**Policy:** The Decision Tree never throws exceptions that propagate to the simulation
orchestrator. All failure modes produce a valid `AgentAction` with full `DecisionMadeEvent`
publication. Internal errors are logged at ERROR level and are visible in the debug
build log.

---

## 2.5 Version History

| Version | Date | Author | Changes |
|---|---|---|---|
| 1.0 | March 01, 2026, 12:00 PM PST | Claude (AI) / Anton | Initial draft. Complete pipeline description (§2.1), all 7 data structures with full field definitions (§2.2), 12 functional requirements with acceptance criteria (§2.3), and 6 failure modes with severity classification (§2.4). All outline OQ-1 through OQ-5 resolutions reflected. INV-01 through INV-08 pipeline invariants established. |
| 1.1 | March 01, 2026 | Claude (AI) / Anton | Four self-critique weaknesses resolved: (1) `TiebreakerApplied` removed from `AgentAction` — it is internal selection metadata; moved to `DecisionMadeEvent` exclusively. Execution systems must not receive selection provenance. (2) `MatchContext.BallZone` thresholds reclassified as [GT] with explicit XC-NOTE: Perception §3.4 contains no conflicting zone definitions; if it does in a future revision, ownership must be unified under Match Configuration (Stage 1). (3) FR-09 Composure acceptance criteria explicitly marked PROVISIONAL with a blocking gate: Section 2 approval requires UT-09 thresholds to be updated to match Section 3 §3.3 derivation before sign-off. (4) `DecisionContext.VisibleTeammates[]` implementation constraint added: per-tick heap allocation (220/sec) is prohibited; NativeArray or ArrayPool pattern required; `VisibleTeammateCount` / `VisibleOpponentCount` fields added; Section 6 must validate. |

---

## Section 2 Summary

Section 2 defines the complete operational model of the Decision Tree before any
technical formulas are introduced. The six-step single-heartbeat pipeline establishes
the order of operations, the data flow, and the evaluation sequence. The seven data
structures (`ActionType`, `AgentAction`, `ActionOption`, `DecisionContext`, `MatchContext`,
`TacticalContext`, `DecisionMadeEvent`) are fully defined with ownership, lifetime, and
field-level documentation. Twelve functional requirements state what the system must do
in unambiguous SHALL language with acceptance criteria. Six failure modes define safe
resolution behaviour for all foreseeable invalid-input and edge-case conditions.

No architecture, formula, or implementation concern is deferred from this section.
Section 3 builds directly on the pipeline defined here to specify the technical
detail of each step.

**Next:** Section 3 — Technical Specifications (Option Generation, Utility Model,
Composure Model, Dispatch Logic).

---

*End of Section 2 — Decision Tree Specification #8*  
*Tactical Director — Specification #8 of 20 | Stage 0: Physics Foundation*  
*v1.1 — March 01, 2026*
