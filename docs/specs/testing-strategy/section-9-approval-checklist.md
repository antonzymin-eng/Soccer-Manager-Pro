# Testing Strategy & Framework Specification #19 — Section 9: Approval Checklist

**Created:** May 12, 2026
**Last Updated:** May 12, 2026
**Purpose:** Self-applied KD-6 checklist. Every row resolves to either
a named, version-controlled file path containing the claimed value, or
a programmatic check whose output is captured. Rows without that
resolution are themselves a §2.3 nonconformance.

> Spec #19's own §9 is the **first** application of KD-6 to itself.
> The auditor MUST NOT exempt Spec #19 from its own mandate.

---

## 9.1 Content Checklist

| # | Claim | Evidence (file path or programmatic check) | Status |
|---|-------|--------------------------------------------|--------|
| 9.1.1 | All required sections present (§1 … §9, appendices, slot reconciliations in §5 / §6) | `docs/specs/testing-strategy/section-1.md` … `section-9-approval-checklist.md`, `appendices.md` | [ ] |
| 9.1.2 | All FR-TS-### present in §2.2 with conformance level and activation stage | `docs/specs/testing-strategy/section-2.md` §2.2 (FR-TS-001 … 085) | [ ] |
| 9.1.3 | KD-1 codified | `docs/specs/testing-strategy/section-1.md` §1.3 KD-1 row + cross-section adoption | [ ] |
| 9.1.4 | KD-2 codified | §3.2 §5.7 §6.2 §1.4 status caveat | [ ] |
| 9.1.5 | KD-3 codified | §6.2 §6.6 §1.4 status caveat | [ ] |
| 9.1.6 | KD-4 codified | §3.5.3 §3.5.4 §5.4 | [ ] |
| 9.1.7 | KD-5 codified | §5.2 (Stage-Gated Activation Table) §7 | [ ] |
| 9.1.8 | KD-6 codified | §2.2 FR-TS-040 … 045, §3.5, §5.3 | [ ] |
| 9.1.9 | KD-7 codified | §3.4.2 §3.4.3 | [ ] |
| 9.1.10 | KD-8 codified | §3.3 §3.3.1 §3.3.5 | [ ] |
| 9.1.11 | KD-9 codified | §3.6 §5.5 | [ ] |
| 9.1.12 | KD-10 codified | §3.3.4 §3.8 §4.2 | [ ] |
| 9.1.13 | Boundary statements with #16 §7 (KD-2) and #18 §4 / §7 (KD-3) explicit | §1.3 §1.4 §3.2 §5.7 §6.2 §6.6 | [ ] |
| 9.1.14 | All appendices A … F present | `docs/specs/testing-strategy/appendices.md` | [ ] |

## 9.2 Quality Checklist

