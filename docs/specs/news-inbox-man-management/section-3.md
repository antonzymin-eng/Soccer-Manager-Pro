# News, Inbox & Man-Management #46 — Section 3: Algorithms

**Created:** July 27, 2026
**Last Updated:** July 27, 2026 (v0.1 — initial section-file set)
**Version:** 0.1
**Status:** IN REVIEW

---

All arithmetic is **integer** (FR-NW-003), and **no formula below makes a stochastic draw at any tier**
(FR-NW-034) — #46 has no draw site to specify, at minimal *or* deep. That is the difference from #35,
whose deep tier genuinely draws.

## 3.1 `Append` — the only write-in (FM-NW-01)

Called by a root-side projector at **its producer's own pinned site** (KD-2). #46 assigns the id.

```
Append(SourceTag src, ItemKind kind, uint worldDay, int subjectId, in int[] payload) -> int:
    RequireDefined(src)                                    # F1
    RequireDefined(kind)                                   # F1
    RequireLength(payload, PayloadArityOf(src, kind))       # F2 — the schema is keyed on the PAIR

    # 1. Bound the log. A full log drops the OLDEST, never refuses the newest (F6 / FR-NW-014).
    while ItemCount() >= INBOX_MAX_ITEMS:
        RemoveOldestByTotalOrder()                          # (WorldDay, SourceTag, ItemId)

    # 2. Allocate from THIS source's cursor. One allocator, owned by #46 (FR-NW-012).
    id := cursors.NextItemIdBySource[(int)src]++
    Insert(new InboxItem { src, kind, id, worldDay, subjectId, Copy(payload) })

    # 3. Compact dead read keys HERE -- at a write that is already happening (FR-NW-019 / KD-6).
    CompactExplicitReadKeys()
    return id
```

**Why the id is #46's to assign, not the caller's.** With `InboxCursors` holding the allocator and the
caller also supplying an id there would be **two id sources and no rule** — and the failure is silent:
duplicate ids break the `(WorldDay, SourceTag, ItemId)` total order's tie-freedom (FR-NW-013), which is
what makes item order canonical across a save/restore. `Append` returning the id gives the projector what
it needs without a second allocator existing.

**Why a full log drops the oldest rather than refusing the newest.** An inbox that refuses new news
because it is full is worse than one that forgets old news — the player would see a frozen feed with no
indication why. The drop is a **recorded, testable branch**, not a silent one.

**Why compaction lives here.** It is the KD-6 half of *"ignore on read, compact on write"*. `Append` is a
write that is already occurring at a point fixed by the producer's step; putting compaction in `Query`
would make reading the inbox mutate persisted state and collapse the KD-7 argument entirely (§3.2).

**`Copy(payload)`, not the caller's array.** The projector's array is a live handle; retaining it would let
post-`Append` mutation rewrite a stored item — the defect class this project has hit at
`SpawnArc`'s pin array, `TacticPreset.Players`, and `MatchReplay`'s frame list. Snapshot at the boundary.

## 3.2 `Query` — lazy retention, and the write-nothing guarantee (FM-NW-02)

```
Query(in InboxFilter filter, uint worldDay) -> readonly InboxItem[]:
    out := []
    foreach item in InTotalOrder():                          # (WorldDay, SourceTag, ItemId)
        if worldDay - item.WorldDay > INBOX_RETENTION_DAYS:  continue   # lazily aged out
        if not filter.Accepts(item):                         continue
        out.Append(ValueCopy(item))                          # never a live handle (FR-AN-015 posture)
    return out

IsRead(in InboxItem item) -> bool:
    if item.WorldDay < readState.ReadBeforeWorldDay:  return true
    return readState.ExplicitReadKeys.Contains((item.SourceTag, item.ItemId))
    # A key whose item is gone is simply never matched here -- IGNORED, not cleaned up (F9).
```

**`Query` mutates nothing, and that is a requirement rather than a property** (FR-NW-020). The whole KD-7
argument — that #46 needs no tick slot because lazy aging has no observable side effect — is false the
moment a read writes. If `Query` pruned aged items or dead keys, a save taken **after** merely opening the
inbox would differ from one taken before, and #46 would owe #30 a step. §5.5 asserts byte-identity across
a read directly.

**Note what "aged out" does and does not mean.** An item past the retention window is **invisible to a
query** but **still in the blob** until an `Append` evicts it or `INBOX_MAX_ITEMS` does. That is
deliberate: it keeps the visible feed a **pure function of `(log, worldDay)`** — the same log queried on
the same day always yields the same answer, regardless of how many times it has been queried before or
whether an `Append` happened to intervene.

