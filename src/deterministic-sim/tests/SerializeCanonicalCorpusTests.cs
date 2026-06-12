// File:     src/deterministic-sim/tests/SerializeCanonicalCorpusTests.cs
// Created:  2026-06-12
// Modified: 2026-06-12
// Author:   —
// Spec:     Deterministic Simulation #16 §3.2.2, §3.2.3, §3.2.4.1, §3.2.5, §9.5 #4(c),
//           Code Standards #20
// Purpose:  Full canonical-serialization known-answer suite against every entry of
//           golden-vectors/serialize-canonical-corpus.md (P/F/S/B/O/E/A/ST/D, 41 rows).
//           Each entry asserts the encoded bytes AND the SHA-256 of those bytes byte-exact.

using System;
using System.Security.Cryptography;
using System.Text;

using NUnit.Framework;

namespace TacticalDirector.DeterministicSim
{
    /// <summary>
    /// SerializeCanonical corpus suite (serialize-canonical-corpus.md v1.0; §9.5 criterion #4(c)).
    /// A single mismatch is a hard failure of FR-DS-009-GATE. The corpus markdown's Appendix A
    /// Python reproducer is the authoritative source of every expected pair below.
    /// </summary>
    [TestFixture]
    public sealed class SerializeCanonicalCorpusTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────────────

        private static string ToHex(byte[] buf, int length)
        {
            StringBuilder sb = new StringBuilder(length * 2);
            for (int i = 0; i < length; i++)
            {
                sb.Append(buf[i].ToString("x2"));
            }
            return sb.ToString();
        }

        private static byte[] Sha256(byte[] buf, int length)
        {
            using (SHA256 sha = SHA256.Create())
            {
                return sha.ComputeHash(buf, 0, length);
            }
        }

        /// <summary>Asserts encoded bytes and their SHA-256 against the corpus columns.</summary>
        private static void AssertEntry(
            string id, byte[] buf, int length, string expectedBytesHex, string expectedSha256Hex)
        {
            Assert.AreEqual(expectedBytesHex, ToHex(buf, length),
                $"{id}: encoded bytes mismatch — serialize-canonical-corpus.md");
            Assert.AreEqual(expectedSha256Hex, ToHex(Sha256(buf, length), 32),
                $"{id}: SHA-256 mismatch — serialize-canonical-corpus.md");
        }

        // ── §1 Primitives (P-01..P-10) ────────────────────────────────────────────────

        /// <summary>Corpus P-01..P-10: bool + all fixed-width integers, little-endian. §3.2.4.1.</summary>
        [Test]
        public void Corpus_Primitives_P01toP10()
        {
            byte[] buf = new byte[8];
            int o;

            o = 0; CanonicalSerializer.WriteBool(buf, ref o, false);
            AssertEntry("P-01", buf, o, "00",
                "6e340b9cffb37a989ca544e6bb780a2c78901d3fb33738768511a30617afa01d");

            o = 0; CanonicalSerializer.WriteBool(buf, ref o, true);
            AssertEntry("P-02", buf, o, "01",
                "4bf5122f344554c53bde2ebb8cd2b7e3d1600ad631c385a5d7cce23c7785459a");

            o = 0; CanonicalSerializer.WriteU8(buf, ref o, 0xAB);
            AssertEntry("P-03", buf, o, "ab",
                "087d80f7f182dd44f184aa86ca34488853ebcc04f0c60d5294919a466b463831");

            o = 0; CanonicalSerializer.WriteI8(buf, ref o, -1);
            AssertEntry("P-04", buf, o, "ff",
                "a8100ae6aa1940d0b663bb31cd466142ebbdbd5187131b92d93818987832eb89");

            o = 0; CanonicalSerializer.WriteU16(buf, ref o, 0x1234);
            AssertEntry("P-05", buf, o, "3412",
                "e74d0e44a658ffcdc0ee7266ebd171413b8fcf182c97a27254d9f48abaea6266");

            o = 0; CanonicalSerializer.WriteI16(buf, ref o, -1);
            AssertEntry("P-06", buf, o, "ffff",
                "ca2fd00fa001190744c15c317643ab092e7048ce086a243e2be9437c898de1bb");

            o = 0; CanonicalSerializer.WriteU32(buf, ref o, 0x12345678u);
            AssertEntry("P-07", buf, o, "78563412",
                "1a2de690568587e6cd9adbd7d9f65ef269becd2f89fb89c224975b0c5944b973");

            o = 0; CanonicalSerializer.WriteI32(buf, ref o, -1);
            AssertEntry("P-08", buf, o, "ffffffff",
                "ad95131bc0b799c0b1af477fb14fcf26a6a9f76079e48bf090acb7e8367bfd0e");

            o = 0; CanonicalSerializer.WriteU64(buf, ref o, 0x0123456789ABCDEFUL);
            AssertEntry("P-09", buf, o, "efcdab8967452301",
                "a85ba2b36261d0dca4b6cbbc840fa8a441ec95200abba5c5623e7ddadeff99e5");

            o = 0; CanonicalSerializer.WriteI64(buf, ref o, -1L);
            AssertEntry("P-10", buf, o, "ffffffffffffffff",
                "12a3ae445661ce5dee78d0650d33362dec29c4f82af05e7e57fb595bbbacf0ca");
        }

