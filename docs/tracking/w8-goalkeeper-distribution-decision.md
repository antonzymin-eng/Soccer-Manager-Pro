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

### 1.6 The current #11 distribution path does not execute a pass or move the ball

Goalkeeper #11 §3.8.3 normatively requires a distribution to construct a Pass Mechanics #5 intent and send it through the existing pass-intent surface before publishing `DistributionExecutedEvent`. The current implementation does not do that.

In `GoalkeeperMechanics.Update`, an active `DistributeIntent` is validated and converted into release-point / windup / emitted-power values. The code then publishes `DistributionExecutedEvent`, clears the intent latch, marks `distributionReleaseReached`, and advances the state machine. It does **not** initiate the production `PassExecutor`, release controlled possession through the Match Engine's canonical possession seam, arm the in-flight-pass receiver latch used by the W5 pass feed, or cause Pass Mechanics to reach its CONTACT-time `Ball.ApplyKick`.

Repository search finds no production consumer of `DistributionExecutedEvent`; its registration in Event System #17 does not execute the distribution. Therefore wiring only a producer into `CommitDistributeIntent` would still leave W8 behaviorally dormant at the executor boundary.

### 1.7 Receiver validation is also stubbed in the live #11 path

`GoalkeeperDistribution.ValidateTarget` supports FR-GK F-05 by taking an `agentRosterContains` input and falling back when the committed receiver has disappeared. The live `GoalkeeperMechanics.Update` call currently supplies `agentRosterContains: true` as a Stage-0 stub. A substituted/sent-off/missing receiver therefore cannot activate the required fallback.

W8 must replace that stub with an authoritative live-roster query at the execution boundary; it must not treat target validation as already wired.

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
5. the #21 §7 T4 “polish” classification — either accept W8 as the approved consumer that activates it or explicitly revise that tiering;
6. RNG policy for receiver selection and commit timing: either make each rule deterministic and draw-free, or name the existing/new deterministic RNG domain and draw-site ID explicitly. Any new draw site/order changes the digest stream and must be declared before measurement.

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

### OD-W8-4 — Distribution executor, possession release, and pass registration

A committed `DistributeIntent` currently stops at an event. The owner must fix the execution contract before preregistration.

The approved #11 contract already requires Pass Mechanics #5 rather than a goalkeeper-local kick implementation. The remaining decision is which composition-root surface owns the adaptation and ordering. The contract must state, before code:

1. how a #11 `DistributeIntent` becomes the production `PassRequest` / `PassExecutor` input without bypassing Pass Mechanics;
2. which Match Engine phase initiates that executor and how #11's windup semantics compose with (rather than duplicate) #5's windup;
3. the exact ordering of controlled-possession release relative to executor initiation and CONTACT-time `Ball.ApplyKick`;
4. how the in-flight-pass receiver latch is armed so W5's pass feed and possession-phase classification see goalkeeper distributions through the same canonical path as other passes;
5. when `DistributionExecutedEvent` is published — it must describe a real launched distribution, not substitute for launching one;
6. how the live roster/sent-off state replaces the current `agentRosterContains: true` stub so F-05 can actually fire;
7. save/restore and snapshot consequences for any new cross-tick executor/adaptation state.

**Decision needed:** executor/adaptation owner and ordering. Direct `Ball.ApplyKick` from Goalkeeper Mechanics is not assumed: #11 FR-GK-007 and §3.8 point to Pass Mechanics #5 as the canonical execution surface.

---

## 3. ERR/spec obligations after the owner decision

The forced-release implementation divergence and the missing execution seam require Goalkeeper Mechanics #11 error records once the owner decision fixes the intended correction. Reserve specific ERR IDs only when the decision is accepted and the exact defect boundaries are known.

The landing must distinguish:

