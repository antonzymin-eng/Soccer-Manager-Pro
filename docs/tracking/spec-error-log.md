# Specification Error Log

**Purpose:** Records architectural errors, unnecessary complexity, and incorrect patterns
identified during specification review. Each entry documents the problem, the correct
approach, and every file requiring revision. Fixes are deferred — this log is the
authoritative remediation backlog.

**Created:** February 19, 2026, 5:00 PM PST
**Version:** 1.12
**Updated:** May 14, 2026 (ERR-018-002 through ERR-018-011 resolved in v0.2 fix pass of Performance Optimization #18 section files)
**Status:** ERR-001 through ERR-012, ERR-016-001, ERR-016-002, ERR-017-001, ERR-018-001 through ERR-018-011 logged. ERR-010 closed (March 6, 2026). ERR-012 appended from addendum (April 22, 2026). ERR-016-001 added May 2, 2026 (phantom interface mitigation in Deterministic Simulation §4.2). ERR-016-002 added May 3, 2026; resolved at the spec-text level May 6, 2026 (`XC-002-001` in #2 §2.5; `XC-008-001` in #8 §1.7.3); only the back-propagation note in #16 §3.2.5 still pending. ERR-017-001 added May 12, 2026 (Event System #17 PASS 2 review — `DOMAIN_TAG_EVENT_LEDGER` allocation back-prop into #16 §3.4; open). ERR-018-001 added May 13, 2026 and resolved same day at outline level (Performance Optimization #18 `outline-detailed.md` v1.1 inverts KD-3 — #18 owns trace pipeline, #16 retains record format / regression scenarios / emission constraints; section-number citations corrected). ERR-018-002 through ERR-018-011 added May 14, 2026 from PASS-1 adversarial review of #18 section files v0.1 (4 H + 6 M findings); all resolved in v0.2 fix pass (May 14, 2026) — #18 section files advanced to IN REVIEW. ERR-002 and ERR-003 remain open.
**Raised During:** Pass Mechanics Spec #5 pre-Section 3 cross-spec audit; Decision Tree Spec #8 BLK-001

---

## Error Index

| ID | Title | Severity | Files Affected | Status |
|----|-------|----------|---------------|--------|
| ERR-001 | `IBallPhysicsCallback` fragments a single operation into four methods | Major | 2 | Closed — fixed in First_Touch_Spec_Section_4_v1_1.md |
| ERR-002 | `StringIDs` papers over an undesigned event bus with the wrong solution | Moderate | 1 | Open — low priority, fix at convenience |
| ERR-003 | `PerformanceContext` violation mandate imposes governance with no Stage 0 benefit | Moderate | 10 | Open — low priority, fix at convenience |
| ERR-004 | `IPossessionManager` and `IFirstTouchEventQueue` interface against unspecified systems | Major | 4 | Closed — fixed in First_Touch_Spec_Section_4_v1_1.md |
| ERR-005 | `KickType` enum encodes caller intent into Ball Physics (eliminated by design decision) | Major | 2 | Closed — resolved during audit |
| ERR-006 | `Ball.ApplyKick()` / `KickType` referenced in Ball Physics §8 but never defined in §3.1.11 | Critical | 2 | Closed — resolved in Ball_Physics_Spec_Section_3_1_v2_5.md |
| ERR-007 | `KickPower`, `WeakFootRating`, `Crossing` absent from `PlayerAttributes` | Critical | 1 | Closed — resolved in Agent_Movement_Spec_Section_3_5_v1_3.md |
| ERR-008 | `BallState` has no `PossessingAgentId` field; `ApplyKick()` amendment references it incorrectly | Critical | 2 | Closed — Option B adopted; possession external to BallState; resolved in Ball_Physics_Spec_Section_3_1_v2_5.md |
| ERR-009 | `PassThroughGround` / `PassThroughAerial` are redundant `KickType` values | Minor | 1 | Closed — resolved during audit; through passes use `PassGround`/`PassLofted` |
| ERR-010 | Shot Mechanics §1.1 refers to Decision Tree as Spec #7 — canonical number is #8 | Minor | 1 | ✅ Closed — Fixed in shot-mechanics/section-1.md v1.2 (March 6, 2026); part of comprehensive audit renumbering cascade |
| ERR-011 | `SpatialHashGrid.Query()` ignores radius parameter — always returns fixed 3×3 neighbourhood | Major | 1 | ✅ Closed — Fixed in Collision_System_Spec_Section_3_v1_1.md (March 5, 2026) |
| ERR-012 | First Touch §7 refers to Decision Tree as Spec #7 (5 occurrences) | Minor | 1 | ✅ Closed — Fixed in first-touch/section-7.md v1.1 (March 5, 2026) |
| ERR-016-001 | Phantom interface risk in Deterministic Simulation §4.2 | Medium | 1 | ✅ Mitigated — §4.2 reclassified as non-normative sketches in v0.7 fix pass |
| ERR-016-002 | EntityId no-reuse cross-spec constraint not back-propagated to specs #2 and #8 | Medium | 3 | Resolved (spec text) — May 6, 2026: `XC-002-001` added to Agent Movement #2 §2.5 (v1.1.1); `XC-008-001` added to Decision Tree #8 §1.7.3 (v1.1.1). Pending only: prose update in #16 §3.2.5. |
| ERR-017-001 | `DOMAIN_TAG_EVENT_LEDGER` allocation needed in Deterministic Simulation #16 §3.4 domain-tag table | Medium | 2 | Open — filed May 12, 2026 during PASS 2 adversarial review of `event-system/outline-detailed.md`. Patch to #16 §3.4 to be submitted at #17 IN REVIEW commit. Pattern parallel to ERR-016-002 cross-spec back-prop. |
| ERR-018-001 | Performance Optimization #18 `outline-detailed.md` cites Deterministic Simulation #16 sections by stale numbers / non-existent name (`#16 §7 regression scenarios`, `#16 §5 canonical save format`, `#16 §8 trace channels`) | Medium | 1 | ✅ Resolved at outline level — May 13, 2026 (same day as filing). `outline-detailed.md` v1.1 (a) inverts KD-3 (Spec #18 owns the trace pipeline; Spec #16 retains authority over canonical record format §3.2.4.1, regression scenarios §5, and determinism-of-emission constraints / veto authority over tick-pipeline trace points §3.1), and (b) corrects every `TBD-NORMATIVE`-marked #16 section-number citation against current `deterministic-sim/section-*.md`. Rationale for inversion: trace channels are an observability concern, not a determinism concern; mirrors KD-4 (#19 owns testing infrastructure, consumes #16 scenarios). New FR-PO-058a in §3.8.3 enforces determinism-of-emission for every #18-emitted trace point. Section files drafted from v1.1 will not inherit the drift. Architectural concern (re-anchor vs invert) is closed; section-file authoring still required to faithfully implement inverted KD-3 (FR-PO-058a in §3.8.3, #16-owner sign-off audit in §5.7, record-format binding in §3.8.4). |
| ERR-018-002 | `[HotPathAllocExempt]` attribute cited in #18 as "declared in Spec #20 §3" but does not exist in `code-standards/` | High | 5 | ✅ Resolved — May 14, 2026 (v0.2 fix pass): §3.7.5 declares governance identifier in #18; Spec #20 §3 cited as policy authority only; C# attribute deferred to Stage 0+1 |
| ERR-018-003 | MUST/MAY conflict between FR-PO-067 (§2.2.9) and §3.4.4 on baseline-reproducibility re-run | High | 1 | ✅ Resolved — May 14, 2026 (v0.2 fix pass): §3.4.4 "MAY" → "MUST" |
| ERR-018-004 | Three-way stage-of-resolution contradiction on +5% threshold: FR-PO-031 "Stage 0+1" vs §7.5 D9 "Stage 1" vs §7.1 Stage 0+1 deliverable | High | 1 | ✅ Resolved — May 14, 2026 (v0.2 fix pass): §7.5 D9 "Stage 1" → "Stage 0+1" |
| ERR-018-005 | Channel registry schema absent from Appendix F; §3.8.2 "Stage 0 declares schema" obligation unmet; F.1/F.2/F.4 reference `perf.budget`/`perf.alloc` channels without registry backing | High | 1 | ✅ Resolved — May 14, 2026 (v0.2 fix pass): Appendix F.0 channel registry schema added |
| ERR-018-006 | Hot-path allocation budget = 0 bytes/tick tagged `[GT]` in §3.10 instead of `[FIXED]` — not a designer-tunable value | Medium | 1 | ✅ Resolved — May 14, 2026 (v0.2 fix pass): §3.10 and §8.4 tags updated `[GT]` → `[FIXED]` |
| ERR-018-007 | Three Spec #19 body-text citations missing `TBD-NORMATIVE` tag and absent from §9.4.1 blocker list: §3.4.3 ("per Spec #19 §3.4.3"), §3.3.5 ("parallel Spec #19 §6.1"), §3.9.5 ("Spec #19 §3.1") | Medium | 1 | ✅ Resolved — May 14, 2026 (v0.2 fix pass): TBD-NORMATIVE added to all three citations; §9.4.1 blocker list extended |
| ERR-018-008 | §3.9.1 ±20% `[EST]`→`[GT]` promotion tolerance untagged; not in §3.10 constants catalogue (CLAUDE.md requires source tag on every constant) | Medium | 1 | ✅ Resolved — May 14, 2026 (v0.2 fix pass): `[GT]` tag added inline; §3.10 and §8.4 rows added |
| ERR-018-009 | FR-PO-070 (Stage 0 MUST) requires `tools/run-perf-local.sh` to invoke `tools/budget-auditor.py`, which is a Stage 0+1 deliverable per §7.1 — bootstrapping contradiction | Medium | 2 | ✅ Resolved — May 14, 2026 (v0.2 fix pass): FR-PO-070 stage column updated to "Stage 0 (manual) / Stage 0+1 (automated)" with qualifier note |
| ERR-018-010 | Appendix F.1 `N=100` captures `[GT]` and Appendix F.5 1% flake-rate threshold are governance constants absent from §3.10 catalogue; F.5 threshold also untagged | Medium | 1 | ✅ Resolved — May 14, 2026 (v0.2 fix pass): §3.10 and §8.4 rows added; F.5 threshold tagged `[GT]` |
| ERR-018-011 | `SPEC_INDEX.md` row 18 still shows `IN PROGRESS`; #18 §9.4 prematurely declares `IN REVIEW` (canonical registry contradicted per CLAUDE.md "SPEC_INDEX.md is the canonical source of truth") | Medium | 3 | ✅ Resolved — May 14, 2026 (v0.2 fix pass): SPEC_INDEX.md row 18 updated to `IN REVIEW`; CLAUDE.md and file-manifest.md updated atomically |

---

## ERR-001: `IBallPhysicsCallback` fragments a single operation into four methods

**Severity:** Major
**Detected:** February 19, 2026
**Root Cause:** Interface written by producer (First Touch) to describe what it provides
to Ball Physics, rather than by the consumer (Ball Physics) to describe what it needs.
The four methods encode First Touch's internal `TouchResult` taxonomy into Ball Physics,
creating coupling between two systems that should be independent.

**Problem in detail:**
`IBallPhysicsCallback` defines four methods:
- `OnControlled(agentID, position, velocity)`
- `OnLooseBall(position, velocity)`
- `OnDeflected(position, deflectionVelocity)`
- `OnIntercepted(interceptingAgentID, position, velocity)`

All four do the same physical thing: set ball position and velocity. The method name
encodes why First Touch is calling — which is First Touch's concern, not Ball Physics'.
Ball Physics does not and should not change its behaviour based on which `TouchResult`
produced the call. Teaching Ball Physics about `TouchResult` states via method names
is inverted responsibility.

**Correct approach:**
Single method: `SetBallState(Vector3 position, Vector3 velocity)`
First Touch calls it once with the computed position and velocity regardless of outcome.
Ball Physics applies the state. The `TouchResult` outcome is First Touch's internal
classification and stays there.

**Files requiring revision:**

| File | Section | Change |
|------|---------|--------|
| `First_Touch_Spec_Section_4_v1_0.md` | §4.5.2 | Remove `IBallPhysicsCallback` interface definition; replace 4-method calls with single `SetBallState(position, velocity)` call in `ApplyTouchResult()`; update §4.5 interface table entry; update flow diagram ASCII art at §4.4 |
| `First_Touch_Spec_Outline_v1_0.md` | Interface contracts table | Remove `IBallControlCallback` row; replace with `SetBallState()` direct call note |

**Version impact:** `First_Touch_Spec_Section_4_v1_0.md` → v1.1

---

## ERR-002: `StringIDs` papers over an undesigned event bus with the wrong solution

**Severity:** Moderate
**Detected:** February 19, 2026
**Root Cause:** Premature optimisation for a system (Event Bus) that has not yet been
designed. The `StringIDs` pattern assumes the Event Bus will dispatch on string keys and
pre-hashes them to avoid runtime allocation. This assumption may be wrong.

**Problem in detail:**
`Master_Vol_4_Tech_Implementation.md` specifies a `StringIDs` static class that
pre-hashes string constants (player names, tactic names) to `int32` at startup:

```csharp
public static class StringIDs {
    public static readonly int TACTIC_GEGENPRESS = Hash("Gegenpressing");
}
```

This pattern only makes sense if the Event Bus dispatches on string keys. If the Event
Bus uses typed event structs (the standard C# pattern: `EventBus.Publish<TEvent>(evt)`),
dispatch is on the type identity — zero strings, zero hashing, zero `StringIDs` class
needed. The `StringIDs` solution solves the wrong problem.

**Correct approach:**
Remove `StringIDs`. Document that the Event Bus will use typed event structs. String
hashing is a last resort for systems that cannot use typed dispatch (e.g., scripting
bridges, serialised network events). Those cases, if they arise, are addressed when
the Event System (Spec #17) is designed.

**Files requiring revision:**

| File | Section | Change |
|------|---------|--------|
| `Master_Vol_4_Tech_Implementation.md` | `StringIDs` section | Remove class definition and example; replace with note: "Event Bus dispatches on typed structs. String-keyed dispatch is not used. String hashing deferred pending Event System Spec #17 design." |

**Version impact:** `Master_Vol_4_Tech_Implementation.md` → minor revision

---

## ERR-003: `PerformanceContext` violation mandate imposes governance with no Stage 0 benefit

**Severity:** Moderate
**Detected:** February 19, 2026
**Root Cause:** Legitimate Stage 4 architecture (`PerformanceContext` modifier chain)
given an enforcement rule that designates direct attribute access as a "specification
violation" — in a stage where the gateway is a passthrough multiplying by 1.0.

**Problem in detail:**
`Agent_Movement_Spec_Section_3_2_v1_0.md` §3.2.1 contains:

> "Any specification that evaluates a player attribute for gameplay purposes MUST call
> `EvaluateAttribute()` or `EvaluateAttributePair()`. Direct access to raw attribute
> values for gameplay calculations is a **specification violation**."

`PerformanceContext` and `EvaluateAttribute()` are correct long-term architecture — in
Stage 4, a rated-18 player performing like a 13 during a bad season is a genuinely
valuable simulation feature. The gateway earns its existence.

The problem is the **violation designation**. Calling `EvaluateAttribute(18)` in Stage 0
returns exactly `18.0f`. The mandate forces every spec (all 20) to import, instantiate,
and route through `PerformanceContext` for a multiply-by-one operation, on pain of
being in violation. This governance overhead is disproportionate to Stage 0 benefit.

**Correct approach:**
Keep `PerformanceContext` and `EvaluateAttribute()` — they are good architecture.
Reword the enforcement rule as a recommendation:

> "Specifications evaluating player attributes for gameplay calculations should route
> through `EvaluateAttribute()`. This enables Stage 4 form, psychology, and career
> modifiers to activate without refactoring downstream formulas."

No violation designation. Compliance by convention, not mandate.

**Files requiring revision:**

| File | Section | Change |
|------|---------|--------|
| `Agent_Movement_Spec_Section_3_2_v1_0.md` | §3.2.1 | Remove bolded violation rule; reword as recommendation |
| `Agent_Movement_Spec_Section_3_5_v1_2.md` | PerformanceContext usage note (`CRITICAL` block) | Remove `CRITICAL` designation; reword as convention note |
| `Agent_Movement_Spec_Section_3_6_v1_1.md` | Any violation reference | Remove violation language |
| `Agent_Movement_Spec_Section_3_7_v1_2.md` | Test descriptions referencing violation | Remove violation language from test pass criteria |
| `Agent_Movement_Spec_Section_4_v1_1.md` | Any violation reference | Remove violation language |
| `Agent_Movement_Spec_Section_6_v1_1.md` | Future extensions referencing enforcement | Remove violation language |
| `Agent_Movement_Spec_Section_9_Approval_Checklist.md` | Any checklist item verifying enforcement compliance | Reword as convention check, not violation check |
| `Agent_Movement_Spec_Appendices_v1_1.md` | Any enforcement reference | Remove violation language |
| `Agent_Movement_Spec_Remaining_Sections_Outline.md` | Any enforcement reference | Remove violation language |
| `First_Touch_Spec_Outline_v1_0.md` | Any PerformanceContext violation reference | Remove violation language |

**Note:** `PerformanceContext` struct definition, `EvaluateAttribute()` method, factory
methods, and all formula usage remain unchanged. Only the enforcement designation is
removed.

**Version impact:** 10 files → minor revision each (single sentence change per file)

---

## ERR-004: `IPossessionManager` and `IFirstTouchEventQueue` interface against unspecified systems

**Severity:** Major
**Detected:** February 19, 2026
**Root Cause:** Interfaces written before the systems they interface with have been
specified. Interfaces written speculatively against undesigned consumers will be
redesigned when the real consumer is specified, making the Stage 0 interface vestigial
or a constraint on the future design.

**Problem in detail:**

**`IPossessionManager`** (First Touch §4.5.4):
The spec notes: *"Implementer: PossessionManager (Spec TBD, Stage 0 stub sufficient)"*
The Stage 0 stub is one line of work. An interface written against "Spec TBD" will
either be replaced when the Possession Manager is specified, or will constrain that
spec's design to fit an interface written without knowing what the system needs to do.

**`IFirstTouchEventQueue`** (First Touch §4.5.5):
A ring buffer interface with capacity 64, connected to Event System (Spec #17, Stage 1).
The Event System has not been designed. The ring buffer capacity (64) and the
`Enqueue(FirstTouchEvent)` method shape are speculative. When Stage 1 Event System is
designed, it will define its own buffering and dispatch requirements — at which point
this interface is either replaced or becomes a constraint.

**Correct approach:**
Remove both interfaces. Replace with direct, minimal Stage 0 implementations:

- Possession: `ball.PossessingAgentId = agentId` (pending BallState amendment ERR-008)
- Event queue: comment stub — *"Event publishing deferred to Stage 1. When Event System
  (Spec #17) is designed, First Touch will implement its consumer interface here."*

Write the interfaces when both sides (First Touch and their consumers) are fully
specified. Do not write an interface when one side is "Spec TBD."

**Files requiring revision:**

| File | Section | Change |
|------|---------|--------|
| `First_Touch_Spec_Section_4_v1_0.md` | §4.5.4 | Remove `IPossessionManager` interface; replace possession assignment logic with direct `BallState` field write; update §4.5 interface table; update flow diagram |
| `First_Touch_Spec_Section_4_v1_0.md` | §4.5.5 | Remove `IFirstTouchEventQueue` interface and ring buffer specification; replace with deferred comment stub; update §4.5 interface table |
| `Agent_Movement_Spec_Section_5_v1_1.md` | Any test mocking `IFirstTouchEventQueue` | Remove or replace with stub |
| `Collision_System_Spec_Section_6_v1_1.md` | Any performance reference to event queue | Remove or note as deferred |
| `First_Touch_Spec_Section_6_v1_0.md` | Event queue in performance budget | Remove ring buffer from budget; note as deferred |

**Version impact:** `First_Touch_Spec_Section_4_v1_0.md` → v1.1 (combined with ERR-001 fix)

---

## ERR-005: `KickType` enum encodes caller intent into Ball Physics

**Severity:** Major
**Detected:** February 19, 2026
**Status:** CLOSED — resolved during audit session

**Resolution:**
`KickType` enum eliminated entirely. `Ball.ApplyKick()` signature reduced to physical
parameters only: `ApplyKick(ref BallState ball, Vector3 velocity, Vector3 spin,
int agentId, float matchTime)`. The pass type is fully encoded in the velocity and
spin vectors — Ball Physics does not need to know the caller's intent label to simulate
correct aerodynamics. Pass Mechanics maps its `PassType` to physical parameters; that
is its entire job.

**Files affected by resolution:**
- `Ball_Physics_Spec_Section_3_1_Amendment_1_v1_0.md` — drafted without `KickType`
- `Pass_Mechanics_Spec_Outline_v1_0.md` — `KickType` references are outline-only;
  will not appear in Section 3 implementation

---

## ERR-006: `Ball.ApplyKick()` referenced in Ball Physics §8 but never defined in §3.1.11

**Severity:** Critical
**Detected:** February 19, 2026
**Status:** CLOSED — Resolved in Ball_Physics_Spec_Section_3_1_v2_5.md (February 21, 2026)

**Resolution:**
`ApplyKick(ref BallState ball, Vector3 velocity, Vector3 spin, int agentId, float matchTime)`
defined at §3.1.11.2. No `KickType` parameter (ERR-005 resolution). Option B possession
model applied (ERR-008 resolution). State transitions to `AIRBORNE` or `ROLLING` on kick;
agent system observes and clears possession on its side.

**Files requiring revision:**

| File | Section | Change |
|------|---------|--------|
| `Ball_Physics_Spec_Section_3_1_v2_4.md` | §3.1.11 | Add §3.1.11.1 label to `CheckPossession()`; add §3.1.11.2 `ApplyKick()` method (no `KickType` per ERR-005 resolution); update table of contents |
| `Ball_Physics_Spec_Section_8_v1_2.md` | §8.3 reference | Update `§3.1.11.2` cross-reference to `§3.1.11.2` (or §3.1.11.3 per final subsection numbering) |

**Version impact:** `Ball_Physics_Spec_Section_3_1_v2_4.md` → v2.5

---

## ERR-007: `KickPower`, `WeakFootRating`, `Crossing` absent from `PlayerAttributes`

**Severity:** Critical
**Detected:** February 19, 2026
**Status:** CLOSED — Resolved in Agent_Movement_Spec_Section_3_5_v1_3.md (February 22, 2026)

**Resolution:**
`KickPower` (1–20), `WeakFootRating` (1–5), and `Crossing` (1–20) added to
`PlayerAttributes` struct. All 9 blocked Pass Mechanics tests (PV-006, WF-001–WF-006,
IT-004) are now unblocked.

**Files requiring revision:**

| File | Section | Change |
|------|---------|--------|
| `Agent_Movement_Spec_Section_3_5_v1_2.md` | §3.5.6 `PlayerAttributes` | Add `KickPower` (1–20), `WeakFootRating` (1–5), `Crossing` (1–20); update struct comment `Consumed by` list; update struct size estimate |

**Version impact:** `Agent_Movement_Spec_Section_3_5_v1_2.md` → v1.3

---

## ERR-008: `BallState` has no `PossessingAgentId` field; `ApplyKick()` amendment references it

**Severity:** Critical
**Detected:** February 19, 2026
**Status:** CLOSED — Option B adopted February 22, 2026. Resolved in Ball_Physics_Spec_Section_3_1_v2_5.md.

**Design Decision: Option B — Possession external to BallState**

Possession is agent state, not ball state. `BallState` is a pure physics struct; adding
`PossessingAgentId` would introduce the only agent reference in Ball Physics, violating
single responsibility. It would also create a synchronisation hazard between two systems
both tracking possession.

**Resolution:**
`ApplyKick()` transitions `ball.State` from `CONTROLLED` to `AIRBORNE` (or `ROLLING`).
The agent system observes this state transition and clears its own possession record.
Agent system is the single source of truth for possession. No `PossessingAgentId` field
added to `BallState`.

Ball_Physics_Spec_Section_3_1_v2_5.md §3.1.11.2 documents this design with full rationale.

---

## ERR-009: `PassThroughGround` / `PassThroughAerial` are redundant `KickType` values

**Severity:** Minor
**Detected:** February 19, 2026
**Status:** CLOSED — resolved during audit session

**Resolution:**
Through passes use the same aerodynamic profile as their non-through equivalents
(`PassGround` and `PassLofted` respectively). The distinction between a through ball
and a regular pass is entirely a Pass Mechanics targeting concern — the receiver
prediction model, lane detection, and lead distance calculation. Ball Physics sees
identical physics profiles. Separate `KickType` values were unnecessary.

The `KickType` enum was subsequently eliminated entirely (ERR-005), making this
resolution moot. Recorded for completeness.

---

## ERR-011: `SpatialHashGrid.Query()` ignores radius parameter — always returns fixed 3×3 neighbourhood

**Severity:** Major
**Detected:** February 23, 2026 (Shot Mechanics Spec #6 §4 cross-spec audit)
**Status:** CLOSED — Fixed in Collision_System_Spec_Section_3_v1_1.md; Query() now uses
dynamic neighbourhood sizing: `cellRadius = Ceil(radius / CELL_SIZE)`. Interim workaround in Shot Mechanics §4.4.1; root cause unfixed

**Root Cause:**

`SpatialHashGrid.Query(Vector3 position, float radius)` accepts a `radius` argument
but never reads it. The implementation unconditionally queries the 3×3 cell neighbourhood
around the query position (covering approximately ±1.5m regardless of the radius
argument passed). This was documented in the Collision System spec as a comment
("not currently used; 3×3 query is always sufficient") but the architectural consequence
for callers using larger pressure radii was not evaluated.

**Problem in detail:**

All three systems that query the spatial hash for pressure detection — Pass Mechanics,
Shot Mechanics, and First Touch — pass `PRESSURE_RADIUS_MAX = 3.0m` to `Query()`. The
call returns only entities within the fixed ±1.5m neighbourhood. Opponents at 1.6–3.0m
are invisible to the pressure model in all three specifications.

**Impact by system:**
- **Pass Mechanics (Spec #5):** `PassErrorCalculator` under-estimates pressure for shots
  taken with opponents at 1.6–3.0m. Passes executed under moderate pressure behave as if
  under no pressure.
- **Shot Mechanics (Spec #6):** Same effect on `ShotErrorCalculator`. Shots under
  moderate defensive pressure are not penalised correctly.
- **First Touch (Spec #4):** Same effect on `FirstTouchPressureEvaluator`. Ball control
  under moderate pressure is over-estimated.

**Interim workaround (applied in Shot Mechanics §4.4.1 v1.3):**

Callers must distance-filter the `Query()` result set after receiving it:

```csharp
List<AgentId> queriedEntities = SpatialHash.QueryRadius(center, PRESSURE_RADIUS_MAX, filter);
List<AgentId> nearbyOpponents = queriedEntities
    .Where(id => Vector3.Distance(center, AgentSystem.GetAgent(id).Position)
                 <= PRESSURE_RADIUS_MAX)
    .ToList();
```

This workaround is correct — the 3×3 neighbourhood is a superset of all entities within
3.0m (a 3.0m radius on 1.0m cells requires at most ±3 cells to capture; the 3×3 returns
±1 cells). **The workaround does NOT fully fix the defect** — opponents at 1.6–3.0m that
fall in cells beyond the ±1 neighbourhood are still missed. However, at normal match
density (22 agents on a 105×68m pitch), the probability of an opponent being at 1.6–3.0m
but outside the 3×3 neighbourhood is low. The workaround reduces the error but does not
eliminate it.

**Correct fix:**

`SpatialHashGrid.Query()` must compute a dynamic neighbourhood based on the radius
parameter:

```csharp
public List<int> Query(Vector3 position, float radius)
{
    int cellRadius = Mathf.CeilToInt(radius / SpatialHashConstants.CELL_SIZE);
    // Query (2*cellRadius+1)² cells instead of fixed 3×3
    for (int dy = -cellRadius; dy <= cellRadius; dy++)
    for (int dx = -cellRadius; dx <= cellRadius; dx++)
    { /* add cells */ }
}
```

For `PRESSURE_RADIUS_MAX = 3.0m` on 1.0m cells: `cellRadius = 3`, query covers 7×7 = 49
cells (vs current 9). Performance impact is negligible at N=22 agents.

**Files requiring revision:**

| File | Section | Change |
|------|---------|--------|
| `Collision_System_Spec_Section_3_v1_0.md` | §3.1.4 `Query()` implementation | Dynamic neighbourhood: `cellRadius = Ceil(radius / CELL_SIZE)`; iterate `(2*cellRadius+1)²` cells |
| `Pass_Mechanics_Spec_Section_4_v1_0.md` | §4.4.1 pressure query | Add interim workaround comment (or remove workaround once Collision System fixed) |
| `First_Touch_Spec_Section_4_v1_1.md` | §4.4 pressure query | Add interim workaround comment |

**Version impact:** `Collision_System_Spec_Section_3_v1_0.md` → v1.1 (when fixed)

---

## Revision Summary

| Priority | ERR ID | Blocking | Status |
|----------|--------|----------|--------|
| ~~1 — Fix before Section 3~~ | ERR-006, ERR-007, ERR-008 | ~~Yes~~ | ✅ All three closed |
| ~~2 — Fix before approval~~ | ERR-001, ERR-004 | ~~Yes~~ | ✅ Both closed in First_Touch_Spec_Section_4_v1_1.md |
| 3 — Fix at convenience | ERR-002, ERR-003 | No | Open — minor edits to Master_Vol_4 and Agent Movement §3.2 |
| **2 — Fix before Collision System approval** | **ERR-011** | **Yes (blocks Collision System §4 approval)** | **Closed — fixed in Collision_System_Spec_Section_3_v1_1.md (Mar 5, 2026)** |
| 3 — Fix at convenience before Shot Mechanics final sign-off | ERR-010 | No | ✅ Closed — fixed in shot-mechanics/section-1.md v1.2 (March 6, 2026) |
| 3 — Fix at convenience | ERR-012 | No | ✅ Closed — fixed in first-touch/section-7.md v1.1 (March 5, 2026) |

**All critical Shot Mechanics cross-spec audit defects resolved (A1–A7). ERR-011 is a
Collision System defect with an interim workaround applied — it blocks Collision System
Section 3 revision, not Shot Mechanics approval. ERR-010 is a minor documentation
error (Decision Tree spec number) in Shot Mechanics §1.1 — non-blocking on approval.**

---

**v1.4 Changes (Mar 5, 2026):
- ERR-009 (SpatialHash Query) renumbered to ERR-011 to resolve duplicate ID
  conflict with ERR-009 (KickType, closed). ERR-011 now CLOSED.

End of Error Log v1.4**

---

## ERR-012: First Touch §7 refers to Decision Tree as Spec #7 (5 occurrences)

**Severity:** Minor (documentation error; no architectural impact)
**Detected:** March 5, 2026
**Detected During:** First Touch Specification #4 comprehensive audit
**Root Cause:** Same as ERR-010 — First Touch Section 7 was written before the specification
numbering was finalised. Decision Tree was tentatively #7; Perception System was subsequently
inserted at #7, bumping Decision Tree to #8.

**Problem in detail:**
`First_Touch_Spec_Section_7_v1_0.md` references "Decision Tree Spec #7" in 5 locations:
- §7.1.4 body text: "Decision Tree (Spec #7, Stage 1)"
- §7.2.4 body text: "Decision Tree (Spec #7, Stage 1/2 scope)"
- §7.2.4 dependency line: "Decision Tree Spec #7"
- §7.6 dependency map row: "Decision Tree Spec #7 | Intent flag | Stage 1"
- §7.6 dependency map row: "Decision Tree Spec #7 | Intent flag | Stage 2"

**Correct approach:**
Replace all 5 instances of "Spec #7" (referring to Decision Tree) with "Spec #8".

**Status:** ✅ CLOSED — Fixed in `first-touch/section-7.md` (March 5, 2026, First Touch
comprehensive audit remediation).

**Files revised:**

| File | Section | Change |
|------|---------|--------|
| `first-touch/section-7.md` (was v1.0 → v1.1) | §7.1.4, §7.2.4, §7.6 | All "Decision Tree Spec #7" → "Decision Tree Spec #8" |

**Version impact:** `first-touch/section-7.md` → v1.1

---

*End of Spec Error Log v1.5 — April 22, 2026. Add new entries after this line.*

---

## ERR-016-001: Phantom interface risk in Deterministic Simulation Spec #16 §4.2

**Severity:** Medium (architectural discipline; no immediate code impact — Stage 0 spec phase)
**Detected:** May 2, 2026
**Detected During:** Deterministic Simulation Spec #16 drafting (adversarial review + v0.7 fix pass)
**Root Cause:** Same root cause as ERR-001 and ERR-004. §4.2 originally contained normative C#-shaped interface sketches (`IDeterministicRngService`, `IReplayRunner`, etc.) against consumer specs (#17 Event System, #18 Performance Optimization, #19 Testing Strategy) that are all currently `NOT STARTED`. Writing normative interface shapes before the consumer is specified creates phantom interfaces that constrain future design.

**Mitigation applied (v0.7 fix pass):**
§4.2 was reframed as explicitly **non-normative sketches** — the C# shapes are illustrative only. The §4.2.1 *behavior contract* remains normative (determinism in inputs→outputs, byte-idempotent serialization, canonical ordering in Compare output). The note at the top of §4.2 explicitly cites CLAUDE.md's "write interfaces only when both sides are specified" rule and the ERR-001/004 hazard, and prohibits promotion to normative `.cs` interfaces until consumer specs #17/#18/#19 reach at least `IN REVIEW`.

**Status:** ✅ MITIGATED — phantom interface risk contained by non-normative classification. Full resolution requires co-authoring final interface shapes with specs #17/#18/#19.

**Files revised:**

| File | Section | Change |
|------|---------|--------|
| `docs/specs/deterministic-sim/section-4.md` | §4.2 preamble | Added non-normative disclaimer and phantom-interface hazard citation |

---

*End of Spec Error Log v1.6 — May 2, 2026.*

---

## ERR-016-002: EntityId no-reuse cross-spec constraint not back-propagated

**Severity:** Medium (consistency/discipline; latent integrity hazard if specs #2/#8 silently reuse EntityIds during a match)
**Detected:** May 3, 2026
**Detected During:** Deterministic Simulation Spec #16 third-pass adversarial critique (finding M-F)
**Root Cause:** Deterministic Simulation §3.2.5 declares a normative constraint binding two already-APPROVED specs:

> "entity allocators in Agent Movement (#2) and the AI subsystem (Decision Tree #8) MUST guarantee EntityId uniqueness for the lifetime of a match; once an EntityId is despawned it MUST NOT be reassigned."

This is the renumbering-cascade hazard CLAUDE.md flags: a downstream spec adding a normative constraint to upstream specs after they have been approved, without filing reciprocal `XC-` cross-references in those specs. As of May 3, 2026, neither Agent Movement (#2) nor Decision Tree (#8) carries a corresponding `XC-` reference to Deterministic Simulation §3.2.5; the constraint is "floating".

**Problem in detail:**
- Agent Movement #2 was approved Apr 27, 2026.
- Decision Tree #8 was approved Apr 27, 2026 (at draft-level rigor).
- The EntityId no-reuse constraint is necessary for #16's RNG stream isolation and replay parity, but is unenforceable until specs #2 and #8 explicitly carry it.
- Without back-propagation, an implementer of Agent Movement could legitimately recycle a despawned EntityId to a new agent on the same tick. This would silently break per-stream RNG cursor isolation in Deterministic Simulation, manifesting only as a hard desync at replay time.

**Required fix:**
1. Add an `XC-002-NNN` cross-reference in Agent Movement #2 §3 (entity allocator) citing Deterministic Simulation §3.2.5; declare the no-reuse constraint normatively in #2's own constants/contracts.
2. Add an `XC-008-NNN` cross-reference in Decision Tree #8 (subsystem entity allocation, if any) likewise.
3. File the back-propagation as a minor revision of both specs, version-bumped (no behavioral changes; constraint is consistent with how a sane allocator would behave anyway).
4. Once both reciprocal references exist, mark this entry CLOSED.

**Status:** RESOLVED (spec text only) — May 6, 2026. Reciprocal cross-spec constraints landed in:
- Agent Movement #2 §2.5 as `XC-002-001` (v1.1.1, non-behavioral patch).
- Decision Tree #8 §1.7.3 as `XC-008-001` (v1.1.1, non-behavioral patch).

Outstanding follow-up: update `docs/specs/deterministic-sim/section-3.md` §3.2.5 prose from "filed for back-propagation" to "back-propagated to #2 §2.5 and #8 §1.7.3". Tracked under the same ERR-016-002 entry but de-listed from CLAUDE.md "Open Issues" once that prose update is committed.

**Files revised:**

| File | Section | Change |
|---|---|---|
| `docs/specs/agent-movement/section-1-2.md` | New §2.5 | `XC-002-001` (EntityId no-reuse). v1.1.1 patch. |
| `docs/specs/decision-tree/section-1.md` | New §1.7.3 | `XC-008-001` (EntityId no-reuse). v1.1.1 patch. |
| `docs/specs/deterministic-sim/section-3.md` §3.2.5 | post-fix prose | Pending: update "filed for back-propagation" line. |

**Version impact:** Patch revision (v1.1 → v1.1.1) of Agent Movement #2 and Decision Tree #8 — no behavioral change; constraint formalizes existing sensible allocator behavior.

---

## ERR-017-001: `DOMAIN_TAG_EVENT_LEDGER` allocation required in Deterministic Simulation #16 §3.4

**Severity:** Medium (cross-spec back-prop; latent if not landed before #17 IN REVIEW)
**Detected:** May 12, 2026
**Detected During:** PASS 2 adversarial review of `event-system/outline-detailed.md` v1.0 (finding 3)
**Root Cause:** Event System #17 §3.4.2 declares the `Events`-phase digest preimage as `SerializeCanonical(DOMAIN_TAG_EVENT_LEDGER ‖ EventLedgerRecord[T])`. This domain-tag entry is normatively owned by Deterministic Simulation #16 §3.4's domain-tag table, but no allocation exists there. There is no documented mechanism by which a downstream spec registers a domain-tag need with #16; the dependency direction (#17 cites #16) makes this a chicken-and-egg.

**Problem in detail:**
- Spec #17 needs a stable numeric `DOMAIN_TAG_EVENT_LEDGER` to commit its FM-017-001 formula to.
- Spec #16 §3.4 currently does not enumerate `EVENT_LEDGER` among its allocated domain tags.
- Without back-prop, #17 cannot reach `APPROVED` (its `[CROSS-PENDING]` constant cannot promote to `[CROSS]`).
- The same hazard class as ERR-016-002 (downstream spec adds normative constraint on upstream after the upstream's review pass).

**Required fix:**
1. At `event-system/outline-detailed.md` reaching IN REVIEW, file a patch to `docs/specs/deterministic-sim/section-3.md` §3.4 domain-tag table allocating `DOMAIN_TAG_EVENT_LEDGER` (next available numeric value in #16's tag-namespace).
2. Update §3.10 constants catalogue in `event-system/outline-detailed.md` (and any drafted §3 section file) to pin the literal value and promote `[CROSS-PENDING]` → `[CROSS]` at the same beat that resolves the citation's `TBD-NORMATIVE` tag (gated on #16 reaching `APPROVED` per KD-2).
3. Once the allocation lands in #16, mark this entry CLOSED.

**Status:** OPEN — May 12, 2026. Patch deferred to #17 IN REVIEW commit. Tracked here so the allocation is not forgotten during section-file authoring.

**Files requiring revision:**

| File | Section | Change |
|---|---|---|
| `docs/specs/deterministic-sim/section-3.md` | §3.4 domain-tag table | Add `DOMAIN_TAG_EVENT_LEDGER` row with allocated numeric value |
| `docs/specs/event-system/section-3.md` (when authored) | §3.10 constants catalogue | Pin literal value; promote `[CROSS-PENDING]` → `[CROSS]` post-#16 APPROVED |
| `docs/specs/event-system/section-3.md` (when authored) | §3.4.2 FM-017-001 | Inline literal value in worked example (Appendix B) |

**Version impact:** Patch revision of #16 once allocation lands (no behavioral change; pure namespace allocation). Spec #17 carries the dependency natively.

---

*End of Spec Error Log v1.8 — May 12, 2026.*

---

## ERR-010: Shot Mechanics §1.1 refers to Decision Tree as Spec #7

**Severity:** Minor (documentation error; no architectural impact)  
**Detected:** February 27, 2026  
**Detected During:** Decision Tree Specification #8 Outline v1.1 pre-approval review (BLK-001)  
**Root Cause:** Shot Mechanics Specification #6 was written before the specification
numbering was finalised. At time of authoring, the Decision Tree was tentatively
assigned #7. Perception System was subsequently inserted at #7, bumping Decision Tree
to #8. The Shot Mechanics text was not updated.

**Problem in detail:**  
`Shot_Mechanics_Spec_Section_1_v1_1.md` §1.1 Dependencies section references:
> "Decision Tree Specification #7"

The canonical specification number for the Decision Tree, as recorded in
`PROGRESS.md` (authoritative), `FILE_MANIFEST.md`, and Perception System
Specification #7 §1.1, is **#8**.

This creates an inconsistency that could mislead implementers cross-referencing
Shot Mechanics with Decision Tree documentation.

**Correct approach:**  
Replace all instances of "Decision Tree Specification #7" with "Decision Tree
Specification #8" in `Shot_Mechanics_Spec_Section_1_v1_1.md`.

**Blocking condition:**  
This error is non-blocking on Shot Mechanics approval (the architectural content is
correct; only the number is wrong). However, it **must be closed before**:
1. Shot Mechanics receives final lead developer sign-off, and
2. Decision Tree Specification #8 Section 4 (interface contracts) is written and
   references Shot Mechanics as a dependency by number.

**Files requiring revision:**

| File | Section | Change |
|------|---------|--------|
| `Shot_Mechanics_Spec_Section_1_v1_1.md` | §1.1 Dependencies table, any other references | Replace "Spec #7" with "Spec #8" for Decision Tree |

**Version impact:** No version increment required for minor text correction. Document
in Shot Mechanics changelog when the edit is made.

---

## ERR-018-002: `[HotPathAllocExempt]` cited as declared in Spec #20 §3 but does not exist there

**Status:** ✅ Resolved — May 14, 2026 (#18 section-file v0.2; option-2 path; Spec #20 not touched).
**Severity:** High (citation of APPROVED spec for content it does not contain — matches CLAUDE.md "fabricated checklist values" hazard class)
**Detected:** May 14, 2026
**Detected During:** PASS-1 adversarial review of Performance Optimization #18 section files v0.1
**Root Cause:** The `[HotPathAllocExempt]` C# attribute is referenced as a key allocation-exemption mechanism in five locations in #18, every one of which treats the attribute as already declared in Spec #20 §3 (APPROVED May 11, 2026). Grep against the entire `code-standards/` folder returns zero hits for `HotPathAllocExempt` or any allocation-exemption attribute. The attribute is not declared in Spec #20.

**Problem in detail:**

Cited locations:
- `section-2.md` FR-PO-053: "exempt via `[HotPathAllocExempt]` (declared in Spec #20 §3, cite-not-redefine per KD-1)"
- `section-3.md` §3.1.2: "exempted via `[HotPathAllocExempt]` (cite Spec #20 §3)"
- `section-3.md` §3.7.5: "exempted via the `[HotPathAllocExempt]` attribute declared in Spec #20 §3"
- `section-8.md` §8.1.4: "§3 `[HotPathAllocExempt]` attribute (cited by §3.7.5, FR-PO-053)"
- `appendices.md` Appendix B: "Exemptions require `[HotPathAllocExempt]` per Spec #20 §3"

§3.7.5 itself hedges with "Coordinate with the #20 author if the attribute is not yet declared … attribute presence to be verified at first `src/` commit," which directly contradicts the surrounding "declared in Spec #20 §3" claim. The spec is simultaneously asserting the attribute exists in #20 and acknowledging it may not.

**Required fix (choose one):**

1. **Update Spec #20 §3** to formally declare the `[HotPathAllocExempt]` attribute with version-history entry and lead-developer re-sign-off (Spec #20 is APPROVED; any spec change requires sign-off per CLAUDE.md). Spec #18 citations then resolve.
2. **Move ownership to Spec #18** — declare the attribute in #18 §3.7 directly; drop the KD-1 cite-not-redefine framing for this case. Update Spec #20's `[HotPathAllocExempt]` row only if/when #20 adopts it.
3. **Tag as `[CROSS-PENDING]`** — treat the attribute name as a cross-spec constant gated on a future Spec #20 patch; file the back-prop expectation here and in #18's body text.

Option (2) has the smallest cross-spec blast radius because #20 is APPROVED and (1) would require re-review.

**Files requiring revision (per resolution path chosen):**

| File | Section | Change |
|---|---|---|
| `docs/specs/performance-optimization/section-2.md` | FR-PO-053 | Reword to remove "declared in Spec #20 §3" claim |
| `docs/specs/performance-optimization/section-3.md` | §3.1.2, §3.7.5 | Same |
| `docs/specs/performance-optimization/section-8.md` | §8.1.4 | Same |
| `docs/specs/performance-optimization/appendices.md` | Appendix B | Same |
| `docs/specs/code-standards/section-3.md` (option 1 only) | §3 | Add attribute declaration |

**Version impact:** #18 section-file revision (v0.1 → v0.2). Option (1) additionally bumps Spec #20 (re-review required).

**Resolution (May 14, 2026):** Option (2) applied. `section-3.md` §3.7.5, `section-2.md` FR-PO-053, and `appendices.md` Appendix B all updated. `[HotPathAllocExempt]` declared as Spec #18 §3.7.5 governance identifier. Spec #20 unchanged.

---

## ERR-018-003: MUST/MAY conflict between FR-PO-067 and §3.4.4 on baseline-reproducibility re-run

**Status:** ✅ Resolved — May 14, 2026 (#18 section-file v0.2; §3.4.4 upgraded MAY → MUST with Stage 0 carve-out).
**Severity:** High (binding-requirement contradiction within the same spec)
**Detected:** May 14, 2026
**Detected During:** PASS-1 review of #18 section files v0.1
**Root Cause:** FR-PO-067 in `section-2.md §2.2.9` states the baseline-reproducibility auditor **MUST** re-run the recorded session manifest. §3.4.4 in `section-3.md` (the implementing mechanics section for that FR) states the validator **MAY** re-run the session. §2 is the binding-requirement section; §3 is the implementing mechanics. The verbs disagree directly on the same action.

**Problem in detail:**

FR-PO-067 (normative MUST): *"The §5.4 baseline-reproducibility auditor MUST re-run the recorded session manifest and confirm the recaptured metric matches within §3.4.3 confidence interval."*

§3.4.4 (mechanics MAY): *"Reproducibility check (Stage 0+1): the validator MAY re-run the session under the recorded seed + fingerprint + platform pin and confirm the captured metric matches within the §3.4.3 confidence interval."*

FR-PO-068 makes failure to re-run a merge-blocking event. The §3.4.4 "MAY" would allow the validator to silently skip the check without triggering FR-PO-068's block.

**Required fix:**

Either upgrade §3.4.4 to "MUST re-run" (aligning §3 with §2's binding requirement), or downgrade FR-PO-067 to SHOULD (aligning §2 with §3's permissive mechanic). FR-PO-068's merge-blocking semantics push toward the MUST resolution.

**Files requiring revision:**

| File | Section | Change |
|---|---|---|
| `docs/specs/performance-optimization/section-3.md` | §3.4.4 | "MAY" → "MUST" (recommended) |

**Version impact:** #18 section-file revision (v0.1 → v0.2).

**Resolution (May 14, 2026):** `section-3.md` §3.4.4 "MAY" → "MUST". FR-PO-067 (MUST) and §3.4.4 (now MUST) are consistent.

---

## ERR-018-004: Three-way stage-of-resolution contradiction on +5% threshold (FR-PO-031 / §7.5 D9 / §7.1)

**Status:** ✅ Resolved — May 14, 2026 (#18 section-file v0.2; §7.5 D9 re-anchored Stage 0+1 to match FR-PO-031 and §7.1).
**Severity:** High (three locations in the same spec state three different resolution stages for the same governance number)
**Detected:** May 14, 2026
**Detected During:** PASS-1 review
**Root Cause:** The +5% per-PR regression threshold (`[GT]` governance number) has its resolution stage stated three times with three different answers.

**Problem in detail:**

- **FR-PO-031** (`section-2.md §2.2.5`): "`[GT]` pinned at Stage 0+1 §7.5 D9" — implies pin at Stage 0+1.
- **§7.5 D9** (`section-7.md`): "Resolution stage: Stage 1 | Notes: Tie to first-month variance measurement" — explicit Stage 1.
- **§7.1** (`section-7.md`) Stage 0+1 Transition Deliverables: "§3.5.2 +5% threshold re-evaluated against actual baseline variance" — listed as Stage 0+1 deliverable.

The three statements cannot all be true. Either the threshold is pinned/re-evaluated at Stage 0+1 (FR-PO-031 + §7.1) and D9 is wrong, or D9 is correct and FR-PO-031 + §7.1 are wrong.

**Required fix:**

Choose one canonical stage and update all three locations. Recommended: Stage 0+1 (matches FR-PO-031 + §7.1 which jointly outvote D9; matches the operational reality that you can't gate Stage 0+1 CI on a Stage-1 threshold).

**Files requiring revision:**

| File | Section | Change |
|---|---|---|
| `docs/specs/performance-optimization/section-7.md` | §7.5 D9 | "Stage 1" → "Stage 0+1" (under recommended resolution) |

**Version impact:** #18 section-file revision (v0.1 → v0.2).

**Resolution (May 14, 2026):** `section-7.md` §7.5 D9 resolution stage changed from "Stage 1" to "Stage 0+1". All three locations (FR-PO-031, §7.1, §7.5 D9) now consistently state Stage 0+1.

---

## ERR-018-005: Channel registry schema absent from Appendix F; §3.8.2 "Stage 0 declares schema" obligation unmet

**Status:** ✅ Resolved — May 14, 2026 (#18 section-file v0.2; new **Appendix F.0 Channel Registry Schema** authored with 12 schema fields; §3.8.2 channel-registry bullet rewritten to cite F.0 as the Stage 0 schema deliverable).
**Severity:** High (declared Stage 0 deliverable is missing; channel names used without registry backing)
**Detected:** May 14, 2026
**Detected During:** PASS-1 review
**Root Cause:** §3.8.2 in `section-3.md` explicitly states the channel registry is a Stage 1 deliverable but the **schema** for the registry is a Stage 0 deliverable to be published in Appendix F. Appendix F as written contains only F.1–F.5 dashboard schemas; there is no channel registry schema. Compounding this, F.1, F.2, and F.4 reference channel names (`perf.budget`, `perf.alloc`) as data sources without those channels having registry entries.

**Problem in detail:**

§3.8.2: *"Channel registry. Named channels per subsystem, declared in Appendix F catalogue (Stage 1 deliverable; **Stage 0 declares schema**)."*

Appendix F section headings: F.1 Per-Spec Per-Tick Budget Dashboard, F.2 Per-PR Delta Dashboard, F.3 Milestone-Baseline Trend Dashboard, F.4 Allocation-Tracker Dashboard, F.5 Flake/Determinism Cross-Reference Dashboard. All five are dashboard schemas; none is a channel registry schema. No section in Appendix F defines what fields a channel registry entry carries (channel name, owning subsystem, default verbosity level, sampling rule, sink routing, determinism class, etc.).

**Required fix:**

Author an "Appendix F.0 — Channel Registry Schema" (or "Appendix H — Channel Registry Schema") before F.1, declaring the schema fields per channel entry. Stage 0 deliverable; populated entries are Stage 1.

**Files requiring revision:**

| File | Section | Change |
|---|---|---|
| `docs/specs/performance-optimization/appendices.md` | New Appendix F.0 / H | Add channel registry schema headers (channel name, subsystem, verbosity, sampling rule, sink, determinism class) |

**Version impact:** #18 appendices revision (v0.1 → v0.2).

**Resolution (May 14, 2026):** Appendix F.0 "Channel Registry Schema" added to `appendices.md` with full field schema (channel_name, subsystem_owner, verbosity_tier_min, sink_targets, emission_veto_required, record_format, declared_stage) and Stage 0 channel registry table with three seed entries (perf.budget, perf.alloc, perf.trace).

---

## ERR-018-006: Hot-path allocation budget = 0 bytes/tick tagged `[GT]` instead of `[FIXED]` in §3.10

**Status:** ✅ Resolved — May 14, 2026 (#18 section-file v0.2; §3.10 row re-tagged `[GT]` → `[FIXED]`; §8.4 mirror row updated).
**Severity:** Medium (constant-tag misclassification; implies designer-tunability of an architectural mandate)
**Detected:** May 14, 2026
**Detected During:** PASS-1 review
**Root Cause:** `section-3.md` §3.10 tags "Hot-path allocation budget = 0 bytes/tick" as `[GT]`. Per CLAUDE.md "Constant Tags" table, `[GT]` = "Gameplay-Tuned; Designer sets value; must live in tunable config." The zero-allocation budget is a non-negotiable architectural mandate from CLAUDE.md "When Writing Code: zero-allocation architecture in the game loop" — not a designer-settable value. Tagging it `[GT]` creates a false implication that a game designer could change it.

**Required fix:**

Re-tag as `[FIXED]` ("invariant by project mandate") or remove from the constants catalogue entirely and treat as a pure CLAUDE.md cite. FR-PO-050's "MUST declare allocation budget = 0 bytes per tick" reinforces the non-tunable nature.

**Files requiring revision:**

| File | Section | Change |
|---|---|---|
| `docs/specs/performance-optimization/section-3.md` | §3.10 Constants Catalogue | "Hot-path allocation budget = 0 bytes/tick" tag `[GT]` → `[FIXED]` |
| `docs/specs/performance-optimization/section-8.md` | §8.4 Constant Provenance Summary | Mirror the tag change |

**Version impact:** #18 section-file revision (v0.1 → v0.2).

**Resolution (May 14, 2026):** `section-3.md` §3.10 tag updated `[GT]` → `[FIXED]`; rationale updated to "non-tunable invariant". `section-8.md` §8.4 mirrored.

---

## ERR-018-007: Three Spec #19 body-text citations missing `TBD-NORMATIVE` tag

**Status:** ✅ Resolved — May 14, 2026 (#18 section-file v0.2; `TBD-NORMATIVE` added to §3.3.5, §3.4.3, §3.9.5; §9.4.1 #19 blocker list extended).
**Severity:** Medium (KD-4 status caveat violated; §9.4.1 blocker list incomplete)
**Detected:** May 14, 2026
**Detected During:** PASS-1 review
**Root Cause:** KD-4 mandates that every Spec #19 citation in #18 carry a `TBD-NORMATIVE` tag because #19 is `IN REVIEW`. §9.4.1 enumerates blocked sections — but three #19 body-text citations are absent from that list and carry no tag.

**Problem in detail:**

1. **`section-3.md` §3.4.3:** *"provisional value 30 samples / 95% CI per Spec #19 §3.4.3 parallel convention"* — no `TBD-NORMATIVE`; not in §9.4.1.
2. **`section-3.md` §3.3.5:** *"selection criteria parallel Spec #19 §6.1 — must support deterministic re-play …"* — no `TBD-NORMATIVE`; not in §9.4.1.
3. **`section-3.md` §3.9.5:** *"owned by Spec #19 §3.1 end-to-end / soak layer for test execution"* — no `TBD-NORMATIVE`; not in §9.4.1.

All three would silently rot if #19's section numbering shifts before #18 is approved.

**Required fix:**

Add `(TBD-NORMATIVE)` parenthetical to each citation and add §3.4.3, §3.3.5, §3.9.5 to §9.4.1's #19 blocker list.

**Files requiring revision:**

| File | Section | Change |
|---|---|---|
| `docs/specs/performance-optimization/section-3.md` | §3.4.3, §3.3.5, §3.9.5 | Add `TBD-NORMATIVE` tag to each #19 citation |
| `docs/specs/performance-optimization/section-9-approval-checklist.md` | §9.4.1 | Add §3.4.3, §3.3.5, §3.9.5 to #19 blocker list |

**Version impact:** #18 section-file revision (v0.1 → v0.2).

**Resolution (May 14, 2026):** `(TBD-NORMATIVE)` added to all three citations in `section-3.md`. `section-9-approval-checklist.md` §9.4.1 #19 blocker list extended with §3.3.5, §3.4.3, §3.9.5.

---

## ERR-018-008: §3.9.1 ±20% promotion tolerance untagged and absent from constants catalogue

**Status:** ✅ Resolved — May 14, 2026 (#18 section-file v0.2; inline `[GT]` tag at §3.9.1; new ±20% row in §3.10 + §8.4 with rationale).
**Severity:** Medium (untagged constant; CLAUDE.md requires source tag on every constant in every spec)
**Detected:** May 14, 2026
**Detected During:** PASS-1 review
**Root Cause:** `section-3.md` §3.9.1 declares: *"the first Stage 0+1 baseline capture promotes the estimate to a measured value tagged `[GT]` if within ±20% of estimate, or files an `ERR-018-NNN` review finding if not."* The ±20% threshold governs whether a spec's implementation matches its design estimate — a consequential governance number. It carries no `[GT]`/`[EST]`/`[FIXED]` tag and is absent from §3.10's constants catalogue.

**Required fix:**

Add the ±20% threshold to §3.10's table with `[GT]` tag and rationale (e.g., "twice the +5% per-PR threshold for first-measurement variance"). Also add to §8.4 constant-provenance summary.

**Files requiring revision:**

| File | Section | Change |
|---|---|---|
| `docs/specs/performance-optimization/section-3.md` | §3.9.1 | Append `[GT]` tag to ±20% |
| `docs/specs/performance-optimization/section-3.md` | §3.10 | Add ±20% row with `[GT]` and rationale |
| `docs/specs/performance-optimization/section-8.md` | §8.4 | Mirror row |

**Version impact:** #18 section-file revision (v0.1 → v0.2).

**Resolution (May 14, 2026):** `[GT]` tag added inline in `section-3.md` §3.9.1. §3.10 row added: "±20% acceptance tolerance `[GT]`". `section-8.md` §8.4 mirrored.

---

## ERR-018-009: FR-PO-070 (Stage 0 MUST) requires invoking Stage 0+1 tooling

**Status:** ✅ Resolved — May 14, 2026 (#18 section-file v0.2; option (b) — FR-PO-070 split Stage 0 manual / Stage 0+1 automated; §5.2 activation row and §5.6 traceability row updated).
**Severity:** Medium (FR activation-stage / tooling-availability mismatch)
**Detected:** May 14, 2026
**Detected During:** PASS-1 review
**Root Cause:** FR-PO-070 (`section-2.md §2.2.10`) has activation stage Stage 0 and MUST-level binding: *"`tools/run-perf-local.sh` (Appendix E) MUST invoke the §5.3 schema-conformance auditor and §5.5 loop-tag auditor against `docs/specs/` only."* Appendix E's shell script invokes `python3 tools/budget-auditor.py`, which §7.1 lists as a Stage 0+1 deliverable. At Stage 0 the tool does not exist; the script as written cannot run.

**Problem in detail:**

Appendix E partially acknowledges this: *"`tools/budget-auditor.py` and `tools/perf-harness/run.sh` are Stage 0+1 deliverables (§7.1). At Stage 0 the auditor's behaviour is a manual review against §3.1.2 schema and §3.2.2 loop-tag mandate; the script above is the structure into which the automated implementation will land."* But FR-PO-070's MUST language and "Stage 0" activation do not reflect this caveat.

**Required fix:**

Either (a) move FR-PO-070 to "Stage 0+1" activation stage in §2.2.10 — matching when its tool dependencies exist — or (b) keep at Stage 0 but qualify the MUST to "MUST execute the manual review equivalents of the schema-conformance and loop-tag auditors per §5.3 and §5.5."

**Files requiring revision:**

| File | Section | Change |
|---|---|---|
| `docs/specs/performance-optimization/section-2.md` | §2.2.10 FR-PO-070 | Move to Stage 0+1, or qualify Stage 0 manual interpretation |
| `docs/specs/performance-optimization/section-5.md` | §5.2 Stage-Gated Activation Table | Update FR-PO-069 … 074 row if FR-PO-070 stage shifts |

**Version impact:** #18 section-file revision (v0.1 → v0.2).

**Resolution (May 14, 2026):** FR-PO-070 stage column updated to "Stage 0 (manual) / Stage 0+1 (automated)" with qualifier note clarifying Stage 0 uses manual audit execution per Appendix E template.

---

## ERR-018-010: Appendix F.1 N=100 and F.5 1% flake-rate thresholds absent from §3.10

**Status:** ✅ Resolved — May 14, 2026 (#18 section-file v0.2; both values added to §3.10 + §8.4 with rationale; Appendix F.5 inline `[GT]` tag appended).
**Severity:** Medium (governance constants outside the declared constants catalogue; F.5 also untagged)
**Detected:** May 14, 2026
**Detected During:** PASS-1 review
**Root Cause:** §3.10 declares itself the constants catalogue for #18's governance numerics. Appendix F (`appendices.md`) introduces two governance numbers not present in §3.10:

- **F.1:** "per-spec p50/p99 over last **N=100** captures (`[GT]`, pinned at Stage 0+1)."
- **F.5:** "flake rate **> 1%** triggers boundary-defect routing (§5.7.3)." — untagged.

§3.10's evidence-artifact convention says each `[GT]` value's evidence is the section-file path that introduces it; these two values introduce themselves in Appendix F but are not catalogued.

**Required fix:**

Add both values to §3.10 (and §8.4 mirror) with tags and rationale. F.5's threshold needs a tag (`[GT]` likely).

**Files requiring revision:**

| File | Section | Change |
|---|---|---|
| `docs/specs/performance-optimization/section-3.md` | §3.10 | Add `N=100 captures` row (`[GT]`, Appendix F.1) and `1% flake-rate threshold` row (`[GT]`, Appendix F.5) |
| `docs/specs/performance-optimization/section-8.md` | §8.4 | Mirror both rows |
| `docs/specs/performance-optimization/appendices.md` | Appendix F.5 | Append `[GT]` tag to "> 1%" |

**Version impact:** #18 section-file revision (v0.1 → v0.2).

**Resolution (May 14, 2026):** `section-3.md` §3.10 rows added for N=100 and 1% flake-rate. `section-8.md` §8.4 mirrored. `appendices.md` F.5 "> 1%" tagged `[GT]`.

---

## ERR-018-011: `SPEC_INDEX.md` row 18 not updated; §9.4 prematurely claims `IN REVIEW`

**Status:** ✅ Resolved — May 14, 2026 (#18 section-file v0.2; option (a) — `SPEC_INDEX.md` row 18 + CLAUDE.md OPEN ISSUES + `file-manifest.md` row 18 all flipped to `IN REVIEW` atomically; §9.3 atomic-update checkbox flipped `[x]` for the `IN PROGRESS → IN REVIEW` transition; `IN REVIEW → APPROVED` flip remains the future atomic update with lead-developer sign-off).
**Severity:** Medium (canonical-registry contradiction; CLAUDE.md says SPEC_INDEX.md is the source of truth on status)
**Detected:** May 14, 2026
**Detected During:** PASS-1 review
**Root Cause:** `section-9-approval-checklist.md` §9.4 declares *"Status: `IN REVIEW` (author-driven flip; lead-developer review pending)."* `SPEC_INDEX.md` row 18 still shows `IN PROGRESS`. CLAUDE.md states: *"SPEC_INDEX.md is the canonical source of truth for spec numbers, folder names, and approval status."* By that rule, the spec is `IN PROGRESS`, regardless of what §9.4 claims. CLAUDE.md OPEN ISSUES entry for #18 also still says "Section files remain stubs," which is no longer accurate.

**Problem in detail:**

§9.3 checklist row *"`SPEC_INDEX.md` status updated atomically with sign-off"* is correctly marked `[ ]` (unchecked) — acknowledging the update hasn't happened. But §9.4's Decision block then asserts `IN REVIEW` as the current status. The §9.4 status claim contradicts both the canonical registry and the unchecked §9.3 checklist row in the same file.

**Required fix:**

Either (a) update `SPEC_INDEX.md` row 18 and CLAUDE.md OPEN ISSUES entry to `IN REVIEW` atomically (the section files are authored — this state would be consistent), or (b) revert §9.4's status claim to `IN PROGRESS` until lead-developer sign-off. The status flip and the registry/CLAUDE.md updates must move together.

**Files requiring revision:**

| File | Section | Change |
|---|---|---|
| `docs/specs/SPEC_INDEX.md` | Row 18 | `IN PROGRESS` → `IN REVIEW` (option a) |
| `CLAUDE.md` | OPEN ISSUES entry for #18 | Update "Section files remain stubs" → "Section files drafted at v0.1; PASS-1 adversarial review filed (ERR-018-002…011); v0.2 fix pass pending"; flip status text to `IN REVIEW` |
| `docs/tracking/file-manifest.md` | #18 rows | Move section files from "stub" to "drafted" |
| `docs/specs/performance-optimization/section-9-approval-checklist.md` | §9.4 (option b alternative) | Revert "IN REVIEW" → "IN PROGRESS" |

**Version impact:** No section-file content revision required; metadata-only across three tracking files (option a). Option b is a one-line §9.4 edit.

**Resolution (May 14, 2026):** Option (a) applied. `SPEC_INDEX.md` row 18 updated `IN PROGRESS` → `IN REVIEW` with changelog entry. `CLAUDE.md` OPEN ISSUES entry for #18 updated to reflect `IN REVIEW` status and v0.2 section files. `file-manifest.md` row 18 updated from "stubs" to "section-1 through section-9-approval-checklist + appendices.md at v0.2".

---

*End of Spec Error Log v1.12 — May 14, 2026.*
