# Perception System Specification #7 — Section 9: Approval Checklist

**File:** `section-9-approval-checklist.md`
**Purpose:** Formal approval gate for Perception System Specification #7. Confirms content completeness, cross-section consistency, and approval readiness before implementation sign-off.
**Created:** April 19, 2026
**Updated:** April 19, 2026
**Version:** 1.4
**Status:** ❌ BLOCKED — Four critical blockers must be resolved before approval
**Author:** Claude (AI) with Anton (Lead Developer)
**Specification Number:** 7 of 20 (Stage 0 — Physics Foundation)

---

## SPECIFICATION FILE INVENTORY

| # | File | Version | Status |
|---|---|---:|---|
| 1 | `outline.md` | 1.1 | ✅ Complete |
| 2 | `section-1.md` | 1.1 | ❌ Blocked — stale `PerceptionSnapshot` throughout |
| 3 | `section-2-1-to-2-3.md` + `section-2-4-to-2-7.md` | 1.2 | ✅ Complete |
| 4 | `section-3-0-to-3-6.md` + `section-3-7-to-3-10.md` | 1.3 | ⚠️ Needs correction — constants table counts inconsistent |
| 5 | `section-4-1-to-4-7.md` + `section-4-8-to-4-9.md` | 1.2 | ✅ Complete |
| 6 | `section-5-1-to-5-10.md` + `section-5-11-to-5-17.md` | 1.3 | ❌ Blocked — stale field names, removed identifiers, stale prereq versions |
| 7 | `section-6-1-to-6-5.md` + `section-6-6-to-6-13.md` | 1.1 | ✅ Complete |
| 8 | `section-7-1-to-7-5.md` + `section-7-6-to-7-9.md` | 1.1 | ✅ Complete |
| 9 | `section-8-1-to-8-5.md` + `section-8-6-to-8-8.md` | 1.2 | ⚠️ Needs correction — stale naming in §8.5 and §8.6.8 |
| 10 | `appendix-a.md` | 1.1 | ✅ Complete |
| 11 | `appendix-b.md` | 1.1 | ✅ Complete |
| 12 | `appendix-c.md` | 1.1 | ✅ Complete |
| 13 | `section-9-approval-checklist.md` (this file) | 1.4 | — |

**Total:** 13 files. Versions range from v1.1 (section 1, appendices) to v1.3 (sections 3 and 5).

**Registry status (`docs/specs/SPEC_INDEX.md`):** Spec #7 is **IN REVIEW**. Dependencies: #1 APPROVED, #2 IN REVIEW, #3 APPROVED, #4 APPROVED, #8 IN PROGRESS.

---

## CONTENT CHECKLIST

| # | Requirement | Status | Evidence |
|---|---|---|---|
| 1 | All required sections present (Outline, 1–9, Appendices A–C) | ✅ PASS | All 13 files present and populated |
| 2 | Struct contracts aligned between §2 (requirements) and §3 (implementation) | ✅ PASS | `FilteredView` 9 fields, `PerceptionDiagnostics` 7 fields, `PerceivedAgent` 4 fields — match confirmed |
| 3 | Test inventory complete and internally consistent | ❌ FAIL | §5.17 summary claims 12 integration tests; §5.11 contains 15 written (4 AM + 2 BP + 3 CS + 6 FULL) — 3-test discrepancy unresolved |
| 4 | Key Decisions locked and propagated | ✅ PASS | KD-1 through KD-7 confirmed locked and consistent |
| 5 | Open Questions resolved | ✅ PASS | OQ-1 through OQ-5 all marked resolved in `outline.md` |
| 6 | Failure modes enumerated with ownership boundaries | ✅ PASS | §4: FM-AM-01–05, FM-BP-01–02, FM-CS-01–04 (11 modes total) |
| 7 | Constants catalogued with provenance tags | ⚠️ PARTIAL | §3.10 paragraph states 17 constants with 3 `[CROSS]`; table has 18 rows and 4 `[CROSS]` entries; `[PHYS]` appears in legend but not in table — counts inconsistent |

