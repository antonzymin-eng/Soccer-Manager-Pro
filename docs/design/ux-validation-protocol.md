# System XI UX Validation Protocol

**Created:** September 11, 2026  
**Last Updated:** October 5, 2026\
**Version:** 0.36\
**Status:** F4 + S0 A–F COMPLETE — G PASS FOR v0.5; H PASS FOR v0.2; I PASS — IMPLEMENTATION HANDOFF\
**Execution authority:** [`ux-detailed-plan.md`](ux-detailed-plan.md) v1.29 §F4 and Gates E–G\
**Shared interaction baseline:** [`ux-shared-system.md`](ux-shared-system.md) v0.4\
**Evidence baseline:** [`ux-baseline-evidence.md`](ux-baseline-evidence.md) v0.3

---

## 1. Purpose and authority

This file is the repeatable operating packet for the validation method defined by
`ux-detailed-plan.md` F4. It does not redefine the F4 rules, Gates E–G, severity scale or
pass/fail policy. Where this packet and the detailed plan disagree, the detailed plan wins and this
file is the defect.

The packet exists so a person other than the UX author can run the same S0/S1 validation process and
produce comparable evidence rather than an informal design review.

F4 is **complete** at this revision. The project owner assigned **Anton Zymin** as the
`ux-detailed-plan.md` §10.1 UX-workstream accountable owner on September 11, 2026 and then directed
that participant names be omitted. The protocol therefore uses stable anonymous slots and never
stores participant names or contact details. For S1, the UX owner attests that two distinct real
people are available for the applicable anonymous slots before Gate F and both complete Gate G. For S0, the September 30 owner decision
supersedes those participant prerequisites with owner image review (§9.1).

---

## 2. Four-layer validation sequence

**S0 review route, owner-directed September 30, 2026:** Anton Zymin conducts the image review
in place of independent tester sessions. The former S0-P1/P2 recruitment, availability and
independence prerequisites are superseded. Sections 3/5 retain the complete task and scripted
interaction coverage; the participant mechanism and session templates below apply to S1 and
optional future research. For S0, use §9.1. No owner image approval is inferred from this decision.
This amendment supersedes the earlier S0 participant requirements throughout this protocol.

Run the layers in this order for each journey:

1. **Contract review** — verify that every displayed datum and action is backed by a current owner or
   explicitly marked `FUTURE-BLOCKED`.
2. **Scripted heuristic/self-walkthrough** — run the complete task against the resilience matrix in
   §5 before showing the prototype to a participant.
3. **Design validation** — S0 uses the owner image review in §9.1; S1 uses the independent
   Gate-G round and neutral moderator script in §6. The designer/implementer is not an S1 participant.
4. **Implementation verification** — after implementation, verify the real client against the
   validated handoff and host/cert requirements at Gate J.

A later layer never repairs a skipped earlier layer. Self-walkthrough evidence does not replace
S0 owner image approval or the S1 two-participant Gate-G round.

---

## 3. Journey validation targets

### 3.1 S0 participant tasks

S0 is the PM-1 journey:

`launch → Main Menu → Tactics Setup → Match View → Post-Match Report → Main Menu`

The current product graph is authoritative for navigation semantics. The prototype may be non-Unity,
but it must distinguish current behavior from future behavior and may not invent a domain command to
make a task testable.

Use these outcome-oriented tasks. Do not tell the participant which control to click.

| ID | Task | Completion evidence |
|---|---|---|
| S0-T1 | From the launch/Main Menu state, use the supported match-entry path presented by the prototype and reach the place where you can prepare your team. | Participant reaches Tactics Setup without moderator navigation help; if the current New Game/start limitation blocks the path, the dependency is recorded rather than treated as participant failure. |
| S0-T2 | Make one understandable pre-match tactical choice, then start the match. | Participant identifies the relevant tactic control, understands the consequence well enough to choose, and reaches Match View. |
| S0-T3 | During the match, determine the score, match time/state and current playback speed. | Participant reports all three from the interface without guessing from animation alone. |
| S0-T4 | Change playback speed, then make one in-match tactical adjustment. | Participant finds both actions, distinguishes presentation pacing from a football decision, and can tell whether each action took effect. |
| S0-T5 | Open/close the live statistics area and identify at least one useful match statistic. | Participant can expose the stats area, locate a requested statistic, and return attention to the match. |
| S0-T6 | At full time, determine the result and key match summary, then return to the main menu. | Participant recognizes the frozen/full-time state, uses the report rather than trying to resume a live match, and returns to Main Menu. |
| S0-T7 | From Tactics Setup, use the available back/cancel path to leave without starting a match, then re-enter Tactics Setup. | Participant returns to Main Menu through the defined cancel/back edge, does not accidentally start a match, and can re-enter the setup flow without losing orientation. |

S0-T7 exists specifically to exercise Gate F's binding requirement that the complete task include
back/cancel behavior. The moderator may place S0-T7 before S0-T2 when that produces a cleaner session,
provided the task ID and evidence remain unchanged.

The no-skip rule below operationalizes [`ux-detailed-plan.md`](ux-detailed-plan.md) §5 **Gate F —
Interactive prototype**: prototype the **complete task**, accurately label real versus future behavior,
and pass only when a participant can attempt that complete task without control-by-control instruction.
It does not add a new gate criterion beyond that authority.

If a prescribed task depends on a capability still marked `FUTURE-BLOCKED`, Gate F remains **FAIL**
until the prototype provides an honest simulated representation of that specified future surface.
Do not run the Gate-G participant round with the task omitted from scoring, and do not silently convert
the dependency to `LIVE`. If the dependency is discovered only during a participant session, record it
as a prototype/dependency blocker, fail Gate G for that round, and return to Gate F; it is not a
participant error. Prescribed task coverage therefore has no "honestly testable" denominator escape.

### 3.2 S1 participant tasks

S1 is the PM-2 / Early Access season-loop journey from `ux-detailed-plan.md` §7:

`Launch → New/Continue/Load → Career Home/Season → inspect/prepare/advance → Match → Report → Career Home/Season → Save/Continue`

The following task IDs operationalize the minimum outcomes in `ux-detailed-plan.md` §7.6 without
inventing a control path or unresolved product rule:

| ID | Task | Completion evidence |
|---|---|---|
| S1-T1 | Start or resume the supported career mode. | Participant enters the supported career state through the behavior admitted by the prototype; unresolved New/Continue/Load product choices are recorded as dependencies rather than invented. |
| S1-T2 | Identify the next match and current league position. | Participant can report both from the Career Home/Season surface without developer knowledge of #30 internals. |
| S1-T3 | Reach the preparation context for the next match. | Participant reaches the admitted preparation surface without moderator navigation help. |
| S1-T4 | Progress correctly toward the next match. | Participant identifies the action that advances the season and can explain what will progress before committing. |
| S1-T5 | Complete one round through the match and report loop. | Participant reaches the match, completes the supported match/report path, and returns to Career Home/Season. |
| S1-T6 | Understand the changed result and league table after the round. | Participant can identify the completed result and explain the visible league-position/table change. |
| S1-T7 | Save, quit and resume according to the product promise. | Participant completes the supported persistence flow exactly as admitted by the prototype; unresolved save/continue promises block Gate F rather than being cosmetically simulated as live. |
| S1-T8 | Find settings/accessibility. | Participant locates the admitted settings/accessibility surface or the prototype records the explicit implementation dependency if that surface is not yet live. |

The same Gate-F no-skip rule applies to S1. A prescribed S1 task with an unresolved dependency remains
in the task set and blocks Gate F/G until an honest prototype representation exists; it is not removed
from the participant denominator.

### 3.3 Critical observations

For every task, capture:

- whether it completed without intervention;
- first click/action and any wrong turn;
- hesitation or repeated scanning;
- information the participant missed;
- control/state misunderstood;
- whether disabled/unavailable state explained why;
- whether the participant knew what would happen next before committing;
- whether keyboard or mouse path caused different understanding;
- direct participant wording only when useful and brief.

Do not infer success from eventual completion alone. A participant who completes after repeated wrong
turns may still expose a Major information-architecture problem.

---

## 4. F4 human assignments and independent participant mechanism

### 4.0 UX workstream accountable owner

`ux-detailed-plan.md` §10.1 defines the UX workstream as accountable for artifacts, flows,
prototypes, findings and handoffs. On September 11, 2026, the project owner assigned **Anton Zymin**
to fill that role.

| Role | Accountable scope | Assignee | Status |
|---|---|---|---|
| UX workstream | UX artifacts, flows, prototypes, findings and implementation handoffs | **Anton Zymin** | ASSIGNED — September 11, 2026 |

The assignee may perform work directly or delegate it, but the role must have one clear accountable
owner before F4 is marked complete. This assignment does not transfer the project owner's decision
rights or the domain/client owners' authority defined in §10.1.

### 4.1 Privacy-safe recruitment slots

`ux-detailed-plan.md` F4.2 requires two independent participants for S1; the S0 rows below are
superseded historical slots under the September 30 owner image-review decision. For S1, the
designer/implementer does not count. Personal names and contact details are intentionally excluded
from repository evidence. Each stable slot instead records the preferred profile, recruiting channel,
a privacy-safe owner availability attestation and an explicit independence/distinctness attestation before its journey reaches Gate F.

| Slot | Journey | Preferred profile | Recruiting channel | Repository identity | Availability attestation | Independence/distinctness attestation | Status |
|---|---|---|---|---|---|---|---|
| S0-P1 | S0 | Historical experienced-player slot | Historical recruitment route | **S0-P1 — superseded** | Not required | Not required | SUPERSEDED — owner image review |
| S0-P2 | S0 | Historical newcomer slot | Historical recruitment route | **S0-P2 — superseded** | Not required | Not required | SUPERSEDED — owner image review |
| S1-P1 | S1 | Experienced football/management-sim player | Owner/team personal or relevant community network | **S1-P1 — anonymous; name omitted** | Due before S1 Gate F | Due before S1 Gate F | READY / FUTURE S1 |
| S1-P2 | S1 | Football-literate newcomer to management sims, where practical | Owner/team personal or relevant community network | **S1-P2 — anonymous; name omitted** | Due before S1 Gate F | Due before S1 Gate F | READY / FUTURE S1 |

Paid recruitment is optional. The important properties are independence from authorship and enough
football/product context to attempt the task without being coached through the UI. Do not add names,
handles, email addresses or other identifying details to this packet. The slot ID, profile,
independence attestation and owner availability attestation are the repository record.

**Historical F4 exit (S0 prerequisites superseded September 30, 2026):** closed under `ux-detailed-plan.md` v1.5's explicit timing relaxation. §4.0 assigns the accountable UX owner and §4.1 defines the two anonymous S0 slots,
profiles, recruiting channels and attestation mechanism. Anonymity alone does not move the availability check; v1.5 separately moves it from F4 exit to pre-Gate-F. Before S0 reaches Gate F, Anton Zymin records
`AVAILABLE — owner attested <date>` in both availability cells and `INDEPENDENT/DISTINCT — owner attested <date>` in both independence/distinctness cells without naming either person. The latter attests that each slot is a real person independent of the UX author/designer and distinct from the other slot. If either participant is unavailable or either independence/distinctness attestation is missing, Gate F does not open and Gate G cannot pass; there is no provisional
bypass to Gate H/I.

### 4.2 Scheduling rule

S0 image review is conducted by Anton Zymin after the complete Gate-F vehicle is ready.
The participant scheduling rule below applies to S1.

The participant round is booked for the first practical session after the applicable journey prototype
passes Gate F. One round is the default cap. A second round is required only when a Blocker/Major
causes a material flow redesign or the project owner explicitly requests another pass.

---

