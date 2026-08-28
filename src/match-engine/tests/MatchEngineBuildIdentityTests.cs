// File:     src/match-engine/tests/MatchEngineBuildIdentityTests.cs
// Created:  2026-08-22
// Modified: 2026-08-22
// Author:   —
// Spec:     Deterministic Simulation #16 §2.3.2 (buildHash) / §3.4 (ERR_DS_REPLAY_BUILD_MISMATCH);
//           on-disk match save file (docs/tracking/match-save-file-design.md) KD-7; Code Standards #20
// Purpose:  Locks the composition root's half of the §2.3.2 buildHash — that the declared closure
//           actually covers every assembly the engine references, that a captured header carries the
//           live hash, that the save format round-trips it and refuses an empty one at BOTH ends, and
//           that a restore under a foreign build hash is refused before any state is touched.

using System;
using System.Collections.Generic;
using System.Reflection;

using NUnit.Framework;

using TacticalDirector.DeterministicSim;

namespace TacticalDirector.MatchEngine
{
    /// <summary>
    /// Test coverage for <see cref="MatchEngineBuildIdentity"/> and the build-identity gate
    /// (#16 §2.3.2, ERR-016-009).
    /// </summary>
    [TestFixture]
    public sealed class MatchEngineBuildIdentityTests
    {
        private const ulong MatchSeed = 0xC0FFEE_1234_5678UL;

        // Offset of the build-hash u32 length prefix inside a v2 save blob:
        // format(4) + seed(8) + schema(4) + digestVersion(2) + tick(8) + prev(32) + cur(32)
        // + cursorTick(8) + cursorPhase(1) = 99.
        private const int BuildHashLengthOffset = 99;

        private static (SnapshotHeader, SnapshotPayload) CaptureAt(int n)
        {
            MatchEngine a = new MatchEngine(MatchSeed);
            for (int i = 0; i < n; i++) a.RunTick();
            return (a.CaptureDurableHeader(), a.CaptureDurablePayload());
        }

        // ── The declared closure ───────────────────────────────────────────────────────

        /// <summary>
        /// The drift lock. `MatchEngineBuildIdentity` names its closure by hand, so adding an asmdef
        /// reference without adding it here would silently shrink what the build hash covers — a new
        /// subsystem could then change the sim while every save still validated. Asserted in the
        /// direction that matters: every `TacticalDirector.*` assembly the engine actually binds
        /// against in metadata must be declared. (The converse is deliberately not asserted — the
        /// compiler drops metadata references for an assembly no emitted code touches, so a declared
        /// but currently-unused reference is not drift.)
        /// </summary>
        [Test]
        public void DeclaredClosure_CoversEveryReferencedTacticalDirectorAssembly()
        {
            var declared = new HashSet<string>(StringComparer.Ordinal);
            foreach (Assembly asm in MatchEngineBuildIdentity.AuthoritativeAssemblies)
            {
                declared.Add(asm.GetName().Name);
            }

            Assembly engine = typeof(MatchEngine).Assembly;
            Assert.IsTrue(declared.Contains(engine.GetName().Name),
                "The engine assembly itself must be in its own build closure.");

            foreach (AssemblyName referenced in engine.GetReferencedAssemblies())
            {
                if (referenced.Name != null && referenced.Name.StartsWith("TacticalDirector.", StringComparison.Ordinal))
                {
                    Assert.IsTrue(declared.Contains(referenced.Name),
                        $"'{referenced.Name}' is referenced by the match engine but is NOT in the declared " +
                        "build closure — MatchEngineBuildIdentity.DeclareClosure() has drifted from " +
                        "match-engine.asmdef (#16 §2.3.2 rule 1).");
                }
            }
        }

        [Test]
        public void DeclaredClosure_HasNoDuplicatesAndIsNonEmpty()
        {
            IReadOnlyList<Assembly> closure = MatchEngineBuildIdentity.AuthoritativeAssemblies;
            Assert.Greater(closure.Count, 1);

            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (Assembly asm in closure)
            {
                Assert.IsTrue(seen.Add(asm.GetName().Name), $"'{asm.GetName().Name}' declared twice.");
            }
        }

        [Test]
        public void BuildHash_IsTheHashOfTheDeclaredClosureAndIsStable()
        {
            string expected = BuildIdentity.ComputeHash(MatchEngineBuildIdentity.AuthoritativeAssemblies);

            Assert.AreEqual(expected, MatchEngineBuildIdentity.BuildHash);
            Assert.AreEqual(MatchEngineBuildIdentity.BuildHash, MatchEngineBuildIdentity.BuildHash);
            Assert.AreEqual(64, MatchEngineBuildIdentity.BuildHash.Length);
        }

        /// <summary>
        /// Sensitivity through the REAL closure, not a fabricated one: drop any single declared module
        /// and the identity must move. A hash that did not would be covering fewer modules than it
        /// claims.
        /// </summary>
        [Test]
        public void DroppingAnyDeclaredModule_ChangesTheBuildHash()
        {
            IReadOnlyList<Assembly> closure = MatchEngineBuildIdentity.AuthoritativeAssemblies;

            for (int drop = 0; drop < closure.Count; drop++)
            {
                var shorter = new List<Assembly>(closure.Count - 1);
                for (int i = 0; i < closure.Count; i++)
                {
                    if (i != drop) shorter.Add(closure[i]);
                }

                Assert.AreNotEqual(MatchEngineBuildIdentity.BuildHash, BuildIdentity.ComputeHash(shorter),
                    $"Dropping '{closure[drop].GetName().Name}' left the build hash unchanged.");
            }
        }

