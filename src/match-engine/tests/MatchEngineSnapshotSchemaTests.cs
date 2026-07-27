// File:     src/match-engine/tests/MatchEngineSnapshotSchemaTests.cs
// Created:  2026-06-16
// Modified: 2026-06-27 (Phase D D4 — schema pin 8 + DT + 4 mechanics-AI + perception probes)
// Modified: 2026-07-11 (engine substrate — schema pin 13 → 14 + ScoreState probe)
// Modified: 2026-07-14 (match-flow completion — schema pin 14 → 15 + discipline/substitution probes)
// Modified: 2026-07-18 (#27 T3 — schema pin 15 → 16 + roster-reference probe)
// Modified: 2026-07-19 (#27 lineup selection Plan-3 — NeutralSquad made position-coherent so proper selection succeeds; no schema change)
// Modified: 2026-07-20 (snapshot-deserialize KD-8 — schema pin 16 → 17 + CardSeverityRngCursor probe)
// Author:   —
// Spec:     Match Engine design note (docs/tracking/match-engine-design.md) §2.6 / §5 Phase B (B3) + Phase D (D4), Code Standards #20
// Purpose:  Phase B step B3 tests — proves the full §2.6 world-state field set (not just the B2
//           kinematic subset) feeds the snapshot digest: perturbing the embedded OscillationGuard
//           ring-buffer state (B0 seam) or the ball spin changes the digest, and the schema pin holds.

using System.Collections.Generic;

using NUnit.Framework;
using UnityEngine;

using TacticalDirector.AgentMovement;
using TacticalDirector.BallPhysics;
using TacticalDirector.DecisionTree;

namespace TacticalDirector.MatchEngine
{
    /// <summary>
    /// Phase B step B3 full-field-set snapshot-serialization tests for <see cref="MatchEngine"/>.
    /// Each "feeds the digest" test perturbs a single field that the B2 kinematic subset did NOT
    /// serialize and asserts the post-tick digest changes — the field is now in the digest preimage.
    /// </summary>
    [TestFixture]
    public sealed class MatchEngineSnapshotSchemaTests
    {
        private const ulong MatchSeed = 0x0123456789ABCDEFUL;
        private const int   OutfieldIndex = 1; // roster index 0 is the goalkeeper (skipped by movement)

        // A locked guard state with a finite far-future lock-until and a finite recorded timestamp,
        // distinct from a freshly initialised guard (all -Infinity, unlocked). Used to perturb one
        // agent's OscillationGuard without touching any other field.
        private static OscillationGuardState LockedGuardState()
        {
            return new OscillationGuardState(
                0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f,
                writeIndex: 0, isLocked: true, lockUntilTime: 1000f);
        }

        [Test]
        public void SchemaVersion_IsPinned()
        {
            // The pin must change deliberately (with a field-set or ordering change), never by drift.
            // v6 Defensive, v7 Attacking, v8 Perception, v9 #21 per-team TeamTactic, v10 #21 per-agent
            // PlayerTactic (active + pending), v11 #21 TeamTactic.MarkingOrientation appended,
            // v12 #23/#24/#25 wiring (marking dwell + build-up zone/settled-team + rotation state +
            // the three #21 back-prop dials in WriteTeamTactic), v13 #26 per-team ManagerState
            // (Appendix C order, FR-TP-012), v14 engine score state (per-team goals + the
            // last-holder tracker — the goal-detection substrate), v15 match-flow completion
            // (per-agent yellow-card count + sent-off flag, the global foul cooldown, per-agent
            // active bench slot, per-team substitutions-used count, half-time/full-time flags),
            // v16 #27 T3 per-team roster reference (the loaded Squad.ClubId or NO_ROSTER_CLUB_ID),
            // v17 snapshot-deserialize KD-8 (the match-flow.card-severity RNG stream cursor — RngCursor +
            // ActionOrdinal, the engine's only mutable RNG stream), v18 GK/Heading engine-integration Phase 2
            // (the two subsystem RNG-stream cursors + the two §4 trigger latches + both orchestrators'
            // in-flight state via their CaptureState seams — making a flag-on engine snapshot-safe).
            Assert.AreEqual(19u, MatchEngineConstants.SNAPSHOT_SCHEMA_VERSION,
                "SNAPSHOT_SCHEMA_VERSION drifted — bump it intentionally only with a field-set/order change.");
        }