1. **code fix:** make the existing FR-GK-043 forced-release requirement actually produce the required distribution/event behavior;
2. **code fix:** replace the live `agentRosterContains: true` stub so F-05 receiver validation is reachable;
3. **execution-contract repair:** connect #11's distribution output to the canonical Pass Mechanics / Match Engine execution path rather than treating `DistributionExecutedEvent` as an executor;
4. **new normative specification:** concrete #21 policy mapping, receiver selector, voluntary commit timing, RNG/draw-order rule, executor adaptation/phase ordering, and the empty-target fallback;
5. **spec back-propagation where authority changes:** amend #11/#21 integration text if the owner moves the producer away from Decision Tree #8, changes Law-12 ownership, or otherwise changes an approved normative owner;
6. **schema obligation:** evaluate any semantic retirement/removal/replacement of `_gkHoldTicks`, `_gkReleaseCooldownRemaining`, `_gkReleasedAgentId`, or any new cross-tick executor state.

Every new numeric policy/timing/power constant must carry an explicit source tag and valid-range rationale in its owning approved spec. Any new gameplay `[GT]` remains **uncalibrated under KD-W1** until the single complete-engine calibration pass; W8 must not fit those values to the observed corpus.

---

## 4. Candidate preregistration measures — not frozen by this packet

This decision packet does **not** freeze seeds, thresholds, acceptance bands, or falsifiers. After OD-W8-1 through OD-W8-4 are resolved, the W8 preregistration should consider these producer-neutral baseline measures:

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

Do **not** define producer-dependent counters such as “DT distribution commits” versus “engine distribution commits” until the producer/executor contract is chosen.

The preregistration must also freeze falsifiers before any result-bearing W8 run. Candidate falsifier classes include: no new keeper-possession stall; no hand-control episode surviving beyond the chosen Law-12 deadline; no duplicate release/kick for one distribution; no `DistributionExecutedEvent` without a corresponding canonical pass execution; no immediate same-keeper reacquisition loop caused by the release path; W5's pass feed observing the launched distribution when a receiver exists; and a predeclared football/source-based band or shape check for hand-hold duration rather than a post-result “looks plausible” judgment. Exact thresholds belong in the later preregistration, not in this decision packet.

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
- PassExecutor initiation / CONTACT / completion-or-cancel counts for goalkeeper distributions;
- canonical possession-release count and ordering relative to pass initiation/contact;
- W5 in-flight/pass-feed registration count and receiver identity;
- ball-launch / `ApplyKick` reachability through Pass Mechanics, without a second goalkeeper-local physics path;
- F-05 receiver-missing validation/fallback count from the authoritative live roster;
- restart count/type before and after release;
- immediate possession/reacquisition result;
- pass outcome/completion where the release enters Pass Mechanics;
- the unchanged source-complete foul/card report.

The frozen six-seed corpus remains the result-bearing comparison population unless a separate owner decision changes that contract.

---

## 6. Landing sequence after approval

1. Owner records OD-W8-1 through OD-W8-4.
2. File the required #11 ERR record(s) and make only the approved same-commit spec back-propagation/new normative contract edits required by those decisions.
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
- choose voluntary release timing or whether it consumes RNG;
- choose a receiver-selection RNG/domain/draw site;
- choose the FR-GK-043 empty-target fallback;
- choose the executor/adaptation owner, possession-release ordering, or pass-feed registration seam;
- authorize a Decision Tree ordinal-width change or digest rebaseline;
- authorize W9;
- change snapshot schema;
- tune distribution `[GT]` constants;
- modify production gameplay.

Those choices require owner approval first.


---

## Version history

| Version | Date | Status | Notes |
|---|---|---|---|
| 0.2 | 2026-09-25 | draft | Review correction: adds missing executor/pass-registration boundary (OD-W8-4), live-roster F-05 stub, RNG/draw-order and source-tag obligations, producer-neutral preregistration candidates/falsifier classes, and clarifies code-fix vs spec back-propagation terminology. |
| 0.1 | 2026-09-25 | draft | Initial decision packet: Law-12 authority, producer/ordinal boundary, #21 incomplete policy contract, FR-GK-043 divergence and empty-target fallback. |
