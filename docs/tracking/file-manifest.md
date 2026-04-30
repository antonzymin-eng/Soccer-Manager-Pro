# File Manifest (Post-Migration Baseline)

**Created:** April 30, 2026  
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

The project currently tracks written specifications in the following folders:

1. `docs/specs/ball-physics/`
2. `docs/specs/agent-movement/`
3. `docs/specs/collision-system/`
4. `docs/specs/first-touch/`
5. `docs/specs/pass-mechanics/`
6. `docs/specs/shot-mechanics/`
7. `docs/specs/perception-system/`
8. `docs/specs/decision-tree/`

> Additional spec folders for #9–#20 will be added as writing progresses.

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
