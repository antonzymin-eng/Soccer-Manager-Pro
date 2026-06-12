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

## Open quarantine entries — by suite

### Ball Physics #1 (1)

| Test | First-run failure | Hypothesis |
|---|---|---|
| `CrossbarCollision_DeflectsVelocityDownward` | Crossbar impact should deflect velocity downward (negative Z) | Crossbar deflection model or test geometry; same family as the AR-7 Z-up normal class. Needs §3.1.9 derivation. |

### Collision System #3 (1)

| Test | First-run failure | Hypothesis |
|---|---|---|
| `SpatialHash_TwoAgentsFar_QueryReturnsOnlySelf` | SH-003: distant agent must not appear in query | Broad-phase query radius vs test distance; check 3×3 window proof (AR-10) against the test's cell geometry. |

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

### Defensive AI #14 (4/4) — resolved June 12, 2026 (1 PRODUCTION-DEFECT + 3 TEST-DEFECT)

| Test | Verdict | Adjudication |
|---|---|---|
| `T_DA_011_NoCandidatesProducesZonal` | PRODUCTION-DEFECT | §3.3.3 Step 6 commits a ZONAL fallback DIRECTLY (`assignments[agent] = …; continue`) — only non-ZONAL candidates route through the Step 7 hysteresis gate. `MarkAssigner.Assign` routed ZONAL through `ApplyGate`, so the `MakeZonal` candidate (target −1) was dwell-held against the default-constructed record (target 0) and no valid ZONAL record ever published. ZONAL now bypasses the gate per spec (hysteresis untouched on the bypass path, matching the Step 6 pseudocode). `MarkAssigner.cs` v1.2. |
| `T_DA_012_TieBreakEntityIdAscending` | TEST-DEFECT (dwell ignored) | The FR-DA-014 tie-break itself is correct in `IsBetter()` (terminal `newId < curId`). The test's single `Assign` call asserted against §3.3.3 Step 7 / §3.11.3: a non-ZONAL candidate commits only after MARK_DWELL_TICKS = 4 consecutive preferences — even from the initial ZONAL state (the §3.11.6 worked example starts from an already-locked assignment; the algorithm text makes no first-assignment exception). Rewritten as a 4-tick dwell loop; 201 wins as specified. |
| `T_DA_016_InterceptRunnerDirectionCheck` | TEST-DEFECT (dwell ignored) | No direction-sign defect: the own-goal direction predicate (`dot(velNorm, defendsX0 ? (−1,0) : (+1,0)) > 0`) is team-relative and correct. Same single-call-vs-dwell defect as T-DA-012; rewritten as dwell loops for both the toward and away arms. |
| `T_DA_034_StepDepthMaxWithShapeLineDepth` | TEST-DEFECT (counter-spec state model) | §2.2.5: "currentLineDepth is updated each tick by reading #12's DefensiveLineDepth (#14 does not compute this value)" — the test's injected `CurrentLineDepth = 35` is legitimately overwritten by the mirror (40) before §3.7.4 runs, so the spec answer is 40 + OFFSIDE_STEP_SIZE_M = **43.0**, exactly what production returned. The §3.7.4 `max(currentLineDepth+step, shape.DefensiveLineDepth)` arm is unreachable while §2.2.5 keeps both operands identical (step > 0) — noted in the test as a locked vacuity; the test now asserts the mirror semantics (divergent injected state must NOT leak into the step). |

Files: `src/defensive-ai/MarkAssigner.cs` v1.2, `src/defensive-ai/Tests/DefensiveAITests.cs` v1.3. Suite: 51 passed / 0 failed / 28 skipped.

### Perception System #7 (4/4) — resolved June 12, 2026 (1 PRODUCTION-DEFECT root cause fixing 2 tests; 2 TEST-DEFECT)

| Test | Verdict | Adjudication |
|---|---|---|
| `Constants_DerivedFovValues_AreCorrect` | PRODUCTION-DEFECT (static-init order) | `BASE_FOV_HALF_ANGLE` ([DERIVED]) and `PERIPHERAL_ARC_INNER_BOUND` ([DERIVED]) are declared in the `#region Derived` block, which the FR-CS catalogue layout places textually BEFORE their `[GT]` source `BASE_FOV_ANGLE`; C# initialises static fields in textual order, so both readonly fields captured 0. Same defect class as the June-12 EventRegistry static-init finding. Converted to expression-bodied properties (evaluation deferred past static init); values unchanged (80° / 40°). `PerceptionConstants.cs` v1.2. |
| `PeripheralArc_Boundaries_AreCorrect` | (fixed by the same root cause) | The arc inner bound read 0°, so 40° was never "at the inner bound". FovCalculator's `>=`/`<=` inclusivity matches §3.3.3 ([40°, 80°] closed interval); no code change beyond the constants fix. |
| `OCC005_MinShadowFloor_NotActive_At2m` | TEST-DEFECT (trig mis-derivation) | Spec §A.4 / Appendix B mandate θ = arcsin(r/d): arcsin(0.4/2.0) = 11.537° (both appendix worked examples print 11.54°/11.537°). The test's 11.31° is arctan(0.2) — tangent used where the tangent-line geometry derives a sine. Production returned 11.536959° — exactly the spec value. Expectation corrected to 11.537° ± 0.05. |
| `LR001_ProcessVisible_ConfirmsAfterLrecTicks` | TEST-DEFECT (noise term ignored) | §3.3.4: L_rec_final = Min(L_rec_base + noise, L_MAX) with deterministic additive noise ∈ {0, +1} = DeterministicHash(obs, tgt, frame) % 2. At D=20, L_rec_base = 1 but for (0, 1, frame 0) the hash is odd → L_rec_final = 2, and the spec REQUIRES the first call to return false. Test asserted unconditional first-call confirmation. Rewritten to derive L_rec_final via ComputeLRec and assert confirmation exactly at the L_rec-th tick (false strictly before). |

