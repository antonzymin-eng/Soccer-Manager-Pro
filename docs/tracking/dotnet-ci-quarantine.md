# Dotnet CI Gate — Quarantine Ledger

> **Created:** June 12, 2026
> **Version:** 1.0
> **Purpose:** Human-readable tracking for every test quarantined from the
> non-certifying Linux compile/test gate (`tools/dotnet-ci/run-gate.sh`). The
> machine-readable mirror is `tools/dotnet-ci/known-failures.txt` — the two MUST
> stay in sync (CI excludes exactly the names in that file).

## Provenance

On **June 12, 2026** the project's NUnit suites executed for the first time in
project history (see `tools/dotnet-ci/README.md` for why no suite had ever run:
seven assemblies carried structural compile errors). After the build-blocking
defects were fixed, 1,165 tests ran: the 30 below failed. They are **not flaky
and not shim artifacts** — each encodes a genuine open question between the
implementation and the test's expectation (the "running wrong tests instead of
dead ones" class predicted when the gate was proposed). Each needs a per-spec
AR-style derivation: decide whether the production model or the test
expectation is wrong, fix that side, verify with a numerical mirror where
applicable, and **remove the quarantine line in the same commit**.

## Rules

1. A test failure NOT in the ledger fails the gate — quarantine only grows via
   a PR that adds BOTH the `known-failures.txt` line and a row here with a
   defect hypothesis.
2. Fixing a test = removing its line from `known-failures.txt` + flipping its
   row to RESOLVED here (or deleting it) in the same commit.
3. The report-only step of `run-gate.sh` re-runs the quarantined set on every
   CI run, so a quarantined test that *starts passing* is visible in the log —
   harvest those promptly.

## Open quarantine entries (30) — by suite

### Ball Physics #1 (1)

| Test | First-run failure | Hypothesis |
|---|---|---|
| `CrossbarCollision_DeflectsVelocityDownward` | Crossbar impact should deflect velocity downward (negative Z) | Crossbar deflection model or test geometry; same family as the AR-7 Z-up normal class. Needs §3.1.9 derivation. |

### Collision System #3 (1)

| Test | First-run failure | Hypothesis |
|---|---|---|
| `SpatialHash_TwoAgentsFar_QueryReturnsOnlySelf` | SH-003: distant agent must not appear in query | Broad-phase query radius vs test distance; check 3×3 window proof (AR-10) against the test's cell geometry. |

### First Touch #4 (4)

| Test | First-run failure | Hypothesis |
|---|---|---|
| `ControlQuality_AllFourVerificationMatrixScenarios_PassSimultaneously` | CQ-012 Scenario 2 lower bound | §5 verification-matrix hand-calcs vs implementation; ERR-004-006 (additive-vs-multiplicative modifier) family — matrix may need re-derivation. |
| `TouchRadius_GoodPoorBoundary_IsContinuous` | TR-004: continuity at Good/Poor boundary | Piecewise touch-radius formula has a step at the band boundary, or the test's continuity tolerance is wrong. §3.2.3. |
| `TouchRadius_PoorHeavyBoundary_IsContinuous` | TR-007: continuity at Poor/Heavy boundary | Same family as TR-004. |
| `TouchRadius_VelocityModifier_IncreasesRadiusForFastBall` | TR-009: 30 m/s should scale radius by ≈ 1.25 | Velocity-modifier formula/constant vs test's expected scale factor. |

### Pass Mechanics #5 (3)

| Test | First-run failure | Hypothesis |
|---|---|---|
| `PV003_Lofted_MidAttributes_40m_VelocityInRange` | PV-003: lofted mid-power 40 m velocity must be ≥ 14 m/s | Velocity-calculator output vs §3.1.4 profile windows; possibly the AR-9 L-5 through-ball underhit family extends to lofted profiles. |
| `VS001_EliteShortGroundPass_OutputsInRange` | VS-001: elite short pass velocity must be ≥ 11.0 m/s | §5 verification-scenario hand-calc vs implementation. |
| `VS003_ChipOverDefensiveLine_OutputsInRange` | VS-003: chip velocity must be ≤ 11.0 m/s | Same family as VS-001 (note VS-001/VS-003 bracket 11.0 from opposite sides — at least one hand-calc is mis-derived). |

### Shot Mechanics #6 (3)

