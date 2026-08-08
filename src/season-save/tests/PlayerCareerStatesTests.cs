// File:     src/season-save/tests/PlayerCareerStatesTests.cs
// Created:  2026-08-06
// Modified: 2026-08-07
// Author:   —
// Spec:     Training System #29 §3.1/§3.3/§3.5, FR-TR-004/016/023/025/026, F6/F7;
//           Injuries & Medical #41 §3.1/§3.5, FR-MD-003/004/022/023/025/027, F2/F6/F7;
//           path-to-playable D2/D3 (T2); Code Standards #20
// Purpose:  Locks the #30-side owner of the #29/#41 per-player state: construction from a roster,
//           the codec round trip and the cross-block coherence gate, both day steps and the KD-6 order
//           between them, the occurrence dial in both positions, the FR-MD-023 availability filter and
//           its depleted-squad back-fill, the §3.3 match-entry fatigue projection, and the
//           FR-TR-025 / FR-MD-025 roster reconciliation.

using NUnit.Framework;

using TacticalDirector.InjuriesMedical;
using TacticalDirector.MatchEngine;
using TacticalDirector.PlayerDatabase;
using TacticalDirector.TrainingSystem;

namespace TacticalDirector.SeasonSave.Tests
{
    [TestFixture]
    internal class PlayerCareerStatesTests
    {
        private const ulong WorldSeed = 0x5EED1EA6D0DEC0DEUL;

        private static readonly int[] TwoClubs = { 0, 1 };

        private static CareerTestRoster.MutableSquadProvider TwoClubProvider(int squadSize = 25)
        {
            var provider = new CareerTestRoster.MutableSquadProvider();
            provider.Set(CareerTestRoster.Build(0, squadSize));
            provider.Set(CareerTestRoster.Build(1, squadSize));
            return provider;
        }

        private static PlayerCareerStates Fresh(
            CareerTestRoster.MutableSquadProvider provider, bool occurrence = false) =>
            PlayerCareerStates.ForLeague(provider, TwoClubs, occurrence);

        /// <summary>Advances one full day in #30's pinned slot order — the loop's own sequence.</summary>
        private static void AdvanceOneDay(
            PlayerCareerStates career, uint worldDay, CareerTestRoster.MutableSquadProvider provider)
        {
            career.AdvanceTrainingDay(worldDay, provider, CoachingModifier.Identity);
            career.AdvanceMedicalDay(worldDay, WorldSeed, provider, MedicalModifier.Identity);
        }

        private static int FirstPlayerId(CareerTestRoster.MutableSquadProvider provider, int clubId) =>
            provider.ResolveByClubId(clubId).GetPlayer(0).PlayerId;

        // ── construction ───────────────────────────────────────────────────────────────────

        [Test]
        public void ForLeague_CreatesAStateForEveryPlayerOfEveryClub()
        {
            CareerTestRoster.MutableSquadProvider provider = TwoClubProvider();
            PlayerCareerStates career = Fresh(provider);

            Assert.AreEqual(2, career.ClubCount);
            ClubTrainingStates[] training = career.TrainingBlocks();
            ClubInjuryStates[] medical = career.MedicalBlocks();
            Assert.AreEqual(25, training[0].Count);
            Assert.AreEqual(25, medical[1].Count);
        }

        [Test]
        public void ForLeague_SeedsTheNeverAdvancedSentinel_NotDefault()
        {
            // The day-0 trap both specs name: a defaulted state carries LastAdvancedWorldDay == 0, a
            // legitimate world day, so the very first advance reads as "already advanced" and that
            // player silently never accrues again.
            PlayerCareerStates career = Fresh(TwoClubProvider());

            Assert.AreEqual(
                TrainingSystemConstants.TRAINING_NOT_ADVANCED_SENTINEL,
                career.TrainingBlocks()[0].States[0].LastAdvancedWorldDay);
            Assert.AreEqual(
                InjuriesMedicalConstants.MEDICAL_NOT_ADVANCED_SENTINEL,
                career.MedicalBlocks()[0].States[0].LastAdvancedWorldDay);
            Assert.AreEqual(
                TrainingSystemConstants.ConditionStart,
                career.TrainingBlocks()[0].States[0].Condition);
            Assert.AreEqual(TrainingFocus.Balanced, career.TrainingBlocks()[0].States[0].Focus);
        }

        [Test]
        public void ForLeague_CanonicalizesToAscendingKeys()
        {
            CareerTestRoster.MutableSquadProvider provider = TwoClubProvider();
            // Hand the clubs in descending order; the blocks must still come back ascending, which is
            // what both codecs require of their input.
            PlayerCareerStates career =
                PlayerCareerStates.ForLeague(provider, new[] { 1, 0 }, injuryOccurrenceEnabled: false);

            ClubTrainingStates[] blocks = career.TrainingBlocks();
            Assert.AreEqual(0, blocks[0].ClubId);
            Assert.AreEqual(1, blocks[1].ClubId);
            for (int i = 1; i < blocks[0].Count; i++)
            {
                Assert.Less(blocks[0].PlayerIds[i - 1], blocks[0].PlayerIds[i]);
            }
        }

        [Test]
        public void ForLeague_RefusesADuplicateClub()
        {
            CareerTestRoster.MutableSquadProvider provider = TwoClubProvider();
            Assert.Throws<System.ArgumentException>(() =>
                PlayerCareerStates.ForLeague(provider, new[] { 0, 0 }, injuryOccurrenceEnabled: false));
        }

