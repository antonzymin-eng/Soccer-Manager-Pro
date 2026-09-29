# System XI — UX Experience Architecture

**Created:** September 6, 2026  
**Last Updated:** September 6, 2026  
**Version:** 0.1  
**Status:** F2 COMPLETE — CURRENT AND TARGET ARCHITECTURE SEPARATED  
**Parent execution authority:** [`ux-detailed-plan.md`](ux-detailed-plan.md)  
**Evidence parent:** [`ux-baseline-evidence.md`](ux-baseline-evidence.md) v0.3  
**Branch evidence head at start:** `7652ac0ca58e749285fa6bdb7029ae61e197667b`

---

## 0. Purpose and boundary

This file is the F2 current-vs-target architecture companion to UX-A. It places every S0/S1 task either in the
**current typed client graph** or in an explicitly **target/future** graph.

It does **not** create a `ScreenId`, navigation move, view model, dispatcher, save lifecycle, or domain command.
Those remain owned by production/client code and APPROVED specifications. Names in the target map are UX labels until
an owning client implementation explicitly registers them.

Authority order:

1. APPROVED specification;
2. verified client/production implementation;
3. F1 capability evidence;
4. this target architecture;
5. visual mockups.

The rule for reading every diagram below is:

> **CURRENT** means the typed client can represent the screen/edge now. **TARGET** means the experience needs it,
> but the client does not yet own the required registration/edge/dispatcher/binding. A target line is not an edge.

---

# 1. F2.1 — CURRENT PM-1 graph: record, do not redesign

## 1.1 Current screen catalogue

`ClientScreens` currently mints exactly four identities:

1. `MainMenu`
2. `TacticsSetup`
3. `MatchView`
4. `PostMatchReport`

No Career Home, New Career, Continue, Load, Save, Settings, Squad, Training, Scouting, Transfers, Club or World
`ScreenId` exists in the current catalogue.

## 1.2 Current five legal moves

| # | Named move | From | Operation | To | UX meaning |
|---|---|---|---|---|---|
| 1 | `OpenTacticsSetup()` | `MainMenu` | `Push` | `TacticsSetup` | enter PM-1 setup; cancel can pop back |
| 2 | `CancelTacticsSetup()` | `TacticsSetup` | `Pop` | current stack returns to `MainMenu` | discard setup |
| 3 | `StartMatch()` | `TacticsSetup` | `Replace` | `MatchView` | setup is no longer a back destination |
| 4 | `ShowPostMatchReport()` | `MatchView` | `Replace` | `PostMatchReport` | finished match is no longer a back destination |
| 5 | `ReturnToMainMenu()` | `PostMatchReport` | `Pop` | current stack returns to `MainMenu` | finish PM-1 loop |

Current graph, verbatim in UX notation:

```text
CURRENT

MainMenu
   ├── OpenTacticsSetup / Push ──> TacticsSetup
   │                                  ├── CancelTacticsSetup / Pop ──> MainMenu
   │                                  └── StartMatch / Replace ─────> MatchView
   │                                                                       │
   │                                                     ShowPostMatchReport / Replace
   │                                                                       v
   └<────────────── ReturnToMainMenu / Pop ───────────── PostMatchReport
```

Two current semantics are load-bearing:

- `TacticsSetup → MatchView` is `Replace`, so stale setup is not reachable by Back after kickoff.
- `MatchView → PostMatchReport` is `Replace`, so a frozen/finished match is not reachable by Back from the report.

**Deliberately absent:** an abandon-match edge from `MatchView`. F2 does not add one.

## 1.3 Current graph disposition

The current graph remains the complete S0 navigation input. S0 may improve the content and interaction design of the
four screens, but it may not silently change the five edges.

---

# 2. F2.2 — TARGET PM-2 career shell

## 2.1 Target Early Access loop

F1 established that the required EA loop is primarily a client-composition problem over already-landed match,
season and save substrates.

The target journey is:

