# W6 Elevated Stationary Ball Fix — ERR-001-006

**Date:** September 15–17, 2026
**Status:** MINIMAL COMPOSITION FIX VALIDATED; PERMANENT DETECTOR FLOORS FROZEN; NORMAL CI RERUN PENDING
**Scope:** Ball Physics #1 state invariant exposed by W6 physical Controlled possession, plus permanent hardening of the existing two-seed `sim_match_engine_inposs_gate` after W2 production activation.


## September 17 live-head closure — supersedes the earlier Ball-Physics branch-point language below

Live GitHub advanced from diagnostic head `07dcc670fed1ecc6aa32e9711d4bc8830c0ec2ad` to
PR head `a7f2b77af61c1d0b7138bfdfce21cd1772c91f26` before the final correction was chosen.
That intervening production delta is the co-located-ball perception correction
(`3a4a228495b73057684c727a55973c5072d7fdc1`) plus its BP-007 lock
(`f92305b58cd8a8dc69d9b95e94d2bda04aa56201`).

Subsequent pair-ablation work on the older head remains useful causal evidence but does **not**
justify retracting any ERR-001-006 Ball Physics invariant. A Rolling+pre-force ablation removed the
old seed-A lock, but a narrower Ball Physics candidate still produced a 90,549-tick terminal HOLD
spell on seed `0x00000000D1A6D05E`. The exact pinned-main full-match baseline already contains the
same pre-existing attractor shape at 85,987 ticks, while restoring the old Rolling order drops that
deterministic trajectory to 1,064 ticks. Those results show trajectory sensitivity; they do not
identify a Ball Physics contract defect requiring v2.11.

The live perception correction provides the stronger mechanism closure. Exact-head validation run
`35305911122` on `a7f2b77…` reports, across the preregistered three full-match tackle seeds:

- longest Controlled spells **1,096 / 1,236 / 1,256 ticks**;
- pooled clean wins / observed dispossessions **5 / 5**;
- `sim_match_engine_shot_outcomes`, `AControlledCarrierIsActuallyDispossessed`,
  `BothOutcomesOccur_TheBallIsSometimesWonAndSometimesKnockedLoose`, and
  `TacklesHappenInComposedPlay` all **Passed**;
- `ATackleFoulIsGivenAsASlideTackleAndNotJudgedTwice` remains **NotExecuted**, matching
  pinned main's assumption-gated outcome.

The census job itself ended red only because its synthetic diagnostic intentionally did not suppress
unrelated composed-play error logs; its emitted measurements above completed before teardown. The
same run separately proved both `SaveAndRestoreCarryTheTackleLatches` seeds pass every latch and
replay assertion when unrelated composed-play `ShotExecutor FM-03` logging is excluded from that
test's oracle. The permanent test now scopes `LogAssert.ignoreFailingMessages` to that one
save/restore lock; no Shot Mechanics log level is changed.

Perception #7 §3.5.1 is the owning defective contract and is back-propagated under
`ERR-007-004`: exact projected co-location has no meaningful bearing and therefore satisfies the
FoV-angle predicate regardless of facing, while range and occlusion remain unchanged. The earlier
Ball Physics v2.11 candidate is **not** landed.

### Corrected-baseline freeze after the final production correction

The valid post-perception corrected-baseline capture is Actions run `35286928656`, exact PR head
`a7f2b77…`:

| Seed | Baseline samples | Home / away share | Frozen 80% floor |
| --- | ---: | ---: | ---: |
| `0x0F1E2D3C4B5A6978` | 15,830 | 0.970815 / 0.970815 | **12,664** |
| `0x1A2B3C4D5E6F7081` | 16,423 | 0.964379 / 0.964379 | **13,138** |

The permanent scenario now enforces those floors per seed alongside the existing independent
`> 0.70` mirrored possession predicates. Pooled population remains diagnostic only. Because the
close-out commit changes tests/spec/tracking but no production behavior, this exact-head capture is
not invalidated again. The temporary one-shot corrected-baseline workflow is retired in the same
landing.

## September 17 post-reconciliation MatchEngine regression diagnosis

After reconciliation onto W2-active main, normal CI exposed three #416-specific MatchEngine regressions:
`AControlledCarrierIsActuallyDispossessed`,
`BothOutcomesOccur_TheBallIsSometimesWonAndSometimesKnockedLoose`, and
`sim_match_engine_shot_outcomes`. Diagnosis was performed against exact #416 head
`07dcc670fed1ecc6aa32e9711d4bc8830c0ec2ad` and pinned main
`1bad655f5826070d1e29f54a845cdf2c549f66bc` before changing production again.

