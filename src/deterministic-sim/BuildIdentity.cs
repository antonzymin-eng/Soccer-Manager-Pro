// File:     src/deterministic-sim/BuildIdentity.cs
// Created:  2026-08-22
// Modified: 2026-08-22
// Author:   —
// Spec:     Deterministic Simulation #16 §2.3 (DeterminismContext.buildHash), §3.4 (domain tag /
//           error code), §3.2.4.1 (CanonicalSerializer), Code Standards #20
// Purpose:  Computes the §2.3 buildHash — the identity of the COMPILED BINARIES a match ran on — as a
//           SHA-256 over the canonically ordered (assembly name, module version id) pairs of a
//           caller-DECLARED authoritative assembly closure. Closes the ERR-016-009 gap.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace TacticalDirector.DeterministicSim
{
    /// <summary>
    /// One module of the authoritative build closure: an assembly's simple name plus the Module
    /// Version ID the compiler stamped into its manifest module. Deterministic Simulation #16 §2.3.
    /// </summary>
    public readonly struct BuildModule
    {
        /// <summary>Assembly simple name (e.g. <c>TacticalDirector.BallPhysics</c>). ASCII, non-empty.</summary>
        public readonly string AssemblyName;

        /// <summary>The compiler-stamped Module Version ID of that assembly's manifest module.</summary>
        public readonly Guid ModuleVersionId;

        /// <summary>
        /// Constructs one closure entry. Fail-loud on a null / empty / non-ASCII assembly name: the
        /// preimage is written through <see cref="CanonicalSerializer.WriteString"/>, which is ASCII
        /// (§3.2.4.1), so a non-ASCII name would be silently mangled into a hash nothing could
        /// reproduce.
        /// </summary>
        public BuildModule(string assemblyName, Guid moduleVersionId)
        {
            if (string.IsNullOrEmpty(assemblyName))
            {
                throw new ArgumentException(
                    "A build-closure module must carry a non-empty assembly name (#16 §2.3).",
                    nameof(assemblyName));
            }
            for (int i = 0; i < assemblyName.Length; i++)
            {
                if (assemblyName[i] > 0x7F)
                {
                    throw new ArgumentException(
                        "Assembly name '" + assemblyName + "' is not ASCII — the §3.2.4.1 canonical " +
                        "string encoding cannot represent it without loss.",
                        nameof(assemblyName));
                }
            }

            AssemblyName    = assemblyName;
            ModuleVersionId = moduleVersionId;
        }
    }

    /// <summary>
    /// The §2.3 <c>buildHash</c>: which compiled binaries produced a run.
    /// <para>
    /// <b>What build identity IS here (ERR-016-009 decision).</b> The hash covers the MODULE VERSION
    /// IDs of a declared authoritative assembly closure. The MVID is the identity Roslyn stamps into
    /// each emitted module: it changes whenever the emitted IL changes, and under deterministic
    /// compilation (the C# compiler's default) it is stable across rebuilds of identical inputs. It is
    /// therefore an identity of the BINARY, which is what a replay actually re-executes.
    /// </para>
    /// <para>
    /// <b>What it is NOT, and why.</b> Not a CI-stamped commit: a commit identifies SOURCE, and the
    /// builds where determinism defects are actually found — a dirty working tree, a different
    /// compiler, a different target framework — all differ as binaries while sharing one commit, or
    /// carry no stamp at all. Not the <c>.asmdef</c> closure alone: the closure names WHICH assemblies
    /// participate, not what is in them, so two builds differing only in compiled code have identical
    /// closures. The closure survives as the SCOPE selector and the MVIDs supply the content.
    /// </para>
    /// <para>
    /// <b>The closure is declared, never discovered.</b> <c>AppDomain.GetAssemblies()</c> returns
    /// whatever happens to have been loaded, which differs between a player run, an editor run and a
    /// test run of one build — a hash that depends on load order is not a build identity. The caller
    /// (a composition root, which by construction references everything it wires) passes the closure
    /// in; this type only hashes it. Compare <see cref="EnvironmentFingerprint.CreateStage0MonoCertified"/>,
    /// whose Mono runtime version is host-supplied for the same reason.
    /// </para>
    /// <para>
    /// <b>Over-sensitivity is the accepted failure direction.</b> Under a toolchain that compiles
    /// non-deterministically the MVID moves on every rebuild, so a save written by the previous build
    /// is REFUSED. That is a loud, correct refusal; the opposite error — two genuinely different
    /// builds hashing alike — silently validates a divergent replay. The contract this hash owes is
    /// collision-avoidance, not rebuild-stability.
    /// </para>
    /// <para>
    /// Not on any hot path: a build hash is computed once, at composition-root construction.
    /// </para>
    /// </summary>
    public static class BuildIdentity
    {
        /// <summary>
        /// Computes the buildHash over an explicit module list: 64-char lowercase hex of
        /// <c>SHA-256( 0x2E ‖ BUILD_IDENTITY_VERSION ‖ moduleCount ‖ (name ‖ mvidHex)* )</c>, with the
        /// modules sorted by ordinal assembly name so the caller's declaration order cannot change the
        /// result. The MVID is written in its canonical 32-char lowercase hex form
        /// (<c>Guid.ToString("N")</c>) rather than as <c>Guid.ToByteArray()</c> bytes, whose field
        /// ordering is a runtime layout detail rather than a canonical wire form.
        /// <para>
        /// Fail-loud on an empty closure (every build would share one identity), on a duplicated
        /// assembly name, and on a null list. Deterministic Simulation #16 §2.3 / §3.4.
        /// </para>
        /// </summary>
        public static string ComputeHash(IReadOnlyList<BuildModule> modules)
        {
            if (modules == null)
            {
                throw new ArgumentNullException(nameof(modules));
            }
            if (modules.Count == 0)
            {
                throw new ArgumentException(
                    "The authoritative build closure is empty — every build would then share one " +
                    "identity, which is the ERR-016-009 gap rather than a fix for it.",
                    nameof(modules));
            }

            BuildModule[] ordered = new BuildModule[modules.Count];
            for (int i = 0; i < modules.Count; i++)
            {
                if (string.IsNullOrEmpty(modules[i].AssemblyName))
                {
                    throw new ArgumentException(
                        "Build-closure module at index " + i + " has no assembly name — a default(BuildModule) " +
                        "never went through the guarding constructor.", nameof(modules));
                }
                ordered[i] = modules[i];
            }

            Array.Sort(ordered, (a, b) => string.CompareOrdinal(a.AssemblyName, b.AssemblyName));

            for (int i = 1; i < ordered.Length; i++)
            {
                if (string.CompareOrdinal(ordered[i - 1].AssemblyName, ordered[i].AssemblyName) == 0)
                {
                    throw new ArgumentException(
                        "Assembly '" + ordered[i].AssemblyName + "' appears twice in the build closure — " +
                        "a module contributes exactly once.", nameof(modules));
                }
            }

            int size = DeterministicSimConstants.FIELD_WIDTH_DOMAIN_TAG
                     + 2   // BUILD_IDENTITY_VERSION
                     + 4;  // module count
            for (int i = 0; i < ordered.Length; i++)
            {
                size += 4 + Encoding.ASCII.GetByteCount(ordered[i].AssemblyName)
                      + 4 + BuildIdentityMvidHexLength;
            }

            byte[] preimage = new byte[size];
            int o = 0;
            CanonicalSerializer.WriteU8 (preimage, ref o, DeterministicSimConstants.DOMAIN_TAG_BUILD_IDENTITY);
            CanonicalSerializer.WriteU16(preimage, ref o, DeterministicSimConstants.BUILD_IDENTITY_VERSION);
            CanonicalSerializer.WriteU32(preimage, ref o, (uint)ordered.Length);
            for (int i = 0; i < ordered.Length; i++)
            {
                CanonicalSerializer.WriteString(preimage, ref o, ordered[i].AssemblyName);
                CanonicalSerializer.WriteString(
                    preimage, ref o, ordered[i].ModuleVersionId.ToString("N", CultureInfo.InvariantCulture));
            }

            using (SHA256 sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(preimage, 0, o);
                var sb = new StringBuilder(hash.Length * 2);
                for (int i = 0; i < hash.Length; i++)
                {
                    sb.Append(hash[i].ToString("x2", CultureInfo.InvariantCulture));
                }
                return sb.ToString();
            }
        }

        /// <summary>
        /// Convenience overload: projects the assemblies through <see cref="FromAssemblies"/> and hashes
        /// them. The caller declares the closure; nothing here discovers it.
        /// </summary>
        public static string ComputeHash(IReadOnlyList<Assembly> assemblies) =>
            ComputeHash(FromAssemblies(assemblies));

        /// <summary>
        /// Projects loaded assemblies onto their (simple name, MVID) pairs. Reading assembly metadata is
        /// not the banned hot-path reflection of FR-CS-034 — this runs once at composition-root
        /// construction and touches no type, member or invocation surface.
        /// </summary>
        public static BuildModule[] FromAssemblies(IReadOnlyList<Assembly> assemblies)
        {
            if (assemblies == null)
            {
                throw new ArgumentNullException(nameof(assemblies));
            }

            var modules = new BuildModule[assemblies.Count];
            for (int i = 0; i < assemblies.Count; i++)
            {
                Assembly asm = assemblies[i];
                if (asm == null)
                {
                    throw new ArgumentException(
                        "Build-closure assembly at index " + i + " is null.", nameof(assemblies));
                }
                modules[i] = new BuildModule(asm.GetName().Name, asm.ManifestModule.ModuleVersionId);
            }
            return modules;
        }

        // Width of Guid.ToString("N") — 32 hex chars, no separators.
        private const int BuildIdentityMvidHexLength = 32;
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-08-22 | —      | Initial implementation. ERR-016-009: the §2.3 buildHash gets a  |
// |         |            |        | representation — SHA-256 over the declared authoritative        |
// |         |            |        | assembly closure's (name, MVID) pairs, domain-tagged 0x2E.      |
#endregion
