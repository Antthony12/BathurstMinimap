# Bathurst Minimap

A mod inspired by nyconing's [Nürburgring minimap](https://www.gta5-mods.com/scripts/vans123-s-nurburgring-nordschleife-minimap), adapted to the [Bathurst](https://www.gta5-mods.com/es/maps/bathurst-mount-panorama-add-on-fivem) circuit by [ON3FLY3R](https://www.gta5-mods.com/es/users/ON3FLY3R).

This project is distributed under the MIT license.

`Bathurst.png` is traced from in-game telemetry (positions recorded while driving a lap of the circuit), so the layout is accurate.

## Files

- `BathurstMinimap.dll` — Main script, draws the map and the pin. Includes the real telemetry points used to detect whether the player is near the circuit directly embedded in the code.
- `BathurstMinimap.ini` — Configuration file for the circuit and for the on-screen position/size of the map/pin.
- `BathurstMinimap/Bathurst.png` — Real circuit layout, generated from telemetry.
- `BathurstMinimap/pin.png` — The same marker used by nyconing.

## Configuration (`BathurstMinimap.ini`)

```ini
[Circuit]
OffsetX=0
OffsetY=0
OnTrackDistance=20

[Map]
PosX=-80
PosY=0
Width=400
Height=400

[Pin]
Width=6
Height=6
```

- `OffsetX`/`OffsetY`: Circuit displacement, in case the map mod ever gets moved in the GTA world (translation only, not rotation).
- `OnTrackDistance`: How close (in game units, approximately meters) the player needs to be to the circuit for the map to appear.
- `PosX`/`PosY`: top-left corner of the map on screen (reference resolution 1280x720).
- `Width`/`Height`: map size on screen.
- `[Pin] Width`/`Height`: size of the position marker.
- Use a dot (`.`) as the decimal separator, not a comma.
- If the file is missing or a value is invalid, the script warns on screen and falls back to the default for that field only.
- Reload scripts (or restart the game) after editing for changes to apply.

## 1. Build

You can build it in Visual Studio (.NET Framework Class Library project +
the `ScriptHookVDotNet3` NuGet package), or simply drop the loose `.cs`
file into GTA V's `scripts/` folder — SHVDN compiles it on game start.

## 2. Install

Copy the files into GTA V's `scripts/` folder:
- `BathurstMinimap.dll`
- `BathurstMinimap.ini`
- `BathurstMinimap/`
  - `Bathurst.png`
  - `pin.png`

Requires the [Bathurst by ON3FLY3R](https://www.gta5-mods.com/es/maps/bathurst-mount-panorama-add-on-fivem) map to be installed.

## Behavior

- The map **only appears when you're near the real circuit**. Nothing is drawn outside the circuit.
- Adjust `OnTrackDistance` in `BathurstMinimap.ini` if you want the detection radius to be stricter or more permissive.

## Notes

- If the pin looks slightly off in a specific part of the track, let me know and I'll look at adjusting it.
- Requires Script Hook V + Script Hook V .NET (ScriptHookVDotNet3).
