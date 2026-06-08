// File:     src/agent-movement/OscillationGuard.cs
// Created:  2026-05-25
// Modified: 2026-06-07 (AR-11 fix pass)
// Author:   —
// Spec:     Agent Movement #2 §3.1.7, Code Standards #20
// Purpose:  Ring-buffer guard that detects rapid state oscillation and enforces a lock-out period.

using UnityEngine;

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
                // Reset the ring buffer on lock entry. Without this, pre-lock timestamps remain
                // within WindowSeconds after unlock and the guard re-locks on the next transition,
                // producing indefinite lockout when underlying inputs still favour the same flap.
                _t0 = _t1 = _t2 = _t3 = _t4 = _t5 = _t6 = _t7 = float.NegativeInfinity;
                _writeIndex = 0;
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
                // AR-11 L-2: assert on out-of-range write so a future BufferSize bump
                // beyond the 8 hardcoded slots fails fast in dev builds rather than
                // silently dropping writes (ReadTime's default arm already returns
                // NegativeInfinity, so without this the asymmetry would corrupt
                // recent-transition counting).
                default:
                    Debug.Assert(false,
                        "OscillationGuard.WriteTime: index out of range — BufferSize bumped beyond hardcoded 8 slots without updating switch arms.");
                    break;
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
                default: return float.NegativeInfinity;
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
// | 1.2     | 2026-05-26 | —      | AR-2 fix: L-1 ReadTime default return changed from 0.0f to float.NegativeInfinity for          |
// |         |            |        | consistency with Initialize() sentinel; prevents false recent-transition count if                |
// |         |            |        | BufferSize is increased without updating the switch cases.                                     |
// | 1.3     | 2026-06-03 | —      | AR-4 fix: M-2 ring buffer reset to NegativeInfinity on lock entry. Closes the indefinite-      |
// |         |            |        | lockout corner case where pre-lock timestamps remained within WindowSeconds after the          |
// |         |            |        | LockDuration expired and the guard re-locked on the very next transition.                      |
// | 1.4     | 2026-06-07 | —      | AR-11 fix: L-2 WriteTime gains a Debug.Assert(false) default arm parallel to ReadTime's       |
// |         |            |        | NegativeInfinity default. A future BufferSize bump beyond the 8 hardcoded switch arms          |
// |         |            |        | would silently drop writes while ReadTime treats unreached slots as -Infinity, corrupting      |
// |         |            |        | the recent-transition count. Fails fast in dev builds instead. `using UnityEngine;` added.    |
#endregion
