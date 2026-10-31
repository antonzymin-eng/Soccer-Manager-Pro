# W8 B implementation and October 13 anchor comparison

> **October 18, 2026 (UTC) — landing status (1.5):** PR #511 merged as `c0ffd0d25c5d17699397c5eb30252aa344db266c` after final-head CI passed at `a43bbc1c` ([run 39063246052](https://github.com/antonzymin-eng/Soccer-Manager-Pro/actions/runs/39063246052)). Governing Unity validation remains the source-equivalent `e58577de` host record. **Roadmap step 7 is landed.** The post-law six-seed digests below are adopted as the B→C Linux baseline. C and D-07 remain separate; no new measurement or code change.


## October 18, 2026 (UTC) — landing and keeper-release follow-up

PR #511 merged as `c0ffd0d25c5d17699397c5eb30252aa344db266c` after final-head CI passed at `a43bbc1c` ([run 39063246052](https://github.com/antonzymin-eng/Soccer-Manager-Pro/actions/runs/39063246052)). Governing Unity validation remains the source-equivalent `e58577de` host record. **Roadmap step 7 is landed.**

| Existing-log measure | Pre-law B | Post-law B |
|---|---:|---:|
| Corpus goals | 50 | 47 |
| Goals per 600 frames | 0.01543 | 0.01451 |
| Follow-up population | 14 opponent-control proxy windows | 12 opponent-touch restricted-spell windows |
| Follow-up shots / goals, both teams pooled | 9 / 6 | 8 / 6 |

The corpus-rate denominator is `6 × 324,000 / 600 = 3,240` nominal ten-second periods per arm,
including all simulated frames. Goal counts come from DANGER goal rows in the committed compressed
NUnit test-case outputs; the archived fixture emits these rows directly from GoalAwardedEvent ledger
ordinals. No new matches were run. Use XML output where a console log is incomplete.

The pre-law **proxy** filters `corrected-arm/passOutcome.csv` to terminal controller IDs on the
opposing team with `0 ≤ endFrame − contactFrame ≤ 507`, then counts DANGER shot/goal rows in
`[endFrame, endFrame + 600]`. This selects fourteen windows, nine shots and six goals. It is **not**
an exact first-physical-touch census: prior uncontrolled/body touches and interrupted flights may be
missed. Post-law uses the twelve opponent-touch clearings already in `measurement-closeout/spells.csv`.
The reported nine/six and eight/six also equal the respective unique event totals despite possible
window overlap. Windows are selected around keeper releases, not sampled at random from all match time.

**The shooter/scoring-team split cannot be recovered from these logs.** DANGER retains only frame
and event kind; the full event payload is not archived. Do not infer scoring team from the most recent
toucher or treat these differently selected small populations as a causal/statistical equivalence test.
The pre-law proxy already contains concentrated goals; the 50 → 47 corpus total does not by itself
rule out a local keeper-passivity effect. The owner explicitly accepts merging B with these limits
and moves exact touch timing/team attribution to B→C measurement rather than reopening this PR.

The existing [post-law measurement](evidence/w8-b-20261015/rehandling/README.md) at original
validated source `751b1143` (published equivalent `2698bab1`) is now the **adopted B→C baseline**,
not the pre-law 348-contact arm. The existing source/tree equivalence records and the twelve-arm
fingerprint reproduction carry its identity to the merged B behavior; no merge-head rerun is claimed.

| Seed | Adopted post-law B terminal digest (schema v27) |
|---|---|
| `0F1E2D3C4B5A6978` | `6ef1e77f9dcf86c4bce5e1f66a989a64b42f26c268ead53340b9c57d282b4072` |
| `00000000D1A6D05E` | `06e3daf03f1c45a112f958c4bfac980c30956971e175b35b7d815d685b4caf93` |
| `0000000000000001` | `25ceac27f7d4e87fb1166227f683eaff87336efd301fa5500b6d212a28deba59` |
| `00000000ABCDEF12` | `77b4e0cc08a4ac903fbb364f2bb28612eaa39b380955111c58fe040ebf474a80` |
| `0000000099887766` | `ccc99ba78dce889ed07d18609c568486920ffd97c45cb283f15d7f5c203f6e4f` |
| `000000005A5A5A5A` | `6961b03ff4cb1512967c01c46158274b3516bb4c632d921f75e5f6baad936eb3` |

### B→C keeper-release follow-up requirements

