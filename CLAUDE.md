# CLAUDE.md — Tactical Director

> **Created:** March 26, 2026, 11:00 PM PST
> **Last Updated:** April 26, 2026
> **Purpose:** Authoritative rules for any AI agent (Claude Code, Claude chat, etc.) working on this project. Read this file completely before every task.

---

## PROJECT IDENTITY

**Tactical Director** is a football (soccer) simulation game targeting "Football Manager killer" ambitions. It follows a 10-year, 6-stage development plan. The project is solo-developed with AI assistance.

**Current phase:** Stage 0 — Physics Foundation. Writing 20 formal technical specifications. **No code exists yet. No code will be written until all 20 specs are approved.**

---

## REPO STRUCTURE

```
TacticalDirector/
├── CLAUDE.md                       ← You are here. Read first. Always.
├── docs/
│   ├── planning/                   ← Master volumes, dev plan, best practices
│   ├── specs/
│   │   ├── SPEC_INDEX.md           ← Canonical spec numbering and status
│   │   ├── ball-physics/           ← Spec #1
│   │   ├── agent-movement/         ← Spec #2
│   │   ├── collision-system/       ← Spec #3
│   │   ├── first-touch/            ← Spec #4
│   │   ├── pass-mechanics/         ← Spec #5
│   │   ├── shot-mechanics/         ← Spec #6
│   │   ├── perception-system/      ← Spec #7
│   │   ├── decision-tree/          ← Spec #8
│   │   └── ...                     ← Specs #9–#20 as written
│   └── tracking/                   ← PROGRESS.md, Spec_Error_Log, FIX_MANIFEST
└── src/                            ← Empty until all 20 specs approved
```

**Rules:**
- Each spec folder contains ONLY current-version files. No version suffixes in filenames. Git tracks history.
- `SPEC_INDEX.md` is the canonical source of truth for spec numbers, folder names, and approval status.
- `PROGRESS.md` is the canonical source of truth for schedule and milestone tracking.
- Never create files in `src/` during the specification phase.

**Deferred: `src/CLAUDE.md`** — Do NOT create until coding begins (after all 20 specs are approved). At that point it should cover: C# file naming conventions, constant catalogue file locations, Unity project structure, and build/test commands.

---

## CRITICAL DOMAIN CONVENTIONS

These conventions have caused bugs. Memorize them.

### Coordinate System

**Authoritative source:** Ball Physics Spec #1, §1.2 and Appendix C.

| Axis | Direction | Range | Notes |
|------|-----------|-------|-------|
| X | Goal-to-goal (pitch length) | 0–105m | |
| Y | Touchline-to-touchline (pitch width) | 0–68m | |
| Z | Height (vertical, up) | 0m = ground | Ball center rests at 0.11m |

**Origin:** Corner of pitch (0, 0, 0) — NOT pitch center.

### Fatigue Convention

`0.0 = fully rested`, `1.0 = fully fatigued`. Any inversion is a critical error. This has been found inverted before (Pass Mechanics §2 FR-02, now fixed).

### Constant Tags

Every constant in every spec MUST have exactly one of these source tags:

| Tag | Meaning | Rule |
|-----|---------|------|
| `[GT]` | Gameplay-Tuned | Designer sets value; must live in tunable config |
| `[EST]` | Estimated | Placeholder; must be validated before implementation |
| `[FIXED]` | Fixed / physical law | Derived from physics; never tune |
| `[DERIVED]` | Derived from other constants | Formula must be documented; never set independently |
| `[CROSS]` | Cross-spec constant | Defined in another approved spec; consumed read-only here; never set independently in this spec. Citation must name the authoritative spec and section. Use `[CROSS]` only when the value is copied verbatim without modification — if a formula transforms it, tag the result `[DERIVED]`. |

Constants live in their designated `.cs` constant catalogues — no magic numbers in formula code.

### Parameter-Based Physics (No Type Enums)

The Decision Tree supplies physical intent parameters (velocity, spin, angle). Physics systems translate these into vectors. There are NO `KickType`, `ShotType`, or `PassType` enums in the physics layer.

### Heartbeat Tick Rate

Tactical/AI loop: **10 Hz** (100ms per tick). Physics/render loop: **60 Hz** (~16.67ms per frame). These are different loops. Do not conflate them.

### Interface Design Principle

