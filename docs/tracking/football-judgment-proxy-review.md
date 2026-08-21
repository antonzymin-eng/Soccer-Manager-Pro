# Football-Judgment Proxy Review

> **Created:** August 4, 2026
> **Updated:** August 21, 2026 — **§6.3 gains an ASSEMBLY-LESS class, and the backlog's workable queue is
> now 24 rather than 32.** Six of the specs itemized in §2/§3 have no `src/` assembly — **#31, #34, #36,
> #43, #46, #54** — carrying **8 of the 32 open findings**. Their fixes are deferred BY RULE to those
> specs' T0 landings: §6.3's formula-patch process requires spec + code in the same commit, and a fix
> here is not landed until a test fails when it is reverted — with no assembly neither half can be
> executed, so the fix would ship as prose with nothing enforcing it, which is precisely the class the
> `ERR-008-021`/`-022` chain demonstrated when three consecutive hand-derived verification claims were
> falsified on first execution. Normative source: `path-to-playable-roadmap.md` **C6** (new). The
> finding counts are UNCHANGED — 34 recorded, 2 fixed, **32 open** — this reclassifies when 8 of them are
> workable, not whether. Deferral is not dismissal: they stay itemized here and are discharged at T0.
> **Updated (prior):** August 6, 2026 — **the adversarial review over the `ERR-008-021` landing LANDED as
> `ERR-008-022`** (§6.4.2). It found that -021's overlap model was being fed by a lane test that
> discarded the **far-post blocker on 100% of 20,213 sampled off-centre shooters** and dropped a
> keeper standing on his line at goal centre for *every* shooter position — the far bound was a plane
> through the goal **centre**, not the goal line — so the fix achieved substantially less than it
> claimed. Two further hard predicates in the same derivation were larger cliffs than the one -021
> removed: `GOAL_MIN_SHOT_DIST` (1.000 ⇒ 0.050 across 1 cm, taking the SHOOT option with it) and the
> goalkeeper classification (0.768 ⇒ 0.311 across 2 cm, which -021 had widened to 0.551). All three
> fixed. **Three of -021's own verification claims were false** and are corrected: the P5 exactness
> argument (holds only for `h ≤ halfArc`; up to **2×** above it), the test count (**10** locks / 9
> evaluable / 5 fail / 4 pass, not "9 / 5 of 8"), and the worked example (its opponent was classified
> a goalkeeper, so every number in it was unreachable). The suite was inadequate too — a mutant
> restoring the pre-fix over-blocking passed all ten locks, and the null-view lock was a tautology.
> Suite now 15. Gate NOT runnable in the authoring environment. **32 itemized findings remain open.**
> **Updated (prior):** August 5, 2026, latest same day — **the §6.4 shot-lane follow-up LANDED as
> `ERR-008-021`**, discharging the deferral the ERR-008-020 template fix opened. #8 §3.1.4.3 /
> §3.2.3.2's occlusion test held the pass lane's two defects rather than one: an opponent
> contributed his **whole** angular width when his angular centre fell inside the goal arc and
> **nothing at all** when it fell outside — a defender standing squarely across the near post scored
> the shooter a *fully open goal*, and 4 cm of lateral position stepped `GoalOpeningScore` by 0.41
> (0.595 → 1.000) — and the width was body radius alone, so blocker identity never entered the read.
> Fixed to the true angular **overlap** of the blocking disc with the goal arc (continuous by
> construction — P1 needed no ramp constant here) scaled by the blocker's Anticipation/Positioning
> ability read through the shooter's Vision fidelity (P2, sharing §3.1.3.3's floor as one dial). The
> **goalkeeper is exempt from the ability term** — #11 §3.5/§3.7.0 owns keeper shot-stopping, so
> pricing it here too would charge the shooter twice (P3). P5 holds *exactly*: the old rectangle and
> the new trapezoid both integrate to `4h·halfArc` over a uniformly-placed blocker. **Digest
> invariance is not claimed** — the model is live on every generated shot. 10 test locks — 5 of the 9 that can be evaluated
> against the old model fail on it; gate NOT runnable in the authoring environment. **The 34-finding tally is unchanged**: the shot lane was
> never itemized as its own §2/§3 finding — it surfaced in §6.4 at the -020 landing — so 32 itemized
> findings remain open.
> **Updated:** August 6, 2026, later same day — **ERR-008-021 AR-1: 1 High, 7 Medium, 5 Low, all
> fixed.** The High: the landed P3 exemption keyed on the whole 6 m GK band rather than the
> goalkeeper, so every near-goal defender escaped the new weighting — inert precisely where shots
> are blocked. Now a single **GK candidate** (goal-line-nearest in band) is exempt and every other
> blocker is weighted (radius stays per-band). The P5 claim below is corrected the same way as
> ERR-008-019's a day earlier: exact only at the ability midpoint (raw 10/11) or under a null
> view — the all-default 10/10 profile reads ≈ 0.979, so the pivot is approximate, which is what
> P5 ("≈ today's behavior") actually requires. See `spec-error-log.md` v1.68 for the full list.
> **Updated (prior):** August 6, 2026 — **the shot-lane follow-up deferred at the ERR-008-020 landing is
> CLOSED, landed as `ERR-008-021`** (#8 §3.1.4.3/§3.2.3.2 step 3a): each blocker's
> occluded arc now scales by §3.1.3.3's Vision-read Anticipation/Pace `perceived_ability`
> (doctrine P2) — no new constants, the ERR-008-020 `[GT]`s reused verbatim (one calibration
> lever, KD-W1); the single GK candidate's arc stays purely geometric (doctrine P3 — keeper
> quality is priced once, at the #11 save; single-candidate form per AR-1 H-1, see above);
> ability-midpoint/null-view = 1.0 reproduces those arcs exactly
> (P5). Not one of the 34 itemized findings (it was ERR-008-020's deferred scope note), so the
> §5 counts are unchanged. Digests move where a generated SHOOT has a non-neutral
> blocker in the path, as intended. Gate NOT runnable in the authoring environment; CI on push
> is the gate.
> **Updated (prior):** August 5, 2026, even later same day — **ERR-008-019's digest-invariance claim
> RETRACTED for the full-range form** at the adversarial review over the landing (documentation
> only; formula, constants and the four test locks untouched). The argument assumed a 0.5 m
> possession radius; the engine's production paths are `RunLooseBallPickup` (§5.Z Phase H, KD-H3,
> `LooseBallPickupRadiusM` = **1.0 m**, ball left where it lies) and first touch (1.0 m), and
> nothing re-anchors the ball to the holder or drops possession on separation afterwards. A
> MIDFIELD ball at x → 70⁻ with the holder 1.0 m goal-side therefore reaches just above **34.0 m**
> — inside raw 19's range gate (34.21 m), where the full-range ramp gives ≈ 0.524 against the old
> step's 0.55 — so a generated option **can** score differently. The behaviour change is
> owner-intended; the superseded narrow ramp's disjoint-bands argument survives (its band caps
> at 29.0 m, still disjoint from the corrected bound). Gate NOT runnable in the authoring
> environment.
> **Updated (prior):** August 5, 2026, later same day — ERR-008-019 owner revision: the long-shot ramp
> widened to the FULL attribute range (`LONG_SHOT_RAMP_HALF_WIDTH` 0.05 → 0.25, its maximum
> valid value) — raw 1 exactly 0.05, raw 20 exactly 0.55, every point between moves the
> modifier ≈ 0.026, no plateaus. P5 holds (same midpoint, same uniform-population mean 0.30);
> still digest-invariant (only raw 20 can generate a MIDFIELD SHOOT, and there ramp = step).
> **Updated (prior):** August 5, 2026 — the long-shot cliff GENUINELY FIXED this time, landed as
> `ERR-008-019` (the soft-reserved id, re-verified free at landing): §3.2.3.1's midfield hard
> threshold is now a linear ramp per doctrine P1/P5. §2/§5 updated — 2 fixed, 32 open. Note the
> branch is production-unreachable in the only band the fix changes (the ramp differs from the old step only at A_LongShots ≤ 0.6, whose §3.1.4.2 range gate caps at 29.0 m, while a generator-reachable MIDFIELD SHOOT needs ≥ ~34.5 m of range — disjoint bands, so no generated option ever scores differently), so no digest moves; gate NOT runnable in the authoring environment.
> **Updated (prior):** August 4, 2026, latest same day — the §6.4 template fix LANDED as `ERR-008-020`;
> §2/§5 corrected: the "ERR-008-019 FIXED" status the prior session recorded was false (no log
> entry, cliff still live, no branch carries a fix) — that finding is re-opened and its id
> soft-reserved.
> **Updated (prior):** August 4, 2026 — §6 remediation doctrine added (owner-converged in session;
> see §6 provenance note). Findings §§2–5 unchanged.
> **Status:** FINDINGS LOG (§§1–5) + REMEDIATION DOCTRINE (§6, the owner-approved general
> approach each fix must cite). Remediation is underway: **three fixes landed** — `ERR-008-020`
> (August 4), `ERR-008-019` (August 5) and `ERR-008-021` (August 5, the §6.4 shot-lane follow-up) —
> all through the spec-error-log's full Filed/Status/Fix/Determinism-impact process. Two of the
> three close an itemized finding, so **32 of the 34 itemized findings remain open**; -021 discharges
> a deferral opened by -020 rather than closing a numbered finding of its own. `ERR-` ids for the
> rest are allocated at fix time, per the `err-file-and-backprop` skill.
> **Scope:** All 53 APPROVED specs in `SPEC_INDEX.md`, read directly from `docs/specs/`, regardless
> of whether a `src/` assembly exists yet for that spec.

