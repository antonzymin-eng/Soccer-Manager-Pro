# Media & Press Interactions #35 — Section 4: Architecture

**Created:** July 27, 2026
**Last Updated:** July 27, 2026 (v0.2 — PASS-1 fix pass)
**Version:** 0.2
**Status:** IN REVIEW

---

## 4.1 Assembly and reference direction

New assembly **`TacticalDirector.Media`** at `src/media/`, referencing **nothing at the minimal tier**
and **only** `TacticalDirector.DeterministicSim` at the deep tier (for `DeterministicRngService` at its
first real draw).

```
root ──▶ #30 Season Loop ──▶ #35 Media ──▶ { }        (minimal — a true leaf)
  │            │                        ──▶ {#16}     (deep)
  │            └──▶ #33 Morale   (receives #35's delta as a committed int, assembled by #30)
  ├──▶ #46 News/Inbox ──▶ #35              (one-directional: #46 reads, #35 never knows)
  └──▶ boundary(MediaTextBoundary) ──▶ {#35, #49}
```

**Acyclic at every tier.** #35 references #30, #33, #45, #46, `TacticalDirector.Localization`,
`living-world`, `SeasonSave` and `MatchEngine` **at no tier** (FR-ME-006), asserted by reference-absence
(§5.8) rather than by review vigilance.

**The minimal tier is a true leaf — it references nothing at all**, which is unusual enough to state.
It is possible because the keyed selection mix is a **local** SplitMix64 (§3.5): `SplitMix64` is not a
shared public primitive in `deterministic-sim` — it lives *inside* `DeterministicRngService.cs`, which is
why `FixtureScheduler` and `LeagueBootstrap` each carry their own copy. #35 follows that precedent, and
the consequence is that the whole minimal tier has no upstream to break it.

**The deferred #33 morale read does not weaken this.** When it lands, morale arrives as **routed integer
values supplied by the caller**, never by #35 referencing #33 — the identical mechanism by which #33
receives #30's match results. So §5.8's structural assertions are **unconditional**, not "true until the
deep tier".

**CS0104 pre-check.** #35 introduces `MediaIntent`, `PressConference`, `PendingDelta`, `MediaCursors`,
`MediaTriggerInput`, `MediaSlots`, `MediaStore`, `MediaSaveCodec`, `ConferenceView`. Each was checked
against every type name that could be in scope with it before authoring, because this project has hit
CS0104 twice (`TacticTranslation`, `PlayerAttributes`). None collides. Note in particular that
`MediaSlots` is deliberately **not** named `InteractionSlots` — #22 owns that name, and FR-LC-014 says the
producers' slots are disjoint, so sharing the name would suggest a compatibility that must not exist.

## 4.2 File layout

```
src/media/
├── MediaConstants.cs             # the Appendix A catalogue — no magic numbers in formula code
├── MediaIntent.cs                # the APPEND-only roster + the [FIXED] band boundary (KD-10)
├── PressConference.cs            # the conference record
├── PendingDelta.cs               # the undelivered consequence
├── MediaTriggerInput.cs          # committed-integer input from #30
├── MediaSlots.cs                 # #35's disjoint native slots for the adapter
├── MediaCatalogue.cs             # question -> option roster, (question, option) -> consequence entries
├── MediaStore.cs                 # the queue + pending deltas + cursors; the SINGLE writer
├── MediaLifecycle.cs             # FM-ME-01/02/03 — queue, answer, expiry
├── MediaSelection.cs             # FM-ME-05 — the local keyed mix (and FM-ME-06 at the deep tier)
├── MediaSaveCodec.cs             # KD-7 sub-blob, version gate first
├── ConferenceView.cs             # read-only value copies for #46 / #38
└── tests/
```

**`MediaTextBoundary.cs` is deliberately absent from this tree.** It lives in the boundary layer beside
#22's `LivingWorldTextBoundary`, because it is the one thing that references both #35 and
`TacticalDirector.Localization` — and FR-LC-012 makes a sim assembly referencing the latter a **build
error**. Placing it here would not merely be untidy; it would not compile.

**`MediaCatalogue.cs` is authoring data, not logic.** It holds the question→option roster and the
(question, option)→consequence entries, and it is where F4/F5's authoring-boundary checks run. Keeping it
separate from `MediaLifecycle.cs` is what lets the deep tier add catalogue rows without touching the
lifecycle at all (KD-8: entries, not branches).

