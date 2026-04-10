# Ball Physics Specification #1 — Comprehensive Audit Report

**Created:** March 2, 2026, 11:18 AM PST  
**Auditor:** Claude (AI)  
**Scope:** All Ball Physics Spec files (Sections 1–2, 3.1, 4, 5, 6, 7, 8, Appendices A–C, Approval Checklist)  
**Purpose:** File-by-file audit identifying gaps, errors, inconsistencies, and stale references across the entire Ball Physics specification

---

## EXECUTIVE SUMMARY

The Ball Physics Specification is **mature and well-maintained**, with strong internal consistency resulting from multiple revision passes (Section 3.1 at v2.5, Section 8 at v1.4). However, this audit identifies **6 issues requiring attention** (1 moderate, 5 minor) and **8 observations** worth documenting for future reference.

No critical or blocking issues were found. The specification is implementation-ready.

| Severity | Count | Description |
|----------|-------|-------------|
| **Critical** | 0 | No blocking issues |
| **Moderate** | 1 | Version drift between approval checklist and current file versions |
| **Minor** | 5 | Stale cross-references, encoding artifacts, minor numerical discrepancies |
| **Observation** | 8 | Non-actionable items; design notes for implementation phase |

---

## AUDIT FINDINGS

### AUD-001: Approval Checklist References Stale File Versions

**Severity:** Moderate  
**File:** `Ball_Physics_Spec_1_Approval_Checklist.md`  
**Details:**

The approval checklist (approved February 8, 2026) records file versions that have since been superseded by post-approval amendments:

| File | Checklist Version | Current Version | Delta |
|------|------------------|-----------------|-------|
| Section 3.1 | v2.4 | **v2.5** | §3.1.11.1/11.2 added (ERR-006/ERR-008 resolution) |
| Section 8 | v1.2 | **v1.4** | DOI corrections, ERR-006 cross-ref fix, spin decay audit |
| Appendices | v1.2 | v1.2 | Match ✅ |
| Sections 1–2 | v1.2 | v1.2 | Match ✅ |
| Section 4 | v1.1 | v1.1 | Match ✅ |
| Section 5 | v1.1 | v1.1 | Match ✅ |
| Section 6 | v1.0 | v1.0 | Match ✅ |
| Section 7 | v1.0 | v1.0 | Match ✅ |

**Impact:** The checklist's version table is a historical approval record and doesn't need re-approval, but the post-approval changes (§3.1.11.2 `ApplyKick()` addition and §8 DOI corrections) were made to resolve ERR-006/ERR-008 and are properly logged in the error log. The disconnect is cosmetic but could cause confusion during implementation if someone treats the checklist as the current file manifest.

**Recommendation:** Add a note to the checklist indicating post-approval amendments were applied to Section 3.1 (v2.4→v2.5) and Section 8 (v1.2→v1.4) with cross-reference to Spec_Error_Log entries ERR-006 and ERR-008.

---

### AUD-002: Section 6 Dependencies Reference Stale Section 3.1 Version

**Severity:** Minor  
**File:** `Ball_Physics_Spec_Section_6_v1_0.md`  
**Details:**

The header states: "Dependencies: Section 3.1 (Core Formulas v2.3)". Section 3.1 is now at v2.5. The operation counts in §6.1.2–6.1.4 were derived from v2.3 code, which did not include `ApplyKick()` (§3.1.11.2, added in v2.5).

**Impact:** Low. `ApplyKick()` is an instantaneous operation called by external systems, not part of the per-frame update loop. It does not affect the per-frame operation counts or the performance analysis. However, the dependency header is technically inaccurate.

**Recommendation:** Update the dependency line to "Section 3.1 (Core Formulas v2.5)" and add a one-line note that `ApplyKick()` (v2.5 addition) is excluded from per-frame analysis as it's event-driven.

---

### AUD-003: Section 7 Dependencies Reference Stale Section 3.1 Version

**Severity:** Minor  
**File:** `Ball_Physics_Spec_Section_7_v1_0.md`  
**Details:**

Header states: "Dependencies: Section 3.1 (Core Formulas v2.3)". Same issue as AUD-002.

**Recommendation:** Update to "Section 3.1 (Core Formulas v2.5)".

---

### AUD-004: Appendices Dependencies Reference Stale Versions

**Severity:** Minor  
**File:** `Ball_Physics_Spec_Appendices_v1_2.md`  
**Details:**