---

## 1. What this review is looking for

A single defect *shape*, first caught as `ERR-008-019` (Decision Tree #8's
`ZoneModifier_SHOOT`, where a continuous football judgment — "is this a viable long-shot
position?" — was implemented as one hard threshold on a raw attribute, producing an 11x output
jump for a 1-point attribute difference; landed August 5, 2026 under the reserved id — see §2,
including the history of the false "FIXED" record it carried before that). The same review pass that found ERR-008-019 also found
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
  **FIXED — landed August 5, 2026 as `ERR-008-019`** (the soft-reserved id, re-verified free at
  landing per the correction below): the hard step — 0.55 strictly above the threshold, 0.05 at or
  below, an 11× jump across one raw LongShots point — is now a linear ramp in the unchanged
  shifted form, centred on the old threshold with `[GT] LONG_SHOT_RAMP_HALF_WIDTH` —
  **owner-revised same day from 0.05 to the full-range 0.25**, so the ramp spans the entire
  attribute: raw 1 exactly 0.05, raw 20 exactly 0.55, every raw point between moves the modifier
  ≈ 0.026, no plateau anywhere (doctrine P1; the midpoint sits at the old cliff and the
  uniform-population mean stays 0.30, so the P5 pivot holds; P2/P3 deliberately not in scope —
  long-shot inclination is the shooter's own execution capability, not a recognition judgment).
  **Digest invariance NOT established for the full-range form** — the claim originally recorded
  here was retracted at the same-day adversarial review over the landing. It assumed the shooter
  sat within a 0.5 m possession radius; the engine's production possession-granting paths are
  `MatchEngine.RunLooseBallPickup` (§5.Z Phase H, KD-H3 — `LooseBallPickupRadiusM` = **1.0 m**,
  and the ball is left where it lies) and the first-touch path (1.0 m), with no rule re-anchoring
  the ball to the holder or releasing possession on separation afterwards. So a MIDFIELD ball at
  x → 70⁻ with the holder 1.0 m goal-side reaches just above **34.0 m**, inside raw 19's range
  gate (20 + (18/19) × 15 = 34.21 m), where the ramp gives ≈ **0.524** against the old step's
  0.55 — a generated option can score differently, and the ramp is behaviour-visible today
  through the pickup path. The behaviour change is owner-intended; the superseded narrow ramp
  (0.05) survives the corrected premise (its band caps at 29.0 m, still disjoint from > 34.0 m).
  Gate NOT runnable in the authoring environment.
  *History:* **CORRECTION (August 4, 2026): the "FIXED as `ERR-008-019`" status this entry originally
  carried was false.** Verified against both this branch and `origin/main` at the ERR-008-020
  landing: no `ERR-008-019` entry existed in `spec-error-log.md`, the `LONG_SHOT_THRESHOLD = 0.75`
  cliff was still live in `UtilityWeights.cs` / `UtilityScorer.cs` and in
  `section-3-2-3-to-3-2-9.md`, and no branch carried a fix — the prior session recorded a fix (and
  a green gate) that never landed anywhere: the root `CLAUDE.md` fabricated-claims trap. Pattern (b).
- **§3.1.3.3 PASS lane "interceptor" test** — pure geometry (distance/angle of defenders to the
  passing lane). No defender attribute (anticipation, pace) enters the interception-likelihood
  calculation, so a slow, poor-anticipation defender scores as equally threatening an interceptor as
  a fast, high-anticipation one standing in the identical spot. Pattern (a).
  **FIXED — landed August 4, 2026 as `ERR-008-020`**, the doctrine's template fix (§6.4).
- **§3.1.4.3 / §3.2.3.2 SHOT lane occlusion test** — *not itemized in the original sweep*; it
  surfaced in §6.4 as the geometry the template fix deliberately left behind, and is recorded here
  at its landing so the #8 record is complete. It held **both** of the pass lane's defects.
  Pattern (b): the occlusion test was *containment*, not overlap — an opponent contributed his whole
  angular blocking width when his angular centre fell inside the goal arc and exactly nothing when it
  fell outside, so a defender standing squarely across the near post scored the shooter a **fully
  open goal**, a defender a centimetre the other side scored a width half of which lay behind the
  post, and 4 cm of lateral position stepped `GoalOpeningScore` by 0.41. Pattern (a): the width was
  body radius alone, so blocker identity never entered the shooter's read of the goal.
  **FIXED — landed August 5, 2026 as `ERR-008-021`**: true angular overlap (P1, continuous by
  construction — no ramp constant needed) × Anticipation/Positioning ability read through the
  shooter's Vision fidelity (P2, sharing §3.1.3.3's floor as one dial), goalkeeper exempt from the
  ability term because #11 owns keeper shot-stopping (P3). P5 holds exactly — the old rectangle and
  the new trapezoid integrate identically over a uniformly-placed blocker. Digest invariance **not**
  claimed: the model is live on every generated shot. Gate NOT runnable in the authoring environment.
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
| Findings fixed (`ERR-008-020` §3.1.3.3 template, August 4; `ERR-008-019` §3.2.3.1 long-shot ramp, August 5) | 2 |
| Findings open | 32 |