## 4.3 The `MediaTextBoundary` sibling adapter (KD-1)

```
# in the boundary layer — NOT in src/media/ and NOT in #49
class MediaTextBoundary
{
    LocalizedTextRequest BuildRequest(MediaIntent intent, ulong selection, in MediaSlots slots)
    {
        RequireRenderableIntent(intent);                  # F1 mirror — defence in depth
        var id = new TextTemplateId(ProducerTag.Media, (int)intent);        # KD-1
        return new LocalizedTextRequest(id, selection, FormatSlots(slots), citation: none);
    }
}
```

Three properties, each testable:

- **#35 never references `TacticalDirector.Localization`** (FR-LC-012) — the adapter does.
- **#35 never emits a display string** (FR-LC-002) — `FormatSlots` runs here, after the deterministic
  decision, so locale cannot perturb #35's serialized bytes (FR-LC-006).
- **`MediaIntent` is the single identity** — the `(tag, ordinal)` pair is built *here*, exactly as
  `LivingWorldTextBoundary.ForInteraction` builds it from `InteractionIntent`. #35 declares no template-id
  type of its own (FR-ME-008).

**The adapter renders both rosters through one mapping** (KD-1): a question intent and an option intent
are both `MediaIntent` values, so the client asks for the question's text and each option's label through
the same call. That is the whole reason the two rosters share an enum rather than being two types.

**#35's obligations under the seam stay on #35's side:** the FR-LC-015 value gate (FR-ME-011, run
**before** any selection work, so a refused item consumes nothing) and the FR-LC-008a coverage assertion
over the **full** roster (FR-ME-012). The adapter's `RequireRenderableIntent` is defence in depth, not the
gate — a gate that lived only in the boundary layer would be bypassed by any other consumer of `MediaIntent`.

## 4.4 The two #30 seams (KD-3 / KD-5)

#30 touches #35 at **two** points in the day, and both must exist or the feature is silently broken.

```
# (1) QUEUE — inside #30's AdvanceAndPlayNextRound(), after EmitMatchOutcome(result)
media.TryQueueConference(MediaTriggerInput.From(result, managedClubId), worldDay);

# (2) DRAIN — inside #30's RunWorldTickInFixedOrder(), at step 3, per player
var ext = media.TryTakePendingDelta(playerId, out var d) ? d : 0;
var input = new HumanSystemsDayInput(result, minutes, boardDelta, externalDelta: ext);
humanSystems.AdvanceHumanSystemsDay(state, input, worldDay);

# (3) EXPIRY — inside RunWorldTickInFixedOrder(), at #35's own slot
media.AdvanceMediaDay(worldDay);
```

**Filing only (1) and (3) would produce an implementation where deltas are recorded and never delivered —
and every #35-local test would still pass.** That is why ERR-030-023 covers both (1) and (2), and why
this section writes the drain out rather than describing it.

**#30 derives `MediaTriggerInput` from its own `MatchResult`;** #35 never names a #30 type (FR-ME-006).
This is the same posture `HumanSystemsDayInput` and `BoardDayInput` already use, and it is what keeps #35
free of a #30 reference.

**Provenance is enforced at #30's call seam, not inside #35.** #35 cannot verify that
`input.ManagedClubId` really is the managed club, only that the fixture involves it. Same division of
responsibility as #33's committed inputs.

**Where the sum happens.** With #46 as a second producer of the same quantity, the root **sums across
producers and clamps** before constructing `HumanSystemsDayInput` (ERR-033-003). Each producer's own
bound is `[MEDIA_DELTA_MIN, MEDIA_DELTA_MAX]`; the **clamp after summing** is the root's, and it must
exist — two producers each at their bound would otherwise exceed the field's contract, which is precisely
the failure a per-producer field would have made impossible and a shared field makes possible.

## 4.5 The #46 read seam (KD-6)

```
# in #46's aggregator — #35 knows nothing about this
foreach (var c in media.Conferences(ConferenceFilter.All))
    inbox.Project(c);                                  # value copies, never a live handle
```

Strictly one-directional. #35 fires no inbox event, holds no unread flag, and never references #46 — the
same posture by which #37 and #44 read the engine's ledger without the engine knowing they exist.

