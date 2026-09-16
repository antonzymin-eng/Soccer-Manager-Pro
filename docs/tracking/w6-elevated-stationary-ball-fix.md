# W6 Elevated Stationary Ball Fix — ERR-001-006

**Date:** September 15, 2026
**Status:** PRODUCTION FIX VALIDATED; W2 ACTIVATION REMAINS BLOCKED ON ARMED NON-VACUITY EVIDENCE
**Scope:** Ball Physics #1 state invariant exposed by W6 physical Controlled possession. W2 remains shipping-disabled.

## Failure evidence

Exact merged W6 head `e4335f7ff059deb483b1aaa534f2190fd3762008`, shipping-disarmed W2, seed `0x0F1E2D3C4B5A6978`, regressed from the exact pre-W6 first parent's **0.975 / 0.975** per-seed mirrored InPoss share to **0.530 / 0.530**. The old pooled scenario remained near its floor because the second seed stayed healthy, masking the collapse.

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

## Validation

Focused Ball Physics validation is green. The direct `BallPhysicsCore` recovery lock also passed independently before its cleanup commit landed.

The composed post-W6 rerun used exact production-fix head `fd88503026f97e5e21d84f14f7b0aa62c65b6ad7` and the same strict per-seed measurement transform used for the earlier W2 evidence. Later branch commits change only spec/history/test documentation and do not change the measured production behavior.

**Shipping-disarmed W2 is green end-to-end:** Actions run `35057791634` produced:

- `0x0F1E2D3C4B5A6978`: 11,539 samples, **0.982 / 0.982** mirrored InPoss share;
- `0x1A2B3C4D5E6F7081`: 11,081 samples, **0.961 / 0.961**;
- total final-third samples: **22,620**, above the unchanged 20,000 non-vacuity floor;
- `gate_rc=0`, `test_rc=0`.

This closes the W6-introduced shipping regression: the failing seed moved from post-W6 main's **0.530** to **0.982** without activating W2. Artifact `w6-fix-disarmed-35057791634` has digest `sha256:f1717464ada8ab56660fe7b8a2be8fc483269923a71326560600a8f9217faa7b`.

**Measurement-armed W2 is behaviorally healthy on the possession predicate but not a fully green governed run:**

- `0x0F1E2D3C4B5A6978`: 8,445 samples, **0.983 / 0.983**;
- `0x1A2B3C4D5E6F7081`: 10,326 samples, **0.971 / 0.971**;
- all four mirrored shares remain above the unchanged strict `> 0.70` bound;
- total final-third samples are **18,771**, below the unchanged 20,000 non-vacuity floor;
- therefore `gate_rc=1`, `test_rc=1`, with the failure specifically `final-third-phase-samples-are-taken`, not either possession-share predicate.

Artifact `w6-fix-armed-35057791634` has digest `sha256:2cf10bece5a7057396abb8aaa25f6bce32b9198c173a8c23299514131c741e5c`.

The Ball Physics correctness fix may therefore proceed independently: production still ships W2 disabled and the shipping-disarmed composed gate is green. **W2 activation does not proceed from this record.** Its armed evidence must receive a separate disposition or a pre-registered measurement design that restores adequate sample population without weakening the possession-share bound.
