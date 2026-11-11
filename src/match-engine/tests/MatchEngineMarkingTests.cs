// File:     src/match-engine/tests/MatchEngineMarkingTests.cs
// Created:  2026-11-11
// Modified: 2026-11-11
// Author:   —
// Spec:     Defensive AI #14 §3.13.1 / §3.10.2 (Invariant 4), Decision Tree #8 §3.1.7.2 / §3.1.8.2,
//           Tactical Instructions #21 §3.4 (TightMarking), ERR-014-010 / ERR-008-027 / ERR-021-010, Code Standards #20
// Purpose:  W13.1 engine-level locks for #14 mark-target movement (switched OFF by default): acquisition,
//           closure goal-side, the 20 m Invariant 3 boundary, anti-chaos invariants every stride, the
//           no-target cases, substitution / dismissal, snapshot-restore continuation, two-run determinism,
//           the switch-off identity (incl. the env-gated TD_MARK_IDENTITY_PROBE), and a descriptive
//           INTERCEPT-versus-MOVE record. Every geometry case runs for the HOME and the AWAY team defending.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using NUnit.Framework;
using UnityEngine;

using TacticalDirector.AgentMovement;
using TacticalDirector.BallPhysics;
using TacticalDirector.DecisionTree;
using TacticalDirector.DefensiveAI;
using TacticalDirector.DeterministicSim;
using TacticalDirector.EventSystem;
using TacticalDirector.PressingAI;

namespace TacticalDirector.MatchEngine
{
    /// <summary>
    /// W13.1 engine-level marking tests. Scenarios are authored in the DEFENDING team's canonical frame
    /// (own goal at x = 0, attacking +X) and mapped to world space with the 180-degree pitch rotation for the
    /// away team, so one body of assertions covers both teams (the ERR-008-002 / ERR-014-008 discipline).
    /// </summary>
    [TestFixture]
    public sealed class MatchEngineMarkingTests
    {
        private const ulong Seed = 0x0123456789ABCDEFUL;
        private const int Per = MatchEngineConstants.PLAYERS_PER_TEAM;
        private static readonly int Stride = DeterministicSimConstants.AI_PHASE_STRIDE;

        [TearDown]
        public void ResetEventBus()
        {
            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = false;
            EventBus.ResetForNewMatch();
        }

        // ── frame helpers ────────────────────────────────────────────────────────

        /// <summary>Canonical (defending team's attack-+X frame) to world: identity for home, 180-degree rotation for away. Self-inverse.</summary>
        private static Vector2 W(int defTeam, Vector2 canonical) =>
            defTeam == 0 ? canonical : new Vector2(105.0f - canonical.x, 68.0f - canonical.y);

        private static void Place(MatchEngine e, int id, Vector2 world)
        {
            AgentState s = e.TestOnly_AgentSnapshot(id);
            s.Position = world;
            s.Velocity = Vector2.zero;
            e.TestOnly_SetAgent(id, s);
        }

        private static void PlaceMoving(MatchEngine e, int id, Vector2 worldPos, Vector2 worldVel)
        {
            AgentState s = e.TestOnly_AgentSnapshot(id);
            s.Position = worldPos;
            s.Velocity = worldVel;
            e.TestOnly_SetAgent(id, s);
        }

        private static Vector2 Pos(MatchEngine e, int id) => e.TestOnly_AgentSnapshot(id).Position;

        // ── the rig ──────────────────────────────────────────────────────────────

        /// <summary>
        /// One defending team (<see cref="Def"/>) against a parked opposition. Every tick the rig re-places the
        /// opposition (parked far away, except the carrier and the attacker), the ball at the carrier's feet, the
        /// possession, and optionally locks the defending team onto its live formation slots; then runs one tick.
        /// The carrier is the opposition's slot 5, the attacker its slot 9 (ST). The two centre backs of the
        /// defending team are its slots 2 and 3.
        /// </summary>
        private sealed class Rig
        {
            public readonly MatchEngine E;
            public readonly int Def, Own, Opp, Carrier, Attacker, Cb2, Cb3, Gk;
            public Vector2? AttackerCanonical;          // null = parked far away
            public bool LockOwn;                        // lock the defending team onto its live slots
            public Vector2 CarrierCanonical = new Vector2(52.0f, 14.0f);
            public bool GiveBallToDefenders;            // possession to the defending team's slot 5 instead
            public Vector2? CarrierFacing;              // canonical facing of the carrier (null = unchanged)
            public int PassKicker = -1, PassReceiver = -1;   // a pass in flight to PassReceiver (no carrier)

            public Rig(MatchEngine e, int def)
            {
                // The rig teleports agents with zero velocity every tick; a sprint command issued from a standing
                // start can then trip the movement system's degenerate-velocity assert (MatchBalanceDiagnosticTests
                // precedent). The fixture TearDown restores strict log checking.
                UnityEngine.TestTools.LogAssert.ignoreFailingMessages = true;
                E = e; Def = def; Own = def * Per; Opp = (1 - def) * Per;
                Carrier = Opp + 5; Attacker = Opp + 9; Cb2 = Own + 2; Cb3 = Own + 3; Gk = Own;
            }

            public Vector2 SlotCanonical(int id) => W(Def, E.TestOnly_FormationSlot(id));

