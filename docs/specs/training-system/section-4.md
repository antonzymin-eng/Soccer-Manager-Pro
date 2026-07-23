# Training System #29 — Section 4: Architecture

**Created:** July 23, 2026
**Last Updated:** July 23, 2026 (v0.2 — APPROVED)
**Version:** 0.2
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
├── TrainingSchedule.cs               // per-club PlayerId → focus
├── CoachingModifier.cs               // KD-3 identity routing seam
├── TrainingStep.cs                   // AdvanceTrainingDay + ComputeTrainingInput + ProjectMatchEntryFatigue
├── InjuryRiskContribution.cs         // KD-5 output
├── TrainingViewModel.cs              // KD-7 observer
├── TrainingSaveCodec.cs              // TRAINING_SAVE_FORMAT_VERSION sub-blob (T1)
├── TrainingSystemConstants.cs        // Appendix A catalogue
└── Tests/ …
```

## 4.3 Seam contracts

- **To #28 (KD-2):** #29 constructs the `TrainingInput` value; #28's `GrowthProjection` reads it. #29
  declares **no** interface for #28 — it calls #28's public API.
- **To the match boot (KD-1):** `ProjectMatchEntryFatigue` returns a `float` the boot caller passes to the
  existing `PlayerAttributeProjection` `float fatigue` parameter. #29 does not know about `MatchEngine`; the
  caller (a higher integration layer / #30 fixture-day path) wires it.
- **From #34 (KD-3):** `CoachingModifier` is a value parameter; #34 becomes the producer when it lands. No
  #34 interface today (FR-LW-031).
- **To #41 (KD-5):** `InjuryRiskContribution` is a read; #41 pulls it. No #41 interface today.

## 4.4 Determinism & persistence

No RNG stream (KD-6). The `TrainingSaveCodec` blob is an opaque, independently version-gated sub-blob under
#30's `SeasonSaveCodec` — the codec never parses #30's other sub-blobs and vice-versa; #30's composing
format-version bump is coordinated at #29 T1. Fail-loud gates per F3/F5. All state serialized via #16's
`CanonicalSerializer` (bitwise round-trip); `serialize, don't regenerate`.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-23 | — | Initial architecture. Status IN REVIEW. |
| 0.2 | 2026-07-23 | — | APPROVED. |
#endregion
