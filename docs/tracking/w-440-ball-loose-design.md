# #440 BallLoose — Stage A investigation and proposed choices

Created November 9, 2026. Purpose: freeze episode measurement, collect within-frame
ownership chronology and present a concrete gameplay choice. Version 1.4.
Status: Stage A measured; option C proposed as a bounded Stage B experiment. Protocol/limits/policy/parameter await owner approval; no gameplay code authorized.
Branch `codex/440-ball-loose`; source baseline `d4b026aa951432d342217314b8c4d49b53a19e7d`.
#434 remains a separate draft; Stage A therefore measures main, not its unmerged head.

## Observation versus contract

#14 §3.6.5.2 requires BallLoose to release control and remain distinct from Missed
and BallWon. It specifies neither velocity nor a blanket same-frame regain ban.
The reported 14/14 original-carrier re-picks are historical evidence, not this run's
result. Source inspection finds BallLoose releasing without movement in AI, followed
by ordinary Resolve pickup. The nearest player can still be the original carrier.
The six-seed chronology below confirms that mechanism on all 37 observed episodes.

Hypotheses: (H1) release leaves the resting ball at its old carrier, then passive
pickup restores that carrier; (H2) moving-ball first touch, rather than passive
pickup, restores it; (H3) end-of-tick sampling obscures an intervening teammate or
opponent acquisition. Record source-order ownership transitions, release and
frame-end kinematics to distinguish them. Do not implement a gameplay fix in Stage A.

## Frozen measurement definitions

- Time is the **60 Hz physics frame** returned by RunTick, not a 10 Hz AI heartbeat.
  Tackle-frame = the frame in which the resolver emits its outcome.
- Episode key: `(frame, tackler ID, carrier ID, source-order sequence)`, created for
  **every** BallLoose outcome before its release. Keep all episodes, even censored.
- Fixed horizon: the end of physics frame `tackleFrame + 60` (one second). Include
  same-frame events after the tackle callback and all events through that endpoint.
- A later **tackle outcome of any kind** supersedes the active episode immediately,
  before its effects. Censor the old one as `new-tackle`; start a new episode only
  if the new outcome is BallLoose. Thus no ownership acquisition belongs to two
  active windows. Multiple same-frame outcomes follow callback order, not sorting IDs.
- Censor at the first observed restart, OutOfPlay at Resolve entry, adjudicated foul,
  half-time, full-time or capture end. Source order resolves ties. A censor event
  precedes its effects; never attribute a restart award as an ordinary recovery.
  The out-of-play hook is at Resolve entry before first-touch/pickup. If the ball
  leaves play later during Resolve, the restart hook censors before its award.
- Mutually exclusive horizon outcomes: `original-carrier`, `carrier-teammate`,
  `opponent`, `still-loose`, `censored`. Classify **owner at the fixed endpoint**,
  not first acquisition. A loose endpoint remains `still-loose` even if an earlier
  recovery occurred. Censored endpoints retain their cause and observed prefix.
- Retain **every holder-ID transition**, including release to −1, in callback order
  throughout the active prefix. Record ball position and velocity at tackle,
  release/acquisition and tackle-frame end. Do not infer a hidden transition from
  end-of-frame ownership alone. Constructor, initial kickoff, restore and explicit
  forced-state fixture writes are excluded boot/test paths; runtime writes use the
  existing SetPossessingAgent seam. Retain the writer inventory as evidence.
- Report separately: episodes with any original-carrier acquisition **within the
  tackle frame before censoring**; tackle-frame end-owner split; horizon outcome
  split. Frame-end split includes every created episode, annotated if censored in
  that frame. Same-frame regain counts are deduplicated by episode, not transitions.
- A **dispossession** requires an observed acquisition by the opponent of the
  episode's carrier team. Count its first occurrence once per episode in the
  observed prefix, including a prefix later censored. Teammate recovery is a
  separate first-occurrence-per-episode metric, never a dispossession. These counts
  can coexist with later horizon outcomes; do not force them into exclusive bins.
- Global ownership changes count every changed holder ID, including −1 release and
  acquisition edges, once, independent of episodes. This is not a team-turnover rate.