### Corrected project-scoped oracle and the historical 513 count

Project-scoped discovery on pinned main and #416 returns 516 test-case lines and 514 unique display
names, with identical discovered sets. The two duplicate display names are
`Apply_NullArguments_Throw` and `TwoSameSeedRuns_ProduceIdenticalDigestChains`.

The historical normal-CI total of 513 is not a contradictory discovery count. Run
`35157097577`, job `104999039293`, invoked VSTest with
`Name!=sim_match_engine_close_chance` and reported 498 passed + 12 skipped + 3 failed = 513
executed cases. The 513 number is therefore the normal-CI execution total under that explicit
one-name exclusion; it is not the project-scoped discovery cardinality.

### Shared tackle corpus and baseline correction

The four `MatchEngineTackleTests` assertions share one `[OneTimeSetUp]` simulation corpus:
two deterministic seeds, 150,000 ticks per seed, with every assertion reading the same pooled
counters. On #416 before the final correction the pooled census was:

`won=0, loose=2, foul=0, missed=23, dispossessions=0`.

That single corpus explains why `TacklesHappenInComposedPlay` passed while the two outcome /
dispossession assertions failed. The foul-share test did not pass: its exact result was
`NotExecuted` / `Skipped` because `Assume.That(connected, Is.GreaterThan(30))` was not
satisfied.

Live pinned-main evidence corrects an earlier diagnosis assumption here. Main run
`35133104970`, job `104918684391`, also reports the foul-share test as `Skipped`, not
`Passed`. The exact two-seed main census measured below has only seven connected challenges.
Consequently a strict-`Passed` requirement for this foul-share assertion is not a #416
regression criterion; satisfying it would require separate tackle/foul test-corpus work, which is
outside ERR-001-006.

### Production-delta lattice

A complete production-union revert, parented directly from `07dcc67`, is commit
`6d18442420ae77da0bfbcb26bacc9ec9ba1587d3`; workflow commit
`e28239b25bea5df190a38aefc099a2e029d47bfa`, run `35270645862`. Reverting the complete
five-clause ERR-001-006 production delta to pinned-main behavior makes all three original target
tests pass. This proves that the branch regressions are triggered by the ERR-001-006 production
delta rather than solely by test/helper churn.

Valid focused arms established the following lattice:

| Arm | Run | Result |
| --- | --- | --- |
| ApplyKick-height clause ablated from #416 | `35182220948` | all three original targets remain red; tackle census remains `0/2/0/23/0` |
| Pre-force normalization ablated from #416 | `35182424557` | all three original targets remain red; tackle census remains `0/2/0/23/0` |
| ReleaseBallControl clause ablated from #416 | `35278313267` | tackle outcome/dispossession targets red, shot target red, foul-share `NotExecuted`; census remains `0/2/0/23/0` |
| Stationary-promotion clause ablated from #416 | `35272599047` | all three original targets remain red; census remains `0/2/0/23/0` |
| Rolling-order clause ablated from #416 | `35272651902` | all three original targets remain red; census remains `0/2/0/23/0` |
| Stationary-promotion clause alone on main-like behavior | `35272069065` | all three original targets pass |
| Rolling-order clause alone on main-like behavior | `35271049186` | tackle targets pass; shot target fails |
| Exact StateMachine pair alone on main-like behavior | `35272495848` | tackle targets pass; shot target fails |

Thus no one-clause removal from full #416 restores the branch. Rolling reorder is sufficient to
expose the shot failure by itself, but it is not sufficient to produce the tackle regression.
No valid pair-removal result exists. The attempted StateMachine-pair removal run
`35283014828` failed its `Verify pair-removal diff` guard before target execution; its
separate residency job did not apply the ablation. That run is void as pair-removal evidence.
Once the composition mechanism below restored all three targets without reverting any
ERR-001-006 clause, further pair-removal search was no longer justified.

Harness failures excluded from causal evidence are:

- `35278057546`: invalid workflow configuration; no jobs.
- `35278200848`: runtime patch failed before tests.
- `35283014828`: pair-removal diff verification failed before focused tests; residency job was
  unablated.
- `35285015171`: invalid co-location-probe workflow configuration; no jobs.
- `35285075938`: co-location runtime patch guard failed before tests.

