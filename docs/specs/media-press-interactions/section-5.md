# Media & Press Interactions #35 — Section 5: Test Plan

**Created:** July 27, 2026
**Last Updated:** July 27, 2026 (v0.2 — PASS-1 fix pass)
**Version:** 0.2
**Status:** IN REVIEW

---

Test-ID prefixes follow #19 §3.1.4: `T-ME-U-*` unit, `T-ME-I-*` integration, `T-ME-DET-*` determinism,
`T-ME-ID-*` identity / behaviour-neutrality, `T-ME-LOC-*` localization compliance, `T-ME-FAIL-*`
fail-loud, `T-ME-BOUND-*` structural.

Every value asserted below is **hand-derivable from §3.8** or is a relational property. Nothing here
requires a fabricated expected number — in particular, no test asserts a specific `SelectionValue` output,
which would be a fabricated hash (§5.4).

## 5.1 Identity / behaviour-neutrality (KD-9)

| ID | Test |
|---|---|
| T-ME-ID-001 | **The headline lock, stated at KD-9's precondition — not a weaker one.** A season in which **no conference is queued**, *or* in which **every answer's consequence is `0`**, produces `HumanSystemsDayInput.ExternalDeltaPermille == 0` at every step 3, and #33's outputs are **field-identical** to a pre-#35 run. |
| T-ME-ID-002 | **The precondition trap, asserted as its own test.** A season in which conferences are queued and **left unanswered** is **not** an identity case — expiry resolves them to the no-comment option, which *is* an answer. This test asserts that a non-zero no-comment delta **does** move morale, so a suite written to the weaker *"no conference answered"* precondition fails here rather than silently proving less than it claims. |
| T-ME-ID-003 | With the #30 media seams null, a season is **byte-identical** to the same season pre-#35 (the FR-SN-026 world-floor property). |
| T-ME-ID-004 | **(T0/T1 only.)** The season save is byte-identical to the pre-#35 save. Scoped deliberately: at **T2** the frame gains #35's sub-blob, so the *save* is not byte-identical — KD-9's identity is about #33's outputs and existing RNG cursors, never about the save frame. |
| T-ME-ID-005 | No RNG stream is registered at the minimal tier: a full season of queueing, answering and expiring leaves **every** registered stream's cursor byte-identical (FR-ME-016). |

## 5.2 Unit — the lifecycle (§3.1 / §3.2 / §3.3)

| ID | Test |
|---|---|
| T-ME-U-001 | §3.8(a) exact: managed club home, wins 2–1 on day 100 ⇒ `QPostWin`, `DeadlineWorldDay = 103`. |
| T-ME-U-002 | §3.8(b) exact — **the away mirror**: managed club **away**, loses 2–1 ⇒ `QPostLoss`. Present because the project's own ERR-008-002 defect class was *"every spec worked example and every fixture used the home team"*, and #35's archetype selection is exactly the home/away-relative geometry that breaks. |
| T-ME-U-003 | §3.8(c): managed club away, 1–1 ⇒ `QPostDraw`. |
| T-ME-U-004 | §3.8(d): a fixture between two non-managed clubs queues **nothing** and leaves `NextConferenceId` unadvanced — the gate is step 1, so no cursor moves. |
| T-ME-U-005 | §3.8(e): a full queue **drops** on a recorded branch and returns `false` — **no throw** (F7 / FR-ME-023). |
| T-ME-U-006 | §3.8(j)/(k): expiry fires at `worldDay >= DeadlineWorldDay`, not before, and resolves to the conference's **no-comment option** — a defined member of its own roster (FR-ME-027). |
| T-ME-U-007 | §3.8(l): re-advancing the same `worldDay` is a **no-op** with field-identical state. |
| T-ME-U-008 | §3.8(m): a **day gap** throws (F6). Paired with T-ME-U-007 so the two halves of the guard cannot drift apart. |
| T-ME-U-009 | **Stamp-last** (§3.3): a throw inside `RecordConsequence` leaves `LastAdvancedWorldDay` unchanged, so the day stays retryable. |
| T-ME-U-010 | An enqueued conference's option roster is non-empty, within `MEDIA_MAX_OPTIONS`, and **entirely in the option band**; its question intent is **entirely in the question band** (F5). |

## 5.3 Unit — the answer command's refuse/throw split (§3.2)

