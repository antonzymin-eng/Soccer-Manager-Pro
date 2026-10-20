// ============================================================================
// File:     src/transfers/TransfersSaveCodec.cs
// Created:  2026-10-18
// Modified: 2026-10-20
// Author:   —
// Specs:    Spec #16 §3.2.4.1 (CanonicalSerializer / save framing helpers)
//           Spec #20 §3.6.2 (style & docs governance)
//           Spec #31 §4.4, KD-4, FR-TX-012/014/018/027, Appendix B (as corrected by ERR-031-003)
// Purpose:  Encodes/decodes #31's independently version-gated transfers sub-blob: the managed club's
//           window/spend state and its contracts. The season frame composes it opaquely (#30 owns the
//           frame; this codec owns the layout).
// ============================================================================

using System;

using TacticalDirector.DeterministicSim;
using TacticalDirector.PlayerDatabase;

namespace TacticalDirector.Transfers
{
    /// <summary>
    /// Canonical codec for the #31 transfers block (Spec #31 Appendix B, ERR-031-003):
    /// <c>u32 magic "XFER", u32 version, u32 clubCount ∈ {0,1}</c>, then for the managed club
    /// <c>i32 ClubId, i64 CommittedSpendThisWindow, u32 OpenWorldDay, u32 CloseWorldDay</c>, then
    /// <c>u32 contractCount</c> and per contract, ascending <c>PlayerId</c>,
    /// <c>i32 PlayerId, i64 WagePerPeriod, i32 LengthSeasons</c>. No RNG cursor (FR-TX-018).
    /// <para>
    /// <b>clubCount is a wiring fact, not a content count.</b> A wired career always encodes exactly one
    /// club record, even after every contract has expired or been sold, because a
    /// <see cref="TransfersState"/> cannot exist without its <see cref="TransfersState.ManagedClubId"/>.
    /// Zero means "no #31 subsystem behind this save", which is the only composition from which the
    /// season root may infer that a populated destination is about to be dropped (the ERR-030-038 lesson:
    /// guard on wiring, never on emptiness).
    /// </para>
    /// </summary>
    public static class TransfersSaveCodec
    {
        /// <summary>Encodes a transfers block. Never mutates <paramref name="stateOrNull"/>.</summary>
        /// <param name="stateOrNull">The managed club's #31 state, or <c>null</c> for a save with no #31
        /// subsystem wired, which writes a well-formed zero-club block.</param>
        /// <returns>The self-identifying, independently versioned canonical block.</returns>
        public static byte[] Encode(TransfersState stateOrNull)
        {
            Contract[] contracts = stateOrNull == null ? Array.Empty<Contract>() : stateOrNull.ContractsSnapshot();
            int clubCount = stateOrNull == null ? 0 : 1;

            int length = checked(
                TransfersConstants.TRANSFERS_SAVE_HEADER_BYTES
                + clubCount * TransfersConstants.TRANSFERS_SAVE_CLUB_RECORD_BYTES
                + 4
                + contracts.Length * TransfersConstants.TRANSFERS_SAVE_CONTRACT_RECORD_BYTES);
            byte[] blob = new byte[length];
            int o = 0;

            CanonicalSerializer.WriteU32(blob, ref o, TransfersConstants.TRANSFERS_SAVE_MAGIC);
            CanonicalSerializer.WriteU32(blob, ref o, TransfersConstants.TRANSFERS_SAVE_FORMAT_VERSION);
            CanonicalSerializer.WriteU32(blob, ref o, (uint)clubCount);

            if (stateOrNull != null)
            {
                TransferWindow window = stateOrNull.ActiveWindow;
                CanonicalSerializer.WriteI32(blob, ref o, stateOrNull.ManagedClubId);
                CanonicalSerializer.WriteI64(blob, ref o, stateOrNull.CommittedSpendThisWindow);
                CanonicalSerializer.WriteU32(blob, ref o, window.OpenWorldDay);
                CanonicalSerializer.WriteU32(blob, ref o, window.CloseWorldDay);
            }

            // ContractsSnapshot is already in ascending PlayerId order (the state's SortedDictionary), and
            // every entry passed InsertContract's validation, which also caps the set at CLUB_SQUAD_SIZE (the
            // bound Decode enforces), so Encode cannot write what Decode refuses.
            CanonicalSerializer.WriteU32(blob, ref o, (uint)contracts.Length);
            for (int i = 0; i < contracts.Length; i++)
            {
                CanonicalSerializer.WriteI32(blob, ref o, contracts[i].PlayerId);
                CanonicalSerializer.WriteI64(blob, ref o, contracts[i].WagePerPeriod);
                CanonicalSerializer.WriteI32(blob, ref o, contracts[i].LengthSeasons);
            }

            if (o != blob.Length)
            {
                throw new InvalidOperationException(
                    "TransfersSaveCodec.Encode wrote " + o + " bytes but sized the buffer at " + blob.Length + ".");
            }

            return blob;
        }

