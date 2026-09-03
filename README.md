# Tactical Director — Football Management Simulation

Tactical Director is a long-horizon football management simulation focused on deep match simulation,
transparent systems, deterministic behaviour, and meaningful managerial decisions.

The project combines a detailed football simulation core with the broader systems required for a full
management game: tactics, player development, squad building, competitions, finances, staff, scouting,
club operations, presentation, and long-term world simulation.

Development is specification-first. Approved design and implemented code are deliberately tracked
separately.

## Project vision

Tactical Director is built around four principles:

- **Observable systems** — tactical and managerial decisions should produce visible, measurable effects.
- **Deep simulation** — football behaviour should emerge from interacting physical, tactical, technical,
  and human systems rather than scripted outcomes.
- **Transparent mechanics** — important outcomes should be explainable rather than opaque.
- **Deterministic foundations** — simulation state, randomness, save/restore, and replay behaviour are
  designed to be reproducible and testable.

The long-term roadmap is defined in the [Master Development Plan](docs/planning/master-development-plan.md)
and the four master design volumes under `docs/planning/`.

## Current state

**Status snapshot — September 3, 2026:** 35 production assemblies exist, while **19 of the 53 approved
specs have no `src/` assembly**. The specification frontier therefore remains materially ahead of
implementation. A2, the architecture-governance schema and executable-semantics freeze, closed
September 2; A3 governance integration is in progress. The Unity target is 6000.4.9f1 / DX11 and was
recertified July 19, 2026.

This snapshot is orientation only. It is not an authoritative project-status registry. For current
state, use:

- [Specification Index](docs/specs/SPEC_INDEX.md) — specification numbering, folders, and approval status.
- [Path to Playable Roadmap](docs/tracking/path-to-playable-roadmap.md) — active implementation sequence
  and remaining implementation gaps.
- [Architecture Governance Integration Plan](docs/planning/project-architecture-governance-integration-plan.md)
  — current architecture-governance rollout.
- [Change Log](docs/tracking/CHANGELOG.md) — landing history and measured development results.

The implementation gap is intentional but important: **an APPROVED specification does not imply that
its runtime implementation exists.** Check `src/` and the active roadmap before assuming a subsystem is
available.

## Repository structure

```text
Soccer-Manager-Pro/
├── Assets/                  Unity project assets
├── Packages/                Unity package configuration
├── ProjectSettings/         Unity project settings
├── src/                     Production C# assemblies and tests
├── docs/
│   ├── specs/               Numbered normative specifications
│   ├── planning/            Master plans, design volumes, governance
│   ├── tracking/            Roadmaps, evidence, histories, manifests
│   ├── design/              Supporting visual/design material
│   └── agent-guides/        Expanded implementation references
├── tools/                   CI, validation, governance, and analysis tooling
├── CLAUDE.md                Compact repository-level agent rules
└── README.md                Project orientation
```

The repository intentionally does not maintain a complete assembly map or specification-status table in
this README. Those inventories change frequently and belong to their authoritative tracking surfaces.

## Documentation authority

Different documents own different kinds of information. When two summaries disagree, use the owning
source rather than the newest-looking prose.

| Information | Authority |
| --- | --- |
| Specification number, folder, approval status | [`docs/specs/SPEC_INDEX.md`](docs/specs/SPEC_INDEX.md) |
| Current implementation sequence | [`docs/tracking/path-to-playable-roadmap.md`](docs/tracking/path-to-playable-roadmap.md) |
| Project architecture governance | [`docs/planning/project-architecture-governance.md`](docs/planning/project-architecture-governance.md) |
| Governance implementation sequence | [`docs/planning/project-architecture-governance-integration-plan.md`](docs/planning/project-architecture-governance-integration-plan.md) |
| File inventory | [`docs/tracking/file-manifest.md`](docs/tracking/file-manifest.md) |
| Landing history and measured results | [`docs/tracking/CHANGELOG.md`](docs/tracking/CHANGELOG.md) |
| Repository-level agent rules | [`CLAUDE.md`](CLAUDE.md) |
| C# coding and verification rules | [`src/CLAUDE.md`](src/CLAUDE.md) |
| Expanded coding reference | [`docs/agent-guides/coding-reference.md`](docs/agent-guides/coding-reference.md) |
| Long-term development roadmap | [`docs/planning/master-development-plan.md`](docs/planning/master-development-plan.md) |

Historical development narratives formerly stored in this README are preserved verbatim in
[`docs/tracking/CHANGELOG-readme.md`](docs/tracking/CHANGELOG-readme.md). That archive records history;
it does not define current project state.

