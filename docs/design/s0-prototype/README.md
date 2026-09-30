# S0 low-fidelity prototype v0.1

**Created:** September 30, 2026\
**Purpose:** executable Gates C–F design evidence for the [S0 journey packet](../ux-s0-pm1-journey.md).

Open `index.html` in a desktop browser. All assets are local; no server, account, build or network is required.
The product surface contains no moderator instructions about which control to click. The banner identifies
the entire experience as a design simulation. The seven journey states retain the four existing screen
identities and five named navigation moves. It does not consume or modify Unity bindings.

Every action and command result is simulated. Clock progression is compressed to one match minute per
second at the 1× selection for practical prototype sessions. Pause retains the selected rung. Captured
snapshots supply pitch, score and #37 statistics; tactical/substitution choices do not recompute the
recorded match or its statistics. The displayed substitution count is explicitly prototype request state.
No shipping speed, timing, telemetry, performance or determinism certificate follows from this prototype.

## Vehicles and provenance

`model.js` contains pure simulated transitions. `prototype.js` renders semantic controls and owns only
presentation state. `prototype.css` provides monochrome hierarchy/reflow. `reference-data.js` records
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
| `scale=2` | explicit 200% text stress value; shipping maximum remains unallocated |
| `captions=1` | future-caption region coexistence fixture only; #51 runtime remains future-blocked |

The fault fixture exposes values captured through minute 18 beneath an incomplete notice; score/time
continue from later frames. The report hides those partial values until disclosure. The unusual-score
fixture is synthetic and marked by its reviewer URL, not a claimed real match result. Long identity text
is a labelled stress fixture; the ordinary demo uses Home/Away and shirt numbers, with no invented names.

## Verification

With Playwright available, run the recorded walkthrough from the repository root:

```bash
NODE_PATH=/path/to/node_modules UX_BROWSER=/path/to/chrome node docs/design/s0-prototype/verify.cjs
```

The runner emits `evidence/walkthrough.json` and seven reference-size wireframe PDFs. It traverses
S0-T1–T7 plus substitution, tests semantic outcomes, and inspects layout/focus/failure fixtures. See the
journey packet's Gate-E matrix for limits, N/A reasons and remaining implementation obligations.
Automated/self-walkthrough evidence supplies no Gate-G participant results.

## Gate boundary

C/D deliverables and E evidence live in the same journey packet. F's executable deliverable is ready,
but its formal opening/pass remains pending the two privacy-safe availability and independence/
distinctness attestations required by the validation protocol §4.1. Gate G then needs the two actual
independent sessions. H/I remain unopened; draft PR #470 stays blocked on Gate I.

| Version | Date | Change |
|---|---|---|
| 0.1 | September 30, 2026 | Initial low-fidelity S0 vehicle, captured reference data and reproducible walkthrough. |