        // ── §2 Floats with NaN / signed-zero normalization (F-01..F-09) ───────────────

        /// <summary>Corpus F-01..F-09: f32/f64 incl. -0.0 → +0.0 and Tier B NaN canonicalization. §3.2.4.1.</summary>
        [Test]
        public void Corpus_Floats_F01toF09()
        {
            byte[] buf = new byte[8];
            int o;

            o = 0; CanonicalSerializer.WriteF32(buf, ref o, 1.0f);
            AssertEntry("F-01", buf, o, "0000803f",
                "e00e5eb9444182f352323374ef4e08ebcb784725fdd4fd612d7730540b3e0c8c");

            o = 0; CanonicalSerializer.WriteF32(buf, ref o, -1.0f);
            AssertEntry("F-02", buf, o, "000080bf",
                "c68830a25204a09f8e77aada6bc5807f607cccaaa0ebb2a7122d317584478a8b");

            o = 0; CanonicalSerializer.WriteF32(buf, ref o, -0.0f);
            AssertEntry("F-03", buf, o, "00000000",
                "df3f619804a92fdb4057192dc43dd748ea778adc52bc498ce80524c014b81119");

            o = 0; CanonicalSerializer.WriteF32TierB(buf, ref o, float.NaN);
            AssertEntry("F-04", buf, o, "0000c07f",
                "ef1eaf26cea96eb18f8fa3137abdf23f52852a855c22ae6f169d21a379dcd739");

            o = 0; CanonicalSerializer.WriteF32(buf, ref o, (float)(1.0 / 60.0)); // PHYSICS_DT §3.4.3
            AssertEntry("F-05", buf, o, "8988883c",
                "8b28d83cb5ddc0a51f24f23094cbfa145fee5d22bd205753291d42ccfef0ccc9");

            o = 0; CanonicalSerializer.WriteF64(buf, ref o, 1.0);
            AssertEntry("F-06", buf, o, "000000000000f03f",
                "6c3c396ed6b5c36dcae172271f462051b1266b851e92df3deea8ac65478fd712");

            o = 0; CanonicalSerializer.WriteF64(buf, ref o, -1.0);
            AssertEntry("F-07", buf, o, "000000000000f0bf",
                "e77817b649821c634355a917817c1224a360514b1244fe09e832bac4e8ea4440");

            o = 0; CanonicalSerializer.WriteF64(buf, ref o, -0.0);
            AssertEntry("F-08", buf, o, "0000000000000000",
                "af5570f5a1810b7af78caf4bc70a660f0df51e42baf91d4de5b2328de0e83dfc");

            o = 0; CanonicalSerializer.WriteF64TierB(buf, ref o, double.NaN);
            AssertEntry("F-09", buf, o, "000000000000f87f",
                "74999fd28ab18ccca2bee199f260d19764603a3c78353d773d16d215eebe8e19");
        }

