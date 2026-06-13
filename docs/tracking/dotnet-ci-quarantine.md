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

### Positioning AI #12 (6)

| Test | First-run failure | Hypothesis |
|---|---|---|
| `ContextModifier_ScoreDiff_ExpandsLateralSpread` | scoreDiff=+3 should expand lateral spread vs neutral | ContextModifier sign/keying — possibly inverted scoreDiff direction. The five positioning failures cluster on §3.5 modifier application; adjudicate together. |
| `Integration_OutOfPoss_NarrowerLateralSpread_Than_InPoss` | OutOfPoss must be narrower laterally than InPoss | Phase-keyed BaseLateral application order (modifier before/after spacing). |
| `TacticalCorrectness_4231_TransToAtk_AMAdvances_RelativeTo_OutOfPoss` | AM slot.x must advance in TransToAtk | Phase anchor/vertical-compactness path. |
| `TacticalCorrectness_OutOfPoss_DefensiveLineCompact` | OutOfPoss defense line must sit deeper (lower X) | Same §3.5 cluster. |
| `TacticalCorrectness_TransToDef_NarrowerLateralSpread` | TransToDef spread must be narrower than InPoss | Same §3.5 cluster. |
| `FailureMode_F3_NaNBallPosition_SlotsRemainFinite` | Slot 0.x must not be NaN after F3 guard | F3 NaN guard not sanitising ball position before anchor math — project NaN-gate pattern (FT AR-8 / AM AR-10 / CS AR-7) probably missing here. |

## Resolved

### Singletons — Ball Physics #1, Collision #3, Pressing AI #13, Deterministic Sim #16 (4/4) — resolved June 13, 2026 (all TEST/FIXTURE-DEFECT; production faithful to spec)

| Test | Spec | Verdict | Adjudication |
|---|---|---|---|
| `CrossbarCollision_DeflectsVelocityDownward` | Ball Physics #1 §3.1.10.2 | FIXTURE-DEFECT | The fixture supplied vertical-**post** geometry (postCenter (105,34,2.44), contact (104.94,34,2.44) — X-offset only, equal Z) → `normal=(−1,0,0)`, which cannot touch Velocity.z (stays 5.0). A crossbar (§3.1.2) is a horizontal cylinder, lower edge at GOAL_HEIGHT 2.44 m, axis at 2.44 + POST_DIAMETER/2; an underside strike gives `normal=(0,0,−1)` → vz = −3.75 < 0. Fixture re-derived to real crossbar geometry; `BallCollision.cs` unchanged. `BallIntegrationTests.cs` v1.5. |
| `SpatialHash_TwoAgentsFar_QueryReturnsOnlySelf` | Collision #3 §3.1/§3.1.3 | FIXTURE-DEFECT | CellSize 1.0 m; the fixture placed both agents exactly on cell boundaries (x=50.0, 52.0). §3.1.3 mandates boundary-spread insertion (an agent on a boundary inserts into both adjacent cells — tested by SH-006), so agent at x=52.0 spreads into cells 51 and 52, and the 3×3 broad-phase query from cell 50 legitimately returns it from cell 51. The broad phase is candidate generation, not radius filtering (§3.2.3 — the caller filters). Positions moved off-boundary into genuinely non-adjacent cells (x=50.5, 53.5); `SpatialHashGrid.cs` unchanged. `CollisionSystemTests.cs` v1.4. |
| `CoverShadow_AssignsHigherThreatReceiverFirst` | Pressing AI #13 §3.4 | FIXTURE-DEFECT | The §3.4 threat skill term `(FirstTouch/20)·THREAT_SKILL_W` is monotonically increasing in FirstTouch; the fixture's comment ("weak skill maximises threat") inverted it. Hand-calc: id 51 (forward, FT=5) = 0.245, id 52 (lateral, FT=18) = 0.280 — so 52 correctly out-scored 51 and ranked first (the observed result). FirstTouch values swapped so id 51 is genuinely higher threat (0.375 vs 0.150); `CoverShadowSelector.cs` unchanged. `PressingAITests.cs` v1.2. |
| `ReplayEngine_PrepareReplay_WellFormedSnapshot_ReturnsZero` | Deterministic Sim #16 §4.2.2 | FIXTURE-DEFECT | Error 0x1608 = `ERR_DS_DIGEST_CHAIN_BREAK` at §4.2.2 step 4 (`ValidatePrevDigest`). The fixture called the recording-side `codec.Encode()`, advancing the codec's `_prevDigest` to the just-encoded digest D, then `PrepareReplay` compared D against the genesis snapshot's recorded `PrevSnapshotDigest` (all-zeros) → mismatch. A fresh `SnapshotCodec` already holds the genesis sentinel (all-zeros), which is exactly what a well-formed genesis snapshot chains to. Removed the `Encode()` call; `ReplayEngine.cs`/`SnapshotCodec.cs` unchanged. `DeterministicSimTests.cs` v1.4. |

