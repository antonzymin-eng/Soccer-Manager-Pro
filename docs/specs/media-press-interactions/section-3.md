# Media & Press Interactions #35 — Section 3: Algorithms

**Created:** July 27, 2026
**Last Updated:** July 27, 2026 (v0.2 — PASS-1 fix pass)
**Version:** 0.2
**Status:** IN REVIEW

---

All arithmetic is **integer** (FR-ME-003). The minimal tier makes **no stochastic draw** (FR-ME-016);
§3.6 is the deep tier's only draw site and is not built at the minimal tier.

## 3.1 `TryQueueConference` — the #30 post-round seam (FM-ME-01)

Invoked after `EmitMatchOutcome(result)`, once per fixture. `input` carries **committed integers** #30
derives from its own `MatchResult` — #35 references no #30 type (FR-ME-006).

```
TryQueueConference(in MediaTriggerInput input, uint worldDay) -> bool:
    # 1. Managed-club gate FIRST — a fixture with no manager has nobody to ask (FR-ME-022).
    if input.ManagedClubId != input.HomeClubId and input.ManagedClubId != input.AwayClubId:
        return false

    # 2. Bounded queue. A full queue DROPS, it does not throw (F7 / FR-ME-023).
    if PendingCount() >= MEDIA_MAX_PENDING_CONFERENCES:
        RecordDroppedConference()                # a recorded branch, so the drop is observable
        return false

    # 3. Archetype selection — a PURE FUNCTION of the trigger at the minimal tier (FR-ME-016).
    #    This is the whole reason the minimal tier is draw-free: there is no choice being made.
    managedIsHome := (input.ManagedClubId == input.HomeClubId)
    ownGoals      := managedIsHome ? input.HomeScore : input.AwayScore
    oppGoals      := managedIsHome ? input.AwayScore : input.HomeScore
    question      := ownGoals >  oppGoals ? MediaIntent.QPostWin
                   : ownGoals == oppGoals ? MediaIntent.QPostDraw
                                          : MediaIntent.QPostLoss

    RequireQuestionBand(question)                # F5 — a question intent MUST be in the question band
    options := OptionRosterFor(question)         # from the authoring catalogue; all in the option band
    RequireOptionBand(options)                   # F5
    RequireCount(options, 1, MEDIA_MAX_OPTIONS)  # F4

    # 4. Enqueue. Deadline is absolute, not a countdown — the KD-3/#53 dated-latch reasoning.
    Append(new PressConference {
        ConferenceId        = cursors.NextConferenceId++,
        QuestionIntent      = question,
        OptionIntents       = options,
        SubjectPlayerId     = MEDIA_NO_SUBJECT,           # a result question has no subject (KD-8)
        TriggerRoundIndex   = input.RoundIndex,
        QueuedWorldDay      = worldDay,
        DeadlineWorldDay    = worldDay + MEDIA_ANSWER_WINDOW_DAYS,
        AnsweredOptionIndex = MEDIA_UNANSWERED })
    return true
```

**Why the managed-club gate is step 1 and not a filter later.** #30 calls this for **every** fixture in a
round — 190 calls in a 20-club round — and all but one must do nothing at all. Placing the gate first
makes the common path a single comparison, and makes *"a non-managed fixture queued a conference"* a
structurally impossible state rather than one caught downstream.

**Why the deadline is an absolute day, not a countdown.** Identical reasoning to #53's KD-3 and #42's
one-shot latch: a countdown must be decremented exactly once per day, which is order-sensitive inside
#30's pinned tick order and wrong after any restore that replays a day boundary. An absolute
`DeadlineWorldDay` is a pure comparison that cannot double-decrement and survives save/restore trivially.

## 3.2 `TryAnswerQuestion` — the command (FM-ME-02)

The one #35 surface a human drives, and the only one where the refuse/throw distinction is load-bearing.

```
TryAnswerQuestion(int conferenceId, int optionIndex) -> bool:
    if not TryFind(conferenceId, out ref PressConference c):
        throw                                    # F2 — unknown id is MALFORMED input

    if c.AnsweredOptionIndex != MEDIA_UNANSWERED:
        return false                             # F3 — a LEGAL race: the expiry sweep may have
                                                 # resolved it between the client's render and the click

    if optionIndex < 0 or optionIndex >= c.OptionIntents.Length:
        throw                                    # F2 — out of THIS conference's own roster

    c.AnsweredOptionIndex := optionIndex
    RecordConsequence(c, optionIndex)            # §3.4
    return true
```

**The two responses are not interchangeable, in either direction.** Throwing on the already-resolved case
would crash a career on an ordinary click, because the race is real and unavoidable: the client renders a
list, the tick expires an entry, the player clicks. Returning `false` on an unknown id would hide a client
bug — a stale or fabricated `conferenceId` is not a race, it is wrong. This is #45's
`TryProjectBoardModifier` distinction (*"not applicable"* is a named legal state; corrupt input still
throws) applied where a human is the caller.

