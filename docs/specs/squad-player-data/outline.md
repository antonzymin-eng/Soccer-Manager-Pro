# Squad / Player Data Layer Specification #27 — Outline

**Created:** July 22, 2026
**Last Updated:** July 22, 2026 (v0.1 — promoted from `docs/tracking/squad-player-data-design.md` v0.6)
**Version:** 0.1
**Status:** APPROVED
**Source:** `docs/tracking/squad-player-data-design.md` v0.6 (July 18, 2026), AR-1..AR-2 converged + T0/T1/T2/T3 implementation-time corrections

---

## Purpose

Defines the **canonical player-data layer** that replaces the match engine's all-synthetic,
all-neutral agent seeding: one canonical `PlayerAttributes` record (the single source of truth every
per-spec attribute struct projects from — closing `ERR-007` for real), deterministic roster
generation, a club-scoped `Squad` container, and a Stage-0 human-authoring text import. This is a
**data layer only** — not squad management, not a UI, not a transfer market. Unusually, the layer is
**already built and wired** into `MatchEngine` (the code preceded the numbered spec, the #21/#22
precedent inverted): this spec documents what exists, in present tense, and §7 records the landed
T-phase wiring status.

## Section map

| Section | Content |
|---|---|
| 1 | Introduction, scope (data layer only), dependencies (DeterministicSim #16 alone), key decisions (KD-1..KD-8), boundary matrix (club vs match-team; PlayerPosition vs RoleId) |
| 2 | Functional requirements (FR-SQ-001..026), data structures, failure modes F1–F5 |
| 3 | Algorithms: canonical record + `AttrIdx` ordinal map; `CreateDefault`; `ToArray`/`FromArray`; `RosterGenerator` draw sequence; position-bias; separate WeakFoot jitter; `SquadFileLoader` grammar. Worked generation example |
| 4 | Architecture: `TacticalDirector.PlayerDatabase` bottom-of-graph assembly, file placement, RNG-stream registration contract, the T1 CS0104 fully-qualify hazard |
| 5 | Test plan (unit / determinism) + FR traceability; no ScenarioRunner scenario at T0 |
| 6 | Performance: club-setup-time only (not per-tick, not zero-alloc); draw budget |
| 7 | Future extensions / T-phase wiring status; Stage-1+ deferrals |
| 8 | References |
| 9 | Approval checklist |
| App. | Constant catalogue, position-bias table, `AttrIdx` ordinal table, worked generation numbers |

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-22 | — | Initial promotion from design supplement v0.6 (documents the already-built, already-wired layer; §7 records landed T-phase status). |
#endregion
