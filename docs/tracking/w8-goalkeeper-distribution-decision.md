# W8 Goalkeeper Distribution — Owner Decision Packet

> **Created:** September 25, 2026  
> **Status:** **ARCHITECTURE RECORDED — staged implementation authorized by the owner; numeric policy and execution details await B spec text. This packet changes no gameplay, approved spec, schema, RNG, or `[GT]`.**
> **Production anchor:** `c50726e67a6636cdc27a7abbc7ae91a1f5c29295` (`main`, PR #455 merge).  
> **Scope:** Record W8 ownership and the ordered A baseline, isolated possession-helper refactor, B spec, and B wiring boundaries.

## Owner decisions at a glance

| ID | Recorded architecture; details to specify in owning specs | Landing |
|---|---|---|
| OD-W8-1 | 2026/27 Law 12; #11 hand clock, live controlled possession **inside own penalty area** for the eight-second corner; separate outside-area handball; feet-only stall guard. | C, after live distribution |
| OD-W8-2 | Match Engine owns hand-held distribution, suppresses Decision Tree on-ball actions, and produces total #21-policy intents outside ordinal 8. | B |
| OD-W8-3 | Retire timeout-forced ROLL; empty-receiver fallback is covered by OD-W8-2. | N/A — covered by OD-W8-2 |
| OD-W8-4 | Extend #5 for a faithful GK request; serialized B windup/cancel, CONTACT release and W5 registration. | B |

**Owner direction (September 25, 2026):** proceed in this order: land a behavior-neutral, nonserialized instrument and preregistration **before reading A results**; run the frozen six-seed A baseline; land the separate, behavior-neutral possession-change helper with exact frozen-seed digest equality (it may be developed alongside A); amend #11, #5, #21 and Match Engine with the actual policy delays, delivery ranges, tie-break and punt-zone geometry and file the #11 ERRs **before any B wiring code**; then wire B and compare the same corpus with A. The architecture choices above govern that work. §7 now lists only unresolved numerical and policy details. Approved specs govern implementation once amended; this packet does not itself amend them. C's Law-12 correction remains a later, separately measured landing.

---

## 1. Verified current state

### 1.1 The #11 consumer exists and is unwired

`GoalkeeperMechanics.CommitDistributeIntent(int gkIndex, DistributeIntent intent)` exists, stores the intent, and arms `_distributeIntentActive`, but it has no production caller.

Goalkeeper Mechanics #11 currently names Decision Tree #8 as the producer:

- §3.1.1: `HandsOnBall → Distributing` when #8 commits `DistributeIntent` after `releaseTickEarliest`;
- §3.8: “Decision Tree #8 supplies `DistributeIntent`”;
- §4's integration surface exposes the corresponding keeper intent path.

The current engine independently documents that the Decision Tree has no **keeper-distribution** action. This does **not** mean the keeper cannot play an ordinary foot pass: `MatchEngine` gives every unsent-off agent, including keepers, a Decision Tree snapshot, and `OptionGenerator.GeneratePossessionBranch` has no keeper filter before PASS/SHOOT/DRIBBLE/HOLD. A keeper in `HandsOnBall` can therefore be offered outfield actions; their actual frequency and whether they end a hand episode are **unmeasured**. The new producer must take exclusive hand-control ownership so the tree cannot race its pass executor.

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

**Broken #11 clock in live composition.** `_claimTick` stores `currentFrame / FramesPerTacticalTick` (10 Hz units), but `MatchEngine.DriveGkHeadingTactical` calls `GoalkeeperMechanics.TacticalTick((int)_clock.CurrentTick, …)` with the **60 Hz frame index**. `GoalkeeperStateMachine` compares that raw argument with `_claimTick` at `>= GK_HOLD_MAX_TICKS` (60). Once a match is past roughly its first second, a post-claim tactical update can therefore trigger the no-intent `HandsOnBall → Distributing` transition almost immediately, rather than after six seconds. The current #11 physics path then marks release reached and recovers **without a kick**, while Match Engine may still hold the ball until its 360-frame drop. The old “up to five physics frames early” account assumed correctly scaled input and was false for production. A must instrument the raw argument, stored claim tick, elapsed frames and actual transition. File this as a #11 integration ERR with the B spec text; B must define and implement a single 10 Hz conversion at the composition seam, prove unit-correct boundary fixtures, and budget CONTACT before the corrected #11 timeout **and** inherited engine guard. Do not normalize the clock in A or in the behavior-neutral helper PR.

Law 12 applies to goalkeeper hand control, not ordinary feet possession. The two timers cannot be treated as interchangeable without an explicit owner decision.

### 1.5 FR-GK-043 and production code diverge

FR-GK-043 requires:

> After `GK_HOLD_MAX_TICKS` elapsed without a `DistributeIntent`, force a default **ROLL** to the nearest own-team agent within the penalty area.

Current #11 behavior does not implement that outcome. When its hand-claim timer forces `Distributing` without an active `DistributeIntent`, the 60 Hz path simply marks distribution release reached, publishes no `DistributionExecutedEvent`, and advances to recovery.

This #11 transition leaves `HandsOnBall` without a kick, but the keeper's authoritative possession can persist until the engine's separate drop or another action. The required forced ROLL and its event are missing; do not infer that every claim runs to either timeout without observing the Decision Tree, restart, foul and goal exits.

Against the **current project spec**, this is a factual code/spec divergence: production does not perform the forced ROLL that FR-GK-043 requires. But the requirement's football-law premise was reviewed (§1.10 below). Neither current nor historical IFAB Law 12 uses a forced ROLL as the timeout sanction. If the project adopts current law, FR-GK-028 / FR-GK-043 and the six-second `[FIXED]` constant require correction; under a historical ruleset, the forced ROLL still needs separate treatment as a project policy.

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

Finally, `DistributionExecutedEvent.cs` currently documents that “`Ball.ApplyKick` precedes this event.” That statement is false on current production because no kick occurs at all. Treat the comment as stale documentation to correct atomically when the executor path lands; do not use it as evidence that execution already exists. The event is registered for **Resolve**, but #11 currently publishes it from **Physics**; the first real production distribution must move publication to the registered phase or amend registration and prove phase order. This Tier-A event changes the digest, so B must declare and capture that movement.

### 1.7 Receiver validation is also stubbed in the live #11 path

`GoalkeeperDistribution.ValidateTarget` supports FR-GK F-05 by taking an `agentRosterContains` input and falling back when the committed receiver has disappeared. The live `GoalkeeperMechanics.Update` call currently supplies `agentRosterContains: true` as a Stage-0 stub. A substituted/sent-off/missing receiver therefore cannot activate the required fallback.

W8 must replace that stub with an authoritative live-roster query at the execution boundary; it must not treat target validation as already wired.

### 1.8 Hand-control teardown and B windup state are missing

`ApplyRestart` replaces the ball/possessor and clears claim intents, but there is no #11 entry point to end an existing `HandsOnBall`/`Distributing` episode when possession is lost. #11 also has no `ClearDistributeIntent` analogue of `ClearSaveIntent`/`ClearRushIntent`: `ResetSlot` alone clears a committed distribution, so an interrupted intent can survive into the next claim. The current `Distributing` branch marks release reached immediately even without an active intent. A real #5 windup in B needs a serialized cross-tick wait/feedback latch and explicit loss/cancel teardown, not that immediate release.

### 1.9 Narrow possession-change seam for the pre-B refactor

The helper centralizes the identity assignment so B can later attach #11 hand-episode teardown; the pre-B refactor must not invoke new teardown or change ball-state or restart semantics. Inventory every `_possessingAgentId` assignment at this production anchor before changing code; line numbers identify this revision and must be rechecked when implementing:

| Writer in `MatchEngine.cs` | Disposition for the isolated helper PR |
|---|---|
| `:750` initialization | Keep direct: no live keeper hand episode. |
| `:1110` opening-kickoff award | Keep direct: startup runs before a keeper can hold the ball. |
| `:3054` `TestOnly_ForceBallLoose` | Keep direct and explicitly test-only; it is not a production writer. |
| `:5427` `ApplyRestart` award | Route through the helper; B later uses this seam to end an existing keeper hand episode. |
| `:6091` unresolved interception release | Route through the helper. |
| `:6097` loose/deflected touch release | Route through the helper. |
| `:6849` snapshot restore | Keep direct: restore reconstructs state rather than creating a gameplay transition. |
| `:8477` `TakeControlledPossession` | Route through the helper before the existing ball-control operations. |
| `:8505` `ReleaseControlledPossession` | Route through the helper after the existing conditional ball-control release. |
| `:8579` `ReleasePossessionOnKick` | Route through the helper inside its existing current-holder guard. |

These are **six mid-match assignment sites**, one test-only site, and three initialization/restore sites. Preserve the existing guard, ball operations, claim cancellation, event timing and assignment order at each caller. Add a guard test that inventories all writers and fails if a new mid-match direct assignment bypasses the seam. Compare frozen-seed digests before and after this refactor exactly, including snapshot/restore; a difference requires investigation before B. B may then add teardown on an actual keeper hand-episode ownership loss via this one seam, with separate behavior tests. The B contract must capture the prior hand-control state before `ApplyRestart` replaces `_ball` or `ReleaseControlledPossession` releases its control, so the hook cannot miss a real episode after the caller has changed ball state. A's measurement instrument must remain nonserialized and behavior-neutral. The helper is not itself a distribution producer or a new Law-12 clock.

### 1.10 The project's “Law 12 six-second rule” is no longer the current IFAB law

The current IFAB Laws of the Game **2026/27** retain the change introduced in 2025/26. A goalkeeper controlling the ball with the hands/arms inside the penalty area may do so for **eight seconds**; if control exceeds eight seconds, the restart is a **corner kick to the opponents**, with the referee visually counting down the final five seconds. Under the pre-2025/26 law, the limit was six seconds and the sanction was an **indirect free kick to the opponents**. Neither edition makes a forced ROLL the sanction.

The eight-second entitlement and corner sanction require the keeper to be **inside their own penalty area**. IFAB Law 12 subjects a goalkeeper outside that area to the ordinary handball restrictions (a handball offence gives the opponents a direct free kick). The current claim producer and rush reach have no complete penalty-area gate, so an outside-area hand claim is reachable in the model. A must count claim/contact locations and exits; B specs must state how the producer treats such episodes. Before C, specify the separate outside-area handling-offence/restart path and the spatial test (including boundary and mirrored ends). A C corner check must never classify an outside-area episode as an eight-second offence.

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

## 2. Recorded architecture and B-spec obligations

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

- The exact 60 Hz claim frame is the authoritative Law-12 start; `_claimTick` remains the tactical state-machine cursor once its live unit mismatch is corrected in B. The C eight-second corner check requires **all four** at adjudication: #11 state `HandsOnBall` or still-controlled `Distributing`, `_possessingAgentId == keeperId`, `BallStateType.Controlled`, and keeper position inside their **own** penalty area. Keep hand control live through windup until actual CONTACT/release or a real loss/cancellation. Set the new frame at all three hand-claim sites, clear it on episode exit and `ResetSlot`, and serialize/restore it. An outside-area hand contact belongs to a separately specified handball/free-kick path, never this corner check.
- The Match Engine's existing `_gkHoldTicks` guard remains as a separately named **non-law feet-possession stall safeguard** at the law-correction landing. Instrument its firings and retain the six-second inherited limit pending evidence and its proper source-tag/rationale; retire it only through a later measured decision. It must not also time hand control after the correction.
- #11 exposes a possession-lost/hand-episode-ended entry point with a named Match Engine caller at authoritative loss (restart, goal, foul, tackle, or other possession/ball-state change). It clears the hand episode, claim frame, pending distribution intent and B wait latch once; neither a former keeper state nor a stale intent can yield a later phantom corner or kick. A real CONTACT ends control through the same canonical ownership transition.
- Any removal or semantic change to the serialized engine fields must follow the snapshot-schema migration/rebaseline process.

**Option B — move Law 12 to Match Engine.**

This can consume the **existing** #11 `GoalkeeperMechanics.GetState(gkIndex)` distinction: `HandsOnBall` marks hand control, and `Distributing` remains hand control while the keeper still holds the ball through #5 windup. It still requires Option A's live-control and own-penalty-area predicates, authoritative possession-lost entry point and exact claim-frame handoff, not an invented new hand-vs-feet concept. Using the present `_possessingAgentId` test unchanged is not acceptable: it would count both hand control and feet possession, and treating `Distributing` windup as feet would let the safeguard cancel a valid launch.

**Recorded architecture for later C implementation:** current 2026/27 law; #11's hand-control clock; Match Engine's shared restart seams with an offence-specific corner entry; retain the engine guard only for feet-possession stalls until measured. Stage this law correction **after** a working voluntary distribution path (§6), never on the pre-wire engine. Freeze the exact 10 Hz / 60 Hz boundary: actual CONTACT/release at elapsed 480 physics frames is legal; a committed intent alone does not stop the clock. If control persists into frame 481, a **new hand-offence check placed before the C3 pass-executor update loop in `RunResolvePhase`** must adjudicate before any #5 CONTACT, cancel the pending pass via an explicitly specified #5 cancellation/feedback seam (there is no public `PassExecutor.Cancel` today) and award the corner once. The existing ground-drop guard runs **after** executors and first touch; reusing that slot would allow a late kick. Keep the feet-only safeguard there. C also removes #11's no-intent 10 Hz forced-recovery transition so hand control cannot disappear at the deadline without a real release, a real loss, or sanction. C must check the live four-part control-and-area predicate before any eight-second offence; a restart, goal, foul or tackle must instead call #11's possession-lost entry point. Keep one Law-12 deadline constant; retag the old `GK_MAX_HOLD_SECONDS` as an explicitly non-law feet-stall value or retire it with code. Review the tackle exemption at `MatchEngine.cs:3753`, which currently treats keeper feet as hands, in C or explicitly defer it as a tracked issue. Add the precise 60 Hz claim frame at all three claim sites and clear it on exit/`ResetSlot`. B **already** needs a separate schema bump for the #5 windup latch; C's exact-frame additions require their own snapshot plan; the own-penalty-area boundary and outside-area handball outcome require separate C fixtures.

### OD-W8-2 — Production `DistributeIntent` producer

Two architectural choices exist.

**Option A — engine-side producer from #21 policy.**

- Keep DISTRIBUTE outside Decision Tree `ActionType`.
- Read the already-carried `TeamTactic.GkDistribution`.
- Convert that policy into a fully specified `DistributeIntent`.
- Commit the intent through #11's existing `CommitDistributeIntent` seam.
- This preserves the ordinal-8 decision for W9 rather than forcing it during W8.

The Match Engine must also own **exclusive** hand-held distribution arbitration: while #11 is `HandsOnBall` or a still-controlled `Distributing` windup, suppress the keeper's Decision Tree on-ball PASS/SHOOT/DRIBBLE/HOLD dispatch (and interrupt any in-flight foot action safely). Normal feet-possession and off-ball Decision Tree actions remain available outside that hand episode. A losing #5 `Execute` result must never be ignored as an ordinary no-op; record and disposition a rejected initiation without a duplicate launch.

This accepted architecture still requires new normative B specification for:
1. exact policy → delivery-kind / target-class / power defaults;
2. a **total**, deterministic receiver-or-zone selector for every live keeper hand claim under every #21 distribution policy: select a currently valid teammate when possible, otherwise supply a fixed, deterministic, in-bounds zone target off the keeper's own goal line. No eligible receiver (including all candidates filtered out by the policy, or roster/sent-off validation) must never mean no intent, a skipped commit, or a forced wait to the timeout. Freeze the A dry-run selector candidate before measurement; specify the final fixed zone's geometry, end-relative orientation and tie rule in B specs before wiring. Proposed default: preserve each policy's delivery-kind semantics for zone targets. The owner must explicitly settle whether receiverless `RollOut`/`ThrowOut` stays that type or uses a separately specified emergency kick; do not switch delivery silently;
3. deterministic per-policy commit delays between `releaseTickEarliest` and the deadline for **actual CONTACT**, accounting for #11's delivery-specific, config-backed windup (current Roll/Throw/Kick defaults 400/700/900 ms), its live allowed configuration bounds, deterministic conversion into #5 physics frames, the 10 Hz commit stride and Resolve-phase ordering; if the allowed maximum cannot meet the deadline, constrain/validate that configuration explicitly in the owning specs before B. In B, **after correcting the live #11 clock-unit mismatch**, planned CONTACT must precede **both** the now correctly scaled #11 10 Hz no-intent timeout and the inherited 360-frame ground-drop guard (whichever comes first for that claim), and the same voluntary schedule must be retained in C to isolate the sanction change;
4. what `SlowDown` and `Quick` mean in that timing rule;
5. the #21 §7 T4 “polish” classification — either accept W8 as the approved consumer that activates it or explicitly revise that tiering;
6. RNG policy for receiver selection and commit timing: either make each rule deterministic and draw-free, or name the existing/new deterministic RNG domain and draw-site ID explicitly. Any new draw site/order changes the digest stream and must be declared before measurement.

**Option B — Decision Tree producer.**

This requires resolving the ordinal-8 / 3-bit composure-noise boundary before W8 and therefore couples W8 to the same digest/rebaseline choice as W9.

**Recorded architecture; B spec still needed:** the engine-side producer and total selector are chosen. No implementation may infer the policy table, target selector, exact fixed punt-zone geometry, delivery ranges, tie-break, or timing rule. The selector's totality applies while the keeper still controls the ball in an active match; actual possession loss or interrupted play follows the new #11 possession-lost/cancellation path and disarms the committed intent.

### OD-W8-3 — Retire the timeout-forced ROLL

FR-GK-043 currently demands a timeout-forced default ROLL toward the nearest own-team agent within the penalty area; no fallback is defined if that teammate does not exist. Under either real IFAB edition, this forced ROLL is not the timeout sanction. **Proposed disposition:** retire the timeout-forced ROLL when OD-W8-1 is corrected in C. A voluntary/default ROLL, if approved as one of #21's policies, uses OD-W8-2's **same total selector** and fixed in-bounds zone fallback in B and C. No separate empty-target decision or parallel fallback mechanism is needed; OD-W8-3's landing is N/A for fallback. Any retained special ROLL target preference must be specified as part of #21's policy mapping before B.

### OD-W8-4 — Distribution executor, possession release, and pass registration

A committed `DistributeIntent` currently stops at an event. The detailed execution contract belongs in the owning B specs before wiring.

The approved #11 contract already requires Pass Mechanics #5 rather than a goalkeeper-local kick implementation. The remaining decision is which composition-root surface owns the adaptation and ordering. The contract must state, before code:

1. how #11 distribution reaches canonical execution given that §3.8.3–§3.8.4 name a nonexistent `PassIntent`, `ConsumePassIntent`, and `PassMechanics.DeliveryKind`, while today's `PassRequest` cannot carry #11's source-point / power / spin / delivery payload without semantic loss;
2. which Match Engine phase initiates the executor and how #11's `ComputeWindupMs` becomes the **single** #5 windup by an explicit deterministic ms→frame rule that deliberately bypasses the ordinary #5 urgency reduction and `MinWindupFrames` floor for this variant; account for the extra update frame between #5's WINDUP→CONTACT transition and its `ApplyKick`, rather than silently dropping #11's duration or running two windups;
3. the exact ordering of controlled-possession release relative to executor initiation and CONTACT-time `Ball.ApplyKick`, including B's old guard during windup and C's strict *more than eight seconds* pre-CONTACT sanction; specify a public #5 cancel/feedback API for restart, loss, guard or C sanction rather than assuming `PassExecutor` already has `Cancel` (its CONTACT-time FM-08 possession recheck alone cannot immediately clear a waiting #11 episode);
4. how the in-flight-pass receiver latch is armed for a real selected receiver so W5's pass feed and possession-phase classification see goalkeeper distributions through the canonical path, and how a receiverless zone-target launch is represented without arming a ghost receiver latch;
5. when `DistributionExecutedEvent` is published — it must describe a real launched distribution, not substitute for launching one; publish in its registered Resolve phase, after the kick, or amend the phase registration with an equivalent lock and declare the Tier-A digest change;
6. how the live roster/sent-off state replaces the current `agentRosterContains: true` stub so F-05 can actually fire; when a committed receiver disappears, preserve #11 F-05's last-known target point as a receiverless zone after F-09's pitch-bound clamp. Propose a new explicit safety exception: if that clamped point lies on the keeper's **own goal line**, replace it with OD-W8-2's fixed in-bounds zone; F-09's clamp alone permits this case. Freeze the goal-line criterion, end orientation and replacement in #11/#21's approved contract with matching code in B. Revalidate at CONTACT and do not cancel the launch solely because the receiver is missing;
7. save/restore and snapshot consequences for any new cross-tick executor/adaptation state: the B-stage #11 `Distributing` wait-for-#5 latch and terminal feedback belong in `GoalkeeperTickState`, any new #5 per-executor fields in `PassExecutorState`, and Match Engine bumps `SNAPSHOT_SCHEMA_VERSION` **in B**, independently of C's later 60 Hz frame/schema change;
8. exactly one distribution accuracy/error model: either #11's `ComputeAccuracyCoeff` feeding emitted power or #5's `ComputeErrorAngle` as adapted for keeper distributions; do not silently apply both.

**Recorded B architecture, with details pending B specs:** extend Pass Mechanics #5 with its **own #5-owned request/variant types** that faithfully carry #11's delivery, emitted power and spin, with either a real receiver or a receiverless zone target. #11 and #5 assemblies do not directly reference each other; Match Engine translates the #11 `DeliveryKind` at the adapter. #11 supplies its delivery-specific `ComputeWindupMs`; #5 executes that as one frame-quantized windup, with #11's release point computed from the live keeper position at CONTACT. Match Engine owns the adapter and initiation, keeps possession through accepted initiation, uses CONTACT-time kick/release, arms W5 through the existing pass adapter exactly once, then publishes `DistributionExecutedEvent` only for a launched ball. Keep #11 in `Distributing` through the windup using a serialized B-stage latch; replace its immediate `distributionReleaseReached = true` and immediate event with completion/cancellation feedback from #5. Add `ClearDistributeIntent` alongside #11's existing clear-intent APIs and invoke it on every loss/restart/cancel; `ResetSlot` alone does not clear interrupted episodes. The receiverless variant must reach the same CONTACT-time launch through #5 without inventing a W5 receiver latch (the existing space-targeted `PassRequest` represents absent receiver as `TargetAgentId = -1`). A narrowed translation into today's foot-pass `PassRequest` would discard #11 semantics; a goalkeeper-local kick would duplicate #5. Fix the live roster check, declare B's Tier-A event/digest movement and bump the snapshot schema in B for the #11/#5 cross-tick state, then separately in C as required.

**Timing rule for the two landings:** A measures the existing 60 Hz-versus-10 Hz #11 clock mismatch as found. In **B**, correct that mismatch at the composition seam and schedule each policy's commit early enough that #5 CONTACT occurs **strictly before the earlier of** (a) the corrected 10 Hz `(tacticalTick - _claimTick) >= 60` no-intent transition and (b) Match Engine's 360-frame `_gkHoldTicks` ground drop. Budget with the actual serialized engine counter, rounded-down #11 claim tick, tactical dispatch, ms→frame rounding and Resolve ordering; do not assume the clocks share a start frame. `Execute` accepting at 5.9 s is not a release. Keep the inherited six-second limits in B while correcting only #11's input units; count the two timeout paths separately: without an intent, #11 may recover with no kick before the engine later drops the ball; with a late/injected pass, engine release can cancel #5 at CONTACT, and no distribution event may publish. Ordinary policy scheduling must depend on neither fallback. In **C**, hand control continues through windup and ends only at actual CONTACT/release. A CONTACT at exactly 480 frames after the precise hand claim is legal; if control survives to frame 481, the new offence check runs **before** the C3 executor loop and awards the corner before any pass CONTACT, even if an intent was committed earlier. The post-first-touch feet guard remains separate. Lock this ordering for both keeper ends and save/restore.

---

## 3. ERR/spec obligations in the staged work

The forced-release implementation divergence and the missing execution seam require Goalkeeper Mechanics #11 error records. Check available IDs and file the B producer/executor defects with the B spec text before wiring; defer C's Law-12 correction and its closure until the matching C code. A filed ERR does not claim its runtime defect has been fixed.

The landing must distinguish:

1. **Law/code correction in its own later landing:** keep the recorded current-law choice, but do not change the six-second guard or #11 timeout spec before a live voluntary distributor exists. A standalone eight-second/corner patch on today's producer-less #11 path could turn hand claims that survive ordinary Decision Tree/restart exits into opponent corners; first measure those exits in A. After the W8 distribution landing and its separate measurement, correct FR-GK-028 / FR-GK-043 and the `[FIXED]` hold constant with C code: eight seconds + opponent corner for the chosen current law. Any forced ROLL is separate project policy. The intermediate old guard is documented as the existing **noncompliant ground-drop backstop**, not historical IFAB compliance;
2. **code fix:** replace the live `agentRosterContains: true` stub so F-05 receiver validation is reachable;
3. **spec defect + execution-contract repair:** #11 §3.8.3–§3.8.4 names a phantom #5 contract: nonexistent `PassIntent`, `PassMechanics.ConsumePassIntent`, `PassMechanics.DeliveryKind`, `LowDriven`, and `GroundRoll`. Today's `PassRequest` also lacks #11's source-point / power / spin / delivery fields and `PassExecutor` derives those semantics independently. File this drift explicitly; the proposed faithful #5 extension requires atomic #11/FR-GK-007 **and #5** amendment rather than claiming “no #5 amendment required.” Specify how #11's existing windup, release-point and emitted-power calculations feed the one #5 executor: retain their semantics, compute release geometry at CONTACT, convert windup once, keep `Distributing` live until real launch/cancel, and remove the current immediate-release/event path. No second GK-local kick;
4. **documentation/code correction:** repair the stale `DistributionExecutedEvent` comment claiming `Ball.ApplyKick` precedes the event, at the same time the real executor ordering is implemented and locked;
5. **new normative specification:** concrete #21 policy mapping, total receiver-or-zone selector with a fixed in-bounds zone rule, voluntary commit timing, RNG/draw-order rule, executor adaptation/phase ordering, and F-05's receiverless last-known-point fallback with fixed-zone replacement only when the F-09-clamped point is on the keeper's own goal line;
6. **spec back-propagation where authority changes:** amend #11/#21 integration text for the chosen engine-side producer and #11 hand-clock owner, plus #5 and Match Engine's execution contract, in the B spec landing before B wiring. The architecture choice does not silently approve numeric policy values, target geometry, timing or RNG;
7. **schema obligation:** B must serialize the new #11 wait/feedback latch in `GoalkeeperTickState` and any new #5 executor fields in `PassExecutorState` and bump `SNAPSHOT_SCHEMA_VERSION` for B. C separately evaluates semantic retirement/removal/replacement of `_gkHoldTicks`, `_gkReleaseCooldownRemaining`, `_gkReleasedAgentId`, and serializes its exact 60 Hz claim frame. Save/restore across deadline, restart, loss and windup must preserve the next CONTACT, cancellation or corner outcome;
8. **ownership and event obligations:** repair the #11 hand-possession-lost entry point and `ClearDistributeIntent`, forbid concurrent Decision Tree on-ball dispatch, reconcile `DistributionExecutedEvent`'s Physics publication with Resolve registration, declare the Tier-A digest change, and require #5-owned types plus one windup/error model.

Every new numeric policy/timing/power constant must carry an explicit source tag and valid-range rationale in its owning approved spec. Any new gameplay `[GT]` remains **uncalibrated under KD-W1** until the single complete-engine calibration pass; W8 must not fit those values to the observed corpus.

---

## 4. Stage A preregistration measures — freeze before looking at results

This decision packet does **not** freeze seeds, thresholds, acceptance bands, or falsifiers. A dedicated preregistration must freeze the six seeds, measures, end-reason classification and falsifiers and be merged alongside the nonserialized instrument **before any result-bearing baseline output is inspected**. It must include at least these producer-neutral baseline measures:

- goalkeeper hand claims per keeper, with the exact keeper exposure denominator and claim/contact location inside, on, or outside each keeper's own penalty-area boundary;
- goalkeeper feet-possession episodes;
- hand-hold duration distribution;
- feet-possession duration distribution;
- count/timing of existing engine keeper-possession hold-guard firings;
- count/timing of #11 hand-clock transitions with raw caller frame index, stored tactical claim tick, correctly converted tactical tick and elapsed frames;
- restart counts/types;
- possession release and same/other-player reacquisition;
- pass attempts/completions following goalkeeper possession, classified by hand/feet and receiver/zone;
- a historical W3 six-seed record counted **753 successful hand claims** (~63 per keeper per match); confirm the A claim rate at A's exact code SHA before comparing A→B, and print the exposure beside every A→B figure. Any real-football 5–10 claim estimate is unverified, so do not use it as a bound or tune KD-W1 values against it;
- how every hand hold ends: engine 360-frame drop, **live mis-scaled #11 transition** (with raw/normalized tick and elapsed-frame values), Decision Tree PASS/SHOOT/DRIBBLE, restart, goal, foul, tackle or another possession/ball-state change, including first divergent end reason and simultaneous triggers;
- same-keeper reclaim within a preregistered N-second window after the keeper's own drop, split into hand vs feet;
- last ball touch before each hand claim, including teammate deliberate kick/back-pass candidates for separate Law-12 classification;
- a behavior-neutral, RNG-neutral dry run of the **preregistered target-selection candidate** on A claims (no intent commit or launch) to count receiver vs zone fallback by policy, plus explicit per-policy fixtures because `SlowDown` is the default and a six-seed corpus may exercise only that value; freeze candidate inputs and outputs before inspection, then place the final tie-break, delivery ranges and punt-zone geometry in owning B specs before code;
- existing source-complete foul/yellow/red census.

Do **not** define producer-dependent counters such as “DT distribution commits” versus “engine distribution commits” until the producer/executor contract is chosen. Once the owner chooses Match Engine ownership, baseline instrumentation must count Decision Tree on-ball keeper actions and executor initiations independently of #11 distribution so the A premise is settled from observed episodes.

The preregistration must include B and C fixtures with no eligible receiver, including a case where roster/sent-off validation removes the last candidate: assert a zone-target intent is committed and reaches one canonical CONTACT before the voluntary deadline, with no timeout-only corner in C and no ghost W5 receiver latch. Also cover a receiver disappearing after commit but before CONTACT: assert F-05 converts to its F-09-clamped last-known point, uses the fixed zone if that point lies on the keeper's own goal line, revalidates at CONTACT, and launches once. Mirror keeper ends and restore mid-windup. The preregistration must also freeze falsifiers before any result-bearing W8 run. Candidate falsifier classes include: no new keeper-possession stall; no inside-own-area hand-control episode surviving beyond the chosen Law-12 deadline; no outside-area hand claim misclassified as an eight-second corner; no duplicate release/kick for one distribution; no `DistributionExecutedEvent` without a corresponding canonical pass execution; no immediate same-keeper reacquisition loop caused by the release path; W5's pass feed observing the launched distribution when a receiver exists; and a predeclared football/source-based band or shape check for hand-hold duration rather than a post-result “looks plausible” judgment. Exact thresholds belong in the later preregistration, not in this decision packet. The six-match corpus cannot by itself prove a rare timeout sanction: preregister deterministic boundary fixtures for hand control released exactly at the limit and continuing beyond it, feet possession beyond the limit, mirrored keeper ends and restart side, plus inside/outside-area hand contact and boundary fixtures for both keeper ends, and save/restore across claim, deadline and pass windup.

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
- target-fallback reason, including no eligible receiver, invalid/missing F-05 receiver, and an F-09-clamped last-known point on the keeper's own goal line;
- `DistributionExecutedEvent` count;
- PassExecutor initiation / CONTACT / completion-or-cancel counts for goalkeeper distributions;
- canonical possession-release count and ordering relative to pass initiation/contact;
- W5 in-flight/pass-feed registration count and receiver identity;
- ball-launch / `ApplyKick` reachability through Pass Mechanics, without a second goalkeeper-local physics path;
- F-05 receiver-missing validation/fallback count from the authoritative live roster;
- restart count/type before and after release;
- immediate possession/reacquisition result;
- pass outcome/completion where the release enters Pass Mechanics, reported separately for receiver-targeted and receiverless zone launches. A receiverless launch enters the loose-ball phase during flight and has no W5 receiver latch. Before A→B, freeze whether and how the canonical #5 outcome counts as a completed **zone** pass; do not treat absence of a receiver or loose-ball flight alone as a failure, and do not silently include a zone launch in a receiver-targeted completion denominator;
- the unchanged source-complete foul/card report.

The total selector must operate identically in B and C; a no-receiver policy state cannot silently turn into a C corner. The frozen six-seed corpus remains the per-claim comparison population unless a separate owner decision changes that contract; six seeds alone do not support claims about match-level outcomes. **B's voluntary releases are all scheduled before six seconds, and C keeps that schedule, so B→C should have little or no ordinary-corpus movement.** For ordinary no-offence episodes, preregister **exact equality** of gameplay event streams and ball-trajectory streams per seed between B and C, rather than a tolerance band. Snapshot digests can change because C adds claim-frame state: report the first divergent frame and its cause. A gameplay mismatch is a falsifier requiring investigation. Preserve three distinct arms on identical seeds and instruments: **A** pre-wire engine/old ground-drop guard and mis-scaled #11 clock; **B** voluntary W8 producer/executor with the old guard unchanged; **C** the same working distribution with current-law hand timeout and a separate feet-only stall guard. A→B measures the distribution wiring in the observed claim population; B→C is a **non-regression check** in ordinary play, not an estimate of the rare law effect. Forced deadline fixtures, not six sampled matches, establish the rare offence's exact corner placement, recipient, event and save/restore behavior. Run each B lock on pre-B code first and require it to fail for the intended reason; use perturbation checks that detect the earlier-of-two-clocks budget and C's offence-before-executor phase position. Include commits before six/eight seconds whose CONTACT would fall after the respective limit; assert B's earlier-clock behavior and guard cancellation without a launch/event, plus an **injected/defensive mid-tactical-tick hand claim with no intent** showing the corrected #11 six-second boundary; A separately records the live mis-scaled early recovery (a total ordinary producer does not naturally leave this path uncommitted). For C, assert the corner before CONTACT, no duplicate restart, and an exactly-at-eight CONTACT that remains legal.

---

## 6. Owner-directed landing sequence

1. Merge this packet and the current `main` into PR #456, run `check_drift.sh`, take it out of draft and merge. This records the architecture and ordered work, including B's Decision Tree suppression, hand-episode teardown and serialized latch, with numeric B details unresolved. The inherited guard is not described as IFAB compliance.
2. **A preregistration and baseline:** merge the behavior-neutral, nonserialized instrument and separate preregistration before inspecting results. Freeze the six seeds and dry-run selector; count claims per keeper and area, every hold end including keeper Decision Tree actions/restarts/six-second drops and mis-scaled #11 recoveries, same-keeper reclaims, and last touches. Run A on the frozen code/corpus and preserve its outputs.
3. **Pre-B helper:** in a separate PR, centralize only the six mid-match writers in §1.9, inventory and guard the direct exceptions, and prove frozen-seed digests exactly equal. This refactor may be developed alongside A; it cannot change A's behavior or be bundled with B wiring. Run the applicable `snapshot-schema-bump` and `dotnet-gate` checks.
4. **B spec text:** before **any** B wiring code, amend approved #11, #5, #21 and Match Engine with the actual per-policy commit delays, delivery ranges, deterministic receiver tie-break, punt-zone geometry, live windup bounds, the #11 tactical-clock unit correction, outside-area claim disposition, RNG/draw-order and executor/phase contract. Explicitly decide receiverless `RollOut`/`ThrowOut`. File the #11 ERRs after checking ID availability, including the phantom pass contract, producer drift and the live #11 clock-unit mismatch; identify which ERRs remain open until the corresponding code lands. The owner specifically orders the reviewable spec text before code for this slice. Defer Law-12/FR-GK-028/043 correction to C.
5. **B wiring:** implement the #21-policy producer and faithful #5 executor under the **unchanged** six-second engine guard, while correcting the #11 tactical-clock input to 10 Hz units as specified and recording that effect in A→B. Suppress on-ball Decision Tree dispatch during hand episodes; attach #11 possession-loss/`ClearDistributeIntent` teardown at the centralized ownership seam, add the serialized wait-for-#5 latch and #5 cancel/feedback, #5-owned request types and one error/windup model. Fix the event phase and declare its Tier-A digest effect; bump `SNAPSHOT_SCHEMA_VERSION` for `GoalkeeperTickState`/`PassExecutorState` changes. Keep voluntary CONTACT before whichever old clock fires first across all policy choices; do not implement a second goalkeeper-local kick.
6. Rerun the **same** corpus and boundary fixtures for B; preserve evidence and compare A→B per claim, end reason and outcome. No new calibration fit is inferred from these six matches.
7. **Later C, separately measured:** correct the approved Law-12 spec, #11 no-intent timeout/recovery path and offence-specific opponent-corner restart. Check the four-part live hand-control and own-area predicate, specify outside-area handball separately, and add an eight-second hand-offence check and #5 cancellation **before** the C3 pass-executor loop; retain the old guard **after** first touch only as a named feet-possession safeguard. Capture/clear/serialize the precise claim frame at all three sites, reconcile the two hold constants and the tackle feet-versus-hands exemption, and apply the C snapshot plan and exact-limit/mirrored-corner tests. Rerun the corpus and forced fixtures; compare B→C ordinary gameplay events and ball trajectories exactly. Forced fixtures prove the rare timeout/law result.
8. Only after the evidence is dispositioned proceed to W10/#441/W9. Any later policy-timing expansion is its own declared change; it is not silently folded into C.

W8 must not be bundled with W9 or W10. Any later #440/cooldown semantics change, W10 landing, #441 Heading reachability change, or W9 landing remains an invalidation trigger for the final KD-W1 calibration basis.

---

## 7. Explicit non-decisions

This architecture record does **not yet**:

- implement the chosen IFAB 2026/27 Law-12 correction; that is the later C landing;
- remove the inherited guard; its feet-only disposition remains subject to measurement and the C contract;
- choose a concrete #21 policy mapping;
- choose a receiver-selection algorithm or the fixed zone's precise geometry;
- choose voluntary release timing or whether it consumes RNG;
- choose a receiver-selection RNG/domain/draw site;
- implement FR-GK-043's retirement or the chosen total receiver-or-zone fallback; exact receiverless RollOut/ThrowOut delivery belongs to the B specs;
- settle the detailed #5 adapter/phase and cancellation API; B specs must do so before wiring;
- authorize a Decision Tree ordinal-width change or digest rebaseline;
- authorize W9;
- change snapshot schema;
- tune distribution `[GT]` constants;
- modify production gameplay.

The owner's staged direction supersedes the v0.12 draft non-decisions for architecture and sequence. Exact #21 policy values, selector and zone geometry, delivery ranges, timing, RNG and #5 execution details require owning-spec approval before B code. Until those amendments land, approved #11 governs current behavior.


---

## Version history

| Version | Date | Status | Notes |
|---|---|---|---|
| 0.13 | 2026-09-25 | architecture recorded | Owner-directed A preregistration/instrument before results, separate behavior-neutral possession helper with explicit ten-writer disposition and exact digest parity, then #11/#5/#21/Match Engine numeric B specs and #11 ERRs before B wiring; same-corpus A→B comparison. Bounds live windup configuration and leaves receiverless RollOut/ThrowOut to B specs. Review correction: A measures the real 60 Hz/10 Hz #11 clock mismatch, B corrects it after its ERR/spec text, and C limits the eight-second corner to own-area hand control with outside-area handling specified separately. |
| 0.12 | 2026-09-25 | draft | Native two-lens advisor correction: Decision Tree keeper foot-action competition; live-control and possession-loss teardown; clearable distribution intent; serialized B windup latch and independent B/C schema changes; architecture-only approval governance. Carries event phase, #5 composition and cancellation obligations, and updates A baseline and B→C non-regression preregistration candidates. |
| 0.11 | 2026-09-25 | draft | Defines the F-05 goal-line safety exception after F-09 clamping as a proposed new B-stage rule, labels the B no-intent timeout fixture injected/defensive, and requires separate receiverless-zone pass outcomes/completion denominators before A→B measurement. |
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
