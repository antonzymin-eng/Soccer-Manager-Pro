# Unity career-lane context

**Created:** November 3, 2026\
**Version:** 0.1\
**Purpose:** concise repository-backed context for P7b/D-P7-6 work.\
**Inspected baseline:** `32ff375f7b2b74ff5e2c95e4c19aedb1d737efce`.

- Confirmed root: `/workspace/Soccer-Manager-Pro`; Unity **6000.4.9f1**, revision `f7258d6eebbe`.
- Built-in rendering (`GraphicsSettings.asset` pipeline null), legacy input (`activeInputHandler: 0`).
  UGUI 2.0.0 and Unity Test Framework 1.6.0 are direct registry dependencies; AI Assistant 2.19.0-pre.2
  is installed. No multiplayer framework or Unity MCP package appears in the inspected manifests;
  no callable Unity Editor tools are available in this session. Editor runtime is unverified.
- Enabled startup scene: `Assets/Scenes/Scene.unity` (`EditorBuildSettings.asset`).
  `Assets/README.md` documents the Scripts junction; first-party C# lives in `src/`, art/prefabs/scenes
  in `Assets/`. Do not modify scene assets merely to inspect the project.
- MatchClientUnity is the thin host binding; ClientApp owns guarded navigation and immutable screen
  projections; UiFramework owns generic substrate. MatchClientCore owns session/command playback.
  SeasonSave owns round/day/save state. Domain code cannot reference presentation assemblies.
- Current shell: `ClientShellBehaviour` composes `ClientMatchCoordinator`'s four-registration PM-1 flow
  and `S0ScreensBehaviour`. Career has seven ids/eight moves and host-free sources; it is not wired
  into the Unity shell. The working P7b change adds seven-root validation and optional host-free
  `ClientMatchCoordinator`/`ClientCareerCoordinator` composition; the actual Unity shell still uses
  four roots by default. An opt-in seven-root UGUI adapter now consumes the cached coordinator via
  `S0ScreensBehaviour.Career.cs`; assign three inactive roots and enable it per the binding README.
  Career runtime, copy/layout acceptance and pinned-editor compile remain unverified.
- Read `CLAUDE.md` and `src/CLAUDE.md`; public XML docs, standard headers and append-only version history,
  explicit asmdef references, no random/clock state in domain logic. Keep decisions outside MonoBehaviours.
- Supplemental Linux tests: source `/workspace/.cloud-tools/activate.sh`, generate projects via
  `tools/dotnet-ci/generate_projects.py`, then targeted generated-project tests. Repository policy
  runner is `tools/run-tests-local.sh`. The Unity binding is excluded from the Linux shim build.
  Pinned Unity compile/EditMode and actual UI checks are required before claiming completion.
- Current direction is [delegated completion](../tracking/d-p7-6-delegated-completion-proposal.md):
  no in-match saves; choose assistant/coach before kickoff or for the remainder, no user re-entry,
  finish/save before normal exit. Preserve match-local card/participation records and apply them to
  career once at completion. Staff policy and supported takeover APIs are new dependencies; the old
  persisted-round/fold resume proposal is withdrawn. `ActiveMatch` is not a UI-thread read surface;
  preserve W8 B→C isolation. Live remote baseline was rechecked at `1d3cebf3` on November 3.
- Authorities inspected: project settings/manifests, README/guides, #38 specification, client design
  §5-P7, roadmap career lane, S1 audit/protocol, ClientScreenFlow/Visibility, ClientMatchCoordinator,
  ClientShellBehaviour, Career sources, SeasonLoop/SaveCodec and CardLedgerFold.

## Version History

| Version | Date | Notes |
|---|---|---|
| 0.1 | 2026-11-03 | Initial confirmed environment, career integration boundaries and validation limitations. |