- Shots = ShotExecutedEvent by TeamId; goals = GoalAwardedEvent by ScoringTeam;
  fouls = FoulCommittedEvent by offender team. Unknown attribution stops the run.
- Report per seed and team, with totals, scheduled frames and pre-full-time live
  frames as separate denominators. Each full match has 324000 RunTick calls; use the
  six frozen Pre-C seeds, SDK 8.0.426 / CLR 8.0.32, Debug/DEVELOPMENT_BUILD, default
  squads/attributes, unbound configuration, Human managers, shipping SlowDown.
  One seed per process, two capture processes. No numeric plausibility bands.

## Observer and validation contract

Development/editor/test-only callbacks copy diagnostics at the actual tackle,
ownership and censor boundaries. They are absent from release builds. A test-side
consumer attaches only when `TD_W440_OBSERVE=1`; ordinary tests never Assert.Ignore
for this flag. No gameplay branch, tuning, RNG draw, serialized field, event ledger
entry or domain tag is added. The observer cannot mutate the engine.

Before capture: commit this note, observer and runner; freeze source/runtime/binary
and observer hashes. Prove observer-enabled versus observer-disabled equality over
all six full matches: every raw digest, complete BallState/trajectory and ordered
canonical ledger byte. Independently validate every observed raw chain and preserve
BuildHash checks. Retain the full streams, chronology, episode rows, command/exits,
SHA256SUMS and source inventory. Also compare the disabled lane to #434's independently
measured main C0 raw stream where available; label this a separate cross-check.
Unit fixtures must cover same-frame acquire/release/re-acquire chronology, endpoint
versus first recovery, all censor causes, horizon inclusion, same-frame overlap,
teammate versus opponent attribution, and complete episode-count reconciliation.
The six-seed census must reconcile episodes exactly to the engine's BallLoose counter.

## Stage A findings — November 10, 2026

[Retained evidence](evidence/ball-loose-stage-a-20261110/README.md) measures the main
baseline with observer freeze `15b110d1`, not unmerged #434. Twelve receipted full
matches pass all 60 enabled/disabled and independent-main stream comparisons.
An independent journal replay checks every trajectory owner and all episode fields.

| Metric | Six-match total |
| --- | ---: |
| BallLoose episodes / original-carrier tackle-frame regains | 37 / 37 |
| Tackle-frame end owner | Original carrier 37; all other bins 0 |
| One-second endpoint | Loose 30; original carrier 5; opponent 2; teammate/censored 0 |
| Opponent recoveries in observed prefixes | 12 |
| Teammate recoveries in observed prefixes | 0 |
| Global holder-ID changes, including loose-ball edges | 16021 |
| Shots / goals / fouls | 146 / 47 / 48 |

All 37 reacquisitions have the passive-pickup marker. The release transition retains
the tackle position with exactly zero velocity in every case, and no other player
acquires within those tackle frames. H1 is supported on this corpus; H2/H3 do not
explain any of the observed immediate regains. The 12 later opponent recoveries
must not be mistaken for the two opponent-owned endpoints. No episode is censored
in this corpus; unit fixtures still cover every frozen censor and overlap rule.

The workspace filled while scheduling after seven exit-0 receipts. That original
attempt and one unreceipted output are preserved; only receipted successes are reused.
Five recovery jobs, including the unreceipted mode rerun, use identical frozen pins
and all exit 0. There are 13 processes started and 12 accepted full matches. Definitions
and the instrument never changed for recovery. All per-frame traces are retained
losslessly in LFS, with exact decoded hashes and portable reanalysis commands.

The full local gate passes: 4487 passed / 225 existing console skips, no failures,
quarantine empty; three existing inconclusives are retained separately in TRX. The
new observer fixture passes 20/20. Release build/reflection confirms stripping of
new diagnostics. Pinned Unity validation remains pending; the final draft PR names
the exact host checkout and unfiltered MatchEngine assembly request. No #14 ERR is
filed until an owner-approved Stage B implementation lands with its amendment.

## Candidate fixes — proposals, not code or approved constants

