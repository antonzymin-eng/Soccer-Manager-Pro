// File:     src/match-engine/TackleIntentCensus.cs
// Created:  2026-08-12
// Modified: 2026-08-12
// Author:   —
// Spec:     Defensive AI #14 §3.2 (FR-DA-010 pool exclusion), §3.6 (tackle intent),
//           match-engine wiring backlog W2 / §1.1, Code Standards #20
// Purpose:  Diagnostic accumulator for the wiring-backlog W2 tackle-intent census — one instance per
//           defending team. Observation only: nothing on a gameplay path reads it and it is not
//           serialized.

namespace TacticalDirector.MatchEngine
{
    /// <summary>
    /// Cumulative tackle-intent census for one team, in its DEFENDING role (the opponent holds the ball).
    ///
    /// <para>Counted in POSSESSION EPISODES, not in ticks. A tick count over a 10 Hz stride is a
    /// sampling-rate artifact — ~54 000 strides per match — and cannot be compared with football's
    /// "tackles per 90"; an episode is one spell of one carrier holding the ball, which is the unit a
    /// challenge actually happens in. The three per-stride totals at the end are kept only as
    /// attribution ratios, and are labelled as such.</para>
    ///
    /// <para>The counters are chosen to DISCRIMINATE, not merely to report. If a wired tackle turns out
    /// unable to fire, exactly three things can be responsible and they want opposite fixes:
    /// nobody is ever near the carrier (a positioning problem, upstream of W2);
    /// somebody is near him but is the primary presser, whom #14 §3.2's FR-DA-010 rule excludes from the
    /// HOLD_SHAPE pool — so the one player sent to the ball is by construction the one who cannot produce
    /// tackle intent; or a pool-eligible agent is near him and #14 still declines to COMMIT. Each has its
    /// own counter here, so a zero arrives with its cause attached.</para>
    /// </summary>
    internal struct TackleIntentCensus
    {
        /// <summary>Instrument reporting band (m) — NOT a gameplay constant and deliberately not in a
        /// catalogue. Nothing reads it but this census; the contact radius a wired tackle would use is a
        /// design decision this instrument exists to inform, and picking it here would pre-empt that.</summary>
        internal const float BandTightM = 2.0f;

        /// <summary>Instrument reporting band (m). See <see cref="BandTightM"/>.</summary>
        internal const float BandContactM = 1.5f;

        /// <summary>Instrument threshold (m) for "the carrier is not standing on the ball he holds".
        /// See <see cref="EpisodesCarrierBallGapOverThreshold"/>.</summary>
        internal const float CarrierBallGapThresholdM = 1.0f;

        /// <summary>Carrier id observed at the previous stride, for episode-edge detection.
        /// <c>MatchEngineConstants.NO_POSSESSION</c> when the ball was loose.</summary>
        public int PrevCarrier;

        /// <summary>Per-episode latches, cleared at each episode edge.</summary>
        public bool SeenWithin3M;
        public bool SeenWithin2M;
        public bool SeenWithin1P5M;
        public bool SeenPoolEligibleWithin3M;
        public bool SeenPresserWithin3M;
        public bool SeenIntentOnCarrier;
        public bool SeenCommitOnCarrier;
        public bool SeenCarrierBallGap;

        // ── Episode counts (the reportable unit) ──────────────────────────────

        /// <summary>Episodes in which the OPPOSING team held the ball — this team's defending episodes.</summary>
        public int DefendEpisodes;

        /// <summary>Of those, episodes in which any active agent of this team came within 3.0 m of the
        /// carrier (#14's own <c>TackleEligibleRadiusM</c>), 2.0 m, and 1.5 m.</summary>
        public int EpisodesWithin3M;
        public int EpisodesWithin2M;
        public int EpisodesWithin1P5M;

        /// <summary>Of the 3 m episodes, those where the nearby agent was HOLD_SHAPE-pool eligible —
        /// not the goalkeeper and not carrying a #13 press role. This is the population #14 can even
        /// consider for tackle intent.</summary>
        public int EpisodesPoolEligibleWithin3M;

        /// <summary>Of the 3 m episodes, those where a PRIMARY-PRESS or COVER-SHADOW agent was the one
        /// within 3 m. FR-DA-010 excludes exactly these from the pool, so every episode counted here and
        /// not in <see cref="EpisodesPoolEligibleWithin3M"/> is a challenge the current design forbids.</summary>
        public int EpisodesPresserWithin3M;

        /// <summary>Episodes in which this team produced at least one tackle intent naming the carrier.</summary>
        public int EpisodesIntentOnCarrier;

        /// <summary>Episodes in which at least one of those was <c>Commit</c> — the population a wired
        /// tackle, gated as W2 proposes, could act on at all.</summary>
        public int EpisodesCommitOnCarrier;

        /// <summary>Episodes in which the "carrier" was ever further than
        /// <see cref="CarrierBallGapThresholdM"/> from the ball he is recorded as possessing.
        ///
        /// <para>Possession here is a FLAG, not a kinematic constraint — the ball is not glued to the
        /// possessor (wiring backlog W6: <c>BallStateType.Controlled</c> has no producer). So
        /// tackler-to-carrier separation and tackler-to-BALL separation are different quantities, and a
        /// contact gate calibrated against one while the mechanism uses the other would be measuring the
        /// wrong distance. This counter says how far apart the two readings can drift.</para></summary>
        public int EpisodesCarrierBallGapOverThreshold;

        // ── Per-stride totals (attribution ratios only — NOT a per-90 rate) ───

        /// <summary>All tackle intents produced, every mode, counted once per intent per stride.</summary>
        public int StrideIntentsTotal;

        /// <summary>Of those, intents naming the carrier, split by mode. The split answers "why not
        /// COMMIT" without a second instrument: Jockey means the coverage floor or the angle refused it,
        /// Hold means both did.</summary>
        public int StrideOnCarrierCommit;
        public int StrideOnCarrierJockey;
        public int StrideOnCarrierHold;

        /// <summary>Opens a fresh episode: banks the closing one's latches and clears them.</summary>
        public void CloseEpisode()
        {
            if (SeenWithin3M) { EpisodesWithin3M++; }
            if (SeenWithin2M) { EpisodesWithin2M++; }
            if (SeenWithin1P5M) { EpisodesWithin1P5M++; }
            if (SeenPoolEligibleWithin3M) { EpisodesPoolEligibleWithin3M++; }
            if (SeenPresserWithin3M) { EpisodesPresserWithin3M++; }
            if (SeenIntentOnCarrier) { EpisodesIntentOnCarrier++; }
            if (SeenCommitOnCarrier) { EpisodesCommitOnCarrier++; }
            if (SeenCarrierBallGap) { EpisodesCarrierBallGapOverThreshold++; }

            SeenWithin3M = false;
            SeenWithin2M = false;
            SeenWithin1P5M = false;
            SeenPoolEligibleWithin3M = false;
            SeenPresserWithin3M = false;
            SeenIntentOnCarrier = false;
            SeenCommitOnCarrier = false;
            SeenCarrierBallGap = false;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-08-12 | —      | Initial. Wiring backlog W2 census accumulator. Episode-based, not  |
// |         |            |        | tick-based (a 10 Hz tick count is a sampling-rate artifact). The   |
// |         |            |        | counters discriminate the three possible causes of a zero: nobody  |
// |         |            |        | near the carrier, the nearby man is the FR-DA-010-excluded         |
// |         |            |        | presser, or #14 declines to COMMIT.                                |
#endregion
