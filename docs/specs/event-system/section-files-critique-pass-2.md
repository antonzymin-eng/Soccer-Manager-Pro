# Event System Specification #17 — Section-Files Adversarial Critique (PASS 2)

**Created:** May 13, 2026
**Reviewer:** Claude Code (adversarial pass)
**Scope:** `section-1.md` … `section-9-approval-checklist.md` + `appendices.md` at v0.2 (May 13, 2026 — post-PASS-1 resolution; commit `a84c811`).
**Prior reviews:**
- `outline.md` PASS 1 — May 6, 2026.
- `outline-detailed.md` PASS 2 — May 12, 2026.
- `section-files-critique-pass-1.md` — May 13, 2026 (this is the **section-files** PASS 1; the spec text variously calls it "PASS 3" in §6.3.2 line 136 and "section-files PASS 1" everywhere else — see M-2-6).
**Status:** DRAFT — 2 H / 6 M / 7 L findings published.

> This pass re-reads the section files at v0.2 to (a) verify each PASS 1 fix did not introduce new problems, (b) catch issues that PASS 1 missed, and (c) audit the coherence of the v0.2 edits as a set. Style follows the PASS 1 critique conventions.

---

## Executive Summary

The 20 PASS 1 findings are all addressed in the spec text as committed in `a84c811`. The v0.2 edits are internally consistent in most places, but **the propagation of two findings (L3 minting `ERR_EVT_REGISTRATION_PHASE`, L6 adding FR-EVT-009a single-marker constraint) is incomplete**, and **the H2 resolution to set `TickHeartbeatEvent` producer phase to `AI_NoOp` reintroduces an inconsistency** with the event's 60 Hz cadence claim (AI_NoOp only fires 5 ticks out of 6).

The High-severity findings in this pass are: (H-2-1) the H2 resolution's choice of `AI_NoOp` as producer phase contradicts the heartbeat's documented 60 Hz cadence and `maxPerTick = 60/tick` value, because AI_NoOp only runs on non-stride ticks; (H-2-2) `ERR_EVT_TIER_MISMATCH` may have no clear runtime trigger remaining after FR-EVT-009a forbids multi-marker structs and the §4.3.3 routing of FR-EVT-016 to Spec #20 lint.

Medium-severity issues fall in two clusters: (a) **partial L3 propagation** — §2.3 prose and the §2.3 routing table still reference `ERR_EVT_TIER_MISMATCH` for the post-init Tier A/B registration case after §2.5 / §3.10 / EC-017-005b were updated; (b) **partial L5 propagation** — FR-EVT-052 still bans `Action<…>` outright, contradicting the §3.5.4 carve-out for struct-ref delegates. Other Medium findings: a phantom-interface `IBootSubscriberRegistration` in §4.3.1, FR-EVT-046a / FR-EVT-046b numbering out of sequence in §2.2.1, and a one-off "PASS 3" label in §6.3.2 inconsistent with the rest of v0.2's "section-files PASS 1" vocabulary.

Low-severity findings cover wording-only issues that do not change behaviour.

No High-severity PASS 1 finding remains open at v0.2 (all three were correctly resolved at the structural level). The H-2-1 finding is a new defect introduced by the H2 fix; the H-2-2 finding was latent at v0.1 and surfaced only because L6 closed off the last compile-time path that previously rationalised the runtime code.

---

## Findings — HIGH severity (2)

### H-2-1. `TickHeartbeatEvent` producer phase `AI_NoOp` contradicts 60 Hz cadence

**Locations:**
- `appendices.md` Appendix A `0x09` row — `Producer phase = "AI_NoOp (typical; Tier C — informational per §A.1 note)"`, `maxPerTick = 60 / tick (rate-limited to once per tick)`.
- `section-2.md` §2.4.2 seed table — `Producer phase = "AI_NoOp (typical; informational for Tier C)"`.
- `section-3.md` §3.3.1 cadence map — row reads `60 Hz` cadence, producing phase `AI_NoOp`.
- `section-3.md` §3.3.2 — "`AI_NoOp` MAY publish a single `TickHeartbeatEvent`".
- `section-2.md` FR-EVT-036 — "Tactical Tier A events are queued only on stride ticks (`tick % 6 == 0`) during the `AI` phase."

