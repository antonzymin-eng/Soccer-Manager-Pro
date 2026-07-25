// File:     src/season-save/tests/LeagueBootstrapGoldenVectorTests.cs
// Created:  2026-07-25
// Modified: 2026-07-25
// Author:   —
// Spec:     League Bootstrap design supplement (docs/tracking/league-bootstrap-design.md) §3 + KD-10;
//           Deterministic Simulation #16 §9.5 #4 (the golden-vector precedent)
// Purpose:  The pinned golden vector for league generation. Every other determinism test on this path
//           is SELF-REFERENTIAL ("generate twice, compare"), which cannot detect a change to the
//           generation function itself — and rosters are not persisted, so such a change silently
//           rewrites every club in every existing save. This file is the only thing that fails when
//           generation moves.

using System;
using System.Text;

using NUnit.Framework;

using TacticalDirector.PlayerDatabase;

namespace TacticalDirector.SeasonSave.Tests
{
    /// <summary>
    /// Absolute-value locks on <see cref="LeagueBootstrap.Generate(ulong, int)"/>.
    /// <para>
    /// <b>Why this exists.</b> A save file carries club IDs, not squads; <see cref="League"/> re-derives
    /// every roster as the <c>ISquadProvider</c> that <c>SeasonSaveManager.Load</c> consumes, and that
    /// interface requires the resolved squad to be the SAME roster the saved match loaded. So the
    /// generation path is persistence-equivalent: a draw-order change in <see cref="RosterGenerator"/>,
    /// a reorder of <c>NameCatalogue</c> / <see cref="ClubNameCatalogue"/>, an edit to
    /// <see cref="LeagueBootstrapConstants.SquadPositionCounts"/>, or a one-line tweak to a
    /// <c>[GT]</c> generation constant such as <c>PlayerDatabaseConstants.AttributeBaseMean</c> — which
    /// reads as an innocuous balance change — invalidates every save. Same-seed determinism tests stay
    /// green through all of those. These do not: verified by perturbing <c>AttributeBaseMean</c>
    /// 10 → 11, which fails the digest and spot-value locks here while the rest of the suite passes.
    /// </para>
    /// <para>
    /// <b>If this fails.</b> Do not re-pin to make it pass. Either the change was unintended — revert
    /// it — or it is deliberate, in which case re-pin in the SAME commit and treat it as a
    /// save-breaking change (the #16 golden-vector convention: <c>hkdf-sha256-kat.md</c>,
    /// <c>siphash-2-4-kat.md</c>, <c>serialize-canonical-corpus.md</c>).
    /// </para>
    /// </summary>
    [TestFixture]
    internal sealed class LeagueBootstrapGoldenVectorTests
    {
        // The pinned vector's inputs. Never change these — a different seed is a different vector.
        private const ulong GoldenWorldSeed = 0x5EED1EA6D0DEC0DEUL;

        // A small league, so the spot values below are readable and a failure is diagnosable.
        private const int GoldenClubCount = 4;

        // ...and the PRODUCTION club count, pinned separately. The small vector alone would not cover
        // what actually changes with league size: the Fisher-Yates permutation runs over N elements,
        // the ramp denominator is N-1, name indexing reaches N-1, and only at a larger N does the ramp
        // produce delta == 0 at all (so ApplyStrength's early-return branch is unexercised at N=4).
        // A generation change that happened to preserve a 4-club league would otherwise ship.
        private const int GoldenDefaultClubCount = 20;

        // ── Pinned expected values (captured 2026-07-25 from the landing commit) ────

        private const ulong ExpectedSeasonSeed = 0xCAA212CD2A2591EBUL;

        // FNV-1a-64 over every canonical field of every club and player — see Digest().
        private const ulong ExpectedLeagueDigest = 0x6E0256C1CF1413C8UL;

        // The same digest over a default-size (20-club) league — the shape a real career has.
        private const ulong ExpectedDefaultLeagueDigest = 0x1E96ECA8D7B20854UL;

        private static readonly int[] ExpectedStrengthDeltas = { 1, -1, 3, -3 };

        private static readonly int[] ExpectedDefaultStrengthDeltas =
        {
            1, 3, -2, -2, 2, 3, 2, -1, 0, -2, 1, 0, -1, 1, 0, -3, -3, -1, 0, 2
        };

