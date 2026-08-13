// File:     src/discipline/IDisciplineTickLedgerTap.cs
// Created:  2026-08-13
// Modified: 2026-08-13
// Author:   —
// Spec:     Discipline & Suspensions #44 §4.1/§4.3 (the tap read, KD-2) / FR-DC-002 / FR-DC-003;
//           Match Analytics #37 FR-AN-002 (the pattern this mirrors); Code Standards #20 FR-CS-048
// Purpose:  The read-only per-tick ledger tap CardLedgerFold consumes — three accessors over the
//           records the engine captured for the tick just completed.

namespace TacticalDirector.Discipline
{
    /// <summary>
    /// The #37-class read-only per-tick ledger tap (#44 §4.3, FR-DC-002), as #44 sees it.
    /// <para>
    /// <b>Scope is the LAST COMPLETED TICK only.</b> The engine fills its snapshot in the Snapshot
    /// phase — after the ledger is serialized for the digest and before the bus resets the tick, the
    /// one moment the records exist and the tick is identified — so a consumer that does not pump
    /// every tick does not read stale records, it loses them. <see cref="CardLedgerFold.ObserveTick"/>
    /// is therefore a per-tick obligation of the composition root, not a poll.
    /// </para>
    /// <para>
    /// <b>Why this interface exists rather than #37's <c>ITickLedgerTap</c>.</b> #44 §4.1 forbids this
    /// assembly to reference the match engine, and <c>season-save</c> — the composition root that owns
    /// the engine — does not reference <c>match-analytics</c> either, so #37's identically-shaped
    /// interface is unreachable from both ends. Two read-only accessor shapes over the same three
    /// engine methods is not the parallel-surface trap this project keeps filing: that trap is
    /// duplicated <i>rules</i> whose divergence changes behaviour (<c>LineupSelector.CanSelect</c>),
    /// and there is no rule here to diverge. FR-CS-048 is satisfied — the consumer is specified and
    /// is in this assembly.
    /// </para>
    /// <para>
    /// <b>Recorded, so no reader assumes otherwise:</b> #44 §4.3's "one tap feeds both [#37 and #44]"
    /// is not achievable today. It is true of the ENGINE-side tap — one <c>TickLedgerSnapshot</c>,
    /// filled once per tick, read by whoever asks — but there is no composition root that can see both
    /// <c>match-analytics</c> and <c>discipline</c>, so nothing shares an adapter. Nothing is lost by
    /// that: the tap is read-only, and reading it twice costs two reads.
    /// </para>
    /// <para>
    /// <b>Observer-neutral by construction</b> (FR-DC-003): every member is a read. A fixture observed
    /// through this interface is digest-identical to the same fixture unobserved, because there is no
    /// member here that could make it otherwise.
    /// </para>
    /// </summary>
    public interface IDisciplineTickLedgerTap
    {
        /// <summary>Number of records captured for the tick just completed.</summary>
        int RecordCount { get; }

        /// <summary>
        /// The event-type ordinal of the record at <paramref name="index"/> — read first so the fold
        /// can ignore an ordinal it does not know (FR-DC-004) without deserializing it.
        /// </summary>
        byte OrdinalAt(int index);

        /// <summary>
        /// The record at <paramref name="index"/>, typed. The caller must have established the ordinal
        /// through <see cref="OrdinalAt"/> first; reading a record as the wrong type is a caller bug the
        /// implementation is expected to fail loud on.
        /// </summary>
        T RecordAt<T>(int index) where T : struct;
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                            |
// | 1.0     | 2026-08-13 | —      | Initial implementation (#44 T2, roadmap C2): the three-accessor  |
// |         |            |        | tap shape, declared here because neither #44 nor season-save can |
// |         |            |        | reach #37's identical one (§4.1's reference rule).               |
#endregion
