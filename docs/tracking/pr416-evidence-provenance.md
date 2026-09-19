# PR #416 Durable Evidence Provenance

> **Created:** September 18, 2026
> **Purpose:** Preserve the exact code/workflow provenance of the PR #416 investigation before
> disposable `evidence/pr416-*` refs are removed.
> **Scope:** Evidence provenance only. This file does not alter or reinterpret the PR #416
> production fix, owner-held close-chance disposition, or the two post-#416 open residuals.

## 1. Stable anchors

PR #416 landed by true merge at
`e8207f4c6f4d9d301872da869e3796163b8b26ad`.

The investigation used three recurring repository anchors:

| Role | Exact SHA |
| --- | --- |
| Pinned main / W2-active reference | `1bad655f5826070d1e29f54a845cdf2c549f66bc` |
| Reconciled #416 diagnostic control | `07dcc670fed1ecc6aa32e9711d4bc8830c0ec2ad` |
| Final code/spec/test head used by final PR CI | `b6a21d2d303baae108e77953719130a440f8f686` |

The final PR CI run `35307891032` executed GitHub's synthetic pull-request merge commit
`e64322a67c1b37dddea145fcd73d2e2076fccb6a`, which merged
`b6a21d2d303baae108e77953719130a440f8f686` into
`1bad655f5826070d1e29f54a845cdf2c549f66bc`. The synthetic merge SHA is therefore
execution provenance, not a substitute for the exact PR-side head.

## 2. Causal lattice arms

The table records the exact evidence head used by the run where the run checked out an evidence
branch directly. Where a workflow checked out an explicit target SHA and then applied a temporary
patch, the exact target SHA and patch identity are recorded instead.

