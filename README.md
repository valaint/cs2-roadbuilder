# RealRoad Builder for Cities: Skylines II

`cs2-roadbuilder` is a standards-driven road and highway auto-builder for Cities: Skylines II.

The long-term goal is to let a player choose start/end points, road class, design speed, and a construction philosophy, then generate an engineering-plausible alignment that respects terrain, horizontal curvature, transition geometry, grades, structures, and access rules.

## Project principles

- **Engineering model first** — alignment and standards logic stays independent from Cities: Skylines II game APIs.
- **Real-world design rules** — regional standards are data-driven and sourceable.
- **Preview before build** — generated alignments should be inspectable before creating game networks.
- **Non-destructive development** — early versions calculate and preview before they modify saves or roads.
- **No required GitHub Actions** — the repository should remain usable without consuming Actions minutes.

## Current v0.3 foundation

The current implementation contains:

- a game-independent `RealRoadBuilder.Core` project targeting .NET Standard 2.1
- a minimal `RealRoadBuilder.Mod` project using the Cities: Skylines II `CSII_TOOLPATH` mod toolchain
- 2D engineering geometry primitives
- tangent and true circular-arc alignment elements
- Euler spiral/clothoid transition elements with linearly changing curvature
- tangent → spiral → circular arc → spiral → tangent highway curve construction
- Japanese Type 1 expressway design rules for 60, 80, 100 and 120 km/h
- separate normal and exceptional minimum horizontal curve radii
- MLIT transition-section lengths, grade limits, crest/sag vertical radii, and vertical-curve lengths
- horizontal alignment validation for circular radius and transition length/radius
- station/elevation vertical-profile primitives
- constant-grade and parabolic vertical-curve elements
- vertical validation for grade limits, grade continuity, curve length, and crest/sag radius
- terrain sampling through a game-independent `ITerrainSampler` abstraction
- grade-aware 8-neighbor A* corridor search
- exact selected start/end points even though the internal search uses a grid
- heading-change penalties to discourage stair-step alignments
- hard exclusion areas through `ICorridorConstraintProvider.IsBlocked`
- soft land-use/demolition-style costs through `GetAdditionalCost`
- normal versus exceptional grade routing modes
- safe corridor simplification that preserves meaningful terrain-profile changes
- fine-grained terrain-profile sampling along a chosen corridor
- a preliminary vertical-profile optimizer that smooths terrain while projecting the design inside grade and approximate crest/sag-radius limits
- preliminary cut/fill depth metrics for later structure and earthwork decisions
- xUnit tests covering standards, horizontal geometry, clothoids, vertical geometry, terrain routing, cost avoidance, profile sampling, and preliminary profile optimization

### Important v0.3 limitations

This is **not yet a finished automatic highway builder**.

- the A* corridor is still a search polyline; it has not yet been automatically fitted into tangent/spiral/arc engineering geometry
- the preliminary vertical optimizer produces sampled design elevations, not the final explicit parabolic vertical curves required for construction
- minimum vertical-curve length is therefore enforced by the exact vertical validator only after final curve construction, not by the preliminary sampled optimizer
- cut/fill is currently reported as depth only; earthwork volume is not calculated
- bridges, embankments, tunnels, water crossings, and structure costs are not implemented yet
- demolition and land-use costs are provided through an abstraction; the CS2 adapter does not populate them from game entities yet
- superelevation/runoff has no cross-section roll model yet
- sight-distance validation is not implemented yet
- the CS2 mod does not create or alter game road networks yet

The current core is deliberately building enough engineering information to preview and reject bad routes before a future game adapter is allowed to construct anything.

## Architecture

```text
Real-world standards
        |
        v
Terrain + constraint adapters
        |
        v
Corridor A* search
        |
        v
Horizontal / vertical engineering fit
        |
        v
Validation + scoring + preview
        |
        v
Cities: Skylines II adapter
        |
        v
Game road networks
```

The core solver is intentionally kept separate from the game adapter so its geometry and routing can be tested without launching Cities: Skylines II.

## Repository layout

