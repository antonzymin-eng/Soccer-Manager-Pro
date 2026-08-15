# Match Presentation Depth #48 — Section 1: Introduction, Scope, Dependencies, Key Decisions

**Created:** July 27, 2026
**Last Updated:** August 15, 2026 (v0.3 — reviewed-findings pass: this section's §1.4(b) and KD-6 built
their live-capture argument and their determinism posture on "one tap feeds #37+#44" — Discipline &
Suspensions #44's own KD-2 line, quoted here verbatim — which #44's `ERR-044-008` (August 15, 2026)
refuted as unachievable under its own §4.1 reference rule, not merely unbuilt. Corrected at all three
sites: the tap is a fill the engine writes once per tick, read by independent accessor interfaces, not
a type shared across assemblies; #48's own argument that it must not build a second tap survives
unchanged, since it never depended on #37 and #44 sharing one)
**Last Updated (prior):** July 27, 2026 (v0.2 — back-prop landed atomically with the ten-spec approval wave; see the version-history row)
**Last Updated (prior):** July 27, 2026 (v0.1 — initial section-file set)
**Version:** 0.3
**Status:** APPROVED

---

## 1.1 Purpose

#48 is the layer that turns a simulated match into something a person watches: which commentary line
fires and when, what the players' animation state is, and which audio cue an event selects.

It is a **mapping spec**, and almost everything it could be confused for belongs to someone else. The
simulation is the engine's, the rendered words are #49's, the audio playback is #51's, the screen is
#38's, the statistics are #37's — and **the content itself** is production's. #48 specifies **triggers,
identities and contracts**. Specifying *when* a line fires is not specifying *the line*, and the asset
surface dwarfs the logic here (§7.4 R-3).

Verification made two of the plan's five decisions **smaller** and one **harder**. Smaller: the
commentary-determinism question is settled by #49's seam plus #35's precedent, and the composition
question is settled by client assemblies that already exist. Harder: **there is no post-match ledger
reader**, which decides whether event-driven commentary can exist on the replay path at all — a
constraint the plan does not mention (§1.4(b)).

## 1.2 Scope

**In scope**

- **Commentary line selection**: which `CommentaryIntent` fires, with what slots, at which tick.
- **Animation / render state**: derived from the observation surface's position history.
- **Audio cue selection**: the mapping from an observed event to a `CueId` plus parameters.
- The **session transcript** that makes replay possible, and its embedding in an exported artifact.

**Out of scope** — each already has an owner; duplicating it is the failure this section prevents:

| Not owned | Owner | #48's relation |
|---|---|---|
| The match simulation and its event ledger | **match-engine** / **#17** | observed **read-only**; #48 emits nothing into it (KD-1) |
| Rendering **text** into a locale | **#49** | #48 is a text **producer**: identity + slots + a `ulong` (KD-2) |
| Audio playback, mixer, buses, the cue **catalogue** | **#51** (Wave 8) | #48 maps event → **cue id**; playback is #51's (KD-4) |
| The UI framework, navigation, the screen hosting the match | **#38** | #38 hosts; #48 supplies view models (KD-5) |
| Match **statistics** | **#37** | a read-only source for stat-driven lines, on the **same** tap (KD-2) |
| Any gameplay outcome | the sim | presentation reads results, **never** produces them (KD-1) |
| **The content** — animation clips, audio assets, the commentary corpus | art / audio / writing production | #48 specifies **triggers, identities and contracts**, and nothing else |

## 1.3 Dependencies

**Upstream (consumed):**

- **match-engine** — the public observation surface (`BallView`, `AgentView(i)`, `AgentTeamId(i)`,
  `AgentIsGoalkeeper(i)`, `PossessingAgentId`, `HomeScore`, `AwayScore`, `MatchEnded`), all **value-type
  copies**; and the **live per-tick event tap** (§1.4(b)).
- **match-viewer** — frame types, as a sibling in the same layer.

**Downstream (consumers):**

- **#38 UI** — renders `CommentaryFeedView` / `AnimationFrameView` through `IViewModelSource<T>`.
- **The boundary layer** — `MatchTextBoundary` renders #48's identities through #49.
- **The client shell** — a `CueSinkAdapter` implements #48's `ICueSink` against #51.

**Reference DAG**

```
#38 (ui-framework) → {#48, match-viewer, match-client-core, match-engine}
#48 → {match-engine, match-viewer}
boundary(MatchTextBoundary) → {#48, #49}        shell(CueSinkAdapter) → {#48, #51}
```

**Acyclic, and the reverse direction is empty:** no sim assembly references #48, and #48 references no
mutation surface. **Both of #48's outward seams are inverted the same way** — #48 *declares* `ICueSink`
and *emits* #49 identities, and the **adapters** at the shell and boundary are what touch #51 and
`TacticalDirector.Localization`. So #48 references neither, and **#51 does not reference #48 either**: a
Wave-8 spec must not become a Wave-7 dependency.

## 1.4 What verification changed

**(a) The client layer #48 composes into is already built.** `src/` carries `match-viewer` (the post-hoc
`HtmlReplayExporter` plus the live `LiveMatchFrame` / `LiveMatchStreamer` path), `match-client-core`
(`MatchClientDriver`, `MatchSession`, `ManagerCommandQueue`, `ILiveMatchMutations`), `match-client-unity`,
and `ui-framework` (#38's T0 substrate). Their references run **one way**, and nothing sim-side
references any of them.

**Consequence:** #48 is a **sibling of `match-viewer` in that layer**, not a new tier. Its host, its
observation path, and its no-reverse-reference discipline all exist — so the plan's composition question
is about *which assembly it lives in*, not about how the layering works.

**(b) There is no post-match ledger reader — and this decides where commentary can exist.** #37's
FR-AN-021 states it in terms: #37 *"MUST consume **live during the match** (there is no post-match ledger
reader); it MUST NOT assume the serialized ledger bytes can be re-parsed."* `EventBus.SerializeLedger` is
a **write-only** surface — the bytes go into the digest and nothing reads them back.

