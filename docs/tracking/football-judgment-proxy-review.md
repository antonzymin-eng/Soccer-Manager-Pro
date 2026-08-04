# Football-Judgment Proxy Review

> **Created:** August 4, 2026
> **Updated:** August 4, 2026 — §6 remediation doctrine added (owner-converged in session; see §6
> provenance note). Findings §§2–5 unchanged.
> **Status:** FINDINGS LOG (§§1–5, identification only) + REMEDIATION DOCTRINE (§6, the
> owner-approved general approach each fix must cite). No fixes applied yet; no `ERR-` ids
> allocated (allocation happens at fix time, per the `err-file-and-backprop` skill). Nothing in
> this file has been through the spec-error-log's Filed/Status/Fix/Determinism-impact process.
> **Scope:** All 53 APPROVED specs in `SPEC_INDEX.md`, read directly from `docs/specs/`, regardless
> of whether a `src/` assembly exists yet for that spec.

---

## 1. What this review is looking for

A single defect *shape*, first caught and fixed as `ERR-008-019` (Decision Tree #8's
`ZoneModifier_SHOOT`, where a continuous football judgment — "is this a viable long-shot
position?" — was implemented as one hard threshold on a raw attribute, producing an 11x output
jump for a 1-point attribute difference). The same review pass that found ERR-008-019 also found
seven more instances of the same shape elsewhere in Decision Tree #8 and Goalkeeper Mechanics #11
before this file existed to record them (§2 below). This document extends that pass across every
other APPROVED spec.

The shape has four recurring forms:

- **(a)** a single geometric/proximity/distance/angle check standing in for a decision that should
  weigh more than geometry (e.g. an attacker's skill, a defender's anticipation, risk);
- **(b)** a hard step-function/threshold on ONE attribute or value — a cliff — where a continuous
  or multi-factor blend would be realistic;
- **(c)** a small, fixed set of raw static attributes compared directly, with no situational or
  contextual modulation;
- **(d)** requirements/governance text (§1/§2 FR- items, KD- decisions) that **claims** a behavior
  which the actual formula in §3 does not implement — described but not built.

A geometric check, a threshold, or a small attribute set is not automatically a defect — several
findings below are close calls where the spec is explicit that the simplification is deliberate and
disclosed (e.g. flagged in its own §7 future-work section). Those are still recorded, with the
disclosure noted, because "disclosed" and "acceptable" are not the same thing, but they read
differently from an undisclosed gap. Findings where the reviewing agent judged the pattern did
**not** apply (continuous multi-factor formulas, or a simplification that is honestly scoped rather
than silently substituted) are not itemized here — only specs with genuine findings are broken out;
§4 lists every spec that was reviewed and returned nothing.

Method: eight parallel review passes (six fresh + the two below, carried forward), each given the
pattern above plus the worked calibration examples, reading `outline.md`, `section-1.md`,
`section-2.md`, and all `section-3*.md` files per assigned spec (the highest-yield sections for
this pattern — FRs and formulas).

---

## 2. Decision Tree #8 and Goalkeeper Mechanics #11 (identified in the prior session, recorded here for the first time)

### Spec #8 Decision Tree

- **§3.2.3.1 `ZoneModifier_SHOOT`, midfield branch — `LONG_SHOT_THRESHOLD` hard cliff.**
  **FIXED as `ERR-008-019`** (spec-error-log.md, both `UtilityWeights.cs`/`UtilityScorer.cs` and the
  three owning spec files patched in the same commit; full dotnet gate green). Recorded here only
  for completeness of the review — no further action needed.
- **§3.1.3.3 PASS lane "interceptor" test** — pure geometry (distance/angle of defenders to the
  passing lane). No defender attribute (anticipation, pace) enters the interception-likelihood
  calculation, so a slow, poor-anticipation defender scores as equally threatening an interceptor as
  a fast, high-anticipation one standing in the identical spot. Pattern (a).
- **§3.1.8, §3.2.7.1 PRESS trigger** — decided by "am I the closest teammate to the ball-carrier,"
  with no risk term (cover behind the presser, whether pressing opens a passing lane). Structurally
  identical to Pressing AI #13's own primary-press-selection defect (§3 below) — the same judgment
  gap recurs in the spec that consumes Pressing AI's output. Pattern (a).
