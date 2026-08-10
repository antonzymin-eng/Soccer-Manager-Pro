# Close-Chance Creation — the final third → penalty area transition

> **Created:** August 4, 2026
> **Class:** DESIGN SUPPLEMENT (class (b) — governs a surface that is not a numbered spec; the pass
> itself is recorded as `match-engine-design.md` §5.Z.24)
> **Status:** LANDED — one formula defect fixed (ERR-008-018) at a deliberately conservative `[GT]`.
> **The creation gap itself is NOT closed.** §7 item 1 named what owns it; **§10 (August 9, 2026)
> retracts that ranking on re-measurement** and puts two bounds ahead of it — read §10 first, and
> treat every figure in §2, §4 and §8 as pre-C1 unless §10.1 restates it.
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

1. ~~**The real bound on ball-into-box is that #8 cannot pass to a place — only to a player.**~~
   **PRIORITY CLAIM RETRACTED August 9, 2026 — see §10.** The *finding* stands and the candidate
   type is still missing; what is withdrawn is "the real bound". Re-measurement after the C1 phase
   fix found two bounds ahead of it, and the second one makes this item actively harmful to land
   first: **44% of final-third passes are already aerial and they complete 1% of the time**, because
   no agent in the engine can receive a ball above 0.5 m. Adding a candidate type that plays *more*
   balls into space adds to that bucket. The original text follows unedited.
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
3. **~~#12 commits `InPoss` only 9.5% of the time the ball is in the final third~~ — FIXED August 8,
   2026 as `ERR-012-011`** (wiring backlog C1). `PhaseClassifier.ComputeCandidate` keyed on
   `PossessionOwnerEntityId >= 0`, false for the entire flight of every pass, so a team passing the
   ball around read as being in transition. Phase now classifies from TEAM possession — the on-ball
   carrier's team, else the intended receiver of a pass in flight. **Two corrections this document
   owes its readers.** First, the "every phase-gated mechanism in #13/#14/#15 is starved" framing
   above is materially wrong: `TacticalContext.HasAttackIntent` has no production reader at all, so
   #15 is inert independent of its gate, and #13's press targets have no consumer outside
   `pressing-ai`. Second, and against this document's own hopes for the funnel, the fix was
   *predicted to make box occupancy slightly worse* — #12's `PullFactor` `InPoss` column is less
   advanced than the `TransToAtk` column it replaces for every attacking role. The C1 fix is a
   correctness fix and a precondition for calibration; it is not the creation lever. Item 1 below
   (#8 cannot pass to a place) remains that lever.
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
6. **The dribble is suppressed, never redirected** (KD-CC3). §3.1.5.3 emits a single candidate —
   the free-space argmax — so the scoring stage cannot steer it, only decide how hard it competes. A
   generator emitting the best *goalward* sector alongside the best *free* one would let the tree
   choose between them; that is a §3.1.5 change, not a `[GT]`. **REOPENED August 9, 2026 — a fix was
   implemented as `ERR-008-024`, measured, and REFUSED (the KD-CC7 pattern; see §4 for the
   precedent).** Rather than a generator emitting TWO competing candidates (goalward + free, which
   §3.1.5.3 would then need scoring rules to arbitrate between, and which would grow the fixed-size
   `ActionOption` buffer this assembly shares across every action type —
   `DecisionTreeConstants.MaxOptions` = 17 — by a second DRIBBLE slot), §3.1.5.2's single-candidate
   scan was changed to RANK its 8 sectors on `spaceInSector × DirectionQuality_DRIBBLE(sectorDir,
   toGoal)` instead of `spaceInSector` alone, reusing the exact term §3.2.4.1 already applies at
   scoring (no new constant — the floor is `DRIBBLE_GOAL_DIR_MIN_MODIFIER` = 0.80, unchanged). Root
   cause, found only once this residual was chased down: `spaceInSector` saturates at exactly 1.0
   for any sector with no opponent within `DRIBBLE_THREAT_RADIUS`, and the old scan's strict `>`
   improvement test therefore always keeps the FIRST sector visited — sector 0,
   `AgentFacingDirection` by construction — whenever two or more sectors are clear, the common case
   in the final third. The carrier dribbles wherever he already faces; goal direction has no
   influence on the choice at all, which is exactly why KD-CC3's scoring-only fix could suppress a
   retreating dribble but never redirect it.
   **This DOES fix the symptom:** `sim_match_engine_close_chance` — meanCosine −0.165 → **PASS**
   (bound −0.16, unmoved), goalwardShare 0.407 → **PASS** (bound 0.42, unmoved).
   **But the same build stalls play outright:** `sim_match_engine_play_develops` fails with "play
   stalled: last possession change at tick 18424, ball last moving at tick 18465 of 32400", and
   `sim_match_engine_shot_outcomes` fails `goals-still-scored` at **0**. A WIDER form ranking on
   `space × DirectionQuality` outright (not as a tie-break) produced the **identical** stall at the
   **identical tick**, plus mean-shot-distance 25.41 m against a 24.00 m ceiling — that identity is
   what localises the cause to the tie-break itself, not to how much space either form trades away.
   **Refused, not landed.** `OptionGenerator.cs` reverted to the pre-fix baseline logic (a
   comment-only diff against the pre-ERR-008-024 commit). Kept, behaviour-neutral: the
   `DirectionQuality_DRIBBLE` formula hoisted to `UtilityWeights.DribbleDirectionQuality(Vector2,
   Vector2)` with `UtilityScorer` delegating to it. The two §3.1.5.2 unit locks the attempted fix
   added are **REMOVED** — they locked behaviour that no longer exists. `DecisionTree.Tests`
   **129 passed / 4 skipped / 0 failed.** Sending the dribble goalward is only safe once §10.2/§10.3's
   blockers are addressed — see §10.5. See `spec-error-log.md` ERR-008-024 and
   `decision-tree/section-3-1.md` §3.1.5.2.

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

## 10. The post-C1 re-measurement — two bounds ahead of §7 item 1

> **Added:** August 9, 2026. **Status:** measurement complete, no mechanism landed.
> **Instrument:** `CloseChanceDiagnosticTests.cs` v1.2 (`TD_CREATION_DIAGNOSTIC=1`), 6 seeds ×
> 90 min. Council convened before any code: `advisor-integrity` ×2, `advisor-evidence` ×1.

**Why this section exists.** Every number in §2, §4 and §8 was measured before `ERR-012-011`
(wiring-backlog C1) landed on August 8. That fix moved final-third possession-phase share from
24.2% to 96.8%, so it changed the population every one of those numbers was drawn from. The
evidence advisor's first instruction was to re-measure before designing against them. That was
done, and it refuted this document's own ranking.

### 10.1 What re-measurement changed

| quantity | §2 (pre-C1) | re-measured (post-C1) | football |
|---|---|---|---|
| mean attacking outfielders in the penalty area | 0.11 | **0.02** | 2–4 |
| samples with zero attackers in the box | 92% | **98%** | — |
| most advanced attacker, from the attacked goal line | 22.2 m | **25.2 m** | — |
| most advanced composed **target slot** | 22.8 m | **25.7 m** | box edge 16.5 m |
| final-third episodes reaching the box | 5–6% | **5%** | ~40% |
| passes whose target is inside the box | 1% | **0%** | — |
| mean final-third pass gain toward goal | not measured | **−20.16 m** | ≈ −2 to +3 |

The support geometry got *worse*, exactly as §7 item 3 predicted it would.

### 10.2 Bound A — the last 17 metres are not playable space, for either side

The players are within 0.5 m of their targets (25.2 m actual against 25.7 m composed). They are not
slow to arrive; they are never asked. And this **cannot be fixed by tuning**, which is the part that
was not previously established:

- F442's most advanced anchor is the ST at `longPct = 0.78` → 81.9 m → **23.1 m from goal**
  (`PositioningAIConstants.cs`, `AnchorCalculator.ComputeAnchor`).
- The ball-relative offset is `pull.x × basisX × OFFSET_RANGE_X_M`, with `OFFSET_RANGE_X_M = 12.0`
  and `basisX = (ball.x − 52.5)/52.5` (`AnchorCalculator.ComputeBallRelativeOffset`).
- With the ball 25 m from goal (`basisX` = 0.524) the ST slot lands at **19.3 m**. At a hypothetical
  `pull.x = 1.0` — above every value in the table — it still only reaches **16.8 m**. Reaching the
  16.5 m line at the shipped 0.60 needs the ball within **4.4 m of the goal line**.
- Both terms are capped, so reshaping the basis curve changes only *when* the cap is reached. Fully
  saturated the ST reaches 15.9 m with zero depth, where football wants 6–12 m.

The defensive mirror is the same arithmetic: CB anchor `0.20` → 21.0 m, maximum OutOfPoss retreat
`0.30 × 12` = 3.6 m, so the block bottoms out at **17.4 m from its own goal** even with the ball on
that goal line. All twenty outfield players are therefore confined to a band of roughly 17–25 m.

**Sequencing consequence, and it inverts the obvious order.** `EvaluateAndApplyOffside` is live on
the reception path (`MatchEngine.cs`), and with the ball 25 m out the defending CB slot computes to
19.1 m. Placing attackers at a realistic 11 m while the block sits at 19.1 m makes them offside on
every completed pass, permanently. **The defensive block must drop first**: it is independently
correct football, it moves the offside line back, and it carries no unmarked-attacker risk because
the attackers are not there yet.

### 10.3 Bound B — every aerial pass fails, and 44% of final-third passes are aerial

This was never measured before, because **no instrument in this tree reported any pass outcome**.
C4 now does. Final-third passes, 891 launched over the corpus:

| type | n | completion | space-targeted? |
|---|---|---|---|
| Ground | 441 | **41%** | no |
| Lofted | 221 | **1%** | no — aims at the receiver's feet |
| Cross | 171 | **1%** | yes |
| ThroughBall | 58 | **28%** | yes |
| Driven / AerialThrough / Chip | 0 | — | never derived |

Overall final-third completion is **23%**; 53% are intercepted and 24% reach a different team-mate.

**The cause is one line of Stage-0 scope, not a formula.** `RunFirstTouch` gate 2 and
`RunLooseBallPickup` both refuse any ball whose centre height exceeds
`FirstTouchConstants.GroundControlHeight` = `BallPhysicsConstants.Possession.ControlHeight` =
**0.5 m**, with the comment "a higher ball is a Heading Mechanics (#10) event, not Stage 0". Heading
was described here as "only opt-in Phase 1" — **CORRECTED the same day: it is default-ON since
July 27, 2026 and its state is serialized in the v18 snapshot block.** The gap is not that heading
is switched off; it is that a header **redirects** the ball and never grants possession, that
`HeadingMechanics` exposes no control/trap entry point, and that the trigger reaches only the single
nearest outfield agent within 1.5 m. DT-emitted HEADER is deferred behind the 3-bit `ActionType` ordinal
ceiling. So **no agent can receive a ball out of the air at all.** An aerial delivery becomes
receivable only after it lands and rolls — by which point the intended receiver is a mean of
**19.0 m** away (C4e; >10 m on 57% of passes) and an opponent is nearer.

That single fact explains the 1% pair, the 53% interception rate, the 86% ownerless share, and the
5% box-reach — a cross is football's primary route into the penalty area and here it is a 1% pass.

Two figures argue the window is not the problem: mean time-to-rest is **2.83 s** (65% exceed 2 s),
which at 5 m/s is 14 m of running, and the counterfactual race for space 8 m goalward of the ball is
**46% attacker-nearer** — close to even, not a rout. Space is contestable; nobody contests it.

### 10.4 The corrected order

1. **Aerial reception** (Bound B). Largest measured footprint, and it is a *wiring* item — the
   heading subsystem exists and is unreached — rather than new physics. Until it lands, 44% of
   final-third passes are guaranteed turnovers.
2. **The defensive block drop** (Bound A, defensive half). Independently correct, creates the space,
   moves the offside line.
3. **Attacking box occupation** (Bound A, attacking half). A new `SlotComposer` step 3c: additive,
   goal-relative, weight ramped continuously in ball advancement (P1 — never a hard third gate),
   owned by **#12**. Not #24: `FR-BU-004` gates its overlay on `zone ∈ {OwnThird, MiddleThird}` and
   build-up §3 delegates the final third to #12/#15 in terms, so a final-third row there is a
   contract violation that breaks its own `T-BU-U-007` lock. Placed after 3b and **before** step 4,
   because an occupation displacement is a shape proposal and placing it after spacing would breach
   `FR-PA-012`.
4. **§7 item 1, pass-to-a-place**, last — after there is someone in the box to aim near and a way to
   receive the ball when it gets there.

### 10.5 Recorded, not fixed

- **`FR-PA-027` ("at most three agents per lane", a MUST) has no enforcement anywhere in `src/`** —
  no constant, no check, no test. Converging attack-line slots into lane C at step 3c would be the
  first thing likely to breach it, and nothing would notice.
- **A defender who reaches the box would be scenery.** `DefensiveAITick.GetAssignment`, the
  per-attacker mark assignment, has no production consumer, and W2 means no agent can tackle.
- **`PassTargetResolver.ResolveSpaceTargetedAimPoint` is dormant** — reachable only when
  `TargetAgentId == -1`, which no producer sets. The wiring backlog's Class-C table lists
  `PassTargetResolver` as "correctly wired"; that row is wrong for this method.
- **`PassOutcome.Invalid` is not observable** without a new `MatchEngine` accessor, so C4 cannot
  report it.
- **No `ERR-` id was filed for either bound, deliberately.** Bound A is a genuine #12 spec defect
  (the §3.1/§3.2 composition cannot place an agent in the attacking penalty area at any legal
  constant value, while #24 §3.2 delegates final-third positioning to it), and `ERR-012-012` was
  verified free by grep on August 9. It is **not** soft-reserved: this project has been burned by
  ids reserved in prose and consumed elsewhere, so the id is to be re-grepped and filed by whichever
  landing fixes it. Bound B is not a spec defect at all — Stage 0 scopes aerial control out by
  design and says so at the gate; it is a wiring item, and belongs in the wiring backlog.
- **`fast-balls-deflect-off-bodies` was never a reachability predicate in practice** — measured by
  execution at 4 events pre-C1 and 0 post-C1, across ~36 minutes of football, against a bound of
  "> 0". A positioning change moved it without touching collision code.
- **Goalward dribbling (§7 item 6 / `ERR-008-024`) stalls the engine — consistent with §10.2/§10.3,
  not a coincidence.** A tie-break fix that sends the carrier's dribble toward goal on a tie passes
  the close-chance acceptance scenario but stalls `sim_match_engine_play_develops` outright (ball
  last moving at tick 18465 of 32400) and zeroes `goals-still-scored`; a wider always-goalward form
  produces the identical stall at the identical tick. That is exactly what §10.2/§10.3 predict:
  nobody can receive a ball above 0.5 m (Bound B) and no composed slot reaches the box (Bound A), so
  a carrier sent goalward runs into congestion with no pass option and no target to run at, and play
  dies there. Implemented, measured, refused August 9, 2026. Sending the ball goalward is only safe
  once those two bounds are addressed.

### 10.6 The header measurement — item 1 is mispriced, and the real bound is narrower

> **Measured August 9, 2026**, instrument v1.3 (Report C5), 6 seeds × 90 min. Ordered by the
> evidence advisor with the decision rule stated BEFORE the run, so the reading is not post-hoc.

§10.4 ranked aerial reception first on the grounds that #10 "exists and is unreached". **Both halves
were wrong.** Heading is default-ON (since July 27, 2026) and its state is serialized in the v18
block. So the question became: does a committed header ever CONNECT? Nothing in this tree had ever
reported a header statistic — `HeadingTelemetry` is five no-op stubs — which is the §5.Z.17
signature exactly.

| | measured |
|---|---|
| headers **executed** | **2** — both in midfield, **zero** in any attacking third |
| headers **failed** | **963** |
| **contact ratio, executed / (executed + failed)** | **0.2%** |
| dominant failure cause | **`positionedPoorly` — 97% away, 99% home** |
| crosses ending in an attacker header | **0%** (n = 171) |
| crosses ending in a defender header | **0%** |
| crosses reaching the intended receiver | **0%** |
| crosses **coming to rest untouched** | **69%** (30% ground interception, 1% keeper) |

**Minimum 3-D distance from an airborne ball to the nearest outfielder, per airborne final-third
episode (1,097 episodes):**

| | ≤ 0.18 m | 0.18–0.5 | 0.5–1.5 | 1.5–3 | 3–5 | > 5 | mean |
|---|---|---|---|---|---|---|---|
| nearest **attacker** | **0%** | 0% | ~10% | ~25% | ~18% | ~46% | 5.9 m |
| nearest **defender** | **0%** | 0% | ~31% | ~21% | 10% | ~38% | 4.7 m |

**The defender row is the one that matters, and it narrows §10.2's claim.** Defenders are present in
numbers and they make contact 0% of the time too. So an airborne ball is not merely unreachable by
*attackers who are not in the box* — it is untouchable by **anybody, anywhere**, and box occupancy is
not the binding constraint on it. Fixing §10.4 items 2–3 would therefore NOT make crosses work.

**The cause is one sentence: nothing in this engine moves a player to where a ball is going.**
`HeadingEligibility.FindContactFrame` holds the head centre fixed at the agent's current position
while only the ball moves, so the prediction never models a player running onto it.
`SelectLooseBallCollector` returns nobody while the ball is in motion. #12's composed slot is
relative to the ball's *current* position, so support drifts after the ball rather than ahead of it.
The 0.18 m contact volume is then unreachable by construction: the trigger arms at 1.5 m, and no
mechanism closes the remaining 1.32 m.

**Consequences for the order in §10.4, which is superseded:**

1. **Item 1 is mispriced and is NOT a wiring item.** #10's contact model cannot connect at any
   distance the rest of the engine actually produces. Widening `HeaderTriggerRangeM` or
   `HeadContactVolumeRadiusM` to force contacts is barred — KD-W1, and §5.Z.9 is the precedent for
   widening a trigger against a distribution nobody had measured.
2. **The candidate first lever is now "attack the ball": move a player to a ball's predicted
   arrival point.** It is upstream of aerial contact, of ground loose balls (`SelectLooseBallCollector`
   is inert while the ball moves), and of pass-to-space (§7 item 1) — one mechanism, three payoffs.
   It is also what the C1 pass-in-flight latch already identifies the receiver for.
3. **Recorded, not fixed, and independent of sequencing:** the header target is a FIXED point —
   opponent goal X, pitch-width / 2 — from anywhere on the pitch, so a defender clearing in his own
   box aims 90 m at the far goal. That is the football-judgment doctrine's P4 shape (intent as a
   first-class object) and is an ERR candidate in its own right.
4. **Recorded, not fixed:** the header trigger arms at ball centre `z ≥ 0.5` while first touch
   refuses at `z − 0.11 > 0.5`, so `z ∈ [0.50, 0.61]` is claimable by both and is resolved today only
   by phase order.
5. **A metric correction this run forced.** For a cross, "completion" is the wrong success criterion —
   a cross should be headed, not controlled — so §10.3's 1% under-states crosses by construction. It
   survives anyway: 0% headed, 0% reached, 69% untouched is not "headed instead of received".
6. **Not observable, and worth a production accessor next time:** the count of header intents
   COMMITTED. C5a's event pair is a proxy that can under-count, because `HeadingMechanics.Update`
   stops re-evaluating once the frame passes the agent's landing frame, dropping such a commit with
   neither event fired.

### 10.7 Two corrections to §10.6 — one of them retracts its headline measurement

> **August 9, 2026.** Both were found while implementing §10.6's own recorded items, and both were
> confirmed against source before anything was changed. The council convened on those items
> (`advisor-integrity` + `advisor-evidence`, pre-implementation) surfaced the second.

**Correction 1 — item 3's consequence was wrong, and the truth is worse.** §10.6 item 3 records "the
header target is a FIXED point — opponent goal X, pitch-width / 2 — from anywhere on the pitch, so a
defender clearing in his own box aims 90 m at the far goal." The **value** is right. The
**consequence** is not: `TargetIntent` reaches no formula anywhere in the tree. Its only production
uses are a pitch clamp and the snapshot serializer. `ContactPointIntent` — hardcoded `Vector2.zero`
by the only producer — reaches exactly one read, §3.4's `pointError`, and never the geometry, because
`contactPointActual` is recomputed from ball-vs-head geometry. The outgoing direction was therefore
**pure specular reflection**: a header was a passive bounce and the player had no influence on
direction at all. A defender clearing in his own box did not aim 90 m at the far goal; **he headed
the ball back the way it came.** Filed and fixed as `ERR-010-002`, together with two further defects
in the same chain that §10.6 did not see: the contact point had two independent derivations, and the
world-space point was rebuilt from its 2-D head-local projection, pinning its height to the head
centre — so the reflection normal was permanently horizontal, `reflected.z = v̂_in.z`, and **a
descending ball was headed further down.** No header could lift the ball.

**Correction 2 — §10.6's proximity census is an instrument artifact, and the ranking built on it is
RETRACTED.** This is the third time this document has had to retract a priority claim, and the first
time the cause is the instrument rather than the reasoning.

`CloseChanceDiagnosticTests.BallToAgentDistance3D` computes
`sqrt(dx² + dy² + ballPos.z²)` — the distance from the ball to the agent's **ground** position,
including the ball's **full height**. Every sample is taken inside the episode gate
`ballPos.z > HeaderTriggerMinBallHeightM` = **0.5 m**. So every sample is `> 0.5 m` **by
construction**, and the census's first two buckets — `≤ 0.18 m` and `0.18–0.5 m` — are
**structurally unreachable.** §10.6 published exactly `0%` in both, for both attackers and
defenders, and read it as a measurement.

What that invalidates, specifically:

| §10.6 claim | status |
|---|---|
| "0% of airborne final-third episodes bring ANY outfielder within the 0.18 m contact volume" | **NOT MEASURED** — the bucket cannot be populated |
| "an airborne ball is untouchable by **anybody, anywhere**" | **NOT ESTABLISHED** |
| "the 0.18 m contact volume is unreachable by construction … no mechanism closes the remaining **1.32 m**" | **ARITHMETIC INVALID** — subtracts a horizontal trigger radius from a 3-D distance carrying a ≥ 0.5 m vertical floor |
| nearest attacker **5.9 m** / nearest defender **4.7 m** | **MIS-LABELLED** — these are not horizontal separations; they carry the same height floor |
| "box occupancy is NOT the binding constraint" | **UNSUPPORTED BY THIS CENSUS** (may still be true; not shown here) |

Read as what it actually is — a 3-D distance including height — the same table shows roughly **31%
of episodes with a defender within 0.5–1.5 m of the ball**, i.e. essentially under it or beside it.
Defenders are in the contest. That is the **opposite** of the premise §10.6 item 2 was ranked on.

**Consequences for the order.** §10.6 item 2 named "attack the ball — move a player to a ball's
predicted arrival point" as the candidate first lever, on the strength of the refuted census. That
ranking is **withdrawn, not replaced**: the mechanism may still be worth building, but nothing in
this document currently establishes it, and the next pass owns the measurement, not the build. Two
candidates that were never considered sit ahead of it, both cheaper and both upstream:

1. **The header commit has no head-height gate.** `TryCommitHeaderIntents` fires for any loose ball
   above **0.5 m**, while the head only occupies ~2.0–2.6 m during the §3.2 eligibility window
   (apex = commit + 20 frames; window `[apex − 9, apex + 6]`). A knee-high ball at 0.6 m commits a
   header that cannot possibly connect, and `positionedPoorly` — 97–99% of the 963 failures — is
   emitted whenever `FindContactFrame` returns −1 **for any reason**, so it conflates "he was 4 m
   away horizontally" with "the ball was 1.8 m below his head". Which of those dominates has never
   been measured, and it is the single split that decides whether item 2 is the lever at all.
2. **`HeadingEligibility.FindContactFrame` freezes the head centre — position AND jump-arc z — at the
   agent's current frame** while sweeping only the ball. Even a perfectly-arriving player is tested
   against the wrong head height. §10.6 named the XY half of this; the vertical half is new.

Also recorded, from the same council: **`ERR-012-012` remains unconsumed and unreserved.** The
intercept Z-blindness that item 2 would fix belongs to **#8 §3.1.9.2**, whose normative pseudocode
has no aerial model at all — not to #12 — so it takes an `ERR-008-0NN` when it is filed, and §10.5's
earmark for Bound A stands. And a KD-W1 note that constrains the shape of any such landing: making
the projection 3-D moves the intercept point for a lofted ball to where the ball actually **lands**,
which is FURTHER away, so `travelTime ≤ t` fails more often inside the 1.5 s horizon — expect
**fewer** intercept candidates, not more. The unlock is `MAX_INTERCEPT_TIME` (1.5 s against a
measured 2.83 s mean time-to-rest), which is `[GT]` and governs aerial reception, which W9/C7 leave
unwired. **Inside the freeze.** Measure the candidate count before and after, or a correctness fix
gets reported as a pursuit mechanism.

### 10.8 The corrected instrument's first run — headers fail horizontally, and "attack the ball" is re-ranked to first

> **Measured 2026-08-09**, `TD_CREATION_DIAGNOSTIC=1`, `CloseChanceDiagnosticTests` v1.4, Report C5b,
> 6 seeds × 90 min, **1,081 aerial final-third episodes** (505 home / 576 away). This is the
> §10.7-corrected instrument's first execution — three series measured side by side, none of them
> carrying the ball-height-floor artifact that forced §10.7's retraction.

**SERIES 1 — horizontal (XY) separation only, no ball-height term. Nearest agent per episode:**

| | home mean | away mean | ≤0.18 m (H / A) | 0.18–0.5 m (H / A) | 0.5–1.5 m (H / A) |
|---|---|---|---|---|---|
| nearest **attacker** | 5.93 m | 5.88 m | 0% / 1% | 1% / 0% | 11% / 10% |
| nearest **defender** | 4.93 m | 4.28 m | 0% / 0% | — | 30% / 37% |

**SERIES 1b — ball height at the tick achieving Series 1's minimum separation:** attacker-side pooled
mean **0.59 m** on both home and away, minimum observed **0.50 m** — which is the episode gate's own
floor (`ballPos.z > HeaderTriggerMinBallHeightM`), not a football fact. Defender-side pooled **0.58 m
/ 0.59 m**.

**SERIES 2 — true 3-D distance to the agent's head point (headZ = 2.20 m):**

| | home mean | away mean | ≤0.18 m | 0.5–1.5 m (H / A) |
|---|---|---|---|---|
| nearest **attacker** | 6.30 m | 6.24 m | **0%** | 1% / 0% |
| nearest **defender** | 5.42 m | 4.81 m | **0%** | 1% / 0% |

**SERIES 3 (retained pre-correction measure, for comparison):** attacker pooled **6.00 m / 5.95 m**,
with **10% / 9%** of episodes in the 0.5–1.5 m band — against Series 2's **1% / 0%** for the same band
measured correctly.

**1. Headers fail horizontally, decisively — before height ever enters the question.** Series 1 drops
the ball-height term entirely and still finds the nearest player **4–6 m** from the ball in pure XY.
That forecloses the vertical hypotheses this document and the wiring backlog have been carrying:
**C8**'s missing head-height gate at the header commit, and `FindContactFrame` freezing the head's z
while sweeping only the ball. Both stay recorded, not promoted — you cannot have a head-height problem
worth building against when the nearest head is five metres away laterally. Fix the gate, fix the
frozen z, and the ball still lands nowhere near anyone.

**2. Series 1b is the sharpest single number in this measurement.** At the moment of closest
horizontal approach, the ball sits at **0.59 m** — essentially on the 0.50 m episode-gate floor. These
are "aerial" episodes only in the instrument's sense that the ball briefly cleared the trigger height
while skimming past players who are, on average, metres away. They are not crosses arriving into a box
with anyone under them.

**3. "Attack the ball" (§10.6 item 1) is RE-RANKED as the first candidate lever, on corrected
evidence.** §10.7's withdrawal of that ranking was **correct** at the time it was made: the census it
was built on, `BallToAgentDistance3D`, carried a ≥ 0.5 m height floor baked into every sample by the
episode gate itself, so its two smallest buckets were structurally unreachable and the published 0%
in both was the instrument reporting its own gate back at the caller, not a football measurement.
Series 2 — the same 3-D-to-head-point question, asked by an instrument that does not carry that flaw —
reaches the **same conclusion the withdrawn census reached**, by a sound route and with a sharper
number: **0%** of episodes bring an attacker's or a defender's head within contact distance of the
ball. That reverses a conclusion this document itself published four commits earlier (§10.7). The
reversal is evidence-driven, not a walk-back: §10.7 was right to distrust its instrument, and the
instrument it distrusted is not the instrument that produced this result.

**4. Recorded, not analysed: the defender side is consistently nearer the ball than the attacker
side** — 4.28–4.93 m against 5.88–5.93 m in Series 1, 4.81–5.42 m against 6.24–6.30 m in Series 2 —
inside the **attacking** third, on every series and both home and away. That is a statement about
attacking movement, not defending: the team without the ball is closer to it than the team playing it
forward. Flagged as an open question. No cause is theorised here.

**Not in scope here: designing the lever.** §10.8 records the measurement and the ranking only.
"Attack the ball" needs its own design pass and, per this repo's convention for a pre-implementation
lever choice, an advisory council before any code is written.

### 10.9 The `DRIBBLE_GOAL_DIR_MIN_MODIFIER` falsifier — the mechanism held, the population moved, and the band stays red

> **Measured August 9, 2026**, post-C1 HEAD `02f7ba7`. `UtilityWeights.DRIBBLE_GOAL_DIR_MIN_MODIFIER`
> is a compile-time `const`; measured by temporary local literal edit (1.0, then 0.80 — the shipping
> value), reverted immediately after each run. Nothing committed, `dotnet` not re-run since.

**Why this falsifier exists.** `sim_match_engine_close_chance` fails on the current build —
`final-third-dribbles-are-not-goal-averse` meanCosine **−0.165** against a −0.16 bound,
`goalward-dribbles-are-not-a-minority-of-one-in-three` goalwardShare **0.407** against a 0.42 bound —
and both PASS at pre-C1 `ba4e194`. Three explanations compete, named in §7 item 3 and Acceptance-3
(§9) without being distinguished: **(a)** retune #12's `PullFactor` `InPoss` column — a `[GT]`, inside
the KD-W1 freeze; **(b)** an upstream defect in the DRIBBLE direction-quality mechanism itself; **(c)**
the band is measuring a population change — §10.1's final-third `InPoss` 7.5% → 40.8% — not a
regression in the `DirectionQuality_DRIBBLE` term (ERR-008-018/KD-CC2) it was built to lock. (a) is
frozen regardless of the answer; the falsifier exists to separate (b) from (c), because the fix each
implies is opposite: (b) needs the mechanism repaired, (c) needs the band re-scoped or left alone.

The test: on the post-C1 population, measure the term's on/off delta and compare it against §8's
pre-C1 on/off delta for the same two rungs. A **preserved** delta means the mechanism still does what
it did pre-C1 and the band moved because C1 legitimately swapped a third of final-third samples onto a
support-geometry column that has never been calibrated. A **collapsed** delta means the mechanism
itself regressed under C1 and the band is correctly catching that.

**Measured, post-C1 HEAD, close-chance scenario corpus:**

| `DRIBBLE_GOAL_DIR_MIN_MODIFIER` | meanCosine | goalwardShare |
|---|---|---|
| 1.0 — term OFF (identity) | **−0.413** | **0.253** |
| 0.80 — term ON (shipping) | **−0.165** | **0.407** |
| on/off delta | **+0.248** | **+0.154** |

§8's pre-C1 ladder, same two rungs: −0.302 (off) → +0.006 (on), delta **+0.308** (≈ +0.30).

**1. The locked mechanism did NOT regress under C1.** If the term were fully intact on the post-C1
population, term-off would read ≈ **−0.45** (shipping −0.165 minus the preserved ≈ +0.30 pre-C1
delta). The measured term-off value, **−0.413**, is close to that prediction, not to the shipping
value it would collapse toward if the mechanism had stopped working. The measured delta, **+0.248**,
is **≈ 83% of the pre-C1 +0.30 delta** — and it is roughly a **factor of seven** away from the
collapse case (a delta ≈ 0.035, the shape a fully-eroded term would produce) that would have signalled
a genuine regression. `DirectionQuality_DRIBBLE` is doing essentially the same work it did before C1.

**1b. The movement decomposes exactly, and that is the strongest form of this result.** Both rungs
were measured on both populations, so the band's failure can be split rather than merely attributed:

| rung | pre-C1 (§8) | post-C1 | movement |
|---|---|---|---|
| term OFF (1.0) | −0.302 | −0.413 | **−0.111** |
| term ON (0.80, shipping) | +0.006 | −0.165 | **−0.171** |
| on/off delta | +0.308 | +0.248 | −0.060 |

The band's whole failure is the term-ON row: **+0.006 → −0.165, a movement of −0.171**, which is what
carried it across the −0.16 bound. Of that, **−0.111 is visible with the mechanism entirely disabled**
— it is pure population, and no state of the DRIBBLE term could have prevented it — and **−0.060 is
the erosion of the term's own effect**. So **≈ 65% of the band's
failure is population and ≈ 35% is the term working less well on that population**, and *none of it*
is the term working incorrectly: the term-off row moving at all is a measurement taken with the locked
mechanism switched off.

**The two components sum to −0.171 exactly, and that exactness is an ALGEBRAIC IDENTITY, not a
confirmation.** Since `on = off + delta` on both populations, `Δon ≡ Δoff + Δdelta` holds whatever the
four numbers are; a decomposition that failed to close would mean an arithmetic error, not a failed
hypothesis. Stated here because the first draft of this section presented the exact closure as if it
were evidence, which it is not. **The evidential content is the MAGNITUDE of the split** — that
−0.111 of the −0.171 survives with the mechanism switched off, so the majority of the band's failure
is reproducible without the locked term participating at all. That is what makes (c) evidential
rather than merely plausible.

It also bounds the claim honestly — "the mechanism held" is not "the mechanism was untouched", and
conclusion 3 below is the part of this that a future pass must re-check. A second caution on the
labels: both components are downstream of C1's population change. The −0.060 is called erosion of the
term's effect rather than a second population effect because it is measured as the term's own on/off
leverage shrinking; it is not an independent cause.

**2. Therefore the band moved because its population changed, not because the mechanism it locks
broke.** C1 legitimately moved roughly a third of final-third samples onto a `PullFactor` `InPoss`
column that was statistically unexercisable at 7.5% when these bounds were fitted (§5/§9) and has
never itself been calibrated. The band is measuring the composed shape those samples now carry, not
the DRIBBLE scoring term. **This makes `sim_match_engine_close_chance`, in its present form, a proxy
for composed positioning rather than for the `DirectionQuality_DRIBBLE` term it was built and locked
to test.** That is a scope finding about the scenario, not a finding about the code it exercises.

**3. Caveat, stated rather than buried: there is mild erosion.** +0.248 against +0.308 is **≈ 17%
smaller**, not identical. The mechanism is intact, not untouched — something in the C1-changed
population very slightly narrows what the term can do. This conclusion is conditional on that erosion
not deepening: **if a future change moves the term-off/term-on delta further from +0.30, conclusion 1
must be RE-TESTED, not inherited.** The recipe to re-run it cheaply: set
`DRIBBLE_GOAL_DIR_MIN_MODIFIER` to `1.0f` (temporary literal edit, `UtilityWeights.cs` line 302),
run the close-chance scenario corpus, revert, set it back to `0.80f`, re-run, revert again, and
diff the on/off delta against the **+0.248 / +0.154** pair recorded here.

**4. Disposition: hold red, queued for the KD-W1 calibration pass — do NOT rebaseline a third
time.** A predicate whose bound has already moved once (§9 Acceptance-3, −0.10 → −0.16, owner call
August 7, 2026) and would now move a second time to pass is a predicate that has stopped being a lock
(the same rule the Acceptance-1 box predicate was deleted under, §9). And retuning #12's `PullFactor`
`InPoss` column today — the only lever that could move the band without touching the DRIBBLE term —
would fit a `[GT]` against an engine that KD-W1 names as not ready to calibrate against: no tackling
(wiring backlog W2), no aerial reception (§10.3 Bound B), and, per §10.8, nobody within **4–6 m** of
an aerial ball in any of the measured series. This is an owner-facing recommendation, not a decision
taken here.

**5. Provenance, recorded because it matters for how much weight this carries.** This disposition —
hold red, blame the population not the mechanism, wait for KD-W1 — was reached independently by two
routes before the falsifier ran: an advisory-model read and this session's own reading of §7–§10.8.
Agreement between two independent reads is not, by itself, evidence; it was **tested** rather than
accepted, and the measured on/off delta above is what turns the agreed disposition into a checked one.
The falsifier recipe in item 3 exists so the next pass can re-check it just as cheaply rather than
re-deriving the whole argument from scratch.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 1.0 | 2026-08-04 | — | Initial: implemented + measured. §1 both premises checked (the entry-count premise SURVIVED — the first in this chain); §2 the finding (box empty at 0.11 attackers, no target slot inside the area, and the average final-third dribble pointing AWAY from goal at cosine −0.30) and ERR-008-018; §3 KD-CC1..CC8; §4 the #15 run overlay implemented, measured and refused (its runner target moves 80.9 → 14.7 m while box occupancy falls 0.11 → 0.08); §5 acceptance, 2 of 3 predicates failing pre-fix by execution; §6 the measured result — a 6-of-6 per-seed flip in dribble direction, and an explicit list of what did NOT move, including the withdrawn box-occupancy claim that turned out to be one stalled match; §7 six recorded residuals headed by "#8 cannot pass to a place, only to a player", which now owns the ball-into-box stage; §8 the monotone ladder with the stall column that bounds the [GT] at 0.80. |
| 1.1 | 2026-08-07 | — | Acceptance-3 (§9): the cosine predicate tripped at the ERR-008-021/-022/-023 main merge, pooled −0.119; per-seed the regression is entirely seed 0xD1A6D05E (−0.232 — its whole ERR-008-018 gain returned) while 0x0F1E…78 held (+0.078). Bound rebaselined −0.10 → −0.16 by owner call; share bound unchanged, margin thinned to 0.030. The regression is RECORDED for the KD-W1 calibration pass, not re-tuned here. `MatchEngineCloseChanceScenarios.cs` v1.1. |
| 1.2 | 2026-08-08 | — | §10 added: the post-C1 re-measurement, and this document's own §7 item 1 priority claim RETRACTED (the finding stands; "the real bound" does not). Two bounds sit ahead of it. **Bound A** — the last 17 m of pitch are unreachable by composition at ANY legal constant value, for either side: the F442 ST anchor is 23.1 m from goal, the ball-relative offset is capped at `pull.x × 12 m`, and even at a `pull.x` of 1.0 (above every table value) the slot reaches only 16.8 m against a 16.5 m box edge, while the defensive block bottoms out at 17.4 m. Offside being live on the reception path inverts the order: the block drops BEFORE attackers occupy, else they are permanently offside. **Bound B, new and larger** — 44% of final-third passes are aerial (Lofted 25% + Cross 19%) and complete **1%**, against Ground 41% and ThroughBall 28%, because `RunFirstTouch` and `RunLooseBallPickup` both refuse any ball above 0.5 m (heading is deferred), so no agent can receive a ball out of the air; overall final-third completion is 23%. Corrected order recorded in §10.4, with pass-to-a-place last. §2/§4/§8 figures are superseded for every quantity restated in §10.1. No mechanism landed this pass; the instrument (v1.2) and the measurement are the deliverable. |
| 1.3 | 2026-08-09 | — | §7 item 6 CLOSED as `ERR-008-024`, by a different route than the item proposed: one ranked DRIBBLE candidate instead of two competing ones. §3.1.5.2's 8-sector scan ranks on `spaceInSector × DirectionQuality_DRIBBLE(sectorDir, toGoal)` instead of `spaceInSector` alone — `spaceInSector` saturates at 1.0 for any clear sector, and the old strict `>` test always kept sector 0 (`AgentFacingDirection`) on a tie, which is exactly why KD-CC3's scoring-only fix could suppress a retreating dribble but never redirect it. Same term §3.2.4.1 already applies at scoring; no new constant. `sim_match_engine_close_chance`: meanCosine −0.165 → PASS (bound −0.16), goalwardShare 0.407 → PASS (bound 0.42); neither bound moved. See `spec-error-log.md` ERR-008-024. **[CORRECTED at v1.4 below — this fix was implemented, measured, and REFUSED. It was never landed: the same build stalls play outright and zeroes goals-still-scored. §7 item 6 is REOPENED, not closed.]** |
| 1.4 | 2026-08-09 | — | **CORRECTION to v1.3: §7 item 6 / `ERR-008-024` was recorded CLOSED; it is not.** The fix was implemented, measured, and REFUSED — the KD-CC7 pattern (§4). The sector-scan tie-break DOES pass `sim_match_engine_close_chance` (meanCosine −0.165 → PASS, goalwardShare 0.407 → PASS) but STALLS `sim_match_engine_play_develops` outright (ball last moving at tick 18465 of 32400) and zeroes `goals-still-scored`; a wider `space × DirectionQuality` form produced the identical stall at the identical tick, plus mean-shot-distance 25.41 m against a 24.00 m ceiling. §7 item 6 REOPENED; §10.5 gains a cross-link recording that goalward dribbling is unsafe until §10.2/§10.3's bounds are addressed. `OptionGenerator.cs` reverted to the pre-fix baseline logic; kept, behaviour-neutral: `UtilityWeights.DribbleDirectionQuality` + `UtilityScorer`'s delegation to it. The two v1.3 unit locks are REMOVED. `DecisionTree.Tests` 129 passed / 4 skipped / 0 failed. See `spec-error-log.md` ERR-008-024 and `decision-tree/section-3-1.md` v1.8. |
| 1.5 | 2026-08-09 | — | §10.6: the header measurement. Contact ratio **0.2%** (2 executed, 963 failed; 97–99% `positionedPoorly`), zero executed headers in any attacking third, and crosses at 0% headed / 0% reached / **69% coming to rest untouched**. The proximity census settles the order: **0% of airborne final-third episodes bring ANY outfielder — attacker or defender — within the 0.18 m contact volume**, so an aerial ball is untouchable by anybody and box occupancy is NOT its binding constraint. §10.4's item 1 is mispriced and is not a wiring item; the candidate first lever becomes "move a player to a ball's predicted arrival point", which is upstream of aerial contact, ground loose balls and pass-to-space alike. Four residuals recorded not fixed, including the fixed-point header target (a P4 candidate) and the 0.50–0.61 m band both first touch and the header trigger claim. |
| 1.6 | 2026-08-09 | — | **§10.7: two corrections to §10.6, one of which retracts its headline measurement.** (1) Item 3's consequence was wrong — `TargetIntent` reaches NO formula, so the header aim was not merely fixed, it was **inert**, and every header was a passive specular mirror: a defender clearing in his own box headed the ball back the way it came rather than aiming 90 m at the far goal. Filed and fixed as `ERR-010-002` with two further defects in the same chain (two independent contact-point derivations; the 3-D point rebuilt from its 2-D head-local projection, pinning the reflection normal horizontal so `reflected.z = v̂_in.z` and **no header could lift the ball**). (2) **The §10.6 proximity census is an instrument artifact.** `BallToAgentDistance3D` measures ball-to-agent-GROUND distance including the ball's full height, while the episode gate requires ball z > 0.5 m — so the `≤ 0.18 m` and `0.18–0.5 m` buckets are structurally unreachable and the published `0%` in both is the instrument reporting its own gate. "0% within the contact volume", "untouchable by anybody, anywhere", the 1.32 m gap arithmetic and the 5.9 m / 4.7 m labels are all withdrawn; read correctly the same table shows ~31% of episodes with a **defender within 0.5–1.5 m in 3-D**, the opposite of the premise. §10.6 item 2's ranking of "attack the ball" as the first lever is **withdrawn, not replaced** — the third retraction in this document, and the first caused by the instrument rather than the reasoning. Two cheaper upstream candidates recorded ahead of it (the header commit's missing head-height gate; `FindContactFrame` freezing head z as well as xy), plus the KD-W1 note that the Z-fix alone yields FEWER intercepts and its unlock `MAX_INTERCEPT_TIME` is `[GT]` and frozen. |
| 1.7 | 2026-08-09 | — | **§10.8: the §10.7-corrected instrument's first execution (Report C5b, 6 seeds × 90 min, 1,081 aerial final-third episodes) — headers fail HORIZONTALLY, and "attack the ball" is RE-RANKED to first.** Series 1 (pure XY, no height term): nearest attacker 5.88–5.93 m, nearest defender 4.28–4.93 m — the vertical hypotheses (C8's missing head-height gate; `FindContactFrame`'s frozen head z) stay recorded, not promoted, because a 5 m lateral gap forecloses them. Series 1b: ball height at closest horizontal approach is **0.59 m**, on the 0.50 m episode-gate floor — these episodes are not crosses into an occupied box. Series 2 (true 3-D distance to headZ = 2.20 m) reaches **0%** within contact distance for both attacker and defender, both sides — the SAME conclusion §10.7 withdrew, reached this time by an instrument without the height-floor artifact; the reversal of §10.7's withdrawal is evidence-driven, not a walk-back. Series 3 (the retained pre-correction measure) shows what the artifact was doing: 10%/9% in the 0.5–1.5 m band against Series 2's 1%/0% for the identical question asked correctly. Recorded, not analysed: defenders sit consistently nearer the ball than attackers in the attacking third (4.28–4.93 m vs 5.88–5.93 m), an open question about attacking movement. Lever design explicitly out of scope — needs its own pass plus an advisory council. |
| 1.8 | 2026-08-09 | — | **§10.9: the `DRIBBLE_GOAL_DIR_MIN_MODIFIER` falsifier, run against the standing `sim_match_engine_close_chance` failure (meanCosine −0.165 / bound −0.16, goalwardShare 0.407 / bound 0.42) to distinguish a population change from a mechanism regression.** Post-C1 HEAD `02f7ba7`, term-off (1.0) meanCosine **−0.413**, term-on (0.80, shipping) **−0.165**, on/off delta **+0.248** — against §8's pre-C1 delta of +0.308 (≈ +0.30) at the same two rungs, **≈ 83% preserved** and roughly a factor of seven from the collapse case (delta ≈ 0.035) a genuine regression would have produced. **Conclusion: the locked `DirectionQuality_DRIBBLE` mechanism did not regress under C1; the band moved because C1 swapped roughly a third of final-third samples onto #12's uncalibrated `PullFactor` `InPoss` column** — the scenario is, in its present form, a proxy for composed positioning rather than for the DRIBBLE term it was built to lock. Caveat: the ≈17% erosion is real and mild, not zero — if it deepens on a future change, re-run the falsifier rather than inherit this conclusion (recipe recorded in §10.9 item 3). **Disposition: hold red, queue for the KD-W1 calibration pass, do not rebaseline a third time** — a band rebaselined twice already stops being a lock, and retuning the `InPoss` column today would fit a `[GT]` against an engine with no tackling (W2), no aerial reception (§10.3), and nobody within 4–6 m of an aerial ball (§10.8); owner-facing recommendation, not a decision taken here. Reached independently by an advisory-model read and this session's own analysis, then tested rather than accepted — the falsifier is what makes it evidence. Measurement only: `UtilityWeights.cs` edited locally and reverted, nothing committed, no gate run. |
| 1.9 | 2026-08-09 | — | **§10.9 item 1b self-correction, from AR pass 2.** The decomposition paragraph presented its exact closure (−0.111 + −0.060 = −0.171) as if the exactness were confirmatory. It is not: since `on = off + delta` on both populations, `Δon ≡ Δoff + Δdelta` holds for any four numbers, so a decomposition that failed to close would indicate an arithmetic slip, not a failed hypothesis. Corrected in place — the evidential content is the **magnitude** of the split (−0.111 of −0.171 survives with the mechanism switched off), not that the arithmetic sums. Second caution added: both components are downstream of C1's population change; the −0.060 is labelled erosion of the term's leverage, not an independent cause. No measured number changes. |
#endregion
