// File:     src/match-engine/tests/BallLooseEpisodeObserver.cs
// Created:  2026-11-09
// Modified: 2026-11-10
// Author:   —
// Spec:     w-440-ball-loose-design.md frozen Stage A measurement contract
// Purpose:  Environment-gated, test-side episode census; no gameplay writes.

using System;
using System.Collections.Generic;

using TacticalDirector.DefensiveAI;

namespace TacticalDirector.MatchEngine
{
    internal sealed class BallLooseEpisodeObserver
    {
        /// <summary>[FIXED] Observation horizon in 60 Hz frames, preregistered before capture.</summary>
        internal const int HORIZON_FRAMES = 60;
        private readonly Func<int, int> _team;
        private readonly List<Episode> _frameEnds = new List<Episode>();
        private Episode _active;
        private int _lastHolder;
        private int _sequence;

        internal readonly List<Episode> Episodes = new List<Episode>();
        internal readonly List<Entry> Journal = new List<Entry>();
        internal int OwnershipChanges { get; private set; }

        internal readonly struct Entry
        {
            public readonly int Sequence;
            public readonly BallLooseStageAEvent Event;
            internal Entry(int sequence, in BallLooseStageAEvent evt) { Sequence = sequence; Event = evt; }
        }

        internal sealed class Episode
        {
            public int Frame, Tackler, Carrier, CarrierTeam, Sequence;
            public int FrameEndOwner = int.MinValue;
            public int EndFrame = -1;
            public string Outcome = "pending";
            public string CensorReason;
            public bool SameFrameCarrierRegain, OpponentRecovery, TeammateRecovery;
            public UnityEngine.Vector3 InitialPosition, InitialVelocity, FrameEndPosition, FrameEndVelocity;
            public readonly List<Entry> Ownership = new List<Entry>();
            public readonly List<Entry> Paths = new List<Entry>();
        }

        internal BallLooseEpisodeObserver(Func<int, int> team, int initialHolder)
        {
            _team = team ?? throw new ArgumentNullException(nameof(team));
            _lastHolder = initialHolder;
        }

        internal static BallLooseEpisodeObserver AttachFromEnvironment(MatchEngine engine)
        {
            if (Environment.GetEnvironmentVariable("TD_W440_OBSERVE") != "1") return null;
            if (engine.TestOnly_BallLooseStageAObserver != null)
                throw new InvalidOperationException("#440 observer already attached.");
            var observer = new BallLooseEpisodeObserver(engine.AgentTeamId, engine.PossessingAgentId);
            engine.TestOnly_BallLooseStageAObserver = observer.Observe;
            return observer;
        }

        internal void Observe(BallLooseStageAEvent evt)
        {
            var entry = new Entry(++_sequence, in evt);
            Journal.Add(entry);
            switch (evt.Kind)
            {
                case BallLooseStageAKind.Tackle:
                    Censor("new-tackle", evt.Frame);
                    if (evt.Detail == (int)TackleOutcome.BallLoose)
                    {
                        int team = _team(evt.Other);
                        Require(team == 0 || team == 1, "invalid carrier team");
                        Require(_team(evt.Actor) != team, "tackler must oppose carrier");
                        Require(_lastHolder == evt.Other, "tackle carrier disagrees with ownership chronology");
                        _active = new Episode { Frame = evt.Frame, Tackler = evt.Actor, Carrier = evt.Other,
                            CarrierTeam = team, Sequence = _sequence, InitialPosition = evt.BallPosition,
                            InitialVelocity = evt.BallVelocity };
                        Episodes.Add(_active); _frameEnds.Add(_active);
                    }
                    break;
                case BallLooseStageAKind.Possession:
                    Require(evt.Previous == _lastHolder, "missing or reordered ownership transition");
                    Require(evt.Previous != evt.Next, "unchanged owner callback");
                    _lastHolder = evt.Next; OwnershipChanges++;
                    if (_active != null)
                    {
                        _active.Ownership.Add(entry);
                        if (evt.Next >= 0)
                        {
                            int team = _team(evt.Next);
                            Require(team == 0 || team == 1, "invalid recovery team");
                            if (evt.Next == _active.Carrier && evt.Frame == _active.Frame)
                                _active.SameFrameCarrierRegain = true;
                            else if (evt.Next != _active.Carrier && team == _active.CarrierTeam)
                                _active.TeammateRecovery = true;
                            if (team != _active.CarrierTeam) _active.OpponentRecovery = true;
                        }
                    }
                    break;
                case BallLooseStageAKind.FirstTouch:
                case BallLooseStageAKind.LoosePickup:
                    _active?.Paths.Add(entry);
                    break;
                case BallLooseStageAKind.Restart: Censor("restart", evt.Frame); break;
                case BallLooseStageAKind.OutOfPlay: Censor("out-of-play", evt.Frame); break;
                case BallLooseStageAKind.Foul: Censor("foul", evt.Frame); break;
                case BallLooseStageAKind.HalfTime: Censor("half-time", evt.Frame); break;
                case BallLooseStageAKind.FullTime: Censor("full-time", evt.Frame); break;
                default: throw new InvalidOperationException("Unknown #440 observation kind.");
            }
        }

        internal void AfterTick(int frame, int owner, UnityEngine.Vector3 position, UnityEngine.Vector3 velocity)
        {
            Require(owner == _lastHolder, "frame-end owner disagrees with transition chronology");
            foreach (var episode in _frameEnds)
            {
                Require(episode.Frame == frame, "missed tackle-frame endpoint");
                episode.FrameEndOwner = owner; episode.FrameEndPosition = position; episode.FrameEndVelocity = velocity;
            }
            _frameEnds.Clear();
            if (_active == null) return;
            int horizon = _active.Frame + HORIZON_FRAMES;
            Require(frame <= horizon, "missed fixed horizon");
            if (frame == horizon)
            {
                _active.Outcome = Classify(owner, _active);
                _active.EndFrame = frame; _active = null;
            }
        }

        internal void Complete(int frame)
        {
            Require(_frameEnds.Count == 0, "missing final frame-end observations");
            Censor("capture-end", frame);
            foreach (var e in Episodes)
                Require(e.Outcome != "pending" && e.FrameEndOwner != int.MinValue, "incomplete episode");
        }

        internal string Classify(int owner, Episode episode)
        {
            if (owner < 0) return "still-loose";
            if (owner == episode.Carrier) return "original-carrier";
            return _team(owner) == episode.CarrierTeam ? "carrier-teammate" : "opponent";
        }

        private void Censor(string reason, int frame)
        {
            if (_active == null) return;
            _active.Outcome = "censored"; _active.CensorReason = reason;
            _active.EndFrame = frame; _active = null;
        }

        private static void Require(bool condition, string reason)
        {
            if (!condition) throw new InvalidOperationException(reason);
        }
    }
}

#region VersionHistory
// | Version | Date | Author | Notes |
// | 1.0 | 2026-11-10 | — | Frozen endpoint, overlap/censor and same-frame chronology census. No test skip or gameplay mutation. |
#endregion
