// File:     src/decision-tree/SnapshotValidator.cs
// Created:  2026-05-29
// Modified: 2026-05-29
// Author:   —
// Spec:     Decision Tree #8 §3.6, §3.8, Code Standards #20
// Purpose:  Step 1 of the 6-step pipeline. Validates the incoming FilteredView before
//           any other pipeline step executes. Pure function with no side effects.

using UnityEngine;
using TacticalDirector.PerceptionSystem;

namespace TacticalDirector.DecisionTree
{
    /// <summary>
    /// Step 1: validates a FilteredView before pipeline execution.
    /// Returns false on fatal validation failure; the DecisionTree will skip this tick.
    /// Decision Tree #8 §3.6, §3.8.
    /// </summary>
    internal static class SnapshotValidator
    {
        /// <summary>
        /// Validates a FilteredView received from the Perception System.
        /// Returns true if the snapshot is safe to process. False if the snapshot
        /// is malformed and the pipeline should abort for this agent this tick.
        /// §3.6.1, §3.8.
        /// </summary>
        internal static bool Validate(in FilteredView snapshot, int expectedObserverId)
        {
            if (snapshot.ObserverId != expectedObserverId)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning($"[DT] Snapshot ObserverId {snapshot.ObserverId} != expected {expectedObserverId}");
#endif
                return false;
            }

            if (snapshot.FrameNumber <= 0)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning($"[DT] Snapshot FrameNumber {snapshot.FrameNumber} invalid for agent {expectedObserverId}");
#endif
                return false;
            }

            if (snapshot.VisibleTeammates == null)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning($"[DT] VisibleTeammates array null for agent {expectedObserverId}");
#endif
                return false;
            }

            if (snapshot.VisibleOpponents == null)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning($"[DT] VisibleOpponents array null for agent {expectedObserverId}");
#endif
                return false;
            }

            if (snapshot.VisibleTeammatesCount < 0 || snapshot.VisibleTeammatesCount > snapshot.VisibleTeammates.Length)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning($"[DT] VisibleTeammatesCount {snapshot.VisibleTeammatesCount} out of range for agent {expectedObserverId}");
#endif
                return false;
            }

            if (snapshot.VisibleOpponentsCount < 0 || snapshot.VisibleOpponentsCount > snapshot.VisibleOpponents.Length)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning($"[DT] VisibleOpponentsCount {snapshot.VisibleOpponentsCount} out of range for agent {expectedObserverId}");
#endif
                return false;
            }

            // INV-8: BallStalenessFrames == 0 iff BallVisible == true (from FilteredView spec §3.7.1)
            if (snapshot.BallVisible && snapshot.BallStalenessFrames != 0)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning($"[DT] INV-8 violation: BallVisible=true but BallStalenessFrames={snapshot.BallStalenessFrames} for agent {expectedObserverId}");
#endif
                // Non-fatal: log and continue; scoring penalises stale ball data naturally
            }

            return true;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-29 | —      | Initial implementation. |
// | 1.1     | 2026-06-11 | —      | Audit AR-2 M-10: all 7 diagnostic emits FR-CS-031 gated (#if UNITY_EDITOR ||  |
// |         |            |        |   DEVELOPMENT_BUILD), body-gate form — the enclosing if-bodies carry           |
// |         |            |        |   `return false` and must survive in release builds (PM AR-8 L-1 precedent).   |
#endregion