        [Test]
        public void GkHeadingState_FeedsSnapshotDigest()
        {
            // v18 (GK/Heading Phase 2): the goalkeeper.mechanics RNG stream cursor — a field in the v18
            // GK/Heading block — reaches the digest preimage. The block is written UNCONDITIONALLY (the flag
            // need not be on), so a clean single-field probe: perturbing the cursor alone must move the digest.
            var baseline = new MatchEngine(MatchSeed);
            baseline.RunTick();

            var perturbed = new MatchEngine(MatchSeed);
            perturbed.TestOnly_SetGoalkeeperStreamCursor(rngCursor: 5, actionOrdinal: 4);
            perturbed.RunTick();

            CollectionAssert.AreNotEqual(
                baseline.CurrentSnapshotDigest, perturbed.CurrentSnapshotDigest,
                "Advancing the goalkeeper.mechanics RNG stream cursor left the digest unchanged — the v18 " +
                "GK/Heading block is not in the digest preimage (round-trip determinism would silently break).");
        }

        [Test]
        public void CardSeverityRngCursor_FeedsSnapshotDigest()
        {
            // v17 (snapshot-deserialize KD-8): the match-flow.card-severity RNG stream cursor reaches the
            // digest preimage. The first processed tick captures no foul (no card-severity draw), so the
            // injected cursor passes through to the snapshot unchanged — a clean single-field probe. Without
            // this in the digest a save taken after a booking would restore the stream at cursor 0 and the
            // next card draw would diverge, silently breaking round-trip determinism (the writer half of KD-8).
            var baseline = new MatchEngine(MatchSeed);
            baseline.RunTick();

            var perturbed = new MatchEngine(MatchSeed);
            perturbed.TestOnly_SetCardSeverityStreamCursor(rngCursor: 3, actionOrdinal: 2);
            perturbed.RunTick();

            CollectionAssert.AreNotEqual(
                baseline.CurrentSnapshotDigest, perturbed.CurrentSnapshotDigest,
                "Advancing the card-severity RNG stream cursor left the digest unchanged — the v17 RNG " +
                "stream state is not in the digest preimage (round-trip determinism would silently break).");
        }

        [Test]
        public void MatchFlowCompletionState_FeedsSnapshotDigest()
        {
            // v15: a sent-off flag with no other change reaches the digest preimage.
            var baseline = new MatchEngine(MatchSeed);
            baseline.RunTick();

            var perturbed = new MatchEngine(MatchSeed);
            perturbed.TestOnly_SetIsSentOff(OutfieldIndex, true);
            perturbed.RunTick();

            CollectionAssert.AreNotEqual(
                baseline.CurrentSnapshotDigest, perturbed.CurrentSnapshotDigest,
                "A sent-off flag left the digest unchanged — the v15 discipline block must feed the preimage.");
        }

        [Test]
        public void SubstitutionState_FeedsSnapshotDigest()
        {
            // v15: a substitution (per-agent active bench slot + per-team substitutions-used) reaches
            // the digest preimage even though the bench identity defaults are neutral (no attribute
            // difference from the starter) — the bookkeeping fields alone must move the digest.
            var baseline = new MatchEngine(MatchSeed);
            baseline.RunTick();

            var perturbed = new MatchEngine(MatchSeed);
            perturbed.SubstitutePlayer(0, OutfieldIndex, benchIndex: 0, SubstitutionReason.Tactical);
            perturbed.RunTick();

            CollectionAssert.AreNotEqual(
                baseline.CurrentSnapshotDigest, perturbed.CurrentSnapshotDigest,
                "A substitution left the digest unchanged — the v15 substitution block must feed the preimage.");
        }

        /// <summary>An all-<c>CreateDefault</c> (neutral-attribute), position-coherent squad of exactly
        /// the consumed size (starters + bench). Positions are ordered [GK, Def×4, Mid×4, Fwd×2, bench]
        /// so proper lineup selection (#27 Plan-3) succeeds AND reproduces roster order (KD-L5) — the
        /// agents still move identically to the unconfigured baseline, so this squad isolates the roster
        /// reference in the digest. Fully-qualifies the PlayerDatabase types (this file's
        /// <c>using TacticalDirector.AgentMovement</c> also imports a <c>PlayerAttributes</c> — the KD-P6
        /// CS0104 discipline).</summary>
        private static TacticalDirector.PlayerDatabase.Squad NeutralSquad(int clubId)
        {
            int count = MatchEngineConstants.PLAYERS_PER_TEAM + MatchEngineConstants.SUBSTITUTES_PER_TEAM;
            var players = new TacticalDirector.PlayerDatabase.PlayerRecord[count];
            for (int k = 0; k < count; k++)
            {
                var p = TacticalDirector.PlayerDatabase.PlayerRecord.CreateDefault(
                    clubId * TacticalDirector.PlayerDatabase.PlayerDatabaseConstants.CLUB_SQUAD_SIZE + k);
                p.Position = CoherentPosition(k);
                players[k] = p;
            }
            return new TacticalDirector.PlayerDatabase.Squad(clubId, players);
        }

