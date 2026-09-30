# S0 low-fidelity prototype v0.5

**Created:** September 30, 2026\
**Purpose:** executable Gates C–F design evidence for the [S0 journey packet](../ux-s0-pm1-journey.md).

Open `index.html` in a desktop browser. All assets are local; no server, account, build or network is required.
The product surface contains no moderator instructions about which control to click. The banner identifies
the entire experience as a design simulation. The seven journey states retain the four existing screen
identities and five named navigation moves. It does not consume or modify Unity bindings.

Every action and command result is simulated. Clock progression is compressed to one match minute per
second at the 1× selection for practical prototype sessions. Pause retains the selected rung. Captured
pitch positions come from the unmodified capture. The ordinary 2–1 score/statistics scenario is
explicitly synthetic. `fixture=scoreline` uses the unmodified 19–9 capture for real-data validation; tactical/substitution choices do not recompute the
selected scenario or its statistics. The captured Substitutions row is omitted from this vehicle;
the pitch states that simulated swaps do not replace its captured markers. The shipping statistic
set in packet §7.7 is unchanged. The displayed substitution count is explicitly prototype request state.
No shipping speed, timing, telemetry, performance or determinism certificate follows from this prototype.

## Vehicles and provenance

`model.js` contains pure simulated transitions. `prototype.js` renders semantic controls and owns only
presentation state. `scenario-data.js` supplies the labelled synthetic ordinary fixture. `prototype.css` provides monochrome hierarchy/reflow. `reference-data.js` records
unmodified `MatchClientHost.Project()` / `BuildReport()` output from the existing reference composition.
Its header object records source SHA, seed and manager modes. It is not a synthetic second accumulator.

Reproduce the capture with .NET 8 and Python 3 available:

```bash
bash docs/design/s0-prototype/capture-reference.sh
```

The capture tool generates existing ignored shim projects and a temporary console caller. It uses the
existing host/session/analytics composition, samples once each match minute, and changes no reference
harness or production file. This is non-certifying Linux evidence. Captured field positions are sparse
snapshots, not a smooth renderer. #37 does not supply shots; xG is omitted when unavailable. Possession
shares include loose-ball time and are not normalized to force Home + Away to equal 100%.

## Reviewer fixtures

These URL parameters are review tools; they are not product controls or shipping settings.
For example, open `index.html?review=1&state=MV-L&fixture=fault&pseudo=1&scale=2`.

| Parameter | Values / purpose |
|---|---|
| `state` | `MM`, `TS`, `MV-0`, `MV-L`, `MV-P`, `MV-FT`, `PR`; direct state inspection only |
| `review=1` | freeze timer for wireframe inspection; navigation/controls still work |
| `fixture` | `ordinary`, `waiting`, `fault`, `refusal`, `limit`, `pending-end`, `long-names`, `scoreline`, `events` |
| `pseudo=1` | bracketed labels with approximately 40% expansion; equivalent stress content, not #49 runtime |
| `scale=2` | 200% base-font stress (16px → 32px), browser zoom stays 100%; shipping maximum unallocated |
| `captions=1` | future-caption region coexistence fixture only; #51 runtime remains future-blocked |

The fault fixture exposes synthetic values through minute 18 beneath an incomplete notice; score/time
continue from later frames. The report hides those partial values until disclosure. The unusual-score
fixture selects the actual 19–9 reference capture and labels that source in the banner.
The ordinary 2–1 scenario is chosen for practical sessions, not as an engine realism result. Long identity text
is a labelled stress fixture; the ordinary demo uses Home/Away and shirt numbers, with no invented names.

## Verification

With Playwright available, run the recorded walkthrough from the repository root:

```bash
NODE_PATH=/path/to/node_modules UX_BROWSER=/path/to/chrome node docs/design/s0-prototype/verify.cjs
```

