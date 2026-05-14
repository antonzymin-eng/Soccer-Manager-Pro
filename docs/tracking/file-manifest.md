# File Manifest (Post-Migration Baseline)

**Created:** April 30, 2026  
**Last Updated:** May 14, 2026 (Performance Optimization #18 section files v0.2 — PASS-1 adversarial review `ERR-018-002` … `ERR-018-011` resolved; status flipped `IN PROGRESS → IN REVIEW`)  
**Purpose:** Canonical inventory aligned with the current folder-based spec layout in `docs/specs/`.

---

## Scope and Authority

This manifest supersedes the legacy flat-file inventory that referenced historical filenames such as `*_v1_0.md`.

- **Canonical spec status source:** `docs/specs/SPEC_INDEX.md`
- **Canonical schedule source:** `docs/tracking/PROGRESS.md`
- **Canonical cross-spec issue source:** `docs/tracking/spec-error-log.md`

Use this file to track the **current folder structure**, not legacy per-version filenames.

---

## Tracking Documents

| File | Purpose |
|------|---------|
| `docs/tracking/PROGRESS.md` | Stage progress, milestones, and current status notes |
| `docs/tracking/spec-error-log.md` | Cross-spec error tracking (`ERR-*`) |
| `docs/tracking/spec-error-log-err012-addendum.md` | ERR-012 addendum details |
| `docs/tracking/fix-manifest-pass-mechanics.md` | Pass Mechanics audit/fix closure tracking |
| `docs/tracking/certification-platform.md` | Stage 0 host platform version pin (required before first Spec #16 certification run) |
| `docs/tracking/file-manifest.md` | This manifest |

---

## Planning Documents

| File |
|------|
| `docs/planning/master-development-plan.md` |
| `docs/planning/master-vol-1-physics-core.md` |
| `docs/planning/master-vol-2-human-systems.md` |
| `docs/planning/master-vol-3-club-operations.md` |
| `docs/planning/master-vol-4-tech-implementation.md` |
| `docs/planning/development-best-practices.md` |

---

## Current Specification Folders

All 20 spec folders now exist in `docs/specs/`. Status reflects authoritative classification in `SPEC_INDEX.md`. Folders marked NOT STARTED contain header-only scaffolding (outline + section-1…9 + appendices skeletons with no body content).

| # | Folder | Status |
|---|--------|--------|
| 1 | `docs/specs/ball-physics/` | APPROVED |
| 2 | `docs/specs/agent-movement/` | APPROVED |
| 3 | `docs/specs/collision-system/` | APPROVED |
| 4 | `docs/specs/first-touch/` | APPROVED |
| 5 | `docs/specs/pass-mechanics/` | APPROVED (re-approved May 6, 2026) |
| 6 | `docs/specs/shot-mechanics/` | APPROVED |
| 7 | `docs/specs/perception-system/` | APPROVED |
| 8 | `docs/specs/decision-tree/` | APPROVED (draft-level) |
| 9 | `docs/specs/fixed64-math/` | IN REVIEW (since May 6, 2026) |
| 10 | `docs/specs/heading-mechanics/` | NOT STARTED (scaffold only) |
| 11 | `docs/specs/goalkeeper-mechanics/` | NOT STARTED (scaffold only) |
| 12 | `docs/specs/positioning-ai/` | NOT STARTED (scaffold only) |
| 13 | `docs/specs/pressing-ai/` | NOT STARTED (scaffold only) |
| 14 | `docs/specs/defensive-ai/` | NOT STARTED (scaffold only) |
| 15 | `docs/specs/attacking-ai/` | NOT STARTED (scaffold only) |
| 16 | `docs/specs/deterministic-sim/` | IN PROGRESS (since May 2, 2026) |
| 17 | `docs/specs/event-system/` | APPROVED (May 13, 2026) — 10 section files + appendices; section-files PASS 1 + PASS 2 adversarial review applied; lead-developer sign-off complete |
| 18 | `docs/specs/performance-optimization/` | IN REVIEW (May 14, 2026) — `outline.md` v1.0 + `outline-detailed.md` v1.1; section files v0.2 (PASS-1 adversarial review `ERR-018-002` … `ERR-018-011` resolved May 14, 2026) |
| 19 | `docs/specs/testing-strategy/` | IN REVIEW (May 12, 2026) — initial draft authored from `outline-detailed.md` v1.1 |
| 20 | `docs/specs/code-standards/` | APPROVED (May 11, 2026) — 10 section files + appendices; adversarial review pass-1 applied; lead-developer R-01..R-05 sign-off complete |

**Notes:**
- Deterministic Simulation (#16) files: `outline.md`, `section-1.md` through `section-9-approval-checklist.md`, `appendices.md`, `critique-log.md` (consolidated review history; supersedes the former `adversarial-review.md` and `third-pass-fix-log.md`, both removed May 3, 2026).
- Fixed64 Math (#9) files include `adversarial-review.md` and `adversarial-critique-pass-2.md` alongside the standard section files.
- Code Standards (#20) outline-tier files: `outline.md` (high-level v1.0), `outline-mid.md` (mid-level v1.3), `outline-detailed.md` (detailed v1.3). Section files authored from the detailed outline May 7–8, 2026; adversarial review pass-1 applied May 11, 2026; lead-developer R-01..R-05 sign-off completed May 11, 2026. Current set: `section-1.md` v1.0.1, `section-2.md` v1.0.1, `section-3.md` v1.0.1, `section-4.md` v1.0, `section-5.md` v1.0.1, `section-6.md` v1.0.1, `section-7.md` v1.0.1, `section-8.md` v1.0, `section-9-approval-checklist.md` v1.1 (APPROVED), `appendices.md` v1.1. SPEC_INDEX.md line 40 reflects APPROVED status.
- Testing Strategy (#19) files: `outline.md` (high-level v1.0 + first adversarial review), `outline-detailed.md` (v1.1 — second adversarial review applied), section-1 through section-9-approval-checklist, and `appendices.md`. Initial section-file draft authored May 12, 2026 from `outline-detailed.md` v1.1; v0.2 self-critique sweep applied same day (3 H / 6 M / 8 L findings, all resolved); status `IN REVIEW`. v0.2 corrected #16 section-number citations against current `deterministic-sim/` text (§7 → §5 regression suite, §1.3.1 → §1.1.1 tier classification, §5 → §3.2.4.1 canonical schema, deleted §8 "trace channels" — no such section). Per KD-2 sequencing in the spec, advancement to `APPROVED` is gated on (a) Spec #16 reaching Tier 2 `APPROVED`, (b) Spec #18 having at least an outline-level draft, and (c) all `TBD-NORMATIVE` tags resolved.
- Performance Optimization Strategy (#18) files: `outline.md` (high-level v1.0 with embedded adversarial review of May 6, 2026), `outline-detailed.md` (v1.1 — May 13, 2026), and section files at v0.2 — `section-1.md` through `section-9-approval-checklist.md` + `appendices.md`. Detailed outline addresses all 13 findings (6 H / 5 M / 2 L) from the May 6 adversarial review and establishes 11 cross-cutting design decisions (KD-1…KD-11). v1.1 (same day as v1.0): **KD-3 inverted** — Spec #18 owns the trace pipeline (channels, verbosity tiers, sampling, instrumentation API, dashboards); Spec #16 retains authority over canonical record format (§3.2.4.1), regression scenarios (§5), and determinism-of-emission veto over tick-pipeline trace points (§3.1). All `TBD-NORMATIVE`-marked #16 section-number citations grep-verified against current `deterministic-sim/section-*.md`. New FR-PO-058a enforces emission constraints. ERR-018-001 (filed and resolved same day) is now closed at outline level. **Section files v0.1 → v0.2** (May 14, 2026): PASS-1 adversarial review filed `ERR-018-002` … `ERR-018-011` (4 H + 6 M); all 10 resolved (option-2 `[HotPathAllocExempt]` ownership relocated to #18 §3.7.5; §3.4.4 MAY → MUST + Stage 0 carve-out; §7.5 D9 re-anchored Stage 0+1 to match FR-PO-031 / §7.1; new Appendix F.0 channel-registry-schema; 0-byte allocation budget re-tagged `[FIXED]`; three #19 citations gain `TBD-NORMATIVE` and §9.4.1 blocker list extended; ±20% / N=100 / 1%-flake catalogued in §3.10 + §8.4; FR-PO-070 split Stage 0 manual / Stage 0+1 automated; `SPEC_INDEX.md` row 18 flipped `IN PROGRESS → IN REVIEW`). Status `IN REVIEW`. Lead-developer sign-off on `IN REVIEW → APPROVED` pending §9.4.1 `TBD-NORMATIVE` resolution (#16 Tier 2 `APPROVED` + #19 `APPROVED`).
- Event System (#17) files: `outline.md` (high-level v1.0), `outline-detailed.md` (v1.1), `section-files-critique-pass-1.md`, `section-files-critique-pass-2.md`, `section-1.md` through `section-8.md`, `section-9-approval-checklist.md`, and `appendices.md`. All section files authored May 13, 2026 from `outline-detailed.md` v1.1. Section-files PASS 1 adversarial critique (3 H / 5 M / 12 L findings) resolved in v0.2; section-files PASS 2 adversarial critique (2 H / 6 M / 7 L findings) resolved in v0.3. All section files at v0.3; lead-developer sign-off granted May 13, 2026. ERR-017-001 (`DOMAIN_TAG_EVENT_LEDGER` allocation in #16 §3.4) open pending #16 approval.

---

## Naming Convention (Current)

- Spec files are stored inside per-spec folders.
- Filenames do **not** carry version suffixes.
- Git history is the version record.

Examples:
- `section-1.md`
- `section-3-1-to-3-2.md`
- `section-9-approval-checklist.md`
- `appendix-a.md`
- `audit-report.md`

---

## Maintenance Rule

Update this manifest when:
- a new spec folder is added,
- a tracking/planning file is added, removed, or renamed,
- project-level documentation paths change.
