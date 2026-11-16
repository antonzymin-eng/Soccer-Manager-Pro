# #434 formation and Duty — approved implementation contract

**Created:** November 9, 2026. **Updated:** November 10, 2026. **Version:** 1.6.
**Purpose:** Preserve the approved kickoff, tactic, save and comparison decisions.
**Status:** Runtime unchanged; review follow-up adds per-team reporting and allocation coverage. Trace diagnosis HOLD for Opus; owner-run pinned Unity passed at `75d62186` (0 failed).
**Analyzed base:** `d4b026aa951432d342217314b8c4d49b53a19e7d`.
**Implementation/evidence:** [retained package](evidence/formation-duty-20261109/README.md).

## 1. Authority, scope and decisions

The owner said “I approved scope reduction. Begin implementation”, then answered
“Yes” to the v1.2 recommendations in this session on November 9, 2026. This is an
agent-attested session record, not an owner-authored GitHub comment or committed
transcript. It approves the kickoff-only scope, all P1–P4 choices below, the
120-match fallback ceiling, no numeric plausibility bands and the slot limitation.

| Decision | Accepted behavior |
| --- | --- |
| P1 | Implicit first-`RunTick` lock at clock 0, before advancing physics frame 1. |
| P2 | Option (b): accept the complete requested tactic at normal strides; keep the effective kickoff formation separately. |
| P3 | Option B: v29 metadata before dependent state, contiguous count-prefixed rotation blocks; one-off exception to the snapshot procedure's append-last layout rule. |
| P4 | Exact structural C0 comparison, C0–C9 matrix, conditional base reuse, 120-match fallback if eligibility fails, no numeric football acceptance bands. |

This revises D-05's raw-digest condition for this landing only; the historical B→C
window stays closed. FR-PA-039 remains unchanged. Runtime formation switching,
formation-aware lineup selection, ScoreDiff, ActiveOutfieldCount, PlayerRole/
PositioningFreedom cleanup and tuning stay outside this landing.

## 2. Source inventory and routing decisions

The inventory refers to the analyzed base, not shifted implementation line numbers.
`rg -n STAGE0_FORMATION src/ --glob '*.cs'` has eight production read expressions;
the constant declaration is separate.

| Base location | Current purpose | Proposed decision |
| --- | --- | --- |
| `MatchEngine.cs:1046` | Construct and seed each `PositioningAITick` using F442. | Retain F442 as the provisional constructor seed. At P1 initialize the existing object from the selected kickoff family; if it is F442, take an exact no-op path. Constructor timing precedes caller setup and cannot select the final tactic. |
| `MatchEngine.cs:1390` | Reconstruct the base lineup from a roster on restore. | Keep `STAGE0_FORMATION`. Do not select different player identities from a subsequently requested tactic. |
| `MatchEngine.cs:2011` and `:2012` | Select each squad's starters/bench during `ConfigureSquads`. | Keep `STAGE0_FORMATION`, for both teams. Formation-dependent lineup selection is a separate identity/restore change. |
| `MatchEngine.cs:4428` | Choose positional roles when filling the #12 snapshot. | Use the effective kickoff formation, not the active tactic's potentially different requested formation. |
| `SquadRating.cs:76`, `StartingElevenMean` | Rate the selected starting eleven; used by season/quick-sim callers. | Keep `STAGE0_FORMATION` so the rating describes the same F442-selected eleven. |
| `SquadRating.cs:105`, `CanFieldStartingEleven` | Check lineup feasibility for availability composition and transfer/season callers. | Keep `STAGE0_FORMATION`; changing only this probe would disagree with the lineup the engine selects. |
| `SquadRating.cs:192`, `StartingElevenPlayerIds` | Project selected starter identities for availability and appearance/discipline accounting. | Keep `STAGE0_FORMATION`; do not silently change the identity set while kickoff/restore selection stays fixed. |

The new consumer-owned `PositioningAI.TacticTranslation` explicitly maps all three
#21 formation values to #12 families. Matching enum ordinals were never an existing
conversion method. Unknown live inputs fall back to F442 under FR-PA-043/F2;
unknown serialized effective families are corruption and reject before conversion.
Pressing/Attacking retain views over the same positioning instance; Defensive reads
its live output through snapshot inputs. The object is never replaced.

The existing roster-local index seeds the selected catalogue row; #25's controller
alone subsequently changes bindings. Lineup selection and all three SquadRating
reads remain F442. The owner expressly accepted this full mapping limitation:

| Index | F442-selected slot occupant | F433 destination | F4231 destination |
| --- | --- | --- | --- |
| 0 | GK | GK | GK |
| 1 | LB | LB | LB |
| 2 | CB1 | CB1 | CB1 |
| 3 | CB2 | CB2 | CB2 |
| 4 | RB | RB | RB |
| 5 | LM | DM | DM1 |
| 6 | CM1 | CM1 | DM2 |
| 7 | CM2 | CM2 | LAM |
| 8 | RM | LW | CAM |
| 9 | ST1 | ST | RAM |
| 10 | ST2 | RW | ST |

