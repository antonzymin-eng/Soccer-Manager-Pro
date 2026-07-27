# Audio & Sound Design #51 — Section 1: Scope, Dependencies, Key Decisions

**Created:** July 27, 2026
**Last Updated:** July 27, 2026 (v0.1 — initial section-file set)
**Version:** 0.1
**Status:** APPROVED

---

## 1.1 Purpose

**#51 is the audio framework.** It owns the bus/mixer taxonomy, the cue catalogue (identity → asset + bus
+ caption decision), the playback API, the ducking rules, music and UI audio, the client-local audio
settings schema, and the accessibility caption contract for audible information.

It does **not** decide when a match sound fires. #48 maps a match event to a cue identity and emits it
into a seam; #51 plays what it is handed and **never observes a match**.

## 1.2 In scope / out of scope

**In scope**

- The **bus set** and the mixer taxonomy, and every catalogue entry's routing to exactly one bus.
- The **cue catalogue** — a `CueKey` to `{ asset, bus, caption decision, parameters }` mapping.
- The **playback API** the shell's adapter calls.
- **Ducking**, as client config keyed on **bus activity**.
- **Music** and **UI audio**.
- The **audio settings schema fragment**, and the accessibility **caption contract**.

**Out of scope**

| Not owned | Owner | How #51 relates |
|---|---|---|
| **When** a match cue fires (event → cue mapping) | **#48** | #48 emits; #51 plays. #51 never observes the match (KD-1) |
| The `ICueSink` seam | **#48** declares it; the **shell** implements it | #51 is what the shell's adapter calls. **#51 implements nothing of #48's** (KD-1) |
| Commentary **text** and its selection | #48 (selection) / #49 (rendering) | #51 plays a commentary *cue*; it neither writes nor localizes a line (KD-4) |
| **Caption rendering** | **#49** | #51 declares a caption **identity of its own type**; #49 renders it (KD-4) |
| The settings **file** and its store | **#38** (proposed — ERR-038-004) | #51 contributes a schema fragment, **not a sixth private file** (KD-3) |
| Audio **assets** and mix tuning | audio production | #51 specifies identities, routing and contracts — never the content (R-1) |

**The last row is the one to keep in view when reading this spec.** #51 is contracts and catalogues; the
*content* dwarfs it, and a spec that drifted into specifying how a match should sound would be specifying
work it cannot verify.

## 1.3 Dependencies

| Spec | Relationship |
|---|---|
| **#48** Match Presentation Depth | Emits `CueId` into `ICueSink`. **Neither spec references the other** (KD-1); the shell joins them. |
| **#49** Localization & Accessibility | Renders #51's `CaptionId`. **The one edge that points at #51** — and it is #49's to add, per its own KD-6 (§4.5). |
| **#38** UI / Client Framework | Proposed owner of the client-local settings store (ERR-038-004); also owns the navigation/mix context KD-2 permits reading. |
| **#16** Deterministic Simulation | **Untouched** — no stream, no tag, no ordinal (KD-6). |
| **#50** Save Migration | Audio settings are explicitly **outside** migration scope (KD-3). |
| the composition root / client shell | Owns the `ICueSink` adapter **and** the `CueId → CueKey` mapping table (KD-1). |

## 1.4 What already exists (verified, not assumed)

**(a) There is no audio code in the tree at all.** A search across `src/**` for `AudioSource` and for
playback surfaces returns nothing but incidental prose matches in unrelated files
(`MatchEnginePhysicsTests`, `ReplayEngine`, `DeterminismTier`). There is no mixer, no cue, no settings
entry, and nothing to retrofit.

**Consequence:** the minimal tier's identity is exact and trivially provable — **silence**. #51 is purely
additive, and *"the framework disabled sounds like today"* is not an approximation, it is literally today.

