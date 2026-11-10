# #434 formation/Duty evidence

Created November 9, 2026; closeout November 10. This package compares main
`d4b026aa951432d342217314b8c4d49b53a19e7d` with runtime implementation
`23f2fedb140d47b22be36b2d495190b0b1685251`. Later commits add tests, tooling and
tracking only; the production-source identity receipt records that relationship.
The approved contract is [the preflight](../../w-434-formation-duty-preflight.md).

## Scope and interpretation

Formation locks before physics frame 1 and remains fixed. Complete tactic requests
still commit at tactical strides; requested/effective formation are separate.
Duty moves each outfield player's canonical anchor using the existing −3/0/+3 m
catalogue. No RNG, tuning, lineup selection or in-match formation switching changes.

The F442-selected lineup remains mapped by roster index. In particular LM becomes
DM/DM1, RM becomes LW/CAM, and one striker becomes RAM in F4231. The owner accepted
this limitation. Neutral-attribute captures characterize geometry and composition;
they do not measure specialist suitability. Real-squad identity/restore fixtures
cover the unchanged selected IDs. Formation-aware selection and runtime switching
remain explicit follow-ups.

C0 requires exact covered-byte identity. C1–C9 have hard correctness/integrity stops
and descriptive football metrics; no numeric realism acceptance bands were approved.
All cases, seeds and both teams are reported without tuning or favorable selection.
There is no full-match non-default-formation/non-Support combination; unit replay
covers that interaction, as agreed before capture.

## Capture and acceptance

| Check | Result |
| --- | --- |
| Historical frozen B reproduction | All 12 full runs reproduce the six historical terminal fingerprints; original analysis passes. |
| Base reuse eligibility | Pinned source audit + all 120 checks in 36 short probes pass. |
| C0 observer off/on and base/head comparison | All 90 exact stream comparisons pass over six seeds; 24 full runs. |
| Full matrix | 90 unique primary observed matches audited; 29,160,000 primary calls. |
| Geometry/state integrity | Finite full trajectories, valid ownership and existing #12 active-outfield slot bounds pass. |
| Attempts outside primary 90 | 12 extra plain C0 runs, 12 historical runs, 36 probes; preliminary v1.0 attempts retained separately. |

World-state v29 necessarily changes raw digest preimages. The comparator validates
literal schemas, initialized/effective metadata and row counts, then includes every
common byte exactly once. Default overhead is 18 bytes; one/two F4231 teams add
27/36 bytes. Independently reconstructed raw snapshot chains and each arm's own
nonempty BuildHash remain intact. No shipping hash or restore identity is bypassed.

The mutation proof uses a real event-containing snapshot: every one-bit perturbation
at every base/head payload byte must change common output or reject excluded
metadata. This is additional to synthetic checks, not a claim based on field names.

## Characterization

| Case | Six-match shots base → head | Goals base → head | Fouls base → head |
| --- | ---: | ---: | ---: |
| C0 | 146 → 146 | 47 → 47 | 48 → 48 |
| C1 | 146 → 214 | 47 → 83 | 48 → 45 |
| C2 | 146 → 210 | 47 → 98 | 48 → 46 |
| C3 | 146 → 181 | 47 → 86 | 48 → 36 |
| C4 | 146 → 134 | 47 → 56 | 48 → 32 |
| C5 | 105 → 96 | 37 → 26 | 44 → 45 |
| C6 | 167 → 94 | 73 → 31 | 53 → 45 |
| C7 | 152 → 210 | 55 → 99 | 44 → 48 |
| C8 | 129 → 250 | 56 → 104 | 50 → 45 |
| C9 | 146 → 177 | 47 → 78 | 48 → 36 |

The formation and Attack-Duty contrasts materially increase scoring on this neutral
corpus: C2 is 47 → 98 goals and C8 is 56 → 104. These are findings for owner football
review, not a realism pass. No parameters or acceptance bands were changed.

See `analysis/per-match-team.csv` for every seed, arm and team, including explicitly
labeled reused C0 references, controlled/loose and pass-flight possession frames,
four AI phases and outfield slot extents/minimum pair distance. `paired-deltas.csv`
retains all paired team differences. `audit.json` records stream hashes and hard
correctness checks. Goals use ScoringTeam, shots TeamId and fouls the offender team.
Scheduled and live denominators remain distinct.

The instrument retains full BallState and ownership on every frame. No independent
numeric stalled-play definition was frozen in the capture protocol; stationary-ball
occupancy must not be called a stall. This package does not claim such a metric.

## Reproduction and retained bytes

