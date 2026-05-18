# Attacking AI Specification #15 — Section 8: References & Citation Audit

**Created:** May 17, 2026
**Last Updated:** May 17, 2026 (v0.1 — initial draft from `outline-detailed.md` v1.1)
**Version:** 0.1
**Status:** DRAFT
**Source:** `outline-detailed.md` v1.1 (May 17, 2026)

---

## 8.1 Cross-Spec Reference Register

All cross-spec citations in this spec. `XC-015-NNN` IDs are assigned
below. Grep-verified at section-file draft time against the cited files.

| XC-ID | Cited spec | Section | What is cited | Use in #15 |
|---|---|---|---|---|
| XC-015-001 | Ball Physics #1 | §1.2, Appendix C | Corner-origin coordinate system; `PITCH_LENGTH_M = 105.0m`; `PITCH_WIDTH_M = 68.0m`; `HALF_LINE_X = 52.5m` | §1.7, §3.4, §3.6, §3.7, §3.8, §6.1.1 |
| XC-015-002 | Agent Movement #2 | §2.5 (`XC-002-001`) | EntityId no-reuse constraint for the lifetime of a match | §1.7, §4.6, §4.7 |
| XC-015-003 | Agent Movement #2 | §3.1 | Dwell-time + dead-zone hysteresis pattern (assignment hysteresis binding) | §3.12, §6.1.8 |
| XC-015-004 | Decision Tree #8 | §1.7.3 (`XC-008-001`) | EntityId no-reuse (duplicate binding — also in #2 §2.5) | §1.7, §4.6 |
| XC-015-005 | Decision Tree #8 | §1.3.2 | "Multi-agent coordination" deferral row — names #12 and #13; #15 covered by implication (ERR-015-005 filed to add #15 explicitly) | §1.3.1, §1.8 |
| XC-015-006 | Decision Tree #8 | §2.2.6 | Amendment process for `TacticalContext` extensions (ERR-015-002 Option B: `AttackIntent[]?` field) | §4.5.1 |
| XC-015-007 | Decision Tree #8 | §3.1.7 | `MOVE_TO_POSITION` utility — consumed by #15's `runTargetPosition` at Stage 1 | §4.5.1 |
| XC-015-008 | Shot Mechanics #6 | §7 | xG model deferred to Stage 1+; dangerous-zone surrogate used at Stage 0 | §5.7 |
| XC-015-009 | Perception System #7 | §3.7–§3.10 | Perception snapshot schema: agent positions, ball position, ball carrier EntityId, `PlayerRole`, attribute lookups | §2.3, §4.4 |
| XC-015-010 | Positioning AI #12 | §2.2 | `formationSlot` struct fields: `lineMembership LineMembership`, `lateralPct float` (0–1), `role RoleId` | §2.3, §3.3, §3.4, §4.4 |
| XC-015-011 | Positioning AI #12 | §4.5 | `RunIntent` writer-layer declaration: Stage 1+ struct; #15 is the declared writer | §4.5.2 |
| XC-015-012 | Positioning AI #12 | §4.5.1 | `PositioningAI.GetPhase(TeamId)` Stage 1 accessor | §4.4 |
| XC-015-013 | Positioning AI #12 | §4.5.2 | `PositioningAI.GetLine(EntityId)` Stage 1 accessor (elevated from Stage 1+ via ERR-013-008 / May 17, 2026) | §3.3, §4.4 |
| XC-015-014 | Positioning AI #12 | FR-PA-048 | No interface produced against #15 at Stage 0 | §1.3.1, §4.5.2 |
| XC-015-015 | Pressing AI #13 | §2.2 (FR-PR-014) | Role partition confirms no RUNNER/SUPPORT_BALL overlap with PRIMARY_PRESS/COVER_SHADOW at Stage 1 | §1.6, KD-5 |
| XC-015-016 | Defensive AI #14 | FR-DA-013 | All-ZONAL directive when IN_POSSESSION — mutual exclusion with #15 confirmed | §3.1, KD-6, §1.6 |
| XC-015-017 | Defensive AI #14 | KD-8 | "#15 is in-possession; #14 is out-of-possession; mutually exclusive at team level" | §1.6, KD-6 |
| XC-015-018 | Defensive AI #14 | §7.4 | `MarkDirective.emergencyFlag` boundary hint for Stage 1+ transition acceleration | §7.3 |
| XC-015-019 | Deterministic Simulation #16 | §3.2, §3.2.5 | Authoritative simulation state definition; EntityId-ascending iteration rule | §4.6 |
| XC-015-020 | Deterministic Simulation #16 | §3.4 | `DeterministicRngService` domain-tag registry; ERR-015-001 filed for `DOMAIN_TAG_ATTACKING_AI = 0x1B` allocation | §4.6, §6.1.9 |
| XC-015-021 | Deterministic Simulation #16 | §5 | Determinism regression test framework | §5.4 |
| XC-015-022 | Deterministic Simulation #16 | §6.2 | Per-tick digest scope definition | §4.6 |
| XC-015-023 | Event System #17 | §3.10 | Channel registry schema; ERR-015-003/004 back-prop at Stage 1 | §7.2 |
| XC-015-024 | Performance Optimization #18 | §3.7 | Zero-allocation hot-path rule | §4.3, §6.5 |
| XC-015-025 | Performance Optimization #18 | §6 | Per-tick budget framework (ratify-not-override per KD-2 of #18) | §6.3 |
| XC-015-026 | Testing Strategy #19 | §3, §4 | Test taxonomy and FR-traceability framework | §5.1 |
| XC-015-027 | Code Standards #20 | §4.2 (FR-CS-025) | Single constant-catalogue file rule | §4.2, §6.1 |

