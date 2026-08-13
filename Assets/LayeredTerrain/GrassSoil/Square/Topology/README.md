# Square contour topology and imagegen contract

The runtime tile contract is one 4-by-4 atlas in row-major mask order `00..15`.
Each tile is 256 by 256 pixels. Corner bits remain NW=`1`, NE=`2`, SE=`4`, SW=`8`.

- `SquareContourTopologyGuide.png` is the machine topology authority: 1024 by 1024 RGBA.
- `SquareContourImagegenReference.png` is the labelled review/reference board only.
- `DualGridSwastikaReference.png` is the clean 1024-by-1024 swastika reference: each
  256-pixel frame maps the canonical NW=`1`, NE=`2`, SE=`4`, SW=`8` corner bits,
  in visual order `8,6,13,12 / 5,14,15,11 / 2,3,7,9 / 0,4,10,1`. Regenerate it
  with `scripts/generate-dual-grid-swastika-reference.ps1`.
- Green marks landform ownership and transparent pixels mark the base material.
- An isolated logical cell assembles from masks `04`, `08`, `01`, and `02` into a rounded
  square contained by that logical cell.
- Landform masks `05` and `10` must retain two disconnected grass components and a
  transparent landform center. Their optional pair-edge may join only the outside-soil paint
  across that center so diagonal grass remains separate without exposing a dark soil seam.

The retained imagegen ribbon at `../Sources/GrassOnSoilContinuousRibbon-v1.png` supplies only the
reviewed irregular grass-lip profile. Its side-view contact shadow, exposed soil wall, and lower
shadow are deliberately excluded from runtime paint. `../../Base/Grass.png` supplies exact RGB for
both the square grass landform and its narrow outside feather; `../../Base/Soil.png` remains visible
as the cell-aligned background. This keeps the join direction-neutral and top-down, without a
uniform dark outline, raised soil skirt, second outer contour, or texture-family switch.

Deterministic packaging maps the retained lip profile around the machine topology, keeps 8
base-grass pixels inside the contact, fades base-grass pixels across at most 7 pixels outside,
keeps diagonal grass components disconnected, removes detached paint, locks two-pixel compatible
sockets, enables mipmapped trilinear minification, crops the sixteen native sprites, and creates
the TileSet. It never imports directional source lighting or changes the square topology.

The approved hand-painted style reference is retained at
`../Sources/ApprovedStyleReference.png` (685 by 352 RGB), so validation never depends on the
original clipboard/Temp path. Both exact prompts, the rejected checkerboard result, the second
raw result, original `image_gen__imagegen` reference arguments, decisions, hashes, dimensions,
and normalization are retained beside it and indexed by
`../Sources/GrassOnSoilSquareCandidate.provenance.json`. Attempt 1 is rejected; attempt 2 is
accepted only as paint source, never as direct atlas topology.

The generated runtime paths are:

- `../LandformGrass/GrassSquareLandformTileSet.asset`
- `../LandformSoil/SoilSquareLandformTileSet.asset`
- `../LandformStoneRoad/StoneRoadSquareLandformTileSet.asset`
- `../EdgeGrassOnSoilPainted/GrassOnSoilSquarePaintedTileSet.asset`

Organic 32-pixel assets remain in their original folders and retain their own import size.
