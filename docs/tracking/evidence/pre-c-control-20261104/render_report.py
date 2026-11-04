"""Created 2026-11-04: evidence-led Pre-C handoff, rendered only after all checks."""
from pathlib import Path
import base64,datetime,hashlib,json,struct
from zoneinfo import ZoneInfo
ROOT=Path(__file__).resolve().parent
REPO=Path('/workspace/Soccer-Manager-Pro')
TARGET=REPO/'docs/tracking/evidence/pre-c-control-20261104'
# This managed executor has no /codex mount; retain the export bundle as scratch.
OUTPUT=ROOT/'deliverables'
report=json.loads((ROOT/'analysis.json').read_text());pin=json.loads((ROOT/'freeze.json').read_text())
assert report['passed']
windows=json.loads((ROOT/'windows.json').read_text())
selected=[w for w in windows if w['selected']]
full_time={};played_frames=0;own_goals=0
for item in report['seeds']:
    seed=item['seed'];rows=[json.loads(x) for x in (ROOT/'results/observed'/seed/'observations.jsonl').read_text().splitlines()]
    transitions=[r for r in rows if r.get('ordinal')==0x1A and base64.b64decode(r['raw'])[12]==1]
    assert len(transitions)==1
    full_time[seed]=transitions[0]['frame'];played_frames+=full_time[seed]-1
    assert all(w['firstOtherTouch']['frame']+600<full_time[seed] for w in selected if w['seed']==seed)
    own_goals+=sum(r['kind']=='goal' and r['team']>=0 and r['team']!=r['scoringTeam'] for r in rows)
frozen_at=datetime.datetime.fromisoformat(pin['frozenAtUtc']).astimezone(ZoneInfo('America/Los_Angeles'))
runs=json.loads((ROOT/'runs.json').read_text())
earliest=min(r['startedUtc'] for r in runs);latest=max(r['endedUtc'] for r in runs)
freeze_hash=hashlib.sha256((ROOT/'freeze.json').read_bytes()).hexdigest()
proof_lines=[]
for item in report['seeds']:
    o=item['observed'];proof_lines.append(f"| `{item['seed']}` | `{o['digest']}` | {o['goals']} | {o['shots']} | {o['distributions']} |")
raw_records=sum(s['publishLedgerRecordsVerified'] for s in report['seeds'])
touches=sum(n for kind,n in report['observationCounts'].items() if kind.startswith('touch:'))
text=f'''# Pre-C control — passed

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

- Source commit: `{pin['sourceCommit']}`; `src` tree `{pin['sourceTree']}`.
- SDK **8.0.426**, CLR **8.0.32**, Debian 13 x64; generated Unity-shim projects,
  Debug, optimization disabled, DEVELOPMENT_BUILD, NUnit assertions 3.14.0.
- `new MatchEngine(seed); EnableGkHeading()`; default squads/attributes, no
  configuration binding or overrides; shipping SlowDown policy asserted for both
  teams; 324,000 ticks per seed. No tuning or C gameplay is included.
- Exact frozen W8 observer Git blob: `520ba31c71e06ed408fd5c282b5fd5611cf74879`.
- Inputs frozen {frozen_at.strftime('%B %d, %Y at %H:%M:%S %Z')}, before accepted
  captures. Freeze manifest SHA-256: `{freeze_hash}`.
- One seed per process, two processes concurrently in a container with a two-CPU
  quota; no shared EventBus across processes. As-run start/end UTC:
  `{earliest}` to `{latest}`. Commands and exit codes are in `runs.json`.
- The post-capture verification confirms all pinned source, compile inputs,
  binary sets and CLR files remained unchanged (`freeze-verification.log`).

## Adopted baseline reproduced

Every digest below equals both its historical adopted value and the new plain
control. The historical source is `../../w8-b-implementation-results.md`.

| Seed | Terminal digest, schema v27 | Goals | Shots | Distributions |
| --- | --- | ---: | ---: | ---: |
{chr(10).join(proof_lines)}

## Supplementary instrument check

The source-level callbacks retain actual applied contacts, including body
deflections and uncontrolled first touches, at 60 Hz with producer order within
the frame. Merely overlapping collision pairs and restart-taker assignments are
excluded. Hand attempts, successful contacts, hand responses and hand releases
remain distinguishable. A shared chronological counter orders physical contacts
against authoritative event publishes. Event publish order is retained separately
from canonical ledger order.

There are **{touches:,} applied touches** and **{raw_records:,} event publishes**.
Every publish's complete payload bytes reconcile exactly once to the independent
canonical ledger. Shots retain shooter/team; goals retain ScoringTeam separately
from credited scorer/team, plus scoring location and raw identity. The corpus
includes **{own_goals} goals whose credited scorer's team differs from ScoringTeam**;
attribution uses the explicit scoring field.

The frozen rule selects the first other-player actual touch after each release,
ignores intervening own touches, excludes teammate-first releases and restart
censoring, and requires an opponent within 507 frames inclusive. Events in its
following 600-frame window must occur after that touch in `(frame,order)`.
The exact protocol, all release decisions, and event memberships are retained.

- Release selection: `{json.dumps(report['releaseSelection'],sort_keys=True)}`.
- Selected windows: **{report['selectedWindows']}**, complete: **{report['completeWindows']}**,
  overlapping pairs: **{report['overlappingWindowPairs']}**. Each selected window
  also ends strictly before the recorded full-time transition.
- Window event memberships by team: `{json.dumps(report['perWindowEventCounts'],sort_keys=True)}`.
- Unique events across the selected windows: `{json.dumps(report['uniqueEvents'],sort_keys=True)}`.
- Corpus goals: **{report['corpusGoals']}**; goals per 600 nominal simulated frames:
  **{report['corpusGoalsPer600Frames']:.8f}**, denominator **1,944,000 simulated
  frames / 600 = 3,240** periods, matching the historical convention.
- The recorded Input-phase full-time transitions leave **{played_frames:,}** frames
  before the full-time freeze; the corresponding rate is
  **{report['corpusGoals']/(played_frames/600):.8f}** per 600 live pre-freeze frames.
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
'''
(TARGET/'README.md').write_text(text)
output_text=text.replace('## Result and acceptance','Full raw captures: [capture archive](pre-c-control-full-captures.tar.xz). Pinned source: [source archive](pre-c-control-source.tar.xz). Runnable binaries: [binary archive](pre-c-control-binaries.tar.xz). Compact evidence: [evidence archive](pre-c-control-evidence.tar.xz).\n\n## Result and acceptance')
(OUTPUT/'pre-c-control-report.md').write_text(output_text)
print('Rendered report; live pre-freeze frames',played_frames,'credited/scoring team differences',own_goals)