All four were test-fixture defects (no production or spec change). Suites: ball-physics 83/83, collision-system 30/30 (+9 skipped), pressing-ai 44/44 (+26 skipped), deterministic-sim 37/37 (+4 skipped).



### Pass Mechanics #5 (3/3) — resolved June 13, 2026 (all TEST/SPEC; production faithful to normative §3)

The §5.11 NUnit suite was uncompilable from v1.1 until June 11 (stray-brace, AR-9 H-1), so its §5.12 validation-scenario expectations had never executed and were never re-derived against the §3 model — the First Touch ERR-004-006 family.

| Test | Verdict | Adjudication |
|---|---|---|
| `PV003_Lofted_MidAttributes_40m_VelocityInRange` | TEST/SPEC | §3.2.7 against the authoritative §3.1.4 Lofted profile (vOffset=9, vMax=22, distMax=60): powerScale = (10/20)·(40/60) = 0.3333 → V_base = 9 + 0.3333·(22−9) = 13.333 m/s (the "40/3" coincidence is 9 + 13/3). The ≥14.0 floor was transcribed from the stale §3.2.9 reference table (Lofted distMax=55) and is unreachable for distMax=60. Test lower bound 14.0→13.0; §5.3 expected band [14,20]→[13,20]; §3.2.9 stale-distMax warning. |
| `VS001_EliteShortGroundPass_OutputsInRange` | TEST/SPEC | Ground vOffset=8/vMax=18/distMax=30, KickPower proxy (19+17)/2=18: V_base = 8 + 0.24·10 = 10.4, ×0.98 = 10.192 m/s (matches runtime). The §5 ≈11.5 m/s is categorically unreachable for an 8 m Ground pass (caps ~10.45 at K=20). Latent second failure: §3.5.3 error = 1.05° vs the §5 ≤0.8° (also unreachable; elite Ground floors at 0.861°). Velocity band [11,12]→[9.5,11.0]; error ≤0.8°→≤1.1°. |
| `VS003_ChipOverDefensiveLine_OutputsInRange` | TEST/SPEC | Chip vOffset=6/vMax=14/distMax=20, KickPower proxy 15.5: V_base = 6 + 0.6975·8 = 11.58, ×0.96 = 11.117 m/s (matches runtime). §3.3.4 aerial angle θ = atan(4·4.5/18) = atan(1) = 45° (the §5 ≈55° needs apexChip≈6.4 m, inconsistent with the 4.5 m chip apex). Velocity band [10,11]→[10.5,11.5]; angle [50,60]→[44,56]. |

The VS-001/VS-003 "11.0 bracketed from opposite sides" paradox is resolved: corrected, the chip (11.12 m/s) correctly travels faster than the short ground pass (10.19 m/s). No production velocity/error/angle code changed. Files: `src/pass-mechanics/Tests/PassMechanicsTests.cs` v1.3, `docs/specs/pass-mechanics/section-5-1-to-5-12.md` v1.3, `section-5-13-to-5-16.md`, `section-3-2.md` v1.2. Suite: 80 passed / 0 failed / 12 skipped; cross-spec (testing-strategy incl. `lofted-pass-kick-bounce-roll`) 20/20.

### Heading Mechanics #10 (2/2) — resolved June 12, 2026 (1 PRODUCTION-DEFECT + 1 TEST-DEFECT)

