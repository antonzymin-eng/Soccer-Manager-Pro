// File:     src/match-engine/W8StageAEvent.cs
// Created:  2026-09-25
// Modified: 2026-10-16
// Author:   —
// Spec:     W8 Stage A observation contract; Goalkeeper Mechanics #11 §3.1.3.
// Purpose:  Copied observational events for the optional W8 census; never serialized.
// Stage A measurement carrier. This and the engine's optional observer are never serialized.
using UnityEngine;

namespace TacticalDirector.MatchEngine
{
    internal enum W8StageAKind
    {
        HandClaim, Acquire, Release, KickRelease, SixSecondDrop, Restart,
        PassKick, ShotKick, GkHeadingKick, FirstTouch, LoosePickup,
        UnattributedDeflection, TacticalBefore, TacticalAfter, KeeperDecision, RestrictedHandContact
    }

    internal readonly struct W8StageAEvent
    {
        internal readonly W8StageAKind Kind;
        internal readonly int Frame;
        internal readonly int Agent;
        internal readonly int Other;
        internal readonly RestartCue Cue;
        internal readonly Vector3 BallPosition;
        internal readonly int HolderBefore;

        internal W8StageAEvent(W8StageAKind kind, int frame, int agent, int other,
            RestartCue cue, Vector3 ballPosition, int holderBefore)
        {
            Kind = kind;
            Frame = frame;
            Agent = agent;
            Other = other;
            Cue = cue;
            BallPosition = ballPosition;
            HolderBefore = holderBefore;
        }
    }
}

#region VersionHistory
// | Version | Date | Author | Notes |
// | 1.0 | 2026-10-16 | — | ERR-011-020: restricted-contact observation and restart follow-up census. |
#endregion
