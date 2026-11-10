# #440 BallLoose — Stage A investigation and proposed choices

Created November 9, 2026. Purpose: freeze episode measurement, collect within-frame
ownership chronology and present a concrete gameplay choice. Version 1.2.
Status: Stage A measured and observer neutrality proven; draft PR, Unity host pending. No gameplay fix or approved policy.
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
| C — velocity plus one-frame carrier fence (recommended) | Combine A's physical knock with a one-frame fence on ordinary first-touch/pickup eligibility. The original carrier competes again on the next frame. This separates loss of control from immediate recovery without a multi-frame immunity advantage. | A's proposed 2.5 m/s parameter plus `BALL_LOOSE_CARRIER_FENCE_FRAMES = 1` **[FIXED] (proposed policy)**. Loss at f=120 blocks ordinary reacquisition only at 120; eligible at 121. | No RNG. A frame-local fence can be excluded from snapshots **only if** it resets before all next-frame consumers and cannot affect an off-tick operation; prove that in Stage B. Otherwise serialize and bump the next free schema. | Same-frame carrier regains zero; later recovery remains possible. Movement is physical; the one-frame fence may still favor the nearby tackler or merely defer the old carrier to the next frame. No forced winner is coded, but a near-automatic BallWon effect is a measured Stage B risk. Shots/goals/fouls have no reliable directional prediction; measure without tuning. |

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
contest. Recommend C for the narrow policy and early restored eligibility, with
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

## Stage B preregistration and stop

Freeze tackle-frame regain count, tackle-frame end-owner split, fixed-horizon split,
dispossessions/match, teammate recoveries/match, global holder changes/match,
shots/goals/fouls per team/match before any Stage B run. Retain every seed and censored
episode; no favorable exclusion or target fitting. Report paired six-seed changes
with raw digests validated independently (gameplay identity is not expected).

After Stage A, publish the observation-only draft PR and measured note, then stop
for the owner to choose a fix, approve its proposed new parameter and decide the
scoped same-frame policy. No Stage B gameplay code is authorized by this note.

| Version | Date | Notes |
| --- | --- | --- |
| 1.0 | November 9, 2026 | Preregisters horizons, overlap/censor rules, within-frame chronology and metrics; proposes three fixes and an explicit policy boundary before measurement. |

| 1.1 | November 10, 2026 | Observer implemented and 20 definition/neutrality tests pass; release stripping verified. Exact [GT]/[FIXED] tag formatting corrected. Measurement definitions and proposed values unchanged; no full census yet. |

| 1.2 | November 10, 2026 | All six seeds measured with exact observer neutrality and independent chronology audit; 37/37 passive same-frame regains. Retains storage interruption/recovery, per-seed metrics, candidate risks and pending owner/Unity decisions. Frozen definitions and proposed constants unchanged. |
