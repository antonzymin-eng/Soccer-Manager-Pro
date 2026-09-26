# Tactical Instructions Specification #21 — Section 3: Algorithms

**Created:** June 20, 2026
**Last Updated:** September 26, 2026 (v0.7 — W8 B amendment approved by owner; 35 m fallback accepted as an uncalibrated B default)
**Version:** 0.7
**Status:** APPROVED baseline (June 20, 2026); **W8 B amendment APPROVED by owner, September 26, 2026**

> Existing implemented constants cited by the pre-W8 baseline live in `TacticalInstructionsConstants.cs` (Appendix A). The W8 §3.4.1 constants are **spec-first allocations**: they deliberately do not exist in `src/` until B code lands. Owner approval was recorded September 26, 2026; their stated values and shapes are therefore normative B inputs. `[GT]` means uncalibrated, not illustrative or optional.

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

The values below are uncalibrated B defaults frozen before production wiring. "Uncalibrated" is a
status, not a source tag: every numeric constant carries exactly one project source tag.

| Constant | Source tag | Units | Value | Valid range / rationale |
|---|---|---:|---:|---|
| `GK_DIST_LOCAL_RECEIVER_RADIUS_M` | `[GT]` | m | 25.0 | [5, 40]; local release option rather than a whole-pitch search |
| `GK_DIST_FALLBACK_ADVANCE_M` | `[GT]` | m | 35.0 | [20, 60]; deterministic receiverless B target, subject to owner realism approval below |
| `GK_DIST_QUICK_DELAY_TICKS` | `[GT]` | tactical ticks @10 Hz | 5 | [1, 35]; voluntary release must remain inside §11 §3.8.4's inherited-guard budget |
| `GK_DIST_STANDARD_DELAY_TICKS` | `[GT]` | tactical ticks @10 Hz | 10 | [1, 35]; same safety envelope |
| `GK_DIST_SLOW_DELAY_TICKS` | `[GT]` | tactical ticks @10 Hz | 35 | [1, 35]; latest permitted voluntary commit |
| `GK_DIST_SLOW_POWER` | `[GT]` | dimensionless | 0.50 | [0, 1] request domain |
| `GK_DIST_QUICK_POWER` | `[GT]` | dimensionless | 0.75 | [0, 1] request domain |
| `GK_DIST_SHORT_KICK_POWER` | `[GT]` | dimensionless | 0.55 | [0, 1] request domain |
| `GK_DIST_LONG_KICK_POWER` | `[GT]` | dimensionless | 0.90 | [0, 1] request domain |
| `GK_DIST_ROLL_OUT_POWER` | `[GT]` | dimensionless | 0.55 | [0, 1] request domain |
| `GK_DIST_THROW_OUT_POWER` | `[GT]` | dimensionless | 0.70 | [0, 1] request domain |
| `GK_DIST_SPIN` | `[FIXED]` | rad/s | `Vector3.zero` | exact B identity; no distribution-spin model in this slice |
| `GK_DIST_MAX_POLICY_DELAY_TICKS` | `[DERIVED]` | tactical ticks @10 Hz | 35 | `max(Quick, Standard, Slow)`; safety ceiling, not an independent tuning value |

A later complete-engine calibration may change only the `[GT]` values through the normal
spec/config process. Stage A results do not fit or retune them.

| Policy | Delivery | Target rule | PowerIntent | Delay after claim |
|---|---|---|---:|---:|
| `SlowDown` | Roll | nearest eligible local receiver; otherwise fallback zone | `GK_DIST_SLOW_POWER` | `GK_DIST_SLOW_DELAY_TICKS` |
| `Quick` | Throw | nearest eligible local receiver; otherwise fallback zone | `GK_DIST_QUICK_POWER` | `GK_DIST_QUICK_DELAY_TICKS` |
| `ShortKick` | Kick | nearest eligible local receiver; otherwise fallback zone | `GK_DIST_SHORT_KICK_POWER` | `GK_DIST_STANDARD_DELAY_TICKS` |
| `LongKick` | Kick | receiverless fallback zone by design | `GK_DIST_LONG_KICK_POWER` | `GK_DIST_STANDARD_DELAY_TICKS` |
| `RollOut` | Roll | nearest eligible local receiver; otherwise fallback zone | `GK_DIST_ROLL_OUT_POWER` | `GK_DIST_STANDARD_DELAY_TICKS` |
| `ThrowOut` | Throw | widest eligible local receiver; otherwise fallback zone | `GK_DIST_THROW_OUT_POWER` | `GK_DIST_STANDARD_DELAY_TICKS` |

Every row uses `GK_DIST_SPIN`. The selector consumes **zero RNG draws**.

**Eligible local receiver.** Active, non-sent-off, outfield team-mate whose XY distance from the
keeper is `<= GK_DIST_LOCAL_RECEIVER_RADIUS_M`. For the nearest rule, minimize squared XY distance
(m²); exact ties choose the lower roster index.

