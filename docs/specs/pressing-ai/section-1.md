# Pressing AI Specification #13 — Section 1: Introduction, Scope, Dependencies, Key Decisions

**Created:** May 17, 2026
**Last Updated:** May 17, 2026 (v0.2 PASS-1 adversarial-review fix pass)
**Version:** 0.2
**Status:** DRAFT (section-file authoring pass)
**Source:** `outline-detailed.md` v1.0 (May 16, 2026)

---

## 1.1 Purpose

Pressing AI (#13) specifies **coordinated** out-of-possession pressing
behaviour: the per-team logic by which a defending side deliberately
commits 1–3 agents to closing the ball-carrier and shadowing nearby
passing lanes, while the remaining agents hold their #12 baseline
shape. The spec exists to (a) name the canonical **trigger catalog**
(§3.1, KD-7), (b) define **per-agent role assignment** (§3.3–§3.4,
KD-8), (c) enforce three measurable **anti-chaos invariants** (§3.9,
KD-16), (d) provide **disengage and reset** logic (§3.8), and (e)
declare the integration surface with Decision Tree #8 — where the
runtime activation lands at **Stage 1** per #8 §1.3.2.

This specification is a producer of one `PressDirective` per team per
tick plus one `PressAssignment` per agent per tick. It does **not**
own the per-agent action loop (that is #8), does **not** steer
agents at 60 Hz (that is #2), and does **not** redefine perception,
fatigue, or coordinate conventions (cite-not-redefine — KD-1).

It is bound by CLAUDE.md "Project Identity" (Stage 0 Physics
Foundation — no code until all 20 specs approved) and the
"Interface Design Principle" (no interfaces against unspecified
consumers — #14 and #15 are NOT STARTED).

## 1.2 Scope

### 1.2.1 In Scope (specification text — Stage 0 deliverable)

- Trigger detection from four canonical upstream surfaces (§3.1,
  KD-7): `BAD_TOUCH` from First Touch #4; `BACKWARD_PASS` from Pass
  Mechanics #5; `SIDELINE_TRAP` from ball + pitch geometry; and
  `WEAK_RECEIVER` from Perception #7.
- Per-team `PressDirective` computation (§3.11).
- Per-agent `PressAssignment` with three-role disjoint partition
  (§3.3, §3.4 — KD-8): `PRIMARY_PRESS`, `COVER_SHADOW`, `HOLD_SHAPE`.
- Cover-shadow lane geometry (§3.5).
- Trigger debounce, role hysteresis, and disengage/reset timing
  (§3.2, §3.6, §3.8) bound to the Agent Movement #2 §3.1 hysteresis
  pattern (KD-9).
- Stamina cost accumulation and fatigue-ceiling exclusion (§3.7).
- Three measurable anti-chaos invariants (§3.9, KD-16).
- Canonical exploit-resistance test corpus (§5.6, KD-17).
- Parameter catalogue (§6.1).

### 1.2.2 Out of Scope (deferred to §7 per KD-12)

Runtime code (Stage 1 deliverable); the #8 §2.2.6 / §3.1.8.2
back-prop amendment text itself (filed as `ERR-013-001` separately);
#17 channel registration (`PRESS_TRIGGERED` / `PRESS_DISENGAGED` —
`ERR-013-002` / `ERR-013-003` at Stage 1); #14 handoff
implementation; authoring tools and coach UI; save-game persistence;
ML-tuned `[GT]` parameter fitting; set-piece pressing; custom
press-style editor; goalkeeper-as-pivot specialised handling (the
goalkeeper is always `HOLD_SHAPE` per KD-13).

## 1.3 Dependencies

### 1.3.1 Approved Upstream

| Spec | Sections Bound | Use |
|---|---|---|
| #1 Ball Physics | §1.2 | Corner-origin coordinates; ball state schema for sideline geometry |
| #2 Agent Movement | §2.5 (`XC-002-001`), §3.1 | EntityId no-reuse; hysteresis pattern (dwell-time + dead-zone) |
| #4 First Touch | §3.1 (control quality `q`), §3.5 (`pressureScalar`) | `BAD_TOUCH` trigger source — `q` is the ground-truth surface (see Q2 note in §2.3) |
| #5 Pass Mechanics | §2 FR-10 (`PassAttemptEvent` published at `CONTACT`) | `BACKWARD_PASS` trigger source: #13 computes pass direction from passer position (perception snapshot `agents[e.AgentID].position`) → `e.TargetPosition` and dots against the attacking direction |
| #7 Perception System | §3.7–§3.10 | Filtered world model: agent positions, ball state, possession owner, `isActive` |
| #8 Decision Tree | §1.3.2, §1.7.2, §1.7.3 (`XC-008-001`), §3.1.8 (+ §3.1.8.1, §3.1.8.2), §3.2.7 | Stage-1 binding row (§1.3.2 deferral prose L231–232 and table row L426; §1.7.2 soft-dependency row L467); PRESS utility surface this spec advises; EntityId no-reuse |
| #16 Deterministic Simulation | §3.2, §3.2.5, §3.4, §5, §6.2 | EntityId iteration; domain-tag registry; per-tick digest scope |
| #17 Event System | §3.10 (channel registry — Stage 1 back-prop) | No channels produced or consumed at Stage 0 |
| #18 Performance | §3.7, §6 | Zero-allocation hot-path discipline; per-tick budget framework |
| #19 Testing | §3, §4 | Test taxonomy + FR-traceability framework |
| #20 Code Standards | §4.2 (FR-CS-025) | Single constant-catalogue file `PressingAIConstants.cs` |

### 1.3.2 Pending Upstream

- **#11 Goalkeeper Mechanics** — `IN REVIEW` (May 16, 2026). Bound
  only to the negative invariant KD-13: GK is never `PRIMARY_PRESS`
  or `COVER_SHADOW`. No surface read at Stage 0 or Stage 1.
- **#12 Positioning AI** — `IN REVIEW` (May 16, 2026). Bound as the
  **baseline shape source** for `HOLD_SHAPE` agents and as the
  reservation site for the `PressOverride` displacement layer
  (#12 §7.3). Stage 1 runtime composes #13 over #12.

### 1.3.3 Cross-Spec Issues Filed at Section-File Draft

- **`ERR-013-001`** — back-prop into #8 §3.1.8.2 (or §2.2.6,
  mechanism deferred per KD-3 / OI-001) adding a read of #13's
  `PressAssignment`.
- **`ERR-013-002`** — `PRESS_TRIGGERED` channel registration in
  #17 §3.10 (Stage 1).
- **`ERR-013-003`** — `PRESS_DISENGAGED` channel registration in
  #17 §3.10 (Stage 1).
- **`ERR-013-004`** — verified present at section-file draft:
  `decision-tree/section-3-1.md` L753 reads "Fatigue System #13",
  but #13 is **Pressing AI**; the Stage 1 Fatigue System is a
  separate (unallocated) spec. One-token patch request.
- **`ERR-013-005`** — `DOMAIN_TAG_PRESSING_AI = 0x19` allocation in
  #16 §3.4. Inherits the Phase B/C block proposed by ERR-012-001
  (shifted to `0x17…0x1C` on May 16, 2026 after #10 took `0x16` via
  ERR-010-001). Whichever spec in the block reaches `APPROVED` first
  claims the next-available slot; #13 expects `0x19` per the
  current block ordering #10 / #11 / #12 / #13 / #14 / #15.
- **`ERR-013-007`** — back-prop into #12 §4 to publish
  `GetPhase(TeamId)` as a Stage 1 accessible (currently internal-only
  per #12 §4.4.3; needed by #13 §3.11 KD-11 phase gate and §4.4.3).
  Filed v0.2 fix pass (AR-S1-H4).
- **`ERR-013-008`** — back-prop into #12 §4 to publish
  `GetLine(EntityId)` as a Stage 1 accessor (currently Stage 1+ only
  per #12 §4.5.1; needed by #13 §3.9 invariant (2) KD-16 backline
  floor). Filed v0.2 fix pass (AR-S1-H4).

**Renumbering note (AR-S1-M2):** `outline-detailed.md` KD-10
originally proposed `ERR-013-001` for both the #8 back-prop AND the
#16 domain-tag back-prop. Section-file draft split these into two
distinct back-props and renumbered the domain-tag request to
`ERR-013-005` to avoid collision. This split is documented here and
in §8.4 for traceability.

### 1.3.4 Downstream (declared, not implemented)

#14 Defensive AI and #15 Attacking AI — both Phase C, NOT STARTED.
No Stage 0 or Stage 1 interface produced against either (CLAUDE.md
"Interface Design Principle"). The KD-5 / KD-6 boundary rules below
are declarations, not contracts.

## 1.4 Key Domain Concepts

| Term | Definition |
|---|---|
| **Trigger** | A boolean condition derived from an upstream perception/event surface that, after debounce (§3.2), authorises a press for one tick. |
| **Directive** | The per-team output `PressDirective` for one tick, naming the primary presser, the cover-shadow set, and the disengage / reset state. |
| **Assignment** | The per-agent `PressAssignment` for one tick, carrying the agent's role and (where applicable) a target `EntityId` and a target `Vector2`. |
| **Primary press** | The single agent assigned to close the ball-carrier directly. At most one per team per tick (FR-PR-015). |
| **Cover shadow** | An agent assigned to occupy the line between the ball-carrier and a candidate receiver, biased slightly past the midpoint toward the receiver (`COVER_SHADOW_LANE_FRACTION = 0.55`). |
| **Shadow lane** | The geometric segment from ball-carrier to candidate receiver; the shadow position is `lerp(carrier, receiver, COVER_SHADOW_LANE_FRACTION)`. |
| **Hold shape** | The default role; the agent's slot remains the #12 `formationSlot` for this tick. |
| **Trap** | A coordinated press configuration that funnels the ball-carrier toward a sideline or designated zone. Stage 0 spec covers timing only; trap-zone authoring is Stage 1+ (§7.3). |
| **Disengage** | The transition from a non-trivial press back to all-`HOLD_SHAPE` after the trigger condition has cleared for `DISENGAGE_TIMEOUT_TICKS`. |
| **Reset latency** | The cooldown period following disengage during which no new press can fire (`RESET_LATENCY_TICKS`). |
| **Anti-chaos invariant** | One of the three measurable constraints in KD-16 enforced before publication. |

## 1.5 Key Design Decisions

Cross-reference to the 17 KDs catalogued in `outline-detailed.md` v1.0:

| KD | Subject | Resolution Locus |
|---|---|---|
| KD-1 | Cite-not-redefine of CLAUDE.md and upstream-spec invariants | §1.7 |
| KD-2 | 10 Hz tactical loop; no 60 Hz work | §1.7, §4.1 |
| KD-3 | Boundary with Decision Tree #8 (Stage-1 runtime binding; mechanism deferred — OI-001) | §1.6, §4.4, §4.5 |
| KD-4 | Boundary with Positioning AI #12 — bias not replace | §1.6, §4.5 |
| KD-5 | Boundary with Defensive AI #14 — Stage 1+ disjoint partition | §7.4 |
| KD-6 | Boundary with Attacking AI #15 — mutually exclusive by possession phase | §1.6 |
| KD-7 | Trigger catalog — four canonical triggers each cited upstream | §3.1 |
| KD-8 | Three-role disjoint partition | §3.3, §3.4 |
| KD-9 | Hysteresis pattern reuse from #2 §3.1 | §3.2, §3.6 |
| KD-10 | Determinism binding to #16 | §3.11, §4.6 |
| KD-11 | Event System binding (#17 channels Stage 1+); phase consumed from #12 | §7.5 |
| KD-12 | Stage-0/Stage-1 scope discipline | §1.8, §7 |
| KD-13 | Goalkeeper excluded from press roles | §3.3, FR-PR-017 |
| KD-14 | Constant-tag discipline | §6.1 |
| KD-15 | Single constant catalogue `PressingAIConstants.cs` | §4.2 |
| KD-16 | Three measurable anti-chaos invariants | §3.9 |
| KD-17 | Canonical exploit-resistance test corpus | §5.6, Appendix E |

## 1.6 Interface Boundaries

Authoritative Boundary Matrix (mirrors `outline-detailed.md` v1.0):

| Boundary | #13 owns | Counterparty owns | Direction | Mechanism | Stage 0? |
|---|---|---|---|---|---|
| #8 Decision Tree | `PressDirective` per team + per-agent `PressAssignment` | Per-agent action loop incl. PRESS utility scoring (#8 §3.1.8) | #8 reads #13 (at Stage 1) | Read-only accessor OR `TacticalContext.PressDirective` field extension via #8 §2.2.6 amendment — mechanism deferred (OI-001 / KD-3) | No (Stage 1 runtime) |
| #12 Positioning AI | `PressOverride` displacement consumed by orchestrator | Baseline out-of-possession `formationSlot` | Orchestrator composes; both read by #8 | Per-agent slot override pre-#8 read (#12 §7.3 reservation) | No (Stage 1) |
| #2 Agent Movement | (none direct — via #8 action output) | 60 Hz steering toward `Action.TargetPosition` | #2 reads #8 | Same path as #12 | No |
| #4 First Touch | (none — read consumer) | Control quality `q` / `pressureScalar` | #13 reads #4 (perception-propagated; see Q2 note in §2.3) | Snapshot field at tick start | Yes (schema only) |
| #5 Pass Mechanics | (none — read consumer) | `PassAttemptEvent` at `CONTACT` (#5 §2 FR-10) | #13 reads #5 | Per-tick event ring read; #13 computes pass direction from `e.AgentID` passer position to `e.TargetPosition` and dots locally | Yes (schema only) |
| #7 Perception | (none — read consumer) | Filtered world model | #13 reads #7 | Snapshot read at tick start | Yes |
| #11 Goalkeeper | (KD-13 invariant: GK never assigned press roles) | GK slot ownership | independent | KD-13 negative invariant; FR-PR-017 | n/a |
| #14 Defensive | Pressing-role-owned agents | Cover/zonal-role-owned agents | Disjoint partition per tick | KD-5 handoff rule (Stage 1+) | No |
| #15 Attacking | (mutually exclusive by possession phase) | In-possession behavior | independent | KD-6 phase gating | No |
| #16 Determinism | `PressDirective` / `PressAssignment` / hysteresis + debounce state digest scope | Digest format; iteration rule | #13 conforms | EntityId iteration + `DOMAIN_TAG_PRESSING_AI` `[CROSS-PENDING]` | Yes (spec text) |
| #17 Event System | `PRESS_TRIGGERED` / `PRESS_DISENGAGED` channel definitions | Channel registry | (deferred) | `ERR-013-002` / `ERR-013-003` at Stage 1 | No (Stage 1) |
| #18 Performance | (conformance only) | Per-tick budget framework | #13 conforms | §6 budget against named host | Yes (spec text) |
| #19 Testing | (conformance only) | Test-framework conventions | #13 conforms | §5 plan | Yes (spec text) |
| #20 Code Standards | (conformance only) | File / catalogue / naming rules | #13 conforms | `PressingAIConstants.cs` per FR-CS-025 | Yes (spec text) |

## 1.7 Coordinate and Convention Bindings (KD-1 cite-not-redefine)

- **Coordinate origin:** corner of pitch at (0, 0, 0); X = 0–105 m
  goal-to-goal, Y = 0–68 m touchline-to-touchline, Z = height.
  Cited from Ball Physics #1 §1.2 (`XC-013-001`).
- **Fatigue convention:** `0.0 = fully rested`, `1.0 = fully
  fatigued`. Cited from CLAUDE.md. Bound by §3.7 stamina cost
  accumulation and `PRESS_FATIGUE_CEILING` exclusion.
- **Tick rates:** 10 Hz tactical (this spec); 60 Hz physics (#1, #2,
  #3). Cited from CLAUDE.md. #13 produces no per-frame work
  (KD-2).
- **EntityId no-reuse:** bound from #2 §2.5 (`XC-002-001`) and #8
  §1.7.3 (`XC-008-001`) — referenced as `XC-013-002`. Required by
  EntityId-sorted iteration (#16 §3.2.5) and by EntityId terminal
  tie-breaks in cover-shadow selection (§3.4).
- **No type enums in physics layer:** #13 produces struct-typed
  `Vector2` target positions and an `EntityId?` target, not a
  `PressType` enum. CLAUDE.md "Parameter-Based Physics".

## 1.8 Stage-Binding Statement

**Spec drafted at Stage 0; runtime activates at Stage 1.** Authoritative
basis: Decision Tree #8 §1.3.2 (Features Deferred to Stage 1+, table row
L426: "No coordinated pressing... Stage 1 — Pressing AI #13 introduces
coordinated press triggers") and #8 §1.7.2 (Soft Dependencies table row
at L467: "Pressing AI #13 (Stage 1) — Coordinated press state — DT will
consult before scoring PRESS").

Stage 0 deliverable from #13 = published specification only — this
document, the ten section files, and the appendices. **No runtime
code at Stage 0.** Stage 1 activation requires three preconditions
(FR-PR-044):

1. #8 ratifies the `ERR-013-001` amendment text (mechanism per OI-001).
2. #12 reaches `APPROVED` (consumed as baseline shape source).
3. `ERR-013-002` / `ERR-013-003` #17 channel rows land.

Until all three clear, #13 ships as inert specification — exactly
the pattern #12 §7.3 reserves for the `PressOverride` slot.

## 1.9 Version History

| Version | Date | Author | Summary |
|---|---|---|---|
| 0.1 | May 17, 2026 | AI agent (claude/draft-ai-specification-5tvwH) | Initial draft from `outline-detailed.md` v1.0. |
| 0.2 | May 17, 2026 | AI agent (claude/fix-ai-specs-review-qgWFR) | PASS-1 adversarial fix pass. AR-S1-H1: `#8 §1.4.21` → `#8 §1.3.2`; `#8 §1.5` → `#8 §1.7.2` in §1.1, §1.3.1, §1.8. AR-S1-H2: `#5 §2 FR-08` → `FR-10`; `passVelocity` → direction from passer position → `TargetPosition` in §1.3.1, §1.6. AR-S1-H4: ERR-013-007 / ERR-013-008 back-prop requests filed in §1.3.3. AR-S1-M2: ERR renumbering note added to §1.3.3. |
