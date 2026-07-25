// File:     src/season-save/LeagueBootstrap.cs
// Created:  2026-07-25
// Modified: 2026-07-25
// Author:   —
// Spec:     League Bootstrap design supplement (docs/tracking/league-bootstrap-design.md)
//           KD-1..KD-6, KD-9, §3 determinism, F1..F6; path-to-playable roadmap A3;
//           Squad/Player Data #27 (RosterGenerator); Code Standards #20
// Purpose:  Turn one world seed into a playable league — N clubs, one 25-player position-coherent
//           Squad each, a per-club strength, and the derived season seed. The roadmap-C3 substitute
//           for #47: playability needs a league to EXIST, not a database editor.
//
// Boot-time only (a new-game / new-season step, never the 10 Hz or 60 Hz loops), so the zero-alloc
// hot-path rules do not apply — the same carve-out RosterGenerator and the tactic file loaders take.

using TacticalDirector.DeterministicSim;
using TacticalDirector.PlayerDatabase;

namespace TacticalDirector.SeasonSave
{
    /// <summary>
    /// Deterministic league generation from a single <c>worldSeed</c> (design KD-4). Pure with respect
    /// to global state: it constructs its own <see cref="DeterministicRngService"/>, touches no
    /// <c>MatchEngine</c>, and registers nothing in the #16 domain-tag / subsystem-ordinal registry
    /// (KD-4 — <c>DOMAIN_TAG_SEASON_LOOP</c> / ordinal 84 stay pinned to #30 T2's first draw site per
    /// ERR-030-001).
    /// <para>
    /// <b>Determinism contract (§3).</b> The same <c>(worldSeed, clubCount)</c> yields byte-identical
    /// output: the same club ids, names, strength deltas, and every player's identity and all 31
    /// attributes. Different seeds change both the rosters and the strength ordering.
    /// </para>
    /// <para>
    /// <b>This generation path is persistence-equivalent — treat it like a save format version.</b>
    /// Rosters are NOT serialized: a save carries club IDs, and <see cref="League"/> re-derives every
    /// squad here when it is handed to <c>SeasonSaveManager.Load</c> as the
    /// <see cref="TacticalDirector.MatchEngine.ISquadProvider"/>. That interface's contract requires the
    /// resolved squad to be the SAME roster the saved match loaded, so ANY change that moves this
    /// function's output silently rewrites every club in every existing save — a draw-order change in
    /// <see cref="RosterGenerator"/>, a reorder of <c>NameCatalogue</c> or
    /// <see cref="ClubNameCatalogue"/>, an edit to
    /// <see cref="LeagueBootstrapConstants.SquadPositionCounts"/>, or a one-line tweak to a
    /// <c>[GT]</c> generation constant such as <c>PlayerDatabaseConstants.AttributeBaseMean</c>. That
    /// last one is the easiest to do by accident: it reads as an innocuous balance tweak, and
    /// <c>player-database</c> is one of the four catalogues carved out of the FR-CS-019 config
    /// migration, so its <c>[GT]</c> rows are still plain literals that invite editing in place.
    /// Every same-seed determinism test is self-referential and would stay green through all of these,
    /// so the guard is the pinned golden vector in <c>LeagueBootstrapGoldenVectorTests</c> — the same
    /// mechanism #16 uses for HKDF / SipHash / canonical serialization. If you change generation
    /// deliberately, re-pin that vector in the same commit and treat it as a save-breaking change.
    /// </para>
    /// </summary>
    public static class LeagueBootstrap
    {
        /// <summary>
        /// Generates a league of <see cref="LeagueBootstrapConstants.DefaultClubCount"/> clubs.
        /// </summary>
        /// <exception cref="System.ArgumentOutOfRangeException">F1 — the configured default club count
        /// is outside <c>[2, MaxClubCount]</c>.</exception>
        public static League Generate(ulong worldSeed) =>
            Generate(worldSeed, LeagueBootstrapConstants.DefaultClubCount);

