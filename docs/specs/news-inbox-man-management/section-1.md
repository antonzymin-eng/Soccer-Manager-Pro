# News, Inbox & Man-Management #46 — Section 1: Introduction, Scope, Dependencies, Key Decisions

**Created:** July 27, 2026
**Last Updated:** July 27, 2026 (v0.1 — initial section-file set)
**Version:** 0.1
**Status:** APPROVED

---

## 1.1 Purpose

#46 is the manager's **inbox**: an ordered, bounded, persisted log of the things that happened — results,
suspensions, board reactions, press items, transfer news — plus the read state that turns it into a feed
rather than an archive. At the deep tier it is also the **man-management** surface: talking to a player,
choosing what to say, and living with the consequence.

It is a spec that looks derived and is not. Two verification findings shape it (§1.4): the most basic
item type it could possibly carry — *"you drew 1–1 away to Everton on matchday 12"* — is **not
recomputable** from a loaded save, because #30 destroys the scoreline the moment the league table absorbs
it. And its morale consequence rides a seam its sibling #35 established one week earlier, whose
producer-specific shape does not survive #46 being producer number two.

Everything else about #46 is a boundary. It owns the log and the read marks; it owns no event, no morale
model, no text, and no screen.

## 1.2 Scope

**In scope**

- The **item log**: a persisted, ordered, bounded sequence of compact integer records.
- **Read state**: a watermark plus a bounded exception set (KD-6).
- **Man-management** (deep tier): talk-to-player interactions with a bounded morale consequence.
- The **`InboxIntent` roster** #46 contributes to #49's catalogue.

**Out of scope** — each already has an owner; duplicating it is the failure this section prevents:

| Not owned | Owner | #46's relation |
|---|---|---|
| The **events** themselves | #30 / #31 / #35 / #44 / #45 | projected into `InboxItem` **value copies** by root-side projectors (KD-2) |
| Press-conference logic — the question, the answers, the consequence | **#35** | #46 **shows** #35's items; it renders no press question of its own (KD-5) |
| The **morale model** and its state | **#33** (FR-HS-002) | #46 emits a bounded delta through the routed seam; it **never writes**, and never **reads** (KD-3) |
| Rendering text into a locale | **#49** | `InboxTextBoundary`, a sibling adapter (KD-4) |
| Match **statistics** | **#37** | #37 holds no state and is a **live-match** reader — so it cannot be called after the fact (§1.4(c)) |
| Rendering the inbox on screen | **#38** | #46 exposes read-only value copies |
| Cross-version save migration | **#50** | #46 declares its own version and fails loud |

## 1.3 Dependencies

**Upstream (consumed): none.** This is unusual enough to state plainly — **#46 is a leaf at every tier**
(KD-2). Items arrive as value copies **pushed** by root-side projectors, and its morale consequence leaves
as a committed integer the root drains. It references no producer, not #33, not #30, and not
`TacticalDirector.Localization`.

**Downstream (consumers):**

- **#33 Personalities & Morale** — receives #46's man-management delta as a committed integer on
  `HumanSystemsDayInput`, **summed with every other producer's and clamped by the root**. No reference in
  either direction.
- **#38 UI** — reads value copies; the rendered text comes from the adapter, not from #46.

**Reference DAG**

```
root → {#30, #33, #35, #44, #45, #46, boundary}        #46 → { }
boundary(InboxTextBoundary) → {#46, #49}
```

**Acyclic, and stronger than #35's** — which takes `DeterministicSim` at its deep tier. #46 takes nothing
at any tier, and that is what makes §5.8's structural assertion unconditional rather than tier-scoped.

## 1.4 What verification changed

Three findings from checking the plan against approved source. The first two are the spec's shape.

**(a) The inbox cannot be derived, because #30 does not retain what the items would say.** #30's
`Fixture` is `{ RoundIndex, HomeClubId, AwayClubId, Played }` — *"the fixture list is the immutable
schedule; `Played` is the only mutable-on-play field"* — and the resolved result is recorded **on the
table, not the fixture**. So after a fixture is played the **scoreline is gone**; only the aggregate table
row survives.

