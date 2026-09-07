# System XI — UX Baseline & Evidence Pack

**Created:** September 6, 2026  
**Last Updated:** September 6, 2026  
**Version:** 0.2  
**Status:** F1.1–F1.2 COMPLETE; F1.3–F1.5 OPEN  
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
8. **Mockups are decomposed by capability.** A useful table, panel or interaction pattern may survive even when
   the screen's data or actions do not; unsupported controls never inherit authority from the visual reference.

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
| Training / progression | `src/training-system/` exists; `TrainingViewModel` exposes focus/condition/fatigue and `TrainingSchedule.TrySetFocus` is a real per-player write | `DESIGNABLE` for the verified per-player training state/focus only; broader session-planning controls require their own seam |
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

---

# 4. F1.2 existing-reference reconciliation

## 4.1 Reconciliation rule

Disposition applies to the **reference**, not automatically to every primitive inside it:

- `KEEP` — reference remains a useful authority for the stated visual/interaction concern.
- `REVISE` — useful structure exists, but behavior/data/navigation must be reconciled before reuse.
- `REFERENCE ONLY` — preserve as visual/research precedent; it may not drive an implementation handoff.
- `DEFER` — useful later, but outside S0/S1 priority.
- `RETIRE` — the represented product behavior should not be carried forward unless an owning spec/client change
  explicitly reintroduces it.

## 4.2 Foundation references

| Reference | Disposition | Keep | Revise / constrain | Capability gaps / gate implication |
|---|---|---|---|---|
| `System XI - Design System.html` | `KEEP + REVISE IN F3` | `touchline`, dense analyst-tool philosophy, tabular numerals, quiet chrome, shared component vocabulary, neutral surfaces, brand accent | Treat token values as reference rather than runtime constants; status meaning must never rely on color alone; font/glyph fallback, pseudo-locale, max text scale and focus states must be added/verified | #49 runtime is absent, so accessibility/localization claims are design constraints until F3/E/J; Google Fonts are not a shipping font contract |
| `Desktop Guardrails.html` | `KEEP + REVISE IN F3` | desktop-first density principles, compact controls, table-first information design, do/don't examples | fixed-stage scaling is a mockup technique, not shipping responsive behavior; validate smallest supported desktop, 1920×1080 and high-resolution/ultrawide, pseudo-locale and max text | F3 defines the supported renderer behavior; Gate E/J prove it |
| `Command Palette.html` | `REFERENCE ONLY / DEFER` | keyboard-first search pattern, visible scope, arrow/Enter/Esc behavior, result grouping, focus concept | discard its current index as capability truth: it advertises unimplemented screens/actions, including transfers, contracts, scouting, finance, staff, inbox, settings, save, advance-day and management commands | no current typed career-shell destinations/command index; pattern may return after F2/S1 establishes real destinations/actions |

## 4.3 PM-1 references

### `Main Menu.html`

**Disposition:** `REVISE` for S0; keep visual hierarchy, not current action inventory.

**Keep:** 

- System XI / football-management identity;
- `touchline` hierarchy and restrained branding;
- art-independent key-art slot and fallback;
- explicit disabled-state reason pattern;
- focus-visible styling, reduced-motion treatment and keyboard hint pattern;
- primary/secondary action separation.

**Revise:** 

- S0's real current forward path is `MainMenu → TacticsSetup`; the S0 low-fidelity version therefore needs a
  supported PM-1 entry such as Play Match / Match Setup rather than presenting `New Game` as implemented;
- `Continue`, `New Game`, `Load Game` and `Settings` are PM-2/future client surfaces until their screen/dispatch
  paths land;
- Credits/Exit are shell concerns whose runtime ownership must be identified before handoff;
- remove “Early Access UX Reference” and other authoring labels from any future production reference.

**Gate implication:** S0-C may reuse the structure, but S0-H must not inherit any future action as live.

### `Tactics.html`

**Disposition:** `REVISE` heavily for S0; this is the most valuable existing screen reference.

**Keep:** 

- formation/pitch + squad + selected-player/context layout concept;
- dense team-tactic controls;
- team mentality/formation/tempo/width/passing/pressing family as a design direction where the exact control maps
  to a real `TeamTactic` field;
- player role + Defend/Support/Attack duty + individual-instruction concept, because `PlayerTactic` genuinely owns
  those three dimensions;
- compact information hierarchy and a clear Start/Cancel-ready setup surface as the target S0 task.

**Revise to actual semantics:** 

- `TeamTactic` currently exposes the verified manager axes: Mentality, Formation, Tempo, Width, Passing, Pressing,
  Line of Engagement, Defensive Line, Defensive Width, Transition Won/Lost, Offside Trap, press-trigger mask,
  Focus Play, GK Distribution, Time Wasting, Marking Orientation, Dismark Intensity, Build-Up Structure and Rotation
  Freedom;
