# GiMap: Real Colors Terrain Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** A client add-on mod `gimapterrain` that makes the vanilla terrain map draw real block colors into the vanilla map db.

**Architecture:** New project `GiMap.Terrain/` next to `GiMap.Underground/`. Its mod system applies its own Harmony instance; one patch forces the private vanilla field `ChunkMapLayer.colorAccurate` to `true` after `OnLoaded` and `OnMapOpenedClient`. Everything else (generation, `<save>.db`, core terrain LOD, cleanup) stays vanilla/core.

**Tech Stack:** C# / .NET 10, Vintage Story API 1.22, HarmonyLib, Cake Frosting (`ZZCakeBuild`).

**Spec:** `docs/superpowers/specs/2026-11-04-terrain-real-colors-addon-design.md`

## Global Constraints

- modid `gimapterrain`, name `GiMap: Real Colors Terrain`, `side: Client`, version `1.0.0`, dependencies `"game": "1.22.0"`, `"gimapcore": "3.0.0"`.
- Harmony id `gimapterrain`; never call `UnpatchAll()` without the id (it would remove the core patches).
- No config, no settings switch, no lang keys, no map tab.
- No automated tests in the repository: verification is `dotnet build`, `dotnet run --project ZZCakeBuild` and the in-game checks listed per task.
- Log prefix `[gimapterrain]`.

## Review Focus

- Map reopened (`OnMapOpenedClient` resets the field from world config) → must stay real colors. Checked in Task 2, step 4.
- Server world already has `colorAccurateWorldmap: true` → add-on is a no-op, nothing breaks. Checked in Task 2, step 4 (`/worldconfig colorAccurateWorldmap true` on a singleplayer world).
- Game update renamed the field → one warning in the log, vanilla map, no exception. Checked by code review of Task 2 (static ctor try/catch, `IsSupported` guard in both postfixes).
- Core `harmonyInstance.PatchAll()` uses the calling assembly, so it does not pick up add-on patches; the add-on must patch its own assembly. Checked in Task 2, step 4 (real colors visible at all).
- Far zoom LOD built from the same pixels → real colors after regeneration. Checked in Task 2, step 4.

---

### Task 1: Add-on project, mod system and build wiring

**Files:**
- Create: `GiMap.Terrain/GiMap.Terrain.csproj` (copy of `GiMap.Underground/GiMap.Underground.csproj`, unchanged content)
- Create: `GiMap.Terrain/modinfo.json`
- Create: `GiMap.Terrain/modicon.png` (copy of `GiMap/modicon.png`)
- Create: `GiMap.Terrain/TerrainModSystem.cs`
- Modify: `GiMap.sln` (add project, Debug/Release configs like `GiMap.Underground`)
- Modify: `ZZCakeBuild/BuildContext.cs:33` → `ProjectNames = ["GiMap", "GiMap.Underground", "GiMap.Terrain"]`
- Modify: `GiMap/GiMap.csproj:68-74` (target `BuildAddons`: add the same Restore + Build `MSBuild` pair for `..\GiMap.Terrain\GiMap.Terrain.csproj`)
- Modify: `GiMap/Properties/launchSettings.json` (profile `Client`: append `"$(ProjectDir)/../GiMap.Terrain/bin/$(Configuration)/Mods"` to `--addModPath`, after the Underground path)
- Modify: `CLAUDE.md`

**Interfaces:**
- Produces: `namespace GiMap.Terrain; public class TerrainModSystem : ModSystem` with `private const string PatchId = "gimapterrain";` and a Harmony instance that patches `typeof(TerrainModSystem).Assembly`. Task 2 adds patch classes to this assembly; no other wiring is needed.

- [ ] **Step 1: Create the project files**

`modinfo.json` per Global Constraints, `"type": "Code"`, `"authors": ["Ginox"]`, `"description": "Real block colors for the terrain map of GiMap"`. Add with `dotnet sln GiMap.sln add GiMap.Terrain/GiMap.Terrain.csproj`.

- [ ] **Step 2: Implement `TerrainModSystem`**

- `ShouldLoad(EnumAppSide forSide) => forSide == EnumAppSide.Client`
- `ExecuteOrder() => base.ExecuteOrder() + 0.01` (after the core)
- `StartPre(ICoreAPI api)`: like the core, `if (!Harmony.HasAnyPatches(PatchId)) { _harmony = new Harmony(PatchId); _harmony.PatchAll(typeof(TerrainModSystem).Assembly); }`
- `Dispose()`: `_harmony?.UnpatchAll(PatchId); base.Dispose();`
- XML doc on the class: separate mod so servers can forbid it by mod id, see `ColorAccurateTerrainPatch`.

