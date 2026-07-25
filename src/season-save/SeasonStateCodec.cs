// File:     src/season-save/SeasonStateCodec.cs
// Created:  2026-07-25
// Modified: 2026-07-25
// Author:   —
// Spec:     Season & Competition Loop #30 §3.6 (season-state sub-blob codec), Appendix B (byte layout),
//           FR-SN-019/022/023, KD-1/KD-4/KD-5; Deterministic Simulation #16 §3.2.4.1
//           (CanonicalSerializer); Code Standards #20
// Purpose:  Pure byte codec for the season-state sub-blob — the third opaque blob the season save file
//           frames (#30 KD-1). Encodes a SeasonState in the pinned Appendix B field order and reads it
//           back through the value types' own validating constructors, fail-loud on any version /
//           length-bound / coherence / trailing-byte violation. No file I/O (that is SeasonSaveManager),
//           so the codec is exhaustively unit-testable in memory.

using System;

using TacticalDirector.DeterministicSim;

namespace TacticalDirector.SeasonSave
{
    /// <summary>
    /// Encodes / decodes the season-state sub-blob (#30 §3.6 / Appendix B). The blob is one
    /// <see cref="SeasonLoopConstants.SEASON_STATE_FORMAT_VERSION"/>-gated payload carrying the whole
    /// serialized surface of a <see cref="SeasonState"/>: seed, season number, managed club, club set,
    /// the CONCRETE schedule (KD-5 — serialized, never regenerated), the calendar cursor (KD-4), the
    /// live table in ClubId order, and the board.
    /// <para>
    /// <b>Fail-loud posture (FR-SN-023 / F3).</b> Every length prefix goes through the overflow-safe
    /// <c>ReadCount</c> bound, the format version gates before any field is interpreted, and trailing
    /// bytes after the declared content throw — the <c>MatchSaveCodec</c> / <c>WorldStateSerializer</c>
    /// posture. Beyond framing, decode lands every value through the type's own validating constructor
    /// (<see cref="Fixture"/>, <see cref="LeagueTableRow.Create"/>, <see cref="SeasonCalendar.Create"/>,
    /// <see cref="BoardState"/>, and finally the <see cref="SeasonState"/> constructor's coherence
    /// gates), so a corrupt file cannot produce a structurally impossible season — it throws.
    /// </para>
    /// <para>
    /// Off the 60 Hz hot path (a save is a host action), so allocation is permitted.
    /// </para>
    /// </summary>
    public static class SeasonStateCodec
    {
        private const byte FixtureNotPlayed = 0;
        private const byte FixturePlayed = 1;

        // Appendix B per-element widths, named so the size computation reads as the layout it mirrors.
        private const int BytesPerClubId = 4;                    // i32
        private const int BytesPerFixture = 4 + 4 + 4 + 1;       // round i32, home i32, away i32, played u8
        private const int BytesPerRoundDay = 4;                  // u32
        private const int BytesPerTableRow = 9 * 4;              // 9 x i32 (Appendix B row 10)

