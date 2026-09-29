# PR #420 owner-held execution-isolation evidence

> **Recorded:** September 19, 2026
> **Purpose:** Durable repository record of the exact executed-result topology that satisfied the first half of the gate-integrity closure criterion. This record does **not** claim to identify the historical pinned-main leakage mechanism.

## Provenance

- PR: #420 `test: prove owner-held RED execution isolation`
- Exact PR head: `2e12fa3e778a82c1afd6a2078ee29f8f7321f58d`
- Base/main used by that run: `c470eace951057d73013c7779fb2f0f2851c3662`
- CI run: `35420947427`
- Functional job: `105838417049`
- Source Actions artifact: `pr-functional-executed-tests`, artifact id `10578621101`
- Source artifact size: `1,106,934` bytes
- Source artifact SHA-256: `8a441096b7e4528d282b6d33388d96c03804560b730c97225c72bfa3fa771570`
- Durable archive: `docs/tracking/evidence/pr420-owner-held-isolation/pr-functional-executed-tests-10578621101.tar.xz`
- Durable archive size: `472,056` bytes
- Durable archive SHA-256: `f8a37e7c0baa5630cab4e3f2fe11899ae67a68ae56b9502aed22eeab80bdbd1c`
- TRX member manifest: `docs/tracking/evidence/pr420-owner-held-isolation/TRX-SHA256SUMS`
- TRX member-manifest SHA-256: `484a92178ba5a385f355a78db464be620f7e32d9ee43e6c4136cb342c0039c67`
- Normalized executed-result census: `docs/tracking/evidence/pr420-owner-held-isolation/executed-results.tsv`
- Normalized TSV SHA-256: `846d72d766fe834afb11c4ccff52bf31ceb8a38874260030a9c4264b1e37cea7`
- Payload manifest: `docs/tracking/evidence/pr420-owner-held-isolation/SHA256SUMS`
- Bootstrap/verify tool: `tools/dotnet-ci/pr420-evidence.py`
- Archive bootstrap toolchain used: GNU tar 1.35; XZ Utils 5.8.1
- Merge commit: `0ed3a5d95fd85426af7f31c9eb0b9764dcca65ee`

The committed archive is a normal Git blob, not Git LFS. It is a deterministic recompression of the 70 TRX members from the pinned source artifact; the source ZIP itself is not the committed container. `TRX-SHA256SUMS` freezes every source member by relative path and SHA-256, so the durable proof survives recompression and Actions expiry. `SHA256SUMS` verifies the committed archive, the 70-row member manifest, and the normalized TSV.

`pr420-evidence.py bootstrap` is the only creation path: it first requires the source file to match artifact `10578621101` by both exact size and SHA-256, then requires exactly 70 safe TRX paths with 35 `coverage/` and 35 `owner-held-red/` members before emitting the member manifest, TSV, and archive. `verify` does not regenerate compressed bytes; it verifies `SHA256SUMS`, requires the archive's exact path set and member hashes to match `TRX-SHA256SUMS`, regenerates the TSV from those verified members, and byte-compares it with the committed TSV.

**Source-substitution rule:** no later #428 or other validation-run artifact may replace artifact `10578621101`, even when it has the same 3,730 / 1 result topology. Later TRX containers embed different run timestamps/durations and therefore different member paths and/or bytes; bootstrap fails closed before they can become durable #420 evidence.

## Executed-result census

The pinned artifact contains 70 TRX files: 35 under `coverage/` for the ordinary sweep and 35 under `owner-held-red/` for the dedicated invocation. Parsing every `UnitTestResult` record gives:

| Scope | TRX files | Result records | Exact `sim_match_engine_close_chance` occurrences |
|---|---:|---:|---:|
| Ordinary sweep | 35 | 3,730 | **0** |
| Dedicated owner-held invocation | 35 | 1 | **1** |

The sole dedicated result is in:

`owner-held-red/_runnervmlun5p_2026-09-19_05_10_16_net8.0.trx`

Its SHA-256 is:

`da89139d52e174b47f5f28f65a90521ffa78f0b9da7338b3e6fa4639a0e1fb01`

The result name is exactly `sim_match_engine_close_chance` and its outcome is `Passed`. That unexpected green is why the non-required functional job is red; it does not invalidate the isolation proof.

The verifier emitted, after its guards established zero ordinary matches and exactly one dedicated match:

`OWNER-HELD ISOLATION: sim_match_engine_close_chance ordinary=0 dedicated=1`

That success line is not the failure-reporting surface: an ordinary leak or duplicate/missing dedicated result exits earlier and reports the observed failing multiplicity in its error message.

## What this proves

This exact-head run proves the held test was absent from the ordinary executed-result set and appeared exactly once in the dedicated executed-result set. PR #420 also landed the mechanical assertion that fails on ordinary leakage or dedicated multiplicity other than one.

This closes the **execution-topology proof** portion of the live gate-integrity issue. It does **not** explain why pinned-main run `35133104970` previously logged the same exclusion filter while showing the 514-case ordinary cardinality. Identifying or bounding that historical runner/filter condition remains the live issue.