**(b) #48 already built the seam, and chose the option that protects #51.** Its KD-4 states that until #51
lands, *"#48 emits cue ids into a seam with a trivial default sink — not into a direct playback call"*,
and that this is *"the spec-51 KD-1 'stub bus API' option, chosen deliberately over 'direct playback'"*.
It further pins that **#51 does not implement `ICueSink`; the composition root does**, because *"having
the audio framework implement a presentation-depth spec's interface would make #51 reference #48 —
inverting the layering … and making a Wave-8 spec carry a Wave-7 dependency."*

**Consequence:** the plan's largest risk — that #48 would land direct playback and force an audible-neutral
rehoming — is **void by an approved decision**. #51's arrival is a **sink implementation**, not a refactor
of anything. And #51 inherits a constraint it must not quietly break.

**(c) …and that same contract, as written, requires exactly the reference it forbids.** #48's KD-4 closes
with *"`CueId` carries the same APPEND-only ordinal stability as the text intents, for the weaker but real
reason that **#51's catalogue will be keyed on it**"* — and #48's section files carry the identical claim
at **FR-MP-027** and in the `CueId` declaration's own comment.

A catalogue in #51 keyed on a type declared in #48 **is** a `#51 → #48` reference: the one ruled out three
paragraphs earlier in the same key decision. The two statements are individually reasonable and jointly
impossible.

**Consequence:** this is the load-bearing decision of the spec (KD-1), not a naming detail. Left
unresolved, whichever spec is implemented second silently acquires a dependency the other's approved text
forbids — and it surfaces as an **assembly cycle at T-phase, after both are APPROVED**. Filed as
ERR-048-001.

**(d) Observer neutrality has a built precedent and an existing lock.** `match-viewer` is referenced by no
sim assembly, and `MatchViewerTests` digest-locks that a recorded run equals an unobserved same-seed run;
#48 extends the same lock to commentary and cue mapping **unconditionally**, not conditioned on a flag.

**Consequence:** #51 does not invent an observer-neutrality argument. It **inherits** one, and must not be
the first presentation spec to weaken it (KD-6).

**(e) The "client-local settings store" is named by five specs and owned by none.** Checked at source,
because the ERR-038-004 back-prop rests on it:

| Spec | Where | What it puts there |
|---|---|---|
| #49 | `localization-accessibility/section-2.md` **FR-LC-018** (a MUST) | locale selection + a11y options |
| #38 | `ui-client-framework/section-4.md` + `section-6.md` | *"UI preferences/layout are client-local settings outside it"* |
| #48 | §4.6 | *"commentary on/off, **audio levels**, animation quality"* |
| #39 | its supplement §5 | achievement progress + Cloud sync state |
| #51 | this spec | per-bus volume/mute (KD-3) |

Nothing in `src/` implements any of it, and **no spec claims the file** — each names the *class* and none
names the owner.

**Consequence:** #51 must not define a **sixth** private store (KD-3). And note #48's row already claims
*"audio levels"*, so a private #51 file would fork state an approved spec believes it describes — a second,
independent reason to decline.

**(f) #49's producer discipline forbids the obvious caption shortcut.** Its KD-6 pins that a producer
*"emits only types it already owns"*, and that #49-owned types are assembled at the #49 boundary — which
is why no sim-side producer references `TacticalDirector.Localization`.

**Consequence:** the caption identity in #51's catalogue is a **#51-owned** identity, mapped at the
boundary (KD-4). Putting a localization key in the cue catalogue would give the audio framework a
localization reference and break the same rule from the other side.

## 1.5 Key decisions

### KD-1 — Two id spaces, joined by data in the shell (resolves §1.4(c))

**#48 owns `CueId` — a semantic event identity** ("goal scored", "whistle", "ball struck"). **#51 owns
`CueKey` — a catalogue identity** naming a playable entry (asset + bus + caption + parameters).
**Neither type appears in the other's assembly.**