        // ── §3 Strings + §4 Bytes (S-01..S-03, B-01..B-02) ───────────────────────────

        /// <summary>Corpus S-01..S-03 / B-01..B-02: u32 length prefix + ASCII / raw payload. §3.2.4.1.</summary>
        [Test]
        public void Corpus_StringsAndBytes_S01toB02()
        {
            byte[] buf = new byte[32];
            int o;

            o = 0; CanonicalSerializer.WriteString(buf, ref o, "");
            AssertEntry("S-01", buf, o, "00000000",
                "df3f619804a92fdb4057192dc43dd748ea778adc52bc498ce80524c014b81119");

            o = 0; CanonicalSerializer.WriteString(buf, ref o, "abc");
            AssertEntry("S-02", buf, o, "03000000616263",
                "3da9865b43fa2ec490f78da9db16acd5638704dbce5cc7b3df2e3c7a23addf19");

            o = 0; CanonicalSerializer.WriteString(buf, ref o, "Tactical Director");
            AssertEntry("S-03", buf, o, "11000000546163746963616c204469726563746f72",
                "5cf517dc212e6764ef025056c198fc3b30d4ea3b8388851324ecead15818be3c");

            o = 0; CanonicalSerializer.WriteBytes(buf, ref o, new byte[0]);
            AssertEntry("B-01", buf, o, "00000000",
                "df3f619804a92fdb4057192dc43dd748ea778adc52bc498ce80524c014b81119");

            o = 0; CanonicalSerializer.WriteBytes(buf, ref o, new byte[] { 0xCA, 0xFE, 0xBA, 0xBE });
            AssertEntry("B-02", buf, o, "04000000cafebabe",
                "ed2b11b284f1d7bc0eb85ec7b84a96b7dd54ea457488170d5334c119abbad0cb");
        }

        // ── §5 Optional + §6 Enum (O-01..O-04, E-01..E-02) ───────────────────────────

        /// <summary>Corpus O-01..O-04 / E-01..E-02: optional tag discrimination + enum widths. §3.2.4.1.</summary>
        [Test]
        public void Corpus_OptionalsAndEnums_O01toE02()
        {
            byte[] buf = new byte[16];
            int o;

            o = 0; CanonicalSerializer.WriteOptionalAbsent(buf, ref o);
            AssertEntry("O-01", buf, o, "00",
                "6e340b9cffb37a989ca544e6bb780a2c78901d3fb33738768511a30617afa01d");

            o = 0;
            CanonicalSerializer.WriteOptionalPresent(buf, ref o);
            CanonicalSerializer.WriteU32(buf, ref o, 42u);
            AssertEntry("O-02", buf, o, "012a000000",
                "54a9fc9cc1ebb46c6257d79a5f512135ef54ca839155386e6a365c999fa98432");

            o = 0; CanonicalSerializer.WriteOptionalAbsent(buf, ref o);
            AssertEntry("O-03", buf, o, "00",
                "6e340b9cffb37a989ca544e6bb780a2c78901d3fb33738768511a30617afa01d");

            o = 0;
            CanonicalSerializer.WriteOptionalPresent(buf, ref o);
            CanonicalSerializer.WriteString(buf, ref o, "x");
            AssertEntry("O-04", buf, o, "010100000078",
                "0958fbeb1db5b861931b66968e84b79a28f0f55b4671bd7d9b4465ddcef1a3b7");

            o = 0; CanonicalSerializer.WriteU8(buf, ref o, 5); // enum ≤256 variants = u8 width
            AssertEntry("E-01", buf, o, "05",
                "e77b9a9ae9e30b0dbdb6f510a264ef9de781501d7b6b92ae89eb059c5ab743db");

            o = 0; CanonicalSerializer.WriteU16(buf, ref o, 0x0123); // enum ≤65536 variants = u16 width
            AssertEntry("E-02", buf, o, "2301",
                "fff4c04ca7c0dea1d9ce552d547b47c4352b05178009e884c025fe6dbb6aa88d");
        }

