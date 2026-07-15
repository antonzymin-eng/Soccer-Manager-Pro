// File:     src/player-database/NameCatalogue.cs
// Created:  2026-07-15
// Modified: 2026-07-15
// Author:   —
// Spec:     Squad/Player Data Layer design supplement (docs/tracking/squad-player-data-design.md) §3
// Purpose:  Stage-0 in-code first/last name corpus for deterministic roster generation. APPEND-only
//           order — RosterGenerator indexes these by RNG draw mod length, so removing/reordering
//           entries changes which name a given draw produces (not determinism-breaking on its own,
//           since generation always re-derives from the current corpus, but changes prior output).

namespace TacticalDirector.PlayerDatabase
{
    /// <summary>Stage-0 in-code name pools for roster generation. Same pattern as InteractionTextCorpus.</summary>
    public static class NameCatalogue
    {
        /// <summary>First-name pool (32 entries). APPEND-only.</summary>
        public static readonly string[] FirstNames =
        {
            "James", "John", "Robert", "Michael", "William", "David", "Richard", "Joseph",
            "Thomas", "Charles", "Daniel", "Matthew", "Anthony", "Mark", "Paul", "Steven",
            "Andrew", "Kenneth", "Joshua", "Kevin", "Brian", "George", "Edward", "Ronald",
            "Timothy", "Jason", "Jeffrey", "Ryan", "Jacob", "Gary", "Nicholas", "Eric"
        };

        /// <summary>Last-name pool (32 entries). APPEND-only.</summary>
        public static readonly string[] LastNames =
        {
            "Smith", "Johnson", "Williams", "Brown", "Jones", "Garcia", "Miller", "Davis",
            "Rodriguez", "Martinez", "Hernandez", "Lopez", "Gonzalez", "Wilson", "Anderson", "Thomas",
            "Taylor", "Moore", "Jackson", "Martin", "Lee", "Perez", "Thompson", "White",
            "Harris", "Sanchez", "Clark", "Ramirez", "Lewis", "Robinson", "Walker", "Young"
        };
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                    |
// | 1.0     | 2026-07-15 | —      | Initial implementation.  |
#endregion
