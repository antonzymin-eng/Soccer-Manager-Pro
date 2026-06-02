// File:     src/testing-strategy/GoldenVectorKind.cs
// Created:  2026-06-02
// Modified: 2026-06-02
// Author:   —
// Spec:     Testing Strategy & Framework #19 §3.8 / Appendix F,
//           Deterministic Simulation #16 §9.5 acceptance criterion #4,
//           Code Standards #20
// Purpose:  Discriminates the three golden-vector corpora pinned by #16 §9.5 #4
//           (a/b/c). Stage 0 cert gate FR-DS-009-GATE consumes all three.

namespace TacticalDirector.TestingStrategy
{
    /// <summary>
    /// Discriminates the three golden-vector corpora #16 §9.5 acceptance criterion #4 pins:
    /// (a) HKDF-SHA256 RFC 5869 known-answer tests, (b) SipHash-2-4-64 reference vectors,
    /// (c) <c>SerializeCanonical</c> byte-level corpus per #16 §3.2.4.1.
    /// Every kind MUST pass for FR-DS-009-GATE (Stage 0 certification gate, #16 §5.5).
    /// Testing Strategy &amp; Framework #19 Appendix F glossary "Golden trace".
    /// </summary>
    public enum GoldenVectorKind : byte
    {
        /// <summary>RFC 5869 HKDF-SHA256 KAT corpus. Authoritative source: #16 §3.4 RNG_KDF.</summary>
        HkdfSha256Kat = 0,

        /// <summary>SipHash-2-4-64 KAT corpus (Aumasson &amp; Bernstein 2012 Appendix A).
        /// Authoritative source: #16 §3.4 RNG_STREAM_HASH.</summary>
        SipHash24Kat = 1,

        /// <summary><c>SerializeCanonical</c> reference corpus.
        /// Authoritative source: #16 §3.2.4.1 primitive encoding table.</summary>
        CanonicalSerializeCorpus = 2,
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-06-02 | —      | Initial implementation. |
#endregion