        [Test]
        public void ForLeague_RefusesAnUnresolvableClub()
        {
            CareerTestRoster.MutableSquadProvider provider = TwoClubProvider();
            Assert.Throws<System.ArgumentException>(() =>
                PlayerCareerStates.ForLeague(provider, new[] { 0, 7 }, injuryOccurrenceEnabled: false));
        }

        // ── persistence ────────────────────────────────────────────────────────────────────

        [Test]
        public void Blocks_RoundTripThroughBothCodecs()
        {
            CareerTestRoster.MutableSquadProvider provider = TwoClubProvider();
            PlayerCareerStates career = Fresh(provider);

            // Both blocks need something to distinguish, or "round-trips" is satisfied by two sets of
            // identical fresh states. A non-default focus varies the #29 side; a directly-installed
            // injury varies the #41 side, which nothing else would with the occurrence dial off.
            Assert.IsTrue(career.TrySetFocus(0, FirstPlayerId(provider, 0), TrainingFocus.Physical));
            var injured = InjuryState.Create();
            injured.Severity = InjurySeverity.Moderate;
            injured.RecoveryRemaining = 12;
            injured.InjuryCount = 3;
            career.SetMedicalState(1, FirstPlayerId(provider, 1), in injured);

            for (uint day = 0; day < 5; day++)
            {
                AdvanceOneDay(career, day, provider);
            }

            byte[] trainingBlob = TrainingSaveCodec.Encode(career.TrainingBlocks());
            byte[] medicalBlob = MedicalSaveCodec.Encode(career.MedicalBlocks());
            byte[] appearanceBlob = AppearanceSaveCodec.Encode(career.AppearanceBlocks());

            PlayerCareerStates restored = PlayerCareerStates.FromBlocks(
                TrainingSaveCodec.Decode(trainingBlob),
                MedicalSaveCodec.Decode(medicalBlob),
                AppearanceSaveCodec.Decode(appearanceBlob),
                injuryOccurrenceEnabled: false);

            Assert.AreEqual(career.ClubCount, restored.ClubCount);
            for (int c = 0; c < career.ClubCount; c++)
            {
                ClubTrainingStates before = career.TrainingBlocks()[c];
                ClubTrainingStates after = restored.TrainingBlocks()[c];
                Assert.AreEqual(before.ClubId, after.ClubId);
                Assert.AreEqual(before.Count, after.Count);

                ClubInjuryStates medBefore = career.MedicalBlocks()[c];
                ClubInjuryStates medAfter = restored.MedicalBlocks()[c];
                Assert.AreEqual(medBefore.ClubId, medAfter.ClubId);
                Assert.AreEqual(medBefore.Count, medAfter.Count);

                for (int i = 0; i < before.Count; i++)
                {
                    Assert.AreEqual(before.PlayerIds[i], after.PlayerIds[i]);
                    Assert.AreEqual(before.States[i].Focus, after.States[i].Focus);
                    Assert.AreEqual(before.States[i].Condition, after.States[i].Condition);
                    Assert.AreEqual(
                        before.States[i].TrainingFatigue, after.States[i].TrainingFatigue);
                    Assert.AreEqual(
                        before.States[i].LastAdvancedWorldDay, after.States[i].LastAdvancedWorldDay);

                    Assert.AreEqual(medBefore.PlayerIds[i], medAfter.PlayerIds[i]);
                    Assert.AreEqual(medBefore.States[i].Severity, medAfter.States[i].Severity);
                    Assert.AreEqual(
                        medBefore.States[i].RecoveryRemaining, medAfter.States[i].RecoveryRemaining);
                    Assert.AreEqual(medBefore.States[i].InjuryCount, medAfter.States[i].InjuryCount);
                    Assert.AreEqual(
                        medBefore.States[i].LastAdvancedWorldDay,
                        medAfter.States[i].LastAdvancedWorldDay);
                }
            }

            Assert.AreEqual(TrainingFocus.Physical, restored.TrainingView(0, FirstPlayerId(provider, 0)).Focus,
                "Precondition on the loop above: the two blocks must differ from a fresh career, or "
                + "every assertion in it holds for two sets of identical Create() states.");
            Assert.IsFalse(restored.IsAvailable(1, FirstPlayerId(provider, 1)),
                "…and the medical side must carry a real injury across, not just an empty block.");
        }

        [Test]
        public void FromBlocks_RefusesADifferentNumberOfClubs()
        {
            PlayerCareerStates career = Fresh(TwoClubProvider());
            ClubInjuryStates[] medical = career.MedicalBlocks();

            Assert.Throws<System.ArgumentException>(() =>
                PlayerCareerStates.FromBlocks(
                    career.TrainingBlocks(), new[] { medical[0] }, career.AppearanceBlocks(),
                    injuryOccurrenceEnabled: false));
        }

        [Test]
        public void FromBlocks_RefusesABlockPairDescribingDifferentClubs()
        {
            // The one coherence rule no codec can own: each sees a single block and is forbidden to
            // read the other's, so "these two blocks describe the same career" can only be checked
            // here — the argument that puts the KD-4 cursor invariant on SeasonSaveManager.Load.
            PlayerCareerStates career = Fresh(TwoClubProvider());
            ClubInjuryStates[] medical = career.MedicalBlocks();
            var swapped = new[] { medical[1], medical[0] };

            Assert.Throws<System.ArgumentException>(() =>
                PlayerCareerStates.FromBlocks(
                    career.TrainingBlocks(), swapped, career.AppearanceBlocks(),
                    injuryOccurrenceEnabled: false));
        }

