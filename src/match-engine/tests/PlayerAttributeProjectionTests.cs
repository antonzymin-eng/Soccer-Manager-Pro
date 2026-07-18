// File:     src/match-engine/tests/PlayerAttributeProjectionTests.cs
// Created:  2026-07-17
// Modified: 2026-07-17
// Author:   —
// Spec:     Player-attribute projection design supplement §3/§4/§7/§9; Code Standards #20
// Purpose:  Pure locks on PlayerAttributeProjection — per-field scale/copy behaviour with distinct
//           inputs, the KD-P1 KickPower derivations, and the KD-P7 neutral-equivalence guarantee
//           (projection of the canonical-neutral record == every pre-T1 STAGE0 seed value).

using NUnit.Framework;

using UnityEngine;

// CS0104 guard (projection design §8 / KD-P6): this file deliberately brings BOTH PlayerAttributes
// types into scope through their namespaces' fully-qualified names below — compiling under the gate
// is the collision lock.
using TacticalDirector.DecisionTree;
using TacticalDirector.PassMechanics;
using TacticalDirector.PerceptionSystem;
using TacticalDirector.ShotMechanics;

namespace TacticalDirector.MatchEngine
{
    /// <summary>
    /// Pure-function locks for <see cref="PlayerAttributeProjection"/> (#27 T1/T2).
    /// </summary>
    [TestFixture]
    public sealed class PlayerAttributeProjectionTests
    {
        /// <summary>
        /// A canonical record with a DISTINCT value in every consumed field (no two target fields
        /// share a source value they could be accidentally swapped with, within each projection's
        /// field set).
        /// </summary>
        private static TacticalDirector.PlayerDatabase.PlayerAttributes Distinct()
        {
            var c = TacticalDirector.PlayerDatabase.PlayerAttributes.CreateDefault();
            c.Pace = 18; c.Acceleration = 17; c.Agility = 16; c.Balance = 15;
            c.Strength = 14; c.Stamina = 13;
            c.Passing = 12; c.Technique = 11; c.Finishing = 9; c.LongShots = 8;
            c.Dribbling = 7; c.Crossing = 6; c.Heading = 5;
            c.Decisions = 4; c.Vision = 3; c.Composure = 2; c.Anticipation = 19;
            c.WorkRate = 20; c.Aggression = 1; c.Positioning = 10;
            c.FirstTouchAbility = 15;
            c.WeakFootRating = 4;
            return c;
        }

        // ── §3.1 Agent Movement (#2): raw int copy ─────────────────────────────────────

        [Test]
        public void ToAgentMovement_CopiesRawValues()
        {
            var c = Distinct();
            TacticalDirector.AgentMovement.PlayerAttributes m =
                PlayerAttributeProjection.ToAgentMovement(in c);

            Assert.AreEqual(18, m.Pace);
            Assert.AreEqual(17, m.Acceleration);
            Assert.AreEqual(16, m.Agility);
            Assert.AreEqual(15, m.Balance);
            Assert.AreEqual(14, m.Strength);
            Assert.AreEqual(13, m.Stamina);
        }

        // ── §3.2 Decision Tree (#8): raw int copy + runtime TeamId ─────────────────────

        [Test]
        public void ToDecisionTree_CopiesRawValues_AndRuntimeTeamId()
        {
            var c = Distinct();
            DtAgentAttributes d = PlayerAttributeProjection.ToDecisionTree(in c, teamId: 1);

            Assert.AreEqual(4,  d.Decisions);
            Assert.AreEqual(3,  d.Vision);
            Assert.AreEqual(12, d.Passing);
            Assert.AreEqual(9,  d.Finishing);
            Assert.AreEqual(7,  d.Dribbling);
            Assert.AreEqual(8,  d.LongShots);
            Assert.AreEqual(6,  d.Crossing);
            Assert.AreEqual(2,  d.Composure);
            Assert.AreEqual(19, d.Anticipation);
            Assert.AreEqual(18, d.Pace);
            Assert.AreEqual(16, d.Agility);
            Assert.AreEqual(20, d.WorkRate);
            Assert.AreEqual(13, d.Stamina);
            Assert.AreEqual(1,  d.Aggression);
            Assert.AreEqual(10, d.Positioning);
            Assert.AreEqual(1,  d.TeamId, "TeamId is caller-supplied runtime identity (KD-P4).");
        }

