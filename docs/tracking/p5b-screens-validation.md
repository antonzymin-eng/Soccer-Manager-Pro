# P5b screens — implementation and validation

**Created:** October 8, 2026\
**Last Updated:** October 8, 2026\
**Version:** 0.3\
**Status:** IMPLEMENTED CANDIDATE; Unity/host acceptance BLOCKED in this environment\
**Purpose:** Record the consumed four-screen slice and concrete evidence required before B8/B9b/B10 and Gate J sign-off.\
**Authority:** [S0 journey](../design/ux-s0-pm1-journey.md) §14; [binding contracts](../design/ux-s0-binding-contracts.md) §§3–5.\
**Base:** main `574b0344db3cfe24ee6bef5db459e1b34e6365f4`, after L2 PR #487 merged. The coordinator's pinned-host acceptance is recorded separately in [lifecycle validation](p5b-lifecycle-validation.md) v0.5; that evidence is not carried forward as a screen compile.

## Consumed implementation

`S0ScreenPresenter` in gate-compiled ClientApp owns staging, enablement, playback steps,
request outcomes, statistics/disclosures and localized text. `S0ScreensBehaviour` binds
persistent UGUI controls beneath the four authored roots. The tracked `Scene.unity`
starts in Main Menu and composes the existing coordinator; the development host component
is removed from that scene. The development helper source remains available for isolated
lifecycle investigations. Start/report/Return use the five existing named navigation moves.
The coordinator remains the only lifecycle owner; full time freezes Match View until an
explicit report action, then Return clears the match before a new setup.

Home Mentality and substitution dialogs stage only. A session-owned request adapter
registers before dispatch, admits one Pending request per kind, and consumes only new
applied/refused suffixes with full payload matching. Captured names/shirts/bench origins
remain immutable in history. Local dispatch failure remains Send failure; refusal is
necessarily generic. Paused requests never call ServiceOnce. At the coordinator's end
barrier, successful evidence remains Applied and only unmatched Pending becomes Not applied.

Statistics use one synchronized analytics publication per refresh interval (physics clock),
with immediate fault/end refresh. Score/period/end come from accepted frames. No shots row
or unavailable xG is fabricated; loose-ball possession is not renormalized. The cutoff
caption uses `ResultThroughTick`, the cutoff of the retained trustworthy result, rather
than later completed observations which may never have been included in that result.
Live disclosure persists at full time; incomplete report partials start closed, and Return
is outside them. Observer faults do not stop playback or report/navigation.

Pitch labels follow identity-matched rendered positions after renderer Update, and snap
with the existing identity-sensitive interpolation. Measured label collision placement
preserves full marker labels and tethers them to their original markers. Pointer/keyboard
selection exposes the complete localized name/shirt/cue description. Applied substitutes
have a white annulus using the existing stroked prefab plus an explicit text cue. Text
scales uniformly at the admitted 100–200%, without autoshrink. Measured columns stack when
necessary; scrollable pages/dialogs preserve focus and disclosure. Keyboard Tab order
follows the actual visible hierarchy after history moves between match and report.
These are implemented mechanisms, not an executed font/layout/input acceptance claim.

## PR #489 review corrections — October 8, 2026

Review baseline: `d2788f15ee6aa7cbd62a3858e14374ac105b3333`. Static tracing confirms
that an undersized label rectangle could throw through LateUpdate into terminal renderer
rejection. The reported first-frame timing trigger has **not** been reproduced in Unity here.
The binding now forces pending UGUI layout after activation/structural or viewport changes
before reading geometry. It measures label text only when that string or available width
changes (font and scale are fixed at composition). A separate label surface can grow into
vertical page scroll; its child image always retains 105:68 pitch aspect and world positions.
Capacity failure returns false, never throws. If near-marker greedy placement exhausts
cells, the pure layout retries all labels on a complete grid; tethers preserve marker identity.
A remaining placement failure emits one warning per failure episode and increments the
binding's `PitchLayoutFailureCount`, leaving match lifetime intact. A nonzero counter fails
host layout acceptance; ordinary unready geometry is deferred until layout is available.
Invalid arguments/content/integration exceptions still fail loudly through their existing paths.

