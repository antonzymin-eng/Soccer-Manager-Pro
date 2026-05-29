// File:     src/attacking-ai/InvariantEnforcer.cs
// Created:  2026-05-29
// Modified: 2026-05-29
// Author:   —
// Spec:     Attacking AI #15 §3.11, FR-AT-018–FR-AT-021, FR-AT-026, Code Standards #20
// Purpose:  Three anti-chaos invariants enforced post-assignment pre-publication.
//           Invariant 1: total RUNNERs <= MAX_RUNNERS. Invariant 2: SUPPORT_BALL+HOLD_WIDTH >= MIN.
//           Invariant 3: no RUNNER run-target past OWN_HALF_RUN_BLOCK_M. Fallback = all HOLD_WIDTH.

using UnityEngine;

namespace TacticalDirector.AttackingAI
{
    /// <summary>
    /// Anti-chaos invariant enforcement. Pure static. Runs post-assignment, pre-publication
    /// (FR-AT-021). Iterates up to MAX_INVARIANT_PASSES times; if invariants still violated,
    /// applies the all-default fallback (FR-AT-026). EntityId tiebreak is implicitly
    /// ascending because pool entries are already EntityId-sorted.
    /// Attacking AI #15 §3.11.
    /// </summary>
    internal static class InvariantEnforcer
    {
        /// <summary>
        /// Applies all three invariants. Returns true when invariants are satisfied; false when
        /// the fallback was required. Mutates <paramref name="pool"/> entries in place.
        /// </summary>
        public static bool Apply(
            AttackPoolEntry[] pool,
            int               poolCount,
            StyleProfile      styleProfile,
            float             teamAttackAngle)
        {
            int passes = 0;
            while (AnyViolated(pool, poolCount, styleProfile, teamAttackAngle)
                   && passes < AttackingAIConstants.MaxInvariantPasses)
            {
                EnforceOnce(pool, poolCount, styleProfile, teamAttackAngle);
                passes++;
            }

            if (AnyViolated(pool, poolCount, styleProfile, teamAttackAngle))
            {
                ApplyFallback(pool, poolCount);
                return false;
            }

            return true;
        }

        // ── Private helpers ─────────────────────────────────────────────────────

        private static bool AnyViolated(
            AttackPoolEntry[] pool, int poolCount,
            StyleProfile styleProfile, float teamAttackAngle)
        {
            return RunnerCountExceeds(pool, poolCount, styleProfile.MaxRunners)
                || SupportCountBelow(pool, poolCount)
                || OwnHalfRunnerExists(pool, poolCount, teamAttackAngle);
        }

        private static void EnforceOnce(
            AttackPoolEntry[] pool, int poolCount,
            StyleProfile styleProfile, float teamAttackAngle)
        {
            EnforceMaxRunners(pool, poolCount, styleProfile.MaxRunners);
            EnforceMinSupport(pool, poolCount);
            EnforceOwnHalfBlock(pool, poolCount, teamAttackAngle);
        }

        // Invariant 1: RUNNER count <= MAX_RUNNERS. Demote shallowest runner (§3.11).
        private static bool RunnerCountExceeds(AttackPoolEntry[] pool, int count, int maxRunners)
        {
            int n = 0;
            for (int i = 0; i < count; i++)
                if (pool[i].AssignedRole == AttackRole.Runner) n++;
            return n > maxRunners;
        }

        private static void EnforceMaxRunners(AttackPoolEntry[] pool, int count, int maxRunners)
        {
            while (true)
            {
                int runnerCount = 0;
                for (int i = 0; i < count; i++)
                    if (pool[i].AssignedRole == AttackRole.Runner) runnerCount++;
                if (runnerCount <= maxRunners) break;

                int    demotee   = FindShallowRunner(pool, count);
                if (demotee < 0) break;
                pool[demotee].AssignedRole  = AttackRole.SupportBall;
                pool[demotee].HasRunParams  = false;
            }
        }

