# Defensive AI Specification #14 — Section 4: Architecture, File Layout, Interface Contracts

**Created:** May 17, 2026
**Last Updated:** August 12, 2026 (v0.3 — KD-6 revised (`ERR-014-006`, wiring backlog W2): §4.2 file structure gains the three new tackle-outcome files and a constant-catalogue mention; §4.1 and §4.5's dead #8-mediates/#3-owns-contact claims corrected, §4.5.2 rewritten as the corrected boundary; §4.3 gains a `TackleOutcomeResolver` row; §4.6 per-tick digest row gains `TackleOutcome`.)
**Version:** 0.3
**Status:** DRAFT
**Source:** `outline-detailed.md` v1.0

---

## 4.1 Architecture Overview

Defensive AI is a single subsystem `DefensiveAI`, scheduled on the 10 Hz
tactical loop **after** #12 Positioning AI (which produces the baseline
`formationSlot[]` and phase enum) and **after** #13 Pressing AI (which
produces the per-agent role partition `PressAssignment[]`) and **before**
#8 Decision Tree (which consumes #14's `MarkDirective` / `MarkAssignment`
per KD-5).

The subsystem exposes a pure-function `Tick(...)` entry point
(pseudocode §3.13) with three authoritative side-effect writes:

1. Writing the per-team `MarkDirective` and per-agent `MarkAssignment[22]`
   output buffers.
2. Mutating `MarkHysteresisState[]` (per-agent dwell counters for
   assignment transitions).
3. Mutating `OffsideLineState` (per-team dwell counter + cooldown for
   offside trap).

`TackleIntentRequest[]` is produced per tick but is **not retained**
across ticks — it is consumed by the orchestrator within the same tick
cycle and requires no dwell state.

All authoritative state (`MarkDirective`, `MarkAssignment[]`,
`MarkHysteresisState[]`, `OffsideLineState`) is included in the per-tick
determinism digest per #16 §3.2 (KD-10). **Runtime activates at Stage 1.**
Stage 0 delivers the published specification; no runtime code is emitted at
Stage 0 per #8 §1.3.2 and KD-16.

**Amended (KD-6 revised — `ERR-014-006`, wiring backlog W2, August 12,
2026):** the tackle-outcome resolution added at §3.6.5 is a scoped
exception to the "no runtime code at Stage 0" statement above —
`TackleOutcomeResolver.cs` ships as runtime code alongside this revision,
because the delegation it replaces was never implementable. This note
narrows only the tackle-outcome claim; it does not re-derive the broader
Stage 0/Stage 1 activation gate (§1.8, §7.1) for the rest of #14, which
this revision does not touch.

No threads, no async work, no per-frame callbacks. All work is synchronous
on the tactical thread (KD-2).

## 4.2 File Structure (#20 §4.2 Compliant — Single Catalogue)

```
src/DefensiveAI/                                     (Stage 1+)
├── DefensiveAITick.cs          (10 Hz entry point; pseudocode §3.13)
├── HoldShapePoolFilter.cs      (§3.2 — excludes GK + #13 press roles)
├── MarkAssigner.cs             (§3.3 + §3.4 + §3.5 assignment algorithm)
├── TackleIntentEvaluator.cs    (§3.6)
├── TackleDuelInputs.cs         (§3.6.5.4 — KD-6 revised, `ERR-014-006`)
├── TackleOutcome.cs            (§3.6.5.2 — KD-6 revised, `ERR-014-006`)
├── TackleOutcomeResolver.cs    (§3.6.5.3 — KD-6 revised, `ERR-014-006`)
├── OffsideTrapController.cs    (§3.7; OffsideLineState is authoritative)
├── LastManDetector.cs          (§3.8 + §3.9)
├── InvariantEnforcer.cs        (§3.10 + KD-17)
├── MarkHysteresis.cs           (§3.11; MarkHysteresisState is authoritative)
└── DefensiveAIConstants.cs     (SINGLE constant catalogue per FR-CS-025 / KD-14)
```

**Amended (KD-6 revised — `ERR-014-006`, wiring backlog W2, August 12,
2026):** `TackleDuelInputs.cs`, `TackleOutcome.cs`, and
`TackleOutcomeResolver.cs` are live in `src/defensive-ai/` as of this
revision — see the §4.1 Stage-0-runtime-code note below.

