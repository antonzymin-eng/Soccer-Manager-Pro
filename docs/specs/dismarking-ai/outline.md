# Dismarking & Marker-Awareness AI Specification #23 — Outline

**Created:** July 8, 2026
**Last Updated:** July 8, 2026 (v0.1 — promoted from `docs/tracking/advanced-positional-behaviors-design.md` v0.3)
**Version:** 0.1
**Status:** APPROVED
**Source:** `docs/tracking/advanced-positional-behaviors-design.md` v0.3 (July 7, 2026), AR-1..AR-3 converged

---

## Purpose

Gives off-ball attacking agents **marker-awareness**: a perception-derived `MarkingPressure` signal
(how tightly the agent is being marked, computed strictly from its own Perception System #7
`FilteredView`) and two default-off consumers of that signal — a **dismarking offset stage** in the
Positioning AI #12 `SlotComposer` pipeline (evasive off-ball movement away from a persistent marker)
and a **marked-pass-target penalty** in the Decision Tree #8 `UtilityScorer` (a passer discounts
heavily marked teammates, scaled by its own awareness attributes). It does **not** change who marks
whom (#14 owns marking) and never reads the opposing team's internal directives (KD-1, the
perception-boundary invariant).

## Section map

| Section | Content |
|---|---|
| 1 | Introduction, scope, dependencies, key decisions (KD-1..KD-7), boundary matrix, stage binding |
| 2 | Functional requirements (FR-DM-001..018), data structures, failure modes F1–F4 |
| 3 | Algorithms: MarkingPressure formula, dwell state machine, dismark offset stage, pass-target penalty |
| 4 | Architecture, file placement (extends existing assemblies — no new assembly), routing contract |
| 5 | Test plan (unit / integration / determinism) + FR traceability |
| 6 | Performance budget |
| 7 | Future extensions and deferrals |
| 8 | References |
| 9 | Approval checklist |
| App. | Worked examples, constant catalogue, sensitivity notes |

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-08 | — | Initial promotion from design supplement v0.3 (§1–§4 content, KD-2/KD-5/KD-6 resolved to concrete decisions). |
#endregion