`MatchClientDriver.TryGetCommandOutcomes` compares append-only counts and copies both logs
under one lock only when they change. The presenter consumes those snapshots, retains its
outcome cursors, and uses the existing frozen report logs at the end barrier. Ordinary
accepted frames update the frame without invalidating structural bindings; score, minute,
period, occupants/choices, command outcomes, playback, disclosure and analytics refreshes
still invalidate the UI when needed. Pitch projection remains per render frame. Control,
selection, feedback and statistics text references are retained instead of repeated searches.
Feedback hierarchy is explicitly heading → earlier toggle → earlier chronological rows →
latest chronological rows on both Match View and Report, including a second match. Hidden
earlier rows remain hidden until disclosed; disclosure and focus ownership are unchanged.

Two presenter regressions fail against the original production assemblies: an ordinary
accepted tick changes structural revision (expected 4, observed 5); 1,000 unchanged
refreshes after one settled command allocate **104,000 bytes**. Both pass with the fixes;
the latter measures **0 bytes** on the same supplementary CLR. This is a targeted
allocation result, not UGUI profiling or B8's 60 FPS proof. #20 §3.3.4's client allocation
carve-out still governs; no claim of whole-client zero allocation is made.

| Review check | Result | Limit |
| --- | --- | --- |
| ClientApp affected fast regression | 99 passed / 0 failed / 0 skipped; 09:51:50 UTC, 0.512 s | Filter `test !~ S0DemoFixtureComparisonTests`; full-match scenario remains in canonical CI |
| Driver regression fixture | 8 passed / 0 failed / 0 skipped; 09:50:05 UTC | FIFO/refusal/end-drop plus conditional atomic/immutable snapshot and unchanged-poll allocation tests |
| Layout regressions | Included in ClientApp run | Unready 0/100 geometry, nonfatal insufficient capacity, all 22 enlarged labels on grown grid, nonoverlap and unchanged markers; no actual font/render evidence |
| NUnit 3.5 compatibility | Changed ClientApp tests and driver fixture compile | Uses repository shim, not Editor compilation |
| Canonical local PR gate | Surveys, metadata and 74-project generation pass; restore aborts exit 134 in `Process.GetStat` / `StartTime` | Re-attempted on corrected source; no local canonical test execution |
| Changed C# syntax | 9 files parsed with C# 9, 0 errors | Unity member resolution excluded |
| Assembly tiers / metadata / package-policy tests | PASS / PASS / 11 passed | Existing package pin/lock policy unchanged |
| GitHub CI at review baseline | Linux functional and other checks passed; RFC link-check retry passed, run 37729959539 | Unity job skipped; corrected-head CI must run again |

The manifest and hand-authored lock are deliberately unchanged in this correction.
Only the pinned Editor can resolve/regenerate them and confirm the exact dependency graph.
No Unity provider or Editor is available here. The new child-image layout, first-frame
activation, density/reflow, Tab chronology and repeat-match caches remain explicit host
checks. Keep PR #489 draft; B8/B9b/B10 and Gate J remain OPEN.

## Follow-up review — counter validity and host profiling