| Arm | Evidence branch | Exact arm/workflow SHA | Parent/reference SHA | Workflow run | Result / defining delta |
| --- | --- | --- | --- | --- | --- |
| Healthy pinned-main control | `evidence/pr416-main-focused-census` | `83a699f9424e81ed98e603698a20f5c348030721` at run time; branch later advanced to `53a8928d3a73fdf66ec3deac663ce541734dd390` | `1bad655f5826070d1e29f54a845cdf2c549f66bc` | `35224582571` | All three original focused regressions passed. |
| ApplyKick-height clause removed from #416 | `evidence/pr416-applykick-height-ablation` + `evidence/pr416-applykick-height-focused` | code head `b00fd8028674f4acf84dc6a3c54de5e875275ae5`; run/workflow head `1e1cc40223e375d18f18ca0cacce559c1ea7145a` | `07dcc670fed1ecc6aa32e9711d4bc8830c0ec2ad` | `35182220948` | All three targets remained red; exact ApplyKick delta is preserved in §3.1. |
| Pre-force normalization removed from #416 | `evidence/pr416-pre-force-normalization-ablation` | code head `d2c09f0979f28403d37a20b470ff9eebec597d3f`; run/workflow head `2df4121f1ba23c13cd1c7372cdb84b1bba4f4806` | `07dcc670fed1ecc6aa32e9711d4bc8830c0ec2ad` | `35182424557` | All three targets remained red; exact removed block is preserved in §3.2. |
| ReleaseBallControl clause removed from #416 | `evidence/pr416-no-release-control-focused` | `7c7f5bd63835600ddd9999388ed161e62f569ee4` | `07dcc670fed1ecc6aa32e9711d4bc8830c0ec2ad` | `35278313267` | Tackle outcomes/dispossession and shot remained red; exact runtime patch is preserved in §3.3. |
| Stationary-promotion clause removed from #416 | `evidence/pr416-no-stationary-focused` | `7f5f47fdc1a33d905257c7d23c15cf8385472cc8` | `07dcc670fed1ecc6aa32e9711d4bc8830c0ec2ad` | `35272599047` | All three targets remained red. Resulting BallStateMachine blob: `ecf326ecab55459b25c2b7fd29220887aabb40bf`. |
| Rolling-order clause removed from #416 | `evidence/pr416-no-rolling-focused` | `9902a8a4a8dde13666a2997fa36decf6abeb3eb1` | `07dcc670fed1ecc6aa32e9711d4bc8830c0ec2ad` | `35272651902` | All three targets remained red. Resulting BallStateMachine blob: `3297dd5a90c8881c510fa1869d34821d395f2a7f`. |
| Stationary-promotion clause alone on main-like behavior | `evidence/pr416-stationary-state-focused` | `3d4854517ef5b128516f21acdc981564f2ab57c9` | `1bad655f5826070d1e29f54a845cdf2c549f66bc` | `35272069065` | All three targets passed. BallStateMachine blob: `0d9e049cdcd6d86f6ed2945ad04e3eca2577ede3`. |
| Rolling-order clause alone on main-like behavior | `evidence/pr416-rolling-order-focused` | `ce5bb0f1eb66e3b55af82c65e8c1800250c168f4` | `1bad655f5826070d1e29f54a845cdf2c549f66bc` | `35271049186` | Both tackle targets passed; shot target failed. BallStateMachine blob: `57fd2284335784eb09870fb94047612012c1cf2e`. |
| Exact StateMachine pair alone on main-like behavior | `evidence/pr416-state-machine-pair-focused` | `1875a6cffc25cb68ef9df577898e9e3ce8701c5d` | `1bad655f5826070d1e29f54a845cdf2c549f66bc` | `35272495848` | Both tackle targets passed; shot target failed. BallStateMachine blob equals full #416 control: `8aa64f36f3f48e4be2d4a9377086e7dca82ca13d`. |
| Complete five-clause production union reverted to pinned-main behavior | `evidence/pr416-production-union-revert` | code commit `6d18442420ae77da0bfbcb26bacc9ec9ba1587d3`; workflow head `e28239b25bea5df190a38aefc099a2e029d47bfa` | `07dcc670fed1ecc6aa32e9711d4bc8830c0ec2ad`, with production semantics restored to `1bad655f5826070d1e29f54a845cdf2c549f66bc` | `35270645862` | All three original targets passed. This is the corrected union control; the three production files are the defining delta. |
| Attempted StateMachine-pair removal from #416 | `evidence/pr416-no-state-machine-pair` | `48d19ceb40216a78479f3fbf7c71856c6f0f128c` | #416 `07dcc670fed1ecc6aa32e9711d4bc8830c0ec2ad`; intended equality target `1bad655f5826070d1e29f54a845cdf2c549f66bc` | `35283014828` | **VOID.** `Verify pair-removal diff` failed before focused tests; the separate residency job did not apply the ablation. |

The nine valid perturbation rows (rows 2–10) are the production-delta lattice. They establish the
negative result already recorded in `w6-elevated-stationary-ball-fix.md`: no one-clause removal
from full #416 restored the three-regression set; the complete union revert did.

## 3. Exact defining production deltas

These snippets preserve the materially significant code changes so deletion of an evidence branch
does not erase what the arm actually represented. Blob SHAs are supplied where a whole-file identity
is useful for reconstruction.

### 3.1 ApplyKick-height clause

Full #416 control, `BallCollision.cs` blob
`d58e69016875df3ba360b1364bf5d5a85e9b5766`:

```diff
- if (velocity.z > 0f)
+ if (ball.Position.z > BallPhysicsConstants.State.AirborneEnterThreshold || velocity.z > 0f)
    ball.State = BallStateType.Airborne;
```

The ablation arm restores the left-hand form. Its resulting `BallCollision.cs` blob is
`9d990db8463cb6fb800633cd777d529fbad9b63c`; pinned-main `BallCollision.cs` blob is
`18e2ae28e59b0523a34dcfdbe26dc673ab4bcc57`.

### 3.2 Pre-force normalization clause

Full #416 control, `BallPhysicsCore.cs` blob
`e6df5a1be442d5ffb1ab6f6153417a758253e4c6`, contains this block before force selection:

```csharp
if ((ball.State == BallStateType.Stationary || ball.State == BallStateType.Rolling)
    && ball.Position.z > BallPhysicsConstants.State.AirborneEnterThreshold)
{
    ball.State = BallStateType.Airborne;
}
```

The ablation removes exactly that block. Its resulting `BallPhysicsCore.cs` blob is
`5fbdb6b44b4f623311e122f72a1b4ccf20e4b228`.

### 3.3 ReleaseBallControl clause

The #416 form under test chooses Airborne for an elevated controlled ball at release:

```csharp
ball.State = ball.Position.z > BallPhysicsConstants.State.AirborneEnterThreshold
    ? BallStateType.Airborne
    : BallStateType.Stationary;
```

The ablation workflow on `evidence/pr416-no-release-control-focused` replaces that exact statement
with:

```csharp
ball.State = BallStateType.Stationary;
```

The workflow's marker guard is part of the evidence: failure to find the exact #416 form fails the
arm rather than silently testing a different patch.

### 3.4 StateMachine Stationary and Rolling clauses

Pinned main `1bad655f…`, `BallStateMachine.cs` blob
`e9686123394aff4f4dd5cacd6046a081f0156cda`, has:

```csharp
case BallStateType.Stationary:
    return BallStateType.Stationary;

case BallStateType.Rolling:
    if (ball.Velocity.magnitude < BallPhysicsConstants.State.MinVelocity)
        return BallStateType.Stationary;
    if (ball.Position.z > BallPhysicsConstants.State.AirborneEnterThreshold)
        return BallStateType.Airborne;
```

Full #416 `07dcc670…`, blob
`8aa64f36f3f48e4be2d4a9377086e7dca82ca13d`, has:

```csharp
case BallStateType.Stationary:
    if (ball.Position.z > BallPhysicsConstants.State.AirborneEnterThreshold)
        return BallStateType.Airborne;
    return BallStateType.Stationary;

case BallStateType.Rolling:
    if (ball.Position.z > BallPhysicsConstants.State.AirborneEnterThreshold)
        return BallStateType.Airborne;
    if (ball.Velocity.magnitude < BallPhysicsConstants.State.MinVelocity)
        return BallStateType.Stationary;
```

Blob SHAs in this record identify the historical Git objects; they are not the durability
mechanism and are not assumed to remain dereferenceable after evidence refs are deleted. The
material one-clause variants are therefore preserved below as executable switch fragments.

**Stationary-promotion removed from full #416** — blob
`ecf326ecab55459b25c2b7fd29220887aabb40bf`:

```csharp
case BallStateType.Stationary:
    return BallStateType.Stationary;

case BallStateType.Rolling:
    if (ball.Position.z > BallPhysicsConstants.State.AirborneEnterThreshold)
        return BallStateType.Airborne;
    if (ball.Velocity.magnitude < BallPhysicsConstants.State.MinVelocity)
        return BallStateType.Stationary;
    if (IsOutOfBounds(ball.Position))
        return BallStateType.OutOfPlay;
    return BallStateType.Rolling;
```

**Rolling-order removed from full #416** — blob
`3297dd5a90c8881c510fa1869d34821d395f2a7f`:

```csharp
case BallStateType.Stationary:
    if (ball.Position.z > BallPhysicsConstants.State.AirborneEnterThreshold)
        return BallStateType.Airborne;
    return BallStateType.Stationary;

case BallStateType.Rolling:
    if (ball.Velocity.magnitude < BallPhysicsConstants.State.MinVelocity)
        return BallStateType.Stationary;
    if (ball.Position.z > BallPhysicsConstants.State.AirborneEnterThreshold)
        return BallStateType.Airborne;
    if (IsOutOfBounds(ball.Position))
        return BallStateType.OutOfPlay;
    return BallStateType.Rolling;
```

**Stationary-promotion alone on main-like behavior** — blob
`0d9e049cdcd6d86f6ed2945ad04e3eca2577ede3`:

```csharp
case BallStateType.Stationary:
    if (ball.Position.z > BallPhysicsConstants.State.AirborneEnterThreshold)
        return BallStateType.Airborne;
    return BallStateType.Stationary;

case BallStateType.Rolling:
    if (ball.Velocity.magnitude < BallPhysicsConstants.State.MinVelocity)
        return BallStateType.Stationary;
    if (ball.Position.z > BallPhysicsConstants.State.AirborneEnterThreshold)
        return BallStateType.Airborne;
    if (IsOutOfBounds(ball.Position))
        return BallStateType.OutOfPlay;
    return BallStateType.Rolling;
```

**Rolling-order alone on main-like behavior** — blob
`57fd2284335784eb09870fb94047612012c1cf2e`:

```csharp
case BallStateType.Stationary:
    return BallStateType.Stationary;

case BallStateType.Rolling:
    if (ball.Position.z > BallPhysicsConstants.State.AirborneEnterThreshold)
        return BallStateType.Airborne;
    if (ball.Velocity.magnitude < BallPhysicsConstants.State.MinVelocity)
        return BallStateType.Stationary;
    if (IsOutOfBounds(ball.Position))
        return BallStateType.OutOfPlay;
    return BallStateType.Rolling;
```

The pair-alone arm uses the full-#416 switch fragment above (blob
`8aa64f36f3f48e4be2d4a9377086e7dca82ca13d`). These embedded fragments, together with the
parent/reference identities in §2, preserve the materially significant source differences without
depending on future reachability of the evidence refs.

### 3.5 Complete union revert

The valid union arm is commit `6d18442420ae77da0bfbcb26bacc9ec9ba1587d3` on top of
`07dcc670…`, with workflow wrapper `e28239b2…`. It restores the complete ERR-001-006 production
delta to pinned-main behavior across:

- `src/ball-physics/BallCollision.cs`;
- `src/ball-physics/BallPhysicsCore.cs`;
- `src/ball-physics/BallStateMachine.cs`.

This arm is not shorthand for “undo some Ball Physics changes”; those three files, the code commit,
and pinned-main reference above define the exact patch. At `6d184424…`, all three resulting source
blobs are byte-identical to their pinned-main `1bad655f…` counterparts.

## 4. Measurement, mechanism, and candidate arms

