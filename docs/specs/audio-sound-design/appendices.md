# Audio & Sound Design #51 — Appendices

**Created:** July 27, 2026
**Last Updated:** July 27, 2026 (v0.2 — PASS-1 fix pass)
**Version:** 0.2
**Status:** IN REVIEW

---

## Appendix A — Constant catalogue

Region order per Spec #20: Fixed → Derived → Cross → GT, **omitting any region with no constants** (#20
prohibits empty regions). #51 has no `[EST]` constants and — because it takes **no determinism
reservation** (KD-6) — **no `[CROSS-PENDING]` constants either**, so neither region appears.

### A.1 Fixed

| Constant | Value | Tag | Notes |
|---|---|---|---|
| `GAIN_UNITY_PER_MILLE` | `1000` | `[FIXED]` | Unity gain. **`[FIXED]`, not `[GT]`:** it is the definition of "no change", and the neutral-mix identity (T-AU-ID-003) is stated in terms of it. A tunable unity would make the identity untestable. |
| `MUTED_GAIN_PER_MILLE` | `0` | `[FIXED]` | Silence. Mute **dominates** every other gain term (T-AU-U-007). |

### A.2 Derived

| Constant | Formula | Tag | Notes |
|---|---|---|---|
| `AUDIO_BUS_COUNT` | `Enum.GetValues(typeof(AudioBus)).Length` | `[DERIVED]` | Derived from the enum, **never a hand-maintained literal** — the `POSITION_COUNT` precedent, where two assemblies each carried a private copy of an enum's member count. The settings fragment is sized on it and the ducking validator sweeps it, so a lagging literal would silently stop covering the newest bus. |
| `AUDIO_ROUTABLE_BUS_COUNT` | `AUDIO_BUS_COUNT − 1` | `[DERIVED]` | The master is the composition point, **never a routing target for a catalogue entry** (§4.3). Derived so the exclusion cannot drift out of sync with the enum. |

### A.3 Cross (consumed read-only; never re-declared)

| Constant / type | Authority | Notes |
|---|---|---|
| The navigation / mix context (menu, match, paused) | **#38** | The **permitted** presentation-state read (FR-AU-016). The only external state #51 may consult. |
| The client-settings store's read/write surface | **#38** (proposed, ERR-038-004) | #51 contributes a **fragment**; it owns no file, path or serializer (FR-AU-018). |
| `CueId`, `ICueSink`, #48's `CueParams` | **#48** | **Never referenced** (FR-AU-001/002). The shell adapts. Listed so the exclusion is deliberate rather than accidental. |
| `TextTemplateId`, `ILocalizer`, `LocalizedTextRequest` | **#49** | **Never referenced** (FR-AU-007). #49 reads #51's `CaptionId`; the edge runs `#49 → #51` (§4.5). |
| The host playback / mixer API | Unity | Behind `AudioMixer`'s host-gated half (KD-5). Not a #51 declaration. |

**#51 references nothing** (§4.1), so this table is a list of things it *consults through a seam* or
*deliberately does not touch* — which is unusual for a Cross region and is the mechanical face of the leaf
claim.

### A.4 GT

| Constant | Value | Notes |
|---|---|---|
| `DEFAULT_MASTER_GAIN_PER_MILLE` | `1000` | The default settings fragment. **Client config** — read by no sim assembly (FR-AU-008). |
| `DEFAULT_BUS_GAIN_PER_MILLE[AudioBus]` | `1000` each at S2 | Per-bus defaults. The **mix pass** (§9.4 G4) tunes these against how the game sounds, which needs the host. |
| the ducking table's `AttenuationPerMille` / `AttackMs` / `ReleaseMs` | Appendix C | `[GT]`, **client config**, and the substance of the mix pass. |
| `AU_BUDGET_PLAY_US` | `30` | §6.3 ceiling for one `Play` **on the tick thread**. |
| `AU_BUDGET_DUCK_FRAME_US` | `10` | §6.3 ceiling for one `DuckGain` fold. |
| `AU_BUDGET_SETTINGS_MS` | `5` | §6.3 ceiling for one settings apply. |
| `AU_BUDGET_CATALOGUE_VALIDATE_MS` | `500` | §6.3 ceiling for the **build-time** completeness sweep. |

