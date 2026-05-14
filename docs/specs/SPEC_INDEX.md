# SPEC_INDEX.md — Canonical Specification Registry

> **Created:** March 26, 2026, 11:00 PM PST
> **Last Updated:** May 14, 2026 (later same day — #16 Tier 1 sign-off; status IN PROGRESS → IN REVIEW)
> **Purpose:** Single source of truth for spec numbers, folder names, and approval status. Every cross-reference in every spec must match the numbers in this file.

---

## HOW TO USE THIS FILE

- Before writing ANY spec cross-reference, verify the number here.
- Before creating a new spec folder, add the entry here first.
- Approval status here overrides any status stated in individual spec files.

---

## SPECIFICATION REGISTRY

| # | Specification | Folder | Priority | Status | Approved |
|---|---------------|--------|----------|--------|----------|
| 1 | Ball Physics | `ball-physics/` | 1 | APPROVED | Feb 8, 2026 |
| 2 | Agent Movement | `agent-movement/` | 1 | APPROVED | Apr 27, 2026 |
| 3 | Collision System | `collision-system/` | 1 | APPROVED | Feb 19, 2026 |
| 4 | First Touch Mechanics | `first-touch/` | 1 | APPROVED | Feb 22, 2026 |
| 5 | Pass Mechanics | `pass-mechanics/` | 1 | APPROVED | May 6, 2026 |
| 6 | Shot Mechanics | `shot-mechanics/` | 2 | APPROVED | Apr 27, 2026 |
| 7 | Perception System | `perception-system/` | 2 | APPROVED | Apr 22, 2026 |
| 8 | Decision Tree | `decision-tree/` | 2 | APPROVED | Apr 27, 2026 |
| 9 | Fixed64 Math Library | `fixed64-math/` | 2 | IN REVIEW | — |
| 10 | Heading Mechanics | `heading-mechanics/` | 3 | NOT STARTED | — |
| 11 | Goalkeeper Mechanics | `goalkeeper-mechanics/` | 3 | NOT STARTED | — |
| 12 | Positioning AI | `positioning-ai/` | 3 | NOT STARTED | — |
| 13 | Pressing AI | `pressing-ai/` | 4 | NOT STARTED | — |
| 14 | Defensive AI | `defensive-ai/` | 4 | NOT STARTED | — |
| 15 | Attacking AI | `attacking-ai/` | 4 | NOT STARTED | — |
| 16 | Deterministic Simulation | `deterministic-sim/` | 4 | IN REVIEW | — |
| 17 | Event System | `event-system/` | 5 | APPROVED | May 13, 2026 |
| 18 | Performance Optimization Strategy | `performance-optimization/` | 5 | IN REVIEW | — |
| 19 | Testing Strategy & Framework | `testing-strategy/` | 5 | IN REVIEW | — |
| 20 | Code Standards & Style Guide | `code-standards/` | 5 | APPROVED | May 11, 2026 |

---

## STATUS DEFINITIONS

| Status | Meaning |
|--------|---------|
| APPROVED | Lead developer signed off. Ready for implementation. |
| SUSPENDED | Was approved or in review, but audit findings require re-review before sign-off. |
| IN REVIEW | All sections written. Awaiting lead developer sign-off. |
| IN PROGRESS | Actively being written. Not all sections complete. |
| NOT STARTED | No work begun. |

---

## NOTES

- **Pass Mechanics (#5):** Originally approved Feb 22, 2026 → suspended March 25, 2026 (19 audit findings) → re-approved May 6, 2026 after all 19 findings fixed (per `fix-manifest-pass-mechanics.md`) plus §3.3–§3.9 follow-up findings F-A01 / F-A02 resolved (option-3 hybrid: spinBase/spinMax columns added to §3.1.4; WINDUP_FRAMES/FOLLOWTHROUGH_FRAMES localized in §3.8.10). Re-review packet at `pass-mechanics/re-review-packet.md`.
- **April 27, 2026 sign-off pass:** Agent Movement (#2), Shot Mechanics (#6), and Decision Tree (#8) all approved by lead developer. Decision Tree (#8) approved at "draft-level" quality gate; comprehensive audit candidate for follow-up before implementation.
- **May 2, 2026 status reconciliation:** Deterministic Simulation (#16) reclassified from `NOT STARTED` to `IN PROGRESS`. Section files (sections 1–9 + appendices) exist at v0.5–v0.7 but were authored ahead of formal status update. Counts now: 7 APPROVED, 1 SUSPENDED, 0 IN REVIEW, 1 IN PROGRESS, 11 NOT STARTED.
- **May 6, 2026 status reconciliation:** Fixed64 Math Library (#9) reclassified from `NOT STARTED` to `IN REVIEW` after Pass 2 adversarial critique fixes landed. All sections (1–9 + appendices) present at v0.2–v0.3; awaiting lead developer sign-off per `fixed64-math/section-9-approval-checklist.md`. Counts after that change: 7 APPROVED, 1 SUSPENDED, 1 IN REVIEW, 1 IN PROGRESS, 10 NOT STARTED.
- **May 6, 2026 (later same day) — Pass Mechanics (#5) re-approved.** F-A01 / F-A02 resolved via option-3 hybrid; lead developer sign-off granted. Counts now: **8 APPROVED, 0 SUSPENDED, 1 IN REVIEW, 1 IN PROGRESS, 10 NOT STARTED.** Stage 0 Priority 1–2 spec set is now complete (Pass Mechanics was the last suspended Priority 1–2 spec).
- **May 11, 2026 — Code Standards & Style Guide (#20) approved.** Section files + appendices authored May 7–8 from `outline-detailed.md` v1.3; adversarial review pass-1 applied May 11; lead-developer R-01..R-05 sign-off completed. Counts: **9 APPROVED, 0 SUSPENDED, 1 IN REVIEW, 1 IN PROGRESS, 9 NOT STARTED.**
- **May 12, 2026 — Testing Strategy & Framework (#19) reclassified `NOT STARTED` → `IN REVIEW`.** Initial section-file draft authored May 12 from `outline-detailed.md` v1.1; v0.2 self-critique sweep applied (3 H / 6 M / 8 L findings, all resolved). Per KD-2 sequencing in spec §9.3.6–§9.3.8, advancement to `APPROVED` is gated on (a) Spec #16 reaching Tier 2 `APPROVED`, (b) Spec #18 outline-level draft, and (c) all `TBD-NORMATIVE` tags resolved. Counts: **9 APPROVED, 0 SUSPENDED, 2 IN REVIEW, 1 IN PROGRESS, 8 NOT STARTED.**
- **May 13, 2026 — Performance Optimization Strategy (#18) reclassified `NOT STARTED` → `IN PROGRESS`.** Detailed outline `outline-detailed.md` v1.0 authored from `outline.md` v1.0 + May 6 adversarial review (6 H / 5 M / 2 L findings, all resolved); v1.1 issued same day to resolve `ERR-018-001` via **KD-3 inversion** (Spec #18 now owns the trace pipeline; Spec #16 retains canonical record format at §3.2.4.1, regression scenarios at §5, and determinism-of-emission veto over tick-pipeline trace points at §3.1). Establishes 11 cross-cutting design decisions: KD-2 per-spec §6 ratify-not-override, KD-3 (inverted) #16 boundary, KD-4 #19 boundary, KD-7 Tier-C-only degradation, KD-8 loop separation, KD-6 determinism-aware profiling, KD-10 hot-path enumeration via per-spec §6 union. Section files remain stubs. Unblocks Spec #19 KD-2 precondition (b). `TBD-NORMATIVE` tags throughout for citations of #16 (`IN PROGRESS`) and #19 (`IN REVIEW`). Counts: **10 APPROVED, 0 SUSPENDED, 2 IN REVIEW, 2 IN PROGRESS, 6 NOT STARTED.**
- **May 14, 2026 — Performance Optimization Strategy (#18) reclassified `IN PROGRESS` → `IN REVIEW`.** Section files authored May 13, 2026 (v0.1) from `outline-detailed.md` v1.1; PASS-1 adversarial review (4 H / 6 M / 13 L findings) resolved in v0.2 (May 14, 2026): ERR-018-002..011 all resolved. All 10 section files + appendices at v0.2. Outstanding `TBD-NORMATIVE` blockers listed at §9.4.1 — advancement to `APPROVED` requires #16 reaching Tier 2 `APPROVED` and #19 reaching `APPROVED`. Lead-developer sign-off pending. Counts: **10 APPROVED, 0 SUSPENDED, 3 IN REVIEW, 1 IN PROGRESS, 6 NOT STARTED.**
- **May 14, 2026 (later same day) — Performance Optimization Strategy (#18) v0.3 fix pass landed; status remains `IN REVIEW`.** PASS-2 adversarial review filed against v0.2 section files (2 H / 5 M / 8 L findings, 15 total); root cause for H-1 / H-2 / M-1 traced to PR #59 + PR #60 parallel-branch merge collision on the v0.2 fix pass (both branches authored independent fixes for the same PASS-1 findings and merged without de-duplication, leaving duplicate Appendix F.0 schemas, duplicate §3.10 constants rows, and duplicate v0.2 version-history rows across seven section files). v0.3 fix pass applied same day: ERR-018-012..018 all resolved; Appendix F.0 consolidated to single canonical 13-field schema with merged anchor rows; §3.10 duplicates removed; version-history rows consolidated; `[HotPathAllocExempt]` C# signature deferred to Stage 0+1 per Interface Design Principle; FR-PO-019 split into FR-PO-019 (MAY) + FR-PO-019a (MUST), FR count now 82; §3.5.2 example rewritten to apply §3.9.1 ±20% promotion tolerance before +5% gate. All 10 section files + appendices at v0.3. Counts unchanged: **10 APPROVED, 0 SUSPENDED, 3 IN REVIEW, 1 IN PROGRESS, 6 NOT STARTED.**
- **May 14, 2026 (later same day) — Deterministic Simulation (#16) reclassified `IN PROGRESS` → `IN REVIEW`.** Tier 1 (Conditional Approval) sign-off granted by lead developer. Per §9 v1.1 two-tier approval model, this advances the self-contained spec content (§1–§7, §9.1, §9.2, §9.5 #1–#3, §9.5 #5) to `IN REVIEW`; Tier 2 (Final Approval / `APPROVED`) remains gated on the §9.4.2 items reviewed below. Upstream-spec gate for Tier 2 is now cleared: #9 IN REVIEW (May 6), #17 APPROVED (May 13, beyond gate), #18 IN REVIEW (May 14), #19 IN REVIEW (May 12). Outstanding Tier 2 items: (i) §9.5 #4(c) `serialize-canonical-corpus.md` golden-vector file authoring; (ii) §8.3.1 re-audit of all four upstream rows and removal of `[TBD-NORMATIVE: pending #N]` suffix; (iii) §9.3 QA-automation + platform-certification sign-offs. **Former item (iv) (§9.5 #4(a)/(b) CI-runnability chicken-and-egg) RESOLVED in §9 v1.3 same day** — criterion split into spec-level (gates #16 Tier 2; hand-verifiable today) and implementation-level (folds into §5.5 `FR-DS-009-GATE`; NOT a #16 blocker). Counts: **10 APPROVED, 0 SUSPENDED, 4 IN REVIEW, 0 IN PROGRESS, 6 NOT STARTED.**
- **May 13, 2026 — Event System (#17) APPROVED.** Section files authored May 13, 2026 from `outline-detailed.md` v1.1; section-files PASS 1 adversarial critique (20 findings) and PASS 2 adversarial critique (2 H / 6 M / 7 L findings) both resolved same day. Lead-developer sign-off granted May 13, 2026. All 10 section files + appendices at v0.3. ERR-017-001 (`DOMAIN_TAG_EVENT_LEDGER` allocation in #16 §3.4) remains open pending #16 approval; `[CROSS-PENDING]` tag in #17 §3.10 / §3.4.2 resolves atomically with #16. Counts: **10 APPROVED, 0 SUSPENDED, 2 IN REVIEW, 1 IN PROGRESS, 7 NOT STARTED.**
- **Specs were renumbered** during early development. Original plan had different ordering. Many early-written files contain stale spec numbers from the old scheme. The numbers in this file are canonical. See FORMER NUMBERING table below.

---

## FORMER NUMBERING (for reference when fixing stale references)

These are numbers that appear in older spec files and are WRONG:

| Old Reference | Correct # | Spec Name |
|---------------|-----------|-----------|
| First Touch = #11 | **#4** | First Touch Mechanics |
| Pass Mechanics = #4 | **#5** | Pass Mechanics |
| Shot Mechanics = #5 | **#6** | Shot Mechanics |
| Perception System = #6 | **#7** | Perception System |
| Decision Tree = #7 | **#8** | Decision Tree |
| Fixed64 Math = #8 | **#9** | Fixed64 Math Library |
| Heading Mechanics = #9 | **#10** | Heading Mechanics |
| Goalkeeper = #10 | **#11** | Goalkeeper Mechanics |