## 5. Gate-E scripted self-walkthrough

Run the complete applicable journey task set once per relevant condition below before independent
testing. Combine conditions when that does not hide the failure mode; split them when interaction would
make the cause ambiguous. The table explicitly covers every minimum F4.6 test-data profile and every
condition named by F4.3; rows beyond that floor are Gate-E-only resilience checks and are marked as
such.

Each Gate-E execution is one identifiable run record. Complete this header before the matrix and keep
the header with the matrix whenever evidence is copied, linked or archived; a matrix without its run
identity is incomplete.

| Run field | Value |
|---|---|
| Gate-E run ID | Stable ID, for example `UX-GE-S0-20260911-01` |
| Journey | S0 / S1 |
| Prototype/version | |
| Date | |
| Runner | |

The `Journey` identifies which prescribed task set was exercised. A prototype containing both S0 and
S1 requires separate Gate-E run records for each journey; one completed matrix never proves both.

| Condition | Required check | Result | Prototype/version | Evidence / finding or N/A reason |
|---|---|---|---|---|
| Ordinary case | Complete all prescribed tasks with representative real data for the journey. | | | |
| Color-independent meaning *(Gate-E requirement)* | Across ordinary applicable states, result/status, availability, selection, warning/error and action-state meaning remains understandable without relying on hue alone; use text, iconography, shape, pattern, position or another non-color cue where meaning would otherwise be color-only. This check is independent of indicator density. | | | |
| Contrast *(Gate-E / F3-001 requirement)* | Verify that critical/high-priority text, controls, focus, disabled states and warning/error/status content remain readable and distinguishable with adequate contrast. Record the actual verification evidence used; existing mockup colors or token values are references and do not count as contrast evidence. | | | |
| Long player/club/competition names | No clipped critical identity, control label or result; long football identities remain distinguishable. | | | |
| Empty/large lists | S1/shared list primitives preserve empty explanation and usable selection/sort behavior. | | | |
| Many status indicators | Dense status presentation remains scannable and priority/order does not collapse; the separate color-independent row above still applies. | | | |
| Pseudo-locale | Expanded/localized strings reflow without hiding critical actions or state. | | | |
| Alternate date/currency formatting | Relevant S1/management surfaces format without hard-coded width assumptions. | | | |
| No save | Surfaces do not imply a usable save/resume capability when no save exists for the tested state; unavailable behavior is explicit. | | | |
| Save/load failure where relevant | A failed save/load never masquerades as success; recovery/next safe action is clear. | | | |
| No match frame yet | Match View does not falsely present the match as live/ended; waiting/initial state is intelligible. | | | |
| Disabled action with reason | The reason is available when it affects the player's next decision. | | | |
| Error/failure state (general) | F4.3 names `disabled/error states`, of which the two save rows above are only one class. Any reachable failure — a rejected or refused action, a load/start that cannot proceed, an unavailable dependency — must not masquerade as success, and the next safe action must be clear. | | | |
| Missing art | Fallback preserves identity/layout and does not create a blank critical region. | | | |
| Event-heavy match | HUD/stat attention remains usable under dense events. | | | |
| Unusual scoreline | Score/result hierarchy survives wider values. | | | |
| Full-time/frozen state | Tactical input is unavailable; save/report behavior is not confused with a still-live match. | | | |
| Small desktop — 1366-wide design-validation case | Preserve critical context/state/action and fold secondary detail. This is the shared-system validation case, **not** a declaration that 1366 is the shipping minimum. | | | |
| Reference — 1920×1080 | Intended hierarchy/density matches the reference composition. | | | |
| Expanded — 2560-wide/high-resolution/ultrawide design-validation case | Reveal useful detail or safe breathing room without merely stretching; avoid unusable line lengths, extreme separation or floating controls. | | | |
| Max supported text scale | Critical path remains operable; focus/control relationships remain clear. | | | |
| Keyboard only | Every critical action in the applicable journey is reachable in coherent order; no focus trap; state is visible without hover. | | | |
| Mouse only | Complete the applicable journey path without keyboard-only dependency. | | | |
| Audio muted/caption path *(Gate-E only)* | No required journey information depends on sound alone. Named by Gate E, not by the F4.6 minimum set; run it where the prototype has any audio/caption surface at all. | | | |

The desktop rows above use the explicit **1366-wide / 1920×1080 / 2560-wide** design-validation cases
from `ux-shared-system.md` §13 so independent runs use comparable dimensions. They do not convert the
1366-wide case into a shipping-platform minimum; that product/implementation decision remains open.

Every Gate-E run must complete its `Gate-E run ID`, `Journey`, `Prototype/version`, and every matrix
row's `Result`, prototype/version and evidence/finding field. `Result` is `PASS`, `FAIL`, or justified
`N/A`; a blank run-identity field or row result/evidence field means Gate E is incomplete and cannot
pass. A `FAIL` links a stable finding ID where one exists. `N/A` must state why the condition cannot
apply to that journey/prototype version rather than merely that it was not run.

For conditions that do not apply to the tested journey/prototype, record `N/A` with a reason rather
than silently skipping the row. The color-independent-meaning row is not discharged by marking `Many
status indicators` N/A: wherever the prototype conveys semantic state or action meaning with color,
the standalone check applies even in an ordinary low-density interface. The contrast row is
independent of that non-hue check and must carry its own result/evidence wherever critical/high-priority
content is present, as required by `ux-shared-system.md` F3-001.

The save rows and the general error row do not substitute for each other in either direction. A
generic error-state check does not discharge the two binding F4.6 save profiles, and the two save
profiles do not discharge F4.3's wider `disabled/error states` condition.

---

## 6. Participant session script (S1; optional S0 research)

### 6.1 Moderator opening

Use a neutral introduction:

> We are testing the interface, not you. Please work through each task as you normally would. You can
> think aloud if comfortable. I may ask what you expect to happen, but I will not tell you which
> control to use unless the task has already failed and we are continuing for observation.

Do not explain the screen hierarchy, intended control names or navigation path before the task.

### 6.2 Per-task procedure

For each task:

1. read the task goal verbatim or in equivalent neutral language;
2. start from the prescribed state;
3. record the first action and visible hesitation/wrong turns;
4. do not rescue the participant while completion remains plausible;
5. if blocked, mark the point of failure before giving any help needed to continue later tasks;
6. after completion/failure, ask:
   - "What did you expect that action to do?"
   - "What tells you the action/state changed?"
   - "What would you do next?"
7. assign no severity during the session; classify findings afterward against §8.

### 6.3 Session stop conditions

Stop or skip a task when:

- the prototype crashes or enters a state from which the task cannot honestly continue;
- a missing `FUTURE-BLOCKED` dependency makes the remaining interaction fictional;
- continuing would contaminate later observations more than restarting from a known state.

Record the stop reason as evidence; do not count it as participant error. A stop caused by a missing
prescribed capability or `FUTURE-BLOCKED` dependency invalidates Gate G for that round and returns the
prototype to Gate F. The task may not be removed from the Gate-G denominator simply because the
prototype could not honestly present it.

---

## 7. Evidence record

For S1 or optional participant research, create one record per participant and one consolidated
finding table per tested prototype version. S0 owner image review uses §9.1 instead.

### 7.1 Session header

| Field | Value |
|---|---|
| Journey | S0 / S1 |
| Prototype/version | |
| Date | |
| Participant slot | S0-P1 / S0-P2 / S1-P1 / S1-P2 |
| Participant profile | |
| Independence/distinctness attestation | `INDEPENDENT/DISTINCT — owner attested <date>`; must match §4.1 for this slot |
| Management-sim familiarity | |
| Football familiarity | |
| Input method used | |
| Display/layout | |
| Text scale / locale condition | |
| Moderator | |

### 7.2 Task evidence

Use the table matching the `Journey` in §7.1. Do not mix S0 and S1 task IDs in one session record.

**S0 task evidence**

| Task | Complete without intervention? | First action | Wrong turns / hesitation | Missed info | Misunderstood state/control | Confidence about next step | Notes |
|---|---|---|---|---|---|---|---|
| S0-T1 | | | | | | | |
| S0-T2 | | | | | | | |
| S0-T3 | | | | | | | |
| S0-T4 | | | | | | | |
| S0-T5 | | | | | | | |
| S0-T6 | | | | | | | |
| S0-T7 | | | | | | | |

**S1 task evidence**

| Task | Complete without intervention? | First action | Wrong turns / hesitation | Missed info | Misunderstood state/control | Confidence about next step | Notes |
|---|---|---|---|---|---|---|---|
| S1-T1 | | | | | | | |
| S1-T2 | | | | | | | |
| S1-T3 | | | | | | | |
| S1-T4 | | | | | | | |
| S1-T5 | | | | | | | |
| S1-T6 | | | | | | | |
| S1-T7 | | | | | | | |
| S1-T8 | | | | | | | |

### 7.3 Finding ledger

`ux-detailed-plan.md` §12 is the field authority for this ledger. The packet keeps the affected
state/task and owner-acceptance evidence alongside the required fields rather than replacing any of
them.

| ID | Journey | Gate | Evidence | Affected task/state | Severity | Owner | Disposition | Acceptance rationale / reference | Release condition | Retest result |
|---|---|---|---|---|---|---|---|---|---|---|
| UX-S0-001 | | | | | | | | | | |

Use stable IDs when a finding survives revisions so evidence and retest results remain traceable. A
deferred Major must name the concrete milestone/condition governing release; a later retest records its
actual result in `Retest result` rather than hiding that outcome in a generic status field. The
`Disposition` field must use exactly one value from the closed vocabulary in §8. If an unresolved
Major is accepted for Gate G, `Acceptance rationale / reference` must contain the project owner's
rationale or a precise durable reference to where that rationale is recorded; an ID alone is not
sufficient acceptance evidence.

---

## 8. Severity and disposition

The definitions below operationalize, but do not replace, `ux-detailed-plan.md` F4.4–F4.5 and §12.

- **Blocker** — critical task cannot be completed; critical path is inaccessible; destructive
  ambiguity exists; or false capability is presented as live.
- **Major** — repeated critical-information miss; frequent navigation failure; common action is
  misunderstood; severe repeated friction; or material localization/accessibility failure.
- **Moderate** — meaningful confusion/inefficiency with a workable path.
- **Minor** — polish, microcopy or low-impact consistency issue.

`ux-detailed-plan.md` §12 defines a closed disposition vocabulary. The `Disposition` field in §7.3
must contain exactly one of:

- `FIX NOW`;
- `ACCEPT FOR CURRENT GATE`;
- `DEFER TO P2/P3`;
- `BLOCKED BY DOMAIN/CLIENT IMPLEMENTATION`;
- `INVALID / NOT REPRODUCED`.

No free-text disposition value is valid. Rationale, milestone/dependency detail and retest evidence
belong in the adjacent ledger fields rather than by inventing a sixth disposition.

Disposition/pass rules:

- Gate G cannot pass with any unresolved Blocker.
- Gate G cannot pass with an unresolved Major unless the project owner explicitly accepts it with a
  rationale and target disposition; the finding ledger must preserve that rationale directly or by a
  precise durable reference in `Acceptance rationale / reference`.
- Moderate may be accepted only when documented and non-compounding.
- Minor enters backlog unless it is cheap and safe to fix in the current revision.
- A Blocker/Major that materially changes the flow triggers a second participant round after revision.

Do not downgrade severity merely because a participant eventually found a workaround.

---

## 9. Gate-G decision record

### 9.1 S0 owner image review

The completed v0.4 record below covers the original 18 images retained at the paths below: seven journey states, both staging
dialogs, live/paused Pending, Applied, Refused, healthy live statistics, full-time Not applied and
the statistics-failure/disclosure/stress views.
Use the interactive prototype and 80-check Gate-E evidence to support questions about behavior;
an image approval alone supplies no runtime or independent usability evidence.