**The last four are ceilings, not measurements.** No certified number exists for #51 and none is invented
here: a certified figure must come from the pinned Windows 11 / Unity 6000.4.9f1 / DX11 / Mono host per
`certification-platform.md`, and #51 has no implementation to measure. **`AU_BUDGET_PLAY_US` is the only
one whose overrun costs simulation time** (§6.3), because `Play` is called on #48's tick thread.

**No `[GT]` constant in this catalogue affects the simulation** (§9.2), and the gain and ducking rows are
where a reader should check that claim: they are **client config**, they live outside every
determinism-gated save (FR-AU-037), and no sim assembly can reach them (FR-AU-008). Retuning any of them
is a zero-risk change to the simulation and a completely visible one to the player — which is the correct
shape for a mix parameter.

**The bus enumeration is deliberately absent from this table.** It is **fixed and closed**, not a `[GT]`
list: a tunable bus set would make *"routed to a bus that does not exist"* a runtime state and destroy the
by-construction completeness property KD-2 bought (FR-AU-012).

## Appendix B — The bus set and the cue-catalogue schema

### B.1 The bus set — fixed, closed, APPEND-only

| Ordinal | Bus | Routing target for entries? | Typical content |
|---|---|---|---|
| 0 | `Master` | **No** — the composition point | — |
| 1 | `Music` | yes | menu and match music, mix states |
| 2 | `SFX` | yes | ball strikes, impacts, whistles |
| 3 | `Crowd` | yes | ambience, reactions |
| 4 | `Commentary` | yes | spoken lines (S3+) |
| 5 | `UI` | yes | clicks, confirmations, notifications |

**APPEND-only** (T-AU-U-003), and for two independent reasons — either alone would justify it:

1. **The settings fragment is keyed on the ordinal**, so a reorder silently re-points every volume slider
   a player has ever set.
2. **The ducking table is keyed on it too**, so a reorder silently re-points every mix rule.

Neither failure has a version gate in front of it, because neither artifact carries a format version —
the settings fragment resets to defaults rather than validating (FR-AU-020), and the ducking table is
build-time config.

### B.2 The cue-catalogue schema

| Field | Rule |
|---|---|
| `CueKey` | #51-owned; unique within the catalogue (FR-AU-003) |
| `AudioBus` | **exactly one**, from B.1's closed set, never `Master` (FR-AU-011) |
| `CaptionDecision` | **mandatory at construction** — `CaptionId` or justified `NoCaption` (FR-AU-023/024/027) |
| `AssetRef` | one asset, or a variant set for display-side variation |
| `CueParams` | #51's own parameter payload — **a different type from #48's** (§4.2) |

**The catalogue is completeness-checkable by construction**, and every clause above is what makes it so: a
closed bus enum means routing cannot dangle; a mandatory caption decision means coverage cannot drift; a
#51-owned key means the catalogue does not depend on another spec's roster. **What it cannot check is
whether every `CueId` #48 emits has a row** — that is the shell's (§4.4 / T-AU-I-001), and the honest
placement of that check is the direct consequence of KD-1's split.

**#51 has no save layout, so there is no byte table here.** The catalogue, the bus set and the ducking
table are **build-time content/config artifacts**; the only durable #51 data is the Appendix D fragment,
persisted by someone else, outside every determinism-gated save (FR-AU-037). A future reader looking for
the *"#51 sub-blob layout"* appendix that every other spec in this wave carries should read its absence as
a classification.

## Appendix C — The ducking table

`[GT]`, **client config**, validated at build time (FR-AU-013/017).

| Trigger bus | Ducked bus | Attenuation | Attack | Release |
|---|---|---|---|---|
| `Commentary` | `Crowd` | `[GT]` | `[GT]` | `[GT]` |
| `Commentary` | `Music` | `[GT]` | `[GT]` | `[GT]` |
| `UI` | `Music` | `[GT]` | `[GT]` | `[GT]` |

**Magnitudes are deliberately not pinned here.** They are the substance of the mix pass (§9.4 G4), which
requires the Unity host — inventing values now would be a number nobody measured, presented in the one
table a mix engineer will read as authoritative.

**Validation rules (FR-AU-017 / F5):**

- No row may name the same bus as **trigger and target**.
- The table must contain **no cycle** capable of sustaining indefinite attenuation.
- Every named bus must be a member of B.1's closed set — free by construction.

