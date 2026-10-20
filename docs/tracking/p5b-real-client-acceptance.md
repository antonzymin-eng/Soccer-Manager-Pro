# P5b real-client acceptance, B8/B10 render-loop capture and S0 Gate J — pinned host

**Created:** October 19, 2026\
**Last Updated:** October 19, 2026\
**Version:** 0.1\
**Status:** Real-client I-Q01–19 EXECUTED on the pinned host; B8/B10 render-loop capture PASSED against the owner-set budget;
**S0 Gate J technical verdict: FAIL** (two Major contract gaps and one owner-judgement set remain). One High defect found and fixed on this branch.
Owner acceptance of the verdict is pending.\
**Purpose:** The owning record for the first real-client run of the four S0 screens, the B8/B10 on-host performance capture
and the Gate-J technical review. It supersedes the "real-client I-Q01–19 NOT RUN" line of
[p5b-screens-validation.md](p5b-screens-validation.md) §"Acceptance matrix" (v0.14) for everything recorded here.\
**Evidence:** [`evidence/p5b-real-client-c95e27ba-20261019/`](evidence/p5b-real-client-c95e27ba-20261019/README.md).

## 1. Identity, host and method

- **Source.** main `c95e27ba7fd398e37d297647ba060c2ab1f4baa7` (PR #517 merge). Run 1 used the Desktop checkout at that
  commit, clean apart from untracked editor recovery files. A parallel session then switched that checkout to another
  branch and started Linux shim builds in `src/`, so runs 2–4 moved to a dedicated worktree at the same commit with its
  own `Library` and `Assets/Scripts` junction. Runs after the I-Q15 fix (§3, J-03) use that commit plus the one-file diff
  in the evidence folder (`S0ScreensBehaviour.cs` v1.7).
- **Host.** The owner's Windows 11 machine (i5-9300H, 8 logical CPUs, GTX 1650, 24 GB), Unity 6000.4.9f1, DX11, Mono,
  Release code optimization. This is the certified determinism host.
- **Compile.** Fresh worktree import: `Tundra build success`, 0 `error CS`. Forced recursive re-import after the fix:
  success, 0 errors; the compiled `TacticalDirector.MatchClientUnity.dll` contains the new guard.
- **Driver.** An untracked Editor-only harness (archived as `probe-source/`) drove the shipped scene in Play mode:
  pointer actions through UGUI `ExecuteEvents` on the real buttons, keyboard actions as real OS keystrokes with scan
  codes, state read by reflection, never written except the named fault/fixture injections below. Text scale was set
  per Play session on the scene component and never saved. Screens were captured synchronously at end of frame
  (an earlier asynchronous capture mislabelled sizes; those images were discarded and retaken).
- **Named injections.** I-Q08 refuses a request by making the staged pair illegal with the engine's internal
  `SubstitutePlayer` (the host-free test's move). I-Q12 swaps the analytics world sample for one that throws.
  I-Q16 pseudo content and long names are harness substitutions (§4): the shipping shell has no pseudo-locale,
  long-name fixture or published-catalogue loader path.

## 2. Results by case

| Case | Result | Notes |
| --- | --- | --- |
| Boot | PASS | Main Menu only; four exclusive roots; one EventSystem; no development host. Loaded content hash `2a8264…202dec` equals the canonical base hash (no glyph fallback with LegacyRuntime). The `51e1a2…` hash in the screens record predates #505's copy change (v1.4 of `S0ScreenContent`). |
| I-Q01 | PASS | Push/Pop by name; heading focus; draft resets to Balanced; no session. |
| I-Q02/03 | PASS ×7 | Every Mentality reaches the session; repeated activation makes one attachment; MV-0 withholds score/clock (`—`, "Clock awaiting first frame") and locks controls; 200 projections while paused change neither tick nor digest. |
| I-Q04 | PASS | Ladder 1/3/5/10 clamps with endpoint reasons; focus moves to Pause/Resume; rung survives pause; clock = tick/3600. 1× measured 62 ticks/s in the Editor. |
| I-Q05 | PASS | Dialog modal for focus and pointer (raycast hits the dialog); Current/Requested tags move correctly; Cancel sends nothing and restores the invoker. |
| I-Q06 | PASS | Applied from a new suffix; identical resubmit is a new record; one log entry each. |
| I-Q07 | PASS | Paused Mentality and substitution coexist as Pending with the Resume instruction; no ticks; Mentality applies on the first drain, the substitution at the next restart. |
| I-Q08 | PASS | Generic persistent Refused; last Applied value kept; playback continues. |
| I-Q09 | **FAIL (J-01, J-02)** | Keeper offered; bench labels 1–7; used/reserved exclusions correct. Role-mismatched pairs and window exhaustion are not gated (§3). |
| I-Q10 | PASS | A chooser open at the whistle closes; focus goes to the report action; every request settles; no mutation after the end. A deferred substitution submitted at 1× near the whistle applied at a minute-89 stoppage; the unmatched-at-end path did not occur live and stays covered host-free. |
| I-Q11 | PASS | Same snapshot both columns; no shots row; xG omitted; possession not renormalized; disclosure and focus survive updates. |
| I-Q12 | PASS (mid-match) | Incomplete warning persists, no healthy partial interval, play continues, report partials start hidden. The before-first-publication variant is not reachable in the real client (the first frame publishes); host-free covers it. |
| I-Q13 | PASS | Open+faulted and closed+healthy both retain disclosure, hide the toggle, offer the report only from the end. |
| I-Q14 | PASS | Return clears; second match has fresh session, context, observer, feedback and draft; old session stays stopped; no blank markers. |
| I-Q15 | **PASS after fix (J-03)** | First run: any arrow key on a choice group crashed the client. With v1.7: 38/38 keyboard checks — action-only Tab, MV order Pause → Faster → Change Mentality → Make substitution → statistics → pitch entry, modal wrap both directions, Escape restores the invoker, Submit focuses the new feedback row, Tab then reaches statistics, read-only pitch inspection re-enters at the inspected player, focus ring visible. Earlier-feedback disclosure by keyboard was not reached in this run (three records). |
| I-Q16 | PASS (with limits) | 1366/1920/2560 × 100/150/200% plus 200% pseudo and 200% long names: 0 clipped texts, 0 unreachable actions, 0 pitch-label overlaps, `PitchLayoutFailureCount` 0, true 105:68 pitch aspect, full strings wrap with no autoshrink, narrow-pitch case clean. At base size the live Match View fits without scrolling at 1920 and 2560. The packaged/published catalogue identity proof is still TO BUILD (journey §14.8). |
| I-Q17 | PARTIAL | No glyph failure with the packaged LegacyRuntime font; meaning carried by words. Contrast not measured; see J-04. |
| I-Q18 | PARTIAL | Real unusual scores (15–14, 14–13, 12–12) and dense chronological history render legibly. The five-substitution cap could not be reached in a real match because of J-02. |
| I-Q19 | PASS | Pinned-host compile, live inputs and the cert capture below. |

## 3. Findings

| ID | Severity | Finding | Disposition |
| --- | --- | --- | --- |
| J-01 | Major | The substitution chooser offers goalkeeper↔outfield pairs that the engine always refuses (`GoalkeeperRoleMismatch`, added by #505). The bench keeper is listed first; eight consecutive outfield requests choosing him were each Refused with the generic row. Journey §14.4 places legal-pair filtering, with a reason, in the presenter. | Open. Fix needs a presenter rule mirroring the engine role check and an owner-approved reason string (binding-contract role register). |
| J-02 | Major | After three Law 3 windows, the engine refuses every second-half request (`NoWindowsLeft`) while Make substitution stays enabled at 4/5 with no reason; each submit is a generic Refused. `LiveMatchFrame` carries substitutions used but not windows used. | Open. Needs a frame projection of windows used (viewer/engine contract), presenter gating and an owner-approved reason. |
| J-03 | High | Keyboard arrows on any choice group re-entered `Keyboard()` through the Control's `Refresh()` in the same frame until the stack overflowed; the exception rejected the pitch renderer and the next Start failed. Gate-invisible (Unity binding only). | **Fixed on this branch**: `S0ScreensBehaviour.cs` v1.7 processes keyboard input once per frame. Verified by the I-Q15 rerun. |
| J-04 | Minor (owner judgement) | The skin is neutral: plain text buttons without chrome, dialogs as full pages, outcome words without the ✓ ✕ – glyphs of I-C11, shirt labels drawn on the marker discs in dark text, and the follow camera frames part of the pitch while `pitch.description` says goals are shown. Opening statistics at 1920×1080 shrinks the pitch to about 305 px wide. | For the owner's Gate-J visual acceptance against H v0.2. |
| J-05 | Minor (owner decision) | The 10× rung delivers about 5.5× on this host (315–340 ticks/s instead of 600). The sim thread is the limit; the engine's per-tick snapshot encode allocates about 35 KB per tick. 1× holds 60 ticks/s. | Owner: accept, relabel, or open engine performance work. Rendering is unaffected (below). |
| J-06 | Info | Editor-only: an unfocused Editor runs the sim far slower (70–550 ticks/s); never use Editor timing as performance evidence. | Recorded. |

## 4. B8/B10 render-loop capture

Owner-set budget (October 18): Windows x64 **Mono development player**, p99 frame time ≤ 16.7 ms at 1× and 10×, statistics
open and closed, 100% and 200% text. Each condition is one player session with the player's own profiler log
(`-profiler-enable -profiler-log-file`), driven by keyboard only, its state verified from a screenshot, held 30 s; the last
20 s minus the 10 quit frames are measured (1,989 frames each). Development players were built from the worktree with
`runInBackground` off as committed.

| Text | Condition | mean FPS | p50 | p99 | max | > 16.7 ms | sim ticks/s |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 100% | 1×, stats closed | 217 | 4.53 | 7.18 | 8.31 | 0 | 60 |
| 100% | 1×, stats open | 226 | 4.22 | 7.01 | 12.50 | 0 | 60 |
| 100% | 10×, stats open | 212 | 4.42 | 11.44 | 14.50 | 0 | 340 |
| 100% | 10×, stats closed | 219 | 4.46 | 7.28 | 8.63 | 0 | 328 |
| 200% | 1×, stats closed | 275 | 3.60 | 5.85 | 6.42 | 0 | 60 |
| 200% | 1×, stats open | 245 | 3.95 | 6.46 | 11.51 | 0 | 60 |
| 200% | 10×, stats open | 218 | 4.37 | 10.40 | 12.46 | 0 | 334 |
| 200% | 10×, stats closed | 229 | 4.21 | 6.96 | 8.77 | 0 | 324 |

Times are milliseconds of main-thread CPU frame time. **Result: PASS** — every condition is far inside the budget, with
no frame over 16.7 ms. Main-thread client allocation is about 2 KB/frame at 1× and 7.5 KB/frame at 10×
(`S0ScreenPresenter.Refresh` and `ClientMatchCoordinator.Refresh`), inside the #20 §3.3.4 client carve-out; the
sim-thread snapshot encode dominates allocation (J-05). Void attempts are kept outside the repo and not used: an
early live-link capture that dropped frames, one where a parked pointer changed speed, one whose keystrokes reached the
desktop (no application was launched; the driver now refuses to send a key unless the player owns the foreground),
and one whose profiler log stopped when the Editor was not recording.

## 5. Gate-J technical verdict

Against [ux-detailed-plan.md](../design/ux-detailed-plan.md) §5 Gate J and §6.9:

| Gate-J requirement | Result |
| --- | --- |
| P4b binding compiles/runs on the pinned host; cert obligations | PASS — compile, scene boot, render-loop capture. Click-to-select is outside S0 (I-C08 admits no tactical pitch click). |
| P5b four-screen binding present, follows `ClientScreenFlow` | PASS — five named moves only; no navigation logic in `MonoBehaviour`s. |
| On-host P6 scene/live-input/performance | PASS with J-05 — boot, live tactical input through screens, 200+ FPS. |
| Pseudo-locale/text-scale/focus/fallback/resolution | PASS with limits — matrix clean; packaging identity proof TO BUILD; contrast unmeasured. |
| No domain/navigation decision moved into `MonoBehaviour` | PASS — the J-03 fix is input plumbing, not a decision. |
| Implementation matches the validated UX | **FAIL** — J-01 and J-02 contradict the §14.4 substitution contract; J-04 needs the owner's visual call. |

**Verdict: FAIL.** Gate J can pass after J-01 and J-02 land with owner-approved reason copy and a windows projection,
the I-Q09/I-Q18 cap case is re-run, and the owner accepts J-04 and J-05. B8 and B10 performance obligations are
discharged by §4. This verdict is technical; owner acceptance is a separate decision.

## Version History

| Version | Date | Notes |
| --- | --- | --- |
| 0.1 | October 19, 2026 | First real-client run of I-Q01–19, B8/B10 render-loop capture against the owner budget, J-03 fix verified, Gate-J technical verdict FAIL on J-01/J-02 with owner items J-04/J-05. |
