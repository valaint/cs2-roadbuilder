# CS2 preview smoke test

This checklist is for the first in-game RealRoad Builder preview integration. The v0.6 tool is deliberately read-only: it raycasts terrain, runs the planning core, and draws overlays. It must not create, upgrade, delete, or modify any road/network entities.

## Build

Install the current Cities: Skylines II modding toolchain from the game, then build:

```powershell
dotnet build src/RealRoadBuilder.Mod/RealRoadBuilder.Mod.csproj -c Release
```

Run the core tests separately:

```powershell
dotnet test tests/RealRoadBuilder.Core.Tests/RealRoadBuilder.Core.Tests.csproj
```

## Activation

1. Start Cities: Skylines II and load a map/city.
2. Open **Options → RealRoad Builder**.
3. Leave the first test at 100 km/h, 40 m search cells, 3 planning iterations, and exceptional geometry disabled.
4. Press **Activate Preview Tool**.
5. Return to the map.

Expected: the cursor can raycast terrain and the normal build tools are not applying anything.

## Selection workflow

1. Left-click once on terrain to set the highway start point.
2. Move the cursor at least 100 m away.
3. Left-click again to set the end point and generate a preview.

Expected before the second click:

- cyan start marker
- cyan cursor marker
- dashed cyan start-to-cursor guide

Expected after a successful plan:

- the engineering corridor appears as a colored line on the terrain surface
- white = at grade
- yellow = cut
- green = embankment
- magenta = bridge candidate
- red = tunnel candidate
- start/end markers remain visible

The colored line is intentionally projected onto the terrain surface so underground/tunnel sections remain visible. It is a planning overlay, not the final 3D road mesh.

## Cancel/reset behavior

Press the normal cancel/Escape action:

1. with a generated preview: removes the preview but keeps the start point
2. with only a start point: clears the start point
3. with no selection: exits the RealRoad Builder tool

The **Clear Preview** button in Options should also clear the current selection/preview.

## Settings checks

Repeat a short test with:

- 60 km/h
- 80 km/h
- 100 km/h
- 120 km/h

Verify that all four values generate or fail cleanly according to the selected geometry constraints. Then test **Allow Exceptional Geometry** and confirm that it can permit candidates rejected in normal mode without crashing the tool.

## Safety checks

Before and after preview generation, verify:

- road count is unchanged
- no road cost is charged
- no buildings are demolished
- no zoning is modified
- no save-game/network entities are changed by the preview action

v0.6 contains no construction/apply pipeline. If any persistent network change occurs, treat it as a bug and do not merge the preview branch.

## Failure behavior

Try a deliberately difficult pair of endpoints, such as opposite sides of steep terrain with a short search window.

Expected:

- no malformed road appears
- the preview tool stays responsive
- `PreviewStatus` reports a failure reason in the mod settings
- the game log contains a RealRoad Builder warning rather than an unhandled exception

## Logs

Use the normal Cities: Skylines II log location and search for `RealRoadBuilder` / `RealRoad Builder` messages. The most useful v0.6 entries are mod load, successful preview summary, and caught planning failures.

## Not yet part of v0.6

- building footprint avoidance from live CS2 entities
- existing road/rail corridor costs
- water crossing detection
- custom in-map React/UI panel
- true 3D bridge/tunnel ghost meshes
- road/network construction

Those should be added only after this terrain-selection and overlay path is confirmed against the current game/toolchain build.
