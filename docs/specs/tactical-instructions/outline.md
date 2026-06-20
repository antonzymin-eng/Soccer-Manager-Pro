# Tactical Instructions Specification #21 — Outline

**Created:** June 20, 2026
**Last Updated:** June 20, 2026 (v0.1 — promoted from `docs/tracking/tactical-instruction-layer-design.md` v0.3)
**Version:** 0.1
**Status:** IN REVIEW (Stage-1 forward spec; runtime activation gated — see §1.8 / §7)
**Source:** `docs/tracking/tactical-instruction-layer-design.md` v0.3 (June 20, 2026), three adversarial fix passes

---

## Purpose

Defines the **manager-facing tactical instruction input layer** — formation, mentality, phase-split
team instructions, behavioural player roles & duties, and individual player instructions — and the
seams by which a chosen tactic drives the already-built AI subsystems (#8 Decision Tree, #11 Goalkeeper,
#12 Positioning, #13 Pressing, #14 Defensive, #15 Attacking). It does **not** redesign those subsystems:
they already produce the emergent behaviours (mark modes, press roles, attacking runs, formation slots).
The gap this spec fills is that nothing currently feeds them a manager's intent.

## Section map

| Section | Content |
|---|---|
| 1 | Introduction, scope, dependencies, key decisions (KD-1..KD-12), boundary matrix, stage binding |
| 2 | Functional requirements (FR-TI-001..032), data structures, failure modes F1–F5 |
| 3 | Algorithms: enum-translation seams, Mentality→profile mapping, role-weight model, instruction biases |
| 4 | Architecture, assembly/file layout, routing, interface contracts |
| 5 | Test plan (unit / integration / simulation / determinism) + FR traceability |
| 6 | Performance budget |
| 7 | Future extensions and Stage-1+ deferrals |
| 8 | Cross-references (XC-021-NNN), ERR-021-NNN, CLAUDE.md invariant binding |
| 9 | Approval checklist |
| Appendices | Constant catalogue + derivations; Mentality / role-weight / bias tables |

## Key decisions (summary; **full set KD-1..KD-12 in §1.5**)

- **KD-1** Input-only layer; produces no per-tick directive.
- **KD-2** Bottom-layer assembly; owns its own instruction enums; consumers translate downward (no upward reference).
- **KD-3** `PlayerRole` (behavioural) is distinct from positional `RoleId` (#12).
- **KD-4** Two-path routing: #8 via `TacticalContext`; #12–#15 via their own snapshots; match-engine Phase-D populates both.
- **KD-5** Snapshots carry the translated *local* enum; translation runs once per tactic-change.
- **KD-6** No RNG / no domain tag (deterministic application).
- **KD-7** In-match changes apply only at a 10 Hz tactical-stride boundary.
- **KD-8** Runtime activation gated on the `[GT]` config-loader + match-engine Phase C/D.
- **KD-9..KD-12** man-mark-vs-safety-floor precedence; identity defaults; `FocusPlay`/`Tempo`/role-weights as new logic; snapshot-schema ownership — see §1.5.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-06-20 | — | Promoted from the design supplement (v0.3) to a formal spec folder. |
#endregion