        /// <summary>
        /// Encodes <paramref name="state"/> into the season sub-blob, in the pinned Appendix B field
        /// order. The buffer is sized exactly to the content; see <see cref="Decode"/> for the inverse
        /// (kept adjacent so a layout change is edited in one place).
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="state"/> is null.</exception>
        public static byte[] Encode(SeasonState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            var clubIds = state.ClubIds;
            var fixtures = state.Fixtures;
            uint[] roundDays = state.Calendar.RoundDaysCopy();
            var rows = state.TableRowsInClubIdOrder();

            int size = 4                                     // 1  version
                     + 8                                     // 2  seed
                     + 4                                     // 3  seasonNumber
                     + 4                                     // 3a managedClubId
                     + 4 + clubIds.Count * BytesPerClubId     // 4  clubCount + 5  clubIds[]
                     + 4 + fixtures.Count * BytesPerFixture   // 6  fixtureCount + 7 fixtures[]
                     + 4                                     // 8  calendar.nextRoundIndex
                     + 4 + roundDays.Length * BytesPerRoundDay // 8 calendar.roundCount + roundToDay[]
                     + 4 + rows.Count * BytesPerTableRow      // 9  tableRowCount + 10 tableRows[]
                     + 4 + 4;                                 // 11 board (targetPosition, jobSecurity)

            byte[] buf = new byte[size];
            int o = 0;

            CanonicalSerializer.WriteU32(buf, ref o, SeasonLoopConstants.SEASON_STATE_FORMAT_VERSION);
            CanonicalSerializer.WriteU64(buf, ref o, state.Seed);
            CanonicalSerializer.WriteI32(buf, ref o, state.SeasonNumber);
            CanonicalSerializer.WriteI32(buf, ref o, state.ManagedClubId);

            CanonicalSerializer.WriteU32(buf, ref o, (uint)clubIds.Count);
            for (int i = 0; i < clubIds.Count; i++)
            {
                CanonicalSerializer.WriteI32(buf, ref o, clubIds[i]);
            }

            CanonicalSerializer.WriteU32(buf, ref o, (uint)fixtures.Count);
            for (int i = 0; i < fixtures.Count; i++)
            {
                Fixture f = fixtures[i];
                CanonicalSerializer.WriteI32(buf, ref o, f.RoundIndex);
                CanonicalSerializer.WriteI32(buf, ref o, f.HomeClubId);
                CanonicalSerializer.WriteI32(buf, ref o, f.AwayClubId);
                CanonicalSerializer.WriteU8(buf, ref o, f.Played ? FixturePlayed : FixtureNotPlayed);
            }

            CanonicalSerializer.WriteI32(buf, ref o, state.Calendar.NextRoundIndex);
            CanonicalSerializer.WriteU32(buf, ref o, (uint)roundDays.Length);
            for (int i = 0; i < roundDays.Length; i++)
            {
                CanonicalSerializer.WriteU32(buf, ref o, roundDays[i]);
            }

            CanonicalSerializer.WriteU32(buf, ref o, (uint)rows.Count);
            for (int i = 0; i < rows.Count; i++)
            {
                LeagueTableRow r = rows[i];
                CanonicalSerializer.WriteI32(buf, ref o, r.ClubId);
                CanonicalSerializer.WriteI32(buf, ref o, r.Played);
                CanonicalSerializer.WriteI32(buf, ref o, r.Won);
                CanonicalSerializer.WriteI32(buf, ref o, r.Drawn);
                CanonicalSerializer.WriteI32(buf, ref o, r.Lost);
                CanonicalSerializer.WriteI32(buf, ref o, r.GoalsFor);
                CanonicalSerializer.WriteI32(buf, ref o, r.GoalsAgainst);
                CanonicalSerializer.WriteI32(buf, ref o, r.GoalDifference);
                CanonicalSerializer.WriteI32(buf, ref o, r.Points);
            }

            CanonicalSerializer.WriteI32(buf, ref o, state.Board.Objective.TargetPositionOrBetter);
            CanonicalSerializer.WriteI32(buf, ref o, state.Board.JobSecurityPerMille);

            if (o != buf.Length)
            {
                // Guards the size computation against Encode drift — the two must agree exactly.
                throw new InvalidOperationException(
                    "SeasonStateCodec.Encode wrote " + o + " bytes but sized the buffer at " +
                    buf.Length + " — the size computation is out of sync with Encode.");
            }

            return buf;
        }

