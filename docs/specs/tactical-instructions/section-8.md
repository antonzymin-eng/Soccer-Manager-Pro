# Tactical Instructions Specification #21 — Section 8: Cross-References, Error Log, Invariant Binding

**Created:** June 20, 2026
**Last Updated:** June 20, 2026 (v0.1)
**Version:** 0.1
**Status:** IN REVIEW

---

## 8.1 Cross-spec references (XC-021-NNN)

| ID | Target | Nature |
|---|---|---|
| XC-021-001 | #8 §2.2.6 `TacticalContext` | extend (replace 2 bool stubs with `TeamTactic`/`PlayerTactic`) |
| XC-021-002 | #8 §3.2 `UtilityScorer` product | insert role/mentality/duty/instruction multipliers |
| XC-021-003 | #8 `OptionGenerator` | new `FocusPlay` lateral branch + `Tempo`/`Width` bias |
| XC-021-004 | #12 `ContextModifierInputs` / `PositioningPerceptionSnapshot` | width/role/duty fields |
| XC-021-005 | #12 `FormationFamily` + pull-factor table | `TacticFormation` translation target; role-offset table |
| XC-021-006 | #13 `PressingSnapshot` / `TriggerEvaluator` | trigger-mask + line-of-engagement |
| XC-021-007 | #13 `TriggerFlags` | `TacticTriggerMask` translation target |
| XC-021-008 | #14 `DefensiveSnapshot` / `MarkAssigner` / `MarkDirective.OffsideTrapActive` | man-mark override + offside toggle |
| XC-021-009 | #14 §3.10 anti-chaos invariants | override precedence (KD-9) |
| XC-021-010 | #15 `AttackingSnapshot` / `StyleProfile` / `OverloadDetector` | style/overload/width |
| XC-021-011 | #11 `DistributeIntent` | distribution-policy default |
| XC-021-012 | #16 `SnapshotPayload` / `SNAPSHOT_SCHEMA_VERSION` | tactics field block + bump (FR-TI-028) |
| XC-021-013 | #16 `MatchClock.IsAiStrideTick` | stride-boundary apply (FR-TI-027) |
| XC-021-014 | Match-engine design note §5 Phase D | assembly-layer population of routing fields |

## 8.2 CLAUDE.md invariant binding

| Invariant | Binding |
|---|---|
| Corner-origin coordinates | inherited; this layer adds none |
| Fatigue 0=rested/1=fatigued | not produced here; consumed convention unchanged |
| Constant tags | every constant tagged (Appendix A) — FR-TI-008 |
| Parameter-based physics (no type enums in physics) | respected — instruction enums never cross into the physics layer |
| 10 Hz / 60 Hz separation | preserved; `Tempo` never alters tick rate (FR-TI-015) |
| Interface Design Principle | no phantom interface (FR-TI-029) |
| Deterministic replay | no RNG/DateTime; stride-boundary mutation (FR-TI-026/027) |

## 8.3 Error log (ERR-021-NNN) — cross-spec amendments this spec requires

| ID | Target | Amendment | Status |
|---|---|---|---|
| ERR-021-001 | #8 §2.2.6 | replace `HasMarkDirective`/`HasAttackIntent` bool stubs path with the `TeamTactic`/`PlayerTactic` carrier | OPEN (Stage 1 / T2) |
| ERR-021-002 | #16 §3.2.4.1 / snapshot field set | reserve the tactics field block + `SNAPSHOT_SCHEMA_VERSION` bump | OPEN (T2) |
| ERR-021-003 | #14 §3.3 | document the man-mark-override request seam + KD-9 precedence | OPEN (T3) |
| ERR-021-004 | #19 §3.1.4 | register `T-TI-*` test-ID prefixes | OPEN (T0) |

All four are non-blocking for **spec approval**; they are implementation-time back-props landing at the
named stage (parallel to how #13/#14/#15 deferred their #17 channel rows).

## 8.4 Stale-reference grep

Run before each revision: grep `docs/specs/` for `#21`, `FR-TI-`, `XC-021-`, `tactical-instructions`,
and the superseded `tactics/`/`TacticalDirector.Tactics` shorthand. v0.1: the design supplement
(`docs/tracking/tactical-instruction-layer-design.md`) is the only other reference; it is marked
superseded by this spec.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-06-20 | — | XC-021-001..014, invariant binding, ERR-021-001..004, grep record. |
#endregion