**Issue:** The `AI` phase runs `AI_NoOp` on **non-stride ticks** (5 of every 6 ticks per the `tick % 6 == 0` stride rule). On stride ticks, the AI phase runs the actual tactical AI — **not** `AI_NoOp`. There is no spec text giving `AI` (non-NoOp) permission to publish `TickHeartbeatEvent`. As written, the heartbeat fires from at most 5 of every 6 ticks (≈ 50 Hz effective), not 60 Hz. The cadence column ("60 Hz") and the `maxPerTick = 60 / tick` value both presume every-tick firing.

This is a v0.2 regression: v0.1 had producer phase `Snapshot`, which runs every tick. The H2 resolution swapped to `AI_NoOp` to match §3.3.2's normative MAY text, but that fix only resolves the contradiction in the direction that introduces the new one.

**Impact:** Three options surface depending on which side of the contradiction is correct:
- If the heartbeat is intended to fire every tick (60 Hz, maxPerTick = 60/tick), the producer-phase column should be `Snapshot` (the only phase that runs every tick) and §3.3.2's MAY should be re-scoped to "any phase MAY publish the heartbeat once per tick" — with the AI_NoOp example retained as a non-binding suggestion.
- If the heartbeat is intended to fire only on non-stride ticks, the cadence column should read `50 Hz (5/6 ticks)` and `maxPerTick` should be adjusted accordingly. A separate "stride-tick heartbeat" would need to be added if 60 Hz coverage is required.
- If the heartbeat is optional and may fire from any tick context, the `Producer phase` column should read `(any)` for Tier C and L7 should be re-stated to clarify that "informational" extends to "the column may be `(any)` when no single phase is typical".

**Recommended fix:** Option 1 — revert the H2 swap to `Snapshot`, keep §3.3.2's MAY-from-`AI_NoOp` as a worked example, and reword `maxPerTick = 60 / tick` accordingly. This preserves the v0.1 cadence/maxPerTick semantics while honouring the H2 critique's actual goal (consistency across all five locations). The §A.1 column-semantics note already makes Tier C `Producer phase` informational, so `Snapshot` is permissible even though the heartbeat may fire from elsewhere.

Alternatively, Option 3 — set the column to `(any)` and let the §A.1 note carry the entire meaning. This is the cleanest taxonomically but requires §6.5.1 trace-channel attribution to drop the "producer" column for Tier C events.

### H-2-2. `ERR_EVT_TIER_MISMATCH` has no clear runtime trigger after FR-EVT-009a closes the compile-time gap

**Locations:**
- §2.2 FR-EVT-016 — "Authoritative gameplay code MUST NOT subscribe to Tier C streams (`ERR_EVT_TIER_MISMATCH` if attempted)."
- §2.2 FR-EVT-076 — "Subscribers cannot register against the wrong tier marker (`ERR_EVT_TIER_MISMATCH`)."
- §2.2 FR-EVT-009a (new in v0.2) — "An event struct MUST implement exactly one tier-marker interface (`IEventA` XOR `IEventB` XOR `IEventC`)."
- §2.5 / §3.10 `ERR_EVT_TIER_MISMATCH = 0x1702`.
- §4.3.3 rejection paths — `CosmeticChannel.Subscribe` by authoritative code routes to **Spec #20 lint** failure, not a runtime error.

**Issue:** Before FR-EVT-009a (L6 of PASS 1), the runtime case for `ERR_EVT_TIER_MISMATCH` could be rationalised by a struct that implemented two markers (`IEventA, IEventC`) — when published, the dispatcher could discover the mismatch and raise the error. FR-EVT-009a eliminates that case at registration / load time. The remaining FR-EVT-016 case ("authoritative gameplay code subscribes to Tier C") is fundamentally a **caller-identity** distinction that the runtime cannot make: the `CosmeticChannel.Subscribe<T> where T : struct, IEventC` overload accepts any caller. §4.3.3 routes the violation to Spec #20 lint (compile-time), not a runtime error code.

