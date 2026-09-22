# W3 — shared AGENT_BALL fan-out and goalkeeper cross-claim wiring

> **Created:** September 22, 2026
> **Status:** ACTIVE DRAFT / PREREGISTRATION AMENDMENT — implementation is in progress on PR #439; v0.2 records the review-corrected architecture before goalkeeper arbitration lands.
> **Owner document:** `docs/tracking/match-engine-wiring-backlog.md` **W3**.
> **Companion preregistration:** `docs/tracking/foul-card-w3-w9-preregistration.md`.
> **Baseline:** `main` at `876a3343319050187c2a5505b18cb32fc3d0f89d`; post-merge CI run
> `35783692588` green, including the non-certifying functional gate.
> **Purpose:** satisfy the W3 prerequisite before implementation begins: name the shared feed owner,
> exact producer/consumer surfaces, the generic-fan-out/policy boundary, tick ordering, deterministic
> state consequences, required tests/evidence, and implementation size.

---

## 0. Decision summary

W3 and the shared `AGENT_BALL` fan-out remain **one landing**.

The generic feed is owned by the **MatchEngine composition root**, but W3 does **not** move the
physical Collision #3 response pipeline. Instead Collision #3 exposes a read-only
`PublishAgentBallContacts(...)` candidate pass in Physics, after movement and before GK/Heading.
That pass uses the existing Stage-0 AGENT_BALL overlap test and mutates neither the ball nor agents.

The Physics candidate feed fans out deterministically to:

1. Heading #10's existing `ICollisionEventConsumer` surface; and
2. the W3 cross-claim collector used by goalkeeper/heading arbitration.

The existing `MatchFlowCollisionConsumer` remains attached to the full
`CollisionSystem.UpdateCollisions(...)` call in **Resolve**, exactly where foul capture, W4 torso
deflection and next-tick movement feedback already live.

The fan-out is intentionally dumb: it preserves entity-order publication and does not decide whether
a candidate is a header, goalkeeper hand contact, cross claim, foul, or any other gameplay policy.
In particular, Collision #3's coarse Stage-0 cylinder is **candidate discovery only**. Heading #10's
own head geometry remains authoritative for header eligibility, including aerial contacts above
Collision #3's 2.0 m reach cap.

No cross-tick collision-event queue is introduced. W3 candidate buffers are cleared, populated and
consumed inside one 60 Hz Physics phase. Physical collision response retains its pre-W3 Resolve
ordering, so W3 does not introduce the unreviewed "torso bounce before header/save" behavior found in
PR #439 review. The feed itself introduces no snapshot field or schema bump.

No `[GT]` values are calibrated in W3. The frozen six-seed corpus is rerun after the wiring as
required by the companion preregistration.

---

## 1. Source findings that constrain the landing

### 1.1 Collision System has one push-consumer slot

`CollisionSystem.UpdateCollisions` accepts one `ICollisionEventConsumer`. MatchEngine currently
passes its nested match-flow consumer. A second direct caller would fork collision detection, so the
composition root must own fan-out.

The W3 feed contract is:

- the read-only Physics pass emits only `AGENT_BALL` candidates;
- each candidate reaches Heading and the W3 collector exactly once;
- publication is canonical agent/entity order, independent of broad-phase list order;
- the read-only pass does not alter Collision #3 contact-onset state or consume its Resolve event budget;
- full Resolve collision events still reach the existing match-flow consumer exactly once.

### 1.2 The current phase order makes Heading's consumer dead

Current MatchEngine order is:

```
Physics:
    BallPhysics
    AgentMovement
    GK movement
    Heading.Update
    Goalkeeper.Update

Resolve:
    CollisionSystem.UpdateCollisions
    ...
```

Heading's `HeadingDuelResolution.OnCollisionEvent` buffers `AGENT_BALL` events, but
`HeadingMechanics.Update` clears that buffer at the start of the earlier Physics phase. The event is
therefore pushed **after** the only same-frame consumer opportunity and is cleared before the next one.

