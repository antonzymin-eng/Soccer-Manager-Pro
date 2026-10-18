# Match Flow Completion — Design Note

> **Created:** July 13, 2026
> **Last Updated:** October 16, 2026 (v1.6 — PR #511 / ERR-011-020 own-release re-handling restart contract; snapshot body v27, save format v3.)

> **Last Updated (prior):** October 15, 2026, later (v1.5 — **PR #505 review fix.** Requests that share a stoppage share its window, including a team's third; lock 12 gains two mirrored tests. Prior entry below.)
> **Last Updated (prior):** October 15, 2026 (v1.4 — **§6 deferred substitutions implemented by PR #505.** New §6.5.1
> maps all thirteen acceptance locks to their tests; schema v25 is taken; saves between ticks use match save
> format v3. Prior entry below.)
> **Last Updated (prior):** July 14, 2026 (**Implementation LANDED and a full code-adversarial-review pass
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

### 5.1 Keeper own-release re-handling

This October 16 owner-approved amendment extends `RestartAwardedEvent` beyond the boundary-only
scope above. Physics queues an actual prohibited hand contact; Resolve applies `RestartCue.FreeKick`
and publishes Tier A `RestartAwardedEvent` (ordinal 0x19, unchanged payload layout/version) with these
appended **event codes**, distinct from Ball Physics `RestartType`:

| RestartKind | Award and placement |
|---|---|
| 5, `INDIRECT_FREE_KICK` | Opponents, at contact inside the keeper's own penalty area; inside the goal area, move X to its parallel goal-area line, preserving Y. |
| 6, `DIRECT_FREE_KICK` | Opponents, at contact outside that area; covers only own-release re-handling. General outside-area handling remains D-07. |

Area depth is measured from the defending goal line, with inclusive penalty-area boundary:
16.5 m `[FIXED: IFAB Law 1]` depth and half-width `16.5 m + goalWidth/2` `[DERIVED]`.
The goal area uses 5.5 m `[FIXED: IFAB Law 1]` and half-width `5.5 m + goalWidth/2` `[DERIVED]`.
Example: inside contact `(3, 34)` / `(102, 34)` m moves to `(5.5, 34)` / `(99.5, 34)` m.

`ApplyRestart` tears down possession and hand episodes, executes eligible queued substitutions, then
selects the nearest eligible, non-sent-off player of the awarded team; squared XY distance is primary,
ascending slot index breaks ties. No special taker or wall is added. The handling whistle clears both
the captured foul flag and its already-decided flag before returning; that losing candidate cannot
produce a second whistle/card next frame. A ball already wholly out takes the boundary restart instead.

For an IFK, remember the selected taker until another player physically touches the ball. The taker's
own touches do not lift the indirect restriction. A direct attacking goal becomes the defenders' goal
kick; a direct own goal becomes an opponents' corner. A subsequent other-player touch permits ordinary
goal adjudication. Every restart clears the old handling/indirect history before setting any new taker.

Snapshot body **v27** appends releasing keeper (I32), queued offender (I32), contact XYZ (three F32),
and IFK taker (I32), in that order: 24 bytes. No new avoidance latch is serialized. The existing strict
body version gate refuses older bodies; outer match-save format remains **v3**. Round-trip validation
rejects invalid IDs/non-finite coordinates, a pending offender different from the releasing keeper, or
a nonzero pending position without an offender. Replacing a restricted keeper or indirect taker clears
that player's history rather than transferring it to the incoming substitute.

## 6. Substitutions — request / restart execution (approved October 13, 2026)

The approved deferred-substitution contract (decision record: `match-flow-s6-substitution-contract-draft.md`
v0.3, October 12) supersedes the immediate-effect text previously in this section. The Stage-0 bench
remains `SUBSTITUTES_PER_TEAM = 7`, with `MAX_SUBSTITUTIONS_PER_TEAM = 5` and the existing
`BenchRosterConfig` source; manager requests remain opt-in. Approval is recorded in the owner's
October 13 step-4 instruction. PR #505 implements this contract (October 15, 2026); its final-head Linux
and Unity 6000.4.9f1 evidence is recorded on the PR, and §6.5.1 maps each lock to its test.

### 6.1 Definitions

- **Request:** a manager's substitution order (team, outgoing roster slot, bench index, reason), validated
  and stored. Requesting changes no on-pitch state.
- **Stoppage:** any call of the engine's single restart seam, `ApplyRestart`: throw-in, corner, goal kick,
  free kick (foul or offside), the kickoff after a goal, and the second-half kickoff. The
  engine's restarts are zero-length (the ball is placed and a taker gets possession in the same tick), so
  the stoppage is the restart tick itself. Before the first tick the match has not kicked off; that is
  also a stoppage (§6.2.1).
- **Execution:** applying one request: today's swap effect (§6.2.3).
- **Engine cancellation:** a request removed without execution because it can no longer be executed
  legally, or because the match ended (§6.2.4).
- **Substitution window (L1, decided October 12, 2026):** a stoppage at which at least one of a team's
  requests executes. Under the Laws of the Game (Law 3, five-substitution competitions), a team has at most
  `MAX_SUBSTITUTION_WINDOWS_PER_TEAM = 3` windows during play. Executions before kick-off and at the
  second-half kickoff (half-time) are **free**: they use no window. Any number of a team's requests
  executed at one stoppage use **one** window for that team only. Stage 0 has no extra time, so no
  extra-time window rule is needed yet.

### 6.2 Contract

#### 6.2.1 Request (`RequestSubstitution`)

`RequestSubstitution(teamId, outSlotIndex, benchIndex, reason) → SubstitutionRequestResult`

Request-time checks. Rule refusals return a typed result and **do not throw**, so a manager command cannot
fault the driver. Malformed indices are programming errors and still throw, as today.

| Check | Result |
|---|---|
| `teamId`, `outSlotIndex`, `benchIndex` out of range, or `outSlotIndex` not on `teamId` | throws (unchanged) |
| match ended | `Refused(MatchEnded)` |
| outgoing slot sent off | `Refused(OutgoingSentOff)` |
| outgoing slot already substituted (`_activeBenchSlot != -1`) | `Refused(OutgoingAlreadyReplaced)` |
| a pending request already names this outgoing slot | `Refused(OutgoingAlreadyPending)` |
| bench index already used, or named by a pending request of this team | `Refused(BenchPlayerUnavailable)` |
| `used + pending ≥ MAX_SUBSTITUTIONS_PER_TEAM` for this team | `Refused(LimitReached)` |
| all windows used and the second half has started (no free stoppage remains) | `Refused(NoWindowsLeft)` |
| role rule broken (§6.2.1.1) | `Refused(GoalkeeperRoleMismatch)` |
| otherwise | `Accepted(sequence)` |

On `Accepted`, the request is appended to a FIFO queue with a monotonically increasing per-match
`sequence`, and `_substitutionsUsed` is **not** incremented. If no tick has run yet (pre-kickoff), the
request executes immediately instead (§6.2.3), since the ball is not in play. A request accepted in the
first half after all three windows are used is valid: it waits for the free half-time execution (§6.2.2).

#### 6.2.1.1 Goalkeeper role rule (R1, decided October 12, 2026)

Keeper identity is per roster slot (`_isGoalkeeper`), and the W1/W8 goalkeeper code reads it. Requests
must therefore preserve roles:

- **Normal rule:** a slot whose occupant is a goalkeeper may be replaced only by a bench goalkeeper; an
  outfield slot only by a bench outfielder.
- **Fallback (1) — no keeper on the pitch:** when a team has no un-dismissed goalkeeper on the pitch (today
  that means its keeper was sent off), and no pending request of that team already brings a bench keeper
  on, the team may request **outfield slot off, bench goalkeeper on**. The slot takes the bench keeper's
  goalkeeper flag, exactly as today's swap does. At most one such request may be pending per team. It
  uses a substitution and, outside the free stoppages, a window, like any other.
- **No fallback is available** when the team has no substitutions left, has no window left in the second
  half, or has no goalkeeper on the bench. The team then plays on without a keeper, as it does today. In
  the first half with no window left, the fallback request is accepted and waits for half-time (§6.2.2). The emergency-keeper rule (an
  outfielder already on the pitch takes over in goal) is a recorded follow-up, not part of this contract
  (§6.7).

**Implementation obligations for fallback (1), found at `a17aa82`:**

- `FirstGoalkeeperOnTeam` (the loose-ball collector restriction) does not skip sent-off keepers. With a
  sent-off keeper in a lower slot it would return that slot, not the new keeper. It must skip `_isSentOff`
  slots, as `ResolveGkAgentId` already does.
- Positioning receives `role` from the formation slot (`formation[k].Role`) and `isGoalkeeper` from the
  slot flag. A lock must prove that a keeper brought on into an outfield slot takes up the goalkeeper
  position, not the slot's formation role.
- `RefreshGkAgentIds` already detects a moved keeper slot and resets #11's per-slot state (W1 AR-2), and
  `RoundTrip_KeeperSubstitutedOntoOutfieldSlot` already covers save/restore of a keeper in an outfield slot.

#### 6.2.2 Execution point

At every stoppage, `ApplyRestart` executes the whole queue in `sequence` order **after** the ball is placed
and any goalkeeper hand episode is ended, and **before** `SelectRestartTaker`. An outgoing player therefore
cannot be chosen as the taker, and an incoming player can be. Each request is re-validated at execution
(§6.2.4) because the match may have changed since it was accepted.

Order at each stoppage: first the §6.2.4 cancellation checks run over **every** queued request, for both
teams, whether or not that team can execute now. Then the window gate decides which of the remaining
requests execute. A request held by the gate is therefore never kept alive for a player who has been sent
off, and it never keeps its bench player reserved after cancellation.

Window gate: at a stoppage that is not free, a team's requests execute only if that team has a window
left; executing them uses one window. If the team has none left, its requests stay queued for the free
half-time execution. Each team is gated separately, so one team's execution never uses the other's window.

#### 6.2.3 Execution effect

Each executed request applies today's §6.5 effect unchanged: attributes, performance context, canonical
attributes, player id, Decision Tree/perception projections, `_yellowCards = 0`, `NotifyInterrupt`,
`_activeBenchSlot = benchIndex`, and `_substitutionsUsed[team]++`. The first execution for a team at a
non-free stoppage also does `_substitutionWindowsUsed[team]++` (once per team per stoppage). Position and
velocity are untouched (no re-entry ceremony). The `SubstitutionEvent` (outgoing slot, synthetic incoming id
`SQUAD_SIZE + team × SUBSTITUTES_PER_TEAM + bench`, team, reason) is published **in the tick of execution**
(§6.2.5).

#### 6.2.4 Engine cancellation

At every stoppage, before the window gate (§6.2.2), a queued request whose outgoing slot has been sent off
since acceptance is removed with outcome `NotApplied(OutgoingSentOff)`; the count is unchanged and its bench
player becomes available again. A dismissal is always followed by its own free-kick stoppage, so the
cancellation happens in the tick of the dismissal. Re-validation of bench availability and the limit
cannot fail when §6.2.1 counted pending requests, and an implementation MUST assert this rather than handle
it silently. The role rule (§6.2.1.1) cannot fail at execution either: a keeper cannot return once sent off,
and only one fallback request may be pending. A request held back by the window gate (§6.2.2) is not
cancelled. At full time, every queued request is removed with `NotApplied(MatchEnded)`. Stage 0 has no
in-match injury producer, so `reason = Injury` is a label only; no injury cancels or forces a request.
When an injury producer lands, an injured keeper must reach fallback (1) the same way a sent-off keeper
does (§6.7).

#### 6.2.5 Events and the transient queue

Two queues must not be confused:

- **Request queue (new):** cross-tick state, serialized (§6.3).
- **Event queue (`_pendingSub*`, existing):** a transient publication buffer. **It MUST be empty at every
  tick boundary.** Execution inside the Resolve phase (throw-ins, corners, goal kicks, goals, fouls, offside) runs
  after the top-of-Resolve flush, so it publishes directly (Resolve is the registered producer phase).
  Execution at the second-half kickoff happens earlier in the same tick and is flushed at that tick's
  Resolve. Pre-kickoff execution keeps today's flush at the first tick's Resolve, so its event is
  published within the first tick.

Engine cancellations are observable without a new event type: the live frame and session view expose the
pending queue (entries and their `sequence`), and a cancellation removes the entry. Whether to add a
`SubstitutionCancelledEvent` is option E1 (§6.6).

#### 6.2.6 Client outcomes (mapping to the S0 binding contract)

| Engine outcome | S0 feedback key |
|---|---|
| `Accepted` (ball in play) | `feedback.substitution_pending` / `_pending_paused` |
| executed (`SubstitutionEvent`, with minute) | `feedback.substitution_applied` |
| `Refused(*)` | `feedback.substitution_refused` |
| `NotApplied(MatchEnded)` | `feedback.substitution_not_applied` |
| `NotApplied(OutgoingSentOff)` | `feedback.substitution_not_applied` (cancelled or ended; no new event) |
| command never reached the engine | `feedback.substitution_send_failure` (client-side, unchanged) |

`context.substitution` currently tells the manager that a substitution "does not wait" for a stoppage. That
copy must change in the same landing; it is a UX-document change, not part of this contract.

### 6.3 Determinism and snapshot obligations

- Request queue entries: `sequence (u32)`, `team (u8)`, `outSlot (u8)`, `bench (u8)`, `reason (u8)`.
  Capacity is `2 × MAX_SUBSTITUTIONS_PER_TEAM`, bounded by the §6.2.1 limit check, so it is preallocated and
  allocation-free.
- `_substitutionWindowsUsed[team]` (u8 per team) is cross-tick state and is serialized with the queue.
  `MAX_SUBSTITUTION_WINDOWS_PER_TEAM = 3` is a new `[FIXED]` constant (Laws of the Game, Law 3), beside
  `MAX_SUBSTITUTIONS_PER_TEAM`.
- The queue, the window counts and the next `sequence` value are serialized. This takes the **next free**
  `SNAPSHOT_SCHEMA_VERSION` at implementation time (24 today; whichever of substitutions, W8 B or W8 C
  lands first takes 25). The implementation follows the snapshot-schema-bump procedure: symmetric write/read,
  version pin and a digest probe.
- Restore needs no attribute work for pending entries: they have changed nothing yet. Executed swaps are
  re-projected from `_activeBenchSlot` as today.
- Manager requests enter through the tick-stamped command channel, so replay re-issues each request at its
  original tick. No RNG is drawn; FIFO order is total.

### 6.4 Callers that change

`MatchEngineMutations` / `ManagerCommand` / `MatchClientDriver` and `ui-framework`'s `MatchTacticsDispatcher`
move to `RequestSubstitution` and report its typed result. Whether `SubstitutePlayer` is deleted or
kept as an internal test helper with immediate effect is an implementation choice. If it is kept, no
production caller may reach it. A new engine-internal caller (AI manager, injury) would invalidate the
October 10 census and must re-run it.

### 6.5 Acceptance (the implementation's locks)

1. A request accepted in open play, by a team with a window left (or arriving at a free stoppage), leaves
   every on-pitch field of the outgoing slot unchanged for each tick until the next stoppage, then applies
   exactly once on that restart tick, before taker selection. A request held by the window gate applies
   exactly once at the half-time kickoff instead (lock 12).
2. FIFO: two requests for one team executed at one stoppage apply in `sequence` order. A duplicate
   outgoing slot or bench index is refused at request time.
3. Limit: `used + pending` at the cap refuses with `LimitReached`; counts change only at execution.
4. Dismissal: the outgoing player sent off while the request is pending gives `NotApplied(OutgoingSentOff)`
   at the free-kick restart, and the count is unchanged. This holds also when the request is held by the
   window gate in the first half: it is cancelled at that free kick, not kept until half-time, and its
   bench player can be named by a new request straight away.
5. Full time cancels every pending request with `NotApplied(MatchEnded)`; nothing executes after full time.
6. Pre-kickoff requests execute immediately.
7. Save/restore with a pending request, at the request tick, mid-play and on the stoppage tick, continues
   with identical per-tick digests to an uninterrupted run. The request executes once, never twice or zero
   times.
8. The transient event queue is empty at every tick boundary in a run that executes substitutions at a
   throw-in, a goal kickoff, a foul free kick, an offside free kick and the second-half kickoff.
9. Home and away mirrored versions of 1, 4 and 7.
10. Role rule: keeper-for-outfielder and outfielder-for-keeper requests are refused while the team has a
    keeper on the pitch.
11. Fallback (1): after the team's keeper is sent off, an outfield-slot-off / bench-keeper-on request is
    accepted and executes at the next stoppage when the team has a window left. The new keeper is the
    team's keeper for #11 (`_gkAgentIds`), for the loose-ball collector restriction, and for positioning
    (takes the goalkeeper position). A second fallback request while one is pending is refused. In the
    first half with no window left, the fallback request is accepted, the team plays without a keeper
    until half-time, and the keeper comes on at the half-time kickoff without using a window. With no
    substitution left, no bench keeper, or no window left in the second half, the request is refused and
    the team plays on without a keeper. Home and away mirrored.
12. Windows: three non-free stoppages with executions use three windows; a fourth open-play request in the
    second half is refused with `NoWindowsLeft`; one in the first half waits and executes at half-time
    without using a window. Two requests executed at one stoppage use one window. One team's execution
    does not use the other team's window. Pre-kickoff execution uses none.
13. Save/restore with windows partly used and with a request held by the window gate continues with
    identical per-tick digests.

#### 6.5.1 Lock evidence (PR #505)

Engine locks are in `src/match-engine/tests/MatchEngineDeferredSubstitutionTests.cs` unless another file is named.

| Lock | Test(s) |
|---|---|
| 1 | `OpenPlayRequestChangesNoRosterStateUntilRestart`; `PendingRequestChangesNoOnPitchStateThenIncomingPlayerCanTakeTheRestart` (no on-pitch change against a control run; the incoming player can take the restart); `ExhaustedWindowsExecuteAtHalfTimeButRefuseSecondHalfRequests` (held request) |
| 2 | `TwoRequestsExecuteInSequenceUsingOneTeamWindow`; `TwoRequestsAtOneRealRestartPublishInSequenceOrder`; `DuplicateSlotAndBenchRefusalsLeaveQueueUnchanged` |
| 3 | `LimitCountsPendingRequestsAndOnlyExecutionChangesTheCount`; `LegacySwapCannotInvalidateQueuedSlotBenchOrCapacity` |
| 4 | `DismissedOutgoingCancelsBeforeTheWindowGate`; `DismissalCancelsARequestHeldByTheWindowGate`; `MirroredTeamsExecuteAndCancelOnlyOwnRequests` |
| 5 | `FullTimeRemovesPreviouslyAcceptedUnexecutedRequest`; `FullTimeRejectsRequestsWithTypedResult` |
| 6 | `PreKickoffRequestAppliesImmediatelyWithNoWindow` |
| 7 | Request tick: `MatchSaveManagerTests.SaveAtAcceptedSubstitutionTick_PreservesPendingAndDigestContinuation`, `MatchSessionTests.PausedOffTickSubstitution_SaveRestore_ContinuesIdenticalDigestChain`. Mid-play: `RestoredPendingRequestMatchesEveryContinuationDigestThroughRestart`. Stoppage tick: `SaveOnTheExecutingStoppageTickContinuesIdenticallyAndAppliesOnce` |
| 8 | `ExecutionAtEveryRestartKindPublishesInItsOwnTick` (throw-in, goal kickoff, foul free kick, second-half kickoff); `OffsideFreeKickExecutionLeavesNoBufferedEvent`; `ActualThrowInExecutesOnItsResolveTick` |
| 9 | Every lock-1, -4 and -7 test above that takes a team parameter runs for home and away |
| 10 | `KeeperMismatchRefusedAndSentOffKeeperEnablesBenchKeeperFallback`; `KeeperReplacementRulesRejectOutfielderForKeeperAndSecondFallback` |
| 11 | `FallbackKeeperImmediatelyBecomesGoalkeeperForElevenAndPositioning`; `KeeperReplacementRulesRejectOutfielderForKeeperAndSecondFallback`; `FallbackKeeperHeldByTheGateComesOnFreeAtHalfTime`; `FallbackKeeperIsRefusedWithoutASecondHalfWindowOrASubstitution` |
| 12 | `ThreeUsedWindowsHoldNextFirstHalfRequest`; `TwoRequestsAtTheThirdWindowBothExecuteThere`; `SecondHalfThirdWindowExecutesEveryRequestQueuedForIt`; `ExhaustedWindowsExecuteAtHalfTimeButRefuseSecondHalfRequests`; `TwoRequestsExecuteInSequenceUsingOneTeamWindow`; `MirroredTeamsExecuteAndCancelOnlyOwnRequests`; `PreKickoffRequestAppliesImmediatelyWithNoWindow` |
| 13 | `SaveWithWindowsUsedAndARequestHeldContinuesIdentically` |

`SubstitutePlayer` is kept as an `internal` test helper (§6.4). Within the engine only the pre-kickoff
branch of `RequestSubstitution` calls it, and no production assembly can reach it. Saves between ticks
use the match save format v3 off-tick overlay (`match-save-file-design.md` KD-8).

### 6.6 Owner decisions (October 12, 2026)

| ID | Question | Options | Decision |
|---|---|---|---|
| S-1 | Stoppage set | (a) every `ApplyRestart` incl. free kicks and kickoffs, plus pre-kickoff; (b) only ball-out-of-play restarts and half-time | **(a)**, as recommended. Under the Laws, any stoppage with the referee's permission allows a substitution, and (a) uses the engine's single restart seam |
| R1 | Goalkeeper role | (R1) role-preserving: keeper slot ↔ bench keeper only; outfield ↔ outfield only. (R2) allow any, slot takes the incoming player's keeper flag (today's code, which can field two keepers or none) | **R1, with fallback (1)** (§6.2.1.1): a team with no keeper on the pitch may bring a bench keeper on for an outfielder. The emergency-keeper rule for a team with no substitution, window or bench keeper left is a follow-up (§6.7) |
| W1 | Manager withdrawal of a pending request | (a) none in this slice; (b) add `WithdrawSubstitutionRequest` | **(a)**, as recommended. Withdrawal is recorded as a future item (§6.7) |
| E1 | Cancellation signal | (a) observable through the frame/pending view only; (b) new `SubstitutionCancelledEvent` (event-registry ordinal) | **(a)**, as recommended. (b) only if #37 analytics or the report needs it |
| L1 | Laws substitution-window limit (three opportunities plus half-time) | (a) not modelled (today); (b) model it | **(b), in this slice** (§6.1, §6.2.2). It shares this slice's snapshot schema bump, so it costs no second save-format change |

### 6.7 Recorded follow-ups (not part of this contract)

- **W1 — manager withdrawal of a pending request.** Owner-requested future item. Additive: a new command
  that removes the manager's own pending entry; window and substitution counts are unaffected because
  pending requests use neither.
- **Emergency keeper.** When a team has no keeper on the pitch and fallback (1) is unavailable, an outfield
  player already on the pitch takes over in goal. This is a role change, not a substitution. It needs keeper
  identity to move between on-pitch slots mid-match, which touches #11 and the W8 B keeper queries, so it
  needs its own contract. Until then the team plays without a keeper, as today.
- **Injured keeper.** Stage 0 has no in-match injuries. When an injury producer lands, an injured keeper
  must open fallback (1) and, later, the emergency-keeper rule, the same way a sent-off keeper does; and
  the October 10 census must be re-run (§6.4).
- **UX copy.** `NotApplied(OutgoingSentOff)` has no feedback key and needs one before the landing that
  exposes it (§6.2.6). `NoWindowsLeft` and `GoalkeeperRoleMismatch` fall under the generic
  `feedback.substitution_refused` key; whether they get specific copy, and how windows left are shown, is a
  UX-document decision.

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
| 0.3 | 2026-07-13 | **AR-3 (during implementation prep): 1 finding, fixed.** Verified `MatchEngine` has no working snapshot-decode/restore method anywhere (`SerializeWorldState` is write-only into the digest; every prior "restore-deterministic" note in this file's history means "now serialized, so a future save/load system would reconstruct it," never an actual working restore path). §6's substitution serialization section corrected to stop describing a restore-reconstruction mechanism that doesn't exist — this note only needs to append `_activeBenchSlot`/`_substitutionsUsed` to `SerializeWorldState` like every other cross-tick field this project has ever added. Confirmed `DeterministicRngService.RegisterStream`/`Reserve`/`DrawReserved`/`CloseReservation` call pattern against the real `InteractionTextGenerator` (living-world #22) usage for the new card-severity draw site. Confirmed `RunPhysicsPhase`/`RunResolvePhase` literally start with `EventBus.BeginPhase(...)`. No further findings — proceeding to implementation.
| 0.4 | 2026-07-14 | **AR-4 (caught mid-implementation): 1 finding, fixed.** The half-time "ends-swap" design (mirror every agent + the ball across the pitch) would have broken goal/offside detection for the entire second half — `team 0 attacks +X` is a fixed convention hardcoded in `CheckRestartAndApply`'s scoring-team classification, `OffsideEvaluator.ComputeOffsideLineX`'s `defendsHomeGoal` branch, and every Mechanics-AI `MirrorPitchIfAway` call; mirroring positions without also flipping that convention everywhere would leave team 0 physically in the away half while the engine still thinks team 0 defends x=0. §7 corrected to drop the position mirror entirely — half-time now only resets the ball to centre and publishes `MatchPhaseChangedEvent`; the true ends-swap is a documented Stage-1+ deferral. No other findings this pass — proceeding.
| 0.5 | 2026-07-14 | **AR-5 (caught mid-implementation): 1 finding, fixed.** `SubstitutePlayer` is a public API a caller invokes between `RunTick()` calls, when `EventBus.CurrentPhase` is the post-`OnTickBoundary` `0xFF` sentinel — publishing a `SubstitutionEvent` immediately there throws (`EventBus.cs`'s AR-8 M-2 stale-Publish guard; verified against the real `EventBus.Publish<T>` phase-index check). Fixed: the substitution's STATE EFFECT (attrs/perf/GK-flag swap, bench-slot/count bookkeeping) still applies immediately in `SubstitutePlayer`, but the `SubstitutionEvent` itself is queued into a small fixed-capacity buffer (bounded by `MAX_SUBSTITUTIONS_PER_TEAM * TEAM_COUNT`, so it cannot overflow) and flushed by a new `PublishPendingSubstitutions()` call at the top of `RunResolvePhase`, where `CurrentPhase == Resolve` (the registered producer phase). No other public-API event-publish call in this note has the same hazard — `ApplyFoulIfCaptured`/`CheckRestartAndApply`/`EvaluateAndApplyOffside` all publish from inside `RunResolvePhase` (or `RunFirstTouch`, itself called from `RunResolvePhase`), and `CheckMatchFlowTransitions` publishes from inside `RunInputPhase` immediately after that phase's own `BeginPhase` call — all four already run with a valid matching `CurrentPhase` by construction.
| 0.6 | 2026-07-14 | **AR-6 (code-review round, caught while writing tests): 1 real logic bug in `OffsideEvaluator.ComputeOffsideLineX`, fixed.** The implementation left the min-tracking accumulator at its initial ±Infinity sentinel when fewer than two active defenders exist, rather than the documented/tested `NaN`. Hand-tracing `IsOffside` against that sentinel showed it does NOT naturally resolve to "never offside" as a first-pass self-review had assumed: for a team-1 attacker evaluated against team 0's degenerate (+Infinity) line, `IsOffside`'s `toucherX < offsideLineX` is satisfied by every finite `toucherX` — i.e. it would call the attacker offside **always**, exactly backwards from the intended "too few defenders means no offside is possible" rule (the same inversion hits team-0 attackers against a team-1 degenerate `-Infinity` line). Fixed by adding an explicit `activeCount` tally in `ComputeOffsideLineX` that gates the return to `float.NaN` when fewer than two active (non-sent-off, correct-team) agents were found, restoring the NaN path `IsOffside`'s existing top-of-function NaN guard already handles correctly. Both degenerate-input tests strengthened to assert the ACTUAL `IsOffside` outcome for a concrete attacker position (not just the sentinel value in isolation), which is what surfaced the bug in the first place. No other findings this pass on the offside surface; the reception-time hook in `RunFirstTouch`, the restart primitive, and the foul/card pipeline were re-traced by hand against their own tests and found consistent.
| 1.0 | 2026-07-14 | **Implementation landed; code-adversarial-review cycle closed.** All nine plan items (§3–§7) implemented per the AR-1..AR-6-converged plan above: `RestartResolver.cs` / `OffsideEvaluator.cs` / `SubstitutionReason.cs` (new), three new Tier A events (`OffsideCalledEvent` 0x18 / `RestartAwardedEvent` 0x19 / `MatchPhaseChangedEvent` 0x1A), `MatchEngine.cs` v1.31 (`CheckRestartAndApply`/`ApplyRestart`, `MatchFlowCollisionConsumer`, `ApplyFoulIfCaptured`/`DetermineCardKind`/`ApplyCardAndCheckSentOff`, `EvaluateAndApplyOffside` hooked into `RunFirstTouch`, public `SubstitutePlayer`/`PublishPendingSubstitutions`, `CheckMatchFlowTransitions`), `SNAPSHOT_SCHEMA_VERSION` 14 → 15, and five new test files (`MatchEngineRestartTests`/`MatchEngineOffsideTests`/`MatchEngineFoulCardTests`/`MatchEngineSubstitutionTests`/`MatchEngineMatchFlowTests`) plus `MatchEngineSnapshotSchemaTests` v1.12. Per the driving instruction, the implementation was then itself adversarially reviewed with the same rigor as the plan — multiple rounds reading the ENTIRE touched surface, not just the diff. This surfaced and fixed, beyond the AR-4/AR-5/AR-6 findings already logged above: a stale doc comment on `HALF_TIME_BOUNDARY_TICK` in `MatchEngineConstants.cs` that still described the AR-4-rejected ends-swap; a misleading test comment in `MatchEngineOffsideTests.cs` (`ComputeOffsideLineX_SentOffAndOpponents_Excluded`) that referenced the wrong index as if it were relevant to the excluded set; a latent agent-position/command-drift risk in the foul-card integration test (repositioning an agent via `TestOnly_SetAgent` without also refreshing its held `MovementCommand` would have let the stale boot-time command steer the agent back toward its original position during the same tick's Physics phase, before the foul is applied in Resolve); and a dead `_foulCandidateForce` field carried by the collision consumer but never read (removed, simplifying `TestOnly_InjectFoulCandidate`'s signature). A subsequent full pass — re-reading `RunResolvePhase` end-to-end, all four Mechanics-AI `IsActive` snapshot fill sites, the boot/constructor additions, the v15 serialization block byte-for-byte, all three new event structs against `EventRegistry`'s producer-phase/header-size contract, `RestartResolver`/`OffsideEvaluator` against their own tests, and all five new test files in full — found nothing further. The code-review cycle is closed per the "repeat until no new errors are found" instruction. Tracking docs (root `CLAUDE.md`, `src/CLAUDE.md`, `file-manifest.md`, `match-engine-design.md`) updated in the same pass. Full `dotnet test` gate not runnable in this environment (no SDK access, per this project's own documented precedent for the authoring environment) — verification was by exhaustive manual code review in its place.
| 1.1 | 2026-10-10 | Pointer only: §6 notes that a deferred substitution contract is drafted for owner approval in `match-flow-s6-substitution-contract-draft.md` v0.1. §6 text and the code are unchanged. |
| 1.2 | 2026-10-12 | Pointer only: the §6 note now cites draft v0.3 (owner decisions recorded; revised text awaiting sign-off). §6 text and the code are unchanged. |
| 1.3 | 2026-10-13 | §6 replaced by the approved deferred-substitution contract (`match-flow-s6-substitution-contract-draft.md` v0.3 decisions S-1, R1 with fallback (1), W1, E1, L1), per the owner's October 13 step-4 instruction: request/execution definitions, Law 3 windows, cancellation, events, client outcomes, v25 snapshot obligations and thirteen acceptance locks. Row added October 15 (omitted when the text landed on the PR #505 branch). |
| 1.4 | 2026-10-15 | PR #505 implements §6. The §6 status sentence records the implementation; new §6.5.1 maps each lock to its test, including the locks completed October 15 (1, 2, 3, 4, 7, 8, 11, 13). `SubstitutePlayer` is internal (§6.4). Saves between ticks use match save format v3 (`match-save-file-design.md` KD-8). |
| 1.5 | 2026-10-15 | PR #505 review: the implementation held the second of two requests at a team's third window (the first execution closed the gate), contrary to §6.1. Fixed in `MatchEngine.cs` v2.05; §6.5.1 lock 12 adds `TwoRequestsAtTheThirdWindowBothExecuteThere` and `SecondHalfThirdWindowExecutesEveryRequestQueuedForIt`. §6 text unchanged. |
| 1.6 | 2026-10-16 | Own-release handling avoidance/law restart contract; event codes 5/6, mirrored placement, taker and direct-goal rules, same-frame foul precedence, v27 history; D-07 remains separate. |
