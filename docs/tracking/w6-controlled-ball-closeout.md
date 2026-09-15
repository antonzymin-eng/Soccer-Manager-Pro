# W6 Controlled Ball — Wiring Closeout

**Date:** 2026-09-15  
**Status:** IMPLEMENTED / REGRESSION-LOCKED; merge readiness depends on final PR #412 gates  
**Scope:** Match-engine wiring backlog W6 only. W2 tackle activation remains a separate post-W6 evidence decision.

## Closed implementation boundary

`BallStateType.Controlled` now has production entry for genuine open-play possession. MatchEngine continues to use `_possessingAgentId` for both physical possession and restart-taker designation, but the Ball Physics state distinguishes the two: open-play possession is `Controlled`; a placed restart ball remains `Stationary`.

The production grant paths are first-touch control/interception, loose-ball pickup, tackle ball-won, and goalkeeper possession. Controlled balls are attached to their holder after movement and goalkeeper/heading physics. Outfield control uses ball-rest height; goalkeeper control preserves the actual claim/contact height while x/y follow the keeper.

Non-kick physical release now exits `Controlled` explicitly. Kicks already leave control through `BallCollision.ApplyKick`; tackle-loose and the six-second goalkeeper release use the explicit release transition. Restart pseudo-possession is not tackleable because the tackle resolver requires a physically `Controlled` ball.

No new durable field, latch, schema version, RNG stream, reservation, or draw order was introduced.

## Regression locks retained

The W6 regression set proves:

1. Real loose-ball pickup enters `Controlled` and anchors the ball to the holder.
2. A controlled outfield ball follows the holder during Physics.
3. Restart-taker designation leaves the placed ball `Stationary` at the restart spot.
4. Goalkeeper physical control preserves claim height while following the keeper.
5. Forced-loose staging exits `Controlled` and derives a physical loose-ball state.
6. The six-second goalkeeper backstop exits `Controlled`, drops the ball to foot height, and arms the re-collect cooldown.
7. Ball Physics' direct Controlled entry/non-kick release transition preserves recovery checkpoints.
8. Loose/restart intervals do not freeze elapsed tackle cooldown.

The two test-only tackle-cooldown seams added for item 8 are retained deliberately as durable regression support. They are not production state and are not temporary measurement scaffolding.

## Recovered Codex P2 — closed

The Codex review correctly identified that the physical-carrier early return could occur before `_tackleCooldown` aging, freezing an elapsed-AI-stride cooldown while the ball was loose, airborne, or placed for a restart.

Commit `f927e115b5e61771b5cf0fd7c07e93514759b1b6` moved cooldown aging before the physical-carrier gate and added `LooseBall_DoesNotFreezeElapsedTackleCooldown`. The PR review thread is resolved. This tackle cooldown is distinct from the W12 Pressing-AI `Cooldown` exit driven by `DisengageResolver` / `_cooldownTicks`.

## Pre-registration carried forward before measurement

`docs/tracking/w6-controlled-ball-preregistration.md` has been copied unchanged from `wiring/w12-evidence-repair` into PR #412 before any post-W6 measurement. Its locked baseline, prediction thresholds, and falsifier therefore remain the pre-registered W12 hypothesis rather than a post-result interpretation.

Because the file is preserved verbatim, its final paragraph still records the cooldown P2 as unresolved at pre-registration time. The resolution above is the later closeout record; the historical pre-registration is not rewritten after the fact.

For the later W12 rejection-wall measurement, `w6-controlled-ball-preregistration.md` is the locked interpretation record. The older `w6-post-wiring-measurement-preregistration.md` remains useful for its separate W2 armed-stall prediction, but its procedural instruction to land W12 evidence repair before PR #412 is superseded by the owner sequence recorded on September 15, 2026.

## Owner sequence after this closeout

1. Merge PR #412 once its final gates are green.
2. Run the post-W6 W2 measurement, record the activation/non-activation decision, and remove the temporary dispatch branch.
3. Salvage `wiring/w12-evidence-repair` onto fresh `main`; port additive evidence/checker/CI material and re-derive the post-#398 comparison against current `main` rather than cherry-picking the stale comparison modification.

This sequencing change affects procedure only. It does not retroactively alter either pre-registered hypothesis.

## Scaffolding disposition

No temporary W6 measurement workflow, evidence corpus, or ad-hoc instrumentation file remains in the PR #412 diff. The final diff is limited to the production wiring, durable regression tests, this closeout, and the pre-registration carried forward for the next measurement step.

## W2 boundary

W6 does **not** activate W2 or change the governed shipping value of `TackleContactRadiusM`. The next step is evidence: rerun the armed tackle corpus/composed-match measurement and make the W2 activation decision separately. A state-model correctness fix must not smuggle in an unmeasured balance change.
