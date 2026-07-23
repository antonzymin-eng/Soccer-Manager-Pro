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

- **`OptionGeneratorTests` / `UtilityScorerTests` / `ActionSelectorTests`** (#8, extend) — SAVE
  generated iff `SaveAvailable`; `ScoreSave` shape (better keeper ⇒ higher); SAVE=7 noise-field
  packing unchanged for ordinals 0–6 (a byte-for-byte lock that adding SAVE did not perturb existing
  noise).
- **`GkHeadingIntentSource` crossing-point helper** (new pure fn) — unit tests for the SaveTarget
  geometry.
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

**AR-3 (2026-07-23) — CONVERGENCE, cycle closed.** Full re-read of the fixed outline against the same
source. The H-1 guard (`if (opt.Type != ActionType.SAVE)`) is confirmed to neutralise the only
crashing consumer without touching the #21 tables; the §3.4a audit checklist covers every
action-ordinal-indexed surface (noise field, player-tactic tables, the two scoring switches, the
gated rest-defense/dismark multipliers, `TacticalModifierResolver`, serialization); the `SaveTarget →
TargetHand` mapping and `DeflectionTarget = null` are internally consistent with the heuristic parity
claim; the GK-movement-skip dependency + the >2×-noise margin close the flicker path; no new High or
Medium. Low remaining (non-gating): the exact `ScoreSave` scale and the `TacticalModifierResolver`
disposition are deferred to the detailed plan by design (that is the plan's job, not the outline's).
Outline is ready to expand.
