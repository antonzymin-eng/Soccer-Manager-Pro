# Testing Strategy & Framework Specification #19 — Section 9: Approval Checklist

**Created:** May 12, 2026
**Last Updated:** September 3, 2026 (A3.2b supporting-surface re-walk; v1.0 APPROVED May 15, 2026 — lead-developer sign-off granted)
**Version:** 1.0.3
**Status:** AMENDMENT DRAFT (A3.2b; May 15, 2026 approved baseline remains in force)
**Amendment plan:** `docs/planning/project-architecture-governance-integration-plan.md` v0.37, §7; A3.2b
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
| 9.1.1 | All required sections present (§1 … §9, appendices, slot reconciliations in §5 / §6) | `docs/specs/testing-strategy/section-1.md` … `section-9-approval-checklist.md`, `appendices.md` | [x] |
| 9.1.2 | All FR-TS-### present in §2.2 with conformance level and activation stage | **Approved baseline (May 15):** FR-TS-001 … 085. **A3.2b draft re-walk (September 3):** §2.2 contains 97 unique contiguous rows FR-TS-001 … 097, each with level and activation stage. This is draft evidence only; A3.4 must re-run before reapproval. | [x] |
| 9.1.3 | KD-1 codified | `docs/specs/testing-strategy/section-1.md` §1.3 KD-1 row + cross-section adoption | [x] |
| 9.1.4 | KD-2 codified | §3.2 §5.7 §6.2 §1.4 status caveat | [x] |
| 9.1.5 | KD-3 codified | §6.2 §6.6 §1.4 status caveat | [x] |
| 9.1.6 | KD-4 codified | §3.5.3 §3.5.4 §5.4 | [x] |
| 9.1.7 | KD-5 codified | §5.2 (Stage-Gated Activation Table) §7 | [x] |
| 9.1.8 | KD-6 codified | §2.2 FR-TS-040 … 045, §3.5, §5.3 | [x] |
| 9.1.9 | KD-7 codified | §3.4.2 §3.4.3 | [x] |
| 9.1.10 | KD-8 codified | §3.3 §3.3.1 §3.3.5 | [x] |
| 9.1.11 | KD-9 codified | §3.6 §5.5 | [x] |
| 9.1.12 | KD-10 codified | §3.3.4 §3.8 §4.2 | [x] |
| 9.1.13 | Boundary statements with #16 §5 (KD-2) and #18 §4 / §7 (KD-3) explicit | §1.3 §1.4 §3.2 §5.7 §6.2 §6.6 | [x] |
| 9.1.14 | All appendices A … G present | `docs/specs/testing-strategy/appendices.md` | [x] |
| 9.1.15 | Architecture proof/evidence ownership remains split correctly: Governance decision layer, #20 integration/activation declarations, #19 proof/execution/gate mechanics | §1.1/§1.2/§1.4; §3.11; §6.2; Appendix G | [x] |

## 9.2 Quality Checklist

