// File:     src/deterministic-sim/DeterministicSimConstants.cs
// Created:  2026-05-29
// Modified: 2026-05-29
// Author:   —
// Spec:     Deterministic Simulation #16 §3.4, §3.2.4.1, Code Standards #20
// Purpose:  All numeric and string constants for the deterministic simulation system.
//           No magic literals permitted in any formula or system file.
//           Region order: Fixed → Derived → Cross → GT.

namespace TacticalDirector.DeterministicSim
{
    /// <summary>
    /// All constants for the deterministic simulation system.
    /// Every symbol in §3.2–§3.9 pseudocode bodies that is a constant appears here.
    /// No magic literals permitted in any formula or system file.
    /// Deterministic Simulation #16 §3.4.
    /// </summary>
    public static class DeterministicSimConstants
    {
        #region Fixed

        // ── Tick rates ────────────────────────────────────────────────────────────────

        /// <summary>[FIXED] Physics/render loop tick rate (Hz). CLAUDE.md §Heartbeat Tick Rate; Ball Physics #1 §1.2.</summary>
        public const int PHYSICS_TICK_HZ = 60;

        /// <summary>[FIXED] Tactical/AI loop tick rate (Hz). CLAUDE.md §Heartbeat Tick Rate.</summary>
        public const int TACTICAL_TICK_HZ = 10;

        // ── Spec versioning ───────────────────────────────────────────────────────────

        /// <summary>[FIXED] Determinism digest format version embedded in every snapshot header. §3.4 / §3.9.2.
        /// Must be bumped on any incompatible change to the digest protocol or serialization layout.</summary>
        public const ushort DETERMINISM_DIGEST_VERSION = 1;

        /// <summary>[FIXED] Schema version for the snapshot binary format. §3.9.2.
        /// Bumped whenever the authoritative-state field set changes in a backward-incompatible way.</summary>
        public const uint SNAPSHOT_SCHEMA_VERSION = 1;

        // ── Serialization format ─────────────────────────────────────────────────────

        /// <summary>[FIXED] IEEE-754 bit pattern of (float)(1.0/60.0) — the authoritative physics dt.
        /// Canonical value: 0x3C888889. §3.4 / §3.2.4.1 corpus entry F-05.</summary>
        public const uint PHYSICS_DT_BITS = 0x3C888889;

        /// <summary>[FIXED] Tag byte indicating 'present' in optional&lt;T&gt; canonical encoding. §3.2.4.1.</summary>
        public const byte OPTIONAL_PRESENT_TAG = 0x01;

        /// <summary>[FIXED] Tag byte indicating 'absent' in optional&lt;T&gt; canonical encoding. §3.2.4.1.</summary>
        public const byte OPTIONAL_ABSENT_TAG = 0x00;

        /// <summary>[FIXED] Boolean canonical encoding for true. §3.2.4.1.</summary>
        public const byte BOOL_TRUE = 0x01;

        /// <summary>[FIXED] Boolean canonical encoding for false. §3.2.4.1.</summary>
        public const byte BOOL_FALSE = 0x00;

        /// <summary>[FIXED] Canonical NaN bit pattern for f32 Tier B fields. §3.2.4.1.</summary>
        public const uint F32_CANONICAL_NAN_BITS = 0x7FC00000u;

        /// <summary>[FIXED] Canonical NaN bit pattern for f64 Tier B fields. §3.2.4.1.</summary>
        public const ulong F64_CANONICAL_NAN_BITS = 0x7FF8000000000000UL;

        /// <summary>[FIXED] Canonical +0.0 bit pattern for f32 (-0.0 normalisation). §3.2.4.1.</summary>
        public const uint F32_POSITIVE_ZERO_BITS = 0x00000000u;

        /// <summary>[FIXED] f32 bit mask for the sign bit. §3.2.4.1.</summary>
        public const uint F32_SIGN_MASK = 0x80000000u;

        /// <summary>[FIXED] Unicode NFC version pinned for string encoding at Stage 0. §3.2.4.1 / §4.8.</summary>
        public const string UNICODE_NFC_VERSION = "15.1";

        // ── Domain tags (§3.4) — allocated per subsystem; each tag is u8 ──────────────

        /// <summary>[FIXED] Domain tag for phase-level digests. §3.4.</summary>
        public const byte DOMAIN_TAG_PHASE = 0x10;

        /// <summary>[FIXED] Domain tag for snapshot payload digest scope. §3.4.</summary>
        public const byte DOMAIN_TAG_SNAPSHOT_PAYLOAD = 0x11;

        /// <summary>[FIXED] Domain tag for snapshot header digest scope. §3.4.</summary>
        public const byte DOMAIN_TAG_SNAPSHOT_HEADER = 0x12;

        /// <summary>[FIXED] Domain tag for per-draw RNG hash preimages. §3.4.</summary>
        public const byte DOMAIN_TAG_RNGDRAW = 0x13;

        /// <summary>[FIXED] Domain tag for EnvironmentFingerprint encoding. §3.4 / §4.8.</summary>
        public const byte DOMAIN_TAG_ENV_FP = 0x14;

