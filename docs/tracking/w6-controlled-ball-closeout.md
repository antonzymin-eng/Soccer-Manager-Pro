# W6 Controlled Ball — Wiring Closeout

**Date:** 2026-09-15
**Status:** IMPLEMENTED; REVIEW CLOSURE COMPLETE — owner-held close-chance RED remains a separate calibration/disposition item
**Scope:** Match-engine wiring backlog W6 only. W2 tackle activation remains a separate post-W6 evidence decision.

## Closed implementation boundary

`BallStateType.Controlled` now has production entry for genuine open-play possession. MatchEngine continues to use `_possessingAgentId` for both physical possession and restart-taker designation, but the Ball Physics state distinguishes the two: open-play possession is `Controlled`; a placed restart ball remains `Stationary`.

The production grant paths are first-touch control/interception, loose-ball pickup, tackle ball-won, and goalkeeper possession. Controlled balls are attached to their holder after movement and goalkeeper/heading physics. Outfield control uses ball-rest height; goalkeeper control preserves the actual claim/contact height while x/y follow the keeper.

Non-kick physical release now exits `Controlled` explicitly. Kicks already leave control through `BallCollision.ApplyKick`; tackle-loose and the six-second goalkeeper release use the explicit release transition. Restart pseudo-possession is not tackleable because the tackle resolver requires a physically `Controlled` ball.

No new durable field, latch, schema version, RNG stream, reservation, draw site, or draw order was introduced by W6.

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
9. The composed keeper-claim scenario proves a claim arrests the incoming ball and the held `Controlled` ball remains attached to the claiming keeper.
10. The composed keeper-claim scenario asserts the independent football consequence that an observably held claim does not end in a keeper-carried own goal.
11. A direct two-goal-plane regression constrains a controlled keeper at either defended goal plane and keeps carrier/ball/recovery kinematics coherent.

The two test-only tackle-cooldown seams added for item 8 are retained deliberately as durable regression support. They are not production state and are not temporary measurement scaffolding.

## Recovered Codex P2 — closed

The Codex review correctly identified that the physical-carrier early return could occur before `_tackleCooldown` aging, freezing an elapsed-AI-stride cooldown while the ball was loose, airborne, or placed for a restart.

Commit `f927e115b5e61771b5cf0fd7c07e93514759b1b6` moved cooldown aging before the physical-carrier gate and added `LooseBall_DoesNotFreezeElapsedTackleCooldown`. The PR review thread is resolved. This tackle cooldown is distinct from the W12 Pressing-AI `Cooldown` exit driven by `DisengageResolver` / `_cooldownTicks`.

## Keeper-held carry defect — closed

The review state that triggered closure measured **2 of 17 claims** ending with the holding keeper carrying the attached `Controlled` ball through his own goal line. That was a W6-exposed locomotion/carry defect, not accepted behavior: W6 made physical possession a kinematic constraint while Agent Movement still permits a general 5 m exterior safety buffer.

Commit `0c065b38cd8e2a3a6d228bd9ba6b6b8f502ce6b8` closes the defect at the MatchEngine composition seam `DriveControlledBallToPossessor()`. When the physical `Controlled` holder is a goalkeeper and locomotion has put that carrier behind the goal plane it defends, MatchEngine clamps the carrier back to that plane before attaching the ball, clears only velocity that points farther outside, recomputes `Speed`, and refreshes `LastValidPosition` / `LastValidVelocity`. The general Agent Movement buffer and legitimate goal adjudication for loose/kicked balls are unchanged.

Commit `3e00cd4c2801c3d245bac537bf112a5b4dd7c2be` adds the direct two-goal-plane regression. Final focused W6 coverage is **9 passed / 0 failed / 0 skipped**, and the exact composed `sim_match_engine_keeper_claim` scenario is **1 passed / 0 failed / 0 skipped** after the production fix. The structural attachment predicate and independent own-goal consequence predicate therefore both remain live rather than being weakened around the defect.

## Owner-held RED observation — measured; disposition unchanged

The ordinary PR functional sweep on head `91eca232b758997a2914dd82b7c65330d8d1ffb4` completed `MatchEngine.Tests` at **502 passed / 0 failed / 12 skipped**. The only failing job conclusion was the separately verified owner-held `sim_match_engine_close_chance` unexpectedly passing; the Linux functional job is explicitly non-certifying and is not a required branch-protection context.

The merge-candidate close-chance population was then measured directly against current `main` `1ba13e071909b5a08ead6cf5ed50a995f8dc301d` with the existing six-seed close-chance diagnostic and workspace-only exact counters (Actions run `35045360775`; no measurement patch entered this PR):

| population | `main` | W6 #412 |
|---|---:|---:|
| exact owner-held pair — mean cosine | **-0.165** | **+0.125** |
| exact owner-held pair — goalward share | **0.407** | **0.598** |
| six seeds pooled — mean cosine | **-0.180** | **+0.061** |
| six seeds pooled — goalward share | **0.381** | **0.583** |

Five of six diagnostic seeds move goalward under W6 and one worsens. This is therefore a broad trajectory/population shift, not merely a lucky selected-pair green. It is **not** evidence that W6 repaired the DRIBBLE direction-scoring mechanism: W6 changes carrier/ball trajectory coupling and does not modify that scorer. `MatchEngineCloseChanceScenarios` explicitly defines the two-seed statistic as a **floor, not an estimator**, warns that trajectory-changing code resamples it, and warns against pricing a different change off the same selected pair.

The standing August 11 owner decision therefore remains: **hold red; do not rebaseline a third time**. This landing does not remove the owner-held entry, widen either bound, change expected diagnostics, or claim close-chance calibration is resolved. The non-required functional verifier may remain red solely because the held-red scenario is unexpectedly green; that calibration/disposition belongs to a separate realism pass.

## Pre-registration carried forward before measurement

`docs/tracking/w6-controlled-ball-preregistration.md` was carried unchanged from `wiring/w12-evidence-repair` into PR #412 before post-W6 measurement. Its locked baseline, prediction thresholds, and falsifier remain the pre-registered hypothesis rather than a post-result interpretation.

Because the file is preserved verbatim, its final paragraph still records the cooldown P2 as unresolved at pre-registration time. The resolution above is the later closeout record; the historical pre-registration is not rewritten after the fact.

## Owner sequence after this closeout

1. Merge PR #412 once the live required contexts are green. The owner-held Linux functional verifier is non-required and its close-chance RED remains unchanged under the explicit owner disposition above.
2. Run the post-W6 W2 measurement, record the activation/non-activation decision, and remove the temporary dispatch branch.
3. Salvage `wiring/w12-evidence-repair` onto fresh `main`; port additive evidence/checker/CI material and re-derive any comparison that is not already current rather than importing stale measurement conclusions.

This sequencing affects procedure only. It does not retroactively alter either pre-registered hypothesis.

## Scaffolding disposition

No temporary W6 measurement workflow, evidence corpus, diagnostic workflow, or ad-hoc instrumentation file remains in the PR #412 diff. Review-time and closeout-time evidence workflows live only on temporary branches and are not part of the landing.

## W2 boundary

W6 does **not** activate W2 or change the governed shipping value of `TackleContactRadiusM`. The next step after PR #412 is evidence: rerun the armed tackle corpus/composed-match measurement and make the W2 activation decision separately. A state-model correctness fix must not smuggle in an unmeasured balance change.
