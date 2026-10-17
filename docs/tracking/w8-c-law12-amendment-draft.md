# W8 C — Proposed Law 12 amendment to Goalkeeper Mechanics #11

**Status:** PROPOSED — owner approval required; not an approved specification or implementation authority  
**Date:** October 17, 2026 (v0.2 review correction; original v0.1 October 16)  
**Scope:** Documentation proposal only. No source, runtime, test-code, tuning, or schema changes in this draft.  
**Prerequisite:** Land and measure the W8 B voluntary-distribution behavior (PR #511 is ready for review but not merged; its measured final head remains the B prerequisite) before implementing C. Reconcile this proposal against the final merged B head.

## 1. Governing rule and boundaries

Adopt the current IFAB Laws of the Game 2026/27, Law 12.3 and Law 17. A goalkeeper controlling the ball with the hands/arms **inside their own penalty area** for **more than eight seconds** concedes a **corner kick to the opposing team**. The corner is taken on the side nearest the goalkeeper's position *when penalised*, not at the ball's last out-of-play coordinate. The referee decides when control began and counts down the last five seconds; the Stage-0 engine represents the control onset deterministically as the accepted 60 Hz hand-claim frame. The countdown can be presentation-only and MUST NOT shift the adjudication frame.

Hand control includes holding the ball between hands/arms or against another surface, holding it in open hands, and bouncing or tossing it while retaining control. These actions do not reset the clock. A keeper with hand control may not be challenged by an opponent. **Control ends at physical hand release**, which can occur before a subsequent kick; *a punt/drop-kick must not be timed until the later foot CONTACT once a real drop has occurred*. This offence is distinct from (a) ordinary goalkeeper possession at the feet, (b) prohibited re-handling following the keeper's own release (PR #511), (c) back-pass restrictions, and (d) general outside-own-penalty-area handball (D-07). No initial yellow card is generated merely for one eight-second offence; repeated-offence discipline follows Law 12 and the project's separate card-policy contract.

**Official 2026/27 authority:** [IFAB Laws of the Game 2026/27, Law 12.3](https://www.theifab.com/laws/latest/fouls-and-misconduct/) and [Law 17.1](https://www.theifab.com/laws/latest/the-corner-kick/) (the current IFAB pages identify the selected edition as 2026/27); corroborated by [The FA's 2026/27 Law 17 publication](https://www.thefa.com/-/media/files/thefaportal/governance-docs/laws-of-the-game/2026-27/law-17---the-corner-kick.ashx). The [IFAB 2025/26 change record](https://www.theifab.com/law-changes/2025-26/) explains its introduction; it is not the sole source for the 2026/27 claim.

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

The one authoritative possession-change/hand-episode teardown seam clears the start frame, pending intent, #5 wait/terminal-feedback state, and hand-control state exactly once on genuine loss, restart, takeover, or completed CONTACT. For an explicitly modelled hand-to-air drop (future two-stage punt), terminate *only hand control and the Law-12 timer* at the genuine hand-release frame; a subsequent foot CONTACT is a separate later event and must not retroactively extend hand control. Distinguish completed #5 CONTACT from cancellation with the existing typed possession-change cause. ResetSlot and restore paths MUST preserve/clear the correct episode state without creating a phantom kick or corner.

### 2.3 Section 3.4 — source-tagged constant

Replace the old law constant row:

| Constant | Tag | Unit | Value | Valid range | Citation |
|---|---|---|---|---|---|
| GK_HAND_CONTROL_MAX_FRAMES | [FIXED] | physics frames @ 60 Hz | 480 (= 8 × 60) | n/a — current Law 12 | #11 §3.1, §3.8; IFAB Law 12.3; FR-GK-028 |

Retire GK_HOLD_MAX_TICKS = 60 as a six-second **law** constant and every associated law-labelled C branch. **Current-code fact, not a completed C behavior:** on both `main` and PR #511, Match Engine's `EnforceGoalkeeperReleaseRule` still increments `_gkHoldTicks` for **any** keeper possession (feet *and* hands) and forces a ground drop at the inherited 360-physics-frame limit; PR #511 also invokes `RecordHandRelease` there. It is not yet a feet-only guard. **Required C code change:** make the legacy backstop explicitly feet-only via `!HasGoalkeeperHandControl(holder)` (plus the narrowly scoped outside-area deferral exception in §3, if approved), reset/segregate its elapsed state at the hand-to-feet transition, and ensure its post-executor placement cannot override an active hand WINDUP. The eight-second check is separate and runs earlier. C must remove the old Law-12 interpretation from `MatchEngineConstants.GK_MAX_HOLD_SECONDS`, `GkMaxHoldTicks`, `EnforceGoalkeeperReleaseRule`, §5.Z.15 and relevant telemetry/guard comments. **Source-tag correction:** the inherited 6-second feet-stall number is not a Law-derived `[FIXED]` value; if temporarily retained, tag the base `GK_MAX_HOLD_SECONDS` `[EST]` as a provisional legacy engine anti-stall duration (unchanged 6 s, pending B-episode measurement and explicit owner review). `GkMaxHoldTicks` may remain `[DERIVED]` from that `[EST]` duration × fixed 60 Hz; replace its false Law-12 rationale. This is tag/rationale correction, not permission to calibrate or change the value. Confirm after B whether a feet guard is still necessary; separately measure feet-possession durations, guard firings and stall recurrence, then retire/ratify it through a later decision. No **new or calibrated** `[GT]`, RNG draw, domain or tuning change is authorized. Any change to saved guard fields follows snapshot/schema migration.

### 2.4 Section 3.8 — precise adjudication and Resolve order

A hand-control offence is eligible only if **all** of these are true at adjudication:

1. The relevant #11 keeper state is HandsOnBall or still-controlled Distributing.
2. The Match Engine's authoritative possessing agent is that keeper.
3. BallStateType is Controlled and the original lawful hand episode is still live.
4. The keeper's position is **inside their own penalty area**, including the boundary lines, using Ball Physics #1's current pitch/area geometry and team orientation.

At each 60 Hz Resolve frame, place **one** offence check **before** the C3 #5 pass-executor update loop and its CONTACT processing. Compute elapsedFrames = currentFrame - handControlStartFrame in the physics-frame domain; no rounded seconds or tactical-tick comparison is admissible.

- elapsedFrames = 0 on the recorded claim frame: no offence. **Explicit discretization choice:** claim frame counts as zero, so the legal frame indices run 0 through 480 inclusive (481 indexed frames); this preserves the recorded 480/481 contract instead of rounding down the claim time.
- elapsedFrames = 480: **actual hand release** during this frame is legal; merely initiating a distribution is not release. In the present B executor's atomic model, its CONTACT is also the hand-release event (see below).
- elapsedFrames >= 481 with all four predicates still true: stop play and award the corner **before** an executor can reach CONTACT on that frame.
- If possession was already released, the ball is no longer Controlled, a prior stoppage has ended the episode, or the keeper is outside their own penalty area: this specific eight-second corner MUST NOT fire.

**Current B delivery modelling versus physical release:** PR #511's dedicated #5 Roll, Throw and Kick variants preserve `Controlled` possession throughout their WINDUP; the executor does not currently perform a separate hand-to-air drop. In **that atomic Stage-0 model only**, successful #5 CONTACT/`ApplyKick` is the first actual release for all three variants. The Kick variant may use lofted foot-pass geometry, but it is **not a two-event physical punt or drop-kick**. Do not classify a keeper as having released a dropped ball merely because the Kick WINDUP started. If a physical punt/drop-kick is subsequently introduced, it MUST publish an authoritative earlier hand-release frame, end the timer then, keep the later kick as its own action, serialize the intermediate free-ball/windup state, and re-check possession/re-handling/restarters and W5 ordering; that extension requires separate specs, tests and approval. A hand release at 480 followed by a kick at 481 must be **legal under that future two-stage model**. C may not silently treat both models as equivalent.

**Worked boundary:** hand claim at frame 1,000. An **atomic-model** successful #5 CONTACT/hand release at frame 1,480 ends the episode lawfully. If the keeper still controls the ball at the beginning of Resolve on frame 1,481, the infringement is adjudicated first, even when a previously accepted #5 request would otherwise CONTACT on frame 1,481. Frame 1,481 results in one opponent corner, zero new ball kicks, zero DistributionExecutedEvent, and no later callback from the cancelled request.

**Atomic sanction behavior:** resolve exactly one offence/stoppage; cancel the accepted #5 request through an explicit cancellation/terminal-feedback path; suppress an in-flight ordinary pass, later CONTACT, distribution event, and any stale completion; end the hand episode; then use the shared match restart/taker/placement/cue/event machinery. Existing goal, foul, boundary, possession-loss and restart ownership MUST be reconciled so no same-frame competing restart or delayed second whistle can occur. An offence shall not be implemented by reusing the post-executor keeper-drop slot.

**Corner position:** recipient = opposing team. Use the **keeper's position at sanction** to select the nearer corner on that keeper's defended goal line. The goal-line **X coordinate flips** between the two defending teams; the lateral **Y side does not invert**. For either team choose low-Y when Y is below pitch-width/2, high-Y when Y is above, and **high-Y** on the exact centre tie — a deterministic project convention, *not* an IFAB rule. Feed this offence-specific corner through the shared corner restart seam; do not pass an invented ball boundary-exit position to an ordinary out-of-play resolver.

**Retry budget in C:** policy selection/delivery values remain as in approved B, with no late fallback ROLL. Under B's current atomic-release semantics, a retained-control retry may be initiated only when its actual candidate CONTACT **and hand release** can occur **no later than** claimFrame + 480 after required tactical dispatch, configured windup and one-frame CONTACT update. Any separately approved future two-stage delivery budgets the real hand release rather than a later foot CONTACT. This replaces B's earlier six-second guard budget for hand episodes; the feet-only safeguard never cancels live hand distribution. Keep B's already-approved earlier ordinary scheduling and measure any gameplay change separately.

### 2.5 Durable state and determinism

Serialize the exact 60 Hz claim frame and validity/lifecycle state in the next **available** snapshot schema at the C implementation landing; do not reserve a schema version in this proposal. Read/write order, ResetSlot, restore, mid-WINDUP pending #5 state, terminal feedback and restart state must deterministically reproduce the next release or corner. Review older saved keeper hold/cooldown/release fields for retention or retirement, and document migration/rebaseline instead of silently changing their meaning. No RNG draws or distribution policy/trajectory tuning are added. Digest differences caused solely by new serialized bytes are reported separately from gameplay events/ball trajectories.

## 3. D-07 — outside-own-area handling: separate owner decision

**Decision still required by the project owner; this draft does not decide it.**

**Recommended D-07 disposition: implement general outside-area handball before C activation/closeout.** General goalkeeper hand/arm contact outside the keeper's **own** penalty area should be adjudicated under ordinary handball as a **direct free kick to the opponents** at the infringement position, with any applicable disciplinary consequences controlled by the owning foul/card policy. The area line belongs to the area: test an exact-line contact and a just-outside contact on both defended ends. Include holding/carrying the ball across the boundary, not only a new claim that starts outside. Do not award an eight-second corner for that episode.

Keep this distinct from PR #511's own-release re-handling restriction, whose outside-area DFK covers only that narrower event. Assign the general outside-area rule its own work item, code/ERR owner, ordering tests, and evidence.

**Blocking stall invariant — applies whichever D-07 choice the owner makes:** a keeper MUST NOT be able to remain in hand control indefinitely outside their own penalty area. #11 presently permits claims without an adequate own-area admission gate. C's in-area Law-12 corner predicate correctly skips outside episodes, but simply making the inherited 360-frame drop feet-only would remove the final bound on those illegal outside episodes (regressing the historical §5.Z.15 hold stall). **Preferred solution:** ship the separately owned outside-area DFK and mandatory hand-episode teardown **before activating the C hands/feet guard split**. Test direct outside claims and hand control carried across an area line, including both orientations and exact boundary.

**Roadmap-permitted alternative (requires explicit owner approval):** defer the general outside-handball DFK, but **temporarily preserve the existing 360-frame anti-stall forced drop for outside-area live hand episodes**. Give this narrow exception an explicit `HasGoalkeeperHandControl(holder) && !InsideOwnPenaltyArea(holder)` branch, measuring elapsed 60 Hz hand possession, while the legacy guard becomes `!HasGoalkeeperHandControl(holder)` for feet. The outside-hand drop is a **non-law interim safeguard**, not an IFAB-compliant sanction or a corner; C must document the remaining illegal handling and explicitly lock a bounded-outside-hold fixture, teardown/replay and zero repeated free-claim loops. It must have a named ERR/issue, expiry decision and measured closeout gate. If a safe bounded deferral cannot be demonstrated, **D-07 implementation is required** before C ships. Neither choice expands the eight-second corner predicate.

## 4. Required #11 Section 5 replacements and cross-spec locks

Retire the obsolete six-second tests T-5.1.1.4 and T-5.1.8.5, including their forced distribution/no-intent outcomes. Replace with deterministic contract tests covering:

1. Hand claim frame recorded at each successful producer site; elapsed 0/479/480/481; claim late within a six-frame tactical cell.
2. Exactly-on-480 CONTACT legal; 481 held hand control punished before same-frame CONTACT, with no kick/distribution event/late callback.
3. Controlled feet possession past 480 does not become a corner; engine anti-stall guard becomes feet-only at C, **except** for an explicitly owner-approved, measured temporary outside-area hand-control exception (§3). It is not football law.
4. Full hand-control predicate in both HandsOnBall and controlled Distributing, including retained-possession reject/cancel and in-progress windup.
5. Bounce/toss does not reset the clock; actual hand release, real loss, restart, goal or foul tears down once; stale intent cannot cause a later restart. All current Roll/Throw/Kick variants end hand control at their atomic CONTACT; a separately modelled punt/drop must stop the clock at the earlier hand-release event and permit a later kick without a phantom corner.
6. Own penalty-area interior, all relevant exact boundary faces/corners, just outside the line, and mirrored team orientation.
7. Corner recipient, selected goal line, nearest Y side, exact Y-centre high-Y tie, shared restart placement/taker/event, and one restart only.
8. General outside-area handball classified by the D-07 decision, never as a time-hold corner; separately distinguish PR #511 re-handling. If D-07 is temporarily deferred, prove its **bounded 360-frame outside-hand fallback** prevents indefinite holding after the C guard split, with reset and save/restore.
9. Save/restore at claim, before frame 480, at 480, before 481, and mid-WINDUP/cancellation with identical continuation, event ordering and restart outcome.
10. No new RNG draw, default policy switch, duplicate CONTACT or bypass of the existing #5 completion/cancellation feedback.
11. No first-offence automatic card; repeated offence handled by the separately approved foul/card policy.
12. Adversarial same-frame ordering against prior whistle, ball boundary, possession loss, successful #5 CONTACT, and replay/restore.

Also update #11 §3.1.1, §3.4, §3.8.3–3.8.4, §§2/5 version histories and any directly contradictory references in appendices, Match Engine, restart and test-contract documentation at the **approved C spec-application landing**. T-5.2.9 and other known stale tests naming phantom #5 PassIntent should be reconciled with the already approved B executor seam rather than restated in C.

## 5. Implementation gate and comparison protocol (no work authorized yet)

1. Owner explicitly approves the C wording, including the exact 480/481 boundary, Resolve-before-C3 ordering, corner placement/tie rule, claim-frame lifecycle, legacy retirement, and cross-spec test scope.
2. Owner records D-07: **separate general outside-area handball implementation before C activation** (recommended) or **explicitly approved bounded fallback plus governed deferral**. A deferral without a verified outside-hand anti-stall bound is prohibited.
3. Final B production PR and its same-lane A→B/post-law evidence **land and pass review**; use only the validated merged B head and its post-re-handling baseline as the B→C comparator, not an older pre-law B snapshot. PR #511 is currently open; do not claim its evidence or measured six-seed reduction has been accepted by merge. **PR #513 was created from main rather than #511's head:** keep this proposal unmergeable by process until #511 lands, then reconcile the C proposal against merged §3.8 and both tracking-history chains before any approval or code.
4. Apply the approved normative #11/Match Engine/restart/test spec edits and ERR entries; then and only then implement C in an isolated code landing with the next available save schema version.
5. Run exact-boundary and mirrored restart fixtures, save/restore/replay, final-PR-head Linux checks and a **pinned Unity 6000.4.9f1 compile** before merge. Record six-seed B→C comparisons: for ordinary no-offence episodes, gameplay event and ball-trajectory streams must be exactly equal; report first divergence and cause otherwise. Record snapshot digests separately. Forced timeout fixtures, not six ordinary matches, establish rare sanction behavior.
6. Keep [GT] values frozen per KD-W1; relabel the **non-law 6 s legacy stall estimate** as `[EST]` and keep derived tick arithmetic `[DERIVED]` until the separate measurement/retirement decision. Verify the actual B Kick/Throw/Roll hand-release representation before code and refuse any unapproved two-stage punt model. No code changes, tests executed, Unity claims, or implementation approval are part of this document-only proposal.

**Requested owner decisions:** approve/amend the C specification text including the Stage-0 **atomic hand release at CONTACT**; choose D-07 implementation (recommended) versus expressly approved bounded interim outside-hand drop plus governed deferral. Until then, C implementation remains blocked.

## Review history

- **v0.1 — October 16:** standalone contract proposal for approval, without normative edits.
- **v0.2 — October 17:** Claude review corrections: close indefinite outside-area hand-hold if D-07 is deferred; identify current broad 360-frame keeper guard and C's `!HasGoalkeeperHandControl` conversion; retag non-law legacy duration `[EST]`; distinguish atomic B Kick from a genuine two-stage punt; cite IFAB's current 2026/27 pages; clarify inclusive claim-frame numbering and X-only mirroring. No code or owner approval.
