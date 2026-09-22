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
- every applied foul from either source re-arms the same global `FoulCooldownTicks = 180` debounce,
  suppressing later opportunities from both sources;
- a decided W2 tackle candidate outranks an ordinary collision candidate in the same tick under
  KD-F4's single-slot rule, so a qualifying collision can be displaced before application;
- every applied foul also creates a restart, changing later played time/contact opportunity.

Therefore collision fouls and tackle fouls **cannot be calibrated by summing two independently
measured rates**. The sources compete before application as well as feeding back after application.

The first implementation step after this preregistration lands is a **measurement-only** extension
that reports the complete live discipline stream and the competition between its sources. No
discipline `[GT]` moves in that instrument landing.

### 2.1 Required source-complete report

For every seed, and in aggregate, the instrument MUST report at least:

| Field | Meaning |
|---|---|
| `fromBehindCandidates` | collision/referee candidates that clear type/force/team/participation gates before the KD-F1 probability decision |
| `fromBehindCalled` | applied fouls whose source is `FROM_BEHIND` |
| `slideTackleCandidates` | already-adjudicated W2 tackle-foul candidates presented to the discipline path |
| `slideTackleCalled` | applied `SLIDE_TACKLE` fouls |
| `candidateDisplacedByDecided` | qualifying collision candidates that lose the single same-tick slot because a decided W2 tackle foul already owns it |
| `foulCooldownSuppressionsFromBehind` | otherwise-qualifying collision/referee candidates suppressed by the shared live foul cooldown |
| `foulCooldownSuppressionsSlideTackle` | already-adjudicated tackle-foul opportunities suppressed by the shared live foul cooldown |
| `totalFouls` | live production total; this is the rate target's numerator |
| `yellowCards` | total cautions, including the second caution that promotes an offender to dismissal; same convention as `MatchEngineDisciplineScenarios` |
| `straightReds` | direct `CARD_KIND_RED` dismissals |
| `secondYellowDismissals` | `CARD_KIND_SECOND_YELLOW` dismissals |
| `totalDismissals` | all sent-off outcomes; MUST equal `straightReds + secondYellowDismissals` |
| `qualifyingContactForce` distribution | p50/p75/p90/p95/p99/p99.9/max for `FROM_BEHIND` collision candidates |
| `playedTicks` | exact denominator actually run for the seed |

The card-counting convention is frozen to the existing scenario's state semantics: a second-yellow
dismissal contributes **one additional caution** to `yellowCards` and **one dismissal** to
`totalDismissals`. The instrument also reports the two dismissal subtypes separately, so the
straight-red band is never inferred from total dismissals. Do not reconstruct these buckets from a
two-way event-kind test; `CardIssuedEvent` has distinct yellow, straight-red and second-yellow
ordinals.

The applied-foul identity
`fromBehindCalled + slideTackleCalled == totalFouls` MUST hold unless a third production foul source
is found. It is only a reconciliation check, not evidence that the sources are independent; the
mandatory displacement and cooldown-suppression counters above expose their competition. A third
source is a **stop-and-localize finding**, not a bucket to silently fold into either existing source.

The instrument remains assertion-free on the measured rates. Its job is to make a zero, a drift, or
a source interaction arrive with its cause attached.

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

The full-length source-complete run uses the same force replay population if offline bracketing is
retained. The measurement-only instrument change MUST add the live production cooldown
`180` to the existing `{60, 300, 600}` diagnostic ladder; the structural cooldown itself remains
frozen under §4.1 and the ladder is descriptive, not a search over candidate cooldown values.

The football anchors remain the already-governed KD-F5 outcome targets:

- fouls: approximately **22 per 90**;
- cautions/yellows: approximately **3.5 per 90**;
- **total dismissals**: approximately **0.25 per 90**.

The red-card anchor is explicitly a target for **all dismissals**, not for the straight-red draw band.
No new external target is introduced by this preregistration.

### 3.1 Rare-dismissal rule and the corrected straight-red derivation

At 0.25 total dismissals per match, six matches contain only 1.5 expected dismissals. The corpus is
therefore unsuitable for fitting a straight-red probability directly from the aggregate dismissal
count.

More importantly, the old analytic derivation `RedCardProbability ~= 0.25 / 22` is **invalid** for
this engine. `RedCardProbability` controls only the direct straight-red band, while
`ApplyCardAndCheckSentOff` also dismisses an offender on a second yellow. The historical KD-F5
statement that second-yellow promotion was negligible is contradicted by the live engine semantics
and the later measured dismissal rate.

Therefore:

- the current production `RedCardProbability = 0.011` is recorded as a **historical/provisional
  value**, not as an analytically justified starting ratio;
- this preregistration does **not** fit or authorize a new `RedCardProbability`;
- every governed run MUST decompose `totalDismissals` into `straightReds` and
  `secondYellowDismissals` using §2.1's convention;
