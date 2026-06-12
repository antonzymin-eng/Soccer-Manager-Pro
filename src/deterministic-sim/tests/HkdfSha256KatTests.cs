// File:     src/deterministic-sim/tests/HkdfSha256KatTests.cs
// Created:  2026-06-12
// Modified: 2026-06-12
// Author:   —
// Spec:     Deterministic Simulation #16 §3.2.4, §9.5 #4(a), Code Standards #20
// Purpose:  Full HKDF-SHA256 known-answer test suite against the RFC 5869 vectors in
//           docs/specs/deterministic-sim/golden-vectors/hkdf-sha256-kat.md (Test Cases 1–4).
//           Asserts both the intermediate PRK and the full OKM byte-exact for every case.

using System;
using NUnit.Framework;

namespace TacticalDirector.DeterministicSim
{
    /// <summary>
    /// HKDF-SHA256 KAT suite (hkdf-sha256-kat.md v1.2; §9.5 acceptance criterion #4(a)).
    /// A single mismatch is a hard failure of FR-DS-009-GATE.
    /// RFC cases require L = 42 / 82, so this suite also locks the multi-block Expand
    /// (the pre-v1.2 T(1)-only implementation could not produce these outputs).
    /// </summary>
    [TestFixture]
    public sealed class HkdfSha256KatTests
    {
        private static ulong ReadUInt64LE(byte[] buf, int offset)
        {
            ulong v = 0UL;
            for (int i = 7; i >= 0; i--)
            {
                v = (v << 8) | buf[offset + i];
            }
            return v;
        }

        private static byte[] FromHex(string hex)
        {
            byte[] result = new byte[hex.Length / 2];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            }
            return result;
        }

        private static void AssertCase(
            string caseId, byte[] ikm, byte[] salt, byte[] info, int outputBytes,
            string expectedPrkHex, string expectedOkmHex)
        {
            byte[] prk = DeterministicRngService.HkdfExtract(ikm, salt);
            CollectionAssert.AreEqual(FromHex(expectedPrkHex), prk,
                $"{caseId}: PRK mismatch — golden vector hkdf-sha256-kat.md");

            byte[] okm = DeterministicRngService.HkdfSha256(ikm, salt, info, outputBytes);
            CollectionAssert.AreEqual(FromHex(expectedOkmHex), okm,
                $"{caseId}: OKM mismatch — golden vector hkdf-sha256-kat.md");
        }

        /// <summary>
        /// RFC 5869 Appendix A.1 — basic case with SHA-256; L=42 (OKM corrected per F-HKDF-01).
        /// hkdf-sha256-kat.md Test Case 1.
        /// </summary>
        [Test]
        public void Hkdf_RfcA1_PrkAndOkm_ByteExact()
        {
            byte[] ikm  = new byte[22]; for (int i = 0; i < 22; i++) ikm[i]  = 0x0b;
            byte[] salt = new byte[13]; for (int i = 0; i < 13; i++) salt[i] = (byte)i;
            byte[] info = new byte[10]; for (int i = 0; i < 10; i++) info[i] = (byte)(0xf0 + i);

            AssertCase("T-HKDF-01 (RFC A.1)", ikm, salt, info, 42,
                "077709362c2e32df0ddc3f0dc47bba6390b6c73bb50f9c3122ec844ad7c2b3e5",
                "3cb25f25faacd57a90434f64d0362f2a2d2d0a90cf1a5a4c5db02d56ecc4c5bf34007208d5b887185865");
        }

        /// <summary>
        /// RFC 5869 Appendix A.2 — longer inputs/outputs; L=82 (3 expand blocks).
        /// hkdf-sha256-kat.md Test Case 2.
        /// </summary>
        [Test]
        public void Hkdf_RfcA2_LongInputs_PrkAndOkm_ByteExact()
        {
            byte[] ikm  = new byte[80]; for (int i = 0; i < 80; i++) ikm[i]  = (byte)i;
            byte[] salt = new byte[80]; for (int i = 0; i < 80; i++) salt[i] = (byte)(0x60 + i);
            byte[] info = new byte[80]; for (int i = 0; i < 80; i++) info[i] = (byte)(0xb0 + i);

            AssertCase("T-HKDF-02 (RFC A.2)", ikm, salt, info, 82,
                "06a6b88c5853361a06104c9ceb35b45cef760014904671014a193f40c15fc244",
                "b11e398dc80327a1c8e7f78c596a49344f012eda2d4efad8a050cc4c19afa97c59045a99cac7827271cb" +
                "41c65e590e09da3275600c2f09b8367793a9aca3db71cc30c58179ec3e87c14c01d5c1f3434f1d87");
        }

