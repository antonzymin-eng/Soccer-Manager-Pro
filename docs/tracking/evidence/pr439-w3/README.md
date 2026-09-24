# PR #439 W3 Durable Evidence

> **Created:** September 23, 2026
> **Purpose:** Preserve the result-bearing W3 / W6-corrected frozen-corpus evidence and Heading #10 reachability localization before disposable `evidence/pr439-*` refs or Actions artifacts are removed or expire.
> **Production status:** PR #439 landed on `main` as merge `e62f2fd215b03419664e2405b7f8f99354257498`. W3 is **wired but dormant**: the keeper Hand path is live, while production Head participation remains dormant upstream and is tracked by issue #441.

## Canonical W3 corpus

`canonical/` is the landing-head result set from Actions run `35925236129` on evidence head `3f391fed45795a9c4f22bc601e187dd2cbe0e39a`. Its harness checked out production SHA `bab4cd41b232cbbe4820279e7d456c505bdab77b` and ran the unchanged frozen six seeds.

Aggregate W3 counters are `agentBallFanoutEvents=215083`, `claimEligibilityEpisodes=1297`, `registeredDuelParticipants=1164`, `resolvedHandContactDuels=1164`, `successfulKeeperClaims=753`, and `headerContacts=0`.

The matched earlier Actions run `35814050060` on production SHA `f40f0853909cc2a42190023fea1da72d25409b12` produced byte-identical `results.tsv`, `results.json`, `report.txt`, and `measurement.runsettings` in the source artifacts. `source-artifact-hashes.tsv` preserves the SHA-256 of every source-artifact payload for both runs, including the two distinct TRX files and full console captures. The canonical per-seed TSV is retained here; SHA-256 identities for both source artifacts, including their full reports and TRX files, are recorded in `source-artifact-hashes.tsv`.

## Heading #10 reachability

`header-reachability/results.tsv` is reconstructed directly from the six successful job logs of run `35887487201` on evidence head `7494300341cae94ed3eae41033d874e23cac0b06`.

Aggregate: 1,811 committed `HeaderIntent`s; 1,811 jump starts; 18 intents ever assigned a predicted contact frame; **0 prepared Head contacts**; **0 executed headers**; 1,793 `PositionedPoorly` terminal failures; 16 `MistimedEarly`; one active-intent overwrite; zero landing drops; zero explicit cancels. The same run records 1,164 W3 participants/Hand resolutions and 753 keeper claims.

This localizes the first complete zero downstream of header commitment/jump initiation and upstream of W3 Hand-vs-Head arbitration. It does not authorize a geometry or `[GT]` change.

## Integrity and ref retention

`SHA256SUMS` covers every committed file in this evidence directory except `SHA256SUMS` itself, including this README. `provenance.tsv` pins run IDs, evidence heads, measured production SHAs, artifact IDs where available, and artifact expiry.

The separate `../pr439-ref-archive/` preserves the disposable evidence refs and branch-exclusive source/workflow states. Do not delete `evidence/pr439-*` refs until that archive lands and its checker passes on `main`.
