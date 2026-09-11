// File:     src/match-client-core/MatchSessionLifecycle.cs
// Created:  2026-09-11
// Modified: 2026-09-11
// Author:   —
// Spec:     Interactive Unity client (docs/tracking/interactive-unity-client-design.md §5-P5b/§8),
//           Code Standards #20
// Purpose:  Host-free owner for a SERIAL sequence of MatchSession instances. MatchSession owns one
//           match composition; this type owns which one is current so a Unity shell can run match 1,
//           return to the menu/report flow, then create match 2 without a MonoBehaviour owning the
//           construction policy or accidentally retaining the prior session as the product current.

using System;

namespace TacticalDirector.MatchClientCore
{
    /// <summary>
    /// Owns the product's current <see cref="MatchSession"/> across repeated matches.
    /// <para>
    /// A <see cref="MatchSession"/> is deliberately single-match and single-use. This coordinator is
    /// the layer above that lifetime: it constructs a fresh session from a <see cref="MatchSetup"/>,
    /// calls Stop on the previous current session before replacement, and provides one explicit clear
    /// path. The Unity P5b binding may attach the returned session, but it does not decide how sessions
    /// are created or replaced.
    /// </para>
    /// <para>
    /// This is an ownership selector, not a capability-revocation wrapper. <see cref="MatchSession.Stop"/>
    /// delegates to <c>LiveMatchStreamer.Stop()</c>, whose existing contract is a no-op before playback
    /// has started. Therefore a caller that kept a stale reference to a never-started predecessor can
    /// still service that stale object. This permissive boundary is CHOSEN for this bounded prerequisite,
    /// not accidental: the later P5b <c>Attach(MatchSession)</c> binding must replace its held reference
    /// atomically and must never keep using a session after this lifecycle installs another. A running
    /// predecessor is fully stopped/joined before the replacement becomes current.
    /// </para>
    /// <para>
    /// Direct-driving callers are outside Stop's paced-thread authority. A caller using TickOnce or
    /// ServiceOnce from another thread must quiesce that external driver before replacement; Stop only
    /// governs the streamer's own pacing loop.
    /// </para>
    /// <para>
    /// <see cref="CreateSession"/> does <b>not</b> call <see cref="MatchSession.Start"/>. Construction,
    /// host attachment, and paced playback remain separate operations so the host can finish wiring
    /// the Match View before the simulation thread begins producing frames.
    /// </para>
    /// <para>
    /// Not thread-safe. The lifecycle is a UI/composition-root concern and is driven by the client
    /// thread. MatchSession itself owns the synchronization needed by its streamer and command path.
    /// </para>
    /// </summary>
    public sealed class MatchSessionLifecycle
    {
        private MatchSession _current;

        /// <summary>True when a current session has been created and not cleared.</summary>
        public bool HasCurrent => _current != null;

        /// <summary>
        /// The current session.
        /// </summary>
        /// <exception cref="InvalidOperationException">No current session exists.</exception>
        public MatchSession Current
        {
            get
            {
                if (_current == null)
                {
                    throw new InvalidOperationException(
                        "MatchSessionLifecycle has no current session. CreateSession must succeed first.");
                }

                return _current;
            }
        }

        /// <summary>
        /// Constructs and installs a fresh, not-yet-started session from <paramref name="setup"/>.
        /// If another session is current, its Stop path is run before the replacement becomes current.
        /// <para>
        /// The replacement is constructed first. Therefore a constructor failure cannot tear down a
        /// valid current match and leave the lifecycle empty.
        /// </para>
        /// <para>
        /// If predecessor Stop throws, the exception propagates and the predecessor remains current;
        /// the already-constructed replacement is discarded rather than published over a failed teardown.
        /// </para>
        /// </summary>
        /// <param name="setup">Immutable boot configuration for the new match. Must not be null.</param>
        /// <returns>The newly installed session; identical by reference to <see cref="Current"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="setup"/> is null.</exception>
        public MatchSession CreateSession(MatchSetup setup)
        {
            if (setup == null)
            {
                throw new ArgumentNullException(nameof(setup));
            }

            // Construct first: if MatchSession ever gains another fail-loud setup validation, a bad
            // replacement must not stop the still-valid current session before that validation fires.
            var replacement = new MatchSession(setup);

            if (_current != null)
            {
                _current.Stop();
            }

            _current = replacement;
            return replacement;
        }

        /// <summary>
        /// Runs Stop on and forgets the current session. Idempotent when no session exists, which keeps
        /// host teardown safe when initialization failed before a session was created. If Stop throws,
        /// the exception propagates and the current reference is retained. See the class ownership note
        /// for the existing Stop-before-Start no-op behavior.
        /// </summary>
        public void ClearSession()
        {
            if (_current == null)
            {
                return;
            }

            _current.Stop();
            _current = null;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.2     | 2026-09-11 | —      | Owner-approved bounded prerequisite: chosen permissive stale-  |
// |         |            |        | reference boundary and direct-drive/Stop-failure docs.         |
// | 1.1     | 2026-09-11 | —      | Review: document Stop failure and direct-driving boundaries.   |
// | 1.0     | 2026-09-11 | —      | P5b lifecycle slice: host-free current-session ownership,      |
// |         |            |        | repeat-match replacement, and idempotent clear.                |
#endregion