**Write interfaces only when both sides are specified.** Do not create interfaces against unspecified systems. This avoids phantom interface proliferation (ERR-001, ERR-004).

---

## CROSS-REFERENCE SYSTEM

Specs use typed cross-reference IDs:

| Prefix | Meaning | Example |
|--------|---------|---------|
| `XC-` | Cross-spec reference | XC-001 |
| `FM-` | Formula reference | FM-003 |
| `EC-` | Edge case reference | EC-012 |
| `ERR-` | Spec Error Log entry | ERR-010 |

**KNOWN HAZARD — Spec Renumbering Cascades:** When any spec changes its canonical number, ALL cross-references across ALL files must be updated. This has been the single most recurring bug class in this project.

**KNOWN HAZARD — Stale Spec Numbers in Old Files:** Many files written before February 2026 contain wrong spec numbers from an earlier numbering scheme. A complete old-to-correct mapping is in `SPEC_INDEX.md` under "FORMER NUMBERING".

---

## SPEC FILE CONVENTIONS

### Template Structure (every spec follows this)

| Section | Content |
|---------|---------|
| 1 | Introduction, scope, dependencies, key decisions |
| 2 | Functional requirements, data structures, failure modes |
| 3 | Core formulas, algorithms, pseudocode (subsections as needed) |
| 4 | Architecture, file layout, interface contracts |
| 5 | Test plan (unit + integration + validation scenarios) |
| 6 | Performance analysis and budgets |
| 7 | Future extensions and Stage 1+ deferrals |
| 8 | References, citations, DOI verification |
| 9 | Approval Checklist (quality gate) |
| Appendices | Derivations, verification tables, sensitivity analysis |

### Naming Inside Spec Folders

Files within a spec folder use descriptive names without version suffixes:

```
pass-mechanics/
├── outline.md
├── section-1.md
├── section-2.md
├── section-3-1.md          ← Subsections use hyphens
├── section-3-2.md
├── section-3-3-to-3-4.md   ← Grouped subsections
├── section-3-5-to-3-6.md
├── section-3-7-to-3-9.md
├── section-4.md
├── section-5.md
├── section-6.md
├── section-7.md
├── section-8.md
├── section-9-approval-checklist.md
├── appendices.md
└── audit-report.md          ← Comprehensive audit (if completed)
```

---

## AI BEHAVIORAL RULES

### Before Any Task

1. Read this entire `CLAUDE.md`.
2. Check `SPEC_INDEX.md` for current spec numbers and approval status.
3. If modifying a spec, read ALL files in that spec's folder first — not just the target file.
4. If the task involves cross-references, grep the entire `docs/specs/` tree for stale references before finishing.

### When Writing or Editing Specs

- Every constant must have a `[GT]`, `[EST]`, `[FIXED]`, `[DERIVED]`, or `[CROSS]` tag.
- Every formula must include units, valid input ranges, and at least one worked example.
- Never fabricate verification values in Approval Checklists. All values must be programmatically verifiable against source files.
- Append a version history entry to every modified file.
- Include creation date and purpose header on every new file.

### When Writing Code (Future — after all 20 specs approved)

