# RealRoad Builder for Cities: Skylines II

`cs2-roadbuilder` is a standards-driven road and highway auto-builder for Cities: Skylines II.

The long-term goal is simple: choose start/end points, road class, design speed, and a planning philosophy, then generate an engineering-plausible road alignment that respects terrain, real-world geometry rules, live city obstacles, structures, and access constraints.

## Project principles

- **Engineering model first** — standards, routing, fitting, and evaluation stay independent from CS2 APIs.
- **Real-world geometry rules** — regional standards are sourceable and kept separate from game heuristics.
- **Preview before build** — generated alignments must be inspectable before any road entities are created.
- **Non-destructive development** — current in-game milestones do not modify saves or networks.
- **No required GitHub Actions** — local/toolchain builds remain the primary verification path.

## Current v0.7 foundation

The game-independent core supports the complete pre-construction planning loop:

- Japanese Type 1 expressway rules for 60, 80, 100 and 120 km/h
- normal and exceptional horizontal-radius and grade limits
- MLIT transition-section lengths and crest/sag vertical-curve requirements
- tangent, circular-arc, Euler spiral/clothoid, constant-grade, and parabolic vertical-curve geometry
- grade-aware A* corridor search with heading-change penalties
- hard obstacle and soft land-use cost hooks
- preliminary terrain/profile optimization
- automatic corridor reduction into engineering PI/control points
- automatic tangent → clothoid → circular arc → clothoid → tangent fitting
- automatic crest/sag vertical-curve fitting
- exact horizontal and vertical validation
- final fitted-alignment station sampling
- cut/fill approximation and at-grade/cut/embankment/bridge/tunnel classification
- relative construction scoring
- route → fit → evaluate → feedback → reroute iterations

The CS2 integration is still read-only, but now supplies real map inputs to that core:

- `Cs2TerrainSampler` backed by registered CPU terrain snapshots and `TerrainUtils.SampleHeight`
- transformed building prefab bounds used as exact-oriented planning footprints
- configurable hard building avoidance or high demolition-style soft cost
- existing road Bézier centerlines used as soft proximity costs
- train/tram Bézier centerlines used as stronger surface-rail proximity costs
- subway tracks intentionally excluded from planar conflicts until 3D clearance exists
- real `WaterSystem` surface depth used to force bridge classification over water
- a spatial index for buildings/roads/rails so A* does not scan the whole city at every node
- a two-click `RealRoadPreviewToolSystem` with color-coded construction overlay
- settings for design speed, search resolution, live-world behavior, and preview controls

## In-game preview workflow

1. Build/install the mod with the current official CS2 modding toolchain.
2. Load a map or city.
3. Open **Options → RealRoad Builder**.
4. Select design speed and planning settings.
5. Leave **Use Live World Constraints** enabled for normal v0.7 testing.
6. Press **Activate Preview Tool**.
7. Click once for the highway start point.
8. Click again, at least 100 m away, for the end point.
9. RealRoad Builder snapshots terrain/buildings/networks/water, runs the planner, and draws the best successful candidate.

Preview colors:

- **white** — at grade
- **yellow** — cut
- **green** — embankment
- **magenta** — bridge candidate / water-forced bridge
- **red** — tunnel candidate

The preview is projected onto the terrain surface so below-grade sections remain visible. It is planning information, not a final 3D road mesh.

See [`docs/CS2_PREVIEW_TESTING.md`](docs/CS2_PREVIEW_TESTING.md) for the current smoke-test checklist.

## Current pipeline

```text
CS2 start/end clicks + road standard
            |
            +---------------------------+
            |                           |
            v                           v
terrain snapshot               live-world snapshot
                               buildings / roads /
                               surface rail / water
            |                           |
            +-------------+-------------+
                          v
               grade-aware corridor A*
                          |
                          v
               preliminary vertical profile
                          |
                          v
               engineering alignment fitting
        (horizontal clothoids/arcs + vertical parabolas)
                          |
                          v
                 exact standards validation
                          |
                          v
        terrain / earthwork / structure evaluation
                          |
                          v
               relative construction score
                          |
                   expensive / failed?
                    /            \
                  yes            no
                   |              |
                   v              v
              feedback zones   best candidate
                   |              |
                   +-----> reroute
                                  |
                                  v
                         CS2 color overlay
```

