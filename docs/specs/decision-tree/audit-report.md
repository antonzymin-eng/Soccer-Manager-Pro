# Decision Tree Specification #8 — Comprehensive Audit Report

**File:** `audit-report.md`
**Created:** June 11, 2026
**Version:** 1.0
**Scope:** Combined document-and-code review — all 24 spec files in
`docs/specs/decision-tree/` against all 32 source + 7 test files in
`src/decision-tree/` (implementation landed May 29, 2026; AR-1 same day).
This is the comprehensive audit carved out as a follow-up item at the April 27, 2026
draft-level approval (root CLAUDE.md OPEN ISSUES). Implementation preceded the audit,
so the audit ran at full adversarial-review rigor over both surfaces simultaneously
(designated **AR-2** in code version histories; AR-1 was the May 29 implementation-day
review).

**Verdict:** the spec's formula layer is sound and internally well-derived, but the
implementation had never compiled, and both surfaces shared a systematic blind spot:
**every prior test and worked example used the home team**, so three independent
home/away asymmetry defects (zone modifiers, press urgency, line-depth sign) survived
spec review, implementation, and AR-1.

---

## Findings Summary: 3 H + 11 M + 9 L (all resolved this commit except one documented-open)

### High

| ID | Finding | Resolution |
|----|---------|-----------|
| H-1 | **The decision-tree assembly has NEVER compiled.** `ActionDispatcher` invoked `PassExecutor.Execute` / `ShotExecutor.Execute` as static calls; both are instance methods on per-agent stateful executors (Pass #5 §4.1 / Shot #6 §4.1) — CS0120. Additionally `EventBusRegistrar.cs` uses `TacticalDirector.DeterministicSim` (SubsystemOrdinals/PhaseId) but `decision-tree.asmdef` carried no reference (asmdef references are not transitive). Every claim of the form "the test suite enforces X" made since May 29 — including AR-1's — was unverifiable while the assembly was dead. **Sixth consecutive spec with a structurally dead build surface; first where the production assembly (not just tests) was dead.** | Executor instances injected via `DecisionTree` ctor (optional; null tolerated as test/bootstrap seam, logged) and threaded through `Dispatch`; asmdef gains the DeterministicSim reference. Sibling sweep: the same missing reference fixed in pass-mechanics / perception-system / heading-mechanics / goalkeeper-mechanics asmdefs (shot-mechanics already had it). |
| H-2 | **Away-team zone modifiers inverted** (ERR-008-002). §3.2.1.3/§2.2.5 define zones "from own goal line", but the implementation consumed the shared home-perspective `MatchContext.BallZone` for both teams. An away agent in shooting range scored SHOOT with the 0.10 DEFENSIVE modifier instead of 1.00; every zone-modified action (PASS/SHOOT/DRIBBLE/HOLD/PRESS/INTERCEPT) was wrong for away agents. The §2.2.5 data structure itself cannot satisfy its own definition for both teams, and enum-mirroring is not exact (home cut points {35, 65} mirror to {40, 70}). | Assembler derives team-relative `DecisionContext.BallZone` from `MatchContext.BallPosition.x` via new `PitchGeometry.ComputeFieldZone(posX, teamId)`; scorer reads only the derived field. §2.2.5 + §3.2.1.3 patched. Locked by `DecisionContextAssemblerTests` (incl. the x=37 mirror-boundary case). |
| H-3 | **§3.7 state machine semantics not implemented.** EXECUTING re-entered the full pipeline unconditionally every heartbeat (the `ForcedRefreshThisTick` check was a no-op ternary returning EVALUATING on both arms), so an agent mid-pass-windup re-decided and re-dispatched each tick; §3.6.3 same-ActionType forced-refresh suppression was absent; HOLD landed in EXECUTING instead of IDLE (§3.7.2 row 3); a malformed snapshot stomped any state — including EXECUTING — to IDLE; `OnInterrupt` fired from any state instead of EXECUTING only. | State machine rewritten to §3.7.2: PASS/SHOOT hold EXECUTING between heartbeats (pipeline skipped; forced refresh re-evaluates with same-type re-dispatch suppression via the new `_hasDispatchedAction` record); HOLD → IDLE; invalid snapshot preserves state; interrupts gated to EXECUTING. Stage 0 deviation (movement actions continuous; no DT→executor cancel) filed as ERR-008-008 and noted in §3.7.2. Locked by UT-33/34/35. |

### Medium

| ID | Finding | Resolution |
|----|---------|-----------|
| M-1 | §3.4.6 press urgency keyed to the literal `PossessionState.AWAY_TEAM` — "opponent" only for home agents. Away agents received the ×1.2 urgency while their OWN team possessed and never while defending. Root cause: §3.4.6 references a nonexistent `PossessionState.OPPONENT` member (ERR-008-005) and `DecisionContext.PossessedByTeam`'s doc claimed perspective semantics the enum doesn't have. | Derived `DecisionContext.OpponentHasBall` (assembler); resolver takes the flag. Spec §3.4.6 reworded. Locked by assembler + scorer tests. |
| M-2 | §3.4.5 line-depth adjustment shifted the formation slot along **Y** (touchline axis) with no team sign — players moved sideways, identically for both teams. The spec pseudocode itself carried the Y-axis error (ERR-008-003; the known "wrong coordinate axis" hazard class). Latent at Stage 0 (depth pinned 0.5). | `GetAdjustedFormationSlot(agentId, teamId)` shifts X, team-signed; §3.4.5 pseudocode rewritten. |
| M-3 | SHOOT risk used `(1 − A_Finishing)`; §3.2.3.1 defines `RiskPenalty_SHOOT = (1 − GoalOpeningScore) × P × coeff`. | Formula corrected; locked by `ShootRisk_ScalesWithBlockedGoal_NotFinishing`. |
| M-4 | SHOOT midfield long-shot gate compared raw `A_LongShots ≥ 0.75` (effective raw ≥ 16); §3.2.3.1 compares the **shifted** form `(0.5 + A×0.5) > 0.75` and §3.2.3.4 explicitly derives "effective threshold raw ≥ 11" while rejecting the raw-form reading. Players with LongShots 11–15 had midfield shots suppressed ×11. | Shifted-form comparison; `LONG_SHOT_THRESHOLD` doc updated; locked by `ShootMidfield_LongShotsRaw12_GetsLongModifier` *(refitted at ERR-008-019 to `ShootMidfield_RampRunsInShiftedForm` — under the August 5, 2026 full-range long-shot ramp no single rating sits past a band edge, so the lock asserts the computed shifted-form ratio at raw 10, which the raw-form defect would suppress to the SHORT endpoint; this lock's shifted-vs-raw intent is preserved)*. |
| M-5 | Dispatch profiles deviated from §3.5.6–3.5.8 (and the §5 UT-DP-04 contract): PRESS lacked EMERGENCY braking + TARGET_LOCK; INTERCEPT lacked ball-watching TARGET_LOCK; MOVE's near band issued `Stop` (agents never closed the last ≤6 m to their slot) instead of WALKING. `DispatcherTests` UT-20/21/22 had encoded the deviations. | New AM `MovementCommand.PressSprint` / `SprintWhileWatching` factories (WalkTo/AR-13 precedent); MOVE near band → `WalkTo`; tests re-derived to the spec profiles. |
| M-6 | INTERCEPT pressure term multiplied a `(1 − A_Anticipation)` factor not present in §3.2.8.1 (`1 − P × coeff`). | Formula corrected. |
| M-7 | `ComputeGoalOpeningScore` omitted the §3.2.3.2 step-4 angular-overlap test (any opponent in the shot corridor occluded the goal even with its angular centre outside the goal arc), used distance-to-goal-CENTRE for the GK heuristic (spec: distance to the goal LINE — a goal-line keeper wide of centre was misclassified as a 0.5 m outfield blocker), and lacked the per-opponent clamp + step-5 `GOAL_OPENING_MIN` floor. | All four geometry elements implemented per §3.2.3.2. |
| M-8 | `decision-tree.asmdef` (and four sibling asmdefs) missing the `TacticalDirector.DeterministicSim` reference their `EventBusRegistrar.cs` requires — Unity asmdef references are not transitive through EventSystem. Compile-blocking in a real Unity import; folded into H-1's resolution. | All five asmdefs patched. |
| M-9 | §3.1.3.6 requires teammates be evaluated in **proximity order** when the Decisions cap binds ("cognitive scope limit"); the implementation iterated in snapshot order with an unfiled "deferred to Stage 1" comment — low-Decisions agents dropped teammates by array position. Also leaves INV-GEN-08 (PASS ordering invariant) unmet; ordering is selection-neutral (argmax) so only the cap path was behavioral. | Closest-first selection when the cap binds (stackalloc marks; zero heap); snapshot order preserved when non-binding per §3.1.3.2. INV-GEN-08 remains a doc-level invariant on the buffer (selection is order-independent); noted here. Locked by `DecisionsCap_BindsByProximity_NotSnapshotOrder`. |
| M-10 | §3.5.9 failure modes FM-DT-10/11/12 (pre-dispatch assertions) unimplemented; FM-DT-09 unknown-type fallback logged but issued no HOLD-safe command; the spec itself double-allocates FM-DT-09 (§3.1.1.3 vs §3.5.9 — ERR-008-007); `WarnFmDt10`'s doc described a failure mode that exists nowhere in the spec; all diagnostic emits ungated (FR-CS-031 drift, PM AR-7 M-1 precedent). | FM-DT-10/11/12 implemented with the project NaN-gate pattern; unknown type → FM-DT-14 (renumbered) + HOLD-safe command; warning-code constants aligned; every emit `#if UNITY_EDITOR || DEVELOPMENT_BUILD` gated (body-gate form where the if-body carries control flow). |
| M-11 | `EventBusRegistrar.Initialize()` not idempotent — a second call (e.g. `EventBusWiringSmokeTests` booting in the same test process) threw `ERR_EVT_ORDINAL_COLLISION`; the smoke test's comment "registrars carry an s_registered guard" holds only for pass-mechanics (v1.2). Compounding: the integration tests MUST boot the registry (every successful `ReceiveSnapshot` publishes Tier C, and `CosmeticChannel.Publish` throws for unregistered ordinals) but carried no boot — they would have thrown on first real run. **Sibling drift (out of DT scope, flagged for follow-up):** shot-mechanics, perception-system, heading-mechanics, and goalkeeper-mechanics registrars also lack the guard. | `s_registered` guard added (registrar v1.2); integration fixture gains a `[OneTimeSetUp]` boot. |

### Low (batch)

1. `tiebreakerApplied` never reset on a strict improvement — violated INV-SEL-07 winner semantics (spec pseudocode resets it). Fixed.
2. INV-GEN-06 pitch-bounds: dribble look-ahead, intercept projection, and depth-adjusted slots could leave the pitch. `PitchGeometry.ClampToPitch` applied at generation; locked by the dribble-clamp test.
3. `UtilityWeights` carried unconsumed duplicates of `TacticalWeights` constants (`PRESS_TACTICAL_HIGH/MEDIUM/LOW`, `URGENCY_PRESSURE_SCALE`) — parallel-surface drift hazard (IsAerialFormula precedent); removed (§3.4.7 declares TacticalWeights exclusive). `MOVE_ZONE_*` were declared-but-unconsumed and untagged — now consumed via `GetZoneModifier` and tagged [GT].
4. `DRAG_APPROX` mis-tagged `[CROSS — Ball Physics #1 §3.x]` — not a verbatim copy and the citation names no section; retagged [EST] in both surfaces (ERR-008-009).
5. Dead `DerivePassType` params (`aCrossing`, `agentPos`) — vestige of the unimplementable §3.1.3.4 WIDE_ZONE cross gate (spec's own "x in WIDE_ZONE" should be Y; ERR-008-006 documented-open). Params dropped; `DtAgentAttributes.Crossing` doc-noted declared-but-unconsumed.
6. Constant tallies: `TacticalWeights.cs` header and spec §3.4.7 / §3 summary claimed 23 constants over 22 rows (`PRESS_URGENCY_FACTOR` double-counted) — corrected (ERR-008-010); §3.2.7.2 vs §3.4.7 file-rule contradiction resolved in favour of §3.4.7.
7. Stale spec values: HOLD nominal 0.25 in §3.1.6/§3.5.5/EC-DT-01 (authority §3.2.5.1 = 0.28); NOISE_MAX 0.20 in EC-DT-07 (authority §3.3.4.3 = 0.15); §3.5.4's 8.0 m "DRIBBLE_MAX_TARGET_DISTANCE" (authority §3.1.5.3 = 5.0 m look-ahead). All patched with authority pointers.
8. Spec pseudocode axis bugs beyond M-2: §3.1.4.3 goal posts offset along X (ERR-008-011); §3.5.6's "Ball Physics uses XZ" note (Ball Physics #1 §1.2 is Z-up; ground plane is XY). Both corrected — the implementation was already right on both.
9. Doc corrections: `PossessionState.cs` header ("perspective" → absolute); `AgentAction` "dispatcher fills AgentId" → TeamId; `DecisionContext.PossessedByTeam` perspective claim; HOLD JOGGING-strafe SPEC-DEVIATION NOTE (AM AR-13 accommodation, vs §3.5.5's IDLE).

---

## Why these survived: the home-team-only blind spot

Every numerical example in §3.1.11/§3.2/§3.3, every AR-1 test fixture, and every
helper in the test suite used team 0 attacking +X. For home agents, the
home-perspective zone IS the team-relative zone, `AWAY_TEAM` IS the opponent, and the
line-depth sign error is unobservable at depth 0.5. The defect class is the same one
the project's closed-loop scenario work (#19) exists to catch: per-function tests
verified the spec as written for the half of the state space the spec's own examples
exercised. **Recommendation (Stage 0+1):** a `sim_` scenario driving one away-team
agent through the full pipeline (mirror of BAL-DT-01) on the #19 ScenarioRunner, once
the DT's orchestrator seam exists.

## Files changed (this commit)

Production: `ActionDispatcher.cs` v1.1, `DecisionTree.cs` v1.1,
`DecisionTreeStateMachine.cs` v1.1, `DecisionContextAssembler.cs` v1.2,
`DecisionContext.cs` v1.1, `PitchGeometry.cs` v1.1, `UtilityScorer.cs` v1.2,
`TacticalModifierResolver.cs` v1.1, `TacticalContext.cs` v1.1, `OptionGenerator.cs`
v1.2, `ActionSelector.cs` v1.2, `UtilityWeights.cs` v1.2, `TacticalWeights.cs` v1.1,
`DecisionTreeConstants.cs` v1.2, `PossessionState.cs` v1.1, `AgentAction.cs` v1.1,
`DtAgentAttributes.cs` v1.2, `decision-tree.asmdef`; cross-spec:
`agent-movement/MovementCommand.cs` v1.4 (two factories), four sibling asmdefs
(DeterministicSim reference).
Tests: `DispatcherTests.cs` v1.1, `UtilityScorerTests.cs` v1.2,
`OptionGeneratorTests.cs` v1.2, `DecisionTreeIntegrationTests.cs` v1.1, new
`DecisionContextAssemblerTests.cs` v1.0.
Spec: `section-2-1-to-2-2.md`, `section-3-1.md`, `section-3-1-9-to-3-1-12.md`,
`section-3-2.md` v1.4, `section-3-4.md` v1.2, `section-3-5.md`,
`section-3-6-to-3-8.md`.
Tracking: `spec-error-log.md` v1.26 (ERR-008-002..011), `file-manifest.md`,
`src/CLAUDE.md` v1.64, root `CLAUDE.md`.

---

*Decision Tree Specification #8 | System XI — Specification #8 of 20*
