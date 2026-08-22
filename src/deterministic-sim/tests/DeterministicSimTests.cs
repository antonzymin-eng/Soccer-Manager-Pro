// File:     src/deterministic-sim/tests/DeterministicSimTests.cs
// Created:  2026-05-29
// Modified: 2026-06-16 (Match Engine Phase B step B1: MatchClock seconds-clock + FrameSeconds coverage)
// Author:   —
// Spec:     Deterministic Simulation #16 §5 (T-DS-ORDER-001..T-DS-FAULT-014), §9.5 (golden vectors),
//           Code Standards #20
// Purpose:  Test suite covering: golden-vector byte-exact validation (HKDF-SHA256, SipHash-2-4-64,
//           canonical serialization), replay lifecycle 8-step contracts, RNG branch-safety, snapshot
//           round-trip idempotence, and fault-injection error code coverage.

using System;
using System.IO;
using System.Reflection;

using NUnit.Framework;

namespace TacticalDirector.DeterministicSim
{
    /// <summary>
    /// Test coverage for Deterministic Simulation #16.
    /// All golden-vector tests compare byte-exact against the corpus in §9.5 / Appendix A.
    /// §5 test card identifiers are noted in each test method's summary.
    /// </summary>
    [TestFixture]
    /// <summary>
    /// A stand-in #16 §2.3.2 buildHash for snapshot headers built inside this assembly.
    /// `deterministic-sim` is a cross-cutting foundation and cannot name a real authoritative closure —
    /// the composition root does that (`MatchEngineBuildIdentity`) — so these headers carry this
    /// assembly's own identity, which is enough to satisfy FR-DS-014's non-empty requirement.
    /// </summary>
    internal static class TestBuildIdentity
    {
        internal static readonly string TestBuildHash =
            BuildIdentity.ComputeHash(new[] { typeof(BuildIdentity).Assembly });
    }

    public sealed class DeterministicSimTests
    {

        // ══════════════════════════════════════════════════════════════════════════════
        // HKDF-SHA256 golden vectors (§9.5 #4 / hkdf-sha256-kat.md v1.1)
        // ══════════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// RFC 5869 Appendix A.1 Test Case 1 (corrected per F-HKDF-01).
        /// IKM=0x0b×22, salt=0x000102...0c (13 bytes), info=0xf0f1...f9 (10 bytes), L=42.
        /// OKM first 16 bytes validated byte-exact.
        /// §9.5 / hkdf-sha256-kat.md T-HKDF-01.
        /// </summary>
        [Test]
        public void HkdfSha256_RfcTestCase1_ByteExact()
        {
            byte[] ikm  = new byte[22]; for (int i = 0; i < 22; i++) ikm[i]  = 0x0b;
            byte[] salt = new byte[13]; for (int i = 0; i < 13; i++) salt[i] = (byte)i;
            byte[] info = new byte[10]; for (int i = 0; i < 10; i++) info[i] = (byte)(0xf0 + i);

            byte[] okm = DeterministicRngService.HkdfSha256(ikm, salt, info, 16);

            // RFC 5869 §A.1 OKM first 16 bytes (F-HKDF-01 corrected value)
            byte[] expected = { 0x3c, 0xb2, 0x5f, 0x25, 0xfa, 0xac, 0xd5, 0x7a,
                                 0x90, 0x43, 0x4f, 0x64, 0xd0, 0x36, 0x2f, 0x2a };

            CollectionAssert.AreEqual(expected, okm,
                "HKDF-SHA256 RFC A.1 OKM mismatch — golden vector T-HKDF-01");
        }

        // ══════════════════════════════════════════════════════════════════════════════
        // SipHash-2-4-64 golden vectors (§9.5 #4 / siphash-2-4-kat.md v1.1)
        // First 8 of the 64 Aumasson & Bernstein 2012 Appendix A reference vectors.
        // ══════════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// SipHash-2-4-64 reference vectors 0–7 (Appendix A, 64 total; all verified).
        /// k = 00 01 02 ... 0f (16 bytes); messages are sequential byte sequences.
        /// §9.5 / siphash-2-4-kat.md.
        /// </summary>
        [Test]
        public void SipHash24_ReferenceVectors_ByteExact()
        {
            ulong k0 = 0x0706050403020100UL;
            ulong k1 = 0x0f0e0d0c0b0a0908UL;

            // 8 vectors from Aumasson & Bernstein 2012 Appendix A (0-indexed; message = 0..i-1)
            ulong[] expected = {
                0x726fdb47dd0e0e31UL, // msg = [] (empty)
                0x74f839c593dc67fdUL, // msg = [00]
                0x0d6c8009d9a94f5aUL, // msg = [00 01]
                0x85676696d7fb7e2dUL, // msg = [00 01 02]
                // Vectors 4-7 corrected on the first-ever suite execution (dotnet CI
                // gate): the original literals matched NO published source — vectors 0-3
                // were correct, 4-7 fabricated. Values below re-derived from an
                // independent Python mirror of the Aumasson & Bernstein reference
                // implementation and byte-identical to siphash-2-4-kat.md rows 4-7
                // (which SipHash24KatTests already locks for all 64 lengths).
                0xcf2794e0277187b7UL, // msg = [00 01 02 03]
                0x18765564cd99a68dUL, // msg = [00 01 02 03 04]
                0xcbc9466e58fee3ceUL, // msg = [00 01 02 03 04 05]
                0xab0200f58b01d137UL, // msg = [00 01 02 03 04 05 06]
            };

            for (int i = 0; i < expected.Length; i++)
            {
                byte[] msg = new byte[i];
                for (int j = 0; j < i; j++) msg[j] = (byte)j;

                ulong result = DeterministicRngService.SipHash24_64(k0, k1, msg);
                Assert.AreEqual(expected[i], result,
                    $"SipHash-2-4 vector {i} mismatch — golden vector siphash-2-4-kat.md");
            }
        }

        // ══════════════════════════════════════════════════════════════════════════════
        // Canonical serialization golden vectors (§9.5 #4 / serialize-canonical-corpus.md v1.0)
        // ══════════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// P-01: bool false → 0x00. §3.2.4.1 / corpus P-01.
        /// </summary>
        [Test]
        public void Serialize_BoolFalse_IsZero()
        {
            byte[] buf = new byte[1];
            int offset = 0;
            CanonicalSerializer.WriteBool(buf, ref offset, false);
            Assert.AreEqual(1, offset);
            Assert.AreEqual(0x00, buf[0], "P-01: bool false must serialize to 0x00");
        }

        /// <summary>
        /// P-02: bool true → 0x01. §3.2.4.1 / corpus P-02.
        /// </summary>
        [Test]
        public void Serialize_BoolTrue_IsOne()
        {
            byte[] buf = new byte[1];
            int offset = 0;
            CanonicalSerializer.WriteBool(buf, ref offset, true);
            Assert.AreEqual(0x01, buf[0], "P-02: bool true must serialize to 0x01");
        }

        /// <summary>
        /// P-03: u32 value 0x01020304 → little-endian [04 03 02 01]. §3.2.4.1 / corpus P-03.
        /// </summary>
        [Test]
        public void Serialize_U32_LittleEndian()
        {
            byte[] buf = new byte[4];
            int offset = 0;
            CanonicalSerializer.WriteU32(buf, ref offset, 0x01020304u);
            Assert.AreEqual(4, offset);
            Assert.AreEqual(0x04, buf[0], "P-03: LSB first");
            Assert.AreEqual(0x03, buf[1]);
            Assert.AreEqual(0x02, buf[2]);
            Assert.AreEqual(0x01, buf[3], "MSB last");
        }

        /// <summary>
        /// P-04: u64 value 0x0102030405060708 → little-endian [08 07 06 05 04 03 02 01].
        /// §3.2.4.1 / corpus P-04.
        /// </summary>
        [Test]
        public void Serialize_U64_LittleEndian()
        {
            byte[] buf = new byte[8];
            int offset = 0;
            CanonicalSerializer.WriteU64(buf, ref offset, 0x0102030405060708UL);
            Assert.AreEqual(8, offset);
            Assert.AreEqual(0x08, buf[0], "P-04: LSB first");
            Assert.AreEqual(0x01, buf[7], "MSB last");
        }

