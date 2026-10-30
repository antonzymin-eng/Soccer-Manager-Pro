# System XI — UX Baseline & Evidence Pack

**Created:** September 6, 2026  
**Last Updated:** September 6, 2026  
**Version:** 0.3  
**Status:** F1 COMPLETE — F2 NEXT  
**Parent:** [`ux-detailed-plan.md`](ux-detailed-plan.md)  
**Source evidence snapshot:** `design/ux-foundation` at `8fd178c9e5add5042a9e3e5c5124354aa1b0daf6`

---

## 0. Purpose and evidence rules

This is UX-A, the Baseline & Evidence Pack. It records what the repository actually supports before UX
creates controls around it and supplies the player/task baseline used by F2–S1.

Rules:

1. **APPROVED does not mean implemented.** Source is checked before a capability is treated as live.
2. **A domain seam is not a client seam.** Read/action behavior, projection, dispatcher, screen identity,
   navigation, binding and host verification are separate facts.
3. **A screen identity is not a screen implementation.** `ClientScreens`/`ClientScreenFlow` can be live while
   UGUI and screen-specific adapters remain absent.
4. **Landed code is not host certification.** `MatchClientBehaviour.cs` is landed but remains host-unverified.
5. **Do not infer absence from folder names.** #30 is implemented under `src/season-save/`.
6. **Do not trust stale comments as current status.** Verify source behavior.
7. **No phantom controls.** A mockup does not create a missing command, view model, screen or navigation edge.
8. **Reference harnesses are not shipping UI.** `match-viewer` and `match-client-web` validate real data/behavior
   but are not extended into a second product client.
9. **Mockups are decomposed by capability.** A useful table, panel or interaction pattern may survive even when
   the screen's data/actions do not; unsupported controls never inherit authority from the visual reference.

Capability labels:

- `LIVE` — production behavior exists.
- `DESIGNABLE` — owning read/action behavior exists while player-facing presentation remains incomplete.
- `FUTURE-BLOCKED` — required owning-system/client implementation is absent.
- `OUT-OF-EA` — deliberately outside the Early Access cut.
- `UNKNOWN` — evidence is insufficient and may not support a design premise.

Useful qualifiers: `HOST-UNVERIFIED`, `HOST-VERIFIED`, `GATE-COMPILED`, `SPEC-ONLY`, `UNWIRED`,
`REFERENCE-HARNESS`.

---

# 1. F1.1 — capability matrix

## 1.1 PM-1 — play one match end-to-end

