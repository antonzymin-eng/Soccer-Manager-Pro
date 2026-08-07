# Close-Chance Creation — the final third → penalty area transition

> **Created:** August 4, 2026
> **Class:** DESIGN SUPPLEMENT (class (b) — governs a surface that is not a numbered spec; the pass
> itself is recorded as `match-engine-design.md` §5.Z.24)
> **Status:** LANDED — one formula defect fixed (ERR-008-018) at a deliberately conservative `[GT]`.
> **The creation gap itself is NOT closed**, and §7 item 1 names what actually owns it.
> **Owner pass:** §5.Z.24. Predecessor: `gk-conversion-at-contact-design.md` §7 item 4 (§5.Z.23),
> which re-localized §5.Z.21's "possession churn" residual to this stage.
> **Instrument:** `src/match-engine/tests/CloseChanceDiagnosticTests.cs` (env-gated,
> `TD_CREATION_DIAGNOSTIC=1`)

---

## 1. The premise check

The brief carried two premises. Neither had been measured.

**Premise (a) — "306.7 final-third entries per match against football's ~110".** That is a raw
boundary-crossing count; a ball oscillating across x = 35 would inflate it without any football
happening, and an inflated denominator would manufacture the headline 6.5%. **The premise SURVIVED.**
Re-counted with a 1 s exit dwell over six full matches: **311 episodes against 312 raw crossings**,
each averaging 5.1 s. The crossings really are distinct spells. This is the first premise in this
seven-pass chain to survive its own check, and it is recorded so no later pass re-opens it.