        /// <summary>
        /// Decodes a season sub-blob produced by <see cref="Encode"/> back into a
        /// <see cref="SeasonState"/>. Fail-loud (throws) on: a null blob; a
        /// <see cref="SeasonLoopConstants.SEASON_STATE_FORMAT_VERSION"/> mismatch (KD-4 — no Stage-0
        /// migration); a truncated read; a length prefix that would read past the blob; a table row
        /// whose serialized goal difference disagrees with its goal columns; or any trailing bytes
        /// after the declared content (FR-SN-023). Values that are individually well-formed but
        /// mutually incoherent (a fixture naming a club outside the club set, a calendar shorter than
        /// the schedule, a table missing a club) throw from the <see cref="SeasonState"/> constructor's
        /// own gates.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="blob"/> is null.</exception>
        /// <exception cref="InvalidOperationException">Framing corruption the codec itself detects: a
        /// version mismatch, a truncated read, an out-of-bounds length prefix, a played flag other than
        /// 0/1, a goal difference disagreeing with its goal columns, or trailing bytes.</exception>
        /// <exception cref="ArgumentOutOfRangeException">A field is individually out of range for the
        /// value type that must hold it (e.g. a negative table column at
        /// <see cref="LeagueTableRow.Create"/>).</exception>
        /// <exception cref="ArgumentException">Fields are individually well-formed but mutually
        /// incoherent, from the <see cref="SeasonState"/> constructor's cross-field gates.</exception>
        public static SeasonState Decode(byte[] blob)
        {
            if (blob == null)
            {
                throw new ArgumentNullException(nameof(blob));
            }

            int len = blob.Length;
            int o = 0;

            Require(o, 4, len, "format version");
            uint format = CanonicalSerializer.ReadU32(blob, ref o);
            if (format != SeasonLoopConstants.SEASON_STATE_FORMAT_VERSION)
            {
                throw new InvalidOperationException(
                    "Season state format version " + format + " != expected " +
                    SeasonLoopConstants.SEASON_STATE_FORMAT_VERSION +
                    " — no cross-version migration at Stage 0 (KD-4).");
            }

            Require(o, 8 + 4 + 4, len, "seed / seasonNumber / managedClubId");
            ulong seed = CanonicalSerializer.ReadU64(blob, ref o);
            int seasonNumber = CanonicalSerializer.ReadI32(blob, ref o);
            int managedClubId = CanonicalSerializer.ReadI32(blob, ref o);

            int clubCount = ReadCount(blob, ref o, len, BytesPerClubId, "club");
            var clubIds = new int[clubCount];
            for (int i = 0; i < clubCount; i++)
            {
                clubIds[i] = CanonicalSerializer.ReadI32(blob, ref o);
            }

            int fixtureCount = ReadCount(blob, ref o, len, BytesPerFixture, "fixture");
            var fixtures = new Fixture[fixtureCount];
            for (int i = 0; i < fixtureCount; i++)
            {
                int round = CanonicalSerializer.ReadI32(blob, ref o);
                int home = CanonicalSerializer.ReadI32(blob, ref o);
                int away = CanonicalSerializer.ReadI32(blob, ref o);
                byte played = CanonicalSerializer.ReadU8(blob, ref o);
                if (played != FixtureNotPlayed && played != FixturePlayed)
                {
                    throw new InvalidOperationException(
                        "Season state fixture " + i + " has played flag " + played +
                        " — neither 0 nor 1; corrupt save.");
                }

                // The Fixture constructor is the fail-loud gate for a negative round or a self-fixture.
                fixtures[i] = new Fixture(round, home, away, played == FixturePlayed);
            }

            Require(o, 4, len, "calendar cursor");
            int nextRoundIndex = CanonicalSerializer.ReadI32(blob, ref o);
            int roundCount = ReadCount(blob, ref o, len, BytesPerRoundDay, "calendar round");
            var roundDays = new uint[roundCount];
            for (int i = 0; i < roundCount; i++)
            {
                roundDays[i] = CanonicalSerializer.ReadU32(blob, ref o);
            }

            // SeasonCalendar.Create is the fail-loud gate for an empty / non-ascending day mapping and
            // an out-of-range cursor.
            SeasonCalendar calendar = SeasonCalendar.Create(nextRoundIndex, roundDays);

            int rowCount = ReadCount(blob, ref o, len, BytesPerTableRow, "table row");
            var rows = new LeagueTableRow[rowCount];
            for (int i = 0; i < rowCount; i++)
            {
                int clubId = CanonicalSerializer.ReadI32(blob, ref o);
                int played = CanonicalSerializer.ReadI32(blob, ref o);
                int won = CanonicalSerializer.ReadI32(blob, ref o);
                int drawn = CanonicalSerializer.ReadI32(blob, ref o);
                int lost = CanonicalSerializer.ReadI32(blob, ref o);
                int goalsFor = CanonicalSerializer.ReadI32(blob, ref o);
                int goalsAgainst = CanonicalSerializer.ReadI32(blob, ref o);
                int goalDifference = CanonicalSerializer.ReadI32(blob, ref o);
                int points = CanonicalSerializer.ReadI32(blob, ref o);

                // GoalDifference is DERIVED on build (§3.2 — recomputed, never accumulated) but is its
                // own serialized field (Appendix B row 10). Rather than trust the serialized value or
                // silently discard it, we check the two agree: a save whose GD contradicts its goal
                // columns is corrupt, and silently preferring either one would hide that.
                if (goalDifference != goalsFor - goalsAgainst)
                {
                    throw new InvalidOperationException(
                        "Season state table row " + i + " (club " + clubId + ") has goal difference " +
                        goalDifference + " but goalsFor(" + goalsFor + ") - goalsAgainst(" +
                        goalsAgainst + ") = " + (goalsFor - goalsAgainst) + " — corrupt save.");
                }

                // LeagueTableRow.Create is the fail-loud gate for negative counts and W+D+L != P.
                rows[i] = LeagueTableRow.Create(
                    clubId, played, won, drawn, lost, goalsFor, goalsAgainst, points);
            }

            Require(o, 4 + 4, len, "board");
            int targetPosition = CanonicalSerializer.ReadI32(blob, ref o);
            int jobSecurity = CanonicalSerializer.ReadI32(blob, ref o);

            // ── Trailing-byte guard (FR-SN-023 / F3) ──────────────────────────────────
            if (o != len)
            {
                throw new InvalidOperationException(
                    "Season state blob has " + (len - o) + " trailing byte(s) after the declared " +
                    "content — truncated, padded, or corrupt.");
            }

            // BoardObjective / BoardState are the fail-loud gates for a non-positive target position
            // and an out-of-scale job-security reading.
            var board = new BoardState(new BoardObjective(targetPosition), jobSecurity);

            // The SeasonState constructor re-validates every cross-field coherence rule (club-set
            // membership of the managed club and of every fixture, calendar coverage, table/club-set
            // agreement) — the decode path's last fail-loud gate.
            return new SeasonState(
                seed,
                seasonNumber,
                managedClubId,
                clubIds,
                fixtures,
                LeagueTable.FromRows(rows),
                calendar,
                board);
        }

