# News, Inbox & Man-Management #46 — Section 5: Test Plan

**Created:** July 27, 2026
**Last Updated:** July 27, 2026 (v0.1 — initial section-file set)
**Version:** 0.1
**Status:** IN REVIEW

---

Test-ID prefixes follow #19 §3.1.4: `T-NW-U-*` unit, `T-NW-I-*` integration, `T-NW-DET-*` determinism,
`T-NW-ID-*` identity / behaviour-neutrality, `T-NW-LOC-*` localization compliance, `T-NW-FAIL-*`
fail-loud, `T-NW-BOUND-*` structural.

Every value asserted below is **hand-derivable from §3.7** or is a relational property.

## 5.1 Identity / behaviour-neutrality (KD-9)

Note the **two distinct scopes**; conflating them would overstate what the minimal tier guarantees.

| ID | Test |
|---|---|
| T-NW-ID-001 | **No projector wired:** the feed is empty, the sub-blob is a version header plus **four** zero counts (items, read marks, pending deltas, cursors), every registered RNG cursor is byte-identical, and a season is **byte-identical** to pre-#46 at the #33 and #30 seams. |
| T-NW-ID-002 | **Projectors wired, man-management off — the shipped minimal tier:** #46 stores items and `ExternalDeltaPermille` is `0` at every step 3, so **#33's outputs are field-identical** to a pre-#46 run. The save frame differs (it carries the sub-blob); the simulation does not. This is the scope that actually ships, and asserting only T-NW-ID-001 would claim a guarantee the minimal tier does not offer. |
| T-NW-ID-003 | **(T0/T1 only.)** The season save is byte-identical to the pre-#46 save. Scoped deliberately — at T2 the frame gains the sub-blob. |
| T-NW-ID-004 | No RNG stream is registered **at either tier** (FR-NW-034): a full season of appends, queries, reads and man-management leaves every stream cursor byte-identical. Unlike #35, this holds at the **deep** tier too. |

## 5.2 Unit — the log (§3.1 / §3.2)

| ID | Test |
|---|---|
| T-NW-U-001 | §3.7(a): a match item stores its full payload and `Append` returns the allocated id. |
| T-NW-U-002 | §3.7(c)/(d): ids are allocated **per `SourceTag`**, and the total order on `(WorldDay, SourceTag, ItemId)` is **tie-free** across two same-day appends from the same source and from different sources. |
| T-NW-U-003 | §3.7(e): a payload of the wrong length for its `(SourceTag, ItemKind)` **throws** (F2) — the lock that makes the payload schema enforceable rather than a convention. |
| T-NW-U-004 | §3.7(f): a full log **drops the oldest** and stores the new item — **no throw**, and **not** a refusal of the new item (F6). |
| T-NW-U-005 | **`Append` snapshots the payload** (§3.1): mutating the caller's array after `Append` leaves the stored item **unchanged**. The `SpawnArc` / `TacticPreset.Players` / `MatchReplay` live-handle defect class, asserted directly. |
| T-NW-U-006 | §3.7(g): an item past `INBOX_RETENTION_DAYS` is **not returned by a query** and is **still present in the blob**. Both halves asserted — the second is what keeps the visible feed a pure function of `(log, worldDay)`. |
| T-NW-U-007 | **A query is a pure function of `(log, worldDay)`**: the same log queried on the same day yields the identical answer regardless of how many prior queries or interleaved appends occurred. |
| T-NW-U-008 | Query results are **value copies**: mutating a returned item leaves the store field-identical. |

## 5.3 Unit — read state (§3.3)

| ID | Test |
|---|---|
| T-NW-U-009 | §3.7(h): **`Query` writes nothing.** The serialized blob is **byte-identical** before and after any number of queries (FR-NW-020). **The KD-7 lock** — if this fails, #46 owes #30 a tick slot and the whole no-slot argument collapses. |
| T-NW-U-010 | §3.7(j): the watermark is **monotone** — `MarkAllReadBefore(200)` after `MarkAllReadBefore(400)` leaves it at `400`, so already-read items never become unread again. |
| T-NW-U-011 | Advancing the watermark **drops** every explicit key below it, so out-of-order reads accumulate only within the retention window — the bound that keeps the exception set from growing across a career (FR-NW-018). |
| T-NW-U-012 | §3.7(i): `MarkRead` on an evicted item is a **no-op**, not a throw — the same render/eviction race #35 classifies identically. |
| T-NW-U-013 | **Dead keys are ignored on read and compacted at the next `Append`** (FR-NW-019 / F9): after an eviction, a query's answer is unaffected and the blob still carries the dead key; after the next `Append`, it is gone. Both halves, in that order. |

## 5.4 Unit — man-management (§3.4, deep tier)

