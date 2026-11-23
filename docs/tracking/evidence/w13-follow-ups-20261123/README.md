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

## Historical switch-off neutrality

`historical-neutrality.csv` and `historical-runs/` retain the pinned-toolchain
rerun of the November 13 comparison. Schema v28 main `39de4662` matched
schema v28 switch-off candidate `6491de79` on all six 324,000-tick matches.
Terminal digests, all-tick SHA-256 values and all 54 checkpoints per seed
matched. The armed control changed the digest by the first checkpoint on all
six seeds. The archived neutrality runner used default squads and enabled
goalkeeper/heading mechanics.

## Current-schema characterization

The first full schema-v31 C0 pair reached full time at tick 324,000 and
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
