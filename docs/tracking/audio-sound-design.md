# Audio & Sound Design #51 — Design Supplement

> **Created:** July 26, 2026
> **Last Updated:** July 26, 2026 (v0.4 — AR-3 sweep: 0H+0M+2L, **CONVERGENCE**; prior v0.3 AR-2, v0.2 AR-1, v0.1 initial)
> **Version:** 0.4
> **Status:** DESIGN SUPPLEMENT (pre-promotion — no section files, no `SPEC_INDEX.md` row)
> **Candidate spec:** **#51** · **FR prefix:** `FR-AU` · **Wave:** 8 · **Tier:** S1 min → S2 full → S3+ deep
> **Promoted from:** `docs/tracking/spec-plans/spec-51-audio-sound-design.md` v0.2
> **Governing feature definition:** `docs/planning/master-plan-amendment-01-audio-multiplayer-transport.md` §2

---

## 0. Purpose and posture

This supplement resolves the five key decisions the #51 plan defers, against **verified** upstream source
rather than assumption. Design only — no code, no section files, no registry row.

Verification changes the plan in three places, one of them favourably:

- **The plan's largest risk is already void.** It worried that #48 might land direct playback in Wave 7
  and force an audible-neutral rehoming in Wave 8. #48's approved design chose the **stub-sink** option
  explicitly, in its own words *"chosen deliberately over 'direct playback'"* (§2(b)). #51's arrival is
  therefore a **sink implementation**, not a refactor of anything.
- **But the same seam carries a layering contradiction that must be resolved before either spec builds**
  (§2(c)): #48 states both that #51 never references it *and* that #51's catalogue is keyed on #48's
  `CueId`. Both cannot hold. **KD-1** resolves it, and files the back-prop.
- **Five specs now name a "client-local settings store" and no spec owns one** (§2(e)). #51 would be the
  sixth. **KD-3** declines to define a sixth private store and files the ownership back-prop instead.

## 1. Scope

**#51 owns:** the **bus/mixer taxonomy**, the **cue catalogue** (identity → asset + bus + caption
decision), the **playback API**, **ducking** rules, **music** and **UI audio**, the **client-local audio
settings** schema, and the **a11y caption contract** for audible information.

**#51 does not own:**

| Not owned | Owner | How #51 relates |
|---|---|---|
| **When** a match cue fires (event → cue mapping) | **#48** | #48 emits; #51 plays. #51 never observes the match (KD-1) |
| The `ICueSink` seam itself | **#48** declares it; the **shell** implements it | #51 is what the shell's adapter calls — #51 implements nothing of #48's (KD-1) |
| Commentary **text** and its selection | #48 (selection) / #49 (rendering) | #51 plays a commentary *cue*; it neither writes nor localizes a line (KD-4) |
| **Caption / subtitle rendering** | **#49** | #51 declares a caption identity of its own type; #49 renders it (KD-4) |
| The **settings file** and its store | **#38** (proposed — §8.1) | #51 contributes a schema fragment, not a sixth private file (KD-3) |
| Audio **assets** and mix tuning | audio production | #51 specifies identities, routing and contracts — not the content (KD-2 / §11 R-1) |

## 2. What already exists (verified)

**(a) There is no audio code in the tree.** A search across `src/**` for `AudioSource` / audio playback
returns nothing but incidental prose matches in unrelated files (`MatchEnginePhysicsTests`,
`ReplayEngine`, `DeterminismTier`). There is no mixer, no cue, no settings entry, and nothing to retrofit.

**Consequence:** the minimal tier's identity is exact and trivially provable — **silence**. #51 is
purely additive, and "the framework disabled sounds like today" is not an approximation, it is *today*.

**(b) #48 already built the seam and chose the option that protects #51.** Its KD-4 states that until #51
lands, *"#48 emits cue ids into a seam with a trivial default sink — not into a direct playback call"*,
and that this is *"the spec-51 KD-1 'stub bus API' option, chosen deliberately over 'direct playback'"*.
It further pins that **#51 does not implement `ICueSink`; the composition root does**, because
*"having the audio framework implement a presentation-depth spec's interface would make #51 reference #48
— inverting the layering … and making a Wave-8 spec carry a Wave-7 dependency."*

