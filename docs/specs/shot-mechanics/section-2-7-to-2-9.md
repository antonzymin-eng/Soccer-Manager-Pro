## 2.7 Failure Modes and Recovery

All failure modes produce a valid `ShotResult` and never leave the state machine in an
undefined state. `Ball.ApplyKick()` is never called with invalid or partially-computed data.

| FM ID | Failure | Detection Point | Recovery Action | Notes |
|-------|---------|-----------------|-----------------|-------|
| FM-01 | ShotRequest for agent without possession | INITIATING validation | IDLE; `Outcome = Invalid`; log error; no event | Programming error in Decision Tree |
| FM-02 | `PowerIntent` or `SpinIntent` out of [0.0, 1.0] | INITIATING validation | IDLE; `Outcome = Invalid`; log error; clamp not applied | Clamping would mask Decision Tree bugs silently |
| FM-03 | `PlacementTarget` component outside [0.0, 1.0] | INITIATING validation | IDLE; `Outcome = Invalid`; log error | Out-of-goal-frame target is a caller error |
| FM-04 | Tackle interrupt during WINDUP | State machine each WINDUP frame | IDLE; `Outcome = Cancelled`; `ShotCancelledEvent` | Normal gameplay event — not an error |
| FM-05 | NaN or Infinity in computed velocity or spin | Post-pipeline guard before `ApplyKick()` | `Outcome = Cancelled`; log error; `Ball.ApplyKick()` not called; increment diagnostic counter | Should not occur with valid inputs; guard is safety net |
| FM-06 | Tackle interrupt during CONTACT | State machine CONTACT frame | Ignore interrupt; shot proceeds | Ball leaving foot; cannot be physically cancelled |
| FM-07 | `DistanceToGoal ≤ 0` | INITIATING validation | IDLE; `Outcome = Invalid`; log error | Zero/negative distance causes undefined sigmoid behaviour |
| FM-08 | `ShotRequest` received while state machine is not IDLE | INITIATING check | Reject; log warning; current shot proceeds unaffected | Duplicate Decision Tree submission; not a state machine error |
| FM-09 | `AgentPhysicalProperties` unavailable (null body state) | BodyMechanicsEvaluator | `BodyMechanicsScore = 0.5` (neutral); log warning; proceed | Conservative fallback; does not propagate as error |
| FM-10 | `Ball.ApplyKick()` throws exception | Post-call guard | Log error; increment diagnostic counter; state machine → IDLE | Should not occur; guard for integration safety |

---

## 2.8 Open Dependency Flags

No open dependency flags at Section 2. All hard dependencies confirmed stable in Section 1.
All soft dependencies are forward references to unwritten specifications (Decision Tree #8,
Goalkeeper Mechanics #11, Event System #17) and do not block Sections 2 or 3.

The following forward references are defined in this section but remain unconsumed until
the downstream specification is written:
- `ShotExecutedEvent` — consumed by Goalkeeper Mechanics (#11)
- `ShotAnimationData` — consumed by Animation System (Stage 1+)
- `ShotExecutedEvent.PowerIntent`, `.BodyMechanicsScore` — consumed by Statistics Engine (Stage 1+)

Per project interface principle (Development Best Practices): **no `IGkResponseSystem`
interface will be drafted until Goalkeeper Mechanics Spec #11 is written.**

---

## 2.9 Version History

| Version | Date | Author | Notes |
|---------|------|--------|-------|
| 1.0 | February 22, 2026, 11:30 PM PST | Claude (AI) / Anton | Initial draft. 11 FRs. Full pipeline specified. Data structures defined. Physical parameter reference table included. FM table formalised (10 failure modes). Zero open dependency flags at Section 2. |
| 1.1 | March 6, 2026 | Claude (AI) / Anton | Comprehensive audit corrections: Decision Tree #7→#8, Goalkeeper Mechanics #10→#11 (spec renumbering cascade per PROGRESS.md and Perception System §7). |

---

## Section 2 Summary

| Subsection | Key Content |
|------------|-------------|
| **2.1 Functional Requirements** | FR-01–FR-11: validation, velocity, power–accuracy, launch angle, spin, placement, error, body mechanics, weak foot, state machine, events |
| **2.2 Architecture** | Sub-system decomposition (10 sub-systems); 13-step evaluation pipeline |
| **2.3 Frame Pipeline** | Step 6 of 9; ordering rationale vs. Collision, Ball Physics, Decision Tree |
| **2.4 Data Structures** | `ShotRequest`, `ShotResult`, `ShotExecutedEvent`, `ShotAnimationData` stub |
| **2.5 Physical Parameter Reference** | 6 intent profiles with estimated velocity/angle ranges (all marked `[EST]`) |
| **2.6 NFRs** | 8 non-functional requirements: determinism, performance, testability, memory |
| **2.7 Failure Modes** | FM-01–FM-10: all modes produce valid `ShotResult`; `Ball.ApplyKick()` never called with invalid data |
| **2.8 Dependency Flags** | None open. Forward references documented; no premature interfaces. |

---

*End of Section 2 — Shot Mechanics Specification #6*
*Next: Section 3 — Technical Specifications (§3.1 Validation through §3.10 Event Publishing)*

*Tactical Director — Specification #6 of 20 | Stage 0: Physics Foundation*