---

## QUALITY CHECKLIST

| # | Requirement | Status | Evidence |
|---|---|---|---|
| 1 | Academic references included and relevant | ✅ PASS | 8 academic sources in §8 |
| 2 | DOI checks completed where applicable | ⚠️ PARTIAL | WILLIAMS-1998, HELSEN-1999, WARD-2003, BEILOCK-2007, MANN-2007, JORDET-2009 verified; FRANKS-1985 pre-DOI era (acceptable); HEADRICK-2012 unresolved — non-blocking |
| 3 | Performance targets explicit and budgeted | ✅ PASS | 2 ms / 22 agents (~90 µs/agent), O(n×k) complexity, zero-GC target documented in §6 |
| 4 | Terminology consistent across all sections | ❌ BLOCKED | `PerceptionSnapshot` survives in §1 (KD-2, §1.1, §1.2), §5 test bodies, §8.5 citation row, §8.6.8 heading — predates v1.3 rename to `FilteredView`/`PerceptionDiagnostics` |

---

## REVIEW CHECKLIST

| # | Requirement | Status | Evidence |
|---|---|---|---|
| 1 | Verification pass completed with severity-tagged findings | ✅ PASS | Four critical blockers and five non-blocking findings documented below |
| 2 | Blocking vs non-blocking findings clearly separated | ✅ PASS | See Critical Blockers and Non-Blocking Findings sections |
| 3 | Lead developer approval granted | ☐ PENDING | Deferred until all four critical blockers are resolved |

---

## DOCUMENTATION CHECKLIST

| # | Requirement | Status | Evidence |
|---|---|---|---|
| 1 | Version metadata present in all files | ✅ PASS | All files carry version headers |
| 2 | Cross-section version references current | ❌ BLOCKED | §5 prerequisite header cites "Section 2 v1.1, Section 3 v1.2" — Section 2 is now v1.2, Section 3 is now v1.3 |
| 3 | Referenced fields and constants exist in active contracts | ❌ BLOCKED | §5 contains `IsForceRefreshed` (stale), `PRESSURE_FOV_THRESHOLD` (removed), `IsInBlindSide` (removed) |
| 4 | Registry status synchronized | ✅ PASS | IN REVIEW status matches `SPEC_INDEX.md` |

---

## TEST COVERAGE SUMMARY

| Group | Tests | Notes |
|---|---:|---|
| FOV | — | |
| OCC | — | |
| LR | — | |
| SC | — | |
| BP | — | |
| PS | — | |
| FR | — | |
| SNAP | — | |
| DET | — | |
| **Unit total (9 groups)** | **73** | Verified |
| Integration (per §5.11 actual) | 15 | 4 AM + 2 BP + 3 CS + 6 FULL — verified by test count in §5.11 |
| Integration (per §5.17 summary) | 12 | ❌ Does not match §5.11 — 3-test discrepancy, must reconcile |

**Action required:** §5.17 summary must be corrected to 15, or three tests must be identified and removed from §5.11. Do not approve until counts agree.

---

## CRITICAL BLOCKERS

All four must be resolved before approval can proceed.

### Blocker 1 — Section 1 uses superseded struct name `PerceptionSnapshot`

**Scope:** `section-1.md` — KD-2, §1.1, §1.2  
**Issue:** These subsections were written before the v1.3 rename. `PerceptionSnapshot` no longer exists; the authoritative names are `FilteredView` (the output struct) and `PerceptionDiagnostics` (the diagnostic struct).  
**Fix:** Replace every occurrence of `PerceptionSnapshot` in `section-1.md` with the correct struct name. Verify each callsite to determine which struct applies.

---

### Blocker 2 — Section 5 FR-001–005 use stale field name `IsForceRefreshed`

