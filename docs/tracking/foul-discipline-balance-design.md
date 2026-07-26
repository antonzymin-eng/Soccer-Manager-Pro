# Foul & Discipline Balance Pass — Design Supplement

> **Created:** July 26, 2026
> **Status:** DESIGN SUPPLEMENT (Stage 0+1 balance pass — the same governance class as
> `match-engine-design.md`, which owns the surface this changes). Opens no numbered spec and changes
> no `SPEC_INDEX.md` row.
> **Owner document:** `docs/tracking/match-engine-design.md` — this note is the detail behind its
> **§5.Z.9**, and §5.Z.7 item 1 is the finding that opened it.
> **Purpose:** Bring the match engine's foul and card rates from "every player dismissed inside a
> match" to football-plausible, and record *why the lever the finding named turned out not to exist*.

---

## 1. The finding, and what measurement changed about it

Phase H (§5.Z) made a match play. It also recorded, deliberately unfixed, that the foul heuristic
issues **~7 red cards per 9 minutes** — consistently, across seeds. §5.Z.7 item 1 framed the fix as a
`[GT]` threshold question (`FOUL_MIN_FORCE_N` / `FoulCooldownTicks` / `RedCardProbability`) needing
"a foul-rate target and a measurement pass, not a guess folded into a correctness fix."

The measurement pass ran (`src/match-engine/tests/FoulRateDiagnosticTests.cs`, four seeds ×
9 minutes of composed play). It confirmed the severity and **refuted the framing**.

### 1.1 Measured, on the shipped engine

| Quantity | Measured (per 90 min) | Real football | Ratio |
|---|---|---|---|
| Fouls given | **480** | ~22 | 22× |
| Yellow cards | **147** | ~3.5 | 42× |
| Red cards | **75** | ~0.25 | 300× |

Underneath: **203 148** agent-agent contacts and **35 997** cross-team FROM_BEHIND contacts across
36 minutes of play — roughly **17 qualifying contacts per second**, with at least one on **20.1%** of
all ticks. The engine's agents are in near-continuous back-on contact.

### 1.2 The force distribution — why the threshold is not a dial

Peak force per tick among qualifying contacts:

| p50 | p75 | p90 | p95 | p99 | p99.9 | max |
|---|---|---|---|---|---|---|
| 221 N | 632 N | 929 N | 1012 N | 1175 N | 2058 N | **2362 N** |

Replaying the *production gate* offline across a threshold ladder (fouls per 90 min):

| threshold | 600 N | 1200 N (shipped) | 2000 N | 3000 N | ≥ 4000 N |
|---|---|---|---|---|---|
| cooldown 60 | 1125 | 480 | 90 | **0** | 0 |
| cooldown 600 | 350 | 258 | 75 | **0** | 0 |

The distribution is **bounded and narrow**: collisions between agents at football speeds produce a
bounded impulse, so `F = j / ContactDurationS` cannot exceed ~2400 N. The threshold therefore has no
setting that yields ~22 fouls. It goes 90 → 0 between 2000 N and 3000 N, and the only values in
between sit on the last ~30 samples of a 130 000-tick run — a setting that would read as calibrated
and would in fact be noise, flipping to 0 or 200 on a different squad, tactic, or physics tweak.

**Nor does the cooldown rescue it.** At 2000 N a ten-second cooldown still leaves 75 fouls per match,
because the surviving events are already sparser than that; reaching 22 would need a ~30-second
suppression window, which would swallow genuine fouls wholesale.

### 1.3 The actual gap

The model says **every hard cross-team contact from behind is a foul**. Football says a referee
*judges* contact, and gives a small fraction of it. With the engine producing 17 qualifying contacts
per second, no threshold can substitute for that judgement: the missing term is not a bigger number,
it is a **probability**.

That is a model change, not a constant change — which is why this note exists rather than a
four-value edit.

---

## 2. Scope

**In scope.** The referee-judgement term; recalibrating the four `[GT]` discipline constants against
a stated target; capturing the strongest rather than the first candidate in a tick; an acceptance
scenario that fails on today's engine.

**Explicitly out of scope**, and recorded rather than silently absorbed:

