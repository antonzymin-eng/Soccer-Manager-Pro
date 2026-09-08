// File:     src/client-app/ClientShellRootSnapshot.cs
// Created:  2026-09-04
// Modified: 2026-09-04
// Author:   —
// Spec:     docs/tracking/interactive-unity-client-design.md §5-P5a / §5-P5b,
//           Code Standards #20 §12 rule 1
// Purpose:  Host-free structural snapshot of one Unity shell/root node. The Unity binding collects
//           instance ids and ancestor ids; every decision over that data stays gate-compiled here.

using System;

namespace TacticalDirector.ClientApp
{
    /// <summary>
    /// Immutable structural facts about one client-shell GameObject: its host identity, saved active
    /// state, and the identities of its ancestors. The type deliberately contains no Unity reference,
    /// so P5b wiring rules can be exercised by the normal gate rather than living in the excluded
    /// <c>match-client-unity</c> assembly.
    /// </summary>
    public readonly struct ClientShellRootSnapshot
    {
        private readonly int[] _ancestorInstanceIds;

        /// <summary>The host object's instance identity. Zero represents an unassigned root.</summary>
        public readonly int InstanceId;

        /// <summary>The host object's own saved active flag; parent activity is irrelevant here.</summary>
        public readonly bool IsActiveSelf;

        /// <summary>Constructs a snapshot and copies the ancestor list so caller memory cannot mutate it.</summary>
        public ClientShellRootSnapshot(int instanceId, bool isActiveSelf, int[] ancestorInstanceIds)
        {
            InstanceId = instanceId;
            IsActiveSelf = isActiveSelf;

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
#endregion
