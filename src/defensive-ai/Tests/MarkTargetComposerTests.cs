// File:     src/defensive-ai/Tests/MarkTargetComposerTests.cs
// Created:  2026-11-10
// Modified: 2026-11-10
// Author:   —
// Spec:     Defensive AI #14 §3.13.1, §3.10.2 (Invariant 4), FR-DA-038 / FR-DA-039, ERR-014-010, Code Standards #20
// Purpose:  Locks the W13.1 defensive-ai half: the goal-side mark-point composition (worked examples,
//           no-target cases, COVER_GK_ZONE passthrough), the stand-off constant invariant, anti-chaos
//           Invariant 4 (one marker per opponent) and its default-off identity, and zero allocation.

using System;

using NUnit.Framework;

using UnityEngine;

using TacticalDirector.PositioningAI;
using TacticalDirector.PressingAI;

namespace TacticalDirector.DefensiveAI.Tests
{
    [TestFixture]
    internal sealed class MarkTargetComposerTests
    {
        private const int Tick = 7;
        private const float Tol = 1e-3f;

        /// <summary>Snapshot for the given defending team holding one marked opponent (id 100) at a canonical position.</summary>
        private static DefensiveSnapshot OneOpponent(int defendingTeam, Vector2 oppCanonical, bool oppActive = true)
        {
            DefensiveAgentSnapshot[] agents = new DefensiveAgentSnapshot[]
            {
                SnapshotBuilder.MakeAgent(entityId: 10, teamId: defendingTeam,
                    position: new Vector2(21.0f, 25.84f), baselineSlot: new Vector2(21.0f, 25.84f), line: LineId.Defense),
                SnapshotBuilder.MakeAgent(entityId: 100, teamId: 1 - defendingTeam,
                    position: oppCanonical, isActive: oppActive, firstTouch: 12f),
            };
            DefensiveSnapshot snap = SnapshotBuilder.Build(agents, tick: Tick);
            snap.DefensiveTeamId = defendingTeam;
            return snap;
        }

        private static MarkAssignment Mark(MarkMode mode, int target, Vector2 pos, int tick = Tick) => new MarkAssignment
        {
            AgentEntityId = 10,
            Mode = mode,
            TargetEntityId = target,
            TargetPosition = pos,
            ValidThroughTick = tick,
        };

        // ── §3.13.1 worked examples ─────────────────────────────────────────────

        /// <summary>
        /// Example A. Away CB 1 marks the home F4231 striker. Striker world (86.1, 34) is, in the away
        /// team's canonical frame (180° rotation), (18.9, 34); the mark point is 2.0 m toward the own goal
        /// (16.9, 34), i.e. world (88.1, 34) after the engine's inverse rotation. A home defender whose
        /// snapshot holds the same canonical point gets the identical result (canonical-only, ERR-014-008).
        /// </summary>
        [TestCase(0)]
        [TestCase(1)]
        public void ExampleA_GoalSidePointAtStandoff_BothTeams(int defendingTeam)
        {
            // The away team's snapshot holds the home ST at the 180-degree mirror of its world position; the
            // home team's snapshot (the mirrored case) holds the same canonical point. One input, both teams.
            Vector2 stWorld = new Vector2(86.1f, 34.0f);
            Vector2 o = new Vector2(105.0f - stWorld.x, 68.0f - stWorld.y);
            Assert.AreEqual(18.9f, o.x, Tol, "precondition: canonical x");

            DefensiveSnapshot snap = OneOpponent(defendingTeam, o);
            Assert.IsTrue(MarkTargetComposer.TryCompose(Mark(MarkMode.ManMark, 100, o), snap, Tick, 2.0f, out Vector2 m));

            Assert.AreEqual(16.9f, m.x, Tol);
            Assert.AreEqual(34.0f, m.y, Tol);
            Assert.AreEqual(2.0f, Vector2.Distance(m, o), Tol, "|M - O| = s");
        }

        /// <summary>Example B (off-centre): O = (20, 30), L = 20.396 — s = 2 and s = 1 (tight).</summary>
        [TestCase(0)]
        [TestCase(1)]
        public void ExampleB_OffCentre_NormalAndTight(int defendingTeam)
        {
            Vector2 o = new Vector2(20.0f, 30.0f);
            DefensiveSnapshot snap = OneOpponent(defendingTeam, o);
            MarkAssignment a = Mark(MarkMode.ManMark, 100, o);

            Assert.IsTrue(MarkTargetComposer.TryCompose(a, snap, Tick, 2.0f, out Vector2 normal));
            Assert.AreEqual(18.039f, normal.x, Tol);
            Assert.AreEqual(30.392f, normal.y, Tol);

            Assert.IsTrue(MarkTargetComposer.TryCompose(a, snap, Tick, 1.0f, out Vector2 tight));
            Assert.AreEqual(19.019f, tight.x, Tol);
            Assert.AreEqual(30.196f, tight.y, Tol);
        }

