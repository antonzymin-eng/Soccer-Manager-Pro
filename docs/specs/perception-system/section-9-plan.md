# Plan: Perception System Section 9 Approval Checklist

## Context

The Perception System Specification #7 has all 8 content sections plus 3 appendices fully written, but the Section 9 Approval Checklist file (`section-9-approval-checklist.md`) is blank. SPEC_INDEX.md explicitly notes: "Section 9 Approval Checklist has not been written yet. Cannot sign off without it." The spec is currently status IN REVIEW — this checklist is the final gate before lead developer sign-off.

---

## Approach

Write the complete Section 9 Approval Checklist following the canonical format established by the 6 existing approval checklists (Ball Physics #1, Agent Movement #2, Collision System #3, First Touch #4, Pass Mechanics #5, Shot Mechanics #6). The Pass Mechanics #5 checklist (`docs/specs/pass-mechanics/section-9-approval-checklist.md`) is the most evolved template and will be the primary structural reference.

Every checklist value must be verified against the actual source files — no fabricated values (per CLAUDE.md rule).

---

## File to Create

**Target:** `/home/user/Soccer-Manager-Pro/docs/specs/perception-system/section-9-approval-checklist.md`

---

## Checklist Structure (section-by-section)

### 1. Header
- Title: `# Perception System Specification #7 — Section 9: Approval Checklist`
- Metadata: file name, purpose, created date (April 10, 2026), version 1.0, status PENDING, author Claude (AI) with Anton (Lead Developer), spec number 7 of 20

### 2. Specification Files — Current Versions
Table listing all 13 files in the spec folder with version, status, notes:
- `outline.md` (v1.1), `section-1.md` (v1.1), `section-2.md` (v1.1), `section-3.md` (v1.2), `section-4.md` (v1.1), `section-5.md` (v1.3), `section-6.md` (v1.1), `section-7.md` (v1.1), `section-8.md` (v1.2), `section-9-approval-checklist.md` (v1.0 — this document), `appendix-a.md` (v1.1), `appendix-b.md` (v1.1), `appendix-c.md` (v1.1)
- Versions must be verified by reading the header of each file

### 3. Content Checklist (8 items)
Table: # | Requirement | Status | Evidence

| # | Requirement | Key evidence to cite |
|---|---|---|
| 1 | All template sections present (1–9 + Appendices) | 13 files, all sections represented |
| 2 | Formulas include derivations | Appendix A derivations, Section 3 inline derivation blocks for all 6 models |
| 3 | Edge cases documented with recovery procedures | Section 4 failure modes (FM-AM-01–05, FM-BP-01–03, FM-CS-01–02), Section 2 invariants INV-1–INV-10 |
| 4 | Test scenarios defined (min 40 unit + 12 integration) | 73 unit + 12 integration + 3 balance + 4 performance = 92 total |
| 5 | Performance targets specified with budget context | Section 6: 2ms/22 agents, ~90us/agent, zero heap allocation |
| 6 | Failure modes enumerated with recovery | Section 4: 12 failure modes total with explicit recovery |
| 7 | Cross-references to all dependencies identified | Section 1 deps, Section 4 integration contracts, Section 8 refs |
| 8 | Constants derived with documented rationale | Section 3 §3.10: 17 constants, all tagged [GT]/[DERIVED]/[CROSS] |

### 4. Quality Checklist (8 items)
Table: # | Requirement | Status | Evidence

Key items:
1. Formulas validated — Appendix B numerical verification tables
2. Tolerances derived — Section 5 §5.15 tolerance derivations
3. Academic references with DOIs — Section 8: Williams-1998, Helsen-1999, Beilock-2007 (corrected), Franks-1985 (pre-DOI)
4. Cross-references to Master Volumes verified
5. Internal consistency checked — **PARTIAL** due to PerceptionSnapshot struct mismatch (see Blocker 1)
6. Changelogs maintained
7. ~71% [GT] constants acknowledged — expected for cognitive simulation
8. Sensitivity analysis completed — Appendix C

### 5. Review Checklist (3 items)
1. AI critique completed — PASS
2. Community feedback — SKIPPED (solo project)
3. Lead developer approval — PENDING

### 6. Documentation Checklist (5 items)
1. Version control updated — PASS
2. Changelogs maintained — PASS
3. PROGRESS.md updated — PENDING
4. FILE_MANIFEST.md updated — PENDING
5. Filed in correct directory — PASS

### 7. Internal Consistency Audit (CRITICAL — largest section)

Organized into subsystem groups, each with a verification table. ~45-50 checks total:

**A. FoV Constants (§3.1 <-> §5.2 <-> Appendix B)** — ~8 checks
- BASE_FOV_ANGLE=160, MAX_FOV_BONUS_ANGLE=10, MAX_FOV_PRESSURE_REDUCTION=30, MIN_FOV_ANGLE=120
- FoV formula uses Decisions/20 (not (Decisions-1)/19) — verify consistent across §3.1, §5, Appendix A/B/C
- Decisions=1 -> 160.5, Decisions=20 -> 170 — verify in test expectations

**B. Occlusion Constants (§3.2 <-> §5.3)** — ~4 checks
- AGENT_BODY_RADIUS=0.4m, MIN_SHADOW_HALF_ANGLE=5
- Opponents only cast shadows at Stage 0 (OQ-1)
- Shadow cone formula consistent

**C. Recognition Latency Constants (§3.3 <-> §5.4 <-> Appendix A)** — ~7 checks
- L_MAX=5 ticks, L_MIN=1 tick
- Uses (Decisions-1)/19 (different from FoV formula)
- Half-turn bonus = x0.85, CONFIRMATION_EXPIRY_TICKS=1 [DERIVED]
- Noise additive-only (+0/+1), floor rounding

**D. Shoulder Check Constants (§3.4 <-> §5.5)** — ~5 checks
- CHECK_MAX_TICKS=30, CHECK_MIN_TICKS=6, SHOULDER_CHECK_DURATION=3
- Possession doubles interval, autonomous triggering (KD-6, OQ-3)

**E. Pressure Scalar Constants (§3.6 <-> First Touch #4 §3.5)** — ~3 checks
- PRESSURE_RADIUS=3.0m, MIN_PRESSURE_DISTANCE=0.3m, PRESSURE_SATURATION=1.5
- All [CROSS] — verify match with First Touch source values

**F. PerceptionSnapshot Struct Consistency (§2.3.1 <-> §3.7.1 <-> §4)** — ~8 checks
- **FLAG MISMATCH**: Section 2 defines 12 fields (ReadOnlySpan, count scalars, no IsForceRefreshed/PressureScalar/BlindSidePerceivedAgents/ShoulderCheckAnim); Section 3 defines 14 fields (arrays, adds IsForceRefreshed, BlindSidePerceivedAgents, PressureScalar, ShoulderCheckAnim, removes count fields)
- Section 3 summary (line 1148) claims "12 fields" but struct has 14
- Section 4 uses "ForcedRefreshThisTick" (name variant of IsForceRefreshed)
- BlindSideWindowExpiry type differs: float in §2, int in §3
- **DUAL AUTHORITY CLAIM**: Section 2 header says "authoritative `PerceptionSnapshot`... struct definitions" AND Section 3 §3.7 says "This section is the authoritative definition." Only one can be authoritative — this ambiguity is a root cause of the field mismatch.
- **FR-10 phantom constant**: FR-10 references `PRESSURE_FOV_THRESHOLD` (KD-7) but this constant does not appear in the §3.10 constants table. Either the FR wording needs updating or a constant is missing from the table.

**G. Cross-Specification Interface Consistency** — ~6 checks
- AgentState fields match Agent Movement #2
- PlayerAttributes.Decisions and .Anticipation are int [1-20]
- BallState.Position matches Ball Physics #1
- spatialHash.QueryRadius signature matches Collision System #3

**H. Constants Table Self-Consistency (§3.10)** — ~5 checks
- **FLAG**: Line 1157 summary says "16 constants, 12 [GT], 3 [CROSS], 1 [GT]" — the "1 [GT]" should be "2 [DERIVED]"; total count inconsistent (paragraph says 17, table has 18 rows including half-turn)
- 12+2+3=17 vs 12+2+4=18 (half-turn is 4th [CROSS] in table but paragraph says "Three are [CROSS]")
- **TAG CONVENTION**: CLAUDE.md specifies four tags: [GT], [EST], [FIXED], [DERIVED]. The spec uses [GT], [CROSS], [DERIVED] — and the §3.10 preamble lists [PHYS] in its legend. [CROSS] and [PHYS] are not in CLAUDE.md's canonical list. Flag for lead developer decision: accept [CROSS] as an evolution (reasonable for cross-spec constants) or align to CLAUDE.md tags.

**H2. Design Decisions Verification (KD-1 through KD-7)** — ~7 checks
- Verify each KD is documented in Section 1, referenced consistently in downstream sections, and marked as locked
- KD-1: Heartbeat-driven (10Hz) — verify §2.1.2
- KD-2: No omniscience — verify §2.1.1, §4.5
- KD-3: Opponent-only occlusion — verify §3.2, OQ-1
- KD-4: No non-deterministic APIs — verify §3.3.4, §3.4.2
- KD-5: Blind-side 200° rear arc — verify §3.1, §3.4
- KD-6: Autonomous shoulder checks — verify §3.4, OQ-3
- KD-7: Pressure narrows FoV — verify §3.1.4, §3.6

**H3. Open Questions Resolution Status (OQ-1 through OQ-5)** — ~5 checks
- Verify each OQ is resolved and locked in the outline, with the resolution reflected in the relevant section
- OQ-1: Teammate occlusion → excluded at Stage 0 (§3.2)
- OQ-2: Ball recognition latency → instant (L_rec=0) (§3.5)
- OQ-3: Shoulder check triggering → autonomous (§3.4)
- OQ-4: Resolution not documented — verify
- OQ-5: MAX_PERCEPTION_RANGE → 120m effectively uncapped (§3.10)

**I. Pipeline Invariants** — ~3 checks
- INV-1 through INV-10 all testable, mapped to tests
- INV-8 (BallStalenessFrames==0 iff BallVisible==true) consistent
- INV-10 (zero heap allocation) documented in §6.5

**J. Determinism Requirements** — ~3 checks
- No System.Random, DeterministicHash from matchSeed, identical inputs -> identical outputs

**K. Coordinate System** — ~2 checks
- Origin at corner (0,0,0) per Ball Physics §1.2
- 2D operations use Vector2

### 8. Algorithm/Physics Quality Assessment
Table: Algorithm | Derivation | Verification | Literature Source | Status

9 algorithms to assess:
1. FoV cone geometry (Appendix A §A.1, Appendix B §B.1, Franks-1985/Williams-1998)
2. FoV pressure degradation (Appendix A §A.2, Beilock-2007)
3. Shadow cone occlusion (Appendix A §A.4/A.5, design decision KD-3)
4. Recognition latency L_rec (Appendix A §A.6/A.7, Franks-1985/Helsen-1999)
5. Blind-side arc (Appendix A §A.3, design decision KD-5)
6. Shoulder check scheduling (Appendix A §A.10, Master Vol 1 §3.1)
7. Pressure scalar (First Touch #4 §3.5, cross-spec)
8. Ball staleness tracking (§3.5.2, design decision OQ-2)
9. Deterministic hash/noise (Appendix A §A.9, project standard)

### 9. Known Limitations (Accepted — Non-Blocking)
~10 items:
1. ~71% [GT] constants — expected for cognitive simulation
2. Teammate occlusion excluded at Stage 0 (OQ-1) — Stage 1 additive extension
3. Ball has no L_rec (OQ-2) — deliberate design decision
4. Shoulder check fully autonomous (OQ-3) — context-sensitive urgency deferred to Stage 1
5. XC-4.2-02: Possession tracking gap — bool parameter workaround functional for Stage 0
6. Float determinism limitation — single-platform acceptable; Fixed64 at Stage 5+
7. FormModifier/PsychologyModifier hooks not yet active — Stage 3+
8. MAX_PERCEPTION_RANGE effectively uncapped — architectural hook for Stage 2 weather
9. No peripheral vision degradation curve — Stage 1+ (§7.1.6)
10. Constants table editorial inconsistencies (§3.10 line 1157) — minor, non-blocking

### 10. Cross-Specification Dependencies
Table: Dependency | Direction | Interface | Status

| Dependency | Direction | Status |
|---|---|---|
| Ball Physics #1 | Upstream | APPROVED |
| Agent Movement #2 | Upstream | IN REVIEW |
| Collision System #3 | Upstream | APPROVED |
| First Touch #4 | Cross-reference | APPROVED |
| Decision Tree #8 | Downstream (sole consumer) | IN PROGRESS |
| Fixed64 #9 | Future | NOT STARTED |
| Goalkeeper #11 | Downstream | NOT STARTED |
| Event System #17 | Downstream | NOT STARTED |

Note: ERR-010 (Shot Mechanics refers to DT as #7, correct is #8) is a Shot Mechanics defect — does not block this spec.

### 11. Test Coverage Summary
Table: Category | Prefix | Count | Blocked | Effective | Section

| Category | Prefix | Count |
|---|---|---|
| Field of View | FOV-* | 8 |
| Shadow Cone Occlusion | OCC-* | 13 |
| Recognition Latency | LR-* | 8 |
| Shoulder Check | SC-* | 8 |
| Ball Perception | BP-* | 4 |
| Pressure Scalar | PS-* | 3 |
| Forced Refresh | FR-* | 10 |
| Snapshot Assembly | SNAP-* | 10 |  
| (subtotal unit) | | **TBD** |
| Integration | IT-* | 12 |
| Balance | BAL-* | 3 |
| Performance | PT-* | 4 |
| **Total** | | **TBD** |

**CRITICAL NOTE:** Test counts must be verified by reading Section 5 before writing. The prefix-based count from exploration (FOV:8 + OCC:13 + LR:8 + SC:8 + BP:4 + PS:3 + FR:10 + SNAP:10 = 64 unit) does not match the Section 5 v1.3 changelog claim of "73 unit." The 9-test gap likely comes from additional test IDs grouped under different prefixes (DET-*, CORE-*, or expanded categories). The checklist MUST use the actual verified counts — no fabrication.

### 12. Citation/Reference Audit
Table for each academic source — verify DOI status from Section 8:
- Williams-1998 (DOI verified)
- Helsen-1999 (DOI verified)
- Beilock-2007 (DOI corrected and verified)
- Franks-1985 (pre-DOI, confirmed no DOI exists)
- Master Vol references (internal)

### 13. Pre-Approval Blockers

**Blocker 1 — CRITICAL: PerceptionSnapshot Struct Mismatch**
- Section 2 §2.3.1 defines 12 fields (ReadOnlySpan, count scalars)
- Section 3 §3.7.1 defines 14 fields (arrays, adds IsForceRefreshed/BlindSidePerceivedAgents/PressureScalar/ShoulderCheckAnim, removes counts, changes types)
- Section 3 summary claims "12 fields" but actual struct has 14
- Section 4 uses different field name variant (ForcedRefreshThisTick vs IsForceRefreshed)
- Both Section 2 and Section 3 claim to be the "authoritative" definition
- Severity upgraded to CRITICAL: 4 missing fields, 2 changed types, 1 naming inconsistency, dual authority claim — would directly block implementation if unresolved

**Blocker 2 — LOW: Agent Movement #2 Not Yet Approved**
- Upstream dependency, IN REVIEW status
- Interface contracts are stable; procedural dependency

### 14. Approval Decision
- Content: 8/8 PASS
- Quality: 7/8 PASS, 1 PARTIAL
- Review: 1/3 PASS, 1 SKIPPED, 1 PENDING
- Documentation: 3/5 PASS, 2 PENDING
- Decision: **PENDING** — Blocker 1 requires resolution or explicit acceptance

### 15. Sign-Off Section
Lead developer checkboxes (unchecked):
- Reviewed spec and checklist
- Verified internal consistency audit
- Verified appendices complete
- Accept/resolve PerceptionSnapshot struct mismatch (Blocker 1)
- Accept Agent Movement #2 interface stability (Blocker 2)
- Accept XC-4.2-02 possession tracking gap
- Approve Perception System #7 for implementation
- Date field

Post-approval actions list (7-8 items):
1. Reconcile PerceptionSnapshot struct if not done pre-approval
2. Fix §3.10 constants table editorial errors (line 1157)
3. Update PROGRESS.md
4. Update FILE_MANIFEST.md
5. Git tag: `spec-perception-system-v1.0-approved`
6. Continue Decision Tree #8

### 16. Approval History
Single row: v1.0, April 10 2026, Created, initial checklist summary

### 17. End Marker

---

## Pre-Writing Verification Steps

Before writing the checklist content, verify these values by reading source files:

1. **File versions** — Read the header of each of the 13 files to confirm version numbers. Already confirmed: section-2 (v1.1), section-3 (v1.2). Still need: section-1, section-4 through section-8, outline, appendices A-C.
2. **Test counts** — CRITICAL: Read Section 5 test inventory sections to get exact counts per category. The prefix-based count (64 unit) doesn't match the exploration agent's claim (73 unit). Must resolve this 9-test discrepancy by reading the actual test tables in §5.2–§5.13.
3. **Constants table** — Already verified: 18 rows in table, paragraph says 17, summary says 16 — editorial inconsistency to flag
4. **Struct field counts** — Already verified: §2.3.1 has 12 fields, §3.7.1 has 14 fields — genuine mismatch to flag as CRITICAL blocker
5. **Cross-spec constant values** — Verify PRESSURE_RADIUS/MIN_PRESSURE_DISTANCE/PRESSURE_SATURATION match First Touch #4
6. **FR-10 PRESSURE_FOV_THRESHOLD** — Verify whether this constant is defined anywhere in §3 or is a phantom reference
7. **OQ-4 resolution** — Verify what OQ-4 was and confirm its resolution status

---

## Critical Files

| File | Purpose in checklist |
|---|---|
| `docs/specs/perception-system/section-9-approval-checklist.md` | **TARGET** — file to write |
| `docs/specs/perception-system/section-2.md` | FR list (50 FRs), struct definitions, pipeline invariants |
| `docs/specs/perception-system/section-3.md` | Formulas, constants master table (§3.10), authoritative struct (§3.7.1) |
| `docs/specs/perception-system/section-4.md` | Architecture, failure modes, cross-spec validation, XC-4.5-01 flag |
| `docs/specs/perception-system/section-5.md` | Test inventory (92 tests), coverage matrix (§5.14) |
| `docs/specs/perception-system/section-6.md` | Performance budgets |
| `docs/specs/perception-system/section-8.md` | References, DOI verification, citation audit |
| `docs/specs/perception-system/appendix-a.md` | Formula derivations |
| `docs/specs/perception-system/appendix-b.md` | Numerical verification tables |
| `docs/specs/perception-system/appendix-c.md` | Sensitivity analysis |
| `docs/specs/pass-mechanics/section-9-approval-checklist.md` | **FORMAT TEMPLATE** — most evolved checklist |
| `docs/specs/SPEC_INDEX.md` | Spec statuses for dependencies table |

---

## Verification

After writing the checklist:
1. Verify every constant value cited in the Internal Consistency Audit against the actual §3.10 table
2. Verify every test count against actual Section 5 content
3. Verify every file version against actual file headers
4. Verify the struct field mismatch is accurately described
5. Commit to branch `claude/review-perception-specs-Fryna` and push
