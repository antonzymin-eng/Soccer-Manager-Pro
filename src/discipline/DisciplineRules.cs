// File:     src/discipline/DisciplineRules.cs
// Created:  2026-08-13
// Modified: 2026-08-16, yet later (final fixer pass — L2's second half: AddYellow's
//           `entry.Yellows + 1` and AddBan's `entry.BanMatchesRemaining + matches` now route through
//           DisciplineEntry.YellowsPlusOne/BanMatchesPlus, so an accumulator overflow throws
//           OverflowException BEFORE any write instead of tripping the constructor's range guard with
//           a misleading "counting bug" message mid-commit — v1.9)
// Modified: 2026-08-16, later (adversarial-review M2, doc only — RequireBanLength and
//           RequireYellowThreshold carry the pointer that adding a guarded [GT] is a five-site change,
//           and name the completeness test that detects the drift — v1.8)
// Modified: 2026-08-16 (ERR-044-014, adversarial-review H1 — OnClubFixturePlayed takes the club's
//           roster ids and matches membership by presence, deleting the packed-id club derivation that
//           nothing validated and that disagrees with the removal half the moment a transfer or an
//           id-space widening lands — v1.7)
// Modified: 2026-08-15, later (reviewed findings pass, L4 — v1.6: a prose reference to
//           LEAGUE_COMPETITION_KEY renamed for that constant's ALL_CAPS -> LeagueCompetitionKey rename
//           (DisciplineConstants.cs v1.5). Doc-only, no behaviour change.)
// Modified: 2026-08-15 (ERR-044-003 stage 1 — OnClubFixturePlayed now takes the club's fielded eleven
//           and exempts anyone in it, so an extremis appearance no longer serves the ban it was fielded
//           through; owner decision, the first of three staged tiers — v1.5)
// Author:   —
// Spec:     Discipline & Suspensions #44 §3.2 (thresholds & bans) / §3.3 (serving) / §3.4 (boundary
//           & hygiene); FR-DC-006/007/011/013/017; F2/F4; Code Standards #20
// Purpose:  The sole mutating entry point onto DisciplineState — card application, the accumulation
//           threshold, ban serving, the season-boundary sweep and the roster re-key/retirement rules.