        /// <summary>Degenerate: opponent within the stand-off of the goal centre — the point is the goal centre.</summary>
        [TestCase(0)]
        [TestCase(1)]
        public void Degenerate_OpponentWithinStandoffOfGoal_ReturnsGoalCentre(int defendingTeam)
        {
            Vector2 o = new Vector2(1.0f, 34.5f); // L = 1.118 <= 2
            DefensiveSnapshot snap = OneOpponent(defendingTeam, o);

            Assert.IsTrue(MarkTargetComposer.TryCompose(Mark(MarkMode.ManMark, 100, o), snap, Tick, 2.0f, out Vector2 m));
            Assert.AreEqual(0.0f, m.x, Tol);
            Assert.AreEqual(34.0f, m.y, Tol);
        }

        /// <summary>Boundary: L == s lands exactly on the goal centre; L just above s is s short of the opponent.</summary>
        [Test]
        public void Boundary_LengthEqualsStandoff_GoalCentre_JustAbove_StandoffPoint()
        {
            DefensiveSnapshot atS = OneOpponent(0, new Vector2(2.0f, 34.0f));
            Assert.IsTrue(MarkTargetComposer.TryCompose(Mark(MarkMode.ManMark, 100, Vector2.zero), atS, Tick, 2.0f, out Vector2 g));
            Assert.AreEqual(0.0f, g.x, Tol);

            DefensiveSnapshot above = OneOpponent(0, new Vector2(2.01f, 34.0f));
            Assert.IsTrue(MarkTargetComposer.TryCompose(Mark(MarkMode.ManMark, 100, Vector2.zero), above, Tick, 2.0f, out Vector2 m));
            Assert.AreEqual(0.01f, m.x, Tol);
        }

        /// <summary>The composed point uses the opponent's LIVE snapshot position, not the assignment's TargetPosition.</summary>
        [Test]
        public void UsesLiveSnapshotPosition_NotTheAssignmentTargetPosition()
        {
            DefensiveSnapshot snap = OneOpponent(0, new Vector2(30.0f, 34.0f));
            MarkAssignment stale = Mark(MarkMode.ManMark, 100, new Vector2(60.0f, 10.0f));

            Assert.IsTrue(MarkTargetComposer.TryCompose(stale, snap, Tick, 2.0f, out Vector2 m));
            Assert.AreEqual(28.0f, m.x, Tol);
            Assert.AreEqual(34.0f, m.y, Tol);
        }

        /// <summary>INTERCEPT_RUNNER composes exactly as MAN_MARK does.</summary>
        [Test]
        public void InterceptRunner_ComposesLikeManMark()
        {
            DefensiveSnapshot snap = OneOpponent(0, new Vector2(20.0f, 30.0f));
            Assert.IsTrue(MarkTargetComposer.TryCompose(Mark(MarkMode.InterceptRunner, 100, Vector2.zero), snap, Tick, 2.0f, out Vector2 m));
            Assert.AreEqual(18.039f, m.x, Tol);
            Assert.AreEqual(30.392f, m.y, Tol);
        }

        /// <summary>Goal-side and stand-off property over a grid: |M - O| = s (or M = G) and M is closer to G than O.</summary>
        [Test]
        public void Property_PointIsGoalSideAtStandoff_OverGrid()
        {
            Vector2 g = DefensiveAIConstants.OwnGoalCenter;
            for (float x = 0.0f; x <= 105.0f; x += 7.5f)
            {
                for (float y = 0.0f; y <= 68.0f; y += 8.5f)
                {
                    Vector2 o = new Vector2(x, y);
                    DefensiveSnapshot snap = OneOpponent(0, o);
                    foreach (float s in new[] { 1.0f, 2.0f, 2.9f })
                    {
                        Assert.IsTrue(MarkTargetComposer.TryCompose(Mark(MarkMode.ManMark, 100, o), snap, Tick, s, out Vector2 m));
                        float distOG = Vector2.Distance(o, g);
                        if (distOG > s)
                        {
                            Assert.AreEqual(s, Vector2.Distance(m, o), 1e-3f, $"O=({x},{y}) s={s}");
                            Assert.Less(Vector2.Distance(m, g), distOG, $"O=({x},{y}) s={s}: goal-side");
                        }
                        else
                        {
                            Assert.AreEqual(0.0f, Vector2.Distance(m, g), 1e-5f);
                        }
                        Assert.LessOrEqual(Vector2.Distance(m, o), s + 1e-4f);
                    }
                }
            }
        }

