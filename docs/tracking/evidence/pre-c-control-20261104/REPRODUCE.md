# Verify or rebuild the Pre-C control

Created November 4, 2026. Run from the repository root. Reanalysis requires
Python 3.12+, Git LFS and an xz-capable tar; it requires no SDK or Unity host.
The single capture archive and all current tools below are committed.

## Reproduce all recorded comparisons

```bash
pre_c_evidence=docs/tracking/evidence/pre-c-control-20261104
git lfs pull --include="$pre_c_evidence/capture-traces.tar.xz" --exclude=""
mkdir -p work/pre-c-review
tar -xJf "$pre_c_evidence/capture-traces.tar.xz" -C work/pre-c-review
python3 tools/dotnet-ci/check_evidence_manifests.py
python3 -m unittest discover -s "$pre_c_evidence" -p 'test_*.py' -v
python3 "$pre_c_evidence/analyze.py" --results work/pre-c-review/results --output-dir work/pre-c-review/analysis
python3 "$pre_c_evidence/review.py" --results work/pre-c-review/results --analysis-dir work/pre-c-review/analysis --output-dir work/pre-c-review/decisions
python3 "$pre_c_evidence/closeout.py" --analysis-dir work/pre-c-review/analysis --output-dir work/pre-c-review/tables
cmp "$pre_c_evidence/analysis.json" work/pre-c-review/analysis/analysis.json
cmp "$pre_c_evidence/windows.json" work/pre-c-review/analysis/windows.json
cmp "$pre_c_evidence/review.json" work/pre-c-review/decisions/review.json
cmp "$pre_c_evidence/policy-freeze.json" work/pre-c-review/decisions/policy-freeze.json
cmp "$pre_c_evidence/per-match.csv" work/pre-c-review/tables/per-match.csv
cmp "$pre_c_evidence/selected-windows.csv" work/pre-c-review/tables/selected-windows.csv
```

Expected: full **PASS**, six adopted fingerprints in both arms; all 1,944,000
corresponding per-tick digests, complete ball states and canonical event records
match. Every publish reconciles to the ledger exactly once. All comparisons above
are byte-identical. Use an empty output directory for a new table bundle.

The CI checker can run without downloading LFS. In that mode it verifies a strict
LFS pointer's referenced OID, only when Git attributes designate the file as LFS;
it does not claim unavailable payload bytes or remote availability were checked.
After download, the same checker hashes the actual bytes. Full reanalysis needs
the downloaded archive, as shown above. Ordinary content corruption still fails.

For an intentionally incomplete trace-only review, pass `--skip-digests` to
`analyze.py`. It reports **PARTIAL**, `digestChainsChecked: false`, `passed: null`.
Missing digests fail the default mode, and `closeout.py` refuses partial results.
This option is not needed for the complete committed archive.

## Rebuild the original B capture from source

This optional step needs Linux x64, SDK **8.0.426** with CLR **8.0.32**, Python,
Git and `patch`. `prepare.py` checks the host/CLR hashes against the original
freeze, reconstructs the source archive from its Git commit with LFS smudging
disabled, checks that archive's original SHA-256, and checks both frozen observers.
It extracts and patches scratch copies, never the source checkout.

```bash
pre_c_evidence=docs/tracking/evidence/pre-c-control-20261104
python3 "$pre_c_evidence/prepare.py" --work-dir work/pre-c-rebuild --build
python3 "$pre_c_evidence/run.py" --work-dir work/pre-c-rebuild
python3 "$pre_c_evidence/analyze.py" --results work/pre-c-rebuild/results --output-dir work/pre-c-rebuild/analysis
```

Choose a new, empty work directory. The source commit is an ancestor of this branch;
a shallow clone may first need `git fetch --unshallow`. The default build restores
the pinned package versions from the configured NuGet source. To use an existing
cache offline, add `--offline --packages /path/to/nuget-cache` to `prepare.py`.
`--repo /path/to/checkout` and `--dotnet /path/to/dotnet` are supported; all output
locations come from `--work-dir`. No original container path is required.

`run.py` checks the prepared source/binary and runtime identities before launching
one seed per process with `--fx-version 8.0.32`; two processes run by default.
A full run checks every terminal fingerprint against `expected.json`. A new
runtime or hardware lane must reproduce B before interpreting C.

For a quick wrapper/build smoke check, use `--seed 0F1E2D3C4B5A6978 --frames 1000`
with `run.py`. Those outputs go under `smoke/`, are labelled incomplete, and cannot
pass the 324,000-frame six-seed analyzer. Full capture uses all six seeds by default.

The as-run scratch scripts and original rule/results remain in `as-run-v1.tar.xz`
for provenance. They are historical records, not the commands for a fresh checkout.
Its capture member is the old committed LFS pointer. Current `capture-traces.tar.xz`
contains all original data plus the previously omitted digest arrays.
