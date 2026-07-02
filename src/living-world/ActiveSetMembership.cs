// File:     src/living-world/ActiveSetMembership.cs
// Created:  2026-07-02
// Modified: 2026-07-02
// Author:   —
// Spec:     Living World System #22 §3.5, §4.3, §4.5, FR-LW-021/023/025, Code Standards #20
// Purpose:  §3.5 active-set membership: entry on first interaction, deterministic LRU demotion of
//           external contacts at the cap (skipping arc-pinned edges), own-club at-club exemption with
//           the FR-LW-025 departure path, and cold-store promotion honouring the TryTake ordering.

using System;
using System.Collections.Generic;

namespace TacticalDirector.LivingWorld
{
    /// <summary>
    /// Tracks which contacts of one manager are in the deep-tier active set (#22 §3.5 / FR-LW-023)
    /// and moves edges between the <see cref="MemoryStore"/> (deep tier) and the
    /// <see cref="ColdStore"/> (background tier) on entry/exit:
    ///
    /// - **Entry**: a contact enters on first interaction (<see cref="RecordInteraction"/>). If it has
    ///   cold history, the summary is promoted (verify-no-live-edge FIRST, then
    ///   <c>TryTake → Rehydrate → InsertEdge</c> — the destructive-take ordering contract documented
    ///   on <see cref="ColdStore.TryTake"/>, FR-LW-025); otherwise a fresh strangers-baseline edge is
    ///   created.
    /// - **Exit (LRU)**: when external contacts exceed <c>ACTIVE_SET_EXTERNAL_CONTACTS_MAX</c>, the
    ///   least-recently-interacted external contact demotes — last-interaction is the max episode
    ///   worldTick on its edge, ties broken by lowest EntityId — skipping any contact whose edge holds
    ///   a live arc-pinned episode (FR-LW-021/023; demoting one would orphan the arc, F1). If every
    ///   candidate is pinned the set grows transiently past the cap, the demotion-path analogue of the
    ///   §3.2 all-pinned transient growth, and shrinks on the next enforcement after arcs resolve.
    /// - **Own-club**: squad/staff/board members never LRU-demote while at the club; on leaving
    ///   (<see cref="Depart"/> — transfer-out, release, sacking) they demote via the same path as an
    ///   external contact, preserving re-sign/face-again history (FR-LW-025).
    ///
    /// Members are kept sorted by EntityId so every pass is canonical (FR-LW-021). The membership
    /// roster (entityId, isOwnClub) is serialisable value state (FR-LW-022); the §4.6 snapshot-schema
    /// entry remains a follow-up. Day-cadence: not subject to the 60 Hz hot-path rules. The
    /// BackgroundTierSim summary *update* (reflecting club-AI outcomes) stays a KD-10 seam — this
    /// service owns only membership and the demotion/promotion transitions.
    /// </summary>
    public sealed class ActiveSetMembership
    {
        private struct Member
        {
            public int EntityId;
            public bool IsOwnClub;
        }

        private readonly int _managerId;
        private readonly MemoryStore _memory;
        private readonly ColdStore _cold;
        private readonly List<Member> _members = new List<Member>(); // sorted by EntityId (FR-LW-021)

        /// <summary>Number of contacts in the active set (own-club + external).</summary>
        public int ActiveCount => _members.Count;

        /// <summary>Number of external (non-own-club) contacts — the population the FR-LW-023 cap bounds.</summary>
        public int ExternalCount
        {
            get
            {
                int n = 0;
                for (int i = 0; i < _members.Count; i++)
                {
                    if (!_members[i].IsOwnClub)
                    {
                        n++;
                    }
                }
                return n;
            }
        }

        public ActiveSetMembership(int managerId, MemoryStore memory, ColdStore cold)
        {
            if (managerId < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(managerId), managerId,
                    "ActiveSetMembership: manager id must be non-negative.");
            }
            _managerId = managerId;
            _memory = memory ?? throw new ArgumentNullException(nameof(memory));
            _cold = cold ?? throw new ArgumentNullException(nameof(cold));
        }

        /// <summary>True if the contact is currently in the active set.</summary>
        public bool IsActive(int entityId)
        {
            FindMemberIndex(entityId, out bool found);
            return found;
        }