**This is what made the plan's *"how does #46 discover it?"* question dissolve.** It only looked hard
while media was assumed to **push**. A pull seam needs no message bus, no event registration, and no
coupling in the direction that would have made #35 depend on a spec authored after it.

## 4.6 Save composition (KD-7)

#35's sub-blob is composed into #30's `SeasonSaveCodec` alongside #40's, #33's, #44's and the rest, as a
length-prefixed **opaque** block: the outer codec never parses it, so `MEDIA_SAVE_FORMAT_VERSION` and
`SEASON_SAVE_FORMAT_VERSION` move independently (FR-ME-037). Layout in Appendix B.

**Two versions are in play and neither implies the other.** #35 changes **no** existing spec's serialized
representation — `HumanSystemsDayInput` is a **transient input struct**, not serialized state, which is
what makes ERR-033-003 cheap and keeps `HUMAN_SYSTEMS_SAVE_FORMAT_VERSION` untouched. #35's landing is
purely additive to the save layer.

**Migration posture at T2: none — pre-T2 saves are rejected fail-loud.** T2 bumps the outer version when
the sub-blob is first composed in, so a save written before it is not loadable after it. #35 defines no
migration path and no silent upgrade (the living-world slice-2 precedent). Cross-version migration is
**#50's** subject; recording the position here means #50 inherits it rather than discovering it.

## 4.7 Contracts with neighbours

| Neighbour | Contract |
|---|---|
| **#30** | Invokes **both** seams (queue at the post-round path, drain at tick step 3) plus the expiry slot; derives `MediaTriggerInput`; composes the sub-blob. **Sole caller** of all three. #35 references #30 never. |
| **#33** | Receives #35's delta as a **committed integer** on `HumanSystemsDayInput`, summed and clamped by the root. **#33 stays the sole writer of its own state** (FR-HS-002 intact); FR-HS-024 stays literally true. **No reference in either direction.** |
| **#46** | **Reads** #35's conference query (`#46 → #35`). #35 imposes nothing on #46 and knows nothing about it. Both specs file ERR-033-003 jointly, since both produce the same routed quantity. |
| **#45** | Deep tier only, and only as a **routed value** into its existing `BoardDayInput` — which already carries a deep-tier `MoraleSignalPermille` neutral at minimal, the same shape. No reference either way. |
| **#49** | #35 is a **producer**; the binding is the sibling `MediaTextBoundary` adapter (FR-LC-013/014). #49's core seam is **untouched** — the one #49 change is ERR-049-001, a wording fix to a single requirement. |
| **#22** | **Untouched.** #35 consumes neither `InteractionTextGenerator` nor `world.text`, so no cursor, corpus, or `WorldStore` surface changes. |
| **#16** | `_RESERVED_0x27_` / `SubsystemOrdinals 89` already exist and are already correct for a draw-free minimal tier — **no back-prop at approval**. `DeterministicRngService` is referenced at the deep tier only. |
| **#38** | Reads `ConferenceView` value copies for display; the **text** comes from the adapter, not from #35. |

**Standing review item:** #35 performs **no** write to any #33, #30, #45, #27 or #49 type. This cannot be
asserted from the reference graph at the deep tier (where #16 becomes reachable), so it is asserted
**behaviourally** in §5.8 and re-checked at each review.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-27 | — | Initial §4 (tiered assembly + DAG with the CS0104 pre-check, file layout, the `MediaTextBoundary` adapter, the two #30 seams, the #46 read seam, save composition, neighbour contracts). Status IN REVIEW. |
| 0.2 | 2026-07-27 | — | PASS-1 fixes. **M:** §4.4 now states **where the sum and clamp happen** — with #46 as a second producer of the same field, two producers each at their own bound would exceed the field's contract, so the **root's** post-sum clamp is load-bearing and was unstated (a per-producer field made this impossible; a shared field makes it possible, which is the cost of ERR-033-003 that must be named). **L:** recorded that `MediaTextBoundary.cs` in `src/media/` would **not compile** under FR-LC-012, rather than merely being untidy; that the adapter's intent check is **defence in depth, not the gate** (a boundary-only gate is bypassed by any other `MediaIntent` consumer); that `MediaSlots` is deliberately not named `InteractionSlots`; and that `MediaCatalogue` is separated precisely so deep-tier rows add entries without touching the lifecycle. |
#endregion
