# Injuries & Medical #41 — Section 4: Architecture

**Created:** July 23, 2026
**Last Updated:** July 23, 2026 (v0.1 — initial authoring)
**Version:** 0.1
**Status:** APPROVED

---

## 4.1 Assembly & reference direction

New assembly `TacticalDirector.InjuriesMedical` (`src/injuries-medical/`), references **only**
`TacticalDirector.TrainingSystem` (#29), `TacticalDirector.PlayerDatabase` (#27), and
`TacticalDirector.DeterministicSim` (#16). It does **not** reference `MatchEngine`, `LivingWorld`,
`SeasonSave`, or #30 itself; #30's season-save assembly references *it* (the one-way composition,
FR-MD-026).

```
#30 SeasonSave/WorldStore ──▶ InjuriesMedical (#41) ──▶ TrainingSystem (#29) ──▶ PlayerProgression (#28)
                                     │                                                    │
                                     ├──▶ PlayerDatabase (#27)   [reads PlayerAttributes]  │
                                     │                                                    ▼
                                     └──▶ DeterministicSim (#16)   [world-tick RNG stream + namespace]
```

#29's assembly is **schema-untouched**: `InjuryRiskContribution` is #29's own already-published output
(FR-TR-017), so #41 reads it without #29 gaining a reference to #41. Likewise #27's assembly is untouched —
`PlayerAttributes` is consumed read-only.

## 4.2 File layout (proposed; lands at T-phase)

```
src/injuries-medical/
├── injuries-medical.asmdef
├── InjurySeverity.cs                 // the severity enum
├── InjuryState.cs                    // the #41-owned per-player state (serialized)
├── MatchLoad.cs                      // KD-3 caller-supplied occurrence input
├── MedicalModifier.cs                // KD-5 identity routing seam
├── MedicalStep.cs                    // AdvanceMedicalDay + ClassifySeverityFromDraw + AssembleRiskScore
├── MedicalViewModel.cs               // KD-8 observer
├── MedicalSaveCodec.cs               // MEDICAL_SAVE_FORMAT_VERSION sub-blob (T1)
├── InjuriesMedicalConstants.cs       // Appendix A catalogue
└── Tests/ …
```

## 4.3 Seam contracts

- **From #29 (KD-2):** `InjuryRiskContribution` is a value #41 reads via #29's public
  `ComputeInjuryRisk` output; #41 declares **no** interface into #29 — it consumes the already-published
  scalar. #41 MUST NOT reference `TrainingState` internals.
- **From #27 (KD-4):** `PlayerAttributes` is read for the robustness/injury-proneness term; #41 declares no
  write path into `PlayerAttributes` (that stays #28's sole `GrowthProjection` writer, per #29's own
  KD-2/FR-PG-008 precedent, transitively honoured here).
- **To #30 (KD-6/KD-8):** #30's `WorldStore.AdvanceDay` loop invokes `AdvanceMedicalDay` per player at the
  new reserved slot (after #28/#29/#33, before the live world-day tick), and reads the read-only
  `IsAvailable` view for squad selection. #41 declares no interface for #30 — #30 calls #41's public API.
- **From #34 (KD-5):** `MedicalModifier` is a value parameter; #34 becomes the producer when it lands. No
  #34 interface today (FR-LW-031).
- **To #38 (KD-8):** `MedicalViewModel` is a read-only value-copy observer; #38 pulls it.
- **Roster membership (FR-MD-025):** #41 exposes an insert/remove entry point over the per-club
  `InjuryState` set that #30 calls at the season boundary from #28's `RegenResult` / `RetirementResult`
  (regen → `InjuryState.Create()`; retiree → remove). #41 does not observe #28's roster events itself — #30
  drives it (the one-way `#30 → #41` composition, mirroring #29's FR-TR-025 handoff).
- **Match event ledger (KD-3, deep tier only):** the deep-tier per-fixture physical-load summary is a
  read-only derivation over the already-emitted Tier-A event ledger (collision / foul events the match
  engine already produces for #37/#44); #41 gains **no** reference to `MatchEngine` and the match engine
  gains **no** reference to #41 — the summary is computed by an integration layer outside both assemblies
  and handed to `AdvanceMedicalDay` via `MatchLoad.HardContacts`.

## 4.4 The `MEDICAL_SAVE_FORMAT_VERSION` sub-blob codec

`MedicalSaveCodec` is an opaque, independently version-gated sub-blob composed into #30's `SeasonSaveCodec`
— the same pattern #28's `PROGRESSION_SAVE_FORMAT_VERSION` and #29's `TRAINING_SAVE_FORMAT_VERSION` blocks
use. The codec never parses #30's other sub-blobs and vice-versa; #30's composing outer
`SEASON_SAVE_FORMAT_VERSION` bump is coordinated at #41's T1 exactly as it was for #28/#29.

```
EncodeMedical(perClubStates) -> bytes:
    WriteU32(MEDICAL_SAVE_FORMAT_VERSION)
    WriteCount(perClubStates.Count)                       # overflow-safe (fail loud on corrupt count, F5)
    for club in perClubStates (deterministic club order):
        WriteCount(club.PlayerCount)
        for (playerId, state) in club (PlayerId ascending):
            WriteI32(playerId)
            WriteByte((byte)state.Severity)
            WriteI32(state.RecoveryRemaining)
            WriteI32(state.InjuryCount)
            WriteU32(state.LastAdvancedWorldDay)
    # NO RNG cursor block — injuries.occurrence draws are position-independent keyed draws (KD-1/FR-MD-007)

DecodeMedical(bytes) -> perClubStates:
    version = ReadU32(); if version != MEDICAL_SAVE_FORMAT_VERSION: throw          # F3
    count = ReadCount()                                     # overflow-safe bound guard (F5)
    for i in [0, count):
        playerCount = ReadCount()
        for j in [0, playerCount):
            playerId = ReadI32()
            severity = (InjurySeverity)ReadByte(); if not defined: throw           # F4
            recoveryRemaining = ReadI32(); injuryCount = ReadI32()
            lastAdvanced = ReadU32()
            if (recoveryRemaining > 0) != (severity != InjurySeverity.None): throw # F1 coherence gate
            ... reconstruct InjuryState ...
    if bytesRemaining != 0: throw                            # trailing-byte guard, F5
```

Fail-loud gates per F1/F3/F4/F5 (the `MatchSaveCodec` / `WorldStateSerializer.ReadCount` posture). All
fields serialized via #16's `CanonicalSerializer` (bitwise round-trip); **serialize, don't regenerate**.

## 4.5 RNG-stream registration (KD-1)

`AdvanceMedicalDay`'s caller registers the `injuries.occurrence` stream on the world-tick
`DeterministicRngService` at `DOMAIN_TAG_INJURIES_MEDICAL = 0x2A` / `SubsystemOrdinals.InjuriesMedical = 92`
(promoted at section-file approval, ERR-041-001 — spec-text-first like `0x22`/`0x20`; the code const + the
stream registration itself land at #41 T2 with the first draw site, per FR-LW-031's no-phantom-stream
posture). Because every draw is keyed on `(playerId, worldDay, purpose)` rather than a free-running cursor,
registering this stream leaves every existing stream's cursor byte-identical (FR-MD-027) — the same
stream-independence property #22's `world.text` and #28's `player-progression.regen` established.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-23 | — | Initial architecture: assembly, file layout, seam contracts, save codec, stream registration. Status IN REVIEW. |
#endregion
