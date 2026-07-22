# tools/unity-ci — Unity-specific PR gates

> **Created:** July 22, 2026
> **Purpose:** CI checks the pre-Unity pure-C# gate (`tools/dotnet-ci`) does not
> cover, now that the repo carries a real Unity 6 project (`ProjectSettings/`,
> `Packages/`, `.meta` files, asmdefs). Runs on Linux, no Unity install required.

## Scripts

| Script | CI job | What it enforces |
|---|---|---|
| `check-meta-integrity.sh` | **Unity .meta integrity** | Every tracked file/folder under `src/` has a committed `.meta`; no orphan `.meta` (asset deleted); no two `.meta` share a GUID. |
| `check-binaries.sh` | **Unity asset hygiene** | Any tracked file over the threshold (`TD_BINARY_THRESHOLD_BYTES`, default 1 MiB) must be routed to Git LFS by `.gitattributes`. |
| `generate-missing-metas.sh` | *(fix helper, not a gate)* | Writes CI-safe `.meta` files for anything missing one. `--check` mode lists gaps and exits non-zero. |

The Unity Test Runner job (`unity-tests`, EditMode + PlayMode inside real Unity
6) lives in `.github/workflows/ci.yml`. It is **gated on the `UNITY_LICENSE`
secret**: it runs once `UNITY_LICENSE` / `UNITY_EMAIL` / `UNITY_PASSWORD` repo
secrets are configured, and is cleanly **skipped** (PR stays green) until then.

## Why `.meta` integrity is a hard gate

A `.cs` (or any asset) without a committed `.meta` gets a **fresh random GUID**
from Unity on every checkout. Unity resolves all inter-asset references by GUID,
so a divergent GUID silently breaks every prefab/scene/asset that referenced the
file — and does so differently on each machine. This is the single most common
Unity PR footgun; catching it in CI is cheap.

## ⚠️ The generated placeholder `.meta` files still need Unity authoring

`generate-missing-metas.sh` derives each GUID from `md5(repo-relative-path)` so
the tree is **immediately GUID-consistent and reproducible without opening
Unity**. These metas are format-correct and reference-stable, but they are
**placeholders**: they were not authored by the Unity editor and carry only the
minimal importer block this repo already uses (no editor-populated importer
settings).

**Follow-up owed (do NOT skip):** when the project is next opened in Unity 6 on
the pinned certification host, let Unity enrich these metas — **but preserve the
existing GUIDs** (do not let Unity reassign them, or references break). The
metas generated on July 22, 2026 cover the previously-uncovered files under
`src/season-save/`, `src/match-engine/` (GkHeadingIntentSource, LineupSelector
and their tests), and `src/deterministic-sim/FloatFlagTuple.cs`.

## Running locally

```bash
bash tools/unity-ci/check-meta-integrity.sh
bash tools/unity-ci/check-binaries.sh
bash tools/unity-ci/generate-missing-metas.sh          # write any missing metas
bash tools/unity-ci/generate-missing-metas.sh --check   # report only
```

## `.gitattributes`

The repo `.gitattributes` (root) does two Unity-critical things beyond LFS
routing: forces `eol=lf` on text/YAML assets, and marks Unity YAML
(`.unity`/`.prefab`/`.asset`/`.meta`/…) `merge=unityyamlmerge` so merges can go
through Unity's Smart Merge tool. Configure the driver once per clone:

```bash
git config merge.unityyamlmerge.driver \
  '"<UnityInstall>/Tools/UnityYAMLMerge" merge -p %O %B %A %A'
```
