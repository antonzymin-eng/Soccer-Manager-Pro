# System XI art-source contract

`art-source/` is the editable/source side of the System XI art pipeline. It is
separate from Unity runtime exports under `Assets/GameArt/`.

This directory is infrastructure, not a signal that production art volume has
started. G0 authorizes AP-01 only.

## Source versus runtime

- Editable masters, generation workflows, licensed source packages, and
  production-candidate source files live under `art-source/`.
- Unity-ready exports eventually live under `Assets/GameArt/` only after the
  applicable import/integration gate.
- `Assets/GameArt/` is intentionally **not** created by AP-01 merely to reserve
  folders. AP-03 creates/imports the first real runtime asset through Unity.
- Final storefront/press exports remain outside the Unity shipping tree under
  the future `release-art/` root.

## Initial source layout

The accepted plan reserves these semantic families as they become needed:

```text
art-source/
  _templates/
  _quarantine/
  identity/
  typography/
  ui/
  match/
  clubs/
  people/
  stadiums/
  marketing/
```

AP-01 creates only `_templates/` because it has an immediate consumer. Other
folders are created when real candidates exist; empty speculative trees are not
part of the contract.

`_quarantine/` is never an export source. Unknown-rights or suspect material is
blocked there (or retained only as an external reference) until disposition.

## Authoritative asset metadata

A production candidate uses one adjacent `.art.json` sidecar. The authoritative
initial schema is:

`docs/design/art/art-asset.schema.json`

The template is:

`art-source/_templates/example.art.json`

The sidecar records semantic identity, family, lifecycle state, style version,
target surface, provenance/rights, real-person/real-club risk, exports, and any
exception notes. Generated candidates additionally record generation context.

There is no competing Markdown provenance record.

## Revision policy

For the same semantic asset, revise the same source/export path and let Git
provide history. Do not create `_v001`, `_v002`, `_final`, `_new`, or similar
copies merely to preserve revisions.

A suffix is valid only when variants intentionally coexist as different
semantic assets.

## Runtime export formats — AP-01 boundary

Allowed initial runtime categories are intentionally narrow:

- PNG raster exports for approved 2D texture/sprite use;
- approved/licensed TTF or OTF font binaries when typography reaches its import
  proof;
- Unity-native text/YAML assets only when an actual consumer and Unity import
  path require them.

Source-only/editable formats do **not** belong in `Assets/GameArt/`, including
PSD, TIFF, EXR/HDR working masters and similar authoring files. Vector masters
remain source-side unless a later proven Unity runtime path explicitly requires
a vector format; the initial safe export is raster.

3D production is deferred to AP-14 even though the repository already has LFS
routing for common model formats. Marketing/reference media also stays outside
`Assets/GameArt/`.

No reusable runtime art may bake user-facing localized copy by default.

## Unity `.meta` boundary

- `check-meta-integrity.sh` owns missing/orphan checks for `src/` and
  `Assets/GameArt/` and project-wide duplicate-GUID detection across tracked
  `Assets/` + `src/` metas.
- `generate-missing-metas.sh` may generate GameArt **folder** metas only.
- Production GameArt file metas must come from actual Unity import so importer
  blocks are authored correctly. AP-03 owns that proof.
