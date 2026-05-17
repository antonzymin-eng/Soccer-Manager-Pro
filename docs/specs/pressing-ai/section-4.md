# Pressing AI Specification #13 — Section 4: Architecture, File Layout, Interface Contracts

**Created:** May 17, 2026
**Last Updated:** May 17, 2026 (v0.2 PASS-1 adversarial-review fix pass)
**Version:** 0.2
**Status:** DRAFT
**Source:** `outline-detailed.md` v1.0

---

## 4.1 Architecture Overview

Pressing AI is a single subsystem `PressingAI`, scheduled on the
10 Hz tactical loop **after** #12 Positioning AI (which produces
the baseline `formationSlot[]`) and **before** #8 Decision Tree
(which consumes #13's `PressDirective` / `PressAssignment` per
KD-3).

The subsystem exposes a pure-function `Tick(...)` entry point
(pseudocode §3.11) with two side effects:

1. Writing the per-team `PressDirective` and per-agent
   `PressAssignment[22]` output buffers.
2. Mutating the internal `RoleHysteresisState` and `PressTrigger`
   structs (themselves authoritative simulation state under #16
   §3.2 — §4.6 / FR-PR-004).

No threads, no async work, no per-frame callbacks. All work is
synchronous on the tactical thread (KD-2).

**Runtime activates at Stage 1.** Stage 0 ships the spec, not the
code (KD-12 / §1.8 / FR-PR-043).

## 4.2 File Structure (#20 §4.2 Compliant — Single Catalogue)

```
src/PressingAI/                                  (Stage 1)
├── PressingAITick.cs           (10 Hz entry point; pseudocode §3.11)
├── TriggerEvaluator.cs         (§3.1 + §3.2)
├── PrimaryPressSelector.cs     (§3.3)
├── CoverShadowSelector.cs      (§3.4 + §3.5)
├── RoleHysteresis.cs           (§3.6; authoritative state)
├── StaminaAccumulator.cs       (§3.7)
├── DisengageResolver.cs        (§3.8)
├── InvariantEnforcer.cs        (§3.9 + KD-16)
└── PressingAIConstants.cs      (SINGLE catalogue per FR-CS-025 / KD-15)
```

KD-15 / FR-PR-007: a single `PressingAIConstants.cs` per #20 §4.2
FR-CS-025. Trigger thresholds, hysteresis constants, role caps,
stamina costs, zone polygons, and anti-chaos floors all live as
`#region` blocks inside the same file.

## 4.3 Internal Module Contracts

| Module | Public Surface | Consumed By |
|---|---|---|
| `TriggerEvaluator` | `TriggerFlags EvaluateRaw(in PerceptionSnapshot, in PassEventRing, Vector2 attackingDir)`; `void UpdateDebounce(in TriggerFlags raw, ref PressTrigger state)` | `PressingAITick` |
| `PrimaryPressSelector` | `EntityId? Select(in PerceptionSnapshot, in PositioningAIView)` | `PressingAITick` |
| `CoverShadowSelector` | `void Select(in PerceptionSnapshot, in PositioningAIView, EntityId? primary, Span<CoverShadow> outShadows, out int count)` | `PressingAITick` |
| `RoleHysteresis` | `void Apply(EntityId? primary, ReadOnlySpan<CoverShadow> shadows, ref RoleHysteresisState state, Span<PressRole> outRoles)` | `PressingAITick` |
| `StaminaAccumulator` | `void Accumulate(ReadOnlySpan<PressRole> roles, Span<float> fatigue)` | `PressingAITick` |
| `DisengageResolver` | `bool ShouldDisengage(in TriggerFlags fired, in BallState ball, Vector2 attackingDir, ref RoleHysteresisState state)` | `PressingAITick` |
| `InvariantEnforcer` | `bool Enforce(Span<PressRole> roles, in PerceptionSnapshot, in PositioningAIView)` | `PressingAITick` |

