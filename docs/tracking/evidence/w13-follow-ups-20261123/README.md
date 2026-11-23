# W13 follow-up evidence

These November 23 captures used SDK 8.0.426 and CLR 8.0.32. The pilots use
schema v31 and seed `0000000000000001`. Each row covers 3,600 physics ticks
(60 simulated seconds). Team pairs list home first.

- `distance-pilot.csv` covers all six stand-off variants in C0, C3 and C7.
  The engine builds are identified per row because the capture runner was
  rebuilt during this smoke run. The rows verify collection at each
  setting and carry no calibration decision.
- `scheme-pilot.csv` compares Mixed and ManOriented in the same three
  cases on a single build. Mixed produced eight cap demotions across the
  six defending-team and case totals. ManOriented produced one. Every run passed
  repeat-run and restore digest checks. This short sample cannot establish
  the full-match frequency of cap demotions.

## Tight-marker follow-up build

`tight-tracking-sensitivity.csv` contains 72 one-minute captures from the
final acceleration rule, which applies only when `TightMarking` is active.
Each of the eight variants covers the same three seeds and #434 cases C0, C3
and C7. All runs passed kickoff repeat and halfway restore. The per-case shot
and goal deltas, tackle counts, grounded entry causes, and cap demotions are
in the CSV. `tight-tracking-source-identity.txt` records build provenance.

| Variant | Goals | Shots | Tackle resolutions | Collision-tagged grounded entries | Invariant 2 demotions |
|---|---:|---:|---:|---:|---:|
| Normal 1.5 m | 1 | 2 | 1 | 996 | 72 |
| Normal 2.0 m | 1 | 3 | 2 | 1,043 | 48 |
| Normal 2.5 m | 0 | 0 | 5 | 1,016 | 61 |
| Tight 0.75 m | 2 | 6 | 2 | 1,034 | 52 |
| Tight 1.0 m | 2 | 4 | 3 | 1,006 | 73 |
| Tight 1.25 m | 2 | 5 | 1 | 1,069 | 85 |
| Mixed | 1 | 3 | 2 | 1,043 | 48 |
| ManOriented | 0 | 2 | 1 | 1,010 | 16 |

The cap bound in both schemes in this sample. The distance sweep is
descriptive and KD-W1 defers calibration. The tight arms apply the existing
contact floor before composing a target.

`tight-tracking-c0-opening.csv` pairs the three default Mixed opening minutes
with marking off and on. All entries below are collision-tagged. Team pairs
list Home first. Each run passed kickoff repeat and halfway restore.

| Seed | Off | On |
|---|---:|---:|
| `0000000000000001` | 54–50 | 54–60 |
| `000000005A5A5A5A` | 53–53 | 54–54 |
| `0000000099887766` | 55–56 | 60–60 |
| Both teams, all three seeds | 321 | 342 |

The high opening rate is present with marking off. Enabling marking raised
the total across the three seeds by 6.5%.

`tight-tracking-full-c0.csv` records both 324,000-tick arms for seed
`0000000099887766`. Both passed the finite-state, invariant, legal-target,
repeat-run and restore checks. The analyzer decoded M1–M8 in both arms.

| Arm | Goals | Shots | Collision-tagged grounded entries | Tackle resolutions |
|---|---:|---:|---:|---:|
| Off | 5–3 | 12–20 | 1,080–1,024 | 13–4 |
| On | 3–4 | 10–13 | 1,432–1,423 | 11–13 |

