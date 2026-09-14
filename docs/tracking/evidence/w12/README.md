# W12 durable evidence

This directory makes the September 14, 2026 W12 evidence independent of GitHub Actions retention. The durable sources are committed bytes plus the SHA-256 values below; workflow run metadata and GitHub artifact digests are provenance corroboration.

## Static unread-field sweep

- Workflow run: `34803051927`
- Head: `b63eb3c68a4ae2b70d776fb6b660b8763bd80a96` (`wiring/w12-gate-firing-diagnostic`)
- GitHub artifact: `w12-static-unread-field-sweep-34803051927` (artifact `10332485025`)
- Committed artifact ZIP: `w12-static-unread-field-sweep-34803051927.zip`
- GitHub ZIP SHA-256: `c1e3526987793b8d736fad3f52e983982a8d10d483215a4ebbcb0f44bdef94f6`
- Extracted `report.json`: 53,109 bytes, SHA-256 `c542a5f4b8596bc8c62120e39c709e3d9e766038527092e0cb21e8f9097a4859`
- Extracted `report.md`: 48,401 bytes, SHA-256 `50fafdcd92459cc89408de89401c42345c2fc6c2bb57f82bffda37aec25295bd`

The ZIP is the exact downloaded artifact archive, not a re-created ZIP.

## W12 gate-firing raw text

The four `w12-raw-text-evidence.tar.xz.b64.part-*` files concatenate to a Base64 representation of one `tar.xz`. Decode and extract it to recover these exact artifact members:

| Run | File | Original SHA-256 |
|---|---|---|
| pre `34844425733` | `pre/instrument-output.txt` | `66dfe2604e963b3d5569eed5ff10a14d3216cf9042fe182ca86e2e7c8a8adebc` |
| pre `34844425733` | `pre/measurement.txt` | `0ad04132db4e707f65332d318c0be12e7220a8a143b3a6530d5bb096fb630f15` |
| post `34847990460` | `post/instrument-output.txt` | `8c3764765968e74d63cfb232815ed6a72b4b3789bed07de9757327adf1cf8655` |
| post `34847990460` | `post/measurement.txt` | `9e8c5bc8ae2698a3989cf6e2b750e489069396872d6c6ba3a95ce40928ef7ab1` |

The reconstructed `w12-raw-text-evidence.tar.xz` must hash to `a1c26890dc7484f59caee0d10a1fe8d7523fced3221d7f5e0d32a471760f07c6`. The checker in `tools/check-w12-evidence.py` performs that reconstruction and all per-file checks automatically.

### Pre-#398 provenance

- Run `34844425733`
- Commit `c379788c2c96f1f49034c8fdb7dc54f487f9cf5f`
- Ref `refs/heads/wiring/w12-gate-firing-diagnostic`
- GitHub artifact ZIP SHA-256 `0d65be3a1b808933a58f785d7e65c321ae5974626089e2c0a49cfe3c00b5231c`

### Post-#398 provenance

- Run `34847990460`, job `103988507476`
- Commit `0821bfa9ff66b24393014eacf0401904fdc0fb37`
- Ref `refs/heads/tmp/pr398-post-w12`
- GitHub artifact ZIP SHA-256 `691efe7a3c77ac1017ed86a78dcfea384a12fcd25248ade4090cbb59a48fc73d`

`w12-gate-firing-census.json` is a mechanical parse of the first complete census block in the committed raw `instrument-output.txt` files. It is checked back against those source bytes in CI.
