using System;
using UnityEngine;
namespace TacticalDirector.MatchEngine
{
    internal readonly struct W8ProbeEvent
    {
        internal readonly int Frame, Agent, Team, ScoringTeam, RestrictionKeeper;
        internal readonly string Kind;
        internal readonly Vector3 Position;
        internal W8ProbeEvent(int frame, string kind, int agent, int team, Vector3 position, int scoringTeam, int restriction)
        { Frame=frame; Kind=kind; Agent=agent; Team=team; Position=position; ScoringTeam=scoringTeam; RestrictionKeeper=restriction; }
    }
    public sealed partial class MatchEngine
    {
        internal Action<W8ProbeEvent> W8ProbeObserver;
        internal Action<byte[],int,int> W8ProbeLedgerObserver;
        internal void AttachW8BodyProbe() => _collisionSystem.W8ProbeAppliedTouch = (agent, point) => ProbeRecord("body", agent, point);
        private void ProbeRecord(string kind, int agent, Vector3 point, int scoringTeam = -1) =>
            W8ProbeObserver?.Invoke(new W8ProbeEvent((int)_clock.CurrentTick, kind, agent,
                agent < 0 ? -1 : _teamIds[agent], point, scoringTeam, _releasedHandKeeper));
    }
}
