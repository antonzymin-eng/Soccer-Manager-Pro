# Positioning AI Specification #12 — Section 1: Introduction, Scope, Dependencies, Key Decisions

**Created:** May 15, 2026
**Last Updated:** May 15, 2026 (v0.1 — initial draft from `outline-detailed.md` v1.2)
**Version:** 0.1
**Status:** DRAFT (section-file authoring pass)

---

## 1.1 Purpose

Positioning AI (#12) is the Stage 0 Formation Engine. It computes, on
the 10 Hz tactical loop, the per-agent `Vector2 formationSlot` that
each agent's Decision Tree (#8) `TacticalContext.FormationSlot`
consumes when scoring and resolving the `MOVE_TO_POSITION` action
(#8 §3.1.7, §3.2.6).

Decision Tree #8 §3.1.7.2 references the Formation System as "wired
in Stage 1"; the Stage-0 path uses `TacticalContext.Stage0Default(slot)`
to populate the same field with a hardcoded fallback. Spec #12
promotes that source to Stage 0 because every Phase C tactical-AI
spec (#13 Pressing, #14 Defensive, #15 Attacking) requires a
non-hardcoded formation source to declare its own Stage 1+ boundary
slots. Without #12 at Stage 0, the Phase C linear chain cannot
advance.

This specification is a producer of #8 input. It does not select
actions, does not steer agents, and does not own the `TacticalContext`
schema. It is bound by CLAUDE.md "Project Identity" (Stage 0 Physics
Foundation — no code until all 20 specs approved) and by the
"Interface Design Principle" (no interfaces against unspecified
consumers).

## 1.2 Scope

### 1.2.1 In Scope (Stage 0)

- Anchor computation per role per formation archetype (§3.1).
- Ball-relative offset, piecewise-linear in ball position (§3.2).
- Local phase classification — `InPoss`, `OutOfPoss`, `TransToAtk`,
  `TransToDef` — computed from possession state + filtered ball
  longitudinal travel (§3.0).
- Shape compactness (lateral + vertical scalars) per phase (§3.5).
- Line membership (Defense / Midfield / Attack) and lane occupation
  (LW / LH / C / RH / RW) classification with dead-zone hysteresis
  (§3.3, §3.4).
- Context modifiers (score difference, team-mean fatigue,
  tactical-intensity) composed multiplicatively onto compactness
  (§3.5).
- Hard inter-agent spacing constraint and cost-based displacement
  on violation (§3.6).
- Three formation archetype families (4-4-2, 4-3-3, 4-2-3-1).

### 1.2.2 Out of Scope (deferred to §7)

Per KD-11: authoring tools and coach UI, save-game persistence of
shape state, ML-tuned `[GT]` parameter fitting, set-piece positioning,
custom formation editor, in-match formation switching, telemetry
event channels via #17, and the Press / Run / Mark override layers
owned by #13 / #14 / #15. The ten named formation variants targeted
for `FormationSystem.cs` (Stage 1, Month 3–4 per
`master-development-plan.md` §3.2) are enumerated in §7.6 as the
Stage 1+ expansion roadmap.

## 1.3 Dependencies

### 1.3.1 Approved Upstream

| Spec | Sections Bound | Use |
|---|---|---|
| #1 Ball Physics | §1.2, §3.x | Corner-origin coordinates; ball state schema |
| #2 Agent Movement | §2.5 (XC-002-001), §3.1 | EntityId no-reuse; hysteresis-pattern reuse |
| #7 Perception System | §3.7–§3.10 | Per-agent perception snapshot; possession state |
| #8 Decision Tree | §1.7.3 (XC-008-001), §2.2.6, §3.1.7, §3.2.6 | `TacticalContext.FormationSlot` consumer; `MOVE_TO_POSITION` action |
| #16 Deterministic Simulation | §3.2, §3.2.5, §3.4, §5, §6.2 | EntityId iteration; domain-tagged RNG; per-tick digest |
| #17 Event System | §3 (schema only) | No channels produced or consumed at Stage 0 |
| #18 Performance Optimization Strategy | §3.7, §6 | Zero-allocation hot-path discipline |
| #19 Testing Strategy & Framework | §3, §4 | Determinism-regression and FR-traceability framework |
| #20 Code Standards | §4.2 (FR-CS-025) | Single constant-catalogue file |

### 1.3.2 Pending Cross-Spec

- **`ERR-012-001`** (filed in `spec-error-log.md`): Phase B/C
  domain-tag block allocation `0x16 … 0x1B` covering #10/#11/#12/
  #13/#14/#15 in #16 §3.4. `DOMAIN_TAG_POSITIONING_AI = 0x16` is
  `[CROSS-PENDING]` until lead-developer ratification.
- **`ERR-012-002`** (filed in `spec-error-log.md`): stale
  "Formation System (Spec #14)" reference in
  `decision-tree/section-3-1.md` L716 — Formation System is #12;
  #14 is Defensive AI. One-line patch.

### 1.3.3 Downstream (declared, not implemented)

#13 Pressing AI, #14 Defensive AI, #15 Attacking AI — all Phase C,
NOT STARTED. No Stage 0 interface produced against any of them
(CLAUDE.md "Interface Design Principle").

## 1.4 Key Domain Concepts

| Term | Definition |
|---|---|
| **Anchor** | Per-role pitch-relative target position, derived from the formation archetype lookup table by multiplying normalized offsets against (105 m × 68 m) pitch dimensions. |
| **Ball-relative offset** | Piecewise-linear displacement from the anchor as a function of `(ball.x, ball.y)`, scaled by a per-role-per-phase `pullFactor`. |
| **Lane** | Lateral classification bin ∈ {LW, LH, C, RH, RW} based on Y coordinate. Five bins of width 13.6 m. |
| **Line** | Longitudinal classification ∈ {Defense, Midfield, Attack}, computed by stable k=3 partition of outfield agents (GK excluded). |
| **Phase** | Local enum ∈ {InPoss, OutOfPoss, TransToAtk, TransToDef}, computed from possession state and filtered ball longitudinal travel. |
| **Compactness** | Pair of scalars (lateral, vertical) per phase that scale the spread of the anchor set around its centroid. |
| **`formationSlot`** | The per-agent `Vector2` output written into each agent's `TacticalContext.FormationSlot` at #8 Step 2. |
| **Hysteresis state** | Per-agent dwell counters for anchor, line, and lane membership — authoritative simulation state under #16 §3.2. |

## 1.5 Key Design Decisions

This section cross-references the 17 design decisions catalogued in
`outline-detailed.md` v1.2:

| KD | Subject | Resolution Locus |
|---|---|---|
| KD-1 | Cite-not-redefine of CLAUDE.md and upstream-spec invariants | §1.7 |
| KD-2 | 10 Hz tactical loop; 60 Hz steering owned by #2 | §3.0, §3.7 |
| KD-3 | Boundary with #8 — #12 publishes per-agent `formationSlot`; #8 owns the action loop | §4.1, §4.4, §4.5 |
| KD-4 | Boundary with #13 (Stage 0: no override layer) | §7.3 |
| KD-5 | Boundary with #14 (Stage 0: no override layer) | §7.5 |
| KD-6 | Boundary with #15 (Stage 0: no run system) | §7.4 |
| KD-7 | Formation data ownership: 3 families at Stage 0; 10 variants at Stage 1 per `master-development-plan.md` §3.2 | §3.1, Appendix B, §7.6 |
| KD-8 | Hysteresis pattern reuse from #2 §3.1 | §3.8 |
| KD-9 | Determinism binding to #16 (digest scope; EntityId iteration; RNG domain tag) | §3.9, §4.6 |
| KD-10 | Event System binding — no #17 channels at Stage 0 | §4.3 |
| KD-11 | Stage 0 scope discipline | §1.2.2, §7 |
| KD-12 | Constant-tag discipline | §6.1 |
| KD-13 | Compositor precedence (Stage 0: only intra-#12 concerns) | §3.7 |
| KD-14 | Spacing tie-break is cost-based, EntityId terminal only | §3.6 |
| KD-15 | Per-tick budget pinned against named reference host | §6.3 |
| KD-16 | Float-comparison epsilon at hard-spacing boundary | §3.6 |
| KD-17 | Single constant catalogue `PositioningAIConstants.cs` per #20 §4.2 FR-CS-025 | §4.2, §6.1 |

## 1.6 Interface Boundaries

Authoritative Boundary Matrix:

| Boundary | #12 owns | Counterparty owns | Direction | Mechanism | Stage 0? |
|---|---|---|---|---|---|
| #8 Decision Tree | Per-agent `Vector2 formationSlot` output | Per-agent action loop incl. `MOVE_TO_POSITION`; the frozen `TacticalContext` schema (#8 §2.2.6) | #8 reads #12 (via orchestrator) | Orchestrator copies #12's slot into each agent's `TacticalContext.FormationSlot` at #8 Step 2 | Yes |
| #2 Agent Movement | (none direct — via #8 action output) | 60 Hz steering toward `Action.TargetPosition` | #2 reads #8 | #8's resolved action carries the target | Yes |
| #7 Perception | (none — read consumer) | Filtered world model | #12 reads #7 | Snapshot read at tick start | Yes |
| #13 Pressing | Schema slot for future `PressOverride` (declared §7.3) | Press trigger + displacement | (deferred) | Stage 1+ only | No |
| #14 Defensive | Read-only `LineMembership` / `LaneAssignment` exposed on #12 subsystem | Mark/cover assignment | (deferred) | Stage 1+ only | No |
| #15 Attacking | Schema slot for future `RunIntent` (declared §7.4) | Off-ball run system | (deferred) | Stage 1+ only | No |
| #16 Determinism | `formationSlot` + hysteresis-state digest contribution | Digest format; iteration rule | #12 conforms | EntityId-sorted iteration; domain-tagged RNG | Yes |
| #17 Event System | (none at Stage 0) | Channel registry | (deferred) | No channels produced or consumed at Stage 0 | No |
| #20 Code Standards | (conformance only) | File / catalogue / naming rules | #12 conforms | Single `PositioningAIConstants.cs` per FR-CS-025 | Yes |

## 1.7 Coordinate and Convention Bindings (KD-1 cite-not-redefine)

- **Coordinate origin:** corner of pitch at (0, 0, 0); X = 0–105 m
  goal-to-goal, Y = 0–68 m touchline-to-touchline, Z = height. Cited
  from Ball Physics #1 §1.2.
- **Fatigue convention:** `0.0 = fully rested`, `1.0 = fully
  fatigued`. Cited from CLAUDE.md. Used by §3.5
  `FATIGUE_LATERAL_RELAX_M`.
- **Tick rates:** 10 Hz tactical (this spec); 60 Hz physics (#1, #2,
  #3). Cited from CLAUDE.md. #12 produces no per-frame work.
- **EntityId no-reuse:** bound from #2 §2.5 (XC-002-001) and #8
  §1.7.3 (XC-008-001). Required by §3.6 cost-based tie-break and by
  #16 EntityId-sorted iteration order.
- **No type enums in physics layer:** #12 produces a `Vector2`
  position, not a `PositionType` enum. CLAUDE.md "Parameter-Based
  Physics".

## 1.8 Version History

| Version | Date | Author | Summary |
|---|---|---|---|
| 0.1 | May 15, 2026 | AI agent (claude/draft-positional-ai-specs-MOejb) | Initial section-file draft from `outline-detailed.md` v1.2. |