- **§3.2.2.1 DT baseline PASS formula — no marked-receiver awareness in the base formula.** The
  formula only discounts a marked receiver when Dismarking AI #23 is active as a modifier; DT's own
  base PASS scoring has no marked-receiver term, so passing reverts to marker-blind behavior whenever
  #23 is toggled off. Pattern (d) — the base spec's own behavior depends on an optional sibling
  system rather than owning the judgment itself.

### Spec #11 Goalkeeper Mechanics

- **§3.5.3 Parry/deflect direction** — saves that cannot be caught are deflected back down the
  incoming shot line rather than steered away from goal and away from traffic/onrushing attackers.
  Already independently recorded as unfixed in `docs/tracking/gk-conversion-at-contact-design.md`.
  Pattern (a)/(c).
- **§3.5.2, KD-21 Catch-vs-parry decision** — a single-attribute threshold check (handling vs. a
  cutoff), with zero situational risk weighting (shot power, traffic, near-post angle) that would
  make parrying correct even for a high-handling keeper. Pattern (b).
- **§3.6.3 Cross/aerial duel resolution** — reduces to comparing jump/strength/heading-type
  attributes directly, with no positioning, run-timing, or traffic modeling. Same shape independently
  found in Heading Mechanics #10 §3.7 (§3 below) — the pattern recurs on both sides of the same
  aerial-contest mechanic. Pattern (c).
- **KD-3, §2 FR-GK-006, §3 KD-20 "Angle-narrowing 1v1"** — governance text describes the keeper as
  actively narrowing the shooting angle in 1v1s; the actual mechanic is a flat stat bonus applied
  regardless of keeper positioning. The described behavior does not exist in the formula. Pattern (d).

---

## 3. New findings, this pass

### Physics layer

**Spec #1 Ball Physics**
- **§3.1.11.1 `CheckPossession`** — possession-taking is gated purely on three geometric/kinematic
  checks (XY distance ≤ 0.5m, relative velocity ≤ 2.0 m/s, ball height ≤ 0.5m); no First
  Touch/Technique skill enters this gate. **Soft instance / likely by design** — the spec discloses
  this as a physics-layer helper, with skill-based control quality deliberately handled downstream in
  First Touch #4. Flagged for completeness, not a clear defect. Pattern (a).

**Spec #4 First Touch Mechanics**
- **§1.4.5 / §2.1 FR-06 / §3.6 Body orientation ("half-turn") bonus** — the +15% effective Technique
  bonus for receiving side-on applies only inside the 30°–60° angle window; the spec is explicit that
  this is a deliberate binary choice ("no partial credit," "hard boundary" — 29° gets 0.0, 30° gets
  the full bonus) over a smooth interpolation. Textbook pattern (b), and unlike some other findings
  here, the hard edge is not a physical discontinuity — it's an authored trade-off.
- **§3.4.2 Possession state machine — INTERCEPTION condition** — a heavy touch becomes an
  interception if any opponent is within `INTERCEPTION_RADIUS` (2.5m) of the ball's landing spot, with
  no opponent attribute (pace, anticipation) modulating the check. Structurally identical to DT #8's
  own PASS-interceptor gap (§2 above). Pattern (a).

**Spec #6 Shot Mechanics**
- **§2.1 FR-08 / §3.7.9 Stumble trigger** — `BodyMechanicsScore` is a rich, continuously-weighted
  composite, but the stumble *consequence* collapses it into a hard AND-gate (`BMS < 0.35 AND
  PowerIntent > 0.75`) with no probability blending — 0.349/0.751 always stumbles, 0.351/0.751 never
  does. Notably inconsistent with this same codebase's own precedent: Collision System #3's
  analogous force-vs-threshold trigger resolves via a continuous linearly-ramped probability, not a
  cutoff. Pattern (b).

