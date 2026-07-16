// File:     src/player-database/RosterGenerator.cs
// Created:  2026-07-15
// Modified: 2026-07-16 (AR-3 L-6, doc-only: DrawBounded's modulo-bias acceptance + fixed-budget rationale recorded)
// Author:   —
// Spec:     Squad/Player Data Layer design supplement (docs/tracking/squad-player-data-design.md) §3, KD-5
// Purpose:  Deterministic squad generation over DeterministicRngService — no System.Random anywhere
//           in this assembly. Stateless: the caller registers the RNG stream (design doc §3 —
//           mirrors the match-flow.card-severity per-call Reserve/Draw/Close pattern), so this can
//           be unit tested without booting a match.

using System;

using TacticalDirector.DeterministicSim;

namespace TacticalDirector.PlayerDatabase
{
    /// <summary>
    /// Deterministic roster generation. Design doc §3. Recommended registration:
    /// <c>rng.RegisterStream("player-database.roster-generation", SubsystemOrdinals.PlayerDatabase, entityId: clubId, streamVersion: 1)</c>.
    /// </summary>
    public static class RosterGenerator
    {
        // Draw order within each player's FIELDS_PER_PLAYER reservation. ORDINAL STABILITY:
        // append-only — reordering changes which value a given RNG draw produces.
        private const int DrawFirstName = 0;
        private const int DrawLastName = 1;
        private const int DrawAge = 2;
        private const int DrawPosition = 3;
        private const int DrawWeakFoot = 4;
        private const int AttributeDrawBase = PlayerDatabaseConstants.IDENTITY_DRAWS_PER_PLAYER;

        // PlayerPosition has exactly 4 members (Goalkeeper..Forward); not PlayerDatabaseConstants
        // because it is a property of the enum, not a tunable.
        private const int PlayerPositionCount = 4;

        /// <summary>
        /// Generates a <paramref name="count"/>-player squad for <paramref name="clubId"/>. Consumes
        /// exactly <c>count * PlayerDatabaseConstants.FIELDS_PER_PLAYER</c> draws from the stream at
        /// <paramref name="streamIndex"/> (one Reserve/DrawReserved.../CloseReservation cycle per player).
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="rng"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="count"/> is outside [1, CLUB_SQUAD_SIZE].</exception>
        /// <exception cref="InvalidOperationException">The RNG stream reservation failed (a reservation was already open on this stream — draw-site misuse).</exception>
        public static Squad Generate(DeterministicRngService rng, int streamIndex, int clubId, int count)
        {
            if (rng == null)
            {
                throw new ArgumentNullException(nameof(rng));
            }
            if (count <= 0 || count > PlayerDatabaseConstants.CLUB_SQUAD_SIZE)
            {
                throw new ArgumentException(
                    $"count must be in [1, {PlayerDatabaseConstants.CLUB_SQUAD_SIZE}]; got {count}.",
                    nameof(count));
            }

            var players = new PlayerRecord[count];
            for (int localIndex = 0; localIndex < count; localIndex++)
            {
                players[localIndex] = GenerateOne(rng, streamIndex, clubId, localIndex);
            }
            return new Squad(clubId, players);
        }

