# Pre-C control — passed

Created November 4, 2026. Purpose: close engine lane item 1 in the ratified roadmap
and establish the B control for W8 C. This is local Linux .NET evidence.

**Validation status: Ready for the Pre-C control scope.**

## Result and acceptance

| Criterion | Result | Evidence |
| --- | --- | --- |
| Pin source, runtime, configuration and instrument | PASS | `freeze.json`, `PROTOCOL.md`, `observer-only.patch`, source/runtime/assembly SHA-256 sets |
| Instrument leaves match results unchanged | PASS | Full digest, ball and Tier A/B ledger streams equal on every tick of all six paired matches; `analysis.json` |
| Reproduce the adopted post-law W8 B baseline | PASS | All six historical v27 terminal digests reproduced in both plain and observed runs; `per-match.csv` |

All **12 full-match executions exited 0**. The two arms each cover 1,944,000 simulated
ticks; neutrality compares all 1,944,000 corresponding digest samples, complete ball
states and canonically ordered event payloads. No differing tick or event was found.
The observed arm contains the unchanged frozen W8 observer and the supplementary
exact-touch/team observer. Equality with an unmodified production build, together
with the adopted historical fingerprints, establishes neutrality for this corpus.

## Pinned lane

- Source commit: `1d3cebf3b8d874715158067d9deb5fa48b309a30`; `src` tree `65c8a8bfc533ac354a9a652a79cf6fb7cc94b2c8`.
- SDK **8.0.426**, CLR **8.0.32**, Debian 13 x64; generated Unity-shim projects,
  Debug, optimization disabled, DEVELOPMENT_BUILD, NUnit assertions 3.14.0.
- `new MatchEngine(seed); EnableGkHeading()`; default squads/attributes, no
  configuration binding or overrides; shipping SlowDown policy asserted for both
  teams; 324,000 ticks per seed. No tuning or C gameplay is included.
- Exact frozen W8 observer Git blob: `520ba31c71e06ed408fd5c282b5fd5611cf74879`.
- Inputs frozen November 04, 2026 at 14:24:40 PST, before accepted
  captures. Freeze manifest SHA-256: `e47471bcf87ceb342ea881e8490ba98d90b7525432e980454ca3a44326a5432c`.
- One seed per process, two processes concurrently in a container with a two-CPU
  quota; no shared EventBus across processes. As-run start/end UTC:
  `2026-11-04T22:24:40.753569+00:00` to `2026-11-04T22:39:21.136311+00:00`. Commands and exit codes are in `runs.json`.
- The post-capture verification confirms all pinned source, compile inputs,
  binary sets and CLR files remained unchanged (`freeze-verification.log`).

## Adopted baseline reproduced

Every digest below equals both its historical adopted value and the new plain
control. The historical source is `../../w8-b-implementation-results.md`.

| Seed | Terminal digest, schema v27 | Goals | Shots | Distributions |
| --- | --- | ---: | ---: | ---: |
| `0F1E2D3C4B5A6978` | `6ef1e77f9dcf86c4bce5e1f66a989a64b42f26c268ead53340b9c57d282b4072` | 7 | 19 | 7 |
| `00000000D1A6D05E` | `06e3daf03f1c45a112f958c4bfac980c30956971e175b35b7d815d685b4caf93` | 5 | 19 | 20 |
| `0000000000000001` | `25ceac27f7d4e87fb1166227f683eaff87336efd301fa5500b6d212a28deba59` | 6 | 16 | 15 |
| `00000000ABCDEF12` | `77b4e0cc08a4ac903fbb364f2bb28612eaa39b380955111c58fe040ebf474a80` | 11 | 29 | 22 |
| `0000000099887766` | `ccc99ba78dce889ed07d18609c568486920ffd97c45cb283f15d7f5c203f6e4f` | 8 | 26 | 23 |
| `000000005A5A5A5A` | `6961b03ff4cb1512967c01c46158274b3516bb4c632d921f75e5f6baad936eb3` | 10 | 37 | 39 |

## Supplementary instrument check

The source-level callbacks retain actual applied contacts, including body
deflections and uncontrolled first touches, at 60 Hz with producer order within
the frame. Merely overlapping collision pairs and restart-taker assignments are
excluded. Hand attempts, successful contacts, hand responses and hand releases
remain distinguishable. A shared chronological counter orders physical contacts
against authoritative event publishes. Event publish order is retained separately
from canonical ledger order.

There are **16,301 applied touches** and **23,948 event publishes**.
Every publish's complete payload bytes reconcile exactly once to the independent
canonical ledger. Shots retain shooter/team; goals retain ScoringTeam separately
from credited scorer/team, plus scoring location and raw identity. The corpus
includes **3 goals whose credited scorer's team differs from ScoringTeam**;
attribution uses the explicit scoring field.

The frozen rule selects the first other-player actual touch after each release,
ignores intervening own touches, excludes teammate-first releases and restart
censoring, and requires an opponent within 507 frames inclusive. Events in its
following 600-frame window must occur after that touch in `(frame,order)`.
The exact protocol, all release decisions, and event memberships are retained.