**Note the ordering.** The already-resolved check precedes the option-range check deliberately: a client
holding a stale render may pass an `optionIndex` valid for the *previous* conference state. Checking
resolution first makes that a `false`, not a throw — which is the correct classification of the same race.

## 3.3 `AdvanceMediaDay` — expiry (FM-ME-03)

The daily tick seam, and the only reason #35 takes a tick slot at all (KD-5).

```
AdvanceMediaDay(uint worldDay):
    # F6 guard — the #33 shape, verbatim, implemented from the serialized cursor (§2.2).
    if cursors.LastAdvancedWorldDay != MEDIA_NOT_ADVANCED_SENTINEL:
        if worldDay == cursors.LastAdvancedWorldDay:      return       # no-op
        if worldDay != cursors.LastAdvancedWorldDay + 1:  throw        # day gap

    foreach c in Pending():                       # bounded by MEDIA_MAX_PENDING_CONFERENCES
        if worldDay < c.DeadlineWorldDay:  continue
        idx := IndexOfNoCommentOption(c)          # a DEFINED answer, not a special case
        c.AnsweredOptionIndex := idx
        RecordConsequence(c, idx)                 # §3.4 — expiry produces a consequence

    cursors.LastAdvancedWorldDay := worldDay      # stamp LAST, so a throw above leaves the day retryable
```

**Expiry resolves to a real answer, not to a null state.** The no-comment option is an ordinary member of
the conference's own option roster with its own consequence (frequently `0`, which by FR-ME-033 records
nothing). That is what makes KD-9's identity precondition *"every consequence `0`"* rather than *"no
conference answered"* — expiry answers conferences, and a spec that forgets this states a weaker
precondition than the one that holds.

**Why #35 carries the F6 guard when #53 does not.** #53's advance is idempotent by construction (its
completion clears the in-progress record). #35's is **not**: `Pending()` shrinks as conferences resolve,
so a re-run is harmlessly empty — but a **day gap** would silently expire conferences whose deadline fell
inside the skipped range **on the wrong day**, stamping consequences with a `RecordedWorldDay` that never
happened. The guard is therefore load-bearing here and ceremony there, which is why each spec argues its
own case rather than copying the other's.

**Stamp-last** matters: `RecordConsequence` can throw (F4, an authoring bug), and a cursor stamped before
it would silently consume the day.

## 3.4 `RecordConsequence` and `TryTakePendingDelta` — the KD-3 routed value (FM-ME-04)

```
RecordConsequence(in PressConference c, int optionIndex):
    entries := ConsequenceFor(c.QuestionIntent, c.OptionIntents[optionIndex])   # authoring catalogue
    RequireCount(entries, 0, MEDIA_MAX_CONSEQUENCE_TARGETS)                     # F4

    foreach e in entries:
        RequireInRange(e.DeltaPermille, MEDIA_DELTA_MIN, MEDIA_DELTA_MAX)       # F4
        if e.DeltaPermille == 0:  continue        # FR-ME-033 — a zero delta is NEVER stored
        RequireTargetKindSupported(e.TargetKind)  # Player only at minimal (KD-8)
        Append(new PendingDelta { e.TargetKind, e.TargetId, e.DeltaPermille, RecordedWorldDay = today })

TryTakePendingDelta(int playerId, out int deltaPermille) -> bool:
    if not TryFindPending(TargetKind.Player, playerId, out i):
        deltaPermille := 0; return false          # NOT an error — #30 asks for EVERY player
    deltaPermille := pending[i].DeltaPermille
    RemoveAt(i)                                   # cleared ON DELIVERY -> exactly-once (FR-ME-034)
    return true
```

**Zero deltas are dropped at the point of recording, not filtered at delivery.** The difference matters:
without it, every expired conference in a 38-round season leaves an inert row in an **APPEND-only** blob,
and *"is there a delta pending for this player?"* stops being answerable by presence. This is #44's
canonical `(0,0)`-entry drop applied to the same problem.

**`TryTakePendingDelta` takes no `worldDay`, and that is deliberate.** It drains whatever is pending
regardless of the day it was recorded on — which is exactly what makes delivery robust across a
save/restore or a multi-day jump. A day-matched drain would strand a delta recorded on a day the loop
subsequently skipped, and the `RecordedWorldDay` field would then be load-bearing for correctness rather
than for auditability, which is not what it is for.

**Exactly-once is a property of clearing at delivery, and it is what the blob exists to protect.** A save
taken between the answer and the next step 3 carries the pending row; the restored run drains it once. A
second step 3 on the same day — #33's own F6 no-op case — finds nothing and cannot re-apply it.