FR-EVT-076's "wrong tier marker" is, after FR-EVT-009a, also a compile-time error — the generic constraint `where T : struct, IEventA` makes `Subscribe<FooEventC>` a compile-time mismatch, not runtime.

Consequence: `ERR_EVT_TIER_MISMATCH = 0x1702` is reserved in the error-code namespace but has **no live runtime trigger remaining**. Reserving an error code with no runtime path violates Spec #20's "errors that the runtime can never throw should not have codes" practice and pollutes crash-dump triage tooling.

**Impact:** The §2.5 row, the §3.10 row, and the §5.3 unit-test fixtures (`wrong_marker_test.cs`, `single_marker_test.cs`) all assume a runtime path. The §5.4 traceability rows for FR-EVT-016 and FR-EVT-076 cite **lint** as the verification mechanism — consistent with the new reality — but the error code itself is described as runtime-raised.

**Recommended fix:** Pick one of:
1. **Remove `ERR_EVT_TIER_MISMATCH`** from §2.5 / §3.10 entirely; rephrase FR-EVT-016 / FR-EVT-076 as compile-time / lint-only enforcement. The `0x1702` slot is recycled or left reserved for future use. This is the most honest fix.
2. **Define a runtime path explicitly**: add a registration-API parameter or context that lets the runtime distinguish "authoritative caller" from "UI/VFX caller" (e.g., a `SubscriberClass` enum) and have `Subscribe` validate it. This adds API surface and a new failure mode but justifies the error code.
3. **Keep the code as a debug-build assertion only**: explicitly mark `ERR_EVT_TIER_MISMATCH` as "raised only in debug builds via the registry-validator that reflects each subscriber's declared type vs its registration generic parameter". This is the smallest change but adds a debug-vs-release behavioural gap.

Recommendation: option 1 — recovers code-namespace cleanliness. The §4.3.3 routing already names lint as the enforcement; the error code adds no value.

---

## Findings — MEDIUM severity (6)

### M-2-1. §2.3 prose still routes post-init Tier A/B registration to `ERR_EVT_TIER_MISMATCH`

**Location:** `section-2.md` §2.3 line 158-160: "Tier-mismatch subscription — authoritative subscriber against a Tier C stream → spec-review failure at Stage 0; Spec #20 lint failure at Stage 0+1. **Runtime attempt at post-init Tier A/B registration → `ERR_EVT_TIER_MISMATCH`.**"

**Issue:** PASS 1 L3 minted `ERR_EVT_REGISTRATION_PHASE = 0x1705` specifically for the lifecycle case (post-init Tier A/B registration) and updated §2.5, §3.10, §3.2.2, §4.3.3, and EC-017-005b. The prose in §2.3 was not updated, so the spec now contradicts itself: §2.5 says `0x1705`, §2.3 says `0x1702`.

**Recommended fix:** Reword §2.3 line 160:

> "Runtime attempt at post-init Tier A/B registration → `ERR_EVT_REGISTRATION_PHASE` (per §2.5 / §3.2.2; lifecycle violation distinct from tier-marker mismatch)."

### M-2-2. §2.3 routing table missing row for `ERR_EVT_REGISTRATION_PHASE`

**Location:** `section-2.md` §2.3 routing table (lines 170-177).

**Issue:** The table has a row for "Tier-mismatch subscription" → `ERR_EVT_TIER_MISMATCH`, but no row for the post-init lifecycle violation. After PASS 1 L3 separated the two error codes, the table should grow a parallel row.

**Recommended fix:** Add a row:

| Violation | Stage 0 enforcement | Stage 0+1 enforcement |
|-----------|---------------------|------------------------|
| ... | ... | ... |
| Post-init Tier A/B registration | Spec review | Runtime `ERR_EVT_REGISTRATION_PHASE` (FR-EVT-021) |
| ... | ... | ... |

