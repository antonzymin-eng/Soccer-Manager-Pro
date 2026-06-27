// File:     src/perception-system/ShoulderCheckScheduler.cs
// Created:  2026-05-28
// Modified: 2026-06-27 (Match Engine Phase D D4: CaptureState snapshot seam)
// Author:   —
// Spec:     Perception System #7 §3.4, Code Standards #20
// Purpose:  Manages autonomous shoulder check scheduling per agent (§3.4.2).
//           Computes CheckInterval_ticks from Anticipation attribute.
//           Applies deterministic jitter to prevent synchronised mass-checks (§3.4.2).
//           Doubles interval when agent is in possession state (§3.4.2 possession modifier).
//           Manages window open/close and BlindSideWindowExpiry timestamps (§3.4.3).
//           Populates ShoulderCheckAnimData stub (§3.4.4) for Stage 1 animation.
//           Not static — owns mutable scheduler state across heartbeats.
//           Zero heap allocation per heartbeat (INV-10); pre-allocated flat arrays used.

using UnityEngine;

namespace TacticalDirector.PerceptionSystem
{
    /// <summary>
    /// Per-agent shoulder check scheduler. Tracks next-check-frame and window-expiry for all 22 agents.
    /// Triggers fire when the current heartbeat tick reaches an agent's next-check-frame.
    /// Perception System #7 §3.4.
    /// </summary>
    public sealed class ShoulderCheckScheduler
    {
        private readonly int    _maxAgents;
        private readonly int[]  _nextCheckFrame;
        private readonly int[]  _windowExpiryFrame;
        private readonly bool[] _windowActive;
        private readonly ShoulderCheckAnimData[] _animData;

        // Blind-side latency counters for entities seen during shoulder check windows.
        // Index: observerId * MaxAgents + targetId
        private readonly int[]  _blindSideLatency;
        private readonly bool[] _blindSideConfirmed;

        /// <summary>Allocates all scheduler arrays at match initialisation. No allocation during heartbeat calls.</summary>
        public ShoulderCheckScheduler()
        {
            _maxAgents         = PerceptionConstants.MaxAgents;
            _nextCheckFrame    = new int[_maxAgents];
            _windowExpiryFrame = new int[_maxAgents];
            _windowActive      = new bool[_maxAgents];
            _animData          = new ShoulderCheckAnimData[_maxAgents];

            int pairCapacity = _maxAgents * _maxAgents;
            _blindSideLatency   = new int[pairCapacity];
            _blindSideConfirmed = new bool[pairCapacity];

            // Stagger initial check frames across agents to prevent mass synchronisation
            for (int i = 0; i < _maxAgents; i++)
            {
                // Use agent index as seed for spread across [0, CHECK_MAX_TICKS)
                _nextCheckFrame[i] = (i * 3 + 7) % PerceptionConstants.CHECK_MAX_TICKS;
            }

            for (int i = 0; i < pairCapacity; i++)
            {
                _blindSideLatency[i] = -1;
            }
        }

        /// <summary>True if a shoulder check window is currently active for the given agent. §3.4.3.</summary>
        public bool IsWindowActive(int agentId) => _windowActive[agentId];

        /// <summary>Heartbeat tick at which the current window expires. Meaningless if IsWindowActive = false. §3.4.3.</summary>
        public int GetWindowExpiryFrame(int agentId) => _windowExpiryFrame[agentId];

        /// <summary>Last shoulder check animation stub for the given agent. §3.4.4.</summary>
        public ShoulderCheckAnimData GetAnimData(int agentId) => _animData[agentId];

        /// <summary>
        /// Updates shoulder check scheduling for one agent this heartbeat.
        /// Fires a check if current frame has reached the scheduled trigger frame.
        /// Manages window open/close state. Returns true if a NEW check fired this tick.
        /// Perception System #7 §3.4.2–§3.4.3.
        /// </summary>
        /// <param name="agentId">Agent ID (0–21).</param>
        /// <param name="anticipation">Anticipation attribute [1–20].</param>
        /// <param name="hasPossession">True if this agent currently has ball possession.</param>
        /// <param name="currentFrame">Current heartbeat tick.</param>
        public bool UpdateAgent(int agentId, int anticipation, bool hasPossession, int currentFrame)
        {
            // Close expired window. GetWindowExpiryFrame is the LAST active tick (inclusive):
            // a window fired at F with duration D is active on F .. F+D and closes at F+D+1
            // (locked by SC-002). Do not tighten to `>=` — that contradicts the diagnostic
            // contract and the published window semantics.
            if (_windowActive[agentId] && currentFrame > _windowExpiryFrame[agentId])
            {
                CloseWindow(agentId);
            }

            // Check if scheduled fire time has been reached
            if (currentFrame < _nextCheckFrame[agentId])
            {
                return false;
            }

            // Fire shoulder check
            FireCheck(agentId, anticipation, hasPossession, currentFrame);
            return true;
        }

