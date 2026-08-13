// File:     src/discipline/DisciplineRules.cs
// Created:  2026-08-13
// Modified: 2026-08-13
// Author:   —
// Spec:     Discipline & Suspensions #44 §3.2 (thresholds & bans) / §3.3 (serving) / §3.4 (boundary
//           & hygiene); FR-DC-006/007/011/013/017; F2/F4; Code Standards #20
// Purpose:  The sole mutating entry point onto DisciplineState — card application, the accumulation
//           threshold, ban serving, the season-boundary sweep and the roster re-key/retirement rules.

using System;
using System.Collections.Generic;

using TacticalDirector.PlayerDatabase;

namespace TacticalDirector.Discipline
{
    /// <summary>
    /// Every rule that writes <see cref="DisciplineState"/> (#44 §3.2–§3.4). Constructor-injected over
    /// the state it owns, and the only public way to change it — the state's own mutators are
    /// <c>internal</c> so a caller cannot add a yellow while skipping the threshold, or serve a ban
    /// while skipping the FR-DC-017 drop.
    /// <para>
    /// <b>All integer, no draw, no clock</b> (FR-DC-019/020/021): the same cards in the same order
    /// always produce the same tally. #44 registers no RNG stream, domain tag or subsystem ordinal —
    /// a positive property of the read-only class, not an omission.
    /// </para>
    /// <para>
    /// <b>The <c>[GT]</c>s are guarded here, not in the catalogue.</b> The catalogue's own locks run
    /// config-unbound and therefore see the fallbacks forever, so a shipped config could violate an
    /// invariant every test still reports green on (ERR-041-003, and AR pass 10's severity-split
    /// finding — the same lesson twice-filed). The guards below fire at the site that would otherwise
    /// write the breach.
    /// </para>
    /// </summary>
    public sealed class DisciplineRules
    {
        private readonly DisciplineState _state;

        /// <summary>Wraps the state this instance is the sole writer of.</summary>
        /// <exception cref="ArgumentNullException"><paramref name="state"/> is null.</exception>
        public DisciplineRules(DisciplineState state)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
        }

        /// <summary>The state this instance writes. Read-only from here — every mutator is on this class.</summary>
        public DisciplineState State => _state;

