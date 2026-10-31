// File:     src/client-app/ClientShellRootSnapshot.cs
// Created:  2026-09-04
// Modified: 2026-10-05 (PR #470 review: ancestor activation fact)
// Author:   —
// Spec:     docs/tracking/interactive-unity-client-design.md §5-P5a / §5-P5b,
//           Code Standards #20 §12 rule 1
// Purpose:  Host-free structural snapshot of one Unity shell/root node. The Unity binding collects
//           instance ids, ancestor ids and ancestor activation; every decision over that data stays
//           gate-compiled here.

using System;

namespace TacticalDirector.ClientApp
{
    /// <summary>
    /// Immutable structural facts about one client-shell GameObject: its host identity, saved active
    /// state, whether every ancestor is active, and the identities of its ancestors. The type deliberately contains no Unity reference,
    /// so P5b wiring rules can be exercised by the normal gate rather than living in the excluded
    /// <c>match-client-unity</c> assembly.
    /// </summary>
    public readonly struct ClientShellRootSnapshot
    {
        private readonly int[] _ancestorInstanceIds;

        /// <summary>The host object's instance identity. Zero represents an unassigned root.</summary>
        public readonly int InstanceId;

        /// <summary>The host object's own saved active flag; parent activity is reported separately.</summary>
        public readonly bool IsActiveSelf;

        /// <summary>
        /// True when every ancestor is active (in Unity: no parent, or the parent is active in the
        /// hierarchy). When false, toggling this node's own active flag cannot make it visible.
        /// </summary>
        public readonly bool AreAncestorsActive;

        /// <summary>Constructs a snapshot and copies the ancestor list so caller memory cannot mutate it.</summary>
        public ClientShellRootSnapshot(
            int instanceId,
            bool isActiveSelf,
            bool areAncestorsActive,
            int[] ancestorInstanceIds)
        {
            InstanceId = instanceId;
            IsActiveSelf = isActiveSelf;
            AreAncestorsActive = areAncestorsActive;

            if (ancestorInstanceIds == null || ancestorInstanceIds.Length == 0)
            {
                _ancestorInstanceIds = Array.Empty<int>();
                return;
            }

            _ancestorInstanceIds = new int[ancestorInstanceIds.Length];
            Array.Copy(ancestorInstanceIds, _ancestorInstanceIds, ancestorInstanceIds.Length);
        }

        /// <summary>True when <paramref name="instanceId"/> occurs in this node's ancestor chain.</summary>
        public bool HasAncestor(int instanceId)
        {
            if (_ancestorInstanceIds == null)
            {
                return false;
            }

            for (int i = 0; i < _ancestorInstanceIds.Length; i++)
            {
                if (_ancestorInstanceIds[i] == instanceId)
                {
                    return true;
                }
            }

            return false;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-09-04 | —      | P5b review extraction: immutable host-free root/ancestor facts. |
// | 1.1     | 2026-10-05 | —      | PR #470 review: AreAncestorsActive host fact, so a root hidden |
// |         |            |        | by an inactive ancestor is refused instead of shown blank.     |
#endregion
