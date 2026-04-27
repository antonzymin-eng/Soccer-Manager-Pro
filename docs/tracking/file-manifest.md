# Tactical Director — File Manifest

**Created:** February 8, 2026, 11:15 PM PST  
**Last Updated:** April 22, 2026  
**Purpose:** Authoritative inventory of all project files with version tracking, status, and categorization  
**Maintained by:** Lead Developer  
**Update frequency:** After every file addition, removal, or version change

> **⚠ NAMING CONVENTION MIGRATION NOTE (April 2026):** This manifest was originally
> written using the flat-file naming convention (`Spec_Name_Section_N_v1_0.md`). The
> project has since migrated to a per-spec folder structure (`spec-name/section-N.md`
> with no version suffixes — Git tracks history). File listings in this manifest still
> show the **pre-migration names**. When cross-referencing against actual repo files,
> use the folder-based names in `docs/specs/<spec-name>/`. The **Pending Removal**
> section in particular refers to pre-migration filename aliases that may no longer
> exist under those names; verify against the actual repo before acting.
>
> **Action required:** Update this manifest to use the new folder-based naming convention
> once the file migration is confirmed complete. Until then, treat all filenames here as
> pre-migration legacy references.

---

## MANIFEST RULES

1. Only **current versions** belong in the project root — superseded versions must be deleted or moved to an archive
2. Every new file gets an entry here before being added to the project
3. Version bumps require updating this manifest in the same commit
4. Files removed from the project are logged in the Removal History at the bottom

---

## PROJECT INFRASTRUCTURE

| File | Version | Size | Description | Status |
|------|---------|------|-------------|--------|
| README.md | — | 13K | Project overview, vision, and quick-start guide | Active |
| PROGRESS.md | — | ~18K | Stage 0 specification progress tracker and weekly log | Active |
| FILE_MANIFEST.md | — | — | This file; authoritative file inventory | Active |
| Development_Best_Practices.md | — | 23K | Process standards, quality gates, review procedures | Active |
| Spec_Error_Log_v1_2.md | 1.2 | ~18K | Cross-specification error log; all critical errors resolved | Active |

---

## MASTER PLANNING DOCUMENTS

| File | Version | Size | Description | Status |
|------|---------|------|-------------|--------|
| Master_Development_Plan_v1_0.md | 1.0 | 38K | 10-year development roadmap across 6 stages | Active |
| Master_Vol_1_Physics_Core.md | — | 25K | Physics Core master volume — ball, player, collision systems | Active |
| Master_Vol_2_Human_Systems.md | — | 19K | Human Systems master volume — AI, perception, decision-making | Active |
| Master_Vol_3_Club_Operations.md | — | 20K | Club Operations master volume — management, transfers, finance | Active |
| Master_Vol_4_Tech_Implementation.md | — | 21K | Tech Implementation master volume — engine, networking, tools | Active |

---

## SPECIFICATION #1: BALL PHYSICS

**Overall status:** ✅ Approved — February 8, 2026  
**Approval checklist:** 14/14 required items PASS  
**Total test scenarios:** 44 (32 unit + 12 integration)  
**Estimated pages:** ~50

| File | Version | Size | Content | Status |
|------|---------|------|---------|--------|
| Ball_Physics_Spec_Sections_1_2_v1_2.md | 1.2 | 8.5K | Introduction, scope, terminology | ✅ Approved |
| Ball_Physics_Spec_Section_3_1_v2_5.md | 2.5 | ~65K | Core formulas — forces, drag, Magnus, bounce, rolling; ApplyKick() (ERR-006); Option B possession (ERR-008) | ✅ Approved |
| Ball_Physics_Spec_Section_4_v1_1.md | 1.1 | 14K | State machine and transitions | ✅ Approved |
| Ball_Physics_Spec_Section_5_v1_1.md | 1.1 | 39K | Testing and validation | ✅ Approved |
| Ball_Physics_Spec_Section_6_v1_0.md | 1.0 | 29K | Performance targets and budgets | ✅ Approved |
| Ball_Physics_Spec_Section_7_v1_0.md | 1.0 | 33K | Integration and dependencies | ✅ Approved |
| Ball_Physics_Spec_Section_8_v1_4.md | 1.4 | ~40K | References; DOI corrections from Shot Mechanics §8 audit applied | ✅ Approved |
| Ball_Physics_Spec_Appendices_v1_2.md | 1.2 | 64K | Appendices A (derivations), B (simulations), C (test data) | ✅ Approved |
| Ball_Physics_Spec_1_Approval_Checklist.md | — | 6.5K | Formal approval gate verification | ✅ Signed off |