```text
TARGET / NOT CURRENT CLIENT GRAPH

Launch
  |
  v
Main Menu [CURRENT screen identity]
  |\
  | +-- Start supported career ----+
  | +-- Resume latest save --------+----> Career Home / Season [TARGET / FUTURE-BLOCKED client surface]
  | +-- Load save -----------------+                  |
  |                                                   | inspect next fixture / table / objective / attention
  |                                                   |
  |                                                   +--> Match Preparation / Tactics
  |                                                          [current TacticsSetup identity exists;
  |                                                           CareerHome entry edge does not]
  |                                                                  |
  |                                                                  v
  |                                                             Match View
  |                                                                  |
  |                                                                  v
  |                                                          Post-Match Report
  |                                                                  |
  |                                                                  v
  |                                                Career Home / Season [TARGET return]
  |                                                                  |
  |                                                   advance round / save / repeat
  +------------------------------------------------------------------+
```

The diagram is an experience requirement, **not** a proposed `ClientScreenFlow` patch.

## 2.2 Target destination matrix

| Target location | Primary player question | F1 capability state | Milestone posture | F2 presentation posture |
|---|---|---|---|---|
| Main Menu | start, resume or choose supported entry | current screen `LIVE`; career actions unwired | S0 current + S1 extension | top-level page; keep PM-1 entry until career entry is implemented |
| Career Home / Season | what matters now and what happens next? | season/table/fixture/objective data `DESIGNABLE`; screen/navigation `FUTURE-BLOCKED` | **S1 Critical** | target top-level page/root of career context |
| Match Preparation / Tactics | how will I set up for the next match? | tactical domain + current `TacticsSetup` identity `DESIGNABLE`; career entry edge absent | **S0/S1 Critical** | reuse current PM-1 setup concept; target career entry explicitly future |
| Match View | what is happening and should I intervene? | `DESIGNABLE / HOST-UNVERIFIED`; current screen identity exists | **S0 Critical** | current top-level match page |
| Post-Match Report | what happened and why at core-stat level? | analytics `DESIGNABLE`; current screen identity exists | **S0 Critical** | current report page; target return destination differs from current flow |
| Managed League / World | where do we stand? | current managed-league data `DESIGNABLE`; no screen identity | S1 High | target page or Home drill-down; broad world browser deferred |
| Squad | who is available / selected? | player/lineup substrate `DESIGNABLE`; no verified client screen | S2 unless explicitly promoted | future page; table/detail patterns may be reused later |
| Training | what focus/state does each player have? | verified per-player training subset `DESIGNABLE`; broader planner unverified | S2 unless explicitly promoted | future page; only verified focus/state subset may be admitted |
| Club | club-level context beyond Career Home | board summary partially `DESIGNABLE`; most current mockup modules depend on absent systems | S2 | future page only after owning capabilities are re-audited |
| Scouting | who should I learn more about? | `FUTURE-BLOCKED / SPEC-ONLY` | S2/later | do not expose as live EA navigation |
| Transfers | who can I sign/sell and on what terms? | `FUTURE-BLOCKED / SPEC-ONLY` | S2/later | do not expose as live EA navigation |
| Finances | what can the club afford? | `FUTURE-BLOCKED / SPEC-ONLY` | S2/later | do not expose as live EA navigation |
| Staff | who works for the club and what can I change? | `FUTURE-BLOCKED / SPEC-ONLY` | S2/later | do not expose as live EA navigation |
| Settings / Accessibility | how do I configure release-critical presentation/input? | #49 contract exists; runtime/client surface `FUTURE-BLOCKED` | **release-critical dependency** | target shell surface; must not be presented as working until implementation exists |
| Help | what does this control/state mean? | generic UX behavior, no dedicated current screen required | S0/S1 support | contextual first; dedicated page only if later justified |
| Inbox / News | what changed in the wider world? | no F1-verified S0/S1 client consumer | `OUT-OF-EA / later` unless separately promoted | excluded from S0/S1 critical navigation |

### Navigation visibility rule

The **target map is broader than the shipping nav**. Early Access navigation must show only destinations whose owning
capability and client registration are actually admitted for the release. F2 explicitly rejects a disabled top bar full
of future modules as a substitute for implementation.

---

# 3. Current-to-target bridge: required client decisions

F2 does not choose the implementation, but it records where the current typed graph cannot satisfy the target journey.