        /// <summary>
        /// F-01: -0.0f → +0.0f normalization (bit pattern 0x00000000). §3.2.4.1 / corpus F-01.
        /// </summary>
        [Test]
        public void Serialize_NegativeZeroFloat_NormalizesToPositiveZero()
        {
            byte[] buf = new byte[4];
            int offset = 0;
            CanonicalSerializer.WriteF32(buf, ref offset, -0.0f);
            uint bits = (uint)buf[0] | ((uint)buf[1] << 8) | ((uint)buf[2] << 16) | ((uint)buf[3] << 24);
            Assert.AreEqual(0x00000000u, bits, "F-01: -0.0 must normalize to +0.0");
        }

        /// <summary>
        /// F-05: PHYSICS_DT bit pattern = 0x3C888889. §3.2.4.1 / corpus F-05.
        /// Verifies that (float)(1.0/60.0) matches the pinned constant.
        /// </summary>
        [Test]
        public void PhysicsDt_BitPattern_MatchesPinnedConstant()
        {
            float dt = (float)(1.0 / 60.0);
            uint bits = CanonicalSerializer.SingleToUInt32Bits(dt);
            Assert.AreEqual(DeterministicSimConstants.PHYSICS_DT_BITS, bits,
                "F-05: PHYSICS_DT bit pattern must be 0x3C888889");
        }

        // ══════════════════════════════════════════════════════════════════════════════
        // T-DS-ORDER-001: identical seeds/inputs → identical phase digests
        // ══════════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// T-DS-ORDER-001: Two MatchClocks started with the same tick produce identical
        /// tick sequences after N advances. §5.
        /// </summary>
        [Test]
        public void MatchClock_SameSeed_IdenticalTicks()
        {
            var clockA = new MatchClock(0UL);
            var clockB = new MatchClock(0UL);

            for (int i = 0; i < 60; i++)
            {
                clockA.Advance();
                clockB.Advance();
                Assert.AreEqual(clockA.CurrentTick, clockB.CurrentTick,
                    $"T-DS-ORDER-001: tick divergence at step {i}");
            }
        }

        // ══════════════════════════════════════════════════════════════════════════════
        // T-DS-RNG-002: Reserve/DrawReserved/CloseReservation branch parity
        // ══════════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// T-DS-RNG-002: A branch that draws 2 out of 4 reserved and one that draws all 4
        /// both end at the same RngCursor (cursor advances by declared budget). §5 / §3.2.5.
        /// </summary>
        [Test]
        public void RngService_BranchCursorParity()
        {
            const ulong matchSeed = 0xDEADBEEFCAFE0000UL;

            var rngA = new DeterministicRngService(matchSeed);
            var rngB = new DeterministicRngService(matchSeed);

            int idxA = rngA.RegisterStream("AI.DecidePass", SubsystemOrdinals.DecisionTree, 1, 0);
            int idxB = rngB.RegisterStream("AI.DecidePass", SubsystemOrdinals.DecisionTree, 1, 0);

            // Branch A: reserve 4, draw 2, close
            rngA.Reserve(idxA, 4);
            rngA.DrawReserved(idxA, 0, out _);
            rngA.DrawReserved(idxA, 1, out _);
            rngA.CloseReservation(idxA);

            // Branch B: reserve 4, draw all 4, close
            rngB.Reserve(idxB, 4);
            rngB.DrawReserved(idxB, 0, out _);
            rngB.DrawReserved(idxB, 1, out _);
            rngB.DrawReserved(idxB, 2, out _);
            rngB.DrawReserved(idxB, 3, out _);
            rngB.CloseReservation(idxB);

            // Both cursors must be at the same position (= declared budget = 4)
            Assert.AreEqual(
                rngA.GetStreamState(idxA).RngCursor,
                rngB.GetStreamState(idxB).RngCursor,
                "T-DS-RNG-002: cursor must match after Reserve(4) regardless of draws made");
        }

        // ══════════════════════════════════════════════════════════════════════════════
        // T-DS-SNAP-003: SnapshotPayload byte round-trip
        // ══════════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// T-DS-SNAP-003: Serialize → Deserialize of a u64 field produces bit-exact identity. §5.
        /// </summary>
        [Test]
        public void Serialize_U64_RoundTrip()
        {
            byte[] buf = new byte[8];
            int wOffset = 0;
            ulong originalValue = 0xFEDCBA9876543210UL;
            CanonicalSerializer.WriteU64(buf, ref wOffset, originalValue);

            int rOffset = 0;
            ulong roundTripped = CanonicalSerializer.ReadU64(buf, ref rOffset);

            Assert.AreEqual(originalValue, roundTripped,
                "T-DS-SNAP-003: u64 round-trip must be bit-exact");
        }

        // ══════════════════════════════════════════════════════════════════════════════
        // T-DS-FAULT-008..014: error code fault injection coverage
        // ══════════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// T-DS-FAULT-009: ERR_DS_RNG_BUDGET_MISMATCH fires when Reserve is called with an
        /// already-open reservation. §5 / §3.4.
        /// </summary>
        [Test]
        public void RngService_DoubleReserve_ReturnsBudgetMismatch()
        {
            var rng = new DeterministicRngService(0xABCDEF0123456789UL);
            int idx = rng.RegisterStream("test.site", SubsystemOrdinals.BallPhysics, 0, 0);

            ushort err1 = rng.Reserve(idx, 4);
            Assert.AreEqual(0, err1, "First Reserve must succeed");

            ushort err2 = rng.Reserve(idx, 2);
            Assert.AreEqual(DeterministicSimConstants.ERR_DS_RNG_BUDGET_MISMATCH, err2,
                "T-DS-FAULT-009: double Reserve must return ERR_DS_RNG_BUDGET_MISMATCH");
        }

        /// <summary>
        /// T-DS-FAULT-010: ERR_DS_TIERA_NONFINITE fires when a Tier A float is NaN. §5.
        /// </summary>
        [Test]
        public void DivergenceDetector_TierA_NaN_TriggersNonFiniteError()
        {
            ushort errorCode;
            DivergenceClass result = DivergenceDetector.CompareTierAFloat(
                float.NaN, 1.0f, out errorCode);

            Assert.AreEqual(DivergenceClass.HardDesync, result,
                "T-DS-FAULT-010: NaN Tier A must return HardDesync");
            Assert.AreEqual(DeterministicSimConstants.ERR_DS_TIERA_NONFINITE, errorCode,
                "T-DS-FAULT-010: error code must be ERR_DS_TIERA_NONFINITE");
        }

        /// <summary>
        /// T-DS-FAULT-011: ERR_DS_TIERB_NONFINITE fires for non-canonical NaN in Tier B. §5.
        /// </summary>
        [Test]
        public void DivergenceDetector_TierB_NonCanonicalNaN_TriggersError()
        {
            // Non-canonical NaN: any NaN that is not 0x7FC00000
            uint nonCanonicalNaNBits = 0x7F800001u;
            float nonCanonicalNaN = CanonicalSerializer.UInt32BitsToSingle(nonCanonicalNaNBits);

            ushort errorCode;
            DivergenceClass result = DivergenceDetector.CompareTierBFloat(
                nonCanonicalNaN, 1.0f, 0.001f, out errorCode);

            Assert.AreEqual(DivergenceClass.HardDesync, result,
                "T-DS-FAULT-011: non-canonical NaN Tier B must return HardDesync");
            Assert.AreEqual(DeterministicSimConstants.ERR_DS_TIERB_NONFINITE, errorCode,
                "T-DS-FAULT-011: error code must be ERR_DS_TIERB_NONFINITE");
        }

