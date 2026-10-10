# Match Flow §6 — Deferred Substitution Contract (DRAFT for owner approval)

> **Created:** October 10, 2026
> **Purpose:** Proposed replacement for `match-flow-completion-design.md` §6 so that substitutions follow the
> owner rule of September 25, 2026: a manager may request a substitution while the ball is in play, and it is
> executed only at the next stoppage. Roadmap step 4 requires this approved contract before implementation.
> **Status:** v0.1 **DRAFT — NOT APPROVED.** Nothing here is in force. §6 and the code keep today's
> immediate-effect behaviour until the owner approves this text (with or without the §7 options) and it is
> folded into §6. **No code, schema, event or `[GT]` change is made by this document.**
> **Code facts verified at:** `2a396d5` (`MatchEngine.cs` v1.96, `SNAPSHOT_SCHEMA_VERSION = 24`).

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
| role mismatch (§7, option R1 only) | `Refused(GoalkeeperRoleMismatch)` |
| otherwise | `Accepted(sequence)` |

On `Accepted`, the request is appended to a FIFO queue with a monotonically increasing per-match
`sequence`, and `_substitutionsUsed` is **not** incremented. If no tick has run yet (pre-kickoff), the
request executes immediately instead (§3.3), since the ball is not in play.

### 3.2 Execution point

At every stoppage, `ApplyRestart` executes the whole queue in `sequence` order **after** the ball is placed
and any goalkeeper hand episode is ended, and **before** `SelectRestartTaker`. An outgoing player therefore
cannot be chosen as the taker, and an incoming player can be. Each request is re-validated at execution
(§3.4) because the match may have changed since it was accepted.

### 3.3 Execution effect

Each executed request applies today's §6 effect unchanged: attributes, performance context, canonical
attributes, player id, Decision Tree/perception projections, `_yellowCards = 0`, `NotifyInterrupt`,
`_activeBenchSlot = benchIndex`, and `_substitutionsUsed[team]++`. Position and velocity are untouched (no
re-entry ceremony). The `SubstitutionEvent` (outgoing slot, synthetic incoming id
`SQUAD_SIZE + team × SUBSTITUTES_PER_TEAM + bench`, team, reason) is published **in the tick of execution**
(§3.5).

### 3.4 Engine cancellation

At execution, a request whose outgoing slot has been sent off since acceptance is removed with outcome
`NotApplied(OutgoingSentOff)`; the count is unchanged. Re-validation of bench availability and the limit
cannot fail when §3.1 counted pending requests, and an implementation MUST assert this rather than handle
it silently. At full time, every queued request is removed with `NotApplied(MatchEnded)`. Stage 0 has no
in-match injury producer, so `reason = Injury` is a label only; no injury cancels or forces a request.

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
- The queue and the next `sequence` value are serialized. This takes the **next free**
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

1. A request accepted in open play leaves every on-pitch field of the outgoing slot unchanged for each
   tick until the next stoppage, then applies exactly once on that restart tick, before taker selection.
2. FIFO: two requests for one team executed at one stoppage apply in `sequence` order. A duplicate
   outgoing slot or bench index is refused at request time.
3. Limit: `used + pending` at the cap refuses with `LimitReached`; counts change only at execution.
4. Dismissal: the outgoing player sent off while the request is pending gives `NotApplied(OutgoingSentOff)`
   at the free-kick restart, and the count is unchanged.
5. Full time cancels every pending request with `NotApplied(MatchEnded)`; nothing executes after full time.
6. Pre-kickoff requests execute immediately.
7. Save/restore with a pending request, at the request tick, mid-play and on the stoppage tick, continues
   with identical per-tick digests to an uninterrupted run. The request executes once, never twice or zero
   times.
8. The transient event queue is empty at every tick boundary in a run that executes substitutions at a
   throw-in, a goal kickoff, a foul free kick, an offside free kick and the second-half kickoff.
9. Home and away mirrored versions of 1, 4 and 7.
10. Option R1 only: keeper-for-outfielder and outfielder-for-keeper requests are refused.

## 7. Decisions for the owner

| ID | Question | Options | Recommendation |
|---|---|---|---|
| S-1 | Stoppage set | (a) every `ApplyRestart` incl. free kicks and kickoffs, plus pre-kickoff; (b) only ball-out-of-play restarts and half-time | **(a)**. Under the Laws, any stoppage with the referee's permission allows a substitution, and (a) uses the engine's single restart seam |
| R1 | Goalkeeper role | (R1) role-preserving: keeper slot ↔ bench keeper only; outfield ↔ outfield only. (R2) allow any, slot takes the incoming player's keeper flag (today's code, which can field two keepers or none) | **R1** for Stage 0, because keeper identity is slot-based (W8 B keeper queries). Revisit with player-role identity |
| W1 | Manager withdrawal of a pending request | (a) none in this slice; (b) add `WithdrawSubstitutionRequest` | **(a)**. The S0 UX has no withdrawal for submitted requests; adding one later is additive |
| E1 | Cancellation signal | (a) observable through the frame/pending view only; (b) new `SubstitutionCancelledEvent` (event-registry ordinal) | **(a)** for the first slice. (b) only if #37 analytics or the report needs it |
| L1 | Laws substitution-window limit (three opportunities plus half-time) | (a) not modelled (today); (b) model it | **(a)**. Executing a FIFO batch at one stoppage counts as one opportunity if it is modelled later |

## Version history

| Version | Date | Change |
|---|---|---|
| v0.1 | October 10, 2026 | Draft for owner approval: request/stoppage/execution/cancellation contract, snapshot obligations, client outcome mapping, acceptance locks and five decision points. Not in force. |