        /// <summary>
        /// Applies one <c>CardIssuedEvent</c> to <paramref name="playerId"/> — the FR-DC-006 dispatch,
        /// which IS the engine's verified emission contract rather than a de-dup heuristic over it:
        /// <list type="bullet">
        /// <item>kind 0 (yellow) → one yellow;</item>
        /// <item>kind 2 (second yellow) → one yellow AND a
        /// <see cref="DisciplineConstants.SecondYellowBanMatches"/> ban, because the engine promotes a
        /// second caution into <b>one</b> event and never emits the red separately;</item>
        /// <item>kind 1 (straight red) → a <see cref="DisciplineConstants.StraightRedBanMatches"/> ban
        /// and no yellow.</item>
        /// </list>
        /// A kind-2 on a player already at the threshold's edge therefore stacks BOTH bans (§3.2's
        /// worked example) — that is FR-DC-007's additive rule, not double-counting.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="cardKind"/> is outside
        /// <c>{0, 1, 2}</c> — <b>F4</b>. Deliberately not the FR-DC-004 ignore posture: an unknown
        /// event ORDINAL is forward compatibility, but a known event carrying an unknown payload value
        /// is an engine-contract change #44 must not guess about.</exception>
        public void ApplyCard(int playerId, int competitionId, byte cardKind)
        {
            switch (cardKind)
            {
                case DisciplineConstants.CARD_KIND_YELLOW:
                    AddYellow(playerId, competitionId);
                    break;

                case DisciplineConstants.CARD_KIND_SECOND_YELLOW:
                    // Validated BEFORE AddYellow runs (M4): a bad SecondYellowBanMatches must refuse
                    // the WHOLE card atomically, not leave the yellow (and any accumulation ban it
                    // triggers) committed while the card itself is refused.
                    int secondYellowBan = RequireBanLength(
                        DisciplineConstants.SecondYellowBanMatches,
                        nameof(DisciplineConstants.SecondYellowBanMatches));
                    AddYellow(playerId, competitionId);
                    AddBan(playerId, competitionId, secondYellowBan);
                    break;

                case DisciplineConstants.CARD_KIND_RED:
                    AddBan(playerId, competitionId, RequireBanLength(
                        DisciplineConstants.StraightRedBanMatches,
                        nameof(DisciplineConstants.StraightRedBanMatches)));
                    break;

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(cardKind), cardKind,
                        "DisciplineRules.ApplyCard: CardKind must be 0 (yellow), 1 (red) or 2 (second " +
                        "yellow) — F4. An unknown kind means the engine's card contract changed; #44 " +
                        "must not guess whether it bans.");
            }
        }

        /// <summary>
        /// Adds one yellow and applies the accumulation threshold (§3.2, FR-DC-007): on reaching
        /// <see cref="DisciplineConstants.YellowAccumulationThreshold"/> the count is reduced BY the
        /// threshold — residual kept, not reset to zero — and an
        /// <see cref="DisciplineConstants.AccumBanMatches"/> ban is added on top of anything already
        /// outstanding.
        /// </summary>
        /// <exception cref="InvalidOperationException">A bound config set
        /// <c>YellowAccumulationThreshold</c> below 1, at which point the residual subtraction cannot
        /// terminate a crossing and every single yellow bans, silently. The catalogue lock cannot see
        /// this — it runs config-unbound (ERR-041-003).</exception>
        public void AddYellow(int playerId, int competitionId)
        {
            int threshold = RequireYellowThreshold(DisciplineConstants.YellowAccumulationThreshold);

            DisciplineEntry entry = _state.EntryFor(playerId, competitionId);
            int yellows = entry.Yellows + 1;
            int ban = entry.BanMatchesRemaining;

            if (yellows >= threshold)
            {
                yellows -= threshold;
                ban += RequireBanLength(
                    DisciplineConstants.AccumBanMatches, nameof(DisciplineConstants.AccumBanMatches));
            }

            _state.Upsert(new DisciplineEntry(playerId, competitionId, yellows, ban));
        }

        /// <summary>
        /// Adds <paramref name="matches"/> to the player's outstanding ban — bans from any source stack
        /// additively (FR-DC-007). A zero-length ban is legal and a no-op; a negative one is a caller bug.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="matches"/> is negative.</exception>
        public void AddBan(int playerId, int competitionId, int matches)
        {
            if (matches < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(matches), matches,
                    "DisciplineRules.AddBan: a ban length must be >= 0 — bans stack additively " +
                    "(FR-DC-007) and nothing in #44 shortens one.");
            }

            DisciplineEntry entry = _state.EntryFor(playerId, competitionId);
            _state.Upsert(new DisciplineEntry(
                playerId, competitionId, entry.Yellows, entry.BanMatchesRemaining + matches));
        }

        /// <summary>
        /// Serves one fixture of every outstanding ban held by a player of <paramref name="clubId"/>
        /// (§3.3, FR-DC-011). Called <b>once per played fixture per club</b>, on either resolution path
        /// — a ban is served by the club playing, not by the engine simulating.
        /// <para>
        /// <b>Club membership is derived, not looked up:</b> <c>PlayerId / CLUB_SQUAD_SIZE == clubId</c>
        /// is #27's club-scoped id formula, and FR-DC-013's migration rule keeps a transferred player's
        /// id current — so no roster read is needed and the serving cannot disagree with a roster this
        /// assembly is not allowed to hold. That derivation rests on #27 FR-SQ-010's global-uniqueness
        /// promise as amended by <b>ERR-027-004</b>; before that amendment ids were unique only within
        /// a club and this method would have served two clubs' bans at once (the ERR-041-019 class).
        /// It ALSO rests on every stored <c>PlayerId</c> being non-negative — C# integer division
        /// truncates toward zero, so a negative id would otherwise derive to club 0 regardless of
        /// uniqueness. That half is enforced separately, at construction: <see cref="DisciplineEntry"/>
        /// (M1, §2.3 F2) and <see cref="DisciplineSaveCodec.Decode"/> both refuse a negative
        /// <c>PlayerId</c>, so no row this method reads can ever carry one.
        /// </para>
        /// <para>A row that reaches <c>(0, 0)</c> here is dropped immediately, mid-season, per FR-DC-017.</para>
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="clubId"/> is negative — <b>F2</b>,
        /// a club outside the resolvable universe. No id divides to a negative club, so this would
        /// otherwise be a silent no-op rather than the caller-contract bug it is.</exception>
        public void OnClubFixturePlayed(int clubId)
        {
            if (clubId < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(clubId), clubId,
                    "DisciplineRules.OnClubFixturePlayed: clubId must be >= 0 (F2). A negative club " +
                    "matches no player id, so serving would silently do nothing.");
            }

            // Walk downward: Upsert may REMOVE the row it just emptied (FR-DC-017), which would shift
            // every later index under an ascending walk and skip a player's ban.
            for (int i = _state.Count - 1; i >= 0; i--)
            {
                DisciplineEntry entry = _state.EntryAt(i);
                if (entry.BanMatchesRemaining <= 0)
                {
                    continue;
                }
                if (entry.PlayerId / PlayerDatabaseConstants.CLUB_SQUAD_SIZE != clubId)
                {
                    continue;
                }

                _state.Upsert(new DisciplineEntry(
                    entry.PlayerId, entry.CompetitionId, entry.Yellows, entry.BanMatchesRemaining - 1));
            }
        }

        /// <summary>
        /// The season-boundary sweep (§3.4, FR-DC-017): every yellow count resets to 0 and every
        /// <b>unserved ban carries</b> — a red card in the last round of May is still a ban in August,
        /// which is the whole reason #44 persists rather than recomputing. A row left at <c>(0, 0)</c>
        /// by the reset is dropped.
        /// </summary>
        public void RollToNextSeason()
        {
            for (int i = _state.Count - 1; i >= 0; i--)
            {
                DisciplineEntry entry = _state.EntryAt(i);
                if (entry.Yellows == 0)
                {
                    continue;
                }

                _state.Upsert(new DisciplineEntry(
                    entry.PlayerId, entry.CompetitionId, 0, entry.BanMatchesRemaining));
            }
        }

        /// <summary>
        /// Re-keys a player's whole discipline history from <paramref name="oldPlayerId"/> to
        /// <paramref name="newPlayerId"/> across every competition (§3.4, FR-DC-013) — tally AND
        /// unserved bans, verbatim. <b>Bans follow the player</b>, the deliberate contrast with #32's
        /// drop rule: a player suspended when he transfers is suspended when he arrives.
        /// <para>A player with no rows is a legitimate no-op — most transfers move a clean player.</para>
        /// </summary>
        /// <exception cref="ArgumentException">The target id already carries a row in a competition the
        /// source also carries — <b>F2</b>. There is no defined winner, and merging two players'
        /// histories silently is worse than refusing.</exception>
        public void MigratePlayerId(int oldPlayerId, int newPlayerId)
        {
            if (oldPlayerId == newPlayerId)
            {
                return;
            }

            // Gathered BEFORE anything is written, for two independent reasons, both of which a
            // walk-and-rewrite loop gets wrong:
            //
            //  (1) Correctness of the walk. Re-keying to a LOWER id inserts the new row ahead of the
            //      cursor, shifting every row between the insertion point and the cursor up by one —
            //      so a descending walk that rewrote in place would step straight over one of them.
            //      With a multi-competition tally that silently strands a whole competition's bans on
            //      an id nobody will ever look up again.
            //  (2) Atomicity of the refusal. F2's conflict check must fail with NOTHING written: a
            //      player carrying rows in two competitions, one of which conflicts, would otherwise
            //      have the non-conflicting one already re-keyed when the throw lands — a half-migrated
            //      player, which is worse than either outcome the caller was choosing between.
            var moving = new List<DisciplineEntry>();
            for (int i = 0; i < _state.Count; i++)
            {
                DisciplineEntry entry = _state.EntryAt(i);
                if (entry.PlayerId != oldPlayerId)
                {
                    continue;
                }
                if (_state.HasEntry(newPlayerId, entry.CompetitionId))
                {
                    throw new ArgumentException(
                        "DisciplineRules.MigratePlayerId: player " + newPlayerId + " already carries a " +
                        "discipline row in competition " + entry.CompetitionId + ", and player " +
                        oldPlayerId + " is being re-keyed onto it (F2). Two histories have no defined " +
                        "merge — one player's cards would silently disappear.",
                        nameof(newPlayerId));
                }

                moving.Add(entry);
            }

            for (int i = 0; i < moving.Count; i++)
            {
                DisciplineEntry entry = moving[i];
                _state.Remove(oldPlayerId, entry.CompetitionId);
                _state.Upsert(new DisciplineEntry(
                    newPlayerId, entry.CompetitionId, entry.Yellows, entry.BanMatchesRemaining));
            }
        }

        /// <summary>
        /// Drops every row for a retired player (§3.4, FR-DC-013). Unlike a transfer, there is nobody
        /// for the ban to follow. A player with no rows is a no-op.
        /// </summary>
        public void DropPlayer(int playerId)
        {
            for (int i = _state.Count - 1; i >= 0; i--)
            {
                DisciplineEntry entry = _state.EntryAt(i);
                if (entry.PlayerId == playerId)
                {
                    _state.Remove(playerId, entry.CompetitionId);
                }
            }
        }

        /// <summary>
        /// Fail-loud gate on the <c>[GT]</c> <see cref="DisciplineConstants.YellowAccumulationThreshold"/>
        /// at the one site that reads it. Extracted out of <see cref="AddYellow"/> so the guard is
        /// directly testable (L5, see <see cref="RequireBanLength"/>'s remark for why).
        /// </summary>
        /// <exception cref="InvalidOperationException"><paramref name="threshold"/> is below 1 — the
        /// residual subtraction can never terminate a crossing, so every single yellow would ban,
        /// silently.</exception>
        internal static int RequireYellowThreshold(int threshold)
        {
            if (threshold < 1)
            {
                throw new InvalidOperationException(
                    "DisciplineRules.AddYellow: YellowAccumulationThreshold is " + threshold +
                    "; it must be >= 1. Below 1 every yellow crosses the threshold and the residual " +
                    "subtraction never brings the count back under it — a config edit would silently " +
                    "ban a player on his first booking, forever.");
            }
            return threshold;
        }

        /// <summary>
        /// Fail-loud gate on a <c>[GT]</c> ban length at the site that would otherwise write it. The
        /// catalogue's locks run config-unbound and see the fallback forever (ERR-041-003), so a
        /// negative length shipped in a config would reach <see cref="DisciplineEntry"/>'s constructor
        /// as a confusing arithmetic error rather than a config error.
        /// <para>
        /// <b>L5:</b> <c>internal</c> rather than <c>private</c> so this exact guard — the one
        /// <see cref="AddYellow"/> and <see cref="ApplyCard"/> actually run — is directly testable.
        /// <see cref="DisciplineConstants"/>' <c>[GT]</c> fields are <c>public static readonly</c>,
        /// resolved once at type initialisation; no test in this process can bind a bad config value
        /// before that first read happens, so driving the guard through the config-reading call sites
        /// can never observe a config-driven breach. Calling this method (and
        /// <see cref="RequireYellowThreshold"/>) directly reaches the identical guarded code with an
        /// explicit value instead — the cheapest honest seam that does not require a config-loader
        /// composition root this project does not have yet.
        /// </para>
        /// </summary>
        /// <exception cref="InvalidOperationException"><paramref name="matches"/> is negative.</exception>
        internal static int RequireBanLength(int matches, string constantName)
        {
            if (matches < 0)
            {
                throw new InvalidOperationException(
                    "DisciplineRules: [GT] " + constantName + " is " + matches +
                    "; a ban length must be >= 0. A negative value in a bound config would shorten " +
                    "bans a different offence had already earned.");
            }
            return matches;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                            |
// | 1.0     | 2026-08-13 | —      | Initial implementation (#44 T0, roadmap C1): the FR-DC-006 card  |
// |         |            |        | dispatch, the §3.2 threshold-and-residual accumulation, §3.3     |
// |         |            |        | serving by derived club, and the §3.4 boundary/re-key/retirement |
// |         |            |        | hygiene. [GT] guards sit at the writing sites, not the catalogue |
// |         |            |        | (ERR-041-003's twice-filed lesson).                              |
// | 1.1     | 2026-08-13 | —      | Self-review before the AR pass: MigratePlayerId gathered the      |
// |         |            |        | moving rows BEFORE writing any. Re-keying to a LOWER id inserts   |
// |         |            |        | the new row ahead of a descending cursor, shifting the rows       |
// |         |            |        | between them up by one, so the old walk stepped over one — a      |
// |         |            |        | multi-competition tally silently stranded a competition's bans    |
// |         |            |        | on an id nobody would look up again. The same change makes the    |
// |         |            |        | F2 conflict refusal atomic: a player with one conflicting and     |
// |         |            |        | one clean competition no longer ends up half-migrated.            |
// | 1.2     | 2026-08-13 | —      | AR fixes. M1: OnClubFixturePlayed's doc now names the negative-  |
// |         |            |        | PlayerId half of the club-derivation hazard and where it is now   |
// |         |            |        | closed (DisciplineEntry's constructor / Decode). M4: ApplyCard's  |
// |         |            |        | kind-2/kind-1 branches now route SecondYellowBanMatches and       |
// |         |            |        | StraightRedBanMatches through RequireBanLength, and the kind-2    |
// |         |            |        | branch validates BEFORE AddYellow runs so the card applies        |
// |         |            |        | atomically or not at all. L5: the threshold check AddYellow ran   |
// |         |            |        | inline is extracted to internal RequireYellowThreshold, and       |
// |         |            |        | RequireBanLength goes private -> internal, so both [GT] guards    |
// |         |            |        | are directly testable without depending on GameplayConfigHolder   |
// |         |            |        | binding before DisciplineConstants' static readonly fields        |
// |         |            |        | resolve, which no test in this process can guarantee.             |
#endregion
