# Assets/GameArt/

Unity-ready production art exports live here. Editable masters belong in `art-source/`; this tree contains only assets intended for Unity import plus Unity-side materials/prefabs/configuration derived from them.

## Planned families

```text
Assets/GameArt/
  Identity/
  UI/
  Match2D/
  Clubs/
  Portraits/
  Stadiums/
  Marketing/
```

Create each directory when the first real asset for that family is imported.

## Import rules

- Commit each imported asset's `.meta` file so its Unity GUID remains stable.
- Preserve lower-snake-case filenames for exported binaries.
- Avoid baked-in user-facing text; localization belongs to the UI/localization layer.
- Prefer reusable 9-sliced panels and separately tintable/renderable elements instead of fixed composites.
- Match-view assets must remain presentation-only; they do not encode or modify simulation state.
- Binary asset types covered by the repository `.gitattributes` are expected to be Git LFS objects.

See `docs/planning/art-pipeline-foundation.md` for naming, technical defaults, provenance, and acceptance checks.