**Every trigger is a bus, and that is the whole architectural content of this appendix.** The natural
designer phrasing is *"duck the crowd when a goal is scored"*, which reads identically and is completely
different: it would require #51 to know what a goal is, hence to read sim state, hence to violate
FR-AU-015. *"The commentary bus is sounding"* carries the same mix intent with none of the coupling.

**A mix state may still be selected from #38's navigation context** (FR-AU-016) — menu vs. match vs.
paused. That is presentation state, not sim state, and the distinction is *who owns the value*.

## Appendix D — The settings fragment

**Schema only.** #51 owns no file, no path and no serializer (FR-AU-018).

| Field | Type | Notes |
|---|---|---|
| `MasterVolumePerMille` | `i32` | clamped on read |
| `MasterMuted` | `bool` | mute dominates gain |
| `BusVolumePerMille[AudioBus]` | `i32` × `AUDIO_BUS_COUNT` | keyed on the **ordinal** — hence B.1's APPEND-only rule |
| `BusMuted[AudioBus]` | `bool` × `AUDIO_BUS_COUNT` | |

**The failure policy is the deliberate inverse of #50's, and the contrast is the design** (KD-3 / F4): an
unreadable or partially-invalid fragment **resets to defaults and continues, silently**. #50 refuses a
save it cannot classify because a career is irreplaceable; a volume slider is not, and applying save-grade
refusal to preferences would let **a corrupt settings byte block launch**.

**A partially-invalid fragment resets only the invalid fields** (T-AU-FAIL-002). An out-of-range `Crowd`
volume must not discard the player's `Music` setting — the coarse "reset the whole file" reading is the
easy implementation and the wrong one.

**Three things this fragment deliberately is not:**

1. **A file.** It is a contribution to **one** client-local settings store (ERR-038-004). Five specs name
   that store and none owns it; #51 declines to be the sixth to write its own (§1.4(e)).
2. **Migratable.** Audio settings are outside #50's scope (FR-AU-021) — an unreadable fragment is already
   *defined* as "use the defaults", so there is nothing for a migration to do.
3. **Sim state.** It carries no format version, enters no save, and is read by no sim assembly
   (FR-AU-037/008).

**If ERR-038-004 is declined**, the fragment is held **in memory with persistence deferred** (FR-AU-022) —
never written to a private file. A sixth store is worse than no persistence, because it is the failure
mode that cannot be undone once shipped.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-27 | — | Initial appendices (A.1 Fixed with the argument that unity gain is a definition rather than a dial; A.2 Derived, sizing the settings fragment and the validator from the enum rather than from a literal; A.3 Cross, which is unusually a list of seams and deliberate exclusions because #51 references nothing; A.4 GT; B the closed bus set with its **two independent** APPEND-only reasons and the catalogue schema, incl. the note that the one thing the catalogue cannot self-check is the shell's; C the ducking table with unpinned magnitudes and its validation rules; D the settings fragment with its three deliberate non-identities). Status IN REVIEW. |
| 0.2 | 2026-07-27 | — | PASS-1 fixes. **M:** the four `[GT]` budget ceilings declared in §6.3 were **absent from this catalogue**, which is meant to be the single catalogue and is what a reader greps for tag discipline — **the #45 PASS-1 M-2 defect, now seen for the ninth time in this wave**, which at this point is a finding about the order sections get authored in rather than nine independent slips; added to A.4 together with the default gains, whose absence had left §9.2's *"no `[GT]` affects the simulation"* claim with nothing to check itself against. **M:** added **A.2 `AUDIO_ROUTABLE_BUS_COUNT`** — the master-is-not-a-routing-target rule was stated in §4.3 prose and nowhere derivable, so a validator would have re-encoded the exclusion as a literal. **L:** A.1 gained the reason unity gain is `[FIXED]` (the neutral-mix identity is stated in terms of it); A.4 gained the explicit note that the **bus enumeration is deliberately not `[GT]`**; B.1 spelled out the **two independent** reasons for APPEND-only and recorded that neither failure has a version gate in front of it; B.2 gained the sentence that #51 has **no save layout at all**, so the byte-table appendix every sibling in this wave carries is absent by classification; C recorded that the magnitudes are unpinned **on purpose** rather than pending; D gained the partial-reset rule and the three deliberate non-identities. |
#endregion