| Choice | Mechanism and realism | Proposed constants / example | RNG and snapshot | Expected metrics / risk |
| --- | --- | --- | --- | --- |
| A — velocity only | Give a deterministic knocked ball a direction and speed, leaving all normal eligibility intact. Physical flight/first touch decide recovery. | `BALL_LOOSE_KNOCK_SPEED_M_S = 2.5` **[GT] (proposed)**, m/s, proposed authoring range (0,6]. At 60 Hz, 2.5/60 = 0.04167 m before the next integration. Direction away from tackler through contact, with the carrier's facing as the coincident-position fallback. | No new RNG; existing ball kinematics already serialize. No new persistent state. | More moving-ball contacts; opponent recovery may rise, but first touch can still restore the original carrier **in the same frame**. Does not guarantee the proposed policy. No guarantee of which team wins. |
| B — bounded carrier exclusion | Exclude the dispossessed carrier from ordinary first-touch and passive-pickup admission for a short window. It gives other players a recovery opportunity, but the resting ball can make the nearest tackler the automatic winner. | `BALL_LOOSE_CARRIER_EXCLUSION_FRAMES = 6` **[GT] (proposed)**, physics frames, authoring range [1,60]. Loss at f=120 blocks frames 120–125; eligible at 126 (0.1 s). | No RNG. Carrier ID + expiry cross snapshots, so next free world-schema bump and restore validation would be required; allocate only on the eventual Stage B base. | Same-frame carrier regains become zero; opponent/teammate recovery rises. Substantial risk of converting BallLoose into BallWon, and a stationary ball may wait for immunity to expire. |
| C — velocity plus one-frame carrier fence (proposed experiment) | Combine A's physical knock with a one-frame fence on ordinary recovery admission, including applicable keeper paths. The original carrier competes again on the next frame. This separates loss of control from immediate recovery without a multi-frame immunity advantage. | A's proposed 2.5 m/s parameter plus `BALL_LOOSE_CARRIER_FENCE_FRAMES = 1` **[FIXED] (proposed policy)**. Loss at f=120 blocks ordinary reacquisition only at 120; eligible at 121. | No RNG. A frame-local fence can be excluded from snapshots **only if** it resets before all next-frame consumers and cannot affect an off-tick operation; prove that in Stage B. Otherwise serialize and bump the next free schema. | Same-frame carrier regains zero; later recovery remains possible. Movement is physical; the one-frame fence may still favor the nearby tackler or merely defer the old carrier to the next frame. No forced winner is coded, but a near-automatic BallWon effect is a measured Stage B risk. Shots/goals/fouls have no reliable directional prediction; measure without tuning. |

These are new proposed parameters for the selected mechanic, not authorization to
change any existing [GT] value or calibrate toward favorable football metrics.
Owner choice must approve a new parameter before Stage B adds it. The initial
2.5 m/s value is uncalibrated, not inferred from this census.

## Expected Stage B directions and limits

| Frozen metric | A: velocity only | B: six-frame exclusion | C: velocity plus one-frame fence |
| --- | --- | --- | --- |
| Tackle-frame original regains | May decrease; no zero guarantee | Zero ordinary regains by policy | Zero ordinary regains by policy |
| Tackle-frame end-owner split | More movement may change bins; no guaranteed winner | Original recovery prevented before censor; opponent/loose/teammate may increase | Same scoped prevention; nearby tackler may dominate |
| One-second end-state split | Direction uncertain after later play | Direction uncertain once immunity expires | Direction uncertain after carrier becomes eligible next frame |
| Opponent-prefix dispossessions | May increase; not guaranteed | Expected increase, with BallWon-equivalence risk | May increase, with the same risk to measure |
| Teammate-prefix recoveries | May increase from zero; no guarantee | Opportunity increases; no guaranteed teammate recovery | Opportunity increases; no guaranteed teammate recovery |
| Global holder-ID changes | No reliable match-level direction | No reliable match-level direction | No reliable match-level direction |
| Shots, goals and fouls per team/match | No reliable direction | No reliable direction | No reliable direction |

Restart awards/censored frames remain law-governed, so an unconditional end-of-frame
carrier count need not be zero even when the scoped ordinary-recovery policy holds.
The measured Stage A census supports fixing the immediate pickup seam. It cannot
establish that an uncalibrated 2.5 m/s knock and one-frame exclusion create a realistic
contest. Propose measuring C for the narrow policy and early restored eligibility, with
explicit Stage B reporting of near-automatic tackler wins and next-frame re-picks.
Do not tune or declare realism from a zero tackle-frame regain count alone.

