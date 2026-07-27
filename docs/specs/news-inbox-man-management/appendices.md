# News, Inbox & Man-Management #46 — Appendices

**Created:** July 27, 2026
**Last Updated:** July 27, 2026 (v0.2 — PASS-1 fix pass)
**Version:** 0.2
**Status:** APPROVED

---

## Appendix A — Constant catalogue

Region order per Spec #20: Fixed → Derived → Cross → GT, **omitting any region with no constants** (#20
prohibits empty regions). #46 has no `[EST]` constants and — because it has **no reserved determinism
value to promote** (KD-8) — **no `[CROSS-PENDING]` constants either**, so neither region appears.
`[GT]` values are **illustrative pending the T3 balance pass** (§7.1).

### A.1 Fixed

| Constant | Value | Tag | Notes |
|---|---|---|---|
| `INBOX_SAVE_FORMAT_VERSION` | `1` | `[FIXED]` | The sub-blob's own version gate (KD-6). Independent of `SEASON_SAVE_FORMAT_VERSION`. |
| `INBOX_NO_SUBJECT` | `-1` | `[FIXED]` | The "this item has no player subject" sentinel. **Not `0`** — `0` is a valid `PlayerId`, the `default(struct)`-looks-valid trap #40's `BoardModifier` F4 and #33's `PersonalityProfile` F4 both exist to catch. |
| `INBOX_DELTA_MIN` / `INBOX_DELTA_MAX` | `-1000` / `+1000` | `[FIXED]` | The per-mille bound on a **single** man-management delta. Fixed rather than `[GT]` because it is the contract #33's field is specified against; the *magnitudes authors choose within it* are the balance knob. |
| `EXTERNAL_DELTA_MIN` / `EXTERNAL_DELTA_MAX` | `-1000` / `+1000` | `[FIXED]` | The bound applied by the **root**, **after** summing every producer's contribution (§3.5). Distinct from the pair above: that one bounds one producer, this one bounds the sum. Both are needed — two producers each within their own bound can exceed the field's contract, which is the cost of the single shared field (KD-3). |
| `INBOX_SELECTION_SEED` | `0x9E3779B97F4A7C15` | `[FIXED]` | The local SplitMix64 seed for the FR-LC-004 `ulong` (FR-NW-033). Changing it re-keys every future phrasing selection. |

### A.2 Derived

| Constant | Formula | Tag | Notes |
|---|---|---|---|
| `SOURCE_TAG_COUNT` | `Enum.GetValues(typeof(SourceTag)).Length` | `[DERIVED]` | The length of `InboxCursors.NextItemIdBySource`. Derived from the enum, never a literal — two surfaces carrying private copies of a member count is the `POSITION_COUNT` parallel-surface defect. |
| `INBOX_INTENT_COUNT` | `Enum.GetValues(typeof(InboxIntent)).Length` | `[DERIVED]` | What the FR-LC-008a coverage assertion iterates. Same reasoning. |
| `PAYLOAD_MAX` | `max` over Appendix C's arities | `[DERIVED]` | The widest payload any `(SourceTag, ItemKind)` declares — the storage bound, never set independently of the schema table it summarises. |

### A.3 Cross (consumed read-only; never re-declared)