The three tests that a single uniform error policy would get wrong — two by throwing on a legal race, one
by silently accepting a client bug.

| ID | Test |
|---|---|
| T-ME-U-011 | §3.8(f): a valid answer on a pending conference returns `true` and records the consequence. |
| T-ME-U-012 | §3.8(g): **answering an already-resolved conference returns `false`** — a legal render/tick race, **not** a throw (F3). |
| T-ME-U-013 | §3.8(h): an `optionIndex` outside **that conference's own** roster **throws** (F2). |
| T-ME-U-014 | §3.8(i): an unknown `conferenceId` **throws** (F2) — never `false`, which would hide a client bug. |
| T-ME-U-015 | **The check ordering** (§3.2): an already-resolved conference given an `optionIndex` that is out of range returns **`false`**, not a throw — because the resolution check precedes the range check, and a client holding a stale render is exhibiting the same legal race F3 classifies. Locked so a later "validate arguments first" tidy-up fails here. |

## 5.4 Determinism

| ID | Test |
|---|---|
| T-ME-DET-001 | Two runs over the same fixture and command sequence produce **field-identical** state. |
| T-ME-DET-002 | `save@N → restore → advance to N+K` is **field-identical** to the uninterrupted run. |
| T-ME-DET-003 | **`SelectionValue` is position-independent** (FR-ME-017): the value for a given `(intent, worldDay, subjectId, purpose)` is **identical regardless of how many selections preceded it**, and identical across a save/restore. Asserted **relationally** — no test pins a specific `ulong`, which would be a fabricated hash. This is the lock that fails if the keyed mix is later "simplified" into a cursor-advancing draw. |
| T-ME-DET-004 | **`SelectionValue` is injective enough to be useful**: over a swept grid of `(intent, worldDay, subjectId)` the outputs are distinct in an overwhelming majority, and — the property that actually matters — the **subject-less** case (`MEDIA_NO_SUBJECT`) does not collide with any real `subjectId` (§3.5's `+1` shift). |
| T-ME-DET-005 | **Nothing RNG-related is serialized**: the sub-blob contains no cursor and no stream state, asserted structurally (FR-ME-020). |
| T-ME-DET-006 | *(deep tier)* **Position-independence of the archetype draw:** an evaluation preceded by a different number of prior draws yields the **same** result — the lock that fails if the keyed ordinal is later replaced by a free-running cursor. |
| T-ME-DET-007 | *(deep tier)* `DeriveActionOrdinal` is injective over `(subjectId, worldDay, purpose)` across the tested range, and **refuses** `purpose >= MEDIA_PURPOSE_RADIX` and `subjectId >= MEDIA_SUBJECT_STRIDE` — the second being the guard that would otherwise silently alias two subjects onto one draw. |
| T-ME-DET-008 | *(deep tier)* Exactly **one** `RegisterStream` call occurs regardless of club or player count (FR-ME-019) — the `MaxRngStreams = 64` bound. |

## 5.5 Integration — delivery, save/restore, and the roster lifecycle

| ID | Test |
|---|---|
| T-ME-I-001 | §3.8(n): **delivery exactly once.** A delta recorded on day 103 is drained at day 104's step 3, cleared, and a second step-3 call the same day returns `false`. |
| T-ME-I-002 | §3.8(o): **the invariant the blob exists for.** A save taken between the answer and the next step 3 carries the pending row; the restored run delivers it **exactly once** — not zero times (dropped) and not twice (re-applied). |
| T-ME-I-003 | §3.8(p): **drop-on-departure** (F9 / FR-ME-035). A pending delta whose target retires at the season boundary is **removed**; and across a full season of roster churn the blob **does not grow**. The second half is what catches a partial implementation that removes the player from iteration but leaves the row. |
| T-ME-I-004 | A pending delta whose target is **transferred** (a #31 re-key) is **dropped**, not delivered to whoever now holds that id. Asserted directly, because silent mis-delivery to a real player is the failure mode, and it produces no error. |
| T-ME-I-005 | State → `Encode` → `Decode` is **field-identical**, including the `MEDIA_NOT_ADVANCED_SENTINEL` cursor, an empty queue, a pending conference, an answered conference, and an undelivered delta. |
| T-ME-I-006 | Round-trip through a full `SeasonSaveCodec` frame: #35's sub-blob is **opaque** to the outer codec, and the world / season / match / sibling blobs are **byte-unchanged**. |
| T-ME-I-007 | The two format versions move **independently**: bumping `MEDIA_SAVE_FORMAT_VERSION` does not require a `SEASON_SAVE_FORMAT_VERSION` bump, and vice versa. |
| T-ME-I-008 | **`HUMAN_SYSTEMS_SAVE_FORMAT_VERSION` is untouched** — `HumanSystemsDayInput` is a transient input struct, so ERR-033-003 adds no serialized field. Asserted so a later reviewer does not "helpfully" persist the routed value. |
| T-ME-I-009 | **A zero delta writes no row** (FR-ME-033): a season of expiries whose no-comment option carries `0` leaves the pending-delta block **empty**, so *"is a delta pending for this player?"* stays answerable by presence. |
| T-ME-I-010 | **The root's post-sum clamp** (§4.4): with #35 and a second producer each contributing at their own bound, the value reaching #33 is within the field's contract. The cost of the shared `ExternalDeltaPermille` field, locked rather than assumed. |

## 5.6 Localization compliance (#49)

| ID | Test |
|---|---|
| T-ME-LOC-001 | **Catalogue coverage over the FULL roster** (FR-ME-012 / FR-LC-008a): **every** defined `MediaIntent` — questions **and** answer options — has a base-locale template row. The "and options" half is the one a coverage test written from KD-1's first paragraph would miss, and missing it is what would push an implementer to bake option labels. |
| T-ME-LOC-002 | **`MediaIntent` ordinal stability** (KD-10 / FR-ME-009): each member's ordinal equals its pinned value, and `MEDIA_INTENT_OPTION_BAND_START` equals its pinned value. This is a **save-correctness** lock, not a style lock: the ordinal is serialized *and* is the catalogue key, so a reorder re-points every saved conference at a different template with **no version gate to catch it** — the save loads cleanly and renders the wrong text. |
| T-ME-LOC-003 | **The band is respected** (FR-ME-010 / F5): every question-band member is used only as a `QuestionIntent` and every option-band member only inside `OptionIntents`, swept over the whole catalogue. |
| T-ME-LOC-004 | **#35 emits no display string** (FR-ME-005 / FR-LC-002): a source-level assertion over `src/media/` finds no `string` field, no `string` return, and no string formatting in any #35 type. |
| T-ME-LOC-005 | **Locale-independence of state** (FR-LC-006): the same career advanced under two display locales produces **byte-identical** serialized #35 state. #35 feeds no match digest, so bytes are the claim. |
| T-ME-LOC-006 | **The value gate runs before any selection work** (FR-ME-011 / FR-LC-015): `MediaIntent.None` and an undefined ordinal are refused, and the refusal consumes nothing — no cursor, no selection computation. |
| T-ME-LOC-007 | **The gate is #35's, not the adapter's**: the refusal in T-ME-LOC-006 holds when `MediaIntent` is used through any #35 surface, not only through `MediaTextBoundary` — a gate living only in the boundary layer would be bypassed by any other consumer. |

## 5.7 Fail-loud (§2.3)

| ID | Test |
|---|---|
| T-ME-FAIL-001 | An out-of-range `deltaPermille`, a consequence list over `MEDIA_MAX_CONSEQUENCE_TARGETS`, or an option list over `MEDIA_MAX_OPTIONS` ⇒ **throws at the authoring boundary** (F4) — before it can reach a save. |
| T-ME-FAIL-002 | A question intent in the option band, or an option intent in the question band ⇒ throws at the authoring boundary **and** at decode (F5). |
| T-ME-FAIL-003 | Decode: wrong `MEDIA_SAVE_FORMAT_VERSION` ⇒ throws, with the version read **before** any field below it is interpreted (F8). |
| T-ME-FAIL-004 | Decode: an out-of-bounds / near-`int.MaxValue` length prefix ⇒ throws via the overflow-safe bound compared against `total − offset`, never wraps (F8). |
| T-ME-FAIL-005 | Decode: trailing bytes ⇒ throws (F8). |
| T-ME-FAIL-006 | Decode: an undefined `MediaIntent` ordinal, or an out-of-range `DeltaPermille` ⇒ throws (F8). |
| T-ME-FAIL-007 | Decode: a **zero** `DeltaPermille` row ⇒ throws. FR-ME-033 makes such a row structurally impossible to produce, so encountering one means corruption — and accepting it silently would break the presence invariant T-ME-I-009 rests on. |
| T-ME-FAIL-008 | `TryTakePendingDelta` for a player with nothing pending returns **`false`** and does **not** throw — #30 asks for every player at step 3, so absence is the common case (§2.3). |

## 5.8 Structural (the boundaries #35 must not cross)

| ID | Test |
|---|---|
| T-ME-BOUND-001 | **The minimal tier references nothing**, and the deep tier references **only** `TacticalDirector.DeterministicSim` — asserted from the assembly's reference set, so a future `using` of #30 / #33 / #45 / #46 / `Localization` / `living-world` / `SeasonSave` / `MatchEngine` fails the build's test gate (FR-ME-006, the #40 `T-FN-BOUND-002` posture). |
| T-ME-BOUND-002 | **#35 never writes #33 state** (FR-ME-004): a `MoraleState` and `PersonalityProfile` handed alongside every #35 entry point are **field-unchanged** after queue, answer, expiry, drain, and save/restore. Asserted behaviourally, since at the deep tier the reference graph alone cannot prove it. |
| T-ME-BOUND-003 | **#35 declares no reputation scalar** (FR-ME-007 / KD-4) — asserted over the public surface, because the temptation is to add one as a convenience and it would compile. |
| T-ME-BOUND-004 | **#35 declares no template-id type** and no type named `InteractionSlots`, `InteractionIntent`, or `TextTemplateId` (FR-ME-008 / FR-ME-014) — the parallel-surface lock. |
| T-ME-BOUND-005 | **#35 fires no event and holds no unread flag** (FR-ME-036 / KD-6) — the one-directional `#46 → #35` property, asserted over the public surface so a future push-notification convenience cannot open the reverse edge. |
| T-ME-BOUND-006 | **No `RegisterStream` call exists at the minimal tier** — asserted over the compiled surface rather than the reference graph, which cannot prove it once the deep tier references #16. |

## 5.9 Closed-loop scenario (#19 `ScenarioRunner`, T-phase)

One Simulation-layer scenario, `press-conference-across-a-season`, owning specs `{16, 19, 27, 30, 33, 35,
49}`, registered under `SCENARIO_PATH_CROSS_SPEC_PREFIX`:

play a season with a managed club; assert conferences queue only on the managed club's fixtures; answer
some and let others expire; **save between an answer and the next step 3**; restore; advance; and assert
that #33's morale outputs match an uninterrupted run **exactly**, that every delta was applied exactly
once, that the pending block is empty at season end, and that a second run under a different display
locale produces byte-identical #35 state.

This is the composition-level proof that KD-3's routed value, KD-5's command/tick split, KD-7's blob
invariant, and KD-9's identity hold **together** — which no unit test exercises jointly, and which is
exactly where the two-seam requirement (§4.4) would fail if only the queue seam had been wired.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-27 | — | Initial §5 (identity, lifecycle units keyed to the §3.8 worked examples, the refuse/throw split, determinism, delivery + save integration, localization compliance, fail-loud, structural, the T-phase closed-loop scenario). Status IN REVIEW. |
| 0.2 | 2026-07-27 | — | PASS-1 fixes. **M:** added **T-ME-ID-002** — the KD-9 precondition trap asserted as its own test, so a suite written to the weaker *"no conference answered"* precondition **fails** rather than silently proving less; this document's own review cycle made that error three times, so a prose warning is demonstrably insufficient. **M:** added **T-ME-FAIL-007** (a decoded zero delta must throw) — FR-ME-033 makes the row impossible to produce, so accepting one on decode would silently break the presence invariant T-ME-I-009 rests on. **M:** added **T-ME-I-010** (the root's post-sum clamp) and **T-ME-I-008** (`HUMAN_SYSTEMS_SAVE_FORMAT_VERSION` untouched) — both consequences of the shared `ExternalDeltaPermille` field that were asserted in prose only. **L:** added T-ME-U-015 (check ordering), T-ME-LOC-007 (the gate is #35's, not the adapter's), T-ME-BOUND-006 (no `RegisterStream` at minimal, asserted over the compiled surface since the deep tier legitimately references #16), T-ME-DET-004 (the `+1` shift's collision property); T-ME-DET-003 explicitly scoped to a **relational** assertion, since pinning a `SelectionValue` output would be a fabricated hash; the away-mirror rationale attached to T-ME-U-002. |
#endregion
