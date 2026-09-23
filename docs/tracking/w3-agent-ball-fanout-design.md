# W3 — shared AGENT_BALL fan-out and goalkeeper cross-claim wiring

> **Created:** September 22, 2026
> **Status:** ACTIVE DRAFT / RESULT-BEARING — PR #439 remains draft. v0.7 records the production corpus, W2 gate diagnosis, M7 mutation proof, dormant contested-arbitration finding, and pending owner decisions.
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
2. the W3 frame-local AGENT_BALL observation collector.

The existing `MatchFlowCollisionConsumer` remains attached to the full
`CollisionSystem.UpdateCollisions(...)` call in **Resolve**, exactly where foul capture, W4 torso
deflection and next-tick movement feedback already live.

The fan-out is intentionally dumb: it preserves entity-order publication and does not decide whether
a candidate is a header, goalkeeper hand contact, cross claim, foul, or any other gameplay policy.
In particular, Collision #3's coarse Stage-0 cylinder is **candidate discovery only**. Heading #10's
own head geometry remains authoritative for header eligibility, including aerial contacts above
Collision #3's 2.0 m reach cap.

No cross-tick collision-event queue is introduced. The Collision #3 observation buffers and #10 prepared-Head
scratch are cleared, populated and consumed inside one 60 Hz Physics phase. Physical collision response retains
its pre-W3 Resolve ordering, so W3 does not introduce the unreviewed "torso bounce before header/save" behavior
found in PR #439 review. Separately, W3 makes #11 `ClaimIntent` a live bounded cross-tick episode; that intent plus
its active latch is authoritative gameplay state and is serialized in MatchEngine snapshot schema **v23**.

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

W3 therefore keeps Heading geometry authoritative. The shared Collision #3 feed is **not load-bearing for
contest membership or body-part classification**. It is a required second consumer/diagnostic fan-out proving
the shared dependency is live. The cross-system arbitration boundary instead combines #10's prepared
current-frame Head geometry with #11's live active-claim Hand reach geometry.

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
  8. Heading.Update Pass 1 prepares #10 geometry-qualified Head contacts
  9. W3 composition arbitration combines prepared #10 Heads + live #11 active-claim Hand reach
       -> canonical mixed-participant registration
       -> #11 cross-claim score/tiebreak
       -> suppress losing Heads before #10 ball mutation
       -> route Hand winner/loss through #11 handling/failure path
 10. Heading.Update Pass 2 applies the surviving Head path; Goalkeeper.Update advances #11 state
 11. controlled-ball attachment

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

### 3.2 Mechanic-owned geometry owns contest membership

The read-only Collision #3 collector is diagnostic only and may contain **zero** records for a valid high aerial.
W3 contest membership comes only from the owning mechanics in the same Physics frame:

- #10 contributes agents that have reached its real geometry-qualified prepared Head-contact point;
- #11 contributes goalkeepers with an active bounded `ClaimIntent` whose live hand/reach envelope intersects the ball.

An uncontested Hand contact is a valid one-participant #11 resolution; a mixed contest exists when additional
Hand/Head participants are present. Registration is canonicalized to #16 entity order, independent of callback
or mechanic discovery order. The tactical arming radius remains `CROSS_CLAIM_VOLUME_RADIUS_M`; it is not retuned
and is not substituted for the live contact geometry.

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

### 4.1 Frame-local arbitration scratch; ClaimIntent is serialized cross-tick state

The Collision #3 observation collector, Heading prepared-Head list, suppression mask, and #11 duel buffer are
frame-local and consumed before Physics exits. No pending collision/arbitration event survives into Snapshot.

`ClaimIntent` is different: W3 requires a high cross to arm once and remain locked while the ball descends into
reachable Hand height. Its target, clutch input, locked lateral reach side, commit tick and active latch therefore
survive tactical/physics strides and are authoritative cross-tick gameplay state. MatchEngine schema **22 → 23**
serializes/restores that complete state. Digest and mid-claim save/restore tests lock the requirement.

Any future arbitration scratch that must survive a phase/tick boundary requires the same treatment; silently
carrying an unserialized pending contact remains forbidden.

### 4.2 Digest trajectory may change only from newly-live W3 gameplay

The read-only candidate feed itself is behavior-neutral: physical collision response ordering is unchanged and
Heading eligibility is unchanged. The live ClaimIntent and goalkeeper/head arbitration may alter ball state,
mechanic RNG consumption, events and subsequent decisions. W3 must therefore not claim pre/post digest equality
for the completed landing.

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

**Attribution correction discovered during PR #439 defect localization.** The final PR head also carries
a pre-existing W6 correctness repair: after Collision #3 applies Resolve-time agent-agent penetration
position correction, MatchEngine re-runs the existing Controlled-ball attachment funnel. That call is
unconditional — it applies to keeper and outfield holders and also to the default engine when GK/Heading
wiring is disabled — so it can change match trajectories/digests independently of W3. W3 made the keeper
symptom observable at scale; it did not create the underlying W6 ordering gap.

