// File:     src/player-progression/RegenGenerator.cs
// Created:  2026-07-24
// Modified: 2026-08-11 (AR pass 7 — GrowthCursor credits its own construction day, ERR-028-018's
//           second writer — v1.5)
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

            // Signed (ERR-028-006): a regen generated on an early world day is born before the epoch,
            // and clamping that to 0 would report him as age worldDay/365 rather than his drawn age.
            long birthWorldDay = (long)worldDay - age * (long)PlayerProgressionConstants.DAYS_PER_YEAR;

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

                // AR pass 7. ERR-028-018 credited the construction day's own band step to the cursor,
                // and applied it at ONE of the two sites that construct a lifecycle from scratch. This
                // is the other one. A regen anchored at worldDay whose cursor starts at 0 accrues
                // N·365 − 1 days over an N-year band — one whole [1,20] point short, with the same
                // 364-day residue, which then survives the accrual-free Stable band. Measured: a regen
                // gained +5 over its remaining Growth band where an identically-generated seeded player
                // gained +6, leaving it one point worse for the whole of ages 24–30, its selectable
                // prime, and taking its first Decline point 364 days late.
                //
                // The comment below asserted these two "agree by construction". They did not: the
                // anchor was worldDay and the cursor was 0, which is exactly the disagreement
                // ERR-028-018 exists to remove. Now they do.
                //
                // Classified rather than hard-coded to GROWTH_DAILY_POINTS: a regen's drawn age is
                // 16–20 and therefore always Growth today, but that is a fact about REGEN_AGE_MAX vs
                // GROWTH_AGE, not about this line, and it should not silently become wrong if either
                // constant moves.
                GrowthCursor = BandStepFor(age),

                BirthWorldDay = birthWorldDay,
                RetirementFlag = false,
                RetirementDay = 0,
                // M3 (ERR-028-014 carryforward): worldDay, NOT the never-advanced sentinel. A regen
                // describes the roster AS OF worldDay, exactly like a seeded player (SeedLifecycle
                // anchors the same way) — his anchor and his cursor agree by construction (TRUE only
                // since AR pass 7 credited the cursor above; when this comment was written the anchor
                // was worldDay and the cursor was 0, so they did not agree and the claim was false),
                // so his first AdvanceDay call correctly treats worldDay itself as already
                // accounted for. The sentinel
                // was retired from the set of legal STORE states by ERR-028-014 the day after this line
                // was written: ProgressionEngine.FromBlocks and ProgressionSaveCodec.Encode/Decode all
                // refuse it by name now, so a block built from a generated regen would fail every one of
                // them — the day-0 trap this comment used to cite no longer applies to a live consumer,
                // because nothing downstream of a regen can hold the sentinel and survive.
                LastAdvancedWorldDay = worldDay
            };

            return (record, life);
        }

        /// <summary>
        /// The construction day's own band step — the ERR-028-018 invariant, shared in shape with
        /// <c>ProgressionEngine.SeedLifecycle</c>. Every site that anchors
        /// <see cref="PlayerLifecycle.LastAdvancedWorldDay"/> at its own construction day owes this,
        /// because that anchor declares the day already lived and a zero cursor accounts for it as
        /// nothing — costing one whole attribute point per band traversal.
        /// </summary>
        private static long BandStepFor(int age)
        {
            AbilityModel.AgeBand band = AbilityModel.ClassifyAgeBand(age);
            return band == AbilityModel.AgeBand.Growth ? PlayerProgressionConstants.GROWTH_DAILY_POINTS
                 : band == AbilityModel.AgeBand.Decline ? PlayerProgressionConstants.DECLINE_DAILY_POINTS
                 : 0;
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
// | 1.4     | 2026-08-10 | —      | M3 (ERR-028-014 carryforward): LastAdvancedWorldDay now seeds  |
// |         |            |        | to worldDay, not the sentinel — ERR-028-014 retired the        |
// |         |            |        | sentinel from every legal store state one day after 1.3, so    |
// |         |            |        | FromBlocks/Encode/Decode all refused what this method returned.|
// |         |            |        | No draw order, budget or value change.                         |
// | 1.5     | 2026-08-11 | —      | AR pass 7. ERR-028-018 credited a lifecycle's construction day  |
// |         |            |        | to its GrowthCursor at ProgressionEngine.SeedLifecycle but      |
// |         |            |        | never visited this, the OTHER site that constructs a            |
// |         |            |        | PlayerLifecycle from scratch — GrowthCursor = 0 accounted the   |
// |         |            |        | construction day (LastAdvancedWorldDay = worldDay) as already   |
// |         |            |        | lived and credited nothing for it, costing a regen one whole    |
// |         |            |        | attribute point across its Growth band versus an identically-   |
// |         |            |        | generated seeded player (measured: +5 vs +6). Now                |
// |         |            |        | GrowthCursor = BandStepFor(age), the construction day's own      |
// |         |            |        | band step (Growth-band only, by the REGEN_AGE_MAX < GROWTH_AGE   |
// |         |            |        | precondition). No draw order, budget or value change — the RNG   |
// |         |            |        | path is untouched. This landing was recorded at 8556ddd with no  |
// |         |            |        | version row (the sixth consecutive FR-CS-057 recurrence,         |
// |         |            |        | L-1) — this row and the corrected `Modified` header above        |
// |         |            |        | backfill it.                                                     |
#endregion