**And #37's own tap consumer is specified but not built — #44's already is, and its own history is the
correction to make here.** `src/match-analytics/` has carried an assembly since July 27, 2026 (value
types + the pure `XgLocationModel`) but no engine wiring, so #37's read is still a **contract awaiting
construction**; `EventBus.OnTickBoundary` is a per-tick *lifecycle reset*, not a consumer hook, either
way. #44 built its own read (`IDisciplineTickLedgerTap`, August 13, 2026) against the same underlying
fill, and its own §4.3 records why *"one tap feeds #37+#44"* — the shape this section quoted verbatim —
is **not achievable, not merely unbuilt**: Discipline & Suspensions #44's §4.1 reference rule makes
#37's identically-shaped interface unreachable from either #44 or the composition root that owns the
match engine, so no shared adapter type exists even once both #37 and #44 carry `src/` assemblies
(`ERR-044-008`, filed August 15, 2026). **What IS shared is the engine's own fill** — one per-tick
record set, written once, read by however many independent accessor interfaces ask for it. **#48 would
be a third such reader, declaring its own accessor shape rather than reusing #37's or #44's, and must
not build a second FILL mechanism** — a parallel re-parse of ledger bytes would double-read the same
ledger with two lifetimes and two sets of ordering assumptions, which is the parallel-surface class
this project keeps catching. The cost of a third reader is a third read of one tick's records, not a
third behaviour.

**Consequence, which the plan does not anticipate:** commentary triggered by *events* — a goal, a card, a
save — can only be produced **live, during the tick loop**, through the engine's per-tick record fill,
read through #48's own accessor rather than a tap shared with #37 or #44. The existing
`HtmlReplayExporter` is a post-hoc exporter over sampled positions, so **a replay cannot reconstruct
event-driven commentary after the fact**. It can only replay commentary that was **captured while the
match ran**, which is why KD-2 makes the output a *recorded artifact* rather than a re-derivation.

**(c) Two questions the plan treats as open are already answered.** Commentary is a **#49 text producer**
— #48 would be its fourth, after #22, #35 and #46 — so the plan's *"consume #22's
`InteractionTextGenerator`"* framing is superseded exactly as #35's was, and #48 never references
`living-world`. And the `world.text` risk the plan names as its top concern is closed by #35's KD-2: a
**local keyed SplitMix64 mix** supplies the FR-LC-004 `ulong` with no stream, no cursor and nothing
serialized.

