# Tackle duel — design note (scaling back #440, proposing a richer duel)

> **Created:** November 16, 2026 · **Version:** 0.3 · **Status:** DRAFT — design supplement, not an
> approved spec. Nothing here is implemented, measured or authorized. Every constant below is a
> proposal, and every decision in §7 is the owner's.
> **Purpose:** The owner reviewed #440 Stage B and judged the tackle model a poor fit for real
> carrier-versus-tackler outcomes. In real duels, sometimes the carrier lets the ball run too far and
> the tackler takes it cleanly. Sometimes the tackler goes for the man rather than the ball. Timing,
> positioning and body orientation all matter. This note records what exists, proposes cutting #440
> down to its correctness fix, and lays out a staged duel that uses state the engine already tracks.
> **Base facts:** main `d290d844` (#543 kickoff formation/Duty, #539 career, #547 W13.1 switched off).
> Owning spec for tackle resolution: Defensive AI #14 §3.6.5 (`ERR-014-006`). #440 Stage A design:
> `w-440-ball-loose-design.md` on `codex/440-ball-loose` (PR #544, unmerged).

## 1. What exists today

**Trigger.** On each 10 Hz AI stride, #14 `TackleIntentEvaluator` emits Hold / Jockey / Commit for
each defender within `TackleEligibleRadiusM` (3.0 m `[GT]`) of his assigned man. The composition root
resolves **at most one** Commit per stride within `TackleContactRadiusM` (= `LooseBallPickupRadiusM`,
1.0 m `[GT]`). The tackler then cannot challenge again for `TackleCooldownStrides` (20 strides = 2 s
`[GT]`).

**Resolution** (`TackleOutcomeResolver`, #14 §3.6.5.3). One keyed SplitMix64 draw goes through
nested inverse transforms:

| Step | Driven by | Not driven by |
|---|---|---|
| `engage` (connect or Missed) | tackler's velocity-to-man alignment (`cos approachAngle`), distance to ball | which side of the carrier he is on, carrier's facing or speed |
| `foulShare` | tackler Aggression (+), Tackling (−) | angle (behind / side / front), closing speed, intent |
| `cleanShare` (BallWon or BallLoose) | tackler Tackling − (carrier Dribbling, Balance) | where the ball is relative to the carrier, carrier's speed |

**BallLoose.** The engine releases control and leaves the ball **where it lies, at zero velocity**.
That point is the carrier's centre, so passive pickup re-awards the ball to the carrier in the same
frame. Stage A measured this as 37 / 37 (#440).

**Inputs the engine has but the duel ignores** (verified at `e0d1e626`):

| Input | Where | Notes |
|---|---|---|
| Facing (unit vector) | `AgentState.FacingDirection` | Every agent, every frame. Not copied into `DefensiveAgentSnapshot`. |
| Velocity, speed, movement band | `AgentState.Velocity` / `Speed` / `CurrentState` | Only the tackler's velocity is used, and only for `approachAngle`. |
| Turn rate, lean | `AgentState.CurrentTurnRate` / `LeanAngle` | Unused by the duel. |
| Side of approach | derivable from positions + carrier facing | `approachAngle` does not encode it (corrected doc, August 12, 2026). Collision #3 has its own `FROM_BEHIND` classifier, used only by the collision-foul path. |

**Not modelled at all.**
- **Dribbling.** While controlled, `DriveControlledBallToPossessor` pins the ball to the carrier's
  exact position with zero velocity, so there is no carried ball and no touch gap.
  `SetDribblingState` is a Stage 0 no-op.
- **Detach monitor.** First Touch #4 §3.4.4 specifies a dribble-detach monitor
  (`DribbleDetachRadius` 1.5 m `[GT]`, declared but unconsumed).
- **Dribbling modifier.** Agent Movement #2 §6.1.2 proposes a `DribblingModifier`, as a
  future-extension placeholder only.
- **Touch model.** No spec defines touch cadence or touch length.
- **Physical contact.** Ball-first versus man-first is undecidable at 60 Hz discrete detection
  (#3 KR-3), and slide-tackle collision is a later-stage #3 deliverable. That is why §3.6.5 is an
  abstract duel.

## 2. Assessment

The owner's critique is correct. The current resolver is a well-built **ability duel**: continuous,
deterministic, with one draw. But its geometry is one scalar (how straight the tackler is running),
and it cannot express the five things that decide real challenges:

1. **The ball's position relative to the carrier.** A heavy touch is a free ball for the tackler.
2. **The tackler's side and the carrier's orientation.** A carrier shielding with his back turned is
   hard to tackle cleanly. From behind, the man is between the tackler and the ball, and fouls follow.
3. **Timing.** A tackler who is set and choosing his moment beats one lunging in at closing speed.
   The lunge catches the man (Foul) or only half-wins the ball (BallLoose).
4. **Intent.** Going through the man versus playing the ball changes the foul and outcome mix.
5. **The carrier's speed and change of direction.** These are hard to time against, and at speed the
   ball runs further from the feet.

#440 Stage B (option C: a fixed 2.5 m/s knock plus a one-frame fence, accepted by a 48-match
preregistered experiment) would fix the immediate re-pickup. But it spends a heavy measurement
programme on a stopgap whose knock direction and speed the duel itself should decide. Items 1–5
would change those numbers anyway.

## 3. Proposal in brief — three stages

| Stage | Scope | New physics? | Snapshot / RNG |
|---|---|---|---|
| **A′ — #440 minimal fix** | BallLoose gives the ball a direction and speed from the duel geometry (§4), and the carrier cannot re-acquire in the tackle frame. Correctness tests only; no 48-run acceptance experiment. | No | Fence: frame-local if provable, otherwise next free schema (v30). No new draw. |
| **B′ — Duel v2** | Resolver reads side of approach, carrier facing and speed, a *virtual ball point*, closing speed (timing) and a ball-first / man-first intent (§5). Remains an abstract duel. | No | No cross-tick state (all inputs are per-frame). The intent needs one decision (§5.4). |
| **C′ — Carried ball** | A real dribbled ball: touches release the ball with velocity, and the carrier re-collects it. The heavy-touch case becomes physical; the B′ virtual ball point is retired. | Yes: new spec work across #2 / #4 / #14 | New persistent state, so a schema bump. |

A′ can land before B′. B′ supersedes A′'s BallLoose direction rule. C′ is a separate specification
programme and is **not** proposed for the current roadmap window.

## 4. Stage A′ — BallLoose direction and speed (replaces the fixed 2.5 m/s knock)

Notation (all in the pitch plane, metres and seconds):

- `p_c` is the carrier position and `v_c` the carrier velocity; `p_t` and `v_t` are the tackler's.
- `n = (p_c − p_t) / |p_c − p_t|` is the unit vector from tackler to carrier. If `|p_c − p_t| < 0.01 m`,
  use the carrier's facing.
- **Closing speed** `c = (v_t − v_c) · n`, in m/s. Positive means the tackler is closing.
  Valid range is about −10 to +18 m/s.

**Knocked-ball velocity:**

```
v_ball = LOOSE_POKE_K · max(c, LOOSE_POKE_MIN_M_S) · n  +  LOOSE_CARRY_K · v_c
```

| Constant | Tag | Proposed | Range | Meaning |
|---|---|---|---|---|
| `LOOSE_POKE_K` | `[GT]` (proposed) | 0.6 | (0, 1.5] | fraction of the closing speed transferred along the poke line |
| `LOOSE_POKE_MIN_M_S` | `[GT]` (proposed) | 2.0 m/s | [0, 5] | floor, so a standing tackler still moves the ball |
| `LOOSE_CARRY_K` | `[GT]` (proposed) | 0.5 | [0, 1] | the ball keeps part of the carrier's momentum |

**Worked example.**
- Tackler at (50, 30) moving (4, 0) m/s; carrier at (51, 30) moving (3, 1) m/s.
- `n = (1, 0)`, so `c = (4 − 3) = 1 m/s`, which is below the floor; use 2.0.
- `v_ball = 0.6 · 2.0 · (1, 0) + 0.5 · (3, 1) = (2.7, 0.5) m/s`, a speed of 2.75 m/s.

The ball runs on ahead of the carrier and away from the tackler. It is a 50-50 that physical pickup
then settles.

**Same-frame fence.** Ordinary recovery (first touch and passive pickup) excludes the carrier in the
tackle frame. This is #440's option C fence, with the same snapshot audit (`w-440` §"One-frame
fence"). Restart awards remain law-governed.

**Acceptance.** The checks are correctness checks, not a football rate:
- zero same-frame carrier re-acquisition;
- exact restore and next-frame proofs;
- mirrored home/away geometry tests;
- a six-seed before/after characterization reported descriptively.

There is no preregistered 25 % cap and no 48-run batch. Football acceptance moves to B′, where the
duel itself is under review.

**Delayed-regain reporting is kept** (review recommendation, v0.2). Every A′ characterization reports
Stage A's frozen time-to-regain bins descriptively: f+1..3, f+4..12, f+13..24 and f+25..60, plus
complete-without-regain and censored. It also reports the f+3, f+24 and f+60 cumulative bounds and
endpoint ownership. These are reported, not gated, so a fence that merely defers the old carrier's
regain by a few frames stays visible.

## 5. Stage B′ — Duel v2 (abstract duel with real geometry)

All inputs are read at resolution from `_agents[]` by the composition root, which already owns the
trigger geometry (§3.6.5.1). No snapshot field or cross-tick state is added. Notation follows §4,
plus `f_c`, the carrier's facing (unit vector).

### 5.1 Side of approach and exposure

Let `r = (p_t − p_c) / |p_t − p_c|`. Then:

```
cosφ = f_c · r        # +1 = tackler in front of the carrier, 0 = side, −1 = behind
X    = (1 + cosφ) / 2  # ball exposure ∈ [0, 1]: front 1, side 0.5, behind 0
```

From behind, the carrier's body is between the tackler and the ball (low `X`). From the front, the
ball is between them (high `X`).

### 5.2 Virtual ball point (heavy-touch proxy until C′)

The ball is modelled as sitting ahead of the carrier's feet, further ahead the faster he runs and the
weaker his close control:

```
d_b  = clamp(BALL_LEAD_BASE_M + BALL_LEAD_SPEED_K · |v_c| · (1 − BALL_LEAD_DRIBBLE_K · dribbling),
             BALL_LEAD_BASE_M, BALL_LEAD_MAX_M)                     # metres
p_b  = clampToPitch(p_c + f_c · d_b)                                  # stays inside the pitch rectangle
X'   = clamp01(X + BALL_LEAD_EXPOSE_K · (d_b − BALL_LEAD_BASE_M))      # a ball well ahead is exposed from the side too
```

| Constant | Tag | Proposed | Range |
|---|---|---|---|
| `BALL_LEAD_BASE_M` | `[GT]` (proposed) | 0.30 m | [0.1, 0.6] |
| `BALL_LEAD_SPEED_K` | `[GT]` (proposed) | 0.08 s | [0, 0.2] |
| `BALL_LEAD_DRIBBLE_K` | `[GT]` (proposed) | 0.5 | [0, 1] |
| `BALL_LEAD_MAX_M` | `[GT]` (proposed) | 1.20 m | [0.6, `DribbleDetachRadius`] |
| `BALL_LEAD_EXPOSE_K` | `[GT]` (proposed) | 0.5 m⁻¹ | [0, 1.5] |

**Worked example.** A carrier at 6 m/s with Dribbling 0.5:
`d_b = 0.30 + 0.08 · 6 · (1 − 0.25) = 0.66 m`. A tackler level with him at the side (`X = 0.5`) gets
`X' = 0.5 + 0.5 · 0.36 = 0.68`. At jogging pace (2 m/s), `d_b = 0.42 m` and `X' = 0.56`. Same angle,
more ball on show at speed. This is the owner's "let the ball go too far" case, without a dribbling
system.

### 5.3 Challenge geometry — one rule for eligibility, selection and resolution

Today the contact gate, the choice of tackler and `reachFraction` all measure to the real ball, which
is pinned to the carrier. B′ replaces all three with one rule, so no step disagrees with another
(review finding, v0.2):

```
d_ball = |p_b − p_t|               # to the virtual ball
d_man  = |p_c − p_t|               # to the carrier's body
d_ch   = min(d_ball, d_man)        # challenge distance
eligible   ⇔  Commit intent  ∧  d_ch ≤ TackleContactRadiusM
selection  :  the eligible Commit with the smallest d_ch (ties: lowest agent id, as today)
targets    :  ball reachable ⇔ d_ball ≤ TackleContactRadiusM;  man reachable ⇔ d_man ≤ TackleContactRadiusM
intent     :  both reachable → drawn with probability m (§5.5)
              only the man   → man-first  (m_eff = 1)
              only the ball  → ball-first (m_eff = 0)
reach      :  ball-first → d_ball / TackleContactRadiusM      # ∈ [0, 1] by construction
              man-first  → d_man  / TackleContactRadiusM
BallLoose  :  the ball is released at p_b (§5.7), not at p_c
```

**A tackler only commits to a target he can reach** (review finding, v0.3). Eligibility guarantees
that at least one target is within `TackleContactRadiusM`, so `reach` never needs a clamp and an
out-of-range target never contributes a positive `engage`. v0.2 clamped an unreachable target to
`reach = 1`, which still gave it about 35 % engagement. The rejected alternative, resolving an
out-of-range intent as a certain Missed, would waste challenges the trigger already approved and turn
intent into a hidden miss penalty.

**Why the minimum.** A defender 1.5 m ahead of a carrier with a 0.66 m lead is 0.84 m from the ball,
so he can challenge it, and only ball-first. A defender 0.6 m directly behind (same 0.66 m lead) can
reach the man but not a ball 1.26 m away. He is eligible, and his challenge is man-first (§5.6). Using the ball distance alone
would make him ineligible. Using the carrier distance alone would let him win a ball outside his reach.

**Known limitation until C′.** The virtual ball exists only inside the tackle path. Pressing,
perception, first touch and passing still see the real ball at `p_c`. The two agree again on
BallLoose (released at `p_b`) and on BallWon (the tackler takes control).

### 5.4 Commitment and timing — both from closing speed

v0.1 kept `cos(approachAngle)`, the tackler's **own** velocity direction, as commitment. That
penalizes a set or retreating defender, whose velocity points away from the carrier even while the
carrier runs into him (review finding, v0.2). B′ derives both commitment and timing from the
**relative** closing speed `c` (§4):

```
commit = clamp01(c / TIMING_SET_SPEED_M_S)                 # 0 when not closing, 1 at the set speed or above
L      = max(0, c − TIMING_SET_SPEED_M_S)                  # lunge excess (m/s)
q      = clamp01(tackling − TIMING_LUNGE_K · L − TIMING_TURN_K · |turnRate_c| / TURN_RATE_REF)
```

`q` is timing quality in [0, 1]. Closing at about the set speed is fully committed and well timed.
Closing faster is a lunge, and timing degrades. So does a carrier who is changing direction.

| Constant | Tag | Proposed | Range |
|---|---|---|---|
| `TIMING_SET_SPEED_M_S` | `[GT]` (proposed) | 1.5 m/s | (0, 4] |
| `TIMING_LUNGE_K` | `[GT]` (proposed) | 0.08 s/m | [0, 0.2] |
| `TIMING_TURN_K` | `[GT]` (proposed) | 0.2 | [0, 0.5] |
| `TURN_RATE_REF` | `[CROSS]` | Agent Movement #2's maximum turn rate | — |

**Worked example.** Tackling 0.7, closing at 5.5 m/s (`L = 4`), carrier running straight:
`commit = 1`, `q = 0.7 − 0.32 = 0.38`. A defender closing at 1.5 m/s gets `commit = 1`, `q = 0.70`.

### 5.5 Intent — ball-first or man-first

```
m = clamp01(INTENT_MAN_BASE + INTENT_MAN_AGGRESSION_K · aggression + INTENT_MAN_SHIELD_K · (1 − X'))
```

`m` is the probability of a man-first challenge when both targets are reachable; §5.3 sets `m_eff`
otherwise. When the ball is shielded, more challenges go through the man. Intent is drawn **after** selection and **before** resolution, because it picks the reach
target (§5.3). `m` does not depend on the tackler's speed.

**Draw-site decision (owner, §7 D3).** Two options:

- **(a)** A second **keyed** derivation, same key plus a sub-key, as in the existing `TackleDrawUniform`
  pattern. This is independent of the outcome draw, perturbs no stream (ERR-030-012 / ERR-041-002
  shape) and adds no state.
- **(b)** Nest one more inverse-transform level into the existing single draw. This keeps "one draw"
  but couples intent with outcome bands.

Recommend **(a)**. It keeps the outcome draw readable, and its determinism story is already
documented.

| Constant | Tag | Proposed | Range |
|---|---|---|---|
| `INTENT_MAN_BASE` | `[GT]` (proposed) | 0.05 | [0, 0.5] |
| `INTENT_MAN_AGGRESSION_K` | `[GT]` (proposed) | 0.25 | [0, 0.6] |
| `INTENT_MAN_SHIELD_K` | `[GT]` (proposed) | 0.30 | [0, 0.6] |

### 5.6 Revised outcome bands (same nested shape as §3.6.5.3)

`engage` answers only "did he reach his target". Lunge, side of approach and intent change **what
happens when he does**, never whether he connects. That separation makes the directional claims in
§6 provable (review finding, v0.2).

```
engage     = clamp01(TackleEngageBase + TackleEngageCommitmentK · commit
                     + TackleEngageProximityK · (1 − reach))                  # reach per intent, §5.3

foulShare  = clamp(TackleFoulShareBase + TackleFoulShareAggressionK · aggression
                   − TackleFoulShareTacklingK · q                             # timing replaces raw Tackling
                   + FOUL_BEHIND_K · (1 − X)                                  # NEW: through the back
                   + FOUL_MAN_FIRST_K · [manFirst],                           # NEW
                   0, TACKLE_FOUL_SHARE_CEILING)

retain     = TackleRetainDribblingWeight · dribbling + TackleRetainBalanceWeight · balance
cleanShare = clamp01(TackleCleanShareBase
                     + TackleCleanShareEdgeK · (q − retain)                   # timing replaces raw Tackling
                     + CLEAN_EXPOSURE_K · (X' − 0.5)                          # NEW: free ball is takeable
                     − CLEAN_MAN_FIRST_K · [manFirst])                        # NEW: man-first rarely wins it cleanly
```

`commit` replaces `cos(approachAngle)` (§5.4). `TackleEngageCommitmentK` keeps its value and meaning:
the weight of a fully committed challenge.

| New constant | Tag | Proposed | Range |
|---|---|---|---|
| `FOUL_BEHIND_K` | `[GT]` (proposed) | 0.25 | [0, 0.5] |
| `FOUL_MAN_FIRST_K` | `[GT]` (proposed) | 0.30 | [0, 0.6] |
| `CLEAN_EXPOSURE_K` | `[GT]` (proposed) | 0.30 | [0, 0.6] |
| `CLEAN_MAN_FIRST_K` | `[GT]` (proposed) | 0.25 | [0, 0.5] |

v0.1's `ENGAGE_EXPOSURE_K` and `ENGAGE_LUNGE_K` are withdrawn. Existing `[GT]` values are unchanged.

**What a lunge no longer does.** v0.1 made a lunge miss more by overrunning the ball. Overrun and
evasion depend on the carrier reacting to the lunge, which needs a carrier-evasion model. That is
out of B′ scope and recorded as a C′ question.

**Worked example 1 — lunge from behind.**
- Abilities: tackler Tackling 0.7, Aggression 0.5; carrier Dribbling 0.6, Balance 0.5.
- Carrier at (50, 34), velocity (3, 0) m/s, facing (1, 0).
- Tackler at (49.4, 34), velocity (8.5, 0) m/s, so he is directly behind.

Derived inputs:
- `n = (1, 0)`, `c = 5.5`, `commit = 1`, `L = 4`, `q = 0.38`.
- `cosφ = −1`, so `X = 0`.
- `d_b = 0.30 + 0.08·3·0.7 = 0.468 m`, `X' = 0.084`, `p_b = (50.468, 34)`.
- `d_ball = 1.068 m`, `d_man = 0.60 m`, so `d_ch = 0.60` and he is eligible.
- The ball is out of reach (1.068 m > 1.0 m), so the challenge is man-first: `m_eff = 1`. The
  formula `m` would be 0.450 and is not used.
- `retain = 0.565`.

| Intent (probability) | `reach` | `engage` | `foulShare` | `cleanShare` | Missed | Foul | BallLoose | BallWon |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| man-first (1.000, forced) | 0.60 | 0.430 | 0.712 | 0 | **0.570** | **0.306** | **0.124** | **0** |

**Worked example 2 — set defender in front.**
- Same abilities.
- Carrier at (50, 34), velocity (3, 0) m/s, facing (1, 0).
- Defender at (50.95, 34), jockeying backwards at velocity (2, 0) m/s.

Derived inputs:
- `n = (−1, 0)`, `c = (2 − 3)·(−1) = 1.0 m/s`, `commit = 0.667`, `L = 0`, `q = 0.70`.
- `cosφ = +1`, so `X = 1` and `X' = 1`.
- `p_b = (50.468, 34)`, `d_ball = 0.482 m`, `d_man = 0.95 m`, so `d_ch = 0.482` and he is eligible.
- Both targets are within 1.0 m, so intent is drawn: `m = 0.05 + 0.125 + 0 = 0.175`.

| Intent (probability) | `reach` | `engage` | `foulShare` | `cleanShare` | Missed | Foul | BallLoose | BallWon |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| man-first (0.175) | 0.95 | 0.277 | 0.430 | 0.281 | 0.723 | 0.119 | 0.113 | 0.044 |
| ball-first (0.825) | 0.482 | 0.370 | 0.130 | 0.531 | 0.630 | 0.048 | 0.151 | 0.171 |
| **mixed** | | | | | **0.646** | **0.061** | **0.144** | **0.149** |

The two challenges have identical abilities. The lunge from behind fouls five times as often and
never wins the ball cleanly. The set frontal challenge wins it cleanly in about one in seven
attempts. The probabilities show direction only; no band is claimed realistic (§6).

### 5.7 BallLoose under B′

The ball is released at `p_b`, with the velocity from §4. The poke line depends on intent:
- **ball-first:** the poke goes along `n`;
- **man-first:** the ball is knocked on with the carrier's momentum, with `LOOSE_POKE_K` scaled by
  `1 − INTENT_KNOCK_DAMP`. `INTENT_KNOCK_DAMP` is `[GT]` (proposed) 0.5, range [0, 1].

## 6. Measurement and acceptance

- **Wiring status.** The tackle path is wired and live (W2), so KD-W1 does not block design work here.
  Tuning `[GT]` values remains a calibration pass gated on the owner's §7 decisions and the open
  window rules (D-05).
- **Directional checks are preregistered as signs, with their conditioning stated.** Each is mirrored
  home/away, and the formulas must guarantee each sign:

  | Check | Holds | Why it holds in §5.6 |
  |---|---|---|
  | A faster approach never lowers P(Foul) per **attempted** challenge | at fixed positions, abilities and carrier motion, over all closing speeds | Below `TIMING_SET_SPEED_M_S`, `L = 0`: `commit`, and so `engage`, rises while `foulShare` is fixed. Above it, `commit = 1` fixes `engage` while `foulShare` rises with `L`. Neither `m` nor which targets are reachable depends on the tackler's velocity. |
  | A lunge lowers P(BallWon) per attempted challenge | **only for `c ≥ TIMING_SET_SPEED_M_S`**, same fixed conditions | `engage` is saturated; `foulShare` is non-decreasing and `cleanShare` non-increasing in `L` (strictly while `q > 0` and no clamp binds). |
  | Below the set speed, closing harder raises P(BallWon) | for `c ≤ TIMING_SET_SPEED_M_S`, same fixed conditions | `engage` rises with `commit`; `q`, `foulShare` and `cleanShare` are fixed. Intended: a defender who never closes does not win the ball. |
  | From behind fouls more and wins cleanly less than from the front | **conditional on connecting**, at equal reach | `X` enters only `foulShare` (+) and `cleanShare` (−, via `X'`) and raises `m`. Across real positions `engage` differs, because the reach target differs, so the unconditional rate is reported descriptively, not asserted. |
  | A faster carrier concedes more BallWon from the side | conditional on connecting, at equal reach | `d_b`, and so `X'`, rises with carrier speed; `CLEAN_EXPOSURE_K > 0`. |

  **Check against the v0.1 counterexample** (carrier at 1.5 m/s, defender 0.6 m behind, abilities as
  in §5.6): the defender at 3 m/s has P(Foul) 0.207; at 7 m/s it is 0.220. Under v0.1 the same pair
  gave 0.136 and 0.118, which was the wrong direction.

  **Check against the v0.2 counterexample** (review, v0.3: carrier at (50, 34) moving (3, 0), frontal
  defender at (50.9, 34), abilities as in §5.6). Moving (2, 0), `c = 1` and P(BallWon) is 0.153. Moving
  (1, 0), `c = 2` and it is 0.177. The two speeds straddle the set speed, so the commitment gain below
  it outweighs the 0.5 m/s lunge excess above it. v0.2 claimed the BallWon sign unconditionally; v0.3
  claims it only where commitment is saturated.

  **Sweep.** 2.6 million script evaluations found no violation of the three rows above. The sweep
  covered an ability grid, carrier speeds 0–9 m/s, tackler positions 0.3–1.5 m all round the carrier,
  and tackler speeds −4 to +14 m/s along `n`.
- **Numeric realism bands** need sourced `[GT]` references (owner, §7 D4) before any calibration.
- **Neutrality.** B′ changes default behaviour (every tackle reads new inputs). It is a
  behaviour-changing landing and must not land inside an open measurement window. It needs a six-seed
  before/after characterization and pinned-host Unity validation.
- **Specification path.** Implementing A′ or B′ amends #14 §3.6.5 through `err-file-and-backprop`:
  a new ERR, spec and code in the same commit. This note is a supplement and confers no authority.

## 7. Owner decisions

| ID | Decision | Recommendation |
|---|---|---|
| D1 | Cut #440 to A′: geometry-derived knock plus one-frame fence, with correctness acceptance only. This withdraws the Stage B 48-run experiment and its 25 % caps. | **Yes.** PR #544's Stage A instrument and evidence stay valuable as the before-baseline. |
| D2 | Approve B′ (Duel v2) for a design-to-spec pass on #14 §3.6.5, including §5.3's single challenge geometry and §5.4's closing-speed commitment, which replaces `cos(approachAngle)`. | **Yes, after A′.** |
| D3 | Intent draw: (a) second keyed derivation, or (b) one more nesting level. | **(a)** |
| D4 | Source for numeric realism bands (foul share by approach side, clean-win rate, lunge outcomes). | Owner to name acceptable data. Until then, signs only. |
| D5 | C′ (carried ball and touch model) as a separate spec programme, after PM-2. | **Defer.** The B′ virtual ball point covers the heavy-touch case abstractly. |
| D6 | Roadmap: the PM-2 quick-sim check currently waits on #440. Should it wait on A′ only, or on B′ too? | **A′ only.** B′ is a behaviour change that PM-2 should not absorb mid-check. |

## Version history

| Version | Date | Author | Notes |
|---|---|---|---|
| 0.3 | November 16, 2026 | — | Second Codex review of PR #548 (two P2 findings). (1) §5.3: a tackler only commits to a target within `TackleContactRadiusM`. When only one target is reachable, intent is forced to it (`m_eff`), so `reach` is never clamped and an out-of-range target never connects. Worked example 1 becomes man-first only; worked example 2 moves to 0.95 m so that both targets are reachable. (2) §6: the lunge check is split at `TIMING_SET_SPEED_M_S`. P(Foul) is non-decreasing in closing speed everywhere; P(BallWon) falls only once commitment is saturated, and rises below that. The reviewer's counterexample (0.153 → 0.177) is reproduced and explained. All figures script-checked, plus a 2.6-million-point sweep. |
| 0.2 | November 16, 2026 | — | Codex review of PR #548 (three P2 findings and one A′ recommendation). (1) §5.3: one challenge geometry for eligibility, selection and resolution, using the minimum of virtual-ball and carrier distance, with reach by intent and BallLoose released at `p_b`. (2) §5.6: lunge, side of approach and intent no longer enter `engage`, so a lunge cannot lower P(Foul). The v0.1 counterexample (0.136 → 0.118) now gives 0.207 → 0.220. §6 states each sign's conditioning; `ENGAGE_EXPOSURE_K` and `ENGAGE_LUNGE_K` are withdrawn. (3) §5.4: commitment comes from relative closing speed, not the tackler's own velocity, and both worked examples are rebuilt from explicit positions, velocities and facing. All figures were script-checked. §4: delayed-regain reporting kept for A′. |
| 0.1 | November 16, 2026 | — | Created from the owner's review of #440 Stage B: current state verified at `e0d1e626`; A′ / B′ / C′ staging, proposed formulas with worked examples, owner decisions D1–D6. Nothing implemented. |