            public void Tick()
            {
                for (int k = 0; k < Per; k++)
                {
                    int id = Opp + k;
                    if (id == Carrier || id == Attacker) continue;
                    Vector2 c = k == 0 ? new Vector2(99.5f, 34.0f) : new Vector2(70.0f + (k % 3) * 3.0f, 6.0f + k * 5.5f);
                    Place(E, id, W(Def, c));
                }
                Place(E, Attacker, W(Def, AttackerCanonical ?? new Vector2(80.0f, 34.0f)));

                int holder = GiveBallToDefenders ? Own + 5 : Carrier;
                Vector2 carrierWorld = GiveBallToDefenders ? Pos(E, holder) : W(Def, CarrierCanonical);
                if (!GiveBallToDefenders)
                {
                    Place(E, Carrier, carrierWorld);
                    if (CarrierFacing.HasValue)
                    {
                        AgentState cs = E.TestOnly_AgentSnapshot(Carrier);
                        cs.FacingDirection = Def == 0 ? CarrierFacing.Value : -CarrierFacing.Value;
                        E.TestOnly_SetAgent(Carrier, cs);
                    }
                }
                BallState b = E.TestOnly_BallSnapshot;
                if (PassReceiver >= 0)
                {
                    // A live pass: the ball 4 m from the receiver, travelling at him at 8 m/s; nobody on the ball.
                    Vector2 rp = Pos(E, PassReceiver);
                    Vector2 toward = (W(Def, new Vector2(1.0f, 0.0f)) - W(Def, Vector2.zero)).normalized;
                    Vector2 from = rp - toward * 4.0f;
                    b.Position = new Vector3(from.x, from.y, 0.11f);
                    b.Velocity = new Vector3(toward.x * 8.0f, toward.y * 8.0f, 0.0f);
                    E.TestOnly_SetBall(in b);
                    E.TestOnly_SetPossession(MatchEngineConstants.NO_POSSESSION);
                    E.TestOnly_ArmPassInFlight(PassKicker, PassReceiver);
                }
                else
                {
                    b.Position = new Vector3(carrierWorld.x, carrierWorld.y, 0.11f);
                    b.Velocity = Vector3.zero;
                    E.TestOnly_SetBall(in b);
                    E.TestOnly_SetPossession(holder);
                }

                if (LockOwn)
                    for (int k = 0; k < Per; k++)
                        if (Own + k != Own + 5 || !GiveBallToDefenders)
                            Place(E, Own + k, E.TestOnly_FormationSlot(Own + k));

                E.RunTick();
            }

            public void Strides(int n)
            {
                for (int i = 0; i < n * Stride; i++) Tick();
            }

            /// <summary>The centre back (slot 2 or 3) currently holding MAN_MARK on the attacker, or -1.</summary>
            public int MarkingCb()
            {
                int found = -1, count = 0;
                foreach (int cb in new[] { Cb2, Cb3 })
                {
                    MarkAssignment a = E.TestOnly_MarkAssignment(cb);
                    if (a.Mode == MarkMode.ManMark && a.TargetEntityId == Attacker) { found = cb; count++; }
                }
                return count == 1 ? found : -1;
            }
        }

        /// <summary>Warm-up: the attacker parked, the defending team locked at its live slots, until #12 reports OutOfPoss and slots settle.</summary>
        private static Rig WarmRig(int def, bool armed = true)
        {
            var e = new MatchEngine(Seed);
            e.TestOnly_ArmMarkMovement(armed);
            var rig = new Rig(e, def) { LockOwn = true };
            rig.Strides(6);
            return rig;
        }

        /// <summary>Introduces the attacker 2.1 m deeper than the centre backs' slots, on the pitch's centre line, in canonical coordinates.</summary>
        private static Vector2 IntroduceAttacker(Rig rig)
        {
            Vector2 cbSlot = rig.SlotCanonical(rig.Cb2);
            Vector2 o = new Vector2(cbSlot.x - 2.1f, 34.0f);
            rig.AttackerCanonical = o;
            return o;
        }

        // ── 1. acquisition ───────────────────────────────────────────────────────

        /// <summary>
        /// The back four sit at their slots; the attacker is 2.1 m deeper than the centre backs and on the centre
        /// line (equidistant from both, well inside the 15 m MAN_MARK radius); the carrier is more than 25 m away.
        /// Within MarkDwellTicks + 1 = 5 strides exactly one centre back holds MAN_MARK on him, and the engine
        /// target is the #14 section 3.13.1 point: 2.0 m from him toward the own goal (worked example A, mirrored).
        /// </summary>
        [TestCase(0)]
        [TestCase(1)]
        public void Acquisition_ExactlyOneCentreBackMarks_AndTargetIsTheGoalSidePoint(int def)
        {
            Rig rig = WarmRig(def);
            Assert.AreEqual(PositioningAI.Phase.OutOfPoss, rig.E.TestOnly_PositioningPhase(def), "precondition: the defending team is out of possession");
            Vector2 o = IntroduceAttacker(rig);
            Assert.Greater(Vector2.Distance(W(def, o), W(def, rig.CarrierCanonical)), 25.0f, "precondition: carrier far from the attacker");

            int cb = -1;
            for (int s = 0; s < DefensiveAIConstants.MarkDwellTicks + 1 && cb < 0; s++)
            {
                rig.Strides(1);
                cb = rig.MarkingCb();
            }

            Assert.GreaterOrEqual(cb, 0, "exactly one centre back holds MAN_MARK within MarkDwellTicks + 1 strides");
            int other = cb == rig.Cb2 ? rig.Cb3 : rig.Cb2;
            Assert.AreEqual(MarkMode.Zonal, rig.E.TestOnly_MarkAssignment(other).Mode, "the other centre back stays ZONAL (Invariant 1 / 4)");

            (bool has, Vector2 target) = rig.E.TestOnly_MarkTarget(cb);
            Assert.IsTrue(has);
            Vector2 expected = W(def, new Vector2(o.x - DefensiveAIConstants.MarkStandoffM, 34.0f));
            Assert.AreEqual(expected.x, target.x, 1e-3f);
            Assert.AreEqual(expected.y, target.y, 1e-3f);

            Assert.IsFalse(rig.E.TestOnly_MarkTarget(other).HasTarget, "no target for the unmarking centre back");
            Assert.IsFalse(rig.E.TestOnly_MarkTarget(rig.Gk).HasTarget, "no target for the goalkeeper");
            for (int k = 0; k < Per; k++)
                Assert.IsFalse(rig.E.TestOnly_MarkTarget(rig.Opp + k).HasTarget, "no target for the possessing team");
        }

        // ── 2. closure ───────────────────────────────────────────────────────────