**Consequence:** observer neutrality becomes **unconditional** rather than — as the plan has it — a test
*"conditioned on that KD-2 decision."*

## 1.5 Key decisions

### KD-1 — Observation-only, enforced structurally

#48 reads (i) the public observation surface's **value copies** and (ii) the **live per-tick event tap**.
It writes nothing, emits no event, advances no tick, and holds no sim state. No sim assembly may
reference it — asserted by the mechanical `.asmdef` reverse-reference scan #38's T0 already ships
(FR-UI-001), extended to #48's assembly.

**The one thing that makes this more than a slogan:** #48 must not acquire a **write path** by accident,
and the tempting one is real. `match-client-core` carries `ILiveMatchMutations` and `ManagerCommandQueue`
— **a genuine mutation surface sitting in the same layer**, one reference away. #48's assembly must not
reference it.

Playback controls (pause, speed) belong to the client shell, and the browser viewer's **playback-only
invariant** is the precedent — the interactive-client AR-1 H-2 finding, and the reason #38's ERR-038-001
exists. **A presentation surface that gains a mutation channel stops being presentation.**

### KD-2 — Commentary is live-captured, display-only, and draw-free

Three decisions, each forced by a different verified fact.

**(i) Live-captured, because there is no post-match ledger reader** (§1.4(b)). A `CommentaryRecorder` taps
the per-tick event stream **during** the run and appends `CommentaryLine` records — `{tick, intent,
slots}`, native values only — to an in-memory transcript. The replay path then **replays the transcript**
rather than re-deriving it. This is the shape `MatchReplayRecorder` already uses for positions, extended
to events.

*Rejected:* re-parse the serialized ledger at replay time. FR-AN-021 states the bytes must not be assumed
re-parseable, `SerializeLedger` is write-only, and building a reader would be a **second, unowned ledger
format**.

**(ii) Display-only and draw-free, so observer neutrality is unconditional.** The FR-LC-004 `ulong` comes
from a **local keyed SplitMix64** over `(tick, intentOrdinal, subjectAgentId)` — #35's KD-2 mechanism — so
#48 registers no stream, touches no cursor, and serializes nothing. **`world.text` is not consumed.**

Two identical triggers at the same tick for the same agent select the **same** variant. That is
deterministic and, for commentary, **correct** — the same moment should not be narrated two ways — but it
means line variety comes from the key varying, so **`tick` is load-bearing in the key** and is present.

This closes the plan's own top risk rather than managing it: because **no draw occurs**, rendering a match
with full commentary cannot perturb the digest **by construction**, and §5.1's neutrality test needs no
conditional.

**(iii) A #49 producer, via a sibling `MatchTextBoundary`.** Its own `CommentaryIntent` roster with the
**ORDINAL STABILITY — APPEND-only** contract, disjoint slots, the FR-LC-015 intent-value pre-gate,
FR-LC-008a coverage over #48's roster, and **no reference** to `TacticalDirector.Localization`
(FR-LC-012). #48 emits identity + slots + the mix; the adapter renders.

**"Not saved" means not in a game save — and the exported replay is the case that distinction exists
for.** There are two replay paths and they differ:

- **In-session replay / scrub** reads the live transcript from memory. Nothing to persist.
- **The exported HTML artifact** is self-contained by design — it already embeds sampled positions — so
  commentary must be **embedded in the export**, or an exported replay silently has none. KD-2(i)'s
  capture-don't-re-derive design makes this the only option: **the file cannot re-derive lines it did not
  carry.**

What gets embedded is **rendered text**, baked by the exporter at the **boundary layer** — not by #48,
which still emits identities only (FR-LC-002 intact). That bakes the export's locale into the file, which
is correct for a display artifact and consistent with the HTML replay's existing contract (*"NOT a
determinism-pinned wire format"*). **FR-LC-006 governs serialized sim state, and no sim state is
involved.**

Neither path puts a byte into a **game save**, which is the claim that matters for §2.2.

### KD-3 — Animation needs no new engine field, and the burden of proof sits on anyone who says otherwise

