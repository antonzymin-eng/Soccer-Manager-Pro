# `TacticalDirector.MatchClientUnity` — Unity-only render/UGUI skin

**Status:** scaffolded at P0 (asmdef only, no scripts yet). The render skin lands at
**P4–P6** of the interactive Unity client plan
(`docs/tracking/interactive-unity-client-design.md`).

## Why this assembly is empty right now

This is the **Unity-host** half of the interactive Unity client. It will hold the
`MonoBehaviour` render/camera/HUD skin and the UGUI screens (P4/P5) — types that need a
Unity host (`Camera`, `SpriteRenderer`, `GameObject`, UGUI). None of that exists yet, and
there is no Unity host in the CI environment, so the folder currently carries only its
`.asmdef` (which keeps it tracked and lets the exclusion below take effect).

All **determinism-bearing** logic — the session, command channel, tick-stamped log, and
view-state math — lives in the host-free sibling `src/match-client-core/`
(`TacticalDirector.MatchClientCore`), which the `tools/dotnet-ci` shim gate compiles and
tests on every push. This assembly only ever depends on that core plus the reused
`match-viewer` streamer; it adds a skin, never new engine-facing logic.

## Excluded from the shim gate

`tools/dotnet-ci/generate_projects.py` compiles every `src/**/*.asmdef` against a ~9-type
UnityEngine shim with **no** rendering types. This assembly is therefore listed in that
generator's `SHIM_EXCLUDED_ASMDEFS` set — it is never generated, compiled, or referenced by
the Linux gate. It is verified only on the pinned Unity host at a cert run. The host-free
core stays in CI; only this skin sits outside it.
