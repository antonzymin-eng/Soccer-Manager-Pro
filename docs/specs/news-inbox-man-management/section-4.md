# News, Inbox & Man-Management #46 — Section 4: Architecture

**Created:** July 27, 2026
**Last Updated:** July 27, 2026 (v0.1 — initial section-file set)
**Version:** 0.1
**Status:** APPROVED

---

## 4.1 Assembly and reference direction

New assembly **`TacticalDirector.Inbox`** at `src/inbox/`, referencing **nothing, at every tier**.

```
root ──▶ {#30, #33, #35, #44, #45, #46, boundary}
  │         │                        ▲
  │         └── projectors ──────────┘   (root-side; push value copies INTO #46)
  │
  └──▶ boundary(InboxTextBoundary) ──▶ {#46, #49}

#46 ──▶ { }        (a leaf, at minimal AND deep)
```

**#46 is a leaf at every tier — stronger than #35's DAG**, which takes `TacticalDirector.DeterministicSim`
at its deep tier. #46 takes nothing at any tier because it is draw-free at every tier (KD-8), and that is
what makes §5.8's structural assertion **unconditional** rather than tier-scoped.

**The consequence worth naming: FR-LW-031 does not bar #46 from being authored.** A #46 that *read* its
five producers could not exist until all five did. Because items are **pushed in** as value copies by
root-side projectors, a #46 with **zero projectors wired** is complete, exercisable, and inert — so #46
lands on its own schedule and each projector lands with its producer. This is the sharpest argument for
root projection, and it is structural rather than stylistic.

**CS0104 pre-check.** #46 introduces `SourceTag`, `ItemKind`, `InboxIntent`, `InboxItem`, `ReadState`,
`PendingDelta`, `InboxCursors`, `InboxSlots`, `InboxStore`, `InboxSaveCodec`, `InboxFilter`. Each was
checked before authoring, because this project has hit CS0104 twice (`TacticTranslation`,
`PlayerAttributes`).

Two names deserve particular note:

- **`SourceTag` is deliberately not `ProducerTag`.** #49 owns `TextTemplateId.ProducerTag`, and the two
  share **neither membership nor meaning**: #30 is an event source but not a text producer; #22 is a text
  producer that emits no inbox items. Naming both "producer tag" is how this project's collisions started,
  so they are named apart **from the outset** rather than after a compile error (FR-NW-009).
- **`InboxSlots` is deliberately not `InteractionSlots` or `MediaSlots`** — FR-LC-014 pins that the
  producers' slots are disjoint, so a shared name would suggest a compatibility that must not exist.

## 4.2 File layout

```
src/inbox/
├── InboxConstants.cs           # the Appendix A catalogue — no magic numbers in formula code
├── SourceTag.cs                # #46's own event-source namespace (NOT #49's ProducerTag)
├── ItemKind.cs                 # the SERIALIZED identity; APPEND-only (schema key)
├── InboxIntent.cs              # the CATALOGUE identity; APPEND-only (template key) — never stored
├── PayloadSchema.cs            # (SourceTag, ItemKind) -> arity + slot meanings; the F2 gate
├── InboxItem.cs                # the stored record
├── ReadState.cs                # watermark + bounded exception set
├── PendingDelta.cs             # #46's own undelivered man-management consequences
├── InboxCursors.cs             # per-source id allocators
├── InboxStore.cs               # the log; Append / Query / MarkRead; the SINGLE writer
├── ManManagement.cs            # FM-NW-04 — deep tier; absent at minimal (FR-LW-031)
├── InboxSaveCodec.cs           # KD-6 sub-blob, version gate first
├── InboxSlots.cs               # #46's disjoint native slots for the adapter
└── tests/
```

**`InboxTextBoundary.cs` is deliberately absent from this tree** — it lives in the boundary layer beside
#35's `MediaTextBoundary` and #22's `LivingWorldTextBoundary`, because it is the one thing that references
both #46 and `TacticalDirector.Localization`. Under FR-LC-012 placing it here would not merely be untidy:
**it would not compile.**

**The root projectors are absent too**, and for the mirror reason: a projector references **both** its
producer and #46, so it must live where both are visible — the `SeasonSave` root (§4.3).

**`ManManagement.cs` is not created at the minimal tier.** An interaction surface with no consequence path
wired is the phantom surface FR-LW-031 forbids.