| Constant / type | Authority | Notes |
|---|---|---|
| `TextTemplateId`, `LocalizedTextRequest`, `ILocalizer`, `ProducerTag` | #49 §2.2 | Used **only inside `InboxTextBoundary`**, which is not a #46 assembly. **`ProducerTag` is #49's; #46's own namespace is `SourceTag`** and the two are deliberately named apart (FR-NW-009). |
| `HumanSystemsDayInput`, `ExternalDeltaPermille` | #33 §2.2 (via ERR-033-003) | #30 assembles it; #46 hands over a bare `int` and never names the type. |
| `MatchResult`, `Fixture` | #30 | **Not consumed.** Listed to record that the *root projector* reads them and hands #46 a `Payload` — #46 names no #30 type. |
| `PressConference` (via #35's read-only query) | #35 | **Not consumed by #46.** The *projector* reads it; #46 defines no press intent and stores only a payload (KD-5). |

### A.4 GT (illustrative, balance-pass pending)

| Constant | Value | Notes |
|---|---|---|
| `INBOX_MAX_ITEMS` | `200` | The hard log bound (FR-NW-014). A full log **drops the oldest**, never refuses the newest (F6). Also the bound on `Query`'s scan and on the exception set. |
| `INBOX_RETENTION_DAYS` | `365` | The recency window (KD-1). Items past it are invisible to a query and evicted at the next `Append`. **The knob §7.4 R-1 says will be argued with** — and the reason a raised bound is the wrong answer (an unbounded log is a save-size commitment, not a tuning choice). |
| `INBOX_TALK_WINDOW_DAYS` | `7` | How long before a player can be talked to again (F5). Deep tier only. |
| `INBOX_BUDGET_APPEND_US` | `10` | §6.3 ceiling for one `Append`. A **ceiling, not a measurement** — no certified number exists for #46. |
| `INBOX_BUDGET_QUERY_MS` | `2` | §6.3 ceiling for one full-log `Query`. **In milliseconds deliberately** — a screen-open operation at human cadence should not carry a loop-step budget. Same caveat. |
| `INBOX_BUDGET_DRAIN_US` | `2` | §6.3 ceiling for one `TryTakePendingDelta`. Same caveat — and **the one to measure first**, since it is multiplied by every player, every day, for the whole career. |

**Where the man-management deltas are not.** No per-option delta value appears in this catalogue: they
live in the authored man-management intent table, bounded by `INBOX_DELTA_MIN`/`MAX`. That keeps the
balance surface in authored data rather than in constants, and it is why §5's tests assert bounds and
direction rather than magnitude.

## Appendix B — Save sub-blob layout (KD-6)

Canonical field order, written through #16's `CanonicalSerializer`. **Opaque to `SeasonSaveCodec`** — the
outer codec sees a length-prefixed byte block and never parses it (FR-NW-035).

| # | Field | Type | Notes |
|---|---|---|---|
| 1 | `INBOX_SAVE_FORMAT_VERSION` | `u16` | **Version gate first** — read and checked before any field below it is interpreted (F7). |
| 2 | `CursorCount` | `i32` | Length prefix for the per-source allocator array — read through the overflow-safe bound compared against `total − offset`, never `offset + need` (F7). |
| 3 | per cursor × `CursorCount` | `i32` | `NextItemId`, in **`SourceTag` ordinal order**. A new `SourceTag` **extends** this array; it never reorders it (FR-NW-012). |
| 4 | `ItemCount` | `i32` | Length prefix, same bound treatment. |
| 5 | per item × `ItemCount` | — | `SourceTag` (`u8`); `ItemKind` (`u8`); `ItemId` (`i32`); `WorldDay` (`u32`); `SubjectId` (`i32`); then **exactly `PayloadArityOf(SourceTag, ItemKind)`** × `i32`. |
| 6 | `ReadBeforeWorldDay` | `u32` | The watermark. |
| 7 | `ExplicitReadKeyCount` | `i32` | Length prefix, same bound treatment. |
| 8 | per key × count | — | `SourceTag` (`u8`), `ItemId` (`i32`). |
| 9 | `PendingDeltaCount` | `i32` | Length prefix, same bound treatment. |
| 10 | per delta × count | — | `TargetPlayerId` (`i32`); `DeltaPermille` (`i32`); `RecordedWorldDay` (`u32`). |
| — | *(trailing-byte guard)* | — | The read MUST end exactly at the block end (F7). |

Items are written in the **canonical total order** `(WorldDay, SourceTag, ItemId)`, read keys in
ascending `(SourceTag, ItemId)`, and deltas in ascending `TargetPlayerId` — so the blob is a function of
**state**, never of insertion or iteration order. This is also what makes the KD-9 identity claim
checkable: an empty store is a version header plus **four zero counts** (row 2, 4, 7, 9).

**Decode validates, it does not trust** (FR-NW-036): `SourceTag` and `ItemKind` must be defined (F1); the
payload length must equal the arity Appendix C pins for that **pair** (F2) — which is what makes the
schema contract enforceable rather than a convention; `DeltaPermille` must be in range **and non-zero**
(F3), because a zero row is structurally impossible to produce and therefore means corruption; and the
cursor array length must equal `SOURCE_TAG_COUNT`.

**Deliberately absent — four things, each for its own reason:**

1. **Any RNG cursor or stream state.** #46 is draw-free at **every** tier (KD-8) — unlike #35, whose deep
   tier draws — so there is nothing to persist at any tier.
2. **Any rendered string, or any locale identifier.** FR-LC-006: a save must not depend on the locale it
   was written in. **This is the point of temptation** — caching an item's rendered headline alongside its
   payload is the obvious optimisation and would silently make every save locale-specific.
   T-NW-LOC-006 fails when it happens.
3. **Any `InboxIntent`** (FR-NW-030). The adapter maps `(SourceTag, ItemKind) → InboxIntent` at render
   time, so a saved item never carries a localization ordinal and the catalogue can be re-organised
   without touching saves.
4. **Any per-item read flag.** Read state is the watermark plus the bounded exception set (KD-6). A flag
   would grow a byte per item forever and make *"mark all read"* an O(n) rewrite of the whole blob.

**APPEND-only** (FR-NW-036). New fields go at the **end** behind a version bump. Appending a `SourceTag`,
an `ItemKind`, or an `InboxIntent` member is **not** a layout change and needs no bump — but **reordering
any of the three is a silent catastrophe with no gate to catch it**, and each fails differently:
`SourceTag` re-bases the cursor array, `ItemKind` re-reads every payload under the wrong schema, and
`InboxIntent` re-points every item at the wrong template.

## Appendix C — Rosters and payload schemas

### C.1 `SourceTag` — #46's own event-source namespace

| Member | Ordinal | Producer | Projector site (§4.3) |
|---|---|---|---|
| `Season` | `0` | #30 | after `EmitMatchOutcome(result)` — **the only instant the scoreline exists** |
| `Media` | `1` | #35 | the post-round path, after #35's own queue seam |
| `Discipline` | `2` | #44 | immediately after #44's own tick step |
| `Board` | `3` | #45 | immediately after #45's own tick step |
| `Transfers` | `4` | #31 | immediately after #31's own tick step |

**Not `ProducerTag`** (FR-NW-009). #49 owns that name for a *text-producer family*, and the two sets
share neither membership nor meaning: **#30 is an event source but not a text producer; #22 is a text
producer that emits no inbox items.** Naming them apart from the outset is the `TacticTranslation` /
`PlayerAttributes` CS0104 lesson applied **before** the collision rather than after.

### C.2 Payload schemas — pinned per `(SourceTag, ItemKind)`

| `SourceTag` | `ItemKind` | Arity | Slots |
|---|---|---|---|
| `Season` | `MatchPlayed` | 5 | `{ homeClubId, awayClubId, homeScore, awayScore, roundIndex }` |
| `Media` | `PressQueued` | 2 | `{ conferenceId, questionIntentOrdinal }` |
| `Discipline` | `SuspensionStarted` | 2 | `{ matchesBanned, competitionId }` |
| `Board` | `ConfidenceBand` | 1 | `{ bandOrdinal }` |
| `Transfers` | `BidReceived` | 3 | `{ playerId, fromClubId, feeThousands }` |

**Each row is APPEND-only in two independent senses** (FR-NW-011): a **new** `(SourceTag, ItemKind)` pair
may be added freely, but the **arity and slot meanings of an existing pair must never change** — a
meaning change re-reads every stored item and is an `INBOX_SAVE_FORMAT_VERSION` bump.

F2 checks the **arity** at both `Append` and decode, which makes half of that contract mechanical. The
other half — what slot 3 *means* — is protected only by this table and by discipline, and is retained as
the residual risk §7.4 R-2.

**`Media/PressQueued` stores #35's intent ordinal, not text** — #46 defines no press intent and renders no
press text (KD-5). The item is a pointer into #35's roster; #35's own adapter renders it.

### C.3 `ItemKind` vs `InboxIntent` — why there are two

| | `ItemKind` | `InboxIntent` |
|---|---|---|
| **Role** | the **serialized** identity | the **catalogue** identity |
| **Stored?** | yes, in every item | **never** (FR-NW-030) |
| **Keyed with** | `SourceTag`, to fix the payload schema | `ProducerTag.Inbox`, to fix the `TextTemplateId` |
| **A reorder breaks** | every stored `Payload`, read under the **wrong schema** | every item, pointed at the **wrong template** |
| **Contract** | APPEND-only (FR-NW-010) | APPEND-only (FR-NW-029) |

**#35 collapsed to a single `MediaIntent`; #46 cannot**, and the difference is load-bearing rather than
stylistic. #35's records are all its own. **#46's items come from five sources whose kinds it does not
own**, so a single intent enum would make adding a #31 item kind an edit to #46's *localization roster* —
coupling a producer's schema to #46's catalogue. The split keeps the two axes independent; the price is
two APPEND-only contracts instead of one, and §5.6 asserts both separately because **checking one proves
nothing about the other**.

**Not tabulated: the base-locale phrasings, or the man-management delta values.** The phrasings are
**#49's** catalogue rows (#46 supplies only the roster they must cover, FR-NW-032), and the deltas are
authored data bounded by `INBOX_DELTA_MIN`/`MAX`. Tabulating either here would create a second source for
data another surface owns — and in #49's case would be a baked string in a sim spec, which is exactly what
FR-LC-002 exists to prevent.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-27 | — | Initial appendices (A.1 Fixed incl. the two distinct delta bounds, A.2 Derived, A.3 Cross, A.4 GT; B save layout with the four deliberately-absent items and the three differently-failing reorder hazards; C.1 the `SourceTag` roster + projector sites, C.2 the pinned payload schemas, C.3 the two-identity comparison). Status IN REVIEW. |
| 0.2 | 2026-07-27 | — | PASS-1 fixes. **M:** the three `[GT]` budget ceilings declared in §6.3 were **absent from this catalogue**, which is meant to be the single catalogue and is what a reader greps for tag discipline (the #45 PASS-1 M-2 defect, now seen for the third time in this wave) — added to A.4. **M:** **C.2 added** — the payload schemas were referenced by §2/§3 but pinned nowhere, which is precisely what made the schema an unversioned convention (PASS-1 M-2); they now have a single authoritative table, with the arity-vs-meaning split stated. **L:** A.1 gained `EXTERNAL_DELTA_MIN`/`MAX` as **distinct from** the per-producer bound, since the root's post-sum clamp needs its own constant and conflating them is how the clamp gets dropped; A.2 added `PAYLOAD_MAX` and the `POSITION_COUNT` derivation rationale; B gained the decode-validates paragraph and the locale-caching *point of temptation*; C.3 added as a side-by-side, since the two-enum split is the design decision most likely to be "simplified". |
#endregion
