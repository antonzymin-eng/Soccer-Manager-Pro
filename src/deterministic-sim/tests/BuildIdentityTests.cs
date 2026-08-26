// File:     src/deterministic-sim/tests/BuildIdentityTests.cs
// Created:  2026-08-22
// Modified: 2026-08-22
// Author:   —
// Spec:     Deterministic Simulation #16 §2.3.2 (buildHash), §3.4 (DOMAIN_TAG_BUILD_IDENTITY /
//           BUILD_IDENTITY_VERSION), §9.5 (golden vectors), Code Standards #20
// Purpose:  Locks the §2.3.2 buildHash: an independently-derived golden vector, declaration-order
//           invariance, per-field sensitivity (name, MVID, module count), and the fail-loud guards
//           that stop an empty or duplicated closure from producing a hash at all.

using System;
using System.Collections.Generic;
using System.Reflection;

using NUnit.Framework;

namespace TacticalDirector.DeterministicSim
{
    /// <summary>
    /// Test coverage for <see cref="BuildIdentity"/> (#16 §2.3.2, ERR-016-009).
    /// </summary>
    [TestFixture]
    public sealed class BuildIdentityTests
    {
        private static readonly Guid MvidAlpha = new Guid("00000000-0000-0000-0000-000000000001");
        private static readonly Guid MvidBeta  = new Guid("0f0e0d0c-0b0a-0908-0706-050403020100");

        private static BuildModule[] TwoModules() => new[]
        {
            new BuildModule("TacticalDirector.Alpha", MvidAlpha),
            new BuildModule("TacticalDirector.Beta",  MvidBeta),
        };

        /// <summary>
        /// Golden vector. The expected digest is derived INDEPENDENTLY of the code under test, by a
        /// Python mirror of the §2.3.2 preimage (the FloatFlagTuple / ERR-016-006 precedent), so this
        /// asserts the specified preimage rather than whatever the implementation happens to build:
        /// <code>
        /// python3 - &lt;&lt;'PY'
        /// import hashlib, struct
        /// def s(x):
        ///     b = x.encode('ascii'); return struct.pack('&lt;I', len(b)) + b
        /// mods = [("TacticalDirector.Alpha", "00000000000000000000000000000001"),
        ///         ("TacticalDirector.Beta",  "0f0e0d0c0b0a09080706050403020100")]
        /// mods.sort(key=lambda m: m[0].encode('utf-8'))
        /// p = bytes([0x2E]) + struct.pack('&lt;H', 1) + struct.pack('&lt;I', len(mods))
        /// for n, g in mods: p += s(n) + s(g)
        /// print(len(p), hashlib.sha256(p).hexdigest())
        /// PY
        /// </code>
        /// prints <c>130 df3b006110165f587228a1ede699211933991b923d4a7d077d977bab5c4f18d7</c>.
        /// </summary>
        [Test]
        public void GoldenVector_TwoModules_MatchesIndependentDerivation()
        {
            Assert.AreEqual(
                "df3b006110165f587228a1ede699211933991b923d4a7d077d977bab5c4f18d7",
                BuildIdentity.ComputeHash(TwoModules()));
        }

        [Test]
        public void Hash_Is64LowercaseHexCharacters()
        {
            string hash = BuildIdentity.ComputeHash(TwoModules());

            Assert.AreEqual(64, hash.Length);
            for (int i = 0; i < hash.Length; i++)
            {
                char c = hash[i];
                Assert.IsTrue((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f'), $"char {i} = '{c}'");
            }
        }

        /// <summary>
        /// The closure is a SET: the composition root's declaration order must not reach the hash, or
        /// re-ordering a `typeof` list would refuse every existing save.
        /// </summary>
        [Test]
        public void DeclarationOrder_DoesNotChangeTheHash()
        {
            BuildModule[] forward = TwoModules();
            var reversed = new[] { forward[1], forward[0] };

            Assert.AreEqual(BuildIdentity.ComputeHash(forward), BuildIdentity.ComputeHash(reversed));
        }

        /// <summary>The whole point: a changed MVID — i.e. changed compiled IL — changes the identity.</summary>
        [Test]
        public void AChangedModuleVersionId_ChangesTheHash()
        {
            var moved = new[]
            {
                new BuildModule("TacticalDirector.Alpha", new Guid("00000000-0000-0000-0000-000000000002")),
                new BuildModule("TacticalDirector.Beta",  MvidBeta),
            };

            Assert.AreNotEqual(BuildIdentity.ComputeHash(TwoModules()), BuildIdentity.ComputeHash(moved));
        }

        [Test]
        public void AChangedAssemblyName_ChangesTheHash()
        {
            var renamed = new[]
            {
                new BuildModule("TacticalDirector.Alpho", MvidAlpha),
                new BuildModule("TacticalDirector.Beta",  MvidBeta),
            };

            Assert.AreNotEqual(BuildIdentity.ComputeHash(TwoModules()), BuildIdentity.ComputeHash(renamed));
        }

        /// <summary>
        /// A dropped module must be visible. Without the module-count field a length-prefixed
        /// concatenation could in principle be re-parsed as a different closure; the count pins it.
        /// </summary>
        [Test]
        public void AddingOrDroppingAModule_ChangesTheHash()
        {
            var three = new List<BuildModule>(TwoModules())
            {
                new BuildModule("TacticalDirector.Gamma", MvidBeta),
            };
            var one = new[] { new BuildModule("TacticalDirector.Alpha", MvidAlpha) };

            string two = BuildIdentity.ComputeHash(TwoModules());
            Assert.AreNotEqual(two, BuildIdentity.ComputeHash(three));
            Assert.AreNotEqual(two, BuildIdentity.ComputeHash(one));
        }

        // ── Fail-loud guards ───────────────────────────────────────────────────────────

        [Test]
        public void NullModuleList_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => BuildIdentity.ComputeHash((IReadOnlyList<BuildModule>)null));
        }