        // ── no-target cases ─────────────────────────────────────────────────────

        [Test]
        public void NoTarget_ZonalMode()
        {
            DefensiveSnapshot snap = OneOpponent(0, new Vector2(30.0f, 34.0f));
            Assert.IsFalse(MarkTargetComposer.TryCompose(
                MarkAssignment.MakeZonal(10, new Vector2(21.0f, 25.84f), Tick), snap, Tick, 2.0f, out Vector2 m));
            Assert.AreEqual(Vector2.zero, m);
        }

        [Test]
        public void NoTarget_StaleTick_AllModes()
        {
            DefensiveSnapshot snap = OneOpponent(0, new Vector2(30.0f, 34.0f));
            Assert.IsFalse(MarkTargetComposer.TryCompose(Mark(MarkMode.ManMark, 100, Vector2.zero, Tick - 1), snap, Tick, 2.0f, out _));
            Assert.IsFalse(MarkTargetComposer.TryCompose(Mark(MarkMode.InterceptRunner, 100, Vector2.zero, Tick + 1), snap, Tick, 2.0f, out _));
            Assert.IsFalse(MarkTargetComposer.TryCompose(Mark(MarkMode.CoverGkZone, -1, new Vector2(7.5f, 34.0f), Tick - 1), snap, Tick, 2.0f, out _));
        }

        [Test]
        public void NoTarget_TargetMissingFromSnapshot()
        {
            DefensiveSnapshot snap = OneOpponent(0, new Vector2(30.0f, 34.0f));
            Assert.IsFalse(MarkTargetComposer.TryCompose(Mark(MarkMode.ManMark, 999, Vector2.zero), snap, Tick, 2.0f, out _));
        }

        [Test]
        public void NoTarget_TargetInactive()
        {
            DefensiveSnapshot snap = OneOpponent(0, new Vector2(30.0f, 34.0f), oppActive: false);
            Assert.IsFalse(MarkTargetComposer.TryCompose(Mark(MarkMode.ManMark, 100, Vector2.zero), snap, Tick, 2.0f, out _));
            Assert.IsFalse(MarkTargetComposer.TryCompose(Mark(MarkMode.InterceptRunner, 100, Vector2.zero), snap, Tick, 2.0f, out _));
        }

        [Test]
        public void NoTarget_NonPositiveOrNaNStandoff()
        {
            DefensiveSnapshot snap = OneOpponent(0, new Vector2(30.0f, 34.0f));
            MarkAssignment a = Mark(MarkMode.ManMark, 100, Vector2.zero);
            Assert.IsFalse(MarkTargetComposer.TryCompose(a, snap, Tick, 0.0f, out _));
            Assert.IsFalse(MarkTargetComposer.TryCompose(a, snap, Tick, -1.0f, out _));
            Assert.IsFalse(MarkTargetComposer.TryCompose(a, snap, Tick, float.NaN, out _));
        }

        // ── COVER_GK_ZONE passthrough ───────────────────────────────────────────

        [TestCase(0)]
        [TestCase(1)]
        public void CoverGkZone_PassesZoneCentreThrough(int defendingTeam)
        {
            DefensiveSnapshot snap = OneOpponent(defendingTeam, new Vector2(30.0f, 34.0f), oppActive: false);
            Vector2 zone = new Vector2(7.5f, 34.0f);

            Assert.IsTrue(MarkTargetComposer.TryCompose(Mark(MarkMode.CoverGkZone, -1, zone), snap, Tick, 2.0f, out Vector2 m));
            Assert.AreEqual(zone, m, "the zone centre is the target, untouched by the stand-off");
        }

        // ── constants ───────────────────────────────────────────────────────────

        /// <summary>
        /// The stand-off range invariant (#14 §3.13.1): 0 &lt; tight &lt; normal &lt; min(TackleEligibleRadiusM,
        /// #23 MARKING_RADIUS_M), and the TacticTranslation lookup returns the matching constant.
        /// </summary>
        [Test]
        public void StandoffConstants_RangeInvariantAndTranslation()
        {
            float bound = Mathf.Min(DefensiveAIConstants.TackleEligibleRadiusM, PositioningAIConstants.MARKING_RADIUS_M);

            Assert.Greater(DefensiveAIConstants.MarkStandoffTightM, 0.0f);
            Assert.Less(DefensiveAIConstants.MarkStandoffTightM, DefensiveAIConstants.MarkStandoffM);
            Assert.Less(DefensiveAIConstants.MarkStandoffM, bound);

            Assert.AreEqual(2.0f, DefensiveAIConstants.MarkStandoffM, "UN-CALIBRATED default");
            Assert.AreEqual(1.0f, DefensiveAIConstants.MarkStandoffTightM, "UN-CALIBRATED default");
            Assert.AreEqual(DefensiveAIConstants.MarkStandoffM, TacticTranslation.MarkStandoffM(false));
            Assert.AreEqual(DefensiveAIConstants.MarkStandoffTightM, TacticTranslation.MarkStandoffM(true));
        }

