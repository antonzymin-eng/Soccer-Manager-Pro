# Event System Specification #17 — Section-Files Adversarial Critique (PASS 1)

**Created:** May 13, 2026
**Reviewer:** Claude Code (adversarial pass)
**Scope:** `section-1.md` … `section-9-approval-checklist.md` + `appendices.md` (v0.1, May 13, 2026 initial section-file draft from `outline-detailed.md` v1.1).
**Prior reviews:** `outline.md` PASS 1 (May 6, 2026); `outline-detailed.md` PASS 2 (May 12, 2026). This is the **first** adversarial pass against the section-file text itself.
**Status:** DRAFT — findings published; resolution and follow-up review pass to be authored by spec author before IN REVIEW transition.

> Style follows the Pass Mechanics #5 / Testing Strategy #19 self-critique conventions: severity-classified findings, each with location → claim → impact → recommended fix. The author should respond inline or via a paired `findings-resolution-map.md` before requesting IN REVIEW.

---

## Executive Summary

`section-1` … `section-9` + `appendices.md` are a coherent, structurally complete first draft and correctly inherit the resolved PASS 1 / PASS 2 findings recorded in `outline-detailed.md` v1.1. The 82 FRs published in §2.2 carry the required conformance / source / verification / activation columns, the constants catalogue (§3.10) is populated, the §6 frame-budget envelope is declared, and the §9 approval checklist is anchored.

However, this pass finds **3 High / 8 Medium / 9 Low** issues which the author should resolve before transitioning to `IN REVIEW`. The High-severity items are: (H1) the §6.3.2 BFS occupancy derivation under-bounds the worst case because it implicitly assumes ≤ 1 secondary publish per handler — that constraint is not enforced anywhere in §3; (H2) `TickHeartbeatEvent` carries two contradictory producer-phase statements (`Snapshot` in Appendix A vs `AI_NoOp` in §3.3.2 / FR-EVT-038); (H3) §5.4 FR-to-verification traceability is degenerate at Stage 0 and §9.2 Q2 claims "every FR resolves" against a six-row example table — this is the exact "fabricated checklist value" trap CLAUDE.md warns about.

Medium and Low findings include tag-vocabulary breach (`[CROSS-PENDING]` is not a sanctioned tag in CLAUDE.md "Constant Tags"), the `[TBD-CITE]` qualifier introduced in FR-EVT-026 escapes the §9.2 Q11 audit rule which only checks `TBD-NORMATIVE`, an unspecified second-order publish `producingPhaseIndex`/`subsystemOrdinal` derivation, multiple naming inconsistencies (`producerSubsystem` vs `subsystemOrdinal`), and a missing back-prop hazard when producer-phase changes update Appendix A without coordinating with #16 §3.6.1 phase WriteSet.

None of these findings change the spec's architectural posture; all are recoverable within the current section-file structure.

---

## Findings — HIGH severity (3)

### H1. §6.3.2 BFS dispatch-depth fanout derivation under-bounds the worst case

**Location:** `section-6.md` §6.3.2; `section-3.md` §3.5.1 derivation (`64 × 8 × 2 = 1024`); `section-8.md` §8.4 row `EVENT_QUEUE_CAPACITY`.

**Claim under test:** "first-order ceiling × MAX_EVENT_DISPATCH_DEPTH = 64 × 8 = 512" worst-case BFS occupancy, ×2 headroom → `EVENT_QUEUE_CAPACITY = 1024 [GT]`.

**Issue:** The derivation is correct ONLY if every Tier A/B handler publishes **at most one** secondary event during dispatch. BFS with bounded **depth** but unbounded **out-degree per handler** has multiplicative — not additive — worst-case occupancy. If every handler at every BFS level publishes `k` secondary events, occupancy is `64 × Σ_{i=0..7} k^i`. For `k = 2`, that is `64 × 255 = 16,320` events, ~16× larger than `EVENT_QUEUE_CAPACITY`. The spec does NOT bound per-handler out-degree anywhere — §3.2.5 only bounds dispatch **depth**, not breadth per handler.

