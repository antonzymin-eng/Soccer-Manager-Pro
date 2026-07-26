# Media & Press Interactions #35 — Design Supplement

> **Created:** July 26, 2026
> **Last Updated:** July 26, 2026 (v0.6 — AR-5 sweep: 0H+0M+3L, **CONVERGENCE**; prior v0.5 AR-4, v0.4 AR-3, v0.3 AR-2, v0.2 AR-1, v0.1 initial)
> **Version:** 0.6
> **Status:** DESIGN SUPPLEMENT (pre-promotion — no section files, no `SPEC_INDEX.md` row)
> **Candidate spec:** **#35** · **FR prefix:** `FR-ME` · **Wave:** 6 · **Tier:** S4
> **Promoted from:** `docs/tracking/spec-plans/spec-35-media-press-interactions.md` v0.1

---

## 0. Purpose and posture

This supplement resolves the five key decisions the #35 plan defers, against **verified** upstream source
rather than assumption. It is the stage `board-ownership-dynamics-design.md`, `staff-backroom-design.md`,
and `tactical-instruction-layer-design.md` occupied before promotion: design only — no code, no section
files, no registry row.

Every claim about an upstream spec in §2 was checked against that spec's own section files (or, where the
subsystem is built, its source); the citations name file and requirement so a reviewer can re-verify
without trusting this document.

**Two of the plan's five key decisions do not survive that check.** The plan was written July 22, 2026 —
one day before Localization #49 and Personalities #33 were approved — and its KD-1 rests on both. §2(a)
and §2(b) record what changed; KD-1 and KD-3 are the corrected answers. This is the supplement stage doing
its job, and it is the reason the plan is a plan.

## 1. Scope

**#35 owns:** the **press-conference lifecycle** — which conference is queued by which season event, the
**question archetype** selected for it, the bounded **answer set** offered, and the **committed consequence
value** an answer produces.

**#35 does not own** (each already has an owner, and duplicating it is the failure mode this section exists
to prevent):

| Not owned | Owner | How #35 relates |
|---|---|---|
| Rendering text into a locale | **#49** (FR-LC-001/004) | #35 emits a template identity + native slots + a `ulong`; a **sibling boundary adapter** renders (KD-1) |
| The template/phrase **corpus** | **#49** `TemplateCatalogue` | #35 supplies the roster its base-locale rows must cover (FR-LC-008a) |
| The **morale model** and its state | **#33** (FR-HS-002) | #35 **never writes** morale — it emits a committed delta #30 routes into #33's own day step (KD-3) |
| Man-management **morale writes** | **#46** (FR-HS-024, in terms) | #35 is not a second man-management path (KD-3) |
| Board **confidence** | **#45** | a deep-tier routed value, not a #35 field (KD-4) |
| The **inbox** that surfaces media items | **#46** | #46 reads #35; #35 never references an inbox (KD-6) |
| Match facts (result, table position) | **#30** / **#37** | consumed as committed values routed at #30's call seam |
| The interaction **text generator** `InteractionTextGenerator` | **#22** | **not consumed** — superseded by the #49 seam (KD-1) |

## 2. What already exists (verified)

This is the load-bearing section. (a) and (b) between them rewrite two of the plan's five key decisions;
(d) is a defect in already-approved text that #35 cannot cite around.

**(a) The plan's "#22 `InteractionTextGenerator` consumer" framing is superseded by #49.**
`localization-accessibility/section-2.md` (APPROVED July 23, 2026) pins:

- **FR-LC-002** — a producer MUST NOT emit a baked, human-readable localized string; it emits an identity
  key or its **native procedural values**.
- **FR-LC-004** — procedural text renders via `string Render(in LocalizedTextRequest req)`, the request
  carrying a `TextTemplateId`, the `ulong` selection draw, the slot facts, and the citation clause.
- **FR-LC-007** — `variant = draw % variantCount(BaseLocale, Id)`.
- **FR-LC-012** — **no sim/loop assembly may reference `TacticalDirector.Localization`** (F6 is a build
  error).
- **FR-LC-013/014** — a producer binds by adding a **sibling boundary adapter**, never by changing the core
  seam; a producer emits only its own native values, and *"#35/#46 carry disjoint slots"* (§2.2 verbatim).
- **FR-LC-015** — the producer's pre-draw gate is an **intent-VALUE roster check**.

`section-7.md` §7.3 goes further and names #35's adapter in advance: *"a **new boundary adapter**
(`MediaTextBoundary`, `InboxTextBoundary`) is added **when that producer is built** (FR-LC-013), each
referencing its producer and mapping its native slots into the generic `LocalizedTextRequest`."*

**Consequence:** #35's text path is **#49's**, not #22's. The plan's KD-1 ("build strictly as a consumer of
#22's `InteractionTextGenerator`") and KD-2 (the `world.text`-vs-selection cursor-separation invariant) are
both answering a question that no longer exists — see KD-1/KD-2. This is a *simplification*: #35 gains a
producer tag and an adapter, and loses a `living-world` dependency it would otherwise have carried. **One
clause of #49 does not survive contact with a second producer — see (h).**

**(b) #33 makes #35 a read-only morale consumer, and names #46 as the sole writer.**
`personalities-morale-dynamics/section-2.md`:

- **FR-HS-002** — #33 owns per-`PlayerId` `MoraleState` + `PersonalityProfile`. *"No other assembly writes
  them."*
- **FR-HS-024** — *"#33 exposes read-only morale accessors for #31/#35/#45. **#46 is the only consumer that
  writes #33 morale** (man-management). All are **deferred** (FR-LW-031)."*

**Consequence:** the plan's KD-1 obligation to *"define … the exact morale-write direction into #33"* is
**unsatisfiable as written** — there is no #35 write direction, by an approved MUST. The consequence must
travel as a value, not a write (KD-3). #33's own committed-input mechanism already exists for exactly this:

```csharp
public readonly struct HumanSystemsDayInput
{
    public readonly MatchDayResult Result;              // None | Win | Draw | Loss
    public readonly int MinutesPlayed;                  // [0,120]
    public readonly int BoardObjectiveDeltaPermille;    // [-1000,1000] committed board-state nudge
    public static HumanSystemsDayInput Neutral => new(MatchDayResult.None, 0, 0);
}
```

consumed by `ComputeMoraleTarget(equilibrium, committedInputs, personality)` inside
`AdvanceHumanSystemsDay` at #30 tick slot 3 (§3.1). It is a **transient input struct, not serialized
state**, which is what makes KD-3's back-prop cheap.