        /// <summary>Coarse position for the [GK, Def×4, Mid×4, Fwd×2, bench] coherent layout (KD-L5).</summary>
        private static TacticalDirector.PlayerDatabase.PlayerPosition CoherentPosition(int localIndex)
        {
            if (localIndex == 0)  return TacticalDirector.PlayerDatabase.PlayerPosition.Goalkeeper;
            if (localIndex <= 4)  return TacticalDirector.PlayerDatabase.PlayerPosition.Defender;
            if (localIndex <= 8)  return TacticalDirector.PlayerDatabase.PlayerPosition.Midfielder;
            if (localIndex <= 10) return TacticalDirector.PlayerDatabase.PlayerPosition.Forward;
            switch ((localIndex - 11) % 3)   // bench filler: Def / Mid / Fwd, no GK
            {
                case 0:  return TacticalDirector.PlayerDatabase.PlayerPosition.Defender;
                case 1:  return TacticalDirector.PlayerDatabase.PlayerPosition.Midfielder;
                default: return TacticalDirector.PlayerDatabase.PlayerPosition.Forward;
            }
        }

        [Test]
        public void RosterReference_FeedsSnapshotDigest()
        {
            // v16 (#27 T3): the per-team roster reference reaches the digest preimage. The configured
            // squad is all-CreateDefault (neutral attributes), so agents move identically to the
            // unconfigured baseline — the ONLY difference is the roster reference (each team's ClubId),
            // present at the very first tick, before any behavioural divergence could exist. A restored
            // save must know which squad was loaded, so the reference must move the digest (KD-T3-2).
            var baseline = new MatchEngine(MatchSeed);
            baseline.RunTick();

            var perturbed = new MatchEngine(MatchSeed);
            perturbed.ConfigureSquads(NeutralSquad(7), NeutralSquad(8));
            perturbed.RunTick();

            CollectionAssert.AreNotEqual(
                baseline.CurrentSnapshotDigest, perturbed.CurrentSnapshotDigest,
                "A configured (all-neutral) squad left the digest unchanged — the v16 roster reference " +
                "is not in the digest preimage.");
        }

        [Test]
        public void ScoreState_FeedsSnapshotDigest()
        {
            // v14: the per-team goal counts reach the digest preimage. The scripted score is the
            // ONLY difference from the baseline run (no tactic, no manager, no ball change) — the
            // digest must move, or a restored save could silently resume with the wrong score.
            var baseline = new MatchEngine(MatchSeed);
            baseline.RunTick();

            var perturbed = new MatchEngine(MatchSeed);
            perturbed.TestOnly_SetGoals(homeGoals: 1, awayGoals: 0);
            perturbed.RunTick();

            CollectionAssert.AreNotEqual(
                baseline.CurrentSnapshotDigest, perturbed.CurrentSnapshotDigest,
                "A non-level score left the digest unchanged — the v14 score block must feed the preimage.");
        }

        [Test]
        public void ManagerState_FeedsSnapshotDigest()
        {
            // #26 FR-TP-012 / T-TP-I-005: the per-team ManagerState reaches the digest preimage.
            // A Pragmatic AI manager's kickoff selection is Balanced (Appendix B.1), so the APPLIED
            // TACTIC is the identity — the only difference from the baseline run is the v13 manager
            // block itself (Mode/ProfileOrdinal/preset seed/decision bookkeeping). The digest must move.
            var baseline = new MatchEngine(MatchSeed);
            baseline.RunTick();

            var perturbed = new MatchEngine(MatchSeed);
            perturbed.ConfigureManager(
                0, ManagerMode.AI, TacticalDirector.TacticalInstructions.TacticalPresetsConstants.ARCHETYPE_PRAGMATIC);
            ManagerAdaptation.ApplyKickoff(
                perturbed, new TacticalDirector.TacticalInstructions.InCodeTacticPresetCatalogue());
            perturbed.RunTick();

            CollectionAssert.AreNotEqual(
                baseline.CurrentSnapshotDigest, perturbed.CurrentSnapshotDigest,
                "An AI-managed team with the identity (Balanced) kickoff selection left the digest " +
                "unchanged — the v13 ManagerState block (#26 Appendix C) must feed the preimage.");
        }

