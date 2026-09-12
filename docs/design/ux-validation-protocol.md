# System XI UX Validation Protocol

**Created:** September 11, 2026  
**Last Updated:** September 11, 2026  
**Version:** 0.8\
**Status:** F4 COMPLETE — anonymous participant slots defined; S0 Gate A next\
**Execution authority:** [`ux-detailed-plan.md`](ux-detailed-plan.md) v1.5 §F4 and Gates E–G\
**Shared interaction baseline:** [`ux-shared-system.md`](ux-shared-system.md) v0.4  
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
stores participant names or contact details. The UX owner attests that two distinct real people are
available for the applicable anonymous slots before a journey reaches Gate F. Gate G still requires
both people to complete the round; anonymity is not a waiver or provisional bypass.

---

## 2. Four-layer validation sequence

Run the layers in this order for each journey:

1. **Contract review** — verify that every displayed datum and action is backed by a current owner or
   explicitly marked `FUTURE-BLOCKED`.
2. **Scripted heuristic/self-walkthrough** — run the complete task against the resilience matrix in
   §5 before showing the prototype to a participant.
3. **Independent task-based participant test** — run the Gate-G round using the neutral moderator
   script in §6. The designer/implementer does not count as a participant.
4. **Implementation verification** — after implementation, verify the real client against the
   validated handoff and host/cert requirements at Gate J.

A later layer never repairs a skipped earlier layer. In particular, self-walkthrough evidence never
substitutes for the two-participant Gate-G round.

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

`ux-detailed-plan.md` F4.2 requires two independent participants for both S0 and S1; the
designer/implementer does not count. Personal names and contact details are intentionally excluded
from repository evidence. Each stable slot instead records the preferred profile, recruiting channel,
a privacy-safe owner availability attestation and an explicit independence/distinctness attestation before its journey reaches Gate F.

| Slot | Journey | Preferred profile | Recruiting channel | Repository identity | Availability attestation | Independence/distinctness attestation | Status |
|---|---|---|---|---|---|---|---|
| S0-P1 | S0 | Experienced football/management-sim player | Owner/team personal or relevant community network | **S0-P1 — anonymous; name omitted** | Due before S0 Gate F | Due before S0 Gate F | READY — slot defined |
| S0-P2 | S0 | Football-literate newcomer to management sims, where practical | Owner/team personal or relevant community network | **S0-P2 — anonymous; name omitted** | Due before S0 Gate F | Due before S0 Gate F | READY — slot defined |
| S1-P1 | S1 | Experienced football/management-sim player | Owner/team personal or relevant community network | **S1-P1 — anonymous; name omitted** | Due before S1 Gate F | Due before S1 Gate F | READY / FUTURE S1 |
| S1-P2 | S1 | Football-literate newcomer to management sims, where practical | Owner/team personal or relevant community network | **S1-P2 — anonymous; name omitted** | Due before S1 Gate F | Due before S1 Gate F | READY / FUTURE S1 |

Paid recruitment is optional. The important properties are independence from authorship and enough
football/product context to attempt the task without being coached through the UI. Do not add names,
handles, email addresses or other identifying details to this packet. The slot ID, profile,
independence attestation and owner availability attestation are the repository record.

**F4 exit:** closed under `ux-detailed-plan.md` v1.5's explicit timing relaxation. §4.0 assigns the accountable UX owner and §4.1 defines the two anonymous S0 slots,
profiles, recruiting channels and attestation mechanism. Anonymity alone does not move the availability check; v1.5 separately moves it from F4 exit to pre-Gate-F. Before S0 reaches Gate F, Anton Zymin records
`AVAILABLE — owner attested <date>` in both availability cells and `INDEPENDENT/DISTINCT — owner attested <date>` in both independence/distinctness cells without naming either person. The latter attests that each slot is a real person independent of the UX author/designer and distinct from the other slot. If either participant is unavailable or either independence/distinctness attestation is missing, Gate F does not open and Gate G cannot pass; there is no provisional
bypass to Gate H/I.

### 4.2 Scheduling rule

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

## 6. Participant session script

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

Create one record per participant and one consolidated finding table per tested prototype version.

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

After both sessions, complete the record for the journey under test.

| Check | Result |
|---|---|
| Journey | S0 / S1 |
| Prescribed task set | S0-T1–T7 / S1-T1–T8 |
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

The implementation is not validated by matching a screenshot alone. It must preserve the tested task
semantics and evidence-backed interaction states.

---

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
the privacy-safe participant mechanism is defined. **S0 Gate A is next.** F4 completion does not pass
Gate E, F or G: both S0 availability attestations are still due before Gate F, and Gate G remains
unavailable until two distinct independent participants complete the entire round.

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
