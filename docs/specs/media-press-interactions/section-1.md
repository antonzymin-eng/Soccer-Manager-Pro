# Media & Press Interactions #35 — Section 1: Introduction, Scope, Dependencies, Key Decisions

**Created:** July 27, 2026
**Last Updated:** July 27, 2026 (v0.2 — PASS-1 fix pass)
**Version:** 0.2
**Status:** APPROVED

---

## 1.1 Purpose

#35 models the **press** as a structured interaction: a season event queues a conference, the conference
poses a question, the manager picks from a bounded set of answers, and the answer produces a **committed
consequence value** that other specs act on.

It is a small spec with an unusually large number of boundaries, and that is the point. Everything a
press-conference feature is *tempted* to own — the words, the morale, the reputation, the inbox — already
has an owner. #35's job is to be the thing in the middle that owns none of them: it emits an **identity
plus values** and lets the specs that own the models decide what to do with them.

Two of the five key decisions in #35's own one-page plan **do not survive verification against source**,
because the plan was written one day before Localization #49 and Personalities #33 were approved. §1.4
records what changed. That correction is the single most load-bearing thing in this specification, and it
is what makes #35 *smaller* than planned rather than larger: it loses a `living-world` dependency and
gains a boundary adapter.

## 1.2 Scope

**In scope**

- The **press-conference lifecycle**: queue → offer → answer (or expire) → consequence.
- The **`MediaIntent` roster** — one enum covering both question archetypes and answer-option phrasings
  (KD-1), which is #35's contribution to #49's template catalogue.
- The **committed consequence value** an answer produces, and its delivery contract.
- A **read-only query** over conference records, which is how #46 discovers media items (KD-6).

**Out of scope** — each already has an owner; duplicating it is the failure this section prevents:

| Not owned | Owner | #35's relation |
|---|---|---|
| Rendering text into a locale | **#49** (FR-LC-001/004) | #35 emits a template identity + native slots + a `ulong`; a **sibling boundary adapter** renders (KD-1) |
| The template / phrase **corpus** | **#49** `TemplateCatalogue` | #35 supplies the roster its base-locale rows must cover (FR-LC-008a) |
| The **morale model** and its state | **#33** (FR-HS-002) | #35 **never writes** morale — it emits a committed delta #30 routes into #33's own day step (KD-3) |
| Man-management **morale writes** | **#46** (FR-HS-024) | #35 is not a second man-management path |
| Board **confidence** | **#45** | a deep-tier routed value, never a #35 field (KD-4) |
| The **inbox** that surfaces media items | **#46** | #46 reads #35; #35 never references an inbox (KD-6) |
| Match facts (result, table position) | **#30** / **#37** | consumed as committed values routed at #30's call seam |
| Any **reputation** scalar | *nobody yet* | #35 declines to create one (KD-4) |
| The interaction text generator `InteractionTextGenerator` | **#22** | **not consumed** — superseded by the #49 seam (KD-1) |

## 1.3 Dependencies

**Upstream (consumed):**

- **#49 Localization & Accessibility** — the text seam #35 produces into. **Not an assembly reference**:
  FR-LC-012 makes referencing `TacticalDirector.Localization` from a sim assembly a build error, so the
  binding is a *sibling boundary adapter* (KD-1).
- **#30 Season & Competition Loop** — invokes both #35 seams and routes committed values in.
  **Reference direction is `#30 → #35`; #35 never references #30.**
- **#16 Deterministic Simulation** — **deep tier only**, for `DeterministicRngService` at the first real
  draw. The minimal tier references **nothing at all** (§1.6).

**Downstream (consumers):**

- **#33 Personalities & Morale** — receives #35's delta as a committed input field on
  `HumanSystemsDayInput`, assembled by #30. **No reference in either direction.**
- **#46 News & Inbox** — reads #35's conference query (KD-6). `#46 → #35`, one-directional.
- **#45 Board & Ownership** *(deep)* — receives a board-facing signal as a routed value.
- **#38 UI** — reads value copies for display; the rendered text comes from the adapter, not from #35.

**Reference DAG**

```
root → {#30, #35, boundary}      #30 → {#33, #35, …}      #35 → { }  (minimal)  →  {#16} (deep)
boundary(MediaTextBoundary) → {#35, #49}
```

