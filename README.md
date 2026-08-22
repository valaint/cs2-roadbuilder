# RealRoad Builder for Cities: Skylines II

`cs2-roadbuilder` is a standards-driven road and highway auto-builder for Cities: Skylines II.

The long-term goal is to let a player choose start/end points, road class, design speed, and a construction philosophy, then generate an engineering-plausible alignment that respects terrain, horizontal curvature, grades, structures, and access rules.

## Project principles

- **Engineering model first** — alignment and standards logic stays independent from Cities: Skylines II game APIs.
- **Real-world design rules** — regional standards are data-driven and sourceable.
- **Preview before build** — generated alignments should be inspectable before creating game networks.
- **Non-destructive development** — early versions calculate and preview before they modify saves or roads.
- **No required GitHub Actions** — the repository should remain usable without consuming Actions minutes.

## Initial roadmap

### v0.1 — Highway alignment foundation

- road-design standard model
- Japanese expressway horizontal-curve presets
- terrain-independent candidate alignment generation
- alignment validation and scoring
- unit tests for geometry and standards
- clean boundary for the future Cities: Skylines II adapter

### Later

- terrain-aware corridor search
- vertical-profile solver
- dual-carriageway generation
- bridges, embankments, and tunnels
- ramps and interchanges
- additional regional standards
- conversion of a validated alignment into Cities: Skylines II road networks

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

## Standards sources

The first standards catalog is based on Japan's Ministry of Land, Infrastructure, Transport and Tourism road-structure material:

- https://www.mlit.go.jp/road/road_e/r1_standard.html
- https://www.mlit.go.jp/road/road_e/r1_standard_2.html

Only values that are explicitly represented in the implementation should be treated as encoded design rules. More detailed vertical, cross-section, and interchange rules will be added incrementally with their sources.
