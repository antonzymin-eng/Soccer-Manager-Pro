# Match Flow Completion — Design Note

> **Created:** July 13, 2026
> **Last Updated:** July 14, 2026 (**Implementation LANDED and a full code-adversarial-review pass
> completed clean.** AR-1 through AR-6 (§12) converged the plan before any code was written; the
> plan was then implemented in full (`RestartResolver.cs`, `OffsideEvaluator.cs`,
> `SubstitutionReason.cs`, three new Tier A events, `MatchEngine.cs` v1.31,
> `SNAPSHOT_SCHEMA_VERSION` 14 → 15, five new test files — see `match-engine-design.md` v2.0 and
> `src/CLAUDE.md` v2.17 for the full file list). The implementation was then itself put through
> repeated adversarial-review rounds reading the ENTIRE touched surface (not just the diff) each
> time, per the driving instruction — this caught the AR-6 `OffsideEvaluator` bug (§12) during the
> first round, and one further full pass (re-reading `RunResolvePhase` end-to-end, all four
> Mechanics-AI `IsActive` fill sites, the boot/ctor additions, the v15 serialization block, all
> three new event structs, `RestartResolver`/`OffsideEvaluator` against their tests, and all five
> new test files) found nothing further — the code-review cycle is closed.)
> **Status:** DESIGN NOTE (Stage 0+1 integration scaffolding, same governance class as
> `match-engine-design.md` — NOT a numbered spec; does not introduce a spec #23+ number).
> **IMPLEMENTED** July 14, 2026 — see the Last Updated entry above.
> **Author:** —
> **Purpose:** Design for the remaining match-flow restart/discipline/clock model the
> match engine has never had: throw-ins, corners, goal kicks, fouls/cards, offside,
> substitutions, half-time break, full-time end. Today only kickoff (boot) and the
> goal centre-spot restart exist (`match-engine-design.md` v1.4, `MatchEngine.cs` v1.30).
> This note is the plan; it is adversarially reviewed before implementation starts,
> mirroring the project's spec-before-code discipline for scaffolding this size.

---

## 0. Scope and governance

Not a numbered spec — it extends `src/match-engine/`, which already owns "engine
substrate" match-flow logic not assigned to any of the 26 approved specs (goal
detection, `MATCH_TICKS_TOTAL`/`HALF_TIME_BOUNDARY_TICK`, the manager decision gate).
This note's additions are the same class of thing: composition-root orchestration
over already-approved subsystems, not new gameplay physics.

**Explicitly out of scope (documented deferrals, not silently dropped):**
- Full geometric offside law (last-instant-of-pass freeze-frame). Stage 0 uses a
  reception-time approximation — see §4.
- A real bench/transfer/injury model. Stage 0 seeds a fixed in-code bench roster
  per team, mirroring the `TeamTacticConfig`/`PlayerTacticConfig` Stage-0 pattern
  (in-code source now, on-disk loader is a later parser swap) — see §6.
- VAR, added/stoppage time, extra time, penalty shootouts.
- The true half-time **ends-swap** (physically repositioning agents / flipping
  attack direction) — `team 0 attacks +X` is hardcoded across goal detection,
  offside, and every Mechanics-AI frame mapping; safely flipping it mid-match
  is out of scope here (AR-4, §7). Stage 0 only resets the ball and publishes
  the transition event.
- Throw-in/corner/goal-kick *ceremony* (a player physically taking the restart,
  a defensive wall, retreat distance). Stage 0 places the ball and clears
  possession, exactly like the existing goal-restart precedent — agents keep
  their positions and naturally contest the loose ball via existing pressing/
  first-touch systems. This mirrors `CheckGoalAndRestart`'s own documented
  minimalism, not a new simplification style.
- Per-pair foul cooldown / tackle physics model. Fouls are read from the
  *existing* `ContactType` classification `CollisionSystem` already produces
  (`FROM_BEHIND` + impact force) — no new physics is invented. A single global
  cooldown prevents a sustained overlap from generating repeated cards (§3).

---

## 1. What exists vs. what this adds