| Arm | Evidence branch | Exact branch/head SHA | Parent/reference SHA | Workflow run | Durable interpretation |
| --- | --- | --- | --- | --- | --- |
| Controlled residency main vs #416 | `evidence/pr416-controlled-residency` | `33f3cbb530d42196300a84ad9f59b85abcdee1c6` | matrix refs `1bad655f…` and `07dcc670…` | `35278109980` | 150,000-tick two-seed residency; jobs `105393487824` / `105393488091`. Exact shares are already durable in the diagnosis file. |
| Mechanism census main vs #416 | `evidence/pr416-mechanism-census` | branch later evolved to `b2079876c8daec0a90b29a837e3285d170ad093b`; run checked out exact matrix refs | matrix refs `1bad655f…` and `07dcc670…` | `35284452456` | Per-seed tackle census plus longest Controlled spell. Exact 97,682-tick HOLD record is already durable in the diagnosis file. |
| Co-location perception runtime probe | `evidence/pr416-colocated-ball-visible` | current branch head `a23683568206423275c80e639f54ea0c80d2d36f` | runtime target refs `1bad655f…` and `07dcc670…` | `35285144898`, strict target confirmation `35285560809` | Temporary patch bypassed FoV angle only for exact XY co-location. First run emitted usable census but failed later on unrelated FM-03 log policing; second supplies strict target evidence. Production form later landed at `3a4a228495b73057684c727a55973c5072d7fdc1`. |
| Rolling + pre-force pair ablation | `evidence/pr416-mechanism-census` | `8784eb235757e9707aaa1b69872cb20324bc6633` | runtime base `07dcc670…`; workflow sources `evidence/pr416-no-rolling-focused` and `evidence/pr416-pre-force-normalization-ablation` | `35293636125` | Runtime composition was `BallStateMachine.cs` blob `3297dd5a90c8881c510fa1869d34821d395f2a7f` + `BallPhysicsCore.cs` blob `5fbdb6b44b4f623311e122f72a1b4ccf20e4b228` + `BallCollision.cs` blob `d58e69016875df3ba360b1364bf5d5a85e9b5766`. Mechanism census passed; focused regression failed the strict five-target TRX outcome enforcement, so this pair did not restore the required target set. |
| Narrow Rolling candidate | `evidence/pr416-narrow-rolling-candidate` | `bb501a2128f9efbef5e98bffadb0d98214493e78` | rooted at `07dcc670…` | `35300926450` | Branch tree is `BallStateMachine.cs` blob `e739a3061c1b74faa7ac71a67da8b20d3690fed8` + `BallPhysicsCore.cs` blob `ce945b5736125f9ea9264632442617155e6b5af4` + `BallCollision.cs` blob `d58e69016875df3ba360b1364bf5d5a85e9b5766`: elevated Rolling promotion/pre-force recovery is narrowed to the low-speed stop path. Ball-physics, widened-corpus, and corrected-baseline-detector jobs passed; focused regression failed main-relative target enforcement. The workflow also locked the known broad high-speed Rolling contract conflict as an expected failure. |
| State-only pre-force candidate | `evidence/pr416-state-only-preforce-candidate` | `6c89365da2abdd2d3ff697da34a55751edeb571f` | rooted at `07dcc670…` | `35301715589` | Branch tree is `BallStateMachine.cs` blob `4511b1cbace009175308b7ce32ec22a93d2b6375` + `BallPhysicsCore.cs` blob `b51e772bc4c9e8dd3f414d2ae372a6591294e7eb` + `BallCollision.cs` blob `d58e69016875df3ba360b1364bf5d5a85e9b5766`. Core pre-force recovery is Stationary-only while the state machine prevents a low-speed elevated Rolling ball from becoming Stationary. Focused-regression, ball-physics, widened-corpus, and corrected-baseline-detector jobs all passed. The `ball-physics` job is green because it explicitly requires the broad high-speed `Rolling_AboveEnterThreshold_TransitionsToAirborne` lock to fail as the known contract conflict; this candidate restores the focused targets while narrowing elevated-Rolling semantics rather than satisfying that broad lock. |
| Low-speed Rolling isolation | `evidence/pr416-state-only-preforce-candidate` | `efa2f8946a9a6a8852946b97a8e4c7d55013b0bf` | candidate lineage through `6c89365d…`; runtime StateMachine sourced from `evidence/pr416-no-rolling-focused` | `35302289613` | Workflow replaced the branch StateMachine with blob `3297dd5a90c8881c510fa1869d34821d395f2a7f` while retaining `BallPhysicsCore.cs` blob `b51e772bc4c9e8dd3f414d2ae372a6591294e7eb` and `BallCollision.cs` blob `d58e69016875df3ba360b1364bf5d5a85e9b5766`. Isolation job passed; D1A6 longest Controlled spell was 1,064 ticks. |
| Committed perception-fix focused validation | `evidence/pr416-final-validation` | `b2ea41e78ba9d77b339b76c7fb24f41f8df0d0d2` | production/test correction through `f92305b58cd8a8dc69d9b95e94d2bda04aa56201` | `35286222621` | Focused validation green; job `105419040400`. |
| Live-head closure | helper surfaces on `fix/w6-elevated-stationary-ball`; `evidence/pr416-current-head-validation` remains as a wrapper branch | wrapper branch current head `9a7755f9422c07c04207d543512c3e5c32098ed9`; tested ref `a7f2b77af61c1d0b7138bfdfce21cd1772c91f26` | `a7f2b77…` | `35305911122` | Focused targets and save/restore assertions passed; three-seed spell/census values are durable in the diagnosis file. |
| Corrected InPoss baseline | one-shot workflow subsequently retired from #416 | tested ref `a7f2b77af61c1d0b7138bfdfce21cd1772c91f26` | `a7f2b77…` | `35286928656` | 15,830 / 16,423 samples, shares 0.970815 / 0.964379, frozen 80% floors 12,664 / 13,138; values already durable in source comments and diagnosis. |
| Final code/spec/test CI | PR #416 branch | PR-side head `b6a21d2d303baae108e77953719130a440f8f686`; synthetic merge `e64322a67c1b37dddea145fcd73d2e2076fccb6a` | base `1bad655f…` | `35307891032` | Six recorded branch-protection contexts green; ordinary MatchEngine 501/0/12; functional job red only on held close-chance unexpected-green policy. |
| Close-chance retirement evidence | `evidence/pr416-close-chance-retirement` | `adcf21bf36c07273f082053d3778693f4075bf2a` | production head `b6a21d2d303baae108e77953719130a440f8f686` | `35344056248` | Evidence-only diagnostic: 2,494 final-third dribbles, meanCosine -0.038, goalwardShare 0.496; owner-held disposition unchanged. |

