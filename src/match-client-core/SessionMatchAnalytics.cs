// File:     src/match-client-core/SessionMatchAnalytics.cs
// Created:  2026-10-06
// Modified: 2026-10-06
// Author:   —
// Spec:     P5b lifecycle/identity plan §§4–7, S0 binding contracts §2, Code Standards #20
// Purpose:  Consumes the single pre-playback observer slot and synchronizes observation/publication.

using System;

using Unity.Profiling;

using TacticalDirector.MatchAnalytics;

namespace TacticalDirector.MatchClientCore
{
    /// <summary>One per-session analytics pump. Lock order is streamer tick gate, then this adapter gate.</summary>
    public sealed class SessionMatchAnalytics
    {
        private static readonly ProfilerMarker ObserveMarker = new ProfilerMarker("SessionMatchAnalytics.ObserveTick");
        private readonly object _gate = new object();
        private readonly MatchAnalyticsAggregator _aggregator = new MatchAnalyticsAggregator();
        private readonly ITickLedgerTap _tap;
        private readonly IWorldStateSample _sample;
        private readonly Func<ulong> _tick;
        private ulong _attemptedTick;
        private ulong _completedTick;
        private long _observedTicks;
        private bool _incomplete;
        private MatchAnalyticsResult? _lastResult;
        private ulong _resultThroughTick;

        internal SessionMatchAnalytics(ITickLedgerTap tap, IWorldStateSample sample, Func<ulong> tick)
        {
            _tap = tap ?? throw new ArgumentNullException(nameof(tap));
            _sample = sample ?? throw new ArgumentNullException(nameof(sample));
            _tick = tick ?? throw new ArgumentNullException(nameof(tick));
        }

        /// <summary>Attaches exactly once, before playback; later per-tick consumers must compose here.</summary>
        public static SessionMatchAnalytics Attach(MatchSession session)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            SessionMatchAnalytics adapter = null;
            session.AttachTickObserver(engine =>
            {
                var observation = new MatchEngineObservation(engine);
                adapter = new SessionMatchAnalytics(observation, observation, () => observation.CurrentTick);
                return adapter.ObserveTick;
            });
            return adapter;
        }

        internal void ObserveTick()
        {
            using (ObserveMarker.Auto())
            lock (_gate)
            {
                try
                {
                    _attemptedTick = _tick();
                    _aggregator.ObserveTick(_tap, _sample, _attemptedTick);
                    _completedTick = _attemptedTick;
                    _observedTicks++;
                }
                catch
                {
                    // Publish failure before the streamer sees/disarms the exception. The aggregator
                    // may already contain half of a tick, so retain only an earlier immutable result.
                    _incomplete = true;
                    throw;
                }
            }
        }

        /// <summary>Publishes a consistent immutable result. Never calls a session/streamer while locked.</summary>
        public MatchAnalyticsPublication Publish()
        {
            lock (_gate)
            {
                if (!_incomplete)
                {
                    _lastResult = _aggregator.Build();
                    _resultThroughTick = _completedTick;
                }
                return new MatchAnalyticsPublication(_lastResult, _incomplete, _attemptedTick,
                    _completedTick, _observedTicks, _resultThroughTick);
            }
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes |
// | 1.0     | 2026-10-06 | —      | Consumed P5b lifecycle/identity implementation. |
#endregion