**Acyclic at every tier, and #35 is a leaf at the minimal tier.** At no tier does it reference #30, #33,
#45, #46, #49, `living-world`, `SeasonSave`, or `MatchEngine`.

## 1.4 What verification changed (the re-basing)

Three findings from checking the plan against approved source. They are recorded here rather than in an
appendix because two of them *are* the spec's shape.

**(a) The text path is #49's, not #22's.** #49 (APPROVED July 23) pins that a producer must not emit a
baked localized string (FR-LC-002), that procedural text renders through `ILocalizer.Render` from a
`LocalizedTextRequest` (FR-LC-004), that **no sim assembly may reference the localization assembly**
(FR-LC-012), and that a producer binds by adding a **sibling boundary adapter** (FR-LC-013/014). #49's own
§7.3 names #35's adapter in advance: *"a new boundary adapter (`MediaTextBoundary`, `InboxTextBoundary`)
is added when that producer is built."*

So the plan's *"build strictly as a consumer of #22's `InteractionTextGenerator`"* is answering a question
that no longer exists. This is a **simplification**: #35 gains a producer tag and an adapter, and loses a
`living-world` dependency. Reusing #22's generator would additionally have bound #35's slot shape to #22's
four match-specific fields — which FR-LC-014 explicitly says the producers do **not** share.

**(b) #35 cannot write morale, by an approved MUST.** #33's FR-HS-002 makes #33 the sole writer of
`MoraleState`, and FR-HS-024 states *"#46 is the only consumer that writes #33 morale."* The plan's
obligation to *"define the exact morale-write direction into #33"* is therefore **unsatisfiable as
written**. The consequence must travel as a **value**, and #33's own committed-input mechanism
(`HumanSystemsDayInput`, consumed by `ComputeMoraleTarget` at #30 tick slot 3) already exists for exactly
this — it is how #33 receives #30's match results without referencing #30. See KD-3.

**(c) `FR-LC-020` is written for one producer and contradicts #49's own extension point.** It requires
`LocalizedTextRequest.SelectionDraw` to be *"the `ulong` returned by `DeterministicRngService.DrawReserved`
(the `world.text` reservation)"* — a MUST on the **generic core seam** naming a **specific producer's
stream**. It cannot be satisfied by any producer that is not #22, and it contradicts three things in the
same approved spec: §7.3's *"if they draw"* (which contemplates a producer that does not draw at all),
FR-LC-013/014's producer-agnostic core, and FR-LC-005's rule that the renderer must not draw at all — so
the seam's legitimate interest is that the value be deterministic and locale-independent, not which stream
produced it.

The clause was correct when #22 was the only producer. **#35 is the first second producer, so #35 is where
it surfaces.** This is a #49 defect, not a #35 constraint; ERR-049-001 generalizes it. KD-2 depends on it
and records a fallback in case #49's owner declines.

## 1.5 Key decisions

### KD-1 — #35 is a #49 producer, not a #22 consumer

#35 emits, per rendered item: a **`MediaIntent` enum value**, its own **disjoint** native slot values, and
a `ulong` selection value. A **sibling boundary adapter `MediaTextBoundary`** maps the intent to a
`TextTemplateId (ProducerTag = Media, LocalOrdinal = (int)intent)`, formats the slots, and calls
`ILocalizer.Render`. The adapter lives in the boundary layer beside #22's `LivingWorldTextBoundary`, and is
the *only* thing that references both #35 and `TacticalDirector.Localization`.

**`MediaIntent` is the single identity type.** #35 defines no parallel "template id" of its own; the
`(tag, ordinal)` pair is the adapter's construction, exactly as `LivingWorldTextBoundary.ForInteraction`
builds it from `InteractionIntent`.

**"Per rendered item" is two rosters, not one — the question *and* every answer option.** A press
conference is a question plus a bounded set of phrased answers the manager chooses between, and the option
labels are user-facing text, so FR-LC-002 binds them exactly as it binds the question. #35 therefore
carries **one `MediaIntent` roster covering both**, split by a `[FIXED]` ordinal band, so a single coverage
assertion covers both and the adapter needs one mapping. A conference record names its question intent
plus its ordered option intents, and `optionIndex` keys both the rendered label and the consequence (KD-8).

Modelling only the question would leave the client with no compliant way to display the choices, and the
pressure would be to bake them — which is the one thing #49's coverage-lock exists to prevent.

Three structural properties fall out, all testable rather than aspirational: **#35 never references
`TacticalDirector.Localization`** (FR-LC-012, asserted by reference-absence); **#35 never emits a display
string** (FR-LC-002); **#35 never references `living-world`**.

**#35's own obligations under the seam:** a pre-render roster gate on the intent **value** (FR-LC-015 —
refuse `None`/undefined *before* any selection work), and a catalogue-coverage assertion extending
FR-LC-008a to #35's roster.