**(c) No reputation state exists anywhere in the approved set.** A tree-wide search for *reputation* across
`docs/specs/**/section-*.md` returns exactly three hits, none of them state: `living-world/section-3.md`
(prose describing a `WonderkidVsVeteran` arc trigger), `living-world/section-7.md` (a Stage-1+ deferral
note), and `youth-academy-intake/section-7.md` — which **explicitly disowns** one: *"A #42-owned
reputation, morale, or finance field. Those belong to their owners."*

**Consequence:** the plan's KD-3 ("reuse an existing #30/#40 reputation field, or introduce a minimal one
here?") has no third option hiding upstream — there is nothing to reuse. KD-4 answers it.

**(d) #30's pinned tick order is currently malformed — and #35 needs a slot in it.**
`season-competition-loop/section-3.md` §3.3 `RunWorldTickInFixedOrder` reads (verbatim, abridged):

```
    # 7. academy       (#42)  — NULL SEAM today (ERR-030-007 …)
    # 8. board         (#45)  — NULL SEAM today (ERR-030-008 …)
    # 9. world day:     WorldStore.AdvanceDay()   <-- the only LIVE tick
    # 7. scouting      (#32)  — NULL SEAM today (ERR-030-007 …)
    # 8. world day:     WorldStore.AdvanceDay()   <-- the only LIVE tick
    WorldStore.AdvanceDay()
```

Three distinct defects, all from same-day parallel approvals (#42 and #32 both landed July 24; #44 and #45
both touched §3 on July 24–25):

