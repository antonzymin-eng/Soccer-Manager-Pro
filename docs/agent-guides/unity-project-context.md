# Unity project context

Created: 2026-11-06. Purpose: concise repository onboarding for client work.

- Project root: repository root (`Assets/`, `Packages/`, `ProjectSettings/`); Unity **6000.4.9f1**, revision `f7258d6eebbe`. The pinned Windows editor governs completion; the Linux .NET shim is supplementary.
- Rendering: Built-in Render Pipeline (`GraphicsSettings.m_CustomRenderPipeline = 0`, no SRP dependency). Input: legacy Input Manager (`activeInputHandler = 0`), with `StandaloneInputModule` in the UGUI shell.
- Relevant packages: UGUI 2.0.0, Unity Test Framework 1.6.0, AI Assistant 2.19.0-pre.2. No Unity MCP provider is configured in `Packages/manifest.json`; no connected Editor tools were available during this onboarding.
- Enabled startup scene: `Assets/Scenes/Scene.unity`. `ClientShellBehaviour` composes the flow/coordinator; `S0ScreensBehaviour` owns generated screen UI and its EventSystem. `MatchClientBehaviour` binds generated pitch visuals. The local `Assets/Scripts` junction is untracked; source authority is `src/`.
- One production asmdef per module and a separate Editor test assembly. `client-app` owns testable presentation, navigation, career composition and snapshots. `ui-framework` owns registration/projection/dispatch contracts. `match-client-unity` owns Unity rendering and UGUI bindings and is deliberately excluded from the shim gate.
- `season-save` owns league generation, season progression and the save format. `CareerSession`, `CareerSaveSlots`, `CareerHomeSource`, `NewCareerSetup` and `LoadCareerSource` are the client composition seams. Slot metadata is client-owned; format migration remains parked by D-10.
- P7b uses `CareerScreenPresenter` as the single career worker owner, with cached immutable projections and UI-thread completion polling. Never read the underlying non-thread-safe sources while the worker runs. Teardown cancels queued work and quiesces running work before replacement. Demo and career share one seven-screen flow; interactive managed fixtures remain D-P7-6 work.
- UI uses shared S0 wrapped/scrolled layouts, typed `S0TextFormatter`/production localizer, explicit keyboard traversal and text-scale configuration. Career copy has its own register; the approved S0 match register is unchanged.
- Conventions: constructor injection, private `_camelCase`, one primary public type per file, required headers/XML summaries and append-only version history. Preserve `.meta` GUIDs and list every directly named assembly in its consuming asmdef.
- Validation: narrow generated-project NUnit tests first, then `bash tools/run-tests-local.sh --pr`. `tools/unity-ci/check-meta-integrity.sh` checks tracked metadata. Pinned-editor compile and real-scene interaction/layout checks remain necessary for Unity-only changes.

Version 1.0 (2026-11-06): repository-backed client onboarding; no Editor inspection claimed.

Version 1.1 (2026-11-06): owner accepts safe teardown waiting for P7b; cooperative cancellation is separate. Quit confirms unsaved progress. Worker success and snapshot publication have separate outcomes; refresh a stale view before progress/save. Career localization uses `ui.career.*`.