        /// <summary>
        /// N = 60 strides. Worst case walking only (the cap near the point is JogEnter 2.2 m/s): accelerate to
        /// 2.2 m/s at 2.0 m/s^2 (1.1 s, 1.21 m), then (9.13 - 1.0 - 1.21) / 2.2 = 3.15 s: 4.25 s = 43 strides, plus 5
        /// acquisition, 2 dispatch / turn margin and 10 (1 s) of safety = 60. The marker must end within 3.0 m of his
        /// stationary man AND goal-side of him. Goal-side holds because PRESS priority 2 is skipped for a marker.
        /// </summary>
        [TestCase(0)]
        [TestCase(1)]
        public void Closure_MarkerEndsWithin3m_AndGoalSideOfHisMan(int def)
        {
            Rig rig = WarmRig(def);
            Vector2 o = IntroduceAttacker(rig);
            rig.Strides(DefensiveAIConstants.MarkDwellTicks + 1);
            int cb = rig.MarkingCb();
            Assert.GreaterOrEqual(cb, 0, "precondition: acquired");
            rig.LockOwn = false;
            float startDist = Vector2.Distance(Pos(rig.E, cb), Pos(rig.E, rig.Attacker));

            rig.Strides(60);

            Vector2 marker = Pos(rig.E, cb);
            Vector2 man = Pos(rig.E, rig.Attacker);
            Vector2 goal = W(def, DefensiveAIConstants.OwnGoalCenter);
            Assert.AreEqual(MarkMode.ManMark, rig.E.TestOnly_MarkAssignment(cb).Mode, "still holds the mark");
            Assert.LessOrEqual(Vector2.Distance(marker, man), 3.0f, $"within 3.0 m of his man (started {startDist:F2} m away)");
            Assert.Less(Vector2.Distance(marker, goal), Vector2.Distance(man, goal), "goal-side of his man");
            Assert.Less(Vector2.Distance(marker, man), startDist, "he closed the gap");
        }

        // ── 3. the 20 m boundary (Invariant 3 on the live position) ──────────────

        /// <summary>
        /// The man's distance from the marker's #12 SLOT follows <paramref name="distanceAt"/> (per stride). The marker
        /// is pinned 7 m deeper than his slot so the man stays inside the 15 m candidate radius while crossing the
        /// 20 m line. Asserts: no MAN_MARK is published on a stride where the man is more than 20 m from the slot, a
        /// consumed target is within 20 + s of the slot, and at least MarkDwellTicks strides separate a demotion
        /// from the next acquisition. Returns (published strides, demotions, reacquisitions).
        /// </summary>
        private static (int Published, int Demotions, int Reacquisitions) RunBoundary(int def, Func<int, float> distanceAt, int strides)
        {
            var e = new MatchEngine(Seed);
            e.TestOnly_ArmMarkMovement(true);
            var rig = new Rig(e, def) { LockOwn = true };
            rig.Strides(6);
            int marker = rig.Own + 6;                  // a central midfielder: Invariant 1 (back line) cannot interfere
            rig.LockOwn = false;

            int published = 0, demotions = 0, reacq = 0, lastDemotion = -1000;
            bool prevMark = false;
            float s = DefensiveAIConstants.MarkStandoffM;
            for (int st = 0; st < strides; st++)
            {
                Vector2 slot = rig.SlotCanonical(marker);
                float d = distanceAt(st);
                Vector2 man = new Vector2(slot.x - d, slot.y);          // d from the marker's SLOT, canonical
                rig.AttackerCanonical = man;
                for (int t = 0; t < Stride; t++)
                {
                    foreach (int k in new[] { 1, 2, 3, 4, 5, 7, 8, 9, 10 }) Place(e, rig.Own + k, e.TestOnly_FormationSlot(rig.Own + k));
                    Place(e, marker, W(def, new Vector2(man.x + 5.0f, man.y)));   // pinned 5 m from the man, ~20 m from his slot
                    rig.Tick();
                }

                MarkAssignment a = e.TestOnly_MarkAssignment(marker);
                bool marked = a.Mode == MarkMode.ManMark && a.TargetEntityId == rig.Attacker;
                // d is the man's distance from the slot AT THE AI TICK (the placed value); a stride-end measurement
                // would include the drift of the 5 intervening physics ticks.
                if (d > DefensiveAIConstants.MaxMarkDisplacementM)
                    Assert.IsFalse(marked, $"stride {st}: MAN_MARK published with the man {d:F2} m from the slot");
                if (marked)
                {
                    published++;
                    (bool has, Vector2 target) = e.TestOnly_MarkTarget(marker);
                    Assert.IsTrue(has, $"stride {st}: a published mark yields a target");
                    Assert.LessOrEqual(Vector2.Distance(target, e.TestOnly_FormationSlot(marker)),
                        DefensiveAIConstants.MaxMarkDisplacementM + s + 1e-3f, $"stride {st}: consumed target within 20 + s of the slot");
                }
                if (prevMark && !marked) { demotions++; lastDemotion = st; }
                if (!prevMark && marked && lastDemotion >= 0)
                {
                    reacq++;
                    Assert.GreaterOrEqual(st - lastDemotion, DefensiveAIConstants.MarkDwellTicks,
                        $"stride {st}: re-acquired only {st - lastDemotion} strides after the demotion at {lastDemotion}");
                }
                prevMark = marked;
            }
            return (published, demotions, reacq);
        }

        /// <summary>The plan's strict alternation 19.9 / 20.1 m: the mark is never published on a > 20 m stride.</summary>
        [TestCase(0)]
        [TestCase(1)]
        public void Boundary_StrictAlternation_NeverPublishesBeyond20m(int def)
        {
            RunBoundary(def, st => st % 2 == 0 ? 19.9f : 20.1f, 40);
        }

        /// <summary>
        /// Eight strides at 19.9 m then one at 20.1 m, repeated: the mark is acquired, demoted on the 20.1 stride
        /// (the lock does not exempt it), and re-acquired only after a full dwell. Non-vacuous: demotions and
        /// re-acquisitions must both occur.
        /// </summary>
        [TestCase(0)]
        [TestCase(1)]
        public void Boundary_HoldThenCross_DemotesAndReacquiresAfterTheDwell(int def)
        {
            (int published, int demotions, int reacq) = RunBoundary(def, st => st % 9 == 8 ? 20.1f : 19.9f, 60);
            Assert.Greater(published, 10, "the mark is held for most strides");
            Assert.GreaterOrEqual(demotions, 3, "each 20.1 m stride demotes");
            Assert.GreaterOrEqual(reacq, 2, "and the mark is re-acquired afterwards");
        }

        // ── 4. invariants every stride ───────────────────────────────────────────