**Total files:** 9  
**Total size:** ~299K

---

## SPECIFICATION #2: AGENT MOVEMENT

**Overall status:** ✅ Approved — April 27, 2026  
**Approval checklist:** Section 9 v2.1 — signed off April 27, 2026  
**Total test scenarios:** 83 (49 functional unit + 19 edge case unit + 10 functional integration + 5 edge case integration)  
**Estimated pages:** ~130

| File | Version | Size | Content | Status |
|------|---------|------|---------|--------|
| Agent_Movement_Spec_Sections_1_2_v1_0.md | 1.0 | 16K | Purpose, scope, requirements, failure modes | 🔍 In Review |
| Agent_Movement_Spec_Section_3_1_v1_1.md | 1.1 | 81K | Movement state machine — 6 states, hysteresis, fatigue | 🔍 In Review |
| Agent_Movement_Spec_Section_3_2_v1_0.md | 1.0 | 60K | Locomotion mechanics — acceleration, deceleration | 🔍 In Review |
| Agent_Movement_Spec_Section_3_3_v1_0.md | 1.0 | 41K | Directional movement — lateral/backward penalties | 🔍 In Review |
| Agent_Movement_Spec_Section_3_4_v1_0.md | 1.0 | 69K | Turning dynamics — hyperbolic decay, stumble mechanics | 🔍 In Review |
| Agent_Movement_Spec_Section_3_5_v1_3.md | 1.3 | ~73K | Data structures — structs, enums, constants; KickPower, WeakFootRating, Crossing added (ERR-007) | 🔍 In Review |
| Agent_Movement_Spec_Section_3_6_v1_1.md | 1.1 | 64K | Edge cases — 11 categories, recovery procedures | 🔍 In Review |
| Agent_Movement_Spec_Section_3_7_v1_2.md | 1.2 | 64K | Testing & validation — 83 tests, tolerance derivations | 🔍 In Review |
| Agent_Movement_Spec_Section_4_v1_1.md | 1.1 | 56K | Implementation details — code organization, pipeline | 🔍 In Review |
| Agent_Movement_Spec_Section_5_v1_1.md | 1.1 | 56K | Performance analysis — budgets, profiling targets | 🔍 In Review |
| Agent_Movement_Spec_Section_6_v1_1.md | 1.1 | 50K | Future extensions — Stage 1+ deferrals, migration | 🔍 In Review |
| Agent_Movement_Spec_Section_7_v1_0.md | 1.0 | 42K | References — academic citations, Master Volume links | 🔍 In Review |
| Agent_Movement_Spec_Appendices_v1_1.md | 1.1 | 63K | Appendices A-D — derivations, verification, diagrams, tolerances | 🔍 In Review |
| Agent_Movement_Spec_Section_9_Approval_Checklist.md | 1.0 | 13K | Formal approval gate verification | 🔍 Pending sign-off |
| Agent_Movement_Spec_FR3_Revision_Note.md | — | 4K | Working document — FR3 sprint threshold revision history | Historical reference |

**Total files:** 15  
**Total size:** ~752K

### Working Documents (Agent Movement)

| File | Size | Description | Status |
|------|------|-------------|--------|
| Agent_Movement_Spec_Remaining_Sections_Outline.md | 45K | Planning outline for sections 4-7 + appendices | Superseded — all sections now written; pending removal |

---

## SPECIFICATION #3: COLLISION SYSTEM

