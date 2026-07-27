# Match Presentation Depth #48 — Section 3: Algorithms

**Created:** July 27, 2026
**Last Updated:** July 27, 2026 (v0.1 — initial section-file set)
**Version:** 0.1
**Status:** APPROVED

---

**Nothing below makes a stochastic draw** (FR-MP-012/033), and that is not incidental — it is what makes
observer neutrality **unconditional** (KD-7) rather than a property that has to be argued per feature.

## 3.1 `CommentaryRecorder.OnTick` — the live capture (FM-MP-01)

Invoked from the **#37-specified shared tap**, on the streamer's **tick thread**, once per tick.

```
OnTick(int tick, in EventSpan events, in ObservationSnapshot obs):
    if (!depthEnabled.Commentary)  return                    # KD-7: no recorder, no work at all

    foreach e in events:                                     # the ONE shared tap (FR-MP-007)
        if (!TryMapToIntent(e, obs, out CommentaryIntent intent, out CommentarySlots slots))
            continue                                          # not every event is narrated
        RequireRenderableIntent(intent)                       # F1 -- FR-MP-015, BEFORE any selection work
        RequireFiniteObservation(obs)                         # F3 -- malformed sim data fails loud here
        transcript.Append(new CommentaryLine(tick, intent, slots))
```

**It writes only into #48's own transcript.** No event is emitted, no tick advanced, no sim state touched
(FR-MP-001). The `events` span is a read-only view supplied by the tap; #48 does not retain it.

**The tap is joined, not built** (FR-MP-007/008). It is a **#37-owned contract** (FR-AN-002) that is
specified and unimplemented — there is no `src/match-analytics/`, and `EventBus.OnTickBoundary` is a
lifecycle reset rather than a consumer hook. So whichever of #37 / #44 / #48 lands first **builds it to
#37's contract** and the others join. **#48 must not build a second**: a parallel tap would double-read
one ledger with two lifetimes and two sets of ordering assumptions, which is the parallel-surface class
this project keeps catching.

**Why capture rather than re-derive.** #37's FR-AN-021 states there is **no post-match ledger reader** and
that the serialized bytes must not be assumed re-parseable; `SerializeLedger` is write-only. So a replay
**cannot** reconstruct event-driven commentary after the fact — it can only replay what was recorded while
the match ran. This is the shape `MatchReplayRecorder` already uses for positions, extended to events.

**Malformed input fails loud rather than being sanitised** (F3). Sanitising a non-finite position would
**hide a sim defect behind presentation** — the observation surface is the sim's output, and presentation
is not the place to repair it.

## 3.2 `SelectionMix` — the FR-LC-004 `ulong` without a draw (FM-MP-02)

```
SelectionMix(int tick, CommentaryIntent intent, int subjectAgentId) -> ulong:
    RequireRenderableIntent(intent)                          # F1
    z := MP_SELECTION_SEED
    z := SplitMix64Step(z ^ (ulong)(uint)tick)               # LOAD-BEARING -- see below
    z := SplitMix64Step(z ^ (ulong)(uint)(int)intent)
    z := SplitMix64Step(z ^ (ulong)(uint)(subjectAgentId + 1))   # +1 so MP_NO_SUBJECT (-1) maps to 0
    return z
```

**A keyed mix, not a draw.** It reads no cursor, advances no stream, and is a **pure function of its
arguments** — #35's KD-2 mechanism, itself following the in-tree `FixtureScheduler` / `LeagueBootstrap`
local-SplitMix64 precedent. Nothing is serialized, `world.text` is untouched, and there is no cursor to
resume.

**`tick` is load-bearing in the key** (FR-MP-013), and this is the one place a plausible simplification
would be badly wrong. Two identical triggers **at the same tick for the same agent** select the **same**
variant — which is correct, since one moment should not be narrated two ways. But drop `tick` and *every
occurrence of one intent for one agent selects the same variant for the whole match*: the same striker's
every goal narrated in identical words. That is the most visible regression a commentary system can have,
and it would pass every determinism test.

**The `subjectAgentId + 1` shift** maps `MP_NO_SUBJECT` (`-1`) to `0`, so the subject-less case is the
mix's **neutral** input rather than its most extreme (`0xFFFFFFFF`), and cannot collide with a maximal
agent id.

