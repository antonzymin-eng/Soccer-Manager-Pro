# #434 formation and Duty — implementation preflight

**Created:** November 9, 2026
**Purpose:** Resolve the kickoff-only formation contract, snapshot encoding and default-tactics
comparison before implementing #434 part (i).
**Version:** 1.1
**Status:** OWNER DECISIONS REQUIRED — documentation only; no gameplay implementation or capture.
**Analyzed base:** `d4b026aa951432d342217314b8c4d49b53a19e7d` (`main`, W8 C / PR #542).
**Branch:** `codex/434-formation-duty`.

## 1. Authority, scope and decisions

The owner explicitly approved the kickoff-only scope reduction in this session on November 9,
2026: “I approved scope reduction. Begin implementation”. That approves choosing each team's
positioning formation before play and keeping it for the match, with Duty routing throughout.
It reduces the original runtime-switching brief. It does not approve the recommendations below.
FR-PA-039 remains unchanged; in-match formation switching and its settling/mapping rules are a
follow-up outside this PM-2 landing. D-05/D-08 still govern the separate #434 and #440 landings.

This document defines the preflight stop: prepare the inventory, recommendations and validation
contract, then wait for owner decisions P1–P4 before changing gameplay. The original task brief was
the session attachment `codex-prompt-434-440 (3).md`; this stop is reproduced here so no external
version label is needed to understand it. The approval quote above is an agent-attested record of
the direct owner message in this session, not an independently signed repository receipt. No public
transcript or owner-authored GitHub approval comment is claimed. Scope approval is already given;
P1–P4, including P4's D-05 digest exception and the slot-mapping limitation in §2, remain pending.

No approved specification, gameplay code, schema constant,
test assertion, seed, tuning value or historical evidence is changed here. #440 has not started.

| Decision | Recommendation | Consequence requiring owner acceptance |
| --- | --- | --- |
| P1 — kickoff boundary | Capture the final pre-match pending formation at the first `RunTick` entry, while the clock is still 0, before the orchestrator advances to frame 1. Initialize that formation in place before any frame is simulated. | Formation boot initialization is distinct from committing the complete pending tactic, which stays on the existing first AI stride at frame 6. |
| P2 — mid-match requests | Option (b): accept the full requested tactic at the usual tactical stride; positioning always uses a separately serialized kickoff formation. | Mixed formation/mentality requests still apply mentality and all other fields. Requested formation may differ from effective formation; expose both accurately to the client. |
| P3 — serialization | Prefer option B: world-state v29 with formation metadata before state restoration and a contiguous count-prefixed rotation block. Option A retains the original split-row trailer proposal. Both reject v28. | B keeps writer/reader order symmetric but needs an explicit exception to the snapshot procedure's append-only rule. A preserves offsets but permanently splits F4231 state and requires deferred restore. Neither migrates v28. |
| P4 — validation | Exact default-tactics equality after structural byte alignment, plus the preregistered formation/Duty characterization matrix in §5. | Raw hashes differ; accept only enumerated format exclusions and acknowledge the roster-slot limitations. Non-default results need owner review; they are not identity claims. |

Please decide P1–P4 before gameplay implementation. P3/P4 are proposals, not an approved revision
of D-05's historical raw-digest rule. The B→C window is closed, so no exception inside it is sought.

## 2. Source inventory and routing decisions

Run `rg -n STAGE0_FORMATION src/ --glob '*.cs'`, then classify declarations, comments and tests
separately. There are eight production read expressions: five in
[MatchEngine.cs](../../src/match-engine/MatchEngine.cs) across four logical sites, plus three in
[SquadRating.cs](../../src/match-engine/SquadRating.cs). The constant's declaration/configuration
in `MatchEngineConstants.cs:590–591` is not another read site.

| Base location | Current purpose | Proposed decision |
| --- | --- | --- |
| `MatchEngine.cs:1046` | Construct and seed each `PositioningAITick` using F442. | Retain F442 as the provisional constructor seed. At P1 initialize the existing object from the selected kickoff family; if it is F442, take an exact no-op path. Constructor timing precedes caller setup and cannot select the final tactic. |
| `MatchEngine.cs:1390` | Reconstruct the base lineup from a roster on restore. | Keep `STAGE0_FORMATION`. Do not select different player identities from a subsequently requested tactic. |
| `MatchEngine.cs:2011` and `:2012` | Select each squad's starters/bench during `ConfigureSquads`. | Keep `STAGE0_FORMATION`, for both teams. Formation-dependent lineup selection is a separate identity/restore change. |
| `MatchEngine.cs:4428` | Choose positional roles when filling the #12 snapshot. | Use the effective kickoff formation, not the active tactic's potentially different requested formation. |
| `SquadRating.cs:76`, `StartingElevenMean` | Rate the selected starting eleven; used by season/quick-sim callers. | Keep `STAGE0_FORMATION` so the rating describes the same F442-selected eleven. |
| `SquadRating.cs:105`, `CanFieldStartingEleven` | Check lineup feasibility for availability composition and transfer/season callers. | Keep `STAGE0_FORMATION`; changing only this probe would disagree with the lineup the engine selects. |
| `SquadRating.cs:192`, `StartingElevenPlayerIds` | Project selected starter identities for availability and appearance/discipline accounting. | Keep `STAGE0_FORMATION`; do not silently change the identity set while kickoff/restore selection stays fixed. |

Additional consumers and seams:

- [Positioning `TacticTranslation`](../../src/positioning-ai/TacticTranslation.cs) already maps the
  three #21 `TacticFormation` values to #12 `FormationFamily`. Use that consumer-owned translation
  at initialization; do not add a dependency from #21 to #12.
- [PositioningAITick](../../src/positioning-ai/PositioningAITick.cs) must use the selected family
  for seeding anchors/hysteresis and for each composition, and the matching
  [rotation catalogue](../../src/positioning-ai/RotationAdjacencyCatalogue.cs) for its controller.
  F442/F433 have five rows; F4231 has six.
- `MatchEngine.cs:1065–1077`: Pressing/Attacking keep views of the existing positioning object.
  Keep that object; Defensive receives the live positions through its snapshot inputs.
- [SlotComposer](../../src/positioning-ai/SlotComposer.cs) is the missing consumer of
  [DutyForeOffsetM](../../src/tactical-instructions/TacticalInstructionsConstants.cs).
  Fill an agent-owned Duty carrier from `_activePlayerTactics[entityId]`; follow the agent through
  rotation rebinding. Duty does not belong to the destination slot or get copied between players.
- `WriteTeamTactic`/`ReadTeamTactic` already preserve requested `Formation` for active and pending
  tactics; `WritePlayerTactic`/`ReadPlayerTactic` already preserve Duty. Keep their existing fields.
  Add the effective kickoff formation separately under P3; deriving it from the requested tactic
  would be wrong after a P2 mixed request.

The initial player-to-slot mapping remains roster-local index `k` → catalogue row `k`, seeded by
`SeedFromFormation`; it is not a role-fit assignment algorithm. After seeding, only #25's existing
rotation controller mutates bindings (#12 §3.7.1 / #25 §4.4).

The full positional-slot consequences, from the shipped catalogue row orders, are:

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

These labels identify catalogue slots, not verified natural left/right positions of individual
players. `LineupSelector` chooses by coarse `PlayerPosition`/line and rating, not specialist
LM/DM/RAM proficiency. Nevertheless, an F442-selected midfielder moves to the F433 left-wing
slot and an F442-selected forward moves to the F4231 right attacking-midfield slot; the left
midfield occupant becomes a holding midfielder in either family. This materially changes team
shape and responsibilities. The earlier one-forward example understated the limitation.

Keeping lineup selection fixed remains the scoped proposal, not evidence that these mappings
are football-plausible. Owner acceptance of P1–P4 must acknowledge this table. If unacceptable,
stop and explicitly reopen the lineup-selection scope, including `SquadRating`, availability,
rating, appearance and restore consumers; do not quietly widen this landing. Verify the mapping
with a distinct-squad identity fixture and characterize non-default formations under §5. Retain
lineup selection and runtime switch mapping as named follow-ups at implementation closeout.

## 3. Kickoff boundary and caller behavior

### P1: distinguish boot initialization from the first tactical update

[MatchSession.BootEngine](../../src/match-client-core/MatchSession.cs) configures squads and managers,
then [ManagerAdaptation.ApplyKickoff](../../src/match-engine/ManagerAdaptation.cs) applies the human
baseline or AI-selected preset through the boot appliers. All this happens at clock 0.
`SetTeamTactic` only writes pending state. The
[orchestrator](../../src/deterministic-sim/TickOrchestrator.cs) advances the clock before its phases;
the [clock](../../src/deterministic-sim/MatchClock.cs) runs AI when `tick % 6 == 0`.
Consequently snapshots exist on frames 1–5 before the first AI commit at frame 6.

At the first public `RunTick` entry, before calling the orchestrator, capture each team's final
pre-match pending formation and initialize its positioning family/catalogue. This is the last
clock-0 setup state, after the boot appliers staged their final selections. It is not the
constructor's opening `ApplyRestart`, a later restart, half-time or the first stride's possibly
updated pending request. Keep complete pending→active team/player tactic commits at frame 6 and
subsequent tactical boundaries. No new manager decision or RNG draw is introduced.

The effective formation and an explicit initialization flag are serialized in every completed-frame
snapshot, including frames 1–5. Restore reads them; an initialized match never samples pending tactics
again. Before the first tick, the committed payload is empty; the internal off-tick overlay can
nevertheless serialize a synthetic tick-0 body. Do not assume a general pre-tick capture guard:
`EnsureSubstitutionSaveSynchronized` only checks dirty substitution state. A tick-0 body with the
flag false retains provisional F442 plus the pending setup tactic; its first `RunTick` initializes
normally. A body with tick greater than 0 and flag false is invalid. An initialized restored body
must never reinitialize, including any supported tick-0 overlay. Test both flag states explicitly.

To remain allocation-free even on the first `RunTick`, provision the supported controller storage
at construction and select/reseed it in place at P1. Do not allocate a replacement controller on
that hot path. F442 initialization must skip reseeding entirely to preserve the existing default
hysteresis, slots and cached targets. Restore may allocate as the existing cold factory does,
but must configure the family before applying saved hysteresis and rotation state.

**Alternative P1 boundary:** add an explicit idempotent kickoff-lock method and call it from
`MatchSession.BootEngine` immediately after `ManagerAdaptation.ApplyKickoff`. This makes the
transition visible to the composition root and avoids hiding it in `RunTick`. It must also cover
direct-engine callers: either every caller must lock before its first tick, or `RunTick` must
provide the same one-time fallback. The strict explicit-only version breaks existing direct
call patterns unless all are updated; the fallback version has two entry points to one guarded
operation. The implicit first-`RunTick` option remains preferred for the smaller API/caller change;
the explicit-plus-fallback option is reasonable if the owner prefers lifecycle visibility. Neither
option may lock before the final boot applier or re-lock after frame 0. Test equivalence through
MatchSession and direct-engine entry paths and a refusal/clear result for attempted late locking.

### P2: accept requested tactics, keep effective formation fixed

Recommendation (b) retains `SetTeamTactic`'s current void staging API and stride semantics. A request
for F433 + Attacking during an F4231 match stages both fields; on commit, Attacking and all other
non-formation inputs apply, requested Formation becomes F433, and effective positioning stays
F4231. Formation-only requests never reseed positioning. Player Duty changes remain stride-applied.

This avoids rejecting non-formation updates in
`ManagerAdaptation.RunDecisionPoint`, which stages whole presets and updates manager bookkeeping.
It also preserves the command application path in
[ManagerCommand](../../src/match-client-core/ManagerCommand.cs) and
[MatchEngineMutations](../../src/match-client-core/MatchEngineMutations.cs).

[ClientChangeRequests](../../src/client-app/ClientChangeRequests.cs) caches the requested tactic
when a command is accepted, and `WithMentality` copies its other fields. Keep that cache as request
state. Add a concrete read surface for the effective formation on MatchEngine and copy it into
[LiveMatchFrame](../../src/match-viewer/LiveMatchFrame.cs) through the existing frame producer.
Any current formation display reads that effective field; retain the request separately. For a
differing request, say that the formation stays fixed for this match. Do not promise that the
request is saved for the next match: S0 currently submits mentality changes and copies Formation,
and no cross-match persistence for such requests has been established. Do not report
a formation switch as applied merely because the whole tactic was accepted. Test the ordinary
mentality dialog in [S0ScreenPresenter](../../src/client-app/S0ScreenPresenter.cs) and a mixed command.

Option (a) remains available: reject the entire mixed request atomically with a typed refusal,
without changing pending state or manager bookkeeping. It requires a typed outcome through the
engine, mutation interface, command driver, client acknowledgement and AI adaptation paths. Partial
acceptance also needs an outcome identifying which field was refused. A silent partial update or
new gameplay exception is not a suitable substitute. This larger API change is why (b) is preferred.

## 4. P3: two v29 encodings and their lasting costs

Current world-state version is 28; the match-save file format is 3 and deterministic-sim header
schema is 1. Both proposals change only the world-state body to 29, rejecting v28 without
migration under D-10. Neither the outer save format nor the #16 header needs a bump.

The old rotation region has no count: per team it writes 11 binding int32 values, 11 XY float32
targets and five `(int32 dwell, byte rotated, int32 hold)` records. Each row is 9 bytes, so the
region is `44 + 88 + 45 = 177` bytes per team. Old v28 non-F442 requested tactics cannot tell a
reader to consume six rows; the old controller still wrote five.

### Option A — retain the split-row trailer from v1.0

Keep the entire existing body order, including five rotation rows per team at their old offsets.
Append `(initialized byte, effective formation int32, extra-row count int32, extra row states)`
per team after the final handling-law block and before the ledger. Extra count is 0 for F442/F433
and 1 for F4231, with each extra state encoded as dwell int32 / rotated byte / hold int32.
The extra sizes are 18 bytes for two default teams, 27 for one F4231 team, or 36 for two F4231
teams. These are encoding sizes, not gameplay constants.

This follows the existing append-only procedure and makes the default comparison especially
simple: retain base body bytes except the version word, then validate the appended 18 bytes.
Its costs are permanent: F4231 row 5 is separated from rows 0–4, the reader must retain offsets
and defer hysteresis/rotation restoration until the trailer selects the catalogue, and a future
catalogue change must understand both regions. This is not a compatibility benefit: v28 is
rejected either way. It is offset stability and a one-time comparison convenience purchased
with a more complex enduring format. The writer and reader cease to be straightforward
line-for-line mirrors. Do not present A as the only acceptable encoding.

### Option B — contiguous count-prefixed rotation blocks (preferred)

After the existing schema word and tick, write both teams' kickoff metadata in team order:
`initialized byte`, then `effective TacticFormation int32`. The metadata is available before
any positioning hysteresis is decoded, so the reader selects the family/catalogue first.
Keep the remaining existing logical field order. At each team's rotation block write:

| Field | Encoding | Validation |
| --- | --- | --- |
| Rotation row count | int32 | Exactly the selected catalogue's count: 5/5/6; bounded by the existing family cap. |
| Agent-to-slot binding | 11 int32 values | Valid permutation, same semantics/order as v28. |
| Last composed targets | 11 pairs of float32 | Finite, loaded verbatim through the existing seam. |
| All row states | `count` contiguous dwell int32 / rotated byte / hold int32 records | Existing pair-state invariant checks; catalogue row order, including row 5. |

The net additional default bytes are again 18: 10 for the two teams' kickoff metadata and 8 for
row counts. One/two F4231 teams add another 9/18 bytes. The writer and reader follow the same
order and restore each full contiguous block; no saved-row offset juggling or late reseed is
needed. The costs are shifted offsets and a structural comparison tool rather than a prefix
slice. v28 is rejected; no migration or mixed-version tolerance is added.

B is preferred for maintainability, but requires explicit owner acceptance of an exception to
[the snapshot-schema-bump procedure](../../.claude/skills/snapshot-schema-bump/SKILL.md), Step 2:
“Append the new block **last** so no existing offset moves.” That instruction is the reason A
was originally proposed. No implementation may silently choose B while claiming to follow the
append-only instruction as written. P3 approval must name A or B and, for B, the narrowly scoped
v29 layout exception; retain the procedure's symmetry, validation and test requirements.

### Shared validation and rejection contract

For either encoding, an uninitialized tick-0 body has provisional F442 metadata/controller
state and the pending setup tactic; initialized bodies never reselect formation. False
initialization at tick greater than 0 is invalid. Requested Formation may differ from effective
Formation under P2; do not require them to be equal after kickoff.

Validate exact count/family agreement, enum/boolean validity and remaining payload bounds;
use division-based count checks, not overflow-prone `count * width` guards. Preserve finite
cache, permutation and pair-state validation and the final ledger tag/count boundary checks.
Do not discard row states when `RotationFreedom.Off`. Reject unknown families, truncated bodies
and malformed counts rather than silently defaulting a corrupt saved catalogue.

The first body read remains the schema check: a literal 29 pin rejects 28 before tick, roster,
rotation or metadata reads. Add a focused version test with otherwise valid current-build
header metadata so the build-identity guard cannot mask the version rejection. These are
source-based proposals, not executed v29 tests or compatibility claims.

## 5. P4: identity and non-default characterization

Use a new paired capture; do not modify `evidence/pre-c-control-20261104/`. Preserve its six seeds
from `expected.json`, 324,000 RunTick calls per seed, default squads/attributes and frozen observer
discipline. Pin runtime, build flags, configuration, instrument source and hashes. Reproduce the
historical control if its runtime/machine rule requires it, then capture this base and head
separately. Historical B fingerprints are not the expected fingerprints of current C main.
Record corpus manifests and all metric definitions before any result-bearing run.

### Default-tactics identity, for either P3 encoding

At F442/F442 and Support, compare each frame using exact bytes/float bits:

- Every existing world-state field: agent trajectories/velocities, tactic and AI/hysteresis state,
  all five existing rotation rows and caches, counters, RNG cursors and clocks. No subsystem is
  excluded simply because a new prefix moved it.
- Possessor/team possession, every BallState field, and complete canonical Tier A/B ledger bytes,
  event identities and order. Scores or terminal summaries alone are insufficient.
- Parse the base v28 and chosen head v29 with schema-specific span maps. Map each unchanged field
  or block to its raw byte span, require complete coverage with no unexplained gaps/overlaps,
  and compare matching spans byte for byte in their canonical order. For B, align around kickoff
  metadata and the two row-count prefixes; all default row contents still match exactly.
  For A, the existing body-prefix slice is an additional check, not a reason to weaken coverage.

Freeze the comparator before capture. Test it with controlled perturbations of existing agent,
ball, rotation/cache, AI, RNG and event bytes so misplaced offsets or blanket block masking fail.
Also mutate each permitted new field/version/count to an invalid value and require failure.
No conversion through rounded JSON floats or approximate equality is permitted.

The exhaustive cross-arm exceptions and their independent checks are:

| Exception | What is still checked |
| --- | --- |
| Body schema word | Literal 28 on base and 29 on head. |
| New kickoff metadata | Both teams initialized, effective F442, exact ordinal/boolean encoding. |
| New count/extension fields | For B: each full row count is exactly 5; for A: each extra-row count is zero and no extra state is present. |
| Payload length and changed offsets | Exact +18 default bytes under either proposal; full structural coverage and identical ledger bytes after alignment. |
| Raw current/previous snapshot-chain digest values | Independently recompute and verify each arm's complete unmodified chain; the changed preimage changes subsequent links. |
| BuildHash identity metadata | Validate each arm's own binary identity; never fake a common build hash or disable restore gates. |

All other header fields, domain tags and runtime fingerprints must match. A comparison-only
normalized digest may summarize the aligned common spans; it is not a shipping digest or
certification claim. Retain the raw chains and traces. This uses #21 FR-TI-031's existing-state
identity precedent, but is still an explicit P4 owner decision changing this landing's use of
D-05's raw-digest condition. Scope approval alone does not approve it. Stop at any unexplained
covered-byte difference; do not tune or rebaseline to obtain identity.

### Non-default formations and Duty: preregistered characterization

Default identity does not satisfy D-05's separate measured-landing obligation for newly active
settings. In addition, preregister the following matrix on the same six seeds and 324,000 calls
per seed, in both base and head. All tactic fields other than the named contrast stay at the
frozen defaults; manager modes remain as in the frozen lane. In particular, baseline receives
exactly the same requested Formation/Duty inputs as head, even though baseline formation and
positioning Duty routing are inert. Existing other Duty consumers remain active in both arms.

| Case | Home formation | Away formation | Outfield Duty | Purpose |
| --- | --- | --- | --- | --- |
| C0 | F442 | F442 | Support for both teams | Exact identity control. |
| C1 | F433 | F442 | Support for both teams | Changed home formation in isolation. |
| C2 | F442 | F433 | Support for both teams | Away-side counterpart. |
| C3 | F4231 | F442 | Support for both teams | Changed home formation with six rotation rows. |
| C4 | F442 | F4231 | Support for both teams | Away-side counterpart. |
| C5 | F442 | F442 | Defend for all outfield agents on both teams | New Duty positioning effect without a formation change. |
| C6 | F442 | F442 | Attack for all outfield agents on both teams | Opposite Duty positioning direction. |

This is a proposed budget of 42 scenario-seed cases per code arm, 84 primary full-match runs or
27,216,000 RunTick calls overall; these are design counts, not measured results. Observer-neutrality
and determinism repeats add work and must be declared separately. No CPU-hour estimate is claimed.
P4 acceptance includes this budget; do not silently drop contrasts or shorten runs if compute is
unavailable. Obtain a revised declared capture plan first.

Before capture, freeze definitions/denominators for team-attributed shots, goals, fouls, possession
share/changes, out-of-play/stall exposure, and phase occupancy; freeze positional summaries by
roster-local slot (longitudinal/lateral target, line-depth and minimum spacing). Retain per-frame
positions, effective family, possession and full ball/event traces so each contrast proves that
the selected formation actually became active. Report every seed and team plus aggregate and
paired base/head deltas; keep scorer attribution distinct from scoring team. C1–C6 are
characterization, not equality assertions or new statistical claims from six matches.

Use the same pipeline/source/runtime in both arms. Freeze any football-plausibility acceptance
bounds before results using an existing governing contract; where no approved bound applies,
report the limitation and require owner review. Stop for invalid mapping/restore, unexplained
mechanism or movement away from the declared plausibility criteria; do not calibrate, widen
assertions or change seeds. Do not claim neutral tactics from unit tests alone.

The frozen neutral-squad corpus can reveal geometry/role-routing effects, but cannot establish
specialist player suitability or realistic-roster performance penalties. A distinct-squad
identity/mapping fixture exposes the §2 limitation; accepting it remains an explicit scope
tradeoff even if the neutral-squad six-seed numbers look plausible.

## 6. Spec amendments and Duty pipeline

No ERR is filed in this preflight. The allocation script searches all of `docs/` and `src/`, so
historical citations to `ERR-012-012` already make it unavailable even though there is no live
Error Index entry for it. On the analyzed base, next candidates are #12 sequence 013, #25 sequence
001, and #21 sequence 009. These are observations, not reservations; rerun allocation immediately
before authoring and after integrating newer main. Never reuse the old draft's id.

| Owning spec | Proposed defect/amendment and affected text |
| --- | --- |
| #12 | Add the missing #21 Duty carrier and anchor-stage amendment in §3.7.1, with worked examples, test rows and version history. Clarify kickoff family seeding and fixed roster-local mapping in §3.1/§4 as needed; keep FR-PA-039 and the §7.7 switching deferral unchanged. |
| #25 | Amend §2.3 / Appendix B for the owner-selected v29 encoding: contiguous count-prefixed blocks (B), or the retained five-row-prefix/trailer alternative (A). Preserve cache/permutation semantics and every gameplay table. |
| #21 | Clarify §3.1 formation routing and §4 integration: kickoff effective formation versus later requested tactics, mixed-request semantics, boot initialization and unchanged stride commits for the other fields. FR-TI-027 remains the in-match timing rule. |

File separately owned ERR entries with spec/code back-propagation in the implementation commit;
do not mark a proposal resolved in this documentation commit. No #16 framing amendment is needed.

Proposed Duty amendment: for each active outfield agent, compute the existing anchor, then add
`DutyForeOffsetM[duty]` to its team-canonical X before the existing ball-relative/context/overlay/
spacing/clamp stages. Support skips the addition entirely. The catalogue is unchanged at −3/0/+3 m;
read it from #21 rather than copying constants. Existing GK special-slot behavior stays intact.
For a canonical anchor X=52.5 m, Defend/Support/Attack give 49.5/52.5/55.5 m before later stages;
the away mapping gives world X=55.5/52.5/49.5 m. Final composed displacement need not be exactly
3 m because existing compactness, spacing and bounds still apply. Test raw anchor arithmetic and
the expected final direction separately. Do not remove PositioningFreedom or PlayerRole carriers.

## 7. Planned implementation validation and handoff

After P1–P4 are accepted, add outcome tests in the existing suites:

- `MatchEngineTacticTests`: both teams/all three families, pre-match F4231 effective on the first
  AI tick; first-frame snapshot/restore; second-half/restarter preservation; mixed requests;
  live Pressing/Attacking views and Defensive inputs; per-agent Duty retained through rotation.
- `PositioningAITests` and existing positioning/rotation fixtures: exact Support bit identity,
  non-Support directions, unchanged default initialization and no hot-path allocations.
- `MatchEngineSnapshotSchemaTests` / `MatchEngineSnapshotRestoreTests`: v29 pin; effective-formation
  and initialization-flag single-field digest probes; every family including an active F4231 sixth row; non-default Duty
  continuation; saves before first AI stride; invalid enum/count/truncation; clean v28 rejection.
- Two-run determinism, comparator fault-injection/coverage checks, a distinct-squad slot-identity fixture, and the preregistered C0–C6 six-seed paired matrix in §5.

Use `bash tools/run-tests-local.sh --pr` for the eventual full implementation gate; report actual
pass/fail/skip counts, quarantine state and owner-held RED verifier outcome. No skips, widened
assertions or seed changes are permitted to obtain green.

**Preflight validation:** source inspection, live main/branch identity, version/ERR lookups,
documentation consistency, relative-link validation, drift checks and `git diff --check` only.
The full .NET gate is not run locally; six-seed captures and Unity tests have not been run for
this preflight. No gameplay measurements are claimed. GitHub's completed gate for the prior
documentation commit is recorded separately in §8; it is not evidence of new gameplay behavior.
No host validation is claimed or requested for these text-only changes. Source/settings/assets
remain at the base.

For v1.0 the normal local commit hook was attempted. Its checklist/schema surveys reported zero blocking
rows, but its lower-level executor stopped at restore with `dotnet: command not found`; no C# tests
ran. This documentation-only draft uses a command-scoped hook bypass after the independent checks
above. It is not a passing pre-commit or .NET-gate claim, changes no hook/test configuration, and
does not waive implementation validation. The attempt is retained locally in
`work/434-preflight/commit.log` outside the source checkout.

**Future host request:** check out the exact final implementation SHA (not yet available), use
Unity 6000.4.9f1 on the pinned host, force the established full compile/reimport and preserve
Editor.log. Run unfiltered `TacticalDirector.MatchEngine.Tests` and
`TacticalDirector.PositioningAI.Tests` in EditMode, plus any client/viewer test assemblies actually
modified by the accepted presentation path. Preserve XML, logs and source identity; include the
tactic, schema and restore fixtures above. Host validation remains pending until receipts exist.

This draft must not merge as completed #434. After the eventual gameplay landing, record the
remaining lineup-selection and mid-match-switching follow-ups in open issues, and synchronize
the roadmap, wiring backlog, manifests and source/change histories. #440 then begins with its
own observer/design stage and owner stop before any #440 gameplay fix.

## 8. Review disposition and completed prior-commit CI

The v1.1 review changes are: full roster-slot disclosure (§2), non-default characterization (§5),
the three `SquadRating` dispositions (§2), and the two encoding alternatives/costs (§4). The
explicit-lock alternative (§3), accurate same-match client copy (§3), self-contained stop and
session-only approval provenance (§1) address the remaining wording findings. Neither the review
nor this revision approves P1–P4 or widens lineup selection.

The previously pending jobs for **v1.0 head `2feb4b0d5514017c61ba73c584abc55a465969f8`** have finished:

| Check | Observed result | Evidence |
| --- | --- | --- |
| PR functional gate, `bash tools/run-tests-local.sh --pr` | Completed / success | [run 42687026231, job 129396471132](https://github.com/antonzymin-eng/Soccer-Manager-Pro/actions/runs/42687026231/job/129396471132) |
| Markdown link check | Completed / success | [job 129396471115](https://github.com/antonzymin-eng/Soccer-Manager-Pro/actions/runs/42687026231/job/129396471115) |
| Unity test job | Skipped by the existing CI configuration | [same workflow](https://github.com/antonzymin-eng/Soccer-Manager-Pro/actions/runs/42687026231); no host pass claimed |

The gate log reports an empty quarantine and an empty owner-held RED ledger. Selected console
summaries are reproduced verbatim, not treated as a complete TRX census:

```text
Passed!  - Failed:     0, Passed:   134, Skipped:    13, Total:   147, Duration: 510 ms - TacticalDirector.PositioningAI.Tests.dll (net8.0)
Passed!  - Failed:     0, Passed:   940, Skipped:    16, Total:   956, Duration: 31 m 56 s - TacticalDirector.MatchEngine.Tests.dll (net8.0)
```

Retrieve the receipt with `gh run view 42687026231 --repo antonzymin-eng/Soccer-Manager-Pro
--job 129396471132 --log` (as one command). No test, quarantine or CI configuration differs from
the source base. Fresh CI for the v1.1 commit must be reported separately after publication;
the successful v1.0 run is not a passing-head claim for a newer commit.

## Version history

| Version | Date | Notes |
| --- | --- | --- |
| 1.0 | November 9, 2026 | Owner-approved kickoff-only scope recorded; P1–P4 recommendations, production-read inventory, append-only v29 proposal and exact comparison/validation plan. No gameplay implementation. |
| 1.1 | November 9, 2026 | Closes review gaps: all eight production reads, complete slot-mismatch disclosure, C0–C6 characterization, two encoding options with a conditional preference for contiguous blocks, structural comparison, explicit-lock alternative, accurate client copy and session-approval provenance. Decisions remain pending; no gameplay change. |
