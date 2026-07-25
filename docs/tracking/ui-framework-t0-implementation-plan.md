# UI / Client Framework #38 — T0 Substrate Implementation Plan

**Created:** July 25, 2026
**Last Updated:** July 25, 2026 (v0.3 — code AR pass 1 over the landed assembly: 1H+3M+4L, all
resolved; pass 2 over the compiled + tested artifact: 1M+2L, all resolved. See §8.)
**Version:** 0.3
**Status:** IMPLEMENTATION PLAN (governs the #38 T0 landing; the APPROVED spec is authoritative on any
disagreement except where a KD below records a deliberate, back-propped deviation)
**Governs:** new `src/ui-framework/` (`TacticalDirector.UiFramework`) + its test assembly
**Spec:** `docs/specs/ui-client-framework/` #38 (APPROVED July 22, 2026) — §2 FRs, §3 contracts,
§4 architecture, §5 test plan

---

## 1. Scope

Land the **pure substrate** #38 §4.2 lays out: the projection contract, the navigation state machine,
the dispatch contract + the one concrete match-tactics dispatcher, and the one concrete match-view
projection. All host-free, all compiled and tested by the Linux shim gate.

**Not in scope** (each deferred by the spec itself, not by this plan): the UGUI rendering binding
(§4.3/§7.2, Unity-host-gated), any Wave-7 screen view model (§7.1/KD-2 — a screen against unbuilt data
is the phantom-consumer trap), and any new sim mutation seam (FR-UI-003/020/KD-4).

## 2. What already exists (verified against source, July 25, 2026)

| Surface | Where | Relevance |
|---|---|---|
| `LiveMatchStreamer` | `src/match-viewer/LiveMatchStreamer.cs` | Owns the tick thread. Public: `TryGetLatestFrame`, `SetPreTickHook(Action)`, `ServiceOnce()`, `Pause`/`Resume`/`SetSpeedMultiplier`. **No `EnqueueIntent`** — see KD-U1. |
| `LiveMatchFrame` | `src/match-viewer/LiveMatchFrame.cs` | Immutable struct, but `AgentPositions` is a `Vector2[]` handed over by ownership convention — a mutable array. See KD-U4. |
| `ManagerCommand` / `ManagerCommandKind` / `ManagerCommandQueue` / `MatchClientDriver` / `ILiveMatchMutations` | `src/match-client-core/` | The P2 deterministic command channel, already adversarially reviewed to convergence: UI-thread `Enqueue` → sim-thread drain installed as the streamer's pre-tick hook → `Apply` onto the three live mutators. See KD-U1/KD-U2. |
| `MatchEngine.SetTeamTactic` / `SetPlayerTactic` / `SubstitutePlayer` | `src/match-engine/MatchEngine.cs:1116/1134/1164` | The three verified public seams FR-UI-013 names. Signatures confirmed. |
| `MatchEngineConstants.SQUAD_SIZE` = 22 | `src/match-engine/MatchEngineConstants.cs:35` | The F1 bounds gate. |

**CS0104 check (§4.5), run July 25, 2026:** `grep` over `src/**` + `docs/specs/**` for `ManagerIntent`,
`NavigationShell`, `ScreenId`, `MatchFrameView`, `IViewModelSource`, `ICommandDispatcher` returns hits
only inside `docs/specs/ui-client-framework/` itself. No collision; no fully-qualification needed at T0.

## 3. Key decisions

### KD-U1 — Live-match marshaling reuses the existing command channel; `LiveMatchStreamer` gains **no** `EnqueueIntent`

#38 §3.3/§4.1 proposes `streamer.EnqueueIntent(intent)` as "a small presentation-side addition to
`LiveMatchStreamer`". **This plan does not add it**, because the interactive-Unity-client work that
landed *after* #38 was approved settled the same question the other way, and settled it correctly:

- `interactive-unity-client-design.md` **AR-1 H-2** (v0.2, July 23) rejected putting the command drain
  inside the shared streamer precisely because it "would have given the browser viewer's streamer a live
  mutation path too, regressing its playback-only / disjoint-by-construction invariant."