Header states: "Dependencies: Sections 1-2 v1.1, Section 3.1 v2.3, ... Section 8 v1.2". Multiple version references are stale:

- Sections 1–2 is v1.2 (not v1.1)
- Section 3.1 is v2.5 (not v2.3)
- Section 8 is v1.4 (not v1.2)

**Recommendation:** Update all dependency versions in the header.

---

### AUD-005: UTF-8 Encoding Corruption Throughout Specification

**Severity:** Minor (cosmetic)  
**Files:** All files  
**Details:**

Extensive UTF-8 encoding corruption is present across all files. Mathematical symbols, arrows, checkmarks, and special characters are rendered as mojibake sequences. Examples:

| Intended Character | Rendered As |
|---|---|
| × (multiplication) | `Ã—` or `Ãƒâ€"` |
| → (arrow) | `â†'` or `Ã¢â€ â€™` |
| ≈ (approximately) | `â‰ˆ` or `Ã¢â€°Ë†` |
| ✓ / ✗ (check/cross) | `âœ"` / various garbled |
| ω (omega) | `Ãâ€°` or `Ï‰` |
| μ (mu) | `Î¼` or `ÃŽÂ¼` |
| π (pi) | `Ãâ‚¬` |
| ρ (rho) | `ÃÂ` or `Ï` |

**Impact:** All mathematical formulas and symbol-heavy content is visually degraded. The actual content is correct — this is a display/storage encoding issue, not a content error. Section 8 preamble already acknowledges this with a UTF-8 rendering note.

**Recommendation:** Before implementation begins, perform a batch encoding normalization pass on all files. This is a one-time text processing task. The content is sound; only the byte representation needs fixing.

---

### AUD-006: `BallState` Struct Size Discrepancy

**Severity:** Minor  
**Files:** Section 3.1 (v2.5) §3.1.1, Section 4 (v1.1) §4.5.1, Section 6 (v1.0) §6.3.1  
**Details:**

Multiple sections state `BallState` is 56 bytes:

- Section 3.1: struct comment says "56 bytes struct"
- Section 4 §4.5.1: "Total: 56 bytes"
- Section 6: references 56-byte struct

However, the actual field layout:

| Field | Size |
|---|---|
| Position (Vector3) | 12 bytes |
| Velocity (Vector3) | 12 bytes |
| AngularVelocity (Vector3) | 12 bytes |
| State (BallStateType enum) | 4 bytes |
| LastValidPosition (Vector3) | 12 bytes |
| LastValidVelocity (Vector3) | 12 bytes |
| **Total** | **64 bytes** |

Section 4 §4.5.1 itself notes "Padding: 4 bytes (aligns to 60 bytes)" — but the raw sum is 64 bytes, not 56. This confusion likely arose from an earlier version of the struct that lacked LastValidVelocity. The struct does still fit in a single 64-byte cache line, so the design rationale remains valid.

**Recommendation:** Correct the size references from "56 bytes" to "64 bytes" in Section 3.1, Section 4, and Section 6. The cache-line optimization claim remains correct.

---

## OBSERVATIONS (Non-Actionable)

### OBS-001: Section 4 Agent Interface Uses Undeclared `ball.IsPossessed` Property

**File:** `Ball_Physics_Spec_Section_4_v1_1.md`, §4.4.3  
**Details:** The agent system interface example (line 314) references `ball.IsPossessed`, which does not exist on the `BallState` struct. Per the Option B design (ERR-008 resolution in §3.1.11), possession is tracked externally by the agent system, not in `BallState`. This code snippet is illustrative, not normative, but it contradicts the approved possession model.

**Note:** This is a documentation-only issue in an example snippet. The actual interface contract (§3.1.11) is correct.

---

### OBS-002: `ApplyKick()` File Listing Gap in Section 4

**File:** `Ball_Physics_Spec_Section_4_v1_1.md`, §4.1.1  
**Details:** The file structure tree in §4.1.1 does not include `ApplyKick()` under `BallCollision.cs` or `BallPhysicsCore.cs`. This method was added in §3.1.11.2 (v2.5) after Section 4 was finalized (v1.1). The file tree should be updated to reflect where `ApplyKick()` will live.

---

### OBS-003: Section 5 Test Count vs. Test IDs

**File:** `Ball_Physics_Spec_Section_5_v1_1.md`  
**Details:** Section 5.1.1 claims "32+" unit tests and the completion status says "32 unit + 12 integration." Counting the individual test IDs:

