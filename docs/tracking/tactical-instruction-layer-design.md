# Tactical Instruction Layer — Design Supplement

> **Created:** June 20, 2026
> **Last Updated:** June 20, 2026 (v0.3 — second adversarial fix pass: "no migration" claim narrowed to
> enum re-homing, translate-once snapshot rule pinned, FocusPlay/RoleWeightModifiers flagged as new
> branches, mentality-collapse softened to open balance question; all supplement code references
> fact-checked against source)
> **SUPERSEDED June 20, 2026** — promoted to formal **Spec #21 (Tactical Instructions)** at
> `docs/specs/tactical-instructions/` (status IN REVIEW; see `SPEC_INDEX.md` row 21). This note is
> retained for history; the spec section files are authoritative. Folder/assembly naming reconciled
> there (`tactical-instructions/` / `TacticalDirector.TacticalInstructions`).
>
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
reviewed. It is **not** itself spec-template-complete — promotion requires adding a §5 test plan, a §9
approval checklist, and FR-IDs (see §6.6); until then it is governance scaffolding parallel to
`match-engine-design.md` and is reviewed as a design note, not as a spec.

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

### 2.1 A new bottom-layer assembly `tactics/` (`TacticalDirector.Tactics`)

Holds the **pure data types** of the instruction layer (enums + the aggregate structs). It sits at the
**bottom** of the reference graph: it references **only** `project-constants`. The five consumers
(`decision-tree`, `positioning-ai`, `pressing-ai`, `defensive-ai`, `attacking-ai`) reference *it*.
This is the only placement that keeps the `Physics ← Mechanics ← AI` graph legal — the instruction set
is read by five assemblies across two layers, so it cannot live in any one of them.

**Enum-ownership rule (resolves the layering hazard).** `tactics/` MUST NOT reference any subsystem
assembly, so it **cannot reuse** enums that live in higher assemblies — `PassingStyle`/`PressingMode`
(in `decision-tree`, AI layer), `TriggerFlags` (in `pressing-ai`, Mechanics), `RoleId` (in
`positioning-ai`, Mechanics). Therefore:

- `tactics/` declares its **own** instruction enums for every field of `TeamTactic`/`PlayerTactic`,
  including instruction-side analogues where a subsystem already has a local enum
  (`TacticPassing`, `TacticPressing`, `TacticTriggerMask`).
- Each consumer **translates** the `tactics`-owned enum into its own local enum at the top of its tick
  (a small pure `static` map function in the consuming assembly — which legally references `tactics/`
  downward). The subsystem-owned enums are left untouched; **no approved file is migrated.**
- The translation maps are the explicit seam, not an implicit "reuse." This trades a few tiny mapping
  functions for a clean acyclic graph and **zero edits to any approved enum file**. Note this avoids
  *enum re-homing only* — the routing in §2.5/§4 still adds fields to four approved snapshot structs and
  to `TacticalContext`; that is unavoidable and is the bounded edit surface this layer accepts.

> **Alternative considered & rejected:** make `tactics/` the canonical owner and have #8/#13/#12 mirror
> its enums. Rejected — that re-homes enums embedded in approved, adversarially-reviewed event payloads
> and hash inputs (e.g. `PassType`/ordinal-stability contracts), a high-risk migration for no
> behavioural gain. Translation at the consumer is cheaper and reversible.

`tactics` is **infrastructure-only** (no game-loop logic), like `project-constants`.

### 2.2 Two-tier data model: `TeamTactic` (team) + `PlayerTactic` (per-agent)

- **`TeamTactic`** — one per team per match: mentality, the phase-split team instructions, formation
  selection. Immutable for the match unless the manager changes it (touchline shout / half-time; see §6.3).
- **`PlayerTactic`** — one per agent: `PlayerRole`, `Duty`, and a `PlayerInstructions` override block.

Both are **input** types, distinct from the existing **output** directives (`MarkDirective`,
`AttackDirective`, `PressDirective`, `AgentAction`) the subsystems produce each tick.

### 2.3 Instructions resolve into existing weight/threshold inputs, not new branches

Wherever possible an instruction maps onto an **existing** tunable — a `[GT]` constant, a directive
field, or a utility weight — rather than a new code path. Mentality scales utility weights; width feeds
`ContextModifier` compactness; line-of-engagement feeds press trigger distances; the role→weight table
multiplies `UtilityWeights`. Small additive surface; the reviewed scoring math is not re-derived.