Therefore the final six-seed report is evidence for the **combined landing state (W3 + the W6 ordering
correction)**. Any changed football population must not be causally attributed to W3 alone. The evidence
write-up must name the W6 correction explicitly and retain `f69aaef0` as the pre-correction W3 head when
describing provenance. Splitting the correctness fix into a separate PR is not required for #439, but
the mixed attribution must remain visible in the final closeout.

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

### 6.1 Result-bearing production evidence

The post-W3 frozen six-seed run is Actions run `35814050060`, measuring exact production SHA
`f40f0853909cc2a42190023fea1da72d25409b12`. The later PR head
`4d31788136b891f37940778d44db2bd28f88d6f6` changes only
`src/season-save/tests/SeasonLoopDisciplineTests.cs`; it does not change production gameplay.

Aggregate post-W3 counters:

| Counter | Six-seed result |
|---|---:|
| `agentBallFanoutEvents` | 135,582 |
| `claimEligibilityEpisodes` | 1,262 |
| registered duel participants | 1,164 |
| resolved Hand-contact events | 1,164 |
| `successfulKeeperClaims` | 753 |
| cross attempts / completions | 157 / 0 |
| lofted attempts / completions | 1,803 / 0 |
| header attempts / contacts | 1,809 / 0 |
| fouls | 43 total = 7.17 / 90 |
| cautions | 5 |
| dismissals | 1 |

The **1,164 resolved Hand-contact events were not contested Hand-vs-Head duels**. Registered
participants equal resolved Hand contacts because each recorded production resolution contained only
the goalkeeper participant. The six-match production corpus therefore reached **zero contested
Hand-vs-Head arbitrations**. The contested arbitration path is proven by the composed tests, not by
production-play reachability.

The production feed and keeper-claim path are nevertheless live: the fan-out, claim-eligibility and
successful-claim counters are positive. That establishes execution of those paths; it does **not**
establish production reachability of the contested branch.

The matched pre-W3 arm is branch `evidence/pr439-w3-prewire-six-seed`, Actions run
`35815761065`, measuring exact baseline `876a3343319050187c2a5505b18cb32fc3d0f89d` with the
same measurement transform. `headerContacts=0` in both pre-W3 and post-W3 arms, so the zero
predates W3. A separate open issue, **#441**, must distinguish **no production header-contact opportunity** from
**broken header-contact wiring** using upstream counters sufficient to locate the first zero.

`successfulKeeperClaims=753` is a W3-only counter and has no valid pre-W3 comparator. Keepers could
claim balls before W3, so this corpus **cannot assess a before/after keeper-claim rate** unless a common
metric (for example keeper possession gains) is added to both arms.

The foul movement **50 → 43** and slide-tackle movement **8 → 4** are characterization only. The PR
also carries the W6 Resolve-time Controlled-ball reattachment correction described above, so those
population changes are not attributed to W3.

### 6.2 Backlog classification remains an owner decision

The evidence supports this factual statement and no stronger completion claim:

> Contest plumbing is wired and test-proven, but the measured production corpus never reaches a
> contested Hand-vs-Head arbitration.

Whether W3 is classified as **complete/wired**, **wired but dormant**, or **still open pending
production contested-arbitration reachability** remains an explicit owner decision. PR #439 must not
silently choose among those classifications.

### 6.3 W2 diagnostic evidence blocking the functional gate

Three evidence arms use the same diagnostic test blob
`e81e859db5bfa2b4c7a6fd09bee2dab6775b8a11` and workflow blob
`dec67b35de829d79f9cc6169b5d9139b4ed2d85a`, applied to three production parents:

| arm | production parent | evidence head / run / job | W/L/F/M | dispossessions | BallLoose end state | `P(0 wins)` |
|---|---|---|---:|---:|---|---:|
| base | `876a3343319050187c2a5505b18cb32fc3d0f89d` | `1ba99b8e…` / `35878618102` / `107240961327` | 3 / 6 / 0 / 20 | 3 | 6 original-carrier / 0 other / 0 loose | 19.586% |
| W3-only | `f69aaef0f27fdd8a10a89a2d4cc599b08d6ef70b` | `754c7ab9…` / `35878656931` / `107241091685` | 2 / 2 / 1 / 36 | 2 | 2 original-carrier / 0 other / 0 loose | 5.816% |
| current-production behavior | `4d31788136b891f37940778d44db2bd28f88d6f6` | `5c75a6cf…` / `35878699989` / `107241232506` | 0 / 6 / 0 / 27 | 0 | 6 original-carrier / 0 other / 0 loose | 13.573% |

The base job's test conclusion was red only because the already-filed ShotExecutor FM-03 Error was
reported at teardown after the diagnostic output completed. An identical evidence-only containment
rerun, run `35879276984` / job `107243207618`, reproduced the base summary exactly and passed.
No production behavior was changed by the diagnostic.