| Capability | Domain/read seam | Command seam | Client/presentation state | Classification |
|---|---|---|---|---|
| PM-1 navigation graph | N/A | `ClientScreenFlow` guarded methods | Four `ScreenId`s and five legal edges exist; P5b UGUI not required to prove graph | `LIVE` |
| Main Menu → Tactics Setup | No domain state required for edge | `ClientScreenFlow.OpenTacticsSetup()` | Screen IDs/edge exist; no Main-Menu-specific source; P5b UGUI absent | `DESIGNABLE / UNWIRED` |
| Tactics Setup boot configuration | `MatchSetup` carries squads, initial team tactics, manager mode/profile and GK-heading flag | boot setup through `MatchSession`; `ClientScreenFlow.StartMatch()` owns transition | no Tactics-Setup-specific #38 source/dispatcher; P5b absent | `DESIGNABLE / UNWIRED` |
| Live Match View frame | `LiveMatchStreamer` → `LiveMatchStreamerFrameSource` → `MatchViewModelSource` → `MatchFrameView` | read-only path | concrete projection exists; P4a landed; P4b code landed but host-unverified; P5b shell absent | `DESIGNABLE / HOST-UNVERIFIED` |
| Live tactical intervention | engine public team/player tactic + substitution seams | `MatchTacticsDispatcher` → tick-stamped command queue | dispatcher exists; P5b controls absent | `DESIGNABLE / UNWIRED` |
| Playback controls | `MatchSession` / streamer + P5a control-state decisions | playback path separate from manager intent | host-free control logic exists; P5b controls absent | `DESIGNABLE / UNWIRED` |
| Post-match report data | #37 `MatchAnalyticsAggregator.Build()` → `MatchAnalyticsResult`; web host `BuildReport()` | read-only | Post-Match `ScreenId`/edges exist; no report-specific #38 source; P5b absent | `DESIGNABLE / UNWIRED` |
| Match presentation depth (#48) | approved contract only | approved contract only | no `src/match-presentation/` implementation | `FUTURE-BLOCKED / SPEC-ONLY` |
| Localization/accessibility runtime (#49) | approved contract only | client settings/actions not landed | no `src/localization/` implementation; no current settings screen | `FUTURE-BLOCKED / SPEC-ONLY` |
| Audio runtime (#51) | approved contract only | client-shell cue/settings actions not landed | no `src/audio/` implementation | `FUTURE-BLOCKED / SPEC-ONLY` |

### PM-1 interpretation

PM-1 has substantial host-free substrate: typed navigation, setup, live projection, tactical dispatch,
playback decisions, analytics and browser/reference composition. The missing shipping layer is chiefly the P5b
four-screen UGUI composition plus Main Menu/Tactics/Post-Match adapters. P4b is landed but still requires pinned-host
verification. S0 may therefore progress host-free through evidence, flow, low fidelity and prototype work; Gate I
feeds P5b and Gate J waits for B8/B9b/B10 evidence.

---

## 1.2 PM-2 / Early Access season loop

| Capability | Domain/read seam | Command seam | Client/presentation state | Classification |
|---|---|---|---|---|
| Season snapshot | `SeasonLoop.View()` → value-copy `SeasonViewModel` | read-only | no season `IViewModelSource<T>`; no Career Home `ScreenId` | `DESIGNABLE / UNWIRED` |
| League table | `SeasonViewModel.Table` | read-only | no #38 adapter/screen/binding | `DESIGNABLE / UNWIRED` |
| Fixtures/calendar/round state | `SeasonViewModel.Fixtures`, `NextRoundIndex`, `RoundCount` | read-only | no #38 adapter/screen/binding | `DESIGNABLE / UNWIRED` |
| Managed-club position / board objective | `ManagedClubPosition`, `ObjectiveTargetPosition`, `IsOnTrack`, `JobSecurityPerMille` | read-only | no Career Home composition | `DESIGNABLE / UNWIRED` |
| Advance/play next round | `SeasonLoop` owns state | `AdvanceAndPlayNextRound(ISquadProvider)` exists | no season dispatcher and no implemented `AdvanceRound` intent kind | `DESIGNABLE / UNWIRED` |
| Generated new career/league | `LeagueBootstrap.Generate(...)` + `League` | construction API exists | no New Game client adapter or screen/navigation | `DESIGNABLE / UNWIRED` domain; screen `FUTURE-BLOCKED` |
| Save/restore domain | `SeasonSaveManager` + season/match/world save surfaces | save/load APIs exist | no player-facing Continue/Load/Save adapter or screen/navigation | `DESIGNABLE / UNWIRED` domain; screen `FUTURE-BLOCKED` |
| Career Home/Season identity | season data available | advance/save commands exist at owners | no `ScreenId` or legal navigation edge | `FUTURE-BLOCKED / UNWIRED` |
| Continue/Load/Save identity | save domain available | save/load APIs exist | no `ScreenId` or legal navigation edge | `FUTURE-BLOCKED / UNWIRED` |
| Release settings/a11y/localization | #49 contract only | settings store/actions not found | no settings screen/runtime | `FUTURE-BLOCKED / SPEC-ONLY` |

### PM-2 interpretation

#30 is materially implemented. The season snapshot already exposes the core information for an EA Home/Season
surface: table, fixtures, progress, managed-club position and board-objective/job-security summary. The season loop owns
the public progression command, and save/bootstrap domains exist.

The PM-2 gap is primarily **client composition**:

- no season #38 adapter;
- no season dispatcher / `AdvanceRound` implementation in the client intent vocabulary;
- no Career Home/New Game/Continue/Load/Save screen identities;
- no career-shell navigation edges;
- no Unity binding for those screens.

The data/action capability can therefore be `DESIGNABLE` while the missing screen/navigation capability remains
`FUTURE-BLOCKED`. The journey must never be globally described as either “implemented” or “absent.”

---

## 1.3 S2 / deeper-management provisional gate

This is only enough to prevent UX from designing against imaginary consumers. Every S2 journey still performs its
own Gate A when activated.

| Capability | Verified source state | UX posture |
|---|---|---|
| Squad inspection/selection | #27/player-data and lineup-related implementation exists | `DESIGNABLE`; re-audit owning read/action seams at Gate A |
| Training/progression | `TrainingViewModel` exposes Focus/Condition/TrainingFatigue; `TrainingSchedule.TrySetFocus` is a real write | `DESIGNABLE` for verified per-player state/focus only |
| Injuries/availability | implementation exists in current management layer | `DESIGNABLE` subject to presentation/read audit |
| Board objective/job security | `BoardState`/`BoardObjective` present under `season-save` and projected into `SeasonViewModel` | `DESIGNABLE` for objective/status summary only |
| Full board/ownership management | #30 summary does not prove deeper board behavior | `FUTURE-BLOCKED` until owning implementation is verified |
| Transfers/contracts (#31) | approved spec/design; symbol-based source search found no implementation | `FUTURE-BLOCKED / SPEC-ONLY` |
| Scouting (#32) | approved spec/design; symbol-based source search found no implementation | `FUTURE-BLOCKED / SPEC-ONLY` |
| Club finances/wages (#40) | approved spec/design; symbol-based source search found no implementation | `FUTURE-BLOCKED / SPEC-ONLY` |
| Staff management (#34) | approved spec/design; symbol-based source search found no implementation | `FUTURE-BLOCKED / SPEC-ONLY` |

The board row is intentionally narrow. Objective/job-security UX is not proof of a general board-management system.

---

# 2. F1.1 findings

- **F1-001 — #30 landed, but #38's season intent did not follow it.** `IntentKind.cs` still says `AdvanceRound`
  is absent because #30 has no source assembly. The premise is stale. Season command seam: `LIVE`; client intent:
  absent; season dispatcher: absent; PM-2 screen/navigation/binding: absent. Disposition:
  `BLOCKED BY DOMAIN/CLIENT IMPLEMENTATION` at the client layer; UX does not patch the enum alone.
- **F1-002 — only Match View has a concrete #38 projection/dispatcher today.** Generic substrate exists, plus
  `MatchFrameView`, `MatchViewModelSource`, `MatchTacticsDispatcher` and the live-frame adapter. Main Menu, Tactics,
  Post-Match, Season, Save and New Game do not have equivalent specific sources/dispatchers.
- **F1-003 — current typed graph is PM-1-only.** Career Home, New Game, Continue, Load, Save and management
  destinations are absent from the current `ScreenId`/edge graph.
- **F1-004 — #48/#49/#51 constrain design but are not runtime capabilities yet.** Their approved contracts may
  reserve space/states/fallbacks, but UX may not imply those systems are live.
- **F1-005 — status can be stale in either direction.** #30 was a false absence; `IntentKind.cs` carries a stale
  absence comment; board data is a partial presence hidden inside #30. Gate A therefore re-verifies source every time.

---

# 3. F1.2 — existing-reference reconciliation

## 3.1 Disposition vocabulary

- `KEEP` — useful authority for the stated visual/interaction concern.
- `REVISE` — useful structure, but behavior/data/navigation must be reconciled before reuse.
- `REFERENCE ONLY` — visual/research precedent only; cannot drive implementation handoff.
- `DEFER` — useful later, outside S0/S1 priority.
- `RETIRE` — represented behavior should not carry forward unless owning implementation explicitly reintroduces it.

## 3.2 Reference matrix

| Reference | Disposition | Keep | Required constraint |
|---|---|---|---|
| `System XI - Design System.html` | `KEEP + REVISE IN F3` | `touchline`, dense analyst-tool philosophy, tabular numerals, quiet chrome, shared components, neutral surfaces, brand accent | token values are references, not runtime constants; no color-only meaning; add/verify focus, pseudo-locale, max text, glyph fallback; Google Fonts are not a shipping contract |
| `Desktop Guardrails.html` | `KEEP + REVISE IN F3` | desktop-first density, compact controls, table-first information design | fixed-stage scaling is a mockup technique; F3/E/J must prove smallest desktop, 1920×1080 and high-resolution/ultrawide behavior |
| `Command Palette.html` | `REFERENCE ONLY / DEFER` | keyboard-first search, scope, arrows/Enter/Esc, grouping/focus | current index advertises unimplemented screens/actions; return only after F2/S1 establishes real destinations/actions |
| `Main Menu.html` | `REVISE` for S0 | hierarchy, restrained branding, art fallback, disabled-reason pattern, focus/reduced-motion/keyboard patterns | S0 real forward path is `MainMenu → TacticsSetup`; Continue/New Game/Load/Settings are future client surfaces until implemented |
| `Tactics.html` | `REVISE` heavily for S0 | formation/pitch + squad + context layout; dense team controls; Role/Duty/Instructions concept | every control maps to verified tactical semantics; strip speculative roles/instructions, scouting advice, template workflows, career chrome and unsupported set-piece editing |
| `Club.html` | `REVISE` — strongest S1 Home candidate | club identity, league position, next fixture, season progress, attention hierarchy, board-objective/job-security summary | remove unsupported finance/contracts/deep medical/full board/multi-competition/global chrome until backed |
| `World.html` | `REVISE / SPLIT` | dense standings, managed-club highlight, form/position readability | managed-league table can serve S1; broader world/multi-league/qualification browser remains later |
| `Squad Screen.html` | `REVISE / DEFER` | dense sortable roster table, detail rail, filtering, keyboard table pattern | split #27 roster from #41 medical, dynamics, #31 contracts, #32 shortlist, #40 wages/value; no mixed-spec screen approval |
| `Training Screen.html` | `REVISE / DEFER` | per-player state table and focus interaction | verified surface is Focus/Condition/Fatigue + `TrySetFocus`; weekly session editor/auto-schedule/copy/apply/group systems are reference-only |
| `Scouting Screen.html` | `REFERENCE ONLY / FUTURE-BLOCKED` | report-table density, progressive-knowledge pattern, side rail/coverage visualization | #32 implementation absent; no scout network/assignments/reports/shortlist actions may drive handoff |
| `Transfers.html` | `REFERENCE ONLY / FUTURE-BLOCKED` | deal-pipeline/state/deadline/detail patterns | #31 implementation absent; no bids/negotiation/clauses/medical-to-signing/budgets/actions may drive handoff |
| `Club Finances.html` | `REFERENCE ONLY / FUTURE-BLOCKED` | KPI, P&L, bar/chart/table patterns | #40 implementation absent; financial values/actions are synthetic references only |
| `Club Staff.html` | `REFERENCE ONLY / FUTURE-BLOCKED` | roster/card/department patterns | #34 implementation absent; hiring/contracts/wages/staff attributes/budgets are not runtime capabilities |
| `Club Board Room.html` | `SPLIT` | objective/status layout may inform #30 summary | chairman/confidence narrative/directives/meetings/requests/financial targets are not established |
| `Club History.html` | `DEFER / REFERENCE ONLY` | honours/history/table/record patterns | #30/#37 do not establish the durable multi-season aggregate represented here |

## 3.3 Tactics semantic reconciliation

The current `TeamTactic` manager axes include Mentality, Formation, Tempo, Width, Passing, Pressing, Line of
Engagement, Defensive Line, Defensive Width, Transition Won/Lost, Offside Trap, press-trigger mask, Focus Play,
GK Distribution, Time Wasting, Marking Orientation, Dismark Intensity, Build-Up Structure and Rotation Freedom.

`PlayerTactic` genuinely owns Role + Duty + Instructions, but the mockup overstates all three:

- `PlayerRole`: `Default`, `Poacher`, `DeepLyingPlaymaker`, `BallWinningMid`, `InsideForward`, `TargetMan`;
- `Duty`: Defend / Support / Attack;
- `PlayerInstructions`: fixed pass/shoot/dribble/cross/positioning/closing-down biases, tight marking, optional
  man-mark target and set-piece duty flags — not arbitrary FM-style toggle lists.

S0-A must map every retained interactive control to these verified types/members. Unmatched controls are removed or
explicitly future-blocked before S0-C.

## 3.4 Training semantic reconciliation

`TrainingViewModel` exposes per-player Focus, Condition and TrainingFatigue. `TrainingSchedule.TrySetFocus` is the
real focus mutation. `TrainingFocus` currently contains Balanced, Rest, Fitness, Technical, Physical and Tactical.

The Training mockup's player-state/focus concept is therefore reusable. Its editable day-by-day session calendar,
named session recipes, Auto-schedule, Copy Last Week, Apply Changes, arbitrary per-attribute focuses, group/staff/
analysis subsystems and “optimal load” recommendations are not established by this seam.

## 3.5 F1.2 findings

- **F1-006 — Tactics is a real seam hidden inside an overgrown mockup.** Preserve the core interaction model;
  remove semantic overbreadth.
- **F1-007 — repeated career chrome is target architecture, not current navigation.** Club/Squad/Tactics/Training/
  Scouting/Transfers/Staff/World/Inbox/date/Continue repetition does not create `ScreenId`s or edges.
- **F1-008 — one visual screen routinely spans several owning specs.** Future Gate A approval occurs by module/control,
  not by screenshot.
- **F1-009 — foundation patterns survive; implementation claims do not.** `touchline`, density, table/detail rails,
  compact KPIs, keyboard patterns and fallbacks are useful; fixed HTML scaling, hosted fonts, color-only semantics and
  synthetic data are not client contracts.

---

# 4. F1.3 — lightweight player/task assumptions

These are hypotheses to test, not fictional personas and not market segmentation claims.

| Hypothesis | Likely prior knowledge / expectation | Likely task behavior | Main UX failure risk | Design implication | Validation signal |
|---|---|---|---|---|---|
| Experienced management-sim player | understands football-management vocabulary; expects dense comparative information, fast navigation, keyboard efficiency and persistent context | scans tables, compares several values, moves quickly between setup/state/detail, expects obvious shortcuts | hiding information behind decorative presentation; excessive modal depth; familiar terms behaving unexpectedly | preserve density and stable navigation; expose precise state and keyboard paths; avoid tutorial friction on routine actions | completes core tasks quickly without hunting; can predict where squad/tactics/season information lives |
| Football-literate newcomer | understands football, formations, positions and match outcomes but may not know management-sim UI conventions or internal system names | follows primary actions and contextual explanations; reads labels before shortcuts; needs to know what matters next | too many equal-weight controls; unexplained jargon; no visible next action; disabled controls with no reason | explicit hierarchy, progressive disclosure, plain-language labels/help, visible readiness/blocked reasons and season next-step model | can complete PM-1/PM-2 critical tasks without prior knowledge of System XI or developer concepts |
| Efficiency / power user | values low interaction cost, keyboard/mouse parity, persistent filters/context and rapid repeated loops | repeats season actions, uses shortcuts, scans deltas/exceptions rather than rereading whole screens | repetitive confirmations, pointer-only workflows, unstable focus, navigation resets, excessive animation | predictable focus order, direct keyboard paths, dense delta/attention states, limited destructive confirmation, preserve context where domain permits | repeated-loop completion time drops without increasing errors; keyboard-only route remains complete |

### F1.3 implications shared by all three

- The player should never need to understand internal spec/assembly names to progress.
- The critical next action must be visible without flattening every available action to equal prominence.
- Dense information is acceptable when structure is stable and comparison is supported.
- Disabled/unavailable actions require a reason when the reason affects the player's next decision.
- Onboarding should explain **decisions and consequences**, not teach every control upfront.
- Keyboard efficiency is a product requirement for frequent desktop workflows, but mouse use must remain complete.
- These assumptions become evidence only after F4/Gate G participant testing; until then they remain explicit hypotheses.

---

# 5. F1.4 — PM-1/PM-2 task hierarchy

Priority definitions:

- `Critical` — the playable/Early Access loop cannot be completed correctly without it.
- `High` — frequent decision or major comprehension task; failure seriously degrades the loop.
- `Medium` — useful supporting task with a workable lower-depth path.
- `Low` — optional depth/polish for the current release slice.

## 5.1 PM-1 tasks

| Task | Goal | Frequency | Failure impact | Required information | Required action | Owning surface/system | Priority | Current capability |
|---|---|---:|---|---|---|---|---|---|
| Enter match setup | reach the supported pre-match context | every managed match | cannot begin PM-1 | current mode / primary action | open Tactics Setup | `ClientScreenFlow` | `Critical` | graph `LIVE`; UI `DESIGNABLE` |
| Understand/select XI and formation state | know who/shape is being used | every managed match | wrong setup or inability to validate | squads, positions, formation, selection/readiness | setup selection/configuration as supported | match setup + lineup/tactical owners | `Critical` | domain substrate exists; exact UI action audit owed S0-A |
| Configure team tactics | establish intended team behavior | every managed match | core management decision unavailable | real `TeamTactic` axes/current values | set supported tactic values | tactical instructions / setup path | `Critical` | domain semantics `LIVE`; setup adapter/UI missing |
| Configure player tactic where needed | assign supported role/duty/instructions | frequent | important tactical control unavailable/misrepresented | actual role/duty/instruction vocabulary | set supported player tactic | tactical instructions | `High` | domain semantics `LIVE`; setup adapter/UI missing |
| Know why match cannot start | resolve invalid/not-ready state | when invalid | hard blocker with no recovery path | validation failure/readiness reason | correct setup or cancel | setup/client validation | `Critical` | presentation state not yet designed |
| Start or cancel setup | commit setup or return safely | every setup | cannot enter match / cannot back out | readiness + destination | Start / Cancel | `ClientScreenFlow` + session boot | `Critical` | transition seams `LIVE`; UI missing |
| Read score/time/match state | know current match situation | continuous | match becomes unintelligible | score, clock, phase/state | read only | `MatchFrameView` / streamer | `Critical` | projection `LIVE`; P5b/host open |
| Understand live events/field state | know what is happening well enough to manage | continuous | cannot make informed interventions | real frame/event information | inspect/read | match client; #48 later deepens presentation | `High` | base frame `DESIGNABLE`; #48 `FUTURE-BLOCKED` |
| Control playback | observe at workable pace/state | frequent | high friction / inability to inspect live match | current playback state + available controls | supported playback command | P5a/client core | `High` | host-free logic exists; UI missing |
| Change team tactic live | influence match | situational but core | key manager agency missing | current state + supported tactic values | dispatch team tactic | `MatchTacticsDispatcher` | `Critical` | dispatcher `LIVE`; UI missing |
| Change player tactic live | influence individual behavior | situational | reduced manager agency | player/current supported tactic | dispatch player tactic | `MatchTacticsDispatcher` | `High` | dispatcher `LIVE`; UI missing |
| Make substitution | replace player legally | situational but expected | core match management incomplete | eligible players/state | substitution intent | `MatchTacticsDispatcher` | `Critical` | dispatcher `LIVE`; UI missing |
| Recognize full time and transition | know match ended / move forward | every match | stuck or ambiguous end state | full-time state/result | proceed to report | streamer/session + `ClientScreenFlow` | `Critical` | graph/data exist; UI missing |
| Understand result/core stats | explain what happened at result level | every match | loop gives no useful closure | score + #37-backed statistics | inspect/read | match analytics #37 | `Critical` | analytics `LIVE`; report adapter/UI missing |
| Return from report | complete PM-1 loop | every match | trapped at report | destination/action | return | `ClientScreenFlow` | `Critical` | current target Main Menu edge `LIVE`; UI missing |

## 5.2 PM-2 / Early Access tasks

| Task | Goal | Frequency | Failure impact | Required information | Required action | Owning surface/system | Priority | Current capability |
|---|---|---:|---|---|---|---|---|---|
| Start or resume supported career | enter persistent season loop | session start | no EA product loop | available start/save state | new/resume/load as product defines | bootstrap/save + future client shell | `Critical` | domain `DESIGNABLE`; client `FUTURE-BLOCKED` |
| Understand current season state | orient after start/return | every session/round | player cannot plan | league position, round/fixture context, objective status | read | `SeasonViewModel` + future Home | `Critical` | domain `DESIGNABLE`; Home missing |
| Know next fixture | know immediate goal | every round | progression/prep unclear | fixture/opponent/round state | inspect/reach prep | #30 + future Home/nav | `Critical` | data `DESIGNABLE`; composition missing |
| Know league position/table context | judge season performance | every round | season lacks understandable stakes | table/managed position | read/inspect | #30 | `High` | data `DESIGNABLE`; UI missing |
| Identify attention/blocking issues | know what must be handled before progress | frequent | player guesses why progression fails or misses important state | real blockers/warnings only | inspect/resolve owning action | Home composition + owning systems | `High` | partial data; target attention model not landed |
| Reach match preparation | move from season to PM-1 | every managed fixture | season cannot connect to playable match | next managed fixture/readiness | navigate to setup | future career graph + existing PM-1 graph | `Critical` | new career edge missing |
| Advance/play next round correctly | progress time/competition through public semantics | every round | season loop cannot progress | next-round state + what command will do | `AdvanceRound`/owned season dispatch | #30 + future season dispatcher | `Critical` | domain command `LIVE`; client intent/dispatcher absent |
| Return from post-match to season context | continue career after match | every managed fixture | PM-1 and PM-2 remain disconnected | updated season state | return to Home/Season | future career graph | `Critical` | current graph returns Main Menu; target edge absent |
| See changed table/result after round | understand consequence of progression | every round | progression feels opaque | updated table/fixture/result state | read | #30 + Home/table UI | `High` | data exists; UI missing |
| Save supported career | preserve progress | session end / chosen cadence | unacceptable persistence failure | save state/success/error | save | `SeasonSaveManager` + future client adapter | `Critical` | domain exists; client UI missing |
| Quit and resume | leave safely and continue later | normal session boundary | EA loop not practically usable | save status / available resume target | quit/resume/load | save/bootstrap + shell | `Critical` | domain exists; shell missing |
| Find release settings/accessibility | configure usable presentation/input | setup + occasional | some players cannot use product comfortably | real setting values/effects | change settings | #49 + client shell | `Critical` for required a11y; otherwise High | #49 runtime `FUTURE-BLOCKED` |

### F1.4 release priority consequence

P0/P1 work is dominated by **connective client surfaces**, not by adding deeper management screens. The highest-risk
EA chain is:

`Launch/start/resume → Career Home/Season → next fixture/prep → PM-1 match → report → Career Home/Season → advance/save`

Any P2 management depth that delays this chain violates the high-level release cut.

---

# 6. F1.5 — Early Access success-floor reconciliation

The detailed plan's EA floor is now mapped to evidence rather than aspiration.

| EA success condition | What exists now | Missing client/runtime work | Current release status |
|---|---|---|---|
| Enter/start/resume supported mode | league bootstrap and save/load domain exist; PM-1 Main Menu identity exists | New Game/Continue/Load projections, screen IDs, navigation, binding and exact product start/resume policy | `NOT YET PLAYER-COMPLETE` |
| Know what needs attention next | `SeasonViewModel` exposes core season/table/fixture/objective state | Career Home composition, attention rules and any real blockers from owning systems | `DESIGNABLE` |
| Prepare and start a match | `MatchSetup`, tactics domain and PM-1 setup/start/cancel graph exist | Tactics Setup adapter, validated low-fi states and P5b screen | `DESIGNABLE` |
| Read live match state | `MatchViewModelSource`/`MatchFrameView` and host-free render decisions exist | P5b Match View plus P4b/B10 host evidence; #48 deeper presentation later | `DESIGNABLE / HOST-UNVERIFIED` |
| Use supported match interventions | `MatchTacticsDispatcher` owns team/player tactic + substitution intents | visible P5b controls, focus/feedback/refusal states, host verification | `DESIGNABLE` |
| Understand result/core statistics | #37 analytics and report-building seam exist | Post-Match-specific #38 source/composition and P5b report screen | `DESIGNABLE` |
| Return to season context | current PM-1 graph returns report → Main Menu | Career Home `ScreenId` and report→career return semantics/navigation | `FUTURE-BLOCKED` |
| Advance correctly | `SeasonLoop.AdvanceAndPlayNextRound` exists | `AdvanceRound` intent, season dispatcher, Home control/state/feedback | `DESIGNABLE` domain; client `FUTURE-BLOCKED` |
| Save/quit/resume | `SeasonSaveManager` and save contents/codecs exist | user-facing save/resume flow, failure states, shell navigation and exact product promise | `DESIGNABLE` domain; client `FUTURE-BLOCKED` |
| Find release-critical settings/accessibility | #49 approved contract/design exists | localization/a11y runtime, settings values/store/application and settings screen/navigation | `FUTURE-BLOCKED / SPEC-ONLY` |

## 6.1 EA readiness interpretation

The project is **not blocked on inventing a season simulation** for the UX loop. The core season and match substrates
are present. The UX/release blocker is assembling those substrates into a coherent player-facing client without
creating phantom behavior.

The minimum EA client therefore needs, before deeper management breadth:

1. launch/start/resume flow consistent with actual product/save semantics;
2. one Career Home/Season surface answering:
   - What changed?
   - What needs attention?
   - What is next?
   - What can I do before then?
   - How do I progress?
3. a verified path into the existing PM-1 setup/match/report journey;
4. a return path from report to season context;
5. a real season dispatcher for progression;
6. save/quit/resume presentation;
7. release-critical settings/accessibility runtime and presentation.

No transfer, scouting, finance, staff, deep board or historical screen is required to prove that core loop.

---

# 7. F1 consolidated findings

| ID | Finding | Consequence |
|---|---|---|
| F1-001 | #30 landed but `AdvanceRound` client intent/season dispatcher did not follow it | owning client implementation dependency; no phantom Advance button |
| F1-002 | only Match View has a concrete #38 screen projection/dispatcher today | other screens need explicit adapters rather than mockup-driven assumptions |
| F1-003 | typed graph is PM-1-only | F2 records current graph unchanged and maps future career graph separately |
| F1-004 | #48/#49/#51 are approved constraints but runtime-absent | design for their contracts; never present them as live |
| F1-005 | folder names/comments/old roadmaps can misstate source status | Gate A re-verifies symbols/source every journey |
| F1-006 | Tactics contains a real implemented seam inside an overgrown reference | preserve core model; remove speculative role/instruction/scouting/template behavior |
| F1-007 | repeated career chrome is target architecture, not current navigation | F2 may consider it only as future shell evidence |
| F1-008 | one visual screen spans several owning specs | approve modules/controls by capability, not screenshots wholesale |
| F1-009 | foundation patterns survive while synthetic implementation claims do not | F3 audits `touchline` patterns against renderer/a11y/localization extremes |
| F1-010 | PM-2's principal gap is connective client composition, not season-domain absence | prioritize Home/navigation/dispatcher/save/start-return shell over deeper management breadth |
| F1-011 | newcomer clarity and power-user efficiency are not opposing goals if hierarchy is stable | task-first hierarchy + progressive disclosure + keyboard/mouse parity become validation targets |

---

# 8. F1 exit

F1 content is complete.

Exit conditions satisfied:

- current PM-1/PM-2 capabilities are classified by domain, command, projection, navigation, binding and verification;
- no `UNKNOWN` capability is being used as a current S0/S1 premise;
- every existing HTML reference has a disposition and capability boundary;
- player hypotheses are explicit and testable;
- PM-1/PM-2 tasks are prioritized against failure impact and current capability;
- the EA success floor is mapped to concrete landed seams and missing client/runtime work;
- the next release-critical problem is stated as client composition, not vague “more UI.”

## 8.1 Open product decisions — not F2 blockers

These remain owner/product decisions before their relevant implementation handoff, but they do **not** block F2:

- exact EA New Game options;
- autosave/manual-save and Continue/Load promise;
- exact user-facing advance semantics when multiple progression choices are eventually exposed;
- exact release settings/accessibility scope beyond #49's required contracts.

Gate-G participant identification remains F4 work, not an F1/F2 prerequisite.

## 8.2 Next action

**F2 — Current-vs-target experience architecture.**

Order:

1. record the existing four-screen/five-edge PM-1 graph verbatim;
2. build a separate target PM-2 career-shell map using only F1-classified capabilities;
3. define page/modal/drawer/subnavigation, attention, blocked-action and contextual-help rules not already owned by
   current navigation code;
4. preserve explicit `CURRENT` versus `TARGET / FUTURE-BLOCKED` labels.

No high-fidelity screen is authorized by F1 completion. Low-fidelity architecture/flow remains next.

---

## Version History

| Version | Date | Change |
|---|---|---|
| 0.1 | September 6, 2026 | Created UX-A and completed the F1.1 source-grounded capability matrix at branch head `8fd178c`; separated domain/read/command, projection/dispatcher, screen/navigation, binding and host-verification state; recorded the stale `AdvanceRound`/#30 client gap and spec-only #48/#49/#51 runtime state. |
| 0.2 | September 6, 2026 | Completed F1.2 reconciliation of all landed UX mockup references; verified actual player-tactic and training surfaces and separated reusable patterns from unsupported behavior. |
| 0.3 | September 6, 2026 | Completed F1.3–F1.5: lightweight player hypotheses, PM-1/PM-2 task hierarchy, evidence-backed Early Access success-floor reconciliation, consolidated findings and F1 exit. Also compacted repeated F1.1/F1.2 prose without changing their decisions. |