Run `35285144898` did apply the co-location diagnostic correction and emitted usable census
values, but its synthetic measurement test later failed Unity-shim LogAssert verification on
`[ShotExecutor] FM-03`. It is not used as strict target evidence; strict target results come
from `35285560809` and final committed-head validation from `35286222621`.

### Controlled residency and per-seed tackle evidence

Controlled-state residency run `35278109980` used the same two tackle seeds and 150,000-tick
horizon on pinned main (job `105393487824`) and #416 (job `105393488091`):

| Seed | Main Controlled share | #416 Controlled share |
| --- | ---: | ---: |
| `0x0F1E2D3C4B5A6978` | 7.386% | 68.240% |
| `0x00000000D1A6D05E` | 8.360% | 8.337% |
| pooled | 7.873% | 38.288% |

The tackle regression is therefore not caused by rarely reaching `BallStateType.Controlled`.
Seed A instead spends far too long under controlled possession.

Mechanism-census run `35284452456` then measured each seed independently.

Pinned main:

- Seed A: `won=0, loose=1, foul=0, missed=18, dispossessions=0`;
  `gateEligible=105`, `gateInRadius=19`.
- Seed B: `won=1, loose=4, foul=1, missed=20, dispossessions=1`;
  `gateEligible=88`, `gateInRadius=26`.
- Pooled connected challenges: 7.

#416:

- Seed A: `won=0, loose=0, foul=0, missed=2, dispossessions=0`;
  `gateEligible=12794`, `gateInRadius=2`.
- Seed B: `won=0, loose=2, foul=0, missed=21, dispossessions=0`;
  `gateEligible=79`, `gateInRadius=23`.

Seed A entered a terminal 97,682-tick `Controlled` run from tick 52,319 through tick 150,000.
The holder was outfield agent 19, not a goalkeeper. Holder and ball remained fixed at
`(52.962357, 4.660061)`, with ball `z=0.110000`. Across 16,281 AI heartbeats the selected
action was `HOLD` every time; pass and shot executors were idle.

Seed B also regressed independently despite essentially unchanged Controlled residency. Main
seed B supplies six connected challenges; #416 supplies only two. The branch-wide tackle failure
therefore cannot be attributed only to seed A, although seed A contains the obvious absorbing
state.

### Final mechanism

Outfield controlled-ball driving attaches the ball to the holder at exactly the same XY
coordinate. Ball perception nevertheless passed that zero displacement into the ordinary FoV
bearing calculation. `atan2(0,0)` then supplies an artificial world-East bearing. When the
carrier faces sufficiently far from East, the carrier can have authoritative possession while
`BallVisible=false`.

That state has a concrete decision-tree consequence: SHOOT and DRIBBLE require visible ball
state, while HOLD remains available. In #416 seed A it becomes absorbing: agent 19 repeatedly
selects HOLD, stays fixed, and challengers almost never reach tackle radius. The altered
ERR-001-006 trajectories expose this pre-existing zero-distance perception defect; the complete
ERR production delta is the trigger, but no ERR-001-006 invariant clause itself needs to be
retracted.

The smallest semantically correct correction is therefore in ball perception: if observer and
ball are exactly co-located, the ball has no meaningful bearing and is treated as inside FoV.
Ordinary non-zero-distance range, FoV, and occlusion behavior is unchanged. Production commit
`3a4a228495b73057684c727a55973c5072d7fdc1` implements that rule; test commit
`f92305b58cd8a8dc69d9b95e94d2bda04aa56201` adds BP-007, which faces away from world-East
while observer and ball share the same XY.

### Final focused validation

The runtime semantic probe `35285560809` first established that the co-location correction
restores the shot and tackle behavior on #416 while preserving comparable healthy behavior on
pinned main.

The committed PR-head validation is evidence branch `evidence/pr416-final-validation`, workflow
commit `b2ea41e78ba9d77b339b76c7fb24f41f8df0d0d2`, run `35286222621`, job
`105419040400`, checking out exact production/test head
`f92305b58cd8a8dc69d9b95e94d2bda04aa56201`.

Results:

- every `TacticalDirector.BallPhysics.Tests` test passed, including the ERR-001-006 locks
  `ApplyKick_ZeroVelocityWhileElevated_RemainsAirborne`,
  `ReleaseBallControl_FromElevatedControlledBall_TransitionsToAirborneWithoutTeleporting`,
  `Rolling_AboveEnterThreshold_BelowMinVelocity_StillTransitionsToAirborne`,
  `Stationary_AboveEnterThreshold_TransitionsToAirborne`, and
  `UpdateBallPhysics_ElevatedStationaryState_RecoversToAirborneAndFalls`;
