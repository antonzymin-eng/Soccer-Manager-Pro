# W8 B local Linux evidence

**October 16 post-law update:** [rehandling/](rehandling/README.md) records owner-approved law plus
feet-only avoidance at `751b1143`, its original-head regression failures, repeated corrected-B control,
and the new six-seed post-B Linux anchor: **126 distributions / 114 receiver controls / zero own-keeper
hand returns / zero restricted attempts or sanctions**. The corrected B results below remain pre-law.
Final-head CI and Unity host validation remain required.

**Created:** October 15, 2026 (America/Los_Angeles)
**Purpose:** Preserve step-7 implementation proof and the isolated October 13 A→B comparison.
**Status:** Corrected-arm measurement and test reconciliation complete. Prior-head `353b2138` PR functional CI passed (run 38841462964; Unity skipped). Review-cleanup final-head CI and Unity validation remain pending.

The governing contract is the September 26 owner-approved W8 B amendment. The October 13 anchor is
`3974b78b0383adca3e9546bb47a7b93a62b3cf92`; the substitutions bridge is
`b60cc532254862246c9fc2622dbf2c99d8b787c5`. Fresh host and bridge reports are retained here.
The current SDK 8.0.426 / runtime 8.0.32 reproduces the archived runtime-8.0.31 anchor's entire common
report and all six terminal digests exactly. The bridge changes only the schema fingerprints in
that report. All simulations are Linux x64 Debug, 324,000 ticks per frozen seed, normal constructor,
`EnableGkHeading()`, no squad override. The exact common-instrument blob is
`520ba31c71e06ed408fd5c282b5fd5611cf74879`.

## October 17 measurement closeout

[Measurement merge gates](measurement-closeout/README.md): exact frozen observer on Linux .NET,
twelve matching arm fingerprints, 348/348 → 126/126 claims/distributions and all 126 post-release
restricted spells. Twelve opponent-touch windows contain eight shots and six goals; no causal
balance claim. Governing Unity evidence and final-head CI remain distinct.

## Arms and provenance

- `first-arm/`: completed but rejected implementation. Common/foul pin `ed0570cf`; detailed pin
  `db7c866e`, production-identical. Its 342 dedicated distributions coexist with 18 ordinary foot
  passes already in windup before hand claims. That violates the approved exclusive handover rule.
  Raw reports, TRX, source/environment/time provenance and derived CSVs remain intact.
- `corrected-arm/`: 348 hand claims join exactly 348 dedicated CONTACTs / events / releases / W5 feeds. No ordinary hand-origin release remains outside that path; no legacy drop or new DT race is observed. Same-keeper hand interruption is 155 versus A’s 24 and stays a separately reported confound.
- Corrected B source: `e54335b051ed51a0c3c394ccdaa016cb8ae28245`, src tree
  `4a9061afbf7dd43cef5451294a2857ae902d739b`. It safely cancels a pending ordinary PASS/SHOOT in
  Resolve before CONTACT when hand control is live. No policy, recovery, claim or physical tuning.
- Supplemental per-keeper A pin `08d104f5` (production identical to the bridge) and B pin `5f32e253`
  (production identical to corrected B). Both add the identical archived `.cs.txt` observer only.
  This attribution supplement was added after the first arm because the frozen instrument pools
  pass outcomes by seed. It introduces no threshold or policy change. Each observer passes a
  600-tick digest-chain neutrality case; all six terminal digests and all pooled hand/feet outcome
  counters must agree before its per-keeper data is used. These checks pass in both arms;
  `keeper-outcome-comparison.csv` contains all 48 keeper/kind/arm rows and
  `keeper-pass-outcomes.csv` retains all individual hand/feet pass outcomes.

`production-equivalence.txt` records checked source differences. Every result-bearing run has a
TRX, console output and source/environment/time record. An interrupted attempt has no completion
record and is never counted as a pass. The three corrected-arm measurements and the earlier full gate were
interrupted during the approval-review usage failure; their status is recorded separately.
The initial five standalone #5 fixture failures came from missing EventBus registration after the
canonical PassAttemptEvent became live. Fixture setup was corrected without weakening assertions.
`development-attempts/` also retains the initial replay-harness and evidence-staging failures.

## Analysis and reproduction