- `PlayerRole` is currently the curated roster `Default`, `Poacher`, `DeepLyingPlaymaker`, `BallWinningMid`,
  `InsideForward`, `TargetMan` — not the dozens of FM-like roles in the mockup;
- `Duty` is exactly Defend / Support / Attack;
- `PlayerInstructions` is a fixed set of pass/shoot/dribble/cross/positioning/closing-down biases plus tight marking,
  optional man-mark target and set-piece duty flags — not arbitrary role-specific toggle lists;
- replace career chrome/date/Continue/Inbox with the current PM-1 setup context for S0;
- add explicit readiness, invalid setup, command refusal and cancel states.

**Reference-only / remove from S0 unless a seam is separately proven:** 

- opponent “defensive alerts”, “our tactical plan” and causal/recommendation copy;
- elaborate opponent scouting intelligence;
- tactical template Import / Save Current workflow;
- elaborate set-piece routine editor beyond the implemented set-piece duty inputs;
- any role/instruction name not present in the verified tactical contracts;
- auto-save/career status chrome.

**Gate implication:** every interactive control is remapped during S0-A; S0-C designs only the verified subset.

## 4.4 PM-2 / career-shell candidates

### `Club.html`

**Disposition:** `REVISE` as the strongest existing starting point for the future S1 Career Home/Season surface.

**Keep for S1 concept:** managed club identity, league position/table context, next fixture, current-season progress,
attention/alert hierarchy, compact quick links, board-objective/job-security summary pattern.

**Revise / remove until backed:** multi-competition claims beyond the implemented competition scope, finance/wage
cards, transfer/contract alerts/actions, deep injury/medical advice, full board-confidence narrative, unsupported quick
links and global career chrome.

**Gate implication:** S1-A should map the five Career Home questions to verified `SeasonViewModel` fields first, then
borrow modules from this reference only where data/action ownership exists.

### `World.html`

**Disposition:** `REVISE / SPLIT`.

**Keep:** dense standings table, managed-club highlighting, form/position readability, competition/list navigation
pattern.

**Current designable subset:** the managed league table/fixtures backed by #30.

**Future-only subset:** multiple active leagues/competitions, UEFA-style qualification zones/rankings, cross-world
competition navigation and any competition data not exposed by the current season implementation.

**Gate implication:** managed-league table may contribute to S1; the broader world browser remains S2/later.

### `Squad Screen.html`

**Disposition:** `REVISE / DEFER` — strong visual pattern, mixed product scope.

**Keep:** sortable/filterable dense roster table, selected-player side rail, position/status filtering, keyboard table
navigation, empty/list states, compact comparison pattern.

**Split out by capability:** basic squad/player inspection can be `DESIGNABLE`; medical/availability content belongs
to #41; dynamics/morale/cliques belong to their owning systems; contracts/wages/financial views and Offer New Contract
are not supported by #27 merely because they appear in the same screen; Shortlist/transfer actions depend on #31/#32.

**Retire from current authority:** “CA/PA”, wage/value/contract, shortlist, contract offer, custom views and any metric
not verified in the owning projection at the journey's Gate A.

**Gate implication:** preserve table/detail design language for later S2; do not let this mockup pull #31/#32/#40
features into EA scope.

### `Training Screen.html`

**Disposition:** `REVISE / DEFER`.

**Verified reusable capability:** per-player `TrainingViewModel` exposes Focus, Condition and TrainingFatigue;
`TrainingSchedule.TrySetFocus` is the real player-focus mutation. `TrainingFocus` currently contains Balanced, Rest,
Fitness, Technical, Physical and Tactical.

**Keep:** player-state table, risk/condition hierarchy, individual-focus interaction pattern.

**Reference-only unless separately implemented:** editable day-by-day session calendar, named session recipes,
Auto-schedule, Copy Last Week, Apply Changes, arbitrary per-attribute development focuses, group-work/staff/analysis
subsystems and “optimal load” recommendations.

**Gate implication:** later Training Gate A starts with the six-value focus contract rather than the mockup calendar.

## 4.5 Implementation-blocked management references

| Reference | Disposition | Reusable visual pattern | Unsupported product behavior that must not drive implementation |
|---|---|---|---|
| `Scouting Screen.html` | `REFERENCE ONLY / FUTURE-BLOCKED` | shortlist/report table density, progressive-knowledge presentation, report side rail, coverage visualization | scout network, assignments, reports, shortlist mutations, estimated fee, recommendations and export; #32 implementation absent |
| `Transfers.html` | `REFERENCE ONLY / FUTURE-BLOCKED` | deal-pipeline visualization, state badges, offer/detail split, deadline/urgency presentation | approaches, bids, negotiations, clauses, medical-to-signing pipeline, transfer/wage budgets and transfer actions; #31 implementation absent and finance constraints absent |
| `Club Finances.html` | `REFERENCE ONLY / FUTURE-BLOCKED` | KPI hierarchy, P&L/bar/chart/table patterns, budget-cap visualization | balances, revenue/expenditure, wage caps, transfer budgets, sponsorships/commercial partners and financial actions; #40 implementation absent |
| `Club Staff.html` | `REFERENCE ONLY / FUTURE-BLOCKED` | staff roster/card hierarchy, department grouping, attribute/budget presentation | Hire Staff, staff contracts/wages, manager/staff attribute system, vacancies and department budgets; #34 implementation absent |
| `Club Board Room.html` | `SPLIT` | objective/status layout may inform the real #30 summary | chairman profile, confidence narrative/history, directives, meetings, requests, financial targets and deeper board interactions are not established; only the #30 objective/job-security summary is currently designable |

