## CRITICAL ARCHITECTURAL RISKS

| ID | Risk | Severity | Resolution |
|----|------|----------|------------|
| AR-1 | Perception-DT evaluation order: if Perception and DT run in wrong order on a heartbeat tick, DT reads stale snapshot. Must be enforced at simulation loop level — not just documented. | **Critical** | KD-1 locks the order. Simulation orchestrator must enforce it. Add to §4 architectural notes. |
| AR-2 | Omniscience leak: if DT accidentally queries world state (e.g., direct AgentState.Position read) outside PerceptionSnapshot, epistemic model collapses. Silent bug — very hard to detect post-implementation. | **High** | Static analysis rule: DecisionTree.cs may not import WorldState namespace. Document in §4 and §9 checklist. |
| AR-3 | PerceptionSnapshot.VisibleOpponents array scope: it's a `ReadOnlySpan<PerceivedAgent>` — cannot be stored across ticks. DT must copy needed data in the same heartbeat. | **High** | Document in §3.6.1 and §5 integration tests (IT: DT does not cache span across ticks). |
| AR-4 | Composure noise magnitude: if noise range is too wide at Composure=1, DT produces pathological decisions (shoots from own half, passes backwards into opponent). Must be bounded and tested. | **Medium** | Clamp noise to max ±0.2 utility; verify in §3.3.1 derivation. Add balance test BAL-003. |
| AR-5 | PassRequest/ShotRequest field population errors: if DT populates fields incorrectly, execution systems silently produce bad physics. No automatic validation at the boundary. | **Medium** | Unit tests §5.5 verify all fields. Integration test verifies correct physics output emerges from DT-generated requests. |

---

## SCOPE ESTIMATE

| Section | Est. Pages | Est. Hours |
|---------|-----------|-----------|
| Section 1 | 4 | 4 |
| Section 2 | 5 | 5 |
| Section 3 | 12 | 16 |
| Section 4 | 5 | 5 |
| Section 5 | 6 | 8 |
| Section 6 | 3 | 3 |
| Section 7 | 3 | 2 |
| Section 8 | 2 | 3 |
| Section 9 | 2 | 2 |
| Review & corrections | — | 8–12 |
| **Total** | **~42** | **~54–60** |

---

## NEXT STEPS

1. **Lead developer reviews and approves this outline** — resolve all OQs and confirm KDs
2. **Log ERR-010** (spec numbering conflict in Shot Mechanics §1.1)
3. **Write Section 1** — Purpose, scope, KDs locked
4. **Write Section 2** — FRs, data structures, pipeline
5. **Write Section 3** — Technical specs (largest section; option generation, utility model, composure)
6. **Write Sections 4–9** — Architecture, testing, performance, future extensions, references, checklist

---

## VERSION HISTORY

| Version | Date | Changes |
|---------|------|---------|
| 1.0 | February 26, 2026, 11:45 PM PST | Initial draft |
| 1.1 | February 26, 2026 | Five weaknesses from v1.0 self-critique resolved: (1) OQ-1 resolved — direct method call from orchestrator, `ReceiveSnapshot()` interface defined in §3.6.1. (2) Utility formula vagueness addressed — preliminary math for all 7 action types added to §3.2.2, including formulas, attribute exponents, zone tables, and notation. (3) ~70% GT constant management formalised — §3.2.4 and §8.3 expanded with governance rules, tuning roadmap, and full constant breakdown (~80 constants). (4) Stage 0 known limitations made explicit — §1.6 now lists 6 named limitations with resolution stages; no longer a blank placeholder. (5) Adjacent-spec scope expectation warning added to §1.3 — implementers reading adjacent specs will not be surprised by what DT does not call at Stage 0. |

---

## CRITIQUE OF THIS OUTLINE

**Strengths:**
- Utility-based scoring is the right architecture for emergent tactical behaviour. It avoids hard-coded play patterns and is tunable without structural changes.
- Composure noise model is well-motivated: it creates natural variation between player quality tiers without arbitrary dice rolls.
- Interface boundary with Perception System is clean and resolves the deferred interface from Perception §4.5.3.
- Heartbeat evaluation order (Perception-first) resolves the KR-5 critical risk from Perception §7.7.
- OQ-1 is now resolved with rationale — snapshot delivery is a direct method call. Testability and ordering guarantees are explicit.
- Stage 0 known limitations are now explicit in §1.6, not buried or implied.
- Preliminary utility formulas give §3 a concrete starting point rather than requiring formula invention from scratch during section drafting.

**Remaining risks (not weaknesses — inherent to this specification):**
- The utility formula exponents and [GT] constants will require post-implementation tuning. This is expected, documented, and governed by §3.2.4. It is not a specification deficiency.
- The `TacticalContext` defaults at Stage 0 mean both teams behave identically. This is explicitly flagged in §1.6 as a Known Limitation. It is a deliberate Stage 0 constraint, not an oversight.
- Section 3 will be the longest and hardest section to write — the preliminary formulas in this outline must be numerically verified at attribute extremes before §3 can be signed off. The worked-example requirement in §3.2.2 notes makes this explicit.

---

**END OF OUTLINE**

*Decision Tree Specification #8 — Outline v1.1*  
*Tactical Director — Specification #8 of 20 | Stage 0: Physics Foundation*
