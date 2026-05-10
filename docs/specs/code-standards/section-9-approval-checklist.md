# Code Standards & Style Guide Specification #20 — Section 9: Approval Checklist

**File:** `docs/specs/code-standards/section-9-approval-checklist.md`
**Purpose:** Quality gate for Spec #20. All items must be checked before the spec moves
from `IN REVIEW` to `APPROVED` in `SPEC_INDEX.md`. Items are programmatically verifiable
against source files unless marked `[manual]`.
**Created:** May 8, 2026
**Version:** 1.0
**Status:** DRAFT
**Specification Number:** 20 of 20 (Stage 0 — Physics Foundation)
**Authoring spec:** `outline-detailed.md` v1.3, §SECTION 9; `outline-mid.md` v1.2, §9.1–§9.4

> **Fabrication prohibition (root CLAUDE.md):** All values below must be verified against
> actual file content. No expected values may be stated before running the verification
> command. Checked boxes (`[x]`) are set only after the command returns the expected result.

---

## Table of Contents

- [9.1 Content Checklist](#91-content-checklist)
- [9.2 Quality Checklist](#92-quality-checklist)
- [9.3 Review Checklist](#93-review-checklist)
- [9.4 Decision](#94-decision)
- [9.5 Version History](#95-version-history)

---

## 9.1 Content Checklist

All items are verified against files in `docs/specs/code-standards/`.

**C-01** — All nine required section files are present.

```bash
ls docs/specs/code-standards/section-{1,2,3,4,5,6,7,8,9-approval-checklist}.md \
   docs/specs/code-standards/appendices.md
# Expected: 10 lines, no "No such file" errors.
```

- [x] Verified May 8, 2026 — all 10 files present.

---

**C-02** — Authority Matrix is present in §1.3.

```bash
grep -c "Authority Matrix" docs/specs/code-standards/section-1.md
# Expected: ≥ 1
```

- [x] Verified May 8, 2026 — returns 2 (heading + body reference).

---

**C-03** — §2.2 contains exactly 73 FR rows (15 style + 10 constants + 10 alloc +
10 det + 10 deps + 10 docs + 5 perf + 3 numeric type).

```bash
grep -oP "\| FR-CS-\d+ \|" docs/specs/code-standards/section-2.md \
  | grep -oP "FR-CS-\d+" | sort -u | wc -l
# Expected: 73
```

- [x] Verified May 8, 2026 — returns 73.

Partition spot-checks:

```bash
# Style group: FR-CS-001..015 (15 FRs)
grep -oP "\| FR-CS-0(0[1-9]|1[0-5]) \|" docs/specs/code-standards/section-2.md | wc -l
# Expected: 15

# Numeric Type group: FR-CS-071..073 (3 FRs)
grep -oP "\| FR-CS-07[1-3] \|" docs/specs/code-standards/section-2.md | wc -l
# Expected: 3
```

- [x] Verified May 8, 2026 — style returns 15; numeric-type returns 3.

---

**C-04** — Every FR row in §2.2 contains a conformance level, a source citation, and a
mechanics-section pointer.

```bash
# Visual scan: every row in the §2.2 table has four pipe-delimited cells —
# ID | Statement | Level | Source | Mechanics §
# Automated proxy: no FR row has an empty "Level" cell (MUST/SHOULD/MAY/MUST NOT present).
grep "| FR-CS-" docs/specs/code-standards/section-2.md \
  | grep -v "MUST\|SHOULD\|MAY" | grep -v "^#"
# Expected: zero output (every FR row contains a RFC 2119 keyword).
```

- [x] Verified May 8, 2026 — zero output; all 73 FR rows contain a conformance keyword.

---

**C-05** — Template-slot reconciliation note (KD-3) is present in §1.3.

```bash
grep -c "KD-3" docs/specs/code-standards/section-1.md
# Expected: ≥ 1
```

- [x] Verified May 8, 2026 — returns 2.

---

**C-06** — Appendices A through E are all present in `appendices.md`.

```bash
grep "^## Appendix" docs/specs/code-standards/appendices.md
# Expected: 5 lines beginning "## Appendix A" through "## Appendix E"
```

- [x] Verified May 8, 2026 — returns Appendix A, B, C, D, E and the Appendix Version
  History heading (6 lines total; A–E confirmed present).

---

**C-07** — Exemplar pair (`ExemplarConstants.cs` and `ExemplarStruct.cs`) is present in
Appendix C. `[manual]` — compilation check is manual at Stage 0 (no `src/` toolchain).

```bash
grep -c "ExemplarConstants\|ExemplarStruct" docs/specs/code-standards/appendices.md
# Expected: ≥ 2 (one declaration each)
```

- [x] Verified May 8, 2026 — both names present; manual review confirms the code blocks
  are syntactically well-formed C# (no toolchain verification at Stage 0).

---

**C-08** — Every section file (sections 1–9 and appendices) carries a `**File:**` metadata
header.

```bash
for f in docs/specs/code-standards/section-*.md \
          docs/specs/code-standards/appendices.md; do
  count=$(grep -c "\*\*File:\*\*" "$f")
  echo "$f: $count"
done
# Expected: every file returns 1
```

- [x] Verified May 8, 2026 — all 10 files return 1.

---

**C-09** — Every section file carries a Version History table.

```bash
for f in docs/specs/code-standards/section-*.md \
          docs/specs/code-standards/appendices.md; do
  count=$(grep -c "Version History" "$f")
  echo "$f: $count"
done
# Expected: every file returns ≥ 1
```

- [x] Verified May 8, 2026 — all 10 files return ≥ 1.

---

## 9.2 Quality Checklist

**Q-01** — Cite-not-redefine audit: the constant tag definitions (GT/EST/FIXED/DERIVED/
CROSS) are reproduced in §3.2.1 with explicit attribution to root `CLAUDE.md`, and no
other section restates the definitions.

```bash
# Count "CLAUDE.md" attribution lines adjacent to the tag table in §3.
grep -n "CLAUDE.md" docs/specs/code-standards/section-3.md | head -5
# Expected: at least one attribution in §3.2.1 naming CLAUDE.md as source.

# Confirm the tag definitions do not appear in §2, §4, §5, §6 as fresh definitions.
grep "\[GT\]\|\[EST\]\|\[FIXED\]\|\[DERIVED\]\|\[CROSS\]" \
     docs/specs/code-standards/section-2.md \
     docs/specs/code-standards/section-4.md \
     docs/specs/code-standards/section-6.md | grep -v "FR-CS-\|§3.2"
# Expected: output is contextual usage only (in FR statements or cross-references),
# not a redefinition block.
```

- [x] Verified May 8, 2026 — §3.2.1 carries the attribution; no other section contains
  a standalone tag-definition block.

---

**Q-02** — Every entry in Appendix D is traced to an `FR-CS-###` and a `CLAUDE.md` or
`development-best-practices.md` citation column.

```bash
# Proxy: Appendix D tables contain "FR-CS-" and "CLAUDE.md" in the same section.
grep -c "FR-CS-" docs/specs/code-standards/appendices.md
# Expected: ≥ 1 (there are many; the value confirms presence, not exact count)

grep -c "CLAUDE.md\|development-best-practices" docs/specs/code-standards/appendices.md
# Expected: ≥ 1
```

- [x] Verified May 8, 2026 — `[manual]` table-level audit confirms every Appendix D row
  carries an FR-CS-### and a source citation in its respective column.

---

**Q-03** — No banned-API symbol list appears outside Appendix D (single source of truth,
KD-6). Sections §3.3, §3.4, §5.2, and §7.1 cite Appendix D by category name only.

```bash
# §3 may name individual symbols for readability but must add "See Appendix D"
# rather than reproducing a complete list.
grep -n "System\.Random\|DateTime\.Now\|Parallel\.For\|LINQ" \
     docs/specs/code-standards/section-3.md | grep -v "Appendix D\|See App"
# Expected: any hits are example/explanatory references, not complete symbol lists.
# A complete list would have ≥ 5 consecutive symbol lines; single mentions are acceptable.
```

- [x] Verified May 8, 2026 — `[manual]` audit confirms §3.3/§3.4 name individual
  symbols for explanatory context but always direct the reader to Appendix D for the
  authoritative complete list. No section other than Appendix D contains a
  multi-symbol enumeration.

---

**Q-04** — All `ERR-` cross-reference citations in Spec #20 resolve to entries in
root `CLAUDE.md` "Things That Have Gone Wrong Before" or `docs/tracking/spec-error-log.md`.

```bash
grep -oP "ERR-\d+" docs/specs/code-standards/section-*.md \
  docs/specs/code-standards/appendices.md | sort -u
# Expected: ERR-001, ERR-004 (cited in §1.3 / §3.5 for phantom-interface hazard).
# Verify each in root CLAUDE.md or spec-error-log.md.
```

- [x] Verified May 8, 2026 — ERR-001 and ERR-004 confirmed present in root `CLAUDE.md`
  "Things That Have Gone Wrong Before" (phantom interfaces row).

---

**Q-05** — All RFC 2119 conformance keywords (MUST, MUST NOT, SHOULD, SHOULD NOT, MAY)
are used correctly: normative rules use bold MUST/MUST NOT/SHOULD; informational text
does not use these words in all-caps unless normative intent is intended.

- [x] `[manual]` — reviewed May 8, 2026. All FR conformance levels in §2.2 use the
  standard keyword set. Body text uses lowercase ("must" / "should") for non-normative
  prose; all-caps forms appear only in FR statements and normative rule blocks.

---

**Q-06** — All informational pointers to other documents resolve to real paths or
confirmed-live URLs (per §8.1 source register).

```bash
# Internal paths: verify each internal document cited in §8.1 exists.
ls docs/planning/development-best-practices.md \
   docs/planning/master-development-plan.md \
   docs/tracking/certification-platform.md \
   CLAUDE.md
# Expected: 4 lines, no errors.
```

- [x] Verified May 8, 2026 — all four internal documents present. External URLs (S-05
  through S-08) verified as live on May 8, 2026 per §8.1.

---

**Q-07** — The §5.4 reviewer-checklist categories collectively cover all 73 FRs with no
gaps and no double-counting.

Coverage mapping (verified against §5.4 and §2.2):

| §5.4 category | FR range | Count |
|---|---|---|
| C-01 Style & Formatting | FR-CS-001..015 | 15 |
| C-02 Constants & Tagging | FR-CS-016..025 | 10 |
| C-03 Allocation & Performance | FR-CS-026..035 + FR-CS-066..070 | 15 |
| C-04 Determinism | FR-CS-036..045 + FR-CS-071..073 | 13 |
| C-05 Dependencies & Interfaces | FR-CS-046..055 | 10 |
| C-06 Documentation & Comments | FR-CS-056..065 | 10 |
| **Total** | | **73** |

```bash
# Verify partition arithmetic: 15 + 10 + 15 + 13 + 10 + 10 = 73
echo $((15 + 10 + 15 + 13 + 10 + 10))
# Expected: 73
```

- [x] Verified May 8, 2026 — arithmetic returns 73; §5.4 category descriptions confirmed
  to name each FR group.

---

**Q-08** — No `XC-`, `FM-`, or `EC-` cross-reference IDs appear in Spec #20 body text
as substantive cross-spec bindings (this spec defines the ID scheme but does not use it
normatively). Format examples in code blocks are permitted.

```bash
grep -oP "(XC|FM|EC)-\d+" docs/specs/code-standards/section-*.md \
  docs/specs/code-standards/appendices.md | sort -u
# Expected: XC-001, FM-001, EC-012 — all appearing inside C# code blocks in §3.6.5
# and Appendix C as format-demonstration examples only. Confirm each hit is inside
# a fenced code block or an inline-comment example, not a normative binding.
```

- [x] Verified May 8, 2026 — XC-001-001, FM-001, EC-012 appear only in the §3.6.5
  code-block examples (section-3.md lines 748–753, 862) and the Appendix C exemplar
  code (appendices.md lines 172, 247, 271, 292). All occurrences are illustrative
  format demonstrations; none are normative cross-spec bindings.

---

## 9.3 Review Checklist

Items in this section require human action and cannot be automated. They are checked by
the lead developer at review time.

**R-01** — No new open issues were created by authoring Spec #20 that require tracking in
root `CLAUDE.md` OPEN ISSUES.

- [ ] Lead developer to confirm: review Spec #20 content for any cross-spec drift or
  discovered inconsistencies; log any findings in `docs/tracking/spec-error-log.md` and
  `CLAUDE.md` OPEN ISSUES before marking this item checked.

---

**R-02** — Lead-developer sign-off captured.

- [ ] Signature / approval note: `[Name] — [Date] — Approved for IN REVIEW`

---

**R-03** — `docs/tracking/spec-error-log.md` updated if any cross-spec drift was
discovered during Spec #20 drafting.

- [ ] Confirm: either (a) no new ERR- entries required, or (b) new ERR- entries added
  and cross-referenced here.

---

**R-04** — `docs/tracking/file-manifest.md` updated to include all 10 new files in the
`code-standards/` folder.

```bash
grep -c "code-standards" docs/tracking/file-manifest.md
# Expected: ≥ 10 (one entry per file in the folder)
```

- [ ] To be verified at review time.

---

**R-05** — `docs/specs/SPEC_INDEX.md` status for Spec #20 updated through the correct
transition sequence: `NOT STARTED` → `IN REVIEW` (on review submission) →
`APPROVED` (on lead-developer sign-off).

```bash
grep "code-standards\|Code Standards" docs/specs/SPEC_INDEX.md
# Expected: line shows current status accurately.
```

- [ ] To be verified at each transition.

---

## 9.4 Decision

**Current status:** `DRAFT`

This section is completed by the lead developer at review time. The content checklist
(§9.1) and quality checklist (§9.2) must be fully checked before status advances to
`IN REVIEW`. All §9.3 items must be checked before status advances to `APPROVED`.

### Approval Evidence

*To be populated at review time. Do not pre-fill — root CLAUDE.md prohibits fabricating
approval values.*

| Evidence item | File path | Verification command result |
|---|---|---|
| 73 FR rows confirmed | `section-2.md` | `[run C-03 command at review time]` |
| Appendices A–E confirmed | `appendices.md` | `[run C-06 command at review time]` |
| File headers confirmed | all section files | `[run C-08 command at review time]` |
| Lead-developer sign-off | — | `[capture in R-02]` |

### Re-Approval Triggers

Any of the following changes MUST trigger a mandatory re-review of Spec #20 and
re-verification of all §9.1 and §9.2 checklist items:

1. **Root `CLAUDE.md` — "Constant Tags" section changes.** §3.2.1 reproduces this table
   verbatim; any tag addition, removal, or definition change requires §3.2.1 and
   Appendix D to be updated and re-reviewed.

2. **Root `CLAUDE.md` — "When Writing Code" determinism rules change.** §3.4 (Determinism
   Rules) is derived from these rules; any change to SplitMix64 requirements, `unchecked{}`
   scope, Python masking convention, or `MatchClock` requirement propagates to §3.4 and
   Appendix D `det-required-*` categories.

3. **Root `CLAUDE.md` — "Interface Design Principle" changes.** §3.5.3 cites this
   principle directly; any rewording changes the normative basis.

4. **Stage 1 calibration of numeric lint thresholds (D1 resolved).** When
   `certification-platform.md` is pinned and FR-CS-008 is activated, §2.2.1, §3.1.4,
   §5.4, and §5.5 must be updated and re-reviewed.

5. **Spec #9 (Fixed64) reaches `APPROVED`.** §3.7, §7.3, §7.5 (D4), and Appendix D
   `det-banned` all require amendment. This is a Major version bump to Spec #20.

---

## 9.5 Version History

| Version | Date | Author | Notes | Reviewer |
|---|---|---|---|---|
| 1.0 | May 8, 2026 | Claude Code | Initial authoring from `outline-detailed.md` v1.3 §SECTION 9. All §9.1 and §9.2 items verified on drafting date; §9.3 items pending lead-developer review. | — |

---

*End of Section 9 — Code Standards & Style Guide Specification #20*
*Tactical Director — Specification #20 of 20 | Stage 0: Physics Foundation*
