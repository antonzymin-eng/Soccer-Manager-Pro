# UI Toolkit T0a host procedure

Created November 20, 2026 for the editor-only feasibility spike. Use the pinned Windows 11 host with Unity 6000.4.9f1 (`f7258d6eebbe`) and the `Assets/Scripts` junction documented by the project. Record the tested commit and host session number. [PR #560](https://github.com/antonzymin-eng/Soccer-Manager-Pro/pull/560) · [Unity project setup](../../Assets/README.md)

## Import and compile first

From the spike checkout on the pinned host, with its `Assets/Scripts` junction in place and the Editor closed for that checkout, run the preflight with Python 3. Use the actual path to the pinned Editor installation.

```powershell
py -3 tools/unity-ci/run-ui-toolkit-t0a.py --editor "C:\Program Files\Unity\Hub\Editor\6000.4.9f1\Editor\Unity.exe"
```

Unity imports and compiles the spike before running its three EditMode tests. The runner creates a fresh directory under `artifacts/ui-toolkit-t0a/` with `editor.log`, `editmode.xml` if Unity writes it, and `receipt.json` containing the commit, Editor version, working-tree status, and result. A nonzero exit points to an Editor, compile, or test failure. Review `editor.log` for UXML, USS, and TSS import diagnostics and review any importer `.meta` changes before collecting Play Mode evidence.

For a run without the batch script, open the spike checkout in the pinned Editor, inspect the Console after import and compilation, then use the Test Runner procedure below.

## Stock input comparison

1. Open `Assets/Scenes/Scene.unity` in Play Mode. With the Game view focused, use Tab and Shift+Tab on Main Menu to establish the S0 baseline. Record the focus indicator, ordering, and Enter activation. Attach via **Tools → UI Toolkit T0a → Attach with stock S0 input**.
2. Confirm that the rail and the existing UGUI canvas are visible together. Click an uncovered UGUI control to check pointer routing. Then focus an S0 button with Tab and use F8 to request entry into the rail. Try Tab, Shift+Tab, and Enter. Also try clicking a rail button. Record whether S0's `LateUpdate` takes back focus before keyboard operation, and whether any single gesture triggers two controls. Use Console messages tagged `[UI Toolkit T0a]` to correlate focus events and EventSystem selection by frame. The F8 log records whether focus held on the next frame, whether `FocusIn` was seen before a loss, and which selection remained when the probe returned to UGUI.
3. With stock input, walk the demo route to Match View. Confirm that the rail, pitch, and UGUI controls render together and that uncovered pitch/UGUI input still reaches its original target. Note any overlay ordering or picking issues. Use the built-in demo flow for this check.

## Isolated focus comparison

1. On the idle Main Menu in Play Mode, detach the stock rail and choose **Attach with isolated Toolkit focus**. Focus a UGUI control with Tab, then press F8. The comparison suspends the two S0 behaviours' automatic updates while Toolkit owns focus. UGUI button handlers can still call `S0ScreensBehaviour.Refresh()` directly, which also runs its keyboard handling. Use the keyboard alone for the isolated traversal.
2. Verify that focus starts on **Cycle text scale**, then use Tab and Shift+Tab through the long fixture text, **Test keyboard activation**, and **Return focus to UGUI**. Record all extra tab stops, skips, duplicate activations, visible focus indicators, and scroll jumps. Press Enter once and check the counter increments once. Press Enter on **Return focus to UGUI**, then check selection and Tab operation on the original S0 control. Repeat the round trip with F8. As a separate pointer check, click outside the rail and observe whether the S0 behaviours resume when focus leaves the rail. Record any S0 action the click also triggers.
3. At 1366×768 and 1920×1080, use F9 to cycle 100%, 200% text via USS, and 200% whole-panel scale. At each setting use Tab and Page Up/Page Down to read the long fixture and reach the last button. After layout settles, use F10 to log the sample bounds and viewport. Capture the Game view and record any clipped lines, hidden focus, or inaccessible controls. Pair the startup glyph log with a screenshot of the accented names at both text sizes. This checks runtime SDF creation and rendering from Unity's built-in `LegacyRuntime.ttf`. Coverage of the proposed Q4 fonts remains open.
4. Detach while the rail owns focus and confirm normal S0 navigation resumes. Repeat with Play Mode exit. Enter Play Mode again and confirm the rail appears after an explicit attach.

## EditMode and decision record

Use the preflight's `editmode.xml`, or run `TacticalDirector.UiToolkitSpike.Tests` in **Window → General → Test Runner → EditMode**. Record each result for UXML, USS, and TSS loading and detached tree structure, synthetic Tab and Shift+Tab dispatch in a live Editor panel, and computed styles and reflow. If batch execution cannot lay out the utility window, retain its receipt and run the same fixture in the interactive Editor. Physical keys, the runtime EventSystem, pointer routing, and screen composition are Play Mode observations.

| Receipt item | Record from the pinned host |
|---|---|
| Host and source | Unity version, host session number, commit, import and compile result, scene, Game view sizes |
| Runtime coexistence | Stock Main Menu and Match View screenshots, overlay and pointer results |
| Focus parity | Stock and isolated forward/backward traversal, activation counts, selection restoration, any workaround |
| Scale | Both 200% experiments, viewport and sample bounds, keyboard reach and screenshots |
| Font | Built-in font used for runtime SDF creation, glyph record, and rendered sample |
| EditMode | Test Runner results for clone structure, synthetic Tab traversal, and Editor panel reflow |

Keep the throwaway probe on the spike branch. Record the host receipts and the **Go** or **No-go** in the UI Toolkit design supplement, with the observed keyboard behavior and the cost of any ownership changes. Factor in the work needed to reproduce the isolated keyboard behavior alongside live S0 updates. T0a's two-session timebox applies to the PM-2 career lane as recorded in §4.1 of the design reviewed in PR #560. [T0a and PM-2 fallback rules](https://github.com/antonzymin-eng/Soccer-Manager-Pro/blob/43d13e42edc6ac5aeba8a85769aee24b94c47398/docs/tracking/ui-toolkit-presentation-design.md#41-pm-2-dependency-and-fallbacks)