All public surfaces are zero-allocation (FR-PR-006 / #18 §3.7):
pass `Span<T>`, `ReadOnlySpan<T>`, and `ref` / `in` struct
parameters. Output is written into caller-supplied buffers.

## 4.4 Upstream Integration Contracts

### 4.4.1 Perception (#7) Read

`PerceptionSnapshot` is read once at tick start. #13 does not
re-read mid-tick (FR-PR-037 / F3). Fields consumed:

| Field | Source | Use |
|---|---|---|
| `tickIndex` | #7 | F1 detection |
| `agents[].position` | #7 §3.7 | primary-press cost; cover-shadow lane; sideline trap |
| `agents[].facing` | #7 §3.7 | `SIDELINE_TRAP` carrier-facing dot |
| `ball.position` / `ball.velocity` | #7 §3.7 | sideline geometry; primary-press interception lookahead |
| `possession.owner` | #7 §3.9 | ball-carrier identification; KD-11 phase gating (via #12) |
| `agents[].isActive` | #7 §3.10 | substituted / red-carded filter |
| `agents[].attribute.FirstTouch` | #7 §3.10 | `WEAK_RECEIVER` |
| `agents[].fatigue` | #7 §3.10 | §3.7 / FR-PR-029 |
| `agents[].perceivedPressure` | #7 §3.10 | `WEAK_RECEIVER` |
| `agents[].lastTouch.q` | #7 §3.10 (Q2 — perception-propagated) | `BAD_TOUCH` |

### 4.4.2 Pass Mechanics (#5) Event Ring

`PassEventRing` is a per-tick read of `PassAttemptEvent` instances
published at #5 `CONTACT` (FR-10). The confirmed event payload
(`pass-mechanics/section-2.md` FR-10 §351) is: `AgentID`,
`PassType`, `TargetPosition`, `FrameNumber` — no velocity field.
#13 reads the most-recent event for the opposing team's pass and
computes the `BACKWARD_PASS` dot-product locally: pass direction is
derived from `perception.agents[e.AgentID].position` (passer) to
`e.TargetPosition` (KD-1: #13 owns the threshold and the direction
derivation; no upstream "backward" classification consumed).

### 4.4.3 #12 Positioning AI Read

**Stage 0 accessors** (sole published Stage 0 surfaces per #12 §4.4.3):

```
PositioningAI.GetFormationSlot(EntityId id)    // baseline slot
PositioningAI.IsSentinel(Vector2 slot)         // F6 detection
```

**Stage 1+ accessors** — NOT exposed at Stage 0 (CLAUDE.md
"Interface Design Principle" / #12 §4.5.1). Back-prop requests
filed to publish these at Stage 1:

```
// ERR-013-007: back-prop into #12 §4 to publish GetPhase at Stage 1
// PositioningAI.GetPhase(TeamId team)         // local phase enum — Stage 1+

// ERR-013-008: back-prop into #12 §4 to publish GetLine at Stage 1
// PositioningAI.GetLine(EntityId id)          // Defense | Midfield | Attack — Stage 1+
```

Because #13 runtime is itself Stage 1 (§1.8 / KD-12), these
accessors will be available when #13 activates — but #12 must ratify
the publications via ERR-013-007 / ERR-013-008 before #13 can treat
them as confirmed surfaces.

KD-4: #13 **biases** but does not **replace** #12's slots. The
orchestrator composes the per-agent target for #8 by:

1. Reading #12's `formationSlot[id]` as the default.
2. Replacing it with #13's `PressAssignment.targetPosition` when
   the role is `PRIMARY_PRESS` or `COVER_SHADOW`.
3. Leaving the #12 slot intact for `HOLD_SHAPE`.

This is the `PressOverride` displacement layer #12 §7.3 reserves.

### 4.4.4 #8 Decision Tree Coupling (KD-3 — mechanism deferred)

**Two candidate mechanisms (OI-001 — section-file draft does NOT
pre-decide; final selection gates §9 sign-off):**

**Option A — read-only accessor on `PressingAI`:**

```
PressAssignment PressingAI.GetAssignment(EntityId id);
PressDirective  PressingAI.GetDirective(TeamId team);
```

#8 §3.1.8.2 (PRESS target selection) calls `GetAssignment(self)`
during utility scoring. The `TacticalContext` schema is NOT
touched. Advantage: zero amendment to the frozen #8 schema —
purely additive. Disadvantage: introduces a cross-subsystem
accessor on the per-agent hot path; the orchestrator-tier wiring
adds one indirection.

**Option B — `TacticalContext.PressDirective` field extension:**

```
struct TacticalContext {
    Vector2 FormationSlot;
    PressingInstruction pressing;
    PassingInstruction  passing;
    DefensiveLineDepth  defensiveLineDepth;
    PressDirective?     pressDirective;      // NEW (Stage 1)
}
```

#8 §2.2.6 amendment ratifies the field addition. #13 (via
orchestrator) writes the field at #8 Step 2 (parallel to #12's
`FormationSlot` write per #12 §4.4.3 AR-S1-04). Advantage:
single read inside #8; no cross-subsystem call. Disadvantage:
unfreezes the `TacticalContext` schema at Stage 1 — but **#12's
Stage-0 freeze argument does NOT apply** to #13, because #13 is
itself a Stage-1 binding. The freeze only forbids Stage-0 writers
from adding fields; Stage-1 writers do so via the §2.2.6
amendment path.

**Recommendation (non-binding):** Option B is cleaner for #8's hot
path and aligns with the #12 freeze-then-amend pattern. Final
selection is `ERR-013-001` resolution / §9.2.

## 4.5 Downstream Integration Contracts

### 4.5.1 To Orchestrator (Stage 1)

```
ReadOnlySpan<PressAssignment> PressingAI.GetAssignments();
PressDirective PressingAI.GetDirective(TeamId team);
```

Used to compose the per-agent target slot for #8 (per §4.4.3) and
to surface telemetry to #17 channels (`ERR-013-002` /
`ERR-013-003` at Stage 1+).

### 4.5.2 To #8 (via OI-001 mechanism)

See §4.4.4 above. Stage 0 declares both candidate surfaces; Stage
1 ratifies one.

### 4.5.3 To #14 / #15 (Stage 1+ — declared, not implemented)

Per CLAUDE.md "Interface Design Principle" — no accessor or field
is published until the downstream consumer spec reaches
`IN REVIEW`. KD-5 / KD-6 are declarations only.

## 4.6 Determinism & Safety Boundaries (Binding to #16)

| Concern | Binding | Notes |
|---|---|---|
| Iteration order | #16 §3.2.5 | Outfield agents iterated EntityId ascending; GK handled by KD-13 exclusion (no iteration over GK in selection loops) |
| RNG domain tag | #16 §3.4 | `DOMAIN_TAG_PRESSING_AI = 0x19` `[CROSS-PENDING]` (`ERR-013-005`; inherits ERR-012-001 block proposal). Stage 0 §3 currently has no stochastic step — the tag is declared **solely to lock the block slot** in the ERR-012-001 Phase B/C allocation (`0x17…0x1C`) before a Phase-C spec (#14 or #15) can claim `0x19`. This is a block-collision-avoidance reservation per the first-to-APPROVED precedent; no `DeterministicRngService` calls exist at Stage 0. The tag gains its first consumer at Stage 1+ when stochastic tie-breaking is added. |
| Per-tick digest | #16 §6.2 | `PressDirective`, all 22 `PressAssignment`, and the full `RoleHysteresisState` + `PressTrigger` structs (FR-PR-004) |
| Stage-0 arithmetic | CLAUDE.md "When Writing Code" | `float`; Fixed64 deferred to Stage 5+ per #9 §8.1 (§7.9) |
| Float-comparison policy | KD-9 / KD-14 reuse | `SPACING_EPSILON_M2 = 1e-4 m²` cited from #12 §3.6.1 / KD-16 |
| State-snapshot determinism | #16 §3.2 | `RoleHysteresisState` and `PressTrigger` are saved/restored verbatim across snapshots |

## 4.7 Cross-Specification Validation Checks

The following checks run during integration testing (§5.4):

| Check | Validates | Trigger |
|---|---|---|
| Disjoint-role partition per tick | FR-PR-014 / KD-8 | every tick |
| GK never assigned press role | FR-PR-017 / KD-13 | every tick |
| Anti-chaos invariants pre-publication | FR-PR-018..021 / KD-16 | every tick |
| Digest scope coverage | #16 §6.2 | nightly determinism regression |
| EntityId iteration stability | #16 §3.2.5 | replay regression |
| Zero allocations on hot path | #18 §3.7 | profiler integration test |
| Single catalogue file | #20 FR-CS-025 | build-time lint |
| No #14 / #15 type references | CLAUDE.md "Interface Design Principle" | build-time grep against `src/PressingAI/` |

## 4.8 Version History

| Version | Date | Author | Summary |
|---|---|---|---|
| 0.1 | May 17, 2026 | AI agent (claude/draft-ai-specification-5tvwH) | Initial draft from `outline-detailed.md` v1.0. KD-3 mechanism options A and B both preserved in §4.4.4 per OI-001. |
| 0.2 | May 17, 2026 | AI agent (claude/fix-ai-specs-review-qgWFR) | PASS-1 adversarial fix pass. AR-S1-H2: §4.4.2 `FR-08` → `FR-10`; removed "kick velocity vector" claim; added direction-from-positions derivation. AR-S1-H4: §4.4.3 split into Stage 0 (GetFormationSlot / IsSentinel) and Stage 1+ (GetPhase / GetLine) accessor groups; ERR-013-007 / ERR-013-008 back-prop requests documented. AR-S1-M3: §4.6 RNG domain tag row updated with block-collision-avoidance rationale for the Stage-0 reservation. |
