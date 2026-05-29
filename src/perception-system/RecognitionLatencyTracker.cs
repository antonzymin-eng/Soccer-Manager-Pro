// File:     src/perception-system/RecognitionLatencyTracker.cs
// Created:  2026-05-28
// Modified: 2026-05-28
// Author:   —
// Spec:     Perception System #7 §3.3, §3.3.5, Code Standards #20
// Purpose:  Manages per-(observer, target) latency counters across heartbeats (§3.3.5).
//           Computes L_rec from Decisions attribute and deterministic hash.
//           Applies half-turn orientation bonus (§3.3.3). Tracks expiry windows (§3.3.6).
//           Not static — owns mutable counter state across heartbeats.
//           Zero heap allocation per heartbeat (INV-10); pre-allocated flat arrays used.

using UnityEngine;

namespace TacticalDirector.PerceptionSystem
{
    /// <summary>
    /// Manages recognition latency counters for all (observer, target) pairs across heartbeats.
    /// Uses pre-allocated int[MaxAgents × MaxAgents] arrays to satisfy INV-10 (no per-heartbeat alloc).
    /// Index encoding: observerId * MaxAgents + targetId.
    /// Perception System #7 §3.3.
    /// </summary>
    public sealed class RecognitionLatencyTracker
    {
        private readonly int _maxAgents;

        // _latencyCounters[obs*N+tgt]: ticks this target has been visible-but-unconfirmed.
        // -1 = entity not currently tracked (never seen or counter removed after exit).
        private readonly int[] _latencyCounters;

        // _confirmed[obs*N+tgt]: true if entity has been fully confirmed (counter >= L_rec).
        private readonly bool[] _confirmed;

        // _expiryCounters[obs*N+tgt]: countdown ticks remaining before confirmed entity is removed.
        // 0 = no expiry in progress. Set to CONFIRMATION_EXPIRY_TICKS when entity leaves visibility.
        private readonly int[] _expiryCounters;

        /// <summary>
        /// Allocates all tracking arrays at match initialisation.
        /// No allocation occurs during subsequent heartbeat calls.
        /// </summary>
        public RecognitionLatencyTracker()
        {
            _maxAgents      = PerceptionConstants.MaxAgents;
            int capacity    = _maxAgents * _maxAgents;
            _latencyCounters = new int[capacity];
            _confirmed       = new bool[capacity];
            _expiryCounters  = new int[capacity];

            // Initialise all counters to -1 (untracked state)
            for (int i = 0; i < capacity; i++)
            {
                _latencyCounters[i] = -1;
            }
        }

