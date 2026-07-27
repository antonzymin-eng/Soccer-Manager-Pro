# Audio & Sound Design #51 — Section 2: Requirements, Data Structures, Failure Modes

**Created:** July 27, 2026
**Last Updated:** July 27, 2026 (v0.2 — PASS-1 fix pass)
**Version:** 0.2
**Status:** APPROVED

---

## 2.1 Functional requirements

**Layering and the two id spaces (KD-1)**

| FR | Requirement | Level | KD |
|---|---|---|---|
| FR-AU-001 | #51 MUST NOT reference #48, in either direction of the id contract. | MUST | KD-1 |
| FR-AU-002 | #51 MUST NOT implement `ICueSink` or any other #48-declared interface. The **client shell** supplies the adapter. | MUST | KD-1 |
| FR-AU-003 | #51's catalogue MUST be keyed on its **own** `CueKey`, never on #48's `CueId`. | MUST | KD-1 |
| FR-AU-004 | The `CueId → CueKey` mapping MUST live in the **composition root / client shell** — the only layer that legitimately sees both id spaces. | MUST | KD-1 |
| FR-AU-005 | An unmapped or unresolvable `CueId` MUST be a **build-time completeness failure** and a **run-time silent no-op**. It MUST NOT throw in a shipped build. | MUST | KD-1 |
| FR-AU-006 | #51 MUST NOT observe the match: no engine reference, no observation surface, no tap. | MUST | KD-1 |
| FR-AU-007 | #51 MUST NOT reference `TacticalDirector.Localization` (FR-LC-012). | MUST | KD-4 |
| FR-AU-008 | **No sim or loop assembly MUST reference #51** — asserted by the mechanical `.asmdef` reverse-reference scan (FR-UI-001), extended to #51. | MUST | KD-6 |
| FR-AU-009 | #51 MUST remain a **leaf**: it references neither the spec that tells it what to play, nor the one that renders its captions, nor the simulation. | MUST | KD-1 |

**Buses, catalogue and ducking (KD-2)**

| FR | Requirement | Level | KD |
|---|---|---|---|
| FR-AU-010 | The bus set MUST be a **fixed, APPEND-only** enumeration: `Music`, `SFX`, `Crowd`, `Commentary`, `UI`, plus a master. | MUST | KD-2 |
| FR-AU-011 | Every catalogue entry MUST name **exactly one** bus, from that enumeration. | MUST | KD-2 |
| FR-AU-012 | The bus set MUST NOT be data-driven at S2. A cue routed to a non-existent bus MUST NOT be a representable state. | MUST | KD-2 |
| FR-AU-013 | Ducking MUST be a table of `(triggerBus, duckedBus, attenuation, attack, release)` rows, `[GT]`-class and **client config**. | MUST | KD-2 |
| FR-AU-014 | A ducking row's trigger MUST be **bus activity**, never a game event. | MUST | KD-2 |
| FR-AU-015 | #51 MUST NOT read **sim** state — score, possession, tick, morale, or any match / world / season value. | MUST | KD-2 |
| FR-AU-016 | #51 MAY select a **mix state** from **presentation** context (menu / match / paused) owned by #38's navigation shell. This is explicitly permitted, and is not a loophole in FR-AU-015. | MAY | KD-2 |
| FR-AU-017 | A ducking row MUST NOT name a bus as both trigger and target, and the table MUST contain no cycle that could sustain indefinite attenuation. | MUST | KD-2 |

**Settings (KD-3)**

