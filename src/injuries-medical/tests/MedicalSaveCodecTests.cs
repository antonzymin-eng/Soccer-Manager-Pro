// File:     src/injuries-medical/tests/MedicalSaveCodecTests.cs
// Created:  2026-08-06
// Modified: 2026-08-06
// Author:   —
// Spec:     Injuries & Medical #41 §4.4 (the sub-blob codec), §5, FR-MD-017/018/019, F1/F3/F4/F5;
//           ERR-041-008; Code Standards #20
// Purpose:  Unit tests for MedicalSaveCodec — the round-trip field-identity FR-MD-018 demands, the
//           canonical key order that makes the bytes deterministic, and every fail-loud gate (version,
//           bounds, ordering, severity contract, the F1 coherence rule on BOTH sides, trailing bytes).

using System;

using NUnit.Framework;

using TacticalDirector.DeterministicSim;

namespace TacticalDirector.InjuriesMedical.Tests
{
    /// <summary>
    /// Tests for <see cref="MedicalSaveCodec"/>. The central property is FR-MD-018: every
    /// <see cref="InjuryState"/> field round-trips identically, keyed by player id — serialize, don't
    /// regenerate. The rest is the fail-loud surface (F1/F3/F4/F5) and the canonical-order guarantee
    /// that makes two equal state sets produce equal bytes.
    /// </summary>
    [TestFixture]
    public sealed class MedicalSaveCodecTests
    {
        private static InjuryState State(InjurySeverity severity, int recovery, int count, uint lastDay) =>
            new InjuryState
            {
                Severity = severity,
                RecoveryRemaining = recovery,
                InjuryCount = count,
                LastAdvancedWorldDay = lastDay,
            };

        private static ClubInjuryStates Club(int clubId, params (int id, InjuryState state)[] players)
        {
            var ids = new int[players.Length];
            var states = new InjuryState[players.Length];
            for (int i = 0; i < players.Length; i++)
            {
                ids[i] = players[i].id;
                states[i] = players[i].state;
            }

            return new ClubInjuryStates(clubId, ids, states);
        }

        private static ClubInjuryStates[] TwoClubs() => new[]
        {
            Club(2,
                (11, State(InjurySeverity.Serious, 180, 9, 42u)),
                (10, State(InjurySeverity.None, 0, 0, InjuriesMedicalConstants.MEDICAL_NOT_ADVANCED_SENTINEL))),
            Club(1,
                (7, State(InjurySeverity.Minor, 3, 1, 0u)),
                (5, State(InjurySeverity.Moderate, 28, 4, uint.MaxValue - 1))),
        };

        // ── Round-trip ──────────────────────────────────────────────────────────────

        [Test]
        public void RoundTrip_EveryFieldSurvives_FRMD018()
        {
            ClubInjuryStates[] got = MedicalSaveCodec.Decode(MedicalSaveCodec.Encode(TwoClubs()));

            Assert.AreEqual(2, got.Length, "both clubs must survive");
            Assert.AreEqual(1, got[0].ClubId, "clubs come back in ascending club id");
            Assert.AreEqual(2, got[1].ClubId);

            CollectionAssert.AreEqual(new[] { 5, 7 }, got[0].PlayerIds);
            Assert.AreEqual(InjurySeverity.Moderate, got[0].States[0].Severity);
            Assert.AreEqual(28, got[0].States[0].RecoveryRemaining);
            Assert.AreEqual(4, got[0].States[0].InjuryCount);
            Assert.AreEqual(uint.MaxValue - 1, got[0].States[0].LastAdvancedWorldDay);

            Assert.AreEqual(InjurySeverity.Minor, got[0].States[1].Severity);
            Assert.AreEqual(3, got[0].States[1].RecoveryRemaining);
            Assert.AreEqual(1, got[0].States[1].InjuryCount);
            Assert.AreEqual(0u, got[0].States[1].LastAdvancedWorldDay);

            CollectionAssert.AreEqual(new[] { 10, 11 }, got[1].PlayerIds);
            Assert.AreEqual(InjuriesMedicalConstants.MEDICAL_NOT_ADVANCED_SENTINEL,
                got[1].States[0].LastAdvancedWorldDay,
                "the never-advanced sentinel must survive as itself — collapsing it to day 0 would " +
                "make that player's first advance read as already-done and he would never be " +
                "evaluated for injury again (§2.2)");
            Assert.AreEqual(InjurySeverity.Serious, got[1].States[1].Severity);
            Assert.AreEqual(180, got[1].States[1].RecoveryRemaining);
            Assert.AreEqual(9, got[1].States[1].InjuryCount);
            Assert.AreEqual(42u, got[1].States[1].LastAdvancedWorldDay);
        }

