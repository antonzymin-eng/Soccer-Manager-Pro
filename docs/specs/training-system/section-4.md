# Training System #29 — Section 4: Architecture

**Created:** July 23, 2026
**Last Updated:** August 6, 2026 (v0.5 — ERR-029-005: §4.4.1's layout gains the leading
TRAINING_SAVE_MAGIC, without which the #41 block decodes here silently; AR pass 1)
**Version:** 0.5
**Status:** APPROVED

---

## 4.1 Assembly & reference direction

New assembly `TacticalDirector.TrainingSystem` (`src/training-system/`), references **only**
`TacticalDirector.PlayerDatabase` (#27) and `TacticalDirector.PlayerProgression` (#28) and
`TacticalDirector.DeterministicSim` (#16). It does **not** reference `MatchEngine`, `LivingWorld`, or the
season-save root; #30's season-save assembly references *it* (the one-way composition, KD-2/FR-TR-024).

```
#30 SeasonSave/WorldStore ──▶ TrainingSystem (#29) ──▶ PlayerProgression (#28) ──▶ PlayerDatabase (#27)
                                     │                                                    ▲
                                     └────────────────────────────────────────────────────┘
                                                    (reads PlayerAttributes)
                                     └──▶ DeterministicSim (#16)   [namespace only — no stream]
```

#28's assembly is **schema-untouched**: `TrainingInput` is #28's own reserved append point, so #29 populates
it without #28 gaining a reference to #29.

## 4.2 File layout (proposed; lands at T-phase)

```
src/training-system/
├── training-system.asmdef
├── TrainingFocus.cs                  // the focus enum
├── TrainingState.cs                  // the #29-owned per-player state (serialized)
├── TrainingSchedule.cs               // read-only VIEW over per-player TrainingState.Focus (not serialized)
├── CoachingModifier.cs               // KD-3 identity routing seam
├── TrainingStep.cs                   // AdvanceTrainingDay + ComputeTrainingInput + ProjectMatchEntryFatigue
├── InjuryRiskContribution.cs         // KD-5 output
├── TrainingViewModel.cs              // KD-7 observer
├── TrainingSaveCodec.cs              // TRAINING_SAVE_FORMAT_VERSION sub-blob (T1)
├── TrainingSystemConstants.cs        // Appendix A catalogue
└── Tests/ …
```

## 4.3 Seam contracts

- **To #28 (KD-2):** #29 constructs each player's `TrainingInput`; #30 gathers them into the batch #28's
  public `AdvanceDay(worldDay, in trainingInputs)` reads (FR-PG-021). #29 declares **no** interface for #28
  — it calls #28's public API. #29's Stage-3 contribution is realized by #28 only when #28's `curveEnabled`
  is on (§2.1 FR-TR-007).
- **To the match boot (KD-1):** `ProjectMatchEntryFatigue` returns a `float` the boot caller passes to the
  existing `PlayerAttributeProjection` `float fatigue` parameter. #29 does not know about `MatchEngine`; the
  caller (a higher integration layer / #30 fixture-day path) wires it.
- **From #34 (KD-3):** `CoachingModifier` is a value parameter; #34 becomes the producer when it lands. No
  #34 interface today (FR-LW-031).
- **To #41 (KD-5):** `InjuryRiskContribution` is a read; #41 pulls it. No #41 interface today.
- **Roster membership (FR-TR-025):** #29 exposes an insert/remove entry point over the per-club
  `TrainingState` set that #30 calls at the season boundary from #28's `RegenResult` / `RetirementResult`
  (regen → `TrainingState.Create(Balanced)`; retiree → remove). #29 does not observe #28's roster events
  itself — #30 drives it (the one-way `#30 → #29` composition).

## 4.4 Determinism & persistence

No RNG stream (KD-6). The `TrainingSaveCodec` blob is an opaque, independently version-gated sub-blob under
#30's `SeasonSaveCodec` — the codec never parses #30's other sub-blobs and vice-versa; #30's composing
format-version bump is coordinated at #29 T1. Fail-loud gates per F3/F5. All state serialized via #16's
`CanonicalSerializer` (bitwise round-trip); `serialize, don't regenerate`.

### 4.4.1 The `TRAINING_SAVE_FORMAT_VERSION` block layout (ERR-029-004)

The byte layout below was **not pinned until #29 T1** — v0.3 of this section described the blob's framing
posture but never its fields, while the sibling #41 §4.4 pinned its own. A format with no cross-version
migration (F3) and no written layout is one an independent reader can only guess at, so it is recorded
here as normative:

```
EncodeTraining(perClubStates) -> bytes:
    WriteU32(TRAINING_SAVE_MAGIC)                         # names the format, not just its generation
    WriteU32(TRAINING_SAVE_FORMAT_VERSION)
    WriteCount(perClubStates.Count)                       # overflow-safe (fail loud on corrupt count, F5)
    for club in perClubStates (ascending ClubId):
        WriteI32(club.ClubId)                             # written, NOT implied by list position
        WriteCount(club.PlayerCount)
        for (playerId, state) in club (PlayerId ascending):
            WriteI32(playerId)
            WriteByte((byte)state.Focus)
            WriteI32(state.Condition)
            WriteI32(state.TrainingFatigue)
            WriteU32(state.LastAdvancedWorldDay)
    # NO RNG cursor block — #29 registers no stream (KD-6 / ERR-029-001)

DecodeTraining(bytes) -> perClubStates:
    magic   = ReadU32(); if magic != TRAINING_SAVE_MAGIC: throw                     # ERR-029-005
    version = ReadU32(); if version != TRAINING_SAVE_FORMAT_VERSION: throw          # F3
    clubCount = ReadCount()                                # overflow-safe bound guard (F5)
    for i in [0, clubCount):
        clubId = ReadI32(); if not strictly ascending: throw                        # duplicate/reordered key
        playerCount = ReadCount()
        for j in [0, playerCount):
            playerId = ReadI32(); if not strictly ascending: throw
            focus = (TrainingFocus)ReadByte(); if not defined: throw                # F4
            condition = ReadI32()
            trainingFatigue = ReadI32(); if < 0: throw                              # structural floor, §2.2
            lastAdvanced = ReadU32()
            ... reconstruct TrainingState ...
    if bytesRemaining != 0: throw                          # trailing-byte guard, F5
```

Four properties the layout carries, each of which is a MUST:

- **The block names its own format.** `TRAINING_SAVE_MAGIC` is written first, and decode MUST refuse a
  block that does not carry it (**ERR-029-005**). The version field cannot do this job: every sub-blob
  format in the save stack — this one, `MEDICAL_SAVE_FORMAT_VERSION`, `SEASON_STATE_FORMAT_VERSION`,
  `MATCH_SAVE_FORMAT_VERSION`, `PROGRESSION_SAVE_FORMAT_VERSION` — sits at version 1, so a version gate
  distinguishes one *generation* of a format from the next and never one format from another. #41's
  medical block is the acute case, because this section deliberately gave the two blocks the same byte
  shape: without the magic each codec decodes the other's bytes cleanly and completely, and a severity
  tier arrives as a training focus while a recovery counter arrives as a conditioning cursor, with no
  gate tripped anywhere in the file. Deliberately not an RNG domain tag — those name draw domains, and a
  save-format identifier must be free to change independently of one.
- **`ClubId` is written.** Grouping by club without naming one leaves club identity carried by list order
  across a save boundary — an implicit agreement with a sibling sub-blob this codec is forbidden to read
  (KD-2 blob independence, `unified-season-save-design.md` — that document's KD-7 is the codec/disk-I/O
  split, a different decision). Four bytes per club buys a self-describing block and a duplicate check.
- **Order is not state.** The block is a map keyed by `(ClubId, PlayerId)` (FR-TR-019), so encode
  **canonicalizes** to ascending keys — two equal state sets MUST produce identical bytes whatever roster
  order the caller holds them in — and decode MUST require that order, so a corrupt blob cannot smuggle in
  a duplicate key. A duplicate key at encode fails loud: there is no defined winner.
- **`[GT]` bands are NOT gated on decode.** `Condition`'s `[CONDITION_MIN, CONDITION_MAX]` band and
  `TRAINING_FATIGUE_MAX` are tunable, and enforcing them at load would turn a designer's ceiling change
  into data loss across every existing save. Only structurally impossible values (a negative fatigue
  accumulator, an undefined focus ordinal) are refused. The band belongs to the day step's clamp (F1).

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-23 | — | Initial architecture. Status IN REVIEW. |
| 0.2 | 2026-07-23 | — | APPROVED. |
| 0.3 | 2026-07-23 | — | PASS-2: §4.3 cites #28's batch `AdvanceDay` + curveEnabled coupling; `TrainingSchedule.cs` file comment = derived view (not serialized). |
| 0.5 | 2026-08-06 | — | **ERR-029-005** (AR pass 1 over the T1 landing): §4.4.1's layout gains a leading `TRAINING_SAVE_MAGIC`, and decode MUST refuse a block without it. v0.4 relied on the version field to gate the block, but every sub-blob format in the save stack is at version 1 — a version gate separates generations of one format, never one format from another. v0.4 had just made this block #41's exact byte shape, so each codec decoded the other's bytes completely and silently: injury tiers read back as training focuses, recovery counters as conditioning cursors, every gate green. Also corrects the `ClubId` bullet's `KD-7 blob independence` citation to `KD-2`. |
| 0.4 | 2026-08-06 | — | **ERR-029-004** (at #29 T1): new **§4.4.1** pins the `TRAINING_SAVE_FORMAT_VERSION` byte layout, which v0.3 never wrote down. Adds the `ClubId` field (club identity must not be positional), the canonical ascending-key rule (order is not state), and the explicit non-gate on `[GT]` bands at decode. |
#endregion