- That invariant is load-bearing and already documented in the root CLAUDE.md: `LiveMatchServer` exposes
  a playback-only `/control` and **holds no `MatchEngine` reference, only a `LiveMatchStreamer`**, so
  "reachable by the presentation surface" and "can mutate the match" are disjoint **by construction**.
  Adding `EnqueueIntent` to the streamer collapses that disjointness for every streamer consumer,
  including the browser viewer, which must never gain one.
- The resulting design — `ManagerCommandQueue` + `MatchClientDriver.Service` installed via
  `SetPreTickHook` — **already satisfies FR-UI-023 exactly**: the intent is marshaled onto the sim
  thread and applied between ticks by the thread that owns the engine. The requirement is met; only the
  proposed mechanism differs.

So `MatchTacticsDispatcher` (live mode) enqueues onto the existing `ManagerCommandQueue`. **Back-prop
`ERR-038-001`** against #38 §3.3 / §4.1 / §5.1 T-UI-DISPATCH-004 to re-anchor the marshaling mechanism.

### KD-U2 — `ManagerIntent` is the UI vocabulary; it translates into the existing `ManagerCommand`, and there is exactly one `Apply`

#38 §2.2 requires a typed `ManagerIntent` as the only thing a dispatcher accepts. `match-client-core`
already has `ManagerCommand` covering the identical closed set (the same three seams, the same payloads,
already zero-value-safe with a `None = 0` sentinel).

