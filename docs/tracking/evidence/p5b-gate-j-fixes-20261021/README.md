# S0 Gate-J fixes (J-01, J-02, J-04) — pinned-host evidence

**Captured:** October 21–22, 2026 UTC (October 21 on the host's Pacific clock). Run-log timestamps are UTC time of day.\
**Purpose:** Raw evidence for [`p5b-real-client-acceptance.md`](../../p5b-real-client-acceptance.md) v0.2 §6.\
**Host:** the owner's Windows 11 machine, Unity 6000.4.9f1 (DX11, Mono). Certified determinism host.\
**Source:** branch `client/p5b-host-acceptance` at `37ca536e` plus the uncommitted J-01/J-02/J-04 working tree that this
evidence lands with. The worktree's `Assets/Scripts` junction points at that tree; every compile was a forced recursive
re-import.\
**Integrity:** `SHA256SUMS` covers every other file in this directory.

| File | What it is |
| --- | --- |
| `probe-log-run3.txt.xz` | Run 3: J-01 through the chooser, three Law 3 windows used, J-02 second-half refusal (`NoWindowsLeft`, reason named), then an I-Q10/13 segment that is void as a check (see the record). The skin is the pre-fix build: playback buttons stretched and the reason accent bar too wide. |
| `probe-log-run4.txt.xz` | Run 4 onward, after the layout fixes: restyled screens, J-01 again, full-time path with windows left, the Shots retake, I-Q16 matrix passes, and the I-Q15 keyboard attempts. Each attempt's boot block shows whether it started clean (see the record). |
| `layout-summary-run4.txt.xz` | Channel summaries of the second I-Q16 matrix (100/150/200%, 200% pseudo, 200% long names) and the 200% re-runs after the team-label fix. |
| `shots-1-screens.tar` | Main Menu, Tactics Setup, live Match View, statistics open, Mentality dialog (1920×1080, 100%). |
| `shots-2-gate-j.tar` | J-01 chooser with outfield and keeper outgoing; Match View after the keeper swap; Post-Match Report; J-02 refused row (run 3, pre-layout-fix build). |
| `shots-3-layout.tar` | I-Q16 samples: narrow 800×1000 at 200% after the team-label fix, substitution dialog with long names at 200%, pseudo-locale at 200%, statistics and history at 1920×1080. |
| `unity-compile-lines.txt` | Every `Tundra build` and `error CS` line of the Editor log for these runs (no errors). |
| `probe-source/` | The untracked harness case, run and probe sources, verbatim as `.txt`, including the keyboard foreground guard. |

The keystroke-driven B8/B10 player capture and one uninterrupted I-Q15 pass on this skin are still due; see the record.

## Version History

| Version | Date | Notes |
| --- | --- | --- |
| 1.0 | October 22, 2026 | Initial evidence set for the J-01/J-02/J-04 fixes. |
