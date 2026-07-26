# News, Inbox & Man-Management #46 — Design Supplement

> **Created:** July 26, 2026
> **Last Updated:** July 26, 2026 (v0.6 — AR-5 sweep: 0H+0M+3L, **CONVERGENCE**; prior v0.5 AR-4, v0.4 AR-3, v0.3 AR-2, v0.2 AR-1, v0.1 initial)
> **Version:** 0.6
> **Status:** DESIGN SUPPLEMENT (pre-promotion — no section files, no `SPEC_INDEX.md` row)
> **Candidate spec:** **#46** · **FR prefix:** `FR-NW` · **Wave:** 6 · **Tier:** S2 min → S4 deep
> **Promoted from:** `docs/tracking/spec-plans/spec-46-news-inbox-man-management.md` v0.1
> **Sibling:** `media-press-interactions-design.md` (#35) — authored first in this wave; §2(f) and KD-3
> carry a **coordination revision** to one of its back-props.

---

## 0. Purpose and posture

This supplement resolves the five key decisions the #46 plan defers, against **verified** upstream source
rather than assumption. Design only — no code, no section files, no registry row.

**One of the plan's five decisions is refuted by verification and a second is superseded by its own sibling.**
The plan's KD-4 ("which inbox state is derived-on-read vs stored — keep the stored surface tiny") assumes the
feed is derivable; §2(a) shows it is not, for the most obvious item type there is. And #35's landing — one
week's work earlier in the same wave — establishes a morale-consequence path whose producer-specific shape
does not survive a second producer, which is #46 (§2(f) / KD-3).

## 1. Scope

**#46 owns:** the **inbox** — a persisted, ordered, bounded log of manager-facing items projected from other
specs' events, plus its **read state** — and **man-management**: talk-to-player interactions producing a
bounded morale consequence.

**#46 does not own:**

| Not owned | Owner | How #46 relates |
|---|---|---|
| The **events** themselves | #30 / #31 / #35 / #44 / #45 | projected into `InboxItem` value copies at the root (KD-2) |
| Press-conference logic | **#35** | #46 shows #35's items; it renders no press question of its own (KD-5) |
| The **morale model** | **#33** (FR-HS-002) | #46 emits a bounded delta through the routed seam; it never writes (KD-3) |
| Rendering text into a locale | **#49** | `InboxTextBoundary` sibling adapter (KD-4) |
| Match **statistics** | **#37** | #37 holds no state (FR-AN-020) and is a live-match reader — not an inbox source (§2(d)) |
| Rendering the inbox | **#38** | #46 exposes read-only value copies (the FR-AN-015 posture) |

## 2. What already exists (verified)

**(a) The inbox cannot be derived, because #30 does not retain what the items would say.**
`season-competition-loop/section-2.md` §2.2:

> **`Fixture`** (readonly struct): `RoundIndex (int)`, `HomeClubId (int)`, `AwayClubId (int)`, `Played (bool)`
> — plus the resolved result once played is recorded **on the table, not the fixture** (the fixture list is
> the immutable schedule; `Played` is the only mutable-on-play field).

So after a fixture is played, the **scoreline is gone** — only the aggregate table row survives. #44 already
hit and recorded the same wall from the other side (`discipline-suspensions/section-1.md` KD-1: *"…and #30
retains no per-fixture ledgers (`MatchResult` is scoreline-shaped), so recompute-on-load has no input"*).

**Consequence:** an inbox item as basic as *"you drew 1–1 away to Everton on matchday 12"* is **not
recomputable** from a loaded save. The plan's KD-4 premise — *"inbox items are largely a derived view over
already-persisted events … minimise new stored state"* — is false for the single most common item type.
KD-1 inverts it, following #44's forced-persistence precedent rather than #37's stateless one.

**(b) `EmitMatchOutcome` is the emission point, and it is already producer-only.**
`section-3.md` §3.4 runs, per fixture: `Table.ApplyResult(result)` → `EmitMatchOutcome(result)` →
`f.Played := true`, with FR-SN-017 pinning #30 as *"the **producer only**"* and barring it from wiring the
outcome into #22's ingest. The `result` in hand at that instant carries the scoreline. That instant is
therefore the **only** moment an accurate match item can be captured — which is what makes KD-1's "persist"
answer forced rather than chosen.

**(c) #33 anticipates #46's write, and its own text is in tension about the mechanism.**
`personalities-morale-dynamics/`:

- **FR-HS-002** — *"No other assembly writes them."*
- **FR-HS-024** — *"**#46 is the only consumer that writes #33 morale** (man-management)."*
- **§3.3** — *"No accessor mutates #33 state; there is no write path INTO #33 morale except **#46's future
  man-management seam** (deferred)."*
- **§8** `XC-033-007` — *"#46 News/Inbox (future) — man-management morale write | the sole write-INTO-#33
  consumer (deferred, KD-3)."*

Read together these permit exactly one coherent shape: a **#33-owned** mutation that #46 causes but does not
perform. Any reading in which #46 itself assigns `MoraleState.MoralePermille` contradicts FR-HS-002 outright.
KD-3 pins the shape; §8.1 files the wording fix so a future implementer cannot pick the other reading.

**(d) #37 is not an inbox source.** `match-analytics-statistics/section-2.md` FR-AN-020 — #37 *"MUST hold no
persistent state"* — and FR-AN-021 — it *"MUST consume **live during the match**"*, and *"MUST NOT assume the
serialized ledger bytes can be re-parsed."* A post-match report in the inbox therefore cannot call #37 after
the fact. Either the root captures #37's view models at emission time alongside the scoreline (KD-1's
mechanism, extended) or post-match stats are not an inbox item. Recorded so the boundary is not discovered
at implementation.

**(e) #49 names #46's adapter in advance, and #46's binding is #35's binding.**
`localization-accessibility/section-7.md` §7.3 names `InboxTextBoundary` alongside `MediaTextBoundary`, and
§2.2 pins that *"#35/#46 carry disjoint slots"*. FR-LC-002/012/013/014/015 apply to #46 identically. #35's
supplement worked this binding out in full (its KD-1), including the **ordinal-stability** obligation that
falls out of an intent enum being both serialized and the catalogue key — #46 inherits both the shape and
that obligation (KD-4).

**(f) #35 has already established the morale-consequence path — with a producer-specific field.**
`media-press-interactions-design.md` KD-3 + its `ERR-033-001` add
`HumanSystemsDayInput.MediaDeltaPermille`, a committed value #30 routes into #33's own day step, so that
#33 keeps its single writer and #35 never references #33.

**Consequence:** the mechanism is right and #46 adopts it — but the **naming is not extensible**. #46 is
producer #2 and would need `InboxDeltaPermille`; #35's own deep tier and any later man-management-adjacent
system would need more. A struct that grows a field per producer is the shape KD-3 replaces, by a
coordination revision to #35's back-prop rather than a second parallel field. This is the same failure
mode #35 found in #49's `FR-LC-020` — a contract written correctly for one producer, surfacing when the
second arrives — and it is caught here for the same reason.

**(g) #46 takes no determinism allocation, and none is reserved for it.** The roadmap §6 block runs
`0x20`–`0x2D` and covers #28–#36 / #40–#45; #16 §3.4's `0x2A` row records in terms that *"#37–#39 are
read-only/presentation/infra and take no tag."* #46 is the same class (its plan: *"read-only/aggregation —
no RNG stream, no domain tag"*), and the catalogue accordingly has no `_RESERVED_` row for it. Nothing to
file (KD-8) — but note this means #46 has **no reserved value to promote later**, so a future stochastic
news generator would need a fresh allocation, not a promotion.

## 3. Staging (minimal-first → deep)

| Tier | Content |
|---|---|
| **Minimal (the identity)** | A persisted, ordered, bounded item log with read marks and a read-only query. **No man-management** (no morale consequence at all), no tick slot. With no producer wired, the feed is empty and #46 is inert. |
| **Deep** | Still **draw-free** (KD-8 holds at every tier — the deep tier adds no stochastic surface, unlike #35's). Talk-to-player man-management on the same log: a bounded answer set per interaction, its consequence routed through the KD-3 external-delta seam; richer filtering/categorisation; #35 media items and #45 board items as they land. |

The deep tier **extends** the minimal identity; it never rewrites it. Note the split is *exactly* the plan's
(*"minimal = a read-only event feed with no man-management writes"*) — that decision survived verification
intact, and is the reason #46's minimal tier needs neither the morale seam nor a save-format coordination.

## 4. Key decisions

### KD-1 — The inbox is a **persisted item log**, not a derived view

§2(a) forces it: the scoreline an item reports is destroyed by #30's own design the moment the table absorbs
it, so "derive on read" yields an item that cannot state what happened. #46 therefore stores its items.

**What is stored is a compact integer record, never a rendered string:**

```
InboxItem : { SourceTag (byte), ItemKind (byte), ItemId (int),          # ItemId assigned by #46 (KD-2)
              WorldDay (uint), SubjectId (int; INBOX_NO_SUBJECT = -1),
              Payload (int[PAYLOAD_MAX]) }                              # full shape + rules in §5
```

**`SourceTag` is #46's own namespace and must not be conflated with #49's `TextTemplateId.ProducerTag`.**
They overlap in neither membership nor meaning: #46's tags a *source of events* (#30 is one; #30 is not a
text producer at all), #49's tags a *text-producer family* (#22 is one; #22 emits no inbox items). Naming
both "producer tag" is how the project's `TacticTranslation` CS0104 and duplicate-`PlayerAttributes`
collisions started, so they are named apart from the outset.

`Payload` is a fixed small integer array whose meaning is fixed **per `(SourceTag, ItemKind)`** — a match
item carries `{homeClubId, awayClubId, homeScore, awayScore, roundIndex}`. Storing integers rather than text
is what keeps the save locale-independent under FR-LC-006 (a stored string would bake the locale the save
was written in) and what lets the same item render differently after a language change.

**Bounded by a retention window, not by a career.** `INBOX_RETENTION_DAYS` `[GT]`; items older than the
window are dropped. Without a bound the log grows monotonically for a 20-season career, and an inbox is a
recency surface — this is a design position, stated rather than left to be discovered when a save doubles in
size. §11 R-1 records the tension with a player who wants career-long history.

*Rejected alternative:* have #30 retain per-fixture results so the feed becomes derivable. Rejected — it
changes #30's `Fixture` contract (*"the fixture list is the immutable schedule"*) to serve a presentation
consumer, adds a second truth beside the table, and #44 already declined the symmetric move.

### KD-2 — #46 references **nothing**; producers are projected in at the root

Each producer's state is projected into `InboxItem` value copies by a small per-producer projector living at
the **`TacticalDirector.SeasonSave` root** — the assembly that already references everything, and the same
layering #33 uses for `RouteIntoLivingWorld` (*"owned by the SeasonSave root, NOT #30, NOT living-world"*)
and #49 uses for its boundary adapters.

Three consequences, all load-bearing:

- **#46 references no producer**, so it cannot drift into owning press logic (#35), discipline (#44), or
  board state (#45) — the plan's §9 first risk, closed structurally rather than by discipline.
- **A producer never references #46** — the reference direction #35's KD-6 already asserts from its side.
  The two specs agree without either importing the other.
- **The phantom-consumer problem disappears.** With zero projectors wired, the feed is empty and every #46
  surface is exercisable. #46 does **not** wait on #31/#35/#45; each projector lands with its producer. This
  is the sharpest reason to prefer root projection over #46-side readers: FR-LW-031 would otherwise bar #46
  from being authored at all until its producers exist.

**Where each projector runs — this is the part KD-7 constrains, so it must be pinned.** A projector is
root-side code invoked at a **fixed, already-existing point owned by its producer**, never at a #46 step:

| Producer | Projector site | Why there |
|---|---|---|
| #30 match result | inside §3.4, right after `EmitMatchOutcome(result)` | the only place the scoreline exists (§2(a)/(b)) |
| #35 press | the #30 post-round path, after #35's own queue seam | the conference exists from that instant |
| #44 discipline · #45 board · #31 transfers | immediately after **that producer's own step** in `RunWorldTickInFixedOrder` | the producer's day-state is final there, and the step is already pinned |

This is load-bearing: a naive reading of KD-7 ("#46 takes no tick slot") would leave a **world-tick**
producer — a suspension incurred on a Tuesday, a board-confidence collapse — with **nowhere to emit**, and
the two obvious repairs are both wrong (give #46 a slot, contradicting KD-7; or silently drop non-fixture
items). Siting each projector at its producer's existing step resolves it: #46 still has no step of its own,
every emission point is inside the pinned fixed order, and a projector lands with its producer rather than
ahead of it. Ordering is therefore inherited from #30's tick order, not defined by #46.

### KD-3 — Man-management routes through **one producer-agnostic external-delta seam**

Adopting #35's mechanism (§2(f)) and generalizing its shape:

```csharp
public readonly struct HumanSystemsDayInput
{
    public readonly MatchDayResult Result;
    public readonly int MinutesPlayed;
    public readonly int BoardObjectiveDeltaPermille;
    public readonly int ExternalDeltaPermille;   // was MediaDeltaPermille (#35 ERR-033-001) — now
                                                 // producer-agnostic: the ROOT sums every producer's
                                                 // pending delta for this player-day, then clamps.
    public static HumanSystemsDayInput Neutral => new(MatchDayResult.None, 0, 0, 0);
}
```

**Why one field and not two.** A per-producer field makes `HumanSystemsDayInput` grow with the number of
systems that can nudge morale, and every addition is a #33 back-prop against an approved struct. One
field with **summation at the root** costs nothing #33 can observe (it receives one committed integer either
way) and is closed under new producers. The sum is clamped to `[-1000, 1000]` **before** it reaches #33, so
two producers cannot compose past the contract's range — the clamp lives at the root, with the summation
that creates the risk, not inside #33.

**This resolves §2(c)'s tension without weakening FR-HS-002.** All morale mutation stays inside
`AdvanceHumanSystemsDay`: #33 keeps exactly one write site, the F6 same-day-no-op / day-gap guard covers
external deltas for free, and the drift/clamp semantics are uniform across every input. #46 causes a morale
change and never performs one; §3.3's *"#46's future man-management seam"* is **this routed field**, not a
#46-callable mutator.

*Rejected alternative:* a #33-owned `ApplyManManagementDelta(ref MoraleState, …)` that #46 (or the root)
calls immediately at command time. Rejected — it introduces a second mutation entry point that bypasses
`ComputeMoraleTarget`, needs its own clamp and its own idempotency story, and gives #33 two write sites
where FR-HS-002's value comes from having one. Immediacy is the only thing it buys, and the project has
already accepted next-day application for board (#45 KD-7) and media (#35 KD-3).

**#46 owns its own pending deltas**, exactly as #35 owns its own (each producer's undelivered deltas live in
its own sub-blob, and the root drains them all at step 3). A shared pending store would need an owner, and
no spec is the natural one.

**#46 never *reads* morale — and that is a requirement, not an omission.** FR-HS-024's read-accessor list is
*"#31/#35/#45"*; #46 appears in that requirement only as the writer. FR-HS-025 then bars **two-way coupling**
with any consumer outright (*"Morale is a projection OUT of #33 — no two-way coupling … avoids
determinism-ordering fragility"*). A #46 that both reads morale and causes a write is exactly the coupling
that forbids. So a man-management outcome is a function of **the chosen option alone**, never of the target's
current morale.

This needs saying because the natural feature — *"he's unhappy, so the reassurance lands harder"* — reads as
obviously desirable and would be implemented without a second thought. If a morale-sensitive outcome is
wanted later it must arrive as a **routed committed value** the root supplies into the interaction, the same
mechanism #46's own consequence uses in the other direction — never a #46-side accessor call. Display of a
player's mood in the man-management screen is **#38's** read through #33's accessor, not #46's.

### KD-4 — #49 binding: `InboxTextBoundary`, and #35's ordinal contract inherited

`InboxIntent` (item headlines/bodies **and** man-management prompts/options, one roster), disjoint slots
from #35's, a sibling `InboxTextBoundary` adapter at the boundary layer, the FR-LC-015 intent-value pre-gate,
and FR-LC-008a coverage over #46's roster. **#46 never references `TacticalDirector.Localization`**
(FR-LC-012).

**Two identities, deliberately separate — and the split is the point.** `ItemKind` is the **serialized**
identity (it, with `SourceTag`, fixes the `Payload` schema); `InboxIntent` is the **catalogue** identity the
adapter maps to a `TextTemplateId`. The adapter's job is exactly the mapping
`(SourceTag, ItemKind) → InboxIntent`, so a saved item never stores a localization ordinal.

This is not the same as #35, and the difference is load-bearing: #35 collapsed to a *single* `MediaIntent`
because its records are all its own, while #46's items come from **five sources whose kinds it does not
own** — forcing them through one intent enum would make adding a #31 item kind an edit to #46's
localization roster. The cost of the split is that **both** carry an **ORDINAL STABILITY — APPEND-only**
contract, for two distinct reasons: reordering `ItemKind` re-reads every saved `Payload` under the wrong
schema, and reordering `InboxIntent` re-points every item at the wrong template. Neither has a version gate
that would catch it, so both are pinned and asserted.

**The selection value.** #46's minimal tier is draw-free (KD-8), so it supplies its `ulong` the way #35 does
— a local keyed mix — and therefore **inherits #35's `ERR-049-001` dependency** (FR-LC-020 binds that field
to #22's `world.text` draw). If that back-prop is refused, #46 takes the same `SelectionDraw = 0` fallback.
#46 files nothing here; it is the second spec blocked on one #49 wording fix, which is itself the argument
for making it.

### KD-5 — Boundary with #35: #46 shows, #35 owns

#35 owns which conference is queued, its question, its answer set, and its consequence. #46's projector reads
#35's read-only conference query (the surface #35's KD-6 already exposes for exactly this) and produces an
item. **#46 renders no press text of its own and defines no press intent** — a press item's text identity is
#35's, carried in the item and rendered through #35's adapter.

The plan's KD-5 asked where the line falls; this is it, and it is enforced by KD-2's reference direction
rather than by convention.

### KD-6 — Persistence: an opaque, independently version-gated sub-blob

`INBOX_SAVE_FORMAT_VERSION` [FIXED] = 1 — the item log, the read marks, and #46's pending man-management
deltas, composed into #30's `SeasonSaveCodec`, **not** a `WORLD_STORE_FORMAT_VERSION` bump (the
#40/#42/#44/#45/#35 pattern). Version gate first; overflow-safe `Require(offset, need, total)` length
prefixes against `total − offset`; trailing-byte guard; fail loud on all three. **APPEND-only** layout.

**Read state is a watermark plus an exception set**, not a flag per item:

```
ReadState : { ReadBeforeWorldDay (uint), ExplicitReadKeys (set of (SourceTag, ItemId)) }
```

Everything older than the watermark is read; the exception set carries only items read out of order within
the window, and collapses into the watermark as it advances. A per-item flag would grow a byte per item
forever and make "mark all read" an O(n) rewrite of the whole blob.

**The exception set is bounded by the log, not merely by the watermark.** A key whose item is no longer in
the log — evicted by the retention window or by `INBOX_MAX_ITEMS` — is dead. Without that bound, a player
who reads out of order for twenty seasons accumulates keys for items that no longer exist, and the set
becomes the unbounded growth the watermark design exists to avoid.

**A dead key is *ignored* on read and *compacted* on write — a query never mutates.** This distinction is
load-bearing against KD-7: retention is evaluated lazily at query time, so if pruning were a query-time
mutation, reading the inbox would change persisted state and the "lazy aging has no observable side effect"
argument would collapse (a save taken after a read would differ from one taken before). Instead a query
filters dead keys out of its answer and writes nothing; the persisted set is compacted at the next
`Append` — a write that is already happening, at a point fixed by the producer's step rather than by when a
human opened a screen.

**Deliberately absent:** any RNG cursor (KD-8), any rendered string (KD-1), and any copy of morale, the
table, or a producer's own state — an item's `Payload` is a **snapshot of what was true at emission**, which
is the point, and is never re-synchronised against the producer afterward.

### KD-7 — **No tick slot** — and this is a real difference from #35

#46 needs no position in `RunWorldTickInFixedOrder`:

- **Retention aging is computed lazily**, at query time. Dropping a stale item has **no consequence** —
  nothing downstream reads it, no delta is produced, no other state moves — so a lazy sweep cannot make state
  depend on whether the client looked. This is precisely the argument #35's KD-5 could **not** make (its
  expiry produces a no-comment consequence), and the contrast is worth stating: the same technique is correct
  in one spec and wrong in the other, for a reason that is checkable.
- **Man-management is command-driven** (`TryTalkToPlayer`), like #35's answer and #31's `SubmitBid`.
- The **step-3 drain** of pending deltas is #30's existing seam (#35's `ERR-030-013`), generalized by KD-3 to
  iterate every external-delta producer rather than #35 alone.

**#46 cites no step number of its own, which is the precise form of its independence from the #30 tick-order
repair** that gates #35 (its §8.0 `ERR-030-012`). Note this is *narrower* than "#46 is unaffected by the tick
order": KD-2's world-tick projectors run **inside** the fixed order, at their producers' steps, so #46's
emission ordering is inherited from whatever that order settles as. What #46 avoids is having a number in it
— nothing in #46 has to be renumbered when the repair lands, and #46 can be promoted while it is still open.

### KD-8 — Draw-free at every tier; no #16 row at all

No stochastic decision anywhere: item order is a total order on `(WorldDay, SourceTag, ItemId)` — canonical
and tie-free by construction, since `ItemId` is unique within a producer — and a man-management outcome is a
deterministic function of the chosen option. #46 registers no stream, promotes no tag, and per §2(g) has no
reserved value to promote. **#16 is untouched**, the #37/#44 read-only-spec property.

### KD-9 — Behaviour-neutral identity

With no projector wired: the feed is empty, no `InboxItem` is stored, the sub-blob is a version header plus
four zero counts (items, read marks, pending deltas, cursors), no stream is registered ⇒ every existing
cursor is byte-identical, and
`ExternalDeltaPermille = 0` ⇒ `ComputeMoraleTarget` is unchanged. A season advanced with #46 present is
byte-identical to the same season pre-#46 at the #33 and #30 seams. With projectors wired but
man-management off (the minimal tier), the same holds at the **#33** seam specifically — #46 stores items and
changes nothing anyone simulates from.

## 5. Persistent state (shape)

```
InboxItem      : { SourceTag (byte), ItemKind (byte), ItemId (int), WorldDay (uint),
                   SubjectId (int; INBOX_NO_SUBJECT = -1), Payload (int[PAYLOAD_MAX]) }
                 # ItemId is assigned BY #46 at Append (KD-2), never by the caller
ReadState      : { ReadBeforeWorldDay (uint), ExplicitReadKeys[] : (SourceTag, ItemId) }
PendingDelta   : { TargetPlayerId (int), DeltaPermille (int [-1000,1000] \ {0}), RecordedWorldDay (uint) }
InboxCursors   : { NextItemId per SourceTag (int[]) }   # length-prefixed + APPEND-only: a new source
                                                       # extends the array, never reorders it
```

All integer. `INBOX_NO_SUBJECT = -1` is explicit, not `0` (`0` is a valid `PlayerId` — the trap #40's
`BoardModifier` F4 and #33's `PersonalityProfile` F4 both exist to catch). A zero delta is never recorded
(the #44 canonical `(0,0)`-drop rule). The log is bounded by `INBOX_MAX_ITEMS` `[GT]` in addition to the
retention window — a full log **drops the oldest item**, since an inbox that refuses new news because it is
full is worse than one that forgets old news, and the drop is a recorded, testable branch.

**`PendingDelta` inherits #35's roster-lifecycle rule verbatim** — an undelivered delta whose target leaves
the managed roster is **dropped** in lockstep with #33's FR-HS-027, never migrated across #31's `PlayerId`
re-key. **`InboxItem` does not:** an item about a departed or retired player stays, because it is a historical
record of something that happened, not a pending effect on a live entity. The two rules differ deliberately,
and the difference is exactly "pending effect" vs "past record".

## 6. Determinism posture

- Command-driven + root-projected; no tick slot (KD-7); never the match loops.
- Draw-free at every tier; no stream, no tag (KD-8).
- All-integer; no float; no stored string, so state is locale-independent (FR-LC-006).
- Item order is total and canonical on `(WorldDay, SourceTag, ItemId)`.
- Retention/`INBOX_MAX_ITEMS` eviction is a pure function of the stored log — evaluated lazily on read,
  which is sound precisely because a read writes nothing (KD-6: dead read-keys are filtered on read and
  compacted at the next `Append`).

## 7. Primary surfaces (proposed)

| Surface | Direction | Notes |
|---|---|---|
| `Append(sourceTag, itemKind, worldDay, subjectId, in payload) → int` | root → #46 | the only write-in; called by each producer's root projector at that producer's own pinned site (KD-2). **#46 assigns the `ItemId`** from its own per-source cursor and returns it — the caller never supplies one, so ids stay unique and monotonic across a restore without a second allocator existing |
| `Query(filter, worldDay) → readonly items` | #46 → #38 | value copies (the FR-AN-015 posture); applies retention lazily |
| `MarkRead(sourceTag, itemId)` / `MarkAllReadBefore(worldDay)` | client → #46 | the watermark + exception set (KD-6) |
| `TryTalkToPlayer(playerId, intentIndex, optionIndex) → bool` | client → #46 | **deep tier**; `false` = not available (a legal state — player departed, already talked this window); fail loud on unknown intent / out-of-range option (#35's `TryAnswerQuestion` split) |
| `TryTakePendingDelta(playerId, out int) → bool` | #30 → #46 | drained at step 3 into `ExternalDeltaPermille` (KD-3), summed with every other producer's and clamped at the root |
| `InboxTextBoundary.BuildRequest(intent, draw, in InboxSlots)` | boundary layer | the #49 sibling adapter (KD-4); **not** a #46 surface |

## 8. Cross-spec back-props

### 8.1 At approval (must land atomically with the status flip)

| ID | Target | Change |
|---|---|---|
| **ERR-033-003** | #33 §2.2 `HumanSystemsDayInput` + §3.1 `ComputeMoraleTarget` | Rename/generalize #35's `MediaDeltaPermille` → **`ExternalDeltaPermille`**, producer-agnostic, **summed across producers and clamped by the root** before it reaches #33 (KD-3). **Supersedes `ERR-033-001` as filed by #35** — one field, not one per producer. If #35 is approved first, this is a rename of a field that has no implementation yet; if #46 is approved first, #35's back-prop lands already generalized. |
| **ERR-033-004** | #33 §3.3 + FR-HS-024 | State that *"#46's man-management seam"* **is** the routed `ExternalDeltaPermille`, not a #46-callable mutator — closing the §2(c) reading under which #46 would assign `MoralePermille` directly and contradict FR-HS-002. No behaviour change; it makes the only coherent reading the only available one. |
| **ERR-030-014** | #30 §3.3 step 3 | Generalize #35's `ERR-030-013` drain so step 3 iterates **every** external-delta producer (#35, #46, …), summing and clamping into `ExternalDeltaPermille`. Empty until the first producer's T2. |
| **ERR-030-015** | #30 §3.4, after `EmitMatchOutcome(result)` | The **match-item projector** null seam (KD-2). Filed by #46 in its own right rather than assumed from #35's `ERR-030-013`: that seam is #35's *conference queue*, so relying on it would make #46's most basic item type depend on #35 being approved — silently contradicting §12's independent-promotion claim. Same site, so if both land they coalesce into one hook with two calls; if #35 never lands, #46 still works. |

### 8.2 Deferred (land at the named tier, not at approval)

- The `SEASON_SAVE_FORMAT_VERSION` bump, at T2 when the sub-blob is first composed in.
- Each producer's **root projector** (#30 match, #35 press, #44 discipline, #45 board, #31 transfers) — each
  lands with its producer, never ahead of it (FR-LW-031). None is a #46 change.
- Man-management itself (deep tier) — until then #46 files no pending deltas and the KD-3 seam carries `0`.
- Capturing #37 view models alongside a match item, **if** post-match stats become an inbox item (§2(d)).

### 8.3 Explicitly **not** back-props

- **#16** — no row exists for #46 and none is needed (§2(g)); the #37/#44 read-only property.
- **#49** — #46 adds a sibling adapter, which is the documented extension point. It **inherits** #35's
  `ERR-049-001` rather than filing its own (KD-4) — the same fix serves both, and a second identical filing
  would be noise.
- **#35** — KD-5's boundary needs nothing from it; #35's KD-6 read-only query is already the surface #46's
  projector consumes. The one #35-side change is the KD-3 coordination, filed above against #33, not #35.
- **#44 / #37** — read-only producers; #46 consumes their view models through root projection.

## 9. Test focus

Identity (a season with no projector wired is byte-identical to pre-#46 at the #33 and #30 seams; every RNG
cursor unchanged); round-trip determinism over the sub-blob **including an undelivered man-management delta
across the save boundary** and the read-state watermark/exception-set collapse; **the KD-1 lock** — a match
item survives a save/restore with its scoreline intact, which is the property the derived-view design would
have failed (assert it against a table that no longer carries the fixture's result); retention + `INBOX_MAX_ITEMS`
eviction as a pure function (same log ⇒ same visible feed, and a query does not mutate); total-order
stability across producers; **drop-on-departure for deltas but NOT for items** (§5's deliberate asymmetry);
delta sum-then-clamp at the root (two producers on one player-day cannot exceed `[-1000,1000]`);
FR-LC-008a coverage over the `InboxIntent` roster and the ordinal-stability locks on **both** `ItemKind` and `InboxIntent`; read-key handling (a key whose item was evicted is **absent from a query's answer but not written away by it** — assert the blob is byte-identical across a read, and compacted only after the next `Append`); **locale-independence**
(the same career under two display locales produces byte-identical serialized state); and **structural**
assertions that #46's assembly references neither `TacticalDirector.Localization`, #33, #30, #35, nor any
other producer (KD-2).

## 10. Reference DAG

```
root → {#30, #33, #35, #44, #45, #46, boundary}        #46 → { }
boundary(InboxTextBoundary) → {#46, #49}
```

**Acyclic — and #46 is a leaf at every tier.** It references nothing: items arrive as value copies pushed by
root projectors, and its morale consequence leaves as a committed integer the root drains. This is stronger
than #35's DAG (which takes `DeterministicSim` at the deep tier) and is what makes the KD-2 structural
assertion unconditional.

## 11. Risks and standing options

- **R-1 — the retention window will be argued with.** A player who wants a career-long news archive meets
  `INBOX_RETENTION_DAYS`. The knob is `[GT]`, but an unbounded log is a save-size commitment, not a tuning
  choice. If archival history is wanted it should be its own compact aggregate (the #22 `ColdStore`
  compression pattern), not a raised bound. Standing option, not a debt.
- **R-2 — `Payload`'s per-`(SourceTag, ItemKind)` schema is an unversioned convention** inside a versioned
  blob. Changing what slot 3 of a match item means silently re-reads old items. The §5 spec must pin each
  payload schema as APPEND-only and treat a change as an `INBOX_SAVE_FORMAT_VERSION` bump — the same
  discipline `ItemKind` and `InboxIntent` carry (KD-4), applied to the payload.
- **R-3 — "the inbox should just read the producers directly"** is the tempting simplification that would
  make #46 reference five specs and re-open the phantom-consumer bar. KD-2's structural assertion is what
  catches it; the reason it exists should be cited in #46's §1.
- **R-4 — ERR-033-003 supersedes a back-prop of a supplement that is itself unapproved.** If #35 lands first
  and unchanged, #46's approval must rename the field before either has an implementation. Cheap now,
  expensive after #35 T2. Sequencing note, not a design risk.

## 12. Promotion pipeline

1. **This supplement, AR-converged** — **DONE at v0.6.** AR-1 (1H+3M+2L) → v0.2, AR-2 (0H+3M+1L) → v0.3,
   AR-3 (0H+2M+2L) → v0.4, AR-4 (0H+2M) → v0.5, AR-5 (0H+0M+3L) → v0.6 = **CONVERGENCE** (an L-only round
   closes the cycle, per the project convention).
2. **Reconcile with #35** — land the KD-3 coordination in #35's supplement so the two agree before either is
   promoted. **Not yet done** — it lands once #46's KD-3 is accepted, as a #35 version bump.
3. **Author 11 section files** at `Status: IN REVIEW` under `docs/specs/news-inbox-man-management/`, FR
   prefix `FR-NW`.
4. **Section-file PASS-1 adversarial review** + a fix pass, recorded in §9.4.1 of the checklist.
5. **`SPEC_INDEX.md` registry row** at promotion.
6. **Lead-developer R-01..R-05 sign-off** — a human authority, not self-grantable.
7. **Flip to `APPROVED`**, landing the §8.1 back-props atomically.

## Version History

| Version | Date | Change |
|---|---|---|
| v0.1 | July 26, 2026 | Initial supplement promoted from the one-page plan. Resolves KD-1..KD-9 against verified upstream source. **The plan's KD-4 premise is refuted:** #30's `Fixture` retains only `Played` and the result goes to the table, so an inbox item's scoreline is **not recomputable** from a save (#44 hit the same wall from the other side) — KD-1 inverts "derive on read" to a persisted compact integer log with a retention window, captured at `EmitMatchOutcome`. **KD-2** makes #46 a leaf that references nothing (root projectors push value copies), which closes the plan's boundary-blur risk structurally *and* removes the FR-LW-031 bar that would otherwise stop #46 being authored before its producers exist. **KD-3** adopts #35's routed morale mechanism but generalizes its producer-specific field to one `ExternalDeltaPermille` summed and clamped at the root (ERR-033-003, superseding #35's ERR-033-001) — the per-producer shape does not survive producer #2, the same failure mode #35 found in #49's FR-LC-020 — and resolves the FR-HS-002 vs FR-HS-024/§3.3 tension by keeping every morale mutation inside `AdvanceHumanSystemsDay` (ERR-033-004). **KD-7** records why lazy retention is correct here while #35's lazy expiry was not: dropping a stale item has no consequence, so #46 needs no tick slot and does not depend on #35's #30 tick-order prerequisite. #16 is untouched and has no reserved row for #46 (§2(g)). |
| v0.2 | July 26, 2026 | **AR-1 fix pass: 1H + 3M + 2L, all resolved.** **H-1** — KD-7 (“no tick slot”) and KD-2 (“root projectors push items”) were **mutually incompatible for any world-tick producer**: match items ride `EmitMatchOutcome`, but a suspension incurred on a Tuesday or a board-confidence collapse had **no emission point at all**, and both obvious repairs are wrong (give #46 a slot, contradicting KD-7; or silently drop non-fixture items). Resolved by siting each projector at **its own producer’s already-pinned step** in `RunWorldTickInFixedOrder` — #46 still owns no step, every emission is inside the fixed order, and ordering is inherited from #30 rather than defined by #46. Added as an explicit per-producer site table. **M-1** — #46 reading morale would be the **two-way coupling FR-HS-025 bars outright**, and FR-HS-024’s read-accessor list (#31/#35/#45) deliberately excludes #46; pinned that a man-management outcome is a function of the chosen option **alone**, with the tempting “he’s unhappy so reassurance lands harder” feature named as the thing that would violate it and the compliant routed-value alternative recorded. **M-2** — `ProducerTag` collided by name with #49’s `TextTemplateId.ProducerTag` while sharing neither membership nor meaning (#30 is an event source but not a text producer; #22 the reverse) — renamed `SourceTag`, the `TacticTranslation` CS0104 / duplicate-`PlayerAttributes` precedent applied before the collision rather than after. **M-3** — `Append(in InboxItem)` took an item carrying an `ItemId` while `InboxCursors` held the allocator, leaving two id sources and no rule; `Append` now takes the fields and **#46 assigns and returns the id**. **L:** KD-9 said “three zero counts” over four state groups; §12 step 2 read as though the #35 reconciliation were already done. |
| v0.3 | July 26, 2026 | **AR-2 fix pass: 0H + 3M + 1L, all resolved.** **M-1** — KD-4 put the **ORDINAL STABILITY** contract on `InboxIntent` and described it as *“serialized inside `InboxItem`’s `ItemKind`/intent mapping”*, but §5’s `InboxItem` stores `ItemKind` and no intent — two identity types with an unspecified relation, the same confusion #35’s AR-2 caught in its own roster. Split explicitly: `ItemKind` is the **serialized** identity fixing the `Payload` schema, `InboxIntent` is the **catalogue** identity, the adapter maps `(SourceTag, ItemKind) → InboxIntent`, and **both** carry APPEND-only stability for two distinct reasons. Recorded why #46 does *not* collapse to one enum the way #35 did: its items come from five sources whose kinds it does not own. **M-2** — `ExplicitReadKeys` was described as collapsing into the watermark, which does not bound it: a player reading out of order accumulates keys for items the retention window has since evicted. Pruning is now tied to the **log**, not the watermark. **M-3** — KD-7’s claim that #46 “does not depend on the #30 tick-order repair” was overstated once AR-1’s H-1 fix sited world-tick projectors *inside* the fixed order; narrowed to the true and still-useful claim — #46 cites **no step number of its own**, so nothing in it is renumbered when the repair lands. **L** — the §9 test list did not cover the two new locks. |
| v0.4 | July 26, 2026 | **AR-3 fix pass: 0H + 2M + 2L, all resolved.** **M-1** — AR-2’s read-key pruning fix contradicted KD-7: pruning “at the same moment the item is evicted” makes a **lazy, query-time** eviction mutate persisted state, so a save taken after merely *reading* the inbox would differ from one taken before — collapsing the “lazy aging has no observable side effect” argument that lets #46 skip a tick slot. Split into **ignore on read, compact on write** (`Append`), so a query provably writes nothing. **M-2** — §6 restated the same “no observable side effect” claim; re-derived from the read/write split rather than asserted. **L-1** — KD-9 still said “three zero counts” over four state groups: **the AR-1 fix for this silently no-op’d** because the string spans a line break, and the v0.2 history claimed a fix that had not landed. Applied for real, and recorded here because a fix claimed but not verified is worse than one not attempted. **L-2** — `InboxCursors`’ per-source array had no growth rule; pinned length-prefixed and APPEND-only. |
| v0.5 | July 26, 2026 | **AR-4 fix pass: 0H + 2M, all resolved.** **M-1** — §8.3 waved the match-item projector seam through as already provided by #35’s `ERR-030-013`, but that seam is #35’s *conference queue*: #46’s most basic item type would have depended on **#35 being approved**, silently contradicting §12 and KD-7’s independent-promotion claim. #46 now files **ERR-030-015** for the same site in its own right; the two coalesce if both land. **M-2** — §9’s read-key test still asserted AR-3’s superseded semantics (“a key whose item was evicted is gone”), which would fail against the ignore-on-read/compact-on-write design AR-3 replaced it with; restated as the stronger property — the blob is **byte-identical across a read**. |
| v0.6 | July 26, 2026 | **AR-5 sweep: 0H + 0M + 3L, all resolved — CONVERGENCE** (an L-only round closes the cycle). **L-1** — KD-1’s illustrative `InboxItem` block had drifted from §5’s authoritative one (no `INBOX_NO_SUBJECT`, no id-allocation note); annotated and pointed at §5 rather than duplicated. **L-2** — R-2 cited `MediaIntent` (which is #35’s enum, not #46’s) for the ordinal discipline; corrected to `ItemKind` / `InboxIntent` after AR-2’s split. **L-3** — §3’s minimal row said “no draw”, implying by contrast that the deep tier has one, while KD-8 makes #46 draw-free at **every** tier; the deep row now says so, and the contrast with #35 (whose deep tier genuinely does draw) is stated. |
