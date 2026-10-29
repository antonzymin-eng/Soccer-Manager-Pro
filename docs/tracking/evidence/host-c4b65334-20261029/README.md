# Pinned-host Unity compile, EditMode run and before/after captures — fixed-height playback reason

**Captured:** October 29, 2026, 15:23–15:52 UTC (08:23–08:52 on the host's Pacific clock)\
**Purpose:** Evidence for the PR #518 fix `c4b65334` (`S0ReservedReason` v1.0, `S0UiFactory` v1.3,
`S0ScreensBehaviour` v1.12). The owner reported that the pitch changed size on the first and the last Faster click.
The pitch is sized from the page height left after the content above it, and the "Already at real time" and "Already at
the fastest speed" reason boxes appeared and disappeared above it. The four playback reasons now share one box that
keeps the tallest one's height. `match-client-unity` is excluded from the Linux shim, so the Unity compile is the only
compile of this change.\
**Host:** the owner's Windows 11 machine, with the Unity 6000.4.9f1 (f7258d6eebbe) editor (Mono). This is the
certified determinism host.\
**Source:** commit `c4b65334f75fd43694c2627976e1c8e46fb30cad` (the fix on top of PR #518 head `3061fcd9`), `src` tree
`ef305bd8a1be9917c4df43bfb8773aca9de4304e`. The compile and EditMode run used a fresh detached worktree with an empty
`Library` and an `Assets/Scripts` junction to its own `src`, as in `host-fbcc3c2f-20261029/`. `source-identity.txt`
records the hashes and an empty `git status --porcelain=v1 --untracked-files=all --ignored -- src Packages ProjectSettings`
before the compile and after the run. To check: `git rev-parse c4b65334:src` must print `ef305bd8…`.\
**Integrity:** `SHA256SUMS` covers every other file in this directory and is verified by
`tools/dotnet-ci/check_evidence_manifests.py`.

| File | What it is |
| --- | --- |
| `source-identity.txt` | Commit and tree hashes for the repo, `src`, `Packages`, `ProjectSettings` and `Assets`, the empty porcelain listing, the Unity version and the junction target. |
| `compile.txt` | Excerpt of the batch-mode Editor log for the full import: `Tundra build success` (1,150 of 1,203 items), 0 `error CS`, 0 references to shim build output, 75 `TacticalDirector.*` assemblies for 75 `src` asmdefs, the three known warnings, exit code 0. |
| `editmode-results.xml.xz` | NUnit 3.5 XML for the EditMode run of `TacticalDirector.ClientApp.Tests`, `TacticalDirector.UiFramework.Tests` and `TacticalDirector.MatchClientCore.Tests`: 424 passed, 0 failed, 0 skipped, 0 inconclusive. |
| `editmode-summary.txt` | Command, times, per-assembly counts (182 / 52 / 190) and the pre-run build line (0 items updated). |
| `captures/pitch-sizes-before.txt`, `captures/pitch-sizes-after.txt` | The pitch rectangle and selected speed logged at every step, before (`3061fcd9`) and after (`c4b65334`). |
| `captures/compare.tar` | `compare-1920x1080.png` and `compare-1366x768.png`: side-by-side sheets, before on the left and after on the right, at 40% scale, labelled with each step's measured pitch size. |
| `captures/shots-before.tar`, `captures/shots-after.tar` | The full-resolution PNG frames behind the sheets (14 each). |

All images are in plain `.tar` files because `*.png` is LFS-tracked and the evidence job checks out without LFS.
| `captures/S0SpeedCapture.cs.txt`, `captures/S0CaptureBuild.cs.txt`, `captures/capture_run.ps1.txt`, `captures/runs.txt` | Capture harness source, archived verbatim, and each run's commit and exit codes. |

**Measured.** In each player the demo match was started through the real buttons, Faster was stepped from 1× to 10×
and Slower back to 1×, and the frame was captured after each click.

| Build | Resolution | Pitch at 1× | Pitch at 3× and 5× | Pitch at 10× |
| --- | --- | --- | --- | --- |
| Before (`3061fcd9`) | 1920×1080 | 1054.6 × 683.0 | **1117.9 × 724.0** | 1054.6 × 683.0 |
| After (`c4b65334`) | 1920×1080 | 1054.6 × 683.0 | 1054.6 × 683.0 | 1054.6 × 683.0 |
| Before (`3061fcd9`) | 1366×768 | 542.0 × 351.0 | **605.3 × 392.0** | 542.0 × 351.0 |
| After (`c4b65334`) | 1366×768 | 542.0 × 351.0 | 542.0 × 351.0 | 542.0 × 351.0 |

Before, the pitch grew on the first Faster click and shrank again on the last, which is the owner's report. After, it
holds one size at every step in both directions. At 3× and 5× the reason line is now blank but keeps its space. The
owner compared the sheets remotely and replied "looks right now" on October 29.

**Capture method.** Release Windows x64 Mono players were built from a separate throwaway worktree with the harness
copied into an untracked `Assets/_Capture`; no tracked file was changed and nothing from it is committed except the
archived source. The project ships without Unity's ScreenCapture and ImageConversion modules, so the harness reads the
back buffer with core `Texture2D.ReadPixels` and writes BMP files, converted to PNG with Windows' System.Drawing.
1920×1080 is borderless full screen on the 1920×1080 host display; 1366×768 is windowed. A first "before" build failed
to compile because the harness used those absent modules; the recorded builds are the rerun.

**Warnings.** The same three as in `host-fbcc3c2f-20261029/`, none in the changed files.

**Not covered.** Text scale 200% and the narrow layout were not captured; the reserved height is measured from the
label's own width and text scale, which is the case those layouts exercise. Only the three test assemblies above were
run, not the full EditMode suite. The full Editor logs are not committed; licensing lines and the host's machine
identifier are left out of every excerpt.

## Version History

| Version | Date | Notes |
| --- | --- | --- |
| 1.0 | October 29, 2026 | Initial evidence set: full-import compile, ClientApp/UiFramework/MatchClientCore EditMode and before/after speed-step captures for the fixed-height playback reason at `c4b65334`. |