        // ── Helpers ───────────────────────────────────────────────────────────────────

        // Reads a u32 length prefix and refuses a count whose elements could not possibly be backed by
        // the bytes that remain. The bound is expressed in ELEMENTS (`remaining / bytesPerElement`)
        // rather than as a byte product `count * bytesPerElement`, because that product can overflow int
        // for a large blob and a crafted count and wrap back to a small positive value that slips past a
        // byte-wise guard — the overflow class the sibling `Require` already defends against. Dividing
        // cannot overflow, so this bound is exact and the per-array `Require` it replaces is unnecessary.
        // Fail-loud, never over-allocate — the WorldStateSerializer.ReadCount posture.
        private static int ReadCount(byte[] blob, ref int o, int total, int bytesPerElement, string what)
        {
            Require(o, 4, total, what + " count");
            uint raw = CanonicalSerializer.ReadU32(blob, ref o);
            int maxCount = (total - o) / bytesPerElement;
            if (raw > (uint)maxCount)
            {
                throw new InvalidOperationException(
                    "Season state " + what + " count " + raw + " exceeds the " + maxCount +
                    " element(s) the " + (total - o) + " remaining byte(s) at offset " + o +
                    " could hold — corrupt save.");
            }

            return (int)raw;
        }

        private static void Require(int offset, int need, int total, string what)
        {
            // Overflow-safe (the MatchSaveCodec / SeasonSaveCodec posture): compare against
            // (total - offset) rather than (offset + need), since a corrupt length prefix can push
            // `need` near int.MaxValue and `offset + need` would wrap negative and slip past the guard.
            // `offset` is always in [0, total] here (every read is guarded), so `total - offset` is a
            // safe non-negative int.
            if (need < 0 || offset > total || need > total - offset)
            {
                throw new InvalidOperationException(
                    "Season state blob truncated reading " + what + " (need " + need +
                    " byte(s) at offset " + offset + " of " + total + ").");
            }
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-07-25 | —      | Initial implementation (#30 T1): Appendix B encode/decode with the |
// |         |            |        | version gate, overflow-safe ReadCount bounds, the serialized-vs-   |
// |         |            |        | derived goal-difference coherence check, the trailing-byte guard,  |
// |         |            |        | and decode-through-the-validating-constructors posture.            |
// | 1.1     | 2026-07-25 | —      | AR pass 1: Decode gains <exception> docs for all four types it     |
// |         |            |        | throws (the fail-loud seam documented only its summary).           |
#endregion
