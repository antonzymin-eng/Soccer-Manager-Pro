# Unity career-lane context

**Created:** November 3, 2026\
**Version:** 0.5\
**Purpose:** concise repository-backed context for P7b/D-P7-6 work.\
**Source context:** main `1d3cebf3` plus the opt-in P7b binding candidate; Unity validation pending.

- Unity **6000.4.9f1**, revision `f7258d6eebbe`.
- Built-in rendering (`GraphicsSettings.asset` pipeline null), legacy input (`activeInputHandler: 0`).
  UGUI 2.0.0 and Unity Test Framework 1.6.0 are direct registry dependencies; AI Assistant 2.19.0-pre.2
  is installed. No multiplayer framework or Unity MCP package appears in the inspected manifests;
  Editor runtime requires validation on the pinned host.
- Enabled startup scene: `Assets/Scenes/Scene.unity` (`EditorBuildSettings.asset`).
  `Assets/README.md` documents the Scripts junction; first-party C# lives in `src/`, art/prefabs/scenes
  in `Assets/`. Do not modify scene assets merely to inspect the project.
- MatchClientUnity is the thin host binding; ClientApp owns guarded navigation and immutable screen
  projections; UiFramework owns generic substrate. MatchClientCore owns session/command playback.
  SeasonSave owns round/day/save state. Domain code cannot reference presentation assemblies.
- `ClientShellBehaviour` defaults to the four-root PM-1 flow. With `_enableCareer`, three additional
  roots and `ClientCareerCoordinator` compose the seven-screen flow. `S0ScreensBehaviour.Career.cs`
  binds cached career snapshots. Follow the binding README for setup; career runtime/copy/layout
  acceptance and pinned-editor compile remain due. No interactive career-match edge exists yet.
- Read `CLAUDE.md` and `src/CLAUDE.md`; public XML docs, standard headers and append-only version history,
  explicit asmdef references, no random/clock state in domain logic. Keep decisions outside MonoBehaviours.
- Supplemental Linux tests: use the configured .NET SDK, generate projects via
  `tools/dotnet-ci/generate_projects.py`, then targeted generated-project tests. Repository policy
  runner is `tools/run-tests-local.sh`. The Unity binding is excluded from the Linux shim build.
  Pinned Unity compile/EditMode and actual UI checks are required before claiming completion.
- November 4 owner staging decision: PM-2 keeps Career Home round resolution. Interactive career
  matches and coach delegation wait together until the real staff system ships; do not bring an
  early #34 slice forward or invent an interim in-match Exit. The future controller uses the shared
  simulation-thread command queue, with deterministically varied opponent profiles. D-P7-6 is
  PARKED (v0.9 content plus an unreviewed §11 carry-forward list) until #34 T0 is scheduled; do not revise, review or implement it.
- Current direction is [the canonical D-P7-6 amendment](../tracking/d-p7-6-interactive-career-match-amendment-draft.md):
  no in-match saves; choose assistant/coach before kickoff or for the remainder, no user re-entry,
  finish/save before normal exit. Preserve match-local card/participation records and apply them to
  career once at completion. Staff policy and supported takeover APIs are new dependencies; the old
  persisted-round/fold resume proposal is withdrawn. `ActiveMatch` is not a UI-thread read surface;
  preserve W8 B→C isolation. The technical amendment remains PROPOSED.
- Authorities inspected: project settings/manifests, README/guides, #38 specification, client design
  §5-P7, roadmap career lane, S1 audit/protocol, ClientScreenFlow/Visibility, ClientMatchCoordinator,
  ClientShellBehaviour, Career sources, SeasonLoop/SaveCodec and CardLedgerFold.

## Version History

| Version | Date | Notes |
|---|---|---|
| 0.1 | 2026-11-03 | Initial confirmed environment, career integration boundaries and validation limitations. |
| 0.2 | 2026-11-04 | Refresh actual P7b composition and canonical authority; remove session-specific environment instructions. |
| 0.3 | 2026-11-04 | Record owner-deferred interactive career/delegation, continued Home-resolution PM-2 and selected future queue/profile policies. |
| 0.4 | 2026-11-04 | D-P7-6 parked by the owner; do not revise, review or implement it until #34 T0 is scheduled. |
| 0.5 | 2026-11-04 | Parked draft pointer made version-neutral after §11 carry-forward (v0.11). |
