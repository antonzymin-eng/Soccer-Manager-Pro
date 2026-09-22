# W3 — shared AGENT_BALL fan-out and goalkeeper cross-claim wiring

> **Created:** September 22, 2026
> **Status:** PRE-IMPLEMENTATION DESIGN / PREREGISTRATION — no runtime change has landed from this note yet.
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

The generic feed is owned by the **MatchEngine composition root**. The producer remains Collision
System #3's existing `CollisionSystem.UpdateCollisions(..., ICollisionEventConsumer, ...)` push
surface. The single consumer slot becomes a deterministic fan-out at the root:

1. the existing match-flow collision consumer (fouls / collision bookkeeping);
2. Heading #10's existing `ICollisionEventConsumer` surface; and
3. a W3 cross-claim collector used by the goalkeeper/heading arbitration policy.

The fan-out is intentionally dumb: it preserves producer order and does not decide whether a contact
is a header, goalkeeper hand contact, cross claim, foul, or any other gameplay policy. Those decisions
remain in the consuming mechanics.

**The collision sweep moves from MatchEngine Resolve to Physics, after Ball Physics + Agent Movement
and before GK/Heading mechanics.** This is not an arbitrary reorder: Collision #3 already requires
`UpdateCollisions()` to run once per frame **after Agent Movement and Ball Physics**, while the
current MatchEngine Resolve placement makes Heading #10's published same-frame collision-consumer
contract impossible to satisfy.

No cross-tick collision-event queue is introduced. All W3/fan-out buffers are cleared, populated,
consumed, and discarded inside one 60 Hz Physics phase. Therefore the feed itself introduces no new
snapshot field and no snapshot-schema bump. Digest trajectories may change because runtime ordering
and gameplay change; save/restore equivalence must still hold.

No `[GT]` values are calibrated in W3. The frozen six-seed corpus is rerun after the wiring as
required by the companion preregistration.

---

## 1. Source findings that constrain the landing

### 1.1 Collision System has one push-consumer slot

`CollisionSystem.UpdateCollisions` accepts one `ICollisionEventConsumer`. MatchEngine currently
passes its nested match-flow consumer. A second direct caller would fork collision detection, so the
composition root must own fan-out.

The fan-out contract is:

- every collision event reaches the existing match-flow consumer exactly once;
- every `AGENT_BALL` event reaches Heading and the W3 collector exactly once;
- non-`AGENT_BALL` events do not enter the heading/W3 branches;
- delivery order is the producer's existing deterministic detection order.

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
`OnCollisionEvent`, but current production duel-candidate registration is driven independently by
Heading's own geometry. The collision buffer is not part of the live decision.

W3 must make the consumer non-vacuous; merely broadcasting into the current buffer is not acceptance.

### 1.3 Collision #3's own scheduling contract supports Physics placement

Collision #3 documents `UpdateCollisions()` as called once per frame after Agent Movement and Ball
Physics with all agent positions finalized. Running the collision sweep after movement and before
GK/Heading mechanics satisfies that producer contract and gives its same-frame consumers a real
window.

The existing movement collision-feedback arrays still affect the **next** movement update because all
agent movement for the current frame has already completed before collision response. That part of the
old C2 contract remains one-frame delayed.

W4's `ballDeflected` feedback moves with the collision sweep and is applied before GK/Heading for
the same Physics frame; tests must lock that ordering explicitly.

### 1.4 Goalkeeper #11 cites a producer surface that does not exist

Goalkeeper #11 §3.6.1 says hand/head classification comes from #3
`agent.handCapsule`, `agent.headSphere`, and a standard
`#3.IntersectsBallSphere` helper.

Those surfaces do not exist in current Collision #3 production code. Current `AGENT_BALL`
classification is a generic agent volume and `AgentBallCollisionData.BodyPart` is still hard-coded
to `Torso`; #3's own staged design defers aerial / goalkeeper special cases.

This is a #11 cross-spec citation defect, not permission for W3 to invent an undocumented #3 API.
Before the runtime policy lands, #11 must be back-propagated to the actual Stage-0 physical geometry
available to the consumers. The next #11 error id was grep-free at plan time
(`ERR-011-011`), but it must be rechecked immediately before filing; the design note does not reserve
an id.

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
  5. clear current-frame collision-consumer buffers
  6. CollisionSystem.UpdateCollisions
       -> MatchEngine collision fan-out
          -> existing match-flow consumer
          -> Heading collision consumer (AGENT_BALL only)
          -> W3 cross-claim collector (AGENT_BALL only)
  7. apply same-frame ball-deflection reaction feedback (W4)
  8. build/resolve W3 cross-claim arbitration from the completed AGENT_BALL set
  9. Heading.Update / Goalkeeper.Update consume the arbitration result
 10. controlled-ball attachment
```

`RunResolvePhase` no longer owns collision detection/response. It still applies any foul candidate
captured by the match-flow consumer and retains the existing executor / restart / first-touch /
possession ordering unless a test proves an explicit W3 dependency requires otherwise.

The move is atomic with the fan-out. There must not be an intermediate commit whose runtime has
collision removed from Resolve but not yet present in Physics.

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

After the collision sweep has completed, W3 considers the current-frame set only when at least two
distinct agents are in the relevant contest around the same ball contact frame. Registration is
canonicalized to #16 entity order; collision arrival order must not decide a winner.

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

### 4.2 Digest trajectory is expected to change

Relocating collision response before GK/Heading and making W3 live can alter ball state, mechanic RNG
consumption, events, and subsequent decisions. W3 must not claim pre/post digest equality.

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

1. fan-out forwards an `AGENT_AGENT` event exactly once to match-flow and zero times to
   Heading/W3;
2. fan-out forwards an `AGENT_BALL` event exactly once to all three destinations, preserving order;
3. Physics ordering lock: movement precedes Collision; Collision precedes Heading/GK;
4. Heading's collision buffer is observably read before it is cleared — a pushed event must change a
   composed contested-duel observation, not only a private count;
5. two-way W3 contest: keeper-hand winner;
6. two-way W3 contest: head winner routes to Heading and does not emit a goalkeeper claim;
7. 3+ participant registration is in entity order independent of callback order;
8. overflow/fixed-capacity paths fail closed or expose a deterministic diagnostic; silent truncation
   is forbidden unless already specified by the owning subsystem;
9. no outfielder is projected through `ToGoalkeeper`;
10. W4 deflection reaction still resets from a real collision in the same Physics frame;
11. existing foul capture still applies once after moving collision production out of Resolve;
12. same-seed determinism and save/restore equivalence remain green.

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
3. **runtime atom** — Physics relocation + fan-out + W3/Heading consumption together;
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
| 0.1 | 2026-09-22 | Pre-implementation W3 / shared AGENT_BALL landing plan. Records the single-consumer composition constraint, Resolve→Physics phase correction, non-vacuous Heading requirement, #11 phantom collider citation, generic-feed/policy boundary, determinism/snapshot posture, required tests, and frozen-corpus rerun. |
