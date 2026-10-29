# Pinned-host I-Q15 rerun with the earlier-outcome disclosure, and 200%/narrow reason-box captures

**Captured:** October 29, 2026, about 17:00–17:40 UTC (10:00–10:40 on the host's Pacific clock): reason-box captures
17:09–17:10, I-Q15 pass 1 17:27–17:30, pass 2 17:36–17:39\
**Purpose:** Two items from the October 29 external review of PR #518 at `4ff45ee8`.
(1) **I-Q15 was recorded as PASS without its earlier-outcome disclosure.** The journey contract
(`docs/design/ux-s0-pm1-journey.md` §14.8, I-Q15) includes the latest/earlier outcome disclosure and "no disclosure
collapse" while live ticks run. The disclosure appears only with four or more change records. The October 19 and
October 29 (§14) passes made three requests (Mentality, substitution, Mentality), and the harness only logged the
toggle's absence instead of failing, so the disclosure was never exercised. (2) **The fixed-height playback reason
(`c4b65334`) had no 200% or narrow-layout capture.**\
**Host:** the owner's Windows 11 machine, Unity 6000.4.9f1 (f7258d6eebbe), Mono. This is the certified determinism host.\
**Source:** commit `4ff45ee89ffee2792155e7cda6c81d5fcb2306a3` (PR #518 head), `src` tree `ef305bd8…`, the same tree as
the fix commit `c4b65334`. `source-identity.txt` lists both worktrees and their untracked harness files.\
**Integrity:** `SHA256SUMS` covers every other file in this directory and is verified by
`tools/dotnet-ci/check_evidence_manifests.py`. Images are in plain `.tar` files under 1 MiB because `*.png` is
LFS-tracked and the evidence job checks out without LFS.

| File | What it is |
| --- | --- |
| `source-identity.txt` | Commit and tree hashes, the two worktrees and their untracked harness additions, clean porcelain for `src`, `Packages` and `ProjectSettings`. |
| `compile.txt` | The I-Q15 worktree's batch first import (0 `error CS`, 75/75 assemblies) and the interactive session's recompiles (0 `error CS`); warnings outside the harness are the three known ones. |
| `iq15/probe-log.txt` | **Governing pass (pass 2):** 59 PASS, 0 FAIL, no abort, 17:36:09–17:39:04 UTC, 155 real keystrokes. |
| `iq15/probe-log-pass1.txt` | Pass 1: 50 PASS, 0 FAIL, no abort, 17:27:20–17:29:37 UTC. Kept because its live window is a separate observation; pass 2 adds the feedback-update steps. |
| `iq15/shots-1.tar` … `iq15/shots-4.tar` | Pass 2's nine captures (3840×2160 Game view), including `iq15-06-stats-history`, `iq15-06b-earlier-open-after-live` and `iq15-06c-earlier-after-new-record`. |
| `iq15/harness.diff.txt`, `iq15/S0HostKeyboard.cs.txt` | The change to the October harness (unified diff against the owner's `SMP-p5b-host` copy) and the resulting routine, archived verbatim. |
| `reasonbox/pitch-sizes-200.txt`, `reasonbox/pitch-sizes-100-narrow.txt` | Pitch rectangle and selected speed at every step, 1× → 10× → 1×. |
| `reasonbox/sheets.tar` | Four sheets (200% at 1920×1080, 1366×768 and 800×1000; 100% at 800×1000), each showing 1×, 3× and 10× with the measured pitch size. |
| `reasonbox/S0SpeedCapture.cs.txt`, `reasonbox/capture_run.ps1.txt`, `reasonbox/runs.txt` | Capture harness source (now taking `-captureRes`), the run script, and each run's commit, text scale and exit codes. |

## 1. I-Q15 rerun

**Harness change.** In a throwaway worktree the October harness (`Assets/HostProbes`, untracked, copied from the owner's
checkout) was changed in one place, the statistics-and-history step of `S0HostKeyboard.IQ15`, plus its output folder.
It now makes Mentality requests by keyboard until four records exist, and *requires* the disclosure instead of logging
its absence. The rest of the routine (Main Menu, setup, Match View Tab order, both dialogs, pitch inspection) is
unchanged. Keys are real Windows keystrokes (`keybd_event`) into the focused Game view; the harness aborts rather than
send a key when Unity is not the foreground window. Unity was in the foreground for both passes and no key was refused.

**Result (pass 2, governing): PASS, uninterrupted — 59/59 (3 boot, 56 keyboard).** Unchanged from §14: Match View Tab
order Pause → Faster → Change Mentality → Make substitution → "▸ Match statistics" → pitch entry (H1) → Pause, modal
wrap in both dialogs, Escape restores the invoker and sends nothing, pitch inspection dispatches nothing. New:

| Check | Result |
| --- | --- |
| Records | Four records by keyboard (Mentality, substitution, Mentality, Mentality); "▸ Earlier change feedback (1)" shown. |
| Traversal | Tab reaches it after "▾ Match statistics" with the focus ring visible; Tab moves on to H1 and Shift-Tab returns to it. |
| Toggle | Enter expands it ("▾ …"), focus stays on it; later Enter collapses and Enter re-expands, focus retained each time. |
| Live ticks | Expanded for 60 s of live play (ticks 3123 → 6723): still expanded, focus and ring still on it. No record changed state in that window; all four were already Applied. |
| Feedback update | With it expanded, a fifth Mentality request by keyboard: it stays expanded behind the modal dialog, the new record arrives (five records), the label follows to "▾ Earlier change feedback (2)", it stays expanded, and Tab reaches it again with the ring visible. The new record was already Applied within 10 frames, so no later Pending → Applied change was observed. |

Pass 1 (before the feedback-update steps were added) gave the same results for the first four rows: 50/50, ticks
3140 → 6740 in its live window.

**Harness incident, not a result.** The first interactive Editor launch stopped at the Package Manager's
"Retry / Continue / Quit" dialog after a transient `EPERM` renaming a folder in `Library/PackageCache`. That Editor was
closed, the package cache deleted, the first import done in batch mode (no dialog), and the Editor reopened; both passes
ran in that second session.

## 2. Reason-box captures at 200% and narrow width

Release Windows x64 Mono players were built from `4ff45ee8` in the throwaway capture worktree, as in
`host-c4b65334-20261029/`: one with the scene's text scale set to 2 for the build only, one at 1. Each stepped
1× → 10× → 1× through the real buttons, capturing every step.

| Text | Resolution | Pitch at all seven steps |
| --- | --- | --- |
| 200% | 1920×1080 (borderless) | 697.0 × 451.4 |
| 200% | 1366×768 (window) | 697.0 × 451.4 |
| 200% | 800×1000 (window, stacked) | 768.0 × 497.4 |
| 100% | 800×1000 (window) | 357.0 × 231.2 |

The pitch holds one size at every step in all four layouts. At 200% on the two wider screens it sits at its measured
minimum and the page scrolls, which is why both read 697.0. At 200% and 800×1000 the reserved line is two text lines
tall, because the longest playback reason ("Match starting — controls unavailable until the first frame.") wraps at
that width; the one-line reasons leave the second line empty.

## Not covered

The full EditMode suite was not rerun: `src` is unchanged since `host-c4b65334-20261029/` (424/424). The I-Q16 matrix
was not rerun. Full Editor logs are not committed; licensing lines and the host's machine identifier are left out.

## Version History

| Version | Date | Notes |
| --- | --- | --- |
| 1.0 | October 29, 2026 | Initial evidence set: I-Q15 rerun with the earlier-outcome disclosure (pass 2 governing, 59/59; pass 1 50/50) and 200%/narrow reason-box captures at PR #518 head `4ff45ee8`. |