**Deferred, not designed:** the citation clause (`HasCitedEpisode` / `CitationKind`). A press question
referencing a remembered episode would need #22's `MemoryStore`, re-introducing exactly the dependency
this decision removes. `HasCitedEpisode = false` at every tier until a deep-tier decision re-argues it.

### KD-2 — Draw-free minimal ⇒ `_RESERVED_0x27_` stays RESERVED

The minimal tier selects one archetype per trigger event — a pure function of the trigger — so there is
**no stochastic decision**. #35 registers no RNG stream and promotes no domain tag at approval (the
#29/#40/#31/#34/#32/#43/#45 precedent). `_RESERVED_0x27_` / `SubsystemOrdinals 89` **already exist and are
already correct**, added by the A-04 gap sweep at #33's approval — so this is a decision with **zero #16
paperwork**, unlike #45, which had to file its own placeholder.

**The `ulong` FR-LC-004 requires is a local keyed mix, not a draw** — *conditional on ERR-049-001*
(§1.4(c)). #35 computes it with a **local SplitMix64** over `(intentOrdinal, worldDay, subjectId,
purpose)`. The precedent is explicit and in-tree: `FixtureScheduler` and `LeagueBootstrap` each carry a
**local** SplitMix64 rather than allocate a domain tag. The mix is position-independent, so **nothing is
serialized**, replay is cursor-free, and phrasing variety survives without a stream.

The helper is genuinely local: `SplitMix64` exists in `src/deterministic-sim/` only *inside*
`DeterministicRngService.cs`, not as a shared public primitive — which is why both existing users copied
it, and why #35's minimal tier needs **no assembly reference at all**.

*Rejected:* register a `media.selection` stream at the **minimal** tier and draw, satisfying FR-LC-020 as
literally written. It manufactures a stochastic surface for a decision with no randomness in it, purely to
satisfy a clause written when #22 was the only producer; it promotes `0x27` with no genuine draw site,
which is the phantom-surface class FR-LW-031 forbids; and it re-opens the persisted-cursor question KD-7
otherwise closes.

**If ERR-049-001 is refused**, the fallback is narrower than that rejected alternative: #35 supplies
`SelectionDraw = 0` and pins variant `0` per intent (FR-LC-007 is total at `draw = 0`), losing phrasing
variety at the minimal tier and regaining it at the deep tier's real draw. Recorded so the spec does not
depend on a back-prop being granted.

**When the deep tier does promote `0x27`, the model is pinned now:** **one** stream for the whole
subsystem — siteId `media.selection`, `entityId = MEDIA_STREAM_ENTITY_SENTINEL` — registered once
regardless of club or player count, with the entity folded into a fixed-radix **keyed action ordinal** on
`(subjectId, worldDay, purpose)`. `RegisterStream` appends into a bounded, never-shrinking table
(`MaxRngStreams` = 64, no unregister), and #42 §7.4 R-1 records that a per-entity registration model
exhausts it in a full-world career. **#35 contributes exactly one registration at any tier** — recorded so
the choice is not later "simplified" into a per-club model.

### KD-3 — The consequence is a committed value routed through #30, never a morale write

Per §1.4(b) this is forced, and the mechanism already exists. On answering, #35 records a bounded delta
against a `PlayerId` in its own state. #30's tick step 3 already builds a `HumanSystemsDayInput` per
player; it folds #35's pending delta into that struct as a committed field, and `ComputeMoraleTarget`
consumes it as an additive term exactly as it consumes `BoardObjectiveDeltaPermille` today.

**#33 remains the sole writer of its own state** (FR-HS-002 intact), and FR-HS-024 stays literally true —
#35 is a read-only consumer that never touches morale; it supplies an *input* to #33's own writer.

`0` is the neutral value and `HumanSystemsDayInput.Neutral` keeps it, so the field is **behaviour-neutral
until a non-zero delta is delivered** — not merely "until a conference is answered", since expiry answers
conferences and a `0`-consequence answer records nothing at all (§2.2). The struct is transient, so there
is **no `HUMAN_SYSTEMS_SAVE_FORMAT_VERSION` implication**.

**The field is producer-agnostic: `ExternalDeltaPermille`, not `MediaDeltaPermille`.** #46 is the second
producer of exactly this quantity, so a per-producer name does not survive — producer #3 would need a
third field on an approved struct. The root **sums across producers and clamps** before it reaches #33
(ERR-033-003, filed jointly with #46). The mechanism #35 chose is unchanged; only the name and arity move.

*Rejected:* back-prop FR-HS-024 to add #35 as a second writer — it widens a MUST approved days earlier, and
two writers into morale is precisely the "media becomes a second morale engine" risk. *Rejected:* route
the delta through #46's man-management write path — #46 is authored after #35, and its write path is
scoped to *talk-to-player*, not to applying another producer's deltas.

**#30 touches #35 at *two* points in the day, and both must be specified.** The drain happens at **step
3**, where #30 assembles each player's `HumanSystemsDayInput` — *not* at #35's own expiry seam, which only
expires (KD-5). Both are filed (§8.1). Filing only the expiry seam would produce an implementation where
deltas are recorded and never delivered, **and every #35-local test would still pass.**

**The delivery contract, pinned rather than implicit.** A delta is delivered at the **first step 3 that
follows the answer command**, and cleared there. Answers are commands issued *between* world ticks — the
day loop is synchronous, `RunWorldTickInFixedOrder` runs to completion per day — so in the normal flow a
conference answered after day *D*'s fixture is delivered on day *D+1*. The lag is a property of *when the
command lands relative to the tick*, not of slot ordering, and it is deterministic under replay because
the command sequence is part of the replayed input.

The alternative — deliver same-day by re-running step 3 after an answer — is **barred**, not merely
undesirable: it would make morale's day step non-idempotent, which #33's own F6 guard
(`worldDay == LastAdvancedWorldDay` ⇒ no-op) forbids outright, so the delta would be silently **dropped**
rather than applied. The one-day contract is the only shape #33's guard permits. It matches #45's
board→morale lag and #23's one-stride-stale dismark carriers.

### KD-4 — #35 introduces no reputation scalar

**There is nothing upstream to reuse.** A tree-wide search for *reputation* across `docs/specs/**/section-*.md`
returns exactly three hits, none of them state: two prose/deferral notes in `living-world`, and
`youth-academy-intake/section-7.md` — which explicitly **disowns** one: *"A #42-owned reputation, morale, or
finance field. Those belong to their owners."*

So the plan's question is really *"may #35 create one?"*, and the answer is **no, at any tier #35 owns**. A
manager/club reputation is consumed by transfers (#31), youth intake (#42), staff hiring (#34), board
patience (#45) and national-team selection (#36); a scalar invented inside a press-conference spec would
become five specs' truth by accident.

**What #35 does instead:** the minimal consequence is the single morale delta of KD-3. The board-facing
half is a **deep-tier routed value** into #45's existing `BoardDayInput` — which already carries a
deep-tier `MoraleSignalPermille` neutral at minimal, the same shape — filed as a **deferred** back-prop,
never a #35 field.

If a genuine reputation system is later wanted, it is its own spec, or **#45's** — which already owns a
club-scoped persistent relationship scalar and the drift machinery for one. Recorded in §7.4 as a standing
option, not a debt.

### KD-5 — #30 queues; the manager's answer is a command

Three paths, deliberately not merged:

- **Queueing is event-driven at #30's §3.4 post-round path.** After `EmitMatchOutcome(result)`, a null
  seam offers the committed result to #35, which may enqueue at most one conference for the managed club.
  **Non-managed fixtures never queue** — there is no manager to ask.
- **Answering is command-driven** — `TryAnswerQuestion(conferenceId, optionIndex) → bool`, invoked from
  the client (the #31 `SubmitBid` precedent). It is **not** a tick step: a conference the player never
  opens must not auto-answer itself.

  **It returns `false` rather than throwing on an already-resolved conference, and that distinction is
  load-bearing.** The client renders a conference list; the tick's expiry sweep can resolve a conference
  *between* that render and the player's click. Answering an already-expired conference is therefore a
  **legal race, not malformed input** — fail-loud there would crash a career on an ordinary click.
  Genuinely malformed input still fails loud: an unknown `conferenceId`, an `optionIndex` outside the
  conference's own option roster. This is #45's `TryProjectBoardModifier` distinction — *"not applicable"*
  is a named legal state, corrupt input still throws — applied to the one #35 surface a human drives.
- **The daily tick seam does one thing: expiry.** An unanswered conference past its deadline resolves to
  its designated *no-comment* option (a defined answer with its own delta, frequently `0`), so the queue
  is bounded and a save can never accumulate stale conferences. That is the only reason #35 takes a tick
  slot at all.

*Rejected:* expire **lazily** on read, taking no tick slot at all — which would also drop the §8.0
prerequisite from #35's critical path. Rejected because expiry produces a *consequence*, so a lazy scheme
makes state depend on whether and when the client happened to read: a career where the player never opens
the inbox would diverge from one where they did, which is not replayable. The eager sweep costs one `uint`
comparison per pending conference per day against a bounded queue.

### KD-6 — #46 discovers items by reading #35; #35 never references an inbox

Strictly one-directional: **`#46 → #35`**. #35 exposes a read-only query over its own conference records
(pending / answered, with the answered option and its committed delta); #46's aggregator reads it, exactly
as #37 and #44 read the engine's ledger without the engine knowing they exist. #35 fires no inbox event,
holds no unread flag, and **never references #46** — asserted by reference-absence.

