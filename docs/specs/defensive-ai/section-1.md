# Defensive AI Specification #14 — Section 1: Introduction, Scope, Dependencies, Key Decisions

**Created:** May 17, 2026
**Last Updated:** May 18, 2026 (v0.4 — FAIL-4 fix (A-03): §1.3.3 ERR-014-004 block — `[CROSS-PENDING]` promoted to `[CROSS: #16 §3.4]`, resolved outcome documented (0x1A final, 0x18/0x1D race resolved). §1.6 #16 boundary row — `[CROSS-PENDING]` promoted to `[CROSS: #16 §3.4]`.)
**Version:** 0.4
**Status:** DRAFT
**Source:** `outline-detailed.md` v1.0 (May 17, 2026)

---

## 1.1 Purpose

Defensive AI (#14) specifies **coordinated** out-of-possession defensive
assignment behaviour: the per-team logic that manages which outfield agents
hold a mark, which opponents receive man-marking attention, when the defensive
line steps up to execute an offside trap, and how the last-man emergency
override fires. The spec applies exclusively to agents carrying the
`HOLD_SHAPE` role from Pressing AI #13 — agents currently in `PRIMARY_PRESS`
or `COVER_SHADOW` roles on a given tick are outside #14's pool (KD-4).

The spec exists to (a) name the canonical **mark-mode catalog** (§3.3,
KD-11): `ZONAL`, `MAN_MARK`, `INTERCEPT_RUNNER`, `COVER_GK_ZONE`;
(b) define the **per-agent assignment algorithm** (§3.3–§3.5, KD-11);
(c) specify the **offside trap execution protocol** (§3.7, KD-9);
(d) define the **last-man emergency protocol** (§3.8–§3.9, KD-12);
(e) declare the **tackle-intent surface** (§3.6, KD-6);
(f) enforce three measurable **anti-chaos invariants** (§3.10, KD-17);
and (g) declare the integration surface with Decision Tree #8 — where
runtime activation lands at **Stage 1** per #8 §1.3.2.

This specification is a producer of one `MarkDirective` per team per
tick plus one `MarkAssignment` per agent per tick for agents in the
HOLD_SHAPE pool. It does **not** own the per-agent action loop (that
is #8), does **not** steer agents at 60 Hz (that is #2), does **not**
adjudicate offside calls (that is a future referee spec per KD-9), and
does **not** redefine perception, fatigue, or coordinate conventions
(cite-not-redefine — KD-1).

It is bound by CLAUDE.md "Project Identity" (Stage 0 Physics Foundation —
no code until all 20 specs approved) and the "Interface Design Principle"
(no interfaces against unspecified consumers — #15 Attacking AI is
NOT STARTED at the time of this draft).

## 1.2 Scope

### 1.2.1 In Scope (specification text — Stage 0 deliverable)

- HOLD_SHAPE agent pool definition and filtering from #13 role partition
  (§3.2, KD-4).
- Mark-mode catalog: `ZONAL`, `MAN_MARK`, `INTERCEPT_RUNNER`,
  `COVER_GK_ZONE` (§3.3, KD-11).
- Per-team `MarkDirective` computation (§3.11).
- Per-agent `MarkAssignment` with displacement-based cost selection
  and EntityId terminal tie-break (§3.3–§3.4, KD-11, KD-10).
- Threat score function (§3.5) citing #7 perception surfaces only
  (cite-not-redefine KD-1).
- Tackle intent evaluation: `COMMIT` / `JOCKEY` / `HOLD` per eligible
  agent; `TackleIntentRequest` output (§3.6, KD-6).
- Offside trap execution: trigger condition, step-up target depth,
  simultaneous backline advance, hysteresis (§3.7, KD-9).
- Last-man predicate and emergency `INTERCEPT_RUNNER` override (§3.8–§3.9,
  KD-12).
- Anti-chaos invariant enforcement before directive publication (§3.10,
  KD-17).
- Assignment-transition dwell-time hysteresis bound to Agent Movement
  #2 §3.1 pattern (§3.11, KD-11).
- Three measurable anti-chaos invariants: `MIN_BACKLINE_AGENTS`,
  `MAX_MAN_MARK_ASSIGNMENTS`, `MAX_MARK_DISPLACEMENT_M` (KD-17).
- Phase gating via #12 phase enum: `IN_POSSESSION` produces all-ZONAL
  directive with no further computation (§3.1, KD-19).
- Canonical exploit-resistance test corpus (§5, KD-18).
- Parameter catalogue (§6.1).

### 1.2.2 Out of Scope (deferred to §7 per KD-16)

Runtime code (Stage 1 deliverable); the #8 §2.2.6 / §3.1.7 back-prop
amendment text itself (filed as `ERR-014-001` separately); #17 channel
registration (`MARK_ASSIGNED` / `LINE_STEPPED` — `ERR-014-002` /
`ERR-014-003` at Stage 1); #15 handoff implementation; authoring tools
and coach UI; save-game persistence; ML-tuned `[GT]` parameter fitting;
set-piece defensive wall formation (Stage 2+ set-piece system);
offside adjudication and goal-line rule logic (future referee spec per
KD-9); per-player man-marking instructions from team tactics screen;
Fixed64 migration (Stage 5+); goalkeeper-as-last-man specialised
handling (GK positioning is owned by #11 per KD-7).

## 1.3 Dependencies

### 1.3.1 Approved Upstream

| Spec | Sections Bound | Use |
|---|---|---|
| #1 Ball Physics | §1.2, Appendix C | Corner-origin coordinates; `PITCH_LENGTH_M` / `PITCH_WIDTH_M` constants; ball-state schema for sideline geometry |
| #2 Agent Movement | §2.5 (`XC-002-001`), §3.1 | EntityId no-reuse; dwell-time + dead-zone hysteresis pattern |
| #3 Collision System | §3.x (tackle contact model) | Boundary reference: #14 produces tackle intent; #3 owns contact physics (KD-6) |
| #4 First Touch | §3.1 (control quality `q`) | Not read directly — consumed via #7 perception snapshot propagation |
| #5 Pass Mechanics | §2 FR-10 (`PassAttemptEvent`) | Not read directly — ball state and opponent positioning consumed via #7 snapshot |
| #6 Shot Mechanics | §2 (shot-event schema) | Boundary awareness for threat scoring near goal; consumed via #7 snapshot only |
| #7 Perception System | §3.7–§3.10 | Filtered world model: agent positions, ball state, possession owner, attribute lookups (`FirstTouch` — threat score §3.5; `Tackling` — declared for future tackle-quality use, not consumed at Stage 0), `isActive` |
| #8 Decision Tree | §1.3.2, §2.2.6, §3.1.7, §3.1.9 | Stage-1 binding row (§1.3.2 deferral of coordinated defensive assignment); `MarkDirective?` field in `TacticalContext` (ERR-014-001, Option B); EntityId no-reuse (`XC-008-001`) |
| #13 Pressing AI | §2.2 (`PressAssignment`), §3.4–§3.5, §4.5, FR-PR-014, FR-PR-017 | Role partition: `HOLD_SHAPE` pool is derived by excluding `PRIMARY_PRESS` and `COVER_SHADOW` agents; GK always `HOLD_SHAPE` from #13's view (FR-PR-017) |
| #16 Deterministic Simulation | §3.2, §3.2.5, §3.4, §5, §6.2 | EntityId iteration order; domain-tag registry; per-tick digest scope |
| #17 Event System | §3.10 (channel registry — Stage 1 back-prop) | No channels produced or consumed at Stage 0 |
| #18 Performance | §3.7, §6 | Zero-allocation hot-path discipline; per-tick budget framework |
| #19 Testing | §3, §4 | Test taxonomy + FR-traceability framework |
| #20 Code Standards | §4.2 (FR-CS-025) | Single constant-catalogue file `DefensiveAIConstants.cs` |

### 1.3.2 Pending Upstream

- **#11 Goalkeeper Mechanics** — `IN REVIEW` (May 16, 2026). Consumed
  as a boundary reference for: (1) the `COVER_GK_ZONE` mode trigger —
  GK x-position is read via the #7 perception snapshot, not a direct
  #11 accessor; (2) defensive wall formation ownership — #11 FR-GK-016
  explicitly assigns defensive wall placement to #14. No direct accessor
  surface between #11 and #14 at Stage 0 or Stage 1 (KD-7 — GK position
  is always read through the #7 perception snapshot at tick start).
- **#12 Positioning AI** — `IN REVIEW` (May 16, 2026). Consumed as the
  **baseline shape source** for all HOLD_SHAPE agents: per-agent
  `formationSlot` (type `Vector2`), `LineMembership`
  (`DEFENSE` / `MIDFIELD` / `ATTACK`), `LaneAssignment`, and
  `DefensiveLineDepth`. Also provides the per-team phase enum
  (`IN_POSSESSION` / `OUT_OF_POSSESSION` / `TRANSITION`) that gates
  all of #14's assignment computation (KD-19). Stage 1 runtime accesses
  these via `BaselineDefensiveShapeView` and the `GetLine(EntityId)`
  accessor per #12 §4.5.2 and §4.5.1 (Stage 1 accessor declarations
  confirmed by ERR-013-008 resolution on May 17, 2026).

### 1.3.3 Cross-Spec Issues Filed at Section-File Draft

- **`ERR-014-001`** — `TacticalContext.MarkDirective?` nullable field
  addition to #8 §2.2.6. **Mechanism selected:** Option B, mirroring
  the `PressDirective?` pattern established by #13's ERR-013-001
  resolution (May 17, 2026). #14 writes `TacticalContext.MarkDirective?`
  per-team per-tick at Stage 1+; #8 §3.1.7 (`MOVE_TO_POSITION`) and
  §3.1.9 (`INTERCEPT`) read it when consulting the HOLD_SHAPE mark
  target. The `MarkDirective?` field is nullable so that a `null` value
  at Stage 0 / before #14 activates is a well-formed signal to #8 that
  no coordinated marking is in effect. **Status:** OPEN — back-prop to
  #8 §2.2.6 to be ratified before Stage 1 activation (FR-DA-037
  gate (a)).
- **`ERR-014-002`** — `MARK_ASSIGNED` channel registration in #17 §3.10
  (Stage 1). Fired when a `MarkAssignment` changes mode or target (not
  on every tick — transitions only).
- **`ERR-014-003`** — `LINE_STEPPED` channel registration in #17 §3.10
  (Stage 1). Fired once per offside-trap step-up event, not per tick.
- **`ERR-014-004`** — `DOMAIN_TAG_DEFENSIVE_AI = 0x1A` allocation in
  #16 §3.4. Proposed value: `0x1A`, the next available slot in the
  Phase B/C block after `DOMAIN_TAG_PRESSING_AI = 0x19` (allocated
  May 17, 2026 via ERR-013-005). Phase B/C block layout: `0x17` = #12
  (ERR-012-001 resolved), `0x18` = reserved (`_RESERVED_0x18_` in
  #16 §3.4 — originally informally noted for #11 before #11 shifted to
  `0x1D`), `0x19` = #13 (resolved), `0x1A` = #14 (this entry),
  `0x1B` = #15 (resolved), `0x1C` = reserved (`_RESERVED_0x1C_` in
  #16 §3.4 — block-end margin), `0x1D` = #11 (resolved, shifted from
  `0x17` because #12 reached `APPROVED` first). **Status:** RESOLVED —
  `DOMAIN_TAG_DEFENSIVE_AI = 0x1A` `[CROSS: #16 §3.4]`, ERR-014-004
  resolved May 18, 2026; allocated in #16 §3.4 v1.0.5.

### 1.3.4 Downstream (declared, not implemented)

#15 Attacking AI — Phase C, NOT STARTED. No Stage 0 or Stage 1 interface
produced against #15 (CLAUDE.md "Interface Design Principle"). #14 and
#15 are mutually exclusive at the team level by possession phase (KD-8);
the phase gating rule in §3.1 (FR-DA-013, KD-19) is the complete coupling
declaration between them at Stage 0.

## 1.4 Key Domain Concepts

| Term | Definition |
|---|---|
| **HOLD_SHAPE pool** | The set of outfield agents that are neither the GK nor assigned a press role (`PRIMARY_PRESS` or `COVER_SHADOW`) by Pressing AI #13 on the current tick. This is the exclusive assignment pool #14 manages. |
| **Mark directive** | The per-team output `MarkDirective` for one tick, carrying team-level parameters: offensive-line depth read, offside-trap active flag, step-up target depth, and emergency flag. |
| **Mark assignment** | The per-agent `MarkAssignment` for one tick, carrying mode and target information for one agent in the HOLD_SHAPE pool. |
| **ZONAL** | Mark mode: agent guards a zone anchored to its #12 `formationSlot`; no specific opponent `EntityId` target. This is the default mode and the safe fallback. |
| **MAN_MARK** | Mark mode: agent directly tracks a specific opponent `EntityId` within `MAN_MARK_CANDIDATE_RADIUS_M [GT]`, selected by highest threat score. |
| **INTERCEPT_RUNNER** | Mark mode: agent tracks a specific opponent on a ball-threatening run into own half (opponent velocity magnitude and direction qualify). Also used as the emergency last-man override mode. |
| **COVER_GK_ZONE** | Mark mode: emergency assignment issued to the nearest DEFENSE-line agent when the GK is detected as significantly out of position via the #7 perception snapshot. |
| **Threat score** | A per-opponent scalar (`perceivedGoalProximity × opponentFirstTouch`) used to rank opponents for mark-assignment priority. All inputs are sourced from the #7 perception snapshot; #14 does not read opponent attributes directly (KD-1). |
| **Displacement cost** | `|agent.position − targetPos|²` in m². Used for assignment optimisation — lower cost preferred; EntityId ascending is the terminal tie-break. |
| **Offside trap** | A coordinated simultaneous advance of all DEFENSE-line agents to a target x-depth, executed on the same tick when trigger conditions are met (ball velocity, hysteresis dwell, phase state). #14 owns the step-up decision; offside adjudication is out of scope (KD-9). |
| **Last-man predicate** | A deterministic per-tick boolean derived from `IsLastManCandidate` and `IsLastManThreat` (KD-12). When both are true, the identified agent is overridden to `INTERCEPT_RUNNER` mode. GK is excluded from this predicate. |
| **Tackle intent** | A per-agent per-tick intent signal (`COMMIT` / `JOCKEY` / `HOLD`) produced by #14 for agents within `TACKLE_ELIGIBLE_RADIUS_M [GT]` of their assigned opponent. #8 reads this to construct an `AgentAction`; #3 owns the contact physics (KD-6). |
| **Anti-chaos invariant** | One of the three measurable constraints in KD-17, enforced before directive publication: minimum backline count (`MIN_BACKLINE_AGENTS`), maximum simultaneous man-mark count (`MAX_MAN_MARK_ASSIGNMENTS`), and maximum mark displacement from #12 anchor (`MAX_MARK_DISPLACEMENT_M`). |
| **Assignment hysteresis** | Per-agent dwell counter tracking how long an agent has held its current `MarkAssignment` mode. Transitions are gated on the `MARK_DWELL_TICKS [GT]` dwell-time pattern from #2 §3.1. |
| **OffsideLineState** | Per-team internal state tracking the current line x-depth, step-up dwell counter, and post-trap cooldown ticks remaining. Digested per #16 §6.2. |

## 1.5 Key Design Decisions

Cross-reference to the 19 KDs catalogued in `outline-detailed.md` v1.0:

| KD | Subject | Resolution Locus |
|---|---|---|
| KD-1 | Cite-not-redefine of CLAUDE.md and upstream-spec invariants | §1.7 |
| KD-2 | 10 Hz tactical loop; no 60 Hz work in #14 | §1.7, §4.1 |
| KD-3 | Boundary with Positioning AI #12 — #12 owns baseline slot, line membership, and depth; #14 reads and overrides HOLD_SHAPE subset | §1.6, §4.4, §4.5 |
| KD-4 | Boundary with Pressing AI #13 — disjoint role partition; #14 owns HOLD_SHAPE subset exclusively | §1.6, §3.2, §4.5 |
| KD-5 | Boundary with Decision Tree #8 — Option B selected: `TacticalContext.MarkDirective?` nullable field via ERR-014-001 (mirrors #13 ERR-013-001 precedent) | §1.6, §4.4 |
| KD-6 | Boundary with Collision System #3 — #14 produces `TackleIntentRequest` intent; #8 mediates dispatch; #3 owns contact physics | §1.6, §3.6 |
| KD-7 | Boundary with Goalkeeper Mechanics #11 — #14 owns defensive wall + `COVER_GK_ZONE`; #11 owns GK saves/positioning; GK position always read via #7 | §1.6, §3.9 |
| KD-8 | Boundary with Attacking AI #15 — mutually exclusive by possession phase; no Stage 0 interface produced | §1.6, §3.1 |
| KD-9 | Offside-line ownership — #14 owns step-up decision; adjudication is out of scope (future referee spec) | §1.2.2, §3.7 |
| KD-10 | Determinism binding (#16) — EntityId-sort, RNG domain tag, digest scope | §3.3, §4.6 |
| KD-11 | Hysteresis pattern reuse from #2 §3.1 (dwell-time + dead-zone) | §3.11 |
| KD-12 | Last-man predicate — formal `IsLastManCandidate` + `IsLastManThreat`; GK excluded; EntityId tie-break | §3.8 |
| KD-13 | Constant-tag discipline (`[GT]` / `[EST]` / `[FIXED]` / `[DERIVED]` / `[CROSS]` / `[CROSS-PENDING]`) | §6.1 |
| KD-14 | Single constant catalogue `DefensiveAIConstants.cs` per #20 §4.2 | §4.2 |
| KD-15 | Event System binding (#17) — `MARK_ASSIGNED` / `LINE_STEPPED` channels at Stage 1 via ERR-014-002/003 | §7.5 |
| KD-16 | Stage-0/Stage-1 scope discipline | §1.8, §7 |
| KD-17 | Three anti-chaos invariants: `MIN_BACKLINE_AGENTS`, `MAX_MAN_MARK_ASSIGNMENTS`, `MAX_MARK_DISPLACEMENT_M` | §3.10 |
| KD-18 | Exploit-resistance test corpus — four canonical exploits | §5 |
| KD-19 | Phase enumeration — #12 `IN_POSSESSION` → all-ZONAL directive; `OUT_OF_POSSESSION` / `TRANSITION` → full algorithm | §3.1 |

## 1.6 Interface Boundaries

Authoritative Boundary Matrix (mirrors `outline-detailed.md` v1.0):

| Boundary | #14 owns | Counterparty owns | Direction | Mechanism | Stage 0? |
|---|---|---|---|---|---|
| #8 Decision Tree | `MarkDirective` per team + per-agent `MarkAssignment` for HOLD_SHAPE agents | Per-agent action loop (`MOVE_TO_POSITION` / `INTERCEPT` scoring) | #8 reads #14 (Stage 1) | `TacticalContext.MarkDirective?` extension via ERR-014-001, Option B (mirrors #13 ERR-013-001 precedent — KD-5) | No (Stage 1 runtime) |
| #12 Positioning AI | Mark-mode overrides for HOLD_SHAPE agents | Baseline out-of-poss `formationSlot`; `LineMembership`; `LaneAssignment`; `DefensiveLineDepth` | Orchestrator composes; both read by #8 | `BaselineDefensiveShapeView` + `GetLine(EntityId)` accessor (Stage 1+; declared in #12 §4.5.1 / §4.5.2) | No (Stage 1) |
| #13 Pressing AI | HOLD_SHAPE agent assignments | `PRIMARY_PRESS` / `COVER_SHADOW` agent assignments | #14 reads #13 role partition | Per-tick `PressAssignment` role filter (KD-4 handoff) | No (Stage 1) |
| #3 Collision System | Tackle intent (`TackleIntentRequest`) | Contact physics, foul detection, impulse response | #8 reads #14 intent, dispatches to #3 | Parameter-based physics pipeline (CLAUDE.md / KD-6) | No (Stage 1 spec; declared at Stage 0) |
| #11 Goalkeeper | Outfield defensive wall (declared #14 scope per #11 FR-GK-016); `COVER_GK_ZONE` assignment | GK positioning, saves, distribution | #14 reads GK position via #7 perception snapshot; no direct #11 accessor | KD-7 boundary; Interface Design Principle (Stage 1 coupling) | No (spec text only) |
| #2 Agent Movement | (none direct — via #8 action output) | 60 Hz steering toward `Action.TargetPosition` | #2 reads #8 | Same composition path as #12 / #13 | No |
| #7 Perception | (none — read consumer only) | Filtered world model | #14 reads #7 | Snapshot read at tick start | Yes (spec text) |
| #15 Attacking AI | (mutually exclusive by possession phase) | In-possession behaviour | Independent | KD-8 phase gating (§3.1 / FR-DA-013) | No |
| #16 Determinism | `MarkDirective` / `MarkAssignment` / `MarkHysteresisState` / `OffsideLineState` / `TackleIntentRequest` digest scope | Digest format + iteration rule | #14 conforms | EntityId iteration + domain-tagged RNG (`DOMAIN_TAG_DEFENSIVE_AI = 0x1A [CROSS: #16 §3.4]` — ERR-014-004 resolved May 18, 2026) | Yes (spec text) |
| #17 Event System | `MARK_ASSIGNED` / `LINE_STEPPED` channel definitions | Channel registry | (deferred) | ERR-014-002 / ERR-014-003 at Stage 1 | No (Stage 1) |
| #18 Performance | (conformance only) | Per-tick budget framework | #14 conforms | §6 budget against named host | Yes (spec text) |
| #19 Testing | (conformance only) | Test-framework conventions | #14 conforms | §5 plan | Yes (spec text) |
| #20 Code Standards | (conformance only) | File / catalogue / naming rules | #14 conforms | `DefensiveAIConstants.cs` per FR-CS-025 | Yes (spec text) |

## 1.7 Coordinate and Convention Bindings (KD-1 cite-not-redefine)

- **Coordinate origin:** corner of pitch at (0, 0, 0); X = 0–105 m
  goal-to-goal; Y = 0–68 m touchline-to-touchline; Z = height (ground
  at Z = 0 m). Cited from Ball Physics #1 §1.2 and Appendix C
  (`XC-014-001`).
- **Own-half normalisation for last-man and line-depth computations:**
  the team defending the x=0 goal treats lower x-values as closer to
  their own goal; the team defending x=105 treats higher x-values as
  closer to their own goal. All last-man predicate and offside-trap
  formulas use a normalised `distanceToOwnGoal` scalar computed once
  per tick per agent so that the same formula body applies to both
  teams without per-team branching.
- **Fatigue convention:** `0.0 = fully rested`, `1.0 = fully fatigued`.
  Cited from CLAUDE.md. Any inversion is a critical error (KD-1, KD-8 FR-DA-008).
- **Tick rates:** 10 Hz tactical (this spec); 60 Hz physics (#1, #2,
  #3). Cited from CLAUDE.md. #14 produces no per-frame work (KD-2).
- **EntityId no-reuse:** bound from #2 §2.5 (`XC-002-001`) and #8
  §1.7.3 (`XC-008-001`) — referenced as `XC-014-002`. Required by
  EntityId-sorted iteration (#16 §3.2.5) and by EntityId terminal
  tie-breaks throughout the assignment algorithm (§3.3–§3.8).
- **Attribute range:** player attributes (`FirstTouch`, `Tackling`,
  `Anticipation`, etc.) are on the integer scale [1–20] per Perception
  System #7 and the master planning volumes. All threat-score and
  tackle-intent formulas normalise these to the continuous range [0, 1]
  before use by dividing by 20.
- **No type enums in physics layer:** #14 produces struct-typed `Vector2`
  target positions and `EntityId?` target identifiers, not enum types
  propagated into the physics pipeline. CLAUDE.md "Parameter-Based
  Physics (No Type Enums)". `TackleIntentRequest.mode` is a #14-owned
  tactical intent enum consumed by #8 before dispatch to #3; it does
  not enter the physics layer directly.

## 1.8 Stage-Binding Statement

**Spec drafted at Stage 0; runtime activates at Stage 1.** Authoritative
basis: Decision Tree #8 §1.3.2 (Features Deferred to Stage 1+:
"Stage 1 — Defensive AI #14 introduces coordinated mark assignments")
and the Phase C tactical-AI activation sequence.

At Stage 0, #8 handles all defensive individual behaviour via its existing
action set (`INTERCEPT`, `PRESS`, `MOVE_TO_POSITION`) without coordination.
#14 introduces the coordinated layer — zonal and man-marking designations
that bias which HOLD_SHAPE agents target which opponents, when to step up
the offside line, and how to manage last-man and emergency scenarios.
This is the same Stage-0-spec / Stage-1-runtime pattern as Pressing AI #13.

Stage 0 deliverable from #14 = published specification only — this document,
the section files, and the appendices. **No runtime code at Stage 0.**
Stage 1 activation requires three preconditions (FR-DA-037):

1. `ERR-014-001` resolved — `MarkDirective?` field ratified in #8 §2.2.6
   (Option B selected; back-prop pending).
2. #12 Positioning AI reaches `APPROVED` (consumed as baseline shape
   source and phase provider).
3. `ERR-014-002` / `ERR-014-003` — #17 channel rows for `MARK_ASSIGNED`
   and `LINE_STEPPED` landed.

Until all three clear, #14 ships as inert specification — exactly the
pattern established by #13 §1.8 for Pressing AI.

## 1.9 Version History

| Version | Date | Author | Summary |
|---|---|---|---|
| 0.1 | May 17, 2026 | AI agent (claude-sonnet-4-6 / draft-defensive-ai) | Initial draft from `outline-detailed.md` v1.0. §1.1–§1.9 authored. Boundary matrix confirmed against `pressing-ai/section-1.md` v0.3 and `outline-detailed.md` v1.0. ERR-014-001..004 declared. KD-1..KD-19 tabulated. Coordinate and convention bindings cited. |
| 0.2 | May 17, 2026 | AI agent | PASS-1 adversarial review fix pass. M7: §1.3.3 ERR-014-004 block layout updated to reflect ERR-011-001/ERR-012-001 race — #11 occupies `0x18` or shifts to `0x1D` depending on which of #11/#12 reaches `APPROVED` first; explicitly notes that #14's `0x1A` slot is stable regardless of that race outcome. |
| 0.3 | May 17, 2026 | AI agent | PASS-3 clean-up. §1.6 boundary matrix row for #7 Perception: removed `Anticipation` from attribute lookups (consistent with M6 fix in §2.3 and XC-014-005 — `Anticipation` is not consumed by #14 at Stage 0); retained `FirstTouch` (threat score §3.5) and `Tackling` (declared for future tackle-quality use). |
| 0.4 | May 18, 2026 | AI agent (adversarial-specs-review-run2-AFrm4) | FAIL-4 fix (A-03): §1.3.3 ERR-014-004 block — `[CROSS-PENDING]` promoted to `[CROSS: #16 §3.4]`, resolved outcome documented (0x1A final, 0x18/0x1D race resolved). §1.6 #16 boundary row — `[CROSS-PENDING]` promoted to `[CROSS: #16 §3.4]`. |
