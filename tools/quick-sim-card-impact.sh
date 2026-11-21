#!/usr/bin/env bash
# Created: November 20, 2026
# Purpose: Emit the #44 T3 20-club fixture comparison as CSV for the specified checkout.

set -euo pipefail

repo="$(git -C "${1:-.}" rev-parse --show-toplevel)"
probe_dir="$(mktemp -d)"
trap 'rm -rf -- "$probe_dir"' EXIT

python3 "$repo/tools/dotnet-ci/generate_projects.py" >&2

cat > "$probe_dir/Probe.csproj" <<'EOF'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="$(QuickSimProbeRepo)/src/season-save/season-save.gen.csproj" />
  </ItemGroup>
</Project>
EOF

cat > "$probe_dir/Program.cs" <<'EOF'
using System;
using TacticalDirector.Discipline;
using TacticalDirector.LivingWorld;
using TacticalDirector.SeasonSave;

const ulong seed = 0x5EED1EA6D0DEC0DEUL;
var league = LeagueBootstrap.Generate(seed, 20);
foreach (bool wired in new[] { false, true })
{
    var discipline = wired ? new DisciplineState() : null;
    var loop = new SeasonLoop(new WorldStore(1, seed), league.CreateSeason(0),
        RoundResolutionMode.QuickSimAll, disciplineOrNull: discipline);
    while (!loop.IsSeasonComplete)
    {
        loop.AdvanceToNextFixtureDay();
        foreach (var result in loop.AdvanceAndPlayNextRound(league))
        {
            Console.WriteLine($"{wired},{result.RoundIndex},{result.HomeClubId},{result.AwayClubId},{result.HomeGoals},{result.AwayGoals}");
        }
    }
    Console.Error.WriteLine($"wired={wired} disciplineRows={discipline?.Count}");
}
EOF

dotnet build "$probe_dir/Probe.csproj" -c Release -p:QuickSimProbeRepo="$repo" >&2
dotnet "$probe_dir/bin/Release/net8.0/Probe.dll"
