// File:     src/match-engine/tests/MatchEngineSnapshotDigestReferenceTests.cs
// Created:  2026-10-21
// Modified: 2026-10-21
// Author:   —
// Spec:     Deterministic Simulation #16 §3.2.3, Match Engine design note §5 Phase A, Code Standards #20;
//           docs/tracking/perception-shared-pass-design.md §5 E3 (P1b)
// Purpose:  Gate-J J-05 lock at engine level. Over a real match stretch (AI strides, perception
//           heartbeats, possession changes, ledger records), every tick's CurrentSnapshotDigest must be
//           SHA-256 of 0x12 ‖ schema ‖ tick ‖ prevDigest ‖ envFp ‖ 0x11 ‖ the engine's own committed
//           payload, re-derived here with System.Security.Cryptography, and must chain off the previous
//           tick. This pins that the faster digest backend (CNG / reused hasher) hashes exactly what
//           the engine serialized. Off Unity it also bounds RunTick's steady-state allocation, which
//           was ~35 KB per tick before J-05 (the digest copied the whole payload).

using System;
using System.Security.Cryptography;

using NUnit.Framework;

using TacticalDirector.DeterministicSim;

namespace TacticalDirector.MatchEngine
{
    [TestFixture]
    public sealed class MatchEngineSnapshotDigestReferenceTests
    {
        private const ulong MatchSeed = 0x0F1E2D3C4B5A6978UL;

        // 15 s of play: 90 AI strides / perception heartbeats and the opening possession phase.
        private const int Ticks = 900;

        // Before J-05 RunTick allocated ~35,600 B per tick; afterwards only rare diagnostic log
        // strings remain. The bound is two orders of magnitude under the old cost, not a zero
        // claim — SnapshotDigestBackendTests holds the codec itself to zero.
        private const long MaxMeanBytesPerTick = 256;

        private static byte[] ReferenceDigest(SnapshotHeader header, SnapshotPayload payload)
        {
            byte[] envFp = header.Fingerprint.ComputeDigest();
            var preimage = new byte[1 + 4 + 8 + 32 + 32 + 1 + payload.BytesWritten];
            int o = 0;
            preimage[o++] = DeterministicSimConstants.DOMAIN_TAG_SNAPSHOT_HEADER;
            for (int i = 0; i < 4; i++) { preimage[o++] = (byte)(header.SchemaVersion >> (8 * i)); }
            for (int i = 0; i < 8; i++) { preimage[o++] = (byte)(header.Tick >> (8 * i)); }
            Array.Copy(header.PrevSnapshotDigest, 0, preimage, o, DeterministicSimConstants.SHA256_BYTES);
            o += DeterministicSimConstants.SHA256_BYTES;
            Array.Copy(envFp, 0, preimage, o, DeterministicSimConstants.SHA256_BYTES);
            o += DeterministicSimConstants.SHA256_BYTES;
            preimage[o++] = DeterministicSimConstants.DOMAIN_TAG_SNAPSHOT_PAYLOAD;
            Array.Copy(payload.PayloadBytes, 0, preimage, o, payload.BytesWritten);

            using (SHA256 sha = SHA256.Create())
            {
                return sha.ComputeHash(preimage);
            }
        }

        [Test]
        public void RunTick_EveryDigest_IsSha256OfTheCommittedPreimage_AndChains()
        {
            var engine = new MatchEngine(MatchSeed);
            engine.EnableGkHeading();
            var previous = new byte[DeterministicSimConstants.SHA256_BYTES];

            for (int t = 1; t <= Ticks; t++)
            {
                engine.RunTick();
                SnapshotHeader header = engine.CaptureCommittedSnapshotHeader();
                SnapshotPayload payload = engine.CaptureCommittedSnapshotPayload();

                Assert.AreEqual((ulong)t, header.Tick);
                CollectionAssert.AreEqual(previous, header.PrevSnapshotDigest, $"tick {t}: chain link broken.");
                byte[] digest = engine.CurrentSnapshotDigest;
                CollectionAssert.AreEqual(ReferenceDigest(header, payload), digest,
                    $"tick {t}: digest is not SHA-256 of the committed §3.2.3 preimage.");
                previous = digest;
            }
        }

#if !UNITY_5_3_OR_NEWER
        // Mono's managed byte counter is not reliable inside Unity; the codec-level native-recorder
        // assertion (SnapshotDigestBackendTests) covers the Unity side.
        [Test]
        public void RunTick_SteadyState_AllocationIsFarBelowThePreJ05PayloadCopy()
        {
            var engine = new MatchEngine(MatchSeed);
            engine.EnableGkHeading();
            for (int t = 0; t < 600; t++) { engine.RunTick(); }

            long controlBefore = GC.GetAllocatedBytesForCurrentThread();
            byte[] control = new byte[1024];
            long controlBytes = GC.GetAllocatedBytesForCurrentThread() - controlBefore;
            GC.KeepAlive(control);
            Assert.Greater(controlBytes, 0L, "Allocation counter must detect a known live allocation on this runtime.");

            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int t = 0; t < Ticks; t++) { engine.RunTick(); }
            long meanBytes = (GC.GetAllocatedBytesForCurrentThread() - before) / Ticks;

            Assert.LessOrEqual(meanBytes, MaxMeanBytesPerTick,
                $"RunTick allocated {meanBytes} B/tick on average after warm-up.");
        }
#endif
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-10-21 | —      | Initial creation (Gate-J J-05 / P1b E3): per-tick independent   |
// |         |            |        | digest re-derivation and chain link over 900 real ticks, and an |
// |         |            |        | off-Unity steady-state allocation bound for RunTick.            |
#endregion
