# GiMap
Client-side mod for Vintage Story that adds new world map modes (map layers).

## Stack
- C# / .NET 10 (`global.json`), Vintage Story API, Harmony
- Game DLLs are referenced via the `VINTAGE_STORY` environment variable (path to the game install)
- Build/packaging: Cake Frosting project in `ZZCakeBuild/`

## Build
```bash
dotnet build GiMap/GiMap.csproj
```
Release zip (validates JSON, builds, packages into `Releases/`):
```bash
dotnet run --project ZZCakeBuild
```
There are no automated tests; verify changes in the game.

## Structure
| Path                       | Purpose                                                        |
|----------------------------|----------------------------------------------------------------|
| `GiMap/GiMapModSystem.cs`  | Entry point: loads config, applies Harmony patches, registers map layers |
| `GiMap/MapTypes.cs`        | String ids of map layers                                       |
| `GiMap/Config/`            | `GiMapConfig` (one nested struct per mode) and `ConfigManager` (`GiMapConfig.json`) |
| `GiMap/Patches/`           | Harmony patches (`WorldMapManagerPatch` orders mode tabs after "terrain"; `GuiDialogWorldMapPatch` stretches the full-screen map to the window when `bigWorldMap` is set) |
| `GiMap/Client/`            | Map modes and their base classes (`AMapLayer`, `ABlockMapLayer`, `AChunkMapComponent`) |
| `GiMap/assets/game/lang/`  | Localization: `en`, `ru`, `sr`                                 |

## Modes
| Folder (`GiMap/Client/`) | Map type id          | Description                                  |
|--------------------------|----------------------|----------------------------------------------|
| Chunk                    | `chunk-grid`         | Draws chunk grid                             |
| Fertility                | `fertility`          | Displays soil fertility                      |
| GeologyActivity          | `geology-activity`   | Displays geology activity                    |
| Height                   | `height`             | Displays landscape height and ocean depths   |
| Light                    | `light`              | Displays light levels                        |
| Ore                      | `ore`                | Displays ore deposits (disabled by default)  |
| Precipitation            | `precipitation`      | Displays world precipitation on surface      |
| Temperature              | `temperature`        | Displays world temperature on surface        |
| TemporalStability        | `temporal-stability` | Displays temporal stability on surface       |
| Topographic              | `topographic`        | Displays type of surface: roads, soil, rock, etc., and contour lines |

## Adding a new mode
1. Create `GiMap/Client/<Mode>/` with a `<Mode>MapLayer` and a map component.
2. Add the id to `MapTypes`.
3. Add a nested struct to `GiMapConfig` with `isEnabled` and the mode's colors, plus a `<Mode>Mode` field.
4. Register the layer in `GiMapModSystem.StartClientSide` behind `isEnabled`.
5. Add the id to `Remove`/`Insert` in `WorldMapManagerPatch`.
6. Add `maplayer-<id>` and any legend/hover keys to **all** lang files (`en`, `ru`, `sr`).
7. Update the Modes table above.

## Rules
- Modes live in `GiMap/Client/<Mode>/`.
- Each mode has its own config struct in `GiMapConfig` with an `isEnabled` flag.
- Colors in config are hex strings (`#rrggbb` or `#rrggbbaa`).
- Every new localization key must be added to all lang files.
