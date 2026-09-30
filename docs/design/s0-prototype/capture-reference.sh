#!/usr/bin/env bash
# Created: 2026-09-30. Purpose: reproducible non-certifying real-data check for Gates E/F.
set -euo pipefail
repo_root="$(git -C "$(dirname "$0")" rev-parse --show-toplevel)"
capture_dir="$(mktemp -d)"
trap 'rm -rf "$capture_dir"' EXIT
python3 "$repo_root/tools/dotnet-ci/generate_projects.py"
cp "$repo_root/docs/design/s0-prototype/capture-reference.cs" "$capture_dir/Program.cs"
cat > "$capture_dir/Capture.csproj" <<EOF
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework></PropertyGroup>
  <ItemGroup><ProjectReference Include="$repo_root/src/match-client-web/match-client-web.gen.csproj" /></ItemGroup>
</Project>
EOF
dotnet run --project "$capture_dir/Capture.csproj" --configuration Release -- \
  "$repo_root/docs/design/s0-prototype/reference-data.js" "$(git -C "$repo_root" rev-parse HEAD)"
