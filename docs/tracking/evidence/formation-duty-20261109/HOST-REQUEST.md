# PR #543 pinned Unity host request

Created November 10, 2026. Purpose: owner-run validation of the reviewed PR head.
Status: **NOT RUN / owner will run**. This cloud has no pinned Unity Editor/bridge,
remote-host connection, VPN or configured host credentials. The owner chose to run
validation in response to the access question; no Unity receipt is claimed.

Use **the full exact commit in PR #543's current host-request block**, not a moving
branch name. The PR body is updated after the test/documentation commit is published.
The previous `4ec8b4d5` request predates seven new/expanded test cases and is superseded.
Production bytes are still identical to the measured runtime; test source changed.

## Prepare an isolated checkout

On the pinned Windows 11 / Unity **6000.4.9f1** host, use a new detached worktree so
it has no existing Library and the import compiles all source. Do not delete the
Library or alter settings in a working project. Record `git rev-parse HEAD`,
`git status --short`, Editor version and host tuple before/after.

```powershell
$ExpectedCommit = '<full exact commit from PR #543 host-request block>'
$Project = '<new detached worktree path>'
$Unity = 'C:\Program Files\Unity\Hub\Editor\6000.4.9f1\Editor\Unity.exe'
$Out = '<artifact directory outside Assets and src>'
git fetch origin codex/434-formation-duty
$env:GIT_LFS_SKIP_SMUDGE = '1'
git worktree add --detach $Project $ExpectedCommit
git -C $Project lfs pull --include="" --exclude="docs/tracking/evidence/**"
if ((git -C $Project rev-parse HEAD).Trim() -ne $ExpectedCommit) { throw 'Wrong source commit' }
New-Item -ItemType Directory -Force -Path $Out | Out-Null
cmd /c mklink /J "$Project\Assets\Scripts" "$Project\src"
$CompileArgs = @('-batchmode', '-quit', '-projectPath', ('"{0}"' -f $Project),
                 '-logFile', ('"{0}"' -f "$Out\compile.log"))
$Compile = Start-Process -FilePath $Unity -ArgumentList $CompileArgs -Wait -PassThru
$Compile.ExitCode | Set-Content "$Out\compile-exit.txt"
if ($Compile.ExitCode -ne 0) { throw 'Unity import/compile failed; retain the log' }
$TestArgs = @('-batchmode', '-runTests', '-projectPath', ('"{0}"' -f $Project),
              '-testPlatform', 'EditMode', '-assemblyNames',
              'TacticalDirector.MatchEngine.Tests;TacticalDirector.PositioningAI.Tests;TacticalDirector.MatchClientCore.Tests;TacticalDirector.MatchViewer.Tests',
              '-testResults', ('"{0}"' -f "$Out\editmode.xml"),
              '-logFile', ('"{0}"' -f "$Out\editmode.log"))
$Tests = Start-Process -FilePath $Unity -ArgumentList $TestArgs -Wait -PassThru
$Tests.ExitCode | Set-Content "$Out\editmode-exit.txt"
if ($Tests.ExitCode -ne 0) { throw 'EditMode failed; retain XML and logs' }
```

The commands are a handoff procedure, not an executed/validated host script. Confirm
the installed editor is 6000.4.9f1 and review the complete compile log for errors,
even if the process returns 0. Keep diagnostics opt-in flags at ordinary defaults;
do not filter tests, disable failures or suppress warnings.

## Required evidence

- Full fresh import/compile: no new compiler errors; keep compile.log / Editor.log.
- Unfiltered EditMode results for all four named assemblies, XML and Editor log.
- `MatchEngineFormationDutyTests`: **42 cases**, including the six mirrored
  `KickoffSelection_FirstUseAllocatesNothing_AndFirstTickKeepsBaseline` cases and
  raw requested 255/effective F442 for both teams. Native GC constraint and its
  positive control must execute; the .NET counter is not a substitute for it.
- Existing schema, rotation, restart, SlotComposerStage, TacticTranslation,
  TickStampedCommandReplay and LiveMatchFrameCue fixtures also run normally.
- Report actual pass/fail/skip/inconclusive counts and reasons. Linux full-tree
  counts are not asserted as Unity counts. No new skip/quarantine is allowed.
- Preserve exact commit/host/version/commands, exit codes, timestamps and SHA-256
  hashes of the receipts. A failure remains a failure and should be returned intact.

The allocation contract being tested is 0 B for the newly activated selection/
seeding path. Unchanged main's first full physics tick has a measured 536 B cost on
the declared Linux lane; this is recorded separately, not silently declared fixed.

Trace diagnosis remains on HOLD until Opus's diagnosis design is ready. Merge still
requires that diagnosis, the named player-control blocker and successful pinned
Unity validation. No merging is authorized by this request.
