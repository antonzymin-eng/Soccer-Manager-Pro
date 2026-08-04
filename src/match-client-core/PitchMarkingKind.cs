// File:     src/match-client-core/PitchMarkingKind.cs
// Created:  2026-08-03
// Modified: 2026-08-04
// Author:   —
// Spec:     Interactive Unity client (docs/tracking/interactive-unity-client-design.md §5-P4a, §7
//           "Reuse the geometry that already exists"), Code Standards #20
// Purpose:  The shapes a pitch marking can be. Each member fixes how PitchMarking's fields are read,
//           so the render binding is a switch over this enum and decides nothing itself.

namespace TacticalDirector.MatchClientCore
{
    /// <summary>
    /// What shape a <see cref="PitchMarking"/> describes.
    ///
    /// <para>The set is closed and small on purpose: a renderer binding it is an exhaustive switch
    /// with one drawing primitive per member, which is the §12 rule ("the Unity types assign
    /// transforms and forward input") applied to the pitch. Adding a marking that needs a new shape
    /// means adding a member here — where the gate compiles every consumer of it — rather than
    /// growing a special case inside a <c>MonoBehaviour</c> the gate cannot see.</para>
    /// </summary>
    public enum PitchMarkingKind
    {
        /// <summary>A straight line from <c>A</c> to <c>B</c>. <c>Radius</c> is unused.</summary>
        Line = 0,

        /// <summary>
        /// An axis-aligned rectangle outline from its <b>min</b> corner <c>A</c> to its <b>max</b>
        /// corner <c>B</c>. <c>Radius</c> is unused.
        ///
        /// <para>The ordering IS guaranteed — <c>A.x &lt;= B.x</c> and <c>A.y &lt;= B.y</c>, so
        /// <c>B - A</c> is a non-negative extent a renderer can use directly. Do not re-normalise
        /// defensively; <see cref="PitchMarking.Rectangle"/> has already done it, which is what stops
        /// the away-end boxes (built from their goal line inwards) from arriving inverted.</para>
        /// </summary>
        Rectangle = 1,

        /// <summary>A circle outline centred on <c>A</c> with radius <c>Radius</c>. <c>B</c> is unused.</summary>
        Circle = 2,

        /// <summary>
        /// A filled spot centred on <c>A</c> with radius <c>Radius</c> (the centre spot and the two
        /// penalty spots). <c>B</c> is unused. Distinct from <see cref="Circle"/> because it is
        /// filled rather than stroked, and a renderer that collapsed the two would draw a solid
        /// centre circle.
        /// </summary>
        Spot = 3,

        /// <summary>
        /// The goal mouth, from post to post: a line from <c>A</c> to <c>B</c> drawn heavier than a
        /// marking line. <c>Radius</c> is unused. Its own member rather than a <see cref="Line"/>
        /// because it is goal furniture, not a Law 1 marking, and a View may want it in a different
        /// colour or as a mesh.
        /// </summary>
        GoalMouth = 4,
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-08-03 | —      | Initial creation (P4a): the closed shape set the pitch-marking  |
// |         |            |        | catalogue emits.                                               |
// | 1.1     | 2026-08-04 | —      | AR pass 2 sweep: the Rectangle member still documented corner   |
// |         |            |        | ordering as NOT guaranteed and told consumers to re-normalise  |
// |         |            |        | — the exact contract AR pass 1's H-1 reversed. PitchMarking.cs |
// |         |            |        | was fixed then and this file, beside it, was not, so the two   |
// |         |            |        | stated opposite contracts for one field. The enum is the one a |
// |         |            |        | renderer switching on Kind reads first.                        |
#endregion
