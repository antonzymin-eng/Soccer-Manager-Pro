# Media & Press Interactions #35 — Section 2: Requirements, Data Structures, Failure Modes

**Created:** July 27, 2026
**Last Updated:** July 27, 2026 (v0.2 — PASS-1 fix pass)
**Version:** 0.2
**Status:** APPROVED

---

## 2.1 Functional requirements

**Cadence & ownership**

| FR | Requirement | Level | KD |
|---|---|---|---|
| FR-ME-001 | All #35 state MUST advance on the **world tick** or the **#30 post-round path** — never the 10 Hz tactical or 60 Hz physics loops. No #35 type MUST be reachable from `MatchEngine.RunTick`. | MUST | KD-5 |
| FR-ME-002 | #35 MUST be the **sole writer** of its own state (the conference queue, the answered records, the pending deltas, its cursors). No other assembly writes them. | MUST | KD-7 |
| FR-ME-003 | All #35 state and formulas MUST be **integer**. No float MUST appear at any tier, and **no `string` MUST be stored** (FR-LC-006 — a save must not depend on the locale it was written in). | MUST | KD-7 |
| FR-ME-004 | #35 MUST NOT write `MoraleState`, `PersonalityProfile`, or any #33 state, at any tier. #33 is its sole writer (FR-HS-002/024). | MUST | KD-3 |
| FR-ME-005 | #35 MUST NOT emit a baked, human-readable localized string (FR-LC-002), at any tier and through any surface. | MUST | KD-1 |
| FR-ME-006 | #35's assembly MUST reference **nothing** at the minimal tier, and **only** `TacticalDirector.DeterministicSim` at the deep tier. It MUST NOT reference #30, #33, #45, #46, `TacticalDirector.Localization`, `living-world`, `SeasonSave`, or `MatchEngine`, at any tier. | MUST | KD-1/KD-6 |
| FR-ME-007 | #35 MUST NOT declare any reputation scalar or expose one, at any tier (KD-4). | MUST | KD-4 |

**The `MediaIntent` roster and the #49 seam**