**Overall status:** ✅ Approved — February 19, 2026  
**Approval checklist:** Section 9 v1.1 — signed off  
**Estimated pages:** ~50

| File | Version | Size | Content | Status |
|------|---------|------|---------|--------|
| Collision_System_Spec_Outline_v1_0.md | 1.0 | 31K | Full specification outline | Reference |
| Collision_System_Spec_Section_1_v1_0.md | 1.0 | 15K | Purpose, scope, terminology | ✅ Approved |
| Collision_System_Spec_Section_2_v1_1.md | 1.1 | 34K | System overview, requirements, failure modes | ✅ Approved |
| Collision_System_Spec_Section_3_v1_0.md | 1.0 | 75K | Technical specifications — collision detection, resolution | ✅ Approved |
| Collision_System_Spec_Section_4_v1_0.md | 1.0 | 53K | Architecture & integration — interface contracts | ✅ Approved |
| Collision_System_Spec_Section_5_v1_0.md | 1.0 | 41K | Testing — unit, integration, validation scenarios | ✅ Approved |
| Collision_System_Spec_Section_6_v1_1.md | 1.1 | 26K | Performance analysis — budgets, profiling | ✅ Approved |
| Collision_System_Spec_Section_7_v1_1.md | 1.1 | 38K | Future extensions — Stage 1+ roadmap | ✅ Approved |
| Collision_System_Spec_Section_8_v1_1.md | 1.1 | 44K | References and citation audit | ✅ Approved |
| Collision_System_Spec_Appendices_v1_1.md | 1.1 | 25K | Mathematical derivations and verification | ✅ Approved |
| Collision_System_Spec_Section_9_Approval_Checklist_v1_1.md | 1.1 | 18K | Formal approval gate verification | ✅ Signed off |

**Total files:** 11  
**Total size:** ~400K

---

## SPECIFICATION #4: FIRST TOUCH MECHANICS

**Overall status:** ✅ Approved — February 22, 2026  
**Approval checklist:** Section 9 v1.0 — signed off  
**Total test scenarios:** 72 (4.8× minimum)  
**Estimated pages:** ~70

| File | Version | Size | Content | Status |
|------|---------|------|---------|--------|
| First_Touch_Spec_Outline_v1_0.md | 1.0 | 66K | Full specification outline | Reference |
| First_Touch_Spec_Section_1_v1_0.md | 1.0 | 22K | Purpose, scope, terminology | ✅ Approved |
| First_Touch_Spec_Section_2_v1_0.md | 1.0 | 35K | System overview, requirements, failure modes | ✅ Approved |
| First_Touch_Spec_Section_3_v1_1.md | 1.1 | 55K | Technical specifications — control quality, touch radius, pressure | ✅ Approved |
| First_Touch_Spec_Section_4_v1_1.md | 1.1 | 77K | Architecture & integration — interfaces (ERR-001, ERR-004 resolved) | ✅ Approved |
| First_Touch_Spec_Section_5_v1_2.md | 1.2 | 75K | Testing — 72 tests, hand calculation corrections | ✅ Approved |
| First_Touch_Spec_Section_6_v1_0.md | 1.0 | 38K | Performance analysis | ✅ Approved |
| First_Touch_Spec_Section_7_v1_0.md | 1.0 | 40K | Future extensions — Stage 1+ roadmap | ✅ Approved |
| First_Touch_Spec_Section_8_v1_1.md | 1.1 | 30K | References and citation audit (~45% gameplay-tuned) | ✅ Approved |
| First_Touch_Spec_Appendices_v1_0.md | 1.0 | 50K | Appendices A-C — derivations, numerical verification, sensitivity | ✅ Approved |
| First_Touch_Spec_Section_9_Approval_Checklist_v1_0.md | 1.0 | 18K | Formal approval gate verification | ✅ Signed off |

**Total files:** 11  
**Total size:** ~506K

---

## SPECIFICATION #5: PASS MECHANICS

**Overall status:** ✅ Approved — February 22, 2026  
**Approval checklist:** Section 9 v1.2 — signed off  
**Total test scenarios:** 100 (6.7× minimum) — all unblocked  
**Estimated pages:** ~65