        [Test]
        public void DismarkBuildUpRotationDials_FeedSnapshotDigest()
        {
            // Baseline: untouched kickoff state (both teams' tactic at the Balanced boot seed —
            // Off / None / Off for the three #23/#24/#25 dials).
            var baseline = new MatchEngine(MatchSeed);
            baseline.RunTick();

            // Perturbed: ONLY the three back-prop dials differ from Balanced (named args over the
            // Balanced identity positional prefix) — isolates the v12 WriteTeamTactic appends.
            var perturbed = new MatchEngine(MatchSeed);
            perturbed.SetTeamTactic(0, new TacticalDirector.TacticalInstructions.TeamTactic(
                TacticalDirector.TacticalInstructions.Mentality.Balanced,
                TacticalDirector.TacticalInstructions.TacticFormation.F442,
                TacticalDirector.TacticalInstructions.Tempo.Standard,
                TacticalDirector.TacticalInstructions.TacticWidth.Standard,
                TacticalDirector.TacticalInstructions.TacticPassing.Mixed,
                TacticalDirector.TacticalInstructions.TacticPressing.Medium,
                TacticalDirector.TacticalInstructions.LineOfEngagement.Standard,
                0.5f,
                TacticalDirector.TacticalInstructions.TacticDefWidth.Standard,
                TacticalDirector.TacticalInstructions.TransitionPlan.HoldShape,
                TacticalDirector.TacticalInstructions.TransitionPlan.Regroup,
                false,
                TacticalDirector.TacticalInstructions.TacticTriggerMask.None,
                TacticalDirector.TacticalInstructions.FocusPlay.Mixed,
                TacticalDirector.TacticalInstructions.GkDistributionPolicy.SlowDown,
                0,
                TacticalDirector.TacticalInstructions.MarkingOrientation.Balanced,
                dismarkIntensity: TacticalDirector.TacticalInstructions.DismarkIntensity.Aggressive,
                buildUpStructure: TacticalDirector.TacticalInstructions.BuildUpStructure.BackThree,
                rotationFreedom:  TacticalDirector.TacticalInstructions.RotationFreedom.Free));
            perturbed.RunTick();

            CollectionAssert.AreNotEqual(
                baseline.CurrentSnapshotDigest, perturbed.CurrentSnapshotDigest,
                "Staging non-identity #23/#24/#25 dials left the digest unchanged — the v12 WriteTeamTactic " +
                "appends are not in the digest preimage.");
        }

        [Test]
        public void BuildUpSettledTeamAndSuppression_FeedSnapshotDigest()
        {
            // Baseline: kickoff, ball loose the whole tick (settledTeam stays −1).
            var baseline = new MatchEngine(MatchSeed);
            baseline.RunTick();

            // Perturbed: an away agent takes possession before the tick — the possession-changed
            // consumer records settledTeam = 1 (serialized at v12), and MatchContext.PossessingAgentId
            // differs too. The digest must move (locks the settled-team tracker into the preimage
            // alongside the possession fields).
            var perturbed = new MatchEngine(MatchSeed);
            perturbed.TestOnly_SetPossession(MatchEngineConstants.PLAYERS_PER_TEAM);
            perturbed.RunTick();

            CollectionAssert.AreNotEqual(
                baseline.CurrentSnapshotDigest, perturbed.CurrentSnapshotDigest,
                "A settled possession change left the digest unchanged — the v12 build-up settled-team " +
                "tracker (and MatchContext possession) must feed the preimage.");
        }

        [Test]
        public void MarkingOrientation_FeedsSnapshotDigest()
        {
            // Baseline: untouched kickoff state (both teams' tactic at the Balanced boot seed, including
            // the appended MarkingOrientation field).
            var baseline = new MatchEngine(MatchSeed);
            baseline.RunTick();

            // Perturbed: only MarkingOrientation differs from Balanced (every other field stays identity)
            // — isolates the v11 field addition from the v9 TeamTactic test above.
            var perturbed = new MatchEngine(MatchSeed);
            perturbed.SetTeamTactic(0, new TacticalDirector.TacticalInstructions.TeamTactic(
                TacticalDirector.TacticalInstructions.Mentality.Balanced,
                TacticalDirector.TacticalInstructions.TacticFormation.F442,
                TacticalDirector.TacticalInstructions.Tempo.Standard,
                TacticalDirector.TacticalInstructions.TacticWidth.Standard,
                TacticalDirector.TacticalInstructions.TacticPassing.Mixed,
                TacticalDirector.TacticalInstructions.TacticPressing.Medium,
                TacticalDirector.TacticalInstructions.LineOfEngagement.Standard,
                0.5f,
                TacticalDirector.TacticalInstructions.TacticDefWidth.Standard,
                TacticalDirector.TacticalInstructions.TransitionPlan.HoldShape,
                TacticalDirector.TacticalInstructions.TransitionPlan.Regroup,
                false,
                TacticalDirector.TacticalInstructions.TacticTriggerMask.None,
                TacticalDirector.TacticalInstructions.FocusPlay.Mixed,
                TacticalDirector.TacticalInstructions.GkDistributionPolicy.SlowDown,
                0,
                TacticalDirector.TacticalInstructions.MarkingOrientation.ManOriented));
            perturbed.RunTick();

            CollectionAssert.AreNotEqual(
                baseline.CurrentSnapshotDigest, perturbed.CurrentSnapshotDigest,
                "Staging a non-Balanced MarkingOrientation left the digest unchanged — the v11 field is not " +
                "in the digest preimage.");
        }