        [Test]
        public void FromBlocks_RefusesABlockPairDescribingDifferentPlayers()
        {
            CareerTestRoster.MutableSquadProvider provider = TwoClubProvider();
            PlayerCareerStates career = Fresh(provider);
            ClubTrainingStates[] training = career.TrainingBlocks();
            ClubInjuryStates[] medical = career.MedicalBlocks();

            var shortened = new[]
            {
                new ClubInjuryStates(
                    medical[0].ClubId,
                    new[] { medical[0].PlayerIds[0] },
                    new[] { medical[0].States[0] }),
                medical[1],
            };

            Assert.Throws<System.ArgumentException>(() =>
                PlayerCareerStates.FromBlocks(
                    training, shortened, career.AppearanceBlocks(), injuryOccurrenceEnabled: false));
        }

        [Test]
        public void FromBlocks_RefusesUnorderedPlayerIds()
        {
            // Every lookup in PlayerCareerStates is a binary search over these ids. The codecs
            // canonicalize and gate the order, but ClubTrainingStates' constructor does not and this
            // entry point is public — an out-of-order block would make IndexOfPlayer miss a player who
            // IS carried, and SyncToRoster would then read the miss as "new" and overwrite a season of
            // his state with Create(). Silent, total, and indistinguishable from a fresh career.
            var ids = new[] { 5, 3, 9 };
            var training = new[]
            {
                new ClubTrainingStates(
                    0,
                    ids,
                    new[]
                    {
                        TrainingState.Create(TrainingFocus.Balanced),
                        TrainingState.Create(TrainingFocus.Balanced),
                        TrainingState.Create(TrainingFocus.Balanced),
                    }),
            };
            var medical = new[]
            {
                new ClubInjuryStates(
                    0,
                    ids,
                    new[] { InjuryState.Create(), InjuryState.Create(), InjuryState.Create() }),
            };
            var appearance = new[]
            {
                new ClubAppearanceStates(0, ids, new AppearanceState[3]),
            };

            Assert.Throws<System.ArgumentException>(
                () => PlayerCareerStates.FromBlocks(
                    training, medical, appearance, injuryOccurrenceEnabled: false));
        }

        [Test]
        public void FromBlocks_CopiesTheStateArrays_SoTheBlocksAreNotABackDoor()
        {
            // ClubTrainingStates.States is a PUBLIC array field, and the documented restore path hands
            // FromBlocks the very arrays SeasonSaveManager.Load returns inside the public
            // SeasonSaveContents. Borrowing them would make every holder of that struct a second writer
            // of #29/#41 state, straight past TrainingStep.AdvanceTrainingDay and
            // MedicalStep.AdvanceMedicalDay — the only declared writers (FR-TR-004 / FR-TR-023 /
            // FR-MD-003). Keeping TrainingBlocks()/MedicalBlocks() internal closes that on the SAVE
            // route; this is the only other route in, and nothing else in either suite would notice if
            // the copy went away.
            CareerTestRoster.MutableSquadProvider provider = TwoClubProvider();
            PlayerCareerStates source = Fresh(provider);
            ClubTrainingStates[] training = source.TrainingBlocks();
            ClubInjuryStates[] medical = source.MedicalBlocks();
            ClubAppearanceStates[] appearance = source.AppearanceBlocks();

            PlayerCareerStates career = PlayerCareerStates.FromBlocks(
                training, medical, appearance, injuryOccurrenceEnabled: false);
            int playerId = training[0].PlayerIds[0];
            int condition = career.TrainingView(0, playerId).Condition;

            // Exactly what a caller holding a SeasonSaveContents can do, with no internals access.
            training[0].States[0].Condition = condition + 500;
            var injured = InjuryState.Create();
            injured.Severity = InjurySeverity.Serious;
            injured.RecoveryRemaining = 40;
            medical[0].States[0] = injured;

            Assert.AreEqual(condition, career.TrainingView(0, playerId).Condition,
                "FromBlocks must COPY the state arrays: the blocks it is handed are the ones Load "
                + "returns inside SeasonSaveContents, so sharing them lets any holder of that struct "
                + "rewrite a season of conditioning (FR-TR-004 / FR-TR-023).");
            Assert.IsTrue(career.IsAvailable(0, playerId),
                "…and the same on the #41 side: a borrowed array would let an outside holder install "
                + "an injury without MedicalStep ever running (FR-MD-003).");

            // AR pass 3 (M2): the ID array is the worse back door — it holds the binary-search keys,
            // so an aliased write breaks the strictly-ascending precondition FromBlocks just enforced
            // and re-opens the pass-1 silent-overwrite High through the keys instead of the states.
            training[0].PlayerIds[0] = int.MaxValue;
            Assert.AreEqual(condition, career.TrainingView(0, playerId).Condition,
                "FromBlocks must COPY the id arrays too: the original id must still resolve after a "
                + "mutation through the handed-in block.");
        }