Baseline run `UX-GE-S0-20260930-05` at `a859ea1` records 85 checks and 19 PDFs under `evidence/v0.5/`,
including full-time statistics failure. Its `walkthrough.json` fingerprints all seven source files
and all 19 PDFs at that commit. The baseline JSON remains unchanged. Focused delta run
`UX-GE-S0-20260930-05-DELTA-01` records three checks and current seven-source/19-image hashes in
`evidence/v0.5/full-time-fault-delta.json`; only `mv-ft-statistics-fault.pdf` is regenerated. Reproduce it with:

```bash
NODE_PATH=/path/to/node_modules UX_BROWSER=/path/to/chrome node docs/design/s0-prototype/verify.cjs --full-time-fault-delta
```

Earlier evidence remains unchanged: the root `evidence/walkthrough.json` and
18 PDFs record the owner-approved v0.4 at `13c2c09`; their source hashes refer to that pinned
commit, not current v0.5. Runs v0.1/v0.2/v0.3 are also retained. Prior v0.4 claims about normal-text
clearance and the fault journey are superseded by run 05, which verifies actual label expansion,
minute 0/17/18/21, direct-state entry, fast cutoff crossing and full-time fault wording.

Keyboard navigation rejects unexpected body focus; submits and both speed limits assert immediate
focus destinations. Dropdown focus outlines are measured clear of labels at normal and pseudo/200% text at all three widths. Earlier feedback is tested over live timer ticks, not only frozen review frames.
The end-race test reaches minute 89 from MM, submits while paused, then resumes to the whistle. It traverses
S0-T1–T7 plus substitution, tests semantic outcomes, and inspects layout/focus/failure fixtures. See the
journey packet's Gate-E matrix for limits, N/A reasons and remaining implementation obligations.
Automated/self-walkthrough evidence supplies no Gate-G participant results.

## Gate boundary

C–F are complete for v0.5. The owner-approved v0.4 images/decisions remain pinned in validation
protocol §9.1; the owner approved the v0.5 full-time fault delta at `0e8bd2b` in §9.1.1.
The other 18 PDFs match approved v0.4 apart from version text and carry forward. G is PASS and H is OPEN
for high-fidelity work and owner image review. The owner conducts S0 image reviews; no tester prerequisite
returns. H then needs its own separate image approval before I; #470 remains blocked on I.
Scripted evidence supplies no runtime or independent usability evidence.

| Version | Date | Change |
|---|---|---|
| 0.1 | September 30, 2026 | Initial low-fidelity S0 vehicle, captured reference data and reproducible walkthrough. |
| 0.2 | September 30, 2026 | Fixes live focus/disclosure; coherent ordinary synthetic 2–1 versus captured 19–9 fixtures; reachable whistle race; 74 checks; archives insufficient original E run; clarifies text-scale method and formal F prerequisites. |
| 0.2 review-route update | September 30, 2026 | Owner-directed S0 image review replaces tester prerequisites; prototype sources, PDFs and 74-check run unchanged. |
| 0.3 | September 30, 2026 | Complete 18-image review coverage; pitch markings/direction and label leaders; raw restart/holder caption removed; changing partial statistics, aligned headers and simplified waiting state. 79-check run 03; explicit G decisions and separate H image approval before I. |
| 0.4 | September 30, 2026 | Adds dropdown label/focus spacing and measured clearance; removes leader-line promise; shortens refusal and player-facing substitution wording. Regenerates 18 images; 80-check run 04; successful run 03 archived. Pending decisions and H/I obligations stay explicit. |
| 0.5 | September 30, 2026 | Codex corrections: fault activates on cutoff crossing; full-time fault notice states final score; normal-text focus check omits pseudo and verifies actual labels/fonts. 85-check run 05 and 19 PDFs stored separately; approved v0.4 images/evidence preserved. Current revision awaits G re-review. |
| 0.5 copy delta | September 30, 2026 | Suppresses the final-statistics report promise only when faulted; three focused checks and one regenerated PDF. Immutable run05 remains historical; separate delta record holds current hashes. Single-image owner approval pending; disabled Close statistics carried to H. |
| 0.5 delta approval | September 30, 2026 | Records actual owner approval pinned to 0e8bd2b; G PASS, H OPEN, I/#470 blocked on separately approved H images. Markdown-only record; sources, PDFs and evidence unchanged. |