        [Test]
        public void RoundTrip_EveryDefinedSeverityTierSurvives()
        {
            // The severity ordinals are APPEND-only because they are persisted as a byte — a renumbering
            // would silently re-tier every saved injury, so each must be proven to survive the byte.
            var players = new (int, InjuryState)[]
            {
                (0, State(InjurySeverity.None, 0, 0, 1u)),
                (1, State(InjurySeverity.Minor, 2, 1, 1u)),
                (2, State(InjurySeverity.Moderate, 20, 2, 1u)),
                (3, State(InjurySeverity.Serious, 120, 3, 1u)),
            };

            ClubInjuryStates[] got = MedicalSaveCodec.Decode(
                MedicalSaveCodec.Encode(new[] { Club(1, players) }));

            Assert.AreEqual(InjurySeverity.None, got[0].States[0].Severity);
            Assert.AreEqual(InjurySeverity.Minor, got[0].States[1].Severity);
            Assert.AreEqual(InjurySeverity.Moderate, got[0].States[2].Severity);
            Assert.AreEqual(InjurySeverity.Serious, got[0].States[3].Severity);
        }

        [Test]
        public void RoundTrip_EmptySet_IsAWellFormedBlock()
        {
            byte[] blob = MedicalSaveCodec.Encode(Array.Empty<ClubInjuryStates>());
            Assert.AreEqual(8, blob.Length, "an empty block is exactly the version + a zero club count");
            Assert.AreEqual(0, MedicalSaveCodec.Decode(blob).Length);
        }

        [Test]
        public void NoRngCursorIsWritten_TheBlockIsExactlyTheStates_FRMD007()
        {
            // KD-1's whole point: the occurrence draw is keyed, so there is no cursor to persist. If a
            // future edit ever adds one to the block, this size arithmetic is what catches it — and the
            // question "did we just make the draw position-dependent?" gets asked before it ships.
            const int BytesPerPlayer = 4 + 1 + 4 + 4 + 4;
            byte[] blob = MedicalSaveCodec.Encode(TwoClubs());

            Assert.AreEqual(4 + 4 + (2 * (4 + 4)) + (4 * BytesPerPlayer), blob.Length,
                "the medical block is the version, the club count, two club headers and four player " +
                "records — nothing else (FR-MD-007).");
        }

        // ── Canonical order ─────────────────────────────────────────────────────────

        [Test]
        public void Encode_IsOrderIndependent_SameStateSameBytes()
        {
            var forward = new[]
            {
                Club(1, (5, State(InjurySeverity.Minor, 2, 1, 1u)),
                        (9, State(InjurySeverity.None, 0, 0, 2u))),
                Club(2, (3, State(InjurySeverity.Serious, 90, 5, 3u))),
            };
            var shuffled = new[]
            {
                Club(2, (3, State(InjurySeverity.Serious, 90, 5, 3u))),
                Club(1, (9, State(InjurySeverity.None, 0, 0, 2u)),
                        (5, State(InjurySeverity.Minor, 2, 1, 1u))),
            };

            CollectionAssert.AreEqual(
                MedicalSaveCodec.Encode(forward),
                MedicalSaveCodec.Encode(shuffled),
                "the same state set must encode to the same bytes whatever order it is presented in");
        }

        [Test]
        public void Encode_OfADecodedBlob_IsByteIdentical()
        {
            byte[] once = MedicalSaveCodec.Encode(TwoClubs());
            byte[] twice = MedicalSaveCodec.Encode(MedicalSaveCodec.Decode(once));
            CollectionAssert.AreEqual(once, twice, "decode ∘ encode must be a fixed point");
        }