        /// <summary>
        /// Eight attackers weave through the defending half on deterministic paths (some above the 3 m/s runner
        /// threshold). Every stride, armed: at least 3 of the back four ZONAL, at most 4 MAN_MARK, no opponent
        /// marked by two agents, and every published target lies within the stand-off of the marked man's live
        /// position (at the AI tick) and no farther from the own goal than he is. With the switch off no target is
        /// ever written.
        /// </summary>
        [TestCase(0)]
        [TestCase(1)]
        public void Invariants_HoldEveryStride_WhileMarksMove(int def)
        {
            (int strides, int marks, int dupArmed, int targets) = RunCrowd(def, armed: true, check: true);
            (_, _, _, int targetsOff) = RunCrowd(def, armed: false, check: false);

            Assert.Greater(marks, 20, "the crowd scenario produces marks");
            Assert.Greater(targets, 20, "and mark targets");
            Assert.AreEqual(0, dupArmed, "armed: never two markers on one opponent");
            Assert.AreEqual(0, targetsOff, "switch off: no mark target is ever written");
            Assert.AreEqual(120, strides);
        }

        /// <summary>
        /// Two central midfielders in range of ONE attacker standing between them. Switch off (today): both commit
        /// MAN_MARK on him (D3, no exclusivity). Armed: Invariant 4 leaves exactly one. The same geometry for both
        /// teams. Proves Invariant 4 does work in the engine and that the off path does not run it.
        /// </summary>
        [TestCase(0, false, 2)]
        [TestCase(1, false, 2)]
        [TestCase(0, true, 1)]
        [TestCase(1, true, 1)]
        public void Invariant4_TwoMidfieldersOneAttacker(int def, bool armed, int expectedMarkers)
        {
            Rig rig = WarmRig(def, armed);
            rig.CarrierCanonical = new Vector2(75.0f, 60.0f);   // out of every defender's 15 m radius
            rig.Strides(2);
            Vector2 s6 = rig.SlotCanonical(rig.Own + 6);
            rig.AttackerCanonical = new Vector2(s6.x - 3.0f, 34.0f);
            rig.Strides(DefensiveAIConstants.MarkDwellTicks + 3);

            int markers = 0;
            foreach (int id in new[] { rig.Own + 6, rig.Own + 7 })
            {
                MarkAssignment a = rig.E.TestOnly_MarkAssignment(id);
                if (a.Mode == MarkMode.ManMark && a.TargetEntityId == rig.Attacker) markers++;
            }
            Assert.AreEqual(expectedMarkers, markers);
        }

        private static (int Strides, int Marks, int Duplicates, int Targets) RunCrowd(int def, bool armed, bool check)
        {
            var e = new MatchEngine(Seed);
            e.TestOnly_ArmMarkMovement(armed);
            int own = def * Per, opp = (1 - def) * Per;
            int carrier = opp + 5;
            int strides = 0, marks = 0, dups = 0, targets = 0;

            for (int st = 0; st < 120; st++)
            {
                for (int t = 0; t < Stride; t++)
                {
                    float time = (st * Stride + t) / 60.0f;
                    for (int k = 1; k < Per; k++)
                    {
                        int id = opp + k;
                        if (id == carrier) continue;
                        // Weavers 1..8 cross the own half; speeds 1.5 - 4.6 m/s.
                        float ph = k * 0.9f, w = 0.25f + 0.04f * k;
                        Vector2 c = new Vector2(40.0f - 1.5f * k, 34.0f + (k % 2 == 0 ? 1 : -1) * 4.0f * k * 0.5f);
                        Vector2 p = c + new Vector2(14.0f * Mathf.Sin(w * time + ph), 6.0f * Mathf.Cos(w * time * 1.3f + ph));
                        Vector2 v = new Vector2(14.0f * w * Mathf.Cos(w * time + ph), -6.0f * w * 1.3f * Mathf.Sin(w * time * 1.3f + ph));
                        PlaceMoving(e, id, W(def, p), def == 0 ? v : -v);
                    }
                    Place(e, opp, W(def, new Vector2(99.5f, 34.0f)));
                    Place(e, carrier, W(def, new Vector2(60.0f, 14.0f)));
                    BallState b = e.TestOnly_BallSnapshot;
                    Vector2 cw = W(def, new Vector2(60.0f, 14.0f));
                    b.Position = new Vector3(cw.x, cw.y, 0.11f); b.Velocity = Vector3.zero;
                    e.TestOnly_SetBall(in b);
                    e.TestOnly_SetPossession(carrier);
                    e.RunTick();
                }
                strides++;

                int zonalBack = 0, manMarks = 0;
                var seen = new HashSet<int>();
                for (int k = 1; k < Per; k++)
                {
                    int id = own + k;
                    MarkAssignment a = e.TestOnly_MarkAssignment(id);
                    if (k <= 4 && a.Mode == MarkMode.Zonal) zonalBack++;
                    if (a.Mode == MarkMode.ManMark) manMarks++;
                    if (a.Mode != MarkMode.Zonal && a.TargetEntityId >= 0)
                    {
                        marks++;
                        if (!seen.Add(a.TargetEntityId)) dups++;
                    }

                    (bool has, Vector2 target) = e.TestOnly_MarkTarget(id);
                    if (!has) continue;
                    targets++;
                    if (!check || a.Mode == MarkMode.CoverGkZone) continue;
                    Vector2 man = W(def, a.TargetPosition);   // the marked man's live position at the AI tick (canonical -> world)
                    Vector2 goal = W(def, DefensiveAIConstants.OwnGoalCenter);
                    Assert.LessOrEqual(Vector2.Distance(target, man), DefensiveAIConstants.MarkStandoffM + 1e-3f, $"stride {st} agent {id}: within the stand-off of his man");
                    Assert.LessOrEqual(Vector2.Distance(target, goal), Vector2.Distance(man, goal) + 1e-3f, $"stride {st} agent {id}: goal-side");
                }

                if (check)
                {
                    Assert.GreaterOrEqual(zonalBack, DefensiveAIConstants.MinBacklineAgents, $"stride {st}: >= 3 of the back four ZONAL");
                    Assert.LessOrEqual(manMarks, DefensiveAIConstants.MaxManMarkAssignments, $"stride {st}: <= 4 MAN_MARK");
                }
            }
            return (strides, marks, dups, targets);
        }

        // ── 5. no-target cases ───────────────────────────────────────────────────

