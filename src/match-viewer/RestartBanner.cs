// File:     src/match-viewer/RestartBanner.cs
// Created:  2026-07-27
// Modified: 2026-07-27
// Author:   —
// Spec:     Interactive Unity client (docs/tracking/interactive-unity-client-design.md) §5-P1 KD-P1-3,
//           Code Standards #20
// Purpose:  The restart a View captions: what kind, who it was awarded to, and when. One type so the
//           "no restart yet" sentinel has exactly one owner instead of one per carrier.

using System;

using TacticalDirector.MatchEngine;

namespace TacticalDirector.MatchViewer
{
    /// <summary>
    /// The most recent restart a frame producer has observed — cue, awarded team, and the tick it
    /// happened on.
    ///
    /// <para><b>Why these three travel together.</b> They are meaningless apart: an awarded team without
    /// a cue is "team 0" for a restart that never happened, which is precisely the defect that showed up
    /// twice while P1 was being built — once in <c>MatchEngine</c>'s constructor, where the awarded-team
    /// field's <c>int</c> zero-default read as the home team beside a <c>None</c> cue, and again in
    /// <c>MatchFrameView.Empty</c>, where the same zero-default survived one layer up because the first
    /// fix was applied at the site rather than to the shape. Carrying them as loose fields means every
    /// carrier has to re-establish the invariant, and one of them will not.</para>
    ///
    /// <para><b>The zero value is correct by construction.</b> <c>default(RestartBanner)</c> is
    /// <see cref="None"/>: <see cref="RestartCue"/>'s zero ordinal is <c>None</c>, and both
    /// <see cref="AwardedTeam"/> and <see cref="Tick"/> are DERIVED from the cue rather than stored
    /// bare — so a zeroed struct reports −1 and 0, not "home team, tick 0". That is what makes this
    /// safe to embed in another readonly struct whose own default value is a legitimate state.</para>
    /// </summary>
    public readonly struct RestartBanner
    {
        /// <summary>What kind of restart, or <see cref="RestartCue.None"/> if none has been seen.</summary>
        public readonly RestartCue Cue;

        private readonly int _awardedTeam;
        private readonly ulong _tick;

        /// <summary>True when there is a restart to caption.</summary>
        public bool HasRestart => Cue != RestartCue.None;

        /// <summary>Team (0/1) awarded the restart, or <see cref="MatchEngineConstants.NO_RESTART_TEAM"/>
        /// (−1) when there is none. Derived from <see cref="Cue"/>, so this can never report a team for
        /// a restart that did not happen.</summary>
        public int AwardedTeam => HasRestart ? _awardedTeam : MatchEngineConstants.NO_RESTART_TEAM;

        /// <summary>The tick the restart was applied on, or 0 when there is none. A View fades its
        /// caption on <c>frameTick − Tick</c>.</summary>
        public ulong Tick => HasRestart ? _tick : 0UL;

        /// <summary>No restart observed — also what <c>default(RestartBanner)</c> is.</summary>
        public static RestartBanner None => default;

        /// <summary>
        /// Records an observed restart.
        /// </summary>
        /// <exception cref="ArgumentException"><paramref name="cue"/> is <see cref="RestartCue.None"/> —
        /// use <see cref="None"/> for that, so "no restart" has one representation rather than several
        /// with differing team and tick values.</exception>
        /// <exception cref="ArgumentOutOfRangeException">An unknown awarded team.</exception>
        public RestartBanner(RestartCue cue, int awardedTeam, ulong tick)
        {
            if (cue == RestartCue.None)
            {
                throw new ArgumentException(
                    "Use RestartBanner.None for the no-restart case; this constructor records a real one.",
                    nameof(cue));
            }
            if (awardedTeam < 0 || awardedTeam >= MatchEngineConstants.TEAM_COUNT)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(awardedTeam), awardedTeam, "awardedTeam must be 0 (home) or 1 (away).");
            }

            Cue = cue;
            _awardedTeam = awardedTeam;
            _tick = tick;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-27 | —      | Initial creation (P1 AR-1 M-3/M-6): the restart triple as one  |
// |         |            |        | type, with AwardedTeam / Tick derived from the cue so the      |
// |         |            |        | no-restart sentinel holds for default(RestartBanner) too — the |
// |         |            |        | zero-default defect that had to be fixed twice as loose fields.|
#endregion
