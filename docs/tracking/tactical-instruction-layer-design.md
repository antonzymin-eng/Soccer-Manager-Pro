# Tactical Instruction Layer — Design Supplement

> **Created:** June 20, 2026
> **Last Updated:** June 20, 2026 (v0.1 — initial draft for review)
> **Status:** DESIGN SUPPLEMENT (forward-looking; **NOT** a formal approved spec, **NOT** yet implemented).
> Targets Stage 1 implementation, sequenced against the match-engine Phase C–F roadmap
> (`match-engine-design.md`). No code is authored from this note until it is reviewed and approved.
> **Author:** —
> **Purpose:** Define the manager-facing tactical instruction layer (formation → mentality →
> team instructions → player roles & duties → individual instructions) that drives the already-built
> AI subsystems (#8 Decision Tree, #12 Positioning, #13 Pressing, #14 Defensive, #15 Attacking),
> and enumerate the concrete variables, constructs, and functions that must be added to support it.

---

## 0. Scope and governance

This supplement covers the **input layer** that a human (or AI) manager sets: how a chosen tactic
is expressed as data and routed into the existing Mechanics/AI subsystems. It does **not** redesign
those subsystems — they already produce the emergent behaviours (marking modes, press roles,
attacking runs, formation slots). The gap this note addresses is that **nothing feeds them a manager's
intent**: `TacticalContext` carries only `Pressing` (3 levels), `Passing` (3 levels),
`DefensiveLineDepth`, a formation slot, and two `bool` stubs.

This note introduces **no spec #21 yet**. It is a candidate for promotion to a formal spec once
reviewed. Until then it is governance scaffolding parallel to `match-engine-design.md`.

**Hard prerequisites (this layer cannot be runtime-driven until these land):**

1. **The `[GT]` config-loader mechanism** (`src/CLAUDE.md` "WHAT IS NOT HERE YET"): every instruction
   default is currently a hardcoded `const` with `// TODO: replace with config loader`. Manager-set
   values have nowhere to be injected until this exists. **This layer's runtime activation is gated on it.**
2. **Match-engine Phase C (Resolve) and Phase D (AI)**: the consumers (executors, decision tree,
   the four Mechanics ticks) are EventBus-lifecycle-only stubs today. Instructions have nothing to
   drive until those phases wire the subsystems into the live tick.

Read `CLAUDE.md`, `src/CLAUDE.md`, and `match-engine-design.md` first. Every rule there applies
(zero-alloc hot path, constructor injection, struct state by `ref`, no static mutable singleton,
deterministic time via `MatchClock`, ordinal-stability on every new enum).

---

## 1. What already exists vs. what this adds

The tactical *vocabulary of outputs* is largely built. The *input that selects among them* is not.

| Tactical concern | Existing construct (output/mechanism) | This note adds (input) |
|---|---|---|
| Phase model | `Phase {InPoss/OutOfPoss/TransToAtk/TransToDef}` (#12) | — (consume as-is) |
| Pressing intensity | `PressingMode {HIGH/MEDIUM/LOW}` (#8) | finer scale + line-of-engagement |
| Passing directness | `PassingStyle {DIRECT/MIXED/SHORT}` (#8) | — (possibly widen) |
| Defensive line height | `TacticalContext.DefensiveLineDepth` (float) | — (consume as-is) |
| Offside trap / step-up | `MarkDirective.OffsideTrapActive/StepUpTargetDepth` (#14) | instruction toggle → directive |
| Press triggers | `TriggerFlags {BadTouch/BackwardPass/SidelineTrap/WeakReceiver}` (#13) | instruction enable/disable mask |
| Attacking style / transition | `StyleProfile {Possession/Direct/Counter}` (#15) | mentality + transition instruction → profile |
| Marking orientation | `MarkMode {Zonal/ManMark/InterceptRunner/CoverGkZone}` + `MarkAssignment.TargetEntityId` (#14) | manager override path (force man-mark / mark target) |
| Formation | `FormationFamily {F442/F433/F4231}` + 13×4 pull-factor table (#12) | more families + behavioural-role offsets |
| **Mentality** | none | `Mentality` enum + global risk multiplier |
| **Tempo** | none | `Tempo` field + decision-threshold consumer |
| **Width (attacking/def)** | compactness *mechanism* in `ContextModifier` (#12) | `Width` input variable |
| **Line of engagement** | press trigger distances (#13) | `LineOfEngagement` variable |
| **Behavioural roles** | positional `RoleId` only (#12) | `PlayerRole` enum + role→weight table |
| **Duty** | none (CB Stopper/Cover partial) | `Duty {Defend/Support/Attack}` enum |
| **Individual instructions** | none | `PlayerInstructions` struct |
| **In-possession granular** (overlap, focus play, work-ball-into-box, cross type, play-out) | partial (`OverloadFlank`, `WidthHolder`) | instruction fields + logic |
| **GK distribution policy** | `DistributeIntent` mechanic (#11) | distribution-policy instruction |
| **Time-wasting / game state** | none | game-management instruction |

---

## 2. Architectural decisions

### 2.1 A new infrastructure assembly `tactics/` (`TacticalDirector.Tactics`)

Holds the **pure data types** of the instruction layer (enums + the aggregate structs). It sits below
the AI/Mechanics layers in the reference graph — `decision-tree`, `positioning-ai`, `pressing-ai`,
`defensive-ai`, `attacking-ai` reference it; it references only `project-constants` (and possibly
`agent-movement` for `RoleId` reuse — see 2.4). It contains **no game-loop logic**, so it does not
violate any layer ban. Rationale: the instruction set is consumed by five assemblies across two layers,
so it cannot live inside any one of them without creating an illegal cross-layer reference.

> **Alternative considered:** extend `TacticalContext` in-place in `decision-tree`. Rejected — the
> Mechanics ticks (#12–#15) would then have to reference the AI-layer `decision-tree` assembly to read
> instructions, which inverts the `Physics ← Mechanics ← AI` direction (FR-CS-046). A shared low assembly
> is the only placement that keeps the reference graph legal.

### 2.2 Two-tier data model: `TeamTactic` (team) + `PlayerTactic` (per-agent)

- **`TeamTactic`** — one per team per match: mentality, the phase-split team instructions, formation
  selection. Immutable for the match unless the manager changes it (touchline shout / half-time).
- **`PlayerTactic`** — one per agent: `PlayerRole`, `Duty`, and a `PlayerInstructions` override block.

Both are **input** types. They are distinct from the existing **output** directives
(`MarkDirective`, `AttackDirective`, `PressDirective`, `AgentAction`) the subsystems already produce
each tick. The flow is: `TeamTactic`/`PlayerTactic` → (config-loader resolves `[GT]` values) →
subsystem tick inputs → per-tick directives → executors.

### 2.3 Instructions are resolved into existing weight/threshold inputs, not new branches

Wherever possible an instruction maps onto an **existing** tunable — a `[GT]` constant, a directive
field, or a utility weight — rather than a new code path. Mentality scales utility weights; width feeds
`ContextModifier`'s compactness; line-of-engagement feeds press trigger distances; the role→weight
table multiplies `UtilityWeights`. This keeps the additive surface small and avoids re-deriving the
adversarially-reviewed scoring math.

### 2.4 `PlayerRole` (behavioural) is distinct from `RoleId` (positional)

`RoleId {GK..ST}` (#12) is a **position** (row index into the formation pull-factor table). FM-style
roles (Poacher, Mezzala, Ball-Playing Defender, Deep-Lying Playmaker, Inside Forward, Target Man, …)
are **behavioural modifiers** layered on a position. The two must not be merged: a single position can
host several behavioural roles, and a behavioural role changes utility weights + positioning offsets,
not the slot identity. `PlayerRole` is therefore a new enum; `RoleId` is unchanged.

---

## 3. New types (the concrete gap)

> All enums are `byte`-backed and **APPEND-only** (ordinal stability — every new enum in this project
> carries the standard paragraph; ordinals feed snapshots/digests once Phase D serializes tactics).
> Constant **values** below are illustrative defaults for review, not pinned `[GT]` values.

### 3.1 Team-level enums (`tactics/`)

| Type | Members (draft) | Maps to / consumed by |
|---|---|---|
| `Mentality` | VeryDefensive, Defensive, Cautious, Balanced, Positive, Attacking, VeryAttacking | global utility risk multiplier in `UtilityScorer`; `StyleProfile` selection (#15); `DefensiveLineDepth` bias |
| `Tempo` | Lowest…Highest (5) | decision/PASS thresholds in `OptionGenerator`/`UtilityScorer` (NOT tick rate) |
| `TeamWidth` | VeryNarrow…VeryWide (5) | `ContextModifierInputs` lateral scaling (#12 `ContextModifier`) |
| `LineOfEngagement` | Lowest…Highest (5) | press trigger distances (#13 `TriggerEvaluator`) |
| `DefensiveWidth` | Narrow/Standard/Wide | `ContextModifier` out-of-possession compactness |
| `TransitionPlan` | CounterPress/Regroup (lost); Counter/HoldShape (won) | `StyleProfile.TransitionHoldTicks` (#15); pressing counter-press gate (#13) |
| `GkDistributionPolicy` | DistributeQuick, SlowDown, ToCentreBacks, ToFullBacks, ToTarget, LongClear | `DistributeIntent` defaults (#11) |
| `FocusPlay` | Left, RightThroughMiddle, Mixed | `OverloadDetector` flank bias (#15); option generation lateral bias (#8) |

Plus widening candidates (not strictly new types): `FormationFamily` (add common shapes —
F352/F532/F4141/F4411/F3421), and optionally finer `PressingMode`/`PassingStyle` step counts.

### 3.2 Player-level enums (`tactics/`)

| Type | Members (draft) | Maps to / consumed by |
|---|---|---|
| `Duty` | Defend, Support, Attack | positioning long-pct bias (#12); utility aggression bias (#8); tackle COMMIT floor (#14) |
| `PlayerRole` | a curated Stage-1 subset (e.g. BallPlayingDefender, NoNonsenseCB, WingBack, DeepLyingPlaymaker, BoxToBox, Mezzala, BallWinningMid, AdvancedPlaymaker, InsideForward, Winger, TargetMan, Poacher, CompleteForward, …) | role→weight-modifier table; positioning offset table |

### 3.3 Aggregate structs (`tactics/`)

```
TeamTactic            // input, one per team
  Mentality           Mentality
  Formation           FormationFamily
  Tempo               Tempo
  Width               TeamWidth
  Passing             PassingStyle        // reuse #8 enum
  Pressing            PressingMode        // reuse #8 enum
  LineOfEngagement    LineOfEngagement
  DefensiveLine       float [0,1]         // reuse DefensiveLineDepth semantics
  DefensiveWidth      DefensiveWidth
  TransitionWon       TransitionPlan
  TransitionLost      TransitionPlan
  OffsideTrap         bool                // → MarkDirective.OffsideTrapActive (#14)
  TriggerPressMask    TriggerFlags        // reuse #13 enum (which triggers enabled)
  FocusPlay           FocusPlay
  GkDistribution      GkDistributionPolicy
  TimeWasting         byte [0..N]         // game-management dial

PlayerInstructions    // input, per agent (all "Default = follow team")
  RiskyPasses         InstrBias {Less,Default,More}
  ShootTendency       InstrBias
  DribbleTendency     InstrBias
  CrossTendency       InstrBias
  PositioningFreedom  InstrBias           // hold position … roam
  CloseDown           InstrBias
  TightMarking        bool
  MarkTargetEntityId  int                 // -1 = none → forces #14 ManMark on a specific opponent
  Forward/FreeKick/Corner/PenaltyTaker    bool (set-piece duties; Stage 1+)

PlayerTactic          // input, per agent
  Role                PlayerRole
  Duty                Duty
  Instructions        PlayerInstructions
```

`InstrBias` is a shared 3-state `byte` enum (Less/Default/More) used by every individual instruction.

### 3.4 The role→weight-modifier table (the load-bearing new construct)

A static lookup `RoleWeightModifiers` keyed by `(PlayerRole, ActionType)` → multiplier, applied in
`UtilityScorer` after the existing zone×AM×context×tactical×risk product. This is what makes a Poacher
shoot more and hold less, a Deep-Lying Playmaker prefer PASS, a Mezzala drift and carry. It is the
single highest-value addition and the one most needing design review (balance + adversarial pass), so
it is called out separately. Structurally it parallels the existing `TacticalWeights` /
`UtilityWeights` catalogues and lives in `tactics/TacticsConstants.cs`.

---

## 4. Functions / wiring changes (per consuming subsystem)

| Subsystem | Change needed |
|---|---|
| **#8 Decision Tree** | `UtilityScorer`: apply (a) `Mentality` global risk multiplier, (b) `RoleWeightModifiers[Role, type]`, (c) `Duty` aggression bias, (d) `PlayerInstructions` per-action biases. `OptionGenerator`: `Tempo`/`FocusPlay` bias on option generation; `Width` bias on MOVE target generation. `TacticalContext`: replace the two `bool` stubs with real `TeamTactic`/`PlayerTactic` references (or carry them alongside). |
| **#12 Positioning** | `ContextModifierInputs`: feed `Width`/`DefensiveWidth` into lateral/vertical compactness. Add a `PlayerRole`/`Duty` positioning-offset table parallel to the pull-factor table (role pulls the slot fore/aft/wide). Expand `FormationFamily` tables. |
| **#13 Pressing** | `TriggerEvaluator`: gate each trigger on `TeamTactic.TriggerPressMask`; scale trigger distances by `LineOfEngagement` and `PressingMode`. Counter-press gate from `TransitionLost`. |
| **#14 Defensive** | `MarkAssigner`: honour a manager `ManMark`/`MarkTargetEntityId` override (force `MarkMode.ManMark` on the named opponent, bypassing the computed assignment within the anti-chaos invariants). `OffsideTrapController`: enable from `TeamTactic.OffsideTrap`. `TackleIntentEvaluator`: `CloseDown`/`Duty` → COMMIT floor + jockey angle. |
| **#15 Attacking** | `RoleAssigner`/`StyleProfile` selection from `Mentality` + `TransitionWon`. `OverloadDetector`: bias from `FocusPlay`. `WidthHolder`: from `TeamWidth`. |
| **#11 Goalkeeper** | `GoalkeeperDistribution`: default `DistributeIntent` from `GkDistributionPolicy`. |
| **Config loader (prereq)** | A `TacticLoader` (or the general `[GT]` config loader) that materializes a `TeamTactic`/`PlayerTactic[]` at boot and on in-match change, then resolves the `[GT]` constants the instructions select. |

---

## 5. Sequencing (slots into the match-engine roadmap)

This layer is **additive and staged** — it lands behind the integration phases so each piece has a
live consumer the moment it's written (avoids the phantom-interface anti-pattern, ERR-001/ERR-004).

- **T0 — low-risk scaffolding (can land now, no live consumer required):** the `tactics/` assembly +
  pure enums (`Mentality`, `Duty`, `PlayerRole`, `Tempo`, `TeamWidth`, `LineOfEngagement`, `InstrBias`,
  `TransitionPlan`, `GkDistributionPolicy`, `FocusPlay`) and the `TeamTactic`/`PlayerTactic`/
  `PlayerInstructions` structs with `Default`/`Balanced` factories. No behaviour change; types only.
  Each enum gets the ordinal-stability paragraph + an `EnumOrdinalStabilityTests` entry.
- **T1 — config loader (prereq):** the `[GT]` loader + `TacticLoader`. Unblocks runtime injection.
- **T2 — with match-engine Phase D (AI):** wire `TeamTactic`/`PlayerTactic` through `TacticalContext`
  into `UtilityScorer` + `OptionGenerator`; implement `Mentality` multiplier and the
  `RoleWeightModifiers` table (its own adversarial + balance review — highest risk).
- **T3 — Mechanics consumers:** #12 width/role offsets, #13 trigger mask + line-of-engagement, #14
  man-mark override + offside toggle, #15 style/overload. Each lands as its Mechanics tick is wired.
- **T4 — polish:** GK distribution policy, in-possession granular instructions, time-wasting,
  expanded formation families, set-piece duties.

Each stage is testable in isolation (unit on the resolver math) and end-to-end through the #19
`ScenarioRunner` once the AI phase composes (the existing closed-loop harness is the verification path).

---

## 6. Risks / open questions for review

1. **Snapshot schema impact.** Once Phase D serializes per-agent tactics into the match snapshot,
   `PlayerTactic`/`PlayerInstructions` become digest-load-bearing cross-tick state and must be added
   to the canonical field set + a `SNAPSHOT_SCHEMA_VERSION` bump. Decide field order **before** T2 to
   avoid a later bump (the match-engine note already flags schema-churn sensitivity).
2. **`RoleWeightModifiers` balance.** This table is where "tactics feel real" lives and where balance
   bugs hide. Needs the same adversarial + numerical-mirror rigor the scoring specs got.
3. **Determinism of in-match tactic changes.** A touchline shout mutates `TeamTactic` mid-match; the
   change must be applied at a deterministic tick boundary (tactical 10 Hz stride), not mid-physics-frame,
   or replay diverges. Define the apply-point with the match-engine owner.
4. **`PlayerRole` roster size.** FM ships dozens; Stage 1 should curate a defensible subset and append
   later. Picking the subset is a design call.
5. **Man-mark override vs. anti-chaos invariants.** A forced man-mark must still respect #14's
   `MinBacklineAgents` / `MaxManMarkAssignments` floors. Define precedence (manager intent vs. safety floor).
6. **Promotion to a formal spec.** If approved, this becomes spec #21 with the full template
   (§1–§9 + approval checklist) and a `tactics/` folder under `docs/specs/`.

---

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-06-20 | — | Initial draft. Cross-checked FM tactical taxonomy (team instructions / roles+duties / individual instructions) against the implemented AI subsystems; enumerated missing enums, structs, the role→weight table, and per-subsystem wiring; sequenced against match-engine Phase C–F. Not yet reviewed or approved; no code authored. |
#endregion
