# Event System Specification #17 — Outline

## Purpose
Define a deterministic, scalable event architecture for gameplay subsystems and future multiplayer compatibility.

## Scope
Typed contracts, pub/sub semantics, allocation strategy, lifecycle/backpressure handling, and instrumentation.

## Section Plan
- Section 1 — Typed event contracts and ownership boundaries.
- Section 2 — Publish/subscribe semantics, ordering, immediate vs queued timing.
- Section 3 — Payload-size rules and allocation-free hot-loop paths.
- Section 4 — Lifecycle, backpressure handling, and dropped-event policy.
- Section 5 — Instrumentation (tracing, counters, debug replay hooks).
- Section 6 — Error handling, observability, and operational safeguards.
- Section 7 — Unit and integration tests for ordering and throughput.
- Section 8 — Compatibility expectations for future networking/multiplayer.
- Section 9 — Approval checklist.
- Appendices — Event taxonomy and versioning strategy.

---

## ADVERSARIAL REVIEW — May 6, 2026

> Reviewer: AI agent (claude/review-spec-sections-JQ8jy). Scope: this outline file
> measured against `CLAUDE.md`, the 9-section template, Deterministic Simulation
> #16 (IN PROGRESS), and approved upstream specs.
> Severity legend: **H** = blocks draft start; **M** = must resolve during draft;
> **L** = follow-up.

### Verified premises
- Spec #17 status in `SPEC_INDEX.md`: NOT STARTED.
- `ShotExecutedEvent` already promised by Shot Mechanics #6 §4.5 — Spec #17
  inherits a published event surface from an APPROVED upstream.
- Per CLAUDE.md, multiplayer / cross-platform parity is Stage 5+; Stage 0
  must "not preclude" but is not delivering networked event semantics.

### Findings

1. **[H] Missing metadata header.** Same gap as siblings. Add per Shot
   Mechanics #6 outline header.

2. **[H] Section plan deviates from CLAUDE.md template.** Backpressure /
   lifecycle in §4 is fine, but error handling in §6 sits where the
   template designates performance budgets, and "compatibility expectations"
   in §8 sits where the template designates references / citations. No
   references slot. Re-map.

3. **[H] Boundary with Deterministic Simulation #16 unstated.** Event
   ordering, immediate-vs-queued timing, and dropped-event policy are all
   determinism-critical. #16 §3 already mandates per-tick phase order and
   #16 §6.2 mandates per-phase digest scopes. Spec #17 must declare:
   - Are events part of the per-phase digest? (Yes per #16 §6.4 implies an
     event ledger digest.)
   - Are events allowed to cross phase boundaries within a tick? (No per
     #16 §3.2 deterministic ordering.)
   - Does the queued-vs-immediate distinction map onto #16 phases?
   Without this declaration, Spec #17 will recreate the determinism contract
   incompatibly.

4. **[H] Dropped-event policy conflicts with replay determinism.** §4
   "dropped-event policy" is a determinism hazard: any drop-decision based
   on transient backpressure (queue depth at time of publish) is non-
   reproducible across replay unless the queue depth itself is part of
   authoritative state. Outline must either ban drops in authoritative
   paths or define a deterministic drop predicate.

5. **[H] Existing event types not enumerated.** Spec #6 already defines
   `ShotExecutedEvent`; future specs in this batch will define
   `SaveAttemptedEvent`, `PressTriggeredEvent`, `MarkAssignedEvent`, etc.
   Outline does not list any concrete event. Without an inventory the §1
   "typed event contracts" section becomes abstract and the §9 Approval
   Checklist has nothing to verify against.

6. **[M] Allocation-free path policy underspecified.** §3 names the goal
   but not the constraint set. CLAUDE.md "When Writing Code" mandates
   struct-based zero-allocation architecture in the game loop. Outline
   should commit to: struct events, ref-passed handlers, no boxing, no
   per-publish allocations. Pre-commit avoids re-litigation at draft.

7. **[M] Multiplayer compatibility scoped wrong.** §8 reads as active
   spec target. CLAUDE.md "Fixed64 stage scope decision" placed cross-
   platform parity at Stage 5+. Stage 0 commitment should be limited to
   "do not preclude": stable wire-compatible struct layout, no engine-
   specific singletons in event types. Re-scope §8.

8. **[M] Versioning strategy in appendix only.** Event-contract versioning
   is a normative concern (every consumer will pin an event-struct version);
   it should be promoted to §1 or §2, not buried in appendices.

9. **[M] Tick-rate split unstated.** Tactical events fire on the 10 Hz
   loop; physics events on the 60 Hz loop. Per #16 §3.1 the canonical
   pipeline is `Input -> Intent -> AI -> Physics -> Resolve -> Events ->
   Snapshot`. Spec #17 must declare which subsystems publish in which
   phase.

10. **[M] No subscription-side authoritative-vs-cosmetic distinction.**
    #16 §1.2 tiers events implicitly: authoritative state-changing events
    (Tier A) vs cosmetic VFX/UI events (Tier C). Spec #17 must surface
    this distinction or downstream consumers will subscribe to Tier C
    streams from authoritative paths and break determinism.

11. **[L] No instrumentation budget cited.** §5 "tracing, counters" must
    fit within the #16 §8.2 instrumentation budget envelope. Cross-link.

12. **[L] Test ratios unstated.** §7 names ordering/throughput tests but
    not target counts. Compare to Shot Mechanics #6 §5.1 which lists
    explicit category counts.

### Recommended next steps
- Add full metadata header.
- Re-map Section Plan to CLAUDE.md 9-section template.
- Add upstream/downstream tables — at minimum, all approved-spec event
  surfaces (Shot #6 `ShotExecutedEvent`).
- Pre-commit determinism contracts via cross-reference to #16 §3 and §6.
- Re-scope §8 to "do not preclude" Stage 5+ networking.
