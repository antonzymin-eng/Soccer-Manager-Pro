# Event System Specification #17 — Section 9: Approval Checklist

**Created:** May 13, 2026
**Last Updated:** May 13, 2026
**Version:** 0.1 (initial section-file draft from `outline-detailed.md` v1.1)
**Status:** DRAFT

> Every row in this checklist cites either a file path or a check
> name (Spec #19 KD-6 self-application — every approval-checklist
> row resolves to a programmatically-verifiable source). "Verify"
> means: the auditor opens the cited path and confirms the literal
> claim.

---

## 9.1 Content Checklist

| # | Check | Evidence path / artifact |
|---|-------|--------------------------|
| C1 | All required template sections present (§1 Purpose & Scope, §2 FRs & data structures, §3 mechanics, §4 architecture, §5 test plan, §6 performance, §7 future extensions, §8 references, §9 approval, appendices). | `docs/specs/event-system/{section-1.md, section-2.md, section-3.md, section-4.md, section-5.md, section-6.md, section-7.md, section-8.md, section-9-approval-checklist.md, appendices.md}` |
| C2 | Template-slot reconciliation acknowledged: §6 is the Performance slot (not error handling), §8 is the References slot (not multiplayer). | `section-6.md` header note + `section-8.md` header note. |
| C3 | All FR-EVT-001 … FR-EVT-082 present in §2.2 with conformance level, source citation, verification pointer, and activation stage columns. | `section-2.md` §2.2.1 — 82 rows. |
| C4 | KD-1 … KD-12 each codified in at least one §3 / §4 / §5 / §6 subsection. | `section-1.md` §1.3 KD index table → resolved sections. |
| C5 | Boundary statements with #16 §3 (KD-2) explicit. | `section-1.md` §1.4 upstream; `section-3.md` §3.1, §3.2, §3.4; `section-4.md` §4.4. |
| C6 | Boundary statements with #19 §3 testing governance (KD-11 / §5.5) explicit. | `section-5.md` §5.1, §5.5, §5.6. |
| C7 | Event registry (Appendix A) populated with at least the 11 initial seed rows. | `appendices.md` Appendix A. |
| C8 | Tier vocabulary cited from #16 §1.3.1 `TBD-NORMATIVE`; not redefined. | `section-1.md` §1.2 out-of-scope row; `section-3.md` §3.1.3. |
| C9 | All section files (§1 … §9 + appendices) include a Version History sub-section. | Every `section-*.md`'s `§X.Y Version History` plus `appendices.md` `Appendices Version History`. |
| C10 | Glossary entries scoped to Spec #17-local terms only. | `section-1.md` §1.5; `appendices.md` Appendix D. |

## 9.2 Quality Checklist