## Architecture at a glance

Production code lives under `src/` as independently defined assemblies with explicit dependency
boundaries. The simulation is built around several broad areas:

- physics and player movement;
- perception, decision-making, and tactical AI;
- match-engine composition and deterministic simulation;
- season and world-state systems;
- management systems;
- presentation and client layers;
- infrastructure for testing, performance, and configuration.

`match-engine` is the composition root for match simulation. Cross-cutting deterministic and event
infrastructure is kept separate from gameplay consumers. Presentation code reads simulation output
rather than becoming part of simulation authority.

The complete dependency model and architecture rules are governed by the Code Standards specification
and the project architecture-governance documents. Do not infer allowed dependencies from this summary.

## Development model

The project follows a specification-first workflow:

1. Define or amend the governing design.
2. Resolve cross-specification dependencies and ownership.
3. Review the design adversarially.
4. Implement in bounded slices.
5. Verify behaviour mechanically.
6. Record evidence and remaining gaps.
7. Land code and its tracking updates together.

Architecture, determinism, save compatibility, test evidence, and lifecycle integration are treated as
engineering constraints rather than documentation afterthoughts.

The project also distinguishes deliberately between:

- **approved design** — what the system is specified to do;
- **implemented capability** — what exists in `src/`;
- **runtime activation** — what is actually wired and exercised;
- **verified behaviour** — what current evidence demonstrates.

Those states must not be treated as interchangeable.

## Getting started

### Understanding the project

Start with:

1. This README for orientation.
2. [`docs/planning/master-development-plan.md`](docs/planning/master-development-plan.md) for the
   long-term roadmap.
3. [`docs/specs/SPEC_INDEX.md`](docs/specs/SPEC_INDEX.md) to locate the governing specification for a
   subsystem.
4. [`docs/tracking/path-to-playable-roadmap.md`](docs/tracking/path-to-playable-roadmap.md) for the
   current implementation sequence.

The four master design volumes under `docs/planning/` describe the broader simulation and management
vision.

### Working on code

Before changing `src/`:

1. Read [`CLAUDE.md`](CLAUDE.md).
2. Read [`src/CLAUDE.md`](src/CLAUDE.md).
3. Read the governing approved specification.
4. Inspect the target assembly, its `.asmdef`, neighboring code, and tests.
5. Check the active roadmap and relevant tracking records for unresolved integration work.
6. Run the narrowest relevant tests before the repository-wide gate.

Exact build, test, coding, allocation, determinism, and constant-management rules live in
`src/CLAUDE.md` and the expanded coding reference. They are not duplicated here because those
operational details change more often than this project overview should.

### Working on specifications or architecture

For normative specification work, begin with the owning specification and its approval checklist.

For architecture-governance work, begin with:

- [`project-architecture-governance.md`](docs/planning/project-architecture-governance.md)
- [`project-architecture-governance-integration-plan.md`](docs/planning/project-architecture-governance-integration-plan.md)
- the relevant artifacts under [`docs/tracking/architecture-governance/`](docs/tracking/architecture-governance/)

The repository uses machine-readable evidence and executable semantics where architectural claims can
be checked mechanically.

## Verification

The repository contains automated checks for compilation, tests, specification consistency, assembly
dependency rules, documentation consistency, and architecture-governance evidence.

The Linux .NET gate is a development and CI verification surface. Certified performance and
platform-sensitive evidence use the separately pinned Unity environment.

Do not infer a successful current gate result from this README. Use the current CI run and the relevant
evidence record.

## Project status and scope

Tactical Director is under active development and is not a finished game.

The project is being developed as a long-term simulation platform rather than a short prototype.
Subsystems may therefore have complete approved specifications well before their implementation reaches
the runtime.

The repository is currently developed by a solo developer with AI assistance.

## README maintenance contract

This README is intentionally an **orientation document**, not a project ledger. It must not become
another authoritative copy of:

- specification status;
- the complete assembly inventory;
- implementation sequencing;
- open issues;
- test counts;
- schema-version history;
- per-landing development narratives.

There is intentionally **no `Last Updated` history chain** in this file, and
`.claude/skills/landing-close-out/scripts/check_drift.sh` reports it and exits non-zero if one reappears.

When a volatile fact matters for orientation, keep at most one clearly dated snapshot and point directly
to its authoritative source. Replace that snapshot when necessary; do not append historical snapshots.

Historical README material belongs in [`docs/tracking/CHANGELOG-readme.md`](docs/tracking/CHANGELOG-readme.md).

## License

**TBD** — to be determined before commercial release.
