# Match Presentation Depth #48 — Section 4: Architecture

**Created:** July 27, 2026
**Last Updated:** July 27, 2026 (v0.1 — initial section-file set)
**Version:** 0.1
**Status:** APPROVED

---

## 4.1 Assembly and reference direction

New assembly **`TacticalDirector.MatchPresentation`** at `src/match-presentation/`, referencing
**`TacticalDirector.MatchEngine`** (the observation surface) and **`TacticalDirector.MatchViewer`** (frame
types) — and nothing else.

```
#38 (ui-framework) ──▶ {#48, match-viewer, match-client-core, match-engine}
#48                ──▶ {match-engine, match-viewer}
boundary(MatchTextBoundary) ──▶ {#48, #49}
shell(CueSinkAdapter)       ──▶ {#48, #51}
```

**Acyclic, and the reverse direction is empty.** No sim assembly references #48 — the mechanical
`.asmdef` reverse-reference scan #38's T0 already ships (FR-UI-001) covers it, extended to #48.

**The reference #48 must not take is `match-client-core`, and it is one line away.** That assembly sits in
the **same layer** and carries `ILiveMatchMutations` and `ManagerCommandQueue` — a genuine mutation
surface. A #48 that referenced it would have a **write path into a live match**, and the browser viewer's
playback-only invariant is the precedent for why that matters: the interactive-client AR-1 H-2 finding,
and the reason #38's ERR-038-001 exists. **A presentation surface that gains a mutation channel stops
being presentation.**

**Both outward seams are inverted, and identically.** #48 **declares** `ICueSink` and **emits** #49
identities; the **adapters** at the shell and boundary are what touch `#51` and
`TacticalDirector.Localization`. So #48 references neither — and, symmetrically, **#51 does not reference
#48 either**, which is what keeps a Wave-8 spec from becoming a Wave-7 dependency (FR-MP-025).

**CS0104 pre-check.** #48 introduces `CommentaryIntent`, `CueId`, `CommentaryLine`, `CommentarySlots`,
`CommentaryFeedView`, `AnimationFrameView`, `CommentaryRecorder`, `CueMapper`, `ICueSink`, `CueParams`.
Each was checked against every name that could be in scope with it before authoring, because this project
has hit CS0104 twice (`TacticTranslation`, `PlayerAttributes`). Note `CommentarySlots` is deliberately
**not** named `InteractionSlots` / `MediaSlots` / `InboxSlots` — FR-LC-014 pins that the producers' slots
are **disjoint**, so a shared name would suggest a compatibility that must not exist.

## 4.2 File layout

```
src/match-presentation/
├── MatchPresentationConstants.cs   # the Appendix A catalogue — no magic numbers in formula code
├── CommentaryIntent.cs             # APPEND-only; the #49 LocalOrdinal AND an export-embedded value
├── CueId.cs                        # APPEND-only; #51's catalogue will be keyed on it
├── CommentaryLine.cs               # {tick, intent, slots} — native values only
├── CommentarySlots.cs              # #48's disjoint slots
├── CommentaryRecorder.cs           # FM-MP-01 — runs on the TICK thread
├── SelectionMix.cs                 # FM-MP-02 — local keyed SplitMix64; no stream, no cursor
├── AnimationDeriver.cs             # FM-MP-03 — over the observation history; no engine field
├── CueMapper.cs                    # FM-MP-04 — the CueId validity guard lives HERE, not in the sink
├── ICueSink.cs                     # declared by #48; the SHELL implements it (FR-MP-025)
├── CommentaryFeedView.cs           # bounded by-value window (FR-MP-029)
├── AnimationFrameView.cs           # by-value; same `where T : struct` constraint
└── tests/
```

**`MatchTextBoundary.cs` is deliberately absent from this tree.** It references both #48 and
`TacticalDirector.Localization`, and FR-LC-012 makes a sim/loop-side reference to the latter a **build
error** — so placing it here would not merely be untidy.

**`CueSinkAdapter.cs` is absent for the mirror reason**: it references both #48 and #51, so it lives in
the client shell (§4.5).