- Magnus: 6 (UT-MAG-001 through 006)
- Drag: 4 (UT-DRG-001 through 004)
- Bounce: 6 (UT-BNC-001 through 006)
- State Machine: 8 (UT-STM-001 through 008)
- Validation: 5 (UT-VAL-001 through 005)
- Spin Decay: 3 (UT-SPN-001 through 003)
- **Total: 32** ✅ Match confirmed

Integration tests: 3 trajectory + 2 multi-bounce + 2 state sequence + 3 collision + 1 logging = **11**, not 12 as claimed.

**Note:** The claim of "12 integration tests" appears to be a count error. There are 11 integration test IDs. This is cosmetic — 11 still far exceeds the minimum requirement.

---

### OBS-004: Drag on Rolling Ball — C_d Value Not Explicit

**File:** `Ball_Physics_Spec_Section_3_1_v2_5.md`, §3.1.8.2 and §3.1.9  
**Details:** When calculating drag on a ROLLING ball (§3.1.9, line 1222), the code calls `CalculateDragForce(relativeVelocity)`, which internally calls `GetDragCoefficient(speed)`. For rolling balls, the relevant drag model may differ from airborne drag since the ball is in ground contact with different wake characteristics. However, the current implementation applies the same drag coefficient model (including drag crisis transition at 20–25 m/s) regardless of whether the ball is airborne or rolling.

**Note:** This is physically acceptable for Stage 0. Rolling drag at typical ground speeds (1–15 m/s) always uses the laminar coefficient (0.20), so the drag crisis region is rarely encountered in the ROLLING state. No change needed.

---

### OBS-005: `BallEventLogger` Uses Unbounded `List<BallEvent>`, Not Ring Buffer

**File:** `Ball_Physics_Spec_Section_3_1_v2_5.md`, §3.1.13  
**Details:** Section 2.2 PR-2 states "Event logger SHALL use fixed-size ring buffer (bounded memory growth)." However, the implementation in §3.1.13 uses `private readonly List<BallEvent> _events = new()` which is an unbounded list. Section 7 §7.8 KR-5 also discusses ring buffer contention, implying it should be a ring buffer.

**Note:** For Stage 0 (single ball, single match), an unbounded list is acceptable since event count per match is bounded by match duration. Over a 90-minute match at worst case, events total ~5,400 snapshots + ~500 discrete events ≈ 6,000 events × ~44 bytes = ~264 KB. However, this contradicts the requirement as written. During implementation, either update the requirement or implement an actual ring buffer.

---

### OBS-006: Section 8 Rolling Friction Citation Audit Shows Original µ_r Values

**File:** `Ball_Physics_Spec_Section_8_v1_4.md`, §8.6.4  
**Details:** The citation audit table in §8.6.4 lists original literature values:

- `µ_r = 0.05 (dry short grass)` — from CARRE-2010, Table 3
- `µ_r = 0.03 (wet grass)` — from CARRE-2010, Table 3
- `µ_r = 0.10 (long grass)` — from CARRE-2010, Table 3

But the implemented values (per REV-001) are:

- GRASS_DRY: 0.13
- GRASS_WET: 0.07
- GRASS_LONG: 0.22

The audit table correctly shows the literature source values, not the gameplay-tuned values. This is not an error — the audit's purpose is to trace sources. However, it could be confusing because the reader might expect the audit table to show current implementation values.

**Note:** Consider adding a "Note: Implementation values differ from literature — see §3.1.2 REV-001" annotation to the audit table header.

---

### OBS-007: Spec Number Discrepancy — Pass Mechanics vs. Shot Mechanics

**File:** `Ball_Physics_Spec_Sections_1_2_v1_2.md`, §1.2  
**Details:** Section 1.2 states "Pass Mechanics Spec #4, Shot Mechanics Spec #5." Per the project's FILE_MANIFEST and PROGRESS tracking, the canonical spec numbers are:

- Pass Mechanics: Spec #5
- Shot Mechanics: Spec #6

