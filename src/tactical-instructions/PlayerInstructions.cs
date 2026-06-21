// File:     src/tactical-instructions/PlayerInstructions.cs
// Created:  2026-06-21
// Modified: 2026-06-21
// Author:   —
// Spec:     Tactical Instructions #21 §2.2.2, §3.4, §3.5, Code Standards #20
// Purpose:  Per-agent individual instructions. Immutable-per-match input carrier with
//           an identity factory (all biases Default = follow team). Field order is the
//           canonical snapshot order (Appendix B) once FR-TI-028 activates.

namespace TacticalDirector.TacticalInstructions
{
    /// <summary>
    /// Per-agent individual instructions (§2.2.2). All <see cref="InstrBias"/> fields default to
    /// <see cref="InstrBias.Default"/> (follow the team tactic). Every bias modulates exactly its
    /// named #8 term (FR-TI-014); <see cref="Default"/> is the behavioural identity (FR-TI-031).
    /// Property order matches the Appendix B canonical snapshot order.
    /// </summary>
    /// <remarks>
    /// HAZARD — <c>default(PlayerInstructions)</c> is NOT the identity: the C# struct default skips
    /// the factory, so <see cref="MarkTargetEntityId"/> is 0 (a *valid* entity id ⇒ a silent man-mark
    /// request on agent 0), not the −1 "none" sentinel. Construct only via <see cref="Default"/> or
    /// the explicit constructor; consumers must treat default-valued instances as malformed
    /// (parallels Testing Strategy #19 AR-2 L-6).
    /// </remarks>
    public readonly struct PlayerInstructions
    {
        /// <summary>Risky/ambitious passing bias (#8 pass-risk term). §3.4.</summary>
        public InstrBias RiskyPasses { get; }

        /// <summary>Shooting tendency bias (#8 SHOOT term). §3.4.</summary>
        public InstrBias ShootTendency { get; }

        /// <summary>Dribbling tendency bias (#8 DRIBBLE term). §3.4.</summary>
        public InstrBias DribbleTendency { get; }

        /// <summary>Crossing tendency bias (#8 cross term). §3.4.</summary>
        public InstrBias CrossTendency { get; }

        /// <summary>Positional-freedom bias (#12 / #8 positioning term). §3.4.</summary>
        public InstrBias PositioningFreedom { get; }

        /// <summary>Closing-down bias (#13 / #8 press term). §3.4.</summary>
        public InstrBias CloseDown { get; }

        /// <summary>Tighter man-mark distance when true. §3.4.</summary>
        public bool TightMarking { get; }

        /// <summary>
        /// Man-mark target entity id. <see cref="TacticalInstructionsConstants.MARK_TARGET_NONE"/> (−1)
        /// = none; ≥0 requests #14 force <c>MarkMode.ManMark</c> on that opponent, honoured only within
        /// #14's §3.10 invariants (FR-TI-023 / KD-9). §3.5.
        /// </summary>
        public int MarkTargetEntityId { get; }

        /// <summary>Set-piece taker duties (Stage 1+; §2.2.2 / §7.3).</summary>
        public SetPieceDutyFlags SetPieceRoles { get; }

        /// <summary>Constructs a <see cref="PlayerInstructions"/> with explicit field values.</summary>
        public PlayerInstructions(
            InstrBias riskyPasses,
            InstrBias shootTendency,
            InstrBias dribbleTendency,
            InstrBias crossTendency,
            InstrBias positioningFreedom,
            InstrBias closeDown,
            bool tightMarking,
            int markTargetEntityId,
            SetPieceDutyFlags setPieceRoles)
        {
            RiskyPasses = riskyPasses;
            ShootTendency = shootTendency;
            DribbleTendency = dribbleTendency;
            CrossTendency = crossTendency;
            PositioningFreedom = positioningFreedom;
            CloseDown = closeDown;
            TightMarking = tightMarking;
            MarkTargetEntityId = markTargetEntityId;
            SetPieceRoles = setPieceRoles;
        }

        /// <summary>
        /// Identity instructions (§2.2.2): all biases <see cref="InstrBias.Default"/>, TightMarking false,
        /// no man-mark target, no set-piece duty. Behaviour-neutral (FR-TI-031).
        /// </summary>
        public static PlayerInstructions Default => new PlayerInstructions(
            InstrBias.Default,
            InstrBias.Default,
            InstrBias.Default,
            InstrBias.Default,
            InstrBias.Default,
            InstrBias.Default,
            false,
            TacticalInstructionsConstants.MARK_TARGET_NONE,
            SetPieceDutyFlags.None);
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-06-21 | —      | Initial implementation (T0 #21).                                   |
// | 1.1     | 2026-06-21 | —      | AR-1 L-1: <remarks> default-value hazard note (default() encodes a |
// |         |            |        | man-mark on agent 0 via MarkTargetEntityId 0, not the −1 sentinel).|
#endregion