        [Test]
        public void TeamTactic_FeedsSnapshotDigest()
        {
            // Baseline: untouched kickoff state (both teams' tactic at the Balanced boot seed).
            var baseline = new MatchEngine(MatchSeed);
            baseline.RunTick();

            // Perturbed: a non-Balanced tactic staged for one team. The first processed tick is not an AI
            // stride tick, so RunAiPhase does not commit pending → active — but the snapshot serializes the
            // PENDING tactic too, so the staged change reaches the digest preimage immediately (v9). This is
            // exactly what makes a mid-match SetTeamTactic restore-deterministic (ERR-021-002).
            var perturbed = new MatchEngine(MatchSeed);
            perturbed.SetTeamTactic(0, new TacticalDirector.TacticalInstructions.TeamTactic(
                TacticalDirector.TacticalInstructions.Mentality.VeryAttacking,
                TacticalDirector.TacticalInstructions.TacticFormation.F442,
                TacticalDirector.TacticalInstructions.Tempo.VeryFast,
                TacticalDirector.TacticalInstructions.TacticWidth.VeryWide,
                TacticalDirector.TacticalInstructions.TacticPassing.Direct,
                TacticalDirector.TacticalInstructions.TacticPressing.High,
                TacticalDirector.TacticalInstructions.LineOfEngagement.VeryHigh,
                0.9f,
                TacticalDirector.TacticalInstructions.TacticDefWidth.Wide,
                TacticalDirector.TacticalInstructions.TransitionPlan.HoldShape,
                TacticalDirector.TacticalInstructions.TransitionPlan.Regroup,
                true,
                TacticalDirector.TacticalInstructions.TacticTriggerMask.None,
                TacticalDirector.TacticalInstructions.FocusPlay.LeftFlank,
                TacticalDirector.TacticalInstructions.GkDistributionPolicy.SlowDown,
                3));
            perturbed.RunTick();

            CollectionAssert.AreNotEqual(
                baseline.CurrentSnapshotDigest, perturbed.CurrentSnapshotDigest,
                "Staging a non-Balanced TeamTactic left the digest unchanged — the per-team tactic is not " +
                "in the digest preimage (v9 / ERR-021-002 regression).");
        }

        [Test]
        public void PlayerTactic_FeedsSnapshotDigest()
        {
            // Baseline: untouched kickoff state (every agent's per-agent tactic at the identity boot seed).
            var baseline = new MatchEngine(MatchSeed);
            baseline.RunTick();

            // Perturbed: a non-identity per-agent tactic staged for one agent. The first processed tick is
            // not an AI stride tick, so RunAiPhase does not commit pending → active — but the snapshot
            // serializes the PENDING per-agent tactic too (v10), so the staged change reaches the digest
            // preimage immediately. This is what makes a mid-match SetPlayerTactic restore-deterministic.
            var perturbed = new MatchEngine(MatchSeed);
            perturbed.SetPlayerTactic(OutfieldIndex, new TacticalDirector.TacticalInstructions.PlayerTactic(
                TacticalDirector.TacticalInstructions.PlayerRole.Poacher,
                TacticalDirector.TacticalInstructions.Duty.Attack,
                TacticalDirector.TacticalInstructions.PlayerInstructions.Default));
            perturbed.RunTick();

            CollectionAssert.AreNotEqual(
                baseline.CurrentSnapshotDigest, perturbed.CurrentSnapshotDigest,
                "Staging a non-identity PlayerTactic left the digest unchanged — the per-agent tactic is " +
                "not in the digest preimage (v10).");
        }

        [Test]
        public void PerceptionState_FeedsSnapshotDigest()
        {
            // Baseline: untouched kickoff state (perception seeded at boot).
            var baseline = new MatchEngine(MatchSeed);
            baseline.RunTick();

            // Perturbed: bump a recognition-latency counter. The first processed tick is not an AI stride
            // tick, so the perception pipeline does not run and the injected counter passes through to the
            // snapshot unchanged — a clean single-field probe.
            var perturbed = new MatchEngine(MatchSeed);
            perturbed.TestOnly_PerceptionState().Latency.LatencyCounters[0] += 1;
            perturbed.RunTick();

            CollectionAssert.AreNotEqual(
                baseline.CurrentSnapshotDigest, perturbed.CurrentSnapshotDigest,
                "Perturbing the Perception recognition-latency state left the digest unchanged — " +
                "the perception cross-tick state is not in the digest preimage (D4 regression).");
        }