Also, depending on how H-2-2 lands, consider removing the existing "Tier-mismatch subscription | … | runtime `ERR_EVT_TIER_MISMATCH`" row in favour of a lint-only entry.

### M-2-3. FR-EVT-052 still bans `Action<…>` outright, contradicting §3.5.4's carve-out

**Locations:**
- `section-2.md` FR-EVT-052: "The publish path MUST NOT call `new T[…]`, `List<T>.Add`, LINQ, **`Action<…>`**, `string.Format`, async/await, or reflection."
- `section-3.md` §3.5.4 (L5 resolution): "`Action<…>` / `Func<…>` instantiated with **value-type generic arguments** … **Exempt:** custom struct-ref delegates declared with an `in T` parameter and `where T : struct` constraint — e.g., `delegate void EventHandler<T>(in T evt) where T : struct` (§3.2.2)."

**Issue:** FR-EVT-052 (the FR rule statement) bans `Action<…>` flat. §3.5.4 (the mechanics) carves out an exemption for struct-ref delegates with `in T` parameter. A reader who reads only §2 will conclude that the `EventHandler<T>` delegate violates FR-EVT-052; a reader who reads only §3 will conclude it's fine. Two parts of the spec disagree on the same rule.

**Recommended fix:** Reword FR-EVT-052:

> "The publish path MUST NOT call `new T[…]`, `List<T>.Add`, LINQ, `Action<…>` / `Func<…>` instantiated with value-type generic arguments (custom struct-ref delegates with `in T` parameter and `where T : struct` are exempt per §3.5.4), `string.Format`, interpolated strings that emit `string.Format`, `async`/`await`, or reflection."

Cross-references the §3.5.4 carve-out so future readers don't need to triangulate.

### M-2-4. `IBootSubscriberRegistration` is a phantom interface (CLAUDE.md "Interface Design Principle")

**Location:** `section-4.md` §4.3.1:

```csharp
public static void RegisterStartupSubscribers(
    IBootSubscriberRegistration boot);
```

**Issue:** The `IBootSubscriberRegistration` parameter type is named but never declared. §4.2.1 declares only `IEventA`, `IEventB`, `IEventC`. §4.2.4 ("Interfaces this spec intentionally does NOT declare") lists `IEventPublisher`, `ITransport`, `IEventSerializer` as phantom abstractions to avoid — `IBootSubscriberRegistration` belongs on that list by the same logic. The CLAUDE.md "Interface Design Principle" requires both sides to be specified; the producer side (match-init code) is not specified.

**Recommended fix:** Pick one of:
1. **Drop the interface** — replace with a concrete struct or array parameter: `RegisterStartupSubscribers(EventSubscription[] subscriptions)` or similar, with `EventSubscription` defined in §4.2.2 as a tagged struct.
2. **Declare both sides** — declare `IBootSubscriberRegistration` in §4.2.2 with a normative method-list contract, and name the match-init implementation site in §4.4 / §7.1. This expands the spec surface but eliminates the phantom.
3. **Add a row to §4.2.4** — explicitly list `IBootSubscriberRegistration` as deferred until match-init code is specified. This is a documentation-only fix and parallel to the `IReplayEventReader` treatment, but at the cost of leaving a phantom in the §4.3.1 signature.

Recommendation: Option 1 (concrete subscription list) — simplest and matches the existing Stage 1 deliverables list at §7.1.

### M-2-5. FR-EVT-046a / FR-EVT-046b numbering placement in §2.2.1 table

**Location:** `section-2.md` §2.2.1 lines 107-110 — FR-EVT-046 (line 107), FR-EVT-047 (line 108), then FR-EVT-046a (line 109), FR-EVT-046b (line 110).

**Issue:** The H1 resolution added FR-EVT-046a and FR-EVT-046b after FR-EVT-047 rather than between FR-EVT-046 and FR-EVT-047. The table is otherwise FR-ID-sorted. A reader scanning for "the FR right after FR-EVT-046" will hit FR-EVT-047, not FR-EVT-046a. Mechanical readers (validators, traceability scripts) that assume ID-sorted order will mis-parse.