**Spec #10 Heading Mechanics**
- **§3.7 Contested duel resolution (KD-8, FR-HE-010)** — the winner of a contested header is decided
  purely by a weighted sum of three static attributes (Balance, Strength, Heading); each
  participant's own `contactQualityScalar` (jump-timing offset, contact-point precision — i.e. who
  timed/positioned their jump better) affects only the *losers'* outcome afterward, never who wins.
  Two agents with identical attributes always resolve identically regardless of who jumped better.
  Near-exact match to GK #11's own aerial-duel gap (§2 above). Pattern (c).

### AI / mechanics layer

**Spec #13 Pressing AI**
- **§3.3 Primary-press selection** — `primaryPress = argmin(‖pos − projInterceptionPoint‖²)`; chosen
  by pure proximity to a projected point, with no weighting of the presser's own pace/tackling, or
  the risk of committing (what's left uncovered). Pattern (a).
- **§3.4 Cover-shadow selection** — `coverCost` is likewise a raw-distance argmin with no covering
  defender's anticipation/pace or the receiver's skill factored in — notable because the adjacent
  `threatScore(r)` two lines above (picking *which receiver* to cover) is a genuine multi-factor
  blend, so the simplification here is a real asymmetry within the same spec. Pattern (a)/(c).
- **§3.1.4 `WEAK_RECEIVER` trigger** — `FirstTouch < 10 AND perceivedPressure >= 0.50`; a hard
  step-function on one raw attribute ANDed with one pressure threshold, no continuous blend with
  presser proximity or numbers around the receiver. Pattern (b).
- **§3.3/§3.7 Fatigue ceiling** — `PRESS_FATIGUE_CEILING = 0.85` is a hard cliff (fully eligible below
  it, instantly excluded at/above it) rather than a graded fall-off in press priority as fatigue
  rises. Pattern (b).

**Spec #14 Defensive AI**
- **§3.5 Threat score** — `threat = perceivedGoalProximity × opponentReceivingAttribute`; this
  two-factor product alone drives MAN_MARK/INTERCEPT_RUNNER priority, with no account of marking
  already applied by teammates or a covering defender's presence. Pattern (c).
- **§3.6 Tackle intent (COMMIT/JOCKEY/HOLD)** — mode is decided entirely by covering-teammate count
  (hard-gated at exactly 1: zero covers never COMMITs, one always does) and approach angle. The
  defender's own `Tackling` attribute and the opponent's ball control are never read, despite §1.4/
  §2.3 explicitly naming `Tackling` as "declared for future tackle-quality use." Pattern (b)/(d).

**Spec #15 Attacking AI**
- **§3.3 RUNNER role assignment** — eligible players are assigned the RUNNER role by
  `EntityId`-ascending iteration order until a cap fills; §2.3 explicitly states `Pace` and
  `Dribbling` are "declared for future RUNNER eligibility weighting... not yet consumed." Which
  player should make the dangerous run — a judgment that in football depends heavily on pace — is
  arbitrary ID order today. Pattern (d).
- **§3.4 Run parameter generation** — depth/timing for every RUNNER on a team comes from an identical
  team-style constant; the same §2.3 note confirms `Pace` is declared but unused here too. Pattern (d).

### Tactical / positional layer

**Spec #23 Dismarking & Marker-Awareness AI**
- **§3.1/§3.3 `MarkingPressure` / dismark offset** — `MarkingPressure = proximity01 × dwell01`, with
  no skill term for either the marked player's off-the-ball movement or the marker's own marking
  ability — pure geometry + elapsed time. Notable asymmetry: the *sibling* formula in the same spec
  (FR-DM-010's marked-pass-target penalty, §3.4) does blend in passer attributes for an analogous
  judgment. **Disclosed**: the spec's own §7.1 names "attribute-scaled dismark quality" as a
  deliberately deferred Stage 2+ item. Pattern (a)/(c), disclosed limitation.

### Player / season core layer

**Spec #27 Squad / Player Data Layer**
- **§7.1 (FR-SQ-026) `LineupSelector`** — starting XI selection is "position-partitioned greedy
  selection by mean-attribute rating, `PlayerId` tie-break, no RNG." No fitness/condition, no form,
  no role-fit weighting; averaging all 31 attributes doesn't distinguish a goalkeeper's
  outfield-irrelevant stats from a winger's. Pattern (c).

**Spec #28 Player Progression & Lifecycle**
- **§3.1 / Appendix A `ClassifyAgeBand`** — daily growth/decline is driven by three hard age-cutoff
  bands (Growth <24, Stable 24–30, Decline >30) with a flat ±1/year step inside each — a cliff at an
  exact integer age, no continuous curve, no per-player variance. Notably, even the "deep" tier
  (`curveEnabled`) still calls the identical 3-way `ClassifyAgeBand` — only the magnitude within a
  band varies. §1.3 promises "per-attribute CA/PA growth-decline curves keyed to age"; no
  age-continuous curve exists anywhere in the spec text. Pattern (b) **and** (d).
- **§3.4 / Appendix A `RETIREMENT_AGE=36`** — retirement is a single hard integer-age comparison with
  no draw and no attribute input — no fitness/robustness/position modulation (goalkeepers, who in
  real football play markedly longer careers, retire on the identical clock as outfield players).
  Pattern (b)/(c).

### Management layer — batch A

**Spec #31 Transfers, Contracts & Negotiation**
- **§3.2 `EvaluateOffer` / Appendix C** — accept/reject is `fee >= counterpartyValuation`, a single
  hard threshold; the spec's own worked example states a bid one currency unit under value is
  certain-rejected, one at value is certain-accepted — no acceptance band, no risk/willingness
  modeling, no `CounterOffered` path (deferred to an unbuilt deep tier). Same shape as ERR-008-019.
  Pattern (b).
- **§3.1/KD-1 valuation baseline** — club-need and personality (#33) are folded in only as
  multiplicative biases that default to exactly neutral (`1000‰`) when the deep-tier flag is off — so
  the shipped baseline negotiation has zero situational/personality awareness, gaining it only if a
  separate optional system is toggled on. Same "reverts to blind behavior when a sibling spec's dial
  is off" shape as DT #8 §3.2.2.1. Pattern (d).

**Spec #34 Staff & Backroom**
- **§3.4 `EvaluateStaffOffer`** — identical hard-threshold shape to #31: `wage >= wageDemand`, with no
  modeling of club prestige, candidate personality/ambition, role fit, or risk. Pattern (b).

**Spec #41 Injuries & Medical**
- **§3.4 `AssembleRiskScore`** — blends training risk, match-appearance load, hard contacts (zeroed at
  Stage 2), and an attribute-derived robustness term, but never includes player **age** anywhere in
  the formula or method signature, despite age being one of the best-established real-world
  injury-risk factors and already available on #27's `PlayerRecord` (and already used by #31's own
  valuation formula). Pattern (c) — a formula that presents as multi-factor risk assembly but omits a
  well-established factor already available elsewhere in the codebase.

### Management layer — batch B

**Spec #43 Competition Structure**
- **§3.3 (FR-CP-026) Knockout tie-break** — a level knockout tie is resolved by `KeyedDraw(...) mod
  2` — a bare 50/50 coin flip with **zero** reference to either team's quality, form, or
  penalty-taking ability. More stripped-down than the calibration examples: no attribute is consulted
  at all. Pattern (a)/(c).

**Spec #54 Manager Career, Reputation & Job Market**
- **§3.1 `EvaluateTenure` (FM-MC-01)** — the sacking decision is a two-band hard threshold on
  `ConfidencePermille` alone: below the floor (200) termination is unconditional regardless of
  objective performance ("the objective cannot save him"); above the ceiling (400) continuation is
  unconditional regardless of how badly the objective is failing. 199 vs. 201 confidence is the
  difference between certain dismissal and a conditional reprieve, with no weighting for recent form,
  cup run, transfer-window timing, or fixture congestion. Pattern (b) — arguably the highest-stakes
  instance of this pattern found in the whole review.
- **§3.4 / Appendix C `ReputationOf` (FM-MC-04)** — reputation is a flat linear accumulator: a fixed
  per-season increment, a fixed 60 points per trophy regardless of competition prestige/tier, and a
  fixed per-mille term per tenure-ending category with no situational weighting. Every trophy and
  every tenure-ending counts identically no matter the context. Pattern (c).

### Late-wave specs

**Spec #36 National Teams & International Management**
- **§3.4 `SelectCallUps`** — call-ups are `Sort(eligible, by: MeanAttributes desc, PlayerId asc)`
  capped only by a per-club count — no positional-balance constraint anywhere, so nothing prevents a
  squad with too few (or zero) recognized goalkeepers, and "mean attribute" blindly averages stats
  irrelevant to a player's position. Pattern (c).

**Spec #46 News, Inbox & Man-Management**
- **§3.4 `TryTalkToPlayer` (FR-NW-022)** — the outcome is a flat, pre-authored catalog value keyed
  only on which dialogue option was picked. FR-NW-022 makes this an explicit MUST and the spec
  actively argues against the "obviously desirable" alternative (reassurance landing harder on an
  unhappy player), so the target's personality (#33's `PersonalityProfile`, which exists and is
  populated elsewhere in the project), current morale, and relationship history never enter the
  formula. Governance frames this as "man-management" — an interpersonal, context-sensitive mechanic —
  but the formula is a context-free lookup. Pattern (c)/(d), and unusually, the simplification is
  explicitly *defended* in the spec text rather than merely disclosed.

---

## 4. Specs reviewed with no findings of this pattern

Cross-cutting infrastructure (no football-judgment surface, or the "decision" is a deterministic
tie-break/ordering rule rather than a football judgment): **#16** Deterministic Simulation, **#17**
Event System, **#18** Performance Optimization Strategy, **#19** Testing Strategy & Framework,
**#20** Code Standards & Style Guide.

Physics layer (continuous, multi-factor, or honestly-disclosed simplifications): **#2** Agent
Movement, **#3** Collision System, **#5** Pass Mechanics, **#9** Fixed64 Math Library (not
applicable — pure arithmetic library).