        private const string ExpectedClub0Name = "Ashford Town";
        private const string ExpectedClub0Player0First = "Joshua";
        private const string ExpectedClub0Player0Last = "Hernandez";
        private const int ExpectedClub0Player0Age = 28;
        private const PlayerPosition ExpectedClub0Player0Position = PlayerPosition.Goalkeeper;
        private const int ExpectedClub0Player0Pace = 12;
        private const int ExpectedClub0Player0Reflexes = 12;
        private const int ExpectedClub0Player0WeakFoot = 4;

        // ── The locks ───────────────────────────────────────────────────────────────

        [Test]
        public void GoldenLeague_DigestIsUnchanged()
        {
            League league = LeagueBootstrap.Generate(GoldenWorldSeed, GoldenClubCount);

            Assert.AreEqual(
                ExpectedLeagueDigest, Digest(league),
                "League generation has changed. Rosters are regenerated from the world seed rather than " +
                "persisted, so this rewrites every club in every existing save. Revert the change, or " +
                "re-pin here in the same commit and treat it as save-breaking.");
        }

        [Test]
        public void GoldenDefaultSizeLeague_DigestIsUnchanged()
        {
            // The production shape. Guard first: if DefaultClubCount is retuned, this vector stops
            // describing what it claims to, and a silently-skipped golden vector is worse than none.
            Assert.AreEqual(
                GoldenDefaultClubCount, LeagueBootstrapConstants.DefaultClubCount,
                "DefaultClubCount changed — re-capture this vector at the new default rather than " +
                "leaving it pinned to a size nobody generates.");

            League league = LeagueBootstrap.Generate(GoldenWorldSeed, GoldenDefaultClubCount);
            Assert.AreEqual(
                ExpectedDefaultLeagueDigest, Digest(league),
                "League generation has changed at the DEFAULT club count. Same consequence as the " +
                "small-league vector: every existing save's rosters are rewritten.");

            for (int c = 0; c < GoldenDefaultClubCount; c++)
            {
                Assert.AreEqual(ExpectedDefaultStrengthDeltas[c], league.ClubAt(c).StrengthDelta,
                    $"club {c}'s delta moved — the ramp/permutation behaves differently at N=20 than " +
                    "the 4-club vector can see.");
            }
        }

        [Test]
        public void GoldenLeague_SeasonSeedDerivationIsUnchanged()
        {
            // Separate from the digest: this one value decides the fixture schedule, so a change here
            // reshuffles the whole calendar even if every roster is byte-identical.
            League league = LeagueBootstrap.Generate(GoldenWorldSeed, GoldenClubCount);
            Assert.AreEqual(ExpectedSeasonSeed, league.SeasonSeed,
                "The worldSeed -> seasonSeed derivation (KD-4) has changed.");
        }

        [Test]
        public void GoldenLeague_StrengthRampIsUnchanged()
        {
            League league = LeagueBootstrap.Generate(GoldenWorldSeed, GoldenClubCount);

            for (int c = 0; c < GoldenClubCount; c++)
            {
                Assert.AreEqual(ExpectedStrengthDeltas[c], league.ClubAt(c).StrengthDelta,
                    $"club {c}'s strength delta moved — the KD-5 rank permutation or ramp has changed.");
            }
        }

        [Test]
        public void GoldenLeague_SpotValuesAreUnchanged()
        {
            // Spelled out so a digest failure is diagnosable: these say WHICH part moved.
            League league = LeagueBootstrap.Generate(GoldenWorldSeed, GoldenClubCount);
            Assert.AreEqual(ExpectedClub0Name, league.ClubAt(0).Name, "club naming");

            PlayerRecord p = league.ResolveByClubId(0).GetPlayer(0);
            Assert.AreEqual(ExpectedClub0Player0First, p.FirstName, "first-name draw or NameCatalogue order");
            Assert.AreEqual(ExpectedClub0Player0Last, p.LastName, "last-name draw or NameCatalogue order");
            Assert.AreEqual(ExpectedClub0Player0Age, p.Age, "age draw");
            Assert.AreEqual(ExpectedClub0Player0Position, p.Position, "position template");
            Assert.AreEqual(ExpectedClub0Player0Pace, p.Attributes.Pace, "attribute draw / bias / strength shift");
            Assert.AreEqual(ExpectedClub0Player0Reflexes, p.Attributes.Reflexes, "GK position bias");
            Assert.AreEqual(ExpectedClub0Player0WeakFoot, p.Attributes.WeakFootRating, "weak-foot draw");
        }