**Exception — instructions that require genuinely new logic** (not just a value fed to an existing
input): `FocusPlay` has no existing hook (`OptionGenerator` generates options from perceived geometry +
`AgentFacingDirection`, not from a directional preference), and the `RoleWeightModifiers` table (§3.4)
is a new multiply stage. These are flagged as **new branches** in §4, not "maps to," and carry the
heavier review burden (§6.2). Everything else resolves into an existing input.

### 2.4 `PlayerRole` (behavioural) is distinct from `RoleId` (positional)

`RoleId {GK..ST}` — owned by `positioning-ai`, a **position** (row index into the formation pull-factor
table). FM-style roles (Poacher, Mezzala, Ball-Playing Defender, …) are **behavioural modifiers** on a
position. `tactics/` declares a **new** `PlayerRole` enum; it does **not** reference or reuse `RoleId`.
The position an agent occupies continues to travel as it does today (formation slot index); `PlayerRole`
is layered on top and changes weights + positioning offsets, never the slot identity.

### 2.5 Routing: how a tactic reaches each consumer (the match-engine assembly layer owns distribution)

The Mechanics ticks (#12–#15) do **not** read the AI-layer `TacticalContext`; they each consume their
own per-tick snapshot (`PositioningPerceptionSnapshot`, `PressingSnapshot`, `DefensiveSnapshot`,
`AttackingSnapshot`). So the instruction layer has **two delivery paths**, both populated by the
match-engine Phase-D snapshot-assembly layer (the single place that reads `TeamTactic`/`PlayerTactic[]`):

| Consumer | Path | New fields required |
|---|---|---|
| #8 Decision Tree | via `TacticalContext` (already AI-layer) | replace the two `bool` stubs with resolved `TeamTactic` ref + per-agent `PlayerTactic` ref |
| #12 Positioning | via `PositioningPerceptionSnapshot` / `ContextModifierInputs` | width/role/duty fields |
| #13 Pressing | via `PressingSnapshot` | trigger-mask + line-of-engagement fields |
| #14 Defensive | via `DefensiveSnapshot` | man-mark override + offside-toggle fields |
| #15 Attacking | via `AttackingSnapshot` | style/overload/width fields |

**Snapshots store the already-translated *local* enums, not `tactics` enums.** The assembly layer runs
the §2.1 translation **once** when it materializes/changes a tactic (i.e. on a tactic-change event, not
per agent per tick) and writes the subsystem's own enum into the snapshot field. So the Mechanics ticks
never run a map on their hot path and never reference `tactics` — they read their own enum type as they
do today. `tactics`-typed values appear only in `TeamTactic`/`PlayerTactic` and at the DT seam.

---

## 3. New types (the concrete gap)

> All enums are `byte`-backed and **APPEND-only** (ordinal stability — every new enum carries the
> standard paragraph; ordinals feed snapshots/digests once Phase D serializes tactics, §6.1). All are
> declared in `tactics/`. Constant **values** below are illustrative defaults for review, **not** pinned
> `[GT]` values.

### 3.1 Team-level enums (`tactics/`)

| Type | Members (draft) | Maps to / consumed by |
|---|---|---|
| `Mentality` | VeryDefensive, Defensive, Cautious, Balanced, Positive, Attacking, VeryAttacking | global utility risk multiplier in `UtilityScorer`; `StyleProfile` selection (§3.5); `DefensiveLineDepth` bias |
| `Tempo` | Lowest…Highest (5) | decision/PASS thresholds in `OptionGenerator`/`UtilityScorer` (NOT tick rate) |
| `TacticWidth` | VeryNarrow…VeryWide (5) | `ContextModifierInputs` lateral scaling (#12) |
| `LineOfEngagement` | Lowest…Highest (5) | press trigger distances (#13 `TriggerEvaluator`) |
| `TacticDefWidth` | Narrow/Standard/Wide | `ContextModifier` out-of-possession compactness |
| `TransitionPlan` | CounterPress/Regroup (lost); Counter/HoldShape (won) | `StyleProfile.TransitionHoldTicks` (#15); counter-press gate (#13) |
| `GkDistributionPolicy` | DistributeQuick, SlowDown, ToCentreBacks, ToFullBacks, ToTarget, LongClear | `DistributeIntent` defaults (#11) |
| `FocusPlay` | Left, Right, ThroughMiddle, Mixed | `OverloadDetector` flank bias (#15); **NEW** lateral-preference branch in #8 `OptionGenerator` (no existing hook — §2.3 exception) |
| `TacticPassing` | Short, Mixed, Direct | translated → #8 `PassingStyle` at the DT seam |
| `TacticPressing` | Low, Medium, High | translated → #8 `PressingMode` at the DT seam |
| `TacticTriggerMask` | `[Flags]` BadTouch/BackwardPass/SidelineTrap/WeakReceiver | translated → #13 `TriggerFlags` at the pressing seam |

`TacticPassing`/`TacticPressing`/`TacticTriggerMask` are the `tactics`-owned analogues of subsystem
enums, per the §2.1 translation rule (Stage 1 may widen `TacticPassing`/`TacticPressing` beyond 3 steps
independently of the subsystem enum, with the map clamping).

### 3.2 Player-level enums (`tactics/`)

| Type | Members (draft) | Maps to / consumed by |
|---|---|---|
| `Duty` | Defend, Support, Attack | positioning long-pct bias (#12); utility aggression bias (#8); tackle COMMIT floor (#14) |
| `PlayerRole` | curated Stage-1 subset (BallPlayingDefender, NoNonsenseCB, WingBack, DeepLyingPlaymaker, BoxToBox, Mezzala, BallWinningMid, AdvancedPlaymaker, InsideForward, Winger, TargetMan, Poacher, CompleteForward, …) | `RoleWeightModifiers` table (§3.4); positioning offset table (#12) |
| `InstrBias` | Less, Default, More | every individual instruction (§3.3) |

### 3.3 Aggregate structs (`tactics/`)

```
TeamTactic            // input, one per team
  Mentality           Mentality
  Formation           FormationFamily*    // see note
  Tempo               Tempo
  Width               TacticWidth
  Passing             TacticPassing
  Pressing            TacticPressing
  LineOfEngagement    LineOfEngagement
  DefensiveLine       float [0,1]         // same semantics as DefensiveLineDepth
  DefensiveWidth      TacticDefWidth
  TransitionWon       TransitionPlan
  TransitionLost      TransitionPlan
  OffsideTrap         bool                // → MarkDirective.OffsideTrapActive (#14)
  TriggerPressMask    TacticTriggerMask   // → #13 TriggerFlags via the seam map
  FocusPlay           FocusPlay
  GkDistribution      GkDistributionPolicy
  TimeWasting         byte [0..4]         // game-management dial (0 = never … 4 = always)

PlayerInstructions    // input, per agent (all biases Default = follow team)
  RiskyPasses         InstrBias
  ShootTendency       InstrBias
  DribbleTendency     InstrBias
  CrossTendency       InstrBias
  PositioningFreedom  InstrBias           // hold position … roam
  CloseDown           InstrBias
  TightMarking        bool
  MarkTargetEntityId  int                 // -1 = none → manager man-mark request (see §4 / §6.5)
  SetPieceRoles       SetPieceDutyFlags   // [Flags]: FreeKickTaker/CornerTaker/PenaltyTaker (Stage 1+)

PlayerTactic          // input, per agent
  Role                PlayerRole
  Duty                Duty
  Instructions        PlayerInstructions
```

\* `FormationFamily` is owned by `positioning-ai`. Per §2.1 the `TeamTactic.Formation` field uses a
`tactics`-owned `TacticFormation` enum translated at the #12 seam; widening the formation set means
adding members to **both** `TacticFormation` (here) and the #12 family table + pull-factors.

### 3.4 The role→weight-modifier table (the load-bearing new construct)

A static lookup `RoleWeightModifiers` keyed by `(PlayerRole, ActionType)` → multiplier, applied in
`UtilityScorer` after the existing zone×AM×context×tactical×risk product. This is what makes a Poacher
shoot more and hold less, a Deep-Lying Playmaker prefer PASS, a Mezzala drift and carry — the single
highest-value addition and the one most needing balance + adversarial review (§6.2). It parallels the
existing `TacticalWeights`/`UtilityWeights` catalogues and lives in `tactics/TacticsConstants.cs`.

### 3.5 `Mentality` → `StyleProfile` mapping (pinned, addresses the lossy-collapse gap)

Mentality is 7-valued; #15 ships 3 `StyleProfile` factories. The collapse is **explicit**, not implied:

| Mentality | StyleProfile (#15) | Global risk mult (illustrative) | DefensiveLine bias |
|---|---|---|---|
| VeryDefensive | Counter | 0.80 | −0.20 |
| Defensive | Counter | 0.88 | −0.12 |
| Cautious | Possession | 0.94 | −0.05 |
| Balanced | Possession | 1.00 | 0.00 |
| Positive | Possession | 1.06 | +0.05 |
| Attacking | Direct | 1.14 | +0.12 |
| VeryAttacking | Direct | 1.20 | +0.20 |

Mentality drives **three** distinct outputs (style profile, a risk multiplier on utility, a
defensive-line bias). The 7→3 style collapse is intended to be covered by the risk-mult + line-bias
gradation, but whether three Mentalities sharing one `StyleProfile` (Cautious/Balanced/Positive →
Possession) feel distinct in play is an **open balance question for §6.2**, not a settled fact — the
risk-mult spread (~6% per step) and all other values here are illustrative pending that pass.

---

## 4. Functions / wiring changes (per consuming subsystem)

| Subsystem | Change needed |
|---|---|
| **#8 Decision Tree** | `UtilityScorer`: apply (a) `Mentality` global risk multiplier (§3.5), (b) `RoleWeightModifiers[Role, type]`, (c) `Duty` aggression bias, (d) `PlayerInstructions` per-action biases. `OptionGenerator`: `Tempo` threshold bias + `Width` bias on MOVE target generation (existing inputs); **new** `FocusPlay` lateral-preference branch (no existing hook — §2.3 exception). `TacticalContext`: replace the two `bool` stubs with resolved `TeamTactic`/`PlayerTactic` refs. Add `tactics`→DT enum maps (`TacticPassing`→`PassingStyle`, `TacticPressing`→`PressingMode`). |
| **#12 Positioning** | `PositioningPerceptionSnapshot`/`ContextModifierInputs`: new width/role/duty fields. Add a `PlayerRole`/`Duty` positioning-offset table parallel to the pull-factor table. Expand `FormationFamily` + `TacticFormation` seam map. |
| **#13 Pressing** | `PressingSnapshot`: new trigger-mask + line-of-engagement fields. `TriggerEvaluator`: gate each trigger on the mask; scale distances by `LineOfEngagement`/`TacticPressing`. Counter-press gate from `TransitionLost`. Add `TacticTriggerMask`→`TriggerFlags` map. |
| **#14 Defensive** | `DefensiveSnapshot`: new man-mark-override + offside-toggle fields. `MarkAssigner`: honour a `MarkTargetEntityId` override **subject to** the §3.10 anti-chaos invariants (precedence defined in §6.5 before implementation, not assumed). `OffsideTrapController`: enable from `OffsideTrap`. `TackleIntentEvaluator`: `CloseDown`/`Duty` → COMMIT floor + jockey angle. |
| **#15 Attacking** | `AttackingSnapshot`: style/overload/width fields. Style from §3.5 mapping + `TransitionWon`. `OverloadDetector`: `FocusPlay` bias. `WidthHolder`: `TacticWidth`. |
| **#11 Goalkeeper** | `GoalkeeperDistribution`: default `DistributeIntent` from `GkDistributionPolicy`. |
| **Config loader (prereq)** | A `TacticLoader` (or the general `[GT]` loader) materializes `TeamTactic`/`PlayerTactic[]` at boot and on in-match change, then resolves the `[GT]` constants the instructions select. |

---

## 5. Sequencing (slots into the match-engine roadmap)

Additive and staged — each piece lands behind the integration phase that gives it a live consumer,
avoiding the phantom-interface anti-pattern (ERR-001/ERR-004).

- **T0 — low-risk scaffolding (landable now, no live consumer):** the `tactics/` assembly + all pure
  enums and the `TeamTactic`/`PlayerTactic`/`PlayerInstructions` structs with `Balanced`/`Default`
  factories. No behaviour change. Each enum gets the ordinal-stability paragraph + an
  `EnumOrdinalStabilityTests` entry. The seam **map functions** are *not* T0 (they live in consumer
  assemblies and need those consumers — T2/T3).
- **T1 — config loader (prereq):** `[GT]` loader + `TacticLoader`. Unblocks runtime injection.
- **T2 — with match-engine Phase D (AI):** route `TeamTactic`/`PlayerTactic` via `TacticalContext` into
  `UtilityScorer`+`OptionGenerator`; implement the `Mentality` multiplier (§3.5) and the
  `RoleWeightModifiers` table (own adversarial + balance review — highest risk).
- **T3 — Mechanics consumers:** add the snapshot fields + seam maps for #12/#13/#14/#15 as each
  Mechanics tick is wired into Phase D.
- **T4 — polish:** GK distribution policy, in-possession granular instructions, time-wasting, expanded
  formations, set-piece duties.

**Verification per stage:** T0 — `EnumOrdinalStabilityTests` + struct-factory unit tests (no behaviour).
T1 — loader round-trip + `[GT]` resolution units. T2 — `Mentality`/`RoleWeightModifiers` units on
`UtilityScorer` (numerical-mirror, per §6.2) + DT closed-loop scenario. T3 — per-subsystem seam-map
units + a Mechanics closed-loop scenario each. T4 — feature units. All closed-loop work runs through the
#19 `ScenarioRunner` once the AI phase composes.

---

## 6. Risks / open questions for review

1. **Snapshot schema impact.** Once Phase D serializes per-agent tactics, `PlayerTactic`/
   `PlayerInstructions` become digest-load-bearing cross-tick state and must enter the canonical field
   set with a `SNAPSHOT_SCHEMA_VERSION` bump. **Fix the field order before T2** to avoid a later bump
   (match-engine note flags schema-churn sensitivity).
2. **`RoleWeightModifiers` + §3.5 values balance.** Where "tactics feel real" lives and where balance
   bugs hide. Needs the same adversarial + numerical-mirror rigor the scoring specs got. All constant
   values in §3.1/§3.4/§3.5 are illustrative until this pass.
3. **Determinism of in-match tactic changes.** A touchline shout mutates `TeamTactic` mid-match; apply
   it only at a tactical-stride (10 Hz) tick boundary, never mid-physics-frame, or replay diverges.
   Define the apply-point with the match-engine owner.
4. **`PlayerRole` roster size.** Stage 1 curates a defensible subset and appends later; the subset is a
   design call.
5. **Man-mark override vs. anti-chaos invariants.** A forced man-mark must still respect #14's
   `MinBacklineAgents`/`MaxManMarkAssignments` floors. **Precedence (manager intent vs. safety floor)
   is unresolved and must be decided before the #14 change in §4** — §4 defers to this entry rather
   than asserting an answer.
6. **Promotion to a formal spec.** If approved, this becomes spec #21 with the full template
   (§1–§9 + §5 test plan + §9 approval checklist + FR-IDs) and a `tactics/` folder under `docs/specs/`.
   This note is a design draft, not a spec; it is not review-complete for sign-off until those are added.

---

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-06-20 | — | Initial draft. Cross-checked FM tactical taxonomy against the implemented AI subsystems; enumerated missing enums, the role→weight table, and per-subsystem wiring; sequenced against match-engine Phase C–F. |
| 0.3 | 2026-06-20 | — | Second adversarial fix pass + full code fact-check. M-1: §2.1 "no approved file migrated" narrowed to "no enum re-homed"; the four snapshot structs + `TacticalContext` edits are named as the accepted bounded surface. M-2: §2.5 pins that snapshots store the **translated local** enum (assembly maps once on tactic-change, not per agent per tick) — no hot-path mapping, Mechanics never reference `tactics`. M-3: `FocusPlay` + `RoleWeightModifiers` flagged as genuinely **new branches** (§2.3 exception, §3.1/§4) — `OptionGenerator` has no lateral hook (verified in source). L: `TimeWasting` range pinned `[0..4]`; §3.5 "non-lossy" softened to an open §6.2 balance question; §5 gains a per-stage verification line. Code fact-check: all supplement references (4 snapshot structs, `ContextModifierInputs`/`MarkDirective` fields, `StyleProfile` factories, `UtilityScorer` product, `OptionGenerator` lack-of-hook) verified against source — no corrections needed. |
| 0.2 | 2026-06-20 | — | Adversarial fix pass. H-1: enum-ownership rule (§2.1) — `tactics/` owns its own instruction enums (`TacticPassing`/`TacticPressing`/`TacticTriggerMask`/`TacticFormation`) and consumers translate at a downward seam; no upward reference, no migration of approved files. H-2: `RoleId` correctly attributed to `positioning-ai` (not `agent-movement`); §2.4 no longer "reuses" it — `PlayerRole` is a new distinct enum. M-1: new §2.5 routing table — Mechanics consumers (#12–#15) receive tactics via new fields on their own per-tick snapshots, only #8 via `TacticalContext`; §4 lists the snapshot-field additions. M-2: §3.5 pins the `Mentality`→`StyleProfile` mapping + risk-mult + line bias (7→3 collapse shown non-lossy in aggregate). M-3: §0 corrected — explicitly a design note, not template-complete; promotion needs §5/§9/FR-IDs. L: set-piece field fixed to `SetPieceDutyFlags` (no bogus "Forward"); §4 #14 defers man-mark precedence to §6.5 instead of asserting; illustrative-value caveat reinforced in §3 + §6.2. |
#endregion
