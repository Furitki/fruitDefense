using UnityEngine;

namespace FruitDefense.Content
{
    [CreateAssetMenu(fileName = "GameContentManifest",
        menuName = "Fruit Defense/Content/Game Content Manifest")]
    public sealed class GameContentManifestAsset : ScriptableObject
    {
        [SerializeField] private GameContentManifestDto manifest = new GameContentManifestDto();

        public GameContentManifestDto Manifest
        {
            get { return manifest; }
            set { manifest = value ?? new GameContentManifestDto(); }
        }
    }
}