        [Test]
        public void OwnGoalCentre_IsDerivedFromPitchWidth()
        {
            Assert.AreEqual(0.0f, DefensiveAIConstants.OwnGoalCenter.x);
            Assert.AreEqual(DefensiveAIConstants.PITCH_WIDTH_M / 2.0f, DefensiveAIConstants.OwnGoalCenter.y);
        }

        // ── zero allocation ─────────────────────────────────────────────────────

#if !UNITY_5_3_OR_NEWER
        // Mono's managed byte counter is not reliable inside Unity (see MatchEngineSnapshotDigestReferenceTests).
        [Test]
        public void TryCompose_And_Invariant4_AllocateNothing()
        {
            DefensiveSnapshot snap = Invariant4Tests.DuplicateMarkerSnapshot(0, out int[] pool, out MarkAssignment[] template);
            snap.MarkMovementActive = true;
            MarkAssignment[] work = new MarkAssignment[template.Length];
            MarkAssignment a = Mark(MarkMode.ManMark, 100, Vector2.zero);

            for (int i = 0; i < 50; i++) // warm up (JIT)
            {
                Array.Copy(template, work, template.Length);
                InvariantEnforcer.Enforce(snap, pool, pool.Length, Tick, work);
                MarkTargetComposer.TryCompose(a, snap, Tick, 2.0f, out _);
            }

            long control0 = GC.GetAllocatedBytesForCurrentThread();
            byte[] control = new byte[1024];
            long controlBytes = GC.GetAllocatedBytesForCurrentThread() - control0;
            GC.KeepAlive(control);
            Assert.Greater(controlBytes, 0L, "Allocation counter must detect a known live allocation on this runtime.");

            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 1000; i++)
            {
                Array.Copy(template, work, template.Length);
                InvariantEnforcer.Enforce(snap, pool, pool.Length, Tick, work);
                MarkTargetComposer.TryCompose(a, snap, Tick, 2.0f, out _);
            }
            long bytes = GC.GetAllocatedBytesForCurrentThread() - before;

            Assert.AreEqual(0L, bytes, "TryCompose and Enforce (Invariant 4 active) must not allocate.");
        }
#endif
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Invariant 4 — one marker per opponent (§3.10.2, FR-DA-039)
    // ─────────────────────────────────────────────────────────────────────────────

    [TestFixture]
    internal sealed class Invariant4Tests
    {
        private const int Tick = 3;
        private const int TargetId = 100;

        /// <summary>
        /// The #14 §3.10.2 worked example: away CMs 17 at (45, 28) and 18 at (45, 40) both mark the home CAM
        /// at canonical (36.75, 34). Midfield line, so Invariants 1-3 are vacuous and only Invariant 4 acts.
        /// </summary>
        internal static DefensiveSnapshot DuplicateMarkerSnapshot(
            int defendingTeam, out int[] pool, out MarkAssignment[] assignments)
        {
            Vector2 cam = new Vector2(36.75f, 34.0f);
            DefensiveAgentSnapshot[] agents = new DefensiveAgentSnapshot[]
            {
                SnapshotBuilder.MakeAgent(entityId: 17, teamId: defendingTeam, position: new Vector2(45.0f, 28.0f),
                    baselineSlot: new Vector2(45.0f, 28.0f), line: LineId.Midfield),
                SnapshotBuilder.MakeAgent(entityId: 18, teamId: defendingTeam, position: new Vector2(45.0f, 40.0f),
                    baselineSlot: new Vector2(45.0f, 40.0f), line: LineId.Midfield),
                SnapshotBuilder.MakeAgent(entityId: TargetId, teamId: 1 - defendingTeam, position: cam, firstTouch: 14f),
            };
            DefensiveSnapshot snap = SnapshotBuilder.Build(agents, tick: Tick);
            snap.DefensiveTeamId = defendingTeam;

            pool = new int[] { 17, 18 };
            assignments = new MarkAssignment[]
            {
                new MarkAssignment { AgentEntityId = 17, Mode = MarkMode.ManMark, TargetEntityId = TargetId, TargetPosition = cam, ValidThroughTick = Tick },
                new MarkAssignment { AgentEntityId = 18, Mode = MarkMode.ManMark, TargetEntityId = TargetId, TargetPosition = cam, ValidThroughTick = Tick },
            };
            return snap;
        }

