# RealRoad Builder for Cities: Skylines II

`cs2-roadbuilder` is a standards-driven road and highway auto-builder for Cities: Skylines II.

The long-term goal is to let a player choose start/end points, road class, design speed, and a construction philosophy, then generate an engineering-plausible alignment that respects terrain, horizontal curvature, transition geometry, grades, structures, and access rules.

## Project principles

- **Engineering model first** — standards, routing, fitting, and construction evaluation stay independent from Cities: Skylines II game APIs.
- **Real-world geometry rules** — regional road standards are sourceable and kept separate from game heuristics.
- **Preview before build** — generated alignments must be inspectable and rejectable before creating game networks.
- **Non-destructive development** — the core calculates and validates before the mod is allowed to mutate roads or saves.
- **No required GitHub Actions** — the repository remains usable without consuming Actions minutes.

## Current v0.5 foundation

The core now contains the full pre-construction planning loop:

- Japanese Type 1 expressway design rules for 60, 80, 100 and 120 km/h
- normal and exceptional horizontal-radius and grade rules
- MLIT transition-section lengths, crest/sag vertical radii, and minimum vertical-curve lengths
- tangent, circular-arc, Euler spiral/clothoid, constant-grade, and parabolic vertical-curve primitives
- grade-aware 8-neighbor A* corridor search
- exact user-selected endpoints
- heading-change penalties to discourage stair-step alignments
- hard obstacle and soft land-use/demolition cost hooks
- preliminary terrain/profile optimization
- automatic corridor reduction into engineering PI/control points
- automatic tangent → clothoid → circular arc → clothoid → tangent fitting
- automatic parabolic crest/sag vertical-curve fitting
- adjacent horizontal/vertical curve overlap detection
- exact horizontal and vertical standards validation
- exact station sampling along the **fitted** horizontal geometry, including tangents, clothoids, and circular arcs
- exact vertical-profile evaluation at the same final stations
- terrain/design elevation and signed cut/fill samples
- preliminary trapezoidal cut/fill volume estimation
- at-grade, cut, embankment, bridge, and tunnel candidate classification
- optional forced bridge/tunnel requirements for future water/rail/protected-corridor adapters
- tunable dimensionless construction-cost comparison
- bridge/tunnel and failed-fit feedback zones for corridor re-search
- an iterative `HighwayPlanningEngine` that can run route → profile → fit → evaluate → feedback → reroute and retain the best successful candidate

## Current pipeline

```text
Start / End + road standard
            |
            v
Terrain + obstacle / land-use adapters
            |
            v
Grade-aware corridor A* search
            |
            v
Preliminary terrain/design profile
            |
            v
Engineering alignment fitting
(horizontal clothoids/arcs + vertical parabolas)
            |
            v
Exact standards validation
            |
            v
Sample FINAL fitted alignment against terrain
            |
            v
Cut / fill volume approximation
            |
            v
At-grade / cut / embankment / bridge / tunnel classification
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
       |
       +-----> corridor A* re-search
```

## Construction evaluation

v0.5 deliberately separates two kinds of rules:

### Standards-backed geometry

The horizontal and vertical alignment rules come from the encoded road standard. For the initial Japanese expressway catalog these include curve radius, transition length, maximum grade, vertical-curve radius, and minimum vertical-curve length.

### Tunable planning heuristics

Structure selection and cost comparison are **not claimed to be real construction estimates** yet.

`ConstructionEvaluationOptions` exposes:

- final alignment sample interval
- assumed formation width
- assumed cut/fill side-slope ratio
- at-grade tolerance
- deep-fill threshold for bridge candidacy
- deep-cut threshold for tunnel candidacy
- minimum bridge/tunnel run length
- relative surface cost per metre
- relative earthwork cost per cubic metre
- relative bridge cost per metre
- relative tunnel cost per metre

The resulting `TotalRelativeCost` is a dimensionless candidate-comparison score, not JPY/USD or an engineering tender estimate.