        /// <summary>
        /// Generates a league of <paramref name="clubCount"/> clubs from <paramref name="worldSeed"/>.
        /// </summary>
        /// <param name="worldSeed">The single generation input; every derived seed comes from it (KD-4).</param>
        /// <param name="clubCount">Number of clubs, in <c>[2, MaxClubCount]</c>.</param>
        /// <exception cref="System.ArgumentOutOfRangeException">F1 — <paramref name="clubCount"/> is
        /// outside <c>[2, LeagueBootstrapConstants.MaxClubCount]</c>.</exception>
        /// <exception cref="System.InvalidOperationException">
        /// F2 — the club-name catalogue has fewer entries than <paramref name="clubCount"/>;
        /// F3 — the squad position template is incoherent; or the club count exceeds the RNG stream
        /// budget (<see cref="DeterministicSimConstants.MaxRngStreams"/>), since the bootstrap registers
        /// one roster stream per club. Each is a catalogue/config coherence bug, not caller input.
        /// </exception>
        public static League Generate(ulong worldSeed, int clubCount)
        {
            // F1 — caller input.
            if (clubCount < 2 || clubCount > LeagueBootstrapConstants.MaxClubCount)
            {
                throw new System.ArgumentOutOfRangeException(
                    nameof(clubCount), clubCount,
                    $"clubCount must be in [2, {LeagueBootstrapConstants.MaxClubCount}].");
            }

            // F2 — catalogue coherence. Checked per-call against the requested count (the test lock is
            // against MaxClubCount, so this only fires if the cap is raised past the catalogue).
            System.Collections.ObjectModel.ReadOnlyCollection<string> names = ClubNameCatalogue.Names;
            if (clubCount > names.Count)
            {
                throw new System.InvalidOperationException(
                    $"ClubNameCatalogue has {names.Count} names but {clubCount} clubs were requested; " +
                    "append names or lower LeagueBootstrapConstants.MaxClubCount.");
            }

            // Config coherence: one roster stream per club must fit the registry (KD-4). Without this
            // the failure would surface as DeterministicRngService's generic "registry full" deep inside
            // the generation loop, after some clubs had already been built.
            if (clubCount > DeterministicSimConstants.MaxRngStreams)
            {
                throw new System.InvalidOperationException(
                    $"{clubCount} clubs need {clubCount} RNG streams but the registry holds " +
                    $"{DeterministicSimConstants.MaxRngStreams}; lower MaxClubCount or raise MaxRngStreams.");
            }

            // F3 — template coherence (throws if the counts do not sum to CLUB_SQUAD_SIZE).
            PlayerPosition[] template = LeagueBootstrapConstants.BuildSquadPositionTemplate();

            // KD-4: three domain-separated derivations from the one world seed.
            ulong rosterSeed = Mix(worldSeed ^ LeagueBootstrapConstants.ROSTER_SEED_DOMAIN);
            ulong strengthSeed = Mix(worldSeed ^ LeagueBootstrapConstants.STRENGTH_SEED_DOMAIN);
            ulong seasonSeed = Mix(worldSeed ^ LeagueBootstrapConstants.SEASON_SEED_DOMAIN);

            int[] rankOfClub = BuildStrengthRanks(clubCount, strengthSeed);

            var rng = new DeterministicRngService(rosterSeed);
            var clubs = new Club[clubCount];
            var squads = new Squad[clubCount];

            for (int clubId = 0; clubId < clubCount; clubId++)
            {
                // entityId = clubId, so each club's stream key is distinct and its BASE roster (identity
                // + pre-strength attributes) is a function of (rosterSeed, clubId) alone — independent
                // of league size and generation order.
                //
                // NOT the shipped squad: ApplyStrength below shifts every attribute by a delta that
                // depends on clubCount TWICE (the rank permutation is over clubCount elements, and the
                // ramp denominator is clubCount - 1). So a club's final attributes DO change with league
                // size — which matters for #43, whose promotion/relegation transform changes the club
                // set. Only the base roster is size-invariant, and that is what the test locks.
                int streamIndex = rng.RegisterStream(
                    LeagueBootstrapConstants.ROSTER_STREAM_SITE_ID,
                    SubsystemOrdinals.PlayerDatabase,
                    entityId: clubId,
                    streamVersion: LeagueBootstrapConstants.ROSTER_STREAM_VERSION);

                Squad baseSquad = RosterGenerator.Generate(rng, streamIndex, clubId, template);

                int delta = StrengthDelta(rankOfClub[clubId], clubCount);
                squads[clubId] = ApplyStrength(baseSquad, delta);
                clubs[clubId] = new Club(clubId, names[clubId], delta);
            }

            return new League(worldSeed, seasonSeed, clubs, squads);
        }

        /// <summary>
        /// The strength delta for a club at <paramref name="rank"/> of <paramref name="clubCount"/>
        /// (design KD-5): a linear ramp from <c>-S</c> at rank 0 to <c>+S</c> at rank <c>N-1</c>,
        /// rounded half away from zero.
        /// <para>
        /// Integer arithmetic throughout — no float rounding enters a value that shapes every roster in
        /// the league (the #41/#40/#33 integer-arithmetic convention). The products cannot overflow
        /// because both inputs are bounded at their catalogue seams: the spread to the <c>[1,20]</c>
        /// attribute range (<c>S ≤ 19</c>, and non-negative, so the ramp cannot invert) and the club
        /// count to <see cref="LeagueBootstrapConstants.MaxClubCount"/>.
        /// </para>
        /// </summary>
        internal static int StrengthDelta(int rank, int clubCount)
        {
            // Guarded rather than assumed: the production caller gates clubCount >= 2 (F1), but this is
            // internal and test-visible, and a one-club league would divide by zero rather than say so.
            if (clubCount < 2)
            {
                throw new System.ArgumentOutOfRangeException(
                    nameof(clubCount), clubCount, "A strength ramp needs at least 2 clubs.");
            }

            if ((uint)rank >= (uint)clubCount)
            {
                throw new System.ArgumentOutOfRangeException(
                    nameof(rank), rank, $"rank must be in [0, {clubCount - 1}].");
            }

            int s = LeagueBootstrapConstants.LeagueStrengthSpread;
            int den = clubCount - 1;          // clubCount >= 2 is gated by the caller
            int num = (2 * s * rank) - (s * den);
            return RoundedDivide(num, den);
        }

