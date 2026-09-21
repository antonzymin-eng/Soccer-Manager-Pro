# Foul/Card Calibration + W3/W9 Preregistration

> **Created:** September 21, 2026  
> **Status:** **FROZEN PRE-RESULT — no result-bearing calibration run has been observed.**  
> **Production anchor:** `6cd05a03d69c84a09577df21f6401c69f60ecdb9` (PR #433 merge on `main`).  
> **Owners:** `foul-discipline-balance-design.md` for discipline semantics;  
> `match-engine-wiring-backlog.md` for KD-W1 and W3/W8/W9/W10 sequencing.  
> **Purpose:** Freeze the post-W2 foul/card measurement contract and the W3/W9 evidence/invalidation
> contract **before** any governed measurement or calibration result is observed.

---

## 1. Governance boundary

The canonical KD-W1 rule is unchanged:

> **KD-W1** — do not land a `[GT]` governing an unwired subsystem. Check
> `match-engine-wiring-backlog.md` first. Measure and fix defects freely; constants wait for one
> calibration pass against the complete engine.

This document is **not** a KD-W1 exception. It authorizes instrumentation and pre-wire
characterization after this preregistration lands, but it does not authorize a gameplay `[GT]`
change while the remaining Class-A wiring is incomplete.

Current production facts at the anchor:

- W2 is active in production and its post-#416 six-seed stall/non-vacuity revalidation is closed.
- W3, W8, W9, and W10 remain Class-A wiring items.
- W3 is blocked on the shared multi-agent `AGENT_BALL` contact-feed/fan-out dependency already
  recorded in the backlog; the feed and W3 are one dependency chain, not two independent fixes.
- W9 remains blocked on the Decision Tree `ActionType` ordinal-8 / 3-bit composure-noise ceiling.
- The historical shallow-ancestry and owner-held leakage items remain tracked residuals and do not
  change this measurement contract.

**Sequencing rule:** the wire order in `match-engine-wiring-backlog.md` stays authoritative. This
preregistration does not reorder W3/W8/W9/W10. The **final** discipline fit uses only a head after
the last Class-A wiring item has landed, unless the owner records a separate explicit KD-W1
exception. No such exception is granted here.

---

## 2. Why the existing foul diagnostic is not yet source-complete

The July/August `FoulRateDiagnosticTests` instrument observes collision-system
`AGENT_AGENT / FROM_BEHIND` candidates and can replay KD-F1's force-scaled referee-call gate
offline. That was sufficient before W2.

It is no longer sufficient as the sole calibration instrument:

- a W2 tackle foul is already adjudicated by Defensive AI #14 §3.6.5;
- it enters the engine's existing single foul-candidate slot as `ContactType.SLIDE_TACKLE`;
- `ApplyFoulIfCaptured` deliberately **does not** apply `FoulCallProbability` a second time to that
  source;
- more fouls create more restarts and therefore change played time/contact opportunity, so collision
  fouls and tackle fouls **cannot be calibrated by summing two independently measured rates**.

Therefore the first implementation step after this preregistration lands is a **measurement-only**
extension that reports the complete live discipline stream by source. No discipline `[GT]` moves in
that instrument landing.

### 2.1 Required source-complete report

For every seed, and in aggregate, the instrument MUST report at least:

| Field | Meaning |
|---|---|
| `fromBehindCandidates` | collision/referee candidates before the KD-F1 probability decision |
| `fromBehindCalled` | applied fouls whose source is `FROM_BEHIND` |
| `slideTackleCandidates` | already-adjudicated W2 tackle-foul candidates entering the discipline slot |
| `slideTackleCalled` | applied `SLIDE_TACKLE` fouls |
| `totalFouls` | live production total; this is the rate target's numerator |
| `yellowCards` | total yellow cards issued |
| `redCards` | total dismissals/red-card outcomes; if straight-red vs second-yellow is observable without new gameplay state, report both as subfields |
| `qualifyingContactForce` distribution | p50/p75/p90/p95/p99/p99.9/max for `FROM_BEHIND` collision candidates |
| `foulCooldownSuppressions` | candidates suppressed by the live foul cooldown, split by source if the source is observable |
| `playedTicks` | exact denominator actually run for the seed |

The report MUST reconcile `fromBehindCalled + slideTackleCalled == totalFouls` unless a third
production foul source is found. A third source is a **stop-and-localize finding**, not a bucket to
silently fold into either existing source.

The instrument remains assertion-free on the measured rates. Its job is to make a zero or a drift
arrive with its source attached.

---

## 3. Frozen calibration corpus

The calibration population is fixed **before results** to the six seeds already used by
`FoulRateDiagnosticTests`; no seed is added, removed, or replaced after output is seen:

| Seed |
|---|
| `0x0F1E2D3C4B5A6978` |
| `0x00000000D1A6D05E` |
| `0x0000000000000001` |
| `0x00000000ABCDEF12` |
| `0x0000000099887766` |
| `0x000000005A5A5A5A` |

Each seed runs **324,000 physics ticks = one full 90-minute match**. The aggregate therefore contains
six match-equivalents, rather than the old one-match-equivalent diagnostic. This is large enough to
fit the common foul/yellow rates without treating one 90-minute trajectory as a calibration sample.

The football anchors remain the already-governed KD-F5 targets:

- fouls: approximately **22 per 90**;
- yellows: approximately **3.5 per 90**;
- reds: approximately **0.25 per 90**.

No new external target is introduced by this preregistration.

### 3.1 Rare-red rule

At 0.25 reds per match, six matches contain only 1.5 expected reds. That is too sparse to estimate a
new red probability from the observed count without fitting noise.

Therefore:

- `RedCardProbability` is **not** fitted to the observed six-match red count;
- its KD-F5 starting value remains the analytic conditional ratio `0.25 / 22` unless a later,
  separately preregistered larger corpus is approved;
- the six-match red count is a validation/non-regression observation, not an optimizer input;
- second-yellow promotions, if material, are reported rather than compensated by silently lowering
  `RedCardProbability`.

---

## 4. Calibration procedure frozen before results

### 4.1 Structural constants are not rate knobs

The first candidate pass holds these constants at their current production values:

- `FoulImpactForceThresholdN = 1200`;
- `FoulCooldownTicks = 180`.

They are not jointly optimized with the foul rate. The July pass already established that the force
threshold is a candidate-quality gate and the cooldown is a temporal de-duplication rule; using them
as extra degrees of freedom to hit 22 would hide a changed contact model.

If the source-complete force/cooldown report shows that either structural premise no longer holds,
the calibration **stops** and files/localizes the model change. It does not search a wider threshold
or cooldown ladder until that revised model is preregistered.

### 4.2 `FoulCallProbability` is the collision-source rate lever

Only the `FROM_BEHIND` source is governed by KD-F1's call probability. The final live fit therefore:

1. keeps the W2 tackle resolver constants unchanged;
2. measures **total live fouls**, not a sum of offline source estimates;
3. varies only `FoulCallProbability` for the first rate fit;
4. selects against the ~22-fouls-per-90 anchor on the same six full-match seeds;
5. re-runs the chosen value live on the same corpus before it can be proposed for production.

If `FoulCallProbability = 0` would still leave the live total above the target envelope because of
already-adjudicated tackle fouls, the referee probability has no valid solution. That is a
**source-model finding**. Tackle `[GT]` values are not retuned inside this foul/card pass to make the
number fit.

Offline replay may bracket candidate probabilities, but it is never final evidence: restart
feedback makes the live composed run authoritative.

### 4.3 Card severity stays target-ratio based unless falsified

The starting conditional ratios remain KD-F5:

- yellow: `3.5 / 22 ~= 0.159`;
- straight red: `0.25 / 22 ~= 0.0114`.

The current values (`0.16`, `0.011`) are therefore treated as analytic starting values, not as
numbers to chase from one sparse trajectory. After the foul-rate fit, the six-match live run checks
whether the yellow/card mix remains compatible with those target ratios and whether second-yellow
promotion materially changes the red outcome.

A card-severity change, if the live evidence requires one, must be preregistered as an amendment
**before** candidate values are executed. No post-result value is inserted directly into production.

---

## 5. Frozen acceptance posture

The existing `MatchEngineDisciplineScenarios` bands (3-90 fouls, 0-20 yellows, 0-5 reds per 90) are
abandonment/plausibility guards. They are intentionally too broad to certify a calibration.

The final calibration landing MUST add a separate executable calibration regression over the frozen
six-full-match corpus. Before any result is observed, its coarse target-relative envelope is fixed as:

| Quantity | Required aggregate per-90 envelope | Purpose |
|---|---:|---|
| Fouls | **15 to 30** | rejects both silence and the known 35+/90 drift while leaving trajectory variance |
| Yellows | **1.5 to 6.0** | keeps booking level in the neighborhood of the 3.5 target |
| Reds | **< 0.75** | rare-event ceiling only; not a point estimator |
| All cards / fouls | **0.08 to 0.30** | catches a severity mix detached from the roughly one-in-six target |

The point objective remains ~22 / ~3.5 / ~0.25. Passing these bands is necessary but does not replace
the source-complete report or justify a value chosen for another reason.

**No-widen rule:** if a post-wiring production head fails one of these frozen calibration bands, the
next action is to remeasure/localize the changed source. Do not widen the band to restore green.

---

## 6. W3 preregistration

### 6.1 Dependency boundary

W3 is not merely a call to `ResolveHandContactDuel`. The backlog records one shared missing
multi-agent `AGENT_BALL` contact feed/fan-out used by the GK/Heading integration and cross-claim duel.

Before W3 implementation begins, its landing plan MUST state, in-repository:

- the owner of the shared feed;
- the exact producer and consumer surfaces;
- the slice boundary between generic `AGENT_BALL` fan-out and goalkeeper cross-claim policy;
- ordering/tick-phase semantics;
- snapshot/determinism consequences;
- tests and measurement evidence;
- implementation estimate.

This preregistration does not choose those design details.

### 6.2 Frozen W3 before/after evidence

On the six-full-match corpus, the pre-W3 and post-W3 arms use the same seeds, duration, reporting
transform, and production configuration. Report at least:

- `AGENT_BALL` multi-agent fan-out events;
- cross-claim eligibility episodes;
- registered duel participants;
- resolved hand-contact duels;
- successful keeper claims;
- cross and lofted-pass attempts/completions;
- header attempts/contacts if the shared feed changes their contact population;
- the complete foul/yellow/red report from §2.1.

The present pre-wire expectation that the cross-claim duel has no registered production participants
is a baseline fact, **not** a permanent zero lock. Post-W3 non-vacuity requires the production path to
exercise the feed and duel; exact football-rate tuning of claims remains a later calibration question.

---

## 7. W9 preregistration

W9 changes who decides a header and can change target selection/ball trajectories. It therefore has
a direct path to collision/contact opportunity even though it does not itself own discipline.

On the same six-full-match corpus, preserve before/after counts for at least:

- engine proximity-triggered header commits;
- Decision-Tree-emitted HEADER commits after W9 exists;
- header attempts, executed contacts, and failures;
- outgoing header target class when observable (goal/teammate/other);
- cross and lofted-pass attempts/completions;
- the complete foul/yellow/red report from §2.1.

The current `ActionType` ordinal-8 / 3-bit composure-noise ceiling is part of W9's implementation
scope. This preregistration does not authorize a digest rebaseline or choose the W9 fix; it only freezes
what must be measured across that change.

---

## 8. Invalidation and remeasurement triggers

The following changes invalidate any earlier foul/card measurement as a **final calibration basis**:

1. landing or materially changing the shared multi-agent `AGENT_BALL` feed;
2. landing W3 cross-claim wiring;
3. landing W9 DT-emitted HEADER wiring;
4. landing W8 or W10;
5. changing W2 tackle production reach/outcome semantics;
6. changing collision classification, possession attachment/release, restart duration/flow, or any
   other mechanism shown to alter the foul opportunity population.

When any trigger occurs, rerun the **same frozen corpus and source-complete report**. Do not replace
seeds, shorten the run, or widen the acceptance envelope because the trajectory moved.

The final `[GT]` proposal is based only on the last post-trigger production head.

---

## 9. Durable evidence contract

Every governed run must retain enough material to reconstruct what was measured:

- preregistration commit SHA;
- production/result head SHA;
- workflow run ID and job ID;
- exact command/environment gate;
- per-seed source-complete TSV or JSON;
- aggregate report;
- detailed console/TRX evidence where the instrument runs under NUnit;
- SHA-256 manifest or the repository's registered external-verifier contract for any committed
  evidence directory.

Interpretation is written **after** the raw evidence exists. This preregistration itself remains
pre-result; do not rewrite its frozen corpus, targets, acceptance envelope, or failure rules with
observed values.

---

## 10. Next actions after this preregistration lands

1. Extend `FoulRateDiagnosticTests` (or add a narrowly-owned companion instrument) to satisfy §2.1,
   with **no gameplay `[GT]` change**.
2. Execute the frozen six-full-match **characterization** on the current post-W2 production head.
   It is a baseline/source census, not the final KD-W1 calibration fit.
3. Complete the remaining Class-A wiring in the authoritative backlog order, collecting the W3/W9
   before/after evidence above.
4. Rerun the frozen source-complete corpus on the complete engine.
5. Only then execute/propose the governed foul/card fit and its executable calibration regression.
