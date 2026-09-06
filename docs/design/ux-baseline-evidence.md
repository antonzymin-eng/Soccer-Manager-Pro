# System XI — UX Baseline & Evidence Pack

**Created:** September 6, 2026  
**Last Updated:** September 6, 2026  
**Version:** 0.1  
**Status:** F1.1 CAPABILITY MATRIX — BASELINE COMPLETE; F1.2–F1.5 OPEN  
**Parent:** [`ux-detailed-plan.md`](ux-detailed-plan.md)  
**Evidence snapshot:** `design/ux-foundation` at `8fd178c9e5add5042a9e3e5c5124354aa1b0daf6`

---

## 0. Purpose and rules

This file is UX-A, the Baseline & Evidence Pack defined by the detailed UX plan. It records what the
repository actually supports before UX designs controls around it.

The capability matrix follows these rules:

1. **APPROVED does not mean implemented.** Source is checked before a capability is treated as live.
2. **A domain seam is not a client seam.** Read/action behavior, projection, dispatcher, screen identity,
   navigation, binding and host verification are recorded separately.
3. **A screen identity is not a screen implementation.** `ClientScreens`/`ClientScreenFlow` can be live while
   UGUI and screen-specific adapters remain absent.
4. **Landed code is not host certification.** `MatchClientBehaviour.cs` is landed but remains host-unverified.
5. **Do not infer absence from folder names.** #30 is implemented under `src/season-save/`.
6. **No phantom controls.** A mockup does not create a missing command, view model, screen or navigation edge.
7. **Reference harnesses are not shipping UI.** `match-viewer` and `match-client-web` may validate real data and
   behavior but are not extended into a second product client.

Capability labels are inherited from the detailed plan:

- `LIVE` — production behavior exists.
- `DESIGNABLE` — owning read/action behavior exists, while player-facing presentation remains incomplete.
- `FUTURE-BLOCKED` — required owning-system/client implementation is absent.
- `OUT-OF-EA` — deliberately outside the Early Access cut.
- `UNKNOWN` — evidence is insufficient and may not support a design premise.

Useful qualifiers: `HOST-UNVERIFIED`, `HOST-VERIFIED`, `GATE-COMPILED`, `SPEC-ONLY`, `UNWIRED`,
`REFERENCE-HARNESS`.

---

# 1. F1.1 capability matrix

## 1.1 PM-1 — play one match end-to-end