The two candidate refs
`evidence/pr416-narrow-rolling-candidate` and
`evidence/pr416-state-only-preforce-candidate`, plus
`evidence/pr416-close-chance-retirement`, are intentionally retained after this record is landed.
This provenance record does not authorize their deletion.

## 5. Failed, void, or non-certifying attempts

This section distinguishes harness failures/void attempts from valid experiments that produced
negative target results. Both matter to the causal record, but they are not the same class.

| Run | Class | Disposition |
| --- | --- | --- |
| `35278057546` | Harness failure | Invalid workflow configuration; no jobs. |
| `35278200848` | Harness failure | Runtime patch failed before tests. |
| `35283014828` | Void arm | Pair-removal diff guard failed before focused tests; residency leg was unablated. |
| `35285015171` | Harness failure | Invalid co-location-probe workflow configuration; no jobs. |
| `35285075938` | Harness failure | Co-location runtime patch guard failed before tests. |
| `35285144898` | Partially usable / non-certifying | Co-location patch and census were usable, but the synthetic measurement test later failed unrelated FM-03 LogAssert teardown; not strict target evidence. |
| `35293636125` | Valid negative-result arm | Rolling + pre-force pair ablation: mechanism census passed, but focused regression failed strict five-target TRX outcome enforcement; non-certifying for target restoration. |
| `35300926450` | Valid negative-result arm | Narrow Rolling candidate: ball-physics, widened-corpus, and corrected-baseline-detector jobs passed, but focused regression failed main-relative target enforcement; candidate did not restore the required focused target set. |

## 6. Full current evidence-ref inventory

This inventory prevents a later branch deletion from erasing the fact that a ref existed or what
role it played. “Helper” means the ref is not an additional independent causal result; its retained
conclusion is represented by the arm/run named above.

