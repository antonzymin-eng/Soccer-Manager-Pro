// File:     src/training-system/tests/TrainingSaveCodecTests.cs
// Created:  2026-08-06
// Modified: 2026-08-06
// Author:   —
// Spec:     Training System #29 §4.4 (the sub-blob codec), §5 T-TR-FAIL-001, FR-TR-018/019, F3/F5;
//           ERR-029-004; Code Standards #20
// Purpose:  Unit tests for TrainingSaveCodec — the round-trip field-identity FR-TR-019 demands, the
//           canonical key order that makes the bytes deterministic, and every fail-loud gate (version,
//           bounds, ordering, focus contract, trailing bytes).

using System;

using NUnit.Framework;

using TacticalDirector.DeterministicSim;

namespace TacticalDirector.TrainingSystem.Tests
{
    /// <summary>
    /// Tests for <see cref="TrainingSaveCodec"/>. The central property is FR-TR-019: every
    /// <see cref="TrainingState"/> field round-trips identically, keyed by player id — serialize, don't
    /// regenerate. The rest is the fail-loud surface (F3/F5) and the canonical-order guarantee that
    /// makes two equal state sets produce equal bytes.
    /// </summary>
    [TestFixture]
    public sealed class TrainingSaveCodecTests
    {
        private static TrainingState State(
            TrainingFocus focus, int condition, int fatigue, uint lastDay) =>
            new TrainingState
            {
                Focus = focus,
                Condition = condition,
                TrainingFatigue = fatigue,
                LastAdvancedWorldDay = lastDay,
            };

        private static ClubTrainingStates Club(int clubId, params (int id, TrainingState state)[] players)
        {
            var ids = new int[players.Length];
            var states = new TrainingState[players.Length];
            for (int i = 0; i < players.Length; i++)
            {
                ids[i] = players[i].id;
                states[i] = players[i].state;
            }

            return new ClubTrainingStates(clubId, ids, states);
        }

        private static ClubTrainingStates[] TwoClubs() => new[]
        {
            Club(2,
                (11, State(TrainingFocus.Fitness, 5000, 250, 3u)),
                (10, State(TrainingFocus.Rest, 9999, 0, TrainingSystemConstants.TRAINING_NOT_ADVANCED_SENTINEL))),
            Club(1,
                (7, State(TrainingFocus.Technical, 1, 10000, 0u)),
                (5, State(TrainingFocus.Balanced, 0, 0, uint.MaxValue - 1))),
        };

        // ── Round-trip ──────────────────────────────────────────────────────────────

        [Test]
        public void RoundTrip_EveryFieldSurvives_FRTR019()
        {
            // Each field carries a distinct non-default value, so a field the codec skipped cannot pass
            // by coincidentally matching a default. Note Condition = 0 and 1 at the extremes: the codec
            // must not "helpfully" clamp a persisted value to the [GT] band.
            ClubTrainingStates[] got = TrainingSaveCodec.Decode(TrainingSaveCodec.Encode(TwoClubs()));

            Assert.AreEqual(2, got.Length, "both clubs must survive");
            Assert.AreEqual(1, got[0].ClubId, "clubs come back in ascending club id");
            Assert.AreEqual(2, got[1].ClubId);

            CollectionAssert.AreEqual(new[] { 5, 7 }, got[0].PlayerIds);
            Assert.AreEqual(TrainingFocus.Balanced, got[0].States[0].Focus);
            Assert.AreEqual(0, got[0].States[0].Condition);
            Assert.AreEqual(0, got[0].States[0].TrainingFatigue);
            Assert.AreEqual(uint.MaxValue - 1, got[0].States[0].LastAdvancedWorldDay);

            Assert.AreEqual(TrainingFocus.Technical, got[0].States[1].Focus);
            Assert.AreEqual(1, got[0].States[1].Condition);
            Assert.AreEqual(10000, got[0].States[1].TrainingFatigue);
            Assert.AreEqual(0u, got[0].States[1].LastAdvancedWorldDay);

            CollectionAssert.AreEqual(new[] { 10, 11 }, got[1].PlayerIds);
            Assert.AreEqual(TrainingSystemConstants.TRAINING_NOT_ADVANCED_SENTINEL,
                got[1].States[0].LastAdvancedWorldDay,
                "the never-advanced sentinel must survive as itself — collapsing it to day 0 is the " +
                "day-0 double-accrual trap §2.2 exists to prevent");
            Assert.AreEqual(TrainingFocus.Fitness, got[1].States[1].Focus);
            Assert.AreEqual(5000, got[1].States[1].Condition);
            Assert.AreEqual(250, got[1].States[1].TrainingFatigue);
            Assert.AreEqual(3u, got[1].States[1].LastAdvancedWorldDay);
        }