The source contains a second symptom of the same defect: the buffer is written by
`OnCollisionEvent`, while current production duel-candidate registration is correctly driven by
Heading's own geometry. PR #439's first implementation incorrectly made Collision #3's coarse overlap
a prerequisite for heading duel membership; review showed that would reject high aerials above 2.0 m
and likely suppress contested headers.

W3 therefore keeps Heading geometry authoritative. The shared feed becomes load-bearing at the
**cross-system arbitration boundary**, where it identifies coarse current-frame candidates for
keeper/head classification without replacing #10's own contact test.

### 1.3 Detection may run in Physics; physical response stays in Resolve

Collision #3's geometric AGENT_BALL test is pure. W3 may therefore evaluate that geometry after Ball
Physics and Agent Movement for same-frame candidate discovery without applying Collision response.

The full `UpdateCollisions()` call stays in Resolve. This preserves all reviewed pre-W3 ordering:
agent-agent response and foul capture remain Resolve-owned; collision feedback is consumed by movement
on the next tick; and W4's applied torso deflection resets keeper reaction in the same Resolve phase.
A composed test must continue to lock that W4 behavior.

### 1.4 Goalkeeper #11 cites a producer surface that does not exist

Goalkeeper #11 §3.6.1 says hand/head classification comes from #3
`agent.handCapsule`, `agent.headSphere`, and a standard
`#3.IntersectsBallSphere` helper.

Those surfaces do not exist in current Collision #3 production code. Current `AGENT_BALL`
classification is a generic agent volume and `AgentBallCollisionData.BodyPart` is still hard-coded
to `Torso`; #3's own staged design defers aerial / goalkeeper special cases.

This is a #11 cross-spec citation defect, not permission for W3 to invent an undocumented #3 API.
It is filed and resolved as **`ERR-011-011`** after a repo-wide recheck on September 22. #11 §3.6.1
now treats #3 as candidate discovery only, keeps #10 as head-geometry owner, and keeps #11's live
hand/reach envelope as hand-geometry owner. A goalkeeper without a live hand envelope is not a Hand
participant; W3 must wire the dormant claim producer before ordinary cross claims can use Hand.

### 1.5 Do not project outfielders through the goalkeeper attribute contract

`GoalkeeperCrossClaimDuel.RegisterParticipant` currently accepts
`GoalkeeperAgentAttributes`, while a real duel includes outfield players. The existing
`PlayerAttributeProjection.ToGoalkeeper` contract is goalkeeper-slot-only.

W3 therefore introduces a narrow cross-claim participant value carrying only the score inputs
(`BalanceNorm`, `StrengthNorm`, `AerialNorm`) from canonical player attributes. Outfield players
must not be disguised as goalkeeper records.

---

## 2. Target same-frame pipeline

The W3 landing pins the following 60 Hz order:

```
RunPhysicsPhase
  1. BallPhysics integration
  2. swept goal-frame / woodwork response
  3. AgentMovement for all agents
  4. goalkeeper movement projection
  5. refresh keeper-slot ids
  6. clear W3/Heading current-frame candidate buffers
  7. CollisionSystem.PublishAgentBallContacts   // READ-ONLY candidate discovery
       -> AgentBallFanout
          -> Heading collision consumer
          -> W3 cross-claim collector
  8. build W3 hand/head arbitration from candidate feed + owning mechanic geometry
  9. Heading.Update / Goalkeeper.Update
 10. controlled-ball attachment

RunResolvePhase
  1. publish pending substitutions + refresh keeper-slot ids
  2. decrement foul cooldown
  3. CollisionSystem.UpdateCollisions           // FULL physical response, unchanged owner
       -> MatchFlowCollisionConsumer
  4. W4 applied-deflection reaction reset
  5. ApplyFoulIfCaptured
  6. executors / restart / first touch / possession as before
```

The Physics feed and Resolve response are deliberately separate. The feed is observation-only and may
not mutate contact-onset state, consume the full collision-event budget, or apply Ball Physics
deflection. This avoids both regressions found in the first PR #439 implementation: Collision #3's
coarse body cylinder cannot gate Heading eligibility, and its torso deflection cannot run before a
header/save in the same frame.

---

## 3. Generic feed versus W3 policy

### 3.1 Generic fan-out owns only delivery

