// File:     src/deterministic-sim/tests/SipHash24KatTests.cs
// Created:  2026-06-12
// Modified: 2026-06-12
// Author:   —
// Spec:     Deterministic Simulation #16 §3.2.1, §3.2.5, §9.5 #4(b), Code Standards #20
// Purpose:  Full SipHash-2-4-64 known-answer test suite against the 64 Aumasson & Bernstein 2012
//           Appendix A reference vectors in golden-vectors/siphash-2-4-kat.md, plus the pinned
//           project-specific RNG_STREAM_HASH draw-preimage case.

using NUnit.Framework;

namespace TacticalDirector.DeterministicSim
{
    /// <summary>
    /// SipHash-2-4-64 KAT suite (siphash-2-4-kat.md v1.2; §9.5 acceptance criterion #4(b)).
    /// All 64 reference vectors executed; a single mismatch is a hard failure of FR-DS-009-GATE.
    /// Expected values are the table's little-endian byte rows interpreted as u64
    /// (same convention as the original 8-vector subset in DeterministicSimTests).
    /// </summary>
    [TestFixture]
    public sealed class SipHash24KatTests
    {
        // Aumasson & Bernstein 2012 Appendix A — all 64 vectors (siphash-2-4-kat.md v1.1 table,
        // each row's little-endian byte sequence loaded as u64).
        private static readonly ulong[] s_expected =
        {
            0x726FDB47DD0E0E31UL, 0x74F839C593DC67FDUL, 0x0D6C8009D9A94F5AUL, 0x85676696D7FB7E2DUL,
            0xCF2794E0277187B7UL, 0x18765564CD99A68DUL, 0xCBC9466E58FEE3CEUL, 0xAB0200F58B01D137UL,
            0x93F5F5799A932462UL, 0x9E0082DF0BA9E4B0UL, 0x7A5DBBC594DDB9F3UL, 0xF4B32F46226BADA7UL,
            0x751E8FBC860EE5FBUL, 0x14EA5627C0843D90UL, 0xF723CA908E7AF2EEUL, 0xA129CA6149BE45E5UL,
            0x3F2ACC7F57C29BDBUL, 0x699AE9F52CBE4794UL, 0x4BC1B3F0968DD39CUL, 0xBB6DC91DA77961BDUL,
            0xBED65CF21AA2EE98UL, 0xD0F2CBB02E3B67C7UL, 0x93536795E3A33E88UL, 0xA80C038CCD5CCEC8UL,
            0xB8AD50C6F649AF94UL, 0xBCE192DE8A85B8EAUL, 0x17D835B85BBB15F3UL, 0x2F2E6163076BCFADUL,
            0xDE4DAAACA71DC9A5UL, 0xA6A2506687956571UL, 0xAD87A3535C49EF28UL, 0x32D892FAD841C342UL,
            0x7127512F72F27CCEUL, 0xA7F32346F95978E3UL, 0x12E0B01ABB051238UL, 0x15E034D40FA197AEUL,
            0x314DFFBE0815A3B4UL, 0x027990F029623981UL, 0xCADCD4E59EF40C4DUL, 0x9ABFD8766A33735CUL,
            0x0E3EA96B5304A7D0UL, 0xAD0C42D6FC585992UL, 0x187306C89BC215A9UL, 0xD4A60ABCF3792B95UL,
            0xF935451DE4F21DF2UL, 0xA9538F0419755787UL, 0xDB9ACDDFF56CA510UL, 0xD06C98CD5C0975EBUL,
            0xE612A3CB9ECBA951UL, 0xC766E62CFCADAF96UL, 0xEE64435A9752FE72UL, 0xA192D576B245165AUL,
            0x0A8787BF8ECB74B2UL, 0x81B3E73D20B49B6FUL, 0x7FA8220BA3B2ECEAUL, 0x245731C13CA42499UL,
            0xB78DBFAF3A8D83BDUL, 0xEA1AD565322A1A0BUL, 0x60E61C23A3795013UL, 0x6606D7E446282B93UL,
            0x6CA4ECB15C5F91E1UL, 0x9F626DA15C9625F3UL, 0xE51B38608EF25F57UL, 0x958A324CEB064572UL,
        };

        /// <summary>
        /// All 64 Appendix A reference vectors: key = 00 01 ... 0f
        /// (k0 = 0x0706050403020100, k1 = 0x0f0e0d0c0b0a0908), message i = bytes 0x00..0x(i-1).
        /// siphash-2-4-kat.md vector table, byte-exact.
        /// </summary>
        [Test]
        public void SipHash24_All64ReferenceVectors_ByteExact()
        {
            const ulong k0 = 0x0706050403020100UL;
            const ulong k1 = 0x0F0E0D0C0B0A0908UL;

            for (int i = 0; i < s_expected.Length; i++)
            {
                byte[] msg = new byte[i];
                for (int j = 0; j < i; j++)
                {
                    msg[j] = (byte)j;
                }

                ulong result = DeterministicRngService.SipHash24_64(k0, k1, msg);
                Assert.AreEqual(s_expected[i], result,
                    $"SipHash-2-4 vector {i} mismatch — golden vector siphash-2-4-kat.md");
            }
        }

        /// <summary>
        /// Project-specific RNG_STREAM_HASH draw case (siphash-2-4-kat.md v1.2, pinned):
        /// (k0, k1) from hkdf-sha256-kat.md Test Case 4; preimage = DOMAIN_TAG_RNGDRAW ‖
        /// StreamKey(u64 LE) ‖ actionOrdinal(u64 LE) ‖ drawIndex(u32 LE) per §3.4
        /// HASH_INPUT_FIELD_WIDTHS — 21 bytes, identical to corpus entry D-03.
        /// </summary>
        [Test]
        public void SipHash24_ProjectDrawPreimage_Pinned()
        {
            // k0/k1 pinned in HkdfSha256KatTests.Hkdf_ProjectCase4_RngKdfInvocationPattern_Pinned
            const ulong k0 = 0xBBD0FA9E7732C34AUL;
            const ulong k1 = 0xA0409EEF6597CA91UL;

            // StreamKey = 0x0102030405060708, actionOrdinal = 1, drawIndex = 0 (corpus D-03 fixture)
            byte[] preimage =
            {
                DeterministicSimConstants.DOMAIN_TAG_RNGDRAW,
                0x08, 0x07, 0x06, 0x05, 0x04, 0x03, 0x02, 0x01, // StreamKey u64 LE
                0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, // actionOrdinal u64 LE
                0x00, 0x00, 0x00, 0x00,                         // drawIndex u32 LE
            };
            Assert.AreEqual(21, preimage.Length,
                "§3.4 HASH_INPUT_FIELD_WIDTHS: draw preimage must be 1+8+8+4 = 21 bytes");

            ulong result = DeterministicRngService.SipHash24_64(k0, k1, preimage);
            Assert.AreEqual(0x9E847C2A31BDDB4EUL, result,
                "Project draw case mismatch — pinned in siphash-2-4-kat.md v1.2");
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-06-12 | —      | Initial KAT suite: all 64 Appendix A vectors + pinned project     |
//           |            |        | RNG_STREAM_HASH draw case per siphash-2-4-kat.md v1.2.            |
#endregion
