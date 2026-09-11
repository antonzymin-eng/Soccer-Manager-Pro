# System XI UX Validation Protocol

**Created:** September 11, 2026  
**Last Updated:** September 11, 2026  
**Version:** 0.4  
**Status:** F4 PROTOCOL AUTHORED — UX owner and participant assignments still open  
**Execution authority:** [`ux-detailed-plan.md`](ux-detailed-plan.md) v1.4 §F4 and Gates E–G  
**Shared interaction baseline:** [`ux-shared-system.md`](ux-shared-system.md) v0.3  
**Evidence baseline:** [`ux-baseline-evidence.md`](ux-baseline-evidence.md) v0.3

---

## 1. Purpose and authority

This file is the repeatable operating packet for the validation method defined by
`ux-detailed-plan.md` F4. It does not redefine the F4 rules, Gates E–G, severity scale or
pass/fail policy. Where this packet and the detailed plan disagree, the detailed plan wins and this
file is the defect.

The packet exists so a person other than the UX author can run the same S0/S1 validation process and
produce comparable evidence rather than an informal design review.

F4 is **not complete** merely because this document exists. Project tracking still records the
`ux-detailed-plan.md` §10.1 UX-workstream accountable owner as unassigned, and the detailed plan
requires two independent participants to be identified/recruitable for S0. The owner slot and the
recruitment slots in §4 therefore remain open until real assignments are recorded.

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

## 3. S0 validation target

S0 is the PM-1 journey:

`launch → Main Menu → Tactics Setup → Match View → Post-Match Report → Main Menu`

The current product graph is authoritative for navigation semantics. The prototype may be non-Unity,
but it must distinguish current behavior from future behavior and may not invent a domain command to
make a task testable.

### 3.1 S0 participant tasks

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

If a task depends on a capability still marked `FUTURE-BLOCKED`, record the dependency and omit that
task from completion scoring until the prototype provides an honest simulated representation of the
specified future surface. Do not silently convert it to `LIVE`.

### 3.2 Critical observations

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
prototypes, findings and handoffs. Project tracking still records the person filling that role as
unassigned. F4 does not invent an identity.

| Role | Accountable scope | Assignee | Status |
|---|---|---|---|
| UX workstream | UX artifacts, flows, prototypes, findings and implementation handoffs | **TBD — real accountable assignee required** | OPEN |

The assignee may perform work directly or delegate it, but the role must have one clear accountable
owner before F4 is marked complete. This assignment does not transfer the project owner's decision
rights or the domain/client owners' authority defined in §10.1.

### 4.1 Recruitment slots

Gate G for S0 requires exactly the binding floor from the detailed plan: two independent participants
in one formative round; the designer/implementer does not count.

| Slot | Preferred profile | Recruiting channel | Candidate | Availability | Status |
|---|---|---|---|---|---|
| S0-P1 | Experienced football/management-sim player | Owner/team personal or relevant community network | **TBD — real person required** | TBD | OPEN |
| S0-P2 | Football-literate newcomer to management sims, where practical | Owner/team personal or relevant community network | **TBD — real person required** | TBD | OPEN |

Paid recruitment is optional. The important properties are independence from authorship and enough
football/product context to attempt the task without being coached through the UI.

**F4 exit blockers:** the UX-workstream assignee in §4.0 and both participant candidate cells in §4.1
must identify real people (or concrete pre-agreed participants for the Gate-G slots) before F4 can be
marked complete. If either participant becomes unavailable, Gate G does not pass; there is no
provisional bypass to Gate H/I.

### 4.2 Scheduling rule

The participant round is booked for the first practical session after the S0 prototype passes Gate F.
One round is the default cap. A second round is required only when a Blocker/Major causes a material
flow redesign or the project owner explicitly requests another pass.

---

## 5. Gate-E scripted self-walkthrough

Run the complete S0 task set once per relevant condition below before independent testing. Combine
conditions when that does not hide the failure mode; split them when interaction would make the cause
ambiguous. The table explicitly covers every minimum F4.6 test-data profile and every condition named
by F4.3; rows beyond that floor are Gate-E-only resilience checks and are marked as such.

