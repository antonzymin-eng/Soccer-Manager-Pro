// Stage A measurement carrier. This and the engine's optional observer are never serialized.
using UnityEngine;

namespace TacticalDirector.MatchEngine
{
    internal enum W8StageAKind
    {
        HandClaim, Acquire, Release, KickRelease, SixSecondDrop, Restart,
        PassKick, ShotKick, GkHeadingKick, FirstTouch, LoosePickup,
        UnattributedDeflection, TacticalBefore, TacticalAfter, KeeperDecision
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