| File | Version | Size | Content | Status |
|------|---------|------|---------|--------|
| Pass_Mechanics_Spec_Outline_v1_0.md | 1.0 | 70K | Full specification outline | Reference |
| Pass_Mechanics_Spec_Section_1_v1_0.md | 1.0 | 23K | Purpose, scope, terminology | ✅ Approved |
| Pass_Mechanics_Spec_Section_2_v1_0.md | 1.0 | 32K | System overview, requirements, failure modes | ✅ Approved |
| Pass_Mechanics_Spec_Section_3_1_v1_1.md | 1.1 | 35K | Pass type taxonomy and physical profiles | ✅ Approved |
| Pass_Mechanics_Spec_Section_3_2_v1_0.md | 1.0 | 21K | Velocity model with V_OFFSET correction (AM-003-001) | ✅ Approved |
| Pass_Mechanics_Spec_Section_4_v1_0.md | 1.0 | 35K | Architecture & integration — interface contracts | ✅ Approved |
| Pass_Mechanics_Spec_Section_5_v1_1.md | 1.1 | 46K | Testing — 100 tests, 6 corrections from numerical verification | ✅ Approved |
| Pass_Mechanics_Spec_Section_6_v1_1.md | 1.1 | 35K | Performance analysis — updated for V_OFFSET ops | ✅ Approved |
| Pass_Mechanics_Spec_Section_7_v1_0.md | 1.0 | 44K | Future extensions — Stage 1+ roadmap | ✅ Approved |
| Pass_Mechanics_Spec_Section_8_v1_1.md | 1.1 | 48K | References; DOI corrections applied from Shot Mechanics §8 audit | ✅ Approved |
| Pass_Mechanics_Spec_Appendices_v1_1.md | 1.1 | 46K | Appendices A-C — derivations, verification, sensitivity | ✅ Approved |
| Pass_Mechanics_Spec_Section_9_Approval_Checklist_v1_2.md | 1.2 | ~24K | Formal approval gate verification — all ERR blockers cleared | ✅ Signed off Feb 22, 2026 |

**Total files:** 12  
**Total size:** ~459K

---

## SPECIFICATION #6: SHOT MECHANICS

**Overall status:** ✅ Approved — April 27, 2026  
**Approval checklist:** Section 9 v1.4 — signed off April 27, 2026 (pre-migration void-file blockers reclassified as moot under folder layout)  
**Total test scenarios:** 104 (86 unit + 12 integration + 6 validation = 6.9× minimum) — zero blocked  
**Estimated pages:** ~70  
**Design note:** ShotType enum eliminated; parameter-based physics approach (consistent with project standard)

| File | Version | Size | Content | Status |
|------|---------|------|---------|--------|
| Shot_Mechanics_Spec_Outline_v1_2.md | 1.2 | ~25K | Full specification outline; ShotType enum eliminated; all OIs resolved | Reference |
| Shot_Mechanics_Spec_Section_1_v1_1.md | 1.1 | — | Purpose, scope, KDs 1–7, dependency flags | 🔍 In Review |
| Shot_Mechanics_Spec_Section_2_v1_0.md | 1.0 | — | FR-01–FR-11; 13-step pipeline; data structures; 10 failure modes | 🔍 In Review |
| Shot_Mechanics_Spec_Section_3_1_to_3_3_v1_1.md | 1.1 | — | §3.1 Validation, §3.2 Velocity model, §3.3 Launch angle | 🔍 In Review |
| Shot_Mechanics_Spec_Section_3_4_to_3_10_v1_2.md | 1.2 | — | §3.4 Spin, §3.5 Placement, §3.6 Error, §3.7 Body Mechanics, §3.8 Weak Foot, §3.9 State Machine, §3.10 Events | 🔍 In Review |
| Shot_Mechanics_Spec_Section_4_v1_3.md | 1.3 | — | Architecture; 28 files; GoalGeometryProvider and IShotVelocityCalculator seams (Amendment 1) | 🔍 In Review |
| Shot_Mechanics_Spec_Section_5_v1_3.md | 1.3 | — | 104 tests; zero blocked; 6.9× minimum | 🔍 In Review |
| Shot_Mechanics_Spec_Section_6_v1_0.md | 1.0 | — | O(1) per evaluation; p95 < 0.050ms; Fixed64 migration notes | 🔍 In Review |
| Shot_Mechanics_Spec_Section_7_v1_0.md | 1.0 | — | Future extensions; Stage 1+ deferrals | 🔍 In Review |
| Shot_Mechanics_Spec_Section_8_v1_3.md | 1.3 | — | References; all 10 DOIs verified; WYSCOUT-VELOCITY removed; cross-spec corrections issued | 🔍 In Review |
| Shot_Mechanics_Spec_Appendices_v1_1.md | 1.1 | — | Appendices A-C — derivations, verification tables, sensitivity analysis | 🔍 In Review |
| Shot_Mechanics_Spec_Section_9_Approval_Checklist_v1_2.md | 1.2 | — | Approval checklist — 8/8 quality checks PASS; void file removal outstanding; awaiting sign-off | 🔍 Pending sign-off |

