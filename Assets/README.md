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

Open **this repository folder** as the Unity project (Unity Hub → Add → select the
repo root). Unity will generate `Library/`, `Temp/`, `Logs/`, `.meta` files, and
fill out `ProjectSettings/` on first open; commit the generated `src/**/*.meta`
files and `ProjectSettings/` after the first successful compile to lock asset GUIDs
(see `src/CLAUDE.md` → "WHAT IS NOT HERE YET").