using System;
using System.Collections.Generic;

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
                case DisciplineConstants.CardKindYellow:
                    AddYellow(playerId, competitionId);
                    break;

                case DisciplineConstants.CardKindSecondYellow:
                    ApplySecondYellow(
                        playerId, competitionId, DisciplineConstants.SecondYellowBanMatches);
                    break;

                case DisciplineConstants.CardKindRed:
                    ApplyStraightRed(
                        playerId, competitionId, DisciplineConstants.StraightRedBanMatches);
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
        /// The kind-2 (second yellow) branch of <see cref="ApplyCard"/>, with the ban length taken as a
        /// PARAMETER rather than read directly off
        /// <see cref="DisciplineConstants.SecondYellowBanMatches"/> (L11) — the same L5 seam
        /// <see cref="RequireBanLength"/> is <c>internal</c> for. That constant is
        /// <c>public static readonly</c>, resolved once at type initialisation to its non-negative
        /// default, so nothing exercised through the public <see cref="ApplyCard"/> can ever observe
        /// the M4 atomicity guarantee below — a test would need a bound config this process cannot
        /// produce. Calling this method directly with an explicit invalid length reaches the identical
        /// guarded code and can genuinely tell "validated before <see cref="AddYellow"/>" from
        /// "validated after".
        /// <para>
        /// Validated BEFORE <see cref="AddYellow"/> runs (M4): a bad ban length must refuse the WHOLE
        /// card atomically, not leave the yellow (and any accumulation ban it triggers) committed while
        /// the card itself is refused.
        /// </para>
        /// </summary>
        /// <exception cref="InvalidOperationException"><paramref name="secondYellowBanMatches"/> is negative.</exception>
        internal void ApplySecondYellow(int playerId, int competitionId, int secondYellowBanMatches)
        {
            int ban = RequireBanLength(
                secondYellowBanMatches, nameof(DisciplineConstants.SecondYellowBanMatches));
            AddYellow(playerId, competitionId);
            AddBan(playerId, competitionId, ban);
        }

        /// <summary>
        /// The kind-1 (straight red) branch of <see cref="ApplyCard"/>, with the ban length taken as a
        /// PARAMETER (M19) rather than read directly off
        /// <see cref="DisciplineConstants.StraightRedBanMatches"/> — the same L5/L11 seam
        /// <see cref="ApplySecondYellow"/> exists for. That constant is <c>public static readonly</c>,
        /// resolved once at type initialisation to its non-negative default, so nothing exercised
        /// through the public <see cref="ApplyCard"/> can ever observe <see cref="RequireBanLength"/>
        /// refusing it — the prior <c>ApplyCard_Kind1_RoutesStraightRedBanMatchesThroughRequireBanLength</c>
        /// test asserted only the successful path and could not tell "the guard runs" from "the guard
        /// was deleted". Calling this method directly with an explicit invalid length reaches the
        /// identical guarded code.
        /// </summary>
        /// <exception cref="InvalidOperationException"><paramref name="straightRedBanMatches"/> is negative.</exception>
        internal void ApplyStraightRed(int playerId, int competitionId, int straightRedBanMatches)
        {
            int ban = RequireBanLength(
                straightRedBanMatches, nameof(DisciplineConstants.StraightRedBanMatches));
            AddBan(playerId, competitionId, ban);
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
        /// <exception cref="OverflowException"><paramref name="playerId"/>'s existing
        /// <see cref="DisciplineEntry.Yellows"/> is already <see cref="int.MaxValue"/> — refused BEFORE
        /// the increment via <see cref="DisciplineEntry.YellowsPlusOne"/>, so the entry is never
        /// written with a value that silently wrapped negative and would otherwise surface here as a
        /// misleading "counting bug" range refusal instead of what it actually is (L2).</exception>
        public void AddYellow(int playerId, int competitionId)
        {
            int threshold = RequireYellowThreshold(DisciplineConstants.YellowAccumulationThreshold);

            DisciplineEntry entry = _state.EntryFor(playerId, competitionId);
            int yellows = DisciplineEntry.YellowsPlusOne(entry.Yellows);
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
        /// <exception cref="OverflowException"><paramref name="playerId"/>'s existing
        /// <see cref="DisciplineEntry.BanMatchesRemaining"/> plus <paramref name="matches"/> would
        /// exceed <see cref="int.MaxValue"/> — refused BEFORE the addition via
        /// <see cref="DisciplineEntry.BanMatchesPlus"/>, for the identical reason
        /// <see cref="AddYellow"/> is guarded (L2).</exception>
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
                playerId, competitionId, entry.Yellows,
                DisciplineEntry.BanMatchesPlus(entry.BanMatchesRemaining, matches)));
        }

        /// <summary>
        /// Serves one fixture of every outstanding ban held by a player of <paramref name="clubId"/>
        /// who did <b>not</b> take part in it (§3.3, FR-DC-011). Called <b>once per played fixture per
        /// club</b>, on either resolution path — a ban is served by the club playing, not by the engine
        /// simulating.
        /// <para>
        /// <b>Why the fielded eleven is a parameter (ERR-044-003, staged answer).</b> The rule is not
        /// "the club played" but "the club played without him" — a match a suspended man appears in is
        /// not a match of his ban served, which is the whole meaning of a suspension. Under #30 §2.3
        /// F9's depleted-squad back-fill a banned player CAN reach the pitch in extremis (see
        /// <c>AvailabilityComposition</c>), and serving his ban for that fixture made the appearance
        /// strictly free: he played AND the ban shortened, so a two-match red could cost a
        /// mass-suspension club nothing at all. Exempting him keeps #30's liveness invariant — the
        /// fixture is still played, the season still advances — while restoring the ban's cost. It is
        /// the first stage of the fuller answer (youth call-ups, then generated cover, ahead of any
        /// suspended player); those tiers need #42 and an id-space widening, this one needs an argument.
        /// </para>
        /// <para>
        /// <b>Normal fixtures are unaffected, by construction.</b> The availability filter removes every
        /// suspended player before selection, so the only way an id with an outstanding ban appears in
        /// <paramref name="fieldedPlayerIds"/> at all is the extremis back-fill. On every fixture that
        /// is not one, this walk skips nobody and behaves exactly as it did before the parameter existed.
        /// </para>
        /// <para>
        /// <b>Competition granularity.</b> The exemption matches on player id alone, at the same
        /// granularity as the club walk below — which already serves EVERY competition's ban on any
        /// played fixture. Both are exact while <c>LeagueCompetitionKey</c> is the only competition
        /// that exists; a real multi-competition calendar (#43) must revisit them together, since a
        /// league fixture should serve a league ban and leave a cup ban alone.
        /// </para>
        /// <para>
        /// <b>Club membership is LOOKED UP, not derived (ERR-044-014).</b> Whose ban this fixture
        /// serves is decided by presence in <paramref name="clubPlayerIds"/> — the club's actual
        /// roster, supplied by the caller that holds it — and by nothing else. Until August 16, 2026
        /// this walk instead computed <c>entry.PlayerId / CLUB_SQUAD_SIZE == clubId</c>, #27's packed
        /// club-scoped id formula, while the removal half of the same subsystem
        /// (<see cref="Availability.MarkSuspended"/>) walked the real squad and read
        /// <c>PlayerRecord.PlayerId</c>. Two notions of "is this player at this club" that agree only
        /// while the packing holds: the moment they disagree — #31 transfers, or the ERR-044-003
        /// stage 3 id-space widening that is already required — a banned player is removed from every
        /// squad he is really in and his ban is never decremented, so he is suspended forever, with no
        /// throw, no log and no test able to see it. Both the code and #44 §3.3 asserted the
        /// precondition was held by FR-DC-013's migration rule; it is not, because
        /// <see cref="MigratePlayerId"/> and <see cref="DropPlayer"/> have no production caller at all
        /// (recorded at <c>src/season-save/SeasonLoop.cs</c>'s <c>RollToNextSeason</c> roster sync).
        /// This is ERR-041-019's defect one subsystem over, and it is closed the same way: one notion
        /// of membership, taken from the roster, enforced at the entry point.
        /// </para>
        /// <para>
        /// <b><paramref name="clubId"/> is now identity and caller-contract only.</b> It names which
        /// club's fixture this is — for the §2.3 <b>F2</b> gate below, for diagnostics, and for the
        /// competition granularity #43 must eventually revisit — and it participates in no matching
        /// decision. Deliberately not cross-checked against <paramref name="clubPlayerIds"/>: any such
        /// check would have to re-derive a club from a player id, which is the second notion of
        /// membership this change exists to delete.
        /// </para>
        /// <para>A row that reaches <c>(0, 0)</c> here is dropped immediately, mid-season, per FR-DC-017.</para>
        /// </summary>
        /// <param name="clubId">The club that played the fixture. Identity only — see above.</param>
        /// <param name="clubPlayerIds">The club's roster: every player id currently at
        /// <paramref name="clubId"/>, whether or not he was available for this fixture. Never null, for
        /// the same reason <paramref name="fieldedPlayerIds"/> is not — a caller who cannot name the
        /// roster cannot name whose ban a fixture served, and the two silent readings of the unknown
        /// case ("serve everybody" / "serve nobody") are respectively this method's old defect and a
        /// permanently-suspended squad.</param>
        /// <param name="fieldedPlayerIds">The eleven this club actually fielded. Never null: a caller
        /// that does not know who played cannot know whose ban was served either, and defaulting the
        /// unknown case to "serve everybody" is precisely the silent-loss shape this project has twice
        /// filed (the #44 H1/H4 chain) — so the ignorance is refused rather than absorbed.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="clubId"/> is negative — <b>F2</b>,
        /// a club outside the resolvable universe. Kept as a caller-contract gate now that no id is
        /// divided into a club: a caller passing a negative club is wrong about something, and the
        /// roster it passed alongside cannot be trusted to be the club it meant.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="clubPlayerIds"/> or
        /// <paramref name="fieldedPlayerIds"/> is null.</exception>
        public void OnClubFixturePlayed(int clubId, int[] clubPlayerIds, int[] fieldedPlayerIds)
        {
            if (clubId < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(clubId), clubId,
                    "DisciplineRules.OnClubFixturePlayed: clubId must be >= 0 (F2). A negative club " +
                    "is outside the resolvable universe, so the roster supplied beside it cannot be " +
                    "the club the caller meant.");
            }
            if (clubPlayerIds == null)
            {
                throw new ArgumentNullException(
                    nameof(clubPlayerIds),
                    "DisciplineRules.OnClubFixturePlayed: the club's roster is required (F2, "
                    + "ERR-044-014). Membership is read from it, never derived from the id packing, "
                    + "so a caller that cannot name the roster cannot name whose ban this fixture "
                    + "served — and both silent readings of the unknown case are wrong.");
            }
            if (fieldedPlayerIds == null)
            {
                throw new ArgumentNullException(
                    nameof(fieldedPlayerIds),
                    "DisciplineRules.OnClubFixturePlayed: the fielded eleven is required. A ban is "
                    + "served by the club playing WITHOUT the banned player, so serving cannot be "
                    + "decided without knowing who took part (ERR-044-003).");
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
                if (!IsOnClubRoster(entry.PlayerId, clubPlayerIds))
                {
                    // Not one of this club's players — the SAME question, asked the SAME way, as the
                    // removal half asks of the same squad (ERR-044-014).
                    continue;
                }
                if (WasFielded(entry.PlayerId, fieldedPlayerIds))
                {
                    // The extremis case, and the only way a banned id reaches here. He played, so this
                    // fixture is not one of his ban — ERR-044-003.
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
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="newPlayerId"/> is negative —
        /// <b>F2</b>. Checked here, before the first row is written, rather than left for
        /// <see cref="DisciplineEntry"/>'s own constructor to refuse mid-write (M15): a walk that
        /// removed the source row and then let the constructor throw on the negative target would
        /// delete that row and abort with the player's remaining competitions untouched — the exact
        /// non-atomic footgun the gather-then-write split below exists to close for the conflict
        /// case.</exception>
        public void MigratePlayerId(int oldPlayerId, int newPlayerId)
        {
            if (oldPlayerId == newPlayerId)
            {
                return;
            }

            if (newPlayerId < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(newPlayerId), newPlayerId,
                    "DisciplineRules.MigratePlayerId: newPlayerId must be >= 0 (F2). " +
                    "DisciplineEntry's own constructor refuses a negative PlayerId, and validating "
                    + "only in the write loop — after the first row has already been removed from "
                    + "the source id — would delete that row and abort with the rest of the player's "
                    + "history stranded on neither id.");
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
        /// Whether <paramref name="playerId"/> is one of the ids a club fielded. A linear scan over
        /// eleven entries, called once per outstanding ban per played club fixture — the season loop,
        /// not the 60 Hz path — so the array stays an array rather than becoming a set the caller has
        /// to build and this assembly has to trust the contents of.
        /// </summary>
        private static bool WasFielded(int playerId, int[] fieldedPlayerIds) =>
            ContainsId(fieldedPlayerIds, playerId);

        /// <summary>
        /// Whether <paramref name="playerId"/> is on the club's roster — the one membership rule
        /// (ERR-044-014), matched by presence exactly as <see cref="Availability.MarkSuspended"/>
        /// matches the squad it walks. A linear scan over at most a squad's worth of ids, run only for
        /// a row that already carries an outstanding ban, on the season path.
        /// </summary>
        private static bool IsOnClubRoster(int playerId, int[] clubPlayerIds) =>
            ContainsId(clubPlayerIds, playerId);

        /// <summary>The shared scan behind <see cref="WasFielded"/> and <see cref="IsOnClubRoster"/> —
        /// one copy, so the two questions cannot drift into two different answers.</summary>
        private static bool ContainsId(int[] ids, int playerId)
        {
            for (int i = 0; i < ids.Length; i++)
            {
                if (ids[i] == playerId)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Fail-loud gate on the <c>[GT]</c> <see cref="DisciplineConstants.YellowAccumulationThreshold"/>
        /// at the one site that reads it. Extracted out of <see cref="AddYellow"/> so the guard is
        /// directly testable (L5, see <see cref="RequireBanLength"/>'s remark for why).
        /// </summary>
        /// <exception cref="InvalidOperationException"><paramref name="threshold"/> is below 1 — the
        /// residual subtraction can never terminate a crossing, so every single yellow would ban,
        /// silently.</exception>
        /// <remarks>
        /// M2: a <b>new</b> guarded <c>[GT]</c> needs a guard of this shape here AND an entry in both
        /// <c>CardLedgerFold.RequireCommittableConfig</c> forms, <c>CommitWithExplicitConfig</c>'s
        /// parameters and #44 §2.3 F6's list — see <see cref="RequireBanLength"/>'s remark.
        /// </remarks>
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
        /// <para>
        /// <b>M2 — adding a guarded <c>[GT]</c> is a five-site change.</b> A guard here is only half of
        /// it: the constant must also reach both <c>CardLedgerFold.RequireCommittableConfig</c> forms
        /// and <c>CommitWithExplicitConfig</c>'s parameter list (so the round-level pre-flight refuses
        /// before anything is written), and #44 §2.3 F6's guard list in the spec. Nothing enforces that
        /// mechanically, so <c>DisciplineConfigCompletenessTests</c> asserts the guarded set equals
        /// <see cref="DisciplineConstants"/>' config-settable set — extend the pre-flight and that test
        /// together, or the new constant ships with the very silent-breach exposure this method exists
        /// to close. The recorded eventual shape is one validated <c>DisciplineConfig</c> struct rather
        /// than a chain of guards; it is gated on the <c>GameplayConfigHolder.Bind</c> composition-root
        /// pass, which no production caller runs yet, so it buys nothing today.
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
// | 1.3     | 2026-08-13 | —      | AR round 3 fix (L11): ApplyCard's kind-2 branch extracted to      |
// |         |            |        | internal ApplySecondYellow(playerId, competitionId,               |
// |         |            |        | secondYellowBanMatches) — the ban length taken as a parameter     |
// |         |            |        | rather than read off DisciplineConstants directly, the same L5    |
// |         |            |        | seam RequireBanLength/RequireYellowThreshold exist for. The prior |
// |         |            |        | test (ApplyCard_Kind2_ValidatesSecondYellowBanMatches_Before-     |
// |         |            |        | AddYellowRuns) could not discriminate order at the non-negative   |
// |         |            |        | default; DisciplineRulesTests now drives ApplySecondYellow         |
// |         |            |        | directly with an explicit -1 and asserts AddYellow's effect is    |
// |         |            |        | absent, plus an equivalence test proving ApplyCard genuinely      |
// |         |            |        | delegates rather than carrying a parallel copy of the same logic. |
// | 1.4     | 2026-08-13 | —      | AR round 5 fixes. M15: MigratePlayerId now validates              |
// |         |            |        | newPlayerId >= 0 in the gather phase, before the first row is      |
// |         |            |        | ever removed — the round-1 M1 fix taught DisciplineEntry's        |
// |         |            |        | constructor to refuse a negative id, but the write loop below     |
// |         |            |        | had no matching pre-check, so it deleted the source's first row   |
// |         |            |        | and then let the constructor throw on the negative target,        |
// |         |            |        | aborting with the rest of the player's history stranded. M19(a):  |
// |         |            |        | ApplyCard's kind-1 branch extracted to internal ApplyStraightRed  |
// |         |            |        | (playerId, competitionId, straightRedBanMatches) — the L11        |
// |         |            |        | ApplySecondYellow shape applied to the sibling branch, so         |
// |         |            |        | RequireBanLength's kind-1 guard is directly testable the same     |
// |         |            |        | way. L17: CARD_KIND_YELLOW/RED/SECOND_YELLOW re-tagged [CROSS]    |
// |         |            |        | (they are #17 CardIssuedEvent.CardKind domain ordinals consumed   |
// |         |            |        | read-only, not values #44 owns) and renamed PascalCase —          |
// |         |            |        | CardKindYellow/Red/SecondYellow — per src/CLAUDE.md §3.2.3.       |
// | 1.5     | 2026-08-15 | —      | ERR-044-003 stage 1, owner decision. OnClubFixturePlayed gains a |
// |         |            |        | required int[] fieldedPlayerIds and skips any banned player in    |
// |         |            |        | it. The C1/C2 landing recorded, as an open owner call, that the   |
// |         |            |        | #30 §2.3 F9 back-fill can field a suspended player in extremis    |
// |         |            |        | AND that this method then decremented his ban for that same       |
// |         |            |        | fixture — making the appearance strictly free and a two-match     |
// |         |            |        | red cost a depleted club nothing. A ban is served by the club     |
// |         |            |        | playing WITHOUT him, so the eleven is now an input rather than    |
// |         |            |        | an assumption. Behaviour-identical on every fixture that does     |
// |         |            |        | not reach the extremis tier, because the availability filter      |
// |         |            |        | removes suspended players before selection and nothing else can   |
// |         |            |        | put a banned id in the eleven. Required, not optional: an         |
// |         |            |        | omitting call site would silently restore the old behaviour,      |
// |         |            |        | which is this landing's own H1/H4 chain.                          |
// | 1.6     | 2026-08-15, later | — | Reviewed findings pass, L4. A prose LEAGUE_COMPETITION_KEY      |
// |         |            |        | reference renamed to LeagueCompetitionKey. Doc-only, no            |
// |         |            |        | behaviour change.                                                  |
// | 1.7     | 2026-08-16 | —      | ERR-044-014 (adversarial review, H1). OnClubFixturePlayed gains  |
// |         |            |        | a required int[] clubPlayerIds and decides membership by          |
// |         |            |        | PRESENCE in it; the PlayerId / CLUB_SQUAD_SIZE == clubId          |
// |         |            |        | derivation is deleted, and with it this file's dependency on      |
// |         |            |        | TacticalDirector.PlayerDatabase. The derivation was a SECOND      |
// |         |            |        | notion of club membership beside Availability.MarkSuspended's,    |
// |         |            |        | which walks the real squad and reads PlayerRecord.PlayerId; the   |
// |         |            |        | two agree only while #27's packing holds, and the moment they     |
// |         |            |        | disagree (a #31 transfer, or the ERR-044-003 stage 3 id-space     |
// |         |            |        | widening) a banned player is removed from every squad he is       |
// |         |            |        | really in while his ban never decrements — suspended forever,     |
// |         |            |        | silently. The precondition both this file and #44 §3.3 cited      |
// |         |            |        | (FR-DC-013's migration rule keeping ids current) is not held:     |
// |         |            |        | MigratePlayerId and DropPlayer have no production caller.         |
// |         |            |        | clubId is retained for the F2 caller-contract gate and identity   |
// |         |            |        | alone and is deliberately NOT cross-checked against the roster,   |
// |         |            |        | since any such check re-derives the club from an id. Behaviour-   |
// |         |            |        | identical on today's packed ids over a full roster, asserted by   |
// |         |            |        | DisciplineRulesTests' agreement lock; the disagreement case is    |
// |         |            |        | locked separately and follows the roster.                         |
// | 1.8     | 2026-08-16, later | — | Adversarial review, M2 (minimal fix), doc only. The two [GT]     |
// |         |            |        | guards now record that a NEW guarded constant is a five-site      |
// |         |            |        | change — this guard, both CardLedgerFold.RequireCommittableConfig |
// |         |            |        | forms, CommitWithExplicitConfig's parameters and #44 §2.3 F6's    |
// |         |            |        | list — and name DisciplineConfigCompletenessTests, which asserts  |
// |         |            |        | the guarded set still equals DisciplineConstants' config-settable |
// |         |            |        | set. The DisciplineConfig struct restructure that would make the  |
// |         |            |        | drift structurally impossible is recorded as the eventual owner-  |
// |         |            |        | shape and gated on the GameplayConfigHolder.Bind composition-root |
// |         |            |        | pass no production caller runs yet. No behaviour change.          |
// | 1.9     | 2026-08-16, yet later | — | Final fixer pass — L2's second half. AddYellow's        |
// |         |            |        | `entry.Yellows + 1` now reads DisciplineEntry.YellowsPlusOne(entry.Yellows); |
// |         |            |        | AddBan's `entry.BanMatchesRemaining + matches` now reads          |
// |         |            |        | DisciplineEntry.BanMatchesPlus(entry.BanMatchesRemaining, matches). Both    |
// |         |            |        | guarded helpers landed at DisciplineEntry.cs v1.3 (L2, first half) but were |
// |         |            |        | unwired pending this file's ownership. An accumulator at int.MaxValue now   |
// |         |            |        | throws OverflowException before the write, entry unmodified, rather than    |
// |         |            |        | silently wrapping negative and tripping the constructor's "counting bug"    |
// |         |            |        | range guard on the wrapped result. No other call site of either addition   |
// |         |            |        | shape exists in this file (AddYellow's separate `ban += RequireBanLength   |
// |         |            |        | (AccumBanMatches)` line is a different addition, not in this pass's scope, |
// |         |            |        | and was not named by the handoff).                                          |
#endregion