---

## 8.2 CLAUDE.md Invariants Bound

The following CLAUDE.md invariants are cited by reference throughout this
spec and must never be restated (KD-1 cite-not-redefine):

| Invariant | Citation | Binding location in #15 |
|---|---|---|
| Corner-origin coordinate system (X = 0–105m, Y = 0–68m, Z = height, origin at pitch corner) | CLAUDE.md §"Coordinate System"; also #1 §1.2 (XC-015-001) | §1.7, §3.4, §3.6, §3.7 |
| Fatigue convention: `0.0 = fully rested`, `1.0 = fully fatigued` | CLAUDE.md §"Fatigue Convention" | §1.7, FR-AT-032 |
| Tactical loop: 10 Hz (100 ms/tick). Physics/render: 60 Hz (~16.67 ms) | CLAUDE.md §"Heartbeat Tick Rate" | §1.7, KD-2, FR-AT-001 |
| Parameter-Based Physics (No Type Enums) | CLAUDE.md §"Parameter-Based Physics" | KD-8, FR-AT-010, FR-AT-011 |
| Interface Design Principle | CLAUDE.md §"Interface Design Principle" | FR-AT-031, §4.5.1, §4.5.2, §7.3 |
| Constant-tag policy (`[GT]` / `[EST]` / `[FIXED]` / `[DERIVED]` / `[CROSS]` / `[CROSS-PENDING]`) | CLAUDE.md §"Constant Tags" | FR-AT-029, §6.1 |
| Zero-allocation hot path | CLAUDE.md (via #18 §3.7 XC-015-024) | §4.3, §6.5 |
| Deterministic RNG (no `System.Random`, no `DateTime.Now`) | CLAUDE.md §"When Writing Code" | §4.6, FR-AT-005 |

---

## 8.3 Cross-Spec Issues Filed at Section-File Draft

These back-propagation amendments and domain-tag allocations are
declared here so that they appear in `spec-error-log.md` tracking:

| ERR-ID | Target spec | Nature | Status | Section in #15 |
|---|---|---|---|---|
| ERR-015-001 | #16 §3.4 | Allocate `DOMAIN_TAG_ATTACKING_AI = 0x1B` in #16 §3.4 domain-tag registry. Follows ERR-012-001 Phase B/C block (`0x1B` = #15). | **OPEN** — `[CROSS-PENDING]` until #16 §3.4 patch lands | §4.6, §6.1.9, FR-AT-005 |
| ERR-015-002 | #8 §2.2.6, §3.1.7 | Add `TacticalContext.AttackIntent[]?` nullable field (Option B, mirrors `PressDirective?` precedent). #8 §3.1.7 `MOVE_TO_POSITION` reads `runTargetPosition` for RUNNER agents. | **OPEN** — filed, mechanism ratified in this spec; back-prop amendment text authored at Stage 1 | §4.5.1 |
| ERR-015-003 | #17 §3.10 | Register `ATTACK_RUN_STARTED` event channel in #17 channel registry. Stage 1 deliverable. | **OPEN** — Stage 1 deferred | §7.2 |
| ERR-015-004 | #17 §3.10 | Register `OVERLOAD_DECLARED` event channel in #17 channel registry. Stage 1 deliverable. | **OPEN** — Stage 1 deferred | §7.2 |
| ERR-015-005 | #8 §1.3.2 | Add "Attacking AI #15" explicitly to the multi-agent-coordination deferral paragraph alongside #12 and #13. One-token patch per ERR-012-002 / ERR-013-004 precedent. | **OPEN** — filed; back-prop text authored at Stage 1 | §1.3.1, XC-015-005 |

---

## 8.4 Typed Cross-Reference Summary

All `XC-015-NNN` identifiers assigned in this spec:

| ID Range | Count | Coverage |
|---|---|---|
| XC-015-001 | 1 | Ball Physics #1 coordinate constants |
| XC-015-002–003 | 2 | Agent Movement #2 (EntityId no-reuse + hysteresis pattern) |
| XC-015-004–007 | 4 | Decision Tree #8 (EntityId, deferral row, TacticalContext amendment, MOVE_TO_POSITION) |
| XC-015-008 | 1 | Shot Mechanics #6 (xG deferral) |
| XC-015-009 | 1 | Perception System #7 (perception snapshot) |
| XC-015-010–014 | 5 | Positioning AI #12 (formationSlot, RunIntent, GetPhase, GetLine, FR-PA-048) |
| XC-015-015 | 1 | Pressing AI #13 (role partition) |
| XC-015-016–018 | 3 | Defensive AI #14 (FR-DA-013, KD-8, emergencyFlag) |
| XC-015-019–022 | 4 | Deterministic Simulation #16 |
| XC-015-023 | 1 | Event System #17 |
| XC-015-024–025 | 2 | Performance Optimization #18 |
| XC-015-026 | 1 | Testing Strategy #19 |
| XC-015-027 | 1 | Code Standards #20 |
| **Total** | **27** | All upstream specs cited |

---

## 8.5 Academic / External References

No direct academic references are required for the Stage-0 algorithm
specification. The attacking movement model uses standard football
geometry and gameplay-tunable constants (`[GT]`); it does not derive
directly from published sports-science research.

If future section-file refinements or Stage-1+ extensions cite published
research on attacking movement coordination models, off-ball run timing,
or overload-zone geometry, DOIs MUST be verified before sign-off per
the ERR-005 fabricated-reference precedent (two fabricated references
in Heading #10 v0.1 were replaced with real equivalents in v0.2). DOI
verification format: `DOI: <doi>` with a URL-accessible link confirmed
before commit.

---

## 8.6 Version History

| Version | Date | Author | Summary |
|---|---|---|---|
| 0.1 | May 17, 2026 | AI agent (claude-sonnet-4-6 / draft-attacking-ai-spec) | Initial draft from `outline-detailed.md` v1.1. §8.1–§8.6 authored. 27 XC-015-NNN IDs assigned. ERR-015-001..005 filed. No academic references at Stage 0. |
