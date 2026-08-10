// File:     src/player-progression/ProgressionEngine.cs
// Created:  2026-08-08
// Modified: 2026-08-10
// Author:   —
// Spec:     Player Progression & Lifecycle #28 §3.1 / §3.4 / §3.5 / §4.2 / §4.5, KD-4 / KD-7 / KD-8,
//           FR-PG-001/005/008/011/013/014/016/019/021/022/023; ERR-029-006 (the batch entry point);
//           ERR-028-003 (new-game PA); ERR-028-014 (the never-advanced sentinel retired from the
//           legal store states); Code Standards #20
// Purpose:  The sole writer of #28 lifecycle state and of the career roster's evolving attributes
//           (FR-PG-022). Owns the per-club career-state set KD-4 makes the SERIALIZED roster, exposes
//           the FR-PG-021 batch daily entry point #30 drives at slot 1, and projects the current roster
//           back out as Squads so there is exactly one attribute authority at every moment.

using System;
using System.Collections.Generic;

using TacticalDirector.PlayerDatabase;

namespace TacticalDirector.PlayerProgression
{
    /// <summary>
    /// The #28 career-state store and its daily step (KD-7: the single writer).
    /// <para>
    /// <b>This type owns the roster's evolving attributes, and that is the point of KD-4.</b> A
    /// <c>Squad</c> is immutable and is rebuilt from the world seed, so before this landing a player's
    /// <c>[1,20]</c> attributes had nowhere to change and nowhere to persist. Growth therefore could not
    /// be represented at all. From here the bootstrap generates the day-0 roster
    /// (<see cref="SeedFrom"/>), this store evolves it, and <see cref="SquadFor"/> projects the current
    /// state back out — so the roster is a function of the SAVE, not of the seed.
    /// </para>
    /// <para>
    /// <b>One authority, not two reconciled.</b> Consumers must read squads through the projection, never
    /// from the bootstrap product they seeded from. If both are live, every read outside a save/load
    /// boundary takes day-0 attributes while this store advances, and the two agree exactly at the one
    /// moment anything would think to compare them — the silent-divergence shape #29/#41 T2's H2 filed.
    /// </para>
    /// <para>
    /// <b>What this landing deliberately does NOT do.</b> <c>RunSeasonBoundary</c> (retiree removal +
    /// 1:1 regen, §3.4) is not here. Retirement FLAGGING is — it is part of the draw-free daily step —
    /// but applying the roster mutation needs the <c>player-progression.regen</c> stream, whose
    /// survival across a save §3.5 does not pin, and ERR-029-006 closes without it. So this landing has
    /// <b>no draw site at all</b>: no stream registration, no <c>0x20</c> promotion, no digest-version
    /// question.
    /// </para>
    /// </summary>
    public sealed class ProgressionEngine
    {
        // Parallel per-club arrays, index-aligned. _clubIds ascends, so ClubIndexOf can binary-search;
        // within a club, records ascend by PlayerId for the same reason.
        private readonly List<int> _clubIds = new List<int>();
        private readonly List<PlayerRecord[]> _records = new List<PlayerRecord[]>();
        private readonly List<PlayerLifecycle[]> _lifecycles = new List<PlayerLifecycle[]>();

        private int _nextPlayerId;

        private ProgressionEngine()
        {
        }

        /// <summary>
        /// A store carrying no clubs — the honest state of a game that tracks no careers yet, and what a
        /// careerless save writes. Distinct from <c>null</c>, which is refused everywhere the block is
        /// required (FR-PG-017). A fresh instance each time, since this type is mutable.
        /// </summary>
        public static ProgressionEngine Empty => new ProgressionEngine();

        /// <summary>The number of clubs this store carries.</summary>
        public int ClubCount => _clubIds.Count;

        /// <summary>
        /// The monotonic id cursor a future regen allocates from (FR-PG-011). Serialized, so a regen can
        /// never reuse a retiree's id across a save boundary.
        /// </summary>
        public int NextPlayerId => _nextPlayerId;

        /// <summary>True when this store carries career state for <paramref name="clubId"/>.</summary>
        public bool CarriesClub(int clubId) => ClubIndexOf(clubId) >= 0;

