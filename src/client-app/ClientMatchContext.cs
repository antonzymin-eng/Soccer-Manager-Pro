// File:     src/client-app/ClientMatchContext.cs
// Created:  2026-10-06
// Modified: 2026-10-06
// Author:   —
// Spec:     P5b lifecycle/identity plan §§4–7, S0 binding contracts §2, Code Standards #20
// Purpose:  Per-match accepted frames and dispatch; invalidation drops every owned session reference.

using System;

using TacticalDirector.MatchClientCore;
using TacticalDirector.MatchViewer;
using TacticalDirector.UiFramework;

namespace TacticalDirector.ClientApp
{
    /// <summary>Match-local presentation context. The coordinator alone accepts frames and changes lifetime.</summary>
    internal sealed class ClientMatchContext : ILiveFrameSource
    {
        internal MatchSession Session { get; private set; }
        internal MatchIdentityContext Identity { get; private set; }
        internal SessionMatchAnalytics Analytics { get; private set; }
        internal MatchViewModelSource Source { get; private set; }
        internal MatchTacticsDispatcher Dispatcher { get; private set; }
        internal ClientMatchReport Report { get; private set; }
        internal bool IsValid => Session != null;
        internal bool IsFullTime { get; private set; }
        private LiveFrameLatch _latch = new LiveFrameLatch();

        internal ClientMatchContext(MatchSession session, MatchIdentityContext identity, SessionMatchAnalytics analytics)
        {
            Session = session;
            Identity = identity;
            Analytics = analytics;
            Source = new MatchViewModelSource(this);
            Dispatcher = new MatchTacticsDispatcher(session);
        }

        /// <summary>Returns only the coordinator's accepted frame; never polls or services simulation.</summary>
        public bool TryGetLatestFrame(out LiveMatchFrame frame)
        {
            frame = IsValid && _latch.HasFrame ? _latch.Current : default;
            return IsValid && _latch.HasFrame;
        }

        internal bool Accept(in LiveMatchFrame frame, float time)
        {
            if (!IsValid || IsFullTime || (_latch.HasFrame && frame.Tick <= _latch.Current.Tick)) return false;
            // Admission must succeed before a malformed frame can replace the last accepted one.
            new MatchFrameView(in frame);
            for (int i = 0; i < Identity.Roster.AgentCount; i++) Identity.Resolve(in frame, i);
            return _latch.TryAccept(in frame, time);
        }

        internal void Complete()
        {
            IsFullTime = true;
            Session.Stop();
            Session.ServiceOnce();
            MatchFrameView view = Source.Project();
            MatchAnalyticsPublication analytics = Analytics.Publish();
            Report = new ClientMatchReport(in view, in analytics, Session.Driver.Log, Session.Driver.FailedCommands);
        }

        internal void Dispatch(in ManagerIntent intent)
        {
            if (!TryGetLatestFrame(out LiveMatchFrame frame) || IsFullTime || frame.MatchEnded)
                throw new InvalidOperationException("Match commands require an accepted live frame.");
            Dispatcher.Dispatch(in intent);
        }

        internal void Invalidate()
        {
            Session = null;
            Identity = null;
            Analytics = null;
            Source = null;
            Dispatcher = null;
            Report = default;
            _latch = new LiveFrameLatch();
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes |
// | 1.0     | 2026-10-06 | —      | Consumed P5b lifecycle/identity implementation. |
#endregion