#44 hit the identical wall from the other side and recorded it: *"#30 retains no per-fixture ledgers …
so recompute-on-load has no input."*

**Consequence:** the plan's KD-4 premise — *"inbox items are largely a derived view over already-persisted
events … minimise new stored state"* — is **false for the single most common item type**. KD-1 inverts it,
following #44's forced-persistence precedent rather than #37's stateless one.

**(b) `EmitMatchOutcome` is the only moment an accurate match item can be captured.** #30 §3.4 runs, per
fixture: `Table.ApplyResult(result)` → `EmitMatchOutcome(result)` → `f.Played := true`, with FR-SN-017
pinning #30 as *"the **producer only**"*. The `result` in hand at that instant carries the scoreline.
That is what makes KD-1's "persist" answer **forced rather than chosen**.

**(c) #37 is not an inbox source.** FR-AN-020 — #37 *"MUST hold no persistent state"* — and FR-AN-021 —
it *"MUST consume **live during the match**"* and *"MUST NOT assume the serialized ledger bytes can be
re-parsed."* A post-match report in the inbox therefore **cannot call #37 after the fact**. Either the
root captures #37's view models at emission time alongside the scoreline, or post-match stats are not an
inbox item. Recorded here so the boundary is not discovered at implementation.

## 1.5 Key decisions

### KD-1 — The inbox is a persisted item log, not a derived view

§1.4(a) forces it: the scoreline an item reports is destroyed by #30's own design, so "derive on read"
yields an item that cannot state what happened. #46 therefore **stores** its items.

**What is stored is a compact integer record, never a rendered string** (§2.2 is authoritative for the
shape). `Payload` is a fixed small integer array whose meaning is fixed **per `(SourceTag, ItemKind)`** —
a match item carries `{ homeClubId, awayClubId, homeScore, awayScore, roundIndex }`. Storing integers
rather than text is what keeps the save locale-independent under FR-LC-006 (a stored string would bake
the locale the save was written in) and what lets the same item render differently after a language
change.

**`SourceTag` is #46's own namespace and must not be conflated with #49's `TextTemplateId.ProducerTag`.**
They overlap in neither membership nor meaning: #46's tags a *source of events* (#30 is one; #30 is not a
text producer at all), #49's tags a *text-producer family* (#22 is one; #22 emits no inbox items). Naming
both "producer tag" is how this project's `TacticTranslation` CS0104 collision and its duplicate
`PlayerAttributes` started, so they are named apart from the outset.

**Bounded by a retention window, not by a career.** `INBOX_RETENTION_DAYS` `[GT]`; items older than the
window are dropped. Without a bound the log grows monotonically across a twenty-season career, and an
inbox is a **recency** surface. This is a design position stated up front rather than discovered when a
save doubles in size; §7.4 R-1 records the tension with a player who wants career-long history.

*Rejected:* have #30 retain per-fixture results so the feed becomes derivable. It changes #30's `Fixture`
contract to serve a **presentation** consumer, adds a second truth beside the table, and #44 already
declined the symmetric move.

### KD-2 — #46 references nothing; producers are projected in at the root

Each producer's state is projected into `InboxItem` value copies by a small per-producer projector living
at the **`TacticalDirector.SeasonSave` root** — the assembly that already references everything, and the
same layering #33 uses for `RouteIntoLivingWorld` (*"owned by the SeasonSave root, NOT #30, NOT
living-world"*) and #49 uses for its boundary adapters.

Three consequences, all load-bearing:

- **#46 references no producer**, so it cannot drift into owning press logic (#35), discipline (#44), or
  board state (#45) — closed **structurally** rather than by discipline.
- **A producer never references #46** — the direction #35's KD-6 already asserts from its side. The two
  specs agree without either importing the other.
- **The phantom-consumer problem disappears.** With zero projectors wired the feed is empty and every #46
  surface is still exercisable. #46 does **not** wait on #31/#35/#45; each projector lands with its
  producer. This is the sharpest reason to prefer root projection: FR-LW-031 would otherwise bar #46 from
  being authored at all until its producers exist.

**Where each projector runs — this is the part KD-7 constrains, so it must be pinned.** A projector is
root-side code invoked at a **fixed, already-existing point owned by its producer**, never at a #46 step:

| Producer | Projector site | Why there |
|---|---|---|
| **#30** match result | inside §3.4, right after `EmitMatchOutcome(result)` | the only place the scoreline exists (§1.4(a)/(b)) |
| **#35** press | the #30 post-round path, after #35's own queue seam | the conference exists from that instant |
| **#44** discipline · **#45** board · **#31** transfers | immediately after **that producer's own step** in `RunWorldTickInFixedOrder` | the producer's day-state is final there, and the step is already pinned |

This is load-bearing. A naive reading of KD-7 (*"#46 takes no tick slot"*) would leave a **world-tick**
producer — a suspension incurred on a Tuesday, a board-confidence collapse — with **nowhere to emit**, and
the two obvious repairs are both wrong: give #46 a slot (contradicting KD-7), or silently drop non-fixture
items. Siting each projector at its producer's existing step resolves it: #46 still has no step of its
own, every emission point is inside the pinned fixed order, and a projector lands with its producer rather
than ahead of it. **Ordering is inherited from #30's tick order, not defined by #46.**

### KD-3 — Man-management routes through one producer-agnostic external-delta seam

#35 established the mechanism; #46 adopts it and generalizes its shape. `HumanSystemsDayInput` gains a
single `ExternalDeltaPermille` — **not** `MediaDeltaPermille` and **not** a second `InboxDeltaPermille`.

**Why one field and not two.** A per-producer field makes `HumanSystemsDayInput` grow with the number of
systems that can nudge morale, and every addition is a #33 back-prop against an approved struct. One field
with **summation at the root** costs nothing #33 can observe — it receives one committed integer either
way — and is **closed under new producers**. The sum is clamped to `[-1000, +1000]` **before** it reaches
#33, so two producers cannot compose past the contract's range; the clamp lives at the root, **with the
summation that creates the risk**, not inside #33.

**This resolves a real tension in #33's own text without weakening FR-HS-002.** #33 says both *"no other
assembly writes them"* (FR-HS-002) and *"#46 is the only consumer that writes #33 morale"* (FR-HS-024),
with §3.3 referring to *"#46's future man-management seam"*. Read together these permit exactly one
coherent shape: a **#33-owned mutation that #46 causes but does not perform.** All morale mutation stays
inside `AdvanceHumanSystemsDay`; #33 keeps exactly one write site; the F6 same-day-no-op / day-gap guard
covers external deltas for free; and the drift/clamp semantics are uniform across every input.
ERR-033-004 files the wording fix so a future implementer cannot pick the other reading.

*Rejected:* a #33-owned `ApplyManManagementDelta(ref MoraleState, …)` the root calls at command time. It
introduces a second mutation entry point that bypasses `ComputeMoraleTarget`, needs its own clamp and its
own idempotency story, and gives #33 **two write sites** where FR-HS-002's value comes from having one.
Immediacy is the only thing it buys, and the project has already accepted next-day application for board
(#45 KD-7) and media (#35 KD-3).

**#46 owns its own pending deltas**, exactly as #35 owns its own: each producer's undelivered deltas live
in its own sub-blob, and the root drains them all at step 3. A shared pending store would need an owner,
and no spec is the natural one.

**#46 never *reads* morale — and that is a requirement, not an omission.** FR-HS-024's read-accessor list
is *"#31/#35/#45"*; #46 appears in that requirement **only as the writer**. FR-HS-025 then bars two-way
coupling with any consumer outright (*"Morale is a projection OUT of #33 — no two-way coupling … avoids
determinism-ordering fragility"*). A #46 that both reads morale and causes a write is exactly the coupling
that forbids. **So a man-management outcome is a function of the chosen option alone, never of the
target's current morale.**

This needs saying because the natural feature — *"he's unhappy, so the reassurance lands harder"* — reads
as obviously desirable and would be implemented without a second thought. If a morale-sensitive outcome is
wanted later it must arrive as a **routed committed value** the root supplies into the interaction — the
same mechanism #46's own consequence uses in the other direction — never a #46-side accessor call.
Displaying a player's mood on the man-management screen is **#38's** read through #33's accessor, not
#46's.

### KD-4 — #49 binding: `InboxTextBoundary`, and two identity types

`InboxIntent` covers item headlines/bodies **and** man-management prompts/options in one roster, with
slots disjoint from #35's, a sibling `InboxTextBoundary` adapter at the boundary layer, the FR-LC-015
intent-value pre-gate, and FR-LC-008a coverage over #46's roster. **#46 never references
`TacticalDirector.Localization`** (FR-LC-012 makes that a build error).

**Two identities, deliberately separate — and the split is the point.** `ItemKind` is the **serialized**
identity (it, with `SourceTag`, fixes the `Payload` schema); `InboxIntent` is the **catalogue** identity
the adapter maps to a `TextTemplateId`. The adapter's job is exactly the mapping
`(SourceTag, ItemKind) → InboxIntent`, **so a saved item never stores a localization ordinal.**

This differs from #35, which collapsed to a *single* `MediaIntent`, and the difference is load-bearing:
#35's records are all its own, while **#46's items come from five sources whose kinds it does not own** —
forcing them through one intent enum would make adding a #31 item kind an edit to #46's localization
roster.

The cost of the split is that **both** carry an **ORDINAL STABILITY — APPEND-only** contract, for two
distinct reasons:

- reordering **`ItemKind`** re-reads every saved `Payload` **under the wrong schema**;
- reordering **`InboxIntent`** re-points every item at the **wrong template**.

Neither has a version gate that would catch it, so both are pinned and asserted.

**The selection value.** #46's minimal tier is draw-free (KD-8), so it supplies its `ulong` the way #35
does — a local keyed mix — and therefore **inherits #35's `ERR-049-001` dependency** (FR-LC-020 binds that
field to #22's `world.text` draw). If that back-prop is refused, #46 takes the same `SelectionDraw = 0`
fallback. **#46 files nothing here**; it is the second spec blocked on one #49 wording fix, which is
itself the argument for making it.

### KD-5 — Boundary with #35: #46 shows, #35 owns

#35 owns which conference is queued, its question, its answer set, and its consequence. #46's projector
reads #35's read-only conference query — the surface #35's KD-6 already exposes for exactly this — and
produces an item. **#46 renders no press text of its own and defines no press intent**: a press item's
text identity is #35's, carried in the item and rendered through #35's adapter.

Enforced by KD-2's reference direction rather than by convention.

### KD-6 — Persistence: an opaque sub-blob, and a query that never mutates

`INBOX_SAVE_FORMAT_VERSION` [FIXED] = 1 — the item log, the read marks, #46's pending man-management
deltas, and its cursors, composed into #30's `SeasonSaveCodec`, **not** a `WORLD_STORE_FORMAT_VERSION`
bump (the #40/#42/#44/#45/#35 pattern). Version gate first; overflow-safe length prefixes against
`total − offset`; trailing-byte guard; fail loud on all three. **APPEND-only** layout.

**Read state is a watermark plus an exception set**, not a flag per item: everything older than
`ReadBeforeWorldDay` is read, and `ExplicitReadKeys` carries only items read out of order within the
window, collapsing into the watermark as it advances. A per-item flag would grow a byte per item forever
and make *"mark all read"* an O(n) rewrite of the whole blob.

**The exception set is bounded by the log, not merely by the watermark.** A key whose item is no longer in
the log — evicted by the retention window or by `INBOX_MAX_ITEMS` — is dead. Without that bound a player
who reads out of order for twenty seasons accumulates keys for items that no longer exist, and the set
becomes the unbounded growth the watermark design exists to avoid.

**A dead key is *ignored* on read and *compacted* on write — a query never mutates.** This is
load-bearing against KD-7: retention is evaluated **lazily at query time**, so if pruning were a
query-time mutation, reading the inbox would change persisted state and the *"lazy aging has no observable
side effect"* argument would collapse — a save taken after a read would differ from one taken before.
Instead a query **filters** dead keys out of its answer and writes nothing; the persisted set is compacted
at the next `Append` — a write that is already happening, at a point fixed by the producer's step rather
than by when a human opened a screen.

**Deliberately absent:** any RNG cursor (KD-8), any rendered string (KD-1), and any copy of morale, the
table, or a producer's own state. An item's `Payload` is a **snapshot of what was true at emission**,
which is the point, and is never re-synchronised against the producer afterward.

### KD-7 — No tick slot — and this is a real difference from #35

#46 needs no position in `RunWorldTickInFixedOrder`:

- **Retention aging is computed lazily**, at query time. Dropping a stale item has **no consequence** —
  nothing downstream reads it, no delta is produced, no other state moves — so a lazy sweep cannot make
  state depend on whether the client looked. **This is precisely the argument #35's KD-5 could not
  make**, because its expiry produces a no-comment consequence. The same technique is correct in one
  spec and wrong in the other, for a reason that is checkable rather than stylistic.
- **Man-management is command-driven** (`TryTalkToPlayer`), like #35's answer and #31's `SubmitBid`.
- The **step-3 drain** of pending deltas is #30's existing seam, generalized by KD-3 to iterate every
  external-delta producer rather than #35 alone.

**#46 cites no step number of its own, which is the precise form of its independence from the #30
tick-order repair** that gates #35. Note this is **narrower** than *"#46 is unaffected by the tick
order"*: KD-2's world-tick projectors run **inside** the fixed order at their producers' steps, so #46's
emission ordering is inherited from whatever that order settles as. What #46 avoids is having a number
**in** it — nothing in #46 has to be renumbered when the repair lands, and #46 can be promoted while it is
still open.

### KD-8 — Draw-free at every tier; no #16 row at all

No stochastic decision anywhere. Item order is a **total order** on `(WorldDay, SourceTag, ItemId)` —
canonical and tie-free by construction, since `ItemId` is unique within a producer — and a man-management
outcome is a deterministic function of the chosen option. #46 registers no stream and promotes no tag.

**#16 is untouched, and #46 has no reserved value to promote later.** The roadmap §6 block runs
`0x20`–`0x2D` covering #28–#36 / #40–#45, and #16 §3.4 records that *"#37–#39 are
read-only/presentation/infra and take no tag."* #46 is the same class, and the catalogue accordingly has
no `_RESERVED_` row for it. **Stated because it is an asymmetry with #35**, which *does* have `0x27`
reserved: a future stochastic news generator in #46 would need a **fresh allocation**, not a promotion.

### KD-9 — Behaviour-neutral identity, at two scopes

**With no projector wired:** the feed is empty, no `InboxItem` is stored, the sub-blob is a version header
plus four zero counts (items, read marks, pending deltas, cursors), no stream is registered ⇒ every
existing cursor is byte-identical, and `ExternalDeltaPermille = 0` ⇒ `ComputeMoraleTarget` is unchanged. A
season advanced with #46 present is byte-identical to the same season pre-#46 at the #33 and #30 seams.

**With projectors wired but man-management off** — which *is* the minimal tier — the same holds at the
**#33** seam specifically: #46 stores items and changes nothing anyone simulates from. The save frame
differs (it carries the sub-blob); the simulation does not.

Stating both scopes matters, because the first is a T0/T1 property and the second is the one that holds
for the shipped minimal tier.

## 1.6 Determinism posture

- **Command-driven and root-projected; no tick slot** (KD-7). Never the 10 Hz tactical or 60 Hz physics
  loops, and #46 feeds no digest.
- **Draw-free at every tier** (KD-8) — including the deep tier, unlike #35's.
- **All-integer**; no float, and **no stored string**, so state is locale-independent (FR-LC-006).
- Item order is **total and canonical** on `(WorldDay, SourceTag, ItemId)`.
- Retention and `INBOX_MAX_ITEMS` eviction are a **pure function of the stored log**, evaluated lazily on
  read — which is sound precisely because a read writes nothing (KD-6).

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-27 | — | Initial §1 (scope, out-of-scope table, leaf DAG, §1.4's three verification findings, KD-1..KD-9 from supplement v0.6, determinism posture). KD-9 split into its two distinct scopes — no-projector (T0/T1) and projectors-wired-but-man-management-off (the shipped minimal tier) — since only the second describes what actually ships. Status IN REVIEW. |
#endregion