        // ── §7 Array<T> incl. nested / variable-width (A-01..A-05) ───────────────────

        /// <summary>Corpus A-01..A-05: u32 count + canonical-sorted self-delimiting elements. §3.2.4.1 / §3.1.1.</summary>
        [Test]
        public void Corpus_Arrays_A01toA05()
        {
            byte[] buf = new byte[32];
            int o;

            o = 0; CanonicalSerializer.WriteU32(buf, ref o, 0u);
            AssertEntry("A-01", buf, o, "00000000",
                "df3f619804a92fdb4057192dc43dd748ea778adc52bc498ce80524c014b81119");

            o = 0;
            CanonicalSerializer.WriteU32(buf, ref o, 3u);
            CanonicalSerializer.WriteU32(buf, ref o, 1u);
            CanonicalSerializer.WriteU32(buf, ref o, 2u);
            CanonicalSerializer.WriteU32(buf, ref o, 3u);
            AssertEntry("A-02", buf, o, "03000000010000000200000003000000",
                "374d429ee174762e85d14ea494b50c91a44a1557227814f14650940d7e283790");

            o = 0;
            CanonicalSerializer.WriteU32(buf, ref o, 3u);
            CanonicalSerializer.WriteU8(buf, ref o, 0x0A);
            CanonicalSerializer.WriteU8(buf, ref o, 0x0B);
            CanonicalSerializer.WriteU8(buf, ref o, 0x0C);
            AssertEntry("A-03", buf, o, "030000000a0b0c",
                "41b6e54688c327cc0990b50b9d0c735db1812e8f0719b441c7af6444a7098698");

            // A-04: bytewise-lex sort "a" < "bb" (0x61 < 0x62); no per-element framing
            o = 0;
            CanonicalSerializer.WriteU32(buf, ref o, 2u);
            CanonicalSerializer.WriteString(buf, ref o, "a");
            CanonicalSerializer.WriteString(buf, ref o, "bb");
            AssertEntry("A-04", buf, o, "020000000100000061020000006262",
                "57e411b2ddb65ad728b9e9b7d166ad7972668e2cfbfec972aa914c264560ac96");

            // A-05: nested array<array<u8>>; inner encodings sorted by their own encoded bytes
            o = 0;
            CanonicalSerializer.WriteU32(buf, ref o, 2u);
            CanonicalSerializer.WriteBytes(buf, ref o, new byte[] { 0x01 });
            CanonicalSerializer.WriteBytes(buf, ref o, new byte[] { 0x02, 0x03 });
            AssertEntry("A-05", buf, o, "020000000100000001020000000203",
                "7aa0997bd00d00df12486693c42807e61f7002c85d24ac4efca2cb094c7e9493");
        }

        // ── §8 Struct flat concatenation (ST-01..ST-03) ──────────────────────────────

        /// <summary>Corpus ST-01..ST-03: declared-schema-order concatenation; no header or field tags. §3.2.4.1 / §3.2.5.3.</summary>
        [Test]
        public void Corpus_Structs_ST01toST03()
        {
            byte[] buf = new byte[32];
            int o;

            o = 0;
            CanonicalSerializer.WriteU32(buf, ref o, 1u);
            CanonicalSerializer.WriteU8(buf, ref o, 0xFF);
            AssertEntry("ST-01", buf, o, "01000000ff",
                "646b2567984a9cdb58007a0b01e3bb373fd30743712760fc5b6bb9c9c71034ac");

            o = 0;
            CanonicalSerializer.WriteString(buf, ref o, "ok");
            CanonicalSerializer.WriteU32(buf, ref o, 7u);
            AssertEntry("ST-02", buf, o, "020000006f6b07000000",
                "e78a4f600003740aa8aff6a8e9801b41e087161c91555d5673535414526306d9");

            // ST-03: the real §3.2.5.3 Tier A tombstone struct, serialized field-by-field
            o = 0;
            WriteDespawnEntry(buf, ref o, new DespawnEntry(42, 7UL, 99UL, 120UL));
            AssertEntry("ST-03", buf, o,
                "2a000000070000000000000063000000000000007800000000000000",
                "2211ce0646d2c821788ff7f57ab18585e848477611d18e85107ad0475a97904f");
        }

