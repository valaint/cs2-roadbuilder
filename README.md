# RealRoad Builder for Cities: Skylines II

`cs2-roadbuilder` is a standards-driven road and highway auto-builder for Cities: Skylines II.

The long-term goal is to let a player choose start/end points, road class, design speed, and a construction philosophy, then generate an engineering-plausible alignment that respects terrain, horizontal curvature, transition geometry, grades, structures, and access rules.

## Project principles

- **Engineering model first** — alignment and standards logic stays independent from Cities: Skylines II game APIs.
- **Real-world design rules** — regional standards are data-driven and sourceable.
- **Preview before build** — generated alignments should be inspectable before creating game networks.
- **Non-destructive development** — early versions calculate and preview before they modify saves or roads.
- **No required GitHub Actions** — the repository should remain usable without consuming Actions minutes.

## Current v0.2 foundation

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
- preliminary alignment scoring based on route length and curvature severity
- terrain-independent seed generation: direct, left-bypass and right-bypass candidates
- xUnit tests covering standards, fillet geometry, clothoids, vertical curves, validation, and seed generation

### Important v0.2 limitations

This is **not yet a complete terrain-aware highway generator**.

- the seed corridor generator does not yet replace circular fillets with spiral-arc-spiral geometry automatically
- terrain sampling and vertical-profile optimization are not implemented yet
- earthwork, bridge, tunnel, demolition and obstacle costs are not implemented yet
- superelevation/runoff is represented only indirectly through minimum transition-section length; no cross-section roll model exists yet
- sight-distance validation is not implemented yet
- the CS2 mod does not create or alter game road networks yet

The current core can validate engineering geometry before a future game adapter converts it into CS2 network segments.

## Architecture

```text
Real-world standards
        |
        v
Engineering alignment model
        |
        v
Geometry / route solver
        |
        v
Validation + scoring
        |
        v
Cities: Skylines II adapter
        |
        v
Game road networks
```

The core solver is intentionally kept separate from the game adapter so its geometry can be tested without launching Cities: Skylines II.

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

- terrain sampling abstraction
- corridor-grid/A* search
- grade-aware search cost
- obstacle and demolition cost hooks
- automatic vertical-profile fitting
- candidate preview data

### Later

- dual-carriageway generation
- cut/fill estimation
- bridges, embankments, and tunnels
- ramps and interchanges
- superelevation and cross-section roll
- sight-distance validation
- additional regional standards
- conversion of a validated alignment into Cities: Skylines II road networks