| Test | Verdict | Adjudication |
|---|---|---|
| `ComputeHeadZ_AtApex_EqualsJumpReach` | PRODUCTION-DEFECT | §3.3 (outline-detailed) specifies the synthetic trajectory "parabolic interpolation peaking at apexFrame with peak value JumpReach_m". `ComputeHeadZ` parameterised u = offset/totalPhaseFrames, peaking at the continuous midpoint: totalPhaseFrames = round(650/16.667) = 39, peak at offset 19.5. But `ComputeApexFrame` rounds the apex to offset 20 (banker's rounding of 19.5), so u(apex) = 20/39 = 0.5128 → headZ = 2.65·4·0.5128·0.4872 = 2.6483 m, not 2.65. Fixed by time-warping u (rising span [0, apexOffset], falling span [apexOffset, total]) so u = 0.5 lands exactly on the rounded apexFrame; endpoints (0 at start/landing) and monotonicity preserved. `HeadingJumpKinematics.cs` v1.2. |
| `OwnGoalFlag_TrajectoryTowardOwnGoal_ReturnsTrue` | TEST-DEFECT | `ComputeOwnGoalFlag`'s ballistic projection (with gravity, §3.8) is correct: the test's outgoing velocity (−14, 0, 1) from (15, 34, 1.5) grounds out — z(t) = 1.5 + t − 4.905t² = 0 at t ≈ 0.66 s / x ≈ 5.7 m — and reaches the goal line x = 0 only at t ≈ 1.07 s / z ≈ −3.05 m, so the ball never enters the goal box and the predicate correctly returned false. The test's "moderate Z keeps it in goal height range" premise is unphysical. vz raised 1.0 → 5.0: z(1.07 s) ≈ 1.23 m ∈ [0, 2.44] at x = 0, y = 34 — a valid own-goal trajectory. `Tests/HeadingMechanicsTests.cs` v1.2. |

Files: `HeadingJumpKinematics.cs` v1.2, `Tests/HeadingMechanicsTests.cs` v1.2. Suite: 45 passed / 0 failed / 15 skipped.

### Shot Mechanics #6 (3/3) — resolved June 12, 2026 (2 PRODUCTION-DEFECT root causes)

| Test | Verdict | Adjudication |
|---|---|---|
| `SN003_OffCentre_ProducesNonZeroSidespin` | PRODUCTION-DEFECT (Z-up axis) | `ShotSpinCalculator.Compute` assembled sidespin on `Vector3.up` = Unity +Y, which in this Z-up project (#1 §1.2) is the **touchline** axis. For a +X-facing shooter the topspin/backspin `left` vector is also ±Y, so sidespin collided with forward spin and the vertical (Z) sidespin component was identically 0. Axis changed to `(0,0,1)` (project vertical); OffCentre contact now produces real lateral spin. `ShotSpinCalculator.cs` v1.3. |
| `SN004_HigherTechnique_IncreasesSpinMagnitude` | (same root cause) | Technique scaled a magnitude that was being zeroed; fixed by the same axis correction. No test change. |
| `BM002_NinetyDegreeRunUp_ReducesScore` | PRODUCTION-DEFECT (deadband vs ramp) | `BodyMechanicsEvaluator.ComputeRunUpScore` used a deadband `1−max(0,dev−tol)/tol` (full score for any deviation ≤ 45°), contradicting §3.7.3's boundary checks (dev=22.5°→0.5, dev=45°→0, 90° approach→0). A 90° approach scored 0.83 (composite 0.958) where the spec requires 0 (composite 0.75). Reverted to the §3.7.3 linear ramp `1−Clamp01(dev/tol)`. The v1.2 history row that introduced the deadband falsely claimed it "matches §3.7.3". Test companions (TEST-DEFECT, same commit): BM-002 +Z "approach" → +Y in-plane 90°, assertion re-derived to the 0.75 no-run-up ceiling; BM-005 moving agent moved straight-on → the ideal 37.5° angle (the only fair "ideal approach" under the ramp — straight-on run-up 0.1667 sits below the stationary neutral 0.5); BM-001 comment corrected. `BodyMechanicsEvaluator.cs` v1.6, `Tests/ShotMechanicsTests.cs` v1.3. |

Files: `ShotSpinCalculator.cs` v1.3, `BodyMechanicsEvaluator.cs` v1.6, `Tests/ShotMechanicsTests.cs` v1.3. Suite: 79 passed / 0 failed / 12 skipped. Cross-suite: ShotExecutor feeds BodyMechanicsScore into the contact-quality modifier; the run-up change shifts BMS for off-ideal approaches — full gate re-run confirms no downstream shot/decision-tree expectation regressed.

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