These are catalogue slot labels, not verified specialist proficiency. LM at DM/DM1,
RM at LW/CAM and a striker at RAM materially change responsibilities. Real-squad
fixtures prove unchanged selected IDs; neutral-squad characterization cannot prove
specialist suitability. Formation-aware selection remains a separate follow-up.

## 3. Kickoff boundary and caller behavior

`MatchSession.BootEngine` configures squads/managers, then `ApplyKickoff` stages its
final choice at clock 0. The first public `RunTick` selects/seeds that pending family
before the orchestrator advances. Complete pending tactics still commit first at
AI frame 6 and then each stride. No constructor, restart, half-time or restore
selects a later requested formation. All supported rotation storage is preallocated;
F442 initialization is an exact no-op. The explicit MatchSession lock plus direct-
engine fallback was considered; P1 selects one guarded implicit boundary instead.

Each completed snapshot records initialized/effective values, including frames 1–5.
A synthetic tick-0 body may be uninitialized with provisional F442 and pending setup;
false initialization at a later tick rejects. Initialized restores never reselect.
Restore selects the family before saved hysteresis, bindings, cache and rows load.

A mixed request such as F433 + Attacking in an F4231 match applies Attacking and all
other non-formation fields at the usual stride; effective positioning remains F4231.
The engine/live frame/HTTP copy both requested and effective choices. The S0 client
currently offers mentality, not formation selection, and continues to copy the
requested tactic. No promise of next-match persistence or applied runtime formation
switch is made. A typed refusal would require a wider API/manager change and was
not selected.

## 4. P3: accepted v29 encoding

The outer match-save format remains 3 and header schema 1. The world-state schema
word is read first; v28 rejects before any layout-dependent read. After schema and
tick, write two team-ordered `(initialized byte, effective #21 int32)` records.
Before each full rotation block write its int32 row count, then the existing
11-agent binding, 11 composed target pairs and all contiguous row states in catalogue
order (dwell int32 / rotated byte / hold int32). Counts are 5/5/6 for F442/F433/F4231.

Default overhead is 18 bytes (10 metadata + 8 count prefixes), or 27/36 with one/two
F4231 teams. Writer/reader order mirrors line for line. Validate enums/booleans,
family/count agreement, division-based remaining-byte bounds, finite targets,
permutation and row invariants; preserve final ledger boundary checks. Load saved
state verbatim, including active and pending row 5, even when rotation is Off.
Do not default corrupt saves, migrate v28 or require requested/effective equality.

The owner explicitly accepted this one-off exception to [snapshot-schema-bump,
Step 2](../../.claude/skills/snapshot-schema-bump/SKILL.md): “Append the new block
**last** so no existing offset moves.” The rejected split-row alternative preserved
o compatibility because v28 rejects either way; it added permanent writer/reader
asymmetry merely to simplify a one-time comparison. All other procedure safeguards
remain in force.

## 5. P4: identity and non-default characterization

The frozen [protocol](evidence/formation-duty-20261109/PROTOCOL.md), as-run tools and
hashes define the capture. On all six fixed seeds, C0 must match every common world
byte, full BallState, all player trajectories, possession/team-pass-flight witness,
AI geometry/phases and complete canonical ordered event ledger, frame by frame.
No floating tolerance, broad block mask or rounded JSON comparison is allowed.

Only the literal schema word, ten validated F442 initialization bytes and two
validated five-row count words differ in the default payload. Structural span
coverage accounts for every other byte exactly once. Offsets/length change by
exactly 18. Raw current/previous chain digests necessarily differ; independently
reconstruct and validate each arm's unmodified chain. Keep nonempty arm-specific
BuildHash and normal restore gates. Mutating every byte of a real event-containing
payload must change common output or reject allowed metadata, in addition to
synthetic perturbations. Any unexplained covered-byte difference is a hard stop.

| Case | Home formation | Away formation | Home outfield Duty | Away outfield Duty | Purpose |
| --- | --- | --- | --- | --- | --- |
| C0 | F442 | F442 | Support | Support | Exact default identity control. |
| C1 | F433 | F442 | Support | Support | Home formation in isolation. |
| C2 | F442 | F433 | Support | Support | Away-side counterpart. |
| C3 | F4231 | F442 | Support | Support | Home six-row formation. |
| C4 | F442 | F4231 | Support | Support | Away six-row formation. |
| C5 | F442 | F442 | Defend | Support | Home Duty change in isolation. |
| C6 | F442 | F442 | Support | Defend | Away-side counterpart. |
| C7 | F442 | F442 | Attack | Support | Home opposite Duty direction. |
| C8 | F442 | F442 | Support | Attack | Away-side counterpart. |
| C9 | F4231 | F4231 | Support | Support | Both teams non-default; exercise both sixth rows and the 36-byte format expansion in a full run. |

