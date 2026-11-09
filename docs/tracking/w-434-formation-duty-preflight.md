# #434 formation and Duty — implementation preflight

**Created:** November 9, 2026
**Purpose:** Resolve the kickoff-only formation contract, snapshot encoding and default-tactics
comparison before implementing #434 part (i).
**Version:** 1.0
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

This is v4's required preflight stop. No approved specification, gameplay code, schema constant,
test assertion, seed, tuning value or historical evidence is changed here. #440 has not started.

| Decision | Recommendation | Consequence requiring owner acceptance |
| --- | --- | --- |
| P1 — kickoff boundary | Capture the final pre-match pending formation at the first `RunTick` entry, while the clock is still 0, before the orchestrator advances to frame 1. Initialize that formation in place before any frame is simulated. | Formation boot initialization is distinct from committing the complete pending tactic, which stays on the existing first AI stride at frame 6. |
| P2 — mid-match requests | Option (b): accept the full requested tactic at the usual tactical stride; positioning always uses a separately serialized kickoff formation. | Mixed formation/mentality requests still apply mentality and all other fields. Requested formation may differ from effective formation; expose both accurately to the client. |
| P3 — serialization | World-state schema v28 → v29, retaining the five legacy rotation rows at their existing positions and appending a formation/extra-row trailer. No migration. | New format rejects v28 bodies; v29 reader uses deferred restoration so it selects the catalogue before decoding the saved rotation state. |
| P4 — identity | Default-tactics equality of every existing world-state byte except the body schema word, plus exact possession, ball, trajectories and complete canonical ledgers. Validate the appended trailer separately. | Raw snapshot-chain hashes differ. Exclude only the enumerated format/identity consequences in §5; do not exclude whole subsystems. |

Please decide P1–P4 before gameplay implementation. P3/P4 are proposals, not an approved revision
of D-05's historical raw-digest rule. The B→C window is closed, so no exception inside it is sought.

## 2. Source inventory and routing decisions

`rg -n STAGE0_FORMATION src/ --glob '*.cs'` finds five production read expressions in
[MatchEngine.cs](../../src/match-engine/MatchEngine.cs), across four logical sites:

