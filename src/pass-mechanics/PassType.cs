// File:     src/pass-mechanics/PassType.cs
// Created:  2026-05-26
// Modified: 2026-06-08
// Author:   —
// Spec:     Pass Mechanics #5 §3.1.2, Code Standards #20
// Purpose:  PassType enum: discrete pass type classification supplied by Decision Tree #8.

namespace TacticalDirector.PassMechanics
{
    /// <summary>
    /// Discrete pass type classification. Pass Mechanics #5 §3.1.2.
    /// Selected by Decision Tree #8; Pass Mechanics does not choose the type (KD-2).
    ///
    /// ORDINAL STABILITY: the enum's backing int ordinal is consumed in TWO digest-
    /// critical surfaces and MUST be APPEND-only:
    ///  1) Payload of both PassAttemptEvent (Tier A 0x0C) and PassCancelledEvent
    ///     (Tier A 0x0D) — FR-DS-009 digest input.
    ///  2) Third hash input to PassErrorCalculator.ComputeErrorDirection
    ///     (`(int)_request.PassType` in PassExecutor.ExecuteContact). Reordering would
    ///     change the deterministic error direction for every logical pass and break
    ///     replay parity even before the event digest catches the drift.
    /// New members MUST be APPENDED — never inserted in the middle. Sibling enums
    /// CancelReason and CrossSubType share the payload contract (digest only); PassType
    /// additionally carries the hash-input contract.
    /// </summary>
    public enum PassType
    {
        /// <summary>Short-to-medium range, surface-rolling. distMax 30m.</summary>
        Ground,

        /// <summary>Firm, penetrating, low-trajectory. distMax 50m.</summary>
        Driven,

        /// <summary>High arc, long diagonal, aerial phase. distMax 60m.</summary>
        Lofted,

        /// <summary>Ground-level ball into space behind defensive line. distMax 40m.</summary>
        ThroughBall,

        /// <summary>Aerial ball into space for a runner. distMax 50m.</summary>
        AerialThrough,

        /// <summary>Wide delivery into penalty area. Sub-type specified by CrossSubType. distMax 45–50m.</summary>
        Cross,

        /// <summary>Steep-arc lob over nearby defender or goalkeeper. distMax 20m.</summary>
        Chip
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                                 |
// | 1.0     | 2026-05-26 | —      | Initial implementation.                                               |
// | 1.1     | 2026-05-26 | —      | H3: CrossSubType and PassOutcome moved to own files (one-type-per-file). |
// | 1.2     | 2026-06-08 | —      | AR-7 L-3: ORDINAL STABILITY paragraph added — backing int ordinal is  |
// |         |            |        |     embedded in PassAttemptEvent (Tier A 0x0C) AND PassCancelledEvent |
// |         |            |        |     (Tier A 0x0D) payloads, AND is the third hash input to            |
// |         |            |        |     PassErrorCalculator.ComputeErrorDirection. Reordering breaks both |
// |         |            |        |     FR-DS-009 digest compat and deterministic error-direction parity. |
// |         |            |        |     APPEND-only rule applies. Stronger contract than sibling enums    |
// |         |            |        |     CancelReason and CrossSubType (digest only); PassType additionally |
// |         |            |        |     carries the hash-input contract.                                  |
#endregion
