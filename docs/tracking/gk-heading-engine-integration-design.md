# GK / Heading Engine-Integration Design Supplement

> **Status:** DESIGN SUPPLEMENT (pre-implementation) — same governance class as
> `match-engine-design.md` / `snapshot-deserialize-design.md`. NOT a numbered spec.
> **Created:** 2026-07-22
> **Author:** —
> **Governs:** wiring Goalkeeper Mechanics (#11) and Heading Mechanics (#10) into the
> `MatchEngine` tick pipeline, and — only once they have a real consumer there — adding the
> `ToGoalkeeper` / `ToHeading` projections deferred under KD-P8 of
> `player-attribute-projection-design.md`.
> **Scope tier:** *Bounded triggers* (see §1.2).

---

## 0. Why this document exists

`player-attribute-projection-design.md` §3.6/§3.7 specify a `ToHeading` and a `ToGoalkeeper`
projection (canonical `PlayerAttributes` → `HeadingAgentAttributes` / `GoalkeeperAgentAttributes`).
KD-P8 **deliberately did not implement them** at T1/T2 because `MatchEngine` builds neither struct —
adding the projections with no caller would be a phantom consumer, the exact class ERR-001 / ERR-004
and the project's Interface Design Principle forbid. Verified still true (2026-07-22): `MatchEngine.cs`
constructs neither `HeadingMechanics` nor `GoalkeeperMechanics`, and neither
`HeadingAgentAttributes` nor `GoalkeeperAgentAttributes` is referenced anywhere in `src/match-engine/`.

The user directive is: **wire #10/#11 into the engine, then add the projections** — i.e. give the
projections a genuine, exercised consumer rather than a dead one. This document designs that wiring,
then the projections ride on top of it.

Both orchestrators are already implemented, adversarially reviewed, and sealed
(`src/heading-mechanics/HeadingMechanics.cs` v1.5, `src/goalkeeper-mechanics/GoalkeeperMechanics.cs`
v1.6). Nothing about their internals changes here — this is purely a composition-root integration:
construct them, feed them the boundary interfaces they were written against, drive their tick entry
points from the correct engine phases, fire Stage-0 heuristic triggers that commit intents seeded
from the two projections, and make the resulting cross-tick state restore-deterministic.

---

## 1. Scope

### 1.1 In scope

1. Construct `HeadingMechanics` + `GoalkeeperMechanics` at `MatchEngine` boot, injected with
   match-engine-implemented adapters for their four boundary interfaces
   (`IHeadingBallSystem`, `IHeadingRngService`, `IGoalkeeperBallSystem`, `IGoalkeeperRngService`).
2. Register two deterministic RNG streams (one per subsystem) and adapt the per-draw RNG interfaces
   onto them (the `match-flow.card-severity` pattern; §3.3).
3. Drive both orchestrators from the engine's 7-phase pipeline at the correct cadence
   (GK: 10 Hz `TacticalTick` + 60 Hz `Update`; Heading: 60 Hz `Update`; §3.4).
4. Drive Heading's contact resolution through `CommitIntent` + `Update` (§3.4). The
   `CollisionConsumer` (AGENT_BALL duel feed) is deliberately **not** wired this landing — see KD-5.
5. Fire tightly-scoped Stage-0 heuristic triggers that call `CommitSaveIntent` /
   `CommitIntent` seeded from the projections (§4) — this is the only surface that makes the
   projections non-phantom.
6. Add `ToGoalkeeper` / `ToHeading` to `PlayerAttributeProjection.cs`, gated so `ToGoalkeeper` is
   built only for the goalkeeper slot (§5).
7. **(Phase 2, deferred — §1.2a)** Serialize the new cross-tick state at
   `SNAPSHOT_SCHEMA_VERSION` 17 → 18 with restore. Phase 1 fails loud on snapshot when the flag is on
   instead (§6).
8. A dedicated flag-on test proving the projection reaches the orchestrator + forward determinism (§7).

### 1.2 Scope tier — *bounded triggers*

This landing wires, drives, and serializes both orchestrators, and seeds them from the projections
through **conservative, tightly-scoped Stage-0 heuristic triggers**. It deliberately does NOT attempt
a full football-accurate GK/heading model driven by the Decision Tree — the DT produces neither
`SaveIntent`/`RushIntent`/`DistributeIntent` nor `HeaderIntent` today, exactly as it produced no foul
candidates before match-flow completion added the `MatchFlowCollisionConsumer` heuristic. The trigger
heuristics here are the GK/heading analogue of that precedent: enough to fire the intents live and
prove the consumer, conservative enough not to destabilize the existing capstone.

### 1.2a Phasing decision (AR-3) — opt-in wiring first

Implementation-time review of the serialization surface (§6) changed the landing shape. The two
orchestrators carry a large in-flight cross-tick state surface (Heading: 5 per-agent arrays × 22;
Goalkeeper: ~22 per-GK arrays × 2, several of struct type). Making an *always-on* engine
restore-deterministic requires byte-exact `CaptureState`/`RestoreState` on both sealed orchestrators
plus a `SNAPSHOT_SCHEMA_VERSION` 17 → 18 body change — which also forces a digest **rebaseline across
the entire existing snapshot suite**. That is a large, high-risk epic disproportionate to the immediate
goal (give the projections a live consumer).

**Decision (KD-11):** land the wiring **opt-in, default-off**, in two phases.

- **Phase 1 (this landing).** `MatchEngine` constructs both orchestrators + their adapters at boot and
  registers the two RNG streams, but drives them and fires the §4 triggers **only when a new
  `EnableGkHeading()` flag is set** (default off). With the flag off the engine is **byte-identical to
  pre-wiring** — no schema change, no digest rebaseline, the whole existing tree (capstone, restore
  tests, snapshot-schema pins) stays green. The projections are proven a **live consumer** in a
  dedicated flag-on scenario (forward two-run determinism + a `TestOnly_` assertion that the
  projection's attributes reached the orchestrator). An ON engine is deterministic *forward* but not yet
  snapshot-safe, so `SerializeWorldState` / the durable-capture seams **fail loud** (`NotSupportedException`)
  when the flag is on — an honest, explicit boundary, not a silent hole.
- **Phase 2 (deferred).** Serialize the two RNG cursors + both orchestrators' in-flight state via
  `CaptureState`/`RestoreState` seams (§6), flip the default to on, and rebaseline the digest. Tracked as
  an OPEN ISSUE.

This mirrors how the project lands large features (behaviour-neutral T0 → activation), keeps every
existing test green, and still makes the projections non-phantom now.

### 1.3 Out of scope

- A DT-driven GK/heading decision layer (a `SaveIntent` / `HeaderIntent` producer). Future work.
- The per-spec attribute *table* changes that would close `ERR-007` beyond what #10/#11 already
  consume. `ToGoalkeeper`/`ToHeading` project only the fields those two structs already declare.
- Any change to the #10/#11 orchestrator internals, their constants, or their events.
- On-disk save-format / transfers / aging (Stage 1+).

### 1.4 Behaviour-neutrality

**Phase 1 (this landing) IS behaviour-neutral by default** (KD-11 / §1.2a): the wiring is opt-in, so a
default engine never constructs a GK save or header and is byte-identical to pre-wiring — no digest
rebaseline. When the flag is **on**, the engine is *not* behaviour-neutral: live GK saves and headers
change ball trajectory → goal detection → digest, and that divergence is expected and correct (the whole
point). A flag-on engine must be **deterministic forward** (proven, §7); it is *not* yet snapshot-safe
(fails loud, §6). **Phase 2** flips the default to on and takes the digest rebaseline.

---

## 2. Existing seams (verified against source, 2026-07-22)

**Adapter pattern.** `MatchEngine` already bridges subsystem boundary interfaces to `this` via private
nested adapter classes: `PassWorldAdapter` (`IPassBallSystem`, line ~4958), `ShotWorldAdapter`
(`IShotBallSystem`, ~4986), `MatchFlowCollisionConsumer` (`ICollisionEventConsumer`, ~5022). The four
GK/heading boundary interfaces get the same treatment.

**RNG.** `DeterministicRngService.RegisterStream(string siteId, int subsystemOrdinal, int entityId,
ushort streamVersion) → int streamIndex`. Draw pattern (from `ApplyFoulIfCaptured`):
`Reserve(idx, 1)` → `DrawReserved(idx, 0, out ulong draw)` → `CloseReservation(idx)`, each returning
`int` (`0` = OK, non-zero = fail-loud). `_cardSeverityStreamIndex` is registered at Boot
(`SubsystemOrdinals.EventSystem`, `entityId: -1`, `streamVersion: 1`) and its cursor is serialized at
v17 (`GetStreamState`/`RestoreStream`, `RngStreamState`).

**Subsystem ordinals.** `SubsystemOrdinals` (deterministic-sim) already allocates
`GoalkeeperMechanics = 7` in the Physics band (0–19) and a Mechanics band; heading is a Physics-tier
spec. Both orchestrators' constant catalogues already mirror their domain tags
(`GoalkeeperConstants.DomainTagGoalkeeper = 0x1D`, `HeadingMechanicsConstants` mirrors
`DOMAIN_TAG_HEADING = 0x16`). We reuse the existing ordinals; no new ordinal/domain-tag allocation.

**Orchestrator public surfaces.**
- `HeadingMechanics(IHeadingBallSystem, IHeadingRngService)`; `ICollisionEventConsumer CollisionConsumer { get; }`;
  `CommitIntent(int agentId, HeaderIntent, HeadingAgentAttributes, BallState currentBall, int currentFrame)`;
  `CancelIntent(int agentId)`; `Update(AgentState[] agentStates, BallState currentBall, int currentFrame, float currentMatchTime)`.
- `GoalkeeperMechanics(IGoalkeeperBallSystem, IGoalkeeperRngService)`; `UpdateBaselineSlot(int gkIndex, Vector2)`;
  `OnShotExecutedEvent(int gkIndex, float shotMatchTimeMs, float ballSpeedMps)`;
  `CommitSaveIntent(int gkIndex, SaveIntent, GoalkeeperAgentAttributes)` **[projection consumer]**;
  `CommitRushIntent(int gkIndex, RushIntent, GoalkeeperAgentAttributes)` **[projection consumer]**;
  `CommitDistributeIntent(int gkIndex, DistributeIntent)`;
  `TacticalTick(int currentTick, AgentState[], BallState, int[] gkAgentIds)` (10 Hz);
  `Update(int currentFrame, float currentMatchTimeMs, AgentState[], BallState, int[] gkAgentIds)` (60 Hz).
  Per-GK arrays indexed `[0, GoalkeeperConstants.MaxGkAgents)`; `gkAgentIds[gkIndex]` → agentId.

**RNG interface shapes.** Heading: `NextFloat(int drawSiteId)` / `NextGaussian(int drawSiteId)`
(no domain tag). Goalkeeper: `NextFloat(int drawSiteId, uint domainTag)` /
`NextGaussian(int drawSiteId, uint domainTag)`. The Stage-0 stub (`HeadingRngServiceStub`) **ignores
the draw-site id** ("draw-site registry wiring is a Stage 1 deliverable per #16 §4.5") and uses
SplitMix64 + Box-Muller. Our adapters follow the same Stage-0 posture (§3.3).

---

## 3. Construction, adapters, cadence

### 3.1 Boot construction

At Boot, after the collision system and executors are constructed (so `_ball`, `_agentStates`,
`_possessingAgentId` exist), construct:

```
_headingRng   = new HeadingRngWorldAdapter(this);      // IHeadingRngService
_goalkeeperRng = new GoalkeeperRngWorldAdapter(this);  // IGoalkeeperRngService
_headingBall   = new HeadingBallWorldAdapter(this);    // IHeadingBallSystem
_goalkeeperBall = new GoalkeeperBallWorldAdapter(this);// IGoalkeeperBallSystem

_heading    = new HeadingMechanics(_headingBall, _headingRng);
_goalkeeper = new GoalkeeperMechanics(_goalkeeperBall, _goalkeeperRng);

_headingStreamIndex = _rng.RegisterStream(
    "heading.mechanics", SubsystemOrdinals.HeadingMechanics, entityId: -1, streamVersion: 1);
_goalkeeperStreamIndex = _rng.RegisterStream(
    "goalkeeper.mechanics", SubsystemOrdinals.GoalkeeperMechanics, entityId: -1, streamVersion: 1);
```

`SubsystemOrdinals.HeadingMechanics` already exists (Physics band). The GK ordinal likewise. **KD-1**:
one engine-level stream per subsystem, `entityId: -1`, mirroring the `match-flow.card-severity`
precedent exactly — NOT one stream per draw site or per agent. Per-draw-site / per-entity stream
registration is a #16 §4.5 Stage-1 deliverable the two RNG stubs already defer; registering it now
(with the Stage-0 orchestrators that draw only a handful of sites) would be phantom precision.

The two `gkAgentIds` for `TacticalTick`/`Update` come from a fixed `int[MaxGkAgents]` computed once
at boot from the two GK slots (`_isGoalkeeper[i]` per team), stored as `_gkAgentIds`.

### 3.2 Ball adapters

`HeadingBallWorldAdapter` / `GoalkeeperBallWorldAdapter` bridge to `this`:

- `GetBallState(matchTime)` → returns `_ball` (the live `BallState`; matchTime advisory — the engine
  ball is always current within a tick).
- `ApplyKick(velocity, spin, agentId, matchTime)` → `_ball.ApplyKick(...)` via the same seam
  `PassWorldAdapter`/`ShotWorldAdapter` use (routes through `BallCollision.ApplyKick`; non-finite
  velocity rejected there, `KickResult`).
- (GK only) `SetPossessor(agentId)` → sets `_possessingAgentId = agentId` (the caught-save path).
- (GK only) `GetBallPossessorId()` → returns `_possessingAgentId`.

**KD-2**: the adapters hold only a back-reference to `this`; they carry no state, exactly like the
existing three. All ball mutation flows through the one `ApplyKick` seam, so the existing NaN gate and
possession bookkeeping apply unchanged.

### 3.3 RNG adapters

Each adapter draws from its subsystem's single registered stream, converting the `ulong` draw to the
shapes the interface promises — the conversions the `HeadingRngServiceStub` already uses:

```
float NextFloat(...):
    Require(_rng.Reserve(idx, 1) == 0)          // fail-loud: reservation already open
    Require(_rng.DrawReserved(idx, 0, out ulong d) == 0)
    _rng.CloseReservation(idx)
    return (float)((d >> 40) * (1.0 / (1UL << 24)))   // [0,1), 24-bit mantissa — stub's formula

float NextGaussian(...):
    u1 = NextFloat(...); u2 = NextFloat(...)
    if (u1 < GUARD_EPSILON) u1 = GUARD_EPSILON
    return sqrt(-2 ln u1) * cos(2π u2)          // Box-Muller — stub's formula
```

**KD-3**: the `drawSiteId` and `domainTag` parameters are **accepted and ignored for stream
selection**, precisely as `HeadingRngServiceStub.NextFloat(int drawSiteId)` ignores its id today (the
draw-site registry is #16 §4.5 Stage-1 work). Determinism comes from the single ordered stream +
fixed draw order, identical to `match-flow.card-severity`. The `Reserve`/`DrawReserved`/
`CloseReservation` triple is atomic within each `NextFloat` (no yield), so the stream is always at a
closed reservation at rest — the property that lets §6 serialize only the cursor.

**KD-4**: the two subsystem streams are **separate** so a heading draw never perturbs the GK cursor
(and vice-versa), and both are separate from `match-flow.card-severity`. Registration order is fixed
at boot, so stream indices are stable across runs.

### 3.4 Cadence wiring

- **GK `TacticalTick`** — 10 Hz, on the AI stride, from `RunAiPhase` (where `RunMechanicsAI` already
  runs). Passes `CurrentTick`, `_agentStates`, `_ball`, `_gkAgentIds`.
- **GK `Update`** — 60 Hz, from `RunPhysicsPhase`, after the executor/collision advance so it sees the
  current ball, BEFORE `CheckRestartAndApply` reads the ball for goal detection (a save must deflect
  the ball before goal detection runs — §4.1). Passes `CurrentFrame`, `CurrentMatchTimeMs`,
  `_agentStates`, `_ball`, `_gkAgentIds`.
- **Heading `Update`** — 60 Hz, from `RunPhysicsPhase`, same placement as GK `Update`.
- **Heading `CollisionConsumer`** — **not wired this landing** (KD-5). `_heading.CollisionConsumer`
  (`_duelResolution`) is the AGENT_BALL feed that lets contested headers disturb each other. KD-7 fires
  at most one header per airborne episode (the single nearest eligible agent), so headers are
  effectively uncontested at Stage 0 and the duel feed changes nothing observable. Leaving it unwired
  keeps `MatchFlowCollisionConsumer` and the whole collision path entirely untouched (a strictly smaller,
  lower-risk surface); the fan-out is a clean follow-up when a contested-header model exists.
- `GoalkeeperMechanics.OnShotExecutedEvent(gkIndex, shotMatchTimeMs, ballSpeedMps)` is called when the
  shot executor's just-fired CONTACT is detected in `RunResolvePhase` (the executor's state transition
  at line ~2767, before the goal check at ~2775), for the GK of the team being shot at, so the keeper's
  reaction pipeline sees the shot. Detection is the executor's CONTACT-this-tick transition, not a
  pre-existing published event.

**Both orchestrators are frozen while `_matchEnded`** (the existing full-time freeze), and their
per-tick calls are skipped for a sent-off keeper (the `_isSentOff` participation gate the AR-2/AR-3
cycle established for every participation surface).

---

## 4. Stage-0 heuristic triggers (the projection consumers)

The projections are consumed ONLY through `CommitSaveIntent` / `CommitRushIntent` (GK) and
`CommitIntent` (heading). Since no DT producer exists, the engine fires them from conservative
world-state heuristics, seeded from the projections built for the acting agent.

### 4.1 GK save trigger

In `RunResolvePhase`, on the tick the shot executor's CONTACT fires (its state transition, detected
right after `_shotExecutors[i].Update` at ~2767, before `CheckRestartAndApply` at ~2775), if the ball's
resulting trajectory is on target for a goal (reuse the same on-target geometry `CheckRestartAndApply`
uses for goal classification) and the defending team's keeper is not already committed to a save this
possession:

```
gkIndex = index of the defending keeper in _gkAgentIds
attrs   = PlayerAttributeProjection.ToGoalkeeper(_canonicalAttrs[keeperSlot], teamId)
intent  = a SaveIntent toward the ball's on-target crossing point
          (TargetHand from lateral side, ClutchFirmness from a fixed Stage-0 constant,
           AttemptCommittedTick = CurrentTick)
_goalkeeper.CommitSaveIntent(gkIndex, intent, attrs)
```

The orchestrator's own `Update` (§3.4) then carries the dive across the ball's **flight ticks**: the
shot is kicked at tick T (Resolve) but crosses the goal line only ticks later (physics integration), and
on every one of those ticks the GK `Update` runs in `RunPhysicsPhase` *before* `CheckRestartAndApply`
runs in `RunResolvePhase`. So on the tick the ball would cross, that same tick's GK `Update` gets the
last chance to deflect/catch via the ball adapter before goal detection reads the ball — a saved
on-target shot is not counted a goal. (The keeper cannot react on tick T itself — Physics precedes the
Resolve-phase kick — but it has the full flight to react, which is realistic.)

**KD-6**: the trigger fires **at most one save intent per shot** (guarded by an
`_saveCommittedForShot` latch cleared when the ball next settles / possession changes), mirroring
`MatchFlowCollisionConsumer`'s "at most one foul candidate per tick" discipline. The rush and
distribute intents are NOT triggered in this landing (rush needs a through-ball model, distribution
needs a DT receiver) — `CommitRushIntent`'s projection consumption is exercised only by the §7 unit
scenario, not the live capstone, so `ToGoalkeeper` still has a live path (the save) plus a covered
path (the rush test).

### 4.2 Header trigger

In `RunPhysicsPhase`, before `_heading.Update`, for the single nearest active outfield agent whose head
is within head-contact range of an airborne ball above control height and moving toward them, and who
does not already have a committed header this episode:

*(Head-contact range: reuse #10's own eligibility distance if it exposes one, else add a single Stage-0
`HeaderTriggerRangeM` trigger constant to `MatchEngineConstants` — there is no `HEADER_RANGE` constant in
`HeadingMechanicsConstants` today.)*

```
attrs  = PlayerAttributeProjection.ToHeading(_canonicalAttrs[slot], teamId)
intent = a HeaderIntent (PowerIntent/ContactPointIntent from Stage-0 constants,
         TargetIntent toward the opponent goal, AttemptCommittedTick = CurrentTick)
_heading.CommitIntent(agentId, intent, attrs, _ball, CurrentFrame)
```

`_heading.Update` then runs the two-pass eligibility/contact resolution and, on a successful header,
applies the new ball velocity via the ball adapter.

**KD-7**: at most one header intent per agent per airborne-ball episode (a per-agent latch cleared when
the ball leaves head range or is possessed). Committing to only the single nearest eligible agent per
tick keeps the trigger conservative and avoids 22 simultaneous headers.

### 4.3 Why heuristics, not the DT

This mirrors `MatchFlowCollisionConsumer` exactly: match-flow completion added heuristic foul
detection because the DT produced no foul candidates, and it was accepted as the Stage-0 substrate a
later DT layer would supersede. GK/heading intents are the same shape of gap. The design does not
pretend these heuristics are a football-accurate model — they are the minimal deterministic producer
that makes the intent seam (and thus the projections) live.

---

## 5. The projections

Added to `PlayerAttributeProjection.cs`, replacing the "deliberately ABSENT" note (which cited KD-P8's
phantom-consumer reasoning — now resolved because §4 gives them a consumer):

- `ToHeading(in PlayerAttributes c, int teamId) → HeadingAgentAttributes`: per §3.6 — `Heading`,
  `Strength`, `Balance` copied from the identically-named canonical `[1,20]` ints; `Fatigue` seeded to
  the Stage-0 rested value (`0.0`); `TeamId` from the runtime arg (KD-P4 — team is runtime, not
  canonical).
- `ToGoalkeeper(in PlayerAttributes c, int teamId) → GoalkeeperAgentAttributes`: per §3.7 — `Reflexes`,
  `Handling`, `Composure`, `Strength`, `Aerial`, `Balance`, `OneVsOne`, `Pace`, `Throwing`, `Kicking`
  copied from identically-named canonical `[1,20]` ints; `Fatigue` seeded `0.0`; `TeamId` runtime.
  The struct's normalized accessors (÷20) are the struct's own concern — the projection writes raw
  `[1,20]`, matching every other `To*` in the file.

**KD-8 (GK routing gate)**: `ToGoalkeeper` is called **only for the goalkeeper slot** — the §4.1
trigger indexes it by the defending keeper, and the design doc §6 GK routing contract ("call iff
`_isGoalkeeper[i]`") is honoured at that one call site. `ToHeading` may be called for any outfield
agent (§4.2). Both are pure static functions with no side effects, consistent with the rest of
`PlayerAttributeProjection`.

The canonical source is `_canonicalAttrs[slot]` (the records `ConfigureSquads` already seeds, default
`CreateDefault()` when unconfigured — so an unconfigured match still gets valid neutral GK/heading
attributes, and a distinct-squad match gets the roster's real ones). CS0104 note (KD-P6): fully-qualify
`TacticalDirector.PlayerDatabase.PlayerAttributes` in the new signatures, as the rest of the file does.

---

## 6. Serialization (`SNAPSHOT_SCHEMA_VERSION` 17 → 18) — **Phase 2, deferred**

> **Phase 1 (this landing):** NOT implemented. The wiring is opt-in (KD-11 / §1.2a); with the flag off
> the snapshot layout is unchanged (`SNAPSHOT_SCHEMA_VERSION` stays **17**, default engine byte-identical).
> With the flag **on**, `SerializeWorldState` and the durable-capture seams (`CaptureDurableHeader` /
> `CaptureDurablePayload`) **throw `NotSupportedException`** — an ON engine is deterministic forward but
> not yet snapshot-safe. The section below is the Phase-2 plan.

The wiring introduces cross-tick, digest-load-bearing state. To keep the snapshot-deserialize KD-5
round-trip contract ("save@N → restore → tick to N+K == uninterrupted run") for an always-on engine,
all of it must be serialized and restored (Phase 2).

### 6.1 New serialized state

1. **Two RNG stream cursors** — `_headingStreamIndex` and `_goalkeeperStreamIndex`, each a
   `RngStreamState` cursor (`RngCursor` + `ActionOrdinal`), serialized exactly like the v17
   `match-flow.card-severity` cursor. Small, mechanical.
2. **Heading in-flight state** — the cross-tick per-agent set that `HeadingMechanics.CaptureState`
   declares and proves (KD-10), for `[0, MaxAgents=22)`. Fixed layout.
3. **Goalkeeper in-flight state** — the cross-tick per-GK set that `GoalkeeperMechanics.CaptureState`
   declares and proves (KD-10), for `[0, MaxGkAgents=2)`. Fixed layout. (The exact field list is the
   orchestrator's to fix at implementation; the design does not pre-commit it.)
4. **Engine-side trigger latches** — `_saveCommittedForShot`, the per-agent header latch, and any
   other §4 latch (small).

### 6.2 Capture/restore seams (KD-2 discipline)

Each orchestrator gets **`CaptureState` / `RestoreState` seams** so the field-by-field serialization
lives inside the orchestrator (encapsulation; mirrors the Pressing/Defensive/Attacking/Perception
`RestoreState(in XxxTickState)` seams Phase 1 added). `MatchEngine.SerializeWorldState` writes them via
a new `WriteHeadingState` / `WriteGoalkeeperState` block appended after the perception block;
`DeserializeWorldState` reads them into the reconstructed orchestrators through the seams. The two RNG
cursors serialize next to the card-severity cursor.

**KD-9**: the orchestrators are boot-constructed identically on restore (same adapters, same injected
RNG), then their in-flight state is restored through the seams — the reader does NOT re-run any partial
save/header. The `RestoreFromSnapshot` factory (Phase 1) already boots a fresh engine and deserializes;
these two blocks slot into that existing flow.

**KD-10 (bounded surface)**: only fields that (a) mutate across ticks AND (b) influence future ball /
agent state or the digest are serialized. Boot-constant per-agent data (e.g. GK bench flags,
`gkAgentIds`) is reconstructed at boot, not serialized — the same B3 exclusion class the existing
writer uses. Each orchestrator's `CaptureState` documents its excluded-set proof, as the match-engine
writer already does.

### 6.3 Version bump

`MatchEngineConstants.SNAPSHOT_SCHEMA_VERSION` 17 → 18. `MatchEngineSnapshotSchemaTests` pins 18 and
adds a `HeadingState_FeedsSnapshotDigest` + `GoalkeeperState_FeedsSnapshotDigest` probe (a committed
intent's in-flight state reaches the digest preimage). No `MATCH_SAVE_FORMAT_VERSION` /
`SEASON_SAVE_FORMAT_VERSION` change — those frame the body opaquely (a bump to the body schema is
carried transparently).

---

## 7. Test plan

- **`PlayerAttributeProjectionTests`** — `ToHeading` / `ToGoalkeeper` per-field scale locks with
  distinct inputs (each canonical field lands in the right struct field), neutral-equivalence
  (`CreateDefault()` → the mid-range struct), team-id routing.
- **`MatchEngineGkHeadingTests`** (new, Phase 1) — the live consumer proof on a **flag-on** engine: a
  booted engine with `EnableGkHeading()` set, a shot on target commits a save intent seeded from
  `ToGoalkeeper` (observed via a `TestOnly_LastCommittedSaveAttrs` seam), and an airborne ball near an
  agent commits a header intent seeded from `ToHeading` (`TestOnly_LastCommittedHeaderAttrs`); a
  distinct-squad match routes the roster's real GK/heading attributes (not neutral); two independent
  flag-on engines run the same seed **forward** and match digest-for-digest (forward determinism); a
  **flag-off** engine is digest-identical to a pre-wiring engine (byte-identical default); and
  `SerializeWorldState` on a flag-on engine throws `NotSupportedException` (the honest Phase-1 boundary).
- **`PlayerAttributeProjectionTests`** — per-field scale locks for `ToHeading` / `ToGoalkeeper`.
- **Phase 2 (deferred):** `MatchEngineSnapshotSchemaTests` pin 18 + digest probes;
  `MatchEngineSnapshotRestoreTests` G3 round-trip with a save + header in flight; the closed-loop
  `#19 ScenarioRunner` scenario; the always-on digest rebaseline.
- **Full dotnet gate** — PASSED, 0 failures (whole tree green; **no** rebaseline in Phase 1 — default off).

---

## 8. Risks

1. **Serialization surface (largest risk).** Two orchestrators' in-flight state is a real, if bounded,
   surface. Mitigation: `CaptureState`/`RestoreState` seams keep the field knowledge inside each
   orchestrator; KD-10 bounds the surface to genuinely cross-tick fields; the restore round-trip test
   (§7) is the correctness gate.
2. **Trigger destabilizing the capstone.** A too-eager save/header trigger could make the capstone
   scenario diverge wildly or loop. Mitigation: the §4 latches (one save per shot, one header per
   episode) + the conservative geometry gates; the capstone is re-run and its envelope re-checked (a
   rebaseline, not a removal).
3. **Phase-order correctness.** The GK `Update` MUST deflect the ball before `CheckRestartAndApply`
   reads it, or a saved shot is still scored. Mitigation: §3.4 pins GK/heading `Update` in
   `RunPhysicsPhase`, strictly before the Resolve-phase goal check.
4. **CS0104 on `PlayerAttributes`.** Fully-qualify in the new projection signatures (KD-P6 precedent).

---

## 9. Key decisions (index)

| KD | Decision |
|----|----------|
| KD-1 | One engine-level RNG stream per subsystem (`entityId: -1`), the card-severity precedent — not per draw site / per agent (#16 §4.5 defers that to Stage 1). |
| KD-2 | Stateless ball/RNG adapters bridging to `this`; all ball mutation through the one `ApplyKick` seam. |
| KD-3 | RNG adapters accept-and-ignore `drawSiteId`/`domainTag` for stream selection (the stub's Stage-0 posture); determinism from the single ordered stream. |
| KD-4 | Heading and GK streams separate from each other and from card-severity; fixed registration order. |
| KD-5 | Heading's `CollisionConsumer` (AGENT_BALL duel feed) is NOT wired this landing — KD-7's single-agent headers are uncontested, so the collision path stays entirely untouched; fan-out is a follow-up. |
| KD-6 | GK save trigger: at most one save intent per shot (latch); rush/distribute not live-triggered this landing. |
| KD-7 | Header trigger: at most one header per agent per airborne episode; single nearest eligible agent per tick. |
| KD-8 | `ToGoalkeeper` called only for the goalkeeper slot; `ToHeading` for outfield agents. |
| KD-9 | On restore, orchestrators boot-construct identically then restore in-flight state through seams — no partial replay. |
| KD-10 | (Phase 2) Serialize only genuinely cross-tick, digest-load-bearing fields; boot-constants reconstructed, with an excluded-set proof per `CaptureState`. |
| KD-11 | Land opt-in, default-off (§1.2a): flag-off engine byte-identical (no schema change, whole tree green); flag-on drives the orchestrators + triggers and proves the projections live, but fails loud on snapshot until Phase-2 serialization exists. |

---

## 9a. Implementation outcome (2026-07-22) — LANDED (Phase 1)

Implemented per the Phase-1 (opt-in) plan. `MatchEngine.cs` constructs both orchestrators + four
stateless adapters at boot, registers `heading.mechanics` / `goalkeeper.mechanics` RNG streams, drives
them under `EnableGkHeading()` (10 Hz tactical + 60 Hz physics), fires the §4 save/header triggers
seeded from `ToGoalkeeper` / `ToHeading`, and fails loud on the durable-capture seams when the flag is
on. `PlayerAttributeProjection.cs` v1.2 adds the two projections. Two new `MatchEngineConstants` GT
trigger blocks; a `RefreshGkAgentIds` keeps the keeper roster correct across `ConfigureSquads` /
substitutions.

**Code adversarial review folded in during landing (verified against the gate):**
- CS0118 — the orchestrator class names collide with their own namespaces; fully-qualified at the
  field decls + construction (the projection-design CS0104 hazard's sibling).
- `_gkAgentIds` staleness — `ConfigureSquads` reassigns `_isGoalkeeper` (and substitutions move the GK
  slot), so the boot-cached keeper roster would go stale; now refreshed at the top of each drive.
- Guard placement — the fail-loud guard is on `CaptureDurableHeader`/`CaptureDurablePayload` (the
  durable save/restore seams), NOT the per-tick `SerializeWorldState`, so a flag-on engine still ticks
  forward-deterministically while refusing to be saved.

**Tests (all green):** `PlayerAttributeProjectionTests` +2 (ToHeading/ToGoalkeeper field-scale locks);
new `MatchEngineGkHeadingTests` (8 — flag semantics; flag-off default determinism + commits-nothing;
save/header commit the projection; distinct-squad roster GK Pace flows through; flag-on forward
determinism; durable-capture fails loud on/succeeds off). **Full dotnet gate: PASSED, 0 failures
(whole tree green; 290 match-engine tests; SDK 8.0.129 via the Linux gate).** No
`SNAPSHOT_SCHEMA_VERSION` change (Phase 1 adds no serialized state — default engine byte-identical,
verified by the unchanged existing snapshot/determinism/restore suite).

## 10. Adversarial review log

**AR-1 (2026-07-22): 0H + 1M + 2L, all resolved. Claims verified against source.**

- **Verified sound (no change):** `SubsystemOrdinals.HeadingMechanics = 6` / `GoalkeeperMechanics = 7`
  (§3.1 uses the existing ordinals — no new allocation); phase order is `RunPhysicsPhase` (MatchEngine.cs
  ~2670) → Resolve shot-kick (`_shotExecutors[i].Update`, ~2767) → goal check (`CheckRestartAndApply`,
  ~2775), so a Physics-phase GK/heading `Update` precedes the Resolve goal check every tick (§3.4/§4.1
  ordering holds); `HeadingMechanicsConstants.MaxAgents = 22`, `GoalkeeperConstants.MaxGkAgents = 2`;
  the `Reserve`/`DrawReserved`/`CloseReservation` draw triple + `RngStreamState` cursor serialization
  match the `match-flow.card-severity` precedent exactly.
- **M-1 (scope reduction):** the Heading `CollisionConsumer` AGENT_BALL fan-out is not required for
  KD-7's single-agent (uncontested) Stage-0 headers — the duel feed only disturbs *contested* headers.
  Deferred; the collision path (`MatchFlowCollisionConsumer` + the whole collision-event surface) stays
  entirely untouched, shrinking the change and lowering risk #2. §1.1(4), §3.4, KD-5 updated.
- **L-1:** §4.2 named a `HEADER_RANGE` constant that does not exist in `HeadingMechanicsConstants`
  (grep-confirmed). Reworded to "reuse #10's eligibility distance or add a Stage-0 `HeaderTriggerRangeM`".
- **L-2:** §4.1/§3.4 clarified that the GK save-intent commit detects the shot executor's just-fired
  CONTACT (state transition at ~2767), not a pre-existing published event, and that the dive spans the
  ball's flight ticks (the keeper cannot react on tick T itself — Physics precedes the Resolve kick — but
  has the full flight; the per-tick Physics-before-Resolve invariant guarantees a save deflects before
  the crossing tick's goal check).

**AR-3 (2026-07-22, implementation-time scope revision): 1 structural change.** Reviewing the §6
serialization surface against the two orchestrators' actual field sets (Heading: 5 per-agent arrays × 22;
Goalkeeper: ~22 per-GK arrays × 2) showed that an always-on landing forces a large byte-exact
`CaptureState`/`RestoreState` epic on both sealed orchestrators + a v18 schema change + a digest
rebaseline across the whole snapshot suite — disproportionate to the goal and high-risk in a 400 KB file.
Split into two phases (§1.2a / KD-11): Phase 1 lands opt-in/default-off (byte-identical default, whole
tree green, projections proven live forward, snapshot fails loud when on); Phase 2 does the serialization
+ always-on default. §1.1(7), §1.4, §6, §7, and the KD table updated. This *reduces* scope and risk while
still resolving the phantom-consumer bar; it does not weaken any correctness claim (an ON engine refuses
snapshot rather than silently mis-restoring).

**AR-2 (2026-07-22): 0H + 0M + 1L — CONVERGENCE (L-only round, cycle closed).**

- **L-1:** §6.1(3) listed "state-machine state" among GK serialized fields; the actual field set is
  whatever `GoalkeeperMechanics.CaptureState` declares — the design must not pre-commit the exact field
  list (KD-10 says each orchestrator's `CaptureState` owns and proves its own excluded/included set).
  Reworded §6.1(2)/(3) to "the orchestrator's cross-tick in-flight state (the set `CaptureState` declares
  and proves per KD-10)" rather than enumerating fields the design cannot authoritatively fix.
- **Re-verified with no change:** the two RNG streams are separate from each other and from
  card-severity (KD-4), registered at a fixed boot order (stable indices); the projections' canonical
  source `_canonicalAttrs[slot]` exists and defaults to `CreateDefault()` (unconfigured matches still get
  valid neutral GK/heading attrs); `ToGoalkeeper` is gated to the GK slot (KD-8) at its one §4.1 call
  site; the CS0104 fully-qualify note (KD-P6) is carried into §5; the v18 bump touches neither
  `MATCH_SAVE_FORMAT_VERSION` nor `SEASON_SAVE_FORMAT_VERSION` (opaque body frames, §6.3). No High or
  Medium found on a full re-read; cycle closed.