**Impact:** `EVENT_QUEUE_CAPACITY = 1024 [GT]` could be violated by a single handler that fan-outs two Tier A events, producing `ERR_EVT_QUEUE_OVERFLOW` at the second-order layer. KD-7 promises hard fail, but the `[GT]` constant's worst-case justification is incomplete and gives false confidence.

**Recommended fix (pick one):**
1. Add a normative FR in §2.2 capping per-handler out-degree to 1 secondary publish (then the additive math is correct). Cite the constraint in §3.2.5 and Spec #20 lint.
2. Add an explicit FR enforcing per-Tier-A-event-type `secondaryFanoutBound` and reflect it in Appendix A registry row schema; redo §6.3.2 math with the registry-summed worst case.
3. Increase `EVENT_QUEUE_CAPACITY` headroom and document the multiplicative model. Note that this loses the `64 × 8 × 2` audit trail and needs a fresh `[GT]` rationale.

Recommendation: Option 1 — caps per-handler out-degree to 1 in §3.2.5 (with the existing depth bound, this matches realistic Tier A coupling). Update FR catalogue, §3.10 derivation note, and §8.4 rationale together.

---

### H2. `TickHeartbeatEvent` (`0x09`) producer-phase contradiction

**Locations:**
- `appendices.md` Appendix A row `0x09` — **Producer phase = `Snapshot`**.
- `section-2.md` §2.4.2 seed table row `0x09` — **Producer phase = `Snapshot`**.
- `section-3.md` §3.3.1 cadence map row — **Producing phase = `Snapshot`**.
- `section-2.md` §2.2 FR-EVT-038 — "**`AI_NoOp` MAY publish a `TickHeartbeatEvent`** (Tier C) via the cosmetic channel."
- `section-3.md` §3.3.2 — "`AI_NoOp` MAY publish a single `TickHeartbeatEvent` … the hook is Tier C and runs through the cosmetic channel, **not** through the `AI` phase WriteSet."

**Issue:** Two different phases recorded for the same registry row. Both `outline-detailed.md` rows agree with the registry tables, but the rule text (FR-EVT-038 and §3.3.2) names a different phase. This is a real registry-row authoring bug, not just nomenclature.

**Impact:** Replay golden bytes derived from §3.2.4 sort key would differ depending on which producer-phase is used (FM-017-002 tuple position 1 is `producingPhaseIndex`). Although Tier C events do NOT enter the digest (FR-EVT-014), the registry row is consulted by §3.6.2 `maxPerTick`, the FR-EVT-038 normative MAY, and Appendix B examples. The `Producer phase` column for Tier C events is also ambiguous (see L7 below) — but the immediate fix is to make all five locations agree.

**Recommended fix:** Decide whether the heartbeat fires from `AI_NoOp` (semantically cleaner since it asserts the "non-stride is still ticking" invariant) or from `Snapshot` (already cited in §6.5.1 trace registry). Sync all five locations. If `AI_NoOp`, also adjust §6.3.1 worst-case math (Tier C aggregate at AI phase) and §6.5.1 trace channel producer.

---

### H3. §5.4 FR-to-verification traceability table is degenerate; §9.2 Q2 evidence is fabricated

**Location:** `section-5.md` §5.4 + `section-9-approval-checklist.md` §9.2 row Q2.

**Issue:** §5.4 publishes **six example rows** out of 82 FRs and explicitly says "full table to land at IN REVIEW". §9.2 Q2 — "Every FR-EVT-### row resolves to a §5.x verification mechanism" — points its evidence at "section-5.md §5.4 traceability table (full table generated at IN REVIEW commit; Stage 0 degenerate rows acknowledged per §5.4 last paragraph)." This is exactly the **fabricated checklist value** trap CLAUDE.md OPEN ISSUES calls out from the Decision Tree #8 history: an approval-checklist row asserting completeness against an incomplete artifact.

