// File:     src/client-app/SeasonCommandDispatcher.cs
// Created:  2026-10-18
// Modified: 2026-10-18
// Author:   —
// Spec:     UI / Client Framework #38 Appendix C (the season dispatcher), §3.3 (F3), FR-UI-003/012;
//           docs/tracking/interactive-unity-client-design.md §5-P7, Code Standards #20
// Purpose:  The season dispatcher #38 Appendix C names: routes the three season intents onto the
//           career's progress command, and refuses every other kind fail-loud. A router, never a seam.

using System;

using TacticalDirector.UiFramework;

namespace TacticalDirector.ClientApp
{
    /// <summary>
    /// Career Home's <see cref="ICommandDispatcher"/> (#38 Appendix C season row, extended by
    /// ERR-038-005 to the day and boundary seams). Each season intent maps onto exactly one #30 seam
    /// through <see cref="CareerSession.Execute"/>: <see cref="IntentKind.AdvanceDay"/> →
    /// <c>AdvanceDays(1)</c>, <see cref="IntentKind.AdvanceRound"/> → <c>AdvanceAndPlayNextRound()</c>,
    /// <see cref="IntentKind.StartNextSeason"/> → <c>RollToNextSeason()</c>. Going through the session
    /// rather than the loop directly is what applies the D-P7-3 legality rule and the D-P7-2 autosave
    /// to an intent exactly as to a direct call — one path, not two.
    /// <para>
    /// The dispatcher holds the <see cref="CareerSessionHolder"/> the client swaps careers into rather
    /// than one session, so a single screen registration serves every career the player starts or
    /// loads in a run.
    /// </para>
    /// </summary>
    public sealed class SeasonCommandDispatcher : ICommandDispatcher
    {
        private readonly CareerSessionHolder _holder;

        /// <summary>Creates a dispatcher over the client's career holder.</summary>
        /// <exception cref="ArgumentNullException"><paramref name="holder"/> is null.</exception>
        public SeasonCommandDispatcher(CareerSessionHolder holder)
        {
            _holder = holder ?? throw new ArgumentNullException(nameof(holder));
        }

        /// <inheritdoc />
        /// <exception cref="InvalidOperationException">The intent is not a season intent (F3), no career
        /// is active, or the action is not legal now.</exception>
        public void Dispatch(in ManagerIntent intent)
        {
            if (intent.Kind == IntentKind.None)
            {
                throw new InvalidOperationException(
                    "ManagerIntent is uninitialized (default value); construct it via a factory.");
            }

            if (!IntentRouting.IsSeasonIntent(intent.Kind))
            {
                // F3: never invent a seam, never drop the intent.
                throw new InvalidOperationException(
                    "IntentKind." + intent.Kind + " is not a season intent; the season dispatcher has no seam for it.");
            }

            _holder.Require().Execute(CareerProgress.ActionFor(intent.Kind));
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-10-18 | —      | Initial creation (§5-P7): the #38 Appendix C season dispatcher.|
#endregion