| Review field | Recorded owner review |
|---|---|
| Review ID | `UX-GG-S0-OWNER-20260930-03` |
| Reviewer | Anton Zymin, project owner |
| Date | September 30, 2026; explicit acceptance received at 11:38:46 PDT (18:38:46 UTC) |
| Prototype / image version | v0.4 at `13c2c095bea6876ffd010dfad1df23fb4f137868` (`13c2c09`) |
| Images reviewed | All 18 paths listed below; fingerprints in the pinned `walkthrough.json` |
| Complete task / state coverage | S0-T1–T7 plus substitution; packet §§9–12 and all §9.4 images |
| Supporting interaction evidence | `UX-GE-S0-20260930-04`, 80 passing scripted browser checks; no independent usability/runtime claim |
| Findings | S0-G-001–007 and S0-G-009–012 fixed/retested; S0-G-008 `ACCEPT FOR CURRENT GATE`; earlier B/E ledgers apply; three minor leftovers enter H backlog (packet §12.5) |
| Carried Majors | S0-B-002 and S0-B-004 `ACCEPT FOR CURRENT GATE`; verbatim rationale and release conditions below; neither is closed as a production finding |
| C-DEC-1 | ACCEPTED — labelled partial live figures; report partial figures behind incomplete disclosure, score/result/Return outside |
| Full-time statistics access | S0-G-008 ACCEPTED — no reopening at full time; retain previously open frozen panel; note points to report, View match report primary |
| Image approval | Owner explicitly accepted the proposed decisions and approved v0.4 / `13c2c09` / all 18 PDFs; statement below |
| Gate G | PASS — complete owner image approval and dispositions; explicit C-DEC-1/full-time decision; no unresolved Blocker or unaccepted Major |
| Gate H / I | H OPEN for high-fidelity production/review; H is not passed. I remains blocked until the owner separately approves H images. PR #470 remains blocked on I. |

**Owner confirmation, verbatim:**

> I accept the proposed owner decisions and approve v0.4 at `13c2c09`, all 18 PDFs.

**Accepted owner decisions, verbatim.** The owner adopted the proposed wording through the
confirmation above; Claude's proposal alone was not recorded as approval.

> Owner decisions, September 30, 2026 (Anton Zymin):
>
> - **C-DEC-1 — Accepted.** Partial live figures stay visible and labelled. In the report, partial figures sit behind the incomplete-statistics disclosure; score, result and Return stay outside it.
> - **S0-G-008 — Accepted: `ACCEPT FOR CURRENT GATE`.** Statistics can't be reopened at full time. A note points to the match report, and "View match report" stays the primary action.
> - **S0-B-002 (Major) — Accepted for G.** Reason: this is a simulated design review; no production adapter is needed to judge the design. Release condition: the Gate-I handoff specifies the adapter that reads the engine's command logs (`Driver.Log`/`FailedCommands`) to show Pending/Applied/Refused/Not applied, and Gate J verifies it. That feedback does not ship without the adapter.
> - **S0-B-004 (Major) — Accepted for S0.** Reason: shirt numbers are the only player identity S0 has. Release condition: recheck at the Gate-H image review. It closes when player names are available (S1 or client work). If H shows players can't be identified, it reopens as a Blocker.
> - **Image approval:** I approve prototype v0.4 at commit `13c2c09`, all 18 PDFs listed in the packet's §9.4, for Gate G. This approves low-fidelity design only: it supplies no runtime or independent usability evidence, and it does not approve H or I.

**Reviewed images at the pinned commit** (paths are relative to the repository root):

| Image path | SHA-256 at `13c2c09` |
|---|---|
| [`docs/design/s0-prototype/evidence/mentality-dialog.pdf`](s0-prototype/evidence/mentality-dialog.pdf) | `b5d0078ba87a4505df773abc528e1bb591cff3d918951dd492f3a0677d651c72` |
| [`docs/design/s0-prototype/evidence/mm.pdf`](s0-prototype/evidence/mm.pdf) | `ce49faa20c6f47979c3cb794efff27bd19fe32acda0384287e6e0d02b37eb053` |
| [`docs/design/s0-prototype/evidence/mv-0.pdf`](s0-prototype/evidence/mv-0.pdf) | `7979e1ccb4a005ab46f5ec079d6eff0564df834e8ac1d6a6c47b987781e9b76c` |
| [`docs/design/s0-prototype/evidence/mv-ft-not-applied.pdf`](s0-prototype/evidence/mv-ft-not-applied.pdf) | `0211872f6d215aa34e4117bd63e3fa38c869992325f9c4c1d8d4d07fbc6351bd` |
| [`docs/design/s0-prototype/evidence/mv-ft.pdf`](s0-prototype/evidence/mv-ft.pdf) | `52daf84ea4b3b2e84b593a29bd547a6b276f19bbec8c241a21d6bbe6a700b571` |
| [`docs/design/s0-prototype/evidence/mv-l.pdf`](s0-prototype/evidence/mv-l.pdf) | `b6fae8521d0388ee6ae717cb7b32fef63ac95d646adfbe3da1362b53d43d6e74` |
| [`docs/design/s0-prototype/evidence/mv-live-applied.pdf`](s0-prototype/evidence/mv-live-applied.pdf) | `edb263e4614c074fd46e527201a2995066f72e042828a837cd87f88fbf965f37` |
| [`docs/design/s0-prototype/evidence/mv-live-pending.pdf`](s0-prototype/evidence/mv-live-pending.pdf) | `964054d13292ce0b96e9f03d53fe407a5641a9064751f440a18f6e622e105b31` |
| [`docs/design/s0-prototype/evidence/mv-live-refused.pdf`](s0-prototype/evidence/mv-live-refused.pdf) | `4ccb71d73190d0857f17126f316c473de80236b8299db951447245998b1b9cf0` |
| [`docs/design/s0-prototype/evidence/mv-live-statistics.pdf`](s0-prototype/evidence/mv-live-statistics.pdf) | `411ea5ff9b7a12603a4779a071d7028822bccf75189d7ba2a1cce92572563791` |
| [`docs/design/s0-prototype/evidence/mv-p.pdf`](s0-prototype/evidence/mv-p.pdf) | `31764ec1927b1f4586119aaf9939021ceb11d7a9e5b9572635f563d6f57db9bd` |
| [`docs/design/s0-prototype/evidence/mv-paused-pending.pdf`](s0-prototype/evidence/mv-paused-pending.pdf) | `1c8bf7efc8d9c683c722213c72b68793b25e8bb94f49ab403a0a37b23c8df952` |
| [`docs/design/s0-prototype/evidence/pr.pdf`](s0-prototype/evidence/pr.pdf) | `c3f3e8a6286fbaece9df18085c857909166d9e2a53c145852db85c7d0ff9fe44` |
| [`docs/design/s0-prototype/evidence/report-incomplete.pdf`](s0-prototype/evidence/report-incomplete.pdf) | `f7e582d2f4474f6f54089f7480a6a9a1f81236508d94114961cd85dda74a614c` |
| [`docs/design/s0-prototype/evidence/report-partial-open.pdf`](s0-prototype/evidence/report-partial-open.pdf) | `32d417d70d67af860503098186419766a4628002ffd2c9d61ce4404d37bab908` |
| [`docs/design/s0-prototype/evidence/stress-fault-1366.pdf`](s0-prototype/evidence/stress-fault-1366.pdf) | `2dddfbd394fcd379f4e39856446b63d22329e014f33352b62ca2409996fc2f33` |
| [`docs/design/s0-prototype/evidence/substitution-dialog.pdf`](s0-prototype/evidence/substitution-dialog.pdf) | `c2242dd8ae17cc3c7107be77e7e4ef091d515aac238a80628c901dcac8ae7d82` |
| [`docs/design/s0-prototype/evidence/ts.pdf`](s0-prototype/evidence/ts.pdf) | `9b6d966d9914348dd7da04f4d57e29b00986d2b4e16c7b95c433ba3f2d79c4d0` |

**Historical recording boundary:** approval-recording commit `d99a1a9` changed documents only;
at that commit the full prototype/evidence tree was byte-identical to `13c2c09`. The approved
version remains v0.4 at that pinned commit. Its 18 PDF files and root run-04 evidence are still
unchanged. Later Codex corrections advance source files to v0.5 and put all new evidence in
`evidence/v0.5/`; the v0.4 approval is not extended to those files.

Tester sessions and attestations are not required for S0. Gate F must still supply the complete
interactive task. Gate H follows the owner-approved low-fidelity images; the owner also reviews
the high-fidelity images before the Gate-I handoff. Gate J verifies the real client.

#### 9.1.1 v0.5 single-image delta review — approved

Codex review found premature/direct-state fault initialization, one-tick-late normal fault activation,
full-time wording claiming continued play, and a normal-text check that used `pseudo=0` despite
presence-based pseudo-localization. These are fixed/retested in v0.5; the prior run-04 claims about
normal-text coverage and the fault timeline are superseded by the actual run-05 checks.

| Field | Current correction-review record |
|---|---|
| Review ID | `UX-GG-S0-OWNER-20260930-04` |
| Reviewer | Anton Zymin |
| Revision / evidence | Prototype v0.5; baseline `UX-GE-S0-20260930-05` at `a859ea1`, 85 passing checks (unchanged historical record). Focused `UX-GE-S0-20260930-05-DELTA-01`: three passing checks and current seven-source/19-image fingerprints in `s0-prototype/evidence/v0.5/full-time-fault-delta.json`. No new full evidence run. |
| Approved image / carry-forward | Anton Zymin approved `s0-prototype/evidence/v0.5/mv-ft-statistics-fault.pdf` at `0e8bd2b6b758b3d9768bf1b1f2551176e845e301`. Text extraction verifies the other 18 paths in packet §9.4 match approved v0.4 apart from version text; carry those forward. This correction preserves those 18 v0.5 PDFs byte-for-byte and all original approved PDFs. |
| Findings | S0-G-013–016 `FIX NOW`, fixed/retested; previous dispositions remain. S0-H-004 adds disabled Close statistics presentation to H backlog. |
| Owner decisions | C-DEC-1 / S0-G-008 and S0-B-002 / S0-B-004 acceptance conditions remain as actually adopted above |
| Owner review date / reviewed commit / statement | September 30, 2026, 13:43 America/Los_Angeles (20:43 UTC); `0e8bd2b6b758b3d9768bf1b1f2551176e845e301`; actual reply: “approved”. Scope is the corrected single-image delta presented immediately before that reply, with the other 18 carried forward. |
| Gate G for v0.5 | PASS — actual owner delta approval recorded; prior v0.4 approval and accepted decisions remain pinned above |
| H / I | H OPEN for high-fidelity work and separate owner image review; H is not passed. I remains blocked until separately owner-approved H images; #470 remains blocked on I. |

**Actual owner confirmation (verbatim):**

> approved

The preceding review request identified the corrected full-time statistics-fault PDF at `0e8bd2b6b758b3d9768bf1b1f2551176e845e301`,
said the other 18 carry forward from approved v0.4, and stated that this delta approval resumes H.
This record applies that scope; it does not create a broader owner statement or H/I approval.
The earlier C-DEC-1/S0-G-008 decisions and S0-B-002/S0-B-004 reasons/release conditions remain as adopted in §9.1.

**Pinned v0.5 image set:** the one new image is approved by this delta; the other 18 are carried forward.

