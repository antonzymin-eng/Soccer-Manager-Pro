// File:     src/season-save/tests/EngineScoringDiagnosticTests.cs
// Created:  2026-07-26
// Modified: 2026-07-26
// Author:   —
// Spec:     League Bootstrap design supplement KD-8 Step 0; path-to-playable roadmap A4a + risk row 2;
//           Season & Competition Loop #30 §3.4.1; Code Standards #20
// Purpose:  Characterise ERR-030-014 — the KD-8 Step 0 pilot found all 20 engine matches finishing 0–0 at
//           a measured rating differential of ±6, so the calibration corpus carries no signal at all. This
//           driver reports what a single 90-minute match actually DOES, so the finding can be filed with
//           evidence rather than a bare "no goals".
//
// Env-gated for the same reason as the corpus driver: one run is a full ~90 s (Release) to ~3.5 min (Debug)
// match. It is a diagnostic, not a lock — nothing here asserts engine behaviour, because pinning the
// current (degenerate) behaviour would turn a defect into a contract.
//
//   TD_ENGINE_DIAGNOSTIC=1 dotnet test -c Release --filter EngineScoringDiagnostic

using System;
using System.Globalization;

using NUnit.Framework;

using TacticalDirector.PlayerDatabase;

using UnityEngine;

namespace TacticalDirector.SeasonSave.Tests
{
    [TestFixture]
    internal class EngineScoringDiagnosticTests
    {
        /// <summary>Pitch length (Ball Physics #1 §1.2: corner origin, X 0–105 m).</summary>
        private const float PitchLengthM = 105.0f;

        /// <summary>
        /// Ticks to run per variant — ~16 minutes of match time rather than the full 324 000. The
        /// observable of interest is whether the ball ever acquires velocity at all; if nothing is kicked in
        /// sixteen minutes of play the finding is established at a fifth of the cost.
        /// </summary>
        private const int TickBudget = 60000;

        [Test]
        [Category("Calibration")]
        public void EngineScoringDiagnostic_ReportsWhatAMatchActuallyDoes()
        {
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TD_ENGINE_DIAGNOSTIC")))
            {
                Assert.Ignore(
                    "Set TD_ENGINE_DIAGNOSTIC=1 to run the ERR-030-014 characterisation (one full match).");
            }

            // Two variants, so the finding can distinguish "ConfigureSquads breaks play" from "the engine
            // never kicks at all": a distinct-squad match (the season loop's actual configuration) and a
            // NEUTRAL match with no ConfigureSquads call (the configuration every existing match-engine test
            // and the kickoff capstone use).
            Report("distinct-squads", configureSquads: true);
            Report("neutral-unconfigured", configureSquads: false);

            Assert.Pass("Diagnostic only — see the run output.");
        }

        private static void Report(string label, bool configureSquads)
        {
            var engine = new TacticalDirector.MatchEngine.MatchEngine(0xD1A6D05EUL);

            if (configureSquads)
            {
                Squad[] rosters = RoundResolutionCalibrationHarness.BuildBaseRosters();
                engine.ConfigureSquads(
                    LeagueBootstrap.ApplyStrength(rosters[0], +3),
                    LeagueBootstrap.ApplyStrength(rosters[1], -3));
            }

            int ticks = 0;
            int ownThirdTicks = 0;
            int middleThirdTicks = 0;
            int finalThirdTicks = 0;
            int possessedTicks = 0;
            int possessionChanges = 0;
            int lastPossessor = -2;
            int airborneTicks = 0;
            float maxBallX = float.NegativeInfinity;
            float minBallX = float.PositiveInfinity;
            float maxBallZ = float.NegativeInfinity;
            float maxBallSpeed = 0f;

            while (!engine.MatchEnded && ticks < TickBudget)
            {
                engine.RunTick();
                ticks++;

                Vector3 ballPos = engine.BallView.Position;
                Vector3 ballVel = engine.BallView.Velocity;

                if (ballPos.x > maxBallX)
                {
                    maxBallX = ballPos.x;
                }

                if (ballPos.x < minBallX)
                {
                    minBallX = ballPos.x;
                }

                if (ballPos.z > maxBallZ)
                {
                    maxBallZ = ballPos.z;
                }

                float speed = ballVel.magnitude;
                if (speed > maxBallSpeed)
                {
                    maxBallSpeed = speed;
                }

                if (ballPos.z > 0.5f)
                {
                    airborneTicks++;
                }

                if (ballPos.x < PitchLengthM / 3f)
                {
                    ownThirdTicks++;
                }
                else if (ballPos.x < 2f * PitchLengthM / 3f)
                {
                    middleThirdTicks++;
                }
                else
                {
                    finalThirdTicks++;
                }

                int possessor = engine.PossessingAgentId;
                if (possessor >= 0)
                {
                    possessedTicks++;
                }

                if (possessor != lastPossessor)
                {
                    possessionChanges++;
                    lastPossessor = possessor;
                }
            }

            TestContext.WriteLine(string.Format(
                CultureInfo.InvariantCulture,
                "ERR-030-014 [" + label + "] ticks={0} score={1}-{2}\n"
                + "  ball x range [{3:F2}, {4:F2}] (pitch 0..105); max z {5:F2}; max speed {6:F2} m/s\n"
                + "  thirds (by ball x): own {7} / middle {8} / final {9}\n"
                + "  possessed ticks {10} ({11:P1} of the match); possession changes {12}\n"
                + "  airborne ticks (z>0.5) {13}",
                ticks, engine.HomeScore, engine.AwayScore,
                minBallX, maxBallX, maxBallZ, maxBallSpeed,
                ownThirdTicks, middleThirdTicks, finalThirdTicks,
                possessedTicks, possessedTicks / (float)ticks, possessionChanges,
                airborneTicks));

            // Deliberately no assertion on scoring: the point is to record behaviour, and asserting the
            // present behaviour would pin a defect as a contract. The only thing checked is that the match
            // ran, so a broken harness cannot masquerade as a clean diagnostic.
            Assert.Greater(ticks, 0);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-07-26 | —      | Initial implementation: env-gated characterisation of the           |
// |         |            |        | ERR-030-014 zero-goal finding (ball travel, thirds occupancy,       |
// |         |            |        | possession churn) over one full engine match. Asserts nothing about |
// |         |            |        | scoring — pinning current behaviour would make a defect a contract. |
#endregion
