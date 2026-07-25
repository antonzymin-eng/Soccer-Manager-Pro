// File:     src/player-database/RosterGenerator.cs
// Created:  2026-07-15
// Modified: 2026-07-25 (additive supplied-position Generate overload — league-bootstrap KD-6; drawn-position path byte-identical)
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

        // The PlayerPosition member count. Sourced from the catalogue rather than re-declared here:
        // the league bootstrap's position template needs the same value, and two private copies of
        // "how many positions are there" is the parallel-surface drift class this project keeps
        // finding. It is a [DERIVED] property of the enum, not a tunable.
        private const int PlayerPositionCount = PlayerDatabaseConstants.POSITION_COUNT;

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
                players[localIndex] = GenerateOne(rng, streamIndex, clubId, localIndex, forcedPosition: null);
            }
            return new Squad(clubId, players);
        }

        /// <summary>
        /// Generates a squad whose player positions are <b>supplied</b> rather than drawn — one player
        /// per entry of <paramref name="positions"/>, in order. The per-player draw budget is identical
        /// to <see cref="Generate(DeterministicRngService, int, int, int)"/> (the position draw still
        /// happens and its value is discarded), so the two overloads share one stream layout and the
        /// drawn-position path stays byte-identical.
        /// <para>
        /// This is the refinement the drawn-position path's own note defers: uniform-over-4 positions
        /// give a 25-player squad a few-percent chance per line of being unable to field a back four,
        /// which a league generator cannot tolerate (it fails <i>by seed</i>). The caller supplies a
        /// position template instead. Designed in <c>docs/tracking/league-bootstrap-design.md</c> KD-6.
        /// </para>
        /// </summary>
        /// <param name="rng">The RNG service holding the registered generation stream.</param>
        /// <param name="streamIndex">Index of that stream.</param>
        /// <param name="clubId">The club whose roster this is (scopes <see cref="PlayerRecord.PlayerId"/>).</param>
        /// <param name="positions">One entry per player, giving that player's position. Length is the squad size.</param>
        /// <exception cref="ArgumentNullException"><paramref name="rng"/> or <paramref name="positions"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="positions"/> is empty, longer than
        /// <see cref="PlayerDatabaseConstants.CLUB_SQUAD_SIZE"/>, or contains a value outside the
        /// <see cref="PlayerPosition"/> roster.</exception>
        /// <exception cref="InvalidOperationException">The RNG stream reservation failed.</exception>
        public static Squad Generate(
            DeterministicRngService rng, int streamIndex, int clubId, PlayerPosition[] positions)
        {
            if (rng == null)
            {
                throw new ArgumentNullException(nameof(rng));
            }
            if (positions == null)
            {
                throw new ArgumentNullException(nameof(positions));
            }
            if (positions.Length == 0 || positions.Length > PlayerDatabaseConstants.CLUB_SQUAD_SIZE)
            {
                throw new ArgumentException(
                    $"positions.Length must be in [1, {PlayerDatabaseConstants.CLUB_SQUAD_SIZE}]; " +
                    $"got {positions.Length}.",
                    nameof(positions));
            }

            // Gate every entry BEFORE drawing, so a bad template consumes no stream cursor (the
            // "validate before the draw" replay-parity rule the living-world text generator follows).
            for (int i = 0; i < positions.Length; i++)
            {
                if ((int)positions[i] < 0 || (int)positions[i] >= PlayerPositionCount)
                {
                    throw new ArgumentException(
                        $"positions[{i}] = {(int)positions[i]} is outside the PlayerPosition roster " +
                        $"[0, {PlayerPositionCount - 1}].",
                        nameof(positions));
                }
            }

            var players = new PlayerRecord[positions.Length];
            for (int localIndex = 0; localIndex < positions.Length; localIndex++)
            {
                players[localIndex] = GenerateOne(rng, streamIndex, clubId, localIndex, positions[localIndex]);
            }
            return new Squad(clubId, players);
        }

        private static PlayerRecord GenerateOne(
            DeterministicRngService rng, int streamIndex, int clubId, int localIndex,
            PlayerPosition? forcedPosition)
        {
            ushort reserveErr = rng.Reserve(streamIndex, PlayerDatabaseConstants.FIELDS_PER_PLAYER);
            if (reserveErr != 0)
            {
                throw new InvalidOperationException(
                    "RosterGenerator.GenerateOne: RNG reservation failed (a reservation is already open on this stream).");
            }

            int firstNameIdx = PlayerGenerationRng.DrawBounded(rng, streamIndex, DrawFirstName, NameCatalogue.FirstNames.Length);
            int lastNameIdx = PlayerGenerationRng.DrawBounded(rng, streamIndex, DrawLastName, NameCatalogue.LastNames.Length);
            int ageSpan = PlayerDatabaseConstants.AgeMax - PlayerDatabaseConstants.AgeMin + 1;
            int age = PlayerDatabaseConstants.AgeMin + PlayerGenerationRng.DrawBounded(rng, streamIndex, DrawAge, ageSpan);
            // Uniform over the 4 positions is a documented Stage-0 simplification — a real squad's
            // position distribution (few GKs, many outfielders) is the caller-supplied-template
            // overload above (league-bootstrap-design.md KD-6). The draw runs either way so both
            // overloads consume exactly FIELDS_PER_PLAYER and share one stream layout.
            var drawnPosition = (PlayerPosition)PlayerGenerationRng.DrawBounded(rng, streamIndex, DrawPosition, PlayerPositionCount);
            PlayerPosition position = forcedPosition ?? drawnPosition;
            int weakFootJitterSpan = 2 * PlayerDatabaseConstants.WeakFootSpread + 1;
            int weakFootJitter = PlayerGenerationRng.DrawBounded(rng, streamIndex, DrawWeakFoot, weakFootJitterSpan) - PlayerDatabaseConstants.WeakFootSpread;
            int weakFoot = PlayerGenerationRng.Clamp(
                PlayerDatabaseConstants.WeakFootBase + weakFootJitter,
                PlayerDatabaseConstants.WEAK_FOOT_MIN,
                PlayerDatabaseConstants.WEAK_FOOT_MAX);

            int[] bias = PlayerDatabaseConstants.PositionAttributeBias[(int)position];
            int[] attrs = new int[AttrIdx.Count];
            int jitterSpan = 2 * PlayerDatabaseConstants.AttributeSpread + 1;
            for (int i = 0; i < AttrIdx.Count; i++)
            {
                int jitter = PlayerGenerationRng.DrawBounded(rng, streamIndex, AttributeDrawBase + i, jitterSpan) - PlayerDatabaseConstants.AttributeSpread;
                attrs[i] = PlayerGenerationRng.Clamp(
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

        // DrawBounded (the reserved-draw → [0, bound) modulo mapping, incl. its accepted-bias rationale)
        // and Clamp now live in PlayerGenerationRng, shared with #28's RegenGenerator — see that file.
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
// | 1.3     | 2026-07-24 | —      | #28 adversarial-review Low: DrawBounded + Clamp extracted to   |
// |         |            |        | shared PlayerGenerationRng (was duplicated in #28's            |
// |         |            |        | RegenGenerator); byte-identical, call sites delegate.          |
// | 1.4     | 2026-07-25 | —      | league-bootstrap KD-6: additive Generate(..., PlayerPosition[])|
// |         |            |        | overload — positions supplied, not drawn, so a league          |
// |         |            |        | generator can guarantee a fieldable squad (LineupSelector      |
// |         |            |        | fails loud on an unfillable starter line, and uniform          |
// |         |            |        | position draws miss a back four a few percent of the time).    |
// |         |            |        | The position draw still runs and is discarded, so the budget,  |
// |         |            |        | the stream layout, and the drawn-position path are unchanged.  |
#endregion