| Capability | Domain/read seam | Command seam | UI projection / adapter | Screen / navigation | Rendering / binding | Verification | Classification |
|---|---|---|---|---|---|---|---|
| PM-1 navigation graph | N/A | `ClientScreenFlow` guarded methods | N/A | Four `ScreenId`s and five legal edges exist | P5b UGUI not required to prove graph | `client-app` is host-free/test-bearing | `LIVE` |
| Main Menu → Tactics Setup | No domain state required for the PM-1 edge | `ClientScreenFlow.OpenTacticsSetup()` | No Main-Menu-specific source exists in `ui-framework` | `MainMenu` + `TacticsSetup` and edge exist | P5b UGUI absent | Host-free graph exists | `DESIGNABLE / UNWIRED` |
| Tactics Setup boot configuration | `MatchSetup` carries squads, initial team tactics, manager mode/profile and GK-heading flag | Boot-only setup is applied by `MatchSession`; `ClientScreenFlow.StartMatch()` owns the screen transition | No Tactics-Setup-specific `IViewModelSource<T>` or dispatcher exists in `ui-framework` | `TacticsSetup → MatchView` exists; cancel/back exists | P5b UGUI absent | `MatchSetup`/session code is host-free/test-bearing | `DESIGNABLE / UNWIRED` |
| Live Match View frame | `LiveMatchStreamer` → `LiveMatchStreamerFrameSource` → `MatchViewModelSource` → `MatchFrameView` | Read-only path | Concrete `MatchViewModelSource` exists | `MatchView` identity and incoming/outgoing graph edges exist | P4a render decisions landed; P4b Unity binding code landed but host-unverified; P5b shell absent | Host-free surfaces test-bearing; Unity host cert open | `DESIGNABLE / HOST-UNVERIFIED` |
| Live tactical intervention | Engine public seams: team tactic, player tactic, substitution | `MatchTacticsDispatcher` → tick-stamped match command queue | Concrete dispatcher exists | Used from Match View; no new navigation needed | P5b controls absent | Host-free dispatcher/test surface exists | `DESIGNABLE / UNWIRED` |
| Playback controls | `MatchSession` / streamer playback state; P5a control-state decisions in client core | Playback control is separate from manager intent | Host-free control-state logic exists | Match View owns the UX location | P5b controls absent | Host-free/test-bearing | `DESIGNABLE / UNWIRED` |
| Post-match analytics/report data | #37 `MatchAnalyticsAggregator.Build()` → `MatchAnalyticsResult`; web host exposes `BuildReport()` | Read-only | No Post-Match-specific `IViewModelSource<T>` exists in `ui-framework` | `PostMatchReport` identity exists; `MatchView → PostMatchReport → MainMenu` exists | P5b UGUI absent | #37 and web composition are host-free/test-bearing | `DESIGNABLE / UNWIRED` |
| Match presentation depth (#48) | Approved contract only | Approved contract only | Planned `CommentaryFeedView` / `AnimationFrameView`, no source implementation found | Would compose into Match View | No `src/match-presentation/` assembly at snapshot | Spec is APPROVED; runtime absent | `FUTURE-BLOCKED / SPEC-ONLY` |
| Localization / accessibility runtime (#49) | Approved contract only | Client settings/actions not landed | Planned `TacticalDirector.Localization`, `ILocalizer`, boundaries and a11y settings shape; no source implementation found | Settings/reflow behavior not registered in current four-screen graph | No `src/localization/` assembly at snapshot | Spec is APPROVED; runtime absent | `FUTURE-BLOCKED / SPEC-ONLY` |
| Audio runtime (#51) | Approved contract only | Client-shell cue/settings actions not landed | Planned leaf `TacticalDirector.Audio` assembly and shell adapter; no source implementation found | No current screen owns implemented audio settings | No `src/audio/` assembly at snapshot | Spec is APPROVED; runtime absent | `FUTURE-BLOCKED / SPEC-ONLY` |

### PM-1 reading

The PM-1 architecture is not “UI absent.” It is split:

- **live host-free substrate:** navigation graph, match setup, live projection, match command dispatch,
  playback/control-state logic, analytics and browser/reference composition;
- **landed but unverified host binding:** P4b `MatchClientBehaviour.cs`;
- **missing player-facing shell:** P5b four-screen UGUI composition and screen-specific adapters for
  Main Menu, Tactics Setup and Post-Match Report;
- **later presentation layers:** #48/#49/#51 remain spec-only on this snapshot.

Therefore S0 can proceed through evidence, flow, low-fidelity and prototype work without pretending the
shipping Unity shell already exists. Gate I feeds P5b; Gate J waits for the named host evidence.

---

## 1.2 PM-2 / Early Access season loop

| Capability | Domain/read seam | Command seam | UI projection / adapter | Screen / navigation | Rendering / binding | Verification | Classification |
|---|---|---|---|---|---|---|---|
| Season snapshot | `SeasonLoop.View()` → value-copy `SeasonViewModel` | Read-only | No season `IViewModelSource<T>` exists in `ui-framework` | No Career Home/Season `ScreenId` | No player-facing binding | `season-save` is implementation/test-bearing | `DESIGNABLE / UNWIRED` |
| League table | `SeasonViewModel.Table` | Read-only | No #38 adapter | No Career Home/table screen identity | No binding | Domain surface exists | `DESIGNABLE / UNWIRED` |
| Fixtures / calendar / next round | `SeasonViewModel.Fixtures`, `NextRoundIndex`, `RoundCount` | Read-only | No #38 adapter | No Career Home/calendar screen identity | No binding | Domain surface exists | `DESIGNABLE / UNWIRED` |
| Managed-club position / board objective summary | `ManagedClubPosition`, `ObjectiveTargetPosition`, `IsOnTrack`, `JobSecurityPerMille` | Read-only | No #38 adapter | No Career Home/board summary screen identity | No binding | Domain surface exists under `season-save` | `DESIGNABLE / UNWIRED` |
| Advance to/play next round | `SeasonLoop` owns state | `AdvanceAndPlayNextRound(ISquadProvider)` exists | **No season dispatcher and no implemented `AdvanceRound` intent kind** | No PM-2 progression screen/edge | No binding | Domain command exists | `DESIGNABLE / UNWIRED` |
| New generated career/league | `LeagueBootstrap.Generate(...)` + `League` | Construction API exists | No new-game client adapter | No New Game `ScreenId`/navigation | No binding | Domain bootstrap exists | `DESIGNABLE / UNWIRED` |
| Save / restore domain | `SeasonSaveManager` and season/match/world save surfaces | Save/load APIs exist | No player-facing save browser/continue adapter | No Continue/Load/Save screen identity or graph edges | No binding | Save implementation/tests exist | `DESIGNABLE / UNWIRED` |
| Career Home/Season screen identity | Season data is available | Advance/save commands exist at owning layers | Not landed | **No `ScreenId` or legal navigation edge exists** | Not landed | Requires client composition change | `FUTURE-BLOCKED / UNWIRED` |
| Continue / Load / Save screen identity | Save domain exists | Save/load APIs exist | Not landed | **No `ScreenId` or legal navigation edge exists** | Not landed | Requires client composition change | `FUTURE-BLOCKED / UNWIRED` |
| Release settings / localization / a11y | #49 contract only | Client settings store/actions not found | Not landed | No current settings screen identity | Not landed | #49 runtime absent | `FUTURE-BLOCKED / SPEC-ONLY` |

### PM-2 reading

#30 is materially implemented. The season snapshot already exposes the major information needed for an
EA Home/Season surface: table, fixtures, progress, managed-club position and a board-objective/job-security
summary. The season loop also owns the public progression command.

The PM-2 gap is primarily **client composition**:

- no season view-model adapter in `ui-framework`;
- no season dispatcher / `AdvanceRound` implementation in the client intent vocabulary;
- no Career Home/New Game/Continue/Load/Save screen identities;
- no navigation edges for the career shell;
- no Unity binding for those screens.

The matrix deliberately labels the *data/action capability* `DESIGNABLE` while separately labelling the
missing *screen/navigation capability* `FUTURE-BLOCKED`. Treating the whole journey as either “implemented”
or “absent” would hide the actual work.

---

## 1.3 S2 / deeper-management provisional capability gate

This is only enough to prevent UX from designing against imaginary consumers. Each S2 journey still runs
its own Gate A when activated.

| Capability | Verified source state at snapshot | UX posture |
|---|---|---|
| Squad inspection / selection | #27/player-data and lineup-related implementation exists | `DESIGNABLE` when prioritized; re-audit owning read/action seams at Gate A |
| Training / progression | implementation exists in current management layer | `DESIGNABLE` subject to wiring/action audit |
| Injuries / availability | implementation exists in current management layer | `DESIGNABLE` subject to presentation/read audit |
| Board objective / job security summary | `BoardState` / `BoardObjective` data is present under `season-save` and projected into `SeasonViewModel` | `DESIGNABLE` for the existing objective/status summary only |
| Full board / ownership management | current #30 board summary does **not** prove the deeper owning feature set | `FUTURE-BLOCKED` until its owning implementation is verified |
| Transfers / contracts (#31) | approved spec/design exists; symbol-based source search found no transfer/contract implementation | `FUTURE-BLOCKED / SPEC-ONLY` |
| Scouting (#32) | approved spec/design exists; symbol-based source search found no scouting implementation | `FUTURE-BLOCKED / SPEC-ONLY` |
| Club finances / wages (#40) | approved spec/design exists; symbol-based source search found no finance implementation | `FUTURE-BLOCKED / SPEC-ONLY` |
| Staff management (#34) | approved spec/design exists; symbol-based source search found no staff implementation | `FUTURE-BLOCKED / SPEC-ONLY` |

The board row is intentionally narrow. `BoardState` inside #30 supports objective/job-security UX; it does
not establish a general board-management system.

---

# 2. F1.1 findings

## F1-001 — #30 landed, but #38's season intent did not follow it

**Severity for UX planning:** Major dependency finding; not a simulation defect.

`src/ui-framework/IntentKind.cs` still states that `AdvanceRound` is deliberately absent because #30 has
“no src/ assembly” and will be appended when #30 is built. That premise is stale: `SeasonLoop` and
`AdvanceAndPlayNextRound` exist.

The approved #38 appendix already assigns `AdvanceRound` to a **season dispatcher**, not the match-tactics
dispatcher. The correct status is therefore:

- owning season command seam: **LIVE**;
- client intent kind: **absent**;
- season dispatcher: **absent**;
- PM-2 screen/navigation/binding: **absent**.

**Disposition:** `BLOCKED BY DOMAIN/CLIENT IMPLEMENTATION` at the UI-client layer. Do not add an
`AdvanceRound` button to an implementation packet until the owning client work lands. Do not “fix” the
stale comment as a UX-only edit; the enum/dispatcher landing belongs with the client implementation and its
tests.

## F1-002 — only Match View has a concrete #38 projection/dispatcher today

The exact `src/ui-framework/` inventory contains the generic substrate plus:

- `MatchFrameView`;
- `MatchViewModelSource`;
- `MatchTacticsDispatcher`;
- live-frame adapter.

It does **not** contain Main-Menu-, Tactics-Setup-, Post-Match-, Season-, Save- or New-Game-specific
projection/dispatcher types.

**Disposition:** expected implementation gap, not an error. S0/S1 design may proceed only from verified
owning seams and must label the missing adapters.

## F1-003 — current screen graph is PM-1-only by construction

`client-app` owns exactly four current screen identities and five legal moves. Career Home, New Game,
Continue, Load, Save and management destinations are not merely “not drawn”; they are absent from the
current typed navigation graph.

**Disposition:** future client-composition work. F2 must record the current graph unchanged and draw the
career shell separately as target/future navigation.

## F1-004 — #48/#49/#51 are design constraints, not runtime dependencies yet

Their APPROVED specs define future source assemblies and boundaries, but the named runtime assemblies are
absent at this snapshot.

**Disposition:** S0/S1 must reserve layout/state/fallback/caption/localization behavior per the contracts,
but no mockup may imply those systems are already functional.

## F1-005 — source/tracking status can be stale in either direction

This audit has now seen both classes:

- false absence: #30 implemented under a differently named assembly;
- stale absence comment: `IntentKind.cs` still says #30 has no source assembly;
- partial presence: board objective/job-security data exists inside #30 even though deeper board-management
  work does not.

**Disposition:** every Gate A repeats source-level verification; folder names and historical roadmap prose
are discovery aids, not final evidence.

---

# 3. F1.1 exit assessment

F1.1 is complete enough to support the next baseline steps:

- PM-1's real graph and real match read/write seams are identified;
- PM-1's host/binding boundary is explicit;
- PM-2's season/save/bootstrap domain surfaces are separated from its absent client composition;
- the stale #30/`AdvanceRound` client gap is recorded;
- #48/#49/#51 are correctly marked spec-only at this snapshot;
- S2 capabilities are prevented from being treated as implemented merely because a spec/mockup exists.

No high-fidelity UX work is authorized by this file. The next detailed-plan action is **F1.2 — reconcile the
existing mockups against this matrix**, beginning with `Tactics.html` and provisional `Main Menu.html`.

F1.3–F1.5 then complete this same evidence pack with player/task assumptions, PM-1/PM-2 task priority and the
Early Access success floor.

---

## Version History

| Version | Date | Change |
|---|---|---|
| 0.1 | September 6, 2026 | Created UX-A and completed the F1.1 source-grounded capability matrix at branch head `8fd178c`; separated domain/read/command, projection/dispatcher, screen/navigation, binding and host-verification state; recorded the stale `AdvanceRound`/#30 client gap and the spec-only #48/#49/#51 runtime state. |