**Recommended fix:** Re-order the rows so they read FR-EVT-046 → FR-EVT-046a → FR-EVT-046b → FR-EVT-047. (Alternatively, rename the new FRs to `FR-EVT-046.1` / `FR-EVT-046.2` if the spec prefers dotted sub-IDs — but the existing FR-EVT-009a pattern argues for the lettered convention being correct as long as ordering is enforced.)

### M-2-6. "PASS 3" label in §6.3.2 inconsistent with rest of v0.2

**Locations:**
- `section-6.md` §6.3.2 line 136: "resolves PASS 2 finding 11; **resolves PASS 3 finding H1**".
- All other v0.2 references use "section-files PASS 1 critique" or "PASS 1 critique resolution" (file history entries at §2.6 / §3.11 / §4.6 / §5.7 / §6.7 / §8.5 / §9.5 / Appendices Version History; §6.3.4 trigger; section-files-critique-pass-1.md title).

**Issue:** The §6.3.2 reference uses overall-pass numbering (PASS 1 = outline May 6; PASS 2 = outline-detailed May 12; PASS 3 = section-files May 13). Everywhere else uses section-files-relative numbering (the section-files critique IS PASS 1 against section files). One vocabulary should win across the spec.

**Recommended fix:** Standardise on the section-files-relative vocabulary (matches the file name `section-files-critique-pass-1.md` and the existing seven v0.2 history rows). Edit §6.3.2:

> "Headroom is therefore **×2 over the dispatch-depth-bounded worst case under FR-EVT-046a**, not ×16 over the first-order ceiling alone (resolves outline-detailed PASS 2 finding 11; resolves section-files PASS 1 finding H1)."

Optionally add a one-line glossary entry in `section-1.md` §1.5 disambiguating the three pass conventions.

---

## Findings — LOW severity (7)

### L-2-1. §3.2.4 "PASS 2 finding 4" reference predates v0.2 and may now be ambiguous

**Location:** `section-3.md` §3.2.4 line 197 — "Counter scope (normative; resolves PASS 2 finding 4)."

**Issue:** "PASS 2 finding 4" refers to `outline-detailed.md` PASS 2 (May 12). After v0.2 introduced "section-files PASS 1" vocabulary, a reader may read this as "section-files PASS 2 finding 4" — which would be a forward reference to this very critique. Inherited from v0.1, but worth disambiguating.

**Recommended fix:** Change to "resolves **outline-detailed** PASS 2 finding 4". Parallel edit to M-2-6.

### L-2-2. §3.2.4 "registration-order tiebreaker" claim is misleading

**Location:** `section-3.md` §3.2.4 sort-tuple attribution table, `entityId` row: "Handlers that aggregate over multiple entities use `EntityId.None` (sentinel; reserved per #16 §2 `TBD-NORMATIVE`); **registration-order acts as the de-facto tiebreaker via the `intraPhaseDrawIndex` increment**."

**Issue:** `intraPhaseDrawIndex` is **enqueue order**, not registration order. The text conflates the two. Subscriber registration order (FR-EVT-074) determines dispatch order; dispatch order determines when each handler runs; each handler's enqueues advance the `Events`-phase `intraPhaseDrawIndex`. The chain is correct, but stating it as "registration-order tiebreaker" elides the indirection.

**Recommended fix:** Reword:

> "Handlers that aggregate over multiple entities use `EntityId.None` (sentinel; reserved per #16 §2 `TBD-NORMATIVE`). Uniqueness of the sort tuple is maintained by the monotonically-incrementing `intraPhaseDrawIndex`, which advances on every enqueue. Because subscribers are dispatched in registration order (FR-EVT-074), the resulting `intraPhaseDrawIndex` values reflect registration order indirectly."

### L-2-3. §3.2.4 sort scratch-buffer element size unspecified

