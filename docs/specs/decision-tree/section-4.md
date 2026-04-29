# Decision Tree Specification #8 — Section 4: Architecture and Integration

**File:** `section-4.md`  
**Purpose:** Define the implementation architecture, module boundaries, and integration contracts for Decision Tree Specification #8.  
**Created:** April 20, 2026  
**Version:** 1.0  
**Status:** ✅ APPROVED — Lead developer signed off April 27, 2026 (draft-level quality gate; see §9 approval checklist)  
**Specification Number:** 8 of 20 (Stage 0 — Physics Foundation)  
**Author:** Claude (AI) with Anton (Lead Developer)

---

## Table of Contents

- [4.1 Architecture Overview](#41-architecture-overview)
- [4.2 File Structure](#42-file-structure)
- [4.3 Internal Module Contracts](#43-internal-module-contracts)
- [4.4 Upstream Integration Contracts](#44-upstream-integration-contracts)
- [4.5 Downstream Integration Contracts](#45-downstream-integration-contracts)
- [4.6 Determinism and Safety Boundaries](#46-determinism-and-safety-boundaries)
- [4.7 Cross-Specification Validation Checks](#47-cross-specification-validation-checks)
- [4.8 Version History](#48-version-history)

---

## 4.1 Architecture Overview

Decision Tree runs once per agent per heartbeat tick (10 Hz), receives a `PerceptionSnapshot`, executes the six-step pipeline (§2.1.2), and dispatches one `AgentAction`.

**Ownership boundary:**
- Decision Tree owns action **selection**.
- Pass/Shot/Movement systems own action **execution**.
- Decision Tree does not read raw world state (`WorldState` namespace prohibited by FR-03).

**Evaluation order constraint (KD-1/KD-7):**
1. Perception batch for all 22 agents
2. Decision Tree evaluations in ascending `AgentId`
3. Execution systems consume dispatched requests

---

## 4.2 File Structure

```text
/Scripts/DecisionTree/
│
│   DecisionTree.cs
│       Orchestrator-facing entry point (`ReceiveSnapshot`).
│       Runs pipeline Steps 1–6 for one agent.
│
│   SnapshotValidator.cs
│       Step 1 guards: malformed snapshot detection, mandatory field checks.
│
│   DecisionContextAssembler.cs
│       Step 2: snapshot + MatchContext + TacticalContext + AgentState → DecisionContext.
│
│   OptionGenerator.cs
│       Step 3: builds PASS/SHOOT/DRIBBLE/HOLD/MOVE/PRESS/INTERCEPT candidates (§3.1).
│
│   UtilityScorer.cs
│       Step 4: computes ScoredUtility from §3.2 + tactical modifiers §3.4.
│
│   ActionSelector.cs
│       Step 5: composure-noise injection + deterministic winner select (§3.3).
│
│   ActionDispatcher.cs
│       Step 6: PASS/SHOOT request population + MovementCommand routing (§3.5).
│
│   TacticalModifierResolver.cs
│       TacticalContext multipliers and possession-conditional PRESS urgency (§3.4).
│
│   DecisionTreeStateMachine.cs
│       DtState transitions IDLE/EVALUATING/EXECUTING/INTERRUPTED (§3.7).
│
│   DecisionTreeConstants.cs
│       Local constants not owned by UtilityWeights/ComposureWeights.
│       Each constant tagged [GT]/[EST]/[FIXED]/[DERIVED].
│
│   AgentAction.cs
│   ActionOption.cs
│   DecisionContext.cs
│   MatchContext.cs
│   TacticalContext.cs
│   DecisionMadeEvent.cs
│
└───/Tests/
        OptionGeneratorTests.cs
        UtilityScorerTests.cs
        ActionSelectorTests.cs
        DispatcherTests.cs
        DecisionTreeIntegrationTests.cs
```

No component in this folder may reference `WorldState` directly.

---

## 4.3 Internal Module Contracts

| Module | Input | Output | Side Effects |
|---|---|---|---|
| `SnapshotValidator` | `PerceptionSnapshot` | validation result | none |
| `DecisionContextAssembler` | snapshot + contexts | `DecisionContext` | none |
| `OptionGenerator` | `DecisionContext` | `ActionOption[]` | none |
| `UtilityScorer` | options + `DecisionContext` | scored options | none |
| `ActionSelector` | scored options + seed data | `AgentAction` | none |
| `ActionDispatcher` | `AgentAction` + `DecisionContext` | dispatch result | writes to external systems |

**Invariant:** Steps 1–5 are pure deterministic transforms. Step 6 is the first boundary with side effects.

---

## 4.4 Upstream Integration Contracts

| Upstream Spec | Dependency | Contract |
|---|---|---|
| Perception #7 | `PerceptionSnapshot` | Delivered via direct call `ReceiveSnapshot()` (§3.6) |
| Agent Movement #2 | `AgentState`, `PlayerAttributes` | Read-only inputs; no writeback |
| Ball Physics #1 | `BallState` via `MatchContext` | Read-only public match context only |
| Collision System #3 | tackle/interrupt signal, spatial pressure queries | read-only query + interrupt consumption |

**Contract rule:** if an upstream struct changes field names/types, Decision Tree section 4 and section 9 checklist must be updated before approval.

---

## 4.5 Downstream Integration Contracts

| Downstream Spec | Output | Boundary Rule |
|---|---|---|
| Pass Mechanics #5 | `PassRequest` | Populate only fields defined in Pass #5 §2.4.1 |
| Shot Mechanics #6 | `ShotRequest` | Populate only fields defined in Shot #6 §2.4.1 |
| Agent Movement #2 | `MovementCommand` | DRIBBLE/HOLD/MOVE/PRESS/INTERCEPT routing |
| Event System #17 | `DecisionMadeEvent` stub | Stage 0 stub publication only |

Decision Tree must not reinterpret execution outcomes inside the same tick. Re-evaluation happens on the next heartbeat or forced refresh.

---

## 4.6 Determinism and Safety Boundaries

1. **Deterministic evaluation order:** ascending `AgentId` (0..21).
2. **Deterministic noise:** SplitMix64-based hash in §3.3 only.
3. **No non-deterministic APIs:** no `System.Random`, no wall-clock time.
4. **Snapshot lifetime safety:** no snapshot span cached after `ReceiveSnapshot()` returns.
5. **Omniscience safety:** static analysis rule rejects `WorldState` namespace usage.

---

## 4.7 Cross-Specification Validation Checks

| Check ID | Verification | Source | Status |
|---|---|---|---|
| XC-DT4-01 | `PassRequest` field names/types aligned with Pass #5 §2.4.1 | Pass #5 | Pending §9 sign-off |
| XC-DT4-02 | `ShotRequest` field names/types aligned with Shot #6 §2.4.1 | Shot #6 | Pending §9 sign-off |
| XC-DT4-03 | `MovementCommand` state/facing enums aligned with Agent Movement #2 §3.5.4 | Agent Movement #2 | Pending §9 sign-off |
| XC-DT4-04 | `ReceiveSnapshot()` ordering aligned with Perception #7 deferred interface notes | Perception #7 | Pending §9 sign-off |
| XC-DT4-05 | Coordinate mapping for movement targets uses authoritative convention from Ball Physics #1 §1.2 | Ball Physics #1 | Pending §9 sign-off |

---

## 4.8 Version History

| Version | Date | Author | Changes |
|---|---|---|---|
| 1.0 | April 20, 2026 | Claude (AI) / Anton | Initial draft of Section 4 architecture and integration contracts. |

---

*End of Section 4 — Decision Tree Specification #8*  
*Tactical Director — Specification #8 of 20 | Stage 0: Physics Foundation*