The generic layer may inspect `CollisionType` only to avoid sending irrelevant events to the two
`AGENT_BALL` consumers. It must not:

- classify Hand versus Head;
- decide cross/lofted-pass semantics;
- choose duel participants or a winner;
- apply heading quality or goalkeeper handling;
- draw RNG;
- alter the ball.

This keeps #3's event surface generic and avoids making Collision System the owner of football policy.

### 3.2 W3 collector owns contest membership

After the read-only candidate pass has completed, W3 considers a cross-system contest only when at
least two distinct agents are inside #11's contest volume and the owning mechanic confirms a relevant
contact geometry. The coarse Collision #3 candidate is neither Head nor Hand truth by itself.
Registration is canonicalized to #16 entity order; callback arrival order must not decide a winner.

The `CROSS_CLAIM_VOLUME_RADIUS_M` gate remains owned by #11 and is not retuned here.

### 3.3 Body-part classification is consumer-owned Stage-0 geometry

The back-propagated #11 rule will use the physical geometry already implemented by the mechanics,
rather than phantom #3 colliders:

- **Head:** Heading #10's head-contact volume / head-centre geometry is the Stage-0 head classifier.
- **Keeper Hand:** Goalkeeper #11's current hand/reach envelope is the Stage-0 hand classifier.
- **Outfielder Hand:** not a legal W3 route; an outfielder may contribute a Head or non-heading/body
  contact but never a goalkeeper hand claim.
- If a goalkeeper is simultaneously eligible for the head and hand proxies, #11 retains an explicit
  deterministic physical tie rule; it must not route by intent.

This is a specification correction, not a new calibration. Existing geometry constants are reused;
W3 does not widen them to make the corpus produce a preferred rate.

### 3.4 One arbitration, two consumers

For a W3 contest:

- a **Head** winner routes to Heading #10's contested-duel path;
- a **Keeper Hand** winner routes to Goalkeeper #11
  `GoalkeeperCrossClaimDuel` and then the existing handling-quality path;
- the losing goalkeeper receives the existing `DisturbedInDuel` failure semantics where applicable;
- normal single-agent heading and normal shot-save handling remain on their existing paths.

The shared contact membership is the common dependency. Heading and Goalkeeper retain their own
quality formulas and event types.

---

## 4. Determinism, snapshot, and RNG consequences

### 4.1 No new cross-tick hidden state

All new feed buffers are frame-local and consumed before Physics exits. No pending collision event
survives into Snapshot, so no new serialized field is justified.

If implementation discovers that an event must survive the phase boundary, this design is invalid:
stop, amend this note, add the state to canonical snapshot/restore, and bump the schema before
continuing. Silently carrying an unserialized pending contact is forbidden.

### 4.2 Digest trajectory may change only from newly-live W3 gameplay

The read-only candidate feed itself is behavior-neutral: physical collision response ordering is
unchanged and Heading eligibility is unchanged. Once goalkeeper/head arbitration becomes live, that
new gameplay may alter ball state, mechanic RNG consumption, events and subsequent decisions. W3 must
therefore not claim pre/post digest equality for the completed landing.

The determinism locks are instead:

- two same-seed post-W3 runs produce byte-identical digest chains;
- save/restore from a post-W3 snapshot reproduces the uninterrupted chain;
- fan-out arrival order cannot change participant iteration order;
- the same collision event is never duplicated to a consumer.

### 4.3 RNG

W3 uses the already-specified goalkeeper cross-claim tiebreak and Heading duel draw sites only at
their documented conditions. No generic-fan-out RNG is permitted.

A changed draw sequence caused by newly reachable gameplay is an intended behavioural consequence,
not evidence of nondeterminism. Any new draw site requires a separate spec amendment and is out of
scope for this plan.

---

## 5. Required tests before merge

Structural / unit locks:

1. read-only AGENT_BALL publication preserves ball/agent state and emits canonical entity order;
2. the coarse feed does not publish above Collision #3's Stage-0 reach, while a separate Heading
   regression proves a realistic >2.0 m two-player aerial can still form a contested header;
