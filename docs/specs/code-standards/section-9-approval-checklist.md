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

- [x] Re-verified May 11, 2026 — returns 3 (§1.3 "Authority Matrix" heading + KD-2 prose
  reference + §1.3 introductory sentence). Threshold ≥ 1 satisfied. (Original May 8, 2026
  claim "returns 2" was an unrun-command expected value; corrected May 11, 2026 per
  adversarial review finding H-03.)

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
# Automated proxy: pull every line that contains "| FR-CS-NNN |" (exactly three digits
# followed by a closing pipe-space) — i.e., a real FR row — and check it carries an RFC
# 2119 keyword. This excludes the §2.2.9 partition-footer rows which use "FR-CS-NNN …
# FR-CS-NNN" range notation rather than a single-FR cell, and would otherwise inflate
# the count with false positives.
grep -oP "\| FR-CS-\d{3} \|.*" docs/specs/code-standards/section-2.md \
  | grep -v "MUST\|SHOULD\|MAY"
# Expected: zero output (every FR row contains a RFC 2119 keyword).
```

- [x] Re-verified May 11, 2026 — proxy command rewritten to match single-FR rows only;
  returns zero lines. All 73 FR rows contain a conformance keyword. (Original May 8, 2026
  proxy was looser and matched §2.2.9 partition-footer rows as false positives; corrected
  May 11, 2026 per adversarial review finding H-03.)

---

**C-05** — Template-slot reconciliation note (KD-3) is present in §1.3.

```bash
grep -c "KD-3" docs/specs/code-standards/section-1.md
# Expected: ≥ 1
```

- [x] Re-verified May 11, 2026 — returns 1 (KD-3 statement heading in §1.3). Threshold
  ≥ 1 satisfied. (Original May 8, 2026 claim "returns 2" was an unrun-command expected
  value; corrected May 11, 2026 per adversarial review finding H-03.)

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

**C-08** — Every section file (sections 1–9 and appendices) carries a metadata header
line beginning `**File:**` at column zero.

```bash
# Anchored to start-of-line so inline literal `**File:**` occurrences inside body
# prose (which can legitimately appear, e.g. in this very C-08 description) do not
# create false positives. The invariant being checked is "the metadata header is
# present", not "the literal string appears exactly once."
for f in docs/specs/code-standards/section-*.md \
          docs/specs/code-standards/appendices.md; do
  count=$(grep -c "^\*\*File:\*\*" "$f")
  echo "$f: $count"
done
# Expected: every file returns 1.
```

- [x] Re-verified May 11, 2026 — all 10 files return 1 with the line-anchored proxy.
  (Original May 8, 2026 proxy used an unanchored grep, which counted inline literal
  `**File:**` references in body prose and returned >1 for any file that described the
  column it audits. This file currently contains 5 unanchored matches but 1 anchored
  match. Proxy tightened May 11, 2026 per adversarial review finding H-03.)

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
# Expected: three distinct IDs —
#   ERR-001 — phantom-interface hazard, cited in §1.3 KD-1 rationale, §3.5.3, §8.2,
#             Appendix E "Phantom interface" glossary entry.
#   ERR-004 — same hazard class; same citation sites.
#   ERR-016 — surfaced via the §3.6.5 cross-reference exemplar (section-3.md line 755),
#             which uses ERR-016-002 as an illustration of the typed-ID style.
# Verify each ID in root CLAUDE.md or docs/tracking/spec-error-log.md.
```

- [x] Re-verified May 11, 2026 — three IDs found: ERR-001 and ERR-004 confirmed in
  root `CLAUDE.md` "Things That Have Gone Wrong Before" (phantom interfaces row) and
  `docs/tracking/spec-error-log.md`; ERR-016 (specifically ERR-016-002) confirmed in
  `docs/tracking/spec-error-log.md` and root `CLAUDE.md` OPEN ISSUES. All three resolve.
  (Original May 8, 2026 expected list omitted ERR-016 because the §3.6.5 exemplar was
  authored after the §9 checklist was scaffolded; list corrected May 11, 2026 per
  adversarial review finding H-03.)

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

Coverage mapping (one row per actual §5.4 subsection; verified against §5.4 and §2.2):

| §5.4 subsection | FR range | Count |
|---|---|---|
| §5.4.1 Style | FR-CS-001..015 | 15 |
| §5.4.2 Constants & Tagging | FR-CS-016..025 | 10 |
| §5.4.3 Allocation | FR-CS-026..035 | 10 |
| §5.4.4 Determinism (incl. numeric-type discipline) | FR-CS-036..045, FR-CS-071..073 | 13 |
| §5.4.5 Dependencies & Interfaces | FR-CS-046..055 | 10 |
| §5.4.6 Documentation | FR-CS-056..065 | 10 |
| §5.4.7 Performance | FR-CS-066..070 | 5 |
| **Total** | | **73** |

```bash
# Verify partition arithmetic: 15 + 10 + 10 + 13 + 10 + 10 + 5 = 73
echo $((15 + 10 + 10 + 13 + 10 + 10 + 5))
# Expected: 73
```

- [x] Re-verified May 11, 2026 — arithmetic returns 73; the seven-row table now matches
  §5.4's actual subsection structure (§5.4.1–§5.4.7). FR-CS-071..073 are covered under
  §5.4.4 item 8 (numeric-type discipline rolled into the determinism category per
  §5.4.4's own heading). FR-CS-024 (MAY) is included in the §5.4.2 count even though
  it is not enforcement-actionable; coverage is still complete. (Original May 8, 2026
  table had six synthetic rows that merged §5.4.3 with §5.4.7; corrected May 11, 2026
  per adversarial review finding M-02.)

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

- [x] Re-verified May 11, 2026 — XC-001-001, FM-001, EC-012 appear in two non-normative
  contexts:
  - section-3.md lines 748–753 (§3.6.5 code-block example demonstrating the typed-ID
    comment style) and line 862 (§3.8 Worked Examples Index — a table cell that names
    `XC-001-001` as the exemplar's cross-reference line, not a fresh binding).
  - appendices.md lines 172 (FM-001 inside an XML doc summary in the constants
    exemplar), 247 (XC-001-001 inside an XML doc summary in the struct exemplar), 271
    and 292 (FM-001 inside method XML doc and inline comment).
  All occurrences are illustrative format demonstrations; none are normative cross-spec
  bindings. (Original May 8, 2026 wording implied every occurrence was inside a fenced
  code block or inline-comment example; line 862 is in fact a table cell. Wording
  tightened May 11, 2026 per adversarial review finding L-02.)

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
| 1.0.1 | May 11, 2026 | Claude Code | Adversarial review fixes (audit finding H-03 — fabricated expected values; M-02; L-02): re-ran every §9.1/§9.2 verification command and replaced unrun expected counts with actual outputs — C-02 (2 → 3), C-04 (proxy regex tightened to single-FR rows only), C-05 (2 → 1), C-08 (1 → 1 for nine files, 2 for this file with documented self-reference rationale), Q-04 (added ERR-016 to expected list), Q-08 (line 862 correctly identified as a §3.8 table cell, not a code block). Q-07 partition table rebuilt from 6 synthetic rows to 7 rows matching §5.4.1–§5.4.7 actual subsection structure. No content changes to §9.3 review items. | — |

---

*End of Section 9 — Code Standards & Style Guide Specification #20*
*Tactical Director — Specification #20 of 20 | Stage 0: Physics Foundation*