AI / mechanics layer: **#7** Perception System, **#12** Positioning AI.

Tactical / positional layer: **#21** Tactical Instructions, **#22** Living World System, **#24**
Scripted Build-Up Structures, **#25** Positional Rotations (explicitly disclaims modeling
player judgment at this trigger), **#26** Tactical Presets & AI-Manager Selection.

Player / season core: **#29** Training System, **#30** Season & Competition Loop, **#37** Match
Analytics & Statistics, **#38** UI / Client Framework, **#49** Localization & Accessibility.

Management layer: **#32** Scouting & Player Knowledge, **#33** Personalities, Morale & Squad
Dynamics, **#40** Club Finances & Economy, **#42** Youth Academy & Intake, **#44** Discipline &
Suspensions, **#45** Board & Ownership Dynamics, **#53** Club Infrastructure & Facilities.

Late-wave / presentation / production: **#35** Media & Press Interactions, **#39** Steam Packaging &
Release Engineering, **#47** New-Game Setup & Database Editor, **#48** Match Presentation Depth,
**#50** Save Migration & Versioning, **#51** Audio & Sound Design.

(53 of 53 specs reviewed: 2 carried forward from the prior session — #8, #11 — plus 51 reviewed
fresh in this pass, of which 24 total specs across both passes returned at least one finding.)

