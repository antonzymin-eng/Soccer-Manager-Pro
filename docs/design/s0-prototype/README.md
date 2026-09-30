# S0 low-fidelity prototype v0.4

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

The successful 79-check v0.3 run is preserved as `evidence/walkthrough-v0.3.json`. The successful 74-check v0.2 run is preserved as `evidence/walkthrough-v0.2.json`. Current evidence includes SHA-256 fingerprints for all seven source files and all 18 PDFs.

The runner emits `evidence/walkthrough.json` and 18 PDFs (journey states, dialogs, request outcomes and statistics disclosure/stress views). The v0.4 run has 80 checks; the original
71-check v0.1 evidence is archived and its Gate-E pass withdrawn after review found two Majors.
Keyboard navigation rejects unexpected body focus; submits and both speed limits assert immediate
focus destinations. Dropdown focus outlines are measured clear of labels at normal and pseudo/200% text at all three widths. Earlier feedback is tested over live timer ticks, not only frozen review frames.
The end-race test reaches minute 89 from MM, submits while paused, then resumes to the whistle. It traverses
S0-T1–T7 plus substitution, tests semantic outcomes, and inspects layout/focus/failure fixtures. See the
journey packet's Gate-E matrix for limits, N/A reasons and remaining implementation obligations.
Automated/self-walkthrough evidence supplies no Gate-G participant results.

## Gate boundary

C–F are complete in the journey packet. Under the owner decision of September 30, 2026,
Anton Zymin conducts the S0 image reviews; tester recruitment, attestations and sessions are not
prerequisites. Gate G awaits all finding dispositions, C-DEC-1, the full-time-statistics decision,
written rationale/release conditions for carried Majors and explicit owner approval of all 18 images
in journey packet §9.4, using validation protocol §9.1. G opens H only. H needs separate owner
approval of high-fidelity images before I; draft PR #470 stays blocked on Gate I.
The 80-check interaction evidence remains distinct from image approval and real-client verification.

| Version | Date | Change |
|---|---|---|
| 0.1 | September 30, 2026 | Initial low-fidelity S0 vehicle, captured reference data and reproducible walkthrough. |
| 0.2 | September 30, 2026 | Fixes live focus/disclosure; coherent ordinary synthetic 2–1 versus captured 19–9 fixtures; reachable whistle race; 74 checks; archives insufficient original E run; clarifies text-scale method and formal F prerequisites. |
| 0.2 review-route update | September 30, 2026 | Owner-directed S0 image review replaces tester prerequisites; prototype sources, PDFs and 74-check run unchanged. |
| 0.3 | September 30, 2026 | Complete 18-image review coverage; pitch markings/direction and label leaders; raw restart/holder caption removed; changing partial statistics, aligned headers and simplified waiting state. 79-check run 03; explicit G decisions and separate H image approval before I. |
| 0.4 | September 30, 2026 | Adds dropdown label/focus spacing and measured clearance; removes leader-line promise; shortens refusal and player-facing substitution wording. Regenerates 18 images; 80-check run 04; successful run 03 archived. Pending decisions and H/I obligations stay explicit. |
