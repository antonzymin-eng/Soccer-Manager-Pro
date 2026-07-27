// File:     src/match-client-web/MatchClientHost.cs
// Created:  2026-07-27
// Modified: 2026-07-27
// Author:   —
// Spec:     Path-to-playable roadmap §7 (B6, option (b)); interactive Unity client design note
//           §5-P2/§5-P3; UI / Client Framework #38 §3.1–§3.3; Match Analytics #37 §4.4;
//           Code Standards #20
// Purpose:  The PM-1 composition root: one running match, its #38 view-model projection, its
//           manager-intent dispatcher, and its live #37 statistics — assembled so the browser (and
//           later the UGUI skin) binds the same substrate.

using System;

using TacticalDirector.MatchAnalytics;
using TacticalDirector.MatchClientCore;
using TacticalDirector.MatchViewer;
using TacticalDirector.UiFramework;

namespace TacticalDirector.MatchClientWeb
{
    /// <summary>
    /// Owns everything one playable match needs and nothing a transport does.
    ///
    /// <para><b>Why this is not an extension of <c>LiveMatchServer</c>.</b> That server is the
    /// spectator surface, and its playback-only invariant is load-bearing — it is why the streamer
    /// has no mutation API at all (<c>interactive-unity-client-design.md</c> AR-1 H-2, ERR-038-001).
    /// PM-1 needs manager input, so the mutating surface is built HERE, above `match-client-core`,
    /// and reaches the engine only through the tick-stamped command queue. `match-viewer` gains a
    /// read-only post-tick observer seam and nothing else.</para>
    ///
    /// <para><b>Three surfaces, three privileges, deliberately not merged:</b> reading a frame
    /// cannot change a match; a playback change (pause / speed) changes when ticks happen but never
    /// what is in them; only an intent changes the match, and it does so through the queue so it
    /// lands on a tick boundary and appears in the replay log.</para>
    ///
    /// <para><b>Threading.</b> The streamer ticks on its own thread. Statistics are accumulated by a
    /// post-tick observer on THAT thread and read by the transport thread, so both take
    /// <see cref="_analyticsGate"/>. The lock is held for one tick's routing (typically zero records)
    /// or one <c>Build()</c>; the streamer already serialises each tick behind its own gate.</para>
    /// </summary>
    public sealed class MatchClientHost
    {
        private readonly MatchSession             _session;
        private readonly MatchViewModelSource     _frames;
        private readonly MatchTacticsDispatcher   _dispatcher;
        private readonly MatchAnalyticsAggregator _analytics = new MatchAnalyticsAggregator();
        private readonly object                   _analyticsGate = new object();

        /// <summary>
        /// Builds the composition for <paramref name="setup"/> and attaches the analytics pump before
        /// anything ticks — attaching after would miss ticks, and #37's every-tick contract fails loud
        /// on a gap rather than quietly under-counting.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="setup"/> is null.</exception>
        public MatchClientHost(MatchSetup setup)
        {
            if (setup == null) { throw new ArgumentNullException(nameof(setup)); }

            _session    = new MatchSession(setup);
            _frames     = new MatchViewModelSource(new LiveMatchStreamerFrameSource(_session.Streamer));
            _dispatcher = new MatchTacticsDispatcher(_session);

            _session.AttachTickObserver(engine =>
            {
                // The factory receives the engine once and closes over a READER built from it. The
                // observation adapter exposes only #37's two read-only taps, so this closure has no
                // way to mutate the match even by accident.
                var observation = new MatchEngineObservation(engine);
                return () =>
                {
                    lock (_analyticsGate)
                    {
                        _analytics.ObserveTick(observation, observation, observation.CurrentTick);
                    }
                };
            });
        }

        /// <summary>The session — playback control lives on its streamer; commands on its queue.</summary>
        public MatchSession Session => _session;

        /// <summary>The #38 projection the View renders. Same view model the UGUI skin will bind.</summary>
        public MatchFrameView Project() => _frames.Project();

        /// <summary>
        /// Routes a manager intent onto the queue (never straight at the engine), so it applies on a
        /// tick boundary and is recorded in the driver's tick-stamped log.
        /// </summary>
        /// <exception cref="InvalidOperationException">The intent kind has no mapped sim seam (F3).</exception>
        public void Dispatch(in ManagerIntent intent) => _dispatcher.Dispatch(in intent);

        /// <summary>
        /// The statistics as of the last completed tick, together with the tick count they were
        /// derived from. Safe to call from any thread and at any cadence — <c>Build()</c> is
        /// idempotent and consumes nothing, so a screen may poll it.
        ///
        /// <para>Both values come out of ONE lock acquisition on purpose: read separately, the sim
        /// thread can advance between them and a caller gets shares computed at tick N labelled with
        /// tick N+1 — a small incoherence, but the kind that turns into a confusing bug report about
        /// percentages that do not add up.</para>
        /// </summary>
        public MatchAnalyticsReport BuildReport()
        {
            lock (_analyticsGate)
            {
                return new MatchAnalyticsReport(_analytics.Build(), _analytics.ObservedTicks);
            }
        }

        /// <summary>Ticks observed so far. Prefer <see cref="BuildReport"/> when both are wanted.</summary>
        public long ObservedTicks
        {
            get { lock (_analyticsGate) { return _analytics.ObservedTicks; } }
        }

        /// <summary>Begins paced playback.</summary>
        public void Start() => _session.Start();

        /// <summary>Stops and joins the pacing loop.</summary>
        public void Stop() => _session.Stop();

        /// <summary>
        /// The exception that disarmed the statistics pump, or null while it is healthy. A screen
        /// MUST surface this rather than showing frozen numbers as if they were current.
        /// </summary>
        public Exception AnalyticsFault => _session.Streamer.PostTickObserverFault;
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-27 | —      | Initial creation (B6): the PM-1 composition — session, #38     |
// |         |            |        | projection, intent dispatch through the queue, and the live    |
// |         |            |        | #37 aggregator pumped by a sim-thread post-tick observer.      |
#endregion
