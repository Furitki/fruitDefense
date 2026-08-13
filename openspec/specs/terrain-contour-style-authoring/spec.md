# terrain-contour-style-authoring Specification

## Purpose
Define contour-style identity, compatibility, square topology, source-art constraints, resolution independence, and acceptance evidence for terrain authoring.

## Requirements

### Requirement: Independent contour-style identity
Every authored landform SHALL identify a stable contour style independently from its semantic surface and optional edge treatment, and the initial supported identities SHALL distinguish square and organic contours.

#### Scenario: Grass uses two contour styles
- **WHEN** two disconnected grass landforms select square and organic contours
- **THEN** both remain semantically grass while presentation resolves two different contour TileSets

#### Scenario: Base-only cell is authored
- **WHEN** a visual cell has no landform
- **THEN** it carries no contour or edge style and renders only its required base

### Requirement: Compatible contour regions
A shared visual vertex SHALL observe at most one contour style among all landform-bearing cells, regardless of landform material, and authoring MUST reject a style change that would create an unsupported shared-edge or shared-vertex mixture without an explicit transition contract.

#### Scenario: Author alternates styles inside one region
- **WHEN** a square landform cell is painted directly beside a same-surface organic landform
- **THEN** the operation is refused or updates the complete affected component as one undoable action

#### Scenario: Disconnected styles coexist
- **WHEN** square and organic components do not share an edge or vertex
- **THEN** both styles may be saved, compiled, and rendered in one map

#### Scenario: Different materials meet with different contours
- **WHEN** square grass and organic stone-road would touch at one shared visual vertex
- **THEN** validation rejects the mixture until an explicit cross-style transition contract exists

### Requirement: Square contour footprint
The square contour TileSet SHALL use the existing sixteen corner masks but SHALL assemble one isolated logical landform cell as a rounded square contained by that logical cell instead of a diamond-like or octagonal patch.

#### Scenario: One square cell is rendered
- **WHEN** exactly one logical cell selects a square contour
- **THEN** its four affected visual tiles assemble a cell-aligned rounded square with no visible internal seam

#### Scenario: Square cells form a strip
- **WHEN** cardinally adjacent square cells form a rectangle or bent strip
- **THEN** their shared sides disappear and only the external straight and rounded turn boundaries remain

#### Scenario: Square diagonal cells are disconnected
- **WHEN** only opposite corners of a visual vertex contain square landforms
- **THEN** masks `5` and `10` keep two disconnected landform and grass-feather components with no grass or edge bridge through the center

### Requirement: High-resolution hand-painted transition source
The accepted square grass/soil sample SHALL use authored or image-generated source information at a native target of at least 128 pixels per visual tile, SHALL package a scale-aware top-down transition independent from source-side-view lighting, and MUST NOT reintroduce the legacy narrow dark contact ribbon.

#### Scenario: Painted square edge is enabled
- **WHEN** square grass over soil selects its registered painted edge
- **THEN** the assembled boundary contains a narrow varied grass encroachment and alpha feather over the existing soil base while preserving the square footprint without a dark contact line, exposed-soil skirt, outer shadow, or raised-platform reading

#### Scenario: Candidate art is packaged
- **WHEN** image-generated source art is converted into runtime sprites
- **THEN** deterministic processing may derive the retained lip profile, sample registered base-material textures, and lock boundary sockets but does not import directional source lighting or replace the approved square topology

### Requirement: Contour resolution independence
Opaque base sampling SHALL use an explicit cell-space UV basis independent from any contour TileSet, and the runtime SHALL permit contour TileSets with different native pixel sizes when each TileSet is internally consistent and paired landform/edge sockets are compatible in normalized tile space.

#### Scenario: Legacy organic and high-resolution square assets coexist
- **WHEN** an organic contour uses 32-pixel tiles and a square contour uses 256-pixel tiles in the same palette
- **THEN** validation accepts both, each renders at the same logical cell scale, and changing palette registration order does not change opaque base repetition

### Requirement: Contour acceptance board
The project SHALL produce machine-readable seam/topology evidence and visual boards for square, organic, and coexistence cases, and final acceptance SHALL inspect the result at real portrait Battle scale.

#### Scenario: Contour validation runs
- **WHEN** focused terrain validation executes
- **THEN** it covers isolated cells, strips, convex and concave turns, holes, masks `5/10`, exact compatible sockets, connected-style rejection, and disconnected coexistence

#### Scenario: Portrait acceptance runs
- **WHEN** the ordinary WebGL Battle is captured at the required portrait viewport
- **THEN** square gameplay-aligned landforms read as square tiles, the grass/soil join remains narrow and free of dark or secondary contours at 46 pixels per cell, organic samples remain available, and gameplay content stays readable above terrain
