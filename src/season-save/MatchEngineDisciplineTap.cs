// File:     src/season-save/MatchEngineDisciplineTap.cs
// Created:  2026-08-13
// Modified: 2026-08-16 (reviewed findings pass, M3 — v1.1: forwards MatchEngine.CurrentTick, an
//           already-public clock read (no MatchEngine.cs change needed), for the new
//           IDisciplineTickLedgerTap.CurrentTick member.)
// Author:   —
// Spec:     Discipline & Suspensions #44 §4.3 (the tap read, KD-2) / FR-DC-002/003; Match Analytics
//           #37 FR-AN-002 (the tap pattern); Season & Competition Loop #30 §3.4; Code Standards #20
// Purpose:  Adapts MatchEngine's current tick plus its three per-tick ledger accessors to #44's
//           IDisciplineTickLedgerTap, so the fold can read an engine it is not allowed to reference.

using System;

using TacticalDirector.Discipline;

namespace TacticalDirector.SeasonSave
{
    /// <summary>
    /// The composition root's adapter between the engine's per-tick ledger tap and #44's
    /// <see cref="IDisciplineTickLedgerTap"/> (#44 §4.3).
    /// <para>
    /// It exists because #44 §4.1 forbids the discipline assembly to reference the match engine, and
    /// <c>match-analytics</c>'s identically-shaped <c>MatchEngineObservation</c> is unreachable from
    /// here (<c>season-save</c> does not reference it, and adding that edge to borrow three forwarding
    /// methods would buy nothing). <c>season-save</c> is the one assembly that sees both sides, which is
    /// exactly what a composition root is for.
    /// </para>
    /// <para>
    /// <b>Holds the engine by reference and reads it live</b> — the tap is scoped to the last completed
    /// tick and this type deliberately caches nothing, so an adapter constructed once per fixture stays
    /// correct for every tick of it.
    /// </para>
    /// <para><b>Read-only</b>: every member forwards to a getter (FR-DC-003, observer neutrality).</para>
    /// </summary>
    internal sealed class MatchEngineDisciplineTap : IDisciplineTickLedgerTap
    {
        private readonly TacticalDirector.MatchEngine.MatchEngine _engine;

        /// <summary>Wraps a live engine.</summary>
        /// <exception cref="ArgumentNullException"><paramref name="engine"/> is null.</exception>
        internal MatchEngineDisciplineTap(TacticalDirector.MatchEngine.MatchEngine engine)
        {
            _engine = engine ?? throw new ArgumentNullException(nameof(engine));
        }

        /// <inheritdoc/>
        public ulong CurrentTick => _engine.CurrentTick;

        /// <inheritdoc/>
        public int RecordCount => _engine.TickLedgerCount;

        /// <inheritdoc/>
        public byte OrdinalAt(int index) => _engine.TickLedgerOrdinal(index);

        /// <inheritdoc/>
        public T RecordAt<T>(int index) where T : struct => _engine.TickLedgerRecord<T>(index);
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                            |
// | 1.0     | 2026-08-13 | —      | Initial implementation (#44 T2, roadmap C2): the composition     |
// |         |            |        | root's three-method forward from MatchEngine's ledger tap to     |
// |         |            |        | #44's IDisciplineTickLedgerTap.                                  |
// | 1.1     | 2026-08-16 | —      | Reviewed findings pass (M3): CurrentTick forwards the engine's   |
// |         |            |        | own already-public clock (MatchEngine.CurrentTick); no engine    |
// |         |            |        | change was needed.                                               |
#endregion
