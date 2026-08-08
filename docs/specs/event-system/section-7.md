# Event System Specification #17 — Section 7: Future Extensions

**Created:** May 13, 2026
**Last Updated:** May 13, 2026
**Version:** 1.0.1
**Status:** DRAFT

> Section heading order follows `outline-detailed.md` v1.1
> §"SECTION 7" (Stage 0+1 → Stage 1 → Stage 5+ → Permanent Exclusions
> → Deferred Decisions Tracker), superseding the v0.0 stub.

---

## 7.1 Stage 0+1 Transition Deliverables

These items activate when the project transitions from
specification phase (Stage 0) to code phase (Stage 1). Each is a
contract written here in Stage 0 normative form; enforcement
begins at the first matching code commit per KD-12.

- **`src/event-system/` initial implementation.** EventBus,
  EventLedger, CosmeticChannel, EventRegistry, EventConstants
  (per §4.1).
- **Appendix A registry generated as `EventRegistry.cs`.** Build-
  step generator reads `appendices.md` Appendix A and emits the
  C# registry tables.
- **Per-publish allocation assertion test.** `Assert.AllocatedBytes(0)`
  on every overload (FR-EVT-048).
- **Golden ledger fixture for 60-second scripted scenario.**
  `tests/data/event-system/golden/g1-phase-digest-60s.fixture`
  (§5.3.2).
- **KD-12 stage-gated FRs flipped to "active".** §5.2 stage-gated
  activation table is updated atomically with the first code
  commit.
- **Trace channel verbosity defaults pinned (D6).**

## 7.2 Stage 1 Deliverables

These items defer past first-code-commit but are still Stage 1
deliverables before any production-quality release:

- **Replay-event reader (`IReplayEventReader`)** — interface
  declared once the replay tool's consumer side is specified
  (CLAUDE.md "Interface Design Principle"). Stage 1+ activation
  per KD-12 / FR-EVT-066 / FR-EVT-077.
- **Per-tier dashboards** for event-rate monitoring (uses
  §6.5 trace channels).
- **Stage 1 second-order dispatch profile** — validates the
  §3.2.5 BFS depth claim against realistic gameplay. If sustained
  depth exceeds 4, the `[GT]` constant `MAX_EVENT_DISPATCH_DEPTH`
  is revisited (D7 candidate).
- **First Stage 1 re-tuning of `[GT]` constants** based on
  microbenchmark output (§6.3.4).

## 7.3 Stage 5+ Extensions (KD-10 mechanics)

Per CLAUDE.md "Fixed64 stage scope decision", cross-platform /
multiplayer parity is Stage 5+. Spec #17 currently commits only to
the "do not preclude" stance:

- **Two-byte `eventTypeOrdinal` expansion** if registry exceeds
  256 rows. Currently allocated through `0x0B` (Spec #17 v1.0
  seeds), 244 free at Stage 0. Trigger criterion per D5 (§7.5).
- **Wire-format design.** Per-event framing, compression,
  acknowledgement semantics. Out of scope at Stage 0. The
  underlying `readonly struct` layout is already wire-compatible
  by KD-10 / FR-EVT-067 / FR-EVT-068.
- **Networked event multiplexing.** Which Tier A events are
  authoritative-server-only vs client-replicable. Out of scope at
  Stage 0.
- **Lossy Tier C transport** (UDP-style) for cosmetic cues over
  the network. Out of scope at Stage 0; FR-EVT-070.
- **Fixed64 (Spec #9) re-verification** of event payload
  arithmetic fields when Spec #9 migrates the engine off `float`.
  Stage 5+. FR-EVT-072.
- **Tier B activation.** No Stage 0 Tier B events; the tier
  vocabulary and `IEventB` marker are kept normative to avoid
  the silent Stage 5+ tier-collapse hazard (KD-3 rationale).

## 7.4 Permanent Exclusions

Decisions intentionally never permitted by this spec, regardless
of stage:

| Exclusion | Rationale |
|-----------|-----------|
| "Soft drop" policy on Tier A/B paths | KD-7; queue-depth-conditional drops are non-reproducible across replay. |
| Runtime register/unregister of Tier A/B subscribers | §3.2.2 anti-pattern; introduces non-deterministic subscriber set at dispatch time. |
| Class-typed events | KD-8 zero-allocation rule (class instantiation allocates on GC heap). |
| Cross-tick aggregation on the publisher side | §3.3.4 anti-pattern; the ledger is the source of truth, aggregation is a subscriber concern. |
| Tier-mismatch subscription from authoritative code | KD-3; would force Tier C non-authoritative data onto authoritative paths and break determinism. |
| `eventTypeOrdinal` reuse after deprecation | KD-9; replay corpus compatibility requires the namespace to be append-only. |

## 7.5 Deferred Decisions Tracker

Pending decisions, owned tracker per item:

| ID | Decision | Stage | Owner / dependency |
|----|----------|-------|-------------------|
| D1 | Microbenchmark tool pin (BenchmarkDotNet vs alternative) | Stage 0+1 | Spec #18 pin. |
| D2 | Trace-channel binary format | Stage 0+1 | Spec #16 §8 `TBD-NORMATIVE`. |
| D3 | Replay-event-reader interface (`IReplayEventReader`) — both sides | Stage 1 | Replay tool consumer-side spec (deferred per CLAUDE.md "Interface Design Principle"). |
| D4 | Multiplayer wire format | Stage 5+ | KD-10; Stage 5 multiplayer decision. |
| D5 | Ordinal-width expansion trigger date | Stage 1+ | Registry-row count monitor; trigger when count approaches 200 (FR-EVT-071). |
| D6 | Per-event trace-channel verbosity defaults | Stage 0+1 | §6.5.1. |
| D7 | Tier C overflow subscriber-array sizing (runtime register growth budget) | Stage 0+1 | §4.3.2 first measurements. |
| D8 | Re-tuning of `[GT]` constants (`EVENT_QUEUE_CAPACITY`, `COSMETIC_PER_TICK_PUBLICATION_BUDGET`, `MAX_EVENT_DISPATCH_DEPTH`) | Stage 0+1 | §6.3.4 microbenchmark output. |
| D9 | `DOMAIN_TAG_EVENT_LEDGER` numeric value | RESOLVED May 14, 2026 (`0x15`) | Allocated in #16 §3.4 v1.0.1; ERR-017-001 RESOLVED; tag promoted `[CROSS-PENDING]` → `[CROSS]` in #17 §3.10 / §3.4.2 v1.0.1. |

## 7.6 Version History

| Version | Date         | Author      | Notes                                                                 |
|---------|--------------|-------------|-----------------------------------------------------------------------|
| 0.1     | May 13, 2026 | Claude Code | Initial section-file draft from `outline-detailed.md` v1.1. Six permanent exclusions, nine deferred decisions (D1–D9) registered. Section heading order superseded the v0.0 stub. |
| 1.0.1   | May 15, 2026 | Claude Code | Patch revision (no behavioral change). D9 marked RESOLVED with `DOMAIN_TAG_EVENT_LEDGER = 0x15` per #16 §3.4 v1.0.1 (ERR-017-001 RESOLVED May 14, 2026). |