        /// <summary>True if the contact is an active own-club member (at-club LRU exemption, §3.5).</summary>
        public bool IsOwnClubMember(int entityId)
        {
            int idx = FindMemberIndex(entityId, out bool found);
            return found && _members[idx].IsOwnClub;
        }

        /// <summary>
        /// Records one interaction with a contact: enters it into the active set if absent (promoting
        /// cold history when present — on that path the rehydrated summary's stored ActiveLayers mask
        /// wins and <paramref name="activeLayers"/> is only checked for consistency), appends the
        /// §3.2 episode (which advances the contact's last-interaction to <paramref name="worldTick"/>),
        /// then enforces the external cap. A re-entering ex-member re-enters with the class the caller
        /// declares (re-signing an ex-player makes it own-club again); an ACTIVE member's class cannot
        /// flip here — own-club exit goes through <see cref="Depart"/> (fail loud on drift).
        /// </summary>
        /// <returns>The allocated episodeId — the durable handle an arc pins (FR-LW-009).</returns>
        public uint RecordInteraction(int entityId, bool isOwnClub, byte activeLayers, EventKind kind, uint worldTick, ushort managerChoiceId)
        {
            int idx = FindMemberIndex(entityId, out bool found);
            if (found)
            {
                if (_members[idx].IsOwnClub != isOwnClub)
                {
                    throw new InvalidOperationException(
                        "ActiveSetMembership.RecordInteraction: membership class cannot flip on an interaction — own-club exit goes through Depart (§3.5).");
                }
                if (!_memory.TryGetEdge(_managerId, entityId, out RelationshipEdge live))
                {
                    throw new InvalidOperationException(
                        "ActiveSetMembership.RecordInteraction: active member has no live edge — membership bookkeeping drifted (FR-LW-025).");
                }
                if (live.ActiveLayers != activeLayers)
                {
                    throw new InvalidOperationException(
                        "ActiveSetMembership.RecordInteraction: ActiveLayers mask conflicts with the live edge (AR-1 L-1 parallel — a silent mismatch misroutes updates).");
                }
            }
            else
            {
                // §3.5 entry. ORDERING (ColdStore.TryTake contract): verify no live edge exists BEFORE
                // the destructive take — a post-take InsertEdge throw would strand the summary outside
                // both tiers (FR-LW-025 neither-loses-nor-duplicates).
                if (_memory.TryGetEdge(_managerId, entityId, out _))
                {
                    throw new InvalidOperationException(
                        "ActiveSetMembership.RecordInteraction: live edge exists for a non-member — membership bookkeeping drifted (FR-LW-025).");
                }
                if (_cold.TryTake(entityId, out ColdSummary summary))
                {
                    _memory.InsertEdge(ColdStore.Rehydrate(summary, _managerId));
                }
                else
                {
                    _memory.GetOrCreateEdge(_managerId, entityId, activeLayers);
                }
                _members.Insert(idx, new Member { EntityId = entityId, IsOwnClub = isOwnClub });
            }

            uint episodeId = _memory.RecordEpisode(_managerId, entityId, kind, worldTick, managerChoiceId);
            EnforceExternalCap();
            return episodeId;
        }

        /// <summary>
        /// Own-club exit (transfer-out, release, sacking): reclassifies the member as external and
        /// demotes it to cold-store via the same path as an external contact (FR-LW-025). If the
        /// contact's edge holds a live arc-pinned episode, demotion is deferred — the contact stays
        /// active *as external* until the arc resolves, after which the LRU/cap enforcement demotes it
        /// (the §3.5 demotion-skip rule; demoting now would orphan the arc, F1).
        /// </summary>
        public void Depart(int entityId)
        {
            int idx = FindMemberIndex(entityId, out bool found);
            if (!found || !_members[idx].IsOwnClub)
            {
                throw new InvalidOperationException(
                    "ActiveSetMembership.Depart: not an active own-club member — Depart is the own-club exit path (§3.5).");
            }
            if (_memory.EdgeHasPinnedEpisode(_managerId, entityId))
            {
                Member m = _members[idx];
                m.IsOwnClub = false;
                _members[idx] = m;
                EnforceExternalCap(); // The conversion itself can push the external population over the cap.
                return;
            }
            DemoteAt(idx);
        }