        [Test]
        public void Encode_DoesNotMutateTheCallersArrays()
        {
            ClubInjuryStates[] clubs = TwoClubs();
            int[] idsBefore = (int[])clubs[0].PlayerIds.Clone();
            int firstClubId = clubs[0].ClubId;

            MedicalSaveCodec.Encode(clubs);

            Assert.AreEqual(firstClubId, clubs[0].ClubId, "the club array must not be reordered");
            CollectionAssert.AreEqual(idsBefore, clubs[0].PlayerIds, "the id array must not be sorted in place");
        }

        [Test]
        public void Encode_DuplicatePlayerId_FailsLoud()
        {
            var clubs = new[]
            {
                Club(1, (5, State(InjurySeverity.None, 0, 0, 1u)),
                        (5, State(InjurySeverity.Minor, 2, 1, 1u))),
            };

            Assert.Throws<ArgumentException>(() => MedicalSaveCodec.Encode(clubs));
        }

        [Test]
        public void Encode_DuplicateClubId_FailsLoud()
        {
            Assert.Throws<ArgumentException>(
                () => MedicalSaveCodec.Encode(new[] { Club(3), Club(3) }));
        }

        [Test]
        public void Encode_UnboundClub_FailsLoud()
        {
            Assert.Throws<ArgumentNullException>(
                () => MedicalSaveCodec.Encode(new ClubInjuryStates[1]));
        }

        [Test]
        public void Encode_NullSet_FailsLoud()
        {
            Assert.Throws<ArgumentNullException>(() => MedicalSaveCodec.Encode(null));
        }

        // ── The F1 coherence rule, on the way OUT as well as in ─────────────────────

        [Test]
        public void Encode_IncoherentState_FailsLoud_F1()
        {
            // A codec that validates only on decode will happily write a file no load of it can accept,
            // and the failure then surfaces a session later, far from the bug that produced it.
            Assert.Throws<InvalidOperationException>(
                () => MedicalSaveCodec.Encode(new[] { Club(1, (5, State(InjurySeverity.None, 7, 0, 1u))) }),
                "healthy with recovery days left is incoherent (F1).");

            Assert.Throws<InvalidOperationException>(
                () => MedicalSaveCodec.Encode(new[] { Club(1, (5, State(InjurySeverity.Serious, 0, 1, 1u))) }),
                "injured with no recovery days left is incoherent (F1).");
        }

        [Test]
        public void Encode_UndefinedSeverity_FailsLoud_F4()
        {
            Assert.Throws<InvalidOperationException>(
                () => MedicalSaveCodec.Encode(
                    new[] { Club(1, (5, State((InjurySeverity)9, 5, 1, 1u))) }));
        }

        [Test]
        public void Decode_IncoherentState_FailsLoud_F1()
        {
            byte[] blob = MedicalSaveCodec.Encode(
                new[] { Club(1, (5, State(InjurySeverity.Minor, 3, 1, 1u))) });
            // version(4) + clubCount(4) + clubId(4) + playerCount(4) + playerId(4) = 20 ⇒ severity byte
            blob[20] = (byte)InjurySeverity.None;   // healthy, but 3 recovery days still recorded

            Assert.Throws<InvalidOperationException>(() => MedicalSaveCodec.Decode(blob),
                "an incoherent (severity, recovery) pair must fail loud, never be repaired (F1).");
        }

        // ── Fail-loud decode gates ──────────────────────────────────────────────────

        [Test]
        public void Decode_Null_FailsLoud()
        {
            Assert.Throws<ArgumentNullException>(() => MedicalSaveCodec.Decode(null));
        }

        [Test]
        public void Decode_WrongFormatVersion_FailsLoud_F3()
        {
            byte[] blob = MedicalSaveCodec.Encode(TwoClubs());
            int o = 0;
            CanonicalSerializer.WriteU32(blob, ref o,
                InjuriesMedicalConstants.MEDICAL_SAVE_FORMAT_VERSION + 1);

            Assert.Throws<InvalidOperationException>(() => MedicalSaveCodec.Decode(blob),
                "a MEDICAL_SAVE_FORMAT_VERSION mismatch must fail loud — no migration at Stage 0 (F3).");
        }

