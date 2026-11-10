# W13 — Defender marking wiring plan

> **Created:** November 10, 2026 · **Version:** 0.5 · **Status:** APPROVED FOR IMPLEMENTATION — the owner accepted all §11 recommendations (OD-1…OD-13) on November 10, 2026 (agent-attested record of the owner's answer in the working session, not a signed repository receipt)
> **Purpose:** Make Defensive AI #14's mark assignment move defenders. Separately, give a
> retained-trace diagnosis (for PR #543's merge decision) of whether its absence explains the
> formation/Duty scoring sensitivity.
> **Base facts:** main `d4b026aa`. Draft PR #543 is `origin/codex/434-formation-duty` (unmerged).
> Work lands as ordered commits on `claude/great-sagan-rpcmhp`; no PR unless the owner asks.
> **Toolchain:** SDK 8.0.133 / runtime 8.0.33 is available here for compile/test gates only
> (`bash tools/dotnet-ci/run-gate.sh --fast --test-filter <f>`). The frozen lane (SDK 8.0.426 / CLR 8.0.32,
> six-seed captures) exists only in the #543 owner's environment. Nothing in this plan was compiled,
> run or measured.

## 0. Recommendation in brief

1. **W13.0 (first commits): two live #14 defects, spec and code together.**
   - D1: #14 resolves "own goal" from the team id, but the engine supplies the defending team's
     canonical frame, so the away team uses the wrong goal.
   - D2: retained assignments keep a stale target position.
   - This is behaviour-changing at default and is characterized like any defect fix.
2. **W13.1: wiring, switched off by default.**
   - A pure #14 `MarkTargetComposer` turns a non-ZONAL assignment into a point 2 m goal-side of the
     marked man (1 m with `TightMarking`).
   - The orchestrator mirrors that point into a per-agent `TacticalContext` carrier, and #8
     `MOVE_TO_POSITION` targets it.
   - **A marking agent does not take PRESS priority 2**, the "nearest opponent within 8 m" option
     (§4.5): without this rule, every marker would sprint onto his man from about 6 m.
   - Anti-chaos Invariant 4 allows one marker per opponent.
   - The gate is a config-level `[GT]` bool on the existing `Config.GetBool` pattern.
   - There is no new state, no schema change and no RNG. Identity at default holds relative to the
     post-W13.0 tree.
3. **Diagnosis (§3) does not block code.** It gates #543's merge and sets §10's expectations; it is a
   handoff to the #543 owner's agent. **Default flip** and **per-team scheme selection (W13.2)** are
   separate owner decisions (§8).

## 1. Verified current state (main `d4b026aa`)

### 1.1 Chain, link by link

| # | Link | Evidence | State |
|---|---|---|---|
| L1 | #12 slot: canonical → world → `TacticalContext.Stage0Default(worldSlot)` | `MatchEngine.cs:3821-3839`, `:4781-4787` | live |
| L2 | #13 role → #14 pool exclusion (also GK, inactive) | `MatchEngine.cs:4697`; `HoldShapePoolFilter.cs:357-366` | live |
| L3 | #14 snapshot: 22 agents in the defending team's **canonical** frame, `DefensiveTeamId = team` | `MatchEngine.cs:4613-4703` (`:4629,4689,4694-4696`) | live |
| L4 | #14 tick: InPoss ⇒ all ZONAL; pool; last-man/COVER_GK_ZONE; assign; tackle intent; trap; invariants | `DefensiveAITick.cs:145-150,153,178-238,247,250,258,263` | live |
| L5 | Assignment: 15 m × `MarkingOrientation` scalar; threat = x-proximity × FirstTouch; priority INTERCEPT_RUNNER > MAN_MARK > ZONAL | `MarkAssigner.cs:115-117,119-183,203-218,228-252` | live |
| L6 | Hysteresis: 4-tick dwell, 4-tick lock | `MarkHysteresis.cs:27-90`; `DefensiveAIConstants.cs:169` | live |
| L7 | Orchestrator reads only `OffensiveLineDepth` → `ctx.DefensiveLineDepth`; sets `HasMarkDirective = !teamHasPossession` | `MatchEngine.cs:3801,3870-3871` | live / stub |
| L8 | `HasMarkDirective` reader | `TestOnly_HasMarkDirective` only (`MatchEngine.cs:3034`); stub `TacticalContext.cs:141-142` | **none** |
| L9 | Per-agent `GetAssignment` → orchestrator | `DefensiveAITick.cs:309-319`; no production caller | **missing** |
| L10 | #8 MOVE target = depth-adjusted own slot | `OptionGenerator.cs:725-749`; `TacticalContext.cs:196-201` | live |
| L11 | #8 PRESS: carrier ≤ 8 m, else nearest opponent ≤ 8 m | `OptionGenerator.cs:753-818`; `UtilityWeights.cs:332` | live |
| L12 | #8 INTERCEPT: ball path only | `OptionGenerator.cs:822-902` | live |
| L13 | Dispatch: MOVE sprint ≥ 15 m / jog ≥ 6 m / walk; PRESS → `PressSprint` | `ActionDispatcher.cs:221-256`; `TacticalWeights.cs:53-54` | live |
| L14 | Tackle intent only vs the assigned opponent ≤ 3 m | `TackleIntentEvaluator.cs:49,64` | live |
| L15 | Tackle resolution | `MatchEngine.cs:3628,4104,4289` | live |
| L16 | Directive trap/emergency fields, `MarkAssignment.TargetPosition` | no reader outside `defensive-ai` except the serializer (`MatchEngine.cs:8044-8073`) | **none** |

Assignments reach gameplay only through tackle intents (L14). The trap also demotes DEFENSE-line marks
to ZONAL (`OffsideTrapController.cs:85-136`), which removes their intents. `InvariantEnforcer`
(Inv. 1 ≥ 3 back-line ZONAL, `:93-151`; Inv. 2 ≤ 4 MAN_MARK, INTERCEPT_RUNNER uncounted, `:156-197`;
Inv. 3 target ≤ 20 m from slot, `:202-233`), `LastManDetector` (`:65-182`) and the trap (coherence
8 m) constrain assignments only. **None constrains anything that moves today.** In the wiring backlog,
C10 (`match-engine-wiring-backlog.md:627-632`) notes that `MarkAssigner` is ball-blind; no item covers
movement. The highest existing item is W12, so this is **W13**.

### 1.2 Defects and facts found while verifying

- **D1 (live, home/away asymmetric).**
  - `LastManDetector.DefendsX0(teamId) => teamId == 0` (`:190`) feeds `MarkAssigner.cs:41,205`,
    `LastManDetector.cs:81`, `OffsideTrapController.cs:39` and `TackleIntentEvaluator.cs:44`.
  - The snapshot is canonical (own goal x = 0 for both teams), yet `DefensiveSnapshot.cs:24` says world space.
  - For the away team, #14 therefore inverts threat, runner direction, last man, coverage depth and the
    trap's half test. No #14 test uses team 1 (`DefensiveAITests.cs:40`).
  - The engine does not swap ends (`tactical-instructions/section-3.md:153`).
  - Effect size is unmeasured.
- **D2.** A retained non-ZONAL assignment keeps its commit-time `TargetPosition` (`MarkHysteresis.cs:39`
  dwell lock; `:58` same-target path), against #14 §2.2.2 (`section-2.md:107-108`). Inv. 3 tests that
  stale point.
- **D3.** There is no exclusivity (#14 §3.3.3, `section-3.md:165-247`). With identical attributes,
  every defender in range picks the deepest attacker.
- **D4.** The trap treats normalized depth 0.5 (`MatchEngine.cs:4642-4644`) as metres, giving a
  step-up target 3.5 m from goal. Its targets are not consumed by W13.1.
- **D5 — de facto marking by PRESS today.**
  - Any off-ball agent presses the nearest opponent within 8 m (`OptionGenerator.cs:789-803`) once he
    is within about 6.2 m (§4.5).
  - At mirrored F442 anchors, a CB is 3.44 m from a striker, so he presses him. The F4231 striker is
    8.43 m from both CBs, which is outside 8 m.
  - Possession is carrier-less on most frames: #543 C3 seed 1 has 31,668 controlled-ball frames of
    323,999 live, while the team-possession witness covers 299,475.
- **Restarts and substitutes.**
  - Restarts are instantaneous: the taker is given possession at once (`MatchEngine.cs:5923-5955`).
  - `MatchPhase` is always `OPEN_PLAY` (`:6463`).
  - Substitutes reuse the agent index (`:1845`) and inherit #14 per-entity state.

**Geometry** (anchors × 105 / × 68, `PositioningAIConstants.cs:300-393`; world, home attacking +X):

| Claim | Arithmetic |
|---|---|
| F442 ST to CB 3.44 m | (81.9, 28.56) to (84.0, 25.84) |
| F442 CMs coincide | home CM1 = away CM2 = (52.5, 25.84) |
| F4231 ST 8.43 m from each CB, 2.1 m beyond them | (86.1, 34) to (84.0, 25.84 / 42.16) |
| F4231 CAM 17.7 m from any opposing slot | (68.25, 34) to (52.5, 42.16) and (84.0, 25.84) |
| Attack Duty | +3 m anchor x on #543 `SlotComposer`. Strikers move to x = 84.9, 0.9 m beyond the CB line and 2.85 m away |

#543 goals, six matches, base → head, home / away:

| Case | Home | Away |
|---|---|---|
| C3 home F4231 | 20 → 71 | 27 → 15 |
| C4 away F4231 | 20 → 17 | 27 → 39 |
| C1 home F433 | 20 → 53 | — |
| C2 away F433 | — | 27 → 64 |
| C5 home Defend | 24 → 5 | — |
| C7 home Attack | 34 → 68 | — |
| C8 away Attack | — | 26 → 57 |

Source: #543 `analysis/per-match-team.csv`.

## 2. Spec position and ERR plan

| Spec | Says | Gap |
|---|---|---|
| #14 §4.4.4 (`section-4.md:158-180`) | team `MarkDirective` written to #8; #8 "adjusts target via MarkAssignment" in MOVE and INTERCEPT | a team struct cannot carry a per-agent target; no formula |
| #14 §4.5.1 (`:184-198`) | orchestrator composes per-agent target from `GetAssignments()`; pressers keep the #13 target | no rule; #13 targets unconsumed |
| #14 §2.2.2, §3.3, §3.10, §3.5.2 | current-position target; assignment; three invariants; `ownGoalCenter = defendsX0 ? (0,34) : (105,34)` | D2; D3; no stand-off or goal-side; frame of `defendsX0` unstated (D1) |
| #14 §7.2 vs #21 §3.5 | manual man-mark is Stage 2+, vs an approved request (FR-TI-023) | stage conflict |
| #8 §2.2.6 (`section-2-1-to-2-2.md:745-753`), §3.1.7.2 (`section-3-1.md:873-897`), §3.1.8.2, §3.1.9 | nullable team `MarkDirective?`; RUNNER override precedent; press target selection | no per-agent target; no MARK override; press unaware of marks |
| #21 §2.2.2/§3.4 | `TightMarking` is a "tighter mark distance"; `MarkingOrientation` is a radius scalar | #14 has no mark distance |
| #23 | marked = opponent ≤ `MARKING_RADIUS_M` 3.0 (`PositioningAIConstants.cs:231`) | reused as a bound |

**ERR rows.**
- Allocate each id with `.claude/skills/err-file-and-backprop/scripts/next_err_id.sh <spec>` in the
  commit that files it, and re-run after every rebase.
- At `d4b026aa` the script reports ERR-014-007 / ERR-008-026 / ERR-021-009 as next free. **#543 already
  uses ERR-021-009, ERR-012-013 and ERR-025-001.** Nothing here is a reservation.
- **ERR-014-007 is intentionally unused (W13.0 landing, November 10, 2026).** This note named it as the
  next free #14 id; `next_err_id.sh` greps `docs/` and so counted this very sentence as a use, and W13.0
  filed #14 as **ERR-014-008** (D1) and **ERR-014-009** (D2). The gap is left, not renumbered, and
  `spec-error-log.md` carries a tombstone row. Do not reuse it.
- ERR-014-001 is cited in #8 §2.2.6 but has no index row found by grep.

| Entry | Spec | Content | Commit |
|---|---|---|---|
| E1 | #14 | D1: the snapshot frame is canonical; `defendsX0` comes from the frame, not the team id | W13.0 |
| E2 | #14 | D2: refresh `TargetPosition` of retained non-ZONAL assignments to the live position | W13.0 |
| E3 | #14 | §3.14 composition, §3.10 Invariant 4, §4.4.4/§4.5.1 per-agent carrier, stand-off constants; INTERCEPT consumption deferred | W13.1 |
| E4 | #8 | §2.2.6 per-agent `MarkTarget`; §3.1.7.2 MARK override; §3.1.8.2 marking agents skip priority 2 | W13.1 |
| E5 | #21 | §3.4 maps `TightMarking` to the tight stand-off | W13.1 |
| E6 / E7 | #14 (+#21) | `MarkTargetEntityId` stage conflict / D4 trap units | follow-ups |

## 3. Diagnosis — self-contained handoff (executor: the #543 owner's agent; does not block W13 code)

**Purpose.** Decide whether #543's sensitivity is consistent with missing marking movement. This is an
input to #543's merge and to §10's expectations. The owner has already decided marking is wired either
way. **D1 confound:** every #543 trace ran with the away team's #14 inverted. That bias reaches play
only through away-defender tackle intents, the trap and last-man choices, but it is not negligible by
assumption.

**Order.** The diagnosis runs on existing traces now, in parallel with W13.0, which changes no
retained trace. Any conclusion resting on a home-versus-away comparison waits for a post-W13.0 recapture
of C0/C3/C4 (frozen lane), per rule R5.

**Steps (verbatim):**
1. Run `git worktree add ../w13diag origin/codex/434-formation-duty`, then
   `git lfs pull --include "docs/tracking/evidence/formation-duty-20261109/retained/**/observed/traces.tar.xz"`.
   This covers 84 needed archives (control base C0 ×6; matrix head C1–C9 ×6; matrix base C5–C8 ×6;
   base C1–C4/C9 reuse base C0, as `analyze.py` does), about 3.6 GB.
2. Commit `docs/tracking/evidence/marking-diagnosis-20261110/PROTOCOL.md` containing this section's
   definitions and rules, unchanged or with owner amendments, **before decoding anything**.
3. Write `analyze_marking.py` in the same directory. Read streams via
   `tools/dotnet-ci/retained_trace_store.py` `open_trace(dir, name)`, exactly as `analyze.py:8-11`.
   - `trajectory.bin`: 244 B/frame — 22 × (x, y) f32 **world**; ball 15 × f32; state i32; possessor i32.
   - `team-possession.bin`: 8 B/frame — receiver i32, team i32 (−1 none).
   - `geometry.bin`: 188 B per sample — frame i32; per team: phase i32 + 11 slot (x, y), **canonical**,
     so away slots map to (105 − x, 68 − y).
   - `ledger.bin`: per frame — count; then (ordinal u8, size i32, raw struct bytes).
   - Source: `Program.cs.txt:114-162`.
4. Get event field offsets for `ShotExecutedEvent`, `GoalAwardedEvent`, `RestartAwardedEvent`,
   `CardIssuedEvent` and `SubstitutionEvent` from a one-file C# probe under the frozen toolchain
   (`Marshal.OffsetOf`), plus ordinals via `EventRegistry.GetOrdinal<T>()`. Gate: decoded shots,
   goals and fouls per team must equal every `summary.json`, otherwise stop.
5. Write outputs: `per-stratum.csv`, `per-attacker.csv`, `shots.csv`, `goals.csv`, `verdicts.json` and
   `README.md`. Every share is printed with its numerator and denominator. Run synthetic self-tests
   first (mirror symmetry, slot mirroring, goal-side, own goal, uncertain link).

**Frozen definitions.**
- *Samples S.* Geometry frames (frame % 6 == 0, before full time), with positions read at the same
  frames (10 Hz).
- *Spell.* A maximal run of samples where team-possession = A.
  - A spell is **restart-origin** if a `RestartAwardedEvent` awarded to A, or any `GoalAwardedEvent`,
    lies in (previous sample, first sample]. A restart to A inside a spell starts a new spell.
  - Primary metrics use **open-play-origin** spells; restart-origin spells are reported separately.
  - Spells cut by full time are flagged censored.
- *Final third of A.* Home: x ≥ 70.0; away: x ≤ 35.0 [DERIVED 105/3].
- *Defenders.* Active outfield agents of D = 1 − A; a GK-included variant is also reported.
  - An agent is inactive from a `CardIssuedEvent` with `CardKind` 1 or 2 onward
    (`CardIssuedEvent.cs:38-40`; dismissals at `MatchEngine.cs:6323,6329`). Samples after any dismissal
    form their own stratum.
  - Substitutions (`SubstitutionEvent.cs:38-44`) are tallied; none are expected (Human managers).
- *Attacker label.* Roster index k, the kickoff slot under #543's index mapping. #25 rotations are not
  recoverable; this is a stated limitation.
- **Stratification (mandatory).** Every metric is reported separately for **home defending** (away
  attacks; #14 correct) and **away defending** (home attacks; #14 affected by D1).
- **M1** — d_a = min_j |p_a − p_j| (m); p10/p25/p50/p75/p90 per (case, arm, stratum, k).
- **M2** — E_r(a): the share of open-play samples with a in the final third and d_a > r, for
  r = 3.0 (#14 `TackleEligibleRadiusM` = #23 radius), 6.0 (#8 `MOVE_PRESS_SUPPRESSION_DIST`),
  8.0 (#8 `PRESS_TRIGGER_DISTANCE`) and 15.0 (#14 `ManMarkCandidateRadiusM`). Wording is "no defender
  within r m", never "unmarked".
- **M3** — goal-side: G_D = (0, 34) if D = 0, else (105, 34). Defender j is goal-side of a iff
  |p_j − G_D| < |p_a − G_D|. Report the share where the nearest defender is goal-side, and E_r restricted
  to goal-side defenders.
- **M4** — M1–M3 recomputed on both teams' #12 slots.
- **M5** — shots by shooter k, with d and goal-side status at the shot's ledger frame.
- **M6** — goals, classified as:
  - *linked*: the latest prior shot is by ScoringTeam with ShootingAgentId = Scorer, and the witness
    never shows the conceding team between the shot and the goal;
  - *own*: Scorer = −1, or the Scorer's team ≠ ScoringTeam (Scorer is `_lastHolderAgentId`,
    `MatchEngine.cs:5842-5846`);
  - *uncertain*: everything else.
  Only linked goals are attributed to a slot.
- **M7** — denominators: samples, spells and final-third entries per team.
- **M8** — ball x at each open-play change of the witness to A (territory).

**Comparisons.**
- Pairs are by seed. Formation cases: head vs reused base C0. Duty cases (C5–C8): head vs their own base.
- Formation and Duty results are never pooled.
- Both teams' attackers are reported, because the changed team's defence changed too.

**Decision rules.** These are frozen before decoding. They are descriptive with n = 6: a one-sided sign
test reaches p = 0.016 only at 6 of 6. Each threshold below is an owner-approvable default, not a
derived value.

| Rule | Fires when | Then |
|---|---|---|
| R1 | For a formation case whose changed team scored more, that team's pooled E_8 rise (head − base) is ≤ 0 in ≥ 3 of 6 seeds | Marking proximity is refuted for that case. *3 of 6 = no better than a coin.* |
| R2 | Slot exposure (M4) rises but position exposure (M2) rises in < 3 of 6 seeds | Existing movement already compensates the slots, so "missing movement" is refuted. *Same coin threshold.* |
| R3 | Attackers whose E_8 rose produce < 50% of the changed team's shot increase | Marking is not the primary mechanism. *"Primary" = majority.* |
| R4 | The Attack-Duty team's goal-side share does not fall and E_r does not rise in ≥ 4 of 6 seeds | Duty is not a marking effect; report M8. *A majority must show no change.* |
| R5 | In mirrored pairs (C3/C4, C1/C2, C7/C8), the two strata's head − base deltas differ in sign in ≥ 3 of 6 seeds, **or** C0 shows a stratum difference of the same sign in ≥ 5 of 6 seeds | The result is "confounded (D1 candidate)": conclude only from the home-defending stratum, and recapture the pair after W13.0 |

A supporting result is that no rule fires. It prioritizes W13.1 but does not predict its size.

**Budget.** 0 captures, about 3.6 GB LFS, ≤ 4 CPU-hours Python, about one agent-day, and one protocol
review before decoding. **Phase B** (OD-9, only if inconclusive) is a frozen-lane re-run of C0/C3/C4/C7
with a read-only observer adding per-sample `GetAssignment` mode/target. That costs about 200 s per
match (`summary.json` `elapsedSeconds`).

## 4. Design

### 4.1 Layer ownership (AI → Mechanics → Physics; no interface added)

| Piece | Owner |
|---|---|
| Frame contract, Invariant 4, goal-side composition (canonical, pure static) | #14: `MarkAssigner.cs`, `InvariantEnforcer.cs`, new `MarkTargetComposer.cs` |
| `TightMarking` → stand-off | #14 `TacticTranslation.cs` (consumer-side, KD-2 pattern) |
| Gate, possession test, mirroring, per-agent write | `MatchEngine.RunMechanicsAI` |
| Carrier `bool HasMarkTarget; Vector2 MarkTarget` (world m; zero value = identity) | #8 `TacticalContext.cs` |
| MOVE override; PRESS priority-2 skip for marking agents | #8 `OptionGenerator.cs` |

The Decision Tree gets a position and a flag (physical parameters), never a #14 type, so
`decision-tree.asmdef` gains no reference.

### 4.2 Composition rule (per pool agent, once per 10 Hz stride, canonical frame)

**Step 1 — no target when any of these holds:**
- the switch is off;
- `a.Mode == Zonal`;
- `a.ValidThroughTick != tick`;
- the target is missing or `!IsActive`;
- **the agent's team holds team possession.** Team possession is
  `teamPossessor = _possessingAgentId >= 0 ? _possessingAgentId : _passInFlightReceiverId`, the
  expression already formed once at `MatchEngine.cs:4443` (the C1 union). W13.1 hoists it into one
  private helper used by `FillPositioningSnapshot` and the gate, which is behaviour-neutral.
  - It is read once at `RunMechanicsAI` entry, before the team loop.
  - `TryResolveTackles` (`:3628`) and the possession refresh (`:3636`) run after `RunMechanicsAI`
    (`:3619`), and nothing inside it writes possession (`:3630-3635`).
  - So both teams read the same stride's value: no home-before-away skew.

**Step 2 — `CoverGkZone`:** M = `a.TargetPosition` (the zone centre, (7.5, 34) canonical).

**Step 3 — `ManMark` / `InterceptRunner`:**
- O = the target's **live** snapshot position; G = (0, 34); v = G − O; L = |v|.
- M = O + (s / L)·v if L > s, else M = G.

**Step 4 — no separate cap.** The 20 m bound is enforced by Invariant 3 alone (§4.3).

**Step 5 — output:** world = `MirrorPitchIfAway(t, M)`. #8 clamps it to the pitch and does not apply
the depth adjustment.

**Units and ranges:** m; s ∈ (0, 3.0); L ∈ [0, 123.6]; |M − O| ≤ s.

**Worked examples.**
- **A (away CB marks home F4231 ST).**
  - ST world (86.1, 34) → canonical O = (18.9, 34).
  - CB1 slot (21, 25.84) is 8.43 m away, so the ST is a candidate.
  - v = (−18.9, 0), s = 2 → M = (16.9, 34) → world **(88.1, 34)**, between the ST and the goal at x = 105.
  - Mirror (home CB): M = **(16.9, 34)** world.
- **B (off-centre).** O = (20, 30), L = 20.396 → s = 2: M = (18.039, 30.392); s = 1: M = (19.019, 30.196).
- **Degenerate.** O = (1.0, 34.5), L = 1.118 ≤ 2 → M = (0, 34), then pitch clamp.

### 4.3 Invariants 3 and 4 after the D2 fix

**Invariant 3** (`InvariantEnforcer.cs:202-233`) tests the **live** O after E2.
- MAN_MARK → ZONAL is immediate, even during a dwell lock: demotion does not set `OverriddenThisTick`.
- Re-acquisition needs `MarkDwellTicks` (4) consecutive preferring ticks. `ApplyGate` restarts the
  candidate at `HoldTicks = 1`, because the commit reset `CandidateMode` to ZONAL (`MarkHysteresis.cs:53-90`).
- So at the 20 m boundary the published mark cannot flap faster than one demotion per ≥ 4 strides.
- With Step 4 removed (option 3 of drop / clamp / Inv. 3), there is one authority and its dwell is
  the hysteresis. Since |M − O| ≤ s, a consumed target is always within 20 + s m of the slot.
- *Rejected — drop on M:* that is a second, unhysteresed boundary offset by up to s, so it could
  alternate every stride.
- *Rejected — clamp to the 20 m circle:* it parks the marker at a point that is neither his slot nor
  goal-side of his man.

**Invariant 4** (E3) — one marker per opponent.
- It runs after Invariants 1–3 inside the existing ≤ 3-pass loop (`:62-80`).
- For a duplicated target: keep an emergency override if present; otherwise keep the lowest |agent − O|²
  (§3.4); on a tie keep the lower EntityId (FR-DA-014). The others are demoted (`DemoteToZonal`).
  Demotion is monotonic, so convergence holds.
- It is gated by the per-tick input `DefensiveSnapshot.MarkMovementActive` (not serialized), so default
  assignments and tackle intents are unchanged.
- *Example:* away CMs 17 at (45, 28) and 18 at (45, 40) both mark the home CAM at canonical (36.75, 34).
  Both are 10.2 m away, cost 104.06 m², so 17 keeps the CAM and 18 is demoted.
- Two CBs on one striker are already reduced to one by Invariant 1.

### 4.4 Exemptions and interactions

- **Pressers, GK, sent-off agents:** outside the pool, so no target. #13 press targets stay unconsumed.
- **Emergencies:** last man → INTERCEPT_RUNNER goal-side of the most advanced attacker; COVER_GK_ZONE →
  the zone centre. Both are meaningful only after D1.
- **In possession:** Step 1 applies. Loose-ball transitions keep marks (#14 §3.1).
- **Restarts:** no set-piece phase exists, so marking applies as in open play. Set-piece marking is #14
  §7.5, Stage 2+.
- **Substitution:** the incoming player inherits the index and the mark (locked by test, not changed).
- **Offside:** a goal-side marker can drop behind the #12 line and play attackers onside. Trap ZONAL
  targets are not consumed until D4 is fixed.
- **#12 rotations / Duty:** they move `BaselineSlot` only; the marker's own Duty does not move the mark point.
- **#23 dismarking (default Off):** with s < 3 m, markers count as "marked" for the nudge
  (`SlotComposer.cs:168-194`).
- **#440 (PR #544, other owner):** more defenders within 3 m means more tackles and `BALL_LOOSE`
  outcomes. Keep the two measurement windows separate.

### 4.5 PRESS versus MOVE (central to W13.1)

The table covers marker-to-man distance d, scored per `UtilityScorer.cs:320-394`. Assumptions:
- neutral attributes 10 ⇒ A = 9/19 (`DecisionContextAssembler.cs:86-151`), shifted 0.7368;
- MEDIUM pressing, Balanced, Support;
- loose ball (CONTESTED: phase ×1.0, no urgency), midfield zone;
- the marker comes from the goal side, so his distance to the mark point is d − 2.

Formulas:
- MOVE = 0.40 × 0.7368^0.7 (0.8075) × max((d − 2)/15, 0.10) × [0.6 if an opponent is ≤ 6 m].
- PRESS = 0.50 × 0.7368^0.8 (0.7833) × (1 − d/8).
- Selection noise is ±0.0825 per option (`ComposureWeights.cs:22,29`; `ActionSelector.cs:52-62`).

| d (m) | MOVE | PRESS | Choice |
|---|---|---|---|
| 8 | 0.129 | 0 → floor 0.01 | MOVE (noise can flip) |
| 6.17 | 0.098 | 0.098 | crossover (no suppression) |
| 6 | 0.052 (suppressed) | 0.098 | PRESS (noise can flip) |
| 3 | 0.019 | 0.245 | PRESS, deterministic (margin 0.226 > 0.165) |
| 2 | 0.019 | 0.294 | PRESS |
| 1 | 0.019 | 0.343 | PRESS |

Effects of other conditions:
- With an opposing carrier, MOVE ×1.25 (`UtilityWeights.cs:200`) and PRESS ×1.2 (`TacticalWeights.cs:45`);
  the same choices result.
- In the own third, PRESS ×0.8 gives 0.196 at 3 m; still PRESS.

**Stride by stride without a rule:**
1. Strides 0–3: dwell. The F4231 ST is 8.43 m from the CB, so there is no press; the CB holds his slot.
2. Stride 4: the mark commits. MOVE jogs toward M, 9.13 m away.
3. Once d ≤ about 6.2 m, PRESS takes over and `PressSprint` (EMERGENCY braking, target lock) heads for
   the man's **current** position: contact distance, not goal-side.
4. Priority 2 picks the **nearest** opponent, so a marker can switch men, defeating Invariant 4.

**Sprint cost.** Sustained sprinting drains the sprint reservoir at 0.12/s (`AgentMovementConstants.cs:128`):
1.0 to the 0.20 floor (`:64`) takes 6.7 s. The aerobic pool drains at 0.006/s (`:140`), and the PRESS
gate needs `AerobicPool > 0.20` (`DecisionContextAssembler.cs:83`). That is 133 s of cumulative
pressing, after which the gate closes. So "marking" by pressing is both not goal-side and self-exhausting.

**Today's default already does this for every agent.** Any defender within about 6 m of an off-ball
opponent presses him. At F442 anchors a CB is 3.44 m from a striker, so the CB is in permanent press
contact, which is plausibly why F442 strikers are contained and F4231's (8.43 m) are not. Marking
without a rule would only **extend the reach** of that press from about 6 m to 15 m.

**Decision (in W13.1, E4):** an agent with `HasMarkTarget` generates PRESS **only via priority 1**
(the carrier within 8 m, which may be his man); priority 2 is skipped. Zonal agents are unchanged, and
switch-off is byte-identical. MOVE then governs. Near the point, MOVE (0.019–0.032) is often the only
off-ball option apart from INTERCEPT, so the marker holds 2 m goal-side and re-targets every stride.
The rule's effect is checked by the closure test (§9) and by §10's goal-side metric.

**Remaining gap — pace (OD-12).** Below 6 m from the point, MOVE walks: the cap is `JogEnter` 2.2 m/s
(`AgentMovementSystem.cs:236,487-488`). An INTERCEPT_RUNNER man moves at > 3 m/s
(`DefensiveAIConstants.cs:92`), so the marker falls behind until 6 m, then jogs at ≤ 5.8 m/s
(`SprintEnter`, `AgentMovementSystem.cs:484-485`). A neutral sprinter reaches 7.5 + 9 × 2.7/19 = 8.78 m/s
(`AgentLocomotion.cs:23-27`), so the runner gets past. Recommended W13.1c: for a mark target, the tier
is the larger of the distance tier and the man's speed tier (via `JogEnter`/`SprintEnter` [CROSS #2]),
carried as one extra float `MarkTargetSpeed`.

## 5. Marking varieties and per-team selection

| Variety | Today | W13.1 | Later | Selector |
|---|---|---|---|---|
| Zonal | all movement | unchanged for zonal agents | W13.2 "Zonal" scheme = no mark targets | `MarkingScheme` (W13.2) |
| Mixed (≥ 3 zonal back line, ≤ 4 man-marks, 15 m) | assignment only | moves, goal-side | — | default |
| Ball- / man-oriented radius | radius live in assignment | moves | ManOriented raises Inv. 2 cap / lowers Inv. 1 (owner) | `MarkingOrientation` |
| One marker per opponent | — | Inv. 4 | — | — |
| Tight vs loose | — | 1.0 / 2.0 m | team tightness; `Marking` attribute (no consumer today) | `TightMarking` |
| Runner tracking | assignment only | goal-side of the live position | W13.1c pace tier; lead point | — |
| Last man / cover GK | assignment only | moves | — | — |
| Specific opponent | serialized only | — | W13-F1 (E6) | `MarkTargetEntityId` |
| Between-lines pickup | — | — | W13-F2, if §10 shows residual exposure | — |
| Trap step-up | target unread | — | W13-F4 after D4 | `OffsideTrap` |

**Per-team scheme roadmap.** W13.1's switch is engine-wide, so both teams mark or neither.

- **W13.2** adds a #21 `TeamTactic.MarkingScheme` enum, `{Zonal, Mixed, ManOriented}`, append-only
  (FR-TI-007).
  - `Zonal`: no mark targets; assignments still feed tackles as today.
  - `Mixed`: W13.1 behaviour.
  - `ManOriented`: per-scheme `[GT]` invariant limits; a #14/#21 amendment.
  - The default row is an owner decision tied to the flip.
  - Once W13.2 exists, the engine-wide switch becomes "engine supports marking", and the scheme selects
    behaviour per team.
- **Cost:** `TeamTactic` is serialized state (#21 Appendix B order), so W13.2 needs a world-schema bump
  that follows #543's v29 (v30, or whatever is next at landing) per
  `.claude/skills/snapshot-schema-bump/SKILL.md`. It also needs an #21 Appendix B append, a
  `PlayerTacticFileLoader`-style key and a client control (blocked until the flip anyway).
- Reusing `MarkingOrientation` was rejected: a zonal value would have to be appended after
  `ManOriented`, mixing two axes in one dial.

## 6. Constants

New. KD-W1 is: "Do not land a `[GT]` change governing a subsystem that is not fully wired. Defect fixes,
instruments, and measurement are unaffected" (`match-engine-wiring-backlog.md:88-90`). W2 read it as
permitting new dials on a dead surface while forbidding tuning them (`DefensiveAIConstants.cs:205-209`).
All values below are **UN-CALIBRATED**.

| Name | Tag | Catalogue | Units | Value | Range |
|---|---|---|---|---|---|
| `MarkStandoffM` | [GT] | `DefensiveAIConstants` (`GetFloat`) | m | 2.0 | (`MarkStandoffTightM`, min(`TackleEligibleRadiusM`, `MARKING_RADIUS_M`) = 3.0) |
| `MarkStandoffTightM` | [GT] | same | m | 1.0 | (0, `MarkStandoffM`) |
| `MarkMovementEnabled` | [GT] | `MatchEngineConstants`, `Config.GetBool("match-engine", "MarkMovementEnabled", false)` | bool | false | {false, true} |
| own-goal centre | [DERIVED] | `DefensiveAIConstants` | m | (0, `PITCH_WIDTH_M`/2) | from [CROSS #1 §1.2]; #14 §3.5.2 |

The loader exists: `GameplayConfig.GetBool` at `src/project-constants/GameplayConfig.cs:120`, tested
in `GameplayConfigTests.cs:36,52,82`. The precedent copied is
`ClubFinancesConstants.DeepRevenueEnabled = Config.GetBool("club-finances", "DeepRevenueEnabled", false)`,
"[GT] sole … gate, default off" (`ClubFinancesConstants.cs:50-51`). Both were verified at `d4b026aa`
with `git grep`.

**Newly live (values unchanged):**
- [GT] in `DefensiveAIConstants.cs`: `ManMarkCandidateRadiusM` 15, `RunnerVelocityThresholdMS` 3.0,
  `MarkDwellTicks` 4, `MinBacklineAgents` 3, `MaxManMarkAssignments` 4, `MaxMarkDisplacementM` 20,
  `LastManBallBufferM` 5, `LastManOwnHalfMinX` 5, `GkExpectedZoneMaxX` 15, `CoverGkZoneMaxTicks` 20.
- [GT] in #21: `MarkingOrientationScalar` {0.8, 1.0, 1.2}.
- [CROSS] bounds: `TackleEligibleRadiusM`, `MARKING_RADIUS_M`, `JogEnter`, `SprintEnter` (W13.1c).

## 7. Determinism and snapshot

- **No new cross-tick state.**
  - The composer is pure.
  - Assignment and hysteresis are already serialized (`MatchEngine.cs:8044-8073`).
  - `_tacticalContexts` is rebuilt every stride and not serialized.
  - `MarkMovementActive` is per-tick input.
  - Result: no `SNAPSHOT_SCHEMA_VERSION` change and no collision with #543's v29.
- **Activation is config-level** (`TackleContactRadiusM` pattern, `MatchEngineConstants.cs:732`), with
  an internal per-engine seam like `TestOnly_ArmTackleChallenge` (`MatchEngine.cs:2730`). A per-match
  flag in the `EnableGkHeading` style is serialized (`:7183,7492`) and would force a bump after v29
  (OD-11). The hazard is that a save restored under different config diverges, as with any [GT].
- **No RNG**, domain tag or draw change. Iteration is the EntityId-ascending pool and the k-ascending loop.
- **Rates:** composition runs at 10 Hz in `RunMechanicsAI`; commands persist across 60 Hz frames as today.
- **Hot path:** structs and `in`/`out`, no LINQ or allocation, O(22) lookups. Extend
  `Performance_T_DA_PERF_002`.

## 8. Rollout on `claude/great-sagan-rpcmhp`

**Order.** Commit ① (plan) is the only commit made so far. Code commits ②–⑨ follow owner approval of
the §11 decisions. W13.0 then lands as one
commit-group: spec, ERR and code together per `.claude/skills/err-file-and-backprop/`. W13.1 follows
as a later group.

**Base.** The branch is at main `d4b026aa`. #543's `MatchEngine.cs` hunks (around 319, 1043, 1552, 2305,
4425-4469, 6911-7057, 7347-7431, 7977-7995) do not overlap W13's (3610-3930, 4430-4446 helper,
4621-4704, test seams). With no schema change, a later rebase or merge with #543 is mechanical. F4231
tests and §10 need #543 merged first.

**Default options.**

| Option | Identity | Verdict |
|---|---|---|
| (a) live by default | every baseline moves (`pre-c-control-20261104/expected.json`, scenario predicates) | no — mixes wiring with a behaviour change |
| **(b) off by default** | identity of gameplay and serialized bytes **relative to the post-W13.0 tip** (W13.0 itself changes defaults) | **yes**: the #434 "exact identity at default" pattern and W2's "shipping disabled" |

**Evidence for (b).**
- Available here (SDK 8.0.133, non-certifying):
  - unit/engine gates;
  - two-run determinism, armed;
  - snapshot-restore continuation;
  - an env-gated probe (`TD_MARK_IDENTITY_PROBE=1`) that prints six-seed digests at frame 18,000 at the
    W13.0 tip and the W13.1 tip. They must be equal.
  - No production test pins gameplay digests today (grep: only `CertifiedPerfBaselineTests.cs`).
- Frozen lane only (the #543 owner's environment):
  - six-seed full-match terminal identity of the W13.1 switch-off build vs the W13.0 tip;
  - W13.0's own six-seed characterization and digest adoption;
  - §10.
- Pinned Unity host: compile and EditMode (src/CLAUDE.md).

**W13.0 vs D-05.**
- KD-W1 exempts defect fixes (`match-engine-wiring-backlog.md:88-90`).
- D-05 (`:321-329`) names #440/#434 and forbids behaviour-changing landings inside an A→B / B→C window.
  By analogy W13.0 must not land inside an open window.
- Main has W8 C merged (`d4b026aa`, PR #542), but whether its measurement window is closed is **not
  verifiable here**; check `path-to-playable-roadmap.md` at commit time (OD-1).
- The default flip (OD-2) is under the same rule. Formation/Duty player controls stay blocked until the
  flip is accepted.

## 9. Ordered commits (each: narrow gate, then the full Linux gate; all engine tests `[TestCase(0)] [TestCase(1)]`)

**① This plan.** Doc only; add its file-manifest row at ④.

**Stop rule for every code commit.** If a pre-existing test fails after a change, or a scenario
predicate moves, stop and report it to the owner with the failing output. Do not edit, widen, skip or
quarantine an existing assertion, and do not tune a constant to recover it. This applies in particular
to W13.0, which changes default behaviour in the live tackle chain.

**② W13.0 — D1 + E1 (spec + ERR + code + tests).**
- Code:
  - `DefensiveSnapshot.cs`: document the frame.
  - `LastManDetector.cs`, `MarkAssigner.cs` (`ThreatScore(…, bool defendsX0)`), `TackleIntentEvaluator.cs`,
    `OffsideTrapController.cs`, `InvariantEnforcer.cs`: take `defendsX0` from the frame.
  - #14 `section-2.md` §2.3 and `section-3.md` notes, with version rows.
- Tests (`DefensiveAITests.cs`): team-1 twins of `T_DA_016/021/026/035/038/041`, with identical canonical
  inputs giving identical outputs.
- Tests (`MatchEngineAwayTeamTests.cs`): a mirrored kickoff gives mirrored assignments, via a new
  `TestOnly_MarkAssignment`.

**③ W13.0 — D2 + E2.** `MarkAssigner.cs` refreshes retained `TargetPosition` from the live snapshot.
Test: a retained assignment tracks a moving target; Invariant 3 demotes on the live distance.

**④ W13.0 close-out.** The ERR log, backlog and the §12 subset. Frozen-lane characterization is a
handoff; adopt digests by owner decision.

**⑤ W13.1 — E3/E4/E5 spec text + `defensive-ai` + tests.**
- Code:
  - `DefensiveAIConstants.cs`: the §6 constants.
  - `TacticTranslation.cs`: `MarkStandoffM(bool)`.
  - New `MarkTargetComposer.cs`:
    `static bool TryCompose(in MarkAssignment, DefensiveSnapshot, int tick, float s, out Vector2)`.
  - `DefensiveSnapshot.MarkMovementActive`.
  - `InvariantEnforcer.cs`: Invariant 4 plus `AreAllSatisfied`.
- Tests:
  - Examples A/B/degenerate to 1e-3.
  - Stale tick, inactive target, COVER_GK_ZONE passthrough.
  - Stand-off invariant (tight < normal < 3.0).
  - Invariant 4 example (17 keeps, 18 demoted).
  - gated-off leaves every existing `DefensiveAITests` case unchanged.
  - Extend `Determinism_T_DA_DET_006` to four invariants.
  - Zero allocation.

**⑥ W13.1 — `decision-tree` + tests.**
- Code: `TacticalContext.cs` adds `HasMarkTarget` and `MarkTarget`; `OptionGenerator.cs` adds the MOVE
  override and skips priority 2 when `HasMarkTarget`.
- Tests (`OptionGeneratorTests.cs`):
  - Unset: MOVE target equals the adjusted slot bit-for-bit, and PRESS is unchanged.
  - Set: MOVE targets MarkTarget without the depth adjustment; PRESS is absent with only a non-carrier
    within 8 m, and present toward a carrier within 8 m.

**⑦ W13.1 — `match-engine` + tests.**
- Code:
  - `MatchEngineConstants.MarkMovementEnabled`.
  - `MatchEngine.cs`: a possession helper shared with `:4443`; `FillDefensiveSnapshot` sets
    `MarkMovementActive`; composition and mirroring after `_defensive[t].Tick`.
  - Seams `TestOnly_ArmMarkMovement(bool)` and `TestOnly_MarkTarget(id)`.
- New `MatchEngineMarkingTests.cs`, built on the `MatchEngineMechanicsTests.cs` patterns and `TestOnly_SetAgent`:
  1. **Acquisition.** The back four sit at their slots; the attacker is 8.43 m from both CBs and 2.1 m
     deeper; the carrier is at least 25 m away. Within `MarkDwellTicks` + 1 = 5 strides, exactly one CB
     holds MAN_MARK, and the seam target equals example A (mirrored).
  2. **Closure, N = 60 strides.** Needing to get within 1.0 m of M (so ≤ 3.0 m from a stationary man),
     the walk-only worst case is:
     - accelerate to 2.2 m/s at 2.0 m/s² (`AgentMovementConstants.cs:168`): 1.1 s, 1.21 m;
     - then (9.13 − 1.0 − 1.21)/2.2 = 3.15 s;
     - total 4.25 s ⇒ 43 strides, + 5 acquisition + 2 dispatch/turn margin + 10 (1 s safety) = 60.
     Jogging only shortens it.
     Assert d ≤ 3.0 m **and** the body is goal-side (|p_marker − G| < |p_man − G|), which now holds
     because priority 2 is skipped.
  3. **Boundary.** The man alternates 19.9 / 20.1 m from the CB's slot each stride. No MAN_MARK is
     published on a > 20 m stride; ≥ `MarkDwellTicks` strides separate a demotion from the next
     acquisition; a consumed target is ≤ 20 + s m from the slot.
  4. **Invariants every stride:** ≥ 3 back-line ZONAL; ≤ 4 MAN_MARK; no duplicate targets.
  5. **No target** for the GK, pressers, the possessing team (carrier and pass-in-flight receiver), a
     sent-off agent, or with the switch off.
  6. **Substitution and dismissal** during a mark continue deterministically.
  7. **Restore continuation**, armed and mid-dwell, for 600 frames (`MatchEngineSnapshotRestoreTests.cs`).
  8. **Two-run determinism**, armed, 2 × 10 min (`MatchEngineDeterminismTests.cs`).
  9. **Identity probe** (§8).

**⑧ W13.1c (if OD-12 approved).** The mark pace tier in #8 §3.5.6 (`ActionDispatcher.cs:221-241`),
carrying `MarkTargetSpeed`. Test: a 5 m/s runner keeps its marker ≤ 3.0 m away for 30 strides.

**⑨ Close-out (§12)**, then the frozen-lane §10 handoff. The flip is a later one-line commit after OD-2.
**After #543 merges:** F4231 kickoff test — the ST acquires a marker in the first defensive spell — and
`MatchEngineFormationDutyTests.cs` must still pass with the switch armed.

## 10. Post-wiring measurement (frozen lane; after #543 and W13.1)

**Arms:**
- (i) Switch off, C0 ×6: exact digests vs the W13.0 tip.
- (ii) Armed: C0–C9 ×6 = 60 full matches.
- "Before" = the post-W13.0 recapture, or #543's retained traces where R5 did not fire.

**Instrument.** #543's runner plus `marking.bin` per sample: mode u8, target i8, `HasMarkTarget` u8,
target 2 × f32. It is armed through the config key, bound before the first engine construction.

**Metrics.** §3's M1–M8 with identical definitions and strata. Also:
- assignment census;
- tackle intents and resolutions, fouls, offsides per team;
- PRESS-on-non-carrier share by markers (must be 0);
- Δgoals / Δshots per case vs C0.

**Hard stops.** NaN; an invariant breach; a target on a GK, presser, possessing team or inactive agent;
a switch-off digest mismatch; a determinism or restore failure; integrity totals.

**Descriptive only.** Every football metric: no realism bands are approved. The owner judges whether
the sensitivity shrinks.

**Budget.** 66 captures × ~200 s ≈ 3.7 CPU-hours (~2 h wall at 2 concurrent), ~2.8 GB LFS, rerun of
the §3 script.

## 11. Risks and open owner decisions

**Risks:**
- The 15 m radius is defender-centric: an F4231 CAM 17.7 m from all slots may stay free (W13-F2).
- Invariant 1 leaves one back-line marker against two strikers.
- Goal-side markers lower the offside line.
- W13.0 moves default baselines and the away tackle rate, and therefore foul/card exposure (measure,
  do not tune).
- Scenario predicates (`close_chance`, `inposs_gate`, shot-outcome reachability) may move at the flip,
  so run the scenario suite before OD-2.
- Skipping priority 2 removes markers from ad-hoc pressing of other nearby attackers; zonal agents
  still press.
- A config-level restore hazard.
- INTERCEPT versus MOVE near the mark point is not scored in §4.5. A marker may leave his man for a
  fast ball passing within intercept range. Commit ⑦ adds one engine case that records the chosen
  option in that situation; it is descriptive, not asserted.

**Testable on main alone:** D1/D2, the composer, Invariants 3 and 4, the DT override and PRESS rule, F442
acquisition/closure/boundary/restore/determinism, and the identity probe.

| ID | Decision | Recommendation |
|---|---|---|
| OD-1 | W13.0 (default-changing defect fix) first, outside any open window | yes; adopt digests after frozen-lane characterization |
| OD-2 | Default (a) live vs (b) off | (b); flip after §10 |
| OD-3 | Work on main-based branch; #543 merges first for F4231 tests/§10 | yes |
| OD-4 | Invariant 4 in W13.1 (gated) | yes |
| OD-5 | Stand-offs 2.0 / 1.0 m as un-calibrated [GT] | yes (KD-W1, W2 precedent) |
| OD-6 | `MarkTargetEntityId` | follow-up W13-F1 with E6 |
| OD-7 | Between-lines coverage pass | only if §10 shows residual exposure |
| OD-8 | PRESS priority-2 skip for marking agents in W13.1 | **yes (decided in plan, §4.5); veto keeps the press-to-contact behaviour** |
| OD-9 | Phase B instrumented re-run | only if §3 is inconclusive |
| OD-10 | R1–R5 thresholds | approve or amend before decoding |
| OD-11 | Config-level switch vs serialized per-match flag | config-level |
| OD-12 | W13.1c mark pace tier | yes; otherwise runners outpace markers |
| OD-13 | W13.2 per-team `MarkingScheme` (schema bump after v29), and its default row | schedule after the flip decision |

## 12. Tracking-document changes (in ④ and ⑨)

- `match-engine-wiring-backlog.md`: Class-A **W13** (cross-reference C10), the follow-ups
  W13.1c/W13.2/W13-F1…F4, a §5 row and a version row.
- `spec-error-log.md`: E1–E5, plus the header chain.
- `open-issues.md`: W13; formation/Duty controls blocked until OD-2; the diagnosis verdict.
- `file-manifest.md` and `src-tree.md`: the new files, the evidence directory and this plan.
- `CHANGELOG.md` and `CHANGELOG-src.md`, and `src/CLAUDE.md` per `landing-close-out`.
- `data-contract-index.md`: the `TacticalContext` row (mark carrier) and `DefensiveSnapshot`
  (frame, `MarkMovementActive`).
- Spec version rows in #14 (§2/3/4/6), #8 (§2.2, §3.1) and #21 (§3), and the `SPEC_INDEX.md` note.
- `path-to-playable-roadmap.md`: a PM-2 note that formation/Duty controls wait on W13.

## Version history

| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-11-10 | — | Initial draft. |
| 0.2 | 2026-11-10 | — | Review round 1. PRESS/MOVE utility analysis and a priority-2 skip for marking agents moved into W13.1. The `GetBool` loader and its precedent are cited. The composer's displacement drop is replaced by Invariant 3 alone, with a boundary test. A per-team `MarkingScheme` roadmap (W13.2) is added. Delivery is ordered commits on one branch, with identity relative to post-W13.0 and evidence split between here and the frozen lane. The diagnosis becomes a non-blocking handoff, stratified by defending team, with R5. N = 60 is derived. Thresholds are justified; KD-W1, D-05 and the possession read are cited. Trimmed. |
| 0.3 | 2026-11-10 | — | Review round 2 (editorial). Status moved to reviewed and awaiting owner decisions. A stop rule for pre-existing test failures is added to §9. The plan's manifest row moves to commit ④. The unscored INTERCEPT interaction is listed as a risk with a descriptive test. No design change. |
| 0.4 | 2026-11-10 | — | Records the owner's acceptance of all thirteen recommended decisions. Code commits ②–⑨ may proceed in order. No design change. |
| 0.5 | 2026-11-10 | — | W13.0 landing note: the ERR-014-007 gap (§2). No design change. |