        // Invariant 2: (SUPPORT_BALL + HOLD_WIDTH) >= MIN_SUPPORT_AGENTS. Demote shallowest runner.
        private static bool SupportCountBelow(AttackPoolEntry[] pool, int count)
        {
            int n = 0;
            for (int i = 0; i < count; i++)
            {
                AttackRole r = pool[i].AssignedRole;
                if (r == AttackRole.SupportBall || r == AttackRole.HoldWidth) n++;
            }
            return n < AttackingAIConstants.MinSupportAgents;
        }

        private static void EnforceMinSupport(AttackPoolEntry[] pool, int count)
        {
            while (true)
            {
                int supportCount = 0;
                for (int i = 0; i < count; i++)
                {
                    AttackRole r = pool[i].AssignedRole;
                    if (r == AttackRole.SupportBall || r == AttackRole.HoldWidth) supportCount++;
                }
                if (supportCount >= AttackingAIConstants.MinSupportAgents) break;

                int demotee = FindShallowRunner(pool, count);
                if (demotee < 0) break; // no runners to demote
                pool[demotee].AssignedRole = AttackRole.SupportBall;
                pool[demotee].HasRunParams = false;
            }
        }

        // Invariant 3: no RUNNER run-target is > OWN_HALF_RUN_BLOCK_M past the half-line in own half.
        private static bool OwnHalfRunnerExists(AttackPoolEntry[] pool, int count, float teamAttackAngle)
        {
            for (int i = 0; i < count; i++)
            {
                ref AttackPoolEntry e = ref pool[i];
                if (e.AssignedRole != AttackRole.Runner || !e.HasRunParams) continue;
                if (DepthIntoOwnHalf(e.RunTargetPosition.x, teamAttackAngle)
                    > AttackingAIConstants.OwnHalfRunBlockM)
                    return true;
            }
            return false;
        }

        private static void EnforceOwnHalfBlock(AttackPoolEntry[] pool, int count, float teamAttackAngle)
        {
            for (int i = 0; i < count; i++)
            {
                ref AttackPoolEntry e = ref pool[i];
                if (e.AssignedRole != AttackRole.Runner || !e.HasRunParams) continue;
                if (DepthIntoOwnHalf(e.RunTargetPosition.x, teamAttackAngle)
                    > AttackingAIConstants.OwnHalfRunBlockM)
                {
                    e.AssignedRole = AttackRole.HoldWidth;
                    e.HasRunParams = false;
                }
            }
        }

        // Returns positive metres if runTargetX is in own half; negative if in opponent half.
        private static float DepthIntoOwnHalf(float runTargetX, float teamAttackAngle)
        {
            if (Mathf.Abs(teamAttackAngle) < AttackingAIConstants.AttackAngleEpsilon)
                return AttackingAIConstants.HALF_LINE_X - runTargetX; // attacking x=105
            else
                return runTargetX - AttackingAIConstants.HALF_LINE_X; // attacking x=0 (π)
        }

        // Finds the pool index of the RUNNER with smallest depthOffset (shallowest run).
        // EntityId tiebreak: since pool is EntityId-ascending, first minimum wins.
        private static int FindShallowRunner(AttackPoolEntry[] pool, int count)
        {
            int   bestIdx  = -1;
            float bestDepth = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                ref AttackPoolEntry e = ref pool[i];
                if (e.AssignedRole != AttackRole.Runner) continue;
                if (e.DepthOffsetM < bestDepth)
                {
                    bestDepth = e.DepthOffsetM;
                    bestIdx   = i;
                }
            }
            return bestIdx;
        }

        // Fallback: all agents → HOLD_WIDTH (or SUPPORT_BALL for those within support floor).
        // No RUNNER, no WEAK_SIDE. Simplest safe state (FR-AT-026).
        private static void ApplyFallback(AttackPoolEntry[] pool, int count)
        {
            for (int i = 0; i < count; i++)
            {
                AttackRole r = pool[i].AssignedRole;
                if (r == AttackRole.Runner || r == AttackRole.WeakSide)
                {
                    pool[i].AssignedRole = AttackRole.HoldWidth;
                    pool[i].HasRunParams = false;
                }
            }
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-29 | —      | Initial implementation. |
// | 1.1     | 2026-05-29 | —      | AR-3 L-1: 0.01f angle-epsilon literal → AttackingAIConstants.AttackAngleEpsilon (FR-CS-016). |
#endregion