This also answers *"how does #46 discover it?"* without inventing a message bus: the question only looked
hard while media was assumed to **push**.

### KD-7 — Persistence: an opaque, independently version-gated sub-blob

`MEDIA_SAVE_FORMAT_VERSION` [FIXED] = 1. #35's state — the pending queue plus the answered-conference
records carrying undelivered deltas — lands as its own sub-blob composed into #30's `SeasonSaveCodec`,
**not** a `WORLD_STORE_FORMAT_VERSION` bump (the #40/#42/#44/#45 pattern; the composite is #22-owned). The
outer codec never parses it. Version gate read **first**; overflow-safe length prefixes compared against
`total − offset`; trailing-byte guard; fail loud on all three. **APPEND-only** layout.

**Deliberately absent:** any RNG cursor (KD-2 leaves none to persist), and any copy of morale, board
confidence, the table, or a **rendered string** — mirroring any of the first three would re-introduce a
double truth, and a stored string would additionally break FR-LC-006's locale-independent-state rule (a
save must not depend on the locale it was written in).

**The undelivered-delta invariant is the one thing this blob exists for.** A delta recorded after a tick is
consumed at the next step 3 (KD-3), so a save taken between them **must** carry it. It is stored with the
`worldDay` it was recorded on, and delivery clears it — which makes *"was this delta applied?"* a
serialized fact rather than an inferred one, and makes double-delivery across a restore impossible.