| Concern | Status |
|---|---|
| `BallCollision.CheckBoundaries` classifies `ThrowIn`/`Corner`/`GoalKick`/`KickOff` | ✅ exists, only `KickOff` (goal) consumed |
| `MatchEngine.CheckGoalAndRestart` (goal detection + centre-spot restart) | ✅ exists |
| `MATCH_TICKS_TOTAL` / `HALF_TIME_BOUNDARY_TICK` constants | ✅ exist, consumed only by `ManagerDecisionGate` (tactic re-evaluation cadence, not a real clock stop) |
| `FoulCommittedEvent` / `CardIssuedEvent` / `SubstitutionEvent` registry rows (0x05/0x06/0x08) | ✅ registered in `EventRegistry`, **zero producers anywhere** |
| `CollisionSystem` → `ContactType`/`ContactForceData` (foul-detection groundwork) | ✅ exists, `ICollisionEventConsumer` is a null-object today |
| `OffsideTrapController` (Defensive AI #14) | ✅ exists — this is AI **marking/line** behaviour, NOT law enforcement; no offside violation/stoppage exists anywhere |
| Bench / squad-beyond-22 model | ❌ none — this note adds the minimal Stage-0 version |
| Throw-in / corner / goal-kick restart application | ❌ this note |
| Foul/card detection + application | ❌ this note |
| Offside violation detection + application | ❌ this note |
| Substitution application | ❌ this note |
| Half-time ends-swap + full-time freeze | ❌ this note |

---

## 2. Data model additions (`MatchEngine` fields)

```
// Discipline (per on-pitch slot, cross-tick — serialized)
byte[22]  _yellowCards;          // 0/1/2 (2nd yellow ⇒ red same tick)
bool[22]  _isSentOff;            // red-carded; skipped by AI + frozen by movement
int       _foulCooldownRemaining; // ticks; global debounce, cross-tick — serialized

// Substitutions (cross-tick — serialized)
int[22]   _activeBenchSlot;      // -1 = original starter; else index into the team's bench
int[2]    _substitutionsUsed;    // per team

// Bench roster (boot-deterministic config; NOT serialized — same class as _attrs/_perfs)
PlayerAttributes[2][SUBSTITUTES_PER_TEAM]   _benchAttrs;
PerformanceContext[2][SUBSTITUTES_PER_TEAM] _benchPerfs;
bool[2][SUBSTITUTES_PER_TEAM]               _benchIsGoalkeeper;

// Match-flow clock (cross-tick — serialized)
bool _secondHalfStarted;
bool _matchEnded;
```

`_activeBenchSlot`/`_substitutionsUsed`/`_secondHalfStarted`/`_matchEnded`/
`_yellowCards`/`_isSentOff`/`_foulCooldownRemaining` are new cross-tick gameplay
state → **`SNAPSHOT_SCHEMA_VERSION` 14 → 15** (§8). The bench arrays stay
unserialized under the same B3 exclusion proof `_attrs`/`_perfs` already use:
they are boot-deterministic constants, and `_activeBenchSlot` is sufficient to
reconstruct which bench identity is active at a slot on restore (§6).

---

## 3. Fouls & cards

**Detection, not invention.** `CollisionSystem` already classifies
`AGENT_AGENT` contacts via `ContactTypeClassifier` into `ContactType` (SHOULDER/
FROM_BEHIND/SIDE_IMPACT/…) and carries `CollisionEvent.FoulData`
(`ContactForceData`: `InstigatorAgentID`/`VictimAgentID`/`Type`/`ForceMagnitude`,
already populated at Stage 0 per its own header — "populated but not consumed").
`MatchEngine` today passes a private nested `NullCollisionEventConsumer` to
`CollisionSystem.UpdateCollisions` (empty `OnCollisionEvent` body). This note
renames/repurposes that class to `MatchFlowCollisionConsumer` — still the
sole `ICollisionEventConsumer`, but its `OnCollisionEvent` now captures **at
most one** foul candidate per tick into fixed scalar fields on `MatchEngine`
(`_foulCandidateFound`/`_foulCandidateOffender`/`_foulCandidateVictim`/
`_foulCandidateForce`; no array/buffer needed — the consumer only ever keeps
the first qualifying event and ignores the rest, since cards are rare and
only one is acted on per tick). Reset before each `UpdateCollisions` call.

**Foul qualification (checked inside `OnCollisionEvent`):**
`evt.Type == CollisionType.AGENT_AGENT`, `evt.FoulData.Type ==
ContactType.FROM_BEHIND`, `evt.FoulData.ForceMagnitude >=
[GT] FoulImpactForceThresholdN` (set high enough — near the top of the
existing 500–1500 N fall/stumble literature band already documented in
`CollisionSystemConstants` — that incidental jogging/shoulder contact in
existing match-engine tests cannot spuriously qualify), the two agents are on
opposite teams, and `_foulCooldownRemaining == 0` (checked at capture time,
not just at apply time, so a second qualifying event later in the same
`UpdateCollisions` call — impossible today since only the first is kept, but
guarded for clarity). Instigator = offender, victim = the other agent.
Processing (RNG draw, card issuance, restart) happens in a new
`ApplyFoulIfCaptured()` called right after `UpdateCollisions` returns, still
inside `RunResolvePhase`, before `CheckRestartAndApply`.

**On a qualifying foul:**
1. Publish `FoulCommittedEvent(offender, victim, location, foulKind: (byte)ContactType.FROM_BEHIND)`.
2. Draw card severity from a new registered RNG stream (`match-flow.card-severity`,
   `DeterministicRngService.RegisterStream` at Boot, one `NextFloat`-equivalent
   draw per foul via `DrawReserved`/`Reserve` — same pattern as collision's own
   foul/stumble draws). `[GT] YellowCardProbability` / `[GT] RedCardProbability`
   (disjoint bands: `[0, Red)` = straight red, `[Red, Red+Yellow)` = yellow,
   else no card — a foul with no card is common and correct).
3. If yellow: `_yellowCards[offender]++`. If this is the offender's 2nd yellow,
   promote to red (`CardKind = 2` per `CardIssuedEvent`'s own documented
   ordinal, `SecondYellow`) and set `_isSentOff[offender] = true`.
   If red (straight or 2nd-yellow): `_isSentOff[offender] = true`.
   Publish `CardIssuedEvent` only when a card is actually issued.
4. `_foulCooldownRemaining = [GT] FoulCooldownTicks` (60 = 1s at 60 Hz).
5. Award a free kick to the victim's team at the foul location — reuses the
   restart-application primitive from §5 (ball placed, possession cleared).

**Sent-off handling.** No replacement is drawn (correct per Laws of the Game —
red card ≠ substitution). A sent-off agent is excluded from the per-agent AI
loop in `RunAiPhase` (their `TacticalContext`/`DecisionTree` snapshot step is
skipped — no new action dispatched) and their `MovementCommand` is forced to
`Stop` each `RunPhysicsPhase` before `UpdateAllAgents` (they decelerate to rest
and stay there; no `AgentMovementSystem`/`DecisionTree` signature changes).
This is an orchestration-level skip, not a new interface — mirrors the existing
goalkeeper-skip pattern in `RunPhysicsPhase`.

`_foulCooldownRemaining` decrements once per tick (floor 0) at the top of
`RunResolvePhase`, before the collision/foul check.

---

## 4. Offside

**Why not full geometric law.** No "Referee System" spec exists yet (Heading
Mechanics OI-006 / KD-9 explicitly defer offside adjudication to "a future
referee spec"). Building the exact law (freeze positions at the instant the
ball is played, compare against the second-last opponent OR the ball,
whichever is nearer) needs a hook at `PassExecutor` CONTACT time that persists
a frozen snapshot across however many ticks the ball is in flight — a
materially larger, separately-reviewable piece of work. This note implements a
documented Stage-0 approximation instead, in the same spirit as
`BallCollision.CheckBoundaries`'s own inline-documented simplifications.

**Stage-0 model — evaluated at reception, not at the pass:**
In `RunFirstTouch`'s `case TouchResult.Controlled:` branch (verified against
the actual method — `_possessingAgentId = result.PossessingAgentID;` is the
only statement there today), check BEFORE that assignment: `_lastHolderAgentId`
still holds the PREVIOUS tick's holder at this point (the `_lastHolderAgentId =
_possessingAgentId` update runs later in `RunResolvePhase`, after
`UpdateMatchContext`, per the existing fixed intra-Resolve order) — so
`_lastHolderAgentId >= 0 && _teamIds[_lastHolderAgentId] == _teamIds[toucher]
&& _lastHolderAgentId != toucher` is exactly "a genuine same-team pass
reception, not an interception and not the same agent re-touching a loose
dribble":

1. Compute the defending team's offside line **from live agent positions**
   (not the Positioning AI's normalized `DefensiveLineDepth` anchor value,
   which is a formation *target*, not a scan of actual defender positions —
   using it here would silently misrepresent the law). New pure helper
   `OffsideEvaluator.ComputeOffsideLineX(agents, teamIds, teamId, squadSize)`:
   among the defending team's active (`!_isSentOff`), on-pitch agents, sort by
   distance-to-own-goal ascending and return the **second-nearest** agent's X
   (index 1 — GK included, matching "second-last opponent" from the Laws,
   since the GK is usually nearest its own goal).
2. Toucher is offside iff: toucher is in the opponent's half (team-relative;
   own-half receptions are never offside) AND toucher's X is nearer the
   opponent's goal line than the computed offside-line X.
3. On a violation: **no explicit "undo" is needed** — `_possessingAgentId =
   result.PossessingAgentID` is simply never executed for this tick (the
   assignment is skipped, not rolled back), and the shared `ApplyRestart`
   primitive (§5) is called immediately with the toucher's position and the
   defending team, which independently sets the ball to the restart position
   at rest and `_possessingAgentId = NO_POSSESSION` — the same "stomp, don't
   undo" minimalism the goal-restart already uses (it does not undo the
   scoring kick's trajectory either). A new
   `OffsideCalledEvent(offendingAgentId, team, location)` is published.
   `ApplyTouchResult`'s ball displacement from the (offside) touch is
   overwritten by `ApplyRestart` in the same tick, so it never reaches the
   snapshot or a later tick — matches how the goal-restart already overwrites
   a possessed-into-the-goal ball the same tick (§3 of `match-engine-design.md`).

**Deliberately dropped nuance (documented, not hidden):** the "or level with
the ball" alternative offside line, and freeze-at-pass-time. Both are
Stage 1+ items once/if a referee spec is written.

---

## 5. Throw-ins / corners / goal kicks — restart primitive

New pure static helper `RestartResolver` (mirrors `BallCollision`'s style):

```
RestartResolver.Resolve(RestartType type, Vector3 ballPos, int lastTouchTeam)
  → (Vector3 restartPosition, int awardedTeam)
