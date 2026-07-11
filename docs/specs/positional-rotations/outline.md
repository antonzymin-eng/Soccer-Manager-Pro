# Positional Rotations Specification #25 — Outline

**Created:** July 8, 2026
**Last Updated:** July 8, 2026 (v0.1 — promoted from `docs/tracking/advanced-positional-behaviors-design.md` v0.3)
**Version:** 0.1
**Status:** APPROVED
**Source:** `docs/tracking/advanced-positional-behaviors-design.md` v0.3 (July 7, 2026), AR-1..AR-3 converged

---

## Purpose

Adds **dynamic slot rotations**: a controller that swaps two agents' `FormationSlotRecord` bindings
(`AgentPositioningData.SlotIndex`) mid-match when they have organically exchanged regions during
in-possession play, instead of forcing both to run home across each other. This is the first system
that reassigns a slot after `SeedFromFormation` — flagged by the design supplement (KD-4) as the
largest and riskiest of the three positional-behavior candidates — so the spec is deliberately
narrow: **pairwise swaps only**, from a **static per-`FormationFamily` adjacency table**, with
two-sided hysteresis and a hard per-tick rotation cap. Default-neutral via
`RotationFreedom.Off = 0`.

## Section map

| Section | Content |
|---|---|
| 1 | Introduction, scope, dependencies, key decisions (KD-1..KD-8), boundary matrix |
| 2 | Functional requirements (FR-RO-001..018), data structures, failure modes F1–F5 |
| 3 | Algorithms: trigger predicate, two-sided hysteresis, swap commit, revert, ShapeAnalyzer contract |
| 4 | Architecture, file placement (extends `src/positioning-ai/`), routing contract |
| 5 | Test plan + FR traceability |
| 6 | Performance budget |
| 7 | Future extensions and deferrals |
| 8 | References |
| 9 | Approval checklist |
| App. | Adjacency tables, worked example, snapshot order, hysteresis-interaction derivation |

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-08 | — | Initial promotion from design supplement v0.3; open question 3 resolved to the static adjacency table. |
#endregion