| Junction | Current truth | Target need | Required owning-client work before implementation handoff |
|---|---|---|---|
| Main Menu → Career Home | no Career Home `ScreenId`/registration/edge | enter a generated or resumed career context | add and test an explicit registered career destination and legal move after the save/bootstrap consumer exists |
| Main Menu → New Career | bootstrap domain exists; no client surface | configure/start supported career | define client projection/dispatcher + presentation form; no mockup-created command |
| Main Menu → Continue/Load | save domain exists; no client surface | resume latest or select save | define save-list/latest-save projection and lifecycle-aware client action/transition |
| Career Home → Tactics Setup | current `OpenTacticsSetup()` is guarded to `MainMenu` | prepare next fixture from season context | new/revised guarded client move is required; UX does not assume the current MainMenu-only move generalizes |
| Tactics cancel → Career Home | current pop lands on Main Menu because current stack is rooted there | return to season context without starting match | client owner must define/test semantics in the career stack; do not rely on incidental `Pop` behavior |
| Tactics → Match View | current `StartMatch()` exists | start the scheduled career match | likely reusable semantic shape, but S1 Gate A must verify the career match/session composition before handoff |
| Match View → Post-Match | current `ShowPostMatchReport()` exists | show finished scheduled match report | current semantic shape remains appropriate; scheduled-career integration still needs composition evidence |
| Post-Match → Career Home | current move is specifically `ReturnToMainMenu()` | return to season context and preserve career stack | **critical target mismatch**: requires explicit client-flow decision/test; do not reuse a differently named/contracted move by accident |
| Career Home → Advance Round | domain command exists; `AdvanceRound` client intent/season dispatcher absent | advance season after player chooses Continue/Advance | add owning season client intent/dispatcher mapping to existing #30 seam; UX does not add the intent itself |
| Career Home ↔ Save/Load | save domain exists; lifecycle UI absent | save, handle error, resume/load safely | define owning client save lifecycle and transition rules before controls become live |

## 3.1 Why incidental stack behavior is not authority

Some target flows could appear to “work” if current `Pop()` happens to reveal a new screen inserted beneath an
existing one. F2 rejects that as a design premise. The project already treats named guarded moves as the navigation
contract. A method named/documented as returning to Main Menu is not silently repurposed as Return to Career Home just
because a future stack layout makes the raw `Pop` land there.

---

# 4. F2.3 — interaction architecture

These rules define presentation behavior not already owned by `NavigationShell`.

## 4.1 Page vs modal vs drawer

### Page

Use a page/top-level destination when the player is changing durable task context and the surface has its own primary
question, data projection, or command set.

Target examples: Career Home, Match Preparation/Tactics, Match View, Post-Match Report, managed-league table, later
Squad/Training/Transfers when those consumers exist.

A page-level target that lacks a registered client identity is labelled `TARGET / FUTURE-BLOCKED`; a visual mockup
cannot make it current.

### Modal

Use a modal only for a short, blocking decision that must complete or be cancelled before the parent task continues.
Examples may include destructive confirmation or a compact save/load confirmation **after** the owning lifecycle is
specified.

Rules:

- modal focus is trapped intentionally while open, with a deterministic Escape/Cancel path unless the owning operation
  is genuinely non-cancellable;
- closing a modal restores focus to the invoking control;
- a modal does not become a substitute for an unimplemented top-level client destination;
- critical error/recovery information must remain available after the modal closes if the problem is unresolved.

### Drawer / side rail

Use a drawer for contextual inspection or secondary editing that does not change the primary task context.
Examples: selected-player detail, a compact tactic explanation, row detail.

Rules:

- the parent page remains visible and logically current;
- opening a drawer does not mint navigation history by itself;
- keyboard focus moves into the drawer predictably and returns to the invoker on close;
- a drawer must not hide a required primary action or a blocking state.

## 4.2 Subnavigation / tabs

Use tabs for alternate views of the **same owning context** when switching does not cross a domain lifecycle boundary.

A tab must not conceal a new top-level screen/command contract. If a destination needs its own view-model ownership,
save/dirty lifecycle or independent deep task, Gate A re-evaluates whether it is actually a page.

For S0, do not add tabbed career chrome around the four PM-1 screens simply to resemble the management mockups.

---

# 5. Attention architecture

Attention has four levels. Color is never the sole carrier.

