# Squad / Player Data Layer Specification #27 — Section 5: Test Plan and Traceability

**Created:** July 22, 2026
**Last Updated:** July 22, 2026 (v0.1)
**Status:** IN REVIEW

---

The T0 data layer is covered by three unit/determinism suites in `src/player-database/tests/`; no
`ScenarioRunner` closed-loop scenario exists at T0 (nothing is wired into an orchestrator to exercise
end-to-end — that arrives with T1's `ConfigureSquads`, design supplement §5). The landed T-phase FRs
(022–026) are covered by the match-engine suites named below.

## 5.1 Unit / determinism (`src/player-database/tests/`)

**`PlayerAttributesTests.cs`** — `CreateDefault_AllAttributesMidRange_WeakFootBase` (all 31 fields = 10,
WeakFoot = 3); `AttrIdx_Count_MatchesConstant`; `ToArray_FromArray_RoundTrips`;
`FromArray_WeakFootRating_Untouched` (the `[1,5]` scale stays out of the `[1,20]` array);
`FromArray_WrongLength_Throws` (F1); `PositionBias_{Goalkeeper,Defender,Midfielder,Forward}_ExactValues`
(direct constant-value assertions per AR-2 — not statistical sampling); `PositionBias_TableLength_MatchesAttributeCount`.

**`RosterGeneratorTests.cs`** — `Generate_SameSeed_ProducesIdenticalSquad` /
`Generate_DifferentSeed_ProducesDifferentSquad` (two-run determinism);
`Generate_PlayerIds_AreClubScopedAndSequential` / `Generate_PlayerIds_NoCollisionAcrossClubs` (KD-3);
`Generate_AllGeneratedValues_WithinDeclaredBounds` (`[1,20]` attrs, `[1,5]` weak foot, `[AgeMin,AgeMax]`,
defined position — the clamp/spread locks); `Generate_RngCursor_AdvancesByExactBudgetPerPlayer` (the
F4 `FIELDS_PER_PLAYER = 36` budget lock); `Generate_InvalidCount_Throws` (F3); `Generate_NullRng_Throws`.
The `NewRng` helper registers the stream (siteId `"player-database.roster-generation"`,
`SubsystemOrdinals.PlayerDatabase`, `entityId: clubId`), exercising FR-SQ-013 on every case.

**`SquadFileLoaderTests.cs`** — `Parse_{NullText,EmptyOrCommentOnly}_AllDefaultSquad` (omitted ⇒ default,
KD-8); `Parse_FullRecord_RoundTrips` (every field + club-scoped `PlayerId` + omitted-key inheritance);
`Parse_OmittedSection_IsFullIdentity`; and every fail-loud gate (F5): `Parse_UnknownKey_Throws`,
`Parse_UnknownSection_Throws`, `Parse_DuplicateSection_Throws`, `Parse_DuplicateKey_Throws`,
`Parse_KeyBeforeSection_Throws`, `Parse_OutOfRangeAttribute_Throws`, `Parse_OutOfRangeWeakFoot_Throws`,
`Parse_UnparsableInt_Throws`, `Parse_OutOfRangeAge_Throws` + `Parse_AgeAtBounds_Parses`,
`Parse_UnknownPosition_Throws`, `Parse_MalformedSectionHeader_Throws`, `Parse_IndexExceedsClubSquadSize_Throws`,
`Parse_CommentsAndBlankLines_Ignored`.

## 5.2 FR traceability

| FR | Verifying test / behaviour |
|---|---|
| FR-SQ-001 | `CreateDefault_*` + `ToArray_FromArray_RoundTrips` — one record is the sole source, projected elsewhere |
| FR-SQ-002 | `AttrIdx_Count_MatchesConstant` + `PositionBias_TableLength_MatchesAttributeCount` (31 grouped fields; `AttrIdx` rows cite consumers) |
| FR-SQ-003 | `FromArray_WeakFootRating_Untouched` |
| FR-SQ-004 | `ToArray_FromArray_RoundTrips` + `Generate_AllGeneratedValues_WithinDeclaredBounds` (clamp) |
| FR-SQ-005 | `CreateDefault_AllAttributesMidRange_WeakFootBase` |
| FR-SQ-006 | `ToArray_FromArray_RoundTrips` + `AttrIdx_Count_MatchesConstant` + `FromArray_WrongLength_Throws` |
| FR-SQ-007 | `Generate_AllGeneratedValues_*` (`Enum.IsDefined`) + `Parse_UnknownPosition_Throws`; ordinal used in `PositionBias_*` |
| FR-SQ-008 | `Parse_FullRecord_RoundTrips` + `Generate_SameSeed_*` (every `PlayerRecord` field) |
| FR-SQ-009 | `Parse_NullText_AllDefaultSquad` (Count), `Generate_*` (GetPlayer), `Parse_IndexExceedsClubSquadSize_Throws` (F3) |
| FR-SQ-010 | `Generate_PlayerIds_AreClubScopedAndSequential` + `_NoCollisionAcrossClubs` + `Parse_FullRecord_RoundTrips` (PlayerId) |
| FR-SQ-011 | Every suite consumes the constants by name (no magic literals); `PositionBias_*` assert the table values/tags |
| FR-SQ-012 | `Generate_RngCursor_AdvancesByExactBudgetPerPlayer` (F4) |
| FR-SQ-013 | `RosterGeneratorTests.NewRng` — `RegisterStream(siteId, PlayerDatabase, clubId)` on every `Generate_*` |
| FR-SQ-014 | `PositionBias_{Goalkeeper,Defender,Midfielder,Forward}_ExactValues` + `PositionBias_TableLength_*` |
| FR-SQ-015 | `Generate_AllGeneratedValues_*` (weak foot in `[1,5]`, never boundary-clamped) + `CreateDefault_*` |
| FR-SQ-016 | `Generate_SameSeed_ProducesIdenticalSquad` + `Generate_DifferentSeed_ProducesDifferentSquad` |
| FR-SQ-017 | `Generate_SameSeed_*` (names deterministic) + `Generate_AllGeneratedValues_*` (names resolve within catalogue) |
| FR-SQ-018 | Every `SquadFileLoaderTests.Parse_*_Throws` gate + round-trip |
| FR-SQ-019 | `Parse_FullRecord_RoundTrips` (resulting values, not grammar) |
| FR-SQ-020 | match-engine `MatchEngineSnapshotSchemaTests` (attribute VALUES excluded) + `MatchEngineSquadTests` (config-default digest) |
| FR-SQ-022 | match-engine `MatchEngineSquadTests` (distinct-squad divergence + config-default digest neutrality) |
| FR-SQ-023 | match-engine `PlayerAttributeProjectionTests` (real Crossing / derived KickPower / WeakFoot) |
| FR-SQ-024 | match-engine `MatchEngineSnapshotSchemaTests` (pin 15→16 + `RosterReference_FeedsSnapshotDigest`) + `MatchEngineSquadTests` |
| FR-SQ-025 | match-engine `MatchEngineSnapshotRestoreTests` (distinct-squad round-trip + `ISquadProvider` fail-loud) |
| FR-SQ-026 | match-engine `LineupSelectorTests` + `MatchEngineSquadTests.MisOrderedSquad_SelectsGoalkeeperForGkSlot` |

FR-SQ-021 (club-setup-time, not per-tick, not zero-alloc governed — KD-6) carries no per-tick test by
design: the generator and loader are never called from a 60 Hz path, so §6 (cost placement) is its
governing evidence, not a hot-path budget assertion.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-22 | — | Initial test plan + complete FR-SQ-001..026 traceability over the three landed T0 suites + the T-phase match-engine suites. |
#endregion