The clean-win branch is **not statistically implausible** under the preregistered threshold:
head `P(0 wins)=13.573%`, well above 1%. The head `p_i` distribution is also ordinary rather than
collapsed (min 1.073%, median 4.177%, max 15.506%, mean 5.793%). Base and W3-only likewise have
ordinary distributions and `P(0 wins)` of 19.586% and 5.816%. The observed 0 wins at head is
therefore plausible sampling variation; changing seeds, tick count, or the positive check requires
an owner decision.

The BallLoose branch is independent and material: **14/14 BallLoose outcomes across all three arms
end the same tick back on the original carrier**, with zero other-player pickups and zero balls left
loose. That latent W2/W6 gameplay issue is filed separately as **#440**. It must not be repaired
inside PR #439; any gameplay fix requires separate before/after measurement, KD-W1 review, explicit
owner approval, and its own PR.

### 6.4 M7 SeasonSave mutation proof

The deterministic ban-order rewrite has direct mutation proof:

- evidence branch: `evidence/pr439-m7-order-mutation`;
- unmutated production/test parent: `4d31788136b891f37940778d44db2bd28f88d6f6`;
- mutation commit: `42ea88a79a9f0dad5071cefafda24f5a2ff35656`;
- evidence-workflow head: `58cf6ec88e33054c069ef8e2914ce395fd2d76f1`;
- Actions run/job: `35878836477` / `107241699913`;
- mutation: reverse `SeasonLoop` fixture discipline order so `CommitFixtureCards` runs before
  `OnClubFixturePlayed`;
- exact target `ANewBanEarnedThisFixtureIsNotServedByThisSameFixture` reports **Failed** under the
  mutation, and the evidence job passes only when that exact target fails.

The mutation is evidence-only and is not present on PR #439. The normal branch retains the
serve-before-commit production order.

---

## 7. Implementation size and commit shape

Expected implementation surface: roughly **8–12 production/test/spec/tracking files**, dominated by
MatchEngine composition, Heading buffer lifecycle, goalkeeper cross-claim participant/adaptation,
spec back-prop, and composed tests. This is a size estimate, not a calibration allowance.

Recommended atomic sequence on this branch:

1. **plan only** — this file;
2. **spec/back-prop + structural types/tests** — correct the #11 phantom dependency, record the serialized
   ClaimIntent episode, and add the narrow cross-claim participant contract;
3. **runtime atom** — read-only Physics observation feed + fan-out plus #10-prepared/#11-live geometry arbitration,
   while full physical Collision #3 response remains in Resolve;
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
be fixed atomically. PR #439 did expose one such blocker: the W6 Resolve-time Controlled attachment
ordering gap described in §6. That correction is intentionally retained, but it is not reclassified as
a W3 mechanism and its default-engine trajectory effect must stay explicit in evidence.

---

## Version history

| Version | Date | Notes |
|---|---|---|
| 0.7 | 2026-09-23 | Adds the three-arm W2 gate diagnosis (identical test/workflow blobs, head P(0 wins)=13.573% > 1%, 14/14 BallLoose same-tick original-carrier re-pickups, issue #440), issue #441 for the header-contact zero, and M7 mutation proof run 35878836477/job 107241699913. No gameplay change. |
| 0.6 | 2026-09-23 | Result-bearing six-seed evidence: records run 35814050060 at production SHA `f40f085…`, 1,164 single-participant Hand-contact resolutions and zero contested Hand-vs-Head production duels; corrects `successfulKeeperClaims` to a non-comparable W3-only counter; records headerContacts=0 pre/post, attribution limits, and the pending owner classification decision. |
| 0.5 | 2026-09-22 | Defect-localization attribution correction: PR #439 also carries an unconditional W6 Resolve-time Controlled reattachment repair that affects keeper/outfield holders and can change default-engine trajectories with GK/Heading disabled. Final frozen-corpus evidence is therefore the combined W3+W6 landing state; `f69aaef0` is retained as the pre-correction W3 provenance point and deltas must not be attributed to W3 alone. |
| 0.4 | 2026-09-22 | Review correction / ERR-011-012: Collision #3 fan-out is observation-only, never W3 membership; #10 prepared Head geometry + #11 active-claim Hand reach form the live contest before ball mutation. ClaimIntent is now a bounded locked episode and its full payload + active latch is serialized in MatchEngine schema v23. |
| 0.3 | 2026-09-22 | `ERR-011-011` filed/resolved atomically with #11 §3.6.1: remove phantom #3 hand/head colliders; #3 is candidate-only, #10 owns head geometry, #11 owns live hand reach, and ordinary Hand claims remain blocked until W3 wires a real claim producer. |
| 0.2 | 2026-09-22 | PR #439 review correction: reject the full Resolve→Physics collision move and the Collision-cylinder Heading gate. W3 now uses a read-only Physics AGENT_BALL candidate pass; full response/W4/fouls stay in Resolve; #10 geometry remains authoritative; direct Heading lifecycle and overflow fail-closed requirements are explicit. |
| 0.1 | 2026-09-22 | Pre-implementation W3 / shared AGENT_BALL landing plan. Records the single-consumer composition constraint, proposed phase ordering, #11 phantom collider citation, generic-feed/policy boundary, determinism/snapshot posture, required tests, and frozen-corpus rerun. |
