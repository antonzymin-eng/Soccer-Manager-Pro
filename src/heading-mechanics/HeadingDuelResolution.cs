// File:     src/heading-mechanics/HeadingDuelResolution.cs
// Created:  2026-05-28
// Modified: 2026-06-05
// Author:   —
// Spec:     Heading Mechanics #10 §3.7, §4.2.1, KD-8, KD-10, FR-HE-010, FR-HE-017, FR-HE-023,
//           FR-HE-026, FR-HE-027, Code Standards #20
// Purpose:  ICollisionEventConsumer implementation, per-frame buffer, and duel-score resolution.

using UnityEngine;
using Unity.Profiling;

using TacticalDirector.CollisionSystem;

namespace TacticalDirector.HeadingMechanics
{
    /// <summary>
    /// Implements ICollisionEventConsumer (Collision System #3 §3.4.2) and resolves contested-heading duels.
    /// Buffers AGENT_BALL CollisionEvents per frame (§4.2.1); reads them during duel resolution (§3.7).
    /// FM-010-005: baseScore = w_B·Balance_norm + w_S·Strength_norm + w_H·Heading_norm.
    /// Zero heap allocation on hot path: backing arrays pre-allocated at Initialize() time.
    /// Heading Mechanics #10 §3.7 / §4.2.1 / KD-8 / FR-HE-010.
    /// </summary>
    public sealed class HeadingDuelResolution : ICollisionEventConsumer
    {
        // ── Pre-allocated buffers (zero per-tick allocation) ─────────────────────────

        private readonly CollisionEvent[] _contactBuffer;
        private int _contactBufferCount;

        // Participant info indexed by duel-local participant index [0, ParticipantCount).
        private readonly int[]   _participantAgentIds;
        private readonly float[] _participantBaseScores;
        private readonly float[] _participantDisturbanceFactors;

        // Duel context buffer (up to MaxSimultaneousDuels duels per frame).
        private readonly ContestedDuelContext[] _duelBuffer;
        private readonly float[]               _duelMatchTimes;
        private int _duelCount;

        // ── Profiler Markers ─────────────────────────────────────────────────────────

        private static readonly ProfilerMarker s_resolveMarker =
            new ProfilerMarker("HeadingMechanics.DuelResolution");

        // ── Constructor ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Allocates all backing buffers. Called once at HeadingMechanics.Initialize().
        /// No allocation on the 60 Hz hot path after this point (FR-CS-066).
        /// </summary>
        public HeadingDuelResolution()
        {
            int cap         = HeadingMechanicsConstants.HeadingContactBufferCapacity;
            int maxPartic   = HeadingMechanicsConstants.MaxDuelParticipants
                            * HeadingMechanicsConstants.MaxSimultaneousDuels;
            int maxDuels    = HeadingMechanicsConstants.MaxSimultaneousDuels;

            _contactBuffer                  = new CollisionEvent[cap];
            _participantAgentIds            = new int[maxPartic];
            _participantBaseScores          = new float[maxPartic];
            _participantDisturbanceFactors  = new float[maxPartic];
            _duelBuffer                     = new ContestedDuelContext[maxDuels];
            _duelMatchTimes                 = new float[maxDuels];
        }

        // ── ICollisionEventConsumer ──────────────────────────────────────────────────

        /// <summary>
        /// Called by Collision System #3 per confirmed collision each frame.
        /// Buffers AGENT_BALL events within the current physics-frame window. §4.2.1.
        /// </summary>
        public void OnCollisionEvent(in CollisionEvent evt)
        {
            if (evt.Type != CollisionType.AGENT_BALL)
            {
                return;
            }

            if (_contactBufferCount < _contactBuffer.Length)
            {
                _contactBuffer[_contactBufferCount++] = evt;
            }
        }

        // ── Frame lifecycle ──────────────────────────────────────────────────────────

        /// <summary>Clears the per-frame contact buffer. Called at the start of each 60 Hz tick by HeadingMechanics.</summary>
        public void ClearFrameBuffer()
        {
            _contactBufferCount = 0;
            _duelCount          = 0;
        }

