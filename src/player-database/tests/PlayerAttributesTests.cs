// File:     src/player-database/tests/PlayerAttributesTests.cs
// Created:  2026-07-15
// Modified: 2026-07-15
// Author:   —
// Spec:     Squad/Player Data Layer design supplement (docs/tracking/squad-player-data-design.md) §5
// Purpose:  PlayerAttributes clamp/array round-trip, identity defaults, AttrIdx ordinal count,
//           and direct constant-value checks on the position-bias table (not statistical sampling
//           over generated squads — design doc AR-2).

using System;

using NUnit.Framework;

namespace TacticalDirector.PlayerDatabase.Tests
{
    [TestFixture]
    public sealed class PlayerAttributesTests
    {
        [Test]
        public void CreateDefault_AllAttributesMidRange_WeakFootBase()
        {
            PlayerAttributes a = PlayerAttributes.CreateDefault();
            int[] arr = a.ToArray();
            for (int i = 0; i < arr.Length; i++)
            {
                Assert.AreEqual(PlayerDatabaseConstants.AttributeBaseMean, arr[i], $"index {i}");
            }
            Assert.AreEqual(PlayerDatabaseConstants.WeakFootBase, a.WeakFootRating);
        }

        [Test]
        public void AttrIdx_Count_MatchesConstant()
        {
            Assert.AreEqual(PlayerDatabaseConstants.ATTRIBUTE_COUNT, AttrIdx.Count);
        }

        [Test]
        public void ToArray_FromArray_RoundTrips()
        {
            var input = new int[AttrIdx.Count];
            for (int i = 0; i < input.Length; i++)
            {
                input[i] = 1 + (i % 20);
            }

            var a = new PlayerAttributes();
            a.FromArray(input);
            int[] output = a.ToArray();

            CollectionAssert.AreEqual(input, output);
        }

        [Test]
        public void FromArray_WeakFootRating_Untouched()
        {
            var a = new PlayerAttributes { WeakFootRating = 5 };
            a.FromArray(new int[AttrIdx.Count]);
            Assert.AreEqual(5, a.WeakFootRating, "FromArray must not touch WeakFootRating (separate [1,5] scale).");
        }

        [Test]
        public void FromArray_WrongLength_Throws()
        {
            var a = new PlayerAttributes();
            Assert.Throws<ArgumentException>(() => a.FromArray(new int[AttrIdx.Count - 1]));
            Assert.Throws<ArgumentException>(() => a.FromArray(new int[AttrIdx.Count + 1]));
            Assert.Throws<ArgumentException>(() => a.FromArray(null));
        }

        [Test]
        public void PositionBias_Goalkeeper_ExactValues()
        {
            int[] bias = PlayerDatabaseConstants.PositionAttributeBias[(int)PlayerPosition.Goalkeeper];
            Assert.AreEqual(4, bias[AttrIdx.Reflexes]);
            Assert.AreEqual(4, bias[AttrIdx.Handling]);
            Assert.AreEqual(4, bias[AttrIdx.Aerial]);
            Assert.AreEqual(4, bias[AttrIdx.OneVsOne]);
            Assert.AreEqual(4, bias[AttrIdx.Throwing]);
            Assert.AreEqual(4, bias[AttrIdx.Kicking]);
            Assert.AreEqual(0, bias[AttrIdx.Passing], "non-signature attribute must be unbiased.");
        }

        [Test]
        public void PositionBias_Defender_ExactValues()
        {
            int[] bias = PlayerDatabaseConstants.PositionAttributeBias[(int)PlayerPosition.Defender];
            Assert.AreEqual(3, bias[AttrIdx.Tackling]);
            Assert.AreEqual(3, bias[AttrIdx.Marking]);
            Assert.AreEqual(3, bias[AttrIdx.Strength]);
            Assert.AreEqual(0, bias[AttrIdx.Finishing]);
        }

        [Test]
        public void PositionBias_Midfielder_ExactValues()
        {
            int[] bias = PlayerDatabaseConstants.PositionAttributeBias[(int)PlayerPosition.Midfielder];
            Assert.AreEqual(3, bias[AttrIdx.Passing]);
            Assert.AreEqual(3, bias[AttrIdx.Vision]);
            Assert.AreEqual(3, bias[AttrIdx.Stamina]);
            Assert.AreEqual(0, bias[AttrIdx.Tackling]);
        }

        [Test]
        public void PositionBias_Forward_ExactValues()
        {
            int[] bias = PlayerDatabaseConstants.PositionAttributeBias[(int)PlayerPosition.Forward];
            Assert.AreEqual(3, bias[AttrIdx.Finishing]);
            Assert.AreEqual(3, bias[AttrIdx.Pace]);
            Assert.AreEqual(3, bias[AttrIdx.Dribbling]);
            Assert.AreEqual(0, bias[AttrIdx.Reflexes]);
        }

        [Test]
        public void PositionBias_TableLength_MatchesAttributeCount()
        {
            foreach (int[] row in PlayerDatabaseConstants.PositionAttributeBias)
            {
                Assert.AreEqual(AttrIdx.Count, row.Length);
            }
        }

        [Test]
        public void PositionCount_MatchesTheEnumAndTheBiasTable()
        {
            // POSITION_COUNT is [DERIVED] from the enum and consumed by two assemblies (RosterGenerator's
            // position draw, the league bootstrap's squad template). Appending a PlayerPosition member
            // without bumping it would silently make the new position undrawable and unindexable.
            Assert.AreEqual(
                System.Enum.GetValues(typeof(PlayerPosition)).Length,
                PlayerDatabaseConstants.POSITION_COUNT,
                "POSITION_COUNT must equal the PlayerPosition member count.");
            Assert.AreEqual(
                PlayerDatabaseConstants.POSITION_COUNT,
                PlayerDatabaseConstants.PositionAttributeBias.Length,
                "The bias table is indexed by PlayerPosition ordinal, so it needs one row per member.");
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                    |
// | 1.0     | 2026-07-15 | —      | Initial implementation.  |
#endregion