| Evidence branch | Current head SHA | Role / retained evidence |
| --- | --- | --- |
| `evidence/pr416-applykick-height-ablation` | `b00fd8028674f4acf84dc6a3c54de5e875275ae5` | ApplyKick code ablation; paired with run `35182220948`. |
| `evidence/pr416-applykick-height-focused` | `1e1cc40223e375d18f18ca0cacce559c1ea7145a` | Focused workflow wrapper for run `35182220948`. |
| `evidence/pr416-close-chance-retirement` | `adcf21bf36c07273f082053d3778693f4075bf2a` | Preserved evidence-only close-chance arm; run `35344056248`. |
| `evidence/pr416-colocated-ball-visible` | `a23683568206423275c80e639f54ea0c80d2d36f` | Co-location perception probe lineage; runs `35285144898` / `35285560809`. |
| `evidence/pr416-control-focused-census` | `ce2ef111491c42ec092ac60f8f32a1e0b512571c` | Early #416 focused/census helper; superseded by exact later lattice and mechanism runs. |
| `evidence/pr416-controlled-residency` | `33f3cbb530d42196300a84ad9f59b85abcdee1c6` | Main/#416 Controlled residency driver; run `35278109980`. |
| `evidence/pr416-current-head-validation` | `9a7755f9422c07c04207d543512c3e5c32098ed9` | Live-head validation helper; retained conclusion is run `35305911122` on exact ref `a7f2b77…`. |
| `evidence/pr416-final-head-7cc` | `4943cc84a850e5fa986989257f60c13232d6d083` | Intermediate exact-head focused-validation wrapper; superseded by committed-fix/live-head validation. |
| `evidence/pr416-final-validation` | `b2ea41e78ba9d77b339b76c7fb24f41f8df0d0d2` | Committed perception-fix focused validation; run `35286222621`. |
| `evidence/pr416-main-focused-census` | `53a8928d3a73fdf66ec3deac663ce541734dd390` | Main control helper; run `35224582571` executed earlier head `83a699f9…`. |
| `evidence/pr416-mechanism-census` | `b2079876c8daec0a90b29a837e3285d170ad093b` | Mechanism/census helper lineage; primary run `35284452456`. Historical head `8784eb235757e9707aaa1b69872cb20324bc6633` drove rolling + pre-force pair-ablation run `35293636125`. |
| `evidence/pr416-narrow-rolling-candidate` | `bb501a2128f9efbef5e98bffadb0d98214493e78` | Preserved narrow Rolling candidate; its own run is `35300926450`. The distinct pair-ablation run `35293636125` belongs to `mechanism-census@8784eb23…`. |
| `evidence/pr416-no-release-control-focused` | `7c7f5bd63835600ddd9999388ed161e62f569ee4` | ReleaseBallControl ablation; run `35278313267`. |
| `evidence/pr416-no-rolling-focused` | `9902a8a4a8dde13666a2997fa36decf6abeb3eb1` | Rolling-order removal; run `35272651902`; source blob `3297dd5a…`. |
| `evidence/pr416-no-state-machine-pair` | `48d19ceb40216a78479f3fbf7c71856c6f0f128c` | Failed pair-removal arm; run `35283014828` is void. |
| `evidence/pr416-no-stationary-focused` | `7f5f47fdc1a33d905257c7d23c15cf8385472cc8` | Stationary-promotion removal; run `35272599047`. |
| `evidence/pr416-pre-force-normalization-ablation` | `2df4121f1ba23c13cd1c7372cdb84b1bba4f4806` | Pre-force ablation workflow head; code head `d2c09f09…`; run `35182424557`. |
| `evidence/pr416-production-union-revert` | `e28239b25bea5df190a38aefc099a2e029d47bfa` | Complete production-union control; code commit `6d184424…`; run `35270645862`. |
| `evidence/pr416-release-control-focused` | `73e76e0f886a9bf791d6e820fefc808e2120f5e7` | Earlier BallCollision-focused helper; no independent conclusion retained beyond the later marker-guarded release ablation. |
| `evidence/pr416-rolling-order-focused` | `ce5bb0f1eb66e3b55af82c65e8c1800250c168f4` | Rolling-order clause alone; run `35271049186`. |
| `evidence/pr416-shot-fm03-severity` | `d06e9b067bd9936d246dd42a4312a5d6ac9444f9` | Diagnostic/helper lineage for FM-03 and widened tackle census; no separate production conclusion retained. |
| `evidence/pr416-state-machine-pair-focused` | `1875a6cffc25cb68ef9df577898e9e3ce8701c5d` | Exact StateMachine pair alone; run `35272495848`. |
| `evidence/pr416-state-only-preforce-candidate` | `efa2f8946a9a6a8852946b97a8e4c7d55013b0bf` | Preserved candidate lineage: state-only pre-force candidate `6c89365d…` / blob `4511b1cb…` / run `35301715589`, followed by low-speed Rolling isolation run `35302289613` at the current head. |
| `evidence/pr416-stationary-state-focused` | `3d4854517ef5b128516f21acdc981564f2ab57c9` | Stationary-promotion clause alone; run `35272069065`. |

## 7. Actions-log retention rule

No repository-specific Actions expiry is asserted here. GitHub's defaults are not evidence of this
repository's configured retention.