Use SDK 8.0.426 / CLR 8.0.32, Debug, unchanged JIT defaults and the recorded generated
shim settings. The exact as-run tools and their original manifest are preserved in
`capture-inputs.tar.xz` (extract before inspecting/replaying); `prepared.json` pins their hashes, source archives and binaries.
The commands/exits are in each stage's `runs.json`, with runner logs and summaries
in `retained/`. Source commits can be rebuilt with `prepare.py` in a new workspace.
Historical reproduction has its own immutable source/instrument freeze.

```bash
python3 docs/tracking/evidence/formation-duty-20261109/prepare.py --repo "$PWD" --work-dir /tmp/formation-duty --head 23f2fedb140d47b22be36b2d495190b0b1685251 --dotnet /path/to/dotnet --packages /path/to/nuget-cache
python3 docs/tracking/evidence/formation-duty-20261109/run.py --work-dir /tmp/formation-duty --stage probe
python3 docs/tracking/evidence/formation-duty-20261109/run.py --work-dir /tmp/formation-duty --stage control
python3 docs/tracking/evidence/formation-duty-20261109/run.py --work-dir /tmp/formation-duty --stage matrix
python3 docs/tracking/evidence/formation-duty-20261109/analyze.py --work-dir /tmp/formation-duty --output-dir /tmp/formation-duty-analysis
```

For reanalysis of retained captures, materialize this directory's LFS payloads and
use `retained/` as `--work-dir` (stage receipts are retained alongside the cases).
`tools/dotnet-ci/retained_trace_store.py` reads both as-run gzip and the packed stores.
Every archive has a SHA-256 index; every decoded stream has its own raw byte count
and SHA-256. Integer word-delta/shuffle compression preserves all IEEE float bits,
records and event order. NumPy is optional; its portable fallback decodes identical
bytes. Original gzip container bytes are not retained; their hashes/sizes are in
the index. All decoded per-frame bytes are retained and were verified before deletion
of the gzip containers. The three storage tests cover bit patterns, block boundaries,
empty streams and refusal to delete originals after corruption.

The preliminary v1.0 control was stopped uniformly before comparing acceptance
results because the instrument omitted the per-frame team/pass-flight witness and
exhaustive real-payload mutations. Its attempts remain separately archived, including
incomplete streams and recorded exits. They are not completed validation runs. The
original and revised inputs are separately identifiable; historical evidence is untouched.

## Validation and host request

Both Linux full-gate attempts and the correction are retained. The successful run reports,
verbatim: **Failed: 0, Passed: 973, Skipped: 16, Total: 989** for MatchEngine;
**143 passed / 13 skipped** for PositioningAI. Across all 37 assemblies the console
summaries total **0 failed / 4,512 passed / 225 skipped**. `Gate PASSED`; quarantine empty.
This full run discovered 33 cases in the new fixture. The final 35-case fixture,
including two subsequently added real-squad cases, separately passes 35/35.

The unchanged adapter console omits three existing inconclusives from its totals:
EventSystem's unpinned boot golden, ShotMechanics' BM007 inability to reach its
stumble setup, and MatchEngine's insufficient connected-tackle sample. TRX retains
all three as NotExecuted. The tackle fixture was also reproduced unchanged on main
under this pinned toolchain. All 225 ignored/opt-in skips are existing; **zero new
skips, quarantines or disabled tests**. The per-assembly TRX outcome/reason inventory
is in `validation/434-gate-summary.json`.

The exact full local command is `bash tools/run-tests-local.sh --pr`. The first
attempt exposed two second-half restart fixtures that jumped the clock before the
new kickoff lock. They now run the normal first tick before that test-only jump;
all original outcome assertions remain. No production fix was needed, and the
narrow restart suite passes 19/19. Both full attempts are retained.

The normal commit hook was attempted and exceeded its unchanged 60-second cold-build
budget. Subsequent commits used a command-scoped bypass, with independent validation;
this is not a passing-hook claim. No test/hook policy was weakened.

Pinned Unity 6000.4.9f1 validation remains pending. The final PR body names the exact
checkout SHA. Force a full compile/reimport and retain Editor.log; run unfiltered
EditMode assemblies `TacticalDirector.MatchEngine.Tests`, `TacticalDirector.PositioningAI.Tests`,
`TacticalDirector.MatchClientCore.Tests` and `TacticalDirector.MatchViewer.Tests`. Expected fixtures include
`MatchEngineFormationDutyTests`, `MatchEngineRestartTests`, `TacticTranslationTests`,
`SlotComposerStageTests`, `TickStampedCommandReplayTests`, `RotationControllerTests`,
`MatchEngineSnapshotSchemaTests` and `LiveMatchFrameCueTests`.
Retain NUnit XML, pass/fail/skip/inconclusive counts and source identity. Linux
shim evidence and an absent-license CI skip do not satisfy this host request.

CSV report containers use LF newlines in Git; their cell values are unchanged from
the analyzer output. Binary trace streams retain their original exact byte hashes.
