# W13 frozen-lane results and §10 capture checkpoint

**Created:** November 13, 2026.

**Purpose:** Retain the completed frozen-lane identity proofs and a bounded checkpoint of the running armed-marking experiment.

**Checkpoint:** November 13, 2026, 16:11:07 PST (`2026-11-14T00:11:07Z`).

**Status:** Identity comparisons PASS; §10 captures IN PROGRESS. No M1–M8 conclusion, calibration, default flip, merge approval, or Unity certification is claimed.

## Completed comparisons

| Comparison | Result |
| --- | --- |
| Historical control `1d3cebf3`, six seeds, 324,000 calls each | All six adopted v27 terminal fingerprints reproduced |
| Post-W13.0 `f64729d2` vs W13 `4171d3d1`, marking off | All 1,944,000 per-tick digests equal; six terminal digests and all 6,000-tick checkpoints equal |
| Exact #543-on-main reference `64592405` vs integrated W13, marking off | Six full C0 pairs equal on all eight retained streams: raw/aligned digests, trajectory, ledger, team-possession, geometry, marking and mark-input witness |
| Corrected §10 preflight | 36 runs passed: structural mutation checks, restore/repeatability proofs, C0–C9 short source/activation pilots, and short observer controls |
| Full §10 C0 controls | All six seeds' 114 stream comparisons passed; each armed C0 differs from its off control |
| Focused reference tests | 44 passed, zero failed/skipped |
| Focused integrated-source tests | 85 passed, zero failed, one existing env-gated identity-probe skip |

The frozen **Linux measurement lane** is Debian 13 x64, kernel 6.18.57, SDK 8.0.426, CLR 8.0.32, Debug/optimization off, DEVELOPMENT_BUILD, default JIT settings, and `DOTNET_PROCESSOR_COUNT=4`. The host executable, all 184 CLR files, OS and kernel match the retained lane receipt. One seed runs per process, with at most two full captures concurrently. This is separate from the governing Windows/Unity certification host.

## Source identities

| Input | Commit | `src` tree |
| --- | --- | --- |
| Historical control | `1d3cebf3b8d874715158067d9deb5fa48b309a30` | recorded in `identity/identity-prepared.json` |
| Post-W13.0 identity baseline | `f64729d2f2a20a2c6c96f4ce26df80844c9571d4` | recorded in `identity/identity-prepared.json` |
| W13 identity candidate | `4171d3d1e3f07a661e085ffa6f51770384af92c8` | `447828bc7f7a50264bf9076d700bba32a82bbfb1` |
| Owner-provided §10 reference | `645924057694fd9aa8bf2ab33e43a5810d3d3feb` | `249ce15b33879717a774b1437a1147d28d7918b4` |
| Isolated §10 source integration | `96684cbc4ced594da581ef2e31ce955f44c38f48` | `95052885a34fe99c4de41f157b6253bc5bee72fc` |

The measurement-only integration has both `4171d3d1` and `64592405` as parents. Runtime conflicts were comment/history hunks in `MatchEngine.cs` and `MatchEngineConstants.cs`; both sides were retained and the #543 history rows renumbered. It introduces no gameplay fix or tuning. `section10/integration-source.json` and `source-integration.patch.xz` preserve the resolution.

**This branch is not a landing merge candidate.** The source integration deliberately retains the W13 parent's documentation snapshot; #543's landing documentation has not been reconciled. The evidence registry is reconciled to the directories actually present in this measurement tree. `main`, the owner-provided reference and the W13 branch are not advanced by this publication.

## Running §10 experiment

The frozen batch contains **138 full captures**:

- 66 required W13 captures: six C0 marking-off matches and 60 armed C0–C9 matches;
- 12 additional plain runs for observer-neutrality controls;
- 60 fresh reference matches, providing an unconfounded post-W13.0 before arm for the same cases.

At this checkpoint **32 full captures** are complete and retained, including seven armed observed matches. The controller has entered the paired C1–C9 matrix. The two running captures listed in `checkpoint.json`, and all later results, are excluded from this commit. The worker continues in the execution workspace; this commit is an immutable checkpoint, not a live status feed.

Activation uses the real config key `match-engine.MarkMovementEnabled`, bound before the first engine/catalogue use. No per-engine test override, gameplay patch, squad change, tuning, callback, or RNG draw is added by the observer. C0 observer controls compare every raw digest, trajectory, ordered ledger, possession witness and geometry record. Every observed capture also independently verifies snapshot preimages and previous links, reconciles ledger totals with the summary, and retains source/binary identities and nonempty BuildHash.

All football metrics remain descriptive. M1–M8 analysis, the complete paired matrix, stand-off calibration and owner review remain outstanding. Recorded W13-F5–F10 issues are not closed by this checkpoint.