KD-14 / FR-DA-007: a single `DefensiveAIConstants.cs` per #20 §4.2
FR-CS-025. Assignment thresholds, hysteresis constants, offside-trap
parameters, tackle-intent thresholds, tackle-outcome-resolution constants
(§3.6.5.5, ten `[GT]` + one `[FIXED]` — KD-6 revised, `ERR-014-006`),
anti-chaos floors, GK zone bounds, and cross-spec scalar imports all live
as `#region` blocks inside the same file.

No class types on the hot path (zero-alloc per #18 §3.7 / FR-DA-006).
All module types are `readonly struct`.

## 4.3 Internal Module Contracts

Each module's input/output is declared as `readonly struct` parameters
(caller-allocated; no heap traffic per FR-DA-006 / #18 §3.7):

| Module | Input | Output |
|---|---|---|
| `HoldShapePoolFilter` | `PerceptionSnapshot`, `ReadOnlySpan<PressAssignment>`, GK `EntityId` | `HoldShapePool` (`EntityId[]` + count) |
| `MarkAssigner` | `HoldShapePool`, `PerceptionSnapshot`, `BaselineDefensiveShapeView`, `Span<MarkHysteresisState>` | `Span<MarkAssignment>` (per pool agent) |
| `TackleIntentEvaluator` | `HoldShapePool`, `PerceptionSnapshot`, `ReadOnlySpan<MarkAssignment>` | `Span<TackleIntentRequest>` (count ≤ pool size) |
| `TackleOutcomeResolver` (KD-6 revised, `ERR-014-006`, §3.6.5) | `in TackleDuelInputs`, `float uniform` | `TackleOutcome` — pure static, no state, no RNG service call (caller supplies the draw) |
| `OffsideTrapController` | `HoldShapePool`, `PerceptionSnapshot`, `PressDirective`, `ref OffsideLineState` | Updated `OffsideLineState`; DEFENSE-line `MarkAssignment` overrides if trap fires |
| `LastManDetector` | `HoldShapePool`, `PerceptionSnapshot` | `LastManResult { isEmergency, lastManEntityId, advancingAttackerEntityId, isGKOutOfZone }` |
| `InvariantEnforcer` | `HoldShapePool`, `ReadOnlySpan<MarkAssignment>`, `BaselineDefensiveShapeView` | `bool allClean`; corrected `Span<MarkAssignment>` |
| `MarkHysteresis` | current `MarkHysteresisState`, proposed `MarkAssignment` | updated `MarkHysteresisState`, committed `MarkAssignment` |

All public surfaces pass `Span<T>`, `ReadOnlySpan<T>`, and `ref` / `in`
struct parameters. Output is written into caller-supplied buffers.

## 4.4 Upstream Integration Contracts

All upstream contracts are Stage 1+ (consistent with the Stage 1 runtime
activation per KD-16 / §1.8). Spec-text references to accessor names are
boundary declarations at Stage 0; no code interfaces are generated against
them until Stage 1.

### 4.4.1 Perception (#7) Read

`PerceptionSnapshot` is read once at tick start (FR-DA-029 / F1 detection
boundary). #14 does not re-read mid-tick. Fields consumed:

| Field | Source | Use |
|---|---|---|
| `tickIndex` | #7 | F1 (stale-snapshot) detection |
| `agents[].position` | #7 §3.7 | assignment cost; last-man predicate; offside line depth; GK zone check |
| `agents[].velocity` | #7 §3.7 | INTERCEPT_RUNNER velocity threshold; tackle approach angle |
| `agents[].isActive` | #7 §3.10 | substituted / red-carded filter |
| `ball.position` | #7 §3.7 | last-man threat (§3.8); offside trap carrier position (§3.7) |
| `ball.velocity` | #7 §3.7 | offside trap ball-speed trigger (§3.7) |
| `possession.owner` | #7 §3.9 | phase gate (team has/lacks possession) |
| `agents[].attribute.FirstTouch` | #7 §3.10 | threat score numerator (§3.5) |

### 4.4.2 #12 Positioning AI Read

**Stage 1 accessors** declared as boundary hints here and confirmed in #12
§4.5.1 v0.3 per ERR-013-007/008 precedent:

```
// Confirmed in #12 §4.5.1 v0.3 (Stage 1+):
BaselineDefensiveShapeView  PositioningAI.GetBaselineShape(TeamId team);
Phase                       PositioningAI.GetPhase(TeamId team);
LineMembership              PositioningAI.GetLine(EntityId id);
float                       PositioningAI.GetDefensiveLineDepth(TeamId team);
```

#14 reads these once at tick start (F2 detection: if `GetBaselineShape`
unavailable, emit all-ZONAL per FR-DA-030). #14 does NOT write any #12
field (FR-DA-012 / KD-3).

### 4.4.3 #13 Pressing AI Read

```
// Confirmed in #13 §4.5 (Stage 1+):
ReadOnlySpan<PressAssignment>  PressingAI.GetAssignments();
PressDirective                 PressingAI.GetDirective(TeamId team);
```

Used by `HoldShapePoolFilter` (§3.2): agents with role `PRIMARY_PRESS` or
`COVER_SHADOW` are excluded from #14's pool (FR-DA-010 / KD-4). The
`PressDirective` is passed to `OffsideTrapController` to detect active
pressing (§3.7 trigger condition 4). If unavailable at tick start, F3
applies: treat all agents as HOLD_SHAPE (FR-DA-031).

### 4.4.4 #8 Decision Tree Coupling (KD-5 — Option B Selected)

**ERR-014-001 filed (May 17, 2026) — Option B selected, mirrors #13
ERR-013-001 precedent:**

`TacticalContext.MarkDirective?` nullable field added to #8 §2.2.6
(`decision-tree/section-2-1-to-2-2.md` amendment via ERR-014-001).
The integration contract at Stage 1:

```csharp
// #14 (via orchestrator) writes per-tick before #8 runs:
ctx.MarkDirective = defensiveAI.GetMarkDirective(team);   // null → #8 ignores

// #8 reads in MOVE_TO_POSITION (§3.1.7) and INTERCEPT (§3.1.9) scoring:
if (ctx.MarkDirective.HasValue) { /* adjust target via MarkAssignment */ }
```

Rationale for Option B: aligns with the `TacticalContext.PressDirective?`
freeze-then-amend pattern already chosen for #13 (ERR-013-001); single
struct-field read is cheaper than a cross-subsystem accessor call on the
#8 per-agent hot path; nullable semantics permit `null = no active defensive
directive` at Stage 0 without requiring a stub. The orchestrator also exposes
the full assignment via §4.5.1 for telemetry and test harness use.

## 4.5 Downstream Integration Contracts

### 4.5.1 To Orchestrator (Stage 1)

```csharp
ReadOnlySpan<MarkAssignment>   DefensiveAI.GetAssignments();
MarkDirective                  DefensiveAI.GetMarkDirective(TeamId team);
ReadOnlySpan<TackleIntentRequest> DefensiveAI.GetTackleIntentRequests();
```

`GetAssignments()` supplies the per-agent assignment span used by the
orchestrator to compose the per-agent target for #8 (HOLD_SHAPE agents use
#14's mark target; PRIMARY_PRESS / COVER_SHADOW agents retain their #13
target). `GetTackleIntentRequests()` surfaces tackle intents; **see the
`ERR-014-006` amendment to §4.5.2 below — they are no longer routed through
#8/#3.** `GetMarkDirective()` is used to write `TacticalContext.MarkDirective?`
(Option B per §4.4.4).

### 4.5.2 Tackle Resolution (KD-6, revised — `ERR-014-006`)

**This section is corrected, not merely amended: its original text
described a dispatch that never had a working delegate.** It read:
*"`TackleIntentRequest` is surfaced to the orchestrator via
`DefensiveAI.GetTackleIntentRequests()`. The orchestrator passes each
intent to #8, which translates it into an `AgentAction` (TACKLE, JOCKEY,
or HOLD) dispatched to #3 §3.3 (agent-agent collision response). #14 does
NOT call #3 directly — the parameter-based physics pipeline principle
(CLAUDE.md) is preserved. #14 produces intent parameters; #3 translates
them into contact physics."* #8's `ActionType` ordinal space is exhausted
(no room for an eighth action without a composure-noise digest rebaseline)
and #8 has no tackle model to translate into; #3 defers slide-tackle
collision to Stage 2 (§7.2.1) and never landed the `TackleContactFlag`
amendment Pass Mechanics #5 §4.4.2 flagged as blocking (`XC-4.4-02`).

**Corrected boundary:** `TackleIntentRequest` is still surfaced via
`DefensiveAI.GetTackleIntentRequests()`, but the composition root owns
only the trigger geometry — which intent becomes a committed challenge,
and when. When a `COMMIT` intent is triggered, the composition root builds
a `TackleDuelInputs` (§2.2.8) from the #7 perception snapshot and calls
`TackleOutcomeResolver.Resolve(in TackleDuelInputs, float uniform)`
(§3.6.5, §4.3) directly — #14 resolves its own outcome. #14 still does NOT
call #3 directly, and the parameter-based physics pipeline principle
(CLAUDE.md) is preserved: no `TackleIntentRequest`/`TackleOutcome` value
enters the physics layer as a type enum. #3's contact-physics model
becomes the fallback authority only at Stage 2+, per §3.6.5.1.

### 4.5.3 To #15 Attacking AI (Stage 1+ — declared, not implemented)

Per CLAUDE.md "Interface Design Principle" — #15 is NOT STARTED (FR-DA-036).
No accessor or field is published until #15 reaches `IN REVIEW`. KD-8
notes that the `emergencyFlag` in `MarkDirective` may be consumed by #15
as a goal-risk signal for transition-recovery behavior; this is a Stage 1+
declaration only and no interface is authored at Stage 0.

### 4.5.4 MarkDirectiveSnapshot (read-only view)

Available to #17 event-emission hooks and the test harness as a read-only
view derived from the published `MarkDirective` and `MarkAssignment[]`
buffers. No additional mutable state. Used by:
- §5.4 determinism regression test harness.
- Stage 1+ #17 MARK_ASSIGNED / LINE_STEPPED channel emission (ERR-014-002 /
  ERR-014-003; deferred to Stage 1 per KD-15 / §7.3).

## 4.6 Determinism & Safety Boundaries (Binding to #16)

| Concern | Binding | Notes |
|---|---|---|
| Iteration order | #16 §3.2.5 | HOLD_SHAPE pool iterated EntityId-ascending on every tick (FR-DA-003) |
| RNG domain tag | #16 §3.4 | `DOMAIN_TAG_DEFENSIVE_AI = 0x1A` `[CROSS-PENDING]` (ERR-014-004; resolves to `[CROSS]` when #16 §3.4 v1.0.4 patch lands) |
| Per-tick digest | #16 §6.2 | `MarkDirective` (2 teams), all 22 `MarkAssignment` slots, all 22 `MarkHysteresisState` slots, `OffsideLineState` (2 teams), `TackleIntentRequest[]` count + contents, and — added at KD-6 revision (`ERR-014-006`) — each resolved `TackleOutcome` this tick (§3.6.5.2: "Ordinals are stable and append-only — the outcome reaches match flow and is digest-visible") |
| Stage-0 arithmetic | CLAUDE.md "When Writing Code" | `float`; Fixed64 deferred to Stage 5+ per #9 §8.1 (§7.9) |
| State-snapshot determinism | #16 §3.2 | `MarkHysteresisState[]` and `OffsideLineState` saved/restored verbatim across snapshots |
| EntityId no-reuse | #2 §2.5 (XC-002-001); #8 §1.7.3 (XC-008-001) | Inherited; no additional constraint added |

The `DOMAIN_TAG_DEFENSIVE_AI = 0x1A` slot is declared within the
ERR-012-001 Phase B/C block (`0x17…0x1C`): `0x17` (#12), `0x18` or `0x1D`
(#11 — final slot depends on ERR-011-001 / ERR-012-001 race; whichever of
#11 and #12 reaches `APPROVED` first takes `0x17` / `0x18` respectively;
if #12 lands first, #11 shifts to `0x1D`), `0x19` (#13), `0x1A` (#14),
`0x1B` (#15). At Stage 0 the tag locks the block slot; the first consumer
call to `DeterministicRngService` occurs at Stage 1 when stochastic
tie-breaking (identical displacement cost across two candidates) is
implemented.

## 4.7 Cross-Specification Validation Checks

The following checks run during integration testing (§5) and as startup
assertions at Stage 1. Each check is grep-verifiable against source files
at section-file draft time.

| # | Check | Validates | Trigger |
|---|---|---|---|
| 1 | GK `EntityId` absent from HOLD_SHAPE pool output on every tick | FR-DA-009 / KD-7 | every tick |
| 2 | No overlap between #13-assigned agents and #14 `MarkAssignment` pool | FR-DA-010 / KD-4 | every tick |
| 3 | Fatigue inputs in [0.0, 1.0]; outside triggers Tier-A assertion | FR-DA-008 / KD-1 | tick start |
| 4 | EntityId no-reuse: cite #2 §2.5 (XC-002-001) and #8 §1.7.3 (XC-008-001) | FR-DA-003 | startup |
| 5 | Offside adjudication absent: grep §3.7 for "offside rule", "VAR", "goal line distance", "offside decision" → zero hits | FR-DA-020 / KD-9 | build-time lint |
| 6 | #15 interface absent at Stage 0: grep section files for any code interface referencing #15 → zero hits | FR-DA-036 / KD-8 | build-time lint |
| 7 | Disjoint role partition per tick: PRIMARY_PRESS ⊕ COVER_SHADOW ⊕ HOLD_SHAPE covers every active outfield agent | FR-DA-010 / KD-4 | every tick |
| 8 | Anti-chaos invariants satisfied before publication (3-pass limit; F4 fallback if violated) | FR-DA-024 / KD-17 | every tick |
| 9 | Single constant catalogue file: build-time grep for `DefensiveAI` constant definitions outside `DefensiveAIConstants.cs` → zero hits | FR-DA-007 / KD-14 | build-time lint |
| 10 | Zero allocations on hot path | FR-DA-006 | profiler integration test (§5.5) |

## 4.8 Version History

| Version | Date | Author | Summary |
|---|---|---|---|
| 0.1 | May 17, 2026 | AI agent | Initial draft. KD-5 Option B resolved: `TacticalContext.MarkDirective?` via ERR-014-001 (mirrors #13 ERR-013-001 precedent). Q2 resolved: #3 §3.3 tackle contact surface. `TackleIntentRequest` surfaced via `DefensiveAI.GetTackleIntentRequests()` accessor. Stage 1+ file layout declared. #12 Stage 1 accessor names confirmed per ERR-013-007/008 precedent. |
| 0.2 | May 17, 2026 | AI agent | PASS-1 adversarial review fix pass. L3: §4.4.2 accessor declaration corrected `LocalPhase` → `Phase` (non-existent type; canonical enum is `Phase` from #12 §2.1). M7: §4.6 domain tag block layout updated to reflect ERR-011-001/ERR-012-001 race: #11 may occupy `0x18` or `0x1D` depending on which of #11/#12 reaches `APPROVED` first. |
| 0.3 | August 12, 2026 | AI agent (wiring backlog W2) | KD-6 revised (`ERR-014-006`): the original delegation ("#14 produces intent; #8 mediates dispatch; #3 owns contact physics") has no working delegate. §4.2 file structure gains `TackleDuelInputs.cs`, `TackleOutcome.cs`, `TackleOutcomeResolver.cs` and a constant-catalogue mention of the new tackle-outcome region. §4.1 gains a scoped Stage-0-runtime-code amendment. §4.3 gains a `TackleOutcomeResolver` module-contract row. §4.5.1's "read by #8" clause corrected; §4.5.2 rewritten in place (its original text quoted verbatim, then corrected) to describe the composition root calling `TackleOutcomeResolver` directly instead of a #8→#3 dispatch. §4.6 per-tick digest row gains `TackleOutcome`. Implemented in `src/defensive-ai/TackleDuelInputs.cs`, `TackleOutcome.cs`, `TackleOutcomeResolver.cs`. |
