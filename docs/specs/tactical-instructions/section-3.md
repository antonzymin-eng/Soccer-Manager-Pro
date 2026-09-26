# Tactical Instructions Specification #21 — Section 3: Algorithms

**Created:** June 20, 2026
**Last Updated:** September 25, 2026 (v0.4 — W8 B goalkeeper-distribution map frozen before wiring)
**Version:** 0.4
**Status:** APPROVED (June 20, 2026)

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
- Whether Cautious/Balanced/Positive (same profile) feel distinct is a §5.6 balance question; the
  risk/line spread is the gradation carrier. **Values PINNED by the §5.6 / G2 balance pass (2026-06-30):**
  the table is strictly monotonic in both `riskMultiplier` and `defensiveLineBias` with the Balanced
  identity row exact (1.00 / 0.00), so the 7-step gradation is preserved and a default tactic is
  behaviour-neutral (FR-TI-031). Locked in code by `BalancePassInvariantsTests`.

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
                  × dutyBias[duty, opt.Type] × instrBias[instructions, opt.Type]
                  × tempoActionBias[tempo, opt.Type],
                  UTILITY_FLOOR, UTILITY_CEILING )
```

- Default role row = all 1.0 (identity; FR-TI-031). A Poacher row raises SHOOT (e.g. 1.25), lowers HOLD
  (0.80); a Deep-Lying Playmaker raises PASS, lowers DRIBBLE; a Ball-Winning Mid raises PRESS/INTERCEPT.
- **`Tempo`** acts in **two** places (both new, KD-11): (a) `tempoActionBias[tempo, opt.Type]` above — a
  per-action forward-vs-retain weighting (raises PASS/SHOOT relative to HOLD), and (b) an
  option-generation **breadth** widening in `OptionGenerator`. It is NOT a tick-rate or threshold change.
- **`FocusPlay`** has no existing hook (verified: #8 selection is pure max EffectiveUtility in
  `ActionSelector`): it adds a lateral-preference term in `OptionGenerator` + a flank bias in #15
  `OverloadDetector`. Reviewed in §5.6.
- All **five** multiplicative factors default to 1.0 (Tempo.Standard / Balanced / Default rows = identity),
  so a default tactic is exactly today's behaviour.

**Worked example.** Poacher, ActionType SHOOT, attacking third, Standard tempo: `utility × 1.25(role) ×
1.14(Attacking) × 1.0(Attack duty SHOOT) × 1.0(no instr) × 1.0(Standard tempo)`, then clamp.

## 3.4 Direct-input instructions (resolve into existing tunables; FR-TI-013..020, 022)

| Instruction | Target tunable (existing) | Transform |
|---|---|---|
| `Duty` | #12 long-pct; #8 risk bias; #14 COMMIT floor | Defend −Δ fore, Attack +Δ fore; aggression ±0.05 |
| `InstrBias` (per action) | the matching #8 term | Less ×0.85, Default ×1.0, More ×1.15 |
| `Tempo` | **NEW branch** (no existing hook — §3.3): `tempoActionBias` factor in the §3.3 product + option-gen breadth | forward-vs-retain per-action weighting; NOT tick rate, NOT a threshold |
| `TacticWidth`/`TacticDefWidth` | **new field** on #12 `ContextModifierInputs`, feeding the existing compactness scaling | map 5/3 steps → scalar [0.85..1.15] |
| `LineOfEngagement` | #13 trigger distances (existing) | scalar [0.80..1.20] on trigger radius |
| `OffsideTrap` | #14 `MarkDirective.OffsideTrapActive` | bool passthrough |
| `TransitionWon/Lost` | #15 `StyleProfile.TransitionHoldTicks`; #13 counter-press gate | enum select |
| `GkDistributionPolicy` | #11 `DistributeIntent` defaults | exact W8 B map in §3.4.1 |

### 3.4.1 Goalkeeper distribution policy — W8 B (FR-TI-022)

These values are **[GT][UNCALIBRATED]** B defaults frozen before production wiring. They are
semantic initial values, not a fit to the Stage A corpus. A later complete-engine calibration may
change them only through the normal spec/config process.

| Policy | Delivery | Target rule | PowerIntent | Delay after claim |
|---|---|---|---:|---:|
| `SlowDown` | Roll | nearest eligible local receiver; otherwise fallback zone | 0.50 | 35 tactical ticks |
| `Quick` | Throw | nearest eligible local receiver; otherwise fallback zone | 0.75 | 5 tactical ticks |
| `ShortKick` | Kick | nearest eligible local receiver; otherwise fallback zone | 0.55 | 10 tactical ticks |
| `LongKick` | Kick | receiverless fallback zone by design | 0.90 | 10 tactical ticks |
| `RollOut` | Roll | nearest eligible local receiver; otherwise fallback zone | 0.55 | 10 tactical ticks |
| `ThrowOut` | Throw | widest eligible local receiver; otherwise fallback zone | 0.70 | 10 tactical ticks |

For every row, `SpinIntent = Vector3.zero` in B. The selector consumes **zero RNG draws**.

**Eligible local receiver.** Active, non-sent-off, outfield team-mate within **25.0 m inclusive**
of the keeper in XY. This is the pre-A dry-selector candidate promoted without using Stage A outcomes.
For the nearest rule, minimize squared XY distance; exact ties choose the lower roster index.

For `ThrowOut`, use the same eligible set and maximize
`abs(candidate.y - PITCH_WIDTH_M / 2)`; ties then minimize squared keeper distance, then choose
the lower roster index. No extra “wide enough” threshold exists.

**Fallback / punt zone.** The deterministic receiverless point is on the pitch centreline, 35 m from
the keeper's own goal line: team 0 `(35, 34, 0)`, team 1 `(70, 34, 0)` on the canonical 105×68 m
pitch. F-09 still clamps through #11. `LongKick` always uses this zone. If a selected receiver
disappears before CONTACT, #11 F-05 converts to a receiverless execution and preserves the selected
delivery kind.

**Timing safety.** Delays are in #11's 10 Hz tactical-tick domain. No B policy may exceed **35
tactical ticks** without revisiting #11 §3.8.4's CONTACT-before-inherited-guard proof.

**`DefensiveLine` single-source (resolves PASS-1 M-2).** `TeamTactic.DefensiveLine` is the manager-set
**input dial** only; it is **not** a parallel depth value. Each tick the assembly layer **recomputes**
`DefensiveLineDepth = Clamp01(TeamTactic.DefensiveLine + MentalityLineBias[mentality])` and writes it
into the **single authoritative** `DefensiveLineDepth` field that #8 (`TacticalContext`) and #14
(`DefensiveSnapshot`, sourced from #12 per FR-DA-012) read. #12 remains the depth authority.
**Serialization (no divergence-on-restore):** only the input dial `TeamTactic.DefensiveLine` is part of
this layer's snapshot block (Appendix B); the resolved `DefensiveLineDepth` is a derived value
recomputed every tick from the dial + mentality, so it is never an independently-restorable second
surface that could diverge from the dial on load.

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
| 0.3 | 2026-06-20 | — | PASS-2 fix pass: §3.3 product gains the fifth factor `tempoActionBias` (M-2); §3.4 `DefensiveLine` serialization pinned to the input dial, resolved depth recomputed each tick (M-1). |
| 0.4 | 2026-09-25 | — | W8 B / ERR-011-016: freezes the total six-value goalkeeper-distribution map before wiring — deterministic receiver/zone selector, 25 m local radius, low-index tie-break, mirrored 35 m centreline fallback zone, power, zero spin, 5/10/35-tick delays and zero RNG. Values are uncalibrated `[GT]`, not Stage A fitted. |
#endregion