Duty entries apply to all named side's outfield players; GKs and the other side use
Support. These one-sided mirrored contrasts avoid cancelling both teams' shifts.
The full non-default-formation/non-Support interaction remains unit-test coverage,
not another characterization case.

Base reuse is confined to the direct-engine Human lane: no file binding, clients or
AI adaptation. Whole-tactic comparisons do exist in file/client code, so reuse does
not generalize to those callers. A complete pinned source audit plus 36 × 1000-frame
probes permits reusing base C0 behavior for C1–C4/C9; validate and ignore only those
base active/pending requested-formation words. All other probe bytes/trajectories/
ledger/team witnesses must match. Short probes prove 1000 frames only; full reuse
also depends on source nonconsumption. Reused references are labeled, never invented
full non-default base captures. Duty cases C5–C8 always have their own base runs.

With eligible reuse: 90 unique primary matches (29,160,000 calls), plus 36,000 probe
calls. Twelve additional full plain C0 runs prove observation neutrality; their
observed partners are shared with the primary 90. Historical reproduction adds
12 separate full matches. The accepted fallback ceiling is 120 primary matches
if reuse fails; no fallback was needed.

NaN, invalid attribution/state, routing/restore/encoding failures, comparator gaps,
default drift, existing #12 output-bound violations or unmodified gate failures stop
acceptance. Football metrics have no approved numeric acceptance bands. Report every
seed/team and paired delta as descriptive characterization, including unfavorable
results, without tuning. No plausibility or Unity certification follows from a
Linux pass.

## 6. Spec amendments and Duty pipeline

Runtime/spec amendments landed together in implementation commit `23f2fedb`:
ERR-012-013 (#12 formation translation, fixed kickoff identity and Duty pipeline),
ERR-021-009 (#21 requested/effective and mixed-request semantics), ERR-025-001 (#25
v29 catalogue/count encoding). #16 framing is unchanged. Approved spec text and
version histories were back-propagated in the same commit; no gameplay tables or
[GT] values changed.

For each active outfield agent, add existing DutyForeOffsetM to its canonical anchor
X before ball-relative/context/overlay/spacing/clamp stages. Support performs no
addition. At anchor X=52.5, Defend/Support/Attack produce 49.5/52.5/55.5 before later
composition; away world X is 55.5/52.5/49.5. Final displacement need not be exactly
3 m after existing composition. Duty belongs to the player through rotations, not
to a destination slot. GK composition, PositioningFreedom and PlayerRole remain.

## 7. Validation and handoff

See the evidence package for exact gates, all attempts, fixtures, results and the
pinned Unity request. The owner-run Unity 6000.4.9f1 receipt at `75d62186` is in
[`host-formation-duty-75d62186-20261110`](evidence/host-formation-duty-75d62186-20261110/README.md): 0 `error CS`; unfiltered EditMode for the four
requested assemblies 1,394 passed / 0 failed / 29 skipped / 1 existing inconclusive;
`MatchEngineFormationDutyTests` 42/42 with the native allocation oracle and its positive control. No test is newly skipped/quarantined/disabled. New
fixture coverage includes first-frame/pre-first-AI and synthetic tick-0 restore,
half-time preservation, mixed commands, retained consumer views, every formation
with active/pending rotations, non-Support replay, real selected IDs, invalid live
mapping versus corrupt-save rejection and exact Support bits. Two restart fixture
clock jumps now initialize kickoff normally; assertions remain unchanged.

Both PRs remain draft and unmerged. #440 Stage A is an independent main-based
observation investigation; no #440 gameplay fix is authorized by these decisions.

## Version history

| Version | Date | Notes |
| --- | --- | --- |
| 1.0–1.2 | November 9, 2026 | Reviewed preflight, alternatives and CI receipts; immutable full text remains in [commit e9caef02](https://github.com/antonzymin-eng/Soccer-Manager-Pro/blob/e9caef02/docs/tracking/w-434-formation-duty-preflight.md). |
| 1.3 | November 9, 2026 | Records direct owner approval of P1–P4, fallback, no bands and slot limitation. |
| 1.4 | November 10, 2026 | Closeout condenses superseded process history into the accepted contract and points to retained implementation evidence. |
| 1.5 | November 10, 2026 | Owner-directed per-team reporting/control blocker, raw-request documentation and scoped allocation coverage; trace diagnosis held for Opus and host handoff assigned to owner. No gameplay changes. |
| 1.6 | November 10, 2026 | Records the owner-run pinned Unity receipt at `75d62186`: clean full-import compile, four-assembly EditMode with 0 failures, formation fixture 42/42. Diagnosis hold and player-control blocker unchanged. |