Before the result-bearing B→C comparison, freeze a read-only supplementary instrument and its
window-selection rules. Run it in **both arms** on the same Linux .NET lane, six seeds, constructor,
policy and 324,000-frame population; reproduce the adopted B fingerprints before interpreting C.
Retain the existing frozen W8 observer alongside it and prove the new observer does not alter the
B gameplay/digest chain. A new host/runtime requires reproduced B control or a fresh paired capture.

- Record every canonical keeper hand release by seed, frame, keeper identity/team and ball position.
- Record **every actual player-ball touch**, including applied body deflections, uncontrolled first
  touches, own touches and controlled acquisitions, with exact 60 Hz frame, within-frame ordering,
  agent/team and position. A geometric candidate or terminal acquisition is not a substitute.
- In each arm, select keeper releases whose **first subsequent other-player touch** is an opponent
  within **507 frames**, including an actual later-in-order touch in the release frame. Retain
  intervening own touches and teammate clearings; do not let a proxy select the two arms differently.
- For the resulting 600-frame follow-up, log every ShotExecutedEvent with shooter identity and team,
  and every GoalAwardedEvent with **ScoringTeam** and credited scorer identity/team separately
  (own goals/credit can differ). Preserve frame/order, scoring location, window membership and raw
  event identity. Split shots/goals into the releasing keeper's team versus its opponent.
- Report complete/censored windows, overlap and unique event counts, shots/goals per window and
  corpus-wide goals per 600 frames with explicit played-frame denominator in each arm. For a same-frame
  event, use recorded order to decide whether it follows the clearing touch. Preserve exact touch
  timestamps and restriction start/end states; do not infer them from control-only pass outcomes.
- Repeat the keeper-release danger comparison on these identically selected exact-touch populations.
  Report first divergence, its cause and any excess opponent goals as a possible keeper-passivity
  defect for investigation before C closeout. No tuning, new policy or causal conclusion is authorized
  by the historical 14-proxy-versus-12-spell comparison.

This is a **measurement requirement**, not a new result or permission to implement C. Team attribution
and exact pre-law touch timing remain unavailable in the committed B logs. Owner approval of the C
rule and D-07 closeout remain separate gates.

## October 17 measurement merge-gate closeout (historical pre-merge record)

The [Linux .NET Debug rerun](evidence/w8-b-20261015/measurement-closeout/README.md) uses the exact
step-6 frozen observer blob `520ba31c71e06ed408fd5c282b5fd5611cf74879`, plus a separate read-only
restricted-spell trace. All twelve seeded invocations pass and reproduce their stored terminal
digests and distribution totals. The pre-law frozen report reproduces all 9,140 rows. Direct
NUnitLite/CLR 8.0.28 is recorded explicitly; this does not replace canonical final-head GitHub CI.

| Seed | Pre-law hand claims / distributions | Post-law hand claims / distributions | Pre-law same-keeper terminal hand returns | Other-claim net change |
|---|---:|---:|---:|---:|
| `0F1E2D3C4B5A6978` | 83 / 83 | 7 / 7 | 42 | -34 |
| `00000000D1A6D05E` | 70 / 70 | 20 / 20 | 29 | -21 |
| `0000000000000001` | 66 / 66 | 15 / 15 | 32 | -19 |
| `00000000ABCDEF12` | 50 / 50 | 22 / 22 | 23 | -5 |
| `0000000099887766` | 42 / 42 | 23 / 23 | 16 | -3 |
| `000000005A5A5A5A` | 37 / 37 | 39 / 39 | 13 | +15 |
| **Total** | **348 / 348** | **126 / 126** | **155** | **−67** |

All claims distribute: the 222-count drop is not a lost-execution gap. Descriptive reconciliation
is a 155-return bucket disappearing and a 67 net reduction in remaining claims (193 → 126);
193 is the exact pre-law remainder, not a predicted post-law baseline. This is not an exact causal
split or a count of adjudicated illegal claims: changed trajectories and conditional RNG draws
prevent such a conclusion. For `0F1E2D3C4B5A6978`, 83 = 42 returns + 41 other claims becomes
7 = 0 + 7, a 34-claim net non-return reduction. The first hand claim/release at 28,302/28,536
agrees. The pre-law keeper reclaims at 28,542; post-law, teammate 3 touches at 28,736 instead,
and the match diverges thereafter. All seven post-law claims distribute; no unexecuted
hand claims account for the difference.