        // ── §3.3 Perception (#7): raw int copy + runtime TeamId/IsHalfTurned ───────────

        [Test]
        public void ToPerception_CopiesRawValues_AndRuntimeFields()
        {
            var c = Distinct();
            PerceptionAgentAttributes p =
                PlayerAttributeProjection.ToPerception(in c, teamId: 1, isHalfTurned: true);

            Assert.AreEqual(4,  p.Decisions);
            Assert.AreEqual(19, p.Anticipation);
            Assert.AreEqual(1,  p.TeamId);
            Assert.IsTrue(p.IsHalfTurned, "IsHalfTurned is caller-supplied runtime stance (KD-P4).");
        }

        // ── §3.4 Pass (#5): int→float widening + derived KickPower + [1,5] WeakFoot ────

        [Test]
        public void ToPass_WidensRawValues_DerivesKickPower_KeepsWeakFootScale()
        {
            var c = Distinct();
            PassAgentAttributes p = PlayerAttributeProjection.ToPass(in c, fatigue: 0.25f);

            Assert.AreEqual(12f, p.Passing);
            Assert.AreEqual(11f, p.Technique);
            Assert.AreEqual((12 + 11) * 0.5f, p.KickPower, 0f,
                "KickPower = (Passing + Technique) × 0.5 (KD-P1).");
            Assert.AreEqual(6f, p.Crossing);
            Assert.AreEqual(4, p.WeakFootRating, "WeakFootRating stays raw [1,5] (KD-2) — never [1,20]-scaled.");
            Assert.AreEqual(0.25f, p.Fatigue, "Fatigue is caller-supplied runtime state (KD-P4).");
        }

        // ── §3.5 Shot (#6): raw int copy + derived rounded KickPower ───────────────────

        [Test]
        public void ToShot_CopiesRawValues_DerivesRoundedKickPower()
        {
            var c = Distinct();
            ShotAgentAttributes s = PlayerAttributeProjection.ToShot(in c, fatigue: 0.75f);

            Assert.AreEqual(9,  s.Finishing);
            Assert.AreEqual(8,  s.LongShots);
            Assert.AreEqual(2,  s.Composure);
            Assert.AreEqual(11, s.Technique);
            // (9 + 8) × 0.5 = 8.5 → Mathf.RoundToInt (round-half-to-even) → 8.
            Assert.AreEqual(8, s.KickPower,
                "KickPower = RoundToInt((Finishing + LongShots) × 0.5), Mathf.RoundToInt pinned (§4 L-1).");
            Assert.AreEqual(4, s.WeakFootRating);
            Assert.AreEqual(0.75f, s.Fatigue);
        }

        [Test]
        public void ToShot_KickPower_IntegerAverage_IsExact()
        {
            var c = TacticalDirector.PlayerDatabase.PlayerAttributes.CreateDefault();
            c.Finishing = 14;
            c.LongShots = 18;
            Assert.AreEqual(16, PlayerAttributeProjection.ToShot(in c, 0f).KickPower);
        }

        [Test]
        public void ToShot_KickPower_HalfAverage_MatchesMathfRounding()
        {
            var c = TacticalDirector.PlayerDatabase.PlayerAttributes.CreateDefault();
            c.Finishing = 13;
            c.LongShots = 14;
            // 13.5 rounds per Mathf.RoundToInt (round-half-to-even ⇒ 14) — the pinned oracle is
            // the exact call the projection uses, so a Unity/shim rounding-mode drift fails here.
            Assert.AreEqual(Mathf.RoundToInt(13.5f), PlayerAttributeProjection.ToShot(in c, 0f).KickPower);
        }

        // ── §3.5a FirstTouchAbility + §3.8/KD-P3 normalization ─────────────────────────

        [Test]
        public void FirstTouchAbility_CopiesRawValue()
        {
            var c = Distinct();
            Assert.AreEqual(15, PlayerAttributeProjection.FirstTouchAbility(in c));
        }