**The drop-on-departure rule** (FR-ME-035 / F9) runs at the same season boundary #33 drops a player's
entries: any `PendingDelta` whose `TargetId` is no longer on the managed roster is **removed**, never
migrated. Without it an undelivered delta for a retiring player is immortal in an APPEND-only blob
(nothing iterates him at step 3), and one for a transferred player could be delivered to **whoever now
holds that re-keyed id** — #31's KD-7 re-key makes that a real, silent mis-delivery rather than a
hypothetical.

## 3.5 `SelectionValue` — the FR-LC-004 `ulong` without a draw (FM-ME-05)

```
SelectionValue(MediaIntent intent, uint worldDay, int subjectId, int purpose) -> ulong:
    RequireRenderableIntent(intent)               # F1 — FR-ME-011: gate on the VALUE, BEFORE any work
    require 0 <= purpose < MEDIA_PURPOSE_RADIX

    z := MEDIA_SELECTION_SEED
    z := SplitMix64Step(z ^ (ulong)(uint)(int)intent)
    z := SplitMix64Step(z ^ (ulong)worldDay)
    z := SplitMix64Step(z ^ (ulong)(uint)(subjectId + 1))   # +1 so MEDIA_NO_SUBJECT (-1) maps to 0
    z := SplitMix64Step(z ^ (ulong)(uint)purpose)
    return z
```

**This is a keyed mix, not a draw.** It reads no cursor, advances no stream, and is a **pure function of
its arguments** — so it is position-independent, **nothing is serialized**, and replay needs no cursor
(FR-ME-017). The precedent is in-tree and explicit: `FixtureScheduler` and `LeagueBootstrap` each carry a
**local** SplitMix64 rather than allocate a domain tag, which is exactly why `DOMAIN_TAG_SEASON_LOOP`
stayed pinned to #30 T2's first *real* draw site.

**The `subjectId + 1` shift is not cosmetic.** `MEDIA_NO_SUBJECT` is `-1`, and mixing `(ulong)(uint)(-1)`
= `0xFFFFFFFF` would collide with a hypothetical `subjectId` of `4294967295` and, more practically, would
make the subject-less case the *most* extreme input to the mix rather than the most neutral. The shift maps
`-1 → 0` and every real `PlayerId ≥ 0` to `≥ 1`, keeping the domain contiguous.

**This function is conditional on ERR-049-001** (§1.4(c)). If #49 declines to generalize FR-LC-020, #35
supplies `SelectionValue = 0` instead (FR-ME-018) — FR-LC-007's `variant = draw % variantCount` is total
at `draw = 0`, so every intent renders variant `0`: phrasing variety is lost at the minimal tier and
returns at the deep tier's real draw. #35 does **not** register a stream to satisfy FR-LC-020 literally,
because that manufactures a stochastic surface for a decision with no randomness in it.

## 3.6 The deep-tier archetype draw (FM-ME-06, deferred)

Specified here so the determinism contract is reviewable now; **not built at the minimal tier**
(FR-ME-016).

```
SelectArchetypeDeep(in MediaTriggerInput input, uint worldDay, int subjectId) -> MediaIntent:
    ordinal := DeriveActionOrdinal(subjectId, worldDay, DRAW_PURPOSE_ARCHETYPE)
    roll    := DrawKeyed(mediaStream, ordinal, candidateCount)
    return candidates[roll]

DeriveActionOrdinal(int subjectId, uint worldDay, int purpose) -> u64:
    require 0 <= purpose  < MEDIA_PURPOSE_RADIX          # bound guard
    require 0 <= subjectId < MEDIA_SUBJECT_STRIDE        # injectivity guard — WITHOUT this, an
                                                         # out-of-stride subject silently ALIASES onto
                                                         # another subject's ordinal: same question,
                                                         # no error, no divergence signal
    return ((u64)worldDay * MEDIA_PURPOSE_RADIX + purpose) * MEDIA_SUBJECT_STRIDE + (u64)subjectId
```