Raw TRX and console files are compressed with xz. Derived CSV files use LF line endings for Git
checkout stability. SHA256SUMS covers every tracked file here except itself; it is regenerated after
final capture and verified against the repository's registered full-coverage contract.

The frozen corpus exercises SlowDown only. The other five policies are composed-test evidence.
LongKick rows named `composed-LongKick-team-*` are separate fixtures, excluded from frozen totals.
A first-ground observation is not controlled reception: outcome tracking continues after landing,
and control / kick / restart before landing is recorded as a censor. Reclaim-confound populations
are kept separate from actual receiver completion and from prior-own-pass-preceded claims. The
raw `guardMargin` is 360 minus hand elapsed frames; it does not include prior feet-possession
time in the engine guard. The live producer uses the actual engine counter; composed tests enforce
that separate deadline. Corrected B’s minimum observed margin to #11’s boundary is 126 frames.

From this directory, `PYTHONDONTWRITEBYTECODE=1 python3 compare_w8.py first-arm` and
`PYTHONDONTWRITEBYTECODE=1 python3 summarize_detail.py first-arm` reproduce the first-arm exports.
Use `corrected-arm` instead of `first-arm` for corrected B (also the no-argument default).
One copy of each analyzer is covered by SHA256SUMS; there are no per-arm script copies. Run
`PYTHONDONTWRITEBYTECODE=1 python3 compare_keeper_outcomes.py` for the fully reconciled A/B
per-keeper tables. The parsers accept either plain or xz raw files. As-run shell scripts preserve the cloud's absolute
paths and toolchain activation; they are provenance, not portable setup scripts. To repeat the
supplement on another host, apply `W8PerKeeperPassDiagnosticTests.cs.txt` as a test-only overlay to
the pinned checkout, generate projects with `python3 tools/dotnet-ci/generate_projects.py`, and run
its named method with `TD_W8_PER_KEEPER=1`. Use the recorded Debug/runtime configuration and verify
digests against the common run before treating its rows as comparable.

See [implementation, acceptance matrix and measured interpretation](../../w8-b-implementation-results.md).
Linux shim evidence is supplementary. Unity 6000.4.9f1 compile / required host tests remain not run;
this archive does not establish a passing Unity gate or merge readiness.

## Final validation disposition

The full 37-assembly policy invocation at e54335b0 exits 1: 4,032 cases pass, one claim-window
observer fails, and 225 are recorded NotExecuted (including explicit deferrals). `claim-probe/`
isolates a legitimate same-keeper free kick, not a physically detached held ball. The corrected
observer and all original attachment / own-goal predicates pass in the integrated 165-case retest.
A final-source 164-case build/run also passes; 346 tooling cases pass. `test-reconciliation.json`
retains the original failure and maps its focused pass plus the two new cases, yielding 4,035
passing current case identities and 225 NotExecuted. This is not a clean single-invocation gate;
fresh CI and the required Unity host remain pending.

Measurement-closeout source commit is 86a9496e (src 74b215f71809d50f483e4ab2cb254692e3c53c75). Production
statements, fields and constant values are unchanged from the measured e54335b0. The final
production-source delta is comment-only (`final-production-comments.diff.xz` and its checked proof);
the executable change is confined to the test-only observation fix. No new measurement arm or
numeric adjustment is inferred from that documentation / observer cleanup.

`pr-gate/` preserves all raw TRX, with the failed driver log alongside. Review cleanup on
October 16 omitted 72 empty coverage XML stubs: each has zero valid/covered lines and branches,
no packages/classes, and no coverage evidence. `empty-coverage-inventory.csv` preserves their
original paths, compressed/raw hashes and XML metadata; original bytes remain at commit 353b2138.
Review source cleanup is described in the implementation report; no measured runtime is replaced.
Coverage collection is not a claim that the separate FR-TS-057/058 tier/delta audit was performed.
The complete evidence archive is self-contained for analysis; run `reconcile_tests.py` here to
reproduce the case reconciliation. Source material and exact raw runtime provenance remain distinct
from that derived status.

## October 16 review checks

`review-cleanup/` retains the 99-case goalkeeper and 164-case W8 integration retests, source-tree/
file hashes, analyzer equivalence, metadata/doc checks and prior-head CI job metadata. No gameplay
measurement is rerun or inferred from the branch/telemetry-name cleanup.
