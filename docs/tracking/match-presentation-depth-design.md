# Match Presentation Depth #48 — Design Supplement

> **Created:** July 26, 2026
> **Last Updated:** July 26, 2026 (v0.6 — AR-5 sweep: 0H+0M+2L, **CONVERGENCE**; prior v0.5 AR-4, v0.4 AR-3, v0.3 AR-2, v0.2 AR-1, v0.1 initial)
> **Version:** 0.6
> **Status:** DESIGN SUPPLEMENT (pre-promotion — no section files, no `SPEC_INDEX.md` row)
> **Candidate spec:** **#48** · **FR prefix:** `FR-MP` · **Wave:** 7 · **Tier:** S1 min → S2+ deep
> **Promoted from:** `docs/tracking/spec-plans/spec-48-match-presentation-depth.md` v0.1

---

## 0. Purpose and posture

This supplement resolves the five key decisions the #48 plan defers, against **verified** upstream source
rather than assumption. Design only — no code, no section files, no registry row.

Two of the plan's five are answered by work that landed *after* it was written, and both answers make #48
smaller than the plan expects: **KD-2's commentary-determinism question is settled by the #49 seam plus the
#35 precedent** (§2(c)/(d)), and **KD-5's composition question is settled by client assemblies that already
exist** (§2(a)). The one place verification makes #48 *harder* is §2(b) — the constraint that decides whether
commentary can exist on the replay path at all, which the plan does not mention.

## 1. Scope

**#48 owns:** the **mapping** from observed match state and emitted events to presentation output —
commentary line selection, animation/render state, and audio **cue selection**.

**#48 does not own:**

| Not owned | Owner | How #48 relates |
|---|---|---|
| The match simulation and its event ledger | **match-engine** / **#17** | observed read-only; #48 emits nothing into it (KD-1) |
| Rendering **text** into a locale | **#49** | #48 is a text **producer**: identity + slots + a `ulong` (KD-2) |
| Audio playback, mixer, buses, cue catalogue | **#51** (Wave 8) | #48 maps event → **cue id**; playback is #51's (KD-4) |
| The UI framework, navigation, the screen hosting the match | **#38** | #38 hosts; #48 supplies view-models (KD-5) |
| Match **statistics** | **#37** | a read-only source for stat-driven lines, on the same tap discipline (KD-2) |
| Any gameplay outcome | the sim | presentation reads results, never produces them (KD-1) |
| The **content** — animation clips, audio assets, the commentary corpus | art/audio/writing production | #48 specifies **triggers, identities and contracts**; specifying when a line fires is not specifying the line, and the asset surface dwarfs the logic here (§11 R-3) |

## 2. What already exists (verified)

