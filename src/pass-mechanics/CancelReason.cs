// File:     src/pass-mechanics/CancelReason.cs
// Created:  2026-05-26
// Modified: 2026-06-06
// Author:   —
// Spec:     Pass Mechanics #5 §3.9.3, §4.6.1, Code Standards #20
// Purpose:  CancelReason enum: reason a pass was cancelled before Ball.ApplyKick().
//           PassAttemptEvent, PassCancelledEvent, and EventBusStub are in their own files.

namespace TacticalDirector.PassMechanics
{
    /// <summary>
    /// Reason a pass was cancelled before Ball.ApplyKick() was called.
    /// Pass Mechanics #5 §3.9.3, §4.6.1.
    ///
    /// ORDINAL STABILITY: the enum's backing int ordinal lands in PassCancelledEvent
    /// payloads (Tier A digest input per FR-DS-009). New members MUST be APPENDED —
    /// never inserted in the middle — to preserve replay/save/analytics compatibility.
    /// </summary>
    public enum CancelReason
    {
        /// <summary>Collision System set the tackle flag during WINDUP state (§3.8.5).</summary>
        TackleInterrupt = 0,

        /// <summary>
        /// FM-08: Possession was lost between WINDUP and CONTACT (§4.2.4). Possessor
        /// changed between the INITIATING possession check and the CONTACT-time re-check.
        /// </summary>
        PossessionLost = 1,

        /// <summary>
        /// FM-04: Final velocity contained NaN at CONTACT. ApplyKick is suppressed; the
        /// ball state is unchanged. Reports the silent-failure path that previously had
        /// no event surface (AR-2 H-2 fix).
        /// </summary>
        InvalidVelocity = 2,

        // Additional reasons reserved for Stage 2+ (e.g. StumbleInterrupt — APPEND only).
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                                      |
// | 1.0     | 2026-05-26 | —      | Initial implementation (combined with PassAttemptEvent et al.).            |
// | 1.1     | 2026-05-26 | —      | H4: PassAttemptEvent, PassCancelledEvent, EventBusStub split to own files. |
// |         |            |        |     File contained CancelReason only; rename was deferred.                 |
// | 1.2     | 2026-05-27 | —      | AR-1 M-3: file renamed PassEvents.cs → CancelReason.cs (FR-CS §4.1:        |
// |         |            |        |     filename must match contained type name).                              |
// | 1.3     | 2026-06-06 | —      | AR-2 H-1/H-2: PossessionLost (FM-08) and InvalidVelocity (FM-04) members   |
// |         |            |        |     appended so the executor's silent-cancel paths emit telemetry.         |
// |         |            |        |     Explicit ordinals + ORDINAL STABILITY paragraph added (Tier A digest). |
// | 1.4     | 2026-06-06 | —      | AR-3 L-2: ORDINAL STABILITY paragraph tightened — the enum's backing int   |
// |         |            |        |     ordinal (not the member name) is what lands in the digest.             |
#endregion