The **shell's `ICueSink` adapter owns the mapping `CueId → CueKey`**, exactly as it already owns the
adapter itself (§1.4(b)), and as the root owns #49's boundary adapters and #50's generator registry. The
coupling becomes a **table in the composition root** — the only place that legitimately sees both.

Why this rather than the two alternatives:

- *Give #51 the `CueId` type* — the literal reading of §1.4(c). **Rejected:** it is the forbidden
  reference, and it makes the audio framework's catalogue schema hostage to a presentation-depth spec's
  event roster.
- *Make `CueId` a bare `int` owned by nobody* — workable, and it does dissolve the reference. **Rejected**
  as the weaker form of the same answer: it discards #48's APPEND-only ordinal-stability guarantee exactly
  where the catalogue depends on it, and replaces a typed mapping with an untyped one. The mapping table
  is where the completeness check lives (§5), so it should be typed on both sides.

**Three consequences, stated rather than discovered:**

- **The dangling-cue check belongs to the shell, not to #51.** #51 can prove only that *its own* catalogue
  is internally complete; only the mapping knows whether every `CueId` #48 can emit resolves. §5 places
  the test there — the honest placement, even though it is the less convenient one.
- **An unmapped `CueId` is a build-time failure and a run-time silent no-op.** Fail-loud at authoring,
  fail-quiet in the field: the alternative is a shipped game crashing over a missing sound.
- **#48's text is corrected** (ERR-048-001), so the next reader of #48 does not re-derive the
  contradiction — and `CueId`'s ordinal stability is **retained with a strengthened rationale**, since the
  shell's table is keyed on it and renumbering would silently re-point cues.

### KD-2 — A **fixed** bus set at S2; ducking is client config, not a graph

Buses: `Music`, `SFX`, `Crowd`, `Commentary`, `UI`, plus a master — a **fixed, APPEND-only** set. Every
catalogue entry names exactly one, and the enumeration is closed at Stage 2.

**Fixed over data-driven, deliberately.** A data-driven graph makes *"a cue routed to a bus that does not
exist"* a runtime state, and lets a content edit delete an identity that settings and ducking rows
reference. A closed set makes the catalogue **completeness-checkable by construction** — every entry names
a member of a known enum — which is the property KD-4 and §5 both lean on. Data-driven routing is a
recorded S3+ deferral, revisited only if a real mix demands it.

**Ducking** is a small table of `(trigger bus, ducked bus, attenuation, attack/release)` rows, `[GT]`-class
and **client config**, never sim config. Its trigger is **bus activity** (*"the commentary bus is
sounding"*), not a game event (*"a goal was scored"*) — and that is precisely what keeps audio out of the
simulation's dependency graph.

**The prohibition is on *sim* state, not on all state**, and the distinction must be drawn precisely
because the over-broad version is unenforceable and would be routed around. A **mix state** selected from
*presentation* context — menu vs. match vs. paused, which #38's navigation shell already owns — is
legitimate and expected; music that never changes between the menu and a match is not something anyone
ships. What #51 must not read is **match / world / season** state: score, possession, tick, morale. The
line is *who owns the value*, and #38's navigation state is on the permitted side.

### KD-3 — #51 contributes a settings **fragment**; it defines no store (resolves §1.4(e))

The audio settings are `{ perBus: map<AudioBus, {volume, muted}>, master: {volume, muted} }` — a schema
fragment #51 owns, persisted by **whichever spec owns the client-settings store** (proposed as #38,
ERR-038-004). **#51 defines no file, no path, and no serializer.**

**The failure policy is deliberately the opposite of #50's, and the contrast is the point.** An unreadable
or partially-invalid audio settings fragment **resets to defaults and continues, silently**. #50 refuses a
save it cannot classify because a career is irreplaceable; a volume slider is not. Applying save-grade
refusal to preferences would let a corrupt settings byte block launch — the classic mismatch of policy to
stakes.

This also puts audio settings explicitly **outside #50's migration scope**, which follows directly.