| Image path | Review basis | SHA-256 at reviewed commit |
|---|---|---|
| [`docs/design/s0-prototype/evidence/v0.5/mentality-dialog.pdf`](s0-prototype/evidence/v0.5/mentality-dialog.pdf) | Carried from approved v0.4; version text only | `62703007c3b7898414f96cc445970373bf67fe1b2a1e05f5507ce5c7dd97c5ac` |
| [`docs/design/s0-prototype/evidence/v0.5/mm.pdf`](s0-prototype/evidence/v0.5/mm.pdf) | Carried from approved v0.4; version text only | `ca250ec4314275f3719f3c86ec3475eb543b8a1e85ca605d66deeee64da64195` |
| [`docs/design/s0-prototype/evidence/v0.5/mv-0.pdf`](s0-prototype/evidence/v0.5/mv-0.pdf) | Carried from approved v0.4; version text only | `97a84b504ffaea95fba53615afa440420045fbc6b578f31cf56dc04b84ee279c` |
| [`docs/design/s0-prototype/evidence/v0.5/mv-ft-not-applied.pdf`](s0-prototype/evidence/v0.5/mv-ft-not-applied.pdf) | Carried from approved v0.4; version text only | `1042e2edb6c1aead93cc7bc84eb24e64ec058fac2f9e8823ea779c8012ee5a75` |
| [`docs/design/s0-prototype/evidence/v0.5/mv-ft-statistics-fault.pdf`](s0-prototype/evidence/v0.5/mv-ft-statistics-fault.pdf) | Owner-approved delta | `0a2b1af953f0262979a19fab831aa4d0f8ca45d574c7bbb5bbb9bb4fc90a0751` |
| [`docs/design/s0-prototype/evidence/v0.5/mv-ft.pdf`](s0-prototype/evidence/v0.5/mv-ft.pdf) | Carried from approved v0.4; version text only | `c3dd83cc75f11e8afc01549abae87a47511ebe3368c53843a90ab1ad7564bf44` |
| [`docs/design/s0-prototype/evidence/v0.5/mv-l.pdf`](s0-prototype/evidence/v0.5/mv-l.pdf) | Carried from approved v0.4; version text only | `b0275702b8933ef5e11e9ab074273788463fc29ef58a808ac7c5ca41caa5e06a` |
| [`docs/design/s0-prototype/evidence/v0.5/mv-live-applied.pdf`](s0-prototype/evidence/v0.5/mv-live-applied.pdf) | Carried from approved v0.4; version text only | `6ec218138a3d6e003322f63254ae239364d66b48f681426963b67720f0cb763a` |
| [`docs/design/s0-prototype/evidence/v0.5/mv-live-pending.pdf`](s0-prototype/evidence/v0.5/mv-live-pending.pdf) | Carried from approved v0.4; version text only | `8f43b54767a8e6862622a5729ab0bc46074a7dfe1cfc64e38842f46db12868de` |
| [`docs/design/s0-prototype/evidence/v0.5/mv-live-refused.pdf`](s0-prototype/evidence/v0.5/mv-live-refused.pdf) | Carried from approved v0.4; version text only | `9d3d49a042730754ed8ea46256c51a0ad015f4d1b39bc01908547545417053aa` |
| [`docs/design/s0-prototype/evidence/v0.5/mv-live-statistics.pdf`](s0-prototype/evidence/v0.5/mv-live-statistics.pdf) | Carried from approved v0.4; version text only | `f2c585e18c207532cca9f61a87993a6445961ffbd3b61877102765d01ac322a7` |
| [`docs/design/s0-prototype/evidence/v0.5/mv-p.pdf`](s0-prototype/evidence/v0.5/mv-p.pdf) | Carried from approved v0.4; version text only | `5bda7dd0fb201e3bdb510ce6a0b1cc9c6297e74dfe21f24d5cf43391ecf0e37d` |
| [`docs/design/s0-prototype/evidence/v0.5/mv-paused-pending.pdf`](s0-prototype/evidence/v0.5/mv-paused-pending.pdf) | Carried from approved v0.4; version text only | `343e36bdb0efe91f0ae09d50a5deecd12d294b56f6c03783b952ee739954a5fd` |
| [`docs/design/s0-prototype/evidence/v0.5/pr.pdf`](s0-prototype/evidence/v0.5/pr.pdf) | Carried from approved v0.4; version text only | `b57b6a6e13c36cd185f7116c6774047fde1d410b10da68f9bc930fa1ea092f79` |
| [`docs/design/s0-prototype/evidence/v0.5/report-incomplete.pdf`](s0-prototype/evidence/v0.5/report-incomplete.pdf) | Carried from approved v0.4; version text only | `6fdf901c556c5eb84a92b04cb3cd4e22ec7d988c889d1601e37236e0b84c76d7` |
| [`docs/design/s0-prototype/evidence/v0.5/report-partial-open.pdf`](s0-prototype/evidence/v0.5/report-partial-open.pdf) | Carried from approved v0.4; version text only | `f9ac7f8d853c5fa267d80ab8d1c896a6f04f01129db7db827957cef0208455e9` |
| [`docs/design/s0-prototype/evidence/v0.5/stress-fault-1366.pdf`](s0-prototype/evidence/v0.5/stress-fault-1366.pdf) | Carried from approved v0.4; version text only | `11fc5c15e84eb792e1b15039f53278f9b895a24c44d62b338936529d642cd6b5` |
| [`docs/design/s0-prototype/evidence/v0.5/substitution-dialog.pdf`](s0-prototype/evidence/v0.5/substitution-dialog.pdf) | Carried from approved v0.4; version text only | `82445da4c7e8f975c4bd2137843583ad5fdbe96885780046cf70e02dc1a1be4e` |
| [`docs/design/s0-prototype/evidence/v0.5/ts.pdf`](s0-prototype/evidence/v0.5/ts.pdf) | Carried from approved v0.4; version text only | `ef89daa7e2a57b97d787a166c6f431db4097e41fe3e00b38997e44f6590db374` |

**Pinned prototype sources:**

| Source path | SHA-256 at reviewed commit |
|---|---|
| `docs/design/s0-prototype/index.html` | `18869864ca1fe8eb233a94e48aa23de6d5626f05e9292e8c4e9a39954b60cf41` |
| `docs/design/s0-prototype/prototype.css` | `60aaede3535622638dcdce0abd89c366b00404663c9df02f9c11e4e2ae557d87` |
| `docs/design/s0-prototype/model.js` | `bf64020cacacf0cdd0275ee3da55af349bcdf3f248d132cb7a03060354ec7f9c` |
| `docs/design/s0-prototype/prototype.js` | `dfd9c8cd639bc0c34bc0bc8d92039c74ef9ac68165dade8dab5d4b99b78b18d5` |
| `docs/design/s0-prototype/reference-data.js` | `fafa28bbd78b067bb63241a64042ebd8d0cea7e8050c95d7db8fdda9feb8ad69` |
| `docs/design/s0-prototype/scenario-data.js` | `05412989bdcbd11eb72fb76673f44d93d190e257ec6dbe529600cce1e1cd6866` |
| `docs/design/s0-prototype/verify.cjs` | `18bf2d1642cd0f9d2116d6a4d016785085976037aa14012f96476b1451f0851a` |

Supporting evidence: `UX-GE-S0-20260930-05-DELTA-01` (three focused checks), delta JSON SHA-256 `d6e61c51b157736199ec5dff21910c24bf046b67aaf352d77da84cada9e8de7e`; baseline run `UX-GE-S0-20260930-05` (85 checks) remains historical at `a859ea1`.

**Recording boundary:** this approval-recording change edits Markdown documents only. All seven prototype
sources, all 19 v0.5 PDFs, the focused delta JSON, and all earlier evidence remain byte-identical to
the reviewed commit `0e8bd2b6b758b3d9768bf1b1f2551176e845e301`. No image regeneration or new test run accompanies the record.

#### 9.1.2 S0 Gate H owner image approval

