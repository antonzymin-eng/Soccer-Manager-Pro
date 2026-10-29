# Gate J re-capture: B8/B10, I-Q15 and I-Q16 on the H v0.2 skin — pinned-host evidence

**Captured:** October 29, 2026, 00:36–03:00 UTC (October 28, 17:36–20:00 on the host's Pacific clock). Run-log timestamps are UTC.\
**Created:** October 29, 2026.\
**Purpose:** Raw evidence for [`p5b-real-client-acceptance.md`](../../p5b-real-client-acceptance.md) v0.10 §14: the three
Gate-J conditions that needed the PC to themselves — the B8/B10 render-loop re-capture on the new skin, one
uninterrupted I-Q15 keyboard pass, and the I-Q16 text-scale and resolution matrix after the October 28 visual changes.\
**Host:** the owner's Windows 11 machine (i5-9300H, GTX 1650), Unity 6000.4.9f1 (DX11, Mono). Certified determinism host.\
**Source:** PR #518 head `aa858e97` (`S0ScreensBehaviour` v1.11, `S0ScreenContent` v1.6, 151 roles), unmodified. The runs
used the detached worktree with an `Assets/Scripts` junction to its own `src`. The owner's open Editor and the
`SMP-p5b-host` checkout were not used.\
**Integrity:** `SHA256SUMS` covers every other file in this directory.

| File | What it is |
| --- | --- |
| `compile.txt` | Compile excerpts of both Editor sessions (0 `error CS`, only the known warnings outside the harness) and the three player builds. |
| `probe-log-iq15-iq16.txt` | The harness log: five I-Q16 matrix passes (100%, 150%, 200%, 200% pseudo, 200% long names), then the I-Q15 keyboard pass. |
| `layout-summary.txt` | One line per I-Q16 pass, generated from the log: samples, clipping, reachability, pitch labels, layout failures, aspect, page fit. |
| `shots-iq15-1.tar`, `shots-iq15-2.tar` | The seven I-Q15 captures (menu focus, setup arrows, Match View Tab order, dialog focus, substitution dialog, statistics and history, pitch inspection). Plain `.tar` because `*.png` is LFS-tracked; split to stay under the 1 MiB binary guard. |
| `shots-iq16.tar` | Four I-Q16 samples: Compare open at 1366×768/150%, the substitution dialog at 1920×1080/200% with long names, Match View with statistics and history at 1920×1080/200% pseudo, and the narrow 800×1000/200% pitch. |
| `perf/capture.log` | One summary line per B8/B10 condition (`-final`): frames, FPS, p50/p95/p99/max, frames over 16.7 ms, sim ticks/s, per-marker cadence. Its first line is the void attempt described below. |
| `perf/frame-csv-final.tar.xz` | The per-frame CSVs behind the eight summaries. |
| `perf/end-states-s100.tar`, `perf/end-states-s200.tar` | The player's screen at the end of each condition's hold, showing the speed and statistics state. |
| `probe-source/pcond.ps1.txt` | The per-condition driver, v2 (see Method). Every other harness file is identical to its copy in `../p5b-gate-j-fixes-20261021/probe-source/` or `../p5b-real-client-c95e27ba-20261019/probe-source/` apart from the output folder. |

The player `.raw` profiles (6.8 GB), the player logs and the development builds stay outside the repository.

## Results

**I-Q16 — PASS.** 95 layout samples across the five passes at 1366×768, 1920×1080 and 2560×1440 plus the narrow 800×1000
case: 0 clipping problems, 0 unreachable actions, 0 pitch-label overlaps or clipped labels, `PitchLayoutFailureCount` 0,
every pitch 105:68, no sample void (the Game view applied every requested size). The live Match View fits without
scrolling at 1920 and 2560 at 100% and 150%, and at 2560 at 200%; it scrolls at 1366 and at 1920/200%, with every action
still reachable. The pseudo pass loads the pseudo catalogue (`5adcfb…35622b`); the others load the canonical 151-role
hash `836f9e…20ef29`.

**I-Q15 — PASS, uninterrupted.** 37/37 checks (3 boot, 34 keyboard) in one run with the Editor in the foreground
throughout and no aborted key. Match View Tab order: Pause → Faster → Change Mentality → Make substitution →
"▸ Match statistics" → pitch entry (H1) → Pause. The modal Tab cycle now includes "✕ Close" and stays inside the dialog.
As in October, the earlier-feedback disclosure was not reached by keyboard: the pass makes three requests and the
disclosure needs four.

**B8/B10 — PASS.** Windows x64 Mono development player, 1920×1080 window, each condition one player session driven by
keyboard only, held 30 s, the last 1,989 frames before the 10 quit frames measured from the player's own profiler log.
Main-thread CPU frame time in milliseconds; budget p99 ≤ 16.7 ms.

| Text | Condition | mean FPS | p50 | p99 | max | > 16.7 ms | sim ticks/s |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 100% | 1×, stats closed | 260 | 3.79 | 5.48 | 7.93 | 0 | 60 |
| 100% | 1×, stats open | 245 | 3.92 | 6.05 | 9.24 | 0 | 60 |
| 100% | 10×, stats open | 205 | 4.55 | 10.41 | 12.74 | 0 | 565 |
| 100% | 10×, stats closed | 220 | 4.41 | 6.65 | 9.48 | 0 | 551 |
| 200% | 1×, stats closed | 296 | 3.29 | 4.79 | 6.01 | 0 | 60 |
| 200% | 1×, stats open | 272 | 3.56 | 5.38 | 8.05 | 0 | 60 |
| 200% | 10×, stats open | 229 | 4.03 | 9.05 | 11.63 | 0 | 586 |
| 200% | 10×, stats closed | 244 | 3.93 | 5.78 | 12.35 | 0 | 579 |

Every condition is inside the budget with no frame over 16.7 ms; the worst p99 is 10.41 ms (100%, 10×, statistics open).
The end-state screenshots show each condition's speed and statistics state.

**Observation, not a J-05 result.** The 10× rung delivered 551–586 sim ticks/s here, against 315–340 in the October 19
capture on the same host. This branch does not contain PR #520's throughput work, and the cause was not investigated.
J-05 asks for 600 ticks/s and stays open.

## Method and void attempts

- **Driver.** `pcond.ps1` launches the player with `-profiler-enable -profiler-log-file`, starts a match by keyboard,
  sends the condition's keys, screenshots, holds 30 s, and quits. Keys go only to the player while it owns the foreground;
  otherwise the script aborts without sending. Keys after kick-off: none (1×, closed); Tab ×5, Enter (1×, open);
  Tab ×5, Enter, Shift-Tab ×3, Enter ×3 (10×, open); Tab ×2, Enter ×3 (10×, closed).
- **v2 changes.** (1) The development player follows the connected Editor's recording state, so the script now asks the
  Editor to attach and record (`S0HostPerf.Start` through the command channel) before the keys; without it the player's
  log stops after about a dozen frames. (2) The player is always stopped in a `finally` block, and the hold aborts if the
  profile passes 4 GB.
- **Firewall.** The first launch of the new build raised a Windows Firewall prompt. The guard sent no key while the prompt
  had focus. The owner answered it; the 200% player was then rebuilt into the same folder so no second prompt appeared.
- **Void attempts, not used.** (a) A v1 trial aborted at the firewall prompt and left the player running with profiling
  on; its log reached 64 GB and filled the disk before it was stopped and deleted. (b) The first 100% 1×-closed capture
  recorded with the Editor not recording: 2 measurable frames (`perf/capture.log` line 1). It was re-captured with v2.
  (c) The 00:43 200% build in `s0-aa858e97-s200` was never launched.
- **Editor restart.** After the disk-full episode the first Editor session stopped polling its command channel while
  staying responsive. It was stopped and restarted (its scene-backup recovery prompt was answered No: the backup was the
  harness's re-saved scene). The 200% build and all eight analyses ran in the second session.

## Version History

| Version | Date | Notes |
| --- | --- | --- |
| 1.0 | October 29, 2026 | Initial evidence set: B8/B10 re-capture on the H v0.2 skin, one uninterrupted I-Q15 pass, and the I-Q16 matrix after the October 28 visual changes, at PR #518 head `aa858e97`. |