        /// <summary>
        /// §3.5 LRU demotion at the cap (the WorldLoop phase-6 budget hook): while more than
        /// <c>ACTIVE_SET_EXTERNAL_CONTACTS_MAX</c> external contacts are active, demotes the
        /// least-recently-interacted eligible one (max episode worldTick; ties → lowest EntityId;
        /// arc-pinned edges skipped). Stops — transiently over cap — when every candidate is pinned.
        /// </summary>
        /// <returns>The number of contacts demoted.</returns>
        public int EnforceExternalCap()
        {
            int demoted = 0;
            while (ExternalCount > LivingWorldConstants.ACTIVE_SET_EXTERNAL_CONTACTS_MAX)
            {
                int lru = SelectLruExternalIndex();
                if (lru < 0)
                {
                    break; // Every external contact is arc-pinned — bounded by live arcs + the §4.5 budget.
                }
                DemoteAt(lru);
                demoted++;
            }
            return demoted;
        }

        // ── internals ──────────────────────────────────────────────────────────────────────

        /// <summary>Demotes the member at the given index: RemoveEdge → Compress → ColdStore.Add (§3.5).</summary>
        private void DemoteAt(int memberIdx)
        {
            int entityId = _members[memberIdx].EntityId;
            RelationshipEdge edge = _memory.RemoveEdge(_managerId, entityId);
            _cold.Add(ColdStore.Compress(edge, LivingWorldConstants.COLD_SUMMARY_RETAINED_EPISODES));
            _members.RemoveAt(memberIdx);
        }

        /// <summary>
        /// Selects the least-recently-interacted eligible external contact (FR-LW-021/023). Iterates
        /// ascending EntityId with a strict &lt; compare, so worldTick ties resolve to the lowest
        /// EntityId. Returns -1 when no external contact is eligible (all pinned, or none external).
        /// </summary>
        private int SelectLruExternalIndex()
        {
            int best = -1;
            uint bestTick = 0u;
            for (int i = 0; i < _members.Count; i++)
            {
                Member m = _members[i];
                if (m.IsOwnClub)
                {
                    continue;
                }
                if (_memory.EdgeHasPinnedEpisode(_managerId, m.EntityId))
                {
                    continue;
                }
                uint last = LastInteractionTick(m.EntityId);
                if (best < 0 || last < bestTick)
                {
                    best = i;
                    bestTick = last;
                }
            }
            return best;
        }

        /// <summary>
        /// Last-interaction = max episode worldTick on the contact's edge (§3.5). An empty buffer
        /// (e.g. rehydrated with zero retained episodes, then never interacted — unreachable through
        /// RecordInteraction, which always appends) reads 0 = oldest possible.
        /// </summary>
        private uint LastInteractionTick(int entityId)
        {
            if (!_memory.TryGetEdge(_managerId, entityId, out RelationshipEdge edge))
            {
                throw new InvalidOperationException(
                    "ActiveSetMembership: active member has no live edge — membership bookkeeping drifted (FR-LW-025).");
            }
            uint last = 0u;
            for (int i = 0; i < edge.Memory.Length; i++)
            {
                if (edge.Memory[i].WorldTick > last)
                {
                    last = edge.Memory[i].WorldTick;
                }
            }
            return last;
        }

        /// <summary>Binary search on the canonical EntityId order. Returns the match or insertion index.</summary>
        private int FindMemberIndex(int entityId, out bool found)
        {
            int lo = 0;
            int hi = _members.Count - 1;
            while (lo <= hi)
            {
                int mid = (lo + hi) >> 1;
                int c = _members[mid].EntityId.CompareTo(entityId);
                if (c == 0)
                {
                    found = true;
                    return mid;
                }
                if (c < 0)
                {
                    lo = mid + 1;
                }
                else
                {
                    hi = mid - 1;
                }
            }
            found = false;
            return lo;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-02 | —      | Initial implementation (season/world loop slice 2): §3.5      |
// |         |            |        | entry-on-interaction with cold-store promotion (verify-live-  |
// |         |            |        | edge-first TryTake ordering, FR-LW-025), deterministic LRU    |
// |         |            |        | demotion at the FR-LW-023 cap (pinned edges skipped; ties →   |
// |         |            |        | lowest EntityId), own-club at-club exemption + Depart path    |
// |         |            |        | (pinned departure defers as external).                        |
#endregion