**Review ID:** UX-H-S0-OWNER-20260930-01.\
**Owner:** Anton Zymin.\
**Actual confirmation:** “images approved”.\
**Confirmation time:** September 30, 2026 at 19:41:55 America/Los_Angeles (October 1, 2026 02:41:55 UTC).\
**Reviewed commit:** `4a6220cf4c394130c29f38716ed3abde1f60477b` (PR #474, high-fidelity reference v0.1).

The confirmation follows the complete 23-view review index and H criteria given in this conversation:
readability, understandable Mentality/substitution choices, coherent feedback/markers, complete states,
visual/stress quality and shirt-number player identification. It approves all 23 views in both PNG/PDF
formats pinned below. No new owner statement or broader production/merge approval is inferred.
The prior work-start instruction and the actual G approvals/pins in §§9.1–9.1.1 remain unchanged.

| Field | Recorded result |
|---|---|
| H verdict | **PASS**, explicit separate owner image approval of v0.1 at 4a6220c |
| Review scope | Complete 23-image set / 46 PNG/PDF files, all hashes below |
| Finding closure | S0-H-001–004 fixed/retested and owner-approved in the image set; Mentality comparison and illustrative Applied identity approved |
| S0-B-004 identity | H shirt-number recheck accepted for this reference; production player-name requirement remains OPEN with its prior release condition |
| S0-B-002 feedback | Production adapter and runtime verification remain I/J obligations; image approval does not close them |
| Scripted support | UX-H-S0-20260930-01, 90 PASS checks; no new run or image regeneration accompanies approval recording |
| Evidence JSON | `docs/design/s0-high-fidelity/evidence/v0.1/walkthrough.json`, SHA-256 `c66d32fa481cc2faf1ddee98a70649eeb2db757aae2f10514e258f86da8b30f3` |
| I / #470 | I OPEN for implementation handoff; I is not passed. #470 remains blocked on the completed Gate-I handoff. |
| PR boundary | #473 merged; reviewed H head CI run 36778408822 succeeded. Approval-recording head requires fresh CI before merge. No merge authorized here. |

Owner image review supplies no independent participant or Unity/runtime evidence. This recording
changes Markdown documents only. All four H executable sources, three shared G sources, 46 image
files and the walkthrough JSON remain byte-identical to the reviewed commit.

**Approved H image set:**

| Path | SHA-256 |
|---|---|
| `docs/design/s0-high-fidelity/evidence/v0.1/mentality-comparison.pdf` | `b4cac7702d919e6b879a47687f8c6a177c572f11052e3fba75e599f283039dc7` |
| `docs/design/s0-high-fidelity/evidence/v0.1/mentality-comparison.png` | `1d79d1e9a369cc5ca693afe2a3235a8069cb575fd6283ba889c351c40c2a5bd6` |
| `docs/design/s0-high-fidelity/evidence/v0.1/mentality-dialog.pdf` | `c57410b7c6f51c8f24cb9036a9f82528c18948e66a879cf17a1fc0228c2f331d` |
| `docs/design/s0-high-fidelity/evidence/v0.1/mentality-dialog.png` | `e668305e0f5e5c7ede797d4b653ddd3b994bb278ec0696eb54d9f0d5a27247c2` |
| `docs/design/s0-high-fidelity/evidence/v0.1/mm.pdf` | `bf6041848b92c759ece422fcf5ed7b2f008e42e1d3bfeba3150ebefd7a072b72` |
| `docs/design/s0-high-fidelity/evidence/v0.1/mm.png` | `d8e8ea02f14b42b5f050d9e6e31b394e1adb0ec8b98194253ff461d6f231f1ee` |
| `docs/design/s0-high-fidelity/evidence/v0.1/mv-0.pdf` | `6a47d83d74c0786039c8284ad8d6685dae8bed955b87a65a34ff9a088bfecafd` |
| `docs/design/s0-high-fidelity/evidence/v0.1/mv-0.png` | `653564b918441f88ef5bd2bc013333b9174b401dd30528fa5091a37d9b32b489` |
| `docs/design/s0-high-fidelity/evidence/v0.1/mv-ft-not-applied.pdf` | `0db0bcfea0ce4599da04a5731494349f88a630cd3e50306037dd58e28bec802f` |
| `docs/design/s0-high-fidelity/evidence/v0.1/mv-ft-not-applied.png` | `d796f56dd1bf0de26973e92af0715c03be4c0e578e9a0a61842d486efde4174a` |
| `docs/design/s0-high-fidelity/evidence/v0.1/mv-ft-statistics-fault.pdf` | `e1b55c5d0088f93e1023b9d00ed6429ce47baebbd69154bead6a6f13067779ca` |
| `docs/design/s0-high-fidelity/evidence/v0.1/mv-ft-statistics-fault.png` | `ce0db0003e68ee6f832e2b4271a380ed949c1013fa6e495703073ca47459be15` |
| `docs/design/s0-high-fidelity/evidence/v0.1/mv-ft-statistics-open.pdf` | `77b628fbdec404d650f1eb711ae07971a330a6aad9e57e6fc7900ded730bc44d` |
| `docs/design/s0-high-fidelity/evidence/v0.1/mv-ft-statistics-open.png` | `4c553bc26c1257c5109f6f24aa6ec276d07c16d2a0c39c4ead6c6d8bf3433e25` |
| `docs/design/s0-high-fidelity/evidence/v0.1/mv-ft.pdf` | `2aaf7f230d66b847ff3870a83ac8c4df5182f4701b518fb4a414c32aacf4ed2c` |
| `docs/design/s0-high-fidelity/evidence/v0.1/mv-ft.png` | `6058fe549a2c740bd3bf559f6d4a8bc6fa05ccb2323624f59e8764db865c43f4` |
| `docs/design/s0-high-fidelity/evidence/v0.1/mv-l.pdf` | `114a092be507ca99c15c141cc344b577d32ba44fec64e0795f04d8689462014a` |
| `docs/design/s0-high-fidelity/evidence/v0.1/mv-l.png` | `04e5eff508185b971edc605ded83d842cfbebef9bdec7b8ca7c0bb6366e5c5da` |
| `docs/design/s0-high-fidelity/evidence/v0.1/mv-live-1366.pdf` | `5f00d02a0f3d86c983ea6b90b30a94c778e4ed05c8cf314ad3cf8cc62a148331` |
| `docs/design/s0-high-fidelity/evidence/v0.1/mv-live-1366.png` | `4ad2efd52ea0ca9d57fd192be5290eaa9d0260fd30be094b6a872d4fc76a1b75` |
| `docs/design/s0-high-fidelity/evidence/v0.1/mv-live-2560.pdf` | `c13dcc14295bedc47e3166349d13668bbfafa3dce7749194c67e4e3756979a78` |
| `docs/design/s0-high-fidelity/evidence/v0.1/mv-live-2560.png` | `968ce5ce17d79773fbcd3075ea36558b01db06f5e4c3af0f736301b725ceeb92` |
| `docs/design/s0-high-fidelity/evidence/v0.1/mv-live-applied.pdf` | `1b514af0f73394df974bd0802112589f72b4b5862657930de917883bc3668cfa` |
| `docs/design/s0-high-fidelity/evidence/v0.1/mv-live-applied.png` | `2c97fc54911d38caca12faf876fcf60b17dcbc9f12daf7ae5e220bf149325608` |
| `docs/design/s0-high-fidelity/evidence/v0.1/mv-live-pending.pdf` | `85d7f269b20cd81eb7f7b5503bc373fa73e36c09de8862180f652ceb41a2a7e7` |
| `docs/design/s0-high-fidelity/evidence/v0.1/mv-live-pending.png` | `ad767498d002eecefae577b544169ea2b4fa392f1f694ea3777b767d95581b8f` |
| `docs/design/s0-high-fidelity/evidence/v0.1/mv-live-refused.pdf` | `d9ced5aa53a7499b06a5a5b31b8ce8c7065b2eca4e06dc1411e98cefb83819f9` |
| `docs/design/s0-high-fidelity/evidence/v0.1/mv-live-refused.png` | `c9b1afaf6b01c4930c1886126781a3e34e9ee00254d061f8cc4fd4f93f7938c1` |
| `docs/design/s0-high-fidelity/evidence/v0.1/mv-live-statistics.pdf` | `83afb4a37dbbf94d11f07f10591752c864b2834153b92501c02cefac6d6ba6c1` |
| `docs/design/s0-high-fidelity/evidence/v0.1/mv-live-statistics.png` | `298b1756a273f39fa69d3ba06eb0ddddb2f49b52bdc68bf07deb705b8788177d` |
| `docs/design/s0-high-fidelity/evidence/v0.1/mv-p.pdf` | `9af9ee627f919aea44c2a0d540b843fda18296b2b25762a6b755bfa815fe277b` |
| `docs/design/s0-high-fidelity/evidence/v0.1/mv-p.png` | `aec344102d5172ff134bd35e1b5e3e01d4e99a190773d3bb75039ca59f41099f` |
| `docs/design/s0-high-fidelity/evidence/v0.1/mv-paused-pending.pdf` | `d23a53cf1d457dd4b7411a406d6f1dce69a90140a37d0b63ec0dbba0cba257fd` |
| `docs/design/s0-high-fidelity/evidence/v0.1/mv-paused-pending.png` | `7ea41f19bc4f40dd78f180276b2d85e3aad76b810519277bb195b7e6cb6883ce` |
| `docs/design/s0-high-fidelity/evidence/v0.1/pr.pdf` | `888ecd00df7bbeb34de7910ece495d565154113dabf9320250c4eb38d5c20f5d` |
| `docs/design/s0-high-fidelity/evidence/v0.1/pr.png` | `379a793f302f4892bf9fd1c25733a9ad9652cbae202996b7a71e9efaaa6d125c` |
| `docs/design/s0-high-fidelity/evidence/v0.1/report-incomplete.pdf` | `1b3b738bb56466e73d8e6842d968af4ab6cc432c3334db88c7ce60e4b7707386` |
| `docs/design/s0-high-fidelity/evidence/v0.1/report-incomplete.png` | `3646a114422d3836188746f04d793459a1728b97adb893ffde2ed187f1d5641a` |
| `docs/design/s0-high-fidelity/evidence/v0.1/report-partial-open.pdf` | `a6e7d7119887caff4572c00df8b41d23f9d8a6b397f7558672cad70a08b41d05` |
| `docs/design/s0-high-fidelity/evidence/v0.1/report-partial-open.png` | `46c4caac41dbfad84c718a9ec8aa726d4b821bd560a3a61c274694a429cfce59` |
| `docs/design/s0-high-fidelity/evidence/v0.1/stress-fault-1366.pdf` | `d7fad02b0fa1bfc06d53a0a1aa2b0492c35b7b85c24e961ba3acc95c840d4612` |
| `docs/design/s0-high-fidelity/evidence/v0.1/stress-fault-1366.png` | `1b90a1a2ccea2f3a2701594b251668d74e3a4e10373c34ca0f7760b8a365ca60` |
| `docs/design/s0-high-fidelity/evidence/v0.1/substitution-dialog.pdf` | `d1d2db30697ee42c37ece5bc0edc9a7f5135f5699438d1d912dca55d811b7522` |
| `docs/design/s0-high-fidelity/evidence/v0.1/substitution-dialog.png` | `a5f0a97f5db0674089c8076dc78f6cd44ab311fc37e6961f660c1ac097e0c3b1` |
| `docs/design/s0-high-fidelity/evidence/v0.1/ts.pdf` | `b6c04d339ec668009e541ae4a29e5ce4dc7b33e944ff8323908d8c16e464a8e9` |
| `docs/design/s0-high-fidelity/evidence/v0.1/ts.png` | `9e4dfd710ba943856e7df127f0fef3a9966e9f45207568362a1079f49d1dd225` |

**Reviewed executable sources:**

| Path | SHA-256 |
|---|---|
| `docs/design/s0-high-fidelity/index.html` | `b1fd01170d8dcb560d80bcae725884d878ef622b31ffe947b769b635bc536d56` |
| `docs/design/s0-high-fidelity/prototype.css` | `89ba17f86ce4df7abaf5ff74517e5e672192b39977ff9a37a0da790ff148aedc` |
| `docs/design/s0-high-fidelity/prototype.js` | `a96a7d6e989fbae1ff6cbf9de64f73a4e3cd9a39a9bc818e8c2af72236555cdd` |
| `docs/design/s0-high-fidelity/verify.cjs` | `1831be61f6c7bffd50584443beadcde8bffbde84f67ea652d8afdbe8c564a16c` |
| `docs/design/s0-prototype/model.js` | `bf64020cacacf0cdd0275ee3da55af349bcdf3f248d132cb7a03060354ec7f9c` |
| `docs/design/s0-prototype/reference-data.js` | `fafa28bbd78b067bb63241a64042ebd8d0cea7e8050c95d7db8fdda9feb8ad69` |
| `docs/design/s0-prototype/scenario-data.js` | `05412989bdcbd11eb72fb76673f44d93d190e257ec6dbe529600cce1e1cd6866` |

#### 9.1.3 S0 Gate H v0.2 owner image approval

**Status: PASS for H v0.2.** Anton Zymin replied “images approved” on October 1, 2026 at 12:35:32 America/Los_Angeles (19:35:32 UTC)
following the complete 23-view v0.2 index and the review corrections. The reviewed commit is
`a1044d525703a42f274b6643f9c96c3c46dc3592`. This confirmation approves all 23 views and their PNG/PDF pairs.
Gate I remains OPEN and now uses approved v0.2 as its handoff baseline; I is not passed.
#470 remains blocked until the Gate-I handoff is complete.

The actual v0.1 approval in §9.1.2 and all its evidence/pins remain unchanged. Before this
confirmation, v0.1 remained the valid I baseline; the earlier author-imposed reopening/hold
was corrected in protocol v0.25. The actual owner approval above now supplies the separate
v0.2 acceptance that was pending. No PR merge or Unity/runtime acceptance is inferred.

| Field | Approval record |
|---|---|
| Owner / actual confirmation | Anton Zymin — “images approved” |
| Confirmation time | October 1, 2026 at 12:35:32 America/Los_Angeles (19:35:32 UTC); `2026-10-01T12:35:32-07:00` |
| Reviewed H commit | `a1044d525703a42f274b6643f9c96c3c46dc3592` — PR #475 |
| Base | Approval-recording head `654c4f8d663c437aed117292fc6c666acabd61e7` of PR #474 |
| Revision source | Claude’s eight fixes at `f7f44b513f5b496ef0d9b2853c2920e49aeb6660`, plus export isolation/completeness and approval-history repairs |
| Approved images | All 23 views / 46 PNG/PDF paths in the [owner-review index](s0-high-fidelity/README.md#owner-review-images), fingerprinted below |
| Finding dispositions | S0-H-005–013 fixed/retested and accepted in approved v0.2 images; prior v0.1 closures preserved |
| Production boundary | S0-B-004 still requires production names; S0-B-002 feedback adapter and runtime checks remain I/J obligations |
| Scripted support | `UX-H-S0-20261001-02`, 99 PASS checks; no new run or image regeneration accompanies this approval record |
| Export evidence | All PDFs have one page, match PNG dimensions and contain every displayed statistics row label; export styles removed after each capture |
| Evidence JSON | `docs/design/s0-high-fidelity/evidence/v0.2/walkthrough.json`, SHA-256 `89837fb0880f4fd7a6726e6cec538fee4a87cdc18a66185ce1ad160a473219fc` |
| I / #470 | I OPEN using approved v0.2; I not passed, #470 blocked on completed I |
| PR / CI boundary | #475 remains a stacked draft into the Gate-H branch. CI only targets main; a fresh CI run is required before the eventual main merge. Image approval does not authorize a merge. |

This recording changes Markdown documents only. All executable sources, all 46 v0.2 images
and the walkthrough JSON remain byte-identical to the reviewed commit. The hashes pin reviewed
bytes; different rendering environments may produce different pixels/wrapping on a rerun.
Owner review supplies no independent participant or Unity/runtime evidence.

**Approved v0.2 executable sources:**

| Path | SHA-256 |
|---|---|
| `docs/design/s0-high-fidelity/index.html` | `12d640deef0f509b73b1b6977546e1063b5992a64b756b619d08c814853d7299` |
| `docs/design/s0-high-fidelity/prototype.css` | `faabe996f65d4a4a84052f3c4c93ec06980cfa35f4dc1999abc08616318f1f97` |
| `docs/design/s0-high-fidelity/prototype.js` | `7c43266b2c6567209f504b572ea685ffccb3ea5bf5c0a853b01fef6672f03472` |
| `docs/design/s0-high-fidelity/verify.cjs` | `8c48c41f39ff96c16cb60e3ad12fac33c3ae4042d395be9fff6ee0dcce9a06c7` |
| `docs/design/s0-prototype/model.js` | `bf64020cacacf0cdd0275ee3da55af349bcdf3f248d132cb7a03060354ec7f9c` |
| `docs/design/s0-prototype/reference-data.js` | `fafa28bbd78b067bb63241a64042ebd8d0cea7e8050c95d7db8fdda9feb8ad69` |
| `docs/design/s0-prototype/scenario-data.js` | `05412989bdcbd11eb72fb76673f44d93d190e257ec6dbe529600cce1e1cd6866` |

**Approved v0.2 image set:**

| Path | SHA-256 |
|---|---|
| `docs/design/s0-high-fidelity/evidence/v0.2/mentality-comparison.pdf` | `d1b1531aeadeed26600d5ffa071cc716823ea27b475ddbdc7921a23b20832308` |
| `docs/design/s0-high-fidelity/evidence/v0.2/mentality-comparison.png` | `2c338b33f1010876e3d8b0db2e8fc4333471994725d1d928f697f20e514ebe0b` |
| `docs/design/s0-high-fidelity/evidence/v0.2/mentality-dialog.pdf` | `8e33870a9754f673dd38001f1b10e5d675205a65c4770b2cad28e6723668d067` |
| `docs/design/s0-high-fidelity/evidence/v0.2/mentality-dialog.png` | `69fd4380727c0597dca0162207ad7b31269d9d7d5f1feea3d33a2bf603e03a91` |
| `docs/design/s0-high-fidelity/evidence/v0.2/mm.pdf` | `e90a86d0079afabaffdd8f747dfff28518716ce64ac5b31b13ca06b967592f99` |
| `docs/design/s0-high-fidelity/evidence/v0.2/mm.png` | `964be94f3055ce9d5c1176292a183b7f122d6dd780f8495cb62bcf3eb7dbd644` |
| `docs/design/s0-high-fidelity/evidence/v0.2/mv-0.pdf` | `3fce13ebb7facbf3043658cdb5eb564262b89d4dea5620ab19788ad548ceea3a` |
| `docs/design/s0-high-fidelity/evidence/v0.2/mv-0.png` | `0b05fdf76f7c94c29b0ab8a96d52e9c90eaa9f3edd421d5a0abc16841e598eb7` |
| `docs/design/s0-high-fidelity/evidence/v0.2/mv-ft-not-applied.pdf` | `43a5e253323aea31a9cff07fe250033a28f3673350db0b03f77a7f84d3751655` |
| `docs/design/s0-high-fidelity/evidence/v0.2/mv-ft-not-applied.png` | `60c677d645a114f1a3cd0659a21895b8321cb231300d13345252328c6e8ff8b5` |
| `docs/design/s0-high-fidelity/evidence/v0.2/mv-ft-statistics-fault.pdf` | `eff592643aa8d7b4d4498fac35c750a3b8aa74c9ebdd6dbd8e506f045f81dc6c` |
| `docs/design/s0-high-fidelity/evidence/v0.2/mv-ft-statistics-fault.png` | `aad1ea697c90b9e64af90ebaafa93676480224857d834c0d13774fed9463883e` |
| `docs/design/s0-high-fidelity/evidence/v0.2/mv-ft-statistics-open.pdf` | `7c04dd0142f61bd0966dd4bf4e53d5918c918e7c69196dee258d27031693ab9f` |
| `docs/design/s0-high-fidelity/evidence/v0.2/mv-ft-statistics-open.png` | `7e37f17adaea3728abfbe90d4d49f818887f3f8dbeabf3ad7efe554f3af2d607` |
| `docs/design/s0-high-fidelity/evidence/v0.2/mv-ft.pdf` | `03507cf896544f6900e5dd6cb0a2e711f6f89d83281197c6b26047bd0cc74f6e` |
| `docs/design/s0-high-fidelity/evidence/v0.2/mv-ft.png` | `c1f3fd74a9d89420164812b5b50620cdd4ff6d02ee79fff7f32b7b3d89b9ca21` |
| `docs/design/s0-high-fidelity/evidence/v0.2/mv-l.pdf` | `078ba17dd65362d5ae1039517104337d35e1e4db2a36a556a070d4d445c44c36` |
| `docs/design/s0-high-fidelity/evidence/v0.2/mv-l.png` | `404ec135537a289525e5c58771e58ad50ccbc1b432f77787c03ecb378334a5f2` |
| `docs/design/s0-high-fidelity/evidence/v0.2/mv-live-1366.pdf` | `658167fd5ff299852a0a5b0b5d618c8c4f2a3db0be7d8380e5b519d6516d9dda` |
| `docs/design/s0-high-fidelity/evidence/v0.2/mv-live-1366.png` | `705a63843b6c507a795b2edcbeed0e2df444e9f103b053dfb0837a2f98091521` |
| `docs/design/s0-high-fidelity/evidence/v0.2/mv-live-2560.pdf` | `dc5d71106a5b99319a3a24d1d75c56542ee83c7ca75cf99567cc6d6faccd2575` |
| `docs/design/s0-high-fidelity/evidence/v0.2/mv-live-2560.png` | `a98923f37ad1b069b3ce8c0a6f7e862d8433278b6ac018ad56fc03b2080814e4` |
| `docs/design/s0-high-fidelity/evidence/v0.2/mv-live-applied.pdf` | `ff89b7bca536c172394f0ed9e2a4c89b5cf60ef06fee322b2d8e10d18a4fa2b4` |
| `docs/design/s0-high-fidelity/evidence/v0.2/mv-live-applied.png` | `2b20ae56c92c7420f20b0ab2374346315fae651a5bc9944b99cbbbb6ec720dc4` |
| `docs/design/s0-high-fidelity/evidence/v0.2/mv-live-pending.pdf` | `cd288427ad2335f2d93fdbb25efa27d444c591f7058945e8b97ff4fc3caa1d7a` |
| `docs/design/s0-high-fidelity/evidence/v0.2/mv-live-pending.png` | `7bfc0a8a2fed91127abe7e245754b4d0b11a8040b8aeaeef40eaf83772af7924` |
| `docs/design/s0-high-fidelity/evidence/v0.2/mv-live-refused.pdf` | `7032ffcb57615f23cc12392da827d12b30ba961e1fd5cca49a91cebd24ec5aa3` |
| `docs/design/s0-high-fidelity/evidence/v0.2/mv-live-refused.png` | `1538a182d3cd70287bf09382dd071d1620f0cc2a5c682c03ca9ea80436b26f89` |
| `docs/design/s0-high-fidelity/evidence/v0.2/mv-live-statistics.pdf` | `493ebe4c57211aa4a82ec0d7c0a30c693db9e6a8f1c9489c0faaa6c67abfd5b9` |
| `docs/design/s0-high-fidelity/evidence/v0.2/mv-live-statistics.png` | `e24c574bc05639d70c4c361a147f6ea12ef1504c3a6a52395ca635ff856ab163` |
| `docs/design/s0-high-fidelity/evidence/v0.2/mv-p.pdf` | `19bb6339115c112c2cdc190f285094e4461c64e7ff5920355ff9d64ba2eb402e` |
| `docs/design/s0-high-fidelity/evidence/v0.2/mv-p.png` | `6455f83dff678bd544c0afa4da39bdb1f1f360b343927d31c8402927517b4f70` |
| `docs/design/s0-high-fidelity/evidence/v0.2/mv-paused-pending.pdf` | `1f61ce004d37a80ee58cb9bb88c55ab746149d681000a5ca8ef4fc8fcfe22a2f` |
| `docs/design/s0-high-fidelity/evidence/v0.2/mv-paused-pending.png` | `947e104edf4dc229ce87621c0ccbd41808d5316ebae9c5e51304e99739336a4a` |
| `docs/design/s0-high-fidelity/evidence/v0.2/pr.pdf` | `4be3a302768f49df8c44deda236cdbf15dbe286e6f9847d0a2c10552f0f6a031` |
| `docs/design/s0-high-fidelity/evidence/v0.2/pr.png` | `ca6c575b96b1e89ccfef06ae76626f47eb8b9366947cbf565096e201c850d9f8` |
| `docs/design/s0-high-fidelity/evidence/v0.2/report-incomplete.pdf` | `2bce618d0e2393832e1be54a1485186db32f4bdff0e96364e3e5bfbf2af74f1e` |
| `docs/design/s0-high-fidelity/evidence/v0.2/report-incomplete.png` | `2191e7344659b2680399295e6c03c14c2379f86737824fad45b5dacf5cf1f093` |
| `docs/design/s0-high-fidelity/evidence/v0.2/report-partial-open.pdf` | `96e4bf4adff3a4f2c437591006646730b1b75cb28684dbfa2ea7b1cb028392f2` |
| `docs/design/s0-high-fidelity/evidence/v0.2/report-partial-open.png` | `0a70a40aa11be47c66c2e3d0c2b0e67369df689f6864c11d8bba090b765fb300` |
| `docs/design/s0-high-fidelity/evidence/v0.2/stress-fault-1366.pdf` | `b3f0090c4c4aa99469c493947ae2fc92cc799dce05471881c93ccd06bfa4a761` |
| `docs/design/s0-high-fidelity/evidence/v0.2/stress-fault-1366.png` | `2ed69fe29fb577249793ea044d80b4fc60c3237a1f482f8d7f5fabf7fdd49d10` |
| `docs/design/s0-high-fidelity/evidence/v0.2/substitution-dialog.pdf` | `995bd228601f0d1b58e697bfd721047dfd2549ba9ecb25fbdd1559dc3be8c796` |
| `docs/design/s0-high-fidelity/evidence/v0.2/substitution-dialog.png` | `c8bf381fcd9bb5227ea11fbdc781db16bcd0b418d5c440986d4fe7bf93534b4f` |
| `docs/design/s0-high-fidelity/evidence/v0.2/ts.pdf` | `d8b95b500c7b19e18c07a82c1112dd485d44fdf3cae1cdef3eabd7d4c268cf7a` |
| `docs/design/s0-high-fidelity/evidence/v0.2/ts.png` | `52b18f6f236ac9cefcef6ad60c71b14e35206ddd9412b0fe41393300a4170a33` |

The superseded initial v0.2 exports remain recoverable at `f7f44b5`; their 98-check run
is not the corrected approved export evidence. G’s source/evidence and §§9.1–9.1.2 approval
records are unchanged.

#### 9.1.4 S0-H-014 long-name pseudo-locale validation delta

Codex review on PR #475 found that S0-H-010 exercised only `fixture=fault`:
`fixture=long-names&pseudo=1` left Home/Away outside the bracketed identity.
The expanded check reproduced 18 unlocalized identity occurrences before the renderer fix.
The complete identity now passes through the pseudo transform once; ordinary output and
non-pseudo long-name output are unchanged. Coverage scans both fault and long-name fixtures
in TS, live/full-time Match View and the report, including live statistics and both dialogs.

Run `UX-H-S0-20261001-03` passes the focused coverage check. A separate scratch-copy
full walkthrough passes 99/99 checks, including long-name/expanded-text geometry at all
three widths. Its regenerated exports are disposable verification output, not approved images.
[Focused evidence](s0-high-fidelity/evidence/v0.2/long-name-pseudo-delta.json)
SHA-256: `44dcb658d3fd93ed2ae593ebd55b3ae4f9d7e2cb44d969a512233045fd48dc60`. It pins current source hashes and the unchanged approved image hashes.

This is a source/verification delta, not a new owner approval. The two current source hashes
below differ from the approved source pins in §9.1.3; that approval remains tied to `a1044d5`.
All 46 approved images, the approved walkthrough JSON and §§9.1–9.1.3 records are unchanged.
H remains PASS and I remains OPEN using that approved baseline with this stress-fixture correction
recorded separately. #470 still requires completed I; no merge/runtime approval is inferred.

| Revised source | SHA-256 |
|---|---|
| `docs/design/s0-high-fidelity/prototype.js` | `562c6c26a7bbdd3cf018e6db32eeb7cbdd5652fd2f6a4bf815672c447a1e8178` |
| `docs/design/s0-high-fidelity/verify.cjs` | `adfc03d2f3cdcb5aaae8bbc3d00ff5a2e3bc63c15a984567f6adb64dd1256dfd` |

### 9.2 S1 participant decision record

After both sessions, complete the record for the journey under test.

| Check | Result |
|---|---|
| Journey | S1 |
| Prescribed task set | S1-T1–T8 |
| Two independent participants completed the round | PASS / FAIL |
| Both session records carry matching independence/distinctness attestations | PASS / FAIL |
| Gate F passed with the complete prescribed task set | PASS / FAIL |
| Participant 1 attempted the complete prescribed task set | PASS / FAIL |
| Participant 2 attempted the complete prescribed task set | PASS / FAIL |
| Aggregate prescribed-task coverage across the round | PASS / FAIL |
| S0 back/cancel task S0-T7 attempted | PASS / FAIL / N/A for S1 |
| Unresolved Blockers | count |
| Unresolved Majors | count |
| Owner-accepted Majors with rationale/reference | IDs + ledger rationale/reference / none |
| Second round required | yes / no |
| Required second-round decision record | stable Gate-G record/run ID / N/A |
| Required second round completed and independently passed Gate G | PASS / FAIL / N/A |
| Gate G | PASS / FAIL |

The per-participant rows are binding. `ux-detailed-plan.md` §7.6 requires both S1 participants to
attempt, at minimum, the full listed task set; the same complete-task rule is used for S0. Aggregate
coverage is diagnostic only and cannot compensate for a task skipped by either participant.

Gate G passes only when Gate F had already passed with the complete prescribed task set for that
journey, both independent participants completed the round, both session records carry the required matching independence/distinctness attestation, **each participant separately attempted
every prescribed task**, there is no unresolved Blocker, and every remaining Major has explicit
project-owner acceptance with both a target disposition and rationale preserved in the finding ledger
directly or by precise durable reference. If `Second round required` is `yes`, Gate G remains `FAIL`
until the second participant round is completed and its own Gate-G decision record independently
passes these same rules; `Required second round completed and independently passed Gate G` must be
`PASS`, never `N/A`. If a prescribed task proves untestable during either round, Gate G is `FAIL`;
record the prototype/dependency blocker and return to Gate F rather than excluding that task from
scoring.

---

## 10. Implementation-verification handoff

When Gate I produces an implementation packet, preserve the same task IDs and finding IDs into Gate J.
Verification should cover at minimum:

- hierarchy, full states and transitions;
- public command paths only;
- presentation logic outside gate-invisible `MonoBehaviour`s;
- focus order and keyboard path;
- pseudo-locale and max text scale;
- missing/fallback art;
- supported resolutions/data extremes;
- required Unity-host/cert behavior.

The reviewed S0 implementation handoff is [journey packet §14](ux-s0-pm1-journey.md#14-gate-i--p5b-implementation-handoff).
Its I-Q01–19 Given/When/Then cases are PLANNED, not executed P5b evidence. Preserve
S0-T1–T7, PM-1 substitution coverage and the carried finding IDs in each result; record
PR/head, lane, dimensions, input mode, actual text-scale maximum/locale, observed result and
artifact/log references. I PASS is recorded in journey §14.11. The October 3 owner choices are recorded in journey
§14.10; [binding contracts](ux-s0-binding-contracts.md) §§2–5 now define S0-I-001 identity/fixture/history,
S0-I-002 configuration/reflow and S0-I-003 copy/provider/L2 dependencies against main `3429fafd`.
The mapping and reference deltas passed technical handoff review at e8ffd89 (journey §14.11). Main release follows #478 landing; #470 still needs fresh CI and exact-head pinned compile before merge.
No G/H approval record or walkthrough is superseded by this handoff.

The implementation is not validated by matching a screenshot alone. It must preserve the tested task
semantics and evidence-backed interaction states.

---

### 10.1 Gate-I owner choices recorded October 3

Anton Zymin instructed “Go with your recommended choices” on October 3, 2026 at 15:21:40
America/Los_Angeles (22:21:40 UTC), following the recommendation of real demo squads, 200% text
scaling and the client formatter. Journey §14.10 records the B/C/A decision, rationale,
ownership and outstanding contracts. This is product-direction approval, not a new G/H image
approval, final Gate-I verdict, runtime verification or merge instruction for #470. The existing
§9 approval records and source/image hashes remain unchanged; I-Q01–19 remain planned.

### 10.2 Contract-completion review and planned evidence

**Name-list decision — October 3, 2026:** Anton Zymin replied “Approved” at 21:06:28
America/Los_Angeles (October 4, 04:06:28 UTC) to the explicit request to approve the
36 demo names in binding contracts §2.1 at PR #478 head
`96f16ba9d7f5b87210ab23796d8c8ea5dddcda38`. The table is unchanged. This records
fixture-name acceptance; separate long-name/pseudo/200% stress fixtures and final
contract/reference review were still required at that decision. I was IN PROGRESS; #470 blocked; all I-Q cases
PLANNED. Earlier G/H approvals and their evidence remain unchanged.

Binding contracts §5.1 identifies the affected H inputs: named chooser/feedback identities,
current-player shirts, the fixed scale/reflow contract and the production statistics row/copy.
Those deltas and journey §§14.2–8 passed the October 4 technical handoff review in
journey §14.11 at e8ffd89. No revised H exports are required for this contract handoff;
original G/H approval pins remain unchanged. Actual named labels/reflow/fonts and
identity require Gate-J evidence; this is not additional image approval.

At Gate J, I-Q09/I-Q14 record fixture revision/content hash, engine-selected XI/bench,
current-frame player-id/name/shirt agreement, keeper replacement, skipped/repeated frames,
immutable request labels, fresh-session reset and the former no-squad setup comparison.
I-Q16/I-Q17 record actual 100/150/200% configuration, fonts/glyphs/pseudo text and display
zoom separately, exact role schemas, the fixed invariant S0 provider, content build rejection
and L2 key-fallback results, and validated/package/loaded-content identities. I-Q16 uses
all six flat substitution outcomes with separately reorderable outgoing/incoming first/last
names, shirts, displayed bench and Applied minute; negative build fixtures reject missing
or extra indices and numeric specifiers on name arguments (binding contracts §4.3). These are PLANNED requirements,
not additional executed browser or P5b cases. #49 L2's full ERR-049-005 proof remains its
own landing obligation; client S0 coverage alone cannot close it.

### 10.3 Gate-I review verdict — October 4

**Owner acceptance:** Anton Zymin replied “I accept” on October 4, 2026 at
15:07:45 America/Los_Angeles (22:07:45 UTC), directly answering the request to accept
this Gate-I handoff PASS at PR #478 head
`a1eae5675b3cadd22318b0640bbdd510ff888dab`. This explicitly accepts the technical
handoff verdict below; it supplies no implementation, image, Gate-J, Unity or merge
approval. The technical reviewer remains Codex; the owner acceptance is a separate decision.

Technical handoff PASS at PR #478 head `e8ffd89661df182d33fa3646db3dc73a070d0610`;
journey §14.11 records the eleven-deliverable checklist, source checks, reference
reconciliation and main-landing boundary. S0-I-001/002/003 decisions close for handoff;
all I-Q01–19 remain PLANNED. Run 37176192739 passed ten executed CI jobs, Unity skipped.
This record is not an owner image approval, Unity compile, runtime acceptance or merge
instruction. #49 L2/full ERR-049-005, client consumers and B8/B9b/B10 evidence remain due.

## 11. Current F4 status

| F4 requirement | Status | Evidence / blocker |
|---|---|---|
| Four-layer method made repeatable | READY | §§2, 5, 6, 10 |
| Scripted self-walkthrough defined | READY | §5 covers the complete F4.6 minimum profile set plus Gate-E-only resilience checks, explicit contrast verification, pinned desktop validation cases, auditable run identity, and per-condition result/evidence records |
| S0 task protocol defined | READY | §§3.1, 6, 7 include the required back/cancel task |
| S1 task/evidence protocol defined | READY | §§3.2, 4.1, 7, 9 map the §7.6 S1 outcomes into S1 participant slots, per-participant complete-task records and journey-specific Gate-G evidence |
| Severity/disposition repeatable | READY | §§7.3–9 include the closed §12 disposition vocabulary, owner-acceptance rationale evidence, required-second-round gating, and Gate-G pass rules |
| Evidence capture format defined | READY | §§5, 7 and 9 include Gate-E journey/run identity, the complete `ux-detailed-plan.md` §12 finding-ledger fields, task/state context, acceptance rationale, per-participant independence/distinctness evidence, and Gate-G coverage |
| §10.1 UX-workstream accountable owner assigned | **READY** | §4.0 records Anton Zymin, assigned by the project owner on September 11, 2026 |
| Privacy-safe S0 participant slot 1 defined | **READY** | §4.1 defines anonymous S0-P1, profile, channel and pre-Gate-F attestation field |
| Privacy-safe S0 participant slot 2 defined | **READY** | §4.1 defines anonymous S0-P2, profile, channel and pre-Gate-F attestation field |

**F4 verdict: COMPLETE.** The protocol is operational for S0 and S1, Anton Zymin is accountable, and
the privacy-safe participant mechanism is defined. **S0 Gate A has since completed; Gate B is next.** *(September 28, 2026: S0 Gate B has since completed (owner-confirmed September 28, 2026) — `ux-s0-pm1-journey.md` v0.5 §7; Gate C is next.)*
Neither F4 nor Gate A passes Gate E, F or G. S0 Gate F is complete in the journey packet;
S0 Gate G passed for pinned v0.4 in §9.1 and the owner-approved v0.5 delta in §9.1.1. H v0.1 remains owner-approved at `4a6220c` (§9.1.2). H v0.2 is owner-approved at `a1044d5` (§9.1.3); I PASS uses approved v0.2 and the technical review in journey §14.11. The #470 Gate-I block clears on main only after #478 lands; its refresh, CI and pinned compile remain due. S1 requires its two independent completions.

---

## 12. Version History

| Version | Date | Change |
|---|---|---|
| 0.1 | September 11, 2026 | Created the repeatable F4 validation packet: four-layer method, S0 task set, participant mechanism, Gate-E walkthrough matrix, moderator/evidence templates, severity/disposition rules and Gate-G/Gate-J continuity. F4 remained open on the §10.1 UX owner plus two real participant assignments. |
| 0.2 | September 11, 2026 | Review correction: added the binding F4.6 `no save` and `save/load failure where relevant` profiles; added S0-T7 to exercise Gate F back/cancel behavior; narrowed S0-T1 so current New Game/start limitations are recorded honestly; added this version history and tightened the READY claims to the corrected coverage. |
| 0.3 | September 11, 2026 | Review correction: restored a general `Error/failure state` row to §5. The v0.2 pass had *replaced* the original generic error row with `Save/load failure where relevant` rather than adding alongside it, narrowing coverage against F4.3, which names `disabled/error states` as a condition in its own right and of which a save/load failure is only one class. §5's preamble now states the F4.3 floor explicitly and the closing note makes the non-substitution symmetric. |
| 0.4 | September 11, 2026 | Codex review correction: split color-independent meaning into its own Gate-E condition so it remains binding even when the `Many status indicators` stress profile is N/A; density/scannability is now checked separately. |
| 0.5 | September 11, 2026 | Review corrections through round eight: prescribed tasks can no longer disappear behind an `honestly testable` qualifier — an untestable prescribed task fails Gate F/G and returns the prototype to Gate F; Gate E now has per-condition `PASS`/`FAIL`/justified-`N/A`, prototype-version, evidence and finding/reason fields, with blank rows explicitly preventing a Gate-E pass; each Gate-E execution now has a required stable run ID and journey identity, and S0/S1 require separate run records even on one prototype; Gate E carries standalone contrast verification per `ux-shared-system.md` F3-001 and pins desktop validation to the shared-system 1366-wide / 1920×1080 / 2560-wide cases without declaring a shipping minimum; the finding ledger preserves all `ux-detailed-plan.md` §12 fields plus project-owner acceptance rationale/reference for accepted unresolved Majors; §8 carries §12's closed five-value disposition vocabulary; the S1 participant slots and §7.6 task outcomes are explicitly defined; Gate G now requires the complete prescribed task set from each participant separately rather than aggregate coverage, and any required second round must complete and independently pass the same Gate-G rules before Gate G can pass. |
| 0.6 | September 11, 2026 | Records the project owner's assignment of **Anton Zymin** as the §10.1 UX-workstream accountable owner. The two S0 participant slots remain unassigned because no participant selection was made; F4 therefore remains NOT COMPLETE, Gate G remains unavailable, and no later gate or P5b implementation is released by this assignment. |
| 0.7 | September 11, 2026 | Records the project owner's direction to omit participant names. Replaces public identity fields with stable anonymous slot IDs and privacy-safe availability/independence attestations; closes F4 and advances the workstream to S0 Gate A. Gate G is unchanged: two distinct real independent participants must complete the round, and neither anonymity nor F4 completion bypasses Gate F/G or releases P5b before Gate I. |
| 0.8 | September 11, 2026 | Codex/review correction: makes the already-required independence evidence mechanically recordable. §4.1 now has an explicit per-slot independence/distinctness attestation, §7.1 carries it into each anonymous session record, and §9 requires both session records to match those attestations before Gate G can pass. Also repins the shared interaction baseline to `ux-shared-system.md` v0.4 after that artifact's status-authority correction and clarifies that pre-Gate-F availability is a separate v1.5 timing relaxation, not a consequence of anonymity. |
| 0.9 | September 21, 2026 | Status/pointer reconciliation after S0 Gate A completed in `ux-s0-pm1-journey.md` v0.2. Execution authority advances to `ux-detailed-plan.md` v1.6 and the next UX action is Gate B. Validation mechanics, anonymous participant evidence, pre-Gate-F availability, and the two-independent-participant Gate-G requirement are unchanged. |
| 0.10 | September 21, 2026 | Pointer-only review sync after the Gate-A packet and execution authority advance to `ux-s0-pm1-journey.md` v0.3 / `ux-detailed-plan.md` v1.7. No validation task, participant rule, Gate-E/G evidence requirement, severity rule or pass/fail policy changes. |
| 0.11 | September 21, 2026 | Review closeout pointer sync to `ux-detailed-plan.md` v1.8 / `ux-s0-pm1-journey.md` v0.4. Validation tasks, participant rules, Gate-E/G evidence requirements, severity and pass/fail policy remain unchanged. |
| 0.12 | September 28, 2026 | Pointer/status sync for S0 Gate B (`ux-s0-pm1-journey.md` v0.5 §7, `ux-detailed-plan.md` v1.9). Gate B maps S0-T1–T7 to flow steps without changing any task, participant rule, Gate-E/G evidence requirement, severity or disposition; S0-T2's pre-match choice is now concretely Mentality because Formation has no simulation consumer. |
| 0.13 | September 30, 2026 | Status/pointer sync to detailed plan v1.10 and S0 packet v0.6. C–E complete; F vehicle ready but both pre-F availability/independence/distinctness attestations remain due. Participant cells and G requirements unchanged. |
| 0.14 | September 30, 2026 | Execution pointer sync to detailed plan v1.11; packet v0.7 corrects E evidence and retests v0.2. Tester attestations are formal F prerequisites, not usability findings. Participant cells, tasks and gate rules unchanged. |
| 0.15 | September 30, 2026 | Records the owner-directed S0 image-review route and §9.1 review/approval template. S0 tester slots superseded; F complete, G pending actual image approval. Participant-session templates retained for S1/optional research; image approval supplies no independent usability/runtime evidence. |
| 0.16 | September 30, 2026 | Expands S0 image-review coverage to 18 views and supporting 79-check run. Requires every finding disposition, explicit C-DEC-1/full-time-statistics decisions and written acceptance rationale/release condition for carried Majors; H approval remains separate before I. |
| 0.17 | September 30, 2026 | Updates execution pointer and supporting evidence to the 80-check review-cleanup run. S0-G-008 stays an undecided owner choice; no image approval or Major acceptance inferred. |
| 0.18 | September 30, 2026 | Fills §9.1 with explicit owner confirmation and adopted statement verbatim, review ID/date/full commit, 18 paths/hashes, run 04 and carried-Major reasons/release conditions. G PASS; H OPEN; I/#470 blocked. Documents-only recording preserves the full prototype/evidence tree. |
| 0.19 | September 30, 2026 | Preserves the completed v0.4 approval verbatim and original image hashes; dates its immutable recording boundary. Adds pending v0.5 correction-review record with 85-check run 05 / 19 separate PDFs. No approval inferred from fixing Codex comments. |
| 0.20 | September 30, 2026 | Fixes the full-time fault report-note contradiction with three focused checks and one regenerated image. Narrows pending v0.5 review to that image; carries the other 18 from approved v0.4 apart from version text. Adds disabled Close statistics to H backlog. Original approval and accepted decisions preserved; no new approval inferred. |
| 0.21 | September 30, 2026 | Records actual owner approval of the corrected v0.5 full-time fault image at 0e8bd2b, carrying the other 18 forward from approved v0.4 apart from version text. G PASS; H OPEN; I/#470 blocked on separately approved H images. Documents-only recording preserves reviewed source, PDFs and evidence. |
| 0.22 | September 30, 2026 | Adds pending separate H v0.1 review record: 23 image pairs, 90 scripted checks and carried finding/identity rechecks. G approvals preserved; H not passed, I/#470 blocked. |
| 0.23 | September 30, 2026 | Records actual “images approved” confirmation for H v0.1 at 4a6220c, full 46-image/seven-source hashes and unchanged evidence. H PASS; I OPEN for handoff; #470 still blocked on I. |
| 0.24 | October 1, 2026 | Preserves the actual v0.1 owner approval/pins in §9.1.2 and adds §9.1.3 for corrected v0.2: 99 checks, 23 image pairs, isolated exports and PDF completeness checks. H reopened for v0.2 review; I on hold, #470 still blocked. Prior history rows unchanged. |
| 0.25 | October 1, 2026 | Corrects the header and execution-plan pointer; distinguishes pending v0.2 adoption from the existing H PASS / I OPEN for pinned v0.1. Earlier reopening/hold wording was an author interpretation, not an owner decision. Prior approval and history preserved. |
| 0.26 | October 1, 2026 | Records actual “images approved” confirmation for H v0.2 at a1044d5, 46 image/seven source hashes and immutable walkthrough hash. H PASS; I OPEN using v0.2. Prior approvals/history/evidence unchanged; no merge/runtime approval inferred. |
| 0.27 | October 1, 2026 | Records S0-H-014 focused source/verification delta: complete long-name identities pseudo-localized; coverage now includes fault and long-name fixtures. Focused check and full 99-check scratch run pass. Approved sources stay pinned to a1044d5; images/walkthrough/approval records unchanged. |
| 0.28 | October 1, 2026 | §10 links the S0 Gate-I draft and its 19 planned P5b QA cases, run/head/lane/context evidence fields and carried task/finding IDs. I IN PROGRESS with identity/scale blockers; no P5b test execution or I PASS inferred. Execution-authority pointer updated; all actual G/H approvals/evidence preserved. |
| 0.29 | October 1, 2026 | PR #476 review correction: §10 is a link/evidence handoff, while the draft lives in journey §14. Names S0-I-003 dynamic-formatting allocation alongside identity/scale blockers; execution pointer updated. No approval/evidence or P5b execution claim changes. |
| 0.30 | October 3, 2026 | Adds §10.1 actual B/C/A owner choice record and distinguishes settled directions from remaining I contracts and J evidence. Current execution pointer advanced; original §9 approvals/hashes unchanged. |
| 0.31 | October 3, 2026 | Adds the S0 binding-contract review and evidence checklist; I-Q01–19 stay planned, identity/scale/copy/package proofs remain Gate J obligations. Actual owner approval records and fingerprints unchanged; I IN PROGRESS, #470 blocked. |
| 0.32 | October 3, 2026 | PR #478 review sync: planned I-Q16 fixtures use the flat substitution argument schema and fixed invariant S0 provider; L2 missing-key fallback remains separate. Original approvals/fingerprints unchanged; I IN PROGRESS, #470 blocked. |
| 0.33 | October 3, 2026 | Records the explicit 36-name approval at 96f16ba, synchronized with binding contracts v0.3 and journey v0.31. Stress coverage and final I review remain due; #470 blocked, QA planned; prior G/H approval records and evidence unchanged. |
| 0.34 | October 4, 2026 | Records technical Gate-I handoff PASS at e8ffd89 and links the deliverable/reference review in journey §14.11. Main release follows #478 landing; QA, runtime/Unity and client dependencies remain due. Existing §9 approval records/fingerprints unchanged. |
| 0.35 | October 4, 2026 | Mirrors explicit owner acceptance of the Gate-I handoff PASS at a1eae56 in journey §14.11. Technical review and owner decision remain distinct; prior approval/evidence records and planned QA unchanged. |
| 0.36 | October 5, 2026 | Execution pointer v1.29 and #470 foundation merge/next consumed implementation recorded; validation rules, approval pins and all planned I-Q evidence unchanged. |

**October 5, 2026 — implementation sequencing pointer:** #470 foundation merged
at f276f700 after green PR CI and recorded pinned-host compile/ClientApp evidence.
The [P5b lifecycle/identity plan](../tracking/p5b-lifecycle-identity-plan.md) v0.1
allocates the next consumed implementation. This is no additional UX gate pass;
all I-Q01–19 remain PLANNED, and actual complete client/host evidence remains due.