## Instrument correction and attempt accounting

The initial instrument incorrectly joined a cached 10 Hz input to post-Resolve possession and treated that extra join as an eligibility assertion. It stopped C9/seed `1` at frame 384: away player 18 gained possession after AI composition while marker 16's input still carried a target for opponent 8. The flag clears at the next AI stride, frame 390.

An independent read-only reproduction repeats all 420 traced digests exactly and matches every one of the stopped run's 384 digests. This establishes a sampling-phase mismatch in the instrument. The approved plan §4.2 reads possession once at `RunMechanicsAI` entry; §7 defines the context as a per-stride input. Corrected eligibility checks use those same composition snapshots. Post-Resolve possession/inactive-agent lag is preserved separately, at every physics tick, in `retained-lag.bin` and descriptive counters. No owner approval or waiver is claimed for this interpretation; the earlier clarification question was withdrawn after the phase mismatch was established.

Every initial failed/superseded probe and the reproduction is preserved in `attempts/`; none is relabeled as passed or counted as a completed full capture. Corrected preflight receipts are preserved separately. The 32 current full captures exclude all those probes.

**Controller startup caveat:** a duplicate controller invocation failed closed on the existing preflight directory and launched no captures. Its shell redirection truncated the shared aggregate stdout logs, and it rewrote `measurement-freeze.json` with the same input hashes and a later timestamp. Per-run raw streams, command/exit receipts, summaries and audits are intact and authoritative. The later JSON timestamp is not presented as the initial freeze time. Details and filesystem birth/modified times are retained in `section10/phase-aligned/controller-startup-accounting.json`; the incomplete aggregate logs remain in the attempts archive.

## Retained bytes and reproduction

`SHA256SUMS` is the complete tracked-file contract. Large `.xz` payloads use Git LFS. Materialize them before inspecting streams:

```bash
git lfs pull --include 'docs/tracking/evidence/w13-frozen-lane-20261113/**/*.xz'
python3 docs/tracking/evidence/w13-frozen-lane-20261113/verify_checkpoint.py
```

Every completed run is stored as `traces.tar.xz` plus `trace-index.json`. The unchanged repository `tools/dotnet-ci/retained_trace_store.py` verifies every decoded raw stream before the copied gzip originals are removed. Raw bytes, IEEE float bits, event order and per-tick digest arrays are unchanged. The index records raw hashes/lengths, archive hash, codec and original gzip-container hashes. Existing `files-SHA256.json` receipts describe the **as-run** containers; the index describes the published lossless store. Analysis must use `open_trace(folder, name)` to support both layouts.

The 62 completed stores are individually decoded and verified while packaging; `publication/packed-stores.json` records the result. Attempt archives have member-level hashes alongside their archive hashes. No active producer directory is packed or modified.

`identity/` and `section10/phase-aligned/` retain the exact as-run runner, preparer/controller source, configuration and identity receipts. Their absolute paths are historical execution paths. `rebuild.py` is a separate portable preparation entry point; it does not masquerade as the old hash-pinned scripts:

```bash
python3 docs/tracking/evidence/w13-frozen-lane-20261113/rebuild.py \
  --scope identity --repo "$PWD" --work-dir /tmp/w13-rebuild \
  --dotnet /path/to/dotnet-8.0.426/dotnet --packages /path/to/nuget-cache
```

Use `--scope section10` in a different empty work directory for the reference/integrated capture binaries. The prepared commands and original frozen protocols define the cases and seeds; no historical result is re-adopted as a new baseline. Rebuilding may produce different module identities at different source paths; preserve the new identities rather than claiming the retained binary hashes.

## Publication checks

The official cold staged-index pre-commit composition passed across 37 assemblies: **4,623 passed / 0 failed / 206 console skips**. The final delta from that validated index contains archival-format/readme/receipt changes only; production and tooling/test inputs are unchanged. Integrity, tooling and whitespace checks cover the final publication. The completed composition is reused for the metadata-only commit instead of rerunning it through the hook. Its documented cold-preparation mode does not enforce or certify the 60-second acceptance bound. Receipts and the exact validated tree are in `publication/validation.json` and `publication/checks/`.

The as-run controller additionally checks non-vacuity of armed observed captures. This is an instrument guard, stricter than §10's stated stop list; a zero-target capture would require review and would be retained, not tuned or silently rebaselined. It is not an approved football-realism acceptance band.

## Version history

| Version | Date | Notes |
| --- | --- | --- |
| 1.0 | November 13, 2026 | Completed frozen-lane identity proofs, 32-capture §10 checkpoint, full observer controls, source/protocol/attempt accounting and verified lossless trace stores. |