## Policy decision required after Stage A

Proposed policy: the original carrier cannot regain through **ordinary recovery**
in the tackle's physics frame before any intervening whistle/restart. Restart awards
remain governed by the Laws and are censored, not rewritten to enforce a tackle
policy. This explicit boundary avoids accidentally changing restart allocation.
Choices B and C guarantee that scoped policy only if every eligible ordinary path
is fenced; A does not. No current spec is claimed to mandate it. If accepted,
Stage B writes the exact policy and chosen mechanism into #14 §3.6.5 with a newly
verified ERR ID and spec version history, in the same commit as the fix.

## Proposed Stage B experiment — owner approval required

This section is a proposal, not an approved football acceptance contract or a
result. No C/D gameplay code, new [GT] parameter, snapshot change, new match capture
or outcome-based tuning is authorized by this document. C is an experiment to test
whether a physical knock plus brief recovery restriction produces a useful loose
ball; it is not accepted as a shipping fix. Keep the original Stage A definitions
above unchanged. Existing [GT] values remain unchanged.

### Fixed experiment configuration and sample

- Select a **v29 base containing #543**, after merging or an explicitly approved
  stacked/rebased composition. Reconcile the shared source/history, LFS/CI and evidence
  registry changes; do not silently overwrite them. Current Stage A remains a v28
  main-based historical record, not the new before arm.
- C proposes the new uncalibrated knock speed **2.5 m/s [GT] (proposed, not approved)**,
  range (0,6], and a **one-physics-frame carrier fence [FIXED] (proposed policy)**.
  At f=120 the carrier is fenced at 120 and can compete again at 121. No new RNG.
  Velocity direction is away from tackler through carrier, with facing as the
  coincident-position fallback. Above 0.5 m/s is not automatic reception: actual
  first touch also requires reach and an approaching ball. Do not predict certain
  BallWon or Missed outcomes from speed alone.
- Same frozen six seeds, 324000 calls/seed, SDK 8.0.426 / CLR 8.0.32 and recorded
  debug/default configuration in both arms. Twelve observed primary matches
  (six before, six C) plus twelve disabled-observer controls = **24 full runs**.
  Off/on raw digests, trajectories/BallState, team witnesses, geometry and ledgers
  must match within each code arm. Gameplay identity between before and C is not
  expected. Pin source, binaries, runtime, observer and analysis before any run.
- Report every seed/team and all original metrics, including global holder changes,
  shots/goals/fouls, frame-end and one-second ownership, teammate/opponent prefix
  recovery, censor causes and loose-window duration. A zero regain count alone
  does not certify a physical contest or football realism.

### Two proposed numerical stop ceilings

Both caps below are **[EST] proposed experiment acceptance policies**, not empirical
football rates and not game constants. They are chosen before any C result. A
one-quarter cap requires immediate tackler wins and rapid carrier returns each to
remain a minority, rather than relabeling most BallLoose episodes as BallWon/Missed.
The owner may change these proposals **before** the experiment is frozen.

| Gate | Proposed maximum | Exact numerator/window |
| --- | ---: | --- |
| G1: immediate original-tackler recovery | **25%** | Episodes with any acquisition by the initiating tackler after BallLoose in physics frame f, before the first censor. Deduplicate by episode; count acquire-then-release even if the tackler does not hold at frame end. |
| G2: short-horizon original-carrier recovery | **25%** | Episodes in which the old carrier holds at the entry to, or acquires at any point during, frames **f+1 through f+24 inclusive**, before censor. The first eligible frame is the nominal fence-lift frame f+1, not the later frame when every geometry/speed gate happens to admit a touch. |

G2 now spans the **first 24 nominally eligible physics frames (0.4 s of ticks)**.
For f=120 it is 121–144 inclusive. The former f+1..f+3 gate is superseded;
keep that 50 ms slice descriptive. Counting ownership at window entry prevents the
before arm's same-frame regain from looking like zero return merely because no
new acquisition edge occurs after frame 120. Report entry-holding and new-window
acquisitions separately as well as their deduplicated union. Never shift the
window to a later favorable geometry/speed admission opportunity.