*(Updated August 5, 2026: `ERR-008-019` — the long-shot cliff, this review's founding finding — is
now genuinely landed; see §2. Corrected August 4, 2026: this table originally counted that same
fix as already landed when it never was — the §2 history note records the false-claim episode.)*

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
- **Assembly-less class — deferred BY RULE, not by priority** *(added August 21, 2026)*. Six of the
  specs itemized in §2/§3 have no `src/` assembly at all — **#31, #34, #36, #43, #46, #54** — and they
  carry **8 of the 32 open findings** (#31 ×2, #34 ×1, #36 ×1, #43 ×1, #46 ×1, #54 ×2). Those fixes do
  **not** land ahead of their spec's T0 code. The formula-patch process immediately above requires
  spec + code in the same commit, and this project's standard for a landed fix is a test that fails
  when the fix is reverted; with no assembly, neither half can be executed, so what would ship is
  edited prose with nothing enforcing it — the same class as the three hand-derived verification
  claims the `ERR-008-021`/`-022` chain falsified on first execution. They stay recorded here and are
  discharged at their spec's T0 landing, under the same doctrine. Normative source and full evidence:
  `path-to-playable-roadmap.md` **C6**. Note this **subsumes the governance-class entry above** — #46
  is deferred on both grounds, and the owner decision it needs is better taken with the code in front
  of it. **The remaining 24 open findings are workable today and are this backlog's actual queue.**

### 6.4 First worked example (LANDED August 4, 2026 as `ERR-008-020`)

**#8 §3.1.3.3 pass-lane interceptor test** — owner-selected as the template fix; **implemented as
designed** (spec §3.1.3.3 v1.3 + `OptionGenerator`/`UtilityWeights`/`DecisionContext(±Assembler)`/
`DecisionTree`/`MatchEngine`, same commit; 6 `OptionGeneratorTests` locks incl. the away mirror;
gate NOT runnable in the authoring environment — no .NET SDK). Converged design:
each opponent near the lane contributes `distance_falloff × perceived_ability`, where
`distance_falloff` fades smoothly from 1.0 near the lane line to 0 at the corridor edge (P1), the
defender's true ability scalar is built from his **Anticipation + Pace** (≈ 0.6–1.4 around
average), and `perceived_ability` applies the passer's **Vision** as discrimination fidelity per
P2's formula (Vision 1 ⇒ everyone looks average ⇒ today's behavior; Vision 20 ⇒ true picture).
Lane score stays `1 − Σweights / PASS_LANE_DIVISOR`; an average defender dead-center still counts
≈ 1.0 (P5). Vision's existing §3.2.2 PASS-utility term is unchanged — it rewards vision generally;
the fidelity term owns risk discrimination only (P3, no double-count). Plumbing: the engine already
holds every agent's `DtAgentAttributes`; the pipeline gains a read of opponent Anticipation/Pace —
the perception system is untouched. The **shot-lane check (§3.1.4)** shares the geometry and was
deliberately deferred to a follow-up fix (owner call, keep the template change small) —
**that deferral is discharged: see §6.4.1.**

