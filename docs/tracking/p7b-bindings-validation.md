# P7b independent career bindings

Created: 2026-11-06. Purpose: implementation contract and validation handoff.

This implements career-lane item 1 in `path-to-playable-roadmap.md` §5.1a and
`interactive-unity-client-design.md` §5-P7. Implementation is present; pinned-host
Unity compilation and runtime acceptance remain outstanding.

The seven-screen shell binds Main Menu New Career / Continue / Load Career, a
club-only generated New Career, the four-slot picker, and Career Home. Career Home
shows the last change, injury/board attention, the next fixture, the existing legal
progress action, table and three manual saves. Continue loads the newest ready
slot directly. Empty, ready, damaged and incompatible slots have explicit copy.
Occupied manual slots require confirmation; cancel performs no write. Autosave and
manual-save failures remain visible, and failed loads retain the current context.

`CareerScreenPresenter` schedules initialization, world generation, start, resume,
metadata reads, season dispatch and saves through one worker. The UI renders cached
snapshots while busy, disables competing actions and navigation, and consumes
completion on the UI thread. Disposal cancels queued work and waits for a running operation to finish before
a replacement shell can own the store or engine. Teardown can therefore wait for
an active matchday or save; normal screen refresh never waits. The underlying sources are never polled concurrently.
The S0 formatter now admits an independent career catalogue while retaining the
existing S0 register. Career controls use the existing scaled, wrapped, scrolling
layout, explicit Tab traversal and focus ring system; overwrite confirmation traps
focus to its actions.

No engine, package, project setting, save format, migration policy or RNG stream was
changed. The host binding supplies entropy only for a new career's initial seed.
The existing synchronous matchday dispatch runs on the worker and stays on Career
Home. D-P7-6 interactive match routes and S1 UX gate approvals are separate work.

## Setup and host acceptance

The tracked `Assets/Scenes/Scene.unity` contains three new inactive sibling roots
with references on `ClientShellBehaviour`; no Inspector wiring is needed for that
scene. Other scenes using this shell must provide the three corresponding roots.
Career files live in `Application.persistentDataPath/Careers`.

On the pinned Unity 6000.4.9f1 Windows host:

1. Compile, inspect Console output and run ClientApp EditMode tests with Unity's bundled NUnit.
2. Start a career, select a club, progress a day and a full matchday, save to each manual slot,
   cancel/confirm an overwrite, quit, Continue and load a chosen slot. Check autosave cadence.
3. Check unavailable Continue, empty/damaged/incompatible rows, failed save/load, retry,
   busy actions, scene teardown during work and the unchanged demo-match journey.
4. Check Tab/Shift-Tab, Submit, focus recovery and overwrite focus trapping. Review full
   labels and scrolling at 100%/200% text scale, narrow supported desktop sizes and expanded
   pseudo-locale content. Automated text admission does not establish rendered layout quality.

No Unity Editor or connected Unity MCP was available in the authoring environment;
there is no claim of Editor compilation, Play mode, visual review or S1 acceptance.

## Executed validation

- Final targeted NUnit run after the teardown refinements: **33 passed, 0 failed,
  0 skipped**. This includes 16 new regressions (10 career actions, five
  binding/content checks and one shared-coordinator check), plus the existing
  career-flow, visibility and wiring guards.
- All seven screens map to exactly one root; career roots participate in missing,
  duplicate, nesting, inactive-ancestor and initially-active guards.
- Expanded pseudo-locale roles admit and format with the shared typed formatter.
  Real career snapshots format context, attention, fixtures, results and table rows.
- Assembly layering: PASS, zero upward references and an acyclic graph.
- Documentation consistency: PASS. `git diff --check`: PASS.
- Tracked metadata integrity: PASS. Additional working-tree checks cover all five
  new source metas, GUID uniqueness, scene file-ID uniqueness and the three new
  inactive sibling roots/references.
- The PR gate's full ClientApp suite passed: **207 passed, 3 skipped, 0 failed**.
  The host-free solution build passed with 0 errors. The final targeted run above
  was rebuilt after the last teardown and registration refinements.
- Repository PR policy gate: **PASS**, all 37 suites, **4,379 passed,
  225 skipped, 0 failed**. Command: `bash tools/run-tests-local.sh --pr`
  with the local .NET SDK 8.0.426 and temporary CLI/NuGet directories. This broad
  run preceded the final client teardown/registration/copy refinements, which were
  rebuilt and checked by the 33-test targeted run above.
  Coverage instrumentation was disabled by its existing scope-ceiling fallback
  (35 assemblies exceeded the limit of 8); no coverage percentage is claimed.
  The earlier standalone full ClientApp sweep was stopped as redundant because
  the policy gate already includes that suite. No completion is claimed for the
  stopped sweep.

Version 1.0 (2026-11-06): initial P7b implementation and host handoff.