        /// <summary>
        /// RFC 5869 Appendix A.3 — zero-length salt/info; salt defaults to HashLen zero bytes
        /// per RFC 5869 §2.2 / #16 §3.2.4. hkdf-sha256-kat.md Test Case 3.
        /// </summary>
        [Test]
        public void Hkdf_RfcA3_EmptySaltAndInfo_PrkAndOkm_ByteExact()
        {
            byte[] ikm  = new byte[22]; for (int i = 0; i < 22; i++) ikm[i] = 0x0b;
            byte[] salt = new byte[DeterministicSimConstants.SHA256_BYTES]; // NULL salt → 32 zero bytes
            byte[] info = new byte[0];

            AssertCase("T-HKDF-03 (RFC A.3)", ikm, salt, info, 42,
                "19ef24a32c717b167f33a91d6f648bdf96596776afdb6377ac434c1c293ccb04",
                "8da4e775a563c18f715f802a063c5a31b8a11f5c5ee1879ec3454e5f3c738d2d9d201395faa4b61a96c8");
        }

        /// <summary>
        /// Project Test Case 4 — the RNG_KDF invocation pattern per #16 §3.2.4:
        /// IKM = matchSeed (u64) little-endian, salt = NULL → 32 zero bytes,
        /// info = raw ASCII "DS-RNG-KEY-v1" (13 bytes), L = RNG_KDF_OUTPUT_BYTES (16).
        /// Pinned values from hkdf-sha256-kat.md v1.2; binds the §3.2.4 normative info literal
        /// to its expected (k0, k1) as the spec mandates.
        /// </summary>
        [Test]
        public void Hkdf_ProjectCase4_RngKdfInvocationPattern_Pinned()
        {
            // Fixture matchSeed = 0xAA55AA55AA55AA55 → IKM LE bytes 55 aa 55 aa 55 aa 55 aa
            byte[] ikm  = FromHex("55aa55aa55aa55aa");
            byte[] salt = new byte[DeterministicSimConstants.RNG_KDF_SALT_BYTES];
            byte[] info = System.Text.Encoding.ASCII.GetBytes(DeterministicSimConstants.RNG_KDF_INFO);

            // §3.2.4 normative byte encoding of the info literal (raw ASCII, no string framing)
            CollectionAssert.AreEqual(FromHex("44532d524e472d4b45592d7631"), info,
                "RNG_KDF_INFO must encode to the §3.2.4 normative 13 ASCII bytes of 'DS-RNG-KEY-v1'");

            AssertCase("T-HKDF-04 (RNG_KDF pattern)", ikm, salt, info,
                DeterministicSimConstants.RNG_KDF_OUTPUT_BYTES,
                "d178986b5af973f6269098e66000584a9132b0e078c11ef78c1c257d146b550f",
                "4ac332779efad0bb91ca9765ef9e40a0");

            // (k0, k1) little-endian load of OKM[0..7] / OKM[8..15] — consumed by SipHash24KatTests
            byte[] okm = DeterministicRngService.HkdfSha256(ikm, salt, info,
                DeterministicSimConstants.RNG_KDF_OUTPUT_BYTES);
            ulong k0 = ReadUInt64LE(okm, 0);
            ulong k1 = ReadUInt64LE(okm, 8);
            Assert.AreEqual(0xBBD0FA9E7732C34AUL, k0, "T-HKDF-04: k0 mismatch (pinned)");
            Assert.AreEqual(0xA0409EEF6597CA91UL, k1, "T-HKDF-04: k1 mismatch (pinned)");
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-06-12 | —      | Initial KAT suite: RFC 5869 A.1–A.3 full PRK+OKM (L=42/82 locks    |
//           |            |        | multi-block Expand) + project Test Case 4 pinned per               |
//           |            |        | hkdf-sha256-kat.md v1.2.                                           |
#endregion