#### 6.4.1 The shot-lane follow-up (LANDED August 5, 2026 as `ERR-008-021`)

**#8 §3.1.4.3 / §3.2.3.2 goal-visibility occlusion** — the geometry §6.4 held back. Reading it out
found the pass lane's defect *twice over*, and the containment half was the more damaging:

- **Containment, not overlap (P1).** Step 4 counted an opponent's occlusion only when his angular
  *centre* lay inside the goal arc, and then counted his **entire** width. A defender whose centre
  sat a hair outside the post direction therefore contributed **exactly zero** — the shooter read a
  *fully open goal* with a man across his near post — while one a centimetre the other side
  contributed a full width, half of it behind the post and blocking nothing. On the suite's fixture,
  4 cm of lateral position moved `GoalOpeningScore` from 0.595 to 1.000. The score prices the SHOOT
  candidate (§3.2.3.1), gates its existence (§3.1.4.1) and drives `PowerIntent` (§3.5.3), so the
  discontinuity reached shot selection, shot value and shot speed alike.
- **Attribute blindness (P2).** The width was `2·atan(radius/distance)` — body radius alone.

Converged fix: the contribution is the true angular **overlap** of the blocking disc with the goal
arc. Unlike -019 and -020 this needed **no ramp constant, no half-width `[GT]` and no tolerance
epsilon** — the intersection is continuous by construction, and is simultaneously the geometrically
honest answer, so the over-blocking and the under-blocking are fixed by the same stroke as the cliff.
The overlap is scaled by the blocker's **Anticipation + Positioning** ability
(`SHOT_BLOCKER_ABILITY_MIN/MAX` = 0.6/1.4, league-average exactly 1.0) read through the shooter's
**Vision** as discrimination fidelity — reusing `LANE_VISION_FIDELITY_FLOOR` rather than declaring a
second one, since fidelity is a property of the assessor and a duplicate would be a parallel surface
rather than a parameter. The **goalkeeper is exempt from the ability term** and occludes on geometry
alone (P3): #11 §3.5's save model and §3.7.0's rush — which *sets* this geometry — own his
shot-stopping, so pricing it here as well would charge the shooter twice for one keeper.

