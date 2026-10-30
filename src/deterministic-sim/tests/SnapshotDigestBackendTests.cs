// File:     src/deterministic-sim/tests/SnapshotDigestBackendTests.cs
// Created:  2026-10-21
// Modified: 2026-10-21
// Author:   —
// Spec:     Deterministic Simulation #16 §3.2.3, Code Standards #20;
//           docs/tracking/perception-shared-pass-design.md §5 E3 (P1b)
// Purpose:  Locks the Gate-J J-05 / P1b snapshot-digest change. SnapshotCodec now hashes one reused
//           contiguous preimage with Windows CNG (CngSha256) or a reused managed SHA-256, instead of a
//           fresh SHA256 per tick fed through TransformFinalBlock. These tests pin that the digest is
//           still exactly SHA-256 over 0x12 ‖ header fields ‖ 0x11 ‖ payload — re-derived here
//           independently with System.Security.Cryptography — across a chain and at every SHA-256
//           padding boundary; that CNG, where it runs, agrees with the managed implementation; that
//           the Windows hosts (the pinned Unity host) actually get the CNG path; and that a
//           steady-state Encode allocates nothing.

using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

using NUnit.Framework;
#if UNITY_5_3_OR_NEWER
using UnityEngine.TestTools.Constraints;
using UnityIs = UnityEngine.TestTools.Constraints.Is;
#endif

namespace TacticalDirector.DeterministicSim
{
    [TestFixture]
    public sealed class SnapshotDigestBackendTests
    {
        // Payload sizes straddling the SHA-256 64-byte block / 56-byte length-field boundaries once
        // the 78-byte header + tag prefix is added, plus a real match's payload and the capacity.
        private static readonly int[] PayloadSizes =
        {
            0, 1, 41, 42, 49, 50, 106, 113, 114, 1000, 35261, DeterministicSimConstants.MaxSnapshotBytes,
        };

        // SplitMix64 (#16 §3.4.4) so the fixture bytes are reproducible on every host.
        private static ulong NextSplitMix64(ref ulong state)
        {
            unchecked
            {
                state += 0x9E3779B97F4A7C15UL; // Spec #16 §3.4.4
                ulong z = state;
                z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
                z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
                return z ^ (z >> 31);
            }
        }

        private static void Fill(byte[] buffer, int count, ulong seed)
        {
            ulong state = seed;
            for (int i = 0; i < count; i++)
            {
                buffer[i] = (byte)NextSplitMix64(ref state);
            }
        }

        // The §3.2.3 preimage written out by hand, independently of SnapshotCodec.
        private static byte[] ReferenceDigest(SnapshotHeader header, SnapshotPayload payload)
        {
            byte[] envFp = header.Fingerprint.ComputeDigest();
            var preimage = new byte[1 + 4 + 8 + 32 + 32 + 1 + payload.BytesWritten];
            int o = 0;
            preimage[o++] = 0x12;
            for (int i = 0; i < 4; i++) { preimage[o++] = (byte)(header.SchemaVersion >> (8 * i)); }
            for (int i = 0; i < 8; i++) { preimage[o++] = (byte)(header.Tick >> (8 * i)); }
            Array.Copy(header.PrevSnapshotDigest, 0, preimage, o, 32);
            o += 32;
            Array.Copy(envFp, 0, preimage, o, 32);
            o += 32;
            preimage[o++] = 0x11;
            Array.Copy(payload.PayloadBytes, 0, preimage, o, payload.BytesWritten);

            using (SHA256 sha = SHA256.Create())
            {
                return sha.ComputeHash(preimage);
            }
        }

        private static SnapshotHeader NewHeader(ulong tick)
        {
            var header = new SnapshotHeader();
            header.Initialize(tick, null, EnvironmentFingerprint.CreateStage0Dev(), TestBuildIdentity.TestBuildHash);
            return header;
        }

        [Test]
        public void Encode_ChainedAcrossPaddingBoundaries_EqualsIndependentSha256OfThePreimage()
        {
            var codec = new SnapshotCodec();
            var payload = new SnapshotPayload();
            var expectedPrev = new byte[DeterministicSimConstants.SHA256_BYTES];

            for (int i = 0; i < PayloadSizes.Length; i++)
            {
                ulong tick = (ulong)(i + 1);
                SnapshotHeader header = NewHeader(tick);
                payload.Reset();
                Fill(payload.PayloadBytes, PayloadSizes[i], 0x5EED0000UL + tick);
                payload.BytesWritten = PayloadSizes[i];

                codec.Encode(header, payload);

                CollectionAssert.AreEqual(expectedPrev, header.PrevSnapshotDigest,
                    $"tick {tick}: the chain must thread the previous digest into the preimage.");
                CollectionAssert.AreEqual(ReferenceDigest(header, payload), header.CurrentSnapshotDigest,
                    $"tick {tick} (payload {PayloadSizes[i]} B): digest is not SHA-256 of the §3.2.3 preimage.");
                Assert.AreEqual(0, codec.ValidateCurrentDigest(header, payload),
                    $"tick {tick}: the validator must accept the record the encoder just wrote.");

                Array.Copy(header.CurrentSnapshotDigest, expectedPrev, DeterministicSimConstants.SHA256_BYTES);
            }
        }