| Pattern | Use | Must not be used for |
|---|---|---|
| Inline state/message | one field, row or local action needs explanation | global blocker or completed action confirmation |
| Banner | current page has a blocking/high-priority condition or task-level warning | routine counters or transient success |
| Badge/count | non-blocking quantity/attention marker on a live destination | sole indication of severity or a future/unimplemented module |
| Toast | transient acknowledgement of a completed non-blocking action | destructive confirmation, save/load failure requiring action, or information needed later |

Rules:

1. **Critical-path blockers are persistent** until resolved or explicitly dismissed where safe.
2. **Badges summarize; they do not explain.** Activating/focusing the destination must reveal the underlying items.
3. **No fake urgency.** UX may expose only urgency/status supplied by the owning projection or a presentation rule
   derived from explicit state, never synthetic recommendation logic.
4. **No future-feature badges.** A module that is not live does not advertise counts or attention in shipping chrome.

---

# 6. Blocked-action and “why disabled” rules

A disabled control is appropriate only when the **capability is real** but the current state makes the action
temporarily unavailable.

Every disabled critical/high-frequency action must expose a reason through visible companion text or a keyboard/mouse
accessible explanation.

Examples of valid disabled-state classes:

- no save exists yet;
- required setup is incomplete/invalid;
- a match action is not legal in the current match state;
- an operation is already in progress;
- the owning system reports a refusal/precondition.

A `FUTURE-BLOCKED`, `SPEC-ONLY`, `UNKNOWN`, or out-of-release capability is **not** represented by a normal enabled
or permanently-disabled production control. It is omitted from shipping interaction surfaces until admitted.

---

# 7. Save/load transition blocking

The save domain exists, but F1 found no player-facing save browser, lifecycle projection or client dispatcher. F2
therefore defines the UX safety rule without inventing lifecycle semantics:

1. no optimistic navigation is designed around an assumed save/load duration or atomicity;
2. once the owning client exposes an in-progress/failure/success lifecycle, duplicate invocation is suppressed while an
   operation is unresolved;
3. navigation that the owning save/load contract marks unsafe is blocked with a persistent reason;
4. load is treated as a context-replacing operation — the current career view is not allowed to continue displaying
   stale pre-load state after the owner reports completion;
5. save/load failure is never toast-only; recovery or retry state remains visible;
6. until that client lifecycle exists, save/load controls remain target/future in S1 and cannot pass Gate A as live.

---

# 8. Contextual help

Help follows “explain at the point of decision” before adding a separate help destination.

Priority order:

1. clear label and current value/state;
2. concise inline reason for invalid/disabled state;
3. tooltip/popover for definition or consequence that is useful but not critical;
4. dedicated help/reference surface only when the concept cannot be explained locally without overwhelming the task.

Requirements:

- critical instructions are never hover-only;
- keyboard focus must reveal the same explanation available to pointer users;
- tooltips never contain the only path to recover from an error;
- explanation text is localizable and allowed to wrap/reflow in F3 test profiles;
- “why disabled” describes the actual precondition, not speculative simulation advice.

---

# 9. S0/S1 task placement

This table closes the F2 requirement that every release-critical task has an unambiguous architectural home.