This pair used default instructions for every player. Neither arm exercised
the `TightMarking` acceleration. It establishes the inherited collision counts
and current-schema repeat/restore behavior for this seed. Totals of 2,104 off
and 2,855 on match the same-seed #562 comparison, and the on digest matches the
v31 build that restored the legacy acceleration during diagnosis. The elevated collision rate remains open
under [`w13-loose-ball-knockdowns`](../../open-issues.md#w13-loose-ball-knockdowns).
The full current-source §10 corpus and Unity Editor validation are pending.

### Goalward runner clearance

The scripted `GoalwardRunner_MarkerRetreats_NeverGrounded_NoOverlap_GoalSide`
test logs the minimum of the centre-to-centre gaps sampled after each of 240
physics ticks in the runner's approach. Two Strength-10 hitbox radii sum to 0.84 m.
The test ran on the Linux shim with SDK 8.0.426 and CLR 8.0.32.

| Stand-off | Runner top speed (m/s) | Home gap (m) | Away gap (m) |
|---|---:|---:|---:|
| Ordinary | 3.0 | 1.753952 | 1.758318 |
| Ordinary | 5.0 | 1.753952 | 1.758318 |
| Tight | 3.0 | 0.921654 | 0.920705 |
| Tight | 5.0 | 0.921654 | 0.920705 |

Minimum tight clearance was 0.081654 m at home and 0.080705 m away.
The scripted rig exercises the overlap correction directly. The one-minute
tight sensitivity arms above configure tight marking in live play.

## Unrestricted acceleration experiment

`review-sensitivity.csv` contains 72 one-minute captures: three seeds,
three #434 cases (C0, C3 and C7), six distance settings and two scheme settings.
Every row came from one build and includes successful kickoff-repeat and
halfway-restore receipts. `review-source-identity.txt` records the base commit,
exact source tree at build, tool blobs, SDK and runtime. These runs applied
the new walking acceleration to all markers. The PR gate found a 24.38 m mean
shot distance against the scenario's 24.00 m ceiling, so the final change
uses the new acceleration only for `TightMarking`.

The old `knockdowns` column counted every transition into `GROUNDED`, regardless
of cause. The new table separates collision-tagged entries, sliding tackles,
diving headers and other causes. The high first-minute total persisted:
44–69 entries per team per capture, all tagged `COLLISION`. These are locomotion
state transitions. Repeated collision impulses while a player is already
grounded do not add entries. The pilot does not identify which interactions
created the transitions or establish a representative full-match rate.
The open loose-ball-knockdown issue remains the home for that behavior.

### C0 opening minute with marking off and on

`review-c0-opening.csv` pairs the same build, seeds and first 3,600 ticks. Every
row passed the kickoff repeat and halfway restore check. Counts are collision-tagged
entries into `GROUNDED` and team pairs list Home first.

| Seed | Off | On |
|---|---:|---:|
| `0000000000000001` | 54–50 | 59–59 |
| `000000005A5A5A5A` | 53–53 | 52–59 |
| `0000000099887766` | 55–56 | 61–61 |
| Both teams, all three seeds | 321 | 351 |

The high opening-minute rate is already present with marking off. Across the
three pairs, enabling marking raised the opening total by 9.3%.

The same build also completed a 324,000-tick C0 off/on pair for the second
full-match seed, `0000000099887766`. Both arms passed kickoff repeat and halfway
restore and were decoded through M1–M8. The paired runs exercise the marking
switch as a whole. The tight runner regression isolates the acceleration fix.

| Arm | Goals | Shots | Collision-tagged grounded entries | Tackle resolutions |
|---|---:|---:|---:|---:|
| off | 5–3 | 12–20 | 1080–1024 | 13–4 |
| on | 2–5 | 13–15 | 1484–1497 | 19–11 |

The rows and digests are in `review-full-c0.csv`. This pair and the
first-minute sweep are descriptive. The full current-source §10 corpus and
Unity Editor validation remain pending.

### Full-match comparison of unrestricted acceleration with #562

`review-c0-knockdown-attribution.csv` uses the same `MarkingFlip_FullMatchMeasurement`
instrument on #562 (`2f513fbc`, schema v30) and the follow-up source recorded
in `review-source-identity.txt` (schema v31). Both Release builds used the pinned
SDK and CLR. Every row covers 324,000 ticks of C0 with default Mixed marking
when armed. Counts combine both teams.

| Seed | #562 off | #562 on | Follow-up off | Follow-up on | Change in on arm |
|---|---:|---:|---:|---:|---:|
| `0000000000000001` | — | 2,828 | — | 2,917 | +3.1% |
| `000000005A5A5A5A` | — | 2,632 | — | 2,993 | +13.7% |
| `0000000099887766` | 2,104 | 2,855 | 2,104 | 2,981 | +4.4% |
| On totals | — | 8,315 | — | 8,891 | +6.9% |

The paired off runs matched in every recorded field other than wall time and
schema-specific digest. For that seed, the marking switch added 751 entries
in #562 (+35.7%) and 877 in the follow-up (+41.7%). A third build used the
follow-up source tree with only `src/agent-movement/AgentMovementSystem.cs`
restored from `2f513fbc`. Its v31 on run reproduced the #562 row in every
recorded field except wall time and digest, including 2,855 collision-tagged
entries. That isolates the unrestricted acceleration change as the source of
the additional 126 entries for seed `99887766`. Across the three on runs,
the no-holder count rose from 7,322 to 7,896, accounting for 574 of the 576
additional entries. `groundedLoose` measures the absence of an individual
ball holder, which includes passes in flight. `groundedMarker` counts entries
for agents that had a mark target at the last sampled stride. It does not
identify the other player in the collision. The collision rate and per-tick
attribution remain open under [`w13-loose-ball-knockdowns`](../../open-issues.md#w13-loose-ball-knockdowns).

## Historical switch-off neutrality

`historical-neutrality.csv` and `historical-runs/` retain the pinned-toolchain
rerun of the November 13 comparison. Schema v28 main `39de4662` matched
schema v28 switch-off candidate `6491de79` on all six 324,000-tick matches.
Terminal digests, all-tick SHA-256 values and all 54 checkpoints per seed
matched. The armed control changed the digest by the first checkpoint on all
six seeds. The archived neutrality runner used default squads and enabled
goalkeeper/heading mechanics.

## Initial current-schema characterization

The first full schema-v31 C0 pair used seed `0000000000000001`, reached full time at tick 324,000 and
decoded 53,999 samples per arm. Both arms passed halfway snapshot
continuation. The first pair predates the separate kickoff-repeat receipt. The subsequent
schema-v31 off-arm rerun reproduced its all-tick digest
`DD9566B99CF9AEF66F6D8CBE8403A441F702EF2A36CAFE9831D7FD5B44C2ED0C` and passed
both the kickoff repeat and the halfway restore check.

| Arm | Goals | Shots | Tackle resolutions | Marker PRESS against non-carrier |
|---|---:|---:|---:|---:|
| Off | 5–4 | 7–12 | 14–12 | 0 of 0 |
| On | 5–6 | 20–14 | 21–15 | 0 of 133 |

See the [validation notes](../../w13-follow-up-validation.md) for the full
capture and analysis commands.