        private static void WriteDespawnEntry(byte[] buf, ref int offset, in DespawnEntry entry)
        {
            CanonicalSerializer.WriteU32(buf, ref offset, (uint)entry.EntityId);
            CanonicalSerializer.WriteU64(buf, ref offset, entry.FinalActionOrdinal);
            CanonicalSerializer.WriteU64(buf, ref offset, entry.FinalRngCursor);
            CanonicalSerializer.WriteU64(buf, ref offset, entry.DespawnTick);
        }

        // ── §9 Domain-tagged digest preimages (D-01..D-07) ───────────────────────────

        /// <summary>
        /// Corpus D-01/D-02: PhaseDigest preimage = DOMAIN_TAG_PHASE ‖ DigestVersion(u16) ‖
        /// Tick(u64) ‖ PhaseId(u8). D-01 reproduces the §3.2.4.1 worked byte example;
        /// D-02 locks the tick-sensitive AI-phase digest semantics. §3.2.2.
        /// </summary>
        [Test]
        public void Corpus_PhaseDigestPreimages_D01toD02()
        {
            byte[] buf = new byte[12];
            int o;

            o = 0;
            CanonicalSerializer.WriteU8(buf, ref o, DeterministicSimConstants.DOMAIN_TAG_PHASE);
            CanonicalSerializer.WriteU16(buf, ref o, DeterministicSimConstants.DETERMINISM_DIGEST_VERSION);
            CanonicalSerializer.WriteU64(buf, ref o, 120UL);
            CanonicalSerializer.WriteU8(buf, ref o, (byte)PhaseId.Physics);
            AssertEntry("D-01", buf, o, "100100780000000000000003",
                "9add9c4e2d104800c98cbfda36858969494a2736bf5ad12bb3accb4731c130a1");

            o = 0;
            CanonicalSerializer.WriteU8(buf, ref o, DeterministicSimConstants.DOMAIN_TAG_PHASE);
            CanonicalSerializer.WriteU16(buf, ref o, DeterministicSimConstants.DETERMINISM_DIGEST_VERSION);
            CanonicalSerializer.WriteU64(buf, ref o, 120UL);
            CanonicalSerializer.WriteU8(buf, ref o, (byte)PhaseId.AI); // corpus "AI_NoOp" ordinal 2
            AssertEntry("D-02", buf, o, "100100780000000000000002",
                "9b8707a501d0bde9e106243e4bea7e85177b7579a2acb9374acdc9fd27b1f26a");
        }

        /// <summary>
        /// Corpus D-03: RngDraw preimage = DOMAIN_TAG_RNGDRAW ‖ StreamKey(u64) ‖
        /// actionOrdinal(u64) ‖ drawIndex(u32) — 21 bytes per §3.4 HASH_INPUT_FIELD_WIDTHS.
        /// SHA-256 here verifies the byte concatenation; the SipHash output over the same
        /// preimage is covered by SipHash24KatTests. §3.2.5.
        /// </summary>
        [Test]
        public void Corpus_RngDrawPreimage_D03()
        {
            byte[] buf = new byte[21];
            int o = 0;
            CanonicalSerializer.WriteU8(buf, ref o, DeterministicSimConstants.DOMAIN_TAG_RNGDRAW);
            CanonicalSerializer.WriteU64(buf, ref o, 0x0102030405060708UL); // StreamKey
            CanonicalSerializer.WriteU64(buf, ref o, 1UL);                  // actionOrdinal
            CanonicalSerializer.WriteU32(buf, ref o, 0u);                   // drawIndex
            AssertEntry("D-03", buf, o, "130807060504030201010000000000000000000000",
                "d3a3aff04348adbe7a3893cd3e7a2df713f001691849c1f9ed1c6ee144b0f8c6");
        }

