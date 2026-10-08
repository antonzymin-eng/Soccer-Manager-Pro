# P5b screens — implementation and validation

**Created:** October 8, 2026\
**Last Updated:** October 8, 2026\
**Version:** 0.9\
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

## Follow-up review at 5214ab1 — counter validity and host profiling (v0.3 snapshot)

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

## Unity allocation measurement correction (v0.4)

The v0.3 counter-failure procedure above is superseded for Editor execution. Both
allocation tests now select their measurement at compile time:

- `UNITY_5_3_OR_NEWER`: use Test Framework's native
  `UnityEngine.TestTools.Constraints.Is.AllocatingGCMemory()` for a retained 1,024-byte
  positive control, then `Is.Not.AllocatingGCMemory()` for 1,000 unchanged calls.
  The delegate and its captures are created outside recording; the unchanged delegate
  is warmed before recording. Driver outcome assertions remain outside the measured loop.
- Linux shim gate: retain `GC.GetAllocatedBytesForCurrentThread()` with the existing
  positive control and zero-byte assertion. An always-zero counter still fails the control.

Neither lane skips, ignores or marks the test Inconclusive. The Unity lane never reads
Mono's managed byte counter. Both test asmdefs explicitly reference `UnityEngine.TestRunner`,
the assembly owning the constraint. The pinned Test Framework 1.6.0
[API documentation](https://docs.unity3d.com/Packages/com.unity.test-framework@1.6/api/UnityEngine.TestTools.Constraints.AllocatingGCMemoryConstraint.html)
provides this constraint and negation/namespace pattern. No production or package pin changes.

Supplementary checks on October 8 at 18:27 UTC: **99/99 ClientApp fast cases** (same
full-match exclusion) and **8/8 driver cases** pass on CLR 8.0.28 / NUnitLite 3.14.
A separate compile-only check defines `UNITY_5_3_OR_NEWER` and compiles both test inputs
against the downloaded Unity NUnit 2.0.5 binary and official registry Test Framework
1.4.6 constraint sources. Those sources use the same documented 1.6.0 calls. The temporary
native `Recorder` type surface throws from every member and is never executed. This
checks imports, delegate/constraint overloads and NUnit compatibility; it is **not** a
compile against the pinned 1.6.0 package, an Editor compile, or a native GC measurement.

At 18:22 UTC, `5214ab1`'s [CI run 37821405416](https://github.com/antonzymin-eng/Soccer-Manager-Pro/actions/runs/37821405416)
was still running. This revision needs fresh CI. Pinned-host ClientApp and MatchClientCore
EditMode must pass both native positive controls and unchanged-path constraints, with raw
NUnit output retained. The uncompiled skin, Editor-regenerated lock and measured B8
layout cadence remain required; `PitchLayoutFailureCount` must be zero. PR #489 remains
draft and B8/B9b/B10/Gate J remain open.

## Codex review at 9aa1aca — keyboard focus correction (v0.5 snapshot)

Review baseline: `3113d8211a36ad9253e77b399ca579d5a99f4856`. Its
[CI run 37825109785](https://github.com/antonzymin-eng/Soccer-Manager-Pro/actions/runs/37825109785)
passed, including the Linux functional gate; Unity tests were skipped. The three inline
focus findings are confirmed by source tracing against journey §14.3:

- The custom Tab loop ignored navigation modes and included every visible `Selectable`.
  Only controls made by `Control` now register as Tab stops. Heading, feedback and pitch
  anchors remain registered for explicit focus and scrolling, but Tab skips them. UGUI
  navigation stays `None` for buttons too, because this binding owns traversal and arrows.
- Both choosers previously opened on the heading. Entry and invalid-focus recovery now
  target the first active/interactable choice in the appropriate Mentality/outgoing group.
  If no selector survives, Cancel remains a meaningful escape; focusing does not select or
  submit a draft.
- Submit previously indexed the retained row pool's tail. It now indexes the last current
  feedback record after binding, so a second match with less history focuses its active row.

A temporary control-flow harness compiles the actual affected method bodies extracted
from the before/after source. Its lightweight hierarchy, input and EventSystem model is
**not Unity**. Nine cases cover forward/reverse traversal past anchors, modal wrap and
background exclusion, both dialog entries, hidden/disabled selector removal, dialog recovery,
repeat-match Submit with a larger retained pool, and explicit heading/feedback focus.
Before: **1 passed / 8 failed**; after: **9 passed / 0 failed**. The registration call sites
are separately inspected: only `Control` opts into Tab traversal. This is supplementary
logic evidence, not full-skin compilation, native input/scroll/ring validation, or a host QA
pass. The complete modified skin parses as C# 9 with zero syntax errors. Tier policy and
its 11 regression tests pass. No automated Unity test or native run is claimed.

Pinned-host I-Q15 must additionally record:

1. From each page heading, Tab reaches an available action, never a section heading,
   feedback anchor or pitch label; Shift-Tab and modal wrapping preserve action order.
   In Match View with four or more records, Tab goes statistics → earlier feedback →
   report (when available) → pitch entry, per §9.3 plus the v0.9 tail.
   Explicit page/feedback/pitch focus and scroll reveal still work.
2. Open both dialogs with keyboard and pointer: focus starts on the first available
   selector, arrows stage only, Tab stays inside, and Cancel restores the invoker.
   Remove/disable the focused selector and verify a surviving selector receives focus.
3. Submit several requests in one match, Return, start a second match, then submit its first
   request. Focus must land on that request's visible Pending feedback row rather than an
   inactive retained row; subsequent Tab must reach a surviving action.

This correction requires fresh exact-head CI and pinned-host compile/QA. The skin and
Editor-regenerated lock, native allocation tests, `PitchLayoutFailureCount == 0`, 1×/10×
profiling and B8/B9b/B10/Gate-J acceptance remain open. No engine/save/schema/RNG, package
pin, scene or gameplay behavior changes.

## Keyboard pitch inspection and committed focus coverage (v0.6)

The v0.5 action-only traversal removed the previously claimed keyboard access to pitch
player descriptions. This revision restores it as **one composite pitch Tab stop**, rather
than making all 22 markers separate Tab stops. This is read-only inspection of the existing
pitch/detail projection; no new global shortcut, command, pause or navigation edge is added.

The pitch's current visible marker is its sole Tab entry. Right/Down moves to the next
visible marker; Left/Up moves to the previous one, with wrap in stable engine-slot order
across Home and Away. Tab/Shift-Tab leaves the group for a surviving action. Pointer clicks
set the same entry, so keyboard re-entry resumes at that inspected player. Hidden/sent-off
markers are skipped; losing the inspected marker recovers to a surviving marker, or an
available page control when none survive. Modal eligibility blocks background inspection.
Return resets pitch entry. Headings and feedback rows remain explicit anchors outside Tab.
At 9e56abe, `pitch.legend` was expanded with key instructions; v0.7 below restores
the approved wording. Keyboard traversal itself is unchanged.

`S0FocusNavigation` is a consumed host-free ClientApp policy for role-based Tab eligibility,
wrapped movement, first surviving selector and latest current feedback index. The Unity
binding supplies visibility/interactability/modal eligibility and applies EventSystem/scroll
selection. Predicates are cached at composition. Pitch arrow handling returns before any
choice-button click, and selected-player descriptions refresh after rendered visibility is
applied. Native focus-ring/scroll/input behavior still needs the pinned host.

Permanent `S0FocusNavigationTests` contributes **18 cases** to the existing ClientApp test
assembly and Linux gate, covering one pitch entry at first/middle/last players, forward and
reverse Tab exit, all 22 markers in both arrow directions, wrapping/removed markers,
modal background exclusion, no visible targets, initial selector eligibility and retained
feedback indexing. A composed presenter test reads 22 distinct current Home/Away shirt
and goalkeeper descriptions through that traversal, with unchanged tick/command count and
no dialog. Tests cover the production policy/projection; they do not execute UGUI input or
prove that the native binding supplies correct eligibility.

Supplementary CLR 8.0.28 / NUnitLite 3.14 at 19:58 UTC: **118/118 ClientApp fast cases**
pass (same full-match exclusion), including the goalkeeper-description assertion.
Focus tests compile against NUnit 3.5. Generic binding calls
with Button-list/Selectable predicates pass a compile-only type-variance check; five changed
C# files parse with zero C# 9 errors. These are not full Unity skin compilation.
Temporary production-policy mutants prove the new tests detect the relevant mistakes:
removing the pitch Tab entry fails **3/18** focus cases; reverting to retained-pool feedback
indexing fails **3/18**. Committed source is restored/unmutated. The earlier transient focus
harness remains historical supplementary evidence; regression protection now lives in repo.

Pinned-host I-Q15 must Tab into the pitch once, inspect each visible player's full name,
shirt and goalkeeper/substitute cue with all four arrows, then Tab and Shift-Tab out.
Verify wrap, sent-off/hidden-marker recovery, no pitch target when none are visible,
pointer-to-keyboard re-entry, fresh-match reset, modal background exclusion and full-time
report-focus precedence. At 100/150/200% text scale, record focus ring/scroll reveal and
complete description glyphs. Capture unchanged command history/playback state while doing
inspection. No keyboard-support waiver is taken and no native QA pass is claimed.

At 20:01 UTC, 9aa1aca's [run 37832229389](https://github.com/antonzymin-eng/Soccer-Manager-Pro/actions/runs/37832229389)
completed successfully, including its Linux functional gate; Unity was skipped.
This revision needs fresh exact-head CI. Keep PR #489 draft. Pinned Unity compilation,
Editor-regenerated lock, native allocation tests, runtime QA, zero layout-failure count and
1×/10× B8 profiling remain due; B8/B9b/B10/Gate J stay open.

## Approved legend correction (v0.7)

Review confirmed that 9e56abe appended keyboard instructions to `pitch.legend` while
[the owner-accepted binding contract](../design/ux-s0-binding-contracts.md) v0.4 §4
still specified the original text. Restore that exact text:

> H/A identify Home/Away player shirts. A white-outlined substitute marker identifies an applied substitution.

No new player-facing keyboard hint is added elsewhere. Keyboard instructions remain in
the implementation README and I-Q15 procedure. The composite pitch navigation, description
projection and committed focus tests are unchanged. The accepted contract and its approval
remain intact; this is a source correction, not a wording proposal or new owner acceptance.

All production content declarations match 9aa1aca exactly (excluding append-only source
history). At 7270bd6, the compiled formatter again reported the original base hash
`6a6e38939b35ec7ed1f2dced11c3fd0d6822eecf1a98db78545bf31164d41c8c`;
v0.8 below supersedes it after restoring three more approved sentences.
Supplementary CLR 8.0.28 / NUnitLite 3.14 at 20:18 UTC: **118/118 ClientApp fast cases**
pass after recompiling ClientApp, including all focus and read-only inspection cases;
the same long full-match exclusion applies. This does not validate Unity wiring or input.
Fresh exact-head CI is required. PR stays draft and the pinned-host gates remain open.
B8 profiling must also distinguish Editor-only missing-component allocation noise from
player-build GC and include the per-frame focus recovery/visibility work. No performance
acceptance is inferred from source review.

## Codex layout, comparison and complete-copy corrections (v0.8)

Five additional Codex findings at 7270bd6 are corrected:

- Every keyboard-focus ring has `LayoutElement.ignoreLayout = true` before it is
  stretched, so the parent button's VerticalLayoutGroup does not treat the overlay
  as a content child. Strip geometry, pointer behavior and Tab roles are unchanged.
- All seven read-only comparison rows retain separate localized Current and Requested
  tags. BindDialog updates both independently on each structural revision, including
  hidden disclosure content. Equal values show both on the same row; changing the draft
  moves Requested only. Tags are plain noninteractive Text, not new Tab stops.
- Restore the complete approved `context.substitution`, `pitch.description` and
  `statistics.loose_ball` sentences verbatim: immediate application does not wait for
  a stoppage, the baseline pitch describes both attack directions and pitch cues, and
  team possession shares need not total 100%. No approved contract/owner record changes.

An audit of all **141/141** role base patterns against binding-contract v0.4 §§4.2–4.3
reports zero mismatches after these three restorations (103 static and 38 dynamic).
The compiled formatter reports the new canonical hash below; 7270bd6's restored-legend
hash remains historical evidence, not the current shipped-content identity.
Recompiled ClientApp on CLR 8.0.28 / NUnitLite 3.14 at 20:37 UTC passes **118/118** fast
cases with the existing long full-match exclusion. Both changed source files parse
with zero C# 9 errors. No permanent test or assembly is added in this small binding/copy
correction.

A temporary bounded harness compiles the comparison-binding block extracted from the
modified BindDialog against tag-visibility stand-ins and the real presenter/coordinator.
It checks all seven Requested drafts against the current Balanced state, Cancel/reopen
with both tags together, and an Applied Attacking request with no stale Balanced tags.
The prior no-binding variant fails, while the extracted new block passes at 20:38 UTC.
This harness is outside the repo and proves neither UGUI layout nor full-skin compilation;
the committed focus-policy suite still covers its previously stated policy boundaries.

Pinned-host I-Q05 must expand comparison, inspect both tags when equal, change each draft
and verify only Requested moves, then Cancel/reopen and repeat after Applied and a new
match. Tags must remain understandable with selectors scrolled out of view at 100/150/200%.
I-Q15 must toggle keyboard focus on/off for action buttons and capture unchanged button
rect/preferred height, stretched ring bounds and complete text with no reflow/clipping at
all three scales. I-Q09/16 must record the complete substitution and loose-ball copy;
I-Q01/16 must record the complete baseline pitch description before marker inspection.
Capture actual loaded-content hash (including any font fallback) at I-Q16. Fresh CI and
all native compile/lock/input/runtime/profiling gates remain due; PR stays draft and
B8/B9b/B10/Gate J remain open.

## Match View Tab order and contract parity (v0.9)

Review baseline: `dbe8e32e183098cdc1b33bb22ae091a314705eed`.

- **Codex: statistics before history.** `Keyboard()` derived Tab order from
  `GetComponentsInChildren<Selectable>`. The earlier-history disclosure lives in the
  feedback region, which precedes the statistics toggle in the rail, so Tab reached history
  first once a fourth record existed. Journey §9.3 MV-L/P orders "Open/Close statistics →
  any earlier-feedback disclosure". Match View now uses an explicit logical order through
  `S0FocusNavigation.MoveInLogicalOrder`: Slower → Pause/Resume → Faster → Change Mentality
  → Make substitution → statistics → earlier feedback, then View match report and the
  single pitch inspection entry. Unavailable targets are skipped. A focused heading or
  feedback anchor enters the order before its next listed hierarchy successor, so Tab after
  Submit's feedback focus reaches statistics. Other screens and both dialogs keep hierarchy
  order. Visual layout is unchanged.
- **Pitch entry is outside §9.3.** The approved MV-L/P sequence lists no pitch stop, and
  contract §4.3 calls pitch descriptions noninteractive. The pitch entry (added in v0.6) is
  therefore placed after the approved sequence, so that sequence is preserved as a prefix.
  Whether keyboard pitch inspection ships at all needs an owner decision; it is not
  claimed as approved.
- **Permanent contract parity.** New `S0ContentContractTests` reads
  `docs/design/ux-s0-binding-contracts.md` §4.2 (arrow register and Mentality table) and
  §4.3 (dynamic tables), and asserts every compiled role's base pattern verbatim, plus no
  missing or extra roles (141). A second case pins the four semicolon-bearing sentences.
  A Python mirror of the parser yields 141/141 at this head and reports exactly
  `context.substitution`, `pitch.description` and `statistics.loose_ball` at 7270bd6.
- Added focus regressions cover the Codex scenario, anchor entry from the feedback row and
  heading, skipped unavailable report, end wrap and the pitch entry at the end. Expected
  values were checked against a Python mirror of `Move`/`MoveInLogicalOrder`.

**Evidence limits.** No .NET SDK was reachable in this environment, so these C# changes
were not compiled or run locally; GitHub's Linux functional gate is the first execution.
Content and the compiled-content hash are unchanged. Native Unity input remains I-Q15.

## Content and dependency boundary

The published representation is the compiled `S0ScreenContent` table: 141 roles
(103 static / 38 dynamic), with schemas and complete outcome patterns. `S0TextFormatter`
loads that same table through real L2 `Localizer.Resolve` and explicit coverage. Constructor
admission rejects every malformed base/selected pattern and missing base role; absent
selected rows retain KD-5 fallback. Numbers use the fixed invariant provider. Flat names,
shirts, bench slot and Applied minute can be independently reordered; brace-containing
names remain literal data. One-value typed cache paths avoid boxing/argument arrays on
unchanged values. Match teardown drops match caches/identities; shell context is fixed.

Canonical base content SHA-256: `51e1a201cc81a81bd2c5e0e945cbffa8a326cc4a8c8fadf368d33d66941b876d`.
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
   NUnit results. Require passing native allocation positive controls and unchanged-path
   constraints in ClientApp and MatchClientCore; do not substitute the Linux counter or
   an Inconclusive result. A source/package change requires a new SHA and fresh checks.
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
| 0.4 | October 8, 2026 | Unity-native allocation constraints and positive controls replace the managed counter in Editor tests; explicit TestRunner references, Linux passes and compile-only evidence limits recorded. Pinned-host acceptance remains open. |
| 0.5 | October 8, 2026 | Codex focus corrections: action-only Tab traversal, first available dialog selector and active current feedback after repeat matches. Baseline CI, bounded before/after control-flow checks and exact I-Q15 host cases recorded; host gates stay open. |
| 0.6 | October 8, 2026 | Restore keyboard pitch inspection through one composite Tab stop and arrows; consume host-free focus policy with 18 permanent regressions plus a composed read-only description test. Content hash/legend and native I-Q15 cases updated; host gates stay open. |
| 0.7 | October 8, 2026 | Restore the owner-approved pitch legend verbatim and original content hash; retain composite keyboard inspection and permanent focus coverage. Pinned-host/CI acceptance remains open. |
| 0.8 | October 8, 2026 | Five Codex corrections: exclude focus overlays from layout, rebind independent comparison tags, restore three complete approved sentences. All 141 role patterns now match the contract; new loaded-content hash and exact native QA obligations recorded, host gates remain open. |
| 0.9 | October 8, 2026 | Explicit journey §9.3 Match View Tab order (Codex: statistics before history), pitch entry placed after the approved sequence pending owner decision, and permanent 141-role binding-contract parity test. Not compiled locally; host gates remain open. |