**`PayloadSchema.cs` is its own file because it is a contract, not a helper.** It is the single place the
`(SourceTag, ItemKind) → arity` mapping lives, and it is what F2 checks at both `Append` and decode.
Inlining those arities at call sites is how the payload schema would drift into being an unversioned
convention again (FR-NW-011).

## 4.3 The root projectors and their sites (KD-2)

A projector is small root-side code that reads a producer's committed state and calls `Append`. It lives
at the **`TacticalDirector.SeasonSave` root** — the same layering #33 uses for `RouteIntoLivingWorld`
(*"owned by the SeasonSave root, NOT #30, NOT living-world"*).

```
# (1) MATCH — inside #30's AdvanceAndPlayNextRound(), right after EmitMatchOutcome(result)
inbox.Append(SourceTag.Season, ItemKind.MatchPlayed, worldDay, INBOX_NO_SUBJECT,
             new[]{ result.HomeClubId, result.AwayClubId, result.HomeScore,
                    result.AwayScore, result.RoundIndex });

# (2) PRESS — the same post-round path, after #35's own queue seam
foreach (var c in media.Conferences(ConferenceFilter.PendingOnly))
    inbox.Append(SourceTag.Media, ItemKind.PressQueued, worldDay, c.SubjectPlayerId, …);

# (3) WORLD-TICK PRODUCERS — immediately after THAT producer's own step in RunWorldTickInFixedOrder
#     (#44 discipline, #45 board, #31 transfers)
```

**Site (1) is not a matter of taste.** The scoreline exists **only** at that instant — #30's `Fixture`
retains `Played` and the result goes to the table (§1.4(a)/(b)). A projector sited anywhere later cannot
produce an accurate match item, which is the whole of KD-1.

**Site (3) is what makes KD-7 coherent.** A naive reading of *"#46 takes no tick slot"* leaves a
world-tick producer — a suspension incurred on a Tuesday, a board-confidence collapse — with **nowhere to
emit**, and both obvious repairs are wrong: give #46 a slot (contradicting KD-7), or silently drop
non-fixture items. Siting each projector at its **producer's** existing step resolves it: #46 owns no
step, every emission is inside the pinned fixed order, and **ordering is inherited from #30 rather than
defined by #46** (§7.4 R-4 records the consequence).

**Each projector lands with its producer, never ahead of it** (FR-LW-031). With none wired the feed is
empty and every #46 surface is still exercisable (§4.1).

## 4.4 The `InboxTextBoundary` sibling adapter (KD-4)

```
# in the boundary layer — NOT in src/inbox/ and NOT in #49
class InboxTextBoundary
{
    LocalizedTextRequest BuildRequest(SourceTag src, ItemKind kind, ulong selection, in InboxSlots slots)
    {
        var intent = IntentFor(src, kind);                       # (SourceTag, ItemKind) -> InboxIntent
        RequireRenderableIntent(intent);                         # FR-LC-015 mirror — defence in depth
        var id = new TextTemplateId(ProducerTag.Inbox, (int)intent);
        return new LocalizedTextRequest(id, selection, FormatSlots(slots), citation: none);
    }
}
```

**The mapping is the adapter's job, and that is why a saved item never stores a localization ordinal**
(FR-NW-030). This is the split #35 did not need: #35's records are all its own, so one enum sufficed;
**#46's items come from five sources whose kinds it does not own**, so forcing them through one intent
enum would make adding a #31 item kind an edit to #46's localization roster.

The cost is that **both** enums carry an APPEND-only contract, for two distinct reasons — reordering
`ItemKind` re-reads every saved `Payload` under the wrong schema; reordering `InboxIntent` re-points every
item at the wrong template — and **neither has a version gate that would catch it** (§5.6 asserts both).

**#46's own obligations stay on #46's side:** the FR-LC-015 value gate and the FR-LC-008a coverage
assertion over the **full** roster (item text **and** man-management prompts/options). The adapter's check
is defence in depth, not the gate — a gate living only in the boundary layer is bypassed by any other
consumer of the enum.

## 4.5 The #30 drain seam (KD-3)

#46 has **no** tick slot of its own, but it is drained at one that already exists:

