# PR #439 W3 Durable Evidence

> **Created:** September 23, 2026
> **Purpose:** Preserve the frozen six-seed W3 before/after result data from PR #439 after the W3 landing, independently of expiring GitHub Actions artifacts and disposable evidence refs.

## Authoritative arms

| Arm | Production SHA measured | Evidence run | Evidence ref/head | Artifact id | GitHub artifact digest |
| --- | --- | ---: | --- | ---: | --- |
| pre-W3 | `876a3343319050187c2a5505b18cb32fc3d0f89d` | `35815761065` | `evidence/pr439-w3-prewire-six-seed` @ `cf36526ce6ecd64632133780ed98ca0afe46c987` | `10732021720` | `sha256:9ce8c73a1e108bb6b06244154eda36e374e772f08cb9dcd30fbace7159a4e171` |
| landed W3 rerun | `bab4cd41...` (PR #439 production head after `ERR-010-004` / `ERR-011-014`) | `35925236129` | `evidence/pr439-w3-six-seed-rerun-20260923` @ `3f391fed45795a9c4f22bc601e187dd2cbe0e39a` | `10779846684` | `sha256:f0b7978e36ae64074eed9cc198b6629af878eda40b7885f416adf1cb6247f1b4` |

The earlier post-W3 run `35814050060` on evidence head `7347b0452d8143c5b540575b4db1f882302f1988` produced structured result files byte-identical to the final rerun. The final rerun is archived here because it is the evidence cited by the landed backlog state.

## Result summary

The pre-W3 arm recorded zero W3 fan-out/claim activity. The final rerun recorded, in aggregate over the same six frozen 90-minute seeds:

- `agentBallFanoutEvents=215083`
- `claimEligibilityEpisodes=1297`
- `registeredDuelParticipants=1164`
- `resolvedHandContactDuels=1164`
- `successfulKeeperClaims=753`
- `headerAttempts=1809`
- `headerContacts=0`

This is the durable data behind the landed classification **W3: wired but dormant**. It proves the Hand path and fan-out are live; it does not close Heading #10 reachability, which remains tracked by issue #441.

`pre-w3-results.tsv` and `final-w3-results.tsv` are copied byte-for-byte from their Actions artifacts. `SHA256SUMS` covers every tracked file in this directory other than itself and is enforced by the repository evidence-manifest checker after this directory is registered.
