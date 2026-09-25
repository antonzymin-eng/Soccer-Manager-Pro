# W8 Goalkeeper Distribution — Owner Decision Packet

> **Created:** September 25, 2026  
> **Status:** **DECISION REQUIRED — investigation only; no gameplay code, approved-spec change, schema change, RNG change, or `[GT]` tuning is authorized by this document.**  
> **Production anchor:** `c50726e67a6636cdc27a7abbc7ae91a1f5c29295` (`main`, PR #455 merge).  
> **Scope:** Resolve the W8 ownership/contract questions that must be settled before a result-bearing preregistration, diagnostic instrument, or production wiring is written.

---

## 1. Verified current state

### 1.1 The #11 consumer exists and is unwired

`GoalkeeperMechanics.CommitDistributeIntent(int gkIndex, DistributeIntent intent)` exists, stores the intent, and arms `_distributeIntentActive`, but it has no production caller.

Goalkeeper Mechanics #11 currently names Decision Tree #8 as the producer:

- §3.1.1: `HandsOnBall → Distributing` when #8 commits `DistributeIntent` after `releaseTickEarliest`;
- §3.8: “Decision Tree #8 supplies `DistributeIntent`”;
- §4's integration surface exposes the corresponding keeper intent path.

The current engine independently documents that the Decision Tree has no keeper-distribution action.

### 1.2 Tactical Instructions #21 already owns the manager policy input

Tactical Instructions #21 is APPROVED. FR-TI-022 states:

> `GkDistributionPolicy` sets the default fields of #11 `DistributeIntent`.

`TeamTactic.GkDistribution` already carries that policy into the match engine and the value is serialized with the team tactic. The enum is six-valued:

- `SlowDown`
- `Quick`
- `ShortKick`
- `LongKick`
- `RollOut`
- `ThrowOut`

However, the approved text currently gives only the abstract mapping “enum → (DeliveryKind, target, power) defaults.” It does **not** define the concrete values for those defaults, does not select a concrete receiver, and does not define when between `releaseTickEarliest` and the timeout the keeper commits.

Therefore #21 supplies an input policy, but not yet a complete production `DistributeIntent`.

### 1.3 Putting DISTRIBUTE in Decision Tree #8 consumes the W9 ordinal boundary

`ActionType.SAVE = 7` is the last ordinal representable by the current 3-bit composure-noise field. Adding a new Decision Tree action for DISTRIBUTE would therefore require the same ordinal-width / digest-rebaseline decision currently blocking W9 HEADER.

W1 Rush deliberately avoided that cost by using an engine-side producer rather than adding another Decision Tree action.

W8 must not silently choose the W9 rebaseline decision.

### 1.4 The two current six-second mechanisms do not measure the same possession

**Match Engine guard.** `EnforceGoalkeeperReleaseRule` increments `_gkHoldTicks` whenever the current possessor is a goalkeeper. It is not hand-specific. Since W6, goalkeeper possession can be genuine Controlled possession at the feet, so this timer can include feet possession. Its cross-tick state is serialized from snapshot schema v19.

This path was introduced as a match-stall backstop while #11 distribution was not engine-driven. Its historical comment says future #11 distribution replaces the method body, but that comment predates W6/W3 making keeper possession and hand claims live.

**Goalkeeper Mechanics #11 clock.** #11's forced-release timing is based on `_claimTick`, which is set from a hand claim. That clock therefore represents hand control rather than arbitrary goalkeeper possession.

Law 12 applies to goalkeeper hand control, not ordinary feet possession. The two timers cannot be treated as interchangeable without an explicit owner decision.

### 1.5 FR-GK-043 and production code diverge

FR-GK-043 requires:

> After `GK_HOLD_MAX_TICKS` elapsed without a `DistributeIntent`, force a default **ROLL** to the nearest own-team agent within the penalty area.

Current #11 behavior does not implement that outcome. When its hand-claim timer forces `Distributing` without an active `DistributeIntent`, the 60 Hz path simply marks distribution release reached, publishes no `DistributionExecutedEvent`, and advances to recovery.

The keeper therefore does not stall, but the required forced ROLL and its event are missing.

This is a factual code/spec divergence and requires an ERR against the implementation. The approved requirement itself is not wrong merely because the code departs from it. A spec amendment is needed only where the owner changes authority or fills presently-undefined fallback semantics.

FR-GK-043 also leaves one edge case undefined: no eligible own-team agent is inside the penalty area at timeout.

---

## 2. Owner decisions required before preregistration

### OD-W8-1 — Law 12 authority and the engine feet-possession guard

Choose the long-term ownership of the timeout behavior.

**Option A — #11 owns Law 12 from the hand-claim clock.**

- `_claimTick` / `GK_HOLD_MAX_TICKS` is the authoritative hand-control timer.
- The Match Engine's existing `_gkHoldTicks` guard is either:
  - narrowed to a separately defined non-hand stall safeguard, or
  - retired once W8 supplies a live distribution path.
- Any removal or semantic change to the serialized engine fields must follow the snapshot-schema migration/rebaseline process.

**Option B — move Law 12 to Match Engine.**

This requires a new hand-vs-feet possession signal because the current engine counter is not hand-specific. Using the present `_possessingAgentId` test unchanged is not acceptable: it would force-release legitimate feet possession after six seconds.

**Decision needed:** authoritative hand-control clock, fate of the historical feet-possession stall guard, and resulting schema plan.

### OD-W8-2 — Production `DistributeIntent` producer

Two architectural choices exist.

**Option A — engine-side producer from #21 policy.**

- Keep DISTRIBUTE outside Decision Tree `ActionType`.
- Read the already-carried `TeamTactic.GkDistribution`.
- Convert that policy into a fully specified `DistributeIntent`.
- Commit the intent through #11's existing `CommitDistributeIntent` seam.
- This preserves the ordinal-8 decision for W9 rather than forcing it during W8.

This option still requires new normative specification for:
1. exact policy → delivery-kind / target-class / power defaults;
2. deterministic receiver selection;
3. deterministic commit timing between `releaseTickEarliest` and the Law-12 timeout;
4. what `SlowDown` and `Quick` mean in that timing rule;
5. the #21 §7 T4 “polish” classification — either accept W8 as the approved consumer that activates it or explicitly revise that tiering.

**Option B — Decision Tree producer.**

This requires resolving the ordinal-8 / 3-bit composure-noise boundary before W8 and therefore couples W8 to the same digest/rebaseline choice as W9.

**Decision needed:** producer architecture. No implementation may infer the policy table, target selector, or timing rule.

### OD-W8-3 — FR-GK-043 empty-target fallback

The current requirement says “nearest own-team agent within the penalty area” but does not say what happens when there is no eligible teammate there.

Freeze one deterministic fallback before implementation. Candidate classes for owner review include:

- nearest eligible own-team outfielder anywhere in bounds;
- deterministic in-bounds zone target with no receiver;
- a separately specified emergency-clear behavior.

The implementation must not invent this after observing results.

---

## 3. ERR/spec obligations after the owner decision

The forced-release implementation divergence requires a Goalkeeper Mechanics #11 ERR. Reserve the next free #11 ERR only when the decision is accepted and the exact correction is known.

The same landing must distinguish:

1. **code defect back-propagation:** implement FR-GK-043's forced distribution/event behavior;
2. **new specification needed:** concrete #21 policy mapping, receiver selector, and commit timing;
3. **spec amendment only if authority changes:** any change to #11's currently named Decision Tree producer or to the owner of the Law-12 clock;
4. **schema obligation:** any semantic retirement/removal/replacement of `_gkHoldTicks`, `_gkReleaseCooldownRemaining`, or `_gkReleasedAgentId`.

No gameplay `[GT]` value is to be fitted as part of this decision pass.

---

## 4. What may be preregistered before the producer contract is chosen

The following outcome metrics are producer-neutral and may be frozen now:

- goalkeeper hand claims;
- goalkeeper feet-possession episodes;
- hand-hold duration distribution;
- feet-possession duration distribution;
- count/timing of existing engine six-second guard firings;
- count/timing of #11 hand-clock timeout transitions;
- restart counts/types;
- possession release and same/other-player reacquisition;
- pass attempts/completions following goalkeeper possession;
- existing source-complete foul/yellow/red census.

Do **not** yet freeze metrics whose meaning depends on the chosen producer, including “DT distribution commits” versus “engine distribution commits.”

---

## 5. Metrics to add after the owner contract is fixed, before W8 production wiring

Land a behavior-neutral, nonserialized diagnostic instrument first. Following the existing §10.1 diagnostic-state precedent, it should record at minimum:

- goalkeeper claims feeding the distribution population;
- distribution commit source;
- policy value at commit;
- voluntary versus timeout-forced commit;
- delivery kind;
- target class and selected receiver/zone;
- time from hand claim to `releaseTickEarliest`;
- time from `releaseTickEarliest` to voluntary commit;
- total hand-hold duration at release;
- feet-possession duration separately;
- forced timeout count;
- forced fallback reason, including empty-penalty-area cases;
- `DistributionExecutedEvent` count;
- restart count/type before and after release;
- immediate possession/reacquisition result;
- pass outcome/completion where the release enters Pass Mechanics;
- the unchanged source-complete foul/card report.

The frozen six-seed corpus remains the result-bearing comparison population unless a separate owner decision changes that contract.

---

## 6. Landing sequence after approval

1. Owner records OD-W8-1, OD-W8-2 and OD-W8-3.
2. File the #11 ERR and make the required same-commit approved-spec/back-propagation edits.
3. Land the W8 preregistration and nonserialized instrument **before** observing result-bearing W8 data.
4. Run the pre-wire baseline with the frozen corpus.
5. Implement W8 only.
6. Run the identical post-wire corpus and preserve durable evidence.
7. Interpret W8 independently before moving to W10/#441/W9.

W8 must not be bundled with W9 or W10. Any later #440/cooldown semantics change, W10 landing, #441 Heading reachability change, or W9 landing remains an invalidation trigger for the final KD-W1 calibration basis.

---

## 7. Explicit non-decisions

This document does **not**:

- choose whether the engine guard is narrowed or retired;
- choose a concrete #21 policy mapping;
- choose a receiver-selection algorithm;
- choose voluntary release timing;
- choose the FR-GK-043 empty-target fallback;
- authorize a Decision Tree ordinal-width change or digest rebaseline;
- authorize W9;
- change snapshot schema;
- tune distribution `[GT]` constants;
- modify production gameplay.

Those choices require owner approval first.
