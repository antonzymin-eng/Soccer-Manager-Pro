# Injuries & Medical #41 — Section 4: Architecture

**Created:** July 23, 2026
**Last Updated:** August 7, 2026 (v0.4 — ERR-041-012 at the balance pass: §4.5 rewritten from stream registration to the keyed derivation that actually exists; ordinal 92 stays deliberately unallocated)
**Last Updated (prior):** August 6, 2026 (v0.3 — ERR-041-009: §4.4's layout gains the leading
MEDICAL_SAVE_MAGIC, without which the #29 block decodes here silently; AR pass 1)
**Version:** 0.4
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
    WriteU32(MEDICAL_SAVE_MAGIC)                          # names the format, not just its generation
    WriteU32(MEDICAL_SAVE_FORMAT_VERSION)
    WriteCount(perClubStates.Count)                       # overflow-safe (fail loud on corrupt count, F5)
    for club in perClubStates (ascending ClubId):
        WriteI32(club.ClubId)                             # written, NOT implied by list position
        WriteCount(club.PlayerCount)
        for (playerId, state) in club (PlayerId ascending):
            WriteI32(playerId)
            WriteByte((byte)state.Severity)
            WriteI32(state.RecoveryRemaining)
            WriteI32(state.InjuryCount)
            WriteU32(state.LastAdvancedWorldDay)
    # NO RNG cursor block — injuries.occurrence draws are position-independent keyed draws (KD-1/FR-MD-007)

DecodeMedical(bytes) -> perClubStates:
    magic   = ReadU32(); if magic != MEDICAL_SAVE_MAGIC: throw                     # ERR-041-009
    version = ReadU32(); if version != MEDICAL_SAVE_FORMAT_VERSION: throw          # F3
    count = ReadCount()                                     # overflow-safe bound guard (F5)
    for i in [0, count):
        clubId = ReadI32(); if not strictly ascending: throw                       # duplicate/reordered key
        playerCount = ReadCount()
        for j in [0, playerCount):
            playerId = ReadI32(); if not strictly ascending: throw
            severity = (InjurySeverity)ReadByte(); if not defined: throw           # F4
            recoveryRemaining = ReadI32(); injuryCount = ReadI32(); if either < 0: throw
            lastAdvanced = ReadU32()
            if (recoveryRemaining > 0) != (severity != InjurySeverity.None): throw # F1 coherence gate
            ... reconstruct InjuryState ...
    if bytesRemaining != 0: throw                            # trailing-byte guard, F5
```

Four properties the layout carries, each of which is a MUST (**ERR-041-008** and **ERR-041-009**, at
#41 T1 — the v0.1 sketch above carried none of them explicitly):

- **The block names its own format.** `MEDICAL_SAVE_MAGIC` is written first, and decode MUST refuse a
  block that does not carry it (**ERR-041-009**). The version field cannot do this job: every sub-blob
  format in the save stack — this one, `TRAINING_SAVE_FORMAT_VERSION`, `SEASON_STATE_FORMAT_VERSION`,
  `MATCH_SAVE_FORMAT_VERSION`, `PROGRESSION_SAVE_FORMAT_VERSION` — sits at version 1, so a version gate
  distinguishes one *generation* of a format from the next and never one format from another. The #29
  training block is the acute case, because ERR-029-004 deliberately made it the same byte shape as this
  one: without the magic each codec decodes the other's bytes cleanly and completely, and a severity
  tier arrives as a training focus while a recovery counter arrives as a conditioning cursor, with no
  gate tripped anywhere in the file.
- **`ClubId` is written.** Grouping by club without naming one leaves club identity carried by list order
  across a save boundary — an implicit agreement with a sibling sub-blob this codec is forbidden to read
  (KD-2 blob independence, `unified-season-save-design.md` — that document's KD-7 is the codec/disk-I/O
  split, a different decision). Four bytes per club buys a self-describing block and a duplicate check.
- **Order is not state.** The block is a map keyed by `(ClubId, PlayerId)` (FR-MD-018), so encode
  **canonicalizes** to ascending keys — two equal state sets MUST produce identical bytes whatever roster
  order the caller holds them in — and decode MUST require that order. A duplicate key at encode fails
  loud: there is no defined winner.
- **The F1 coherence gate runs on ENCODE as well as decode**, and `[GT]` bands are gated on neither. A
  codec validating only on decode writes files no load of it can accept, surfacing the bug a session
  later; conversely, enforcing `RECOVERY_MAX` at load would turn a designer's ceiling change into data
  loss across every existing save. Only structurally impossible values (a negative day counter or injury
  count, an undefined severity ordinal) and the F1 contradiction are refused.

Fail-loud gates per F1/F3/F4/F5 (the `MatchSaveCodec` / `WorldStateSerializer.ReadCount` posture). All
fields serialized via #16's `CanonicalSerializer` (bitwise round-trip); **serialize, don't regenerate**.

## 4.5 The keyed occurrence derivation (KD-1) — no registered stream

*(Rewritten at ERR-041-012, discharging the re-anchor ERR-041-002 deferred. The v0.1–v0.3 text required
registering an `injuries.occurrence` stream at `SubsystemOrdinals.InjuriesMedical = 92` "at the first
draw site" — and the first draw site, landed at T0, proved the requirement self-contradictory: a
registered `DeterministicRngService` stream is CURSOR-positioned, which FR-MD-006/007 forbid, and #16
exposes no keyed-draw API. Arming the dial at the balance pass made this the moment normative text
describing a nonexistent stream would have governed a live subsystem.)*

There is **no registered stream and there must not be one**. The draw is a local keyed derivation
(`MedicalStep.DrawOccurrence` — the #30 `RoundResolutionModel.FixtureKey` / `LeagueBootstrap`
precedent): `DOMAIN_TAG_INJURIES_MEDICAL = 0x2A` (allocated in #16 §3.4, ERR-041-001; `[CROSS]`-mirrored
in this catalogue) is folded into the key first, then `playerId`, then the `(worldDay, purpose)` action
ordinal, each through a SplitMix64 finalizer, reduced into `[0, OCCURRENCE_DRAW_DENOM)`.
`SubsystemOrdinals.InjuriesMedical = 92` stays **deliberately unallocated** — an ordinal exists only to
key a registered stream, and a const with no stream behind it is the zero-consumer phantom FR-LW-031
forbids (the ERR-030-012 posture). Because the derivation is keyed and cursor-free, it holds FR-MD-027's
stream-independence property vacuously: nothing #41 does can move any other subsystem's cursor.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-23 | — | Initial architecture: assembly, file layout, seam contracts, save codec, stream registration. Status IN REVIEW. |
| 0.2 | 2026-08-06 | — | **ERR-041-008** (at #41 T1): §4.4's `MEDICAL_SAVE_FORMAT_VERSION` layout gains `WriteI32(club.ClubId)` — v0.1 grouped the blocks by club without naming one, leaving club identity carried by list order across a save boundary, which is an implicit agreement with a sibling sub-blob this codec may not read. Also pins the canonical ascending-key rule, the negative-counter refusals, and the requirement that the F1 coherence gate run on encode as well as decode. |
| 0.3 | 2026-08-06 | — | **ERR-041-009** (AR pass 1 over the T1 landing): §4.4's layout gains a leading `MEDICAL_SAVE_MAGIC`, and decode MUST refuse a block without it. v0.2 relied on the version field to gate the block, but every sub-blob format in the save stack is at version 1 — a version gate separates generations of one format, never one format from another. ERR-029-004 had just made the #29 training block this block's exact byte shape, so each codec decoded the other's bytes completely and silently: injury tiers read back as training focuses, recovery counters as conditioning cursors, every gate green. Also corrects the ERR-041-008 bullet's `KD-7 blob independence` citation to `KD-2` (`unified-season-save-design.md` KD-7 is the codec/disk-I/O split). |
| 0.4 | 2026-08-07 | — | **ERR-041-012** (the balance pass, D4): §4.5 rewritten — the `injuries.occurrence` registered-stream requirement was self-contradictory (cursor-positioned, forbidden by FR-MD-006/007) and was resolved in code at T0 as the keyed derivation (ERR-041-002); arming the dial is the moment the stale text would govern a live subsystem, so it now describes the derivation and pins ordinal 92 as deliberately unallocated (FR-LW-031). (Rows 0.3/0.4 were appended out of order and swapped at the balance-pass AR pass 3 — L2.) |
#endregion