- **The underlying contact rate.** 17 hard from-behind contacts per second is itself unrealistic and
  points at agent spacing (#12) or the `BehindDotThreshold = 0.5` 60° classification cone (#3). Both
  are behaviour changes to subsystems with their own specs, their own tests, and no current defect
  report. This pass makes the *refereeing* plausible over the contact stream it is given; it does not
  reshape the contact stream. Recorded in §7.
- **Foul kinds beyond FROM_BEHIND.** The engine models one foul type. Trips, handball, and shirt-pulls
  are a Stage-1 referee model.
- **Advantage, persistent-infringement bookkeeping, dissent, and off-ball fouls.** Same reason.

---

## 3. Key decisions

### KD-F1 — A referee-call probability, force-scaled, on the existing candidate

A candidate that clears every existing gate (cross-team, FROM_BEHIND, force ≥ threshold, cooldown
expired, neither participant sent off) is **whistled with probability**

```
p(F) = min(1, FoulCallProbability × F / FoulImpactForceThresholdN)
```

At the threshold `p = FoulCallProbability`; it rises linearly with force and saturates at certainty.
Over the measured band (1200–2362 N) that is a ~2× spread, so a hard challenge is about twice as
likely to be given as a marginal one — enough to bias the calls that *are* made toward the contacts
that deserve them, without the "hard contact ⇒ automatic foul" rule the measurement just disproved.

**Rejected: a ramp to certainty** (`p = base + (1−base)·clamp01((F−thr)/span)`). Swept first, and it
does not work here: any contact past `thr + span` is called every time, so at 17 contacts/second the
rate is set by the ramp's endpoint and `base` is almost inert — measured 512–860 fouls per 90 min
across the whole `base` ladder. The intuition it encodes ("a really hard one is always a foul") is
sound football and wrong for this contact stream.

**Rejected: a constant probability with no force term.** Works numerically, but then which contacts
get called is independent of how hard they were, and the force threshold degenerates into a pure
rate knob. The scaling costs one multiply.

### KD-F2 — One draw, partitioned; no new RNG stream, no schema change

`ApplyFoulIfCaptured` already draws exactly one uniform `u` from the `match-flow.card-severity`
stream. It keeps drawing exactly one:

- `u ≥ p(F)` → **waved on.** No event, no card, no restart, **no cooldown** (see KD-F3).
- `u < p(F)` → **whistled.** Card severity comes from the rescaled remainder `v = u / p(F)`, which is
  uniform on `[0,1)` conditional on the call, fed to the unchanged `DetermineCardKind`.

This is ordinary inverse-transform partitioning: `u` carries no meaning of its own, so `v` is a
correct uniform and the severity bands keep their exact semantics. The alternative — a second
`match-flow.foul-call` stream — would mean a new registered stream, a new serialized cursor, and
`SNAPSHOT_SCHEMA_VERSION` 18 → 19, all to avoid one division.

**Consequence:** the draw now happens per *candidate* rather than per *foul*, so the stream advances
much faster. The cursor is already serialized (v17, snapshot-deserialize KD-8), so save/restore
determinism is unaffected — but every digest moves, which is expected and is the point.

### KD-F3 — A no-call arms no cooldown

The cooldown exists to stop one sustained tangle producing a card every tick. A wave-on is not an
event; suppressing detection after one would silently swallow the genuine foul two ticks later. The
cooldown arms only on a whistle — which is also what makes the offline replay in §1.2 exact.

### KD-F4 — Capture the strongest candidate in a tick, not the first

Pre-existing behaviour: the first qualifying contact in a tick wins and the rest are ignored. That
was harmless when every candidate was equally a foul. Under KD-F1 the *force* decides the call
probability, so a trivial 1201 N contact arriving first would shadow a 2300 N challenge in the same
tick and systematically under-call the hardest fouls. The consumer now keeps the strongest.

Still at most one candidate per tick, still deterministic (detection order is deterministic, and ties
keep the earlier one).

### KD-F5 — The target is stated, and it is a rate, not a value

| Quantity | Target per 90 min | Source |
|---|---|---|
| Fouls | ~22 | Top-tier league seasonal averages, both teams combined |
| Yellow cards | ~3.5 | ditto |
| Red cards | ~0.25 | ditto (roughly one every four matches) |

Cards follow from fouls by ratio: `YellowCardProbability ≈ 3.5/22 ≈ 0.16`,
`RedCardProbability ≈ 0.25/22 ≈ 0.011`. The second-yellow promotion adds a negligible further red
rate at 3.5 bookings spread across 22 players.

The acceptance scenario (§5) asserts **bands**, not the measured numbers: the contract is "a match
looks like football", and pinning today's exact counts would make every future physics or AI change
a discipline-test failure.

---

## 4. The changes

### 4.1 Constants (`MatchEngineConstants`)

| Constant | Was | Now | Why |
|---|---|---|---|
| `FoulImpactForceThresholdN` | 1200 | **1200** (unchanged) | p99 of the distribution — in the meaningful part of the band, not on the last thirty samples. It is now the "hard enough to *consider*" gate, with the rate carried by `FoulCallProbability`. |
| `FoulCallProbability` | *(new)* | **0.025** | Calibrated in §1.2's sweep: 0.02 → 17.5, 0.03 → 30 fouls per 90 min. |
| `YellowCardProbability` | 0.35 | **0.16** | KD-F5 ratio. |
| `RedCardProbability` | 0.05 | **0.011** | KD-F5 ratio. |
| `FoulCooldownTicks` | 60 (1 s) | **180 (3 s)** | A restart takes several seconds and the players are still tangled through it; 1 s was thin. Rate-neutral at the new call probability (measured: ≤ 2 fouls per 90 min difference). |

### 4.2 `MatchEngine`

- `_foulCandidateForceN` joins the existing candidate triple. Same lifecycle — written during the
  collision step, consumed and reset in the same tick's `ApplyFoulIfCaptured`, never serialized.
- `MatchFlowCollisionConsumer.OnCollisionEvent` keeps the strongest qualifying candidate (KD-F4).
- `ApplyFoulIfCaptured` computes `p(F)`, partitions the single draw (KD-F2), and returns early on a
  wave-on without publishing, carding, restarting, or arming the cooldown (KD-F3).
- `TestOnly_InjectFoulCandidate` gains an optional force, defaulting to the certainty point
  (`threshold / callProbability`), so every existing injection test keeps meaning "a foul happens".
- `TestOnly_SetCollisionObserver` — the measurement seam (§5.Z.9). Null in production.

### 4.3 What deliberately does not change

The sent-off discards (AR-9 M-1), the restart award and taker selection (KD-H1), the second-yellow
promotion, `DetermineCardKind`'s band arithmetic, and the physical collision response. This pass
changes **how often the whistle goes**, not what happens when it does.

---

## 5. Acceptance

`src/match-engine/tests/MatchEngineDisciplineScenarios.cs` on the #19 ScenarioRunner
(`match-engine-discipline-plausible`, Tier B, cross-spec). Four seeds × 9 minutes, with per-90-minute
rates extrapolated across the aggregate:

Six seeds × 9 minutes = 54 minutes of composed play; rates extrapolated to per-90-minutes.

| Predicate | Band | Football | Pre-fix | Post-fix |
|---|---|---|---|---|
| `fouls-per-match-in-band` | 3 – 90 | ~22 | **478** ✗ | 21 ✓ |
| `yellows-per-match-in-band` | 0 – 20 | ~3.5 | **138** ✗ | 3.0 ✓ |
| `reds-per-match-in-band` | 0 – 5 | ~0.25 | **78** ✗ | 1.0 ✓ |
| `seedN-both-teams-keep-nine-players` (×6) | ≥ 9 per team | 11 | **5 – 7** ✗ | 11 ✓ |
| `card-rate-is-a-minority-of-fouls` | ≤ 0.6 | ~0.17 | 0.40 ✓ | 0.19 ✓ |

**Nine of the ten predicates fail on the pre-fix engine**, every one of them by more than an order of
magnitude. (The tenth, the card-to-foul ratio, passes pre-fix — it guards a different failure mode:
a model that books nearly every foul it gives.)

The bands are deliberately wide — several times the football target on either side. They are a
**plausibility floor**, not a pin: pinning today's counts would make every future physics or AI
change a discipline-test failure. The **lower** bound on fouls matters as much as the upper one: this
pass added a probability gate, so "discipline silently switched off" is now a reachable regression,
and a scenario that only caught over-officiating would happily pass an engine that never blows the
whistle.

`seedN-both-teams-keep-nine-players` is the predicate a player would notice, and it is asserted **per
seed, not aggregated** — one match ending eight-a-side is the failure, and averaging it across six
seeds is exactly how it would be missed. A team below nine means the match is abandoned under the
Laws; it is the direct expression of §5.Z.7's "every player on the pitch would be dismissed inside a
full match."

---

## 6. Verification

Measured on the shipped code over six seeds × 15 minutes — one full match-equivalent of composed
play, sized so the sample can separate a correct rate from a wrong one (at the target, a 90-minute
match contains only ~22 fouls).

| Quantity | Before | After | Football | 
|---|---|---|---|
| Fouls per 90 min | 480 | **21.0** | ~22 |
| Yellow cards per 90 min | 147 | **3.0** | ~3.5 |
| Red cards per 90 min | 75 | **1.0** | ~0.25 |
| Most players dismissed from one team, per 9-min run | 7 | **0 – 1** | — |

Reds land a little high against a target of one every four matches, but the whole 90 minutes of the
corpus contained a single dismissal — the sample cannot resolve a rate that low, and chasing it
would be fitting noise. The band (≤ 5) is what the scenario asserts.

**Calibration required a live run, not the offline sweep.** The sweep pointed at
`FoulCallProbability = 0.025`; a real match measured 37.5 fouls per 90 minutes there. The cause is
feedback the offline replay cannot model: giving 20× fewer fouls means 20× fewer restarts, so play
runs on, and the qualifying-contact count *rose* from 36 000 to 129 000 over a comparable corpus.
0.015 was then measured, not predicted. Recorded because it generalises — an offline gate replay is
a way to find the right *shape* cheaply, never the final value.

---

## 7. Recorded, not fixed

1. **The contact rate itself.** 17 qualifying cross-team FROM_BEHIND contacts per second, on 20% of
   all ticks, is not football. The refereeing model now sits plausibly on top of it, but the stream
   underneath is wrong, and it is the next thing to look at for match realism — most likely agent
   spacing (#12 `SpacingResolver`) or the 60° `BehindDotThreshold` cone (#3). Fixing it would let
   `FoulCallProbability` rise toward a value that reads as a real refereeing rate rather than
   1-in-40.
2. **`FoulCallProbability` is a rate knob, not a physical quantity.** Its value is only meaningful
   relative to the contact stream of item 1. If that stream changes, this must be re-measured — the
   diagnostic driver exists for exactly that and is committed alongside.
3. **Every engine digest moves.** The draw now fires per candidate rather than per foul, so the
   `match-flow.card-severity` cursor advances differently from tick one. All determinism tests are
   comparative (two runs, same seed) so none needed rebaselining, but the `FR-PO-052` certified perf
   baseline — already stale since Phase H — remains to be re-captured on the pinned host.

---

## 8. Adversarial review history

| Round | Findings | Notes |
|---|---|---|
| AR-1 (design) | 1 H + 2 M | H-1: the note's first draft proposed a ramp-to-certainty probability without having swept it; the sweep showed `base` is nearly inert at this contact rate (512–860 fouls/90 across the whole ladder), so the model was replaced with the force-scaled form and the rejection recorded in KD-F1. M-1: no decision covered whether a wave-on arms the cooldown — it must not, or a no-call silently suppresses the next genuine foul (KD-F3). M-2: first-wins candidate capture becomes a systematic bias once force drives the call probability (KD-F4). |
| AR-2 (design) | 0 H + 1 M + 2 L | M-1: §5's bands were originally centred on the measured post-fix numbers, which would have made them a pin rather than a floor and coupled every future physics change to this test; widened to a plausibility band with the pre-fix failure margin stated. L-1: the target table cited no provenance; sourced. L-2: §7 item 2 added — the calibrated value is only meaningful against the current contact stream. |
| AR-3 (code) | see §9 | Over the shipped diff. |

---

## 9. Code review

Adversarial pass over the shipped diff — 0 H + 3 M + 0 L, all fixed.

- **M-1 — the private helper shared its bare name with the `[GT]` constant it consumes.**
  `MatchEngine.FoulCallProbability(float)` next to `MatchEngineConstants.FoulCallProbability` is
  legal C# (the constant is always qualified) but is precisely the same-name confusion class this
  project has been bitten by before — the `TacticTranslation` CS0104 cascade, where five assemblies
  each grew a type of the same name and the composition root stopped compiling. Renamed
  `ComputeFoulCallProbability`.
- **M-2 — the measuring instrument disagreed with the thing it measures.** The diagnostic sampled
  the foul cooldown *before* each tick, so a foul given on a seed's final tick was never counted,
  while the acceptance scenario samples after. A ≤ 1-per-seed undercount is immaterial to the
  numbers, but an instrument that does not agree with the contract it calibrates is worse than no
  instrument. Split into `BeginTick`/`EndTick`, sampling after.
- **M-3 — a stale cost clause on the AR-9 sent-off discard.** Its comment reasoned about the capture
  slot under the old first-wins rule. Re-checked under KD-F4 and extended: the interaction is in fact
  *weaker* now, because a sent-off agent is held at rest by the forced stop and the FROM_BEHIND
  classifier requires both participants moving, so a sent-off participant rarely raises a candidate
  at all.

Verified clean, no change: the candidate fields' non-serialization still holds (the force joins the
existing always-reset-within-the-tick triple); `v = u / callProbability` cannot divide by zero
(reaching it requires `u < callProbability`, and `u ≥ 0`); the wave-on returns before every
observable effect, so no partial application is possible; the yellow total is read from final state
and `SubstitutePlayer` clears a substituted player's count (noted in the scenario, immaterial here
since nothing substitutes); and all 333 pre-existing match-engine tests pass untouched, because the
injection seam defaults to the certainty force.

---

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-26 | — | Initial: the measurement result, the refutation of the threshold framing, KD-F1..KD-F5, the constant table, and the acceptance scenario. |
| 1.0 | 2026-07-26 | — | LANDED. §5 filled with the measured pre/post per-predicate margins (9 of 10 predicates fail pre-fix); §6 with the verification numbers (480 → 21 fouls, 147 → 3.0 yellows, 75 → 1.0 reds per 90 min) and the finding that calibration needed a live run because giving fewer fouls raises the contact rate; §9 with the code-review pass (0H+3M). |
#endregion