3. fan-out forwards each AGENT_BALL candidate exactly once to Heading and the W3 collector;
4. Physics ordering lock: movement precedes read-only candidate publication; publication precedes
   Heading/GK; full physical collision response remains Resolve-owned;
5. Heading direct callers that omit `BeginPhysicsFrame` self-clear stale contact/duel state;
6. fixed-capacity candidate buffers fail closed; silent truncation is forbidden;
7. two-way W3 contest: keeper-hand winner;
8. two-way W3 contest: head winner routes to Heading and does not emit a goalkeeper claim;
9. 3+ participant registration is in entity order independent of callback order;
10. no outfielder is projected through `ToGoalkeeper`;
11. W4 deflection reaction still resets from a real physical collision in the same Resolve phase;
12. existing foul capture still fires exactly once with physical collision retained in Resolve;
13. same-seed determinism and save/restore equivalence remain green.

Whole-tree acceptance:

- required CI contexts green;
- MatchEngine / Heading / Goalkeeper / Collision suites green apart from any owner-held policy that
  is already explicitly excluded by the gate;
- no new skip used to conceal a W3 failure.

---

## 6. Frozen-corpus evidence

Because W3 and the feed are preregistered baseline invalidators, rerun the **same frozen source-complete
six-seed corpus** after the implementation. Do not calibrate before reading that result.

The result-bearing report must include, per seed and aggregate:

- `AGENT_BALL` multi-agent fan-out events;
- cross-claim eligibility episodes;
- registered duel participants;
- resolved hand-contact duels;
- successful goalkeeper claims;
- cross / lofted-pass attempts and completions;
- header attempts / contacts if the shared feed changes the header contact population;
- the complete foul / yellow / red report already defined by the preregistration.

Any zero must retain enough upstream counters to distinguish “no opportunity” from “broken wire”.

---

## 7. Implementation size and commit shape

Expected implementation surface: roughly **8–12 production/test/spec/tracking files**, dominated by
MatchEngine composition, Heading buffer lifecycle, goalkeeper cross-claim participant/adaptation,
spec back-prop, and composed tests. This is a size estimate, not a calibration allowance.

Recommended atomic sequence on this branch:

1. **plan only** — this file;
2. **spec/back-prop + structural types/tests** — correct the #11 phantom dependency and add the
   narrow cross-claim participant contract;
3. **runtime atom** — read-only Physics candidate feed + fan-out + W3 arbitration, while full physical collision response remains in Resolve;
4. **tests/adversarial corrections**;
5. **frozen-corpus evidence + closeout text**.

The runtime atom must not be split into a state where only one of the two consumers is live; backlog
row 7 is one dependency with two consumers.

---

## 8. Non-goals

W3 does not:

- tune foul/card probabilities;
- settle the separate cooldown-bypass owner decision;
- perform KD-W1 calibration;
- widen heading/head or goalkeeper reach constants to manufacture event rates;
- land W8/W9/W10;
- change Collision #3's generic `AgentBallCollisionData.BodyPart = Torso` Stage-0 response model
  merely to satisfy #11's stale citation.

Those remain subsequent work unless implementation evidence exposes a correctness blocker that must
be fixed atomically.

---

## Version history

| Version | Date | Notes |
|---|---|---|
| 0.3 | 2026-09-22 | `ERR-011-011` filed/resolved atomically with #11 §3.6.1: remove phantom #3 hand/head colliders; #3 is candidate-only, #10 owns head geometry, #11 owns live hand reach, and ordinary Hand claims remain blocked until W3 wires a real claim producer. |
| 0.2 | 2026-09-22 | PR #439 review correction: reject the full Resolve→Physics collision move and the Collision-cylinder Heading gate. W3 now uses a read-only Physics AGENT_BALL candidate pass; full response/W4/fouls stay in Resolve; #10 geometry remains authoritative; direct Heading lifecycle and overflow fail-closed requirements are explicit. |
| 0.1 | 2026-09-22 | Pre-implementation W3 / shared AGENT_BALL landing plan. Records the single-consumer composition constraint, proposed phase ordering, #11 phantom collider citation, generic-feed/policy boundary, determinism/snapshot posture, required tests, and frozen-corpus rerun. |