**Impact:** §9.2 Q2 cannot honestly be ticked at DRAFT or IN REVIEW under the current §5.4 state. A PASS 2 reviewer running the §5.4 grep will find 6 rows / 82 FRs and the gate fails — yet §9.4 says "DRAFT → IN REVIEW: All §9.1 / §9.2 rows pass". The author should not advance to IN REVIEW under the current §5.4.

**Recommended fix:** Either (a) author the full 82-row traceability table in §5.4 now (mechanical work — each row already has a `Verification` column in §2.2.1 that can be lifted directly), or (b) explicitly weaken §9.2 Q2 to "every FR-EVT-### row has a `Verification` column populated in §2.2.1; the full §5.4 traceability table with tooling pins and artifact paths lands at IN REVIEW commit". Option (a) is the honest fix and matches the rigor CLAUDE.md demands. Note that Spec #20 §5.5 acknowledged a "degenerate row" pattern — but it published the FULL table with each row visibly degenerate, not six rows out of 82.

---

## Findings — MEDIUM severity (8)

### M1. `[CROSS-PENDING]` is not a sanctioned constant tag

**Location:** `section-1.md` §1.4 (`DOMAIN_TAG_EVENT_LEDGER`); `section-3.md` §3.10 row; `section-8.md` §8.4 + §8.3.4; KD-2 in §1.3.

**Issue:** CLAUDE.md "Constant Tags" defines exactly five tags: `[GT]`, `[EST]`, `[FIXED]`, `[DERIVED]`, `[CROSS]`. Spec #17 introduces `[CROSS-PENDING]` as a sixth, with promotion semantics ("promoted to `[CROSS]` at #16 approval"). The intent is reasonable, but the vocabulary breach is not authorised by CLAUDE.md and creates a precedent.

**Fix options:**
1. Use `[CROSS]` with an explicit `(TBD-NORMATIVE: numeric value pending #16 §3.4 patch per ERR-017-001)` qualifier — keeps the tag taxonomy intact.
2. Get `[CROSS-PENDING]` added to CLAUDE.md "Constant Tags" table with a normative definition. Coordinate via a separate non-behavioural CLAUDE.md patch (low scope; broadly applicable to any spec that imports a not-yet-allocated cross-spec constant).

Recommendation: Option 2 — the pattern will recur for any cross-spec constant blocked on an upstream `IN PROGRESS` spec. ERR-017-001 already documents the back-prop; expanding CLAUDE.md once is cheaper than re-litigating per spec.

### M2. `[TBD-CITE]` qualifier escapes §9.2 Q11 audit rule

**Locations:**
- `section-2.md` §2.2 FR-EVT-026 — "writes a crash dump (#16 §3.10 failure-mode table, `[TBD-CITE]`)".
- `section-3.md` §3.2.5 — "`[TBD-CITE: tick-fail / crash-dump path; provisional anchor #16 §3.10 failure-mode table]`".
- `section-3.md` §3.6.1 — "Caller is responsible for crash handling per `[TBD-CITE: tick-fail path; provisional anchor #16 §3.10 failure-mode table]`."
- `section-9-approval-checklist.md` §9.2 Q11 — "Every `#16 §x.x.x` and `#19 §x.x` citation in this spec carries the `TBD-NORMATIVE` qualifier."

**Issue:** Q11's audit grep only catches `TBD-NORMATIVE`. Three citations of `#16 §3.10` use `[TBD-CITE]` instead and would slip past the audit while still being unresolved citations to an `IN PROGRESS` spec.

**Recommended fix:** Either (a) replace all `[TBD-CITE]` instances with `TBD-NORMATIVE` (semantically equivalent — both flag "cited subsection not yet approved"), or (b) extend §9.2 Q11 to require "`TBD-NORMATIVE` OR `[TBD-CITE]` qualifier" in the grep and add an inventory row to §8.1.2. Option (a) is the simplest path to a single audit vocabulary.