        private static PlayerRecord GenerateOne(DeterministicRngService rng, int streamIndex, int clubId, int localIndex)
        {
            ushort reserveErr = rng.Reserve(streamIndex, PlayerDatabaseConstants.FIELDS_PER_PLAYER);
            if (reserveErr != 0)
            {
                throw new InvalidOperationException(
                    "RosterGenerator.GenerateOne: RNG reservation failed (a reservation is already open on this stream).");
            }

            int firstNameIdx = DrawBounded(rng, streamIndex, DrawFirstName, NameCatalogue.FirstNames.Length);
            int lastNameIdx = DrawBounded(rng, streamIndex, DrawLastName, NameCatalogue.LastNames.Length);
            int ageSpan = PlayerDatabaseConstants.AgeMax - PlayerDatabaseConstants.AgeMin + 1;
            int age = PlayerDatabaseConstants.AgeMin + DrawBounded(rng, streamIndex, DrawAge, ageSpan);
            // Uniform over the 4 positions is a documented Stage-0 simplification — a real squad's
            // position distribution (few GKs, many outfielders) is a future refinement, not designed here.
            var position = (PlayerPosition)DrawBounded(rng, streamIndex, DrawPosition, PlayerPositionCount);
            int weakFootJitterSpan = 2 * PlayerDatabaseConstants.WeakFootSpread + 1;
            int weakFootJitter = DrawBounded(rng, streamIndex, DrawWeakFoot, weakFootJitterSpan) - PlayerDatabaseConstants.WeakFootSpread;
            int weakFoot = Clamp(
                PlayerDatabaseConstants.WeakFootBase + weakFootJitter,
                PlayerDatabaseConstants.WEAK_FOOT_MIN,
                PlayerDatabaseConstants.WEAK_FOOT_MAX);

            int[] bias = PlayerDatabaseConstants.PositionAttributeBias[(int)position];
            int[] attrs = new int[AttrIdx.Count];
            int jitterSpan = 2 * PlayerDatabaseConstants.AttributeSpread + 1;
            for (int i = 0; i < AttrIdx.Count; i++)
            {
                int jitter = DrawBounded(rng, streamIndex, AttributeDrawBase + i, jitterSpan) - PlayerDatabaseConstants.AttributeSpread;
                attrs[i] = Clamp(
                    PlayerDatabaseConstants.AttributeBaseMean + bias[i] + jitter,
                    PlayerDatabaseConstants.ATTRIBUTE_MIN,
                    PlayerDatabaseConstants.ATTRIBUTE_MAX);
            }

            rng.CloseReservation(streamIndex);

            var attributes = new PlayerAttributes();
            attributes.FromArray(attrs);
            attributes.WeakFootRating = weakFoot;

            return new PlayerRecord
            {
                PlayerId = clubId * PlayerDatabaseConstants.CLUB_SQUAD_SIZE + localIndex,
                FirstName = NameCatalogue.FirstNames[firstNameIdx],
                LastName = NameCatalogue.LastNames[lastNameIdx],
                Age = age,
                Position = position,
                Attributes = attributes
            };
        }

        // Maps one reserved draw to a value in [0, bound). bound must be > 0 (caller-guaranteed —
        // every call site here uses a fixed positive catalogue length or span). AR-3 L-6: the plain
        // `value % bound` mapping carries modulo bias, deliberately accepted — over a u64 draw the
        // bias for every bound used here (≤ 32) is < 2^-59, generation is not a statistically
        // load-bearing surface (unlike the pinned RNG-quality work in PassErrorCalculator), and the
        // mapping is deterministic either way. Do NOT "fix" this with rejection sampling: a
        // variable draw count per field would break the FIELDS_PER_PLAYER fixed-budget reservation.
        private static int DrawBounded(DeterministicRngService rng, int streamIndex, int drawIndex, int bound)
        {
            ushort err = rng.DrawReserved(streamIndex, drawIndex, out ulong value);
            if (err != 0)
            {
                throw new InvalidOperationException(
                    "RosterGenerator.DrawBounded: draw failed — corrupt reservation state (internal invariant).");
            }
            return (int)(value % (ulong)bound);
        }

        private static int Clamp(int value, int min, int max)
        {
            if (value < min)
            {
                return min;
            }
            if (value > max)
            {
                return max;
            }
            return value;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-15 | —      | Initial implementation.                                        |
// | 1.1     | 2026-07-15 | —      | Code-review pass: added the missing DrawPosition identity      |
// |         |            |        | draw (PlayerRecord.Position previously had no generation       |
// |         |            |        | input at all); WeakFootRating jitter now uses its own          |
// |         |            |        | WeakFootSpread instead of the much-wider AttributeSpread.      |
// | 1.2     | 2026-07-16 | —      | AR-3 L-6 (doc-only): DrawBounded's `value % bound` modulo bias |
// |         |            |        | recorded as deliberate (< 2^-59 for bounds ≤ 32; rejection     |
// |         |            |        | sampling would break the fixed FIELDS_PER_PLAYER reservation). |
#endregion