```

**AR-1 finding (self-review), fixed before implementation:** for all three
non-goal `RestartType` values, `awardedTeam` is uniformly `1 - lastTouchTeam`
— verified against the actual `BallCollision.CheckBoundaries` branches: at the
home goal line, `lastTouchTeamID == 0` (home touched last, put it behind their
*own* line) ⇒ `Corner`, awarded to away = `1 - 0`; `lastTouchTeamID == 1` ⇒
`GoalKick`, awarded to home = `1 - 1`... = 0. Both branches reduce to
"awarded to whichever team did NOT touch it last" — the same rule a
`ThrowIn` already follows. `RestartResolver` therefore computes `awardedTeam
= 1 - lastTouchTeam` once, for all three types; only the ball **position**
differs by type:

- **ThrowIn:** position = exit point clamped to the touchline (`y = 0` or
  `y = PITCH_WIDTH_M`, whichever the ball was nearer, `x` clamped into
  `[0, PITCH_LENGTH_M]`).
- **Corner:** position = the corner point on the exited goal line nearest the
  ball's Y (`(0 or LENGTH, 0 or WIDTH)`), inset by
  `BallPhysicsConstants.Ball.RADIUS` so the ball stays in bounds.
- **GoalKick:** position = centre of the six-yard box on the exited goal line
  (new `[FIXED] GOAL_AREA_DEPTH_M = 5.5`, IFAB Laws of the Game §1; `y =
  PITCH_WIDTH_M / 2`).

**Application (shared by §3/§4/§5, one method `ApplyRestart(Vector3 pos, int
awardedTeam)`):** ball placed at `pos` at rest height, `Velocity = 0`,
`_possessingAgentId = NO_POSSESSION` — identical minimalism to the existing
goal-restart (agents keep positions; no ceremony; the natural gameplay loop
picks up the loose ball). A new Tier A `RestartAwardedEvent(restartKind,
awardedTeam, location)` is published for non-goal restarts (goals already
publish `GoalAwardedEvent`; fouls/offside already publish their own event —
`RestartAwardedEvent` covers exactly ThrowIn/Corner/GoalKick, the three kinds
that previously had no restart model at all).

`CheckGoalAndRestart` is extended (not replaced) into `CheckRestartAndApply`:
still classifies via `BallCollision.CheckBoundaries` first; `KickOff` (goal)
keeps its existing centre-spot + `GoalAwardedEvent` path unchanged; the other
three non-`None` results now route through `RestartResolver` + `ApplyRestart`
+ `RestartAwardedEvent` instead of being silently ignored.

---

## 6. Substitutions

**Bench roster (Stage 0 in-code source, parser-swap-ready — the `TeamTacticConfig`
precedent):** `[GT] SUBSTITUTES_PER_TEAM = 7`, `[GT] MAX_SUBSTITUTIONS_PER_TEAM = 5`.
New `BenchRosterConfig` (mirrors `TeamTacticConfig`): `Default` seeds every bench
slot to `PlayerAttributes.CreateDefault()` / `PerformanceContext.CreateNeutral()`
/ `IsGoalkeeper = false` — i.e., configuring nothing changes nothing (no
identity risk since a substitution is opt-in via an explicit API call, never
autonomous).

**API:** `public void SubstitutePlayer(int teamId, int outSlotIndex, int
benchIndex, SubstitutionReason reason)` (new small enum: `Tactical`/`Injury`).
Guards (fail loud, `ArgumentException`/`InvalidOperationException` — this is
a boot/manager-decision-time call, not hot-path, so exceptions are fine, same
class as `SetTeamTactic`): `teamId` in range; `outSlotIndex` on-pitch, belongs
to `teamId`, not already `_isSentOff`, not already substituted
(`_activeBenchSlot[outSlotIndex] == -1`); `benchIndex` in range and not
already used this match for that team; `_substitutionsUsed[teamId] <
MAX_SUBSTITUTIONS_PER_TEAM`.

**Effect:** `_attrs[outSlotIndex] = _benchAttrs[teamId][benchIndex]`;
`_perfs[outSlotIndex] = _benchPerfs[teamId][benchIndex]`;
`_isGoalkeeper[outSlotIndex] = _benchIsGoalkeeper[teamId][benchIndex]`;
`_decisionTrees[outSlotIndex].NotifyInterrupt()` (existing seam — forces a
fresh plan next AI stride instead of continuing whatever the outgoing player
was mid-executing); `_activeBenchSlot[outSlotIndex] = benchIndex`;
`_substitutionsUsed[teamId]++`. Position/velocity are left untouched (no
re-entry ceremony — same minimalism precedent as the rest of this note).
Queues `SubstitutionEvent(outgoing: outSlotIndex, incoming:
SQUAD_SIZE + teamId * SUBSTITUTES_PER_TEAM + benchIndex, team: (byte)teamId,
substitutionReason: (byte)reason)` for publication at the top of the next
`RunResolvePhase` (AR-5 — this method may be called between ticks, when
`EventBus.CurrentPhase` is not a valid producer phase; the state effect above
still applies immediately) — the synthetic incoming id gives the bench player
a stable identity distinct from any on-pitch slot index.

**Documented Stage-0 simplification:** Positioning/Pressing/Defensive/
Attacking AI hysteresis state is keyed by slot index, not player identity, so
the incoming player inherits whatever momentary tactical state that slot held
(e.g. mid-dwell counters). This is a minor behavioural quirk, not a
correctness bug — the state re-converges within a few ticks, same class as
the `RotationController` slot-identity model already accepted in #25.

**Serialization scope (AR-3 correction):** `MatchEngine` has no working
snapshot-decode/restore path today — `SerializeWorldState` is a one-way write
into the digest/payload (verified: no `Deserialize`/`Restore` method exists
anywhere in the file; every prior "restore-deterministic" landing, e.g.
ERR-021-002, means only "now serialized, so a future real save/load system
would reconstruct it," not that one exists). So this note does the same as
every prior field addition: `_activeBenchSlot`/`_substitutionsUsed` are new
cross-tick fields appended to `SerializeWorldState` (§9) and digest-probed in
tests — `_attrs`/`_perfs`/`_isGoalkeeper` stay unserialized under the existing
B3 exclusion (still boot/config-deterministic; a substitution just mutates
which config values populate those slots).

---

## 7. Half-time break & full-time end

**No real-world pause.** The sim has no wall-clock at all (`MatchClock` is
tick-only); "break" and "full-time whistle" are modelled as instantaneous
tick-boundary transitions, not a suspended interval — consistent with every
other restart in this note being instantaneous.

New `CheckMatchFlowTransitions()`, called every tick (not stride-gated) at the
top of `RunInputPhase`, after `EventBus.BeginTick`/`BeginPhase(Input)`:

- **Half-time (once, guarded by `_secondHalfStarted`):** fires the first tick
  `CurrentTick >= HALF_TIME_BOUNDARY_TICK`. **AR-4 correction (caught during
  implementation): no agent-position ends-swap is performed.** `team 0 always
  attacks +X` is a FIXED convention hardcoded across goal detection
  (`CheckRestartAndApply`'s scoring-team branch), `OffsideEvaluator`
  (`defendsHomeGoal = defendingTeam == 0`), and every Mechanics-AI
  `MirrorPitchIfAway` call — physically mirroring agent positions without
  ALSO flipping that convention everywhere it's load-bearing would make team 0
  sit in the away half while the engine still thinks team 0 defends x=0,
  breaking goal/offside classification for the entire second half. Flipping
  the convention itself is a large, invasive change to already-reviewed
  methods, disproportionate to this note's scope. Stage-0 model: ball reset
  to centre spot at rest, possession cleared (goal-restart precedent),
  `MatchPhaseChangedEvent(newPhase: SecondHalf, homeScore: _goals[0],
  awayScore: _goals[1])` published — a real, visible transition marker —
  but agent positions and the attack-direction convention are untouched.
  Sets `_secondHalfStarted = true`. The true ends-swap is a documented
  Stage-1+ deferral (§0), same class as throw-in/corner ceremony.
- **Full-time (once, guarded by `_matchEnded`):** fires the first tick
  `CurrentTick >= MATCH_TICKS_TOTAL`. Publishes `MatchPhaseChangedEvent(newPhase:
  FullTime, homeScore, awayScore)`. Sets `_matchEnded = true`.

**Freeze after full-time (verified against actual phase bodies).**
`RunPhysicsPhase` and `RunResolvePhase` both start with
`EventBus.BeginPhase(PhaseId.X);` as their literal first line (confirmed by
reading the current source) — each gains `if (_matchEnded) return;`
immediately after that line, so the EventBus phase-lifecycle invariant (every
phase entered every tick) is preserved and only the gameplay mutation body is
skipped. `RunAiPhase` does **not** call `BeginPhase` itself (that happens
unconditionally in `RunIntentPhase` per the existing §2.4 contract) — its
guard goes after the two observation-counter increments
(`_aiPhaseRanThisTick`/`_aiPhaseRunCount`, so stride-cadence tests are
unaffected) and before `RunManagerDecisionPoints`/any tactic commit/mechanics
AI call. A host that keeps calling `RunTick` past full time gets a frozen,
still-serializable match rather than continued simulation or a thrown
exception.

**Incidental fix, called out explicitly:** `UpdateMatchContext` today hardcodes
`_matchContext.HomeScore = 0; AwayScore = 0;` even though `_goals[]` has
existed since the goal-detection landing. This note fixes it to
`_matchContext.HomeScore = _goals[0]; AwayScore = _goals[1];` while editing
this method for the half-time score in the new event — a one-line correction
of a pre-existing latent bug, not new scope creep.

---

## 8. Event additions (`src/event-system/`)

Three new Tier A events, next free ordinals after the existing 0x01–0x17
range (`EventRegistry.cs`): `0x18`, `0x19`, `0x1A`. All authored by
`match-engine`, so `subsystemOrdinal: SubsystemOrdinals.EventSystem` (the same
convention `GoalAwardedEvent`/`PossessionChangedEvent`/etc. already use — no
new `SubsystemOrdinals` entry needed), `producerPhaseIndex: PhaseId.Resolve`
(all three are Resolve-phase events, matching every existing Tier A row here)
except `MatchPhaseChangedEvent`, whose half-time/full-time transitions are
detected in `RunInputPhase` — `producerPhaseIndex: PhaseId.Input`.

```
OffsideCalledEvent   (0x18): OffendingAgentId (int), Team (byte), Location (Vector3)
RestartAwardedEvent  (0x19): RestartKind (byte, mirrors BallPhysics.RestartType ordinal),
                             AwardedTeam (byte), Location (Vector3)