| # | Claim | Evidence | Status |
|---|-------|----------|--------|
| 9.2.1 | Cite-not-redefine rule audited (no #16 / #18 / #20 restatements) | Manual review of `section-3.md` and `section-6.md`; grep for restated tier definitions or performance numbers | [ ] |
| 9.2.2 | Every FR row resolves to a §5.x verification mechanism | §5.6 FR-to-verification table covers FR-TS-001 … 085 | [ ] |
| 9.2.3 | Every approval-checklist row in *this* checklist cites either a file path or a check name (KD-6 self-application) | This table; manual auditor walk per §5.3 | [ ] |
| 9.2.4 | All cross-references (XC-/FM-/EC-/ERR-) resolve | `grep -rn "XC-\|FM-\|EC-\|ERR-" docs/specs/testing-strategy/`; manual resolution | [ ] |
| 9.2.5 | Per-spec §5 schema (Appendix C) present and complete | `docs/specs/testing-strategy/appendices.md` Appendix C | [ ] |
| 9.2.6 | All `TBD-NORMATIVE`-tagged citations of #16 (KD-2) and #18 (KD-3) enumerated; outstanding tags listed for the reviewer | `grep -n "TBD-NORMATIVE" docs/specs/testing-strategy/*.md` output captured in PR description | [ ] |
| 9.2.7 | `[GT]` governance numbers in §3.10 each match the literal value in their cited section | Manual auditor walk: §3.10 table vs §3.1.2, §3.6.2, §3.7.3, §3.7.4 | [ ] |
| 9.2.8 | **Appendix D survey is NOT a #19-approval gate (M3).** The survey of #1–#8 §5 sections is a Stage 0+1 deliverable (§7.2); for #19's own approval the requirement is only that Appendix D *exists with the schema and an empty / partial table*. | `docs/specs/testing-strategy/appendices.md` Appendix D headers present | [ ] |
| 9.2.9 | Naming convention §3.1.4 covers every taxonomy layer | §3.1.4 entries match §3.1.1 layer list | [ ] |
| 9.2.10 | Interface declarations in §4.4 conform to CLAUDE.md "Interface Design Principle" (`IFlakeReporter` deferred) | §4.4 explicit `IFlakeReporter` non-declaration paragraph; §7.2 deferral row | [ ] |

## 9.3 Review Checklist

| # | Claim | Evidence | Status |
|---|-------|----------|--------|
| 9.3.1 | Open issues logged in `CLAUDE.md` "OPEN ISSUES" if any (e.g., #19 IN REVIEW → #16 Tier 2 APPROVED → #19 APPROVED sequencing) | `CLAUDE.md` OPEN ISSUES section updated | [ ] |
| 9.3.2 | Lead-developer sign-off captured | PR approval signature | [ ] |
| 9.3.3 | `spec-error-log.md` updated with any cross-spec drift discovered during drafting | `docs/tracking/spec-error-log.md` | [ ] |
| 9.3.4 | `SPEC_INDEX.md` status updated atomically with sign-off | `docs/specs/SPEC_INDEX.md` row 19 | [ ] |
| 9.3.5 | `file-manifest.md` updated for every section file in this folder | `docs/tracking/file-manifest.md` | [ ] |
| 9.3.6 | **Precondition for `APPROVED` status:** Spec #18 has at least an outline-level draft confirming the cited section numbers (§4 / §7) | `docs/specs/performance-optimization/outline.md` exists with §4 and §7 headers | [ ] |
| 9.3.7 | **Precondition for `APPROVED` status:** Spec #16 has reached Tier 2 `APPROVED` per KD-2 sequencing constraint | `docs/specs/SPEC_INDEX.md` row 16 status `APPROVED` | [ ] |
| 9.3.8 | All `TBD-NORMATIVE` tags resolved before `APPROVED` (Stage 0+1 gate) | `grep -n "TBD-NORMATIVE" docs/specs/testing-strategy/*.md` returns empty | [ ] |

## 9.4 Decision

- **Status:** `IN REVIEW` (initial draft, May 12, 2026).
- **Approval evidence:** file paths to programmatically-verifiable
  sources (KD-6 self-application — every row of §9.1 / §9.2 / §9.3
  above must comply).
- **Evidence-artifact convention for `[GT]` governance numbers.** Per
  §3.10, each governance number's evidence is the section-file
  citation that publishes the number. Checklist rows pointing at
  `[GT]` numbers (9.2.7) MUST cite the section-file path verbatim;
  the §5.3 auditor confirms the literal number is present at that
  path.

**Status transitions:**

| From | To | Trigger |
|------|-----|---------|
| `NOT STARTED` | `IN REVIEW` | Initial draft committed (May 12, 2026) |
| `IN REVIEW` | `APPROVED` | All §9.1 / §9.2 / §9.3 rows resolved AND §9.3.6 / §9.3.7 / §9.3.8 preconditions met (KD-2 sequencing) |
| `APPROVED` | `SUSPENDED` | Any §9.2.6 `TBD-NORMATIVE` tag re-introduced due to upstream churn |
