# W12 durable evidence manifest

**Date:** 2026-09-14  
**Purpose:** Preserve the exact GitHub Actions artifact bytes used by the W12 static sweep and the corrected pre-/post-PR #398 gate-firing comparison. GitHub artifact retention is 90 days; this directory is the durable repository copy.

## Verification rule

The committed `.zip` files below are the exact archives downloaded from GitHub Actions. Their SHA-256 values therefore match the GitHub artifact digest directly. The per-file hashes below were computed from the extracted archive contents and allow an extracted copy to be checked independently of ZIP container metadata.

No table in a tracking document is authoritative over these raw files. Where a derived tracking document disagrees with the raw census, the raw file plus these hashes wins and the derived document must be corrected.

## Static unread serialized/snapshot field sweep

- Workflow run: `34803051927`
- Artifact id: `10332485025`
- Artifact name: `w12-static-unread-field-sweep-34803051927`
- Workflow head: `b63eb3c68a4ae2b70d776fb6b660b8763bd80a96`
- Original artifact expiry: `2026-12-13T03:34:17Z`
- Committed archive: `w12-static-unread-field-sweep-34803051927.zip`
- GitHub ZIP SHA-256: `c1e3526987793b8d736fad3f52e983982a8d10d483215a4ebbcb0f44bdef94f6`

Extracted file SHA-256:

| File | SHA-256 |
|---|---|
| `report.json` | `c542a5f4b8596bc8c62120e39c709e3d9e766038527092e0cb21e8f9097a4859` |
| `report.md` | `50fafdcd92459cc89408de89401c42345c2fc6c2bb57f82bffda37aec25295bd` |

## Corrected pre-PR #398 W12 measurement

- Workflow run: `34844425733`
- Artifact id: `10347159738`
- Artifact name: `w12-corrected-pre398-34844425733`
- Workflow head: `c379788c2c96f1f49034c8fdb7dc54f487f9cf5f`
- Original artifact expiry: `2026-12-13T12:37:23Z`
- Committed archive: `w12-corrected-pre398-34844425733.zip`
- GitHub ZIP SHA-256: `0d65be3a1b808933a58f785d7e65c321ae5974626089e2c0a49cfe3c00b5231c`

Extracted file SHA-256:

| File | SHA-256 |
|---|---|
| `instrument-output.txt` | `66dfe2604e963b3d5569eed5ff10a14d3216cf9042fe182ca86e2e7c8a8adebc` |
| `measurement.txt` | `0ad04132db4e707f65332d318c0be12e7220a8a143b3a6530d5bb096fb630f15` |
| `measurement.runsettings` | `879ed0b251e93f41612861a56d087307a1e6f2cc6299b03cd8c9bebad6a709ec` |
| `trx/measurement.trx` | `05e14c82fc6e8a785b3b08a2f2d980170f1e8bd71733f064d1b371736f120ac8` |

## Post-PR #398 W12 measurement

- Workflow run: `34847990460`
- Artifact id: `10349033198`
- Artifact name: `pr398-post-w12-34847990460`
- Workflow head: `0821bfa9ff66b24393014eacf0401904fdc0fb37`
- Original artifact expiry: `2026-12-13T13:14:13Z`
- Committed archive: `pr398-post-w12-34847990460.zip`
- GitHub ZIP SHA-256: `691efe7a3c77ac1017ed86a78dcfea384a12fcd25248ade4090cbb59a48fc73d`

Extracted file SHA-256:

| File | SHA-256 |
|---|---|
| `instrument-output.txt` | `8c3764765968e74d63cfb232815ed6a72b4b3789bed07de9757327adf1cf8655` |
| `measurement.txt` | `9e8c5bc8ae2698a3989cf6e2b750e489069396872d6c6ba3a95ce40928ef7ab1` |
| `measurement.runsettings` | `879ed0b251e93f41612861a56d087307a1e6f2cc6299b03cd8c9bebad6a709ec` |
| `trx/measurement.trx` | `6bea0eb6a357ab20f7e505fb5a62e232d3036b4ad0ee7034c2f725aed4bad7a5` |

## Machine-readable census and enforcement

`w12-gate-firing-census.json` is the machine-readable pre/post census salvaged from
`wiring/w12-evidence-repair` and reconciled to the exact ZIP archives above. Its rows,
aggregates, archive hashes, and static-sweep member hashes are verified by
`tools/dotnet-ci/check_w12_evidence.py`.

The dedicated `.github/workflows/w12-evidence-check.yml` runs that verifier whenever
the W12 evidence, comparison document, checker, or workflow changes. The obsolete
Base64-split raw bundle and duplicate root-level checker from the stale branch are not
restored because the exact GitHub artifact ZIPs and the migrated checker on current
`main` supersede them.

## Provenance boundary

The corrected post-#398 comparison is derived from the committed post-run archive above. The workflow run and job log corroborate that archive and its GitHub digest. The temporary workflow branch is not itself treated as the durable evidence source.