| ID | Test |
|---|---|
| T-NW-U-014 | §3.7(k): a valid talk returns `true` and records one `PendingDelta`. |
| T-NW-U-015 | §3.7(l): an option whose delta is `0` returns `true` and records **no row** (FR-NW-024). |
| T-NW-U-016 | §3.7(m): a repeat within the window returns **`false`** — a legal state, no throw (F5). |
| T-NW-U-017 | §3.7(n): an out-of-roster `optionIndex`, or an unknown intent, **throws** (F4). |
| T-NW-U-018 | **The outcome is a function of the chosen option alone** (FR-NW-022): with the target's morale varied across its full range, the recorded delta is **bit-identical**. Asserted directly, because *"he's unhappy so it lands harder"* is the natural feature and it is the FR-HS-025 violation. |

## 5.5 Integration — delivery, the root clamp, and save/restore

| ID | Test |
|---|---|
| T-NW-I-001 | §3.7(p): **delivery exactly once.** A delta survives a save between the talk and the next step 3, is delivered once on restore, and a second step 3 the same day finds nothing. |
| T-NW-I-002 | §3.7(o): **the root's post-sum clamp.** #35 pending `+600` and #46 pending `+700` for one player-day yields `ExternalDeltaPermille = 1000`, not `1300`. The cost of the single producer-agnostic field, locked rather than assumed. |
| T-NW-I-003 | **One field, not two:** `HumanSystemsDayInput` carries exactly one external-delta field, and #46 declares no `InboxDeltaPermille` (FR-NW-026). |
| T-NW-I-004 | §3.7(q): **the deliberate asymmetry.** A pending *delta* for a departed player is **dropped** (F8) while an *item* about them is **retained** (FR-NW-016) — asserted as one test over one scenario, so a later "consistency" pass unifying them fails here rather than in play. |
| T-NW-I-005 | **The KD-1 lock** (§3.7(b)): a match item's scoreline survives a save/restore **and is still correct after the league table has absorbed further results** — asserted against a table that no longer carries that fixture's result. This is the property the derived-view design would have failed. |
| T-NW-I-006 | State → `Encode` → `Decode` is **field-identical**: an empty store, a populated log, a watermark with a non-empty exception set, an undelivered delta, and the per-source cursors. |
| T-NW-I-007 | Round-trip through a full `SeasonSaveCodec` frame: #46's sub-blob is **opaque** to the outer codec, and the world / season / match / sibling blobs are **byte-unchanged**. |
| T-NW-I-008 | The two format versions move **independently**. |
| T-NW-I-009 | **`HUMAN_SYSTEMS_SAVE_FORMAT_VERSION` is untouched** — `HumanSystemsDayInput` is transient, so ERR-033-003 adds no serialized field. Asserted so a later reviewer does not "helpfully" persist the routed value. |
| T-NW-I-010 | **Cursor stability across a source append** (FR-NW-012 / `InboxCursors`): adding a new `SourceTag` **extends** the array and leaves every existing source's next id unchanged — the lock that makes the per-source allocator APPEND-safe. |

## 5.6 Localization compliance (#49)

| ID | Test |
|---|---|
| T-NW-LOC-001 | **Coverage over the full `InboxIntent` roster** (FR-NW-032 / FR-LC-008a): every defined intent — item text **and** man-management prompts **and** options — has a base-locale template row. |
| T-NW-LOC-002 | **`ItemKind` ordinal stability** (FR-NW-010). A **save-correctness** lock: a reorder re-reads every stored `Payload` under the wrong schema, with no version gate to catch it. |
| T-NW-LOC-003 | **`InboxIntent` ordinal stability** (FR-NW-029). A **different** save-correctness lock: a reorder re-points every item at the wrong template. Both are asserted because the two enums fail in two different ways, and a suite that checks one has proven nothing about the other. |
| T-NW-LOC-004 | **No localization ordinal is stored** (FR-NW-030): the decoded `InboxItem` carries no `InboxIntent`, and the blob layout has no slot for one. |
| T-NW-LOC-005 | **#46 emits no display string** (FR-NW-007): a source-level assertion over `src/inbox/` finds no `string` field, no `string` return, and no string formatting. |
| T-NW-LOC-006 | **Locale-independence of state** (FR-LC-006): the same career advanced under two display locales produces **byte-identical** serialized #46 state. |
| T-NW-LOC-007 | The FR-LC-015 value gate refuses `InboxIntent.None` and undefined ordinals **before** any selection work, and holds through **any** #46 surface — not only through `InboxTextBoundary`, which any other consumer would bypass. |

## 5.7 Determinism

