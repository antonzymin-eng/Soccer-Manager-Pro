# Pinned-host career startup checks 6–8 at `52d1f000`, and the switch to Career on

**Captured:** November 20, 2026, 02:19–03:43 UTC (November 19, 18:19–19:43 on the host's Pacific clock).\
**Created:** November 20, 2026.\
**Purpose:** The remaining items of the [P7b binding checklist](../../../../src/match-client-unity/README.md#p7b-career-binding)
for the [career startup open issue](../../open-issues-resolved.md#career-startup-unity-acceptance). Items 1–5 passed in the
[`cd3c7b99` run](../host-cd3c7b99-20261119/README.md) on November 19. This run covers item 6 (two consecutive matchdays,
autosave failure and recovery, manual-save retry, repeated new/load/quit cycles), item 7 (keyboard, layouts, 200% text,
long names) and item 8 (the PM-1 journey with Career on and off). With every item passed, the tracked scene now
enables Career at startup.\
**Host:** the owner's Windows 11 laptop, Unity 6000.4.9f1 (f7258d6eebbe), Mono, a 1920×1080 panel at 96 DPI. This is the
certified host.\
**Source:** `main` at `52d1f0003012a3be60e9f5345610451632577eee` (PR #555 merge), `src` tree `4db2b694…`. The change
committed with this folder only sets `_enableCareer: 1` in `Assets/Scenes/Scene.unity`; build variant A below is that scene.\
**Integrity:** `SHA256SUMS` covers every other file in this directory and is verified by
`tools/dotnet-ci/check_evidence_manifests.py`. Screenshots are PNGs inside plain `.tar` files under 1 MiB, because
`*.png` is LFS-tracked and the evidence job checks out without LFS.

## Result

| Check | Result |
| --- | --- |
| Compile, fresh worktree, empty `Library` | `Tundra build success` (1,150 of 1,203 items), **0 `error CS`**, 75 `TacticalDirector.*` assemblies for 75 `src` asmdefs; only the three known warnings |
| Item 6, build A | **108 passed / 0 failed**, 0 console errors |
| Item 7 at 100% text, build A | **233 passed / 0 failed**, 0 console errors, 178 real keystrokes |
| Item 7 at 200% text, build B | **237 passed / 0 failed**, 0 console errors, 185 real keystrokes |
| Item 8, Career on, build A | **14 passed / 0 failed**, 0 console errors |
| Item 8, Career off, build C | **11 passed / 0 failed**, 0 console errors |

Each check is a `PASS`/`FAIL` line in the run's probe log. A console error or exception logged anywhere in the player
would have been counted in the run's `SUMMARY` line; there were none, and no player log contains "Exception".

## Item 6 — matchdays, saves and cycles

A new career (Ashford Town) was started, saved to manual slot 1, and played through four matchdays in the player
(about 3.5 minutes per round):

- **Scores and table.** After each round the score block showed all ten fixtures of that round under
  "Round N results" on the first frame after the work finished. Every one of the 200 table cells matched the table
  model. Every row satisfied P = W + D + L, GD = GF − GA and Pts = 3W + D, the rows were in tie-break order, and
  negative goal differences were shown with their sign. Through round 4, every club's row equalled the totals of the
  displayed scores of all rounds so far.
- **Autosave failure.** During round 2 the driver held `autosave.tdsave` open with `FileShare.None`. Career Home then
  showed "Autosave failed: The process cannot access the file because it is being used by another process." The
  autosave pair still held round 1 byte for byte, and no temporary or backup file was left.
- **Manual-save failure and retry.** With `manual-1.tdsave` held, Save to manual slot 1 showed the error and left the
  slot unchanged. After the file was released, the same button wrote the slot (round 2, sequence 4). The coordinator
  error cleared, and the notice fell back to the still-outstanding autosave failure.
- **Recovery.** Round 3's autosave succeeded, the failure notice cleared, and the autosave held round 3.
- **Continue.** Return to Main Menu, then Continue, restored all 20 standings rows exactly. The score block stays hidden
  after a load until the next round. After round 4 it showed all ten round-4 scores.
- **Cycles.** Five times: New career with a different club, quit, Load the autosave, quit, Load manual slot 1
  (the round-2 Ashford Town save), quit, Continue. Every load reached Career Home with the expected club and round,
  Continue always resumed the newest save, and no error notice appeared.

Damaged and Incompatible slots were covered by the `cd3c7b99` run and were not repeated.

## Item 7 — layouts, text scale, long names and keyboard

**Layouts.** Each run audited Main Menu, New Career (with a club selected), Load Career and Career Home (schedule
closed, fixture list, calendar) at 1366×768, 1920×1080 and 2560×1440. A real 2560×1440 window cannot open on the
1920×1080 panel. That size is emulated: the shell's ConstantPixelSize canvas gets scale factor 0.75, so its logical
size is 2560×1440 and it is drawn downscaled. Each audit checked these four points:

- No text is clipped, truncated, best-fit or missing a glyph.
- Every control is on screen horizontally and inside a scroll view vertically.
- All ten standings columns align with their header cells to within 0.5 px, and no cell is clipped or overlaps its
  neighbour.
- The score block is fully laid out and can be scrolled into view.

The menu pages were audited before the layout round, so that leaving Career Home did not clear the score block.

**Long names.** The generated league has no long club names, so the driver injected them into the rendered text.
Every club name gained the 35-character suffix " Athletic Football Association Club" (in the table, scores, club
buttons and summary), and every other career and menu label grew by about 40%. The layout was then rebuilt and the same audit
repeated, after which the original text was restored. At 1366×768 and 200% the club column is 451 px. Long names wrap
onto two lines with the numeric columns still aligned, and the score block is taller than the viewport but complete
when scrolled.

**Keyboard.** Real Windows keystrokes (`keybd_event`) went only to the player's own window. The driver brings that window
to the foreground and refuses to send a key otherwise. The keyboard pass ran at 1920×1080 at 100% and at 1366×768 at
200%. On Main Menu, New Career (before and after choosing a club), Load Career, Career Home and Career Home with the
calendar open, the checks were:

- Tab visits every enabled control exactly once and wraps.
- Tab never lands on a disabled control, and a focus ring is shown on every stop.
- Shift-Tab reverses the order.
- Disabled Start is not a tab stop until a club is chosen.
- Enter opens each page with its heading focused, and both Back paths and Return to Main Menu land on the Main Menu
  heading.
- Focus returns to Save to manual slot 2 after a successful save.
- A failed save by keyboard shows the error, with focus on the page heading directly above it.
- After a day advance, focus returns to the progress control.
- Paging the calendar back to the first week disables Previous week and moves focus to Current week with its ring.
  Shift-Tab from there skips the disabled control.

## Item 8 — PM-1 journey with Career on and off

In both builds the journey was:

1. Play a demo match.
2. In Tactics Setup, choose Positive.
3. Start match and go to 10×.
4. Change Mentality to Very Attacking (Applied).
5. Make a substitution, shirt 6 for bench slot 2 (Applied).
6. Play to full time, then View match report.
7. Check the report: it has statistics and no Career control.
8. Return to main menu, which clears the match.

The loaded S0 content hash was `836f9e79…20ef29` in both builds before and after the journey. That is the canonical
hash recorded in the October I-Q15/I-Q16 runs.

- **Career on:** the Main Menu offers the demo next to New, Continue and Load Career. Career Home has no
  Match, Tactics Setup or report control. After the demo, Continue showed the same career summary and table as before it.
- **Career off:** no career action exists, and the journey is otherwise the same.

No Career Home → Match or Report → Career Home edge exists. The D-P7-6 contract is unchanged.

**Observation outside this change.** The two demo matches ended 19–11 and 16–15 with the home side on Positive, then
Very Attacking. These are match-engine realism figures from a deliberately attacking setup, not career behaviour. They
are recorded here and not investigated.

## Method and limits

- Three Win64 players were built in batch mode from a fresh detached worktree (`build.sh.txt`):
  - A: Career on, text scale 1.0. This is the committed scene.
  - B: Career on, text scale 2.0.
  - C: Career off. This is `main` before this change.
- The scratch driver (`career-probe-driver.cs.txt`) runs only with `-careerProbe`. It clicks the real UGUI buttons
  (`Button.onClick`), sends real keystrokes and reads state. Screenshots come from frame-buffer readback, because this
  project includes no screen-capture or image module. It was never committed.
- Saves lived in the player's `persistentDataPath/careers`, which was empty before the first run. Item 6 started from an
  empty folder. Item 7 and item 8 continued the career it left.
- The failure cases were provoked by holding the save file open from inside the player, a sharing violation that
  `File.Replace` reports as an `IOException`. A full disk or a read-only folder was not tried.
- Item 7's high-resolution layout is emulated, as described above, not a physical 2560×1440 display. Long names are
  injected text, not a generated league. No screen reader was tested.
- Not run: the Linux dotnet gate (no `src/` code changed) and the EditMode suites (covered at `cd3c7b99`; `src` has
  changed since only in Career Home results and paging, whose tests ran in their own PRs).
- Superseded attempts are listed at the end of `runs.txt`. Each failure there came from the driver, not the game, and
  was rerun.

| File | What it is |
| --- | --- |
| `source-identity.txt` | Commit and tree hashes, worktree setup, the untracked driver, build variants and times |
| `compile.txt`, `compile-editor-log.txt.xz` | Clean import receipt and the full redacted Editor log |
| `runs.txt` | Every governing run's build, times, exit and summary; superseded attempts and why |
| `item6-probe-log.txt` | Item 6 run log: clicks, notices, save hashes, score blocks and tables |
| `layout-100-probe-log.txt`, `layout-200-probe-log.txt` | Item 7 audits and the keyboard pass at 100% and 200% |
| `pm1-career-on-probe-log.txt`, `pm1-career-off-probe-log.txt` | Item 8 journeys |
| `shots-1.tar` … `shots-6.tar`, `shots-index.txt` | Selected screenshots (item 6, the 1366×768 and emulated 2560×1440 layouts, keyboard, PM-1) |
| `career-probe-driver.cs.txt`, `build.sh.txt` | The driver and the build script, verbatim |

## Version History

| Version | Date | Notes |
| --- | --- | --- |
| 1.0 | 2026-11-20 | Created: pinned-host career startup checks 6–8 at `52d1f000`; Career on by default. |
