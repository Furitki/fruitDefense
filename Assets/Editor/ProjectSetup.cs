using FruitDefense.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FruitDefense.Editor
{
    public static class ProjectSetup
    {
        [MenuItem("Fruit Defense/Configure Project")]
        public static void Configure()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Scenes")) AssetDatabase.CreateFolder("Assets", "Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("FruitDefenseGame");
            root.AddComponent<FruitDefenseGame>();
            EditorSceneManager.SaveScene(scene, "Assets/Scenes/Main.unity");
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/Scenes/Main.unity", true) };

            PlayerSettings.companyName = "Fruit Defense";
            PlayerSettings.productName = "水果塔防";
            PlayerSettings.defaultScreenWidth = 1206;
            PlayerSettings.defaultScreenHeight = 2622;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = false;
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Standalone, "com.fruitdefense.game");
            QualitySettings.vSyncCount = 1;
            AssetDatabase.SaveAssets();
            Debug.Log("Fruit Defense project configured: Assets/Scenes/Main.unity");
        }

        public static void SmokeValidate()
        {
            ValidateLegacyMigrationBaseline();
            ValidateBattlefieldDefinition();
            ValidateDragRegressionCoverage();
            var simulation = new GameSimulation(12345);
            Assert(simulation.State.Pots.Count == 8, "initial pot count");
            foreach (var group in simulation.Map.InitialPotGroups.Values)
                Assert(simulation.State.Pots.FindAll(pot => System.Linq.Enumerable.Contains(group.Cells, pot.Cell)).Count == group.InitialCount,
                    "initial pot distribution in semantic group " + group.Name);
            Assert(simulation.State.Sun == 10 && simulation.State.Lives == 10, "initial resources");
            Assert(GameConfig.GetWave(1).Sequence.Count == 5, "wave 1 count");
            Assert(GameConfig.GetWave(6).Sequence.Count == (9 + 5 + 2) * 3, "wave 6 count scaling");
            Assert(Mathf.Approximately(GameConfig.WaveHpMultiplier(3), 2f), "wave health scaling");
            Assert(GameConfig.PlantingCells.Count == 48 && simulation.State.Pots.Count < GameConfig.PlantingCells.Count,
                "full planting grid contains visible empty cells");
            Assert(simulation.RefreshNursery(out _), "first nursery refresh");
            Assert(simulation.State.Plants.Count + simulation.LastNurseryPotSlots.Count == 5, "nursery result count includes pots");
            var plant = simulation.State.Plants[0];
            var pot = simulation.State.Pots[0];
            var plantDrop = simulation.GetPlantDropStatus(plant.Id, pot.Id);
            Assert(plantDrop.Legal && plantDrop.Action == PlantDropAction.Plant, "nursery plant can be dragged to pot");
            Assert(simulation.MoveOrMergePlant(plant.Id, pot.Id, out _), "plant drop commits");
            var nurseryDrop = simulation.GetNurseryDropStatus(plant.Id, 0);
            Assert(nurseryDrop.Legal && nurseryDrop.Action == PlantDropAction.Move, "planted fruit can return to nursery");
            simulation.State.Inventory.Ice = 1;
            Assert(simulation.GetWeaponInstallStatus(plant.Id, WeaponKind.Ice).Legal, "weapon can be dragged to plant");
            simulation.State.Inventory.Pots = 1;
            var expansion = System.Linq.Enumerable.First(GameConfig.PlantingCells, cell => simulation.CanExpand(cell));
            Assert(simulation.CanExpand(expansion), "pot can be dragged to legal expansion cell");
            simulation.State.Phase = GamePhase.Playing;
            plant.MoveCooldown = 1f;
            var otherPot = simulation.State.Pots.Find(candidate => candidate.Id != pot.Id);
            Assert(!simulation.GetPlantDropStatus(plant.Id, otherPot.Id).Legal, "move cooldown blocks drag drop");
            var overwritten = simulation.State.Plants.Find(candidate => candidate.NurseryIndex >= 0);
            Assert(overwritten != null, "occupied nursery has a refresh replacement target");
            overwritten.Weapon = WeaponKind.Ice;
            var iceBeforeRefresh = simulation.State.Inventory.Ice;
            simulation.State.Sun = 100;
            Assert(simulation.RefreshNursery(out _), "occupied nursery can refresh");
            Assert(simulation.PlantById(overwritten.Id) == null, "refresh replaces occupied nursery fruit");
            Assert(simulation.State.Inventory.Ice == iceBeforeRefresh + 1, "refresh recovers overwritten weapon");
            Assert(simulation.State.Plants.FindAll(candidate => candidate.NurseryIndex >= 0).Count
                + simulation.LastNurseryPotSlots.Count == 5, "replacement refresh fills five result slots");

            var foundPotReward = false;
            for (var seed = 1; seed <= 256 && !foundPotReward; seed++)
            {
                var potRoll = new GameSimulation(seed);
                Assert(potRoll.RefreshNursery(out _), "pot roll refresh succeeds");
                foundPotReward = potRoll.LastNurseryPotSlots.Count > 0
                    && potRoll.State.Inventory.Pots == potRoll.LastNurseryPotSlots.Count;
            }
            Assert(foundPotReward, "nursery refresh can roll and auto-store flowerpots");
            ValidateMigrationBehavior();
            ValidateCombatActions();
            var collisionTarget = new Rect(100f, 100f, 40f, 40f);
            var cursorOutsideTarget = new Vector2(160f, 160f);
            var preview = DragGeometry.PreviewRect(cursorOutsideTarget);
            Assert(!collisionTarget.Contains(cursorOutsideTarget), "drag cursor remains outside collision target");
            Assert(DragGeometry.OverlapArea(preview, collisionTarget) > 0f, "drag preview overlaps target independently of cursor");
            var bestTarget = DragGeometry.BestOverlapIndex(preview, new[]
            {
                collisionTarget,
                new Rect(142f, 142f, 30f, 30f),
            });
            Assert(bestTarget == 0, "largest preview overlap wins drop target selection");
            Assert(AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/TempArt/fruit-defense-temp-atlas.png") != null,
                "temporary art atlas imported");
            Assert(AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/TempArt/combat-vfx-atlas.png") != null,
                "temporary combat effect atlas imported");
            var uiFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/Resources/Fonts/NotoSansSC-UI.ttf");
            Assert(uiFont != null, "bundled WebGL UI font imported");
            Assert(uiFont.HasCharacter('水') && uiFont.HasCharacter('果') && uiFont.HasCharacter('塔')
                && uiFont.HasCharacter('防'), "bundled WebGL UI font covers representative Chinese copy");
            foreach (var glyph in "立即开始下一波继续游戏重新")
                Assert(uiFont.HasCharacter(glyph), "bundled WebGL UI font covers session-control glyph: " + glyph);
            Assert(FruitDefenseGame.ValidatePortraitLayout(out var portraitLayoutReason),
                "portrait layout geometry: " + portraitLayoutReason);
            Assert(FruitDefenseGame.ValidateInspectionOnlyInteraction(out var inspectionReason),
                "inspection-only interaction contract: " + inspectionReason);
            Assert(FruitDefenseGame.ValidateSessionControlContract(out var sessionControlReason),
                "session control contract: " + sessionControlReason);
            Assert(EditorBuildSettings.scenes.Length == 1 && EditorBuildSettings.scenes[0].enabled, "build scene configured");
            Debug.Log("FRUIT_DEFENSE_SMOKE_OK");
        }

        private static void ValidateLegacyMigrationBaseline()
        {
            const float legacyRouteLength = 228f;
            const float legacyNormalSpeed = 4.4f;
            const float expectedTraversalSeconds = legacyRouteLength / legacyNormalSpeed;
            const float previousBoardWidth = 386f;
            const float previousBoardHeight = 320f;
            const float previousBoardScale = previousBoardWidth / 1050f;
            const float previousPotSize = 62f * previousBoardScale;
            const float previousHorizontalCellPitch = previousBoardWidth * 8f / 100f;
            const float previousVerticalCellPitch = previousBoardHeight * 10f / 100f;

            Assert(Mathf.Approximately(previousPotSize, 22.79238f), "legacy reference flowerpot geometry recorded");
            Assert(Mathf.Approximately(previousHorizontalCellPitch, 30.88f)
                && Mathf.Approximately(previousVerticalCellPitch, 32f), "legacy reference cell pitch recorded");
            Assert(Mathf.Approximately(GameConfig.PathLength / GameConfig.Zombie(ZombieKind.Normal).Speed, expectedTraversalSeconds),
                "normal zombie route duration preserved from legacy baseline");

            var legacyNear = Vector2.Distance(new Vector2(17f, 25f), new Vector2(17f, 12f));
            var legacyRepresentative = Mathf.Min(
                Vector2.Distance(new Vector2(41f, 55f), new Vector2(41f, 12f)),
                Vector2.Distance(new Vector2(41f, 55f), new Vector2(41f, 88f)));
            Assert(legacyNear <= 18f && legacyRepresentative <= 44f && legacyRepresentative > 18f,
                "legacy representative target coverage recorded");

            var map = GameConfig.DefaultBattlefield;
            var nearDistance = DistanceToRoute(map, map.CellToMap(new Vector2Int(0, 0)));
            var representativeDistance = DistanceToRoute(map, map.CellToMap(new Vector2Int(3, 3)));
            Assert(nearDistance <= GameConfig.Plant(PlantKind.Durian).Range
                && representativeDistance <= GameConfig.Plant(PlantKind.Pea).Range
                && representativeDistance > GameConfig.Plant(PlantKind.Durian).Range,
                "representative target coverage preserved after map-unit migration");
        }

        private static void ValidateBattlefieldDefinition()
        {
            var map = GameConfig.DefaultBattlefield;
            Assert(map.Validate(out var reason), "default battlefield topology: " + reason);
            Assert(map.GridWidth == 8 && map.GridHeight == 6 && map.PlantableCells.Count == 48,
                "default battlefield is an 8-by-6 grid with 48 cells");
            Assert(map.PlantableCells.Count == System.Linq.Enumerable.Count(System.Linq.Enumerable.Distinct(map.PlantableCells)),
                "default battlefield cells are unique");
            Assert(map.InitialPotGroups.Count == 3
                && System.Linq.Enumerable.Sum(map.InitialPotGroups.Values, group => group.InitialCount) == GameConfig.InitialPotCount,
                "semantic groups place eight initial flowerpots");
            Assert(map.Entry == map.Route.Sample(0f) && map.Exit == map.Route.Sample(map.Route.TotalLength),
                "route endpoint sampling matches entry and exit");
            Assert(Mathf.Approximately(map.Route.TotalLength, 23f), "route length derives from unequal segments");
            var beforeCorner = map.Route.Sample(7.999f);
            var atCorner = map.Route.Sample(8f);
            var afterCorner = map.Route.Sample(8.001f);
            Assert(Vector2.Distance(beforeCorner, atCorner) < .002f && Vector2.Distance(atCorner, afterCorner) < .002f,
                "route sampling is continuous across an arbitrary segment boundary");
            var center = new Vector2Int(3, 3);
            Assert(System.Linq.Enumerable.Count(map.Topology.CardinalNeighbors(center)) == 4
                && map.Topology.AreCardinalNeighbors(center, center + Vector2Int.right)
                && !map.Topology.AreCardinalNeighbors(center, center + Vector2Int.one),
                "topology exposes cardinal neighbors and rejects diagonals");

            var duplicateCellMap = CreateInvalidMap(new[] { Vector2Int.zero, Vector2Int.zero },
                new[] { Vector2.zero, Vector2.right }, new[] { Vector2Int.zero });
            Assert(!duplicateCellMap.Validate(out reason) && reason.Contains("unique"), "duplicate cell topology is rejected");
            var zeroSegmentMap = CreateInvalidMap(new[] { Vector2Int.zero },
                new[] { Vector2.zero, Vector2.zero }, new[] { Vector2Int.zero });
            Assert(!zeroSegmentMap.Validate(out reason) && reason.Contains("zero-length"), "zero-length route segment is rejected");
            var invalidGroupMap = CreateInvalidMap(new[] { Vector2Int.zero },
                new[] { Vector2.zero, Vector2.right }, new[] { Vector2Int.right });
            Assert(!invalidGroupMap.Validate(out reason) && reason.Contains("non-plantable"), "invalid semantic group is rejected");
            var outOfBoundsMap = CreateInvalidMap(new[] { new Vector2Int(2, 0) },
                new[] { Vector2.zero, Vector2.right }, new[] { new Vector2Int(2, 0) });
            Assert(!outOfBoundsMap.Validate(out reason) && reason.Contains("outside grid bounds"), "out-of-bounds cell is rejected");
        }

        private static BattlefieldMapDefinition CreateInvalidMap(
            Vector2Int[] cells,
            Vector2[] route,
            Vector2Int[] initialCells)
        {
            return new BattlefieldMapDefinition(
                2,
                2,
                1f,
                cells,
                route,
                Vector2.one * .5f,
                new[] { new InitialPotGroup("test", 1, initialCells) });
        }

        private static float DistanceToRoute(BattlefieldMapDefinition map, Vector2 point)
        {
            var best = float.MaxValue;
            var step = Mathf.Max(.001f, map.Route.TotalLength / 1000f);
            for (var progress = 0f; progress <= map.Route.TotalLength; progress += step)
                best = Mathf.Min(best, Vector2.Distance(point, map.Route.Sample(progress)));
            return best;
        }

        private static void ValidateMigrationBehavior()
        {
            var expansion = new GameSimulation(2468);
            expansion.State.Pots.Clear();
            expansion.State.Pots.Add(new Pot { Id = 1, Cell = new Vector2Int(3, 3), Active = true });
            expansion.State.Inventory.Pots = 1;
            Assert(expansion.CanExpand(new Vector2Int(4, 3)), "cardinal expansion remains legal");
            Assert(!expansion.CanExpand(new Vector2Int(4, 4)), "diagonal-only expansion remains illegal");

            var traversal = new GameSimulation(1357);
            traversal.State.Plants.Clear();
            traversal.State.Zombies.Clear();
            traversal.State.Feedback.Clear();
            traversal.State.Phase = GamePhase.Playing;
            traversal.State.WaveIndex = 1;
            traversal.State.WaveTotal = 0;
            traversal.State.WaveSpawned = 0;
            traversal.State.Zombies.Add(new Zombie
            {
                Id = 999,
                Kind = ZombieKind.Normal,
                Hp = 1000f,
                MaxHp = 1000f,
                Speed = GameConfig.Zombie(ZombieKind.Normal).Speed,
                Reward = 0,
                Threat = 1,
            });
            var lives = traversal.State.Lives;
            for (var step = 0; step < 2000 && traversal.State.Lives == lives; step++) traversal.Tick(.05f);
            var expectedSeconds = BattlefieldMapDefinition.LegacyRouteLength / 4.4f;
            Assert(traversal.State.Lives == lives - 1
                && Mathf.Abs(traversal.State.Elapsed - expectedSeconds) <= .1f,
                "normal zombie traversal timing remains within migration tolerance");
        }

        private static void ValidateDragRegressionCoverage()
        {
            var simulation = new GameSimulation(4242);
            simulation.State.Plants.Clear();
            simulation.State.Feedback.Clear();
            var firstPot = simulation.State.Pots[0];
            var secondPot = simulation.State.Pots[1];
            var source = new Plant
            {
                Id = 7001,
                Kind = PlantKind.Pea,
                Star = 1,
                PotId = -1,
                NurseryIndex = 0,
            };
            simulation.State.Plants.Add(source);

            var placement = simulation.GetPlantDropStatus(source.Id, firstPot.Id);
            Assert(placement.Legal && placement.Action == PlantDropAction.Plant
                && simulation.MoveOrMergePlant(source.Id, firstPot.Id, out _)
                && source.PotId == firstPot.Id && source.NurseryIndex == -1,
                "drag placement remains available");

            var movement = simulation.GetPlantDropStatus(source.Id, secondPot.Id);
            Assert(movement.Legal && movement.Action == PlantDropAction.Move
                && simulation.MoveOrMergePlant(source.Id, secondPot.Id, out _)
                && source.PotId == secondPot.Id,
                "drag movement remains available");

            var nurseryReturn = simulation.GetNurseryDropStatus(source.Id, 0);
            Assert(nurseryReturn.Legal && nurseryReturn.Action == PlantDropAction.Move
                && simulation.MoveToNursery(source.Id, 0, out _)
                && source.PotId == -1 && source.NurseryIndex == 0,
                "drag return to nursery remains available");

            Assert(simulation.MoveOrMergePlant(source.Id, firstPot.Id, out _),
                "merge source can be planted before drag merge");
            var mergeTarget = new Plant
            {
                Id = 7002,
                Kind = PlantKind.Pea,
                Star = 1,
                PotId = secondPot.Id,
                NurseryIndex = -1,
            };
            simulation.State.Plants.Add(mergeTarget);
            var merge = simulation.GetPlantDropStatus(source.Id, secondPot.Id);
            Assert(merge.Legal && merge.Action == PlantDropAction.Merge
                && simulation.MoveOrMergePlant(source.Id, secondPot.Id, out _)
                && simulation.PlantById(source.Id) == null && mergeTarget.Star == 2,
                "drag merge remains available");

            var invalidSource = new Plant
            {
                Id = 7003,
                Kind = PlantKind.Banana,
                Star = 1,
                PotId = firstPot.Id,
                NurseryIndex = -1,
            };
            simulation.State.Plants.Add(invalidSource);
            var invalid = simulation.GetPlantDropStatus(invalidSource.Id, secondPot.Id);
            Assert(!invalid.Legal && invalid.Action == PlantDropAction.Invalid
                && invalidSource.PotId == firstPot.Id && mergeTarget.PotId == secondPot.Id,
                "invalid drag target leaves both plants in place");

            simulation.State.Inventory.Ice = 1;
            var weaponStatus = simulation.GetWeaponInstallStatus(invalidSource.Id, WeaponKind.Ice);
            Assert(weaponStatus.Legal
                && simulation.InstallWeapon(invalidSource.Id, WeaponKind.Ice, out _)
                && invalidSource.Weapon == WeaponKind.Ice
                && simulation.State.Inventory.Ice == 0,
                "explicit weapon tool installation remains available");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new System.InvalidOperationException("Smoke validation failed: " + message);
        }

        private static void ValidateCombatActions()
        {
            var pea = CreateCombatScenario(PlantKind.Pea);
            pea.Tick(.01f);
            Assert(pea.State.Projectiles.Count == 1 && Mathf.Approximately(pea.State.Zombies[0].Hp, 1000f),
                "pea creates a delayed tracking projectile");
            TickUntilProjectilesFinish(pea, 40);
            Assert(pea.State.Zombies[0].Hp < 1000f && pea.State.CombatEffects.Exists(effect => effect.Kind == CombatEffectKind.PeaImpact),
                "pea projectile tracks and creates an impact action");

            var watermelon = CreateCombatScenario(PlantKind.Watermelon);
            watermelon.Tick(.01f);
            Assert(watermelon.State.Projectiles.Count == 1 && watermelon.State.Projectiles[0].Progress > 0f,
                "watermelon starts a timed arc projectile");
            for (var step = 0; step < 12; step++) watermelon.Tick(.05f);
            Assert(watermelon.State.CombatEffects.Exists(effect => effect.Kind == CombatEffectKind.WatermelonBlast)
                && watermelon.State.Zombies[0].Hp < 1000f, "watermelon lands and creates an area blast");

            var banana = CreateCombatScenario(PlantKind.Banana);
            banana.Tick(.01f);
            banana.State.Plants[0].AttackCooldown = 999f;
            TickUntilProjectilesFinish(banana, 90);
            Assert(Mathf.Approximately(banana.State.Zombies[0].Hp, 988f),
                "banana hits once outbound and once while returning");

            var durian = CreateCombatScenario(PlantKind.Durian);
            durian.Tick(.01f);
            Assert(durian.State.CombatEffects.Exists(effect => effect.Kind == CombatEffectKind.DurianDrop)
                && durian.State.Zombies[0].Hp < 1000f, "durian uses a melee drop and shockwave action");

            var sunflower = CreateCombatScenario(PlantKind.Sunflower);
            sunflower.State.Plants[0].ProductionProgress = 9.99f;
            sunflower.State.Sun = 0;
            sunflower.Tick(.02f);
            Assert(sunflower.State.Sun == 1
                && sunflower.State.CombatEffects.Exists(effect => effect.Kind == CombatEffectKind.SunBurst),
                "sunflower production creates a visible sun burst");

            var iceSunflower = CreateCombatScenario(PlantKind.Sunflower, WeaponKind.Ice);
            iceSunflower.State.Zombies.Clear();
            iceSunflower.State.WaveSpawned = 0;
            iceSunflower.State.WaveTotal = GameConfig.GetWave(1).Sequence.Count;
            iceSunflower.State.SpawnCooldown = 0f;
            iceSunflower.Tick(.01f);
            Assert(iceSunflower.State.Zombies.Count > 0
                && iceSunflower.State.Zombies[0].SlowUntil > iceSunflower.State.Elapsed,
                "ice sunflower slows the battlefield on the first wave spawn");

            var gatling = CreateCombatScenario(PlantKind.Pea, WeaponKind.Gatling);
            gatling.Tick(.01f);
            Assert(gatling.State.Plants[0].BurstShotsRemaining == 3, "gatling starts a four-shot burst");
            for (var step = 0; step < 5; step++) gatling.Tick(.05f);
            Assert(gatling.State.Plants[0].BurstShotsRemaining == 2
                && gatling.State.CombatEffects.Exists(effect => effect.Kind == CombatEffectKind.GatlingMuzzle),
                "gatling spaces burst shots by 0.2 seconds");

            var ice = CreateCombatScenario(PlantKind.Pea, WeaponKind.Ice);
            ice.Tick(.01f);
            TickUntilProjectilesFinish(ice, 40);
            Assert(ice.State.Zombies[0].SlowUntil > ice.State.Elapsed
                && ice.State.CombatEffects.Exists(effect => effect.Kind == CombatEffectKind.IceImpact),
                "ice weapon adds slow and a crystal impact");

            var chili = CreateCombatScenario(PlantKind.Pea, WeaponKind.Chili);
            chili.Tick(.01f);
            TickUntilProjectilesFinish(chili, 40);
            Assert(chili.State.Zombies[0].Burns.Count == 1
                && chili.State.CombatEffects.Exists(effect => effect.Kind == CombatEffectKind.ChiliImpact),
                "chili weapon adds a burn stack and flame impact");
        }

        private static GameSimulation CreateCombatScenario(PlantKind kind, WeaponKind weapon = WeaponKind.None)
        {
            var simulation = new GameSimulation(9876 + (int)kind * 17 + (int)weapon);
            simulation.State.Plants.Clear();
            simulation.State.Zombies.Clear();
            simulation.State.Projectiles.Clear();
            simulation.State.CombatEffects.Clear();
            simulation.State.Feedback.Clear();
            simulation.State.Phase = GamePhase.Playing;
            simulation.State.WaveIndex = 1;
            simulation.State.WaveTotal = 1;
            simulation.State.WaveSpawned = 1;
            simulation.State.NextId = 10000;
            var pot = simulation.State.Pots[0];
            simulation.State.Plants.Add(new Plant
            {
                Id = 9001,
                Kind = kind,
                Star = 1,
                PotId = pot.Id,
                NurseryIndex = -1,
                Weapon = weapon,
            });
            simulation.State.Zombies.Add(new Zombie
            {
                Id = 9002,
                Kind = ZombieKind.Normal,
                Hp = 1000f,
                MaxHp = 1000f,
                Speed = 0f,
                PathProgress = NearestPathProgress(simulation, simulation.PotPoint(pot)),
                Reward = 0,
                Threat = 1,
            });
            return simulation;
        }

        private static float NearestPathProgress(GameSimulation simulation, Vector2 point)
        {
            var bestProgress = 0f;
            var bestDistance = float.MaxValue;
            var step = GameConfig.MapDistance(.25f);
            for (var progress = 0f; progress <= simulation.Map.Route.TotalLength; progress += step)
            {
                var distance = Vector2.SqrMagnitude(simulation.Map.Route.Sample(progress) - point);
                if (distance >= bestDistance) continue;
                bestDistance = distance;
                bestProgress = progress;
            }
            return bestProgress;
        }

        private static void TickUntilProjectilesFinish(GameSimulation simulation, int maxSteps)
        {
            for (var step = 0; step < maxSteps && simulation.State.Projectiles.Count > 0; step++)
                simulation.Tick(.05f);
        }
    }
}