1. **Two seams claim step 7** (#42 academy, #32 scouting), and the scouting block sits **after** the
   live-tick line with its own duplicate `# 8. world day` — i.e. the pinned order has no unambiguous
   position for #32.
2. **`FR-SN-034` omits #32 entirely.** Its enumeration reads *"(#28/#29/#33/#41/#31/#34/#42/#45)"* — so the
   MUST that exists to keep the order pinned does not list one of the seams the order carries.
3. **Two ERR ids are each used twice for different changes:** `ERR-030-007` (#42 academy step **and** #32
   scouting step) and `ERR-030-009` (#45's `JobSecurity` band **and** #44's §3.4 availability filter).
   `section-2.md`/`section-3.md` version histories carry duplicate `0.7` and `0.8` rows to match.

**Consequence:** #35 must cite a step number, and there is no defensible number to cite until this is
repaired. §8.0 files the repair as a **prerequisite**, not a #35-internal decision — it is #30's text, it
predates #35, and it needs fixing whether or not #35 is ever authored.

**(e) `_RESERVED_0x27_` / `SubsystemOrdinals 89` already exist for #35.**
`deterministic-sim/section-3.md` §3.4 (v1.0.13, added at #33's approval by the A-04 gap sweep):
*"**Reserved — held for Media & Press Interactions #35 per roadmap §6 (`SubsystemOrdinals` 89); MUST NOT be
reused.** Placeholder pending #35's promotion. No code const."*

**Consequence:** #35 needs **no #16 back-prop at approval** if its minimal tier is draw-free (KD-2) — unlike
#45, which had to file the placeholder itself. The row is already correct.

**(f) The #30 hook #35 needs already exists and is already producer-only.** §3.4
`AdvanceAndPlayNextRound` ends each fixture with `Table.ApplyResult(result)` then
`EmitMatchOutcome(result)` — *"records the event in season state and is producer-only (KD-3), one per
fixture"* — where `result` is a `MatchResult { HomeClubId, AwayClubId, HomeScore, AwayScore, RoundIndex,
WorldTick }`. That is the committed value a post-match conference is queued from (KD-5), and the same path
already carries #44's availability null seam, so a second null seam there is an established shape.

**(g) #22's generator, for the record.** `src/living-world/InteractionTextGenerator.cs` `Generate(intent,
in slots)` performs *exactly one* `world.text` reserved draw and returns a **string** selected from the
in-code `InteractionTextCorpus`; `InteractionSlots` is `{SubjectName, OpponentName, HomeGoals, AwayGoals,
+ optional cited episode}`. #49's FR-LC-016 retrofit changes `Generate` to return native values and
migrates that corpus into the localization catalogue. #35 reusing this type would (i) bind #35's slot shape
to #22's four match-specific fields, which FR-LC-014 explicitly says the producers do **not** share, and
(ii) make #35 reference `living-world`. KD-1 does neither.

**(h) `FR-LC-020` is written for one producer and contradicts #49's own extension point.** Verbatim:

> **FR-LC-020** — `LocalizedTextRequest.SelectionDraw` MUST be the `ulong` value returned by
> `DeterministicRngService.DrawReserved` (the `world.text` reservation), carried verbatim.

That is a **MUST on the generic core seam** naming a **specific producer's stream**. It cannot be
satisfied by any producer that is not #22, and it contradicts three things in the same approved spec:

- §7.3 — producers *"emit their native template identity + slots + draw **(if they draw)**"*, which
  explicitly contemplates a producer that does not draw at all;
- FR-LC-013/014 + §2.2 — the core seam is **producer-agnostic** and *"references nothing sim-side"*, yet
  FR-LC-020 binds one of its fields to `world.text`;
- FR-LC-005 — the renderer *"MUST NOT … draw from any RNG stream"*, so the value is by construction
  supplied by the producer, and the seam's legitimate interest is that it be **deterministic and
  locale-independent** (FR-LC-006), not which stream produced it.

The clause was correct when #22 was the only producer and #49 was authored against it. #35 is the first
second producer, so #35 is where it surfaces. **This is a #49 defect, not a #35 constraint** — resolved by
ERR-049-001 (§8.1), which generalizes the requirement to *"the producer's own deterministic selection value,
carried verbatim"* while keeping #22's binding as the named example. KD-2 depends on it.

## 3. Staging (minimal-first → deep)

| Tier | Content |
|---|---|
| **Minimal (the identity)** | One question archetype per trigger event, a fixed bounded answer set, one **committed integer morale delta** for a single named subject. Variant selection is a **local keyed mix**, not a draw. **No stream, no tag promotion, no reputation, no board consequence, no #33 read.** The identity property is stated precisely in KD-9 — note it is *not* "no conference is answered", since expiry resolves an unanswered conference to its no-comment option (KD-5) and that is an answer. |
| **Deep** | Multi-archetype selection (context / rivalry / form) as a genuine keyed **draw** — the first and only stochastic surface, promoting `0x27`; mood-aware phrasing via the deferred #33 morale **read**; multi-target consequence spread (subject, squad, board) as *values* on the same code path; a board-facing signal routed into #45. |

The deep tier **extends** the minimal identity; it never rewrites it (the #21 / #40 / #45 KD-8 posture).

## 4. Key decisions

### KD-1 — #35 is a **#49 producer**, not a #22 consumer

#35 emits, per rendered item: a **`MediaIntent` enum value**, its own **disjoint** native slot values, and a
`ulong` selection value. A **sibling boundary adapter `MediaTextBoundary`** — named in advance by #49
§7.3 — maps the intent to a `TextTemplateId (ProducerTag = Media, LocalOrdinal = (int)intent)`, formats the
slots to strings, and calls `ILocalizer.Render`. The adapter lives in the composition/boundary layer with
#22's `LivingWorldTextBoundary`; it is the *only* thing that references both #35 and
`TacticalDirector.Localization`. **`MediaIntent` is the single identity type** — #35 defines no parallel
"template id" of its own; the `(tag, ordinal)` pair is the adapter's construction, exactly as
`LivingWorldTextBoundary.ForInteraction` builds it from `InteractionIntent`.

**"Per rendered item" is two rosters, not one — the question *and* every answer option.** A press
conference is a question plus a bounded set of phrased answers the manager chooses between, and the option
labels are user-facing text, so they are subject to FR-LC-002 exactly as the question is. #35 therefore
carries **one `MediaIntent` roster covering both** (question archetypes and answer phrasings are values in
the same enum, distinguished by an ordinal band, so a single coverage assertion covers both and the adapter
needs one mapping): a conference record names its question intent plus its ordered option intents, and
`optionIndex` keys both the rendered label and the consequence (KD-8). Modelling only the question — the
v0.1 gap — would have left the client with no compliant way to display the choices, and the pressure would
have been to bake them, which is the one thing #49's coverage-lock exists to prevent.

Three structural properties fall out, and all three are testable rather than aspirational:

- **#35 never references `TacticalDirector.Localization`** (FR-LC-012, asserted by assembly-reference
  absence — the #40 `T-FN-BOUND-002` posture).
- **#35 never emits a display string** (FR-LC-002; #35's own spec carries the coverage-lock, per #49 §7.3).
- **#35 never references `living-world`.** The plan's #22 dependency disappears entirely.

**#35's own obligations under the seam:** a `MediaIntent` enum whose **values** are the pre-render roster
gate (FR-LC-015 — refuse `None` / undefined *before* any selection work), and a catalogue-coverage
assertion extending FR-LC-008a to #35's roster (every defined `MediaIntent` has a base-locale template row).

**`MediaIntent` carries an ORDINAL STABILITY contract — APPEND-only, never reordered.** Its ordinal is
doubly load-bearing: it is serialized inside `PressConference` (§5) *and* it is the `LocalOrdinal` half of
the `TextTemplateId` the catalogue is keyed on (KD-1). Reordering or inserting a value therefore silently
re-points every saved conference at a different template *and* invalidates every catalogue row at once,
with no version gate to catch it — the save would load cleanly and render the wrong text. New values are
appended; a retired value keeps its ordinal and its base-locale row. This is the same contract
`CancelReason` / `PassType` carry for the same reason (ordinals embedded in persisted/hashed data), and it
is why the question/option split is a **documented ordinal band with a pinned boundary constant** rather
than an informal convention — the boundary is `[FIXED]` and asserted, so "add a question" cannot quietly
shift the options.

**Deferred, not designed:** the citation clause (`HasCitedEpisode` / `CitationKind`). A press question
referencing a remembered episode would need #22's `MemoryStore`, re-introducing exactly the dependency this
decision removes. `HasCitedEpisode = false` at every tier until a deep-tier decision says otherwise, and
that decision must re-argue the dependency.

### KD-2 — Draw-free minimal ⇒ `_RESERVED_0x27_` stays RESERVED

The minimal tier selects one archetype per trigger event — a pure function of the trigger — so there is
**no stochastic decision**, and #35 registers **no RNG stream** and promotes **no domain tag** at approval
(the #29 / #40 / #31 / #34 / #32 / #43 / #45 precedent; §2(e) shows the placeholder row is already correct,
so this is a decision with *zero* #16 paperwork).

**The `ulong` that FR-LC-004 requires is a local keyed mix, not a draw** — *conditional on ERR-049-001*
(§2(h)). #35 computes it with a **local SplitMix64** over `(intentOrdinal, worldDay, subjectId, purpose)`.
The precedent is explicit and in-tree: `FixtureScheduler` and `LeagueBootstrap` each carry a **local**
SplitMix64 rather than allocate a domain tag, and the root `CLAUDE.md` records that this is *why*
`DOMAIN_TAG_SEASON_LOOP` stayed pinned to #30 T2's first real draw site. The mix is position-independent, so
**nothing is serialized**, replay is cursor-free, and phrasing variety survives without a stream. (Note the
helper is genuinely local: `SplitMix64` exists in `src/deterministic-sim/` only *inside*
`DeterministicRngService.cs`, not as a shared public primitive — which is why both existing users copied it,
and why #35's minimal tier needs no assembly reference at all. §10.)

*Rejected alternative:* register a `media.selection` stream at the **minimal** tier and draw, satisfying
FR-LC-020 as literally written. Rejected — it manufactures a stochastic surface for a decision that has no
randomness in it (one archetype per trigger), purely to satisfy a clause written when #22 was the only
producer; it promotes `0x27` with no genuine draw site, which is the phantom-surface class FR-LW-031
forbids and the exact thing #29/#40/#31/#34/#32/#43/#45 all declined to do; and it re-opens the persisted-
cursor question KD-7 otherwise closes. Fixing the over-specified requirement is cheaper and correct.

**If ERR-049-001 is refused**, the fallback is not the rejected alternative above but a narrower one:
#35 supplies `SelectionDraw = 0` and pins variant `0` per intent (FR-LC-007 is total at `draw = 0`), losing
phrasing variety at the minimal tier and regaining it at the deep tier's real draw. Recorded so the
supplement does not depend on a back-prop being granted.

**When the deep tier does promote `0x27`, the model is pinned now:** **one** stream for the whole
subsystem — siteId `media.selection`, `entityId = MEDIA_STREAM_ENTITY_SENTINEL` — registered once
regardless of club or player count, with the entity folded into a fixed-radix **keyed action ordinal** on
`(subjectId, worldDay, purpose)`. `DeterministicRngService.RegisterStream` appends into a bounded,
never-shrinking table (`MaxRngStreams` = 64, no unregister), and #42 §7.4 R-1 records that a per-entity
registration model exhausts it in a full-world career. **#35 contributes exactly one registration at any
tier** — recorded here so the choice is not later "simplified" into a per-club model.

### KD-3 — The consequence is a **committed value routed through #30**, never a morale write

Per §2(b) this is forced, and the mechanism already exists. On answering, #35 records a bounded
`MediaDeltaPermille` against a `PlayerId` in its own state. #30's tick step 3 already builds a
`HumanSystemsDayInput` per player; it folds #35's pending delta into that struct as a **new committed
field**, and `ComputeMoraleTarget` consumes it as an additive term exactly as it consumes
`BoardObjectiveDeltaPermille` today. **#33 remains the sole writer of its own state** (FR-HS-002 intact),
and FR-HS-024 stays literally true — #35 is a read-only consumer that never touches morale; it supplies an
*input* to #33's own writer, which is the identical mechanism by which #33 receives #30's match results
without referencing #30.

`0` is the neutral value and `HumanSystemsDayInput.Neutral` keeps it, so the field is **behaviour-neutral
until a non-zero delta is delivered** (not merely "until a conference is answered" — expiry answers
conferences, and a `0`-consequence answer records nothing at all, §5) and the struct is transient (no `HUMAN_SYSTEMS_SAVE_FORMAT_VERSION`
implication). Filed as **ERR-033-001** (§8.1).

*Rejected alternative:* back-prop FR-HS-024 to add #35 as a second writer. Rejected — it widens a MUST
approved three days earlier, and two writers into morale is precisely the "media becomes a second morale
engine" risk the plan's §9 names.

*Rejected alternative:* route the delta through #46's man-management write path. Rejected — #46 is
unauthored (this wave, after #35), so #35's consequence would depend on a spec that does not exist, and
#46's own plan scopes its write path to *talk-to-player*, not to applying another producer's deltas.

**#30 touches #35 at *two* points in the day, and both must be specified.** The drain happens at **step 3**,
where #30 assembles each player's `HumanSystemsDayInput` — *not* at #35's own step-10 seam, which only
expires (KD-5). Both are filed in §8.0/§8.1; stating only the step-10 seam would produce an implementation
where deltas are recorded and never delivered, and every #35-local test would still pass.

**The delivery contract, pinned rather than implicit.** A delta is delivered at the **first step 3 that
follows the answer command**, and cleared there. Answers are commands issued **between** world ticks (the
day loop is synchronous — `RunWorldTickInFixedOrder` runs to completion per day), so in the normal flow a
conference answered after day *D*'s fixture is delivered on day *D+1*. The lag is a property of *when the
command lands relative to the tick*, not of slot ordering — and it is deterministic under replay because
the command sequence is part of the replayed input.

The alternative — deliver same-day by re-running step 3 after an answer — is rejected: it would make
morale's day step non-idempotent, which #33's own F6 guard (`worldDay == LastAdvancedWorldDay` ⇒ **no-op**)
forbids outright, so the delta would be silently dropped rather than applied. The one-day contract is not a
compromise; it is the only shape #33's guard permits. It matches #45's board→morale lag (KD-7) and #23's
one-stride-stale dismark carriers.

### KD-4 — #35 introduces **no** reputation scalar

§2(c) establishes there is nothing to reuse — so the plan's question is really *"may #35 create one?"*, and
the answer is no, at any tier #35 owns. A manager/club reputation is consumed by transfers (#31), youth
intake (#42), staff hiring (#34), board patience (#45) and national-team selection (#36); a scalar invented
inside a press-conference spec would become five specs' truth by accident, which is the duplicate the plan
warns about with the owner reversed.

**What #35 does instead:** the answer's consequence at minimal is the single morale delta of KD-3. The
board-facing half is a **deep-tier routed value** into #45's existing committed-input struct
(`BoardDayInput`, which already carries a deep-tier `MoraleSignalPermille` neutral at minimal — the same
shape), filed as a **deferred** back-prop (§8.2), never a #35 field.

If a genuine reputation system is later wanted, it is its own spec (or #45's, which already owns a
club-scoped persistent relationship scalar and the drift machinery for one). Recorded in §11 as a standing
option, not a debt.

### KD-5 — #30 queues; the manager's answer is a **command**

Two distinct paths, deliberately not merged:

- **Queueing is event-driven at #30's §3.4 post-round path** — after `EmitMatchOutcome(result)`, a null
  seam offers the committed `MatchResult` to #35, which may enqueue at most one conference for the managed
  club. Non-managed fixtures never queue (no manager to ask).
- **Answering is command-driven** — `TryAnswerQuestion(conferenceId, optionIndex) → bool`, invoked from the
  client, the #31 `SubmitBid` precedent. It is **not** a tick step: a conference the player never opens must
  not auto-answer itself.

  **It returns `false` rather than throwing on an already-resolved conference, and that distinction is
  load-bearing.** The client renders a conference list; the tick's expiry sweep can resolve a conference
  *between* that render and the player's click. Answering an already-expired conference is therefore a
  **legal race, not malformed input** — fail-loud there would crash a career on an ordinary click. Genuinely
  malformed input still fails loud: an unknown `conferenceId`, an `optionIndex` outside the conference's own
  option roster. This is #45's `TryProjectBoardModifier` distinction — "not applicable" is a named legal
  state, corrupt input still throws — applied to the one #35 surface a human drives.
- **The daily tick seam does one thing: expiry.** An unanswered conference past its deadline resolves to
  its designated *no-comment* option (a defined answer with its own delta, frequently `0`), so the queue is
  bounded and a save can never accumulate stale conferences. That is the only reason #35 takes a tick slot
  at all, and it is why the slot may sit last among the null seams.

*Rejected alternative:* expire **lazily** on read, taking no tick slot at all — which would also drop the
§8.0 prerequisite from #35's critical path. Rejected: expiry produces a *consequence* (the no-comment
delta), so a lazy scheme makes state depend on whether and when the client happened to read, which is not
replayable — a career where the player never opens the inbox would diverge from one where they did. The
eager sweep costs one `uint` comparison per pending conference per day against a bounded queue.

### KD-6 — #46 discovers items by **reading #35**; #35 never references an inbox

Strictly one-directional: **`#46 → #35`**. #35 exposes a read-only query over its own conference records
(pending / answered, with the answered option and its committed delta); #46's aggregator reads it, exactly
as #37 and #44 read the engine's ledger without the engine knowing they exist. #35 fires no inbox event,
holds no unread flag, and **never references #46** — asserted by assembly-reference absence.

This also answers the plan's KD-5 second half without inventing a message bus: the "how does #46 discover
it" question only looked hard while media was assumed to *push*.

### KD-7 — Persistence: an opaque, independently version-gated sub-blob

`MEDIA_SAVE_FORMAT_VERSION` [FIXED] = 1. #35's state (the pending queue + the answered-conference records
carrying undelivered deltas) lands as its own sub-blob composed into #30's `SeasonSaveCodec` — **not** a
`WORLD_STORE_FORMAT_VERSION` bump (the #40 / #42 / #44 / #45 pattern; the composite is #22-owned). The outer
codec never parses it. Version gate read **first**; overflow-safe `Require(offset, need, total)` length
prefixes compared against `total − offset`; trailing-byte guard. Fail loud on all three. **APPEND-only**
layout — deep-tier fields go at the end behind a version bump, never inserted mid-block (the #42 Appendix B
rule).

**Deliberately absent:** any RNG cursor (KD-2 leaves none to persist), and any copy of morale, board
confidence, the table, or a rendered string — mirroring any of them would re-introduce a double truth, and a
stored *string* would additionally break FR-LC-006's locale-independent-state rule (a save must not depend
on the locale it was written in).

**The undelivered-delta invariant** is the one thing this blob exists for: a delta recorded after a tick is
consumed at the next step 3 (KD-3), so a save taken between them **must** carry it. It is stored with the
`worldDay` it was recorded on, and delivery clears it — which makes "was this delta applied?" a serialized
fact rather than an inferred one, and makes double-delivery across a restore impossible.

**A pending delta names a `PlayerId`, so it needs a roster-lifecycle rule** — the same one #33 needed and
wrote down as FR-HS-027 (regen inserts / retirement removes, in lockstep with #28's season-boundary churn),
and which #31 extends by **re-keying** a club-scoped `PlayerId` on transfer. Without a rule, an undelivered
delta targeting a player who retires is never drained (nothing iterates him at step 3) and lives in an
APPEND-only blob forever, and one targeting a transferred player could be delivered to **whoever now holds
that id**. The rule: an undelivered delta whose target leaves the managed roster is **dropped** at the same
boundary #33 drops that player's entries — never migrated. A press reaction to a departed player has no
subject left, so dropping is semantically right as well as safe, and it keeps #35 out of #31's re-key
protocol entirely.

### KD-8 — Consequence scope: one code path at both tiers

An answer's consequence is a **list of (targetKind, targetId, deltaPermille)** at every tier. Minimal ships
**zero or one** entry: one with `targetKind = Player` when the conference has a subject, and **zero** when
it does not (`SubjectPlayerId == MEDIA_NO_SUBJECT` — a result or board question, whose answers are text-only
at minimal because the only minimal target kind is `Player` and there is no player to name). The deep
tier's squad/board spread adds *entries*, not code paths or branches, and is where a subject-less
conference acquires a consequence. The bound (`MEDIA_MAX_CONSEQUENCE_TARGETS`) is `[GT]` and enforced at the
authoring boundary, so a deep-tier catalogue row cannot silently make one answer touch the whole roster.

Stating the zero case matters: without it, an implementer meets the first subject-less conference and picks
a fallback — a `0`-delta entry against `PlayerId 0` (a real player) being the obvious wrong one.

### KD-9 — Behaviour-neutral identity

With no conference queued or every conference unanswered-and-expired to a `0` no-comment option: no stream
is registered ⇒ every existing stream's cursor is **byte-identical** (the #22/#26/#28/#29/#40/#41/#42/#45
stream-independence precedent); `HumanSystemsDayInput` carries `MediaDeltaPermille = 0` ⇒ `ComputeMoraleTarget`
is unchanged; and a season advanced with the #30 media seams null is byte-identical to the same season
pre-#35 (the FR-SN-026 world-floor property).

## 5. Persistent state (shape)

```
PressConference   : { ConferenceId (int, monotonic), QuestionIntent (MediaIntent),
                      OptionIntents (MediaIntent[], ordered; index = optionIndex),   # KD-1 second roster
                      SubjectPlayerId (int; MEDIA_NO_SUBJECT = -1 for a board/result question),
                      TriggerRoundIndex (int), QueuedWorldDay (uint), DeadlineWorldDay (uint),
                      AnsweredOptionIndex (int, -1 = unanswered) }
PendingDelta      : { TargetKind (byte), TargetId (int), DeltaPermille (int [-1000,1000] \ {0}),
                      RecordedWorldDay (uint) }          # cleared on delivery (KD-7)
MediaCursors      : { NextConferenceId (int),
                      LastAdvancedWorldDay (uint, sentinel uint.MaxValue) }   # the F6 guard's state
```

`LastAdvancedWorldDay` is **not optional bookkeeping** — it is the state §6's same-day-no-op / day-gap
fail-loud guard is implemented *from*, and its unadvanced sentinel is `uint.MaxValue`, **not** `0`
(#33's FR-HS-008 records why: a `0` sentinel makes the guard silently no-op a day-0 advance instead of
failing). It is subsystem-scoped rather than per-conference, because expiry is one sweep per day.

`OptionIntents` is bounded by `MEDIA_MAX_OPTIONS` `[GT]` (a fixed small array, not an open list — a
conference the client cannot fit on a screen is an authoring error, caught at the catalogue boundary).
`MEDIA_NO_SUBJECT = -1` is an explicit sentinel, not `0`: `0` is a valid `PlayerId`, and the
`default(struct)`-is-a-valid-looking-value trap is the one #40's `BoardModifier` F4 and #33's
`PersonalityProfile` F4 both exist to catch.

**A zero delta is never recorded.** An answer (including the no-comment option most expiries resolve to)
whose consequence is `0` writes no `PendingDelta` at all — the canonical-representation rule #44 adopted as
its immediate `(0,0)`-entry drop. Without it, every expired conference in a 38-round season would leave an
inert row in an APPEND-only blob, and "is there a delta pending for this player?" would stop being
answerable by presence.

All integer; no string, no float, no locale-dependent value (FR-LC-006). The queue is bounded by
`MEDIA_MAX_PENDING_CONFERENCES` `[GT]`, enforced at enqueue — a full queue **drops the new conference**
rather than throwing (a press conference is not a correctness-critical event, and fail-loud here would let a
UI that never opens the inbox crash a career). The drop is a recorded, testable branch, not a silent one.

**What #35 does *not* define:** the *semantics* of the match result it is handed (#30's `MatchResult`), the
morale scale's meaning (#33's `MORALE_NEUTRAL_PERMILLE`), or what a locale does with a template. Recording
the boundary here prevents a later "which spec is wrong?" argument over a case neither had claimed.