        // ── Construction ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Seeds a new career from the bootstrapped day-0 squads — the new-game path. Every player's
        /// <see cref="PlayerLifecycle.BirthWorldDay"/> is derived from his generated
        /// <see cref="PlayerRecord.Age"/> (§3.1.1) and his <see cref="PlayerLifecycle.PotentialAbility"/>
        /// from <see cref="PlayerProgressionConstants.NEW_GAME_PA_HEADROOM"/> above his computed CA.
        /// <para>
        /// <b>The PA seed is a placeholder for #47's authored value (ERR-028-003)</b>, and deliberately
        /// deterministic: a draw here would be #28's first draw site and would force the regen stream to
        /// register for a number the authored database is going to replace.
        /// </para>
        /// </summary>
        /// <param name="squads">The bootstrap's day-0 rosters, one per club. Copied in — the caller's
        /// <c>Squad</c> objects are never retained, so the bootstrap product cannot become a second
        /// live roster.</param>
        /// <param name="newGameWorldDay">The world day the career starts on (the birth-day anchor).</param>
        /// <exception cref="ArgumentNullException"><paramref name="squads"/> or an element is null.</exception>
        /// <exception cref="ArgumentException">Two squads share a club id, or two players share a player
        /// id across the whole set — ids must be globally unique across a career (ERR-041-019 /
        /// ERR-027-004), not merely club-scoped.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="newGameWorldDay"/> is
        /// <see cref="PlayerProgressionConstants.PROGRESSION_NOT_ADVANCED_SENTINEL"/> — the one cursor
        /// value <see cref="FromBlocks"/> refuses (F8 / ERR-028-014).</exception>
        public static ProgressionEngine SeedFrom(Squad[] squads, uint newGameWorldDay)
        {
            if (squads == null)
            {
                throw new ArgumentNullException(nameof(squads));
            }

            // F8 at the seed site (ERR-028-015). Since ERR-028-014 the seed day IS every player's
            // cursor, so seeding on the sentinel writes the one value FromBlocks refuses and AdvanceDay
            // refuses as a day — producing a store that cannot be saved, restored or advanced, and
            // failing far from the call that caused it. AdvanceDay has carried this guard since
            // ERR-028-009; anchoring the cursor made the seed site a second way in, so it carries it too.
            if (newGameWorldDay == PlayerProgressionConstants.PROGRESSION_NOT_ADVANCED_SENTINEL)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(newGameWorldDay), newGameWorldDay,
                    "The never-advanced sentinel is not a legal world day to seed a career on "
                    + "(F8 / ERR-028-014): it is the one cursor value FromBlocks refuses.");
            }

            var engine = new ProgressionEngine();
            var byClub = new SortedDictionary<int, Squad>();

            for (int i = 0; i < squads.Length; i++)
            {
                Squad squad = squads[i];
                if (squad == null)
                {
                    throw new ArgumentNullException(nameof(squads), $"Squad at index {i} is null.");
                }
                if (byClub.ContainsKey(squad.ClubId))
                {
                    throw new ArgumentException(
                        $"Two squads share club id {squad.ClubId} — a career carries one roster per club.",
                        nameof(squads));
                }
                byClub.Add(squad.ClubId, squad);
            }

            int maxPlayerId = int.MinValue;
            foreach (KeyValuePair<int, Squad> entry in byClub)
            {
                Squad squad = entry.Value;
                var records = new PlayerRecord[squad.Count];
                var lifecycles = new PlayerLifecycle[squad.Count];

                for (int p = 0; p < squad.Count; p++)
                {
                    PlayerRecord rec = squad.GetPlayer(p);   // value copy — Squad hands out copies
                    records[p] = rec;
                    lifecycles[p] = SeedLifecycle(in rec, newGameWorldDay);

                    // AR pass 5, and this is the PREVIOUS pass's fix stopping one file short of the gap
                    // its own commit message named ("ProgressionEngine.FromBlocks gates none either").
                    // The codec now refuses out-of-range values at Encode and Decode; the two STORE
                    // construction boundaries still admitted them, so the breach was caught only at the
                    // save — the one boundary where it is unrecoverable. A store built from a squad
                    // carrying WeakFootRating = 0 advances, plays and projects a squad perfectly, and
                    // then cannot be persisted, ever. Same owner as the codec's two calls: the RULE has
                    // one home and every boundary that can admit a breach delegates to it.
                    string outOfRange = ProgressionSaveCodec.DescribeOutOfRangeValues(
                        in rec, in lifecycles[p], entry.Key);
                    if (outOfRange != null)
                    {
                        throw new ArgumentException(
                            "Refusing to seed a career from state the save path would refuse to write: "
                            + outOfRange, nameof(squads));
                    }

                    if (rec.PlayerId > maxPlayerId)
                    {
                        maxPlayerId = rec.PlayerId;
                    }
                }

                SortByPlayerId(records, lifecycles);
                engine._clubIds.Add(entry.Key);
                engine._records.Add(records);
                engine._lifecycles.Add(lifecycles);
            }