        // ── Duel API ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// Registers a contested-duel candidate (called once per eligible agent per contact frame).
        /// Finds or creates a duel context for the given contact frame match-time and appends the agent.
        /// Returns the duel ID for later resolution, or -1 if the duel buffer is full.
        /// </summary>
        public int RegisterDuelCandidate(
            int agentId,
            float contactFrameMatchTime,
            float baseScore)
        {
            // Find existing duel for this contact frame, or allocate a new one.
            int duelIndex = FindOrCreateDuel(contactFrameMatchTime);
            if (duelIndex < 0)
            {
                return -1;
            }

            ref ContestedDuelContext duel = ref _duelBuffer[duelIndex];
            int slot = duel.BufferStartIndex + duel.ParticipantCount;

            if (slot < _participantAgentIds.Length)
            {
                _participantAgentIds[slot]   = agentId;
                _participantBaseScores[slot] = baseScore;
                duel.ParticipantCount++;
            }

            return duel.DuelId;
        }

        /// <summary>
        /// Resolves all buffered duels. For each duel:
        /// - Finds the winner (highest base score, with near-tie RNG perturbation per FR-HE-023).
        /// - Computes disturbance factors for losers.
        /// - Populates WinnerAgentId and DisturbanceFactor entries.
        /// Returns the number of duels resolved.
        /// FM-010-005. Heading Mechanics #10 §3.7 / KD-10 / FR-HE-017 / FR-HE-023.
        /// </summary>
        public int ResolveAll(IHeadingRngService rng)
        {
            using var _ = s_resolveMarker.Auto();

            for (int d = 0; d < _duelCount; d++)
            {
                ResolveDuel(d, rng);
            }

            return _duelCount;
        }

        /// <summary>
        /// Returns the duel context at the given index. Valid for index in [0, ResolvedCount).
        /// </summary>
        public ContestedDuelContext GetDuel(int index)
        {
            return _duelBuffer[index];
        }

        /// <summary>Returns the number of duels registered for the current frame.</summary>
        public int DuelCount => _duelCount;

        /// <summary>
        /// Returns the disturbance factor for a given duel and participant slot offset.
        /// Slot offset is relative to ContestedDuelContext.BufferStartIndex.
        /// </summary>
        public float GetDisturbanceFactor(int bufferStartIndex, int participantSlot)
        {
            return _participantDisturbanceFactors[bufferStartIndex + participantSlot];
        }

        /// <summary>Returns the agent ID at a given buffer position.</summary>
        public int GetParticipantAgentId(int bufferStartIndex, int participantSlot)
        {
            return _participantAgentIds[bufferStartIndex + participantSlot];
        }

        /// <summary>
        /// Computes the base duel score for one agent from their normalised attributes.
        /// FM-010-005: baseScore = w_B·Balance_norm + w_S·Strength_norm + w_H·Heading_norm.
        /// Heading Mechanics #10 §3.7.
        /// </summary>
        public static float ComputeBaseScore(HeadingAgentAttributes attrs)
        {
            float headingNorm  = NormAttr(attrs.Heading);
            float strengthNorm = NormAttr(attrs.Strength);
            float balanceNorm  = NormAttr(attrs.Balance);

            return HeadingMechanicsConstants.DuelBalanceWeight  * balanceNorm
                 + HeadingMechanicsConstants.DuelStrengthWeight * strengthNorm
                 + HeadingMechanicsConstants.DuelHeadingWeight  * headingNorm;
        }

        // ── Private ──────────────────────────────────────────────────────────────────

        private int FindOrCreateDuel(float contactFrameMatchTime)
        {
            // Search existing duels for this match-time window using stored float match times directly.
            for (int i = 0; i < _duelCount; i++)
            {
                if (Mathf.Abs(_duelMatchTimes[i] - contactFrameMatchTime) < HeadingMechanicsConstants.DuelFrameMatchToleranceS)
                {
                    return i;
                }
            }

            // Create new duel.
            if (_duelCount >= _duelBuffer.Length)
            {
                return -1;
            }

            int newDuelIndex = _duelCount++;
            int startSlot    = newDuelIndex * HeadingMechanicsConstants.MaxDuelParticipants;

            _duelMatchTimes[newDuelIndex] = contactFrameMatchTime;
            _duelBuffer[newDuelIndex] = new ContestedDuelContext
            {
                DuelId           = Mathf.RoundToInt(contactFrameMatchTime * HeadingMechanicsConstants.MS_PER_SECOND),
                ParticipantCount = 0,
                WinnerAgentId    = ContestedDuelContext.UnresolvedWinnerId,
                BufferStartIndex = startSlot
            };

            return newDuelIndex;
        }

