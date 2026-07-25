// File:     src/ui-framework/MatchTacticsDispatcher.cs
// Created:  2026-07-25
// Modified: 2026-07-25
// Author:   —
// Spec:     UI / Client Framework #38 §3.3 (FR-UI-012/013/014/023, failure modes F3/F6),
//           docs/tracking/ui-framework-t0-implementation-plan.md KD-U1/KD-U2, Code Standards #20
// Purpose:  The one concrete dispatcher: routes a ManagerIntent onto the three verified public engine
//           seams (SetTeamTactic / SetPlayerTactic / SubstitutePlayer). During a live streamed match it
//           marshals through the existing deterministic command channel so the seam is called on the
//           thread that owns the engine; in a single-threaded context it applies directly.

using System;

using TacticalDirector.MatchClientCore;

namespace TacticalDirector.UiFramework
{
    /// <summary>
    /// Routes manager intent to the match engine's three public live mutators.
    /// <para>
    /// TWO MODES (#38 §3.3). Which one you get is fixed at construction, because the choice is a
    /// property of the composition, not of the individual intent:
    /// </para>
    /// <list type="bullet">
    /// <item><description><b>Live</b> — constructed from a <see cref="MatchSession"/>. The engine is
    /// owned by the streamer's tick thread and the public seams are not documented cross-thread-safe, so
    /// the intent is <b>enqueued</b> and applied by that thread between ticks (FR-UI-023; a direct
    /// cross-thread call is F6). The dispatcher takes the whole session rather than a bare
    /// <c>ManagerCommandQueue</c> on purpose: a loose queue nobody drains would accept intent forever
    /// and apply none — a silent drop wearing a constructor for a disguise. Taking the session makes the
    /// drained path the only constructible one.</description></item>
    /// <item><description><b>Single-threaded</b> — constructed from an <see cref="ILiveMatchMutations"/>
    /// surface, for pre-kickoff configuration or a turn-based advance with no streamer running. There is
    /// no other thread to race, so the seam is called directly (§3.3).</description></item>
    /// </list>
    /// <para>
    /// MARSHALING MECHANISM (KD-U1): #38 §3.3/§4.1 sketched a new <c>LiveMatchStreamer.EnqueueIntent</c>.
    /// This uses the already-shipped <c>ManagerCommandQueue</c> + pre-tick drain instead, which satisfies
    /// FR-UI-023 identically while leaving <c>LiveMatchStreamer</c> free of any mutation surface — the
    /// browser viewer shares that streamer, and "reachable by a presentation surface" must stay disjoint
    /// from "can mutate the match". Back-propped as ERR-038-001.
    /// </para>
    /// <para>
    /// This type adds no seam. Every path below terminates in <c>ManagerCommand.Apply</c>, the one
    /// already-reviewed piece of code that touches an engine mutator.
    /// </para>
    /// </summary>
    public sealed class MatchTacticsDispatcher : ICommandDispatcher
    {
        // Exactly one of these is non-null; the constructors guarantee it.
        private readonly MatchSession _session;
        private readonly ILiveMatchMutations _mutations;

        /// <summary>
        /// Live-match dispatcher: intent is marshaled onto the session's sim thread and applied between
        /// ticks (FR-UI-023).
        /// </summary>
        /// <param name="session">The live match composition. Must not be null.</param>
        public MatchTacticsDispatcher(MatchSession session)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
        }

        /// <summary>
        /// Single-threaded dispatcher: intent is applied directly, for contexts with no running
        /// streamer (pre-kickoff configuration, turn-based advance).
        /// </summary>
        /// <param name="mutations">The live mutation surface. Must not be null.</param>
        public MatchTacticsDispatcher(ILiveMatchMutations mutations)
        {
            _mutations = mutations ?? throw new ArgumentNullException(nameof(mutations));
        }

        /// <summary>True when this dispatcher marshals onto a sim thread; false when it applies directly.</summary>
        public bool IsLiveMarshaling => _session != null;

        /// <inheritdoc/>
        /// <exception cref="InvalidOperationException">The intent has no mapped public seam (F3),
        /// including the uninitialized <see cref="IntentKind.None"/>.</exception>
        public void Dispatch(in ManagerIntent intent)
        {
            ManagerCommand command = ToCommand(in intent);

            if (_session != null)
            {
                // Write path: enqueue only. The session's driver drains this on the tick thread at the
                // top of the next tick (or on ServiceOnce when paused / at full time).
                _session.Commands.Enqueue(in command);
                return;
            }

            command.Apply(_mutations);
        }

        /// <summary>
        /// Translates a client-wide <see cref="ManagerIntent"/> into the match channel's
        /// <see cref="ManagerCommand"/> (KD-U2). This is the single point where the two vocabularies
        /// meet; <c>CommandDispatchTests.EveryMatchIntentKind_MapsToACommandKind</c> locks them together
        /// so a kind added to one cannot silently go unmapped in the other.
        /// </summary>
        internal static ManagerCommand ToCommand(in ManagerIntent intent)
        {
            switch (intent.Kind)
            {
                case IntentKind.SetTeamTactic:
                {
                    TacticalInstructions.TeamTactic tactic = intent.NewTeamTactic;
                    return ManagerCommand.SetTeamTactic(intent.TeamId, in tactic);
                }
                case IntentKind.SetPlayerTactic:
                {
                    TacticalInstructions.PlayerTactic tactic = intent.NewPlayerTactic;
                    return ManagerCommand.SetPlayerTactic(intent.AgentId, in tactic);
                }
                case IntentKind.Substitute:
                    return ManagerCommand.Substitute(
                        intent.TeamId, intent.OutSlotIndex, intent.BenchIndex, intent.Reason);

                case IntentKind.None:
                    throw new InvalidOperationException(
                        "ManagerIntent is uninitialized (default value); construct it via a factory.");

                default:
                    // F3: an intent kind with no mapped public seam. Never invent one, never drop it.
                    throw new InvalidOperationException(
                        "No public sim seam is mapped for IntentKind " +
                        ((int)intent.Kind).ToString(System.Globalization.CultureInfo.InvariantCulture) +
                        ". A screen needing a new seam files it against the owning spec (FR-UI-020).");
            }
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-25 | —      | Initial creation (#38 T0): live (marshaling) + single-threaded |
// |         |            |        | modes, intent→command translation, fail-loud unmapped intent.  |
// |         |            |        | Code AR pass-1 Medium: live mode takes the MatchSession, not a  |
// |         |            |        | bare ManagerCommandQueue — an undrained queue would have made  |
// |         |            |        | silent-drop constructible.                                     |
#endregion