        [Test]
        public void GoldenLeague_DigestCoversEveryPlayerField()
        {
            // Non-vacuity: the digest must actually respond to a change anywhere in the league. Perturb
            // one attribute of the last player of the last club — the furthest point from the start of
            // the fold — and require a different digest.
            League league = LeagueBootstrap.Generate(GoldenWorldSeed, GoldenClubCount);

            Squad last = league.ResolveByClubId(GoldenClubCount - 1);
            var players = new PlayerRecord[last.Count];
            for (int i = 0; i < last.Count; i++)
            {
                players[i] = last.GetPlayer(i);
            }

            int lastIndex = last.Count - 1;
            players[lastIndex].Attributes.Pace =
                players[lastIndex].Attributes.Pace == PlayerDatabaseConstants.ATTRIBUTE_MAX
                    ? PlayerDatabaseConstants.ATTRIBUTE_MIN
                    : players[lastIndex].Attributes.Pace + 1;

            var perturbed = new Squad(last.ClubId, players);
            Assert.AreNotEqual(
                DigestSquad(FnvOffsetBasis, last),
                DigestSquad(FnvOffsetBasis, perturbed),
                "A single attribute on the LAST player of the LAST club must move the digest. If it " +
                "does not, the fold has a coverage hole and would not catch a generation change.");
        }

        // ── Digest ──────────────────────────────────────────────────────────────────

        /// <summary>
        /// FNV-1a-64 over the league's full canonical field stream: for each club in ascending id,
        /// the id, name, strength delta, and then every player's id, names, age, position, all 31
        /// attributes and the weak-foot rating. Deliberately not the #16 snapshot digest — nothing
        /// here is serialized; this is a test-local fingerprint of the generator's output.
        /// </summary>
        private const ulong FnvOffsetBasis = 0xCBF29CE484222325UL;

        private static ulong Digest(League league)
        {
            ulong h = FnvOffsetBasis;
            h = FoldU64(h, league.SeasonSeed);
            h = FoldI32(h, league.ClubCount);

            for (int c = 0; c < league.ClubCount; c++)
            {
                Club club = league.ClubAt(c);
                h = FoldI32(h, club.ClubId);
                h = FoldString(h, club.Name);
                h = FoldI32(h, club.StrengthDelta);
                h = DigestSquad(h, league.ResolveByClubId(c));
            }

            return h;
        }

        private static ulong DigestSquad(ulong baseline, Squad squad)
        {
            ulong h = FoldI32(baseline, squad.ClubId);
            h = FoldI32(h, squad.Count);

            for (int i = 0; i < squad.Count; i++)
            {
                PlayerRecord p = squad.GetPlayer(i);
                h = FoldI32(h, p.PlayerId);
                h = FoldString(h, p.FirstName);
                h = FoldString(h, p.LastName);
                h = FoldI32(h, p.Age);
                h = FoldI32(h, (int)p.Position);
                foreach (int a in p.Attributes.ToArray())
                {
                    h = FoldI32(h, a);
                }

                h = FoldI32(h, p.Attributes.WeakFootRating);
            }

            return h;
        }

        private static ulong FoldByte(ulong h, byte b)
        {
            unchecked  // Spec #16 §3.4.4: deliberate 64-bit wrap-around; not an overflow bug
            {
                return (h ^ b) * 0x100000001B3UL;
            }
        }

        private static ulong FoldI32(ulong h, int value)
        {
            unchecked
            {
                uint u = (uint)value;
                h = FoldByte(h, (byte)(u & 0xFF));
                h = FoldByte(h, (byte)((u >> 8) & 0xFF));
                h = FoldByte(h, (byte)((u >> 16) & 0xFF));
                h = FoldByte(h, (byte)((u >> 24) & 0xFF));
                return h;
            }
        }

        private static ulong FoldU64(ulong h, ulong value)
        {
            unchecked
            {
                for (int i = 0; i < 8; i++)
                {
                    h = FoldByte(h, (byte)((value >> (i * 8)) & 0xFF));
                }

                return h;
            }
        }

        private static ulong FoldString(ulong h, string value)
        {
            foreach (byte b in Encoding.UTF8.GetBytes(value ?? string.Empty))
            {
                h = FoldByte(h, b);
            }

            return FoldByte(h, 0);   // terminator, so "ab"+"c" and "a"+"bc" differ
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-07-25 | —      | Initial golden vector (AR-5 H-1): absolute-value locks on league   |
// |         |            |        | generation, which is persistence-equivalent because rosters are    |
// |         |            |        | regenerated from the world seed rather than saved.                 |
#endregion