        private void ResolveDuel(int duelIndex, IHeadingRngService rng)
        {
            ref ContestedDuelContext duel = ref _duelBuffer[duelIndex];
            int count = duel.ParticipantCount;

            if (count < 2)
            {
                // Degenerate — mark winner as sole participant.
                if (count == 1)
                {
                    duel.WinnerAgentId = _participantAgentIds[duel.BufferStartIndex];
                    _participantDisturbanceFactors[duel.BufferStartIndex] = 0.0f;
                }
                return;
            }

            int start = duel.BufferStartIndex;

            // Step 3: near-tie tiebreak perturbation (FR-HE-023 / pass-1 H-5).
            // Find current best score.
            float bestScore = _participantBaseScores[start];
            for (int i = 1; i < count; i++)
            {
                if (_participantBaseScores[start + i] > bestScore)
                {
                    bestScore = _participantBaseScores[start + i];
                }
            }

            for (int i = 0; i < count; i++)
            {
                float gap = bestScore - _participantBaseScores[start + i];
                if (gap < HeadingMechanicsConstants.DuelTiebreakEpsilon)
                {
                    // Only near-tie participants receive perturbation.
                    _participantBaseScores[start + i] +=
                        HeadingMechanicsConstants.DuelTiebreakNoiseAmplitude
                        * rng.NextFloat(HeadingMechanicsConstants.DrawSiteDuelTiebreak);
                }
            }

            // Step 4: find winner after perturbation.
            int   winnerSlot  = 0;
            float winnerScore = _participantBaseScores[start];

            for (int i = 1; i < count; i++)
            {
                if (_participantBaseScores[start + i] > winnerScore)
                {
                    winnerScore = _participantBaseScores[start + i];
                    winnerSlot  = i;
                }
            }

            duel.WinnerAgentId = _participantAgentIds[start + winnerSlot];
            _participantDisturbanceFactors[start + winnerSlot] = 0.0f;

            // Compute loser disturbance factors.
            for (int i = 0; i < count; i++)
            {
                if (i == winnerSlot)
                {
                    continue;
                }

                float gap = winnerScore - _participantBaseScores[start + i];
                float disturbance = HeadingMechanicsConstants.DuelDisturbanceMax
                    * Clamp01(gap / HeadingMechanicsConstants.DuelDisturbanceGapSaturation);

                _participantDisturbanceFactors[start + i] = disturbance;
            }
        }

        private static float NormAttr(int attribute)
        {
            float clamped = Mathf.Clamp(attribute,
                HeadingMechanicsConstants.ATTR_MIN,
                HeadingMechanicsConstants.ATTR_MAX);
            return clamped / HeadingMechanicsConstants.ATTR_MAX;
        }

        private static float Clamp01(float v)
        {
            return v < 0.0f ? 0.0f : v > 1.0f ? 1.0f : v;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-28 | —      | Initial implementation.                                                         |
// | 1.1     | 2026-05-28 | —      | AR-1 H-2: add _duelMatchTimes[] array; FindOrCreateDuel compares stored float    |
// |         |            |        | match times directly, eliminating lossy int round-trip via DuelId.             |
// | 1.2     | 2026-05-28 | —      | AR-2 M-1: local 0.001f tolerance → DuelFrameMatchToleranceS constant.           |
// |         |            |        | AR-2 M-2: * 1000.0f literal → MS_PER_SECOND constant in DuelId assignment.      |
// | 1.3     | 2026-06-05 | —      | Collision-system AR-3 M-1 follow-through. OnCollisionEvent signature gains `in` |
// |         |            |        | parameter modifier to match the updated ICollisionEventConsumer contract.       |
// | 1.4     | 2026-06-12 | —      | Build fix (dotnet CI gate): using UnityEngine.Profiling -> Unity.Profiling.     |
// |         |            |        | ProfilerMarker's actual namespace is Unity.Profiling; the old using was CS0246  |
// |         |            |        | under Unity and the Linux compile gate alike, so this assembly could not have   |
// |         |            |        | compiled in-engine. No functional change.                                       |
#endregion