        [Test]
        public void ACrossClubDuplicatePlayerId_IsRefusedAtEveryIdEntryPoint()
        {
            // ERR-041-019 (AR pass 3, the High): #41's armed occurrence draw is keyed on
            // (worldSeed, playerId, worldDay) with NO club term, so two clubs carrying one id would
            // draw bit-identical injury luck on every world day forever — and #27 promises id
            // uniqueness only WITHIN a club. Today's generator formula is globally unique by
            // accident of one allocator; this locks the precondition loud at all three id entry
            // points, so the first allocator that breaks it (#42 intake, #31 transfers) fails at
            // construction/sync instead of shipping two players who are always injured together.
            var colliding = new int[PlayerDatabaseConstants.CLUB_SQUAD_SIZE];
            for (int k = 0; k < colliding.Length; k++)
            {
                colliding[k] = k;
            }

            // Club 0, suffix CLUB_SQUAD_SIZE ⇒ id 0×N+N == club 1's first id (1×N+0).
            colliding[colliding.Length - 1] = PlayerDatabaseConstants.CLUB_SQUAD_SIZE;

            var provider = new CareerTestRoster.MutableSquadProvider();
            provider.Set(CareerTestRoster.Build(
                0, PlayerDatabaseConstants.CLUB_SQUAD_SIZE, colliding));
            provider.Set(CareerTestRoster.Build(1, PlayerDatabaseConstants.CLUB_SQUAD_SIZE));

            Assert.Throws<System.ArgumentException>(
                () => PlayerCareerStates.ForLeague(provider, TwoClubs, injuryOccurrenceEnabled: false),
                "construction must refuse a cross-club duplicate id");

            CareerTestRoster.MutableSquadProvider clean = TwoClubProvider();
            PlayerCareerStates career = Fresh(clean);
            clean.Set(CareerTestRoster.Build(
                0, PlayerDatabaseConstants.CLUB_SQUAD_SIZE, colliding));
            Assert.Throws<System.ArgumentException>(
                () => career.SyncToRoster(clean),
                "the roster sync — the entry point a future allocator actually comes through — "
                + "must refuse it too, in the validating half");

            int sharedId = PlayerDatabaseConstants.CLUB_SQUAD_SIZE;
            var training = new[]
            {
                new ClubTrainingStates(0, new[] { sharedId }, new[] { TrainingState.Create(TrainingFocus.Balanced) }),
                new ClubTrainingStates(1, new[] { sharedId }, new[] { TrainingState.Create(TrainingFocus.Balanced) }),
            };
            var medical = new[]
            {
                new ClubInjuryStates(0, new[] { sharedId }, new[] { InjuryState.Create() }),
                new ClubInjuryStates(1, new[] { sharedId }, new[] { InjuryState.Create() }),
            };
            var appearance = new[]
            {
                new ClubAppearanceStates(0, new[] { sharedId }, new AppearanceState[1]),
                new ClubAppearanceStates(1, new[] { sharedId }, new AppearanceState[1]),
            };

            Assert.Throws<System.ArgumentException>(
                () => PlayerCareerStates.FromBlocks(
                    training, medical, appearance, injuryOccurrenceEnabled: false),
                "the restore path must refuse a hand-edited or mispaired save the same way");
        }

        // ── the slot-2 training step ───────────────────────────────────────────────────────

        [Test]
        public void AdvanceTrainingDay_AccruesConditioningForEveryPlayer()
        {
            CareerTestRoster.MutableSquadProvider provider = TwoClubProvider();
            PlayerCareerStates career = Fresh(provider);

            career.AdvanceTrainingDay(0u, provider, CoachingModifier.Identity);

            ClubTrainingStates block = career.TrainingBlocks()[1];
            for (int i = 0; i < block.Count; i++)
            {
                Assert.Greater(block.States[i].Condition, TrainingSystemConstants.ConditionStart,
                    $"Player {block.PlayerIds[i]} must have conditioned on day 0.");
                Assert.AreEqual(0u, block.States[i].LastAdvancedWorldDay);
            }
        }

        [Test]
        public void AdvanceTrainingDay_OnBalanced_NeverAccruesTrainingFatigue()
        {
            // The load-bearing neutrality claim of this whole landing: the default focus's daily load
            // equals the passive daily recovery exactly, so an untouched career projects zero
            // match-entry fatigue and every match stays byte-identical to the unwired engine.
            CareerTestRoster.MutableSquadProvider provider = TwoClubProvider();
            PlayerCareerStates career = Fresh(provider);

            for (uint day = 0; day < 60; day++)
            {
                career.AdvanceTrainingDay(day, provider, CoachingModifier.Identity);
            }

            ClubTrainingStates block = career.TrainingBlocks()[0];
            for (int i = 0; i < block.Count; i++)
            {
                Assert.AreEqual(0, block.States[i].TrainingFatigue,
                    "Balanced must be fatigue-neutral, or the wiring is not behaviour-neutral.");
            }

            Squad squad = provider.ResolveByClubId(0);
            float[] fatigue = career.MatchEntryFatigue(squad);
            for (int i = 0; i < fatigue.Length; i++)
            {
                Assert.AreEqual(0f, fatigue[i], 0f);
            }
        }

        [Test]
        public void AdvanceTrainingDay_IsIdempotentPerWorldDay()
        {
            CareerTestRoster.MutableSquadProvider provider = TwoClubProvider();
            PlayerCareerStates career = Fresh(provider);

            career.AdvanceTrainingDay(0u, provider, CoachingModifier.Identity);
            int once = career.TrainingBlocks()[0].States[0].Condition;
            career.AdvanceTrainingDay(0u, provider, CoachingModifier.Identity);

            Assert.AreEqual(once, career.TrainingBlocks()[0].States[0].Condition,
                "Re-running an advanced day must be a no-op (F6) — a mid-week restore re-runs it.");
        }

        [Test]
        public void AdvanceTrainingDay_WithADayGap_FailsLoud()
        {
            CareerTestRoster.MutableSquadProvider provider = TwoClubProvider();
            PlayerCareerStates career = Fresh(provider);
            career.AdvanceTrainingDay(0u, provider, CoachingModifier.Identity);

            Assert.Throws<System.ArgumentException>(() =>
                career.AdvanceTrainingDay(2u, provider, CoachingModifier.Identity),
                "A gap means the world clock was advanced around the loop; #29 does not batch-replay "
                + "it (F7 / FR-TR-026).");
        }

