# Goalkeeper Contact Rate — §5.Z.20 §7.1, the uncontacted three quarters

> **Created:** July 28, 2026
> **Status:** DESIGN SUPPLEMENT — the same governance class as `match-engine-design.md`. Opens no
> numbered spec and changes no `SPEC_INDEX.md` row. Files cross-spec back-props against Goalkeeper
> Mechanics **#11** (`ERR-011-007`) and Positioning AI **#12** (`ERR-012-010`) — ids verified free
> against `spec-error-log.md` and both spec folders before assignment (ERR-012-005/006 stay
> soft-reserved by the June-13 quarantine cluster and are not touched).
> **Owner document:** `docs/tracking/match-engine-design.md` **§5.Z.22**.
> **Purpose:** §5.Z.20 fixed the catch/parry conversion and measured its own residual: a contact
> almost always stops the shot, and the keeper contacts only ~a quarter of on-target shots — so the
> uncontacted three quarters of on-target shots is where nearly all the surplus goals live
> (goals/shot 0.19–0.26 against football's ~0.10). §7.1 named the two levers and put both out of
> that pass's scope: the #12 GK slot's lateral positioning, and the #11 commit-to-arrival timing.
> This note is the design, calibration and measurement of both. Neither is a `[GT]` dial: each is a
> behaviour change to an APPROVED spec, which is why this pass exists as its own supplement.

---

## 1. The finding

### 1.0 The anatomy, measured per episode (new instrument, 3 full matches, `ConfigureSquads` path, the §5.Z.20 seeds)

§5.Z.20 §7.1's anatomy was measured over airborne *frames* (mean lateral offset 1.7–4.6 m against
2.2 m of dive displacement + ~1.35 m reach). A frame aggregate cannot attribute: it cannot say
whether the keeper was standing in the wrong place (#12) or diving at the wrong time (#11). The
new `GkContactRateDiagnosticTests` classifies each goalward threat episode at the ball's actual
goal-plane crossing — the moment a save happens or does not:

| classification | meaning | lever |
|---|---|---|
| `contact` | the hand envelope met the ball | — |
| `no-dive` | ball crossed the plane, keeper never went airborne | #11 (commit never fired usefully) |
| `dive-early` | the dive was launched AND OVER before the ball crossed | #11 commit timing |
| `dive-late` | the dive launched after the crossing | #11 commit timing |
| `lateral-miss` | airborne AT the crossing, envelope laterally short | #12 positioning / dive direction |
| `faded` | episode disarmed before any crossing | — (not a save situation) |

Measured (per keeper, three matches; the table below is the corpus aggregate):

| | measured (corpus aggregate, 6 keeper-matches) |
|---|---|
| threat episodes | 82 (59 faded — deflected, picked up, or ran out of pace before any crossing) |
| contacted episodes | **8** |
| crossed un-contacted | **15** (8 of them in-mouth = conceded on target) |
| — `dive-early` | **9 of 15 (60%)** — the dive was over **456–2000 ms** before the ball crossed |
| — `no-dive` | 3 of 15 |
| — `lateral-miss` | 3 of 15 |
| — `dive-late` | 0 |
| contact rate over resolved episodes | 8 / 23 ≈ **35%** |
| mean flight time (strike → crossing) | 925–2006 ms, against the **600 ms** dive envelope |
| mean \|ballY − gkY\| at the crossing | **1.91–3.83 m**, against 2.2 m displacement + ~1.35 m reach |
| mean \|gkY − 34\| at episode start | 0.29–1.97 m |
| goals | 2-2 / 3-2 / 5-0 (the §5.Z.21 population) |

The attribution is unambiguous: **timing is the dominant class** — the keeper dives the moment
SAVE commits and is back on the ground half a second to two seconds before the ball arrives
(§1.1) — with the lateral need at the crossing running at or beyond the envelope's total coverage
in the tail (§1.2). `dive-late` at zero confirms the commit is never *slow*; it is always too
eager. The two levers §5.Z.20 §7.1 named are exactly these two classes.

### 1.1 The #11 defect: the dive is launched the moment SAVE commits, not when the ball arrives

`GoalkeeperStateMachine.EvaluateTacticalTransition`'s `Anticipate → Diving` row is
`if (hasSaveIntent) return Diving;` — the dive launches at the FIRST 10 Hz tick after the DT's
SAVE commits, unconditionally. The dive is then a fixed **600 ms** window
(`DivePhaseDurationMs`): the reach envelope sweeps `diveDirectionLateral ×
DiveLaunchDisplacementM × t` over the dive and the state resolves to `Recovering` at its end.
Against measured shot flight times of ~0.9–1.6 s (15–25 m at 14.7–16.1 m/s tick means, §5.Z.19),
the envelope has closed and the keeper is on the ground **before the ball arrives** — the
`dive-early` class. §5.Z.20 §7.1 item 2 recorded exactly this: *"the engine cannot time a dive —
the keeper commits at the earliest stride after the DT's SAVE; a real keeper delays to match the
ball."* The spec's own §3.1 transition table carries the unconditional row, so the spec is the
defect (**ERR-011-007**).

### 1.2 The #12 deficit: the GK slot's lateral term cannot put the keeper on the shot line

#12 §3.3.3: `gkSlot.y = PITCH_WIDTH_M/2 + GK_LATERAL_FACTOR × basisY(ball.y)` with
`GK_LATERAL_FACTOR = 2.0` and `basisY = (ball.y − 34)/34` — the keeper's slot moves at most
**±2 m** from the pitch centreline over the entire 68 m width, and the gain is anchored to pitch
geometry, not goal geometry. A keeper positions on the line from the ball to the goal it defends;
at depth `d` off the goal line with the ball `bx` metres out, the ball-line point is
`34 + (ball.y − 34) × d/bx` — a *goal-anchored* gain that grows as the ball closes, which the
pitch-width form cannot express at any `[GT]` value (at `GK_LATERAL_FACTOR` large enough to track
a close ball it would drag the keeper out of the goal mouth for a far one). This is a formula-shape
change, not a retune (**ERR-012-010**).

### 1.3 The interplay that must not regress §5.Z.20

ERR-011-005 froze the §3.2.3 reaction window at the dive-LAUNCH frame. With an immediate dive,
launch ≈ intent commit (one frame apart), so that anchor was equivalent to "the keeper's
reaction". Under a held dive they separate — a keeper that decides in 200 ms and then deliberately
*waits* 600 ms for the ball would be scored as sluggish, un-fixing the window §5.Z.20 just fixed.
The window's `elapsed` therefore anchors at the **intent commit** (`SaveIntent.
AttemptCommittedTick`, already carried), which is measurement-equivalent pre-hold and semantically
the keeper's reaction post-hold. Folded into ERR-011-007's spec patch. *(This design-time form was
itself refuted by the first full-corpus measurement and generalised to the first decision
opportunity at/after the live stamp — the §3 calibration iteration; the shipped anchor is §3's.)*

---

## 2. Key decisions

- **KD-CR1 — the dive is held in `Anticipate` until the ball's predicted time-to-plane matches the
  envelope's coverage of the predicted lateral need.** The commit gate is a pure function of the
  current ball state and keeper position, evaluated at the 10 Hz tactical tick:

  ```
  timeToPlaneS   = (gk.x − ball.x) / ball.vx           (only when closing; else hold)
  predictedY     = ball.y + ball.vy × timeToPlaneS      (the ERR-011-003 linear interception)
  lateralNeedM   = |predictedY − gk.y|
  commitLeadS    = clamp(lateralNeedM / DiveLaunchDisplacementM, DiveCommitMinLeadFrac, 1)
                   × DivePhaseDurationMs / 1000
  commit when timeToPlaneS ≤ commitLeadS
  ```

  The envelope reaches lateral offset `L` at `t = L / DiveLaunchDisplacementM` of the dive, so
  committing at that lead puts the hands at the predicted crossing point when the ball crosses —
  full extension for a corner ball, a short sharp step for a central one (the
  `DiveCommitMinLeadFrac` floor keeps a central commit from degenerating to zero lead; the 10 Hz
  gate quantises the lead by ≤ 100 ms, well inside the envelope's reach margin). A ball already
  inside the lead when SAVE commits dives immediately — today's behaviour, preserved for
  close-range blasts. A ball that is not closing (deflected away, possessed) holds; the engine's
  existing disarm path (`ClearSaveIntent` when the arming geometry drops) ends the episode, so the
  hold cannot deadlock — `Anticipate` keeps its ERR-011-002 `→ Set` exit.
- **KD-CR2 — no new cross-tick state.** The gate is recomputed from ball state each tactical tick;
  nothing is latched, so there is **no `SNAPSHOT_SCHEMA_VERSION` change**. (A latched
  "hold-until tick" would have to be serialized and would drift from the ball on restore; the
  recompute is both simpler and restore-correct by construction.)
- **KD-CR3 — the #12 GK slot's lateral term becomes the ball-line point, clamped to the goal
  mouth.** In the #12 canonical frame (keeper defends x = 0):

  ```
  gkSlot.x = GK_DEPTH_M + GK_ADVANCE_FACTOR × basisX(ball.x_clamped)      (unchanged)
  gain     = gkSlot.x / max(ball.x_clamped, gkSlot.x)                      ∈ (0, 1]
  gkSlot.y = PITCH_WIDTH_M/2 + clamp((ball.y − PITCH_WIDTH_M/2) × gain,
                                     −GK_LATERAL_CLAMP_M, +GK_LATERAL_CLAMP_M)
  ```

  `[GT] GK_LATERAL_CLAMP_M` (3.0 m, inside the 3.66 m half-mouth) replaces `GK_LATERAL_FACTOR`'s
  role as the lateral bound; the gain is `[DERIVED]` geometry, not a tunable. The formula is
  continuous in both inputs, stays inside the goal mouth by construction, and reduces to
  goal-centre when the ball is central — the pre-fix identity for the common case. Because
  `MirrorPitchIfAway` is a 180° rotation, one canonical formula serves both teams (the §5.Z.12
  rule: a mirror has one place that must agree).
- **KD-CR4 — `GK_LATERAL_FACTOR` is retired, not retuned.** No `[GT]` value of the pitch-width
  form expresses ball-line tracking (§1.2); leaving the constant in place "for compatibility"
  would be the parallel-surface trap. The #12 §6.1 catalogue row is replaced by
  `GK_LATERAL_CLAMP_M` in the same patch (ERR-012-010).
- **KD-CR5 — the reaction window's `elapsed` anchors at `SaveIntent.AttemptCommittedTick`**
  (§1.3). The freeze site stays the dive-launch frame (ERR-011-005's frozen-value contract is
  untouched); only the anchor of `elapsed` changes from the launch frame's time to the intent's
  commit time. Pre-hold the two differ by ≤ one tactical stride, which is why §5.Z.20's measured
  windows (0.30–0.67) remain valid calibration.
- **KD-CR6 — attribution comes from the per-episode classifier, not sequential landings.** The
  instrument classifies every crossed episode as `dive-early` (#11's lever) or `lateral-miss` /
  `no-dive` (#12's and the commit's), so one pre/post pair attributes both levers at episode
  level; a sequential timing-only → timing+positioning ladder would have spent two more
  full-corpus runs to learn the same split. (The measured calibration iteration this pass DID
  need was the §3 window-anchor regression, found by the funnel instrument on the first
  full-corpus run.)
- **KD-CR7 — standing-catch and angle-narrowing rushes stay out.** §7.1 item 5 (a standing-catch
  path for slow central balls) and a #11 rush model are real gaps, but each is a new mechanism
  with its own risk surface; this pass changes when the existing dive fires and where the existing
  slot stands. Both stay recorded (§7).

---

## 3. Calibration

New constants (all consumed by the mechanisms above):

| constant | tag | value | note |
|---|---|---|---|
| `GoalkeeperConstants.DiveCommitMinLeadFrac` | `[GT]` | 0.25 | floor on the commit lead as a fraction of the dive duration; keeps a central commit from a zero-length dive |
| `PositioningAIConstants.GK_LATERAL_CLAMP_M` | `[GT]` | 3.0 | lateral bound of the GK slot off goal centre; inside the 3.66 m half-mouth so the slot never leads the keeper past a post |

**Calibration path (KD-CR6).** The first full-corpus run after the two mechanisms landed exposed a
regression the design's own KD-CR5 had mispredicted: the reaction window at contact collapsed to
0.000–0.084 (§5.Z.20's band was 0.30–0.67). Under the hold, the actual SHOT is frequently struck
*after* the intent commit — the ERR-011-006 overwrite re-stamps the episode with the newer shot,
and `elapsed = commit − stamp` reads seconds-negative, clamping the window to 0. The anchor was
refined to the keeper's **first decision opportunity at or after the live stamp**:
`max(AttemptCommittedTick × 100 ms, ceil(stamp / 100 ms) × 100 ms)` — one formula covering both
orderings (a set, coiled keeper re-reads the new trajectory at its next tactical tick; the dive
direction is computed at launch from the live trajectory, so there is no misdirection window
beyond that tick to penalise). Folded into ERR-011-007's §3.2.3 patch; locked by
`ReactionWindow_ShotStruckAfterCommit_AnchorsAtNextTickAfterStamp`.

## 4. What was deliberately not done

- No change to the dive model itself (displacement, duration, reach, the {-1, 0, +1} direction
  axis) — §3.3's synthetic dive is Stage-0-scoped and migrates to AM #2 kinematics at Stage 1
  (KD-12 in the #11 spec); re-tuning it here would couple two passes.
- No change to the SAVE decision (DT ordinal 7, sole-off-ball-option, `HostSaveDispatch`) — the
  decision fires correctly; what was wrong is when the dive it commits launches.
- No pointQuality fix (§5.Z.20 §7.1 item 3) — the fixed-noise lottery only matters once contacts
  happen at a realistic rate; it is the recorded next conversion lever, after this pass.

## 5. Acceptance

`match-engine-keeper-contact` (#19 ScenarioRunner, Tier B, 2 seeds × 45 min, `ConfigureSquads`
path — the §5.Z.20 population): (a) threat episodes resolve (non-vacuity), (b) a committed dive
is HELD — `Anticipate` + live `SaveIntent` across ≥ 2 consecutive tactical ticks, structurally
impossible pre-fix, (c) contacted episodes outnumber un-contacted goal-plane crossings, (d) no
crossed episode's dive resolved more than 350 ms before the ball arrived (pre-fix: 456–2000 ms).
**Verified by execution in a worktree at the pre-fix commit: 3 of 4 predicates fail**
(`heldCommits = 0`; contacts 3 vs 4 crossings, inverted; one deep dive-early). Plus unit locks:
`GoalkeeperCommitGateTests` (11 — the shared predictor, the lead arithmetic, the hold-vs-commit
verdicts including the measured dive-early geometry that must now hold), four ball-line GK-slot
locks in `PositioningAITests` (near-post tracking, clamp, central identity, far-ball bound), and
the intent-commit / shot-after-commit window-anchor locks in `GoalkeeperConversionTests`.

## 6. Measured result

Three full matches, `ConfigureSquads` path, same seeds pre/post (per-episode anatomy + the
§5.Z.17 funnel, both re-run on the final tree):

| | baseline | post-fix |
|---|---|---|
| contacted episodes | 8 | **23** |
| crossed un-contacted | 15 | **9** |
| contact rate over resolved episodes | ~35% | **~72%** |
| `dive-early` (of crossed) | 9, over by **456–2000 ms** | 4, over by **83–183 ms** (10 Hz-grid scale) |
| `no-dive` / `lateral-miss` | 3 / 3 | 2 / 3 |
| `dive-late` | 0 | 0 |
| mean reaction window at contact | 0.30–0.67 (§5.Z.20) | **0.34–0.44** (recovered after the §3 anchor iteration) |
| catches | 6 | **10** |
| goals | 2-2 / 3-2 / 5-0 (**14**) | 2-2 / 2-0 / 6-3 (**15**) |

**The mechanisms are alive and did what they were designed to do** — the keeper now meets
roughly three of every four resolved threats instead of one in three, the deep dive-early class
is gone (the residue is one-to-two tactical strides of quantisation), holds are observable, and
catches rose with the window intact. **And the goal rate did not measurably move: 14 → 15 over
the corpus (4.7 → 5.0 per match), with per-seed movement in both directions (5 → 2, 4 → 9).**
This is the §5.Z.17 shape again — the prediction that a realistic contact rate would
mechanically drop goals/shot into the 0.10–0.15 band assumed *a contact stops the shot*, and
that premise (measured true at §5.Z.20's 8 contacts) does not survive tripling the contact
count: the added contacts are marginal, end-of-envelope touches whose parries and spills keep
the ball alive in the box, and the 6-3 outlier match shows goal chains that a parried-dead ball
would have ended. The surplus has moved from "the keeper never touches it" to **what a touch
does** — the §7.1 conversion residue.

Full `tools/dotnet-ci/run-gate.sh`: see the §5.Z.22 landing record.

## 7. Recorded, NOT fixed

1. **Conversion at contact is now the goal-rate residual, measured for the first time against a
   realistic contact rate.** A marginal, end-of-envelope touch parries or spills the ball LIVE
   into the box, and the goal count shows the chains that follow. Two named sub-levers:
   the **pointQuality lottery** (§5.Z.20 §7.1 item 3 — E ≈ 0.68, invariant under every `[GT]`,
   attribute-blind; with contacts at 23/corpus it is no longer second-order), and **parry
   placement** (`GoalkeeperHandlingQuality`'s parry/deflect velocity helpers aim by clutch
   firmness and deflection target, but nothing steers a parry AWAY from the goal mouth or the
   crowd in front of it — football keepers parry wide by training).
2. **Standing catch / smother for slow central balls** — a ball the keeper could simply hold still
   requires a dive today (§5.Z.20 §7.1 item 5).
3. **No angle-narrowing rush** — the keeper defends the line; nothing comes for a through-ball or
   narrows a one-on-one (the #11 rush model exists but has no engine-driven producer).
4. **Close-chance creation / possession churn** (§5.Z.21's recorded residual) — final-third
   entries ~3× football's; owns the residual shot-count gap. Out of scope here, next after the
   keeper's conversion.

## 8. Adversarial review history

| Round | Findings | Notes |
|---|---|---|
| Measurement-1 (baseline per-episode anatomy, 3 full matches) | — | Attributed §5.Z.20 §7.1's frame-aggregate anatomy per episode: 9 of 15 crossed episodes dive-early (456–2000 ms deep), 3 no-dive, 3 lateral-miss, 0 dive-late — both named levers confirmed, timing dominant |
| AR-1 (first full-corpus run of the landed mechanisms) | 1H | **The KD-CR5 anchor regressed the §5.Z.20 window** (measured 0.000–0.084 at contact): under the hold the shot is usually struck AFTER the intent commit and the ERR-011-006 overwrite re-stamps the episode, so `commit − stamp` read seconds-negative. Fixed: `elapsed` anchors at the first decision opportunity at/after the live stamp (§3); windows re-measured 0.34–0.44 |
| AR-2 (code + spec sweep over the shipped diff) | 0H+1M+2L | M: five #11 tests + the save-launch scenario ENCODED the parked-ball/immediate-dive contract (the Phase-H class) — re-anchored to a closing ball with intent preserved, +1 new lock for the shot-after-commit anchor. L: the v0.5 history row cited §3.4.5 for a §3.4.4 row; the ungated `OneOnOne → Diving` path gains its deliberate-scope note (close-range by construction; the predictor does not model a dribbled ball) |
| AR-3 (fresh full-surface re-read) | 0H+0M | CONVERGENCE — gate purity (no clock read, no draw, no cross-tick state) re-verified against the serialization exclusion set; the canonical-frame claim (one formula, both teams) re-verified against `MirrorPitchIfAway`'s 180° rotation |
| AR-4 (full-gate fallout — 2 failures, both instruments, neither a defect in the landed mechanisms) | 0H+2M | **M-1: the shot instruments' end-of-tick sampling broke the moment the keeper actually contacts.** `MatchEngineShotSpeedScenarios` (and the env-gated `ShotOutcomeDiagnosticTests`) sampled the strike's speed and its attacked-goal attribution from `BallView` at END of the strike tick, with the goal named by the sampled velocity's x-sign. A same-tick Resolve step after the strike — a first touch by a defender or keeper within reach — redirects the ball before that observation, and this pass made such touches common: measured, a 13 m strike read as **92.3 m** (velocity reversed ⇒ wrong goal), driving the scenario's `mean-shot-distance` predicate to 51.80 vs its 24.0 ceiling, and the same dilution had left the speed-mean floor passing by **0.08**. Fixed at the root with a new engine diagnostic seam `TestOnly_LastShotStrikePosition/Velocity` (captured beside the `_shotContacts` increment — post-ApplyKick, before anything else can move the ball; the `WoodworkStrikes` class, not serialized), consumed by both instruments; and the scenario's windows resized 9 → 18 min/seed (the §5.Z.21 AR-4 precedent) because this pass thinned the 9-min windows to **3 strikes total**, a per-sample lottery for a mean. Predicates and bounds UNCHANGED — pre-fix shots still cluster at the range gate (30–34 m measured means), which the 24.0 ceiling still refuses. Measured clean: 11 strikes, distMean 22.7 ≤ 24.0, speedMean 21.9, max 24.9. **M-2: `ReadingTheP1Surface_IsObserverNeutral`'s non-vacuity guard tripped** — this pass moved its seed's first restart from ~tick 3 900 to a measured 7 270, past the 6 000-tick window sized against the old trajectory; window re-measured to 8 000, guard intact. Both are the expected class for a behaviour change to the most-composed assembly: whole-match trajectories move, and instruments calibrated against the old trajectory surface their assumptions |


#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-28 | — | Initial draft: per-episode anatomy instrument + KD-CR1..CR7; measured tables pending the baseline run. |
| 1.0 | 2026-07-28 | — | Implemented + measured. §1.0 baseline anatomy (9/15 dive-early — timing dominant); §2 KD-CR6 rewritten to the classifier-attribution form actually used; §3 the window-anchor regression iteration (AR-1); §6 measured table (contact rate 35% → 72%, catches 6 → 10, goals 14 → 15 — unchanged at n=3, residual moved to conversion at contact); §7 the conversion/parry-placement residue; §8 AR history to convergence. |
| 1.1 | 2026-07-28 | — | AR-4 (full-gate fallout): the shot instruments' end-of-tick sampling replaced with the strike-time `TestOnly_LastShotStrike*` seam (a same-tick post-strike touch reversed the sampled velocity — a 13 m strike attributed 92.3 m); shot-speed scenario windows 9 → 18 min/seed; the P1 observer-neutrality window 6 000 → 8 000 ticks against this pass's measured first-restart shift (~3 900 → 7 270). Predicates and bounds unchanged throughout. |
#endregion
