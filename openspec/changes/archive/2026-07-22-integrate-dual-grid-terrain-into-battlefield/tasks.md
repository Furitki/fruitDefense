## 1. Runtime terrain presentation

- [x] 1.1 Extend the runtime Dual-Grid utility to validate both TileSets and resolve independent top-down plantable-grass and route-stone masks on shared projection vertices
- [x] 1.2 Bind grass, road, and soil assets on `FruitDefenseGame`; draw soil → grass → stone road; retain route markers and clear plantable/expansion affordances

## 2. Release scene integration

- [x] 2.1 Wire PixelGrass, StoneFloor, and PixelGrass soil assets into `Assets/Scenes/Battle.unity`
- [x] 2.2 Update `ProjectSetup.Configure` so recreated Battle scenes receive the same three explicit runtime bindings

## 3. Automated validation

- [x] 3.1 Extend editor smoke coverage for both required Sprite sets, three release-scene bindings, exact grass/road role masks, visual counts/bounds, feedback containment, and all bundled maps
- [x] 3.2 Run strict OpenSpec validation and the required Unity project smoke validation

## 4. Runtime acceptance

- [x] 4.1 Build the ordinary WebGL release artifact and run portrait acceptance without changing release scene order
- [x] 4.2 Capture and inspect all three bundled maps to verify grass placement areas, stone monster routes, layer transitions, entity readability, projection alignment, safe-area containment, and controls
- [x] 4.3 Clamp transient battlefield feedback inside the projected grid after visual QA exposed left-edge overflow on `orchard-02`, then rebuild and re-accept that flow
