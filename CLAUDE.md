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
| `GiMap/Config/`            | `GiMapConfig` (one nested struct per mode), `ConfigManager` (`GiMapConfig.json`) and `GiMapSettingsTab` (content of the "GiMap" tab of the in-game settings: mode switches, opacity, min zoom, topographic/fertility options, "Reset settings" button that deletes the config after `GuiDialogConfirm`; baked options call `AMapLayer.Reload`, which purges the layer db and redraws) |
| `GiMap/Patches/`           | Harmony patches (`WorldMapManagerPatch` orders mode tabs after "terrain"; `GuiDialogWorldMapPatch` stretches the full-screen map to the window when big map is on; `GuiComposerHelpersPatch` widens its mode tabs to the longest name (vanilla cuts inactive names longer than 200); `GuiElementDialogTitleBarPatch` adds the toggle and "Map properties" to the map title bar menu; state is kept in client settings `gimapBigWorldMap`; `GuiElementMapPatch` lowers the min zoom to config `minZoom`, halving/doubling the step below vanilla 0.25, and remembers the map for `LodState`; `ChunkMapLayerPatch` gives the vanilla terrain layer LOD tiles; `GuiCompositeSettingsPatch` adds the "GiMap" tab button to the in-game settings; `MapContextMenuPatch` intercepts the vanilla "Add waypoint" dialog of a right click on the map and opens the context menu instead; `GuiDialogEditWayPointPatch` adds "Teleport here" (to the exact waypoint position, only with the `tp` privilege) under the buttons of the vanilla "Modify waypoint" dialog, injected in `GuiComposer.EndChildElements` of its composer; `WaypointNotesPatch` skips vanilla rendering and mouse handling of waypoints that store notes) |
| `GiMap/Client/`            | Map modes and their base classes (`AMapLayer`, `ABlockMapLayer`, `AChunkMapComponent`) |
| `GiMap/Client/Contours/`   | Shared contour code: `AContourMapLayer` (colors columns by `SampleColumn`, draws contour lines over a value with neighbor-chunk edges, index contour labels and peaks), `HeightLabel` (labels appended after the 1024 chunk pixels), `ContourLabelsMapComponent` (thins out and renders labels), `ContourLabelTextures`; used by Topographic and Bathymetric |
| `GiMap/Client/Storage/`    | "Map properties" dialog (`GuiDialogMapProperties`): sizes of the world map files per mode (`MapStorage` groups `Maps/<save>-<id>*.db`, vanilla `<save>.db` and `-terrain-lod.db` as terrain) and deleting them; open dbs are cleared on the map thread (`AMapLayer.ClearStorage`, `ChunkMapLayerPatch.ClearStorage`, `MapDbUtil.Clear`: purge + vacuum + WAL checkpoint), other files are deleted |
| `GiMap/Client/ContextMenu/` | Right-click menu of the world map (`GuiDialogMapContextMenu`): "Add waypoint" opens the intercepted vanilla dialog; "Add note" opens a second page with note types (see `Client/Notes/`); "Teleport here" (only with the `tp` privilege) sends `/tp` with absolute coordinates onto the surface, or to sea level when the chunk is not loaded on the client (`MapTeleport`) |
| `GiMap/Client/Notes/`      | Map notes (text, line, circle, rect, polygon/polyline) stored in titles of vanilla waypoints, so they work on servers without the mod: `{GIMAP_NOTE_<TYPE> key=value key="quoted \"text\""}`, points are block offsets from the waypoint position, color is the waypoint color (`MapNoteCodec`). `NotesMapLayer` has no map tab (removed in `WorldMapManagerPatch`), so it is always active; draws only on the full-screen map through a Cairo texture with a margin (`NotesOverlay`, `NoteRenderer`), right click on a note opens `GuiDialogMapNote`. New notes: context menu → type → points by clicks (`NoteDrawingSession`, panel `GuiDialogNoteDrawing`) → editor; saved with `/waypoint addati/modify/remove` (`MapNoteCommands`, index among own waypoints). `WaypointNotesPatch` hides the vanilla icon, hover and edit dialog of note waypoints |
| `GiMap/Client/Lod/`        | LOD for zoom below 0.5: `LodTileSet` merges chunk pieces into 128x128 tiles (2^level blocks per texel, level 2+), cached in `Maps/<save>-<layer>-lod.db`; a mode picks `LodMode` (`Tiles`, `Hidden`, `PerChunk`) |
| `GiMap/assets/game/lang/`  | Localization: `en`, `ru`, `sr`                                 |

## Modes
| Folder (`GiMap/Client/`) | Map type id          | Description                                  |
|--------------------------|----------------------|----------------------------------------------|
| Bathymetric              | `bathymetric`        | Displays depth of water bodies (from the water surface, ice included) as a shallow → medium → deep gradient (`mediumDepth`, `deepDepth`), land in one color; isobaths, index isobath depth labels and deepest points |
| Caves                    | `caves`              | Displays cave floors by layers of 32 blocks (layer 1 is the deepest); layer picked by slider or follows player Y |
| Chunk                    | `chunk-grid`         | Draws chunk grid                             |
| Fertility                | `fertility`          | Displays soil fertility; block lists in config, forest floor is low fertility drawn hatched; snow is skipped, fertile blocks under it are hatched white |
| GeologyActivity          | `geology-activity`   | Displays geology activity                    |
| Height                   | `height`             | Displays landscape height and ocean depths   |
| Light                    | `light`              | Displays light levels                        |
| Ore                      | `ore`                | Displays ore deposits (disabled by default); pixels store up to 4 ore groups of the column, shown ores are picked in the layer dialog (`hiddenOres`) without regenerating; `presets` toggle several ores at once |
| Precipitation            | `precipitation`      | Displays world precipitation on surface      |
| Temperature              | `temperature`        | Displays world temperature on surface        |
| TemporalStability        | `temporal-stability` | Displays temporal stability on surface       |
| Topographic              | `topographic`        | Displays type of surface: roads, soil, rock, etc., contour lines with height labels and peaks |
| Notes                    | `gimap-notes`        | Map notes in waypoint titles (text, line, circle, rectangle, polygon/polyline), no map tab, always shown on the full-screen map |

## Adding a new mode
1. Create `GiMap/Client/<Mode>/` with a `<Mode>MapLayer` and a map component.
2. Add the id to `MapTypes`.
3. Add a nested struct to `GiMapConfig` with `isEnabled` and the mode's colors, plus a `<Mode>Mode` field.
4. Register the layer in `GiMapModSystem.StartClientSide` behind `isEnabled`.
5. Add the id to `Order` in `WorldMapManagerPatch`.
6. Add the mode switch to `Modes` in `GiMapSettingsTab`.
7. Add `maplayer-<id>` and any legend/hover keys to **all** lang files (`en`, `ru`, `sr`).
8. Update the Modes table above.

## Rules
- Modes live in `GiMap/Client/<Mode>/`.
- Each mode has its own config struct in `GiMapConfig` with an `isEnabled` flag.
- Colors in config are hex strings (`#rrggbb` or `#rrggbbaa`).
- Every new localization key must be added to all lang files.