        /// <summary>
        /// T-DS-FAULT-012: ERR_DS_DIGEST_CHAIN_BREAK fires when prevDigest mismatches. §5.
        /// </summary>
        [Test]
        public void SnapshotCodec_DigestChainBreak_ReturnsError()
        {
            var codec = new SnapshotCodec();

            var header = new SnapshotHeader();
            header.Initialize(1UL, null, EnvironmentFingerprint.CreateStage0Dev(), TestBuildIdentity.TestBuildHash);

            // Corrupt the stored prev digest by writing non-zero bytes into PrevSnapshotDigest
            for (int i = 0; i < DeterministicSimConstants.SHA256_BYTES; i++)
            {
                header.PrevSnapshotDigest[i] = 0xFF;
            }

            ushort err = codec.ValidatePrevDigest(header);
            Assert.AreEqual(DeterministicSimConstants.ERR_DS_DIGEST_CHAIN_BREAK, err,
                "T-DS-FAULT-012: mismatched prevDigest must return ERR_DS_DIGEST_CHAIN_BREAK");
        }

        /// <summary>
        /// T-DS-FAULT-013: ERR_DS_REPLAY_ENV_MISMATCH fires when fingerprints differ. §5.
        /// </summary>
        [Test]
        public void EnvironmentFingerprint_WorkerCountMismatch_ReturnsEnvMismatch()
        {
            var recorded = EnvironmentFingerprint.CreateStage0Dev();
            var live = new EnvironmentFingerprint(
                workerCount:               2, // differs — the only field that should differ from CreateStage0Dev
                schedulerPolicy:           "Stage0-SingleThread-v1",
                reductionTopology:         "Serial",
                simdFeatureLevel:          "SSE4.2",
                floatModelHash:            EnvironmentFingerprint.FloatModelHashDevPlaceholder,
                unicodeNormalizationVersion: DeterministicSimConstants.UNICODE_NFC_VERSION);

            ushort err = recorded.ValidateAgainst(live);
            Assert.AreEqual(DeterministicSimConstants.ERR_DS_REPLAY_ENV_MISMATCH, err,
                "T-DS-FAULT-013: workerCount mismatch must return ERR_DS_REPLAY_ENV_MISMATCH");
        }

        /// <summary>
        /// ERR-016-006: the Stage-0 dev fingerprint is flagged as a placeholder (its floatModelHash is
        /// the sentinel, not a real §4.8.3 hash) and its simdFeatureLevel matches the pinned SSE4.2
        /// baseline (was "SSE2", which matched no pin). A genuine (non-placeholder) fingerprint reports
        /// IsDevPlaceholder == false.
        /// </summary>
        [Test]
        public void EnvironmentFingerprint_Stage0Dev_IsPlaceholder_AndMatchesPinnedSimdLevel()
        {
            var dev = EnvironmentFingerprint.CreateStage0Dev();
            Assert.IsTrue(dev.IsDevPlaceholder,
                "ERR-016-006: CreateStage0Dev must be flagged as a non-certification placeholder");
            Assert.AreEqual(EnvironmentFingerprint.FloatModelHashDevPlaceholder, dev.FloatModelHash);
            Assert.AreEqual("SSE4.2", dev.SimdFeatureLevel,
                "ERR-016-006: the dev fingerprint's SIMD level must match the pinned SSE4.2 baseline");

            var genuine = new EnvironmentFingerprint(
                workerCount:               1,
                schedulerPolicy:           "Stage0-SingleThread-v1",
                reductionTopology:         "Serial",
                simdFeatureLevel:          "SSE4.2",
                floatModelHash:            "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef",
                unicodeNormalizationVersion: DeterministicSimConstants.UNICODE_NFC_VERSION);
            Assert.IsFalse(genuine.IsDevPlaceholder,
                "ERR-016-006: a fingerprint carrying a real hash must not report IsDevPlaceholder");
        }

        /// <summary>
        /// ERR-016-006 (Option A): FloatFlagTuple.ComputeHash produces the §4.8.3 floatModelHash =
        /// SHA-256(SerializeCanonical(0x14 ‖ tuple)). Golden vector for the Stage-0 Mono tuple with a
        /// test compilerVersion, independently computed by a Python mirror of CanonicalSerializer.
        /// </summary>
        [Test]
        public void FloatFlagTuple_Stage0Mono_ComputeHash_MatchesGoldenVector()
        {
            var tuple = new FloatFlagTuple(
                compilerToolchain: EnvironmentFingerprint.Stage0MonoToolchain,
                compilerVersion:   "6.13.0", // test input, not a claim about the real host
                targetTriple:      EnvironmentFingerprint.Stage0MonoTargetTriple,
                il2cppVersion:     EnvironmentFingerprint.Stage0MonoIl2cppSentinel,
                denormalsAreZero:  false, flushToZero: false,
                roundingMode:      0, fpContractMode: 0,
                fmaEnabled:        false, fastMath: false,
                simdLevel:         EnvironmentFingerprint.Stage0SimdLevel);

            Assert.AreEqual(
                "89f50a313db7544e78942b6c7cb62ee736d9eb2ee863feb2abbd175309f343e7",
                tuple.ComputeHash());
        }

        /// <summary>
        /// ERR-016-006: ComputeHash is deterministic and sensitive to every field (a version change and a
        /// float-mode flag flip both change the hash).
        /// </summary>
        [Test]
        public void FloatFlagTuple_ComputeHash_IsDeterministicAndSensitive()
        {
            FloatFlagTuple Tuple(string ver, bool daz) => new FloatFlagTuple(
                "Mono", ver, "win-x64", "MONO", daz, false, 0, 0, false, false, "SSE4.2");

            Assert.AreEqual(Tuple("6.13.0", false).ComputeHash(), Tuple("6.13.0", false).ComputeHash(),
                "same tuple ⇒ same hash");
            Assert.AreNotEqual(Tuple("6.13.0", false).ComputeHash(), Tuple("6.14.0", false).ComputeHash(),
                "a compilerVersion change must change the hash");
            Assert.AreNotEqual(Tuple("6.13.0", false).ComputeHash(), Tuple("6.13.0", true).ComputeHash(),
                "a float-mode flag flip must change the hash");
        }

        /// <summary>
        /// ERR-016-006 (Option A): CreateStage0MonoCertified builds a NON-placeholder fingerprint carrying
        /// the real §4.8.3 hash, and rejects a missing host-supplied Mono version rather than inventing one.
        /// </summary>
        [Test]
        public void EnvironmentFingerprint_Stage0MonoCertified_CarriesRealHash_NotPlaceholder()
        {
            var fp = EnvironmentFingerprint.CreateStage0MonoCertified("6.13.0");
            Assert.IsFalse(fp.IsDevPlaceholder, "a certified Mono fingerprint must not report IsDevPlaceholder");
            Assert.AreEqual(
                "89f50a313db7544e78942b6c7cb62ee736d9eb2ee863feb2abbd175309f343e7", fp.FloatModelHash);
            Assert.AreEqual("SSE4.2", fp.SimdFeatureLevel);
            Assert.Throws<ArgumentException>(() => EnvironmentFingerprint.CreateStage0MonoCertified(null));
            Assert.Throws<ArgumentException>(() => EnvironmentFingerprint.CreateStage0MonoCertified(string.Empty));
        }

        /// <summary>
        /// T-DS-FAULT-014: ERR_DS_REPLAY_BOUNDARY fires when ReplayCursor is not at EndOfSnapshot.
        /// §5 / §4.2.2 step 7.
        /// </summary>
        [Test]
        public void ReplayCursor_NotAtEndOfSnapshot_IsAtEndOfSnapshotReturnsFalse()
        {
            var cursor = new ReplayCursor(tick: 100UL, phaseOrdinal: 3); // Physics phase (ordinal 3), not Snapshot
            Assert.IsFalse(cursor.IsAtEndOfSnapshot,
                "T-DS-FAULT-014: cursor at phase 3 (Physics) must not satisfy IsAtEndOfSnapshot");
        }

        /// <summary>
        /// Positive test: ReplayCursor at EndOfSnapshot[T] satisfies IsAtEndOfSnapshot. §4.2.2 step 7.
        /// </summary>
        [Test]
        public void ReplayCursor_AtEndOfSnapshot_IsAtEndOfSnapshotReturnsTrue()
        {
            var cursor = ReplayCursor.EndOfSnapshot(tick: 200UL);
            Assert.IsTrue(cursor.IsAtEndOfSnapshot,
                "ReplayCursor.EndOfSnapshot must satisfy IsAtEndOfSnapshot");
        }

