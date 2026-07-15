// File:     src/player-database/AttrIdx.cs
// Created:  2026-07-15
// Modified: 2026-07-15
// Author:   —
// Spec:     Squad/Player Data Layer design supplement (docs/tracking/squad-player-data-design.md)
// Purpose:  Single ordinal mapping for the 31 int[1,20] fields on PlayerAttributes, shared by
//           PlayerAttributes.ToArray/FromArray, RosterGenerator, and SquadFileLoader so the slot
//           order is declared exactly once.

namespace TacticalDirector.PlayerDatabase
{
    /// <summary>
    /// Ordinal indices into the 31-element int[1,20] attribute array (<see cref="PlayerAttributes.ToArray"/>).
    /// ORDINAL STABILITY: append-only. Reordering breaks RosterGenerator's fixed RNG draw order and any
    /// serialized attribute array. Design doc §3.
    /// </summary>
    public static class AttrIdx
    {
        // -- Physical (6, Agent Movement #2) --
        public const int Pace = 0;
        public const int Acceleration = 1;
        public const int Agility = 2;
        public const int Balance = 3;
        public const int Strength = 4;
        public const int Stamina = 5;

        // -- Technical (8) --
        public const int Passing = 6;      // Pass Mechanics #5
        public const int Technique = 7;    // Pass Mechanics #5 / Shot Mechanics #6
        public const int Finishing = 8;    // Shot Mechanics #6
        public const int LongShots = 9;    // Shot Mechanics #6 / Decision Tree #8
        public const int Dribbling = 10;   // Decision Tree #8
        public const int Crossing = 11;    // Decision Tree #8 (declared-but-unconsumed, ERR-008-006)
        public const int Heading = 12;     // Heading Mechanics #10

        // -- Mental (7, Decision Tree #8 / Shot #6 / Goalkeeper #11 / Perception #7) --
        public const int Decisions = 13;
        public const int Vision = 14;
        public const int Composure = 15;
        public const int Anticipation = 16;
        public const int WorkRate = 17;
        public const int Aggression = 18;
        public const int Positioning = 19;

        // -- Goalkeeping (6, Goalkeeper Mechanics #11) --
        public const int Reflexes = 20;
        public const int Handling = 21;
        public const int Aerial = 22;
        public const int OneVsOne = 23;
        public const int Throwing = 24;
        public const int Kicking = 25;

        // -- Reserved (5) -- master plan §4.2 attributes not yet consumed by any Stage-0 spec.
        public const int Tackling = 26;
        public const int Marking = 27;
        public const int Concentration = 28;
        public const int Teamwork = 29;
        public const int FirstTouchAbility = 30;

        /// <summary>One past the highest index; must equal PlayerDatabaseConstants.ATTRIBUTE_COUNT.</summary>
        public const int Count = 31;
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                    |
// | 1.0     | 2026-07-15 | —      | Initial implementation.  |
#endregion