### M3. Second-order publish `producingPhaseIndex` / `subsystemOrdinal` derivation unspecified

**Location:** `section-3.md` §3.2.4 "Second-order publishes from inside the same-tick `Events`-phase dispatch (§3.2.5) reuse the `Events`-phase counter (itself fresh per tick), preserving uniqueness under BFS dispatch."

**Issue:** A second-order publish needs five sort-key components (FM-017-002): `producingPhaseIndex`, `subsystemOrdinal`, `entityId`, `eventTypeOrdinal`, `intraPhaseDrawIndex`. §3.2.4 specifies the counter scope but not:
- `producingPhaseIndex` — is it set to `Events`, or inherited from the publishing handler's first-order producing phase?
- `subsystemOrdinal` — is it the **handler's** subsystem (the dispatcher subsystem is `EventBus`) or the originally-publishing producer's subsystem? §3.2.4 says "the per-tick-per-producingPhase counter" but is silent on subsystem attribution at BFS depth ≥ 1.
- `entityId` — handlers don't necessarily have a single canonical entity; some handle aggregates.

§4.4.4 explicitly enumerates the producing phases as `[AI/AI_NoOp, Physics, Resolve]` — `Events` is **not** listed there, but §3.2.4 implies it as a producing phase for second-order publishes.

**Impact:** Replay golden bytes depend on these sort-tuple values. Without normative resolution, two implementations of `EventBus` could diverge while both satisfy the rule statements.

**Recommended fix:** Add §3.2.4.x "Second-order sort-tuple attribution" specifying:
- `producingPhaseIndex = phaseIndex(Events)` (the BFS level happens inside `Events` dispatch).
- `subsystemOrdinal = handler.subsystemOrdinal` (the subsystem currently executing).
- `entityId` from the secondary event's payload (per existing §3.2.4 component definition; no change).
- Add `Events` to §4.4.4's per-phase `intraPhaseDrawIndex` reset enumeration; specify that the `Events`-phase counter is reset at `DrainTick` entry (not per BFS level).

### M4. `producerSubsystem` (§2.4.1 header field) vs `subsystemOrdinal` (§3.2.4 sort-tuple component) naming

**Locations:** §2.4.1 header schema (`producerSubsystem: ushort`); §3.2.4 sort key (`subsystemOrdinal`); Appendix B header decode tables ("`producerSubsystem` (`0x0006`)").

**Issue:** Two names for the same byte field. §3.2.4 says "`subsystemOrdinal` — assigned per #16 §3.1.1 `TBD-NORMATIVE` ordering rules" — that's the header field. Pick one name and use it everywhere.

**Recommended fix:** Rename §2.4.1 / Appendix B uses to `subsystemOrdinal` for consistency with the sort-key component (which is what §3.2.4 says it is). Update §3.2.4, §2.4.1 header table, §3.1.1, Appendix B tables, and §6.5.1 trace channel column header ("producer").

### M5. `[StructLayout(LayoutKind.Sequential)]` without `Pack = 1` and FR-EVT-002 ambiguity

**Location:** `section-2.md` §2.4.1 ("Pack = 1 is NOT required ... the serializer is the only authoritative source of on-disk and digest bytes") vs FR-EVT-002 ("Each event struct **begins with** the fixed 12-byte header").

**Issue:** FR-EVT-002 reads naturally as an in-memory layout constraint ("the struct starts with these 12 bytes"). The §2.4.1 elaboration says in-memory layout is permitted to differ from canonical layout, and the serializer is authoritative. With `Sequential` and 64-bit fields appearing later, the C# layout could insert 4 bytes of padding between `intraPhaseDrawIndex` (offset 10) and the next field if the next field is `long`/`ulong`/`Vector3` (12 bytes; first `float` at offset 12 → no padding, but a `ulong` at offset 12 → padding to offset 16). FR-EVT-002 does NOT bind that.