**(a) The client layer #48 composes into is already built — the plan's KD-5 is largely answered.**
`src/` carries `match-viewer` (the `HtmlReplayExporter` post-hoc replay + `LiveMatchFrame` /
`LiveMatchStreamer` live path), `match-client-core` (`MatchClientDriver`, `MatchSession`,
`ManagerCommandQueue`, and the `ILiveMatchMutations` command seam), `match-client-unity` (a host shell), and
`ui-framework` (#38's T0 substrate). Their `.asmdef` references run **one way** — `ui-framework` →
`{MatchEngine, MatchViewer, MatchClientCore, TacticalInstructions, ProjectConstants}`; nothing sim-side
references any of them.

**Consequence:** #48 is a **sibling of `match-viewer` in that layer**, not a new tier. Its host, its
observation path, and its no-reverse-reference discipline all exist; KD-5 is about which assembly it lives
in, not how the layering works.

**(b) There is no post-match ledger reader — and this decides where commentary can exist.**
`match-analytics-statistics/section-2.md`:

- **FR-AN-002** — #37 consumes *"exactly two deterministic taps: the read-only per-tick ledger tap … and the
  observational world-state sample (`BallView`/`AgentView`)."*
- **FR-AN-021** — #37 *"MUST consume **live during the match** (there is no post-match ledger reader); it
  MUST NOT assume the serialized ledger bytes can be re-parsed."*

`EventBus.SerializeLedger(Span<byte> dst)` is a **write-only** surface (the #37 KD-1 finding #44 later
depended on): the bytes go into the digest, and nothing reads them back.

**The tap itself is specified but not built.** There is no `src/match-analytics/` — #37 is approved and
unimplemented — and `EventBus.OnTickBoundary` is a per-tick *lifecycle reset*, not a consumer hook. So the
"live per-tick ledger tap" is a #37-owned **contract** (FR-AN-002 / its KD-7) awaiting construction, and the
root `CLAUDE.md` already records the intended shape: *"the #37-class per-tick ledger tap (FR-AN-002, the
approved observational pattern — **one tap feeds #37+#44**)."* #48 makes it **three consumers of one tap**,
and must not build a second — a parallel tap would double-read the same ledger with two lifetimes and two
sets of ordering assumptions, which is the parallel-surface class this project keeps catching.

**Consequence, which the plan does not anticipate:** commentary triggered by *events* (a goal, a card, a
save) can only be produced **live, during the tick loop**, through that one shared tap. The existing `HtmlReplayExporter` is a *post-hoc*
exporter over sampled positions, so a replay cannot reconstruct event-driven commentary after the fact — it
can only replay commentary that was **captured while the match ran**. KD-2 takes that as a constraint rather
than discovering it at implementation, and it is why commentary output is a **recorded artifact**, not a
re-derivation.

**(c) Commentary is a #49 text producer, and #48 would be its fourth.** `localization-accessibility/`
FR-LC-002 (no baked localized strings), FR-LC-004 (`Render(in LocalizedTextRequest)`), FR-LC-012 (no sim/loop
assembly references `TacticalDirector.Localization`), FR-LC-013/014 (bind by adding a **sibling boundary
adapter**; producers carry disjoint slots), FR-LC-015 (intent-value pre-gate). §7.3 names `MediaTextBoundary`
(#35) and `InboxTextBoundary` (#46) and #38-static; #48 adds a fourth.

**Consequence:** the plan's KD-2 framing — *"consume #22's `InteractionTextGenerator` rather than forking a
fresh text system"* — is superseded exactly as #35's was. #22's generator is not the text seam any more; #49
is. #48 therefore never references `living-world` either.

**(d) The `world.text` question the plan flags as its top risk is already answered by #35.** The plan's §5/§9
worry that commentary riding `world.text` would advance persisted state and break observer neutrality — and
it is right. #35's KD-2 established the alternative: a **local keyed SplitMix64 mix** supplies the
FR-LC-004 `ulong` with **no stream, no cursor, nothing serialized** (the `FixtureScheduler` /
`LeagueBootstrap` local-mix precedent), leaving `_RESERVED_` tags unpromoted.

**Consequence:** KD-2 resolves to display-only, draw-free commentary, which makes observer neutrality
**unconditional** rather than — as the plan's §8 has it — a test *"conditioned on that KD-2 decision."*
#48 also inherits #35's `ERR-049-001` dependency (FR-LC-020 binds `SelectionDraw` to #22's `world.text`
draw); it is now the **third** spec blocked on that one wording fix.

**(e) Observer neutrality is an established, enforced property — not a new one.** `MatchViewerTests`
digest-locks that a recorded run is byte-identical to an unobserved same-seed run, and #37's FR-AN-017
carries the same requirement. The public observation surface (`BallView` / `AgentView(i)` / `AgentTeamId(i)`
/ `AgentIsGoalkeeper(i)` / `PossessingAgentId` / `HomeScore` / `AwayScore` / `MatchEnded`) returns
**value-type copies**.

**(f) #51 owns playback; #48 owns the mapping.** The roadmap's §1 row and the spec-51 plan's KD-1 split it:
#48 may land *"against direct playback or a stub bus API, with #51's later rehoming onto buses a
playback-side refactor."*

## 3. Staging (minimal-first → deep)

| Tier | Content |
|---|---|
| **Minimal (the identity)** | Exactly today's fidelity: 2D positions + score through `LiveMatchStreamer` / `HtmlReplayExporter`. **No commentary, no animation state, no audio cue.** Depth disabled ⇒ the pipeline is the existing viewer, byte-for-byte. |
| **Deep** | Live-captured commentary (KD-2), animation/render state over the observation surface (KD-3), audio **cue selection** off the same live tap (KD-4). Each is independently switchable; all three are additive over one observation path. |

The deep tier adds **readers**, never writers — which is why "all depth disabled ⇒ the minimal viewer" is a
structural property rather than a flag to maintain (KD-1).

## 4. Key decisions

### KD-1 — Observation-only, enforced structurally

#48 reads (i) the public observation surface's value copies and (ii) the **live per-tick event tap** (§2(b)).
It writes nothing, emits no event, advances no tick, and holds no sim state. No sim assembly may reference
it — asserted by the mechanical `.asmdef` reverse-reference scan #38's T0 already ships (FR-UI-001),
extended to #48's assembly.

**The one thing that makes this more than a slogan:** #48 must not acquire a *write* path by accident, and
the tempting one is real — `match-client-core` carries `ILiveMatchMutations` and `ManagerCommandQueue`, a
genuine mutation surface sitting in the same layer. #48's assembly **must not reference it**. The playback
controls (pause/speed) belong to the client shell, and the browser viewer's playback-only invariant — the
interactive-client AR-1 H-2 finding, and the reason #38's ERR-038-001 exists — is the precedent: a
presentation surface that gains a mutation channel stops being presentation.

### KD-2 — Commentary is **live-captured, display-only, and draw-free**

Three decisions, each forced by a different verified fact:

**(i) Live-captured, because there is no post-match ledger reader (§2(b)).** A `CommentaryRecorder` taps the
per-tick event stream *during* the run and appends `CommentaryLine` records — `{tick, intent, slots}`, native
values only — to an in-memory transcript. The replay path then **replays the transcript** rather than
re-deriving it. This is the same shape `MatchReplayRecorder` already uses for positions, extended to events.

*Rejected alternative:* re-parse the serialized ledger at replay time. Rejected — FR-AN-021 states in terms
that the bytes must not be assumed re-parseable, and `SerializeLedger` is write-only; building a reader
would be a second, unowned ledger format.

**(ii) Display-only and draw-free, so observer neutrality is unconditional.** The FR-LC-004 `ulong` comes
from a **local keyed SplitMix64** over `(tick, intentOrdinal, subjectAgentId)` — #35's KD-2 mechanism — so
#48 registers no stream, touches no cursor, and serializes nothing. `world.text` is **not** consumed.
(Two identical triggers at the same tick for the same agent select the same variant. That is deterministic
and, for a commentary line, correct — the same moment should not be narrated two ways — but it does mean
line variety comes from the key varying, so `tick` must be in the key and is.)

This is the plan's own top risk (§9) closed rather than managed: because no draw occurs, rendering a match
with full commentary cannot perturb the digest **by construction**, and §8's neutrality test needs no
conditional. Commentary being deterministic-but-display-only also matches the HTML replay's existing
contract (rendered output is not a determinism-pinned wire format).

**(iii) A #49 producer, via a sibling `MatchTextBoundary`.** Its own `CommentaryIntent` roster with the
**ORDINAL STABILITY — APPEND-only** contract (#35's KD-1), disjoint slots, the FR-LC-015 intent-value
pre-gate, FR-LC-008a coverage over #48's roster, and **no reference** to `TacticalDirector.Localization`
(FR-LC-012). #48 emits identity + slots + the mix; the adapter renders.

**"Not saved" means not in a *game save* — and the exported replay is the case that distinction exists
for.** There are two replay paths and they differ:

- **In-session replay / scrub** reads the live transcript from memory. Nothing to persist.
- **The exported HTML artifact** is self-contained by design — it already embeds sampled positions — so
  commentary must be **embedded in the export** or an exported replay silently has none. KD-2(i)'s
  capture-don't-re-derive design makes this the only option: the file cannot re-derive lines it did not
  carry.

What gets embedded is **rendered text**, baked by the exporter at the boundary layer — not by #48, which
still emits identities only (FR-LC-002 intact). That bakes the export's locale into the file, which is
correct for a display artifact and consistent with the HTML replay's existing contract (*"NOT a
determinism-pinned wire format"*): the exported file is a snapshot of one viewing, not a save. FR-LC-006
governs **serialized sim state**, and no sim state is involved.

Neither path puts a byte into a game save, which is the claim that matters for §5.

### KD-3 — Animation needs **no new engine field**, and the burden of proof sits on anyone who says otherwise

The observation surface already exposes per-agent position and the ball's position and height, which is what
a 2D/2.5D presentation needs. Richer pose (limb state, foot planting, turn phase) is **presentation-derived**:
it is a function of the position history #48 already samples, plus its own animation state machine, which is
display state and lives in #48.

**If some future fidelity genuinely needs a sim-side fact** — a foot-strike frame, say — the rule is: it is
an **additive read-only property on match-engine**, in the same class as `BallView`/`AgentView`, added by a
match-engine change, never a presentation-side push and never a new serialized field. The plan names this
risk (its §9 "inverting the layer taxonomy"); the concrete guard is that any such addition must state why
the value cannot be derived from the observation history, and pass the existing observer-neutrality digest
lock unchanged.

**The Unity host gate is real and belongs here:** 3D rendering proper is blocked on the same Unity host
access that gates the interactive client (a standing OPEN ISSUE). #48's contract — trigger mapping and
animation *state* — is authorable and testable without it; only the renderer is gated. That split is why the
spec is *"mostly contract + trigger mapping, not sim logic"*, as its plan says.

### KD-4 — Audio is **cue selection**, and #48 stops at the cue id

#48 maps an observed event to a `CueId` + parameters, off the **same live tap** as commentary (KD-2(i)) —
read-only, emitting nothing (the #37/#44 observational posture). Playback, mixer, buses, and the cue
**catalogue** are #51's (§2(f)).

**Until #51 lands, #48 emits cue ids into a seam with a trivial default sink** — not into a direct playback
call. The difference matters: an `ICueSink` with a no-op default keeps #51's later arrival a
*sink-implementation* change, whereas direct playback calls scattered through the mapper would make it a
rewrite of #48. This is the spec-51 KD-1 "stub bus API" option, chosen deliberately over "direct playback".

**#51 does not implement `ICueSink`; the composition root does.** Having the audio *framework* implement a
presentation-depth spec's interface would make #51 reference #48 — inverting the layering (a lower-level
service depending on a higher-level consumer) and making a Wave-8 spec carry a Wave-7 dependency. Instead
the client shell supplies a small adapter implementing `ICueSink` against #51's playback API, exactly as the
root supplies #49's boundary adapters and #46's projectors. Neither spec references the other, and #48's
default no-op sink remains valid forever for a headless run.

`CueId` carries the same **APPEND-only ordinal stability** as the text intents, for the weaker but real
reason that #51's catalogue will be keyed on it.

### KD-5 — #48 composes as a sibling of `match-viewer`, hosted by #38

Layering (§2(a)): #48 is its own assembly in the presentation layer, referencing `match-engine` (observation)
and `match-viewer` (frame types) — **not** `match-client-core` (KD-1), **not** `TacticalDirector.Localization`
(FR-LC-012), **not** `living-world` (KD-2), and nothing sim-side references it.

#38 hosts it the way it hosts everything else: #48 exposes **immutable view-model value types**
(`CommentaryFeedView`, `AnimationFrameView`) through #38's `IViewModelSource<T>` contract, so #38 renders
without knowing how any of it was produced and #48 owns no navigation, layout, or input. That contract is
already built (#38 T0), so this is composition, not new machinery.

**Two constraints the contract imposes, both easy to miss:**

- **`IViewModelSource<T>` is `where T : struct`.** A commentary *feed* is variable-length, so the view model
  cannot be a struct wrapping a growing list without either allocating per frame or handing out a live
  alias — the mutable-handle defect this project has caught repeatedly (`SquadPositionCounts`,
  `MatchReplay`'s frame list, `TacticPreset.Players`). `CommentaryFeedView` is therefore a **bounded window**
  — a fixed-capacity struct carrying the last `COMMENTARY_WINDOW_LINES` `[GT]` entries by value — not a
  handle to the transcript. The full transcript stays inside #48.
- **The recorder and the renderer are on different threads.** FR-UI-023 and its F6 pin that during a live
  streamed match the engine is owned by the streamer's **tick thread**, and that even *commands* must
  marshal rather than touch the engine cross-thread. `CommentaryRecorder.OnTick` runs on that tick thread;
  #38 renders on the UI thread. The window is therefore produced by **snapshot-copy at the boundary**, never
  by the renderer reading the live transcript — the same discipline, in the read direction, that FR-UI-023
  imposes in the write direction. The interactive-viewer AR-1 `Start()`/`Stop()` race is the precedent for
  what happens when a lifecycle boundary here is left implicit.

### KD-6 — Determinism posture: presentation/infra, no allocation

No RNG stream, no domain tag, no `SubsystemOrdinal` — the `match-viewer`/#37/#44/#46 read-only class. #16's
§3.4 catalogue has **no row and no `_RESERVED_` placeholder for #48**, consistent with its `0x2A` row's note
that the read-only/presentation/infra specs take no tag; the keyed mix of KD-2(ii) is local arithmetic, not a
stream. **#16 is untouched** — and, as with #46, that means #48 has no reserved value to promote, so a future
stochastic presentation surface would need a fresh allocation rather than a promotion.

### KD-7 — Behaviour-neutral identity

With all depth disabled, #48 registers no consumer on the shared tap (§2(b) — the tap's existence is #37's
concern, not #48's), constructs no recorder, and emits no cue ⇒ the pipeline is
exactly today's viewer and the digest chain is byte-identical. With depth **enabled**, the same holds — that
is KD-2(ii)'s point, and the difference from every other spec in this project is that #48's identity claim
is not "neutral when off" but **"neutral when on"**.

## 5. Persistent state (shape)

**None.** #48 holds no persistent game state, bumps no format version, and adds no save sub-blob — the #37
property (FR-AN-020), for the same reason: everything is derived per-frame from observation plus the live
tap, and the commentary transcript is session-scoped (KD-2). The **exported HTML replay** embeds rendered
commentary, but an export is a display artifact rather than a save — no format version, no restore path, and
nothing the sim reads back (KD-2).

Client-local presentation settings (commentary on/off, audio levels, animation quality) sit **outside the
determinism boundary** with locale and a11y settings — #49's FR-LC-018 already established that class.

## 6. Determinism posture

- Observation + live per-tick tap only; #48 advances no tick and mutates nothing.
- **Draw-free at every tier** — no stream, no tag, nothing serialized (KD-6). The commentary selection value
  is a local keyed mix (KD-2(ii)).
- **Observer neutrality is unconditional**, not conditional on a flag or a KD (§2(d)/(e)).
- Rendering runs display-side, strictly after the tick (FR-LC-005), so neither locale nor presentation
  settings can perturb sim state.
- The commentary transcript is deterministic given the match: same seed ⇒ same lines, same ticks.

## 7. Primary surfaces (proposed)

| Surface | Direction | Notes |
|---|---|---|
| `CommentaryRecorder.OnTick(tick, in events, in observation)` | shared tap → #48 | the #37-class live tap (KD-2(i)), **joined not duplicated**; appends to a session transcript. Runs on the **tick thread** |
| `CommentaryFeedView` (immutable **struct**) | #48 → #38 | via `IViewModelSource<T>` (`where T : struct`); a bounded **window** over the transcript (KD-5), snapshot-copied at the thread boundary |
| `AnimationFrameView` (immutable **struct**) | #48 → #38 | same `where T : struct` constraint as the feed view (KD-5); derived from observation history (KD-3) |
| `ICueSink.Emit(cueId, in params)` | #48 → **its own seam** | declared by #48; the **shell** adapter implements it against #51. #48 never references #51 (KD-4/§10). No-op default keeps a headless run valid |
| `MatchTextBoundary.BuildRequest(intent, mix, in CommentarySlots)` | boundary layer | the #49 sibling adapter (KD-2(iii)); **not** a #48 surface |

## 8. Cross-spec back-props

### 8.1 At approval

**None.** #48 is a pure consumer of surfaces that already exist or are already specified: the observation surface, the live tick tap,
#38's view-model contract, and #49's adapter extension point. This is the same positive property #37, #44
and #46 have, and it is worth stating explicitly because a presentation spec is where "just add a field to
the engine for rendering" pressure lands (KD-3).

### 8.2 Deferred (land at the named tier)

- `ICueSink`'s real implementation, when **#51** lands (KD-4) — a sink change, not a #48 change.
- The 3D renderer, when Unity host access exists (KD-3) — the contract is authorable now.
- Any additive read-only match-engine observation property, **if** a future fidelity proves it underivable
  (KD-3) — with the burden of proof stated there.

### 8.3 Explicitly **not** back-props

- **#16** — no stream, no tag, no ordinal; nothing reserved and nothing needed (KD-6).
- **#49** — #48 adds a sibling adapter, the documented extension point. It **inherits** #35's
  `ERR-049-001` (FR-LC-020's `world.text` binding) as the third spec blocked on that one fix, and files no
  duplicate (§2(d)).
- **#22** — untouched. #48 consumes neither `InteractionTextGenerator` nor `world.text`, superseding the
  plan's KD-2 framing (§2(c)).
- **#37** — a read-only source #48 may sample for stat-driven lines. **The shared tap needs no back-prop
  either**, but it does need a sequencing note: the tap is #37-specified and unbuilt (§2(b)), so whichever of
  #37 / #44 / #48 is implemented first builds it, and the others join. #48's spec must state that it joins
  rather than defines — if #48 lands first, the tap it builds is #37's contract, not a #48 surface.
- **match-engine** — no new field (KD-3).

## 9. Test focus

**Observer neutrality, unconditionally and at full depth:** a match run with commentary, animation and audio
cue mapping all enabled produces a digest chain byte-identical to an unobserved same-seed run (the
`MatchViewerTests` digest lock extended — and note this is now an *unconditional* assertion, where the plan
expected it to be conditioned on KD-2). **Layer-taxonomy locks:** the mechanical `.asmdef` reverse-reference
scan (no sim assembly references #48) **plus** the specific assertion that #48 does not reference
`match-client-core` (KD-1's mutation-channel guard) or `TacticalDirector.Localization` (FR-LC-012).
Commentary transcript determinism (same seed ⇒ identical `{tick, intent, slots}` sequence) and its
replay-equivalence **on both replay paths** (KD-2): an in-session scrub yields the live transcript, and an
**exported HTML artifact contains exactly the lines the live run produced** — the second is the one that can
regress, since the export is the path that must carry what it cannot re-derive. FR-LC-008a coverage over the `CommentaryIntent` roster and its
ordinal-stability lock; the same for `CueId`. `ICueSink`'s no-op default changes nothing observable.
Fail-loud on malformed observation/ledger input. **Thread-boundary locks (KD-5):** the feed window is a
value copy — mutating the transcript after a window is taken does not change the window — and a live
streamed match renders correctly while the tick thread appends (the interactive-viewer race class). And the
negative that keeps KD-3 honest: **no new match-engine surface is referenced** by #48 beyond the enumerated
observation properties.

## 10. Reference DAG

```
#38 (ui-framework) → {#48, match-viewer, match-client-core, match-engine}
#48 → {match-engine, match-viewer}
boundary(MatchTextBoundary) → {#48, #49}          shell(CueSinkAdapter) → {#48, #51}
```

**Acyclic, and the reverse direction is empty:** no sim assembly references #48, and #48 references no
mutation surface. Both of #48's outward seams are inverted the same way — #48 **declares** `ICueSink` and
emits #49 identities, and the **adapters at the shell/boundary** are what touch #51 and
`TacticalDirector.Localization`. So #48 references neither, #51 does not reference #48 either (a Wave-8 spec
must not become a Wave-7 dependency, and it does not), and the no-op default sink keeps a headless run
valid.

## 11. Risks and standing options

- **R-1 — "just expose it from the engine" (KD-3).** The single most likely way this spec damages the
  project. Mitigated by requiring a derivability argument plus an unchanged neutrality lock for any new
  observation property — and by §9 asserting the current property set explicitly, so an addition is visible
  in a diff.
- **R-2 — the transcript is session-scoped, and someone will want it saved** (a shareable match report).
  That is a new persistence surface with a format version and a locale question, i.e. its own decision — not
  a quiet extension of KD-2. Standing option.
- **R-3 — #48 is mostly assets, and the spec is mostly contract.** The engineering reality is that
  animation and audio content dwarf the logic here; the spec must not imply the content is specified by
  specifying the triggers. Its §1 should say so plainly.
- **R-4 — `ERR-049-001` is now load-bearing for three specs** (#35, #46, #48). If #49's owner declines it,
  all three take the `SelectionDraw = 0` fallback and lose phrasing variety at the minimal tier — for #48
  that is the most visible, since repeated commentary lines are immediately noticeable.
- **R-5 — the Unity host gate** (KD-3) blocks the renderer, not the spec. The risk is the inverse of the
  usual one: that #48 is *deferred* wholesale because 3D is blocked, losing the commentary and cue-mapping
  contracts that are authorable now and that #38's match view wants.

## 12. Promotion pipeline

1. **This supplement, AR-converged** — **DONE at v0.6.** AR-1 (0H+3M) → v0.2, AR-2 (0H+1M+1L) → v0.3,
   AR-3 (0H+1M+2L) → v0.4, AR-4 (0H+1M+1L) → v0.5, AR-5 (0H+0M+2L) → v0.6 = **CONVERGENCE** (an L-only
   round closes the cycle, per the project convention).
2. **Author 11 section files** at `Status: IN REVIEW` under `docs/specs/match-presentation-depth/`, FR
   prefix `FR-MP`.
3. **Section-file PASS-1 adversarial review** + a fix pass, recorded in §9.4.1 of the checklist.
4. **`SPEC_INDEX.md` registry row** at promotion.
5. **Lead-developer R-01..R-05 sign-off** — a human authority, not self-grantable.
6. **Flip to `APPROVED`** — with **no** back-props to land (§8.1), which is unusual and is itself the
   evidence that #48 sits correctly in the layer.

## Version History

| Version | Date | Change |
|---|---|---|
| v0.1 | July 26, 2026 | Initial supplement promoted from the one-page plan. Two of the plan's five KDs are answered by work that postdates it, and both shrink #48: **KD-2's** commentary path is #49's seam with a **local keyed mix** for the FR-LC-004 `ulong` (#35's KD-2 mechanism), not #22's `InteractionTextGenerator` on `world.text` — which turns the plan's top risk into a closed question and makes observer neutrality **unconditional** rather than conditioned on the KD; **KD-5's** composition is settled by `match-viewer` / `match-client-core` / `ui-framework` already existing, so #48 is a sibling in that layer rather than a new tier. The constraint the plan misses is **§2(b)**: `SerializeLedger` is write-only and FR-AN-021 states there is **no post-match ledger reader**, so event-driven commentary can only be produced **live** — the replay path replays a captured transcript rather than re-deriving one. KD-1 adds the concrete guard that #48 must not reference `match-client-core`, whose `ILiveMatchMutations`/`ManagerCommandQueue` sit in the same layer and would silently give presentation a write channel (the browser-viewer playback-only precedent). KD-4 takes spec-51's "stub bus API" option over direct playback so #51's arrival is a sink change. **No back-props at approval** — the #37/#44/#46 read-only property. |
| v0.2 | July 26, 2026 | **AR-1 fix pass: 0H + 3M, all resolved.** **M-1** — §2(b) leaned on *"the live per-tick ledger tap"* as though it were a built surface: there is **no `src/match-analytics/`** (#37 is approved and unimplemented) and `EventBus.OnTickBoundary` is a lifecycle reset, not a consumer hook. The tap is a #37-owned **contract** awaiting construction; recorded as such, with #48 as its **third** consumer (after #37/#44 per root `CLAUDE.md`) under an explicit must-not-build-a-second rule, plus a §8.3 sequencing note that whoever implements first builds it to #37's contract. **M-2** — `IViewModelSource<T>` is `where T : struct`, so a variable-length commentary feed cannot be the view model without allocating per frame or handing out a live alias (the `SquadPositionCounts` / `MatchReplay` mutable-handle class); `CommentaryFeedView` is now a **bounded by-value window**, with the transcript staying inside #48. **M-3** — FR-UI-023/F6 pin the engine to the streamer's **tick thread** during a live match while #38 renders on the UI thread; the recorder writes on one and the view is read on the other. Pinned as a snapshot-copy at the boundary — the read-direction counterpart of the marshalling FR-UI-023 already requires for writes — with a test lock added. |
| v0.3 | July 26, 2026 | **AR-2 fix pass: 0H + 1M + 1L, both resolved.** **M-1** — KD-2 promised that the replay path *"replays the transcript"* while also declaring the transcript unsaved, which silently left the **exported HTML replay with no commentary at all**: the export is self-contained and, under KD-2(i)'s capture-don't-re-derive design, cannot reconstruct lines it did not carry. Split the two replay paths — in-session scrub reads memory; the export **embeds rendered text**, baked by the exporter at the boundary layer rather than by #48 (FR-LC-002 intact), which is correct for a display artifact and consistent with the HTML replay's existing "not a determinism-pinned wire format" contract. The claim that matters for §5 — no byte in a game save — survives both. **L-1** — noted that the keyed mix makes two identical triggers at one tick select one variant (right for commentary) and that `tick` is therefore load-bearing in the key. |
| v0.4 | July 26, 2026 | **AR-3 fix pass: 0H + 1M + 2L, all resolved.** **M-1** — §10 asserted `#51 → { }` and, one clause later, that #51 **implements** #48's `ICueSink` — a self-contradiction in the section whose only job is precision, and the resolution matters beyond the typo: if #51 implemented it, the **audio framework would reference a presentation-depth spec**, inverting the layering and giving a Wave-8 spec a Wave-7 dependency. Fixed by putting a `CueSinkAdapter` in the client shell, so both of #48's outward seams (audio and localization) are inverted the same way and neither #51 nor `Localization` is referenced by #48. **L-1** — KD-6 claimed the roadmap "lists the presentation specs as taking no tag"; #48 in fact has no row and no `_RESERVED_` placeholder at all, so the honest statement is #46's: nothing to file, and nothing to promote later either. **L-2** — KD-7 said #48 "taps nothing" when depth is off, implying #48 owns the shared tap; it registers no *consumer* on a tap whose existence is #37's concern. |
| v0.5 | July 26, 2026 | **AR-4 fix pass: 0H + 1M + 1L, both resolved.** **M-1** — §7 still gave `ICueSink.Emit` the direction **#48 → #51**, stale against AR-3's fix and contradicting both KD-4 and §10: #48 declares the seam and the **shell** adapter implements it, so #48 reaches #51 never. The surface table is where an implementer looks first, so a stale direction there outweighs a correct one in prose. **L-1** — `AnimationFrameView` was not marked a struct although the same `IViewModelSource<T>` constraint binds it. |
| v0.6 | July 26, 2026 | **AR-5 sweep: 0H + 0M + 2L, both resolved — CONVERGENCE** (an L-only round closes the cycle). **L-1** — §11 R-3 required §1 to say plainly that #48 specifies triggers rather than content, and §1 did not; the scope table now disowns the asset surface explicitly, since "we specified when the line fires" reading as "we specified the commentary" is the misunderstanding R-3 exists to prevent. **L-2** — §9's replay-equivalence test was near-tautological for the in-session path and silent on the exported one, which is the path AR-2 showed can actually regress (it must carry what it cannot re-derive); split across both. |
