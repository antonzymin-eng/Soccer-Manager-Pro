# Tackle duel — design note (scaling back #440, proposing a richer duel)

> **Created:** November 16, 2026 · **Version:** 0.6 · **Status:** DRAFT — design supplement, not an
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
| **B′ — Duel v2 (slim)** | Resolver reads side of approach, carrier facing, speed and turn rate, and closing speed (timing). Same eligibility, one target, seven new constants (§5). Remains an abstract duel. | No | No cross-tick state (all inputs are per-frame) and no new draw. |
| **C′ — Carried ball** | A real dribbled ball: touches release the ball with velocity, and the carrier re-collects it. The heavy-touch case becomes physical, and B′'s touch-exposure term is retired. | Yes: new spec work across #2 / #4 / #14 | New persistent state, so a schema bump. |

A′ can land before B′. B′ keeps A′'s BallLoose direction rule unchanged. C′ is a separate specification
programme and is **not** proposed for the current roadmap window.

## 4. Stage A′ — BallLoose direction and speed (replaces the fixed 2.5 m/s knock)

Notation (all in the pitch plane, metres and seconds):

- `p_c` is the carrier position and `v_c` the carrier velocity; `p_t` and `v_t` are the tackler's.
- `n = (p_c − p_t) / |p_c − p_t|` is the unit vector from tackler to carrier. If `|p_c − p_t| < 0.01 m`,
  use the carrier's facing. This fallback only gives the A′ knock a direction (a loosed ball must go
  somewhere). B′'s outcome probabilities never use it: they fade side of approach and closing speed
  to neutral near coincidence (§5.2), so no probability jumps at 0.01 m.
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

## 5. Stage B′ — Duel v2, slim form (abstract duel with real geometry)

**v0.5 replaces the B′ of v0.1–v0.4** after the owner judged that form overbuilt (§5.1). B′ keeps
today's §3.6.5 skeleton:
- same eligibility, tackler selection and `reachFraction`, all still measured to the real ball;
- one target;
- one keyed draw through the same nested inverse transforms.

It adds geometry and timing terms only. All inputs are read at resolution from `_agents[]` by the
composition root, which already owns the trigger geometry (§3.6.5.1). No snapshot field, cross-tick
state or new draw is added. Notation follows §4, plus `f_c`, the carrier's facing (unit vector).

### 5.1 What v0.5 drops, and why

| Dropped (v0.1–v0.4) | Constants withdrawn |
|---|---|
| Virtual ball point ahead of the carrier's feet | `BALL_LEAD_BASE_M`, `BALL_LEAD_SPEED_K`, `BALL_LEAD_DRIBBLE_K`, `BALL_LEAD_MAX_M`, `BALL_LEAD_EXPOSE_K` |
| Two-target challenge geometry and reach taper | `REACH_TAPER_M` |
| Drawn ball-first / man-first intent and its outcome terms | `INTENT_MAN_BASE`, `INTENT_MAN_AGGRESSION_K`, `INTENT_MAN_SHIELD_K`, `FOUL_MAN_FIRST_K`, `CLEAN_MAN_FIRST_K`, `INTENT_KNOCK_DAMP` |

Twelve constants are withdrawn and one is added (`TOUCH_EXPOSE_K`, §5.3), so in v0.5 B′'s new `[GT]`
constants fell from 17 to 6, a net cut of 11. v0.6 adds `APPROACH_BLEND_M` (§5.2), making seven.

**Why.** Every review finding on v0.2–v0.4 came from the two-target machinery:
- eligibility measured to one target and reach to the other;
- an out-of-reach target that could still connect;
- a ball won from out of reach;
- a probability cliff at 1 m.

The virtual ball imitated a carried ball abstractly, but it carried the complexity of physics. The
honest home for a real touch gap is C′.

**What the reviews established, and v0.5 keeps:**
- commitment from relative closing speed (v0.2);
- the lunge sign stated only where commitment is saturated (v0.3);
- #14 §3.6.5.3 continuity (v0.4).

With a single target, the reach defects cannot arise.

B′ now adds **seven** `[GT]` constants and one `[CROSS]` reference, where v0.4 had 17: v0.5's six,
plus v0.6's near-coincident blend width `APPROACH_BLEND_M` (§5.2). A′ adds three more (§4).