        // ══════════════════════════════════════════════════════════════════════════════
        // MatchClock AI stride gating
        // ══════════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// MatchClock.IsAiStrideTick fires exactly every AI_PHASE_STRIDE ticks. §3.1.2.
        /// </summary>
        [Test]
        public void MatchClock_IsAiStrideTick_FiresEveryStride()
        {
            var clock = new MatchClock(0UL);
            int aiTickCount = 0;

            for (int i = 0; i < DeterministicSimConstants.AI_PHASE_STRIDE * 3; i++)
            {
                clock.Advance();
                if (clock.IsAiStrideTick)
                {
                    aiTickCount++;
                }
            }

            Assert.AreEqual(3, aiTickCount,
                "IsAiStrideTick must fire exactly 3 times in 3 × AI_PHASE_STRIDE ticks");
        }

        // ══════════════════════════════════════════════════════════════════════════════
        // Derived constants
        // ══════════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// AI_PHASE_STRIDE must be 6 (60 Hz / 10 Hz). §3.1.2 / §3.4.
        /// </summary>
        [Test]
        public void Constants_AiPhaseStride_Is6()
        {
            Assert.AreEqual(6, DeterministicSimConstants.AI_PHASE_STRIDE,
                "AI_PHASE_STRIDE must equal PHYSICS_TICK_HZ / TACTICAL_TICK_HZ = 6");
        }

        /// <summary>
        /// FrameSeconds is FrameMs / 1000 — the per-tick dt / seconds-clock step. §3.4.
        /// </summary>
        [Test]
        public void Constants_FrameSeconds_IsFrameMsOverThousand()
        {
            Assert.AreEqual(
                DeterministicSimConstants.FrameMs / 1000.0f,
                DeterministicSimConstants.FrameSeconds,
                "FrameSeconds must equal FrameMs / 1000 (shared PHYSICS_TICK_HZ → FrameMs → FrameSeconds chain)");
        }

        // ══════════════════════════════════════════════════════════════════════════════
        // Match Engine Phase B step B1: MatchClock seconds-domain plumbing
        // ══════════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// MatchClock.CurrentMatchTimeSeconds advances by FrameSeconds per tick and equals
        /// CurrentTick × FrameSeconds — the seconds-domain clock OscillationGuard.WindowSeconds
        /// consumers must read (the silent 1000× ms↔s unit fix).
        /// </summary>
        [Test]
        public void MatchClock_CurrentMatchTimeSeconds_TracksTicks()
        {
            var clock = new MatchClock(0UL);
            Assert.AreEqual(0.0f, clock.CurrentMatchTimeSeconds,
                "At tick 0 the seconds clock must read 0.");

            // One full second of physics ticks lands the clock at exactly 1.0 s.
            for (int i = 0; i < DeterministicSimConstants.PHYSICS_TICK_HZ; i++)
            {
                clock.Advance();
            }

            Assert.AreEqual(
                (float)DeterministicSimConstants.PHYSICS_TICK_HZ * DeterministicSimConstants.FrameSeconds,
                clock.CurrentMatchTimeSeconds,
                "After PHYSICS_TICK_HZ ticks the seconds clock must equal CurrentTick × FrameSeconds.");
            Assert.AreEqual(1.0f, clock.CurrentMatchTimeSeconds, 1e-4f,
                "PHYSICS_TICK_HZ ticks is one match second.");
        }

        /// <summary>
        /// CurrentMatchTimeSeconds is the millisecond clock scaled by 1/1000 — the two clocks
        /// agree on the same instant so neither domain drifts from the other.
        /// </summary>
        [Test]
        public void MatchClock_SecondsAndMs_Agree()
        {
            var clock = new MatchClock(0UL);
            for (int i = 0; i < 37; i++)
            {
                clock.Advance();
            }

            Assert.AreEqual(
                clock.CurrentMatchTimeMs / 1000.0f,
                clock.CurrentMatchTimeSeconds, 1e-6f,
                "Seconds clock must equal the ms clock / 1000 at the same tick.");
        }

        /// <summary>
        /// DespawnLog: Append and ContainsEntity operate correctly; overflow returns false. §3.2.3.
        /// </summary>
        [Test]
        public void DespawnLog_AppendAndContains_Correct()
        {
            var log = new DespawnLog();
            Assert.IsFalse(log.ContainsEntity(42));

            bool appended = log.Append(new DespawnEntry(42, 10UL, 5UL, 100UL));
            Assert.IsTrue(appended);
            Assert.IsTrue(log.ContainsEntity(42));
            Assert.IsFalse(log.ContainsEntity(99));
            Assert.AreEqual(1, log.Count);
        }
    }

    /// <summary>
    /// Save/load equivalence tests for the §4.6.1.1 atomic save contract.
    /// Tests T-DS-004..008 per §5.3 / §5.5.2 sample protocol.
    /// </summary>
    [TestFixture]
    public sealed class DeterministicSimSaveLoadTests
    {
        // ══════════════════════════════════════════════════════════════════════════════
        // T-DS-REPLAY-004 / T-DS-004: save/load equivalence
        // ══════════════════════════════════════════════════════════════════════════════

        // A temp directory per test, deleted afterwards. The three save/load cards below were
        // `Assert.Ignore` stubs for "requires temp-directory fixture … activate when Stage 1 CI
        // infrastructure supports file I/O in EditMode tests". That premise was stale: the gate runs
        // plain NUnit on net8.0 and the sibling MatchSaveManagerTests has been doing real file I/O on
        // it for a month. Activated at ERR-016-010, because a record format nothing ever wrote or read
        // is exactly how this one came to contradict its own normative layout in four places.
        private sealed class TempDir : IDisposable
        {
            public string Path { get; }

            public TempDir(string label)
            {
                Path = System.IO.Path.Combine(
                    System.IO.Path.GetTempPath(), "td-savemanager-" + label + "-" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(Path);
            }

            public void Dispose()
            {
                try { Directory.Delete(Path, recursive: true); } catch (Exception) { }
            }
        }

        private static (SnapshotHeader, SnapshotPayload) EncodedSnapshotAt(ulong tick, byte payloadByte)
        {
            var header = new SnapshotHeader();
            header.Initialize(
                tick, null, EnvironmentFingerprint.CreateStage0Dev(), TestBuildIdentity.TestBuildHash);

            var payload = new SnapshotPayload();
            payload.PayloadBytes[0] = payloadByte;
            payload.BytesWritten    = 1;

            new SnapshotCodec().Encode(header, payload);
            return (header, payload);
        }

        /// <summary>
        /// T-DS-004: SnapshotCodec.Encode → SaveManager.CommitAtomic → SaveManager.Load →
        /// SnapshotCodec.ValidateHeader → digest matches. Executed against a real temp directory.
        /// Also the ERR-016-010 lock that the record carries what §3.9.2 and FR-DS-010 require: the
        /// EnvironmentFingerprint and the §2.3.2 buildHash survive the disk round-trip, so the §4.2.2
        /// step-3 environment check is a real check rather than a guaranteed fail-closed.
        /// §5.3 T-DS-REPLAY-004 / §5.5.2.
        /// </summary>
        [Test]
        public void SaveLoad_Encode_CommitAtomic_Load_ValidateHeader_DigestMatches()
        {
            using var dir = new TempDir("roundtrip");
            var manager  = new SaveManager(dir.Path);
            (SnapshotHeader header, SnapshotPayload payload) = EncodedSnapshotAt(42UL, 0x5A);

            Assert.AreEqual(0, manager.CommitAtomic(header, payload), "CommitAtomic must succeed.");

            var loadedHeader  = new SnapshotHeader();
            var loadedPayload = new SnapshotPayload();
            Assert.AreEqual(0, manager.Load(42UL, loadedHeader, loadedPayload), "Load must succeed.");

            Assert.AreEqual(0, new SnapshotCodec().ValidateHeader(loadedHeader),
                "The loaded header must pass §4.2.2 steps 1–2.");
            Assert.AreEqual(header.Tick, loadedHeader.Tick);
            Assert.AreEqual(header.Cursor.Tick, loadedHeader.Cursor.Tick);
            Assert.AreEqual(header.Cursor.PhaseOrdinal, loadedHeader.Cursor.PhaseOrdinal);
            CollectionAssert.AreEqual(header.PrevSnapshotDigest, loadedHeader.PrevSnapshotDigest);
            CollectionAssert.AreEqual(header.CurrentSnapshotDigest, loadedHeader.CurrentSnapshotDigest,
                "The digest computed before the write must survive the round-trip byte-exact.");
            Assert.AreEqual(payload.BytesWritten, loadedPayload.BytesWritten);
            Assert.AreEqual(0x5A, loadedPayload.PayloadBytes[0]);

            // ERR-016-010, the two gaps this landing closes.
            Assert.AreEqual(TestBuildIdentity.TestBuildHash, loadedHeader.BuildHash,
                "The §2.3.2 buildHash must survive the round-trip (FR-DS-014).");
            Assert.IsNotNull(loadedHeader.Fingerprint,
                "The EnvironmentFingerprint must survive the round-trip — FR-DS-010 requires it in " +
                "every snapshot header, and §3.9.2's normative layout has always listed it.");
            Assert.AreEqual(0,
                loadedHeader.Fingerprint.ValidateAgainst(EnvironmentFingerprint.CreateStage0Dev()),
                "The reconstructed fingerprint must validate against the live one.");
        }