## 3.3 `MarkRead` / `MarkAllReadBefore` — the watermark (FM-NW-03)

```
MarkAllReadBefore(uint worldDay):
    readState.ReadBeforeWorldDay := Max(readState.ReadBeforeWorldDay, worldDay)
    DropKeysBelow(readState.ExplicitReadKeys, worldDay)      # they are now implied by the watermark

MarkRead(SourceTag src, int itemId):
    if not TryFind(src, itemId, out item):  return           # already evicted -- a legal no-op, not an error
    if item.WorldDay < readState.ReadBeforeWorldDay:  return  # already implied
    AddIfAbsent(readState.ExplicitReadKeys, (src, itemId))
```

**The watermark is monotone (`Max`), which is what bounds the exception set.** Every key below it is
dropped as it advances, so out-of-order reads accumulate only *within the retention window* — never across
a career. Without the monotone guarantee a caller passing a smaller day would resurrect dropped keys as
"unread", which reads to the player as the inbox forgetting what they had read.

**`MarkRead` on an evicted item is a no-op, not an error.** The client renders a list and the player
clicks; an eviction can land between those two moments, exactly as #35's expiry can land between a render
and an answer. The two specs classify the race the same way for the same reason.

## 3.4 `TryTalkToPlayer` — man-management (FM-NW-04, deep tier)

```
TryTalkToPlayer(int playerId, int intentIndex, int optionIndex) -> bool:
    RequireKnownIntent(intentIndex)                          # F4 -- malformed input
    intent := ManManagementIntents[intentIndex]
    if optionIndex < 0 or optionIndex >= intent.Options.Length:  throw       # F4

    if not IsOnManagedRoster(playerId):        return false  # F5 -- a legal state
    if TalkedThisWindow(playerId):             return false  # F5

    delta := intent.Options[optionIndex].DeltaPermille       # a function of the OPTION ALONE (FR-NW-022)
    RequireInRange(delta, INBOX_DELTA_MIN, INBOX_DELTA_MAX)  # F3
    if delta != 0:                                            # FR-NW-024 -- a zero delta is never stored
        AppendPendingDelta(playerId, delta, today)
    MarkTalked(playerId, today)
    return true
```

**The outcome is a function of the chosen option alone — never of the target's current morale**
(FR-NW-006 / FR-NW-022). This is not a simplification; it is what FR-HS-025 requires. #46 is the one
consumer that *causes* a morale write, so a #46 that also *read* morale would be exactly the two-way
coupling that requirement bars — and #33's read-accessor list (*"#31/#35/#45"*) deliberately excludes #46.

The tempting feature — *"he's unhappy, so the reassurance lands harder"* — would be implemented without a
second thought, and is precisely the violation. If a morale-sensitive outcome is wanted later it arrives
as a **routed committed value** the root supplies **into** the interaction, the same mechanism the
consequence uses in the other direction. Displaying the player's mood on the screen is **#38's** read
through #33's own accessor, not #46's.

**The refuse/throw split** mirrors #35's `TryAnswerQuestion`: an unknown intent or an out-of-roster option
is a **client bug** and throws; a departed player or a spent window is a **named legal state** and returns
`false`. Collapsing them in either direction either crashes on an ordinary click or hides a real bug.

## 3.5 `TryTakePendingDelta` — the routed consequence (FM-NW-05)

```
TryTakePendingDelta(int playerId, out int deltaPermille) -> bool:
    if not TryFindPending(playerId, out i):
        deltaPermille := 0; return false          # NOT an error -- #30 asks for EVERY player
    deltaPermille := pending[i].DeltaPermille
    RemoveAt(i)                                   # cleared ON DELIVERY -> exactly-once (FR-NW-025)
    return true
```

and, at the **root**, where the summation that creates the risk lives:

```
# inside #30's RunWorldTickInFixedOrder(), step 3, per player -- root-side code
ext := 0
foreach producer in ExternalDeltaProducers:                   # {#35, #46, ...} -- ERR-030-024
    if producer.TryTakePendingDelta(playerId, out var d):  ext += d
ext := Clamp(ext, EXTERNAL_DELTA_MIN, EXTERNAL_DELTA_MAX)     # clamp AFTER summing (KD-3)
input := new HumanSystemsDayInput(result, minutes, boardDelta, externalDelta: ext)
```

**The clamp is after the sum, and it lives at the root.** Two producers each at their own bound would
otherwise compose past the field's contract. This is the price of the single producer-agnostic field
(KD-3): a per-producer field would have made over-contribution structurally impossible. The trade was made
because producer #3 would otherwise need a third field on an approved struct — but the clamp is now a
correctness dependency **outside** #46, which is why §5.5 locks it and §7.4 records it as a risk.

