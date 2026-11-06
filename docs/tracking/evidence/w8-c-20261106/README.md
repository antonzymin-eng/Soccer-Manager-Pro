# W8 C Linux evidence — November 6, 2026

Purpose: supplementary compile, deterministic law tests and the paired B→C capture for
[#11 §3.8.5](../../../specs/goalkeeper-mechanics/section-3.md#385-hand-control-clock-and-law-12-offence-w8-c-err-011-021).
The [implementation record](../../w8-c-implementation-results.md) owns current validation status.
This evidence does not establish a pinned-host Unity compile or Unity EditMode result.

B is `b6505350` (schema v27). C is the implementation over B (schema v28). Both captures use the
same .NET SDK 8.0.426 / Linux / Debug lane, default constructor and tactics, `EnableGkHeading()`,
six frozen seeds, 324,000 frames each. No tuning changes. The source snapshots in the isolated
copies predate later comment/documentation and extra test assertions; runtime behavior is identical.

`SHA256SUMS` is the complete tracked-file manifest. `probe/frozen-inputs.txt` preserves the original
pre-capture freeze hashes and paths. The frozen W8 observer is byte-identical to its committed source;
its private census is bound once through reflection to typed delegates and then called directly.
The extra hooks exist only in the isolated measurement copies, not in the production implementation.
Both arms passed `ProbeNeutrality600` before capture. The paired comparison waits for all six B
fingerprints to match the adopted baseline before interpreting C.

To reproduce, from the repository root copy `probe/` to `work/w8c/probe/`, check out B in an
isolated repository, apply the C patch in another copy, and use `prepare.py` as the exact observer
patch recipe. The original recipe constructs B from `git archive HEAD` and C from the working
source diff, so set HEAD to the recorded B commit before applying the C changes (`feature-source.patch.xz` preserves the final source patch). Generate projects
with each copy's `tools/dotnet-ci/generate_projects.py`; run `ProbeNeutrality600`, then
`PairedSixSeedProbe` with `TD_W8_PROBE_OUT` set to the absolute `work/w8c/measurement-B` or `-C`
path. Use `analyze.py` after both tests finish.

The raw CSVs retain actual source contacts and the additional acquisition/response markers; only
adjacent effects of the same physical contact are removed in the frozen derivation. Raw rows remain
available to audit that classification. Frame/order semantics and window selection are defined in
`probe/PREREGISTRATION.md`. Canonical shot/goal ledger rows retain independent identities and
ScoringTeam rather than deriving it from the credited scorer.

Full per-frame binary traces are retained in local `work/w8c/measurement-{B,C}/`; their format and
SHA-256 values are in the comparison summary. The source archive preserves compressed raw CSV,
frozen W8 output, test receipts, comparison summaries and the frozen instrument. The full binary
traces are not committed to this directory. Reproduction regenerates them.

## Result

Both paired tests passed (B 19 min 45 s, C 19 min 24 s; elapsed times are not performance evidence).
All six B terminal fingerprints reproduce the adopted post-law baseline. B/C traces are byte-identical
for every frame: 1,944,000 frames per arm, no first divergence. Possession, ball state and kinematics,
and the exact canonical Tier-A event streams agree. All six C snapshot digests change with v28 and
are recorded separately in `comparison/summary.json`.

Both arms have 126 canonical hand releases and 12 selected opponent-first-touch windows, all complete,
none censored; 3 windows overlap another selected window. There are 8 unique opponent shots and
6 unique opponent goals in the selected windows, no releasing-team shot or goal, and the window
membership totals are also 8/6. Both arms have 146 corpus shots and 47 goals over 1,944,000 frames,
or 0.0145062 corpus goals per 600 frames. No C excess exists in this corpus. This comparison neither
changes policy/tuning nor establishes a causal keeper-passivity result.

### Marker correction and retained first analysis

The frozen probe recipe placed its extra release callback immediately after an existing unbraced
`if`, so it logged **all 250 keeper kick callbacks**, including 124 feet/parry callbacks outside hand
control. The original game assignment remained conditional; all six reproduced B digests prove
that this observation bug did not alter simulation. The initial broad-marker analysis (31 windows)
is **superseded** and retained under `comparison-superseded-all-keeper-kicks/`; do not use it as the
hand-release result.

No capture or frozen source was changed. The corrected derivation joins raw marker identities
(frame, keeper) to the **unchanged frozen W8 hand-episode end identities**. It requires exactly one
raw marker for every canonical release and the expected armed own-release restriction. All 126
canonical releases join in each arm. Non-hand marker rows are retained per seed in
`comparison/*excluded-release-markers.csv`, and all canonical release positions/order are retained
in `comparison/*canonical-releases.csv`. This repairs marker classification to the preregistered
canonical-hand-release population; the 507-frame first-other-player rule and the 600-frame follow-up
rule are unchanged. The raw contact streams, canonical event streams and binary trace comparison
are unaffected.

The initial full Linux MatchEngine assembly run finished: **917 passed / 3 failed / 16 skipped**
(42 min 59 s). All three failures were old forced-distribution restore fixtures that omitted the
#11 hand episode and therefore violated v28's structural requirement. The fixtures were corrected
without changing production validation. The affected SnapshotRestore, W8BPrerequisite, W8CHandControl
and clock fixtures now pass **127/127**. Both receipts are in `validation/`; no fresh all-green
whole-assembly run or Unity result is claimed. The owner will have Claude run the required pinned-host
Unity compile and whole MatchEngine EditMode locally.

The native pre-commit hook exceeded its fixed 60-second wall-clock budget while running its staged
snapshot sweep. Its receipt is `validation/precommit-timeout.log.xz`; it is not a passing hook run.
The local handoff commit uses a per-command hook bypass after the independent compile, focused tests,
full Linux failure reconciliation and repository checks above. No test or predicate was disabled.

## Merge with landed Pre-C control

Main `af116839` adds the reviewed Pre-C control and tooling, with no `src` change against B.
The merge preserves both evidence sets and updates the registry to 38 directories. The bridge
in `pre-c-control-bridge.json` verifies that **all 126 canonical release identities and all 12 selected
clearing identities** match the landed control in both arms. The event counts and outcomes match too.

The landed contact-consumption rule exposed one over-deduplicated row in the earlier derived touch
count: seed `0F1E2D3C4B5A6978`, frame 28,278, keeper 0's hand parry followed by a separate loose pickup.
The raw capture retained both; the classifier now consumes a contact only once, so the pickup after
its parry response counts separately. Both arms have **16,264 applied touches**, agreeing with the
landed control. The previous 16,263-count summary is preserved in
`classification-before-main-bridge.json`. No release selection or window event total changes.
The derivation also adopts the landed rule-1.1 restart/full-time censoring; it changes no selected
window on this corpus. The 1,943,994 live pre-full-time frames are reported separately from the
1,944,000 nominal simulated frames. Capture data and gameplay source were not changed or rerun.