| EA task | Current location | Target location | F2 status |
|---|---|---|---|
| enter PM-1 supported mode | `MainMenu → TacticsSetup` | current S0 path remains available | `CURRENT` |
| start a career | none | Main Menu → Career Home via New Career flow | `TARGET / FUTURE-BLOCKED client` |
| resume/load career | none | Main Menu → Career Home via Continue/Load | `TARGET / FUTURE-BLOCKED client` |
| know what needs attention next | none | Career Home | `TARGET`; backing season data `DESIGNABLE` |
| inspect league/next fixture/objective | none | Career Home / managed-league drill-down | `TARGET`; backing data `DESIGNABLE` |
| prepare match | `TacticsSetup` from Main Menu | Tactics Setup from Career Home | content `DESIGNABLE`; career edge `TARGET` |
| cancel preparation | `TacticsSetup → MainMenu` | return to Career Home | `TARGET client-flow decision` |
| start match | `TacticsSetup → MatchView` | same task in career composition | current move exists; S1 composition still future |
| read live state | `MatchView` | `MatchView` | `CURRENT identity / DESIGNABLE binding` |
| intervene tactically | `MatchView` | `MatchView` | domain/dispatcher `DESIGNABLE`; P5b controls absent |
| understand result/core stats | `PostMatchReport` identity | `PostMatchReport` | data `DESIGNABLE`; binding absent |
| return to season context | none | Post-Match → Career Home | `TARGET / critical client-flow gap` |
| advance correctly | none | Career Home | domain seam exists; season intent/dispatcher `TARGET` |
| save/quit/resume | none | Career Home + Main Menu entry/recovery | domain exists; client lifecycle `TARGET` |
| release-critical settings/a11y | none | Settings/accessibility shell surface | `TARGET / FUTURE-BLOCKED` (#49 runtime/client) |

Nothing marked `UNKNOWN` supports the S0/S1 critical path.

---

# 10. Target shell rules

When the future career shell becomes implementable:

- **Career Home is the career context root**, not the current Main Menu.
- Main Menu remains the application/root entry and does not become an always-visible career module bar.
- Match View remains task-focused; it does not inherit the full management chrome.
- Post-Match Report must have one obvious primary continuation destination appropriate to the active mode: Main Menu
  for the current standalone PM-1 loop; Career Home for the future career loop once the client graph explicitly owns
  that distinction.
- S2 destinations are not added to EA global navigation merely because historical mockups contain them.
- Global navigation is capability-driven: only registered/admitted destinations appear as live destinations.
- Context-specific back/cancel behavior follows named client moves, not arbitrary stack pokes in the rendering layer.

---

# 11. Critique / revision record

## Round 1 — rejected: mockup-style global module bar as the target shell

**Problem:** Reusing the existing Club/Squad/Tactics/Training/Scouting/Transfers/Staff/World chrome would make
future-blocked systems look like current product structure and would let visual precedent define navigation.

**Revision:** target destination matrix carries capability/milestone labels; shipping navigation exposes only admitted
live destinations. S2 modules remain absent from EA chrome until their owning consumers exist.

## Round 2 — rejected: assume current `Pop` moves automatically generalize to Career Home

**Problem:** With a future Career Home under Tactics or Post-Match, raw stack behavior might incidentally pop to the
right visual destination while violating named/current move semantics such as `ReturnToMainMenu()`.

**Revision:** the current-to-target bridge names every semantic mismatch and requires explicit client-flow ownership
and tests. Incidental stack layout is not evidence.

## Round 3 — rejected: treat Settings as either “optional polish” or already designed

**Problem:** #49 makes localization/accessibility a release concern, but F1 found no runtime/client settings surface.
Calling Settings optional hides a release dependency; drawing it as normal navigation falsely implies implementation.

**Revision:** Settings/accessibility is a **release-critical target dependency** labelled `FUTURE-BLOCKED` until its
owning runtime/client consumer exists.

## Round 4 — final check

The revised architecture now satisfies the F2 exit test:

- current PM-1 graph is copied without redesign;
- target career graph is visually and semantically separated from current behavior;
- every S0/S1 critical task has a current or target location;
- no `UNKNOWN` capability supports a critical premise;
- unsupported S2 modules cannot leak into EA navigation through the mockups;
- future bridge points are recorded as client dependencies rather than invented UX contracts;
- page/modal/drawer/tab, attention, blocked-action, save/load and contextual-help rules are defined without simulation
  logic.

**F2 status: COMPLETE.**

---

# 12. Next work

Per `ux-detailed-plan.md`, next is **F3 — Shared S0/S1 interaction system**.

F3 audits only primitives required by S0/S1: actions, focus/navigation, tables, inputs, tooltips/help, modal/feedback,
loading/empty/stale/error states, keyboard behavior, localization/reflow, accessibility semantics, font/glyph fallback,
art fallback, caption/HUD coexistence and desktop resolution behavior.

No additional high-fidelity screen is authorized by F2.

---

## Version History

| Version | Date | Change |
|---|---|---|
| 0.1 | September 6, 2026 | Completed F2 from synced branch head `7652ac0`: recorded the current four-screen/five-move PM-1 graph verbatim, separated the target PM-2 career shell, mapped current-to-target client gaps, defined interaction/attention/blocked/save/help architecture, mapped every EA task, and closed four critique rounds without creating runtime contracts. |
