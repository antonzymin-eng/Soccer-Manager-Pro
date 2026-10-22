// File:     src/player-progression/ProgressionEngine.cs
// Created:  2026-08-08
// Modified: 2026-10-20 (AR round 5 L17 — the AdvanceDay backward-guard comment no longer says "AdvanceDay is public"; comment-only — v1.14)
//           2026-10-20 (AR round 4 M13 / L12 / L16 — AdvanceDay / PrepareSeasonBoundary / CommitSeasonBoundary demoted to internal; SeedFrom doc; Prepare refuses a worldDay outside the cursor span — v1.13)
//           2026-10-20 (AR round 3 M13 — CanMovePlayer / MovePlayer demoted to internal; L9 stale comment — v1.12)
//           (#31 T2 — TryFindPlayer / CanMovePlayer / MovePlayer, the id-preserving re-home;
//           AR round 1 P-M1 — every mutator goes through BeginMutation — v1.11)
//           (ERR-028-025 — the staged season boundary: PrepareSeasonBoundary /
//           CommitSeasonBoundary, the boundary-scoped regen stream, the mutation generation — v1.10)
//           (round-2 M/L adversarial findings: LifecycleView computes AgeBand (M7) +
//           the retirement-day feedback-loop invariant documented at the daily evaluation (M5) — v1.9)
//           (construction-day-credit-implemented-twice — SeedLifecycle delegates — v1.8)
//           (ERR-028-020 + ERR-028-021 — football-judgment proxy review batch 1 — v1.7)
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