Window rationale: a stationary carrier and a ball starting at zero separation,
travelling at constant 2.5 m/s, cover a 1.0 m reach in 1.0/2.5 = 0.4 s = 24 frames.
The unbound 1.0 m defaults for FIRST_TOUCH_ACCEPTANCE_RADIUS_M and
LooseBallPickupRadiusM motivate this reference, not a proof of ordinary-recovery
eligibility or actual relative separation. Moving players,
starting offset/direction, approaching-ball gates and deceleration can shorten or
lengthen exposure; do not claim the ball stays in reach for at least 24 frames.
This fixed diagnostic window is a proposed policy, not an escape guarantee or a
new gameplay lockout. The carrier fence still lasts only frame f.

**Only the pooled six-seed candidate result gates.** Per-team and per-seed counts,
completion bounds and episode totals remain mandatory descriptive reports, with
no team-specific 25% gate or 10-per-team quota. Small clustered samples are not
population estimates; pooling does not establish a nominal statistical error rate.
The two 25% ceilings remain uncalibrated owner proposals, not empirical football
rates. Both are still awaiting approval.

### Reported time to original-carrier regain

Retain each episode's first ordinary original-carrier acquisition after the release,
with relative physics frame and callback/source order. Report disjoint first-event
bins **same frame f**, **f+1..f+3**, **f+4..f+12**, **f+13..f+24**, **f+25..f+60**,
**complete one-second window without regain**, and **censored before first regain**.
These bins exhaust all episodes, keeping a positive before a later censor in its
observed event bin. Do not create a new recovery when ownership merely persists
from frame f; entry-holding is a separate flag for G2. Tackle-frame acquisition is
reported in its own bin, not silently recounted as a first event in frame f+1.
A later regain/release does not erase the first event. No restart award is a regain.

At horizons f+3, f+24 and f+60, also report first-regain cumulative K/N and
[K/N,(K+U)/N] completion bounds using the same all-episode denominator/censor rules.
Do not estimate a median from recovered-only episodes or treat censored windows as
zero returns. If bounds do not locate a quantile, mark it unidentifiable. No new
numeric gate is attached to this distribution. This is a future Stage B report;
no additional result-bearing analysis or capture is run in this documentation pass.

### Denominators, overlap and censoring

For each code arm and carrier-team stratum, **N is every created BallLoose episode**,
including episodes censored by restart/out-of-play/foul/half/full time, later tackle
of any outcome, or capture end. Never remove difficult or censored episodes from N.
Use the existing source-sequence key and first-censor ordering; no restart award
counts as an ordinary recovery. A later tackle censors the old window before its
effects and starts a new one only for BallLoose, so no edge is double-attributed.

For each gate, keep disjoint counts:

- **K**: a positive acquisition/entry-holding was actually observed before censor;
- **U**: no positive observed, but censor truncated that gate's window;
- **Z**: the whole gate window was observed without a positive.

Require K+U+Z=N. Report observed-positive K/N and the censor-uncertainty interval
**[K/N, (K+U)/N]**. These are completion bounds, not statistical confidence bounds.
Do not assume censored episodes are successes/zeros, and do not infer events after
the censor. Censor after a completed window adds no U for that gate. A positive
before censor stays in K, not U. G2 censors at/before f+1 can therefore leave the
whole early-return window unknown.

Proposed adequacy conditions, also requiring owner approval: at least **30 fully
observed windows per gate pooled**, with **no per-team minimum**, where complete
means the window ended before censor (record this separately from K/U/Z because an
early positive can be known even when the remainder is censored). If N=0 or this
coverage is missing, stop as **inconclusive**. Do not extend seeds or horizons to
reach the quota without a separately approved protocol amendment. A longer window
can increase censoring and C can change episode counts; the fixed 24-run plan may
therefore end inconclusive. Thirty is a proposed coverage floor, not a power
calculation or precision guarantee. The owner may change the fixed sample/floor
before freezing the protocol, never in response to results.

### Breach rule — stop, report, no tuning

