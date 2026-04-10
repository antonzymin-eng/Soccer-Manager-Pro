# Agent Movement Specification #2 — Comprehensive Audit Report

**Created:** March 2, 2026, Audit Session  
**Purpose:** File-by-file audit of all 14 Agent Movement spec files identifying gaps, errors, inconsistencies, and cross-specification conflicts  
**Auditor:** Claude (AI)  
**Scope:** Sections 1–2 v1.0, Section 3.1 v1.1, Section 3.2 v1.0, Section 3.3 v1.0, Section 3.4 v1.0, Section 3.5 v1.3, Section 3.6 v1.1, Section 3.7 v1.2, Section 4 v1.1, Section 5 v1.1, Section 6 v1.1, Section 7 v1.0, Appendices v1.1, Section 9 Approval Checklist v1.0, FR3 Revision Note

---

## SEVERITY CLASSIFICATION

- **CRITICAL** — Incorrect values that would cause implementation bugs or cross-spec interface failures. Must fix before approval.
- **MAJOR** — Significant inconsistencies between sections that create confusion or incorrect documentation. Should fix before approval.
- **MODERATE** — Stale references, outdated values, or documentation gaps that won't cause implementation failures but reduce spec quality.
- **MINOR** — Cosmetic issues, encoding artifacts, or non-blocking documentation improvements.

---

## FINDING SUMMARY

| Severity | Count |
|----------|-------|
| CRITICAL | 5 |
| MAJOR | 7 |
| MODERATE | 8 |
| MINOR | 6 |
| **TOTAL** | **26** |

---

## CRITICAL FINDINGS

### C-01: SPRINT_EXIT Threshold Conflict (Section 3.1 vs Section 3.5)

**Section 3.1 v1.1** defines: `SPRINT_EXIT = 5.5f` (line 203)  
**Section 3.5 v1.3 MovementConstants** defines: `SPRINT_EXIT_THRESHOLD = 5.2f` (line 1341)

These are 0.3 m/s apart. The dead zone comment in Section 3.5 says `SPRINT: [5.2, 5.8]` while Section 3.1 says `SPRINT: [5.5, 5.8]`. This would cause a direct implementation conflict — the state machine and the constants class disagree on when an agent drops out of SPRINTING.

**Authoritative source:** Section 3.1 (state machine owner). Section 3.5 must be corrected to 5.5f.

**Impact:** State machine behavior would differ depending on which constant file the implementer reads.

---

### C-02: STUMBLE_SPEED_THRESHOLD Conflict (Section 3.1 vs Section 3.4/3.5)

**Section 3.1 v1.1** defines: `STUMBLE_SPEED_THRESHOLD = 5.5f` with comment "Set to SPRINT_EXIT" (line 255)  
**Section 3.4 v1.0** defines: Stumble activates above `JOG_ENTER` (2.2 m/s)  
**Section 3.5 v1.3** defines: `STUMBLE_SPEED_THRESHOLD = 2.2f` with comment "FIXED v1.2: Changed from 3.0 → 2.2 to match Section 3.4" (line 1472)

Section 3.5 v1.2 correctly fixed the value to match Section 3.4, but **Section 3.1 was never updated** from its original 5.5f value. This is a significant gameplay difference: stumble risk activating at 2.2 m/s (jogging) vs 5.5 m/s (sprinting) fundamentally changes how often agents stumble during normal play.

**Authoritative source:** Section 3.4 (stumble mechanics owner). Section 3.1 must be updated.

**Impact:** An implementer reading Section 3.1 would disable stumble risk for all jogging agents — a major gameplay behavior difference.

---

### C-03: Approval Checklist Contains Multiple Incorrect Verification Values

**Section 9 Approval Checklist** line 66: Claims `SPRINT_ENTER threshold in Section 3.1 | 5.5 m/s`  
**Actual value in Section 3.1:** `SPRINT_ENTER = 5.8f`

**Section 9** line 67: Claims `SPRINT_EXIT threshold in Section 3.1 | 5.0 m/s`  
**Actual value in Section 3.1:** `SPRINT_EXIT = 5.5f`

**Section 9** line 69: Claims `MIN_SPEED in Section 3.5 MovementConstants | 6.5 m/s`  
**No such constant exists** in Section 3.5 MovementConstants. TOP_SPEED_MIN = 7.5f exists.

**Section 9** line 73: Claims `K_TURN_MIN in Section 3.5 | 0.08`  
**Actual value in Section 3.5:** `K_TURN_MIN = 0.35f`

**Section 9** line 74: Claims `K_TURN_MAX in Section 3.5 | 0.20`  
**Actual value in Section 3.5:** `K_TURN_MAX = 0.78f`

**Section 9** line 79: Claims `Mass derivation formula: 65 + (Strength - 10) × 1.5 kg`  
**Actual formula in Section 3.5.4.2:** `mass = 70 + (strength / 20) × 30` → range [72.5, 100] kg