        /// <summary>Both markers cost 8.25² + 6² = 104.06 m²; the tie goes to the lower EntityId (17); 18 is demoted.</summary>
        [TestCase(0)]
        [TestCase(1)]
        public void WorkedExample_TieKeepsLowerEntityId_OtherDemoted(int team)
        {
            DefensiveSnapshot snap = DuplicateMarkerSnapshot(team, out int[] pool, out MarkAssignment[] a);
            snap.MarkMovementActive = true;

            Assert.IsTrue(InvariantEnforcer.Enforce(snap, pool, 2, Tick, a));

            Assert.AreEqual(MarkMode.ManMark, a[0].Mode, "17 keeps the CAM");
            Assert.AreEqual(TargetId, a[0].TargetEntityId);
            Assert.AreEqual(MarkMode.Zonal, a[1].Mode, "18 is demoted");
            Assert.AreEqual(-1, a[1].TargetEntityId);
            Assert.AreEqual(new Vector2(45.0f, 40.0f), a[1].TargetPosition, "demoted to its own #12 slot");
        }

        /// <summary>Flag off (the default): Invariant 4 does not run — both duplicates survive exactly as before W13.1.</summary>
        [TestCase(0)]
        [TestCase(1)]
        public void FlagOff_DuplicatesSurvive_IdentityToPreW13_1(int team)
        {
            DefensiveSnapshot snap = DuplicateMarkerSnapshot(team, out int[] pool, out MarkAssignment[] a);
            Assert.IsFalse(snap.MarkMovementActive, "the shipped default");

            Assert.IsTrue(InvariantEnforcer.Enforce(snap, pool, 2, Tick, a));

            Assert.AreEqual(MarkMode.ManMark, a[0].Mode);
            Assert.AreEqual(MarkMode.ManMark, a[1].Mode);
            Assert.AreEqual(TargetId, a[1].TargetEntityId);
        }

        [Test]
        public void DefaultSnapshot_MarkMovementActive_IsFalse()
        {
            Assert.IsFalse(new DefensiveSnapshot().MarkMovementActive);
        }

        /// <summary>The closer marker keeps the opponent regardless of EntityId.</summary>
        [Test]
        public void CloserMarkerKeeps_EvenWithHigherEntityId()
        {
            DefensiveSnapshot snap = DuplicateMarkerSnapshot(0, out int[] pool, out MarkAssignment[] a);
            snap.MarkMovementActive = true;
            snap.Agents[1].Position = new Vector2(40.0f, 34.0f); // 18 now 3.25 m from the CAM, 17 still 10.2 m.

            Assert.IsTrue(InvariantEnforcer.Enforce(snap, pool, 2, Tick, a));

            Assert.AreEqual(MarkMode.Zonal, a[0].Mode, "17 is farther and is demoted");
            Assert.AreEqual(MarkMode.ManMark, a[1].Mode, "18 is closer and keeps the mark");
        }

        /// <summary>An emergency override outranks a closer ordinary marker.</summary>
        [Test]
        public void EmergencyOverrideKept_OverCloserOrdinaryMarker()
        {
            DefensiveSnapshot snap = DuplicateMarkerSnapshot(0, out int[] pool, out MarkAssignment[] a);
            snap.MarkMovementActive = true;
            snap.Agents[0].Position = new Vector2(38.0f, 34.0f);  // 17 is the closer one...
            a[1].Mode = MarkMode.InterceptRunner;                  // ...but 18 holds the emergency override.
            a[1].OverriddenThisTick = true;

            Assert.IsTrue(InvariantEnforcer.Enforce(snap, pool, 2, Tick, a));

            Assert.AreEqual(MarkMode.Zonal, a[0].Mode);
            Assert.AreEqual(MarkMode.InterceptRunner, a[1].Mode);
            Assert.IsTrue(a[1].OverriddenThisTick);
        }

