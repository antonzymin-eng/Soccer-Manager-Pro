# PR #439 W3 Durable Evidence

> **Created:** September 23, 2026
> **Purpose:** Preserve the frozen six-seed W3 characterization data from PR #439 after the W3 landing, independently of expiring GitHub Actions artifacts and disposable evidence refs.

## Authoritative arms

| Arm | Production SHA measured | Evidence run | Evidence ref/head | Artifact id | GitHub artifact digest |
| --- | --- | ---: | --- | ---: | --- |
| pre-W3 structural comparator | `876a3343319050187c2a5505b18cb32fc3d0f89d` | `35815761065` | `evidence/pr439-w3-prewire-six-seed` @ `cf36526ce6ecd64632133780ed98ca0afe46c987` | `10732021720` | `sha256:9ce8c73a1e108bb6b06244154eda36e374e772f08cb9dcd30fbace7159a4e171` |
| original post-W3 characterization | `f40f0853909cc2a42190023fea1da72d25409b12` | `35814050060` | `evidence/pr439-w3-six-seed-20260922` @ `7347b0452d8143c5b540575b4db1f882302f1988` | `10731556057` | `sha256:5f044a005facd2274d1953e98af12e28670cbd8e68175dea7a6fb08c0bdf029a` |
| landed W3 rerun | `bab4cd41b232cbbe4820279e7d456c505bdab77b` | `35925236129` | `evidence/pr439-w3-six-seed-rerun-20260923` @ `3f391fed45795a9c4f22bc601e187dd2cbe0e39a` | `10779846684` | `sha256:f0b7978e36ae64074eed9cc198b6629af878eda40b7885f416adf1cb6247f1b4` |

The original post-W3 and landed-rerun `report.txt`, `results.tsv`, and `results.json` are byte-identical. Their logs/TRX are not asserted byte-identical. The final rerun is the durable post-W3 table copied here because it measures the production head used for the landing closeout.

## Supporting (non-authoritative) arms

The pre-W6 attribution arm is preserved here as supporting evidence, not added to the authoritative-arm table and not added to `pr439-ref-archive/run-heads.tsv`. Run `35815667045` measured production SHA `f69aaef0f27fdd8a10a89a2d4cc599b08d6ef70b`, the W3 point before the W6 Resolve-time Controlled-ball reattachment correction. Its surviving artifact `10732311509` was downloaded through the GitHub Actions API on September 24, 2026, verified against the API digest `sha256:f6b550828d97895e475c73bc2d2172d86d5ff43fa73920603b78a603a2824fb8`, and its own internal `SHA256SUMS` verified cleanly. The API reports `expires_at=2026-12-22T03:46:31Z` for the original artifact.

All eight uploaded artifact files are now accounted for. The archive retains `report.txt`, `results.tsv`, `results.json`, and `measurement.runsettings` byte-for-byte; preserves the raw `instrument-output.txt` losslessly as nine ordered xz chunks so `parse_foul_card_characterization.py` can be rerun after artifact expiry; preserves the original TRX losslessly as seven ordered xz chunks; and commits the artifact's original `SHA256SUMS` ledger. The redundant full-console `measurement.txt` is not committed, but its exact original SHA-256 is retained in both the committed artifact ledger and provenance. The binary chunking is an archival-transport choice, not a data-format requirement. `supporting-pre-w6-provenance.txt` records reconstruction commands and original/compressed hashes. These files preserve the result without promoting the arm into PR #439's cited-run ledger or making a W3-vs-W6 attribution claim.

`supporting-focused-run-excerpts.transcribed.txt` separately preserves the result-bearing lines for the W6 pre-fix discriminator, W6 focused post-fix proof, and SeasonSave focused v2 proof. Those excerpts are explicitly transcribed from the Actions job-log API rather than asserted as byte copies.

## Data carried over from closed PR #446

Two data files from the superseded #446 archive attempt (branch `archive/pr439-evidence-20260923`, head `d8f61356ba5b45debe04371343a5e2715d493ede`, also held by GitHub as `refs/pull/446/head`) had no copy on `main`. Both are committed byte-for-byte; their Git blob ids are unchanged.

