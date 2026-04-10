# Collision System Specification #3 — Comprehensive Audit

**Purpose:** Independent audit of all Collision System specification files identifying gaps, errors, inconsistencies, and cross-specification conflicts.

**Created:** March 05, 2026, audit session
**Auditor:** Claude (AI)
**Files Audited:** 10 files (Outline, Sections 1–9, Appendices)
**Verdict:** **NOT READY FOR APPROVAL** — 5 Critical, 4 Major, 6 Moderate, 5 Minor findings

**Post-audit status (Mar 5, 2026):** All 20 findings resolved. 1 finding (F-16) withdrawn as false positive. Re-audit recommended before final approval.

---

## FINDING SUMMARY

| # | Severity | Section(s) | Finding |
|---|----------|------------|---------|
| F-01 | **CRITICAL** | §2 vs §4 | BeginFrame() seed formula conflict |
| F-02 | **CRITICAL** | §9 | Approval Checklist references wrong subsection numbers after §2 renumbering |
| F-03 | **CRITICAL** | §2 vs §5 | FR test ID ranges don't match actual tests in Section 5 |
| F-04 | **CRITICAL** | Cross-spec | ERR-009 (SpatialHash Query ignores radius) still OPEN — blocks approval |
| F-05 | **CRITICAL** | §1 | Hitbox range stated as "0.35–0.50m" but actual min is 0.3525m |
| F-06 | **MAJOR** | §2 vs §4 vs §6 | Memory budget conflict: §2 says <180KB, §4 says ~297KB, never reconciled in §2 |
| F-07 | **MAJOR** | §2 vs §4 | DeterministicRNG has zero-state guard in §4 but not in §2 |
| F-08 | **MAJOR** | §3 vs §4 | Section 3 prerequisite header still references "Section 2 v1.0" — Section 2 is now v1.1 |
| F-09 | **MAJOR** | Spec Error Log | ERR-009 is used for TWO different errors (KickType, closed; SpatialHash Query, open) |
| F-10 | MODERATE | §2 vs §5 | Section 5 Summary claims 30 unit tests, but only 6+7+5+5+4+3 = 30 — matches, but FR table promises test IDs up to SH-010, CD-014, CR-012, FL-010, DT-005 (62 tests implied) |
| F-11 | MODERATE | §9 | Approval Checklist file table lists "Section_2_v1_0.md" but only v1_1 exists |
| F-12 | MODERATE | §1 vs §3 | Section 1.6.1 says cell size = 2× max radius = 2 × 0.50m = 1.0m, but Appendix A.1.2 says cell_size ≥ max_combined_radius = 1.00m — different derivation paths, same result by coincidence |
| F-13 | MODERATE | §5 vs Outline | Outline §5 promises 25+ unit, 8 integration tests. §5 delivers 30 unit + 12 integration + 5 performance. FR table promises even more IDs that don't exist. |
| F-14 | MODERATE | §7 | Section 7 says DeterministicRNG is "sole consumer" in Collision namespace but Decision Tree §3.3 independently implements SplitMix64 — shared utility not planned |
| F-15 | MODERATE | §8 | Two DOIs flagged as unverified (PATLA-1991, PAVOL-2001) since v1.1 — still unresolved |
| F-16 | MINOR | §1 | Section 1.5.1 references "Agent Movement §3.5.1" for TeamID but Agent Movement §3.5 is about AgentPhysicalProperties, not TeamID specifically |
| F-17 | MINOR | §2 | DeterministicRNG field naming: §2 uses `_s0/_s1`, §4 uses `_state0/_state1` |
| F-18 | MINOR | §4 | CollisionManifold memory comment says "28 bytes (two Vector2 + float + two ints)" — actual: 2×8 + 4 + 2×4 = 28 bytes ✓, but ContactPoint is Vector2 (not Vector3) while Section 3.4 RecordCollisionEvent passes Vector3 ContactPoint |
| F-19 | MINOR | §6 | Section 6 Preamble says "O(N+K) ~0.18ms typical" but §2.5 says "<0.15ms typical" and §4.6.2 says "<0.3ms target" — three different "typical" numbers |
| F-20 | MINOR | §9 | Checklist claims "35 checks PASS" but actual count of check rows in tables = 35 — verified correct, but several checks reference §2.4 and §2.5 with wrong meaning post-renumber |

---

## DETAILED FINDINGS

### F-01: BeginFrame() Seed Formula Conflict [CRITICAL]

**Section 2 (§2.6.4):**
```csharp
_rng = new DeterministicRNG(matchSeed ^ (ulong)frameNumber);
```

**Section 4 (§4.7.3):**
```csharp
ulong frameSeed = matchSeed ^ (ulong)frameNumber ^ ((ulong)frameNumber << 32);
_rng = new DeterministicRNG(frameSeed);
```

These produce **different seeds** and therefore **different RNG sequences**. Section 4 adds `((ulong)frameNumber << 32)` which Section 2 omits. This is a determinism-breaking inconsistency — implementer would get different behavior depending on which section they follow.

**Impact:** Breaks determinism guarantee (FR-08). Any test validating specific RNG outputs will fail if the wrong formula is used.

