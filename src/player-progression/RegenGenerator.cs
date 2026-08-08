// File:     src/player-progression/RegenGenerator.cs
// Created:  2026-07-24
// Modified: 2026-08-08
// Author:   —
// Spec:     Player Progression & Lifecycle #28 §3.3 (regen generation); Deterministic Simulation #16 (RNG); Code Standards #20
// Purpose:  Pure single-player regen generation (§3.3) — a young player with a drawn PotentialAbility
//           ceiling, generated over a fixed-budget reservation (the #27 RosterGenerator draw pattern).

using System;

using TacticalDirector.DeterministicSim;
using TacticalDirector.PlayerDatabase;

namespace TacticalDirector.PlayerProgression
{
    /// <summary>
    /// Deterministic single-player regen generation (§3.3), mirroring #27's
    /// <c>RosterGenerator.GenerateOne</c> per-player draw pattern (Reserve / DrawReserved… / CloseReservation)
    /// over a fixed <c>PROGRESSION_REGEN_FIELDS</c> budget, plus a PotentialAbility draw. Stateless — the
    /// caller registers the <c>player-progression.regen</c> stream (<c>entityId = clubId</c>, FR-PG-020),
    /// so it is unit-testable without booting a season. A regen is byte-reproducible from
    /// <c>(seed, clubId, stream position)</c> (FR-PG-010).
    /// </summary>
    public static class RegenGenerator
    {
        // Draw order within each regen's PROGRESSION_REGEN_FIELDS reservation. ORDINAL STABILITY:
        // append-only — reordering changes which value a given RNG draw produces. Attributes precede the
        // PA draw so PA can be floored above the generated CurrentAbility (guaranteeing "room to grow").
        private const int DrawFirstName = 0;
        private const int DrawLastName = 1;
        private const int DrawAge = 2;
        private const int DrawPosition = 3;
        private const int DrawWeakFoot = 4;
        private const int AttributeDrawBase = PlayerDatabaseConstants.IDENTITY_DRAWS_PER_PLAYER;
        private const int DrawPotentialAbility = AttributeDrawBase + AttrIdx.Count; // = 36

        // PlayerPosition has exactly 4 members (Goalkeeper..Forward) — a property of the enum.
        private const int PlayerPositionCount = 4;

