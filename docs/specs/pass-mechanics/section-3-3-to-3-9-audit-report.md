# Pass Mechanics §3.3–§3.9 — Audit Report

**Purpose:** Documents findings from a systematic audit of the three §3.3–§3.9 draft files against §3.1, §3.2, §5, §9, and the Appendices.
**Date:** March 25, 2026, 11:30 PM PST
**Files Audited:** 3 files, 2,431 lines total
**Author:** Claude (AI)

---

## AUDIT VERDICT: CONDITIONALLY READY

**Total findings: 0 Critical, 2 Major, 2 Moderate, 1 Minor = 5 findings**

No blockers to approval. Two Major findings require resolution before sign-off.

---

## MAJOR FINDINGS (2)

### F-A01: §3.4.7 spin constants cite "from §3.1.4" but values not in master table

**Severity:** MAJOR — Source citation is misleading
**Location:** §3.4.7 header: "Per-Type Spin Constants (from §3.1.4 PhysicalProfile)"

The PhysicalProfile struct definition in §3.1.3 (lines 128–129) declares `spinMagnitudeBase` and `spinMagnitudeMax` as fields. However, the §3.1.4 master table does NOT populate these columns — it only has: vMin, vOffset, vMax, angleMin, angleMax, distMin, distMax, Dominant Spin, isAerial.

The per-type spin magnitude values (Ground: 8.0/15.0, Driven: 10.0/18.0, etc.) exist ONLY in §3.4.7.

**Impact:** An implementer reading §3.1.4 would not find spin magnitude values and would not know where to look. The struct fields exist but are unpopulated in the authoritative table.

**Resolution:** Either:
- (A) Add spinMagnitudeBase and spinMagnitudeMax columns to the §3.1.4 master table, OR
- (B) Change §3.4.7 header to "Per-Type Spin Constants (defined here; field declarations in §3.1.3)" and add a note to §3.1 that spin magnitude values are populated in §3.4.7.

Option (A) is preferred — it keeps the master table as the single source of truth.

---

### F-A02: §3.8.10 timing constants cite "from §3.1.4" but values not in master table

**Severity:** MAJOR — Same pattern as F-A01
**Location:** §3.8.10 header: "Per-Type Timing Constants (from §3.1.4 PhysicalProfile)"

The §3.1.4 master table has no WINDUP_FRAMES or FOLLOWTHROUGH_FRAMES columns. These values (Ground: 8/6, Driven: 12/8, etc.) exist only in §3.8.10.

**Impact:** Same as F-A01 — implementer cannot find timing values in the cited location.

**Resolution:** Either add WINDUP_FRAMES and FOLLOWTHROUGH_FRAMES columns to §3.1.4 master table, or correct the citation.

---

## MODERATE FINDINGS (2)

### F-A09: §3.6.7 error rotation pseudocode applies only one axis component

**Severity:** MODERATE — Implementation would produce incorrect error distribution
**Location:** §3.6.7 ApplyErrorToDirection pseudocode

The code computes both `errorX` (from Sin) and `errorZ` (from Cos) but applies only `errorX` via `Quaternion.Euler(0f, errorX, 0f)`. The `errorZ` component is computed but discarded. This would produce error deviations only in the Y-axis rotation plane, not uniformly around the pass direction.

The file's own note acknowledges this: "The exact rotation implementation may vary in final code."

**Impact:** During implementation, the pseudocode cannot be followed literally. The intent (rotate by errorAngleDeg at a hash-determined heading) is clear from the prose, but the code contradicts it.

**Resolution:** Replace with a correct quaternion rotation:
```
Quaternion errorRotation = Quaternion.AngleAxis(errorAngleDeg, Vector3.up);
Vector3 deviatedDirection = errorRotation * kickDirection;
```
Where errorAngleDeg is applied at the hash-determined direction by first rotating the reference frame. Or simplify the note to say implementation will use proper Y-axis rotation.

---

### F-A03: DD-3.4-03 contradicts itself

**Severity:** MODERATE — Confusing documentation
**Location:** §3.4.10 DD-3.4-03

States: "The 0.7 (Lofted topspin fraction) and 0.5 (Cross High mix fraction) are initially embedded in the switch statement rather than extracted as named constants."

But the §3.4.6 pseudocode already uses `LOFTED_TOPSPIN_FRACTION` and `CROSS_HIGH_MIX_FRACTION` as named constants, and §3.4.7 lists them in the constants table.

**Impact:** Minor confusion only — the code and constants table are correct.

**Resolution:** Update DD-3.4-03 to say the constants have been extracted per this decision.

---

## MINOR FINDINGS (1)

### F-A04: §3.9 event struct name "PassAttemptEvent" vs §4 naming

**Severity:** MINOR — Names match (verified), but worth noting
**Location:** §3.9.2 vs §4

The §3.9 file uses "PassAttemptEvent" and §4 also uses this name (confirmed by automated check). However, §9's original Known Limitations #8 referenced a "PassExecutedEvent" (now corrected). No remaining inconsistency, but future edits should maintain the "PassAttemptEvent" name.

---

## POSITIVE OBSERVATIONS

1. **Template compliance is excellent.** All 7 subsections follow the established pattern: responsibilities, inputs/outputs, formula, pseudocode, constants table, boundary verification, failure modes, design decisions.

2. **Decision Tree correctly numbered as #8** throughout all three files (6 references total, 0 errors).

3. **Fatigue convention correct** (0 = rested, 1 = fatigued) consistently in §3.5.

4. **WeakFootRating correctly [1, 5]** throughout §3.7 with proper boundary checks.

5. **Formulas trace to Appendix derivations.** Cross-references to Appendix A sections are specific (A.2, A.3, A.5, A.6, A.7) and checkable.

6. **Boundary verifications are hand-calculated** and match formula outputs. ELITE_ERROR cross-check (0.675°) confirmed against both §3.5 and Appendix B.

7. **State machine is well-specified.** 6 states, exhaustive transition table, forbidden transitions listed, timing constants documented.

8. **Event structs are zero-allocation** (struct-based, not class), consistent with performance policy.

9. **Design decisions are numbered and rationale is documented** consistently.

10. **Cross-spec dependency tables are present** with status flags.

---

*End of §3.3–§3.9 Audit Report*