        // ── The header carries it ──────────────────────────────────────────────────────

        [Test]
        public void CapturedHeader_CarriesTheLiveBuildHash()
        {
            (SnapshotHeader header, SnapshotPayload _) = CaptureAt(5);

            Assert.AreEqual(MatchEngineBuildIdentity.BuildHash, header.BuildHash,
                "Every snapshot header this engine writes must carry the running build's identity (FR-DS-014).");
        }

        [Test]
        public void SnapshotHeaderInitialize_RefusesAnEmptyBuildHash()
        {
            var header = new SnapshotHeader();

            Assert.Throws<ArgumentException>(
                () => header.Initialize(0UL, null, EnvironmentFingerprint.CreateStage0Dev(), string.Empty));
            Assert.Throws<ArgumentException>(
                () => header.Initialize(0UL, null, EnvironmentFingerprint.CreateStage0Dev(), null));
        }

        // ── The save format carries it (match-save-file-design.md KD-7) ────────────────

        [Test]
        public void Codec_RoundTripsTheBuildHash()
        {
            (SnapshotHeader header, SnapshotPayload payload) = CaptureAt(20);

            MatchSaveContents got = MatchSaveCodec.Decode(MatchSaveCodec.Encode(MatchSeed, header, payload));

            Assert.AreEqual(header.BuildHash, got.Header.BuildHash);
            Assert.AreEqual(MatchEngineBuildIdentity.BuildHash, got.Header.BuildHash);
        }

        /// <summary>
        /// Write side of KD-7: a codec that can write a file its own reader rejects is the defect this
        /// project has filed against sibling codecs twice.
        /// </summary>
        [Test]
        public void Codec_RefusesToEncodeAHeaderWithoutABuildHash()
        {
            (SnapshotHeader header, SnapshotPayload payload) = CaptureAt(5);
            header.BuildHash = null;

            Assert.Throws<ArgumentException>(() => MatchSaveCodec.Encode(MatchSeed, header, payload));

            header.BuildHash = string.Empty;
            Assert.Throws<ArgumentException>(() => MatchSaveCodec.Encode(MatchSeed, header, payload));
        }

        /// <summary>
        /// Read side of KD-7. Encode refuses to produce such a blob, so it is spliced by hand: rewrite
        /// the build-hash length prefix to 0 and drop its bytes. Without this the decode-side refusal
        /// would have no lock at all.
        /// </summary>
        [Test]
        public void Codec_RefusesABlobCarryingAnEmptyBuildHash()
        {
            (SnapshotHeader header, SnapshotPayload payload) = CaptureAt(5);
            byte[] blob = MatchSaveCodec.Encode(MatchSeed, header, payload);

            int o = BuildHashLengthOffset;
            uint hashLen = CanonicalSerializer.ReadU32(blob, ref o);
            Assert.AreEqual((uint)header.BuildHash.Length, hashLen,
                "The build-hash length prefix is not at the offset this test splices at — the v2 layout moved.");

            var spliced = new byte[blob.Length - (int)hashLen];
            Array.Copy(blob, 0, spliced, 0, BuildHashLengthOffset);
            int w = BuildHashLengthOffset;
            CanonicalSerializer.WriteU32(spliced, ref w, 0u);
            Array.Copy(blob, o + (int)hashLen, spliced, w, blob.Length - o - (int)hashLen);

            Assert.Throws<InvalidOperationException>(() => MatchSaveCodec.Decode(spliced),
                "A save blob whose build identity is empty must be refused, not restored (KD-7).");
        }

        // ── The restore gate ───────────────────────────────────────────────────────────

        /// <summary>
        /// Positive control for the two refusal tests below: an untouched round-trip must restore, so a
        /// green refusal test cannot be green for an unrelated reason.
        /// </summary>
        [Test]
        public void Restore_AcceptsTheLiveBuildHash()
        {
            (SnapshotHeader header, SnapshotPayload payload) = CaptureAt(20);
            MatchSaveContents got = MatchSaveCodec.Decode(MatchSaveCodec.Encode(MatchSeed, header, payload));

            Assert.DoesNotThrow(
                () => MatchEngine.RestoreFromSnapshot(got.Header, got.Payload, got.MatchSeed));
        }

        [Test]
        public void Restore_RefusesAForeignBuildHash()
        {
            (SnapshotHeader header, SnapshotPayload payload) = CaptureAt(20);
            MatchSaveContents got = MatchSaveCodec.Decode(MatchSaveCodec.Encode(MatchSeed, header, payload));

            got.Header.BuildHash = new string('0', 64);

            InvalidOperationException ex = Assert.Throws<InvalidOperationException>(
                () => MatchEngine.RestoreFromSnapshot(got.Header, got.Payload, got.MatchSeed),
                "A snapshot written by different binaries must be refused before any state is touched " +
                "(#16 §2.3.2 rule 3 / EC-016-015).");

            StringAssert.Contains(
                DeterministicSimConstants.ERR_DS_REPLAY_BUILD_MISMATCH.ToString("X4"), ex.Message,
                "The refusal must name the build-mismatch code, not the environment-mismatch one — the " +
                "two axes were conflated before ERR-016-009.");
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-08-22 | —      | Initial suite. ERR-016-009: closure-drift lock, per-module     |
// |         |            |        | sensitivity, header carriage, KD-7 codec round-trip + both     |
// |         |            |        | empty-hash refusals, and the restore gate with its positive    |
// |         |            |        | control.                                                       |
#endregion