        [Test]
        public void DefensiveState_FeedsSnapshotDigest()
        {
            // Baseline: untouched kickoff state (both teams' defensive AI seeded at boot).
            var baseline = new MatchEngine(MatchSeed);
            baseline.RunTick();

            // Perturbed: bump one team's per-agent mark-hysteresis dwell. The first processed tick is not
            // an AI stride tick, so RunMechanicsAI does not run and the injected counter passes through to
            // the snapshot unchanged — a clean single-field probe.
            var perturbed = new MatchEngine(MatchSeed);
            perturbed.TestOnly_DefensiveState(0).Hysteresis[0].DwellCounter += 1;
            perturbed.RunTick();

            CollectionAssert.AreNotEqual(
                baseline.CurrentSnapshotDigest, perturbed.CurrentSnapshotDigest,
                "Perturbing the Defensive AI hysteresis left the digest unchanged — " +
                "the per-team defensive state is not in the digest preimage (D4 regression).");
        }

        [Test]
        public void AttackingState_FeedsSnapshotDigest()
        {
            // Baseline: untouched kickoff state (both teams' attacking AI seeded at boot).
            var baseline = new MatchEngine(MatchSeed);
            baseline.RunTick();

            // Perturbed: bump one team's per-agent role-hysteresis dwell. The first processed tick is not
            // an AI stride tick, so the injected counter passes through to the snapshot unchanged.
            var perturbed = new MatchEngine(MatchSeed);
            perturbed.TestOnly_AttackingState(0).Hysteresis[0].DwellCounter += 1;
            perturbed.RunTick();

            CollectionAssert.AreNotEqual(
                baseline.CurrentSnapshotDigest, perturbed.CurrentSnapshotDigest,
                "Perturbing the Attacking AI hysteresis left the digest unchanged — " +
                "the per-team attacking state is not in the digest preimage (D4 regression).");
        }

        [Test]
        public void PressingState_FeedsSnapshotDigest()
        {
            // Baseline: untouched kickoff state (both teams' pressing seeded at boot, all counters 0).
            var baseline = new MatchEngine(MatchSeed);
            baseline.RunTick();

            // Perturbed: bump one team's pressing cooldown dwell via the role-hysteresis ledger. The first
            // processed tick is not an AI stride tick, so RunMechanicsAI does not run and the injected
            // counter passes through to the snapshot unchanged — a clean single-field probe.
            var perturbed = new MatchEngine(MatchSeed);
            perturbed.TestOnly_PressingState(0).Roles.RoleDwell[0] += 1;
            perturbed.RunTick();

            CollectionAssert.AreNotEqual(
                baseline.CurrentSnapshotDigest, perturbed.CurrentSnapshotDigest,
                "Perturbing the Pressing AI hysteresis left the digest unchanged — " +
                "the per-team pressing state is not in the digest preimage (D4 regression).");
        }

        [Test]
        public void PositioningHysteresis_FeedsSnapshotDigest()
        {
            // Baseline: untouched kickoff state (both teams' positioning seeded at boot, dwell = 0).
            var baseline = new MatchEngine(MatchSeed);
            baseline.RunTick();

            // Perturbed: bump one team's positioning phase-dwell counter. The first processed tick is not
            // an AI stride tick, so RunPositioningAI does not run and the injected dwell passes through to
            // the snapshot unchanged — a clean single-field probe (parallel to the probes above).
            var perturbed = new MatchEngine(MatchSeed);
            perturbed.TestOnly_PositioningState(0).PhaseDwellCount += 1;
            perturbed.RunTick();

            CollectionAssert.AreNotEqual(
                baseline.CurrentSnapshotDigest, perturbed.CurrentSnapshotDigest,
                "Perturbing the Positioning AI hysteresis left the digest unchanged — " +
                "the per-team positioning state is not in the digest preimage (D4 regression).");
        }

        [Test]
        public void DecisionTreeState_FeedsSnapshotDigest()
        {
            // Baseline: untouched kickoff state (every DecisionTree at the fresh IDLE default).
            var baseline = new MatchEngine(MatchSeed);
            baseline.RunTick();

            // Perturbed: one agent's DecisionTree restored to an EXECUTING state with a dispatched
            // action. The first processed tick is not an AI stride tick, so the DecisionTree is not
            // re-evaluated this tick and the injected state passes through to the snapshot unchanged —
            // a clean single-field probe (parallel to the OscillationGuard probe above).
            //
            // The injected action must be a CONTINUOUS one. §5.Z Phase H added the orchestrator's
            // PASS/SHOOT completion sweep (ERR-008-015), which releases a tree parked in EXECUTING on a
            // pass/shot whose executor is idle — and `default(AgentAction).Type` is PASS (ordinal 0), so
            // the old probe erased its own perturbation during the very tick it was measuring, leaving
            // the two digests equal and the probe silently vacuous.
            var perturbed = new MatchEngine(MatchSeed);
            var injected = new DecisionTreeState(
                state: (int)DtState.EXECUTING,
                lastAction: new AgentAction(
                    agentId: OutfieldIndex, type: ActionType.MOVE_TO_POSITION, targetAgentId: -1,
                    targetPosition: Vector2.zero, passParams: default, shotParams: default,
                    utilityScore: 0f, heartbeatTick: 0),
                hasDispatchedAction: true);
            perturbed.TestOnly_SetDecisionTreeState(OutfieldIndex, injected);
            perturbed.RunTick();

            CollectionAssert.AreNotEqual(
                baseline.CurrentSnapshotDigest, perturbed.CurrentSnapshotDigest,
                "Perturbing the DecisionTree state machine left the digest unchanged — " +
                "the D0 decision state is not in the digest preimage (D4 regression).");
        }