        [Test]
        public void MatchEntryFatigue_RisesWithAHeavyFocus()
        {
            CareerTestRoster.MutableSquadProvider provider = TwoClubProvider();
            PlayerCareerStates career = Fresh(provider);
            Squad squad = provider.ResolveByClubId(0);

            Assert.IsTrue(
                career.TrySetFocus(0, squad.GetPlayer(0).PlayerId, TrainingFocus.Physical));

            for (uint day = 0; day < 14; day++)
            {
                career.AdvanceTrainingDay(day, provider, CoachingModifier.Identity);
            }

            float[] fatigue = career.MatchEntryFatigue(squad);
            Assert.Greater(fatigue[0], 0f, "A fortnight of Physical work must reach match entry.");
            Assert.AreEqual(0f, fatigue[1], 0f, "A Balanced team-mate must be unaffected.");
        }

        // ── the slot-4 medical step ────────────────────────────────────────────────────────

        [Test]
        public void AdvanceMedicalDay_WithTheDialOff_NeverInjuresAnyone()
        {
            // FR-MD-027's identity — the dial-off half of the contract, still supported and locked
            // after the balance pass ARMED the production posture (the 23%-first-day absurdity this
            // comment used to cite is fixed: the fitted chain reads 0.63%/day for a regen, and the
            // characterization suite in injuries-medical carries the full AFTER table).
            CareerTestRoster.MutableSquadProvider provider = TwoClubProvider();
            PlayerCareerStates career = Fresh(provider, occurrence: false);

            for (uint day = 0; day < 120; day++)
            {
                AdvanceOneDay(career, day, provider);
            }

            ClubInjuryStates block = career.MedicalBlocks()[0];
            for (int i = 0; i < block.Count; i++)
            {
                Assert.AreEqual(InjurySeverity.None, block.States[i].Severity);
                Assert.AreEqual(0, block.States[i].InjuryCount);
            }
        }

        [Test]
        public void AdvanceMedicalDay_WithTheDialOn_DrawsInjuries()
        {
            // The dial's counterpart: "off injures nobody" is satisfiable by a step that is never
            // called, so the armed path has to be shown to reach the draw.
            CareerTestRoster.MutableSquadProvider provider = TwoClubProvider();
            PlayerCareerStates career = Fresh(provider, occurrence: true);

            for (uint day = 0; day < 30; day++)
            {
                AdvanceOneDay(career, day, provider);
            }

            int injuries = 0;
            ClubInjuryStates block = career.MedicalBlocks()[0];
            for (int i = 0; i < block.Count; i++)
            {
                injuries += block.States[i].InjuryCount;
            }

            Assert.Greater(injuries, 0,
                "With the occurrence dial armed the keyed draw must reach players — otherwise the "
                + "wiring runs a step that can never do anything.");
        }

        [Test]
        public void AdvanceMedicalDay_RecoversAnInjuredPlayerAndReleasesHim()
        {
            CareerTestRoster.MutableSquadProvider provider = TwoClubProvider();
            PlayerCareerStates career = Fresh(provider);
            int playerId = FirstPlayerId(provider, 0);

            var injured = InjuryState.Create();
            injured.Severity = InjurySeverity.Minor;
            injured.RecoveryRemaining = 3;
            career.SetMedicalState(0, playerId, in injured);

            Assert.IsFalse(career.IsAvailable(0, playerId));

            for (uint day = 0; day < 3; day++)
            {
                career.AdvanceMedicalDay(day, WorldSeed, provider, MedicalModifier.Identity);
            }

            Assert.IsTrue(career.IsAvailable(0, playerId),
                "Three days of a three-day injury must end it.");
            Assert.AreEqual(InjurySeverity.None, career.MedicalView(0, playerId).Severity);
        }

        [Test]
        public void SlotOrder_MedicalReadsTheSameDaysTrainingState()
        {
            // ERR-030-002 / #41 KD-6: #41's slot sits AFTER #29's so the risk score reads the day's
            // updated conditioning. Nothing throws if the two are transposed — it just prices every
            // injury off a one-day-stale state — so the ordering needs a test that can see it.
            // Conditioning rises daily, so a same-day read has a HIGHER condition (lower shortfall,
            // lower risk) than a stale one; asserted through the training view the medical step reads.
            CareerTestRoster.MutableSquadProvider provider = TwoClubProvider();
            PlayerCareerStates career = Fresh(provider);
            int playerId = FirstPlayerId(provider, 0);

            int before = career.TrainingView(0, playerId).Condition;
            career.AdvanceTrainingDay(0u, provider, CoachingModifier.Identity);
            int afterTraining = career.TrainingView(0, playerId).Condition;
            career.AdvanceMedicalDay(0u, WorldSeed, provider, MedicalModifier.Identity);

            Assert.Greater(afterTraining, before,
                "Slot 2 must have written this day's conditioning before slot 4 reads it.");
            Assert.AreEqual(afterTraining, career.TrainingView(0, playerId).Condition,
                "Slot 4 must not write #29's state (FR-MD-009 — no shared counter).");
        }

        // ── the FR-MD-023 availability filter ──────────────────────────────────────────────

        [Test]
        public void SelectAvailable_WithNobodyInjured_ReturnsTheSameInstance()
        {
            CareerTestRoster.MutableSquadProvider provider = TwoClubProvider();
            PlayerCareerStates career = Fresh(provider);
            Squad squad = provider.ResolveByClubId(0);

            Assert.AreSame(squad, career.SelectAvailable(squad),
                "A fully fit club must resolve through a reference-identical squad, so the filtered "
                + "path is byte-identical to the unfiltered one.");
        }

