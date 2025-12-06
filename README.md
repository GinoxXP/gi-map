# GiMap

Ultimative map for your Vintage Story

## Modes

At now moment the mod has 9 modes

![overview](.promo/world-map.png)

### Topographic

Surface type detection mode. Displays soil, sand, gravel, rock, and roads.

![topographic](.promo/topographic.png)

### Height

Altitude display mode. Works for both surface and underwater.

![height](.promo/height.png)

### Fertility

Displays soil fertility and is helpful when planning a garden.

![fertility](.promo/fertility.png)

### Precipitation

Displays the average annual precipitation level.

![precipitation](.promo/precipitation.png)

### Temperature

Displays the average annual temperature level.

![temperature](.promo/temperature.png)

### Geological activity

Displays the level of geological activity. 
Areas with high values will have geothermal vents on the surface and lava lakes underground.

![geological-activity](.promo/geological-activity.png)

### Lighting

Displays the level of artificial lighting. Useful for illuminating settlements and identifying dark areas.

![lighting](.promo/lighting.png)

### Chunk grid

Brutal chunk grid. x32, x16 and x8.

![chunk-grid](.promo/chunk-grid.png)

### Ore

Displays ore deposits.

![chunk-grid](.promo/ore.png)

### Temporal stability

Build your house so that your friend's room is in the unstable zone.

![temporal-stability](.promo/temporal-stability.png)

## Mix modes

Why? Yes.

![mix](.promo/mix.png)

## Config

You can change default config. It can be founded by path _\VintagestoryData\ModConfig\GiMapConfig.json_

All colors defined as HEX format, ex.
```json
"TopographicMode": {
    "soilColor": "#c9ea9d",
    "sandColor": "#ffffff",
    "gravelColor": "#c8c8c8",
    "stoneColor": "#969696",
    "waterColor": "#22a4ab",
    "iceColor": "#caedee",
    "snowColor": "#e6e6ff",
    "roadColor": "#323232"
  },
```

or RGBA, ex.
```json
"mountainColor": {
      "X": 255,
      "Y": 0,
      "Z": 0,
      "W": 255
    },
```

You can disable unnecessary modes. For do it change
```json
"isEnable":
```

to false

## Credits

I used [Geology Map](https://github.com/carlosganhao/VS-GeologyMap) as an example. I'm not sure if
anything from the original solution remains as this point, but I decided to mention it nonetheless.

[TemporalityMap](https://github.com/kryptokatze/TemporalityMap/)