| Base location | Current purpose | Proposed decision |
| --- | --- | --- |
| `MatchEngine.cs:1046` | Construct and seed each `PositioningAITick` using F442. | Retain F442 as the provisional constructor seed. At P1 initialize the existing object from the selected kickoff family; if it is F442, take an exact no-op path. Constructor timing precedes caller setup and cannot select the final tactic. |
| `MatchEngine.cs:1390` | Reconstruct the base lineup from a roster on restore. | Keep `STAGE0_FORMATION`. Do not select different player identities from a subsequently requested tactic. |
| `MatchEngine.cs:2011` and `:2012` | Select each squad's starters/bench during `ConfigureSquads`. | Keep `STAGE0_FORMATION`, for both teams. Formation-dependent lineup selection is a separate identity/restore change. |
| `MatchEngine.cs:4428` | Choose positional roles when filling the #12 snapshot. | Use the effective kickoff formation, not the active tactic's potentially different requested formation. |

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
rotation controller mutates bindings (#12 §3.7.1 / #25 §4.4). An F442-selected forward can therefore
occupy an attacking-midfield slot in F4231. Disclose this and retain lineup selection and runtime
switch mapping as named follow-ups at implementation closeout.

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
Any current formation display reads that effective field; retain the request separately and show
that formation changes take effect next match when a differing request is presented. Do not report
a formation switch as applied merely because the whole tactic was accepted. Test the ordinary
mentality dialog in [S0ScreenPresenter](../../src/client-app/S0ScreenPresenter.cs) and a mixed command.

Option (a) remains available: reject the entire mixed request atomically with a typed refusal,
without changing pending state or manager bookkeeping. It requires a typed outcome through the
engine, mutation interface, command driver, client acknowledgement and AI adaptation paths. Partial
acceptance also needs an outcome identifying which field was refused. A silent partial update or
new gameplay exception is not a suitable substitute. This larger API change is why (b) is preferred.

## 4. P3: append-only v29 world-state encoding

Current world-state version is 28; the match-save file format is 3 and deterministic-sim header
schema is 1. Only the world-state body version is proposed to change. D-10 permits rejecting old
saves; neither the outer save format nor the #16 header needs a bump for this proposal.

The old rotation region has no count: per team it writes 11 binding int32 values, 11 XY float32
targets and five `(int32 dwell, byte rotated, int32 hold)` records. Each row is 9 bytes, so this
region is `44 + 88 + 45 = 177` bytes per team. Merely changing its iteration bound to six would
shift later data, and old v28 non-F442 requested tactics cannot disambiguate it.

Recommended encoding respects the snapshot procedure's instruction to append new fields last:

1. Preserve the entire existing body field order, including exactly five rotation rows per team
   at the existing rotation offsets. These are rows 0–4 of the selected catalogue.
2. After the current final handling-law block and before the canonical event ledger, append a
   trailer in team order 0 then 1:

   | Field | Encoding | Rule |
   | --- | --- | --- |
   | Kickoff initialization complete | byte boolean | False only for an uninitialized tick-0 body; true in every completed-frame capture. |
   | Effective kickoff formation | int32 #21 `TacticFormation` ordinal | Valid shipped family; translate through #12's mapping. |
   | Extra rotation-row count | int32 | Exactly catalogue row count minus five: 0 for F442/F433, 1 for F4231. |
   | Extra row states | repeated int32 dwell, byte rotated, int32 hold | Catalogue rows 5 onward, preserving existing validating restore seams. |

The trailer adds 18 bytes for two default teams, 27 bytes for one F4231 team, or 36 for two F4231
teams. These are encoding sizes, not gameplay constants. Pin fixed legacy row count 5 and derived
row width 9 with source tags in the owning constant catalogue; no new tunable is proposed.

The reader must first validate schema 29, then parse the legacy prefix while retaining the byte
locations of positioning hysteresis and the fixed-size rotation blocks. Do not interpret rotation
state through the boot F442 controller. After reading/validating the trailer, select each live
positioning object's family/catalogue, then restore the saved hysteresis, binding, target cache,
five legacy rows and any extra rows through their validating seams. The extra rows are state,
even at `RotationFreedom.Off`; never discard them. This deferred restore avoids reseeding over
loaded state or reading six rows from a five-row prefix.

Validate exact count/family agreement, enum validity, bounds against `payload.BytesWritten`,
finite cached targets, permutation and pair-state invariants. Use division-based count bounds,
not overflow-prone `count * width` checks. Preserve the final event-ledger tag/count boundary
checks. A malformed count, truncated trailer or unknown family fails closed.

The first body read is the existing schema check (`DeserializeWorldState`); with a literal 29
pin, a 28 body is rejected before tick, roster, rotation or trailer reads. Add a focused version
test using otherwise valid current-build header metadata so the build-identity guard cannot mask
the version rejection. This is a source-based proof plan, not an executed v29 test.

## 5. P4: precise comparison contract

Use a new paired capture; do not modify `evidence/pre-c-control-20261104/`. Preserve its six seeds,
324,000 calls per seed, default squads/attributes, shipping tactic defaults and frozen observer
discipline. Pin SDK/runtime, flags, configuration, instrumentation source and hashes identically
in both arms. Reproduce the historical control when required by its runtime/machine rule, then
capture this base and implementation head separately. The historical B fingerprints are not the
expected fingerprints of current C `main`.

At default F442/Support, compare every frame with exact float-bit equality:

- All agent positions, velocities and every other existing world-state byte, including tactic
  state, AI/hysteresis state, all existing rotation rows/caches, counters, RNG cursors and clocks.
- Possessor/team possession, every BallState field, and the complete canonical Tier A/B ledger
  bytes and event identities/order. Scores and event counts alone are insufficient.
- For base body length `L`, head bytes `[4,L)` must equal base bytes `[4,L)` byte for byte. The
  first word must be the expected 28/29 respectively. Head's next 18 bytes must decode to two
  explicit initialized/F442/zero-extra-row entries. Ledger bytes immediately after their respective body
  boundaries must match exactly; relocation is not an event-field exclusion.

The exhaustive cross-arm exceptions are:

| Exception | What is still checked |
| --- | --- |
| Body schema word, offsets 0–3 | Literal 28 on base and 29 on head. |
| New trailer at the end of the body | Exact default encoding above, valid in non-default restore tests; no arbitrary masking. |
| Payload length / ledger offset | Exact +18 bytes for defaults; full ledger comparison after structural alignment. |
| Raw current/previous snapshot-chain digest values | Recompute and verify each arm's complete unmodified chain independently. The new preimage changes the current digest and all later links. |
| BuildHash identity metadata | Record and validate each arm's own build hash; never pretend different binaries are one build or disable restore identity gates. |

All other header fields, domain tags, runtime fingerprint, clocks and existing body bytes must
match. A comparison-only normalized digest may summarize the retained common bytes; it is never
a shipping digest or certification claim. Raw digests/traces remain preserved separately.
This follows #21 FR-TI-031's explicit existing-world-state-subset identity precedent, subject to
owner acceptance for this landing. Any unexplained difference is a stop, not a rebaseline.

## 6. Spec amendments and Duty pipeline

No ERR is filed in this preflight. The allocation script searches all of `docs/` and `src/`, so
historical citations to `ERR-012-012` already make it unavailable even though there is no live
Error Index entry for it. On the analyzed base, next candidates are #12 sequence 013, #25 sequence
001, and #21 sequence 009. These are observations, not reservations; rerun allocation immediately
before authoring and after integrating newer main. Never reuse the old draft's id.

| Owning spec | Proposed defect/amendment and affected text |
| --- | --- |
| #12 | Add the missing #21 Duty carrier and anchor-stage amendment in §3.7.1, with worked examples, test rows and version history. Clarify kickoff family seeding and fixed roster-local mapping in §3.1/§4 as needed; keep FR-PA-039 and the §7.7 switching deferral unchanged. |
| #25 | Amend §2.3 / Appendix B to pin the five-row legacy prefix plus versioned formation/extra-row trailer and catalogue-aware deferred restore. Preserve cache/permutation semantics and every gameplay table. |
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
- Two-run determinism and the separately preregistered six-seed paired contract in §5.

Use `bash tools/run-tests-local.sh --pr` for the eventual full implementation gate; report actual
pass/fail/skip counts, quarantine state and owner-held RED verifier outcome. No skips, widened
assertions or seed changes are permitted to obtain green.

**Preflight validation:** source inspection, live main/branch identity, version/ERR lookups,
documentation consistency, relative-link validation, drift checks and `git diff --check` only.
The full .NET gate, six-seed captures and Unity tests are not run for this documentation-only
preflight; there are no gameplay measurements or pass counts to report. No host validation is
claimed or requested for these text-only changes. Source/settings/assets remain at the base.

The normal local commit hook was attempted. Its checklist/schema surveys reported zero blocking
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

## Version history

| Version | Date | Notes |
| --- | --- | --- |
| 1.0 | November 9, 2026 | Owner-approved kickoff-only scope recorded; P1–P4 recommendations, production-read inventory, append-only v29 proposal and exact comparison/validation plan. No gameplay implementation. |
