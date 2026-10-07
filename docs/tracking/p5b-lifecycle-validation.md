# P5b lifecycle/identity — candidate validation

**Created:** October 6, 2026\
**Last Updated:** October 6, 2026\
**Version:** 0.4\
**Status:** DRAFT IMPLEMENTATION — full PR gate and pinned-host evidence pending\
**Authority:** [approved implementation plan](p5b-lifecycle-identity-plan.md) v0.2;
[S0 binding contracts](../design/ux-s0-binding-contracts.md) §§2–5.\
**Implementation baseline:** main `7c870dd850acb462a335cdd8f6fbe13c49549215`.

## Implemented candidate

The A–C slice consumes the existing `MatchSessionLifecycle` through one
`ClientMatchCoordinator`. Its lifetime-stable match/report registrations hold a
privately replaceable match context. Start admits setup, copies the real engine
boot mapping, installs the single analytics observer, binds the renderer, navigates
and only then starts playback. Duplicate/in-flight activation creates nothing extra.

S0-approved-demo-v1 provides the exact 36 approved synthetic players, existing
numeric defaults and authored shirts. Its setup uses seed 1, home Human,
away AI/default profile, heading off, Balanced axes with only home Mentality changed.
The engine selects XI/bench. Scalar `AgentPlayerId` is captured under the streamer's
existing tick gate into `LiveAgentCue`; accepted frames own live occupant identity.
Authored shirts follow incoming players. Interpolation snaps on PlayerId replacement;
neutral clients explicitly retain slot shirts and NO_PLAYER_ID (real id 0 remains valid).

`SessionMatchAnalytics` synchronizes observation/publication with tick gate → analytics
gate lock order. A partial observation fault marks the publication incomplete before
unlock/disarm; attempted/completed ticks, observed count and last complete-result cutoff
remain distinct. No session Stop/ServiceOnce occurs under the analytics lock.

An accepted ended frame freezes Match View. Stop/join precedes one ServiceOnce which
drops end residue without inventing command logs. Report projection retains immutable
analytics plus applied/refused command evidence. Showing it requires the explicit
named report action. Return clears references/bindings before the named menu Pop;
old renderer callbacks cannot reach a replacement match.

`MatchClientBehaviour` shares idempotent initialization between Awake and external
Attach, owns only generated visuals, hides them before deferred Destroy, and reports
active failures to the owner. Internal demo fields/ownership are removed. The isolated
Scene.unity has `DevelopmentMatchHost`, consuming the same coordinator and approved
fixture. Player-facing Start/report controls remain withheld pending #49 L2 and the
complete localized screen slice. No new navigation edge, mutable global context,
schema, RNG stream/draw site, gameplay tuning or calibration change is introduced.

## Measured host-free evidence

.NET SDK 8.0.425 / CLR 8.0.31 on this Linux worker; generated C# 9 projects from the
checked-in asmdefs. Full generated solution compilation succeeded at source commit `816dfede`
(74 projects, 37 test assemblies; Unity rendering assembly intentionally excluded):
zero warnings and zero errors. Later evidence-record edits change Markdown only.

The worker exposes host `/proc` with namespace process IDs, which broke .NET CLI/MSBuild
process metadata and initial VSTest initialization. Local compilation/testing used a
temporary, external native shim mapping only the current process's `/proc/<namespace-id>/...`
reads to `/proc/self/...`; it changes no repository/runtime simulation code. Initial
NUnitLite 3.14 runs were followed by all five standard `dotnet test --no-build
--no-restore` runs on the rebuilt solution, using the generated NUnit3 adapter and
TRX results. All five pass with the counts below. This is non-certifying affected-suite
evidence and is not a full normal PR gate pass.

| Assembly | Passed | Failed | Skipped |
| --- | ---: | ---: | ---: |
| ClientApp.Tests | 43 | 0 | 0 |
| MatchClientCore.Tests | 182 | 0 | 0 |
| MatchViewer.Tests | 75 | 0 | 0 |
| MatchAnalytics.Tests | 59 | 0 | 0 |
| UiFramework.Tests | 50 | 0 | 0 |
| **Total** | **409** | **0** | **0** |

New tests consume real sessions and cover single/reentrant Start, cancel/re-entry,
real engine full-time transition and streamer capture, explicit report/return,
applied/refused/dropped command evidence, two-session dispatch, stale callbacks,
partial attachment/detachment and active paced-session failure, home/away real
substitution frames, real id 0 versus neutral sentinel, all seven Mentalities,
fixture repeatability and observer neutrality, locked mid-tick publication failure,
and streamer disarm with continuing frames. The bounded full-time tests invoke the
existing internal transition seam in a valid Input phase; they do not claim a full
324,000-tick simulation or complete-match outcome comparison.

Fixture SHA-256 (canonical UTF-8 content including revision, clubs, player fields,
attributes, weak foot and shirts):
`769dc27f8941991862acf88a7adf92367f3624af2b2471007023d9e7b5f5da79`.