        [Test]
        public void SelectAvailable_DropsTheInjured()
        {
            CareerTestRoster.MutableSquadProvider provider = TwoClubProvider();
            PlayerCareerStates career = Fresh(provider);
            Squad squad = provider.ResolveByClubId(0);

            int injuredId = squad.GetPlayer(24).PlayerId;   // the best-rated, so selection would use him
            var injured = InjuryState.Create();
            injured.Severity = InjurySeverity.Moderate;
            injured.RecoveryRemaining = 20;
            career.SetMedicalState(0, injuredId, in injured);

            Squad filtered = career.SelectAvailable(squad);

            Assert.AreEqual(squad.Count - 1, filtered.Count);
            for (int i = 0; i < filtered.Count; i++)
            {
                Assert.AreNotEqual(injuredId, filtered.GetPlayer(i).PlayerId);
            }
        }

        [Test]
        public void SelectAvailable_ChangesWhatTheQuickSimWouldRate()
        {
            // The filter has to be observable where #30 actually uses it: the round-resolution model
            // rates a club by the mean of the XI it would field, so injuring the best players must
            // move that number. Without this the filter could drop players nobody was going to pick.
            CareerTestRoster.MutableSquadProvider provider = TwoClubProvider();
            PlayerCareerStates career = Fresh(provider);
            Squad squad = provider.ResolveByClubId(0);

            float before = SquadRating.StartingElevenMean(squad);

            for (int local = 24; local >= 18; local--)
            {
                var injured = InjuryState.Create();
                injured.Severity = InjurySeverity.Serious;
                injured.RecoveryRemaining = 60;
                career.SetMedicalState(0, squad.GetPlayer(local).PlayerId, in injured);
            }

            Squad filtered = career.SelectAvailable(squad);

            Assert.AreEqual(CareerTestRoster.MinimumSquad, filtered.Count);
            Assert.Less(SquadRating.StartingElevenMean(filtered), before,
                "Losing the seven best players must lower the club's rating.");
        }

        [Test]
        public void SelectAvailable_BackfillsTheLeastInjuredRatherThanRefusingToFieldATeam()
        {
            // The depleted-squad policy: a club must always be able to play. The alternative is a
            // career that stops, which is worse than fielding a half-fit player.
            CareerTestRoster.MutableSquadProvider provider = TwoClubProvider();
            PlayerCareerStates career = Fresh(provider);
            Squad squad = provider.ResolveByClubId(0);

            // Injure locals 0..9, leaving 15 fit — three short of the eighteen the formation needs,
            // AND with no goalkeeper among them (local 0 is the only one). Recoveries ascend with the
            // local index, so the press-back-in order is 0, then 1, then 2.
            for (int local = 0; local < 10; local++)
            {
                var injured = InjuryState.Create();
                injured.Severity = InjurySeverity.Serious;
                injured.RecoveryRemaining = 80 + local;   // local 0 is the closest to fit
                career.SetMedicalState(0, squad.GetPlayer(local).PlayerId, in injured);
            }

            Squad filtered = career.SelectAvailable(squad);

            Assert.AreEqual(CareerTestRoster.MinimumSquad, filtered.Count,
                "The press-back-in must stop the moment the club can field a team, never go further.");

            bool[] present = new bool[10];
            for (int i = 0; i < filtered.Count; i++)
            {
                for (int local = 0; local < 10; local++)
                {
                    if (filtered.GetPlayer(i).PlayerId == squad.GetPlayer(local).PlayerId)
                    {
                        present[local] = true;
                    }
                }
            }

            Assert.IsTrue(present[0],
                "The only goalkeeper must be pressed in — a fit outfield eighteen is not a team, and "
                + "selection refuses a position-incomplete squad outright (KD-L3).");
            Assert.IsTrue(present[1] && present[2],
                "The next two least-injured must make up the numbers.");
            for (int local = 3; local < 10; local++)
            {
                Assert.IsFalse(present[local],
                    $"Local {local} has a longer recovery than the three pressed in.");
            }

            Assert.IsTrue(SquadRating.CanFieldStartingEleven(filtered),
                "Whatever the filter returns must be fieldable — that is the loop's exit condition.");
        }

        [Test]
        public void SelectAvailable_WhenTheRosterItselfCannotFieldATeam_FailsLoud()
        {
            var provider = new CareerTestRoster.MutableSquadProvider();
            provider.Set(CareerTestRoster.Build(0, 12));
            PlayerCareerStates career = PlayerCareerStates.ForLeague(
                provider, new[] { 0 }, injuryOccurrenceEnabled: false);
            Squad squad = provider.ResolveByClubId(0);

            // With nobody injured the filter returns the squad untouched and the engine's own gate
            // rejects it later — the pre-T2 behaviour. One injury is what engages the press-back-in
            // loop, which then runs out of players and has to say so.
            var injured = InjuryState.Create();
            injured.Severity = InjurySeverity.Minor;
            injured.RecoveryRemaining = 2;
            career.SetMedicalState(0, squad.GetPlayer(5).PlayerId, in injured);

            Assert.Throws<System.InvalidOperationException>(() => career.SelectAvailable(squad),
                "Selection cannot invent a player; a 12-man club is a roster problem, not a filter one.");
        }

        // ── FR-TR-025 / FR-MD-025 roster reconciliation ────────────────────────────────────

        [Test]
        public void SyncToRoster_OnAnUnchangedRoster_IsANoOp()
        {
            CareerTestRoster.MutableSquadProvider provider = TwoClubProvider();
            PlayerCareerStates career = Fresh(provider);
            career.AdvanceTrainingDay(0u, provider, CoachingModifier.Identity);
            int conditioned = career.TrainingBlocks()[0].States[0].Condition;

            Assert.AreEqual(0, career.SyncToRoster(provider));
            Assert.AreEqual(conditioned, career.TrainingBlocks()[0].States[0].Condition,
                "An unchanged roster must not reset anybody's accrued state.");
        }