        [Test]
        public void RoundTrip_EveryDefinedFocusOrdinalSurvives()
        {
            // The focus ordinals are APPEND-only precisely because they are persisted. Every one of them
            // must survive the byte, or a saved player silently changes how he trains.
            var players = new (int, TrainingState)[6];
            for (int i = 0; i < 6; i++)
            {
                players[i] = (i, State((TrainingFocus)i, 100 * i, i, (uint)i));
            }

            ClubTrainingStates[] got = TrainingSaveCodec.Decode(
                TrainingSaveCodec.Encode(new[] { Club(1, players) }));

            for (int i = 0; i < 6; i++)
            {
                Assert.AreEqual((TrainingFocus)i, got[0].States[i].Focus, "focus ordinal " + i);
            }
        }

        [Test]
        public void RoundTrip_EmptySet_IsAWellFormedBlock()
        {
            // The state of the game today: #29 is inert, so every save carries this. It must be a legal
            // block, not a special case the frame has to flag.
            byte[] blob = TrainingSaveCodec.Encode(Array.Empty<ClubTrainingStates>());
            Assert.AreEqual(8, blob.Length, "an empty block is exactly the version + a zero club count");
            Assert.AreEqual(0, TrainingSaveCodec.Decode(blob).Length);
        }

        [Test]
        public void RoundTrip_ClubWithNoPlayers_Survives()
        {
            ClubTrainingStates[] got = TrainingSaveCodec.Decode(
                TrainingSaveCodec.Encode(new[] { Club(4) }));

            Assert.AreEqual(1, got.Length);
            Assert.AreEqual(4, got[0].ClubId);
            Assert.AreEqual(0, got[0].Count);
        }

        // ── Canonical order ─────────────────────────────────────────────────────────

        [Test]
        public void Encode_IsOrderIndependent_SameStateSameBytes()
        {
            // Order is not state (FR-TR-019 keys the block by player id), so two callers holding the
            // same states in different roster orders must produce the same file. Without this, a save
            // written after a squad-list reshuffle would differ byte-for-byte from an identical game.
            var forward = new[]
            {
                Club(1, (5, State(TrainingFocus.Rest, 10, 20, 1u)),
                        (9, State(TrainingFocus.Fitness, 30, 40, 2u))),
                Club(2, (3, State(TrainingFocus.Physical, 50, 60, 3u))),
            };
            var shuffled = new[]
            {
                Club(2, (3, State(TrainingFocus.Physical, 50, 60, 3u))),
                Club(1, (9, State(TrainingFocus.Fitness, 30, 40, 2u)),
                        (5, State(TrainingFocus.Rest, 10, 20, 1u))),
            };

            CollectionAssert.AreEqual(
                TrainingSaveCodec.Encode(forward),
                TrainingSaveCodec.Encode(shuffled),
                "the same state set must encode to the same bytes whatever order it is presented in");
        }

        [Test]
        public void Encode_OfADecodedBlob_IsByteIdentical()
        {
            byte[] once = TrainingSaveCodec.Encode(TwoClubs());
            byte[] twice = TrainingSaveCodec.Encode(TrainingSaveCodec.Decode(once));
            CollectionAssert.AreEqual(once, twice, "decode ∘ encode must be a fixed point");
        }