        [Test]
        public void GuardState_FeedsSnapshotDigest()
        {
            // Baseline: untouched kickoff state (every guard freshly initialised, unlocked).
            var baseline = new MatchEngine(MatchSeed);
            baseline.RunTick();

            // Perturbed: identical kickoff state except one outfielder's OscillationGuard is locked.
            // The agent holds a Stop command at rest, so the state machine takes no transition this
            // tick and the guard passes through to the snapshot unchanged — a clean single-field probe.
            var perturbed = new MatchEngine(MatchSeed);
            AgentState a = perturbed.TestOnly_AgentSnapshot(OutfieldIndex);
            a.OscillationGuard.RestoreState(LockedGuardState());
            perturbed.TestOnly_SetAgent(OutfieldIndex, a);
            perturbed.RunTick();

            CollectionAssert.AreNotEqual(
                baseline.CurrentSnapshotDigest, perturbed.CurrentSnapshotDigest,
                "Perturbing the OscillationGuard ring-buffer state left the digest unchanged — " +
                "the B0 guard state is not in the digest preimage (B3 regression).");
        }

        [Test]
        public void BallSpin_FeedsSnapshotDigest()
        {
            // Baseline: stationary ball at the centre spot, zero spin.
            var baseline = new MatchEngine(MatchSeed);
            baseline.RunTick();

            // Perturbed: a STATIONARY ball with non-zero spin. The Stationary branch of
            // BallPhysicsCore returns early without clearing AngularVelocity, so the spin survives the
            // tick and reaches the snapshot — proving the expanded ball field set is serialized.
            var perturbed = new MatchEngine(MatchSeed);
            BallState spun = BallState.CreateAtPosition(new Vector3(
                MatchEngineConstants.KickoffBallXM,
                MatchEngineConstants.KickoffBallYM,
                MatchEngineConstants.BALL_REST_HEIGHT_M));
            spun.AngularVelocity = new Vector3(0f, 0f, 5f);
            perturbed.TestOnly_SetBall(spun);
            perturbed.RunTick();

            Assert.AreEqual(BallStateType.Stationary, perturbed.TestOnly_BallSnapshot.State,
                "Probe precondition: the ball must remain Stationary so spin is not physics-driven.");
            Assert.AreEqual(5f, perturbed.TestOnly_BallSnapshot.AngularVelocity.z, 1e-6f,
                "Probe precondition: Stationary physics must preserve the injected spin.");

            CollectionAssert.AreNotEqual(
                baseline.CurrentSnapshotDigest, perturbed.CurrentSnapshotDigest,
                "Perturbing ball AngularVelocity left the digest unchanged — the ball spin field is " +
                "not serialized (B2 serialized position only).");
        }

        [Test]
        public void TwoSameSeedRuns_WithLockedGuard_ProduceIdenticalDigestChains()
        {
            // The guard serialization must itself be deterministic: two same-seed runs that inject the
            // same locked guard must agree tick-for-tick.
            List<byte[]> chainA = RunWithLockedGuard();
            List<byte[]> chainB = RunWithLockedGuard();

            Assert.AreEqual(chainA.Count, chainB.Count);
            for (int i = 0; i < chainA.Count; i++)
            {
                CollectionAssert.AreEqual(
                    chainA[i], chainB[i],
                    $"Digest chain diverged at tick {i + 1} with a serialized locked OscillationGuard.");
            }
        }

