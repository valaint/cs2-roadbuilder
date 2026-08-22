# RealRoad Builder for Cities: Skylines II

`cs2-roadbuilder` is a standards-driven road and highway auto-builder for Cities: Skylines II.

The long-term goal is simple: choose start/end points, road class, design speed, and a planning philosophy, then generate an engineering-plausible road alignment that respects terrain, curvature, transition geometry, grades, structures, and access rules.

## Project principles

- **Engineering model first** — standards, routing, fitting, and evaluation stay independent from CS2 APIs.
- **Real-world geometry rules** — regional standards are sourceable and kept separate from game heuristics.
- **Preview before build** — generated alignments must be inspectable before any road entities are created.
- **Non-destructive development** — preview milestones do not modify saves or networks.
- **No required GitHub Actions** — local/toolchain builds remain the primary verification path.

## Current v0.6 foundation

The core now supports the full pre-construction planning loop:

- Japanese Type 1 expressway rules for 60, 80, 100 and 120 km/h
- normal and exceptional horizontal-radius and grade limits
- MLIT transition-section lengths and crest/sag vertical-curve requirements
- tangent, circular-arc, Euler spiral/clothoid, constant-grade, and parabolic vertical-curve geometry
- grade-aware A* corridor search
- heading-change penalties
- hard obstacle and soft land-use cost hooks
- preliminary terrain/profile optimization
- automatic corridor reduction into engineering PI/control points
- automatic tangent → clothoid → circular arc → clothoid → tangent fitting
- automatic crest/sag vertical-curve fitting
- exact horizontal and vertical validation
- final fitted-alignment station sampling
- cut/fill volume approximation
- at-grade, cut, embankment, bridge, and tunnel classification
- relative construction scoring
- route → fit → evaluate → feedback → reroute iterations

v0.6 adds the first direct Cities: Skylines II integration:

- `Cs2TerrainSampler` backed by `TerrainSystem.GetHeightData()` and `TerrainUtils.SampleHeight`
- a read-only `RealRoadPreviewToolSystem`
- terrain-only cursor raycasting
- two-click start/end selection
- in-game generation through the existing `HighwayPlanningEngine`
- terrain-surface ghost rendering through `OverlayRenderSystem.Buffer`
- color-coded construction sections
- mod settings for design speed, search cell size, iteration count, exceptional geometry, line width, activation, and clearing
- preview status text for success/failure diagnostics

## In-game preview workflow

1. Build/install the mod with the current official CS2 modding toolchain.
2. Load a map or city.
3. Open **Options → RealRoad Builder**.
4. Select the design speed and preview settings.
5. Press **Activate Preview Tool**.
6. Click once for the highway start point.
7. Click again, at least 100 m away, for the end point.
8. RealRoad Builder runs the planning pipeline and draws the best successful candidate.

Preview colors:

- **white** — at grade
- **yellow** — cut
- **green** — embankment
- **magenta** — bridge candidate
- **red** — tunnel candidate

The preview trace is projected onto the terrain surface so tunnel/cut sections remain visible. The real engineering vertical profile remains in the core result; v0.6 does not attempt to render final road meshes.

See [`docs/CS2_PREVIEW_TESTING.md`](docs/CS2_PREVIEW_TESTING.md) for the smoke-test checklist.

## Current pipeline

```text
CS2 start/end clicks + road standard
            |
            v
CS2 terrain adapter
            |
            v
Grade-aware corridor A* search
            |
            v
Preliminary vertical profile
            |
            v
Engineering alignment fitting
(horizontal clothoids/arcs + vertical parabolas)
            |
            v
Exact standards validation
            |
            v
Final terrain / earthwork / structure evaluation
            |
            v
Relative construction score
            |
     expensive / failed?
        /          \
      yes          no
       |            |
       v            v
feedback zones    best candidate
       |            |
       +-----> reroute
                    |
                    v
             CS2 color overlay
```

## Construction evaluation

The project deliberately separates standards-backed geometry from tunable planning heuristics.

### Standards-backed geometry

The initial Japanese expressway catalog encodes:

- minimum/exceptional horizontal curve radius
- minimum transition length
- normal/exceptional maximum grade
- crest/sag vertical-curve radius
- minimum vertical-curve length

### Tunable heuristics

`ConstructionEvaluationOptions` currently controls assumptions such as formation width, side slopes, bridge/tunnel thresholds, minimum structure run lengths, and relative structure/earthwork cost coefficients.

`TotalRelativeCost` is a **dimensionless candidate-comparison score**, not JPY/USD and not a construction tender estimate.

## Important v0.6 limitations