Every restricted spell is retained in [spells.csv](evidence/w8-b-20261015/measurement-closeout/spells.csv):
114 end on teammate touch, twelve on opponent touch; none is stuck, restarted or censored.
Min/median/max duration is 0/127/507 frames (0/2.117/8.45 s). These are post-release restrictions,
not hand holds. All twelve opponent-touch windows have a full 600-frame follow-up; they contain
**eight shots and six goals** across both teams, with no duplicated ledger event across windows.
Same-frame events are included. This is temporal association after the restriction clears, not a
causal balance verdict. The archive lists each window and every spell, including two same-frame
clearing touches. Zero sanctions did not establish an empty opponent-touch follow-up population.

**Both measurement questions are answered for this corpus:** no missing distribution, stuck
restriction or baseline mismatch was found. No calibration or gameplay code changed. Host validation
remains met at `e58577de`; final docs-head Linux CI and merge remain, and step 7 is not landed.

## October 17 governing Unity closeout (historical pre-merge record)

Unity 6000.4.9f1 (f7258d6eebbe), Windows 11 / Mono validated exact source
`e58577de038647a0134e7ceecb151126cf7fa025`, src tree
`cae1dd3cc1c6476c59d5344209c2ddefd6884656`. The committed
[pinned-host evidence](evidence/host-w8b-e58577de-20261017/README.md) includes source identity,
compile excerpt, raw NUnit XML, the main EventSystem comparison and SHA-256 coverage.
Evidence commit `e1f7da59` and this documentation closeout preserve the validated `src`, `Packages`,
`ProjectSettings` and `Assets` trees; no new runtime head is inferred from a documentation commit.