        [TestCase(0)]
        [TestCase(1)]
        public void NoTarget_SwitchOff(int def)
        {
            Rig rig = WarmRig(def, armed: false);
            IntroduceAttacker(rig);
            rig.Strides(12);

            Assert.GreaterOrEqual(rig.MarkingCb(), 0, "precondition: #14 still commits the mark with the switch off (assignment-only, as today)");
            for (int k = 0; k < 2 * Per; k++)
                Assert.IsFalse(rig.E.TestOnly_MarkTarget(k).HasTarget, $"agent {k}: no target with the switch off");
        }

        /// <summary>
        /// The mark gate: when the MARKING team itself gains the ball the very next stride composes no target for
        /// any of its agents, even while #12's phase hysteresis still lets #14 publish a MAN_MARK for a stride or two.
        /// </summary>
        [TestCase(0)]
        [TestCase(1)]
        public void NoTarget_ForATeamInPossession_CarrierAndPassInFlight(int def)
        {
            Rig rig = WarmRig(def);
            IntroduceAttacker(rig);
            rig.Strides(DefensiveAIConstants.MarkDwellTicks + 2);
            int cb = rig.MarkingCb();
            Assert.GreaterOrEqual(cb, 0, "precondition: a live target");
            Assert.IsTrue(rig.E.TestOnly_MarkTarget(cb).HasTarget);

            // (a) the marking team's carrier: possession flips to the defending team.
            rig.GiveBallToDefenders = true;
            rig.LockOwn = false;
            int stridesWithMarkButNoTarget = 0;
            for (int s = 0; s < 3; s++)
            {
                rig.Strides(1);
                if (rig.E.TestOnly_MarkAssignment(cb).Mode != MarkMode.Zonal) stridesWithMarkButNoTarget++;
                for (int k = 0; k < Per; k++)
                    Assert.IsFalse(rig.E.TestOnly_MarkTarget(rig.Own + k).HasTarget, $"stride {s}: possessing team agent {k} has no target");
            }
            Assert.Greater(stridesWithMarkButNoTarget, 0, "the gate, not the phase, removed the target for at least one stride");

            // (b) a pass in flight to a defending-team receiver: nobody is on the ball, but the team has it.
            Rig rig2 = WarmRig(def);
            IntroduceAttacker(rig2);
            rig2.Strides(DefensiveAIConstants.MarkDwellTicks + 2);
            int cb2 = rig2.MarkingCb();
            Assert.GreaterOrEqual(cb2, 0, "precondition: a live target");
            rig2.LockOwn = false;
            rig2.PassKicker = rig2.Own + 4;
            rig2.PassReceiver = rig2.Own + 6;
            int gateStrides = 0;
            for (int s = 0; s < 3; s++)
            {
                rig2.Strides(1);
                if (rig2.E.TestOnly_MarkAssignment(cb2).Mode != MarkMode.Zonal) gateStrides++;
                for (int k = 0; k < Per; k++)
                    Assert.IsFalse(rig2.E.TestOnly_MarkTarget(rig2.Own + k).HasTarget, $"pass in flight, stride {s}: agent {k} has no target");
            }
            Assert.Greater(gateStrides, 0, "pass in flight: the gate, not the phase, removed the target for at least one stride");
        }

        /// <summary>A presser (PRIMARY_PRESS / COVER_SHADOW) is outside #14's pool and gets no mark target; neither does the GK.</summary>
        [TestCase(0)]
        [TestCase(1)]
        public void NoTarget_PressersAndTheGoalkeeper(int def)
        {
            Rig rig = WarmRig(def);
            // A carrier facing a nearby touchline is #13's SIDELINE_TRAP: the nearest defender becomes PRIMARY_PRESS.
            rig.CarrierCanonical = new Vector2(45.0f, 64.5f);
            rig.CarrierFacing = new Vector2(0.0f, 1.0f);
            IntroduceAttacker(rig);
            int pressers = 0, targetsSeen = 0;
            for (int s = 0; s < 30; s++)
            {
                rig.Strides(1);
                for (int k = 0; k < Per; k++)
                {
                    int id = rig.Own + k;
                    PressRole role = rig.E.TestOnly_PressRole(id);
                    if (role == PressRole.PrimaryPress || role == PressRole.CoverShadow)
                    {
                        pressers++;
                        Assert.IsFalse(rig.E.TestOnly_MarkTarget(id).HasTarget, $"stride {s}: presser {id} ({role}) has a mark target");
                    }
                }
                Assert.IsFalse(rig.E.TestOnly_MarkTarget(rig.Gk).HasTarget, "the goalkeeper never has a mark target");
                for (int k = 0; k < Per; k++)
                    if (rig.E.TestOnly_MarkTarget(rig.Own + k).HasTarget) targetsSeen++;
            }
            Assert.Greater(pressers, 0, "the scenario produced pressers, or the assertion is vacuous");
            Assert.Greater(targetsSeen, 0, "and non-pressers still receive mark targets in the same run");
        }

        /// <summary>A sent-off agent gets no target; a sent-off marked man releases his marker's mark at once.</summary>
        [TestCase(0)]
        [TestCase(1)]
        public void NoTarget_SentOffMarker_And_SentOffMarkedMan(int def)
        {
            Rig rig = WarmRig(def);
            IntroduceAttacker(rig);
            rig.Strides(DefensiveAIConstants.MarkDwellTicks + 2);
            int cb = rig.MarkingCb();
            Assert.GreaterOrEqual(cb, 0);

            rig.E.TestOnly_SetIsSentOff(cb, true);
            rig.Strides(1);
            Assert.IsFalse(rig.E.TestOnly_MarkTarget(cb).HasTarget, "a sent-off marker has no target");

            Rig rig2 = WarmRig(def);
            IntroduceAttacker(rig2);
            rig2.Strides(DefensiveAIConstants.MarkDwellTicks + 2);
            int cb2 = rig2.MarkingCb();
            Assert.GreaterOrEqual(cb2, 0);
            rig2.E.TestOnly_SetIsSentOff(rig2.Attacker, true);
            rig2.Strides(1);
            Assert.IsFalse(rig2.E.TestOnly_MarkTarget(cb2).HasTarget, "the marked man was sent off: no target");
            Assert.AreEqual(MarkMode.Zonal, rig2.E.TestOnly_MarkAssignment(cb2).Mode, "and the mark is released to ZONAL");
        }

