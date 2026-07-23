# DT-Driven GK / Heading Intent Producer — Design Supplement

> **Status:** DESIGN SUPPLEMENT (pre-implementation, HIGH-LEVEL OUTLINE) — same governance class as
> `gk-heading-engine-integration-design.md` / `gk-heading-scenario-design.md`. NOT a numbered spec.
> **Created:** 2026-07-23
> **Author:** —
> **Governs:** superseding the Stage-0 pure-geometry GK/Heading intent triggers
> (`GkHeadingIntentSource.SaveArmed` / `NearestHeaderCandidate`) with a producer whose commit
> decision is **driven by each agent's Decision Tree (#8) output**, closing the "DT-driven producer"
> item deferred by `gk-heading-engine-integration-design.md` §1.3 / §4.3.
> **Scope tier:** *DT-consulting producer* (see §2) — opt-in, spec-neutral.

---

## 0. Why this document exists

`gk-heading-engine-integration-design.md` landed the GK (#11) / Heading (#10) wiring opt-in
(`EnableGkHeading()`, default off) and seeded the orchestrators from `PlayerAttributeProjection.
ToGoalkeeper` / `ToHeading`. It fires the intents from **conservative Stage-0 world-state
heuristics** (§4 of that doc): a `SaveIntent` when a loose ball is on-target near the defended
goal, a `HeaderIntent` for the single nearest active outfielder to a loose airborne ball. Those
heuristics are pure geometry — extracted into the pure static `GkHeadingIntentSource` — and they
**ignore the acting agent entirely**: attributes, tactical context, and (critically) the agent's
own decision-making play no part in whether it attempts.

That doc explicitly listed **"a DT-driven GK/heading decision layer"** as out of scope / future
work (§1.3), the GK/heading analogue of the `MatchFlowCollisionConsumer` heuristic-foul substrate
(§4.3). This document designs the first step of that follow-up: making the *decision to attempt*
flow from the agent's Decision Tree, not from raw geometry.

Verified against source (2026-07-23):
- `GkHeadingIntentSource` (`src/match-engine/GkHeadingIntentSource.cs`) is pure geometry with no
  agent-decision input.
- `TryCommitSaveIntents` / `TryCommitHeaderIntents` (`MatchEngine.cs` ~2896 / ~2940) call it, then
  project attributes + commit — the only two producer seams.
- `DecisionTree.LastAction` (`AgentAction`) and `.State` (`DtState`) are **public** and refreshed
  each AI stride; the match-engine host already exposes `TestOnly_DtState`.
- The DT has **no** SAVE/HEADER action, branch, or intent (subagent grep of `src/decision-tree/`).

---

## 1. What "DT-driven" means here

The heuristic fires whenever the geometry is right — the nearest body heads any loose airborne
ball; the keeper's dive is armed by ball trajectory alone. A **DT-driven** producer instead lets
the agent's own per-tick Decision-Tree evaluation govern the attempt:

- A keeper whose DT chose to **engage the ball** (close it down / cut it out) commits the save; a
  keeper whose DT chose to **hold its line / reset to its slot** does not dive.
- The nearest header candidate commits **only if its own DT chose to go for the ball**, not if it
  is holding defensive shape — so a player committed elsewhere no longer abandons their role to
  head a ball they were not going for.

This is a genuine behavioural improvement over "nearest body regardless of intent," and it makes
the two projections' live consumer *decision-gated* rather than *geometry-gated*.

---

## 2. Scope decision — two options weighed

### Option A — Full #8 integration (SAVE / HEADER as `ActionType`)

The eventual correct end-state: the DT *emits* SAVE/HEADER as first-class actions
(`OptionGenerator` branch → `UtilityScorer` case → `ActionSelector` populates a `SaveIntent` /
`HeaderIntent` on `AgentAction` → `ActionDispatcher` dispatches to the injected orchestrator).

**Rejected for this landing** (recorded here so the decision is explicit and reviewable):

1. **It is a spec-level change to an APPROVED, audited spec (#8).** The project's "Specification
   Before Code" rule applies; #8 went through a comprehensive AR-2 audit. Touching
   `ActionType` / `OptionGenerator` / `UtilityScorer` / `ActionSelector` / `ActionDispatcher` is a
   spec pass, not a single implement-cycle.
2. **The 3-bit composure-noise ceiling.** `ActionSelector.ComputeOptionNoise` packs the action
   ordinal into 3 bits (max ordinal 7). `SAVE=7` fits, but a second action (`HEADER=8`) overflows,
   forcing the noise field wider — which changes the composure-noise hash for **every existing
   action** and rebaselines the whole determinism digest, not just the GK path.
3. **`AgentAction` grows a new intent field** → `WriteDecisionTreeState`/`ReadDecisionTreeState`
   symmetry change + `SNAPSHOT_SCHEMA_VERSION` bump touching every agent's serialized DT state,
   even flag-off (unless made conditional — fragile).

Option A is real future work and deserves its own design supplement + spec pass; it is **not**
this landing.

### Option B — Match-engine DT-consulting producer (CHOSEN)

Keep the two producer seams (`TryCommitSaveIntents` / `TryCommitHeaderIntents`) and the geometry
gate (`GkHeadingIntentSource` — "is a save/header even physically possible this tick"), but insert
a new **pure, spec-neutral decision layer** that reads the candidate agent's already-computed DT
output (`LastAction.Type` / `State`) and **gates (and optionally shapes)** the intent from it.

- **No #8 change** — `ActionType`, `AgentAction`, the DT pipeline, and the composure-noise field
  are all untouched. (This also finally gives the dormant `decision-tree.asmdef` →
  HeadingMechanics/GoalkeeperMechanics references a rationale note, though it does not consume
  them.)
- **No new serialized state** — the decision is a pure function of already-serialized inputs
  (the v18 `DecisionTreeState.LastAction`, the geometry from live ball/agent state, and the v18
  `_saveCommittedForGk` / `_headerCommittedThisEpisode` latches). Flag-on stays snapshot-safe at
  the current `SNAPSHOT_SCHEMA_VERSION`; flag-off stays byte-identical (the producer runs only
  under `EnableGkHeading()`).
- **Reviewable in one implement-cycle** and consistent with the opt-in Phase-1/2 posture.

The honest characterization: Option B is "DT-*gated*," not "the DT *emits* the intent." But it
supersedes the pure-geometry heuristic with the agent's decision output — exactly what "a
DT-driven producer to supersede the Stage-0 heuristic triggers" asks for — without a spec pass.
Option A remains the tracked next step beyond it.

---

## 3. Design sketch (Option B)

A new pure static `GkHeadingDecisionProducer` (`src/match-engine/`), composed with the existing
geometry gate:

```
// Save: keeper commits iff geometry armed AND its DT chose to engage the ball this tick.
ShouldCommitSave(bool geometryArmed, ActionType keeperDtAction, DtState keeperDtState) → bool

// Header: nearest candidate commits iff geometry selected it AND its DT chose to engage the ball.
ShouldCommitHeader(bool geometryCandidate, ActionType agentDtAction, DtState agentDtState) → bool

// Shared Stage-0 predicate: which DT actions count as "going for the loose ball".
IsBallEngagingAction(ActionType) → bool   // { PRESS, INTERCEPT } at Stage 0
```

Rationale for `{ PRESS, INTERCEPT }`: a keeper/outfielder facing a **loose** ball runs the DT
**off-ball** branch (`MOVE_TO_POSITION` / `PRESS` / `INTERCEPT`). `PRESS` (close down) and
`INTERCEPT` (cut it out) are the ball-engaging choices; `MOVE_TO_POSITION` (reset to formation
slot) and `HOLD` mean the agent decided **not** to go for it. Possession actions
(PASS/SHOOT/DRIBBLE) require the agent to already hold the ball, so they never coincide with a
loose-ball trigger. (Exact predicate membership is a detailed-plan decision; the outline commits
only to "the agent's DT decision gates the commit.")

**Intent parameterization (detailed-plan candidate, flagged not committed here):** the save's
`DeflectionTarget` / the header's `TargetIntent` could derive from the DT's `LastAction.
TargetPosition` / tactical context rather than the fixed Stage-0 constants, so a DT-driven attempt
also *aims* per the agent's decision. The outline keeps this optional; the gate is the core.

---

## 4. Wiring

`TryCommitSaveIntents` / `TryCommitHeaderIntents` keep their structure (geometry gate → latch →
projection → orchestrator commit) and insert one call:

- **Save:** after `GkHeadingIntentSource.SaveArmed(...)`, before the latch/commit, gate on
  `GkHeadingDecisionProducer.ShouldCommitSave(armed, _decisionTrees[gkAgentId].LastAction.Type,
  _decisionTrees[gkAgentId].State)`.
- **Header:** after `NearestHeaderCandidate(...)` returns `nearest`, gate on
  `ShouldCommitHeader(nearest >= 0, _decisionTrees[nearest].LastAction.Type,
  _decisionTrees[nearest].State)`.

Ordering is already correct: `DriveGkHeadingTactical` (which calls both) runs in `RunAiPhase`
**after** `RunMechanicsAI` (which runs `ReceiveSnapshot` → refreshes `LastAction`), so the DT
output the producer reads is **this tick's fresh decision**. Sent-off agents are already excluded
from both the DT dispatch and the GK/heading drive.

---

## 5. Determinism & snapshot safety

- **No new cross-tick state.** The producer is a pure function of already-serialized inputs
  (`DecisionTreeState.LastAction`, geometry, the v18 latches). No `SNAPSHOT_SCHEMA_VERSION` bump.
- **Flag-off byte-identical.** The producer runs only inside the flag-gated
  `TryCommit*` paths; a default engine never reaches it. The existing 305-test
  snapshot/determinism/restore suite stays green with no rebaseline.
- **Flag-on remains snapshot-safe (v18).** The producer adds no state the Phase-2 v18
  serialization must capture; a flag-on save@N → restore → tick-to-N+K run still matches (the DT
  state that drives the decision is already restored).
- **Flag-on digest changes vs. the pre-producer heuristic** — expected and correct (KD-11: flag-on
  is already non-neutral). The flag-on scenario / GK tests rebaseline their *expected commit*
  conditions, not a determinism golden.

---

## 6. Test plan (outline)

- **`GkHeadingDecisionProducerTests`** (new, pure) — `IsBallEngagingAction` membership;
  `ShouldCommitSave` / `ShouldCommitHeader` truth tables over (geometry × DT action × DtState);
  the gate blocks a geometry-armed attempt when the DT chose HOLD / MOVE_TO_POSITION and permits
  it when the DT chose PRESS / INTERCEPT.
- **`MatchEngineGkHeadingTests`** (extend) — flag-on: a keeper whose DT engages commits the save
  (projection reaches the orchestrator via the existing `TestOnly_LastCommittedSaveAttrs`), while
  the same geometry with a non-engaging DT decision commits nothing; forward two-run determinism
  still holds; flag-off still byte-identical.
- **Full dotnet gate** — PASSED, 0 failures (no rebaseline; flag-off default unchanged).

---

## 7. Risks

1. **"Not really DT-driven" critique.** Option B gates, it does not emit. Mitigation: the decision
   genuinely flows from the agent's DT output and supersedes pure geometry; Option A is recorded
   as the tracked next step, not silently dropped.
2. **DT off-ball action semantics.** The `{ PRESS, INTERCEPT }` predicate assumes a keeper facing
   a shot runs the off-ball branch and picks an engaging action. Mitigation: verify against the
   #8 OptionGenerator off-ball branch in the detailed plan; keep the predicate a single
   documented helper so a refinement is one edit.
3. **Behavioural regression in the flag-on scenario/capstone.** Gating fewer commits could change
   the flag-on closed-loop scenario's observed behaviour. Mitigation: re-run and rebaseline the
   flag-on scenario's *expected* conditions (a rebaseline of expectations, not a determinism
   golden).

---

## 8. Key decisions (index)

| KD | Decision |
|----|----------|
| KD-1 | Scope = Option B (match-engine DT-consulting producer), NOT Option A (full #8 SAVE/HEADER integration). Rationale: spec-neutral, no 3-bit noise-ceiling / schema-bump blast radius, one implement-cycle. |
| KD-2 | The producer is a pure static `GkHeadingDecisionProducer` composed with the geometry `GkHeadingIntentSource`, keeping both unit-testable without a booted engine. |
| KD-3 | The commit decision reads `DecisionTree.LastAction.Type` + `.State` (public, refreshed this tick because `DriveGkHeadingTactical` runs after `RunMechanicsAI`). |
| KD-4 | Ball-engaging predicate = `{ PRESS, INTERCEPT }` at Stage 0 (the off-ball ball-winning actions); membership isolated in one helper for easy refinement. |
| KD-5 | No new serialized state, no `SNAPSHOT_SCHEMA_VERSION` bump; flag-off byte-identical; flag-on snapshot-safe at v18. |
| KD-6 | Intent parameterization from the DT (deflection/target from `LastAction.TargetPosition`) is a detailed-plan option, not committed by this outline; the gate is the core. |
| KD-7 | Option A (full DT emission) is recorded as the tracked follow-up requiring its own supplement + #8 spec pass. |

---

## 9. Adversarial review log

**AR-1 (2026-07-23) — 1 High, design pivot forced; convergence BLOCKED pending a scope decision.**
Claims verified against `src/decision-tree/OptionGenerator.cs`.

- **H-1 (core mechanism unsound):** the KD-4 `{ PRESS, INTERCEPT }` gate is the wrong signal for
  both cases, because the DT off-ball branch models *run-to-the-ball*, not *attempt a save/header*.
  (a) **Save, inverted:** `INTERCEPT` (`OptionGenerator.cs:561`) generates only when the agent can
  run to a projected ball point in time (`travelTime <= t`, `:596`) — precisely the shot that needs
  no dive. A corner-placed on-target shot is unreachable by running, so the keeper's DT emits only
  `MOVE_TO_POSITION` and the gate **suppresses the save exactly when a dive is required**. `PRESS`
  needs an opponent within trigger range (`:519`); the shooter is far, so it never fires for a
  keeper facing a struck shot. (b) **Header, unreachable:** `INTERCEPT`'s speed gate uses
  **horizontal** ball speed only (`:575–576`), so a vertically-dropping header ball (near-zero
  horizontal speed) generates no `INTERCEPT` and the header is suppressed. Root cause: the DT has
  **no** save/header decision concept (confirmed), so any "gate the geometry commit on the DT's
  chosen off-ball action" design fights the DT's semantics. The spec-neutral bounded Option B, as
  designed, **degrades** behaviour relative to the geometry heuristic it claims to supersede.
- **Consequence:** the two spec-neutral resolutions each fail the headline goal —
  a *permissive DT veto* (commit unless the DT clearly committed elsewhere) is sound but so rarely
  fires (a keeper picking `MOVE_TO_POSITION` to its nearby slot is not "committed elsewhere") that
  it is effectively a no-op, not a "DT-driven producer"; and a genuinely DT-*driven* save/header
  requires the DT to **emit** the decision (Option A), which is a spec-level change to the APPROVED,
  audited #8 spec (new `ActionType` member, `OptionGenerator`/`UtilityScorer`/`ActionSelector`/
  `ActionDispatcher` branches, orchestrator injection, and — even in the minimal SAVE-only form that
  fits the 3-bit noise field at ordinal 7 — its own spec-section edits + ERR entry + AR cycle).
- **M-1 / M-2 / L-x (folded):** the "genuinely DT-driven" framing (§1/§2) is overclaimed under any
  spec-neutral resolution; the save-deflection-from-`LastAction.TargetPosition` idea (KD-6) is
  incoherent (a DT move/run target is not a deflection target). Both are moot pending the scope
  decision below. Recorded correction to L-2: the dormant `decision-tree.asmdef` →
  GoalkeeperMechanics/HeadingMechanics references are *not* a non-sequitur — they indicate #8's
  original design anticipated dispatching to those orchestrators (i.e. Option A), which recontextualizes
  the whole scope.

**Decision escalated (2026-07-23):** because AR-1 shows the clean/bounded (spec-neutral) form is
unsound and the sound form (Option A) is a spec-level change to an approved audited spec, the scope
choice is genuinely the owner's. Options put to the user: **(A)** bounded Option A — the DT emits a
`SAVE` action (spec + code, larger, genuinely DT-driven); **(B)** a lightweight spec-neutral DT-aware
*guard* only (e.g. don't head/save while the agent's DT is mid-EXECUTING a pass/shot), honest but
marginal; **(C)** stop and keep the geometry heuristic, deferring a true DT layer to its own spec
pass. Implementation is paused at this outline until the scope is chosen — no code written against an
unsound design.
