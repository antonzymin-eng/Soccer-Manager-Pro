# Audio & Sound Design #51 — Section 5: Test Plan

**Created:** July 27, 2026
**Last Updated:** July 27, 2026 (v0.1 — initial section-file set)
**Version:** 0.1
**Status:** APPROVED

---

Test-ID prefixes follow #19 §3.1.4: `T-AU-U-*` unit, `T-AU-I-*` integration, `T-AU-ID-*` identity,
`T-AU-A11Y-*` accessibility, `T-AU-FAIL-*` fail-loud, `T-AU-BOUND-*` structural, `T-AU-HOST-*`
host-gated.

**Every test below except §5.7 runs without a Unity host** (FR-AU-029). That split is the spec's honest
line, and §5.7 exists to name what a green CI does **not** prove.

## 5.1 Mapping completeness — in the shell, not in #51 (KD-1)

| ID | Test |
|---|---|
| T-AU-I-001 | **Every `CueId` #48 can emit resolves** to a defined `CueKey` on a defined bus. **A build-time failure.** Lives in the shell's suite, because only the mapping sees both id spaces — #51 can prove its catalogue is internally coherent and nothing more. |
| T-AU-I-002 | **An unmapped `CueId` is a silent run-time no-op** — no throw, no log spam, no missing-asset placeholder sound (F1). |
| T-AU-I-003 | **Both halves together.** T-AU-I-001 without T-AU-I-002 licenses a crash in a shipped game over a missing sound; T-AU-I-002 without T-AU-I-001 licenses shipping with silent cues nobody noticed. Asserted as a pair so neither is removed as redundant. |
| T-AU-I-004 | Every `CueKey` in the mapping exists in #51's catalogue — the reverse direction, which catches a mapping row pointing at a deleted entry. |

## 5.2 Caption coverage — by construction, not by audit (KD-4)

| ID | Test |
|---|---|
| T-AU-A11Y-001 | **A `CueEntry` cannot be constructed without a caption decision** (F2). Asserted by the constructor **refusing** — not by counting entries, because a count is exactly the audit that drifts by whatever is added after it. |
| T-AU-A11Y-002 | `default(CaptionDecision)` is **refused** (FR-AU-024): the zero value is *defined as invalid*, so a decision cannot be acquired by omission. |
| T-AU-A11Y-003 | `NoCaption` **without a justification is refused** (F3). Without this, the construction-time rule is satisfiable by reflex and drifts one step later than an audit would. |
| T-AU-A11Y-004 | An **information-carrying** cue with `NoCaption` is flagged at authoring; **ambience with `NoCaption` is valid** (FR-AU-026) — the rule is that the decision was *made*, not that every sound has a caption. |
| T-AU-A11Y-005 | FR-LC-008a base-locale coverage over the full `CaptionId` roster. |
| T-AU-A11Y-006 | **#51 emits no display string** (FR-AU-028): a source-level assertion over `src/audio/` finds no string field, no string return and no string formatting. |

## 5.3 Bus, catalogue and ducking well-formedness (KD-2)

| ID | Test |
|---|---|
| T-AU-U-001 | Every catalogue entry names **exactly one** bus, from the closed enum (FR-AU-011). |
| T-AU-U-002 | **"Routed to a non-existent bus" is not representable** (FR-AU-012) — asserted over the type, since the enum is closed. The mechanical form of the fixed-over-data-driven decision. |
| T-AU-U-003 | `AudioBus` **ordinal stability** — APPEND-only. Settings fragments and ducking rows are both keyed on it, so a reorder silently re-points every volume slider and every ducking rule. |
| T-AU-U-004 | A ducking row with `Trigger == Ducked` is **refused** (F5). |
| T-AU-U-005 | A ducking **cycle** that could sustain indefinite attenuation is **refused** (FR-AU-017). The failure has no error to report and no recovery — a mix that ducks itself into silence just sounds broken. |
| T-AU-U-006 | Ducking is triggered by **bus activity**, never by a game value: asserted structurally, since #51 holds no sim type to trigger on (FR-AU-014). |
| T-AU-U-007 | Gain composition is monotone in each of master volume, bus volume and duck attenuation, and **mute dominates** every gain. |

## 5.4 Settings (KD-3)

| ID | Test |
|---|---|
| T-AU-U-008 | The fragment **round-trips** through the store. |
| T-AU-FAIL-001 | **A corrupt fragment resets to defaults and continues, silently** (F4) — and specifically **does not block launch**. The explicit contrast with #50's refusal, asserted rather than described, because the two policies sit one wave apart and the wrong one is easy to copy. |
| T-AU-FAIL-002 | A **partially**-invalid fragment resets **only the invalid fields**: an out-of-range `Crowd` volume must not discard the player's `Music` setting. |
| T-AU-BOUND-001 | **#51 defines no settings file, path or serializer** (FR-AU-018) — a source-level assertion, since the tempting repair for any settings bug is to write one. |

## 5.5 Identity (KD-6)

