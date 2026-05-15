# File Manifest (Post-Migration Baseline)

**Created:** April 30, 2026  
**Last Updated:** May 14, 2026 (later same day — Deterministic Simulation #16 Tier 2 APPROVED; ERR-017-001 closed atomically via `DOMAIN_TAG_EVENT_LEDGER = 0x15` in #16 §3.4 v1.0.1; Performance Optimization #18 section files v0.3 — PASS-2 adversarial review `ERR-018-012` … `ERR-018-018` resolved; status IN REVIEW)  
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
| 16 | `docs/specs/deterministic-sim/` | APPROVED (May 14, 2026, later same day) — Tier 2 Final Approval. §9 v1.7. All §9.4.2 gates cleared: §9.5 #4(a)/(b)/(c) spec-level sub-conditions SATISFIED (golden-vector files `hkdf-sha256-kat.md` v1.1, `siphash-2-4-kat.md` v1.1, `serialize-canonical-corpus.md` v1.0); §8.3.1 cross-spec re-audit COMPLETE (§8 v1.2, all four upstream rows promoted to `complete`); §9.3 sign-offs (lead-developer Tier 2, QA-automation, platform-certification) granted, platform-certification with explicit Stage-0 host-platform-pin caveat. ERR-017-001 closed atomically via `DOMAIN_TAG_EVENT_LEDGER = 0x15` allocation in §3.4 v1.0.1 (no `DETERMINISM_DIGEST_VERSION` bump). |
| 17 | `docs/specs/event-system/` | APPROVED (May 13, 2026) — 10 section files + appendices; section-files PASS 1 + PASS 2 adversarial review applied; lead-developer sign-off complete |
| 18 | `docs/specs/performance-optimization/` | IN REVIEW (May 14, 2026, later same day) — `outline.md` v1.0 + `outline-detailed.md` v1.1 (KD-3 inverted; ERR-018-001 resolved); section-1 through section-9-approval-checklist + appendices.md at v0.3 (PASS-1 adversarial review 23 findings resolved in v0.2 + PASS-2 adversarial review 15 findings resolved in v0.3; ERR-018-002..018 all closed). FR count 82 (FR-PO-019 split into 019 MAY + 019a MUST; FR-PO-058a emission constraints). Advancement to APPROVED gated on #19 reaching APPROVED per KD-2. |
| 19 | `docs/specs/testing-strategy/` | IN REVIEW (May 12, 2026) — initial draft authored from `outline-detailed.md` v1.1 |
| 20 | `docs/specs/code-standards/` | APPROVED (May 11, 2026) — 10 section files + appendices; adversarial review pass-1 applied; lead-developer R-01..R-05 sign-off complete |

**Notes:**
- Deterministic Simulation (#16) files: `outline.md`, `section-1.md` through `section-9-approval-checklist.md`, `appendices.md`, `critique-log.md` (consolidated review history; supersedes the former `adversarial-review.md` and `third-pass-fix-log.md`, both removed May 3, 2026).
- Fixed64 Math (#9) files include `adversarial-review.md` and `adversarial-critique-pass-2.md` alongside the standard section files.
- Code Standards (#20) outline-tier files: `outline.md` (high-level v1.0), `outline-mid.md` (mid-level v1.3), `outline-detailed.md` (detailed v1.3). Section files authored from the detailed outline May 7–8, 2026; adversarial review pass-1 applied May 11, 2026; lead-developer R-01..R-05 sign-off completed May 11, 2026. Current set: `section-1.md` v1.0.1, `section-2.md` v1.0.1, `section-3.md` v1.0.1, `section-4.md` v1.0, `section-5.md` v1.0.1, `section-6.md` v1.0.1, `section-7.md` v1.0.1, `section-8.md` v1.0, `section-9-approval-checklist.md` v1.1 (APPROVED), `appendices.md` v1.1. SPEC_INDEX.md line 40 reflects APPROVED status.
- Testing Strategy (#19) files: `outline.md` (high-level v1.0 + first adversarial review), `outline-detailed.md` (v1.1 — second adversarial review applied), section-1 through section-9-approval-checklist, and `appendices.md`. Initial section-file draft authored May 12, 2026 from `outline-detailed.md` v1.1; v0.2 self-critique sweep applied same day (3 H / 6 M / 8 L findings, all resolved); status `IN REVIEW`. v0.2 corrected #16 section-number citations against current `deterministic-sim/` text (§7 → §5 regression suite, §1.3.1 → §1.1.1 tier classification, §5 → §3.2.4.1 canonical schema, deleted §8 "trace channels" — no such section). Per KD-2 sequencing in the spec, advancement to `APPROVED` is gated on (a) Spec #16 reaching Tier 2 `APPROVED`, (b) Spec #18 having at least an outline-level draft, and (c) all `TBD-NORMATIVE` tags resolved.
- Performance Optimization Strategy (#18) files: `outline.md` (high-level v1.0 with embedded adversarial review of May 6, 2026), `outline-detailed.md` (v1.1 — May 13, 2026), `section-1.md` through `section-9-approval-checklist.md` + `appendices.md`, `pass-2-adversarial-review.md`. Section files authored May 13, 2026 (v0.1) from `outline-detailed.md` v1.1; PASS-1 adversarial review (4 H / 6 M / 13 L findings, 23 total) resolved in v0.2 (May 14, 2026). PASS-2 adversarial review (2 H / 5 M / 8 L findings, 15 total) filed May 14, 2026 against v0.2; resolved in v0.3 fix pass same day. ERR-018-002..018 all resolved. PASS-2 H-1 / H-2 / M-1 traced to PR #59 + PR #60 parallel-branch merge collision on the v0.2 fix pass. Status `IN REVIEW`; lead-developer sign-off pending. KD-3 inverted: #18 owns trace pipeline; #16 retains §3.2.4.1/§5/§3.1.2. `TBD-NORMATIVE` tags throughout for #16 (IN PROGRESS) and #19 (IN REVIEW) citations. FR count is 82 (FR-PO-001 … 080 + FR-PO-019a + FR-PO-058a — FR-PO-019a split from FR-PO-019 per ERR-018-017).
- Event System (#17) files: `outline.md` (high-level v1.0), `outline-detailed.md` (v1.1), `section-files-critique-pass-1.md`, `section-files-critique-pass-2.md`, `section-1.md` through `section-8.md`, `section-9-approval-checklist.md`, and `appendices.md`. All section files authored May 13, 2026 from `outline-detailed.md` v1.1. Section-files PASS 1 adversarial critique (3 H / 5 M / 12 L findings) resolved in v0.2; section-files PASS 2 adversarial critique (2 H / 6 M / 7 L findings) resolved in v0.3. All section files at v0.3; lead-developer sign-off granted May 13, 2026. ERR-017-001 RESOLVED at #16 May 14, 2026 (`DOMAIN_TAG_EVENT_LEDGER = 0x15` allocated in #16 §3.4 v1.0.1); #17-side `[CROSS-PENDING]` → `[CROSS]` promotion in §3.10 / §3.4.2 (and literal-value inlining in Appendix B) is the one residual mechanical step.

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