| # | Claim | Evidence | Status |
|---|-------|----------|--------|
| 9.2.1 | Cite-not-redefine rule audited (no #16 / #18 / #20 restatements) | Manual review of `section-3.md` and `section-6.md`; grep for restated tier definitions or performance numbers | [x] |
| 9.2.2 | Every FR row resolves to a §5.x verification mechanism | **Approved baseline (May 15):** §5.6 covered FR-TS-001 … 085. **A3.2b draft re-walk (September 3):** §5.6 now covers FR-TS-001 … 097, with explicit architecture-proof fixture obligations in §5.6.1. A3.4 must re-run before reapproval. | [x] |
| 9.2.3 | Every approval-checklist row in *this* checklist cites either a file path or a check name (KD-6 self-application) | This table; manual auditor walk per §5.3 | [x] |
| 9.2.4 | All cross-references (XC-/FM-/EC-/ERR-) resolve | `grep -rn "XC-\|FM-\|EC-\|ERR-" docs/specs/testing-strategy/`; manual resolution | [x] |
| 9.2.5 | Per-spec §5 schema (Appendix C) present and complete | `docs/specs/testing-strategy/appendices.md` Appendix C | [x] |
| 9.2.6 | All `TBD-NORMATIVE`-tagged citations of #16 (KD-2) and #18 (KD-3) enumerated; outstanding tags listed for the reviewer | `grep -n "TBD-NORMATIVE" docs/specs/testing-strategy/*.md` output captured in PR description | [x] |
| 9.2.7 | `[GT]` governance numbers in §3.10 each match the literal value in their cited section | Manual auditor walk: §3.10 table vs §3.1.2, §3.6.2, §3.7.3, §3.7.4 | [x] |
| 9.2.8 | **Appendix D survey is NOT a #19-approval gate (M3).** The survey of #1–#8 §5 sections is a Stage 0+1 deliverable (§7.2); for #19's own approval the requirement is only that Appendix D *exists with the schema and an empty / partial table*. | `docs/specs/testing-strategy/appendices.md` Appendix D headers present | [x] |
| 9.2.9 | Naming convention §3.1.4 covers every taxonomy layer | §3.1.4 entries match §3.1.1 layer list | [x] |
| 9.2.10 | Interface declarations in §4.4 conform to CLAUDE.md "Interface Design Principle" (`IFlakeReporter` deferred) | §4.4 explicit `IFlakeReporter` non-declaration paragraph; §7.2 deferral row | [x] |
| 9.2.11 | FR-TS-086 … 097 proof semantics match the frozen A2 schema/reference semantics and required negative fixture classes are enumerated | §3.11; Appendix G; §5.6/§5.6.1 | [x] |
| 9.2.12 | A3.2b creates no enforcement claim: architecture/evidence gate remains topology/report-only until A8 and `SPEC_INDEX.md` is unchanged until A3.4 | §4.5.2; §5.2; §6.2; §9.4; tracking records | [x] |

## 9.3 Review Checklist

| # | Claim | Evidence | Status |
|---|-------|----------|--------|
| 9.3.1 | Open issues logged in `CLAUDE.md` "OPEN ISSUES" if any (e.g., #19 IN REVIEW → #16 Tier 2 APPROVED → #19 APPROVED sequencing) | `CLAUDE.md` OPEN ISSUES section updated | [x] |
| 9.3.2 | Lead-developer sign-off captured | **Approved-baseline evidence:** May 15, 2026 sign-off remains valid for the operative baseline. **A3 amendment:** fresh sign-off is pending A3.4; none is claimed by A3.2b. | [x] |
| 9.3.3 | `spec-error-log.md` updated with any cross-spec drift discovered during drafting | `docs/tracking/spec-error-log.md` | [x] |
| 9.3.4 | `SPEC_INDEX.md` status updated atomically with sign-off | **Approved-baseline evidence:** May 15 approval was synchronized. **A3 amendment:** intentionally unchanged in A3.2b; A3.4 owns atomic reapproval/status synchronization. | [x] |
| 9.3.5 | `file-manifest.md` updated for every section file in this folder | `docs/tracking/file-manifest.md` | [x] |
| 9.3.6 | **Precondition for `APPROVED` status:** Spec #18 has at least an outline-level draft confirming the cited section numbers (§4 / §7) | `docs/specs/performance-optimization/outline.md` exists with §4 and §7 headers | [x] |
| 9.3.7 | **Precondition for `APPROVED` status:** Spec #16 has reached Tier 2 `APPROVED` per KD-2 sequencing constraint | `docs/specs/SPEC_INDEX.md` row 16 status `APPROVED` | [x] |
| 9.3.8 | All `TBD-NORMATIVE` citation qualifiers resolved before `APPROVED` (Stage 0+1 gate) | `grep -n "\` *\[TBD-NORMATIVE\] *\`" docs/specs/testing-strategy/section-*.md docs/specs/testing-strategy/appendices.md` returns empty (live citation form swept in v1.0.1 May 15, 2026). Remaining grep hits on the bare token are sweep history, version-history entries, glossary definitions, §2.3 self-applied failure-mode descriptions, and the §9 grep instructions themselves — all of which are meta-references the auditor expects to see. | [x] |

## 9.4 Decision

- **Current A3 amendment state:** **AMENDMENT DRAFT.** The May 15, 2026 approved baseline remains operative. A3.2b performs a supporting-surface/evidence re-walk only; it does not grant fresh approval, change `SPEC_INDEX.md`, create a required status, or activate the architecture/evidence gate. A3.4 owns coordinated reapproval and status synchronization; A8 owns enforcement activation.
- **Approved baseline status:** **`APPROVED`** (May 15, 2026). All §9.1 / §9.2 / §9.3 rows resolved; §9.3.6 cleared by #18 outline-detailed v1.1 + section files v0.3 (IN REVIEW); §9.3.7 cleared by #16 Tier 2 `APPROVED` May 14, 2026; §9.3.8 cleared by the v1.0.1 `[TBD-NORMATIVE]` sweep. Lead-developer sign-off granted (solo project, per CLAUDE.md "Solo Development"). `SPEC_INDEX.md` row 19 updated atomically. Downstream effect: clears Spec #18's KD-2 gate for its own `IN REVIEW → APPROVED` advancement.
- **Historical status:** `IN REVIEW` (initial draft, May 12, 2026).
- **Approval evidence:** file paths to programmatically-verifiable
  sources (KD-6 self-application — every row of §9.1 / §9.2 / §9.3
  above must comply).
- **Evidence-artifact convention for `[GT]` governance numbers.** Per
  §3.10, each governance number's evidence is the section-file
  citation that publishes the number. Checklist rows pointing at
  `[GT]` numbers (9.2.7) MUST cite the section-file path verbatim;
  the §5.3 auditor confirms the literal number is present at that
  path.

**Status transitions:** (failure-mode definitions in §2.3)

| From | To | Trigger |
|------|-----|---------|
| `NOT STARTED` | `IN REVIEW` | Initial draft committed (May 12, 2026); per CLAUDE.md status definition this is "draft complete, awaiting lead-developer sign-off." Author-driven flip; the §9.1 / §9.2 / §9.3 rows have NOT been walked at the time of the flip. See `section-1.md §1.5` v0.1 entry. |
| `IN REVIEW` | `APPROVED` | Cleared May 15, 2026: all §9.1 / §9.2 / §9.3 rows resolved AND §9.3.6 / §9.3.7 / §9.3.8 preconditions met. |
| `APPROVED` | `SUSPENDED` | Per §2.3 "TBD-NORMATIVE re-introduction" failure mode: any §9.2.6 citation re-tagged `[TBD-NORMATIVE]` due to upstream churn |

## 9.5 Version History

- v1.0.3 (2026-09-03): **A3.2b supporting-surface re-walk.** §9 metadata distinguishes the amendment draft from the May 15 approved baseline; 9.1.2/9.2.2 record current 001…097 / §5.6 draft evidence; Appendix G and architecture-boundary rows are included; fresh sign-off and `SPEC_INDEX.md` synchronization remain explicitly pending A3.4; no enforcement activated.
- v1.0.2 (2026-09-02): A3.2a annotation only. Rows 9.1.2 / 9.2.2 evidence marked invalid for the A3 amendment draft; no row re-walked, approved status unchanged.
- v1.0 (2026-05-15): **APPROVED.** §9.1 / §9.2 / §9.3 rows all ticked; §9.3.6 / §9.3.7 cleared by #18 IN REVIEW v0.3 + #16 Tier 2 APPROVED (May 14, 2026); §9.3.8 cleared by v1.0.1 `[TBD-NORMATIVE]` sweep across §1 / §3 / §4 / §5 / §6 / §8 / appendices. Lead-developer sign-off granted. SPEC_INDEX.md row 19 IN REVIEW → APPROVED.
- v0.2 (2026-05-12): Initial self-applied KD-6 checklist with #16 / #18 / #20 boundary preconditions enumerated.
- v0.1 (2026-05-12): Stub from outline.