**No tap implementation lives here** (FR-MP-007/008). The live per-tick tap is **#37's contract**; #48
registers a consumer on it and does not own it — even in the case where #48 is implemented first and
therefore builds it.

**`SelectionMix.cs` is its own file with no state**, deliberately: co-locating it with the recorder is the
shortest path to someone caching a variant per intent, which would break FR-MP-013's per-tick variety
without failing any determinism test.

## 4.3 The shared tap (KD-2(i))

```
# The tap is #37's contract (FR-AN-002). #48 is its THIRD consumer, after #37 and #44.
tap.Register(commentaryRecorder);      # OnTick(tick, in events, in observation)
tap.Register(cueMapper);
```

**Joined, not defined** (FR-MP-008). The tap is specified and **unimplemented** — there is no
`src/match-analytics/`, and `EventBus.OnTickBoundary` is a lifecycle reset rather than a consumer hook.
The root `CLAUDE.md` records the intended shape: *"the #37-class per-tick ledger tap (FR-AN-002, the
approved observational pattern — **one tap feeds #37+#44**)."* #48 makes it three.

**Whichever of #37 / #44 / #48 is implemented first builds it — to #37's contract.** If that is #48, the
tap it builds is **#37's surface**, not a #48 one, and #37/#44 join it later unchanged. This is a
sequencing note rather than a back-prop (§8.3), because nothing in #37's approved text needs to change for
it to be true.

**#48 must not build a second tap** (F6). A parallel tap would double-read one ledger with **two
lifetimes and two sets of ordering assumptions** — the parallel-surface class this project keeps
catching, and the reason the rule is a requirement rather than a convention.

**Nothing here re-parses the serialized ledger** (FR-MP-009): `SerializeLedger` is write-only, and
FR-AN-021 states the bytes must not be assumed re-parseable.

## 4.4 The #38 hosting split and the thread boundary (KD-5)

```
# in #38 — the SCREEN
class MatchScreen
{
    IViewModelSource<CommentaryFeedView>  _feed;      // where T : struct
    IViewModelSource<AnimationFrameView>  _anim;
    void Render() { var w = _feed.Current; /* draw w's Count lines */ }   // a VALUE, not a handle
}
```

| Layer | Owns |
|---|---|
| **#38** | navigation, layout, input, the screen's lifecycle |
| **#48** | the mapping, the transcript, the view-model projections |
| **match-engine** | the simulation and the observation surface |