**P5 pivot.** Over a uniformly-placed blocker the pre-fix rule integrated a rectangle of area
`4h·halfArc`; the overlap integrates a trapezoid of the same area. With the ability midpoint at
exactly 1.0 the attribute axis is neutral too, so the fix redistributes occlusion from a step to a
slope and from anonymous bodies to identified ones without opening or closing the goal on average.

> **Corrected at ERR-008-022.** This paragraph originally read "for every disc width and arc" — it
> is **false above `h = halfArc`**, where the old model's per-opponent clamp saturates its rectangle
> at `4·halfArc²` and the trapezoid does not (measured 1.198× at `h`=10°/`halfArc`=8.35°, **2.000×**
> at `h`=16.7°). That regime is a blocker inside roughly `d_goal × r / 3.66` of the shooter — ~2.7 m
> at 20 m out — which is routine. The claim was the stated reason no recalibration was needed; the
> reason is withdrawn and the residual left for the balance pass (KD-W1).

**Digest invariance is not claimed** (the -019 lesson applied at authoring time): this model is live
on every SHOOT candidate the generator produces and moves for any blocker who is not both exactly
average and wholly inside the arc. 10 `OptionGeneratorTests` locks, including the P5 pivot on the
*computed* path as well as the null-view path, the GK exemption proved by moving the keeper's
attributes between the extremes, and the away mirror. A reference implementation of both models
confirms 5 of the 9 evaluable-pre-fix locks fail on the old one; the four that pass pre-fix are the
two P5 pivot rows and null-view neutrality, by construction. Gate NOT runnable in the authoring
environment.

