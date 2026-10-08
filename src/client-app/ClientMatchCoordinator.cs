// File:     src/client-app/ClientMatchCoordinator.cs
// Created:  2026-10-06
// Modified: 2026-10-08 (P5b screens)
// Author:   —
// Spec:     P5b lifecycle/identity plan §§4–7, S0 binding contracts §2, Code Standards #20
// Purpose:  Consumes one lifecycle with transaction, accepted-frame end barrier and repeat-match teardown.

using System;

using Unity.Profiling;

using TacticalDirector.MatchClientCore;
using TacticalDirector.MatchViewer;
using TacticalDirector.TacticalInstructions;
using TacticalDirector.UiFramework;

namespace TacticalDirector.ClientApp
{
    /// <summary>Client-thread shell coordinator. Owns playback; the renderer owns only generated visuals.</summary>
    public sealed class ClientMatchCoordinator : IDisposable
    {
        private static readonly ProfilerMarker RefreshMarker = new ProfilerMarker("ClientMatchCoordinator.Refresh");
        private readonly MatchSessionLifecycle _lifecycle = new MatchSessionLifecycle();
        private readonly IMatchRendererBinding _renderer;
        private readonly S0DemoFixture _fixture;
        private ClientMatchContext _context;
        private bool _starting;
        private bool _rejected;
        private bool _disposed;
        private long _attachmentId;
        /// <summary>One fixed five-move graph with lifetime-stable registrations.</summary>
        public ClientScreenFlow Flow { get; }
        /// <summary>Current setup draft; reset on cancel/re-entry.</summary>
        public Mentality DraftMentality { get; private set; } = Mentality.Balanced;
        /// <summary>Stable source/dispatcher registered for Match View.</summary>
        public ClientMatchScreenHandle MatchView { get; } = new ClientMatchScreenHandle();
        /// <summary>Stable frozen report source.</summary>
        public ClientReportScreenHandle Report { get; } = new ClientReportScreenHandle();
        /// <summary>True when the lifecycle owns a match.</summary>
        public bool HasMatch => _lifecycle.HasCurrent;
        /// <summary>Terminal developer integration failure; no new player retry/navigation edge.</summary>
        public bool IsRejected => _rejected;
        /// <summary>Client-local monotonic callback identity, never a simulation/save input.</summary>
        public long AttachmentId => _attachmentId;
        internal MatchSession Session => _lifecycle.Current;
        internal ClientMatchContext Context => _context;
        internal S0DemoFixture Fixture => _fixture;

        /// <summary>Constructs one shell with explicit renderer and admitted approved fixture.</summary>
        public ClientMatchCoordinator(IMatchRendererBinding renderer, S0DemoFixture fixture)
        {
            _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
            _fixture = fixture ?? throw new ArgumentNullException(nameof(fixture));
            var menu = new ScreenRegistration(ClientScreens.MainMenu, null, null);
            var setup = new ScreenRegistration(ClientScreens.TacticsSetup, null, null);
            var match = new ScreenRegistration(ClientScreens.MatchView, MatchView, MatchView);
            var report = new ScreenRegistration(ClientScreens.PostMatchReport, Report, null);
            Flow = new ClientScreenFlow(in menu, in setup, in match, in report);
        }

        /// <summary>Opens a fresh Balanced draft without constructing a session.</summary>
        public void OpenTacticsSetup()
        {
            RequireReady();
            Flow.OpenTacticsSetup();
            DraftMentality = Mentality.Balanced;
        }
        /// <summary>Validates a setup-only home Mentality selection.</summary>
        public void SelectMentality(Mentality mentality)
        {
            RequireReady();
            if (Flow.Current != ClientScreens.TacticsSetup || !Enum.IsDefined(typeof(Mentality), mentality))
                throw new InvalidOperationException("Mentality selection requires a valid setup draft.");
            DraftMentality = mentality;
        }
        /// <summary>Discards setup without a match or observer.</summary>
        public void CancelTacticsSetup()
        {
            RequireReady();
            Flow.CancelTacticsSetup();
            DraftMentality = Mentality.Balanced;
        }

