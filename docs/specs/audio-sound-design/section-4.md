# Audio & Sound Design #51 — Section 4: Architecture

**Created:** July 27, 2026
**Last Updated:** July 27, 2026 (v0.1 — initial section-file set)
**Version:** 0.1
**Status:** APPROVED

---

## 4.1 Assembly and reference direction

New assembly **`TacticalDirector.Audio`** at `src/audio/`, referencing **nothing**.

```
shell → {#48, #51, #49, #38, sim}      #49 → {#51}ᵃ      #51 → { }      #48 → { }      sim → { }
```

ᵃ **The one edge pointing *at* #51 — and it is #49's to add, not #51's to avoid** (§4.5).

**#51 is a leaf, and that is the entire architectural content of KD-1.** It references neither the spec
that tells it what to play (#48), nor the one that renders its captions (#49), nor the simulation.
Everything that must know two of these lives in the composition root: the `ICueSink` adapter, the
`CueId → CueKey` map, and the caption boundary.

**This is the third instance of the same inversion in this wave** — #48's cue sink, #50's generator
registry, now #51's mapping — which is a sign the pattern is the project's actual convention for
cross-layer joins rather than a one-off.

**The reference #51 must not take is #48's, and it is the one the approved text currently asks for**
(§1.4(c)). ERR-048-001 corrects that sentence; until it lands, an implementer reading #48's FR-MP-027
literally would write exactly the reference this section forbids, and would discover it as an assembly
cycle.

## 4.2 File layout

```
src/audio/
├── AudioConstants.cs           # the Appendix A catalogue — no magic numbers in formula code
├── AudioBus.cs                 # FIXED, APPEND-only — a closed enum is what makes routing unfailable
├── CueKey.cs                   # #51's OWN catalogue identity — never #48's CueId
├── CaptionId.cs                # #51-OWNED; #49 reads it, #51 holds no #49 type
├── CaptionDecision.cs          # no default; `default` is DEFINED invalid (FR-AU-024)
├── CueEntry.cs                 # ctor REFUSES an undeclared caption decision
├── CueCatalogue.cs             # CueKey -> CueEntry; internally completeness-checkable
├── DuckingRow.cs               # (trigger, ducked, attenuation, attack, release)
├── DuckingTable.cs             # FM-AU-03 + the FR-AU-017 well-formedness gate
├── AudioSettingsFragment.cs    # SCHEMA ONLY — no file, no path, no serializer
├── IAudioPlayback.cs           # what the SHELL's ICueSink adapter calls
├── AudioMixer.cs               # FM-AU-02 — gain composition; the host-gated half is behind it
└── tests/
```

**`CueSinkAdapter.cs` is deliberately absent from this tree.** It references both #48 and #51, so it lives
in the client shell (§4.4) — placing it here would be the `#51 → #48` reference FR-AU-001 forbids.

**No caption renderer lives here** (FR-AU-007). #51 emits a `CaptionId`; `TacticalDirector.Localization`
renders it, and the reference runs **#49 → #51** (§4.5).

**No sim type appears anywhere in this tree** (FR-AU-006/015). #51 has no engine reference, no observation
surface, and no tap — it is the only presentation-class spec in this wave that cannot even *see* the
simulation.

**CS0104 pre-check.** #51 introduces `AudioBus`, `CueKey`, `CaptionId`, `CaptionDecision`, `CueEntry`,
`CueCatalogue`, `DuckingRow`, `DuckingTable`, `AudioSettingsFragment`, `IAudioPlayback`, `AudioMixer`.
Each was checked against every name that could be in scope with it before authoring, because this project
has hit CS0104 twice (`TacticTranslation`, `PlayerAttributes`).

**One collision is real and unavoidable: `CueParams`.** #48 declares one and #51 declares one, and they
are different types by design — #48's carries what its mapper derived from the observation surface, #51's
carries what playback needs. **Exactly one file in the tree sees both**: the shell's `CueSinkAdapter`. It
MUST fully qualify both from line one (the discipline the `PlayerAttributeProjection` landing adopted for
the same reason), and §5.6 asserts that no other file references both assemblies.

**`CueKey` is deliberately not named `CueId`.** The obvious name is the one #48 already owns, and a
same-named type in two assemblies is precisely the CS0104 class above — with the added hazard that a
reviewer skimming an adapter would not notice which one was meant.

## 4.3 The bus graph

```
Music ─┐
SFX   ─┤
Crowd ─┼─▶ Master ─▶ host output
Comm. ─┤
UI    ─┘
```

**Fixed, closed, APPEND-only** (FR-AU-010/012). Every catalogue entry names exactly one non-master bus;
the master is the composition point, never a routing target for an entry.

**Fixed over data-driven is a correctness decision, not a simplification.** A data-driven graph makes *"a
cue routed to a bus that does not exist"* a runtime state, and lets a content edit delete an identity that
settings rows and ducking rows both reference. The closed set makes the catalogue **completeness-checkable
by construction**, which is the property KD-4 and §5 both lean on. Data-driven routing is a recorded S3+
deferral (§7.2), revisited only if a real mix demands it.

## 4.4 The shell join

```
# CLIENT SHELL — references #48 and #51, and is the only thing that does
sealed class CueSinkAdapter : ICueSink                       # #48 declares; the SHELL implements
{
    readonly IReadOnlyDictionary<MatchPresentation.CueId, Audio.CueKey> _map;   # THE JOIN
    readonly Audio.IAudioPlayback _audio;
}
```

**The mapping is data in the root**, and #51's assembly never learns that `CueId` exists (FR-AU-004).

**The dangling-cue completeness check lives here too** (§5.1), because it can only live here: #51 can
prove its catalogue is internally coherent, and **only the mapping knows whether every `CueId` #48 can
emit resolves**. Placing the check in #51 would require #51 to enumerate #48's roster — the forbidden
reference, arriving through the test suite instead of the production code.

**R-2's warning belongs at this file.** The adapter is the one place that sees both id spaces, which is
exactly why unrelated adapter logic will accumulate in it. It should hold **the map and the adapter,
nothing else**, and any additional responsibility proposed for it should be read as a request to put
cross-layer logic in the one file with permission to see across the layer.

## 4.5 The one edge that points at #51

**`#49 → #51` is legitimate, expected, and #49's to add.** #49's KD-6 pins that *"the renderer references
each **built** producer"* in order to consume that producer's native identity types, and that such a
reference *"is added **only when that producer is built** — never speculatively"*. `CaptionId` is
#51-owned (KD-4 / §1.4(f)), so when captions land, `TacticalDirector.Localization` gains a reference to
the audio assembly — the same relationship it already has to `living-world`.

**This is #49's approved design executing, not being amended** (§8.4), and it leaves **#51 a leaf**, which
is the property that matters.

**It also constrains how the layer scan must be written** (§5.6): the scan must be **directional**. A
symmetric *"these two assemblies never appear together"* check would flag the correct architecture as a
violation — and the natural repair for that false positive is to move `CaptionId` into #49, which would
give the audio framework a localization reference and break KD-4 from the other side.

## 4.6 State and persistence

**#51 holds no sim state, bumps no format version and adds no save block** (FR-AU-037).

Its only durable data is the **settings fragment** (KD-3), persisted by the client-local settings store
and living **outside every determinism-gated save**. #51 defines no file, no path and no serializer
(FR-AU-018), and if ERR-038-004 is declined the fallback is **in-memory with persistence deferred**
(FR-AU-022) — never a private file.

**The cue catalogue, the bus set and the ducking table are content/config artifacts, not live state.**
They are authored, validated at build time, and read-only at run time.

**Audio settings are explicitly outside #50's migration scope** (FR-AU-021), which follows directly from
the reset-to-defaults policy: there is nothing to migrate, because an unreadable fragment is already
defined as *use the defaults*.

## 4.7 Contracts with neighbours

| Neighbour | Contract |
|---|---|
| **#48** | **No reference in either direction** (FR-AU-001/002). #48 emits `CueId` into `ICueSink`; the **shell** adapts. ERR-048-001 corrects the one sentence that currently asks otherwise. |
| **#49** | #51 declares `CaptionId`; **#49 references #51** to render it (§4.5). #51 references `Localization` never (FR-AU-007). |
| **#38** | Proposed owner of the client-settings store (ERR-038-004); also the owner of the navigation/mix context FR-AU-016 permits reading. |
| **the shell / composition root** | Owns the `ICueSink` adapter, the `CueId → CueKey` map, and the completeness check. The only layer that sees both id spaces. |
| **#50** | Audio settings are **outside migration scope** (FR-AU-021). No format version, no step, no registry row. |
| **#16** | **Untouched — no stream, no tag, no ordinal, no `_RESERVED_` row** (FR-AU-032). |
| **the simulation** | **Unreachable, in both directions** (FR-AU-034/035). #51 has no engine reference at all. |

**Standing review item:** #51's isolation rests on two properties a reference graph proves only half of.
The graph shows #51 references nothing — but it cannot show that **the shell adapter stayed a map and an
adapter** (R-2), nor that a future host-side callback did not acquire a sim read (F6). §5.6 asserts the
second behaviourally; the first is a review discipline, and is stated here so it is reviewed rather than
assumed.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-27 | — | Initial §4 (the leaf assembly referencing nothing; file layout with three deliberate absences and the `CueParams` collision named as real, unavoidable and confined to exactly one file; the closed bus graph argued as a correctness decision rather than a simplification; the shell join, with R-2's dumping-ground warning placed at the file it concerns; §4.5 the one inbound edge, with the argument that the layer scan must therefore be **directional** or it will flag the correct architecture and invite the wrong repair; state and persistence; neighbour contracts and a standing review item naming the half a reference graph cannot prove). Status IN REVIEW. |
#endregion