`MEDIA_PURPOSE_RADIX` is **fixed**, never *"the current purpose count"* — a growing radix re-keys every
historical ordinal the moment a purpose is appended, breaking cross-version replay parity (#41's finding,
adopted here and by #45). The ordinal is a pure function of its arguments, so the draw is
**position-independent** and there is consequently **no cursor to persist**.

**One stream, once** (FR-ME-019): `media.selection` with a fixed entity sentinel, registered a single
time regardless of club or player count. `RegisterStream` appends into a bounded, never-shrinking table
(`MaxRngStreams = 64`, no unregister), and #42 §7.4 R-1 records that a per-entity model exhausts it across
a full-world career.

## 3.7 Arithmetic convention (pinned)

Every expression in §3.1–§3.4 is exact integer arithmetic — comparison, addition, and array indexing only.
**#35 performs no division outside §3.5's SplitMix64 and §3.6's `DrawKeyed` modulo**, both of which operate
on `ulong` where the semantics are unambiguous. No signed division and no rounding arises anywhere, so no
rounding convention is needed — and none may be introduced without a spec change, since `Math.Round`
operates on `double` and would violate FR-ME-003 outright.

## 3.8 Worked examples (hand-verifiable)

At `MEDIA_MAX_PENDING_CONFERENCES = 8`, `MEDIA_ANSWER_WINDOW_DAYS = 3`, `MEDIA_UNANSWERED = -1`,
`MEDIA_NO_SUBJECT = -1`.

| # | Setup | Working | Result |
|---|---|---|---|
| (a) | Managed club is home, wins 2–1, day 100 | `managedIsHome`; `own = 2 > opp = 1` | `QPostWin` queued, `DeadlineWorldDay = 103` |
| (b) | Managed club is **away**, loses 2–1, day 100 | `own = 1`, `opp = 2` | `QPostLoss` — the away mirror, which is the case a home-only fixture would miss |
| (c) | Managed club is away, draws 1–1 | `own == opp` | `QPostDraw` |
| (d) | A fixture between two non-managed clubs | step 1 gate | `false` — **nothing queued**, no cursor advanced |
| (e) | Queue already holds 8 pending | step 2 | `false` + a recorded drop — **no throw** (F7) |
| (f) | `TryAnswerQuestion(id, 1)` on a pending conference with 4 options | in range, unanswered | `true`; consequence recorded |
| (g) | Same call repeated | `AnsweredOptionIndex != -1` | **`false`** — a legal race, no throw (F3) |
| (h) | `TryAnswerQuestion(id, 9)` on the same 4-option conference | out of roster | **throws** (F2) |
| (i) | `TryAnswerQuestion(999, 0)`, no such conference | unknown id | **throws** (F2) |
| (j) | (a) unanswered, `AdvanceMediaDay(102)` | `102 < 103` | still pending |
| (k) | (a) unanswered, `AdvanceMediaDay(103)` | `103 >= 103` | resolved to the no-comment option; if its delta is `0`, **no `PendingDelta` row is written** |
| (l) | `AdvanceMediaDay(103)` twice | second call: `worldDay == LastAdvanced` | **no-op** |
| (m) | `AdvanceMediaDay(103)` then `AdvanceMediaDay(110)` | `110 != 104` | **throws** (F6) — unlike #53, a gap here is an error (§3.3) |
| (n) | Answer with delta `−120` for player 7 on day 103; step 3 on day 104 | drain finds the row, returns `−120`, removes it | delivered **once**; a second step-3 call the same day returns `false` |
| (o) | (n) but a save is taken on day 103 after the answer | the row is in the blob (KD-7) | restored run delivers `−120` on day 104 — **exactly once**, not zero or twice |
| (p) | (n) but player 7 retires at the season boundary before delivery | drop-on-departure (F9) | the row is **removed**; the blob does not grow |
| (q) | `SelectionValue(QPostWin, 100, MEDIA_NO_SUBJECT, 0)` | `subjectId + 1 = 0` | a defined `ulong`; the subject-less case is the mix's *neutral* input, not its most extreme (§3.5) |

Examples (g)/(h)/(i) are the three that a single uniform error policy would get wrong — two of them by
throwing on a legal race, one by silently accepting a client bug. Example (m) is the one that differs from
#53 by design, and (o) is the one the sub-blob exists for.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-27 | — | Initial §3 (FM-ME-01..06: queue, answer, expiry, consequence + drain, the keyed selection value, the deferred deep draw; arithmetic convention; worked examples) from supplement v0.7. Status IN REVIEW. |
| 0.2 | 2026-07-27 | — | PASS-1 fixes. **M:** §3.2's **check ordering** pinned — the already-resolved test must precede the option-range test, because a client holding a stale render may pass an index valid for the previous state, and that is the same race F3 classifies as legal (v0.1 left the order unstated, and the natural validate-args-first ordering would have thrown). **M:** §3.5's **`subjectId + 1` shift** added — `MEDIA_NO_SUBJECT = -1` mixed as `0xFFFFFFFF` would make the subject-less case the most extreme input to the mix and collide with a maximal `subjectId`. **M:** §3.3 now argues **why #35 needs the F6 day-gap guard when #53 does not** — a gap here would expire conferences on the wrong day and stamp consequences with a `RecordedWorldDay` that never happened, so the guard is load-bearing here and ceremony there; copying either spec's posture into the other would be wrong. **L:** the managed-club gate moved to step 1 with its 190-calls-per-round rationale; the absolute-deadline reasoning cross-referenced to the same dated-latch argument #53 and #42 use; away-mirror worked examples (b)/(c) added — the #8 ERR-008-002 *"every example used the home team"* defect class; §3.7 arithmetic convention added. |
#endregion