        /// <summary>
        /// ERR-016-010: the reconstructed header is complete enough for the §4.2.2 step-3 environment
        /// check to PASS. Before this landing the loaded fingerprint was always null and step 3 could
        /// only ever fail closed, so a disk-loaded replay was unreachable by construction.
        /// </summary>
        [Test]
        public void SaveLoad_DiskLoadedHeader_PassesTheReplayEnvironmentCheck()
        {
            using var dir = new TempDir("replaygate");
            var manager = new SaveManager(dir.Path);
            (SnapshotHeader header, SnapshotPayload payload) = EncodedSnapshotAt(7UL, 0x01);
            Assert.AreEqual(0, manager.CommitAtomic(header, payload));

            var loadedHeader  = new SnapshotHeader();
            var loadedPayload = new SnapshotPayload();
            Assert.AreEqual(0, manager.Load(7UL, loadedHeader, loadedPayload));

            var engine = new ReplayEngine(
                new SnapshotCodec(),
                new DeterministicRngService(0xABCDEF0123456789UL),
                new MatchClock(0UL),
                EnvironmentFingerprint.CreateStage0Dev());

            Assert.AreEqual(0, engine.PrepareReplay(loadedHeader, loadedPayload),
                "A disk-loaded snapshot must pass the §4.2.2 lifecycle, environment check included.");
        }

        /// <summary>
        /// ERR-016-010: a record written under a FOREIGN environment must be refused at §4.2.2 step 3.
        /// The positive test above cannot distinguish "the check passed" from "the check is inert", so
        /// this is the half that proves the gate discriminates.
        /// </summary>
        [Test]
        public void SaveLoad_ForeignFingerprintOnDisk_FailsTheReplayEnvironmentCheck()
        {
            using var dir = new TempDir("foreignenv");
            var manager = new SaveManager(dir.Path);

            var header = new SnapshotHeader();
            header.Initialize(
                3UL,
                null,
                new EnvironmentFingerprint(
                    workerCount: 4, schedulerPolicy: "foreign", reductionTopology: "Tree",
                    simdFeatureLevel: "AVX2", floatModelHash: "deadbeef", unicodeNormalizationVersion: "9.0"),
                TestBuildIdentity.TestBuildHash);
            var payload = new SnapshotPayload();
            payload.PayloadBytes[0] = 0x01;
            payload.BytesWritten    = 1;
            new SnapshotCodec().Encode(header, payload);

            Assert.AreEqual(0, manager.CommitAtomic(header, payload));

            var loadedHeader  = new SnapshotHeader();
            var loadedPayload = new SnapshotPayload();
            Assert.AreEqual(0, manager.Load(3UL, loadedHeader, loadedPayload));

            var engine = new ReplayEngine(
                new SnapshotCodec(),
                new DeterministicRngService(0xABCDEF0123456789UL),
                new MatchClock(0UL),
                EnvironmentFingerprint.CreateStage0Dev());

            Assert.AreEqual(DeterministicSimConstants.ERR_DS_REPLAY_ENV_MISMATCH,
                engine.PrepareReplay(loadedHeader, loadedPayload),
                "A snapshot recorded under a different environment must be refused (EC-016-007).");
        }

        /// <summary>ERR-016-010: a null fingerprint round-trips as null (the KD-3 presence-flag
        /// contract) rather than being invented, and the replay gate then fails closed as before.</summary>
        [Test]
        public void SaveLoad_NullFingerprint_RoundTripsToNull()
        {
            using var dir = new TempDir("nullfp");
            var manager = new SaveManager(dir.Path);

            var header = new SnapshotHeader();
            header.Initialize(9UL, null, null, TestBuildIdentity.TestBuildHash);
            var payload = new SnapshotPayload();
            payload.PayloadBytes[0] = 0x02;
            payload.BytesWritten    = 1;
            new SnapshotCodec().Encode(header, payload);

            Assert.AreEqual(0, manager.CommitAtomic(header, payload));

            var loadedHeader  = new SnapshotHeader();
            var loadedPayload = new SnapshotPayload();
            Assert.AreEqual(0, manager.Load(9UL, loadedHeader, loadedPayload));
            Assert.IsNull(loadedHeader.Fingerprint);
            Assert.AreEqual(TestBuildIdentity.TestBuildHash, loadedHeader.BuildHash);
        }

        /// <summary>ERR-016-010: the write side refuses a header with no §2.3.2 build hash, so this
        /// codec cannot produce a file its own reader rejects. Throws rather than returning the
        /// storage-failure code, which would send the reader looking at the disk.</summary>
        [Test]
        public void SaveLoad_CommitWithoutBuildHash_Throws()
        {
            using var dir = new TempDir("nobuildhash");
            var manager = new SaveManager(dir.Path);
            (SnapshotHeader header, SnapshotPayload payload) = EncodedSnapshotAt(11UL, 0x03);
            header.BuildHash = null;

            Assert.Throws<ArgumentException>(() => manager.CommitAtomic(header, payload));
        }

        /// <summary>ERR-016-010: bad magic is refused. This is also the gate that refuses a file in
        /// the pre-ERR-016-010 unversioned layout, whose first four bytes were the schema version —
        /// refused, never mis-parsed as the new frame.</summary>
        [Test]
        public void SaveLoad_ForeignBytes_AreRefusedAsSchemaIncompatible()
        {
            using var dir = new TempDir("badmagic");
            var manager = new SaveManager(dir.Path);
            (SnapshotHeader header, SnapshotPayload payload) = EncodedSnapshotAt(13UL, 0x04);
            Assert.AreEqual(0, manager.CommitAtomic(header, payload));

            string path = Directory.GetFiles(dir.Path, "*.bin")[0];
            byte[] raw  = File.ReadAllBytes(path);
            raw[0] ^= 0xFF;                       // corrupt the leading magic
            File.WriteAllBytes(path, raw);

            Assert.AreEqual(DeterministicSimConstants.ERR_DS_SCHEMA_INCOMPATIBLE,
                manager.Load(13UL, new SnapshotHeader(), new SnapshotPayload()));
        }

        /// <summary>ERR-016-010: an appended byte is refused. Note this is a FRAMING lock, not a
        /// trailer lock — padding is caught by the trailing-byte guard whether or not the §3.9.2
        /// trailer is checked, which a mutation run proved by deleting the trailer check and watching
        /// this test stay green. The trailer's own lock is the test below it.</summary>
        [Test]
        public void SaveLoad_PaddedRecord_IsRefused()
        {
            using var dir = new TempDir("padded");
            var manager = new SaveManager(dir.Path);
            (SnapshotHeader header, SnapshotPayload payload) = EncodedSnapshotAt(17UL, 0x05);
            Assert.AreEqual(0, manager.CommitAtomic(header, payload));

            string path = Directory.GetFiles(dir.Path, "*.bin")[0];
            byte[] raw  = File.ReadAllBytes(path);
            var padded  = new byte[raw.Length + 1];
            Array.Copy(raw, padded, raw.Length);
            File.WriteAllBytes(path, padded);

            Assert.AreEqual(DeterministicSimConstants.ERR_DS_SCHEMA_INCOMPATIBLE,
                manager.Load(17UL, new SnapshotHeader(), new SnapshotPayload()));
        }