**Impact:** A reader who takes FR-EVT-002 literally believes `MemoryMarshal.AsBytes(in evt)` yields a layout that starts with the 12-byte header followed by payload — which is true for `Sequential` IF and only if no payload field forces alignment padding. The spec needs to either (a) require `Pack = 1` (and accept the alignment cost on certain platforms) OR (b) restate FR-EVT-002 as "the **canonical serialized layout** of each event struct begins with the 12-byte header" and remove any implication that in-memory bytes match.

**Recommended fix:** Restate FR-EVT-002 in §2.2.1 to: "The canonical serialized layout of each event begins with the fixed 12-byte header (per §2.4.1); the in-memory C# struct layout follows `[StructLayout(LayoutKind.Sequential)]` without `Pack = 1`, and the §3.4.2 `SerializeCanonical` routine is the sole authoritative source of on-disk and digest bytes." Aligns §2.4.1 elaboration with the FR rule text.

### M6. Producer-phase change in §3.7.1 misses back-propagation to #16 §3.6.1 WriteSet

**Location:** `section-3.md` §3.7.1 registry row evolution rules — "Producer phase change | Yes (registry-row update only) | … new producer phase must still publish only Tier A/B from `Events`-phase WriteSet at drain time."

**Issue:** Tier A/B events appear in the `Events`-phase WriteSet by virtue of being drained there. But the **producing-phase** for a Tier A event (e.g., `BallContactEvent` produced in `Physics`) is reflected in #16 §3.6.1's phase WriteSet table — that table records which phase **enqueues** what. Changing a Tier A event's producer phase therefore requires a #16 §3.6.1 WriteSet table update, not just an Appendix A row edit. Spec #17 §3.7.1 currently presents it as a unilateral registry change.

**Recommended fix:** Add to §3.7.1 row: "If the new producer phase differs from the old one, a coordinated #16 §3.6.1 WriteSet table back-prop is required (parallel to ERR-017-001 / `DOMAIN_TAG_EVENT_LEDGER`). Filed in `spec-error-log.md` at the time the change is proposed." Also tighten §3.4.2 sort-key replay-stability: changing producer-phase shifts FM-017-002 tuple component 1 and breaks G1 golden — the registry version row should be retained for replay-corpus compatibility, or the change should mint a new ordinal under V5.

### M7. `Subscribe<T>` boot-time delegate allocation and FR-EVT-051 array allocation are not silent

**Location:** `section-4.md` §4.3.1 + §4.3.2; `section-6.md` §6.2 row `Subscribe<T> (boot)`.

**Issue:** §6.2 row says boot `Subscribe<T>` allocates `O(handler-count)` bytes one-time and is "off hot path" — true. But §6.2 doesn't acknowledge that **each registration** also allocates an `EventHandler<T>` delegate instance (the method group → delegate conversion). C# method-group caches changed across compiler versions; without a Spec #20 / Spec #18 pinned compiler/runtime, delegate allocation is not guaranteed to be a single GC-heap object per handler. Tier C runtime registration (FR-EVT-022) inherits the same property — but during a match.

**Impact:** Low — Tier C registration is described as "UI and VFX subsystems use this surface", not gameplay code. But §6.2 explicitly markets the entire publish/subscribe surface as zero-allocation; the runtime Tier C `Subscribe` row says "Bounded one-time per subscriber" which dodges the delegate cost.

**Recommended fix:** Add an explicit line to §6.2 / §3.5.2 acknowledging the delegate-instance allocation at registration, with a Spec #18 pin (D1) for the compiler/runtime guaranteeing single-allocation method-group conversion. Document that runtime Tier C `Subscribe` calls happen during loading screens / scene transitions, never during the simulation tick.

### M8. §3.10 `[GT]` tag is overloaded for design-fixed (non-tunable) constants