## 6. Determinism posture

- World tick + the #30 post-round path only; never the 10 Hz / 60 Hz match loops.
- Minimal tier: **draw-free**; the FR-LC-004 `ulong` is a local keyed SplitMix64 mix (KD-2).
- Deep tier: one stream, keyed position-independent draws, no persisted cursor (KD-2).
- All-integer arithmetic; no float enters #35 at any tier.
- Expiry is a `worldDay` comparison — same-day re-run is a **no-op**, a day gap is **fail loud** (the #33 F6
  guard, adopted verbatim).
- Rendering is display-side and runs strictly **after** the deterministic decision (FR-LC-005), so locale
  cannot perturb #35's serialized bytes (#35 feeds no digest at all — it never touches the match engine).

## 7. Primary surfaces (proposed)

| Surface | Direction | Notes |
|---|---|---|
| `TryQueueConference(in MediaTriggerInput, worldDay) → bool` | #30 → #35 | post-round seam; `MediaTriggerInput` = committed integers only (`{ HomeClubId, AwayClubId, HomeScore, AwayScore, RoundIndex, ManagedClubId }`, derived by #30 from its own `MatchResult` — #35 references no #30 type, the `BoardDayInput`/`HumanSystemsDayInput` posture); `false` = nothing queued (queue full / no archetype) |
| `TryAnswerQuestion(conferenceId, optionIndex) → bool` | client → #35 | command-driven (KD-5); `false` = already resolved (a legal render/tick race); fail loud on unknown id / out-of-range option |
| `AdvanceMediaDay(worldDay)` | #30 → #35 | expiry sweep only (KD-5); same-day no-op / day-gap fail loud off `LastAdvancedWorldDay` (§5) |
| `TryTakePendingDelta(playerId, out int) → bool` | #30 → #35 | drains into `HumanSystemsDayInput` (KD-3); `false` = no delta, caller supplies `0` — the #45 `TryProjectBoardModifier` "not modelled is a named legal state" posture. Takes **no** `worldDay`: it drains whatever is pending regardless of the day it was recorded, which is what makes delivery robust across a save/restore or a multi-day jump |
| `Conferences(...)` read-only query | #35 → #46 / #38 | value copies (the `FinancesViewModel` posture); the only #46 discovery path (KD-6) |
| `MediaTextBoundary.BuildRequest(intent, draw, in MediaSlots)` | boundary layer | the #49 sibling adapter (KD-1); **not** a #35 surface |