**Location:** `section-3.md` §3.2.4 sort-timing paragraph: "The sort routine uses a stackalloc'd scratch buffer sized to `EVENT_QUEUE_CAPACITY` to preserve KD-8 (§6.2 allocation budget)."

**Issue:** `EVENT_QUEUE_CAPACITY = 1024` slots, but the slot element size is undefined here. If the scratch holds event records (~24-64 bytes each), the stackalloc is 24-64 KB — borderline for nested call stacks on Windows. If it holds indices into the ring buffer (`ushort` = 2 bytes), the stackalloc is 2 KB — trivial. Implementation matters for the §6.2 KD-8 budget claim.

**Recommended fix:** Add a sentence: "The scratch buffer holds 1024 × `sizeof(ushort)` = 2 KB of `ushort` indices into the ring buffer; events are not copied during sort." If the intent is in-place index sort, the §3.2.4 sort algorithm description should also specify "indirect sort by index" rather than implying a value sort.

### L-2-4. §3.5.4 `foreach` rule wording reads as a ban-with-weakening

**Location:** `section-3.md` §3.5.4 line 439-444.

**Issue:** The first sentence reads "`foreach` over a type that implements `IEnumerable<T>` (the compiler emits an allocating `GetEnumerator()` call when the target is not a fixed-size array, `Span<T>`, or a struct enumerator)." A reader sees "banned" first, then a parenthetical that re-permits most cases. The "when the target is not" clause is doing all the actual work but is buried in the parenthetical.

**Recommended fix:** Restructure as two bullets:

> - **Banned:** `foreach` over any type whose enumerator allocates on the GC heap (e.g., `List<T>`, `Dictionary<,>`, any `IEnumerable<T>`-typed reference).
> - **Permitted:** `foreach` over `T[]`, `Span<T>`, or any type whose `GetEnumerator()` returns a struct enumerator (the compiler avoids the heap allocation).

Parallel to the rewording proposed for FR-EVT-052 in M-2-3.

### L-2-5. Appendix A row `0x09` Producer-phase column reads as embedded prose

**Location:** `appendices.md` Appendix A row `0x09` — `Producer phase = "AI_NoOp (typical; Tier C — informational per §A.1 note)"`.

**Issue:** The column-semantics note at §A.1 already covers the Tier C "informational" rule. Embedding the same note in the row makes the row read like a paragraph and breaks the table's scannability. Other Tier C rows (`VfxImpactCue`, `UiNotificationCue`) just say `Resolve` without the disclaimer.

**Recommended fix:** Pending H-2-1 resolution. If the column reverts to `Snapshot` (H-2-1 option 1), make it just `Snapshot` like the other Tier A/C rows. If the column becomes `(any)` (H-2-1 option 3), use that single token.

### L-2-6. FR-EVT-076 cites §2.5 as Source — should cite the rule's mechanics

**Location:** `section-2.md` §2.2 FR-EVT-076 row — `Source: §2.5`.

**Issue:** §2.5 is the error-code table, not the source of the rule statement. Other FR rows in §2.2.1 cite §3.x or §4.x mechanics, or a CLAUDE.md / Spec-#-§-x.x normative source. The rule about tier-marker mismatch belongs to §3.2.2 (subscriber lifecycle) or §4.3.3 (registration rejection paths).

**Recommended fix:** Change FR-EVT-076 Source column from `§2.5` to `§3.2.2 / §4.3.3`.

### L-2-7. §3.10 row-level "design-fixed" sub-class flag would aid scanability

**Location:** `section-3.md` §3.10 rows for `EVENT_TYPE_ORDINAL_WIDTH`, `PAYLOAD_VERSION_WIDTH`, and the five `ERR_EVT_*` codes.

**Issue:** The §3.10 note (post-table) classifies these as "design-fixed `[GT]`", but the row-level Tag column just shows `[GT]` — the same as the runtime-tunable rows. A reader skim-reading the table sees `[GT]` everywhere and doesn't distinguish the sub-class. The §6.3.4 re-tuning trigger applies to only the runtime-tunable sub-set; this distinction is invisible row-level.