        [Test]
        public void Decode_TrailingBytes_FailsLoud_F5()
        {
            byte[] blob = MedicalSaveCodec.Encode(TwoClubs());
            var padded = new byte[blob.Length + 1];
            Array.Copy(blob, padded, blob.Length);

            Assert.Throws<InvalidOperationException>(() => MedicalSaveCodec.Decode(padded));
        }

        [Test]
        public void Decode_Truncated_FailsLoud_F5()
        {
            byte[] blob = MedicalSaveCodec.Encode(TwoClubs());
            var chopped = new byte[blob.Length - 3];
            Array.Copy(blob, chopped, chopped.Length);

            Assert.Throws<InvalidOperationException>(() => MedicalSaveCodec.Decode(chopped));
        }

        [Test]
        public void Decode_OversizeClubCount_FailsLoud_WithoutOverAllocating()
        {
            byte[] blob = MedicalSaveCodec.Encode(TwoClubs());
            int o = 4;
            CanonicalSerializer.WriteU32(blob, ref o, uint.MaxValue);

            Assert.Throws<InvalidOperationException>(() => MedicalSaveCodec.Decode(blob));
        }

        [Test]
        public void Decode_UndefinedSeverityOrdinal_FailsLoud_F4()
        {
            byte[] blob = MedicalSaveCodec.Encode(
                new[] { Club(1, (5, State(InjurySeverity.Minor, 3, 1, 1u))) });
            blob[20] = 200;

            Assert.Throws<InvalidOperationException>(() => MedicalSaveCodec.Decode(blob));
        }

        [Test]
        public void Decode_NegativeRecoveryOrInjuryCount_FailsLoud()
        {
            // version(4)+clubCount(4)+clubId(4)+playerCount(4)+playerId(4)+severity(1) = 21 ⇒ recovery
            byte[] withBadRecovery = MedicalSaveCodec.Encode(
                new[] { Club(1, (5, State(InjurySeverity.Minor, 3, 1, 1u))) });
            int o = 21;
            CanonicalSerializer.WriteI32(withBadRecovery, ref o, -1);
            Assert.Throws<InvalidOperationException>(() => MedicalSaveCodec.Decode(withBadRecovery),
                "a negative recovery counter is structurally impossible, not a [GT] band violation.");

            byte[] withBadCount = MedicalSaveCodec.Encode(
                new[] { Club(1, (5, State(InjurySeverity.Minor, 3, 1, 1u))) });
            o = 25;   // ... + recovery(4) ⇒ the injury-count i32
            CanonicalSerializer.WriteI32(withBadCount, ref o, -1);
            Assert.Throws<InvalidOperationException>(() => MedicalSaveCodec.Decode(withBadCount),
                "a negative cumulative injury count is corrupt.");
        }

        [Test]
        public void Decode_NonAscendingClubIds_FailsLoud()
        {
            byte[] blob = MedicalSaveCodec.Encode(new[] { Club(1), Club(2) });
            int o = 8;                                          // version + clubCount ⇒ first club id
            CanonicalSerializer.WriteI32(blob, ref o, 5);       // 5 then 2 — no longer ascending

            Assert.Throws<InvalidOperationException>(() => MedicalSaveCodec.Decode(blob));
        }

        [Test]
        public void Decode_OutOfBandRecoveryIsAccepted_TuningMustNotBrickSaves()
        {
            // Deliberate non-gate, mirroring #29's: RecoveryMax is [GT]. Enforcing it on decode would
            // mean a designer lowering the ceiling turns every existing save into an unloadable file.
            int beyondCeiling = InjuriesMedicalConstants.RecoveryMax + 100;
            ClubInjuryStates[] got = MedicalSaveCodec.Decode(MedicalSaveCodec.Encode(
                new[] { Club(1, (5, State(InjurySeverity.Serious, beyondCeiling, 1, 1u))) }));

            Assert.AreEqual(beyondCeiling, got[0].States[0].RecoveryRemaining);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-08-06 | —      | Initial suite (#41 T1): round-trip field identity, severity-tier   |
// |         |            |        | survival, the no-RNG-cursor block-size lock, canonical-order       |
// |         |            |        | determinism, the encode-side duplicate / unbound / F1 / F4 guards, |
// |         |            |        | and the decode fail-loud gates (F1/F3/F4/F5).                      |
#endregion
