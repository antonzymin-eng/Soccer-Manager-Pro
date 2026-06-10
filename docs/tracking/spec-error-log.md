# Specification Error Log

**Purpose:** Records architectural errors, unnecessary complexity, and incorrect patterns
identified during specification review. Each entry documents the problem, the correct
approach, and every file requiring revision. Fixes are deferred — this log is the
authoritative remediation backlog.

**Created:** February 19, 2026, 5:00 PM PST
**Version:** 1.24
**Updated:** May 22, 2026 (ERR-020-001 filed and resolved: Code Standards #20 §4.2 `[CROSS]` mirror ALL_CAPS → PascalCase; `section-4.md` v1.0.1 patched; `src/CLAUDE.md` v1.4 discrepancy note updated)
**Status:** ERR-001 through ERR-012, ERR-010-001 (closed May 16, 2026), ERR-011-001 (closed May 18, 2026), ERR-012-001 (closed May 18, 2026), ERR-012-002 (closed), ERR-016-001, ERR-016-002 (FULLY CLOSED May 18, 2026), ERR-017-001, ERR-018-001 through ERR-018-018 logged. ERR-010 closed (March 6, 2026). ERR-012 appended from addendum (April 22, 2026). ERR-016-001 added May 2, 2026 (phantom interface mitigation in Deterministic Simulation §4.2). ERR-016-002 added May 3, 2026; spec-text resolved May 6, 2026 (`XC-002-001` in #2 §2.5; `XC-008-001` in #8 §1.7.3); #16 §3.2.5 back-prop prose confirmed landed (OBS-1, stress-test run 2, May 18, 2026) — FULLY CLOSED. ERR-017-001 added May 12, 2026 (Event System #17 PASS 2 review — `DOMAIN_TAG_EVENT_LEDGER` allocation back-prop into #16 §3.4); fully resolved May 15, 2026 — #16-side allocation landed May 14, 2026 (`0x15` in #16 §3.4 v1.0.1) and #17-side `[CROSS-PENDING]` → `[CROSS]` promotion landed in #17 §1.0.1 patch revision May 15, 2026 (literal value inlined across §3.4.2 / §3.10 / §1.4 / §2.4.4 / §7.5 D9 / §8.1.4 / §8.3.4 / §8.4 / §9.2 Q10 / §9.3 R3 / Appendix B / Appendix D). ERR-018-001 added May 13, 2026 and resolved same day at outline level (Performance Optimization #18 `outline-detailed.md` v1.1 inverts KD-3 — #18 owns trace pipeline, #16 retains record format / regression scenarios / emission constraints; section-number citations corrected). ERR-018-002 through ERR-018-011 added May 14, 2026 from PASS-1 adversarial review of #18 section files v0.1 (4 H + 6 M findings); all resolved in v0.2 fix pass (May 14, 2026). ERR-018-012 through ERR-018-018 added May 14, 2026 from PASS-2 adversarial review of #18 section files v0.2 (2 H + 5 M findings tracing primarily to PR #59 + PR #60 parallel-branch merge collisions); all resolved in v0.3 fix pass (May 14, 2026) — #18 section files at v0.3. ERR-002 and ERR-003 remain open. ERR-003-001 through ERR-003-004 added June 10, 2026 (Collision System #3 implementation AR-7 adversarial review — force-conversion calibration, FROM_BEHIND normal convention, same-team stumble gap, candidate-counted pair valve); ERR-003-005 and ERR-003-006 added same day from the AR-8 follow-up sweep (inverted approach gate in §3.3 impulse response; FROM_BEHIND shadowed by the shoulder predicate); all six spec-and-code patched and CLOSED June 10, 2026.
**Raised During:** Pass Mechanics Spec #5 pre-Section 3 cross-spec audit; Decision Tree Spec #8 BLK-001

---

## Error Index

| ID | Title | Severity | Files Affected | Status |
|----|-------|----------|---------------|--------|
| ERR-001 | `IBallPhysicsCallback` fragments a single operation into four methods | Major | 2 | Closed — fixed in First_Touch_Spec_Section_4_v1_1.md |
| ERR-002 | `StringIDs` papers over an undesigned event bus with the wrong solution | Moderate | 1 | Open — low priority, fix at convenience |
| ERR-003 | `PerformanceContext` violation mandate imposes governance with no Stage 0 benefit | Moderate | 10 | Open — low priority, fix at convenience |
| ERR-004 | `IPossessionManager` and `IFirstTouchEventQueue` interface against unspecified systems | Major | 4 | Closed — fixed in First_Touch_Spec_Section_4_v1_1.md |
| ERR-005 | `KickType` enum encodes caller intent into Ball Physics (eliminated by design decision) | Major | 2 | Closed — resolved during audit |
| ERR-006 | `Ball.ApplyKick()` / `KickType` referenced in Ball Physics §8 but never defined in §3.1.11 | Critical | 2 | Closed — resolved in Ball_Physics_Spec_Section_3_1_v2_5.md |
| ERR-007 | `KickPower`, `WeakFootRating`, `Crossing` absent from `PlayerAttributes` | Critical | 1 | Closed — resolved in Agent_Movement_Spec_Section_3_5_v1_3.md |
| ERR-008 | `BallState` has no `PossessingAgentId` field; `ApplyKick()` amendment references it incorrectly | Critical | 2 | Closed — Option B adopted; possession external to BallState; resolved in Ball_Physics_Spec_Section_3_1_v2_5.md |
| ERR-009 | `PassThroughGround` / `PassThroughAerial` are redundant `KickType` values | Minor | 1 | Closed — resolved during audit; through passes use `PassGround`/`PassLofted` |
| ERR-010 | Shot Mechanics §1.1 refers to Decision Tree as Spec #7 — canonical number is #8 | Minor | 1 | ✅ Closed — Fixed in shot-mechanics/section-1.md v1.2 (March 6, 2026); part of comprehensive audit renumbering cascade |
| ERR-011 | `SpatialHashGrid.Query()` ignores radius parameter — always returns fixed 3×3 neighbourhood | Major | 1 | ✅ Closed — Fixed in Collision_System_Spec_Section_3_v1_1.md (March 5, 2026) |
| ERR-012 | First Touch §7 refers to Decision Tree as Spec #7 (5 occurrences) | Minor | 1 | ✅ Closed — Fixed in first-touch/section-7.md v1.1 (March 5, 2026) |
| ERR-012-001 | `DOMAIN_TAG_POSITIONING_AI` allocation + Phase B/C block (originally proposed `0x16…0x1B`; shifted to `0x17…0x1C` May 16, 2026 after #10 took `0x16`) needed in #16 §3.4 | Medium | 1 | ✅ Resolved May 18, 2026 — `DOMAIN_TAG_POSITIONING_AI = 0x17` allocated in #16 §3.4 v1.0.5; §6.1 `[CROSS-PENDING]` → `[CROSS: #16 §3.4]` promoted atomically with #12 `APPROVED`; body-text instances in §1/§2/§3/§4/§8 promoted in v0.3/v0.4 fix passes |
| ERR-012-002 | Decision Tree #8 `section-3-1.md` L716 cites Formation System as "Spec #14" — current #14 is Defensive AI; Formation System is #12 | Minor | 1 | ✅ Closed — Fixed in decision-tree/section-3-1.md v1.1.1 (May 15, 2026); single-token "Spec #14" → "Positioning AI, Spec #12"; approval status preserved |
| ERR-008-001 | Decision Tree #8 §3.2 `PitchGeometry` pseudocode class uses centered origin `(0,0) = centre of pitch` with X:−52.5–+52.5m/Y:−34–+34m — contradicts CLAUDE.md + Ball Physics #1 §1.2 corner-origin; all goal constants wrong | High | 1 | ✅ Resolved May 18, 2026 — `section-3-2.md` v1.3: class rewritten to corner-origin (0,0,0); all `Vector2` goal constants replaced with `Vector3` using correct values; citation corrected to §1.2 and Appendix C; XC-GEOM-01 verification note added |
| ERR-015-006 | Attacking AI #15 §1/§2/§3/§4 retain 7 stale `[CROSS-PENDING]` tags on `DOMAIN_TAG_ATTACKING_AI` after ERR-015-001 declared resolved; §9 checklist falsely claims "0 `[CROSS-PENDING]` remain" | Medium | 4 | ✅ Resolved May 18, 2026 — promoted all 7 hits to `[CROSS: #16 §3.4]` in §1 (4 instances), §2 FR-AT-005, §3 constant table, §4 §4.6 prose; v0.3 version-history rows added to all four section files |
| ERR-016-003 | Domain tag registry (#16 §3.4) silent gaps at `0x18` and `0x1C` — no `_RESERVED_0xNN_` placeholder rows; `0x18` orphaned when GK shifted to `0x1D`; `0x1C` block-end margin never documented | Medium | 1 | ✅ Resolved May 18, 2026 — `deterministic-sim/section-3.md` v1.0.6: `_RESERVED_0x18_` and `_RESERVED_0x1C_` placeholder rows added to §3.4 domain-tag table; v1.0.6 version-history row added |
| ERR-016-001 | Phantom interface risk in Deterministic Simulation §4.2 | Medium | 1 | ✅ Mitigated — §4.2 reclassified as non-normative sketches in v0.7 fix pass |
| ERR-016-002 | EntityId no-reuse cross-spec constraint not back-propagated to specs #2 and #8 | Medium | 3 | ✅ FULLY RESOLVED May 18, 2026 — (1) `XC-002-001` added to Agent Movement #2 §2.5 (v1.1.1, May 6, 2026); (2) `XC-008-001` added to Decision Tree #8 §1.7.3 (v1.1.1, May 6, 2026); (3) #16 §3.2.5 prose updated from "filed for back-propagation" to "back-propagated to #2 §2.5 and #8 §1.7.3" (confirmed landed per OBS-1 stress-test run 2, May 18, 2026). CLAUDE.md OPEN ISSUES entry removed. |
| ERR-017-001 | `DOMAIN_TAG_EVENT_LEDGER` allocation needed in Deterministic Simulation #16 §3.4 domain-tag table | Medium | 2 | ✅ FULLY RESOLVED. (1) #16-side May 14, 2026: `DOMAIN_TAG_EVENT_LEDGER = 0x15` allocated in #16 §3.4 (v1.0.1 patch revision); §8.3.1 #17 row promoted to `complete`. (2) #17-side May 15, 2026 (§1.0.1 patch revision): `[CROSS-PENDING]` → `[CROSS]` promotion completed across §3.4.2 / §3.10 / §1.4 / §2.4.4 / §7.5 D9 / §8.1.4 / §8.3.4 / §8.4 / §9.2 Q10 / §9.3 R3; Appendix B byte streams and Appendix D glossary now carry the literal value `0x15`. |
| ERR-018-001 | Performance Optimization #18 `outline-detailed.md` cites Deterministic Simulation #16 sections by stale numbers / non-existent name (`#16 §7 regression scenarios`, `#16 §5 canonical save format`, `#16 §8 trace channels`) | Medium | 1 | ✅ Resolved at outline level — May 13, 2026 (same day as filing). `outline-detailed.md` v1.1 (a) inverts KD-3 (Spec #18 owns the trace pipeline; Spec #16 retains authority over canonical record format §3.2.4.1, regression scenarios §5, and determinism-of-emission constraints / veto authority over tick-pipeline trace points §3.1), and (b) corrects every `TBD-NORMATIVE`-marked #16 section-number citation against current `deterministic-sim/section-*.md`. Rationale for inversion: trace channels are an observability concern, not a determinism concern; mirrors KD-4 (#19 owns testing infrastructure, consumes #16 scenarios). New FR-PO-058a in §3.8.3 enforces determinism-of-emission for every #18-emitted trace point. Section files drafted from v1.1 will not inherit the drift. Architectural concern (re-anchor vs invert) is closed; section-file authoring still required to faithfully implement inverted KD-3 (FR-PO-058a in §3.8.3, #16-owner sign-off audit in §5.7, record-format binding in §3.8.4). |
| ERR-018-002 | `[HotPathAllocExempt]` attribute cited in #18 as "declared in Spec #20 §3" but does not exist in `code-standards/` | High | 5 | ✅ Resolved — May 14, 2026 (v0.2 fix pass): §3.7.5 declares governance identifier in #18; Spec #20 §3 cited as policy authority only; C# attribute deferred to Stage 0+1 |
| ERR-018-003 | MUST/MAY conflict between FR-PO-067 (§2.2.9) and §3.4.4 on baseline-reproducibility re-run | High | 1 | ✅ Resolved — May 14, 2026 (v0.2 fix pass): §3.4.4 "MAY" → "MUST" |
| ERR-018-004 | Three-way stage-of-resolution contradiction on +5% threshold: FR-PO-031 "Stage 0+1" vs §7.5 D9 "Stage 1" vs §7.1 Stage 0+1 deliverable | High | 1 | ✅ Resolved — May 14, 2026 (v0.2 fix pass): §7.5 D9 "Stage 1" → "Stage 0+1" |
| ERR-018-005 | Channel registry schema absent from Appendix F; §3.8.2 "Stage 0 declares schema" obligation unmet; F.1/F.2/F.4 reference `perf.budget`/`perf.alloc` channels without registry backing | High | 1 | ✅ Resolved — May 14, 2026 (v0.2 fix pass): Appendix F.0 channel registry schema added |
| ERR-018-006 | Hot-path allocation budget = 0 bytes/tick tagged `[GT]` in §3.10 instead of `[FIXED]` — not a designer-tunable value | Medium | 1 | ✅ Resolved — May 14, 2026 (v0.2 fix pass): §3.10 and §8.4 tags updated `[GT]` → `[FIXED]` |
| ERR-018-007 | Three Spec #19 body-text citations missing `TBD-NORMATIVE` tag and absent from §9.4.1 blocker list: §3.4.3 ("per Spec #19 §3.4.3"), §3.3.5 ("parallel Spec #19 §6.1"), §3.9.5 ("Spec #19 §3.1") | Medium | 1 | ✅ Resolved — May 14, 2026 (v0.2 fix pass): TBD-NORMATIVE added to all three citations; §9.4.1 blocker list extended |
| ERR-018-008 | §3.9.1 ±20% `[EST]`→`[GT]` promotion tolerance untagged; not in §3.10 constants catalogue (CLAUDE.md requires source tag on every constant) | Medium | 1 | ✅ Resolved — May 14, 2026 (v0.2 fix pass): `[GT]` tag added inline; §3.10 and §8.4 rows added |
| ERR-018-009 | FR-PO-070 (Stage 0 MUST) requires `tools/run-perf-local.sh` to invoke `tools/budget-auditor.py`, which is a Stage 0+1 deliverable per §7.1 — bootstrapping contradiction | Medium | 2 | ✅ Resolved — May 14, 2026 (v0.2 fix pass): FR-PO-070 stage column updated to "Stage 0 (manual) / Stage 0+1 (automated)" with qualifier note |
| ERR-018-010 | Appendix F.1 `N=100` captures `[GT]` and Appendix F.5 1% flake-rate threshold are governance constants absent from §3.10 catalogue; F.5 threshold also untagged | Medium | 1 | ✅ Resolved — May 14, 2026 (v0.2 fix pass): §3.10 and §8.4 rows added; F.5 threshold tagged `[GT]` |
| ERR-018-011 | `SPEC_INDEX.md` row 18 still shows `IN PROGRESS`; #18 §9.4 prematurely declares `IN REVIEW` (canonical registry contradicted per CLAUDE.md "SPEC_INDEX.md is the canonical source of truth") | Medium | 3 | ✅ Resolved — May 14, 2026 (v0.2 fix pass): SPEC_INDEX.md row 18 updated to `IN REVIEW`; CLAUDE.md and file-manifest.md updated atomically |
| ERR-018-012 | Appendix F has two `### F.0 Channel Registry Schema` sections (lines 231 and 258) with conflicting field sets (13 fields vs 7 fields, different names — `owning_subsystem` vs `subsystem_owner`, `inside_tick_pipeline`+`sign_off_log_ref` vs `emission_veto_required`) | High | 1 | ✅ Resolved — May 14, 2026 (v0.3 fix pass): kept canonical 13-field F.0; merged in `perf.budget`/`perf.alloc`/`perf.trace` anchor rows from the duplicate as Stage 0 illustrative entries. Root cause: PR #59 + PR #60 parallel-branch merge of independent ERR-018-005 fixes |
| ERR-018-013 | `section-3.md` §3.10 Constants Catalogue has three pairs of duplicate-constant rows: ±20% promotion tolerance (565↔572), N=100 dashboard window (566↔573), 1% flake threshold (567↔574) | High | 1 | ✅ Resolved — May 14, 2026 (v0.3 fix pass): deleted the three v0.1 rows; kept the v0.2 rows with richer rationale. Root cause: same PR #59 + PR #60 merge collision as ERR-018-012 |
| ERR-018-014 | Seven section files (section-2 / 3 / 5 / 7 / 8 / 9 + appendices) carry duplicate v0.2 version-history rows sandwiching the v0.1 row | Medium | 7 | ✅ Resolved — May 14, 2026 (v0.3 fix pass): consolidated each pair into a single v0.2 row carrying the union of fix-list notes; v0.3 row appended below |
| ERR-018-015 | `section-1.md` header `Last Updated: May 13, 2026` is stale vs its own v0.2 row dated May 14, 2026 (every other section file's header is May 14) | Medium | 1 | ✅ Resolved — May 14, 2026 (v0.3 fix pass): header updated to `May 14, 2026 (v0.3 PASS-2 adversarial-review fix pass)` |
| ERR-018-016 | `section-3.md` §3.5.2 Shot Mechanics example conflates the +5% per-PR gate (vs measured pre-PR baseline) with the ±20% `[EST]`→`[GT]` promotion tolerance from §3.9.1 — invokes the +5% gate against an un-promoted spec-time anchor | Medium | 1 | ✅ Resolved — May 14, 2026 (v0.3 fix pass): example rewritten to apply ±20% promotion tolerance at first capture, then +5% (or per-spec tighter override) for subsequent per-PR captures |
| ERR-018-017 | FR-PO-019 levels `MAY` but its statement embeds an unconditional MUST ("manifest ID and seed MUST be recorded the same way") — same structural shape as ERR-018-003 | Medium | 1 | ✅ Resolved — May 14, 2026 (v0.3 fix pass): split into FR-PO-019 (MAY: cross-scenario profiling is permitted) and FR-PO-019a (MUST: manifest ID and seed MUST be recorded per FR-PO-016) |
| ERR-018-018 | §3.7.5 pre-specifies a C# attribute signature (`Method | Constructor` targets, `string rationale` constructor argument) at spec-time without a specified consumer — phantom-interface trap per CLAUDE.md "Interface Design Principle" (ERR-001 / ERR-004 hazard) | Medium | 1 | ✅ Resolved — May 14, 2026 (v0.3 fix pass): §3.7.5 deferred concrete C# signature to Stage 0+1 alongside §7.5 D2 alloc-tracker pin; retained governance contract (rationale, sign-off, source-level marker) which is signature-independent |
| ERR-013-001 | Pressing AI #13 requires a back-prop into Decision Tree #8 §2.2.6 to add `PressDirective?` field to `TacticalContext`. Option B selected. | Medium | 2 | ✅ **Resolved May 17, 2026** — `decision-tree/section-2-1-to-2-2.md` v1.1.1: nullable `PressDirective?` field added to `TacticalContext` struct (null at Stage 0; #13 writes at Stage 1+; DT reads for PRESS utility §3.2.7). |
| ERR-013-002 | Pressing AI #13 requires `PRESS_TRIGGERED` channel registration in Event System #17 §3.10 channel registry. Channel emitted when a `PressDirective` becomes non-empty (non-trivial press fires). | Low | 1 | Open (Stage 1) — filed May 17, 2026 from #13 section-files v0.1. Non-blocking for #13 Stage 0 spec text per KD-11 ("no #17 channels at Stage 0"). Lands at Stage 1 first commit per #18 Appendix F.0 / §7.2. |
| ERR-013-003 | Pressing AI #13 requires `PRESS_DISENGAGED` channel registration in Event System #17 §3.10 channel registry. Channel emitted when a `PressDirective` returns to all-`HOLD_SHAPE` after a non-trivial press. | Low | 1 | Open (Stage 1) — filed May 17, 2026 from #13 section-files v0.1. Non-blocking for #13 Stage 0 spec text per KD-11. Lands at Stage 1 first commit per #18 Appendix F.0 / §7.2. |
| ERR-013-004 | Stale "Fatigue System #13" reference at `decision-tree/section-3-1.md` L753 — but #13 is Pressing AI. | Minor | 1 | ✅ **Resolved May 17, 2026** — one-token patch: "Fatigue System #13" → "Pressing AI #13" at `decision-tree/section-3-1.md` L753. |
| ERR-013-005 | `DOMAIN_TAG_PRESSING_AI = 0x19` allocation needed in Deterministic Simulation #16 §3.4. | Medium | 1 | ✅ **Resolved May 17, 2026** — allocated in `deterministic-sim/section-3.md` v1.0.3 (`0x17` reserved for #12, `0x18` for #11, `0x19` for #13); #13 §6.1 `[CROSS-PENDING]` → `[CROSS]` atomically. |
| ERR-013-007 | Pressing AI #13 requires `GetPhase(TeamId)` as a Stage 1 accessor on Positioning AI #12. | Medium | 2 | ✅ **Resolved May 17, 2026** — declared in `positioning-ai/section-4.md` §4.5.1 v0.3 patch as Stage 1 publication commitment. |
| ERR-013-008 | Pressing AI #13 requires `GetLine(EntityId)` elevated from Stage 1+ to Stage 1 on Positioning AI #12. | Medium | 2 | ✅ **Resolved May 17, 2026** — declared in `positioning-ai/section-4.md` §4.5.1 v0.3 patch; `GetLine` elevated Stage 1+ → Stage 1. |
| ERR-020-001 | Code Standards #20 §4.2 `[CROSS]` mirror example uses ALL_CAPS field name (`PHYSICS_TICK_HZ`) — contradicts §3.2.3 PascalCase rule for `[CROSS]` constants. | Minor | 2 | ✅ **Resolved May 22, 2026** — `code-standards/section-4.md` v1.0.1: mirror field renamed `PHYSICS_TICK_HZ` → `PhysicsTickHz`; XML doc updated with spec+section citation. `src/CLAUDE.md` v1.4: discrepancy note updated with ERR-020-001 reference. |

---

## ERR-001: `IBallPhysicsCallback` fragments a single operation into four methods

**Severity:** Major
**Detected:** February 19, 2026
**Root Cause:** Interface written by producer (First Touch) to describe what it provides
to Ball Physics, rather than by the consumer (Ball Physics) to describe what it needs.
The four methods encode First Touch's internal `TouchResult` taxonomy into Ball Physics,
creating coupling between two systems that should be independent.

**Problem in detail:**
`IBallPhysicsCallback` defines four methods:
- `OnControlled(agentID, position, velocity)`
- `OnLooseBall(position, velocity)`
- `OnDeflected(position, deflectionVelocity)`
- `OnIntercepted(interceptingAgentID, position, velocity)`

All four do the same physical thing: set ball position and velocity. The method name
encodes why First Touch is calling — which is First Touch's concern, not Ball Physics'.
Ball Physics does not and should not change its behaviour based on which `TouchResult`
produced the call. Teaching Ball Physics about `TouchResult` states via method names
is inverted responsibility.

**Correct approach:**
Single method: `SetBallState(Vector3 position, Vector3 velocity)`
First Touch calls it once with the computed position and velocity regardless of outcome.
Ball Physics applies the state. The `TouchResult` outcome is First Touch's internal
classification and stays there.

**Files requiring revision:**

| File | Section | Change |
|------|---------|--------|
| `First_Touch_Spec_Section_4_v1_0.md` | §4.5.2 | Remove `IBallPhysicsCallback` interface definition; replace 4-method calls with single `SetBallState(position, velocity)` call in `ApplyTouchResult()`; update §4.5 interface table entry; update flow diagram ASCII art at §4.4 |
| `First_Touch_Spec_Outline_v1_0.md` | Interface contracts table | Remove `IBallControlCallback` row; replace with `SetBallState()` direct call note |

**Version impact:** `First_Touch_Spec_Section_4_v1_0.md` → v1.1

---

## ERR-002: `StringIDs` papers over an undesigned event bus with the wrong solution

**Severity:** Moderate
**Detected:** February 19, 2026
**Root Cause:** Premature optimisation for a system (Event Bus) that has not yet been
designed. The `StringIDs` pattern assumes the Event Bus will dispatch on string keys and
pre-hashes them to avoid runtime allocation. This assumption may be wrong.

**Problem in detail:**
`Master_Vol_4_Tech_Implementation.md` specifies a `StringIDs` static class that
pre-hashes string constants (player names, tactic names) to `int32` at startup:

```csharp
public static class StringIDs {
    public static readonly int TACTIC_GEGENPRESS = Hash("Gegenpressing");
}
```

This pattern only makes sense if the Event Bus dispatches on string keys. If the Event
Bus uses typed event structs (the standard C# pattern: `EventBus.Publish<TEvent>(evt)`),
dispatch is on the type identity — zero strings, zero hashing, zero `StringIDs` class
needed. The `StringIDs` solution solves the wrong problem.

**Correct approach:**
Remove `StringIDs`. Document that the Event Bus will use typed event structs. String
hashing is a last resort for systems that cannot use typed dispatch (e.g., scripting
bridges, serialised network events). Those cases, if they arise, are addressed when
the Event System (Spec #17) is designed.

**Files requiring revision:**

| File | Section | Change |
|------|---------|--------|
| `Master_Vol_4_Tech_Implementation.md` | `StringIDs` section | Remove class definition and example; replace with note: "Event Bus dispatches on typed structs. String-keyed dispatch is not used. String hashing deferred pending Event System Spec #17 design." |

**Version impact:** `Master_Vol_4_Tech_Implementation.md` → minor revision

---

## ERR-003: `PerformanceContext` violation mandate imposes governance with no Stage 0 benefit

**Severity:** Moderate
**Detected:** February 19, 2026
**Root Cause:** Legitimate Stage 4 architecture (`PerformanceContext` modifier chain)
given an enforcement rule that designates direct attribute access as a "specification
violation" — in a stage where the gateway is a passthrough multiplying by 1.0.

**Problem in detail:**
`Agent_Movement_Spec_Section_3_2_v1_0.md` §3.2.1 contains:

> "Any specification that evaluates a player attribute for gameplay purposes MUST call
> `EvaluateAttribute()` or `EvaluateAttributePair()`. Direct access to raw attribute
> values for gameplay calculations is a **specification violation**."

`PerformanceContext` and `EvaluateAttribute()` are correct long-term architecture — in
Stage 4, a rated-18 player performing like a 13 during a bad season is a genuinely
valuable simulation feature. The gateway earns its existence.

The problem is the **violation designation**. Calling `EvaluateAttribute(18)` in Stage 0
returns exactly `18.0f`. The mandate forces every spec (all 20) to import, instantiate,
and route through `PerformanceContext` for a multiply-by-one operation, on pain of
being in violation. This governance overhead is disproportionate to Stage 0 benefit.

**Correct approach:**
Keep `PerformanceContext` and `EvaluateAttribute()` — they are good architecture.
Reword the enforcement rule as a recommendation:

> "Specifications evaluating player attributes for gameplay calculations should route
> through `EvaluateAttribute()`. This enables Stage 4 form, psychology, and career
> modifiers to activate without refactoring downstream formulas."

No violation designation. Compliance by convention, not mandate.

**Files requiring revision:**

| File | Section | Change |
|------|---------|--------|
| `Agent_Movement_Spec_Section_3_2_v1_0.md` | §3.2.1 | Remove bolded violation rule; reword as recommendation |
| `Agent_Movement_Spec_Section_3_5_v1_2.md` | PerformanceContext usage note (`CRITICAL` block) | Remove `CRITICAL` designation; reword as convention note |
| `Agent_Movement_Spec_Section_3_6_v1_1.md` | Any violation reference | Remove violation language |
| `Agent_Movement_Spec_Section_3_7_v1_2.md` | Test descriptions referencing violation | Remove violation language from test pass criteria |
| `Agent_Movement_Spec_Section_4_v1_1.md` | Any violation reference | Remove violation language |
| `Agent_Movement_Spec_Section_6_v1_1.md` | Future extensions referencing enforcement | Remove violation language |
| `Agent_Movement_Spec_Section_9_Approval_Checklist.md` | Any checklist item verifying enforcement compliance | Reword as convention check, not violation check |
| `Agent_Movement_Spec_Appendices_v1_1.md` | Any enforcement reference | Remove violation language |
| `Agent_Movement_Spec_Remaining_Sections_Outline.md` | Any enforcement reference | Remove violation language |
| `First_Touch_Spec_Outline_v1_0.md` | Any PerformanceContext violation reference | Remove violation language |

**Note:** `PerformanceContext` struct definition, `EvaluateAttribute()` method, factory
methods, and all formula usage remain unchanged. Only the enforcement designation is
removed.

**Version impact:** 10 files → minor revision each (single sentence change per file)

---

## ERR-004: `IPossessionManager` and `IFirstTouchEventQueue` interface against unspecified systems

**Severity:** Major
**Detected:** February 19, 2026
**Root Cause:** Interfaces written before the systems they interface with have been
specified. Interfaces written speculatively against undesigned consumers will be
redesigned when the real consumer is specified, making the Stage 0 interface vestigial
or a constraint on the future design.

**Problem in detail:**

**`IPossessionManager`** (First Touch §4.5.4):
The spec notes: *"Implementer: PossessionManager (Spec TBD, Stage 0 stub sufficient)"*
The Stage 0 stub is one line of work. An interface written against "Spec TBD" will
either be replaced when the Possession Manager is specified, or will constrain that
spec's design to fit an interface written without knowing what the system needs to do.

**`IFirstTouchEventQueue`** (First Touch §4.5.5):
A ring buffer interface with capacity 64, connected to Event System (Spec #17, Stage 1).
The Event System has not been designed. The ring buffer capacity (64) and the
`Enqueue(FirstTouchEvent)` method shape are speculative. When Stage 1 Event System is
designed, it will define its own buffering and dispatch requirements — at which point
this interface is either replaced or becomes a constraint.

**Correct approach:**
Remove both interfaces. Replace with direct, minimal Stage 0 implementations:

- Possession: `ball.PossessingAgentId = agentId` (pending BallState amendment ERR-008)
- Event queue: comment stub — *"Event publishing deferred to Stage 1. When Event System
  (Spec #17) is designed, First Touch will implement its consumer interface here."*

Write the interfaces when both sides (First Touch and their consumers) are fully
specified. Do not write an interface when one side is "Spec TBD."

**Files requiring revision:**

| File | Section | Change |
|------|---------|--------|
| `First_Touch_Spec_Section_4_v1_0.md` | §4.5.4 | Remove `IPossessionManager` interface; replace possession assignment logic with direct `BallState` field write; update §4.5 interface table; update flow diagram |
| `First_Touch_Spec_Section_4_v1_0.md` | §4.5.5 | Remove `IFirstTouchEventQueue` interface and ring buffer specification; replace with deferred comment stub; update §4.5 interface table |
| `Agent_Movement_Spec_Section_5_v1_1.md` | Any test mocking `IFirstTouchEventQueue` | Remove or replace with stub |
| `Collision_System_Spec_Section_6_v1_1.md` | Any performance reference to event queue | Remove or note as deferred |
| `First_Touch_Spec_Section_6_v1_0.md` | Event queue in performance budget | Remove ring buffer from budget; note as deferred |

**Version impact:** `First_Touch_Spec_Section_4_v1_0.md` → v1.1 (combined with ERR-001 fix)

---

## ERR-005: `KickType` enum encodes caller intent into Ball Physics

**Severity:** Major
**Detected:** February 19, 2026
**Status:** CLOSED — resolved during audit session

**Resolution:**
`KickType` enum eliminated entirely. `Ball.ApplyKick()` signature reduced to physical
parameters only: `ApplyKick(ref BallState ball, Vector3 velocity, Vector3 spin,
int agentId, float matchTime)`. The pass type is fully encoded in the velocity and
spin vectors — Ball Physics does not need to know the caller's intent label to simulate
correct aerodynamics. Pass Mechanics maps its `PassType` to physical parameters; that
is its entire job.

**Files affected by resolution:**
- `Ball_Physics_Spec_Section_3_1_Amendment_1_v1_0.md` — drafted without `KickType`
- `Pass_Mechanics_Spec_Outline_v1_0.md` — `KickType` references are outline-only;
  will not appear in Section 3 implementation

---

## ERR-006: `Ball.ApplyKick()` referenced in Ball Physics §8 but never defined in §3.1.11

**Severity:** Critical
**Detected:** February 19, 2026
**Status:** CLOSED — Resolved in Ball_Physics_Spec_Section_3_1_v2_5.md (February 21, 2026)

**Resolution:**
`ApplyKick(ref BallState ball, Vector3 velocity, Vector3 spin, int agentId, float matchTime)`
defined at §3.1.11.2. No `KickType` parameter (ERR-005 resolution). Option B possession
model applied (ERR-008 resolution). State transitions to `AIRBORNE` or `ROLLING` on kick;
agent system observes and clears possession on its side.

**Files requiring revision:**

| File | Section | Change |
|------|---------|--------|
| `Ball_Physics_Spec_Section_3_1_v2_4.md` | §3.1.11 | Add §3.1.11.1 label to `CheckPossession()`; add §3.1.11.2 `ApplyKick()` method (no `KickType` per ERR-005 resolution); update table of contents |
| `Ball_Physics_Spec_Section_8_v1_2.md` | §8.3 reference | Update `§3.1.11.2` cross-reference to `§3.1.11.2` (or §3.1.11.3 per final subsection numbering) |

**Version impact:** `Ball_Physics_Spec_Section_3_1_v2_4.md` → v2.5

---

## ERR-007: `KickPower`, `WeakFootRating`, `Crossing` absent from `PlayerAttributes`

**Severity:** Critical
**Detected:** February 19, 2026
**Status:** CLOSED — Resolved in Agent_Movement_Spec_Section_3_5_v1_3.md (February 22, 2026)

**Resolution:**
`KickPower` (1–20), `WeakFootRating` (1–5), and `Crossing` (1–20) added to
`PlayerAttributes` struct. All 9 blocked Pass Mechanics tests (PV-006, WF-001–WF-006,
IT-004) are now unblocked.

**Files requiring revision:**

| File | Section | Change |
|------|---------|--------|
| `Agent_Movement_Spec_Section_3_5_v1_2.md` | §3.5.6 `PlayerAttributes` | Add `KickPower` (1–20), `WeakFootRating` (1–5), `Crossing` (1–20); update struct comment `Consumed by` list; update struct size estimate |

**Version impact:** `Agent_Movement_Spec_Section_3_5_v1_2.md` → v1.3

---

## ERR-008: `BallState` has no `PossessingAgentId` field; `ApplyKick()` amendment references it

**Severity:** Critical
**Detected:** February 19, 2026
**Status:** CLOSED — Option B adopted February 22, 2026. Resolved in Ball_Physics_Spec_Section_3_1_v2_5.md.

**Design Decision: Option B — Possession external to BallState**

Possession is agent state, not ball state. `BallState` is a pure physics struct; adding
`PossessingAgentId` would introduce the only agent reference in Ball Physics, violating
single responsibility. It would also create a synchronisation hazard between two systems
both tracking possession.

**Resolution:**
`ApplyKick()` transitions `ball.State` from `CONTROLLED` to `AIRBORNE` (or `ROLLING`).
The agent system observes this state transition and clears its own possession record.
Agent system is the single source of truth for possession. No `PossessingAgentId` field
added to `BallState`.

Ball_Physics_Spec_Section_3_1_v2_5.md §3.1.11.2 documents this design with full rationale.

---

## ERR-009: `PassThroughGround` / `PassThroughAerial` are redundant `KickType` values

**Severity:** Minor
**Detected:** February 19, 2026
**Status:** CLOSED — resolved during audit session

**Resolution:**
Through passes use the same aerodynamic profile as their non-through equivalents
(`PassGround` and `PassLofted` respectively). The distinction between a through ball
and a regular pass is entirely a Pass Mechanics targeting concern — the receiver
prediction model, lane detection, and lead distance calculation. Ball Physics sees
identical physics profiles. Separate `KickType` values were unnecessary.

The `KickType` enum was subsequently eliminated entirely (ERR-005), making this
resolution moot. Recorded for completeness.

---

## ERR-011: `SpatialHashGrid.Query()` ignores radius parameter — always returns fixed 3×3 neighbourhood

**Severity:** Major
**Detected:** February 23, 2026 (Shot Mechanics Spec #6 §4 cross-spec audit)
**Status:** CLOSED — Fixed in Collision_System_Spec_Section_3_v1_1.md; Query() now uses
dynamic neighbourhood sizing: `cellRadius = Ceil(radius / CELL_SIZE)`. Interim workaround in Shot Mechanics §4.4.1; root cause unfixed

**Root Cause:**

`SpatialHashGrid.Query(Vector3 position, float radius)` accepts a `radius` argument
but never reads it. The implementation unconditionally queries the 3×3 cell neighbourhood
around the query position (covering approximately ±1.5m regardless of the radius
argument passed). This was documented in the Collision System spec as a comment
("not currently used; 3×3 query is always sufficient") but the architectural consequence
for callers using larger pressure radii was not evaluated.

**Problem in detail:**

All three systems that query the spatial hash for pressure detection — Pass Mechanics,
Shot Mechanics, and First Touch — pass `PRESSURE_RADIUS_MAX = 3.0m` to `Query()`. The
call returns only entities within the fixed ±1.5m neighbourhood. Opponents at 1.6–3.0m
are invisible to the pressure model in all three specifications.

**Impact by system:**
- **Pass Mechanics (Spec #5):** `PassErrorCalculator` under-estimates pressure for shots
  taken with opponents at 1.6–3.0m. Passes executed under moderate pressure behave as if
  under no pressure.
- **Shot Mechanics (Spec #6):** Same effect on `ShotErrorCalculator`. Shots under
  moderate defensive pressure are not penalised correctly.
- **First Touch (Spec #4):** Same effect on `FirstTouchPressureEvaluator`. Ball control
  under moderate pressure is over-estimated.

**Interim workaround (applied in Shot Mechanics §4.4.1 v1.3):**

Callers must distance-filter the `Query()` result set after receiving it:

```csharp
List<AgentId> queriedEntities = SpatialHash.QueryRadius(center, PRESSURE_RADIUS_MAX, filter);
List<AgentId> nearbyOpponents = queriedEntities
    .Where(id => Vector3.Distance(center, AgentSystem.GetAgent(id).Position)
                 <= PRESSURE_RADIUS_MAX)
    .ToList();
```

This workaround is correct — the 3×3 neighbourhood is a superset of all entities within
3.0m (a 3.0m radius on 1.0m cells requires at most ±3 cells to capture; the 3×3 returns
±1 cells). **The workaround does NOT fully fix the defect** — opponents at 1.6–3.0m that
fall in cells beyond the ±1 neighbourhood are still missed. However, at normal match
density (22 agents on a 105×68m pitch), the probability of an opponent being at 1.6–3.0m
but outside the 3×3 neighbourhood is low. The workaround reduces the error but does not
eliminate it.

**Correct fix:**

`SpatialHashGrid.Query()` must compute a dynamic neighbourhood based on the radius
parameter:

```csharp
public List<int> Query(Vector3 position, float radius)
{
    int cellRadius = Mathf.CeilToInt(radius / SpatialHashConstants.CELL_SIZE);
    // Query (2*cellRadius+1)² cells instead of fixed 3×3
    for (int dy = -cellRadius; dy <= cellRadius; dy++)
    for (int dx = -cellRadius; dx <= cellRadius; dx++)
    { /* add cells */ }
}
```

For `PRESSURE_RADIUS_MAX = 3.0m` on 1.0m cells: `cellRadius = 3`, query covers 7×7 = 49
cells (vs current 9). Performance impact is negligible at N=22 agents.

**Files requiring revision:**

| File | Section | Change |
|------|---------|--------|
| `Collision_System_Spec_Section_3_v1_0.md` | §3.1.4 `Query()` implementation | Dynamic neighbourhood: `cellRadius = Ceil(radius / CELL_SIZE)`; iterate `(2*cellRadius+1)²` cells |
| `Pass_Mechanics_Spec_Section_4_v1_0.md` | §4.4.1 pressure query | Add interim workaround comment (or remove workaround once Collision System fixed) |
| `First_Touch_Spec_Section_4_v1_1.md` | §4.4 pressure query | Add interim workaround comment |

**Version impact:** `Collision_System_Spec_Section_3_v1_0.md` → v1.1 (when fixed)

---

## Revision Summary

| Priority | ERR ID | Blocking | Status |
|----------|--------|----------|--------|
| ~~1 — Fix before Section 3~~ | ERR-006, ERR-007, ERR-008 | ~~Yes~~ | ✅ All three closed |
| ~~2 — Fix before approval~~ | ERR-001, ERR-004 | ~~Yes~~ | ✅ Both closed in First_Touch_Spec_Section_4_v1_1.md |
| 3 — Fix at convenience | ERR-002, ERR-003 | No | Open — minor edits to Master_Vol_4 and Agent Movement §3.2 |
| **2 — Fix before Collision System approval** | **ERR-011** | **Yes (blocks Collision System §4 approval)** | **Closed — fixed in Collision_System_Spec_Section_3_v1_1.md (Mar 5, 2026)** |
| 3 — Fix at convenience before Shot Mechanics final sign-off | ERR-010 | No | ✅ Closed — fixed in shot-mechanics/section-1.md v1.2 (March 6, 2026) |
| 3 — Fix at convenience | ERR-012 | No | ✅ Closed — fixed in first-touch/section-7.md v1.1 (March 5, 2026) |

**All critical Shot Mechanics cross-spec audit defects resolved (A1–A7). ERR-011 is a
Collision System defect with an interim workaround applied — it blocks Collision System
Section 3 revision, not Shot Mechanics approval. ERR-010 is a minor documentation
error (Decision Tree spec number) in Shot Mechanics §1.1 — non-blocking on approval.**

---

**v1.4 Changes (Mar 5, 2026):
- ERR-009 (SpatialHash Query) renumbered to ERR-011 to resolve duplicate ID
  conflict with ERR-009 (KickType, closed). ERR-011 now CLOSED.

End of Error Log v1.4**

---

## ERR-012: First Touch §7 refers to Decision Tree as Spec #7 (5 occurrences)

**Severity:** Minor (documentation error; no architectural impact)
**Detected:** March 5, 2026
**Detected During:** First Touch Specification #4 comprehensive audit
**Root Cause:** Same as ERR-010 — First Touch Section 7 was written before the specification
numbering was finalised. Decision Tree was tentatively #7; Perception System was subsequently
inserted at #7, bumping Decision Tree to #8.

**Problem in detail:**
`First_Touch_Spec_Section_7_v1_0.md` references "Decision Tree Spec #7" in 5 locations:
- §7.1.4 body text: "Decision Tree (Spec #7, Stage 1)"
- §7.2.4 body text: "Decision Tree (Spec #7, Stage 1/2 scope)"
- §7.2.4 dependency line: "Decision Tree Spec #7"
- §7.6 dependency map row: "Decision Tree Spec #7 | Intent flag | Stage 1"
- §7.6 dependency map row: "Decision Tree Spec #7 | Intent flag | Stage 2"

**Correct approach:**
Replace all 5 instances of "Spec #7" (referring to Decision Tree) with "Spec #8".

**Status:** ✅ CLOSED — Fixed in `first-touch/section-7.md` (March 5, 2026, First Touch
comprehensive audit remediation).

**Files revised:**

| File | Section | Change |
|------|---------|--------|
| `first-touch/section-7.md` (was v1.0 → v1.1) | §7.1.4, §7.2.4, §7.6 | All "Decision Tree Spec #7" → "Decision Tree Spec #8" |

**Version impact:** `first-touch/section-7.md` → v1.1

---

*End of Spec Error Log v1.5 — April 22, 2026. Add new entries after this line.*

---

## ERR-016-001: Phantom interface risk in Deterministic Simulation Spec #16 §4.2

**Severity:** Medium (architectural discipline; no immediate code impact — Stage 0 spec phase)
**Detected:** May 2, 2026
**Detected During:** Deterministic Simulation Spec #16 drafting (adversarial review + v0.7 fix pass)
**Root Cause:** Same root cause as ERR-001 and ERR-004. §4.2 originally contained normative C#-shaped interface sketches (`IDeterministicRngService`, `IReplayRunner`, etc.) against consumer specs (#17 Event System, #18 Performance Optimization, #19 Testing Strategy) that are all currently `NOT STARTED`. Writing normative interface shapes before the consumer is specified creates phantom interfaces that constrain future design.

**Mitigation applied (v0.7 fix pass):**
§4.2 was reframed as explicitly **non-normative sketches** — the C# shapes are illustrative only. The §4.2.1 *behavior contract* remains normative (determinism in inputs→outputs, byte-idempotent serialization, canonical ordering in Compare output). The note at the top of §4.2 explicitly cites CLAUDE.md's "write interfaces only when both sides are specified" rule and the ERR-001/004 hazard, and prohibits promotion to normative `.cs` interfaces until consumer specs #17/#18/#19 reach at least `IN REVIEW`.

**Status:** ✅ MITIGATED — phantom interface risk contained by non-normative classification. Full resolution requires co-authoring final interface shapes with specs #17/#18/#19.

**Files revised:**

| File | Section | Change |
|------|---------|--------|
| `docs/specs/deterministic-sim/section-4.md` | §4.2 preamble | Added non-normative disclaimer and phantom-interface hazard citation |

---

*End of Spec Error Log v1.6 — May 2, 2026.*

---

## ERR-016-002: EntityId no-reuse cross-spec constraint not back-propagated

**Severity:** Medium (consistency/discipline; latent integrity hazard if specs #2/#8 silently reuse EntityIds during a match)
**Detected:** May 3, 2026
**Detected During:** Deterministic Simulation Spec #16 third-pass adversarial critique (finding M-F)
**Root Cause:** Deterministic Simulation §3.2.5 declares a normative constraint binding two already-APPROVED specs:

> "entity allocators in Agent Movement (#2) and the AI subsystem (Decision Tree #8) MUST guarantee EntityId uniqueness for the lifetime of a match; once an EntityId is despawned it MUST NOT be reassigned."

This is the renumbering-cascade hazard CLAUDE.md flags: a downstream spec adding a normative constraint to upstream specs after they have been approved, without filing reciprocal `XC-` cross-references in those specs. As of May 3, 2026, neither Agent Movement (#2) nor Decision Tree (#8) carries a corresponding `XC-` reference to Deterministic Simulation §3.2.5; the constraint is "floating".

**Problem in detail:**
- Agent Movement #2 was approved Apr 27, 2026.
- Decision Tree #8 was approved Apr 27, 2026 (at draft-level rigor).
- The EntityId no-reuse constraint is necessary for #16's RNG stream isolation and replay parity, but is unenforceable until specs #2 and #8 explicitly carry it.
- Without back-propagation, an implementer of Agent Movement could legitimately recycle a despawned EntityId to a new agent on the same tick. This would silently break per-stream RNG cursor isolation in Deterministic Simulation, manifesting only as a hard desync at replay time.

**Required fix:**
1. Add an `XC-002-NNN` cross-reference in Agent Movement #2 §3 (entity allocator) citing Deterministic Simulation §3.2.5; declare the no-reuse constraint normatively in #2's own constants/contracts.
2. Add an `XC-008-NNN` cross-reference in Decision Tree #8 (subsystem entity allocation, if any) likewise.
3. File the back-propagation as a minor revision of both specs, version-bumped (no behavioral changes; constraint is consistent with how a sane allocator would behave anyway).
4. Once both reciprocal references exist, mark this entry CLOSED.

**Status:** ✅ FULLY RESOLVED — May 18, 2026. All three required steps confirmed complete:
1. Agent Movement #2 §2.5 as `XC-002-001` (v1.1.1, non-behavioral patch) — landed May 6, 2026.
2. Decision Tree #8 §1.7.3 as `XC-008-001` (v1.1.1, non-behavioral patch) — landed May 6, 2026.
3. `docs/specs/deterministic-sim/section-3.md` §3.2.5 prose confirmed updated from "filed for back-propagation" to "back-propagated to #2 §2.5 and #8 §1.7.3" (verified by OBS-1 probe in stress-test Tier A Run 2, May 18, 2026). CLAUDE.md OPEN ISSUES entry removed.

**Files revised:**

| File | Section | Change |
|---|---|---|
| `docs/specs/agent-movement/section-1-2.md` | New §2.5 | `XC-002-001` (EntityId no-reuse). v1.1.1 patch. |
| `docs/specs/decision-tree/section-1.md` | New §1.7.3 | `XC-008-001` (EntityId no-reuse). v1.1.1 patch. |
| `docs/specs/deterministic-sim/section-3.md` §3.2.5 | post-fix prose | Pending: update "filed for back-propagation" line. |

**Version impact:** Patch revision (v1.1 → v1.1.1) of Agent Movement #2 and Decision Tree #8 — no behavioral change; constraint formalizes existing sensible allocator behavior.

---

## ERR-017-001: `DOMAIN_TAG_EVENT_LEDGER` allocation required in Deterministic Simulation #16 §3.4

**Severity:** Medium (cross-spec back-prop; latent if not landed before #17 IN REVIEW)
**Detected:** May 12, 2026
**Detected During:** PASS 2 adversarial review of `event-system/outline-detailed.md` v1.0 (finding 3)
**Root Cause:** Event System #17 §3.4.2 declares the `Events`-phase digest preimage as `SerializeCanonical(DOMAIN_TAG_EVENT_LEDGER ‖ EventLedgerRecord[T])`. This domain-tag entry is normatively owned by Deterministic Simulation #16 §3.4's domain-tag table, but no allocation exists there. There is no documented mechanism by which a downstream spec registers a domain-tag need with #16; the dependency direction (#17 cites #16) makes this a chicken-and-egg.

**Problem in detail:**
- Spec #17 needs a stable numeric `DOMAIN_TAG_EVENT_LEDGER` to commit its FM-017-001 formula to.
- Spec #16 §3.4 currently does not enumerate `EVENT_LEDGER` among its allocated domain tags.
- Without back-prop, #17 cannot reach `APPROVED` (its `[CROSS-PENDING]` constant cannot promote to `[CROSS]`).
- The same hazard class as ERR-016-002 (downstream spec adds normative constraint on upstream after the upstream's review pass).

**Required fix:**
1. At `event-system/outline-detailed.md` reaching IN REVIEW, file a patch to `docs/specs/deterministic-sim/section-3.md` §3.4 domain-tag table allocating `DOMAIN_TAG_EVENT_LEDGER` (next available numeric value in #16's tag-namespace).
2. Update §3.10 constants catalogue in `event-system/outline-detailed.md` (and any drafted §3 section file) to pin the literal value and promote `[CROSS-PENDING]` → `[CROSS]` at the same beat that resolves the citation's `TBD-NORMATIVE` tag (gated on #16 reaching `APPROVED` per KD-2).
3. Once the allocation lands in #16, mark this entry CLOSED.

**Status:** ✅ FULLY RESOLVED.

- **#16-side — May 14, 2026.** `DOMAIN_TAG_EVENT_LEDGER = 0x15` allocated in `docs/specs/deterministic-sim/section-3.md` §3.4 (next value after `DOMAIN_TAG_ENV_FP = 0x14`); §3.5 v1.0.1 patch-revision history entry recorded; §8.3.1 #17 row promoted `pending re-audit → complete` atomically with this resolution; §8 v1.2 version-history entry recorded.
- **#17-side — May 15, 2026 (#17 §1.0.1 patch revision).** `[CROSS-PENDING]` → `[CROSS]` promotion completed and literal value `0x15` inlined across `docs/specs/event-system/`: §3.4.2 prose; §3.10 constants catalogue row + trailing-notes paragraph; §1.4 cross-spec-constants-imported summary; §2.4.4 `EventLedgerRecord` preimage description; §7.5 D9 deferred-decisions row (RESOLVED); §8.1.4 ERR-017-001 row; §8.3.4 imported-constants table (heading renamed `[CROSS]` constants imported); §8.4 constant-provenance summary row; §9.2 Q10 quality-checklist row; §9.3 R3 review-checklist row; Appendix B preamble + B.1 / B.2 / B.3 byte streams (symbolic `DT` replaced with literal `15`); Appendix D glossary row. Section-version histories on §1 / §2 / §3 / §7 / §8 / §9 / appendices each carry a v1.0.1 row recording the patch.

**Files revised at #16 side:**

| File | Section | Change |
|---|---|---|
| `docs/specs/deterministic-sim/section-3.md` | §3.4 constants catalogue | Added `DOMAIN_TAG_EVENT_LEDGER = 0x15` `[FIXED]` row citing ERR-017-001 |
| `docs/specs/deterministic-sim/section-3.md` | §3.5 version history | v1.0.1 patch-revision entry recording the allocation and rationale (no `DETERMINISM_DIGEST_VERSION` bump) |
| `docs/specs/deterministic-sim/section-8.md` | §8.3.1 audit table + §8.5 v1.2 | #17 row promoted to `complete`; ERR-017-001 closure recorded |

**Files revised at #17 side (May 15, 2026; §1.0.1 patch revision):**

| File | Section | Change |
|---|---|---|
| `docs/specs/event-system/section-1.md` | §1.4 | `[CROSS-PENDING]` → `[CROSS]`; literal value `0x15` inlined; ERR-017-001 marked RESOLVED |
| `docs/specs/event-system/section-2.md` | §2.4.4 | `EventLedgerRecord` preimage prose updated to `0x15` / `[CROSS]` |
| `docs/specs/event-system/section-3.md` | §3.4.2, §3.10 + trailing notes | `[CROSS-PENDING]` → `[CROSS]`; literal value `0x15` inlined in formula prose and constants catalogue |
| `docs/specs/event-system/section-7.md` | §7.5 D9 | Deferred-decision row marked RESOLVED with `0x15` |
| `docs/specs/event-system/section-8.md` | §8.1.4 ERR-017-001, §8.3.4 heading + row, §8.4 row | ERR-017-001 RESOLVED; `[CROSS]` table and provenance summary updated to `0x15` |
| `docs/specs/event-system/section-9-approval-checklist.md` | §9.2 Q10, §9.3 R3 | Evidence rows updated to reflect `[CROSS]` promotion and ERR-017-001 RESOLVED |
| `docs/specs/event-system/appendices.md` | Appendix B preamble + B.1 / B.2 / B.3, Appendix D | Byte streams inline literal `15`; glossary row updated to `0x15` / `[CROSS]` |

**Version impact:** Patch revision (`v1.0` → `v1.0.1`) on the #16 side (§3.5) and on the #17 side (sections 1, 2, 3, 7, 8, 9-approval-checklist, appendices). No behavioral change on either side; pure namespace allocation in #16 (catalogue grew; no preimage layout, field width, or hash-input rule changed; no `DETERMINISM_DIGEST_VERSION` bump) and pure tag/value substitution in #17 (no FR text changed, no formula re-derived).

---

## ERR-010-001: `DOMAIN_TAG_HEADING` allocation required in Deterministic Simulation #16 §3.4

**Severity:** Medium (cross-spec back-prop; latent if not landed before #10 APPROVED)
**Detected:** May 16, 2026
**Detected During:** Section-files v0.1 → v0.2 PASS-1 adversarial-review fix pass (`heading-mechanics/adversarial-review-section-files-v1.md` finding M-1). v0.1 KD-10 / Appendix G / §9.4 OI-001 each claimed the entry was "created during section authoring", but `grep ERR-010 docs/tracking/spec-error-log.md` returned only the long-closed ERR-010 (Shot Mechanics renumbering; March 6, 2026). v0.2 files this row.
**Root Cause:** Heading Mechanics #10 §3.4 + §3.7 route Gaussian and float draws through `DeterministicRngService` (Deterministic Simulation #16 §4.1) keyed on `DOMAIN_TAG_HEADING`. This domain-tag entry is normatively owned by #16 §3.4's domain-tag table, but no allocation exists there yet. Same hazard class and same resolution shape as `ERR-017-001` (Event System #17 / `DOMAIN_TAG_EVENT_LEDGER = 0x15`, closed May 15, 2026).

**Problem in detail:**
- Spec #10 needs a stable numeric `DOMAIN_TAG_HEADING` to commit its three draw-site IDs (`DRAW_SITE_DUEL_TIEBREAK`, `DRAW_SITE_CONTACT_POINT_ERROR`, `DRAW_SITE_TIMING_JITTER`) to.
- Spec #16 §3.4 currently does not enumerate `HEADING` among its allocated domain tags.
- Without back-prop, #10 cannot reach `APPROVED` (its `[CROSS-PENDING]` constant in §3.1 cannot promote to `[CROSS]`).
- Next available numeric slot in #16 §3.4's tag-namespace is `0x16` (verified May 16, 2026: current allocations run `0x10`..`0x15`).

**Required fix:**
1. At `heading-mechanics/SPEC_INDEX.md` row 10 reaching `IN REVIEW`, file a patch to `docs/specs/deterministic-sim/section-3.md` §3.4 domain-tag table allocating `DOMAIN_TAG_HEADING = 0x16` (next available numeric value in #16's tag-namespace). Pure namespace allocation — no `DETERMINISM_DIGEST_VERSION` bump required, per the `ERR-017-001` precedent (#16 §3.5 v1.0.1 patch revision, May 14, 2026).
2. Update §3.1 Master Physical Profile Table in `heading-mechanics/section-3.md` to pin the literal value `0x16` and promote `[CROSS-PENDING]` → `[CROSS]` at the same beat that #16's allocation lands.
3. Once the allocation lands in #16, mark this entry CLOSED.

**Status:** ✅ FULLY RESOLVED.

- **#16-side — May 16, 2026.** `DOMAIN_TAG_HEADING = 0x16` allocated in `docs/specs/deterministic-sim/section-3.md` §3.4 (next value after `DOMAIN_TAG_EVENT_LEDGER = 0x15`); §3.5 v1.0.2 patch-revision history entry recorded. Pure namespace allocation in #16's tag-namespace; no `DETERMINISM_DIGEST_VERSION` bump (catalogue grew; no preimage layout, field width, or hash-input rule changed). Follows the v1.0.1 / ERR-017-001 precedent exactly.
- **#10-side — May 16, 2026 (#10 v0.3 patch revision).** `[CROSS-PENDING]` → `[CROSS]` promotion completed in `heading-mechanics/section-3.md` §3.1 Master Physical Profile Table; literal value `0x16` retained; ERR-010-001 reference updated `pending → RESOLVED`. §1.3 KD-10 wording updated; §1.4 dependency table updated; §8.2 / §8.4 / §9.1 / §9.2 / §9.4 OI-001 status rows all updated. Section-version histories on §1 / §3 / §9 / appendices each carry a v0.3 row recording the patch.

**Files revised at #16 side:**

| File | Section | Change |
|---|---|---|
| `docs/specs/deterministic-sim/section-3.md` | §3.4 constants catalogue | Added `DOMAIN_TAG_HEADING = 0x16` `[FIXED]` row citing ERR-010-001 |
| `docs/specs/deterministic-sim/section-3.md` | §3.5 version history | v1.0.2 patch-revision entry recording the allocation and rationale (no `DETERMINISM_DIGEST_VERSION` bump) |

**Files revised at #10 side (May 16, 2026; v0.3 patch revision):**

| File | Section | Change |
|---|---|---|
| `docs/specs/heading-mechanics/section-1.md` | §1.3 KD-10, §1.4 | Wording updated to reflect RESOLVED filing; #16 anchor pinned |
| `docs/specs/heading-mechanics/section-3.md` | §3.1 | `[CROSS-PENDING]` → `[CROSS]`; literal value `0x16` retained |
| `docs/specs/heading-mechanics/section-8.md` | §8.2, §8.4 | XC-010-004 row marked RESOLVED; #16 row updated |
| `docs/specs/heading-mechanics/section-9-approval-checklist.md` | §9.1, §9.2, §9.4 OI-001, §9.5 | All checklist rows referencing OI-001 / `DOMAIN_TAG_HEADING` checked/RESOLVED |
| `docs/specs/heading-mechanics/appendices.md` | Appendix G | OI-001 status updated to RESOLVED |

**Version impact:** Patch revision (#16 §3.5: `v1.0.1 → v1.0.2`; #10 sections: `v0.2 → v0.3`). No behavioral change on either side; pure namespace allocation in #16 and pure tag-promotion in #10.

---

## ERR-011-001: `DOMAIN_TAG_GOALKEEPER` allocation required in Deterministic Simulation #16 §3.4

**Severity:** Medium (cross-spec back-prop; latent if not landed before #11 APPROVED)
**Detected:** May 16, 2026
**Detected During:** Section-files v0.1 → v0.2 PASS-1 adversarial-review fix pass (`goalkeeper-mechanics/adversarial-review-section-files-v1.md`). Filed at the moment Goalkeeper Mechanics #11 section files v0.2 land and `SPEC_INDEX.md` row 11 flips `NOT STARTED → IN REVIEW`.

**Root Cause:** Goalkeeper Mechanics #11 §3.3 / §3.5 / §3.6 route Gaussian draws through `DeterministicRngService` (Deterministic Simulation #16 §4.1) keyed on `DOMAIN_TAG_GOALKEEPER`. Same hazard class and same resolution shape as `ERR-010-001` (Heading #10 / `DOMAIN_TAG_HEADING = 0x16`, closed May 16, 2026) and `ERR-017-001` (Event System #17 / `DOMAIN_TAG_EVENT_LEDGER = 0x15`, closed May 15, 2026).

**Problem in detail:**
- Spec #11 needs a stable numeric `DOMAIN_TAG_GOALKEEPER` to commit its four draw-site IDs (`DRAW_SITE_HANDLING_NOISE`, `DRAW_SITE_HANDLING_POINT_NOISE`, `DRAW_SITE_DIVE_TIMING_JITTER`, `DRAW_SITE_CROSS_CLAIM_TIEBREAK`) to.
- Spec #16 §3.4 currently does not enumerate `GOALKEEPER` among its allocated domain tags.
- Without back-prop, #11 cannot reach `APPROVED` (its `[CROSS-PENDING]` constant in §3.4 cannot promote to `[CROSS]`).
- **Collision-management policy (KD-7).** Open ERR-012-001 proposes block `0x17…0x1C` for Positioning AI #12 Phase B/C; whichever spec reaches `APPROVED` first takes `0x17`. If ERR-011-001 lands first, the #12 block re-shifts to `0x18…0x1D` (mirroring the May 16, 2026 #10 / #12 shift via ERR-010-001 vs. ERR-012-001). If ERR-012-001 lands first, `DOMAIN_TAG_GOALKEEPER` shifts to `0x1D`. The `[CROSS-PENDING]` tag accommodates either outcome.

**Required fix:**
1. At `goalkeeper-mechanics/SPEC_INDEX.md` row 11 reaching `APPROVED`, file a patch to `docs/specs/deterministic-sim/section-3.md` §3.4 domain-tag table allocating `DOMAIN_TAG_GOALKEEPER`. Numeric value depends on collision-management outcome (`0x17` or `0x1D`). Pure namespace allocation — no `DETERMINISM_DIGEST_VERSION` bump, per ERR-010-001 / ERR-017-001 precedent.
2. Update §3.4.9 in `goalkeeper-mechanics/section-3.md` to pin the literal value and promote `[CROSS-PENDING]` → `[CROSS]` at the same beat that #16's allocation lands.
3. Once the allocation lands in #16, mark this entry CLOSED.

**Status:** ✅ Resolved May 18, 2026 — `DOMAIN_TAG_GOALKEEPER = 0x1D` allocated in #16 §3.4 v1.0.5 (Positioning AI #12 reached APPROVED first and claimed `0x17`; per KD-7 first-to-APPROVED precedent GK shifted to `0x1D`); #11 §3.4.9 `[CROSS-PENDING]` → `[CROSS: #16 §3.4]` promoted atomically with #16 back-prop landing.

---

*End of Spec Error Log v1.11 — May 16, 2026.*

---

## ERR-010: Shot Mechanics §1.1 refers to Decision Tree as Spec #7

**Severity:** Minor (documentation error; no architectural impact)  
**Detected:** February 27, 2026  
**Detected During:** Decision Tree Specification #8 Outline v1.1 pre-approval review (BLK-001)  
**Root Cause:** Shot Mechanics Specification #6 was written before the specification
numbering was finalised. At time of authoring, the Decision Tree was tentatively
assigned #7. Perception System was subsequently inserted at #7, bumping Decision Tree
to #8. The Shot Mechanics text was not updated.

**Problem in detail:**  
`Shot_Mechanics_Spec_Section_1_v1_1.md` §1.1 Dependencies section references:
> "Decision Tree Specification #7"

The canonical specification number for the Decision Tree, as recorded in
`PROGRESS.md` (authoritative), `FILE_MANIFEST.md`, and Perception System
Specification #7 §1.1, is **#8**.

This creates an inconsistency that could mislead implementers cross-referencing
Shot Mechanics with Decision Tree documentation.

**Correct approach:**  
Replace all instances of "Decision Tree Specification #7" with "Decision Tree
Specification #8" in `Shot_Mechanics_Spec_Section_1_v1_1.md`.

**Blocking condition:**  
This error is non-blocking on Shot Mechanics approval (the architectural content is
correct; only the number is wrong). However, it **must be closed before**:
1. Shot Mechanics receives final lead developer sign-off, and
2. Decision Tree Specification #8 Section 4 (interface contracts) is written and
   references Shot Mechanics as a dependency by number.

**Files requiring revision:**

| File | Section | Change |
|------|---------|--------|
| `Shot_Mechanics_Spec_Section_1_v1_1.md` | §1.1 Dependencies table, any other references | Replace "Spec #7" with "Spec #8" for Decision Tree |

**Version impact:** No version increment required for minor text correction. Document
in Shot Mechanics changelog when the edit is made.

---

## ERR-018-002: `[HotPathAllocExempt]` cited as declared in Spec #20 §3 but does not exist there

**Status:** ✅ Resolved — May 14, 2026 (#18 section-file v0.2; option-2 path; Spec #20 not touched).
**Severity:** High (citation of APPROVED spec for content it does not contain — matches CLAUDE.md "fabricated checklist values" hazard class)
**Detected:** May 14, 2026
**Detected During:** PASS-1 adversarial review of Performance Optimization #18 section files v0.1
**Root Cause:** The `[HotPathAllocExempt]` C# attribute is referenced as a key allocation-exemption mechanism in five locations in #18, every one of which treats the attribute as already declared in Spec #20 §3 (APPROVED May 11, 2026). Grep against the entire `code-standards/` folder returns zero hits for `HotPathAllocExempt` or any allocation-exemption attribute. The attribute is not declared in Spec #20.

**Problem in detail:**

Cited locations:
- `section-2.md` FR-PO-053: "exempt via `[HotPathAllocExempt]` (declared in Spec #20 §3, cite-not-redefine per KD-1)"
- `section-3.md` §3.1.2: "exempted via `[HotPathAllocExempt]` (cite Spec #20 §3)"
- `section-3.md` §3.7.5: "exempted via the `[HotPathAllocExempt]` attribute declared in Spec #20 §3"
- `section-8.md` §8.1.4: "§3 `[HotPathAllocExempt]` attribute (cited by §3.7.5, FR-PO-053)"
- `appendices.md` Appendix B: "Exemptions require `[HotPathAllocExempt]` per Spec #20 §3"

§3.7.5 itself hedges with "Coordinate with the #20 author if the attribute is not yet declared … attribute presence to be verified at first `src/` commit," which directly contradicts the surrounding "declared in Spec #20 §3" claim. The spec is simultaneously asserting the attribute exists in #20 and acknowledging it may not.

**Required fix (choose one):**

1. **Update Spec #20 §3** to formally declare the `[HotPathAllocExempt]` attribute with version-history entry and lead-developer re-sign-off (Spec #20 is APPROVED; any spec change requires sign-off per CLAUDE.md). Spec #18 citations then resolve.
2. **Move ownership to Spec #18** — declare the attribute in #18 §3.7 directly; drop the KD-1 cite-not-redefine framing for this case. Update Spec #20's `[HotPathAllocExempt]` row only if/when #20 adopts it.
3. **Tag as `[CROSS-PENDING]`** — treat the attribute name as a cross-spec constant gated on a future Spec #20 patch; file the back-prop expectation here and in #18's body text.

Option (2) has the smallest cross-spec blast radius because #20 is APPROVED and (1) would require re-review.

**Files requiring revision (per resolution path chosen):**

| File | Section | Change |
|---|---|---|
| `docs/specs/performance-optimization/section-2.md` | FR-PO-053 | Reword to remove "declared in Spec #20 §3" claim |
| `docs/specs/performance-optimization/section-3.md` | §3.1.2, §3.7.5 | Same |
| `docs/specs/performance-optimization/section-8.md` | §8.1.4 | Same |
| `docs/specs/performance-optimization/appendices.md` | Appendix B | Same |
| `docs/specs/code-standards/section-3.md` (option 1 only) | §3 | Add attribute declaration |

**Version impact:** #18 section-file revision (v0.1 → v0.2). Option (1) additionally bumps Spec #20 (re-review required).

**Resolution (May 14, 2026):** Option (2) applied. `section-3.md` §3.7.5, `section-2.md` FR-PO-053, and `appendices.md` Appendix B all updated. `[HotPathAllocExempt]` declared as Spec #18 §3.7.5 governance identifier. Spec #20 unchanged.

---

## ERR-018-003: MUST/MAY conflict between FR-PO-067 and §3.4.4 on baseline-reproducibility re-run

**Status:** ✅ Resolved — May 14, 2026 (#18 section-file v0.2; §3.4.4 upgraded MAY → MUST with Stage 0 carve-out).
**Severity:** High (binding-requirement contradiction within the same spec)
**Detected:** May 14, 2026
**Detected During:** PASS-1 review of #18 section files v0.1
**Root Cause:** FR-PO-067 in `section-2.md §2.2.9` states the baseline-reproducibility auditor **MUST** re-run the recorded session manifest. §3.4.4 in `section-3.md` (the implementing mechanics section for that FR) states the validator **MAY** re-run the session. §2 is the binding-requirement section; §3 is the implementing mechanics. The verbs disagree directly on the same action.

**Problem in detail:**

FR-PO-067 (normative MUST): *"The §5.4 baseline-reproducibility auditor MUST re-run the recorded session manifest and confirm the recaptured metric matches within §3.4.3 confidence interval."*

§3.4.4 (mechanics MAY): *"Reproducibility check (Stage 0+1): the validator MAY re-run the session under the recorded seed + fingerprint + platform pin and confirm the captured metric matches within the §3.4.3 confidence interval."*

FR-PO-068 makes failure to re-run a merge-blocking event. The §3.4.4 "MAY" would allow the validator to silently skip the check without triggering FR-PO-068's block.

**Required fix:**

Either upgrade §3.4.4 to "MUST re-run" (aligning §3 with §2's binding requirement), or downgrade FR-PO-067 to SHOULD (aligning §2 with §3's permissive mechanic). FR-PO-068's merge-blocking semantics push toward the MUST resolution.

**Files requiring revision:**

| File | Section | Change |
|---|---|---|
| `docs/specs/performance-optimization/section-3.md` | §3.4.4 | "MAY" → "MUST" (recommended) |

**Version impact:** #18 section-file revision (v0.1 → v0.2).

**Resolution (May 14, 2026):** `section-3.md` §3.4.4 "MAY" → "MUST". FR-PO-067 (MUST) and §3.4.4 (now MUST) are consistent.

---

## ERR-018-004: Three-way stage-of-resolution contradiction on +5% threshold (FR-PO-031 / §7.5 D9 / §7.1)

**Status:** ✅ Resolved — May 14, 2026 (#18 section-file v0.2; §7.5 D9 re-anchored Stage 0+1 to match FR-PO-031 and §7.1).
**Severity:** High (three locations in the same spec state three different resolution stages for the same governance number)
**Detected:** May 14, 2026
**Detected During:** PASS-1 review
**Root Cause:** The +5% per-PR regression threshold (`[GT]` governance number) has its resolution stage stated three times with three different answers.

**Problem in detail:**

- **FR-PO-031** (`section-2.md §2.2.5`): "`[GT]` pinned at Stage 0+1 §7.5 D9" — implies pin at Stage 0+1.
- **§7.5 D9** (`section-7.md`): "Resolution stage: Stage 1 | Notes: Tie to first-month variance measurement" — explicit Stage 1.
- **§7.1** (`section-7.md`) Stage 0+1 Transition Deliverables: "§3.5.2 +5% threshold re-evaluated against actual baseline variance" — listed as Stage 0+1 deliverable.

The three statements cannot all be true. Either the threshold is pinned/re-evaluated at Stage 0+1 (FR-PO-031 + §7.1) and D9 is wrong, or D9 is correct and FR-PO-031 + §7.1 are wrong.

**Required fix:**

Choose one canonical stage and update all three locations. Recommended: Stage 0+1 (matches FR-PO-031 + §7.1 which jointly outvote D9; matches the operational reality that you can't gate Stage 0+1 CI on a Stage-1 threshold).

**Files requiring revision:**

| File | Section | Change |
|---|---|---|
| `docs/specs/performance-optimization/section-7.md` | §7.5 D9 | "Stage 1" → "Stage 0+1" (under recommended resolution) |

**Version impact:** #18 section-file revision (v0.1 → v0.2).

**Resolution (May 14, 2026):** `section-7.md` §7.5 D9 resolution stage changed from "Stage 1" to "Stage 0+1". All three locations (FR-PO-031, §7.1, §7.5 D9) now consistently state Stage 0+1.

---

## ERR-018-005: Channel registry schema absent from Appendix F; §3.8.2 "Stage 0 declares schema" obligation unmet

**Status:** ✅ Resolved — May 14, 2026 (#18 section-file v0.2; new **Appendix F.0 Channel Registry Schema** authored with 12 schema fields; §3.8.2 channel-registry bullet rewritten to cite F.0 as the Stage 0 schema deliverable).
**Severity:** High (declared Stage 0 deliverable is missing; channel names used without registry backing)
**Detected:** May 14, 2026
**Detected During:** PASS-1 review
**Root Cause:** §3.8.2 in `section-3.md` explicitly states the channel registry is a Stage 1 deliverable but the **schema** for the registry is a Stage 0 deliverable to be published in Appendix F. Appendix F as written contains only F.1–F.5 dashboard schemas; there is no channel registry schema. Compounding this, F.1, F.2, and F.4 reference channel names (`perf.budget`, `perf.alloc`) as data sources without those channels having registry entries.

**Problem in detail:**

§3.8.2: *"Channel registry. Named channels per subsystem, declared in Appendix F catalogue (Stage 1 deliverable; **Stage 0 declares schema**)."*

Appendix F section headings: F.1 Per-Spec Per-Tick Budget Dashboard, F.2 Per-PR Delta Dashboard, F.3 Milestone-Baseline Trend Dashboard, F.4 Allocation-Tracker Dashboard, F.5 Flake/Determinism Cross-Reference Dashboard. All five are dashboard schemas; none is a channel registry schema. No section in Appendix F defines what fields a channel registry entry carries (channel name, owning subsystem, default verbosity level, sampling rule, sink routing, determinism class, etc.).

**Required fix:**

Author an "Appendix F.0 — Channel Registry Schema" (or "Appendix H — Channel Registry Schema") before F.1, declaring the schema fields per channel entry. Stage 0 deliverable; populated entries are Stage 1.

**Files requiring revision:**

| File | Section | Change |
|---|---|---|
| `docs/specs/performance-optimization/appendices.md` | New Appendix F.0 / H | Add channel registry schema headers (channel name, subsystem, verbosity, sampling rule, sink, determinism class) |

**Version impact:** #18 appendices revision (v0.1 → v0.2).

**Resolution (May 14, 2026):** Appendix F.0 "Channel Registry Schema" added to `appendices.md` with full field schema (channel_name, subsystem_owner, verbosity_tier_min, sink_targets, emission_veto_required, record_format, declared_stage) and Stage 0 channel registry table with three seed entries (perf.budget, perf.alloc, perf.trace).

---

## ERR-018-006: Hot-path allocation budget = 0 bytes/tick tagged `[GT]` instead of `[FIXED]` in §3.10

**Status:** ✅ Resolved — May 14, 2026 (#18 section-file v0.2; §3.10 row re-tagged `[GT]` → `[FIXED]`; §8.4 mirror row updated).
**Severity:** Medium (constant-tag misclassification; implies designer-tunability of an architectural mandate)
**Detected:** May 14, 2026
**Detected During:** PASS-1 review
**Root Cause:** `section-3.md` §3.10 tags "Hot-path allocation budget = 0 bytes/tick" as `[GT]`. Per CLAUDE.md "Constant Tags" table, `[GT]` = "Gameplay-Tuned; Designer sets value; must live in tunable config." The zero-allocation budget is a non-negotiable architectural mandate from CLAUDE.md "When Writing Code: zero-allocation architecture in the game loop" — not a designer-settable value. Tagging it `[GT]` creates a false implication that a game designer could change it.

**Required fix:**

Re-tag as `[FIXED]` ("invariant by project mandate") or remove from the constants catalogue entirely and treat as a pure CLAUDE.md cite. FR-PO-050's "MUST declare allocation budget = 0 bytes per tick" reinforces the non-tunable nature.

**Files requiring revision:**

| File | Section | Change |
|---|---|---|
| `docs/specs/performance-optimization/section-3.md` | §3.10 Constants Catalogue | "Hot-path allocation budget = 0 bytes/tick" tag `[GT]` → `[FIXED]` |
| `docs/specs/performance-optimization/section-8.md` | §8.4 Constant Provenance Summary | Mirror the tag change |

**Version impact:** #18 section-file revision (v0.1 → v0.2).

**Resolution (May 14, 2026):** `section-3.md` §3.10 tag updated `[GT]` → `[FIXED]`; rationale updated to "non-tunable invariant". `section-8.md` §8.4 mirrored.

---

## ERR-018-007: Three Spec #19 body-text citations missing `TBD-NORMATIVE` tag

**Status:** ✅ Resolved — May 14, 2026 (#18 section-file v0.2; `TBD-NORMATIVE` added to §3.3.5, §3.4.3, §3.9.5; §9.4.1 #19 blocker list extended).
**Severity:** Medium (KD-4 status caveat violated; §9.4.1 blocker list incomplete)
**Detected:** May 14, 2026
**Detected During:** PASS-1 review
**Root Cause:** KD-4 mandates that every Spec #19 citation in #18 carry a `TBD-NORMATIVE` tag because #19 is `IN REVIEW`. §9.4.1 enumerates blocked sections — but three #19 body-text citations are absent from that list and carry no tag.

**Problem in detail:**

1. **`section-3.md` §3.4.3:** *"provisional value 30 samples / 95% CI per Spec #19 §3.4.3 parallel convention"* — no `TBD-NORMATIVE`; not in §9.4.1.
2. **`section-3.md` §3.3.5:** *"selection criteria parallel Spec #19 §6.1 — must support deterministic re-play …"* — no `TBD-NORMATIVE`; not in §9.4.1.
3. **`section-3.md` §3.9.5:** *"owned by Spec #19 §3.1 end-to-end / soak layer for test execution"* — no `TBD-NORMATIVE`; not in §9.4.1.

All three would silently rot if #19's section numbering shifts before #18 is approved.

**Required fix:**

Add `(TBD-NORMATIVE)` parenthetical to each citation and add §3.4.3, §3.3.5, §3.9.5 to §9.4.1's #19 blocker list.

**Files requiring revision:**

| File | Section | Change |
|---|---|---|
| `docs/specs/performance-optimization/section-3.md` | §3.4.3, §3.3.5, §3.9.5 | Add `TBD-NORMATIVE` tag to each #19 citation |
| `docs/specs/performance-optimization/section-9-approval-checklist.md` | §9.4.1 | Add §3.4.3, §3.3.5, §3.9.5 to #19 blocker list |

**Version impact:** #18 section-file revision (v0.1 → v0.2).

**Resolution (May 14, 2026):** `(TBD-NORMATIVE)` added to all three citations in `section-3.md`. `section-9-approval-checklist.md` §9.4.1 #19 blocker list extended with §3.3.5, §3.4.3, §3.9.5.

---

## ERR-018-008: §3.9.1 ±20% promotion tolerance untagged and absent from constants catalogue

**Status:** ✅ Resolved — May 14, 2026 (#18 section-file v0.2; inline `[GT]` tag at §3.9.1; new ±20% row in §3.10 + §8.4 with rationale).
**Severity:** Medium (untagged constant; CLAUDE.md requires source tag on every constant in every spec)
**Detected:** May 14, 2026
**Detected During:** PASS-1 review
**Root Cause:** `section-3.md` §3.9.1 declares: *"the first Stage 0+1 baseline capture promotes the estimate to a measured value tagged `[GT]` if within ±20% of estimate, or files an `ERR-018-NNN` review finding if not."* The ±20% threshold governs whether a spec's implementation matches its design estimate — a consequential governance number. It carries no `[GT]`/`[EST]`/`[FIXED]` tag and is absent from §3.10's constants catalogue.

**Required fix:**

Add the ±20% threshold to §3.10's table with `[GT]` tag and rationale (e.g., "twice the +5% per-PR threshold for first-measurement variance"). Also add to §8.4 constant-provenance summary.

**Files requiring revision:**

| File | Section | Change |
|---|---|---|
| `docs/specs/performance-optimization/section-3.md` | §3.9.1 | Append `[GT]` tag to ±20% |
| `docs/specs/performance-optimization/section-3.md` | §3.10 | Add ±20% row with `[GT]` and rationale |
| `docs/specs/performance-optimization/section-8.md` | §8.4 | Mirror row |

**Version impact:** #18 section-file revision (v0.1 → v0.2).

**Resolution (May 14, 2026):** `[GT]` tag added inline in `section-3.md` §3.9.1. §3.10 row added: "±20% acceptance tolerance `[GT]`". `section-8.md` §8.4 mirrored.

---

## ERR-018-009: FR-PO-070 (Stage 0 MUST) requires invoking Stage 0+1 tooling

**Status:** ✅ Resolved — May 14, 2026 (#18 section-file v0.2; option (b) — FR-PO-070 split Stage 0 manual / Stage 0+1 automated; §5.2 activation row and §5.6 traceability row updated).
**Severity:** Medium (FR activation-stage / tooling-availability mismatch)
**Detected:** May 14, 2026
**Detected During:** PASS-1 review
**Root Cause:** FR-PO-070 (`section-2.md §2.2.10`) has activation stage Stage 0 and MUST-level binding: *"`tools/run-perf-local.sh` (Appendix E) MUST invoke the §5.3 schema-conformance auditor and §5.5 loop-tag auditor against `docs/specs/` only."* Appendix E's shell script invokes `python3 tools/budget-auditor.py`, which §7.1 lists as a Stage 0+1 deliverable. At Stage 0 the tool does not exist; the script as written cannot run.

**Problem in detail:**

Appendix E partially acknowledges this: *"`tools/budget-auditor.py` and `tools/perf-harness/run.sh` are Stage 0+1 deliverables (§7.1). At Stage 0 the auditor's behaviour is a manual review against §3.1.2 schema and §3.2.2 loop-tag mandate; the script above is the structure into which the automated implementation will land."* But FR-PO-070's MUST language and "Stage 0" activation do not reflect this caveat.

**Required fix:**

Either (a) move FR-PO-070 to "Stage 0+1" activation stage in §2.2.10 — matching when its tool dependencies exist — or (b) keep at Stage 0 but qualify the MUST to "MUST execute the manual review equivalents of the schema-conformance and loop-tag auditors per §5.3 and §5.5."

**Files requiring revision:**

| File | Section | Change |
|---|---|---|
| `docs/specs/performance-optimization/section-2.md` | §2.2.10 FR-PO-070 | Move to Stage 0+1, or qualify Stage 0 manual interpretation |
| `docs/specs/performance-optimization/section-5.md` | §5.2 Stage-Gated Activation Table | Update FR-PO-069 … 074 row if FR-PO-070 stage shifts |

**Version impact:** #18 section-file revision (v0.1 → v0.2).

**Resolution (May 14, 2026):** FR-PO-070 stage column updated to "Stage 0 (manual) / Stage 0+1 (automated)" with qualifier note clarifying Stage 0 uses manual audit execution per Appendix E template.

---

## ERR-018-010: Appendix F.1 N=100 and F.5 1% flake-rate thresholds absent from §3.10

**Status:** ✅ Resolved — May 14, 2026 (#18 section-file v0.2; both values added to §3.10 + §8.4 with rationale; Appendix F.5 inline `[GT]` tag appended).
**Severity:** Medium (governance constants outside the declared constants catalogue; F.5 also untagged)
**Detected:** May 14, 2026
**Detected During:** PASS-1 review
**Root Cause:** §3.10 declares itself the constants catalogue for #18's governance numerics. Appendix F (`appendices.md`) introduces two governance numbers not present in §3.10:

- **F.1:** "per-spec p50/p99 over last **N=100** captures (`[GT]`, pinned at Stage 0+1)."
- **F.5:** "flake rate **> 1%** triggers boundary-defect routing (§5.7.3)." — untagged.

§3.10's evidence-artifact convention says each `[GT]` value's evidence is the section-file path that introduces it; these two values introduce themselves in Appendix F but are not catalogued.

**Required fix:**

Add both values to §3.10 (and §8.4 mirror) with tags and rationale. F.5's threshold needs a tag (`[GT]` likely).

**Files requiring revision:**

| File | Section | Change |
|---|---|---|
| `docs/specs/performance-optimization/section-3.md` | §3.10 | Add `N=100 captures` row (`[GT]`, Appendix F.1) and `1% flake-rate threshold` row (`[GT]`, Appendix F.5) |
| `docs/specs/performance-optimization/section-8.md` | §8.4 | Mirror both rows |
| `docs/specs/performance-optimization/appendices.md` | Appendix F.5 | Append `[GT]` tag to "> 1%" |

**Version impact:** #18 section-file revision (v0.1 → v0.2).

**Resolution (May 14, 2026):** `section-3.md` §3.10 rows added for N=100 and 1% flake-rate. `section-8.md` §8.4 mirrored. `appendices.md` F.5 "> 1%" tagged `[GT]`.

---

## ERR-018-011: `SPEC_INDEX.md` row 18 not updated; §9.4 prematurely claims `IN REVIEW`

**Status:** ✅ Resolved — May 14, 2026 (#18 section-file v0.2; option (a) — `SPEC_INDEX.md` row 18 + CLAUDE.md OPEN ISSUES + `file-manifest.md` row 18 all flipped to `IN REVIEW` atomically; §9.3 atomic-update checkbox flipped `[x]` for the `IN PROGRESS → IN REVIEW` transition; `IN REVIEW → APPROVED` flip remains the future atomic update with lead-developer sign-off).
**Severity:** Medium (canonical-registry contradiction; CLAUDE.md says SPEC_INDEX.md is the source of truth on status)
**Detected:** May 14, 2026
**Detected During:** PASS-1 review
**Root Cause:** `section-9-approval-checklist.md` §9.4 declares *"Status: `IN REVIEW` (author-driven flip; lead-developer review pending)."* `SPEC_INDEX.md` row 18 still shows `IN PROGRESS`. CLAUDE.md states: *"SPEC_INDEX.md is the canonical source of truth for spec numbers, folder names, and approval status."* By that rule, the spec is `IN PROGRESS`, regardless of what §9.4 claims. CLAUDE.md OPEN ISSUES entry for #18 also still says "Section files remain stubs," which is no longer accurate.

**Problem in detail:**

§9.3 checklist row *"`SPEC_INDEX.md` status updated atomically with sign-off"* is correctly marked `[ ]` (unchecked) — acknowledging the update hasn't happened. But §9.4's Decision block then asserts `IN REVIEW` as the current status. The §9.4 status claim contradicts both the canonical registry and the unchecked §9.3 checklist row in the same file.

**Required fix:**

Either (a) update `SPEC_INDEX.md` row 18 and CLAUDE.md OPEN ISSUES entry to `IN REVIEW` atomically (the section files are authored — this state would be consistent), or (b) revert §9.4's status claim to `IN PROGRESS` until lead-developer sign-off. The status flip and the registry/CLAUDE.md updates must move together.

**Files requiring revision:**

| File | Section | Change |
|---|---|---|
| `docs/specs/SPEC_INDEX.md` | Row 18 | `IN PROGRESS` → `IN REVIEW` (option a) |
| `CLAUDE.md` | OPEN ISSUES entry for #18 | Update "Section files remain stubs" → "Section files drafted at v0.1; PASS-1 adversarial review filed (ERR-018-002…011); v0.2 fix pass pending"; flip status text to `IN REVIEW` |
| `docs/tracking/file-manifest.md` | #18 rows | Move section files from "stub" to "drafted" |
| `docs/specs/performance-optimization/section-9-approval-checklist.md` | §9.4 (option b alternative) | Revert "IN REVIEW" → "IN PROGRESS" |

**Version impact:** No section-file content revision required; metadata-only across three tracking files (option a). Option b is a one-line §9.4 edit.

**Resolution (May 14, 2026):** Option (a) applied. `SPEC_INDEX.md` row 18 updated `IN PROGRESS` → `IN REVIEW` with changelog entry. `CLAUDE.md` OPEN ISSUES entry for #18 updated to reflect `IN REVIEW` status and v0.2 section files. `file-manifest.md` row 18 updated from "stubs" to "section-1 through section-9-approval-checklist + appendices.md at v0.2".

---

## ERR-018-012: Appendix F has two conflicting `### F.0 Channel Registry Schema` sections

**Status:** ✅ Resolved — May 14, 2026 (#18 section-file v0.3 fix pass)
**Severity:** High
**Detected:** May 14, 2026
**Detected During:** PASS-2 adversarial review (`pass-2-adversarial-review.md` H-1)
**Root Cause:** PR #59 (`claude/fix-performance-specs-J1t5Z`, commit `14c6ba6`) and PR #60 (`claude/review-performance-specs-YHGga`, commit `dd6a87c`) both authored an Appendix F.0 channel-registry schema as fixes for `ERR-018-005`. Both PRs merged into `main` without de-duplication, leaving two `### F.0 Channel Registry Schema` sections in `appendices.md` (lines 231–256 and 258–281) with materially different field sets — 13 fields vs 7 fields, different names (`owning_subsystem` vs `subsystem_owner`, `inside_tick_pipeline` + `sign_off_log_ref` pair vs single `emission_veto_required` boolean, `record_format_version` semver vs `record_format` reference). The §5.7.1 audit hook walks `sign_off_log_ref` — present only in the first schema. The F.1–F.5 dashboards cite `perf.budget` / `perf.alloc` / `perf.trace` channel names — populated only as anchor rows in the second schema.

**Resolution:** Kept the canonical 13-field F.0 (richer, supports §5.7.1 audit hook against `sign_off_log_ref`, declares `record_format_version` semver per KD-11). Merged the duplicate's `perf.budget` / `perf.alloc` / `perf.trace` example rows into the canonical schema as illustrative Stage 0 anchor entries so F.1–F.5 dashboard data-source citations resolve at draft time. Per-subsystem channels (`ai.*`, `physics.*`) remain Stage 0+1 deliverables.

---

## ERR-018-013: `section-3.md` §3.10 has three duplicate-constant rows

**Status:** ✅ Resolved — May 14, 2026 (#18 section-file v0.3 fix pass)
**Severity:** High
**Detected:** May 14, 2026
**Detected During:** PASS-2 adversarial review (`pass-2-adversarial-review.md` H-2)
**Root Cause:** Same PR #59 + PR #60 parallel-branch merge as ERR-018-012. Both branches resolved ERR-018-008 (±20% promotion tolerance) and ERR-018-010 (N=100 dashboard window, 1% flake threshold) by appending rows to §3.10. Merge retained both row sets:

| First (v0.1) row | Duplicate (v0.2) row | Constant |
|------------------|----------------------|----------|
| `[EST]-baseline acceptance tolerance = ±20%` `[GT]` → §3.9.1 | `[EST]→[GT]` promotion tolerance = ±20% `[GT]` → §3.9.1 | ±20% promotion tolerance |
| Dashboard sample window = 100 captures `[GT]` → Appendix F.1 | Per-spec p50/p99 rolling window N = 100 captures `[GT]` → Appendix F.1 | N=100 dashboard window |
| Flake-rate alert threshold = 1% `[GT]` → Appendix F.5 | Flake-rate boundary-defect routing threshold = 1% `[GT]` → Appendix F.5 | 1% flake threshold |

**Resolution:** Deleted the three v0.1 rows; kept the v0.2 rows whose rationale columns are richer. §8.4 mirror table was already correct (v0.1 §3.10 was not mirrored there) — no §8.4 change required.

---

## ERR-018-014: Seven section files carry duplicate v0.2 version-history rows

**Status:** ✅ Resolved — May 14, 2026 (#18 section-file v0.3 fix pass)
**Severity:** Medium
**Detected:** May 14, 2026
**Detected During:** PASS-2 adversarial review (`pass-2-adversarial-review.md` M-1)
**Root Cause:** Same PR #59 + PR #60 merge as ERR-018-012 / 013. Each branch independently authored its own v0.2 version-history row. Merge retained both, producing the pattern `v0.2 (summary) | v0.1 | v0.2 (detailed fix list)` in seven files: `section-2.md`, `section-3.md`, `section-5.md`, `section-7.md`, `section-8.md`, `section-9-approval-checklist.md`, `appendices.md`. (`section-1.md`, `section-4.md`, `section-6.md` were not affected — only one branch touched each.)

**Resolution:** Consolidated each pair into a single v0.2 row carrying the union of fix-list notes — the more detailed (PR #59) text plus any uniquely-stated items from the PR #60 summary. v0.3 row appended below for this fix-pass landing.

---

## ERR-018-015: `section-1.md` header `Last Updated` is stale

**Status:** ✅ Resolved — May 14, 2026 (#18 section-file v0.3 fix pass)
**Severity:** Medium
**Detected:** May 14, 2026
**Detected During:** PASS-2 adversarial review (`pass-2-adversarial-review.md` M-2)
**Root Cause:** `section-1.md` line 4 still reads `**Last Updated:** May 13, 2026` despite the v0.2 row at §1.5 being dated May 14, 2026. Every other section file's header is `May 14, 2026 (v0.2 PASS-1 adversarial-review fix pass)`. The v0.2 PR for section-1 updated §1.5 but missed the header.

**Resolution:** Updated header to `**Last Updated:** May 14, 2026 (v0.3 PASS-2 adversarial-review fix pass)`.

---

## ERR-018-016: §3.5.2 conflates +5% per-PR gate with ±20% `[EST]`→`[GT]` promotion tolerance

**Status:** ✅ Resolved — May 14, 2026 (#18 section-file v0.3 fix pass)
**Severity:** Medium
**Detected:** May 14, 2026
**Detected During:** PASS-2 adversarial review (`pass-2-adversarial-review.md` M-3)
**Root Cause:** §3.5.2 *"Per-spec overrides"* bullet says: *"For example, Shot Mechanics #6 §4.5 already declares a 0.05 ms total budget; deviations larger than 5% from the 0.017 ms estimated cite #6 §4.5 authority, not §3.5.2 default."* The +5% per-PR threshold (§3.5.2 / FR-PO-031) is defined against a **measured pre-PR baseline**. The 0.017 ms is a spec-time `[EST]` anchor, not a captured baseline. Per §3.9.1, the first Stage 0+1 capture promotes `[EST]` → `[GT]` if within ±20%; the +5% gate only activates against promoted `[GT]` baselines. The example invokes the +5% gate against an un-promoted anchor.

**Resolution:** Rewrote the example to clarify the staging:
- First Stage 0+1 capture: apply §3.9.1 ±20% promotion tolerance (gate's MAY-override surface not exercised yet — value still an `[EST]` anchor).
- Once promoted: subsequent per-PR captures apply §3.5.2 default +5% gate against the measured baseline, or tighter per-spec override.

---

## ERR-018-017: FR-PO-019 levels `MAY` but embeds an unconditional MUST

**Status:** ✅ Resolved — May 14, 2026 (#18 section-file v0.3 fix pass)
**Severity:** Medium
**Detected:** May 14, 2026
**Detected During:** PASS-2 adversarial review (`pass-2-adversarial-review.md` M-4)
**Root Cause:** FR-PO-019 stated: *"Cross-scenario profiling (Spec #19 KD-8 cross-spec scenarios) is permitted; the manifest ID and seed MUST be recorded the same way."* Level column: `MAY`. RFC 2119 grammar treats the row's declared level as binding for the whole statement — a MAY-row that embeds a MUST is structurally identical to the MUST/MAY conflict PASS-1 caught as `ERR-018-003` (FR-PO-067 vs §3.4.4). Conformance auditor reading the level column would not enforce the recording requirement.

**Resolution:** Split into two FRs:
- FR-PO-019 (MAY): *"Cross-scenario profiling (Spec #19 KD-8 cross-spec scenarios) is permitted."*
- FR-PO-019a (MUST): *"For any cross-scenario profiling session entered into the baseline corpus, the manifest ID and seed MUST be recorded per FR-PO-016."*

---

## ERR-018-018: §3.7.5 pre-specifies C# attribute signature without specified consumer

**Status:** ✅ Resolved — May 14, 2026 (#18 section-file v0.3 fix pass)
**Severity:** Medium
**Detected:** May 14, 2026
**Detected During:** PASS-2 adversarial review (`pass-2-adversarial-review.md` M-5)
**Root Cause:** §3.7.5 stated: *"the C# `Attribute` definition lands at first `src/` commit (targets: `Method | Constructor`; required constructor argument: `string rationale`; companion lead-developer-sign-off comment cites the `spec-error-log.md` row that authorizes the exemption)."* The attribute's C# signature is fully pinned at spec time — target enum, constructor argument, companion-comment grammar — but its consumer (the CI allocation-tracker build step that reads the attribute) is unspecified anywhere in #18 / #19 / #20. The allocation-tracker pin is §7.5 D2 / Stage 0+1. CLAUDE.md "Interface Design Principle" (ERR-001 / ERR-004 hazard): *"Write interfaces only when both sides are specified."*

**Resolution:** §3.7.5 deferred the concrete C# signature to Stage 0+1 alongside §7.5 D2. Retained the signature-independent governance contract:
- Every exemption MUST carry a rationale.
- Every exemption MUST be authorized by lead-developer sign-off recorded in `spec-error-log.md`.
- Every exempted call site MUST be marked at the source level so the alloc-tracker CI step can exclude it from the §3.7.4 diff.

---

## ERR-012-001: `DOMAIN_TAG_POSITIONING_AI` allocation needed in #16 §3.4 — proposed Phase B/C block-allocation policy

**Status:** ✅ Resolved May 18, 2026 — `DOMAIN_TAG_POSITIONING_AI = 0x17` allocated in #16 §3.4 v1.0.5; #12 §6.1 `[CROSS-PENDING]` → `[CROSS: #16 §3.4]` promoted atomically; all body-text instances in §1/§2/§3/§4/§8 promoted in v0.3/v0.4 fix passes.
**Severity:** Medium
**Detected:** May 15, 2026
**Detected During:** Positioning AI #12 `outline-detailed.md` v1.1 self-adversarial review (AR-V1-01); resolution proposed in v1.2.
**Files Affected:** 1 (`deterministic-sim/section-3.md` §3.4 domain-tag table)

**Root Cause:** Spec #12 Positioning AI requires a `DOMAIN_TAG_POSITIONING_AI` value to bind `DeterministicRngService` calls per #16 §3.4 / KD-9. The current §3.4 table ends at `DOMAIN_TAG_EVENT_LEDGER = 0x15` (#17, allocated May 14, 2026 per ERR-017-001). Five further Phase B/C specs (#10 Heading, #11 Goalkeeper, #13 Pressing, #14 Defensive, #15 Attacking) will each need their own tag during their own outline → section-file phases.

If each spec unilaterally claims the next-available value at outline time (first-come, first-served), there is a real risk of (a) value collisions when two specs draft concurrently and (b) fragmented patch revisions to #16's APPROVED tag namespace. The cleanest pattern is a single block allocation now, gated on lead-developer sign-off, that all six specs cite as `[CROSS-PENDING]` until the patch lands.

**Proposed Resolution (Phase B/C block `0x17 … 0x1C`) — REVISED May 16, 2026:**

The original proposal (`0x16…0x1B`) assigned `0x16` to Positioning AI #12. However, Heading Mechanics #10 reached `APPROVED` first (May 16, 2026, via ERR-010-001 resolution per the same project precedent — first-to-APPROVED claims the next-available slot) and took `0x16`. The block therefore shifts one slot:

| Spec | Domain Tag | Proposed Value | Notes |
|---|---|---|---|
| #10 Heading Mechanics | `DOMAIN_TAG_HEADING` | `0x16` | ✅ ALLOCATED May 16, 2026 via ERR-010-001 (#16 §3.5 v1.0.2 patch) |
| #12 Positioning AI | `DOMAIN_TAG_POSITIONING_AI` | `0x17` | Drafting NOW (#12 IN REVIEW); shifted from `0x16` after #10's allocation landed |
| #11 Goalkeeper Mechanics | `DOMAIN_TAG_GOALKEEPER` | `0x18` | NOT STARTED |
| #13 Pressing AI | `DOMAIN_TAG_PRESSING_AI` | `0x19` | NOT STARTED |
| #14 Defensive AI | `DOMAIN_TAG_DEFENSIVE_AI` | `0x1A` | NOT STARTED |
| #15 Attacking AI | `DOMAIN_TAG_ATTACKING_AI` | `0x1B` | NOT STARTED |
| #16 reserve | — | `0x1C` | Reserved (one slot of margin from the original `0x1B` ceiling). |

The collision avoidance ERR-012-001 was authored to prevent — multiple specs unilaterally claiming the same slot at outline time — did NOT trigger here because #10's allocation was formal (#16 §3.4 patch landed) before #12's `0x16` `[CROSS-PENDING]` was promoted. #12 must update its `outline-detailed.md` and section files to cite `0x17` when its own back-prop lands.

Block is contiguous with `DOMAIN_TAG_HEADING = 0x16` and consumes one nibble of u8 namespace. No `DETERMINISM_DIGEST_VERSION` bump required (pure namespace allocation, no preimage layout / field width / hash-input rule changes — mirrors the ERR-017-001 resolution pattern).

**Patch landing site:** `deterministic-sim/section-3.md` §3.4 constants catalogue (add 6 rows in canonical numerical order). One revision, six rows; #16 §3.5 version-history row notes Phase B/C namespace allocation.

**Atomic promotion mechanic:** all six specs carry the tag as `[CROSS-PENDING]` until the #16 patch revision lands. On patch merge, each spec promotes its row from `[CROSS-PENDING]` → `[CROSS]` in its own §3.10 / §3.4 / KD-9 citation site in a follow-up patch (parallel to ERR-017-001 #17-side promotion).

**Sign-off required:** Lead developer (#16 owner). Once ratified, #12 outline KD-9 and FR-PA-005 promote from `[CROSS-PENDING]` to `[CROSS]` and section-file authoring proceeds with the value fixed.

---

## ERR-012-002: `decision-tree/section-3-1.md` L716 cites Formation System as "Spec #14" — stale spec number

**Status:** ✅ Closed — Fixed May 15, 2026 in `decision-tree/section-3-1.md` v1.1.1 (single-token patch; approval status preserved)
**Severity:** Minor
**Detected:** May 15, 2026
**Detected During:** Positioning AI #12 `outline-detailed.md` v1.2 Outstanding-Questions resolution pass (Q3 grep against #8).
**Files Affected:** 1 (`decision-tree/section-3-1.md` L716)

**Root Cause:** Decision Tree #8 §3.1.7.2 reads: *"Stage 1 wires the Formation System (Spec #14) to provide live formation slot positions that adjust with tactical instructions and ball position."* Current `SPEC_INDEX.md` row 14 is **Defensive AI**. The Formation System functionality is #12 Positioning AI (verified — #8 §1.4.21 and §1.7.3 already use the canonical #12 number elsewhere in #8). Stale spec number left over from an earlier numbering scheme — same regression class as ERR-010 (Shot Mechanics #6 §1.1 calling Decision Tree #7) and ERR-012 (First Touch §7 calling Decision Tree #7), both closed in the March 2026 renumbering cascade. #8 §3.1.7.2 was missed by that cascade.

**Resolution:** Patch `decision-tree/section-3-1.md` L716 to read "Positioning AI (Spec #12)". One-token change in an APPROVED spec; no behavioural impact; patch-revision row in #8 §3.x version history.

**Detection grep:** `grep -n "Spec #14" decision-tree/` returns only this one line in `section-3-1.md`. (`grep -n "Formation System" decision-tree/section-*.md` returns multiple "Formation System (Stage 1+)" references without spec numbers — those are correct as-is and should not be touched.)

**Recommended patch landing:** alongside #16 §3.4 ERR-012-001 patch (same lead-developer revision pass), or as a standalone one-token revision.

---

## ERR-012-003: Documentary anchor for `XC-012-001`..`XC-012-009` allocation

**Status:** ✅ Closed (informational — no remediation required)
**Severity:** Minor
**Detected:** May 16, 2026
**Detected During:** Positioning AI #12 section-files PASS-1 adversarial review (AR-S1-18).
**Files Affected:** 1 (`positioning-ai/section-8.md` §8.3)

**Root Cause:** AR-S1-18 noted that #9 / #16 / #17 / #19 precedent files at least a short error-log row when allocating `XC-NNN-NNN` typed cross-reference IDs, so cross-spec readers can discover them by grep. Spec #12 §8.3 allocates `XC-012-001`..`XC-012-009` at section-file v0.1 without a corresponding error-log entry.

**Resolution:** This entry serves as the documentary anchor. `XC-012-NNN` are not erratum-class entries — they are typed cross-reference IDs published in `positioning-ai/section-8.md` §8.3 against approved upstreams #2, #8, #16, #18, #20. No remediation; entry exists for grep discoverability.

---

---

## ERR-008-001: Decision Tree #8 §3.2 `PitchGeometry` class uses centered coordinate origin

**Status:** ✅ Resolved May 18, 2026 — `decision-tree/section-3-2.md` v1.3: class rewritten to corner-origin (0,0,0); all `Vector2` goal constants replaced with `Vector3` using correct values; citation corrected to §1.2 and Appendix C; XC-GEOM-01 verification note added.
**Severity:** High
**Detected:** May 18, 2026
**Detected During:** Stress-test Tier A run 1, probe A-06 (coordinate-convention-guard FAIL) + T-03 (inverted domain conventions).
**Files Affected:** 1 (`decision-tree/section-3-2.md`, lines 305–350+)

**Root Cause:** The `PitchGeometry` static class in Decision Tree #8 §3.2 is authored with a center-origin coordinate system — the same defect class logged in CLAUDE.md "Things That Have Gone Wrong Before" ("Wrong coordinate origin — 'Pitch center' comment in Agent Movement §3.5"). The class comment states:

```
/// Coordinate system (consistent with Ball Physics #1 §2.2 and Agent Movement #2 §2.1):
///   Origin (0, 0) = centre of pitch
///   X-axis: pitch length (−52.5m to +52.5m; total 105m)
///   Y-axis: pitch width (−34m to +34m; total 68m)
```

The authoritative coordinate system (CLAUDE.md §"Coordinate System", Ball Physics #1 §1.2 and Appendix C, verified in `ball-physics/section-3-1.md` and `agent-movement/section-3-5-part-1.md`) is:
- Origin: corner of pitch (0, 0, 0)
- X: 0–105m (goal-to-goal)
- Y: 0–68m (touchline-to-touchline)

**Consequence — all goal position constants are wrong:**

| Constant | DT §3.2 value (centered) | Correct corner-origin value |
|----------|--------------------------|----------------------------|
| `HOME_OPPONENT_GOAL_CENTRE` | `(52.5, 0)` | `(105.0, 34.0, 0)` |
| `HOME_OPPONENT_GOAL_POST_L` | `(52.5, +3.66)` | `(105.0, 37.66, 0)` |
| `HOME_OPPONENT_GOAL_POST_R` | `(52.5, −3.66)` | `(105.0, 30.34, 0)` |
| `HOME_OWN_GOAL_CENTRE` | `(−52.5, 0)` | `(0.0, 34.0, 0)` |
| `HOME_OWN_GOAL_POST_L` | `(−52.5, +3.66)` | `(0.0, 37.66, 0)` |
| `HOME_OWN_GOAL_POST_R` | `(−52.5, −3.66)` | `(0.0, 30.34, 0)` |

The citation "consistent with Ball Physics #1 §2.2" is also incorrect — the authoritative section per CLAUDE.md is §1.2 (not §2.2).

**Resolution:**
1. Rewrite `PitchGeometry` class in `decision-tree/section-3-2.md` to use corner-origin (0,0,0) throughout.
2. Update `Origin` comment to `Origin (0, 0, 0) = corner of pitch (home team's left defensive corner)`.
3. Update `X-axis` range to `0m to 105m`. Update `Y-axis` range to `0m to 68m`.
4. Recalculate and update all `Vector2`/`Vector3` goal position constants using the correct system.
5. Switch goal positions to `Vector3` (not `Vector2`) to match the 3D coordinate system; or add a note that Y-component = 0 (ground-level Z in the spec's convention) and Y in `Vector2` here maps to X in the global system — this requires careful thought; simpler to use `Vector3` directly to avoid axis-label confusion.
6. Correct the citation from "§2.2" to "§1.2 and Appendix C".
7. Append a version-history row to `section-3-2.md`.

**Probe trigger:** A-06 FAIL — phrase "Origin (0, 0) = centre of pitch" is a direct origin claim. T-03 defect class (inverted coordinate convention).

---

## ERR-015-006: Attacking AI #15 §1/§2/§3/§4 retain stale `[CROSS-PENDING]` tags after ERR-015-001 resolution

**Status:** ✅ Resolved May 18, 2026 — all 7 stale `[CROSS-PENDING]` hits promoted to `[CROSS: #16 §3.4]` in §1 (4 instances), §2 FR-AT-005, §3 constant table, §4 §4.6 prose; v0.3 version-history rows added to all four section files.
**Severity:** Medium
**Detected:** May 18, 2026
**Detected During:** Stress-test Tier A run 1, probe A-03 (cross-pending-tracker FAIL) + T-05 + T-02.
**Files Affected:** 4 (`attacking-ai/section-1.md`, `section-2.md`, `section-3.md`, `section-4.md`)

**Root Cause:** ERR-015-001 was resolved on May 18, 2026 — `DOMAIN_TAG_ATTACKING_AI = 0x1B` was allocated in `deterministic-sim/section-3.md` §3.4 (v1.0.4), and the `[CROSS-PENDING]` → `[CROSS: #16 §3.4]` promotion was applied in `section-6.md` §6.1.9 and `section-9-approval-checklist.md`. However, the same tag appears as `[CROSS-PENDING]` in four additional section files that were not part of the promotion pass. The approval checklist therefore falsely claims "0 `[CROSS-PENDING]` remain" (T-02: fabricated checklist entry).

**Stale hits (all in `attacking-ai/`):**

| File | Line | Stale text |
|------|------|------------|
| `section-1.md` | 114 | `DOMAIN_TAG_ATTACKING_AI = 0x1B [CROSS-PENDING]` in §1.4 dependency table |
| `section-1.md` | 164 | "`[CROSS-PENDING]` throughout this spec until ERR-015-001 is ratified" in KD-11 note |
| `section-1.md` | 245 | `0x1B [CROSS-PENDING]` in KD table column |
| `section-1.md` | 266 | `DOMAIN_TAG_ATTACKING_AI = 0x1B [CROSS-PENDING] until ERR-015-001 ratified` in cross-spec compliance table |
| `section-2.md` | 25 | FR-AT-005: `([CROSS-PENDING] until ERR-015-001 is ratified in #16 §3.4)` |
| `section-3.md` | 948 | Constant reference table: `\| DOMAIN_TAG_ATTACKING_AI \| [CROSS-PENDING] \| 0x1B (ERR-015-001) \|` |
| `section-4.md` | 206 | `DOMAIN_TAG_ATTACKING_AI = 0x1B [CROSS-PENDING] (ERR-015-001; see …)` |

**Resolution:** In each location above, replace `[CROSS-PENDING]` with `[CROSS: #16 §3.4]` and update "until ERR-015-001 is ratified" clauses to "resolved May 18, 2026". Update `section-9-approval-checklist.md` §9.1 evidence row to accurately state which files were updated. Append version-history rows to each of the four section files.

**Probe trigger:** A-03 FAIL — `[CROSS-PENDING]` present in approved spec body text with no matching `Status: OPEN` ERR entry (ERR-015-001 is CLOSED). T-05 (dangling tag after upstream APPROVED). T-02 (fabricated checklist claim).

---

## ERR-016-003: Domain tag registry (#16 §3.4) silent gaps at `0x18` and `0x1C`

**Status:** ✅ Resolved May 18, 2026 — `deterministic-sim/section-3.md` v1.0.6: `_RESERVED_0x18_` and `_RESERVED_0x1C_` placeholder rows added to §3.4 domain-tag table; v1.0.6 version-history row added.
**Severity:** Medium
**Detected:** May 18, 2026
**Detected During:** Stress-test Tier A run 1, probe A-04 (domain-tag-allocator-audit FAIL) + T-08.
**Files Affected:** 1 (`deterministic-sim/section-3.md` §3.4 domain-tag table)

**Root Cause:** The ERR-012-001 Phase B/C block originally proposed the range `0x17…0x1C` (with `0x1C` as one slot of margin). As allocations landed, `0x18` was informally noted in the v1.0.3 changelog as "reserved for #11 Goalkeeper" before Goalkeeper Mechanics was reallocated to `0x1D` (because Positioning AI reached APPROVED first and claimed `0x17`, triggering the first-to-APPROVED cascade that shifted GK from `0x17` to `0x1D`). Neither `0x18` nor `0x1C` was ever assigned or documented in the live §3.4 table as a placeholder.

**A-04 requirement:** "every gap in the allocation sequence has an explicit `_RESERVED_0xNN_` placeholder row in the §3.4 table."

**Actual allocation sequence:**
```
0x10 DOMAIN_TAG_PHASE
0x11 DOMAIN_TAG_SNAPSHOT_PAYLOAD
0x12 DOMAIN_TAG_SNAPSHOT_HEADER
0x13 DOMAIN_TAG_RNGDRAW
0x14 DOMAIN_TAG_ENV_FP
0x15 DOMAIN_TAG_EVENT_LEDGER
0x16 DOMAIN_TAG_HEADING
0x17 DOMAIN_TAG_POSITIONING_AI
[0x18 — MISSING; no row]
0x19 DOMAIN_TAG_PRESSING_AI
0x1A DOMAIN_TAG_DEFENSIVE_AI
0x1B DOMAIN_TAG_ATTACKING_AI
[0x1C — MISSING; no row]
0x1D DOMAIN_TAG_GOALKEEPER
```

**Risk:** A developer assigning the next subsystem domain tag would search for the last-allocated value and find `0x1D`, concluding `0x1E` is next-available. The orphaned `0x18` and `0x1C` remain permanently unavailable for reuse but are not documented as such, creating a silent encoding hole.

**Resolution:** Add two rows to the §3.4 domain-tag table in `deterministic-sim/section-3.md` (in numerical order between the existing rows):

```
| _RESERVED_0x18_ | 0x18 | — | Skipped. Originally informally noted in #16 §3.4 v1.0.3 changelog as a reservation for Goalkeeper Mechanics #11 (ERR-011-001). GK was subsequently reallocated to 0x1D when Positioning AI #12 reached APPROVED first and claimed 0x17 per first-to-APPROVED precedent (ERR-011-001 KD-7 policy). Value 0x18 is permanently orphaned — must not be reassigned to any subsystem without explicit ERR tracking. |
| _RESERVED_0x1C_ | 0x1C | — | Skipped. Block-end margin value of the ERR-012-001 Phase B/C block (0x17…0x1C). Block was closed when 0x1B was allocated to Attacking AI #15 (ERR-015-001). Value 0x1C was never assigned; permanently orphaned — must not be reassigned without explicit ERR tracking. |
```

Append a v1.0.6 version-history row to `deterministic-sim/section-3.md`. No `DETERMINISM_DIGEST_VERSION` bump required (placeholder rows are namespace documentation, not preimage-layout changes).

**Probe trigger:** A-04 FAIL (silent gap without placeholder row). T-08 (DOMAIN_TAG gap).

---

## ERR-020-001: Code Standards #20 §4.2 `[CROSS]` mirror example uses ALL_CAPS field name, contradicting §3.2.3 PascalCase rule

**Spec:** Code Standards #20  
**Section:** §4.2 Constant Catalogue File Convention — `ProjectConstants.cs` Cross-Spec Source of Truth  
**Severity:** Minor  
**Detected During:** `src/CLAUDE.md` v1.3 adversarial review (May 22, 2026), finding M-3.  
**Status:** ✅ Resolved May 22, 2026

**Problem:** The §4.2 worked example for a `[CROSS]` mirror constant in `BallPhysicsConstants.cs` used `PHYSICS_TICK_HZ` (ALL_CAPS) as the mirror field name:

```csharp
public static readonly float PHYSICS_TICK_HZ = ProjectConstants.PHYSICS_TICK_HZ;
```

Spec #20 §3.2.3 (Tag → C# Storage Class Mapping) is the authoritative naming rule and explicitly states that `[CROSS]` constants use PascalCase. The ALL_CAPS convention is reserved exclusively for `[FIXED]` (`public const`) constants. A developer reading only §4.2 would use ALL_CAPS for every `[CROSS]` mirror, producing a codebase-wide naming inconsistency.

**Root Cause:** The §4.2 example was authored with the `PHYSICS_TICK_HZ` name matching the source constant in `ProjectConstants.cs` (which is correctly `[FIXED]` ALL_CAPS) rather than following the mirror field naming convention from §3.2.3.

**Files Affected:**
| File | Location | Change |
|---|---|---|
| `docs/specs/code-standards/section-4.md` | §4.2 mirror example (line ~160) | `PHYSICS_TICK_HZ` → `PhysicsTickHz`; XML doc updated with spec+section citation |
| `src/CLAUDE.md` | `[CROSS]` mirrors naming discrepancy note | Reference to ERR-020-001 added; "has been patched" noted |

**Resolution:** `code-standards/section-4.md` v1.0.1 patch: mirror field renamed to `PhysicsTickHz` (PascalCase); XML doc updated to include authoritative spec and section citation (`Ball Physics #1 §1.2`) and value (`60 Hz`) per FR-CS-022. `src/CLAUDE.md` v1.4 discrepancy note updated with ERR-020-001 reference.

**Rule confirmed:** The source constant in `ProjectConstants.cs` is `[FIXED]` and correctly uses ALL_CAPS (`PHYSICS_TICK_HZ`). The mirror field in any spec's constants catalogue is `[CROSS]` and uses PascalCase (`PhysicsTickHz`). The right-hand side of the mirror assignment must reference the source by its ALL_CAPS name (`= ProjectConstants.PHYSICS_TICK_HZ`).

---

## ERR-004-002: `FirstTouchContext` does not expose the nearest opponent's agent ID — `PossessionStateMachine` cannot resolve `InterceptingAgentID` on INTERCEPTION outcome

**Spec:** First Touch Mechanics #4
**Section:** §3.4.2 (priority-ordered outcome state machine), §4.3.1 (FirstTouchContext fields), §4.3.2 (FirstTouchResult fields)
**Severity:** Minor (Stage 0 carve-out; documented placeholder behaviour)
**Detected During:** `src/first-touch/` AR-5 adversarial review (June 6, 2026), finding L-4.
**Status:** 🟡 Open — placeholder behaviour in place; spec revision deferred

**Problem:** `PossessionStateMachine.Determine` (Priority 1 — INTERCEPTION branch) returns `(TouchResult.Interception, AGENT_ID_NONE, AGENT_ID_NONE)` because `FirstTouchContext` exposes only `HasNearbyOpponent` (bool) + `NearestOpponentDistance` (float) — there is no field carrying the nearest opponent's entity ID. The third tuple element of the return value is supposed to be `InterceptingAgentID`, but the data needed to populate it is not in the context. Result: the `FirstTouchResult.InterceptingAgentID` field surfaced to callers is `AGENT_ID_NONE = -1` on every INTERCEPTION outcome, which is indistinguishable from "no interception" downstream — Stage 1+ consumers that route possession to the intercepting opponent have no way to identify the receiving agent.

**Root Cause:** First Touch #4 §3.4.2 specifies the outcome classification logic but §4.3.1 omits a `NearestOpponentEntityId` field from `FirstTouchContext`. The omission was discovered post-implementation when `PossessionStateMachine` was wired up. The implementation placed an inline `// TODO: spec gap …` comment at the INTERCEPTION return; the AR-5 review found the gap was untracked in the error log.

**Files Affected:**
| File | Location | Change |
|---|---|---|
| `src/first-touch/PossessionStateMachine.cs` | Priority 1 INTERCEPTION return (~line 40) | Inline `TODO:` comment replaced with `ERR-004-002` anchor |
| `docs/specs/first-touch/section-4.md` (pending) | §4.3.1 FirstTouchContext field list | Add `NearestOpponentEntityId : int` field (or equivalent) |
| `src/first-touch/FirstTouchContext.cs` (pending) | Field declarations after `NearestOpponentDistance` | Add the field once §4.3.1 is patched |
| `src/first-touch/FirstTouchSystem.cs` (pending) | EvaluateFirstTouch wiring | Forward the ID into `PossessionStateMachine.Determine` |

**Resolution (proposed):** Add `int NearestOpponentEntityId` (sentinel `AGENT_ID_NONE` when `!HasNearbyOpponent`) to `FirstTouchContext` in a coordinated §4.3.1 patch. Caller (currently the integration boundary in `FirstTouchSystem`) populates it from the same scan that produces `NearestOpponentDistance` — typically the `PressureEvaluator` result. `PossessionStateMachine.Determine` then uses it for the INTERCEPTION return tuple. No formula change; pure data-flow gap closure.

**Stage 0 carve-out:** Until §4.3.1 is patched, INTERCEPTION outcomes carry `InterceptingAgentID = AGENT_ID_NONE`. Stage 0 has no downstream consumer that routes on this field (FirstTouchSystem.ApplyTouchResult only consumes `PossessingAgentID`); the gap blocks Stage 1+ AI-routed interception handoffs but not the Stage 0 test surface.

**Probe trigger:** AR-5 L-4 (June 6, 2026).

---

## ERR-003-001: Collision System #3 §3.3 impulse-to-force conversion F = j × 60 Hz inflates contact force ~10× against literature-calibrated thresholds

**Spec:** Collision System #3
**Section:** §3.3 Step 6 (impact force); contradicts the §3.3.1 threshold derivations (FALL_FORCE_BASE 500 N, FALL_FORCE_PER_STRENGTH 50 N, FALL_PROBABILITY_RANGE 500 N — sustained-force literature values)
**Severity:** Critical
**Detected During:** `src/collision-system/` AR-7 adversarial review (June 10, 2026), finding H-1.
**Status:** ✅ Closed — spec and implementation patched June 10, 2026

**Problem:** `F = Mathf.Abs(j) * 60f` assumes the whole collision impulse acts within one 16.7 ms frame. For an 85 kg equal pair, F ≈ 3315 × vRel (N), so the entire stochastic fall/stumble band (500–1500 N) spanned closing speeds of 0.15–0.6 m/s — below walking pace. Every real contact (jog ≈ 4 m/s closing → 13 kN) was a guaranteed knockdown roll, the failed roll guaranteed a stumble, and `knockdownForceOut` saturated at 1.0 (MaxCollisionForceRef = 2000 N at vRel ≈ 0.6 m/s). The test suite encoded the same scale (FL-002 asserted likely-stumble at vRel = 0.23 m/s), so the calibration defect was invisible to it.

**Resolution:** New `[GT]` `CONTACT_DURATION_S = 0.15 s` (biomechanics contact time ~0.1–0.3 s) added to the §3.3 catalogue; conversion patched to `F = j / CONTACT_DURATION_S` in spec pseudocode and `CollisionResponse.cs` v1.5 (`CollisionPhysicsConstants.ContactDurationS`; `PHYSICS_TICK_HZ` removed — that conversion was its sole consumer). Stochastic band now spans vRel ≈ 1.4–5.4 m/s. FL-001..005 / DT-001..002 closing speeds re-derived (tests v1.2).

---

## ERR-003-002: Collision System #3 §3.3/§3.4 FROM_BEHIND classification — normal convention sign-inverted on two surfaces

**Spec:** Collision System #3
**Section:** §3.3 `ClassifyContactType` (behindDot formula) and §3.4 `ProcessAgentAgentCollision` (Classify call site + `ForceDirection`)
**Severity:** Major
**Detected During:** `src/collision-system/` AR-7 adversarial review (June 10, 2026), finding M-1.
**Status:** ✅ Closed — spec and implementation patched June 10, 2026

**Problem:** Both `Classify` and `ContactForceData.ForceDirection` are documented against an instigator→victim normal, but the §3.4 call site always passed `manifold.Normal` (entity1→entity2) unflipped — sign-inverted whenever the instigator is the second agent. Compounding it, the §3.3 formula `Dot(-collisionNormal, victimDir) > 0.5` detects a victim moving TOWARD the instigator (head-on), not a fleeing victim; with a doc-correct normal FROM_BEHIND could never fire. Net behaviour: FROM_BEHIND fired only when the second agent instigated, via two cancelling sign errors; identical geometry with the first agent instigating yielded SIDE_IMPACT.

**Resolution:** §3.3 formula corrected to `Dot(collisionNormal, victimDir)` (victim fleeing along instigator→victim normal); §3.4 call site computes `instigatorToVictim = instigatorIdx == 0 ? manifold.Normal : -manifold.Normal` and feeds it to both `Classify` and `ForceDirection`. Implementation: `ContactTypeClassifier.cs` v1.2 + `CollisionSystem.cs` v1.6. Stage 0 consumers do not act on FoulData (Referee is Stage 1+), but the event stream is replay/analytics surface.

---

## ERR-003-003: Collision System #3 §3.3 same-team contacts above fallThreshold escape both fall and stumble branches

**Spec:** Collision System #3
**Section:** §3.3 `DetermineFallOrStumble` (stumble condition)
**Severity:** Major
**Detected During:** `src/collision-system/` AR-7 adversarial review (June 10, 2026), finding M-2.
**Status:** ✅ Closed — spec and implementation patched June 10, 2026

**Problem:** The fall branch requires `!isSameTeam`; the stumble branch required `impactForce <= fallThreshold`. A same-team impact above fallThreshold matched neither — the hardest same-team collisions were consequence-free while moderate ones could stumble (non-monotonic).

**Resolution:** Upper gate dropped; stumble probability clamped to 1 (`Clamp01`). Opposing-team forces above fallThreshold still return from the fall branch first, so its behaviour is unchanged. Spec pseudocode + `CollisionResponse.cs` v1.5.

---

## ERR-003-004: Collision System #3 §3.4 MAX_COLLISION_PAIRS_PER_FRAME valve counts broad-phase candidates and aborts the whole frame

**Spec:** Collision System #3
**Section:** §3.4 `UpdateCollisions` pair loop; §8 sizing rationale ("~10–20 pairs in practice") counted colliding pairs, not candidates
**Severity:** Major
**Detected During:** `src/collision-system/` AR-7 adversarial review (June 10, 2026), finding M-3.
**Status:** ✅ Closed — spec and implementation patched June 10, 2026

**Problem:** The valve charged the 50-pair budget per broad-phase candidate (3×3-cell neighbour after dedupe) and on exceedance aborted all remaining processing including agent-ball. A goalmouth scramble (~15 clustered agents) generates 100+ unique candidates, so the valve fired in exactly the scenarios where collisions matter, deterministically but silently dropping response for the higher-indexed roster half. Candidate iteration needs no valve — it is already bounded at 253 pairs by the dedupe bitfield.

**Resolution:** `ProcessAgentAgent` / `ProcessAgentBall` return narrow-phase confirmation; the valve counts confirmed collisions only (cap = event-buffer capacity, so the buffer cannot overflow). Spec pseudocode + `CollisionSystem.cs` v1.6.

---

## ERR-003-005: Collision System #3 §3.3 impulse response — approach/separation gate inverted for the a1→a2 normal convention

**Spec:** Collision System #3
**Section:** §3.3 Step 2 (relative velocity gate) and Step 4 (impulse application signs); §3.2 defines the manifold normal as pointing from Entity1 toward Entity2
**Severity:** Critical
**Detected During:** `src/collision-system/` AR-8 adversarial review (June 10, 2026), finding H-1.
**Status:** ✅ Closed — spec and implementation patched June 10, 2026

**Problem:** With n pointing a1→a2, `vRel = (v1 − v2)·n > 0` means a1 closes on a2 — approaching. The pseudocode gated `if (vRel > 0) → separation only` (labelled "separating") and computed `j = −(1+e)·vRel/Σ(1/m)` with `Δv1 = +j·n/m1`. Net behaviour: genuine closing collisions produced penetration separation only — no momentum exchange, no ImpactForce, and `DetermineFallOrStumble` was unreachable for real contacts — while overlapped pairs already moving apart received a velocity-reversing impulse back toward re-collision (energy injection). The unit suite encoded the inversion: CR-001 set both agents moving outward and rationalised it as a "passed-through state".

**Resolution:** Gate corrected to `vRel <= 0 → separation only`; `j = +(1+e)·vRel/Σ(1/m)` (preserving the j > 0 invariant the AR-3/AR-5 simplifications rely on); application signs corrected to `Δv1 = −j·n/m1`, `Δv2 = +j·n/m2`. Restitution verified: equal-mass head-on at ±5 m/s, e = 0.3 → ∓1.5 m/s with separation speed = e·closing speed. Spec §3.3 pseudocode + `CollisionResponse.cs` v1.6; CR-001..003 / FL-001..005 / DT-001..002 / EC-004 setups flipped to approaching geometry (tests v1.3).

---

## ERR-003-006: Collision System #3 §3.3 contact classification — FROM_BEHIND shadowed by the velocity-only shoulder predicate

**Spec:** Collision System #3
**Section:** §3.3 `ClassifyContactType` branch order
**Severity:** Major
**Detected During:** `src/collision-system/` AR-8 adversarial review (June 10, 2026), finding M-1.
**Status:** ✅ Closed — spec and implementation patched June 10, 2026

**Problem:** A chase-down (instigator catching a fleeing victim) has parallel velocities, so the shoulder predicate `Dot(approachDir, victimDir) > 0.7` — which tests velocity alignment only, with no contact geometry — classified every from-behind contact as SHOULDER_TO_SHOULDER before the from-behind test ran. Latent until ERR-003-002 made the from-behind geometry test correct; the two defects together meant FROM_BEHIND was effectively unreachable for its canonical geometry.

**Resolution:** FROM_BEHIND evaluated before SHOULDER_TO_SHOULDER; the contact normal is the discriminator (back-on contact: victimDir ∥ instigator→victim normal; side-by-side: perpendicular, falls through to the shoulder test). Spec §3.3 pseudocode + `ContactTypeClassifier.cs` v1.3.

---


## ERR-001-001: Ball Physics #1 §3.1.8.1 bounce pseudocode uses Unity Y-up `Vector3.up` as the ground normal in a Z-up coordinate system

**Spec:** Ball Physics #1
**Section:** §3.1.8.1 (Impulse-Based Bounce); contradicts §1.2 / Appendix C (Z = height) and Appendix B ("v_n ... vertical for a flat pitch")
**Severity:** Critical
**Detected During:** `src/ball-physics/` AR-7 adversarial review (June 9, 2026), finding H-1.
**Status:** ✅ Closed — spec and implementation patched June 9, 2026

**Problem:** The §3.1.8.1 pseudocode sets `Vector3 normal = Vector3.up;`. Unity's `Vector3.up` is `(0, 1, 0)` — the touchline (Y) axis in this project's corner-origin Z-up coordinate system. `BallGroundInteraction.ApplyBounce` implemented the line faithfully, so restitution and friction were computed against the lateral velocity component: a vertically falling ball had `v_n = v_y = 0`, zero restitution impulse, zero friction budget (`J_n = 0`), and never rebounded. Every other surface in the assembly (gravity `-Z`, height gates `.z`, the bounce's own `Position.z = RADIUS` write) is Z-up. Undetectable by the test suite because the Unity project is not yet initialized (tests have never executed).

**Resolution:** Spec §3.1.8.1 pseudocode patched to `new Vector3(0f, 0f, 1f)` with an inline ERR-001-001 warning (changelog row 2.8); `BallGroundInteraction.cs` v1.3 fixed identically (AR-7 H-1). Unit/integration expectations re-verified by a numerical mirror of the corrected model.

---

## ERR-001-002: Ball Physics #1 §3.1.8.1 friction stick impulse omits the rotational-coupling divisor

**Spec:** Ball Physics #1
**Section:** §3.1.8.1 STEP 4 (tangential friction impulse)
**Severity:** Major
**Detected During:** `src/ball-physics/` AR-7 adversarial review (June 9, 2026), finding M-1.
**Status:** ✅ Closed — spec and implementation patched June 9, 2026

**Problem:** `J_t_required = m * contactSpeed` is the impulse that zeroes contact-point slip for a non-rotating body. For a sphere the friction impulse also changes ω, so the contact-point velocity changes by `(1 + m·r²/I)` per unit of tangential Δv — for the hollow-sphere model (I = ⅔·m·r²) the factor is 2.5. When the μ·J_n cap is not binding, the applied impulse therefore reversed the slip by ~150% instead of zeroing it, injecting spurious tangential velocity and spin at every gripping bounce.

**Resolution:** Stick impulse divided by the catalogued `[DERIVED]` constant `BallPhysicsConstants.Bounce.StickImpulseCouplingDivisor = 1 + (MASS × RADIUS²) / MomentOfInertia` in both the spec pseudocode (changelog row 2.8) and `BallGroundInteraction.cs` v1.3 (AR-7 M-1).

---

## ERR-001-003: Ball Physics #1 — seven `[EST]` constants lack the FR-CS-020 validation log entries

**Spec:** Ball Physics #1 / Code Standards #20 (FR-CS-020)
**Section:** `src/ball-physics/BallPhysicsConstants.cs` — `Drag.CrisisSpeedLow` (20.0 m/s), `Drag.CrisisSpeedHigh` (25.0 m/s), `Spin.RollingSpinDecayPerSecond` (5.0 rad/s²), `Bounce.SpinToLinearRatio` (0.1), `Limits.MaxVelocity` (50 m/s), `Limits.MaxSpin` (80 rad/s), `Limits.MaxHeight` (50 m)
**Severity:** Minor (documentation-governance gap; values plausible, none validated)
**Detected During:** `src/ball-physics/` AR-8 adversarial review (June 9, 2026), finding L-2.
**Status:** 🟡 Open — this entry IS the required FR-CS-020 record; per-constant validation (promotion to `[GT]`/`[DERIVED]`/`[FIXED]`) is a Stage 1 tuning task

**Problem:** FR-CS-020 requires every `[EST]` constant to carry a `spec-error-log.md` entry tracking its validation path; the seven constants above had none. (An eighth, `Ball.MomentOfInertia`, was retagged `[EST]` → `[DERIVED]` in AR-7 L-2 — it is a documented formula over `[FIXED]` inputs, not an estimate.)

**Validation paths:** `CrisisSpeedLow/High` — literature check against Asai et al. (2007) drag-crisis Reynolds range; `RollingSpinDecayPerSecond` and `SpinToLinearRatio` — empirical tuning against rolling/bounce footage at Stage 1; `Limits.*` — sanity ceilings (fastest recorded shot ≈ 45 m/s) that promote to `[GT]` once gameplay tuning begins.

---

*End of Spec Error Log v1.24 — June 10, 2026.*
