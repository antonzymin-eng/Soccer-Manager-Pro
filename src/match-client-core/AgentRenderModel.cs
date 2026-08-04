// File:     src/match-client-core/AgentRenderModel.cs
// Created:  2026-08-03
// Modified: 2026-08-04
// Author:   —
// Spec:     Interactive Unity client (docs/tracking/interactive-unity-client-design.md §5-P4a, §12
//           rule 1), Code Standards #20
// Purpose:  Everything a renderer needs to draw one agent this display frame, already decided:
//           where, how big, which team, which cues. The binding assigns a transform and picks a
//           colour from TeamId; it makes no further judgement.

using UnityEngine;

namespace TacticalDirector.MatchClientCore
{
    /// <summary>
    /// One agent's fully-resolved draw state (§5-P4a).
    ///
    /// <para><b>Why this exists rather than the renderer reading the frame directly.</b> §12's
    /// standing rule is that nothing which decides may live in a <c>MonoBehaviour</c>, because the
    /// CI gate cannot compile one. "Which slot is the keeper", "is this agent in possession", "how
    /// big is the ring", "which shirt number goes on the marker" are all decisions. Resolving them
    /// here puts them where the gate compiles and tests them, and leaves the Unity side assigning
    /// <c>transform.position</c> and <c>localScale</c> — binding, which a cert run genuinely
    /// verifies, rather than behaviour, which it verifies only where someone thought to look.</para>
    ///
    /// <para>Deliberately colour-free. A team's colours are a skin decision with no correct answer a
    /// test could assert, and <c>UnityEngine.Color</c> is not in the CI shim's surface. The renderer
    /// maps <see cref="TeamId"/> to a palette; everything upstream of that is here.</para>
    /// </summary>
    public readonly struct AgentRenderModel
    {
        /// <summary>The roster slot this model draws.</summary>
        public readonly int RosterIndex;

        /// <summary>Team id (0 = home, 1 = away) — the renderer's palette key.</summary>
        public readonly int TeamId;

        /// <summary>Shirt number on the marker (1-based within the team; see <see cref="MatchRoster"/>).</summary>
        public readonly int ShirtNumber;

        /// <summary>
        /// Position in world space, already projected and already interpolated — on the ground plane
        /// (world Y = 0), since agents do not leave the turf in the sim's model.
        /// </summary>
        public readonly Vector3 WorldPosition;

        /// <summary>
        /// Marker radius in world units (1 unit = 1 m). Stored rather than derived because a marker size is
        /// legitimately a per-agent quantity — nothing today varies it, but a future skin might.
        /// </summary>
        public readonly float MarkerRadius;

        /// <summary>True when this agent is the one in possession of the ball.</summary>
        public readonly bool HasBall;

        /// <summary>
        /// Radius of the possession ring in world units, or 0 when this agent is not in possession.
        /// A zero means "draw no ring", so the renderer needs no separate boolean and cannot draw a
        /// ring of a size nobody chose.
        ///
        /// <para><b>Derived from <see cref="HasBall"/>, not the other way round.</b> The two cannot
        /// disagree either way, but only this direction keeps a question about the simulation —
        /// "who has the ball" — independent of a [GT] presentation size. With the derivation
        /// inverted, a config setting <c>PossessionRingRadiusM</c> to zero would have answered "no
        /// one" for every agent in the match.</para>
        /// </summary>
        public float PossessionRingRadius =>
            HasBall ? MatchClientConstants.PossessionRingRadiusM : 0f;

        /// <summary>True when this slot is currently occupied by a goalkeeper (live, per-frame).</summary>
        public readonly bool IsGoalkeeper;

        /// <summary>Yellow cards held (0 or 1 — a second is promoted to a sending-off).</summary>
        public readonly int YellowCards;

        /// <summary>
        /// True when this agent has been sent off. They keep a position — the engine still carries
        /// them as a body — so a renderer marks them rather than hiding them; hiding a body that is
        /// still on the pitch would misrepresent what the simulation is doing.
        /// </summary>
        public readonly bool IsSentOff;

        /// <summary>True when a substitute currently occupies this slot.</summary>
        public readonly bool IsSubstitute;

        /// <summary>
        /// Constructs one agent's draw state. Built by <see cref="MatchRenderProjection"/>.
        ///
        /// <para><b>On the parameter list.</b> Three adjacent <c>int</c>s and three adjacent
        /// <c>bool</c>s is the transposable shape <c>LiveMatchFrame</c>'s AR-1 M-6 collapsed into
        /// carriers. It is left flat here because this type has exactly ONE production construction
        /// site — the projector — where a carrier would add a hop without removing a decision, and
        /// because <c>MatchRenderProjectionTests</c> asserts every field against a distinct source
        /// value, which is the guard the carriers were bought for. If a second construction site
        /// ever appears, revisit that trade.</para>
        /// </summary>
        public AgentRenderModel(
            int rosterIndex,
            int teamId,
            int shirtNumber,
            Vector3 worldPosition,
            float markerRadius,
            bool hasBall,
            bool isGoalkeeper,
            int yellowCards,
            bool isSentOff,
            bool isSubstitute)
        {
            RosterIndex   = rosterIndex;
            TeamId        = teamId;
            ShirtNumber   = shirtNumber;
            WorldPosition = worldPosition;
            MarkerRadius  = markerRadius;
            HasBall       = hasBall;
            IsGoalkeeper  = isGoalkeeper;
            YellowCards   = yellowCards;
            IsSentOff     = isSentOff;
            IsSubstitute  = isSubstitute;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-08-03 | —      | Initial creation (P4a): per-agent resolved draw state. HasBall  |
// |         |            |        | is derived from the ring radius rather than stored, so the two  |
// |         |            |        | cannot disagree.                                                |
// | 1.1     | 2026-08-04 | —      | AR pass M-3: the HasBall/ring derivation is INVERTED. HasBall  |
// |         |            |        | is now the stored fact and PossessionRingRadius derives from   |
// |         |            |        | it. The two still cannot disagree, but a question about the    |
// |         |            |        | simulation no longer depends on a [GT] presentation size — with|
// |         |            |        | the old direction, a config setting PossessionRingRadiusM to   |
// |         |            |        | zero answered "nobody has the ball" for the whole match.       |
// | 1.2     | 2026-08-04 | —      | Tilted-view revision (owner call): ViewPosition (Vector2, a    |
// |         |            |        | flat view plane) becomes WorldPosition (Vector3, on the ground |
// |         |            |        | plane) now that the scene has a real third axis and a tilted   |
// |         |            |        | camera projects it.                                            |
#endregion
