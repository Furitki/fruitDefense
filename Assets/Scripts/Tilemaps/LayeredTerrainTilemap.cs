using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace FruitDefense.Tilemaps
{
    public enum LayeredTerrainMaterial
    {
        A,
        B,
    }

    public enum LayeredTerrainPaintMode
    {
        Base,
        Landform,
        Pair,
        Erase,
    }

    [Serializable]
    public sealed class LayeredTerrainContourBinding
    {
        [SerializeField] private string contourStyleId = string.Empty;
        [SerializeField] private DualGridTileSet landformTileSetA;
        [SerializeField] private DualGridTileSet landformTileSetB;
        [SerializeField] private DualGridTileSet edgeAOnBTileSet;
        [SerializeField] private DualGridTileSet edgeBOnATileSet;

        public string ContourStyleId { get { return contourStyleId ?? string.Empty; } }
        public DualGridTileSet LandformTileSetA { get { return landformTileSetA; } }
        public DualGridTileSet LandformTileSetB { get { return landformTileSetB; } }
        public DualGridTileSet EdgeAOnBTileSet { get { return edgeAOnBTileSet; } }
        public DualGridTileSet EdgeBOnATileSet { get { return edgeBOnATileSet; } }

        public bool TryResolveEdgeTileSet(LayeredTerrainMaterial landform,
            LayeredTerrainMaterial baseMaterial, out DualGridTileSet tileSet,
            out bool complementMask)
        {
            return LayeredTerrainTilemap.TryResolveEdgeTileSet(landform, baseMaterial,
                edgeAOnBTileSet, edgeBOnATileSet, out tileSet, out complementMask);
        }

        public LayeredTerrainContourBinding(string contourStyleId,
            DualGridTileSet landformTileSetA, DualGridTileSet landformTileSetB,
            DualGridTileSet edgeAOnBTileSet, DualGridTileSet edgeBOnATileSet)
        {
            this.contourStyleId = contourStyleId ?? string.Empty;
            this.landformTileSetA = landformTileSetA;
            this.landformTileSetB = landformTileSetB;
            this.edgeAOnBTileSet = edgeAOnBTileSet;
            this.edgeBOnATileSet = edgeBOnATileSet;
        }
    }

    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class LayeredTerrainTilemap : MonoBehaviour
    {
        [Header("Canonical authoring layers")]
        [SerializeField] private Tilemap baseLogicalTilemap;
        [SerializeField] private Tilemap landformLogicalTilemap;
        [SerializeField] private Tilemap edgeLogicalTilemap;
        [SerializeField] private TileBase logicalMaterialA;
        [SerializeField] private TileBase logicalMaterialB;
        [SerializeField] private TileBase logicalEdgeEnabled;

        [Header("Generated outputs - do not edit")]
        [SerializeField] private Tilemap baseOutputTilemap;
        [SerializeField] private Tilemap landformAOutputTilemap;
        [SerializeField] private Tilemap landformBOutputTilemap;
        [SerializeField] private Tilemap edgeAOnBOutputTilemap;
        [SerializeField] private Tilemap edgeBOnAOutputTilemap;

        [Header("Material A")]
        [SerializeField] private TileBase baseTileA;
        [SerializeField] private DualGridTileSet landformTileSetA;

        [Header("Material B")]
        [SerializeField] private TileBase baseTileB;
        [SerializeField] private DualGridTileSet landformTileSetB;

        [Header("Optional directed pair edges")]
        [SerializeField] private DualGridTileSet edgeAOnBTileSet;
        [SerializeField] private DualGridTileSet edgeBOnATileSet;
        [SerializeField] private string activeContourStyleId =
            FruitDefense.Core.BattlefieldLayerIds.ContourStyles.Organic;
        [SerializeField] private LayeredTerrainContourBinding[] contourBindings =
            Array.Empty<LayeredTerrainContourBinding>();
        [SerializeField] private bool automaticRefresh = true;

        [Header("Author-facing painter")]
        [SerializeField] private string materialADisplayName = string.Empty;
        [SerializeField] private Sprite materialAPreview;
        [SerializeField] private Color materialASwatch = new Color(0f, 0f, 0f, 0f);
        [SerializeField] private string materialBDisplayName = string.Empty;
        [SerializeField] private Sprite materialBPreview;
        [SerializeField] private Color materialBSwatch = new Color(0f, 0f, 0f, 0f);

        [NonSerialized] private int lastSourceSignature;
        [NonSerialized] private bool signatureInitialized;
        [NonSerialized] private readonly Vector3Int[] affectedVertices = new Vector3Int[4];

        public Tilemap BaseLogicalTilemap { get { return baseLogicalTilemap; } }
        public Tilemap LandformLogicalTilemap { get { return landformLogicalTilemap; } }
        public Tilemap EdgeLogicalTilemap { get { return edgeLogicalTilemap; } }
        public Tilemap BaseOutputTilemap { get { return baseOutputTilemap; } }
        public Tilemap LandformAOutputTilemap { get { return landformAOutputTilemap; } }
        public Tilemap LandformBOutputTilemap { get { return landformBOutputTilemap; } }
        public Tilemap EdgeAOnBOutputTilemap { get { return edgeAOnBOutputTilemap; } }
        public Tilemap EdgeBOnAOutputTilemap { get { return edgeBOnAOutputTilemap; } }
        public string ActiveContourStyleId
        {
            get { return activeContourStyleId ?? string.Empty; }
        }
        public IReadOnlyList<string> AvailableContourStyleIds
        {
            get { return ContourBindings().Select(value => value.ContourStyleId).ToArray(); }
        }
        public bool TryResolveEdgeTileSet(LayeredTerrainMaterial landform,
            LayeredTerrainMaterial baseMaterial, out DualGridTileSet tileSet,
            out bool complementMask)
        {
            return TryResolveEdgeTileSet(landform, baseMaterial, edgeAOnBTileSet,
                edgeBOnATileSet, out tileSet, out complementMask);
        }
        public bool AutomaticRefresh
        {
            get { return automaticRefresh; }
            set
            {
                automaticRefresh = value;
                signatureInitialized = false;
            }
        }

        public void ConfigureAuthoringPresentation(string displayNameA, Sprite previewA,
            Color swatchA, string displayNameB, Sprite previewB, Color swatchB)
        {
            materialADisplayName = displayNameA == null ? string.Empty : displayNameA.Trim();
            materialAPreview = previewA;
            materialASwatch = swatchA;
            materialBDisplayName = displayNameB == null ? string.Empty : displayNameB.Trim();
            materialBPreview = previewB;
            materialBSwatch = swatchB;
        }

#if UNITY_EDITOR
        public void ConfigureBaseVisuals(TileBase materialABase, TileBase materialBBase)
        {
            if (materialABase == null) throw new ArgumentNullException(nameof(materialABase));
            if (materialBBase == null) throw new ArgumentNullException(nameof(materialBBase));
            baseTileA = materialABase;
            baseTileB = materialBBase;
            signatureInitialized = false;
        }

        public bool TryConfigureLaboratoryBrush(string contourStyle,
            TileBase foregroundBase, TileBase backgroundBase,
            DualGridTileSet foregroundLandform, DualGridTileSet backgroundLandform,
            DualGridTileSet foregroundOnBackgroundEdge,
            string foregroundName, Sprite foregroundPreview, Color foregroundSwatch,
            string backgroundName, Sprite backgroundPreview, Color backgroundSwatch,
            out string reason)
        {
            if (foregroundBase == null || backgroundBase == null
                || foregroundLandform == null || foregroundOnBackgroundEdge == null)
            {
                reason = "注册笔刷缺少前景地貌、背景底图或组合边缘。";
                return false;
            }
            baseTileA = foregroundBase;
            baseTileB = backgroundBase;
            contourBindings = new[]
            {
                new LayeredTerrainContourBinding(contourStyle, foregroundLandform,
                    backgroundLandform, foregroundOnBackgroundEdge, null),
            };
            ApplyContourBinding(contourBindings[0]);
            ConfigureAuthoringPresentation(foregroundName, foregroundPreview,
                foregroundSwatch, backgroundName, backgroundPreview, backgroundSwatch);
            signatureInitialized = false;
            return Rebuild(out reason);
        }

        public bool MatchesLaboratoryBrush(string contourStyle,
            TileBase foregroundBase, TileBase backgroundBase,
            DualGridTileSet foregroundLandform, DualGridTileSet backgroundLandform,
            DualGridTileSet foregroundOnBackgroundEdge,
            string foregroundName, string backgroundName)
        {
            return string.Equals(ActiveContourStyleId, contourStyle,
                    StringComparison.Ordinal)
                && baseTileA == foregroundBase
                && baseTileB == backgroundBase
                && landformTileSetA == foregroundLandform
                && landformTileSetB == backgroundLandform
                && edgeAOnBTileSet == foregroundOnBackgroundEdge
                && edgeBOnATileSet == null
                && string.Equals(MaterialDisplayName(LayeredTerrainMaterial.A),
                    foregroundName, StringComparison.Ordinal)
                && string.Equals(MaterialDisplayName(LayeredTerrainMaterial.B),
                    backgroundName, StringComparison.Ordinal);
        }
#endif

        public bool HasAuthoredCells()
        {
            return HasAnyTile(baseLogicalTilemap) || HasAnyTile(landformLogicalTilemap)
                || HasAnyTile(edgeLogicalTilemap);
        }

        public bool ClearAuthoring(out string reason)
        {
            if (baseLogicalTilemap == null || landformLogicalTilemap == null
                || edgeLogicalTilemap == null)
            {
                reason = "Laboratory logical Tilemaps are not configured.";
                return false;
            }
            baseLogicalTilemap.ClearAllTiles();
            landformLogicalTilemap.ClearAllTiles();
            edgeLogicalTilemap.ClearAllTiles();
            return Rebuild(out reason);
        }

        public bool ValidateAuthoringPresentation(out string reason)
        {
            if (string.IsNullOrWhiteSpace(materialADisplayName)
                || string.IsNullOrWhiteSpace(materialBDisplayName))
            {
                reason = "Both terrain painter material display names are required.";
                return false;
            }
            if (string.Equals(materialADisplayName.Trim(), materialBDisplayName.Trim(),
                    StringComparison.OrdinalIgnoreCase))
            {
                reason = "Terrain painter material display names must be distinct.";
                return false;
            }
            if (!HasAuthoringPreview(materialAPreview, materialASwatch)
                || !HasAuthoringPreview(materialBPreview, materialBSwatch))
            {
                reason = "Each terrain painter material requires a thumbnail or visible swatch.";
                return false;
            }
            reason = "ok";
            return true;
        }

        public string MaterialDisplayName(LayeredTerrainMaterial material)
        {
            var value = material == LayeredTerrainMaterial.A
                ? materialADisplayName : materialBDisplayName;
            return string.IsNullOrWhiteSpace(value)
                ? (material == LayeredTerrainMaterial.A ? "Material A" : "Material B")
                : value.Trim();
        }

        public Sprite MaterialPreview(LayeredTerrainMaterial material)
        {
            return material == LayeredTerrainMaterial.A ? materialAPreview : materialBPreview;
        }

        public Color MaterialSwatch(LayeredTerrainMaterial material)
        {
            return material == LayeredTerrainMaterial.A ? materialASwatch : materialBSwatch;
        }

#if UNITY_EDITOR
        public bool TryGetBasePreviewSprite(LayeredTerrainMaterial material, out Sprite sprite)
        {
            var tile = BaseTile(material) as Tile;
            sprite = tile == null ? null : tile.sprite;
            return sprite != null && sprite.texture != null;
        }

        public bool TryGetRefinedPreviewSources(LayeredTerrainMaterial landform,
            LayeredTerrainMaterial baseMaterial, out Sprite baseSprite,
            out DualGridTileSet landformTileSet, out DualGridTileSet edgeTileSet,
            out string reason)
        {
            bool ignored;
            return TryGetRefinedPreviewSources(landform, baseMaterial, out baseSprite,
                out landformTileSet, out edgeTileSet, out ignored, out reason);
        }

        public bool TryGetRefinedPreviewSources(LayeredTerrainMaterial landform,
            LayeredTerrainMaterial baseMaterial, out Sprite baseSprite,
            out DualGridTileSet landformTileSet, out DualGridTileSet edgeTileSet,
            out bool complementEdgeMask, out string reason)
        {
            baseSprite = null;
            landformTileSet = null;
            edgeTileSet = null;
            complementEdgeMask = false;
            if (!CanPaintPair(landform, baseMaterial, true, out reason)) return false;
            if (!TryGetBasePreviewSprite(baseMaterial, out baseSprite))
            {
                reason = "The selected base material has no renderable preview Sprite.";
                return false;
            }
            landformTileSet = landform == LayeredTerrainMaterial.A
                ? landformTileSetA : landformTileSetB;
            TryResolveEdgeTileSet(landform, baseMaterial, out edgeTileSet,
                out complementEdgeMask);
            reason = "ok";
            return true;
        }
#endif

        public void Configure(Tilemap baseLogical, Tilemap landformLogical, Tilemap edgeLogical,
            Tilemap baseOutput, Tilemap landformAOutput, Tilemap landformBOutput,
            Tilemap edgeAOnBOutput, Tilemap edgeBOnAOutput,
            TileBase materialAMarker, TileBase materialBMarker, TileBase edgeEnabledMarker,
            TileBase materialABase, TileBase materialBBase,
            DualGridTileSet materialALandform, DualGridTileSet materialBLandform,
            DualGridTileSet aOnBEdge, DualGridTileSet bOnAEdge,
            bool refreshAutomatically = true)
        {
            baseLogicalTilemap = baseLogical;
            landformLogicalTilemap = landformLogical;
            edgeLogicalTilemap = edgeLogical;
            baseOutputTilemap = baseOutput;
            landformAOutputTilemap = landformAOutput;
            landformBOutputTilemap = landformBOutput;
            edgeAOnBOutputTilemap = edgeAOnBOutput;
            edgeBOnAOutputTilemap = edgeBOnAOutput;
            logicalMaterialA = materialAMarker;
            logicalMaterialB = materialBMarker;
            logicalEdgeEnabled = edgeEnabledMarker;
            baseTileA = materialABase;
            baseTileB = materialBBase;
            landformTileSetA = materialALandform;
            landformTileSetB = materialBLandform;
            edgeAOnBTileSet = aOnBEdge;
            edgeBOnATileSet = bOnAEdge;
            activeContourStyleId = FruitDefense.Core.BattlefieldLayerIds.ContourStyles.Organic;
            contourBindings = new[]
            {
                new LayeredTerrainContourBinding(activeContourStyleId,
                    materialALandform, materialBLandform, aOnBEdge, bOnAEdge),
            };
            automaticRefresh = refreshAutomatically;
            signatureInitialized = false;
        }

        public void ConfigureContourBindings(IEnumerable<LayeredTerrainContourBinding> bindings,
            string defaultContourStyleId)
        {
            contourBindings = (bindings ?? Array.Empty<LayeredTerrainContourBinding>())
                .Where(value => value != null).ToArray();
            var selected = contourBindings.FirstOrDefault(value => string.Equals(
                    value.ContourStyleId, defaultContourStyleId, StringComparison.Ordinal))
                ?? contourBindings.FirstOrDefault();
            if (selected != null) ApplyContourBinding(selected);
            signatureInitialized = false;
        }

        public bool CanSelectContourStyle(string contourStyleId, out string reason)
        {
            var binding = FindContourBinding(contourStyleId);
            if (binding == null)
            {
                reason = "Contour style '" + (contourStyleId ?? string.Empty)
                    + "' is not registered for this terrain laboratory.";
                return false;
            }
            if (binding.LandformTileSetA == null && binding.LandformTileSetB == null)
            {
                reason = "Contour style '" + binding.ContourStyleId
                    + "' does not provide a laboratory landform material.";
                return false;
            }
            BoundsInt bounds;
            if (!TryGetLogicalBounds(out bounds))
            {
                reason = "ok";
                return true;
            }
            foreach (var cell in bounds.allPositionsWithin)
            {
                LayeredTerrainMaterial foreground;
                LayeredTerrainMaterial background;
                if (!TryGetMaterial(landformLogicalTilemap, cell, out foreground)) continue;
                if ((foreground == LayeredTerrainMaterial.A
                        ? binding.LandformTileSetA : binding.LandformTileSetB) == null)
                {
                    reason = "The existing canvas uses a landform unavailable for contour '"
                        + binding.ContourStyleId + "'.";
                    return false;
                }
                if (!edgeLogicalTilemap.HasTile(cell)
                    || !TryGetMaterial(baseLogicalTilemap, cell, out background)) continue;
                DualGridTileSet edge;
                bool complementMask;
                if (binding.TryResolveEdgeTileSet(foreground, background, out edge,
                        out complementMask)) continue;
                reason = "The existing canvas requests a refined "
                    + (foreground == LayeredTerrainMaterial.A ? "A on B" : "B on A")
                    + " edge unavailable for contour '" + binding.ContourStyleId
                    + "'. Disable that refinement before switching the whole canvas.";
                return false;
            }
            reason = "ok";
            return true;
        }

        public bool TrySetContourStyle(string contourStyleId, out string reason)
        {
            if (string.Equals(ActiveContourStyleId, contourStyleId,
                    StringComparison.Ordinal))
            {
                reason = "ok";
                return true;
            }
            if (!CanSelectContourStyle(contourStyleId, out reason)) return false;
            var previous = FindContourBinding(ActiveContourStyleId);
            var next = FindContourBinding(contourStyleId);
            ApplyContourBinding(next);
            if (Rebuild(out reason)) return true;
            if (previous != null)
            {
                ApplyContourBinding(previous);
                string ignored;
                Rebuild(out ignored);
            }
            return false;
        }

        public bool ValidateConfiguration(out string reason)
        {
            return ValidateConfiguration(true, out reason);
        }

        public bool ValidateAuthoringConfiguration(out string reason)
        {
            return ValidateConfiguration(false, out reason);
        }

        private bool ValidateConfiguration(bool validateAuthoredContent, out string reason)
        {
            var logical = new[] { baseLogicalTilemap, landformLogicalTilemap, edgeLogicalTilemap };
            var outputs = new[]
            {
                baseOutputTilemap, landformAOutputTilemap, landformBOutputTilemap,
                edgeAOnBOutputTilemap, edgeBOnAOutputTilemap,
            };
            foreach (var tilemap in logical)
            {
                if (tilemap != null) continue;
                reason = "All three layered terrain logical Tilemaps are required.";
                return false;
            }
            foreach (var tilemap in outputs)
            {
                if (tilemap != null) continue;
                reason = "All five layered terrain output Tilemaps are required.";
                return false;
            }
            var all = new List<Tilemap>(logical);
            all.AddRange(outputs);
            var seen = new HashSet<Tilemap>();
            var reference = baseLogicalTilemap;
            foreach (var tilemap in all)
            {
                if (!seen.Add(tilemap))
                {
                    reason = "Logical and generated layered terrain Tilemaps must be distinct.";
                    return false;
                }
                if (tilemap.layoutGrid != reference.layoutGrid
                    || tilemap.transform.parent != reference.transform.parent)
                {
                    reason = "Layered terrain Tilemaps must be sibling objects under one Grid.";
                    return false;
                }
            }
            if (logicalMaterialA == null || logicalMaterialB == null || logicalEdgeEnabled == null)
            {
                reason = "Layered terrain logical marker tiles are required.";
                return false;
            }
            if (logicalMaterialA == logicalMaterialB)
            {
                reason = "Material A and B require distinct logical marker tiles.";
                return false;
            }
            if (baseTileA == null || baseTileB == null)
            {
                reason = "Material A and B base tiles are required.";
                return false;
            }
            var activeContour = FindContourBinding(ActiveContourStyleId);
            if (activeContour == null || (landformTileSetA == null && landformTileSetB == null))
            {
                reason = "The active terrain contour requires at least one exact landform asset.";
                return false;
            }
            if (landformTileSetA != null && !landformTileSetA.Validate(out reason)) return false;
            if (landformTileSetB != null && !landformTileSetB.Validate(out reason)) return false;
            if (edgeAOnBTileSet != null && !edgeAOnBTileSet.Validate(out reason)) return false;
            if (edgeBOnATileSet != null && !edgeBOnATileSet.Validate(out reason)) return false;
            DualGridTileSet resolvedEdge;
            bool complementMask;
            if (landformTileSetA != null
                && TryResolveEdgeTileSet(LayeredTerrainMaterial.A, LayeredTerrainMaterial.B,
                    out resolvedEdge, out complementMask)
                && !landformTileSetA.HasCompatibleNormalizedSockets(resolvedEdge,
                    out reason)) return false;
            if (landformTileSetB != null
                && TryResolveEdgeTileSet(LayeredTerrainMaterial.B, LayeredTerrainMaterial.A,
                    out resolvedEdge, out complementMask)
                && !landformTileSetB.HasCompatibleNormalizedSockets(resolvedEdge,
                    out reason)) return false;
            if (validateAuthoredContent && !ValidateUniformEdgeRegions(out reason)) return false;
            reason = "ok";
            return true;
        }

        public bool CanPaintPair(LayeredTerrainMaterial landform, LayeredTerrainMaterial baseMaterial,
            bool refinedEdge, out string reason)
        {
            if (landform == baseMaterial)
            {
                reason = "Foreground and background materials must be different.";
                return false;
            }
            var landformSet = landform == LayeredTerrainMaterial.A
                ? landformTileSetA : landformTileSetB;
            if (landformSet == null)
            {
                reason = "The active contour '" + ActiveContourStyleId + "' has no "
                    + (landform == LayeredTerrainMaterial.A ? "A" : "B")
                    + " landform asset; another contour is never substituted.";
                return false;
            }
            if (!refinedEdge)
            {
                reason = "ok";
                return true;
            }
            DualGridTileSet set;
            bool complementMask;
            if (!TryResolveEdgeTileSet(landform, baseMaterial, out set, out complementMask))
            {
                reason = "No refined edge is configured for the "
                    + (landform == LayeredTerrainMaterial.A ? "A on B" : "B on A")
                    + " pair in contour '" + ActiveContourStyleId + "'.";
                return false;
            }
            return set.Validate(out reason);
        }

        public bool PaintBase(Vector3Int cell, LayeredTerrainMaterial material, out string reason)
        {
            if (!ValidateAuthoringConfiguration(out reason)) return false;
            baseLogicalTilemap.SetTile(cell, Marker(material));
            landformLogicalTilemap.SetTile(cell, null);
            edgeLogicalTilemap.SetTile(cell, null);
            RefreshCell(cell);
            RememberCurrentSignature();
            reason = "ok";
            return true;
        }

        public bool PaintLandform(Vector3Int cell, LayeredTerrainMaterial material,
            bool refinedEdge, out string reason)
        {
            if (!ValidateAuthoringConfiguration(out reason)) return false;
            LayeredTerrainMaterial baseMaterial;
            if (!TryGetMaterial(baseLogicalTilemap, cell, out baseMaterial))
            {
                reason = "Landform painting requires an existing base cell.";
                return false;
            }
            if (!CanPaintPair(material, baseMaterial, refinedEdge, out reason)) return false;
            landformLogicalTilemap.SetTile(cell, Marker(material));
            ApplyUniformEdgeStyle(cell, material, baseMaterial, refinedEdge);
            RememberCurrentSignature();
            reason = "ok";
            return true;
        }

        public bool PaintPair(Vector3Int cell, LayeredTerrainMaterial landform,
            LayeredTerrainMaterial baseMaterial, bool refinedEdge, out string reason)
        {
            if (!ValidateAuthoringConfiguration(out reason)
                || !CanPaintPair(landform, baseMaterial, refinedEdge, out reason)) return false;
            baseLogicalTilemap.SetTile(cell, Marker(baseMaterial));
            landformLogicalTilemap.SetTile(cell, Marker(landform));
            ApplyUniformEdgeStyle(cell, landform, baseMaterial, refinedEdge);
            RememberCurrentSignature();
            reason = "ok";
            return true;
        }

        public bool EraseLandform(Vector3Int cell, out string reason)
        {
            if (!ValidateAuthoringConfiguration(out reason)) return false;
            landformLogicalTilemap.SetTile(cell, null);
            edgeLogicalTilemap.SetTile(cell, null);
            RefreshCell(cell);
            RememberCurrentSignature();
            reason = "ok";
            return true;
        }

        public bool EraseCell(Vector3Int cell, out string reason)
        {
            if (!ValidateAuthoringConfiguration(out reason)) return false;
            baseLogicalTilemap.SetTile(cell, null);
            landformLogicalTilemap.SetTile(cell, null);
            edgeLogicalTilemap.SetTile(cell, null);
            RefreshCell(cell);
            RememberCurrentSignature();
            reason = "ok";
            return true;
        }

        public bool Rebuild(out string reason)
        {
            if (!ValidateAuthoringConfiguration(out reason) || !AlignOutputs()) return false;
            ClearOutputs();
            BoundsInt bounds;
            if (!TryGetLogicalBounds(out bounds))
            {
                RememberCurrentSignature();
                reason = "ok";
                return true;
            }

            foreach (var cell in bounds.allPositionsWithin)
            {
                LayeredTerrainMaterial material;
                if (TryGetMaterial(baseLogicalTilemap, cell, out material))
                    baseOutputTilemap.SetTile(cell, BaseTile(material));
            }
            var vertices = DualGridMaskUtility.ToVertexBounds(bounds);
            foreach (var vertex in vertices.allPositionsWithin) RefreshVertex(vertex);
            CompressOutputs();
            RememberCurrentSignature();
            reason = "ok";
            return true;
        }

        [ContextMenu("Rebuild Layered Terrain")]
        private void RebuildFromMenu()
        {
            string ignored;
            Rebuild(out ignored);
        }

        public bool RefreshIfSourceChanged(out string reason)
        {
            if (!automaticRefresh)
            {
                reason = "Automatic refresh is disabled.";
                return false;
            }
            if (!ValidateAuthoringConfiguration(out reason)) return false;
            var signature = ComputeSourceSignature();
            if (signatureInitialized && signature == lastSourceSignature)
            {
                reason = "unchanged";
                return false;
            }
            return Rebuild(out reason);
        }

        public int ComputeSourceSignature()
        {
            unchecked
            {
                var hash = 17;
                AddTilemapSignature(ref hash, baseLogicalTilemap);
                AddTilemapSignature(ref hash, landformLogicalTilemap);
                AddTilemapSignature(ref hash, edgeLogicalTilemap);
                foreach (var character in ActiveContourStyleId)
                    hash = hash * 31 + character;
                return hash;
            }
        }

        public bool HasExpectedAlignment(float tolerance = .0001f)
        {
            if (baseLogicalTilemap == null) return false;
            var basePosition = baseLogicalTilemap.transform.localPosition;
            if (Vector3.Distance(baseOutputTilemap.transform.localPosition, basePosition) > tolerance)
                return false;
            var grid = baseLogicalTilemap.layoutGrid;
            var halfCell = grid.CellToLocalInterpolated(new Vector3(.5f, .5f, 0f))
                - grid.CellToLocalInterpolated(Vector3.zero);
            var transformedHalfCell = baseLogicalTilemap.transform.localRotation
                * Vector3.Scale(baseLogicalTilemap.transform.localScale, halfCell);
            var expected = basePosition - transformedHalfCell;
            return Vector3.Distance(landformAOutputTilemap.transform.localPosition, expected) <= tolerance
                && Vector3.Distance(landformBOutputTilemap.transform.localPosition, expected) <= tolerance
                && Vector3.Distance(edgeAOnBOutputTilemap.transform.localPosition, expected) <= tolerance
                && Vector3.Distance(edgeBOnAOutputTilemap.transform.localPosition, expected) <= tolerance;
        }

        private void RefreshCell(Vector3Int cell)
        {
            LayeredTerrainMaterial material;
            baseOutputTilemap.SetTile(cell, TryGetMaterial(baseLogicalTilemap, cell, out material)
                ? BaseTile(material) : null);
            DualGridMaskUtility.GetAffectedVertices(cell, affectedVertices);
            for (var index = 0; index < affectedVertices.Length; index++)
                RefreshVertex(affectedVertices[index]);
        }

        private void RefreshVertex(Vector3Int vertex)
        {
            SetResolvedTile(landformAOutputTilemap, landformTileSetA,
                ResolveMaterialMask(landformLogicalTilemap, vertex, LayeredTerrainMaterial.A),
                vertex, false);
            SetResolvedTile(landformBOutputTilemap, landformTileSetB,
                ResolveMaterialMask(landformLogicalTilemap, vertex, LayeredTerrainMaterial.B),
                vertex, false);
            RefreshEdgeVertex(edgeAOnBOutputTilemap, vertex, LayeredTerrainMaterial.A,
                LayeredTerrainMaterial.B);
            RefreshEdgeVertex(edgeBOnAOutputTilemap, vertex, LayeredTerrainMaterial.B,
                LayeredTerrainMaterial.A);
        }

        private void RefreshEdgeVertex(Tilemap output, Vector3Int vertex,
            LayeredTerrainMaterial landform, LayeredTerrainMaterial baseMaterial)
        {
            DualGridTileSet tileSet;
            bool complementMask;
            TryResolveEdgeTileSet(landform, baseMaterial, out tileSet, out complementMask);
            SetResolvedTile(output, tileSet, ResolveEdgeMask(vertex, landform, baseMaterial),
                vertex, complementMask);
        }

        private DualGridMask ResolveMaterialMask(Tilemap source, Vector3Int vertex,
            LayeredTerrainMaterial material)
        {
            return DualGridMaskUtility.Resolve(cell => IsMaterial(source, cell, material), vertex);
        }

        private DualGridMask ResolveEdgeMask(Vector3Int vertex, LayeredTerrainMaterial landform,
            LayeredTerrainMaterial baseMaterial)
        {
            return DualGridMaskUtility.Resolve(cell => edgeLogicalTilemap.HasTile(cell)
                && IsMaterial(landformLogicalTilemap, cell, landform)
                && IsMaterial(baseLogicalTilemap, cell, baseMaterial), vertex);
        }

        private bool ValidateUniformEdgeRegions(out string reason)
        {
            BoundsInt bounds;
            if (!TryGetLogicalBounds(out bounds))
            {
                reason = "ok";
                return true;
            }
            var visited = new HashSet<Vector3Int>();
            foreach (var cell in bounds.allPositionsWithin)
            {
                if (visited.Contains(cell)) continue;
                LayeredTerrainMaterial landform;
                LayeredTerrainMaterial baseMaterial;
                if (!TryGetMaterial(landformLogicalTilemap, cell, out landform)
                    || !TryGetMaterial(baseLogicalTilemap, cell, out baseMaterial)) continue;
                var region = ResolveExactPairRegion(cell, landform, baseMaterial);
                foreach (var member in region) visited.Add(member);
                var edgeEnabled = edgeLogicalTilemap.HasTile(cell);
                var mismatch = region.FirstOrDefault(member =>
                    edgeLogicalTilemap.HasTile(member) != edgeEnabled);
                if (region.Any(member => edgeLogicalTilemap.HasTile(member) != edgeEnabled))
                {
                    reason = "Connected " + landform + " on " + baseMaterial
                        + " cells must use one edge treatment; partial edge refinement was found at "
                        + mismatch + ".";
                    return false;
                }
            }
            reason = "ok";
            return true;
        }

        private void ApplyUniformEdgeStyle(Vector3Int seed, LayeredTerrainMaterial landform,
            LayeredTerrainMaterial baseMaterial, bool refinedEdge)
        {
            var region = ResolveExactPairRegion(seed, landform, baseMaterial);
            foreach (var cell in region)
                edgeLogicalTilemap.SetTile(cell, refinedEdge ? logicalEdgeEnabled : null);
            foreach (var cell in region) RefreshCell(cell);
        }

        private IReadOnlyCollection<Vector3Int> ResolveExactPairRegion(Vector3Int seed,
            LayeredTerrainMaterial landform, LayeredTerrainMaterial baseMaterial)
        {
            var resolved = new HashSet<Vector3Int> { seed };
            var queue = new Queue<Vector3Int>();
            queue.Enqueue(seed);
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                for (var offsetY = -1; offsetY <= 1; offsetY++)
                for (var offsetX = -1; offsetX <= 1; offsetX++)
                {
                    if (offsetX == 0 && offsetY == 0) continue;
                    var neighbor = current + new Vector3Int(offsetX, offsetY, 0);
                    if (resolved.Contains(neighbor)
                        || !IsMaterial(landformLogicalTilemap, neighbor, landform)
                        || !IsMaterial(baseLogicalTilemap, neighbor, baseMaterial)) continue;
                    resolved.Add(neighbor);
                    queue.Enqueue(neighbor);
                }
            }
            return resolved;
        }

        private static void SetResolvedTile(Tilemap output, DualGridTileSet tileSet,
            DualGridMask mask, Vector3Int vertex, bool complementMask)
        {
            DualGridMask tileMask;
            if (tileSet == null || !DualGridMaskUtility.TryResolveSharedEdgeMask(
                    mask, complementMask, out tileMask))
            {
                output.SetTile(vertex, null);
                return;
            }
            output.SetTile(vertex, tileSet.GetTile(tileMask));
        }

        private static bool HasAnyTile(Tilemap tilemap)
        {
            if (tilemap == null) return false;
            foreach (var cell in tilemap.cellBounds.allPositionsWithin)
                if (tilemap.HasTile(cell)) return true;
            return false;
        }

        internal static bool TryResolveEdgeTileSet(LayeredTerrainMaterial landform,
            LayeredTerrainMaterial baseMaterial, DualGridTileSet edgeAOnB,
            DualGridTileSet edgeBOnA, out DualGridTileSet tileSet,
            out bool complementMask)
        {
            tileSet = null;
            complementMask = false;
            if (landform == baseMaterial) return false;
            if (landform == LayeredTerrainMaterial.A
                && baseMaterial == LayeredTerrainMaterial.B)
            {
                if (edgeAOnB != null)
                {
                    tileSet = edgeAOnB;
                    return true;
                }
                Sprite reverseCenter;
                if (edgeBOnA == null
                    || !edgeBOnA.TryGetSprite(DualGridMask.Empty, out reverseCenter))
                    return false;
                tileSet = edgeBOnA;
                complementMask = true;
                return true;
            }
            if (landform == LayeredTerrainMaterial.B
                && baseMaterial == LayeredTerrainMaterial.A)
            {
                if (edgeBOnA != null)
                {
                    tileSet = edgeBOnA;
                    return true;
                }
                Sprite reverseCenter;
                if (edgeAOnB == null
                    || !edgeAOnB.TryGetSprite(DualGridMask.Empty, out reverseCenter))
                    return false;
                tileSet = edgeAOnB;
                complementMask = true;
                return true;
            }
            return false;
        }

        private IReadOnlyList<LayeredTerrainContourBinding> ContourBindings()
        {
            if (contourBindings != null && contourBindings.Length > 0) return contourBindings;
            return new[]
            {
                new LayeredTerrainContourBinding(
                    FruitDefense.Core.BattlefieldLayerIds.ContourStyles.Organic,
                    landformTileSetA, landformTileSetB, edgeAOnBTileSet, edgeBOnATileSet),
            };
        }

        private LayeredTerrainContourBinding FindContourBinding(string contourStyleId)
        {
            return ContourBindings().FirstOrDefault(value => value != null
                && string.Equals(value.ContourStyleId, contourStyleId,
                    StringComparison.Ordinal));
        }

        private void ApplyContourBinding(LayeredTerrainContourBinding binding)
        {
            if (binding == null) return;
            activeContourStyleId = binding.ContourStyleId;
            landformTileSetA = binding.LandformTileSetA;
            landformTileSetB = binding.LandformTileSetB;
            edgeAOnBTileSet = binding.EdgeAOnBTileSet;
            edgeBOnATileSet = binding.EdgeBOnATileSet;
            signatureInitialized = false;
        }

        private bool AlignOutputs()
        {
            if (baseLogicalTilemap == null || baseOutputTilemap == null) return false;
            CopyTransform(baseLogicalTilemap.transform, baseOutputTilemap.transform, Vector3.zero);
            CopyTransform(baseLogicalTilemap.transform, landformLogicalTilemap.transform, Vector3.zero);
            CopyTransform(baseLogicalTilemap.transform, edgeLogicalTilemap.transform, Vector3.zero);
            var grid = baseLogicalTilemap.layoutGrid;
            var halfCell = grid.CellToLocalInterpolated(new Vector3(.5f, .5f, 0f))
                - grid.CellToLocalInterpolated(Vector3.zero);
            var transformedHalfCell = baseLogicalTilemap.transform.localRotation
                * Vector3.Scale(baseLogicalTilemap.transform.localScale, halfCell);
            CopyTransform(baseLogicalTilemap.transform, landformAOutputTilemap.transform,
                -transformedHalfCell);
            CopyTransform(baseLogicalTilemap.transform, landformBOutputTilemap.transform,
                -transformedHalfCell);
            CopyTransform(baseLogicalTilemap.transform, edgeAOnBOutputTilemap.transform,
                -transformedHalfCell);
            CopyTransform(baseLogicalTilemap.transform, edgeBOnAOutputTilemap.transform,
                -transformedHalfCell);
            return true;
        }

        private static void CopyTransform(Transform source, Transform target, Vector3 positionOffset)
        {
            target.localPosition = source.localPosition + positionOffset;
            target.localRotation = source.localRotation;
            target.localScale = source.localScale;
        }

        private bool TryGetLogicalBounds(out BoundsInt bounds)
        {
            var found = false;
            var min = new Vector3Int(int.MaxValue, int.MaxValue, 0);
            var max = new Vector3Int(int.MinValue, int.MinValue, 0);
            foreach (var tilemap in new[] { baseLogicalTilemap, landformLogicalTilemap, edgeLogicalTilemap })
            foreach (var cell in tilemap.cellBounds.allPositionsWithin)
            {
                if (!tilemap.HasTile(cell)) continue;
                found = true;
                min = Vector3Int.Min(min, cell);
                max = Vector3Int.Max(max, cell);
            }
            bounds = found ? new BoundsInt(min, max - min + Vector3Int.one) : default(BoundsInt);
            return found;
        }

        private TileBase Marker(LayeredTerrainMaterial material)
        {
            return material == LayeredTerrainMaterial.A ? logicalMaterialA : logicalMaterialB;
        }

        private TileBase BaseTile(LayeredTerrainMaterial material)
        {
            return material == LayeredTerrainMaterial.A ? baseTileA : baseTileB;
        }

        private bool IsMaterial(Tilemap tilemap, Vector3Int cell, LayeredTerrainMaterial material)
        {
            return tilemap != null && tilemap.GetTile(cell) == Marker(material);
        }

        private bool TryGetMaterial(Tilemap tilemap, Vector3Int cell,
            out LayeredTerrainMaterial material)
        {
            if (IsMaterial(tilemap, cell, LayeredTerrainMaterial.A))
            {
                material = LayeredTerrainMaterial.A;
                return true;
            }
            if (IsMaterial(tilemap, cell, LayeredTerrainMaterial.B))
            {
                material = LayeredTerrainMaterial.B;
                return true;
            }
            material = LayeredTerrainMaterial.A;
            return false;
        }

        private void ClearOutputs()
        {
            foreach (var output in new[]
                     {
                         baseOutputTilemap, landformAOutputTilemap, landformBOutputTilemap,
                         edgeAOnBOutputTilemap, edgeBOnAOutputTilemap,
                     }) output.ClearAllTiles();
        }

        private void CompressOutputs()
        {
            foreach (var output in new[]
                     {
                         baseOutputTilemap, landformAOutputTilemap, landformBOutputTilemap,
                         edgeAOnBOutputTilemap, edgeBOnAOutputTilemap,
                     }) output.CompressBounds();
        }

        private static void AddTilemapSignature(ref int hash, Tilemap tilemap)
        {
            if (tilemap == null)
            {
                hash = hash * 31;
                return;
            }
            var count = 0;
            foreach (var cell in tilemap.cellBounds.allPositionsWithin)
            {
                var tile = tilemap.GetTile(cell);
                if (tile == null) continue;
                count++;
                hash = hash * 31 + cell.x;
                hash = hash * 31 + cell.y;
                hash = hash * 31 + cell.z;
                hash = hash * 31 + tile.GetInstanceID();
            }
            hash = hash * 31 + count;
        }

        private static bool HasAuthoringPreview(Sprite preview, Color swatch)
        {
            return preview != null || swatch.a > .01f;
        }

        private void RememberCurrentSignature()
        {
            lastSourceSignature = ComputeSourceSignature();
            signatureInitialized = true;
        }

        private void OnEnable()
        {
            signatureInitialized = false;
        }

        private void OnValidate()
        {
            signatureInitialized = false;
        }

        private void Update()
        {
            if (Application.isPlaying || !automaticRefresh) return;
            string ignored;
            RefreshIfSourceChanged(out ignored);
        }
    }
}
