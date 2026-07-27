# Media & Press Interactions #35 — Appendices

**Created:** July 27, 2026
**Last Updated:** July 27, 2026 (v0.2 — PASS-1 fix pass)
**Version:** 0.2
**Status:** APPROVED

---

## Appendix A — Constant catalogue

Region order per Spec #20: Fixed → Derived → Cross → GT, **omitting any region with no constants** (#20
prohibits empty regions). #35 has no `[EST]` constants, so that region does not appear. `[GT]` values are
**illustrative pending the T3 balance pass** (§7.1) — the spec's contract is their *shape and identity
behaviour*, never their magnitude, and §5 asserts nothing else.

### A.1 Fixed

| Constant | Value | Tag | Notes |
|---|---|---|---|
| `MEDIA_SAVE_FORMAT_VERSION` | `1` | `[FIXED]` | The sub-blob's own version gate (KD-7). Independent of `SEASON_SAVE_FORMAT_VERSION`; bumping either never implies the other (§4.6). |
| `MEDIA_INTENT_OPTION_BAND_START` | `1000` | `[FIXED]` | The question/option ordinal boundary (FR-ME-010). **`[FIXED]` and asserted by test, not a tunable:** it is a save-correctness boundary — the ordinal is serialized *and* is the #49 catalogue key (KD-10), so moving it re-points every saved conference. The gap to `1000` is deliberate headroom so appending questions can never reach the option band. |
| `MEDIA_NO_SUBJECT` | `-1` | `[FIXED]` | The "this question has no player subject" sentinel. **Not `0`** — `0` is a valid `PlayerId`, and a `0` sentinel is the `default(struct)`-looks-valid trap #40's `BoardModifier` F4 and #33's `PersonalityProfile` F4 both exist to catch. |
| `MEDIA_UNANSWERED` | `-1` | `[FIXED]` | The `AnsweredOptionIndex` sentinel. Not `0`, which is a valid option index. |
| `MEDIA_NOT_ADVANCED_SENTINEL` | `uint.MaxValue` | `[FIXED]` | The unadvanced day cursor. **Not `0`** — day `0` is a legal world day, and a `0` sentinel silently no-ops a day-0 advance instead of failing loud (#33 FR-HS-008). |
| `MEDIA_DELTA_MIN` / `MEDIA_DELTA_MAX` | `-1000` / `+1000` | `[FIXED]` | The per-mille bound on a single consequence delta (FR-ME-032). Fixed rather than `[GT]` because it is the contract #33's field is specified against, not a balance knob — the *magnitudes authors choose within it* are the balance knob. |
| `MEDIA_SELECTION_SEED` | `0x9E3779B97F4A7C15` | `[FIXED]` | The local SplitMix64 seed (§3.5). Changing it re-keys every future phrasing selection — a deliberate, visible act, never a tidy-up. |
| `MEDIA_PURPOSE_RADIX` | `16` | `[FIXED]` | The §3.5 / §3.6 purpose radix. **Never "the current purpose count"** — a growing radix re-keys every historical ordinal the moment a purpose is appended, breaking cross-version replay parity (#41's finding, adopted by #45 and here). |
| `MEDIA_SUBJECT_STRIDE` | `65536` | `[FIXED]` | The §3.6 subject stride; bounds the id space the deep-tier ordinal keeps injective. |
| `MEDIA_STREAM_SITE_ID` | `"media.selection"` | `[FIXED]` | The single deep-tier `RegisterStream` site id (FR-ME-019). |
| `MEDIA_STREAM_ENTITY_SENTINEL` | `-1` | `[FIXED]` | The fixed entity for that single registration — **not** a club or player id, which is what keeps #35 at exactly one registration regardless of world size. |

### A.2 Derived

| Constant | Formula | Tag | Notes |
|---|---|---|---|
| `MEDIA_QUESTION_INTENT_COUNT` | count of `MediaIntent` members below `MEDIA_INTENT_OPTION_BAND_START` | `[DERIVED]` | Derived from the enum, never a literal — two surfaces carrying private copies of a member count is the `POSITION_COUNT` parallel-surface defect. |
| `MEDIA_OPTION_INTENT_COUNT` | count of `MediaIntent` members at or above the band start | `[DERIVED]` | Same reasoning. Together these two are what the FR-ME-012 coverage assertion iterates. |

### A.3 Cross (consumed read-only; never re-declared)

| Constant / type | Authority | Notes |
|---|---|---|
| `TextTemplateId`, `LocalizedTextRequest`, `ILocalizer`, `ProducerTag` | #49 `localization-accessibility` §2.2 | Used **only inside `MediaTextBoundary`**, which is not a #35 assembly (FR-LC-012 makes referencing it from `src/media/` a build error). |
| `HumanSystemsDayInput`, `ExternalDeltaPermille` | #33 §2.2 (via ERR-033-003) | #30 assembles it; #35 hands over a bare `int` and never names the type. |
| `_RESERVED_0x27_`, `SubsystemOrdinals.Media` (89) | #16 §3.4 | `[CROSS-PENDING]` until the **deep-tier** promotion (§8.3). Already present and already correct — **no back-prop at approval**. |
| `MatchResult` | #30 §3.4 | **Not consumed.** Listed to record that #30 derives `MediaTriggerInput` from it and #35 never names a #30 type (FR-ME-006). |

### A.4 GT (illustrative, balance-pass pending)

| Constant | Value | Notes |
|---|---|---|
| `MEDIA_MAX_PENDING_CONFERENCES` | `8` | The queue bound (FR-ME-023). A full queue **drops** rather than throwing (F7). Also the bound on the per-day expiry sweep and the per-player drain scan, which is why §6.3 flags it as the one number to re-check if the drain budget ever bites. |
| `MEDIA_MAX_OPTIONS` | `4` | Answer options per conference. A conference the client cannot fit on a screen is an authoring error, caught at the catalogue boundary (F4). |
| `MEDIA_ANSWER_WINDOW_DAYS` | `3` | Days from queue to `DeadlineWorldDay`. Long enough that a player advancing a week at a time still sees most conferences; short enough that the queue drains. |
| `MEDIA_MAX_CONSEQUENCE_TARGETS` | `4` | Entries per answer (KD-8). **Enforced at the authoring boundary, not by review** — because review is exactly what fails when a catalogue grows one row at a time (§7.4 R-6). At the minimal tier the effective arity is **0 or 1**. |
| `MEDIA_BUDGET_QUEUE_US` | `5` | §6.3 ceiling for one `TryQueueConference` call. A **ceiling, not a measurement** — no certified number exists for #35. |
| `MEDIA_BUDGET_EXPIRY_US` | `10` | §6.3 ceiling for one day's expiry sweep. Same caveat. |
| `MEDIA_BUDGET_DRAIN_US` | `2` | §6.3 ceiling for one `TryTakePendingDelta` call. Same caveat — and **the one to measure first**, since it is multiplied by every player, every day, for the whole career. |

**Where the consequence *magnitudes* are not.** No per-answer delta value appears in this catalogue, and
that is deliberate: the deltas live in `MediaCatalogue`'s authored rows, bounded by `MEDIA_DELTA_MIN` /
`MEDIA_DELTA_MAX`. That is also why §7.4 R-1 exists — T2's behavioural neutrality is a property of those
**authored rows**, not of any constant here.

## Appendix B — Save sub-blob layout (KD-7)

Canonical field order, written through #16's `CanonicalSerializer`. **Opaque to `SeasonSaveCodec`** — the
outer codec sees a length-prefixed byte block and never parses it (FR-ME-037).

| # | Field | Type | Notes |
|---|---|---|---|
| 1 | `MEDIA_SAVE_FORMAT_VERSION` | `u16` | **Version gate first** — read and checked before any field below it is interpreted (F8). |
| 2 | `NextConferenceId` | `i32` | From `MediaCursors`. |
| 3 | `LastAdvancedWorldDay` | `u32` | The F6 guard's state (§2.2). `MEDIA_NOT_ADVANCED_SENTINEL` round-trips as itself. |
| 4 | `ConferenceCount` | `i32` | Length prefix — read through the overflow-safe bound compared against `total − offset`, never `offset + need` (F8; the `MatchSaveCodec` hardening). |
| 5 | per conference × `ConferenceCount` | — | `ConferenceId` (`i32`); `QuestionIntent` (`i32`); `OptionCount` (`i32`) then that many `MediaIntent` (`i32`); `SubjectPlayerId` (`i32`); `TriggerRoundIndex` (`i32`); `QueuedWorldDay` (`u32`); `DeadlineWorldDay` (`u32`); `AnsweredOptionIndex` (`i32`). |
| 6 | `PendingDeltaCount` | `i32` | Length prefix, same bound treatment. |
| 7 | per delta × `PendingDeltaCount` | — | `TargetKind` (`u8`); `TargetId` (`i32`); `DeltaPermille` (`i32`); `RecordedWorldDay` (`u32`). |
| — | *(trailing-byte guard)* | — | The read MUST end exactly at the block end (F8). |

Conferences are written in **ascending `ConferenceId` order** and deltas in **ascending
`(TargetKind, TargetId)` order**, so the blob is a function of state — never of insertion or iteration
order.

**Decode validates, it does not trust** (FR-ME-038): every `MediaIntent` ordinal must be defined, every
question intent must be **below** `MEDIA_INTENT_OPTION_BAND_START` and every option intent **at or above**
it (F5), `OptionCount` must be within `[1, MEDIA_MAX_OPTIONS]`, `DeltaPermille` must be in range **and
non-zero** (F8 / T-ME-FAIL-007 — a zero row is structurally impossible to produce, so encountering one is
corruption), and `AnsweredOptionIndex` must be `MEDIA_UNANSWERED` or a valid index into that conference's
own option list.

**Deliberately absent — four things, each for its own reason:**

1. **Any `RngStreamState` or cursor.** The minimal tier is draw-free and the deep tier's draws are keyed
   and position-independent, so at no tier is there a cursor to persist (FR-ME-020).
2. **Any rendered string, or any locale identifier.** FR-LC-006: a save must not depend on the locale it
   was written in. **This is the point of temptation** — caching the rendered question text alongside the
   conference would look like an obvious optimisation and would silently make saves locale-specific.
   T-ME-LOC-005 is what fails when it happens.
3. **Any copy of morale, board confidence, or the league table.** Mirroring any of them re-introduces a
   double truth that would only diverge *after* a restore.
4. **Any zero-valued `PendingDelta` row** (FR-ME-033). Not merely unnecessary: keeping *"is a delta
   pending for this player?"* answerable **by presence** is what makes the drain a single lookup, and
   what stops a 38-round season of expiries filling an APPEND-only blob with inert rows.

**APPEND-only** (FR-ME-038). New fields go at the **end** with a `MEDIA_SAVE_FORMAT_VERSION` bump.
Appending a `MediaIntent` member is **not** a layout change and needs no bump — but **reordering one is a
silent catastrophe with no gate to catch it** (KD-10), which is why ordinal stability is a test
(T-ME-LOC-002) rather than a convention.

## Appendix C — The `MediaIntent` roster and ordinal bands

| Band | Ordinal range | Contents | Used as |
|---|---|---|---|
| **Reserved** | `0` | `None` | the FR-ME-011 gate value; **never rendered**, never stored |
| **Questions** | `[1, MEDIA_INTENT_OPTION_BAND_START)` | `QPostWin`, `QPostDraw`, `QPostLoss`, `QSubjectForm`, `QBoardObjective`, … | `PressConference.QuestionIntent` **only** |
| **Options** | `[MEDIA_INTENT_OPTION_BAND_START, …)` | `OSupportive`, `OCritical`, `ODeflect`, `ONoComment`, … | `PressConference.OptionIntents[]` **only** |

**One enum, two rosters** (KD-1). The alternative — two enums — was rejected because the #49 seam keys on
a single `(ProducerTag, LocalOrdinal)` pair, so two enums would need either two producer tags or an
ad-hoc offset applied by the adapter; the band makes the split explicit, checkable (F5), and coverable by
**one** FR-LC-008a assertion.

**Both bands are APPEND-only and neither may be reordered** (KD-10 / FR-ME-009). The ordinal is
load-bearing twice: it is **serialized** inside `PressConference` and it is the **`LocalOrdinal`** the #49
catalogue is keyed on. A reorder therefore re-points every saved conference at a different template *and*
invalidates every catalogue row **with no version gate to catch it** — the save loads cleanly and renders
the wrong text, which is the worst available failure shape. A retired value keeps its ordinal and its
base-locale row.

**`ONoComment` is a real answer, not a null state.** It is an ordinary member of a conference's option
roster with its own consequence entry (frequently `0`, which by FR-ME-033 records nothing), and it is what
expiry resolves to (FR-ME-027). This is the fact behind KD-9's precondition being *"every consequence
`0`"* rather than *"no conference answered"* — a distinction this spec's own review cycle got wrong three
times before T-ME-ID-002 was written to catch it mechanically.

**Not tabulated: the base-locale phrasings, or the per-answer consequence values.** The phrasings are
**#49's** catalogue rows (#35 supplies only the roster they must cover, FR-ME-012), and the consequence
values are authored data in `MediaCatalogue` bounded by `MEDIA_DELTA_MIN`/`MAX`. Tabulating either here
would create a second source for data another surface owns — and in #49's case would be a baked string in
a sim spec, which is the thing FR-LC-002 exists to prevent.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-27 | — | Initial appendices (A.1 Fixed incl. the three sentinel rationales, A.2 Derived, A.3 Cross, A.4 GT; B save layout with the four deliberately-absent items; C the roster + ordinal-band table). Status IN REVIEW. |
| 0.2 | 2026-07-27 | — | PASS-1 fixes. **M:** the three `[GT]` budget ceilings declared in §6.3 were **absent from this catalogue**, which is meant to be the single catalogue and is what a reader greps for tag discipline (the #45 PASS-1 M-2 defect) — added to A.4 with their ceiling-not-measurement caveat. **L:** A.1 gained the reason `MEDIA_INTENT_OPTION_BAND_START` is `[FIXED]`-and-asserted rather than tunable, and why `MEDIA_DELTA_MIN`/`MAX` are `[FIXED]` (they are #33's field contract, not a balance knob); A.2 added, deriving both intent counts from the enum per the `POSITION_COUNT` precedent; B's decode-validates paragraph and the locale-caching *point of temptation* spelled out; C gained the why-one-enum-not-two rationale and the `ONoComment`-is-a-real-answer note. |
#endregion