        [Test]
        public void ToNormalized_DividesByAttributeMax()
        {
            Assert.AreEqual(0.05f, PlayerAttributeProjection.ToNormalized(1), 1e-6f);
            Assert.AreEqual(0.5f,  PlayerAttributeProjection.ToNormalized(10), 0f);
            Assert.AreEqual(1f,    PlayerAttributeProjection.ToNormalized(20), 0f);
        }

        // ── §7 / KD-P7: neutral-equivalence — projection(CreateDefault) == pre-T1 seeds ──

        [Test]
        public void NeutralRecord_ProjectsToEveryPreT1Seed()
        {
            var n = TacticalDirector.PlayerDatabase.PlayerAttributes.CreateDefault();

            TacticalDirector.AgentMovement.PlayerAttributes am =
                PlayerAttributeProjection.ToAgentMovement(in n);
            TacticalDirector.AgentMovement.PlayerAttributes amDefault =
                TacticalDirector.AgentMovement.PlayerAttributes.CreateDefault();
            Assert.AreEqual(amDefault.Pace, am.Pace);
            Assert.AreEqual(amDefault.Acceleration, am.Acceleration);
            Assert.AreEqual(amDefault.Agility, am.Agility);
            Assert.AreEqual(amDefault.Balance, am.Balance);
            Assert.AreEqual(amDefault.Strength, am.Strength);
            Assert.AreEqual(amDefault.Stamina, am.Stamina);

            DtAgentAttributes dt = PlayerAttributeProjection.ToDecisionTree(in n, teamId: 1);
            Assert.AreEqual(DtAgentAttributes.CreateDefault(1), dt,
                "Neutral DT projection must equal the pre-T1 CreateDefault(teamId) seed.");

            PerceptionAgentAttributes pn = PlayerAttributeProjection.ToPerception(in n, 0, false);
            Assert.AreEqual(PerceptionAgentAttributes.CreateDefault(), pn,
                "Neutral Perception projection must equal the pre-T1 CreateDefault seed.");

            PassAgentAttributes pass = PlayerAttributeProjection.ToPass(in n, fatigue: 0f);
            Assert.AreEqual(MatchEngineConstants.STAGE0_NEUTRAL_ATTRIBUTE, pass.Passing);
            Assert.AreEqual(MatchEngineConstants.STAGE0_NEUTRAL_ATTRIBUTE, pass.Technique);
            Assert.AreEqual(MatchEngineConstants.STAGE0_NEUTRAL_ATTRIBUTE, pass.KickPower);
            Assert.AreEqual(MatchEngineConstants.STAGE0_NEUTRAL_ATTRIBUTE, pass.Crossing);
            Assert.AreEqual(MatchEngineConstants.STAGE0_NEUTRAL_WEAK_FOOT, pass.WeakFootRating);

            ShotAgentAttributes shot = PlayerAttributeProjection.ToShot(in n, fatigue: 0f);
            int neutralInt = Mathf.RoundToInt(MatchEngineConstants.STAGE0_NEUTRAL_ATTRIBUTE);
            Assert.AreEqual(neutralInt, shot.Finishing);
            Assert.AreEqual(neutralInt, shot.LongShots);
            Assert.AreEqual(neutralInt, shot.Composure);
            Assert.AreEqual(neutralInt, shot.KickPower);
            Assert.AreEqual(neutralInt, shot.Technique);
            Assert.AreEqual(MatchEngineConstants.STAGE0_NEUTRAL_WEAK_FOOT, shot.WeakFootRating);

            Assert.AreEqual(Mathf.RoundToInt(MatchEngineConstants.STAGE0_NEUTRAL_ATTRIBUTE),
                PlayerAttributeProjection.FirstTouchAbility(in n),
                "The three FirstTouchAbility sites' pre-T1 seed was the rounded neutral (= 10).");

            Assert.AreEqual(MatchEngineConstants.STAGE0_NEUTRAL_NORMALIZED,
                PlayerAttributeProjection.ToNormalized(n.Pace), 0f,
                "Neutral ÷ ATTRIBUTE_MAX must equal the pre-T1 normalized seed exactly (KD-P3).");
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-17 | —      | Initial implementation (#27 T1/T2 projection locks).           |
#endregion