**Recommended fix:** Annotate the Tag column for design-fixed rows: `[GT]` → `[GT] (design-fixed)`. Alternatively, add a separate column "Sub-class" that takes values `runtime-tunable` / `design-fixed` / `cross-pending`. The note then becomes a glossary entry rather than the sole source of the distinction.

---

## Cross-Cutting Observations

- **PASS 1 H3 / §5.4 traceability table — overall coherent.** Each FR row in §5.4.1 … §5.4.10 has a Tooling + Activation + Artifact column populated. Spot-checked 12 rows against §2.2.1 — all match. Two minor observations: (a) FR-EVT-046a and FR-EVT-046b rows in §5.4.4 share artifact ("Same fixture as FR-EVT-046a") but FR-EVT-046b's row says "Same fixture as FR-EVT-046a" without naming the file path, requiring a click-through; minor but worth inlining; (b) the §5.4 traceability rows for FR-EVT-016 and FR-EVT-076 cite lint as the verification — consistent with H-2-2's resolution toward lint-only enforcement.
- **CLAUDE.md `[CROSS-PENDING]` tag addition is minimal and correct.** The added row follows the existing tag-table format and reads cleanly. Future specs can now declare `[CROSS-PENDING]` constants without inventing local vocabulary. Recommended: when ERR-017-001 resolves (#16 §3.4 patch lands), file a row in `docs/tracking/spec-error-log.md` flagging the first `[CROSS-PENDING]` → `[CROSS]` promotion so the new tag's lifecycle is exercised on a real case.
- **Section-files-critique-pass-1.md naming.** The file name uses lowercase "pass-1" while the spec body sometimes uses "PASS 1" / "PASS 2" (overall) / "PASS 3" (overall). M-2-6 standardises on the section-files vocabulary; recommend keeping the lowercase file name and using "section-files PASS 1" prose form throughout the spec.

---

## Recommended Resolution Plan (author-side)

1. **H-2-1 first** — pick the producer-phase column convention for `TickHeartbeatEvent` and propagate to all five locations (Appendix A row, §2.4.2 seed table, §3.3.1 cadence map, §3.3.2 normative MAY, §6.5.1 trace channel attribution). Recommended: revert to `Snapshot` and re-state §3.3.2 as a non-binding example.
2. **H-2-2 second** — either remove `ERR_EVT_TIER_MISMATCH` from the runtime code-namespace (recommended) or add a runtime path. Decision propagates to §2.5, §3.10, §4.3.3, §5.4 (FR-EVT-016 / FR-EVT-076 rows), §6.5.1 trace channel `event-system.tier-mismatch`, EC-017-005a, §8.4.
3. **M-2-1 / M-2-2** as a single edit pass over §2.3 (prose + routing table) — both surface the same L3 propagation gap.
4. **M-2-3** as a single edit to FR-EVT-052 (10-line reword).
5. **M-2-4** — choose between dropping `IBootSubscriberRegistration`, declaring it, or listing it in §4.2.4. Recommended: drop and replace with a concrete-array parameter.
6. **M-2-5 / M-2-6** — mechanical re-ordering / wording edits.
7. **L-2-1 … L-2-7** — bundle into a single follow-up commit; none individually block IN REVIEW.
8. **Open a PASS 3 review window** once H/M findings clear. The PASS 3 reviewer should re-run the §3.4.5 cite-precision grep, the §9.2 Q-row evidence verification, and a fresh sweep on cross-section consistency (the same pattern that surfaced M-2-1 / M-2-2 / M-2-3 as L3 / L5 / L6 propagation gaps).

---

## Version History

| Version | Date         | Author      | Notes                                                                 |
|---------|--------------|-------------|-----------------------------------------------------------------------|
| 0.1     | May 13, 2026 | Claude Code | Initial PASS 2 critique against v0.2 section files. 2 H / 6 M / 7 L findings; resolution plan published. |
