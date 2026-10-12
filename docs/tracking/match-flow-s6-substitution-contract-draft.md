# Match Flow §6 — Deferred Substitution Contract (DRAFT for owner approval)

> **Created:** October 10, 2026
> **Purpose:** Proposed replacement for `match-flow-completion-design.md` §6 so that substitutions follow the
> owner rule of September 25, 2026: a manager may request a substitution while the ball is in play, and it is
> executed only at the next stoppage. Roadmap step 4 requires this approved contract before implementation.
> **Status:** v0.3 **DRAFT — owner decisions recorded October 12, 2026 (§7); revised text awaiting owner
> sign-off.** Nothing here is in force. §6 and the code keep today's immediate-effect behaviour until the
> owner signs off this revised text and it is folded into §6. **No code, schema, event or `[GT]` change is
> made by this document.**
> **Code facts verified at:** `2a396d5` (`MatchEngine.cs` v1.96, `SNAPSHOT_SCHEMA_VERSION = 24`); the v0.2
> goalkeeper-fallback facts (§3.1.1) re-verified at `a17aa82` (`SNAPSHOT_SCHEMA_VERSION` still 24).

## 1. Problem

`MatchEngine.SubstitutePlayer` swaps the outgoing slot's attributes, goalkeeper flag, player id, yellow
cards and Decision Tree plan at the moment of the call; only the `SubstitutionEvent` is deferred, through the
transient `_pendingSub*` queue flushed at the top of the next Resolve phase. `MatchClientDriver.Service`
drains manager commands at the top of the next tick and applies them at once. The result is a roster
replacement in open play, which the owner rule forbids (`open-issues.md`, "Substitutions take effect
mid-play"). The census of October 10 found no frozen measurement driver that requests a substitution, so
this change is not a pre-B prerequisite.

## 2. Definitions

- **Request:** a manager's substitution order (team, outgoing roster slot, bench index, reason), validated
  and stored. Requesting changes no on-pitch state.
- **Stoppage:** any call of the engine's single restart seam, `ApplyRestart`: throw-in, corner, goal kick,
  free kick (foul or offside), the kickoff after a goal, and the second-half kickoff. The
  engine's restarts are zero-length (the ball is placed and a taker gets possession in the same tick), so
  the stoppage is the restart tick itself. Before the first tick the match has not kicked off; that is
  also a stoppage (§3.1).
- **Execution:** applying one request: today's swap effect (§3.3).
- **Engine cancellation:** a request removed without execution because it can no longer be executed
  legally, or because the match ended (§3.4).
- **Substitution window (L1, decided October 12, 2026):** a stoppage at which at least one of a team's
  requests executes. Under the Laws of the Game (Law 3, five-substitution competitions), a team has at most
  `MAX_SUBSTITUTION_WINDOWS_PER_TEAM = 3` windows during play. Executions before kick-off and at the
  second-half kickoff (half-time) are **free**: they use no window. Any number of a team's requests
  executed at one stoppage use **one** window for that team only. Stage 0 has no extra time, so no
  extra-time window rule is needed yet.

## 3. Contract

### 3.1 Request (`RequestSubstitution`)

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
| role rule broken (§3.1.1) | `Refused(GoalkeeperRoleMismatch)` |
| otherwise | `Accepted(sequence)` |

On `Accepted`, the request is appended to a FIFO queue with a monotonically increasing per-match
`sequence`, and `_substitutionsUsed` is **not** incremented. If no tick has run yet (pre-kickoff), the
request executes immediately instead (§3.3), since the ball is not in play. A request accepted in the
first half after all three windows are used is valid: it waits for the free half-time execution (§3.2).

### 3.1.1 Goalkeeper role rule (R1, decided October 12, 2026)

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
  the first half with no window left, the fallback request is accepted and waits for half-time (§3.2). The emergency-keeper rule (an
  outfielder already on the pitch takes over in goal) is a recorded follow-up, not part of this contract
  (§8).

**Implementation obligations for fallback (1), found at `a17aa82`:**

- `FirstGoalkeeperOnTeam` (the loose-ball collector restriction) does not skip sent-off keepers. With a
  sent-off keeper in a lower slot it would return that slot, not the new keeper. It must skip `_isSentOff`
  slots, as `ResolveGkAgentId` already does.
- Positioning receives `role` from the formation slot (`formation[k].Role`) and `isGoalkeeper` from the
  slot flag. A lock must prove that a keeper brought on into an outfield slot takes up the goalkeeper
  position, not the slot's formation role.
- `RefreshGkAgentIds` already detects a moved keeper slot and resets #11's per-slot state (W1 AR-2), and
  `RoundTrip_KeeperSubstitutedOntoOutfieldSlot` already covers save/restore of a keeper in an outfield slot.

### 3.2 Execution point

At every stoppage, `ApplyRestart` executes the whole queue in `sequence` order **after** the ball is placed
and any goalkeeper hand episode is ended, and **before** `SelectRestartTaker`. An outgoing player therefore
cannot be chosen as the taker, and an incoming player can be. Each request is re-validated at execution
(§3.4) because the match may have changed since it was accepted.

Order at each stoppage: first the §3.4 cancellation checks run over **every** queued request, for both
teams, whether or not that team can execute now. Then the window gate decides which of the remaining
requests execute. A request held by the gate is therefore never kept alive for a player who has been sent
off, and it never keeps its bench player reserved after cancellation.

Window gate: at a stoppage that is not free, a team's requests execute only if that team has a window
left; executing them uses one window. If the team has none left, its requests stay queued for the free
half-time execution. Each team is gated separately, so one team's execution never uses the other's window.

### 3.3 Execution effect

Each executed request applies today's §6 effect unchanged: attributes, performance context, canonical
attributes, player id, Decision Tree/perception projections, `_yellowCards = 0`, `NotifyInterrupt`,
`_activeBenchSlot = benchIndex`, and `_substitutionsUsed[team]++`. The first execution for a team at a
non-free stoppage also does `_substitutionWindowsUsed[team]++` (once per team per stoppage). Position and
velocity are untouched (no re-entry ceremony). The `SubstitutionEvent` (outgoing slot, synthetic incoming id
`SQUAD_SIZE + team × SUBSTITUTES_PER_TEAM + bench`, team, reason) is published **in the tick of execution**
(§3.5).

### 3.4 Engine cancellation

At every stoppage, before the window gate (§3.2), a queued request whose outgoing slot has been sent off
since acceptance is removed with outcome `NotApplied(OutgoingSentOff)`; the count is unchanged and its bench
player becomes available again. A dismissal is always followed by its own free-kick stoppage, so the
cancellation happens in the tick of the dismissal. Re-validation of bench availability and the limit
cannot fail when §3.1 counted pending requests, and an implementation MUST assert this rather than handle
it silently. The role rule (§3.1.1) cannot fail at execution either: a keeper cannot return once sent off,
and only one fallback request may be pending. A request held back by the window gate (§3.2) is not
cancelled. At full time, every queued request is removed with `NotApplied(MatchEnded)`. Stage 0 has no
in-match injury producer, so `reason = Injury` is a label only; no injury cancels or forces a request.
When an injury producer lands, an injured keeper must reach fallback (1) the same way a sent-off keeper
does (§8).

### 3.5 Events and the transient queue

Two queues must not be confused:

- **Request queue (new):** cross-tick state, serialized (§4).
- **Event queue (`_pendingSub*`, existing):** a transient publication buffer. **It MUST be empty at every
  tick boundary.** Execution inside the Resolve phase (throw-ins, corners, goal kicks, goals, fouls, offside) runs
  after the top-of-Resolve flush, so it publishes directly (Resolve is the registered producer phase).
  Execution at the second-half kickoff happens earlier in the same tick and is flushed at that tick's
  Resolve. Pre-kickoff execution keeps today's flush at the first tick's Resolve, so its event is
  published within the first tick.

Engine cancellations are observable without a new event type: the live frame and session view expose the
pending queue (entries and their `sequence`), and a cancellation removes the entry. Whether to add a
`SubstitutionCancelledEvent` is option E1 (§7).

### 3.6 Client outcomes (mapping to the S0 binding contract)

| Engine outcome | S0 feedback key |
|---|---|
| `Accepted` (ball in play) | `feedback.substitution_pending` / `_pending_paused` |
| executed (`SubstitutionEvent`, with minute) | `feedback.substitution_applied` |
| `Refused(*)` | `feedback.substitution_refused` |
| `NotApplied(MatchEnded)` | `feedback.substitution_not_applied` |
| `NotApplied(OutgoingSentOff)` | **no current key.** UX copy decision, outside this engine contract |
| command never reached the engine | `feedback.substitution_send_failure` (client-side, unchanged) |

`context.substitution` currently tells the manager that a substitution "does not wait" for a stoppage. That
copy must change in the same landing; it is a UX-document change, not part of this contract.

## 4. Determinism and snapshot obligations

- Request queue entries: `sequence (u32)`, `team (u8)`, `outSlot (u8)`, `bench (u8)`, `reason (u8)`.
  Capacity is `2 × MAX_SUBSTITUTIONS_PER_TEAM`, bounded by the §3.1 limit check, so it is preallocated and
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

## 5. Callers that change

`MatchEngineMutations` / `ManagerCommand` / `MatchClientDriver` and `ui-framework`'s `MatchTacticsDispatcher`
move to `RequestSubstitution` and report its typed result. Whether `SubstitutePlayer` is deleted or
kept as an internal test helper with immediate effect is an implementation choice. If it is kept, no
production caller may reach it. A new engine-internal caller (AI manager, injury) would invalidate the
October 10 census and must re-run it.

## 6. Acceptance (the implementation's locks)

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

## 7. Owner decisions (October 12, 2026)

| ID | Question | Options | Decision |
|---|---|---|---|
| S-1 | Stoppage set | (a) every `ApplyRestart` incl. free kicks and kickoffs, plus pre-kickoff; (b) only ball-out-of-play restarts and half-time | **(a)**, as recommended. Under the Laws, any stoppage with the referee's permission allows a substitution, and (a) uses the engine's single restart seam |
| R1 | Goalkeeper role | (R1) role-preserving: keeper slot ↔ bench keeper only; outfield ↔ outfield only. (R2) allow any, slot takes the incoming player's keeper flag (today's code, which can field two keepers or none) | **R1, with fallback (1)** (§3.1.1): a team with no keeper on the pitch may bring a bench keeper on for an outfielder. The emergency-keeper rule for a team with no substitution, window or bench keeper left is a follow-up (§8) |
| W1 | Manager withdrawal of a pending request | (a) none in this slice; (b) add `WithdrawSubstitutionRequest` | **(a)**, as recommended. Withdrawal is recorded as a future item (§8) |
| E1 | Cancellation signal | (a) observable through the frame/pending view only; (b) new `SubstitutionCancelledEvent` (event-registry ordinal) | **(a)**, as recommended. (b) only if #37 analytics or the report needs it |
| L1 | Laws substitution-window limit (three opportunities plus half-time) | (a) not modelled (today); (b) model it | **(b), in this slice** (§2, §3.2). It shares this slice's snapshot schema bump, so it costs no second save-format change |