| FR | Requirement | Level | KD |
|---|---|---|---|
| FR-ME-008 | `MediaIntent` MUST be #35's **single** text-identity type. #35 MUST NOT declare a parallel template-id type; the `(ProducerTag, LocalOrdinal)` pair is the boundary adapter's construction. | MUST | KD-1 |
| FR-ME-009 | `MediaIntent` MUST carry an **ORDINAL STABILITY** contract: **APPEND-only, never reordered**, with retired values keeping their ordinal and their base-locale row. | MUST | KD-10 |
| FR-ME-010 | The roster MUST cover **both** question archetypes and answer-option phrasings, separated by a **`[FIXED]` ordinal band boundary** (`MEDIA_INTENT_OPTION_BAND_START`), asserted by test — never an informal convention. | MUST | KD-1/KD-10 |
| FR-ME-011 | #35 MUST apply a **pre-render roster gate on the intent VALUE** (FR-LC-015): `None` and any undefined ordinal MUST be refused **before** any selection work is done. | MUST | KD-1 |
| FR-ME-012 | #35's spec MUST carry a **catalogue-coverage assertion** extending FR-LC-008a to its full roster — every defined `MediaIntent`, **questions and options alike**, has a base-locale template row. | MUST | KD-1 |
| FR-ME-013 | The rendering binding MUST be a **sibling boundary adapter** (`MediaTextBoundary`, named in advance by #49 §7.3), never a change to #49's core seam (FR-LC-013/014). | MUST | KD-1 |
| FR-ME-014 | #35 MUST emit only its **own native slot values**, disjoint from #22's (FR-LC-014). It MUST NOT consume `InteractionTextGenerator`, `InteractionSlots`, or `world.text`. | MUST | KD-1 |
| FR-ME-015 | `HasCitedEpisode` MUST be `false` at every tier described in this spec. A citation clause would require #22's `MemoryStore`, re-introducing the dependency KD-1 removes; enabling it is a deep-tier decision that MUST re-argue that dependency. | MUST | KD-1 |

**The selection value**

| FR | Requirement | Level | KD |
|---|---|---|---|
| FR-ME-016 | The minimal tier MUST be **draw-free**: no `RegisterStream`, no domain-tag promotion. `_RESERVED_0x27_` / `SubsystemOrdinals 89` MUST stay **RESERVED**. | MUST | KD-2 |
| FR-ME-017 | The `ulong` supplied to `LocalizedTextRequest.SelectionDraw` MUST be a **local keyed SplitMix64 mix** over `(intentOrdinal, worldDay, subjectId, purpose)` — **position-independent**, so **nothing is serialized** and replay is cursor-free. | MUST | KD-2 |
| FR-ME-018 | FR-ME-017 is **conditional on ERR-049-001**. If #49 declines it, #35 MUST supply `SelectionDraw = 0` (FR-LC-007 is total at `draw = 0`), losing phrasing variety at the minimal tier and regaining it at the deep tier's real draw. #35 MUST NOT register a stream to satisfy FR-LC-020 literally. | MUST | KD-2 |
| FR-ME-019 | Any deep-tier draw MUST use **one** subsystem-wide stream (`media.selection`, a fixed entity sentinel) with a **position-independent keyed action ordinal** over `(subjectId, worldDay, purpose)` at a **fixed** radix. **#35 MUST contribute exactly one `RegisterStream` call at any tier** — never one per club or per player (the `MaxRngStreams = 64` bound, #42 §7.4 R-1). | MUST | KD-2 |
| FR-ME-020 | No RNG cursor MUST be serialized, at any tier. | MUST | KD-2/KD-7 |

**The lifecycle**

| FR | Requirement | Level | KD |
|---|---|---|---|
| FR-ME-021 | Queueing MUST occur at #30's post-round seam, after `EmitMatchOutcome(result)`, and MUST enqueue **at most one** conference per fixture. | MUST | KD-5 |
| FR-ME-022 | A fixture not involving the **managed club** MUST queue nothing — there is no manager to ask. | MUST | KD-5 |
| FR-ME-023 | The queue MUST be bounded by `MEDIA_MAX_PENDING_CONFERENCES`. A full queue MUST **drop the new conference** on a recorded, testable branch — **not** throw. A press conference is not correctness-critical, and fail-loud here would let a client that never opens the inbox crash a career. | MUST | KD-5 |
| FR-ME-024 | Answering MUST be **command-driven** (`TryAnswerQuestion`), never a tick step. A conference the player never opens MUST NOT auto-answer itself. | MUST | KD-5 |
| FR-ME-025 | `TryAnswerQuestion` MUST return **`false`** for an already-resolved conference — a **legal race** between the client's render and the tick's expiry sweep, not malformed input. | MUST | KD-5 |
| FR-ME-026 | `TryAnswerQuestion` MUST **fail loud** on genuinely malformed input: an unknown `conferenceId`, or an `optionIndex` outside **that conference's own** option roster (F2). | MUST | KD-5 |
| FR-ME-027 | The daily tick seam MUST do **expiry only**. An unanswered conference past `DeadlineWorldDay` MUST resolve to its designated **no-comment** option — a defined answer with its own consequence. | MUST | KD-5 |
| FR-ME-028 | Expiry MUST be **eager** (a per-day sweep), never lazy-on-read: expiry produces a consequence, so a lazy scheme would make state depend on whether and when the client read, which is not replayable. | MUST | KD-5 |
| FR-ME-029 | Re-advancing the same `worldDay` MUST be a **no-op**; a `worldDay` **gap** MUST **fail loud** — implemented from `LastAdvancedWorldDay`, whose unadvanced sentinel MUST be `uint.MaxValue`, **not** `0` (#33 FR-HS-008: day `0` is a legal world day). | MUST | KD-5 |

**The consequence**

| FR | Requirement | Level | KD |
|---|---|---|---|
| FR-ME-030 | A consequence MUST be a **list of `(targetKind, targetId, deltaPermille)`** at every tier — one code path, with the deep tier adding **entries**, never branches. | MUST | KD-8 |
| FR-ME-031 | The minimal tier MUST ship **zero or one** entry: one with `targetKind = Player` when the conference has a subject, and **zero** when `SubjectPlayerId == MEDIA_NO_SUBJECT`. A fallback entry against `PlayerId 0` MUST NOT be substituted — `0` is a **real player**. | MUST | KD-8 |
| FR-ME-032 | `deltaPermille` MUST be bounded to `[MEDIA_DELTA_MIN, MEDIA_DELTA_MAX]` (`[-1000, +1000]`) and the entry count by `MEDIA_MAX_CONSEQUENCE_TARGETS`, both enforced at the **authoring boundary**. | MUST | KD-8 |
| FR-ME-033 | A **zero delta MUST NOT be recorded**. An answer whose consequence is `0` — including the no-comment option most expiries resolve to — writes **no** `PendingDelta` row (the #44 canonical `(0,0)`-drop rule). | MUST | KD-7 |
| FR-ME-034 | Delivery MUST occur through `TryTakePendingDelta` at **#30's tick step 3**, where the per-player `HumanSystemsDayInput` is assembled, and the entry MUST be **cleared on delivery** — so a delta is applied **exactly once**. | MUST | KD-3 |
| FR-ME-035 | An undelivered delta whose target **leaves the managed roster** MUST be **dropped** at the same boundary #33 drops that player's entries — **never migrated**, and never left in the blob (FR-HS-027 lockstep). | MUST | KD-7 |

**#46 discovery and persistence**

| FR | Requirement | Level | KD |
|---|---|---|---|
| FR-ME-036 | #35 MUST expose a **read-only** query over its conference records, returning **value copies**. It MUST fire no inbox event, hold no unread flag, and never reference #46 (`#46 → #35`, one-directional). | MUST | KD-6 |
| FR-ME-037 | `MEDIA_SAVE_FORMAT_VERSION` [FIXED] = 1; #35's state MUST land as an **opaque, independently version-gated** sub-blob composed into #30's `SeasonSaveCodec` — **not** a `WORLD_STORE_FORMAT_VERSION` bump. Every field MUST round-trip **field-identical**; **serialize, don't regenerate**. | MUST | KD-7 |
| FR-ME-038 | Restore MUST **fail loud** on version mismatch, an out-of-bounds length prefix (overflow-safe bound compared against `total − offset`), trailing bytes, an undefined `MediaIntent` ordinal, an option intent outside the option band, or an out-of-range delta. The layout MUST be **APPEND-only**. | MUST | KD-7 |

## 2.2 Data structures

```csharp
// #35's SINGLE text-identity type (KD-1). APPEND-only, NEVER reordered (KD-10 / FR-ME-009):
// the ordinal is serialized inside PressConference AND is the LocalOrdinal half of the #49
// TextTemplateId -- reordering re-points every saved conference at a different template and
// invalidates every catalogue row, with NO version gate to catch it.
public enum MediaIntent : int
{
    None = 0,                                   // FR-ME-011 gate value; never rendered

    // --- Question band: [1, MEDIA_INTENT_OPTION_BAND_START) ---
    QPostWin = 1, QPostDraw, QPostLoss, QSubjectForm, QBoardObjective, /* append here */

    // --- Option band: [MEDIA_INTENT_OPTION_BAND_START, ...) --- boundary is [FIXED] + asserted
    OSupportive = MEDIA_INTENT_OPTION_BAND_START, OCritical, ODeflect, ONoComment, /* append here */
}

// One queued or resolved conference (serialized, KD-7).
public struct PressConference
{
    public int   ConferenceId;          // monotonic, from MediaCursors.NextConferenceId
    public MediaIntent QuestionIntent;  // question band (FR-ME-010)
    public MediaIntent[] OptionIntents; // ordered; index == optionIndex; option band; <= MEDIA_MAX_OPTIONS
    public int   SubjectPlayerId;       // MEDIA_NO_SUBJECT (-1) for a board/result question.
                                        // NOT 0: 0 is a valid PlayerId (FR-ME-031).
    public int   TriggerRoundIndex;
    public uint  QueuedWorldDay;
    public uint  DeadlineWorldDay;
    public int   AnsweredOptionIndex;   // MEDIA_UNANSWERED (-1) while pending
}

// A recorded, not-yet-delivered consequence (serialized, KD-7). A ZERO delta is NEVER stored
// (FR-ME-033) -- so "is a delta pending for this player?" stays answerable by presence.
public struct PendingDelta
{
    public byte TargetKind;             // Player at minimal; Squad/Board are deep-tier VALUES
    public int  TargetId;
    public int  DeltaPermille;          // [MEDIA_DELTA_MIN, MEDIA_DELTA_MAX], never 0
    public uint RecordedWorldDay;       // makes "was this applied?" a serialized fact, not an inference
}

// Subsystem-scoped cursors (serialized). NOT optional bookkeeping: LastAdvancedWorldDay is the
// state the FR-ME-029 same-day-no-op / day-gap-fail-loud guard is implemented FROM.
public struct MediaCursors
{
    public int  NextConferenceId;
    public uint LastAdvancedWorldDay;   // sentinel MEDIA_NOT_ADVANCED_SENTINEL = uint.MaxValue, NOT 0
}

// The committed-values input #30 routes IN (the HumanSystemsDayInput / BoardDayInput posture):
// integers only, derived by #30 from its own MatchResult -- #35 references no #30 type.
public readonly struct MediaTriggerInput
{
    public readonly int HomeClubId, AwayClubId, HomeScore, AwayScore, RoundIndex, ManagedClubId;
}

// #35's native slots, handed to the boundary adapter. DISJOINT from #22's (FR-LC-014).
// Native values, never formatted strings (FR-LC-002 / FR-ME-005).
public readonly struct MediaSlots
{
    public readonly int SubjectPlayerId;    // MEDIA_NO_SUBJECT when absent
    public readonly int HomeScore, AwayScore, RoundIndex;
    // HasCitedEpisode is absent by decision (FR-ME-015) -- not merely false.
}
```

**Types #35 consumes but does not declare:**

| Type | Owner | #35's use |
|---|---|---|
| `TextTemplateId`, `LocalizedTextRequest`, `ILocalizer` | #49 | used **only inside `MediaTextBoundary`**, which is not a #35 assembly |
| `HumanSystemsDayInput` | #33 | #30 assembles it; #35 never names it (it hands over a bare `int`) |
| `MatchResult` | #30 | **not consumed** — #30 derives `MediaTriggerInput` from it (FR-ME-006) |

## 2.3 Failure modes

| ID | Condition | Response |
|---|---|---|
| **F1** | `MediaIntent.None` or an undefined ordinal reaching the render path. | **Fail loud** at the **pre-selection** gate (FR-ME-011) — before any selection work, so a refused item consumes nothing. |
| **F2** | `TryAnswerQuestion` with an unknown `conferenceId`, or an `optionIndex` outside that conference's own roster. | **Fail loud** — genuinely malformed input, distinct from F3. |
| **F3** | `TryAnswerQuestion` on an **already-resolved** conference. | **Return `false`.** A **legal race** between the client's render and the tick's expiry sweep; throwing would crash a career on an ordinary click (KD-5). |
| **F4** | An out-of-range `deltaPermille`, or a consequence list longer than `MEDIA_MAX_CONSEQUENCE_TARGETS`, or an option list longer than `MEDIA_MAX_OPTIONS`. | **Fail loud** at the **authoring boundary** — a catalogue-authoring bug, caught before it can reach a save. |
| **F5** | A question intent in the option band, or an option intent in the question band. | **Fail loud** at the authoring boundary and at decode — the `[FIXED]` band boundary is what makes this checkable (FR-ME-010). |
| **F6** | Re-advancing the same `worldDay`; **or** a `worldDay` gap past `LastAdvancedWorldDay` (when not the sentinel). | **No-op** / **fail loud**, respectively (the #33 F6 guard, verbatim). |
| **F7** | The pending-conference queue is **full** at enqueue. | **Drop the new conference** on a recorded branch — **not** a throw (FR-ME-023). The one deliberate non-fail-loud refusal in #35, and it is deliberate because the alternative lets an unopened inbox kill a career. |
| **F8** | Bad `MEDIA_SAVE_FORMAT_VERSION`, an out-of-bounds length prefix, trailing bytes, or an out-of-contract value on restore. | **Fail loud** — version gate read **first**; the bound compared against `total − offset`, never `offset + need`, which can wrap negative on a crafted near-`int.MaxValue` prefix. |
| **F9** | An undelivered `PendingDelta` whose `TargetId` is no longer on the managed roster. | **Dropped** at the #33 FR-HS-027 boundary — never migrated, never delivered to whoever now holds a re-keyed id (KD-7). Not an error: a press reaction to a departed player has no subject left. |

**Deliberately not a failure mode: a `TryTakePendingDelta` for a player with nothing pending.** It returns
`false` and the caller supplies `0` — the #45 `TryProjectBoardModifier` posture, *"not modelled" is a named
legal state*. #30 iterates **every** player at step 3, so absence is the overwhelmingly common case and
must not be exceptional.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-27 | — | Initial §2 (FR-ME-001..038, data structures, F1..F9) from supplement v0.7. Status IN REVIEW. |
| 0.2 | 2026-07-27 | — | PASS-1 fixes. **M:** added **FR-ME-010 / F5** — the question/option ordinal band had a `[FIXED]` boundary constant but **no stated failure mode**, so a catalogue row with a question intent in the option band (or vice versa) was undetectable; both the authoring boundary and decode now check it. **M:** added **FR-ME-015** making `HasCitedEpisode` a requirement rather than a KD-1 aside, and recorded in `MediaSlots` that the field is **absent by decision** rather than present-and-false — a present-but-false field is what a later maintainer flips. **L:** `MediaTriggerInput`, `MediaSlots` and `MediaCursors` written out in full (v0.1 described them in prose); added the *"not a failure mode"* note for an empty `TryTakePendingDelta`, since #30 iterates every player and absence is the common case; F7's non-fail-loud refusal flagged as #35's one deliberate exception, with its reason. |
#endregion