## Reroute feedback

`RerouteFeedbackConstraintProvider` implements the existing corridor constraint interface, so evaluated construction problems can be fed directly back into A*.

By default:

- bridge candidates add a soft avoidance penalty
- tunnel candidates add a larger soft avoidance penalty
- failed horizontal-fit areas add a strong soft penalty
- feedback uses radial falloff rather than a binary exclusion
- prior obstacle/demolition providers can be composed with the feedback provider

Hard blocking of fit-failure zones is available but disabled by default. This lets the router reuse an expensive area when every alternative is worse.

## Important v0.5 limitations

This is still **not a finished in-game automatic highway builder**.

- bridge/tunnel selection is based on alignment-versus-terrain depth plus optional external requirements; there is no geotechnical, geology, hydrology, pier, portal, or foundation model
- cut/fill volume uses an approximate symmetric trapezoidal formation cross-section
- the default formation width is a planning parameter, not yet generated from an actual lane/shoulder/median cross-section standard
- structure cost coefficients are relative heuristics, not currency estimates
- water, rail, buildings, existing road networks, protected land, and demolition data are not yet populated from CS2 entities
- final structure segments are not yet converted into CS2 bridge/tunnel network prefab choices
- superelevation/runoff has no cross-section roll model yet
- sight-distance validation is not implemented yet
- dual carriageways, medians, ramps, and interchanges are not implemented yet
- the CS2 mod still does **not** create or alter game road networks

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

At v0.5 the in-game project still only loads the core. Network construction remains deliberately disabled.

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

Only values explicitly represented in the standards catalog should be treated as encoded design rules. Cross-section, superelevation, sight-distance, geotechnical, structure, and interchange rules will be added incrementally with their sources.

## Roadmap

### v0.1 — Highway alignment foundation

- [x] road-design standard model
- [x] Japanese expressway horizontal-curve presets
- [x] tangent/circular-arc geometry
- [x] terrain-independent candidate generation
- [x] validation and scoring

### v0.2 — Transition and vertical geometry

- [x] Euler spiral/clothoid transitions
- [x] spiral-arc-spiral builder
- [x] transition validation
- [x] parabolic crest/sag curves
- [x] grade and vertical-radius validation

### v0.3 — Terrain-aware corridor solver

- [x] terrain sampling abstraction
- [x] grade-aware A* search
- [x] heading penalty
- [x] hard obstacle hooks
- [x] soft land-use costs
- [x] preliminary vertical-profile optimization

### v0.4 — Engineering alignment fitting

- [x] corridor-to-PI reduction
- [x] standards-valid horizontal fitting
- [x] standards-valid vertical fitting
- [x] horizontal/vertical overlap checks
- [x] exact validation
- [x] structured diagnostics

### v0.5 — Final terrain, structures, and reroute feedback

- [x] station sampling on the final fitted horizontal alignment
- [x] map final vertical profile onto final plan geometry
- [x] estimate preliminary cut/fill volumes
- [x] classify at-grade/cut/embankment/bridge/tunnel candidates
- [x] expose forced structure requirement adapter
- [x] add relative construction-cost scoring
- [x] generate bridge/tunnel and fit-failure reroute feedback
- [x] compose feedback with the existing A* constraint provider
- [x] add iterative route → fit → evaluate → reroute planning engine
- [x] expose final preview-ready samples and metrics

### v0.6 — CS2 preview integration

- CS2 terrain sampler adapter
- CS2 building/road/rail/water constraint adapters
- in-game start/end selection tool
- standard/design-speed/settings panel
- ghost alignment rendering
- cut/fill/bridge/tunnel colour overlay
- diagnostics and candidate comparison panel
- **still no network mutation until preview is reliable**

### Later

- dual-carriageway and median generation
- validated CS2 road/network construction
- bridge/tunnel prefab selection
- ramps and interchanges
- superelevation and cross-section roll
- sight-distance validation
- additional regional standards
