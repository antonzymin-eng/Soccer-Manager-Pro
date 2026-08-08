// File:     src/decision-tree/Tests/ActionSelectorTests.cs
// Created:  2026-05-29
// Modified: 2026-06-12
// Author:   —
// Spec:     Decision Tree #8 §3.3, §3.3.3–3.3.6, Code Standards #20
// Purpose:  Unit tests for ActionSelector. Verifies empty-buffer fallback, single-option
//           selection, high-utility dominance, and composure noise application.

using NUnit.Framework;
using UnityEngine;
using TacticalDirector.PerceptionSystem;

namespace TacticalDirector.DecisionTree.Tests
{
    [TestFixture]
    internal class ActionSelectorTests
    {
        private static readonly ActionOption[] Buffer = new ActionOption[DecisionTreeConstants.MaxOptions];

        // ── UT-10: Empty buffer returns HOLD fallback ─────────────────────────

        [Test]
        public void EmptyBuffer_ReturnsFallbackHold()
        {
            DecisionContext ctx = BuildContext(agentId: 3, composure: 0.5f);
            AgentAction action = ActionSelector.SelectAction(
                Buffer, 0, in ctx, out bool tiebreak, out bool fallback);

            Assert.IsTrue(fallback, "fallbackToHold must be true when buffer is empty");
            Assert.IsFalse(tiebreak, "tiebreakerApplied must be false on empty buffer");
            Assert.AreEqual(ActionType.HOLD, action.Type);
            Assert.AreEqual(3, action.AgentId, "Fallback action must carry the correct agent ID");
        }

        // ── UT-11: Single option is always selected ───────────────────────────

        [Test]
        public void SingleOption_IsAlwaysSelected()
        {
            DecisionContext ctx = BuildContext(agentId: 0, composure: 0.5f);
            Buffer[0] = new ActionOption { Type = ActionType.DRIBBLE, BaseUtility = 0.6f };

            AgentAction action = ActionSelector.SelectAction(
                Buffer, 1, in ctx, out _, out bool fallback);

            Assert.IsFalse(fallback);
            Assert.AreEqual(ActionType.DRIBBLE, action.Type);
        }

        // ── UT-12: Option with clearly higher utility dominates ───────────────

        [Test]
        public void HigherBaseUtility_Dominates()
        {
            // Gap of 0.70 >> NOISE_MAX (0.15); winner is robust even at max noise.
            DecisionContext ctx = BuildContext(agentId: 0, composure: 1.0f);
            Buffer[0] = new ActionOption { Type = ActionType.HOLD,    BaseUtility = 0.15f };
            Buffer[1] = new ActionOption { Type = ActionType.DRIBBLE, BaseUtility = 0.85f };

            AgentAction action = ActionSelector.SelectAction(
                Buffer, 2, in ctx, out _, out _);

            Assert.AreEqual(ActionType.DRIBBLE, action.Type,
                "Option with clearly higher BaseUtility must win regardless of noise");
        }

        // ── UT-13: Fallback action carries correct AgentId ────────────────────

        [Test]
        public void FallbackHold_CarriesCorrectAgentId()
        {
            DecisionContext ctx = BuildContext(agentId: 7, composure: 0.5f);
            AgentAction action = ActionSelector.SelectAction(
                Buffer, 0, in ctx, out _, out _);

            Assert.AreEqual(ActionType.HOLD, action.Type);
            Assert.AreEqual(7, action.AgentId);
        }

        // ── UT-14: Noise is applied (EffectiveUtility ≠ BaseUtility) ─────────

