# P7b career shutdown cancellation

**Created:** November 18, 2026

**Version:** 0.1

**Status:** Draft for technical review

**Purpose:** Resolve the [P7b shutdown wait](open-issues.md#career-shutdown-main-thread-wait) before the career shell becomes the default in a merged build. The owner selected cooperative cancellation, discard of unfinished work, and protection of an in-flight save transaction on November 18, 2026.

## Player behavior

Closing the game or stopping Play during matchday requests cancellation. The last completed save remains available. On relaunch, the player resumes from that save and plays any unfinished matchday again. Days, transfers, or other changes since that save follow the existing unsaved-progress policy. If a save transaction has already begun, shutdown waits for it to finish or for its existing error recovery to run. A quit confirmation is optional follow-up work. Cancellation and writer safety must also work for Editor Stop, scene teardown, and other paths that bypass a confirmation.

Keep the tracked scene's default enablement gated on this fix and pinned Unity acceptance. The current career branch has enablement on for review and Play testing.

## Season loop and cancellation points

[#30 §3.3.2 and §3.4](../specs/season-competition-loop/section-3.md) place matchday's own daily work before selection and require the table, outcome, fixture marks and cursor to reflect a whole round on success. [FR-SN-012/013/016/021/024/027/032](../specs/season-competition-loop/section-2.md) govern that order, deterministic continuation, save capture and the sole writer. [§6](../specs/season-competition-loop/section-6.md) distinguishes the world command from the match engine's hot path.

- Pass a lifetime cancellation token through the queued season command in `ClientCareerCoordinator.Dispatch` → `SeasonCommandDispatcher` → `CareerSession.Execute(PlayMatchday)` → a cancellation-aware owned-squad entry to `SeasonLoop`. `CareerProgress` continues to select the legal action. Keep both existing `AdvanceAndPlayNextRound` entry points and their existing non-cancellable behavior. Route all three through the same guards and resolution body.
- Poll before starting mutable matchday work, at fixture boundaries, in `SeasonLoop.PlayThroughEngine` between complete batches of ticks, and before a successful round exit. Keep each `RunTick` immediately followed by its existing discipline observation. Propagate the token through `ResolveFixture` on the engine path. The quick-sim path needs fixture-boundary polls. Start with batches of up to 64 ticks and measure on the pinned host. At 60 nominal ticks per simulated second, a 90-minute match uses roughly 324,000 nominal ticks before stoppage. If that many ticks take 120 wall-clock seconds, 64 ticks average about 24 ms. Boot, fixture bookkeeping and individual ticks have variable cost. Measure request-to-worker-exit directly.
- The poll reads a host cancellation flag synchronously. It allocates nothing on the running path and does not register a callback, consult a clock, or enter the engine's deterministic state. Keep `MatchEngine.RunTick`, RNG, match tick and tap ordering, result application and the save format as they are. Observe cancellation with a distinct outcome, such as a matching-token `OperationCanceledException` at the checkpoint. Ordinary engine and storage errors keep their existing handling.
- The cancellation-aware #30 entry is for callers that can discard the loop. Document that contract and make the client enforce it through shutdown ownership. `PlayNextRound` can already have applied earlier fixtures, including table rows, buffered outcomes, appearances and discipline. Matchday morning work can already have advanced career subsystem cursors. The engine's `finally` clears `ActiveMatch` and the unfinished match's card fold is discarded. The worker discards the entire career on shutdown, including any earlier completed fixtures and transient counters. Skip publication, further projections and autosave on this path. A new career session restores from the persisted save through the existing restore seam. The current outcome producer buffers locally, and #22 ingest remains deferred under KD-3. Future externally visible consumers would need a review of this discard boundary.

Amend the cancellation-aware contract in #30 with its implementation, covering §3.4 success/cancel behavior and §5 test traceability. Record the client lifetime and storage boundary in §5-P7 of the interactive client design. The existing full-round contract remains authoritative for successful calls. This draft adds no normative spec text.

## Shutdown and save ownership

Current `ClientCareerCoordinator.Dispose` waits on a single idle event covering the operation, autosave, refresh and projections. Use a write-phase barrier alongside the cancellation signal. Serialize writer admission with shutdown under a short lock. Never hold that lock around simulation, filesystem I/O, or a wait.

| State when shutdown wins | Required behavior |
|---|---|
| Queued or computing | Close admission to filesystem mutation, signal cancellation and return without joining the worker. A queued job observes shutdown before starting. Running matchday work exits at the next checkpoint. Its worker retains and releases the in-memory owner when it unwinds. |
| Round complete and save still awaiting admission | Skip the save and discard the unsaved round. A last-moment token check on its own would leave a race here. Writer admission and shutdown must use the same gate. |
| Filesystem mutation already admitted | Let the worker finish the admitted transaction and signal the write barrier in a finally block, including when I/O fails. Dispose waits only for that barrier. The worker skips further projection and UI publication. |
| Finished or completion queued for UI publication | Clear pending navigation/completion and stop publication. Dispose returns without running a callback. |

Prepare the round's save bytes from the quiescent loop before requesting the permit. Extract the existing `SeasonSaveManager.Save` capture and encoding path so its current callers reuse the same implementation and destination checks. Pass the prepared bytes and the metadata required for those checks into the existing `CareerSaveSlots.Write` transaction. A cancellation during capture can discard the buffer and leave the store untouched. Once the worker has prepared the bytes, it requests the permit before entering the store transaction. The permit covers slot sequence selection and the read checks it relies on, temp-file and sidecar staging, flush, digest, paired swaps, rollback and cleanup. Snapshot capture stays before file open as required by FR-SN-021. Test that preparation preserves the existing encoded bytes and overwrite guards.

Measure the time inside the permit, including storage latency. A slow or blocked disk can still delay shutdown. Continue holding the permit until the save succeeds or its error recovery finishes.

Cover every mutation reachable from an accepted career operation through the same gate. This includes creation/autosaves and manual saves, interrupted-write recovery and directory creation when opening the store, and `CareerSession.Resume` setting aside a damaged sidecar. Pure slot reads and projection can be skipped or allowed to unwind with the detached worker. A read already in progress must acquire the permit before it can invalidate a slot. Cancellation cannot mark a healthy save damaged or surface as an autosave failure. For the existing single-shell contract, a replacement shell can open the same directory after disposal returns because the old one has finished or been denied every future write.

Keep UI and worker cleanup distinct. Disposal closes admission and clears UI publication under the shared gate, then waits outside it only if a writer was admitted. Defer disposal of token/signal resources and clearing worker-owned state until the final worker user has exited. Preserve idempotence across `OnApplicationQuit`, `OnDestroy`, wiring rejection and schedule failure. Avoid a worker-thread Unity callback or a continuation that depends on the Unity synchronization context to release the write barrier.

## Acceptance

1. **Cancel after work has started.** Use deterministic test barriers, with a fixture known to resolve before the managed match. Cancel after at least one previous fixture has applied and after the real engine has completed ticks inside the managed match. Verify worker exit before the engine reaches full time, no report or navigation publication, and byte-identical pre-existing save and sidecar. Check slot sequence, latest-slot choice and absence of new temporary or backup files. Keep the bytes in the checkpoint as the replay input.
2. **Replay and compare.** Start uninterrupted and resume-after-cancel runs from identical pre-round save bytes, using identical inputs and resolution mode. Compare ordered results, cursor, season snapshot and the entire serialized career payload, including world, training, medical, appearances, progression, discipline, finances and transfers. Compare the completed autosave payload and sidecar from independent stores with identical slot history. The existing `ResumedCareer_ContinuesExactlyAsTheUninterruptedOne` is the quick-sim precedent. Add an engine-backed case so the in-match poll and discipline tap are exercised.
3. **Prove both sides of writer admission.** Park the real worker during save capture, then after write admission, including cancellation between the pair swaps. Before admission, dispose returns, the original pair stays intact and the old worker cannot write after a new shell opens the store. After admission, dispose waits until success or rollback and returns even if a subsequent projection would be blocked. Verify queued cancellation, repeated disposal, failed scheduling, initialization recovery and damaged-slot invalidation races. Use barriers to determine phase ordering. Test timeouts detect hangs and do not drive production behavior.
4. **Verify on the pinned host.** Compile the exact source/scene tree in Unity 6000.4.9f1, run affected EditMode suites and the [career Play checklist](../../src/match-client-unity/README.md#p7b-career-binding). Exercise window close in a player and Editor Stop during the real match loop and during a save, including a second Play session using the same directory. Record stop latency and time spent in the write phase separately, successful re-open and continued play, Console output and steady-state allocations. Verify no-cancel result identity against the existing path and check that polling has no material impact on match throughput. Keep default enablement gated until the receipts cover the updated tree.

The small Unity focus guard reported in review can use explicit Unity-object null checks in both focus capture and focus return when the binding next changes.

## Version history

| Version | Date | Notes |
|---|---|---|
| 0.1 | 2026-11-18 | Draft cooperative cancellation, discard and writer ownership design with acceptance cases. |
