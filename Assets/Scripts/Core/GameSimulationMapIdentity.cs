using System;
using System.Globalization;
using System.Linq;
using System.Text;

namespace FruitDefense.Core
{
    public sealed partial class GameSimulation
    {
        public string MapId { get; private set; } = BattlefieldMapDefinition.DefaultMapId;

        private static string ResolveMapIdentity(BattlefieldMapDefinition map,
            bool bundledDefault)
        {
            if (!string.IsNullOrWhiteSpace(map.MapId)) return map.MapId;
            if (bundledDefault) return BattlefieldMapDefinition.DefaultMapId;
            if (map.UsesLayeredMap) return map.GameplayFingerprint;
            const ulong offset = 14695981039346656037ul;
            var hash = offset;
            AddMapHash(ref hash, map.GridWidth);
            AddMapHash(ref hash, map.GridHeight);
            AddMapHash(ref hash, map.MapUnitsPerCell);
            AddMapHash(ref hash, map.LegacyToMapScale);
            foreach (var cell in map.RouteCells)
            {
                AddMapHash(ref hash, cell.x);
                AddMapHash(ref hash, cell.y);
            }
            foreach (var cell in map.PlantableCells.OrderBy(value => value.x)
                .ThenBy(value => value.y))
            {
                AddMapHash(ref hash, cell.x);
                AddMapHash(ref hash, cell.y);
            }
            foreach (var node in map.RouteNodes)
            {
                AddMapHash(ref hash, node.x);
                AddMapHash(ref hash, node.y);
            }
            AddMapHash(ref hash, map.Core.x);
            AddMapHash(ref hash, map.Core.y);
            foreach (var groupName in map.InitialPotGroupOrder)
            {
                AddMapHash(ref hash, groupName);
                var group = map.InitialPotGroups[groupName];
                AddMapHash(ref hash, group.InitialCount);
                foreach (var cell in group.Cells)
                {
                    AddMapHash(ref hash, cell.x);
                    AddMapHash(ref hash, cell.y);
                }
            }
            return "map." + hash.ToString("x16", CultureInfo.InvariantCulture);
        }

        private static void AddMapHash(ref ulong hash, float value)
        {
            AddMapHash(ref hash, BitConverter.ToInt32(BitConverter.GetBytes(value), 0));
        }

        private static void AddMapHash(ref ulong hash, int value)
        {
            var raw = unchecked((uint)value);
            for (var shift = 0; shift < 32; shift += 8)
            {
                hash ^= (byte)(raw >> shift);
                hash *= 1099511628211ul;
            }
        }

        private static void AddMapHash(ref ulong hash, string value)
        {
            foreach (var part in Encoding.UTF8.GetBytes(value ?? string.Empty))
            {
                hash ^= part;
                hash *= 1099511628211ul;
            }
            hash ^= 0xff;
            hash *= 1099511628211ul;
        }
    }
}
