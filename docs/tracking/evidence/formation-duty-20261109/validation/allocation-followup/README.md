# First versus later physics-frame allocation probe

Created November 10, 2026. Purpose: distinguish startup allocation from recurring
allocation, without modifying gameplay or running a new football-result capture.

SDK 8.0.426 / CLR 8.0.32, Linux Debug/development shim lane, seed 0123456789ABCDEF.
Each arm warms global/JIT paths on a separate 1,200-tick engine, then measures eight
fresh engines for 1,200 ticks each. Engine construction, codec warm-up, storage,
CSV/log output are outside each allocation window; the window is exactly RunTick.
GetAllocatedBytesForCurrentThread measures managed allocation on that thread.
The same frozen base/runtime binaries as the earlier first-tick probe are used;
all assembly hashes/MVIDs and original source pins are retained in the summary/logs.

Both arms have exactly the same 9,600 samples: every first tick allocates 536 B.
Of the 9,592 later samples, 9,568 allocate zero. Each repetition allocates 832 B
at ticks 371 and 785, and 840 B at tick 382. Thus 536 B is first-tick-only in this
bounded sample, not an every-frame charge; zero allocation on every frame is also
not established. Do not generalize one seed/20-second windows to all match states.

The three later allocating ticks coincide with development FirstTouch log output.
FirstTouchSystem.cs lines 189–196 formats/logs strings under UNITY_EDITOR or
DEVELOPMENT_BUILD. This identifies a candidate source, not an allocation-stack
attribution or proof of Release/Unity behavior. No logging or game code is changed.
The original first-tick source of 536 B is not isolated by this counter probe.

Reproduce outside the repository: copy an arm's *.txt inputs to Program.cs and
probe.csproj in a scratch directory, point the HintPath references at that exact
arm's frozen capture binaries, then run with the declared SDK:

```bash
dotnet run --project /path/to/probe.csproj -- /path/to/allocations.csv
```

The checked-in csproj text preserves the actual workspace paths, not a portable
bundle of binaries. Rebuild those binary inputs from the pinned source using the
parent package's frozen preparation instructions if needed. No trace diagnosis
was run; formation/Duty diagnosis remains held for Opus.

The open issue is [engine startup/development allocation](../../../../open-issues.md#engine-frame-allocation).
Closing it requires native pinned-Unity GC.Alloc call stacks/counters for first and
later ticks, separation of setup/development logging from gameplay allocation,
and a separately authorized fix plus regression coverage. Linux measurements do
not certify the Unity runtime. Existing Unity request at source/test head 75d62186
can proceed; this follow-up changes documentation/evidence only.