**Consequence:** the plan's §9 inversion risk is closed by an approved decision, and #51 inherits a
constraint it must not quietly break: **#51 references #48 nowhere, in either direction of the id
contract.**

**(c) …and that contract, as written, requires exactly the reference it forbids.** #48's KD-4 closes with:

> *"`CueId` carries the same **APPEND-only ordinal stability** as the text intents, for the weaker but real
> reason that **#51's catalogue will be keyed on it**."*

A catalogue in #51 keyed on a type declared in #48 is a `#51 → #48` reference — the one the sentence three
paragraphs above rules out. The two statements are individually reasonable and jointly impossible.

**Consequence:** this is the load-bearing decision of the supplement (KD-1), not a naming detail. Left
unresolved, whichever spec is implemented second silently acquires a dependency the other's approved text
forbids, and it would surface as an asmdef cycle at T-phase — after both are `APPROVED`.

**(d) Observer neutrality has a built precedent and an existing lock.** `match-viewer` is referenced by no
sim assembly and `MatchViewerTests` digest-locks that a recorded run equals an unobserved same-seed run;
#48 extends the same lock to commentary and cue mapping *"unconditionally, not conditional on a flag"*.

**Consequence:** #51 does not invent an observer-neutrality argument; it inherits one and must not be the
first presentation spec to weaken it (KD-6).

**(e) The "client-local settings store" is named by five specs and owned by none.** Checked at source,
because §8.1 rests on it:

| Spec | Where it says so | What it puts there |
|---|---|---|
| #49 | `localization-accessibility/section-2.md` **FR-LC-018** (a MUST) | locale selection + a11y options |
| #38 | `ui-client-framework/section-4.md` + `section-6.md` | *"UI preferences/layout are client-local settings outside it"* |
| #48 | supplement §5 | *"commentary on/off, audio levels, animation quality"* — citing #49 as having *"already established that class"* |
| #39 | supplement §5 (this wave) | achievement progress + Cloud sync state |
| #51 | this supplement | per-bus volume/mute (KD-3) |

Nothing in `src/` implements any of it, and **no spec claims the file** — each names the *class* and none
names the owner.

Note #48's row already claims *"audio levels"*, so a private #51 store would fork state that an approved
spec believes it describes — a second reason KD-3 declines to define one.

**Consequence:** #51 must not define a sixth private store (KD-3). Note the audio settings are already
*claimed* by #48's sentence above — a second reason #51 defining its own file would fork state that two
approved specs both believe they describe.

**(f) #49's producer discipline forbids the obvious caption shortcut.** Its KD-6 pins that a producer
*"emits only types it already owns"* and that #49-owned types are assembled at the #49 boundary — which is
why no sim-side producer references `TacticalDirector.Localization`.

**Consequence:** the caption identity in #51's catalogue is a **#51-owned** identity, mapped at the
boundary (KD-4). Putting a `LocalizationKey` in the cue catalogue would give the audio framework a
localization reference and break the same rule from the other side.

## 3. Staging (minimal-first → deep)

| Tier | Content |
|---|---|
| **Minimal (the identity)** | **#51 absent.** #48's no-op default sink; no mixer, no settings, no assets. Silence — and per §2(a) that is literally the current build, not an emulation of it. |
| **S2 (full framework)** | The bus graph, cue catalogue, playback API, music + UI audio, settings fragment, ducking, and the caption contract. The shell's `ICueSink` adapter binds #48's mapper to it. |
| **S3+ (deep)** | Commentary-audio delivery alongside #48's presentation-depth tier; richer ducking and mix states. |

**Neutral-settings identity within S2:** with every bus at unity gain and no ducking rule triggered, the
mix is exactly the sum of its cues — so enabling the framework changes routing, not sound. That is the
in-tier identity the eventual §5 asserts.

## 4. Key decisions

### KD-1 — Two id spaces, joined by data in the shell (resolves §2(c))

