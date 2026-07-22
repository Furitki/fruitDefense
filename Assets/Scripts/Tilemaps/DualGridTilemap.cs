using System;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace FruitDefense.Tilemaps
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class DualGridTilemap : MonoBehaviour
    {
        [SerializeField] private Tilemap logicalTilemap;
        [SerializeField] private Tilemap generatedTilemap;
        [SerializeField] private DualGridTileSet tileSet;
        [SerializeField] private bool automaticRefresh = true;

        [NonSerialized] private bool signatureInitialized;
        [NonSerialized] private int lastSourceSignature;
        [NonSerialized] private readonly Vector3Int[] affectedVertices = new Vector3Int[4];

        public Tilemap LogicalTilemap { get { return logicalTilemap; } }
        public Tilemap GeneratedTilemap { get { return generatedTilemap; } }
        public DualGridTileSet TileSet { get { return tileSet; } }
        public bool AutomaticRefresh
        {
            get { return automaticRefresh; }
            set
            {
                automaticRefresh = value;
                signatureInitialized = false;
            }
        }

        public void Configure(Tilemap source, Tilemap output, DualGridTileSet set, bool refreshAutomatically = true)
        {
            logicalTilemap = source;
            generatedTilemap = output;
            tileSet = set;
            automaticRefresh = refreshAutomatically;
            signatureInitialized = false;
        }

        public bool ValidateConfiguration(out string reason)
        {
            if (logicalTilemap == null)
            {
                reason = "Logical Tilemap is required.";
                return false;
            }
            if (generatedTilemap == null)
            {
                reason = "Generated Tilemap is required.";
                return false;
            }
            if (logicalTilemap == generatedTilemap)
            {
                reason = "Logical and generated Tilemaps must be different; generated output is cleared on rebuild.";
                return false;
            }
            if (logicalTilemap.layoutGrid != generatedTilemap.layoutGrid)
            {
                reason = "Logical and generated Tilemaps must share one GridLayout.";
                return false;
            }
            if (logicalTilemap.transform.parent != generatedTilemap.transform.parent)
            {
                reason = "Logical and generated Tilemaps must be sibling objects for half-cell alignment.";
                return false;
            }
            if (tileSet == null)
            {
                reason = "Dual-Grid tile set is required.";
                return false;
            }
            return tileSet.Validate(out reason);
        }

        [ContextMenu("Align Generated Tilemap")]
        public bool AlignGeneratedTilemap()
        {
            if (logicalTilemap == null || generatedTilemap == null
                || logicalTilemap.layoutGrid != generatedTilemap.layoutGrid
                || logicalTilemap.transform.parent != generatedTilemap.transform.parent)
                return false;

            var grid = logicalTilemap.layoutGrid;
            var halfCell = grid.CellToLocalInterpolated(new Vector3(.5f, .5f, 0f))
                - grid.CellToLocalInterpolated(Vector3.zero);
            var outputTransform = generatedTilemap.transform;
            var sourceTransform = logicalTilemap.transform;
            var transformedHalfCell = sourceTransform.localRotation
                * Vector3.Scale(sourceTransform.localScale, halfCell);
            outputTransform.localPosition = sourceTransform.localPosition - transformedHalfCell;
            outputTransform.localRotation = sourceTransform.localRotation;
            outputTransform.localScale = sourceTransform.localScale;
            return true;
        }

        public bool HasExpectedAlignment(float tolerance = .0001f)
        {
            if (logicalTilemap == null || generatedTilemap == null
                || logicalTilemap.layoutGrid != generatedTilemap.layoutGrid
                || logicalTilemap.transform.parent != generatedTilemap.transform.parent)
                return false;

            var grid = logicalTilemap.layoutGrid;
            var halfCell = grid.CellToLocalInterpolated(new Vector3(.5f, .5f, 0f))
                - grid.CellToLocalInterpolated(Vector3.zero);
            var transformedHalfCell = logicalTilemap.transform.localRotation
                * Vector3.Scale(logicalTilemap.transform.localScale, halfCell);
            var expected = logicalTilemap.transform.localPosition - transformedHalfCell;
            return Vector3.Distance(generatedTilemap.transform.localPosition, expected) <= tolerance
                && Quaternion.Angle(generatedTilemap.transform.localRotation,
                    logicalTilemap.transform.localRotation) <= tolerance
                && Vector3.Distance(generatedTilemap.transform.localScale,
                    logicalTilemap.transform.localScale) <= tolerance;
        }

        [ContextMenu("Rebuild Generated Tilemap")]
        public void Rebuild()
        {
            string ignored;
            Rebuild(out ignored);
        }

        public bool Rebuild(out string reason)
        {
            if (!ValidateConfiguration(out reason)) return false;

            generatedTilemap.ClearAllTiles();
            BoundsInt occupiedBounds;
            if (!DualGridMaskUtility.TryGetOccupiedBounds(logicalTilemap, out occupiedBounds))
            {
                RememberCurrentSignature();
                reason = "ok";
                return true;
            }

            var vertexBounds = DualGridMaskUtility.ToVertexBounds(occupiedBounds);
            var outputTiles = new TileBase[vertexBounds.size.x * vertexBounds.size.y * vertexBounds.size.z];
            var index = 0;
            foreach (var vertex in vertexBounds.allPositionsWithin)
            {
                outputTiles[index] = tileSet.GetTile(DualGridMaskUtility.Resolve(logicalTilemap, vertex));
                index++;
            }
            generatedTilemap.SetTilesBlock(vertexBounds, outputTiles);
            generatedTilemap.CompressBounds();
            RememberCurrentSignature();
            reason = "ok";
            return true;
        }

        public bool SetLogicalTile(Vector3Int logicalCell, TileBase tile, out string reason)
        {
            if (!ValidateConfiguration(out reason)) return false;
            logicalTilemap.SetTile(logicalCell, tile);
            var refreshed = RefreshLogicalCell(logicalCell, out reason);
            if (refreshed) RememberCurrentSignature();
            return refreshed;
        }

        public bool RefreshLogicalCell(Vector3Int logicalCell, out string reason)
        {
            if (!ValidateConfiguration(out reason)) return false;
            DualGridMaskUtility.GetAffectedVertices(logicalCell, affectedVertices);
            for (var index = 0; index < affectedVertices.Length; index++)
                RefreshVertex(affectedVertices[index]);
            reason = "ok";
            return true;
        }

        public bool RefreshIfSourceChanged(out string reason)
        {
            if (!automaticRefresh)
            {
                reason = "Automatic refresh is disabled.";
                return false;
            }
            if (!ValidateConfiguration(out reason)) return false;

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
            if (logicalTilemap == null) return 0;
            unchecked
            {
                var hash = 17;
                var occupiedCount = 0;
                foreach (var position in logicalTilemap.cellBounds.allPositionsWithin)
                {
                    var tile = logicalTilemap.GetTile(position);
                    if (tile == null) continue;
                    occupiedCount++;
                    hash = hash * 31 + position.x;
                    hash = hash * 31 + position.y;
                    hash = hash * 31 + position.z;
                    hash = hash * 31 + tile.GetInstanceID();
                }
                return hash * 31 + occupiedCount;
            }
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

        private void RefreshVertex(Vector3Int vertex)
        {
            var mask = DualGridMaskUtility.Resolve(logicalTilemap, vertex);
            generatedTilemap.SetTile(vertex, tileSet.GetTile(mask));
        }

        private void RememberCurrentSignature()
        {
            lastSourceSignature = ComputeSourceSignature();
            signatureInitialized = true;
        }
    }
}
