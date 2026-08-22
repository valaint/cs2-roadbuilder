# RealRoad Builder for Cities: Skylines II

`cs2-roadbuilder` is a standards-driven road and highway auto-builder for Cities: Skylines II.

The long-term goal is to let a player choose start/end points, road class, design speed, and a construction philosophy, then generate an engineering-plausible alignment that respects terrain, horizontal curvature, transition geometry, grades, structures, and access rules.

## Project principles

- **Engineering model first** — standards, routing, and fitting stay independent from Cities: Skylines II game APIs.
- **Real-world design rules** — regional standards are data-driven and sourceable.
- **Preview before build** — generated alignments must be inspectable and rejectable before creating game networks.
- **Non-destructive development** — early versions calculate and validate before they modify saves or roads.
- **No required GitHub Actions** — the repository remains usable without consuming Actions minutes.

## Current v0.4 foundation

The current implementation contains:

- a game-independent `RealRoadBuilder.Core` project targeting .NET Standard 2.1
- a minimal `RealRoadBuilder.Mod` project using the Cities: Skylines II `CSII_TOOLPATH` mod toolchain
- Japanese Type 1 expressway design rules for 60, 80, 100 and 120 km/h
- separate normal and exceptional horizontal-radius and grade rules
- MLIT transition-section lengths, crest/sag vertical radii, and minimum vertical-curve lengths
- tangent, circular-arc, Euler spiral/clothoid, constant-grade, and parabolic vertical-curve primitives
- terrain sampling through a game-independent `ITerrainSampler`
- grade-aware 8-neighbor A* corridor search
- exact user-selected endpoints even though the internal corridor search uses a grid
- heading-change penalties to discourage stair-step alignments
- hard exclusion zones and soft land-use/demolition-style costs
- fine terrain-profile sampling along a selected corridor
- preliminary standards-aware vertical-profile smoothing and cut/fill depth output
- Douglas-Peucker-style reduction of rough corridor polylines into engineering PI/control points
- automatic horizontal fitting into tangent → clothoid → circular arc → clothoid → tangent sequences
- automatic radius increase for shallow bends when the required transition length would otherwise consume the whole deflection
- global adjacent-curve overlap checks across neighboring PIs
- sampled vertical-profile reduction into PVIs
- automatic parabolic crest/sag curve sizing from the applicable MLIT radius and minimum-length rules
- adjacent vertical-curve overlap checks
- exact horizontal and vertical standards validation after fitting
- combined horizontal/vertical fitting results with structured diagnostic code, severity, and message fields
- explicit failure instead of forcing geometry when the selected corridor cannot physically fit the standards

## Current pipeline

```text
Start / End + road standard
            |
            v
Terrain + constraint adapters
            |
            v
Grade-aware corridor A* search
            |
            v
Preliminary terrain/design profile
            |
            v
Corridor control-point reduction
            |
            +--------------------------+
            |                          |
            v                          v
Horizontal engineering fit      Vertical engineering fit
Tangent / clothoid / arc        Tangent / parabola
            |                          |
            +-------------+------------+
                          |
                          v
                Exact standards validation
                          |
                          v
                 Structured diagnostics
                          |
                          v
                  Future CS2 preview
                          |
                          v
                  Future network build
```

## Important v0.4 limitations

This is **not yet a finished automatic highway builder**.

- a failed engineering fit returns diagnostics, but the corridor router does not yet automatically re-search with feedback from the failed PI/PVI geometry
- the final fitted horizontal alignment has not yet been re-sampled against the CS2 terrain; the v0.3 terrain/cut-fill profile still follows the rough search corridor
- cut/fill is currently reported as depth only; earthwork volume is not calculated
- bridges, embankments, tunnels, water crossings, and structure costs are not implemented yet
- demolition and land-use costs are abstractions; the CS2 adapter does not populate them from game entities yet
- superelevation/runoff has no cross-section roll model yet
- sight-distance validation is not implemented yet
- dual carriageways, medians, ramps, and interchanges are not implemented yet
- the CS2 mod does not create or alter game road networks yet

The core now produces enough information to distinguish a feasible rough corridor from a final standards-valid engineering alignment. Construction remains disabled until the final alignment is checked against terrain and structure requirements.

## Repository layout

```text
src/
├── RealRoadBuilder.Core/
│   ├── Alignment/
│   ├── Design/
│   ├── Fitting/
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

### Cities: Skylines II mod project

Install the official Cities: Skylines II modding toolchain from the game first. The mod project imports `Mod.props` and `Mod.targets` through the user-level `CSII_TOOLPATH` environment variable.

Then build:

```powershell
dotnet build src/RealRoadBuilder.Mod/RealRoadBuilder.Mod.csproj -c Release
```

At v0.4 the in-game project still only loads the core. Network construction is deliberately disabled.

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
- [x] terrain-independent candidate generation
- [x] validation and scoring

### v0.2 — Transition and vertical geometry

- [x] Euler spiral/clothoid transition element
- [x] spiral-arc-spiral builder
- [x] transition validation
- [x] parabolic crest/sag vertical curves
- [x] grade and vertical-radius validation

### v0.3 — Terrain-aware corridor solver

- [x] terrain sampling abstraction
- [x] grade-aware corridor A* search
- [x] heading penalty
- [x] hard obstacle hooks
- [x] soft demolition/land-use costs
- [x] fine terrain-profile sampling
- [x] preliminary vertical-profile optimization

### v0.4 — Engineering alignment fitting

- [x] reduce rough corridor bends into engineering control points
- [x] fit standards-valid tangent/clothoid/arc/clothoid/tangent geometry
- [x] detect insufficient tangent space between neighboring curves
- [x] reduce sampled vertical profiles into PVIs
- [x] size and build explicit parabolic crest/sag curves
- [x] detect overlapping vertical curves
- [x] run exact horizontal + vertical validation
- [x] expose structured fit diagnostics

### v0.5 — Final terrain and structure evaluation

- re-sample terrain along the fitted horizontal alignment
- map the fitted vertical alignment onto final plan geometry
- estimate cut/fill volumes instead of depth only
- classify earthwork, bridge, embankment, and tunnel candidate segments
- add structure-aware route costs
- feed engineering-fit failures back into corridor re-search
- produce final preview geometry and metrics for the CS2 UI adapter

### Later

- CS2 terrain/entity adapters and in-game preview tool
- dual-carriageway and median generation
- actual validated CS2 network construction
- ramps and interchanges
- superelevation and cross-section roll
- sight-distance validation
- additional regional standards
