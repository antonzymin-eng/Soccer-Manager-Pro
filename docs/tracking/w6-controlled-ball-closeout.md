# W6 Controlled Ball — Wiring Closeout

**Date:** 2026-09-15  
**Status:** IMPLEMENTED; REVIEW CLOSURE BLOCKED on keeper-held carry correction + post-fix evidence  
**Scope:** Match-engine wiring backlog W6 only. W2 tackle activation remains a separate post-W6 evidence decision.

## Closed implementation boundary

`BallStateType.Controlled` now has production entry for genuine open-play possession. MatchEngine continues to use `_possessingAgentId` for both physical possession and restart-taker designation, but the Ball Physics state distinguishes the two: open-play possession is `Controlled`; a placed restart ball remains `Stationary`.

The production grant paths are first-touch control/interception, loose-ball pickup, tackle ball-won, and goalkeeper possession. Controlled balls are attached to their holder after movement and goalkeeper/heading physics. Outfield control uses ball-rest height; goalkeeper control preserves the actual claim/contact height while x/y follow the keeper.

Non-kick physical release now exits `Controlled` explicitly. Kicks already leave control through `BallCollision.ApplyKick`; tackle-loose and the six-second goalkeeper release use the explicit release transition. Restart pseudo-possession is not tackleable because the tackle resolver requires a physically `Controlled` ball.

No new durable field, latch, schema version, RNG stream, reservation, or draw order was introduced by the W6 wiring itself.

## Regression locks retained

The W6 regression set proves or, where noted, deliberately exposes:

1. Real loose-ball pickup enters `Controlled` and anchors the ball to the holder.
2. A controlled outfield ball follows the holder during Physics.
3. Restart-taker designation leaves the placed ball `Stationary` at the restart spot.
4. Goalkeeper physical control preserves claim height while following the keeper.
5. Forced-loose staging exits `Controlled` and derives a physical loose-ball state.
6. The six-second goalkeeper backstop exits `Controlled`, drops the ball to foot height, and arms the re-collect cooldown.
7. Ball Physics' direct Controlled entry/non-kick release transition preserves recovery checkpoints.
8. Loose/restart intervals do not freeze elapsed tackle cooldown.
9. The composed keeper-claim scenario proves a claim arrests the incoming ball and the held `Controlled` ball remains attached to the claiming keeper.
10. The composed keeper-claim scenario again asserts the football consequence that an observably held claim does not end in an own goal. This predicate is intentionally red on the pre-closure W6 behavior described below until the production carry defect is corrected.

The two test-only tackle-cooldown seams added for item 8 are retained deliberately as durable regression support. They are not production state and are not temporary measurement scaffolding.

## Recovered Codex P2 — closed

The Codex review correctly identified that the physical-carrier early return could occur before `_tackleCooldown` aging, freezing an elapsed-AI-stride cooldown while the ball was loose, airborne, or placed for a restart.

Commit `f927e115b5e61771b5cf0fd7c07e93514759b1b6` moved cooldown aging before the physical-carrier gate and added `LooseBall_DoesNotFreezeElapsedTackleCooldown`. The PR review thread is resolved. This tackle cooldown is distinct from the W12 Pressing-AI `Cooldown` exit driven by `DisengageResolver` / `_cooldownTicks`.

## Keeper-held carry defect — BLOCKING REVIEW CLOSURE

The earlier keeper-claim compatibility pass reached an incomplete conclusion. It correctly established that W6 turns physical possession into a kinematic constraint, so a held ball follows live keeper locomotion rather than continuing the incoming shot independently. It was also correct that Law 10 adjudication awards a goal when the ball crosses the line.

What it missed was the football consequence of that new coupling. The deterministic keeper-claim corpus measured **2 of 17 claims** ending with the holding keeper carrying the attached `Controlled` ball through his own goal line. The score path is therefore not the defect; the defect is keeper locomotion while holding, combined with Agent Movement's general 5 m exterior safety buffer. Treating that measured rate as intended behavior would encode a football-implausible result into the W6 closeout.

The attachment predicate remains useful and is retained. The independent consequence predicate `held-claim-does-not-concede-own-goal` has also been restored. Its attribution reads score changes before holder-based claim-window closure because a goal restart clears possession within the same `RunTick`; reversing that order would make the consequence structurally unreachable on the scoring tick.

The required production closure is deliberately narrow: constrain only a goalkeeper whose post-physics state is `HandsOnBall` at the goal plane it defends, after keeper locomotion and before the MatchEngine W6 attachment step. The general Agent Movement pitch buffer and legitimate goal adjudication must remain unchanged. No new `[GT]`, RNG surface, durable field, or snapshot schema is justified by this correction.

## Owner-held RED observation — PENDING POST-FIX REMEASUREMENT

On pre-closure W6 head `4dd62477f381f4cb3fe285e972d43f9d6acc7e86`, the ordinary MatchEngine suite completed **499 passed / 0 failed / 12 skipped**, while the separately executed owner-held `sim_match_engine_close_chance` unexpectedly passed. The testing-policy verifier correctly blocked on that unexpected green.

That result is **not** a retirement or rebaseline decision. The standing ledger remains `meanCosine=-0.165` / `goalwardShare=0.407`, and W6 changes carrier/ball position coupling while the measured pre-closure state also contains the keeper-held carry defect above. The exact close-chance scenario must therefore be rerun after the carry correction. Do not change its expected diagnostics or thresholds from this provisional run; any retirement of the owner-held RED is a later owner decision based on post-fix evidence.

## Pre-registration carried forward before measurement

`docs/tracking/w6-controlled-ball-preregistration.md` has been copied unchanged from `wiring/w12-evidence-repair` into PR #412 before any post-W6 measurement. Its locked baseline, prediction thresholds, and falsifier therefore remain the pre-registered W12 hypothesis rather than a post-result interpretation.

Because the file is preserved verbatim, its final paragraph still records the cooldown P2 as unresolved at pre-registration time. The resolution above is the later closeout record; the historical pre-registration is not rewritten after the fact.

For the later W12 rejection-wall measurement, `w6-controlled-ball-preregistration.md` is the locked interpretation record. The older `w6-post-wiring-measurement-preregistration.md` remains useful for its separate W2 armed-stall prediction, but its procedural instruction to land W12 evidence repair before PR #412 is superseded by the owner sequence recorded on September 15, 2026.

## Owner sequence after this closeout

1. Finish PR #412 review closure: correct keeper-held carry, rerun the keeper corpus and exact close-chance scenario, sync landing records, merge current `main`, and require final gates green.
2. Run the post-W6 W2 measurement, record the activation/non-activation decision, and remove the temporary dispatch branch.
3. Salvage `wiring/w12-evidence-repair` onto fresh `main`; port additive evidence/checker/CI material and re-derive the post-#398 comparison against current `main` rather than cherry-picking the stale comparison modification.

This sequencing change affects procedure only. It does not retroactively alter either pre-registered hypothesis.

## Scaffolding disposition

No temporary W6 measurement workflow, evidence corpus, diagnostic workflow, or ad-hoc instrumentation file remains in the PR #412 diff. The earlier one-shot CI experiments used during review closure were removed without modifying production source and are not part of the final changed-file set.

## W2 boundary

W6 does **not** activate W2 or change the governed shipping value of `TackleContactRadiusM`. The next step after PR #412 is evidence: rerun the armed tackle corpus/composed-match measurement and make the W2 activation decision separately. A state-model correctness fix must not smuggle in an unmeasured balance change.