**Conditional on `ERR-049-001`** (FR-MP-018). #49's FR-LC-020 currently binds `SelectionDraw` to #22's
`world.text` draw — a MUST on a generic seam naming one producer's stream. #48 is the **third** spec
blocked on that one wording fix (#35, #46, #48). If it is refused, #48 supplies `SelectionMix = 0`;
FR-LC-007's `variant = draw % variantCount` is total at `0`, so every intent renders variant `0` and
phrasing variety is lost — **most visibly for #48**, since repeated commentary lines are immediately
noticeable (§7.4 R-4).

## 3.3 The animation derivation (FM-MP-03)

```
DeriveAnimationFrame(int tick, in ObservationHistory h) -> AnimationFrameView:
    for each agent i:
        v      := (h.Position(tick, i) - h.Position(tick - 1, i)) * TICKS_PER_SECOND   # derived
        speed  := Length(v)
        gait   := speed < MP_WALK_MAX ? Walk : speed < MP_JOG_MAX ? Jog : Sprint
        facing := speed > MP_FACING_MIN_SPEED ? Normalize(v) : h.Facing(tick - 1, i)   # hold on stop
        state[i] := AdvanceStateMachine(state[i], gait, facing, tick)                  # #48's OWN state
    return Snapshot(state)                                                              # by value
```

**Every input is already on the observation surface**, and every output is display state living in #48
(FR-MP-019). Position history gives velocity, velocity gives gait and facing, and the state machine that
turns those into a pose is **presentation**, not simulation.

**The `facing` hold on a stopped agent is the example worth stating**: it is exactly the kind of fact that
*looks* like it needs a sim-side field (*"the engine knows which way he's facing"*) and does not — a
stationary agent's facing is the last direction he moved, which the history carries.

**If a future fidelity genuinely needs a sim-side fact** — a foot-strike frame, say — FR-MP-020/021 pin
the rule: an **additive read-only property on match-engine**, in the `BallView` / `AgentView` class, whose
addition must (a) state **why it cannot be derived** from the history and (b) pass the observer-neutrality
digest lock **unchanged**. Never a presentation-side push, and never a new serialized field. §5.3 asserts
the current property set explicitly, so an addition is **visible in a diff**.

**No Unity host is required for any of this.** The derivation, the state machine and the view model are
plain arithmetic over value copies; only the **renderer** is host-gated (FR-MP-022).

## 3.4 Cue mapping (FM-MP-04)

```
MapCue(in Event e, in ObservationSnapshot obs):
    if (!depthEnabled.Audio)  return
    if (!TryMapToCue(e, obs, out CueId cue, out CueParams p))  return
    RequireDefinedCue(cue)                                    # F2 -- AT THE MAPPER, not the sink
    sink.Emit(cue, p)                                          # ICueSink; default impl is a NO-OP
```

**The guard is at the mapper, deliberately.** `ICueSink`'s default is a **no-op** (FR-MP-026), so a
validity check living only in the sink would be **silently absent in a headless or pre-#51 run** — which
is the default configuration, not an edge case.

**#48 stops at the cue id** (FR-MP-023). Playback, mixer, buses and the cue **catalogue** are #51's.

**#48 declares `ICueSink`; the client shell implements it against #51** (FR-MP-024/025). The alternative —
#51 implementing #48's interface — would make **the audio framework reference a presentation-depth spec**,
inverting the layering and giving a Wave-8 spec a Wave-7 dependency. With the shell adapter, **neither
spec references the other**, exactly as the root supplies #49's boundary adapters and #46's projectors.

**Emitting into a seam rather than calling playback directly is what makes #51's arrival cheap**: a
sink-implementation change rather than a rewrite of every call site in the mapper.

## 3.5 The window snapshot and the thread boundary

```
# Called on the UI thread, by #38, through IViewModelSource<CommentaryFeedView>
GetFeedView() -> CommentaryFeedView:
    lock (transcriptLock):                                    # the tick thread appends under the same lock
        n := Min(transcript.Count, COMMENTARY_WINDOW_LINES)
        return CopyLast(transcript, n)                        # BY VALUE -- never a handle (FR-MP-029)
```

**Two threads, one discipline.** #38's FR-UI-023 and its F6 pin that during a live streamed match the
engine is owned by the streamer's **tick thread**, and that even *commands* must marshal rather than touch
it cross-thread. `CommentaryRecorder.OnTick` runs on that thread; #38 renders on the UI thread. The window
is therefore a **snapshot-copy at the boundary** — the same discipline, in the **read** direction, that
FR-UI-023 imposes in the write direction.

**The `where T : struct` constraint is what forces the shape**, not a stylistic preference.
`IViewModelSource<T>` requires a struct, and a variable-length feed behind a struct is either a **per-frame
allocation** or a **live alias** — the mutable-handle defect this project has caught three times
(`SquadPositionCounts`, `MatchReplay`'s frame list, `TacticPreset.Players`). A bounded by-value window is
neither.

**The interactive-viewer `Start()` / `Stop()` race is the precedent** for what happens when a lifecycle
boundary here is left implicit, which is why §5.5 asserts the copy semantics directly rather than trusting
the type.

## 3.6 Arithmetic convention (pinned)

The animation derivation is the only place #48 computes anything numeric, and it operates on the
observation surface's `float` positions — display arithmetic, downstream of every determinism boundary
(FR-LC-005 / KD-7). **It feeds no digest and no save**, so no rounding convention is pinned and none is
needed.

The selection mix is `ulong` arithmetic with one modulo performed **inside #49's renderer**, not here.

**The rule that matters instead:** no #48 arithmetic may ever flow **back** into the simulation
(FR-MP-001). That is a layering constraint, not a numeric one, and §5.2 asserts it as such.

## 3.7 Worked examples (hand-verifiable)

| # | Setup | Working | Result |
|---|---|---|---|
| (a) | Depth disabled; a full match runs | `OnTick` returns immediately; no recorder constructed | **no transcript, no cue, byte-identical digest** (KD-7's "off" half) |
| (b) | Depth **enabled**; the same match | no stream, no cursor, nothing serialized | **byte-identical digest** — KD-7's *"neutral when on"*, the claim that distinguishes #48 |
| (c) | A goal at tick 5 000 by agent 7 | `SelectionMix(5000, Goal, 7)` | a defined `ulong`; the same value on every replay of that seed |
| (d) | The same striker's **second** goal, tick 9 000 | the key differs in `tick` | a **different** variant — the reason FR-MP-013 exists |
| (e) | (d) with `tick` dropped from the key | key identical to (c) | **the same words twice** — the most visible possible regression, and it passes every determinism test |
| (f) | Two triggers at tick 5 000 for agent 7, same intent | identical key | the **same** variant — correct: one moment, one narration |
| (g) | A subject-less intent (`MP_NO_SUBJECT`) | `subjectAgentId + 1 = 0` | the mix's **neutral** input, not `0xFFFFFFFF` |
| (h) | `CommentaryIntent.None` reaches the render path | pre-selection gate | **throws** (F1) — before any selection work |
| (i) | An undefined `CueId`, sink is the no-op default | guard at the **mapper** | **throws** — a sink-side guard would be silently absent here (F2) |
| (j) | A non-finite agent position from the observation surface | `RequireFiniteObservation` | **throws** (F3) — not sanitised, which would hide a sim defect |
| (k) | A stationary agent | `speed < MP_FACING_MIN_SPEED` | facing **held** from the previous tick — derived, no engine field (§3.3) |
| (l) | Transcript has 200 lines, window is 20 | `CopyLast(20)` | a **by-value** window; appending line 201 does **not** change a window already taken |
| (m) | In-session scrub after the match | reads the live transcript | the lines the run produced |
| (n) | Exported HTML artifact | **embeds rendered text** at export time | the same lines — the file **cannot re-derive** them (FR-MP-017) |
| (o) | (n) opened under a different UI locale | text was baked at export | the **export's** locale — correct for a display artifact, and no sim state involved |

Examples (b), (e) and (i) are the three that matter most: (b) is the spec's headline claim, (e) is the
regression that would ship silently, and (i) is the guard whose natural placement is wrong.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-27 | — | Initial §3 (FM-MP-01..04: the live capture on the shared tap, the keyed selection mix, the animation derivation, cue mapping; the window snapshot and thread boundary; the §3.6 layering-not-numeric rule; fifteen worked examples). `tick`'s presence in the selection key is argued with its counterexample (e) rather than asserted, and the cue guard's placement at the mapper is argued from the no-op default being the *default* configuration. Status IN REVIEW. |
#endregion