**Exactly-once is a property of clearing at delivery.** A save taken between the talk and the next step 3
carries the pending row (KD-6); the restored run drains it once. A second step 3 on the same day — #33's
own F6 no-op case — finds nothing.

**`TryTakePendingDelta` takes no `worldDay`**, deliberately: it drains whatever is pending regardless of
when it was recorded, which is what makes delivery robust across a save/restore or a multi-day jump. A
day-matched drain would strand a delta recorded on a day the loop subsequently skipped.

## 3.6 Arithmetic convention (pinned)

Every expression above is exact integer arithmetic — comparison, addition, array indexing, and one
`Clamp`. **#46 contains no division at any tier**, so no rounding convention arises and none may be
introduced without a spec change: `Math.Round` operates on `double` and would violate FR-NW-003 outright.

## 3.7 Worked examples (hand-verifiable)

At `INBOX_MAX_ITEMS = 200`, `INBOX_RETENTION_DAYS = 365`, `INBOX_DELTA_MIN/MAX = ∓1000`,
`EXTERNAL_DELTA_MIN/MAX = ∓1000`, `INBOX_NO_SUBJECT = -1`.

| # | Setup | Working | Result |
|---|---|---|---|
| (a) | Match projector appends `Season/MatchPlayed` on day 300, payload `{5, 9, 1, 1, 12}` | arity check passes; cursor `Season` = 41 | item id **41**; the **scoreline is stored**, which the derived design could not do |
| (b) | (a) restored from a save on which the table has since absorbed further results | the item's payload is read back verbatim | still `1–1 vs club 9, round 12` — **the KD-1 lock** |
| (c) | Second `Season` append same day | cursor → 42 | id **42**; total order `(300, Season, 41) < (300, Season, 42)` — **tie-free** |
| (d) | A `Media` append on day 300 | different `SourceTag` cursor | id **0**; order breaks the tie on `SourceTag`, not on id |
| (e) | `Append` with a 4-element payload for a 5-arity kind | `RequireLength` | **throws** (F2) — the lock that makes the payload schema enforceable |
| (f) | Log holds 200 items; a new `Append` | drop-oldest loop | oldest evicted, new item stored, **no throw** (F6) |
| (g) | `Query` on day 700 over an item from day 300 | `700 − 300 = 400 > 365` | **not returned** — and **still in the blob** |
| (h) | The same `Query` run five times | no write path | **the blob is byte-identical** before and after (FR-NW-020) |
| (i) | `MarkRead(Season, 41)` after (g) evicted it from view | `TryFind` misses | **no-op**, no throw (§3.3) |
| (j) | `MarkAllReadBefore(400)` then `MarkAllReadBefore(200)` | `Max(400, 200)` | watermark stays **400** — monotone, so read items do not become unread |
| (k) | `TryTalkToPlayer(7, intent 2, option 1)`, delta `+80` | in range, non-zero | `true`; one `PendingDelta` row |
| (l) | Same, but the option's delta is `0` | `delta != 0` fails | `true`; **no row written** (FR-NW-024) |
| (m) | `TryTalkToPlayer(7, …)` again the same window | `TalkedThisWindow` | **`false`** — a legal state, no throw (F5) |
| (n) | `TryTalkToPlayer(7, intent 2, option 9)` on a 3-option intent | out of roster | **throws** (F4) |
| (o) | Step 3 on day *D+1*: #35 pending `+600`, #46 pending `+700` for player 7 | sum `1300`, then clamp | `ExternalDeltaPermille` = **1000** — the root's post-sum clamp (§3.5) |
| (p) | (k) then a save, then restore, then step 3 | the row is in the blob | delivered **once**; a second step 3 the same day returns `false` |
| (q) | (k) then player 7 is transferred before delivery | drop-on-departure (F8) | the **delta is dropped**; but an *item* about player 7 **stays** (FR-NW-016) |

Example (o) is the one that would be wrong under a per-producer field or a per-producer clamp, and (q) is
the deliberate asymmetry a "consistency" pass would unify in the wrong direction.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-27 | — | Initial §3 (FM-NW-01..05: append with id allocation and compaction, query with lazy retention and the write-nothing guarantee, the monotone read watermark, man-management with its refuse/throw split, the routed drain with the root's post-sum clamp; arithmetic convention; seventeen worked examples). The `Copy(payload)` snapshot at `Append` is stated explicitly — retaining the projector's live array is the `SpawnArc` / `TacticPreset.Players` / `MatchReplay` defect class this project has hit three times. Status IN REVIEW. |
#endregion