        // ── 6. substitution and dismissal ────────────────────────────────────────

        /// <summary>
        /// A substitute reuses the agent index and inherits #14's per-entity state, so he continues the mark
        /// (locked, not changed — wiring plan section 4.4). Two identical runs stay byte-identical across the swap.
        /// </summary>
        [TestCase(0)]
        [TestCase(1)]
        public void Substitution_DuringAMark_ContinuesDeterministically(int def)
        {
            List<byte[]> a = RunSubstitution(def, out int cbA, out bool hasA, out Vector2 targetA);
            List<byte[]> b = RunSubstitution(def, out _, out bool hasB, out Vector2 targetB);

            Assert.GreaterOrEqual(cbA, 0);
            Assert.IsTrue(hasA, "the incoming player inherits the index and so the mark target");
            Assert.AreEqual(hasA, hasB);
            Assert.AreEqual(targetA, targetB);
            Assert.AreEqual(a.Count, b.Count);
            for (int i = 0; i < a.Count; i++) CollectionAssert.AreEqual(a[i], b[i], $"digest diverged at {i}");
        }

        private static List<byte[]> RunSubstitution(int def, out int cb, out bool has, out Vector2 target)
        {
            Rig rig = WarmRig(def);
            IntroduceAttacker(rig);
            rig.Strides(DefensiveAIConstants.MarkDwellTicks + 2);
            cb = rig.MarkingCb();
            rig.LockOwn = false;
            var chain = new List<byte[]>();
            rig.E.SubstitutePlayer(def, cb, 0, SubstitutionReason.Tactical);
            for (int t = 0; t < 4 * Stride; t++) { rig.Tick(); chain.Add(rig.E.CurrentSnapshotDigest); }
            (has, target) = rig.E.TestOnly_MarkTarget(cb);
            return chain;
        }

        /// <summary>A dismissal mid-mark (marker or marked man) continues deterministically.</summary>
        [TestCase(0)]
        [TestCase(1)]
        public void Dismissal_DuringAMark_ContinuesDeterministically(int def)
        {
            List<byte[]> a = RunDismissal(def);
            List<byte[]> b = RunDismissal(def);
            Assert.AreEqual(a.Count, b.Count);
            for (int i = 0; i < a.Count; i++) CollectionAssert.AreEqual(a[i], b[i], $"digest diverged at {i}");
        }

        private static List<byte[]> RunDismissal(int def)
        {
            Rig rig = WarmRig(def);
            IntroduceAttacker(rig);
            rig.Strides(DefensiveAIConstants.MarkDwellTicks + 2);
            int cb = rig.MarkingCb();
            rig.LockOwn = false;
            var chain = new List<byte[]>();
            rig.E.TestOnly_SetIsSentOff(cb, true);
            for (int t = 0; t < 8 * Stride; t++) { rig.Tick(); chain.Add(rig.E.CurrentSnapshotDigest); }
            return chain;
        }

        // ── 7. restore continuation, armed, mid-dwell ────────────────────────────

        /// <summary>
        /// Saves mid-dwell (the candidate has accumulated but the mark has not committed), restores, re-arms the
        /// restored engine and continues 600 frames: the digest chain equals the uninterrupted run's at every tick.
        /// Nothing new is serialized: #14's hysteresis and last assignment already are. An UNarmed restored engine
        /// diverges (the config-level hazard, OD-11), which documents why the switch must be re-armed.
        /// </summary>
        [TestCase(0)]
        [TestCase(1)]
        public void RestoreContinuation_ArmedMidDwell_IsByteIdentical(int def)
        {
            Rig a = WarmRig(def);
            IntroduceAttacker(a);
            a.Strides(2);
            int cb = a.Cb3;
            MarkHysteresisState h = a.E.TestOnly_DefensiveState(def).Hysteresis[cb];
            Assert.Greater(h.HoldTicks, 0, "precondition: mid-dwell (candidate accumulating)");
            Assert.AreNotEqual(MarkMode.ManMark, a.E.TestOnly_MarkAssignment(cb).Mode, "precondition: not yet committed");
            a.LockOwn = false;

            SnapshotHeader header = a.E.CaptureDurableHeader();
            SnapshotPayload payload = a.E.CaptureDurablePayload();
            // Rig state that lives outside the engine and must be replicated on the restored engine.
            Vector2? attacker = a.AttackerCanonical;

            var reference = new List<byte[]>();
            for (int i = 0; i < 600; i++) { a.Tick(); reference.Add(a.E.CurrentSnapshotDigest); }

            MatchEngine c = MatchEngine.RestoreFromSnapshot(header, payload, Seed);
            c.TestOnly_ArmMarkMovement(true);
            var rc = new Rig(c, def) { AttackerCanonical = attacker, LockOwn = false };
            for (int i = 0; i < 600; i++)
            {
                rc.Tick();
                CollectionAssert.AreEqual(reference[i], c.CurrentSnapshotDigest, $"tick {i}: restored armed engine diverged");
            }
            Assert.AreEqual(a.E.TestOnly_MarkTarget(cb).HasTarget, c.TestOnly_MarkTarget(cb).HasTarget);
            Assert.IsTrue(c.TestOnly_MarkTarget(a.MarkingCb()).HasTarget, "the continued run holds a live mark target");

            MatchEngine u = MatchEngine.RestoreFromSnapshot(header, payload, Seed);   // NOT re-armed: config says off
            var ru = new Rig(u, def) { AttackerCanonical = attacker, LockOwn = false };
            bool diverged = false;
            for (int i = 0; i < 600 && !diverged; i++)
            {
                ru.Tick();
                if (!Equal(reference[i], u.CurrentSnapshotDigest)) diverged = true;
            }
            Assert.IsTrue(diverged, "a restored engine left at the config default diverges from the armed run (OD-11 hazard)");
        }

        private static bool Equal(byte[] x, byte[] y)
        {
            if (x.Length != y.Length) return false;
            for (int i = 0; i < x.Length; i++) if (x[i] != y[i]) return false;
            return true;
        }

        // ── 8. two-run determinism, armed, 2 x 10 minutes ────────────────────────

