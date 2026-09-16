# W6 Elevated Stationary Ball Fix — ERR-001-006

**Date:** September 15, 2026
**Status:** IMPLEMENTED ON FIX BRANCH; VALIDATION PENDING
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
- Direct Ball Physics regression locks replace the W6 test that previously asserted an elevated non-kick release becomes `Stationary`.

No W2 activation or tackle calibration is part of this fix. After the focused Ball Physics gate is green, the same strict two-seed post-W6 disarmed and armed measurements must be rerun on the fix head before W2 sequencing resumes.