        /// <summary>
        /// Rounds <paramref name="num"/> / <paramref name="den"/> to the nearest integer, halves away
        /// from zero. <paramref name="den"/> is always positive here.
        /// </summary>
        private static int RoundedDivide(int num, int den)
        {
            if (num >= 0)
            {
                return ((2 * num) + den) / (2 * den);
            }

            return -(((-2 * num) + den) / (2 * den));
        }

        /// <summary>
        /// Assigns each club a strength rank by Fisher–Yates over <paramref name="strengthSeed"/>
        /// (design KD-5). Returns <c>rankOfClub[clubId]</c> — a permutation of <c>0 .. N-1</c>, so the
        /// ramp is a bijection onto the ranks and no two clubs share one.
        /// <para>
        /// Uses a LOCAL SplitMix64 rather than a registered <see cref="DeterministicRngService"/> stream,
        /// exactly as <see cref="FixtureScheduler"/> does for its club-label permutation: registering a
        /// stream would need a subsystem ordinal, and the only season-adjacent one is pinned to #30 T2's
        /// first draw site (KD-4 / ERR-030-001).
        /// </para>
        /// </summary>
        internal static int[] BuildStrengthRanks(int clubCount, ulong strengthSeed)
        {
            var rankOfClub = new int[clubCount];
            for (int i = 0; i < clubCount; i++)
            {
                rankOfClub[i] = i;
            }

            ulong state = strengthSeed;
            for (int i = clubCount - 1; i > 0; i--)
            {
                int j = (int)NextBounded(ref state, (ulong)(i + 1));
                int tmp = rankOfClub[i];
                rankOfClub[i] = rankOfClub[j];
                rankOfClub[j] = tmp;
            }

            return rankOfClub;
        }

        /// <summary>
        /// Returns a copy of <paramref name="squad"/> with every <c>[1,20]</c> attribute of every player
        /// shifted by <paramref name="delta"/> and clamped (design KD-5).
        /// <para>
        /// <c>WeakFootRating</c> is deliberately NOT shifted: it is a <c>[1,5]</c> scale (#27 keeps it off
        /// the <c>[1,20]</c> array for exactly this reason), and a ±3 shift would pin nearly every club to
        /// a boundary.
        /// </para>
        /// </summary>
        internal static Squad ApplyStrength(Squad squad, int delta)
        {
            if (delta == 0)
            {
                return squad;
            }

            var players = new PlayerRecord[squad.Count];
            for (int i = 0; i < players.Length; i++)
            {
                PlayerRecord p = squad.GetPlayer(i);
                int[] attrs = p.Attributes.ToArray();
                for (int a = 0; a < attrs.Length; a++)
                {
                    attrs[a] = PlayerGenerationRng.Clamp(
                        attrs[a] + delta,
                        PlayerDatabaseConstants.ATTRIBUTE_MIN,
                        PlayerDatabaseConstants.ATTRIBUTE_MAX);
                }

                // FromArray leaves WeakFootRating untouched, so the struct copy carries it through.
                p.Attributes.FromArray(attrs);
                players[i] = p;
            }

            return new Squad(squad.ClubId, players);
        }

        /// <summary>
        /// SplitMix64's finalizing mix — the domain-separation function for KD-4's three derived seeds
        /// and the step function of the local Fisher–Yates generator.
        /// </summary>
        private static ulong Mix(ulong value)
        {
            // Exactly one step of the generator below from a state of `value` — expressed by delegation
            // rather than a second copy of the same four lines, so the two uses cannot drift apart.
            ulong state = value;
            return NextUInt64(ref state);
        }

        /// <summary>Advances the local SplitMix64 state and returns the next 64-bit value.</summary>
        private static ulong NextUInt64(ref ulong state)
        {
            unchecked  // Spec #16 §3.4.4: deliberate 64-bit wrap-around; not an overflow bug
            {
                state += 0x9E3779B97F4A7C15UL;
                ulong z = state;
                z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
                z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
                return z ^ (z >> 31);
            }
        }

        /// <summary>
        /// Draws a uniformly distributed value in <c>[0, bound)</c> by rejecting the unrepresentable
        /// tail — unbiased, and deterministic for a given state sequence (the
        /// <see cref="FixtureScheduler"/> shape).
        /// </summary>
        private static ulong NextBounded(ref ulong state, ulong bound)
        {
            ulong limit = ulong.MaxValue - (ulong.MaxValue % bound) - 1UL;
            ulong draw;
            do
            {
                draw = NextUInt64(ref state);
            }
            while (draw > limit);

            return draw % bound;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-07-25 | —      | Initial implementation (roadmap A3): domain-separated seed         |
// |         |            |        | derivation, seeded strength ramp, position-template roster         |
// |         |            |        | generation, per-club strength application, F1..F3 gates.           |
#endregion
