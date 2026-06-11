// File:     src/pass-mechanics/PassOutcome.cs
// Created:  2026-05-26
// Modified: 2026-06-11
// Author:   —
// Spec:     Pass Mechanics #5 §2.4.2, Code Standards #20
// Purpose:  PassOutcome enum: result classification of a pass execution attempt.

namespace TacticalDirector.PassMechanics
{
    /// <summary>
    /// Outcome of a pass execution attempt. Pass Mechanics #5 §2.4.2.
    /// </summary>
    public enum PassOutcome
    {
        /// <summary>
        /// Default (zero-init) value. No pass has been attempted on this result.
        /// Check IsIdle before reading LastResult — LastResult.Outcome is only
        /// meaningful after a pass cycle completes.
        /// </summary>
        None = 0,

        /// <summary>
        /// Execute() accepted the request; windup has begun. Ball has NOT been kicked yet.
        /// Poll IsIdle and read LastResult to obtain the final outcome.
        /// </summary>
        Initiated,

        /// <summary>Ball.ApplyKick() was called; ball is in flight.</summary>
        Completed,

        /// <summary>
        /// Pass cancelled before Ball.ApplyKick(); ball not kicked. Causes (§3.9.3):
        /// WINDUP tackle interrupt (CancelReason.TackleInterrupt), CONTACT-time
        /// possession loss (PossessionLost / FM-08), CONTACT-time NaN velocity
        /// (InvalidVelocity / FM-04). PassCancelledEvent carries the reason.
        /// </summary>
        Cancelled,

        /// <summary>PassRequest failed validation (programming error in caller; logged only).</summary>
        Invalid
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                             |
// | 1.0     | 2026-05-26 | —      | Extracted from PassType.cs per one-type-per-file rule (H3).       |
// | 1.1     | 2026-05-27 | —      | AR-1 H-2: Added Initiated for Execute() sentinel (Completed        |
// |         |            |        |     was semantically wrong — ball not kicked at Execute return). |
// | 1.2     | 2026-05-27 | —      | AR-1 round-2 L-B: None=0 added as explicit default so zero-init   |
// |         |            |        |     PassResult.Outcome does not claim Initiated.                 |
// | 1.3     | 2026-06-11 | —      | AR-9 L-4 (doc-only): Cancelled doc updated for the AR-2 H-1/H-2   |
// |         |            |        |     surface — covers PossessionLost (FM-08) and InvalidVelocity   |
// |         |            |        |     (FM-04) CONTACT-time cancels, not just the WINDUP tackle.     |
#endregion