        /// <summary>Distinct opponents are untouched; four markers on one man leave exactly one, order-independent.</summary>
        [Test]
        public void ManyDuplicates_ExactlyOneKeeper_DistinctTargetsUntouched()
        {
            Vector2 cam = new Vector2(36.75f, 34.0f);
            DefensiveAgentSnapshot[] agents = new DefensiveAgentSnapshot[]
            {
                SnapshotBuilder.MakeAgent(entityId: 11, position: new Vector2(46.0f, 20.0f), baselineSlot: new Vector2(46.0f, 20.0f), line: LineId.Midfield),
                SnapshotBuilder.MakeAgent(entityId: 12, position: new Vector2(44.0f, 30.0f), baselineSlot: new Vector2(44.0f, 30.0f), line: LineId.Midfield),
                SnapshotBuilder.MakeAgent(entityId: 13, position: new Vector2(43.0f, 38.0f), baselineSlot: new Vector2(43.0f, 38.0f), line: LineId.Midfield),
                SnapshotBuilder.MakeAgent(entityId: 14, position: new Vector2(47.0f, 50.0f), baselineSlot: new Vector2(47.0f, 50.0f), line: LineId.Midfield),
                SnapshotBuilder.MakeAgent(entityId: 15, position: new Vector2(60.0f, 10.0f), baselineSlot: new Vector2(60.0f, 10.0f), line: LineId.Midfield),
                SnapshotBuilder.MakeAgent(entityId: 100, teamId: 1, position: cam, firstTouch: 14f),
                SnapshotBuilder.MakeAgent(entityId: 101, teamId: 1, position: new Vector2(58.0f, 12.0f), firstTouch: 14f),
            };
            DefensiveSnapshot snap = SnapshotBuilder.Build(agents, tick: Tick);
            snap.MarkMovementActive = true;

            int[] pool = new int[] { 11, 12, 13, 14, 15 };
            MarkAssignment[] a = new MarkAssignment[5];
            for (int i = 0; i < 4; i++)
                a[i] = new MarkAssignment { AgentEntityId = pool[i], Mode = MarkMode.ManMark, TargetEntityId = 100, TargetPosition = cam, ValidThroughTick = Tick };
            // INTERCEPT_RUNNER is uncounted by Invariant 2, so the four duplicates + this one stay within the cap.
            a[4] = new MarkAssignment { AgentEntityId = 15, Mode = MarkMode.InterceptRunner, TargetEntityId = 101, TargetPosition = new Vector2(58.0f, 12.0f), ValidThroughTick = Tick };

            Assert.IsTrue(InvariantEnforcer.Enforce(snap, pool, 5, Tick, a));

            int onCam = 0, keeper = -1;
            for (int i = 0; i < 4; i++)
                if (a[i].Mode == MarkMode.ManMark) { onCam++; keeper = a[i].AgentEntityId; }
            Assert.AreEqual(1, onCam, "exactly one marker on the CAM");
            Assert.AreEqual(13, keeper, "13 is nearest the CAM (55.06 m² vs 68.56 / 281.6 / 361)");
            Assert.AreEqual(MarkMode.InterceptRunner, a[4].Mode, "the distinct opponent's marker is untouched");
            Assert.AreEqual(101, a[4].TargetEntityId);
        }

        /// <summary>
        /// Review M2: Invariant 4 must run before the MAN_MARK cap. Five midfield marks: agents 1 and 2 on X, 3 on Y,
        /// 4 on Z, 5 on W (the lowest-threat man). Armed (order 1-3-4-2): the duplicate on X is removed first, four
        /// marks remain, none exceeds the cap, and W stays marked. The old order (cap first) dropped W's marker and
        /// then the duplicate, leaving 3 marks and W unmarked. Switch off (order 1-2-3): unchanged — the cap drops W's
        /// marker and the duplicate on X survives.
        /// </summary>
        [TestCase(0)]
        [TestCase(1)]
        public void CapRunsAfterInvariant4_DuplicatesDoNotCostALoneMarkersMark(int team)
        {
            MarkAssignment[] armed = RunFiveMarks(team, markMovementActive: true, out int[] pool);
            int marks = 0;
            var targets = new System.Collections.Generic.HashSet<int>();
            for (int p = 0; p < 5; p++)
            {
                if (armed[p].Mode != MarkMode.ManMark) continue;
                marks++;
                Assert.IsTrue(targets.Add(armed[p].TargetEntityId), $"opponent {armed[p].TargetEntityId} has two markers");
            }
            Assert.AreEqual(4, marks, "four marks, each opponent marked once");
            Assert.AreEqual(MarkMode.ManMark, armed[0].Mode, "agent 1 keeps X (tie on cost, lower id)");
            Assert.AreEqual(MarkMode.Zonal, armed[1].Mode, "agent 2, the duplicate on X, is demoted");
            Assert.AreEqual(MarkMode.ManMark, armed[4].Mode, "W (lowest threat) keeps his marker");
            Assert.AreEqual(103, armed[4].TargetEntityId);

            MarkAssignment[] off = RunFiveMarks(team, markMovementActive: false, out _);
            Assert.AreEqual(MarkMode.ManMark, off[0].Mode);
            Assert.AreEqual(MarkMode.ManMark, off[1].Mode, "switch off: the duplicate on X survives exactly as before W13.1");
            Assert.AreEqual(MarkMode.Zonal, off[4].Mode, "switch off: the cap demotes the lowest-threat man's marker (pre-W13.1 order)");
        }