## 8. Recorded follow-ups (not part of this contract)

- **W1 — manager withdrawal of a pending request.** Owner-requested future item. Additive: a new command
  that removes the manager's own pending entry; window and substitution counts are unaffected because
  pending requests use neither.
- **Emergency keeper.** When a team has no keeper on the pitch and fallback (1) is unavailable, an outfield
  player already on the pitch takes over in goal. This is a role change, not a substitution. It needs keeper
  identity to move between on-pitch slots mid-match, which touches #11 and the W8 B keeper queries, so it
  needs its own contract. Until then the team plays without a keeper, as today.
- **Injured keeper.** Stage 0 has no in-match injuries. When an injury producer lands, an injured keeper
  must open fallback (1) and, later, the emergency-keeper rule, the same way a sent-off keeper does; and
  the October 10 census must be re-run (§5).
- **UX copy.** `NotApplied(OutgoingSentOff)` has no feedback key and needs one before the landing that
  exposes it (§3.6). `NoWindowsLeft` and `GoalkeeperRoleMismatch` fall under the generic
  `feedback.substitution_refused` key; whether they get specific copy, and how windows left are shown, is a
  UX-document decision.

## Version history

| Version | Date | Change |
|---|---|---|
| v0.1 | October 10, 2026 | Draft for owner approval: request/stoppage/execution/cancellation contract, snapshot obligations, client outcome mapping, acceptance locks and five decision points. Not in force. |
| v0.2 | October 12, 2026 | Owner decisions recorded: S-1 (a), R1 with a no-keeper-on-the-pitch fallback (§3.1.1), W1 (a) with withdrawal as a future item, E1 (a), L1 (b) modelled in this slice (§2, §3.2, `NoWindowsLeft`, window counts serialized). Adds fallback implementation obligations found at `a17aa82`, locks 10–13 and §8 follow-ups (withdrawal, emergency keeper, injured keeper, UX copy). Revised text awaits owner sign-off; not in force. |
| v0.3 | October 12, 2026 | PR #501 review: §3.2/§3.4 run dismissal cancellation over every queued request at each stoppage **before** the window gate, so a held request never outlives its player or keeps a bench player reserved. Locks 1, 4 and 11 now cover requests held by the window gate, including the keeper fallback waiting until half-time. Not in force. |