        /// <summary>
        /// Generates one regen for <paramref name="clubId"/> with the fresh, caller-supplied
        /// <paramref name="newPlayerId"/> (never a retiree's — KD-3 / FR-PG-011). Consumes exactly
        /// <c>PROGRESSION_REGEN_FIELDS</c> draws from the stream at <paramref name="streamIndex"/> (one
        /// Reserve / DrawReserved… / CloseReservation cycle). Returns the record + its lifecycle overlay
        /// (which carries the drawn PotentialAbility — a record-only return would drop the ceiling).
        /// </summary>
        /// <param name="rng">The deterministic RNG service (the caller has registered the regen stream).</param>
        /// <param name="streamIndex">The registered <c>player-progression.regen</c> stream index — this alone scopes the draw to the club's stream (the caller registered it with <c>entityId = clubId</c>).</param>
        /// <param name="clubId">
        /// The club the regen joins. Retained for the §3.3 signature and T2 use (the <c>RegenResult</c>
        /// per-club grouping and nation-from-the-reference-roster). NOT consumed at T0 — the stream is
        /// already scoped by <paramref name="streamIndex"/> and the id is the explicit <paramref name="newPlayerId"/>.
        /// </param>
        /// <param name="newPlayerId">The fresh, monotonically-allocated PlayerId (FR-PG-011).</param>
        /// <param name="worldDay">The current world-day (anchors <see cref="PlayerLifecycle.BirthWorldDay"/>).</param>
        /// <exception cref="ArgumentNullException"><paramref name="rng"/> is null.</exception>
        /// <exception cref="InvalidOperationException">The RNG reservation/draw failed (draw-site misuse or corrupt reservation state).</exception>
        public static (PlayerRecord record, PlayerLifecycle life) GenerateRegen(
            DeterministicRngService rng,
            int streamIndex,
            int clubId,
            int newPlayerId,
            uint worldDay)
        {
            if (rng == null)
            {
                throw new ArgumentNullException(nameof(rng));
            }

            ushort reserveErr = rng.Reserve(streamIndex, PlayerProgressionConstants.PROGRESSION_REGEN_FIELDS);
            if (reserveErr != 0)
            {
                throw new InvalidOperationException(
                    "RegenGenerator.GenerateRegen: RNG reservation failed (a reservation is already open on this stream).");
            }

            int firstNameIdx = PlayerGenerationRng.DrawBounded(rng, streamIndex, DrawFirstName, NameCatalogue.FirstNames.Length);
            int lastNameIdx = PlayerGenerationRng.DrawBounded(rng, streamIndex, DrawLastName, NameCatalogue.LastNames.Length);
            // Nation is not a #27 PlayerRecord field today, so a regen draws name/age/position/weakFoot/
            // attrs/PA only (the §3.3 "club/nation from the reference roster" is a forward reference).
            int ageSpan = PlayerProgressionConstants.REGEN_AGE_MAX - PlayerProgressionConstants.REGEN_AGE_MIN + 1;
            int age = PlayerProgressionConstants.REGEN_AGE_MIN + PlayerGenerationRng.DrawBounded(rng, streamIndex, DrawAge, ageSpan);
            var position = (PlayerPosition)PlayerGenerationRng.DrawBounded(rng, streamIndex, DrawPosition, PlayerPositionCount);

            int weakFootSpan = 2 * PlayerDatabaseConstants.WeakFootSpread + 1;
            int weakFootJitter = PlayerGenerationRng.DrawBounded(rng, streamIndex, DrawWeakFoot, weakFootSpan) - PlayerDatabaseConstants.WeakFootSpread;
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

            var attributes = new PlayerAttributes();
            attributes.FromArray(attrs);
            attributes.WeakFootRating = weakFoot;

            int currentAbility = AbilityModel.ComputeCA(in attributes, position);

            // PA is drawn in [paFloor, ABILITY_MAX] where paFloor guarantees PA ≥ CA + headroom (§3.3
            // "room to grow"), floored at PA_MIN and never above ABILITY_MAX.
            int paFloor = Math.Max(
                PlayerProgressionConstants.PA_MIN,
                Math.Min(currentAbility + PlayerProgressionConstants.REGEN_PA_HEADROOM, PlayerProgressionConstants.ABILITY_MAX));
            int paSpan = PlayerProgressionConstants.ABILITY_MAX - paFloor + 1;
            int potentialAbility = paFloor + PlayerGenerationRng.DrawBounded(rng, streamIndex, DrawPotentialAbility, paSpan);

            rng.CloseReservation(streamIndex);

            long birthDays = age * (long)PlayerProgressionConstants.DAYS_PER_YEAR;
            uint birthWorldDay = worldDay >= birthDays ? (uint)(worldDay - birthDays) : 0u;

            var record = new PlayerRecord
            {
                PlayerId = newPlayerId,
                FirstName = NameCatalogue.FirstNames[firstNameIdx],
                LastName = NameCatalogue.LastNames[lastNameIdx],
                Age = age,
                Position = position,
                Attributes = attributes
            };

            var life = new PlayerLifecycle
            {
                PotentialAbility = potentialAbility,
                CurrentAbility = currentAbility,
                GrowthCursor = 0,
                BirthWorldDay = birthWorldDay,
                RetirementFlag = false,
                RetirementDay = 0,
                // Never the 0 default: day 0 is a legitimate world day, so a zero here would read as
                // "already advanced on day 0" and skip this regen's first daily step (the day-0 trap).
                LastAdvancedWorldDay = PlayerProgressionConstants.PROGRESSION_NOT_ADVANCED_SENTINEL
            };

            return (record, life);
        }

        // DrawBounded (the reserved-draw → [0, bound) modulo mapping + its accepted-bias rationale) and
        // Clamp live in PlayerDatabase.PlayerGenerationRng, shared with #27's RosterGenerator.
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-24 | —      | Initial implementation.                                        |
// | 1.1     | 2026-07-24 | —      | Adversarial-review L (doc-only): corrected the `clubId`        |
// |         |            |        | `<param>` doc — it is inert at T0 (retained for the §3.3       |
// |         |            |        | signature / T2 use), not the stream/id scoper the old doc      |
// |         |            |        | claimed (streamIndex scopes; newPlayerId is the id).           |
// | 1.2     | 2026-07-24 | —      | Adversarial-review L: DrawBounded + Clamp extracted to the     |
// |         |            |        | shared PlayerDatabase.PlayerGenerationRng (was duplicated with |
// |         |            |        | #27's RosterGenerator); byte-identical, call sites delegate.   |
// | 1.3     | 2026-08-08 | —      | #28 T1: the returned lifecycle seeds LastAdvancedWorldDay to   |
// |         |            |        | the never-advanced sentinel rather than leaving the 0 default,  |
// |         |            |        | which would have skipped a regen's first daily step. No draw   |
// |         |            |        | order, budget or value change — the RNG path is untouched.     |
#endregion
