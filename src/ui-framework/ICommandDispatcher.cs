// File:     src/ui-framework/ICommandDispatcher.cs
// Created:  2026-07-25
// Modified: 2026-07-25
// Author:   —
// Spec:     UI / Client Framework #38 §2.2 / §3.3 (FR-UI-003/012/014, failure mode F3),
//           docs/tracking/ui-framework-t0-implementation-plan.md, Code Standards #20
// Purpose:  The dispatch contract: map a typed ManagerIntent onto an EXISTING public sim seam. The
//           framework provides no mutation path of its own — a dispatcher is a router, never a seam.

namespace TacticalDirector.UiFramework
{
    /// <summary>
    /// Routes a <see cref="ManagerIntent"/> to an existing public sim/loop seam (#38 §3.3).
    /// <para>
    /// The contract:
    /// </para>
    /// <list type="bullet">
    /// <item><description>An implementation MUST map only onto <b>public, sim-validated</b> seams. It
    /// carries no seam of its own and invents none (FR-UI-003/012 / KD-4); the assembly boundary is what
    /// makes this enforceable rather than merely promised — sim internals are not visible here.</description></item>
    /// <item><description>An intent with no mapped seam MUST <b>throw</b> (F3) — never a silent drop and
    /// never an improvised alternative. A manager whose instruction vanished without a word is worse
    /// than one who saw an error.</description></item>
    /// <item><description>Validation of the intent's <i>content</i> (a real team id, a legal
    /// substitution) belongs to the seam owner, not here. A dispatcher that pre-validated would drift
    /// out of sync with the rules it is duplicating.</description></item>
    /// </list>
    /// </summary>
    public interface ICommandDispatcher
    {
        /// <summary>
        /// Routes <paramref name="intent"/> to its seam. Throws when the intent has no mapped seam,
        /// including the uninitialized <see cref="IntentKind.None"/> (F3).
        /// </summary>
        void Dispatch(in ManagerIntent intent);
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-25 | —      | Initial creation (#38 T0): the routing contract, fail-loud on  |
// |         |            |        | an unmapped intent, validation left to the seam owner.         |
#endregion