After each completed seed pair, check run integrity, observer neutrality and hard
correctness; any failure stops immediately. Assess the numerical ceilings on the
**candidate C observed arm only**, after the fixed six-seed batch completes.
Do not stop early on a provisional rate or assume an unknown number of future
episodes. The before arm is descriptive and is not a candidate acceptance gate.
Disabled-observer replicas prove neutrality; they are not additional independent
episodes in N. A confirmed final **K > 0.25 × N** in either candidate gate in the pooled
sample is a breach: stop before any further experiment or variant.

At final assessment, passing requires **(K+U)/N ≤ 0.25** for both pooled gates,
plus coverage and correctness. If K/N passes but the upper bound
exceeds the cap, stop **inconclusive due to censoring**. Equality at 25% is allowed;
use exact count inequalities (4×K versus N, and 4×(K+U) versus N), not rounded displays.
The before arm is reported against the same windows/rules; its known defect is not
new candidate acceptance. Only C's caps decide candidate rejection/continuation.

On breach/inconclusive: retain completed and partial attempts, all episode keys,
commands, counts, first failing pooled gate and source/runtime hashes; report and
**stop**. Do not adjust 2.5 m/s, fence length, reach radii, sample selection, windows,
censor rules or caps to manufacture a pass. No automatic D implementation or rerun.
Passing these two gates only permits owner review of the full metric table; it
neither merges the PR nor establishes realism. The proposed scoped ordinary-recovery
ban during frame f remains a separate correctness requirement: a violation is a
failed implementation, not something tolerated up to 25%.

## One-frame fence state/save audit — design feasibility, not implemented proof

Audit baseline: `ce32b38078596c21adc14682357e0c47f83dd5f8`, production source identical
to the measured Stage A observer. The proposed fence does not exist yet. Relevant
paths (line references are this audited base):

| Path | Current behavior / audit consequence |
| --- | --- |
| TickOrchestrator.RunTick, phases Input→Intent→AI→Physics→Resolve→Events→Snapshot | All ordinary per-frame mechanics precede snapshot commit. A fence can be bounded inside one RunTick, but only with explicit reset/clear invariants. |
| MatchEngine.TryResolveTackles (4316–4325) | BallLoose happens in AI. Carrier fencing must be active before later Physics/Resolve acquisition. Preserve all affected carrier IDs if multiple episodes share a frame; do not overwrite one ID and release an earlier carrier. |
| RunResolvePhase (5738, 5746) → private RunFirstTouch (6508) / RunLooseBallPickup (6663) | These two methods have no off-tick production caller. Candidate-admission guards here can be frame-local. A first-touch guard must act before Evaluate/Apply changes ball state, not undo a completed touch. |
| TakeControlledPossession (9214), including goalkeeper adapter.SetPossessor (9646–9649) | This is wider than the two pickup paths. Keeper claim/save callers commit their own state around this void host call; silently dropping SetPossessor after reporting a successful claim is unsafe. Stage B must classify every open-play acquisition and guard admission before a successful mechanic commits. Audit targeted keeper cases, not only the 37 outfield-style observed episodes. |
| ApplyRestart (5937–5960) → SetPossessingAgent | Law awards bypass ordinary pickup. Censor first, clear the fence before restart effects, and preserve the awarded taker. No blanket owner setter filter. |
| CaptureDurableHeader/Payload (3136/3162) and CapturePostSnapshotWorldState / ApplyPostSnapshotWorldState (3179/3190) | Durable copies and the format-3 off-tick overlay operate at the completed-frame boundary. No live fence may alter an off-tick mutation, the overlay, or the next tick after restoring. |
| SetTeamTactic / SetPlayerTactic / RequestSubstitution and test-only force possession (2866) | Off-tick staging/substitution/test operations must not inherit a previous frame's exclusion. A frame-number-only guard in a shared acquisition setter can remain live after RunTick returns and differ from a restored engine. Reject that implementation shape. |