**Locations:** §3.10 rows for `EVENT_TYPE_ORDINAL_WIDTH = 1 byte`, `PAYLOAD_VERSION_WIDTH = 1 byte`, `ERR_EVT_QUEUE_OVERFLOW = 0x1701`, `ERR_EVT_TIER_MISMATCH`, `ERR_EVT_ORDINAL_UNKNOWN`, `ERR_EVT_VERSION_INCOMPATIBLE`.

**Issue:** CLAUDE.md "Constant Tags" defines `[GT]` as "Designer sets value; must live in tunable config". A designer cannot tune `EVENT_TYPE_ORDINAL_WIDTH` from 1 byte to 2 bytes at runtime — that would invalidate every replay corpus and every cross-process wire frame. Similarly, error-code numeric values are not gameplay-tunable; changing `0x1701` after publication breaks crash-dump triage and any external log analysis tooling.

The vocabulary doesn't cleanly cover "design-fixed but not physics-derived". `[FIXED]` is described in CLAUDE.md as "Fixed / physical law; Derived from physics" — non-physics constants don't fit.

**Recommended fix:** Either (a) accept that `[GT]` over-covers "designer-set" to include "design-time-set, not runtime-tunable" and add a clarifying note in §3.10 / §8.4 distinguishing the two sub-classes, or (b) coordinate a CLAUDE.md "Constant Tags" expansion to add a `[DESIGN]` tag (parallel to `[FIXED]` but not physics-derived). Option (a) is consistent with current Spec #20 usage; option (b) is cleaner taxonomy.

---

## Findings — LOW severity (9)

### L1. §6.3.1 worst-case Tier A AI math is fuzzy

**Location:** `section-6.md` §6.3.1 "AI events (one per player) — ≤ 22 (11 per side; one of each type)".

**Issue:** "One of each type" is unclear given §3.3.1 forward-references `PressTriggeredEvent`, `MarkAssignedEvent`, `RunCalledEvent` — three types, not two. 11 players × 2 sides = 22 (one event per player) OR 11 players × 3 types = 33 if each player publishes one of each type. The "Margin ×2 → 44 worst case" suggests the first reading; the §6.3 budget therefore tacitly assumes one AI event per player per stride tick — which isn't a stated invariant.

**Recommended fix:** State "one AI event per player per stride tick" explicitly as an FR or as a §6.3.1 sentence ("worst-case assumes ≤ 1 AI event per agent per stride tick; per-event-type aggregation lives in subscribers per §3.3.4"). Otherwise the ×2 margin is the only thing standing between the design ceiling and reality.

### L2. §6.3.1 first-order ceiling depends on yet-to-be-seeded AI events

**Location:** `section-6.md` §6.3.1 derivation; §3.3.1 status column.

**Issue:** The Aggregate per-tick first-order Tier A ceiling of ≤ 64 includes AI-cadence rows that §3.3.1 marks as "future — populated at #13/#14 IN REVIEW". The `EVENT_QUEUE_CAPACITY = 1024` derivation therefore depends on speculative future events.

**Recommended fix:** Either (a) explicitly mark §6.3 ceiling as **provisional** pending #13–#15 registry rows, with a re-tuning trigger at #13 IN REVIEW commit (D8 already names §6.3.4 re-tuning broadly; tighten to a specific upstream-spec trigger), or (b) redo the math against currently-seeded events only and add headroom for the future AI rows separately.

### L3. `FR-EVT-021` reuses `ERR_EVT_TIER_MISMATCH` for a lifecycle violation

**Location:** §2.2 FR-EVT-021 ("Runtime register/unregister of Tier A/B subscribers post-init MUST raise `ERR_EVT_TIER_MISMATCH`") + §2.5 `ERR_EVT_TIER_MISMATCH` mnemonic.