        /// <summary>
        /// An empty closure would hash identically for every build — the ERR-016-009 gap wearing a
        /// hash. It must be impossible to produce one.
        /// </summary>
        [Test]
        public void EmptyClosure_Throws()
        {
            Assert.Throws<ArgumentException>(() => BuildIdentity.ComputeHash(new BuildModule[0]));
        }

        [Test]
        public void DuplicateAssemblyName_Throws()
        {
            var dupe = new[]
            {
                new BuildModule("TacticalDirector.Alpha", MvidAlpha),
                new BuildModule("TacticalDirector.Alpha", MvidBeta),
            };

            Assert.Throws<ArgumentException>(() => BuildIdentity.ComputeHash(dupe));
        }

        /// <summary>
        /// <c>default(BuildModule)</c> bypasses the guarding constructor, so the hasher re-checks:
        /// a zero-value struct in the list must throw rather than contribute an empty name.
        /// </summary>
        [Test]
        public void DefaultBuildModuleInTheList_Throws()
        {
            var withDefault = new[] { new BuildModule("TacticalDirector.Alpha", MvidAlpha), default(BuildModule) };

            Assert.Throws<ArgumentException>(() => BuildIdentity.ComputeHash(withDefault));
        }

        [Test]
        public void EmptyAssemblyName_ThrowsAtConstruction()
        {
            Assert.Throws<ArgumentException>(() => new BuildModule(string.Empty, MvidAlpha));
            Assert.Throws<ArgumentException>(() => new BuildModule(null, MvidAlpha));
        }

        /// <summary>
        /// §3.2.4.1's canonical string encoding is ASCII, so a non-ASCII assembly name would be
        /// silently mangled into a hash no other process could reproduce. Refuse it at the boundary.
        /// </summary>
        [Test]
        public void NonAsciiAssemblyName_ThrowsAtConstruction()
        {
            Assert.Throws<ArgumentException>(() => new BuildModule("TacticalDirector.Ünicode", MvidAlpha));
        }

        // ── Assembly projection ────────────────────────────────────────────────────────

        [Test]
        public void FromAssemblies_ProjectsSimpleNameAndManifestMvid()
        {
            Assembly self = typeof(BuildIdentity).Assembly;

            BuildModule[] modules = BuildIdentity.FromAssemblies(new[] { self });

            Assert.AreEqual(1, modules.Length);
            Assert.AreEqual(self.GetName().Name, modules[0].AssemblyName);
            Assert.AreEqual(self.ManifestModule.ModuleVersionId, modules[0].ModuleVersionId);
            Assert.AreNotEqual(Guid.Empty, modules[0].ModuleVersionId,
                "A compiled module always carries an MVID; Guid.Empty means the projection read the wrong thing.");
        }

        [Test]
        public void FromAssemblies_NullEntry_Throws()
        {
            Assert.Throws<ArgumentException>(() => BuildIdentity.FromAssemblies(new Assembly[] { null }));
        }

        [Test]
        public void FromAssemblies_NullList_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => BuildIdentity.FromAssemblies(null));
        }

        /// <summary>
        /// The assembly overload must agree with the explicit-module overload — they are one contract,
        /// and a divergence would mean the production path hashes something the golden vector does not
        /// describe.
        /// </summary>
        [Test]
        public void AssemblyOverload_AgreesWithModuleOverload()
        {
            Assembly[] assemblies = { typeof(BuildIdentity).Assembly };

            Assert.AreEqual(
                BuildIdentity.ComputeHash(BuildIdentity.FromAssemblies(assemblies)),
                BuildIdentity.ComputeHash(assemblies));
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-08-22 | —      | Initial suite. ERR-016-009: golden vector (Python-mirrored),   |
// |         |            |        | order invariance, name/MVID/count sensitivity, and the         |
// |         |            |        | empty/duplicate/default/non-ASCII fail-loud guards.            |
#endregion