        [Test]
        public void SyncToRoster_InsertsAFreshStateForANewPlayer()
        {
            CareerTestRoster.MutableSquadProvider provider = TwoClubProvider(24);
            PlayerCareerStates career = Fresh(provider);
            career.AdvanceTrainingDay(0u, provider, CoachingModifier.Identity);

            provider.Set(CareerTestRoster.Build(0, 25));   // one more player on the roster

            Assert.AreEqual(1, career.SyncToRoster(provider));
            Assert.AreEqual(25, career.TrainingBlocks()[0].Count);

            int newId = provider.ResolveByClubId(0).GetPlayer(24).PlayerId;
            Assert.AreEqual(
                TrainingSystemConstants.TRAINING_NOT_ADVANCED_SENTINEL,
                career.TrainingBlocks()[0].States[24].LastAdvancedWorldDay,
                "A regen must be inserted via Create, never default — the day-0 trap (FR-TR-025).");
            Assert.IsTrue(career.IsAvailable(0, newId));
        }

        [Test]
        public void SyncToRoster_RemovesADepartedPlayerAndKeepsTheRest()
        {
            CareerTestRoster.MutableSquadProvider provider = TwoClubProvider();
            PlayerCareerStates career = Fresh(provider);
            career.AdvanceTrainingDay(0u, provider, CoachingModifier.Identity);

            Squad before = provider.ResolveByClubId(0);
            int survivorId = before.GetPlayer(0).PlayerId;
            int departedId = before.GetPlayer(24).PlayerId;
            int survivorCondition = career.TrainingView(0, survivorId).Condition;

            provider.Set(CareerTestRoster.Build(0, 24));   // the last player retires

            Assert.AreEqual(1, career.SyncToRoster(provider));
            Assert.AreEqual(24, career.MedicalBlocks()[0].Count);
            Assert.AreEqual(survivorCondition, career.TrainingView(0, survivorId).Condition,
                "A departure must not disturb anybody else's state.");
            Assert.Throws<System.ArgumentException>(() => career.TrainingView(0, departedId),
                "The retiree's entry must be gone, not merely orphaned — the block would otherwise "
                + "leak entries unboundedly across seasons (FR-TR-025).");
        }

        [Test]
        public void SyncToRoster_WithAnUnresolvableClub_LeavesTheCareerWhollyUnsynced()
        {
            CareerTestRoster.MutableSquadProvider provider = TwoClubProvider();
            PlayerCareerStates career = Fresh(provider);

            provider.Set(CareerTestRoster.Build(0, 24));   // club 0 would shrink…
            provider.Remove(1);                             // …but club 1 cannot be resolved at all

            Assert.Throws<System.ArgumentException>(() => career.SyncToRoster(provider));
            Assert.AreEqual(25, career.TrainingBlocks()[0].Count,
                "Validate-all-then-write: a provider that fails on the second club must leave the "
                + "first one untouched.");
        }

        [Test]
        public void AdvanceTrainingDay_AgainstAnUnreconciledRoster_FailsLoud()
        {
            // The inverse of the handoff: a state for a player the roster no longer carries is a
            // lifecycle bug, and skipping him quietly would hide whichever boundary forgot to sync.
            CareerTestRoster.MutableSquadProvider provider = TwoClubProvider();
            PlayerCareerStates career = Fresh(provider);
            provider.Set(CareerTestRoster.Build(0, 24));

            Assert.Throws<System.ArgumentException>(() =>
                career.AdvanceTrainingDay(0u, provider, CoachingModifier.Identity));
        }

        [Test]
        public void SyncToRoster_BumpsTheRosterGeneration_EvenWithNoChurn()
        {
            // The generation is what CommitRosterSync refuses a stale plan on, and it has to move on a
            // no-churn sync too: the arrays are replaced either way, so a plan prepared beforehand is
            // stale either way.
            CareerTestRoster.MutableSquadProvider provider = TwoClubProvider();
            PlayerCareerStates career = Fresh(provider);
            int before = career.RosterGeneration;

            Assert.AreEqual(0, career.SyncToRoster(provider), "Precondition: no churn.");
            Assert.AreEqual(before + 1, career.RosterGeneration);
        }

        [Test]
        public void TrySetFocus_SurvivesASeasonBoundary_WhereACachedHandleWouldNot()
        {
            // The command exists instead of a public ScheduleFor for exactly this: a TrainingSchedule
            // binds a club's id and state arrays BY REFERENCE, and SyncToRoster replaces both. A screen
            // that cached the handle across a boundary — the obvious thing to write — would get true
            // back from TrySetFocus and write into a discarded array, losing the manager's instruction
            // with nothing to notice. Resolving fresh per call cannot go stale.
            CareerTestRoster.MutableSquadProvider provider = TwoClubProvider();
            PlayerCareerStates career = Fresh(provider);
            int playerId = FirstPlayerId(provider, 0);

            TrainingSchedule staleHandle = career.ScheduleFor(0);
            career.SyncToRoster(provider);   // the boundary: both arrays replaced

            // The handle now writes into a detached array and says it succeeded — this is the defect,
            // asserted so the reason the command exists is visible rather than merely described.
            Assert.IsTrue(staleHandle.TrySetFocus(playerId, TrainingFocus.Physical));
            Assert.AreEqual(TrainingFocus.Balanced, career.TrainingView(0, playerId).Focus,
                "A handle acquired before the sync must NOT be able to reach the career's state — if "
                + "this ever passes through, the staleness has stopped being detectable at all.");

            // The public surface, over the same boundary, works.
            Assert.IsTrue(career.TrySetFocus(0, playerId, TrainingFocus.Physical));
            Assert.AreEqual(TrainingFocus.Physical, career.TrainingView(0, playerId).Focus);
        }