using TacticalDirector.DeterministicSim;
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
    /// <b>The season boundary (§3.4, ERR-028-025).</b> Retirement FLAGGING is part of the draw-free daily
    /// step; the roster mutation — retiree removal + 1:1 regen — is <see cref="PrepareSeasonBoundary"/>
    /// (pure) then <see cref="CommitSeasonBoundary"/> (infallible), so #30 stages it before its one
    /// fallible write and installs it after. The regen stream is BOUNDARY-SCOPED: a fresh
    /// <c>DeterministicRngService</c> seeded from <c>(worldSeed, completed season)</c> under
    /// <c>DOMAIN_TAG_PLAYER_PROGRESSION</c>, so it carries no cursor across a save — every input is
    /// already persisted — which is the save story §3.5 had left unpinned.
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

        // Bumped by every mutation of the per-club arrays or the id cursor, so a SeasonBoundaryPlan
        // prepared before one cannot be installed after it — it would resurrect the records it was
        // computed from (the PlayerCareerStates.RosterGeneration precedent). Not serialized: it guards
        // an in-memory plan's lifetime, which never crosses a save.
        //
        // CLASS INVARIANT (AR round 1 P-M1): every method that writes _records, _lifecycles or
        // _nextPlayerId calls BeginMutation() BEFORE its first write. A writer that forgets turns
        // CommitSeasonBoundary's "infallible on a fresh plan" into a silent overwrite of its own work —
        // a moved player duplicated at his old club, or a regen erasing a transfer.
        private int _generation;

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
        /// deterministic: a draw here would put a draw site on the new-game path for a number the
        /// authored database is going to replace. (The regen stream has been registered since ERR-028-025 —
        /// its only draw site is <see cref="PrepareSeasonBoundary"/>'s regen generation; this seed adds none.)
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

            // L-3 (AR pass 8): SeedFrom was the only construction boundary NOT delegating to the shared
            // id-cursor owner — it computed `maxPlayerId + 1` and wrote it straight to `_nextPlayerId`
            // with no check at all. At maxPlayerId == int.MaxValue that addition overflows (silently,
            // this project runs unchecked arithmetic by default) to a negative cursor, producing a store
            // that seeds, advances and plays but can NEVER be saved — RequireIdCursorAheadOfCarriedIds
            // refuses it at Encode, forever, since the wrapped value reads as far behind every carried
            // id it is actually ahead of. Delegating here means the overflow is refused AT THE SEED SITE
            // rather than silently produced and only discovered at the one boundary nothing can recover
            // from — and it is refused by the exact same gate Encode/Decode/FromBlocks already share, so
            // there is one enforcer of FR-PG-011, not four independent copies of "don't let this happen".
            int nextPlayerId = maxPlayerId == int.MinValue ? 0 : maxPlayerId + 1;
            ProgressionSaveCodec.RequireIdCursorAheadOfCarriedIds(engine.ToBlocks(), nextPlayerId);
            engine._nextPlayerId = nextPlayerId;
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

            // M3: never BUILD a career from a club SquadFor cannot project. The codec's Encode/Decode
            // sides refuse the same rule (ProgressionSaveCodec.RequireClubSizeInRange); FromBlocks is
            // the third and documented restore path (a #47 authored-data loader, a tool, a fixture) and
            // must not be the one boundary a malformed block sails through — probe-verified before the
            // fix: a 0-player and a 30-player club both round-tripped through AdvanceDay and Save, and
            // SquadFor threw only when something finally asked to PLAY.
            ProgressionSaveCodec.RequireClubSizeInRange(clubs);

            // M2: the whole point of serializing NextPlayerId is that "a regen can never reuse a
            // retiree's id across a save boundary" (FR-PG-011). A cursor at or behind an id the store
            // already carries defeats that silently — the next allocation collides with a LIVE player,
            // and the collision then reads as one player with two careers.
            // AR pass 5 (recorded), fixed here: this rule lived ONLY at this boundary, so both codec
            // sides admitted a bad cursor and Encode could write a blob whose own Restore refuses it
            // forever. One owner now, called from Encode, Decode and here.
            ProgressionSaveCodec.RequireIdCursorAheadOfCarriedIds(clubs, nextPlayerId);

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
        /// #28's daily entry point (FR-PG-021) — the batch #30 drives at its KD-2 slot 1 and #29
        /// §3.5 composes against. Advances every carried player to <paramref name="worldDay"/>.
        /// <b>INTERNAL (AR round 4 M13 / ERR-028-028):</b> reachable only from <c>TacticalDirector.SeasonSave</c>
        /// (and the test assemblies) through <c>InternalsVisibleTo</c>. A public day step was reachable through
        /// <c>SeasonLoop.Progression</c> and advanced the roster without the per-day career seams.
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
        internal void AdvanceDay(uint worldDay, in TrainingInputBatch trainingInputs)
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
            BeginMutation();

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
                // cursor-vs-clock gates refuse the composition that could), but AdvanceDay is reachable by
                // the composition root and the test assemblies (internal + InternalsVisibleTo), which CAN
                // call backward, so the guard stays. Locked by AdvanceDay_BackwardCall_DoesNotEvaluateRetirement, which
                // isolates the `return;` from the `if` condition above it.
                return;
            }

            // §3.4 daily retirement evaluation: deterministic, no draw (FR-PG-013). Flagged only — the
            // player stays selectable until the season boundary (FR-PG-014), whose roster mutation
            // (retire + 1:1 regen) is CommitSeasonBoundary's (ERR-028-025).
            //
            // ERR-028-021: the comparison is in DAYS against a PER-PLAYER retirement day, not in whole
            // years against one league-wide integer. The retired form retired a goalkeeper on the same
            // clock as a forward and made one day of the calendar the whole difference between a career
            // continuing and ending, for everyone at once. At a zero goalkeeper bonus and a zero
            // reading span, `ageDays >= RETIREMENT_AGE * DAYS_PER_YEAR` is exactly the old
            // `rec.Age >= RETIREMENT_AGE` (age being that quotient, floored), so the identity is
            // reproducible rather than approximated.
            //
            // round-2 finding retirement-day-derived-from-attributes-the-same-step-mutates: `rec` here
            // is the SAME record the spend/drain loop above (inside this call's replay) just mutated —
            // AbilityModel.RetirementAgeDays reads Anticipation/Positioning/Composure, which
            // TrySpendOnePoint raises during Growth and DrainOnePoint lowers during Decline. So the
            // retirement day is a moving target this very step moves: re-evaluating tomorrow can return
            // a different day than today's read did. That is bounded today only because each attribute
            // is monotone within one band and RetirementFlag is sticky once true (no oscillation) — an
            // accident of §3.1.2's one-directional spend/drain order, not a property this method
            // enforces, and it stops holding the day something can move these three attributes
            // independently of age (the T3 curveEnabled tier; #47's authored data). See
            // AbilityModel.RetirementAgeDays' own doc for the invariant statement and
            // RetirementAgeDays_IsMonotonicWithinABand_AsTheAttributesItReadsAreMonotone
            // (AbilityModelTests.cs) for the locked half of the bound.
            long ageDaysAtTarget = (long)worldDay - life.BirthWorldDay;
            if (!life.RetirementFlag && ageDaysAtTarget >= AbilityModel.RetirementAgeDays(in rec))
            {
                life.RetirementFlag = true;
                life.RetirementDay = worldDay;
            }
        }

        // ── The season boundary (§3.4 as amended by ERR-028-025) ─────────────────────────

        /// <summary>
        /// Stages #28's season boundary (§3.4 <c>RunSeasonBoundary</c>): every player whose
        /// <see cref="PlayerLifecycle.RetirementFlag"/> is set is removed, and each is replaced 1:1
        /// (FR-PG-019) by a regen at the same club, in the retiree's position (ERR-028-024), with a fresh id
        /// from <see cref="NextPlayerId"/> (FR-PG-011). <b>Writes nothing</b> — the store is unchanged until
        /// <see cref="CommitSeasonBoundary"/>.
        /// <para>
        /// <b>INTERNAL (AR round 4 M13 / ERR-028-028):</b> a seam for #30's roll (<c>SeasonLoop.RollToNextSeason</c>,
        /// step (d)) only. A public Prepare/Commit pair reachable through <c>SeasonLoop.Progression</c> removed
        /// retirees and inserted regens mid-season with no reconciliation of the #29/#41/#30 career rows.
        /// </para>
        /// <para>
        /// <b>Draw order (normative, ERR-028-025).</b> One stream per club that has a retiree, registered
        /// under <see cref="PlayerProgressionConstants.SUBSYSTEM_ORDINAL_PLAYER_PROGRESSION"/> with
        /// <c>entityId = clubId</c>; a club's k-th retiree by ascending PlayerId consumes that stream's k-th
        /// reservation. <b>Id order (normative):</b> clubs ascending, then each club's retirees by ascending
        /// PlayerId, one <c>NextPlayerId</c> each. Stream keys depend only on (ordinal, entityId, version), so
        /// registration order cannot move a draw.
        /// </para>
        /// <para>
        /// <b>Seed and save story.</b> The service is seeded from
        /// <see cref="DeriveRegenSeed"/>(<paramref name="worldSeed"/>, <paramref name="completedSeasonNumber"/>)
        /// and discarded with the plan: every input — the world seed, the season number, the retirement flags,
        /// the cursor — is already persisted, so the stream needs no serialized cursor and a save taken
        /// before the boundary reproduces it exactly. Folding the season number in is what makes successive
        /// boundaries draw different regens.
        /// </para>
        /// </summary>
        /// <param name="worldSeed">The career's world seed (<c>WorldStore.WorldSeed</c>).</param>
        /// <param name="completedSeasonNumber">The season this boundary closes.</param>
        /// <param name="worldDay">The day the regens are constructed on — #30 passes the last LIVED day
        /// (the clock minus one), which is where every carried player's cursor sits, so a regen's cursor and
        /// its peers' agree and the next day step advances all of them together.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="worldDay"/> is the never-advanced
        /// sentinel (F8).</exception>
        /// <exception cref="ArgumentException"><paramref name="worldDay"/> is outside the span of the carried
        /// players' progression cursors (AR round 4 L16 — the §3.5 anchor MUST).</exception>
        /// <exception cref="InvalidOperationException">The id cursor would overflow, or regen generation
        /// failed (a draw-site misuse).</exception>
        internal SeasonBoundaryPlan PrepareSeasonBoundary(ulong worldSeed, int completedSeasonNumber, uint worldDay)
        {
            if (worldDay == PlayerProgressionConstants.PROGRESSION_NOT_ADVANCED_SENTINEL)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(worldDay), worldDay,
                    "The never-advanced sentinel is not a legal day to construct a regen on (F8).");
            }

            // AR round 4 L16 (the M12 MUST, enforced): a regen is anchored on worldDay and the next day step
            // advances every player from his own cursor, so a worldDay OUTSIDE the span the carried players'
            // cursors occupy plants a regen ahead of (or behind) every peer — one day ahead skips that day's
            // step by the ERR-030-027 same-day idempotence and the regen trails his peers for life. The
            // caller MUST pass the day the cursors sit on (#30: clock - 1); this refuses the unambiguous
            // violations (an empty store has no span to violate).
            bool anyPlayer = false;
            uint minCursor = uint.MaxValue;
            uint maxCursor = 0u;
            for (int c = 0; c < _lifecycles.Count; c++)
            {
                PlayerLifecycle[] held = _lifecycles[c];
                for (int p = 0; p < held.Length; p++)
                {
                    uint cursor = held[p].LastAdvancedWorldDay;
                    anyPlayer = true;
                    minCursor = cursor < minCursor ? cursor : minCursor;
                    maxCursor = cursor > maxCursor ? cursor : maxCursor;
                }
            }

            if (anyPlayer && (worldDay < minCursor || worldDay > maxCursor))
            {
                throw new ArgumentException(
                    $"worldDay {worldDay} is outside the span [{minCursor}, {maxCursor}] the carried players' "
                    + "progression cursors occupy: a regen must be constructed on the day his peers' cursors "
                    + "sit on (#28 §3.5), or the next day step skips or trails him (AR round 4 L16).",
                    nameof(worldDay));
            }

            DeterministicRngService rng = null;
            int next = _nextPlayerId;
            var clubIds = _clubIds.ToArray();
            var records = new PlayerRecord[clubIds.Length][];
            var lifecycles = new PlayerLifecycle[clubIds.Length][];
            var retired = new List<int>();
            var regens = new List<int>();
            var regenClubs = new List<int>();

            for (int c = 0; c < clubIds.Length; c++)
            {
                PlayerRecord[] heldRecords = _records[c];
                PlayerLifecycle[] heldLives = _lifecycles[c];
                var nextRecords = new PlayerRecord[heldRecords.Length];
                var nextLives = new PlayerLifecycle[heldLives.Length];
                int streamIndex = -1;

                // Records ascend by PlayerId, so this walk visits a club's retirees in the normative order.
                for (int p = 0; p < heldRecords.Length; p++)
                {
                    if (!heldLives[p].RetirementFlag)
                    {
                        nextRecords[p] = heldRecords[p];
                        nextLives[p] = heldLives[p];
                        continue;
                    }

                    if (rng == null)
                    {
                        rng = new DeterministicRngService(DeriveRegenSeed(worldSeed, completedSeasonNumber));
                    }

                    if (streamIndex < 0)
                    {
                        streamIndex = rng.RegisterStream(
                            PlayerProgressionConstants.REGEN_STREAM_SITE_ID,
                            PlayerProgressionConstants.SUBSYSTEM_ORDINAL_PLAYER_PROGRESSION,
                            entityId: clubIds[c],
                            streamVersion: PlayerProgressionConstants.REGEN_STREAM_VERSION);
                    }

                    if (next == int.MaxValue)
                    {
                        throw new InvalidOperationException(
                            "The PlayerId cursor is exhausted (FR-PG-011): no fresh id remains for a regen.");
                    }

                    int regenId = next++;
                    (PlayerRecord regen, PlayerLifecycle life) = RegenGenerator.GenerateRegen(
                        rng, streamIndex, clubIds[c], regenId, heldRecords[p].Position, worldDay);

                    string outOfRange = ProgressionSaveCodec.DescribeOutOfRangeValues(in regen, in life, clubIds[c]);
                    if (outOfRange != null)
                    {
                        throw new InvalidOperationException(
                            "A generated regen carries state the save path would refuse: " + outOfRange);
                    }

                    retired.Add(heldRecords[p].PlayerId);
                    regens.Add(regenId);
                    regenClubs.Add(clubIds[c]);
                    nextRecords[p] = regen;
                    nextLives[p] = life;
                }

                SortByPlayerId(nextRecords, nextLives);
                records[c] = nextRecords;
                lifecycles[c] = nextLives;
            }

            return new SeasonBoundaryPlan(
                this, _generation, clubIds, records, lifecycles, next,
                retired.ToArray(), regens.ToArray(), regenClubs.ToArray());
        }

        /// <summary>
        /// Installs a plan <see cref="PrepareSeasonBoundary"/> built from this store. <b>Cannot fail</b> on a
        /// plan this store prepared with no mutation since, which is what lets #30 call it after its roll's
        /// last fallible write. The store instance is kept (arrays are replaced in place), so every
        /// <c>ProgressionSquads</c> projection and every holder of this reference sees the new roster.
        /// <b>INTERNAL (AR round 4 M13 / ERR-028-028):</b> #30's roll only, for the reason
        /// <see cref="PrepareSeasonBoundary"/> states.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="plan"/> is null.</exception>
        /// <exception cref="ArgumentException">The plan belongs to another store, or this store has mutated
        /// since it was prepared (installing it would resurrect stale records).</exception>
        internal void CommitSeasonBoundary(SeasonBoundaryPlan plan)
        {
            if (plan == null)
            {
                throw new ArgumentNullException(nameof(plan));
            }

            if (!ReferenceEquals(plan.Owner, this) || plan.Generation != _generation)
            {
                throw new ArgumentException(
                    "This season-boundary plan was not prepared from this store's current state "
                    + $"(plan generation {plan.Generation}, store generation {_generation}).",
                    nameof(plan));
            }

            BeginMutation();
            for (int c = 0; c < _clubIds.Count; c++)
            {
                // Copied, not aliased (AR round 1 P-L4): AdvanceDay mutates the store's arrays in place,
                // and a retained plan's PlannedSquadFor must keep describing the boundary it staged.
                _records[c] = (PlayerRecord[])plan.Records[c].Clone();
                _lifecycles[c] = (PlayerLifecycle[])plan.Lifecycles[c].Clone();
            }

            _nextPlayerId = plan.NextPlayerId;
        }

        // The one place _generation moves (P-M1): see the field's class-invariant note.
        private void BeginMutation()
        {
            _generation++;
        }

        // ── Transfers: the stable-id re-home (#31 T2, ERR-031-005) ──────────────────────

        /// <summary>
        /// Finds a carried player by id across every club — the roster's answer to "who owns him",
        /// which #31 must use instead of deriving a club from the id (ERR-031-005).
        /// </summary>
        /// <returns>True with his current record and club; false (and <c>clubId = -1</c>) when no carried
        /// club has him.</returns>
        public bool TryFindPlayer(int playerId, out PlayerRecord record, out int clubId)
        {
            for (int c = 0; c < _clubIds.Count; c++)
            {
                int p = PlayerIndexOf(c, playerId);
                if (p >= 0)
                {
                    record = _records[c][p];
                    clubId = _clubIds[c];
                    return true;
                }
            }

            record = default;
            clubId = -1;
            return false;
        }

        /// <summary>
        /// Whether <see cref="MovePlayer"/> would succeed (INTERNAL, AR round 3 M13: the roster authority's
        /// re-home is a seam for #30's transfer adapter only — <c>SeasonTransferRoster</c> in TacticalDirector.SeasonSave,
        /// via InternalsVisibleTo. A public re-home would let any client forge a free transfer that leaves the
        /// #29/#41/#30 rows and #31's contracts behind): both clubs carried and distinct, the player on
        /// <paramref name="fromClubId"/>'s roster, and <paramref name="toClubId"/>'s roster below
        /// <see cref="PlayerDatabaseConstants.CLUB_SQUAD_SIZE"/>. Exactly the move's preconditions, so a
        /// preview that asks this makes the commit infallible (#31 §3.4). The source keeping at least one
        /// player is implied by the caller's matchday-squad guard (ERR-031-007), and re-checked here so the
        /// store can never hold an empty club (F11).
        /// </summary>
        internal bool CanMovePlayer(int playerId, int fromClubId, int toClubId)
        {
            if (fromClubId == toClubId)
            {
                return false;
            }

            int from = ClubIndexOf(fromClubId);
            int to = ClubIndexOf(toClubId);
            return from >= 0
                && to >= 0
                && PlayerIndexOf(from, playerId) >= 0
                && _records[from].Length > 1
                && _records[to].Length < PlayerDatabaseConstants.CLUB_SQUAD_SIZE;
        }

        /// <summary>
        /// #31 T2's re-home (ERR-031-005): moves one player's record AND lifecycle from
        /// <paramref name="fromClubId"/> to <paramref name="toClubId"/> under his UNCHANGED id. Nothing is
        /// allocated and the id cursor does not move (FR-PG-011: transfers allocate nothing). His growth
        /// cursor, potential and retirement state travel with him. Both clubs stay in ascending id order.
        /// INTERNAL (AR round 3 M13): reachable only through #30's transfer adapter, never from a client.
        /// </summary>
        /// <exception cref="InvalidOperationException"><see cref="CanMovePlayer"/> is false.</exception>
        internal void MovePlayer(int playerId, int fromClubId, int toClubId)
        {
            if (!CanMovePlayer(playerId, fromClubId, toClubId))
            {
                throw new InvalidOperationException(
                    $"Cannot re-home player {playerId} from club {fromClubId} to club {toClubId}: a club is "
                    + "not carried, he is not on the source, the source would be emptied, or the destination "
                    + "is at CLUB_SQUAD_SIZE.");
            }

            BeginMutation();
            int from = ClubIndexOf(fromClubId);
            int to = ClubIndexOf(toClubId);
            int p = PlayerIndexOf(from, playerId);
            PlayerRecord record = _records[from][p];
            PlayerLifecycle life = _lifecycles[from][p];

            var fromRecords = new PlayerRecord[_records[from].Length - 1];
            var fromLives = new PlayerLifecycle[fromRecords.Length];
            Array.Copy(_records[from], 0, fromRecords, 0, p);
            Array.Copy(_records[from], p + 1, fromRecords, p, fromRecords.Length - p);
            Array.Copy(_lifecycles[from], 0, fromLives, 0, p);
            Array.Copy(_lifecycles[from], p + 1, fromLives, p, fromLives.Length - p);

            var toRecords = new PlayerRecord[_records[to].Length + 1];
            var toLives = new PlayerLifecycle[toRecords.Length];
            Array.Copy(_records[to], toRecords, _records[to].Length);
            Array.Copy(_lifecycles[to], toLives, _lifecycles[to].Length);
            toRecords[toRecords.Length - 1] = record;
            toLives[toLives.Length - 1] = life;
            SortByPlayerId(toRecords, toLives);

            _records[from] = fromRecords;
            _lifecycles[from] = fromLives;
            _records[to] = toRecords;
            _lifecycles[to] = toLives;
        }

        /// <summary>
        /// The boundary-scoped regen stream's seed (ERR-028-025): the #16 tag folded in first, then the
        /// world seed, then the completed season number — the <c>RoundResolutionModel.FixtureKey</c> fold.
        /// </summary>
        internal static ulong DeriveRegenSeed(ulong worldSeed, int completedSeasonNumber)
        {
            ulong h = Mix(PlayerProgressionConstants.DOMAIN_TAG_PLAYER_PROGRESSION ^ worldSeed);
            return Mix(h ^ (ulong)(uint)completedSeasonNumber);
        }

        // SplitMix64's finalizer. A per-assembly copy is this project's accepted norm (RoundResolutionModel,
        // MedicalStep, LeagueBootstrap each carry one); no shared helper exists on deterministic-sim.
        private static ulong Mix(ulong value)
        {
            unchecked  // Spec #16 §3.4.4: deliberate 64-bit wrap-around; not an overflow bug
            {
                ulong z = value + 0x9E3779B97F4A7C15UL;
                z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
                z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
                return z ^ (z >> 31);
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
            int age = _records[c][p].Age;
            return new LifecycleViewModel(
                playerId,
                age,
                life.CurrentAbility,
                life.PotentialAbility,
                AbilityModel.ClassifyAgeBand(age),
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

            // AR pass 5 (High). The comment below says the seed day is "already accounted for, not a day
            // still to be lived" — and then the cursor started at 0, which accounts for it as NOTHING.
            // The band exit is decided by the DERIVED AGE, not by the cursor, so leaving the anchor day
            // uncredited shifts the accrual window one day right against a fixed band edge: every
            // traversal of a band N years long accrued N·365 − 1 days, and since POINT_COST is exactly
            // DAYS_PER_YEAR (KD-8) that is one whole attribute point short, every time. Measured through
            // the public API before this fix:
            //     seedAge 16 (8 years of Growth) -> 7 points, residue cursor 364
            //     seedAge 20 (4 years)           -> 3 points, residue cursor 364
            //     seedAge 23 (1 year)            -> 0 points, residue cursor 364
            // Appendix A and KD-8 both say +1/yr, so this is a defect against normative spec text, not
            // a tuning choice. The 364 residue then survives the Stable band (which accrues nothing, so
            // it can never be spent) and eats the first year of Decline, delaying a player's first
            // decline point to day 728 — and it makes two otherwise identical players diverge purely by
            // the age they were seeded at.
            //
            // Crediting the anchor day's own band step here is ERR-028-014's reading made arithmetically
            // honest. Deliberately NOT fixed by anchoring the cursor at newGameWorldDay - 1: at day 0
            // that underflows to uint.MaxValue, which is the sentinel FromBlocks refuses.
            //
            // ERR-028-020: the credit is the seed day's own CONTINUOUS step, taken from the same
            // AbilityModel.DailyBandPoints the daily step uses. The retired three-way form read
            // ClassifyAgeBand and picked one of three constants, which agreed with the daily step only
            // while that step was itself a three-way band — leaving it behind would have re-opened
            // exactly the one-day accrual discrepancy this block was written to close, silently and in
            // the ramp only.
            //
            // construction-day-credit-implemented-twice (round-2 High): the expression is no longer
            // written out here. It was character-for-character RegenGenerator.BandStepFor's body, at the
            // OTHER site that constructs a PlayerLifecycle from scratch, and that duplication is what
            // made ERR-028-018 a one-site fix to a two-site rule and ERR-028-020 a re-visit of both.
            // AbilityModel.ConstructionDayCredit is the single owner now; this site delegates.
            long seedCursor = AbilityModel.ConstructionDayCredit(rec.Age);

            return new PlayerLifecycle
            {
                PotentialAbility = potentialAbility,
                CurrentAbility = currentAbility,
                GrowthCursor = seedCursor,
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
// | 1.4     | 2026-08-10 | —      | AR pass 5 (time/arithmetic axis), High (ERR-028-018).            |
// |         |            |        | SeedLifecycle credits the seed day's own band step instead of   |
// |         |            |        | starting GrowthCursor at 0. A band exit is decided by the       |
// |         |            |        | DERIVED AGE, so leaving the anchor day uncredited shifted the   |
// |         |            |        | accrual window one day right of a fixed band edge: every        |
// |         |            |        | traversal of an N-year band accrued N·365 − 1 days, and since   |
// |         |            |        | POINT_COST == DAYS_PER_YEAR (KD-8) that is one whole attribute  |
// |         |            |        | point short every time (seedAge 16 measured 7 points not 8,     |
// |         |            |        | residue cursor 364; seedAge 23 measured 0 not 1). The residue   |
// |         |            |        | then survived the Stable band and ate the first year of         |
// |         |            |        | Decline. Appendix A and KD-8 both promise +1/yr, so this        |
// |         |            |        | contradicted normative spec text. Deliberately NOT fixed by     |
// |         |            |        | anchoring the cursor at newGameWorldDay - 1: at day 0 that       |
// |         |            |        | underflows to uint.MaxValue, the sentinel FromBlocks refuses.   |
// |         |            |        | Mutation-verified: reverting the seed credit fails 6 of 109     |
// |         |            |        | player-progression tests.                                       |
// | 1.5     | 2026-08-11 | —      | AR pass 6, M3. FromBlocks gains a                                |
// |         |            |        | ProgressionSaveCodec.RequireClubSizeInRange call: a club outside |
// |         |            |        | [1, CLUB_SQUAD_SIZE] had no gate anywhere in this file, so a     |
// |         |            |        | 0- or 30-player block built, advanced-day'd and saved cleanly    |
// |         |            |        | and only threw from SquadFor — mid-round, inside                 |
// |         |            |        | ISquadProvider.ResolveByClubId, after earlier fixtures in that   |
// |         |            |        | round had already been applied. Same shared owner Encode/Decode  |
// |         |            |        | now call.                                                        |
// | 1.6     | 2026-08-11 | —      | AR pass 8, L-3. SeedFrom was the only construction boundary NOT  |
// |         |            |        | delegating to the shared id-cursor owner — it wrote               |
// |         |            |        | `maxPlayerId + 1` straight to `_nextPlayerId` with no check at    |
// |         |            |        | all, so at maxPlayerId == int.MaxValue the addition overflows     |
// |         |            |        | (silently; this project's arithmetic is unchecked by default) to  |
// |         |            |        | a negative cursor — a store that seeds, advances and plays but    |
// |         |            |        | can NEVER be saved (ProgressionSaveCodec.RequireIdCursorAhead-    |
// |         |            |        | OfCarriedIds refuses it at Encode, forever). Now computes the     |
// |         |            |        | candidate cursor and calls that same shared gate before assigning |
// |         |            |        | it — the overflow-wrapped value reads as far behind the carried   |
// |         |            |        | int.MaxValue id, so the gate refuses it AT THE SEED SITE instead. |
// |         |            |        | Mutation-verified: reverting to the bare assignment fails exactly |
// |         |            |        | the new SeedFrom_APlayerAtIntMaxValue_IsRefused lock.             |
// | 1.7     | 2026-08-22 | —      | ERR-028-020 / ERR-028-021. SeedLifecycle's seed-day credit takes the
// |         |            |        | CONTINUOUS step (leaving the three-way form here would have re-opened
// |         |            |        | ERR-028-018's one-day discrepancy inside the ramp, silently).
// |         |            |        | AdvancePlayerTo's retirement test moves from rec.Age >= RETIREMENT_AGE to
// |         |            |        | ageDays >= AbilityModel.RetirementAgeDays(rec) — per-player, in days.
// | 1.8     | 2026-08-24 | —      | Round-2 adversarial finding construction-day-credit-implemented-twice
// |         |            |        | (High). SeedLifecycle no longer spells the construction-day credit out
// |         |            |        | inline — it calls AbilityModel.ConstructionDayCredit(rec.Age). The two
// |         |            |        | lines deleted here were character-for-character RegenGenerator.
// |         |            |        | BandStepFor's body, i.e. the same rule implemented twice, which is
// |         |            |        | why ERR-028-018 landed at this site only and ERR-028-020 had to
// |         |            |        | visit both. Behaviour-identical (verified by probe over ages 0..200
// |         |            |        | and the int edges before the collapse). Newly locked at this site by
// |         |            |        | SeedFrom_AtAnAgeInsideTheGrowthRamp_CreditsTheContinuousStep, which
// |         |            |        | fails if the seed credit reverts to the retired three-way band step —
// |         |            |        | no case in this file drove a ramp age before.
// | 1.9     | 2026-08-24 | —      | Round-2 Medium/Low adversarial findings. M7
// |         |            |        | (classifyageband-public-with-no-callers-and-a-silently-changed-
// |         |            |        | meaning): LifecycleView now computes AbilityModel.ClassifyAgeBand(age)
// |         |            |        | and passes it into the new LifecycleViewModel.AgeBand field —
// |         |            |        | ClassifyAgeBand's first production caller anywhere in src/, and the
// |         |            |        | reason it can now be demoted internal (AbilityModel.cs v1.5). M5
// |         |            |        | (retirement-day-derived-from-attributes-the-same-step-mutates): the
// |         |            |        | §3.4 retirement evaluation in AdvancePlayerTo now states in comment
// |         |            |        | that `rec` there is the SAME record the spend/drain loop just
// |         |            |        | mutated earlier in this call, so RetirementAgeDays reads a moving
// |         |            |        | target — bounded today only by the one-directional band order and
// |         |            |        | RetirementFlag's stickiness, locked by a new AbilityModelTests.cs
// |         |            |        | case rather than left as an unstated accident. No behaviour change,
// |         |            |        | no format version, no draw.
// | 1.10    | 2026-10-19 | —      | ERR-028-025: the season boundary lands. PrepareSeasonBoundary   |
// |         |            |        | (pure) stages retiree removal + 1:1 regen in the retiree's      |
// |         |            |        | position (ERR-028-024) with fresh NextPlayerId ids;             |
// |         |            |        | CommitSeasonBoundary (infallible, generation-checked) installs  |
// |         |            |        | it in place. The regen stream is boundary-scoped: seeded from   |
// |         |            |        | (worldSeed, completed season) under DOMAIN_TAG 0x20, one        |
// |         |            |        | registration per club (ordinal 82), no serialized cursor. A     |
// |         |            |        | mutation generation (bumped by AdvanceDay too) refuses stale    |
// |         |            |        | plans. #28's first draw site.                                   |
// | 1.11    | 2026-10-20 | —      | #31 T2 (ERR-031-005): TryFindPlayer (roster-answered ownership),  |
// |         |            |        | CanMovePlayer / MovePlayer (record + lifecycle re-homed under the |
// |         |            |        | unchanged id; no allocation). AR round 1 P-M1: every mutator calls |
// |         |            |        | BeginMutation, the one place the plan-staleness generation moves.  |
// |         |            |        | P-L4: CommitSeasonBoundary installs COPIES of the plan's arrays.   |
// | 1.12    | 2026-10-20 | —      | AR round 3 M13 (ERR-028-028): CanMovePlayer / MovePlayer demoted   |
// |         |            |        | public -> internal (a public re-home forged a free transfer and    |
// |         |            |        | orphaned the #29/#41/#30 rows); the IVT grant lives in            |
// |         |            |        | AssemblyInfo.cs. L9: the retirement-evaluation comment no longer   |
// |         |            |        | calls the boundary roster mutation "the deferred landing's".       |
// | 1.13    | 2026-10-20 | —      | AR round 4 M13 / L12 (ERR-028-028 widening): AdvanceDay /          |
// |         |            |        | PrepareSeasonBoundary / CommitSeasonBoundary demoted public ->     |
// |         |            |        | internal — the same forged-roster-mutation class as CanMovePlayer /|
// |         |            |        | MovePlayer, reachable through the public SeasonLoop.Progression:   |
// |         |            |        | a client's CommitSeasonBoundary(PrepareSeasonBoundary(..)) mid-    |
// |         |            |        | season removed retirees and inserted regens with no career-row     |
// |         |            |        | reconciliation. Callers (grep of src/): SeasonLoop and the         |
// |         |            |        | SeasonSave / PlayerProgression test assemblies only — all covered  |
// |         |            |        | by the existing IVT grants. SeedFrom doc: the regen stream has     |
// |         |            |        | been registered since ERR-028-025. No behaviour change.            |
// |         |            |        | L16: PrepareSeasonBoundary also refuses (ArgumentException) a      |
// |         |            |        | worldDay outside [min, max] of the carried players' cursors —      |
// |         |            |        | enforcing the §3.5 anchor MUST; no legal call changes behaviour.   |
// | 1.14    | 2026-10-20 | —      | AR round 5 L17 (comment-only): the backward-call guard's comment  |
// |         |            |        | said "AdvanceDay is public, so the guard stays" - it has been     |
// |         |            |        | internal since v1.13. Restated: reachable by the composition root |
// |         |            |        | and the test assemblies, which can call backward. No behaviour.   |
#endregion