        [Test]
        public void ValidateCurrentDigest_SingleFlippedPayloadByte_IsRejected()
        {
            var codec = new SnapshotCodec();
            var payload = new SnapshotPayload();
            Fill(payload.PayloadBytes, 35261, 0xF11FUL);
            payload.BytesWritten = 35261;
            SnapshotHeader header = NewHeader(9UL);
            codec.Encode(header, payload);

            payload.PayloadBytes[35260] ^= 0x01;

            Assert.AreEqual(DeterministicSimConstants.ERR_DS_SNAPSHOT_DIGEST_MISMATCH,
                new SnapshotCodec().ValidateCurrentDigest(header, payload));
        }

        [Test]
        public void CngSha256_OnWindows_IsActive_AndAgreesWithManagedSha256()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Assert.IsFalse(CngSha256.IsAvailable, "CNG must never be selected off Windows.");
                return;
            }

            // The pinned host is Windows 11: losing CNG there silently restores the managed digest
            // that made 10× playback unreachable (Gate-J J-05), so its absence is a failure.
            Assert.IsTrue(CngSha256.IsAvailable, "CNG SHA-256 must be active on a Windows host.");

            var input = new byte[70000];
            Fill(input, input.Length, 0xC0DEUL);
            var native = new byte[DeterministicSimConstants.SHA256_BYTES];
            using (SHA256 managed = SHA256.Create())
            {
                for (int length = 0; length <= 300; length++)
                {
                    Assert.IsTrue(CngSha256.TryHash(input, length, native));
                    CollectionAssert.AreEqual(managed.ComputeHash(input, 0, length), native, $"length {length}");
                }
                foreach (int length in new[] { 35261, 65613, 70000 })
                {
                    Assert.IsTrue(CngSha256.TryHash(input, length, native));
                    CollectionAssert.AreEqual(managed.ComputeHash(input, 0, length), native, $"length {length}");
                }
            }
        }

        [Test]
        public void Encode_SteadyState_AllocatesNothing()
        {
            var codec = new SnapshotCodec();
            var payload = new SnapshotPayload();
            Fill(payload.PayloadBytes, 35261, 0xA110CUL);
            payload.BytesWritten = 35261;
            SnapshotHeader header = NewHeader(1UL);
            codec.Encode(header, payload); // first call creates the managed hasher where CNG is absent

            TestDelegate encode = () =>
            {
                for (int i = 0; i < 20; i++) { codec.Encode(header, payload); }
            };
            encode();

#if UNITY_5_3_OR_NEWER
            // Mono's managed byte counter is not the Editor oracle; use the native GC recorder.
            TestDelegate control = () => GC.KeepAlive(new byte[1024]);
            Assert.That(control, UnityIs.AllocatingGCMemory(),
                "Unity allocation recorder must detect a known live allocation.");
            Assert.That(encode, UnityIs.Not.AllocatingGCMemory());
#else
            // A stubbed/unsupported runtime counter must never certify the zero-allocation assertion.
            long controlBefore = GC.GetAllocatedBytesForCurrentThread();
            byte[] controlBuffer = new byte[1024];
            long controlBytes = GC.GetAllocatedBytesForCurrentThread() - controlBefore;
            GC.KeepAlive(controlBuffer);
            Assert.Greater(controlBytes, 0L, "Allocation counter must detect a known live allocation on this runtime.");
            // .NET's tiered JIT allocates on the calling thread at unpredictable points while a hot
            // method is promoted, so a single window can carry a one-off charge. A per-call
            // allocation cannot produce a zero window, so the cleanest of several decides.
            long fewest = long.MaxValue;
            for (int round = 0; round < 8; round++)
            {
                long before = GC.GetAllocatedBytesForCurrentThread();
                encode();
                fewest = Math.Min(fewest, GC.GetAllocatedBytesForCurrentThread() - before);
            }
            Assert.AreEqual(0L, fewest, "Encode allocated in every window; before J-05 it copied the whole payload every tick.");
#endif
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-10-21 | —      | Initial creation (Gate-J J-05 / P1b E3): independent preimage   |
// |         |            |        | re-derivation across a chain and SHA-256 padding boundaries,    |
// |         |            |        | flipped-byte rejection, CNG active on Windows and equal to the   |
// |         |            |        | managed SHA-256, and steady-state zero allocation of Encode.    |
#endregion