        /// <summary>Attaches the real composition before navigating and starting playback exactly once.</summary>
        public bool StartMatch() => StartMatchCore(true);

        // The deterministic tests drive the SAME transaction without starting the paced thread.
        // No production UI/renderer entry point selects this mode or mixes it with paced playback.
        internal bool StartMatchForTests() => StartMatchCore(false);

        private bool StartMatchCore(bool startPlayback)
        {
            RequireReady();
            if (_starting || HasMatch) return false;
            if (Flow.Current != ClientScreens.TacticsSetup)
                throw new InvalidOperationException("Start requires Tactics Setup.");
            _starting = true;
            long attachment = checked(++_attachmentId);
            try
            {
                MatchSetup setup = _fixture.BuildSetup(DraftMentality);
                MatchSession session = _lifecycle.CreateSession(setup);
                MatchIdentityContext identity = _fixture.Bind(session);
                SessionMatchAnalytics analytics = SessionMatchAnalytics.Attach(session);
                var context = new ClientMatchContext(session, identity, analytics, setup.HomeTactic);
                _context = context;
                MatchView.Install(context);
                Report.Install(context);
                _renderer.Attach(session, identity, context, () => RejectAttachment(attachment));
                if (_context != context || !context.IsValid || _rejected)
                    throw new InvalidOperationException("Renderer rejected the active attachment.");
                Flow.StartMatch();
                if (startPlayback) session.Start();
                return true;
            }
            catch
            {
                _rejected = true;
                ClearOwnedMatch();
                throw;
            }
            finally { _starting = false; }
        }

        /// <summary>Acquires one accepted frame per refresh. Full time freezes Match View, never navigates.</summary>
        public void Refresh(float presentationTime)
        {
            using (RefreshMarker.Auto())
            {
                if (_disposed || _rejected || _context == null || _context.IsFullTime) return;
                try
                {
                    if (_context.Session.TryGetLatestFrame(out LiveMatchFrame frame) && _context.Accept(in frame, presentationTime))
                    {
                        if (frame.MatchEnded) _context.Complete();
                    }
                }
                catch
                {
                    _rejected = true;
                    ClearOwnedMatch();
                    throw;
                }
            }
        }

        /// <summary>Explicit report action, refused until the complete end barrier.</summary>
        public void ShowPostMatchReport()
        {
            RequireReady();
            if (!Report.Project().IsAvailable) throw new InvalidOperationException("No completed report.");
            Flow.ShowPostMatchReport();
        }
        /// <summary>Clears ownership/bindings before the named report-to-menu Pop.</summary>
        public void ReturnToMainMenu()
        {
            RequireReady();
            if (Flow.Current != ClientScreens.PostMatchReport) throw new InvalidOperationException("Return requires the report.");
            try { ClearOwnedMatch(); }
            catch { _rejected = true; throw; }
            Flow.ReturnToMainMenu();
            DraftMentality = Mentality.Balanced;
        }

        private void RejectAttachment(long attachment)
        {
            if (_disposed || _context == null || attachment != _attachmentId) return;
            _rejected = true;
            ClearOwnedMatch();
        }

        private void ClearOwnedMatch()
        {
            if (_lifecycle.HasCurrent) _lifecycle.Current.Stop();
            _context?.Invalidate();
            _context = null;
            MatchView.Install(null);
            Report.Install(null);
            try { _renderer.Detach(); }
            finally { _lifecycle.ClearSession(); }
        }
        private void RequireReady()
        {
            if (_disposed || _rejected) throw new InvalidOperationException("The client shell is disposed or rejected.");
        }
        /// <summary>Quiesces playback, invalidates callbacks, clears handles and detaches visuals; idempotent.</summary>
        public void Dispose()
        {
            if (_disposed) return;
            _rejected = true;
            ClearOwnedMatch();
            _disposed = true;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes |
// | 1.0     | 2026-10-06 | —      | Consumed P5b lifecycle/identity implementation. |
// | 1.1     | 2026-10-08 | —      | Expose context only inside ClientApp; seed request state from actual setup. |
#endregion
