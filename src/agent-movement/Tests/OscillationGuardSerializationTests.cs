// File:     src/agent-movement/Tests/OscillationGuardSerializationTests.cs
// Created:  2026-06-16
// Author:   —
// Spec:     Agent Movement #2 §3.1.7; Match Engine design note §2.6 (Phase B step B0); Code Standards #20
// Purpose:  Locks the OscillationGuard GetState/RestoreState serialization seam (Phase B step B0):
//           the guard's private ring-buffer state survives a CanonicalSerializer byte round-trip
//           with bit-exact fidelity (incl. the float.NegativeInfinity empty-slot sentinel) and the
//           restored guard behaves identically to the original on the next transition. This is the
//           gating proof for the match-engine snapshot — without it, agent OscillationGuard state
//           could not be serialized canonically and save/restore replay would diverge.

using NUnit.Framework;

using TacticalDirector.DeterministicSim;

namespace TacticalDirector.AgentMovement.Tests
{
    /// <summary>
    /// Round-trip and behavioural-equivalence tests for the OscillationGuard B0 serialization seam.
    /// </summary>
    [TestFixture]
    internal sealed class OscillationGuardSerializationTests
    {
        // 8 timestamps (f32) + writeIndex (i32) + isLocked (bool) + lockUntilTime (f32).
        private const int SerializedBytes = OscillationGuardState.TimestampSlots * 4 + 4 + 1 + 4;

        // ── Serialization helpers (the canonical field order the match-engine snapshot will use) ──

        private static byte[] Serialize(in OscillationGuardState s)
        {
            byte[] buf = new byte[SerializedBytes];
            int o = 0;
            CanonicalSerializer.WriteF32(buf, ref o, s.T0);
            CanonicalSerializer.WriteF32(buf, ref o, s.T1);
            CanonicalSerializer.WriteF32(buf, ref o, s.T2);
            CanonicalSerializer.WriteF32(buf, ref o, s.T3);
            CanonicalSerializer.WriteF32(buf, ref o, s.T4);
            CanonicalSerializer.WriteF32(buf, ref o, s.T5);
            CanonicalSerializer.WriteF32(buf, ref o, s.T6);
            CanonicalSerializer.WriteF32(buf, ref o, s.T7);
            CanonicalSerializer.WriteI32(buf, ref o, s.WriteIndex);
            CanonicalSerializer.WriteBool(buf, ref o, s.IsLocked);
            CanonicalSerializer.WriteF32(buf, ref o, s.LockUntilTime);
            return buf;
        }

        private static OscillationGuardState Deserialize(byte[] buf)
        {
            int o = 0;
            float t0 = CanonicalSerializer.ReadF32(buf, ref o);
            float t1 = CanonicalSerializer.ReadF32(buf, ref o);
            float t2 = CanonicalSerializer.ReadF32(buf, ref o);
            float t3 = CanonicalSerializer.ReadF32(buf, ref o);
            float t4 = CanonicalSerializer.ReadF32(buf, ref o);
            float t5 = CanonicalSerializer.ReadF32(buf, ref o);
            float t6 = CanonicalSerializer.ReadF32(buf, ref o);
            float t7 = CanonicalSerializer.ReadF32(buf, ref o);
            int writeIndex = CanonicalSerializer.ReadI32(buf, ref o);
            bool isLocked = CanonicalSerializer.ReadBool(buf, ref o);
            float lockUntil = CanonicalSerializer.ReadF32(buf, ref o);
            return new OscillationGuardState(t0, t1, t2, t3, t4, t5, t6, t7, writeIndex, isLocked, lockUntil);
        }