**A pending delta names a `PlayerId`, so it needs a roster-lifecycle rule** — the same one #33 needed and
wrote down as FR-HS-027 (regen inserts / retirement removes, in lockstep with #28's season-boundary churn),
and which #31 extends by **re-keying** a club-scoped `PlayerId` on transfer. Without a rule, an undelivered
delta targeting a retiring player is never drained (nothing iterates him at step 3) and lives in an
APPEND-only blob forever, and one targeting a transferred player could be delivered to **whoever now holds
that id**. The rule: an undelivered delta whose target leaves the managed roster is **dropped** at the same
boundary #33 drops that player's entries — never migrated. A press reaction to a departed player has no
subject left, so dropping is semantically right as well as safe, and it keeps #35 out of #31's re-key
protocol entirely.

### KD-8 — Consequence scope: one code path at both tiers

An answer's consequence is a **list of `(targetKind, targetId, deltaPermille)`** at every tier. Minimal
ships **zero or one** entry: one with `targetKind = Player` when the conference has a subject, and **zero**
when it does not (`SubjectPlayerId == MEDIA_NO_SUBJECT` — a result or board question, whose answers are
text-only at minimal because the only minimal target kind is `Player` and there is no player to name).

The deep tier's squad/board spread adds **entries**, not code paths or branches, and is where a
subject-less conference acquires a consequence. The bound `MEDIA_MAX_CONSEQUENCE_TARGETS` is `[GT]` and
enforced at the authoring boundary, so a deep-tier catalogue row cannot silently make one answer touch the
whole roster.

**Stating the zero case matters.** Without it, an implementer meets the first subject-less conference and
picks a fallback — a `0`-delta entry against `PlayerId 0`, which is a **real player**, being the obvious
wrong one.

### KD-9 — Behaviour-neutral identity, stated precisely

With no conference queued, **or** with every answer's consequence `0`:

- no stream is registered ⇒ every existing stream's cursor is **byte-identical**;
- `HumanSystemsDayInput` carries `ExternalDeltaPermille = 0` ⇒ `ComputeMoraleTarget` is unchanged;
- a season advanced with the #30 media seams null is byte-identical to the same season pre-#35 (the
  FR-SN-026 world-floor property).