The observation surface already exposes per-agent position and the ball's position and height — what a
2D/2.5D presentation needs. Richer pose (limb state, foot planting, turn phase) is
**presentation-derived**: a function of the position history #48 already samples, plus its own animation
state machine, which is display state and lives in #48.

**If some future fidelity genuinely needs a sim-side fact** — a foot-strike frame, say — the rule is: it
is an **additive read-only property on match-engine**, in the same class as `BallView` / `AgentView`,
added by a match-engine change, **never a presentation-side push and never a new serialized field**.

The concrete guard is that any such addition must (a) **state why the value cannot be derived** from the
observation history, and (b) **pass the existing observer-neutrality digest lock unchanged**. §5.3 asserts
the *current* property set explicitly, so an addition is visible in a diff rather than arriving quietly.

**The Unity host gate is real and belongs here:** 3D rendering proper is blocked on the same Unity host
access that gates the interactive client (a standing OPEN ISSUE). #48's contract — trigger mapping and
animation **state** — is authorable and testable without it; **only the renderer is gated**.

### KD-4 — Audio is cue selection, and #48 stops at the cue id

#48 maps an observed event to a `CueId` plus parameters, off the **same live tap** as commentary —
read-only, emitting nothing. Playback, mixer, buses and the cue **catalogue** are #51's.

**Until #51 lands, #48 emits cue ids into a seam with a trivial default sink** — not into a direct
playback call. The difference matters: an `ICueSink` with a **no-op default** keeps #51's later arrival a
*sink-implementation* change, whereas direct playback calls scattered through the mapper would make it a
**rewrite of #48**.

**#51 does not implement `ICueSink`; the composition root does.** Having the audio *framework* implement a
presentation-depth spec's interface would make **#51 reference #48** — inverting the layering (a
lower-level service depending on a higher-level consumer) and giving a Wave-8 spec a Wave-7 dependency.
Instead the client shell supplies a small adapter, exactly as the root supplies #49's boundary adapters
and #46's projectors. **Neither spec references the other**, and #48's default no-op sink remains valid
forever for a headless run.

`CueId` carries the same **APPEND-only ordinal stability** as the text intents, for the weaker but real
reason that **the shell's `CueId → CueKey` mapping table** is keyed on it (ERR-048-001, at #51's approval:
this originally said *#51's catalogue* would be keyed on it, which would have required the `#51 → #48`
reference this same key decision forbids — #51's catalogue is keyed on its own `CueKey`, and the **shell**
holds the mapping).

### KD-5 — #48 composes as a sibling of `match-viewer`, hosted by #38

#48 is its own assembly in the presentation layer, referencing `match-engine` (observation) and
`match-viewer` (frame types) — **not** `match-client-core` (KD-1), **not**
`TacticalDirector.Localization` (FR-LC-012), **not** `living-world`, and nothing sim-side references it.

