# News, Inbox & Man-Management #46 — Section 2: Requirements, Data Structures, Failure Modes

**Created:** July 27, 2026
**Last Updated:** July 27, 2026 (v0.2 — PASS-1 fix pass)
**Version:** 0.2
**Status:** APPROVED

---

## 2.1 Functional requirements

**Ownership & cadence**

| FR | Requirement | Level | KD |
|---|---|---|---|
| FR-NW-001 | #46 MUST take **no position** in `RunWorldTickInFixedOrder` and MUST cite no step number of its own. | MUST | KD-7 |
| FR-NW-002 | #46 MUST be the **sole writer** of its own state (the item log, read state, pending deltas, cursors). | MUST | KD-6 |
| FR-NW-003 | All #46 state MUST be **integer**. **No `string` MUST be stored**, so state is locale-independent (FR-LC-006). | MUST | KD-1 |
| FR-NW-004 | #46's assembly MUST reference **nothing**, at every tier — no producer, not #33, not #30, not `TacticalDirector.Localization`, not `SeasonSave`, not `MatchEngine`. | MUST | KD-2 |
| FR-NW-005 | #46 MUST write no #33 state and MUST expose no morale mutator. | MUST | KD-3 |
| FR-NW-006 | #46 MUST NOT **read** morale, at any tier, through any surface — FR-HS-025 bars two-way coupling with a consumer, and #46 is the one consumer that also causes a write. | MUST | KD-3 |
| FR-NW-007 | #46 MUST NOT emit a baked, human-readable localized string (FR-LC-002), through any surface. | MUST | KD-4 |

**The item log**

| FR | Requirement | Level | KD |
|---|---|---|---|
| FR-NW-008 | An `InboxItem` MUST be a compact **integer** record. Its `Payload` MUST be a fixed-length integer array whose meaning is fixed **per `(SourceTag, ItemKind)`**. | MUST | KD-1 |
| FR-NW-009 | `SourceTag` MUST be #46's own namespace and MUST NOT be conflated with, aliased to, or named after #49's `TextTemplateId.ProducerTag` — they share neither membership nor meaning. | MUST | KD-1 |
| FR-NW-010 | `ItemKind` MUST carry an **ORDINAL STABILITY — APPEND-only** contract: reordering re-reads every saved `Payload` **under the wrong schema**, and no version gate catches it. | MUST | KD-4 |
| FR-NW-011 | Each `(SourceTag, ItemKind)` **payload schema** MUST itself be APPEND-only; changing the meaning of an existing slot MUST be treated as an `INBOX_SAVE_FORMAT_VERSION` bump. | MUST | KD-1 |
| FR-NW-012 | `Append` MUST be the **only** write-in, and **#46 MUST assign the `ItemId`** from its own per-`SourceTag` cursor and return it. The caller MUST NOT supply one. | MUST | KD-2 |
| FR-NW-013 | Item order MUST be the **total order** on `(WorldDay, SourceTag, ItemId)` — tie-free by construction, since `ItemId` is unique within a `SourceTag`. | MUST | KD-8 |
| FR-NW-014 | The log MUST be bounded by **both** `INBOX_RETENTION_DAYS` and `INBOX_MAX_ITEMS`. A full log MUST **drop the oldest item** on a recorded, testable branch — an inbox that refuses new news because it is full is worse than one that forgets old news. | MUST | KD-1 |
| FR-NW-015 | Retention MUST be evaluated **lazily at query time**, and a query MUST NOT mutate persisted state (FR-NW-020). | MUST | KD-6/KD-7 |
| FR-NW-016 | An `InboxItem` about a departed or retired player MUST be **retained** — it is a historical record, not a pending effect (contrast FR-NW-028). | MUST | KD-1 |

**Read state**

