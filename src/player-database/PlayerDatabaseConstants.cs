// File:     src/player-database/PlayerDatabaseConstants.cs
// Created:  2026-07-15
// Modified: 2026-07-15
// Author:   —
// Spec:     Squad/Player Data Layer design supplement (docs/tracking/squad-player-data-design.md), Code Standards #20
// Purpose:  All numeric constants for the player database — attribute bounds, generation tuning,
//           and the position-bias table. Region order: Fixed -> Derived -> GT.

namespace TacticalDirector.PlayerDatabase
{
    /// <summary>
    /// All constants for the player database. No magic literals in RosterGenerator/SquadFileLoader.
    /// Squad/Player Data Layer design supplement.
    /// </summary>
    public static class PlayerDatabaseConstants
    {
        #region Fixed

        /// <summary>[FIXED] Minimum value for any [1,20]-scale attribute.</summary>
        public const int ATTRIBUTE_MIN = 1;

        /// <summary>[FIXED] Maximum value for any [1,20]-scale attribute.</summary>
        public const int ATTRIBUTE_MAX = 20;

        /// <summary>[FIXED] Minimum value for WeakFootRating (a distinct [1,5] scale — never folded into the [1,20] attribute set).</summary>
        public const int WEAK_FOOT_MIN = 1;

        /// <summary>[FIXED] Maximum value for WeakFootRating.</summary>
        public const int WEAK_FOOT_MAX = 5;

        /// <summary>
        /// [FIXED] Maximum players in one club's roster (master plan §4.2 — 25). Deliberately named
        /// distinctly from <c>MatchEngineConstants.SQUAD_SIZE</c> (=22), which is the unrelated
        /// match-scoped "both teams' on-pitch agent count" concept. Design doc KD-3.
        /// </summary>
        public const int CLUB_SQUAD_SIZE = 25;

        #endregion

        #region Derived

        /// <summary>
        /// [DERIVED] Number of int[1,20] fields on <see cref="PlayerAttributes"/> (26 consumed +
        /// 5 reserved; excludes WeakFootRating, a separate [1,5] field). Source: AttrIdx member count.
        /// </summary>
        public const int ATTRIBUTE_COUNT = 31;

        /// <summary>
        /// [DERIVED] Number of <see cref="PlayerPosition"/> members — a property of the enum, not a
        /// tunable. Source: the PlayerPosition member count; also the row count of
        /// <see cref="PositionAttributeBias"/>, which is indexed by that ordinal.
        /// <para>
        /// Declared once here because two independent consumers need it (<c>RosterGenerator</c>'s
        /// position draw and the league bootstrap's position template), and a second private copy is
        /// exactly the parallel-surface drift this project keeps finding. Locked against the enum by
        /// test.
        /// </para>
        /// </summary>
        public const int POSITION_COUNT = 4;

        /// <summary>
        /// [DERIVED] Identity-draw count per generated player: first name index, last name index,
        /// age, position, weak foot.
        /// </summary>
        public const int IDENTITY_DRAWS_PER_PLAYER = 5;

        /// <summary>
        /// [DERIVED] RNG draws reserved per generated player: IDENTITY_DRAWS_PER_PLAYER identity
        /// draws + ATTRIBUTE_COUNT attribute draws. Source: IDENTITY_DRAWS_PER_PLAYER, ATTRIBUTE_COUNT.
        /// </summary>
        public const int FIELDS_PER_PLAYER = IDENTITY_DRAWS_PER_PLAYER + ATTRIBUTE_COUNT;

        #endregion

        #region GT

        /// <summary>[GT] Mean value new attributes are generated around, before position bias and jitter. TODO: replace with config loader (Stage 1).</summary>
        public static readonly int AttributeBaseMean = 10;

        /// <summary>[GT] Symmetric jitter half-width applied around (BaseMean + PositionBias). TODO: replace with config loader (Stage 1).</summary>
        public static readonly int AttributeSpread = 4;

        /// <summary>[GT] Minimum generated player age (years). TODO: replace with config loader (Stage 1).</summary>
        public static readonly int AgeMin = 17;

        /// <summary>[GT] Maximum generated player age (years). TODO: replace with config loader (Stage 1).</summary>
        public static readonly int AgeMax = 35;

        /// <summary>[GT] Mean value new WeakFootRating is generated around (mid-scale). TODO: replace with config loader (Stage 1).</summary>
        public static readonly int WeakFootBase = 3;

        /// <summary>
        /// [GT] Symmetric jitter half-width for WeakFootRating — deliberately its own (narrower)
        /// value rather than reusing AttributeSpread: WeakFootBase(3) +/- 2 exactly spans the valid
        /// [1,5] range with no clamping, where +/- AttributeSpread(4) would clamp roughly 6 of every
        /// 9 draws to the boundary. TODO: replace with config loader (Stage 1).
        /// </summary>
        public static readonly int WeakFootSpread = 2;

        /// <summary>
        /// [GT] Per-position attribute bias table: [(int)PlayerPosition][AttrIdx.*] -> additive bias.
        /// Nonzero only at each position's signature attributes; everything else is 0 (no bias).
        /// Array-valued [GT] carve-out per the TacticalInstructionsConstants precedent — no runtime
        /// config override at Stage 0; covered by direct constant-value tests, not statistical
        /// sampling (design doc AR-2). TODO: replace with config loader (Stage 1).
        /// </summary>
        public static readonly int[][] PositionAttributeBias = BuildPositionAttributeBias();

        private static int[][] BuildPositionAttributeBias()
        {
            int[] gk = new int[ATTRIBUTE_COUNT];
            gk[AttrIdx.Reflexes] = 4;
            gk[AttrIdx.Handling] = 4;
            gk[AttrIdx.Aerial] = 4;
            gk[AttrIdx.OneVsOne] = 4;
            gk[AttrIdx.Throwing] = 4;
            gk[AttrIdx.Kicking] = 4;

            int[] df = new int[ATTRIBUTE_COUNT];
            df[AttrIdx.Tackling] = 3;
            df[AttrIdx.Marking] = 3;
            df[AttrIdx.Strength] = 3;

            int[] mf = new int[ATTRIBUTE_COUNT];
            mf[AttrIdx.Passing] = 3;
            mf[AttrIdx.Vision] = 3;
            mf[AttrIdx.Stamina] = 3;

            int[] fw = new int[ATTRIBUTE_COUNT];
            fw[AttrIdx.Finishing] = 3;
            fw[AttrIdx.Pace] = 3;
            fw[AttrIdx.Dribbling] = 3;

            // Indexed by (int)PlayerPosition — Goalkeeper=0, Defender=1, Midfielder=2, Forward=3.
            return new[] { gk, df, mf, fw };
        }

        #endregion
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-15 | —      | Initial implementation.                                        |
// | 1.1     | 2026-07-15 | —      | Code-review pass: IDENTITY_DRAWS_PER_PLAYER (5, was implicitly |
// |         |            |        | 4 — position had no draw) + FIELDS_PER_PLAYER 35->36;          |
// |         |            |        | WeakFootSpread added (its own narrower jitter — WeakFootBase   |
// |         |            |        | was jittering by +/-ATTRIBUTE_SPREAD, clamping most draws).    |
#endregion