## 4.6 Historical / later reference

### `Club History.html`

**Disposition:** `DEFER / REFERENCE ONLY`.

The season-history table, honours, records, top scorers, manager history and milestones are useful long-term
information-design patterns. Current #37 match analytics and #30 season state do not by themselves establish the durable
multi-season club-history aggregate represented here.

**Gate implication:** no S0/S1 dependency; re-audit persistence/history ownership before S2/P3 use.

---

# 5. F1.2 findings

## F1-006 — Tactics is a real seam hidden inside an overgrown mockup

The existing Tactics reference should not be discarded. Its central interaction model corresponds to implemented
`TeamTactic` and `PlayerTactic` inputs. The defect is **semantic overbreadth**: the mockup invents a much larger role
roster, instruction vocabulary, scouting advice and template/set-piece workflows.

**Disposition:** `REVISE`. S0-A maps every retained control to the exact tactical type/member; unmatched controls are
removed or explicitly future-blocked before low fidelity.

## F1-007 — existing career chrome is a target architecture, not current navigation

Nearly every management mockup repeats Club/Squad/Tactics/Training/Scouting/Transfers/Staff/World/Inbox, calendar,
Continue and command-palette affordances. None of that repetition proves a current `ScreenId`, navigation edge or
command registration.

**Disposition:** F2 treats the chrome as a candidate target shell only. Current PM-1 navigation remains the typed
four-screen graph.

## F1-008 — one visual screen routinely spans several owning specs

Examples:

- Squad mixes #27 roster data with #41 medical, dynamics, #31 contracts, #32 shortlist and #40 wages/value;
- Club mixes #30 table/fixture/board summary with finance, contracts, medical and deeper board claims;
- Training mixes a real #29 individual-focus seam with an unverified session-calendar editor.

**Disposition:** no future Gate A may approve a screen as one unit. Controls/modules are admitted by their owning
capability.

## F1-009 — foundation patterns survive; their implementation claims do not

`touchline`, table density, selected-detail rails, compact KPI hierarchy, keyboard-first navigation and art-independent
fallbacks remain useful. Fixed HTML stage scaling, Google-hosted fonts, color-only status accents, and synthetic data
are not client contracts.

**Disposition:** F3 audits the shared patterns against #49, renderer constraints and test extremes before they become
implementation requirements.

---

# 6. F1.2 exit assessment

Every landed HTML reference is now classified enough to prevent visual precedent from becoming a phantom runtime
contract:

- **Foundation:** Design System + Desktop Guardrails kept for F3 audit; Command Palette deferred as an interaction
  pattern only.
- **S0:** Main Menu and Tactics both require reconciliation; neither current action inventory is implementation-ready.
- **S1:** Club and the managed-league subset of World are useful starting points, but are split from unsupported
  finance/transfer/deep-board/world claims.
- **S2/later:** Squad and Training contain real reusable substrates mixed with unsupported modules; Scouting,
  Transfers, Finances and Staff are implementation-blocked references; Board is only partially backed; History is
  deferred.

No high-fidelity screen is approved by this audit. The next F1 work is:

1. **F1.3** lightweight player/task assumptions;
2. **F1.4** PM-1/PM-2 task hierarchy and priority;
3. **F1.5** Early Access success floor reconciliation.

After those land, F1 exits into F2. The next visual work remains low-fidelity, not another polished mockup.

---

## Version History

| Version | Date | Change |
|---|---|---|
| 0.1 | September 6, 2026 | Created UX-A and completed the F1.1 source-grounded capability matrix at branch head `8fd178c`; separated domain/read/command, projection/dispatcher, screen/navigation, binding and host-verification state; recorded the stale `AdvanceRound`/#30 client gap and the spec-only #48/#49/#51 runtime state. |
| 0.2 | September 6, 2026 | Completed F1.2 reconciliation of all landed UX mockup references. Verified the real `PlayerTactic`/`PlayerRole`/`PlayerInstructions` and #29 training surfaces, separated reusable interaction/layout patterns from unsupported actions/data, and identified Main Menu/Tactics as S0 revisions and Club/managed-league World as the strongest S1 starting references. |
