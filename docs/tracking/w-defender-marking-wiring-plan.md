# W13 — Defender marking wiring plan

> **Created:** November 10, 2026
> **Version:** 0.1
> **Status:** DRAFT — under review (planning only; no code, spec or tracking file is changed by this document)
> **Purpose:** Make Defensive AI #14's mark assignment drive defender movement, and first diagnose, from
> PR #543's retained traces, whether its absence explains the formation/Duty scoring sensitivity.
> **Inputs:** main `d4b026aa`; draft PR #543 branch `origin/codex/434-formation-duty` (not merged).
> **Toolchain note:** SDK 8.0.133 / runtime 8.0.33 is usable here for compile/test gates
> (`bash tools/dotnet-ci/run-gate.sh --fast --test-filter <f>`); it is **not** the frozen-lane
> toolchain (SDK 8.0.426 / CLR 8.0.32) and must not produce result-bearing six-seed captures. Nothing
> in this plan was compiled, run or measured.

## 0. Recommendation in brief

1. **Diagnose first** (§3): measurement-only Python over #543's retained traces; no new captures.
2. **W13.0 — defect landing, separate PR, on main:** #14 computes "own goal" from team id while the
   engine hands it a canonical frame, so the away team's assignments, last-man and tackle coverage use
   the wrong goal (D1); retained assignments keep a stale target position (D2). Both are in the live
   tackle chain; fix, characterize, adopt new digests.
