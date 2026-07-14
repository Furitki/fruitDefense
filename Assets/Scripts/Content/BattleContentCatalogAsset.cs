using UnityEngine;

namespace FruitDefense.Content
{
    [CreateAssetMenu(fileName = "BattleContentCatalog", menuName = "Fruit Defense/Battle Content Catalog")]
    public sealed class BattleContentCatalogAsset : ScriptableObject
    {
        [SerializeField]
        private BattleContentCatalogDto catalog = new BattleContentCatalogDto();

        public BattleContentCatalogDto Catalog
        {
            get { return catalog; }
            set { catalog = value ?? new BattleContentCatalogDto(); }
        }
    }
}
