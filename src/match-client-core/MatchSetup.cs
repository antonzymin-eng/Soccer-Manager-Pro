// File:     src/match-client-core/MatchSetup.cs
// Created:  2026-07-24
// Modified: 2026-07-24
// Author:   —
// Spec:     Interactive Unity client (docs/tracking/interactive-unity-client-design.md §3-2/§5-P0/§6.1),
//           Code Standards #20
// Purpose:  The immutable value MatchSession boots a whole live-match composition from: seed, the
//           home/away squads, initial team tactics, manager configuration, and the GK/heading flag —
//           i.e. everything applied ONCE, pre-kickoff, via the engine's boot-only mutators. Half of the
//           reproducibility invariant (the other half is the tick-stamped command log): the same
//           MatchSetup + the same log re-ticks byte-identically (§6.1).

using System;

using TacticalDirector.MatchEngine;
using TacticalDirector.PlayerDatabase;
using TacticalDirector.TacticalInstructions;

namespace TacticalDirector.MatchClientCore
{
    /// <summary>
    /// Immutable boot configuration for one live match. Everything here is applied once by
    /// <see cref="MatchSession"/> before kickoff through pre-kickoff/boot-only engine mutators
    /// (<c>ConfigureSquads</c>/<c>ConfigureManager</c>/<c>EnableGkHeading</c>) plus the initial
    /// stride-committed team tactics — never live (§3-2). Squads are both-or-neither: null on both is a
    /// neutral demo match on the engine's synthetic agents.
    /// </summary>
    public sealed class MatchSetup
    {
        /// <summary>The match seed (the engine's determinism root).</summary>
        public ulong Seed { get; }

        /// <summary>Home (team 0) roster, or null for a neutral demo match. Both squads null, or both non-null.</summary>
        public Squad HomeSquad { get; }

        /// <summary>Away (team 1) roster, or null for a neutral demo match. Both squads null, or both non-null.</summary>
        public Squad AwaySquad { get; }

        /// <summary>Initial home team tactic (staged pre-kickoff, committed at the first stride).</summary>
        public TeamTactic HomeTactic { get; }

        /// <summary>Initial away team tactic (staged pre-kickoff, committed at the first stride).</summary>
        public TeamTactic AwayTactic { get; }

        /// <summary>Home manager mode. <see cref="ManagerMode.Human"/> (default) applies no manager config.</summary>
        public ManagerMode HomeManagerMode { get; }

        /// <summary>Away manager mode. <see cref="ManagerMode.Human"/> (default) applies no manager config.</summary>
        public ManagerMode AwayManagerMode { get; }

        /// <summary>Home manager profile ordinal (applied only when <see cref="HomeManagerMode"/> ≠ Human).</summary>
        public byte HomeManagerProfile { get; }

        /// <summary>Away manager profile ordinal (applied only when <see cref="AwayManagerMode"/> ≠ Human).</summary>
        public byte AwayManagerProfile { get; }

        /// <summary>When true, the session opts the engine into GK/heading (Phase-1 opt-in) at boot.</summary>
        public bool GkHeadingEnabled { get; }

        /// <summary>True when both squads are supplied (a distinct-squad match); false for a neutral demo.</summary>
        public bool HasDistinctSquads => HomeSquad != null && AwaySquad != null;

        /// <summary>
        /// Constructs a setup. <paramref name="homeSquad"/>/<paramref name="awaySquad"/> must be both
        /// null (neutral demo) or both non-null (distinct squads) — a single squad is refused fail-loud,
        /// since the engine configures both teams together.
        /// </summary>
        public MatchSetup(
            ulong seed,
            Squad homeSquad = null,
            Squad awaySquad = null,
            TeamTactic? homeTactic = null,
            TeamTactic? awayTactic = null,
            ManagerMode homeManagerMode = ManagerMode.Human,
            ManagerMode awayManagerMode = ManagerMode.Human,
            byte homeManagerProfile = 0,
            byte awayManagerProfile = 0,
            bool gkHeadingEnabled = false)
        {
            if ((homeSquad == null) != (awaySquad == null))
            {
                throw new ArgumentException(
                    "MatchSetup: squads must be both null (neutral demo) or both non-null (distinct squads).");
            }

            Seed               = seed;
            HomeSquad          = homeSquad;
            AwaySquad          = awaySquad;
            HomeTactic         = homeTactic ?? TeamTactic.Balanced;
            AwayTactic         = awayTactic ?? TeamTactic.Balanced;
            HomeManagerMode    = homeManagerMode;
            AwayManagerMode    = awayManagerMode;
            HomeManagerProfile = homeManagerProfile;
            AwayManagerProfile = awayManagerProfile;
            GkHeadingEnabled   = gkHeadingEnabled;
        }

        /// <summary>A neutral demo match on the engine's synthetic agents: given seed, Balanced tactics, no managers, GK/heading off.</summary>
        public static MatchSetup NeutralDemo(ulong seed) => new MatchSetup(seed);
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-24 | —      | Initial creation (P0): immutable boot config (seed / squads /  |
// |         |            |        | tactics / managers / GK-heading) + NeutralDemo factory.        |
#endregion
