// File:     src/pass-mechanics/PassEvents.cs
// Created:  2026-05-26
// Modified: 2026-05-26
// Author:   —
// Spec:     Pass Mechanics #5 §3.9.3, §4.6.1, Code Standards #20
// Purpose:  CancelReason enum: reason a pass was cancelled before Ball.ApplyKick().
//           Note: filename should be CancelReason.cs per FR-CS naming rule; rename
//           when tooling supports it. PassAttemptEvent, PassCancelledEvent, and
//           EventBusStub were split to their own files (H4 fix).

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
// |         |            |        |     File now contains CancelReason only. TODO: rename to CancelReason.cs. |
#endregion
