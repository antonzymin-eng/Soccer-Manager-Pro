# W8 C — Proposed Law 12 amendment to Goalkeeper Mechanics #11

**Status:** PROPOSED — owner approval required; not an approved specification or implementation authority  
**Date:** October 16, 2026  
**Scope:** Documentation proposal only. No source, runtime, test-code, tuning, or schema changes in this draft.  
**Prerequisite:** Land and measure the W8 B voluntary-distribution behavior (currently in draft PR #511) before implementing C. Reconcile this proposal against the final merged B head.

## 1. Governing rule and boundaries

Adopt the current IFAB Laws of the Game 2026/27, Law 12.3 and Law 17. A goalkeeper controlling the ball with the hands/arms **inside their own penalty area** for **more than eight seconds** concedes a **corner kick to the opposing team**. The corner is taken on the side nearest the goalkeeper's position *when penalised*, not at the ball's last out-of-play coordinate. The referee decides when control began and counts down the last five seconds; the Stage-0 engine represents the control onset deterministically as the accepted 60 Hz hand-claim frame. The countdown can be presentation-only and MUST NOT shift the adjudication frame.

Hand control includes holding the ball between hands/arms or against another surface, holding it in open hands, and bouncing or tossing it while retaining control. These actions do not reset the clock. A keeper with hand control may not be challenged by an opponent. This offence is distinct from (a) ordinary goalkeeper possession at the feet, (b) prohibited re-handling following the keeper's own release (PR #511), (c) back-pass restrictions, and (d) general outside-own-penalty-area handball (D-07). No initial yellow card is generated merely for one eight-second offence; repeated-offence discipline follows Law 12 and the project's separate card-policy contract.

**Official authority:** [IFAB Law 12.3](https://www.theifab.com/laws/latest/fouls-and-misconduct/), [IFAB Law 17.1](https://www.theifab.com/laws/latest/the-corner-kick/), and [IFAB 2025/26 law-change explanation](https://www.theifab.com/law-changes/2025-26/). The 2026/27 edition retains the eight-second measure.

## 2. Proposed normative edits to #11

The following wording is proposed for insertion into the owning spec on approval; this draft file alone does not change its normative requirements.

### 2.1 Section 2 — functional requirements

**Replace FR-GK-028:**

> The goalkeeper hand-control episode MUST record the exact 60 Hz frame on which lawful hand control is established. When the keeper remains in live controlled hand possession inside their own penalty area for **more than 480 elapsed physics frames** after that frame, the engine MUST stop play before any same-frame pass-executor CONTACT and award an opponent corner using the keeper's position at the sanction. A genuine release at elapsed frame 480 is legal. The clock MUST survive keeper distribution WINDUP, retained-control cancellation, bouncing, and save/restore; ordinary feet possession does not count. The eight-second limit is a [FIXED] Laws-of-the-Game value, not gameplay tuning.

**Retire FR-GK-043's six-second forced ROLL requirement.** Reaching a hand-control deadline MUST NOT create a default Roll intent, move to Distributing without #5 acceptance, simulate a no-kick release, or force a keeper-specific ground drop. It creates the Law-12 offence and opponent corner if the eligibility predicate still holds. Preserve the old FR identifier in history with an explicit retirement/correction record; do not renumber other requirements.

Add explicit cross-references from FR-GK-029 and any hand-control definitions to the C clock and the existing canonical #5 CONTACT contract. Do not reintroduce phantom PassIntent APIs. File the owning #11 ERR/back-propagation entry at the spec-application landing, with ID allocated against that head; do not claim an unimplemented runtime defect resolved by documentation alone.

### 2.2 Section 3.1 — episode lifecycle and transition table

Remove the W8 B temporary no-intent transition row keyed to GK_HOLD_MAX_TICKS. Timeout is a **Resolve-phase infringement**, not a tactical state transition. HandsOnBall remains hand control until actual CONTACT/release, real possession loss, or sanction. Distributing remains hand control for the entire accepted #5 WINDUP while control is live. Only an accepted #5 request may enter Distributing.

The exact hand-control start frame is recorded at **all three live successful hand-claim sites**, not inferred from the 10 Hz tactical claim tick. Keep the existing tactical claimTick/releaseTickEarliest for policy scheduling (10 Hz, floor(currentFrame / FramesPerTacticalTick)); neither field has legal deadline authority. Use one 64-bit absolute physics-frame start value plus an explicit inactive sentinel/validity invariant. A continuing episode, including bounce/toss and retained-possession reject/cancel, preserves its original start. A new lawful hand-control episode after actual release has its own start, subject to any separate re-handling restriction.

The one authoritative possession-change/hand-episode teardown seam clears the start frame, pending intent, #5 wait/terminal-feedback state, and hand-control state exactly once on genuine loss, restart, takeover, or completed CONTACT. Distinguish completed #5 CONTACT from cancellation with the existing typed possession-change cause. ResetSlot and restore paths MUST preserve/clear the correct episode state without creating a phantom kick or corner.

### 2.3 Section 3.4 — source-tagged constant

Replace the old law constant row:

| Constant | Tag | Unit | Value | Valid range | Citation |
|---|---|---|---|---|---|
| GK_HAND_CONTROL_MAX_FRAMES | [FIXED] | physics frames @ 60 Hz | 480 (= 8 × 60) | n/a — current Law 12 | #11 §3.1, §3.8; IFAB Law 12.3; FR-GK-028 |

Retire GK_HOLD_MAX_TICKS = 60 as a six-second **law** constant and every associated law-labelled C branch. Match Engine's existing six-second keeper-possession ground-drop backstop is a different **non-law, feet-possession anti-stall safeguard**. At C it must run only for feet possession, never for HandsOnBall or still-controlled Distributing. Preserve its inherited duration until a measured follow-up decision; rename/retag its source to engine stall protection rather than IFAB Law 12. Any change to saved engine guard fields follows the snapshot/schema migration procedure. No new [GT], RNG draw, domain, or tuning change is authorized.

### 2.4 Section 3.8 — precise adjudication and Resolve order

A hand-control offence is eligible only if **all** of these are true at adjudication:

1. The relevant #11 keeper state is HandsOnBall or still-controlled Distributing.
2. The Match Engine's authoritative possessing agent is that keeper.
3. BallStateType is Controlled and the original lawful hand episode is still live.
4. The keeper's position is **inside their own penalty area**, including the boundary lines, using Ball Physics #1's current pitch/area geometry and team orientation.

At each 60 Hz Resolve frame, place **one** offence check **before** the C3 #5 pass-executor update loop and its CONTACT processing. Compute elapsedFrames = currentFrame - handControlStartFrame in the physics-frame domain; no rounded seconds or tactical-tick comparison is admissible.

- elapsedFrames = 0 on the recorded claim frame: no offence.
- elapsedFrames = 480: release/CONTACT during this frame is legal; merely initiating a distribution is not release.
- elapsedFrames >= 481 with all four predicates still true: stop play and award the corner **before** an executor can reach CONTACT on that frame.
- If possession was already released, the ball is no longer Controlled, a prior stoppage has ended the episode, or the keeper is outside their own penalty area: this specific eight-second corner MUST NOT fire.

**Worked boundary:** hand claim at frame 1,000. A genuine #5 CONTACT at frame 1,480 ends the episode lawfully. If the keeper still controls the ball at the beginning of Resolve on frame 1,481, the infringement is adjudicated first, even when a previously accepted #5 request would otherwise CONTACT on frame 1,481. Frame 1,481 results in one opponent corner, zero new ball kicks, zero DistributionExecutedEvent, and no later callback from the cancelled request.

**Atomic sanction behavior:** resolve exactly one offence/stoppage; cancel the accepted #5 request through an explicit cancellation/terminal-feedback path; suppress an in-flight ordinary pass, later CONTACT, distribution event, and any stale completion; end the hand episode; then use the shared match restart/taker/placement/cue/event machinery. Existing goal, foul, boundary, possession-loss and restart ownership MUST be reconciled so no same-frame competing restart or delayed second whistle can occur. An offence shall not be implemented by reusing the post-executor keeper-drop slot.

**Corner position:** recipient = opposing team. Use the **keeper's position at sanction** to select the nearer corner on that keeper's defended goal line. In the pitch's Y axis, select low-Y for Y less than half the pitch width, and high-Y for Y greater than half; at the exact centre tie choose **high-Y**, a deterministic project convention, *not* an IFAB rule. Mirror correctly for both defended ends. Feed this offence-specific corner through the shared corner restart seam; do not pass an invented ball boundary-exit position to an ordinary out-of-play resolver.

**Retry budget in C:** policy selection/delivery values remain as in approved B, with no late fallback ROLL. A retained-control retry may be initiated only when its actual candidate CONTACT can occur **no later than** claimFrame + 480 after the required tactical dispatch and configured windup/one-frame CONTACT update. This replaces B's earlier six-second guard budget for hand episodes; the feet-only safeguard never cancels live hand distribution. Keep B's already-approved earlier ordinary scheduling and measure any gameplay change separately.

### 2.5 Durable state and determinism

Serialize the exact 60 Hz claim frame and validity/lifecycle state in the next **available** snapshot schema at the C implementation landing; do not reserve a schema version in this proposal. Read/write order, ResetSlot, restore, mid-WINDUP pending #5 state, terminal feedback and restart state must deterministically reproduce the next release or corner. Review older saved keeper hold/cooldown/release fields for retention or retirement, and document migration/rebaseline instead of silently changing their meaning. No RNG draws or distribution policy/trajectory tuning are added. Digest differences caused solely by new serialized bytes are reported separately from gameplay events/ball trajectories.

## 3. D-07 — outside-own-area handling: separate owner decision

**Decision still required by the project owner; this draft does not decide it.**

**Recommended D-07 disposition: implement separately.** General goalkeeper hand/arm contact outside the keeper's **own** penalty area should be adjudicated under ordinary handball as a **direct free kick to the opponents** at the infringement position, with any applicable disciplinary consequences controlled by the owning foul/card policy. The area line belongs to the area: test an exact-line contact and a just-outside contact on both defended ends. Include holding/carrying the ball across the boundary, not only a new claim that starts outside. Do not award an eight-second corner for that episode.

Keep this distinct from PR #511's own-release re-handling restriction, whose outside-area DFK covers only that narrower event. Assign the general outside-area rule its own work item, code/ERR owner, ordering tests, and evidence. **Alternative D-07 choice:** an explicitly owner-approved governed deferral naming the residual noncompliance, issue, responsible phase, and measurable closeout gate. The roadmap permits implementation or explicit deferral before W8 C closeout; silent omission is not an option. Neither choice changes the four-part eligibility predicate above.

## 4. Required #11 Section 5 replacements and cross-spec locks

Retire the obsolete six-second tests T-5.1.1.4 and T-5.1.8.5, including their forced distribution/no-intent outcomes. Replace with deterministic contract tests covering:

1. Hand claim frame recorded at each successful producer site; elapsed 0/479/480/481; claim late within a six-frame tactical cell.
2. Exactly-on-480 CONTACT legal; 481 held hand control punished before same-frame CONTACT, with no kick/distribution event/late callback.
3. Controlled feet possession past 480 does not become a corner; engine anti-stall guard applies **only** to feet and is not represented as football law.
4. Full hand-control predicate in both HandsOnBall and controlled Distributing, including retained-possession reject/cancel and in-progress windup.
5. Bounce/toss does not reset the clock; actual release, real loss, restart, goal or foul tears down once; stale intent cannot cause a later restart.
6. Own penalty-area interior, all relevant exact boundary faces/corners, just outside the line, and mirrored team orientation.
7. Corner recipient, selected goal line, nearest Y side, exact Y-centre high-Y tie, shared restart placement/taker/event, and one restart only.
8. A general outside-area handball classified by the D-07 decision, never as a time-hold corner; separately distinguish PR #511 re-handling.
9. Save/restore at claim, before frame 480, at 480, before 481, and mid-WINDUP/cancellation with identical continuation, event ordering and restart outcome.
10. No new RNG draw, default policy switch, duplicate CONTACT or bypass of the existing #5 completion/cancellation feedback.
11. No first-offence automatic card; repeated offence handled by the separately approved foul/card policy.
12. Adversarial same-frame ordering against prior whistle, ball boundary, possession loss, successful #5 CONTACT, and replay/restore.

Also update #11 §3.1.1, §3.4, §3.8.3–3.8.4, §§2/5 version histories and any directly contradictory references in appendices, Match Engine, restart and test-contract documentation at the **approved C spec-application landing**. T-5.2.9 and other known stale tests naming phantom #5 PassIntent should be reconciled with the already approved B executor seam rather than restated in C.

## 5. Implementation gate and comparison protocol (no work authorized yet)

1. Owner explicitly approves the C wording, including the exact 480/481 boundary, Resolve-before-C3 ordering, corner placement/tie rule, claim-frame lifecycle, legacy retirement, and cross-spec test scope.
2. Owner records D-07: **separate implementation** (recommended) or **governed deferral**. Neither is silently included in C.
3. Final B production PR and A→B evidence land; use the actual post-B-law baseline (including the separate re-handling fix) as the B→C comparator, not the earlier pre-law B snapshot. Reconcile with PR #511 before C code.
4. Apply the approved normative #11/Match Engine/restart/test spec edits and ERR entries; then and only then implement C in an isolated code landing with the next available save schema version.
5. Run exact-boundary and mirrored restart fixtures, save/restore/replay, final-PR-head Linux checks and a **pinned Unity 6000.4.9f1 compile** before merge. Record six-seed B→C comparisons: for ordinary no-offence episodes, gameplay event and ball-trajectory streams must be exactly equal; report first divergence and cause otherwise. Record snapshot digests separately. Forced timeout fixtures, not six ordinary matches, establish rare sanction behavior.
6. Keep [GT] values frozen per KD-W1. No code changes, tests executed, Unity claims, or implementation approval are part of this document-only proposal.

**Requested owner decisions:** approve/amend the C specification text; choose D-07 implementation versus governed deferral. Until then, C implementation remains blocked.