Two items were **recorded, not fixed** at this landing: `IsInShotPath`'s corridor end-bounds, and
§3.2.10's constant catalogue, which five consecutive #8 landings have now left behind. The first of
those turned out to be the larger of the two defects in this whole section — see §6.4.2. The
reasoning that deferred it ("front-of vs behind the goal line is a physical fact, not a football
judgment, so P1 does not obviously reach it") was wrong on its own terms: the bound was not a
judgment collapsed into a threshold, it was simply **the wrong plane**, and it was silently deleting
the far half of the goal from the model this section had just built.

#### 6.4.2 The adversarial review over the -021 landing (LANDED August 6, 2026 as `ERR-008-022`)

Three hostile passes over the ERR-008-021 landing. Headline: **the fix achieved substantially less
than it claimed**, because the lane test feeding it discarded much of the geometry the overlap model
exists to price.

- **(a) The far bound was a plane through the goal CENTRE**, not the goal line. For any off-centre
  shooter it cuts diagonally across the goal mouth, so the **far-post** blocker was dropped and the
  near-post one kept — on **20,213 of 20,213** sampled in-range off-centre shooters. A keeper on his
  line at goal centre gave `proj == distToGoal` exactly and was dropped for **every** shooter
  position: shooter (95,20) read **1.000, a completely open goal**. The mirror admitted an opponent
  standing *behind* the goal line at the keeper's 1.5 m radius. Now bounded by the goal-line plane.
- **(b) `GOAL_MIN_SHOT_DIST` was a 0.95 cliff** — 1.000 at 0.995 m of lane depth, 0.050 at 1.005 m,
  and since 0.050 is below `MIN_GOAL_VISIBILITY` it also deleted the SHOOT option. More than twice
  the step this section was opened to remove, in the same function. Now ramps (`[GT]
  SHOT_BLOCKER_NEAR_FADE_M`).
- **(c) The goalkeeper read was a predicate** whose boundary stepped `GoalOpeningScore` 0.768 ⇒ 0.311
  across 2 cm — and ERR-008-021 *widened* its worst case to 0.551 by making it attribute-dependent,
  three lines from the code it rewrote, unrecorded. Now a scalar `gkness` lerping the radius and the
  P3 exemption together (`[GT] GK_PROXIMITY_FADE_M`).

**Three of the -021 landing's own verification claims were false** and are corrected in the record:
the P5 exactness argument (above), the test count (**10** locks / 9 evaluable / 5 fail / 4 pass, not
"9 / 5 of 8"), and the §3.2.3.2 worked example — whose opponent sat 4.5 m from the goal line, so the
algorithm classified him a **goalkeeper** and exempted him from the very ability term the example was
demonstrating; all three of its numbers were unreachable.

**The suite was also inadequate to its own claim.** The over-blocking half of ERR-008-021 had no lock
at all — a mutant restoring the pre-fix full-width contribution passed all ten tests — 8 of 12
plausible mutants survived, every fixture put the shooter on the goal's centre line (making
`bisector` and the post clipping untestable, and the away "mirror" bit-identical to the home case),
and `ShotLane_NullAttributeView_IsAbilityNeutral` was a **tautology**: the helper's own
`if (attrs != null)` guard discards the differing arguments, so it asserted `f(x) == f(x)`. That is
the same shape the ERR-008-020 review caught one landing earlier, and the -021 commit message claimed
to have avoided it "at authoring time rather than at review". Suite now 15 locks; the pass-lane twin
tautology is fixed too.

