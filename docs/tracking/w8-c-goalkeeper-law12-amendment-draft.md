# W8 C — Goalkeeper Mechanics #11 Law 12 amendment (owner-review draft)

**Status:** PROPOSED — NOT APPROVED. Documentation only; no gameplay change or authorization to implement.
**Prepared:** October 16, 2026. **Target:** Goalkeeper Mechanics #11, especially §2 FR-GK-028/043, §3.1, §3.4, §3.8, and the Match Engine Resolve/restart interfaces.
**Sequence:** Roadmap step 8, after W8 B step 7 has landed, been measured and received its final-head host evidence. No C implementation until the owner approves this contract and closes D-07.
**Working base:** main `1963cfbeb2b33465e4b6f22ab26a58e4d53446f5`. PR #511 is a **separate, open W8 B + own-release re-handling implementation**, including ERR-011-020 and snapshot body v27 on its branch. Reconcile these proposed #11 amendments against the **merged** #511 text before ratification; do not overwrite its own-release law or assume that v27 remains the next current schema.

## 1. Authority and scope

This proposal adopts the IFAB **2026/27** edition:
- [Law 12.3](https://www.theifab.com/laws/latest/fouls-and-misconduct/): goalkeeper **inside their own penalty area** controlling the ball by hand/arm for **more than eight seconds** is penalised by an **opponents' corner kick**. Control includes holding, bouncing and tossing the ball in the air. The referee determines the start of control and signals the last five seconds.
- [Law 17.1](https://www.theifab.com/laws/latest/the-corner-kick/): the corner area is the one **nearest to the goalkeeper's position when penalised**, not necessarily the side on which the ball last crossed the goal line.
- This replaces the historical six-second indirect-free-kick rule. A forced ROLL or ground-drop is neither the current nor the old Law-12 penalty.

W8 C applies to hand-control episodes following a genuine hand claim. **Feet-only control never starts an eight-second law clock.** An existing #11 `Distributing` state remains hand control until canonical #5 CONTACT or an authoritative loss; accepting a pass request does not release the ball. **General outside-own-area handling is not an eight-second corner** and is subject to owner decision D-07 below. Other Law-12 prohibitions (own-release re-handling, deliberate teammate kick/throw-in, etc.) remain separate; this change does not replace those sanctions.

## 2. Proposed replacement requirements in #11 §2

**Replace FR-GK-028:**

> FR-GK-028 | MUST | Each genuine goalkeeper hand-control episode records the exact **60 Hz physics-frame index** at first controlled hand claim. The fixed maximum is `GK_HAND_CONTROL_MAX_FRAMES = 480` physics frames (8 seconds at 60 Hz). While #11 reports `HandsOnBall` or still-controlled `Distributing`, the keeper remains the possessor, the ball is `Controlled`, and the keeper is inside their own penalty area, release at elapsed frame 480 is legal; if the same hand control persists at elapsed frame 481, award a **corner kick to the opponents** before a pending pass CONTACT. A tactical 10 Hz tick is not a lawful precision clock. | Law 12.3 / W8 C | §3.1 / §3.4 / §3.8 |

**Replace FR-GK-043:**

> FR-GK-043 | MUST | A keeper hand episode ends only on actual canonical release/CONTACT, authoritative ownership loss, or an adjudicated law offence. No six-second timeout forces ROLL, transitions the keeper into recovery, drops hand possession or silently clears the claim; a rejected/cancelled request retaining possession does not reset the law clock. On an eight-second offence, cancel a pending dedicated goalkeeper pass through its explicit #5 cancellation/feedback path, prevent CONTACT and duplicate publication, clear the episode exactly once and award one opponents' corner via the shared restart system. | Law 12.3 / W8 C | §3.1 / §3.8 |

**Data contract / field ownership:** retain `_claimTick` and `releaseTickEarliest` in their **10 Hz tactical** domain for B scheduling. Add a separate, authoritative `handClaimFrame60` (or equivalently named exact frame) in #11's serialized `GoalkeeperTickState` and the canonical engine snapshot. Stamp it at **all three real hand-claim entry sites** through one consistent claim/ownership seam, not via `tacticalTick × 6` or an end-of-tick approximation. Clear it on genuine episode exit and `ResetSlot`, never on merely submitting/rejecting an intent. Restore the same index, not an age inferred from current clock. Declare the next available snapshot-body schema, full round-trip compatibility policy and digest rebaseline **when implementing C**, not in this docs PR.

## 3. Proposed §3.1 / §3.4 / §3.8 amendment

### §3.1 transition semantics

Remove the legacy `HandsOnBall → HandsOnBall` six-second no-intent timeout condition. Remove any no-intent `HandsOnBall → Recovering` or synthetic `Distributing` transition keyed to that expired six-second guard. The accepted B #5 request still gates `HandsOnBall → Distributing`; voluntary distribution remains on B's unchanged schedule. Retained-possession cancellation returns `Distributing → HandsOnBall` **without changing the original hand claim frame**. Actual CONTACT, restart, goal, tackle, loss of controlled ball or any other authoritative change in possessor closes the episode through the existing ownership seam.

### §3.4 fixed constant and guard retirement

| Symbol / domain | Proposed C treatment | Classification |
|---|---|---|
| `GK_HAND_CONTROL_MAX_FRAMES = 480` @ 60 Hz | Only Law-12 hold deadline | **[FIXED]**, IFAB 2026/27 Law 12.3 |
| `GK_HOLD_MAX_TICKS = 60` @ 10 Hz | **Retire** as a Law-12 deadline and as a no-intent forced-recovery/ROLL trigger | Superseded historical hand law |
| `GK_MAX_HOLD_SECONDS = 6` / `GkMaxHoldTicks = 360` | **Remove from every hand-control timing/enforcement path.** If retained, rename/source-tag explicitly as a **non-law feet-only anti-stall safeguard**, instrument firings, and retain its 360-frame value only pending separate measurement/owner retirement | Project safeguard, NOT [FIXED] football law |

A single fixed Law-12 hand deadline must remain. No 10 Hz rounding, second cumulative hand clock or after-executor ground-drop may silently enforce it. The feet-only guard must not tick or drop the ball during `HandsOnBall` or still-controlled `Distributing`, including pending windup; it may continue in its existing later stage of Resolve for keeper **feet** possession only. Do not remove the feet safeguard incidentally: its final retirement is a separate measured decision.

### §3.8 / Match Engine Resolve order

Add a deterministic **hand-offence adjudication step in `RunResolvePhase` before the C3 `PassExecutor` update loop**. The step reads one keeper-owned 60 Hz claim frame and requires **all** of:
1. #11 live state is `HandsOnBall` or still-controlled `Distributing`;
2. authoritative `_possessingAgentId` identifies that same keeper;
3. `BallStateType.Controlled` is still true for that keeper; and
4. the keeper's position is inside or on the boundary of **their own** penalty area at the adjudication frame.

Elapsed frames are `currentPhysicsFrame - handClaimFrame60`, with **claim = elapsed 0**. If `elapsed <= 480`, no offence; if `elapsed >= 481` and all four conditions hold, the check atomically (a) cancels any keeper #5 windup before its CONTACT, consuming exactly one terminal cancellation/feedback, (b) clears hand claim, pending intent and B latch through canonical possession-loss teardown, and (c) awards exactly one opponents' corner. No `Ball.ApplyKick`, `DistributionExecutedEvent`, `PassAttemptEvent` or ghost W5 receiver completion may appear for a cancelled attempt. Existing earlier terminal events/restart decisions must not be overwritten by a stale keeper timeout, and the same-frame foul/restart queue must not produce a second whistle.

**Boundary example:** claim at frame 10,000. A genuine #5 CONTACT at frame 10,480 is lawful, ends control and suppresses the timeout. An accepted request that would CONTACT at frame 10,481 is **not** a release at 10,480: the pre-executor check at frame 10,481 cancels it and awards the corner. A keeper with only feet possession at 10,481 is not sanctioned under this rule.

### Opponent-corner restart

Use the existing shared restart lifecycle (ownership, event/cue, set-piece state and taker selection), but expose a distinct **offence-origin corner** entry that takes the penalised keeper's **position**, the defending keeper's team and a stable frame. The ordinary goal-line boundary resolver cannot be reused unchanged. The opponents receive the corner on their attacking goal line at the corner nearest the keeper's **Y** at sanction; for a mathematical centre-line tie `y == 34` m, **choose the high-Y corner** as an explicit software tie-break (not an IFAB rule). Mirror both goal ends; do not infer recipient from last ball touch. Preserve ordinary restart lifecycle and single-event semantics. An offence cannot create a second restart when a legal release or another higher-priority terminal event already ended the episode.

### Own-area spatial rule

Use the live team's own goal-end mapping, not a hardcoded left-hand goal. On a 105 × 68 m pitch, the inclusive keeper-own penalty area spans 16.5 m from the own goal line and `y ∈ [34 - 20.16, 34 + 20.16]`. Every boundary is **inside** for this law predicate. Keeper outside own area fails the corner predicate even when a stale hand state exists; D-07 decides the separate handling response. Claim-contact location and position at sanction are independently recorded: the former determines an initial outside-area contact offence, while Law 17 uses the latter to select the corner when the eight-second offence applies.

## 4. D-07 — outside-own-area handball (explicit owner decision)

**Present state:** PR #511 separately handles *own-release re-handling*: prohibited second hand contact gives an opponents' indirect free kick **inside** the keeper's area and direct free kick **outside**. That limited outside branch **does not implement general outside-area handball** after an otherwise new claim. The C eight-second check must not accidentally supply that missing sanction.

**Option A — implement and measure separately (recommended).** Author a separate, narrowly scoped general-outside-area handling offence contract, with source-tagged Law 12.1 direct-free-kick outcome, the contact position, immediate claim prevention/cancellation and shared restart ownership; cover the keeper's movement across the line while retaining hand control, the inclusive boundary, both ends, and precedence against PR #511's already-sanctioned re-handling. Land/measure that slice independently from the C timeout amendment to retain clear B→C attribution. D-07 is **closed only once the scope, interaction and tests are recorded**, not merely by adding a generic DFK branch to #511.

**Option B — explicit governed deferral.** File a named open issue/ERR with current reachable outside-area claim scenarios, precise unsupported sanctions, a designated later owner/milestone, prevention of any false eight-second corner, and tests proving the C isolation boundary. Do **not** describe the simulation as completely Law-12-compliant while the general handball offence remains absent.

**Owner decision required:** select A or B before W8 C closeout; if A, approve separate implementation and boundary policy (including the carrying-across-the-line case); if B, approve the identified compliance gap and explicit later closure criterion. This document **does not choose on the owner's behalf**.

## 5. Preregistered C acceptance and non-goals

C code may begin **only after owner approval**, W8 B has landed and its A→B measurement is archived, and the approved C contract includes D-07 disposition. Implementation gate must include, at minimum:

- Frame-480 lawful CONTACT, frame-481 still-controlled corner **before** CONTACT, and frame-481 feet possession without a Law-12 sanction; assert an intent submitted at 480 but not yet struck does **not** stop the clock.
- `HandsOnBall` vs `Distributing` during windup, cancellation/retry without timer reset, no duplicate kick/event/restart, real possession loss/goal/foul/tackle before deadline, and no stale corner after `ResetSlot` or substitution.
- Own-area edge/corner inclusivity, just-outside-area exclusion, both mirrored keeper goals, exactly `y=34` tie, keeper-position corner placement, opponent recipient and ordinary restart cue/taker.
- Canonical save/restore before, at and beyond the deadline, including mid-windup, exact 60 Hz frame and schema compatibility; restored/uninterrupted outcomes and event order identical.
- Six frozen W8 seeds for isolated B→C comparison on the **same pinned host/runtime**, reporting per-seed gameplay-event and ball-trajectory equality when no offence occurs; for changed episodes, report first divergent frame/reason. Snapshot digests are separately reported because C's new serialized field can change them without changing ordinary play. Forced boundary fixtures, not six matches, prove the rare offence.
- Final-head Unity compile and EditMode evidence under the roadmap's host gate before C merges. No new [GT], RNG draw/site/order, selector, kick trajectory, #21 policy or B release schedule is authorized here.

## 6. Approval record / application checklist

**Owner: pending.** Approve or request edits to the proposed FR-GK-028/043 text, frame precision, before-executor ordering, fixed constant, feet-guard separation, restart/tie semantics, and D-07 selection. After approval, apply these amendments **to the actual #11 §2/§3 files** on top of merged PR #511, update the owning §3 version histories and documented ERR/back-props, review cross-spec Match Flow/Pass Mechanics contracts and test names, then schedule the separate C code PR. Merely merging this *proposal* document is **not** an implementation authorization.

**Related records:** [W8 decision packet](w8-goalkeeper-distribution-decision.md), [W8 A preregistration](w8-stage-a-preregistration.md), [Roadmap step 8 / D-07](path-to-playable-roadmap.md), [#11 §2](../specs/goalkeeper-mechanics/section-2.md), [#11 §3](../specs/goalkeeper-mechanics/section-3.md), and [PR #511](https://github.com/antonzymin-eng/Soccer-Manager-Pro/pull/511).
