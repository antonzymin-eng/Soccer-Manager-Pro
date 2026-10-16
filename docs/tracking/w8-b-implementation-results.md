# W8 B implementation and October 13 anchor comparison

> **October 16 — PR #511 re-handling amendment (1.2):** owner-approved own-release sanction at `fff7cf5b`, now supplemented with feet-only avoidance and same-frame foul disposal. Snapshot body **v27** supersedes B's v26; outer save format stays v3. The v26 and corrected-B measurements below are historical pre-law records. [#11 contract](../specs/goalkeeper-mechanics/section-3.md#313-own-release-hand-restriction-err-011-020) and [restart contract](match-flow-completion-design.md#51-keeper-own-release-re-handling). The post-law comparison/anchor and final-head CI/Unity remain pending; D-07 general outside-area handling is separate.


**Created:** October 15, 2026
**Purpose:** Record roadmap step 7 under the September 26 owner-approved W8 B contract.
**Status:** Implemented and measured locally. The corrected frozen corpus has 348 voluntary
CONTACTs with no structural falsifier; same-keeper hand interruption rises 24 → 155 and remains
an explicit confound. All current cases have passing results across the sweep and focused retest;
the PR functional gate passed at `353b2138` ([run 38841462964](https://github.com/antonzymin-eng/Soccer-Manager-Pro/actions/runs/38841462964)); review-cleanup final-head CI and governing Unity validation remain pending.

Measurement-closeout source is `86a9496e` (src `74b215f71809d50f483e4ab2cb254692e3c53c75`). Its executable
production code at that closeout is unchanged from measured `e54335b0`: those later production
edits are comments only, and the remaining changes correct the test observer and add mirrored
negative controls. October 16 review cleanup is recorded separately below.

## October 16 review disposition

The [separate Law 12 re-handling issue](open-issues.md#gk-rehandling-law12) is Medium and open.
B explicitly defers enforcement; C's comparison baseline must record an owner scope/deferral
disposition before freezing. This does not expand C or change D-03 recovery eligibility. Corrected
B is **155/348 (44.5%)** same-keeper hand interruptions and **89** target controls; the review's
154/342 and 87 refer to the rejected first arm (45.0%). The observer does not adjudicate every
incidental intervening touch/location, so neither population is an exact Law 12 offence count.

Review cleanup removes a redundant same-result state branch, renames the stubbed timeout signal
method to `RecordHoldTimeoutSignal` (no counter/schema or release behavior changes), and documents
why dedicated CONTACT pressure can use the current holder. This is behavior-preserving source
cleanup after the measurement pins above; no new gameplay measurement is claimed. Canonical
`PassAttemptEvent` intentionally includes goalkeeper distributions in match/analytics pass attempts;
it supplies W5 without inventing completed passes.

The archive now has one copy of each analyzer, with an explicit arm-directory argument. All 72
coverage XML files contained zero valid/covered lines/branches and no packages; they are omitted,
with original paths/hashes/metadata in `empty-coverage-inventory.csv`. Raw TRX and failed-run evidence
remain. These empty files never established coverage. Fresh full CI and exact-final-head Unity
6000.4.9f1 validation remain merge gates. The prior `353b2138` functional CI job passed in run
38841462964; its Unity job was skipped. Review cleanup passes 99 goalkeeper cases (27 skipped)
and 164 W8 integration cases (one opt-in diagnostic skipped). All 25 derived arm files remain
byte-identical after analyzer consolidation; metadata and cross-document checks pass. The normal
commit hook exceeded its fixed 60-second budget on the socket-enabled retry; it is not counted
as passing. The local commit uses a per-command hook override after the focused checks; final-head
CI remains required.

## Contract and implementation

`MatchEngine.GoalkeeperDistribution.cs` produces all six #21 policies on the tactical heartbeat.
The selector uses the live eligible outfield roster, inclusive 25 m radius, deterministic distance /
width / roster-index tie-breaks and the shared own-goal/attack-direction fallback. LongKick is
receiverless by design. The approved delays, powers, delivery mapping and zero spin are unchanged.
New `[GT]` inputs use the existing config loader; no value was fitted to these matches.

The producer suppresses the holding keeper's Decision Tree dispatch, holds possession through #5
windup and gates every attempt against both inherited deadlines. Only accepted requests enter
`Distributing`. CONTACT revalidates the live receiver, launches through the canonical #5 adapter,
uses a typed normal-completion possession cause, arms the existing receiver latch when applicable,
and publishes `PassAttemptEvent` for W5. Resolve publishes `DistributionExecutedEvent` and settles
#11 exactly once. Loss/restart cancels; retained cancellation preserves the original hand clocks.
The old #11 local release/event approximation is removed. Snapshot body schema v26 adds the
accepted-request wait latch; v24 request/terminal storage is reused. Match save format remains v3.

Exclusive hand control also interrupts an ordinary foot PASS or SHOOT already in WINDUP or pending
CONTACT when a genuine hand claim occurs. The host performs that handover before Resolve executor
updates. #5 appends `CancelReason.HandControlTakenOver = 3`; #6 returns Cancelled/Idle without
publishing the tackle-only `ShotCancelledEvent`. Dedicated distribution windups are preserved.
This implements the approved decision's explicit requirement to interrupt in-flight foot actions.

| Policy | Delivery | Delay (10 Hz ticks) | Power intent | Target |
|---|---|---:|---:|---|
| SlowDown | Roll | 35 | 0.50 | nearest eligible local outfielder, else zone |
| Quick | Throw | 5 | 0.75 | nearest eligible local outfielder, else zone |
| ShortKick | Kick | 10 | 0.55 | nearest eligible local outfielder, else zone |
| LongKick | Kick | 10 | 0.90 | fixed zone |
| RollOut | Roll | 10 | 0.55 | nearest eligible local outfielder, else zone |
| ThrowOut | Throw | 10 | 0.70 | widest absolute lateral position, then nearest, else zone |

The retained six-second ground-drop backstop is unchanged. Current-law hand timeout, the separate
feet-only guard and outside-area handling remain W8 C work. This branch does not claim C completion.
Goal-kick restart tactics remain the separately recorded follow-up outside W8 hand distribution.

## Frozen measurement protocol

The comparison anchor is **October 13, `3974b78b0383adca3e9546bb47a7b93a62b3cf92`**, with evidence in
`evidence/pre-b-anchor-3974b78-20261013/`. The pre-wire bridge is `b60cc532` (includes #505). All arms
use `new MatchEngine(seed)`, `EnableGkHeading()`, no squad override and all six preregistered seeds
for 324,000 ticks each, on Linux .NET Debug. The common W8 instrument is the exact frozen blob
`520ba31c71e06ed408fd5c282b5fd5611cf74879`, including the same/opponent-keeper reclaim split.
The unchanged source-complete foul diagnostic is run separately on B.

Before interpreting B, reproduce the archived anchor on this host and verify the bridge's observed
outcome counters against it. Runtime versions are recorded rather than assumed interchangeable.
Terminal digests are fingerprints across schema v24/v25/v26, not cross-schema equality criteria.

The supplemental B diagnostic is frozen before its result-bearing run: it records every commit,
CONTACT, cancellation/rejection, executed event, canonical release, W5 receiver/zone count, F-05,
hand duration, guard margin, discipline and per-tick stalls. It applies the existing preregistered
structural falsifiers without changing their thresholds. The supplemental pass tracker continues past first ground contact and records each actual receiver /
zone outcome, including the controlling player; a landing is not a completed pass.
The common instrument retains per-keeper
exposure, end reasons, pass outcomes and the self-reclaim confound; all six frozen matches are kept.
Only SlowDown is exercised by this corpus. Separate composed LongKick fixtures at both ends supply
landing evidence and are explicitly excluded from frozen-corpus totals.
The raw `guardMargin` field is `360 - hand elapsed frames`, not the actual engine-counter margin
after any earlier feet possession. Runtime initiation uses the actual `_gkHoldTicks` counter;
the strict composed budget tests cover that distinction.

For landing evidence, observe the first settled tick after CONTACT with `Bouncing` state or ball
height at/below the physical radius. Report its XY position, requested and actual radial distance,
and signed radial shortfall (negative means overshoot). End earlier with an explicit reason if a
kick, hand claim, restart or controlled acquisition intervenes; otherwise censor at the run end.
A settled controlled acquisition is censored before testing ground height, so foot attachment is
never invented as a physical landing. Simultaneous ground/acquisition may therefore be censored;
this instrument does not claim sub-frame ground-contact resolution.
A receiverless launch is a valid zone execution, with no receiver-completion denominator or ghost
receiver latch. These observations describe the existing trajectory; they do not tune it or certify
realistic punt length.

## Validation record

| Acceptance surface | Evidence / status |
|---|---|
| All six policy mappings, both ends, receiver or zone | PASS: 24 composed cases; intended pre-wire red proof retained |
| Live target / removed target / F-05 / W5 / canonical CONTACT | PASS: composed execution and restored continuation locks |
| Exclusive hand control, including an older foot windup | PASS: eight genuine smother PASS/SHOOT WINDUP/CONTACT cases |
| Retained retry, both inherited deadlines, loss/restart teardown | PASS: composed clock / budget / cancellation locks |
| Canonical schema and deterministic continuation | PASS: v26 codec / replay / possession-writer selection |
| October 13 anchor and substitutions bridge agreement | PASS: exact report agreement; six anchor fingerprints reproduced |
| Corrected frozen B census and independent foul reconciliation | PASS: 348 CONTACT / event / release / W5 identities; all six independent fingerprints and discipline counts agree |
| Per-keeper A→B pass-outcome reconciliation | PASS: six digests and all 12 per-seed hand/feet outcome groups agree in each arm; 48 keeper/kind rows retained |
| Full local PR policy | Sweep: 4,032 pass / one diagnosed observer failure; focused retest resolves it. Reconciled current cases: 4,035 pass / 225 not executed. Fresh clean policy invocation remains pending |
| Governing final-head Unity 6000.4.9f1 validation | BLOCKED: no connected Editor / pinned host in this environment |

- 24 composed all-policy / both-end / receiver-or-zone locks fail on `b60cc532` with the same intended
  reason: the approved request never reaches #5 (execution mode remains ordinary). The scratch copy
  removes only references to the newly added latch and analytical maximum-delay constant so it builds
  against pre-B APIs; the selected policy test body is unchanged.
- 144 targeted Linux tests pass, including policy mapping, zero RNG stream/cursor movement, removed
  receivers, real W5 feed, canonical mid-windup restore, exactly-once F-05, retained cancellation,
  repeated rejection, deadline cancellation, snapshot schema and possession-writer inventory.
- After the handover correction, **162 targeted checks pass** at `e54335b0`, including all eight
  genuine smother-claim cases (both ends, PASS/SHOOT, WINDUP/pending CONTACT), canonical continuation,
  and the four observer-neutrality cases. The original four WINDUP witnesses fail before the fix
  because the old executor remains active while hand control is live.
- All three corrected diagnostic runs pass their catalog verifier (passing test plus captured report).
  The two attribution-only runs also complete successfully and reconcile every expected counter.
- The complete 37-assembly sweep records **4,032 passed, one failed and 225 NotExecuted** in TRX
  (including explicit / deferred / environment-gated cases). Its sole failure is the observation
  issue below. The integrated focused rerun passes **165 cases**, including the original full
  two-seed scenario and the two new mirrored cases. An additional **164-case final-source build/run**
  passes after comment-only cleanup. Reconciliation by assembly/class/case resolves the one failure
  and adds the two new cases: **4,035 passed / 225 NotExecuted**, with no unresolved failure.
  This is not represented as a clean single-invocation PR gate; fresh full CI remains required.
- **346 tooling tests pass**. Metadata, binary, assembly-tier and documentation-consistency checks
  pass. Governing Unity 6000.4.9f1 compile / required host validation remains not run.

The broader gate exposes a separate observation assumption in `sim_match_engine_keeper_claim`.
At configured-squad seed `5EED000000000003`, frame 30457, a legitimate free-kick restart ends the
keeper's hand episode: #11 is `Recovering` and the ball is `Stationary`. The same keeper (11) is
designated taker, so the old ID-only window incorrectly calls that a held claim and reports one
detachment. A fail-fast lifecycle trace preserves the exact witness.

The test-only correction closes claim observations on an explicit restart, even when identity is
retained; a claim ending before a same-tick restart cannot open a fresh window. It does not filter
out bad attachment states. Both mirrored real-claim/restart cases fail the old window rule and pass
the correction; their negative controls retain detection of a deliberately detached live claim.
The original full two-seed scenario also passes (three isolated cases together, then the integrated 165-case rerun). The attachment predicate,
0.001 m tolerance, 300-frame window, arrest/non-vacuity thresholds and pre-closure own-goal
attribution remain unchanged. Production code and all frozen measurements are unchanged; those
measurement observers already close episodes on explicit restart. Evidence is under
`evidence/w8-b-20261015/claim-probe/`.

## Results

The current host reproduces the October 13 anchor's entire common report **and all six terminal
digests exactly**, despite the recorded runtime patch difference (archived 8.0.31, current 8.0.32).
The `b60cc532` substitutions bridge reproduces every report row except the six expected schema-v25
fingerprints. This establishes the comparison anchor before interpreting B.

### First B arm: rejected implementation, retained evidence

Common/foul source `ed0570cf03a383259bb108304750fc95f35ac10a` (src
`fc7b539bbadf540ab25cf4007cce4a7a5263a6bf`); supplemental census
`db7c866e72bbe98fa02f7a741027bed573e83867` (src
`c4a6834c4247b31171ec27526ed2619791845c3a`). Production code is identical between those pins.

| Common observation, six frozen matches | October 13 A / bridge | First B arm |
|---|---:|---:|
| Hand claims / hand-origin pass CONTACTs | 135 / 135 | 360 / 360 |
| Target controlled | 22 | 89 |
| Other player controlled | 74 | 26 |
| Interrupted by later hand claim (same / opponent keeper) | 24 (24 / 0) | 156 (156 / 0) |
| Interrupted by kick | 15 | 89 |
| Fouls / yellows / straight reds | 49 / 7 / 2 | 50 / 6 / 1 |

The dedicated B census accounts for 342 commits, 342 canonical CONTACTs, 342 executed events and
342 real W5 receiver feeds, with no rejects/cancels/pending requests, drops or newly dispatched DT
races. Its independent six terminal digests and source-complete foul totals match the common run.
However, **18 additional hand episodes end through ordinary passes committed before the claim**,
after only 7–14 frames. Suppressing new DT dispatch alone did not interrupt those old windups.
That violates exclusive hand-control ownership, so this is not the accepted B arm. The handover
correction was made before a new source-pinned comparison; the original arm remains in
`evidence/w8-b-20261015/first-arm/`.

The 342 actual distributions have their own outcome identities: 87 receiver-controlled,
12 other-player-controlled, 154 same-keeper hand reclaims and 89 interrupted by kick. These are a
different denominator from all 360 hand-origin passes in the common report. No pooled keeper
decision count is labelled a hand-control action: the common decision counter also covers feet
episodes. Hand rows show no newly dispatched DT decision in this arm.

### Corrected B arm

Source is frozen at **`e54335b051ed51a0c3c394ccdaa016cb8ae28245`**, src tree
**`4a9061afbf7dd43cef5451294a2857ae902d739b`**. All policy values, receiver radius, delivery
geometry, claim eligibility and recovery rules are unchanged. The only gameplay change after the
first arm is the required interruption of a pending ordinary foot action during hand control.
All six-seed common, detailed and independent foul measurements complete successfully. All six
terminal fingerprints agree between the independently observed B runs, including the separate
test-only attribution overlay. The unchanged source-complete foul report agrees per seed.

| Common observation, six frozen matches | October 13 A / bridge | Corrected B |
|---|---:|---:|
| Hand claims / hand-origin pass CONTACTs | 135 / 135 | **348 / 348** |
| Dedicated GK CONTACTs / executed events / real W5 feeds | 0 / 0 / 0 | **348 / 348 / 348** |
| Target controlled | 22 (16.3% of 135) | **89 (25.6% of 348)** |
| Other player controlled | 74 | **14** |
| Interrupted by later hand claim (same / opponent keeper) | 24 (24 / 0) | **155 (155 / 0)** |
| Interrupted by kick | 15 | **90** |
| Hand duration frames: min / median / max | 6 / 25 / 246 | **229 / 234 / 234** |
| Hand duration frames: mean | 37.0444 | **233.2989** |
| Old engine ground drops | 0 | **0** |
| Fouls / yellows / straight reds / second-yellow dismissals | 49 / 7 / 2 / 0 | **46 / 6 / 1 / 0** |

The 348 accepted requests each complete once: no rejected, cancelled, pending or F-05 requests
occur in this fixed corpus, and no receiverless requests occur under its default SlowDown policy.
Forced edge cases are established by the composed fixtures. Every dedicated CONTACT joins the
matching common hand-episode end by seed, keeper and frame. **Ordinary hand-origin releases outside
that path fall from 18 in the rejected first arm to zero.** All 348 hand rows have no newly
dispatched DT action. Every CONTACT precedes both inherited backstops; the observed minimum margin
to the corrected #11 boundary is 126 frames. Maximum OutOfPlay run is zero; maximum observed
stationary run is 469 frames. No declared structural falsifier fires in corrected B.

These are descriptive six-seed effects, not a balance or realism certification. In particular,
same-keeper interruption rises from 24/135 (17.8%) to 155/348 (44.5%). The separate population of
hand claims immediately preceded by that keeper's own pass rises **53 → 203**; it includes feet
passes and must not be substituted for the hand-distribution interruption denominator.

The frozen report pools pass outcomes by seed. To supply the required per-keeper hand **and feet**
outcome comparison, a supplementary observer was added after the first arm. It follows the same
event-resolution rules without changing the frozen instrument. Both A and corrected B pass a
600-tick observer-neutrality lock; result use additionally requires all six terminal digests and
every per-seed hand/feet outcome counter to agree with their common runs. Those checks pass in
both arms: 208 A keeper-pass CONTACTs (135 hand, 73 feet) and 478 B (348 hand, 130 feet). The
[48-row keeper/kind comparison](evidence/w8-b-20261015/keeper-outcome-comparison.csv) preserves
exposure, duration, every end reason, decision labels, receiver/zone counts and outcomes; individual
pass identities remain in `keeper-pass-outcomes.csv`. This adds attribution, not a new acceptance
threshold or a calibration arm.

### Scope and unresolved validation

The frozen corpus exercises **SlowDown only**. It cannot estimate the match-level effects of the
other five policies. Their evidence is the composed policy matrix. Mirrored composed LongKick
fixtures separately show requested distance about 27.805 m from live CONTACT position, first-ground
distance about 8.085 m and radial shortfall about 19.720 m. Target geometry does not establish punt
realism; the existing #5 trajectory follow-up stays open.

Same-keeper reacquisition remains a separately reported A→B confound. This branch neither changes
the owner-frozen `Recovering` eligibility / baseline exit nor introduces a new own-release ban or
cooldown. No claim about its physical cause or about football-level balance follows from six seeds.

The temporary approval-review usage error interrupted one corrected measurement attempt and the
earlier full gate; neither is counted as complete. Runs were restarted after the user continued.
The normal pre-commit composition exceeded its fixed 60-second budget; local commits used a
per-command hook bypass while the separate full policy gate ran. This does not claim a passing hook.
A fresh clean full-policy invocation has not been rerun after the test-only observer correction;
all other sweep cases and the focused correction are preserved with their actual outcomes.
Governing Unity 6000.4.9f1 compile / required host validation remains **not run**: this environment
has no connected Unity Editor. The branch is not marked merge-ready or step 7 landed.

## Version history

| Version | Date | Change |
|---|---|---|
| 1.2 | 2026-10-16 | — | PR #511 review fixes: own-release avoidance/law and simultaneous-foul disposal; supplementary tests and isolated corrected-B comparison. |
| 1.0 | October 15, 2026 | Implementation and measurement protocol recorded before final result-bearing B runs. |
| 1.1 | October 15, 2026 | Retain failed first arm, correct the pending-foot-action handover, record the 162-case proof and reconcile the corrected six-seed / per-keeper measurements; governing host validation pending. |
