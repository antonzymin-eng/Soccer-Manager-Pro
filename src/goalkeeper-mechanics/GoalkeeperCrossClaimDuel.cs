// File:     src/goalkeeper-mechanics/GoalkeeperCrossClaimDuel.cs
// Created:  2026-05-28
// Modified: 2026-05-28
// Author:   —
// Spec:     Goalkeeper Mechanics #11 §3.6, KD-14, Code Standards #20
// Purpose:  Body-part determination (§3.6.1) and hand-contact duel resolution (§3.6.3).
//           Accepts pre-allocated participant buffers; zero heap allocation on hot path.

using UnityEngine;
using Unity.Profiling;

namespace TacticalDirector.GoalkeeperMechanics
{
    /// <summary>
    /// Cross-claim and aerial duel resolution per §3.6 / KD-14.
    /// Mirrors Heading #10 HeadingDuelResolution for algorithm consistency.
    /// Body-part determination uses physical geometry (capsule/sphere intersection), not intent (FR-GK-022).
    /// Participants are iterated in #16 §3.2 entity order (deterministic).
    /// Zero heap allocation: caller pre-allocates all participant buffers.
    /// Goalkeeper Mechanics #11 §3.6.
    /// </summary>
    public sealed class GoalkeeperCrossClaimDuel
    {
        // ── Pre-allocated participant buffers ────────────────────────────────────────

        private readonly int[]           _participantAgentIds;
        private readonly float[]         _participantBaseScores;
        private readonly float[]         _participantDisturbanceFactors;
        private readonly BodyPartEnum[]  _participantBodyParts;

        // ── Duel context buffers ─────────────────────────────────────────────────────

        private readonly CrossClaimDuelContext[] _duelBuffer;
        private int _duelCount;

        // ── Profiler Markers ─────────────────────────────────────────────────────────

        private static readonly ProfilerMarker s_resolveMarker =
            new ProfilerMarker("GoalkeeperMechanics.CrossClaimDuel");

        // ── Constructor ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Allocates all backing buffers. Called once at GoalkeeperMechanics construction.
        /// No allocation occurs on the 60 Hz hot path after this point (FR-CS-066).
        /// </summary>
        public GoalkeeperCrossClaimDuel()
        {
            int maxPartic = GoalkeeperConstants.MaxCrossClaimParticipants;

            _participantAgentIds           = new int[maxPartic];
            _participantBaseScores         = new float[maxPartic];
            _participantDisturbanceFactors = new float[maxPartic];
            _participantBodyParts          = new BodyPartEnum[maxPartic];
            _duelBuffer                    = new CrossClaimDuelContext[1]; // one duel per frame per §3.6
            _duelCount                     = 0;
        }

        // ── Frame lifecycle ──────────────────────────────────────────────────────────

        /// <summary>Clears per-frame participant and duel buffers. Called at the start of each 60 Hz tick.</summary>
        public void ClearFrameBuffer()
        {
            _duelCount = 0;
        }

        /// <summary>Returns the number of duels buffered for the current frame.</summary>
        public int DuelCount => _duelCount;

        // ── Body-part determination (§3.6.1) ────────────────────────────────────────

