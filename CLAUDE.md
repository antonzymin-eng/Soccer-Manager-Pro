# CLAUDE.md — Tactical Director

> **Created:** March 26, 2026, 11:00 PM PST
> **Last Updated:** May 13, 2026
> **Purpose:** Authoritative rules for any AI agent (Claude Code, Claude chat, etc.) working on this project. Read this file completely before every task.

---

## PROJECT IDENTITY

**Tactical Director** is a football (soccer) simulation game targeting "Football Manager killer" ambitions. It follows a 10-year, 6-stage development plan. The project is solo-developed with AI assistance.

**Current phase:** Stage 0 — Physics Foundation. Writing 20 formal technical specifications. **No code exists yet. No code will be written until all 20 specs are approved.**

---

## REPO STRUCTURE

```
Soccer-Manager-Pro/
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
│   └── tracking/                   ← PROGRESS.md, spec-error-log.md, fix-manifest-pass-mechanics.md, file-manifest.md
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
| `[CROSS-PENDING]` | Cross-spec constant blocked on an upstream `IN PROGRESS` spec | Used when a spec consumes a constant that will be `[CROSS]` once the upstream authority spec reaches `APPROVED`, but the numeric value is not yet allocated. Citation must name the authoritative spec, section, and the `spec-error-log.md` back-prop ID tracking the allocation. Promoted to `[CROSS]` atomically with upstream approval. Use sparingly — every `[CROSS-PENDING]` tag is an outstanding cross-spec dependency that gates the consuming spec's own `APPROVED` transition. |

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
- **Stage 0 uses `float`. Fixed64 migration is a Stage 5+ concern** (Spec #9 will define the library; existing approved physics specs are drafted against `float` and re-verified against Fixed64 only when cross-platform multiplayer becomes a requirement). Single-machine determinism (replay, save/load, debug rewind) is achieved via state snapshots, not deterministic arithmetic. Cross-platform bit-exact parity is a Stage 5 deliverable, not a Stage 0 quality gate.
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
| `spec-error-log.md` | `docs/tracking/spec-error-log.md` | Cross-spec architectural errors and remediation status |
| `fix-manifest-pass-mechanics.md` | `docs/tracking/fix-manifest-pass-mechanics.md` | Per-audit fix tracking (Pass Mechanics #5) |
| `file-manifest.md` | `docs/tracking/file-manifest.md` | Authoritative file inventory (update after every file change) |

---

## OPEN ISSUES

> **Keep this section current.** When resolving an issue, remove or update its entry here in the same commit. Each entry includes a "since" date so staleness is visible.

- **Event System (#17) — APPROVED** — *May 13, 2026.* Section files (section-1 through section-9-approval-checklist + appendices) authored May 13, 2026 from `outline-detailed.md` v1.1. Section-files PASS 1 adversarial critique (3 H / 5 M / 12 L findings, all resolved in v0.2) and section-files PASS 2 adversarial critique (2 H / 6 M / 7 L findings, all resolved in v0.3) applied. Lead-developer sign-off granted May 13, 2026. All section files at v0.3. `SPEC_INDEX.md` row 17 reflects `APPROVED`. ERR-017-001 (`DOMAIN_TAG_EVENT_LEDGER` allocation in #16 §3.4) remains open; `[CROSS-PENDING]` tag in #17 §3.10 / §3.4.2 promotes to `[CROSS]` atomically with #16 approval.
- **Performance Optimization Strategy (#18) — IN PROGRESS** — *May 13, 2026.* Detailed outline (`outline-detailed.md` v1.0) authored May 13, 2026 from `outline.md` v1.0 + May 6 adversarial review (6 H / 5 M / 2 L findings, all resolved). Establishes 11 cross-cutting design decisions (KD-1…KD-11) including KD-2 per-spec §6 ratify-not-override authority, KD-3 boundary with #16, KD-4 boundary with #19, KD-7 Tier-C-only degradation (deterministic-replay-safe), KD-8 10 Hz / 60 Hz loop-separation tagging, KD-6 determinism-aware profiling, KD-10 hot-path enumeration via per-spec §6 union. Section files remain stubs; advancement to `IN REVIEW` requires authoring `section-1.md` through `section-9-approval-checklist.md` + `appendices.md` from the detailed outline. **Outstanding follow-up — ERR-018-001 (May 13, 2026):** `outline-detailed.md` cites "#16 §8 trace channels", "#16 §5 canonical save format", and "#16 §7 regression scenarios" — same citation-drift pattern #19 v0.2 corrected on May 12 (#16 §7 actual location is §5; #16 §5 actual location is §3.2.4.1; no "§8 trace channels" section exists in #16). The outline's KD-3 status caveat already mandates `grep` verification of #16 subsection numbers at section-file draft time and tags every #16 citation `TBD-NORMATIVE`, so the outline-level inaccuracy is bounded; section-file authoring MUST resolve each citation, and for "trace channels" specifically MUST determine whether #16 will publish a trace-channel section or whether KD-3 needs re-anchoring (or whether Spec #18 owns the trace pipeline from scratch). Tracked as `ERR-018-001` in `docs/tracking/spec-error-log.md`. **Unblocks:** Spec #19 KD-2 precondition (b) "Spec #18 having at least an outline-level draft with §4 and §7 headers" — now satisfied (both §4 and §7 headers present and detailed); precondition (b) closure pending #19 reviewer confirmation.
- **Testing Strategy & Framework (#19) — IN REVIEW** — *May 12, 2026; precondition (b) cleared May 13, 2026.* Initial section-file draft authored May 12 from `outline-detailed.md` v1.1; v0.2 self-critique sweep applied same day (3 H / 6 M / 8 L findings, all resolved). Per spec §9.3.6–§9.3.8 (KD-2 sequencing), advancement to `APPROVED` is gated on three preconditions: (a) Spec #16 (Deterministic Simulation) reaching Tier 2 `APPROVED`, (b) Spec #18 (Performance Optimization Strategy) having at least an outline-level draft with §4 and §7 headers, and (c) all `TBD-NORMATIVE`-tagged citations of #16 and #18 in the #19 spec text resolved. **Status (May 13, 2026):** (a) #16 still `IN PROGRESS` — NOT cleared; (b) **CLEARED** by Spec #18 `outline-detailed.md` v1.0 landing — §4 (Architecture & Integration) and §7 (Future Extensions) headers both present and detailed; (c) `TBD-NORMATIVE` tags remain in `testing-strategy/section-3.md` and `appendices.md` — NOT cleared (still gated on #16 approval, and now also pending #18 section-file drafting before #18-side tags can resolve). Lead-developer sign-off on `IN REVIEW → APPROVED` remains blocked until (a) and (c) clear.
- **Code Standards & Style Guide (#20) — APPROVED** — *May 11, 2026.* Section files + appendices authored May 7–8 from `outline-detailed.md` v1.3; adversarial review pass-1 applied May 11; lead-developer R-01..R-05 sign-off completed same day. `SPEC_INDEX.md` row 20 reflects `APPROVED`. §9 v1.1 APPROVED.
- **Pass Mechanics (#5) — RESOLVED** — *re-approved May 6, 2026.* Original Feb 22 approval → suspended Mar 25 (19 audit findings, all fixed per `fix-manifest-pass-mechanics.md`) → re-approved May 6 after §3.3–§3.9 follow-up findings F-A01 / F-A02 resolved via option-3 hybrid (`spinBase`/`spinMax` columns added to §3.1.4 Master Physical Profile Table; `WINDUP_FRAMES`/`FOLLOWTHROUGH_FRAMES` localized in §3.8.10). Re-review packet at `pass-mechanics/re-review-packet.md`. Stage 0 Priority 1–2 spec set (#1–#8) is now complete.
- **Decision Tree (#8) draft-level quality gate** — *April 27, 2026.* Approved at draft-level rigor (lighter than Pass Mechanics #5 / Shot Mechanics #6 programmatic audit). Comprehensive audit is a candidate before implementation begins. Tracked here as a follow-up item, not a blocker.
- **Renumbering cascade resolved** — *April 26, 2026:* Three-pass sweep applied across all specs. ~115 body-text substitutions in three commits covering Decision Tree #7→#8, Heading #9→#10, Goalkeeper #10→#11, Fixed64 #8→#9. Audit reports and changelog rows intentionally left unchanged (they document history). Verification grep on April 26, 2026 returns zero remaining stale body-text references. Re-run grep before any future tagging.
- **Approval tags created locally, not yet pushed** — *April 26–27, 2026; precondition check added May 6, 2026; tag 8 added May 13, 2026:* Annotated tags `spec-ball-physics-v1.0-approved`, `spec-agent-movement-v1.0-approved`, `spec-collision-system-v1.0-approved`, `spec-first-touch-v1.0-approved`, `spec-shot-mechanics-v1.0-approved`, `spec-perception-system-v1.0-approved`, `spec-decision-tree-v1.0-approved`, `spec-event-system-v1.0-approved` created on their respective branches. Remote returns HTTP 403 on tag push (branch-protection). Push after merge to main with privileged credentials. **Precondition check (run BEFORE `git push --tags`):** (1) After merge to main, run `git tag -v <tagname>` for each of the 7 tags and confirm the SHA each tag points to is still reachable from `origin/main` (`git merge-base --is-ancestor <tag-sha> origin/main`). (2) If main uses **squash-merge** or **rebase-merge**, the original branch commit SHAs are orphaned and the tags point to dead commits — in that case **delete and recreate** each tag against the corresponding squashed/rebased commit on main (use `git log --grep` or commit-message search to identify the new commit), do NOT just `--force` push. (3) If main uses true merge commits, normal `git push origin --tags` is correct. Document which path was taken in this OPEN ISSUES entry when resolving.
- **Fixed64 stage scope decision** — *April 26, 2026:* After review, Fixed64 migration is **Stage 5+** (not Stage 0). Stage 0 uses `float` and achieves single-machine determinism via state snapshots. Cross-platform bit-exact parity is no longer a Stage 0 quality gate — it moves to Stage 5 when multiplayer is added. CLAUDE.md "When Writing Code" rules and `master-development-plan.md` (§2.3, §8 risk mitigation, §9 metrics, Stage 0 success criteria, Stage 6 description) all updated to match. Existing approved physics specs were drafted under this Stage 5+ assumption and remain consistent — no spec content changes needed.
- **EntityId no-reuse cross-spec back-propagation — RESOLVED at spec-text level** — *May 3, 2026 → May 6, 2026.* Deterministic Simulation #16 §3.2.5 declares a normative cross-spec constraint binding APPROVED specs Agent Movement (#2) and Decision Tree (#8) to guarantee EntityId uniqueness for the lifetime of a match (no despawned-ID reuse). Filed as `ERR-016-002` in `docs/tracking/spec-error-log.md`. **Resolution (May 6, 2026):** added `XC-002-001` to Agent Movement #2 §2.5 (v1.1.1, non-behavioral patch) and `XC-008-001` to Decision Tree #8 §1.7.3 (v1.1.1, non-behavioral patch). Both are patch-level revisions — no formula or contract change, only the formal cross-spec binding. **Outstanding:** small prose update in `docs/specs/deterministic-sim/section-3.md` §3.2.5 to change "filed for back-propagation" → "back-propagated to #2 §2.5 and #8 §1.7.3". Once that prose update lands, this issue closes and the spec-error-log row for ERR-016-002 flips to fully closed.
- **Deterministic Simulation #16 third-pass critique resolved (specs only)** — *May 3, 2026; approval model split May 6, 2026.* Third adversarial pass (4 H, 6 M, 6 L, 1 cross-cutting) addressed in §1, §3, §4, §5, §9 at v0.9. Full per-finding fix log: `docs/specs/deterministic-sim/third-pass-fix-log.md`. **§9 v1.1 (May 6, 2026)** split approval into Tier 1 (Conditional Approval — self-contained spec content; pursuable now) and Tier 2 (Final Approval — gated on #9 / #17 / #18 / #19 reaching `IN REVIEW`; #17 is now `APPROVED` — beyond the `IN REVIEW` gate — satisfying that precondition). `TBD-NORMATIVE` tag introduced for §8.3.1 placeholder cross-spec citation rows. **Golden-vector files (§9.5 #4):** the two RFC-derived KAT files (`hkdf-sha256-kat.md`, `siphash-2-4-kat.md`) are authorable now and are part of Tier 1; `serialize-canonical-corpus.md` remains Tier 2.
- **Superseded files from old naming convention** — Tracked in `file-manifest.md`. Manifest reconciled May 6, 2026 to post-migration folder names; covers all 20 spec folders with current statuses.
- **Stage 0 host platform pin — `certification-platform.md` is a placeholder, not pinned** — *verified May 6, 2026.* `docs/tracking/certification-platform.md` exists (created May 2, 2026) but every Stage 0 row except CPU architecture is `_TBD_` / `⏳ Not pinned`. Required pins: OS (Win 10/11), Unity LTS revision (e.g. 2022.3.X exact), backend (Mono vs IL2CPP), IL2CPP version (if applicable), compiler-flag set (denormals-are-zero off, fp-contract off, fma off unless platform-pinned), worker thread count, SIMD feature level. **Action:** lead-developer to fill in concrete values before the first Spec #16 §5.5 `FR-DS-009-GATE` certification run; until then the gate is unactivatable on Stage 0 (which is consistent with §5.5's own note). Not blocking spec drafting; blocking first cert run only.