**Section 9** line 80: Claims `Hitbox radius: 0.35 + (Height - 175) × 0.001 m`  
**Actual formula in Section 3.5.4.3:** `radius = 0.35 + (strength / 20) × 0.15` → uses Strength, not Height

**Assessment:** The checklist was written with stale or fabricated values and was never actually verified against the spec files. At least 7 of the 31 verification checks contain wrong expected values. The checklist's "☐ PASS" marks are unreliable.

**Impact:** The checklist cannot be used for approval sign-off in its current state. It needs to be completely rebuilt.

---

### C-04: Coordinate System Origin Conflict (Section 3.5 vs Section 3.6 vs Ball Physics)

**Section 3.5 v1.3** (Agent.Position comment, line 166): "Origin at pitch center"  
**Section 3.6 v1.1** (PitchConfiguration): Uses corner-origin (0,0,0) matching Ball Physics  
**Ball Physics Spec #1:** Corner-origin convention

Section 3.6 v1.1 changelog (Issue #3) explicitly flagged this conflict and noted "Section 3.5 flagged for correction in its next revision." However, Section 3.5 advanced from v1.2 to v1.3 (for ERR-007 attributes) **without fixing the coordinate system comment**.

**Impact:** Implementers reading Section 3.5 would place agents relative to pitch center; those reading Section 3.6/Ball Physics would use corner origin. Position calculations would be offset by ~52.5m × 34m.

---

### C-05: Section 3.5 File Version Listed Wrong in Approval Checklist

**Approval Checklist** line 126: Lists `Agent_Movement_Spec_Section_3_5_v1_2.md | 1.2 | ☐`  
**Actual current file:** `Agent_Movement_Spec_Section_3_5_v1_3.md` (v1.3, updated Feb 21 for ERR-007)

The checklist was written before the v1.3 amendment and was never updated. This means the approval gate references a superseded file version.

---

## MAJOR FINDINGS

### M-01: FR-3 Revision Never Applied to Sections 1–2

The `FR3_Revision_Note.md` documents that FR-3 deceleration distances were revised from [2.0–3.0m controlled, 1.0–1.5m emergency] to [3.0–5.0m controlled, 2.5–3.5m emergency]. The note says "Pending — apply to Sections 1-2 when advancing to v1.1."

**Sections 1–2 remain at v1.0** and still contain the original, biomechanically implausible FR-3 values (lines 141–142):
> "Controlled deceleration SHALL bring agent to stop within 2.0–3.0 meters"
> "Emergency deceleration SHALL bring agent to stop within 1.0–1.5 meters"

Section 3.2 uses the revised values correctly, but the requirements document itself contradicts the implementation.

---

### M-02: FR-1 Lists 7 States, Section 3.1 Defines 7 States, But FR-1 Text Says 6

**Sections 1–2** FR-1 (line 125): "System SHALL implement movement state machine: IDLE, WALKING, JOGGING, SPRINTING, DECELERATING, STUMBLING, GROUNDED"

That's actually 7 states listed. But the Section 3.1 description and the Manifest say "6 states" in several places. The actual implementation has 7 states (the enum defines 7). The "6 states" references appear to be counting errors that should say 7.

---

### M-03: Section 1.2 Out-of-Scope Spec Numbers Don't Match Current Numbering

**Section 1.2** references:
- "First Touch Mechanics → Spec #11" — **Actually Spec #4**
- "Goalkeeper Mechanics → Spec #10" — **Actually Spec #11 per development plan**
- "Decision Tree → Spec #7" — **Actually Spec #8**
- "Perception System → Spec #6" — **Actually Spec #7**

These appear to be from an earlier numbering scheme before the spec order was finalized. The numbers are inconsistent with the File Manifest and PROGRESS.md.

---

### M-04: Section 3.1 STUMBLE_TURN_ANGLE vs Section 3.4 Stumble Model Inconsistency

**Section 3.1** defines `STUMBLE_TURN_ANGLE = 60.0f` as "Turn angle beyond which stumble risk is evaluated."

**Section 3.4** uses a completely different stumble trigger model: fraction-based safe zone (`SAFE_FRACTION_MIN/MAX = 0.55/0.85` of max turn rate), not an absolute angle threshold. The stumble system in Section 3.4 triggers when turn rate exceeds the safe fraction of max turn rate, regardless of angle.

The `STUMBLE_TURN_ANGLE` constant in Section 3.1 appears to be a vestigial design from before Section 3.4 was written. It's not referenced in Section 3.4's stumble mechanics at all.

**Impact:** Implementer may try to use both a 60° angle threshold AND the fraction-based model, creating duplicate stumble triggers.

---

### M-05: MAX_LEAN_ANGLE Discrepancy (Section 3.4 vs Section 3.5)

**Section 3.4** defines: `MAX_LEAN_ANGLE = 40.0f` degrees  
**Section 3.5 MovementConstants** defines: `MAX_LEAN_ANGLE = 45f` degrees  

These differ by 5° and neither references the other as authoritative.

---

### M-06: Section 3.2 Validation Example 4 Stopping Distance Range Mismatch

Section 3.2.6 Example 4 claims `Emergency stop dist (Agility 1) | 2.5–3.5m | 3.50m | ✓ PASS (boundary)` and the validation summary says the range is "2.5–3.5m (FR-3 revised)."

But the actual emergency stopping distance at sprint speed (9 m/s) with the derived deceleration rate of 11.57 m/s² is:
`d = v² / (2 × a) = 81 / 23.14 = 3.50m` — this matches.

However, for the controlled stop, the table says "3.0–5.0m (FR-3 revised)" but FR-3 in Sections 1–2 still says 2.0–3.0m (unfixed). This is a consequence of M-01.

---

### M-07: SPRINT_RESERVOIR_REENTRY Conflict (Section 3.1 FR-6 vs Section 3.5)

**Sections 1–2** FR-6 (line 168): `SPRINT_RESERVOIR_REENTRY = 0.35`  
**Section 3.5 v1.3 MovementConstants** (line 1376): `SPRINT_RESERVOIR_REENTRY = 0.20f`

FR-6 specifies 0.35 as the re-entry threshold (must recover above 0.35 before re-entering sprint). Section 3.5 uses 0.20, which is the exit threshold (`SPRINT_RESERVOIR_FLOOR`). The distinction between exit floor (0.20) and re-entry (0.35) is the hysteresis gap — Section 3.5 appears to have used the wrong value.

**Impact:** Sprint hysteresis would not function correctly — agents could immediately re-enter sprint after briefly recovering from 0.20 to 0.21, instead of needing to reach 0.35.

---

## MODERATE FINDINGS

### MOD-01: Section 7 DOIs Still Unverified

All 12+ academic DOIs remain marked "⚠ Verify" as noted since v1.0 (February 15). The approval checklist lists this as a Known Limitation, but the spec has been "In Review" for two weeks with no DOI verification performed. This is a non-blocking but increasingly stale item.

---

### MOD-02: Section 5 Dependencies Reference Section 3.5 v1.2, Not v1.3

Section 5 header (line 9): "Section 3.5 (Data Structures v1.2)" — should be v1.3 since the ERR-007 amendment added 3 attributes that affect the memory size calculations.

Section 5's agent struct memory count (224 bytes) was derived from v1.2's ~80-byte PlayerAttributes. v1.3 increased this to ~84 bytes, making the agent struct approximately 228 bytes. This is within tolerance but technically incorrect.

---

### MOD-03: Appendices Dependencies Also Reference v1.2

Appendices v1.1 header: "Section 3.5 v1.2 (Data Structures)" — should reference v1.3.

---

### MOD-04: Section 1.4 Dependency Spec Numbers

Section 1.4 lists:
- "Pass Mechanics (Spec #4)" — **Actually Spec #5**
- "First Touch Mechanics (Spec #11)" — **Actually Spec #4**

Same numbering issue as M-03 but in the dependencies section.

---

### MOD-05: Section 3.4 Future Extensions Lists "Facing Rate Multiplier" Twice

Lines 1182–1183 and 1188–1189 both describe the Stage 1 facing rate multiplier extension. The first suggests range 1.2–1.5×, the second suggests 1.3–1.5×. Duplicate with minor inconsistency.

---

### MOD-06: Sections 1–2 Status Says "Draft" But Manifest Says "In Review"

Section 1–2 header: `Status: Draft`  
File Manifest: `🔍 In Review`

All section headers still say "Draft" while the manifest has promoted them to "In Review." Headers should be updated.

---

### MOD-07: PlayerAttributes Strength Description Inconsistency

**Section 3.5 PlayerAttributes.Strength** (line 1186): "Affects: agent mass (72.5–100 kg), hitbox radius (0.35–0.50m)"  
**Section 3.5.4.2 Mass formula**: Produces range [72.5, 100] kg — matches  
**Section 3.5.4.3 Radius formula**: Produces range [0.3525, 0.50] m — but description says 0.35–0.50

The min radius at Strength 1 is 0.3525, not 0.35. Minor rounding in the description.

---

### MOD-08: Section 4 Telemetry Event Size Flagged But Section 3.5 Not Corrected

Section 4 v1.1 changelog (Issue #4) identified that MovementTelemetryEvent is ~96 bytes, not ~24 bytes, and "Flagged Section 3.5.7 discrepancy for correction." Section 3.5 advanced to v1.3 without this correction being applied.

---

## MINOR FINDINGS

### MIN-01: UTF-8 Encoding Artifacts Throughout

Multiple sections display garbled characters (e.g., `â€"` instead of `—`, `Ã—` instead of `×`, `â†'` instead of `→`). This affects readability but not correctness. Noted in several changelogs as a known cosmetic issue.

---

### MIN-02: Superseded Files Still in Project

The File Manifest notes `Agent_Movement_Spec_Remaining_Sections_Outline.md` as "Superseded — pending removal." It also notes `Agent_Movement_Spec_Section_3_5_v1_2.md` as pending removal (superseded by v1.3). These should be cleaned up.

---

### MIN-03: Section 3.4 References "Section 3.5" for Fatigue But Fatigue Is Not in Section 3.5

Section 3.4 cross-reference table (line 1175): "Section 3.5 (Fatigue)" — but Section 3.5 is Data Structures. The fatigue model is described within Section 3.1 (energy pool definitions) and Section 3.2 (PerformanceContext modifiers). There is no standalone fatigue section in the Agent Movement spec.

---

### MIN-04: Section 3.3 Backward Zone Boundary Name Inconsistency

Section 3.3 defines both `BACKWARD_START = 90.0f` and `BACKWARD_FULL = 90.0f` as separate constants with the same value. The intent is that the interpolation band runs from `LATERAL_LIMIT (80°)` to `BACKWARD_START (90°)`, and `BACKWARD_FULL` marks where the backward multiplier is fully applied. Having two constants at 90° is confusing — one could be removed.

---

### MIN-05: Section 6.4 Cross-Spec Dependencies Table Outdated

The approval checklist's cross-spec dependencies table (line 171) lists:
- "Fatigue System (Spec #13)" — Spec #13 is actually Pressing AI per the development plan
- "First Touch Mechanics (Spec #11)" — Should be Spec #4

---

### MIN-06: No Section 8 (References for Section 9 Template Compliance)

The standard template is 9 sections. Agent Movement uses Section 7 for references and Section 9 for the approval checklist. There is no Section 8. While this doesn't affect content, it breaks template compliance and differs from other approved specs (Ball Physics, Collision System, etc.) which all have Section 8 for references.

---

## RECOMMENDATIONS

### Blocking (Must Fix Before Approval)

1. **Rebuild Approval Checklist (Section 9)** from scratch with correct values verified against actual file contents. At least 7 of 31 checks contain wrong expected values. This is the most urgent fix.

2. **Fix SPRINT_EXIT in Section 3.5**: Change from 5.2 → 5.5 to match Section 3.1 authoritative value.

3. **Fix STUMBLE_SPEED_THRESHOLD in Section 3.1**: Change from 5.5 → 2.2 to match Section 3.4/3.5 authoritative value. Remove or deprecate STUMBLE_TURN_ANGLE.

4. **Fix coordinate system comment in Section 3.5**: Change "Origin at pitch center" → "Origin at corner (0,0,0)" per Ball Physics convention and Section 3.6.

5. **Apply FR-3 revision to Sections 1–2**: Advance to v1.1 with revised deceleration distances [3.0–5.0m, 2.5–3.5m].

6. **Fix SPRINT_RESERVOIR_REENTRY in Section 3.5**: Should be 0.35 (re-entry) not 0.20 (exit floor), or add both constants with clear naming.

### Recommended (Should Fix)

7. Correct all spec numbers in Sections 1–2 to match current numbering scheme.
8. Fix MAX_LEAN_ANGLE conflict (40° vs 45°) — decide authoritative value.
9. Remove duplicate facing rate multiplier entry in Section 3.4.
10. Update all section headers from "Draft" → "In Review."
11. Update dependency references to Section 3.5 v1.3 in Sections 5 and Appendices.

### Deferred (Can Fix Post-Approval)

12. Verify all DOIs in Section 7.
13. Clean up superseded files from project.
14. Fix UTF-8 encoding artifacts in a final export pass.
15. Correct telemetry event size in Section 3.5.7.

---

## AUDIT VERDICT

**Status: NOT READY FOR APPROVAL**

The specification contains 5 critical issues that would cause implementation bugs, most notably the SPRINT_EXIT threshold conflict (C-01) and the fundamentally broken approval checklist (C-03). The checklist — the document that is supposed to verify internal consistency — itself contains at least 7 incorrect values, meaning it was never actually verified against the spec files.

The core physics models (Sections 3.2–3.4) are internally consistent and well-derived. The problems are concentrated in three areas: (1) Section 3.1 lagging behind corrections made in 3.4/3.5, (2) Sections 1–2 never being updated for the FR-3 revision, and (3) the approval checklist being written from memory rather than verified.

Estimated effort to reach approval-ready state: 2–3 focused working sessions to fix all blocking items, rebuild the checklist, and perform a final cross-verification pass.

---

**END OF AUDIT REPORT**