CI for `ac9a0ac6a92a7daa7893c35042771cd5cbd73efe` is now complete:
[run 37760302461](https://github.com/antonzymin-eng/Soccer-Manager-Pro/actions/runs/37760302461)
passed the Linux functional gate and all lint/link/format/metadata/spec checks. Unity tests
were skipped. This evidence belongs to that head; the following test correction needs
fresh corrected-head CI and pinned Editor execution.

Both allocation tests now first retain a known 1,024-byte array and require the runtime
counter to detect that allocation. An always-zero counter fails explicitly before any
zero-allocation assertion; it cannot silently certify the presenter/driver. The probe is
outside the unchanged-path measurement. No test is skipped or disabled. Unity's official
[UUM-100690](https://issuetracker.unity.com/issues/5660/crash-on-runtimefieldinforesolvetype-with-il2cpp-and-returns-0-with-mono-when-calling-gcgetallocatedbytesforcurrentthread-method)
records this problem in older releases and marks the 6.1/6.2 tracks fixed; that status is
not a direct observation of the pinned 6000.4.9f1 Mono host. If the control fails there,
record the counter failure and obtain Unity Profiler allocation evidence; do not describe
that run as a passing allocation measurement or remove the assertion.

Supplementary counter check, October 8 at 17:58 UTC: ClientApp fast filter
`test !~ S0DemoFixtureComparisonTests` passed **99/99**, and the driver fixture passed
**8/8**. In temporary test-source copies only, replacing every counter read with `0L`
made both original tests falsely pass and both corrected tests fail at the exact positive
control assertion. Production and committed test sources were not mutated. These results
remain CLR 8.0.28 / NUnitLite 3.14 evidence, not a Unity counter/skin/performance result.

The narrow-label statement needs qualification: the binding limits the text rectangle,
retains the entire string, uses `HorizontalWrapMode.Wrap`, and reads `preferredHeight`
**after** setting its width. There is no substring/ellipsis or font-size reduction. Unity's
[UGUI Text implementation](https://github.com/Unity-Technologies/uGUI/blob/main/com.unity.ugui/Runtime/UGUI/UI/Core/Text.cs)
measures preferred height against that current width. This supports the wrapping intent;
it does not prove the selected font fits an arbitrarily narrow rectangle. The pinned-host
font test below must verify complete glyphs and adequate height with a deliberately narrow
pitch and expanded marker text. No native font/render result is claimed here.

The remaining forced-layout cadence is intentionally unmeasured. Every structural rebind
marks pitch layout dirty, and statistics publish once per simulated second, so 10× playback
can drive approximately ten such refreshes per real second. No speculative cadence/observer
change is made before profiling. B8 must record actual forced-layout/bind/publication counts
and CPU/GC/frame results at 1× and 10×, rather than infer performance from this rate.

## Content and dependency boundary

The published representation is the compiled `S0ScreenContent` table: 141 roles
(103 static / 38 dynamic), with schemas and complete outcome patterns. `S0TextFormatter`
loads that same table through real L2 `Localizer.Resolve` and explicit coverage. Constructor
admission rejects every malformed base/selected pattern and missing base role; absent
selected rows retain KD-5 fallback. Numbers use the fixed invariant provider. Flat names,
shirts, bench slot and Applied minute can be independently reordered; brace-containing
names remain literal data. One-value typed cache paths avoid boxing/argument arrays on
unchanged values. Match teardown drops match caches/identities; shell context is fixed.

Canonical base content SHA-256: `6a6e38939b35ec7ed1f2dced11c3fd0d6822eecf1a98db78545bf31164d41c8c`.
The hash format is the ordered UTF-8 sequence `key|schema|pattern-length:pattern\n`.
The real binding exposes `LoadedContentSha256`. Packaged-font admission applies equivalent
punctuation fallbacks before Resolve/validation/hash; host evidence must record the actual
loaded hash when fallbacks change a pattern. Missing mandatory text characters reject
composition instead of publishing blank copy. No locale switch, settings persistence,
procedural producer identity, RNG draw or #49 schema change is introduced.

`client-app` and the Unity skin are the only newly admitted production localization
consumers. The reverse-reference test admits those exact paths and retains the simulation
ban. Direct asmdef references are declared for named contracts. The skin adds built-in
`com.unity.ugui` 2.0.0 / `UnityEngine.UI`, the Unity 6000.4 core version. Manifest and candidate
lock are paired; the candidate lock was authored from built-in package dependency metadata,
**not generated by an Editor here**. Pinned-host package resolution/Editor regeneration is
required before merge. The tier checker resolves that assembly only from the exact skin
asmdef with the matching package pin and both module dependencies. Package metadata is
bound into graph evidence; unknown references, project tier rules and cycles still fail.

## Initial candidate evidence (v0.1 authoring snapshot)

Ubuntu 24.04.3, SDK 8.0.422 / CLR 8.0.28; direct Roslyn C# 9 against netstandard2.1
and the repository Unity shim, NUnitLite 3.14. This is supplementary, non-certifying
execution. The normal dotnet CLI is broken in this worker; with the SDK on PATH the canonical local gate reaches `dotnet restore`, then aborts (exit 134) in `Process.GetStat` / `Process.StartTime` before compilation/test execution. Checklist/schema surveys, metadata and project generation pass. At that authoring snapshot no canonical PR
functional gate or Unity compile was claimed; the subsequent baseline CI result is recorded above.

| Check | Result | Limit |
| --- | --- | --- |
| Dependency compilation from real asmdefs | 28 assemblies compiled | Unity-only skin excluded |
| Focused P5b tests | 48 passed / 0 failed / 0 skipped | Presenter/request/content/layout, no UGUI execution |
| Broader ClientApp candidate run | 93 passed / 0 failed / 0 skipped, 484.466 s, 04:27:16–04:35:20 UTC | Includes complete-match comparison, before the two added font/bounds cases |
| Final changed-source fast regression | 94 passed / 0 failed / 0 skipped, 04:47:21 UTC | `test !~ S0DemoFixtureComparisonTests`; long comparison passed in the earlier full run; fresh unfiltered PR CI remains due |
| Complete final ClientApp assembly | 95 passed / 0 failed / 0 skipped, 497.949 s, 04:46:44–04:55:02 UTC | Includes the long full-match comparison and both added font/bounds cases; supplementary Linux only |
| Localization | 67 passed / 0 failed / 0 skipped | Includes consumed-client reverse-reference lock |
| NUnit 3.5 compatibility | ClientApp and Localization test sources compile | Supplementary signatures, not Unity compilation |
| Assembly package-policy tooling | 11 tests passed | Valid pin plus missing/mismatched/foreign/unknown negative cases |
| Assembly tier graph | PASS, no upward reference/cycle | Pin metadata checks do not prove package import |
| New C# source syntax | Roslyn parse PASS | Does not resolve Unity members |
| Scene references and metadata | PASS: 31 unique scene objects, four root activation/parent mappings, shell/skin script and camera references; metadata integrity PASS | Does not prove Unity import/runtime |

Focused command: NUnitLite ClientApp filter `class =~ S0Screen or class =~ S0Text or
class =~ ClientChangeRequestsTests`, `--workers=0`. The earlier full ClientApp run has no filter; the final fast regression filter is recorded above. An attempted final unfiltered rerun ended without a test summary/result artifact. The completed final run used `class != S0DemoFixtureComparisonTests`: this unqualified exclusion matched no fully qualified class, so all 95 cases executed, including `S0DemoFixtureComparisonTests.sim_ApprovedFixtureFullMatchIsRepeatableAndObserverNeutral`. Its XML reports 95 passed / 0 failed / 0 skipped. Localization runs its full assembly. The canonical entry point remains
`bash tools/run-tests-local.sh --pr`; GitHub Actions must execute it against the published
head. No tests are marked Explicit, disabled or removed in this slice. A failed new font
assertion initially checked the legend rather than the substitute-marker role; correcting
that oracle gives 48/48 without changing production behavior.

The published source commit is `0643b71bd63d3b65fd79029d2b76ef9415dc17a8`; this final evidence update changes Markdown only. The connected GitHub publication tree matched the local tree exactly (`878bf7302fb544c06e2a8a6ba1386afc94152e3d`). At that snapshot a final PR CI pass remained due. The baseline subsequently passed with the external link-check retry above; the corrected published PR head is the required fresh CI and pinned-host target.

## Acceptance matrix and pinned-host procedure

All **real-client I-Q01–19 remain NOT RUN**, and B8/B9b/B10/Gate J remain OPEN.
Supplementary results below cover portions of the contracts, not their complete dual lanes.

| Contract | Supplementary evidence | Required host evidence |
| --- | --- | --- |
| I-Q01–04 | Setup reset, waiting/no advance, seven Mentalities, owner speed ladder/physics clock | Real Start/repeat activation, awaiting-first-frame transition, playback/focus |
| I-Q05–10 | Draft cancel, paused paired requests, repeated payload, real drain refusal, stale pair, keeper/bench, end settlement | Both actual dialogs, duplicate guard, keeper/used/sent-off/cap choices, whistle focus |
| I-Q11–14 | Real synchronized stats, forced observer exception before/after healthy publication, continued frame/report/Return/repeat | Real observer-fault UI, no healthy partial interval, retained/live and closed/report disclosures |
| I-Q15 | Hierarchy/modal focus implementation only | Keyboard-only full journey, Shift-Tab wrap, arrows/Enter/Escape, scroll reveal, no tick reset |
| I-Q16 | Actual compiled content loader, schema negatives, KD-5, reordered names/numbers, cache/teardown, measured collision bounds | Actual font metrics and matching validated/package/load hash; expanded pseudo content and full labels at 100/150/200% |
| I-Q17–18 | Equivalent glyph fallback admission and dense-label geometry | All glyph failures, neutral fallback, measured contrast, real unusual score/cap/history/fault layouts |
| I-Q19 / B8/B9b/B10 | No host/cert evidence in this slice | Exact-head pinned Unity compile, player/runtime smoke, 60 FPS and required FR-PO-052-class capture |

1. Check out the published final PR SHA on Windows / Unity **6000.4.9f1**; record the
   full SHA, clean source tree, Editor revision, DX11/Mono host and evidence paths. Preserve
   Console baseline. Confirm the existing `Assets/Scripts` junction and built-in UGUI 2.0.0
   package resolution; let the Editor regenerate the lock and review any change.
2. Force recursive import/compile and retain Editor.log with import/reload completion,
   errors and warnings. Run ClientApp and Localization EditMode plus affected coordinator/
   analytics/core/framework regressions. Respect the existing full-match timeout; retain raw
   NUnit results. A source/package change requires a new SHA and fresh checks.
3. Open the enabled `Assets/Scenes/Scene.unity`: verify Main Menu only, four exclusive roots,
   one EventSystem and no development-host duplicate. Run I-Q01–14 serially (process-static
   EventBus requires one engine at a time). Confirm viewport pitch coordinates, authored
   markers/white substitute outline, first-frame waiting and activation after forced layout,
   report acknowledgement and a
   second match with no old names/outcomes/callbacks.
4. Run I-Q15–18 at **1366×768, 1920×1080, 2560×1440**, display zoom recorded, each at
   **100/150/200%**, with packaged font, expanded pseudo content, long names, reordered
   substitution output, full history, real unusual score and observer fault. At base size,
   the two larger dimensions must show the whole pitch without scrolling; 1366×768 may
   scroll. No clipping, autoshrink, distorted pitch, missing context or unreachable action.
   Freeze text scale per shell construction; record real supported maximum and loaded hash.
   Verify child pitch image aspect, tether visibility and full label-surface growth at dense
   200% layouts. Record `PitchLayoutFailureCount` (must remain zero) and Console warnings.
   Expand earlier feedback before/after Report and in a second match; visual and Tab order
   must agree and retained label caches must never produce blank repeat-match markers.
   Also deliberately narrow the pitch rectangle and expand marker text: verify the entire
   string/glyph set wraps, its measured height grows, and no glyph disappears or autoshrinks.
   A font/layout failure is a failed I-Q16 case even if the simulation continues.
5. Retain target build/runtime and cert-host profiling artifacts under the existing B8/B10
   procedures. An editor FPS reading, shim outcome or browser screenshot is not a certificate.
   Compare 1× and 10× playback with statistics closed and open, including the 200% stacked
   layout. Capture actual `Canvas.ForceUpdateCanvases`/structural-bind/analytics-publication
   cadence per real second, UI/layout and presenter CPU time, GC allocations and frame-time
   distribution under the existing cert-host procedure/budgets. Separate resize/activation
   spikes from sustained playback; retain raw captures and `PitchLayoutFailureCount` (zero).
   Record product/runtime acceptance explicitly; do not close Gate J from these unit results.

## Version History

| Version | Date | Notes |
| --- | --- | --- |
| 0.1 | October 8, 2026 | Four consumed screen candidate, supplementary validation and exact-head host acceptance procedure; all host gates remain open. |
| 0.2 | October 8, 2026 | PR #489 review fixes: nonfatal/growable label layout, changed-outcome snapshots, structural refresh and stable chronological feedback; before/after allocation evidence and baseline CI retry recorded, all host acceptance remains open. |
| 0.3 | October 8, 2026 | Follow-up review: known-allocation controls reject always-zero runtime counters; ac9a0ac CI completion, wrapping qualification and explicit 1×/10× host layout profiling cases recorded. Unity/runtime/cert acceptance remains open. |
