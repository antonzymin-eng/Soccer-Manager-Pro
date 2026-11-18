# W13 switch-off neutrality against main — six seeds, full match

**Created:** November 13, 2026.\
**Purpose:** Show that the W13 changes not yet on main (W13.0 review fixes `fd73de77`, W13.1 and W13.1c on
`claude/great-sagan-rpcmhp`) leave default gameplay unchanged when `MatchEngineConstants.MarkMovementEnabled` is
`false`. The branch's earlier identity probe compared only frame 18,000, and only against the branch's own W13.0 tip.
This run compares every tick of six full matches **against main**.\
**Status:** NON-CERTIFYING. It does not replace the frozen-lane run (SDK 8.0.426 / CLR 8.0.32) named in
[the W13 plan §8](../../w-defender-marking-wiring-plan.md#8-rollout-on-claudegreat-sagan-rpcmhp), and it is not a
Unity result.\
**Integrity:** `SHA256SUMS` covers every other file in this directory.

## Result

| Check | Result |
| --- | --- |
| W13 switch off vs main, terminal digest at tick 324,000 | **Equal on 6 / 6 seeds** |
| W13 switch off vs main, SHA-256 over all 324,000 per-tick snapshot digests | **Equal on 6 / 6 seeds** (1,944,000 digest samples) |
| W13 switch off vs main, digest at every 6,000-tick checkpoint | **Equal on 6 / 6 seeds** (54 checkpoints each) |
| Positive control: W13 with mark movement armed vs W13 switch off | **Different on 6 / 6 seeds**, already at the first checkpoint (tick 6,000) |

The positive control shows the comparison can detect the wiring when it is active. `markMovementEnabled=False`
was read from the W13 build at run time; main has no such field (`absent`).

## Inputs

| Arm | Commit | `src` tree |
| --- | --- | --- |
| main | `39de4662` (PR #546 merge) | `89a92cb3` |
| W13 | `6491de79` (`claude/great-sagan-rpcmhp` `ed0b60c1` merged with main `39de4662`) | `447828bc` |

- Seeds: the six frozen-corpus seeds of [`pre-c-control-20261104/expected.json`](../pre-c-control-20261104/expected.json).
  Those file digests are schema v27 and predate W8 C (v28), so they are not expected to match here.
- Per seed: `new MatchEngine(seed); EnableGkHeading();` then 324,000 `RunTick()` calls, one seed per process, default
  squads and attributes, default configuration (no config file).
- The armed control calls the internal `TestOnly_ArmMarkMovement(true)` seam by reflection after construction.
- Build: `tools/dotnet-ci/generate_projects.py` in a fresh detached worktree per arm, then
  [`runner.csproj.txt`](runner.csproj.txt) (net8.0, Release) referencing that arm's `match-engine.gen.csproj`.
  Runner source: [`Program.cs.txt`](Program.cs.txt), identical for all arms.
- Host: Windows 11, .NET SDK 8.0.425, runtime 8.0.31 (`runtime=` in every run file). Three processes ran concurrently.

## Files

| File | What it is |
| --- | --- |
| `runs/<arm>-<mode>-<seed>.txt` | First line: seed, ticks, switch value, armed flag, terminal digest, all-tick SHA-256, elapsed, runtime. Then the 54 checkpoint digests |
| `runs.csv` | The first lines of all 18 run files as a table |
| `Program.cs.txt`, `runner.csproj.txt` | The runner and its project (worktree path replaced by `<worktree-w13>`; the main arm differs only in that path) |

## Limits

- Not the frozen lane, so it cannot be adopted as the frozen-lane identity result; that run stays outstanding.
- Covers default configuration with `EnableGkHeading()` on the frozen corpus only.
- Says nothing about behaviour with the switch on, which is the W13 §10 measurement.

## Version History

| Version | Date | Notes |
| --- | --- | --- |
| 1.0 | November 13, 2026 | Initial six-seed full-match switch-off comparison against main, with an armed positive control. |
