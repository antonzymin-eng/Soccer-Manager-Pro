# Positioning AI Specification #12 — Section 4: Architecture, File Layout, Interface Contracts

**Created:** May 15, 2026
**Last Updated:** May 18, 2026 (v0.4 — FAIL-4 fix (A-03): §4.6 RNG domain tag row — corrected value `0x16` → `0x17` and promoted `[CROSS-PENDING]` → `[CROSS: #16 §3.4]`; ERR-012-001 resolved.)
**Version:** 0.4
**Status:** DRAFT

---

## 4.1 Architecture Overview

Positioning AI is a single subsystem `PositioningAI`, scheduled on
the 10 Hz tactical loop ahead of Decision Tree #8. It exposes a
pure-function `Tick(...)` entry point with two side effects:

1. Writing the per-agent `Vector2 formationSlot` output buffer.
2. Mutating the internal `HysteresisState` struct (which is itself
   authoritative simulation state under #16 §3.2 — §4.6).

There are no threads, no async work, no per-frame callbacks. All
work is synchronous on the tactical thread.

## 4.2 File Structure (#20 §4.2 Compliant — Single Catalogue)

```
src/PositioningAI/
├── PositioningAITick.cs       (10 Hz entry point; pseudocode §3.11)
├── PhaseClassifier.cs         (§3.0)
├── AnchorCalculator.cs        (§3.1 + §3.2)
├── ShapeAnalyzer.cs           (§3.3 + §3.4)
├── ContextModifier.cs         (§3.5)
├── SpacingResolver.cs         (§3.6)
├── SlotComposer.cs            (§3.7)
├── HysteresisState.cs         (dwell counters; authoritative state)
└── PositioningAIConstants.cs  (SINGLE catalogue — archetypes + scalars)
```

KD-17 / FR-PA-011: a single `PositioningAIConstants.cs` per #20
§4.2 FR-CS-025. Formation archetype tables live as `#region`
blocks inside the same file — they do NOT split into a separate
`FormationCatalogue.cs`.

## 4.3 Internal Module Contracts

| Module | Public Surface | Consumed By |
|---|---|---|
| `PhaseClassifier` | `Phase Classify(PerceptionSnapshot, ref HysteresisState)` | `PositioningAITick` |
| `AnchorCalculator` | `Vector2 Anchor(FormationArchetype, RoleId)`; `Vector2 BallRelativeOffset(Vector3 ball, RoleId, Phase, FormationArchetype)` | `SlotComposer` |
| `ShapeAnalyzer` | `LineMembership ResolveLine(EntityId, Vector2 slot, ref HysteresisState)`; `LaneAssignment ResolveLane(EntityId, Vector2 slot, ref HysteresisState)` | `SlotComposer` |
| `ContextModifier` | `Vector2 Apply(Vector2 slot, Vector2 centroid, ContextModifierInputs, Phase)` | `SlotComposer` |
| `SpacingResolver` | `void EnforceHardSpacing(Span<Vector2> slots, ReadOnlySpan<Vector2> anchors, ReadOnlySpan<EntityId> idsAscending)` | `PositioningAITick` |
| `SlotComposer` | `void Compose(... refs and spans ...)` | `PositioningAITick` |

All public surfaces are zero-allocation: pass `Span<T>`,
`ReadOnlySpan<T>`, and `ref` / `in` struct parameters (FR-PA-006,
#18 §3.7).

## 4.4 Upstream Integration Contracts

### 4.4.1 Perception (#7) Read

`PerceptionSnapshot` is read once at tick start. #12 does not
re-read mid-tick (FR-PA-045). Fields consumed:

| Field | Source | Use |
|---|---|---|
| `tickIndex` | #7 | F1 detection |
| `agents[].position` | #7 §3.7 | line/lane partition, anchor distance |
| `ball.position` | #7 §3.7 | anchor offset, GK slot, phase |
| `ball.velocity` | #7 §3.7 | phase classification |
| `possession.owner` | #7 §3.9 | phase classification |
| `agents[].isActive` | #7 §3.10 | FR-PA-036 substitution/red-card filter |

### 4.4.2 Orchestrator Inputs

The match orchestrator supplies `ContextModifierInputs` at tick
start. Score difference, team-mean fatigue, and tactical-intensity
are computed by the orchestrator (#12 does not own those
computations).

### 4.4.3 #8 Decision Tree Coupling (KD-3)

**#12 does NOT write into `TacticalContext` directly.** The
orchestrator reads #12's per-agent `formationSlot` via the stable
accessor:

```
Vector2 PositioningAI.GetFormationSlot(EntityId id);
bool    PositioningAI.IsSentinel(Vector2 slot);
```

and **assigns the value into the agent's existing
`TacticalContext.FormationSlot` field** (AR-S1-04: NOT via
`Stage0Default()`). `TacticalContext.Stage0Default()` per #8 §2.2.6
is a **match-initialisation factory** that ALSO seeds
`PressingInstruction`, `PassingInstruction`, and
`DefensiveLineDepth` to their Stage 0 defaults; invoking it per
agent per 10 Hz tick would clobber those fields ten times per
second, breaking the Stage 1+ writer contracts that will publish
into those fields. The `TacticalContext` schema (#8-owned, §2.2.6)
remains frozen at Stage 0 — #12 mutates only the `FormationSlot`
field of an already-initialised struct.

The order of operations within one tactical tick is:

```
PER-MATCH (init):
  Orchestrator calls TacticalContext.Stage0Default(initialSlot)
  once per agent at match start to seed the per-agent
  TacticalContext struct.

PER-TICK (10 Hz):
  1. #7 Perception produces a fresh snapshot.
  2. PositioningAI.Tick(...) computes all 22 formationSlots.
  3. Orchestrator, per agent, executes:
         var slot = PositioningAI.GetFormationSlot(id);
         if (!PositioningAI.IsSentinel(slot))
             agentContext[id].TacticalContext.FormationSlot = slot;
     (Sentinel agents — substitutes / red cards — leave the field
     at its prior value; AR-S1-07.)
  4. #8 Decision Tree evaluates action utilities per agent;
     MOVE_TO_POSITION reads ctx.TacticalContext.FormationSlot.
  5. #8 emits resolved Action; #2 steers the agent at 60 Hz toward
     Action.TargetPosition.
```

## 4.5 Downstream Integration Contracts

### 4.5.1 To #8 (via orchestrator)

The `formationSlot` accessor described in §4.4.3 is the sole Stage 0
downstream interface.

**Stage 1 accessors (ERR-013-007 / ERR-013-008):** Pressing AI #13
requires two additional accessors at Stage 1 to implement KD-11 phase
gating and KD-16 invariant (2) backline floor. These are NOT exposed
at Stage 0 per CLAUDE.md "Interface Design Principle"; they are declared
here as Stage 1 publication commitments so #13 can cite them:

```
// Stage 1 (ERR-013-007 — required by #13 §3.11 KD-11 phase gate):
LocalPhase     PositioningAI.GetPhase(TeamId team);

// Stage 1 (ERR-013-008 — required by #13 §3.9 invariant (2) KD-16 backline floor):
LineMembership PositioningAI.GetLine(EntityId id);
```

Stage 0 pressing-ai code uses only `GetFormationSlot` and `IsSentinel`.
Stage 1 activation of these accessors is gated on #12 reaching `APPROVED`.

### 4.5.2 To #14 / #15 (Stage 1+ — declared, not implemented)

Stage 1+ #14 (Defensive AI) and #15 (Attacking AI) may consume
`LineMembership` and `LaneAssignment` for mark/cover assignment
and run selection. The Stage 1+ accessor shape will be:

```
LineMembership PositioningAI.GetLine(EntityId id);   // Stage 1 (#13 KD-16) / Stage 1+ (#14/#15 mark/cover)
LaneAssignment PositioningAI.GetLane(EntityId id);   // Stage 1+
```

`GetLine` is elevated to Stage 1 (from Stage 1+) per ERR-013-008 to
serve #13's KD-16 backline floor. `GetLane` remains Stage 1+ (name
reservation only). These accessors are NOT exposed at Stage 0 (CLAUDE.md
"Interface Design Principle").

## 4.6 Determinism & Safety Boundaries (Binding to #16)

| Concern | Binding | Notes |
|---|---|---|
| Iteration order | #16 §3.2.5 | Outfield agents iterated EntityId ascending; GK handled separately and is order-independent |
| RNG domain tag | #16 §3.4 | `DOMAIN_TAG_POSITIONING_AI = 0x17` `[CROSS: #16 §3.4]` — ERR-012-001 resolved May 18, 2026 |
| Per-tick digest | #16 §6.2 | All 22 `Vector2 formationSlot` values plus the full `HysteresisState` struct (FR-PA-038) |
| Stage-0 arithmetic | CLAUDE.md "When Writing Code" | `float`; Fixed64 deferred to Stage 5+ per #9 §8.1 |
| Float-comparison policy | KD-16 | Squared distance with `SPACING_EPSILON_M2 = 1e-4 m²` |
| State-snapshot determinism | #16 §3.2 | The `HysteresisState` struct is saved/restored verbatim across snapshots |

## 4.7 Cross-Specification Validation Checks

The following checks run during integration testing (§5.4):

| Check | Validates | Trigger |
|---|---|---|
| `FormationSlot` is finite & in-bounds | FR-PA-033, FR-PA-046 | every tick |
| Digest scope coverage | #16 §6.2 | nightly determinism regression |
| EntityId iteration stability | #16 §3.2.5 | replay regression |
| Zero allocations on hot path | #18 §3.7 | profiler integration test |
| Single catalogue file | #20 FR-CS-025 | build-time lint |
| No #13/#14/#15 type references | CLAUDE.md "Interface Design Principle" | build-time grep against `src/PositioningAI/` |

## 4.8 Version History

| Version | Date | Author | Summary |
|---|---|---|---|
| 0.1 | May 15, 2026 | AI agent (claude/draft-positional-ai-specs-MOejb) | Initial section-file draft from `outline-detailed.md` v1.2. |
| 0.2 | May 16, 2026 | AI agent (claude/review-positional-ai-specs-v4rmD) | PASS-1 adversarial fix pass. AR-S1-04 §4.4.3 rewritten: `Stage0Default()` is match-init-only per #8 §2.2.6; per-tick path is direct field write `ctx.FormationSlot = slot`; `IsSentinel(slot)` accessor added for AR-S1-07 substitute/red-card semantics. |
| 0.3 | May 17, 2026 | AI agent (claude/fix-ai-specs-review-qgWFR) | ERR-013-007 / ERR-013-008 back-prop: §4.5.1 updated to declare `GetPhase(TeamId)` and `GetLine(EntityId)` as Stage 1 accessor commitments for Pressing AI #13 KD-11 / KD-16 requirements. `GetLine` elevated from Stage 1+ to Stage 1 in §4.5.2. |
| 0.4 | May 18, 2026 | AI agent (adversarial-specs-review-run2-AFrm4) | FAIL-4 fix (A-03): §4.6 RNG domain tag row — corrected value `0x16` → `0x17` and promoted `[CROSS-PENDING]` → `[CROSS: #16 §3.4]`; ERR-012-001 resolved. |