        /// <summary>
        /// Corpus D-04..D-07: EnvironmentFingerprint / SnapshotHeader / SnapshotPayload
        /// preimages and the chained SnapshotDigest
        /// SHA-256(D-04 preimage ‖ D-06 preimage) per §3.2.3 / §3.9.2.
        /// </summary>
        [Test]
        public void Corpus_SnapshotDigestChain_D04toD07()
        {
            // D-05: EnvironmentFingerprint preimage (illustrative empty struct) = tag only
            byte[] envFpPreimage = { DeterministicSimConstants.DOMAIN_TAG_ENV_FP };
            AssertEntry("D-05", envFpPreimage, 1, "14",
                "83891d7fe85c33e52c8b4e5814c92fb6a3b9467299200538a6babaa8b452d879");
            byte[] envFpDigest = Sha256(envFpPreimage, 1);

            // D-04: SnapshotHeader preimage = 0x12 ‖ schemaVersion(u32=1) ‖ tick(u64=120) ‖
            //        prevSnapshotDigest(32×0x00) ‖ environmentFingerprint(SHA-256 of D-05)
            byte[] header = new byte[1 + 4 + 8 + 32 + 32];
            int h = 0;
            CanonicalSerializer.WriteU8(header, ref h, DeterministicSimConstants.DOMAIN_TAG_SNAPSHOT_HEADER);
            CanonicalSerializer.WriteU32(header, ref h, DeterministicSimConstants.SNAPSHOT_SCHEMA_VERSION);
            CanonicalSerializer.WriteU64(header, ref h, 120UL);
            h += DeterministicSimConstants.SHA256_BYTES; // prevSnapshotDigest = 32 zero bytes
            Array.Copy(envFpDigest, 0, header, h, DeterministicSimConstants.SHA256_BYTES);
            h += DeterministicSimConstants.SHA256_BYTES;
            AssertEntry("D-04", header, h,
                "12010000007800000000000000000000000000000000000000000000000000000000000000000000000000" +
                "000083891d7fe85c33e52c8b4e5814c92fb6a3b9467299200538a6babaa8b452d879",
                "cbec838550524abb8e07b94b9ce345748dccfc02067c8d38feb474cd113b85fb");

            // D-06: SnapshotPayload preimage = 0x11 ‖ array<DespawnEntry> = [ST-03]
            byte[] payload = new byte[1 + 4 + 28];
            int p = 0;
            CanonicalSerializer.WriteU8(payload, ref p, DeterministicSimConstants.DOMAIN_TAG_SNAPSHOT_PAYLOAD);
            CanonicalSerializer.WriteU32(payload, ref p, 1u);
            WriteDespawnEntry(payload, ref p, new DespawnEntry(42, 7UL, 99UL, 120UL));
            AssertEntry("D-06", payload, p,
                "11010000002a000000070000000000000063000000000000007800000000000000",
                "244959992ce01bdcdeeae842e51002eb092a5497da6ec13a4865bd7a5bd042d2");

            // D-07: chained SnapshotDigest = SHA-256(D-04 preimage ‖ D-06 preimage)
            byte[] chained = new byte[h + p];
            Array.Copy(header, 0, chained, 0, h);
            Array.Copy(payload, 0, chained, h, p);
            AssertEntry("D-07", chained, chained.Length,
                "12010000007800000000000000000000000000000000000000000000000000000000000000000000000000" +
                "000083891d7fe85c33e52c8b4e5814c92fb6a3b9467299200538a6babaa8b452d879" +
                "11010000002a000000070000000000000063000000000000007800000000000000",
                "40215bc9a6c6eb968a1a0097d2e9aa35da15ec5cfc2dba45004aadc692bbbb3c");
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-06-12 | —      | Initial corpus suite: all 41 serialize-canonical-corpus.md rows   |
//           |            |        | (P-01..P-10, F-01..F-09, S/B, O/E, A-01..A-05, ST-01..ST-03,      |
//           |            |        | D-01..D-07 incl. chained SnapshotDigest), bytes + SHA-256 each.   |
#endregion