### 5.2 Side of approach — exposure

Let `r = −n`, the unit vector from carrier to tackler. Then:

```
cosφ = f_c · r          # +1 = tackler in front of the carrier, 0 = side, −1 = behind
X    = (1 + cosφ) / 2    # ball exposure ∈ [0, 1]: front 1, side 0.5, behind 0
```

From behind, the carrier's body is between the tackler and the ball (low `X`). From the front, the
ball is between them (high `X`).

**Near coincidence.** When the two players almost overlap, the direction `n` is undefined and flips
as the tackler crosses the carrier. v0.5 used §4's 0.01 m facing fallback here, which turned a
tackler directly in front into one directly behind at 0.01 m: a probability cliff (Codex review,
v0.6). B′ instead fades both direction-dependent inputs to neutral:

```
w  = clamp01(|p_c − p_t| / APPROACH_BLEND_M)     # 0 at coincidence, 1 from APPROACH_BLEND_M outward
X  = 0.5 + w · ((1 + cosφ)/2 − 0.5)              # side of approach fades to "side" (0.5)
c̃  = w · c                                       # effective closing speed fades to 0 (§5.4)
```

At coincidence (`w = 0`), `X = 0.5` and `c̃ = 0` whatever direction the fallback would pick, so `n`
is never needed there. Beyond `APPROACH_BLEND_M`, `X` and `c̃` equal the plain values, so every
worked example below is unchanged. The fade is Lipschitz: `w` cancels the `1/d` growth of `n`'s
derivative, so the slope stays bounded (§6).

| Constant | Tag | Proposed | Range | Meaning |
|---|---|---|---|---|
| `APPROACH_BLEND_M` | `[GT]` (proposed) | 0.10 m | [0.05, 0.3] | separation below which side of approach and closing speed fade to neutral; a numerical guard, not a calibration dial |

**Worked example.** Both players stationary, carrier facing (1, 0), worked-example abilities, tackler
directly in front. Under v0.5, moving him from 0.010001 m to 0.009999 m flipped `X` from 1 to 0:
P(Foul) jumped from 0.0387 to 0.1132 and P(BallWon) fell from 0.1377 to 0.0427. Under v0.6,
`w ≈ 0.1` and `X ≈ 0.55` on both sides: P(Foul) is 0.0723 and P(BallWon) 0.0894 on both sides.

### 5.3 Heavy touch — exposure rises with speed and falls with close control

The owner's "lets the ball run too far" case is modelled as **more ball on show**, not as a moved ball:

```
X' = clamp01(X + TOUCH_EXPOSE_K · |v_c| · (1 − dribbling))
```

| Constant | Tag | Proposed | Range | Units |
|---|---|---|---|---|
| `TOUCH_EXPOSE_K` | `[GT]` (proposed) | 0.10 | [0, 0.2] | s/m (exposure per m/s of carrier speed, at Dribbling 0) |

**Worked example.** A tackler is level with the carrier at the side (`X = 0.5`), and the carrier has
Dribbling 0.6:
- jogging at 2 m/s: `X' = 0.5 + 0.10 · 2 · 0.4 = 0.58`;
- sprinting at 6 m/s: `X' = 0.74`;
- a Dribbling-1.0 carrier: `X' = X` at any speed.

`X'` enters only the clean-win share (§5.6). `X`, the body position, decides the foul term.

### 5.4 Commitment and timing — both from closing speed

v0.1 kept `cos(approachAngle)`, the tackler's **own** velocity direction, as commitment. That
penalizes a set or retreating defender, whose velocity points away from the carrier even while the
carrier runs into him (review finding, v0.2). B′ derives both commitment and timing from the
**relative** closing speed, in its effective form `c̃` (§5.2: equal to `c` from §4 beyond
`APPROACH_BLEND_M`, fading to 0 at coincidence):

```
commit = clamp01(c̃ / TIMING_SET_SPEED_M_S)                # 0 when not closing, 1 at the set speed or above
L      = max(0, c̃ − TIMING_SET_SPEED_M_S)                 # lunge excess (m/s)
q      = clamp01(tackling − TIMING_LUNGE_K · L − TIMING_TURN_K · |turnRate_c| / TURN_RATE_REF)
```

