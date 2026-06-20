# Tactical Instructions Specification #21 — Section 2: Functional Requirements, Data Structures, Failure Modes

**Created:** June 20, 2026
**Last Updated:** June 20, 2026 (v0.3 — PASS-2 fix pass)
**Version:** 0.3
**Status:** APPROVED (June 20, 2026)

---

## 2.1 Functional Requirements

Conformance per RFC 2119. Citations resolve to a KD in §1.5 or a downstream section.

| FR | Subject | Conformance | Source |
|---|---|---|---|
| FR-TI-001 | The layer is input-only: it produces no per-tick directive. `MarkDirective`/`AttackDirective`/`PressDirective`/`AgentAction` remain owned by their subsystems. | MUST | KD-1 |
| FR-TI-002 | All instruction types live in one assembly `TacticalDirector.TacticalInstructions` that references **only** `project-constants`. | MUST | KD-2 / #20 §3.5.2 |
| FR-TI-003 | The assembly MUST NOT reference any subsystem assembly (#8/#11/#12/#13/#14/#15); consumers reference it downward. | MUST | KD-2 / #20 FR-CS-046 |
| FR-TI-004 | Each instruction enum that parallels a subsystem-local enum is declared locally (`TacticPassing`/`TacticPressing`/`TacticTriggerMask`/`TacticFormation`); the consumer translates it (§3.1). No approved enum file is re-homed. | MUST | KD-2 |
| FR-TI-005 | `PlayerRole` (behavioural) is a new enum distinct from positional `RoleId` (#12); this layer never references `RoleId`. | MUST | KD-3 |
| FR-TI-006 | The data model is two-tier: one `TeamTactic` per team, one `PlayerTactic` per agent. | MUST | KD-1 / §2.2 |
| FR-TI-007 | Every enum is `byte`-backed and APPEND-only. The 14 sequential enums assert `(int)Member == N`; the 2 `[Flags]` enums (`TacticTriggerMask`, `SetPieceDutyFlags`) instead assert **bit-position** stability and the 8-flag `byte` ceiling. Each has a stability test entry. | MUST | #16 §6.2 / #1 precedent |
| FR-TI-008 | Every constant carries exactly one tag: `[GT]`, `[FIXED]`, `[DERIVED]`, or `[CROSS]`. | MUST | CLAUDE.md |
| FR-TI-009 | No `[EST]` tag remains at `APPROVED`; placeholders promote to `[GT]`/`[DERIVED]`. | MUST | #20 FR-CS-020 |
| FR-TI-010 | All constants live in one catalogue `TacticalInstructionsConstants.cs` in `#region` order Fixed→Derived→GT. | MUST | #20 FR-CS-025 |
| FR-TI-011 | `Mentality` maps deterministically to `(StyleProfile, riskMultiplier, defensiveLineBias)` per the §3.2 table; pure function, no side effects. | MUST | §3.2 / KD-11 |
| FR-TI-012 | `RoleWeightModifiers[(PlayerRole, ActionType)] → float` is applied in #8 `UtilityScorer` after the existing zone×AM×context×tactical×risk product and before the `[UTILITY_FLOOR, UTILITY_CEILING]` clamp. | MUST | §3.3 |
| FR-TI-013 | `Duty {Defend,Support,Attack}` biases positioning long-pct (#12), utility aggression (#8), and the tackle COMMIT floor (#14). | MUST | §3.4 |
| FR-TI-014 | Each `PlayerInstructions` bias (`InstrBias {Less,Default,More}`) modulates exactly its named #8 term; `Default` is the multiplicative/additive identity. | MUST | §3.4 / KD-10 |
| FR-TI-015 | `Tempo` is a **new branch** (no existing #8 hook — selection is pure max EffectiveUtility): it adds a `tempoActionBias` factor to the §3.3 utility product (forward-vs-retain per-action weighting) and an option-generation breadth bias. It MUST NOT change either loop tick rate (10 Hz / 60 Hz invariant) and is NOT a decision threshold. `Tempo.Standard` is the identity row. | MUST | §3.3 / §3.4 / KD-11 |
| FR-TI-016 | `TacticWidth`/`TacticDefWidth` feed #12's existing lateral-compactness scaling **via a new field on `ContextModifierInputs`** (the struct has no width field today); the scaling code is reused, no new positioning branch is introduced. | MUST | §3.4 / KD-11 |
| FR-TI-017 | `LineOfEngagement` scales #13 press trigger distances. | MUST | §3.4 |
| FR-TI-018 | `TacticTriggerMask` (`[Flags]`) gates which #13 triggers are active; an unset flag disables that trigger. | MUST | §3.1 |
| FR-TI-019 | `OffsideTrap` (bool) enables #14 `MarkDirective.OffsideTrapActive`. | MUST | §3.4 |
| FR-TI-020 | `TransitionWon`/`TransitionLost` override **only** the transition dimension of the `Mentality`-selected `StyleProfile` (`TransitionHoldTicks`) and the #13 counter-press gate — never the profile's other multipliers. Composition with `Mentality` is defined in §3.2. | MUST | §3.2 / §3.4 |
| FR-TI-021 | `FocusPlay` is a **new** lateral-preference branch in #8 `OptionGenerator` and a flank bias in #15 `OverloadDetector` (no existing hook — §2.3 of supplement / KD-11). | MUST | §3.3 / KD-11 |
| FR-TI-022 | `GkDistributionPolicy` sets the default fields of #11 `DistributeIntent`. | MUST | §3.4 |
| FR-TI-023 | A manager man-mark override (`PlayerInstructions.MarkTargetEntityId ≥ 0`) requests #14 force `MarkMode.ManMark` on that opponent, honoured **only within** #14's §3.10 anti-chaos invariants (safety floor wins on conflict). | MUST | KD-9 / §3.5 |
| FR-TI-024 | Routing: #8 receives tactics via `TacticalContext`; #12–#15 via new fields on their own per-tick snapshots. The match-engine Phase-D assembly layer is the sole populator. | MUST | KD-4 / §4.4 |
| FR-TI-025 | Routing fields store the **translated local** enum; the assembly layer runs translation once per tactic-change, not per agent per tick. Mechanics ticks never reference a `Tactic*` enum on the hot path. | MUST | KD-5 |
| FR-TI-026 | This layer registers no `DeterministicRngService` draw site and allocates no domain tag. | MUST | KD-6 |
| FR-TI-027 | In-match tactic changes apply only at a tactical-stride (10 Hz) tick boundary, never mid-physics-frame. | MUST | KD-7 |
| FR-TI-028 | Once match-engine Phase D serializes tactics, `TeamTactic`/`PlayerTactic`/`PlayerInstructions` enter the canonical snapshot field set with a `SNAPSHOT_SCHEMA_VERSION` bump; the field order is pinned (Appendix B) before T2. | MUST | KD-12 / #16 |
| FR-TI-029 | No interface or accessor is produced against an unspecified consumer (no phantom interfaces). | MUST | CLAUDE.md / #20 FR-CS-048 |
| FR-TI-030 | Stage-1 activation is gated on (a) the `[GT]` config-loader existing and (b) match-engine Phase C+D wiring the consumers. | MUST | KD-8 / §7 |
| FR-TI-031 | `TeamTactic.Balanced` and `PlayerTactic.Default` (with `PlayerInstructions.Default`) reproduce the current no-instruction **behaviour** exactly — realised agent trajectories, ball state, and the pre-tactics world-state fields are bit-identical. The full snapshot *payload* digest necessarily differs once FR-TI-028 adds the tactics block; identity is asserted on the world-state subset, not the full payload (see DET-002). | MUST | KD-10 |
| FR-TI-032 | Every mapping/formula in §3 includes units, valid input ranges, and at least one worked example (inline or Appendix A). | MUST | CLAUDE.md |

## 2.2 Data structures

All are Stage-1 value types per #20 §4.2. Field order in the structs below is the **canonical
snapshot order** (Appendix B) once FR-TI-028 activates.

### 2.2.1 `TeamTactic` (one per team)

| Field | Type | Notes |
|---|---|---|
| Mentality | `Mentality` | master risk dial (§3.2) |
| Formation | `TacticFormation` | translated → #12 `FormationFamily` |
| Tempo | `Tempo` | forward-vs-retain weighting (NEW branch, §3.3 — not a threshold) |
| Width | `TacticWidth` | → #12 compactness |
| Passing | `TacticPassing` | translated → #8 `PassingStyle` |
| Pressing | `TacticPressing` | translated → #8 `PressingMode` |
| LineOfEngagement | `LineOfEngagement` | → #13 trigger distances |
| DefensiveLine | `float [0,1]` | manager **input** only; assembly layer writes it (+ Mentality line bias) into the single authoritative `DefensiveLineDepth` (#12-owned). NOT a parallel depth surface — see §3.4 |
| DefensiveWidth | `TacticDefWidth` | → #12 OOP compactness |
| TransitionWon | `TransitionPlan` | → #15 / #13 |
| TransitionLost | `TransitionPlan` | → #15 / #13 |
| OffsideTrap | `bool` | → #14 `OffsideTrapActive` |
| TriggerPressMask | `TacticTriggerMask` | translated → #13 `TriggerFlags` |
| FocusPlay | `FocusPlay` | NEW branch (#8/#15) |
| GkDistribution | `GkDistributionPolicy` | → #11 `DistributeIntent` |
| TimeWasting | `byte [0..4]` | 0 = never … 4 = always |

`static TeamTactic Balanced` → Mentality.Balanced, Tempo.Standard (index 2), Width.Standard,
Passing.Mixed, Pressing.Medium, LineOfEngagement.Standard, DefensiveLine 0.5, DefensiveWidth.Standard,
both transitions = HoldShape/Regroup, OffsideTrap false, TriggerPressMask = None, FocusPlay.Mixed,
GkDistribution.SlowDown, TimeWasting 0 — reproduces today's `Stage0Default` (FR-TI-031).

### 2.2.2 `PlayerInstructions` (one per agent; all biases `Default` = follow team)

| Field | Type | Notes |
|---|---|---|
| RiskyPasses / ShootTendency / DribbleTendency / CrossTendency / PositioningFreedom / CloseDown | `InstrBias` | per-action modulation |
| TightMarking | `bool` | tighter mark distance |
| MarkTargetEntityId | `int` | −1 = none; ≥0 = man-mark request (FR-TI-023) |
| SetPieceRoles | `SetPieceDutyFlags` | `[Flags]` FreeKickTaker/CornerTaker/PenaltyTaker (Stage 1+) |

`static PlayerInstructions Default` → all biases `Default`, TightMarking false, MarkTargetEntityId −1,
SetPieceRoles None.

### 2.2.3 `PlayerTactic` (one per agent)

| Field | Type |
|---|---|
| Role | `PlayerRole` |
| Duty | `Duty` |
| Instructions | `PlayerInstructions` |

`static PlayerTactic Default(PlayerRole role)` → given role, `Duty.Support`, `PlayerInstructions.Default`.

### 2.2.4 Enum inventory (all `byte`, APPEND-only — §3.1 / Appendix A)

`Mentality`(7), `Tempo`(5), `TacticWidth`(5), `TacticDefWidth`(3), `LineOfEngagement`(5),
`TransitionPlan`(4), `GkDistributionPolicy`(6), `FocusPlay`(4), `TacticPassing`(3), `TacticPressing`(3),
`TacticTriggerMask`(`[Flags]`), `TacticFormation`(exactly the 3 #12 families today; widen with #12),
`Duty`(3), `PlayerRole`(curated subset, §3.3), `InstrBias`(3), `SetPieceDutyFlags`(`[Flags]`).
The two `[Flags]` enums use bit-position stability (FR-TI-007), not sequential ordinals.

## 2.3 Determinism notes

No RNG (FR-TI-026). All mappings (§3) are pure deterministic functions of the instruction value.
Snapshot contribution is governed by FR-TI-028. In-match mutation timing by FR-TI-027.

## 2.4 Failure modes

| ID | Condition | Detection | Recovery | Test |
|---|---|---|---|---|
| F1 | No tactic supplied at boot (null/absent) | assembly layer null check | substitute `TeamTactic.Balanced` / `PlayerTactic.Default` (identity, FR-TI-031) | T-TI-FAIL-001 |
| F2 | Man-mark target invalid (EntityId off-pitch / own team / −1 with TightMarking semantics) | range + team check at translation | ignore override; fall back to #14 computed assignment; dev-log `TI_MARK_TARGET_INVALID` | T-TI-FAIL-002 |
| F3 | Honouring the man-mark override would violate a #14 §3.10 invariant | #14 invariant cascade | safety floor wins — override demoted to `ZONAL` per KD-9 precedence | T-TI-FAIL-003 |
| F4 | In-match tactic change arrives mid-physics-frame | assembly layer guards on `MatchClock.IsAiStrideTick` | defer apply to next tactical-stride boundary (FR-TI-027) | T-TI-FAIL-004 |
| F5 | A widened `TacticPassing`/`TacticPressing` value has no exact subsystem enum target | translation map range check | clamp to nearest valid subsystem value; dev-log `TI_ENUM_CLAMP` | T-TI-FAIL-005 |

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-06-20 | — | Initial FRs (FR-TI-001..032), data structures, failure modes from supplement v0.3. |
| 0.2 | 2026-06-20 | — | PASS-1 fix pass: FR-TI-007 `[Flags]` carve-out (M-3); FR-TI-015 Tempo reclassified new branch (H-1); FR-TI-020 StyleProfile composition (H-2); `DefensiveLine` single-source note (M-2); §2.2.4 `TacticFormation` tightened (L-4). |
| 0.3 | 2026-06-20 | — | PASS-2 fix pass: FR-TI-031 reworded to world-state (not full-payload) identity vs FR-TI-028 (H-1); FR-TI-015 gains the `tempoActionBias` utility factor (M-2); FR-TI-016 names the new `ContextModifierInputs` field (M-4); §2.2.1 Tempo note updated. |
#endregion
