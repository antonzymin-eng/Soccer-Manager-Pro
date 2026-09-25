# W8 Goalkeeper Distribution — Owner Decision Packet

> **Created:** September 25, 2026  
> **Status:** **DECISION REQUIRED — investigation only; no gameplay code, approved-spec change, schema change, RNG change, or `[GT]` tuning is authorized by this document.**  
> **Production anchor:** `c50726e67a6636cdc27a7abbc7ae91a1f5c29295` (`main`, PR #455 merge).  
> **Scope:** Resolve the W8 ownership/contract questions that must be settled before a result-bearing preregistration, diagnostic instrument, or production wiring is written.

## Owner decisions at a glance

| ID | Proposed choice for owner approval | Landing |
|---|---|---|
| OD-W8-1 | 2026/27 Law 12; #11 hand clock; offence-specific opponent corner; keep a feet-only stall guard. | C, after live distribution |
| OD-W8-2 | Match Engine produces #21-policy intents with a total receiver-or-zone selector, outside Decision Tree ordinal 8. | B |
| OD-W8-3 | Retire timeout-forced ROLL; empty-receiver fallback is covered by OD-W8-2. | N/A — covered by OD-W8-2 |
| OD-W8-4 | Extend #5 for a faithful GK request; one windup, CONTACT release and W5 registration. | B |

**Status:** recommendations only; no owner decision has been recorded. The frame, phase and contract rules below are planning obligations. At each implementation landing, transcribe the accepted rules into approved #11/#5/#21 and the relevant Match Engine contract **in the same commit as code**; those owning specs then take precedence over this decision packet.

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

**Match Engine guard.** `EnforceGoalkeeperReleaseRule` increments `_gkHoldTicks` whenever the current possessor is a goalkeeper. It is not hand-specific. Since W6, goalkeeper possession can be genuine Controlled possession at the feet, so this timer can include feet possession. Its cross-tick state is serialized from snapshot schema v19. It is currently the only keeper-at-feet anti-stall backstop; removing it before a separate feet-possession measurement would reopen that stall risk.

This path was introduced as a match-stall backstop while #11 distribution was not engine-driven. Its historical comment says future #11 distribution replaces the method body, but that comment predates W6/W3 making keeper possession and hand claims live.

**Goalkeeper Mechanics #11 clock.** #11's forced-release timing is based on `_claimTick`, set from a hand claim as `currentFrame / FramesPerTacticalTick`. It represents hand control rather than arbitrary goalkeeper possession, but its 10 Hz quantization loses the exact 60 Hz claim frame. The proposed 2026/27 *more than eight seconds* sanction therefore needs a serialized 60 Hz hand-claim start frame (or an explicitly approved quantization rule) before CONTACT-time adjudication; `_claimTick` alone cannot distinguish exact-boundary cases.

**Second six-second clock.** Independently, #11's `GoalkeeperStateMachine` takes the no-intent timeout when `(currentTick - _claimTick) >= GK_HOLD_MAX_TICKS` at 10 Hz. `_claimTick` was rounded down from the 60 Hz claim frame, so a mid-stride claim can reach this transition up to five physics frames before the engine's 360-frame guard. The current #11 physics path then marks release reached and recovers **without a kick**, while Match Engine may still hold the ball until its later ground drop. B must budget against the **earlier of these two clocks** and instrument both outcomes.

Law 12 applies to goalkeeper hand control, not ordinary feet possession. The two timers cannot be treated as interchangeable without an explicit owner decision.

### 1.5 FR-GK-043 and production code diverge

FR-GK-043 requires:

> After `GK_HOLD_MAX_TICKS` elapsed without a `DistributeIntent`, force a default **ROLL** to the nearest own-team agent within the penalty area.

Current #11 behavior does not implement that outcome. When its hand-claim timer forces `Distributing` without an active `DistributeIntent`, the 60 Hz path simply marks distribution release reached, publishes no `DistributionExecutedEvent`, and advances to recovery.

The keeper therefore does not stall, but the required forced ROLL and its event are missing.

Against the **current project spec**, this is a factual code/spec divergence: production does not perform the forced ROLL that FR-GK-043 requires. But the requirement's football-law premise is itself now under review (§1.8 below). Neither current nor historical IFAB Law 12 uses a forced ROLL as the timeout sanction. If the project adopts current law, FR-GK-028 / FR-GK-043 and the six-second `[FIXED]` constant require correction; under a historical ruleset, the forced ROLL still needs separate treatment as a project policy.

FR-GK-043 also leaves one edge case undefined if its forced-ROLL model is retained: no eligible own-team agent is inside the penalty area at timeout.

### 1.6 The current #11 distribution path does not execute a pass or move the ball

Goalkeeper #11 §3.8.3 normatively requires a distribution to construct a Pass Mechanics #5 intent and send it through the existing pass-intent surface before publishing `DistributionExecutedEvent`. The current implementation does not do that.

In `GoalkeeperMechanics.Update`, an active `DistributeIntent` is validated and converted into release-point / windup / emitted-power values. The code then publishes `DistributionExecutedEvent`, clears the intent latch, marks `distributionReleaseReached`, and advances the state machine. It does **not** initiate the production `PassExecutor`, release controlled possession through the Match Engine's canonical possession seam, arm the in-flight-pass receiver latch used by the W5 pass feed, or cause Pass Mechanics to reach its CONTACT-time `Ball.ApplyKick`.

Repository search finds no production consumer of `DistributionExecutedEvent`; its registration in Event System #17 does not execute the distribution. Therefore wiring only a producer into `CommitDistributeIntent` would still leave W8 behaviorally dormant at the executor boundary.

There is also a broader normative integration-surface defect in approved #11 §3.8.3–§3.8.4:

- `PassMechanics.ConsumePassIntent(passIntent)` does not exist anywhere in production `src/`;
- the pseudocode's `PassIntent` type does not exist in `src/pass-mechanics/`;
- `PassMechanics.DeliveryKind` does not exist; Pass Mechanics exposes `PassType` instead;
- the named `LowDriven` and `GroundRoll` delivery values do not exist in Pass Mechanics;
- the real `PassRequest` has no `sourcePoint`, `powerIntent`, `spinIntent`, or `deliveryKind` fields. `PassExecutor` derives launch speed, angle and spin from its own pass type, passer attributes and request fields.

So FR-GK-007's statement that distribution can use the existing #5 intent surface with “no #5 amendment required” is not established by the live interface. In particular, blindly translating a goalkeeper Throw/Roll/Kick into today's `PassRequest` would silently replace #11-owned release geometry / emitted-power / spin semantics with #5's foot-pass model. OD-W8-4 must therefore decide whether the canonical solution is a real #5 extension/adaptation surface, a narrowed subset that existing `PassRequest` can faithfully represent, or an explicitly re-specified execution boundary. The implementation must not pretend the current APIs are structurally equivalent.

Finally, `DistributionExecutedEvent.cs` currently documents that “`Ball.ApplyKick` precedes this event.” That statement is false on current production because no kick occurs at all. Treat the comment as stale documentation to correct atomically when the executor path lands; do not use it as evidence that execution already exists.

### 1.7 Receiver validation is also stubbed in the live #11 path

`GoalkeeperDistribution.ValidateTarget` supports FR-GK F-05 by taking an `agentRosterContains` input and falling back when the committed receiver has disappeared. The live `GoalkeeperMechanics.Update` call currently supplies `agentRosterContains: true` as a Stage-0 stub. A substituted/sent-off/missing receiver therefore cannot activate the required fallback.

W8 must replace that stub with an authoritative live-roster query at the execution boundary; it must not treat target validation as already wired.

### 1.8 The project's “Law 12 six-second rule” is no longer the current IFAB law

The current IFAB Laws of the Game **2026/27** retain the change introduced in 2025/26. A goalkeeper controlling the ball with the hands/arms inside the penalty area may do so for **eight seconds**; if control exceeds eight seconds, the restart is a **corner kick to the opponents**, with the referee visually counting down the final five seconds. Under the pre-2025/26 law, the limit was six seconds and the sanction was an **indirect free kick to the opponents**. Neither edition makes a forced ROLL the sanction.

The repository still encodes the superseded rule in multiple live/normative surfaces:

- #11 FR-GK-028: `GK_HOLD_MAX_TICKS = 60` at 10 Hz, tagged `[FIXED]` as a Laws-of-the-Game constant;
- #11 FR-GK-043: after six seconds the keeper must force a default ROLL;
- #11 §3.1 / §3.4 / §3.8 and tests repeat the same six-second model;
- Match Engine `EnforceGoalkeeperReleaseRule` calls its broad possession timer “Law 12” and force-releases possession after the same historical interval.

Repository search found no project-wide declaration that Soccer-Manager-Pro intentionally targets an older Laws edition. Ball Physics contains at least one 2024/25 law citation, but that is not a global rules-version policy.

**External authority checked for this decision packet:** [IFAB Laws of the Game 2026/27, Law 12.3](https://www.theifab.com/laws/latest/fouls-and-misconduct/) (“Corner kick”), [Law 17 §17.1](https://www.theifab.com/laws/latest/the-corner-kick/) (placement clause expressly says “or the goalkeeper’s position when penalised”), and the [IFAB 2025/26 change explanation](https://www.theifab.com/news/the-ifab-tackles-goalkeeper-time-wasting/) (earlier six-second indirect-free-kick sanction). The keeper-position clause was verified on the official 2026/27 Law 17 page, not inferred from ordinary boundary-exit corners.

Therefore W8 must not silently preserve “six seconds + forced release” merely because it is already tagged `[FIXED]`. The owner must either:

1. explicitly freeze the simulation to a named pre-2025/26 Laws edition, including its indirect-free-kick timeout sanction, or record a separately named project-specific departure; or
2. adopt the current Law 12 model, which changes both the `[FIXED]` duration and the timeout outcome/restart ownership.

This is a spec-governance decision, not gameplay tuning.

---

## 2. Owner decisions required before preregistration

### OD-W8-1 — Laws edition, hand-control authority, and the engine feet-possession guard

First choose the rules edition for this mechanic.

**Option CURRENT — adopt IFAB Laws of the Game 2026/27, Law 12.**

- Hand/arm control limit is eight seconds, not six.
- Exceeding the limit awards a corner kick to the opponents; it does not force a keeper distribution.
- #11 owns the hand-control clock. Preserve `_claimTick` for tactical decisions but capture the exact 60 Hz claim frame as serialized state for the law boundary; the current integer division by `FramesPerTacticalTick` is too coarse to adjudicate CONTACT exactly at eight seconds.
- Match Flow / restart ownership must apply the corner-kick outcome through the shared restart placement, taker, cue and event seams. The existing `CheckRestartAndApply` and `RestartResolver.Resolve(Corner, …)` are written for a ball exiting the field: a hand-hold offence needs a new entry point using the **keeper's position when penalised**, the opposing team as recipient, and an explicit deterministic Y-centre tie rule (provisionally high-Y, an arbitrary software convention inherited from the resolver's `<` comparison, **not** a football rule). Calling the boundary-exit path unchanged is not sufficient.
- FR-GK-028, FR-GK-043, the `[FIXED]` hold constant, related tests/comments, and the Match Engine “Law 12” wording require ERR/back-propagation.
- Voluntary distribution still needs to occur before the deadline through OD-W8-2/OD-W8-4.

**Option LEGACY — explicitly target a named pre-2025/26 Laws edition.**

- Hand/arm control limit is six seconds, with an **indirect free kick to the opponents** if exceeded. Specify its restart owner and boundary timing.
- Record the targeted edition so `[FIXED]` means “fixed to an explicit historical ruleset,” not “current football law.”
- A forced default ROLL is not the old-law sanction. If retained, specify it separately as an earlier voluntary-release policy or an explicit project-specific house rule; do not label it Law 12 compliance. A house rule that replaces the indirect free kick must be chosen expressly.

After the rules edition is chosen, choose the long-term ownership of any remaining timeout/stall behavior.

**Option A — #11 owns Law 12 from the hand-claim clock.**

- The exact 60 Hz claim frame is the authoritative Law-12 start; `_claimTick` remains the tactical state-machine cursor. Keep hand control live through `HandsOnBall` and the `Distributing` windup until actual CONTACT/release or a real cancellation, with the claim frame serialized and restored.
- The Match Engine's existing `_gkHoldTicks` guard remains as a separately named **non-law feet-possession stall safeguard** at the law-correction landing. Instrument its firings and retain the six-second inherited limit pending evidence and its proper source-tag/rationale; retire it only through a later measured decision. It must not also time hand control after the correction.
- Any removal or semantic change to the serialized engine fields must follow the snapshot-schema migration/rebaseline process.

**Option B — move Law 12 to Match Engine.**

This can consume the **existing** #11 `GoalkeeperMechanics.GetState(gkIndex)` distinction: `HandsOnBall` marks hand control, and `Distributing` remains hand control while the keeper still holds the ball through #5 windup. It needs a live-control/possession check and exact claim-frame handoff, not an invented new hand-vs-feet concept. Using the present `_possessingAgentId` test unchanged is not acceptable: it would count both hand control and feet possession, and treating `Distributing` windup as feet would let the safeguard cancel a valid launch.

**Proposed disposition for owner approval:** current 2026/27 law; #11's hand-control clock; Match Engine's shared restart seams with an offence-specific corner entry; retain the engine guard only for feet-possession stalls until measured. Stage this law correction **after** a working voluntary distribution path (§6), never on the pre-wire engine. Freeze the exact 10 Hz / 60 Hz boundary: actual CONTACT/release at elapsed 480 physics frames is legal; a committed intent alone does not stop the clock. If control persists into frame 481, a **new hand-offence check placed before the C3 pass-executor update loop in `RunResolvePhase`** must adjudicate before any #5 CONTACT, cancel the pending pass and award the corner once. The existing ground-drop guard runs **after** executors and first touch; reusing that slot would allow a late kick. Keep the feet-only safeguard there. C also removes #11's no-intent 10 Hz forced-recovery transition so hand control cannot disappear at the deadline without a real release or sanction. Any changed serialized guard or new claim-frame state requires a schema plan.

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
2. a **total**, deterministic receiver-or-zone selector for every live keeper hand claim under every #21 distribution policy: select a currently valid teammate when possible, otherwise supply a fixed, deterministic, in-bounds zone target. No eligible receiver (including all candidates filtered out by the policy, or roster/sent-off validation) must never mean no intent, a skipped commit, or a forced wait to the timeout. Specify the fixed zone's geometry, end-relative orientation and tie rule before preregistration; preserve the policy's delivery-kind semantics for zone targets;
3. deterministic commit timing between `releaseTickEarliest` and the deadline for **actual CONTACT**, accounting for #11's delivery-specific, config-backed windup (current Roll/Throw/Kick defaults 400/700/900 ms), its deterministic conversion into #5 physics frames, the 10 Hz commit stride and Resolve-phase ordering; in B, planned CONTACT must precede **both** #11's 10 Hz no-intent timeout and the inherited 360-frame ground-drop guard (whichever comes first for that claim), and the same voluntary schedule must be retained in C to isolate the sanction change;
4. what `SlowDown` and `Quick` mean in that timing rule;
5. the #21 §7 T4 “polish” classification — either accept W8 as the approved consumer that activates it or explicitly revise that tiering;
6. RNG policy for receiver selection and commit timing: either make each rule deterministic and draw-free, or name the existing/new deterministic RNG domain and draw-site ID explicitly. Any new draw site/order changes the digest stream and must be declared before measurement.

**Option B — Decision Tree producer.**

This requires resolving the ordinal-8 / 3-bit composure-noise boundary before W8 and therefore couples W8 to the same digest/rebaseline choice as W9.

**Decision needed:** producer architecture and the total selector contract, including the exact fixed zone. No implementation may infer the policy table, target selector, or timing rule. The selector's totality applies while the keeper still controls the ball in an active match; actual possession loss or interrupted play follows the normal cancellation path.

### OD-W8-3 — Retire the timeout-forced ROLL

FR-GK-043 currently demands a timeout-forced default ROLL toward the nearest own-team agent within the penalty area; no fallback is defined if that teammate does not exist. Under either real IFAB edition, this forced ROLL is not the timeout sanction. **Proposed disposition:** retire the timeout-forced ROLL when OD-W8-1 is corrected in C. A voluntary/default ROLL, if approved as one of #21's policies, uses OD-W8-2's **same total selector** and fixed in-bounds zone fallback in B and C. No separate empty-target decision or parallel fallback mechanism is needed; OD-W8-3's landing is N/A for fallback. Any retained special ROLL target preference must be specified as part of #21's policy mapping before B.

### OD-W8-4 — Distribution executor, possession release, and pass registration

A committed `DistributeIntent` currently stops at an event. The owner must fix the execution contract before preregistration.

The approved #11 contract already requires Pass Mechanics #5 rather than a goalkeeper-local kick implementation. The remaining decision is which composition-root surface owns the adaptation and ordering. The contract must state, before code:

1. how #11 distribution reaches canonical execution given that §3.8.3–§3.8.4 name a nonexistent `PassIntent`, `ConsumePassIntent`, and `PassMechanics.DeliveryKind`, while today's `PassRequest` cannot carry #11's source-point / power / spin / delivery payload without semantic loss;
2. which Match Engine phase initiates the executor and how #11's `ComputeWindupMs` becomes the **single** #5 windup by an explicit deterministic ms→frame rule, rather than silently dropping #11's duration or running two windups;
3. the exact ordering of controlled-possession release relative to executor initiation and CONTACT-time `Ball.ApplyKick`, including B's old guard during windup and C's strict *more than eight seconds* pre-CONTACT sanction;
4. how the in-flight-pass receiver latch is armed for a real selected receiver so W5's pass feed and possession-phase classification see goalkeeper distributions through the canonical path, and how a receiverless zone-target launch is represented without arming a ghost receiver latch;
5. when `DistributionExecutedEvent` is published — it must describe a real launched distribution, not substitute for launching one;
6. how the live roster/sent-off state replaces the current `agentRosterContains: true` stub so F-05 can actually fire; when a committed receiver disappears, preserve #11 F-05's last-known target point as a receiverless, in-bounds zone if valid, otherwise use OD-W8-2's fixed zone; revalidate at CONTACT and do not cancel the launch solely because the receiver is missing;
7. save/restore and snapshot consequences for any new cross-tick executor/adaptation state.

**Proposed disposition for owner approval:** extend Pass Mechanics #5 with an explicit goalkeeper-distribution request/variant that faithfully carries #11's delivery, emitted power and spin, with either a real receiver or a receiverless zone target. #11 supplies its delivery-specific `ComputeWindupMs`; #5 executes that as one frame-quantized windup, with #11's release point computed from the live keeper position at CONTACT. Match Engine owns the adapter and initiation, keeps possession through accepted initiation, uses CONTACT-time kick/release, arms W5 through the existing pass adapter exactly once, then publishes `DistributionExecutedEvent` only for a launched ball. Keep #11 in `Distributing` through the windup; replace its immediate `distributionReleaseReached = true` and immediate event with completion/cancellation feedback from #5. The receiverless variant must reach the same CONTACT-time launch through #5 without inventing a W5 receiver latch (the existing space-targeted `PassRequest` represents absent receiver as `TargetAgentId = -1`). A narrowed translation into today's foot-pass `PassRequest` would discard #11 semantics; a goalkeeper-local kick would duplicate #5. Fix the live roster check and serialize any new cross-tick state.

**Timing rule for the two landings:** In **B**, schedule each policy's commit early enough that #5 CONTACT occurs **strictly before the earlier of** (a) #11's 10 Hz `(currentTick - _claimTick) >= 60` no-intent transition and (b) Match Engine's 360-frame `_gkHoldTicks` ground drop. Budget with the actual serialized engine counter, rounded-down #11 claim tick, tactical dispatch, ms→frame rounding and Resolve ordering; do not assume the clocks share a start frame. `Execute` accepting at 5.9 s is not a release. Keep both old timeout paths unchanged for B and count them separately: without an intent, #11 may recover with no kick before the engine later drops the ball; with a late/injected pass, engine release can cancel #5 at CONTACT, and no distribution event may publish. Ordinary policy scheduling must depend on neither fallback. In **C**, hand control continues through windup and ends only at actual CONTACT/release. A CONTACT at exactly 480 frames after the precise hand claim is legal; if control survives to frame 481, the new offence check runs **before** the C3 executor loop and awards the corner before any pass CONTACT, even if an intent was committed earlier. The post-first-touch feet guard remains separate. Lock this ordering for both keeper ends and save/restore.

---

## 3. ERR/spec obligations after the owner decision

The forced-release implementation divergence and the missing execution seam require Goalkeeper Mechanics #11 error records once the owner decision fixes the intended correction. Reserve specific ERR IDs only when the decision is accepted and the exact defect boundaries are known.

The landing must distinguish:

1. **Law choice now, law/code correction in its own later landing:** resolve OD-W8-1 before preregistration, but do not change the six-second guard or #11 timeout spec before a live voluntary distributor exists. A standalone eight-second/corner patch on today's producer-less engine would make hand claims run to an opponent corner. After the W8 distribution landing and its separate measurement, correct FR-GK-028 / FR-GK-043 and the `[FIXED]` hold constant together with the code: eight seconds + opponent corner for current law, or six seconds + opponent indirect free kick for a named historical edition. Any forced ROLL is separate project policy. The intermediate old guard is documented as the existing **noncompliant ground-drop backstop**, not historical IFAB compliance;
2. **code fix:** replace the live `agentRosterContains: true` stub so F-05 receiver validation is reachable;
3. **spec defect + execution-contract repair:** #11 §3.8.3–§3.8.4 names a phantom #5 contract: nonexistent `PassIntent`, `PassMechanics.ConsumePassIntent`, `PassMechanics.DeliveryKind`, `LowDriven`, and `GroundRoll`. Today's `PassRequest` also lacks #11's source-point / power / spin / delivery fields and `PassExecutor` derives those semantics independently. File this drift explicitly; the proposed faithful #5 extension requires atomic #11/FR-GK-007 **and #5** amendment rather than claiming “no #5 amendment required.” Specify how #11's existing windup, release-point and emitted-power calculations feed the one #5 executor: retain their semantics, compute release geometry at CONTACT, convert windup once, keep `Distributing` live until real launch/cancel, and remove the current immediate-release/event path. No second GK-local kick;
4. **documentation/code correction:** repair the stale `DistributionExecutedEvent` comment claiming `Ball.ApplyKick` precedes the event, at the same time the real executor ordering is implemented and locked;
5. **new normative specification:** concrete #21 policy mapping, total receiver-or-zone selector with a fixed in-bounds zone rule, voluntary commit timing, RNG/draw-order rule, executor adaptation/phase ordering, and F-05's receiverless last-known-point fallback with fixed-zone replacement for an unusable point;
6. **spec back-propagation where authority changes:** amend #11/#21 integration text if the owner moves the producer away from Decision Tree #8, changes Law-12 ownership, or otherwise changes an approved normative owner;
7. **schema obligation:** evaluate any semantic retirement/removal/replacement of `_gkHoldTicks`, `_gkReleaseCooldownRemaining`, `_gkReleasedAgentId`, the new exact 60 Hz claim frame, and any new cross-tick executor/state-machine latch. Save/restore across the deadline and windup must preserve the next CONTACT or corner outcome.

Every new numeric policy/timing/power constant must carry an explicit source tag and valid-range rationale in its owning approved spec. Any new gameplay `[GT]` remains **uncalibrated under KD-W1** until the single complete-engine calibration pass; W8 must not fit those values to the observed corpus.

---

## 4. Candidate preregistration measures — not frozen by this packet

This decision packet does **not** freeze seeds, thresholds, acceptance bands, or falsifiers. After OD-W8-1 through OD-W8-4 are resolved, the W8 preregistration should consider these producer-neutral baseline measures:

- goalkeeper hand claims;
- goalkeeper feet-possession episodes;
- hand-hold duration distribution;
- feet-possession duration distribution;
- count/timing of existing engine keeper-possession hold-guard firings;
- count/timing of #11 hand-clock timeout transitions;
- restart counts/types;
- possession release and same/other-player reacquisition;
- pass attempts/completions following goalkeeper possession;
- existing source-complete foul/yellow/red census.

Do **not** define producer-dependent counters such as “DT distribution commits” versus “engine distribution commits” until the producer/executor contract is chosen.

The preregistration must include B and C fixtures with no eligible receiver, including a case where roster/sent-off validation removes the last candidate: assert a zone-target intent is committed and reaches one canonical CONTACT before the voluntary deadline, with no timeout-only corner in C and no ghost W5 receiver latch. Also cover a receiver disappearing after commit but before CONTACT: assert F-05 converts to its valid last-known in-bounds point (or the same fixed zone if unusable), revalidates at CONTACT, and launches once. Mirror keeper ends and restore mid-windup. The preregistration must also freeze falsifiers before any result-bearing W8 run. Candidate falsifier classes include: no new keeper-possession stall; no hand-control episode surviving beyond the chosen Law-12 deadline; no duplicate release/kick for one distribution; no `DistributionExecutedEvent` without a corresponding canonical pass execution; no immediate same-keeper reacquisition loop caused by the release path; W5's pass feed observing the launched distribution when a receiver exists; and a predeclared football/source-based band or shape check for hand-hold duration rather than a post-result “looks plausible” judgment. Exact thresholds belong in the later preregistration, not in this decision packet. The six-match corpus cannot by itself prove a rare timeout sanction: preregister deterministic boundary fixtures for hand control released exactly at the limit and continuing beyond it, feet possession beyond the limit, mirrored keeper ends and restart side, plus save/restore across claim, deadline and pass windup.

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
- time from commit to #5 CONTACT or cancellation, and planned versus actual margin to the six-/eight-second boundary;
- total hand-hold duration at release;
- feet-possession duration separately, with `HandsOnBall` and controlled `Distributing` windup excluded from the C feet-only guard;
- forced timeout count;
- target-fallback reason, including no eligible receiver, invalid/missing F-05 receiver, and unusable last-known point;
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

The total selector must operate identically in B and C; a no-receiver policy state cannot silently turn into a C corner. The frozen six-seed corpus remains the result-bearing comparison population unless a separate owner decision changes that contract. **B's voluntary releases are all scheduled before six seconds, and C keeps that schedule, so B→C should have little or no ordinary-corpus movement.** Freeze a pre-result band for that near-zero expectation; a large movement is a falsifier requiring investigation, not evidence of a successful law change. Preserve three distinct arms on identical seeds and instruments: **A** pre-wire engine/old ground-drop guard; **B** voluntary W8 producer/executor with the old guard unchanged; **C** the same working distribution with current-law hand timeout and a separate feet-only stall guard. A→B estimates distribution wiring; B→C estimates the law/restart change. Forced deadline fixtures, not six sampled matches, establish the rare offence's exact corner placement, recipient, event and save/restore behavior. Include commits before six/eight seconds whose CONTACT would fall after the respective limit; assert B's earlier-clock behavior and guard cancellation without a launch/event, plus a **mid-tactical-tick hand claim with no intent** showing #11's empty recovery before the later ground drop. For C, assert the corner before CONTACT, no duplicate restart, and an exactly-at-eight CONTACT that remains legal.

---

## 6. Landing sequence after approval

1. Owner records OD-W8-1 through OD-W8-4, including the two-landing isolation rule and the interim voluntary-release window **before** the inherited six-second ground-drop guard. This interim guard is not described as IFAB compliance.
2. File the #11/#5 producer/executor ERR record(s) and amend only those approved specs together with their W8 distribution code. Defer the Law-12/FR-GK-028/043 ERR back-propagation until its matching code landing; do not claim the pre-existing forced-ROLL divergence is resolved in the interim.
3. Freeze preregistration for A→B and B→C and land the behavior-neutral nonserialized instrument before result-bearing W8 data.
4. Run **A**, the pre-wire frozen corpus, and forced boundary baseline fixtures against the old ground-drop behavior.
5. Land **B**, the #21-policy producer and faithful #5 executor under the **unchanged** six-second engine guard **and #11 tactical timeout**. Keep voluntary CONTACT before whichever old clock fires first across all policy choices; do not implement a second goalkeeper-local kick.
6. Run the identical corpus and boundary fixtures for B; preserve evidence and interpret the distribution effect A→B.
7. Land **C** in a separate measured change: atomically correct the approved Law-12 spec, #11 no-intent timeout/recovery path and offence-specific opponent-corner restart. Add a hand-offence check **before** the C3 pass-executor loop; retain the old guard **after** first touch only as a named feet-possession safeguard. Apply the snapshot-schema plan and exact-limit/mirrored-corner tests.
8. Rerun the same corpus and forced fixtures; preserve evidence and interpret the law/restart effect B→C separately.
9. Only after both results are dispositioned proceed to W10/#441/W9. Any later policy-timing expansion is its own declared change; it is not silently folded into C.

W8 must not be bundled with W9 or W10. Any later #440/cooldown semantics change, W10 landing, #441 Heading reachability change, or W9 landing remains an invalidation trigger for the final KD-W1 calibration basis.

---

## 7. Explicit non-decisions

This document does **not**:

- choose current IFAB 2026/27 Law 12 versus a named historical edition or an explicit project house rule;
- approve or reject retaining the engine guard as a separately named feet-possession safeguard pending measurement;
- choose a concrete #21 policy mapping;
- choose a receiver-selection algorithm or the fixed zone's precise geometry;
- choose voluntary release timing or whether it consumes RNG;
- choose a receiver-selection RNG/domain/draw site;
- approve the proposed retirement of FR-GK-043's timeout-forced ROLL or the total OD-W8-2 receiver-or-zone fallback;
- approve or reject the explicit #5 distribution extension and Match Engine adapter/ordering proposed in §2;
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
| 0.10 | 2026-09-25 | draft | Makes OD-W8-2's selector total for every live hand claim: a valid receiver or a deterministic in-bounds zone under every #21 policy. Carries the fallback through F-05 invalidation, #5 receiverless execution and W5 latch handling, adds B/C no-eligible-receiver fixtures, and makes OD-W8-3's separate fallback N/A under this contract. |
| 0.9 | 2026-09-25 | draft | Two-clock and phase-order correction: B budgets actual CONTACT before both the rounded 10 Hz #11 no-intent timeout and the 360-frame engine guard; adds a mid-stride no-commit fixture. C places a distinct hand-offence check ahead of the pass-executor loop and removes #11's empty forced recovery; the feet guard remains after first touch. Adds a one-screen owner-decision summary and states accepted rules move into owning approved specs with matching code. |
| 0.8 | 2026-09-25 | draft | Boundary correction: verifies Law 17 §17.1 keeper-position placement directly; freezes B guard-versus-windup and C pre-CONTACT >8 s ordering using a precise serialized 60 Hz claim frame; names existing #11 hand states for the feet-only guard, preserves #11 windup/release-point semantics in the proposed #5 extension, and preregisters near-zero B→C ordinary-corpus expectation with forced boundary fixtures. |
| 0.7 | 2026-09-25 | draft | Review correction: separates W8 distribution wiring from the later Law-12/restart landing with A→B→C measurements; records the offence-specific keeper-position corner seam, preserves the feet-only stall guard pending evidence, chooses a faithful #5 extension as the proposed OD-W8-4 option, and fixes the §4 guard typo. The v0.5 row's “current 2025/26” wording describes the edition that introduced the change; 2026/27 is the current edition. |
| 0.6 | 2026-09-25 | draft | Advisor correction: names the current 2026/27 IFAB edition, distinguishes the pre-2025/26 six-second indirect-free-kick sanction from the project's forced ROLL, and calls for exact deadline/restore fixtures because six sampled matches cannot certify a rare timeout. |
| 0.5 | 2026-09-25 | draft | Evidence correction: current IFAB Law 12 (2025/26) is eight seconds with an opponent corner-kick sanction, not the repository's six-second forced-release model. OD-W8-1 now requires an explicit rules-edition decision before any timeout implementation; FR-GK-028/043 are treated as candidate spec defects if current law is adopted. |
| 0.4 | 2026-09-25 | draft | Integrity correction: broadens the #11/#5 defect from one nonexistent method to the full phantom §3.8.3–§3.8.4 contract — no `PassIntent`, no Pass Mechanics `DeliveryKind`, no `LowDriven`/`GroundRoll`, and current `PassRequest` cannot carry #11 source-point/power/spin/delivery semantics. OD-W8-4 now requires an explicit faithful execution contract and allows that #5 may need atomic amendment. |
| 0.3 | 2026-09-25 | draft | Review correction: records #11 §3.8.3's nonexistent `PassMechanics.ConsumePassIntent` surface as an explicit spec defect/back-propagation obligation, flags the false `DistributionExecutedEvent` ApplyKick-order comment, and tightens OD-W8-4 around the real `PassRequest` / `PassExecutor` integration seam. |
| 0.2 | 2026-09-25 | draft | Review correction: adds missing executor/pass-registration boundary (OD-W8-4), live-roster F-05 stub, RNG/draw-order and source-tag obligations, producer-neutral preregistration candidates/falsifier classes, and clarifies code-fix vs spec back-propagation terminology. |
| 0.1 | 2026-09-25 | draft | Initial decision packet: Law-12 authority, producer/ordinal boundary, #21 incomplete policy contract, FR-GK-043 divergence and empty-target fallback. |
