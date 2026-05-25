// File:     src/agent-movement/OscillationGuard.cs
// Created:  2026-05-25
// Modified: 2026-05-25
// Author:   —
// Spec:     Agent Movement #2 §3.1.7, Code Standards #20
// Purpose:  Ring-buffer guard that detects rapid state oscillation and enforces a lock-out period.

namespace TacticalDirector.AgentMovement
{
    /// <summary>
    /// Detects state machine oscillation and locks state for a cooldown period.
    /// Uses a fixed-size ring buffer — no heap allocations after initialisation.
    /// Agent Movement #2 §3.1.7.
    ///
    /// IMPORTANT: Always pass by ref. Call Initialize() before first use (via AgentState.CreateAtPosition).
    /// Determinism note: currentTime passed to RecordAndCheck MUST come from MatchClock
    /// (Spec #16), never from Time.time or DateTime.Now (FR-CS-042).
    /// </summary>
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public struct OscillationGuard
    {
        private float _t0, _t1, _t2, _t3, _t4, _t5, _t6, _t7;
        private int _writeIndex;
        private bool _isLocked;
        private float _lockUntilTime;

        /// <summary>
        /// Initialises all timestamp slots to a sentinel predating any match time.
        /// Must be called from AgentState.CreateAtPosition — C# struct zero-init sets all fields
        /// to 0.0f, which causes false-positive lockout at match time t=0 (all 8 slots appear recent).
        /// </summary>
        public void Initialize()
        {
            _t0 = _t1 = _t2 = _t3 = _t4 = _t5 = _t6 = _t7 = float.NegativeInfinity;
            _writeIndex = 0;
            _isLocked = false;
            _lockUntilTime = float.NegativeInfinity;
        }

        /// <summary>
        /// Records a transition and returns true if the transition should be BLOCKED.
        /// currentTime must come from MatchClock (Spec #16 §3.2.3) — not Time.time.
        /// Agent Movement #2 §3.1.7.
        /// </summary>
        public bool RecordAndCheck(float currentTime)
        {
            if (_isLocked && currentTime < _lockUntilTime)
            {
                return true;
            }

            _isLocked = false;

            WriteTime(_writeIndex, currentTime);
            _writeIndex = (_writeIndex + 1) % OscillationGuardConstants.BufferSize;

            int recentCount = 0;
            for (int i = 0; i < OscillationGuardConstants.BufferSize; i++)
            {
                if (currentTime - ReadTime(i) < OscillationGuardConstants.WindowSeconds)
                {
                    recentCount++;
                }
            }

            if (recentCount > MovementThresholds.MaxTransitionsPerSecond)
            {
                _isLocked = true;
                _lockUntilTime = currentTime + OscillationGuardConstants.LockDuration;
                return true;
            }

            return false;
        }

        private void WriteTime(int index, float value)
        {
            switch (index)
            {
                case 0: _t0 = value; break;
                case 1: _t1 = value; break;
                case 2: _t2 = value; break;
                case 3: _t3 = value; break;
                case 4: _t4 = value; break;
                case 5: _t5 = value; break;
                case 6: _t6 = value; break;
                case 7: _t7 = value; break;
            }
        }

        private float ReadTime(int index)
        {
            switch (index)
            {
                case 0: return _t0;
                case 1: return _t1;
                case 2: return _t2;
                case 3: return _t3;
                case 4: return _t4;
                case 5: return _t5;
                case 6: return _t6;
                case 7: return _t7;
                default: return 0.0f;
            }
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                                                              |
// | 1.0     | 2026-05-25 | —      | Extracted from AgentStateMachine.cs (was M-1 violation: two public types in one file).             |
// |         |            |        | H-4: BufferSize/LockDuration/WindowSeconds moved to OscillationGuardConstants in constants file.   |
// |         |            |        | M-8: MatchClock determinism requirement documented in XML doc and method summary.                   |
// | 1.1     | 2026-05-25 | —      | Pass-4 fix: H-4 Initialize() method added; [StructLayout(Sequential)] added (L-5).                |
#endregion