        private static MarkAssignment[] RunFiveMarks(int team, bool markMovementActive, out int[] pool)
        {
            Vector2[] own = { new Vector2(38, 30), new Vector2(38, 38), new Vector2(40, 22), new Vector2(40, 46), new Vector2(55, 34) };
            Vector2[] opp = { new Vector2(30, 34), new Vector2(35, 20), new Vector2(35, 48), new Vector2(60, 34) };
            float[] ft = { 14f, 14f, 14f, 8f };
            DefensiveAgentSnapshot[] agents = new DefensiveAgentSnapshot[9];
            for (int i = 0; i < 5; i++)
                agents[i] = SnapshotBuilder.MakeAgent(entityId: i + 1, teamId: team, position: own[i], baselineSlot: own[i], line: LineId.Midfield);
            for (int i = 0; i < 4; i++)
                agents[5 + i] = SnapshotBuilder.MakeAgent(entityId: 100 + i, teamId: 1 - team, position: opp[i], firstTouch: ft[i]);
            DefensiveSnapshot snap = SnapshotBuilder.Build(agents, tick: Tick);
            snap.DefensiveTeamId = team;
            snap.MarkMovementActive = markMovementActive;

            pool = new int[] { 1, 2, 3, 4, 5 };
            int[] tgt = { 100, 100, 101, 102, 103 };
            MarkAssignment[] a = new MarkAssignment[5];
            for (int i = 0; i < 5; i++)
                a[i] = new MarkAssignment { AgentEntityId = i + 1, Mode = MarkMode.ManMark, TargetEntityId = tgt[i], TargetPosition = opp[tgt[i] - 100], ValidThroughTick = Tick };
            Assert.IsTrue(InvariantEnforcer.Enforce(snap, pool, 5, Tick, a));
            return a;
        }

        /// <summary>
        /// Whole pipeline: two midfielders in range of the single attacker both commit MAN_MARK on him after the
        /// dwell with the flag off (today), exactly one with it on. Both teams (canonical inputs identical).
        /// </summary>
        [TestCase(0)]
        [TestCase(1)]
        public void WholePipeline_DuplicatesOnlyWhenFlagOff(int team)
        {
            int[] manMarkersOff = RunPipeline(team, markMovementActive: false);
            int[] manMarkersOn  = RunPipeline(team, markMovementActive: true);

            Assert.AreEqual(2, manMarkersOff.Length, "flag off: both midfielders mark the lone attacker (D3, today's behaviour)");
            Assert.AreEqual(1, manMarkersOn.Length, "flag on: Invariant 4 leaves one");
            Assert.AreEqual(manMarkersOff[0], manMarkersOn[0], "the lower EntityId / nearer marker is kept");
        }

        private static int[] RunPipeline(int team, bool markMovementActive)
        {
            DefensiveAITick tick = new DefensiveAITick();
            int own0 = team * 11 + 1, own1 = team * 11 + 2, opp = (1 - team) * 11 + 5;

            for (int t = 1; t <= DefensiveAIConstants.MarkDwellTicks + 2; t++)
            {
                DefensiveAgentSnapshot[] agents = new DefensiveAgentSnapshot[]
                {
                    SnapshotBuilder.MakeAgent(entityId: own0, teamId: team, position: new Vector2(45.0f, 28.0f),
                        baselineSlot: new Vector2(45.0f, 28.0f), line: LineId.Midfield),
                    SnapshotBuilder.MakeAgent(entityId: own1, teamId: team, position: new Vector2(45.0f, 40.0f),
                        baselineSlot: new Vector2(45.0f, 40.0f), line: LineId.Midfield),
                    SnapshotBuilder.MakeAgent(entityId: opp, teamId: 1 - team, position: new Vector2(36.75f, 34.0f), firstTouch: 14f),
                };
                Array.Sort(agents, (x, y) => x.EntityId.CompareTo(y.EntityId)); // the snapshot is EntityId-ascending

                DefensiveSnapshot snap = SnapshotBuilder.Build(agents,
                    ballPosition: new Vector2(90.0f, 34.0f), ballVelocity: new Vector2(0.5f, 0.0f), tick: t);
                snap.DefensiveTeamId = team;
                snap.MarkMovementActive = markMovementActive;
                tick.Tick(snap);
            }

            int n = 0;
            int[] tmp = new int[2];
            foreach (int id in new[] { own0, own1 })
            {
                MarkAssignment m = tick.GetAssignment(id);
                if (m.Mode == MarkMode.ManMark && m.TargetEntityId == opp) tmp[n++] = id;
            }
            int[] result = new int[n];
            Array.Copy(tmp, result, n);
            return result;
        }