**Scope:** `section-5-1-to-5-10.md` — forced-refresh test group FR-001 through FR-005  
**Issue:** The field is named `ForcedRefreshThisTick` in the authoritative §3 struct definition. `IsForceRefreshed` does not exist.  
**Fix:** Replace `IsForceRefreshed` with `ForcedRefreshThisTick` in all five FR tests.

---

### Blocker 3 — Section 5 references two removed identifiers

**Scope:** `section-5-1-to-5-10.md` or `section-5-11-to-5-17.md`

| Test | Phantom identifier | Notes |
|---|---|---|
| PS-005 | `PRESSURE_FOV_THRESHOLD` | Constant removed from §3.10; no replacement defined |
| SNAP-007 | `IsInBlindSide` | Field removed from structs; not present in any current contract |

**Fix:** For each test, either (a) restore the removed identifier with justification and add it back to §3, or (b) rewrite the test to use currently defined identifiers. Whichever path is chosen must be consistent with §3 as the implementation authority.

---

### Blocker 4 — Section 5 prerequisite header cites stale versions

**Scope:** `section-5-1-to-5-10.md` — prerequisite header block  
**Issue:** Header reads "Section 2 v1.1, Section 3 v1.2." Current versions are Section 2 v1.2 and Section 3 v1.3.  
**Fix:** Update prerequisite header to "Section 2 v1.2, Section 3 v1.3."

---

## NON-BLOCKING FINDINGS

These do not block approval but must be addressed before final sign-off or in the next revision cycle.

### NB-1 — §3.10 constants table counts do not match paragraph

**Location:** `section-3-7-to-3-10.md`, §3.10  
**Issue:** Paragraph states 17 constants with 3 `[CROSS]` entries. The table has 18 rows and 4 `[CROSS]` entries. Additionally, `[PHYS]` appears in the legend preamble but in no actual table row.  
**Action:** Reconcile paragraph text with table: update the stated count (17→18 or remove one row), update the `[CROSS]` count (3→4 or audit which entry should not be `[CROSS]`), and either add a `[PHYS]` row or remove `[PHYS]` from the legend.

---

### NB-2 — SNAP-003, SNAP-008, SNAP-010 test `PerceptionDiagnostics` fields but label struct as `PerceptionSnapshot`

**Location:** SNAP test group in §5  
**Issue:** Tests assert on `BlindSideWindowActive`, `BlindSideWindowExpiry`, and `EffectiveFoVAngle`, which are defined in `PerceptionDiagnostics`. The test bodies reference these fields as members of `PerceptionSnapshot`, which does not exist.  
**Action:** Correct the struct name in these three tests to `PerceptionDiagnostics`. Also confirm intent: are these diagnostics tests (if so, clarify in test description) or are these fields expected to be promoted to `FilteredView`?

---

### NB-3 — §8.5 citation summary row and §8.6.8 heading use `PerceptionSnapshot`

**Location:** `section-8-1-to-8-5.md` §8.5; `section-8-6-to-8-8.md` §8.6.8  
**Issue:** Two references to the old struct name remain in Section 8.  
**Action:** Update both to the correct struct name (`FilteredView` or `PerceptionDiagnostics` as appropriate to the context).

---

### NB-4 — `[CROSS]` tag is not in the `CLAUDE.md` canonical tag list

**Location:** §3.10 constants table; `CLAUDE.md` tag convention  
**Issue:** `CLAUDE.md` defines four canonical tags: `[GT]`, `[EST]`, `[FIXED]`, `[DERIVED]`. The `[CROSS]` tag used in §3.10 for constants sourced from other specs is a Perception-local extension not in the canonical list.  
**Action:** Lead developer ruling required: either add `[CROSS]` to the canonical list in `CLAUDE.md` (and define when it is appropriate vs `[DERIVED]`), or replace `[CROSS]` entries with whichever canonical tag best fits.

---

### NB-5 — Agent Movement #2 is IN REVIEW in registry but marked APPROVED in section files