```
# inside #30's RunWorldTickInFixedOrder(), step 3, per player — root-side code (ERR-030-024)
ext := 0
foreach producer in ExternalDeltaProducers:                   # {#35, #46, ...}
    if producer.TryTakePendingDelta(playerId, out var d):  ext += d
ext := Clamp(ext, EXTERNAL_DELTA_MIN, EXTERNAL_DELTA_MAX)     # clamp AFTER summing
humanSystems.AdvanceHumanSystemsDay(state, new HumanSystemsDayInput(…, externalDelta: ext), worldDay);
```

**The loop is the generalization ERR-030-024 files.** #35's seam drains one producer; with #46 it must
drain every producer, sum, and clamp. Filing it as a generalization rather than as a second seam is what
keeps `HumanSystemsDayInput` at **one** external field (KD-3) — the alternative grows the struct by a
field per producer, each addition a back-prop against an approved spec.

**#33 sees no difference.** It receives one committed integer either way, all morale mutation stays inside
`AdvanceHumanSystemsDay`, and FR-HS-002's single-writer property is untouched. That is the whole argument
for routing rather than writing.

## 4.6 Save composition (KD-6)

#46's sub-blob is composed into #30's `SeasonSaveCodec` alongside #40's, #33's, #44's, #35's and the rest,
as a length-prefixed **opaque** block: the outer codec never parses it, so `INBOX_SAVE_FORMAT_VERSION` and
`SEASON_SAVE_FORMAT_VERSION` move independently (FR-NW-035). Layout in Appendix B.

**#46 changes no existing spec's serialized representation.** `HumanSystemsDayInput` is a **transient
input struct**, not serialized state, so ERR-033-003 carries **no** `HUMAN_SYSTEMS_SAVE_FORMAT_VERSION`
bump. #46's landing is purely additive to the save layer.

**Migration posture at T2: none — pre-T2 saves are rejected fail-loud.** The living-world slice-2
precedent; cross-version migration is **#50's** subject, and recording the position here means #50
inherits it rather than discovering it.

## 4.7 Contracts with neighbours

| Neighbour | Contract |
|---|---|
| **#30** | Hosts two things #46 needs and owns neither: the **match projector site** (after `EmitMatchOutcome`) and the **step-3 drain**. #46 references #30 never, and cites **no step number of its own** (KD-7). |
| **#33** | Receives #46's delta as a committed integer, **summed with every other producer's and clamped by the root**. **#46 never writes and never reads morale** (FR-NW-005/006 — FR-HS-025 bars the two-way coupling). No reference either way. |
| **#35** | **#46 shows, #35 owns** (KD-5). #46's projector reads #35's read-only conference query — the surface #35's own KD-6 exposes for exactly this. #46 defines no press intent and renders no press text. Neither references the other; the two specs agree from opposite sides. |
| **#44 / #45 / #31** | Root projectors read their committed day-state at their own pinned steps. No reference in either direction. |
| **#37** | **Not an inbox source.** It holds no state and is a live-match reader, so it cannot be called after the fact (§1.4(c)). If post-match stats become an item, the **root** must capture #37's view models at emission time — an extension, not a #46 change. |
| **#49** | #46 is a **producer**; the binding is the sibling `InboxTextBoundary` (FR-LC-013/014). #49's core is untouched, and #46 **inherits #35's ERR-049-001** rather than filing its own. |
| **#38** | Reads value copies; the **text** comes from the adapter, not from #46. |
| **#16** | **Untouched — and no `_RESERVED_` row exists for #46.** A future stochastic news generator needs a **fresh allocation**, not a promotion (KD-8). |
| **#50** | Registers `INBOX_SAVE_FORMAT_VERSION` in the version registry. |

**Standing review item:** #46 performs **no** write to any producer's type, to #33, or to #30 state. #46
references nothing, so the reference graph *does* prove most of this — but the **projectors** are root-side
code holding both sides, and a projector that mutated its producer while reading it would be invisible to
#46's own tests. §5.8 asserts producer-state immutability across every projection.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-27 | — | Initial §4 (leaf-at-every-tier assembly + DAG with the FR-LW-031 consequence spelled out, the CS0104 pre-check with both deliberate non-collisions, file layout with the three deliberate absences, the root projectors and their per-producer sites, the `InboxTextBoundary` adapter and the two-enum split, the #30 drain generalization, save composition, neighbour contracts). The standing review item is scoped to the **projectors** rather than to #46 — #46 references nothing, so the only place a foreign write could hide is root-side code holding both sides. Status IN REVIEW. |
#endregion
