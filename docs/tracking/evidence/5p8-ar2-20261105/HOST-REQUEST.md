# PR #539 pinned Unity host request

Created November 16, 2026. Purpose: owner-run D-01a validation of the P7 career bindings,
§5-P8 transfers/finances and AR-2 source tree before PR #539 can leave draft.
Status: **RUN November 16, 2026 — PASS.** Receipt: [`host-20261116/`](host-20261116/README.md)
(conflict-free merge of main `39de4662` into the PR, `src` tree `967655cc`). The Linux gate and the
skipped GitHub Unity job do not count as Unity certification.

## Which commit

PR #539 currently **conflicts with `main`** (head `34f29b843b0f1ffb67bc5112030a5cc69c7e606a`,
merge base `b6505350`). Validating that head would be invalidated by the conflict resolution,
because main has moved since then (#543–#546 and others). Resolve the conflict first, then run this request
on **the full exact commit named in PR #539's host-request block**, not on the moving `work`
branch. If a later commit changes anything under `src/`, `Packages/` or `ProjectSettings/`,
this run is superseded and must be repeated. Commits that touch only docs or evidence keep it valid.

## What the run covers

Changes in `src/` against the merge base:

| Area | Changes | Unity coverage |
|---|---|---|
| `src/match-client-unity/` | `ClientShellBehaviour.cs`, `S0ScreensBehaviour.cs`, new partial `S0ScreensBehaviour.Career.cs` (+ `.meta`), asmdef adds a `TacticalDirector.SeasonSave` reference | **Compile only.** This assembly has no EditMode test assembly |
| `src/client-app/` (+ tests) | Career coordinator, transfers, screen text, visibility, shell wiring validator, S0 formatter | `TacticalDirector.ClientApp.Tests` |
| `src/season-save/` (+ tests) | Career drift diagnostic, daily revenue, finance loop, T2 transfers | `TacticalDirector.SeasonSave.Tests` |
| `src/transfers/` (+ tests) | Preflight / typed refusals | `TacticalDirector.Transfers.Tests` |
| `src/club-finances/` (+ tests) | T0 / T2 bootstrap | `TacticalDirector.ClubFinances.Tests` |

`TacticalDirector.MatchClientCore.Tests` and `TacticalDirector.UiFramework.Tests` are included
as the client regression set used by earlier host runs (#518, `host-963f48ee-20261028`).

## Prepare an isolated checkout

On the pinned Windows 11 / Unity **6000.4.9f1** host, use a new detached worktree with no
existing `Library`, so the import compiles all source. Do not delete the `Library` of a
working project or change its settings. Record `git rev-parse HEAD`, `git status --short`,
the Editor version and the host details before and after.

```powershell
$ExpectedCommit = '<full exact commit from PR #539 host-request block>'
$Project = '<new detached worktree path>'
$Unity = 'C:\Program Files\Unity\Hub\Editor\6000.4.9f1\Editor\Unity.exe'
$Out = '<artifact directory outside Assets and src>'
git fetch origin work
$env:GIT_LFS_SKIP_SMUDGE = '1'
git worktree add --detach $Project $ExpectedCommit
git -C $Project lfs pull --include="" --exclude="docs/tracking/evidence/**"
if ((git -C $Project rev-parse HEAD).Trim() -ne $ExpectedCommit) { throw 'Wrong source commit' }
New-Item -ItemType Directory -Force -Path $Out | Out-Null
cmd /c mklink /J "$Project\Assets\Scripts" "$Project\src"
git -C $Project status --porcelain -- src Packages ProjectSettings | Set-Content "$Out\porcelain-before.txt"
$CompileArgs = @('-batchmode', '-quit', '-projectPath', ('"{0}"' -f $Project),
                 '-logFile', ('"{0}"' -f "$Out\compile.log"))
$Compile = Start-Process -FilePath $Unity -ArgumentList $CompileArgs -Wait -PassThru
$Compile.ExitCode | Set-Content "$Out\compile-exit.txt"
if ($Compile.ExitCode -ne 0) { throw 'Unity import/compile failed; retain the log' }
$TestArgs = @('-batchmode', '-runTests', '-projectPath', ('"{0}"' -f $Project),
              '-testPlatform', 'EditMode', '-assemblyNames',
              'TacticalDirector.ClientApp.Tests;TacticalDirector.SeasonSave.Tests;TacticalDirector.Transfers.Tests;TacticalDirector.ClubFinances.Tests;TacticalDirector.MatchClientCore.Tests;TacticalDirector.UiFramework.Tests',
              '-testResults', ('"{0}"' -f "$Out\editmode.xml"),
              '-logFile', ('"{0}"' -f "$Out\editmode.log"))
$Tests = Start-Process -FilePath $Unity -ArgumentList $TestArgs -Wait -PassThru
$Tests.ExitCode | Set-Content "$Out\editmode-exit.txt"
git -C $Project status --porcelain -- src Packages ProjectSettings | Set-Content "$Out\porcelain-after.txt"
```

These commands are a handoff procedure; nobody has run them as a script on the host. Confirm the installed
Editor is 6000.4.9f1. Read the complete compile log for `error CS`, even when the exit
code is 0. Do not filter tests, mark failures as ignored or suppress warnings. Keep any
environment-gated diagnostics at their default (off).

**Runtime:** `TacticalDirector.SeasonSave.Tests` contains real-engine season fixtures that
carry `[Timeout(7200000)]`. One of them took 1,106 s alone on this host on October 9, 2026.
Expect the EditMode process to take a long time; do not stop it early.

**Exit code:** a non-zero test exit can come from an inconclusive case alone (as in the
#544 receipt). Inspect `editmode.xml` before deciding pass or fail.

## Required evidence

- **Compile:** a fresh full import gives `Tundra build success`, **0 `error CS`**, no new
  warnings compared with the known set, and one assembly per asmdef. Keep `compile.log`.
  This is the only Unity proof for `TacticalDirector.MatchClientUnity`, including the new
  `SeasonSave` reference and the `S0ScreensBehaviour.Career.cs` partial.
- **EditMode:** unfiltered results for all six named assemblies, with the XML and log.
  Report actual passed / failed / skipped / inconclusive counts per assembly and the reason
  for each non-pass.
- **Named fixtures run normally under Unity:** `CareerScreenTextTests`,
  `CareerTransfersTests`, `ClientCareerCoordinatorTests`, `ClientShellWiringValidatorTests`,
  `ClientScreenVisibilityTests`, `S0TextFormatterTests`, `CareerDriftDiagnosticTests`,
  `SeasonDailyRevenueTests`, `SeasonLoopFinanceTests`, `SeasonTransfersT2Tests`,
  `TransfersT0Tests`, `ClubFinancesT0Tests`, `ClubFinancesT2BootstrapTests`, and the four
  AR-2 M1 finance-restore cases in ClientApp (2 empty + 2 populated; the populated cases
  carry the +1,234 balance marker).
- **Linux reference only** (CI run 42122407644 at `d35bf396`, before `34f29b84` and before
  the main merge): ClientApp 275/0/3, SeasonSave 553/0/4, Transfers 79/0/0,
  ClubFinances 70/0/0. Explain any Unity difference; do not copy these numbers.
- **Clean tree:** `porcelain-before.txt` and `porcelain-after.txt` are both empty. No
  new skips or quarantines are allowed.
- **Receipt:** keep the exact commit, host details, Editor version, commands, exit codes,
  timestamps, and SHA-256 hashes of every artifact. Return a failure as it is; do not
  patch the source in the host worktree.

## Out of scope

- The Unity transfer and finance **pages**. PR #539 ships the host-free layer only; the
  pages remain a separate Unity-host follow-up.
- Play-mode runtime, the target-player build, and pinned-host timing. They are not
  required for this request, and the receipt must not claim them unless they actually run.
- The independent full-scope review, which PR #539 also still requires. This request does
  not authorize a merge.