The Ball Physics spec was written early when the numbering may have been different. This matches ERR-010 in the error log (Shot Mechanics §1.1 refers to Decision Tree as Spec #7 when canonical is #8).

**Note:** This is a pre-existing numbering confusion from early project days. Should be corrected in a batch pass across all files.

---

### OBS-008: Hysteresis Test Values vs. Threshold Constants

**File:** `Ball_Physics_Spec_Section_3_1_v2_5.md`, §3.1.14 test code  
**Details:** The hysteresis test `StateTransition_Hysteresis_PreventsOscillation` (line 2028) places the ball at `Position.z = 0.055f` and comments "Between thresholds." The thresholds are:

- AIRBORNE_EXIT_THRESHOLD = 0.13m (ball center)
- AIRBORNE_ENTER_THRESHOLD = 0.17m (ball center)

Position.z = 0.055m is **below** both thresholds, not between them. The ball's resting ground level is RADIUS = 0.11m. The hysteresis zone is 0.13m–0.17m. The test value of 0.055m appears to be from a pre-v2.2 era when thresholds were relative to z=0, not RADIUS.

The subsequent test lines at 0.065m and 0.045m have the same issue — they don't actually fall within the hysteresis band.

**Impact:** The test logic itself is sound (it tests that a ROLLING ball below ENTER threshold stays ROLLING), but the comments are misleading. The "between thresholds" comment is incorrect for these values.

**Recommendation:** Update the test to use values actually within the hysteresis band (0.13m–0.17m), e.g., 0.15m for "between thresholds."

---

## CROSS-SPECIFICATION CONSISTENCY CHECK

| Check | Result |
|---|---|
| `BallState` struct fields match across §3.1.1 and §4.5.1 | ✅ Match (field order, types, names) |
| Surface property values match between §3.1.2 table and `SurfaceProperties` switch | ✅ All 5 surfaces × 4 properties consistent |
| Constants in §3.1.2 match §3.1.12 limits table | ✅ All values match |
| Rolling distance 26–31m consistent across QR-1, §3.1.14, §5 IT-TRJ-003, Appendix D.9, Appendix B.4 | ✅ Consistent (REV-001 applied everywhere) |
| µ_r = 0.13 for GRASS_DRY consistent across §3.1.2 constants, §3.1.2 table, `GetRollingResistance()`, §3.1.14 derivation | ✅ Consistent |
| `ApplyKick()` signature in §3.1.11.2 matches §8.3.5 contract | ✅ Match (fixed in v1.3/v2.5) |
| Possession model (Option B) consistent between §3.1.11 preamble, §3.1.11.1, §3.1.11.2 | ✅ Consistent |
| State machine transitions match physics-by-state table (§3.1.3) | ✅ All 6 states correctly mapped |
| Performance targets: §2.2 PR-1 matches §6.2.1 matches §4.6.2 | ✅ All state <0.5ms target, 0.3ms average |
| CoR = 0.65 for dry grass: §3.1.2 constants vs. `SurfaceProperties` vs. bounce test vs. Appendix A.3 | ✅ Consistent |
| GoalPost CoR = 0.75: §3.1.2 constants vs. `ApplyGoalPostCollision()` code | ✅ Consistent |
| Bounce position clamped to RADIUS: §3.1.8.1 code vs. §5 UT-BNC-004 | ✅ Consistent |

---

## SUMMARY OF REQUIRED ACTIONS

| ID | Severity | Action | Status |
|---|---|---|---|
| AUD-001 | Moderate | Add post-approval amendment note to checklist | **FIXED** — Checklist updated |
| AUD-002 | Minor | Update Section 6 dependency version | **FIXED** — Header updated |
| AUD-003 | Minor | Update Section 7 dependency version | **FIXED** — Header updated |
| AUD-004 | Minor | Update Appendices dependency versions | **FIXED** — Header updated |
| AUD-005 | Minor | Batch UTF-8 encoding normalization (all files) | **DEFERRED** — Cosmetic; batch fix at implementation start |
| AUD-006 | Minor | Correct BallState struct size (56→64) in 3 files | **FIXED** — Section 4 corrected; Section 6 corrected |

**Additional fixes applied beyond original audit scope:**

| Fix | File | Description |
|---|---|---|
| Hysteresis test values | Section 3.1 v2.5→v2.6 | Test positions updated from pre-v2.2 (z=0 ground) to current (z=RADIUS ground) |
| File tree gap | Section 4 v1.1→v1.2 | Added ApplyKick() and SetBallControlled() to §4.1.1 |
| Agent interface | Section 4 v1.1→v1.2 | Replaced `ball.IsPossessed` with `ball.State != CONTROLLED` (Option B) |
| Spec numbers | Sections 1-2 v1.2→v1.3, Section 4 v1.1→v1.2 | Pass Mechanics #4→#5, Shot Mechanics #5→#6 |

---

**END OF AUDIT REPORT**