        [Test]
        public void NoiseApplied_EffectiveUtilityDiffersFromBase()
        {
            // At zero composure the noise bound is NOISE_MAX (0.15); hash is non-zero
            // for the chosen seed/agent/frame combination, so EffectiveUtility != BaseUtility.
            DecisionContext ctx = BuildContext(agentId: 0, composure: 0.0f);
            const float baseU = 0.5f;
            Buffer[0] = new ActionOption { Type = ActionType.HOLD, BaseUtility = baseU };

            AgentAction action = ActionSelector.SelectAction(
                Buffer, 1, in ctx, out _, out _);

            // UtilityScore is the EffectiveUtility stored in the action.
            Assert.That(action.UtilityScore, Is.Not.EqualTo(baseU).Within(0.0001f),
                "Selector must add composure noise; EffectiveUtility should differ from BaseUtility");
        }

        // ── UT-15: Three options — highest still wins ─────────────────────────

        [Test]
        public void ThreeOptions_HighestWins()
        {
            DecisionContext ctx = BuildContext(agentId: 0, composure: 1.0f);
            Buffer[0] = new ActionOption { Type = ActionType.HOLD,    BaseUtility = 0.20f };
            Buffer[1] = new ActionOption { Type = ActionType.DRIBBLE, BaseUtility = 0.50f };
            Buffer[2] = new ActionOption { Type = ActionType.SHOOT,   BaseUtility = 0.90f };

            AgentAction action = ActionSelector.SelectAction(
                Buffer, 3, in ctx, out _, out _);

            Assert.AreEqual(ActionType.SHOOT, action.Type);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static DecisionContext BuildContext(int agentId, float composure)
        {
            var mc = new MatchContext
            {
                Phase              = MatchPhase.OPEN_PLAY,
                BallZone           = FieldZone.MIDFIELD,
                Possession         = PossessionState.HOME_TEAM,
                PossessingAgentId  = 0
            };
            var tc   = TacticalContext.Stage0Default(new Vector2(50f, 34f));
            var snap = new FilteredView
            {
                ObserverId               = agentId,
                FrameNumber              = 1,
                VisibleTeammates         = new PerceivedAgent[0],
                VisibleOpponents         = new PerceivedAgent[0],
                BlindSidePerceivedAgents = new PerceivedAgent[0]
            };

            return new DecisionContext
            {
                AgentId              = agentId,
                AgentTeamId          = 0,
                CurrentFrame         = 1,
                AgentHasBall         = true,
                PossessedByTeam      = PossessionState.HOME_TEAM,
                AgentPosition        = new Vector2(52f, 34f),
                AgentFacingDirection  = Vector2.right,
                AgentState           = default,
                A_Vision             = 0.5f,
                A_Passing            = 0.5f,
                A_Finishing          = 0.5f,
                A_Dribbling          = 0.5f,
                A_LongShots          = 0.5f,
                A_Composure          = composure,
                A_Decisions          = 0.5f,
                A_Anticipation       = 0.5f,
                A_Pace               = 0.5f,
                A_Agility            = 0.5f,
                A_WorkRate           = 0.5f,
                A_Stamina            = 0.5f,
                A_Aggression         = 0.5f,
                A_Positioning        = 0.5f,
                A_Crossing           = 0.5f,
                MatchContext         = mc,
                TacticalContext      = tc,
                PressureScalar       = 0.0f,
                MatchSeed            = 0xDEADBEEFUL,
                Snapshot             = snap,
                OpponentGoalCentre   = new Vector2(105f, 34f),
                OpponentGoalPostL    = new Vector2(105f, 30.34f),
                OpponentGoalPostR    = new Vector2(105f, 37.66f)
            };
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-29 | —      | Initial implementation. |
// | 1.1     | 2026-06-12 | —      | Build fix (dotnet CI    |
// |         |            |        | gate):                  |
// |         |            |        | Assert.AreNotEqual has  |
// |         |            |        | no tolerance overload   |
// |         |            |        | in NUnit 3 (CS1503;     |
// |         |            |        | UT-14 never compiled).  |
// |         |            |        | Rewritten as            |
// |         |            |        | Is.Not.EqualTo(baseU).Within(0.0001f)|
// |         |            |        | - same                  |
// |         |            |        | must-differ-beyond-tolerance|
// |         |            |        | substance.              |
#endregion