```text
src/
├── RealRoadBuilder.Core/
│   ├── Alignment/
│   ├── Design/
│   ├── Generation/
│   ├── Geometry/
│   ├── Scoring/
│   ├── Standards/
│   ├── Terrain/
│   ├── Validation/
│   └── Vertical/
└── RealRoadBuilder.Mod/
    ├── Mod.cs
    └── RealRoadBuilder.Mod.csproj

tests/
└── RealRoadBuilder.Core.Tests/
```

## Local development

### Core tests

Install a current .NET SDK with .NET 8 support or newer, then run:

```powershell
dotnet test tests/RealRoadBuilder.Core.Tests/RealRoadBuilder.Core.Tests.csproj
```

The test project currently uses `Microsoft.NET.Test.Sdk` 18.9.0 and `xunit.v3` 4.0.0.

### Cities: Skylines II mod project

Install the official Cities: Skylines II modding toolchain from the game first. The mod project imports the toolchain-provided `Mod.props` and `Mod.targets` from the user-level `CSII_TOOLPATH` environment variable.

Then build:

```powershell
dotnet build src/RealRoadBuilder.Mod/RealRoadBuilder.Mod.csproj -c Release
```

At this stage the in-game mod only loads and logs that the engineering core is available. Network construction is intentionally disabled until the alignment pipeline is safe enough to preview and validate first.

Official code-modding background:

- https://www.paradoxinteractive.com/games/cities-skylines-ii/modding/dev-diary-3-code-modding

## Standards sources

The first standards catalog is based on Japan's Ministry of Land, Infrastructure, Transport and Tourism Road Structure Ordinance material:

- https://www.mlit.go.jp/road/road_e/r1_standard.html
- https://www.mlit.go.jp/road/road_e/r1_standard_2.html
- https://www.mlit.go.jp/road/road_e/r1_standard_3.html

The initial Type 1 expressway rules encode values from:

- Article 15 — minimum and exceptional horizontal curve radius
- Article 18 — minimum transition-section length
- Article 20 — normal and exceptional maximum grade
- Article 22 — minimum crest/sag vertical-curve radius and minimum vertical-curve length

Only values explicitly represented in the implementation should be treated as encoded design rules. Cross-section, superelevation, sight-distance, earthwork, structure, and interchange rules will be added incrementally with their sources.

## Roadmap

### v0.1 — Highway alignment foundation

- [x] road-design standard model
- [x] Japanese expressway horizontal-curve presets
- [x] tangent/circular-arc geometry model
- [x] terrain-independent candidate alignment generation
- [x] alignment validation and scoring
- [x] geometry and standards tests
- [x] clean boundary for the Cities: Skylines II adapter

### v0.2 — Transition and vertical geometry

- [x] Euler spiral/clothoid transition element
- [x] spiral-arc-spiral curve builder
- [x] transition length/radius validation
- [x] vertical profile model
- [x] parabolic crest/sag vertical curves
- [x] grade and vertical-radius validation
- [x] MLIT Article 22 values

### v0.3 — Terrain-aware corridor solver

- [x] terrain sampling abstraction
- [x] corridor-grid/A* search
- [x] grade-aware search feasibility and cost
- [x] turn/heading penalty
- [x] hard obstacle hooks
- [x] soft demolition/land-use cost hooks
- [x] fine terrain-profile sampling
- [x] preliminary automatic vertical-profile fitting
- [x] candidate preview/profile data

### v0.4 — Engineering alignment fitting

- convert corridor corners into standards-valid tangent/spiral/arc/spiral/tangent sequences
- convert the preliminary vertical solution into explicit tangent/parabolic-curve elements
- validate the final fitted horizontal and vertical alignment together
- reject or re-route corridors that cannot be geometrically fitted inside the available space
- expose structured preview diagnostics for the future in-game UI

### Later

- dual-carriageway generation
- cut/fill volume estimation
- bridges, embankments, and tunnels
- ramps and interchanges
- superelevation and cross-section roll
- sight-distance validation
- additional regional standards
- conversion of a validated alignment into Cities: Skylines II road networks