Balanced/no-command 120-tick snapshot digest, repeat with analytics attached:
`654AB49662F5DB4858DBE488AC1CE26372100C9C5F0949C4B35A21FC2ACF68F0`.
Same seed/tactics/manager settings without squads, 120 ticks:
`47637733814014A4B1B1ACE5D6165BAD4BD18730A2E5C8BC57D8F7A477C6DA30`.
Distinct squads deliberately change the simulation input; this short comparison
neither claims former no-squad outcome identity nor authorizes calibration changes.

Assembly-tier checker passes: 38 production folders, 165 references (117 downward,
43 intra-tier, zero upward, five infrastructure), acyclic. The sanctioned analytics
consumer lock passes with the actual client/core/report references. Metadata,
cross-document consistency and diff hygiene are checked separately on the candidate.
Recurring-defect lint remains a survey with existing repository findings; none of
its existing debt is treated as a new P5b acceptance claim.

## Resumed validation — October 6, 2026

The interrupted workspace retained source commit `816dfede` and its Markdown-only
validation update `fc0a924f`, with a clean working tree. Its temporary SDK and raw
TRX files did not survive the worker restart. The 409-case record above describes
that earlier execution; it is not a rerun of the continued candidate.

`S0DemoFixtureComparisonTests.sim_ApprovedFixtureFullMatchIsRepeatableAndObserverNeutral`
now runs in the normal PR suite. Four sequential real full matches cover the approved
fixture, an independently constructed repeat, analytics attached versus unobserved,
and the former no-squad setup with otherwise identical seed/tactics/managers/heading.
It requires actual full time without the bounded-test transition seam, final chained
digest and score equality for the first three, and complete healthy analytics/score
agreement for observed runs. It prints the fixture hash, final ticks/digests and all
existing basic/advanced team fields using invariant formatting. No outcome target or
calibration baseline is imposed on the deliberately different no-squad input.

Execution is **PENDING** until the fresh PR gate produces a passing TRX containing
`P5B_FULL_MATCH PASS` and all four labelled outcomes. The local worker currently has
no .NET SDK or connected Unity Editor. Static dependency/document/metadata checks
can run here; CI is the available compile/full-match runner. Three Unity Update
entry points now have explicit profiler scopes; their pinned-host checks remain due.
The recovered #20 v1.17 history row was moved out of the inline region example
into the actual §3.11 history table before publication; no normative rule changed.

Resumed static checks pass: assembly tiers (38 production assemblies, acyclic,
zero upward references), cross-document consistency, tracking chains/counts
(33 active / 60 archived), managed metadata integrity, and full candidate diff
hygiene. All 343 Python tooling tests pass with `/usr/bin/git` first on PATH.
The worker's `/usr/local/bin/git` fails the unchanged offline-LFS snapshot test
with “remote end hung up unexpectedly”; the standard Git rerun passes without
any test or hook edit. These tooling results do not compile the new C# test.

The candidate is locally committed; branch publication and draft-PR creation
remain pending. Fresh normal CI and the complete-match result remain unexecuted.

## Published-head review and corrections — October 6, 2026

The sections above are the historical record at `816dfede`/`6617ae33` and are not
rewritten. The candidate was later published as `ui/p5b-lifecycle-identity` at
`50e6e5a` (one commit; tree `70b9e2f4`, identical to `6617ae33`). No PR or Actions
run existed for that head at review time.

**Independent run at `50e6e5a`.** .NET SDK 8.0.131 (Ubuntu package),
`tools/dotnet-ci/run-gate.sh --owner-held-red report-only`. 36 of 37 test assemblies
completed; all passed except `ClientApp.Tests` (43/44):
`sim_ApprovedFixtureFullMatchIsRepeatableAndObserverNeutral` failed with
`Expected: 324001 But was: 324000`. `MatchEngine.Tests` did not finish before the
worker's 60-minute limit and is **not** evidence. The run is non-certifying.

**Corrections in this revision.**

- *Full-match end tick.* The clock advances before Input checks full time, so the
  ended frame is `MATCH_TICKS_TOTAL` (324,000), not +1. The test bound/assertion and
  its comment are corrected. With only that change, `ClientApp.Tests` passed 44/44,
  including all four full matches.
- *Restored `BootRoster`.* The boot and restore paths share the session constructor,
  so a session restored after a substitution recorded current occupants as starters,
  and `MatchIdentityContext` then rejected the duplicate bench origin. A session now
  captures the descriptor only when neither team has used a substitution. Otherwise
  `BootRoster` throws `InvalidOperationException`, while `RestoreFrom` and resumed
  playback are unchanged (existing replay scenarios restore after substitutions).
  The engine re-derives the replaced starters internally at restore but exposes no
  query for them. Exposing one is a possible follow-up; no save-format change is made.
  New tests cover restore before any substitution (identical starter/bench mapping),
  restore after a substitution on either team (restore and playback continue, the
  substitute stays on the pitch, `BootRoster` and `Bind` refuse), and a fresh session
  keeping its boot starter after a substitution. With the guard removed, both
  after-substitution cases fail.
- *Cue mapping test.* `LiveAgentCue`'s positional test now passes a distinct
  `playerId` and asserts it.
- *Visible identity effect.* Shirt labels remain deferred, but frame identity already
  changes rendering: interpolation snaps a replaced occupant instead of blending.

