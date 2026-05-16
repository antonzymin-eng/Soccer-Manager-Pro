# Positioning AI Specification #12 — Section 8: References and Citations

**Created:** May 15, 2026
**Last Updated:** May 16, 2026 (v0.2 — PASS-1 adversarial fix pass)
**Version:** 0.2
**Status:** DRAFT

---

## 8.1 Cross-Spec References

All references are grep-verifiable against the cited section files
in `docs/specs/`. Each row identifies what #12 reads from the cited
section.

| Spec | Section(s) | What #12 binds |
|---|---|---|
| #1 Ball Physics | §1.2 | Coordinate system: corner origin, X = 0–105 m, Y = 0–68 m, Z = height |
| #1 Ball Physics | §3.x | Ball state schema (position, velocity) |
| #2 Agent Movement | §2.5 (XC-002-001) | EntityId no-reuse cross-spec constraint |
| #2 Agent Movement | §3.1 | Hysteresis pattern (dwell-time + dead-zone) — bound by KD-8 |
| #7 Perception System | §3.7 | Per-agent perception snapshot schema |
| #7 Perception System | §3.9 | Possession state |
| #7 Perception System | §3.10 | Active/inactive flag for FR-PA-036 |
| #8 Decision Tree | §1.7.3 (XC-008-001) | EntityId no-reuse |
| #8 Decision Tree | §2.2.6 (L688–721) | `TacticalContext` schema (per-agent; field set FROZEN at Stage 0); `Stage0Default(Vector2)` is the **match-init-only** factory — NOT called per tick (AR-S1-04) |
| #8 Decision Tree | §3.1.7 | `MOVE_TO_POSITION` action; per-tick orchestrator writes `ctx.FormationSlot` field directly |
| #8 Decision Tree | §3.2.6 | `MOVE_TO_POSITION` utility scoring |
| #16 Deterministic Simulation | §3.2 | Authoritative simulation state definition |
| #16 Deterministic Simulation | §3.2.5 | EntityId-sorted iteration order |
| #16 Deterministic Simulation | §3.4 | Domain-tag registry — target of `ERR-012-001` |
| #16 Deterministic Simulation | §5 | Determinism regression scenarios — bound by §5.4 |
| #16 Deterministic Simulation | §6.2 | Per-tick digest scope for tactical-AI outputs |
| #17 Event System | §3 (schema only) | No channels produced or consumed at Stage 0 (KD-10) |
| #18 Performance Optimization Strategy | §3.7 | Zero-allocation hot-path discipline; `[HotPathAllocExempt]` attribute (declaration site #18 §3.7.5) |
| #18 Performance Optimization Strategy | §6 | Per-tick budget framework |
| #19 Testing Strategy & Framework | §3 | Test taxonomy — bound by §5.1 |
| #19 Testing Strategy & Framework | §4 | FR-traceability framework — bound by §5 |
| #20 Code Standards | §4.2 (FR-CS-025) | Single constant catalogue file |

## 8.2 CLAUDE.md Invariants Bound

| Invariant | Where bound in #12 |
|---|---|
| Corner-origin coordinate system | §1.7, §3.1 |
| Fatigue convention `0 = rested, 1 = fatigued` | §1.7, §3.5, FR-PA-016 |
| 10 Hz tactical / 60 Hz physics tick split | §1.7, §4.1, FR-PA-001 |
| Parameter-based physics (no type enums) | §1.7 — #12 outputs `Vector2`, not enum |
| Constant-tag policy | KD-12, §6.1, FR-PA-040 |
| Interface Design Principle | KD-4..6, KD-11, §7 — declared bindings only |
| Stage 0 uses `float` (Fixed64 deferred to Stage 5+) | §4.6, §7.10 |
| State-snapshot determinism | §4.6 |
| EntityId no-reuse | §1.7 (via XC-002-001 / XC-008-001) |
| Spec Renumbering Cascades hazard | §8.3 (this section) — cross-refs verified by grep |

## 8.3 Typed Cross-Reference IDs

`XC-012-NNN` IDs allocated at section-file draft (this draft):

| ID | Subject | Authoritative spec / section |
|---|---|---|
| `XC-012-001` | EntityId no-reuse for #12 iteration order | #2 §2.5 (XC-002-001) and #8 §1.7.3 (XC-008-001) |
| `XC-012-002` | `TacticalContext.FormationSlot` producer-side write contract | #8 §2.2.6 |
| `XC-012-003` | `MOVE_TO_POSITION` consumer-side read contract | #8 §3.1.7, §3.2.6 |
| `XC-012-004` | `DOMAIN_TAG_POSITIONING_AI = 0x17` allocation request | #16 §3.4 (`ERR-012-001`; value shifted from 0x16 on May 16, 2026 after #10 took 0x16) |
| `XC-012-005` | Per-tick digest scope contribution (`formationSlot[22]` + `HysteresisState`) | #16 §6.2 |
| `XC-012-006` | EntityId-sorted iteration | #16 §3.2.5 |
| `XC-012-007` | Hysteresis-pattern reuse | #2 §3.1 |
| `XC-012-008` | Zero-allocation hot path | #18 §3.7 |
| `XC-012-009` | Single constant catalogue | #20 §4.2 (FR-CS-025) |

## 8.4 Spec Error Log Entries Filed

| ERR | Subject | Resolution |
|---|---|---|
| `ERR-012-001` | Phase B/C domain-tag block allocation `0x16…0x1B` for #10/#11/#12/#13/#14/#15 in #16 §3.4 | Pending lead-developer ratification; `[CROSS-PENDING]` until promoted to `[CROSS]` atomically across all six specs |
| `ERR-012-002` | Stale "Formation System (Spec #14)" reference at `decision-tree/section-3-1.md` L716 (current #14 is Defensive AI; Formation System is #12) | One-line patch request to #8 |
| `ERR-012-003` | Documentary anchor for `XC-012-001`..`XC-012-009` allocation (AR-S1-18) | Filed as informational; no remediation required |

## 8.5 Citation Verification

Per CLAUDE.md "Things That Have Gone Wrong Before" — Stale Spec
Numbers — every cross-spec reference in this document was
grep-checked at draft time against the cited spec's current section
files. Verification commands:

```
grep -rn "TacticalContext"       docs/specs/decision-tree/
grep -rn "FormationSlot"          docs/specs/decision-tree/
grep -rn "DOMAIN_TAG_"            docs/specs/deterministic-sim/
grep -rn "FR-CS-025"              docs/specs/code-standards/
grep -rn "FR-PO-"                 docs/specs/performance-optimization/
grep -rn "EntityId"               docs/specs/agent-movement/
```

These commands MUST be re-run before §9 sign-off and any output
that changes the cited section numbers MUST be reconciled here.

## 8.6 Version History

| Version | Date | Author | Summary |
|---|---|---|---|
| 0.1 | May 15, 2026 | AI agent (claude/draft-positional-ai-specs-MOejb) | Initial section-file draft from `outline-detailed.md` v1.2. `XC-012-001`..`XC-012-009` allocated. `ERR-012-001` and `ERR-012-002` referenced. |
| 0.2 | May 16, 2026 | AI agent (claude/review-positional-ai-specs-v4rmD) | PASS-1 adversarial fix pass. AR-S1-04 §8.1 #8 row clarified `Stage0Default()` is match-init-only; per-tick path is direct field write. AR-S1-18 `ERR-012-003` filed as documentary anchor for the `XC-012-NNN` allocation. |