| FR | Requirement | Level | KD |
|---|---|---|---|
| FR-NW-017 | Read state MUST be a **watermark** (`ReadBeforeWorldDay`) plus a bounded **exception set** — never a per-item flag. | MUST | KD-6 |
| FR-NW-018 | The exception set MUST be bounded by the **log**, not merely by the watermark: a key whose item is no longer present is **dead**. | MUST | KD-6 |
| FR-NW-019 | A dead key MUST be **ignored on read** and **compacted at the next `Append`**. | MUST | KD-6 |
| FR-NW-020 | **A query MUST write nothing.** A save taken immediately after any number of queries MUST be **byte-identical** to one taken before them. This is what makes the KD-7 no-tick-slot argument sound, so it is a requirement rather than an implementation note. | MUST | KD-6/KD-7 |

**Man-management (deep tier)**

| FR | Requirement | Level | KD |
|---|---|---|---|
| FR-NW-021 | Man-management MUST be **command-driven** (`TryTalkToPlayer`), never a tick step. | MUST | KD-7 |
| FR-NW-022 | A man-management outcome MUST be a deterministic function of **the chosen option alone** — never of the target's current morale (FR-NW-006). | MUST | KD-3 |
| FR-NW-023 | `TryTalkToPlayer` MUST return **`false`** for a legal unavailable state (player departed, already talked this window) and MUST **fail loud** on genuinely malformed input (unknown intent, out-of-range option). | MUST | KD-3 |
| FR-NW-024 | A consequence MUST be recorded as a `PendingDelta` bounded to `[INBOX_DELTA_MIN, INBOX_DELTA_MAX]`, and a **zero delta MUST NOT be recorded**. | MUST | KD-3 |
| FR-NW-025 | Delivery MUST occur through `TryTakePendingDelta` at **#30's tick step 3**, and the entry MUST be **cleared on delivery** — applied **exactly once**. | MUST | KD-3 |
| FR-NW-026 | The routed field MUST be the producer-agnostic `ExternalDeltaPermille`, **summed across producers and clamped by the root** before it reaches #33. #46 MUST NOT declare a `InboxDeltaPermille` or any second field. | MUST | KD-3 |
| FR-NW-027 | #46 MUST own its **own** pending deltas; a shared cross-producer pending store MUST NOT be introduced. | MUST | KD-3 |
| FR-NW-028 | An undelivered `PendingDelta` whose target **leaves the managed roster** MUST be **dropped** in lockstep with #33's FR-HS-027 — never migrated across #31's `PlayerId` re-key (contrast FR-NW-016). | MUST | KD-3 |

**The #49 seam**

