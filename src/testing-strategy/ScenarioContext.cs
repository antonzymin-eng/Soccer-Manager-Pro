// File:     src/testing-strategy/ScenarioContext.cs
// Created:  2026-06-10
// Modified: 2026-06-10
// Author:   —
// Spec:     Testing Strategy & Framework #19 §3.3.3 / KD-7,
//           Deterministic Simulation #16 §3.2 (DeterministicRngService), Code Standards #20
// Purpose:  Per-invocation context handed to a closed-loop scenario body: the manifest,
//           the run seed, a freshly seeded DeterministicRngService (KD-7: seeded before
//           any subsystem is initialised), and the outcome envelope to record into.
//           Also declares the ScenarioBody delegate consumed by ClosedLoopScenario.

using System;

using TacticalDirector.DeterministicSim;

namespace TacticalDirector.TestingStrategy
{
    /// <summary>
    /// Body of a closed-loop scenario: constructs fresh subsystem state (hermeticity per
    /// §3.3.3 — no state survives between invocations), drives the real subsystem loop,
    /// and records ≥ 1 envelope predicate (FR-TS-030). Bodies MUST draw randomness only
    /// from <see cref="ScenarioContext.Rng"/> (FR-TS-036; no System.Random).
    /// </summary>
    public delegate void ScenarioBody(ScenarioContext context);

    /// <summary>
    /// Inputs for one scenario-body invocation. Constructed by
    /// <see cref="ClosedLoopScenario"/>; one instance per run.
    /// Testing Strategy &amp; Framework #19 §3.3.3 / KD-7.
    /// </summary>
    public sealed class ScenarioContext
    {
        /// <summary>The manifest entry under which this run was launched.</summary>
        public ScenarioManifest Manifest { get; }

        /// <summary>Caller-supplied seed for this run, verbatim from ScenarioRunner.Run (KD-7).
        /// May differ from <see cref="ScenarioManifest.Seed"/> during seed sweeps.</summary>
        public ulong RunSeed { get; }

        /// <summary>RNG service seeded with <see cref="RunSeed"/> before any subsystem
        /// initialisation (KD-7). Scenario bodies that need randomness register draw-site
        /// streams here per #16 §3.2.5.1.</summary>
        public DeterministicRngService Rng { get; }

        /// <summary>Outcome envelope this run's predicates are recorded into.</summary>
        public ScenarioEnvelope Envelope { get; }

        internal ScenarioContext(
            ScenarioManifest manifest,
            ulong runSeed,
            DeterministicRngService rng,
            ScenarioEnvelope envelope)
        {
            Manifest = manifest ?? throw new ArgumentNullException(nameof(manifest));
            RunSeed  = runSeed;
            Rng      = rng ?? throw new ArgumentNullException(nameof(rng));
            Envelope = envelope ?? throw new ArgumentNullException(nameof(envelope));
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-06-10 | —      | Initial implementation. |
#endregion