#38 hosts it the way it hosts everything else: #48 exposes **immutable view-model value types** through
#38's `IViewModelSource<T>` contract, so #38 renders without knowing how any of it was produced and #48
owns no navigation, layout or input. That contract is already built (#38 T0), so this is **composition,
not new machinery**.

**Two constraints the contract imposes, both easy to miss:**

- **`IViewModelSource<T>` is `where T : struct`.** A commentary *feed* is variable-length, so the view
  model cannot be a struct wrapping a growing list without either **allocating per frame** or **handing
  out a live alias** — the mutable-handle defect this project has caught repeatedly
  (`SquadPositionCounts`, `MatchReplay`'s frame list, `TacticPreset.Players`). `CommentaryFeedView` is
  therefore a **bounded window**: a fixed-capacity struct carrying the last `COMMENTARY_WINDOW_LINES`
  `[GT]` entries **by value**, never a handle to the transcript. The full transcript stays inside #48.
- **The recorder and the renderer are on different threads.** #38's FR-UI-023 and its F6 pin that during
  a live streamed match the engine is owned by the streamer's **tick thread**, and that even *commands*
  must marshal rather than touch the engine cross-thread. `CommentaryRecorder.OnTick` runs on that tick
  thread; #38 renders on the UI thread. The window is therefore produced by **snapshot-copy at the
  boundary**, never by the renderer reading the live transcript — the same discipline, in the **read**
  direction, that FR-UI-023 imposes in the write direction. The interactive-viewer AR-1 `Start()` /
  `Stop()` race is the precedent for what happens when a lifecycle boundary here is left implicit.

### KD-6 — Determinism posture: presentation/infra, nothing reserved

No RNG stream, no domain tag, no `SubsystemOrdinal` — the `match-viewer` / #37 / #44 / #46 read-only
class. #16's §3.4 catalogue has **no row and no `_RESERVED_` placeholder for #48**, consistent with its
`0x2A` row's note that the read-only / presentation / infra specs take no tag; the keyed mix of KD-2(ii)
is **local arithmetic, not a stream**.

**#16 is untouched — and, as with #46, that means #48 has no reserved value to promote**, so a future
stochastic presentation surface would need a **fresh allocation** rather than a promotion.

### KD-7 — Behaviour-neutral identity: neutral when *on*

With all depth disabled, #48 registers no consumer on the engine's per-tick record fill (that fill's
existence is the engine's concern, not #48's — see §1.4(b) on why no shared tap type exists even for
#37 and #44, the two consumers already built), constructs no recorder, and emits no cue ⇒ the pipeline
is exactly today's viewer and the digest chain is byte-identical.

**With depth enabled, the same holds** — that is KD-2(ii)'s point, and **the difference from every other
spec in this project is that #48's identity claim is not "neutral when off" but "neutral when on".** A
match rendered with full commentary, animation and audio cue mapping produces a byte-identical digest
chain to an unobserved same-seed run.

Stated as its own decision rather than as a clause inside KD-2 because it is the claim a reviewer checks
first, and because it is the property that makes #48 safe to enable by default.

## 1.6 Determinism posture

- **Observation + the live per-tick tap only.** #48 advances no tick and mutates nothing.
- **Draw-free at every tier** — no stream, no tag, nothing serialized (KD-6). The commentary selection
  value is a **local keyed mix** (KD-2(ii)).
- **Observer neutrality is unconditional**, not conditional on a flag or a decision (KD-7).
- Rendering runs **display-side, strictly after the tick** (FR-LC-005), so neither locale nor presentation
  settings can perturb sim state.
- The commentary transcript is **deterministic given the match**: same seed ⇒ same lines, same ticks.
- **No persistent state, no format version, no save sub-blob** (§2.2) — the #37 property, for the same
  reason.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-27 | — | Initial §1 (scope with the content/trigger distinction stated in the out-of-scope table, dependencies + the doubly-inverted DAG, §1.4's verification findings — the built client layer, the missing post-match ledger reader, and the two already-answered questions — KD-1..KD-6 from supplement v0.6 plus **KD-7** promoted to its own decision, determinism posture). KD-7 is separated because *"neutral when on"* is the property that distinguishes #48 from every sibling and is what makes it safe to enable by default. Status IN REVIEW. |
| 0.2 | 2026-07-27 | — | **ERR-048-001** (at #51's approval): KD-4's closing rationale corrected — the shell's `CueId → CueKey` mapping table is keyed on `CueId`, **not** #51's catalogue. See section-2. |
| 0.3 | 2026-08-15 | — | **Reviewed-findings pass, cross-spec back-prop under `ERR-044-008`** (not a new id — #44's own error, whose fix this section quoted before the fix landed). §1.4(b)'s "one tap feeds #37+#44" quote of #44's KD-2, and the "that one shared tap" / "the shared tap" phrasing it fed at the (b) consequence paragraph and at KD-6, all built on a claim #44 §4.1's own reference rule made unachievable (`src/discipline/IDisciplineTickLedgerTap.cs`) and #44 itself withdrew the same day. Restated at all three sites: the engine's per-tick record fill is written once and read by independent accessor interfaces (#44's own `IDisciplineTickLedgerTap`, #37's when built); #48 would be a third such reader, not a third sharer of one type. §1.4(b)'s consequence — commentary can only be produced live, through that fill — is unchanged; only the "shared tap" framing was wrong. See `docs/tracking/spec-error-log.md` `ERR-044-008`. |
#endregion