**Fix:** Designate one as authoritative. Section 4 formula is stronger (better seed distribution for small frame numbers) — update Section 2 to match.

---

### F-02: Approval Checklist References Wrong Subsection Numbers [CRITICAL]

Section 2 was renumbered in v1.1:
- §2.1 → Functional Requirements (NEW)
- §2.2 → Architecture (was §2.1)
- §2.3 → Data Flow (was §2.2)
- §2.4 → Collision Types (was §2.3)
- **§2.5 → Performance Budget (was §2.4)**
- **§2.6 → Failure Modes (was §2.5)**

Approval Checklist §9 references:
- "Section 2.5 (Failure Modes)" → **WRONG**, should be §2.6
- "Section 2.4" for performance budget → **WRONG**, should be §2.5

**Impact:** Checklist verification is unreliable. Reviewer following checklist references will look at wrong subsections.

**Fix:** Update all §2.x references in Approval Checklist to match v1.1 numbering.

---

### F-03: FR Test ID Ranges Don't Match Actual Tests [CRITICAL]

Section 2 FR table claims:
- FR-01: SH-001–SH-010 (10 tests)
- FR-02: CD-001–CD-008 (8 tests)
- FR-03: CD-009–CD-014 (6 tests)
- FR-04: CR-001–CR-012 (12 tests)
- FR-05: FL-001–FL-010 (10 tests)
- FR-08: DT-001–DT-005 (5 tests)

Section 5 actual tests:
- SH-001–SH-006 (6 tests, not 10)
- CD-001–CD-007 (7 tests, not 14 split across FR-02/FR-03)
- CR-001–CR-005 (5 tests, not 12)
- FL-001–FL-005 (5 tests, not 10)
- DT-001–DT-003 (3 tests, not 5)

The FR table implies **51 tests** in those categories alone. Section 5 delivers **26**. The gap is massive. Either the FR table over-promised or Section 5 under-delivered.

**Impact:** Traceability between requirements and tests is broken. A reviewer cannot verify which tests cover which FRs.

**Fix:** Either write the missing tests (SH-007 through SH-010, CD-008 through CD-014, etc.) or correct the FR table to match actual test IDs.

---

### F-04: ERR-009 SpatialHash Query Defect Still Open [CRITICAL]

The Spec Error Log v1.3 documents ERR-009 (the SpatialHash one, not the closed KickType one): `SpatialHashGrid.Query()` ignores its radius parameter and always returns a fixed 3×3 neighbourhood. This is marked **"blocks Collision System §4 approval"** in the error log's priority table.

The Approval Checklist (§9) does not list this as a pre-approval blocker. It should.

**Impact:** Pass Mechanics, Shot Mechanics, and First Touch all pass `PRESSURE_RADIUS_MAX = 3.0m` to Query() and get truncated results. This is a live architectural defect.

**Fix:** Either fix Query() in Section 3 to respect the radius parameter, or explicitly document the 3×3 limitation and require callers to work around it. Either way, ERR-009 must be closed before approval.

---

### F-05: Hitbox Range Inconsistency in Section 1 [CRITICAL]

Section 1 Terminology table and §1.5.1 state hitbox radius as "0.35–0.50m". The actual minimum per Agent Movement §3.5.4.3 is **0.3525m** (Strength 1: 0.35 + 1/20 × 0.15 = 0.3525).

The Approval Checklist v1.1 correctly identifies MIN_HITBOX_RADIUS as 0.3525m, but Section 1 was never updated to match.

**Impact:** Cell size derivation in §1.6.1 uses "max agent radius of 0.50m" which is correct for the *maximum*, but the stated range "0.35–0.50m" is misleading since no agent actually has 0.35m radius.

**Fix:** Update Section 1 hitbox range to "0.3525–0.50m" in both §1.3 and §1.5.1.

---

### F-06: Memory Budget Never Reconciled [MAJOR]

- Section 2.5 (Performance Budget): "<180 KB static memory"
- Section 4.5 (Memory Layout): "~297 KB total"
- Section 4 Cross-Reference: "⚠ Memory slightly over (297 KB vs 180 KB target)"

Section 4 acknowledges the discrepancy and recommends accepting 297 KB, but Section 2 was never updated. The 180 KB figure is still stated as a target in §2.5.

**Fix:** Update §2.5 to state "<300 KB static memory" or add a note referencing §4.5 as authoritative.

---

### F-07: DeterministicRNG Zero-State Guard Missing in Section 2 [MAJOR]

Section 4 (§4.7.2) includes:
```csharp
if (_state0 == 0 && _state1 == 0)
{
    _state0 = 0x853C49E6748FEA9BUL;
    _state1 = 0xDA3E39CB94B95BDBUL;
}
```

Section 2 (§2.6.4) does not include this guard. While SplitMix64 is unlikely to produce all-zero output, the absence creates an implementation ambiguity — an engineer reading §2 would produce a different struct than one reading §4.

**Fix:** Add zero-state guard to §2 implementation, or add a note that §4 is the authoritative implementation.

---

### F-08: Section 3 Prerequisites Reference Stale Version [MAJOR]