        /// <summary>
        /// ERR-016-010: the §3.9.2 record trailer, locked by the ONE corruption only it can catch — a
        /// trailer whose declared size is wrong while the file length is unchanged. Every other
        /// structural check still passes on these bytes, so deleting the trailer comparison makes this
        /// test and only this test fail. Written after a mutation run found the padded-record test
        /// above did not distinguish the two.
        /// </summary>
        [Test]
        public void SaveLoad_CorruptRecordTrailer_IsRefused()
        {
            using var dir = new TempDir("badtrailer");
            var manager = new SaveManager(dir.Path);
            (SnapshotHeader header, SnapshotPayload payload) = EncodedSnapshotAt(19UL, 0x06);
            Assert.AreEqual(0, manager.CommitAtomic(header, payload));

            string path = Directory.GetFiles(dir.Path, "*.bin")[0];
            byte[] raw  = File.ReadAllBytes(path);
            raw[raw.Length - 8] ^= 0x01;   // low byte of the u64 total-size trailer; length unchanged
            File.WriteAllBytes(path, raw);

            Assert.AreEqual(DeterministicSimConstants.ERR_DS_SCHEMA_INCOMPATIBLE,
                manager.Load(19UL, new SnapshotHeader(), new SnapshotPayload()),
                "A record whose trailer disagrees with its own length must be refused (§3.9.2).");
        }

        /// <summary>A missing file is a STORAGE failure, not a malformed record — the two codes must
        /// stay distinguishable (the AR L-2 split this suite never executed).</summary>
        [Test]
        public void SaveLoad_MissingFile_IsAStorageFailure()
        {
            using var dir = new TempDir("missing");
            var manager = new SaveManager(dir.Path);

            Assert.AreEqual(DeterministicSimConstants.ERR_DS_STORAGE_ATOMICITY,
                manager.Load(999UL, new SnapshotHeader(), new SnapshotPayload()));
        }

        // ══════════════════════════════════════════════════════════════════════════════
        // T-DS-005: save/load with mid-tick snapshot (SaveAtomicMidTick)
        // ══════════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// T-DS-005: Save/load with a mid-tick snapshot (SaveAtomicMidTick).
        /// Stub: SaveManager has no SaveAtomicMidTick method at Stage 0 — activate when
        /// the mid-tick save overload is added in Stage 1.
        /// §5.3 T-DS-SNAP-003 / §5.5.2.
        /// </summary>
        [Test]
        public void SaveLoad_MidTickSnapshot_SaveAndLoad()
        {
            Assert.Ignore("Stage 0+1: SaveManager.SaveAtomicMidTick does not exist at Stage 0 — " +
                          "activate when the mid-tick save overload is added at Stage 1 " +
                          "(§5.3 T-DS-005 / §4.6.1).");
        }

        // ══════════════════════════════════════════════════════════════════════════════
        // T-DS-006: two consecutive saves — second overwrite does not corrupt first digest
        // ══════════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// T-DS-006: Two consecutive saves at tick T and tick T+1.
        /// The second CommitAtomic (overwrite) must not leave a partial or corrupted file
        /// at the first tick's path, and each snapshot's digest chain must be independently valid.
        /// Stub: requires temp-directory fixture at Stage 1.
        /// §5.3 / §4.6.1.1 atomic-write contract (overwrite:true).
        /// </summary>
        [Test]
        public void SaveLoad_ConsecutiveSaves_SecondOverwriteDoesNotCorruptFirstDigest()
        {
            using var dir = new TempDir("consecutive");
            var manager = new SaveManager(dir.Path);
            var codec   = new SnapshotCodec();

            var h1 = new SnapshotHeader();
            h1.Initialize(100UL, null, EnvironmentFingerprint.CreateStage0Dev(), TestBuildIdentity.TestBuildHash);
            var p1 = new SnapshotPayload();
            p1.PayloadBytes[0] = 0x11; p1.BytesWritten = 1;
            codec.Encode(h1, p1);
            Assert.AreEqual(0, manager.CommitAtomic(h1, p1));

            // Same tick, re-saved: exercises the File.Replace overwrite arm of §4.6.1.1 step 3.
            var h1b = new SnapshotHeader();
            h1b.Initialize(100UL, null, EnvironmentFingerprint.CreateStage0Dev(), TestBuildIdentity.TestBuildHash);
            var p1b = new SnapshotPayload();
            p1b.PayloadBytes[0] = 0x22; p1b.BytesWritten = 1;
            codec.Encode(h1b, p1b);
            Assert.AreEqual(0, manager.CommitAtomic(h1b, p1b));

            var h2 = new SnapshotHeader();
            h2.Initialize(101UL, null, EnvironmentFingerprint.CreateStage0Dev(), TestBuildIdentity.TestBuildHash);
            var p2 = new SnapshotPayload();
            p2.PayloadBytes[0] = 0x33; p2.BytesWritten = 1;
            codec.Encode(h2, p2);
            Assert.AreEqual(0, manager.CommitAtomic(h2, p2));

            var loaded1 = new SnapshotHeader();
            var body1   = new SnapshotPayload();
            Assert.AreEqual(0, manager.Load(100UL, loaded1, body1));
            CollectionAssert.AreEqual(h1b.CurrentSnapshotDigest, loaded1.CurrentSnapshotDigest,
                "The overwritten record must be the SECOND save, intact — not a partial file.");
            Assert.AreEqual(0x22, body1.PayloadBytes[0]);

            var loaded2 = new SnapshotHeader();
            var body2   = new SnapshotPayload();
            Assert.AreEqual(0, manager.Load(101UL, loaded2, body2));
            Assert.AreEqual(0x33, body2.PayloadBytes[0]);
            Assert.AreEqual(0, new SnapshotCodec().ValidateHeader(loaded2));

            Assert.AreEqual(0, Directory.GetFiles(dir.Path, "*.tmp").Length,
                "§4.6.1.1 step 5: no temp file may survive a successful commit.");
        }

        // ══════════════════════════════════════════════════════════════════════════════
        // T-DS-007: ValidateHeader rejects tampered digest
        // ══════════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// T-DS-007: After CommitAtomic, flip one bit in the persisted CurrentSnapshotDigest field,
        /// reload the file via SaveManager.Load into a fresh SnapshotHeader, and call
        /// SnapshotCodec.ValidateHeader — it must return ERR_DS_SCHEMA_INCOMPATIBLE or
        /// the subsequent ValidatePrevDigest must return ERR_DS_DIGEST_CHAIN_BREAK.
        /// Stub: requires temp-directory fixture and structured header re-read at Stage 1.
        /// §5.3 / §5.11.6 (T-DS-FAULT-009 variant).
        /// </summary>
        [Test]
        public void SaveLoad_ValidateHeader_RejectsTamperedDigest()
        {
            using var dir = new TempDir("tampered");
            var manager = new SaveManager(dir.Path);
            (SnapshotHeader header, SnapshotPayload payload) = EncodedSnapshotAt(200UL, 0x44);
            Assert.AreEqual(0, manager.CommitAtomic(header, payload));

            // prevSnapshotDigest sits at offset 22: magic(4) + fileVersion(4) + schemaVersion(4) +
            // digestVersion(2) + tick(8). Flipping a bit there leaves the record structurally valid —
            // the trailer still agrees with the file length — which is what makes this a digest test
            // rather than a framing test.
            string path = Directory.GetFiles(dir.Path, "*.bin")[0];
            byte[] raw  = File.ReadAllBytes(path);
            raw[22] ^= 0x01;
            File.WriteAllBytes(path, raw);

            var loaded = new SnapshotHeader();
            var body   = new SnapshotPayload();
            Assert.AreEqual(0, manager.Load(200UL, loaded, body),
                "A flipped digest bit must not break the framing — the record is still well-formed.");

            CollectionAssert.AreNotEqual(header.PrevSnapshotDigest, loaded.PrevSnapshotDigest,
                "The tampered digest must reach the caller, not be silently normalised.");

            var engine = new ReplayEngine(
                new SnapshotCodec(),
                new DeterministicRngService(0xABCDEF0123456789UL),
                new MatchClock(0UL),
                EnvironmentFingerprint.CreateStage0Dev());
            Assert.AreEqual(DeterministicSimConstants.ERR_DS_DIGEST_CHAIN_BREAK,
                engine.PrepareReplay(loaded, body),
                "§4.2.2 step 4 must reject a record whose chain link no longer matches.");

            // RECORDED, not fixed (ERR-016-010): the §4.2.2 lifecycle checks the chain LINK
            // (prevSnapshotDigest) and never recomputes currentSnapshotDigest from the payload it just
            // read, so tampering with the stored current digest — or with the payload — is NOT
            // detected here. That is a third defect on this surface, outside the two ERR-016-010
            // closes, and is written down rather than silently passed over. The assertion below pins
            // today's behaviour so the day someone adds the recomputation, this test fails and says so.
            byte[] again = File.ReadAllBytes(path);
            again[again.Length - 9] ^= 0x01;   // last byte of currentSnapshotDigest (before the u64 trailer)
            File.WriteAllBytes(path, again);

            var loadedCur = new SnapshotHeader();
            var bodyCur   = new SnapshotPayload();
            Assert.AreEqual(0, manager.Load(200UL, loadedCur, bodyCur),
                "A tampered CURRENT digest is currently invisible to the loader — recorded, not fixed.");
        }