| ID | Test |
|---|---|
| T-NW-DET-001 | Two runs over the same projection and command sequence produce **field-identical** state. |
| T-NW-DET-002 | `save@N → restore → advance to N+K` is **field-identical** to the uninterrupted run. |
| T-NW-DET-003 | **Total-order stability across producers**: items appended from five sources on the same world day enumerate in the same order in every run and after a restore — the tie-freedom `ItemId` uniqueness provides (FR-NW-013). |
| T-NW-DET-004 | **Eviction is a pure function of the stored log**: the same log and world day yield the same visible feed and the same eviction set, independent of query history. |

## 5.8 Structural (the boundaries #46 must not cross)

| ID | Test |
|---|---|
| T-NW-BOUND-001 | **#46's assembly references nothing, at every tier** — asserted from the reference set, so a future `using` of any producer, #33, #30, `Localization`, `SeasonSave` or `MatchEngine` fails the build's test gate (FR-NW-004). Unconditional, because #46 is a leaf at the deep tier too. |
| T-NW-BOUND-002 | **#46 exposes no morale read and no morale write** (FR-NW-005/006) — asserted over the public surface, because a read accessor is the natural convenience and would compile. |
| T-NW-BOUND-003 | **#46 declares no type named `ProducerTag`, `MediaIntent`, `InteractionSlots`, `MediaSlots`, or `TextTemplateId`** — the parallel-surface lock, including the deliberate `SourceTag` non-collision (FR-NW-009). |
| T-NW-BOUND-004 | **Projectors do not mutate their producers** (§4.7): a `MatchResult`, a `PressConference`, and each world-tick producer's state are **field-unchanged** after every projection. This is the one place a foreign write could hide — #46 references nothing, but a root-side projector holds both sides — so it is asserted behaviourally rather than inferred from #46's own graph. |
| T-NW-BOUND-005 | **No `RegisterStream` call exists at any tier** (FR-NW-034), asserted over the compiled surface. |

## 5.9 Fail-loud (§2.3)

| ID | Test |
|---|---|
| T-NW-FAIL-001 | An undefined `SourceTag` or `ItemKind` at `Append` or decode ⇒ throws (F1). |
| T-NW-FAIL-002 | A `Payload` of the wrong arity ⇒ throws at **both** `Append` and decode (F2). |
| T-NW-FAIL-003 | An out-of-range **or zero** `DeltaPermille` on decode ⇒ throws (F3). Zero is structurally impossible to produce, so one in a blob means corruption — and accepting it silently would break the presence invariant the drain relies on. |
| T-NW-FAIL-004 | Decode: wrong `INBOX_SAVE_FORMAT_VERSION` ⇒ throws, version read **first** (F7). |
| T-NW-FAIL-005 | Decode: an out-of-bounds / near-`int.MaxValue` length prefix ⇒ throws via the overflow-safe bound against `total − offset`, never wraps (F7). |
| T-NW-FAIL-006 | Decode: trailing bytes ⇒ throws (F7). |
| T-NW-FAIL-007 | `TryTakePendingDelta` for a player with nothing pending returns **`false`** and does not throw — #30 asks for every player at step 3 (§2.3). |
| T-NW-FAIL-008 | `MarkRead` for an unknown or evicted item is a **no-op**, not a throw (F9 / §3.3). |

## 5.10 Closed-loop scenario (#19 `ScenarioRunner`, T-phase)

One Simulation-layer scenario, `inbox-across-a-season`, owning specs `{16, 19, 27, 30, 33, 35, 46, 49}`,
registered under `SCENARIO_PATH_CROSS_SPEC_PREFIX`:

play a season with the match and press projectors wired; assert a match item's **scoreline is still
correct after the table has absorbed later results** (T-NW-I-005 at composition level); query the inbox
repeatedly and assert the **blob is byte-identical** across every read; perform a man-management talk;
**save between the talk and the next step 3**; restore; advance; and assert #33's morale outputs match an
uninterrupted run exactly, that the delta was applied **once**, and that a second display locale produces
byte-identical #46 state.

This is the composition-level proof that KD-1's persistence, KD-2's projector siting, KD-3's routed
delta, KD-6's write-nothing query, and KD-7's no-tick-slot argument hold **together** — and it is exactly
where a query-time mutation (§3.2) would surface, since only a composed save/restore comparison catches it.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-27 | — | Initial §5. The two identity scopes are separate tests (T-NW-ID-001/002) because only the second describes the shipped minimal tier; T-NW-U-009 is flagged as **the KD-7 lock**, since a query that writes would oblige #46 to take a tick slot; T-NW-LOC-002/003 are separate because the two enums fail in two different ways and checking one proves nothing about the other; T-NW-BOUND-004 asserts producer-immutability over the **projectors**, the one place a foreign write can hide given #46 references nothing. Status IN REVIEW. |
#endregion