**Location:** §1, §4 dependency tables  
**Issue:** `SPEC_INDEX.md` shows Agent Movement #2 as IN REVIEW. Section files that list it as a dependency mark it APPROVED. The interfaces appear stable but procedural sign-off is pending.  
**Action:** No spec change required now. Verify Agent Movement #2 status before final Perception System sign-off. If it remains IN REVIEW at that time, document the dependency risk explicitly.

---

## ITEMS VERIFIED — NO ACTION REQUIRED

- **Struct field counts:** `FilteredView` 9 fields, `PerceptionDiagnostics` 7 fields, `PerceivedAgent` 4 fields — match confirmed between §2 and §3.
- **Key Decisions:** KD-1 through KD-7 all locked and internally consistent.
- **Open Questions:** OQ-1 through OQ-5 all resolved in `outline.md`.
- **Performance budget:** 2 ms / 22 agents target, O(n×k) complexity, zero-GC requirement — all documented in §6.
- **Academic references:** 7 of 8 DOIs verified; FRANKS-1985 is pre-DOI era (acceptable); HEADRICK-2012 unresolved but non-blocking.
- **Failure modes:** 11 modes across §4 with clear ownership boundaries.

---

## APPROVAL DECISION

**Checklist result:** Four critical blockers open. Approval withheld.

**Decision:** ❌ **DO NOT APPROVE**

### Required actions before sign-off

1. Fix `PerceptionSnapshot` references throughout `section-1.md` (Blocker 1).
2. Replace `IsForceRefreshed` with `ForcedRefreshThisTick` in FR-001–005 (Blocker 2).
3. Resolve PS-005 (`PRESSURE_FOV_THRESHOLD`) and SNAP-007 (`IsInBlindSide`) — either restore identifiers in §3 or rewrite tests (Blocker 3).
4. Update §5 prerequisite header to "Section 2 v1.2, Section 3 v1.3" (Blocker 4).
5. Reconcile §5.17 integration test count with §5.11 actual count (73 + 15, not 73 + 12).
6. Address NB-1 through NB-5 at minimum before or concurrent with final sign-off.

### Re-verification exit criteria

Approval may proceed only when all of the following are true:

- No occurrence of `PerceptionSnapshot` remains in §1, §5, or §8.
- All test field names and constants in §5 map to identifiers present in §3.
- §5 prerequisite versions match current §2 and §3 versions.
- §5.17 integration test count matches the count of integration tests physically present in §5.11.
- §3.10 paragraph counts match table row counts.
- Lead developer has ruled on the `[CROSS]` tag convention.
- Agent Movement #2 status is confirmed compatible with sign-off (APPROVED or risk documented).

---

## SIGN-OFF

**Lead Developer Approval:**

- [ ] I have reviewed the specification and this checklist.
- [ ] All critical blockers have been resolved and re-verified.
- [ ] I approve Perception System Specification #7 for implementation.
- [ ] Date:

---

## CHANGELOG

| Version | Date | Author | Changes |
|---|---|---|---|
| 1.4 | April 19, 2026 | Claude (AI) / Anton | Full rewrite based on complete verification pass. Separated 4 critical blockers individually. Added §5.11 vs §5.17 integration test count discrepancy. Added NB-1 (§3.10 counts), NB-2 (SNAP struct name), NB-3 (§8 stale naming). Corrected test total to reflect actual counts. |
| 1.3 | April 19, 2026 | Claude (AI) / Anton | Addressed review feedback: normalized file inventory, clarified blocker remediation targets, tightened exit criteria. |
| 1.2 | April 19, 2026 | Claude (AI) / Anton | Reworked to match canonical Section 9 checklist style. |
| 1.1 | April 19, 2026 | Claude (AI) / Anton | Critique-driven rewrite with explicit pass/partial/blocked states. |
| 1.0 | April 19, 2026 | Claude (AI) / Anton | Initial checklist draft. |

---

**END OF APPROVAL CHECKLIST**
