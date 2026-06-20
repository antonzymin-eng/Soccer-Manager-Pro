# Tactical Instructions Specification #21 — Section 3: Algorithms

**Created:** June 20, 2026
**Last Updated:** June 20, 2026 (v0.2 — PASS-1 fix pass)
**Version:** 0.2
**Status:** IN REVIEW

> All constants cited here live in `TacticalInstructionsConstants.cs` (Appendix A). Values shown are
> illustrative `[GT]` defaults pending the §5.6 balance pass; the **shapes** are normative.

---

## 3.1 Enum-translation seams (KD-2 / FR-TI-004 / FR-TI-025)

For each `Tactic*` enum that parallels a subsystem enum, a pure `static` map lives **in the consuming
assembly** (which legally references this layer downward). The match-engine assembly layer invokes it
once per tactic-change and writes the *subsystem* enum into the routing field.

| Map (lives in consumer) | Domain → Range | Clamp rule (F5) |
|---|---|---|
| `#8 TacticPassing → PassingStyle` | Short→SHORT, Mixed→MIXED, Direct→DIRECT | nearest if widened |
| `#8 TacticPressing → PressingMode` | Low→LOW, Medium→MEDIUM, High→HIGH | nearest if widened |
| `#13 TacticTriggerMask → TriggerFlags` | bitwise 1:1 by flag name | drop unknown bits |
| `#12 TacticFormation → FormationFamily` | F442→F442, F433→F433, F4231→F4231 (exactly the 3 #12 families today) | reject/clamp (F5) if a Stage-1 widening adds a family with no #12 table |

**Worked example.** `TacticPressing.High` → `PressingMode.HIGH`; if a Stage-1 widening adds
`TacticPressing.UltraHigh` with no `PressingMode` peer, F5 clamps to `HIGH`.

## 3.2 Mentality → (profile, risk, line) mapping (FR-TI-011)

`Mentality` is 7-valued; #15 ships 3 `StyleProfile` factories. The collapse is explicit and drives
**three** outputs so the gradation is not lost to the 3-way style bucket:

| Mentality | StyleProfile (#15) | riskMultiplier (×utility) | defensiveLineBias (+DefensiveLine) |
|---|---|---|---|
| VeryDefensive | Counter | 0.80 | −0.20 |
| Defensive | Counter | 0.88 | −0.12 |
| Cautious | Possession | 0.94 | −0.05 |
| Balanced | Possession | 1.00 | 0.00 |
| Positive | Possession | 1.06 | +0.05 |
| Attacking | Direct | 1.14 | +0.12 |
| VeryAttacking | Direct | 1.20 | +0.20 |

- `riskMultiplier` ∈ [0.80, 1.20], dimensionless, multiplies each scored option's utility in #8
  `UtilityScorer` (before clamp). Higher = bolder (PASS/SHOOT/DRIBBLE rise relative to HOLD).
- `defensiveLineBias` ∈ [−0.20, +0.20], added to `TeamTactic.DefensiveLine` then re-`Clamp01`'d.
- Whether Cautious/Balanced/Positive (same profile) feel distinct is an **open balance question**
  (§5.6); the risk/line spread is the gradation carrier. Values illustrative.

**Composition with `TransitionWon/Lost` (resolves PASS-1 H-2).** `Mentality` selects the **base**
`StyleProfile`. `TransitionWon`/`TransitionLost` (FR-TI-020) then **override only the transition
dimension** of that profile — `StyleProfile.TransitionHoldTicks` and the #13 counter-press gate — never
its `DepthMult`/`SupportMult`/`MaxRunners`. So the two inputs are composed, not competing:
`profile = Mentality→base`, then `profile.TransitionHoldTicks = TransitionPlan→ticks`.

**Worked example.** `Attacking` → profile `Direct`, options scored ×1.14, `DefensiveLine` 0.50 → 0.62.

## 3.3 Role → utility-weight model (FR-TI-012 / FR-TI-021 — NEW logic, KD-11)

`RoleWeightModifiers : (PlayerRole, ActionType) → float`, a static table (Appendix A) applied in #8
`UtilityScorer.ComputeUtility` **after** the existing `zone × AM × context × tactical × risk` product
and **before** the `[UTILITY_FLOOR, UTILITY_CEILING]` clamp:

```
utility' = clamp( utility × RoleWeightModifiers[role, opt.Type] × mentalityRiskMult
                  × dutyBias[duty, opt.Type] × instrBias[instructions, opt.Type],
                  UTILITY_FLOOR, UTILITY_CEILING )
```

- Default role row = all 1.0 (identity; FR-TI-031). A Poacher row raises SHOOT (e.g. 1.25), lowers HOLD
  (0.80); a Deep-Lying Playmaker raises PASS, lowers DRIBBLE; a Ball-Winning Mid raises PRESS/INTERCEPT.
- **`FocusPlay`** and **`Tempo`** have no existing hook (verified: #8 selection is pure max
  EffectiveUtility in `ActionSelector` — there is no decision threshold to bias). Both are **new
  branches** (KD-11): `FocusPlay` adds a lateral-preference term in `OptionGenerator` + a flank bias in
  #15 `OverloadDetector`; `Tempo` adds a forward-vs-retain weighting (raise PASS/SHOOT relative to HOLD,
  widen option-generation breadth) — **not** a threshold. Reviewed in §5.6.
- All four multiplicative factors default to 1.0, so a default tactic is exactly today's behaviour.

**Worked example.** Poacher, ActionType SHOOT, attacking third: `utility × 1.25 × 1.14(Attacking) ×
1.0(Attack duty SHOOT) × 1.0(no instr) `, then clamp.

## 3.4 Direct-input instructions (resolve into existing tunables; FR-TI-013..020, 022)

| Instruction | Target tunable (existing) | Transform |
|---|---|---|
| `Duty` | #12 long-pct; #8 risk bias; #14 COMMIT floor | Defend −Δ fore, Attack +Δ fore; aggression ±0.05 |
| `InstrBias` (per action) | the matching #8 term | Less ×0.85, Default ×1.0, More ×1.15 |
| `Tempo` | **NEW branch** (no existing hook — §3.3) | forward-vs-retain utility weighting; NOT tick rate, NOT a threshold |
| `TacticWidth`/`TacticDefWidth` | **new field** on #12 `ContextModifierInputs`, feeding the existing compactness scaling | map 5/3 steps → scalar [0.85..1.15] |
| `LineOfEngagement` | #13 trigger distances (existing) | scalar [0.80..1.20] on trigger radius |
| `OffsideTrap` | #14 `MarkDirective.OffsideTrapActive` | bool passthrough |
| `TransitionWon/Lost` | #15 `StyleProfile.TransitionHoldTicks`; #13 counter-press gate | enum select |
| `GkDistributionPolicy` | #11 `DistributeIntent` defaults | enum → (DeliveryKind, target, power) defaults |

**`DefensiveLine` single-source (resolves PASS-1 M-2).** `TeamTactic.DefensiveLine` is the manager-set
**input** only; it is **not** a parallel depth value. The assembly layer writes
`DefensiveLineDepth = Clamp01(TeamTactic.DefensiveLine + MentalityLineBias[mentality])` into the **single
authoritative** `DefensiveLineDepth` field that #8 (`TacticalContext`) and #14 (`DefensiveSnapshot`,
sourced from #12 per FR-DA-012) already read. #12 remains the depth authority; this layer never creates
a second depth surface.

Each transform is a pure function with a default that is the identity (Default/Standard/Mixed →
unchanged), satisfying FR-TI-031.

## 3.5 Man-mark override precedence (FR-TI-023 / KD-9)

```
if instructions.MarkTargetEntityId >= 0 and target is valid (F2):
    request MarkMode.ManMark on target for this agent
#14 then runs its §3.10 anti-chaos cascade UNCHANGED:
    if the override breaches MinBacklineAgents / MaxManMarkAssignments / MaxMarkDisplacement:
        demote this override to ZONAL (safety floor wins, F3)
```

The override is a **request**, never a guarantee — #14 remains the adjudicator. This is the deliberate
KD-9 precedence, not a limitation to be "fixed."

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-06-20 | — | Translation seams, mentality table, role-weight model, direct-input transforms, man-mark precedence. |
| 0.2 | 2026-06-20 | — | PASS-1 fix pass: §3.2 Mentality/Transition composition (H-2); §3.3+§3.4 Tempo reclassified new branch (H-1); §3.4 Width relabelled new-field-feeds-existing (M-1) + `DefensiveLine` single-source (M-2); §3.1 `TacticFormation` 3-family clamp (L-4). |
#endregion
