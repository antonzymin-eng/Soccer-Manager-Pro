// File:     src/goalkeeper-mechanics/GoalkeeperState.cs
// Created:  2026-05-28
// Modified: 2026-05-28
// Author:   —
// Spec:     Goalkeeper Mechanics #11 §3.1, Code Standards #20
// Purpose:  GK state-machine state enumeration. Each value corresponds to one row
//           in the §3.1.1 transition table.

namespace TacticalDirector.GoalkeeperMechanics
{
    /// <summary>
    /// Enumeration of all valid GK state-machine states.
    /// Transition rules are defined in §3.1.1; evaluation logic lives in GoalkeeperStateMachine.
    /// Goalkeeper Mechanics #11 §3.1.
    /// </summary>
    public enum GoalkeeperState
    {
        /// <summary>Default holding state. Ball in own/middle third with own-team possession. §3.1.</summary>
        Resting,

        /// <summary>Ball has entered attacking third; GK is alert but not committing. §3.1.</summary>
        Set,

        /// <summary>Decision Tree anticipation score exceeds ANTICIPATE_THRESHOLD; GK is actively reading the shot. §3.1.</summary>
        Anticipate,

        /// <summary>SaveIntent committed; dive launch impulse being applied. §3.1 / §3.3.</summary>
        Diving,

        /// <summary>Dive launch impulse applied; GK is off the ground tracking the ball. §3.1 / §3.3.</summary>
        Airborne,

        /// <summary>Hand contact resolved with handlingQualityScalar ≥ CATCH_THRESHOLD; GK holds the ball. §3.1 / §3.5.</summary>
        HandsOnBall,

        /// <summary>Contact resolved below CATCH_THRESHOLD, or ground re-entry after failed save. §3.1.</summary>
        Recovering,

        /// <summary>DecisionTree DistributeIntent committed; GK is releasing ball. §3.1 / §3.8.</summary>
        Distributing,

        /// <summary>RushIntent committed; GK moving toward ball/attacker. §3.1 / §3.7.</summary>
        Rushing,

        /// <summary>Attacker with ball is within ONE_VS_ONE_TRIGGER_RADIUS_M during rush. §3.1 / §3.7.</summary>
        OneOnOne,

        /// <summary>GK has closed within SMOTHER_TRIGGER_RADIUS_M; hand-ball contact pending. §3.1 / §3.7.</summary>
        Smothered
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-28 | —      | Initial implementation. |
#endregion