        private static List<byte[]> RunWithLockedGuard()
        {
            const int ticks = 30;
            var engine = new MatchEngine(MatchSeed);
            AgentState a = engine.TestOnly_AgentSnapshot(OutfieldIndex);
            a.OscillationGuard.RestoreState(LockedGuardState());
            engine.TestOnly_SetAgent(OutfieldIndex, a);

            var chain = new List<byte[]>(ticks);
            for (int i = 0; i < ticks; i++)
            {
                engine.RunTick();
                chain.Add(engine.CurrentSnapshotDigest);
            }
            return chain;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-06-16 | —      | Initial Phase B step B3 tests: schema-version pin, OscillationGuard |
// |         |            |        | + ball-spin digest-preimage probes, and locked-guard determinism.  |
// | 1.1     | 2026-06-27 | —      | Phase D D4: schema pin 2 → 3; new DecisionTreeState_FeedsSnapshot-  |
// |         |            |        | Digest probe (D0 decision state reaches the digest preimage).       |
// | 1.2     | 2026-06-27 | —      | Phase D D4 (cont.): schema pin 3 → 4; new PositioningHysteresis_    |
// |         |            |        | FeedsSnapshotDigest probe (per-team #12 hysteresis in the preimage).|
// | 1.3     | 2026-06-27 | —      | Phase D D4 (cont.): schema pin 4 → 5; new PressingState_FeedsSnap-  |
// |         |            |        | shotDigest probe (per-team #13 cross-tick state in the preimage).   |
// | 1.4     | 2026-06-27 | —      | Phase D D4 (cont.): schema pin 5 → 7; new DefensiveState_ +         |
// |         |            |        | AttackingState_FeedsSnapshotDigest probes (#14 / #15 cross-tick).   |
// | 1.5     | 2026-06-27 | —      | Phase D D4 (final): schema pin 7 → 8; new PerceptionState_FeedsSnap-|
// |         |            |        | shotDigest probe (#7 recognition-latency cross-tick state).         |
// | 1.6     | 2026-06-29 | —      | #21 / ERR-021-002: schema pin 8 → 9; new TeamTactic_FeedsSnapshot-  |
// |         |            |        | Digest probe (per-team active/pending manager tactic in the         |
// |         |            |        | preimage — a mid-match change is restore-deterministic).            |
// | 1.7     | 2026-06-30 | —      | #21 §3.3: schema pin 9 → 10; new PlayerTactic_FeedsSnapshotDigest   |
// |         |            |        | probe (per-agent active/pending tactic in the preimage).            |
// | 1.8     | 2026-07-07 | —      | Cheap-item addition: schema pin 10 → 11; new MarkingOrientation_    |
// |         |            |        | FeedsSnapshotDigest probe (appended TeamTactic field in preimage).  |
// | 1.9     | 2026-07-11 | —      | #23/#24/#25 wiring: schema pin 11 → 12; new DismarkBuildUpRotation- |
// |         |            |        | Dials_FeedSnapshotDigest (the three WriteTeamTactic appends) +      |
// |         |            |        | BuildUpSettledTeamAndSuppression_FeedSnapshotDigest (settled-team   |
// |         |            |        | tracker) probes.                                                    |
// | 1.10    | 2026-07-11 | —      | #26 manager-AI wiring: schema pin 12 → 13; new ManagerState_        |
// |         |            |        | FeedsSnapshotDigest probe (v13 Appendix C block in the preimage,    |
// |         |            |        | isolated via the Pragmatic → Balanced identity kickoff selection).  |
// | 1.11    | 2026-07-11 | —      | Engine substrate: schema pin 13 → 14; new ScoreState_FeedsSnapshot- |
// |         |            |        | Digest probe (per-team goals + last-holder tracker in the preimage).|
// | 1.12    | 2026-07-14 | —      | Match-flow completion: schema pin 14 → 15; new MatchFlowCompletion- |
// |         |            |        | State_FeedsSnapshotDigest (sent-off flag) + SubstitutionState_      |
// |         |            |        | FeedsSnapshotDigest (bench-slot/count bookkeeping) probes.          |
// | 1.13    | 2026-07-18 | —      | #27 T3: schema pin 15 → 16; new RosterReference_FeedsSnapshotDigest |
// |         |            |        | probe — a configured all-neutral squad (behaviour identical to the |
// |         |            |        | unconfigured baseline) moves the digest, isolating the v16 per-team |
// |         |            |        | roster reference (KD-T3-2).                                         |
// | 1.14    | 2026-07-20 | —      | Snapshot-deserialize KD-8 (writer half): schema pin 16 → 17; new   |
// |         |            |        | CardSeverityRngCursor_FeedsSnapshotDigest probe — advancing the    |
// |         |            |        | match-flow.card-severity RNG stream cursor (the engine's only      |
// |         |            |        | mutable RNG stream) moves the digest, so a save after a booking    |
// |         |            |        | round-trips deterministically.                                     |
// | 1.15    | 2026-07-23 | —      | Pin 17 → 18 (GK/Heading Phase 2) + GkHeadingState_FeedsSnapshot-   |
// |         |            |        | Digest probe (the goalkeeper.mechanics RNG cursor — a v18 field —  |
// |         |            |        | moves the digest, written unconditionally so the flag need not be  |
// |         |            |        | on).                                                               |
#endregion