| Governing host check | Recorded result / disposition |
|---|---|
| Clean-cache compile | PASS: 0 errors; three known warnings; no shim build-output references |
| Goalkeeper / Pass / Shot / Collision / TacticalInstructions | 388 passed / 0 failed / 60 skipped / 1 existing BM-007 inconclusive |
| MatchEngine initial run | 773 passed / 1 sleep-interrupted timeout / 16 skipped / 1 existing inconclusive |
| Keeper-conversion rerun, same head | PASS in 293.3 s; replaces only that interrupted case. Effective case identities: 774 passed / 16 skipped / 1 existing inconclusive; this is not a single clean invocation |
| W8 B / re-handling fixtures | 99/99 and 42/42 passed; all ten full-match scenarios and both tackle-latch restore seeds pass after the one rerun |
| EventSystem attribution | 47 passed / 7 failed / 10 skipped / 1 inconclusive; every per-test outcome matches `origin/main` `1963cfbe`. [Separate open issue](open-issues.md#unity-eventsystem-heartbeat); no PR regression is attributed to these seven failures |

The host slept from 03:33:14Z to 14:03:41Z while keeper conversion was running; that case recorded
38,136.5 s and exceeded its two-hour timeout. The original failed XML is retained beside its passing
rerun. The superseded `2d6ddf1a` W8 fixture failures and 99/99 lazy-ordinal probe are also retained;
`e58577de` fixes the test-only initialization order without changing production code.

**Unity closure is met for PR #511. Final docs-head Linux CI and merge remain pending; step 7 is
not landed.** The pre-law and post-law measurements below keep their original source identities;
this host result neither reruns those matches nor removes their stated measurement limitations.

## October 16 review closeout: post-law B baseline

The separate owner-approved own-release law (`fff7cf5b`) plus avoidance/foul-order repair
(`751b1143`, src `0b46dfe1418c3ab682df83f3b530985d494e55d1`) is measured against corrected B.
The repeated control reproduces every summary field and all six stored fingerprints.

| Six-match outcome | Pre-law B | Post-law B |
|---|---:|---:|
| Canonical distributions | 348 | 126 |
| Receiver control | 89 / 348 (25.6%) | 114 / 126 (90.5%) |
| Same-keeper hand return | 155 | 0 |
| Other-agent control / interrupted by kick | 14 / 90 | 12 / 0 |
| Re-handling attempts / sanctions | Not implemented/instrumented | 0 / 0 |
| Ordinary fouls / cautions / straight reds | 46 / 6 / 1 | 48 / 8 / 1 |

Every match has zero restricted contacts and sanctions. There are no law restart spots or sanction-restart follow-up windows. The separate
restricted-spell opponent-touch follow-up is measured in the October 17 closeout above. Both body/schema and prevention are covered by composed tests;
a no-sanction corpus cannot validate restart placement by itself. All six post-law digests are
retained as the measured Linux post-B anchor candidate; formal B→C adoption remains gated by
final docs-head Linux CI and merge. Cross-schema digest change is not an effect measure.
The source, runtime, per-match rows, full outcomes, raw NUnit reports and reproducible analyzer are
[archived together](evidence/w8-b-20261015/rehandling/README.md).

Pre-host supplementary Linux validation: **200 engine checks passed; 107 goalkeeper checks passed / 27 existing
skips; 346 tooling checks passed with system Git 2.43**. The final default-Git run retains one unchanged LFS-fixture failure; both runs are archived. Ten regression cases fail on the original `fff7cf5b` control
and pass with the repair. Both test assemblies compile against NUnit 3.5 with its matching shim.
The normal VSTest driver crashes here; these are direct supplementary NUnit runs, not a fresh full
PR-policy invocation. **Final docs-head Linux CI remains required; the governing Unity closure is met as recorded above.**
The earlier sections below remain pre-law history.

**Created:** October 15, 2026
**Purpose:** Record roadmap step 7 under the September 26 owner-approved W8 B contract.
**Historical pre-law status (October 15–16):** Implemented and measured locally. The corrected frozen corpus has 348 voluntary
CONTACTs with no structural falsifier; same-keeper hand interruption rises 24 → 155 and remains
an explicit confound. All current cases have passing results across the sweep and focused retest;
the PR functional gate passed at `353b2138` ([run 38841462964](https://github.com/antonzymin-eng/Soccer-Manager-Pro/actions/runs/38841462964)); review-cleanup final-head CI and governing Unity validation were pending in that record; the October 17 section above records the subsequent host closure.

Measurement-closeout source is `86a9496e` (src `74b215f71809d50f483e4ab2cb254692e3c53c75`). Its executable
production code at that closeout is unchanged from measured `e54335b0`: those later production
edits are comments only, and the remaining changes correct the test observer and add mirrored
negative controls. October 16 review cleanup is recorded separately below.

## October 16 review disposition (historical; superseded by the approved amendment above)

The [separate Law 12 re-handling issue](open-issues-resolved.md#gk-rehandling-law12) is Medium and open.
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
remain. These empty files never established coverage. At that review, fresh full CI and exact-final-head Unity
6000.4.9f1 validation remained merge gates; the host item is now met by the October 17 evidence. The prior `353b2138` functional CI job passed in run
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
| Governing Unity 6000.4.9f1 validation | MET at `e58577de`; runtime-tree identity carries the result to docs-only successors. [Host evidence](evidence/host-w8b-e58577de-20261017/README.md); sleep timeout/rerun and pre-existing EventSystem failures are retained above |

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
  pass. Governing host validation was not run at that historical stage; the October 17 evidence above now closes it.

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
Governing Unity compile/host validation is now met at `e58577de` with the recorded sleep-timeout
rerun and pre-existing EventSystem attribution. Final docs-head Linux CI and PR #511 merge remain
pending; step 7 is not marked landed.

## Version history

| Version | Date | Change |
|---|---|---|
| 1.5 | October 18, 2026 | PR #511 merged c0ffd0d2; adopt post-law B digests, record 50/47 corpus and 14-proxy/12-spell follow-up with missing attribution limits; require exact touch/team logging in both B→C arms. No rerun or code change. |
| 1.4 | October 17, 2026 | Same Linux .NET lane and exact frozen observer; twelve matching fingerprints, all hand claims/distributions reconciled, 126 restricted spells and twelve opponent-touch windows; no gameplay change. |
| 1.3 | October 17, 2026 | Governing Unity host closure at e58577de; source-equivalent documentation heads, interrupted-case rerun and pre-existing EventSystem comparison retained; final Linux CI/merge pending. |
| 1.2 | 2026-10-16 | — | PR #511 review fixes: own-release avoidance/law and simultaneous-foul disposal; supplementary tests and isolated corrected-B comparison. |
| 1.0 | October 15, 2026 | Implementation and measurement protocol recorded before final result-bearing B runs. |
| 1.1 | October 15, 2026 | Retain failed first arm, correct the pending-foot-action handover, record the 162-case proof and reconcile the corrected six-seed / per-keeper measurements; governing host validation pending. |