        /// <summary>
        /// One armed run of <paramref name="frames"/> frames: the digest every 600 frames and the per-stride
        /// (HasMarkTarget, X, Y) of all 22 agents. Engines are run ONE AT A TIME: the EventBus is process-static, so
        /// interleaving two engines tick by tick perturbs the digest ledger (that is a harness property, not a
        /// determinism defect; MatchEngineDeterminismTests runs its chains sequentially too).
        /// </summary>
        private static (List<byte[]> Digests, List<float> Targets, int TargetStrides) RunArmed(int frames, bool armed)
        {
            var e = new MatchEngine(Seed);
            e.TestOnly_ArmMarkMovement(armed);
            var digests = new List<byte[]>();
            var targets = new List<float>();
            int targetStrides = 0;
            for (int i = 1; i <= frames; i++)
            {
                e.RunTick();
                if (i % 600 == 0 || i == frames) digests.Add(e.CurrentSnapshotDigest);
                if (i % Stride != 0) continue;
                for (int k = 0; k < 2 * Per; k++)
                {
                    (bool has, Vector2 t) = e.TestOnly_MarkTarget(k);
                    targets.Add(has ? 1.0f : 0.0f); targets.Add(t.x); targets.Add(t.y);
                    if (has)
                    {
                        targetStrides++;
                        // Finite and near the pitch. Not clamped here: the point is anchored to the marked man, who may
                        // stand a hair outside the line; the Decision Tree clamps it (#8 section 3.1.7.2).
                        Assert.IsTrue(float.IsFinite(t.x) && float.IsFinite(t.y), $"frame {i} agent {k}: finite target");
                        Assert.That(t.x, Is.InRange(-1.0f, 106.0f));
                        Assert.That(t.y, Is.InRange(-1.0f, 69.0f));
                    }
                }
            }
            EventBus.ResetForNewMatch();
            return (digests, targets, targetStrides);
        }

        /// <summary>Two armed same-seed runs of 10 minutes (36,000 frames) produce identical digests and identical mark targets; marks are actually composed.</summary>
        [Test]
        public void TwoRuns_Armed_TenMinutes_AreByteIdentical()
        {
            const int Frames = 36000;
            (List<byte[]> dA, List<float> tA, int nA) = RunArmed(Frames, armed: true);
            (List<byte[]> dB, List<float> tB, int nB) = RunArmed(Frames, armed: true);

            Assert.AreEqual(dA.Count, dB.Count);
            for (int i = 0; i < dA.Count; i++) CollectionAssert.AreEqual(dA[i], dB[i], $"digest diverged at checkpoint {i}");
            CollectionAssert.AreEqual(tA, tB, "per-stride mark targets diverged");
            Assert.AreEqual(nA, nB);
            Assert.Greater(nA, 0, "a ten-minute armed match composes mark targets (not vacuous)");
        }

        // ── 9. switch-off identity ───────────────────────────────────────────────

        /// <summary>
        /// Switch off (the shipped default, no arming) writes no mark target over a full five-minute match, and an
        /// explicit Arm(false) is byte-identical to the default throughout; the armed match, by contrast, differs
        /// (so the identity is not vacuous).
        /// </summary>
        [Test]
        public void SwitchOff_NeverWritesATarget_AndExplicitFalseEqualsTheDefault()
        {
            Assert.IsFalse(MatchEngineConstants.MarkMovementEnabled, "the shipped default is off");
            const int Frames = 18000;
            (List<byte[]> plain, List<float> tPlain, int nPlain) = RunArmedOrDefault(Frames, mode: 0);
            (List<byte[]> off, List<float> tOff, int nOff) = RunArmedOrDefault(Frames, mode: 1);
            (List<byte[]> on, _, int nOn) = RunArmedOrDefault(Frames, mode: 2);

            Assert.AreEqual(0, nPlain, "default: no mark target is ever written");
            Assert.AreEqual(0, nOff, "explicitly disarmed: none either");
            for (int i = 0; i < plain.Count; i++) CollectionAssert.AreEqual(plain[i], off[i], $"checkpoint {i}: Arm(false) differs from the default");
            CollectionAssert.AreEqual(tPlain, tOff);
            Assert.Greater(nOn, 0, "armed: targets are written");
            bool differs = false;
            for (int i = 0; i < plain.Count && !differs; i++) differs = !Equal(plain[i], on[(i + 1) * 5 - 1]);   // 3,000-frame checkpoints vs the armed run's 600-frame ones
            Assert.IsTrue(differs, "the armed match differs from the switch-off match, so the identity above is not vacuous");
        }

        /// <summary>mode 0 = engine as shipped (no arming call), 1 = Arm(false), 2 = Arm(true).</summary>
        private static (List<byte[]> Digests, List<float> Targets, int TargetStrides) RunArmedOrDefault(int frames, int mode)
        {
            if (mode == 2) return RunArmed(frames, armed: true);
            var e = new MatchEngine(Seed);
            if (mode == 1) e.TestOnly_ArmMarkMovement(false);
            var digests = new List<byte[]>();
            var targets = new List<float>();
            int n = 0;
            for (int i = 1; i <= frames; i++)
            {
                e.RunTick();
                if (i % 3000 == 0) digests.Add(e.CurrentSnapshotDigest);
                if (i % Stride != 0) continue;
                for (int k = 0; k < 2 * Per; k++)
                {
                    (bool has, Vector2 t) = e.TestOnly_MarkTarget(k);
                    targets.Add(has ? 1.0f : 0.0f);
                    if (has) n++;
                }
            }
            EventBus.ResetForNewMatch();
            return (digests, targets, n);
        }