        // ══════════════════════════════════════════════════════════════════════════════
        // T-DS-008: ReplayEngine.PrepareReplay steps 1–7 complete without exception
        // ══════════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// T-DS-008: ReplayEngine.PrepareReplay steps 1–7 complete without exception on a
        /// well-formed snapshot. Verifies the full happy path: ValidateHeader → ValidateAgainst
        /// (same fingerprint) → ValidatePrevDigest (first snapshot, all-zero prev) →
        /// payload non-empty → cursor at EndOfSnapshot.
        /// Uses DeterministicSimConstants for all constant references.
        /// §5.3 / §4.2.2.
        /// </summary>
        [Test]
        public void ReplayEngine_PrepareReplay_WellFormedSnapshot_ReturnsZero()
        {
            var codec = new SnapshotCodec();
            EnvironmentFingerprint fingerprint = EnvironmentFingerprint.CreateStage0Dev();

            var rng   = new DeterministicRngService(0xABCDEF0123456789UL);
            var clock = new MatchClock(0UL);
            var engine = new ReplayEngine(codec, rng, clock, fingerprint);

            var header = new SnapshotHeader();
            // Initialize sets Cursor = EndOfSnapshot(tick) — step 7 already satisfied — and
            // (prevDigest == null) clears PrevSnapshotDigest to all-zeros: this is the GENESIS
            // snapshot of the chain (§4.2.2 step 4 "expected predecessor" is the genesis
            // sentinel for the first snapshot).
            header.Initialize(1UL, null, fingerprint, TestBuildIdentity.TestBuildHash);

            var payload = new SnapshotPayload();
            // Write one non-zero byte so step 5 (BytesWritten > 0) passes.
            payload.PayloadBytes[0] = 0x01;
            payload.BytesWritten    = 1;

            // Fixture corrected (dotnet CI gate). The previous version called codec.Encode(),
            // which is the RECORDING-side operation: it ADVANCES the codec's stored _prevDigest
            // to the just-encoded payload digest D, leaving the chain authority positioned to
            // record the NEXT snapshot. PrepareReplay then ran step-4 ValidatePrevDigest with
            // _prevDigest = D against the genesis snapshot's recorded PrevSnapshotDigest = zeros,
            // which mismatched and returned ERR_DS_DIGEST_CHAIN_BREAK (0x1608). That conflated
            // the recording-side chain authority with the replay-side one. On a fresh codec the
            // stored _prevDigest is already the genesis sentinel (all-zeros), which is exactly
            // what a well-formed genesis snapshot's recorded PrevSnapshotDigest must match, so
            // no Encode is needed here. CurrentSnapshotDigest stays all-zeros (a valid digest
            // value); CommitLoadedDigest threads it forward without affecting this assertion.
            // ReplayEngine / SnapshotCodec are unchanged — production is correct per §4.2.2.

            ushort err = engine.PrepareReplay(header, payload);
            Assert.AreEqual(0, err,
                "T-DS-008: PrepareReplay must return 0 on a well-formed snapshot (§4.2.2 steps 1–7).");
        }
    }

    /// <summary>
    /// Regression locks for the 2026-06-15 adversarial-review fixes:
    /// H-1 (Skip branch parity) and M-1 (§3.2.3 chained snapshot digest). These exercise the
    /// production code paths the prior suites encoded but never executed.
    /// </summary>
    [TestFixture]
    public sealed class DeterministicSimAdversarialRegressionTests
    {
        /// <summary>
        /// AR H-1: a branch that Skips a draw-site evaluation and a branch that Reserves+draws it
        /// must leave the stream in lockstep, so a subsequent draw produces the SAME value.
        /// Draw values key on ActionOrdinal (not RngCursor); the pre-fix RngCursor-only Skip
        /// desynced this. §3.2.5.
        /// </summary>
        [Test]
        public void RngService_Skip_PreservesActionOrdinalParity_WithReserveBranch()
        {
            const ulong seed = 0x1234567890ABCDEFUL;
            var taken   = new DeterministicRngService(seed);
            var skipped = new DeterministicRngService(seed);

            int a = taken.RegisterStream("site", SubsystemOrdinals.BallPhysics, 7, 0);
            int b = skipped.RegisterStream("site", SubsystemOrdinals.BallPhysics, 7, 0);

            // First action: one branch draws it, the other skips it (drawing branch would use 3).
            taken.Reserve(a, 3);
            taken.DrawReserved(a, 0, out _);
            taken.CloseReservation(a);

            ushort skipErr = skipped.Skip(b, 3);
            Assert.AreEqual(0, skipErr, "Skip on an idle stream must succeed");

            // Second action: both draw — value MUST match if ActionOrdinal stayed in lockstep.
            taken.Reserve(a, 1);
            taken.DrawReserved(a, 0, out ulong takenValue);
            taken.CloseReservation(a);

            skipped.Reserve(b, 1);
            skipped.DrawReserved(b, 0, out ulong skippedValue);
            skipped.CloseReservation(b);

            Assert.AreEqual(takenValue, skippedValue,
                "AR H-1: Skip must keep ActionOrdinal in lockstep with the drawing branch");
            Assert.AreEqual(taken.GetStreamState(a).ActionOrdinal,
                            skipped.GetStreamState(b).ActionOrdinal,
                "AR H-1: ActionOrdinal must match after Skip(3) vs Reserve(3)");
            Assert.AreEqual(taken.GetStreamState(a).RngCursor,
                            skipped.GetStreamState(b).RngCursor,
                "AR H-1: RngCursor must match after Skip(3) vs Reserve(3)");
        }

        /// <summary>AR H-1: Skip during an open reservation is rejected. §3.2.5 / §3.4.</summary>
        [Test]
        public void RngService_Skip_DuringOpenReservation_ReturnsBudgetMismatch()
        {
            var rng = new DeterministicRngService(0xAAUL);
            int s = rng.RegisterStream("site", SubsystemOrdinals.BallPhysics, 0, 0);
            rng.Reserve(s, 2);
            Assert.AreEqual(DeterministicSimConstants.ERR_DS_RNG_BUDGET_MISMATCH, rng.Skip(s, 1),
                "AR H-1: Skip during an open reservation must return ERR_DS_RNG_BUDGET_MISMATCH");
        }