Files: `src/perception-system/PerceptionConstants.cs` v1.2, `src/perception-system/Tests/PerceptionSystemTests.cs` v1.2. Suite: 46 passed / 0 failed / 15 skipped. Cross-suite note: the constants were 0 at runtime for ALL consumers — FovCalculator peripheral/blind-side predicates effectively classified everything ≥ 0° separation as outside the peripheral arc; downstream pressing-ai/decision-tree suites re-verified in the full gate run.

### First Touch #4 (4/4) — resolved June 12, 2026 (all TEST-DEFECT; production matches normative §3)

| Test | Verdict | Adjudication |
|---|---|---|
| `ControlQuality_AllFourVerificationMatrixScenarios_PassSimultaneously` | TEST-DEFECT (stale spec copy) | §5.2 CQ-012 Scenario 2 band 0.55–0.65 predated the unconditional §3.1.1 Step-5 movement difficulty; the normative §3.1.3 matrix row was corrected to ≈0.48–0.55 on 2026-05-26 (section-3-1-to-3-5.md v1.3) but the §5.2 copy never synced (parallel-surface drift, ERR-004-006 family). Hand-calc: WA = 0.7×12 + 0.3×11 = 11.7 → NormAttr 0.585; VelDiff = 15/15 = 1.0; MoveDiff = 1 + (2/7)×0.5 = 1.142857 → q = 0.511875 — exactly what production returns. Spec §5.2 synced (section-5-1-to-5-6.md v1.5); test rebanded. |
| `TouchRadius_GoodPoorBoundary_IsContinuous` | TEST-DEFECT (probe mis-construction) | §5.3 TR-004 prescribes DIRECT q probes (0.6001/0.5999). The pipeline form (attrs Tech=FT=12, agent 0.05 vs 0.20 m/s) put BOTH probes inside the Poor band (q ≈ 0.59786/0.59155 — MoveDiff ≥ 1 keeps q strictly below 0.60) and measured Poor-band slope (dr/dq = −2.4 m over Δq ≈ 0.00632 → 0.01516 m), not a discontinuity. Hand-calc at the true boundary: r(0.6001) = 0.59988 m, r(0.5999) = 0.60024 m, gap 0.00036 m — continuous. Rewritten to internal TouchRadiusCalculator probes + shared-boundary asserts (0.60 m). |
| `TouchRadius_PoorHeavyBoundary_IsContinuous` | TEST-DEFECT (probe mis-construction) | Same family: pipeline probes landed at q ≈ 0.19845/0.19184 — deep inside the Heavy band, nowhere near 0.35 — and measured Heavy-band slope amplified by the 20 m/s velocity modifier (×1.08333) → 0.01638 m. Hand-calc at the boundary: r(0.3501) = 1.19976 m, r(0.3499) = 1.20023 m, gap 0.00047 m — continuous. Rewritten to direct probes + shared-boundary asserts (1.20 m). |
| `TouchRadius_VelocityModifier_IncreasesRadiusForFastBall` | TEST-DEFECT (q not held constant) | §5.3 TR-009 holds q = 0.72 constant (r = 0.456 m at 15 m/s; ×1.25 → 0.570 m at 30 m/s). The pipeline form let §3.1.1 Step-4 velocity difficulty halve q at 30 m/s (0.735 → 0.3675, different band), so r30 = r_base(q30)×1.25 ≠ r15×1.25 (observed 1.643 vs expected 0.5475). Rewritten to the spec's direct probe; production formula verified: VelMod = 1 + (15/15)×0.25 = 1.25. |

Files: `src/first-touch/Tests/FirstTouchTests.cs` v1.3, `docs/specs/first-touch/section-5-1-to-5-6.md` v1.5. Suite: 71 passed / 0 failed / 8 skipped; cross-spec scenario suite (testing-strategy) re-run green.

## Version History

| Version | Date | Author | Notes |
|---|---|---|---|
| 1.0 | 2026-06-12 | — | Initial ledger: 30 entries from the first-ever full suite execution on the new dotnet CI gate. |
