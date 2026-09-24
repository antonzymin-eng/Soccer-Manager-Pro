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
