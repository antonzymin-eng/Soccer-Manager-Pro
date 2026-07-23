# DT-Driven GK Save Producer — Design Supplement

> **Status:** DESIGN SUPPLEMENT (pre-implementation, OUTLINE) — same governance class as
> `gk-heading-engine-integration-design.md` / `gk-heading-scenario-design.md`. NOT a numbered spec,
> but it back-props edits into the APPROVED Decision Tree #8 section files (an ERR-008-xxx entry, the
> established #8-patch pattern — ERR-008-002..011 precedent).
> **Created:** 2026-07-23
> **Author:** —
> **Governs:** replacing the Stage-0 heuristic **goalkeeper save** trigger
> (`GkHeadingIntentSource.SaveArmed` → `MatchEngine.TryCommitSaveIntents`) with a **Decision Tree
> (#8)-emitted `SAVE` action**, so the keeper's own decision-making (utility scoring + composure
> noise) decides whether to commit the save. Closes the "DT-driven producer" item deferred by
> `gk-heading-engine-integration-design.md` §1.3, for the SAVE case.
> **Scope tier:** *DT emits SAVE* (owner-chosen, 2026-07-23; see §2).

---

## 0. Why this document exists

`gk-heading-engine-integration-design.md` landed the GK (#11) / Heading (#10) wiring opt-in
(`EnableGkHeading()`, default off) and fires the intents from **conservative Stage-0 world-state
heuristics** — a `SaveIntent` when a loose ball is on-target near the defended goal
(`MatchEngine.TryCommitSaveIntents` → `GkHeadingIntentSource.SaveArmed`), a `HeaderIntent` for the
nearest agent to a loose airborne ball. Those heuristics ignore the acting agent's own
decision-making; that doc listed **"a DT-driven GK/heading decision layer"** as future work (§1.3).

**AR-1 of this document's first outline (Option B) killed the spec-neutral shortcut** (see §9): the
DT's off-ball actions (`PRESS`/`INTERCEPT`) model *run to the ball*, not *attempt a save/header*, so
gating a geometry commit on them inverts the save case (a diving save is precisely the shot you
cannot run to) and misses the vertical-drop header. The owner therefore chose **Option A**: the DT
**emits** the save decision as a first-class action. This document designs that, bounded to the
**SAVE** case (the header follows separately — see §2 / §8).

Verified against source (2026-07-23):
- `ActionType` = `PASS`..`INTERCEPT` (0–6). Ordinals are canonical hash inputs packed into a **3-bit
  field** in `ActionSelector.ComputeOptionNoise` (`:113` — `[0,7]`). `SAVE = 7` is the **last ordinal
  that fits the noise field**; a further action would overflow and force a composure-noise rebaseline.
  But the noise field is **not the only `ActionType`-ordinal-indexed surface** — see the §3.4a
  mandatory consumer audit (AR-2 H-1): `PlayerTacticActionMultiplier` indexes 7-wide #21 tables by the
  ordinal and would throw on `a = 7`. "SAVE fits" is true of the noise field alone.
- `ActionSelector.BuildAgentAction` (`:145`) populates `PassParams`/`ShotParams` only for PASS/SHOOT;
  SAVE reuses `Type` + `TargetPosition`, so **`AgentAction` grows no field** and
  `WriteDecisionTreeState`/`ReadDecisionTreeState` are unchanged ⇒ **no `SNAPSHOT_SCHEMA_VERSION`
  bump** (subject to the §3.4a check that no restore-path validation rejects `Type = 7`).
- `DecisionTree.ReceiveSnapshot` assembles a per-tick `DecisionContext`; the DT dispatches PASS/SHOOT
  to constructor-injected executors and movement to the injected `IDtMovementController`
  (`ActionDispatcher.Dispatch`) — the exact seam pattern SAVE reuses.
- `DecisionTreeStateMachine.IsContinuousAction` (`DecisionTree.cs:150`) classifies non-holding
  actions; SAVE joins it (dispatched each stride, deduped downstream — §3).

---

## 1. What "DT emits SAVE" means

Today `TryCommitSaveIntents` unconditionally commits a save whenever the geometry is armed — the
keeper is a passive geometry sensor. Under this design the keeper's **Decision Tree** owns the
choice: a new `SAVE` action option is generated when a save is geometrically available, **scored**
against the keeper's other options (MOVE/HOLD/…) with the keeper's attributes and the deterministic
composure noise, and dispatched only if it **wins**. The keeper's reflexes/composure and its
tactical situation now shape whether and when it commits — a genuine decision, not a reflex.

The geometry (is a save even possible this tick) stays in the proven pure `GkHeadingIntentSource.
SaveArmed`; the DT consumes its result as one more world fact, exactly as it consumes `BallVisible`,
`PossessingAgentId`, ball velocity, etc. This keeps the on-target geometry in one unit-tested place
(no duplication of `CheckRestartAndApply`) while the *decision* moves into the DT.

---

## 2. Scope decision (owner-chosen: Option A, SAVE-only)

- **Chosen:** the DT emits `SAVE`. Bounded to the goalkeeper save. The header stays on its existing
  heuristic (`TryCommitHeaderIntents`) this landing.
- **Header deferred (not dropped):** a DT-emitted `HEADER` would be `ActionType` ordinal 8, which
  **overflows the 3-bit composure-noise field** and forces a rebaseline of every agent's noise hash
  — a materially larger change with its own digest impact. It is its own follow-up (widen the noise
  field + rebaseline, or a different encoding), recorded in §8.
- **Still opt-in.** Everything here runs only when `EnableGkHeading()` is set, via the same gate the
  heuristic used: the match engine computes `SaveAvailable` only under the flag, so flag-off the DT
  never sees a SAVE option and is **byte-identical** to today (§5).

---

## 3. Design (SAVE-only)

### 3.1 The fact into the DT — `SaveAvailable` on `TacticalContext`

`TacticalContext` gains two routing fields (the #21 routing-field pattern — `Stage0Default` seeds the
identity, `SaveAvailable = false`, so an unset context is behaviour-neutral):

- `bool SaveAvailable` — true only for the keeper of the team a loose on-target ball threatens
  this tick.
- `Vector2 SaveTarget` — the ball's on-target goal-line crossing point (the dive target), from the
  geometry.

The match engine, in `RunMechanicsAI` (where it already writes each agent's `TacticalContext` before
the DT reads it), sets these for the keeper **only under `EnableGkHeading()`**, computed from
`GkHeadingIntentSource.SaveArmed` (+ a crossing-point helper). All other agents / flag-off ⇒
`SaveAvailable = false`. `TacticalContext` is per-tick assembled, **not serialized** — no schema
impact.

### 3.2 Generate — `OptionGenerator.GenerateSaveCandidate` (#8 §3.1 new)

In the **off-ball** branch (a keeper facing a shot does not have the ball), a new candidate gated on
`ctx.SaveAvailable`:

```
if (!ctx.SaveAvailable) return count;      // only the threatened keeper, only under the flag
buf[count++] = new ActionOption {
    Type = ActionType.SAVE,
    TargetPosition = ctx.SaveTarget,        // the crossing point (→ TargetHand at dispatch, §3.5)
};
```

`ActionOption` gains **no** new field for SAVE at Stage 0: `ScoreSave` reads `ctx` (the keeper's
projected attributes + the geometry already encoded in `SaveAvailable`/`SaveTarget`), not an option
scratch field. If a later refinement needs a feasibility scalar, the `SaveArmed` geometry helper
returns it and it rides `ctx` — not a new `ActionOption` column (which would drift from the PASS/SHOOT
scratch groups). The Stage-0 `ScoreSave` is a keeper-attribute-modulated constant (§3.3).

### 3.3 Score — `UtilityScorer.ScoreSave` (#8 §3.2 new)

`ScoreSave` returns a high base utility (a keeper facing an on-target shot should usually save),
modulated by the keeper's **DT-consumed decision attributes** (`ctx.A_Decisions` / `ctx.A_Anticipation`
— already used elsewhere in `ComputeUtility`) so a more decisive keeper commits more readily.
**Important:** the DT's `DecisionContext` carries `DtAgentAttributes`, **not**
`GoalkeeperAgentAttributes` — the GK-specific Reflexes/Handling are **not** available here and must not
be read from `ctx`; they shape the save's *execution quality* later, in the orchestrator, via the
sink's `ToGoalkeeper` projection (§3.5). `ScoreSave` competes with the always-present
`MOVE_TO_POSITION` (return-to-slot) and any other off-ball option; composure noise (`ActionSelector`)
gives deterministic per-keeper variation. Clamped to `[UTILITY_FLOOR, UTILITY_CEILING]`, NaN-gated to
the floor (the existing pattern). Per §3.6 the base scale must clear `MOVE_TO_POSITION` by > 2× the
noise bound.

