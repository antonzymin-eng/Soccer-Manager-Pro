# SPEC_INDEX.md — Canonical Specification Registry

> **Created:** March 26, 2026, 11:00 PM PST
> **Last Updated:** May 6, 2026
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
| 16 | Deterministic Simulation | `deterministic-sim/` | 4 | IN PROGRESS | — |
| 17 | Event System | `event-system/` | 5 | NOT STARTED | — |
| 18 | Performance Optimization Strategy | `performance-optimization/` | 5 | NOT STARTED | — |
| 19 | Testing Strategy & Framework | `testing-strategy/` | 5 | NOT STARTED | — |
| 20 | Code Standards & Style Guide | `code-standards/` | 5 | NOT STARTED | — |

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