3. **W13.1 — wiring, on main, off by default:** a pure `MarkTargetComposer` (#14) turns a non-ZONAL
   assignment into a goal-side point at a stand-off; the orchestrator mirrors it into a new per-agent
   `TacticalContext` carrier; #8 `MOVE_TO_POSITION` uses it instead of the slot. A new anti-chaos
   Invariant 4 (one marker per opponent) lands with it. Gated by a config-level `[GT]` that ships
   `false`: exact identity at default, no snapshot schema change, no RNG.
4. **Flip the default** only as a separate owner decision after the §10 six-seed measurement.

## 1. Verified current state (main `d4b026aa`)

### 1.1 The chain, link by link

| # | Link | Evidence | State |
|---|---|---|---|
| L1 | #12 slot: canonical slot → world slot → `TacticalContext.Stage0Default(worldSlot)` | `MatchEngine.cs:3821-3839`, mirror `:4781-4787` | live |
| L2 | #13 press role → #14 snapshot `PressRole` | `MatchEngine.cs:4697`; exclusion `HoldShapePoolFilter.cs:357-366` (also GK, inactive) | live |
| L3 | #14 snapshot fill: all 22 agents in the defending team's **canonical** attack-+X frame; `DefensiveTeamId = team`; `BaselineSlot`/`Line` from #12 | `MatchEngine.cs:4613-4703` (`:4629`, `:4689`, `:4694-4696`) | live |
| L4 | #14 tick: phase gate (InPoss ⇒ all ZONAL) → pool → last-man/COVER_GK_ZONE overrides → `MarkAssigner.Assign` → tackle intent → offside trap → `InvariantEnforcer` | `DefensiveAITick.cs:145-150,153,178-238,247,250,258,263` | live |
| L5 | Assignment: 15 m candidate radius × `MarkingOrientation` scalar, threat = x-proximity × FirstTouch, INTERCEPT_RUNNER > MAN_MARK > ZONAL, ties cost then EntityId | `MarkAssigner.cs:115-117,119-183,203-218,228-252` | live |
| L6 | Hysteresis: 4-tick dwell to commit, 4-tick lock | `MarkHysteresis.cs:27-90`; `DefensiveAIConstants.cs:169` | live |
| L7 | Orchestrator reads only `MarkDirective.OffensiveLineDepth` → `ctx.DefensiveLineDepth`, and sets `ctx.HasMarkDirective = !teamHasPossession` | `MatchEngine.cs:3801,3870-3871` | live / stub |
| L8 | `HasMarkDirective` consumers | only `TestOnly_HasMarkDirective` (`MatchEngine.cs:3034`); field labelled stub `TacticalContext.cs:141-142` | **no consumer** |
| L9 | Per-agent assignment → orchestrator (`GetAssignment`) | `DefensiveAITick.cs:309-319`; no production caller (grep) | **missing** |
| L10 | #8 MOVE target = depth-adjusted own slot | `OptionGenerator.cs:725-749`, `TacticalContext.cs:196-201` | live, slot only |
| L11 | #8 PRESS: carrier within 8 m, else **nearest opponent within 8 m** | `OptionGenerator.cs:753-818`; `UtilityWeights.cs:332` | live |
| L12 | #8 INTERCEPT: ball-path geometry, no mark input | `OptionGenerator.cs:822-902` | live, no mark |
| L13 | Movement command: MOVE → sprint ≥15 m / jog ≥6 m / walk; PRESS → `PressSprint` | `ActionDispatcher.cs:221-256`; `TacticalWeights.cs:53-54` | live |
| L14 | Tackle intent only vs **assigned** opponent within 3 m | `TackleIntentEvaluator.cs:49,64`; `DefensiveAIConstants.cs:183` | live |
| L15 | Tackle resolution reads #14 intents | `MatchEngine.cs:3628,4104,4157,4289` | live |
| L16 | Directive `OffsideTrapActive`/`StepUpTargetDepth`/`EmergencyFlag`, `MarkAssignment.TargetPosition` | grep: no reader outside `defensive-ai` (serializer only, `MatchEngine.cs:8044-8073`) | **no consumer** |

**Missing:** L8/L9 — nothing turns an assignment into a position. Assignments reach gameplay only
through tackle intents (L14) and through the trap's demotion of DEFENSE-line marks to ZONAL
(`OffsideTrapController.cs:85-136`), which removes those agents' tackle intents.

### 1.2 What the anti-chaos and line machinery guarantees today

| Item | Guarantee (code) | Constrains anything that moves? |
|---|---|---|
| `InvariantEnforcer` Inv. 1 `MinBacklineAgents=3` | ≥3 DEFENSE-line pool agents ZONAL (`InvariantEnforcer.cs:93-151`) | No — assignments only |
| Inv. 2 `MaxManMarkAssignments=4` | ≤4 MAN_MARK; INTERCEPT_RUNNER **not** counted (`:156-197`) | No |
| Inv. 3 `MaxMarkDisplacementM=20` | non-ZONAL `TargetPosition` ≤20 m from `BaselineSlot` (`:202-233`) | No |
| `HoldShapePoolFilter` | GK, PRIMARY_PRESS, COVER_SHADOW, inactive excluded (`:357-366`) | No |
| `LastManDetector` | deepest pool agent gets INTERCEPT_RUNNER on most advanced attacker; COVER_GK_ZONE when GK > 15 m out (`LastManDetector.cs:65-182`) | Only via tackle intents (last man never COMMITs) |
| `OffsideTrapController` / `LineCoherenceThresholdM=8` | trap arms on slow ball in opponent half, coherent line, no press; writes ZONAL target `x = step depth` (`:39-136`) | No (target unread) |

None of these constrain movement; they become live constraints only when L9 exists.
**Backlog:** `match-engine-wiring-backlog.md:627-632` (C10) records that `MarkAssigner` has no ball
model; no item covers the missing movement consumer. Highest item is W12, so this is **W13**.

### 1.3 Defects and facts found while verifying (not in the brief)

- **D1 — frame mismatch (live, home/away asymmetric).** `LastManDetector.DefendsX0(teamId) => teamId == 0`
  (`LastManDetector.cs:190`) is used at `MarkAssigner.cs:41,205`, `LastManDetector.cs:81`,
  `OffsideTrapController.cs:39`, `TackleIntentEvaluator.cs:44`. The snapshot is canonical (own goal at
  x = 0 for **both** teams, `MatchEngine.cs:4615,4689`), but `DefensiveSnapshot.cs:24` documents world
  space. For the away team #14 therefore takes x = 105 as its own goal: threat favours opponents near
  the *other* goal, INTERCEPT_RUNNER fires for opponents running away, the "last man" is the most
  advanced away player, and tackle coverage depth counts the wrong teammates. No #14 test uses
  `DefensiveTeamId = 1` (only `DefensiveAITests.cs:40`, team 0). The engine does not swap ends
  (#21 §3 text, `tactical-instructions/section-3.md:153`). Effect size is **unmeasured**; it is a
  candidate contributor to the C3/C4 home/away asymmetry below, nothing more.
- **D2 — stale target.** A retained non-ZONAL assignment keeps its commit-time `TargetPosition`
  (`MarkHysteresis.cs:39` dwell lock; `:58` same-target path), contradicting #14 §2.2.2 ("opponent's
  current perceived position", `section-2.md:107-108`). Inv. 3 tests that stale point.
- **D3 — no exclusivity.** #14 §3.3.3 (`section-3.md:165-247`) lets several defenders pick the same
  opponent; with identical attributes threat is x-only, so every defender in range picks the deepest
  attacker. Harmless while nothing moves; a convergence pathology once it does.
- **D4 — trap units.** `snap.DefensiveLineDepth` is `Clamp01(...)` = 0.5 at default
  (`MatchEngine.cs:4642-4644`), but `OffsideTrapController.cs` treats it as metres (step depth
  0.5 + 3 = 3.5 m from goal) and `MarkDirective.cs` documents metres. Not consumed by W13.1; follow-up ERR.
- **D5 — de facto marking already exists.** PRESS priority 2 (nearest opponent ≤ 8 m) plus MOVE
  suppression at 6 m (`UtilityWeights.cs:194-195`) already pull a defender onto any opponent within 8 m.
- **Possession witness.** On #543 C3 seed 1, controlled-ball frames are 31,668 of 323,999 live; the
  team-possession witness (carrier ∪ pass-in-flight receiver) covers 299,475. Use the witness.
- **Substitutes reuse the agent index** (`MatchEngine.cs:1845`); #14 per-entity state is not reset, so a
  substitute inherits the outgoing player's assignment/hysteresis.
- **Restarts are instantaneous:** `ApplyRestart` gives possession to a taker at once
  (`MatchEngine.cs:5923-5955`) and `MatchPhase` is always `OPEN_PLAY` (`:6463`). There is no dead-ball phase to exempt.

### 1.4 Geometry claims (anchors × 105 / × 68; `PositioningAIConstants.cs:300-393`)

| Claim | Computation (world, home attacking +X vs away F442) | Verified |
|---|---|---|
| F442 ST ↔ CB 3.5 m | ST1 (81.9, 28.56) vs away CB2 (84.0, 25.84): 3.44 m | yes |
| F442 midfielders coincide | home CM1 (52.5, 25.84) = away CM2 world (52.5, 25.84) | yes |
| F4231 ST 8.5 m from each CB, ~2 m beyond line | ST (86.1, 34) vs CBs (84.0, 25.84/42.16): 8.43 m; Δx 2.1 m | yes |
| F4231 CAM ~18 m from any slot | CAM (68.25, 34) vs away CMs/CBs: 17.7 m | yes |
| Attack Duty = F442 shape +3 m | `SlotComposer` on #543 adds `DutyForeOffsetM` {−3,0,+3} to anchor x | yes |

With +3 m, home strikers sit at x = 84.9, i.e. 0.9 m **beyond** the away CB line, still 2.85 m away;
home defenders move to x = 24, level with/ahead of away strikers (x = 23.1). The 8 m PRESS radius does
not separate Duty from default; a goal-side hypothesis can (§3). #543 per-team goals (six matches,
base → head): C3 home F4231 20 → 71 (away 27 → 15); C4 away F4231 away 27 → 39 (home 20 → 17); C1 home
F433 20 → 53; C2 away F433 27 → 64; C5 home Defend 24 → 5; C7 home Attack 34 → 68; C8 away Attack
26 → 57 (`analysis/per-match-team.csv` on #543).

## 2. Spec position and ERR plan

| Spec | What it says | Gap |
|---|---|---|
| #14 §4.4.4 (`section-4.md:158-180`) | orchestrator writes team-level `MarkDirective`; #8 "adjusts target via MarkAssignment" in MOVE and INTERCEPT | a team struct cannot carry a per-agent target; no formula |
| #14 §4.5.1 (`:184-198`) | orchestrator composes per-agent #8 target from `GetAssignments()`; pressers keep #13 target | no composition rule; #13 targets have no consumer (backlog C1 note) |
| #14 §2.2.2 (`section-2.md:86-111`) | `targetPosition` = current perceived opponent position | code stale (D2) |
| #14 §3.3 / §3.10 | assignment + three invariants | no exclusivity (D3); no stand-off or goal-side |
| #14 §3.5.2 (`section-3.md:316-320`) | `ownGoalCenter = defendsX0 ? (0,34) : (105,34)` | frame of `defendsX0` unstated (D1) |
| #14 §7.1/§7.2 | activation preconditions; manual man-mark Stage 2+ | contradicts #21 §3.5 (FR-TI-023 approved request semantics) |
| #8 §2.2.6 (`section-2-1-to-2-2.md:745-753,817`) | nullable team `MarkDirective?` | no per-agent target |
| #8 §3.1.7.2 (`section-3-1.md:873-897`) | slot target; RUNNER override precedent | no MARK override |
| #8 §3.1.9 | nothing about marks | INTERCEPT consumption unspecified |
| #12 | slot authority; Duty offsets (on #543, ERR-012-013) | none for this landing |
| #13 | press roles exclude agents from #14 pool | press targets unconsumed (separate item, C9) |
| #21 §2.2.2/§3.4/§3.5 | `MarkingOrientation` scales radius; `TightMarking` "tighter mark distance"; `MarkTargetEntityId` request | #14 has no mark distance |
| #23 | `MARKING_RADIUS_M = 3.0` defines "marked" for dismarking/FM-DM-03 (`PositioningAIConstants.cs:231`) | none; reuse as bound |

**ERR entries** (allocate with `.claude/skills/err-file-and-backprop/scripts/next_err_id.sh <spec>` at
authoring and again after every rebase; never treat an id here as reserved). At `d4b026aa` the script
reports next free ERR-014-007, ERR-008-026, ERR-021-009, ERR-012-013 — but #543 already uses
ERR-012-013, ERR-021-009 and ERR-025-001. ERR-014-001 is cited in #8 §2.2.6 but no index row was found
by grep in `spec-error-log.md`.

| Entry | Spec | Content | Lands with |
|---|---|---|---|
| E1 | #14 | D1 frame contract: snapshot is the defending team's canonical frame, own goal x = 0; `defendsX0` resolved from the snapshot, not the team id | W13.0 |
| E2 | #14 | D2: refresh `TargetPosition` of retained non-ZONAL assignments to the live position each tick | W13.0 |
| E3 | #14 | consumer contract: new §3.14 composition rule, §3.10 Invariant 4, §4.4.4/§4.5.1 rewritten to a per-agent carrier; stand-off constants; INTERCEPT consumption deferred by name | W13.1 |
| E4 | #8 | §2.2.6 per-agent `MarkTarget` carrier; §3.1.7.2 MARK override (after RUNNER precedent) | W13.1 |
| E5 | #21 | §3.4 maps `TightMarking` to #14's tight stand-off (back-prop of E3) | W13.1 |
| E6 | #14/#21 | §7.2 vs #21 §3.5 stage conflict for `MarkTargetEntityId` | follow-up W13-F1 |
| E7 | #14 | D4 trap depth units | follow-up W13-F4 |

## 3. Retained-trace diagnosis (measurement only; precedes gameplay code)

**Question.** Does missing marking movement explain #543's formation and Duty sensitivity? Formation
cases (C1–C4, C9) and Duty cases (C5–C8) are analysed and reported separately; never pooled.

**Data actually retained** (`Program.cs.txt:114-162`, `PROTOCOL.md`, `trace-index.json`, `analyze.py`):

| Stream | Record | Content |
|---|---|---|
| `trajectory.bin` | 244 B/frame × 324,000 | 22 × (x, y) f32 **world** positions; ball pos/vel/spin/last-valid ×15 f32; ball state i32; possessing agent i32 |
| `team-possession.bin` | 8 B/frame | pass-in-flight receiver i32; team in possession i32 (carrier's team else receiver's team, −1) |
| `geometry.bin` | 188 B per 10 Hz sample (frame % 6 == 0, live) | frame i32; per team: #12 phase i32 + 11 slots (x, y) from `GetFormationSlot` — **canonical** frame, so away slots need `(105 − x, 68 − y)` |
| `ledger.bin` | per frame: count, then (ordinal u8, size i32, raw bytes) | every Tier-A event: `ShotExecutedEvent` (ShootingAgentId, TeamId, ContactFrame…), `GoalAwardedEvent` (Scorer = `_lastHolderAgentId`, `MatchEngine.cs:5842-5846`; ScoringTeam), `RestartAwardedEvent`, `OffsideCalledEvent`, `CardIssuedEvent`, `SubstitutionEvent`, `FoulCommittedEvent`, `PossessionChangedEvent` |
| `digests`/`aligned` | 32 B/frame | hashes only (raw snapshots not retained) |

**Not retained:** agent velocities (finite-difference only), #14 assignments, press roles, DT actions,
rotation state. So the diagnosis can test **exposure**, not **who was assigned**. Feasible without new
captures: yes. Required matches: control base C0 ×6, matrix head C1–C9 ×6, matrix base C5–C8 ×6 = 84
archives (base C1–C4, C9 reuse base C0, as `analyze.py` does). All 138 archives total 4.27 GB of LFS
(pointers only in this checkout; not fetched). Reader: `tools/dotnet-ci/retained_trace_store.py` (on #543).

**Frozen definitions** (commit as `docs/tracking/evidence/marking-diagnosis-<date>/PROTOCOL.md` before any decode):

- *Sample set S:* geometry frames (frame % 6 == 0, before full time). Positions read at the same frames.
- *Spell:* maximal run of samples with team-possession = A. A spell is **restart-origin** if a
  `RestartAwardedEvent` for A or a `GoalAwardedEvent` falls in (previous sample, first sample]; a restart
  awarded to A inside a spell starts a new spell. Primary metrics use **open-play-origin** spells only;
  restart-origin spells are reported separately. Spells cut by full time are flagged censored.
- *Final third of A:* world x ≥ 70.0 (A = 0) or x ≤ 35.0 (A = 1) [DERIVED 105/3].
- *Defenders:* active outfield agents of D = 1 − A (GK excluded; a GK-included variant reported).
  Agents are inactive from their dismissal record onward; samples after any dismissal are reported in a
  separate stratum. Substitution records are tallied (none expected: Human managers, `PROTOCOL.md`).
- *Attacker label:* roster index k (kickoff slot of the selected family; #543 maps roster index to slot
  index). #25 rotations can swap roles mid-match and are not recoverable; stated as a limitation.
- **M1** nearest-defender distance d_a = min_j |p_a − p_j| (m); per (case, arm, team, k): p10/p25/p50/p75/p90.
- **M2** exposure E_r(a) = share of samples in open-play spells with a in the final third and d_a > r,
  for r ∈ {3.0 (#14 `TackleEligibleRadiusM`, #23 `MARKING_RADIUS_M`), 6.0 (#8 `MOVE_PRESS_SUPPRESSION_DIST`),
  8.0 (#8 `PRESS_TRIGGER_DISTANCE`), 15.0 (#14 `ManMarkCandidateRadiusM`)}. Labels say "no defender within
  r m"; the word "unmarked" is not used.
- **M3** goal-side: with defending-goal centre G_D = (0, 34) or (105, 34), j is goal-side of a iff
  |p_j − G_D| < |p_a − G_D|. Report share of samples whose nearest defender is goal-side, and
  exposure to "no goal-side defender within r".
- **M4** slot counterpart: M1–M3 with both teams' #12 slots instead of positions.
- **M5** shots by shooter roster index, with d and goal-side status at the shot's ledger frame.
- **M6** goals: *linked* if the latest shot before the goal frame was by ScoringTeam with
  ShootingAgentId = Scorer and the witness never shows the conceding team in between; *own goal* if
  Scorer = −1 or team(Scorer) ≠ ScoringTeam; otherwise *uncertain* (deflection, rebound, shooter ≠
  scorer). Only linked goals are attributed to a slot.
- **M7** denominators: samples, spells, final-third entries per team; every share printed with its count.
- **M8** regain x: x of the ball at each open-play change of the witness to A (territory check for Duty).
- *Integrity gate:* decoded shots/goals/fouls per team must equal each `summary.json`; decode event
  layouts from the C# structs with a self-test, else stop.

**Comparisons.** Paired by seed: formation cases head vs reused base C0; Duty cases head vs their own
measured base. Report the changed team's attackers **and** the opponent's attackers (the changed team's
defensive shape changed too).

**Decision rules (frozen; descriptive, n = 6, not significance tests):**
- R1: for a formation case whose changed team scored more, the marking hypothesis is **refuted for that
  case** if the changed team's pooled E_8 rise (head − base) is ≤ 0 in ≥ 3 of 6 seeds.
- R2: if slot-based exposure (M4) rises but position-based exposure (M2) rises in < 3 of 6 seeds, base
  slots are compensated by existing movement: "missing marking movement" is refuted as the mechanism.
- R3: if attackers whose E_8 rose account for < 50% of the changed team's shot increase, marking is not
  the primary mechanism.
- R4 (Duty): if the Attack-Duty team's nearest-defender goal-side share does not fall and E_r does not
  rise in ≥ 4 of 6 seeds, the Duty result is not a marking phenomenon; report M8 as the alternative.
- Supporting result = none of R1–R4 fires. It justifies W13.1's priority; it does not predict its size.

**Budget:** 0 captures; ≤ 3.6 GB LFS fetch; ≤ 4 CPU-hours of Python; about one agent-day to write
the protocol, script and synthetic self-tests; one adversarial review of the protocol **before** decoding.
**Optional Phase B (owner decision OD-9):** if Phase A is inconclusive, rerun C0/C3/C4/C7 with a
read-only observer that adds per-sample `GetAssignment` mode/target to the #543 runner. These are new
captures on the frozen toolchain (~200 s per match at Debug, `summary.json` `elapsedSeconds`).

## 4. Design

### 4.1 Layer ownership (AI → Mechanics → Physics preserved; no new interface)

| Piece | Owner | File |
|---|---|---|
| Frame contract, assignment, Invariant 4, goal-side composition | #14 `defensive-ai` (pure static, canonical frame) | `MarkAssigner.cs`, `InvariantEnforcer.cs`, new `MarkTargetComposer.cs` |
| `TightMarking` → stand-off translation | #14 `TacticTranslation.cs` (consumer-side, KD-2 pattern) | existing file |
| Gate, mirroring to world, per-agent write | composition root | `MatchEngine.RunMechanicsAI` / `FillDefensiveSnapshot` |
| Carrier | #8 `TacticalContext` (`bool HasMarkTarget`, `Vector2 MarkTarget`, world m; zero value = identity) | `TacticalContext.cs` |
| Use | #8 `OptionGenerator.GenerateMoveCandidate` | existing |

The DT receives a position (a physical parameter), never a #14 type, so `decision-tree.asmdef` gains no
reference. Producer and consumer are both specified by E3/E4; there is a struct field and no interface.

### 4.2 Composition rule (per HOLD_SHAPE agent, each 10 Hz stride, canonical frame)

Inputs: assignment `a` from `GetAssignment(i)`, the team's `DefensiveSnapshot` (still valid after
`Tick`), stand-off `s`. Output: canonical target `M`, or no target (agent keeps today's slot behaviour).

1. No target if: switch off; `a.Mode == Zonal`; `a.ValidThroughTick != tick`; target index missing or
   `!IsActive`; the agent's team holds team possession (carrier or pass-in-flight receiver, as C1).
2. `CoverGkZone`: `M = a.TargetPosition` (abandoned-zone centre, (7.5, 34) canonical).
3. `ManMark` / `InterceptRunner`: O = target's **live** snapshot position (not `TargetPosition`; D2),
   G = (0, 34), v = G − O, L = |v|.
   `M = O + (s / L)·v` if L > s, else `M = G`.
4. Displacement cap: if |M − BaselineSlot| > `MaxMarkDisplacementM`, no target (the §3.10 Inv. 3 value).
5. World target = `MirrorPitchIfAway(t, M)`; #8 clamps to the pitch (`PitchGeometry.ClampToPitch`) and
   does **not** apply the `DefensiveLineDepth` adjustment to it.

Units: metres; s ∈ (0, 3.0) m (§6); L ∈ [0, 123.6] m; M is on the pitch after the clamp.

**Worked example A (away CB marks home F4231 ST; brief's case).** Home ST world (86.1, 34) → away
canonical O = (18.9, 34). Away CB1 slot (21.0, 25.84): distance 8.43 m ≤ 15 m, so a MAN_MARK candidate.
v = (−18.9, 0), L = 18.9, s = 2.0 → M = (16.9, 34.0). |M − slot| = √(4.1² + 8.16²) = 9.13 m ≤ 20.
World = (105 − 16.9, 68 − 34) = **(88.1, 34.0)**: between the ST (86.1) and the away goal (105).
**Mirror (home CB marks away F4231 ST):** O = world (18.9, 34) = canonical → M = **(16.9, 34.0)** world.
**Worked example B (off-centre, tight).** O = (20, 30): v = (−20, 4), L = 20.396; s = 2.0 →
M = (18.039, 30.392); s = 1.0 → M = (19.019, 30.196).
**Degenerate:** O = (1.0, 34.5), s = 2.0 → L = 1.118 ≤ s → M = (0, 34), then pitch clamp.

### 4.3 Invariant 4 — one marker per opponent (E3)

After Inv. 1–3 inside the same ≤3-pass loop (`InvariantEnforcer.cs:62-80`): for any opponent targeted by
more than one non-ZONAL assignment, keep the emergency override if present; otherwise keep the lowest
displacement cost |agent − O|² (§3.4); a tie keeps the lower EntityId (FR-DA-014). Demote the rest to ZONAL
(`DemoteToZonal`). Demotion is monotonic toward ZONAL, so the existing convergence argument holds.
It is gated by a per-tick `DefensiveSnapshot.MarkMovementActive` input (not serialized), so default
assignments, and therefore tackle intents, are unchanged.
*Example:* away CM1 (id 17) at (45, 28) and CM2 (18) at (45, 40) both MAN_MARK the home CAM at canonical
(36.75, 34) (10.2 m each). The costs tie at 104.06 m², so 17 keeps the CAM and 18 is demoted to ZONAL.
Note that two CBs on one striker are already reduced to one by Inv. 1 (≥3 of the back four ZONAL).

### 4.4 Exemptions and interactions

- **Pressers / GK / sent-off:** not in the pool (`HoldShapePoolFilter.cs:357-366`), so no target. Pressers
  keep today's behaviour; #13 press targets remain unconsumed (separate item).
- **Last man / cover:** emergency overrides are consumed as INTERCEPT_RUNNER (goal-side of the most
  advanced attacker) and COVER_GK_ZONE (zone centre). They require D1 fixed first.
- **In possession:** no targets (#14 emits all ZONAL on InPoss; the extra team-possession gate covers
  the hysteresis lag of #12's phase dwell, `PhaseClassifier.cs:30-45`). Loose-ball transitions keep marks (#14 §3.1).
- **Restarts:** the engine has no set-piece phase (§1.3), so marking applies as in open play. Set-piece
  marking is #14 §7.5 (Stage 2+). The diagnosis and measurement tag restart-origin spells.
- **Substitution:** the incoming player inherits the index and assignment (§1.3); a marker of a replaced
  opponent marks the substitute. Locked by test, not changed.
- **Offside line/trap:** a goal-side marker can drop behind the #12 line and play attackers onside. Trap
  ZONAL targets are **not** consumed (ZONAL ⇒ slot) until D4 is fixed (W13-F4); the trap still demotes
  DEFENSE-line marks. Offside calls are measured descriptively.
- **#12 rotations / Duty:** these move the zonal slot and therefore `BaselineSlot` (displacement cap and
  pool positions). The mark point ignores the marker's Duty. Attack-Duty defenders still drop goal-side.
- **PRESS (D5):** near the mark point, MOVE urgency falls to its floor and PRESS on the nearest opponent
  (≤ 8 m) usually wins, sprinting to the man's position. Goal-side therefore holds for the composed MOVE
  target, not necessarily for the body. This is measured; mark-aware PRESS is follow-up W13-F3.
- **INTERCEPT:** unchanged (E3 defers #8 §3.1.9 consumption by name).
- **#23 dismarking:** default Off. When on, a marked attacker (opponent ≤ 3 m) nudges away
  (`SlotComposer.cs:168-194`); the stand-off < 3 m makes markers visible to it. This is characterized, not tuned.
- **#21 dials:** `MarkingOrientation` (radius ×0.8/1.0/1.2) becomes behavioural; `TightMarking` selects
  the tight stand-off; `MarkTargetEntityId` is out of scope (W13-F1).
- **#440 (PR #544, other owner):** markers near their man raise tackle and `BALL_LOOSE` exposure, and #440
  changes who re-collects. No code interaction; the order of measurement matters (§11).

## 5. Marking varieties

| Variety | In code today | First landing (W13.1) | Follow-up | #21 dial |
|---|---|---|---|---|
| Zonal (slot) | live (all movement) | unchanged | — | — |
| Mixed/hybrid: zonal back line ≥3, ≤4 man-marks, radius 15 m | assignment only | **moves** (goal-side) | — | `MarkingOrientation` |
| Ball-oriented / man-oriented radius | radius scalar live in assignment | moves | raising Inv. 2 cap for ManOriented = owner decision | `MarkingOrientation` |
| One marker per opponent | absent | Invariant 4 | — | — |
| Tight vs loose | absent | stand-off 1.0 / 2.0 m | team-level tightness; `Marking` attribute (no consumer) | `TightMarking` (per player) |
| Runner tracking | INTERCEPT_RUNNER assignment | moves goal-side of runner (live position) | pace tier for marks; predicted point | — |
| Last-man / cover GK zone | assignment only | moves | — | — |
| Specific opponent | serialized, unconsumed | — | W13-F1 (E6) | `MarkTargetEntityId` |
| Coverage of an unassigned between-lines attacker | absent | — | W13-F2 attacker-centric pickup, conditional on §10 | — |
| Mark-aware press | absent | — | W13-F3 | — |
| Offside-trap step-up | ZONAL target, unread | — | W13-F4 after D4 | `OffsideTrap` |
| Set-piece marking | absent | — | #14 §7.5 Stage 2+ | — |

## 6. Constants

New (KD-W1 allows new dials on a dead surface; **UN-CALIBRATED**, no tuning in this landing):

| Name | Tag | Catalogue | Units | Value | Valid range / invariant |
|---|---|---|---|---|---|
| `MarkStandoffM` | [GT] | `DefensiveAIConstants` (config `defensive-ai`) | m | 2.0 | `MarkStandoffTightM` < v < min(`TackleEligibleRadiusM`, `MARKING_RADIUS_M`) = 3.0 |
| `MarkStandoffTightM` | [GT] | `DefensiveAIConstants` | m | 1.0 | 0 < v < `MarkStandoffM` |
| `MarkMovementEnabled` | [GT] | `MatchEngineConstants` (config `match-engine`, `Config.GetBool`) | bool | false | {false, true}; flipping it is OD-2 |
| own-goal centre (0, 34) canonical | [DERIVED] | `DefensiveAIConstants` | m | (0, `PITCH_WIDTH_M`/2) | from [CROSS #1 §1.2]; matches #14 §3.5.2 |

The upper bound keeps a marker inside the tackle-intent radius and inside #23's "marked" radius. It is
locked by a test. Names follow the catalogue's existing PascalCase `[GT]` convention.

Newly live (values unchanged): `ManMarkCandidateRadiusM` 15, `RunnerVelocityThresholdMS` 3.0,
`MarkDwellTicks` 4, `MinBacklineAgents` 3, `MaxManMarkAssignments` 4, `MaxMarkDisplacementM` 20,
`LastManBallBufferM` 5, `LastManOwnHalfMinX` 5, `GkExpectedZoneMaxX` 15, `CoverGkZoneMaxTicks` 20
(all [GT], `DefensiveAIConstants.cs`), `MarkingOrientationScalar` {0.80, 1.00, 1.20} ([GT], #21).
`TackleEligibleRadiusM` and `MARKING_RADIUS_M` are read as [CROSS] bounds.

## 7. Determinism and snapshot

- **No new cross-tick state.** The composer is a pure function of this stride's assignment and
  snapshot. The assignment and hysteresis per entity are already serialized (`MatchEngine.cs:8044-8073`).
  `_tacticalContexts` is rebuilt every stride and not serialized (grep). `MarkMovementActive` is
  per-tick input. Therefore no `SNAPSHOT_SCHEMA_VERSION` change, and no v29 collision with #543 on
  either base. Raw-digest identity at default is achievable.
- **Activation is config-level.** It is a [GT] like `TackleContactRadiusM` (`MatchEngineConstants.cs:732`),
  with an internal test seam like `TestOnly_ArmTackleChallenge` (`MatchEngine.cs:2730`). A per-match
  flag in the `EnableGkHeading` style is serialized (`MatchEngine.cs:7183,7492`) and would force a
  bump: 28 → 29 collides with #543, so it would have to land after #543 as v30. That route is not
  recommended (OD-11). Hazard: a save restored under a different config diverges, as for any [GT].
- **No RNG**, no domain tag, no draw-site or draw-order change. Iteration order is the EntityId-ascending
  pool and the k-ascending orchestrator loop.
- **Rates:** composition runs once per 10 Hz stride inside `RunMechanicsAI`; MOVE commands persist across
  the 60 Hz frames as today. Nothing is added to the physics loop.
- **Hot path:** structs, `in`/`out`, no LINQ, no allocation. `SnapshotIndexOf` is O(22). An allocation
  test extends `Performance_T_DA_PERF_002`.
- If OD-11 picks the serialized flag, follow `.claude/skills/snapshot-schema-bump/SKILL.md`: version
  pin, symmetric write/read, single-field digest probe, round-trip test.

## 8. Rollout, base branch and default behaviour

**Base: main for both code landings** (recommended; OD-3). Neither W13.0 nor W13.1 needs a non-default
formation to be correct. Their unit tests and mirrored F442 engine tests run on main. #543's
`MatchEngine.cs` hunks (around lines 319, 1043, 1552, 2305, 4425-4469, 6911-7057, 7347-7431 and
7977-7995) do not overlap W13's (RunMechanicsAI 3760-3930, FillDefensiveSnapshot 4621-4704, test
seams). No schema change, so rebasing either way is mechanical. F4231 engine tests and the §10 corpus
need #543: run them after #543 merges, or on a throwaway integration branch main + #543 head (never landed).
Basing on #543 instead would put formation tests in-tree immediately, but stacks review churn on an
unmerged draft.

| Option | Identity | Pros | Cons |
|---|---|---|---|
| (a) live by default | every baseline moves (`pre-c-control-20261104/expected.json`, scenario predicates) | simplest; no switch | mixes wiring with a behaviour change; one-shot adoption; must avoid open measurement windows as D-05 does for #434/#440 |
| **(b) off by default (recommended)** | exact raw-digest identity at default, gameplay and serialized state, on main or #543 | follows the "exact identity at default, characterization for non-default" pattern (#434) and W2's "built, shipping disabled" | one config constant to flip later; a dead path until flipped |

The default flip is a separate measured owner decision after §10. It must not land inside an open
measurement window (the D-05 rule, `match-engine-wiring-backlog.md:321-329`). Whether a W8 B→C window is
open at landing time could not be determined from main and must be checked then.
**W13.0 is behaviour-changing at default by nature** (a defect fix in the live tackle chain). It is a
separate PR with its own six-seed characterization and digest adoption (OD-1).
**Player-facing formation/Duty controls** stay blocked until the flip is accepted.

## 9. Implementation steps

Each step is one reviewable PR or commit. Tests run first with `run-gate.sh --fast --test-filter`, then
the full Linux gate, then the pinned Unity host (src/CLAUDE.md). Every engine test runs as
`[TestCase(0)] [TestCase(1)]` (home/away mirror).

**S0 — Diagnosis** (no `src/`). Add `docs/tracking/evidence/marking-diagnosis-<date>/` with
`PROTOCOL.md` (frozen first), `analyze_marking.py` and synthetic self-tests (mirror, goal-side, own goal,
uncertain link, slot mirroring). Then run and report. Gate: integrity totals match the `summary.json` files.

**S1 — W13.0 (E1, E2), main.**
- `DefensiveSnapshot.cs`: document the canonical frame. `LastManDetector.cs`, `MarkAssigner.cs`
  (`ThreatScore` takes `defendsX0`), `TackleIntentEvaluator.cs`, `OffsideTrapController.cs` and
  `InvariantEnforcer.cs` (`TargetThreat`): resolve `defendsX0` from the snapshot frame, never `teamId`.
- `MarkAssigner.cs`: refresh `TargetPosition` from the live snapshot for retained non-ZONAL assignments.
- #14 `section-2.md` §2.3, `section-3.md` §3.3/§3.5/§3.8 notes, version rows; ERR rows.
- Tests (`DefensiveAITests.cs`): team-1 canonical twins of `T_DA_016`, `T_DA_035`, `T_DA_038`, `T_DA_041`
  and `T_DA_021`/`026` (identical canonical inputs ⇒ identical outputs for team 0 and team 1). Add a
  retained-assignment refresh test. `MatchEngineAwayTeamTests.cs`: a mirrored kickoff produces mirrored
  #14 assignments (via a new `TestOnly_MarkAssignment(agentId)`).
- Six-seed characterization on main (frozen toolchain lane): tackles, fouls, shots and goals per team,
  descriptive. Adopt the new digests by owner decision.

**S2 — Spec text (E3, E4, E5)**, authored and owner-reviewed before S3–S5. It lands in the same PR as the code.
#14 new §3.14 (formula, worked examples A/B/degenerate, ranges), §3.10 Invariant 4 plus a worked example,
§4.4.4/§4.5.1 rewrite, §6.1 constants. #8 §2.2.6 and §3.1.7.2. #21 §3.4 `TightMarking` row.

**S3 — `defensive-ai`.**
- `DefensiveAIConstants.cs`: + `MarkStandoffM`, `MarkStandoffTightM`, goal centre.
- `TacticTranslation.cs`: + `MarkStandoffM(bool tight)`.
- New `MarkTargetComposer.cs`: `static bool TryCompose(in MarkAssignment, DefensiveSnapshot, int tick, float s, out Vector2)`.
- `DefensiveSnapshot.cs`: + `MarkMovementActive`. `InvariantEnforcer.cs`: Invariant 4 when active,
  plus a final-check term.
- Tests: worked examples to 1e-3; degenerate case; cap fallback; stale tick; inactive target;
  COVER_GK_ZONE passthrough; stand-off invariant; Invariant 4 example (17 keeps, 18 demoted); gated-off
  leaves every existing test unchanged; extend `Determinism_T_DA_DET_006` to four invariants;
  zero-allocation.

**S4 — `decision-tree`.**
`TacticalContext.cs`: + `HasMarkTarget` and `MarkTarget`; `Stage0Default` seeds false.
`OptionGenerator.cs`: MOVE target = clamp(MarkTarget) when set, else unchanged.
Tests (`OptionGeneratorTests.cs`): unset ⇒ target equals the adjusted slot bit-for-bit; set ⇒ target and
`DistanceToSlot` use MarkTarget; the depth adjustment is not applied.

**S5 — `match-engine`.**
- `MatchEngineConstants.cs`: + `MarkMovementEnabled`.
- `MatchEngine.cs`: `FillDefensiveSnapshot` sets `MarkMovementActive`; `RunMechanicsAI` composes and
  mirrors per agent after `_defensive[t].Tick`; add `TestOnly_ArmMarkMovement(bool)` and
  `TestOnly_MarkTarget(agentId)`.
- New fixture `MatchEngineMarkingTests.cs`, extending the `MatchEngineMechanicsTests.cs` patterns
  (`TestOnly_SetAgent`):
  1. **Acquisition:** place the defending back four at slots and an attacker 8.43 m from both CBs and
     2.1 m deeper (§1.4); the other team has possession ≥ 25 m away. Within `MarkDwellTicks` + 1 = 5
     strides exactly one CB holds MAN_MARK on him, and `TestOnly_MarkTarget` equals worked example A
     (mirrored for team 0).
  2. **Closure:** the marker reaches ≤ `TackleEligibleRadiusM` (3.0 m) of the attacker within N strides.
     Derive N in the test from #2's jog/walk tier speeds as ceil((d_jog/v_jog + d_walk/v_walk + 0.5 s) × 10),
     where d = 9.13 m split at the 6 m jog threshold. Record the goal-side flag; do not assert it
     physically (PRESS, §4.4).
  3. **Invariants hold every stride:** ≥3 DEFENSE ZONAL, ≤4 MAN_MARK, no duplicate targets, consumed
     target ≤ 20 m from slot.
  4. **No targets** for GK, pressers, the possessing team, a sent-off agent, or with the switch off.
  5. **Substitution and red card** during a mark: deterministic continuation; no target onto an
     inactive agent.
  6. **Snapshot/restore continuation:** armed, mid-dwell (non-zero hysteresis); restore into a fresh
     armed engine; 600 frames give an identical digest chain (extend `MatchEngineSnapshotRestoreTests.cs`).
  7. **Two-run determinism**, armed, 2 × 10 min (extend `MatchEngineDeterminismTests.cs`).
  8. **Identity off:** `MatchEngineSnapshotDigestReferenceTests.cs` pins are unchanged; six-seed terminal
     digests are unchanged versus the base commit (frozen lane).

**S6 — after #543 merges:** in `MatchEngineMarkingTests.cs`, add home F4231 vs away F442 (and mirror) from
kickoff: the ST acquires a marker in the first defensive spell. Also check `MatchEngineFormationDutyTests.cs`
still passes with the switch armed.

**S7 — §10 measurement, then tracking close-out (§12).** The default flip is its own one-line PR.

## 10. Post-wiring measurement

**Arms:** (i) W13 branch, switch off — identity proof: C0 ×6 full plus #543's probe pattern, exact digests
versus the base. (ii) Switch armed — C0–C9 ×6 = 60 full matches. "Before" reuses the retained #543 head
traces when (i) proves identity; otherwise rerun.
**Instrument:** #543's read-only runner plus one stream `marking.bin` per 10 Hz sample: per agent mode
(u8), target id (i8), `HasMarkTarget` (u8) and composed target (2 × f32). Arm through the config key bound
before the first engine construction, declared in the protocol.
**Metrics:** §3's M1–M8 with identical definitions (before/after comparable), plus assignment census
(MAN_MARK/INTERCEPT shares, attackers with no marker while in the final third), tackle intents and
resolutions, fouls, offsides per team, and the case-level sensitivity Δgoals and Δshots versus C0.
**Hard stops:** NaN; any invariant breach above; a target on a GK, a presser, the possessing team or an
inactive agent; any switch-off identity mismatch; determinism or restore failure; integrity totals.
**Descriptive only:** every football metric. No realism bands are approved; the owner judges whether the
formation/Duty sensitivity shrinks. **Budget:** 66 full captures × ~200 s Debug ≈ 3.7 CPU-h (two
concurrent ≈ 2 h wall), ~2.8 GB LFS, an analysis rerun of §3's script, one review.

## 11. Risks and open owner decisions

Risks:
- PRESS dominates the final approach, so goal-side is nominal (§4.4). Measured; W13-F3.
- The radius is defender-centric: an F4231 CAM 17.7 m from all slots may stay unassigned, so the
  structural advantage can persist. Measured; W13-F2.
- Inv. 1 leaves one back-line marker against two strikers by design.
- Goal-side markers lower the offside line.
- Runner tracking lags because the walk tier applies below 6 m.
- W13.0 moves default baselines and the away tackle rate, which shifts foul/card exposure under KD-W1 —
  measure, do not tune.
- Scenario predicates (`sim_match_engine_close_chance`, `inposs_gate`, shot-outcome reachability) may
  move at the flip, so run the scenario suite before OD-2.
- #440's re-pick change interacts with tackle-generated loose balls; keep #440's and W13's measurement
  windows separate.
- **Testable on main alone:** D1/D2 unit and mirror tests, the composer, Invariant 4, the DT override,
  F442 engine acquisition/closure/restore/determinism, switch-off identity, and a C0 assignment census.

| ID | Decision | Recommendation |
|---|---|---|
| OD-1 | Land D1/D2 as separate W13.0 (changes default) vs gate them | separate, first; adopt digests after characterization |
| OD-2 | Default (a) live vs (b) off | (b); flip after §10 |
| OD-3 | Base main vs #543 | main; formation tests and corpus after #543 merges |
| OD-4 | Invariant 4 in first landing | yes (gated) |
| OD-5 | Stand-off 2.0 / 1.0 m as un-calibrated [GT] | accept per KD-W1 (W2 precedent) |
| OD-6 | `MarkTargetEntityId` | follow-up W13-F1 with E6 |
| OD-7 | Attacker-centric coverage pass | only if §10 shows residual exposure |
| OD-8 | Mark-aware PRESS | only if §10 shows goal-side not held |
| OD-9 | Phase B instrumented re-run | only if Phase A is inconclusive |
| OD-10 | Approve §3 decision rules R1–R4 before decoding | approve as written or amend before data |
| OD-11 | Config-level [GT] vs serialized per-match flag | config-level (no schema bump) |

## 12. Tracking-document changes for the landing

- `match-engine-wiring-backlog.md`: new Class-A item **W13 — defender marking never moves anyone**
  (cross-reference C10), §5 sequence row, version row. W13-F1…F4 are listed as follow-ups. Re-check the
  W number at landing.
- `spec-error-log.md`: E1–E5 rows (E6/E7 when filed), version header chain.
- `open-issues.md`: W13 entry; formation/Duty player controls blocked until OD-2 is accepted; the
  #543 diagnosis result.
- `file-manifest.md` (and `src-tree.md`): `MarkTargetComposer.cs`, `MatchEngineMarkingTests.cs`, the
  evidence directories, this plan.
- `CHANGELOG.md` and `CHANGELOG-src.md` header chains; `src/CLAUDE.md` bump per the
  `landing-close-out` skill.
- `data-contract-index.md`: the `TacticalContext` row (per-agent mark target carrier) and the
  `DefensiveSnapshot` row (canonical frame, `MarkMovementActive`).
- Spec version rows in #14 (`section-2/3/4/6.md`), #8 (`section-2-1-to-2-2.md`, `section-3-1.md`) and
  #21 (`section-3.md`); `SPEC_INDEX.md` last-updated note only.
- `path-to-playable-roadmap.md`: PM-2 note that formation/Duty controls wait on W13.

## Version history

| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-11-10 | — | Initial draft: verified chain at `d4b026aa`, defects D1–D5, retained-trace diagnosis design, composition rule with worked examples, Invariant 4, constants, rollout (base main, off by default), steps, measurement, decisions. Nothing compiled, run or measured. |
