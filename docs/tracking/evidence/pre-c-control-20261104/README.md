# Pre-C control — passed, review closeout 1.1

Created November 4, 2026. Purpose: close engine lane item 1 and retain the B control
for W8 C. **Ready for the Pre-C control scope**, on the declared Linux .NET lane.
C implementation and its result-bearing comparison remain separate work.

| Acceptance criterion | Result | Retained proof |
| --- | --- | --- |
| Pin source, runtime, configuration and read-only instrument | PASS | Original `freeze.json`, frozen C# sources and `observer-only.patch` |
| Instrument leaves the measured matches unchanged | PASS | Every per-tick digest, full ball state and canonical ledger record equal in both arms |
| Reproduce the adopted post-law B control | PASS | All six historical v27 terminal digests reproduced in both arms |
| Reproduce the proof from a checkout | PASS | Complete LFS capture, portable analysis/rebuild commands, explicit partial-mode limits |

All 12 original full-match executions exited 0. Each arm contains 1,944,000 ticks:
six fixed seeds, 324,000 ticks each. The comparisons include all 1,944,000
corresponding digest samples, not just terminal summaries. The full arrays are
now retained in the repository; no ephemeral export is needed for reanalysis.

## Pinned lane and source

- Source `1d3cebf3b8d874715158067d9deb5fa48b309a30`, `src` tree
  `65c8a8bfc533ac354a9a652a79cf6fb7cc94b2c8`.
- Debian 13 x64, SDK **8.0.426**, CLR **8.0.32**, generated Unity-shim projects,
  Debug, optimization disabled, DEVELOPMENT_BUILD, NUnit assertions 3.14.0.
- `new MatchEngine(seed); EnableGkHeading()`, default squads/attributes, no
  config binding or overrides, shipping SlowDown asserted for both teams.
- Exact frozen W8 observer blob `520ba31c71e06ed408fd5c282b5fd5611cf74879`;
  `Program.cs.txt` and the observation overlay are unchanged from the captures.
- One seed per process, two processes concurrently, two-CPU quota,
  DOTNET_PROCESSOR_COUNT=4, selected JIT overrides unset. No shared EventBus.
- Inputs frozen November 4 at 14:24:40 PST. Original freeze SHA-256:
  `e47471bcf87ceb342ea881e8490ba98d90b7525432e980454ca3a44326a5432c`.
  Accepted runs: 14:24:40–14:39:21 PST (22:24:40–22:39:21 UTC), November 4.
  Commands/exits are in `runs.json`; original input/runtime checks are in
  `freeze-verification.log`.

`freeze.json` describes the **original capture**, including its historical scratch
paths and original tool hashes. Those tool versions and outputs are preserved in
`as-run-v1.tar.xz` and commit `7accb592`. Current portable wrappers do not masquerade
as those old hash-pinned scripts. `policy-freeze.json` separately pins analysis
rule 1.1 before any C capture. No game, capture runner or observer was changed or
recaptured during this review correction.

## Six adopted fingerprints

Each value equals its historical adopted B value and both captured arms.

| Seed | Terminal digest, schema v27 | Goals | Shots | Releases |
| --- | --- | ---: | ---: | ---: |
| `0F1E2D3C4B5A6978` | `6ef1e77f9dcf86c4bce5e1f66a989a64b42f26c268ead53340b9c57d282b4072` | 7 | 19 | 7 |
| `00000000D1A6D05E` | `06e3daf03f1c45a112f958c4bfac980c30956971e175b35b7d815d685b4caf93` | 5 | 19 | 20 |
| `0000000000000001` | `25ceac27f7d4e87fb1166227f683eaff87336efd301fa5500b6d212a28deba59` | 6 | 16 | 15 |
| `00000000ABCDEF12` | `77b4e0cc08a4ac903fbb364f2bb28612eaa39b380955111c58fe040ebf474a80` | 11 | 29 | 22 |
| `0000000099887766` | `ccc99ba78dce889ed07d18609c568486920ffd97c45cb283f15d7f5c203f6e4f` | 8 | 26 | 23 |
| `000000005A5A5A5A` | `6961b03ff4cb1512967c01c46158274b3516bb4c632d921f75e5f6baad936eb3` | 10 | 37 | 39 |

Totals: **47 goals, 146 shots, 126 releases**. Historical nominal rate:
47 / (1,944,000 / 600) = **0.01450617 goals per 600 frames**. There are 1,943,994
live pre-full-time frames; that separate rate is 0.01450622. The historical
denominator is not silently replaced.

## Exact-touch/team review and rule 1.1

All **23,948** event publishes reconcile exactly once to the independent canonical
ledger. Source-order callbacks record applied contacts, release/clearing state and
within-frame order. Shots retain shooter/team; goals retain ScoringTeam separately
from credited scorer/team, with scoring position and raw event identity. Three
corpus goals have credited-scorer team different from ScoringTeam; attribution
uses ScoringTeam, not a recent toucher or the credit field.