**#48 owns `CueId` — a semantic event identity** ("goal scored", "whistle", "ball struck"). **#51 owns
`CueKey` — a catalogue identity** naming a playable entry (asset + bus + caption + parameters). Neither
type appears in the other's assembly.

The **shell's `ICueSink` adapter owns the mapping** `CueId → CueKey`, exactly as it already owns the
adapter itself (§2(b)) and as the root owns #49's boundary adapters and #50's generator registry. The
coupling becomes a **table in the composition root**, which is the only place that legitimately sees both.

Why this rather than the two alternatives:

- *Give #51 the `CueId` type* — the literal reading of §2(c). Rejected: it is the forbidden reference, and
  it makes the audio framework's catalogue schema hostage to a presentation-depth spec's event roster.
- *Make `CueId` a bare `int` owned by nobody* — workable, and it does dissolve the reference. Rejected as
  the weaker form of the same answer: it discards #48's APPEND-only ordinal stability guarantee at the
  point where the catalogue actually depends on it, and replaces a typed mapping with an untyped one. The
  mapping table is where the completeness check lives (§9), so it should be typed on both sides.

**Consequences, stated:**

- **The dangling-cue check belongs to the shell**, not to #51 — #51 can only prove *its own* catalogue is
  internally complete; only the mapping knows whether every `CueId` #48 can emit resolves. §9 places the
  test accordingly, which is the honest placement even though it is the less convenient one.
- **A `CueId` with no mapping is silence, not an error, at runtime** — an unmapped event must never crash a
  match. It is a **build-time** completeness failure and a **run-time** no-op. Fail-loud at authoring,
  fail-quiet in the field, because the alternative is a crash in a shipped game over a missing sound.
- **#48's back-prop** (§8.1) corrects the sentence, so the next reader of #48 does not re-derive the
  contradiction.

### KD-2 — A **fixed** bus set at S2; ducking is client config, not a graph

Buses: `Music`, `SFX`, `Crowd`, `Commentary`, `UI` — a fixed, APPEND-only set, plus a master. Every
catalogue entry names exactly one, and the enumeration is closed at Stage 2.

**Fixed over data-driven, deliberately.** A data-driven graph makes "cue routed to a bus that does not
exist" a runtime state, and makes settings/ducking rules reference identities that a content edit can
delete. A closed set makes the catalogue **completeness-checkable by construction** — every entry names a
member of a known enum — which is the property KD-4 and §9 both lean on. Data-driven routing is a
recorded S3+ deferral, to be revisited only if a real mix demands it.