| # | Check | Evidence path / artifact |
|---|-------|--------------------------|
| Q1 | Cite-not-redefine rule audited (no #16 / #18 / #19 / #20 restatements; CLAUDE.md invariants are cited not paraphrased). | `section-3.md` §3.1.3 (tier); §3.2.5 (failure path); §3.3.2 (phase WriteSet); `section-4.md` §4.2 (interfaces); §4.4 (phase integration). |
| Q2 | Every FR-EVT-### row resolves to a §5.x verification mechanism. | `section-5.md` §5.4 traceability table (full 82-row + FR-EVT-046a/046b/009a table at §5.4.1 … §5.4.10; Stage 0 degenerate rows visible per row's `Activation` column = `Stage 0`). |
| Q3 | All cross-references (FM-/EC-/ERR-) resolve. | FM-017-001 (`section-3.md` §3.4.2, §3.4.3); FM-017-002 (`section-3.md` §3.2.4, §3.4.3); EC-017-001 … 006 (`section-3.md` §3.8, `appendices.md` Appendix E); ERR-017-001 (`docs/tracking/spec-error-log.md`). |
| Q4 | All `TBD-NORMATIVE` citations of #16 (KD-2) and #19 (KD-2 §19 status caveat) enumerated for the reviewer. | `section-8.md` §8.1.2 (status column = "Yes"); §9.3 row R6. |
| Q5 | Tier classification consistent across §2.4, §3.1.3, and Appendix A (every event has exactly one tier; every row in Appendix A has a tier column populated). | `section-2.md` §2.4.2 seed table; `appendices.md` Appendix A registry. |
| Q6 | Ordinal uniqueness verified across Appendix A registry rows. | Registry-validator output (Stage 0 manual grep over `appendices.md` Appendix A `Ordinal` column). |
| Q7 | Error-code numeric pins (`0x17NN`) verified against #16's `0x16NN` block for non-collision. | `section-3.md` §3.10; `section-2.md` §2.5. Verification: `grep -E '0x16[0-9A-Fa-f]{2}' docs/specs/deterministic-sim/` and confirm no overlap with `0x1701`–`0x1704`. |
| Q8 | Spec #17 frame-budget claim (≤ 0.5 ms / frame ≈ 3%) sits within #16 §6.2 `TBD-NORMATIVE` envelope (Resolve + Events = 18%). | `section-6.md` §6.4. Tag-resolution gated on #16 approval. |
| Q9 | KD-9 versioning rules consistently apply across §2.4.3 (rule statements), §3.7 (mechanics), and Appendix C (recipes). | `section-2.md` §2.4.3; `section-3.md` §3.7; `appendices.md` Appendix C. |
| Q10 | All `[GT]` constants in §3.10 carry rationale; the one `[CROSS]` constant (`DOMAIN_TAG_EVENT_LEDGER = 0x15`) cites its #16 §3.4 v1.0.1 allocation. | `section-3.md` §3.10 + `section-6.md` §6.3 + `section-8.md` §8.4. Promotion `[CROSS-PENDING]` → `[CROSS]` completed in #17 §1.0.1 patch (May 15, 2026) per ERR-017-001 RESOLVED. |
| Q11 | Every `#16 §x.x.x` and `#19 §x.x` citation in this spec carries the `TBD-NORMATIVE` qualifier. | grep `'#16 §'` and `'#19 §'` over `section-*.md` and confirm `TBD-NORMATIVE` co-located. |
| Q12 | No phantom interfaces (CLAUDE.md "Interface Design Principle"). `IEventA` / `IEventB` / `IEventC` both-sides-specified justification recorded. | `section-3.md` §3.1.3 (both-sides argument); `section-4.md` §4.2.4 (intentionally not declared). |
| Q13 | No magic numbers in formula text — every numeric value in §3 / §6 cites §3.10 catalogue or is derived. | grep `[0-9]+` over §3 / §6; expected hits resolve to `EVENT_QUEUE_CAPACITY`, `MAX_EVENT_DISPATCH_DEPTH`, `COSMETIC_PER_TICK_PUBLICATION_BUDGET`, `0x17NN` codes. |
| Q14 | No `System.Random`, no `DateTime.Now`, no wall-clock dependencies in any formula or pseudocode (CLAUDE.md "When Writing Code"). | grep over `section-3.md`, `section-4.md`, `appendices.md`. |

## 9.3 Review Checklist

| # | Check | Evidence path / artifact |
|---|-------|--------------------------|
| R1 | Open issues logged in `CLAUDE.md` OPEN ISSUES section, if any. | `CLAUDE.md` OPEN ISSUES — add #17 IN REVIEW entry at IN REVIEW commit. |
| R2 | Lead-developer sign-off captured. | At IN REVIEW commit, record in `section-9-approval-checklist.md` Decision block (§9.4). |
| R3 | `spec-error-log.md` updated with any cross-spec drift discovered during drafting. ERR-017-001 (`DOMAIN_TAG_EVENT_LEDGER` allocation back-prop into #16 §3.4) filed May 12, 2026; RESOLVED May 14, 2026 at #16 (value `0x15` allocated in #16 §3.4 v1.0.1); #17-side promotion completed in §1.0.1 patch May 15, 2026. | `docs/tracking/spec-error-log.md` ERR-017-001 row. |
| R4 | `SPEC_INDEX.md` row 17 updated atomically with sign-off (`IN PROGRESS` → `IN REVIEW`; later `IN REVIEW` → `APPROVED` after #16 Tier 2 approval per KD-2). | `docs/specs/SPEC_INDEX.md` row 17 status column. |
| R5 | `docs/tracking/file-manifest.md` updated with section-file entries at IN REVIEW commit. | `docs/tracking/file-manifest.md` + `section-4.md` §4.5 stub. |
| R6 | KD-2 sequencing constraint satisfied: (1) #17 reaches `IN REVIEW` with `TBD-NORMATIVE` citations to #16; (2) #16 reaches Tier 2 `APPROVED`; (3) #17's `TBD-NORMATIVE` tags resolved and #17 advances to `APPROVED`. | `SPEC_INDEX.md` rows 16 + 17 status sequence; `section-1.md` §1.4 sequencing block. |
| R7 | Appendix A registry seeds (11 rows) reviewed against producer-spec owners. | `appendices.md` Appendix A. Cross-check each `Owning spec` column against `SPEC_INDEX.md` and the cited spec's §2.4 surface. |
| R8 | KD-12 stage-gated activation table reviewed: Stage 0 rows are enforceable now (Appendix A registry, typed-contract rules, versioning rules); all other rows clearly marked for Stage 0+1. | `section-5.md` §5.2. |
| R9 | PASS 1 (May 6) and PASS 2 (May 12) adversarial-review findings traceability map remains accurate after section-file authoring. | `outline.md` adversarial-review block; `outline-detailed.md` v1.1 FINDINGS RESOLUTION MAP + PASS 2 ADVERSARIAL REVIEW block. |

## 9.4 Decision

```
Status: DRAFT (Stage 0; section files authored from outline-detailed.md v1.1).
Next gate: IN REVIEW (lead-developer review of all section files).
Final gate: APPROVED (gated on KD-2 sequencing — #16 Tier 2 APPROVED;
            TBD-NORMATIVE tag resolution per Q11 / R6).
```

**Approval evidence convention.** Every row in §9.1 / §9.2 / §9.3
cites a file path or a check name; the auditor opens that path and
confirms the literal claim. Evidence-artifact convention for `[GT]`
governance numbers parallels Spec #19 §9.4 L5: the section-file
citation IS the evidence, and the auditor confirms the literal
number is present at the cited path.

When the gate transitions:

1. **DRAFT → IN REVIEW.** All §9.1 / §9.2 rows pass; all §9.3 rows
   except R2 / R6 / Q4 (#16-pending) pass. Status updated in
   `SPEC_INDEX.md`. PASS 3 adversarial review window opens.
2. **IN REVIEW → APPROVED.** PASS 3 review resolved; #16 Tier 2
   APPROVED; Q4 / Q11 tag-resolution row swept; R6 sequencing
   satisfied; R2 sign-off captured. ERR-017-001 closed.

## 9.5 Version History

| Version | Date         | Author      | Notes                                                                 |
|---------|--------------|-------------|-----------------------------------------------------------------------|
| 0.1     | May 13, 2026 | Claude Code | Initial section-file draft from `outline-detailed.md` v1.1. 10 content + 14 quality + 9 review rows. Decision block records gate path. |
| 0.2     | May 13, 2026 | Claude Code | PASS 1 critique resolution. §9.1 C9 coverage list now includes §1 / §2 / §9 / appendices (L8). §9.2 Q2 evidence row points at the fully-authored §5.4.1 … §5.4.10 traceability table (H3). |
| 1.0.1   | May 15, 2026 | Claude Code | Patch revision (no behavioral change). §9.2 Q10 evidence updated to reflect `DOMAIN_TAG_EVENT_LEDGER = 0x15` / `[CROSS]`; §9.3 R3 evidence updated to mark ERR-017-001 RESOLVED. Reflects #16 §3.4 v1.0.1 allocation (May 14, 2026). |