**Issue:** The error mnemonic is "TIER_MISMATCH" but FR-EVT-021 fires it for a **lifecycle** violation (subscriber registered after boot, even when the tier matches). The semantically correct code would be `ERR_EVT_REGISTRATION_AFTER_BOOT` or similar. Overloading TIER_MISMATCH muddies crash-dump triage.

**Recommended fix:** Mint a separate code `ERR_EVT_REGISTRATION_PHASE` (or similar) at `0x1705` in the reserved `0x17NN` block. Update §2.5, §3.10, §6.5.1 trace channel registry, EC-017-005.

### L4. §3.5.4 banned-API list bans `foreach`-style enumeration too broadly

**Location:** `section-3.md` §3.5.4 — "`IEnumerable<T>` `foreach` over a reference enumerator."; §3.5.2 — "Subscriber-list iteration uses an indexed `for` loop over the pre-allocated array; no `IEnumerable`, no `foreach` over a reference enumerator."

**Issue:** In C#, `foreach` over a `T[]` array uses the array indexer and is allocation-free; only `foreach` over `IEnumerable<T>` allocates an enumerator. The intent is clear, but the wording reads as forbidding `foreach` outright. Likely to be enforced too strictly by Spec #20 lint authors.

**Recommended fix:** Reword to "`foreach` over a type that implements `IEnumerable<T>` (the compiler emits an allocating `GetEnumerator()` call); `foreach` over a fixed-size array or `Span<T>` is permitted because the compiler emits indexed access." Cross-reference Spec #20 §3.x rule wording.

### L5. `EventHandler<T>` delegate vs §3.5.4 ban on `Action<…>` / `Func<…>` needs clarifying language

**Location:** §3.2.2 `EventHandler<T>` delegate; §3.5.4 banned APIs ("`Action<…>` / `Func<…>` (delegate types that box value-type captures)").

**Issue:** `EventHandler<T>` IS a delegate type. The reader has to infer that the rule against `Action`/`Func` is really about (a) closure capture allocations and (b) value-type boxing on T param. A custom `delegate void EventHandler<T>(in T evt) where T : struct;` avoids both — but the spec doesn't say *why* it's exempt.

**Recommended fix:** Reword §3.5.4 banned-API row to: "`Action<…>` / `Func<…>` with **value-type generic arguments** (cause boxing on each invocation). Custom struct-ref delegates such as `EventHandler<T>` (taking `in T evt`) avoid boxing because `T : struct` is constrained at the call site." This makes the exemption rationale explicit.

### L6. Marker-interface multi-implementation hazard (`struct FooEvent : IEventA, IEventC`)

**Location:** `section-4.md` §4.2.1; `section-3.md` §3.1.3.

**Issue:** Marker interfaces `IEventA`, `IEventB`, `IEventC` are public; a struct can implement multiple at once. If `struct FooEvent : IEventA, IEventC`, both `Publish<T>` overloads are eligible — the C# overload-resolution behavior depends on the constraint set and may be ambiguous or favour one path silently.

**Recommended fix:** Add an FR / Spec #20 lint forbidding multi-tier-marker implementation on a single struct. Reflect in Appendix A registry validator.

### L7. Appendix A `Producer phase` column semantics for Tier C are not declared normative

**Location:** Appendix A §A.1 — Tier C rows have `Producer phase` populated (`VfxImpactCue` → `Resolve`, `UiNotificationCue` → `Resolve`, `TickHeartbeatEvent` → `Snapshot`).

**Issue:** §3.2.1 says Tier C publish has no phase restriction. So `Producer phase` for Tier C events is descriptive — but the registry-row consumers (§3.6.2 drop predicate, §4.4 phase scheduler) don't actually use Tier C's producer-phase column. Whether the column is normative or informational is not stated.

**Recommended fix:** Add a note to §A.1 schema describing the Tier C semantics: "For Tier C rows, `Producer phase` is the **typical** producing phase (informational; used for telemetry attribution in §6.5.1 trace channels). Tier C publish is permitted from any phase per §3.2.1." Also harmonises H2 ambiguity for `TickHeartbeatEvent`.