        /// <summary>
        /// Processes one agent ID that was found in the blind-side arc during an active window.
        /// Returns true if the entity is confirmed (latency counter satisfied).
        /// Perception System #7 §3.4.3.
        /// </summary>
        public bool ProcessBlindSideEntity(
            int observerId,
            int targetId,
            int decisions,
            bool isHalfTurned,
            float angularSeparationAbsDeg,
            int frameNumber,
            RecognitionLatencyTracker latencyTracker)
        {
            int key = observerId * _maxAgents + targetId;

            if (_blindSideConfirmed[key])
            {
                return true;
            }

            if (_blindSideLatency[key] < 0)
            {
                _blindSideLatency[key] = 0;
            }

            _blindSideLatency[key]++;

            int lRec = latencyTracker.ComputeLRec(
                observerId, targetId, decisions, isHalfTurned, angularSeparationAbsDeg, frameNumber);

            if (_blindSideLatency[key] >= lRec)
            {
                _blindSideConfirmed[key]              = true;
                _animData[observerId].AnyEntityConfirmed = true;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Read-only query: true if the (observer, target) pair has been confirmed in the
        /// current blind-side window. Used by the forced mid-heartbeat refresh path (§4.6.2),
        /// which rebuilds the view WITHOUT advancing the blind-side latency counter. No side effects.
        /// Perception System #7 §4.6.2.
        /// </summary>
        public bool IsBlindSideConfirmed(int observerId, int targetId)
        {
            return _blindSideConfirmed[observerId * _maxAgents + targetId];
        }

        /// <summary>
        /// Clears blind-side latency state for all targets seen by an observer.
        /// Called when a window closes (unconfirmed entries are discarded). §3.4.3.
        /// </summary>
        public void ClearBlindSideState(int observerId)
        {
            for (int targetId = 0; targetId < _maxAgents; targetId++)
            {
                int key = observerId * _maxAgents + targetId;
                _blindSideLatency[key]   = -1;
                _blindSideConfirmed[key] = false;
            }
        }

        /// <summary>
        /// Snapshot seam: returns a read-only view over the per-agent scheduling arrays and per-pair
        /// blind-side counters (§3.4) so a host snapshot layer can serialize them canonically for
        /// deterministic save/restore. The arrays are the live, allocated-once instances (read-only
        /// serialization use only).
        /// </summary>
        public ShoulderCheckState CaptureState() =>
            new ShoulderCheckState(
                _nextCheckFrame, _windowExpiryFrame, _windowActive, _animData,
                _blindSideLatency, _blindSideConfirmed);

        // ── Private helpers ──────────────────────────────────────────────────────────

        private void FireCheck(int agentId, int anticipation, bool hasPossession, int currentFrame)
        {
            _windowActive[agentId]      = true;
            _windowExpiryFrame[agentId] = currentFrame + PerceptionConstants.SHOULDER_CHECK_DURATION;

            // Compute next check frame
            float dNorm = (anticipation - PerceptionConstants.ATTR_MIN) / PerceptionConstants.ATTR_RANGE;
            float interval = PerceptionConstants.CHECK_MAX_TICKS
                - dNorm * (PerceptionConstants.CHECK_MAX_TICKS - PerceptionConstants.CHECK_MIN_TICKS);

            if (hasPossession)
            {
                // Doubles check interval when in possession = halves check frequency (§3.4.2)
                interval *= PerceptionConstants.PossessionCheckIntervalMultiplier;
            }

            int jitter = (RecognitionLatencyTracker.DeterministicHash(agentId, currentFrame, 0)
                % (2 * PerceptionConstants.ShoulderCheckJitterRange + 1))
                - PerceptionConstants.ShoulderCheckJitterRange;

            int nextFrame = currentFrame + Mathf.RoundToInt(interval) + jitter;
            _nextCheckFrame[agentId] = Mathf.Max(nextFrame, currentFrame + 1);

            // Populate anim stub (§3.4.4)
            _animData[agentId] = new ShoulderCheckAnimData
            {
                AgentId          = agentId,
                FireFrame        = currentFrame,
                CheckDirection   = 0.0f, // Stage 0: direction not yet computed; Stage 1+ refines
                AnyEntityConfirmed = false
            };
        }

        private void CloseWindow(int agentId)
        {
            _windowActive[agentId] = false;
            ClearBlindSideState(agentId);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-28 | —      | Initial implementation.                                                                                       |
// | 1.1     | 2026-05-28 | —      | AR-1 fixes: M-2 AnyEntityConfirmed updated on blind-side confirmation; L-5 possession comment clarified.      |
// | 1.2     | 2026-06-13 | —      | AR-3 fixes: H-1 added read-only IsBlindSideConfirmed() for the forced-refresh path (no counter advance); L-2 magic 2.0f possession multiplier → PerceptionConstants.PossessionCheckIntervalMultiplier (FR-CS-016). NOTE: the AR-3 L-3 window-close `>`→`>=` change was REVERTED before merge — SC-002 locks GetWindowExpiryFrame as the last active tick (inclusive), so the window is active through expiry by design; comment added to prevent re-tightening. |
// | 1.3     | 2026-06-27 | —      | Match Engine Phase D D4 follow-up: CaptureState() snapshot seam returns a ShoulderCheckState view over the per-agent scheduling arrays + per-pair blind-side counters for the host snapshot layer. Read-only; no behaviour change. |
#endregion