- BP-007 `BP007_CoLocatedBall_IsVisibleRegardlessOfFacing` passed;
- `sim_match_engine_shot_outcomes` passed;
- `AControlledCarrierIsActuallyDispossessed` passed;
- `BothOutcomesOccur_TheBallIsSometimesWonAndSometimesKnockedLoose` passed;
- `TacklesHappenInComposedPlay` passed;
- final shared tackle census is
  `won=3, loose=6, foul=0, missed=20, dispossessions=3`, restoring meaningful challenge volume,
  clean wins, both connected outcomes, and actual dispossessions;
- `ATackleFoulIsGivenAsASlideTackleAndNotJudgedTwice` remains `Skipped` /
  `NotExecuted` because only nine challenges connect. Pinned main and pinned main plus the same
  perception correction also skip this assumption-gated test, so this is inherited baseline
  behavior rather than a #416 regression.

No KD-W1 calibration, foul/card calibration, `[GT]` tuning, T-DA-DET-005 work, detector-floor
tuning, six-seed expansion, or PR #419 work was performed as part of this diagnosis.

## September 16 reconciliation and detector preregistration

PR #416 was reconciled onto W2-active production `main` (`1bad655f5826070d1e29f54a845cdf2c549f66bc`) by merge commit `8c4eb795dee930684b6896d14baf1cfc0a4f2903`. The pre-#418 measurements below are historical causal evidence only. They are **superseded as governing activation/detector evidence** because they were captured before the production W2 activation engine state.

Before observing any corrected-baseline final-third sample counts on the reconciled production behavior, the permanent detector derivation rule is frozen as follows:

- corpus: the existing two worst-separator adversarial seeds only — `0x0F1E2D3C4B5A6978` and `0x1A2B3C4D5E6F7081`;
- run length: `324000` ticks per seed (full 90-minute match);
- sample cadence: every `6` ticks (0.1 s), unchanged from the existing scenario;
- possession criterion: each seed independently requires home-view share `> 0.70` **and** away-view share `> 0.70`;
- non-vacuity derivation: for each seed independently, `floor(seed) = floor(0.80 × corrected_baseline_samples(seed))`, where the outer `floor` is mathematical round-down to an integer sample count;
- the corrected baseline must be captured on the exact reconciled production head before the numeric floors are frozen;
- the pooled two-seed sample count/share may be retained as a diagnostic statistic, but it is not a validity basis for either seed;
- any production-behavior change after corrected-baseline capture invalidates that capture and requires the baseline/floors to be derived again.

**Rationale for 0.80:** the sample floor is a non-vacuity guard against denominator starvation, not a football-realism target. Freezing 80% before observing the corrected counts allows bounded future trajectory movement without making exact sample cardinality a contract, while forcing a loud failure if a seed loses more than 20% of the final-third population that made its possession ratio meaningful at this corrected baseline. Per-seed floors prevent a healthy seed from masking starvation of the other seed.

The broader six-seed W2 evidence corpus is deliberately **not** part of this permanent PR gate. Its separate preregistration and run belong to Step 1b after this correctness landing.

## Failure evidence — historical pre-#418 record

Exact merged W6 head `e4335f7ff059deb483b1aaa534f2190fd3762008`, shipping-disarmed W2, seed `0x0F1E2D3C4B5A6978`, regressed from the exact pre-W6 first parent's **0.975 / 0.975** per-seed mirrored InPoss share to **0.530 / 0.530**. The old pooled scenario remained near its floor because the second seed stayed healthy, masking the collapse.

That **0.530 / 0.530 result was originally accepted as an expected disarmed negative control in the W2 evidence chain. PR #416 supersedes that interpretation:** trajectory tracing demonstrates that it was a persistent elevated-ball deadlock exposed by W6, not valid evidence that disarming W2 should collapse possession. The historical measurement and artifact remain part of the record; only their interpretation is superseded.

A whole-pitch 0.1 s state trace isolated one continuous no-holder/no-pass interval from tick **100,062** to the half-time reset at tick **162,000**. Throughout it the ball was `Stationary`, speed `0.000`, at exactly `(7.241, 30.676, 0.973)` m. `Stationary` receives no gravity; first-touch requires motion; loose-ball pickup rejects an elevated ball. The match therefore cannot recover until the restart resets the state.

