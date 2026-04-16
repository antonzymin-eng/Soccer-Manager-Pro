# Agent Movement Specification â€” Section 5: Performance Analysis

**Purpose:** Authoritative performance analysis for the Agent Movement System â€” computational complexity, memory budget, profiling targets, and optimization roadmap. All operation counts are derived line-by-line from the implementations in Sections 3.1â€“3.6, following the methodology established in Ball Physics Spec #1 Section 6.

**Created:** February 13, 2026, 11:30 PM PST  
**Updated:** March 4, 2026, 12:00 AM PST  
**Version:** 1.2  
**Status:** Draft  
**Dependencies:** Section 3.1 (State Machine v1.2), Section 3.2 (Locomotion v1.0), Section 3.3 (Directional Movement v1.0), Section 3.4 (Turning v1.0), Section 3.5 (Data Structures v1.4), Section 3.6 (Edge Cases v1.1), Ball Physics Spec #1 Section 6 (pattern reference, budget allocation)

---

## CHANGELOG v1.0 â†’ v1.1

**CRITICAL FIXES:**

1. **IDLE turning cost corrected (Issue #1):** v1.0 showed IDLE turning as 0 ops (assumed pipeline skip). Corrected: IDLE agents still execute `CalculateMaxTurnRate()` (returns 720Â°/s) and one `ApplyTurnRateLimit()` for facing rotation (TARGET_LOCK mode). IDLE equiv ops: 46 â†’ **87**. STUMBLING/GROUNDED also corrected from 0 â†’ **19 float ops** (function calls execute but produce zero output via modifier).

2. **WALKING directional efficiency note added (Issue #2):** Full directional pipeline executes for WALKING but result is nearly always 1.0 (forward zone). Flagged as "correct but wasteful" â€” feeds P1-C optimization justification.

3. **Safety validation branch count corrected (Issue #3):** 35 â†’ **33** branches. Detailed per-sub-function breakdown added (HasInvalidValues: 22, ApplySafetyClamps: 4, EnforcePitchBoundaries: 6, UpdateLastValidState: 1).

4. **OscillationGuard amortized cost added (Issue #4):** ~12 float ops + ~10 branches per state transition. Amortized to ~1.0 op/frame at 5 transitions/second. Explicitly noted as excluded from per-state table but required in profiling.

**MAJOR FIXES:**

5. **Agent struct memory count rebuilt from Section 3.5 v1.2 (Issue #5):** Replaced grouped estimates with field-by-field count matching authoritative Agent class definition. 220 â†’ **224 bytes** per agent. Includes class object header (16 bytes), PlayerAttributes (60 bytes for 15 int fields), and all fields from Section 3.5.1. Cross-reference note added for drift prevention.

6. **STUMBLING 3% estimate flagged as likely overestimate (Issue #6):** Added caveat explaining 3% implies ~162 seconds stumbling per agent per match (60â€“180 stumble events). Noted actual rate likely 1â€“1.5% pending AI behavior tuning.

7. **LOD determinism resolution path specified (Issue #7):** LOD tier assignment is deterministic (pure function of ball/agent positions). Added blend protocol (3â€“5 frame lerp) for tier transitions. Replay/multiplayer synchronization preserved.

8. **Budget utilization finding made prominent (Issue #8):** Added KEY FINDING callout: expected ~2â€“8% utilization of 3.0ms budget. Surplus (~2.75â€“2.95ms) available for reallocation. This should inform Spec #3 and #7 budget negotiations.

**MINOR FIXES:**

9. **GROUNDED integration cost corrected (Issue #9):** Was 0 ops in v1.0 table, corrected to 12 ops (Euler integration still executes; velocity is zero so position unchanged but multiplies still fire).

10. **P2-C Section 3.5 amendment requirement noted (Issue #10):** `StableFrameCount` field requires Section 3.5 amendment if optimization is needed.

11. **Footer next section corrected (Issue #11):** Changed from ambiguous "Section 4 or Appendix A" to "Section 4 (Implementation Details)" per confirmed writing order.

12. **Match-level totals updated:** All state equiv ops updated (IDLE: 46â†’87, STUMBLING: 51â†’70, GROUNDED: 39â†’70). Match total: 1.044B â†’ **1.096B** equiv ops. Safety branch totals: 35â†’33 throughout.

---

## CHANGELOG v1.1 -> v1.2 (March 4, 2026)

**MINOR FIXES:**

1. **Dependency version references updated:** Section 3.1 v1.0 -> v1.2, Section 3.5 v1.2 -> v1.4. Agent struct memory estimate note: PlayerAttributes increased from ~80 to ~84 bytes in v1.3, making per-agent total approximately 228 bytes (was 224). Within tolerance of 256-byte PR-2 budget.

