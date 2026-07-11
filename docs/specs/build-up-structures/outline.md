# Scripted Build-Up Structures Specification #24 — Outline

**Created:** July 8, 2026
**Last Updated:** July 8, 2026 (v0.1 — promoted from `docs/tracking/advanced-positional-behaviors-design.md` v0.3)
**Version:** 0.1
**Status:** APPROVED
**Source:** `docs/tracking/advanced-positional-behaviors-design.md` v0.3 (July 7, 2026), AR-1..AR-3 converged

---

## Purpose

Adds **phase-gated structural overlays** to in-possession positioning: named build-up structures
(e.g. a back-three build-up, a double pivot) expressed as small anchor-offset tables indexed by
**ball-progression zone** (own / middle / final third, team-relative), applied as one additional
offset stage in Positioning AI #12's `SlotComposer` pipeline. It introduces **no new action type**
(KD-1) and is default-neutral: `BuildUpStructure.None` (zero value) leaves every composed target
byte-identical to today. `TeamTactic.TransitionWon` gains its first AI-side consumer as a
post-regain suppression window (a counter-attacking plan suppresses patient structure).

## Section map

| Section | Content |
|---|---|
| 1 | Introduction, scope, dependencies, key decisions (KD-1..KD-7), boundary matrix |
| 2 | Functional requirements (FR-BU-001..016), data structures, failure modes F1–F4 |
| 3 | Algorithms: zone classifier + hysteresis, overlay tables, post-regain suppression |
| 4 | Architecture, file placement (extends `src/positioning-ai/`), routing contract |
| 5 | Test plan + FR traceability |
| 6 | Performance budget |
| 7 | Future extensions and deferrals |
| 8 | References |
| 9 | Approval checklist |
| App. | Structure catalogue tables, worked example, snapshot order |

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-08 | — | Initial promotion from design supplement v0.3; supplement's "gated by TransitionWon" refined to opt-in dial + TransitionWon suppression window (see §1 KD-3 rationale). |
#endregion
