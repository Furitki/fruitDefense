using System.Collections.Generic;
using UnityEngine;

namespace FruitDefense.Core
{
    public static class P0GameplayParityBaseline
    {
        public const string MapId = BattlefieldMapDefinition.DefaultMapId;
        public const float RouteLength = BattlefieldMapDefinition.DefaultRouteLength;
        public const float LegacyNormalEnemySpeed = 4.4f;
        public const float NormalEnemyTraversalSeconds = BattlefieldMapDefinition.LegacyRouteLength / LegacyNormalEnemySpeed;
        public const int WaveCount = 15;
        public const int InitialPotCount = 8;
        public const string WaveContentSignature =
            "5,0,0,0|6,2,0,0|7,3,0,0|8,3,1,0|8,4,2,0|9,5,2,0|9,6,3,0|"
            + "10,6,4,0|10,7,5,0|11,7,5,1|11,8,6,0|12,8,7,0|12,9,8,0|13,10,9,1|14,11,10,2";
        public const string CombatNumericSignature =
            "plants:pea=12/1/44,watermelon=12/2.2/44,banana=6/1.6/38,durian=12/1.8/18,sunflower=0/10/0;"
            + "enemies:normal=36/4.4/1/1,runner=25/6.4/1/1,armored=80/3.4/1/2,boss=430/2.7/1/3;"
            + "stars:damage=1/1.5/3/5,speed=1/1.05/1.1/1.2,range=1/1.05/1.1/1.15;"
            + "waves=15,between=15,initial-pots=8";
    }

    public static class GameConfig
    {
        public static readonly BattlefieldMapDefinition DefaultBattlefield = BattlefieldMapDefinition.CreateDefault();
        public static IReadOnlyList<Vector2Int> PlantingCells { get { return DefaultBattlefield.PlantableCells; } }

        public static int RefreshCost(int refreshCount) { return 10 + refreshCount * 5; }

        public static float MapDistance(float legacyDistance) { return DefaultBattlefield.FromLegacyDistance(legacyDistance); }
    }
}
