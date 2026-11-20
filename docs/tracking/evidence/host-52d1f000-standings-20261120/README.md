# Pinned-host Unity run — #554 Career Home scores and P7b checklist items 6–8 at 52d1f000

**Captured:** November 20, 2026, 14:27–15:45 UTC (06:27–07:45 on the host's Pacific clock)\
**Purpose:** Two open pinned-host items on `main` at `52d1f000`:
(1) compile and EditMode for [PR #554](https://github.com/antonzymin-eng/Soccer-Manager-Pro/pull/554)
(latest-round scores and goal difference on Career Home), merged without a Unity run; and
(2) the remaining [P7b career startup checklist](../../../../src/match-client-unity/README.md#p7b-career-binding)
items 6, 7 and 8 left open by the [`cd3c7b99` run](../host-cd3c7b99-20261119/README.md), tracked in
[open issues](../../open-issues.md#career-startup-unity-acceptance). The run found a keyboard-reach gap in the
standings; the same folder records the paging fix the owner chose and its re-run.\
**Host:** the owner's Windows 11 machine, Unity 6000.4.9f1 (f7258d6eebbe), Mono. This is the certified host.
Its only display is 1920×1080.\
**Integrity:** `SHA256SUMS` covers every other file in this directory and is verified by
`tools/dotnet-ci/check_evidence_manifests.py`. The PNG screenshots are stored through Git LFS.

| File | What it is |
| --- | --- |
| `source-identity.txt` | Commit and tree hashes, and the five development players with their scene settings. |
| `compile.txt` | Clean batch-mode import: 0 `error CS`, 75 assemblies for 75 asmdefs. |
| `editmode-results.xml.xz`, `editmode-summary.txt` | ClientApp.Tests and UiFramework.Tests: 346 passed, 0 failed, 3 skipped. |
| `career-rounds-resume-cycles.log` | Item 6: three rounds, scores and standings checked against each other, save failures, Continue, cycles. |
| `harness-rounds.log` | Player launches and exits, save-directory hashes, and the slot damage applied between processes. |
| `keys-100.log`, `keys-200.log`, `keys-200-long-expanded.log` | Item 7: every key press with the focused control, ring and viewport state; layout audits. |
| `pm1-career-on.log`, `pm1-career-off.log`, `pm1-texts-career-*.txt` | Item 8: the demo journey and every visible text on each screen. |
| `editmode-paged-results.xml.xz`, `editmode-paged-summary.txt` | The same suites with the standings paging change: 350 passed, 0 failed, 3 skipped. |
| `keys-paged-100.log`, `keys-paged-200.log`, `keys-paged-200-long-expanded.log` | The keyboard runs repeated on the paged standings. |
| `item7-paged-*.png` | Standings page 2 with focus on *Lower positions*, 1366×768. |
| `player-log-summary.txt` | Exception counts in the player logs, and one engine diagnostic from the discarded first attempt. |
| `item6-*.png`, `item7-*.png`, `item8-*.png` | Screenshots (1366×768 unless named 1920). |
| `career-accept-probe.cs.txt`, `career-accept-probe-paged.cs.txt`, `build-probe-player.cs.txt`, `run-accept.ps1.txt`, `long-copy-patch.py.txt` | Scratch driver, build script, launcher and the long-name substitution. None were committed. |

## Compile and EditMode (#554)

A fresh sparse worktree with an empty `Library` compiled in 92 s: Tundra built 1,150 of 1,203 items with
0 `error CS` and the same three warnings as earlier runs. EditMode ran 349 tests: 346 passed, 0 failed,
3 skipped (the existing opt-in diagnostics). That includes `CareerScreensTests` 18/18 and
`CareerScreenTextTests` 13/13 (#554 added five and two cases), and `ClientCareerCoordinatorTests` 47/47.

## Item 6 — scores, standings, saves and cycles

Player A (Career on, 100%, 1366×768) drove the real buttons. The probe parsed the score block after
every round and kept running totals for each club. It then checked every standings row against those
totals: P, W, D, L, GF, GA, GD, points and position.

| Check | Result |
| --- | --- |
| Score block on the first frame after progress finishes, rounds 1, 2 and 3 | PASS: "Round N results" with all 10 fixtures, each of the 20 clubs once |
| Every standings row matches the accumulated scores (P W D L GF GA GD Pts, position) after rounds 1, 2 and 3 | PASS, 0 mismatches; order follows points, then GD, then GF |
| Negative goal difference | 9, 11 and 10 clubs after rounds 1–3, all shown correctly, for example `-3` |
| All controls disabled while a round runs | PASS (0 interactable) |
| Autosave failure: autosave file held open exclusively during round 2 | Round completes. Career Home shows "Autosave failed: The process cannot access the file because it is being used by another process." The previous autosave is unchanged byte for byte. |
| Manual save to a locked slot, then retry after release | Failure shown; the retry succeeds and clears the manual-save error |
| Quit to Main Menu, Continue (same process) | Standings restored exactly; the score block is hidden until the next round |
| Damaged and incompatible slots (save file removed / sidecar `format=999`, applied between processes) | Picker: Autosave Ready, slot 1 Ready, slot 2 "Damaged or unreadable - cannot load", slot 3 "Incompatible with this build - cannot load"; both disabled |
| Continue in a new process | Standings restored exactly; no score block until round 3; round 3 scores and the table then match the totals from all three rounds |
| Three new / quit / Load (picker) / Back / Continue cycles in one process | Each Continue opens the career just created; no error notice |
| Two relaunches of the player, Continue each time | Opens the newest career; no error notice |

**Observed, not a failure:** the autosave failure notice stays on Career Home after later manual saves
succeed, until an autosave succeeds or the session ends. The notice precedence in
`S0ScreensBehaviour.Career.cs` makes that the intended reading: the newest autosave is still the old one.

## Item 7 — keyboard, focus, layouts, text scale, long names

Players E (100%), B (200%) and C (200% with long names and expanded copy) were driven only by real Tab,
Shift+Tab and Enter key messages posted to their own windows. Shift is now detected: posting `VK_LSHIFT`
reaches `Input.GetKey(KeyCode.LeftShift)`, which closes the limitation recorded in the #551 run.
Player C is a harness substitution, as in the I-Q16 matrix. Every club name gains
" Metropolitan & District Athletic Association" (up to 62 characters). Every `ui.career.*` pattern is
expanded by 40% in the form `[text ~~~]`.

| Check | 100% | 200% | 200% long + expanded |
| --- | --- | --- | --- |
| Tab and Shift+Tab cycles wrap; Shift+Tab is the exact reverse | PASS on Main Menu (3 stops), New Career (21), Career Home (6, before and after a round), Load Career (2) | Same | Same |
| Disabled controls skipped (Continue with no save, Start before a club, empty slots) | PASS | PASS | PASS |
| Focus ring drawn and inside the viewport at every stop | PASS | PASS | PASS |
| Back paths: New Career → Back, Load Career → Back, Career Home → Return to Main Menu | Focus lands on the Main Menu heading | Same | Same |
| Focus during a round / after it | Heading while busy; returns to *Advance one day* when progress was started from it | Same | Same |
| Successful save | Focus returns to the save control | Same | Same |
| Failed save (slot file locked) | Notice shown under the heading and in view; focus moves to the heading | Same | Same |
| Standings: ten columns aligned with their headers, no overlap, nothing outside the viewport (1366×768 and 1920×1080) | PASS | PASS | PASS |
| Text clipped or overflowing anywhere on Career Home | 0 | 0 | 0 |
| Header and number cells on one line | PASS | PASS | 1920: PASS. 1366: the seven expanded short headers (`[Pts ~~]`) wrap to two lines, but stay aligned and unclipped |
| Long club names | — | — | Wrap to two or three lines in the club column with no overlap |
| Every score-block line fully on screen at some Tab stop | 11 of 11 | 11 of 11 | 11 of 11 |

**Finding: the standings cannot be read by keyboard alone.** The table is read-only text after the
last Tab stop (*Return to Main Menu*). No key scrolls past that control. Counting rows fully on screen
at any stop of a full Tab cycle, out of 21 (header plus 20 clubs):

| Window | 100% | 200% | 200% long names |
| --- | --- | --- | --- |
| 1366×768 | 2 | 0 | 0 |
| 1920×1080 | 15 | 0 | 0 |

With the mouse wheel (scrolled to the bottom), all 21 rows show at 100%. At 200%, 17 of 21 show at
1366×768 and all 21 at 1920×1080. In the long-name build the table is taller than the window, so 8 and
12 rows show at the bottom and the rest further up. This is the same class of gap the #551 run found for
the fixture list, which was fixed by paging. The owner chose the same fix here (below).

**Not tested:** 2560×1440. The host's only display is 1920×1080, and the windowed player could not be
made larger, so the high-resolution layout remains open. Because the screens use a constant-pixel-size
canvas, 2560×1440 gives every column more room than the two sizes tested.

## Fix: paged standings, re-run

The owner chose the #551 fix. `CareerStandingsPage` shows `ROWS_PER_PAGE` (8) table rows under a heading
such as *Positions 9 to 16 of 20*. The column headers stay on every page. *Higher positions*,
*Your position* and *Lower positions* controls follow the rows, and *Your position* returns to the page
holding the managed club, which is also the default. The controls sit **after** the rows because they are
the last Tab stops on Career Home, and `Reveal` scrolls only far enough to show the focused control. A
disabled end control moves focus to *Your position*, the rule the fixture and calendar paging share.

The candidate (`src` tree `a976f67b`) compiled with 0 `error CS`. EditMode passed 350 of 353 tests with
0 failures, including four new tests: page coverage and bounds, the managed-club page, and the heading
and labels. Players E, B and C were rebuilt from it. On every page, the probe ran a full Tab cycle and
counted the page heading and each row (column headers plus clubs) only when fully inside the viewport.

| Pages fully readable by keyboard (heading + all rows at some Tab stop) | 1366×768 | 1920×1080 |
| --- | --- | --- |
| 100% | 3 of 3 | 3 of 3 |
| 200% | **3 of 3** (before: 0 of 21 rows) | **3 of 3** (before: 0 of 21 rows) |
| 200%, long names + expanded copy | **1 of 3**: pages 1 and 2 show 7 of 9 rows and miss the heading | 3 of 3 |

First and last pages: the disabled end control hands focus to *Your position* at 200% (player B). In
players E and C the managed club was on page 1, so *Higher positions* was already disabled and skipped by
Tab. The probe logged that as a failure to reach it, which is a probe expectation, not a product fault.
Alignment, clipping and the score-block checks are unchanged from the first run. The player logs show
0 exceptions.

**Residual:** with the harness's 62-character club names and 40% expanded copy at 1366×768 and 200%,
most rows wrap to two or three lines. An eight-row page plus its three controls is then taller than the
window, so the heading and the top rows of a full page never fit beside a focused control. Shipped names
pass. The #551 fixture list has the same page size and was never tested with long names.

## Item 8 — PM-1 journey with Career on and off

Players E (Career on) and D (Career off) each played the demo through: Main Menu → *Play a demo match* →
Tactics Setup → *Start match* → Match View at the fastest speed to full time (540 s) → *View match report*
→ *Return to main menu* → a second demo to Tactics Setup → *Back*. All four checks passed in both.

Every visible text on Tactics Setup, Match View at kick-off, Match View at full time and the Report is
identical between the two runs (`pm1-texts-career-on.txt` and `-off.txt`, 15, 46, 48 and 44 lines). The
only difference on the Main Menu is the three career buttons (*New career*, *Continue career*,
*Load career*), before and after the match. The Report returns to the Main Menu in both. No Career Home →
Match or Report → Career Home edge exists (D-P7-6 remains PROPOSED).

## Probe corrections (first attempt discarded)

The first attempt ran on the same players and source, and its logs were discarded for four probe
defects, not product ones:

1. The score parser kept a trailing `\r`. `CareerScreenText` joins score lines with `AppendLine`, which
   is CRLF on Windows, so club names did not match the table.
2. The clipping audit read `cachedTextGenerator`. That is stale for text culled by the scroll mask, so
   it reported dropped glyphs that the screenshots show are drawn.
3. The Tab cycles started from the page heading, which is not a Tab stop.
4. The PM-1 text dump looked for a canvas without a parent; the shell's canvas has one.

The corrected probe uses a fresh `TextGenerator` per text and a 3 px glyph slack. Its runs are the ones
recorded here. The first attempt's player logs had one development-build engine diagnostic, described in
`player-log-summary.txt`.

## Method

Compile, EditMode and the five development players ran from the command line in a sparse worktree of
`52d1f000`; the owner's open editor was not used. Each player had its own product name, and so its own
save directory under `%USERPROFILE%\AppData\LocalLow\DefaultCompany\`, which let rounds run concurrently.
Round times (324–582 s) reflect up to four players sharing the CPU and are not performance evidence.
Scene settings, the probe and the build script are recorded in `source-identity.txt`; none were committed.

## Version History

| Version | Date | Notes |
| --- | --- | --- |
| 1.0 | 2026-11-20 | Created: #554 compile and EditMode; P7b checklist items 6, 7 and 8 on the pinned host. |
| 1.1 | 2026-11-20 | Standings paging fix chosen by the owner: compile, EditMode and keyboard re-run at 100%, 200% and 200% long names. |
