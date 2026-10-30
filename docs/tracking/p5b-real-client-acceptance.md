# P5b real-client acceptance, B8/B10 render-loop capture and S0 Gate J — pinned host

**Created:** October 19, 2026\
**Last Updated:** October 30, 2026\
**Version:** 0.14\
**Status:** Real-client I-Q01–19 EXECUTED on the pinned host; B8/B10 render-loop capture PASSED against the owner-set budget
(v0.1 skin) and again on the H v0.2 skin (§14, v0.10). The owner's October 20 decisions are implemented and verified on the host (§6):
- **J-01 FIXED:** goalkeeper-mismatched choices are greyed out, with a reason.
- **J-02 FIXED:** a refusal names its engine-typed reason; the button is kept.
- **J-04 FIXED and ACCEPTED:** H v0.2 skin, whole-pitch view, statistics in the rail; the owner accepted the visuals on October 28 (§12, v0.8).
- **J-05 RE-MEASURED, PASS on PR #520 (§18, v0.14):** 10× delivers 601–602 sim ticks/s in the development player; it closes when #520 merges.

Since v0.10 (§14) the B8/B10 re-capture on the new skin, one uninterrupted I-Q15 pass and the I-Q16 re-run have PASSED. §14's I-Q15 pass did not reach the earlier-outcome disclosure; v0.13 (§17) reruns I-Q15 with it, PASS. v0.14 (§18) re-measures J-05 on PR #520's merged head: PASS. Still due: #520's merge and Gate-J acceptance. The owner approved the new wording on October 28 (§6, v0.4).
**S0 Gate J technical verdict: CONDITIONAL PASS** (§7; the v0.1 FAIL is kept in §5).\
**Purpose:** The owning record for the first real-client run of the four S0 screens, the B8/B10 on-host performance capture
and the Gate-J technical review. It supersedes the "real-client I-Q01–19 NOT RUN" line of
[p5b-screens-validation.md](p5b-screens-validation.md) §"Acceptance matrix" (v0.14) for everything recorded here.\
**Evidence:** [`evidence/p5b-real-client-c95e27ba-20261019/`](evidence/p5b-real-client-c95e27ba-20261019/README.md) (v0.1);
[`evidence/p5b-gate-j-fixes-20261021/`](evidence/p5b-gate-j-fixes-20261021/README.md) (v0.2 fixes);
[`evidence/host-963f48ee-20261028/`](evidence/host-963f48ee-20261028/README.md) (v0.3 merge-readiness compile, §8);
[`evidence/p5b-j04-statsfix-20261028/`](evidence/p5b-j04-statsfix-20261028/README.md) (v0.5 statistics-rail fix, §9; v0.6 disclosure titles, §10; v0.7 report disclosure and pop-up closing, §11);
[`evidence/p5b-gatej-recapture-20261029/`](evidence/p5b-gatej-recapture-20261029/README.md) (v0.10 B8/B10, I-Q15 and I-Q16 re-capture, §14);
[`evidence/host-fbcc3c2f-20261029/`](evidence/host-fbcc3c2f-20261029/README.md) (v0.11 Unity compile and EditMode for the §13 fix, §15);
[`evidence/host-c4b65334-20261029/`](evidence/host-c4b65334-20261029/README.md) (v0.12 fixed-height playback reason, Unity compile, EditMode and before/after captures, §16);
[`evidence/host-4ff45ee8-20261029/`](evidence/host-4ff45ee8-20261029/README.md) (v0.13 I-Q15 rerun with the earlier-outcome disclosure; 200% and narrow reason-box captures, §17);
[`evidence/host-05898cd0-20261030/`](evidence/host-05898cd0-20261030/README.md) (v0.14 J-05 re-measure on PR #520 with a `main` control; Unity compile and EditMode at the merged head, §18).

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
| I-Q09 | **FAIL (J-01, J-02)** → PASS after fixes (§6) | Keeper offered; bench labels 1–7; used/reserved exclusions correct. Role-mismatched pairs and window exhaustion are not gated (§3). |
| I-Q10 | PASS | A chooser open at the whistle closes; focus goes to the report action; every request settles; no mutation after the end. A deferred substitution submitted at 1× near the whistle applied at a minute-89 stoppage; the unmatched-at-end path did not occur live and stays covered host-free. |
| I-Q11 | PASS | Same snapshot both columns; no shots row; xG omitted; possession not renormalized; disclosure and focus survive updates. |
| I-Q12 | PASS (mid-match) | Incomplete warning persists, no healthy partial interval, play continues, report partials start hidden. The before-first-publication variant is not reachable in the real client (the first frame publishes); host-free covers it. |
| I-Q13 | PASS | Open+faulted and closed+healthy both retain disclosure, hide the toggle, offer the report only from the end. |
| I-Q14 | PASS | Return clears; second match has fresh session, context, observer, feedback and draft; old session stays stopped; no blank markers. |
| I-Q15 | **PASS after fix (J-03)** | First run: any arrow key on a choice group crashed the client. With v1.7: 38/38 keyboard checks — action-only Tab, MV order Pause → Faster → Change Mentality → Make substitution → statistics → pitch entry, modal wrap both directions, Escape restores the invoker, Submit focuses the new feedback row, Tab then reaches statistics, read-only pitch inspection re-enters at the inspected player, focus ring visible. Earlier-feedback disclosure by keyboard was not reached in this run (three records). |
| I-Q16 | PASS (with limits) | 1366/1920/2560 × 100/150/200% plus 200% pseudo and 200% long names: 0 clipped texts, 0 unreachable actions, 0 pitch-label overlaps, `PitchLayoutFailureCount` 0, true 105:68 pitch aspect, full strings wrap with no autoshrink, narrow-pitch case clean. At base size the live Match View fits without scrolling at 1920 and 2560. The packaged/published catalogue identity proof is still TO BUILD (journey §14.8). |
| I-Q17 | PARTIAL → glyphs PASS (§6) | No glyph failure with the packaged LegacyRuntime font; meaning carried by words. Contrast not measured; see J-04. |
| I-Q18 | PARTIAL → windows path PASS (§6) | Real unusual scores (15–14, 14–13, 12–12) and dense chronological history render legibly. The five-substitution cap could not be reached in a real match because of J-02. |
| I-Q19 | PASS | Pinned-host compile, live inputs and the cert capture below. |

## 3. Findings

| ID | Severity | Finding | Disposition |
| --- | --- | --- | --- |
| J-01 | Major — **fixed v0.2** | The substitution chooser offers goalkeeper↔outfield pairs that the engine always refuses (`GoalkeeperRoleMismatch`, added by #505). The bench keeper is listed first; eight consecutive outfield requests choosing him were each Refused with the generic row. Journey §14.4 places legal-pair filtering, with a reason, in the presenter. | **Fixed v0.2** (§6): `S0ScreenPresenter` v1.4 mirrors the engine role check and greys out each refused choice with a reason. Host PASS ×3. The owner approved the reason wording on October 28 (v0.4). |
| J-02 | Major — **fixed v0.2** | After three Law 3 windows, the engine refuses every second-half request (`NoWindowsLeft`) while Make substitution stays enabled at 4/5 with no reason; each submit is a generic Refused. `LiveMatchFrame` carries substitutions used but not windows used. | **Fixed v0.2** (§6): each typed refusal gets its own row (`NoWindowsLeft` → `feedback.substitution_refused_windows`); Make substitution stays available. The owner chose this over a windows-used projection, so `LiveMatchFrame` and the engine are unchanged. Host PASS (run 3). The owner approved the wording on October 28 (v0.4). |
| J-03 | High | Keyboard arrows on any choice group re-entered `Keyboard()` through the Control's `Refresh()` in the same frame until the stack overflowed; the exception rejected the pitch renderer and the next Start failed. Gate-invisible (Unity binding only). | **Fixed on this branch**: `S0ScreensBehaviour.cs` v1.7 processes keyboard input once per frame. Verified by the I-Q15 rerun. |
| J-04 | Minor — **fixed v0.2** | The skin is neutral: plain text buttons without chrome, dialogs as full pages, outcome words without the ✓ ✕ – glyphs of I-C11, shirt labels drawn on the marker discs in dark text, and the follow camera frames part of the pitch while `pitch.description` says goals are shown. Opening statistics at 1920×1080 shrinks the pitch to about 305 px wide. | **Fixed v0.2** (§6): H v0.2 skin, whole-pitch orthographic view, statistics in the rail (pitch 1055×683 with statistics open). The owner's visual acceptance and the B8/B10 re-capture on this skin are still due. |
| J-05 | Minor — owner chose 10×; PR #520 | The 10× rung delivers about 5.5× on this host (315–340 ticks/s instead of 600). The sim thread is the limit; the engine's per-tick snapshot encode allocates about 35 KB per tick. 1× holds 60 ticks/s. | Owner chose a real 10× (§6); engine throughput work is in PR #520, which still needs a Unity compile and a 10× re-measure on this host. Rendering is unaffected (below). **Re-measured v0.14 (§18): PASS, 601–602 ticks/s at 10× on #520.** |
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

*Superseded by §7 (v0.2) after the owner's decisions and fixes; kept as the v0.1 record.*

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

## 6. Owner decisions and Gate-J fixes (v0.2)

On October 20, 2026 the owner, Anton Zymin, chose a fix for each open finding:

| ID | Owner decision | Landed on this branch |
| --- | --- | --- |
| J-01 | Show goalkeeper-mismatched choices greyed out, with a reason. The owner approves the wording. | `S0ScreenPresenter` v1.4 mirrors the engine's request-time role check. When the outgoing player is chosen, each bench player the engine would refuse stays listed but is unavailable, with a reason in words: `reason.keeper_out`, `reason.keeper_in`, or `reason.keeper_on_way` (one keeper fallback already pending). The keeper fallback after a sent-off keeper stays available once. Entry needs a role-legal pair. The Unity binding disables the choice and shows the reason under its label. |
| J-02 | Keep the button and show the refusal message more clearly. No engine change. | The engine already returns a typed `SubstitutionRequestStatus`. `ManagerCommand.Apply` now passes it to the driver, which keeps it on its refused record (`TickStampedCommand.SubstitutionRefusal`, not part of the replay identity). `ClientChangeRecord.RefusalReason` stores it. Each typed refusal gets its own row: `feedback.substitution_refused_{cap, ended, keeper, sent_off, unavailable, windows}`. A refusal that threw keeps the generic row. No reason is inferred. Make substitution stays available. |
| J-04 | Fix the visual issues before acceptance. | The S0 skin is rebuilt to H v0.2:<br>• navy page with a centred column; bordered panels for the scoreboard, home changes and statistics, the entry pages under a blue rule;<br>• raised and primary control chrome; dashed borders and secondary text when a control is unavailable; gold outline and tag on a selected choice;<br>• reason and warning boxes;<br>• outcome rows with a status bar and ✓ ✕ – … badge;<br>• zebra statistics table;<br>• navy and gold shirt chips with leaders;<br>• the whole pitch framed top-down by an orthographic camera, so `pitch.description` ("goals … shown") is true;<br>• a centred modal staging dialog over a dimmed backdrop.<br>Statistics moved into the rail, so opening them no longer shrinks the pitch. |
| J-05 | Make 10× really deliver 10×. | Separate session and [PR #520](https://github.com/antonzymin-eng/Soccer-Manager-Pro/pull/520) (engine throughput, bit-identical). It still needs a Unity compile and a 10× re-measure on this host. **Both done October 29–30 (§18): compile clean, 10× PASS.** |

The new copy is drafted in the binding-contract register (`ux-s0-binding-contracts.md` v0.7, 150 roles). **Wording approved (v0.4):** on October 28, 2026 the owner replied “approve all” to the list of the nine new strings (`reason.keeper_out`, `reason.keeper_in`, `reason.keeper_on_way` and the six `feedback.substitution_refused_*` rows). The copy is approved exactly as drafted and shipped at `e4a0dec9`; four optional rewordings offered with the list were not adopted, so no string, test or content hash changes. The record is in `ux-s0-binding-contracts.md` v0.8.

**Host-free tests (Windows .NET 8 shim):** MatchClientCore 190/190, ClientApp 58/58, UiFramework 50/50. New tests:
- the presenter greys out keeper pairs with their reasons;
- the fallback after a sent-off keeper is offered and Applied;
- a real-drain refusal shows `OutgoingAlreadyReplaced` as "one of these players is no longer available";
- the driver keeps a typed refusal, and a thrown refusal stays untyped;
- the whole-pitch framing holds both goal lines.

**Pinned host (Unity 6000.4.9f1).** Every compile was a forced recursive re-import, with 0 `error CS`; the only warnings are the two existing `GetInstanceID` ones. The loaded content hash is `96fe4eee…8b864c98`, equal to the canonical base hash for 150 roles.

| Check | Result |
| --- | --- |
| J-01 (run 3, run 4, Shots) | **PASS** ×3:<br>• nothing greys before an outgoing pick;<br>• outfield out → only the bench keeper is unavailable ("…can only come on for the goalkeeper.");<br>• keeper out → all six outfield bench players are unavailable ("…can only be replaced by a goalkeeper."), shown under the label;<br>• a click on a greyed choice changes nothing;<br>• the keeper-for-keeper swap is Applied with no refusal. |
| J-02 (run 3) | **PASS:**<br>• windows used: 1 (keeper swap), then 2 at tick 1732 and 3 at tick 2405, each Applied;<br>• in the second half Make substitution stays enabled;<br>• the next request is Refused with `NoWindowsLeft`;<br>• the row reads "Refused — Daniel North (shirt 4) → Oliver Shaw (shirt 15, bench slot 4); no substitution windows are left; substitution count unchanged.";<br>• two later requests are refused the same way. |
| Full time with windows left (run 4) | **PASS** 12/12, including:<br>• a deferred substitution near the whistle settles as Applied;<br>• no mutation after the end;<br>• the report is available only from the end.<br>Run 3's two I-Q10/13 FAIL lines are void: its oracle assumes unused windows (the near-whistle request was correctly refused `NoWindowsLeft`), and the report screen had already been opened by outside input (below). |
| J-04 views | Menu, setup, live, statistics, Mentality and substitution dialogs, report and the J-02 row match H v0.2 (`shots-*.tar`). The pitch is 1055×683 at 105:68 with statistics open or closed (v0.1: shrank to about 305 px). |
| I-Q16 matrix (second run, 90 samples) | **PASS:**<br>• 100/150/200%, 200% pseudo and 200% long names at 1366/1920/2560 plus narrow 800×1000;<br>• 0 clipped texts, 0 pitch-label overlaps, 0 layout failures; every pitch sample 105:68.<br>Three samples are void: the Editor reported a transient screen size while sampling (1411×298, 1094×45, 367×618), and every control those audits flagged lies inside the requested resolution. The rerun found one defect: at 800×1000/200% the scoreboard broke "Home"/"Away" mid-word. It is fixed (team labels keep their own width) and the 200% passes re-ran clean. |
| I-Q15 keyboard | **PASS in parts; one clean pass still due.** Attempt 1 passed all 28 checks from boot through statistics open; attempt 3 passed dialog, feedback, statistics and pitch inspection (25). Both were cut short when another application took the foreground; the guard stopped each attempt without sending a key. Attempts 2–3 also booted into a match already started by stray input (the owner was playing a game on the same PC), so their boot-dependent checks are void. |
| B8/B10 on the new skin | **Not re-run.** The v0.1 capture (§4) used the neutral skin. The re-capture needs the PC idle for about 30 minutes of player keystrokes. |

**Harness safety change.** The Editor focus helper no longer taps Alt to steal the foreground. Each harness key now goes only to the Unity Editor and aborts otherwise. The player driver already had this guard (v0.1).

## 7. Gate-J technical verdict (v0.2)

J-01 and J-02 no longer contradict the §14.4 substitution contract, and the I-Q09/I-Q18 cases pass on the host. J-04's visual set is implemented against H v0.2.

**Verdict: CONDITIONAL PASS.** Gate J can pass when all of these hold:
- ~~B8/B10 are re-captured on this skin within the 16.7 ms p99 budget~~ — **done October 29** (worst p99 10.41 ms, §14, v0.10);
- ~~one uninterrupted I-Q15 run passes~~ — **done October 29** (37/37, §14, v0.10);
- ~~the owner accepts the visuals~~ — **done October 28** (J-04 visuals accepted, §12, v0.8; the new wording was approved the same day, v0.4);
- J-05 lands, or its re-measure is accepted — **re-measured PASS October 30** on PR #520's merged head (§18, v0.14); met when #520 merges.

Owner acceptance stays a separate decision.

## 8. Merge-readiness Unity compile at `963f48ee` (v0.3)

The last Unity compile on this branch ran on `37ca536` plus the uncommitted J-01/J-02/J-04 changes. The branch then
gained `14b43e8` (a test's expected role count) and the merge of main that brought in #519's C3/C4 career shell.
#519 landed without a Unity compile, so the combined tree had not been built in Unity.

On October 28 the head `963f48ee` was built on the pinned host from a fresh worktree with an empty `Library`:
`Tundra build success`, 0 `error CS`, all 75 `src` assemblies, and three warnings, none in #519's or this branch's
files. The EditMode run of `TacticalDirector.ClientApp.Tests`, `TacticalDirector.UiFramework.Tests` and
`TacticalDirector.MatchClientCore.Tests` passed 423 of 423 (181 / 52 / 190), with 0 failed and 0 skipped. Evidence:
[`evidence/host-963f48ee-20261028/`](evidence/host-963f48ee-20261028/README.md).

This clears the Unity-compile condition for merging. It does not change the §7 verdict: the B8/B10 re-capture, the
clean I-Q15 pass, the owner's wording approval (since given, v0.4) and J-05 still gate Gate J.


## 9. J-04 owner review: statistics rail fix (v0.5)

On October 28 the owner reviewed the J-04 screenshots (menu, setup, live, statistics open, Mentality dialog, report)
and asked for two points to be fixed before accepting the visuals:

- at 1920×1080 the open statistics table ran off the bottom of the screen (the last visible row was Throw-ins);
- "Match statistics" appeared twice in the statistics panel, as the panel heading and again as the table caption.

`S0ScreensBehaviour` v1.9 fixes both in the Unity binding only:

- the rail table caption is shown only when it adds something, i.e. the partial-statistics cutoff caption; complete
  statistics keep the panel heading alone. The report table keeps its caption, since the report heading is
  "Post-Match Report";
- rail table rows abut, separated by their rule lines, instead of being spaced 8 px apart. Each row is 33 px instead
  of 41 px; the report table keeps its spacing;
- opening statistics scrolls the page just far enough to show the whole panel, never past its toggle.

No copy, presenter, simulation or schema change; the loaded content hash is unchanged (`96fe4eee…8b864c98`).

**Pinned host.** The changed tree compiles in Unity 6000.4.9f1 with 0 `error CS` and the two existing `GetInstanceID`
warnings. At a 1920×1080 Game view and 100% text, both captures pass all three checks (whole panel inside the
viewport, toggle visible, "Match statistics" shown once):

| Capture | Panel | Page scrolled on open |
| --- | --- | --- |
| Right after kick-off, no feedback | 541 px, spanning 539–1080 px of the viewport | 9 px |
| After one Applied substitution (one feedback row) | 541 px, spanning 539–1080 px | 62 px |

In both, the score, clock and playback controls stay visible; only the page title scrolls out of view. With more
feedback rows the page scrolls further, and the scoreboard can leave the view while statistics are open.
Evidence: [`evidence/p5b-j04-statsfix-20261028/`](evidence/p5b-j04-statsfix-20261028/README.md).

**Not re-run:** the I-Q16 text-scale and resolution matrix on this change, and the report screen capture. The owner's
visual acceptance of J-04 is still due, together with the B8/B10 re-capture, one clean I-Q15 pass and J-05.

## 10. Owner decision: a disclosure keeps one title (v0.6)

After seeing §9, the owner decided on October 28 that "Match statistics" should itself be the open and close control,
and then that, in general, every control that opens a drop-down or a sub-screen should work this way.

`S0ScreensBehaviour` v1.10 applies this to the three in-place disclosures in the S0 screens. Each keeps one title and
shows its state with a ▸ (closed) or ▾ (open) glyph, or a covered substitute when the font lacks one:

| Control | Before | Now |
| --- | --- | --- |
| Match View statistics | heading "Match statistics" plus a button "Open statistics" / "Close statistics" | one button "▸ Match statistics" / "▾ Match statistics"; the plain heading returns only at full time, when the toggle is hidden |
| Earlier change feedback | "Earlier change feedback (N)" in both states | "▸ / ▾ Earlier change feedback (N)" |
| Mentality dialog comparison | "Compare all seven Mentalities" in both states | "▸ / ▾ Compare all seven Mentalities" |

No role, copy or content hash changes. `action.statistics_open` is no longer shown; `action.statistics_close` is still
used by the report's partial-statistics toggle.

**Pinned host.** The final tree compiles with 0 `error CS` (the `MatchClientUnity` build artifacts were deleted first, so
that assembly was rebuilt). The first probe run found that the Compare glyph stayed ▸ after opening; the comparison is
binding-local state and its label had waited for a presenter revision. After the fix, the rerun passes all 19 checks at
1920×1080: each title shows ▸ closed and ▾ open; there is no Open/Close statistics button and no separate heading; and
the open statistics panel (548–1062 px) fits without scrolling, since the separate button row is gone.
Evidence: [`evidence/p5b-j04-statsfix-20261028/`](evidence/p5b-j04-statsfix-20261028/README.md) v1.2.

**Not yet applied, pending the owner's answer:**
- the report's "Show partial statistics — incomplete" / "Close statistics" toggle, whose title would need new wording
  that reads correctly both open and closed;
- "Change Mentality" and "Make substitution", which open modal dialogs rather than content in place.

The owner's visual acceptance of J-04 is still due, together with the B8/B10 re-capture, one clean I-Q15 pass and J-05.

## 11. Owner decisions: report disclosure title and pop-up closing (v0.7)

The owner answered both questions left open in §10 on October 28:

1. **Report partial-statistics toggle:** use the suggested wording. `action.partial` is now the disclosure title
   "Partial statistics — incomplete" (was "Show partial statistics — incomplete"), shown as "▸ Partial statistics —
   incomplete" closed and "▾ Partial statistics — incomplete" open. `action.statistics_close` is no longer shown.
2. **Pop-ups:** the one-title rule applies only to content that expands in place. A pop-up window (Change Mentality,
   Make substitution) has a Close button, and a click outside it closes it too.

`S0ScreensBehaviour` v1.11 adds a "✕ Close" control (new role `action.close` → "Close", with the `Glyph` fallback for
the ✕) beside the dialog title, in the frame's top-right corner. The dimmed backdrop is now its own node behind the
frame, so a left click on it closes the pop-up and a click inside the frame never reaches it. Close and the outside
click do what Cancel and Escape already did: discard the draft, submit nothing and return focus to the button that
opened the pop-up. Cancel stays. The register grows from 150 to 151 roles (`S0ScreenContent` v1.6; both role-count
tests and binding contract v0.10). The owner's reply approves both strings.

**Pinned host.** A probe run at 1920×1080 passes all 27 checks (3 boot, 24 case), with the loaded content hash equal to
the new canonical 151-role hash:
- Close: labelled "✕ Close", 16 px from the frame's top and right edges, right of the title; it closes the Mentality
  pop-up with a draft pending, submits nothing and returns focus to "Change Mentality".
- Outside click: a click on text inside the substitution frame keeps it open; a right click on the backdrop keeps it
  open; a left click on the backdrop closes it, submits nothing and returns focus to "Make substitution". Pointer
  events were raycast at a screen point and sent to the handler the input module would choose.
- Report: after an injected observer fault and a full match at 10×, the toggle reads "▸ Partial statistics —
  incomplete" with the table closed, "▾ …" with it open, and "▸ …" again after a second click; no "Show partial
  statistics" or "Close statistics" label remains.

The Editor import of the changed tree had 0 `error CS` (only the two known `GetInstanceID` warnings in this assembly).
After the probe harness was removed, a batch EditMode run of the ClientApp, UiFramework and MatchClientCore tests
passed 423/423, including both 151-role count tests. Evidence: [`evidence/p5b-j04-statsfix-20261028/`](evidence/p5b-j04-statsfix-20261028/README.md) v1.3.

**Not changed:** the journey (`ux-s0-pm1-journey.md`) and the two HTML prototypes still show the old partial-statistics
wording and no pop-up Close control; the binding contract is the copy authority. The I-Q16 text-scale and resolution
matrix was not re-run. The match in this run ended 12–12; the engine's high goal rate is already recorded in
`open-issues.md` and is unrelated to this change.

The owner's visual acceptance of J-04 is still due, together with the B8/B10 re-capture, one clean I-Q15 pass and J-05.

## 12. Owner visual acceptance of J-04 (v0.8)

After reviewing the screenshots of §§9–11 (statistics rail, one-title disclosures, the report's partial-statistics
disclosure and the pop-ups' Close control), the owner replied “Accept J-04” on October 28, 2026 (recorded 22:27 UTC, 15:27 America/Los_Angeles).
The J-04 visuals are accepted as shipped at `2887ec10` (`S0ScreensBehaviour` v1.11, `S0ScreenContent` v1.6, 151 roles).

This discharges the visual-acceptance condition of §7. No code, copy or evidence changes here. The Gate-J verdict
stays **CONDITIONAL PASS** until the remaining conditions hold: the B8/B10 re-capture on this skin, one uninterrupted
I-Q15 pass, and J-05 (PR #520) landing or its re-measure being accepted. Gate-J acceptance itself remains the owner's
separate decision. Not re-run since the last visual changes: the I-Q16 text-scale and resolution matrix.

## 13. Review fix: reopened substitution keeps no stale availability (v0.9)

A review of PR #518 at `aa858e97` found a J-01 defect in the presenter. `OpenSubstitution()` cleared the outgoing and
incoming selections but did not re-derive bench availability. Choosing the goalkeeper as the outgoing player, cancelling
and reopening therefore showed no outgoing player selected while six bench players stayed unavailable with "…can only
be replaced by a goalkeeper."; choosing an outgoing player cleared it. The engine was never at risk, because the presenter
only greys choices out, but the reopened dialog contradicted §6's "nothing greys before an outgoing pick".

`S0ScreenPresenter` v1.5 re-derives availability when the dialog opens. `S0ScreenPresentationTests` v1.8 adds
`ReopeningSubstitutionAfterCancelGreysNothingBeforeAnOutgoingPick`, which failed before the fix (Linux shim; the other
22 presentation tests passed). With the fix, the Linux shim passes ClientApp 182/182. **Not re-run:** the pinned-host
Unity compile and EditMode suite for this change. It touches only `client-app`, which the Unity binding reads
through the presenter, so the J-04 visuals accepted in §12 are unchanged.

## 14. B8/B10, I-Q15 and I-Q16 re-captured on the H v0.2 skin (v0.10)

On October 29 (00:36–03:00 UTC; October 28 evening on the host clock) the owner left the PC idle and the three
remaining host runs were made at `aa858e97` (the PR #518 head when they began), unmodified (`S0ScreensBehaviour` v1.11, 151 roles). Both Editor
sessions compiled with 0 `error CS`. Evidence: [`evidence/p5b-gatej-recapture-20261029/`](evidence/p5b-gatej-recapture-20261029/README.md).
The §13 presenter fix (`9139701c`, `S0ScreenPresenter` v1.5) landed on the branch during these runs and is not in the
measured build; it re-derives bench availability when the substitution dialog opens and does not touch the render loop.

| Check | Result |
| --- | --- |
| I-Q16 matrix | **PASS.** Five passes (100%, 150%, 200%, 200% pseudo, 200% long names) at 1366×768, 1920×1080 and 2560×1440 plus narrow 800×1000: 95 layout samples, 0 clipped texts, 0 unreachable actions, 0 pitch-label overlaps or clipped labels, `PitchLayoutFailureCount` 0, every pitch 105:68, no void sample. The live Match View fits without scrolling at 1920 and 2560 at 100% and 150%, and at 2560 at 200%. |
| I-Q15 keyboard | **PASS, uninterrupted.** 37/37 (3 boot, 34 keyboard), no aborted key. Tab order Pause → Faster → Change Mentality → Make substitution → "▸ Match statistics" → pitch entry; the modal Tab cycle includes "✕ Close" and stays in the dialog. The earlier-feedback disclosure was again not reached by keyboard (three records). |
| B8/B10 | **PASS.** Same method as §4 (Mono development player, keyboard only, 30 s holds, last 1,989 frames from the player's own profiler log). Every condition inside the 16.7 ms p99 budget with no frame over 16.7 ms. |

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

The worst p99 is 10.41 ms (100%, 10×, statistics open), against 11.44 ms on the v0.1 skin. The 10× rung delivered
551–586 sim ticks/s here against 315–340 on October 19, without PR #520's throughput work; the cause was not
investigated, and J-05 (600 ticks/s) stays open.

**Harness incidents, not results.** The new player's first launch raised a Windows Firewall prompt; the key guard
sent nothing while it had focus, and the owner answered it. The trial that hit the prompt left the player profiling
until its log filled the disk (64 GB); it was stopped and the file deleted, and the driver now always stops the
player and caps the log. A first 1×-closed capture recorded only 2 frames because the Editor was not recording; the
driver now starts Editor recording for each condition, and that condition was re-captured.

**Verdict.** The B8/B10 and I-Q15 conditions of §7 are met, and the I-Q16 matrix is clean after the October 28 visual
changes. Gate J stays **CONDITIONAL PASS** with one condition left: J-05 (PR #520) lands or its re-measure is
accepted. Gate-J acceptance remains the owner's separate decision.

## 15. Unity compile and EditMode for the §13 fix at `fbcc3c2f` (v0.11)

§13's presenter fix (`9139701c`) had been verified only on the Linux shim, and §14's host runs used `aa858e97`, which
predates it. On October 29 the PR head `fbcc3c2f` was built on the pinned host from a fresh worktree with an empty
`Library`. `fbcc3c2f` is docs-only after `9139701c`; both have `src` tree `3b7981ef`. Result: `Tundra build success`,
0 `error CS`, all 75 `src` assemblies, and the same three warnings as §8, none in the fix's files. The EditMode run of
`TacticalDirector.ClientApp.Tests`, `TacticalDirector.UiFramework.Tests` and `TacticalDirector.MatchClientCore.Tests`
passed 424 of 424 (182 / 52 / 190), with 0 failed and 0 skipped. ClientApp's extra case against §8 is the §13
regression test `ReopeningSubstitutionAfterCancelGreysNothingBeforeAnOutgoingPick`, which passed under Unity's
NUnit 3.5. Evidence: [`evidence/host-fbcc3c2f-20261029/`](evidence/host-fbcc3c2f-20261029/README.md).

This closes §13's "not re-run" note. It does not touch the render loop or the §14 captures. Gate J stays
**CONDITIONAL PASS** with J-05 (PR #520) remaining; Gate-J acceptance remains the owner's separate decision.

## 16. Owner report: Faster resized the pitch; fixed-height playback reason (v0.12)

On October 29 the owner reported that the pitch changed size on the first and the last Faster click. The pitch is sized
from the page height left after the content above it (`S0BodyLayout`), and two reason boxes sat above it: "Already at
real time — slower is unavailable." showed at 1×, and "Already at the fastest speed — faster is unavailable." at 10×.
The first Faster click removed one and the last added the other. The waiting and ended reasons did the same at kick-off
and full time.

The four playback reasons never apply together, so `c4b65334` puts them in one box. `S0ReservedReason` v1.0 lays it out
at the tallest reason's height, measured at the label's current width and text scale, and blanks its chrome while
empty instead of deactivating it (`S0UiFactory` v1.3, `S0ScreensBehaviour` v1.12). The copy is unchanged.

On the pinned host, a full import of `c4b65334` from a fresh worktree gave `Tundra build success`, 0 `error CS`, all 75
assemblies and the three known warnings. EditMode ClientApp/UiFramework/MatchClientCore passed 424/424 (182 / 52 / 190).
`match-client-unity` is excluded from the Linux shim, so this is the change's only compile. Release players built from
`3061fcd9` (before) and `c4b65334` (after) stepped 1× → 10× → 1× through the real buttons, capturing every step:

| Build | Resolution | Pitch at 1× and 10× | Pitch at 3× and 5× |
| --- | --- | --- | --- |
| Before | 1920×1080 | 1054.6 × 683.0 | 1117.9 × 724.0 |
| After | 1920×1080 | 1054.6 × 683.0 | 1054.6 × 683.0 |
| Before | 1366×768 | 542.0 × 351.0 | 605.3 × 392.0 |
| After | 1366×768 | 542.0 × 351.0 | 542.0 × 351.0 |

At 3× and 5× the reason line is now blank but keeps its space. The owner compared the before/after sheets remotely and
replied "looks right now". Evidence: [`evidence/host-c4b65334-20261029/`](evidence/host-c4b65334-20261029/README.md).
**Not captured:** 200% text and the narrow layout; the reserved height follows the label's own width and text scale. (Captured since: §17.)
The §14 B8/B10 and I-Q16 captures predate this change, which touches only the reason boxes above the pitch.

Gate J stays **CONDITIONAL PASS** with J-05 (PR #520) remaining; Gate-J acceptance remains the owner's separate decision.

## 17. External review: I-Q15 earlier-outcome disclosure; 200% and narrow reason-box captures (v0.13)

An October 29 external review of `4ff45ee8` found no code defect and one evidence gap: §14 records I-Q15 as PASS, but
its pass never reached the earlier-outcome disclosure, which the I-Q15 contract (journey §14.8: "latest/earlier outcome
disclosure … no … disclosure collapse") includes. The disclosure appears only with four or more change records. The
October 19 and §14 passes made three requests, and the harness logged the toggle's absence instead of failing. The
review also noted that §16 had no 200% or narrow-layout capture.

**I-Q15 rerun — PASS, uninterrupted, 59/59** (3 boot, 56 keyboard; pinned host, `4ff45ee8`, real keystrokes, Unity in
the foreground throughout). The October harness was changed only in its statistics-and-history step: it makes
Mentality requests by keyboard until four records exist and requires the disclosure. Everything §14 recorded holds,
and in addition:

- four records (Mentality, substitution, Mentality, Mentality) show "▸ Earlier change feedback (1)";
- Tab reaches it after "▾ Match statistics" with the focus ring visible, and Shift-Tab returns to it from H1;
- Enter expands it with focus retained; later Enter collapses and re-expands it, focus retained each time;
- expanded through 60 s of live play (3,600 ticks), it stays expanded with focus and ring on it. No record changed state
  in that window, because all four were already Applied;
- with it expanded, a fifth request by keyboard: it stays expanded behind the modal dialog, the fifth record arrives,
  the label follows to "▾ Earlier change feedback (2)", and Tab reaches it again, still expanded. The new record was
  Applied within 10 frames, so no later Pending → Applied change was observed.

An earlier pass of the same session without the fifth-request steps gave 50/50.

**Reason box at 200% and narrow width.** Release players from `4ff45ee8` (same `src` as `c4b65334`) stepped
1× → 10× → 1×. The pitch held one size at every step: 697.0 × 451.4 at 200% on 1920×1080 and on 1366×768 (its measured
minimum, with the page scrolling), 768.0 × 497.4 at 200% on narrow 800×1000 (stacked), and 357.0 × 231.2 at 100% on
800×1000. At 200% and 800×1000 the reserved line is two text lines tall, because the longest playback reason wraps
there; one-line reasons leave the second line empty.

Evidence: [`evidence/host-4ff45ee8-20261029/`](evidence/host-4ff45ee8-20261029/README.md). The I-Q15 condition of §7
is met with the disclosure covered. Gate J stays **CONDITIONAL PASS** with J-05 (PR #520) remaining; Gate-J acceptance
remains the owner's separate decision.

## 18. J-05 re-measured on PR #520 (v0.14)

PR #520 (design P1a + P1b, bit-identical) had its Unity compile and EditMode leg at `01a5fcdc`. Merging PR #518 into
main made the merged head `05898cd0` a new `src` tree, so both legs were repeated there, then the development-player
10× re-measure was run. Evidence: [`evidence/host-05898cd0-20261030/`](evidence/host-05898cd0-20261030/README.md).

**Unity compile and EditMode at `05898cd0`.** Full import from an empty `Library`: 0 `error CS`, 75 assemblies, the three
known warnings. EditMode over DeterministicSim, CollisionSystem, ClientApp, UiFramework and MatchClientCore: 554
passed, 0 failed, 12 ignored by attribute; `SnapshotDigestBackendTests` 4/4, `SpatialHashGridQueryOrderTests` 2/2 and
`MatchEngineSnapshotDigestReferenceTests` 1/1.

**10× re-measure — PASS.** Same method as §4 and §14 (Mono development player, keyboard only, 30 s holds, last 1,989
frames of the player's own profiler log), with `main` as a same-session control alternating with #520 on the 10×
conditions.

| Build | Text | Condition | p99 | max | > 16.7 ms | sim ticks/s |
| --- | --- | --- | --- | --- | --- | --- |
| main | 100% | 10×, stats open (two runs) | 11.29 / 12.85 | 14.46 / 14.11 | 0 | 557 / 468 |
| main | 100% | 10×, stats closed | 6.85 | 8.29 | 0 | 516 |
| #520 | 100% | 10×, stats open (two runs) | 10.88 / 9.73 | 13.72 / 12.45 | 0 | **601 / 601** |
| #520 | 100% | 10×, stats closed | 6.47 | 7.70 | 0 | **602** |
| #520 | 200% | 10×, stats open | 9.85 | 13.45 | 0 | **601** |
| #520 | 200% | 10×, stats closed | 6.26 | 11.04 | 0 | **601** |
| #520 | 100% / 200% | 1×, stats open and closed | 5.94–6.31 | 7.93–11.37 | 0 | 60 |

Every #520 10× condition delivered the 600 ticks/s the rung asks for (601–602; the extra tick is the window's edge),
so the pace, not the simulation, is now the limit. `main` delivered 468–557 in the same session and 551–586 in §14,
varying with host load. The B8/B10 budget still holds: worst #520 p99 10.88 ms, no frame over 16.7 ms. End-state
screenshots confirm each condition's speed and statistics state. Not covered: allocation (the player logs carried no
per-frame GC counter; the Unity allocation leg is the EditMode test above) and the FR-PO-052 baseline (design E5).

**Verdict.** The J-05 re-measure passes. The last §7 condition is met when PR #520 merges. Gate J stays **CONDITIONAL
PASS** until then, and Gate-J acceptance remains the owner's separate decision.

## Version History

| Version | Date | Notes |
| --- | --- | --- |
| 0.1 | October 19, 2026 | First real-client run of I-Q01–19, B8/B10 render-loop capture against the owner budget, J-03 fix verified, Gate-J technical verdict FAIL on J-01/J-02 with owner items J-04/J-05. |
| 0.2 | October 22, 2026 | Owner decisions of October 20 implemented and host-verified (§6): J-01 greyed keeper pairs with reasons, J-02 typed refusal rows, J-04 H v0.2 skin with whole-pitch view and statistics in the rail; narrow-width team-label fix found by the I-Q16 rerun. Verdict CONDITIONAL PASS pending the B8/B10 re-capture, one clean I-Q15 pass, owner copy approval and acceptance. |
| 0.3 | October 28, 2026 | §8: merge-readiness Unity compile of head `963f48ee`, the first Unity build with #519 merged in. Full import: 0 `error CS`, 75 assemblies. EditMode ClientApp/UiFramework/MatchClientCore: 423/423 passed. Evidence `host-963f48ee-20261028/`. Gate-J verdict unchanged (CONDITIONAL PASS). |
| 0.4 | October 28, 2026 | Owner wording approval recorded (§6): all nine new Gate J strings approved as drafted in `ux-s0-binding-contracts.md` v0.7 (now v0.8). No copy, code or evidence change. Still due: B8/B10 re-capture, one clean I-Q15 pass, visual acceptance and J-05. Gate-J verdict unchanged (CONDITIONAL PASS). |
| 0.5 | October 28, 2026 | §9: the two points from the owner's J-04 screenshot review fixed in `S0ScreensBehaviour` v1.9 (no repeated "Match statistics"; whole statistics panel visible at 1920×1080). Unity compile clean; host captures pass. Evidence `p5b-j04-statsfix-20261028/`. Visual acceptance still due; Gate-J verdict unchanged (CONDITIONAL PASS). |
| 0.6 | October 28, 2026 | §10: owner decision that a control opening content keeps one title. The statistics, earlier-feedback and Compare toggles now show ▸/▾ on one title (`S0ScreensBehaviour` v1.10); the separate Open/Close statistics button is gone. Unity compile clean; host rerun 19/19 PASS after a Compare-label fix. The report partial-statistics toggle and the two dialog buttons await the owner's answer. Gate-J verdict unchanged (CONDITIONAL PASS). |
| 0.7 | October 28, 2026 | §11: owner decisions on the two §10 questions. The report toggle is the one-title disclosure "Partial statistics — incomplete" with ▸/▾; pop-ups get a "✕ Close" control and close on an outside click (`S0ScreensBehaviour` v1.11, `S0ScreenContent` v1.6, 151 roles). Host probe 27/27 PASS. Gate-J verdict unchanged (CONDITIONAL PASS). |
| 0.8 | October 28, 2026 | §12: the owner accepted the J-04 visuals (“Accept J-04”) as shipped at `2887ec10`; the §7 visual-acceptance condition is met. No code, copy or evidence change. Gate-J verdict unchanged (CONDITIONAL PASS): B8/B10 re-capture, one clean I-Q15 pass and J-05 remain. |
| 0.9 | October 29, 2026 | §13: PR #518 review fix. `S0ScreenPresenter` v1.5 re-derives bench availability when the substitution dialog opens, so a cancelled goalkeeper pick no longer leaves bench choices greyed out on reopen; regression test added (failed before the fix). Linux shim ClientApp 182/182; the pinned-host Unity run is not repeated. Verdict unchanged. |
| 0.10 | October 29, 2026 | §14: B8/B10 re-captured on the H v0.2 skin (8/8 within budget, worst p99 10.41 ms), one uninterrupted I-Q15 pass (37/37) and the I-Q16 matrix (95 samples, 0 problems) at `aa858e97`. Evidence `p5b-gatej-recapture-20261029/`. The §7 B8/B10 and I-Q15 conditions are met; Gate-J verdict CONDITIONAL PASS with J-05 remaining. |
| 0.11 | October 29, 2026 | §15: pinned-host Unity compile and EditMode for the §13 fix at head `fbcc3c2f` (same `src` tree as `9139701c`). Full import: 0 `error CS`, 75 assemblies. EditMode ClientApp/UiFramework/MatchClientCore: 424/424 passed, including the §13 regression test. Evidence `host-fbcc3c2f-20261029/`. Gate-J verdict unchanged (CONDITIONAL PASS, J-05 remaining). |
| 0.12 | October 29, 2026 | §16: owner report that Faster resized the pitch on the first and last click. Fixed in `c4b65334`: the four playback reasons share one `S0ReservedReason` box that keeps the tallest one's height. Unity compile clean (75 assemblies), EditMode 424/424, before/after captures at 1920×1080 and 1366×768 show the pitch holding one size; owner replied "looks right now". Evidence `host-c4b65334-20261029/`. Gate-J verdict unchanged (CONDITIONAL PASS, J-05 remaining). |
| 0.13 | October 29, 2026 | §17: external review of `4ff45ee8` — §14's I-Q15 pass never reached the earlier-outcome disclosure (three records; the disclosure needs four). I-Q15 rerun on the pinned host with four and then five records: disclosure reached by Tab and Shift-Tab, toggled by Enter, expanded state and focus kept through 3,600 live ticks and through a new record; 59/59, uninterrupted. Reason box captured at 200% (1920×1080, 1366×768, 800×1000) and 100% narrow: pitch holds one size at every speed step. Evidence `host-4ff45ee8-20261029/`. Gate-J verdict unchanged (CONDITIONAL PASS, J-05 remaining). |
| 0.14 | October 30, 2026 | §18: J-05 re-measured on PR #520's merged head `05898cd0` (main with #518 merged in). Unity compile clean (75 assemblies); EditMode 554 passed, 0 failed, 12 ignored. Development player at 10×: 601–602 sim ticks/s in all six #520 sessions (100% and 200% text, statistics open and closed) against 468–557 for a same-session `main` control; 1× holds 60; worst p99 10.88 ms, no frame over 16.7 ms. Evidence `host-05898cd0-20261030/`. The J-05 condition of §7 is met when #520 merges; Gate-J verdict CONDITIONAL PASS until then. |
