// File:     src/match-analytics/MatchEngineObservation.cs
// Created:  2026-07-27
// Modified: 2026-07-27
// Author:   —
// Spec:     Match Analytics & Statistics #37 §4.3 / §4.4, FR-AN-001 / FR-AN-002, Code Standards #20
// Purpose:  Binds the aggregator's two seams to a live MatchEngine — the KD-7 ledger tap and the
//           §3.4 positional sample, both read-only.

using System;

using UnityEngine;

using TacticalDirector.MatchEngine;

namespace TacticalDirector.MatchAnalytics
{
    /// <summary>
    /// The production adapter: exposes a running <see cref="MatchEngine.MatchEngine"/> as the two read-only
    /// inputs #37 consumes.
    ///
    /// <para>Every member forwards to an existing public observation accessor and copies out a value;
    /// there is no path from here into engine state (FR-AN-001), which is what makes the
    /// observer-neutrality lock a property of the design rather than of the tests.</para>
    ///
    /// <para><b>Scope: the last completed tick.</b> Both halves read whatever
    /// <c>engine.RunTick()</c> most recently produced, so the caller must pump once per tick —
    /// see <see cref="MatchAnalyticsAggregator.ObserveTick"/>, which refuses a skipped one (F6).</para>
    /// </summary>
    public sealed class MatchEngineObservation : ITickLedgerTap, IWorldStateSample
    {
        private readonly MatchEngine.MatchEngine _engine;

        /// <summary>Wraps <paramref name="engine"/>. The engine is never mutated through this type.</summary>
        /// <exception cref="ArgumentNullException"><paramref name="engine"/> is null.</exception>
        public MatchEngineObservation(MatchEngine.MatchEngine engine)
        {
            _engine = engine ?? throw new ArgumentNullException(nameof(engine));
        }

        /// <summary>The engine's current tick — what <see cref="MatchAnalyticsAggregator.ObserveTick"/>
        /// should be handed, so the F6 consecutive-tick check measures the engine's own clock rather
        /// than a counter the caller maintains separately (and could let drift).</summary>
        public ulong CurrentTick => _engine.CurrentTick;

        // ── ITickLedgerTap ───────────────────────────────────────────────────────────────────────

        /// <inheritdoc />
        public int RecordCount => _engine.TickLedgerCount;

        /// <inheritdoc />
        public byte RecordOrdinal(int index) => _engine.TickLedgerOrdinal(index);

        /// <inheritdoc />
        public T ReadRecord<T>(int index) where T : struct => _engine.TickLedgerRecord<T>(index);

        // ── IWorldStateSample ────────────────────────────────────────────────────────────────────

        /// <inheritdoc />
        public Vector2 BallPosition
        {
            get
            {
                Vector3 p = _engine.BallView.Position;
                return new Vector2(p.x, p.y);
            }
        }

        /// <inheritdoc />
        public int AgentCount => MatchEngineConstants.SQUAD_SIZE;

        /// <inheritdoc />
        public Vector2 AgentPosition(int index) => _engine.AgentView(index).Position;

        /// <inheritdoc />
        public int AgentTeamId(int index) => _engine.AgentTeamId(index);

        /// <inheritdoc />
        public bool AgentIsActive(int index) => !_engine.AgentIsSentOff(index);
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-27 | —      | Initial creation (#37 T1): the live-engine adapter for both    |
// |         |            |        | aggregator seams.                                              |
// | 1.1     | 2026-07-27 | —      | AR-1 M-2: AgentIsActive forwards the engine's sent-off flag.   |
#endregion