| Condition | Required check |
|---|---|
| Ordinary case | Complete all S0 tasks with representative real-match data. |
| Color-independent meaning *(Gate-E requirement)* | Across ordinary applicable S0 states, score/result, availability, selection, warning/error and action-state meaning remains understandable without relying on hue alone; use text, iconography, shape, pattern, position or another non-color cue where meaning would otherwise be color-only. This check is independent of indicator density. |
| Long player/club/competition names | No clipped critical identity, control label or result; long football identities remain distinguishable. |
| Empty/large lists | S1/shared list primitives preserve empty explanation and usable selection/sort behavior. |
| Many status indicators | Dense status presentation remains scannable and priority/order does not collapse; the separate color-independent row above still applies. |
| Pseudo-locale | Expanded/localized strings reflow without hiding critical actions or state. |
| Alternate date/currency formatting | Relevant S1/management surfaces format without hard-coded width assumptions. |
| No save | Surfaces do not imply a usable save/resume capability when no save exists for the tested state; unavailable behavior is explicit. |
| Save/load failure where relevant | A failed save/load never masquerades as success; recovery/next safe action is clear. |
| No match frame yet | Match View does not falsely present the match as live/ended; waiting/initial state is intelligible. |
| Disabled action with reason | The reason is available when it affects the player's next decision. |
| Error/failure state (general) | F4.3 names `disabled/error states`, of which the two save rows above are only one class. Any reachable failure — a rejected or refused action, a load/start that cannot proceed, an unavailable dependency — must not masquerade as success, and the next safe action must be clear. |
| Missing art | Fallback preserves identity/layout and does not create a blank critical region. |
| Event-heavy match | HUD/stat attention remains usable under dense events. |
| Unusual scoreline | Score/result hierarchy survives wider values. |
| Full-time/frozen state | Tactical input is unavailable; save/report behavior is not confused with a still-live match. |
| Smallest supported desktop | No critical action or information is pushed irretrievably off-screen. |
| 1920×1080 reference | Intended hierarchy/density matches the reference composition. |
| High-resolution/ultrawide behavior | Expansion does not produce unusable line lengths, extreme separation or floating controls. |
| Max supported text scale | Critical path remains operable; focus/control relationships remain clear. |
| Keyboard only | Every critical action, including S0-T7 back/cancel, is reachable in coherent order; no focus trap; state is visible without hover. |
| Mouse only | Complete path, including S0-T7 back/cancel, without keyboard-only dependency. |
| Audio muted/caption path *(Gate-E only)* | No required S0 information depends on sound alone. Named by Gate E, not by the F4.6 minimum set; run it where the prototype has any audio/caption surface at all. |

For S0 conditions that do not apply to a given prototype, record `N/A` with a reason rather than
silently skipping the row. The color-independent-meaning row is not discharged by marking `Many status
indicators` N/A: wherever the prototype conveys semantic state or action meaning with color, the
standalone check applies even in an ordinary low-density interface.

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

Record the stop reason as evidence; do not count it as participant error.

---

## 7. Evidence record

Create one record per participant and one consolidated finding table per tested prototype version.

### 7.1 Session header

| Field | Value |
|---|---|
| Journey | S0 / S1 |
| Prototype/version | |
| Date | |
| Participant slot | S0-P1 / S0-P2 / S1 equivalent |
| Participant profile | |
| Management-sim familiarity | |
| Football familiarity | |
| Input method used | |
| Display/layout | |
| Text scale / locale condition | |
| Moderator | |

### 7.2 Task evidence

| Task | Complete without intervention? | First action | Wrong turns / hesitation | Missed info | Misunderstood state/control | Confidence about next step | Notes |
|---|---|---|---|---|---|---|---|
| S0-T1 | | | | | | | |
| S0-T2 | | | | | | | |
| S0-T3 | | | | | | | |
| S0-T4 | | | | | | | |
| S0-T5 | | | | | | | |
| S0-T6 | | | | | | | |
| S0-T7 | | | | | | | |

### 7.3 Finding ledger

| ID | Evidence | Affected task/state | Severity | Disposition | Owner | Retest needed? | Status |
|---|---|---|---|---|---|---|---|
| UX-S0-001 | | | | | | | |

