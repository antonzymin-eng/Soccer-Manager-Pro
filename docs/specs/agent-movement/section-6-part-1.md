# Agent Movement Specification â€” Section 6: Future Extensions

**Purpose:** Authoritative roadmap for all planned agent movement extensions â€” what changes at each stage, what architectural hooks exist today, what risks each extension introduces, and what is permanently excluded  
**Created:** February 14, 2026, 12:00 PM PST  
**Updated:** February 14, 2026, 1:30 PM PST  
**Version:** 1.1  
**Status:** Draft  
**Dependencies:** Section 1.2 (Out of Scope), Section 3.1 (State Machine), Section 3.2 (Locomotion Model), Section 3.3 (Directional Movement), Section 3.4 (Turning Mechanics), Section 3.5 (Data Structures), Section 4 (Implementation Details), Section 5 (Performance Analysis â€” pending), Ball Physics Spec #1 Section 7 (Future Extensions), Master Vol 1 (Physics Core), Master Vol 2 (Human Systems), Master Development Plan v1.0

---

## CHANGELOG v1.0 â†’ v1.1

**CRITICAL FIXES:**

1. **Added Stage 3â€“4 Extensions (Issue #1):** New section 6.3 covers Stage 3 (Management Depth, Year 5â€“6) and Stage 4 (Human Systems, Year 7â€“8) extensions including form modifier activation, psychology system integration, and injury system hooks. Stage 5 renumbered to 6.4.

2. **Flagged LOD distance thresholds as placeholders (Issue #2):** LOD tier boundaries (20m/40m) now marked with âš ï¸ PLACEHOLDER warning and include derivation rationale based on pitch geometry (penalty area 16.5m, half-pitch 52.5m).

3. **Added Section 5 pending notes (Issue #3):** All cross-references to Section 5 now include "(pending Section 5 completion)" qualifier. Performance budget values flagged as provisional.

**MODERATE FIXES:**

4. **Strengthened Kinetic Profile deferral rationale (Issue #4):** Section 6.2.3 now includes quantitative analysis showing Â±5% effect is below perceptual threshold, with explicit criteria for revisiting in Stage 3.

5. **Added psychology modifier activation (Issue #5):** New section 6.3.2 documents when PsychologyModifier transitions from 1.0 (neutral) to active values, including integration with Master Vol 2 H-Gate system.

6. **Specified BASE_WIND_COST constant (Issue #6):** Section 6.2.2 now defines `BASE_WIND_COST = 0.05` fatigue units per second per m/s headwind, with derivation rationale.

7. **Qualified Section 5 cross-references (Issue #7):** All references to Section 5.3, 5.6 now note "pending Section 5 completion" with placeholder values clearly marked.

**MINOR FIXES:**

8. **Aligned stage/year terminology (Issue #8):** All stage references now use "Stage N (Year Xâ€“Y)" format matching Master Development Plan exactly.

9. **Added goalkeeper exclusion to 6.4 table (Issue #9):** Permanently Excluded Features table now includes "Goalkeeper-specific movement" with cross-reference to Spec #11.

10. **Flagged LOD distribution estimate (Issue #10):** Agent distribution (6/8/8) now marked as âš ï¸ PLACEHOLDER with note that actual distribution depends on match ball position analysis.

---