| FR | Requirement | Level | KD |
|---|---|---|---|
| FR-NW-029 | `InboxIntent` MUST be the **catalogue** identity and MUST carry an **ORDINAL STABILITY — APPEND-only** contract: reordering re-points every item at the **wrong template**. | MUST | KD-4 |
| FR-NW-030 | An `InboxItem` MUST **NOT** store an `InboxIntent` or any localization ordinal. The adapter maps `(SourceTag, ItemKind) → InboxIntent`. | MUST | KD-4 |
| FR-NW-031 | The rendering binding MUST be a **sibling boundary adapter** (`InboxTextBoundary`, named in advance by #49 §7.3), never a change to #49's core seam. | MUST | KD-4 |
| FR-NW-032 | #46 MUST apply the FR-LC-015 **intent-value pre-gate** and MUST carry an FR-LC-008a **coverage assertion** over its full `InboxIntent` roster — item text **and** man-management prompts/options alike. | MUST | KD-4 |
| FR-NW-033 | #46 MUST supply its `SelectionDraw` as a **local keyed mix**, inheriting #35's `ERR-049-001` dependency; if that fix is refused, #46 MUST take the same `SelectionDraw = 0` fallback. #46 MUST NOT file its own #49 back-prop. | MUST | KD-4 |

**Determinism & persistence**

| FR | Requirement | Level | KD |
|---|---|---|---|
| FR-NW-034 | #46 MUST be **draw-free at every tier** — no `RegisterStream`, no domain tag, no `SubsystemOrdinal`. #16 is untouched, and **#46 has no reserved value to promote**: a future stochastic news generator needs a **fresh allocation**. | MUST | KD-8 |
| FR-NW-035 | `INBOX_SAVE_FORMAT_VERSION` [FIXED] = 1; #46's state MUST land as an **opaque, independently version-gated** sub-blob composed into #30's `SeasonSaveCodec` — **not** a `WORLD_STORE_FORMAT_VERSION` bump. Every field MUST round-trip **field-identical**. | MUST | KD-6 |
| FR-NW-036 | Restore MUST **fail loud** on version mismatch, an out-of-bounds length prefix (overflow-safe bound against `total − offset`), trailing bytes, an undefined `SourceTag` / `ItemKind`, a `Payload` of the wrong length for its `(SourceTag, ItemKind)`, or an out-of-range / **zero** `DeltaPermille`. The layout MUST be **APPEND-only**. | MUST | KD-6 |

## 2.2 Data structures

```csharp
// #46's OWN event-source namespace. NOT #49's TextTemplateId.ProducerTag -- they share neither
// membership nor meaning (FR-NW-009). APPEND-only: the ordinal keys InboxCursors' per-source array.
public enum SourceTag : byte { Season = 0, Media = 1, Discipline = 2, Board = 3, Transfers = 4 }

// The SERIALIZED identity. With SourceTag it fixes the Payload schema (FR-NW-008/010).
// APPEND-only, NEVER reordered: a reorder re-reads every saved Payload under the WRONG schema,
// and there is no version gate that would catch it.
public enum ItemKind : byte { /* per-source kinds; see Appendix C for the pinned schemas */ }

// The CATALOGUE identity -- mapped to by the adapter, NEVER stored (FR-NW-030).
// Also APPEND-only, for the OTHER reason: a reorder re-points every item at the wrong template.
public enum InboxIntent : int { None = 0, /* item text + man-management prompts/options */ }

public struct InboxItem
{
    public SourceTag SourceTag;
    public ItemKind  ItemKind;
    public int       ItemId;        // assigned BY #46 at Append (FR-NW-012), never by the caller
    public uint      WorldDay;
    public int       SubjectId;     // INBOX_NO_SUBJECT (-1); NOT 0, which is a valid PlayerId
    public int[]     Payload;       // length fixed per (SourceTag, ItemKind) -- Appendix C
}

// Watermark + bounded exception set (FR-NW-017/018). A per-item flag would grow a byte per item
// forever and make "mark all read" an O(n) rewrite of the whole blob.
public struct ReadState
{
    public uint ReadBeforeWorldDay;
    public (SourceTag, int)[] ExplicitReadKeys;   // dead keys ignored on read, compacted on Append
}

// #46's OWN undelivered man-management consequences (FR-NW-027). A ZERO delta is never stored.
public struct PendingDelta
{
    public int  TargetPlayerId;
    public int  DeltaPermille;      // [INBOX_DELTA_MIN, INBOX_DELTA_MAX], never 0
    public uint RecordedWorldDay;
}

// Per-source id allocators. Length-prefixed and APPEND-only: a new SourceTag EXTENDS the array,
// never reorders it -- so an existing source's ids are never re-based.
public struct InboxCursors { public int[] NextItemIdBySource; }

// #46's native slots for the adapter. DISJOINT from #35's (FR-LC-014). Native values, never strings.
public readonly struct InboxSlots
{
    public readonly int SubjectId;                 // INBOX_NO_SUBJECT when absent
    public readonly int P0, P1, P2, P3, P4;        // the payload, positionally
}
```

**Types #46 consumes but does not declare:**

| Type | Owner | #46's use |
|---|---|---|
| `TextTemplateId`, `LocalizedTextRequest`, `ILocalizer` | #49 | used **only inside `InboxTextBoundary`**, which is not a #46 assembly |
| `HumanSystemsDayInput`, `ExternalDeltaPermille` | #33 (via ERR-033-003) | #30 assembles it; #46 hands over a bare `int` |
| `MatchResult`, `Fixture` | #30 | **not consumed** — the root projector reads them and hands #46 a `Payload` |
| `MediaIntent`, `PressConference` | #35 | **not consumed** — #46's projector reads #35's query; #46 defines no press intent (KD-5) |

## 2.3 Failure modes

| ID | Condition | Response |
|---|---|---|
| **F1** | An undefined `SourceTag` or `ItemKind` at `Append` or on decode. | **Fail loud** — the `Payload` schema is keyed on the pair, so an undefined member means the schema is unknown. |
| **F2** | A `Payload` whose length does not match the pinned schema for its `(SourceTag, ItemKind)`. | **Fail loud** at both `Append` and decode. This is what makes FR-NW-011's APPEND-only payload rule **enforceable** rather than aspirational. |
| **F3** | An out-of-range or **zero** `DeltaPermille`. | **Fail loud**. Zero is structurally impossible to produce (FR-NW-024), so encountering one — especially on decode — means corruption, and accepting it would break the presence invariant the drain relies on. |
| **F4** | `TryTalkToPlayer` with an unknown intent, or an `optionIndex` outside that intent's own roster. | **Fail loud** — malformed input, distinct from F5. |
| **F5** | `TryTalkToPlayer` for a legal unavailable state — the player has departed, or has already been talked to this window. | **Return `false`.** A named legal state, the #45 `TryProjectBoardModifier` / #35 `TryAnswerQuestion` posture. |
| **F6** | The log is **full** at `Append` (`INBOX_MAX_ITEMS`). | **Drop the oldest item** on a recorded branch — **not** a throw, and **not** a refusal of the new item (FR-NW-014). |
| **F7** | Bad `INBOX_SAVE_FORMAT_VERSION`, an out-of-bounds length prefix, or trailing bytes on restore. | **Fail loud** — version gate read **first**; the bound compared against `total − offset`, never `offset + need`, which can wrap negative on a crafted near-`int.MaxValue` prefix. |
| **F8** | An undelivered `PendingDelta` whose `TargetPlayerId` has left the managed roster. | **Dropped** at the #33 FR-HS-027 boundary (FR-NW-028) — never migrated, never delivered to whoever now holds a re-keyed id. Not an error. |
| **F9** | An `ExplicitReadKey` referencing an item no longer in the log. | **Ignored on read, compacted at the next `Append`** (FR-NW-019). **Not** an error and **not** a query-time write — see below. |

**Deliberately not a failure mode, and deliberately not a query-time mutation: a dead read key.** F9 is the
one place a reader might expect either a throw or a cleanup, and it must be neither. A throw would crash
on an ordinary click after a retention eviction; a query-time cleanup would make **reading the inbox
change persisted state**, collapsing the KD-7 argument that lets #46 skip a tick slot entirely. The
compaction happens at the next `Append` — a write that is already occurring, at a point fixed by the
producer's step rather than by when a human opened a screen.

**Also not a failure mode: an empty `TryTakePendingDelta`.** It returns `false` and the root contributes
`0`. #30 asks for **every** player at step 3, so absence is the overwhelmingly common case.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-27 | — | Initial §2 (FR-NW-001..036, data structures, F1..F9) from supplement v0.6. Status IN REVIEW. |
| 0.2 | 2026-07-27 | — | PASS-1 fixes. **M:** added **FR-NW-011 / F2** — the per-`(SourceTag, ItemKind)` payload schema was an *unversioned convention inside a versioned blob* (the supplement's own R-2), with nothing making it checkable; a length check at `Append` **and** decode is what turns it from a convention into an enforceable rule. **M:** added **FR-NW-020** promoting *"a query writes nothing"* from an implementation note to a **requirement**, since the entire KD-7 no-tick-slot argument rests on it. **M:** added **FR-NW-016** (items about departed players are **retained**) alongside FR-NW-028 (deltas are **dropped**), with an explicit cross-reference — the supplement stated the asymmetry in prose, and it is exactly the kind of pair a later "consistency" pass unifies in the wrong direction. **L:** `SourceTag`, `InboxCursors` and `InboxSlots` written out; F9's *"neither a throw nor a cleanup"* stated as its own note, since both wrong answers are natural. |
#endregion
