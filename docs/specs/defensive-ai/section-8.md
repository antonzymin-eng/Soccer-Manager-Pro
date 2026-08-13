# Defensive AI Specification #14 — Section 8: References, Citations, Cross-Reference Audit

**Created:** May 17, 2026
**Last Updated:** August 12, 2026 (v0.3 — KD-6 revised (`ERR-014-006`, wiring backlog W2): XC-014-004 and §8.2 invariant row 7 corrected — the #8-mediates/#3-owns-contact dispatch has no working delegate; #14 resolves the tackle outcome itself (§3.6.5).)
**Version:** 0.3
**Status:** DRAFT
**Source:** `outline-detailed.md` v1.0 (May 17, 2026)

---

## 8.1 Cross-Spec References

Every cross-spec reference is allocated a unique `XC-014-NNN` identifier.
Citations are grep-verified against the authoritative source section files at
section-file draft time (May 17, 2026). All references resolve against the
spec versions current on that date.

| XC-ID | Spec | Section | Version at citation | What is consumed |
|---|---|---|---|---|
| XC-014-001 | #1 Ball Physics | §1.2, Appendix C | v1.0 APPROVED | Corner-origin coordinate system (X = 0–105 m goal-to-goal, Y = 0–68 m touchline-to-touchline, Z = height, origin = corner); `PITCH_LENGTH_M = 105.0 m`; `PITCH_WIDTH_M = 68.0 m`; `HALF_LINE_X = 52.5 m` (§6.1 CROSS constants). |
| XC-014-002 | #2 Agent Movement | §2.5 (XC-002-001) | v1.1.1 APPROVED | EntityId no-reuse constraint: no despawned EntityId is reused within the lifetime of a match. Inherited by #14 for all EntityId iteration and tie-break logic. |
| XC-014-003 | #2 Agent Movement | §3.1 | v1.1 APPROVED | Dwell-time + dead-zone hysteresis pattern reused by §3.11 (KD-11). #14 parameterises the #2 pattern with `MARK_DWELL_TICKS [GT]`. |
| XC-014-004 | #3 Collision System | §3.3 (section-3-3.md) | v1.0 APPROVED | Agent-agent collision response surface. **Amended (KD-6 revised — `ERR-014-006`, August 12, 2026):** originally read "#14's `TackleIntentRequest` is dispatched here via #8 (KD-6). #14 does NOT call #3 directly; #8 mediates." That dispatch has no working delegate — #8's `ActionType` ordinal space is exhausted and #3 defers slide-tackle collision to Stage 2 (§7.2.1). #14 now resolves the tackle outcome itself as an abstract attribute duel (§3.6.5); #3 §3.3 remains cited here only as the Stage 2+ fallback contact-physics authority, not a Stage 0 dispatch target. Q2 resolution: confirmed at section-file draft (superseded by this amendment). |
| XC-014-005 | #7 Perception System | §3.7–§3.10 | v1.0 APPROVED | Perception snapshot schema: agent positions, velocities, `isActive`, ball position, ball velocity, possession owner. Perceived opponent attributes consumed at Stage 0: `FirstTouch` (threat score §3.5), `Tackling` (declared for future tackle-quality use; not consumed by §3.6 algorithm at Stage 0). All attribute reads go through `perception.GetPerceivedAttribute` (KD-1 cite-not-redefine). |
| XC-014-006 | #8 Decision Tree | §1.3.2 | v1.1 APPROVED | Stage 1+ deferral of coordinated defensive assignment. Authoritative basis for the §1.8 Stage-Binding Statement: "#14 introduces coordinated mark assignments" at Stage 1 per §1.3.2. |
| XC-014-007 | #8 Decision Tree | §1.7.3 (XC-008-001) | v1.1.1 APPROVED | EntityId no-reuse constraint (back-propagated from #16 §3.2.5 via ERR-016-002). Inherited by #14 per XC-014-002 chain. |
| XC-014-008 | #8 Decision Tree | §2.2.6 | v1.1 APPROVED | `TacticalContext` struct definition. ERR-014-001 files the amendment to add `MarkDirective?` nullable field (Option B — mirrors #13 ERR-013-001 PressDirective? pattern per KD-5). Status: OPEN pending lead-developer ratification. |
| XC-014-009 | #8 Decision Tree | §3.1.7 | v1.1 APPROVED | `MOVE_TO_POSITION` utility function. At Stage 1, this utility consumes #14's `MarkAssignment.targetPosition` for HOLD_SHAPE agents when `TacticalContext.MarkDirective?` is non-null. |
| XC-014-010 | #8 Decision Tree | §3.1.9 | v1.1 APPROVED | `INTERCEPT` utility function. At Stage 1, this utility consumes #14's `MarkAssignment.targetEntityId` when mode is `INTERCEPT_RUNNER`. |
| XC-014-011 | #11 Goalkeeper Mechanics | §1.4 | v0.2 IN REVIEW | Defensive wall ownership table: wall placement is #14's responsibility. XC-014-011 and XC-014-012 together confirm wall is #14-scoped (§7.5 Stage 2+ extension). |
| XC-014-012 | #11 Goalkeeper Mechanics | FR-GK-016 | v0.2 IN REVIEW | Explicit functional requirement confirming defensive wall placement is NOT #11's responsibility — it belongs to #14. |
| XC-014-013 | #12 Positioning AI | §2.2 | v0.2 IN REVIEW | `FormationSlot` struct definition: `baselinePosition` (Vector2), `LineMembership` enum, `LaneAssignment`. Used as displacement-cost anchor and anti-chaos invariant 1 backline check. |
| XC-014-014 | #12 Positioning AI | §3.7 | v0.2 IN REVIEW | Formation slot output schema: per-agent `formationSlot` as produced by the positioning algorithm. Confirms `baselinePosition` is a `Vector2` in pitch-space coordinates. |
| XC-014-015 | #12 Positioning AI | §4.5.1 and §4.5.2 (v0.3 per ERR-013-007/008) | v0.2 IN REVIEW (§4.5 patch v0.3) | Stage 1+ accessor declarations: `GetBaselineShape(TeamId)`, `GetPhase(TeamId)`, `GetLine(EntityId)`, `GetDefensiveLineDepth(TeamId)`. These are the boundary declarations #14 relies on at Stage 1. Confirmed by ERR-013-007/008 resolution on May 17, 2026. |
| XC-014-016 | #12 Positioning AI | FR-PA-048 | v0.2 IN REVIEW | Confirms that #12 does not produce an interface against #14 at Stage 0 (CLAUDE.md Interface Design Principle; #14 is a consumer, not a target of #12 interfaces). |
| XC-014-017 | #13 Pressing AI | §2.2 (FR-PR-014) | v0.3 APPROVED (May 17, 2026) | Disjoint role partition: `PRIMARY_PRESS ⊕ COVER_SHADOW ⊕ HOLD_SHAPE`. #14 owns the `HOLD_SHAPE` subset exclusively (KD-4). |
| XC-014-018 | #13 Pressing AI | §4.5 | v0.3 APPROVED | `GetAssignments()` returning `ReadOnlySpan<PressAssignment>` and `GetDirective(TeamId)` returning `PressDirective`. Used by `HoldShapePoolFilter` (§3.2) and `OffsideTrapController` (§3.7). |
| XC-014-019 | #13 Pressing AI | §7.4 | v0.3 APPROVED | #14 handoff slot explicitly declared in #13 §7.4: "HOLD_SHAPE agents' mark/cover behaviour is owned by Defensive AI #14." Authoritative confirmation of the KD-4 boundary. |
| XC-014-020 | #16 Deterministic Simulation | §3.2 | v1.0.1 APPROVED | Authoritative state classification and per-tick digest scope. #14's authoritative state (`MarkDirective`, `MarkAssignment[]`, `MarkHysteresisState[]`, `OffsideLineState`, `TackleIntentRequest[]`) must be included in the per-tick digest per §3.2 (§4.6). |
| XC-014-021 | #16 Deterministic Simulation | §3.2.5 | v1.0.1 APPROVED | EntityId iteration order: all agent iteration must be EntityId-ascending to guarantee determinism across replay sessions (FR-DA-003). |
| XC-014-022 | #16 Deterministic Simulation | §3.4 | v1.0.1 APPROVED (with ERR-014-004 pending) | Domain tag registry. `DOMAIN_TAG_DEFENSIVE_AI = 0x1A [CROSS-PENDING]` pending ERR-014-004. Phase B/C block layout: `0x17` = #12, `0x18` or `0x1D` = #11 (ERR-011-001/ERR-012-001 race — if #12 reaches `APPROVED` first, #11 shifts from `0x18` to `0x1D`), `0x19` = #13, `0x1A` = #14, `0x1B` = #15. Promoted to `[CROSS]` atomically with #16 §3.4 v1.0.4 patch. |
| XC-014-023 | #16 Deterministic Simulation | §5 | v1.0.1 APPROVED | Determinism regression scenario corpus. #14's §5.4 tests (T-DA-DET-001..006) are scoped against this corpus. |
| XC-014-024 | #16 Deterministic Simulation | §6.2 | v1.0.1 APPROVED | Digest scope for tactical-AI outputs: defines which fields contribute to the per-tick digest. Confirms `MarkDirective`, `MarkAssignment[]`, `MarkHysteresisState[]`, `OffsideLineState`, and `TackleIntentRequest[]` are all digested. |
| XC-014-025 | #17 Event System | §3.10 | v0.3 APPROVED | Channel registry. The `0x18–0x1B` block within the domain-tag block is reserved for #14's channels (`MARK_ASSIGNED`, `LINE_STEPPED`) at Stage 1 via ERR-014-002 / ERR-014-003. No channels produced or consumed at Stage 0. |
| XC-014-026 | #18 Performance Optimization | §3.7 | v1.0 APPROVED | Zero-allocation hot-path constraint (`[HotPathAllocExempt]` attribute and zero-bytes-per-tick `[FIXED]` constant). FR-DA-006 binding. |
| XC-014-027 | #18 Performance Optimization | §6 | v1.0 APPROVED | Per-spec §6 ratify-not-override framework (KD-2). #18 §6 ratifies #14's 0.12 ms budget at Stage 1; it does not override it without an explicit amendment to this section. |
| XC-014-028 | #19 Testing Strategy | §3, §4 | v1.0 APPROVED | Test framework conventions and FR-traceability framework. Test ID prefix `T-DA-NNN` assigned per #19 §3.1.4 simulation-layer naming rule. |
| XC-014-029 | #20 Code Standards | §4.2 (FR-CS-025) | v1.1 APPROVED | Single constant catalogue per subsystem. `DefensiveAIConstants.cs` is the unique constant catalogue for #14; no magic numbers elsewhere (FR-DA-007 / KD-14). |

---

## 8.2 CLAUDE.md Invariants Bound

The following CLAUDE.md invariants are explicitly bound by this spec.
Each invariant is cited-not-redefined (KD-1).

| # | Invariant | Binding point in #14 |
|---|---|---|
| 1 | Corner-origin coordinate system: X = 0–105 m, Y = 0–68 m, origin = corner (not pitch centre) | XC-014-001 / §1.7 / §3.5.2 / §3.7.2 |
| 2 | Fatigue convention: 0.0 = fully rested, 1.0 = fully fatigued; any inversion is a critical error | FR-DA-008 / §1.7 / §4.7 check 3 |
| 3 | 10 Hz tactical loop / 60 Hz physics loop — do not conflate | FR-DA-001 / KD-2 / §1.7 / §4.1 |
| 4 | Zero-allocation hot path | FR-DA-006 / §4.2 / §6.2 / XC-014-026 |
| 5 | Constant-tag policy: every constant carries exactly one of `[GT]` / `[EST]` / `[FIXED]` / `[DERIVED]` / `[CROSS]` / `[CROSS-PENDING]` | FR-DA-035 / KD-13 / §6.1 |
| 6 | Interface Design Principle: no interface against #15 Attacking AI at Stage 0 | FR-DA-036 / KD-8 / §4.5.3 |
| 7 | Parameter-based physics (no type enums in physics layer): #14 produces `TackleIntentRequest` and (KD-6 revised, `ERR-014-006`) `TackleOutcome` structs; neither crosses into the physics layer. Original text ("#8 mediates; #3 owns contact physics") superseded — #14 resolves the tackle outcome itself (§3.6.5) | KD-6 / §3.6 / §3.6.5 / §4.5.2 |
| 8 | EntityId no-reuse: no despawned ID reused within a match | XC-014-002 / XC-014-007 / §1.7 |
| 9 | Stage 0 uses `float`; Fixed64 is Stage 5+ per #9 §8.1 | §7.10 / §4.6 |
| 10 | No magic numbers: all constants in `DefensiveAIConstants.cs` | FR-DA-007 / KD-14 / XC-014-029 |

---

## 8.3 Cross-Spec Error Report (ERR-014 Series)

All ERR-014-NNN entries filed at section-file draft time (May 17, 2026).

| ID | Description | Target spec | Target section | Status |
|---|---|---|---|---|
| ERR-014-001 | Add `TacticalContext.MarkDirective?` nullable field to #8 `TacticalContext` struct. Option B selected: mirrors #13's `PressDirective?` pattern (ERR-013-001). #14 writes the field per-team per-tick at Stage 1; #8 §3.1.7 (`MOVE_TO_POSITION`) and §3.1.9 (`INTERCEPT`) read it when non-null. The field is nullable so `null` at Stage 0 is a well-formed no-op signal to #8. | Decision Tree #8 | §2.2.6 (`decision-tree/section-2-1-to-2-2.md`) | OPEN — back-prop pending lead-developer ratification. Gates Stage 1 activation (FR-DA-037(a)). |
| ERR-014-002 | Register `MARK_ASSIGNED` channel in #17 §3.10 channel registry within the `0x18–0x1B` block reserved for #14. Payload: `agentId`, `newMode`, `newTargetId`, `tick`. Fired on mode/target transitions (not every tick). | Event System #17 | §3.10 | DEFERRED to Stage 1 first `src/DefensiveAI/` commit per KD-15. No spec-text change needed at Stage 0. Gates Stage 1 activation (FR-DA-037(c)). |
| ERR-014-003 | Register `LINE_STEPPED` channel in #17 §3.10 channel registry within the `0x18–0x1B` block. Payload: `team`, `stepUpTargetDepth`, `agentCount`, `tick`. Fired once per offside-trap step-up event (§3.7.4). | Event System #17 | §3.10 | DEFERRED to Stage 1 first `src/DefensiveAI/` commit per KD-15. Gates Stage 1 activation (FR-DA-037(c)). |
| ERR-014-004 | Allocate `DOMAIN_TAG_DEFENSIVE_AI = 0x1A` in #16 §3.4 domain-tag registry. Proposed value `0x1A` is the next available slot in the Phase B/C block after `DOMAIN_TAG_PRESSING_AI = 0x19`. The adjacent slot `0x18` is proposed for #11 (Goalkeeper Mechanics) but the ERR-011-001/ERR-012-001 race may shift #11 to `0x1D` if #12 reaches `APPROVED` first — this does not affect #14's `0x1A` slot, which is stable regardless. If a slot conflict emerges before ratification, the shift-right collision policy from CLAUDE.md OPEN ISSUES applies. | Deterministic Simulation #16 | §3.4 | OPEN — `DOMAIN_TAG_DEFENSIVE_AI` is `[CROSS-PENDING]` throughout this spec. `[CROSS-PENDING]` → `[CROSS]` promoted atomically with #16 §3.4 v1.0.4 patch landing. |

---

## 8.4 Academic / External References

No academic references are cited at Stage 0. The Defensive AI algorithms are
derived from first principles and from the upstream spec surfaces (#7, #8,
#12, #13), not from published literature.

**Caveat for future reviewers:** if future sections cite published research on
defensive shape efficiency, man-marking vs. zonal marking performance, or
offside trap strategy, DOIs must be verified before lead-developer sign-off.
The fabricated-reference precedent (Heading #10 v0.1 — two unverifiable citations
replaced with real DOIs at v0.2) establishes that unverified citations are a
High-severity finding in the adversarial review process. This spec contains
zero citations to verify at Stage 0.

---

## 8.5 Stale-Reference Grep Record

Cross-reference grepping performed at section-file draft time (May 17, 2026).
The following searches were executed against the full `docs/specs/defensive-ai/`
folder and against the 14 upstream spec folders listed in §1.3:

| Search term | Expected result | Result |
|---|---|---|
| `#7 in section` (stale Decision Tree #7 ref) | Zero hits in body text | PASS — no stale #7 spec-number references |
| `#9 in section` (stale Heading #9 ref) | Zero hits in body text (correct: #9 = Fixed64) | PASS |
| `#10 in section` (stale Goalkeeper #10 ref) | Zero hits in body text (correct: #10 = Heading) | PASS |
| `ERR-014-001` | Present in §1.3.3, §4.4.4, §7.1, §8.3, §9.2, §9.3 | PASS — all expected sites |
| `ERR-014-002` / `ERR-014-003` | Present in §1.3.3, §4.5.4, §7.3, §8.3 | PASS |
| `ERR-014-004` | Present in §2.1, §4.6, §6.1, §8.3, §9.3 | PASS |
| `CROSS-PENDING` | Present in §2.1 FR-DA-005, §4.6, §6.1 `DOMAIN_TAG_DEFENSIVE_AI` row, §8.1 XC-014-022, §8.3 ERR-014-004 | PASS — all expected sites; no untagged CROSS-PENDING |
| `offside rule` / `VAR` / `goal line distance` / `offside decision` (adjudication leak check) | Zero hits in §3 text | PASS — §4.7 check 5 validated |
| Interface referencing #15 in code form | Zero hits in code blocks | PASS — §4.7 check 6 validated |
| `1.0 = fully rested` / `1 = rested` (fatigue inversion) | Zero hits | PASS — fatigue convention correct throughout |

---

## 8.6 Version History

| Version | Date | Author | Summary |
|---|---|---|---|
| 0.1 | May 17, 2026 | AI agent | Initial draft. 29 XC-014-NNN cross-references allocated and audit-verified. ERR-014-001..004 declared with target spec, section, and status. Zero academic references at Stage 0. CLAUDE.md invariants 1–10 bound. Stale-reference grep record (7 search terms) with all-PASS results. |
| 0.2 | May 17, 2026 | AI agent | PASS-1 adversarial review fix pass. M6: XC-014-005 "what is consumed" list corrected — `Anticipation` removed (not used in any §3 formula); `Tackling` annotated as "declared for future tackle-quality use; not consumed at Stage 0". M7: XC-014-022 and ERR-014-004 updated to reflect ERR-011-001/ERR-012-001 race — #11 may occupy `0x18` or shift to `0x1D` if #12 reaches `APPROVED` first; #14's `0x1A` slot is stable regardless. |
| 0.3 | August 12, 2026 | AI agent (wiring backlog W2) | KD-6 revised (`ERR-014-006`): XC-014-004 corrected — the "dispatched here via #8... #8 mediates" claim had no working delegate; #3 §3.3 is now cited only as the Stage 2+ fallback contact-physics authority. §8.2 invariant row 7 corrected to name `TackleOutcome` alongside `TackleIntentRequest` and drop the superseded #8/#3 clause. `ERR-014-006` itself is not added as a new §8.3 row in this pass — it is filed against #14's own KD-6 rather than as a cross-spec back-prop target, and `docs/tracking/tackle-wiring-design.md` is its authoritative record; flagged for a follow-up decision rather than added here to avoid an uncoordinated change to the "ERR-014-001..004" count in `section-9-approval-checklist.md` §9.1 item 25. |