        /// <summary>[FIXED] Domain tag allocated for Event System #17. §3.4 v1.0.1; ERR-017-001 resolved.</summary>
        public const byte DOMAIN_TAG_EVENT_LEDGER = 0x15;

        /// <summary>[FIXED] Domain tag allocated for Heading Mechanics #10. §3.4 v1.0.2; ERR-010-001 resolved.</summary>
        public const byte DOMAIN_TAG_HEADING = 0x16;

        /// <summary>[FIXED] Domain tag allocated for Positioning AI #12. §3.4 v1.0.5; ERR-012-001 resolved.</summary>
        public const byte DOMAIN_TAG_POSITIONING_AI = 0x17;

        /// <summary>[FIXED] Domain tag allocated for Pressing AI #13. §3.4.</summary>
        public const byte DOMAIN_TAG_PRESSING_AI = 0x19;

        /// <summary>[FIXED] Domain tag allocated for Defensive AI #14. §3.4 v1.0.5; ERR-014-004 resolved.</summary>
        public const byte DOMAIN_TAG_DEFENSIVE_AI = 0x1A;

        /// <summary>[FIXED] Domain tag allocated for Attacking AI #15. §3.4; ERR-015-001 resolved.</summary>
        public const byte DOMAIN_TAG_ATTACKING_AI = 0x1B;

        /// <summary>[FIXED] Domain tag allocated for Goalkeeper Mechanics #11. §3.4 v1.0.5; ERR-011-001 resolved.</summary>
        public const byte DOMAIN_TAG_GOALKEEPER = 0x1D;

        // ── Error codes (u16; §3.4 / §3.10) ──────────────────────────────────────────

        /// <summary>[FIXED] Phase ownership violation: a system mutated fields outside its declared WriteSet. §3.4 / EC-016-001.</summary>
        public const ushort ERR_DS_PHASE_OWNERSHIP = 0x1601;

        /// <summary>[FIXED] Snapshot schema or digest version is incompatible with the running build. §3.4.</summary>
        public const ushort ERR_DS_SCHEMA_INCOMPATIBLE = 0x1602;

        /// <summary>[FIXED] A required RNG stream is absent from the snapshot. §3.4.</summary>
        public const ushort ERR_DS_RNG_STREAM_MISSING = 0x1603;

        /// <summary>[FIXED] Replay-side EnvironmentFingerprint does not match the recording. §3.4.</summary>
        public const ushort ERR_DS_REPLAY_ENV_MISMATCH = 0x1604;

        /// <summary>[FIXED] Save attempted outside a legal snapshot boundary. §3.4.</summary>
        public const ushort ERR_DS_SAVE_BOUNDARY = 0x1605;

        /// <summary>[FIXED] Tier A field contains NaN or Infinity. §3.4 / FR-DS-011.</summary>
        public const ushort ERR_DS_TIERA_NONFINITE = 0x1606;

        /// <summary>[FIXED] Tier B field has no approved tolerance row in the tolerance matrix. §3.4 / FR-DS-011.</summary>
        public const ushort ERR_DS_TIERB_TOLERANCE_MISSING = 0x1607;

        /// <summary>[FIXED] Snapshot digest chain break: prevSnapshotDigest does not match. §3.4.</summary>
        public const ushort ERR_DS_DIGEST_CHAIN_BREAK = 0x1608;

        /// <summary>[FIXED] Replay cursor is not at EndOfSnapshot[T] when step 7 is executed. §3.4 / §4.2.2.</summary>
        public const ushort ERR_DS_REPLAY_BOUNDARY = 0x1609;

        /// <summary>[FIXED] Tier B field contains a non-canonical NaN (not normalised to 0x7FC00000). §3.4.</summary>
        public const ushort ERR_DS_TIERB_NONFINITE = 0x160A;

        /// <summary>[FIXED] Reserve() count mismatches the declared draw-site budget. §3.4.</summary>
        public const ushort ERR_DS_RNG_BUDGET_MISMATCH = 0x160B;

        /// <summary>[FIXED] Save atomic-write contract failed (temp-rename-fsync sequence broken). §3.4 / §4.6.1.1.</summary>
        public const ushort ERR_DS_STORAGE_ATOMICITY = 0x160C;

        /// <summary>[FIXED] Recording-side EnvironmentFingerprint mutated after match start. §3.4 / EC-016-013.</summary>
        public const ushort ERR_DS_ENV_MUTATION = 0x160D;

        // ── RNG cryptographic parameters ──────────────────────────────────────────────

        /// <summary>[FIXED] HKDF key derivation function identifier. §3.2.1 / §3.4.</summary>
        public const string RNG_KDF = "HKDF-SHA256";

        /// <summary>[FIXED] HKDF info string for stream key derivation. §3.2.1.</summary>
        public const string RNG_KDF_INFO = "DS-RNG-KEY-v1";

        /// <summary>[FIXED] HKDF output length in bytes (16 bytes = k0 ‖ k1 for SipHash-2-4). §3.2.1.</summary>
        public const int RNG_KDF_OUTPUT_BYTES = 16;

