// File:     src/match-engine/MatchEngineBuildIdentity.cs
// Created:  2026-08-22
// Modified: 2026-08-22
// Author:   —
// Spec:     Deterministic Simulation #16 §2.3 (DeterminismContext.buildHash); Match Engine design note
//           (docs/tracking/match-engine-design.md); Code Standards #20
// Purpose:  Declares the match engine's AUTHORITATIVE ASSEMBLY CLOSURE — the compiled modules whose IL
//           can change a tick's result — and caches the #16 §2.3 buildHash over it. The composition
//           root owns this list because only it references everything it wires (ERR-016-009).

using System;
using System.Collections.Generic;
using System.Reflection;

using TacticalDirector.DeterministicSim;

namespace TacticalDirector.MatchEngine
{
    /// <summary>
    /// The match engine's build identity: <see cref="BuildHash"/> is
    /// <see cref="BuildIdentity.ComputeHash(IReadOnlyList{Assembly})"/> over
    /// <see cref="AuthoritativeAssemblies"/>, computed once per process.
    /// <para>
    /// <b>Why the list lives here.</b> <c>deterministic-sim</c> is a cross-cutting foundation and may
    /// not reference the layers above it, so it cannot name the closure; and the closure must be
    /// declared rather than discovered (see <see cref="BuildIdentity"/>). The composition root is the
    /// one place that already references every assembly it wires, so it can name them with ordinary
    /// <c>typeof</c> expressions — no reflection by name, no probing, and a missing assembly is a
    /// compile error rather than a silently shorter hash.
    /// </para>
    /// <para>
    /// <b>Scope.</b> Exactly the <c>match-engine.asmdef</c> reference closure plus this assembly. Those
    /// are the modules whose compiled code participates in a tick; assemblies above the engine
    /// (clients, viewers, analytics) and beside it (season / career subsystems) do not run inside one
    /// and are deliberately out. <c>MatchEngineBuildIdentityTests</c> fails if a new asmdef reference
    /// is added to the engine without being declared here.
    /// </para>
    /// <para>
    /// A <c>static readonly</c> computed once at type initialization is not the banned static mutable
    /// singleton of FR-CS-051..054: it is immutable, deterministic and never re-assigned — the
    /// <c>GameplayConfigHolder</c> boot-binding boundary this project already accepts.
    /// </para>
    /// </summary>
    public static class MatchEngineBuildIdentity
    {
        private static readonly Assembly[] s_authoritativeAssemblies = DeclareClosure();

        private static readonly string s_buildHash =
            BuildIdentity.ComputeHash(s_authoritativeAssemblies);

        /// <summary>
        /// The #16 §2.3 buildHash of the running binaries: 64-char lowercase hex. Stamped into every
        /// snapshot header this engine writes and re-checked at
        /// <see cref="MatchEngine.RestoreFromSnapshot"/>.
        /// </summary>
        public static string BuildHash => s_buildHash;

        /// <summary>
        /// The declared authoritative closure, in declaration order (the hash sorts internally, so this
        /// order is presentational only).
        /// </summary>
        public static IReadOnlyList<Assembly> AuthoritativeAssemblies => s_authoritativeAssemblies;

        // Every entry is fully qualified on purpose: five distinct `TacticalTranslation`-style name
        // collisions already live in this assembly's scope (MatchEngine.cs v1.17, CS0104), and a
        // closure declaration is the last place a using-directive ambiguity should be resolvable.
        private static Assembly[] DeclareClosure()
        {
            return new[]
            {
                typeof(MatchEngineConstants).Assembly,                                        // TacticalDirector.MatchEngine
                typeof(DeterministicSimConstants).Assembly,                                    // TacticalDirector.DeterministicSim
                typeof(TacticalDirector.EventSystem.EventSystemConstants).Assembly,
                typeof(TacticalDirector.BallPhysics.BallPhysicsConstants).Assembly,
                typeof(TacticalDirector.AgentMovement.MovementThresholds).Assembly,
                typeof(TacticalDirector.CollisionSystem.SpatialHashConstants).Assembly,
                typeof(TacticalDirector.FirstTouch.FirstTouchConstants).Assembly,
                typeof(TacticalDirector.PassMechanics.PassMechanicsConstants).Assembly,
                typeof(TacticalDirector.ShotMechanics.ShotMechanicsConstants).Assembly,
                typeof(TacticalDirector.PerceptionSystem.PerceptionConstants).Assembly,
                typeof(TacticalDirector.HeadingMechanics.HeadingMechanicsConstants).Assembly,
                typeof(TacticalDirector.GoalkeeperMechanics.GoalkeeperConstants).Assembly,
                typeof(TacticalDirector.DecisionTree.DecisionTreeConstants).Assembly,
                typeof(TacticalDirector.PositioningAI.PositioningAIConstants).Assembly,
                typeof(TacticalDirector.PressingAI.PressingAIConstants).Assembly,
                typeof(TacticalDirector.DefensiveAI.DefensiveAIConstants).Assembly,
                typeof(TacticalDirector.AttackingAI.AttackingAIConstants).Assembly,
                typeof(TacticalDirector.TacticalInstructions.TacticalInstructionsConstants).Assembly,
                typeof(TacticalDirector.ProjectConstants.GameplayConfig).Assembly,
                typeof(TacticalDirector.PlayerDatabase.PlayerDatabaseConstants).Assembly,
            };
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-08-22 | —      | Initial implementation. ERR-016-009: the composition root       |
// |         |            |        | declares the authoritative assembly closure and caches the      |
// |         |            |        | §2.3 buildHash over it.                                         |
#endregion