        /// <summary>
        /// Determines the contact body part for one agent using capsule/sphere intersection priority.
        /// Priority: both hit → closest Z to ball centre wins; hand only → Hand; head only → Head; none → treated as Body.
        /// FR-GK-022: body part determined by physical geometry, NOT intent.
        /// §3.6.1. Goalkeeper Mechanics #11 §3.6.
        /// </summary>
        /// <param name="ballPosition">Ball world-space position. §3.6.1.</param>
        /// <param name="handCapsuleCenter">Centre of the agent's hand capsule collider. §3.6.1.</param>
        /// <param name="headSphereCenter">Centre of the agent's head sphere collider. §3.6.1.</param>
        /// <param name="handCapsuleRadius">Radius of the hand capsule (m). §3.6.1.</param>
        /// <param name="headSphereRadius">Radius of the head sphere (m). §3.6.1.</param>
        /// <returns>BodyPartEnum: Hand if hand capsule intersects ball; Head if head sphere intersects; Body if neither.</returns>
        public static BodyPartEnum DetermineBodyPart(
            Vector3 ballPosition,
            Vector3 handCapsuleCenter,
            Vector3 headSphereCenter,
            float handCapsuleRadius,
            float headSphereRadius)
        {
            float handDistSq = (ballPosition - handCapsuleCenter).sqrMagnitude;
            float headDistSq = (ballPosition - headSphereCenter).sqrMagnitude;

            bool handHit = handDistSq <= handCapsuleRadius * handCapsuleRadius;
            bool headHit = headDistSq <= headSphereRadius  * headSphereRadius;

            if (handHit && headHit)
            {
                // Priority: closest Z to ball centre
                float handZDist = Mathf.Abs(ballPosition.z - handCapsuleCenter.z);
                float headZDist = Mathf.Abs(ballPosition.z - headSphereCenter.z);
                return handZDist < headZDist ? BodyPartEnum.Hand : BodyPartEnum.Head;
            }

            if (handHit) return BodyPartEnum.Hand;
            if (headHit) return BodyPartEnum.Head;
            return BodyPartEnum.Body;
        }

        // ── Duel registration ────────────────────────────────────────────────────────

        /// <summary>
        /// Registers a participant in the current-frame duel.
        /// Creates the duel context if this is the first participant.
        /// Returns false if the participant buffer is full.
        /// Participants must be registered in #16 §3.2 entity order. §3.6.3.
        /// </summary>
        public bool RegisterParticipant(
            int agentId,
            GoalkeeperAgentAttributes attrs,
            BodyPartEnum bodyPart,
            int currentFrame)
        {
            if (_duelCount == 0)
            {
                _duelBuffer[0] = new CrossClaimDuelContext
                {
                    DuelId           = currentFrame,
                    ParticipantCount = 0,
                    WinnerAgentId    = CrossClaimDuelContext.UnresolvedWinnerId,
                    ContactBodyPart  = bodyPart,
                    BufferStartIndex = 0
                };
                _duelCount = 1;
            }

            ref CrossClaimDuelContext duel = ref _duelBuffer[0];
            int slot = duel.ParticipantCount;

            if (slot >= _participantAgentIds.Length)
            {
                return false;
            }

            _participantAgentIds[slot]   = agentId;
            _participantBaseScores[slot] = ComputeBaseScore(attrs);
            _participantBodyParts[slot]  = bodyPart;
            duel.ParticipantCount++;
            return true;
        }

        // ── Duel resolution (§3.6.3) ─────────────────────────────────────────────────