        /// <summary>
        /// Processes one entity that survived FoV and occlusion tests this heartbeat.
        /// Increments the latency counter and returns true if the entity is confirmed.
        /// Clears any pending expiry for this entity (it became visible again).
        /// Perception System #7 §3.3.5.
        /// </summary>
        /// <param name="observerId">Observer agent ID (0–21).</param>
        /// <param name="targetId">Target agent ID (0–21).</param>
        /// <param name="decisions">Observer's Decisions attribute [1–20].</param>
        /// <param name="isHalfTurned">True if observer is in half-turned stance.</param>
        /// <param name="angularSeparationAbsDeg">Absolute angular separation to target (degrees).</param>
        /// <param name="frameNumber">Current heartbeat tick for deterministic hash.</param>
        /// <param name="forcedRefresh">If true, counter is reset to 0 per §3.8.2 (L_rec bypass on forced refresh).</param>
        public bool ProcessVisible(
            int observerId,
            int targetId,
            int decisions,
            bool isHalfTurned,
            float angularSeparationAbsDeg,
            int frameNumber,
            bool forcedRefresh)
        {
            int key = Key(observerId, targetId);

            // Forced refresh: reset counter and confirm immediately (§3.8.2)
            if (forcedRefresh)
            {
                _latencyCounters[key] = 0;
                _confirmed[key]       = true;
                _expiryCounters[key]  = 0;
                return true;
            }

            // Clear expiry — entity re-entered visibility
            _expiryCounters[key] = 0;

            // If already confirmed: keep confirmed (entity remains visible, no counter reset)
            if (_confirmed[key])
            {
                return true;
            }

            // Initialise counter if first appearance
            if (_latencyCounters[key] < 0)
            {
                _latencyCounters[key] = 0;
            }

            _latencyCounters[key]++;

            int lRec = ComputeLRec(observerId, targetId, decisions, isHalfTurned,
                angularSeparationAbsDeg, frameNumber);

            if (_latencyCounters[key] >= lRec)
            {
                _confirmed[key] = true;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Called for each entity that is NOT visible this heartbeat (failed FoV or occlusion).
        /// Manages the expiry countdown. Once the expiry window closes, the entity is evicted.
        /// Returns true if the entity remains confirmed during the expiry window (stale data).
        /// Perception System #7 §3.3.6.
        /// </summary>
        public bool ProcessInvisible(int observerId, int targetId)
        {
            int key = Key(observerId, targetId);

            if (_confirmed[key])
            {
                if (_expiryCounters[key] <= 0)
                {
                    // Begin expiry countdown
                    _expiryCounters[key] = PerceptionConstants.CONFIRMATION_EXPIRY_TICKS;
                }
                else
                {
                    _expiryCounters[key]--;
                    if (_expiryCounters[key] <= 0)
                    {
                        // Expiry elapsed: evict entity
                        RemoveEntity(observerId, targetId);
                        return false;
                    }
                }
                return true;
            }
            else
            {
                // Not confirmed: remove latency counter immediately (§3.3.5)
                RemoveEntity(observerId, targetId);
                return false;
            }
        }

        /// <summary>
        /// Resets all tracking state for a given observer–target pair.
        /// Called when a confirmed entity's expiry elapses, or on match reset.
        /// Perception System #7 §3.3.5.
        /// </summary>
        public void RemoveEntity(int observerId, int targetId)
        {
            int key = Key(observerId, targetId);
            _latencyCounters[key] = -1;
            _confirmed[key]       = false;
            _expiryCounters[key]  = 0;
        }

        /// <summary>
        /// Resets all recognition latency state for a given observer.
        /// Called on forced refresh when the observer's full snapshot is rebuilt.
        /// Perception System #7 §3.8.2.
        /// </summary>
        public void ResetObserver(int observerId)
        {
            for (int targetId = 0; targetId < _maxAgents; targetId++)
            {
                int key = Key(observerId, targetId);
                _latencyCounters[key] = -1;
                _confirmed[key]       = false;
                _expiryCounters[key]  = 0;
            }
        }

        // ── L_rec computation ────────────────────────────────────────────────────────

        /// <summary>
        /// Computes final L_rec (integer ticks) for a given observer–target pair.
        /// Applies half-turn bonus (§3.3.3) and additive deterministic noise (§3.3.4).
        /// Perception System #7 §3.3.2–§3.3.4.
        /// </summary>
        public int ComputeLRec(
            int observerId,
            int targetId,
            int decisions,
            bool isHalfTurned,
            float angularSeparationAbsDeg,
            int frameNumber)
        {
            float dNorm = (decisions - PerceptionConstants.ATTR_MIN) / PerceptionConstants.ATTR_RANGE;
            float lRecBase = PerceptionConstants.L_MAX - dNorm * (PerceptionConstants.L_MAX - PerceptionConstants.L_MIN);
            int lRecBaseTicks = Mathf.FloorToInt(lRecBase); // §3.3.2: floor() is the required rounding

            // Half-turn peripheral bonus (§3.3.3)
            float lRecModified = lRecBaseTicks;
            if (isHalfTurned && FovCalculator.IsInPeripheralArc(angularSeparationAbsDeg))
            {
                lRecModified *= PerceptionConstants.HalfTurnLRecReduction;
            }

            int lRecTicks = Mathf.FloorToInt(lRecModified);
            lRecTicks = Mathf.Max(lRecTicks, PerceptionConstants.L_MIN);

            // Additive-only deterministic noise: 0 or +1 tick (§3.3.4)
            int noise = DeterministicHash(observerId, targetId, frameNumber) % 2;
            int lRecFinal = Mathf.Min(lRecTicks + noise, PerceptionConstants.L_MAX);

            return lRecFinal;
        }

        // ── Deterministic hash ───────────────────────────────────────────────────────

        /// <summary>
        /// Non-cryptographic integer hash of three inputs. Consistent with project-wide determinism
        /// (no System.Random). Produces varied but reproducible values per (observer, target, frame).
        /// Perception System #7 §3.3.4 / KD-4.
        /// </summary>
        public static int DeterministicHash(int a, int b, int c)
        {
            // Wang/Jenkins integer hash — avalanche property, no allocations
            unchecked // deliberate integer wrap-around; not an overflow bug
            {
                int h = a * 2246822519 + b * 2654435761 + c * 1013904223;
                h ^= h >> 16;
                h *= unchecked((int)0x45d9f3b);
                h ^= h >> 16;
                return Mathf.Abs(h);
            }
        }

        private int Key(int observerId, int targetId)
        {
            return observerId * _maxAgents + targetId;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-28 | —      | Initial implementation.                                                  |
// | 1.1     | 2026-05-28 | —      | AR-1 fix L-6: ProcessVisible uses Key() helper (style consistency fix). |
#endregion