### KD-4 — Caption equivalence by **construction**, in #51's own identity space

**Every catalogue entry must declare a caption decision at registration** — either a `CaptionId` (a
**#51-owned** identity; #49 renders it, per §1.4(f)) or an explicit `NoCaption` justified in the entry.
**There is no default**, so a cue cannot acquire one by omission.

**Why construction rather than an audit.** An audited registry drifts by exactly the cues added after the
audit, and audio content grows continuously. A required field cannot drift. The cost is real and worth
naming — an author who wants a sound *now* must make a caption decision *now* — and `NoCaption` exists
precisely so the answer can be *"this conveys nothing"*; what it cannot be is **unanswered**.

**Scope bound:** the obligation covers cues that carry **information** (a goal, a whistle, an error, a
notification). Ambience and layered texture take `NoCaption` as their normal case. Stated so the
requirement is not read as *"subtitle the crowd loop"*.

### KD-5 — Host gating: the contract layer is host-free; playback is Unity-only

**Host-free and CI-gated:** the catalogue and its completeness checks, the bus enumeration, the ducking
table's well-formedness, the settings fragment round-trip, the `CueId → CueKey` mapping completeness, and
the caption-coverage rule.

**Unity-host-only:** actual playback, mixing, and any DSP behaviour.

This is the #38 rendering-binding split, and it carries the same caveat, which the spec states rather than
implies: **a green contract gate is not a playback green-light.** The same honesty the project applies to
its non-certifying Linux gate applies here, and §5.7 names which properties remain unverified until a host
runs them.

### KD-6 — Determinism posture and the one prohibition that matters

- **No RNG stream, no domain tag, no `SubsystemOrdinal`**; nothing serialized into any sim save; #16 has
  no row for #51 and needs none — the #37 / #44 / #46 / #48 / #50 presentation-and-infra class.
- **Cue variation** (alternating footfall or ball-contact samples) uses **display-side** randomness. It
  must never draw from a `deterministic-sim` stream — not because it would look wrong, but because a draw
  advances a **cursor that is serialized state**, making *what you hear* alter *what is saved*.
- **The sim may not read audio state, and the audio path may not call into the sim.** Both directions,
  because the one-directional phrasing covers only half: an audio callback querying live match state would
  put presentation on the tick thread's critical path and create a read the digest does not account for.
  (Presentation context — #38's navigation/mix state — is on the permitted side; see KD-2.)
- **Observer neutrality is unconditional** (§1.4(d)): a full-audio run is byte-identical to an unobserved
  same-seed run.

## 1.6 Staging

| Tier | Content | Behaviour |
|---|---|---|
| **Minimal (the identity)** | **#51 absent.** #48's no-op default sink; no mixer, no settings, no assets | **Silence** — and per §1.4(a) that is literally the current build, not an emulation of it |
| **S2 (full framework)** | Bus graph, cue catalogue, playback API, music + UI audio, settings fragment, ducking, caption contract. The shell's `ICueSink` adapter binds #48's mapper to it | Audible |
| **S3+ (deep)** | Commentary-audio delivery alongside #48's deep tier; richer ducking and mix states | Audible |

**Neutral-settings identity within S2:** with every bus at unity gain and no ducking rule triggered, the
mix is exactly the sum of its cues — **enabling the framework changes routing, not sound**. That is the
in-tier identity §5.5 asserts, and it is the one that matters once the minimal tier's trivial silence is
behind us.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-27 | — | Initial §1 from supplement v0.4 (scope with the assets-are-not-specified boundary stated up front; the six verified facts, with (c) — the layering contradiction in #48's approved text, now anchored at FR-MP-027 in its section files as well as in its KD-4 — as the spec's load-bearing finding; KD-1..KD-6, including KD-2's precise sim-state-vs-presentation-state line and KD-3's deliberate inversion of #50's failure policy; the three-tier staging whose minimal identity is literally today's build). Status IN REVIEW. |
#endregion