        /// <summary>
        /// Decodes one complete transfers block (F3: magic, version, length bounds and trailing bytes all
        /// fail loud with <see cref="InvalidOperationException"/>, the corrupt-file posture).
        /// </summary>
        /// <param name="blob">The exact transfers sub-blob.</param>
        /// <returns>The restored state, or <c>null</c> for a zero-club (transfers-unwired) block.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="blob"/> is null.</exception>
        /// <exception cref="InvalidOperationException">The block is corrupt, truncated, padded, a different
        /// format, another generation of this one, or carries a value no live state can hold.</exception>
        public static TransfersState Decode(byte[] blob)
        {
            if (blob == null)
            {
                throw new ArgumentNullException(nameof(blob));
            }

            const string Subject = "Transfers save";
            int len = blob.Length;
            int o = 0;

            SaveBlobFramingHelpers.Require(o, 8, len, Subject, "magic/version");
            uint magic = CanonicalSerializer.ReadU32(blob, ref o);
            if (magic != TransfersConstants.TRANSFERS_SAVE_MAGIC)
            {
                throw new InvalidOperationException(
                    Subject + " magic mismatch: got 0x" + magic.ToString("X8") + ", expected 0x" +
                    TransfersConstants.TRANSFERS_SAVE_MAGIC.ToString("X8") +
                    " — this is not a transfers block (ERR-031-003).");
            }

            uint version = CanonicalSerializer.ReadU32(blob, ref o);
            if (version != TransfersConstants.TRANSFERS_SAVE_FORMAT_VERSION)
            {
                throw new InvalidOperationException(
                    Subject + " format version " + version + " != expected " +
                    TransfersConstants.TRANSFERS_SAVE_FORMAT_VERSION + " — no cross-version migration (F3).");
            }

            int clubCount = SaveBlobFramingHelpers.ReadCount(
                blob, ref o, len, TransfersConstants.TRANSFERS_SAVE_CLUB_RECORD_BYTES, Subject, "club");
            if (clubCount > TransfersConstants.TRANSFERS_SAVE_MAX_CLUBS)
            {
                throw new InvalidOperationException(
                    Subject + " carries " + clubCount + " club records; the minimal block is scoped to the one " +
                    "managed club (Spec #31 §2.2) — corrupt save.");
            }

            bool hasClub = clubCount == 1;
            int managedClubId = 0;
            long committedSpend = 0;
            uint openDay = 0;
            uint closeDay = 0;
            if (hasClub)
            {
                SaveBlobFramingHelpers.Require(o, TransfersConstants.TRANSFERS_SAVE_CLUB_RECORD_BYTES, len, Subject, "club record");
                managedClubId = CanonicalSerializer.ReadI32(blob, ref o);
                committedSpend = unchecked((long)CanonicalSerializer.ReadU64(blob, ref o));
                openDay = CanonicalSerializer.ReadU32(blob, ref o);
                closeDay = CanonicalSerializer.ReadU32(blob, ref o);
            }

            int contractCount = SaveBlobFramingHelpers.ReadCount(
                blob, ref o, len, TransfersConstants.TRANSFERS_SAVE_CONTRACT_RECORD_BYTES, Subject, "contract");
            if (!hasClub && contractCount != 0)
            {
                throw new InvalidOperationException(
                    Subject + " carries " + contractCount + " contract(s) but no managed club — contracts belong " +
                    "to the managed club's state and cannot exist without it; corrupt save.");
            }

            // Pre-allocation guard, symmetric with TransfersState.InsertContract's live cap: it also stops a
            // corrupt count from sizing the contracts array before any record is read.
            if (contractCount > PlayerDatabaseConstants.CLUB_SQUAD_SIZE)
            {
                throw new InvalidOperationException(
                    Subject + " carries " + contractCount + " contracts for one club; a squad holds at most " +
                    PlayerDatabaseConstants.CLUB_SQUAD_SIZE + " players — corrupt save.");
            }

            var contracts = new Contract[contractCount];
            long previousPlayerId = (long)int.MinValue - 1L;
            for (int i = 0; i < contractCount; i++)
            {
                SaveBlobFramingHelpers.Require(
                    o, TransfersConstants.TRANSFERS_SAVE_CONTRACT_RECORD_BYTES, len, Subject, "contract record");
                int playerId = CanonicalSerializer.ReadI32(blob, ref o);
                SaveBlobFramingHelpers.RequireAscending(playerId, ref previousPlayerId, Subject, "contract player id", i);
                contracts[i].PlayerId = playerId;
                contracts[i].WagePerPeriod = unchecked((long)CanonicalSerializer.ReadU64(blob, ref o));
                contracts[i].LengthSeasons = CanonicalSerializer.ReadI32(blob, ref o);
            }

            if (o != len)
            {
                throw new InvalidOperationException(
                    Subject + " has " + (len - o) + " trailing byte(s) after the declared content — truncated, " +
                    "padded, or corrupt (F3).");
            }

            if (!hasClub)
            {
                return null;
            }

            // Value gates: route every field through the same validation a live state applies, re-typed to the
            // corrupt-file exception so a caller cannot mistake a bad file for a bad argument.
            try
            {
                var window = new TransferWindow(openDay, closeDay);
                return TransfersState.Restore(managedClubId, committedSpend, in window, contracts);
            }
            catch (ArgumentException e)
            {
                throw new InvalidOperationException(Subject + " carries a value no live state can hold: " + e.Message, e);
            }
            catch (InvalidOperationException e)
            {
                throw new InvalidOperationException(Subject + " carries a value no live state can hold: " + e.Message, e);
            }
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Change |
// | --------|------------|--------|---------------------------------------------- |
// | 1.0     | 2026-10-18 | —      | #31 T1: magic-led transfers sub-blob codec (ERR-031-003 corrects Appendix B's version-first, magic-less layout). |
// | 1.1     | 2026-10-20 | —      | AR round 1 (ERR-031-006 cycle) M1: the Encode-cannot-write-what-Decode-refuses claim is now true (InsertContract enforces the CLUB_SQUAD_SIZE contract cap); Decode's guard documented as the symmetric pre-allocation bound. |
#endregion