Use stable IDs when a finding survives revisions so evidence and retest results remain traceable.

---

## 8. Severity and disposition

The definitions below operationalize, but do not replace, `ux-detailed-plan.md` F4.4–F4.5.

- **Blocker** — critical task cannot be completed; critical path is inaccessible; destructive
  ambiguity exists; or false capability is presented as live.
- **Major** — repeated critical-information miss; frequent navigation failure; common action is
  misunderstood; severe repeated friction; or material localization/accessibility failure.
- **Moderate** — meaningful confusion/inefficiency with a workable path.
- **Minor** — polish, microcopy or low-impact consistency issue.

Disposition rules:

- Gate G cannot pass with any unresolved Blocker.
- Gate G cannot pass with an unresolved Major unless the project owner explicitly accepts it with a
  rationale and target disposition.
- Moderate may be accepted only when documented and non-compounding.
- Minor enters backlog unless it is cheap and safe to fix in the current revision.
- A Blocker/Major that materially changes the flow triggers a second participant round after revision.

Do not downgrade severity merely because a participant eventually found a workaround.

---

## 9. Gate-G decision record

After both sessions, record:

| Check | Result |
|---|---|
| Two independent participants completed the round | PASS / FAIL |
| All prescribed tasks attempted where honestly testable | PASS / FAIL |
| Back/cancel task S0-T7 attempted | PASS / FAIL |
| Unresolved Blockers | count |
| Unresolved Majors | count |
| Owner-accepted Majors with rationale | IDs / none |
| Second round required | yes / no |
| Gate G | PASS / FAIL |

Gate G passes only when both independent participants completed the round, all honestly testable
prescribed tasks including back/cancel were attempted, there is no unresolved Blocker, and every
remaining Major has explicit owner acceptance with rationale.

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
| Scripted self-walkthrough defined | READY | §5 covers the complete F4.6 minimum profile set plus Gate-E-only resilience checks |
| S0 task protocol defined | READY | §§3, 6, 7 include the required back/cancel task |
| Severity/disposition repeatable | READY | §§8–9 |
| Evidence capture format defined | READY | §7 |
| §10.1 UX-workstream accountable owner assigned | **OPEN** | §4.0 assignee is TBD |
| S0 participant 1 identified/recruitable | **OPEN** | §4.1 candidate is TBD |
| S0 participant 2 identified/recruitable | **OPEN** | §4.1 candidate is TBD |

**F4 verdict: NOT COMPLETE.** The protocol is operational. Three human-assignment facts remain open:
assign the UX-workstream accountable owner and identify/recruit S0-P1 and S0-P2. No UX or client
implementation work should claim Gate G is available until the participant rows are closed, and the
workstream should not claim F4 complete until all three assignments are explicit.

---

## 12. Version History

| Version | Date | Change |
|---|---|---|
| 0.1 | September 11, 2026 | Created the repeatable F4 validation packet: four-layer method, S0 task set, participant mechanism, Gate-E walkthrough matrix, moderator/evidence templates, severity/disposition rules and Gate-G/Gate-J continuity. F4 remained open on the §10.1 UX owner plus two real participant assignments. |
| 0.4 | September 11, 2026 | Codex review correction: split color-independent meaning into its own Gate-E condition so it remains binding even when the `Many status indicators` stress profile is N/A; density/scannability is now checked separately. |
| 0.3 | September 11, 2026 | Review correction: restored a general `Error/failure state` row to §5. The v0.2 pass had *replaced* the original generic error row with `Save/load failure where relevant` rather than adding alongside it, narrowing coverage against F4.3, which names `disabled/error states` as a condition in its own right and of which a save/load failure is only one class. §5's preamble now states the F4.3 floor explicitly and the closing note makes the non-substitution symmetric. |
| 0.2 | September 11, 2026 | Review correction: added the binding F4.6 `no save` and `save/load failure where relevant` profiles; added S0-T7 to exercise Gate F back/cancel behavior; narrowed S0-T1 so current New Game/start limitations are recorded honestly; added this version history and tightened the READY claims to the corrected coverage. |