        [Test]
        public void Encode_DoesNotMutateTheCallersArrays()
        {
            // Canonicalization sorts. It must sort a copy — a codec that reorders the live roster arrays
            // would silently reindex whatever else is holding them (a TrainingSchedule, for one).
            ClubTrainingStates[] clubs = TwoClubs();
            int[] idsBefore = (int[])clubs[0].PlayerIds.Clone();
            int firstClubId = clubs[0].ClubId;

            TrainingSaveCodec.Encode(clubs);

            Assert.AreEqual(firstClubId, clubs[0].ClubId, "the club array must not be reordered");
            CollectionAssert.AreEqual(idsBefore, clubs[0].PlayerIds, "the id array must not be sorted in place");
        }

        [Test]
        public void Encode_DuplicatePlayerId_FailsLoud()
        {
            var clubs = new[]
            {
                Club(1, (5, State(TrainingFocus.Rest, 1, 1, 1u)),
                        (5, State(TrainingFocus.Fitness, 2, 2, 2u))),
            };

            Assert.Throws<ArgumentException>(() => TrainingSaveCodec.Encode(clubs),
                "a duplicate player id has no defined winner and must not be silently deduplicated");
        }

        [Test]
        public void Encode_DuplicateClubId_FailsLoud()
        {
            var clubs = new[] { Club(1, (5, State(TrainingFocus.Rest, 1, 1, 1u))), Club(1) };

            Assert.Throws<ArgumentException>(() => TrainingSaveCodec.Encode(clubs));
        }

        [Test]
        public void Encode_UnboundClub_FailsLoud()
        {
            // default(ClubTrainingStates) reads Count == 0, so it would quietly encode as an empty club.
            // An unbound value in the save set is a caller bug; an empty club has to be asked for.
            Assert.Throws<ArgumentNullException>(
                () => TrainingSaveCodec.Encode(new ClubTrainingStates[1]));
        }

        [Test]
        public void Encode_NullSet_FailsLoud()
        {
            Assert.Throws<ArgumentNullException>(() => TrainingSaveCodec.Encode(null));
        }

        // ── Fail-loud decode gates ──────────────────────────────────────────────────

        [Test]
        public void Decode_Null_FailsLoud()
        {
            Assert.Throws<ArgumentNullException>(() => TrainingSaveCodec.Decode(null));
        }

        [Test]
        public void Decode_WrongFormatVersion_FailsLoud_TTRFAIL001()
        {
            byte[] blob = TrainingSaveCodec.Encode(TwoClubs());
            int o = 0;
            CanonicalSerializer.WriteU32(blob, ref o,
                TrainingSystemConstants.TRAINING_SAVE_FORMAT_VERSION + 1);

            Assert.Throws<InvalidOperationException>(() => TrainingSaveCodec.Decode(blob),
                "a TRAINING_SAVE_FORMAT_VERSION mismatch must fail loud — no migration at Stage 0 (F3).");
        }

        [Test]
        public void Decode_TrailingBytes_FailsLoud()
        {
            byte[] blob = TrainingSaveCodec.Encode(TwoClubs());
            var padded = new byte[blob.Length + 1];
            Array.Copy(blob, padded, blob.Length);

            Assert.Throws<InvalidOperationException>(() => TrainingSaveCodec.Decode(padded),
                "trailing bytes after the declared content must fail loud (F5).");
        }

        [Test]
        public void Decode_Truncated_FailsLoud()
        {
            byte[] blob = TrainingSaveCodec.Encode(TwoClubs());
            var chopped = new byte[blob.Length - 3];
            Array.Copy(blob, chopped, chopped.Length);

            Assert.Throws<InvalidOperationException>(() => TrainingSaveCodec.Decode(chopped),
                "a truncated block must fail loud (F5).");
        }

        [Test]
        public void Decode_OversizeClubCount_FailsLoud_WithoutOverAllocating()
        {
            // The overflow class the ReadCount bound exists for: a crafted count near uint.MaxValue must
            // be refused against the bytes that remain, not turned into a multi-gigabyte allocation.
            byte[] blob = TrainingSaveCodec.Encode(TwoClubs());
            int o = 4;
            CanonicalSerializer.WriteU32(blob, ref o, uint.MaxValue);

            Assert.Throws<InvalidOperationException>(() => TrainingSaveCodec.Decode(blob));
        }

