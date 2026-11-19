# Pinned-host Unity run — Career Home schedule (#551) and fixture-list paging

**Captured:** November 19, 2026, 22:03–23:05 UTC (14:03–15:05 on the host's Pacific clock)\
**Purpose:** The PM-2 checks [PR #551](https://github.com/antonzymin-eng/Soccer-Manager-Pro/pull/551) left open:
Unity 6000.4.9f1 compile, Play, and the schedule UX acceptance in
[`ux-detailed-plan.md` §Schedule candidate](../../../design/ux-detailed-plan.md). Squad screens are unbuilt
and were not tested. The run found a 200% keyboard-reach gap; the same folder records the paging fix that
the owner chose and its re-run.\
**Host:** the owner's Windows 11 machine, Unity 6000.4.9f1 (f7258d6eebbe), Mono. This is the certified host.\
**Integrity:** `SHA256SUMS` covers every other file in this directory and is verified by
`tools/dotnet-ci/check_evidence_manifests.py`. The PNG screenshots are stored through Git LFS.

| File | What it is |
| --- | --- |
| `source-identity.txt` | Commit and tree hashes. The tested `ad8d4f80` has the same `src` and `Assets` as the #551 merge `62b60429`. |
| `compile.txt` | Clean batch-mode import: 0 `error CS`, 75 assemblies for 75 asmdefs. |
| `editmode-results.xml.xz`, `editmode-summary.txt` | ClientApp.Tests and UiFramework.Tests: 339 passed, 0 failed, 3 skipped. |
| `schedule-probe-1x.log`, `schedule-probe-2x.log` | Every key press, the focused control, its focus ring and which schedule rows were on screen. |
| `player-1x-summary.txt`, `player-2x-summary.txt` | Exception counts in the player logs (0 and 0). |
| `1x-*.png`, `2x-*.png` | Screenshots at 1280×720, 100% and 200% text scale. |
| `editmode-paged-results.xml.xz` | The same suites with the paging change: 343 passed, 0 failed, 3 skipped. |
| `schedule-probe-paged-1x.log`, `schedule-probe-paged-2x.log`, `player-paged-*-summary.txt` | The keyboard task re-run on the paged list. |
| `*-paged-*.png` | Screenshots of the paged list. |
| `schedule-probe-before.cs.txt`, `schedule-probe-paged.cs.txt`, `build-probe-player.cs.txt` | The scratch drivers for both runs and the development-build script. None were committed. |

## Compile and EditMode

A fresh worktree with an empty `Library` compiled in 75 s with 0 `error CS` and the same three warnings as
earlier runs. EditMode passed 339 of 342 tests with 0 failures, including `CareerScreensTests` 13/13,
`CareerScreenTextTests` 11/11 and `ClientCareerCoordinatorTests` 47/47. The 3 skips are the existing opt-in
diagnostics.

## Play: the keyboard-only schedule task

Two development players with Career enabled ran at 1280×720, one at 100% text scale and one at 200%. Each
started a new career with clicks, then did the UX plan's task using only real key messages posted to its own
window: Tab and Enter, through the legacy Input Manager the screens read. After every key the probe logged
the focused control, whether its focus ring was drawn and in view, and which schedule rows were inside the
scroll viewport.

| Check | 100% | 200% |
| --- | --- | --- |
| Next opponent, date and venue | Visible on the Career Home summary and the first fixture row | Same |
| Tab order | Advance one day → Schedule → Fixtures → Calendar → week controls (calendar only) → Save slots 1–3 → Return to Main Menu → wraps | Same |
| Focus ring drawn and in view at every stop | Yes, every one of 76 keys | Yes, every one of 76 keys |
| Fixture rows ever on screen during a full Tab cycle | **38 of 38** | **23 of 38** |
| Calendar week rows on screen during the cycle | 8 of 8 | 8 of 8 (rows 6–8 only after Tab moves past Next week) |
| Browse a later week and return to the current week | Works | Works |
| First week: Previous week disabled and skipped by Tab | Yes | Yes |
| Last week (38 pages): focus after Next week disables | Moves to Current week | Moves to Current week |
| Exceptions in the player log | 0 | 0 |

**The 200% fixture list is not fully readable by keyboard.** The list is one read-only text block of 38
rows between Calendar and *Save to manual slot 1*. With focus on Calendar the viewport shows rows 1–10. One
Tab moves focus to the save slot, and scrolling it into view jumps to rows 26–38. Rows 11–25 (days 77–175 in
this career) never appear on screen without a mouse; nothing in the screens scrolls the viewport from the
keyboard. At 100% the two views overlap, so all 38 rows are reachable. The calendar is less affected: after
paging, the week's last three rows are below the fold until the next Tab, which shows all eight.

This is the evidence for the UX plan's open question: whether finding and reading fixtures needs
individually focusable rows. As built, a single block is enough at 100% but not at 200%.

## Fix: paged fixture list, re-run

The owner chose paging over focusable rows. The list now shows `CareerFixturePage.FIXTURES_PER_PAGE` (8)
fixtures under a heading such as *Fixtures 9 to 16 of 38*, with *Earlier fixtures*, *Upcoming fixtures* and
*Later fixtures* controls in the place the week controls take for the calendar. *Upcoming fixtures* returns to
the page holding the next unplayed fixture. A disabled end control moves focus to *Upcoming fixtures*, the
same rule #551 applies to the calendar. Rows stay read-only text: two to three Tab stops instead of 38.

The paging candidate's `src` tree (`ce194427`, recorded in `source-identity.txt`) passed ClientApp.Tests and
UiFramework.Tests with 343 passed, 0 failed and 3 skipped, including four new paging tests. Both players were
rebuilt from it and the probe re-ran the keyboard task. On every page it ran a full Tab cycle from *Later
fixtures* and recorded every schedule line that came on screen.

| Check | 100% | 200% |
| --- | --- | --- |
| Pages | 5 (8, 8, 8, 8, 6 fixtures) | 5 |
| Pages where every line (heading + rows) came on screen during the Tab cycle | **5 of 5** | **5 of 5** (was 23 of 38 rows unpaged) |
| Last page: *Later fixtures* disabled, focus | Moves to *Upcoming fixtures* | Moves to *Upcoming fixtures* |
| *Upcoming fixtures* returns to page 1; *Earlier fixtures* disabled there | Yes | Yes |
| Calendar checks from the first run | Unchanged | Unchanged |
| Exceptions in the player log | 0 | 0 |

**Not verified:** Shift+Tab. Windows does not update the modifier state for posted key messages, and setting
the thread's keyboard state did not reach Unity's input, so every simulated Shift+Tab moved forward. That is
a probe limitation, not a product result. Long club names and window sizes other than 1280×720 were not
tested.

## Method

Compile and EditMode ran from the command line in a sparse worktree of `ad8d4f80`. The two development
players were built from the same worktree with Career enabled and the text scale set in the scene. None of
those changes were committed. The owner's open editor was not used. Probe saves went to the normal
persistent-data path, which did not exist before the run, and were moved out afterwards.

## Version History

| Version | Date | Notes |
| --- | --- | --- |
| 1.0 | 2026-11-19 | Created: #551 schedule compile, EditMode and keyboard-only Play checks at 100% and 200% text scale, and the re-run with the paged fixture list. |