**And the hardened suite's own headline lock was itself wrong — found by the first gate run, not by
review.** CI run 402 (PR #302, August 6, 2026) compiled and executed this work for the first time.
`ShotLane_FarPostBlocker_OccludesTheGoal` failed: expected 0.782157, got **0.728880**. The model was
right; the test read `ctx.OpponentGoalPostL`, which the home fixture defines as y = 30.34 — the post
*nearer* the (90, 24) shooter. Since the pre-fix goal-centre-plane bound **kept** the near post and
discarded only the far one, the lock named for this section's headline finding would have **passed
against the broken model**. The far post is now selected by geometry rather than by the `PostL`/
`PostR` label, which carries opposite sides in the file's two fixtures. Three consequences worth
carrying forward: the "12 of 12 mutants killed" figure overstates the far-bound mutant (the Python
harness killed it, the committed test did not); this is the **third** hand-derived verification claim
in the -021/-022 chain that execution falsified, after the P5 exactness argument and the §3.2.3.2
worked example; and the common factor in all three is that no compiler had ever run them. The rest of
the run was NOT clean, and the first version of this paragraph said it was. Build 0 errors and 127 of
128 `DecisionTree.Tests` passing are real — the sweep ran to completion — but the gate job was
cancelled at 16:59:45 before `run-gate.sh` reached its `Gate PASSED` line, and four hygiene checks
(link check, spec hygiene, file manifest, `.meta` integrity) were cancelled without ever being given a
runner. So the fix itself stands on measurement; it is the *gate* that has still never returned a
verdict, and the correction in `0612bcc` has never been compiled at all.

**Second review pass (AR-2, same day).** A hostile re-read of this fix found the two new ramps were
**not centred on the predicates they replace** — `laneWeight` ran 1.0 → 2.0 m and `gkness` 6 → 8 m,
i.e. entirely on one side. That is a systematic one-sided change in occlusion dressed as a continuity
fix, and it violates the same P5 pivot this entry criticises -021 for getting wrong: both ERR-008-019
and ERR-008-020 explicitly centred their ramps on the old cliff so the population integral is
preserved. Corrected to half-width either side (0.5 → 1.5 m and 5 → 7 m), so a blocker at exactly
`GOAL_MIN_SHOT_DIST` now contributes half his occlusion and one at exactly `GK_PROXIMITY_TO_GOAL`
reads half keeper. Every value lock is unchanged (all sit outside the ramp bands); the two continuity
sweeps were re-ranged to span the centred bands.

**Still recorded, not fixed:** `MIN_GOAL_VISIBILITY` remains a hard predicate on option *existence*
(what changed is that the opening now decays to it rather than jumping past it); the GK positional
proxy still reads a deep defender as part-keeper; §3.2.10's constant catalogue is now six landings
behind; and the P5 residual above waits for the balance pass. **The 34-finding tally is unchanged** —
the shot lane was never itemized as its own §2/§3 finding, so **32 itemized findings remain open**.
deliberately deferred to a follow-up fix (owner call, keep the template change small) — **closed
August 6, 2026 as `ERR-008-021`** (see the header entry above: outfield arcs × the same
perceived-ability scalar, GK arc geometric per P3, no new constants).

### 6.5 Adjacent gap recorded (not a §2/§3 finding)

**Pairwise playing familiarity does not exist anywhere in the design.** #33 owns a pairwise
*social* relationship scalar (chemistry/cliques are derived reads over it); Agent Movement #2's
`PerformanceContext` Stage-4 `ContextModifier` reserves per-player `TacticalFamiliarity` and
`TeamChemistry` hooks — but nothing maps any of it into *match-sim pairwise* terms ("this passer
reads this runner's movement faster"). It is the natural third input to P4's run-signal handshake
(signal + passer Vision + passer↔runner familiarity) and needs a small design decision about
ownership (#33's graph feeding #2's gateway, per-pair rather than per-player). Candidate design
supplement; deliberately not designed here.