        [Test]
        public void Decode_OversizePlayerCount_FailsLoud()
        {
            byte[] blob = TrainingSaveCodec.Encode(new[] { Club(1, (5, State(TrainingFocus.Rest, 1, 1, 1u))) });
            int o = 4 + 4 + 4;   // version, clubCount, clubId
            CanonicalSerializer.WriteU32(blob, ref o, 0x7FFFFFFFu);

            Assert.Throws<InvalidOperationException>(() => TrainingSaveCodec.Decode(blob));
        }

        [Test]
        public void Decode_UndefinedFocusOrdinal_FailsLoud()
        {
            byte[] blob = TrainingSaveCodec.Encode(new[] { Club(1, (5, State(TrainingFocus.Rest, 1, 1, 1u))) });
            // version(4) + clubCount(4) + clubId(4) + playerCount(4) + playerId(4) = 20 ⇒ the focus byte
            blob[20] = 200;

            Assert.Throws<InvalidOperationException>(() => TrainingSaveCodec.Decode(blob),
                "a focus ordinal outside the enum must fail loud, never clamp to Balanced (F4).");
        }

        [Test]
        public void Decode_NegativeTrainingFatigue_FailsLoud()
        {
            byte[] blob = TrainingSaveCodec.Encode(new[] { Club(1, (5, State(TrainingFocus.Rest, 1, 1, 1u))) });
            // ... + focus(1) + condition(4) = 25 ⇒ the training-fatigue i32
            int o = 25;
            CanonicalSerializer.WriteI32(blob, ref o, -1);

            Assert.Throws<InvalidOperationException>(() => TrainingSaveCodec.Decode(blob),
                "the training-fatigue accumulator's floor of 0 is structural (§2.2), not a [GT] band.");
        }

        [Test]
        public void Decode_NonAscendingPlayerIds_FailsLoud()
        {
            // Encode cannot produce this, so reaching it means the bytes were hand-edited or corrupted.
            // It matters because a repeated key would give one player two states with no defined winner.
            byte[] blob = TrainingSaveCodec.Encode(
                new[]
                {
                    Club(1, (5, State(TrainingFocus.Rest, 1, 1, 1u)),
                            (6, State(TrainingFocus.Fitness, 2, 2, 2u))),
                });

            int o = 4 + 4 + 4 + 4;      // version, clubCount, clubId, playerCount
            CanonicalSerializer.WriteI32(blob, ref o, 9);   // first player id 5 -> 9, now above the second

            Assert.Throws<InvalidOperationException>(() => TrainingSaveCodec.Decode(blob));
        }

        [Test]
        public void Decode_OutOfBandConditionIsAccepted_TuningMustNotBrickSaves()
        {
            // Deliberate non-gate: Condition's bounds are [GT]. If decode enforced them, a designer
            // lowering ConditionMax in config would turn every existing save into an unloadable file —
            // a tuning edit causing data loss. The day step's clamp owns the band; the codec owns the
            // bytes. Only structurally impossible values (negative fatigue) are refused here.
            int beyondCeiling = TrainingSystemConstants.ConditionMax + 5000;
            ClubTrainingStates[] got = TrainingSaveCodec.Decode(TrainingSaveCodec.Encode(
                new[] { Club(1, (5, State(TrainingFocus.Rest, beyondCeiling, 0, 1u))) }));

            Assert.AreEqual(beyondCeiling, got[0].States[0].Condition);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-08-06 | —      | Initial suite (#29 T1): round-trip field identity, focus-ordinal   |
// |         |            |        | survival, canonical-order determinism, the encode-side duplicate   |
// |         |            |        | and unbound guards, and the decode fail-loud gates (F3/F5 + the    |
// |         |            |        | focus and fatigue contracts).                                      |
#endregion
