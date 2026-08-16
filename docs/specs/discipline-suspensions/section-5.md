# Discipline & Suspensions #44 — Section 5: Test Plan

**Created:** July 24, 2026
**Last Updated:** August 16, 2026 (v0.7 — final fixer pass, M10: §5.4's residue paragraph on
`AvailabilityTests.cs` was stale a THIRD time — v0.6's "the real residue is `T-DC-BAN-004`" was true
when written but that file's own v1.4, landed the same day, removed it too; verified directly against
the file's current header, which names neither retired id. Paragraph deleted, replaced with a one-line
dated closure note (nothing left to record); `spec-error-log.md`'s `ERR-044-006` row annotated in
place, same commit. Also swapped the v0.4/v0.5 version-history rows into chronological order
(`tools/recurring-defect-lint.py`'s out-of-order-version check) — content unchanged, position only.)
**Last Updated (prior):** August 15, 2026, later still (v0.6 — reviewed-findings pass: §5.4's residue note on
`AvailabilityTests.cs` was itself stale — that file's own v1.3, landed the same day as this row,
already removed `T-DC-VIEW-001` from its `Spec:` header, so "line 7 still lists it" was wrong at the
moment it was written. The real residue at that header is `T-DC-BAN-004`, withdrawn at ERR-044-003
and un-noted here until now; corrected, recorded, not fixed (outside this pass's owned file set))
**Last Updated (prior):** August 15, 2026, later (v0.5 — **ERR-044-006**, the round-5 High: this table named
**two tests that do not exist** and, in a third row, stated a contract the implementation deliberately
inverts. **T-DC-VIEW-001**'s only test was deleted at C1/C2 AR round 1 as tautological
(`AvailabilityTests.cs` v1.1, L4(a)) and never replaced — now **WITHDRAWN** in place with what
establishes the property instead. **T-DC-INT-001**'s "static/reflection assertion" was never written
(`grep -n "typeof\|GetFields\|Reflection" src/discipline/tests/*.cs` returns nothing) — now
**WITHDRAWN**, with FR-DC-019 recorded as a structural negative no test can assert and FR-DC-020's
missing regression lock recorded as an open follow-up (§9.2) rather than claimed. **T-DC-VIEW-002**
required "a pass-through filter returns an equal (but **distinct-copy**) squad", the opposite of
FR-DC-009's "pass the squad through unchanged" and of the enforced `Is.SameAs` contract on which
FR-DC-018's identity floor rests — **corrected**. Two further rows overstated their tests
(T-DC-FOLD-001's scripted sequence; T-DC-DET-001's filtered-squad half) and are re-cited to what
exists. **§5.6 replaced by an explicit per-FR disposition map**, so G14 is verifiable by grep rather
than asserted; the round-4 pass that certified G14 "against the corrected table" had verified only the
four rows it added. Every surviving row re-verified by reading the named test methods in
`src/discipline/tests/` and `src/season-save/tests/`.)
**Last Updated (prior):** August 15, 2026 (v0.4 — M25, the spec half of #44's adversarial-review round 4
(`open-issues.md`): this was the one section file no back-prop pass had touched since v0.3 — it still
mandated **T-DC-BAN-004 (F5)**, the fail-loud `ERR-044-003` withdrew at the C1/C2 landing (August 13),
while §9's G14 ✅ certified total FR→test traceability against a table that no longer matched §2.3's
current failure-mode set. T-DC-BAN-004 marked WITHDRAWN in place (git tracks history; not deleted, per
this project's own convention for a retired requirement) with a pointer to §2.3's F5 note. Four rows
added for tests that exist in `src/discipline/tests/` and were enforced in production but had no §5
row: **T-DC-FOLD-004** (F6, the bound-`[GT]` commit guard), **T-DC-BAN-006** (ERR-044-003 stage 1's
fielded-eleven exemption, landed August 15), **T-DC-VIEW-003** (FR-DC-009's all-suspended `null`
return, ERR-044-005), **T-DC-SAV-003** (F2's negative-`PlayerId` refusal at both boundary sites).
§5.6 updated to reflect the corrected table; §9's G14 re-checked against it and left unchanged — see
that section's own version history for the verification note.)
**Last Updated (prior):** July 24, 2026 (v0.3 — cross-set AR pass 3; prior v0.2 PASS-1, v0.1 initial)
**Version:** 0.7
**Status:** APPROVED

---

## 5.1 Observer-neutrality & identity (KD-7) — the headline

- **T-DC-NEU-001** — an engine-resolved fixture with the fold tapped is **digest-identical** to
  the same fixture unobserved (the `match-viewer` lock; FR-DC-003) —
  `SeasonLoopDisciplineTests.ARealEngineFixtureFoldsItsCardsOntoPlayerRecordsAndChangesNothingElse`.
  **The neutrality assertion alone is satisfied by a fold that never runs**, so that test pairs it
  with a positive control (`tally.Count > 0` over a real 90-minute fixture) and an attribution check;
  a row asking only for digest-identity would be ERR-030-014's shape one layer up.
- **T-DC-NEU-002** — a season with no threshold-crossing cards is byte-identical to pre-#44
  except #44's own sub-blob (the filter passes every squad through unchanged; FR-DC-018). The
  operative half — reference-identity through the seam, which is what makes a clean career's every
  fixture byte-identical — is
  `SeasonLoopDisciplineTests.WithNothingUnavailable_TheSeamHandsBackTheSAMESquadInstance` (a
  composition rebuilding an equal-but-distinct `Squad` passes every other test here and silently moves
  every digest). *No test compares against a pre-#44 build — none exists in the tree to compare
  against; the property is that identity plus T-DC-NEU-001's neutrality, and this row asserts no more
  than those two establish.*

## 5.2 The fold (KD-2/KD-5)

- **T-DC-FOLD-001 (de-dup)** — a kind-2 counts **one** yellow + **one** dismissal, never a
  yellow-then-red pair; kind 0 is one yellow and no ban; kind 1 is a ban and **no** yellow
  (FR-DC-006). **Locked per kind rather than as one scripted sequence**, which is stronger for this
  property because each case isolates one kind:
  `DisciplineRulesTests.ApplyCard_Kind0_AddsExactlyOneYellow_NoBan`,
  `ApplyCard_Kind1_StraightRed_AddsBanOnly_NoYellow`,
  `ApplyCard_Kind2_SecondYellow_AddsOneYellowAndASecondYellowBan`; through the fold's own attribution
  path, `CardLedgerFoldTests.Card_AttributesToTheOccupantOfRecipient` and the mixed yellow/red
  sequence inside `SameRecordSequence_FoldedTwiceIntoFreshStates_YieldsIdenticalEncodedBytes`.
  *(Corrected at ERR-044-006: v0.1–v0.4 of this row described a single scripted kind-{0, 0, 2, 1}
  test that has never existed in `src/discipline/tests/`. The requirement it traces was covered
  throughout; the named test shape was not.)*
- **T-DC-FOLD-002 (occupancy)** — a card before a substitution attributes to the outgoing
  player; a card after, to the incoming player (occupancy at the card's tick, FR-DC-005); the
  engine's v1.33 slot-reset never leaks into the tally (a subbed-off player's cards persist) —
  `CardLedgerFoldTests.CardBeforeSubstitution_AttributesToOutgoing_CardAfter_AttributesToIncoming`,
  and against the live engine's own occupancy report,
  `SeasonLoopDisciplineTests.PlayerIdsByAgentId_FollowsASubstitution`.
- **T-DC-FOLD-003 (F1/F4)** — a card/sub for an unmapped agent id fails loud; a `CardKind`
  outside `{0,1,2}` fails loud; an unknown Tier A ordinal is **ignored** (FR-DC-004 — the
  contrast is deliberate and both directions are locked) —
  `CardLedgerFoldTests.Card_ForAnAgentIdWithNoPlayerOccupancy_Throws`,
  `Card_ForAnOutOfRangeAgentId_Throws`, `Substitution_WithUnmappedIncoming_Throws`,
  `CardKind3_ThrowsAtObserveTick_NotDeferredToCommit`,
  `UnknownOrdinal_IsIgnored_KnownOrdinalInTheSameBatchStillFolds`.
- **T-DC-FOLD-004 (F6, atomic commit)** — `Commit` validates all four bound `[GT]`s
  (`YellowAccumulationThreshold`, `AccumBanMatches`, `SecondYellowBanMatches`,
  `StraightRedBanMatches`) **before** applying the first buffered card: an invalid yellow
  threshold or a negative ban length refuses the whole call, leaves `DisciplineState` untouched,
  and leaves the fold itself uncommitted so a corrected retry still succeeds (`CardLedgerFoldTests.
  Commit_WithAnInvalidYellowThreshold_RefusesBeforeApplyingAnyCard_AndLeavesTheFoldUncommitted`,
  `Commit_WithAnInvalidBanLength_RefusesBeforeApplyingAnyCard` — §3.1's M13 atomicity property).
  `DisciplineRules.AddYellow`/`ApplyCard` carry the identical guard directly, for a caller that
  never goes through a fold (`RequireYellowThreshold_BelowOne_Throws`,
  `RequireBanLength_Negative_Throws`).

## 5.3 Thresholds, bans & serving (KD-3)

- **T-DC-BAN-001** — the §3.2 worked example: 4 yellows + kind-0 ⇒ `Yellows 0`, ban 1; 4 yellows
  + kind-2 ⇒ `Yellows 0`, ban 2 (stacking); kind-1 ⇒ ban +2, yellows untouched (FR-DC-007) —
  `DisciplineRulesTests.WorkedExample_FourYellows_PlusKind0_EndsAtYellows0Ban1`,
  `WorkedExample_FourYellows_PlusKind2_EndsAtYellows0Ban2_Stacking`,
  `WorkedExample_Kind1_AddsBanPlus2_YellowsUntouched`, plus the residual and stacking rules
  (`ResidualIsKept_NotReset_WhenACrossingLandsAboveTheThreshold`,
  `Bans_StackAdditively_AcrossMultipleSources`).
- **T-DC-BAN-002 (off-by-one)** — a card in fixture N ⇒ the player is filtered from fixture
  N+1's selection ⇒ available again for N+2 after a 1-match ban (the §3.3 ordering lock,
  FR-DC-010/011) — `SeasonLoopDisciplineTests.AOneMatchBan_CostsExactlyTheNextFixtureAndNoMore`,
  with the within-fixture half (a ban earned this fixture is not served by it) at
  `ANewBanEarnedThisFixtureIsNotServedByThisSameFixture` (ERR-030-037) and the filter itself at
  `EnginePath_ExcludesASuspendedManagedPlayerFromTheFieldedEleven`.
- **T-DC-BAN-003 (serving path-independence)** — a ban decrements on the club's quick-sim
  fixtures exactly as on engine-resolved ones (FR-DC-011) —
  `SeasonLoopDisciplineTests.QuickSimPath_MakesASuspensionCostSomething`,
  `EveryClubThatPlayedServesOneMatchOfItsBans`, and the unit-level decrement rules
  (`DisciplineRulesTests.OnClubFixturePlayed_DecrementsOnlyThatClubsPlayers`,
  `OnClubFixturePlayed_NeverGoesBelowZero`).
- **T-DC-BAN-004 (F5) — WITHDRAWN, ERR-044-003 (August 13, 2026).** *Superseded text, kept for
  history rather than deleted (this project's standing convention — git tracks history, IDs are not
  reused): "a filter reducing the squad below the 18 `ConfigureSquads` consumes fails loud."* #44's
  original F5 fail-loud was withdrawn at the C1/C2 landing — #30 §2.3 F9 (approved after this spec)
  settles the identical depleted-squad event by back-filling instead, and #44 implements **no
  viability gate at all** (`src/discipline/Availability.cs`); see §2.3's F5 row and the
  "ERR-044-003 (F5 vs #30 §2.3 F9)" note below it for the full account. No test exists or should
  exist for a fail-loud #44 no longer performs.
- **T-DC-BAN-005 (both squads)** — an **opponent** player banned by accumulation is filtered from
  the engine-resolved fixture against the managed club (both clubs' resolved squads pass the
  resolve→configure seam — FR-DC-010); a managed-squad-only filter fails this test
  (`SeasonLoopDisciplineTests.EnginePath_ExcludesASuspendedOPPONENTPlayerToo`).
- **T-DC-BAN-006 (ERR-044-003 stage 1, the fielded-eleven exemption)** — a banned player fielded
  through #30 §2.3 F9's extremis back-fill does **not** have his ban served by that fixture, while
  an unfielded banned team-mate in the same call still serves one match, asserted in the same call
  so the test cannot pass by the method exempting everybody unconditionally
  (`DisciplineRulesTests.OnClubFixturePlayed_ExemptsAFieldedBannedPlayer_ButStillServesAnUnfieldedTeammate`,
  FR-DC-011 as amended). A fielded player who carries no ban gains no row just for having played
  (`OnClubFixturePlayed_AFieldedPlayerWithNoBan_IsUnaffected_NoRowInvented`); a null
  `fieldedPlayerIds` fails loud, since the exemption is required, not optional
  (`OnClubFixturePlayed_NullFieldedPlayerIds_Throws`); the exemption does not suppress FR-DC-017's
  immediate `(0,0)` drop for a *different* player served in the same call
  (`OnClubFixturePlayed_ExemptionDoesNotBlockTheFRDC017DropForANonFieldedPlayer`).

## 5.4 The view (KD-4)

- **T-DC-VIEW-001 — WITHDRAWN, ERR-044-006 (August 15, 2026).** *Superseded text, kept for history
  rather than deleted (the T-DC-BAN-004 precedent one section up): "exercising every #44 path leaves
  the #27 canonical squads byte-identical (`FilterAvailable` returns a reduced value copy;
  FR-DC-001/009 — the #32 T-SC-VIEW-001 class)."* Its only test —
  `AvailabilityTests.FilterAvailable_LeavesTheSourceSquadUntouched` — was **deleted at the C1/C2
  adversarial review, round 1, as tautological**, and the deletion is recorded in that file's own
  version history (v1.1, L4(a)): *"`Squad` is immutable and exposes no mutator, so no implementation
  could ever fail that assertion; it had no real failure mode."* No replacement was written, in that
  suite or any other, and this row went on naming the test for two more days. **Re-verified
  independently rather than taken on the deletion note's word (August 15, 2026):**
  `src/player-database/Squad.cs` is a `sealed class` over a `private readonly PlayerRecord[]` that
  its constructor **deep-copies** (`Array.Copy`), exposing `ClubId`/`Count` and a `GetPlayer(int)`
  that returns `PlayerRecord` **by value** — a struct copy, not a `ref` — so there is no surface
  through which #44, or any other caller, could write a `Squad` or a record inside one. #44's own
  side matches: `Availability.MarkSuspended` writes only the caller-supplied `bool[]` mask and
  `FilterAvailable` constructs a **new** `Squad`. **The property is real and holds; it is #27's by
  construction, not #44's by behaviour, and is therefore not testable at #44** — which is why the
  row is withdrawn rather than re-tested. It would become testable again only if `Squad` gained a
  mutator or a by-`ref` accessor, at which point it is #27's test to write. See §5.6's disposition
  map for what carries FR-DC-001 now. **Consumers of the retired id:**
  `src/season-save/tests/SeasonLoopDisciplineTests.cs` also listed it in its `Spec:` header while
  implementing no test for it — corrected at that file's v1.8. **Residue chain CLOSED (August 16,
  2026, M10, third and final correction):** `AvailabilityTests.cs`'s `Spec:` header no longer names
  either retired id — `T-DC-VIEW-001` (removed at that file's v1.3) or `T-DC-BAN-004` (removed at
  v1.4, L6) — verified directly against the file's current header, which reads `§5 T-DC-VIEW-002,
  T-DC-BAN-005`. Nothing is left to record; the two prior corrections here (v0.5's "still lists it",
  v0.6's "the real residue is a different id") were each stale at the moment they were written, and
  this note is deliberately terse rather than a third guess at what the header says.
- **T-DC-VIEW-002** — `IsAvailable` is a pure predicate over `DisciplineState` (FR-DC-008): an
  absent entry ⇒ available; yellows below the threshold ⇒ available; `BanMatchesRemaining > 0` ⇒
  unavailable; a null state fails loud (`AvailabilityTests.IsAvailable_AbsentRow_IsTrue`,
  `IsAvailable_YellowsButNoBan_IsTrue`, `IsAvailable_ActiveBan_IsFalse`, `IsAvailable_NullState_Throws`).
  **A pass-through filter — nobody suspended — MUST return the SAME `Squad` instance**, and a
  reduction MUST return a distinct copy preserving `ClubId` and roster order
  (`FilterAvailable_NobodySuspended_ReturnsTheSameInstance`,
  `FilterAvailable_SomeoneSuspended_ReturnsAReducedCopy_PreservingClubIdAndOrder`; FR-DC-009).
  *(**Corrected at ERR-044-006.** v0.1–v0.4 required "a pass-through filter returns an equal (but
  **distinct-copy**) squad" — the opposite of FR-DC-009's "with no active ban it MUST pass the squad
  through unchanged" and of the contract the code actually enforces. This was the worst of the three
  defective rows, because it was not merely absent evidence: an implementer conforming to §5 as
  written would have returned an equal-but-distinct `Squad` on every clean fixture, which passes
  every other row here and silently moves **every** fixture's digest — precisely the failure
  `WithNothingUnavailable_TheSeamHandsBackTheSAMESquadInstance` exists to catch and precisely what
  FR-DC-018's identity floor forbids.)*
- **T-DC-VIEW-003 (FR-DC-009, the all-suspended case, ERR-044-005)** — `FilterAvailable` returns
  `null`, not a zero-player `Squad`, when every player in the squad is suspended (`Squad`'s own
  constructor refuses `players.Length == 0`, so there is no reduced value copy to return) —
  `AvailabilityTests.FilterAvailable_EveryPlayerSuspended_ReturnsNull`. A null `squad`/`state`
  argument fails loud rather than being read as "nothing suspended"
  (`FilterAvailable_NullArguments_Throw`). `FilterAvailable` is FR-DC-009's own surface, not #44's
  production path — #30's composed seam consumes `MarkSuspended`'s removal mask directly and never
  calls this method.

## 5.5 Save, boundary & hygiene (KD-1/KD-6/KD-8)

- **T-DC-SAV-001** — the sub-blob round-trips field-identical (populated tallies + active bans;
  empty at genesis); fail-loud on version/length/trailing/non-ascending-keys/negative values
  (F3, FR-DC-015); **no RNG-state field** (schema-shape, FR-DC-016). Round-trip:
  `DisciplineSaveCodecTests.RoundTrip_PopulatedMultiEntryMultiCompetitionState_IsFieldIdentical`,
  `RoundTrip_EmptyState_IsWellFormedAndComesBackEmpty`. Refusals: the `Decode_*` battery (wrong
  magic, foreign magic, wrong version, **every** truncation length, trailing bytes, non-ascending
  keys, duplicate keys, negative `PlayerId`/`Yellows`/`BanMatchesRemaining`, all-zero row, null
  blob). **The FR-DC-016 half is carried by the exact-layout locks, not by an absence assertion**:
  `Encode_ByteLayout_MagicThenVersionThenLength` pins `blob.Length == 12 + 16 * Count` and
  `Encode_EmptyState_IsExactlyTheTwelveByteHeader` pins the genesis blob at exactly magic + version
  + count — a smuggled RNG-state field of any width fails both, which is a real failure mode rather
  than a restatement of the schema.
- **T-DC-SAV-002 (boundary + canonical minimality)** — `RollToNextSeason`: yellows reset, an
  unserved ban carries and still serves; a `(0,0)` entry is dropped **immediately** wherever it
  arises (mid-season serve-out and boundary alike), so two equivalent runs serialize identical
  bytes (FR-DC-017) — `DisciplineRulesTests.RollToNextSeason_UnservedBanCarries_YellowsResetBut-
  RowSurvives`, `RollToNextSeason_ARowThatBecomesZeroZero_IsDropped`,
  `RollToNextSeason_TwoRowsBothBecomeZeroZero_InOneCall_BothDropped`,
  `OnClubFixturePlayed_DropsARowThatReachesZeroZero_MidSeason`,
  `OnClubFixturePlayed_TwoSameClubRowsBothReachZeroZero_InOneCall_BothDropped`, and the wiring lock
  `SeasonLoopDisciplineTests.TheSeasonRollResetsYellowsAndCarriesUnservedBans`.
- **T-DC-SAV-003 (F2, negative-`PlayerId` refusal)** — a negative `PlayerId` is refused at both
  boundary sites named in §2.3's F2 row: the file boundary
  (`DisciplineSaveCodecTests.Decode_NegativePlayerId_Throws`, an explicit F3 gate ahead of the
  constructor so the exception and message come from the same layer as every other F3 refusal in
  the file) and `DisciplineEntry`'s own constructor, which the codec test exercises transitively
  and which is also reachable directly through `DisciplineRules.MigratePlayerId`'s negative-target
  refusal (`DisciplineRulesTests.MigratePlayerId_NegativeTargetId_ThrowsAndWritesNothing` — the
  migration conflict-refusal is atomic, nothing written on refusal).
- **T-DC-HYG-001** — a re-key migrates tally + unserved bans old→new `PlayerId` verbatim (a
  banned player stays banned through a transfer); retirement drops the entry; a conflicting
  migration target fails loud (FR-DC-013/F2) — `DisciplineRulesTests.MigratePlayerId_MovesTallyAnd-
  UnservedBanVerbatim`, `MigratePlayerId_ToALowerId_MovesEVERYCompetitionsRows`,
  `MigratePlayerId_WithOneConflictingCompetition_WritesNOTHING`,
  `MigratePlayerId_ConflictingTargetRow_Throws`, `MigratePlayerId_ToTheSameId_IsANoOp`,
  `MigratePlayerId_NoRows_IsANoOp`,
  `DropPlayer_RemovesEveryRowForThatPlayer_AcrossCompetitions_LeavesOthersAlone`.
- **T-DC-DET-001** — two-run determinism: the same fixture events produce byte-identical
  `DisciplineState` (FR-DC-021) —
  `CardLedgerFoldTests.SameRecordSequence_FoldedTwiceIntoFreshStates_YieldsIdenticalEncodedBytes`
  (the same tick sequence, twice, into fresh states, compared as encoded bytes) and
  `DisciplineSaveCodecTests.Encode_StatesBuiltThroughDifferentDisciplineRulesCallOrders_Produce-
  IdenticalBytes` (canonical representation — call order cannot be read back out of the bytes).
  *(Narrowed at ERR-044-006: v0.1–v0.4 also claimed "and identical filtered squads". No such test
  exists, and one would be a tautology of the L4(a) class — `IsAvailable`/`MarkSuspended`/
  `FilterAvailable` are pure functions of `(Squad, DisciplineState, competitionId)` over integer
  fields, so equal inputs give equal outputs by construction and no implementation satisfying the
  byte-identity half could fail the squad half. FR-DC-021's filtered-squad clause is therefore
  discharged by the state clause plus purity, and this row no longer claims a lock it never had.)*
- **T-DC-INT-001 — WITHDRAWN, ERR-044-006 (August 15, 2026).** *Superseded text, kept for history
  rather than deleted: "every field integer; no float (static/reflection assertion, FR-DC-020); #44
  registers no RNG stream (FR-DC-019 — the #40 cursor-untouched class)."* **Neither half was ever
  implemented** — `grep -n "typeof\|GetFields\|Reflection" src/discipline/tests/*.cs` returns
  nothing, and no equivalent assertion exists under another name. The two halves are withdrawn for
  *different* reasons, which is why they should never have shared a row:
  - **FR-DC-019 (no RNG stream / domain tag / `SubsystemOrdinals` entry) is a structural negative
    with nothing to assert against.** There is no registry a test could interrogate for an absence:
    the property is that `deterministic-sim`'s #16 §3.4 tag catalogue and `SubsystemOrdinals` carry
    **no #44 row**, which is a fact about *another* assembly's source, and G3 already accepts it on
    exactly those terms ("a positive property; no #16 row"). Established by construction and
    grep-verifiable at any time: `src/discipline/**` contains zero occurrences of `DOMAIN_TAG`,
    `SubsystemOrdinal`, `SplitMix` or any RNG-service type (verified August 15, 2026), and
    `discipline.asmdef` references only `EventSystem`, `PlayerDatabase`, `DeterministicSim` and
    `ProjectConstants` — the `DeterministicSim` reference being for `CanonicalSerializer`'s byte
    helpers in `DisciplineSaveCodec` alone, its single `using` in the assembly. A test asserting
    this would be the L4(a) class.
  - **FR-DC-020 (integer posture) is NOT established by any test, and the honest statement is that
    it rests on audit.** What holds today: **`src/discipline/**` contains no `float`, `double` or
    `decimal` declaration of any kind** — `grep -nE "\b(float|double|decimal)\b" src/discipline/*.cs`
    returns five lines, **all five of them English prose** in doc comments and one exception message
    ("would double its cards", "not double-counting", and `DisciplineConstants.cs`'s own two
    statements of this very posture); not one is a type (verified August 15, 2026) — and the save
    format is integer-only by layout (T-DC-SAV-001's `12 + 16 * Count` lock over four `i32` fields).
    **That is an audit, not enforcement**: nothing in the compiler or the suite would stop a future
    field being declared `float`. A reflection assertion over the assembly's fields *would* have a
    real failure mode — unlike the two tautologies withdrawn above, it guards against a change that
    can actually be made — so it is worth writing, and it is recorded as an open follow-up in
    **§9.2** rather than claimed here. This row stays withdrawn until that test exists; if it is
    written, it lands as a **new** id, not by reviving this one.

## 5.6 Requirement traceability

**Replaced at ERR-044-006 (August 15, 2026) by the explicit per-FR map below.** The prose this
section carried through v0.4 — *"Every FR-DC-001..022 maps to a T-DC-* test **or** a recorded §7
deferral"* — was a summary claim with nothing behind it that a reader could check, and it was false
on at least five requirements at the moment §9's G14 last certified it: FR-DC-001, FR-DC-019 and
FR-DC-020 traced only to rows whose tests do not exist, and FR-DC-002 and FR-DC-022 were named by no
row at all. A traceability claim a reader cannot re-derive is not traceability, so it is stated as a
map with one row per requirement.

**Three dispositions, not two.** *Test* — a named test method that fails if the requirement is
violated. *Construction* — a property of the code's shape that no runtime assertion could fail
(immutability, an absent reference, a structural negative); each names what establishes it and is
grep-checkable. *Deferral* — recorded in §7. The third column is not a softening of the gate: a
requirement like FR-DC-019 ("registers **no** RNG stream") has no observable to assert, and G3 has
accepted it on structural grounds since approval. Where a requirement is carried by construction
*and* a test would have a real failure mode, that is a gap and is named as one — there is exactly
one such case, **FR-DC-020**, tracked as a §9.2 follow-up.

| FR | Disposition | Established by |
|---|---|---|
| FR-DC-001 | Construction + Test | #27 immutability (`Squad` sealed, deep-copying ctor, `GetPlayer` by value) and #44's write surface being the caller's mask / a new `Squad`; the engine half by T-DC-NEU-001 over a real fixture. *T-DC-VIEW-001 withdrawn — see §5.4.* |
| FR-DC-002 | Construction | `IDisciplineTickLedgerTap` is the assembly's only read surface; no `SerializeLedger` parse, no post-match slot read, no new subscription anywhere in `src/discipline/**`. G4's verified-source evidence (§1 KD-2, §3.1, XC-044-001/002); exercised live by T-DC-NEU-001. |
| FR-DC-003 | Test | T-DC-NEU-001 (with its positive control). |
| FR-DC-004 | Test | T-DC-FOLD-003. |
| FR-DC-005 | Test | T-DC-FOLD-002. |
| FR-DC-006 | Test | T-DC-FOLD-001. |
| FR-DC-007 | Test | T-DC-BAN-001. |
| FR-DC-008 | Test | T-DC-VIEW-002 (the four `IsAvailable_*` cases). |
| FR-DC-009 | Test | T-DC-VIEW-002 (pass-through identity + reduced copy), T-DC-VIEW-003 (the all-suspended `null`). |
| FR-DC-010 | Test | T-DC-BAN-002, T-DC-BAN-005 (both clubs, both paths). |
| FR-DC-011 | Test | T-DC-BAN-002/003/006. |
| FR-DC-012 | Test | `DisciplineRulesTests.SamePlayer_DifferentCompetitions_TalliesAreIndependent_BothSurvive`, `MigratePlayerId_ToALowerId_MovesEVERYCompetitionsRows`; the key's canonical ordering by `DisciplineStateTests.Upsert_MaintainsCanonicalAscendingOrder_RegardlessOfInsertOrder` and `DisciplineSaveCodecTests.Decode_NonAscendingKeys_Throws`. |
| FR-DC-013 | Test | T-DC-HYG-001. |
| FR-DC-014 | Test | The sub-blob's composition into #30's frame: `SeasonSaveManagerTests.Restore_CarriesTheDisciplineTallyIntoTheResumedLoop`, `Save_AnUnwiredDiscipline_OverAPopulatedTally_IsRefused`, `Save_ThePublicLongForm_DrivingNoDiscipline_CannotEmptyAPopulatedTally` (`src/season-save/tests/`). |
| FR-DC-015 | Test | T-DC-SAV-001's `Decode_*` refusal battery; T-DC-SAV-003 for F2's negative-`PlayerId` half. |
| FR-DC-016 | Test | T-DC-SAV-001's exact-layout locks (`12 + 16 * Count`; the 12-byte genesis blob). |
| FR-DC-017 | Test | T-DC-SAV-002. |
| FR-DC-018 | Test | T-DC-NEU-002's reference-identity lock, with T-DC-NEU-001's neutrality. |
| FR-DC-019 | **Construction** | No RNG stream, domain tag or `SubsystemOrdinals` entry to assert against — zero `DOMAIN_TAG`/`SubsystemOrdinal`/`SplitMix`/RNG-service occurrences in `src/discipline/**`; `discipline.asmdef` references `EventSystem`, `PlayerDatabase`, `DeterministicSim` (for `CanonicalSerializer` only), `ProjectConstants`. See §5.5's withdrawal note and G3. |
| FR-DC-020 | **Construction (audit) — GAP** | No `float`/`double`/`decimal` declared in `src/discipline/**`; integer-only save layout. **No test enforces this and one would have a real failure mode** — recorded as a §9.2 follow-up, not claimed as coverage. |
| FR-DC-021 | Test + Construction | T-DC-DET-001 for the state half; the filtered-squad half follows from it by the purity of `IsAvailable`/`MarkSuspended`/`FilterAvailable` over integer fields. |
| FR-DC-022 | Construction | `discipline.asmdef` declares no #38 (`ui-framework`), #43 or #46 reference and the assembly defines no interface facing them; the only interface it owns is `IDisciplineTickLedgerTap`, whose producer and consumer both exist (FR-CS-048/049). |

**Failure modes.** §2.3's rows beyond F1..F4 are covered too: **F6** by T-DC-FOLD-004, **F2** by
T-DC-SAV-003, and **F5** by the T-DC-BAN-004 withdrawal record (ERR-044-003 — a fail-loud #44 no
longer performs; #30 §2.3 F9 owns the event).

**Deferrals (§7.2), unchanged and out of scope for the map above:** the #30-owned quick-sim card
synthesis (T3), #43 competition partitions over the FR-DC-012 key, and the #31 FR-TX-022 hygiene-hook
wiring — each locked at its minimal boundary today.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-24 | — | Initial §5 (observer-neutrality/identity, fold, thresholds/serving, view, save/boundary/hygiene, traceability), promoted from design supplement v0.3. Status IN REVIEW. |
| 0.2 | 2026-07-24 | — | Section-file AR PASS-1 (M follow-through): T-DC-SAV-002 extended to lock the immediate `(0,0)` drop + identical-bytes property. |
| 0.3 | 2026-07-24 | — | Cross-set AR pass 3 (M follow-through): new **T-DC-BAN-005** locks the both-squads filter coverage (a banned opponent excluded from the engine-resolved fixture — the case the managed-club-only tests never exercised). |
| 0.4 | 2026-08-15 | — | **M25** (#44 adversarial-review round 4, `open-issues.md`): this section had not been touched since v0.3 (July 24) despite five back-prop passes over the C1/C2 landing touching every other #44 section file. **T-DC-BAN-004** (F5's fail-loud) marked WITHDRAWN in place, pointing at §2.3's F5/ERR-044-003 note, rather than deleted or silently left mandating a test #44 no longer performs — §9's G14 ✅ certified total FR→test traceability against a table this left stale. New **T-DC-FOLD-004** (F6, the bound-`[GT]` atomic-commit guard, §5.2), **T-DC-BAN-006** (ERR-044-003 stage 1's fielded-eleven exemption, §5.3), **T-DC-VIEW-003** (FR-DC-009's all-suspended `null` return, ERR-044-005, §5.4), **T-DC-SAV-003** (F2's negative-`PlayerId` refusal at both boundary sites, §5.5) — all four cite tests already present and green in `src/discipline/tests/`, verified by reading each named test method directly rather than by inference from the surrounding code. §9 checked against the corrected table (see that section's own version history) and its G14 ✅ left standing — the claim is true of this table, it was the table that was stale. **⚠️ CORRECTED at v0.5 (ERR-044-006), annotated rather than rewritten: that last sentence was FALSE.** This pass verified the four rows it *added* and left every pre-existing row unchecked, then re-certified G14 against "the corrected table" as though the whole table had been verified — three rows (T-DC-VIEW-001, T-DC-INT-001, T-DC-VIEW-002) were defective at the moment of that certification, and two requirements (FR-DC-002, FR-DC-022) appeared in no row at all. A partial verification presented as a complete one is the same class of defect this row was filed to fix, one pass later. |
| 0.5 | 2026-08-15 | — | **ERR-044-006** (#44 adversarial-review round 5, High): this table named **two tests that do not exist** and, in a third row, mandated the **opposite** of the contract the code enforces — and §9's G6/G13/G14 had ratified gates on all three. **T-DC-VIEW-001 WITHDRAWN** (its only test was deleted at C1/C2 AR round 1 as tautological — `AvailabilityTests.cs` v1.1, L4(a) — and never replaced; the property is #27's by construction, independently re-verified here against `Squad.cs` rather than taken from the deletion note). **T-DC-INT-001 WITHDRAWN**, both halves, for different reasons: FR-DC-019 is a structural negative with no observable to assert (G3's own posture), while FR-DC-020's integer posture rests on **audit, not enforcement** — a reflection lock would have a real failure mode and is recorded as a §9.2 follow-up rather than written into a row as if it existed. **T-DC-VIEW-002 CORRECTED**: it required a pass-through filter to return "an equal (but **distinct-copy**) squad", contradicting FR-DC-009, FR-DC-018's identity floor and the enforced `Is.SameAs` contract — a conformant implementation of the old text would have moved every clean fixture's digest. **T-DC-FOLD-001 and T-DC-DET-001 re-cited to what exists** (the scripted kind-{0,0,2,1} sequence has never existed; T-DC-DET-001's "identical filtered squads" half has no test and would be a tautology). **§5.6 replaced** by a per-FR disposition map (Test / Construction / Deferral) so G14 is re-derivable by grep — the old prose was false on five requirements, two of which (FR-DC-002, FR-DC-022) no row had ever named. Every surviving row was verified by reading the named test methods in `src/discipline/tests/` and `src/season-save/tests/`; test-method citations added throughout so the next verification is a grep rather than a re-reading. See §9's version history for the G6/G13/G14 re-derivation. |
| 0.6 | 2026-08-15 | — | **Reviewed-findings pass.** §5.4's `AvailabilityTests.cs` residue note (v0.5) said the file's `Spec:` header "line 7 still lists" `T-DC-VIEW-001` — checked directly against the file: its own v1.3, landed the same day, had already removed the id, so the note was stale at the moment v0.5 wrote it. The real residue at that same header (line 11) is `T-DC-BAN-004`, withdrawn at ERR-044-003 (August 13, 2026), of which §5.3 says "No test exists or should exist" — recorded here instead, out of this pass's owned file set (`src/discipline/`). No new ERR id; a correction of v0.5's own claim. |
| 0.7 | 2026-08-16 | — | **Final fixer pass, M10 (third and final correction) + a version-row ordering fix.** §5.4's residue paragraph was itself stale, a third time: v0.6's "the real residue is `T-DC-BAN-004`, recorded not fixed" was true when written but `AvailabilityTests.cs` v1.4 (L6, the SAME day, August 15) removed that id too — verified directly against the file's current header (`§5 T-DC-VIEW-002, T-DC-BAN-005`, no retired id of either kind). The residue paragraph is deleted and replaced with a one-line dated closure note per this file's own annotate-in-place convention; nothing is left to record. `spec-error-log.md`'s `ERR-044-006` row (which still carries the original, also-false, "line 7 / T-DC-VIEW-001" claim) is annotated in place in the same commit — see that entry. **Also fixed:** this table's v0.5 row was published ahead of v0.4 (`tools/recurring-defect-lint.py`'s out-of-order-version check) — the two rows are swapped above into chronological order; no row's content changed, only its position. |
#endregion