| Test | First-run failure | Hypothesis |
|---|---|---|
| `BM002_NinetyDegreeRunUp_ReducesScore` | BM-002: 90° approach offset should reduce composite BMS | Run-up angle term in §3.7 body-mechanics score not penalising (or test's "noticeably reduced" threshold mis-set). |
| `SN003_OffCentre_ProducesNonZeroSidespin` | SN-003: OffCentre contact must produce non-zero sidespin (Z) | Sidespin output is 0.0 — §3.4.5 sidespin path likely never engages (dead branch or wrong axis); SN-004 corroborates. |
| `SN004_HigherTechnique_IncreasesSpinMagnitude` | SN-004: Technique=18 must out-spin Technique=5 | Same root cause as SN-003 (sidespin identically 0). |

### Perception System #7 (4)

| Test | First-run failure | Hypothesis |
|---|---|---|
| `Constants_DerivedFovValues_AreCorrect` | BASE_FOV_HALF_ANGLE must equal BASE_FOV_ANGLE / 2 | A `[DERIVED]` constant is not equal to its documented formula — catalogue drift; trivial to adjudicate against §3.10. |
| `PeripheralArc_Boundaries_AreCorrect` | 40.0° must be exactly at inner bound — inclusive | Arc-boundary inclusivity (`<` vs `<=`) in FovCalculator vs §3.1. |
| `OCC005_MinShadowFloor_NotActive_At2m` | OCC-005: arcsin(0.4/2)≈11.31° expected (±0.15°) | Shadow-cone min-floor activation distance vs §3.2.3 derivation. |
| `LR001_ProcessVisible_ConfirmsAfterLrecTicks` | LR-001 (D=20, L_rec=1): confirm on first ProcessVisible call | Off-by-one in latency-counter confirm (counts from 0 vs 1) vs §3.3. |

### Positioning AI #12 (6)

| Test | First-run failure | Hypothesis |
|---|---|---|
| `ContextModifier_ScoreDiff_ExpandsLateralSpread` | scoreDiff=+3 should expand lateral spread vs neutral | ContextModifier sign/keying — possibly inverted scoreDiff direction. The five positioning failures cluster on §3.5 modifier application; adjudicate together. |
| `Integration_OutOfPoss_NarrowerLateralSpread_Than_InPoss` | OutOfPoss must be narrower laterally than InPoss | Phase-keyed BaseLateral application order (modifier before/after spacing). |
| `TacticalCorrectness_4231_TransToAtk_AMAdvances_RelativeTo_OutOfPoss` | AM slot.x must advance in TransToAtk | Phase anchor/vertical-compactness path. |
| `TacticalCorrectness_OutOfPoss_DefensiveLineCompact` | OutOfPoss defense line must sit deeper (lower X) | Same §3.5 cluster. |
| `TacticalCorrectness_TransToDef_NarrowerLateralSpread` | TransToDef spread must be narrower than InPoss | Same §3.5 cluster. |
| `FailureMode_F3_NaNBallPosition_SlotsRemainFinite` | Slot 0.x must not be NaN after F3 guard | F3 NaN guard not sanitising ball position before anchor math — project NaN-gate pattern (FT AR-8 / AM AR-10 / CS AR-7) probably missing here. |

### Pressing AI #13 (1)

| Test | First-run failure | Hypothesis |
|---|---|---|
| `CoverShadow_AssignsHigherThreatReceiverFirst` | First shadow must cover the higher-threat receiver | Threat-ranking order vs greedy assignment in CoverShadowSelector §3.x. |

### Defensive AI #14 (4)

| Test | First-run failure | Hypothesis |
|---|---|---|
| `T_DA_011_NoCandidatesProducesZonal` | ZONAL assignment must have TargetEntityId = -1 | `MakeZonal` factory or assigner default leaves a stale/0 id. |
| `T_DA_012_TieBreakEntityIdAscending` | EntityId 201 (lower) must win tie-break per FR-DA-014 | `IsBetter()` tie-break comparison direction. |
| `T_DA_016_InterceptRunnerDirectionCheck` | Runner toward own goal must qualify INTERCEPT_RUNNER | Direction predicate sign — home/away asymmetry family (DT AR-2 root cause: all worked examples home-team). |
| `T_DA_034_StepDepthMaxWithShapeLineDepth` | StepUpTargetDepth must equal max(lineDepth+step, shapeLineDepth) = 40.0 | OffsideTrapController max() arm order/operand. |

### Heading Mechanics #10 (2)

| Test | First-run failure | Hypothesis |
|---|---|---|
| `ComputeHeadZ_AtApex_EqualsJumpReach` | Head Z at apex frame must equal jumpReach | KD-18 synthetic parabola apex sampling (frame quantisation vs exact apex) — implementation may never sample the exact apex frame. |
| `OwnGoalFlag_TrajectoryTowardOwnGoal_ReturnsTrue` | Trajectory at own goal must set own-goal flag | §3.8 HeadingPowerAngle own-goal bounding-box predicate; possible home/away or axis-sign defect (Decision Tree AR-2 asymmetry family). |

### Deterministic Sim #16 (1)

| Test | First-run failure | Hypothesis |
|---|---|---|
| `ReplayEngine_PrepareReplay_WellFormedSnapshot_ReturnsZero` | T-DS-008: PrepareReplay must return 0; returned 5640 (0x1608) | PrepareReplay step validation rejects the fixture's "well-formed" snapshot — either a validation step the fixture doesn't satisfy (fixture defect) or a step mis-ordered in ReplayEngine. Error code 0x1608 names the step; adjudicate against §4.2.2. |

## Resolved

*(none yet — entries move here with the commit hash that fixed them)*

## Version History

| Version | Date | Author | Notes |
|---|---|---|---|
| 1.0 | 2026-06-12 | — | Initial ledger: 30 entries from the first-ever full suite execution on the new dotnet CI gate. |