- the total-dismissal outcome is judged against §5's frozen rare-event ceiling;
- if the dismissal ceiling fails, the calibration stops. Before any straight-red candidate value is
  executed, a separate pre-result amendment must freeze how the measured second-yellow contribution
  is converted into the remaining straight-red budget. A larger corpus may improve that estimator,
  but corpus size does not repair the derivation by itself.

The stale `MatchEngineConstants.RedCardProbability` doc comment still describes the historical
`0.25 / 22` conflation. This preregistration treats that comment as non-authoritative and records
its correction as part of the eventual card-severity landing; no runtime value or source file moves
in this pre-result PR.

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

The current production starting value is **`FoulCallProbability = 0.030`**. It was already
recalibrated once after §5.Z.9: on July 27 (§5.Z.13) collision emission changed from one event per
tick of sustained overlap to one event per contact, cross-team from-behind opportunity rate moved
from roughly 58/s to 0.5/s, and the old 0.015 value produced roughly 0.4 fouls per 90. That history
is itself an invalidation precedent for §8.

Only the `FROM_BEHIND` source is governed by KD-F1's call probability. The final live fit therefore:

1. keeps the W2 tackle resolver constants unchanged;
2. measures **total live fouls**, not a sum of offline source estimates;
3. begins from `0.030` and varies only `FoulCallProbability` for the first rate fit;
4. selects against the ~22-fouls-per-90 anchor on the same six full-match seeds;
5. re-runs the chosen value live on the same corpus before it can be proposed for production.

If `FoulCallProbability = 0` would still leave the live total above the target envelope because of
already-adjudicated tackle fouls, the referee probability has no valid solution. That is a
**source-model finding**. Tackle `[GT]` values are not retuned inside this foul/card pass to make the
number fit.

Offline replay may bracket candidate probabilities, but it is never final evidence: the shared
cooldown, same-tick candidate displacement, and restart feedback make the live composed run
authoritative.

### 4.3 Card severity is outcome-constrained, not ratio-substituted

The yellow/caution target still supplies a useful starting ratio:
`3.5 / 22 ~= 0.159`, consistent with the current `YellowCardProbability = 0.16`.

There is **no corresponding direct `0.25 / 22` straight-red ratio**. The 0.25 target governs total
dismissals, while the engine reaches that outcome through both straight reds and second-yellow
promotions. The current `RedCardProbability = 0.011` is held unchanged during characterization
only because this preregistration does not yet authorize a corrected card-severity fit.

After the foul-rate fit, the six-match live run checks the caution rate, straight-red count,
second-yellow dismissal count, and total-dismissal rate separately. If card severity requires a
change, the straight-red estimator/value is preregistered in an amendment **before** candidate values
are executed. No post-result value is inserted directly into production.

---

## 5. Frozen acceptance posture

The existing `MatchEngineDisciplineScenarios` bands (3-90 fouls, 0-20 yellows, 0-5 dismissals per
90) are abandonment/plausibility guards. They are intentionally too broad to certify a calibration.

The final calibration landing MUST add a separate executable calibration regression over the frozen
six-full-match corpus. Before any result is observed, its coarse target-relative envelope is fixed as:

| Quantity | Required aggregate per-90 envelope | Purpose |
|---|---:|---|
| Fouls | **15 to 30** | rejects both silence and the known 35+/90 drift while leaving trajectory variance |
| Cautions/yellows | **1.5 to 6.0** | keeps the caution level in the neighborhood of the 3.5 target; includes second cautions per §2.1 |
| Total dismissals | **< 0.50** | rare-event ceiling over straight reds + second-yellow dismissals; deliberately fails at 0.50/90 |
| Cautions / fouls | **0.08 to 0.30** | catches a severity mix detached from the roughly 3.5/22 caution target without double-counting dismissals |

All four predicates are conjunctive; a run must satisfy their intersection. The point objective
remains ~22 fouls / ~3.5 cautions / ~0.25 total dismissals. Passing these bands is necessary but does
not replace the source-complete report or justify a value chosen for another reason.

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
6. changing **collision-event emission granularity or contact episode semantics** — explicitly named
   because the July 27 §5.Z.13 one-event-per-contact change already invalidated this exact fit;
7. changing collision classification, possession attachment/release, restart duration/flow, or any
   other mechanism shown to alter the foul opportunity population.

When any trigger occurs, rerun the **same frozen corpus and source-complete report**. Do not replace
seeds, shorten the run, or widen the acceptance envelope because the trajectory moved.

The final `[GT]` proposal is based only on the last post-trigger production head.

---

## 9. Durable evidence contract

Every governed run must retain enough material to reconstruct what was measured:

- the **landed preregistration commit/merge SHA**, recorded by the first governed run after this PR
  reaches `main` (it cannot be self-recorded in a pre-merge document);
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