        /// <summary>
        /// AR M-1: SnapshotCodec.Encode must emit the §3.2.3 chained digest
        /// SHA-256(0x12‖schemaVersion‖tick‖prevDigest‖envFpDigest ‖ 0x11‖payload),
        /// not SHA-256(payload) alone.
        /// </summary>
        [Test]
        public void SnapshotCodec_Encode_ProducesSpecChainedDigest()
        {
            var codec = new SnapshotCodec();
            EnvironmentFingerprint fp = EnvironmentFingerprint.CreateStage0Dev();

            var header = new SnapshotHeader();
            header.Initialize(120UL, prevDigest: null, fp, TestBuildIdentity.TestBuildHash);

            var payload = new SnapshotPayload();
            payload.PayloadBytes[0] = 0xAB;
            payload.BytesWritten = 1;

            codec.Encode(header, payload);

            byte[] expected = ExpectedSnapshotDigest(
                DeterministicSimConstants.SNAPSHOT_SCHEMA_VERSION, 120UL,
                new byte[DeterministicSimConstants.SHA256_BYTES], fp, payload.PayloadBytes, 1);

            CollectionAssert.AreEqual(expected, header.CurrentSnapshotDigest,
                "AR M-1: Encode digest must equal SHA-256 of the §3.2.3 header‖payload preimage");
            CollectionAssert.AreEqual(new byte[DeterministicSimConstants.SHA256_BYTES],
                header.PrevSnapshotDigest,
                "AR M-1: genesis snapshot PrevSnapshotDigest must be all-zero");
        }

        /// <summary>
        /// AR M-1: the digest genuinely chains — the second snapshot records the first's digest as
        /// prev, and an identical tick+payload hashed from the genesis sentinel yields a DIFFERENT
        /// digest (proving prevDigest is part of the preimage).
        /// </summary>
        [Test]
        public void SnapshotCodec_Encode_DigestChain_DependsOnPrevDigest()
        {
            EnvironmentFingerprint fp = EnvironmentFingerprint.CreateStage0Dev();

            var codec = new SnapshotCodec();
            var h1 = new SnapshotHeader(); h1.Initialize(1UL, null, fp, TestBuildIdentity.TestBuildHash);
            var p1 = new SnapshotPayload(); p1.PayloadBytes[0] = 0x01; p1.BytesWritten = 1;
            codec.Encode(h1, p1);
            byte[] d1 = (byte[])h1.CurrentSnapshotDigest.Clone();

            var h2 = new SnapshotHeader(); h2.Initialize(2UL, null, fp, TestBuildIdentity.TestBuildHash);
            var p2 = new SnapshotPayload(); p2.PayloadBytes[0] = 0x01; p2.BytesWritten = 1;
            codec.Encode(h2, p2);

            CollectionAssert.AreEqual(d1, h2.PrevSnapshotDigest,
                "AR M-1: second snapshot must record the first snapshot's digest as prev");

            // Same tick + payload, but chained from genesis (all-zero prev) → must differ.
            var genesisCodec = new SnapshotCodec();
            var hg = new SnapshotHeader(); hg.Initialize(2UL, null, fp, TestBuildIdentity.TestBuildHash);
            var pg = new SnapshotPayload(); pg.PayloadBytes[0] = 0x01; pg.BytesWritten = 1;
            genesisCodec.Encode(hg, pg);

            CollectionAssert.AreNotEqual(hg.CurrentSnapshotDigest, h2.CurrentSnapshotDigest,
                "AR M-1: identical tick+payload must hash differently under a different prev digest");
        }

        private static byte[] ExpectedSnapshotDigest(
            uint schemaVersion, ulong tick, byte[] prevDigest,
            EnvironmentFingerprint fingerprint, byte[] payload, int payloadLen)
        {
            byte[] envFpDigest = fingerprint.ComputeDigest();
            byte[] pre = new byte[1 + 4 + 8 + (DeterministicSimConstants.SHA256_BYTES * 2) + 1 + payloadLen];
            int o = 0;
            CanonicalSerializer.WriteU8 (pre, ref o, DeterministicSimConstants.DOMAIN_TAG_SNAPSHOT_HEADER);
            CanonicalSerializer.WriteU32(pre, ref o, schemaVersion);
            CanonicalSerializer.WriteU64(pre, ref o, tick);
            Array.Copy(prevDigest,  0, pre, o, DeterministicSimConstants.SHA256_BYTES); o += DeterministicSimConstants.SHA256_BYTES;
            Array.Copy(envFpDigest, 0, pre, o, DeterministicSimConstants.SHA256_BYTES); o += DeterministicSimConstants.SHA256_BYTES;
            CanonicalSerializer.WriteU8(pre, ref o, DeterministicSimConstants.DOMAIN_TAG_SNAPSHOT_PAYLOAD);
            Array.Copy(payload, 0, pre, o, payloadLen); o += payloadLen;
            using (System.Security.Cryptography.SHA256 sha = System.Security.Cryptography.SHA256.Create())
            {
                return sha.ComputeHash(pre, 0, o);
            }
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-05-29 | —      | Initial implementation. All §5 test card IDs mapped.             |
// | 1.1     | 2026-06-01 | —      | Add DeterministicSimSaveLoadTests: T-DS-004..007 stubs with       |
//           |            |        | Assert.Ignore (file I/O requires Stage 1 CI), T-DS-008 concrete   |
//           |            |        | ReplayEngine.PrepareReplay happy-path test (no file I/O needed).   |
// | 1.2     | 2026-06-12 | —      | Golden-vector pass compile fixes: v1.1 closed the namespace BEFORE |
// | 1.3     | 2026-06-12 | —      | Dotnet CI gate fix (first-ever execution):                         |
// |         |            |        | SipHash24_ReferenceVectors_ByteExact vectors 4-7 were fabricated   |
// |         |            |        | (matched no published source; 0-3 correct) - the assert loop died  |
// |         |            |        | at vector 4. Corrected from an independent Python mirror of the    |
// |         |            |        | reference implementation; byte-identical to siphash-2-4-kat.md     |
// |         |            |        | rows 4-7, which SipHash24KatTests locks for all 64 lengths.        |
// |         |            |        | Production SipHash24_64 was CORRECT; test data was wrong.          |
//           |            |        | the appended save/load fixture, stranding it in the global         |
//           |            |        | namespace with unresolvable type refs (CS0246) — identical defect  |
//           |            |        | class to First Touch ERR-004 / Pass Mechanics AR-9 H-1; namespace  |
//           |            |        | now closes at EOF. Full KAT coverage moved to dedicated fixtures   |
//           |            |        | (HkdfSha256KatTests / SipHash24KatTests /                          |
//           |            |        | SerializeCanonicalCorpusTests).                                    |
// | 1.4     | 2026-06-13 | —      | Dotnet CI gate adjudication (T-DS-008): fixture defect, not a      |
// |         |            |        | production defect. The happy-path test called codec.Encode()       |
// |         |            |        | (recording side), which advances the codec's _prevDigest to the    |
// |         |            |        | just-encoded digest; PrepareReplay step 4 (§4.2.2) then compared    |
// |         |            |        | that against the genesis snapshot's recorded all-zero              |
// |         |            |        | PrevSnapshotDigest and returned ERR_DS_DIGEST_CHAIN_BREAK (0x1608).|
// |         |            |        | A fresh codec already holds the genesis sentinel (zeros), which is |
// |         |            |        | what a genesis snapshot must chain to, so the Encode call was      |
// |         |            |        | removed. ReplayEngine / SnapshotCodec unchanged.                   |
// | 1.5     | 2026-06-15 | —      | New DeterministicSimAdversarialRegressionTests fixture pins AR     |
// |         |            |        | H-1 (Skip ActionOrdinal branch parity + open-reservation reject)  |
// |         |            |        | and AR M-1 (§3.2.3 chained Encode digest + chain dependence on    |
// |         |            |        | prevDigest) — production paths the corpus suite rebuilt by hand    |
// |         |            |        | but never executed against SnapshotCodec.Encode.                  |
// | 1.6     | 2026-06-16 | —      | Match Engine Phase B step B1: FrameSeconds constant test +         |
// |         |            |        | MatchClock.CurrentMatchTimeSeconds coverage (tick tracking,        |
// |         |            |        | one-second landing, seconds↔ms agreement).                        |
#endregion
