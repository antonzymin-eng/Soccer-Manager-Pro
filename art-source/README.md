# art-source/

Editable art-production sources live here. This tree is intentionally outside Unity's `Assets/` folder so Unity imports only controlled game-ready exports rather than every working file.

## Layout

```text
art-source/
  identity/        # logo/wordmark exploration and master files
  ui/              # UI masters, icon masters, panel construction sources
  match-2d/        # pitch/marker/effect masters
  clubs/           # fictional badge + kit masters
  portraits/       # fictional player/staff portrait masters
  stadiums/        # stadium/environment masters
  marketing/       # later Steam/store/press sources
  references/      # reference boards that are safe to retain in-repo
```

Create subdirectories as those families actually begin production; do not create empty directory trees merely to mirror the plan.

## Source rules

- Editable revisions may use `_v001`, `_v002`, etc.; game-ready exports should not.
- Keep commercial-use provenance with the source asset or its batch.
- Do not place unlicensed copyrighted production art in this repository.
- Do not use real club badges, competition logos, sponsor marks, or real-player likenesses as shippable assets unless rights are explicitly secured.
- Binary formats covered by the root `.gitattributes` are stored through Git LFS.
- Export only approved assets into `Assets/GameArt/`.

See `docs/planning/art-pipeline-foundation.md` for the active pipeline and acceptance rules.