| FR | Requirement | Level | KD |
|---|---|---|---|
| FR-AU-018 | #51 MUST contribute a settings **schema fragment** — `{ perBus: map<AudioBus, {volume, muted}>, master: {volume, muted} }` — and MUST define **no file, no path and no serializer**. | MUST | KD-3 |
| FR-AU-019 | The fragment MUST be persisted by the client-local settings store (proposed #38, ERR-038-004). #51 MUST NOT define a sixth private store. | MUST | KD-3 |
| FR-AU-020 | An unreadable or partially-invalid fragment MUST **reset to defaults and continue**, silently. It MUST NOT block launch and MUST NOT refuse. | MUST | KD-3 |
| FR-AU-021 | Audio settings are **outside #50's migration scope**; no format version, no migration step, no registry row. | MUST | KD-3 |
| FR-AU-022 | If the ERR-038-004 ownership assignment is declined, #51's fallback MUST be to hold the fragment **in memory with persistence deferred** — never to define its own file. | MUST | KD-3 |

**Captions (KD-4)**

| FR | Requirement | Level | KD |
|---|---|---|---|
| FR-AU-023 | Every catalogue entry MUST declare a caption decision **at registration**: either a `CaptionId` or an explicit `NoCaption`. | MUST | KD-4 |
| FR-AU-024 | There MUST be **no default** caption decision. A cue MUST NOT be constructible without one. | MUST | KD-4 |
| FR-AU-025 | `CaptionId` MUST be a **#51-owned** identity. #49 renders it; #51 MUST NOT hold a #49-owned key. | MUST | KD-4 |
| FR-AU-026 | The caption obligation MUST cover cues that carry **information**. Ambience and layered texture MAY take `NoCaption` as their normal case. | MUST | KD-4 |
| FR-AU-027 | `NoCaption` MUST require a stated justification in the entry, so the escape stays deliberate rather than reflexive. | MUST | KD-4 |
| FR-AU-028 | #51 MUST NOT emit a display string (FR-LC-002). Caption text is #49's. | MUST | KD-4 |

**Host gating (KD-5)**

| FR | Requirement | Level | KD |
|---|---|---|---|
| FR-AU-029 | The **contract layer** — catalogue, bus enumeration, ducking well-formedness, settings round-trip, mapping completeness, caption coverage — MUST be testable **without Unity host access**. | MUST | KD-5 |
| FR-AU-030 | Playback, mixing and DSP behaviour MUST be host-gated, and MUST be marked as such. | MUST | KD-5 |
| FR-AU-031 | The spec MUST state plainly which properties remain **unverified** by a green contract gate. A green CI MUST NOT be presented as verified playback. | MUST | KD-5 |

**Determinism (KD-6)**

| FR | Requirement | Level | KD |
|---|---|---|---|
| FR-AU-032 | #51 MUST register **no** RNG stream, allocate **no** domain tag or `SubsystemOrdinal`, and take **no `_RESERVED_` placeholder** — #16 is untouched, and #51 has **nothing to promote later**. | MUST | KD-6 |
| FR-AU-033 | Cue variation MUST use **display-side** randomness and MUST NOT draw from any `deterministic-sim` stream — a draw advances a **serialized cursor**, making what is heard alter what is saved. | MUST | KD-6 |
| FR-AU-034 | The **sim MUST NOT read audio state**. | MUST | KD-6 |
| FR-AU-035 | The **audio path MUST NOT call into the sim**. Both directions are stated because a one-directional rule covers only half the hazard. | MUST | KD-6 |
| FR-AU-036 | **Observer neutrality MUST be unconditional**: a full-audio match run MUST produce a digest chain byte-identical to an unobserved same-seed run. | MUST | KD-6 |
| FR-AU-037 | #51 MUST serialize nothing into any sim save and MUST bump no format version. | MUST | KD-6 |
| FR-AU-038 | With **#51 absent**, the build MUST be exactly today's: #48's no-op sink, no mixer, no settings — **silence**. | MUST | KD-6 |

## 2.2 Data structures

```csharp
// FIXED and APPEND-only (FR-AU-010/012). A closed set is what makes the catalogue
// completeness-checkable BY CONSTRUCTION -- every entry names a member of a known enum.
public enum AudioBus : int { Master = 0, Music, SFX, Crowd, Commentary, UI }

// #51's OWN catalogue identity (FR-AU-003). Deliberately NOT #48's CueId, and deliberately
// not named `CueId` either -- a same-named type in two assemblies is the CS0104 class this
// project has hit twice.
public readonly struct CueKey { public readonly int Value; }

// #51-OWNED caption identity (FR-AU-025). #49 renders it; #51 holds no #49 type, because a
// localization key here would give the audio framework a Localization reference (FR-LC-012).
public readonly struct CaptionId { public readonly int Value; }

// The caption decision. NO DEFAULT (FR-AU-024) -- `default(CaptionDecision)` is refused, so a
// cue cannot acquire a decision by omission. This is the inverse of the zero-value trap the
// wave's siblings carry: here `default` is DEFINED as invalid and fails loud.
public readonly struct CaptionDecision
{
    public readonly bool      HasCaption;      // false => NoCaption, with Justification required
    public readonly CaptionId Caption;
    public readonly int       JustificationId; // FR-AU-027: NoCaption must be justified
}

// One catalogue entry. Constructed only through a ctor that REFUSES an undeclared decision.
public readonly struct CueEntry
{
    public readonly CueKey          Key;
    public readonly AudioBus        Bus;            // exactly one (FR-AU-011)
    public readonly CaptionDecision Caption;        // mandatory (FR-AU-023)
    public readonly AssetRef        Asset;
    public readonly CueParams       Params;
}

// Client config, [GT]. Triggered by BUS ACTIVITY, never by a game event (FR-AU-014).
public readonly struct DuckingRow
{
    public readonly AudioBus Trigger, Ducked;       // Trigger != Ducked (FR-AU-017)
    public readonly int      AttenuationPerMille, AttackMs, ReleaseMs;
}

// Schema only. #51 owns NO file, NO path, NO serializer (FR-AU-018).
public readonly struct AudioSettingsFragment
{
    public readonly int  MasterVolumePerMille;  public readonly bool MasterMuted;
    // per-bus volume/mute, indexed by AudioBus
}

// What the SHELL's ICueSink adapter calls (FR-AU-002/004).
public interface IAudioPlayback
{
    void Play(in CueKey key, in CueParams p);
    void Stop(in CueKey key);
    void SetBus(AudioBus bus, int volumePerMille, bool muted);
}
```

**Types #51 consumes but does not declare — and the two it deliberately does not consume:**

| Type | Owner | #51's use |
|---|---|---|
| The navigation / mix context (menu, match, paused) | **#38** | Read for **mix state** selection — explicitly permitted (FR-AU-016) |
| `CueId`, `ICueSink`, `CueParams`-as-#48-declares-it | **#48** | **Never referenced** (FR-AU-001/002). The shell adapts. Listed so the exclusion is deliberate |
| `TextTemplateId`, `ILocalizer` | **#49** | **Never referenced** (FR-AU-007). #49 reads #51's `CaptionId`, not the reverse |

**Note `CueParams` is declared by both #48 and #51 and they are different types.** #48's carries what its
mapper derived from the observation surface; #51's carries what playback needs. The shell's adapter
translates. This is deliberate and is exactly the KD-1 split applied to the parameter payload — but it is
also a **CS0104 hazard the moment a file references both**, which only the shell adapter does, and §4.2
records the fully-qualified-from-line-one discipline that file must follow.

## 2.3 Failure modes

| ID | Condition | Response |
|---|---|---|
| **F1** | A `CueId` #48 can emit has **no** mapping to a `CueKey`. | **Build-time failure** in the shell's completeness check; **run-time silent no-op** (FR-AU-005). Both halves are required: a shipped game must not crash over a missing sound, and a missing sound must not ship unnoticed. |
| **F2** | A catalogue entry is constructed **without** a caption decision. | **Barred by construction** — the ctor refuses (FR-AU-024). Not an audit, because an audit drifts by exactly the cues added after it. |
| **F3** | A `NoCaption` entry with no stated justification. | **Refused at registration** (FR-AU-027). The escape must stay deliberate; a reflexive `NoCaption` is how caption coverage quietly becomes zero. |
| **F4** | An unreadable or partially-invalid settings fragment. | **Reset to defaults and continue, silently** (FR-AU-020) — the deliberate inverse of #50's refusal, because a volume slider is not a career. |
| **F5** | A ducking row naming the same bus as trigger and target, or a cycle sustaining indefinite attenuation. | **Refused at table validation** (FR-AU-017) — a mix that ducks itself into silence has no error to report and no way to recover. |
| **F6** | Any #51 read of sim state, or any audio-path call into the sim. | **Barred structurally** (FR-AU-034/035) and asserted by the **directional** scan (§5.6). |
| **F7** | Cue variation drawing from a `deterministic-sim` stream. | **Barred** (FR-AU-033). It would advance a serialized cursor, so what the player *hears* would change what is *saved* — the one way an audio framework can break determinism. |
| **F8** | #51 defining its own settings file after ERR-038-004 is declined. | **Barred** (FR-AU-022) — the fallback is in-memory with persistence deferred. **A sixth store is worse than no persistence**, because it cannot be undone once shipped. |

**Deliberately not a failure mode: silence.** With #51 absent, or with every bus muted, the build produces
no sound and that is **correct** (FR-AU-038) — the minimal tier's normal state, and literally today's
build.

**Deliberately not a failure mode: a `NoCaption` cue.** Ambience and texture are expected to take it
(FR-AU-026). The requirement is that the decision was **made**, not that every sound has a caption.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-27 | — | Initial §2 (FR-AU-001..038, data structures, F1..F6) from supplement v0.4. Status IN REVIEW. |
| 0.2 | 2026-07-27 | — | PASS-1 fixes. **M:** added **FR-AU-017 / F5** — the ducking table had no well-formedness rule at all, so a self-ducking row or a cycle between two buses could sustain indefinite attenuation; a mix that ducks itself into silence reports no error and offers no recovery, and it is exactly the kind of table a content author edits without a compiler. **M:** added **FR-AU-022 / F8** — R-3 names the declined-ownership case but nothing said what #51 *does* then, and the obvious answer (define our own file after all) is the one outcome KD-3 exists to prevent; the fallback is now in-memory-with-persistence-deferred, on the stated ground that **a sixth store is worse than no persistence because it cannot be undone once shipped**. **M:** added **FR-AU-027 / F3** — `NoCaption` with no justification requirement would have made KD-4's construction-time rule satisfiable by reflex, which is the same drift an audit suffers, arriving one step later. **L:** wrote out `AudioBus`, `CueKey`, `CaptionId`, `CaptionDecision`, `CueEntry`, `DuckingRow`, `AudioSettingsFragment` and `IAudioPlayback`, each annotated with the constraint that shapes it; recorded that `CaptionDecision`'s `default` is **defined as invalid** — the inverse of the zero-value trap the wave's siblings carry; and named the **`CueParams` collision** explicitly, since #48 and #51 each declare one and the shell adapter is the single file that sees both. |
#endregion