- Release selection: `{"opponent-first-within-507": 12, "teammate-first": 114}`.
- Selected windows: **12**, complete: **12**,
  overlapping pairs: **2**. Each selected window
  also ends strictly before the recorded full-time transition.
- Window event memberships by team: `{"goal:opponent": 6, "shot:opponent": 8}`.
- Unique events across the selected windows: `{"goal": 6, "shot": 8}`.
- Corpus goals: **47**; goals per 600 nominal simulated frames:
  **0.01450617**, denominator **1,944,000 simulated
  frames / 600 = 3,240** periods, matching the historical convention.
- The recorded Input-phase full-time transitions leave **1,943,994** frames
  before the full-time freeze; the corresponding rate is
  **0.01450622** per 600 live pre-freeze frames.
  The nominal historical denominator above is not silently replaced.

These selected windows describe temporal association. This is a B control, not a
B-versus-C result or a causal/balance/passivity verdict.

## Validation and retained evidence

- Both clean source-copy builds completed with zero compiler errors; three
  existing CS0649 warnings in ActionSelector appeared in each full compile.
  The final incremental builds have zero warnings/errors. Logs are retained.
- Seven Python selection tests pass (same-frame before/after, teammate-first,
  own touches, 507/508, 600-frame boundary, censoring, overlap and rejected attempts).
- Six C# contact-classifier assertions pass, including headers by a goalkeeper,
  consumed hand responses, claim consumption, different agents and different frames.
- Five independent Python evidence checks pass, rejecting omitted/duplicate
  publishes, trailing ledger bytes and a nonterminal digest change despite an
  equal final digest. Publish order may differ from canonical order.
- Twelve complete CLR captures and full-corpus stream/publish reconciliation pass.

`capture-traces.tar.xz` retains both arms' logs, summaries, trajectories and complete
ledgers, and every observed exact-touch/team row and frozen W8 report. Equal files
are stored once with tar hard links. Complete per-tick digest-chain SHA-256 values
for both arms are in `analysis.json`; the full raw digest arrays are additionally
retained in the local output artifact `pre-c-control-full-captures.tar.xz`.
The pinned source archive and runnable compiled binary sets are also local outputs.
These export bundles are retained under `/workspace/work/pre-c-control/deliverables/`;
the source repository evidence remains the durable review record for this task.
`SHA256SUMS` covers all other files in this repository evidence directory.

The initial parallel build attempt returned nonzero without a useful compiler
diagnostic; serialized builds were then used. A missing harness namespace import
was corrected. An initial v1 capture was superseded after source review found
that roster role could misclassify a goalkeeper's header as a hand response.
The accepted classifier consumes an actual preceding hand contact. No v1 result
enters this control. Preflight sources, manifests, logs and run receipts are in
`preflight-attempts.tar.xz`; incomplete superseded stream data remain scratch only.

## Reproduction and next step

Restore the pinned source commit/archive and .NET SDK/runtime, then use the
retained `prepare.py`, `Program.cs.txt` and `FrozenW8.cs.txt` to reproduce the two
builds. `prepare.py` takes source copies from `source.tar`; its original checkout
and scratch paths are explicit and must be adapted if replayed elsewhere. Restore
the frozen observer file at its named repository path, or substitute the retained
identical file. `Program.cs.txt` must be named `Program.cs` in the capture workspace.
Generate projects, build Debug with `-m:1 --disable-build-servers`, using the existing
package cache and `-p:NuGetAudit=false --source <empty-local-feed>` for an offline
restore. No dependency version was changed. `global.json` pins SDK 8.0.426.

Use `dotnet --fx-version 8.0.32 <arm>/capture/bin/Debug/net8.0/TacticalDirector.MatchEngine.Tests.dll
<16-digit-seed> <plain|observed> <output-directory>` for an explicit runtime replay.
The original as-run commands use the same CLR from the sole installed runtime;
`runs.json` preserves those exact commands. `freeze.py` and `run.py` document the
as-run preparation and fail-closed input hashes; an adapted path needs a new
provenance record. Run `analyze.py` against the retained result layout.

**Pre-C control is complete. W8 C is the next engine item.** Its implementation,
boundary/restart/restore tests, Linux paired comparison and required pinned-host
validation remain separate work. Preserve this declared lane and observer; compare
C's gameplay event and ball streams against this B control and explain the first
divergence. Snapshot fingerprints are reported separately across the C schema change.

Unity Editor compilation, EditMode, player builds, canonical CI policy and Windows
certification were not run by this measurement task. Shipping `src`, Assets,
Packages and ProjectSettings are unchanged; no host work was needed.

## Version history

| Version | Date | Notes |
| --- | --- | --- |
| 1.0 | November 4, 2026 | Frozen Pre-C control, six adopted digests in both arms, full neutrality and exact-touch/team evidence. |