**Note what the precondition is not.** It is *not* "no conference is answered": expiry resolves an
unanswered conference to its no-comment option, and that **is** an answer. A test written to the weaker
precondition would either fail or silently prove less than it claims — an error this document made three
times across its own review cycle before it was pinned here.

### KD-10 — The `MediaIntent` ordinal is doubly load-bearing

`MediaIntent` carries an **ORDINAL STABILITY** contract: **APPEND-only, never reordered.** Its ordinal is
load-bearing twice over:

1. it is **serialized** inside `PressConference` (both the question intent and every option intent), and
2. it is the **`LocalOrdinal` half of the `TextTemplateId`** the #49 catalogue is keyed on (KD-1).

Reordering or inserting a value therefore silently re-points every saved conference at a different
template **and** invalidates every catalogue row at once — **with no version gate to catch it.** The save
loads cleanly and renders the wrong text, which is the worst available failure shape: no error, no crash,
just a manager whose answers no longer mean what he said.

New values are **appended**; a retired value keeps its ordinal and its base-locale row. This is the same
contract `CancelReason` and `PassType` carry, for the same reason (ordinals embedded in persisted or hashed
data). It is also why the question/option split is a **documented ordinal band with a `[FIXED]` boundary
constant** rather than an informal convention — the boundary is asserted, so "add a question" cannot
quietly shift the options.

## 1.6 Determinism posture

- **World tick + the #30 post-round path only**; never the 10 Hz tactical or 60 Hz physics loops. #35
  feeds **no digest at all** — it never touches the match engine.
- **Minimal tier: draw-free.** The FR-LC-004 `ulong` is a local keyed SplitMix64 mix (KD-2), so nothing is
  serialized and replay is cursor-free.
- **Deep tier:** one stream, keyed position-independent draws, **no persisted cursor**.
- **All-integer arithmetic**; no float, and no string, enters #35's state at any tier.
- Expiry is a `worldDay` comparison — same-day re-run is a **no-op**, a day **gap** is **fail loud** (the
  #33 F6 guard, adopted verbatim, implemented from the `LastAdvancedWorldDay` cursor in §2.2).
- **Rendering is display-side and runs strictly after the deterministic decision** (FR-LC-005), so locale
  cannot perturb #35's serialized bytes (FR-LC-006).

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-27 | — | Initial §1 (scope, out-of-scope table, dependencies + tiered DAG, §1.4's three verification findings, KD-1..KD-9 from supplement v0.7, determinism posture). Status IN REVIEW. |
| 0.2 | 2026-07-27 | — | PASS-1 fixes. **M:** **KD-10** promoted from a paragraph inside KD-1 to a key decision — the `MediaIntent` ordinal contract is a *save-correctness* property with no version gate behind it, and burying it under the text-seam decision is how it gets missed by a reviewer scanning the KD list. **L:** KD-3 now carries the `ExternalDeltaPermille` producer-agnostic naming inline (v0.7 recorded it only in the back-prop table, so §1 still read `MediaDeltaPermille`); KD-9's *"what the precondition is not"* note records that the document itself made that error three times, which is the honest reason it needs pinning. |
#endregion
