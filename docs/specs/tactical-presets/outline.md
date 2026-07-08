# Tactical Presets & AI-Manager Selection Specification #26 — Outline

**Created:** July 8, 2026
**Last Updated:** July 8, 2026 (v0.1 — promoted from `docs/tracking/game-model-ai-manager-design.md` v0.4)
**Version:** 0.1
**Status:** IN REVIEW
**Source:** `docs/tracking/game-model-ai-manager-design.md` v0.4 (July 7, 2026), AR-1/AR-2 + convergence

---

## Purpose

Adds (a) a **named preset library** — immutable bundles of one `TeamTactic` plus optional
per-agent `PlayerTactic`s over the existing Spec #21 substrate — and (b) **AI-manager
selection/adaptation logic** that picks a preset at kickoff and re-evaluates it on a coarse,
deterministic cadence from own-team-observable match state (score differential, time remaining).
One spec, two halves, staged T0–T4 (mirroring #21's T-phase pattern), because both halves share
one consumer path: preset → config → the existing `TeamTacticConfigApplier`/`PlayerTacticConfigApplier`
boot seam, or `MatchEngine.SetTeamTactic`/`SetPlayerTactic` directly for mid-match changes.
Default-neutral at the subsystem level: with the manager AI disabled (the default), a match is
byte-identical to today.

## Section map

| Section | Content |
|---|---|
| 1 | Introduction, scope, dependencies, key decisions (KD-1..KD-8), boundary matrix, T-phase staging |
| 2 | Functional requirements (FR-TP-001..020), data structures, failure modes F1–F5 |
| 3 | Algorithms: preset projection, decision cadence, selection scoring, adaptation ladder |
| 4 | Architecture, file placement, routing/caller contracts |
| 5 | Test plan + FR traceability |
| 6 | Performance budget |
| 7 | Future extensions and deferrals (opponent-aware adaptation; on-disk preset format) |
| 8 | References |
| 9 | Approval checklist |
| App. | Preset catalogue, scoring worked examples, snapshot order |

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-08 | — | Initial promotion from design supplement v0.4; open questions 1–4 resolved to concrete decisions (§1 KD list). |
#endregion
