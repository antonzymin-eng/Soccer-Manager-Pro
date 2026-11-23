// File:     src/defensive-ai/Tests/TackleRequestReconciliationTests.cs
// Created:  2026-11-21
// Modified: 2026-11-21
// Author:   —
// Spec:     Defensive AI #14 §3.6, §3.13 (Step 8a), §4.3, FR-DA-023, ERR-014-010 amendment 2, Code Standards #20
// Purpose:  W13-F9 regression: a tackle request is published only for an agent whose FINAL assignment (after the
//           offside trap and the anti-chaos invariants) still targets that opponent.

using NUnit.Framework;

using UnityEngine;

using TacticalDirector.PositioningAI;

namespace TacticalDirector.DefensiveAI.Tests
{
    /// <summary>
    /// W13-F9: two midfielders 1 m apart both prefer the same attacker; with mark movement active Invariant 4 (one marker
    /// per opponent) demotes the lower-priority duplicate AFTER Step 6 has evaluated tackle intents. Before the fix the
    /// demoted agent still published a COMMIT against the man he no longer marked (agent 17 at ticks 3, 7, 11 — the
    /// retained movement-review reproduction). Both team ids, identical canonical inputs.
    /// </summary>
    [TestFixture]
    public sealed class TackleRequestReconciliationTests
    {
        private static DefensiveSnapshot DuplicateMarkSnapshot(int team)
        {
            var agents = new[]
            {
                SnapshotBuilder.MakeAgent(17, team, new Vector2(38, 34), new Vector2(1, 0), line: LineId.Midfield, baselineSlot: new Vector2(38, 34)),
                SnapshotBuilder.MakeAgent(18, team, new Vector2(39, 34), new Vector2(1, 0), line: LineId.Midfield, baselineSlot: new Vector2(39, 34)),
                SnapshotBuilder.MakeAgent(19, team, new Vector2(10, 34), line: LineId.Midfield, baselineSlot: new Vector2(10, 34)),
                SnapshotBuilder.MakeAgent(20, team, new Vector2(15, 34), line: LineId.Midfield, baselineSlot: new Vector2(15, 34)),
                SnapshotBuilder.MakeAgent(100, 1 - team, new Vector2(40, 34), line: LineId.Midfield),
            };
            DefensiveSnapshot snap = SnapshotBuilder.Build(agents, ballPosition: new Vector2(40, 34));
            snap.DefensiveTeamId = team;
            snap.MarkMovementActive = true;
            return snap;
        }

        /// <summary>Every published request matches its agent's final assignment target, on every tick, both teams.</summary>
        [TestCase(0)]
        [TestCase(1)]
        public void EveryPublishedRequest_MatchesTheAgentsFinalAssignment(int team)
        {
            DefensiveSnapshot snap = DuplicateMarkSnapshot(team);
            var ai = new DefensiveAITick(100);
            int published = 0, demotedDuplicateTicks = 0;
            for (int tick = 0; tick < 12; tick++)
            {
                snap.TickIndex = tick;
                ai.Tick(snap);
                foreach (TackleIntentRequest req in ai.GetTackleIntentRequests())
                {
                    MarkAssignment final = ai.GetAssignment(req.AgentEntityId);
                    Assert.AreEqual(req.TargetEntityId, final.TargetEntityId,
                        $"tick {tick}: agent {req.AgentEntityId} published {req.Mode} against {req.TargetEntityId} but finally marks {final.TargetEntityId} ({final.Mode})");
                    published++;
                }
                if (ai.GetAssignment(17).Mode == MarkMode.Zonal && ai.GetAssignment(18).TargetEntityId == 100)
                    demotedDuplicateTicks++;
            }
            // Outcome preconditions: the scenario really demotes the duplicate, and the retained marker still challenges.
            Assert.Greater(demotedDuplicateTicks, 0, "precondition: Invariant 4 demoted agent 17 at least once");
            Assert.Greater(published, 0, "the retained marker (18) still publishes requests");
        }

        /// <summary>The demoted duplicate (17) never publishes a request against 100 on a tick he ends ZONAL; the retained marker (18) does.</summary>
        [TestCase(0)]
        [TestCase(1)]
        public void DemotedDuplicate_PublishesNoRequest_RetainedMarkerDoes(int team)
        {
            DefensiveSnapshot snap = DuplicateMarkSnapshot(team);
            var ai = new DefensiveAITick(100);
            int retainedRequests = 0;
            for (int tick = 0; tick < 12; tick++)
            {
                snap.TickIndex = tick;
                ai.Tick(snap);
                bool seventeenZonal = ai.GetAssignment(17).Mode == MarkMode.Zonal;
                foreach (TackleIntentRequest req in ai.GetTackleIntentRequests())
                {
                    if (req.AgentEntityId == 17)
                        Assert.IsFalse(seventeenZonal, $"tick {tick}: demoted agent 17 published {req.Mode}");
                    if (req.AgentEntityId == 18 && req.TargetEntityId == 100)
                        retainedRequests++;
                }
            }
            Assert.Greater(retainedRequests, 0);
        }

        /// <summary>
        /// Two runs over the same inputs publish the same request sequence (count, agents, modes, targets) — Step 8a uses no
        /// RNG and no hidden state.
        /// </summary>
        [Test]
        public void Reconciliation_IsDeterministic()
        {
            string Run()
            {
                DefensiveSnapshot snap = DuplicateMarkSnapshot(0);
                var ai = new DefensiveAITick(100);
                var sb = new System.Text.StringBuilder();
                for (int tick = 0; tick < 12; tick++)
                {
                    snap.TickIndex = tick;
                    ai.Tick(snap);
                    foreach (TackleIntentRequest req in ai.GetTackleIntentRequests())
                        sb.Append(tick).Append(':').Append(req.AgentEntityId).Append(':').Append((int)req.Mode).Append(':').Append(req.TargetEntityId).Append(';');
                }
                return sb.ToString();
            }
            Assert.AreEqual(Run(), Run());
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-11-21 | —      | W13-F9: Step 8a reconciliation regression (both team ids) + determinism. |
#endregion
