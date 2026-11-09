# #440 BallLoose — Stage A investigation and proposed choices

Created November 9, 2026. Purpose: freeze episode measurement, collect within-frame
ownership chronology and present a concrete gameplay choice. Version 1.0.
Status: preregistration; no capture results, gameplay fix or approved policy yet.
Branch `codex/440-ball-loose`; source baseline `d4b026aa951432d342217314b8c4d49b53a19e7d`.
#434 remains a separate draft; Stage A therefore measures main, not its unmerged head.

## Observation versus contract

#14 §3.6.5.2 requires BallLoose to release control and remain distinct from Missed
and BallWon. It specifies neither velocity nor a blanket same-frame regain ban.
The reported 14/14 original-carrier re-picks are historical evidence, not this run's
result. Source inspection finds BallLoose releasing without movement in AI, followed
by ordinary Resolve pickup. The nearest player can still be the original carrier.
Keep that mechanism as a hypothesis until acquisition chronology confirms it.

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

## Candidate fixes — proposals, not code or approved constants

| Choice | Mechanism and realism | Proposed constants / example | RNG and snapshot | Expected metrics / risk |
| --- | --- | --- | --- | --- |
| A — velocity only | Give a deterministic knocked ball a direction and speed, leaving all normal eligibility intact. Physical flight/first touch decide recovery. | `BALL_LOOSE_KNOCK_SPEED_M_S = 2.5` **[GT, PROPOSED]**, m/s, proposed authoring range (0,6]. At 60 Hz, 2.5/60 = 0.04167 m before the next integration. Direction away from tackler through contact, with the carrier's facing as the coincident-position fallback. | No new RNG; existing ball kinematics already serialize. No new persistent state. | More moving-ball contacts; opponent recovery may rise, but first touch can still restore the original carrier **in the same frame**. Does not guarantee the proposed policy. No guarantee of which team wins. |
| B — bounded carrier exclusion | Exclude the dispossessed carrier from ordinary first-touch and passive-pickup admission for a short window. It guarantees a contest involving others but the resting ball can make the nearest tackler the automatic winner. | `BALL_LOOSE_CARRIER_EXCLUSION_FRAMES = 6` **[GT, PROPOSED]**, physics frames, authoring range [1,60]. Loss at f=120 blocks frames 120–125; eligible at 126 (0.1 s). | No RNG. Carrier ID + expiry cross snapshots, so next free world-schema bump and restore validation would be required; allocate only on the eventual Stage B base. | Same-frame carrier regains become zero; opponent/teammate recovery rises. Substantial risk of converting BallLoose into BallWon, and a stationary ball may wait for immunity to expire. |
| C — velocity plus one-frame carrier fence (recommended) | Combine A's physical knock with a one-frame fence on ordinary first-touch/pickup eligibility. The original carrier competes again on the next frame. This separates loss of control from immediate recovery without a multi-frame immunity advantage. | A's proposed 2.5 m/s parameter plus `BALL_LOOSE_CARRIER_FENCE_FRAMES = 1` **[FIXED, PROPOSED policy]**. Loss at f=120 blocks ordinary reacquisition only at 120; eligible at 121. | No RNG. A frame-local fence can be excluded from snapshots **only if** it resets before all next-frame consumers and cannot affect an off-tick operation; prove that in Stage B. Otherwise serialize and bump the next free schema. | Same-frame carrier regains zero; later recovery remains possible. More contested movement; tackler may recover, but there is no forced winner. Shots/goals/fouls have no reliable directional prediction; measure without tuning. |

These are new proposed parameters for the selected mechanic, not authorization to
change any existing [GT] value or calibrate toward favorable football metrics.
Owner choice must approve a new parameter before Stage B adds it. The initial
2.5 m/s value is uncalibrated, not inferred from this census.

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
