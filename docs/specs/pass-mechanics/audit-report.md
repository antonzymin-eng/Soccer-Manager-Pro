# Pass Mechanics Specification #5 — Comprehensive Audit Report

**File:** `Pass_Mechanics_Spec_Comprehensive_Audit.md`
**Purpose:** Documents all gaps, errors, and inconsistencies found during a
file-by-file audit of all 12 Pass Mechanics specification files. This audit
follows the same methodology established for the Ball Physics (#1), Agent
Movement (#2), and Collision System (#3) comprehensive audits.

**Created:** March 6, 2026, 12:00 PM PST
**Author:** Claude (AI) with Anton (Lead Developer)
**Specification Audited:** Pass Mechanics Specification #5 (approved February 22, 2026)
**Files Audited:** 12 of 12

---

## AUDIT VERDICT: NOT READY FOR IMPLEMENTATION

**Total findings: 19**
- Critical: 5
- Major: 7
- Moderate: 4
- Minor: 3

The specification was approved on February 22, 2026, but this post-approval audit
reveals that the core technical specification (Section 3) is fundamentally incomplete.
Seven of nine subsections were never written. The Approval Checklist contains
fabricated consistency checks that do not match any existing file content.
Implementation cannot proceed until all Critical and Major findings are resolved.

---

## CRITICAL FINDINGS (5)

---

### C-01: Sections §3.3–§3.9 Were Never Written

**Severity:** CRITICAL — Blocks implementation entirely
**Location:** Missing files; referenced by §2, §4, §5, §6, §7, §8, Appendices
**Evidence:**

Only two Section 3 files exist in the project:
- `Pass_Mechanics_Spec_Section_3_1_v1_1.md` (Pass Type Taxonomy)
- `Pass_Mechanics_Spec_Section_3_2_v1_0.md` (Pass Velocity Model)

The following subsections, each assigned to a Functional Requirement in §2.1, have
no specification file:

| Missing Subsection | FR Owner | Description |
|---|---|---|
| §3.3 | FR-03 | Launch Angle Derivation |
| §3.4 | FR-04 | Spin Vector Calculation |
| §3.5 | FR-05 | Deterministic Error Model |
| §3.6 | FR-06, FR-07 | Target Resolution (player + space) |
| §3.7 | FR-08 | Weak Foot Penalty Model |
| §3.8 | FR-09 | Pass Execution State Machine |
| §3.9 | FR-10 | Event Publishing |

The Approval Checklist §9 (line 27) claims: "Pass_Mechanics_Spec_Section_3_2_v1_0.md
| 1.0 | ✅ Complete | §3.2–§3.9: velocity, launch angle, spin, error, target
resolution, weak foot, state machine, event publishing." This is false — that file
contains only §3.2 (Pass Velocity Model, 478 lines) and ends with the line:
"*Next subsection: §3.3 — Launch Angle Derivation*".

**Impact:** The entire implementation layer — formulas, constants, pseudocode, and
design decisions for 7 of 9 sub-systems — is unspecified. Appendix A contains
formula *derivations* for these subsections, and Appendix B contains numerical
*verifications*, but the authoritative specification text with implementation
references, constants tables, failure modes, and design decision documentation
does not exist.

**Resolution:** Draft §3.3–§3.9 as individual files (or a combined file) before
any implementation work begins. Each subsection must include: formula reference,
constants table with [GT]/[EST]/[FIXED]/[DERIVED] tags, pseudocode, boundary
verification, failure modes, and design decisions — matching the quality standard
established by §3.1 and §3.2.

---

### C-02: Pass Type Enum Mismatch — §3.1 vs §9 Consistency Audit

**Severity:** CRITICAL — Checklist verifies against non-existent data
**Location:** §3.1.2 (enum definition) vs §9 Internal Consistency Audit (line 106)

**§3.1.2 defines the PassType enum as 7 types + 3 cross subtypes:**
```
PassType: Ground, Driven, Lofted, ThroughBall, AerialThrough, Cross, Chip
CrossSubType: Flat, Whipped, High
```

**§9 Consistency Audit (line 106) lists 9 completely different names:**
```
GROUND_DIRECT, GROUND_THROUGH_BALL, GROUND_CROSS, DRIVEN_DIRECT,
DRIVEN_CROSS, LOFTED, CHIP, BACK_HEEL, HEADER
```

The §9 audit also says "Pass type count: 9 types" and marks this as ✅ PASS.
But §3.1 explicitly defines 7 types. The names GROUND_DIRECT, BACK_HEEL, and
HEADER do not exist anywhere in §3.1 or any other section. The §9 consistency
audit appears to have been written against a different (non-existent) version of
the enum, or was fabricated without cross-checking the actual §3.1 file.

**Impact:** The Approval Checklist's internal consistency audit is unreliable.
If the pass type names in §9 were used for implementation, the result would not
match any other section in the specification.

**Resolution:** Rewrite the §9 Internal Consistency Audit to verify against actual
§3.1.2 enum values: 7 PassType values + 3 CrossSubType values = 9 physical profiles
(not 9 PassType values). Correct all names to match §3.1.2.

---

### C-03: Decision Tree Numbered #7 — Should Be #8

**Severity:** CRITICAL — Cross-spec numbering error, 30 instances
**Location:** All 12 Pass Mechanics files
**Evidence:**

Every Pass Mechanics file references "Decision Tree Spec #7" or "Decision Tree (#7)".
The actual specification numbers are:
- **Perception System: Spec #7** (confirmed: Perception_System_Spec_Section_1_v1_1.md line 12)
- **Decision Tree: Spec #8** (confirmed: Decision_Tree_Spec_Section_1_v1_1.md line 13)

`grep` across all Pass Mechanics files finds **30 instances** of this error.

**Impact:** Any developer or future AI session looking up "Spec #7" to find the
Decision Tree will find the Perception System instead. Cross-spec validation checks
that reference "Decision Tree §7.x" will point to wrong sections.

**Resolution:** Global find-and-replace across all 12 files:
- "Decision Tree (#7)" → "Decision Tree (#8)"
- "Decision Tree Spec #7" → "Decision Tree Spec #8"
- "#7" in Decision Tree contexts → "#8"
Must be done carefully to avoid changing "Spec #7" references that correctly point
to the Perception System.

---

### C-04: Fatigue Convention Inverted in §2 FR-02 Acceptance Criteria

**Severity:** CRITICAL — Would cause test implementation to verify wrong behaviour
**Location:** Section 2, line 113 (FR-02 acceptance criteria #2)

**§2 FR-02 states:**
> "Fatigue = 1.0 (fully rested) produces higher kickSpeed than Fatigue = 0.0
> (exhausted), all else equal."

**Every other file in the specification uses the opposite convention:**
- §3.2 line 81: "Fatigue | [0.0, 1.0] | 0 = fully rested; 1 = fully fatigued"
- §3.2.5 boundary table: "0.0 (fully rested) → 1.00 | 1.0 (fully fatigued) → 0.80"
- Appendix A.1.3: "F = 0.0: FatigueModifier = 1.0 (no degradation)"
- Appendix C.4: Fatigue increases moving down the table (more fatigue = more error)

**Impact:** If an implementer follows §2 FR-02 literally, they would write the
FatigueModifier to *increase* velocity as Fatigue rises from 0→1, producing the
exact opposite of intended behaviour. The acceptance test PV-008 (fatigue
monotonicity) would pass with inverted logic if written from §2.

**Resolution:** Correct §2 FR-02 acceptance criteria #2 to:
"Fatigue = 0.0 (fully rested) produces higher kickSpeed than Fatigue = 1.0
(fully fatigued), all else equal."

---

### C-05: §9 Has Dual Contradictory Approval Status

**Severity:** CRITICAL — Specification approval state is ambiguous
**Location:** Section 9, lines 12–13

Lines 12–13 read:
```
**Status:** ✅ APPROVED — Lead developer sign-off granted February 22, 2026
**Status:** PENDING — Lead developer sign-off required (all ERR blockers resolved)
```

Both lines exist simultaneously. The second line was presumably the original v1.0
text and was never removed when the APPROVED line was added at v1.2.

**Impact:** The formal approval state of the specification is ambiguous in the
authoritative document. An automated or human audit reading both lines cannot
determine the intended status.

**Resolution:** Remove the "PENDING" status line. Retain only the "APPROVED" line.
(Note: Given finding C-01, the approval itself may need to be reconsidered, but
the duplicate status line is independently a document error.)

---

## MAJOR FINDINGS (7)

---

### M-01: Chip `distMax` Conflict — §3.1 Says 20m, §3.2 Says 25m

**Severity:** MAJOR — Direct constant contradiction between authoritative sections
**Location:** §3.1.11 (lines 188, 206, 481) vs §3.2.8 (line 347) and §3.2.9 (line 378)

**§3.1 (declared authoritative for profiles):**
- Master table line 188: Chip distMax = 20 m
- Prose line 481: "Chip.distMax = 20 m is conservative and intentional."
- OI-3.1-04 line 591: "Chip distMax = 20 m may be too conservative"

**§3.2 (velocity model):**
- Boundary check line 347: "K = 20, D = 25m (D_MAX_Chip)"
- Constants table line 378: "Chip | 5.0 | 6.0 | 14.0 | 25.0"

The §3.2 boundary verification (Check 5, PV-005) uses D_MAX_Chip = 25m, which
would produce a correct V_MAX result only if distMax is actually 25. But §3.1
explicitly set it to 20m with documented rationale. These cannot both be correct.

**Resolution:** Determine the intended value and update the other section.
If §3.1 (20m) is correct, §3.2.8 Check 5 and §3.2.9 constants table must be
corrected to 20m, and the PV-005 boundary verification must be recalculated.

---

### M-02: Chip `vMax` Prose Does Not Match Table in §3.1

**Severity:** MAJOR — Internal inconsistency within single section
**Location:** §3.1.11 line 477 vs §3.1.4 master table line 188

**§3.1.11 prose (line 477):**
> "The velocity range (8–16 m/s) is narrow and low. A chip requiring more than
> 16 m/s to reach the target..."

**§3.1.4 master table (line 188):**
> Chip: vMin = 5.0, vOffset = 6.0, **vMax = 14.0**

The prose says the range tops out at 16 m/s and that 16+ is out of envelope.
The table says vMax = 14.0. The vMin in the prose (8) also doesn't match the
table (5.0). The prose appears to be from an earlier draft before the profile
values were revised.

**Resolution:** Update §3.1.11 prose to match the authoritative table values:
"The velocity range (5–14 m/s) is narrow and low. A chip requiring more than
14 m/s..."

---

### M-03: §2.4.3 Physical Profile Table Is Stale

**Severity:** MAJOR — Provisional values never updated to match §3.1 authoritative values
**Location:** §2 line 608–618 vs §3.1.4 master table and §3.2.9

§2.4.3 contains a "Provisional physical profile values" table. Many values
differ significantly from the §3.1/§3.2 authoritative values:

| Pass Type | §2 V_MIN | §3.2 vMin | §2 V_MAX | §3.2 vMax |
|---|---|---|---|---|
| Ground | 8 | 5.0 | 22 | 18.0 |
| Driven | 18 | 10.0 | 30 | 28.0 |
| Lofted | 12 | 8.0 | 24 | 22.0 |
| ThroughBall | 10 | 6.0 | 22 | 20.0 |
| Chip | 8 | 5.0 | 18 | 14.0 |

While §2 marks these as "Provisional," the discrepancies are large enough that
someone reading §2 in isolation would get materially wrong expectations. The §2
table also lacks the `vOffset` column entirely.

**Resolution:** Either update §2.4.3 to match §3.1/§3.2 authoritative values,
or add a prominent note: "SUPERSEDED — See §3.1.4 and §3.2.9 for authoritative
profile values."

---

### M-04: Test Count Mismatch Between §5 and §9

**Severity:** MAJOR — Different test counts for the same categories
**Location:** §5 line 93–105 (§5.1.3 table) vs §9 lines 288–300 (Test Coverage Summary)

| Category | §5.1.3 Count | §9 Count | Match? |
|---|---|---|---|
| PT- | 8 | 8 | ✅ |
| PV- | 12 | 12 | ✅ |
| LA- | 8 | 8 | ✅ |
| SV- | **8** | **6** | ❌ |
| PE- | 10 | 10 | ✅ |
| TR- | **16** | **8** | ❌ |
| WF- | 6 | 6 | ✅ |
| PSM- | 6 | 6 | ✅ |
| EC- | 8 | 8 | ✅ |
| IT- | 12 | 12 | ✅ |
| VS- | 6 | 6 | ✅ |
| **Total** | **100** | **100** | ✅ (but distribution differs) |

Both totals are 100, but the distribution is different: §5 says 8 SV tests and
16 TR tests; §9 says 6 SV tests and 8 TR tests. Either §5 or §9 was modified
without updating the other. Since §5 is the authoritative test section, §9 should
match it.

**Resolution:** Determine the correct counts by examining the actual test
specifications in §5.5 (SV-) and §5.7 (TR-). Update whichever section is wrong.

---

### M-05: V_OFFSET Formula Incorrectly Described in §9

**Severity:** MAJOR — Approval checklist verifies wrong formula
**Location:** §9 Internal Consistency Audit, line 117

**§9 line 117 states:**
> "V_OFFSET term present in formula | V_OFFSET = (V_MAX − V_MIN) / 2"

**Actual definition (§3.1, §3.2):**
V_OFFSET is a per-type constant independently set in PhysicalProfile. It is NOT
derived from (V_MAX − V_MIN) / 2.

Numerical proof: Ground V_MAX = 18.0, V_MIN = 5.0.
- Formula in §9: (18.0 − 5.0) / 2 = 6.5
- Actual V_OFFSET_Ground: 8.0

These do not match. The formula in §9 is fabricated and does not correspond to any
definition in §3.1 or §3.2.

**Resolution:** Correct §9 line 117 to: "V_OFFSET term present in formula |
V_OFFSET is a per-type gameplay-tuned constant in PhysicalProfile (not derived)"

---

### M-06: Section 8 Footer Version Says "1.0" but Document Is v1.2

**Severity:** MAJOR — Misleading version identifier
**Location:** §8 lines 905–907

The footer block between the end-of-content and the version history reads:
```
**Page Count:** ~18 pages estimated
**Version:** 1.0
**Constants Audited:** 66
```

But the document header says Version 1.2, and the version history at lines 915–919
confirms three versions (1.0, 1.1, 1.2). The footer was never updated.

**Resolution:** Update footer version to 1.2 (or remove the redundant version
line from the footer, since the header already carries the authoritative version).

---

### M-07: §5 §5.14.4 Regression Strategy Cites "82 unit tests" but Total Is Different

**Severity:** MAJOR — Stale count in test execution guidance
**Location:** §5 line 810

§5.14.4 states: "All 82 unit tests constitute the regression suite."

But §5.1.3 counts: PT(8) + PV(12) + LA(8) + SV(8) + PE(10) + TR(16) + WF(6) +
PSM(6) + EC(8) = 82. The total in §5.1.3 then adds IT(12) + VS(6) = 100.

Meanwhile §9 gives different per-category counts that sum to a different unit test
total: PT(8) + PV(12) + LA(8) + SV(6) + PE(10) + TR(8) + WF(6) + PSM(6) + EC(8) = 72.

If the §9 counts are correct, the regression suite is 72 unit tests, not 82.
This directly affects regression testing scope.

**Resolution:** Resolve M-04 first (determine correct counts), then update §5.14.4
to match.

---

## MODERATE FINDINGS (4)

---

### Mod-01: §2 FR-03 Lofted Angle Range vs §3.1

**Location:** §2 FR-03 acceptance criteria (line 143) vs §3.1 master table (line 185)

§2 FR-03 #3: "Lofted pass (long): angle ∈ [20°, 35°]"
§3.1 master table: Lofted ANGLE_MIN = 20°, ANGLE_MAX = **45°**

The acceptance criteria uses a narrower range (35° max vs 45° max). If §3.1 is
authoritative, the FR-03 acceptance bound is wrong and would fail valid lofted
passes at long distance.

**Resolution:** Update §2 FR-03 #3 to "[20°, 45°]" to match §3.1.

---

### Mod-02: Outline Pass Type Taxonomy Diagram Omits "Lobbed"

**Location:** Outline lines 1199–1214 (Appendix E Mermaid diagram)

§1.1 (line 49) lists "Chip/Lobbed" and the Outline (line 41) says "chip, lobbed."
The Mermaid diagram at Outline line 1209 labels the node as "CH[Chip]" with no
mention of "Lobbed." If Chip and Lobbed are synonyms, this should be explicit. If
they are distinct, Lobbed is missing from the taxonomy.

**Resolution:** Clarify in §3.1: Chip and Lobbed are the same pass type (Chip is
the implementation name). Or add Lobbed as a separate type if distinct.

---

### Mod-03: §4 Prerequisites Reference §3.1 v1.0 but Current Is v1.1

**Location:** §4 header, line 15

§4 prerequisites say "Section 3.1 (v1.0)" but the current §3.1 is v1.1 (which
added the critical `vOffset` field). §4 was not updated when §3.1 was revised.

**Resolution:** Update §4 prerequisites to reference §3.1 v1.1.

---

### Mod-04: Outline Open Questions OQ-6 Never Explicitly Resolved

**Location:** Outline line 1235 (OQ-6)

OQ-6 asks: "Does Ball Physics KickType enum cover all 7 pass types?"

The resolution was that KickType was removed entirely (ERR-005/ERR-006 — parameter-
based physics approach adopted). However, OQ-6 is never marked as resolved in the
Outline, and no resolution note was added. While ERR-006 closure implicitly resolves
this, the Outline should document it for traceability.

**Resolution:** Add resolution note to OQ-6 in the Outline: "Resolved — KickType
enum removed per ERR-005/ERR-006. Pass type is fully encoded in velocity and spin
vectors. No mapping required."

---

## MINOR FINDINGS (3)

---

### Min-01: §1 Header Lists "Spec Error Log v1.0" but Current Is v1.4

**Location:** §1 line 21

Section 1 references "Spec Error Log v1.0 (active)" but the current Error Log
version is v1.4. This is a stale version reference in a non-authoritative context
(the error log version at time of §1 drafting was v1.0).

**Resolution:** No action required. Note for completeness only.

---

### Min-02: §6 Profiling Budget Inconsistency

**Location:** §6.7 line 611 vs §6.3 (budget)

§6.7 says the primary gate for `PassMech.Evaluate` is "p95 < 0.05ms."
§6.3 and §6.11 consistently use "p95 < 0.05ms" as the target.
§2.5 NFR-02 says "CONTACT execution ≤ 0.05ms."
§5.1.5 says "EvaluatePass() mean < 0.15ms, p99 < 0.25ms."

The §5.1.5 mean target (0.15ms) is 3× the §6 p95 target (0.05ms). A system where
the mean is 3× the p95 is mathematically impossible (mean ≤ p95 always). §5.1.5
appears to have been written with different (looser) targets than §6.

**Resolution:** Align §5.1.5 performance targets with §6: mean < 0.03ms (must be
below p95), p95 < 0.05ms, p99 < 0.10ms. Or document that §5 uses different
measurement scope.

---

### Min-03: §7 and §4 Both Mention "Event System (#17)" — Spec Not Yet Defined

**Location:** §4 line 46, §7 line 703 and others

The Event System is consistently referenced as Spec #17 but does not exist in the
project. This is not an error — it is a forward reference to an unwritten
specification — but the number #17 has no source. If spec numbering changes, 
all references will need updating.

**Resolution:** No immediate action. Track as a dependency. When Event System is
assigned a number, verify it matches #17.

---

## SUMMARY TABLE

| ID | Severity | Section(s) | Short Description |
|---|---|---|---|
| C-01 | CRITICAL | §3.3–§3.9 | Seven subsections never written |
| C-02 | CRITICAL | §3.1 vs §9 | Pass type enum names completely different |
| C-03 | CRITICAL | All files | Decision Tree #7 → should be #8 (30 instances) |
| C-04 | CRITICAL | §2 FR-02 | Fatigue convention inverted |
| C-05 | CRITICAL | §9 | Dual contradictory approval status |
| M-01 | MAJOR | §3.1 vs §3.2 | Chip distMax: 20m vs 25m |
| M-02 | MAJOR | §3.1 | Chip vMax prose says 16, table says 14 |
| M-03 | MAJOR | §2 vs §3.1/§3.2 | §2 profile table stale, never updated |
| M-04 | MAJOR | §5 vs §9 | SV and TR test counts differ |
| M-05 | MAJOR | §9 | V_OFFSET formula wrong in consistency check |
| M-06 | MAJOR | §8 | Footer says v1.0, document is v1.2 |
| M-07 | MAJOR | §5 | "82 unit tests" count may be wrong |
| Mod-01 | MODERATE | §2 vs §3.1 | Lofted angle range mismatch |
| Mod-02 | MODERATE | Outline | "Lobbed" missing from taxonomy diagram |
| Mod-03 | MODERATE | §4 | Prerequisites cite §3.1 v1.0, current is v1.1 |
| Mod-04 | MODERATE | Outline | OQ-6 never marked resolved |
| Min-01 | MINOR | §1 | Stale Spec Error Log version reference |
| Min-02 | MINOR | §5 vs §6 | Performance mean target > p95 (impossible) |
| Min-03 | MINOR | §4, §7 | Event System #17 is unverified forward reference |

---

## RECOMMENDED RESOLUTION ORDER

**Phase 1 — Structural (blocks all other work):**
1. Draft §3.3–§3.9 specification files (C-01)
2. Fix Decision Tree numbering #7 → #8 across all files (C-03)

**Phase 2 — Consistency (blocks approval re-verification):**
3. Rewrite §9 Internal Consistency Audit against actual file contents (C-02, M-05)
4. Fix §9 dual status (C-05)
5. Fix §2 FR-02 fatigue convention (C-04)
6. Resolve Chip distMax 20m vs 25m and update affected files (M-01)
7. Fix Chip vMax prose in §3.1.11 (M-02)
8. Update §2.4.3 profile table or mark superseded (M-03)

**Phase 3 — Reconciliation:**
9. Resolve SV/TR test counts between §5 and §9 (M-04, M-07)
10. Fix §8 footer version (M-06)
11. Fix §5.1.5 performance targets (Min-02)
12. Moderate and minor fixes (Mod-01 through Mod-04, Min-01, Min-03)

**Phase 4 — Re-audit:**
13. Re-run the §9 Approval Checklist against actual file contents once all fixes land

---

## CRITIQUE OF THIS AUDIT

**Strengths:**
- Every finding includes file name, line number, and verbatim evidence
- Severity ratings are conservative — nothing is inflated
- C-01 (missing sections) is definitive and verifiable with a single `ls` command
- Cross-references between findings are identified (M-04 feeds M-07; C-02 feeds M-05)

**Weaknesses / Risks:**
1. This audit cannot verify the *content quality* of §3.3–§3.9 because those sections
   do not exist. Once written, a second audit pass will be needed.
2. The Appendices contain formula derivations and numerical verifications for the
   missing subsections. These may be correct despite the sections not existing as
   formal spec files. However, Appendix content alone is insufficient — the spec
   sections provide the authoritative implementation reference, constants tables,
   failure modes, and design decision documentation that Appendices do not.
3. Some findings (M-03, Min-01) are "stale reference" issues that may be considered
   acceptable in a living document. The audit flags them conservatively.
4. The 30-instance Decision Tree numbering error (C-03) is pervasive but mechanically
   fixable with find-and-replace. The risk is collateral damage to legitimate "#7"
   references to the Perception System. Manual verification after replacement is needed.

---

**END OF AUDIT**

*Pass Mechanics Specification #5 — Comprehensive Audit Report*