MatchPhaseChangedEvent (0x1A): NewPhase (byte: 0=SecondHalf, 1=FullTime),
                             HomeScore (int), AwayScore (int)
```

Each embeds the standard 12-byte header per §2.4.1 (matching
`GoalAwardedEvent`/`FoulCommittedEvent` exactly).

---

## 9. Snapshot schema — `SNAPSHOT_SCHEMA_VERSION` 14 → 15

Appended fields, in this order, after the existing v14 tail
(`ManagerState` per-team block): `_yellowCards[22]` (u8 ×22), `_isSentOff[22]`
(bool ×22), `_foulCooldownRemaining` (i32), `_activeBenchSlot[22]` (i32 ×22),
`_substitutionsUsed[2]` (i32 ×2), `_secondHalfStarted` (bool),
`_matchEnded` (bool).

---

## 10. New constants (`MatchEngineConstants.cs`)

| Constant | Tag | Value | Notes |
|---|---|---|---|
| `GOAL_AREA_DEPTH_M` | `[FIXED]` | 5.5 | IFAB Laws of the Game §1 (six-yard box) |
| `FoulImpactForceThresholdN` | `[GT]` | illustrative | above this + FROM_BEHIND ⇒ candidate foul |
| `YellowCardProbability` | `[GT]` | illustrative | RNG band width |
| `RedCardProbability` | `[GT]` | illustrative | RNG band width, disjoint from yellow |
| `FoulCooldownTicks` | `[GT]` | 60 | 1 s @ 60 Hz global foul-detection debounce |
| `SUBSTITUTES_PER_TEAM` | `[GT]` | 7 | bench size |
| `MAX_SUBSTITUTIONS_PER_TEAM` | `[GT]` | 5 | per team per match |

All `[GT]` magnitudes are illustrative pending a balance pass, same carve-out
already accepted project-wide (#21 G2 precedent) — the contract under review
here is the shape/wiring, not the tuned numbers.

---

## 11. Test plan (new files under `src/match-engine/tests/`)

- `MatchEngineRestartTests.cs` — `RestartResolver` pure-function unit tests
  (all 3 restart types × both goal ends × both touchline sides) + `MatchEngine`
  integration (ball out for throw-in/corner/goal-kick actually places the ball
  and clears possession; goal path untouched — regression lock).
- `MatchEngineFoulCardTests.cs` — qualifying vs non-qualifying contact (wrong
  `ContactType`, same-team, sub-threshold force, cooldown active); yellow/red
  RNG-band boundaries; second-yellow ⇒ red; sent-off agent frozen in
  Physics + skipped in AI; cooldown decrements/expires.
- `MatchEngineOffsideTests.cs` — `OffsideEvaluator.ComputeOffsideLineX` pure
  unit tests (second-nearest-to-goal selection, both teams); own-half
  exemption; violation reverts the touch + awards the free kick; non-pass
  (interception) receptions never trigger it.
- `MatchEngineSubstitutionTests.cs` — guard rejections (bad team/slot/bench
  index/cap/reused bench/sent-off slot); applied effect on attrs/perf/GK
  flag; event payload; restore-determinism (`Snapshot` → `Restore` reproduces
  the substituted identity from `_activeBenchSlot` alone).
- `MatchEngineMatchFlowTests.cs` — half-time fires once at the boundary tick,
  mirrors positions, resets ball, publishes the event; full-time freezes
  Physics/Resolve/AI while still advancing the tick/snapshot; both guarded
  against double-fire on repeated ticks past their boundary.
- `MatchEngineSnapshotSchemaTests.cs` extended — schema pin 14 → 15 + one
  digest-preimage probe per new field group.

All new tests follow the existing `TestOnly_*` seam pattern (e.g.
`TestOnly_YellowCards(agentId)`, `TestOnly_IsSentOff(agentId)`,
`TestOnly_ActiveBenchSlot(agentId)`, `TestOnly_SecondHalfStarted`,
`TestOnly_MatchEnded`) — no production API surface is added purely for
testability beyond what §3–§7 already need publicly (`SubstitutePlayer`).

**Verification note:** the `dotnet-ci` gate is not runnable in this authoring
environment (network policy blocks the SDK download); every file is
hand-verified for brace/paren balance, member-name accuracy against actual
source (verified by direct `Read`/`Grep` during implementation, not recalled
from memory), and pure-function logic (`RestartResolver`/`OffsideEvaluator`)
is checked against hand-worked coordinate examples in the adversarial review
pass. Full compile+test verification happens in CI on push, per the
project's established pattern for this environment.

---

## 12. Version History

| Version | Date | Notes |
|---|---|---|
| 0.1 | 2026-07-13 | Initial draft. |
| 0.2 | 2026-07-13 | **AR-1 (self-review against actual source): 4 findings, all fixed.** (1) Restart-award logic unified — `RestartResolver.Resolve` computes `awardedTeam = 1 - lastTouchTeam` for ALL THREE non-goal restart types (verified against `BallCollision.CheckBoundaries`'s actual branches: Corner and GoalKick both reduce to "award to whoever did not touch it last", identical to ThrowIn — the original draft implied per-type award logic that would have been redundant/error-prone to implement separately). (2) Foul detection simplified from a 4-slot candidate buffer to single-scalar capture (only the first qualifying collision per tick is ever acted on, so a buffer was over-engineered); qualification fields corrected to the real `ContactForceData`/`CollisionType`/`ContactType` member names (verified by reading the actual files, not recalled). (3) `RunAiPhase`/`RunPhysicsPhase`/`RunResolvePhase` freeze-guard placement corrected against the actual method bodies (Physics/Resolve DO start with `EventBus.BeginPhase`; AI phase does NOT call BeginPhase itself — guard placed after the observation counters instead). (4) Offside hook re-specified against the actual `RunFirstTouch` switch statement and the actual intra-Resolve ordering of `_lastHolderAgentId`'s update (confirmed it is still the PREVIOUS holder at the point `RunFirstTouch` runs), and the "revert" mechanism corrected from an implied undo to "skip the assignment, let `ApplyRestart` stomp the state" — matching the project's existing minimalism precedent instead of inventing new rollback machinery. **AR-2 (second pass): no new findings** — re-checked event-ordinal allocation (0x18–0x1A are free per the actual `EventRegistry.cs` contents through 0x17), `SUBSTITUTES_PER_TEAM`/bench-serialization exclusion proof against the real B3 precedent for `_attrs`/`_perfs`, and confirmed `AgentState.Position` is `Vector2` (so `OffsideEvaluator` and `RestartResolver` signatures in §4/§5 are dimensionally correct as planned). Proceeding to implementation.
| 0.6 | 2026-07-14 | **AR-6 (code-review round, caught while writing tests): 1 real logic bug in `OffsideEvaluator.ComputeOffsideLineX`, fixed.** The implementation left the min-tracking accumulator at its initial ±Infinity sentinel when fewer than two active defenders exist, rather than the documented/tested `NaN`. Hand-tracing `IsOffside` against that sentinel showed it does NOT naturally resolve to "never offside" as a first-pass self-review had assumed: for a team-1 attacker evaluated against team 0's degenerate (+Infinity) line, `IsOffside`'s `toucherX < offsideLineX` is satisfied by every finite `toucherX` — i.e. it would call the attacker offside **always**, exactly backwards from the intended "too few defenders means no offside is possible" rule (the same inversion hits team-0 attackers against a team-1 degenerate `-Infinity` line). Fixed by adding an explicit `activeCount` tally in `ComputeOffsideLineX` that gates the return to `float.NaN` when fewer than two active (non-sent-off, correct-team) agents were found, restoring the NaN path `IsOffside`'s existing top-of-function NaN guard already handles correctly. Both degenerate-input tests strengthened to assert the ACTUAL `IsOffside` outcome for a concrete attacker position (not just the sentinel value in isolation), which is what surfaced the bug in the first place. No other findings this pass on the offside surface; the reception-time hook in `RunFirstTouch`, the restart primitive, and the foul/card pipeline were re-traced by hand against their own tests and found consistent.
| 0.5 | 2026-07-14 | **AR-5 (caught mid-implementation): 1 finding, fixed.** `SubstitutePlayer` is a public API a caller invokes between `RunTick()` calls, when `EventBus.CurrentPhase` is the post-`OnTickBoundary` `0xFF` sentinel — publishing a `SubstitutionEvent` immediately there throws (`EventBus.cs`'s AR-8 M-2 stale-Publish guard; verified against the real `EventBus.Publish<T>` phase-index check). Fixed: the substitution's STATE EFFECT (attrs/perf/GK-flag swap, bench-slot/count bookkeeping) still applies immediately in `SubstitutePlayer`, but the `SubstitutionEvent` itself is queued into a small fixed-capacity buffer (bounded by `MAX_SUBSTITUTIONS_PER_TEAM * TEAM_COUNT`, so it cannot overflow) and flushed by a new `PublishPendingSubstitutions()` call at the top of `RunResolvePhase`, where `CurrentPhase == Resolve` (the registered producer phase). No other public-API event-publish call in this note has the same hazard — `ApplyFoulIfCaptured`/`CheckRestartAndApply`/`EvaluateAndApplyOffside` all publish from inside `RunResolvePhase` (or `RunFirstTouch`, itself called from `RunResolvePhase`), and `CheckMatchFlowTransitions` publishes from inside `RunInputPhase` immediately after that phase's own `BeginPhase` call — all four already run with a valid matching `CurrentPhase` by construction.
| 0.4 | 2026-07-14 | **AR-4 (caught mid-implementation): 1 finding, fixed.** The half-time "ends-swap" design (mirror every agent + the ball across the pitch) would have broken goal/offside detection for the entire second half — `team 0 attacks +X` is a fixed convention hardcoded in `CheckRestartAndApply`'s scoring-team classification, `OffsideEvaluator.ComputeOffsideLineX`'s `defendsHomeGoal` branch, and every Mechanics-AI `MirrorPitchIfAway` call; mirroring positions without also flipping that convention everywhere would leave team 0 physically in the away half while the engine still thinks team 0 defends x=0. §7 corrected to drop the position mirror entirely — half-time now only resets the ball to centre and publishes `MatchPhaseChangedEvent`; the true ends-swap is a documented Stage-1+ deferral. No other findings this pass — proceeding.
| 0.3 | 2026-07-13 | **AR-3 (during implementation prep): 1 finding, fixed.** Verified `MatchEngine` has no working snapshot-decode/restore method anywhere (`SerializeWorldState` is write-only into the digest; every prior "restore-deterministic" note in this file's history means "now serialized, so a future save/load system would reconstruct it," never an actual working restore path). §6's substitution serialization section corrected to stop describing a restore-reconstruction mechanism that doesn't exist — this note only needs to append `_activeBenchSlot`/`_substitutionsUsed` to `SerializeWorldState` like every other cross-tick field this project has ever added. Confirmed `DeterministicRngService.RegisterStream`/`Reserve`/`DrawReserved`/`CloseReservation` call pattern against the real `InteractionTextGenerator` (living-world #22) usage for the new card-severity draw site. Confirmed `RunPhysicsPhase`/`RunResolvePhase` literally start with `EventBus.BeginPhase(...)`. No further findings — proceeding to implementation.
| 1.0 | 2026-07-14 | **Implementation landed; code-adversarial-review cycle closed.** All nine plan items (§3–§7) implemented per the AR-1..AR-6-converged plan above: `RestartResolver.cs` / `OffsideEvaluator.cs` / `SubstitutionReason.cs` (new), three new Tier A events (`OffsideCalledEvent` 0x18 / `RestartAwardedEvent` 0x19 / `MatchPhaseChangedEvent` 0x1A), `MatchEngine.cs` v1.31 (`CheckRestartAndApply`/`ApplyRestart`, `MatchFlowCollisionConsumer`, `ApplyFoulIfCaptured`/`DetermineCardKind`/`ApplyCardAndCheckSentOff`, `EvaluateAndApplyOffside` hooked into `RunFirstTouch`, public `SubstitutePlayer`/`PublishPendingSubstitutions`, `CheckMatchFlowTransitions`), `SNAPSHOT_SCHEMA_VERSION` 14 → 15, and five new test files (`MatchEngineRestartTests`/`MatchEngineOffsideTests`/`MatchEngineFoulCardTests`/`MatchEngineSubstitutionTests`/`MatchEngineMatchFlowTests`) plus `MatchEngineSnapshotSchemaTests` v1.12. Per the driving instruction, the implementation was then itself adversarially reviewed with the same rigor as the plan — multiple rounds reading the ENTIRE touched surface, not just the diff. This surfaced and fixed, beyond the AR-4/AR-5/AR-6 findings already logged above: a stale doc comment on `HALF_TIME_BOUNDARY_TICK` in `MatchEngineConstants.cs` that still described the AR-4-rejected ends-swap; a misleading test comment in `MatchEngineOffsideTests.cs` (`ComputeOffsideLineX_SentOffAndOpponents_Excluded`) that referenced the wrong index as if it were relevant to the excluded set; a latent agent-position/command-drift risk in the foul-card integration test (repositioning an agent via `TestOnly_SetAgent` without also refreshing its held `MovementCommand` would have let the stale boot-time command steer the agent back toward its original position during the same tick's Physics phase, before the foul is applied in Resolve); and a dead `_foulCandidateForce` field carried by the collision consumer but never read (removed, simplifying `TestOnly_InjectFoulCandidate`'s signature). A subsequent full pass — re-reading `RunResolvePhase` end-to-end, all four Mechanics-AI `IsActive` snapshot fill sites, the boot/constructor additions, the v15 serialization block byte-for-byte, all three new event structs against `EventRegistry`'s producer-phase/header-size contract, `RestartResolver`/`OffsideEvaluator` against their own tests, and all five new test files in full — found nothing further. The code-review cycle is closed per the "repeat until no new errors are found" instruction. Tracking docs (root `CLAUDE.md`, `src/CLAUDE.md`, `file-manifest.md`, `match-engine-design.md`) updated in the same pass. Full `dotnet test` gate not runnable in this environment (no SDK access, per this project's own documented precedent for the authoring environment) — verification was by exhaustive manual code review in its place.
