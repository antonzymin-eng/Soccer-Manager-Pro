# #434 paired capture protocol 1.1

Created November 9, 2026. Freeze this file and the runner/comparator before any new
#434 result-bearing run. Historical control reproduction uses its own existing freeze.
Owner approved the preflight P1–P4 and roster-index limitation in-session (“Yes”).

Base: d4b026aa951432d342217314b8c4d49b53a19e7d. Head: the implementation commit
recorded by `prepared.json`; never substitute uncommitted production input. Freeze
source archives, runner, runtime and binaries with SHA-256 before running probes.
SDK 8.0.426, CLR 8.0.32, Debug, optimization disabled, DEVELOPMENT_BUILD, unchanged
JIT defaults, DOTNET_PROCESSOR_COUNT=4; one seed per process, two captures concurrently.
Use the historical six seeds and 324000 calls per full match. Reproduce all six
historical B terminal fingerprints before interpreting new results. No historical
file changes, configuration binding, squad changes, AI managers, or tuning.

C0–C9 and conditional base reuse are exactly preflight §5. C5/C7 set all home
outfield Duty to Defend/Attack, C6/C8 mirror away; other teams and GK are Support.
C1/C2 one F433 home/away, C3/C4 one F4231 home/away, C9 both F4231; C0 both F442.
All other team fields are Balanced, shipping SlowDown; Human managers asserted.
No formation-aware lineup selection. Formation + non-Support interaction has unit
replay coverage but no full characterization case; no budget expansion for it.

The instrument is a standalone, read-only runner over **unmodified** production
binaries. It reads committed snapshots and existing state/ledger accessors after
RunTick, uses no callbacks, subscriptions, simulation writes, config or RNG draws.
Plain mode omits structural inspection; observed mode adds pure copied-byte processing.
Six-seed C0 full matches in both modes on both arms prove observation neutrality:
every raw digest, trajectory and ledger must match within the same arm. These proof
runs are additional to the 90 unique characterization matches, not new case-arms.
Base/head C0 observed runs are shared with that matrix (12 additional full runs).
Historical reproduction adds 12 further full runs under its existing protocol.

Retain compressed per-frame raw digests, aligned-world hashes, all 22 positions,
full BallState and possession; retain complete canonical ordered ledger bytes and
per-frame geometry/phase witnesses. Independently reconstruct each snapshot SHA-256
preimage and check its previous link on every observed frame. Preserve nonempty
per-arm BuildHash and compiled module identities; no equality bypass at restore.

Alignment is structural: find/validate each real rotation block from its complete
binding/cache/row bytes using existing read-only state (unique ordered match), then
validate head count and metadata. Before rotation, all serializer loops have fixed
length; record and assert exact offsets. C0 removes only the 4-byte world schema,
10-byte new metadata and two 4-byte count words. Every other byte, including the
canonical event ledger, is included exactly once, in order. No floating tolerance
or field masks. A span-coverage map accounts for the whole payload. Compare aligned
hashes per frame plus trajectory and ledger bytes independently. Mutation self-tests
must detect changed common bytes (agent/ball/ledger) and reject changed permitted
metadata/counts; failed alignment or identity is a hard stop, never a rebaseline.

For base reuse, first retain a complete production source search for Formation,
whole-tactic copies/comparisons and direct-engine reachability. File loaders and
client acknowledgments compare complete tactics but are absent in this lane; Human
mode prevents adaptation. Probe base C0/C1/C2/C3/C4/C9 for 1000 frames on each seed.
Only active/pending requested-formation int32 values of the changed teams may differ;
validate expected values before ignoring those exact fields. Every other state,
trajectory and ledger byte must match; independently check each original raw chain.
This short probe establishes 1000 frames only. Full reuse additionally depends on
pinned source nonconsumption, not an extrapolated empirical guarantee. Report reused
base C0 references as such, never fabricate full non-default base traces or digests.
If source/probe eligibility fails, run the approved 120-match characterization fallback.

Shots = every ShotExecutedEvent, attributed by TeamId; goals = GoalAwardedEvent by
ScoringTeam; fouls = FoulCommittedEvent by offender team (unknown team rejects).
Possession = controlled-ball physics frames per team, loose frames separate; team
pass-flight possession and phase are separate recorded witnesses. Denominators:
324000 scheduled calls, with live frames before full time reported separately.
AI phase samples on the 6-frame stride before full time, per team, all four #12
phase counts. Geometry: finite active outfield slots, longitudinal/lateral extents,
minimum pair distance, all 22 positions/slots retained at each AI sample. Report
per-seed/team metrics and paired deltas. No numeric football plausibility bands;
metrics are descriptive, subject to normal owner PR review. NaN, invalid attribution,
missing records, state/restore/invariant failures or default drift stop acceptance.
No post-result tuning or favorable-case exclusion. Full gate is exactly
`bash tools/run-tests-local.sh --pr`; distinguish existing skips/inconclusives from
new skips (none authorized). Unity host validation remains separately required.

## Instrument review, before acceptance comparisons

Version 1.1 adds the omitted per-physics-frame pass-flight witness (little-endian
int32 receiver ID, int32 team-in-possession; −1 for none) and its per-team live-frame
counts. It also adds exhaustive one-bit mutations at every byte of a real committed
snapshot with an actual canonical event record. Every common-byte mutation must
change alignment output; mutations in excluded metadata must reject. No field
exclusion, case, seed, metric definition, denominator, production source or tuning
changes. The preliminary v1.0 control batch was stopped uniformly during collection,
before evaluating base/head identity. Preserve its attempts separately as aborted;
all acceptance captures restart with the same v1.1 instrument in both arms. The
original protocol/runner remain recoverable in commit 23f2fedb. Counts include every
attempt; aborted runs are never counted as completed validation.