        /// <summary>
        /// Resolves the current-frame duel: ranks participants, applies near-tie tiebreak,
        /// sets WinnerAgentId, and sets disturbance factors.
        /// Participants are iterated in registration order (caller must register in #16 §3.2 entity order).
        /// §3.6.3. Goalkeeper Mechanics #11 §3.6.
        /// </summary>
        /// <param name="gaussianSample">Pre-drawn Gaussian sample for near-tie tiebreak (draw-site: CROSS_CLAIM_TIEBREAK). §3.6.3 / KD-7.</param>
        public void ResolveHandContactDuel(float gaussianSample)
        {
            using var _ = s_resolveMarker.Auto();

            if (_duelCount == 0)
            {
                return;
            }

            ref CrossClaimDuelContext duel = ref _duelBuffer[0];
            int count = duel.ParticipantCount;

            if (count < 2)
            {
                if (count == 1)
                {
                    duel.WinnerAgentId = _participantAgentIds[0];
                    _participantDisturbanceFactors[0] = 0.0f;
                }
                return;
            }

            // Near-tie tiebreak (§3.6.3): applied ONLY when top-2 scores are close
            float topScore    = _participantBaseScores[0];
            int   topSlot     = 0;
            float secondScore = float.MinValue;

            for (int i = 1; i < count; i++)
            {
                if (_participantBaseScores[i] > topScore)
                {
                    secondScore = topScore;
                    topScore    = _participantBaseScores[i];
                    topSlot     = i;
                }
                else if (_participantBaseScores[i] > secondScore)
                {
                    secondScore = _participantBaseScores[i];
                }
            }

            float gap = topScore - secondScore;
            if (gap < GoalkeeperConstants.CrossClaimTiebreakEpsilon)
            {
                float perturbation = GoalkeeperConstants.CrossClaimTiebreakNoiseAmplitude * gaussianSample;
                _participantBaseScores[topSlot] += perturbation;
                // Re-find winner after perturbation
                topScore = float.MinValue;
                topSlot  = 0;
                for (int i = 0; i < count; i++)
                {
                    if (_participantBaseScores[i] > topScore)
                    {
                        topScore = _participantBaseScores[i];
                        topSlot  = i;
                    }
                }
            }

            duel.WinnerAgentId = _participantAgentIds[topSlot];
            _participantDisturbanceFactors[topSlot] = 0.0f;

            // Update ContactBodyPart on the duel context to reflect winner's body part
            duel.ContactBodyPart = _participantBodyParts[topSlot];

            for (int i = 0; i < count; i++)
            {
                if (i == topSlot)
                {
                    continue;
                }

                _participantDisturbanceFactors[i] = 0.0f; // losers in cross-claim: no disturbance factor (DisturbedInDuel FailureCause emitted instead)
            }
        }

        /// <summary>Returns the duel context at the given index. Valid for [0, DuelCount).</summary>
        public CrossClaimDuelContext GetDuel(int index)
        {
            return _duelBuffer[index];
        }

        /// <summary>Returns the agent ID at a given participant slot.</summary>
        public int GetParticipantAgentId(int slot)
        {
            return _participantAgentIds[slot];
        }

        /// <summary>Returns the body part for a given participant slot.</summary>
        public BodyPartEnum GetParticipantBodyPart(int slot)
        {
            return _participantBodyParts[slot];
        }

        // ── Duel score ───────────────────────────────────────────────────────────────

        /// <summary>
        /// Computes the base duel score for cross-claim / aerial contest.
        /// Formula: CROSS_CLAIM_DUEL_BALANCE_W × Balance_norm
        ///        + CROSS_CLAIM_DUEL_STRENGTH_W × Strength_norm
        ///        + CROSS_CLAIM_DUEL_AERIAL_W × Aerial_norm.
        /// Weights sum to 1.0. §3.6.3. Goalkeeper Mechanics #11 §3.6.
        /// </summary>
        public static float ComputeBaseScore(GoalkeeperAgentAttributes attrs)
        {
            return GoalkeeperConstants.CrossClaimDuelBalanceW  * attrs.BalanceNorm
                 + GoalkeeperConstants.CrossClaimDuelStrengthW * attrs.StrengthNorm
                 + GoalkeeperConstants.CrossClaimDuelAerialW   * attrs.AerialNorm;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-28 | —      | Initial implementation. |
// | 1.1     | 2026-06-12 | —      | Build fix (dotnet CI    |
// |         |            |        | gate): using            |
// |         |            |        | UnityEngine.Profiling   |
// |         |            |        | -> Unity.Profiling.     |
// |         |            |        | ProfilerMarker's actual |
// |         |            |        | namespace is            |
// |         |            |        | Unity.Profiling; the    |
// |         |            |        | old using was CS0246    |
// |         |            |        | under Unity and the     |
// |         |            |        | Linux compile gate      |
// |         |            |        | alike, so this assembly |
// |         |            |        | could not have compiled |
// |         |            |        | in-engine. No           |
// |         |            |        | functional change.      |
#endregion
