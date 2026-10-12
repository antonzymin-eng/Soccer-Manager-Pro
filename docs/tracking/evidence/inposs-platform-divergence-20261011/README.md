# Six-seed platform comparison of the C1 final-third possession-phase share

**Captured:** October 11, 2026, 21:32–22:45 UTC (afternoon on the host's Pacific clock)\
**Purpose:** Answer the open question on `sim_match_engine_inposs_gate`: on seed `0x0F1E2D3C4B5A6978`
the Unity editor measured a share of 0.361 and the Linux shim 0.928 at the same commit. Is that gap
systematic across seeds, so that Linux measurements might not describe the game that ships, or is it
specific to one seed?\
**Answer:** It is one seed, and the cause is an engine defect, not the platform. The two runtimes do
play different matches from tick 5 onward, starting from a one-ulp difference. On five of the six
frozen seeds, though, the share agrees to within ±0.025 with no consistent sign. On the sixth, the
Unity match reaches a ball-state deadlock: a shot strikes the outside of the near post, the ball is
left `OutOfPlay` but back on the pitch side of the goal line, no restart is ever awarded, and the ball
stays frozen from 15:49 until the half-time reset at 45:00. The Linux match on that seed never
reaches the deadlock.\
**Source:** see `source-identity.txt`. Both lanes ran `src` tree `1839bb0f…`, which is `main`
`9d70dc8b` plus the two test-only files of this landing.\
**Integrity:** `SHA256SUMS` covers every other file in this directory and is verified by
`tools/dotnet-ci/check_evidence_manifests.py`.

## Result

`PlatformDivergenceDiagnosticTests` runs the gate's own match loop (`MatchEngineInPossGateScenarios.PlayOne`)
for a full 324,000 ticks on each of the six frozen seeds from
`docs/tracking/w2-six-seed-evidence-preregistration.md`. Shares are home view; the away view was
bit-identical to it in all twelve runs.

| Seed | Unity samples | Unity share | .NET 8 samples | .NET 8 share | Unity − .NET |
| --- | ---: | ---: | ---: | ---: | ---: |
| `0x0F1E2D3C4B5A6978` | 28,363 | **0.360611** | 15,924 | 0.927719 | −0.567 |
| `0x00000000D1A6D05E` | 18,688 | 0.939052 | 17,116 | 0.963660 | −0.025 |
| `0x5EED000000000003` | 17,185 | 0.944487 | 16,178 | 0.952343 | −0.008 |
| `0x5EED000000000004` | 14,620 | 0.971409 | 14,386 | 0.954192 | +0.017 |
| `0x00000000D1A6D05F` | 17,453 | 0.951241 | 17,051 | 0.952613 | −0.001 |
| `0x1A2B3C4D5E6F7081` | 17,882 | 0.943910 | 16,234 | 0.937539 | +0.006 |

Without the deadlocked seed, the means are 0.9500 (Unity) and 0.9521 (.NET), a paired mean difference
of −0.002 with differences of both signs. Every run except the deadlocked one clears the gate's
`> 0.70` bound by more than 0.23.

## Where the platforms diverge

- **Rosters are identical.** The `squadHome`/`squadAway` fingerprints in both `*-shares.tsv` files
  agree for every seed, so roster generation and its RNG stream agree.
- **Ticks −1 to 4 are bit-identical** for the ball and all 22 agents, on every seed.
- **Tick 5 is the first difference on all six seeds**, in agent movement only; the ball is still
  identical. `divergence-summary.txt` lists the agents. `tick3-5-agent-state-{unity,net8}.txt` dump
  the raw state on the gate seed: the tick-5 differences are one unit in the last place in facing
  direction, speed and velocity components (for example agent 1 facing `3F7B9218,BE3DA912` against
  `3F7B9219,BE3DA913`; agent 3 speed `3D088888` against `3D088889`).

That is an evaluation-precision difference between the Unity Mono runtime and CoreCLR, which the match
then amplifies chaotically. It is not a logic or set-up difference, and it is not an OS math-library
difference: the .NET 8 shim on this Windows host reproduces the Linux CI run of the gate test exactly
(`gate-trace-comparison.txt`, identical 1,979-event engine log). The project already records
cross-runtime bit-equality as Stage 5+ (Fixed64) work, so this run confirms the existing position
rather than opening a new one.

## The deadlock on seed `0x0F1E2D3C4B5A6978` (Unity)

`stall-window-unity.txt` traces every tick around the event:

1. Tick 56,955: the ball is airborne at (0.034, 30.104, 0.468) heading out over the home goal line
   toward the near post (velocity (−12.58, +14.44, −2.22) m/s).
2. Tick 56,956: `BallPhysicsCore.UpdateBallPhysics` integrates the ball past x < −0.11 m, so
   `BallStateMachine` sets `OutOfPlay`. `BallCollision.ApplySweptGoalFrameCollision`, which runs next
   in `MatchEngine.RunPhysicsPhase`, finds the post strike, moves the ball back to the contact point
   (0.0273, 30.1122, 0.4668) and reflects its y velocity (+14.44 → −13.63). It does not change
   `State`, so the ball remains `OutOfPlay`.
3. In the Resolve phase `CheckRestartAndApply` calls `BallCollision.CheckBoundaries` on the
   post-rebound position. x = 0.027 is inside the pitch, so it returns "not out" and no restart is
   applied.
4. `OutOfPlay` applies no physics forces, and nothing in the engine moved the ball out of that state
   until the 45:00 half-time reset. The ball stays at (0.027, 30.112) with its velocity frozen at
   every checkpoint from tick 57,000 to 161,400 and is back on the centre spot at tick 162,000. That
   is about 105,000 ticks, roughly 29 minutes of match time
   (`divergence-summary.txt`: the 1,740 s static run on the Unity gate-seed trace; no other match on
   either platform is static for more than 10 s).

The stall adds about 17,500 final-third samples (one every 6 ticks, assuming the reset lands at tick
162,000), which is why the Unity sample count for this seed is 28,363 instead of about 14,000–19,000.
If the committed phase during the stall is a transition (the ball is `OutOfPlay` and held by nobody),
the rest of the match scores about 10,228 / 10,856 = 0.94. That figure is arithmetic on the totals;
the phase inside the window was not separately recorded. So the gate was correct to fail on Unity: it caught a real defect that the Linux
lane missed only because its match takes a different path.

The defect does not depend on the platform. Any match on either runtime can reach it whenever a ball
crossing the goal line is turned back by the outside of a post within the same tick. None of the six
.NET matches here reached it.

## Files

| File | What it is |
| --- | --- |
| `source-identity.txt` | Commits, `src` tree hashes, checkouts and porcelain state for both lanes, and the Linux-equivalence argument. |
| `unity-shares.tsv`, `net8-shares.tsv` | Per-seed ticks, samples, home/away share (and their float bits), and roster fingerprints. |
| `unity-status.txt` | The Unity `TestRunnerApi` callback log: test passed in 1,890.8 s. The run's NUnit XML was not written (its `RunFinished` callback never logged; a `CancelAllRuns` request issued and then blocked during the run is the likely cause), so this status line is the run record. The `.NET` run passed in 20 m 6 s. |
| `*-trace-<seed>-trimmed.tsv.xz` | Per-tick bitwise state fingerprints: ball position, velocity and state bits, plus one FNV-1a hash per agent. Trimmed to ticks −1 to 60 and every 600th tick from 3,600. |
| `full-trace-sha256.txt` | SHA-256 of the untrimmed traces (every tick −1 to 3,599, then every 600th), which were not committed for size. |
| `divergence-summary.txt` | First divergent tick per seed and the longest static-ball stretch per match. |
| `tick3-5-agent-state-{unity,net8}.txt` | Full agent state bits on the gate seed at ticks 3–5, from one shared probe body (`analysis/ProbeBody.cs.txt`). |
| `stall-window-unity.txt` | Ball position, velocity and state before and after every tick from 56,400 to the stall, on Unity. |
| `gate-trace-comparison.txt` | `sim_match_engine_inposs_gate` engine logs on Linux CI, this host's .NET 8 and Unity: Linux and Windows .NET identical, Unity different from the first event. |
| `analysis/*.txt` | The comparison and probe scripts used above, stored as text. |

## Version History

| Version | Date | Notes |
| --- | --- | --- |
| 1.0 | October 11, 2026 | Initial evidence set: six-seed Unity vs .NET 8 shares, per-tick divergence, and the goal-frame `OutOfPlay` deadlock on the gate seed. |
