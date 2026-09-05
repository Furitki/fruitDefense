using System;
using FruitDefense.Content;

namespace FruitDefense.Editor
{
    internal static class OutgameContentFixture
    {
        public static OutgameContentCatalogDto Clone(
            OutgameContentCatalogDto source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            return OutgameContentJson.DeepCopy(source);
        }

        public static OutgameContentCatalogDto WithMissingCostItem(
            OutgameContentCatalogDto source)
        {
            var copy = Clone(source);
            copy.growthEquipment[0].ranks[1].costs[0].itemId = "item.missing";
            return copy;
        }

        public static OutgameContentCatalogDto WithUnsupportedOperation(
            OutgameContentCatalogDto source)
        {
            var copy = Clone(source);
            copy.growthEquipment[0].ranks[1].contributions[0].operationId =
                "modifier.unknown";
            return copy;
        }

        public static OutgameContentCatalogDto WithInvalidCap(
            OutgameContentCatalogDto source)
        {
            var copy = Clone(source);
            copy.growthPolicies[0].caps[0].minimumValue = 2f;
            copy.growthPolicies[0].caps[0].maximumValue = 1f;
            return copy;
        }
    }
}
