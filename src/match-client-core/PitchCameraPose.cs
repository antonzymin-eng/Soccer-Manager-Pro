// File:     src/match-client-core/PitchCameraPose.cs
// Created:  2026-08-04
// Modified: 2026-08-04
// Author:   —
// Spec:     Interactive Unity client (docs/tracking/interactive-unity-client-design.md §5-P4a, §7
//           "Rendering, camera, HUD", §12 rule 1), Code Standards #20
// Purpose:  Where the camera sits and what it looks at, in world space — the resolved placement a
//           binding assigns, carrying no rotation type so the CI shim's surface is enough.

using UnityEngine;

namespace TacticalDirector.MatchClientCore
{
    /// <summary>
    /// A resolved camera placement: a world position and the world point it is aimed at.
    ///
    /// <para><b>Why a look-at point rather than a rotation.</b> <c>Quaternion</c> is not in the CI
    /// shim's surface, and adding it to buy coverage is the same bargain §12 refuses for
    /// <c>MonoBehaviour</c>. Two points say everything the placement needs to say, are trivially
    /// assertable, and leave the Unity side calling <c>transform.LookAt</c> — binding, with no
    /// decision in it.</para>
    /// </summary>
    public readonly struct PitchCameraPose
    {
        /// <summary>Camera position in world space (metres, centre-origin, +Y up).</summary>
        public readonly Vector3 Position;

        /// <summary>The world-space point the camera is aimed at — always on the ground plane.</summary>
        public readonly Vector3 LookAt;

        /// <summary>Constructs a placement. Built by <see cref="PitchCameraRig"/>.</summary>
        public PitchCameraPose(Vector3 position, Vector3 lookAt)
        {
            Position = position;
            LookAt   = lookAt;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-08-04 | —      | Initial creation (tilted-view revision): the resolved camera    |
// |         |            |        | placement as two world points, so no rotation type is needed —  |
// |         |            |        | Quaternion is not in the shim, and widening it to buy coverage  |
// |         |            |        | is the bargain §12 already refuses for MonoBehaviour.           |
#endregion