The preview path is intentionally conservative.

- the planning tool runs synchronously on the simulation/tool update when the second point is clicked; performance still needs in-game measurement
- live CS2 buildings, roads, railways, water, protected land, and demolition footprints are **not yet** adapted into the core constraint providers
- bridge/tunnel classification is planning logic, not geotechnical/hydrology design
- cut/fill volume still uses an approximate symmetric trapezoidal formation cross-section
- the default formation width is not yet derived from an encoded lane/shoulder/median standard
- the settings UI is the temporary control surface; there is no dedicated in-map panel yet
- the overlay is a centerline planning trace, not a true 3D ghost road mesh
- superelevation/runoff and sight-distance validation are not implemented yet
- dual carriageways, medians, ramps, and interchanges are not implemented yet
- the mod still does **not** create, upgrade, delete, or modify CS2 road/network entities

## Repository layout

```text
src/
├── RealRoadBuilder.Core/
│   ├── Alignment/
│   ├── Design/
│   ├── Evaluation/
│   ├── Fitting/
│   ├── Generation/
│   ├── Geometry/
│   ├── Planning/
│   ├── Scoring/
│   ├── Standards/
│   ├── Terrain/
│   ├── Validation/
│   └── Vertical/
└── RealRoadBuilder.Mod/
    ├── GameInterop/
    ├── Settings/
    ├── Tools/
    ├── Mod.cs
    └── RealRoadBuilder.Mod.csproj

docs/
└── CS2_PREVIEW_TESTING.md

tests/
└── RealRoadBuilder.Core.Tests/
```

## Local development

### Core tests

```powershell
dotnet test tests/RealRoadBuilder.Core.Tests/RealRoadBuilder.Core.Tests.csproj
```

### CS2 mod build

Install the official Cities: Skylines II modding toolchain from the game first. The project imports `Mod.props` and `Mod.targets` through the user-level `CSII_TOOLPATH` environment variable.

```powershell
dotnet build src/RealRoadBuilder.Mod/RealRoadBuilder.Mod.csproj -c Release
```

## Standards sources

Initial Japanese rules are sourced from MLIT Road Structure Ordinance material:

- https://www.mlit.go.jp/road/road_e/r1_standard.html
- https://www.mlit.go.jp/road/road_e/r1_standard_2.html
- https://www.mlit.go.jp/road/road_e/r1_standard_3.html

Encoded articles currently include:

- Article 15 — horizontal curve radius
- Article 18 — transition-section length
- Article 20 — maximum grade
- Article 22 — crest/sag vertical-curve radius and minimum length

Only values explicitly represented in the standards catalog should be treated as encoded engineering rules.

## Roadmap

### v0.1 — Highway alignment foundation

- [x] standards model
- [x] Japanese expressway curve presets
- [x] tangent/circular-arc geometry
- [x] validation and scoring

### v0.2 — Transition and vertical geometry

- [x] Euler spiral/clothoids
- [x] spiral-arc-spiral builder
- [x] parabolic crest/sag curves
- [x] vertical/horizontal validation

### v0.3 — Terrain-aware corridor search

- [x] terrain abstraction
- [x] grade-aware A*
- [x] heading penalty
- [x] obstacle/cost hooks
- [x] preliminary vertical-profile optimization

### v0.4 — Engineering alignment fitting

- [x] corridor-to-PI reduction
- [x] standards-valid horizontal fitting
- [x] standards-valid vertical fitting
- [x] overlap checks
- [x] structured diagnostics

### v0.5 — Final terrain, structures, and rerouting

- [x] final fitted-alignment terrain sampling
- [x] cut/fill volume estimates
- [x] at-grade/cut/embankment/bridge/tunnel classification
- [x] relative construction scoring
- [x] structure/fit feedback zones
- [x] iterative rerouting engine

### v0.6 — CS2 preview integration

- [x] CS2 terrain sampler adapter
- [x] terrain-only start/end selection tool
- [x] settings-based design-speed controls
- [x] ghost centerline rendering
- [x] cut/fill/bridge/tunnel color overlay
- [x] preview status / failure text
- [ ] live building footprint constraints
- [ ] existing road/rail corridor costs
- [ ] water crossing / forced bridge adapter
- [ ] dedicated in-map UI panel
- [ ] performance profiling on a live city
- [ ] **still no network mutation until preview is verified**

### Later

- dual carriageway and median generation
- validated CS2 network construction through the native tool/apply pipeline
- bridge/tunnel prefab selection
- ramps and interchanges
- superelevation and cross-section roll
- sight-distance validation
- additional regional standards