**Feasible schema-neutral design:** preallocate a fixed roster-sized mask (or an
integer bit mask); zero it before the first Input consumer; arm/union carrier bits
only during the tackle frame; clear at law stops/restarts and **before any Snapshot
serialization**, and leave it zero after RunTick returns. A later tackle closes the
old measurement episode but does not release its carrier fence: union all affected
carriers for the frame until a law stop or frame-end clear. Analytical censoring
and gameplay fence lifetime are distinct. Restrict reads to the
current simulation's ordinary acquisition-admission paths. Snapshot, restore and
off-tick public surfaces never consult a live mask. The mask is then identically
zero at every supported save boundary and cannot cause future divergence.
Do not add an expiry countdown that survives the frame, and do not reset gameplay
ball kinematics: the knock itself already belongs to serialized BallState.

**Conclusion:** a one-frame fence can be schema-neutral under that contract; merely
calling it “one-frame” is not proof. This is a static feasibility audit, not a tested
implementation. Before accepting a no-bump Stage B implementation, require:

1. Mirrored first-touch/pickup and applicable keeper admission cases, including
   multiple same-frame losses; every intended ordinary path observes the policy.
2. Exact save/restore continuation at the tackle's committed boundary, next frame,
   and an off-tick tactic/substitution overlay, compared with uninterrupted play.
3. Test-only force-possession/off-tick operations after the boundary behave the same
   in original and restored engines; no stale exclusion survives.
4. Restart/foul/half/full-time early-return paths leave the mask cleared; law awards
   remain unchanged. No unsupported mid-phase save/resume claim.
5. Both observer modes preserve their own raw chain and deterministic replay.

If a supported future consumer can read armed fence state after Snapshot or during
an off-tick operation, or these proofs fail, it **is causal saved state**: serialize
the full affected-ID state/expiry, allocate the next free world schema on the selected
v29 base (currently v30), validate IDs/booleans/count/bounds/expiry/restore state and
add digest single-field probes and exact continuation tests. Do not omit it to
preserve a “cheap” experiment. Outer framing changes need separate justification.

## Symmetric lockout — fallback only

D remains unselected: investigate physical knock plus exclusion of carrier and
tackler **only if C fails and the owner authorizes a new design**. Reuse the same
stop definitions to compare candidates, with any new eligibility window frozen
before a D run. A bare radius/speed duration does not guarantee separation from two
moving players under deceleration; event-dependent exclusion needs an expiry/deadlock
policy and generally adds saved state. Do not promise a contest or a no-save-state
implementation, derive an unqualified escape guarantee, or automatically tune C
into D. No symmetric-lockout gameplay code is authorized.

## Owner stop

Approve/revise the proposed pooled-only 25% / 25% caps, f+1..f+24 carrier window,
descriptive f+1..f+3/time-to-regain reports, all-episode/censor-bound rules, 30 pooled
complete-window floor, fixed 24-run plan, proposed new 2.5 m/s parameter and scoped
ordinary-recovery policy before any Stage B code/capture. Source/save feasibility
must still be proved by the eventual implementation tests. Then review results;
no merge or tuning is implied by an experimental pass.

| Version | Date | Notes |
| --- | --- | --- |
| 1.0 | November 9, 2026 | Preregisters horizons, overlap/censor rules, within-frame chronology and metrics; proposes three fixes and an explicit policy boundary before measurement. |

| 1.1 | November 10, 2026 | Observer implemented and 20 definition/neutrality tests pass; release stripping verified. Exact [GT]/[FIXED] tag formatting corrected. Measurement definitions and proposed values unchanged; no full census yet. |

| 1.2 | November 10, 2026 | All six seeds measured with exact observer neutrality and independent chronology audit; 37/37 passive same-frame regains. Retains storage interruption/recovery, per-seed metrics, candidate risks and pending owner/Unity decisions. Frozen definitions and proposed constants unchanged. |

| 1.3 | November 10, 2026 | Owner-requested C experiment proposal: two 25% ceilings, exact early window, denominator/censor bounds, adequacy and stop/no-tuning rules; static save/off-tick audit and conditional schema requirement. D is fallback only; no gameplay/capture or approval claim. |

| 1.4 | November 10, 2026 | Review revision, still proposed: G2 f+1..f+24 with geometric caveats and first-regain distribution; pooled-only gates, no per-team quota; retains proposed 30 pooled coverage floor and explicit inconclusive risk. No gameplay/capture or approval. |