- C# with Unity 2022 LTS conventions.
- Struct-based, zero-allocation architecture in the game loop.
- All constants in designated constant catalogue files — no magic numbers.
- **Fixed64 from Stage 0.** All deterministic game-loop arithmetic uses Fixed64 (Spec #9 defines the library). `float` is permitted only in non-deterministic code paths (rendering, debug visualization). This matches `master-development-plan.md` §6 (Fixed64.cs in Core) and §8 risk mitigation. Existing approved physics specs were drafted against `float`; their formulas must be re-verified against the Fixed64 backend during implementation (test parity is part of each spec's §5 plan).
- Deterministic replay is a hard requirement — no `System.Random`, no `DateTime.Now` in game logic.
- SplitMix64 for deterministic RNG. In Python tooling: omit `UL` suffix from C# constants; mask all intermediate multiplications with `& 0xFFFFFFFFFFFFFFFF`.

### Things That Have Gone Wrong Before (Learn From History)

| Trap | What happened | Prevention |
|------|---------------|------------|
| Stale spec numbers | Decision Tree was #7 in ~75 places; canonical is #8 | Always check SPEC_INDEX.md |
| Fabricated checklist values | Approval Checklist claimed sections existed that were never written | Verify every checklist entry against actual files |
| Inverted fatigue | FR-02 said "1 = rested" — opposite of correct | 0 = rested, 1 = fatigued. Always. |
| Wrong coordinate origin | "Pitch center" comment in Agent Movement §3.5 | Corner-origin is authoritative (Ball Physics §1.2) |
| Phantom interfaces | Interfaces written against unspecified consumer systems | Only write interfaces when both sides are specified |
| Superseded file references | Approval checklist pointed to v1.2 when current was v1.3 | Git versioning eliminates this (no version suffixes) |

---

## TRACKING DOCUMENTS

| Document | Location | Purpose |
|----------|----------|---------|
| `SPEC_INDEX.md` | `docs/specs/SPEC_INDEX.md` | Canonical spec numbering, folder mapping, approval status |
| `PROGRESS.md` | `docs/tracking/PROGRESS.md` | Schedule, milestones, weekly log |
| `Spec_Error_Log` | `docs/tracking/spec-error-log.md` | Cross-spec architectural errors and remediation status |
| `FIX_MANIFEST` | `docs/tracking/fix-manifest-pass-mechanics.md` | Per-audit fix tracking (Pass Mechanics #5) |
| `FILE_MANIFEST` | `docs/tracking/file-manifest.md` | Authoritative file inventory (update after every file change) |

---

## OPEN ISSUES

> **Keep this section current.** When resolving an issue, remove or update its entry here in the same commit. Each entry includes a "since" date so staleness is visible.

- **Pass Mechanics (#5)** — *since March 25, 2026.* Approval SUSPENDED after audit (19 findings, all fixed per `fix-manifest-pass-mechanics.md`). Awaiting lead developer re-review and re-sign-off.
- **Pending lead developer sign-off** — *Agent Movement (#2) since Feb 15; Shot Mechanics (#6) since Feb 24; Decision Tree (#8) since Apr 20, 2026.*
- **Perception System (#7)** — ✅ APPROVED April 22, 2026. (Tracked here only until external sources are reconciled — README.md v1.4 still shows IN REVIEW; remove this line once README is updated.)
- **Renumbering cascade resolved** — *April 26, 2026:* Three-pass sweep applied across all specs. ~115 body-text substitutions in three commits covering Decision Tree #7→#8, Heading #9→#10, Goalkeeper #10→#11, Fixed64 #8→#9. Audit reports and changelog rows intentionally left unchanged (they document history). Verification grep on April 26, 2026 returns zero remaining stale body-text references. Re-run grep before any future tagging.
- **Approval tags created locally, not yet pushed** — *April 26, 2026:* Annotated tags `spec-ball-physics-v1.0-approved`, `spec-collision-system-v1.0-approved`, `spec-first-touch-v1.0-approved`, `spec-perception-system-v1.0-approved` created on `claude/review-project-docs-1ndtt` HEAD. Remote returns HTTP 403 on tag push (branch-protection). Push after merge to main with privileged credentials.
- **Collision System (#3) status disagreement** — *flagged April 26, 2026.* `collision-system/section-9-approval-checklist.md` header reads "Pending Review" with Decision "PENDING — Awaiting Agent Movement Spec #2 approval," but SPEC_INDEX.md, README.md, PROGRESS.md, and file-manifest.md all record APPROVED Feb 19, 2026. Either the §9 file was never updated post-approval or the trackers approved prematurely. Needs lead developer adjudication.
- **Tracking-doc divergence on approval counts** — *flagged April 26, 2026.* PROGRESS.md summary line says "Approved: 3" while its own table shows 4 approved (#1, #3, #4, #7). README.md v1.4 (Apr 21) predates Perception #7 sign-off and shows IN REVIEW. file-manifest.md still shows Perception #7 as IN REVIEW BLOCKED on 4 critical items. SPEC_INDEX.md is the only source consistent with Perception §9 v1.7's APPROVED stamp.
- **`spec-error-log.md` is incomplete** — *since at least April 22, 2026.* File contains only stub entries for ERR-009/010/011 with placeholder text. ERR-001/002/003/004/006/007/008 are cited as resolved across specs but have no entries. The "single source of truth for cross-spec errors" needs to be rebuilt from the spec audit reports that originally produced each ERR.
- **Superseded files from old naming convention** — Tracked in `file-manifest.md` Pending Removal section; manifest itself flagged as pre-migration legacy and last-content-updated Feb 26 despite Apr 22 header. Reconcile during migration completion.