**Total files:** 12  
**⚠ Void file pending removal:** Shot_Mechanics_Spec_Section_8_v1_2.md (superseded by v1.3) — required before sign-off

---

## SPECIFICATION #7: PERCEPTION SYSTEM

**Overall status:** ✅ Approved — April 22, 2026  
**Approval checklist:** v1.7 (April 22, 2026) — ✅ APPROVED; lead developer signed off; see section-9-approval-checklist.md  
**Total test scenarios:** 95 (73 unit + 15 integration + 3 balance + 4 performance = 1.8× unit minimum)  
**Estimated pages:** ~80  
**Design note:** 10Hz tactical heartbeat (not 60Hz); sole consumer is Decision Tree (#8); ~62% gameplay-tuned constants

| File | Version | Size | Content | Status |
|------|---------|------|---------|--------|
| Perception_System_Spec_Outline_v1_1.md | 1.1 | — | Full specification outline; all 5 OQs resolved; 7 KDs locked; interface boundary to DT defined | Reference |
| Perception_System_Spec_Section_1_v1_1.md | 1.1 | — | Purpose, scope, 7 KDs, hard/soft dependencies | 🔍 In Review |
| Perception_System_Spec_Section_2_v1_1.md | 1.1 | — | FR-01–FR-13; 8-step pipeline; PerceptionSnapshot struct; 10 failure modes | 🔍 In Review |
| Perception_System_Spec_Section_3_v1_2__REPLACES_Section_3_v1_1_.md | 1.2 | — | FoV, shadow cone, L_rec, blind-side, shoulder check, pressure scalar, snapshot assembly, forced refresh | 🔍 In Review |
| Perception_System_Spec_Section_4_v1_1.md | 1.1 | — | Architecture; output contract to DT (#8); forced refresh events; no interface defined (DT unspecified) | 🔍 In Review |
| Perception_System_Spec_Section_5_v1_3__REPLACES_Section_5_v1_2_.md | 1.3 | — | 92 tests (73 unit, 12 integration, 3 balance, 4 performance); all deterministic | 🔍 In Review |
| Perception_System_Spec_Section_6_v1_1.md | 1.1 | — | ~40,000 equiv-ops per heartbeat; <20% of 2ms budget; 10Hz analysis | 🔍 In Review |
| Perception_System_Spec_Section_7_v1_1.md | 1.1 | — | Future extensions; per-entity fidelity variation deferred to Stage 1 | 🔍 In Review |
| Perception_System_Spec_Section_8_v1_2.md | 1.2 | — | References; all DOIs verified; ~62% gameplay-tuned documented | 🔍 In Review |
| Perception_System_Spec_Appendix_A_v1_1.md | 1.1 | — | Formula derivations (FoV, occlusion, L_rec, blind-side) | 🔍 In Review |
| Perception_System_Spec_Appendix_B_v1_1.md | 1.1 | — | Numerical verification tables | 🔍 In Review |
| Perception_System_Spec_Appendix_C_v1_1.md | 1.1 | — | Sensitivity analysis | 🔍 In Review |
| Perception_System_Spec_Section_9_Approval_Checklist_v1_4.md | 1.4 | — | **Section 9 Approval Checklist** — written April 19, 2026; BLOCKED on 4 critical items (PerceptionSnapshot rename, stale field names, prereq version pins) | ❌ BLOCKED |

**Total files:** 13  
**⚠ Superseded files pending removal:** pre-migration format filenames (see Pending Removal section)

---

## SPECIFICATION #8: DECISION TREE

**Overall status:** ✅ Approved — April 27, 2026 (draft-level quality gate)  
**Approval checklist:** Section 9 v1.2 — signed off April 27, 2026; comprehensive audit candidate before implementation  
**Estimated pages:** ~55+

| File | Version | Description | Status |
|------|---------|-------------|--------|
| `outline-part-1.md` | 1.1 | Outline context and architecture scope | Reference |
| `outline-part-2.md` | 1.1 | Outline technical breakdown and planned Sections 4–9 content | Reference |
| `outline-part-3.md` | 1.1 | Outline closing notes and risk summary | Reference |
| `section-1.md` | 1.1 | Purpose, scope, key design decisions | Draft |
| `section-2-1-to-2-2.md` | 1.1 | Pipeline and data structures | Draft |
| `section-2-3-to-2-5.md` | 1.1 | Functional requirements, failure modes, version history | Draft |
| `section-3-1.md` + `section-3-1-9-to-3-1-12.md` | 1.1 | Option generation | Draft |
| `section-3-2.md` + `section-3-2-3-to-3-2-9.md` + `section-3-2-10-to-3-2-13.md` | 1.2 | Utility scoring model | Draft |
| `section-3-3.md` + `section-3-3-7-to-3-3-12.md` | 1.0 | Action selection/composure model | Draft |
| `section-3-4.md`, `section-3-5.md`, `section-3-6-to-3-8.md` | 1.1 | Tactical context, dispatch, intake interface, state machine, edge cases | Draft |
| `section-4.md` | 1.0 | Architecture and integration contracts | Draft |
| `section-5.md` | 1.0 | Test plan | Draft |
| `section-6.md` | 1.0 | Performance analysis | Draft |
| `section-7.md` | 1.0 | Future extensions and exclusions | Draft |
| `section-8.md` | 1.0 | References and constant governance | Draft |
| `appendices.md` | 1.0 | Derivations, worked examples, sensitivity notes | Draft |
| `section-9-approval-checklist.md` | 1.0 | Approval checklist | Draft |

---

## SPECIFICATIONS #9–20: NOT YET STARTED

See PROGRESS.md for schedule.

| # | Specification | Priority | Target Week | Notes |
|---|---------------|----------|-------------|-------|
| 9 | Fixed64 Math Library | 2 | Week 5-6 | Migration target for all float arithmetic in prior specs |
| 10 | Heading Mechanics | 3 | Week 9 | — |
| 11 | Goalkeeper Mechanics | 3 | Week 10 | Perception System notes goalkeeper treated as standard agent at Stage 0 |
| 12 | Positioning AI | 3 | Week 12 | — |
| 13 | Pressing AI | 4 | Week 13 | — |
| 14 | Defensive AI | 4 | Week 14 | — |
| 15 | Attacking AI | 4 | Week 15 | — |
| 16 | Deterministic Simulation | 4 | Week 16 | — |
| 17 | Event System | 5 | Week 17 | PerceptionRefreshEvent stub already defined in Perception §4 |
| 18 | Performance Optimization Strategy | 5 | Week 18 | — |
| 19 | Testing Strategy & Framework | 5 | Week 19 | — |
| 20 | Code Standards & Style Guide | 5 | Week 20 | — |

---

## REMOVAL HISTORY

| File | Removed | Reason |
|------|---------|--------|
| Ball_Physics_Spec_Section_3_1_v2_3.md | Feb 8, 2026 | Superseded by v2.4 |
| Ball_Physics_Spec_Sections_1_2_v1_1.md | Feb 8, 2026 | Superseded by v1.2 |
| Ball_Physics_Spec_Section_5_v1_0.md | Feb 8, 2026 | Superseded by v1.1 |
| Ball_Physics_Spec_Section_8_v1_1.md | Feb 8, 2026 | Superseded by v1.2 |
| Ball_Physics_Spec_Appendices_v1_1.md | Feb 8, 2026 | Superseded by v1.2 |
| Rolling_Friction_Revision_v1_0.md | Feb 8, 2026 | Working document; changes incorporated into spec revisions |

### ⚠ Pending Removal (Superseded — Action Required)

> **Note:** All filenames below use the **pre-migration flat-file naming convention**.
> After the folder-based migration, these files live under `docs/specs/<spec-name>/`
> with names like `section-N.md`. Verify each entry against the actual repo folder
> structure before removing. The version numbers are tracked by Git; only verify that
> the correct version is the live file before deleting any old revision.

| File | Superseded By | Priority |
|------|--------------|----------|
| Ball_Physics_Spec_Section_3_1_v2_4.md | v2_5.md | Normal |
| Ball_Physics_Spec_Section_8_v1_2.md | v1_4.md | Normal |
| Agent_Movement_Spec_Section_3_5_v1_2.md | v1_3.md | Normal |
| First_Touch_Spec_Section_4_v1_0.md | v1_1.md | Normal |
| Pass_Mechanics_Spec_Section_3_1_v1_0.md | v1_1.md | Normal |
| Pass_Mechanics_Spec_Section_9_Approval_Checklist_v1_0.md | v1_2.md | Normal |
| Pass_Mechanics_Spec_Section_9_Approval_Checklist_v1_1.md | v1_2.md | Normal |
| Spec_Error_Log_v1_0.md | v1_2.md | Normal |
| Agent_Movement_Spec_Remaining_Sections_Outline.md | All sections written | Archive or delete |
| Shot_Mechanics_Spec_Section_8_v1_2.md | v1_3.md | **Blocking** — required before Shot Mechanics sign-off |
| Perception_System_Spec_Section_5_v1_2__REPLACES_Section_5_v1_1_.md | v1_3 file | Normal — verify replacement confirmed |
| Perception_System_Spec_Section_3_v1_1 (if present) | v1_2 file | Normal — verify replacement confirmed |

---

## FILE NAMING CONVENTIONS

**Specifications:** `[Spec_Name]_Spec_Section_[#]_v[Major]_[Minor].md`  
**Master documents:** `Master_[Name]_v[Major]_[Minor].md`  
**Infrastructure:** Plain descriptive names (e.g., `PROGRESS.md`, `README.md`)  
**Approval gates:** `[Spec_Name]_Spec_Section_9_Approval_Checklist_v[Major]_[Minor].md`  
**Note:** Perception System Section 3 and 5 use non-standard filenames due to in-place replacement — should be renamed to standard convention at next version bump.

Version numbering: Major increments for structural changes; minor increments for corrections, clarifications, and consistency fixes.

---

## SIZE SUMMARY

| Category | Files | Total Size |
|----------|-------|------------|
| Project Infrastructure | 5 | ~76K |
| Master Planning Documents | 5 | ~143K |
| Ball Physics (Spec #1) | 9 | ~299K |
| Agent Movement (Spec #2) | 15 | ~752K |
| Collision System (Spec #3) | 11 | ~400K |
| First Touch (Spec #4) | 11 | ~506K |
| Pass Mechanics (Spec #5) | 12 | ~459K |
| Shot Mechanics (Spec #6) | 12 | ~TBD |
| Perception System (Spec #7) | 12 | ~TBD |
| Working Documents | 1 | ~45K |
| **TOTAL (est.)** | **93** | **~2.68M+** |

---

**Last Updated:** February 26, 2026, 11:30 PM PST  
**Updated By:** AI Assistant (Anton review)