## 8. Cross-spec back-props

### 8.0 Prerequisite (must land **before or with** #35's promotion — not a #35-internal change)

| ID | Target | Change |
|---|---|---|
| **ERR-030-012** | #30 §3.3 + FR-SN-034 (+ `spec-error-log.md` errata) | **Tick-order reconciliation** (§2(d)): give #32 scouting an unambiguous step (**9**, satisfying its own "after staff" rationale without renumbering the six slots approved specs cite by number); delete the orphaned duplicate `AdvanceDay` line; append the **#35 media expiry seam as step 10**; `AdvanceDay` → **step 11**; extend FR-SN-034's enumeration to include **#32 and #35**. Record the two duplicate-id collisions (`ERR-030-007` ×2, `ERR-030-009` ×2) and the duplicate `0.7`/`0.8` version-history rows as errata. |

This is filed as a prerequisite rather than a #35 back-prop deliberately: it is a defect in #30's text that
exists today, independent of #35, and #35 merely cannot cite a step number until it is fixed.

### 8.1 At approval (must land atomically with the status flip)

| ID | Target | Change |
|---|---|---|
| **ERR-049-001** | #49 §2.1 FR-LC-020 | Generalize the `SelectionDraw` provenance from *"the `ulong` returned by `DeterministicRngService.DrawReserved` (the `world.text` reservation)"* to **"the producer's own deterministic, locale-independent selection value, carried verbatim"**, keeping #22's `world.text` draw as the named example. Resolves the internal contradiction with §7.3 (*"if they draw"*) and FR-LC-013/014's producer-agnostic core (§2(h)). Contract-widening only — #22's existing binding still satisfies it verbatim, and #49 needs no code or catalogue change. |
| **ERR-033-001** | #33 §2.2 `HumanSystemsDayInput` + §3.1 `ComputeMoraleTarget` | Add `MediaDeltaPermille (int [-1000,1000])`, `Neutral` = `0`, consumed as an additive term alongside `BoardObjectiveDeltaPermille`. Transient input struct — **no** `HUMAN_SYSTEMS_SAVE_FORMAT_VERSION` bump; `0` ⇒ target unchanged, so it is behaviour-neutral until a non-zero delta is delivered (KD-3). |
| **ERR-033-002** | #33 §2.1 FR-HS-027 | Extend the roster-lifecycle lockstep to state that a **routed input's** pending source-side value is dropped with the player's entries (the rule #35's undelivered deltas bind to, §5). Alternatively filed #35-side if #33's owner prefers the obligation on the producer — the *rule* is what must exist, not its file. |
| **ERR-030-013** | #30 §3.4 `AdvanceAndPlayNextRound` + §3.3 step 3 | **Two** media seams: the **queue** null seam after `EmitMatchOutcome(result)` (the #44 availability-seam shape), and the **drain** at step 3 where the per-player `HumanSystemsDayInput` is assembled (`TryTakePendingDelta` → `MediaDeltaPermille`, KD-3). Both empty/`0` until #35 T2. Filing only the first would produce recorded-but-never-delivered deltas. |