`q` is timing quality in [0, 1]:
- Closing at about the set speed is fully committed and well timed.
- Closing faster is a lunge, and timing degrades.
- So does a carrier who is changing direction.

| Constant | Tag | Proposed | Range |
|---|---|---|---|
| `TIMING_SET_SPEED_M_S` | `[GT]` (proposed) | 1.5 m/s | (0, 4] |
| `TIMING_LUNGE_K` | `[GT]` (proposed) | 0.08 s/m | [0, 0.2] |
| `TIMING_TURN_K` | `[GT]` (proposed) | 0.2 | [0, 0.5] |
| `TURN_RATE_REF` | `[CROSS]` | Agent Movement #2's maximum turn rate | — |

**Worked example.** Tackling 0.7, closing at 5.5 m/s (`L = 4`), carrier running straight:
`commit = 1`, `q = 0.7 − 0.32 = 0.38`. A defender closing at 1.5 m/s gets `commit = 1`, `q = 0.70`.

### 5.5 Going for the man — a lean, not a separate challenge

There is no drawn intent. The existing `TackleFoulShareAggressionK` term already says that
aggressive tacklers foul more, and that **is** the "goes for the man" lean. Combined with
`FOUL_BEHIND_K` (§5.6), an aggressive tackler arriving from behind is the most foul-prone challenge
in the model.

If playtesting shows aggressive tacklers winning the ball cleanly too often, the follow-up is one
`CLEAN_AGGRESSION_K` term. It is not proposed now.

### 5.6 Revised outcome bands (same nested shape as §3.6.5.3)

```
engage     = clamp01(TackleEngageBase + TackleEngageCommitmentK · commit
                     + TackleEngageProximityK · (1 − reachFraction))       # reachFraction unchanged from §3.6.5

foulShare  = clamp(TackleFoulShareBase + TackleFoulShareAggressionK · aggression
                   − TackleFoulShareTacklingK · q                          # timing replaces raw Tackling
                   + FOUL_BEHIND_K · (1 − X),                              # NEW: through the back
                   0, TACKLE_FOUL_SHARE_CEILING)

retain     = TackleRetainDribblingWeight · dribbling + TackleRetainBalanceWeight · balance
cleanShare = clamp01(TackleCleanShareBase
                     + TackleCleanShareEdgeK · (q − retain)                # timing replaces raw Tackling
                     + CLEAN_EXPOSURE_K · (X' − 0.5))                      # NEW: exposed ball is takeable
```

`commit` replaces `cos(approachAngle)` (§5.4). `TackleEngageCommitmentK` keeps its value and meaning:
the weight of a fully committed challenge. Side of approach and timing change **what happens when he
connects**, never whether he connects. That keeps the §6 signs provable.

| New constant | Tag | Proposed | Range |
|---|---|---|---|
| `FOUL_BEHIND_K` | `[GT]` (proposed) | 0.25 | [0, 0.5] |
| `CLEAN_EXPOSURE_K` | `[GT]` (proposed) | 0.30 | [0, 0.6] |

Existing `[GT]` values are unchanged. Every term is a continuous function of positions, velocities
and abilities, including at near coincidence (§5.2). The eligibility edge at `TackleContactRadiusM` behaves exactly as it does today.

**What a lunge does not do.** It does not make the tackler miss more by overrunning the ball. That
needs a carrier-evasion model, which is a C′ question.

**Worked example 1 — lunge from behind.**
- Abilities: tackler Tackling 0.7, Aggression 0.5; carrier Dribbling 0.6, Balance 0.5.
- Carrier at (50, 34), velocity (3, 0) m/s, facing (1, 0).
- Tackler at (49.4, 34), velocity (8.5, 0) m/s: directly behind, 0.6 m away.

Derived inputs:
- `n = (1, 0)`, `c = 5.5`, `commit = 1`, `L = 4`, `q = 0.38`.
- `X = 0`, `X' = 0 + 0.10 · 3 · 0.4 = 0.12`.
- `reachFraction = 0.6`, `retain = 0.565`.