        [Test]
        public void TrySetFocus_RefusesAnUnknownPlayerAndAnUnknownClub()
        {
            CareerTestRoster.MutableSquadProvider provider = TwoClubProvider();
            PlayerCareerStates career = Fresh(provider);

            Assert.IsFalse(career.TrySetFocus(0, 999_999, TrainingFocus.Physical),
                "A stale id from a screen is refused, not thrown (#29 F2).");
            Assert.Throws<System.ArgumentException>(
                () => career.TrySetFocus(9, FirstPlayerId(provider, 0), TrainingFocus.Physical),
                "An unknown CLUB is a composition bug, not a stale id.");
            Assert.Throws<System.ArgumentOutOfRangeException>(
                () => career.TrySetFocus(0, FirstPlayerId(provider, 0), (TrainingFocus)99),
                "An undefined focus ordinal fails loud rather than clamping (#29 F4 / FR-TR-021).");
        }

        [Test]
        public void CommitRosterSync_WithAStalePlan_FailsLoud()
        {
            // The staged form exists so RollToNextSeason can compute the sync before its one throwing
            // commit and install it after. Installing a plan built against an older roster would
            // resurrect it over the newer one — the very thing the staging is meant to avoid.
            CareerTestRoster.MutableSquadProvider provider = TwoClubProvider();
            PlayerCareerStates career = Fresh(provider);

            PlayerCareerStates.RosterSyncPlan stale = career.PrepareRosterSync(provider);
            career.SyncToRoster(provider);   // a later sync moves the generation on

            Assert.Throws<System.ArgumentException>(() => career.CommitRosterSync(in stale));
        }

        [Test]
        public void CommitRosterSync_WithADefaultPlan_FailsLoud()
        {
            PlayerCareerStates career = Fresh(TwoClubProvider());
            PlayerCareerStates.RosterSyncPlan none = default;

            Assert.Throws<System.ArgumentException>(() => career.CommitRosterSync(in none));
        }

        // ── the observer reads ─────────────────────────────────────────────────────────────

        [Test]
        public void Views_RefuseAnUnknownClubOrPlayer()
        {
            CareerTestRoster.MutableSquadProvider provider = TwoClubProvider();
            PlayerCareerStates career = Fresh(provider);

            Assert.Throws<System.ArgumentException>(() => career.TrainingView(9, 0));
            Assert.Throws<System.ArgumentException>(() => career.MedicalView(0, 999_999));
            Assert.Throws<System.ArgumentException>(() => career.IsAvailable(0, 999_999));
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-08-06 | —      | Initial (#29/#41 T2): construction and the day-0-trap seeding, the |
// |         |            |        | codec round trip and the cross-block coherence gate, both day      |
// |         |            |        | steps with their idempotency / gap / ordering contracts, the        |
// |         |            |        | occurrence dial in BOTH positions, the availability filter with     |
// |         |            |        | its rating effect and back-fill, and the roster reconciliation.     |
// | 1.1     | 2026-08-06 | —      | T2 AR pass 3 (1M + 1L). **M:** + TrySetFocus_SurvivesASeason-      |
// |         |            |        | Boundary_WhereACachedHandleWouldNot and its refusal cases —        |
// |         |            |        | ScheduleFor's handle is now internal and the public focus surface  |
// |         |            |        | is a command that resolves fresh, so the staleness is prevented    |
// |         |            |        | rather than merely detectable; the test asserts the stale handle   |
// |         |            |        | still cannot reach the career, which is the reason the command     |
// |         |            |        | exists. **L:** Blocks_RoundTripThroughBothCodecs verified one      |
// |         |            |        | codec despite its name — no medical field and no Focus /           |
// |         |            |        | TrainingFatigue — and with a fresh career its assertions held for  |
// |         |            |        | two sets of identical Create() states anyway. Both blocks are now  |
// |         |            |        | varied before encoding and every persisted field is compared.      |
// | 1.2     | 2026-08-06 | —      | T2 AR pass 6 (1M). + FromBlocks_CopiesTheStateArrays_SoTheBlocks-  |
// |         |            |        | AreNotABackDoor. Pass 3 changed FromBlocks from borrowing the two  |
// |         |            |        | state arrays to copying them — the single-writer hole on the LOAD  |
// |         |            |        | route, the counterpart of the internal block accessors on the save |
// |         |            |        | route — and nothing locked it: reverting the Array.Copy left every |
// |         |            |        | suite green. Eleven of the twelve pass-3-5 fixes had an enforcing  |
// |         |            |        | test; this was the twelfth, and it guards the same silent-overwrite|
// |         |            |        | class as pass 1's ascending-ids High.                              |
// | 1.3     | 2026-08-07 | —      | Balance pass D2/D4: FromBlocks call sites carry the third          |
// |         |            |        | (appearance) block set and the now-required dial argument; the     |
// |         |            |        | round trip covers the APPR codec; the copy lock keeps its shape.   |
// | 1.4     | 2026-08-08 | —      | Balance-pass AR pass 3: + ACrossClubDuplicatePlayerId lock over    |
// |         |            |        | all three id entry points (ERR-041-019); the FromBlocks copy lock  |
// |         |            |        | extends to the ID arrays (M2); the dial-off comment no longer      |
// |         |            |        | cites the fixed 23%-first-day absurdity as current (L5).           |
#endregion
