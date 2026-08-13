using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FruitDefense.Editor
{
    public static class SquareTerrainEvidence
    {
        private static readonly Color32 Frame = new Color32(27, 35, 42, 255);

        public static void RenderReviewEvidence()
        {
            var report = SquareTerrainArtValidator.ValidateGeneratedAssetsInternal(true);
            var square = ReadFamily(SquareTerrainArtProfile.GrassLandformFolder,
                SquareTerrainArtProfile.TileSize);
            var edge = ReadFamily(SquareTerrainArtProfile.GrassOnSoilEdgeFolder,
                SquareTerrainArtProfile.TileSize);
            var organic = ReadFamily(SquareTerrainArtProfile.OrganicGrassFolder, 32);
            var soilTexture = LoadPng(SquareTerrainArtProfile.SoilBaseSourcePath);
            Color32[] soil;
            var soilSize = soilTexture.width;
            try
            {
                if (soilTexture.width != soilTexture.height)
                    throw new InvalidOperationException("Evidence base-soil texture must be square.");
                soil = soilTexture.GetPixels32();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(soilTexture);
            }
            var pattern = CreateAcceptancePattern();

            var squareBoard = RenderPattern(pattern, square, edge, soil, soilSize, 72);
            var organicBoard = RenderPattern(pattern, organic, null, soil, soilSize, 72);
            WritePixels(SquareTerrainArtProfile.SquareBoardEvidencePath,
                squareBoard.Width, squareBoard.Height, squareBoard.Pixels);
            WritePixels(SquareTerrainArtProfile.OrganicBoardEvidencePath,
                organicBoard.Width, organicBoard.Height, organicBoard.Pixels);

            var coexistence = JoinHorizontal(squareBoard, organicBoard, 24);
            WritePixels(SquareTerrainArtProfile.CoexistenceBoardEvidencePath,
                coexistence.Width, coexistence.Height, coexistence.Pixels);

            var battlePattern = CreateBattlePattern();
            var battleTerrain = RenderPattern(battlePattern, square, edge, soil, soilSize, 46);
            var battle = PlaceOnPortraitCanvas(battleTerrain, 402, 500);
            WritePixels(SquareTerrainArtProfile.BattleScaleBoardEvidencePath,
                battle.Width, battle.Height, battle.Pixels);
            WriteValidationReport(report);
            Debug.Log("Square, organic, coexistence and real Battle-scale boards rendered to "
                + SquareTerrainArtProfile.EvidenceFolder + ".");
        }

        internal static void WriteValidationReport(
            SquareTerrainArtValidator.ValidationReport report)
        {
            var absolute = SquareTerrainArtGenerator.AbsolutePath(
                SquareTerrainArtProfile.ValidationEvidencePath);
            Directory.CreateDirectory(Path.GetDirectoryName(absolute));
            File.WriteAllText(absolute, JsonUtility.ToJson(report, true));
        }

        private static bool[,] CreateAcceptancePattern()
        {
            var cells = new bool[12, 8];
            cells[1, 6] = true; // isolated rounded square
            for (var x = 4; x <= 8; x++) cells[x, 6] = true; // strip
            cells[1, 3] = true;
            cells[2, 3] = true;
            cells[3, 3] = true;
            cells[3, 4] = true;
            cells[3, 5] = true; // bent strip / convex turn
            for (var y = 1; y <= 4; y++)
            for (var x = 5; x <= 8; x++)
                cells[x, y] = x == 5 || x == 8 || y == 1 || y == 4; // hole
            cells[10, 1] = true;
            cells[11, 2] = true;
            cells[10, 4] = true;
            cells[11, 3] = true; // both diagonal orientations
            return cells;
        }

        private static bool[,] CreateBattlePattern()
        {
            var cells = new bool[7, 8];
            for (var y = 0; y < cells.GetLength(1); y++)
            for (var x = 0; x < cells.GetLength(0); x++)
                cells[x, y] = x < 3 || (x == 3 && (y == 1 || y == 2 || y == 5));
            cells[1, 2] = false;
            cells[1, 3] = false;
            return cells;
        }

        private static ImageBuffer RenderPattern(bool[,] cells, Color32[][] landform,
            Color32[][] edge, Color32[] baseTexture, int baseTextureSize, int displayTileSize)
        {
            var cellWidth = cells.GetLength(0);
            var cellHeight = cells.GetLength(1);
            var width = (cellWidth + 1) * displayTileSize;
            var height = (cellHeight + 1) * displayTileSize;
            var pixels = new Color32[width * height];
            FillTiledBase(pixels, width, height, baseTexture, baseTextureSize, displayTileSize);

            for (var vertexY = 0; vertexY <= cellHeight; vertexY++)
            for (var vertexX = 0; vertexX <= cellWidth; vertexX++)
            {
                var mask = 0;
                if (Occupied(cells, vertexX - 1, vertexY)) mask |= 1;
                if (Occupied(cells, vertexX, vertexY)) mask |= 2;
                if (Occupied(cells, vertexX, vertexY - 1)) mask |= 4;
                if (Occupied(cells, vertexX - 1, vertexY - 1)) mask |= 8;
                BlitScaled(pixels, width, height, landform[mask],
                    SourceSize(landform[mask]), vertexX * displayTileSize,
                    vertexY * displayTileSize, displayTileSize);
                if (edge != null)
                    BlitScaled(pixels, width, height, edge[mask], SourceSize(edge[mask]),
                        vertexX * displayTileSize, vertexY * displayTileSize, displayTileSize);
            }
            DrawBorder(pixels, width, height);
            return new ImageBuffer(width, height, pixels);
        }

        private static void FillTiledBase(Color32[] pixels, int width, int height,
            Color32[] source, int sourceSize, int displayTileSize)
        {
            var half = displayTileSize / 2;
            for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
            {
                var sourceX = PositiveModulo((x + half) * sourceSize / displayTileSize,
                    sourceSize);
                var sourceY = PositiveModulo((y + half) * sourceSize / displayTileSize,
                    sourceSize);
                pixels[x + y * width] = source[sourceX + sourceY * sourceSize];
            }
        }

        private static int PositiveModulo(int value, int divisor)
        {
            var result = value % divisor;
            return result < 0 ? result + divisor : result;
        }

        private static ImageBuffer JoinHorizontal(ImageBuffer left, ImageBuffer right, int gap)
        {
            var width = left.Width + right.Width + gap;
            var height = Math.Max(left.Height, right.Height);
            var pixels = new Color32[width * height];
            for (var index = 0; index < pixels.Length; index++) pixels[index] = Frame;
            Copy(left, pixels, width, 0, 0);
            Copy(right, pixels, width, left.Width + gap, 0);
            return new ImageBuffer(width, height, pixels);
        }

        private static ImageBuffer PlaceOnPortraitCanvas(ImageBuffer source, int width, int height)
        {
            var pixels = new Color32[width * height];
            for (var index = 0; index < pixels.Length; index++) pixels[index] = Frame;
            var offsetX = (width - source.Width) / 2;
            var offsetY = (height - source.Height) / 2;
            for (var y = 0; y < source.Height; y++)
            for (var x = 0; x < source.Width; x++)
            {
                var targetX = x + offsetX;
                var targetY = y + offsetY;
                if (targetX < 0 || targetX >= width || targetY < 0 || targetY >= height) continue;
                pixels[targetX + targetY * width] = source.Pixels[x + y * source.Width];
            }
            return new ImageBuffer(width, height, pixels);
        }

        private static void BlitScaled(Color32[] target, int targetWidth, int targetHeight,
            Color32[] source, int sourceSize, int targetX, int targetY, int targetSize)
        {
            for (var y = 0; y < targetSize; y++)
            for (var x = 0; x < targetSize; x++)
            {
                var destinationX = targetX + x;
                var destinationY = targetY + y;
                if (destinationX < 0 || destinationX >= targetWidth
                    || destinationY < 0 || destinationY >= targetHeight) continue;
                var sourceMinX = x * sourceSize / targetSize;
                var sourceMaxX = Math.Max(sourceMinX + 1,
                    (x + 1) * sourceSize / targetSize);
                var sourceMinY = y * sourceSize / targetSize;
                var sourceMaxY = Math.Max(sourceMinY + 1,
                    (y + 1) * sourceSize / targetSize);
                var color = BoxFilterPremultiplied(source, sourceSize,
                    sourceMinX, sourceMinY, sourceMaxX, sourceMaxY);
                if (color.a == 0) continue;
                var index = destinationX + destinationY * targetWidth;
                target[index] = AlphaBlend(target[index], color);
            }
        }

        private static Color32 BoxFilterPremultiplied(Color32[] source, int sourceSize,
            int minX, int minY, int maxX, int maxY)
        {
            long alpha = 0;
            long red = 0;
            long green = 0;
            long blue = 0;
            var samples = 0;
            for (var y = minY; y < maxY; y++)
            for (var x = minX; x < maxX; x++)
            {
                var color = source[Mathf.Clamp(x, 0, sourceSize - 1)
                    + Mathf.Clamp(y, 0, sourceSize - 1) * sourceSize];
                alpha += color.a;
                red += color.r * color.a;
                green += color.g * color.a;
                blue += color.b * color.a;
                samples++;
            }
            if (alpha == 0 || samples == 0) return new Color32(0, 0, 0, 0);
            return new Color32(
                (byte)Mathf.RoundToInt(red / (float)alpha),
                (byte)Mathf.RoundToInt(green / (float)alpha),
                (byte)Mathf.RoundToInt(blue / (float)alpha),
                (byte)Mathf.RoundToInt(alpha / (float)samples));
        }

        private static Color32 AlphaBlend(Color32 background, Color32 foreground)
        {
            var alpha = foreground.a / 255f;
            var inverse = 1f - alpha;
            return new Color32(
                (byte)Mathf.RoundToInt(foreground.r * alpha + background.r * inverse),
                (byte)Mathf.RoundToInt(foreground.g * alpha + background.g * inverse),
                (byte)Mathf.RoundToInt(foreground.b * alpha + background.b * inverse),
                255);
        }

        private static bool Occupied(bool[,] cells, int x, int y)
        {
            return x >= 0 && x < cells.GetLength(0) && y >= 0 && y < cells.GetLength(1)
                && cells[x, y];
        }

        private static int SourceSize(Color32[] pixels)
        {
            var size = Mathf.RoundToInt(Mathf.Sqrt(pixels.Length));
            if (size * size != pixels.Length)
                throw new InvalidOperationException("Contour evidence source is not square.");
            return size;
        }

        private static Color32[][] ReadFamily(string folder, int expectedSize)
        {
            var family = new Color32[SquareTerrainArtProfile.MaskCount][];
            for (var mask = 0; mask < family.Length; mask++)
            {
                var path = SquareTerrainArtProfile.MaskTexturePath(folder, mask);
                var texture = LoadPng(path);
                try
                {
                    if (texture.width != expectedSize || texture.height != expectedSize)
                        throw new InvalidOperationException("Evidence mask has unexpected size: " + path);
                    family[mask] = texture.GetPixels32();
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(texture);
                }
            }
            return family;
        }

        private static Texture2D LoadPng(string path)
        {
            var absolute = SquareTerrainArtGenerator.AbsolutePath(path);
            if (!File.Exists(absolute)) throw new FileNotFoundException("Evidence source is missing.", path);
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false, false);
            if (texture.LoadImage(File.ReadAllBytes(absolute), false)) return texture;
            UnityEngine.Object.DestroyImmediate(texture);
            throw new InvalidOperationException("Evidence source could not be decoded: " + path);
        }

        private static void DrawBorder(Color32[] pixels, int width, int height)
        {
            for (var x = 0; x < width; x++)
            {
                pixels[x] = Frame;
                pixels[x + (height - 1) * width] = Frame;
            }
            for (var y = 0; y < height; y++)
            {
                pixels[y * width] = Frame;
                pixels[width - 1 + y * width] = Frame;
            }
        }

        private static void Copy(ImageBuffer source, Color32[] target, int targetWidth,
            int offsetX, int offsetY)
        {
            for (var y = 0; y < source.Height; y++)
            for (var x = 0; x < source.Width; x++)
                target[offsetX + x + (offsetY + y) * targetWidth] =
                    source.Pixels[x + y * source.Width];
        }

        private static void WritePixels(string path, int width, int height, Color32[] pixels)
        {
            SquareTerrainArtGenerator.WritePng(path, width, height, pixels);
        }

        private sealed class ImageBuffer
        {
            public readonly int Width;
            public readonly int Height;
            public readonly Color32[] Pixels;

            public ImageBuffer(int width, int height, Color32[] pixels)
            {
                Width = width;
                Height = height;
                Pixels = pixels;
            }
        }
    }
}
