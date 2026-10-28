# J-04 owner review: statistics rail fix — pinned-host evidence

**Captured:** October 28, 2026, 19:24–19:29 UTC (v1.0) and 19:55–20:17 UTC (v1.2) and 21:44–22:15 UTC (v1.3); 12:24–15:15 on the host's Pacific clock\
**Purpose:** Raw evidence for [`p5b-real-client-acceptance.md`](../../p5b-real-client-acceptance.md) v0.5 §9, v0.6 §10 and v0.7 §11. While
reviewing the J-04 screenshots, the owner asked for two points to be fixed before accepting the visuals: at 1920×1080
the open statistics table ran off the bottom of the screen, and "Match statistics" appeared twice in the statistics panel.\
**Host:** the owner's Windows 11 machine, Unity 6000.4.9f1 (f7258d6eebbe) editor (DX11, Mono). Certified determinism host.\
**Source:** PR #518 head `239eae85` plus the change to `src/match-client-unity/S0ScreensBehaviour.cs` (v1.9) that lands
with this directory. The run used a detached worktree with an `Assets/Scripts` junction to its own `src`. The owner's open
Editor and the `SMP-p5b-host` checkout were not used.\
**Integrity:** `SHA256SUMS` covers every other file in this directory.

| File | What it is |
| --- | --- |
| `compile.txt` | Batch-mode compile of the changed tree before the probe harness was added: `Tundra build success`, 0 `error CS`, the two existing `GetInstanceID` warnings. |
| `probe-log.txt` | The probe run at a 1920×1080 Game view, 100% text: boot checks, the loaded content hash (equal to the canonical 150-role hash), the J-01 keeper swap, and the `STATSFIT` geometry line and three checks for each capture. |
| `shots-statsfix.tar` | The two captures, packed in a plain `.tar` because `*.png` is LFS-tracked and the evidence job checks out without LFS: `fix-live-statistics.png` (Match View right after kick-off with statistics opened) and `fix-statistics-after-sub.png` (Match View after one Applied substitution, one feedback row in the changes panel, with statistics opened). |
| `compile-disclosure.txt` | (v1.2) Batch-mode compile of the final disclosure change after the probe harness was removed and the `MatchClientUnity` build artifacts deleted, so that assembly is rebuilt: 0 `error CS`, the same two warnings. |
| `probe-log-disclosure-run1.txt` | (v1.2) First disclosure run. It found one defect: the Compare toggle's glyph stayed ▸ after opening, because the comparison is binding-local state and the label waited for a presenter revision. Fixed; see the next file. |
| `probe-log-disclosure.txt` | (v1.2) Rerun after the fix: 19 checks, all PASS. Statistics, Compare and Earlier change feedback each show ▸ closed and ▾ open on one title; there is no Open/Close statistics button and no separate heading; the open statistics panel fits the viewport with no scroll. |
| `shots-disclosure-1.tar` | (v1.2) `d-statistics-closed.png` and `d-statistics-open.png` (Match View with the "Match statistics" title as the toggle, closed and open) and `d-compare-open.png` (Mentality dialog with "Compare all seven Mentalities" open). Plain `.tar` because `*.png` is LFS-tracked; split in two to stay under the 1 MiB binary guard. |
| `shots-disclosure-2.tar` | (v1.2) `d-earlier-closed.png` and `d-earlier-open.png`: "Earlier change feedback (5)" closed and open, after the substitutions reached the window limit. |
| `compile-popup.txt` | (v1.3) Compile excerpts for the pop-up change: the Editor import with 0 `error CS` and only the two known `GetInstanceID` warnings in `match-client-unity`; the batch EditMode run after the harness was removed, which reused those assemblies; and its result line, 423/423 passed, with both 151-role count tests passing. `TacticalDirector.ClientApp` was rebuilt in the same import; its log line is not among the excerpted progress lines, but the passing count tests and the new loaded content hash show the 151-role catalogue. |
| `probe-log-popup.txt` | (v1.3) 27 checks, all PASS (3 boot, 24 case). The loaded content hash equals the new canonical 151-role hash. Cases: `close-button` (the "✕ Close" label and its top-right placement; closing with a draft pending submits nothing and returns focus), `outside-click` (a click inside the frame and a right click keep the pop-up open; a left click on the backdrop closes it), `partial` (observer fault at tick 65, full match at 10×, the report toggle "▸/▾ Partial statistics — incomplete"). |
| `shots-popup-1.tar` | (v1.3) `p-mentality-open.png` (Change Home Mentality with a draft and the "✕ Close" control) and `p-substitution-open.png` (Make Home substitution with the same control). |
| `shots-popup-2.tar` | (v1.3) `p-partial-closed.png` and `p-partial-open.png`: the Post-Match Report after an observer fault, with the partial-statistics disclosure closed and open. |
| `probe-source/` | `S0HostStatsFix.cs` (v1.0), `S0HostDisclosure.cs` (v1.2), `S0HostPopup.cs` (v1.3), plus `S0HostChannel.cs` and `S0HostCapture.cs`, which the earlier evidence did not archive. The other harness files are identical to `../p5b-gate-j-fixes-20261021/probe-source/` apart from the output folder. |

**Result.** Both captures PASS all three checks: the whole statistics panel lies inside the viewport, its toggle is
visible, and "Match statistics" is shown once. The panel is 541 px tall. Opening it scrolled the page 9 px with an empty
feedback list and 62 px after one substitution, so the page title moves out of view but the score and clock stay visible.

**Not covered.** The I-Q16 text-scale and resolution matrix was not re-run on this change; the report screen, whose table
keeps its caption and spacing, was not re-captured. The full Editor log is not committed; the licensing lines it contains,
including the host's machine identifier, are left out of every excerpt here.

The runs were driven through the probe harness and synthetic UGUI pointer events; no keystrokes were sent. No project
asset, scene or setting was modified.

## Version History

| Version | Date | Notes |
| --- | --- | --- |
| 1.0 | October 28, 2026 | Initial evidence set for the J-04 statistics-rail fix. |
| 1.1 | October 28, 2026 | The two `.png` captures are packed into `shots-statsfix.tar` (contents byte-identical) so `SHA256SUMS` verifies on the LFS-free CI checkout. |
| 1.2 | October 28, 2026 | Owner decision: in-place disclosures keep one title with a ▸/▾ glyph, and the "Match statistics" title is the statistics toggle. Compile, both disclosure runs and five captures (in two plain `.tar` files) added. |
| 1.3 | October 28, 2026 | Owner decisions: the report partial-statistics toggle keeps one title, and pop-ups get a Close control and close on an outside click. Compile and EditMode excerpt, the pop-up probe log, four captures in two plain `.tar` files and the probe source added. |