        /// <summary>
        /// Termination and post-conditions over 1,000 deterministic pseudo-random assignment states (the
        /// four-invariant analogue of the T-DA-DET-006 harness, which remains an Ignored stub): Enforce
        /// never loops, and whenever it reports success no duplicate target remains and Invariant 2 holds.
        /// </summary>
        [Test]
        public void Randomised_FourInvariants_Terminate_AndHold()
        {
            ulong state = 0x9E3779B97F4A7C15UL;
            for (int iter = 0; iter < 1000; iter++)
            {
                DefensiveAgentSnapshot[] agents = new DefensiveAgentSnapshot[20];
                int[] pool = new int[10];
                MarkAssignment[] a = new MarkAssignment[10];
                for (int i = 0; i < 10; i++)
                {
                    float ax = 20.0f + Next(ref state) * 40.0f, ay = 5.0f + Next(ref state) * 58.0f;
                    agents[i] = SnapshotBuilder.MakeAgent(entityId: i + 1, teamId: 0, position: new Vector2(ax, ay),
                        baselineSlot: new Vector2(ax, ay), line: i < 4 ? LineId.Defense : LineId.Midfield);
                    agents[10 + i] = SnapshotBuilder.MakeAgent(entityId: 101 + i, teamId: 1,
                        position: new Vector2(15.0f + Next(ref state) * 50.0f, 5.0f + Next(ref state) * 58.0f), firstTouch: 1f + Next(ref state) * 19f);
                    pool[i] = i + 1;
                    int tgt = (int)(Next(ref state) * 4.0f); // only 4 distinct targets -> heavy duplication
                    bool mark = Next(ref state) < 0.8f;
                    a[i] = mark
                        ? new MarkAssignment { AgentEntityId = i + 1, Mode = Next(ref state) < 0.8f ? MarkMode.ManMark : MarkMode.InterceptRunner,
                            TargetEntityId = 101 + tgt, TargetPosition = agents[10 + tgt].Position, ValidThroughTick = Tick }
                        : MarkAssignment.MakeZonal(i + 1, agents[i].BaselineSlot, Tick);
                }

                DefensiveSnapshot snap = SnapshotBuilder.Build(agents, tick: Tick);
                snap.MarkMovementActive = true;

                // No emergency overrides here, so every violation is demotable: Enforce must succeed.
                Assert.IsTrue(InvariantEnforcer.Enforce(snap, pool, 10, Tick, a), $"iter {iter}: F4 on a demotable state");

                int manMarks = 0;
                for (int p = 0; p < 10; p++)
                {
                    if (a[p].Mode == MarkMode.ManMark) manMarks++;
                    if (a[p].Mode == MarkMode.Zonal || a[p].TargetEntityId < 0) continue;
                    for (int q = p + 1; q < 10; q++)
                        Assert.IsFalse(a[q].Mode != MarkMode.Zonal && a[q].TargetEntityId == a[p].TargetEntityId,
                            $"iter {iter}: agents {p} and {q} both mark {a[p].TargetEntityId}");
                }
                Assert.LessOrEqual(manMarks, DefensiveAIConstants.MaxManMarkAssignments, $"iter {iter}");
            }
        }

        private static float Next(ref ulong s)
        {
            unchecked
            {
                s += 0x9E3779B97F4A7C15UL;
                ulong z = s;
                z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
                z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
                z ^= z >> 31;
                return (z >> 40) / (float)(1UL << 24);
            }
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                                                   |
// | 1.0     | 2026-11-10 | —      | W13.1 / ERR-014-010: MarkTargetComposer worked examples A / B / degenerate (team 0 and 1),  |
// |         |            |        |   goal-side property, no-target cases, COVER_GK_ZONE passthrough, stand-off constant        |
// |         |            |        |   invariant, Invariant 4 (worked example, default-off identity, whole-pipeline duplicates,   |
// |         |            |        |   1,000-state termination harness) and zero allocation.                                      |
// | 1.1     | 2026-11-12 | —      | W13.1 review M2: CapRunsAfterInvariant4_DuplicatesDoNotCostALoneMarkersMark (5 marks: armed leaves 4,   |
// |         |            |        |   each opponent once, W kept; switch off keeps the pre-W13.1 cap-first result).                          |
#endregion
