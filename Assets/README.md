# Assets/

This folder makes the repository root a Unity project. All game source code lives
in the repo-root `src/` tree (45 assemblies, one `.asmdef` per spec folder — see
`src/CLAUDE.md`), **not** under `Assets/`.

Unity only compiles code found under `Assets/` or `Packages/`. To bring `src/`
into the compile without moving it, create a **directory junction** (Windows) or
**symlink** (macOS/Linux) named `Assets/Scripts` that points at `../src`:

**Windows (run from the repo root, cmd — not PowerShell):**
```
mklink /J Assets\Scripts src
```

**macOS / Linux (run from the repo root):**
```
ln -s ../src Assets/Scripts
```

The junction/symlink is a **local** link and is intentionally NOT committed
(`.gitignore` excludes `/Assets/Scripts`). Recreate it once per fresh clone.
Unity walks the junction and compiles every `src/**` file (and its `.asmdef`),
which is why compiler errors report `Assets\Scripts\...` paths — that is expected
junction behaviour, not a misconfiguration.

## Project configuration

- **Client scene:** `Assets/Scenes/Scene.unity`, the only scene in Build Settings. It hosts
  `MatchClientBehaviour`; its setup contract is `src/match-client-unity/README.md`.
- **Renderer:** the Built-in Render Pipeline. No URP package or pipeline asset is part of the
  project; moving to URP is a deliberate migration, not a setting to flip.
- **Input:** Active Input Handling is "Input Manager (Old)". `MatchClientBehaviour` reads the legacy
  `UnityEngine.Input` API, and the Input System package is not installed.
- **Packages:** `Packages/manifest.json` and `Packages/packages-lock.json` are committed as a pair.
  Change packages in the Unity editor so it regenerates the lock, and commit both files together.
  `com.unity.ai.assistant` is kept because it provides the Unity MCP relay the editor workflow uses.

Open **this repository folder** as the Unity project (Unity Hub → Add → select the
repo root). Unity will generate `Library/`, `Temp/`, `Logs/`, `.meta` files, and
fill out `ProjectSettings/` on first open; commit the generated `src/**/*.meta`
files and `ProjectSettings/` after the first successful compile to lock asset GUIDs
(see `src/CLAUDE.md` → "WHAT IS NOT HERE YET").