**Premise (b) — "the final third → penalty area transition is the bottleneck".** True as a
*location*, but it names no mechanism, and two mechanisms would produce it while wanting opposite
fixes: nobody is in the box to receive (support geometry — #12/#15), or somebody is and the carrier
never plays them in (decision — #8). Both were measured. **Both are real, and neither is what closes
the gap** — see §7 item 1.

---

## 2. The finding

Six full 90-minute matches, `ConfigureSquads` path, at `7fcd897`.

**C2 — the box is empty, and nobody is being asked to enter it.**

| | measured | football |
|---|---|---|
| mean attacking outfielders inside the penalty area, ball in the final third | **0.11** | 2–4 |
| samples with **zero** attackers in the box | **92%** | — |
| depth of the most advanced attacker (from the attacked goal line) | 22.2 m | — |
| depth of the most advanced **composed target slot** | **22.8 m** | — |
| samples where any target slot is inside the area | 5% | — |

The last row convicts. Players are within 0.6 m of where they are told to be — they are not slow,
they are **never asked into the box**. Read against source: the 4-4-2 ST anchor is
`0.78 × 105 = 81.9 m`, and #12's ball-relative offset adds at most `pull.x(0.60) × basisX × 12 m`,
reaching the area edge only for a ball already on the goal line.

**C3 — and the carrier walks the ball back out.**

| carrier decision in the attacking third | share |
|---|---|
| **DRIBBLE** | **40%** (modal) |
| HOLD | 20% |
| PASS | 17% |
| SHOOT | 5% |

| | measured |
|---|---|
| mean cosine between the chosen dribble direction and the direction to goal | **−0.302** |
| dribbles pointing goalward at all | **31%** |
| mean pass "gain" (carrier's distance to goal − target's) | −19.6 m |
| passes whose target is inside the penalty area | **1%** |

**The average dribble in the attacking third points away from the goal**, and the dribble is the
modal action. Localized against source, this is the ERR-008-017 shape — a formula missing the term
it should be dominated by:

- **ERR-008-018.** #8 §3.1.5.2 chooses `best_direction = argmax(space_in_dir)` and closes with *"No
  backward-sector penalty is applied to `SpaceScore` at generation time; the scoring stage (§3.2.2)
  applies directional-to-goal modifiers to the DRIBBLE utility."* §3.2.4.1 — DRIBBLE's actual
  scoring section — has no such factor, and the cross-reference points at **§3.2.2, the PASS
  formula**. The promised term never had a home. `SpaceScore` is direction-blind by construction, so
  a dribble toward halfway scored exactly as well as the same dribble at goal; and in the final
  third, where the free space is behind the carrier, that is what the argmax picks.

The pass numbers share C2's root rather than having their own: #8 §3.1.3 generates one PASS
candidate per visible teammate **at that teammate's current position**, so with the box empty there
is nothing in it to pass to.

---

## 3. Key decisions

| id | decision |
|---|---|
| **KD-CC1** | Re-count final-third entries as dwell-filtered episodes before trusting the 6.5%. The raw count was sound (312 raw vs 311 episodes) — recorded so it is not re-litigated. |
| **KD-CC2** | Fix ERR-008-018 by adding `DirectionQuality_DRIBBLE` to §3.2.4.1, using the same linear-in-cosine shape as §3.1.3.5's PASS `GOAL_DIR_MIN_MODIFIER`. |
| **KD-CC3** | The term SUPPRESSES a retreating dribble; it does not redirect it. The generator emits exactly one DRIBBLE candidate, so the scoring stage can only decide how hard that candidate competes with PASS / SHOOT / HOLD. Redirecting needs a multi-direction generator — out of scope (§7 item 6). |
| **KD-CC4** | A degenerate `BestDribbleDirection` (the zero vector — what every direct-injection test option carries) resolves to the exact ×1.0 identity, not the perpendicular midpoint. The ERR-008-017 / KD-V3 contract restated; it is what keeps all 22 pre-existing `UtilityScorerTests` bitwise unchanged. |
| **KD-CC5** | Widen the corpus 3 → 6 seeds **before** fitting any dial. At 3 seeds a single rung spanned 15–65 shots and produced a 1–10 scoreline. The §5.Z.23 AR-1 finding one level up: there the estimator's *window* was too thin for a mean, here the *corpus* was too thin for a ladder. |
| **KD-CC6** | Land the floor at **0.80**, not at the 0.50 that maximises the effect. Lower floors introduce a stall (§8) — the value is bounded by a defect in a different action, and that is recorded rather than absorbed. |
| **KD-CC7** | Implement #15 §4.5.2's run overlay, measure it, and refuse to land it (§4). |
| **KD-CC8** | Assert only the mechanism's own signature in the acceptance scenario. Goal rate, shot count and box occupancy are all deliberately unpinned — §6 explains that none of them moved in a way this corpus supports. |

---

## 4. The probe that was refused — #15 §4.5.2's run-target overlay

#15 §4.5.2 declares a five-step composition rule: #12 publishes a baseline slot, the orchestrator
reads each `AttackIntent`, and where the role is RUNNER the orchestrator overrides that slot with the
run target. It is marked *"Stage 1+ — declared, not implemented"*, and its stated precondition — the
interface is produced once both #12 and #15 are `APPROVED` — is now satisfied. So it was implemented
in the composition root exactly as specified, and measured in isolation (the `[GT]` floor at 1.0,
which is the exact ERR-008-018 identity, so this rung is lever A alone).

**Its own mechanism works. Nothing downstream improves.**

| | baseline | + run overlay |
|---|---|---|
| committed RUNNER's target depth from the attacked goal | **80.9 m** | **14.7 m** |
| mean attackers in the penalty area | 0.11 | **0.08** |
| ball entering the box (% of episodes) | 6% | 5% |
| shots / match | 19.3 | 15.0 |
| goals / match | 3.67 | 2.00 |

The overlay does exactly what it says — it moves the runner's target from deep in its own half to
inside the penalty area — and box occupancy goes **down**. The reason is structural, not a tuning
miss: a RUNNER's target is `ball carrier + 12 m`, and the carrier is usually still in midfield, so
the overlay replaces a forward's deep formation slot with a *shallower* carrier-relative one. It
encodes "support the carrier", not "attack the box", and at Stage 0 those conflict.

Two further measured facts show it cannot be rescued by tuning:

1. **Its gate is almost never open.** While the ball is in the final third, #12's committed phase is
   `TransToAtk` **58.3%**, `OutOfPoss` 16.0%, `TransToDef` 16.2%, and `InPoss` only **9.5%** —
   because `PossessionOwnerEntityId >= 0` is false for the whole flight of every pass. A team
   knocking the ball around is classified as being in transition, so #15 emits run parameters on 4%
   of samples.
2. **It destabilises the `[GT]` ladder.** With the overlay on, mean box occupancy across the floor
   ladder reads 0.08 / 0.12 / 0.58 / 0.37 at 1.0 / 0.8 / 0.65 / 0.5 — non-monotone, with a rung
   producing 33 shots a match and a 1–10 scoreline. Without it the same ladder is monotone (§8).

Landing it would swap a measured defect for a guessed one — the §5.Z.23 `pointQuality` precedent.
The code is reverted; a pointer comment sits at the composition site so the next reader does not
re-derive it from the spec text. Two defects found while building it are recorded in §7 (items 3 and
4) because they are real whether or not the overlay ever lands.

---

## 5. Acceptance

`match-engine-close-chance` (#19 ScenarioRunner, Tier B, 2 seeds × 90 min). Full matches, not
windows — the §5.Z.23 AR-1 finding applies: these distributions are not stationary within a match.
The two seeds are chosen for **margin**, from per-seed pre/post measurements, not for traffic;
picking the busiest seeds would have put the scenario on the corpus's two worst separators.

| predicate | bound | pre-fix | post-fix |
|---|---|---|---|
| `final-third-dribbles-are-sampled` | ≥ 200 | passes (non-vacuity) | passes |
| `final-third-dribbles-are-not-goal-averse` | mean cosine > −0.10 | **−0.291 FAIL** | +0.083 |
| `goalward-dribbles-are-not-a-minority-of-one-in-three` | share > 0.42 | **0.306 FAIL** | 0.52 |

**2 of 3 predicates fail on the pre-fix engine, verified by executing the scenario in a worktree at
`7fcd897`** — not inferred. The bounds sit in the gap between the pre-fix per-seed maximum (−0.211
cosine, 36% goalward) and the post-fix per-seed minimum across the whole six-seed corpus (−0.128,
40%), so they discriminate on every seed, not only the two the scenario runs.

It pins **no goal rate, no shot count and no box-occupancy figure** — see §6.

Unit locks: 4 new `UtilityScorerTests` — the unset-direction exact identity (which is also the proof
that the other 22 fixtures in that file are untouched), monotonicity in the cosine, the exact
floor/midpoint ratios, and the guard that the DRIBBLE floor stays deliberately weaker than the PASS
one with the stall evidence cited.

---

## 6. Measured result

Six full matches, `ConfigureSquads` path, identical seeds pre/post, floor 0.80.

**What moved — on all six seeds, with no overlap between the pre- and post-fix distributions:**

| per-seed | pre-fix cosine | post-fix cosine | pre goalward | post goalward |
|---|---|---|---|---|
| `0x0F1E…6978` | −0.221 | +0.074 | 36% | 52% |
| `0x…D1A6D05E` | −0.348 | +0.091 | 26% | 53% |
| `0x5EED…0003` | −0.448 | −0.128 | 26% | 40% |
| `0x5EED…0004` | −0.232 | −0.050 | 34% | 50% |
| `0x…D1A6D05F` | −0.211 | +0.037 | 36% | 54% |
| `0x1A2B…7081` | −0.332 | +0.002 | 31% | 46% |
| **pooled** | **−0.302** | **+0.006** | **31%** | **49%** |

Carrier action mix moves with it: DRIBBLE 40% → 33%, HOLD 20% → 23%, PASS 17% → 19%.

**What did NOT move — and is therefore not claimed:**

| | baseline | post-fix |
|---|---|---|
| mean attackers in the penalty area | 0.11 | 0.10 |
| ball entering the box (% of episodes) | 6% | 5% |
| passes into the box | 1% | 0% |
| shots / match | 19.3 | 19.5 |
| mean shot distance | 19.2 m | 19.5 m |
| goals / match | 3.67 | 3.50 |
| final-third episodes / match | 311 | 317 |

**The creation gap is not closed and the residual shot-count gap is not closed.** This pass fixes a
formula that provably omitted a term its own spec promised, and it moves the quantity that term
governs, on every seed. It does not move the funnel.

**A pooled number nearly carried a false claim, and the acceptance scenario is what caught it.** At
floor 0.50 the corpus reads mean box occupancy 0.11 → **0.59** and ≥2-attackers-in-box 3% → **28%**,
which looks like the creation fix the brief asked for. It is not: five of the six seeds read
0.10 / 0.06 / 0.15 / 0.13 / 0.12 — flat — and the entire pooled movement comes from one seed reading
**1.62** while contributing 32% of all samples, because that match **stalled** (mean final-third
episode length 28.6 s against a healthy 5.1 s). The scenario, running two other seeds, failed the
box predicate at 0.043 and forced the per-seed breakdown that exposed it.

**Determinism.** No `SNAPSHOT_SCHEMA_VERSION` change, no new RNG stream, no new domain tag, no new
draw site, no draw-order change. `DirectionQuality_DRIBBLE` is a pure function of current-tick option
and context state. Digests move for any match containing a dribble decision — a behaviour change, as
intended.

**Blast radius.** Goal rate moved 3.67 → 3.50 (inside noise), so `round-resolution-corpus.md`'s
existing Step-0 re-run requirement is unchanged, not newly triggered. Per-tick cost is one dot
product and one lerp per DRIBBLE option, of which there is at most one per agent per heartbeat — the
FR-PO-052 certified perf baseline is untouched.

**Gate.** Full `tools/dotnet-ci/run-gate.sh`: **PASSED, 0 failures** — whole tree green across 30
assemblies, quarantine empty so the full suite is enforced. Match-engine 369 passed / 9 env-gated
diagnostics skipped in 48 m 27 s (the ~22 min over §5.Z.23 is this pass's own 2-seed × 90-min
acceptance scenario); decision-tree 101 passed / 4 skipped. The away-team mirror lock was added after
that run began — per the CLAUDE.md "home-team-only worked examples" trap, since three home/away
asymmetry defects (#8 ERR-008-002) shipped that way — and is covered by re-running the decision-tree
suite in the gate's own Debug configuration: **102 passed / 4 skipped, 0 failures**.

---

## 7. Recorded, NOT fixed

1. **The real bound on ball-into-box is that #8 cannot pass to a place — only to a player.**
   §3.1.3 generates one PASS candidate per visible teammate **at that teammate's current position**.
   There is no pass-into-space, no through-ball-to-a-run, no cross-to-an-arriving-header. So the ball
   can only enter the penalty area if a teammate is *already standing in it at the moment of the
   decision* and the lane happens to be clear — measured, passes into the box are **1%** and stayed
   there through every rung of the ladder, including the rungs where players did reach the box. This
   is the next lever and it is bigger than a `[GT]`: it needs a PASS candidate whose target is a
   *position* rather than an agent.
2. **HOLD has no timeout, and suppressing the dribble exposes it.** A carrier with no pass, no shot
   and no viable dribble can select HOLD indefinitely. HOLD share rises with the strength of the new
   term (20% → 23% at floor 0.80 → 31% at 0.50), and at floors 0.65 and 0.50 one seed in six stalled
   outright: mean final-third episode length 5.1 s → **17.5 s** and **28.6 s**. This is what bounds
   `DRIBBLE_GOAL_DIR_MIN_MODIFIER` at 0.80 rather than the 0.50 that would match the PASS floor. It
   is a #8 §3.1.6 / §3.2.5 surface.
3. **#12 commits `InPoss` only 9.5% of the time the ball is in the final third** (`TransToAtk`
   58.3%). `PhaseClassifier.ComputeCandidate` keys on `PossessionOwnerEntityId >= 0`, false for the
   entire flight of every pass, so a team passing the ball around reads as being in transition. Every
   phase-gated mechanism in #13/#14/#15 — including the whole #15 run pipeline — is gated behind a
   state the engine rarely occupies. A #12 §3.0.2 change with a blast radius across four consumers.
4. **#15's TRANSITION branch never republishes per-agent intents.** `AttackingAITick.Tick` freezes
   the *directive* and returns, leaving `_intentBuffer` / `_entityToIntentIdx` holding the previous
   IN_POSSESSION stride's intents, which `GetIntent` then serves with a stale `ValidThroughTick`. The
   contract is satisfiable — a consumer must test that field — but it has no consumer today and will
   trap the first one that arrives. Not filed as an ERR because the fix (republish, or clear as
   `SetEmpty` does) is a design choice #15 should make deliberately.
5. **`RunParameters.RunTriggerTick` is inert by construction.** `RoleAssigner.Assign` regenerates run
   parameters for every committed RUNNER every heartbeat, so the trigger tick is always re-stamped
   `currentTick + delay`; a consumer that waited for it would never fire. A timed run commit needs
   the parameters latched at assignment first.
6. **The dribble is suppressed, never redirected** (KD-CC3). §3.1.5.3 emits a single candidate — the
   free-space argmax — so the scoring stage cannot steer it, only decide how hard it competes. A
   generator emitting the best *goalward* sector alongside the best *free* one would let the tree
   choose between them; that is a §3.1.5 change, not a `[GT]`.

---

## 8. The ladder

Six seeds × 90 min per rung, identical seeds across rungs. `floor = 1.0` is the exact term-off
identity (`dirQ ≡ 1.0`). The run overlay is OFF on every rung below (§4 measures it separately).

| floor | dribble cosine | goalward | DRIBBLE share | HOLD share | shots | goals | seeds stalled |
|---|---|---|---|---|---|---|---|
| 1.0 (off) | −0.302 | 31% | 40% | 20% | 19.3 | 3.67 | 0 / 6 |
| **0.80** | **+0.006** | **49%** | **33%** | **23%** | **19.5** | **3.50** | **0 / 6** |
| 0.65 | +0.253 | 65% | 25% | 28% | 21.5 | 4.33 | **1 / 6** (17.5 s) |
| 0.50 | +0.476 | 82% | 22% | 31% | 23.7 | 4.33 | **1 / 6** (28.6 s) |

Monotone in the floor on every column — which is what makes 0.80 a defensible landing point rather
than a lucky sample, and what makes the stall column readable as a consequence of the dial rather
than as noise. "Stalled" = a seed whose mean final-third episode length leaves the healthy 4.5–5.6 s
band; the two stalls are different seeds, and each contributes ~32% of that rung's samples, which is
how a stall can dominate a pooled statistic (§6).

**The ladder refuses the creation target.** No floor produces both a healthy corpus and a materially
better funnel: the rungs that move box occupancy are exactly the rungs that stall, and even at those
rungs passes into the box stay at 1%. That refusal is the evidence for §7 item 1 being the real
lever, and it is more useful than a fitted number would have been.

---

## 9. Adversarial review history

| Round | Findings | Notes |
|---|---|---|
| Premise-1 (dwell-filtered re-count) | — | **Premise survived** — 312 raw crossings vs 311 episodes. The 6.5% denominator was not chatter. First surviving premise in the seven-pass chain; recorded so it is not re-opened |
| Measurement-1 (support geometry) | — | Localized the support half to the composed slot, not to player speed: deepest attacker 22.2 m vs deepest **target** 22.8 m. The players are where they are told to be, and no slot is inside the area |
| Self-1 (instrument correctness) | 1 defect in the INSTRUMENT | The first C2 cut pooled settled attacks with "the ball is in this third and nobody owns it" — football does not fill the box in the second case either. Added the IN_POSSESSION-conditional row, which is also what exposed the 9.5% phase finding (§7 item 3) |
| Self-2 (consumer contract) | 1 defect in the PROBE | The lever-A overlay consumed `GetIntent` without testing `ValidThroughTick`, so it steered runners at a carrier who no longer had the ball for the whole transition window. Found by reading #15's TRANSITION branch, not by a failing test — it has no consumer to fail. Fixed in the probe; recorded as §7 item 4 |
| Ladder-1 (3 seeds) | — | **The ladder REFUSED to resolve**: one rung spanned 15–65 shots with a 1–10 scoreline, and rung-to-rung differences were non-monotone. Corpus widened 3 → 6 seeds before any value was chosen (KD-CC5) |
| Ladder-2 (6 seeds, paired) | 1 finding against LEVER A | The run overlay moves its runner's target 80.9 m → 14.7 m and moves box occupancy 0.11 → **0.08**. Refused on the measurement, not on an argument |
| Acceptance-1 (predicate executed post-fix) | **1 defect in this pass's own CONCLUSION** | The scenario's box predicate FAILED post-fix at 0.043 against a bound set from the pooled 0.28. The per-seed breakdown it forced showed the pooled figure was **one stalled match** (1.62 over 32% of samples) with the other five seeds flat. The creation claim was withdrawn, the floor moved 0.50 → 0.80 on the stall evidence, and the box predicate was deleted rather than re-tuned — a predicate whose bound has to be lowered to pass is measuring nothing |
| Acceptance-2 (pre-fix execution) | — | 2 of 3 predicates fail at `7fcd897` by execution (cosine −0.291 vs −0.10; goalward 0.306 vs 0.42), with the non-vacuity predicate passing |
| Acceptance-3 (main run 419 fallout — the cosine predicate tripped at the ERR-008-021/-022/-023 merge; owner-approved rebaseline) | 1 regression RECORDED, not fixed | The shot-lane chain moved the pooled cosine to **−0.119** vs the −0.10 bound — and per-seed measurement shows the regression is **one seed, entirely**: 0x0F1E…78 held its gain (+0.078 / 110 dribbles, vs +0.074 post-fix) while 0xD1A6D05E gave back the whole ERR-008-018 flip (**−0.232** / 192 dribbles, vs −0.221 pre-fix and +0.091 post-fix). The goalward share held at 0.450 pooled but only because the healthy seed carries it (0.564 / 0.385 — the regressed seed is below the 0.42 bound alone). Bound moved −0.10 → −0.16 (owner call, August 7, 2026), still refusing the pre-fix pooled ≈ −0.29; share bound unchanged with its thinned margin recorded in the scenario. The pull-back belongs to the KD-W1 calibration pass — the chain's own P5 residuals (the withdrawn -021 population-preserving claim, -022's uncalibrated added blockers) are the suspects, and re-tuning them here would repeat the exact mistake -023 exists to record. Scenario v1.1 |

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 1.0 | 2026-08-04 | — | Initial: implemented + measured. §1 both premises checked (the entry-count premise SURVIVED — the first in this chain); §2 the finding (box empty at 0.11 attackers, no target slot inside the area, and the average final-third dribble pointing AWAY from goal at cosine −0.30) and ERR-008-018; §3 KD-CC1..CC8; §4 the #15 run overlay implemented, measured and refused (its runner target moves 80.9 → 14.7 m while box occupancy falls 0.11 → 0.08); §5 acceptance, 2 of 3 predicates failing pre-fix by execution; §6 the measured result — a 6-of-6 per-seed flip in dribble direction, and an explicit list of what did NOT move, including the withdrawn box-occupancy claim that turned out to be one stalled match; §7 six recorded residuals headed by "#8 cannot pass to a place, only to a player", which now owns the ball-into-box stage; §8 the monotone ladder with the stall column that bounds the [GT] at 0.80. |
| 1.1 | 2026-08-07 | — | Acceptance-3 (§9): the cosine predicate tripped at the ERR-008-021/-022/-023 main merge, pooled −0.119; per-seed the regression is entirely seed 0xD1A6D05E (−0.232 — its whole ERR-008-018 gain returned) while 0x0F1E…78 held (+0.078). Bound rebaselined −0.10 → −0.16 by owner call; share bound unchanged, margin thinned to 0.030. The regression is RECORDED for the KD-W1 calibration pass, not re-tuned here. `MatchEngineCloseChanceScenarios.cs` v1.1. |
#endregion
