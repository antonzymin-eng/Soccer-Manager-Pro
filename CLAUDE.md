# CLAUDE.md — System XI agent guide

> **Purpose:** Small, always-loaded routing guide for AI agents.
> **Expanded reference:** [`docs/agent-guides/project-reference.md`](docs/agent-guides/project-reference.md).
> Load that reference only when a task needs its repository map, document catalogue, historical traps,
> or open-issue index. Live status belongs to the tracking documents linked below.

## Start here

1. Read this file before every task.
2. Inspect the files relevant to the task; do not preload the expanded reference by default.
3. For code changes, read [`src/CLAUDE.md`](src/CLAUDE.md). It routes detailed coding topics on demand.
4. For specification changes, check [`docs/specs/SPEC_INDEX.md`](docs/specs/SPEC_INDEX.md), then read
   every file in the affected spec folder.
5. Use [`docs/tracking/path-to-playable-roadmap.md`](docs/tracking/path-to-playable-roadmap.md) for
   implementation order and [`docs/tracking/open-issues.md`](docs/tracking/open-issues.md) for live blockers.
6. For player-facing UX/design work, read [`docs/design/ux-high-level-plan.md`](docs/design/ux-high-level-plan.md)
   and [`docs/design/ux-detailed-plan.md`](docs/design/ux-detailed-plan.md) before editing mockups or client UX.
   The detailed plan's F0 tracking close-out precedes F1 and any further substantial UX production.

## Project essentials

System XI is a Unity 6 LTS football simulation. Specifications intentionally run ahead of
implementation: **APPROVED does not mean implemented**. Check `src/` before assuming a consumer exists,
and do not wire or harden an assembly-less specification ahead of its T0 landing.

Authoritative indexes:

- Spec number, folder, and status: `docs/specs/SPEC_INDEX.md`.
- Schedule and milestones: `docs/tracking/PROGRESS.md`.
- Entity ownership and implementation pointer: `docs/tracking/data-contract-index.md`.
- Match-engine dormant capabilities and tuning freeze: `docs/tracking/match-engine-wiring-backlog.md`.
- File inventory: `docs/tracking/file-manifest.md`.
- Change history: `docs/tracking/CHANGELOG.md` and `docs/tracking/CHANGELOG-src.md`.

## Non-negotiable domain rules

- Coordinates use a corner origin: X is goal-to-goal (0–105 m), Y is touchline-to-touchline
  (0–68 m), and Z is height. Ball Physics Spec #1 §1.2 and Appendix C are authoritative.
- Fatigue is `0.0 = fully rested`, `1.0 = fully fatigued`.
- Every spec constant has exactly one source tag: `[GT]`, `[EST]`, `[FIXED]`, `[DERIVED]`, `[CROSS]`,
  or (only for a blocked upstream allocation) `[CROSS-PENDING]`.
- The Decision Tree emits physical parameters, never physics-layer `KickType`, `ShotType`, or
  `PassType` enums.
- Tactical/AI ticks at 10 Hz; physics/render runs at 60 Hz. Do not conflate the loops.
- Create an interface only when both producer and consumer are specified.
- Stage 0 uses `float`; Fixed64 is deferred to Stage 5+.
- Deterministic replay is mandatory: no `System.Random` or wall-clock state in game logic. Use
  SplitMix64; Python mirrors omit `UL` and mask intermediate products with `& 0xFFFFFFFFFFFFFFFF`.

## Specification work

- Cross-references use `XC-`, `FM-`, `EC-`, and `ERR-` IDs. Renumbering requires a repository-wide
  search; old documents may contain former numbers, whose mapping is in `SPEC_INDEX.md`.
- Every formula includes units, valid ranges, and a worked example.
- Never fabricate approval-checklist evidence; verify it programmatically against source files.
- Append a version-history row to every modified spec file and include creation date and purpose in
  every new file.
- If cross-references change, search all of `docs/specs/` for stale references before finishing.
- `docs/tracking/*-design.md` files are design supplements, not approved specs. When a promoted spec
  and its historical supplement differ, the spec wins.

## Evidence and sequencing

- Run the relevant compile/test gate before claiming a suite enforces a behavior.
- Assert outcomes, not merely that a composition loop completes.
- Mirror team-relative geometry tests for home and away teams.
- Do not tune a subsystem that is not wired. Check the wiring backlog and honor KD-W1.
- A specification defect and its implementation back-propagation land together.

## Load-on-demand reference

Read `docs/agent-guides/project-reference.md` when you need:

- the expanded repository and assembly maps;
- the full tracking-document and agent-skill catalogue;
- historical failure narratives and prevention notes;
- the snapshot of the open-issue index.

Treat status and counts in that expanded file as historical context. The owning spec and tracking
files remain authoritative.
