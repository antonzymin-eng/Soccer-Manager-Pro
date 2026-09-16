# W6 Elevated Stationary Ball Fix — ERR-001-006

**Date:** September 15–16, 2026
**Status:** RECONCILED ON W2-ACTIVE MAIN; CORRECTED-BASELINE CAPTURE PENDING
**Scope:** Ball Physics #1 state invariant exposed by W6 physical Controlled possession, plus permanent hardening of the existing two-seed `sim_match_engine_inposs_gate` after W2 production activation.

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