        /// <summary>
        /// Env-gated identity probe (<c>TD_MARK_IDENTITY_PROBE=1</c>): prints the six-seed snapshot digests at frame
        /// 18,000 (5 minutes) with the engine as shipped (switch off, not armed). Run it at the W13.0 tip and at
        /// the W13.1 tip: the lines must be equal. Uses no W13.1 API, so the body compiles at both tips. When
        /// <c>TD_MARK_IDENTITY_PROBE_OUT</c> names a file the lines are written there as well.
        /// </summary>
        [Test]
        public void IdentityProbe_SixSeedDigestsAtFrame18000()
        {
            if (Environment.GetEnvironmentVariable("TD_MARK_IDENTITY_PROBE") != "1")
                Assert.Ignore("Set TD_MARK_IDENTITY_PROBE=1 to print the six-seed digests (wiring plan section 8).");

            ulong[] seeds =
            {
                0x0123456789ABCDEFUL, 0x1111111111111111UL, 0x2222222222222222UL,
                0x3333333333333333UL, 0xDEADBEEFCAFEBABEUL, 0x0F0F0F0F0F0F0F0FUL,
            };
            var sb = new StringBuilder();
            foreach (ulong seed in seeds)
            {
                var e = new MatchEngine(seed);
                for (int i = 0; i < 18000; i++) e.RunTick();
                sb.Append("digest seed=0x").Append(seed.ToString("X16")).Append(" frame=18000 ")
                  .AppendLine(BitConverter.ToString(e.CurrentSnapshotDigest).Replace("-", string.Empty));
                EventBus.ResetForNewMatch();
            }
            Console.WriteLine(sb.ToString());
            string path = Environment.GetEnvironmentVariable("TD_MARK_IDENTITY_PROBE_OUT");
            if (!string.IsNullOrEmpty(path)) File.WriteAllText(path, sb.ToString());
        }

        // ── TightMarking (#21 section 3.4) ───────────────────────────────────────

        /// <summary>A marker whose #21 TightMarking is set stands 1.0 m, not 2.0 m, from his man.</summary>
        [TestCase(0)]
        [TestCase(1)]
        public void TightMarking_UsesTheTightStandoff(int def)
        {
            var e = new MatchEngine(Seed);
            e.TestOnly_ArmMarkMovement(true);
            var rig = new Rig(e, def) { LockOwn = true };
            rig.Strides(6);
            TacticalInstructions.PlayerInstructions tight = new TacticalInstructions.PlayerInstructions(
                TacticalInstructions.InstrBias.Default, TacticalInstructions.InstrBias.Default, TacticalInstructions.InstrBias.Default,
                TacticalInstructions.InstrBias.Default, TacticalInstructions.InstrBias.Default, TacticalInstructions.InstrBias.Default,
                true, -1, TacticalInstructions.SetPieceDutyFlags.None);
            foreach (int cb in new[] { rig.Cb2, rig.Cb3 })
                e.SetPlayerTactic(cb, new TacticalInstructions.PlayerTactic(TacticalInstructions.PlayerRole.Default, TacticalInstructions.Duty.Support, tight));
            Vector2 o = IntroduceAttacker(rig);
            rig.Strides(DefensiveAIConstants.MarkDwellTicks + 2);

            int cbm = rig.MarkingCb();
            Assert.GreaterOrEqual(cbm, 0);
            Vector2 expected = W(def, new Vector2(o.x - DefensiveAIConstants.MarkStandoffTightM, 34.0f));
            Vector2 target = e.TestOnly_MarkTarget(cbm).Target;
            Assert.AreEqual(expected.x, target.x, 1e-3f);
            Assert.AreEqual(expected.y, target.y, 1e-3f);
        }

        // ── descriptive: INTERCEPT versus MOVE near the mark point ───────────────

        /// <summary>
        /// DESCRIPTIVE, not asserted (wiring plan section 11, risks). A marker holding his mark point while a fast
        /// loose ball is struck across his path: records which option the Decision Tree selected each decision
        /// (MOVE_TO_POSITION vs INTERCEPT) so the interaction can be read, not pinned. Writes the tally to the
        /// test log; the only assertions are that the run completes and the marker stays on the pitch.
        /// </summary>
        [TestCase(0)]
        [TestCase(1)]
        public void Descriptive_InterceptVersusMove_NearTheMarkPoint(int def)
        {
            Rig rig = WarmRig(def);
            IntroduceAttacker(rig);
            rig.Strides(DefensiveAIConstants.MarkDwellTicks + 2);
            int cb = rig.MarkingCb();
            Assert.GreaterOrEqual(cb, 0);
            rig.LockOwn = false;

            var tally = new Dictionary<ActionType, int>();
            EventBus.Subscribe<DecisionMadeEvent>((in DecisionMadeEvent evt) =>
            {
                if (evt.AgentId != cb) return;
                tally.TryGetValue(evt.SelectedAction.Type, out int n);
                tally[evt.SelectedAction.Type] = n + 1;
            });

            Vector2 cbPos = Pos(rig.E, cb);
            for (int s = 0; s < 20; s++)
            {
                // A ball struck at 14 m/s across the marker's path, 3 m in front of him, every 2 s.
                if (s % 4 == 0)
                {
                    Vector2 start = cbPos + W(def, new Vector2(3.0f, -6.0f)) - W(def, Vector2.zero);
                    BallState b = rig.E.TestOnly_BallSnapshot;
                    b.Position = new Vector3(start.x, start.y, 0.11f);
                    Vector2 dir = (W(def, new Vector2(0.0f, 1.0f)) - W(def, Vector2.zero)) * 14.0f;
                    b.Velocity = new Vector3(dir.x, dir.y, 0.0f);
                    rig.E.TestOnly_SetBall(in b);
                }
                rig.E.TestOnly_SetPossession(MatchEngineConstants.NO_POSSESSION);
                for (int t = 0; t < Stride; t++) rig.E.RunTick();
            }

            var line = new StringBuilder("INTERCEPT-vs-MOVE (descriptive) def=" + def + ":");
            foreach (KeyValuePair<ActionType, int> kv in tally) line.Append(' ').Append(kv.Key).Append('=').Append(kv.Value);
            TestContext.Progress.WriteLine(line.ToString());
            Console.WriteLine(line.ToString());

            Vector2 end = Pos(rig.E, cb);
            Assert.That(end.x, Is.InRange(0.0f, 105.0f));
            Assert.That(end.y, Is.InRange(0.0f, 68.0f));
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                                                   |
// | 1.0     | 2026-11-11 | —      | W13.1 / ERR-014-010: acquisition, closure, 20 m boundary, invariants every stride, no-target |
// |         |            |        |   cases, substitution / dismissal, restore continuation (armed, mid-dwell), two-run armed   |
// |         |            |        |   determinism, switch-off identity + env-gated probe, TightMarking, descriptive INTERCEPT.  |
#endregion
