# CS2 preview smoke test

This checklist covers the read-only RealRoad Builder in-game preview integration through v0.7. The tool raycasts terrain, snapshots live-world constraints, runs the planning core, and draws overlays. It must not create, upgrade, delete, or modify any road/network entities.

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
3. Leave the first test at 100 km/h, 40 m search cells, 3 planning iterations, exceptional geometry disabled, and **Use Live World Constraints** enabled.
4. Keep **Buildings Are Hard Obstacles** enabled.
5. Press **Activate Preview Tool**.
6. Return to the map.

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
- `PreviewStatus` includes the captured building, road-edge, and rail-edge counts when live-world constraints are enabled

The colored line is intentionally projected onto the terrain surface so underground/tunnel sections remain visible. It is a planning overlay, not the final 3D road mesh.

## Live building constraints

Choose two endpoints where the direct route would cross an existing developed block.

With **Buildings Are Hard Obstacles** enabled:

- the route should avoid transformed building footprints plus the configured building clearance
- it must never cross straight through a building footprint simply because the A* cell center falls outside it
- the building count in the status should be plausible for the loaded city

Then disable **Buildings Are Hard Obstacles** and repeat the same route.

Expected: building footprints become high soft costs rather than impossible cells. The route may choose demolition only when the alternative is sufficiently expensive. This is still planning-only; no building is actually removed.

Increase **Building Clearance** and verify the route stays farther away from structures.

## Existing roads and surface rail

Choose a corridor near an existing road and then near a train/tram line.

Expected:

- roads create a soft proximity cost rather than an absolute barrier
- train/tram centerlines create a stronger soft proximity cost
- crossings remain possible
- underground subway tracks are not treated as planar surface conflicts in v0.7
- increasing the road/rail influence distances should make the planner begin avoiding those corridors earlier

v0.7 deliberately does not invent interchange or rail grade-separation rules yet. These inputs influence corridor selection only.

## Water → bridge requirement

Choose endpoints on opposite sides of a river, lake edge, or other visible body of water.

Expected:

- points where CS2 reports surface-water depth above **Minimum Bridge Water Depth** are forced to `Bridge` in the construction evaluation
- the corresponding preview portions should appear magenta
- increasing the minimum depth threshold should reduce shallow-water bridge forcing

This is based on `WaterSystem` surface-depth data; it does not infer water from map textures or manually drawn zones.

## Live-world toggle

Disable **Use Live World Constraints** and regenerate the same corridor.

Expected:

- status says world constraints are off
- building, road/rail, and water inputs are omitted
- the result falls back to the v0.6 terrain-only planning behavior

This is a useful A/B check when diagnosing unexpected route choices.

## Cancel/reset behavior

Press the normal cancel/Escape action:

1. with a generated preview: removes the preview but keeps the start point
2. with only a start point: clears the start point
3. with no selection: exits the RealRoad Builder tool

The **Clear Preview** button in Options should also clear the current selection/preview.

## Design-standard checks

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

v0.7 contains no construction/apply pipeline. If any persistent network change occurs, treat it as a bug.

## Performance checks

Test at least one developed city with many buildings and network edges.

Expected:

- second-click planning may take noticeable time because planning is still synchronous, but should complete without a prolonged freeze under ordinary test corridors
- status reports plausible world snapshot counts
- route cost queries should not scale as a full-city scan per A* node; v0.7 spatially indexes captured buildings/roads/rails into 128 m cells

Record particularly slow cases. Moving the planner off the interaction frame remains a later milestone.

## Failure behavior

Try a deliberately difficult pair of endpoints, such as opposite sides of steep terrain or endpoints enclosed by protected building footprints.

Expected:

- no malformed road appears
- the preview tool stays responsive after the failure
- `PreviewStatus` reports a failure reason
- the game log contains a RealRoad Builder warning rather than an unhandled exception

## Logs

Use the normal Cities: Skylines II log location and search for `RealRoadBuilder` / `RealRoad Builder` messages. The useful entries are mod load, successful preview summary (including live-world counts), and caught planning failures.

## Still not part of v0.7

- exact interchange selection at existing-road crossings
- explicit railway over/under crossing design
- 3D subway clearance/conflict checking
- dedicated in-map React/UI panel
- true 3D bridge/tunnel ghost meshes
- dual-carriageway/median generation
- road/network construction

Those remain downstream of a stable, read-only planning preview.