**Corrected tree, affected suites** (same SDK, `run-gate.sh --fast` with a test
filter; `sim_` excluded): ClientApp 46/46, MatchClientCore 180/180, MatchViewer 73/73,
MatchClientWeb 58/58. Full `ClientApp.Tests` including the full-match test: 47/47
passed (about 15 minutes on this shared worker). The full normal PR gate, including `MatchEngine.Tests`, is left
to PR CI.

## Before merge — exact review head

- [ ] Normal PR CI functional and policy gates pass at the final review head.
- [ ] On the owner Windows 11 host, clean exact-head checkout, confirm Assets/Scripts
  junction and Unity **6000.4.9f1 (f7258d6eebbe)**; force recursive reimport/compile,
  retain Editor.log and report zero errors with warning disposition.
- [ ] Run relevant ClientApp/core EditMode cases on that head, including the new
  lifecycle, real identity and analytics failure tests. Record test XML and counts.
  The existing Unity repository-root-discovery issue remains separately tracked.
- [ ] In Play mode, attach while Match View is inactive and before renderer Awake;
  later activation creates no duplicate objects and starts no second playback loop.
- [ ] Verify partial prefab-construction failure is terminal and stops owned playback;
  active rendering rejection/destruction also stops it. Unattached renderer is inert.
- [ ] Detach twice, then attach again in the same frame: old generated root is hidden
  immediately, destroyed after the frame, and never visible beside the new root.
  Authored camera/pitch/prefabs/materials survive. Disable/enable changes visibility only.
- [ ] Exercise full-time freeze, explicit report, return and second Start through the
  coordinator development harness. The same registrations/flow survive; old frames,
  latches, commands, analytics, identity, report and failure callbacks are cleared.
- [ ] Smoke Scene.unity's explicit development host with approved distinct identity;
  confirm successful prefab wiring, fresh generated container and quit/destroy cleanup.
- [ ] Record complete-match fixture repeatability, observed versus unobserved digest,
  and outcomes against the former no-squad setup using otherwise identical inputs.
  Pin fixture revision/hash, setup, code head and comparison. No frozen corpus or
  calibration reference update is authorized by this slice.

#49 L2, complete four-screen bindings/copy/reflow, command outcomes/choosers/focus,
B8/B9b/B10 and Gate J remain open. Prior #470 host evidence applies to its foundation,
not this changed source tree. No merge or host acceptance is claimed here.

## Post-merge Unity compile fix — October 6, 2026

Unity 6000.4.9f1 (bundled NUnit 3.5) rejected `src/client-app/tests/S0DemoFixtureComparisonTests.cs(22,6)` with CS0246: `[NonParallelizable]` arrived in NUnit 3.7, and PR #482 (`50e6e5a6`) added it. The Linux shim gate (NUnit 3.14) accepted it, so main stopped rebuilding `TacticalDirector.ClientApp.Tests.dll` in the governing editor while CI stayed green.

`S0DemoFixtureComparisonTests.cs` v1.2 drops the attribute and does not replace it. Neither runner opts into parallel execution: no `[Parallelizable]`, `LevelOfParallelism` or worker-count setting exists in `src/` or `tools/dotnet-ci/`, and NUnit runs fixtures serially by default, so the attribute was inert. A sweep of `src/` for other post-3.5 APIs (`Parallelizable`, `Assert.Multiple`, `Assert.Warn`, `DefaultFloatingPointTolerance`, `FixtureLifeCycle`, `Does.Not.Contain(non-string)`) found none.

Pinned host, Unity 6000.4.9f1, clean detached checkout of `0a236ed4` (main `fc66c43f` + this fix; the landing commit amends it with Markdown only, so the compiled input is identical) with `Assets/Scripts` as the `src/` junction, forced `AssetDatabase.ImportAsset("Assets/Scripts", ImportRecursive | ForceUpdate)`: `Tundra build success (5.79 seconds), 19 items updated, 1151 evaluated`, `TacticalDirector.ClientApp.Tests.dll` rebuilt and copied, **0 `error CS`**; warnings only the two known, deferred CS0618 `GetInstanceID()` at `ClientShellBehaviour.cs` (143,34)/(151,17). Unity EditMode tests were **not** run in this pass. This is compile evidence only; every unchecked item above stays open.

## Version History

| Version | Date | Notes |
| --- | --- | --- |
| 0.1 | October 6, 2026 | Records consumed A–C candidate, actual bounded Linux evidence and explicit exact-head host/full-outcome prerequisites. |
| 0.2 | October 6, 2026 | Recovery record, normal-suite real full-match comparison and explicit pending execution; Unity profiler scopes and host checks. |
| 0.3 | October 6, 2026 | Published-head review: independent run at `50e6e5a`, full-match end-tick fix, restored-session `BootRoster` refusal with mirrored tests, cue `PlayerId` assertion; historical sections preserved. |
| 0.4 | October 6, 2026 | Post-merge Unity compile fix: NUnit 3.7 `[NonParallelizable]` removed from the full-match comparison test; pinned Unity 6000.4.9f1 compile 0 errors; EditMode/Play checks still due. |