Section 3 header states: `Prerequisites: Section 1 (Purpose & Scope) v1.0, Section 2 (System Overview) v1.0`

Section 2 is now v1.1 with significant renumbering (§2.1 FR table added, all subsections shifted). Section 3 references §2.x subsection numbers that are from v1.0 numbering.

**Fix:** Update Section 3 prerequisite header and verify all internal §2.x references against v1.1 numbering.

---

### F-09: Duplicate ERR-009 in Spec Error Log [MAJOR]

The Spec Error Log v1.3 contains TWO entries labeled ERR-009:
1. **ERR-009 (Closed):** "`PassThroughGround`/`PassThroughAerial` are redundant `KickType` values"
2. **ERR-009 (Open):** "`SpatialHashGrid.Query()` ignores radius parameter"

This is a numbering collision that could cause confusion during audit. The open defect should have been ERR-011 or higher.

**Fix:** Renumber the SpatialHash Query defect to ERR-011 in Spec Error Log v1.4.

---

### F-10 through F-20: Moderate and Minor findings

Detailed above in the summary table. These require attention but are not approval-blocking individually. However, F-10 (FR test ID mismatch) compounds with F-03 to create a serious traceability gap.

---

## SECTIONS WITH NO SIGNIFICANT ISSUES

- **Section 6 (Performance Analysis):** Internally consistent. Operation counts match Section 3 code. Budget figures align with Section 4 profiling targets. Known limitations properly documented.
- **Section 7 (Future Extensions):** Well-structured. Stage mapping is clear. All existing hooks verified against Section 4. Risk assessments provided.
- **Appendices A–C:** Formulas correctly derived. Numerical verification tables check out. v1.1 corrections (hitbox radius, cell size) properly applied.

---

## CORE PHYSICS ASSESSMENT

The core physics is **sound**. Specifically:
- Circle-circle detection formula is textbook-correct (Ericson/Eberly)
- Impulse-based response correctly implements conservation of momentum with restitution
- Penetration resolution via mass-weighted MTV is standard
- Spatial hash design is appropriate for 23 entities
- Fall/stumble severity system is gameplay-tuned but explicitly documented as such

The problems are in **cross-section consistency, versioning hygiene, and requirement-test traceability** — not in the physics.

---

## VERDICT

**NOT READY FOR APPROVAL**

### Must-fix before approval (5 Critical + 4 Major):
1. Resolve BeginFrame seed formula conflict (F-01)
2. Update Approval Checklist §2.x references for v1.1 numbering (F-02)
3. Reconcile FR test ID ranges with actual Section 5 tests (F-03)
4. Close ERR-009 (SpatialHash Query radius) or document workaround (F-04)
5. Fix Section 1 hitbox range to 0.3525–0.50m (F-05)
6. Reconcile memory budget across §2/§4 (F-06)
7. Align DeterministicRNG implementations between §2 and §4 (F-07)
8. Update Section 3 prerequisite versions (F-08)
9. Fix duplicate ERR-009 numbering (F-09)

### Should-fix (6 Moderate) — ALL RESOLVED:
- F-10: Resolved by F-03 (FR test ID ranges corrected in Section 2 v1.2)
- F-11: Resolved in Approval Checklist v2.0 (file table updated)
- F-12: Resolved in Section 1 v1.1 (cell size derivation aligned with Appendix A)
- F-13: Resolved by F-03 (FR table no longer over-promises test IDs)
- F-14: Resolved in Section 4 v1.1 ("sole consumer" updated; Decision Tree SplitMix64 noted)
- F-15: Resolved in Section 8 v1.2 (PATLA-1991 corrected to PATLA-1997 with verified DOI;
  PAVOL-2001 DOI verified via PubMed)

### Nice-to-fix (5 Minor) — 4 RESOLVED, 1 WITHDRAWN:
- F-16: WITHDRAWN — false positive. Agent Movement §3.5.1 does contain TeamID (verified).
- F-17: Resolved in Section 2 v1.2 (_state0/_state1 naming aligned with Section 4)
- F-18: Resolved in Section 4 v1.1 (ContactPoint Vector2/Vector3 conversion documented)
- F-19: Resolved in Section 4 v1.1 and Section 6 v1.2 (timing terminology clarified:
  ~0.18ms estimate vs <0.15ms typical target vs <0.30ms p95 target)
- F-20: Resolved in Approval Checklist v2.0 (§2.x references corrected)

---

## COMPARISON TO AGENT MOVEMENT AUDIT

| Metric | Agent Movement Audit | Collision System Audit |
|--------|---------------------|----------------------|
| Total findings | 26 | 20 |
| Critical | 5 | 5 |
| Major | 7 | 4 |
| Core physics sound? | Yes (§3.2–§3.4) | Yes (detection + response) |
| Primary failure mode | Approval Checklist broken | Cross-section versioning + ERR-009 |
| Estimated fix effort | ~4 hours | ~3 hours |

The Collision System has fewer total findings but the same critical count. The pattern is similar: core technical content is solid, but consistency across sections degrades as revisions accumulate without full cross-section sweeps.

---

**END OF AUDIT**