Bands:
- `engage = 0.10 + 0.25 + 0.08 = 0.430`
- `foulShare = 0.14 + 0.06 − 0.038 + 0.25 = 0.412`
- `cleanShare = 0.30 + 0.60 · (−0.185) + 0.30 · (−0.38) = 0.075`

**Worked example 2 — set defender in front.**
- Same abilities and carrier.
- Defender at (50.6, 34), jockeying backwards at velocity (2, 0) m/s: directly in front, 0.6 m away.

Derived inputs:
- `n = (−1, 0)`, `c = (2 − 3) · (−1) = 1.0 m/s`, `commit = 0.667`, `L = 0`, `q = 0.70`.
- `X = 1`, `X' = 1`.
- `reachFraction = 0.6`.

Bands:
- `engage = 0.10 + 0.167 + 0.08 = 0.347`
- `foulShare = 0.14 + 0.06 − 0.07 + 0 = 0.130`
- `cleanShare = 0.30 + 0.60 · 0.135 + 0.30 · 0.5 = 0.531`

**Worked example 3 — heavy touch, side challenge.**
- Same abilities.
- Tackler at the carrier's side, 0.6 m away, closing at 1.5 m/s (`commit = 1`, `q = 0.70`, `X = 0.5`).
- The only difference between the two rows is the carrier's speed.

| Challenge | `engage` | `foulShare` | `cleanShare` | Missed | Foul | BallLoose | BallWon |
|---|---:|---:|---:|---:|---:|---:|---:|
| 1. lunge from behind | 0.430 | 0.412 | 0.075 | 0.570 | 0.177 | 0.234 | 0.019 |
| 2. set defender in front | 0.347 | 0.130 | 0.531 | 0.653 | 0.045 | 0.141 | 0.160 |
| 3a. side, carrier jogging 2 m/s (`X' = 0.58`) | 0.430 | 0.255 | 0.405 | 0.570 | 0.110 | 0.191 | 0.130 |
| 3b. side, carrier sprinting 6 m/s (`X' = 0.74`) | 0.430 | 0.255 | 0.453 | 0.570 | 0.110 | 0.175 | 0.145 |

The abilities and distance are identical throughout:
- **Lunge from behind against the set frontal challenge.** It fouls about four times as often
  (0.177 against 0.045) and wins the ball cleanly about an eighth as often (0.019 against 0.160).
  It connects more often (0.430 against 0.347) because it is committed. The lunge is punished in
  what happens on contact, not in the miss rate.
- **The sprinting carrier** concedes more clean wins than the jogging one, and nothing else changes.

The probabilities show direction only; no band is claimed realistic (§6).

### 5.7 BallLoose under B′

A′'s rule stands: the ball is released where it lies, with the §4 velocity. v0.4's man-first damping
(`INTENT_KNOCK_DAMP`) is withdrawn along with intent.

## 6. Measurement and acceptance

- **Wiring status.** The tackle path is wired and live (W2), so KD-W1 does not block design work here.
  Tuning `[GT]` values remains a calibration pass gated on the owner's §7 decisions and the open
  window rules (D-05).