For `ThrowOut`, use the same eligible set and maximize the absolute lateral distance in metres from
the pitch centreline, `abs(candidate.y - PitchWidthM/2)`; ties then minimize squared keeper distance,
then choose the lower roster index. No extra "wide enough" threshold exists.

**Fallback / punt zone.** Do not hard-code literal team-0/team-1 points at the call site. Derive:

```
ownGoalX = OwnGoalX(teamId)                         // [CROSS] Match Engine fixed-end convention
attackDir = AttackDirectionX(teamId)                // [CROSS] +1 for team 0, -1 for team 1 today
targetX = ownGoalX + attackDir * GK_DIST_FALLBACK_ADVANCE_M
targetY = PitchWidthM / 2                            // [CROSS] Ball Physics pitch width
fallback = (targetX, targetY, 0)
```

The current engine does **not** swap ends at half-time, so this evaluates to (35, 34, 0) for team 0
and (70, 34, 0) for team 1 on the canonical 105×68 m pitch. If end swapping is introduced, W8 must
consume the shared runtime own-goal/attack-direction helper rather than preserve the present
team-id literals. F-09 still clamps through #11. `LongKick` always uses this receiverless zone.

If a selected receiver disappears before CONTACT, #5 performs #11 F-05 at CONTACT: it clears the
receiver id and uses the committed fallback position, then F-09/safety clamping. If the receiver is
still eligible, CONTACT aims at that receiver's **live CONTACT-frame position**, not the stale
commit-time point.

**Worked selector example.** Keeper at (10, 34) m has eligible roster indices 3 at (18, 30) and 5 at
(18, 38). Both are sqrt(80) m away, so nearest-policy tie-break chooses index 3. If no candidate is
within 25.0 m, team 0 uses `0 + (+1)*35 = 35 m` and the centreline `68/2 = 34 m`, producing
(35, 34, 0). Units are metres throughout.

**Timing safety.** Delays are in #11's 10 Hz tactical-tick domain.
`GK_DIST_MAX_POLICY_DELAY_TICKS = 35` is the derived ceiling; any change to a policy delay that
raises the maximum requires revisiting #11 §3.8.4's CONTACT-before-inherited-guard proof.

**LongKick realism / owner approval.** On September 26, 2026 the owner explicitly accepted the
current `GK_DIST_FALLBACK_ADVANCE_M = 35 m` receiverless target as an **uncalibrated B default**. It is
a bounded semantic target and ordinarily remains in the keeper's own half; it is **not** approval that
realistic punt length is solved. The follow-up belongs to #5's Lofted trajectory model (current top
speed 22 m/s) together with W8's zero-spin LongKick input. After B lands, the required evidence is the
intended target versus the ball's actual first ground-contact point/distance; that measurement closes
the follow-up decision rather than changing the 35 m target inside this approval landing.

**B coverage obligation.** The frozen Stage A six-seed corpus observed `SlowDown` on every keeper
episode. The same-corpus A→B comparison therefore covers only the SlowDown row and MUST NOT be cited
as execution evidence for the other five policies. Before B merges, deterministic composed tests
must exercise each of the six enum values and lock delivery variant, delay, receiver selector or
fallback zone, power/spin mapping, and no-RNG behavior. At least one fixture per receiver-capable
policy must cover both eligible-receiver and no-eligible-receiver outcomes; `LongKick` must lock
its receiverless-by-design path.

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
| 0.4 | 2026-09-25 | — | W8 B / ERR-011-016: freezes the first six-value goalkeeper-distribution draft before wiring — deterministic receiver/zone selector, 25 m local radius, low-index tie-break, mirrored 35 m centreline fallback zone, power, zero spin, 5/10/35-tick delays and zero RNG. Values are uncalibrated `[GT]`, not Stage A fitted. |
| 0.5 | 2026-09-26 | — | W8 B review closure: gives every new numeric exactly one source tag and valid range, makes the 35-tick ceiling derived, routes fallback geometry through own-goal/attack-direction helpers instead of fixed-end literals, pins CONTACT to live receiver position with committed-position fallback, adds a selector worked example, preserves zero RNG, and records the 35 m LongKick fallback as an explicit owner-approval realism choice. |
| 0.7 | 2026-09-26 | — | W8 B amendment APPROVED by owner, September 26, 2026. Accepts `GK_DIST_FALLBACK_ADVANCE_M = 35 m` as the uncalibrated B default while explicitly leaving realistic punt length open against #5's Lofted trajectory; B landing-point evidence is the closing input. B wiring is authorized only after PR #461 merges. |
| 0.6 | 2026-09-26 | — | W8 B final consistency: corrects the inherited section preamble so spec-first W8 constants are not falsely claimed to already exist in `TacticalInstructionsConstants.cs` and `[GT]` is not mistaken for an optional illustrative value. Source implementation remains blocked on owner approval. |
#endregion
