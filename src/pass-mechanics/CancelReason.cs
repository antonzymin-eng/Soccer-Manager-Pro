// File:     src/pass-mechanics/CancelReason.cs
// Created:  2026-05-26
// Modified: 2026-05-27
// Author:   —
// Spec:     Pass Mechanics #5 §3.9.3, §4.6.1, Code Standards #20
// Purpose:  CancelReason enum: reason a pass was cancelled before Ball.ApplyKick().
//           PassAttemptEvent, PassCancelledEvent, and EventBusStub are in their own files.

namespace TacticalDirector.PassMechanics
{
    /// <summary>
    /// Reason a pass was cancelled before Ball.ApplyKick() was called.
    /// Pass Mechanics #5 §3.9.3, §4.6.1.
    /// </summary>
    public enum CancelReason
    {
        /// <summary>Collision System set the tackle flag during WINDUP state.</summary>
        TackleInterrupt,
        // Additional reasons reserved for Stage 2+ (e.g. StumbleInterrupt).
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                                      |
// | 1.0     | 2026-05-26 | —      | Initial implementation (combined with PassAttemptEvent et al.).            |
// | 1.1     | 2026-05-26 | —      | H4: PassAttemptEvent, PassCancelledEvent, EventBusStub split to own files. |
// |         |            |        |     File contained CancelReason only; rename was deferred.                 |
// | 1.2     | 2026-05-27 | —      | AR-1 M-3: file renamed PassEvents.cs → CancelReason.cs (FR-CS §4.1:      |
// |         |            |        |     filename must match contained type name).                              |
#endregion