- **Directional checks are preregistered as signs, with their conditioning stated.** Each is mirrored
  home/away, and the formulas must guarantee each sign:

  | Check | Holds | Why it holds in §5.6 |
  |---|---|---|
  | A faster approach never lowers P(Foul) per **attempted** challenge | at fixed positions, abilities and carrier motion, over all closing speeds | Below `TIMING_SET_SPEED_M_S`, `L = 0`: `commit`, and so `engage`, rises while `foulShare` is fixed. Above it, `commit = 1` fixes `engage` while `foulShare` rises with `L`. |
  | A lunge lowers P(BallWon) per attempted challenge | **only for `c̃ ≥ TIMING_SET_SPEED_M_S`**, same fixed conditions | `engage` is saturated; `foulShare` is non-decreasing and `cleanShare` non-increasing in `L` (strictly while `q > 0` and no clamp binds). |
  | Below the set speed, closing harder raises P(BallWon) | for `c̃ ≤ TIMING_SET_SPEED_M_S`, same fixed conditions | `engage` rises with `commit`; `q`, `foulShare` and `cleanShare` are fixed. Intended: a defender who never closes does not win the ball. |
  | From behind fouls more and wins cleanly less, per attempted challenge | at equal distance and closing speed | `engage` does not depend on `X`. Lower `X` raises `foulShare` and lowers `X'`, and so `cleanShare`. This now holds per attempt, not only conditional on connecting. |
  | A faster carrier concedes more BallWon | at a fixed position and closing speed | Once `c` is fixed, carrier speed enters only `X'`, which is non-decreasing in it (strictly while Dribbling < 1 and no clamp binds). |
  | Continuity (#14 §3.6.5.3) | over the eligible region, separation 0 included | Every term is continuous (clamps and products). There is one target and no threshold inside resolution. Near coincidence, side of approach and closing speed fade to neutral (§5.2), so the 0.01 m direction fallback never reaches a probability. The eligibility edge is unchanged from today. |

  At a fixed position `w` is fixed, so `c̃ = w · c` rises with `c` and the closing-speed rows hold in
  `c̃` as written; beyond `APPROACH_BLEND_M`, `c̃ = c`.

  **Earlier counterexamples under the current formulas.**
  - **v0.1's foul reversal** (carrier at 1.5 m/s, defender 0.6 m behind at 3 m/s and then 7 m/s):
    P(Foul) rises from 0.163 to 0.177. Under v0.1 it fell from 0.136 to 0.118.
  - **v0.2's BallWon case** (frontal defender 0.9 m away, closing at 1 m/s and then 2 m/s):
    P(BallWon) rises from 0.132 to 0.162. That is the intended below-set-speed rise.
  - **v0.3's two cases**, an unreachable ball and an intent cliff at 1 m, cannot arise: there is no
    virtual ball and no intent.
  - **v0.5's near-coincident cliff** (tackler directly in front, 0.010001 m against 0.009999 m):
    P(Foul) 0.0387 against 0.1132 under v0.5; 0.0723 on both sides under v0.6 (§5.2).

  **Sweeps** (script, v0.5 formulas, ability grid throughout) found no violation of the rows above:
  - **Closing speed:** 3.9 million evaluations. Carrier speeds were 0–9 m/s, tackler positions
    0.2–0.99 m all round the carrier, and tackler speeds −4 to +14 m/s along `n`.
  - **Side of approach:** 72,000 evaluations, rotating the tackler from front to behind at a fixed
    distance and closing speed.
  - **Carrier speed:** 157,000 evaluations, raising carrier speed from 0 to 9 m/s at a fixed
    position and closing speed.
  - **Near coincidence (v0.6).** The sweeps above start at 0.2 m and missed the 0.01 m fallback.
    A further 7.7 million sign evaluations at separations 0–0.2 m (exactly 0 included; closing
    speed, and front-to-behind rotation at fixed closing speed) found no violation. A continuity
    sweep over a 0.3 m disc round the carrier, 2.4 million perturbation pairs of 10 µm, found a
    worst slope of 15.1 probability per metre and no jump.
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
| D2 | Approve slim B′ (Duel v2, §5) for a design-to-spec pass on #14 §3.6.5, including §5.4's closing-speed commitment, which replaces `cos(approachAngle)`. | **Yes, after A′.** |
| D3 | *Withdrawn in v0.5.* Slim B′ has no intent draw, so no draw-site decision is needed. | — |
| D4 | Source for numeric realism bands (foul share by approach side, clean-win rate, lunge outcomes). | Owner to name acceptable data. Until then, signs only. |
| D5 | C′ (carried ball and touch model) as a separate spec programme, after PM-2. | **Defer.** B′'s touch-exposure term (§5.3) covers the heavy-touch case abstractly. |
| D6 | Roadmap: the PM-2 quick-sim check currently waits on #440. Should it wait on A′ only, or on B′ too? | **A′ only.** B′ is a behaviour change that PM-2 should not absorb mid-check. |

## Version history

| Version | Date | Author | Notes |
|---|---|---|---|
| 0.6 | November 18, 2026 | — | Codex review of v0.5 at `1079bf9c` (one P2). §4's 0.01 m facing fallback reversed `n` for a tackler directly in front, a probability cliff (P(Foul) 0.0387 → 0.1132) that contradicted §6's continuity. §5.2 adds a near-coincident fade: side of approach and closing speed blend to neutral below `APPROACH_BLEND_M` (new, 0.10 m `[GT]`), so B′ adds seven `[GT]` constants. §4 limits the fallback to the A′ knock direction; §5.4 and §6 use effective closing speed `c̃`. Worked examples unchanged. New near-coincident sweeps (7.7 million sign evaluations, 2.4 million continuity pairs, separations from 0) found no violation and no jump. §3 updated. |
| 0.5 | November 16, 2026 | — | Owner review: B′ judged overbuilt. §5 is rewritten as a slim duel on today's §3.6.5 skeleton, with the same eligibility, one target and one draw. It adds side-of-approach exposure, heavy touch as exposure (new `TOUCH_EXPOSE_K` 0.10 s/m), closing-speed commitment and timing, and keeps the existing Aggression term as the man-first lean. Withdrawn: the virtual ball point, two-target geometry, reach taper and drawn intent: 12 constants withdrawn and one added (`TOUCH_EXPOSE_K`), so B′'s new `[GT]` constants fall from 17 to 6, plus one `[CROSS]`. Kept from the reviews: closing-speed commitment, the set-speed-conditional lunge sign and continuity. The from-behind sign now holds per attempt. §3 and §7 are updated, and D3 is withdrawn. Three new worked examples; sign sweeps run on the v0.5 formulas found no violation. |
| 0.4 | November 16, 2026 | — | Third Codex review of PR #548 (two P2 findings). (1) §5.6: `cleanShare` carries the ball-reach weight `ρ_b`, so no challenge wins a ball out of reach, whatever the intent. The reviewer's case (ball 1.128 m away, P(BallWon) 7.14 %) now gives 0. (2) §5.3: v0.3's hard intent forcing at 1 m is replaced by continuous reach weights `ρ` (new `REACH_TAPER_M` 0.25 m `[GT]`, proposed). Intent `π_m` is `m` reweighted by reachability, and `engage` is scaled by `ρ_ch`, so P(Missed) → 1 at the eligibility edge. The reviewer's 0.9999 m / 1.0001 m pair (14.48 % → 16.64 %) now gives 16.64 % on both sides. Worked example 2 and the §6 counterexample figures are regenerated. Sweeps: 2.6 million sign evaluations and 17 million continuity pairs, with no violation; the steepest response at body separations of at least 0.3 m is under 8 % per metre. |
| 0.3 | November 16, 2026 | — | Second Codex review of PR #548 (two P2 findings). (1) §5.3: a tackler only commits to a target within `TackleContactRadiusM`. When only one target is reachable, intent is forced to it (`m_eff`), so `reach` is never clamped and an out-of-range target never connects. Worked example 1 becomes man-first only; worked example 2 moves to 0.95 m so that both targets are reachable. (2) §6: the lunge check is split at `TIMING_SET_SPEED_M_S`. P(Foul) is non-decreasing in closing speed everywhere; P(BallWon) falls only once commitment is saturated, and rises below that. The reviewer's counterexample (0.153 → 0.177) is reproduced and explained. All figures script-checked, plus a 2.6-million-point sweep. |
| 0.2 | November 16, 2026 | — | Codex review of PR #548 (three P2 findings and one A′ recommendation). (1) §5.3: one challenge geometry for eligibility, selection and resolution, using the minimum of virtual-ball and carrier distance, with reach by intent and BallLoose released at `p_b`. (2) §5.6: lunge, side of approach and intent no longer enter `engage`, so a lunge cannot lower P(Foul). The v0.1 counterexample (0.136 → 0.118) now gives 0.207 → 0.220. §6 states each sign's conditioning; `ENGAGE_EXPOSURE_K` and `ENGAGE_LUNGE_K` are withdrawn. (3) §5.4: commitment comes from relative closing speed, not the tackler's own velocity, and both worked examples are rebuilt from explicit positions, velocities and facing. All figures were script-checked. §4: delayed-regain reporting kept for A′. |
| 0.1 | November 16, 2026 | — | Created from the owner's review of #440 Stage B: current state verified at `e0d1e626`; A′ / B′ / C′ staging, proposed formulas with worked examples, owner decisions D1–D6. Nothing implemented. |
