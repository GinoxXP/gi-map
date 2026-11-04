# GiMap: Real Colors Terrain — design

Date: 2026-11-04
Status: approved in brainstorming, awaiting spec review

## Goal

A separate add-on mod (like `GiMap.Underground`) that replaces the colors of the vanilla terrain map
(`ChunkMapLayer`, the "terrain" tab) with real block colors. It writes into the same vanilla map db
(`Maps/<save>.db`). It is a separate mod so a server can forbid it by mod id while keeping the core GiMap.

### Agreed requirements

- Pixel color = block texture color with climate/season tint, plus the vanilla relief shading.
- No new map tab: the vanilla terrain tab shows the new colors.
- Same db as vanilla, vanilla loading/saving/rendering, core terrain LOD and "Map properties" cleanup keep working.
- Already explored map is left as is (mixed vanilla and real colors); chunks are recolored when they are
  generated again (loaded on the client), a full redraw is manual (`.map redraw` or "Map properties").
  Removing the add-on leaves the same mixed state the other way round.
- No config, no settings switch, no lang keys: the add-on installed means real colors.

## Approach

Vanilla `ChunkMapLayer` already has a color-accurate mode: private field `bool colorAccurate`, passed to
`GenerateChunkImage(chunkPos, mc, colorAccurate)`. With it the pixel is
`ColorOverlay(block.GetColor(capi, pos), block.GetRandomColor(capi, pos, UP, ...), colorRandomizationWeight)`
with the same relief shading, stored in the same db. Vanilla enables it only by world config
`colorAccurateWorldmap` or the player privilege `colorAccurateWorldmap`, assigned in the constructor
and in `OnMapOpenedClient`.

The add-on forces the field to `true` on the client with Harmony. The vanilla algorithm is used as is:
snow is not skipped (drawn white), water takes its block color with tint, no lake-edge highlight,
texture graininess with vanilla weight 0.6 (`colorRandomizationWeight`, world config).

Rejected:
- Own `GenerateChunkImage` (prefix replacing the method): full control, but ~200 lines copied from vanilla
  that must follow game updates. Can be done later without changing the db format.
- Own layer opening the same db: vanilla keeps the file open from its thread, two writers in one SQLite file
  and broken cleanup in "Map properties".

## Structure

New project `GiMap.Terrain/`:

| Path                                          | Purpose |
|-----------------------------------------------|---------|
| `GiMap.Terrain/GiMap.Terrain.csproj`          | Same as `GiMap.Underground.csproj`: references `GiMap.csproj` with `Private=false` and the game DLLs, output `bin/$(Configuration)/Mods/mod` |
| `GiMap.Terrain/modinfo.json`                  | modid `gimapterrain`, name "GiMap: Real Colors Terrain", `side: Client`, version `1.0.0`, dependencies `game 1.22.0`, `gimapcore 3.0.0` |
| `GiMap.Terrain/modicon.png`                   | Icon (copy of the core one until a new one is drawn) |
| `GiMap.Terrain/TerrainModSystem.cs`           | Client only, `ExecuteOrder` after the core (`base + 0.01`); `StartPre` (like the core, before the map layers are created) applies `new Harmony("gimapterrain").PatchAll(...)` for its own assembly (the core `PatchAll()` only patches the core assembly), `Dispose` unpatches by that id |
| `GiMap.Terrain/Patches/ColorAccurateTerrainPatch.cs` | Forces `ChunkMapLayer.colorAccurate = true` |

Build wiring:
- Add the project to `GiMap.sln`.
- Add `"GiMap.Terrain"` to `BuildContext.ProjectNames` in `ZZCakeBuild`.
- Add Restore + Build of `GiMap.Terrain.csproj` to the `BuildAddons` target of `GiMap.csproj`, so the
  `Client` launch profile can load it.
- `GiMap/Properties/launchSettings.json`, profile `Client`: append
  `"$(ProjectDir)/../GiMap.Terrain/bin/$(Configuration)/Mods"` to `--addModPath` (no `--addOrigin`, the add-on
  has no assets).

## Patch

`ColorAccurateTerrainPatch`:
- Static `AccessTools.FieldRef<ChunkMapLayer, bool>` for `"colorAccurate"`, created in a static constructor
  inside try/catch; `IsSupported` flag like `ChunkMapLayerPatch`.
- Postfix on `ChunkMapLayer.OnLoaded` and on `ChunkMapLayer.OnMapOpenedClient`: when supported and the api is
  client side, set the field to `true`. `OnLoaded` runs after the constructor assignment and before chunks are
  generated; `OnMapOpenedClient` reassigns the field on every map open, so it is overridden right after.
- When the field is missing (game update), log once
  `[gimapterrain] Unknown terrain map layer fields, real colors are disabled` and leave vanilla behavior.

Data flow is unchanged vanilla: `OnOffThreadTick` → `GenerateChunkImage(..., colorAccurate: true)` →
`toSaveList` → `<save>.db`. The core terrain LOD (`-terrain-lod.db`, `ChunkMapLayerPatch`) reads the same
pixels, so LOD tiles get real colors too. Both patch sets touch different methods/fields and do not conflict.

## Documentation

- `CLAUDE.md`: describe the repository as three mods, add `GiMap.Terrain/` to the Structure table, fix the
  outdated mod ids (`gimap` → `gimapcore`, `gimapunderground` → `gimapcavesnores`), mention keeping the
  third `modinfo.json` in sync.

## Verification

No automated tests in the repository.
1. `dotnet build GiMap.sln` — no errors.
2. `dotnet run --project ZZCakeBuild` — three zips in `Releases/`, JSON validated.
3. In game (`Client` launch profile), world without `colorAccurateWorldmap`:
   - log shows the add-on loaded, no `[gimapterrain]` warning;
   - terrain near the player shows texture colors with tint and relief shading;
   - closing and reopening the map keeps real colors (the `OnMapOpenedClient` override works);
   - far zoom (LOD) shows real colors after the chunks were regenerated;
   - `.map redraw` redraws the explored area in real colors;
   - without the add-on the map generates vanilla colors again.
