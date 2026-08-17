// File:     src/defensive-ai/TackleDuelInputs.cs
// Created:  2026-08-12
// Modified: 2026-08-12
// Author:   —
// Spec:     Defensive AI #14 §3.6.5, Player Database #27 (attribute projection), Code Standards #20
// Purpose:  The inputs to the #14 §3.6.5 tackle duel — the two players' relevant abilities plus the
//           geometry of the challenge. Projected floats only; no roster or engine types cross here.

namespace TacticalDirector.DefensiveAI
{
    /// <summary>
    /// Everything <see cref="TackleOutcomeResolver"/> needs to resolve one challenge (#14 §3.6.5).
    ///
    /// <para>Abilities arrive as already-normalized floats in [0, 1], projected by the composition root
    /// exactly as <c>DefensiveAgentSnapshot.PerceivedFirstTouch</c> already is. #14 does not reference
    /// the roster and must not: the reference-direction rule puts Player Database above Mechanics, and
    /// the parameter-based-physics principle says a Mechanics module receives numbers, not identities.
    /// </para>
    /// </summary>
    public readonly struct TackleDuelInputs
    {
        /// <summary>Tackler's Tackling attribute, normalized to [0, 1].</summary>
        public readonly float TacklerTackling;

        /// <summary>Tackler's Aggression, normalized to [0, 1]. Raises the foul share: a committed
        /// challenge from an aggressive player is likelier to be mistimed into contact.</summary>
        public readonly float TacklerAggression;

        /// <summary>Carrier's Dribbling, normalized to [0, 1] — his ability to keep the ball away.</summary>
        public readonly float CarrierDribbling;

        /// <summary>Carrier's Balance, normalized to [0, 1] — his ability to ride the challenge rather
        /// than be knocked off it.</summary>
        public readonly float CarrierBalance;

        /// <summary>
        /// #14 §2.2.3's approach angle (rad, [0, π]): the angle between the tackler's own velocity and
        /// his bearing to the man.
        ///
        /// <para><b>0 means running straight at him; π means running directly away.</b> It carries no
        /// information about which side of the carrier the tackler is on — see
        /// <c>TackleIntentRequest.ApproachAngle</c>, whose doc claimed otherwise until August 12, 2026.
        /// §3.6.5 uses it only for what it is: how committed the tackler's movement is to the
        /// challenge.</para>
        /// </summary>
        public readonly float ApproachAngle;

        /// <summary>Tackler-to-BALL separation (m) at the moment of the challenge, as a fraction of the
        /// contact radius, clamped to [0, 1]. 0 = on top of the ball, 1 = at the edge of reach.
        ///
        /// <para>To the BALL, not to the carrier: possession in this engine is a flag rather than a
        /// kinematic constraint, and the W2 census measured the two diverging by more than a metre in
        /// 12% of defending episodes. A tackle is a challenge for the ball.</para></summary>
        public readonly float ReachFraction;

        /// <summary>Constructs the duel inputs. Callers are responsible for the normalization; the
        /// resolver clamps defensively but does not rescale.</summary>
        public TackleDuelInputs(
            float tacklerTackling,
            float tacklerAggression,
            float carrierDribbling,
            float carrierBalance,
            float approachAngle,
            float reachFraction)
        {
            TacklerTackling = tacklerTackling;
            TacklerAggression = tacklerAggression;
            CarrierDribbling = carrierDribbling;
            CarrierBalance = carrierBalance;
            ApproachAngle = approachAngle;
            ReachFraction = reachFraction;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                            |
// | 1.0     | 2026-08-12 | —      | Initial. #14 §3.6.5 duel inputs (wiring backlog W2). Normalized  |
// |         |            |        | floats only — #14 does not reference the roster. ReachFraction   |
// |         |            |        | is measured to the BALL, not the carrier, on the census finding  |
// |         |            |        | that the two diverge >1 m in 12% of defending episodes.          |
#endregion