### 3.4 Select / build — `ActionSelector` (SAVE=7)

`SAVE = 7` slots into the existing 3-bit noise field with no rebaseline of ordinals 0–6.
`BuildAgentAction` sets `Type = SAVE`, `TargetPosition = opt.TargetPosition`; no new field.

### 3.4a MANDATORY `ActionType`-consumer audit (AR-2 H-1 — the "SAVE=7 fits" claim is false beyond the noise field)

`SAVE = 7` fits the 3-bit noise field, but **every other `ActionType`-ordinal-indexed lookup must be
proven to handle 7 before implementation** — one already crashes:

- **`TacticTranslation.PlayerTacticActionMultiplier` (`:79, :83`) — CRASHES.** It is applied to
  **every** scored option in `UtilityScorer.ComputeUtility` (`:66`), and indexes `RoleWeightModifiers
  [role][a]` and `TempoActionBias[tempo][a]` by `a = (int)opt.Type`. Those #21 tables are **7 columns
  wide** (ordinals 0–6); `a = 7` is out of bounds ⇒ `IndexOutOfRangeException` on the first flag-on
  save. **Fix (chosen):** guard SAVE in `ComputeUtility` — `if (opt.Type != ActionType.SAVE) u *=
  PlayerTacticActionMultiplier(...)`. SAVE is not player-tactic/tempo-modulated (a keeper's save is
  not shaped by RiskyPasses or attacking tempo), so skipping is semantically correct **and** avoids
  widening the #21 tables (which would also trip `BalancePassInvariantsTests`, which lock their
  dimensions). Do **not** widen the tables.
- **`MentalityRiskMultiplier` (`UtilityScorer.cs:59`)** — indexed by `Mentality`, not action. Safe.
- **`ComputeUtility` action switch (`:42–52`)** — `default → UTILITY_FLOOR`; add the `ScoreSave` case
  (§3.3) or SAVE silently scores the floor and never wins.
- **`RestDefense` / `Dismark` multipliers (`:78, :93`)** — already gated on explicit
  PASS/SHOOT/DRIBBLE membership; SAVE is untouched. Safe.
- **`ActionDispatcher.Dispatch` switch** — `default → HOLD-safe command` (per
  `DispatcherTests.UnknownActionType_DispatchesHoldSafeCommand`); add the SAVE case (§3.5) or a SAVE
  winner is silently swallowed as a HOLD.