- [ ] **Step 3: Wire the build** (`BuildContext`, `BuildAddons`, `launchSettings.json` as listed in Files)

- [ ] **Step 4: Update `CLAUDE.md`**

- Intro: three mods — core `GiMap/` (mod id `gimapcore`), `GiMap.Underground/` (mod id `gimapcavesnores`, depends on `gimapcore`), `GiMap.Terrain/` (mod id `gimapterrain`, depends on `gimapcore`, real block colors on the vanilla terrain map); keep all three `modinfo.json` versions and the `gimapcore` dependencies in sync.
- Build section: "building `GiMap.csproj` also builds the add-ons".
- Structure table row `GiMap.Terrain/`: `TerrainModSystem` (own Harmony id `gimapterrain`, patches its assembly), `Patches/ColorAccurateTerrainPatch` forces the vanilla `ChunkMapLayer.colorAccurate` on the client (texture color with climate tint + relief, same `<save>.db`, core terrain LOD works unchanged); no config. References `GiMap.csproj` with `Private=false`.

- [ ] **Step 5: Verify the build**

Run: `dotnet build GiMap.sln`
Expected: `Build succeeded`, `GiMap.Terrain/bin/Debug/Mods/mod/GiMap.Terrain.dll` and `modinfo.json` exist.

Run: `dotnet run --project ZZCakeBuild`
Expected: success, `Releases/` contains a zip for `gimapterrain` next to the core and underground zips.

- [ ] **Step 6: Commit**

```bash
git add GiMap.Terrain GiMap.sln ZZCakeBuild/BuildContext.cs GiMap/GiMap.csproj GiMap/Properties/launchSettings.json CLAUDE.md
git commit -m "add: GiMap.Terrain addon project"
```

---

### Task 2: Force color-accurate terrain map

**Files:**
- Create: `GiMap.Terrain/Patches/ColorAccurateTerrainPatch.cs`

**Interfaces:**
- Consumes: `TerrainModSystem` Harmony instance patching its own assembly (Task 1).
- Produces: `namespace GiMap.Terrain.Patches; static class ColorAccurateTerrainPatch` with nested `[HarmonyPatch(typeof(ChunkMapLayer), "OnLoaded")]` and `[HarmonyPatch(typeof(ChunkMapLayer), "OnMapOpenedClient")]` postfix classes.

- [ ] **Step 1: Implement the patch**

- `private static readonly AccessTools.FieldRef<ChunkMapLayer, bool>? ColorAccurate;` and `private static readonly bool IsSupported;` set in a static constructor with try/catch around `AccessTools.FieldRefAccess<ChunkMapLayer, bool>("colorAccurate")`, same pattern as `GiMap/Patches/ChunkMapLayerPatch.cs`.
- Shared `private static void Apply(ChunkMapLayer layer, ICoreAPI api)`: return unless `api is ICoreClientAPI`; if `!IsSupported`, log `api.Logger.Warning("[gimapterrain] Unknown terrain map layer fields, real colors are disabled")` once (static `bool _warned`) and return; else `ColorAccurate!(layer) = true`.
- `OnLoadedPatch.Postfix(ChunkMapLayer __instance, ICoreAPI ___api)` and `OnMapOpenedClientPatch.Postfix(ChunkMapLayer __instance, ICoreAPI ___api)` call `Apply`.
- XML doc: vanilla enables the mode only by world config / privilege `colorAccurateWorldmap` and reassigns it in the constructor and `OnMapOpenedClient`; the pixels go to the same `<save>.db`.

- [ ] **Step 2: Build**

Run: `dotnet build GiMap.sln`
Expected: `Build succeeded`, no new warnings in `GiMap.Terrain`.

- [ ] **Step 3: Launch the game**

Run the `Client` launch profile of `GiMap`, open a singleplayer world without `colorAccurateWorldmap`.
Expected: `client-main.log` lists mod `gimapterrain`, no `[gimapterrain]` warning, no Harmony exception.

- [ ] **Step 4: In-game checks**

1. Open the full-screen map, terrain tab: chunks around the player show texture colors with tint and relief shading.
2. Close and reopen the map: still real colors.
3. Zoom out below 0.5 (LOD): regenerated area shows real colors.
4. `.map redraw`: the explored area is redrawn in real colors.
5. `/worldconfig colorAccurateWorldmap true`, reload the world: map unchanged, no errors.
6. Start without the add-on (remove its path from `--addModPath` temporarily): new chunks get vanilla colors.

- [ ] **Step 5: Commit**

```bash
git add GiMap.Terrain/Patches/ColorAccurateTerrainPatch.cs
git commit -m "add: real block colors on the terrain map"
```
