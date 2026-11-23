# W13 follow-up validation

The November 23 KD-W1 decision defers landing changes to the 2.0 m and 1.0 m
stand-offs. Calibrate after W9/W10 and the loose-ball marking decision, against
agreed football reference measures. Distance sweeps are descriptive.
The 0.02 m goalward overlap is
handled in Agent Movement #2 by using the existing physical acceleration curve
when a tracking agent launches or walks toward a moving mark point.

W13.2 uses Mixed by default. The ManOriented row uses a 2-player zonal back-line
minimum and a 5-player man-mark cap as uncalibrated [GT] values. Invariant 2
demotions are counted separately for each defending team. Zonal disables mark
movement for the selected team and preserves its legacy assignment path for
tackles. Set the scheme independently in a team tactics file.

```ini
[home]
markingScheme = Zonal

[away]
markingScheme = ManOriented
```

The demo client exposes the home scheme on Tactics Setup. World snapshot schema
v31 includes both active and pending schemes. Every raw digest moves with the
schema bump. No RNG stream, domain tag, draw site or draw order was added.

## §10 capture and analysis

Run on SDK 8.0.426 and CLR 8.0.32 with NumPy available to the analyzer.

```sh
bash tools/dotnet-ci/run-marking-protocol.sh protocol /path/to/captures
python3 tools/dotnet-ci/analyze_marking.py /path/to/captures /path/to/report
```

The default corpus is the #434 six seeds, with C0–C9 armed and C0 disarmed.
Each match captures sample positions and #12 slots in world coordinates, all-frame
possession and ball x, and typed ledger rows. Each sample contributes 11 bytes
per player to `marking.bin.gz` in §10 order. Dispatched PRESS actions are checked
against the mechanics-phase carrier. The match verifies assignment invariants,
eligible mark-target recipients, finite state, a second run from kickoff, and
continuation from a halfway snapshot. Only completed runs appear in the matrix. A failed attempt remains in
the output's `.attempts` directory.

The analyzer reports M1–M4 by seed, case, defending team, attacker slot, spell
origin, dismissal stratum and goalkeeper inclusion. M5 preserves shot-frame
geometry, M6 reports linked, own and uncertain goals, M7 reports denominators
and censored spells, and M8 records ball x at open-play possession changes.
Each exposure field contains its numerator and denominator. The match report
includes tackle intents and resolutions, fouls, offsides, knockdowns, mark census,
Invariant 2 demotions and goal/shot deltas to the same-seed C0.

The archived November 13 same-schema comparison closes the historical digest
criterion for W13.1 candidate `6491de79` against W13.0-era main `39de4662`.
Every per-tick digest matched on all six seeds with the switch off, and the
armed positive controls differed. That run used non-certifying .NET 8.0.425.
The November 23 pinned-runtime rerun reproduced switch-off identity for all
six seeds, covering 1,944,000 tick digests and 324 checkpoints. See the
[recorded evidence](evidence/w13-follow-ups-20261123/README.md).
The v29 and v30 schema bumps, and F9's intentional switch-independent tackle
fix, supersede the historical identity rule for current builds. Current v31
checks two-run determinism and restore within each arm. Off/on football
differences are descriptive.

## Descriptive sensitivity

```sh
bash tools/dotnet-ci/run-marking-protocol.sh distances /path/to/distance-runs
python3 tools/dotnet-ci/analyze_marking.py /path/to/distance-runs /path/to/distance-report --sensitivity

bash tools/dotnet-ci/run-marking-protocol.sh schemes /path/to/scheme-runs
python3 tools/dotnet-ci/analyze_marking.py /path/to/scheme-runs /path/to/scheme-report --sensitivity
```

The distance arms vary one dial at a time. Ordinary marking uses 1.5, 2.0 or
2.5 m. The tight arms set TightMarking on both teams' outfield agents and use
0.75, 1.0 or 1.25 m. The body clearance floor still applies and may limit how
much the tight arms differ. The default sensitivity cases C0, C3, C4, C7 and C8
cover the neutral match, mirrored F4231 changes and mirrored Attack Duty
changes. The scheme run covers C0–C9. The
[short development captures](evidence/w13-follow-ups-20261123/README.md) show
the metrics and demotion counter working. Use `TD_MARKING_MEASURE_SEEDS` and
`TD_MARKING_MEASURE_CASES` to select a batch. `TD_MARKING_MEASURE_TICKS` and
`--allow-short` are only for smoke runs.
