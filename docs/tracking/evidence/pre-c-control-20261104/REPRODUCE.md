# Verify or replay the Pre-C control

Created November 4, 2026. All commands run on Linux; no Unity host is needed.

To verify the retained comparison without rebuilding:

```bash
mkdir pre-c-review
tar -xJf pre-c-control-evidence.tar.xz -C pre-c-review
tar -xJf pre-c-control-full-captures.tar.xz -C pre-c-review/pre-c-control-20261104
cd pre-c-review/pre-c-control-20261104
sha256sum -c SHA256SUMS
python3 -m unittest -v test_selection test_evidence_checks
python3 analyze.py
```

`analyze.py` must report `passed: true`, six adopted fingerprints in both arms,
equal full streams and complete publish/ledger reconciliation. It regenerates
`analysis.json` and `windows.json`. All original per-tick digest bytes are present
in the full capture archive. The smaller `capture-traces.tar.xz` inside the evidence
archive omits only those redundant digest arrays; their SHA-256 values remain in
the retained comparison report.

To run the retained binaries with .NET **8.0.32** installed:

```bash
mkdir pre-c-binaries
tar -xJf pre-c-control-binaries.tar.xz -C pre-c-binaries
dotnet --fx-version 8.0.32 pre-c-binaries/observed/capture/bin/Debug/net8.0/TacticalDirector.MatchEngine.Tests.dll --self-test
dotnet --fx-version 8.0.32 pre-c-binaries/plain/capture/bin/Debug/net8.0/TacticalDirector.MatchEngine.Tests.dll 0F1E2D3C4B5A6978 plain replay-plain
dotnet --fx-version 8.0.32 pre-c-binaries/observed/capture/bin/Debug/net8.0/TacticalDirector.MatchEngine.Tests.dll 0F1E2D3C4B5A6978 observed replay-observed
cmp replay-plain/digests.bin replay-observed/digests.bin
cmp replay-plain/trajectory.bin replay-observed/trajectory.bin
cmp replay-plain/ledger.bin replay-observed/ledger.bin
```

Use a fresh output directory for each run. Repeat for the six seeds in
`expected.json`. The as-run environment set `DOTNET_PROCESSOR_COUNT=4`, with all
selected JIT overrides unset; see `freeze.json` for exact runtime, CPU, source and
binary identities. A different runtime or machine requires reproducing the B
control before interpreting C.

For a source rebuild, `pre-c-control-source.tar.xz` is the unmodified pinned Git
source archive. The evidence archive supplies the exact frozen W8 observer,
harness source, overlay and preparation scripts. Follow the README reproduction
notes and adapt explicit scratch paths. Use SDK **8.0.426**, Debug, optimization
disabled, DEVELOPMENT_BUILD and the exact package versions. Keep C changes in a
separate source copy and retain this control unchanged.