**`IViewModelSource<T>` is `where T : struct`, and that constraint shapes the design rather than decorating
it.** A commentary *feed* is variable-length, so a struct wrapping a growing list is either a **per-frame
allocation** or a **live alias** — the mutable-handle defect this project has caught three times
(`SquadPositionCounts`, `MatchReplay`'s frame list, `TacticPreset.Players`). `CommentaryFeedView` is
therefore a **bounded window**: the last `COMMENTARY_WINDOW_LINES` entries **by value**, with the full
transcript staying inside #48.

**The recorder and the renderer are on different threads**, and this is pinned rather than assumed.
#38's FR-UI-023 and its F6 establish that during a live streamed match the engine is owned by the
streamer's **tick thread**, and that even *commands* must marshal rather than touch it cross-thread.
`CommentaryRecorder.OnTick` runs on that thread; #38 renders on the UI thread. The window is produced by
**snapshot-copy at the boundary** — the same discipline, in the **read** direction, that FR-UI-023 imposes
in the write direction.

**The interactive-viewer `Start()` / `Stop()` race is the precedent** for what happens when a lifecycle
boundary here is left implicit, which is why §5.5 asserts the copy semantics behaviourally rather than
trusting the type to imply them.

## 4.5 The two inverted seams

```
# BOUNDARY LAYER — references #48 and #49
class MatchTextBoundary
{
    LocalizedTextRequest BuildRequest(CommentaryIntent intent, ulong mix, in CommentarySlots slots)
        => new(new TextTemplateId(ProducerTag.MatchCommentary, (int)intent), mix, Format(slots), none);
}

# CLIENT SHELL — references #48 and #51
sealed class CueSinkAdapter : ICueSink              // #48 DECLARES the interface; the shell implements
{
    public void Emit(CueId cue, in CueParams p) => audio.Play(cue, p);      // #51's playback API
}
```

**Both seams point the same way**, and that symmetry is the architectural point: #48 emits **identities**
and the adapters resolve them. The result is that #48 references neither `Localization` nor #51, **and
neither of them references #48**.

**The #51 direction is the one that would have been got wrong.** Having the audio *framework* implement a
presentation-depth spec's interface is the natural-looking arrangement, and it inverts the layering — a
lower-level service depending on a higher-level consumer, with a Wave-8 spec acquiring a Wave-7
dependency.

**The exported artifact bakes rendered text at the boundary, not in #48** (FR-MP-017). The exporter calls
`MatchTextBoundary` once per line at export time and embeds the result. #48 still emits identities only,
so FR-LC-002 is intact — and the export's locale is baked into the file, which is correct for a display
artifact and consistent with the HTML replay's existing *"NOT a determinism-pinned wire format"* contract.

## 4.6 State and persistence

**#48 holds no persistent game state, bumps no format version, and adds no save sub-blob** (FR-MP-032) —
the #37 property, for the same reason: everything is derived per-frame from observation plus the live tap,
and the commentary transcript is **session-scoped**.

**The exported HTML replay embeds rendered commentary, and an export is not a save.** No format version,
no restore path, and nothing the sim reads back. The distinction is load-bearing precisely because the
export *is* self-contained: it must carry what it cannot re-derive (§3.1), and carrying it does not make
it save state.

**Client-local presentation settings** — commentary on/off, audio levels, animation quality — sit
**outside the determinism boundary**, alongside locale and accessibility settings. #49's FR-LC-018 already
established that class, so #48 inherits it rather than defining a new one.

## 4.7 Contracts with neighbours

| Neighbour | Contract |
|---|---|
| **match-engine** | Read-only **value copies** from the public observation surface, and no new field (FR-MP-019/020). #48 writes nothing and is referenced by nothing sim-side. |
| **match-viewer** | A **sibling in the same layer**; #48 consumes its frame types. The existing observer-neutrality digest lock is the pattern #48's own extends. |
| **match-client-core** | **No reference, in either direction** (FR-MP-004). Its `ILiveMatchMutations` is exactly the write path #48 must not acquire. |
| **#37** | Owns the live per-tick tap's **contract**; #48 is its **third consumer** and never redefines it. #37 is also a read-only source for stat-driven lines, on the same tap. |
| **#38** | Hosts the match screen over #48's view models through `IViewModelSource<T>`. **#38 owns the UI; #48 owns the mapping.** The thread boundary is FR-UI-023's, applied in the read direction. |
| **#49** | #48 is a **producer** — its fourth, after #22, #35 and #46. The binding is the sibling `MatchTextBoundary`; **#49's core is untouched**, and #48 **inherits** #35's `ERR-049-001` rather than filing a duplicate. |
| **#51** | **No reference in either direction.** #48 declares `ICueSink`; the **shell** adapts it. #48's no-op default keeps a headless run valid forever. |
| **#16** | **Untouched — no row and no `_RESERVED_` placeholder.** #48 is presentation/infra: no stream, no tag, no ordinal, and **nothing to promote later**. |
| **#22** | **Untouched.** #48 consumes neither `InteractionTextGenerator` nor `world.text`. |

**Standing review item:** #48 performs **no** write to any sim type. Its own reference set proves it
cannot reach `match-client-core` — but **match-engine's types are reachable**, so the no-mutation property
against the observation surface is asserted **behaviourally** in §5.2, and the **boundary/shell adapters**
(§4.5) are asserted too, since they are the only code holding both #48 and an external service.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-27 | — | Initial §4 (assembly with the `match-client-core` trap named as one line away, the CS0104 pre-check incl. the deliberate slot-type non-collision, file layout with three deliberate absences and the reason `SelectionMix` is stateless and separate, the shared tap as a joined #37 contract, the #38 hosting split and the read-direction thread discipline, the two identically-inverted seams, state and persistence, neighbour contracts). The standing review item is scoped to the **adapters** as well as to #48, since they are the only code holding both sides. Status IN REVIEW. |
#endregion
