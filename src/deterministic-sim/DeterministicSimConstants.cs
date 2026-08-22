// File:     src/deterministic-sim/DeterministicSimConstants.cs
// Created:  2026-05-29
// Modified: 2026-08-22 (ERR-016-009: DOMAIN_TAG_BUILD_IDENTITY 0x2E + BUILD_IDENTITY_VERSION +
//           ERR_DS_REPLAY_BUILD_MISMATCH 0x160E for the §2.3.2 buildHash — v1.8)
// Author:   —
// Spec:     Deterministic Simulation #16 §3.4, §3.2.4.1, Code Standards #20
// Purpose:  All numeric and string constants for the deterministic simulation system.
//           No magic literals permitted in any formula or system file.
//           Region order: Fixed → Derived → Cross → GT.

using static TacticalDirector.ProjectConstants.GameplayConfigHolder;

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
        /// Bumped whenever the authoritative-state field set changes in a backward-incompatible way.
        /// <para>
        /// <b>This is NOT the on-disk file frame's version</b> — see <see cref="SNAPSHOT_FILE_FORMAT_VERSION"/>.
        /// It rides inside the §3.2.3 snapshot-digest preimage, so moving it moves every digest and
        /// invalidates the golden-vector corpus; only a change to the authoritative STATE shape earns
        /// that. Identity metadata added to the file frame does not.
        /// </para></summary>
        public const uint SNAPSHOT_SCHEMA_VERSION = 1;

        /// <summary>
        /// [FIXED] Magic identifying a <c>SaveManager</c> snapshot file — ASCII <c>'S''N''A''P'</c>.
        /// §3.9.2.1. Written first and checked first: the magic says WHICH format the bytes are, and
        /// <see cref="SNAPSHOT_FILE_FORMAT_VERSION"/> says which generation of it (ERR-029-005 /
        /// ERR-041-009: a format version is not a format identifier). It is also what distinguishes a
        /// file written by the pre-ERR-016-010 unversioned layout, whose first four bytes were the
        /// schema version — such a file fails the magic check and is refused, never mis-parsed.
        /// </summary>
        public const uint SNAPSHOT_FILE_MAGIC = 0x534E4150;   // 'S''N''A''P'

        /// <summary>
        /// [FIXED] Generation of the on-disk snapshot FILE frame identified by
        /// <see cref="SNAPSHOT_FILE_MAGIC"/> (§3.9.2.1). Distinct from both
        /// <see cref="SNAPSHOT_SCHEMA_VERSION"/> (the #16 header framing schema, which rides in the
        /// digest preimage) and <see cref="DETERMINISM_DIGEST_VERSION"/> — the same three-version
        /// split `MATCH_SAVE_FORMAT_VERSION` already draws for the match save file. Version 1 is the
        /// first frame to carry the <see cref="EnvironmentFingerprint"/> and the §2.3.2 build hash
        /// (ERR-016-010).
        /// </summary>
        public const uint SNAPSHOT_FILE_FORMAT_VERSION = 1;

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

        /// <summary>[FIXED] Domain tag allocated for Living World #22 (world.text RNG stream + §4.6
        /// snapshot block). §3.4 v1.0.7; ERR-022-001 resolved — next value after 0x1D (0x18/0x1C stay
        /// permanently orphaned per ERR-016-003).</summary>
        public const byte DOMAIN_TAG_LIVING_WORLD = 0x1E;

        /// <summary>[FIXED] Domain tag allocated for the Player Database (roster-generation RNG
        /// stream). §3.4; next value after 0x1E. Back-prop from
        /// docs/tracking/squad-player-data-design.md KD-5 (design-supplement stage; no numbered
        /// spec yet — candidate #27).</summary>
        public const byte DOMAIN_TAG_PLAYER_DATABASE = 0x1F;

        /// <summary>
        /// [FIXED] Domain tag allocated for the Season &amp; Competition Loop #30 (FR-SN-027 — the season
        /// RNG sub-stream). §3.4; back-prop ERR-030-001, landing at #30 T2's first draw site (the
        /// round-resolution model's key derivation) exactly as that entry pins it.
        /// <para>
        /// <b>0x20 and 0x21 are deliberate gaps</b>, reserved for Player Progression #28 and Training
        /// #29 respectively (#30 KD-5's honesty note); #28 T0 recorded the same reservation from its own
        /// side (KD-B) and will claim 0x20 when its production regen stream lands. Allocating 0x22 here
        /// rather than compacting to 0x20 keeps the spec-pinned numbers stable across the three specs.
        /// </para>
        /// </summary>
        public const byte DOMAIN_TAG_SEASON_LOOP = 0x22;

        /// <summary>
        /// [FIXED] Domain tag allocated for Injuries &amp; Medical #41 (FR-MD-005 — the keyed
        /// world-tick occurrence draws; no registered stream, ERR-041-012). §3.4; back-prop ERR-041-001, which promoted
        /// the number spec-text-first at #41's approval and pinned the code const to land at #41's
        /// first draw site. This is that site: #41's daily occurrence draw.
        /// <para>
        /// No <c>SubsystemOrdinals.InjuriesMedical</c> mirror lands with it. #41's draws are keyed on
        /// <c>(playerId, worldDay, purpose)</c> and register no cursor stream (KD-1 / FR-MD-007), so a
        /// subsystem ordinal — whose only job is to key a registered stream — would be a phantom
        /// surface. This is exactly the #30 <c>DOMAIN_TAG_SEASON_LOOP</c> / ERR-030-012 precedent: tag
        /// allocated, ordinal deliberately absent.
        /// </para>
        /// <para>
        /// 0x23–0x29 remain the gaps reserved for #31/#32/#33/#34 and #40 (see the roadmap §6 block);
        /// allocating 0x2A here rather than compacting keeps every spec-pinned number stable.
        /// </para>
        /// </summary>
        public const byte DOMAIN_TAG_INJURIES_MEDICAL = 0x2A;

        /// <summary>
        /// [FIXED] Domain tag for the §2.3.2 <c>buildHash</c> preimage — the identity of the compiled
        /// binaries a run executed on. §3.4; resolves the <c>buildHash</c> half of ERR-016-009.
        /// <para>
        /// Allocated AFTER the roadmap §6 reserved block (<c>_RESERVED_0x2B_</c>/<c>_0x2C_</c>/<c>_0x2D_</c>,
        /// held for #42/#43/#45) so every spec-pinned subsystem number stays stable. This is an
        /// infrastructure tag, not a subsystem allocation, so no <c>SubsystemOrdinals</c> value lands
        /// with it: an ordinal exists only to key a registered RNG stream, and a build hash registers
        /// none (the <c>DOMAIN_TAG_INJURIES_MEDICAL</c> / ERR-041-012 precedent).
        /// </para>
        /// </summary>
        public const byte DOMAIN_TAG_BUILD_IDENTITY = 0x2E;

        /// <summary>
        /// [FIXED] Preimage-layout version of the §2.3.2 <c>buildHash</c>. Separates one GENERATION of
        /// the preimage from the next; the domain tag above is what separates this format from another
        /// (ERR-029-005 / ERR-041-009: a format version is not a format identifier). Bumping this
        /// changes every build's hash, which refuses saves written by earlier builds — the same
        /// failure direction §2.3.2 rule 4 already accepts.
        /// </summary>
        public const ushort BUILD_IDENTITY_VERSION = 1;

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

        /// <summary>
        /// [FIXED] Restore/replay refused: the recorded §2.3.2 <c>buildHash</c> differs from the live
        /// one — the snapshot was produced by different compiled binaries. §3.4 / EC-016-015.
        /// Distinct from <see cref="ERR_DS_REPLAY_ENV_MISMATCH"/> (0x1604), which is host/float-model
        /// divergence: the fingerprint pins the HOST, this code pins the BINARY, and collapsing the
        /// two is the reading ERR-016-009 was filed against.
        /// </summary>
        public const ushort ERR_DS_REPLAY_BUILD_MISMATCH = 0x160E;

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

        /// <summary>
        /// [DERIVED] Physics frame duration in seconds (the per-tick simulation timestep dt).
        /// Formula: FrameMs / 1000.0f. Deterministic Simulation #16 §3.4.
        /// Source constants: FrameMs (transitively PHYSICS_TICK_HZ).
        /// Derived from FrameMs (rather than 1.0f / PHYSICS_TICK_HZ) so the seconds clock and the
        /// physics-integration dt share one derivation chain (PHYSICS_TICK_HZ → FrameMs → FrameSeconds);
        /// consumers that need seconds (e.g. AgentMovement OscillationGuard's WindowSeconds comparison)
        /// MUST source seconds here, never by reinterpreting FrameMs.
        /// </summary>
        public static readonly float FrameSeconds = FrameMs / 1000.0f;

        #endregion

        #region GT

        /// <summary>[GT] Maximum number of DespawnLog entries per match (pre-allocated). §3.2.3.
        /// Sized at 512 = 2 × 22 agents × 11.6 average lifetime minutes; ample headroom for Stage 0.</summary>
        public static readonly int MaxDespawnEntries = Config.GetInt("deterministic-sim", "MaxDespawnEntries", 512);

        /// <summary>[GT] Maximum number of concurrent RNG streams registered per match. §3.2.5.</summary>
        public static readonly int MaxRngStreams = Config.GetInt("deterministic-sim", "MaxRngStreams", 64);

        /// <summary>[GT] Maximum size in bytes of the in-memory snapshot ring buffer slot. §3.9.2.
        /// Conservatively sized; actual serialized size depends on authoritative state surface.</summary>
        public static readonly int MaxSnapshotBytes = Config.GetInt("deterministic-sim", "MaxSnapshotBytes", 65536);

        #endregion
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                           |
// | 1.0     | 2026-05-29 | —      | Initial implementation.                                              |
// | 1.1     | 2026-05-29 | —      | AR-1 M-1: AI_PHASE_STRIDE changed from const to static readonly.     |
// | 1.2     | 2026-06-16 | —      | Match Engine Phase B step B1: added [DERIVED] FrameSeconds           |
// |         |            |        | (FrameMs / 1000) — the per-tick dt / seconds-clock derivation.       |
// | 1.3     | 2026-07-02 | —      | DOMAIN_TAG_LIVING_WORLD = 0x1E allocated (ERR-022-001; #16 §3.4      |
// |         |            |        | v1.0.7 — next value after 0x1D; 0x18/0x1C stay orphaned).            |
// | 1.4     | 2026-07-15 | —      | DOMAIN_TAG_PLAYER_DATABASE = 0x1F allocated for the new              |
// |         |            |        | player-database roster-generation RNG stream (next value after      |
// |         |            |        | 0x1E). Back-prop from squad-player-data-design.md KD-5.              |
// | 1.5     | 2026-07-26 | —      | DOMAIN_TAG_SEASON_LOOP = 0x22 allocated at its first draw site       |
// |         |            |        | (#30 T2's round-resolution key derivation), per ERR-030-001.        |
// |         |            |        | 0x20 / 0x21 stay reserved gaps for #28 / #29 (#30 KD-5).            |
// | 1.6     | 2026-08-05 | —      | DOMAIN_TAG_INJURIES_MEDICAL = 0x2A allocated at its first draw site  |
// |         |            |        | (#41's daily occurrence draw), per ERR-041-001. No SubsystemOrdinals |
// |         |            |        | mirror: #41 keys its draws and registers no cursor stream, the       |
// |         |            |        | ERR-030-012 precedent. 0x23-0x29 stay reserved gaps.                 |
// | 1.7     | 2026-08-08 | —      | Balance-pass AR pass 9 (M2 repo-wide sweep): the 0x2A tag's own  |
// |         |            |        | doc still designated the draws "the injuries.occurrence          |
// |         |            |        | world-tick draws" — the retired stream name as the live          |
// |         |            |        | designation (ERR-041-012); re-anchored to the keyed derivation.  |
// | 1.8     | 2026-08-22 | —      | ERR-016-009: DOMAIN_TAG_BUILD_IDENTITY = 0x2E allocated at its    |
// |         |            |        | first (and only) computation site, BuildIdentity.ComputeHash,     |
// |         |            |        | plus BUILD_IDENTITY_VERSION = 1 and the new refusal code          |
// |         |            |        | ERR_DS_REPLAY_BUILD_MISMATCH = 0x160E. Allocated AFTER the       |
// |         |            |        | roadmap §6 reserved block 0x2B-0x2D so every spec-pinned number   |
// |         |            |        | stays stable; no SubsystemOrdinals mirror (no registered stream). |
#endregion