- **`TacticalModifierResolver` (#8 §3.4)** — audit for an action-ordinal-indexed lookup; guard/extend
  as needed.
- **Serialization round-trip** — confirm `Write/ReadDecisionTreeState` and the `AgentAction` ctor do
  **not** validate `Type ∈ {0..6}` (a restore of a saved `Type = 7` must not throw). `Type` is a plain
  i32; expected safe, but the detailed plan verifies it.

The detailed plan MUST reproduce this audit as a checklist with each consumer's disposition; SAVE=7 is
not "free."

### 3.5 Dispatch — `IDtSaveDispatch` sink (#8 §3.5 new; the `IDtMovementController` precedent)

New interface in the decision-tree assembly (both sides specified — DT produces, match engine
consumes):

```
public interface IDtSaveDispatch { void CommitSave(int agentId, Vector2 target); }
```

`ActionDispatcher.Dispatch` gains a `SAVE` case → `saveDispatch?.CommitSave(agentId, action.
TargetPosition)` (null sink ⇒ logged wiring drop, the null-executor precedent). `DecisionTree`'s
constructor gains an optional `IDtSaveDispatch saveDispatch = null` parameter (parallel to the
executors); `ActionDispatcher.Dispatch` takes it through.

`MatchEngine`'s implementation (`HostSaveDispatch`, parallel to `HostMovementController`): map
`agentId → gkIndex` (via `_gkAgentIds`), **apply the `_saveCommittedForGk` per-episode latch** (so a
SAVE re-picked each stride commits once — the latch is already serialized at v18), project
`PlayerAttributeProjection.ToGoalkeeper`, build the `SaveIntent`, call `_goalkeeper.CommitSaveIntent`,
and record `_lastCommittedSaveAttrs` (the existing `TestOnly_` proof).

**`SaveIntent` field mapping (AR-2 M-1 — the crossing point is NOT a deflection target).** `target`
(the ball's on-target goal-line **crossing point**) determines **which side the keeper commits to**,
not where it pushes the ball: `TargetHand = Left / Right / Either` from the lateral side of `target`
relative to the goal centre (a Stage-0 threshold band around centre → `Either`). `DeflectionTarget`
stays **`null`** (heuristic parity — a real deflect-to-safety point is future work), and
`ClutchFirmness` is the existing Stage-0 constant. Do **not** set `DeflectionTarget = target` — that
would tell the keeper to push the ball *into* the goal.

### 3.6 State machine — SAVE is a continuous action

SAVE joins `DecisionTreeStateMachine.IsContinuousAction` (like MOVE): it does **not** hold EXECUTING
between heartbeats (the keeper re-evaluates each stride; the sink latch dedupes the commit). This is
the smallest state-machine change and avoids inventing a SAVE-complete lifecycle hook.

**Flicker + the load-bearing "GK movement is skipped" assumption (AR-2 M-2).** Because SAVE is
continuous and competes each stride with the always-present `MOVE_TO_POSITION` (both carrying
composure noise), a small `ScoreSave`−`ScoreMove` margin would let the keeper's dispatch flicker
SAVE↔MOVE across strides. The latch keeps the *commit* single, but on a MOVE stride the keeper is
also dispatched a movement command — which is harmless **only because the Physics phase skips
goalkeeper movement at Stage 0** (`HostMovementController` note, `MatchEngine.cs:~5828`). This design
**depends on that skip**; it must be stated (here and §5), and it breaks the day GK movement is
enabled. Two guards: (1) `ScoreSave` must dominate `MOVE_TO_POSITION` for a threatened keeper by a
**margin greater than 2× the composure-noise bound**, so no realistic noise draw flips it — the
detailed plan pins the scale against the actual `ScoreMove` value for a keeper at its slot; (2) a
**multi-stride** test asserts the keeper stays on SAVE across consecutive strides (not just a
single-stride commit).

### 3.7 Wiring changes in `MatchEngine`

- `DriveGkHeadingTactical` **drops** `TryCommitSaveIntents()` (the save decision now lives in the DT);
  it keeps `TacticalTick` (baselines + GK state machine) and `TryCommitHeaderIntents()` (header stays
  heuristic).
- `RunMechanicsAI` writes `SaveAvailable`/`SaveTarget` into the keeper's `TacticalContext` under the
  flag.
- `HostSaveDispatch` is constructed at boot and injected into every `DecisionTree` (unconditional; it
  is only ever *called* when a SAVE option exists, i.e. flag-on).
- Ordering holds (AR-2 L): today `TryCommitSaveIntents` commits inside `DriveGkHeadingTactical`
  (`MatchEngine.cs:~2181`), immediately after the GK `TacticalTick`. Under this design the commit moves
  to the DT dispatch in the per-agent loop (`~2221`) — **later in the same AI phase, still after
  `TacticalTick`, still before the Physics-phase GK `Update`** (phase 3) that carries the dive and
  before the Resolve-phase goal check (phase 4). So the `TacticalTick → CommitSaveIntent → Update`
  order the orchestrator relies on is preserved; only the intra-phase position shifts. `RunMechanicsAI`
  (writes `SaveAvailable`/`SaveTarget`) runs before the DT loop reads it.

---

## 4. Determinism & snapshot safety

- **No schema change.** `SAVE` is an existing-typed `ActionType` value (serialized in the unchanged
  `DecisionTreeState.LastAction.Type` i32); no new `AgentAction` field; the latch + orchestrator
  state are already serialized at v18. `SNAPSHOT_SCHEMA_VERSION` stays **18**.
- **Flag-off byte-identical.** `SaveAvailable` is false without `EnableGkHeading()`, so no SAVE
  option is ever generated, `SAVE=7` never enters the noise field, and the dispatch sink is never
  called. The existing snapshot/determinism/restore suite stays green with no rebaseline.
- **Flag-on remains snapshot-safe (v18).** The keeper's SAVE choice is in the restored
  `DecisionTreeState`; `RunMechanicsAI` recomputes `SaveAvailable` (pure function of restored
  ball/possession) on the post-restore tick, the latch suppresses double-commit, and the orchestrator
  in-flight state restores — save@N → restore → tick-to-N+K matches.
- **Flag-on digest differs from the pre-change heuristic** — expected/correct (KD-11: flag-on is
  already non-neutral). The flag-on scenario/GK tests rebaseline their *expected commit conditions*
  (a keeper now saves iff its DT chose SAVE), not a determinism golden.

---

## 5. Behaviour-neutrality / opt-in

Flag-off: byte-identical (§4). Flag-on: the keeper saves when its DT scores SAVE above its
alternatives — no longer an unconditional geometry reflex. This is the intended behavioural change.

---

## 6. Test plan (outline)

> **§11.5 is authoritative** — the detailed plan refined the tests (no crossing-point helper: `SaveTarget`
> was dropped in §11.0, `ScoreSave` is a dominant constant not an attribute-shaped value). This section
> is the high-level intent; implement per §11.5.

- **`OptionGeneratorTests` / `UtilityScorerTests` / `ActionSelectorTests`** (#8, extend) — SAVE
  generated iff `SaveAvailable`; the `ScoreSave`-dominates-off-ball no-flicker lock; the
  `PlayerTacticActionMultiplier`-not-applied-to-SAVE crash-regression lock; SAVE=7 noise-field packing
  unchanged for ordinals 0–6.
- **`MatchEngineGkHeadingTests`** (extend) — flag-on: a keeper facing an on-target shot dispatches
  SAVE and the projection reaches the orchestrator (`TestOnly_LastCommittedSaveAttrs`); flag-off:
  byte-identical to a pre-change engine; two-run forward determinism; the once-per-episode latch
  holds (SAVE re-picked each stride commits once).
- **Full dotnet gate** — PASSED, 0 failures. No schema rebaseline (flag-off default unchanged); the
  flag-on GK-test expectations updated to the DT-decision condition.

---

## 7. Risks

1. **SAVE never wins the utility contest** → the keeper never saves (regression vs. the heuristic).
   Mitigation: `ScoreSave` must dominate `MOVE_TO_POSITION` for a threatened keeper (the keeper's
   slot is right there, so MOVE scores its own "already home" utility); the detailed plan pins the
   scale so a facing-an-on-target-shot keeper picks SAVE by default, with attributes/noise modulating
   the margin. Locked by a `MatchEngineGkHeadingTests` flag-on "keeper saves an on-target shot" case.
2. **Editing an APPROVED spec (#8).** Mitigation: this is the established ERR-008-xxx back-prop
   pattern; the change is additive (a new action, off-ball-branch-only, gated on a new context flag),
   the §3.1/§3.2/§3.5 edits are localized, and the full AR loop runs on the code.
3. **Ordering / double-commit.** Mitigation: §3.7 keeps the AI→Physics→Resolve ordering and the
   v18-serialized `_saveCommittedForGk` latch in the sink; the latch test locks once-per-episode.
4. **Noise-field ceiling for the header.** Mitigation: header is explicitly out of scope; §8 records
   the ordinal-8 overflow as its own follow-up.

---

## 8. Out of scope / follow-ups

- **DT-emitted HEADER** (ordinal 8 → noise-field widening + rebaseline). Its own supplement.
- **GK rush/distribute as DT actions.** Untouched.
- **Flipping `EnableGkHeading()` default on + the flag-on digest rebaseline.** Separate, project-wide.

---

## 9. Key decisions (index)

| KD | Decision |
|----|----------|
| KD-1 | Scope = Option A (DT emits `SAVE`), owner-chosen after AR-1 killed the spec-neutral gate. SAVE-only; header deferred. |
| KD-2 | Geometry stays in `GkHeadingIntentSource` (proven, pure); the DT consumes availability as a `TacticalContext.SaveAvailable` fact and **decides** via utility scoring. No re-derivation of on-target geometry. |
| KD-3 | `SAVE = 7` fits the 3-bit composure-noise field; no rebaseline of ordinals 0–6. Header (ordinal 8) would overflow — deferred. |
| KD-4 | SAVE reuses `AgentAction.Type`/`TargetPosition`; no new field; **no `SNAPSHOT_SCHEMA_VERSION` bump**. |
| KD-5 | Dispatch via a new `IDtSaveDispatch` sink (the `IDtMovementController` precedent); `MatchEngine`'s `HostSaveDispatch` maps agent→GK, applies the v18 latch, projects `ToGoalkeeper`, and commits. |
| KD-6 | SAVE is a continuous action (`IsContinuousAction`) — dispatched each stride, deduped by the sink latch; no new lifecycle hook. |
| KD-7 | Opt-in via `SaveAvailable` (computed only under `EnableGkHeading()`) ⇒ flag-off byte-identical; flag-on genuinely DT-decided. |
| KD-8 | `MatchEngine.DriveGkHeadingTactical` drops `TryCommitSaveIntents`; the header heuristic stays. |

---

## 10. Adversarial review log

**AR-1 (2026-07-23) — on the original Option-B outline: 1 High; scope pivot.** The spec-neutral
"gate the geometry commit on the DT off-ball action" design was unsound — `INTERCEPT`/`PRESS` model
run-to-the-ball, inverting the save (a diving save is the shot you cannot run to) and missing the
vertical-drop header (INTERCEPT's speed gate is horizontal-only). The DT has no save/header decision
concept, so any spec-neutral gate either inverts (restrictive) or no-ops (permissive). Owner chose
Option A (DT emits SAVE); this document was rewritten to that scope. Full detail retained in git
history (commit 1a29b75).

**AR-2 (2026-07-23) — on the Option-A outline: 1 High + 2 Medium + 2 Low, all resolved.** Verified
against `UtilityScorer.cs` / `TacticTranslation.cs` / `ActionSelector.cs`.
- **H-1:** the "SAVE=7 just fits" claim was false beyond the noise field —
  `PlayerTacticActionMultiplier` indexes 7-wide #21 tables (`RoleWeightModifiers`/`TempoActionBias`)
  by the action ordinal and is applied to every option, so a SAVE option (a=7) throws
  `IndexOutOfRangeException` on the first flag-on save. Resolved: new §3.4a mandatory consumer audit
  (guard SAVE in `ComputeUtility`, skip the player-tactic multiplier — do NOT widen the #21 tables;
  enumerate every `ActionType`-ordinal-indexed consumer with its disposition).
- **M-1:** `SaveIntent.DeflectionTarget = target` was wrong (the crossing point is where the ball
  enters, not where to deflect). Resolved: `target → TargetHand` (which side); `DeflectionTarget`
  stays `null` (§3.5).
- **M-2:** the SAVE/MOVE flicker is harmless only because Stage-0 physics skips GK movement — an
  unstated load-bearing assumption. Resolved: stated in §3.6/§5; `ScoreSave` must dominate MOVE by
  > 2× the noise bound; multi-stride "stays on SAVE" test required.
- **L:** the dormant `decision-tree.asmdef → GoalkeeperMechanics` claim was dropped (the
  primitive-only `IDtSaveDispatch` keeps it dormant); `SaveUrgency` scratch field dropped (ScoreSave
  reads `ctx`); the commit-ordering shift (2181 → 2221, same phase) stated explicitly (§3.7).

**AR-3 (2026-07-23) — CONVERGENCE of the outline, cycle closed.** Full re-read of the fixed outline
against the same source. The H-1 guard (`if (opt.Type != ActionType.SAVE)`) neutralises the only
crashing consumer without touching the #21 tables; the §3.4a audit covers every action-ordinal-indexed
surface; the `DeflectionTarget = null` / heuristic-parity intent is consistent; the flicker path is
closed; no new High or Medium. Outline ready to expand.

**AR-4 (2026-07-23) — on the §11 detailed plan: 1 Medium (structural) + 2 Low, all resolved.**
Verified against source.
- **M-1 (correctness/structural):** the `ScoreSave = UTILITY_CEILING` dominance mechanism was not
  robust — `ComputeUtility` clamps SAVE at 1.00 while `INTERCEPT` (base 0.55) can be lifted to 1.00 by
  `MentalityRiskMult` (max 1.20) × `RoleWeightModifiers` (up to 2.0), tying at the ceiling; the
  lowest-ordinal tiebreak then picks `INTERCEPT(6)` over `SAVE(7)` ⇒ a **missed save** under a
  supported flag-on per-agent keeper tactic, which the plan's default-tactic dominance test would not
  catch. Resolved by making SAVE the **sole** off-ball option when `SaveAvailable`
  (`OptionGenerator.GenerateOffBallBranch` short-circuit) — robust selection independent of
  noise/mentality/role/tiebreak; the `U_BASE_SAVE`/2×noise dominance analysis is dropped, the
  `PlayerTacticActionMultiplier` guard kept.
- **L:** §11.3.3 write-site pinned to `MatchEngine.cs:2344–2395` (before `_tacticalContexts[i] = ctx`)
  with the exact statement; §11.5 dominance test replaced by the sole-option selection lock.

**AR-5 (2026-07-23) — CONVERGENCE, plan cycle closed.** Full re-read of §11 after the fix. The
sole-option branch touches only the flag-on threatened keeper (`SaveAvailable` false elsewhere) ⇒
flag-off byte-identity + the §11.1 audit + no-schema-bump all survive; SAVE is still scored as the
sole option so the `PlayerTacticActionMultiplier` guard remains load-bearing and is kept; the §11.5
locks now assert the robust property (exactly-one-SAVE-option + selection under adversarial
tactic/mentality). No new High or Medium. Plan ready to implement.

**AR-6 (2026-07-23) — on the implementation: 0 High + 0 Medium + 3 Low — CONVERGENCE, cycle closed.**
Full re-read of the shipped code against source. Verified: SAVE re-evaluates each heartbeat
(`IsContinuousAction(SAVE)` = true ⇒ the ball-resolves-then-re-arms cycle re-commits); `SaveArmed`
uses raw world coordinates with the team index (no home/away mirror bug — matches the removed
heuristic); the latch is team-keyed consistently on both the clear (`RunMechanicsAI`) and set
(`HostSaveDispatch`) sides; no RNG stream cursor is perturbed by emitting SAVE (composure noise is a
stateless `SplitMix64` hash); flag-off is byte-identical by construction; keeper-with-ball is excluded
(`!loose` ⇒ `SaveArmed` false + possession branch). The three Low: (1) `RunMechanicsAI` set
`SaveAvailable` on a sent-off keeper (harmless — the DT loop skips sent-off agents and the sink
re-checks — **fixed** anyway with a `!_isSentOff[i]` gate for participation-convention consistency);
(2) the latch lifecycle is split clear/set across two files (inherent to the design — the clear must
run every armed-check stride, documented at both sites); (3) `SaveDecision_SurvivesAdversarialTactic`
passes trivially under the sole-option design (a valid regression lock against the *rejected* scoring
approach; the precise lock is the `OptionGeneratorTests` sole-option test). **Full dotnet gate:
PASSED, 0 failures (whole tree green; DecisionTree 84, MatchEngine 306; SDK 8.0.129 via apt).**

## 12. Implementation outcome (2026-07-23) — LANDED

Implemented per §11 (SAVE-only, sole-option). `decision-tree`: `ActionType.SAVE = 7`,
`TacticalContext.SaveAvailable`, `OptionGenerator` sole-option short-circuit + `GenerateSaveCandidate`,
`UtilityScorer` `ScoreSave` + `PlayerTacticActionMultiplier` SAVE exemption, `IDtSaveDispatch` +
`ActionDispatcher` SAVE case + `DecisionTree` ctor param. `match-engine`: `HostSaveDispatch` sink
(agent→GK map + v18 latch + `ToGoalkeeper` + commit), `RunMechanicsAI` `SaveAvailable`/latch-clear,
`DriveGkHeadingTactical` drops `TryCommitSaveIntents`. No state-machine edit (SAVE continuous for
free); no `SNAPSHOT_SCHEMA_VERSION` change. Spec back-prop: `ERR-008-013` +
`decision-tree/section-3-1.md`/`section-3-2.md` anchor notes. Tests: `OptionGeneratorTests`
sole-option lock, `UtilityScorerTests` crash-guard lock, `MatchEngineGkHeadingTests` DT-path
save-commit + `SaveDecision_SurvivesAdversarialTactic`. **Still deferred:** a DT-emitted HEADER
(ordinal 8 overflows the noise field → rebaseline), attribute-modulated/hesitant save commit, flip the
`EnableGkHeading` default on + flag-on digest rebaseline.

---

## 11. Detailed implementation plan

### 11.0 Source-grounded refinements to §3 (all *reduce* scope/risk; verified against source)

Reading the actual #8 / #11 source tightened three §3 sketches — the plan supersedes the outline on these:

- **Drop `SaveTarget` entirely; `TacticalContext` gains only `SaveAvailable` (bool).** `SaveIntent.
  TargetHand` is "anatomy-lookup only; no formula gating (KD-1)" (`SaveIntent.cs:20`) and
  `DeflectionTarget` is future parry-intent (`:29`) — at Stage 0 **neither is load-bearing**, and the
  orchestrator reads the ball directly for the dive. So the crossing point is unused; the DT needs only
  the **boolean availability**. This fully dissolves AR-2 M-1 (no crossing point ⇒ nothing to misplace
  into `DeflectionTarget`). The sink builds the *same* intent the heuristic built:
  `{ TargetHand = Either, ClutchFirmness = SaveTriggerClutchFirmness, DeflectionTarget = null,
  AttemptCommittedTick = CurrentTacticalTick }`.
- **No state-machine edit.** `DecisionTreeStateMachine.IsContinuousAction(type) => type != PASS &&
  type != SHOOT` (`:34`) — `SAVE` is continuous **for free**; §3.6 is a no-op. (SAVE re-dispatched each
  stride; the sink latch dedupes — the SAVE/MOVE flicker AR-2 M-2 raised is now impossible anyway, see
  the score below.)
- **SAVE is the SOLE off-ball option when `SaveAvailable` — not a scored contest (AR-4 fix).** A
  utility-dominance approach (`ScoreSave = ceiling`) is NOT robust: `ComputeUtility` clamps SAVE at
  `UTILITY_CEILING` while `INTERCEPT` (base 0.55) can be lifted to the ceiling too by
  `MentalityRiskMult` (max 1.20) × `RoleWeightModifiers` (up to 2.0) → both tie at 1.00 → the
  lowest-ordinal tiebreak picks `INTERCEPT(6)` over `SAVE(7)` → **missed save** under a supported
  flag-on per-agent keeper tactic. A must-happen, geometry-gated action must not depend on
  out-scoring a tiebreak-disadvantaged competitor. **Instead:** when `ctx.TacticalContext.
  SaveAvailable`, `OptionGenerator.GenerateOffBallBranch` emits the **SAVE candidate alone** (skips
  MOVE/PRESS/INTERCEPT). SAVE is then selected regardless of noise / mentality / role / tiebreak — no
  missed save, no flicker, no dependence on the GK-movement-skip (the keeper issues no competing
  movement command at all while a save is available). It stays "DT-emits-SAVE" (generated → scored →
  selected → dispatched) and is semantically correct (a keeper committing to a save is not
  simultaneously repositioning or intercepting). `ScoreSave`'s value is no longer load-bearing for
  selection; `U_BASE_SAVE = UTILITY_CEILING` is kept only so the `AgentAction.UtilityScore` /
  `DecisionMadeEvent` reads sensibly. The `PlayerTacticActionMultiplier` SAVE guard (§11.1) is still
  required — SAVE is still *scored* (as the sole option), so scoring it without the guard still
  crashes. **Behaviour:** the keeper commits whenever a save is available (heuristic-equivalent, now
  DT-emitted); attribute-modulated / hesitant commit is the named next refinement.

### 11.1 Completed §3.4a `ActionType`-consumer audit (every ordinal-indexed surface, verified)

| Consumer | Site | SAVE=7 disposition |
|---|---|---|
| `ComputeOptionNoise` 3-bit field | `ActionSelector.cs:113` | Fits `[0,7]`. **Safe.** |
| `MentalityRiskMultiplier` | `UtilityScorer.cs:59` | Indexed by `Mentality`, not action. **Safe.** |
| `PlayerTacticActionMultiplier` | `TacticTranslation.cs:79,83` (7-wide `[role/tempo][action]` tables) | **CRASHES** (OOB at a=7). **Guard:** `if (opt.Type != ActionType.SAVE) u *= PlayerTacticActionMultiplier(...)` in `ComputeUtility`. Do NOT widen the #21 tables. |
| `ComputeUtility` action switch | `UtilityScorer.cs:42–52` | `default → UTILITY_FLOOR`. **Add `ScoreSave` case** (else SAVE floors, never wins). |
| RestDefense / Dismark multipliers | `UtilityScorer.cs:78,93` | Gated on explicit PASS/SHOOT/DRIBBLE membership. **Safe** (SAVE untouched). |
| `TacticalModifierResolver.Resolve` | `TacticalModifierResolver.cs` (per-`ScoreXxx`) | Every `switch(type)` has `default → 1.0f`. **Safe** (and `ScoreSave` won't call it). |
| `ActionDispatcher.Dispatch` switch | `ActionDispatcher.cs:44–85` | `default → HOLD-safe strafe`. **Add SAVE case** → the sink. |
| Serialization round-trip | `AgentAction` ctor (`:55` plain assign), `Write/ReadDecisionTreeState` (i32 `Type`) | No `Type ∈ {0..6}` validation. **Safe** — `Type=7` round-trips. |

### 11.2 `decision-tree` assembly edits (the #8 change)

1. **`ActionType.cs`** — append `SAVE = 7`. Update the ordinal-stability header note (7 remains the max that fits the 3-bit noise field; HEADER would be 8 → deferred).
2. **`TacticalContext.cs`** — add `public bool SaveAvailable;` (zero-value `false` = identity, safe like `DismarkIntensity.Off`; `Stage0Default` seeds `false` explicitly for symmetry). Doc it as the #11-anticipated DT-save gate.
3. **`ActionOption.cs`** — no new field (SAVE reuses `Type`/`TargetPosition`; `ScoreSave` reads `ctx`).
4. **`OptionGenerator.cs`** — `GenerateOffBallBranch` **short-circuits to SAVE alone** when a save is available (AR-4 fix — robust selection, not a scored contest):
   ```
   private static int GenerateOffBallBranch(in DecisionContext ctx, ActionOption[] buf, int count)
   {
       if (ctx.TacticalContext.SaveAvailable)          // flag-on threatened keeper only
           return GenerateSaveCandidate(in ctx, buf, count);
       count = GenerateMoveCandidate(in ctx, buf, count);
       count = GeneratePressCandidate(in ctx, buf, count);
       count = GenerateInterceptCandidate(in ctx, buf, count);
       return count;
   }
   ```
   `GenerateSaveCandidate` emits one `{ Type = SAVE, TargetPosition = ctx.Snapshot.BallPerceivedPosition (observability only), TargetAgentId = -1 }`. Because `SaveAvailable` is only ever set for the flag-on threatened keeper (§11.3.3), no other agent / flag-off path is affected — the off-ball branch is byte-identical otherwise.
5. **`UtilityWeights.cs`** — add `public const float U_BASE_SAVE = 1.00f; // [GT] SAVE base utility (= UTILITY_CEILING). NOT load-bearing for selection — SAVE is the sole off-ball option when available (OptionGenerator); this only feeds AgentAction.UtilityScore / DecisionMadeEvent.`
6. **`UtilityScorer.cs`** — (a) add `case ActionType.SAVE: u = ScoreSave(ref opt, in ctx); break;`; (b) new `ScoreSave` = `U_BASE_SAVE` (no AM/risk/tactM); (c) **guard** the `PlayerTacticActionMultiplier` call with `if (opt.Type != ActionType.SAVE)` — still required, since SAVE is *scored* (as the sole option) and would otherwise crash at the 7-wide-table lookup (§11.1).
7. **`IDtSaveDispatch.cs`** (new) — `public interface IDtSaveDispatch { void CommitSave(int agentId); }` (primitives only; `agentId` suffices — the sink owns geometry/projection). No `Vector2` param (SaveTarget dropped).
8. **`ActionDispatcher.cs`** — add `IDtSaveDispatch saveDispatch` param + `case ActionType.SAVE: saveDispatch?.CommitSave(action.AgentId); break;` (null sink ⇒ FR-DT-14-style logged drop, the null-executor precedent).
9. **`DecisionTree.cs`** — ctor gains `IDtSaveDispatch saveDispatch = null` (parallel to `passExecutor`/`shotExecutor`); stored `_saveDispatch`; threaded into the `ActionDispatcher.Dispatch` call (`:201`).

### 11.3 `match-engine` assembly edits

1. **`HostSaveDispatch`** (new nested class in `MatchEngine`, parallel to `HostMovementController`): `CommitSave(int agentId)` → map `agentId → (teamId, gkIndex)` via `_teamIds`/`_gkAgentIds`; if not a current keeper or sent-off, drop; apply the v18 `_saveCommittedForGk[teamId]` latch (commit once per episode); `attrs = PlayerAttributeProjection.ToGoalkeeper(in _canonicalAttrs[agentId], teamId, fatigue: 0f)`; `_goalkeeper.CommitSaveIntent(teamId, new SaveIntent { TargetHand = HandEnum.Either, ClutchFirmness = MatchEngineConstants.SaveTriggerClutchFirmness, DeflectionTarget = null, AttemptCommittedTick = (int)_clock.CurrentTacticalTick }, attrs)`; set `_lastCommittedSaveAttrs`/`_lastSaveAttrsValid` (the existing `TestOnly_` proof).
2. **Boot** — construct `_saveDispatch = new HostSaveDispatch(this)` and pass it into every `new DecisionTreeAI(i, movementController, matchSeed, _passExecutors[i], _shotExecutors[i], _saveDispatch)` (`:696`). Unconditional injection; only *called* when a SAVE option exists (flag-on).
3. **`RunMechanicsAI`** — the per-agent loop that builds `ctx` and writes `_tacticalContexts[i] = ctx` is at `MatchEngine.cs:2344–2395`. Immediately **before** line 2395 add:
   ```
   ctx.SaveAvailable = _gkHeadingEnabled && _isGoalkeeper[i]
       && GkHeadingIntentSource.SaveArmed(
              t, in _ball.Position, in _ball.Velocity,
              _possessingAgentId == MatchEngineConstants.NO_POSSESSION);
   ```
   `t` is the team, `i` the agent index; the `_isGoalkeeper[i]` gate makes only the keeper's context carry `true`, and only under the flag. This loop runs in `RunMechanicsAI` (`:2175`) before the DT loop reads `_tacticalContexts[i]` (`:2221–2222`), so the fact is fresh. Flag-off / non-keeper ⇒ `false` (the `Stage0Default` seed), so the off-ball branch is unchanged.
4. **`DriveGkHeadingTactical`** — **remove** the `TryCommitSaveIntents()` call (and the method, if unused). Keep `TacticalTick` (baselines + GK state machine) and `TryCommitHeaderIntents()` (header stays heuristic). `_saveCommittedForGk` + `GkHeadingIntentSource.SaveArmed` are now consumed by `RunMechanicsAI` + `HostSaveDispatch`.
5. **`GkHeadingIntentSource.SaveArmed`** — unchanged (now called from `RunMechanicsAI` to set `SaveAvailable`). No crossing-point helper needed (SaveTarget dropped).

### 11.4 Spec #8 section edits + ERR-008 back-prop

- File **`ERR-008-013`** in `spec-error-log.md`: "#8 gains a `SAVE` action (ordinal 7) — the DT-emitted goalkeeper save the #11 `SaveIntent` doc always anticipated; supersedes the `MatchEngine` heuristic save trigger. Off-ball-branch-only, gated on `TacticalContext.SaveAvailable`; `PlayerTacticActionMultiplier` guarded (not table-widened)."
- **`decision-tree/section-3-1.md`** (§3.1): new §3.1.x SAVE generation (off-ball, `SaveAvailable` gate).
- **`decision-tree/section-3-2.md`** (§3.2): new `ScoreSave` = `U_BASE_SAVE` ceiling constant; note the `PlayerTacticActionMultiplier` SAVE exemption.
- **`decision-tree/section-3-5.md`** (§3.5): new SAVE dispatch case + `IDtSaveDispatch` seam.
- **`ActionType`/§2.2.x**: document `SAVE = 7` + the ordinal-stability / noise-field-ceiling note.
- No #21 spec change (the tables are NOT widened — the guard exempts SAVE).

### 11.5 Tests

- **`OptionGeneratorTests`** — when `SaveAvailable`, the off-ball branch yields **exactly one** option and it is SAVE (the sole-option lock — robust selection); when `!SaveAvailable`, the off-ball branch is unchanged (MOVE/PRESS/INTERCEPT, no SAVE). `SelectAction` over that single-option buffer returns SAVE regardless of a non-Balanced `Mentality` / non-identity `PlayerTactic` context (the AR-4 missed-save regression lock — the case that broke the scoring-dominance approach).
- **`UtilityScorerTests`** — **`PlayerTacticActionMultiplier` not applied to SAVE**: scoring a SAVE option under a non-identity `PlayerTactic`/`Tempo` context does not throw (the §11.1 OOB-crash regression lock) and yields the finite `U_BASE_SAVE` (no attribute/tactic modulation).
- **`ActionSelectorTests`** — SAVE=7 noise packing leaves ordinals 0–6 byte-identical (adding SAVE perturbs no existing option's noise).
- **`MatchEngineGkHeadingTests`** (extend): flag-on — a keeper facing an on-target loose ball **dispatches SAVE** and `ToGoalkeeper` reaches the orchestrator (`TestOnly_LastCommittedSaveAttrs`); the once-per-episode latch holds (SAVE re-picked each stride ⇒ one commit); **multi-stride** the keeper stays on SAVE (no flicker); flag-off — digest byte-identical to a pre-change engine; two-run forward determinism.
- **Snapshot/restore** — `MatchEngineSnapshotRestoreTests` flag-on: a keeper mid-save (LastAction=SAVE, latch set) round-trips at v18 (no schema change; the DT-state + latch + orchestrator state already serialize).
- **Full dotnet gate** — PASSED, 0 failures. No `SNAPSHOT_SCHEMA_VERSION` change; flag-off default byte-identical (no rebaseline); flag-on GK-test expectations updated to the DT-decision condition.

### 11.6 Determinism / versions

No `SNAPSHOT_SCHEMA_VERSION` bump (SAVE adds no serialized field; `Type=7` rides the existing i32). `MatchEngineConstants` unchanged except possibly removing the now-unused save-trigger-min-speed constant only if `SaveArmed` no longer references it (it still does — keep). `MatchEngine.cs` / `UtilityScorer.cs` / `ActionSelector`-adjacent version-history rows appended.