---

## 5. Summary count

| | Count |
|---|---|
| Specs reviewed | 53 / 53 |
| Specs with ≥1 finding | 24 |
| Specs with no findings | 29 |
| Total findings recorded | 34 |
| Findings already fixed (`ERR-008-019`) | 1 |
| Findings open | 33 |

No fixes were applied in this pass. No `ERR-` ids were allocated. Prioritization and remediation are
a separate, later step — governed by §6 below.

---

## 6. Remediation doctrine

> **Provenance:** converged with the owner in a working session on August 4, 2026, before any fix
> landed. Recorded here so each individual fix cites the principle it applies instead of
> re-litigating the approach up to 33 times. Every fix commit for a §2/§3 finding should name the
> principles (P1–P5) it relies on in its design note or ERR entry.

### 6.1 The organizing frame

Every player action follows **Play recognition → Decision → Execution** (owner-stated, and already
the project's Perception #7 → Decision Tree #8 → Mechanics/Physics pipeline). Worked example: a
player receives the ball facing the opponent's half; he *recognizes* a teammate starting a run into
space (Vision), *decides* to play it (Decisions/Composure, presence of opponents, body shape), and
*executes* the pass into space (Passing/Technique, with Anticipation informing where the space will
be). Each §2/§3 finding is a place where one of those stages was collapsed into bare geometry, a
hard threshold, or silently omitted.

The frame has known failure modes, each with a standing mitigation — these are binding on every fix:

| Frame flaw | Standing mitigation |
|---|---|
| A stage implemented as a binary gate recreates the cliff defect one layer up (a missed recognition deletes the option; downstream attributes never consulted) | Stages degrade **assessment quality**, never delete options outright (P1, P2) |
| The same attribute entering two stages double-counts silently | The attribute ownership ledger (P3) |
| Decide-then-execute commits to a frozen snapshot — the pass is aimed at where things *were*. The flaw is not the ~100 ms latency (that is realistic reaction time); it is the lock onto a stale coordinate | Decisions output **intent** (P4). Coordinates are fine as targets *provided* the coordinate is "where a teammate will arrive," not a lock onto a player's current position |
| Coordination is two-sided; a lone reader of moving obstacles cannot model it | Explicit signals — run-intent events, set-piece routine targets (P4) |
| Sequential stage failure rates compound multiplicatively | Calibrate the end-to-end chain, not each link (P5) |

### 6.2 The five principles

**P1 — Continuous, never a cliff.** Every hard step-function on a continuous football judgment
becomes a smooth blend (ramp, falloff, or probability), per the `ERR-008-019` precedent. A 1-point
attribute difference or a 2 cm positional difference must never flip an outcome discretely. This
alone covers the pattern-(b) findings: the stumble AND-gate (#6), the press fatigue ceiling (#13),
the WEAK_RECEIVER trigger (#13), age bands and retirement (#28), offer thresholds (#31, #34),
sacking bands (#54), the catch-vs-parry cutoff (#11).

**P2 — Skill is discrimination fidelity, not a bonus.** Where an attribute enters a *recognition*
or *assessment* judgment, it controls how **accurately** the actor perceives the true situation,
blending toward the population-average as skill drops:

```
perceived_value = neutral + fidelity × (true_value − neutral)
```

with `fidelity` rising in the assessing attribute. A low-skill assessor sees everyone/everything as
average — which is exactly the current attribute-blind behavior, so low skill degrades gracefully
to today's engine rather than to something new. High skill sees the true picture and is
*selectively* brave, not uniformly braver. No RNG enters assessment at this stage (deterministic
mis-weighting, not noise; noise is a possible later layer and would need its own draw-ordering
design). This is the fix shape for every attribute-blind proxy: pass-lane and shot-lane
interceptors (#8), the first-touch interception radius (#4), press/cover selection (#13), the
tackle-mode gate (#14), aerial duels (#10, #11).

**P3 — One attribute, one stage (the ownership ledger).** Each judgment documents which attribute
owns which stage, and an attribute enters a given judgment **once**. Ledger entries fixed so far
(owner-confirmed):

| Stage | Owner |
|---|---|
| On-ball play recognition (reading the current picture: who is dangerous, who is open) | **Vision** |
| Off-ball / predictive recognition (where play *will* go: space, arrival points, interception) | **Anticipation** |
| Decision under pressure (option choice, risk appetite) | **Decisions**, **Composure** |
| Execution (delivering the chosen action) | Technical attributes (**Passing**, **Finishing**, **Dribbling**, …) + physical |

No new "play recognition" attribute is added — **Vision is that attribute**; adding one would
ripple through #27's data layer and every attribute-consuming spec for no modeling gain. This
ledger is also the discharge path for the pattern-(d) findings ("declared for future use, never
consumed"): consuming a declared attribute means finding its stage here first.

**P4 — Intent is a first-class object.** Decisions target *intents*, not frozen entity
coordinates. Three owner-agreed forms: (i) **pass-to-space** — a pass may target a pitch
coordinate chosen as "where the teammate will arrive," which is the fix behind #8 §3.1.3's
current-position-only candidate bound; (ii) **run signaling** — a runner emits an explicit
run-intent (target point, start tick) on the event bus, consumed through the passer's recognition
stage (gated by Vision), replacing one-sided mind-reading and ID-order run assignment (#15);
(iii) **routine targets** — the same mechanism covers dead-ball situations, where a set-piece
executor aims at areas/players from a routine rather than at a teammate's standing position.
P4 items are **mechanism-class** (see §6.3): they need a design supplement, not a formula patch.

**P5 — Calibrate the chain, pivot on today's baseline.** Every fix is tuned so that an
average-attribute actor in the average situation reproduces ≈ today's behavior — the current match
balance is the pivot, not a casualty. New constants land as first-guess `[GT]`s; real calibration
waits for a complete-engine pass per **KD-W1** (`match-engine-wiring-backlog.md`). Calibration
targets are **end-to-end outcome rates** (pass completion, chance creation), not per-stage rates —
sequential stages compound, so per-stage realism can still produce absurd chain totals. Attribute
profiles are differentiated by *how* they fail, not only how often: a 15-Passing/10-Vision player
completes what he attempts but never sees the killer ball; a 15-Vision/10-Passing player attempts
more line-breaking passes and misplaces some. Similar completion percentages, very different chance
creation — that separation is the acceptance test that the stages are genuinely distinct.

### 6.3 Finding classes and their process

- **Formula-patch class** (most findings): fixable in place by P1–P3. Process per fix:
  `err-file-and-backprop` (ERR id allocated at fix time), spec section + code patched in the same
  commit, first-guess `[GT]`s, tests mirrored home/away plus attribute-extreme cases, calibration
  deferred per P5/KD-W1.
- **Mechanism class** (needs new coordination or data surfaces, P4 or new state): the #8
  pass-to-space bound, #15 RUNNER assignment + run parameters, #36 positional-balance constraint,
  #27 lineup-selector role fit. These get a design supplement first (`docs/tracking/*-design.md`
  per the standing governance class), then implementation.
- **Governance class**: #46 `TryTalkToPlayer`, where the spec actively *defends* the context-free
  lookup — overturning it is an owner decision about the spec's stated design intent, not a patch.
- **Management-layer findings** (#31, #34, #54, #43, #36, #27, #28): the three-stage frame does not
  map literally (there is no "execution" of a sacking), but P1, P3, and P5 apply unchanged.

### 6.4 First worked example (chosen, not yet implemented)

**#8 §3.1.3.3 pass-lane interceptor test** — owner-selected as the template fix. Converged design:
each opponent near the lane contributes `distance_falloff × perceived_ability`, where
`distance_falloff` fades smoothly from 1.0 near the lane line to 0 at the corridor edge (P1), the
defender's true ability scalar is built from his **Anticipation + Pace** (≈ 0.6–1.4 around
average), and `perceived_ability` applies the passer's **Vision** as discrimination fidelity per
P2's formula (Vision 1 ⇒ everyone looks average ⇒ today's behavior; Vision 20 ⇒ true picture).
Lane score stays `1 − Σweights / PASS_LANE_DIVISOR`; an average defender dead-center still counts
≈ 1.0 (P5). Vision's existing §3.2.2 PASS-utility term is unchanged — it rewards vision generally;
the fidelity term owns risk discrimination only (P3, no double-count). Plumbing: the engine already
holds every agent's `DtAgentAttributes`; the pipeline gains a read of opponent Anticipation/Pace —
the perception system is untouched. The **shot-lane check (§3.1.4)** shares the geometry and is
deliberately deferred to a follow-up fix (owner call, keep the template change small).

### 6.5 Adjacent gap recorded (not a §2/§3 finding)

**Pairwise playing familiarity does not exist anywhere in the design.** #33 owns a pairwise
*social* relationship scalar (chemistry/cliques are derived reads over it); Agent Movement #2's
`PerformanceContext` Stage-4 `ContextModifier` reserves per-player `TacticalFamiliarity` and
`TeamChemistry` hooks — but nothing maps any of it into *match-sim pairwise* terms ("this passer
reads this runner's movement faster"). It is the natural third input to P4's run-signal handshake
(signal + passer Vision + passer↔runner familiarity) and needs a small design decision about
ownership (#33's graph feeding #2's gateway, per-pair rather than per-player). Candidate design
supplement; deliberately not designed here.
