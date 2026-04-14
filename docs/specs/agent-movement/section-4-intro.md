# Agent Movement Specification â€” Section 4: Implementation Details

**Purpose:** Defines the code organization, file structure, dependencies, configuration management, execution order, memory layout, and profiling strategy for the Agent Movement System. This section translates the physics models from Sections 3.1â€“3.6 into an implementable architecture. All file names, class responsibilities, and execution sequences defined here are binding for Stage 0 implementation.

**Created:** February 13, 2026, 11:45 PM PST  
**Updated:** February 14, 2026, 12:15 AM PST  
**Version:** 1.1 (Revised)  
**Status:** Draft  
**Dependencies:** Section 3.1 (State Machine), Section 3.2 (Locomotion), Section 3.3 (Directional Movement), Section 3.4 (Turning & Momentum), Section 3.5 (Data Structures), Section 3.6 (Edge Cases & Error Handling), Ball Physics Spec #1 Section 4 (architectural precedent)

---

## v1.1 Changelog

**CRITICAL FIXES:**

1. **Namespace inconsistency with Ball Physics (Issue #1):** Changed from `TacticalDirector.Physics.Agent` â†’ `TacticalDirector.Core.Physics.Agent` to match Ball Physics Spec #1's established `TacticalDirector.Core.Physics.Ball` convention. Common namespace also updated.

2. **PerformanceContext.InvalidateCache() phantom method (Issue #2):** Removed references to `InvalidateCache()` which does not exist on the PerformanceContext struct (Section 3.5.6). PerformanceContext is a plain value type â€” `EvaluateAttribute()` multiplies raw attribute by the three modifier floats with no caching layer. The 10Hz heartbeat simply overwrites the `FatigueModifier` float directly. Section 4.4.3 rewritten to reflect actual struct semantics.

3. **Missing Match Simulator integration point (Issue #3):** Added Section 4.4.4 with concrete `UpdateAllAgents()` entry point signature and Match Simulator call site example, following Ball Physics Section 4.4.1 pattern. Shows goalkeeper skip logic, tactical tick coordination, and frame-level system ordering.

**MODERATE FIXES:**

4. **Telemetry event size undercount (Issue #4):** MovementTelemetryEvent actual size is ~96 bytes (9 fields including embedded MovementCommand), not ~24 bytes (4 fields). Section 3.5.7's inline estimate counted only 4 of 9 fields. Corrected all downstream calculations: per-agent buffer ~93.75 KB (was ~23.4 KB), total footprint ~2.07 MB (was ~521 KB). Flagged Section 3.5.7 discrepancy for correction. Added design concern note about embedded Command struct's string reference creating GC pressure.

5. **WALK_ENTER_THRESHOLD missing from constants table (Issue #5):** Added to Section 4.3.1 state machine constants table with note that it equals IDLE_EXIT_THRESHOLD (0.3 m/s).

6. **Stumble RNG ownership unclear (Issue #6):** Added Step 6b to pipeline showing that the main loop owns the RNG roll between Steps 6 and 7. AgentTurning returns probability only (pure function). If roll succeeds, a pending stumble flag is set and consumed at Step 2 on the next frame. Current frame continues with unchanged state.

7. **Deceleration mode missing from pipeline (Issue #7):** Step 4 now shows `cmd.DecelerationMode` as a parameter to `CalculateAcceleration()` with inline note that the function handles both acceleration (exponential approach) and deceleration (constant-force braking, mode-dependent) cases.

**MINOR FIXES:**

8. **PlayerAttributes size hedge (Issue #8):** Committed to 60 bytes (15 ints Ã— 4B) as Stage 0 reality. Section 3.5's ~80B estimate noted separately as forward projection including future attribute fields and alignment. Per-agent total pinned at ~240 bytes.

9. **Editor-vs-release telemetry behavior (Issue #9):** Added note in Section 4.5.2 documenting that release builds log state transitions only (~5â€“20 events/agent/minute) vs. editor's full 60Hz capture. Effective release-mode telemetry footprint is under 50 KB total.

10. **Validation check #5 deceleration ordering (Issue #10):** Expanded from single cross-mode check to three checks: min < max within each mode, plus emergency max < controlled min (emergency stops are always shorter).

---