### L8. §9.1 C9 omits §1 / §2 / §9 from "all sections include Version History" coverage

**Location:** `section-9-approval-checklist.md` §9.1 C9 — "All §3 / §4 / §5 / §6 / §7 / §8 sections include a Version History sub-section."

**Issue:** §1, §2, and §9 also have Version History sub-sections (verified by reading those files). C9's coverage list is incomplete.

**Recommended fix:** Change C9 to "All section files (§1 … §9 + appendices) include a Version History sub-section." Evidence path: "Every `section-*.md`'s `§X.Y Version History`."

### L9. §3.4.5 / §8.2 cite-precision guard is implicit on `[TBD-CITE]` instances

**Location:** §3.4.5 + §8.2 verification notes.

**Issue:** §3.4.5 mandates a re-grep of `#16 §x.x.x` citations at draft time. As noted in M2, `[TBD-CITE]` instances at FR-EVT-026, §3.2.5, and §3.6.1 reference `#16 §3.10` but use the alternate qualifier. The cite-precision guard's grep pattern (implicit) targets `TBD-NORMATIVE`-tagged citations only.

**Recommended fix:** Closed by the M2 fix (unify tag vocabulary). Add explicit grep pattern to §3.4.5 ("`grep -E '#16 §[0-9.]+ (TBD-NORMATIVE|\\[TBD-CITE\\])' docs/specs/event-system/`") so the guard is reproducible.

---

## Cross-Cutting Observations

- **`outline.md` and `outline-detailed.md` reference status.** The section files frequently cite the outlines for rationale (KD-1 through KD-12 binding text). After section-file authoring is complete, the outlines arguably should be retired or re-positioned as "background / rationale only" — otherwise the outline-detailed.md becomes a parallel normative source that future readers need to audit for drift. Suggest §1.3 KD table claim that rationale "lives in `outline-detailed.md`" be retired in favour of inlining KD rationale in the relevant §3 / §4 / §5 subsections (current practice in Spec #20's KD treatment).
- **No deterministic-replay-failure mode for the BFS-depth path.** EC-017-006 records `MAX_EVENT_DISPATCH_DEPTH` overflow as a hard fail, but if a Tier A handler at depth 7 publishes a Tier A event triggering a different code path in subsequent ticks, replay would diverge silently from the original. Worth a property test (P4 — "BFS depth distribution is stable across replay of the same seed"). Cross-link to Spec #19 §3.4.
- **Spec-error-log.md ERR-017-001 row.** Verified the citation exists structurally in section files; not verified that the actual `spec-error-log.md` file row matches (I didn't read that file). Reviewer should grep the log at IN REVIEW commit.

---

## Recommended Resolution Plan (author-side)

1. **Address H1, H2, H3 before any IN REVIEW request.** None require external dependencies — all can be resolved by editing the section files.
2. **Batch M1, M2, M8 as a CLAUDE.md / spec-error-log coordination patch.** Three findings touch the tag taxonomy and audit vocabulary; resolving once produces a stable foundation for future specs.
3. **M3 / M4 / M5 / M6 / M7 are localised edits.** Author can apply directly to §2 / §3 / §4 / §6.
4. **Low-severity items** can be batched into a single follow-up commit; none individually block IN REVIEW.
5. **Open a PASS 2 review window** once H/M findings are resolved. The PASS 2 reviewer should re-run §3.4.5's cite-precision grep and §9.2 Q-row evidence verification end-to-end.

---

## Version History

| Version | Date         | Author      | Notes                                                                 |
|---------|--------------|-------------|-----------------------------------------------------------------------|
| 0.1     | May 13, 2026 | Claude Code | Initial adversarial critique against v0.1 section files. 3 H / 8 M / 9 L findings; resolution plan published. |