Rather than a second command vocabulary reaching the engine, `ManagerIntent` is the **UI-facing** type
(#38's contract) and the dispatcher **translates** it:

- **live mode** → `ManagerCommand.SetTeamTactic/SetPlayerTactic/Substitute` → `ManagerCommandQueue`;
- **single-threaded mode** (§3.3's "no streamer running" case) → the same `ManagerCommand` → `Apply`
  onto an `ILiveMatchMutations` directly.

Both paths funnel through `ManagerCommand.Apply`, so there remains exactly **one** piece of code that
touches an engine mutator, and it is the one already reviewed. `ManagerIntent` adds no seam
(FR-UI-012/014).

**AR-1 M-1:** the translation lives in `MatchTacticsDispatcher` (the concrete surface), **not** as a
`ManagerIntent.ToCommand()` method. Putting it on the intent would make the intent type itself depend on
`match-client-core`, dragging the client-core dependency into the part of the substrate a future
non-match dispatcher (a season screen, say) has no business carrying. The intent stays a plain typed
payload; only the dispatcher knows the channel.

### KD-U7 — `MatchViewModelSource` reads an `ILiveFrameSource` seam, not `LiveMatchStreamer` directly

**AR-1 M-2.** #38 §3.4 has the source read `streamer.TryGetLatestFrame`. Binding the concrete class
directly makes the whole match-view group **untestable without threads**: `LiveMatchStreamer` publishes
frames only from its own paced background thread, and its manual-step seams (`TickOnce`,
`ApplyCapturedFrame`) are `internal` to `match-viewer` — so a deterministic test would need either
wall-clock pacing (flaky, slow) or a new `InternalsVisibleTo` on `match-viewer`, which would break this
plan's own KD-U1 promise to leave that assembly untouched.

Resolution: a one-method seam owned by the framework —

```
interface ILiveFrameSource { bool TryGetLatestFrame(out LiveMatchFrame frame); }
sealed class LiveMatchStreamerFrameSource : ILiveFrameSource   // adapter over the real streamer
```

Both sides are specified and both are built here (adapter + consumer), so this is not the phantom-
interface class ERR-001/ERR-004 forbid. It preserves FR-UI-005 exactly — the source still holds no
engine, and now provably cannot: the seam's only member returns a published frame. `match-viewer`
is unchanged.

### KD-U3 — `ui-framework` references `TacticalInstructions` (value types only)

§4.1 says the generic substrate "references nothing sim-side", but §2.2 gives `ManagerIntent` a
`TeamTactic` / `PlayerTactic` payload — those types live in `TacticalDirector.TacticalInstructions`.
The two statements cannot both hold literally.

Resolution: reference it. `tactical-instructions` is a leaf config assembly (its own asmdef references
only `ProjectConstants`) of pure immutable value types; it is not the engine and carries no mutation
path. The invariants that actually matter are **FR-UI-001** (no sim/loop assembly references the UI —
preserved: the reference points UI → config, never back) and **FR-UI-003** (the framework provides no
mutation path of its own — preserved: `TeamTactic` is data, and only `MatchEngine` can act on it). A
stringly-typed or `object`-boxed payload would satisfy the letter of §4.1 while destroying the type
safety FR-UI-012 asks for. **Back-prop `ERR-038-002`** to reword §4.1.

### KD-U4 — `MatchFrameView` copies the agent array; it never wraps the streamer's

`LiveMatchFrame.AgentPositions` is a `Vector2[]` whose ownership is handed to the caller by convention.
FR-UI-002/007 forbids a live buffer or mutable handle escaping into a view model (F4), and the
`MatchReplay` AR-3 finding is the exact precedent — a `ReadOnlyCollection` over a *live* list is a view,
not a snapshot. So `MatchFrameView`:

- copies positions into its own array at construction, and exposes them only through a
  `ReadOnlyCollection<Vector2>` over that private copy (no indexer onto caller memory);
- **bounds-gates** the array length against `SQUAD_SIZE` and the possessing-agent id against
  `[-1, SQUAD_SIZE)` (−1 = loose ball, the engine's documented sentinel) — F1 / FR-UI-008;
- **NaN-gates** every sampled float (ball x/y/z and each agent x/y) — F1 / the match-viewer guard;
- refuses a negative score (defensive; the engine cannot produce one).

### KD-U5 — `MatchViewModelSource` holds only the streamer, and never forces a frame

Per FR-UI-005 the match view is a pure-observation surface: the source is constructed from a
`LiveMatchStreamer` and holds **no** `MatchEngine` reference, so `RunTick` is not reachable from it
(FR-UI-015). Per F5, `Project()` before any frame is published returns the **last-known** view, or an
explicitly empty one if none has ever been published — never a throw, never a forced tick. The
last-known cache lives in the source (a client-local presentation cache, not sim state — FR-UI-022 is
unaffected).

**AR-1 L-3:** that cache makes `Project()` **stateful across calls**, which reads oddly against
FR-UI-006's "pure". The purity FR-UI-006 requires is *no sim mutation* and *same observed state ⇒ same
`T`*, both of which hold; F5 explicitly sanctions "last-known". The source is documented as
**render-thread-only** (it is not thread-safe and does not need to be — one renderer reads it), and the
cache is called out in the type's own doc so the next reader does not mistake it for hidden sim state.

### KD-U6 — Navigation is a struct-id stack machine with an explicit registry

`ScreenId` is a `readonly struct` over an `int` (spec §2.2) with value equality, so it is dictionary-safe
without boxing. The shell holds `registry: Dictionary<ScreenId, ScreenRegistration>` + `stack: List<ScreenId>`.
`Push`/`Replace` to an unregistered id throws (F2); `Pop` at depth 1 throws (FR-UI-011 — the shell never
empties). A shell with an empty stack (nothing pushed yet) has no `Current`: reading it throws rather
than returning a default id that would alias a real screen — the zero-value-safety convention
`ManagerCommandKind.None` follows. `Push` onto an empty stack is legal and establishes the root (§3.5
starts at `stack=[0]`, which is that first `Push`); `Pop` then refuses at depth 1, so the stack can
never be emptied once rooted.

**AR-1 L-1:** `ScreenRegistration` stores the source as a non-generic marker `IViewModelSource` (with
`IViewModelSource<T> : IViewModelSource`), not `object`. The registry must hold heterogeneous sources,
but `object` would let any unrelated type be registered as one; the marker keeps the registry honest
while the owning screen casts to its own `IViewModelSource<T>` to project.

### KD-U8 — `Register` refuses a duplicate `ScreenId` (a recorded deviation from §3.2)

**Code AR pass-1 M-1.** #38 §3.2 pins `Register(reg): registry[reg.Id] = reg` — an assignment, so a
second `Register` of the same id silently overwrites. That swaps a live screen's source and dispatcher
underneath a navigation stack that still holds the id, and the shell is otherwise uniformly fail-loud
(unregistered `Push`/`Replace`, root `Pop`). `NavigationShell.Register` therefore throws on a duplicate.

This is a deliberate deviation, not an oversight, and is filed as **`ERR-038-003`** so the spec text and
the code do not stay silently divergent.

## 4. File-by-file plan (`src/ui-framework/`)

| File | Contents |
|---|---|
| `ui-framework.asmdef` | `TacticalDirector.UiFramework`; references `MatchEngine`, `MatchViewer`, `MatchClientCore`, `TacticalInstructions`, `ProjectConstants` (all built today — no speculative reference, §4.1) |
| `IViewModelSource.cs` | `interface IViewModelSource<T> where T : struct { T Project(); }` — the KD-1 projection contract |
| `ScreenId.cs` | `readonly struct ScreenId` (int value, `IEquatable<ScreenId>`) + `readonly struct ScreenRegistration { ScreenId Id; object ViewModelSource; ICommandDispatcher Dispatcher; }` |
| `NavigationShell.cs` | The §3.2 stack machine: `Register` / `Push` / `Pop` / `Replace` / `Current`, plus read-only additions beyond the §2.2 sketch — `Depth`, `RegisteredCount`, `IsRegistered`, `GetRegistration`, `SnapshotStack` (deliberate; they make the shell observable without exposing mutable state) |
| `IntentKind.cs` | `None = 0` sentinel + `SetTeamTactic` / `SetPlayerTactic` / `Substitute` (the `ManagerCommandKind` zero-value convention) |
| `ManagerIntent.cs` | `readonly struct` + one factory per kind + `ToCommand()` (the KD-U2 translation); a `default` instance is `None` and is refused |
| `ICommandDispatcher.cs` | `interface ICommandDispatcher { void Dispatch(in ManagerIntent intent); }` |
| `MatchTacticsDispatcher.cs` | Both §3.3 modes in one type via two constructors: live (`ManagerCommandQueue`) and single-threaded (`ILiveMatchMutations`). Unmapped/`None` intent ⇒ throw (F3) |
| `MatchFrameView.cs` | The immutable match VM (KD-U4) |
| `MatchViewModelSource.cs` | `IViewModelSource<MatchFrameView>` over the streamer (KD-U5) |
| `ILiveFrameSource.cs` | The KD-U7 frame seam + `LiveMatchStreamerFrameSource` adapter over the real streamer |
| `UiFrameworkConstants.cs` | `[GT]` render cadence via `GameplayConfigHolder.Config.GetFloat("ui-framework", …)` — the June-30 catalogue-migration conformity. **AR-1 L-2:** at T0 this has no production consumer (nothing renders yet), so it carries an explicit declared-but-unconsumed doc-note naming its consumer (the §7.2 UGUI binding), following the project's existing doc-note convention for values that land ahead of their call site. It is one value, not a speculative catalogue |
| `AssemblyInfo.cs` | `InternalsVisibleTo` the test assembly, so the internal intent→command translation can be locked directly by the drift guard |
| `Tests/ui-framework-tests.asmdef` | `TacticalDirector.UiFramework.Tests`, Editor-only, `autoReferenced: false` (the match-client-core-tests precedent) |
| `Tests/NavigationShellTests.cs` | T-UI-NAV-001/002/003 |
| `Tests/CommandDispatchTests.cs` | T-UI-DISPATCH-001/002/003/004 |
| `Tests/MatchViewProjectionTests.cs` | T-UI-MATCHVIEW-001/002 + T-UI-FAIL-001/002 + T-UI-LAYER-002 |
| `Tests/MatchViewObserverNeutralityTests.cs` | T-UI-NEU-001 (digest-lock) |

Every `.cs` gets the house header block (File/Created/Modified/Author/Spec/Purpose) + a
`#region VersionHistory` footer, and a two-line `.meta` sibling (the `match-client-core` format).

## 5. Test plan → spec test IDs

| Spec ID | How it is discharged |
|---|---|
| T-UI-LAYER-001 | Parse every `src/**/*.asmdef` and assert none outside the UI/test pair references `TacticalDirector.UiFramework`. **AR-1 M-3:** the repo root is resolved by walking up from `AppContext.BaseDirectory` looking for a directory containing both `src/` and `tools/` (the generator writes each `.gen.csproj` beside its asmdef, so the test binary sits under `src/ui-framework/Tests/bin/…` and the walk terminates); if no root is found the test **fails loud** rather than passing vacuously, and it also asserts it actually scanned a plausible number of asmdefs (> 20) so a mis-resolved root cannot masquerade as "nothing references us" |
| T-UI-LAYER-002 | `typeof(MatchFrameView).IsValueType` + every field readonly + the positions view is not the source array (reference-inequality assert) |
| T-UI-NEU-001 | Two same-seed engines: one ticked bare; the other ticked with, after every tick, a frame built from the engine's **public observation surface**, published through a stub `ILiveFrameSource`, and `MatchViewModelSource.Project()` called on it. Assert the two `CurrentSnapshotDigest` chains are byte-identical. This exercises the real projection against a real engine with no threads; the streamer's own neutrality is already digest-locked by `MatchViewerTests` (KD-U7) |
| T-UI-DISPATCH-001 | Single-threaded dispatch of each kind mutates exactly the expected team/agent via the `TestOnly_` read-backs |
| T-UI-DISPATCH-002 | `default(ManagerIntent)` and an out-of-range `IntentKind` both throw |
| T-UI-DISPATCH-003 | A recording `ILiveMatchMutations` observes exactly the three mutator calls and nothing else |
| T-UI-DISPATCH-004 | Live-mode dispatch enqueues (queue depth grows) and applies **nothing** until a drain runs; after `MatchClientDriver.Service` the mutation is visible (the marshaling contract, KD-U1) |
| T-UI-NAV-001/002/003 | The §3.5 worked transition reproduced exactly; unregistered `Push`/`Replace` + root `Pop` throw; no Unity type in the shell |
| T-UI-MATCHVIEW-001 | Projecting N times leaves the engine tick unchanged (the source cannot reach `RunTick`) |
| T-UI-MATCHVIEW-002 | Project before any publish ⇒ `IsEmpty` view, no throw |
| T-UI-FAIL-001/002 | Out-of-range agent index / agent count, and a NaN ball or agent coordinate ⇒ throw |

## 6. What this plan does NOT do

- No UGUI, no `MonoBehaviour`, no prefab (§4.3 — and the shim gate has no rendering types).
- No screen view models for #29/#31/#32/#37/#30 (§7.1 gating; those specs have no `src/` at all).
- No new engine seam, no RNG stream, no domain tag, no `SubsystemOrdinal`, no save-format bump
  (FR-UI-022) — so **no `SNAPSHOT_SCHEMA_VERSION` change** and no digest rebaseline.
- No change to `LiveMatchStreamer` (KD-U1) — the browser viewer's playback-only invariant is untouched.

## 7. Back-props to file at landing

| ID | Against | Change |
|---|---|---|
| `ERR-038-001` | #38 §3.3 / §4.1 / §5.1 T-UI-DISPATCH-004 | Marshaling re-anchored from a proposed `LiveMatchStreamer.EnqueueIntent` to the shipped `ManagerCommandQueue` + pre-tick drain (KD-U1); the streamer keeps zero mutation surface |
| `ERR-038-002` | #38 §4.1 | "generic substrate references nothing sim-side" reworded to the invariant that holds: no reverse reference (FR-UI-001) + no own mutation path (FR-UI-003); a config-value-type reference is neither (KD-U3) |

## 8. Adversarial review history

**AR-1 (self-review over v0.1, checked against source — 0H + 3M + 3L, all resolved in v0.2).** The pass
read the plan against the actual assemblies rather than against itself, which is where all three Mediums
came from.

- **M-1** — `ManagerIntent.ToCommand()` would have pulled `match-client-core` into the intent type
  itself, so every future dispatcher (season, squad) would inherit a match-channel dependency it has no
  use for. Translation moved into `MatchTacticsDispatcher`. (KD-U2)
- **M-2** — the whole match-view test group was **untestable as planned**: `LiveMatchStreamer` publishes
  only from its paced background thread and its manual-step seams (`TickOnce` / `ApplyCapturedFrame`)
  are `internal` to `match-viewer`, so the plan implied either flaky wall-clock tests or an
  `InternalsVisibleTo` edit that would have broken KD-U1's own "no change to `match-viewer`" promise.
  Introduced the `ILiveFrameSource` seam + adapter. (KD-U7)
- **M-3** — T-UI-LAYER-001 had no defined repo-root resolution, so on a path change it would have
  scanned nothing and **passed vacuously** — the worst failure mode for a lock test. Added the walk-up
  resolution, the fail-loud when unresolved, and a scanned-count floor.
- **L-1** — `ScreenRegistration` held the source as `object`; switched to a non-generic
  `IViewModelSource` marker. (KD-U6)
- **L-2** — `UiFrameworkConstants` would be a phantom catalogue at T0 (nothing renders); reduced to one
  value with an explicit declared-but-unconsumed doc-note naming its §7.2 consumer.
- **L-3** — the F5 last-known cache makes `Project()` stateful across calls, unremarked against
  FR-UI-006's "pure"; documented the reconciliation and pinned the source as render-thread-only. (KD-U5)

### Code AR pass 1 (over the landed assembly, before it compiled) — 1H + 3M + 4L, all resolved

- **H-1** — no `ui-framework.asmdef` existed, so `generate_projects.py` (which globs `src/**/*.asmdef`)
  compiled none of the new files, and `ScreenRegistration` referenced an unwritten `ICommandDispatcher`.
  A gate run would have returned green while proving nothing about this assembly. Fixed by landing the
  asmdef pair + remaining types + `.meta` siblings in the same increment as the first gate run.
- **M-1** — the `Register` duplicate-id deviation was undocumented (see KD-U8 + `ERR-038-003`).
- **M-2** — the live dispatcher took a bare `ManagerCommandQueue`, which is publicly constructible while
  `MatchSession` builds its own internally: `new MatchTacticsDispatcher(new ManagerCommandQueue())`
  would have accepted intents forever and applied none — a silent drop arriving through the constructor.
  The live constructor now takes the `MatchSession`, so an undrained queue is not constructible.
- **M-3** — the `ManagerIntent` / `ManagerCommand` duplication had no stated justification and no drift
  guard. Both added: the justification (intent is the client-wide vocabulary, including a future
  `AdvanceRound` that is not a match-channel command; command is the match-channel subset) and
  `CommandDispatchTests.EveryMatchIntentKind_MapsToACommandKind`.
- **L** — marker-interface claim softened; the extra `NavigationShell` members recorded in §4;
  `AdvanceRound`'s deliberate absence documented at the enum with #30 named as its gate; no ordinal-
  stability note needed (`IntentKind` is never serialized — it translates to `ManagerCommandKind`, which
  carries the note).

### Code AR pass 2 (over the compiled, tested artifact) — 1M + 2L, all resolved

- **M-1** — `CommandDispatchTests.UnmappedIntentKind_Throws` was **vacuous**: it forced an out-of-range
  kind by reflection, but if the write silently failed the intent stayed `None`, which *also* throws —
  so the test would pass without ever reaching the `default:` arm it exists to cover. Added an assertion
  that the forced kind actually took.
- **L-1** — the streamer adapter split out of `ILiveFrameSource.cs` into its own file (the
  `ScenarioIndexEntry` file-naming precedent).
- **L-2** — `MatchFrameView`'s int-formatting helper was named `Count`, which reads as a quantity;
  renamed `Inv` (culture-invariant formatting).

**One real defect was caught by the tests rather than by reading:** `MatchFrameView.Empty` is
`default(MatchFrameView)`, and `IsEmpty` was a `bool` **field** — which defaults to `false`, so the empty
view reported itself as carrying a frame. The field is now `_hasFrame` with `IsEmpty => !_hasFrame`, so
the zero value means "empty". This is the zero-value trap the project has hit before
(`ManagerCommandKind.None`, `MarkingOrientation`, `TacticalContext.RestDefenseSufficient`).

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-25 | — | Initial plan: scope, verified-source inventory, KD-U1..U6, file-by-file layout, test-ID mapping, back-props. Not yet adversarially reviewed. |
| 0.2 | 2026-07-25 | — | AR-1 (0H+3M+3L, all resolved): M-1 translation moved off `ManagerIntent` onto the dispatcher; M-2 new KD-U7 `ILiveFrameSource` seam (the match-view group was untestable without threads or a `match-viewer` edit); M-3 T-UI-LAYER-001 root resolution + fail-loud + scanned-count floor (it could pass vacuously); L-1 typed `ScreenRegistration` marker; L-2 constants phantom doc-note; L-3 last-known-cache purity reconciliation. |
#endregion
