// File:     src/season-save/ClubNameCatalogue.cs
// Created:  2026-07-25
// Modified: 2026-07-25
// Author:   —
// Spec:     League Bootstrap design supplement (docs/tracking/league-bootstrap-design.md) KD-3;
//           Code Standards #20
// Purpose:  Stage-0 in-code club-name corpus for the league bootstrap. Club k always gets entry k
//           (KD-3: a new-game seed changes who is GOOD, not who EXISTS), so names are drawn from no
//           RNG stream and enter no digest.

namespace TacticalDirector.SeasonSave
{
    /// <summary>
    /// Stage-0 in-code club names, assigned by <c>ClubId</c> index (design KD-3). Same authoring
    /// pattern as <c>PlayerDatabase.NameCatalogue</c> and <c>LivingWorld.InteractionTextCorpus</c>: a
    /// later data swap, not a format.
    /// <para>
    /// <b>APPEND-only.</b> Club <c>k</c> is bound to entry <c>k</c>, so reordering or removing an entry
    /// renames existing clubs — including in an already-saved career, where the club id is what the
    /// season blob persists. The names themselves are never serialized.
    /// </para>
    /// <para>
    /// Length must be at least <see cref="LeagueBootstrapConstants.MaxClubCount"/>; a test locks that
    /// and per-entry uniqueness, so raising the cap cannot silently produce two identically named clubs.
    /// </para>
    /// </summary>
    public static class ClubNameCatalogue
    {
        /// <summary>Club names, indexed by <c>ClubId</c>. APPEND-only (see the type remarks).</summary>
        public static readonly string[] Names =
        {
            "Ashford Town",      "Belmont Rovers",    "Carrowmore City",   "Dunhaven United",
            "Eastvale Athletic", "Fernbrook FC",      "Granton Wanderers", "Halloway City",
            "Ironbridge United", "Juniper Park FC",   "Kesgrave Rovers",   "Langmere Athletic",
            "Marlow Heath",      "Northgate United",  "Oakenfield FC",     "Pendleton City",
            "Quarrywood Town",   "Redmarsh Rovers",   "Stanbury Athletic", "Thorncliff United",
            "Ullswater FC",      "Vansbrook City",    "Westmill Rovers",   "Yarrowgate Town",
            "Alderney Athletic", "Brackenhall FC",    "Coldwater United",  "Denholm Rovers",
            "Elmsworth City",    "Fallowfield Town",  "Greystoke United",  "Havelock Athletic",
            "Inverleith FC",     "Jarrowby Rovers",   "Kirkstall City",    "Lyndhurst Town",
            "Millgate United",   "Netherby Athletic", "Orrindale FC",      "Priorswood Rovers"
        };
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-07-25 | —      | Initial implementation (roadmap A3): 40 club names, ≥ MaxClubCount.|
#endregion
