## 4.8 Cross-Specification Validation Checks

Before Section 4 can be approved, the following cross-specification checks must be
completed and signed off. Each check identifies a value or interface in another
specification that must be verified for consistency with this section.

| Check ID | What to Verify | Source Location | Target Location | Blocking? |
|---|---|---|---|---|
| XC-4.2-01 | `AgentState.FacingDirection` is `Vector2` (not `Vector3`) — 2D confirmed for Stage 0 | Agent Movement §3.5.3 | Perception §4.2.1, FovCalculator.cs | ✅ Blocking |
| XC-4.2-02 | Possession state source confirmed: `AgentState` does NOT contain `HasPossession` (Option B, ERR-008). Confirm that simulation scheduler supplies `bool hasPossession` to `ShoulderCheckScheduler` at pipeline start, and that the external possession tracker's interface is documented when fully specified | Ball Physics §3.1.11 (Option B) | Perception §4.2.1, ShoulderCheckScheduler.cs | ⚠ Non-blocking at Stage 0 (bool parameter accepted); blocking before possession system is formally specified |
| XC-4.2-03 | `PlayerAttributes.Decisions` type is `int` [1–20] — not float | Agent Movement §3.5.6 | Perception §4.2.2, RecognitionLatencyTracker.cs | ✅ Blocking |
| XC-4.2-04 | `PlayerAttributes.Anticipation` type is `int` [1–20] — not float | Agent Movement §3.5.6 | Perception §4.2.2, ShoulderCheckScheduler.cs | ✅ Blocking |
| XC-4.3-01 | `BallState.Position` is `Vector3` (confirmed 3D in Ball Physics — 2D projection required) | Ball Physics §3.1.1 | Perception §4.3.1, BallPerceptionEvaluator.cs | ✅ Blocking |
| XC-4.4-01 | `spatialHash.QueryRadius(Vector2, float)` call signature matches §4.4.1 exactly | Collision System §3.1.4 | Perception §4.4.1, PerceptionSystem.cs Step 1 | ✅ Blocking |
| XC-4.4-02 | `spatialHash.QueryRadius(Vector2, float, int)` overload exists for team-filtered queries | Collision System §3.1.4 | Perception §4.4.2, PressureEvaluator.cs | ✅ Blocking — may require Collision System amendment if overload not present |
| XC-4.4-03 | `PRESSURE_RADIUS` value in `PerceptionConstants.cs` matches First Touch §3.5 authoritative value (3.0m) | First Touch §3.5.1 | Perception `PerceptionConstants.cs` [CROSS] tag | ✅ Blocking |
| XC-4.5-01 | `FilteredView` struct field count matches between §3.7.1 (Section 3 v1.3) and §4.5.1 (this section v1.2). **RESOLVED** — both sections define identical `FilteredView` (9 fields) and `PerceptionDiagnostics` (7 fields). Former `PerceptionSnapshot` mismatch (12-vs-14 fields, dual authority claim, naming conflicts) eliminated by architectural rework. | Perception §3.7.1 (v1.3) | Perception §4.5.1 (v1.2) | ✅ Resolved |
| XC-4.6-01 | Ball Physics emits a detectable signal on possession change compatible with `PerceptionRefreshEvent.POSSESSION_CHANGE` trigger | Ball Physics §3.1.11 (possession model) | Perception §4.6.1, §4.6.3 | ⚠ Non-blocking at Stage 0 (stub accepts direct call); blocking before Stage 1 Event System integration |
| XC-4.6-02 | Collision System emits a detectable signal on tackle completion compatible with `PerceptionRefreshEvent.TACKLE_COMPLETION` trigger | Collision System §4 (event model) | Perception §4.6.1, §4.6.3 | ⚠ Non-blocking at Stage 0; blocking before Stage 1 |

**Status key:** ✅ = must be resolved before Section 4 approval. ⚠ = must be resolved
before Stage 1 Event System integration; non-blocking for Stage 0 stub operation.

**Checks XC-4.4-02 note:** If the Collision System's `QueryRadius()` does not have a
team-filter overload, `PressureEvaluator.cs` must implement client-side team filtering
on the unfiltered result. This is a fallback, not a preference. The team-filter overload
is the preferred approach for consistency with the pressure model pattern established
in First Touch (#4) and Pass Mechanics (#5). A Collision System amendment should be
requested if the overload does not exist.

---

## 4.9 Version History

| Version | Date | Author | Changes |
|---|---|---|---|
| 1.0 | February 25, 2026, 12:00 PM PST | Claude (AI) / Anton | Initial draft. All four integration contracts defined. File structure (9 source files, 8 test files). Forced refresh event model formalised. PerceptionRefreshEvent stub defined. Dependency graph complete. Cross-spec validation table (11 checks). |
| 1.1 | February 26, 2026 | Claude (AI) / Anton | Four fixes: (1) Removed `AgentState.HasPossession` — field does not exist; replaced with Option B external possession tracker pattern (XC-4.2-02 updated). (2) Removed reflection-based `GetFrame<T>()` from EventBusStub — replaced with simple type-name log to preserve determinism philosophy. (3) Added explicit Stage 0 coupling note to §4.6.1 documenting that direct-call cross-layer reference is a known shortcut pending Event System (#17). (4) Added amendment warning to §4.5.1 flagging that `ForcedRefreshThisTick` field is absent from Section 3 §3.7.1 — Section 3 v1.2 amendment required before approval; XC-4.5-01 updated to blocking. |
| 1.2 | April 11, 2026 | Claude (AI) / Anton | **Architectural rework — separation of filter output from filter metadata.** (1) §4.5.1 replaced: `PerceptionSnapshot` replaced by `FilteredView` (9 fields — consumer output delivered to DT) and `PerceptionDiagnostics` (7 fields — filter metadata NOT delivered to DT). (2) XC-4.5-01 RESOLVED — former PerceptionSnapshot struct mismatch (12-vs-14 fields, dual authority claim, IsForceRefreshed vs ForcedRefreshThisTick naming conflict) eliminated by architectural rework. Both Section 3 v1.3 and Section 4 v1.2 now define identical structs. (3) File structure updated: `PerceptionSnapshot.cs` → `FilteredView.cs`; `SnapshotBuilder.cs` → `ViewBuilder.cs`. (4) §4.5.2 delivery mechanism updated to reflect FilteredView-only delivery to Decision Tree. (5) §4.5.4 amendment authority updated with lower-impact rule for PerceptionDiagnostics changes. (6) §4.7 dependency graph updated. |

---

*End of Section 4 — Perception System Specification #7*  
*Tactical Director — Specification #7 of 20 | Stage 0: Physics Foundation*