The original raw label `touch:tackle-loose` overstates what the producer does.
`TackleOutcome.BallLoose` releases control without applying contact or calling
RecordPlayerBallTouch. **Rule 1.1 excludes its 37 diagnostic outcome rows** from
physical-touch eligibility. Won-tackle acquisition remains eligible because it
does record the touch. Unknown new producers fail closed. The full eligibility
set and window rules are in [PROTOCOL.md](PROTOCOL.md).

Reanalysis finds **16,264 applied touches**. `review.py` verifies all 126 release
decisions, all 12 selected windows and every recorded stream hash against the
original results. The correction changes none of them. C must use this clarified,
separately frozen rule, rather than inherit the raw label as a physical claim.

Release selection remains **114 teammate-first, 12 opponent-first within 507
frames**. All 12 windows are complete. Two window pairs overlap, but no shot or
goal belongs to more than one window: **8 opponent shots and 6 opponent goals**,
also 8/6 unique events. There are no releasing-team shots/goals in these windows.
This is temporal association in a selected small population, not a causal,
balance or keeper-passivity verdict.

### The two same-frame clearings

Both belong to seed `00000000ABCDEF12`, at frames **24,126** and **24,378**.
Keeper 11 releases at order 2; PassAttemptEvent follows at order 3, distribution
at order 4, opponent 10's first touch at order 5 and restriction clearing at 6.
The opponent is about **0.8403 m** / **0.8351 m** from the release point, within
the shipping 1 m first-touch reach, and the emitted velocity approaches it.

This follows the source's documented Resolve order: executors apply CONTACT,
then RunFirstTouch can receive the now-loose approaching ball in the same tick,
before the next Physics integration. Both end with opponent 10 controlling the
ball. `review.json` preserves positions-derived distance, emitted velocity,
approach dot product and order witnesses. Both follow-up windows have zero shots
and goals. **Retain both** under the same ordered-touch rule in B and C; do not
drop them after seeing the result or infer an extra elapsed flight frame.

### Coverage limits

The corpus contains **0 header contacts** and **0 rejected hand-contact gates**:
all 165 attempts become 165 contacts (126 claims, 39 responses). The six C#
classifier assertions and Python rejected-attempt check validate decisions in
isolation; they do not demonstrate those unexecuted physical producer paths.
Required C boundary/restart/rejection/replay fixtures remain necessary. Ordinary
matches cannot establish rare-sanction behavior or resolve #441 reachability.

## Reproduction, integrity and validation

Follow [REPRODUCE.md](REPRODUCE.md). Its first command block uses only this checkout
and the single committed LFS archive. It recomputes the unmodified full analyzer,
review decisions and tables, including digest bytes; no four-export bundle or
original container directory is required. `prepare.py`, `run.py` and `closeout.py`
accept explicit scratch paths. The exact source archive is reconstructed from
its pinned Git commit with LFS smudging disabled and matches its original SHA-256.

`capture-traces.tar.xz` contains all 108 original per-arm files: twelve each of
digests, trajectories, ledgers, raw observations, frozen reports, summaries,
progress, engine logs and runner logs. Equal files are stored once with tar hard
links after comparison; both arm paths and original per-arm stream hashes remain.
`SHA256SUMS` covers every other tracked file in this evidence directory.

Pointer-only CI checks strict LFS reference OIDs using the file's Git LFS
designation. It does not claim unavailable payload bytes or remote availability
were verified. Materialized checkouts hash actual content bytes. Full analysis
requires the downloaded archive; `--skip-digests` explicitly reports **PARTIAL**
and `passed: null`, and cannot produce a passing closeout.

Validation includes the selection/evidence unit tests, strict LFS-pointer and
materialized-corruption tests, full six-seed reanalysis and review parity checks.
Both portable source builds pass with the same three existing CS0649 warnings;
their 1,000-frame smoke streams match, and the rebuilt classifier assertions pass.
That smoke run is not represented as a new six-seed capture. Review check commands
and outcomes are retained in `review-validation.log`.

Initial build/harness failures and the superseded preflight classifier attempt are
retained in `preflight-attempts.tar.xz`. No superseded result enters this control.
Unity Editor, player build, Windows certification and C implementation were not
validated by this task. Shipping `src`, Assets, Packages and ProjectSettings remain
unchanged; no host work was needed.

## Version history

| Version | Date | Change |
| --- | --- | --- |
| 1.1 | November 4, 2026 | Review closeout: pointer-safe CI verification; complete digest capture in LFS; portable replay/rebuild; explicit BallLoose exclusion with unchanged decisions; same-frame and unexercised-path review. |
| 1.0 | November 4, 2026 | Original six-pair control and frozen capture, retained in as-run-v1.tar.xz and commit 7accb592. |
