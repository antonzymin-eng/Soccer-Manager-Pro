// File:     src/match-engine/BallLooseStageAEvent.cs
// Created:  2026-11-09
// Modified: 2026-11-10
// Author:   —
// Spec:     #14 §3.6.5; w-440-ball-loose-design.md Stage A (observation only)
// Purpose:  Copied tackle/ownership/censor chronology; never serialized.

#if UNITY_EDITOR || DEVELOPMENT_BUILD || UNITY_INCLUDE_TESTS
using UnityEngine;

namespace TacticalDirector.MatchEngine
{
    internal enum BallLooseStageAKind
    {
        Tackle, Possession, Restart, OutOfPlay, Foul, HalfTime, FullTime, FirstTouch, LoosePickup
    }

    internal readonly struct BallLooseStageAEvent
    {
        internal readonly BallLooseStageAKind Kind;
        internal readonly int Frame, Actor, Other, Detail, Previous, Next;
        internal readonly Vector3 BallPosition, BallVelocity;

        internal BallLooseStageAEvent(BallLooseStageAKind kind, int frame, int actor, int other,
            int detail, int previous, int next, Vector3 ballPosition, Vector3 ballVelocity)
        {
            Kind = kind; Frame = frame; Actor = actor; Other = other; Detail = detail;
            Previous = previous; Next = next; BallPosition = ballPosition; BallVelocity = ballVelocity;
        }
    }
}
#endif

#region VersionHistory
// | Version | Date | Author | Notes |
// | 1.0 | 2026-11-10 | — | Development-only copied chronology carrier for #440 Stage A. |
#endregion