| ID | Test |
|---|---|
| T-AU-ID-001 | **The headline lock.** A full-audio match run produces a digest chain **byte-identical** to an unobserved same-seed run (FR-AU-036) — the `MatchViewerTests` lock extended, and **unconditional**: asserted with audio **enabled**, since neutrality with the feature off proves nothing about the feature. |
| T-AU-ID-002 | **#51 absent ⇒ silence, and the build is exactly today's** (FR-AU-038): #48's no-op sink, no mixer, no settings. Per §1.4(a) this is not an emulation of the current build — it *is* it. |
| T-AU-ID-003 | **Neutral-mix identity** (§1.6): unity gain on every bus with no ducking triggered ⇒ the output equals the unrouted sum. **The identity that matters once the trivial one is behind us** — enabling the framework changes routing, not sound. |
| T-AU-ID-004 | **No RNG stream is registered and no cursor moves** (FR-AU-032/033): a full-audio match leaves every registered stream's cursor byte-identical, **including** across cue variation (F7). The one plausible way an audio framework breaks determinism. |
| T-AU-ID-005 | **Nothing is serialized** (FR-AU-037): the season save frame is byte-identical with #51 present and playing. |

## 5.6 Structural locks

| ID | Test |
|---|---|
| T-AU-BOUND-002 | **#51 references nothing** — asserted by the mechanical `.asmdef` scan. In particular it references neither #48 nor `TacticalDirector.Localization` (FR-AU-001/007), the two references KD-1 and KD-4 exist to prevent. |
| T-AU-BOUND-003 | **The scan is DIRECTIONAL, and this is a requirement on the test itself** (§4.5). `#49 → #51` is legitimate and expected, so a symmetric *"these two never appear together"* check would flag the correct architecture — and the natural repair for that false positive is to move `CaptionId` into #49, which breaks KD-4 from the other side. |
| T-AU-BOUND-004 | **No sim or loop assembly references #51** (FR-AU-008) — the FR-UI-001 reverse-reference scan extended. |
| T-AU-BOUND-005 | **#51 holds no sim type at all** (FR-AU-006/015): no engine reference, no observation surface, no tap. Stronger than the behavioural no-read assertions its siblings need, and provable from the reference graph precisely because #51 is a leaf. |
| T-AU-BOUND-006 | **The audio path makes no call into the sim** (FR-AU-035) — asserted behaviourally over the host callback path, which the reference graph does not cover once a host is involved (F6). |
| T-AU-BOUND-007 | **Exactly one file references both #48 and #51** — the shell's `CueSinkAdapter` — and it fully qualifies both `CueParams` types (§4.2). The CS0104 lock, and the mechanical form of R-2's "the adapter holds the map and nothing else". |

## 5.7 Host-gated — and what a green CI does **not** prove (KD-5)

| ID | Test |
|---|---|
| T-AU-HOST-001 | Playback: a cue plays on its routed bus at the composed gain. |
| T-AU-HOST-002 | Ducking behaves audibly as its envelope specifies. |
| T-AU-HOST-003 | Mute and per-bus volume take effect on real output. |

**The contract layer can be entirely green while the game is silent, mis-mixed, or ducking wrongly**
(FR-AU-031). Everything §5.1–§5.6 asserts is about *identities, routing and isolation*; **not one of those
tests can hear anything.** The properties that remain unverified until a host runs them are: that any
sound is produced at all, that gains compose audibly as they compose numerically, that ducking envelopes
sound like ducking rather than like a dropout, and that the mix is one a person would accept.

**Stated plainly rather than implied**, because this project applies exactly this honesty to its
non-certifying Linux gate, and an audio spec is the easiest place in the tree to mistake a green
contract suite for a working feature.

## 5.8 Closed-loop scenario (#19 `ScenarioRunner`, T-phase)

One Simulation-layer scenario, `full-audio-run-is-observer-neutral`, owning specs `{16, 19, 48, 51}`,
registered under `SCENARIO_PATH_CROSS_SPEC_PREFIX`:

run a full match **twice from one seed** — once with no audio, once with the framework enabled, cue
mapping live and cue **variation active** — and assert the two digest chains are **byte-identical** and
every registered RNG cursor is unchanged.

**Cue variation being active is the point of the scenario**, not incidental colour: it is the one part of
an audio framework that plausibly draws, and a neutrality scenario that ran only single-variant cues would
pass while leaving the actual hazard (F7) untested.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-27 | — | Initial §5. §5.1 places mapping completeness **in the shell**, with T-AU-I-003 asserting the two halves as a pair so neither is later removed as redundant. §5.2 asserts caption coverage by **construction** (the ctor refusing), never by counting. T-AU-BOUND-003 makes the directionality of the layer scan a requirement **on the test**, since a symmetric scan would flag the legitimate `#49 → #51` edge and invite the wrong repair. §5.7 names what a green contract gate does not prove, and §5.8's scenario runs with cue **variation active**, which is the whole hazard. Status IN REVIEW. |
#endregion
