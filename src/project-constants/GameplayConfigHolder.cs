// File:     src/project-constants/GameplayConfigHolder.cs
// Created:  2026-06-30
// Modified: 2026-06-30
// Author:   —
// Spec:     Code Standards #20 §3.2.3 (FR-CS-019), §4.2
// Purpose:  Resolves the boot-sequencing design point left open by GameplayConfig.cs: a [GT]
//           catalogue's constants are `public static readonly` fields, so they are assigned in
//           the catalogue type's static constructor — a point no ordinary constructor-injection
//           call site can reach. Bind() is the single explicit injection point a composition
//           root calls, once, before any catalogue is referenced.

using System;

namespace TacticalDirector.ProjectConstants
{
    /// <summary>
    /// Single boot-time binding point for the injected <see cref="GameplayConfig"/>. Every
    /// migrated <c>[GT]</c> catalogue field reads <see cref="Config"/> in its own static field
    /// initializer (<c>using static TacticalDirector.ProjectConstants.GameplayConfigHolder;</c>
    /// lets a catalogue write the bare <c>Config.GetFloat(...)</c> form documented in
    /// <c>src/CLAUDE.md</c>).
    /// </summary>
    /// <remarks>
    /// THE PROBLEM — a catalogue's <c>[GT]</c> constants are <c>public static readonly</c> fields.
    /// They are assigned once, in the catalogue's static constructor, which the CLR is free to run
    /// at any point at or before the type's first use (<c>beforefieldinit</c>). There is no object
    /// instance at that point for ordinary constructor injection to reach — FR-CS-019's loader
    /// produces a <see cref="GameplayConfig"/> instance, but nothing hands it to a static field
    /// initializer.
    ///
    /// THE RESOLUTION — <see cref="Bind"/> is the one explicit call a composition root makes,
    /// before referencing any <c>[GT]</c> catalogue, to supply the loaded config. Until
    /// <see cref="Bind"/> runs, <see cref="Config"/> resolves to <see cref="GameplayConfig.Empty"/>,
    /// so every catalogue keeps today's literal fallback values — a process that never calls
    /// <see cref="Bind"/> (e.g. a unit test, or Stage 0 before a composition root exists) is
    /// behaviour-neutral. The FIRST read of <see cref="Config"/> locks the binding: any
    /// <see cref="Bind"/> call after that point means at least one catalogue's static constructor
    /// already ran against the wrong (default) instance — silently continuing would leave that
    /// catalogue permanently stuck on fallback values with no symptom, so the lock makes the
    /// ordering mistake fail loud immediately, the same fail-loud posture <see cref="GameplayConfig"/>
    /// itself takes on a malformed value.
    ///
    /// WHY THIS IS NOT THE BANNED "STATIC MUTABLE SINGLETON" PATTERN (FR-CS-051..054) — that ban
    /// targets hidden, repeatedly-mutated AMBIENT GAME STATE (the kind that breaks deterministic
    /// replay because two ticks can observe different values through the same static handle). This
    /// holder is written exactly once, by the composition root, before the simulation starts, and
    /// every subsequent read for the lifetime of the process returns the same instance — the same
    /// one-shot boot-wiring boundary the project already accepts for
    /// <c>EventBusRegistrar.Initialize()</c> / <c>EventRegistry.EnsureInitialized()</c>. Nothing on
    /// the 60 Hz path calls <see cref="Bind"/>; catalogues call <see cref="Config"/>'s getter only
    /// from their own static field initializers, i.e. once per catalogue, at boot.
    /// </remarks>
    public static class GameplayConfigHolder
    {
        private static GameplayConfig s_config = GameplayConfig.Empty;
        private static bool s_locked;

        /// <summary>
        /// The active config. Resolves to <see cref="GameplayConfig.Empty"/> until <see cref="Bind"/>
        /// is called. Reading this property locks the binding — see the type remarks.
        /// </summary>
        public static GameplayConfig Config
        {
            get
            {
                s_locked = true;
                return s_config;
            }
        }

        /// <summary>
        /// Supplies the loaded <see cref="GameplayConfig"/>. Must be called before any <c>[GT]</c>
        /// catalogue is referenced (i.e. before <see cref="Config"/> has been read by anything).
        /// </summary>
        /// <param name="config">The config to bind. Must not be <c>null</c>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="config"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">
        /// A catalogue already read the default config before this call — the composition root
        /// bound too late to take effect everywhere.
        /// </exception>
        public static void Bind(GameplayConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }
            if (s_locked)
            {
                throw new InvalidOperationException(
                    "GameplayConfigHolder.Bind() was called after a [GT] catalogue already read " +
                    "the default config. Bind() must run before any game-constant catalogue is " +
                    "referenced (the composition root's very first step).");
            }
            s_config = config;
        }

        /// <summary>
        /// Test-only reset of the bound config and the lock. Production code MUST NOT call this —
        /// it exists so NUnit fixtures can exercise <see cref="Bind"/> repeatedly in one process.
        /// </summary>
        internal static void ResetForTests()
        {
            s_config = GameplayConfig.Empty;
            s_locked = false;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-06-30 | —      | Initial implementation — resolves the boot-sequencing design   |
// |         |            |        | point for migrating [GT] catalogues onto GameplayConfig.        |
#endregion