*(The last id assumes ERR-030-012 is taken by §8.0; both are #30-side and land together.)*

### 8.2 Deferred (land at the named tier, not at approval)

- Promotion of `_RESERVED_0x27_` → `DOMAIN_TAG_MEDIA = 0x27` at the deep tier's first selection draw (KD-2).
- The outer `SEASON_SAVE_FORMAT_VERSION` bump, at T2 when the sub-blob is first composed in.
- `BoardDayInput.MediaSignalPermille` on #45 — the deep-tier board-facing consequence (KD-4), arriving as a
  routed committed value exactly like its existing `MoraleSignalPermille`.
- A #33 morale **read** for mood-aware phrasing (FR-HS-024 anticipates it; deferred per FR-LW-031). **When
  it lands it arrives as routed committed values, not an assembly reference** — preserving §10's DAG.

### 8.3 Explicitly **not** back-props

- **#16** — nothing to change. `_RESERVED_0x27_` / `SubsystemOrdinals 89` already exist and are already
  correct for a draw-free minimal tier (§2(e)).
- **#49's core seam** — nothing to change *structurally*. FR-LC-013/014 and §7.3 already specify the
  sibling-adapter extension point **by name** (`MediaTextBoundary`); #35 fits the existing contract rather
  than extending it, and the core `ILocalizer` / `TextTemplateId` / `LocalizedTextRequest` are untouched —
  the extensibility guarantee #49 was approved on, now exercised for the first time. The **one** #49 change
  is ERR-049-001 (§8.1), which corrects an internal contradiction in a single requirement's wording (§2(h))
  and touches no type, catalogue row, or code.
- **#46** — unauthored; KD-6 makes it the reader, so #35's landing imposes nothing on it.
- **#22** — untouched. #35 consumes neither `InteractionTextGenerator` nor `world.text` (KD-1/KD-2), so no
  cursor, corpus, or `WorldStore` surface changes.

## 9. Test focus

Identity **stated as KD-9 states it** — a season in which no conference is queued, *or* in which every
answer's consequence is `0`, is byte-identical to pre-#35 at the #33 and #30 seams, and every RNG cursor is
unchanged. (Not "no conference answered": expiry answers them, so a test written to that weaker precondition
would either fail or silently prove less than it claims.) Round-trip determinism over the sub-blob **including an undelivered delta
across the save boundary** (the KD-7 invariant — a save between record and delivery must apply it exactly
once); expiry no-op / day-gap fail-loud pair; **delivery-exactly-once** across the drain (a delta drained at step 3
is cleared, so a second step 3 the same day — #33's F6 no-op case — cannot re-apply it); **drop-on-departure**
(a pending delta whose target leaves the roster is gone, and the blob does not grow across a season of
churn); the FR-LC-015 intent-value gate refusing `None` before any selection work; FR-LC-008a catalogue
coverage over #35's full `MediaIntent` roster — **questions and answer options both** (KD-1); **locale-independence**
(the same career advanced under two display locales produces **byte-identical** serialized state — the
FR-LC-006 lock; #35 feeds no match digest, so bytes are the claim); delta bounds `[-1000,1000]` and the
`MEDIA_MAX_CONSEQUENCE_TARGETS` / `MEDIA_MAX_OPTIONS` caps; queue-full
drop (a recorded branch, not a throw); and **structural** assertions that #35's assembly references neither
`TacticalDirector.Localization` (FR-LC-012), `living-world`, `#33`, nor `#30` — the properties KD-1/KD-3/KD-6
rest on.

## 10. Reference DAG

```
root → {#30, #35, boundary}     #30 → {#33, #35, …}     #35 → { }  (minimal)  →  {#16} (deep)
boundary(MediaTextBoundary) → {#35, #49}
```

**Acyclic.** At the minimal tier #35 references **nothing** — the keyed mix is a local SplitMix64 (KD-2),
and `SplitMix64` is not a shared public primitive in `deterministic-sim` anyway (it lives inside
`DeterministicRngService.cs`; `FixtureScheduler` and `LeagueBootstrap` each carry their own copy, which is
the precedent #35 follows). The deep tier adds `TacticalDirector.DeterministicSim` for the RNG service at
its first real draw. At neither tier does #35 reference #30, #33, #46, #49, `living-world`, `SeasonSave`, or
`MatchEngine`.

**This holds at every tier.** The deferred #33 morale read (§8.2) does not weaken it: morale reaches #35 as
**routed integer values** supplied by the caller, never by #35 referencing #33 — the identical mechanism by
which #33 receives #30's match results. So the structural assertions in §9 are unconditional, not "true
until the deep tier", which is what makes them testable by assembly-reference absence rather than by review
vigilance.

## 11. Risks and standing options

- **R-1 — the §8.0 prerequisite is not #35's to decide.** If the #30 tick-order repair lands with different
  numbers than proposed, #35's cited step must follow it. Re-verify at promotion.
- **R-2 — the reputation vacuum will be filled by someone.** #36 (national-team selection) and #42 (intake
  quality) both want one. KD-4 keeps #35 out of that decision; whoever needs it first should own it, and
  #45 is the natural home (it already owns a club-scoped persistent scalar and the drift machinery).
  Standing option, not a debt.
- **R-3 — "media should just write morale" is the recurring temptation.** It is barred by FR-HS-002/024, and
  KD-3's routed-value path is the only compliant shape. A future maintainer adding a direct write would pass
  every #35 test while breaking #33's single-writer contract; §9's structural reference assertion is what
  actually catches it.
- **R-4 — deep-tier consequence-spread creep** (the plan's §9 third risk). Bounded by KD-8's
  `MEDIA_MAX_CONSEQUENCE_TARGETS` cap enforced at the authoring boundary rather than by review.
- **R-5 — ERR-049-001 is a change to a spec approved three days ago.** #49's owner may reasonably prefer to
  keep FR-LC-020 as written; KD-2 records the `SelectionDraw = 0` fallback so #35 does not stall on the
  answer. Either way the *contradiction* in #49 §2(h) is real and outlives #35 — the next producer (#46,
  this same wave) hits it identically.

## 12. Promotion pipeline

The steps from here to `APPROVED`, per `spec-plans/README.md`:

1. **This supplement, AR-converged** — **DONE at v0.6.** AR-1 (2H+4M+5L) → v0.2, AR-2 (0H+3M+1L) → v0.3,
   AR-3 (0H+2M+3L) → v0.4, AR-4 (0H+2M+0L) → v0.5, AR-5 (0H+0M+3L) → v0.6 = **CONVERGENCE** (an L-only
   round closes the cycle, per the project convention).
2. **Land the §8.0 prerequisite** (ERR-030-012) — or confirm its numbering — so §4/§8 can cite a real step.
3. **Author 11 section files** at `Status: IN REVIEW` under `docs/specs/media-press-interactions/`
   (`outline`, `section-1`..`section-8`, `section-9-approval-checklist`, `appendices`), FR prefix `FR-ME`.
4. **Section-file PASS-1 adversarial review** + a v0.2 fix pass, recorded in §9.4.1 of the checklist.
5. **`SPEC_INDEX.md` registry row** added at promotion (never at supplement stage).
6. **Lead-developer R-01..R-05 sign-off** — a human authority, not self-grantable.
7. **Flip to `APPROVED`**, landing the §8.1 back-props **atomically** with the flip.

## Version History

| Version | Date | Change |
|---|---|---|
| v0.1 | July 26, 2026 | Initial supplement promoted from the one-page plan. Resolves KD-1..KD-9 against verified upstream source. Two of the plan's five key decisions are **superseded by specs approved after it was written**: KD-1 re-bases the text path from #22's `InteractionTextGenerator` onto #49's localization seam + a `MediaTextBoundary` sibling adapter (which #49 §7.3 names in advance), dissolving the plan's KD-2 `world.text` cursor-separation question and removing a `living-world` dependency; KD-3 replaces the plan's "morale-write direction into #33" — unsatisfiable under FR-HS-002/024, which make #46 the sole morale writer — with a committed delta routed through #30 into #33's own day step (ERR-033-001). KD-4 records that **no reputation state exists anywhere** in the approved set and declines to create one. §2(d) records a **defect in #30's already-approved tick order** (two seams claiming step 7, #32 omitted from FR-SN-034, two ERR ids used twice), filed as the §8.0 prerequisite ERR-030-012 because #35 cannot cite a step number until it is repaired. #16 needs no back-prop — `_RESERVED_0x27_` already exists and is already correct for a draw-free minimal tier. |
| v0.2 | July 26, 2026 | **AR-1 fix pass: 2H + 4M + 5L, all resolved.** **H-1** — KD-2's local keyed mix **contradicted `FR-LC-020`**, an approved MUST binding `LocalizedTextRequest.SelectionDraw` to *"the `ulong` returned by `DeterministicRngService.DrawReserved` (the `world.text` reservation)"* — unsatisfiable by any producer that is not #22. Verified against source and traced to a contradiction **internal to #49**: §7.3 says producers emit a draw *"if they draw"*, and FR-LC-013/014 make the core seam producer-agnostic, yet FR-LC-020 names one producer's stream. Recorded as §2(h), resolved by the new **ERR-049-001** (generalize the provenance, keep #22's binding as the example), with a `SelectionDraw = 0` fallback pinned in KD-2 so #35 does not depend on the back-prop being granted, and the "register a stream at minimal" alternative rejected on the phantom-surface rule. **H-2** — the design modelled only the **question's** text identity; a conference's **answer options** are user-facing text too, so #35 had no compliant way to render the choices and the pressure would have been to bake them (the exact FR-LC-002 violation #49's coverage-lock exists to prevent). `MediaIntent` now covers both rosters; `optionIndex` keys the label and the consequence. **M-1** — #30 touches #35 at **two** points (queue at §3.4, **drain at step 3** where `HumanSystemsDayInput` is assembled), but only the step-10 expiry seam was filed; an implementation built to v0.1 would record deltas and never deliver them, with every #35-local test still green (ERR-030-013 now covers both). **M-2** — the one-day-stale contract was justified by slot ordering (#33 at 3, #35 later), which is the wrong mechanism since the drain *is* at step 3; restated as "the first step 3 following the answer command", with the same-day alternative shown to be barred by #33's own F6 no-op guard rather than merely undesirable. **M-3** — a pending delta names a `PlayerId` with no roster-lifecycle rule, so an undelivered delta for a retired player was immortal in an APPEND-only blob and one for a transferred player could be delivered to whoever now holds the re-keyed id (#31 KD-7); now dropped in lockstep with #33's FR-HS-027 (ERR-033-002). **M-4** — §10 claimed #35 references `DeterministicSim` for SplitMix64, but there is no shared public SplitMix64 there (it lives inside `DeterministicRngService.cs`; both existing users carry local copies), so the minimal tier references **nothing**. **L:** the spurious second identity type (`MediaTemplateId` vs `MediaIntent`) collapsed to one; "identical digest" → "byte-identical serialized state" (#35 feeds no digest); an explicit `MEDIA_NO_SUBJECT = -1` sentinel (`0` is a valid `PlayerId`); the lazy-expiry alternative recorded and rejected on replayability; `MediaTriggerInput`'s committed-integer shape specified. |
| v0.3 | July 26, 2026 | **AR-2 fix pass: 0H + 3M + 1L, all resolved.** **M-1** — `MediaIntent` had no **ordinal-stability** contract while its ordinal is doubly load-bearing: serialized inside `PressConference` **and** the `LocalOrdinal` half of the `TextTemplateId` the catalogue is keyed on. Reordering or inserting a value would silently re-point every saved conference at a different template and invalidate every catalogue row, with no version gate to catch it — the save loads cleanly and renders the wrong text. Pinned APPEND-only (the `CancelReason`/`PassType` precedent), with the AR-1 question/option split promoted from an informal ordinal band to a `[FIXED]` asserted boundary constant. **M-2** — §3's identity claim (*"with no conference ever answered"*) contradicted KD-9: expiry resolves an unanswered conference to its no-comment option, which **is** an answer, so §3 stated a weaker precondition than the one that actually holds; §3 now defers to KD-9. **M-3** — the AR-1 `MEDIA_NO_SUBJECT` sentinel collided with KD-8's *"minimal ships exactly one entry, `targetKind = Player`"*: a board/result question has no player to name, leaving the consequence arity undefined at exactly the point an implementer would invent a fallback (a `0`-delta against `PlayerId 0` — a real player — being the obvious wrong one). Minimal arity is now **zero or one**, explicitly. **L-1** — §2's items ran (f), (h), (g) after the AR-1 insertion; re-ordered. |
| v0.4 | July 26, 2026 | **AR-3 fix pass: 0H + 2M + 3L, all resolved.** **M-1** — §6 claimed the #33 F6 same-day-no-op / day-gap-fail-loud guard *“verbatim”*, but §5’s persisted shape had **no cursor to implement it from** (`MediaCursors` held only `NextConferenceId`) — a determinism claim the state could not support. Added `LastAdvancedWorldDay (uint, sentinel uint.MaxValue)`, with #33’s FR-HS-008 rationale for why the sentinel is not `0`. **M-2** — `AnswerQuestion` was specified to **fail loud on an already-answered conference**, but the daily expiry sweep can resolve a conference between the client’s render and the player’s click, making that an ordinary race rather than malformed input — a career-crashing throw on a legal click. Now `TryAnswerQuestion → bool` (`false` = already resolved; unknown id / out-of-range option still throw), the #45 `TryProjectBoardModifier` legal-state-vs-corrupt-input split applied to the one human-driven surface. **L:** §6’s residual “or the digest” corrected (#35 feeds none); zero-value `PendingDelta` rows are never recorded (the #44 canonical `(0,0)`-drop rule — otherwise every expired conference leaves an inert row in an APPEND-only blob); `TryTakePendingDelta`’s unused `worldDay` parameter dropped, with the reason delivery must be day-agnostic (save/restore, multi-day jumps) recorded. |
| v0.5 | July 26, 2026 | **AR-4 fix pass: 0H + 2M, both regressions introduced by earlier AR rounds — the case for re-reading the whole document each pass rather than the diff.** **M-1** — §8.3 still asserted “**#49** — nothing to change” while AR-1 had added **ERR-049-001** against #49 to §8.1: the two back-prop tables directly contradicted each other, and a reader working from §8.3 would have skipped the one change KD-2 depends on. §8.3 now scopes its claim to #49’s *core seam* and points at the single wording fix. **M-2** — §9’s identity test was still specified against *“no conference answered”*, the precondition AR-2 corrected in §3 and KD-9 but not here; since expiry answers conferences, a test built to it would either fail or prove less than it claims. Restated as KD-9’s condition (nothing queued, or every consequence `0`), with the trap named so it is not re-introduced. |
| v0.6 | July 26, 2026 | **AR-5 sweep: 0H + 0M + 3L, all resolved — CONVERGENCE** (an L-only round closes the cycle). **L-1** — KD-3 and the ERR-033-001 row still said the routed field is neutral *“until a conference is answered”*, the third and last instance of the precondition AR-2/AR-4 corrected elsewhere; the true condition is “until a **non-zero** delta is delivered”, since expiry answers conferences and a `0`-consequence answer records no row at all (§5). **L-2** — a dropped “that” in KD-2’s opening sentence. **L-3** — the ordinal-band boundary constant was tagged “`[GT]`-free `[FIXED]`”, a garbled construction from the AR-2 edit; it is simply `[FIXED]`. |