**Ducking** is a small table of `(trigger bus, ducked bus, attenuation, attack/release)` rows, `[GT]`-class
and **client config** — never sim config. Its trigger is **bus activity** ("the commentary bus is
sounding"), not a game event ("a goal was scored"): that is what keeps audio out of the sim's dependency
graph.

**The prohibition is on *sim* state, not on all state** — a distinction worth drawing precisely, because
the over-broad version is unenforceable and would be routed around. A **mix state** selected from
*presentation* context (menu vs. match vs. paused — which #38's navigation shell already owns) is
legitimate and expected; music that never changes between the menu and a match is not a design anyone
will ship. What #51 must not read is match/world/season state: score, possession, tick, morale. The line
is "who owns the value", and #38's navigation state is on the permitted side.

### KD-3 — #51 contributes a settings **fragment**; it does not define a store (resolves §2(e))

The audio settings are `{ perBus: map<Bus, {volume, muted}>, master: {volume, muted} }` — a schema
fragment #51 owns — persisted by **whichever spec owns the client-settings store**, proposed as #38 in
§8.1. #51 defines no file, no path, and no serializer.

**The failure policy is deliberately the opposite of #50's**, and the contrast is the point: an unreadable
or partially-invalid audio settings fragment **resets to defaults and continues**, silently. #50 refuses a
save it cannot classify because a career is irreplaceable; a volume slider is not. Applying save-grade
refusal to preferences would make a corrupt settings byte block launch — the classic mismatch of policy to
stakes. This also puts audio settings explicitly **outside #50's migration scope**, which the plan asked
(KD-3) and which follows directly.

### KD-4 — Caption equivalence by **construction**, in #51's own identity space

Every catalogue entry must declare a caption decision at registration — either a `CaptionId` (a
**#51-owned** identity; #49 renders it, per §2(f)) or an explicit `NoCaption` justified in the entry.
There is no default, so a cue cannot acquire one by omission.

**Why construction rather than an audit** (the plan's KD-4 asks the question): an audited registry drifts
by exactly the cues added after the audit, and audio content grows continuously. A required field cannot
drift. The cost is real and worth naming — an author who wants a sound *now* must make a caption decision
*now* — and `NoCaption` exists precisely so the answer can be "this conveys nothing"; what it cannot be is
unanswered.

**Scope bound:** the obligation covers cues that carry **information** (a goal, a whistle, an error, a
notification). Ambience and layered texture take `NoCaption` as their normal case. The eventual §3 states
that rule so the requirement is not read as "subtitle the crowd loop".

### KD-5 — Host gating: the contract layer is host-free; playback is Unity-only

Host-free and CI-gated: the catalogue and its completeness checks, the bus enumeration, the ducking table's
well-formedness, the settings fragment round-trip, the `CueId → CueKey` mapping completeness, and the
caption-coverage rule. Unity-host-only: actual playback, mixing, and any DSP behaviour.

This is the #38 rendering-binding split, and it carries the same caveat, which the spec must state rather
than imply: **a green contract gate is not a playback green-light.** The same honesty the project applies
to the non-certifying Linux gate applies here.

### KD-6 — Determinism posture and the one prohibition that matters

- **No RNG stream, no domain tag, no `SubsystemOrdinal`**; nothing serialized into any sim save; #16 has
  no row for #51 and needs none (the #37/#44/#46/#48/#50 presentation-and-infra class).
- **Cue variation** (alternating footfall or ball-contact samples) uses **display-side** randomness. It
  must never draw from a `deterministic-sim` stream — not because it would be wrong-looking, but because a
  draw would advance a cursor that is serialized state, making *what you hear* alter *what is saved*.
- **The sim may not read audio state, and the audio path may not call into the sim.** Both directions
  stated, because the plan's phrasing (*"audio code can never become game logic by being read back"*)
  covers only one. An audio callback that queried live match state would put presentation on the tick
  thread's critical path and create a read the digest does not account for. (Presentation context — #38's
  navigation/mix state — is on the permitted side of this line; see KD-2.)
- **Observer neutrality is unconditional** (§2(d)): a full-audio run is byte-identical to an unobserved
  same-seed run.

## 5. Persistent state (shape)

**No sim persistent state; no format-version bump anywhere.** #51's only durable data is the settings
fragment (KD-3), persisted by the client-settings store, outside every determinism-gated save. The cue
catalogue, the bus set, and the ducking table are **content/config artifacts**, not live state.

## 6. Determinism posture

- Presentation; no stream, tag, or ordinal (KD-6).
- Bidirectional isolation from the sim (KD-6) — the load-bearing rule.
- Display-side randomness only; no serialized cursor is touched.
- Observer neutrality unconditional, inherited from the `match-viewer` lock (§2(d)).

## 7. Primary surfaces (proposed)

| Surface | Direction | Notes |
|---|---|---|
| `CueKey` | #51-owned identity | catalogue key; **not** #48's `CueId` (KD-1) |
| `AudioBus` (fixed, APPEND-only enum) | #51 | `Music/SFX/Crowd/Commentary/UI` + master (KD-2) |
| `CueCatalogue` : `CueKey → { asset, AudioBus, caption decision, params }` | #51 | completeness-checkable by construction (KD-2/KD-4) |
| `IAudioPlayback.Play(in CueKey, in CueParams)` / `Stop` / bus control | shell → #51 | what the shell's `ICueSink` adapter calls |
| `DuckingTable` (`[GT]`, client config) | #51 | bus-triggered only, never game-state-triggered (KD-2) |
| `AudioSettingsFragment` | #51 → the client-settings store | schema only; #51 owns no file (KD-3) |
| `CaptionId` | #51-owned; **#49 reads it** | #49 gains the reference when captions land (§10ᵃ) — #51 references #49 never (KD-4/§2(f)) |
| `CueId → CueKey` map | **shell** | the join; owns the dangling-cue check (KD-1/§9) |

**CS0104 note:** `CueKey`, `AudioBus`, `CaptionId` are new names — a `docs/specs/**` + `src/**` grep at T0
must confirm no collision before wiring (the `TacticTranslation` / `PlayerAttributes` precedent).

## 8. Cross-spec back-props

### 8.1 At approval

| ID | Target | Change |
|---|---|---|
| **ERR-048-001** | #48 KD-4 (`match-presentation-depth`, APPROVED) | Correct *"#51's catalogue will be keyed on it [`CueId`]"* — it cannot be, without the `#51 → #48` reference the same KD forbids two paragraphs earlier (§2(c)). Restate as: `CueId` is #48's semantic event identity; **#51's catalogue is keyed on its own `CueKey`**; the shell's `ICueSink` adapter holds the mapping. `CueId`'s APPEND-only ordinal stability is **retained and its rationale strengthened** — the shell's mapping table is keyed on it, so renumbering silently re-points cues. Text-only; no #48 code, contract, or test changes. (`ERR-048-*` is unfiled and unproposed — verified, not assumed.) |
| **ERR-038-004** | #38 (`ui-client-framework`, APPROVED) | Assign ownership of **one client-local settings store** — file location, schema-fragment registration, and the reset-to-defaults failure policy — to the client framework, with #49 (locale + a11y), #48 (presentation), #51 (audio), and #39 (achievements/Cloud state) contributing fragments. Today five specs name this store and none owns it (§2(e)), so each is one implementation decision away from writing its own file. #38 is the natural owner: it is the client framework, it already holds UI preferences, and it is the only one every contributor already composes with. (`ERR-038-001..003` are filed; `-004` is the next free number — verified.) |

### 8.2 Deferred (land at the named tier)

- The **cue identity set** and the shell mapping rows — content, landing with #48's mapper and the first
  audio assets, under KD-1's build-time completeness check.
- **Data-driven bus routing** (KD-2) — S3+, only if a real mix demands it.
- **Commentary-audio delivery** — S3+, alongside #48's deep tier.

### 8.3 Explicitly **not** back-props

- **#49** — #51 is an ordinary caption producer through the existing boundary. The `#49 → #51` asmdef
  reference that captions require (§10ᵃ) is **not** a back-prop: #49's KD-6 already specifies that the
  renderer gains a reference to each producer *as it is built*, so this is the approved design executing,
  not being amended — no requirement changes, no catalogue change, no text change. (Note `ERR-049-001` is
  already proposed by #35 and inherited by #46/#48; #51 adds nothing to it.)
- **#16** — no stream, no tag, nothing reserved (KD-6), and therefore no `_RESERVED_` row to file.
- **#50** — audio settings are explicitly outside migration scope (KD-3).
- **The sim, in any form** — #51's whole design is that it is unreachable from it.

## 9. Test focus

**Mapping completeness, in the shell** (KD-1): every `CueId` #48 can emit resolves to a defined `CueKey` on
a defined bus — a **build-time** failure, paired with the runtime lock that an unmapped id is a **silent
no-op, not a throw**. Both halves matter; a test suite asserting only the first would license a crash in a
shipped game.

**Caption coverage by construction** (KD-4): a catalogue entry cannot be constructed without a caption
decision — asserted by the type/constructor refusing, not by counting entries, because a count is exactly
the audit that drifts.

**Observer neutrality, unconditional** (§2(d)/KD-6): a full-audio match run produces a digest chain
byte-identical to an unobserved same-seed run (the `MatchViewerTests` lock extended). **Layer lock:** a
mechanical scan asserts no sim/loop assembly references the audio assembly, and that the audio assembly
references neither #48 nor `TacticalDirector.Localization` (the two references KD-1/KD-4 exist to prevent
— a scan is the only thing that keeps them prevented). The scan must be written as a **directional**
assertion, not a "these two never appear together" one: `#49 → #51` is legitimate and expected (§10ᵃ), and
a symmetric scan would flag the correct architecture as a violation.

**Neutral-mix identity** (§3): unity gain on every bus with no ducking triggered ⇒ output equals the
unrouted sum. **Settings** (KD-3): the fragment round-trips through the store, and a corrupt or partial
fragment **resets to defaults and continues** — the explicit contrast with #50's refusal. **Host split**
(KD-5): the contract-layer tests run in CI; playback tests are host-gated and marked as such, so a green CI
is never mistaken for verified playback.

## 10. Reference DAG

```
shell → {#48, #51, #49, #38, sim}     #49 → {#51}ᵃ     #51 → { }     #48 → { }     sim → { }
```

ᵃ **The one edge pointing *at* #51, and it is #49's to add, not #51's to avoid.** #49's KD-6 pins that
*"the renderer references each **built** producer"* to consume that producer's native identity types, and
that such a reference *"is added **only when that producer is built** — never speculatively"*. `CaptionId`
is #51-owned (KD-4/§2(f)), so when captions land, `TacticalDirector.Localization` gains a reference to the
audio assembly — the same relationship it already has to `living-world`. This is anticipated by #49's
approved design rather than a change to it (§8.3), and it leaves #51 a leaf, which is what matters.

**#51 is a leaf, and that is the entire architectural content of KD-1.** It references neither the spec
that tells it what to play (#48), nor the one that renders its captions (#49), nor the sim. Everything
that must know two of these lives in the composition root: the `ICueSink` adapter, the `CueId → CueKey`
map, and the caption boundary. This is the third instance of the same inversion in this wave (#48's cue
sink, #50's generator registry, now #51's mapping), which is a sign the pattern is the project's actual
convention for cross-layer joins rather than a one-off.

## 11. Risks and standing options

- **R-1 — asset-heavy, engineering-light.** The spec is contracts and catalogues; the *content* dwarfs it.
  Mix tuning and "match feel" must stay out of spec text (the #48 §11 R-3 risk, which applies here with
  more force since #51 owns the catalogue itself).
- **R-2 — the shell mapping table is a natural dumping ground** (KD-1). It is the one place that sees both
  id spaces, which is exactly why unrelated adapter logic will accumulate there. It should hold the map and
  the adapter, nothing else, and the eventual §4 should say so.
- **R-3 — the settings-store ownership back-prop may be declined** (ERR-038-004). If #38's owner declines,
  #51's fallback is *not* to define a private file but to hold the fragment in memory with persistence
  deferred — a sixth store is worse than no persistence, because it is the failure mode that cannot be
  undone once shipped.
- **R-4 — caption coverage has a real authoring cost** (KD-4), and cost is what erodes construction-time
  rules. The `NoCaption` escape must stay cheap and legitimate, or authors will route around the rule.
- **R-5 — no playback verification in CI** (KD-5). The contract layer can be fully green while the game is
  silent, mis-mixed, or ducking wrongly. The eventual §5 should say plainly which properties remain
  unverified until a host runs it.

## 12. Promotion pipeline

1. **This supplement, AR-converged** — **DONE at v0.4.** AR-1 (0H+2M) → v0.2, AR-2 (0H+1M) → v0.3,
   AR-3 (0H+0M+2L) → v0.4 = **CONVERGENCE** (an L-only round closes the cycle, per the project
   convention).
2. **Author 11 section files** at `Status: IN REVIEW` under `docs/specs/audio-sound-design/`, FR prefix
   `FR-AU`.
3. **Section-file PASS-1 adversarial review** + a fix pass, recorded in §9.4.1 of the checklist.
4. **`SPEC_INDEX.md` registry row** at promotion.
5. **Lead-developer R-01..R-05 sign-off** — a human authority, not self-grantable.
6. **Flip to `APPROVED`**, landing the §8.1 back-props atomically.

## Version History

| Version | Date | Change |
|---|---|---|
| v0.1 | July 26, 2026 | Initial supplement promoted from the one-page plan. Verification closes one risk and opens two findings. **Closed:** the plan's §9 Wave-7/8 inversion risk is void — #48's approved KD-4 already chose the stub-sink option *"deliberately over 'direct playback'"*, so #51's arrival is a sink implementation, not a rehoming. **Finding 1 (KD-1, load-bearing):** #48's KD-4 simultaneously forbids `#51 → #48` and states that *"#51's catalogue will be keyed on"* #48's `CueId` — jointly impossible, and it would have surfaced as an asmdef cycle at T-phase after both specs were APPROVED. Resolved with two id spaces (`CueId` semantic, `CueKey` catalogue) joined by a **shell-owned mapping table**, which also relocates the dangling-cue check to the only layer that can perform it, and pins that an unmapped id is a build-time failure but a **run-time silent no-op**. Filed as ERR-048-001. **Finding 2 (KD-3):** five specs (#49/#48/#38/#39/#51) name a client-local settings store and **none owns it**, so #51 declines to define a sixth private file and files ERR-038-004 assigning the store to #38 with contributed fragments. **Also:** §2(a) verifies there is no audio code at all, making the minimal tier's identity literally today's build; KD-4 requires caption decisions **by construction** rather than by audit, since audio content grows continuously and an audit drifts by exactly what is added after it; KD-6 states the sim/audio prohibition in **both** directions, where the plan stated one. |
| v0.2 | July 26, 2026 | **AR-1 fix pass: 0H + 2M, both resolved.** **M-1** — KD-2 said ducking/routing *"never reads game state"*, which is over-broad to the point of being unenforceable: it forbids selecting a **mix state** from presentation context (menu vs. match vs. paused), which every shipping game does and which #38's navigation shell already owns. An unenforceable rule is routed around rather than followed, and the route is usually a sim read. Restated on the line that actually matters — **sim** state (score, possession, tick, morale) is forbidden; presentation/navigation state is permitted — with KD-6 pointed at the same distinction. **M-2** — §2(e) asserted the five-spec settings-store claim with no checkable citation, while §8.1's `ERR-038-004` rests entirely on it (and partly on "#38 already holds UI preferences"). Verified at source and rewritten as a table: FR-LC-018 (`localization-accessibility/section-2.md`), `ui-client-framework/section-4.md` + `section-6.md`, #48 §5, #39 §5. The check also strengthened the argument — #48's row already claims *"audio levels"*, so a private #51 store would fork state an approved spec believes it describes. |
| v0.3 | July 26, 2026 | **AR-2 fix pass: 0H + 1M, resolved.** **M-1** — §10's DAG showed no edge pointing at #51 and §8.3 claimed *"no #49 change"*, but KD-4 puts a **#51-owned `CaptionId`** in the catalogue, and #49's KD-6 pins that *"the renderer references each **built** producer"* — so `Localization → #51` is required the moment captions land. The DAG was therefore incomplete in the one section whose only job is precision, and the omission is the kind that gets "fixed" later by moving `CaptionId` into #49 — which would give the audio framework a localization reference and break KD-4 from the other side. Edge added with its rationale; §8.3 now distinguishes an anticipated reference (#49's approved design executing) from a back-prop (amending it). #51 remains a leaf either way, which is the property KD-1 protects. |
| v0.4 | July 26, 2026 | **AR-3 sweep: 0H + 0M + 2L, both resolved — CONVERGENCE** (an L-only round closes the cycle). The sweep re-read every statement about reference direction for AR-2 ripple. **L-1** — §7's `CaptionId` row still gave the direction as `#51 → #49`, stale against the corrected DAG and pointing an implementer at exactly the reference KD-4 forbids; the surface table is read before the DAG, so a stale direction there outweighs a correct one below it (the #48 AR-4 lesson). **L-2** — §9's layer lock, written as "the audio assembly references neither #48 nor `Localization`", invites a symmetric "these two never appear together" scan that would flag the legitimate `#49 → #51` edge as a violation; now explicitly required to be **directional**. |
