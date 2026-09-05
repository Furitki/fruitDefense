using UnityEngine;

namespace FruitDefense.Content
{
    [CreateAssetMenu(fileName = "OutgameContentCatalog",
        menuName = "Fruit Defense/Content/Outgame Content Catalog")]
    public sealed class OutgameContentCatalogAsset : ScriptableObject
    {
        [SerializeField] private OutgameContentCatalogDto catalog =
            new OutgameContentCatalogDto();

        public OutgameContentCatalogDto Catalog
        {
            get { return catalog; }
            set { catalog = value ?? new OutgameContentCatalogDto(); }
        }
    }
}