- `header-reachability-441-results.tsv` (was `pr439-w3/header-reachability/results.tsv`, blob `2a71483e6d91fecd44ca2f7ccfd8deb363d09fc2`): the per-seed #441 header funnel from run `35887487201` on evidence head `7494300341cae94ed3eae41033d874e23cac0b06` (production `f40f0853…` plus the evidence-branch counters archived under `pr439-ref-archive/history/header-reachability/`). That run uploaded no artifact. On September 24, 2026 all 16 counters for all six seeds, and the aggregate row, were checked against the six job logs; `header-reachability-441-run-log-lines.transcribed.txt` keeps those source lines. The aggregate (1,811 commits → 1,811 jump starts → 18 ever-predicted → 0 prepared → 0 executed; 1,793 `failedPositionedPoorly` + 16 `failedEarly`; 1 overwrite) matches `w3-agent-ball-fanout-design.md`. This is #441 localization data, not a W3 arm.
- `authoritative-artifact-member-hashes.tsv` (was `pr439-w3/source-artifact-hashes.tsv`, blob `569c6b91d5a022349f472fc2cd0c26e5b7ac4e4f`): the SHA-256 of each file inside the artifacts of runs `35925236129` and `35814050060` (instrument output, `measurement.txt`, runsettings, report, JSON, TSV, TRX). Checked on September 24, 2026: both zip digests equal the Actions API digests, and the TSV hash equals `final-w3-results.tsv`. Not re-derived: the other member hashes, because the artifacts could not be downloaded from the archiving environment. Not covered: the pre-W3 arm, run `35815761065`. The two artifacts expire `2026-12-22T21:54:06Z` and `2026-12-22T03:22:15Z`.

## Interpretation boundary

The pre-W3 W3-specific counters are **structural zeros, not observations of a live W3 instrument**. The pre-W3 instrument explicitly emitted literal zero values because the fan-out/claim surfaces did not exist yet. Therefore those zeros do not provide a valid before/after rate comparator for keeper claims or fan-out frequency.

The two arms are also **not a pure W3 A/B comparison**. Between the pre-W3 production anchor and the landed W3 rerun, the measured code includes the W6 Resolve-time Controlled-ball reattachment correction surfaced during #439, the #442/#443 test-contract merge brought into #439 via merge-only PR #444, and review-closure gameplay fixes `ERR-010-004` and `ERR-011-014`. The foul, slide-tackle, aerial-delivery, and other trajectory deltas in the TSVs are descriptive characterization only and must not be attributed to W3 alone.

`headerAttempts` in these tables is the diagnostic's terminal-event count (`HeaderContacts + HeaderFailures`), not a count of committed `HeaderIntent`s. The separate #441 localization measured the actual commit path: 1,811 HeaderIntent commits → 1,811 jump starts → 18 intents ever assigned a predicted contact frame → 0 prepared Head contacts / 0 executed headers.

The authoritative interpretation remains `docs/tracking/w3-agent-ball-fanout-design.md` §6.1–§6.2 and `docs/tracking/match-engine-wiring-backlog.md` W3.

## Landed result summary

The final rerun recorded, in aggregate over the same six frozen 90-minute seeds:

- `agentBallFanoutEvents=215083`
- `claimEligibilityEpisodes=1297`
- `registeredDuelParticipants=1164`
- `resolvedHandContactDuels=1164`
- `successfulKeeperClaims=753`
- `headerAttempts=1809`
- `headerContacts=0`

This is the durable data behind the landed classification **W3: wired but dormant**. It proves the Hand path and fan-out are live; it does not close Heading #10 reachability, which remains tracked by issue #441.

`pre-w3-results.tsv` and `final-w3-results.tsv` are copied byte-for-byte from their respective Actions artifacts. `SHA256SUMS` covers every tracked file in this directory other than itself and is enforced by the repository evidence-manifest checker after this directory is registered.
