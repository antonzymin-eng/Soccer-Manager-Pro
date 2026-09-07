# tools/unity-ci — Unity-specific PR gates

> **Created:** July 22, 2026
> **Updated:** September 6, 2026
> **Purpose:** CI checks the pre-Unity pure-C# gate (`tools/dotnet-ci`) does not
> cover, now that the repo carries a real Unity 6 project (`ProjectSettings/`,
> `Packages/`, `.meta` files, asmdefs). Runs on Linux, no Unity install required.

## Scripts

| Script | CI job | What it enforces |
|---|---|---|
| `check-meta-integrity.sh` | **Unity .meta integrity** | Missing/orphan metas for the managed `src/` + `Assets/GameArt/` roots; one project-wide duplicate-GUID scan across every tracked `.meta` under `Assets/` plus the junction-backed `src/` tree. |
| `check-binaries.sh` | **Unity asset hygiene** | Any tracked file over the threshold (`TD_BINARY_THRESHOLD_BYTES`, default 1 MiB) must be routed to Git LFS by `.gitattributes`. |
| `generate-missing-metas.sh` | *(fix helper, not a gate)* | Preserves existing deterministic placeholder generation under `src/`; for `Assets/GameArt/`, writes **folder metas only**. `--check` reports generator-owned gaps and exits non-zero. |
| `test-meta-integrity-gameart.sh` | *(mutation proof)* | Proves missing GameArt meta, orphan GameArt meta, GameArt↔`src/` duplicate GUID, and GameArt↔other-`Assets/` duplicate GUID are detected without modifying the real Git index. |

The Unity Test Runner job (`unity-tests`, EditMode + PlayMode inside real Unity
6) lives in `.github/workflows/ci.yml`. It is **gated on the `UNITY_LICENSE`
secret**: it runs once `UNITY_LICENSE` / `UNITY_EMAIL` / `UNITY_PASSWORD` repo
secrets are configured, and is cleanly **skipped** (PR stays green) until then.

## Why `.meta` integrity is a hard gate

A `.cs` or Unity asset without a committed `.meta` gets a **fresh random GUID**
from Unity on checkout. Unity resolves inter-asset references by GUID. Missing
identity therefore breaks references, and duplicate GUIDs are unsafe even when
the colliding assets live in different folders.

AP-01 intentionally separates two scopes:

- **missing/orphan ownership:** `src/` and `Assets/GameArt/`;
- **duplicate-GUID ownership:** every tracked `.meta` under `Assets/` plus the
  junction-backed `src/` tree, scanned as one universe.

The second scope is project-wide because a new GameArt texture must not be able
to collide with an existing scene, plugin, reference asset, or any other Unity
asset elsewhere under `Assets/`.

## Placeholder generation boundary

`generate-missing-metas.sh` derives helper-owned GUIDs from
`md5(repo-relative-path)` so those paths are immediately stable and
reproducible without opening Unity.

For **`src/`**, the existing behavior remains: source files and folders may get
minimal placeholder metas. Unity may later enrich them, but the GUID must be
preserved.

For **`Assets/GameArt/`**, the helper is deliberately narrower:

- it may create missing **folder** metas;
- it may support temporary CI-safety fixtures used by the mutation proof;
- it must **not** create production file metas for textures, vector exports,
  fonts, or other imported art assets.

Production GameArt file metas must come from an actual Unity import so their
`TextureImporter`, font importer, or other importer-specific settings are
editor-authored from the start. AP-03 owns that import proof.

`generate-missing-metas.sh --check` therefore checks only generator-owned paths.
Use `check-meta-integrity.sh` for the full integrity gate.

## Running locally

```bash
bash tools/unity-ci/check-meta-integrity.sh
bash tools/unity-ci/check-binaries.sh
bash tools/unity-ci/generate-missing-metas.sh          # write eligible missing metas
bash tools/unity-ci/generate-missing-metas.sh --check  # report eligible gaps only
bash tools/unity-ci/test-meta-integrity-gameart.sh     # mutation proof
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