## Live-world planning behavior

### Buildings

Building entities are captured from their CS2 transforms and prefab `ObjectGeometryData` bounds. The bounds are rotated with the building instance, giving an oriented planning footprint rather than an arbitrary radius.

By default, buildings plus a configurable clearance are hard obstacles. They can instead be treated as very expensive soft constraints for demolition-style route comparisons. The preview never demolishes anything.

### Existing roads and surface rail

Existing road and train/tram curves are captured from their real `Curve.m_Bezier` centerlines. They are currently **soft proximity costs**, not hard barriers, so crossings remain possible.

This is deliberately not yet an interchange or railway-grade-separation model. Underground subway edges are excluded because a 2D proximity test would create false surface conflicts.

### Water

A planning run snapshots `WaterSurfaceData`. `WaterUtils.SampleDepth` is used by the construction evaluator; samples above the configured water-depth threshold require `StructureRequirement.Bridge`.

No water texture guessing or hand-authored river polygons are used.

### Spatial indexing

Buildings and network curves are indexed into conservative 128 m spatial cells when the snapshot is created. A* point-cost/blocked checks therefore query nearby indexed features instead of scanning every captured feature in the city.

## Construction evaluation

The project separates standards-backed geometry from tunable planning heuristics.

### Standards-backed geometry

The initial Japanese expressway catalog encodes:

- minimum/exceptional horizontal curve radius
- minimum transition length
- normal/exceptional maximum grade
- crest/sag vertical-curve radius
- minimum vertical-curve length

### Tunable heuristics

`ConstructionEvaluationOptions` controls assumptions such as formation width, side slopes, bridge/tunnel thresholds, minimum structure run lengths, and relative structure/earthwork coefficients.

`TotalRelativeCost` is a **dimensionless candidate-comparison score**, not JPY/USD and not a construction tender estimate.

## Important v0.7 limitations

- planning still runs synchronously after the second click; live-city performance needs measurement
- building footprints use conservative geometry bounds, not exact mesh polygons
- road/rail influence is planar and does not yet understand existing elevated/underground separation
- subway conflicts are deferred until 3D clearance checking exists
- existing-road crossings do not yet select interchanges
- railway crossings do not yet choose over/under structure geometry
- water forces bridge classification but does not model hydrology, piers, navigation clearance, or flood design
- bridge/tunnel classification remains planning logic, not geotechnical design
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

```powershell
# Core tests
dotnet test tests/RealRoadBuilder.Core.Tests/RealRoadBuilder.Core.Tests.csproj

# CS2 mod build (requires the official toolchain / CSII_TOOLPATH)
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

### v0.1–v0.5 — engineering planning core

- [x] standards model and Japanese expressway presets
- [x] tangent/arc/clothoid geometry
- [x] parabolic vertical geometry
- [x] terrain-aware corridor A*
- [x] engineering alignment fitting and validation
- [x] earthwork/structure evaluation and iterative rerouting

### v0.6 — CS2 terrain preview

- [x] live terrain sampler
- [x] start/end selection tool
- [x] settings-based controls
- [x] color-coded read-only overlay
- [x] source-grounded terrain/tool/overlay API integration

### v0.7 — live-world planning inputs

- [x] transformed building footprint constraints
- [x] building hard/soft planning modes
- [x] existing-road proximity costs
- [x] train/tram proximity costs
- [x] water-depth forced bridge adapter
- [x] spatial indexing for live-world point queries
- [ ] dedicated in-map diagnostics panel
- [ ] live performance profiling
- [ ] **still no network mutation**

### Next

- exact crossing diagnostics and road/rail interaction classification
- dedicated in-map preview/diagnostics UI
- planning work scheduling/performance improvements
- dual carriageway and median generation model
- only after preview reliability: validated CS2 network construction through the native tool/apply pipeline
- bridge/tunnel prefab selection
- ramps and interchanges
- superelevation and cross-section roll
- sight-distance validation
- additional regional standards