        /// <summary>[FIXED] HKDF salt length in bytes (32 zero bytes). §3.2.1.</summary>
        public const int RNG_KDF_SALT_BYTES = 32;

        /// <summary>[FIXED] SipHash-2-4 compression rounds (c). §3.2.1.</summary>
        public const int SIPHASH_C_ROUNDS = 2;

        /// <summary>[FIXED] SipHash-2-4 finalization rounds (d). §3.2.1.</summary>
        public const int SIPHASH_D_ROUNDS = 4;

        // ── Snapshot canonical field widths (bytes) for hash preimages. §3.2.4.1 ─────

        /// <summary>[FIXED] Serialized width of a domain tag in bytes. §3.2.4.1.</summary>
        public const int FIELD_WIDTH_DOMAIN_TAG = 1;

        /// <summary>[FIXED] Serialized width of DigestVersion in bytes. §3.2.4.1.</summary>
        public const int FIELD_WIDTH_DIGEST_VERSION = 2;

        /// <summary>[FIXED] Serialized width of Tick in bytes (u64). §3.2.4.1.</summary>
        public const int FIELD_WIDTH_TICK = 8;

        /// <summary>[FIXED] Serialized width of PhaseId in bytes (u8). §3.2.4.1.</summary>
        public const int FIELD_WIDTH_PHASE_ID = 1;

        /// <summary>[FIXED] Serialized width of SchemaVersion in bytes (u32). §3.2.4.1.</summary>
        public const int FIELD_WIDTH_SCHEMA_VERSION = 4;

        /// <summary>[FIXED] Serialized width of entityId in bytes (u32). §3.2.4.1.</summary>
        public const int FIELD_WIDTH_ENTITY_ID = 4;

        /// <summary>[FIXED] Serialized width of streamVersion in bytes (u16). §3.2.4.1.</summary>
        public const int FIELD_WIDTH_STREAM_VERSION = 2;

        /// <summary>[FIXED] Serialized width of actionOrdinal in bytes (u64). §3.2.4.1.</summary>
        public const int FIELD_WIDTH_ACTION_ORDINAL = 8;

        /// <summary>[FIXED] Serialized width of drawIndex in bytes (u32). §3.2.4.1.</summary>
        public const int FIELD_WIDTH_DRAW_INDEX = 4;

        // ── SHA-256 output ────────────────────────────────────────────────────────────

        /// <summary>[FIXED] SHA-256 output length in bytes; used for snapshot digest and floatModelHash. §3.4.</summary>
        public const int SHA256_BYTES = 32;

        // ── Snapshot ring buffer / save protocol ──────────────────────────────────────

        /// <summary>[FIXED] EndOfSnapshot phase ordinal value for ReplayCursor step-7 assertion. §4.2.2 step 7.
        /// PhaseId.Snapshot has ordinal 6 (0-indexed; Input=0..Snapshot=6).</summary>
        public const byte END_OF_SNAPSHOT_PHASE_ORDINAL = 6;

        #endregion

        #region Derived

        /// <summary>
        /// [DERIVED] Physics frames per tactical tick (AI phase stride).
        /// Formula: PHYSICS_TICK_HZ / TACTICAL_TICK_HZ. Deterministic Simulation #16 §3.4 / §3.1.2.
        /// Source constants: PHYSICS_TICK_HZ, TACTICAL_TICK_HZ (both Fixed const).
        /// AR-1 M-1: static readonly (not const) to signal derived nature per project convention.
        /// </summary>
        public static readonly int AI_PHASE_STRIDE = PHYSICS_TICK_HZ / TACTICAL_TICK_HZ;

        /// <summary>
        /// [DERIVED] Physics frame duration in milliseconds.
        /// Formula: 1000.0f / PHYSICS_TICK_HZ. Deterministic Simulation #16 §3.4.
        /// Source constants: PHYSICS_TICK_HZ (Fixed const).
        /// </summary>
        public static readonly float FrameMs = 1000.0f / PHYSICS_TICK_HZ;

        #endregion

        #region GT

        /// <summary>[GT] Maximum number of DespawnLog entries per match (pre-allocated). §3.2.3.
        /// Sized at 512 = 2 × 22 agents × 11.6 average lifetime minutes; ample headroom for Stage 0.</summary>
        public static readonly int MaxDespawnEntries = 512; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Maximum number of concurrent RNG streams registered per match. §3.2.5.</summary>
        public static readonly int MaxRngStreams = 64; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Maximum size in bytes of the in-memory snapshot ring buffer slot. §3.9.2.
        /// Conservatively sized; actual serialized size depends on authoritative state surface.</summary>
        public static readonly int MaxSnapshotBytes = 65536; // TODO: replace with config loader (Stage 1)

        #endregion
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                           |
// | 1.0     | 2026-05-29 | —      | Initial implementation.                                              |
// | 1.1     | 2026-05-29 | —      | AR-1 M-1: AI_PHASE_STRIDE changed from const to static readonly.     |
#endregion