## Causal localization

Ablation against exact post-W6 `main` shows removing only W6's goalkeeper kick-side `ReleasePossessionOnKick` does **not** change the failure (`0.530`). Restoring either goalkeeper physical-Controlled acquisition or outfield physical-Controlled acquisition to legacy flag-only behavior changes the deterministic trajectory and avoids the dead state (`0.986` and `0.981` respectively); restoring all legacy acquisition reproduces exact pre-W6 `0.975`. W6 therefore exposes the latent Ball Physics invariant violation through its new physical-Controlled trajectories; reverting W6 is not the fix.

## ERR-001-006

Ball Physics #1 encoded two mutually incompatible rules: `STATIONARY` applies no forces and is externally exited, while §3.1.11.2 selected post-kick state from velocity alone and §3.1.3 let a slow `ROLLING` ball become `STATIONARY` before checking airborne height. W6 also added a non-kick Controlled release that selected `STATIONARY` regardless of height. Together these permit an uncontrolled ball above `AIRBORNE_ENTER_THRESHOLD` to become a force-free `STATIONARY` ball indefinitely.

The corrected invariant is: **an uncontrolled ball whose centre is above `AIRBORNE_ENTER_THRESHOLD` is Airborne regardless of low/zero horizontal speed; altitude is evaluated before the ground-rest stop rule.**

## Fix boundary

- `BallCollision.ApplyKick`: current height participates in state selection.
- `BallCollision.ReleaseBallControl`: elevated release becomes `Airborne`, ground release remains `Stationary`.
- `BallStateMachine`: elevated `Stationary` recovers to `Airborne`; `Rolling` checks height before low speed.
- `BallPhysicsCore`: normalizes an already-invalid elevated `Stationary`/`Rolling` state before choosing forces, covering restored/legacy state as well as new producers.
- Direct Ball Physics regression locks replace the W6 test that previously asserted an elevated non-kick release becomes `Stationary` and directly prove restored/legacy elevated `Stationary` state self-heals and begins falling in the same physics tick.
- Ball Physics §3.1 is back-propagated as **v2.10**; `ERR-001-006` follows already-occupied `ERR-001-005`.

## Validation — historical pre-#418 evidence, superseded for governing use

Focused Ball Physics validation was green on the pre-#418 branch. The direct `BallPhysicsCore` recovery lock also passed independently before its cleanup commit landed.

The composed post-W6 rerun used exact production-fix head `fd88503026f97e5e21d84f14f7b0aa62c65b6ad7` and the same strict per-seed measurement transform used for the earlier W2 evidence. Later pre-reconciliation branch commits changed only spec/history/test documentation and did not change the measured production behavior.

**Historical shipping-disarmed run:** Actions run `35057791634` produced:

- `0x0F1E2D3C4B5A6978`: 11,539 samples, **0.982 / 0.982** mirrored InPoss share;
- `0x1A2B3C4D5E6F7081`: 11,081 samples, **0.961 / 0.961**;
- total final-third samples: **22,620**, above the then-current pooled 20,000 non-vacuity floor;
- `gate_rc=0`, `test_rc=0`.

This closed the W6-introduced shipping regression on that pre-#418 engine: the failing seed moved from post-W6 main's **0.530** to **0.982** without activating W2. Artifact `w6-fix-disarmed-35057791634` has digest `sha256:f1717464ada8ab56660fe7b8a2be8fc483269923a71326560600a8f9217faa7b`.

**Historical measurement-armed run:**

- `0x0F1E2D3C4B5A6978`: 8,445 samples, **0.983 / 0.983**;
- `0x1A2B3C4D5E6F7081`: 10,326 samples, **0.971 / 0.971**;
- all four mirrored shares were above the unchanged strict `> 0.70` bound;
- total final-third samples were **18,771**, below the then-current pooled 20,000 non-vacuity floor;
- therefore `gate_rc=1`, `test_rc=1`, with the failure specifically `final-third-phase-samples-are-taken`, not either possession-share predicate.

Artifact `w6-fix-armed-35057791634` has digest `sha256:2cf10bece5a7057396abb8aaa25f6bce32b9198c173a8c23299514131c741e5c`.

These pre-#418 measurements remain useful causal history but are not governing evidence for the now-W2-active production engine. The next evidence step is the preregistered corrected-baseline capture on the reconciled head, followed by freezing the two numeric per-seed floors and running the hardened detector.