        /// <summary>Round-trips a guard's state through serialize → deserialize → RestoreState and
        /// asserts the restored guard re-serializes to identical bytes.</summary>
        private static void AssertByteRoundTrip(in OscillationGuard original)
        {
            byte[] before = Serialize(original.GetState());

            OscillationGuardState restoredState = Deserialize(before);
            OscillationGuard restored = default;
            restored.RestoreState(restoredState);

            byte[] after = Serialize(restored.GetState());
            Assert.AreEqual(before, after, "OscillationGuard state did not survive the CanonicalSerializer round-trip byte-for-byte.");
        }

        // ── B0-001: a freshly initialised guard (all -Infinity sentinels) round-trips exactly ──
        [Test]
        public void Initialized_AllNegativeInfinitySentinels_RoundTripsExactly()
        {
            OscillationGuard g = default;
            g.Initialize();

            OscillationGuardState s = g.GetState();
            // Guard precondition: the empty-slot sentinel is -Infinity, the bit pattern WriteF32 must preserve.
            Assert.IsTrue(float.IsNegativeInfinity(s.T0), "Initialize did not seed slot 0 to -Infinity.");
            Assert.IsTrue(float.IsNegativeInfinity(s.LockUntilTime), "Initialize did not seed lockUntilTime to -Infinity.");

            AssertByteRoundTrip(g);
        }

        // ── B0-002: a populated-but-unlocked guard round-trips exactly ──
        [Test]
        public void Populated_Unlocked_RoundTripsExactly()
        {
            OscillationGuard g = default;
            g.Initialize();

            // Transitions spaced > WindowSeconds apart never accumulate enough recent hits to lock,
            // leaving finite timestamps in the first three slots, writeIndex = 3, isLocked = false.
            Assert.IsFalse(g.RecordAndCheck(0.0f));
            Assert.IsFalse(g.RecordAndCheck(2.0f));
            Assert.IsFalse(g.RecordAndCheck(4.0f));

            OscillationGuardState s = g.GetState();
            Assert.AreEqual(3, s.WriteIndex, "Expected three transitions recorded.");
            Assert.IsFalse(s.IsLocked, "Spaced transitions should not have locked the guard.");

            AssertByteRoundTrip(g);
        }

        // ── B0-003: a locked guard round-trips exactly ──
        [Test]
        public void Locked_RoundTripsExactly()
        {
            OscillationGuard g = default;
            g.Initialize();

            // Rapid transitions within one window exceed MaxTransitionsPerSecond and lock the guard.
            // Loop to a safe cap rather than hardcoding the threshold.
            bool locked = false;
            float t = 0.0f;
            for (int i = 0; i < 32 && !locked; i++)
            {
                locked = g.RecordAndCheck(t);
                t += 0.01f;
            }

            Assert.IsTrue(locked, "Rapid transitions should have locked the guard.");
            Assert.IsTrue(g.GetState().IsLocked, "Guard reports unlocked after a lock-triggering transition.");

            AssertByteRoundTrip(g);
        }

        // ── B0-004: a restored guard behaves identically to the original on the next transition ──
        [Test]
        public void Restored_BehavesIdenticallyToOriginal_OnNextTransition()
        {
            OscillationGuard original = default;
            original.Initialize();
            original.RecordAndCheck(0.0f);
            original.RecordAndCheck(0.05f);
            original.RecordAndCheck(0.10f);

            // Restore a fresh guard to the same state via the serialization seam.
            OscillationGuard restored = default;
            restored.RestoreState(Deserialize(Serialize(original.GetState())));

            // Drive both with the identical next input; output AND resulting state must match.
            const float nextTime = 0.12f;
            bool originalBlocked = original.RecordAndCheck(nextTime);
            bool restoredBlocked = restored.RecordAndCheck(nextTime);

            Assert.AreEqual(originalBlocked, restoredBlocked, "Restored guard returned a different block decision.");
            Assert.AreEqual(Serialize(original.GetState()), Serialize(restored.GetState()),
                "Restored guard diverged from the original after an identical transition.");
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-06-16 | —      | Initial implementation — Phase B step B0 OscillationGuard serialization-seam round-trip locks. |
#endregion