The durable rule is:

> If any unique evidence exists only in Actions console output and is not already represented
> durably in the lattice or repository, capture it now rather than relying on future Actions-log
> availability.

For PR #416, the causal measurements and failed-harness dispositions are already recorded in
`docs/tracking/w6-elevated-stationary-ball-fix.md`. This file adds the provenance that previously
depended on live refs or console output: exact evidence heads, exact target/reference SHAs, exact
workflow run IDs, relevant source blob identities, and the materially significant defining deltas.

The SHA and blob fields are historical identities, not a promise that GitHub will retain unreachable
objects after ref deletion. Material source deltas needed to interpret the disposable causal arms are
embedded in §3. The three candidate/retirement refs explicitly retained in §4 remain live by policy.
If cleanup would remove a unique workflow or evidence detail not represented in this record or the
owning diagnosis, §7 forbids that deletion until the detail is captured durably.

## 8. Step 5 evidence-ref archival and disposition audit

Step 5 first re-derived each live evidence ref's tip delta directionally from its merge base to the
ref. That tip inventory was exact, but review identified a separate deletion-risk dimension:
intermediate branch-exclusive file states can disappear before the tip and therefore are invisible
to a tip-only audit.

The corrected archive is `docs/tracking/evidence/pr416-ref-archive/`:

- **72** current-tip snapshot paths cover all 24 refs;
- **13** run-time snapshot paths preserve six cited run heads that later advanced;
- **15** intermediate-history paths preserve **13 previously unarchived blob identities**, including
  two revisions of `MatchEngineTackleTests.cs`, one revision of `ShotExecutorStateTests.cs`,
  historical workflow revisions, and the ApplyKick ablation generator/workflow;
- all snapshots are byte-identical Git blobs quarantined with a terminal `.txt` suffix;
- `MANIFEST.tsv` records all current/run/history snapshot provenance;
- `run-heads.tsv` records authoritative GitHub Actions metadata for all **31** run ids cited by the
  durable provenance/diagnosis;
- `ref-disposition.tsv` remains **21 `deletable` / 0 `retain` / 3
  `policy-retained`**; all 21 `deletable` rows are now `delete_now=true` as one atomic
  authorization set, while the three `policy-retained` rows remain `false`.

The earlier statement that archive completeness could be verified from `main` alone is withdrawn.
Main-only checks can verify byte integrity and reconstructability after landing; they cannot prove
that a live ref carried no unarchived intermediate history. Deletion therefore requires two gates:
(1) a **live-ref full-history reconciliation while the refs still exist**, covering every
branch-exclusive commit and changed-path blob state against the durable post-deletion set; and
(2) a **mainline archive-integrity/reconstruction check** after the archive lands.

The three policy-retained refs remain `evidence/pr416-close-chance-retirement`,
`evidence/pr416-narrow-rolling-candidate`, and
`evidence/pr416-state-only-preforce-candidate`. Archival does not revoke that policy.

The Step 5 authorization revision adds a post-deletion gate before any ref is removed. Gate A accepts
only two complete topologies: all 24 recorded refs at their pinned heads before deletion, or exactly
the three policy-retained refs after the authorized 21-ref batch is removed. Any partial 4–23-ref
state fails closed. CI requires the 21 `delete_now=true` values, so reverting authorization to
`false` is not a valid steady state. Immediately before deletion, Gate A must be re-run explicitly
in pre-delete mode; each remote deletion must be protected by its recorded head SHA (for Git,
`--force-with-lease=refs/heads/<ref>:<current_head>`) so a moved ref is rejected rather than
silently deleted. After the batch, post-delete mode must pass with exactly the three policy refs.
The 21 disposable refs were subsequently removed by an external deletion action while this authorization PR remained open. On the revised PR head, Gate A resolved the live state as post-delete and passed with exactly **3 refs / 21 deleted / 3 policy / authorized=True**; all three retained refs matched their recorded heads. This PR did not perform the deletion. The cleanup issue remains open only until this post-delete-capable gate and durable record land on `main`.