            engine.RequireGloballyUniquePlayerIds(nameof(squads));
            engine._nextPlayerId = maxPlayerId == int.MinValue ? 0 : maxPlayerId + 1;
            return engine;
        }

        /// <summary>
        /// Rebuilds a career from decoded save blocks (the restore path).
        /// </summary>
        /// <param name="clubs">The decoded per-club blocks. <b>Copied</b>, not borrowed — the documented
        /// restore path hands in the very arrays the codec just produced, and borrowing them would make
        /// any holder of that decode result a second writer into a running career (the #29/#41 AR pass-3
        /// finding, closed on one route and left open on the other).</param>
        /// <param name="nextPlayerId">The decoded id cursor.</param>
        /// <exception cref="ArgumentNullException"><paramref name="clubs"/> or an inner array is null.</exception>
        /// <exception cref="ArgumentException">Club ids are not strictly ascending, player ids within a
        /// club are not strictly ascending, or a player id repeats across clubs.</exception>
        public static ProgressionEngine FromBlocks(ClubCareerStates[] clubs, int nextPlayerId)
        {
            if (clubs == null)
            {
                throw new ArgumentNullException(nameof(clubs));
            }

            var engine = new ProgressionEngine();
            long previousClubId = long.MinValue;

            for (int c = 0; c < clubs.Length; c++)
            {
                ClubCareerStates club = clubs[c];
                if (club.Records == null || club.Lifecycles == null)
                {
                    throw new ArgumentNullException(
                        nameof(clubs), $"Club block at index {c} was never bound (default(ClubCareerStates)).");
                }
                if (club.ClubId <= previousClubId)
                {
                    throw new ArgumentException(
                        $"Club id {club.ClubId} at index {c} does not exceed the preceding "
                        + $"{previousClubId} — every lookup here is a binary search over ascending ids.",
                        nameof(clubs));
                }
                previousClubId = club.ClubId;

                long previousPlayerId = long.MinValue;
                for (int p = 0; p < club.Count; p++)
                {
                    // ERR-028-014: the never-advanced sentinel is not a legal STORE state, only a
                    // refused world day (F8). SeedFrom anchors the cursor at the seed day, so nothing
                    // writes it; admitting one here would restore the unbounded-span defect through
                    // the one entry point that could still carry it, and would keep alive a branch of
                    // AdvancePlayerTo that no longer exists.
                    if (club.Lifecycles[p].LastAdvancedWorldDay
                        == PlayerProgressionConstants.PROGRESSION_NOT_ADVANCED_SENTINEL)
                    {
                        throw new ArgumentException(
                            $"Player {club.Records[p].PlayerId} in club {club.ClubId} carries the "
                            + "never-advanced sentinel as its progression cursor. A career's lived "
                            + "history has to start somewhere it can be checked against the world "
                            + "clock (ERR-028-014); seed through SeedFrom, which anchors it.",
                            nameof(clubs));
                    }

                    // AR pass 5, the sibling half of the SeedFrom guard above. This is the DOCUMENTED
                    // restore path and it is public, so a caller hand-building blocks — a #47 authored
                    // data loader, a tool, a fixture — got a store that advanced, played and projected
                    // fine and could never be saved. `default(PlayerLifecycle)` is the live trigger:
                    // PotentialAbility = 0, which PA_MIN = 4000 refuses at the codec.
                    string outOfRange = ProgressionSaveCodec.DescribeOutOfRangeValues(
                        in club.Records[p], in club.Lifecycles[p], club.ClubId);
                    if (outOfRange != null)
                    {
                        throw new ArgumentException(
                            "Refusing to build a career from state the save path would refuse to write: "
                            + outOfRange, nameof(clubs));
                    }

                    int id = club.Records[p].PlayerId;
                    if (id <= previousPlayerId)
                    {
                        throw new ArgumentException(
                            $"Player id {id} at index {p} of club {club.ClubId} does not exceed the "
                            + $"preceding {previousPlayerId} — an unordered block makes a carried player "
                            + "un-findable, and the miss reads as 'new'.",
                            nameof(clubs));
                    }
                    previousPlayerId = id;
                }

                var records = new PlayerRecord[club.Count];
                var lifecycles = new PlayerLifecycle[club.Count];
                Array.Copy(club.Records, records, club.Count);
                Array.Copy(club.Lifecycles, lifecycles, club.Count);

                engine._clubIds.Add(club.ClubId);
                engine._records.Add(records);
                engine._lifecycles.Add(lifecycles);
            }

            engine.RequireGloballyUniquePlayerIds(nameof(clubs));

            // M2: the whole point of serializing NextPlayerId is that "a regen can never reuse a
            // retiree's id across a save boundary" (FR-PG-011). A cursor at or behind an id the store
            // already carries defeats that silently — the next allocation collides with a LIVE player,
            // and the collision then reads as one player with two careers.
            int highest = int.MinValue;
            for (int c = 0; c < engine._records.Count; c++)
            {
                PlayerRecord[] recs = engine._records[c];
                if (recs.Length > 0 && recs[recs.Length - 1].PlayerId > highest)
                {
                    highest = recs[recs.Length - 1].PlayerId;   // ascending within a club
                }
            }
            if (highest != int.MinValue && nextPlayerId <= highest)
            {
                throw new ArgumentException(
                    $"The id cursor is {nextPlayerId} but this career already carries player "
                    + $"{highest}. The next allocated id would collide with a live player, which is "
                    + "exactly what serializing the cursor exists to prevent (FR-PG-011).",
                    nameof(nextPlayerId));
            }

            engine._nextPlayerId = nextPlayerId;
            return engine;
        }

        // ── Persistence (§3.5) ────────────────────────────────────────────────────────

        /// <summary>
        /// The per-club blocks for serialization. Returns <b>copies</b> of the state arrays: the store is
        /// the single writer (FR-PG-022), and handing out its live arrays would make every caller a
        /// second one.
        /// </summary>
        public ClubCareerStates[] ToBlocks()
        {
            var blocks = new ClubCareerStates[_clubIds.Count];
            for (int c = 0; c < blocks.Length; c++)
            {
                PlayerRecord[] records = _records[c];
                PlayerLifecycle[] lifecycles = _lifecycles[c];
                var recordCopy = new PlayerRecord[records.Length];
                var lifecycleCopy = new PlayerLifecycle[lifecycles.Length];
                Array.Copy(records, recordCopy, records.Length);
                Array.Copy(lifecycles, lifecycleCopy, lifecycles.Length);
                blocks[c] = new ClubCareerStates(_clubIds[c], recordCopy, lifecycleCopy);
            }
            return blocks;
        }

        /// <summary>Encodes this store as the #28 sub-blob (§3.5).</summary>
        public byte[] Snapshot() => ProgressionSaveCodec.Encode(ToBlocks(), _nextPlayerId);

        /// <summary>Rebuilds a store from a blob written by <see cref="Snapshot"/> (§3.5).</summary>
        public static ProgressionEngine Restore(byte[] blob)
        {
            ClubCareerStates[] clubs = ProgressionSaveCodec.Decode(blob, out int nextPlayerId);
            return FromBlocks(clubs, nextPlayerId);
        }

        // ── The daily step (FR-PG-021 / §3.1) ─────────────────────────────────────────

        /// <summary>
        /// #28's public daily entry point (FR-PG-021) — the batch #30 drives at its KD-2 slot 1 and #29
        /// §3.5 composes against. Advances every carried player to <paramref name="worldDay"/>.
        /// <para>
        /// <b>Idempotent per day, and gap-complete.</b> Each player carries
        /// <see cref="PlayerLifecycle.LastAdvancedWorldDay"/>: a day already advanced is a no-op (which
        /// is what makes #30's twice-per-fixture-day call safe, ERR-030-027), and a day beyond the
        /// cursor replays every intervening day rather than accruing once for the whole gap. The latter
        /// is what makes #28 §5.2's T-PG-DET-002 true as written — age is gap-independent because it is
        /// derived, but the growth cursor is an accumulator and would otherwise lose the skipped days.
        /// </para>
        /// </summary>
        /// <param name="worldDay">The world day to advance to (#30 passes the pre-increment clock).</param>
        /// <param name="trainingInputs">Per-player contributions from #29, or
        /// <see cref="TrainingInputBatch.Neutral"/> for none (FR-PG-009).</param>
        /// <exception cref="ArgumentException">A bound batch does not describe exactly the players being
        /// advanced — a club this store does not carry, or a club whose ids do not match the store's.</exception>
        public void AdvanceDay(uint worldDay, in TrainingInputBatch trainingInputs)
        {
            // F8 (ERR-028-009): the never-advanced sentinel is not a legal world day. Storing it would re-arm the
            // day-0 trap (a player anchored at the sentinel reads as never-advanced forever, so the
            // step is not idempotent), and the gap replay below would wrap at uint.MaxValue and never
            // terminate. Both siblings (#29 TrainingStep, #41 MedicalStep) refuse it for the same
            // reason; #28 adopted their sentinel and must adopt their guard.
            if (worldDay == PlayerProgressionConstants.PROGRESSION_NOT_ADVANCED_SENTINEL)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(worldDay), worldDay,
                    "The never-advanced sentinel is not a legal world day (F8 / ERR-028-009).");
            }

            ValidateBatch(in trainingInputs);

            for (int c = 0; c < _clubIds.Count; c++)
            {
                PlayerRecord[] records = _records[c];
                PlayerLifecycle[] lifecycles = _lifecycles[c];
                // ValidateBatch has established positional agreement, so index c addresses the same club
                // on both sides — no search, and no way to read another club's contributions.
                TrainingInput[] inputs = trainingInputs.IsNeutral ? null : trainingInputs.Clubs[c].Inputs;

                for (int p = 0; p < records.Length; p++)
                {
                    TrainingInput input = inputs == null ? TrainingInput.Neutral : inputs[p];
                    AdvancePlayerTo(ref records[p], ref lifecycles[p], worldDay, in input);
                }
            }
        }

        // Advances one player from his own cursor up to worldDay inclusive, then applies the §3.4
        // retirement flag. Kept private: FR-PG-022 makes the batch step the only way in.
        private static void AdvancePlayerTo(
            ref PlayerRecord rec, ref PlayerLifecycle life, uint worldDay, in TrainingInput input)
        {
            // ERR-028-014: there is no never-advanced branch. Every carried player has a real cursor —
            // SeedFrom anchors it at the seed day and FromBlocks refuses the sentinel — so the only
            // two cases are "there is a gap to replay" and "this day is already done". The branch that
            // used to sit here banked a single day for an arbitrary span, and after the anchoring
            // above nothing production-shaped could reach it: a guard on an unreachable branch ships
            // green forever, which is why it is deleted rather than left as defence in depth.
            if (worldDay > life.LastAdvancedWorldDay)
            {
                for (uint d = life.LastAdvancedWorldDay + 1; d <= worldDay; d++)
                {
                    GrowthProjection.AdvanceDayForPlayer(ref rec, ref life, d, in input, curveEnabled: false);
                }
                life.LastAdvancedWorldDay = worldDay;
            }
            else
            {
                // Already advanced through this day — two separate things are true here, and only one
                // of them is this `return;`'s doing (AR pass 4).
                //
                // Cursor regression is guarded by the `if` CONDITION above, not by this statement: the
                // assignment `life.LastAdvancedWorldDay = worldDay;` sits INSIDE the `if`, so a
                // backward call (worldDay <= cursor) never reaches it regardless of what this branch
                // contains — deleting the whole if/else and running the body unconditionally is what
                // regresses the cursor, and that is what
                // AdvanceDay_BackwardCall_DoesNotRegressTheCursor locks.
                //
                // What this bare `return;` actually buys is narrower, and it is real: without it, a
                // call that advanced nothing (same-day repeat OR backward) falls through to the §3.4
                // retirement evaluation below. For the ERR-030-027 same-day case that is merely
                // redundant (Age and RetirementFlag are unchanged by the empty replay, so re-evaluating
                // is a no-op) — but for a BACKWARD call it is not: a player not yet flagged whose
                // rec.Age already satisfies RETIREMENT_AGE would be flagged on that call, stamping
                // RetirementDay with a day EARLIER than his own cursor. #30 never calls backward (the
                // cursor-vs-clock gates refuse the composition that could), but AdvanceDay is public, so
                // the guard stays. Locked by AdvanceDay_BackwardCall_DoesNotEvaluateRetirement, which
                // isolates the `return;` from the `if` condition above it.
                return;
            }

            // §3.4 daily retirement evaluation: hard at RETIREMENT_AGE, deterministic, no draw
            // (FR-PG-013). Flagged only — the player stays selectable until the season boundary
            // (FR-PG-014), whose roster mutation is the deferred landing's.
            if (!life.RetirementFlag && rec.Age >= PlayerProgressionConstants.RETIREMENT_AGE)
            {
                life.RetirementFlag = true;
                life.RetirementDay = worldDay;
            }
        }

        // ── Projection + observation ──────────────────────────────────────────────────

        /// <summary>
        /// The club's roster as it stands NOW — the single attribute authority (KD-4). Built fresh from
        /// the store's records, so a caller holding the returned <c>Squad</c> holds a snapshot and cannot
        /// write back through it.
        /// </summary>
        /// <param name="clubId">The club to project.</param>
        /// <returns>The projected squad, or <c>null</c> if this store carries no such club (the
        /// <c>ISquadProvider.ResolveByClubId</c> contract).</returns>
        public Squad SquadFor(int clubId)
        {
            int c = ClubIndexOf(clubId);
            if (c < 0)
            {
                return null;
            }

            // One copy, not two (L7). `Squad`'s constructor snapshot-copies what it is handed, so the
            // store's live array is never aliased by the returned squad — the extra defensive Array.Copy
            // that used to sit here bought nothing and doubled the per-projection cost, which is paid
            // once per club per KD-2 slot per world day and grows with every slot that lands.
            return new Squad(clubId, _records[c]);
        }

        /// <summary>
        /// The FR-PG-023 observer-neutral read of one player's lifecycle. Reading never mutates.
        /// </summary>
        /// <exception cref="ArgumentException">The club or player is not carried.</exception>
        public LifecycleViewModel LifecycleView(int clubId, int playerId)
        {
            int c = ClubIndexOf(clubId);
            if (c < 0)
            {
                throw new ArgumentException($"No career state for club {clubId}.", nameof(clubId));
            }
            int p = PlayerIndexOf(c, playerId);
            if (p < 0)
            {
                throw new ArgumentException(
                    $"No career state for player {playerId} in club {clubId}.", nameof(playerId));
            }

            PlayerLifecycle life = _lifecycles[c][p];
            return new LifecycleViewModel(
                playerId,
                _records[c][p].Age,
                life.CurrentAbility,
                life.PotentialAbility,
                life.RetirementFlag,
                life.RetirementDay);
        }

        // ── Internals ─────────────────────────────────────────────────────────────────

        private static PlayerLifecycle SeedLifecycle(in PlayerRecord rec, uint newGameWorldDay)
        {
            int currentAbility = AbilityModel.ComputeCA(in rec.Attributes, rec.Position);

            // ERR-028-003: the authored value is #47's; this is the deterministic stand-in.
            int potentialAbility = Clamp(
                currentAbility + PlayerProgressionConstants.NEW_GAME_PA_HEADROOM,
                PlayerProgressionConstants.PA_MIN,
                PlayerProgressionConstants.ABILITY_MAX);

            // §3.1.1: the one serialized age anchor, and it is SIGNED on purpose (ERR-028-006). A new
            // world starts on day 0, so for every player with a non-zero generated age this is NEGATIVE
            // — the ordinary case, not an edge one. The earlier clamp to 0 made the derived age
            // worldDay/365, which turned the whole league into age 0 on the first daily step.
            long birthWorldDay = (long)newGameWorldDay - (long)rec.Age * PlayerProgressionConstants.DAYS_PER_YEAR;

            return new PlayerLifecycle
            {
                PotentialAbility = potentialAbility,
                CurrentAbility = currentAbility,
                GrowthCursor = 0,
                BirthWorldDay = birthWorldDay,
                RetirementFlag = false,
                RetirementDay = 0,

                // ERR-028-014: the seed day IS the cursor. A generated player describes the roster as
                // of newGameWorldDay — §3.1.1 pins BirthWorldDay so his derived age equals Age0 exactly
                // ON that day — so that day is already accounted for, not a day still to be lived.
                //
                // Leaving the sentinel here was the defect. It made "where does this career's lived
                // history start" unrepresentable, so a store seeded at day 0 and composed against a
                // clock at day 3650 banked ONE day of growth for a decade while every player silently
                // read ten years older. Anchoring it means the ordinary lag rule refuses that pairing
                // with no new gate: the gap machinery and the cursor-vs-clock predicate already
                // handle every case the sentinel was carved out of.
                LastAdvancedWorldDay = newGameWorldDay
            };
        }

        private static int Clamp(int value, int min, int max)
        {
            if (value < min)
            {
                return min;
            }
            return value > max ? max : value;
        }

        // Sorts the two parallel arrays together by PlayerId (insertion sort — squads are ~25 players and
        // this runs once per club at seed time). Keeping them in one routine is what stops the pair from
        // being reordered independently.
        private static void SortByPlayerId(PlayerRecord[] records, PlayerLifecycle[] lifecycles)
        {
            for (int i = 1; i < records.Length; i++)
            {
                PlayerRecord rec = records[i];
                PlayerLifecycle life = lifecycles[i];
                int j = i - 1;
                while (j >= 0 && records[j].PlayerId > rec.PlayerId)
                {
                    records[j + 1] = records[j];
                    lifecycles[j + 1] = lifecycles[j];
                    j--;
                }
                records[j + 1] = rec;
                lifecycles[j + 1] = life;
            }
        }

        // ERR-041-019 / ERR-027-004: #27 KD-3 promises PlayerId uniqueness only WITHIN a club, but a
        // career keyed (ClubId, PlayerId) — and #41's club-less injury draw key — need it globally.
        // #28 is the second allocator to arrive (NextPlayerId), so it enforces the same precondition at
        // its own entry points rather than inheriting an accident of today's formula.
        private void RequireGloballyUniquePlayerIds(string paramName)
        {
            var seen = new Dictionary<int, int>();
            for (int c = 0; c < _clubIds.Count; c++)
            {
                PlayerRecord[] records = _records[c];
                for (int p = 0; p < records.Length; p++)
                {
                    int id = records[p].PlayerId;
                    if (seen.TryGetValue(id, out int owningClub))
                    {
                        throw new ArgumentException(
                            $"Player id {id} appears in both club {owningClub} and club {_clubIds[c]}. "
                            + "A career requires GLOBALLY unique player ids, not merely club-scoped ones "
                            + "(#27 FR-SQ-010 / ERR-041-019) — duplicates would share career and injury "
                            + "state silently.",
                            paramName);
                    }
                    seen.Add(id, _clubIds[c]);
                }
            }
        }

        private void ValidateBatch(in TrainingInputBatch batch)
        {
            if (batch.IsNeutral)
            {
                return;
            }

            ClubTrainingInputs[] clubs = batch.Clubs;

            // A bound batch must cover EVERY carried club, not merely agree wherever it happens to
            // overlap. Without this a batch that silently dropped a club would advance that club's
            // players on Neutral — indistinguishable from a club that genuinely trained neutrally, and
            // exactly the "gaps quietly filled" failure the per-club check below refuses within a club.
            if (clubs.Length != _clubIds.Count)
            {
                throw new ArgumentException(
                    $"The training-input batch covers {clubs.Length} club(s) but this career carries "
                    + $"{_clubIds.Count}. A bound batch describes every club being advanced; use "
                    + "TrainingInputBatch.Neutral to say 'no training anywhere'.",
                    nameof(batch));
            }

            for (int i = 0; i < clubs.Length; i++)
            {
                ClubTrainingInputs club = clubs[i];
                if (club.PlayerIds == null || club.Inputs == null)
                {
                    throw new ArgumentException(
                        $"Training-input batch entry {i} was never bound (default(ClubTrainingInputs)).",
                        nameof(batch));
                }

                // Positional agreement, not a lookup: both this store and the #30-side career hold their
                // clubs in ascending id order, so a correct batch is in the store's own order. Requiring
                // that makes "covers exactly these clubs, once each" structural — no duplicate scan, no
                // per-club search — and turns any drift between the two roster views into a fail-loud
                // here rather than growth attributed to the wrong club.
                if (club.ClubId != _clubIds[i])
                {
                    throw new ArgumentException(
                        $"The training-input batch has club {club.ClubId} at index {i}, but this career "
                        + $"carries club {_clubIds[i]} there. Both sides order clubs by ascending id, so "
                        + "a mismatch means the two roster views have drifted apart.",
                        nameof(batch));
                }

                PlayerRecord[] records = _records[i];
                if (club.PlayerIds.Length != records.Length)
                {
                    throw new ArgumentException(
                        $"The training-input batch supplies {club.PlayerIds.Length} input(s) for club "
                        + $"{club.ClubId}, which carries {records.Length} player(s). A partial batch is "
                        + "refused rather than having its gaps filled with Neutral, which would make a "
                        + "dropped player indistinguishable from an untrained one.",
                        nameof(batch));
                }

                for (int p = 0; p < records.Length; p++)
                {
                    if (club.PlayerIds[p] != records[p].PlayerId)
                    {
                        throw new ArgumentException(
                            $"The training-input batch for club {club.ClubId} is keyed to player "
                            + $"{club.PlayerIds[p]} at index {p}, but this career carries "
                            + $"{records[p].PlayerId} there — the batch would train the wrong player.",
                            nameof(batch));
                    }
                }
            }
        }

        private int ClubIndexOf(int clubId)
        {
            int lo = 0;
            int hi = _clubIds.Count - 1;
            while (lo <= hi)
            {
                int mid = lo + ((hi - lo) >> 1);
                int v = _clubIds[mid];
                if (v == clubId)
                {
                    return mid;
                }
                if (v < clubId)
                {
                    lo = mid + 1;
                }
                else
                {
                    hi = mid - 1;
                }
            }
            return -1;
        }

        private int PlayerIndexOf(int clubIndex, int playerId)
        {
            PlayerRecord[] records = _records[clubIndex];
            int lo = 0;
            int hi = records.Length - 1;
            while (lo <= hi)
            {
                int mid = lo + ((hi - lo) >> 1);
                int v = records[mid].PlayerId;
                if (v == playerId)
                {
                    return mid;
                }
                if (v < playerId)
                {
                    lo = mid + 1;
                }
                else
                {
                    hi = mid - 1;
                }
            }
            return -1;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-08-08 | —      | #28 T1 (ERR-029-006): the sole-writer career store, the        |
// |         |            |        | FR-PG-021 batch AdvanceDay, Snapshot/Restore over §3.5's       |
// |         |            |        | block, the SquadFor projection that makes KD-4's "one roster   |
// |         |            |        | authority" real, and the FR-PG-023 lifecycle view. Per-player  |
// |         |            |        | cursor makes the step idempotent (ERR-030-027) and gap-complete |
// |         |            |        | (T-PG-DET-002). RunSeasonBoundary deliberately deferred — no   |
// |         |            |        | draw site lands here.                                          |
// | 1.1     | 2026-08-09 | —      | ERR-028-014: the never-advanced sentinel is retired from #28's |
// |         |            |        | legal store states. SeedLifecycle anchors LastAdvancedWorldDay |
// |         |            |        | at newGameWorldDay (the seed day IS the cursor, not a marker   |
// |         |            |        | for "not yet lived") — a generated player describes the roster |
// |         |            |        | as of that day, so it is already accounted for. AdvancePlayerTo |
// |         |            |        | loses its never-advanced branch: only "gap to replay" and      |
// |         |            |        | "already done" remain. FromBlocks now refuses a lifecycle       |
// |         |            |        | carrying the sentinel cursor. Fixes the silent-data defect      |
// |         |            |        | where a store seeded on day 0 and composed against a clock at  |
// |         |            |        | day 3650 banked one day of growth for a decade while every      |
// |         |            |        | player silently read ten years older.                          |
// | 1.2     | 2026-08-10 | —      | AR pass 3 (ERR-028-015). SeedFrom gains the F8 sentinel guard —  |
// |         |            |        | anchoring the cursor made the seed site a second way to write   |
// |         |            |        | the one value FromBlocks refuses. AdvancePlayerTo's else-branch |
// |         |            |        | comment CORRECTED: it does not make the ERR-030-027 same-day    |
// |         |            |        | re-run safe (the replay loop is empty by construction there —   |
// |         |            |        | deleting the branch left all 469 tests green). It is            |
// |         |            |        | load-bearing only against a BACKWARD call, which would rewind   |
// |         |            |        | the cursor and replay banked days.                              |
// | 1.3     | 2026-08-10 | —      | AR pass 4. The 1.2 else-branch comment over-attributed the fix: |
// |         |            |        | it called the bare `return;` "load-bearing against a BACKWARD  |
// |         |            |        | call", but cursor regression is guarded by the `if` CONDITION   |
// |         |            |        | (the assignment sits inside it), not by `return;`. Rewritten to |
// |         |            |        | separate the two: the condition guards regression (locked by    |
// |         |            |        | AdvanceDay_BackwardCall_DoesNotRegressTheCursor); `return;`      |
// |         |            |        | additionally stops the §3.4 retirement evaluation from running  |
// |         |            |        | on a non-advancing call, which for a backward call would flag a |
// |         |            |        | player and stamp RetirementDay earlier than his own cursor —    |
// |         |            |        | now isolated by AdvanceDay_BackwardCall_DoesNotEvaluateRetirement |
// |         |            |        | (deleting only `return;`, if/else shell intact, fails it and no |
// |         |            |        | other test). SeedFrom's XML doc gains the ArgumentOutOfRangeException |
// |         |            |        | tag its sentinel guard already throws but was undocumented.     |
#endregion
