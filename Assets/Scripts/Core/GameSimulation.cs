using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FruitDefense.Core
{
    public sealed class GameSimulation
    {
        public const float FixedStepSeconds = .05f;
        public const float MaxFrameDeltaSeconds = .25f;
        public const int MaxStepsPerFrame = 5;
        private const double AccumulatorEpsilon = 0.0000001;
        private const float HitStunSeconds = .1f;
        private const double NurseryPotChance = .1;
        private static readonly float ProjectileSpeed = GameConfig.MapDistance(65f);
        private static readonly float BananaSpeed = GameConfig.MapDistance(48f);
        private const float BananaRangeMultiplier = 1.5f;
        private const float WatermelonFlightSeconds = .4f;
        private static readonly float WatermelonBlastRadius = GameConfig.MapDistance(7f);
        private static readonly float ZombieHitRadius = GameConfig.MapDistance(2.25f);
        private const int GatlingBurstShots = 4;
        private const float GatlingBurstInterval = .2f;
        private readonly DeterministicRandom _random;
        private readonly List<int> _lastNurseryPotSlots = new List<int>();
        private double _frameAccumulator;
        public GameState State { get; private set; }
        public BattlefieldMapDefinition Map { get; private set; }
        public IReadOnlyList<int> LastNurseryPotSlots { get { return _lastNurseryPotSlots; } }
        public double FrameAccumulatorSeconds { get { return _frameAccumulator; } }
        public uint RandomState { get { return _random.State; } }

        public GameSimulation(int seed = 0, BattlefieldMapDefinition map = null)
        {
            Map = map ?? GameConfig.DefaultBattlefield;
            string mapReason;
            if (!Map.Validate(out mapReason)) throw new InvalidOperationException("Invalid battlefield map: " + mapReason);
            _random = new DeterministicRandom(seed);
            Reset(seed);
        }

        public void Reset(int seed = 0)
        {
            _random.Reset(seed);
            ResetFrameAccumulator();
            State = new GameState { Phase = GamePhase.Ready, Sun = 10, Lives = 10, RandomSeed = seed };
            _lastNurseryPotSlots.Clear();
            AddInitialPots();
            AddGlobalFeedback("准备守护果园", new Color(.27f, .48f, .18f));
        }

        private void AddInitialPots()
        {
            foreach (var groupName in Map.InitialPotGroupOrder)
            {
                var group = Map.InitialPotGroups[groupName];
                var cells = group.Cells.ToList();
                Shuffle(cells);
                for (var index = 0; index < group.InitialCount; index++) AddPot(cells[index]);
            }
        }

        private void Shuffle<T>(IList<T> items)
        {
            for (var index = items.Count - 1; index > 0; index--)
            {
                var target = _random.NextInt(index + 1);
                var value = items[index];
                items[index] = items[target];
                items[target] = value;
            }
        }

        private void AddPot(Vector2Int cell)
        {
            State.Pots.Add(new Pot
            {
                Id = State.NextId++, Cell = cell, Active = true,
            });
        }

        public bool RefreshNursery(out string reason)
        {
            var cost = GameConfig.RefreshCost(State.RefreshCount);
            if (State.Sun < cost) { reason = "阳光不足"; return false; }

            var overwritten = State.Plants.Where(plant => plant.NurseryIndex >= 0).ToList();
            foreach (var plant in overwritten)
                if (plant.Weapon != WeaponKind.None) State.Inventory.Add(plant.Weapon, 1);
            State.Plants.RemoveAll(plant => plant.NurseryIndex >= 0);

            var firstBatch = State.RefreshCount == 0;
            var results = new List<int>();
            var sunflowerCount = 0;
            for (var index = 0; index < 5; index++)
            {
                var forceAttacker = firstBatch && index < 2;
                if (!forceAttacker && _random.NextUnitDouble() < NurseryPotChance)
                {
                    results.Add(-1);
                    continue;
                }
                var kind = forceAttacker || sunflowerCount >= 2 ? _random.NextInt(0, 4) : _random.NextInt(0, 5);
                if ((PlantKind)kind == PlantKind.Sunflower) sunflowerCount++;
                results.Add(kind);
            }
            Shuffle(results);

            State.Sun -= cost;
            State.RefreshCount++;
            _lastNurseryPotSlots.Clear();
            for (var slot = 0; slot < 5; slot++)
            {
                if (results[slot] < 0)
                {
                    _lastNurseryPotSlots.Add(slot);
                    State.Inventory.Pots++;
                    continue;
                }
                State.Plants.Add(new Plant
                {
                    Id = State.NextId++, Kind = (PlantKind)results[slot],
                    Star = 1, NurseryIndex = slot,
                });
            }
            var plantCount = 5 - _lastNurseryPotSlots.Count;
            reason = _lastNurseryPotSlots.Count > 0
                ? "刷新完成：水果 " + plantCount + " 株，花盆×" + _lastNurseryPotSlots.Count + " 已入库"
                : "获得五株水果";
            AddGlobalFeedback(reason, new Color(.35f, .58f, .2f));
            return true;
        }

        public bool StartWave(out string reason)
        {
            if (State.Phase == GamePhase.Playing) { reason = "波次正在进行"; return false; }
            if (State.Phase == GamePhase.Victory || State.Phase == GamePhase.Defeat) { reason = "本局已经结束"; return false; }
            var next = State.WaveIndex + 1;
            if (next > GameConfig.MaxWaves) { reason = "所有波次已经完成"; return false; }
            var wave = GameConfig.GetWave(next);
            State.Phase = GamePhase.Playing;
            State.WaveIndex = next;
            State.WaveSpawned = 0;
            State.WaveTotal = wave.Sequence.Count;
            State.SpawnCooldown = 0f;
            State.BetweenTimer = 0f;
            reason = "第 " + next + " 波来袭";
            AddGlobalFeedback(reason, next == GameConfig.MaxWaves ? new Color(.8f, .2f, .15f) : new Color(.2f, .42f, .7f));
            return true;
        }

        public void TogglePause() { State.Paused = !State.Paused; }
        public void SetSpeed(int speed) { State.Speed = Mathf.Clamp(speed, 1, 2); }

        public Plant PlantAtPot(int potId) { return State.Plants.FirstOrDefault(plant => plant.PotId == potId); }
        public Plant PlantAtNursery(int slot) { return State.Plants.FirstOrDefault(plant => plant.NurseryIndex == slot); }
        public Pot PotById(int id) { return State.Pots.FirstOrDefault(pot => pot.Id == id); }
        public Plant PlantById(int id) { return State.Plants.FirstOrDefault(plant => plant.Id == id); }
        public Vector2 PotPoint(Pot pot) { return pot == null ? Map.Core : Map.CellToMap(pot.Cell); }

        public PlantDropStatus GetPlantDropStatus(int plantId, int potId)
        {
            var plant = PlantById(plantId);
            if (plant == null) return new PlantDropStatus(false, PlantDropAction.Invalid, "植物不存在");
            var pot = PotById(potId);
            if (pot == null || !pot.Active) return new PlantDropStatus(false, PlantDropAction.Invalid, "这里不是可用花盆");
            if (plant.PotId == potId) return new PlantDropStatus(false, PlantDropAction.Cancel, "植物已在这个花盆中");
            if (State.Phase == GamePhase.Playing && plant.MoveCooldown > 0f)
                return new PlantDropStatus(false, PlantDropAction.Invalid, "移动冷却 " + plant.MoveCooldown.ToString("0.0") + " 秒");

            var target = PlantAtPot(potId);
            if (target == null)
            {
                var action = plant.NurseryIndex >= 0 ? PlantDropAction.Plant : PlantDropAction.Move;
                return new PlantDropStatus(true, action, action == PlantDropAction.Plant ? "可种植" : "可移动");
            }
            if (target.Kind != plant.Kind || target.Star != plant.Star)
                return new PlantDropStatus(false, PlantDropAction.Invalid, "只能合成同种类、同星级植物");
            if (target.Star >= 4)
                return new PlantDropStatus(false, PlantDropAction.Invalid, "植物已达到四星");
            return new PlantDropStatus(true, PlantDropAction.Merge, "可合成为 " + (target.Star + 1) + " 星");
        }

        public PlantDropStatus GetNurseryDropStatus(int plantId, int slot)
        {
            var plant = PlantById(plantId);
            if (plant == null) return new PlantDropStatus(false, PlantDropAction.Invalid, "植物不存在");
            if (slot < 0 || slot >= 5) return new PlantDropStatus(false, PlantDropAction.Invalid, "这里不是苗圃槽位");
            if (plant.NurseryIndex == slot) return new PlantDropStatus(false, PlantDropAction.Cancel, "植物已在这个苗圃槽位中");
            if (State.Phase == GamePhase.Playing && plant.MoveCooldown > 0f)
                return new PlantDropStatus(false, PlantDropAction.Invalid, "移动冷却 " + plant.MoveCooldown.ToString("0.0") + " 秒");

            var target = PlantAtNursery(slot);
            if (target == null)
                return new PlantDropStatus(true, PlantDropAction.Move, plant.PotId >= 0 ? "可放回苗圃" : "可移动到此槽位");
            if (target.Kind != plant.Kind || target.Star != plant.Star)
                return new PlantDropStatus(false, PlantDropAction.Invalid, "只能合成同种类、同星级植物");
            if (target.Star >= 4)
                return new PlantDropStatus(false, PlantDropAction.Invalid, "植物已达到四星");
            return new PlantDropStatus(true, PlantDropAction.Merge, "可合成为 " + (target.Star + 1) + " 星");
        }

        public bool MoveOrMergePlant(int plantId, int potId, out string reason)
        {
            var plant = PlantById(plantId);
            var pot = PotById(potId);
            var status = GetPlantDropStatus(plantId, potId);
            if (plant == null || pot == null || !status.Legal) { reason = status.Reason; return false; }
            var target = PlantAtPot(potId);
            if (target == null)
            {
                var wasPlanted = plant.PotId >= 0;
                plant.PotId = potId;
                plant.NurseryIndex = -1;
                plant.AttackCooldown = 0f;
                if (wasPlanted && State.Phase == GamePhase.Playing) plant.MoveCooldown = 2f;
                reason = wasPlanted ? "水果已移动" : "水果已种下";
                AddFeedback(reason, PotPoint(pot), new Color(.25f, .62f, .24f));
                return true;
            }
            if (plant.Weapon != WeaponKind.None) State.Inventory.Add(plant.Weapon, 1);
            target.Star++;
            target.AttackCooldown = 0f;
            target.ProductionProgress = 0f;
            State.Plants.Remove(plant);
            reason = GameConfig.Plant(target.Kind).Name + "升至 " + target.Star + " 星";
            AddFeedback(reason, PotPoint(pot), new Color(.95f, .68f, .12f));
            return true;
        }

        public bool MoveToNursery(int plantId, int slot, out string reason)
        {
            var plant = PlantById(plantId);
            var status = GetNurseryDropStatus(plantId, slot);
            if (plant == null || !status.Legal) { reason = status.Reason; return false; }
            var target = PlantAtNursery(slot);
            if (target == null)
            {
                var returningDuringBattle = plant.PotId >= 0 && State.Phase == GamePhase.Playing;
                plant.PotId = -1;
                plant.NurseryIndex = slot;
                plant.AttackCooldown = 0f;
                if (returningDuringBattle) plant.MoveCooldown = 2f;
                reason = status.Reason == "可放回苗圃" ? "水果已放回刷新栏" : "水果已移动到新槽位";
                return true;
            }
            if (plant.Weapon != WeaponKind.None) State.Inventory.Add(plant.Weapon, 1);
            target.Star++;
            State.Plants.Remove(plant);
            reason = GameConfig.Plant(target.Kind).Name + "升至 " + target.Star + " 星";
            return true;
        }

        public bool InstallWeapon(int plantId, WeaponKind weapon, out string reason)
        {
            var plant = PlantById(plantId);
            var status = GetWeaponInstallStatus(plantId, weapon);
            if (plant == null || !status.Legal) { reason = status.Reason; return false; }
            State.Inventory.Add(weapon, -1);
            plant.Weapon = weapon;
            reason = GameConfig.WeaponName(weapon) + "安装成功";
            var point = plant.PotId >= 0 ? PotPoint(PotById(plant.PotId)) : Map.Core;
            AddFeedback(reason, point, new Color(.25f, .5f, .85f));
            return true;
        }

        public InteractionStatus GetWeaponInstallStatus(int plantId, WeaponKind weapon)
        {
            var plant = PlantById(plantId);
            if (plant == null) return new InteractionStatus(false, "找不到这株植物");
            if (weapon == WeaponKind.None || State.Inventory.Get(weapon) <= 0) return new InteractionStatus(false, "武器库存不足");
            if (plant.Weapon != WeaponKind.None) return new InteractionStatus(false, "这株植物已经装备武器");
            if (weapon == WeaponKind.Gatling && (plant.Kind == PlantKind.Durian || plant.Kind == PlantKind.Sunflower))
                return new InteractionStatus(false, "机枪只能安装到远程水果");
            return new InteractionStatus(true, "可安装" + GameConfig.WeaponName(weapon));
        }

        public bool CanExpand(Vector2Int cell)
        {
            if (State.Inventory.Pots <= 0 || !Map.IsPlantable(cell)) return false;
            if (State.Pots.Any(pot => pot.Active && pot.Cell == cell)) return false;
            return State.Pots.Any(pot => pot.Active && Map.Topology.AreCardinalNeighbors(pot.Cell, cell));
        }

        public bool ExpandPot(Vector2Int cell, out string reason)
        {
            if (!CanExpand(cell)) { reason = "只能扩建到现有花盆的上下左右"; return false; }
            State.Inventory.Pots--;
            AddPot(cell);
            reason = "花盆扩建完成";
            AddFeedback(reason, Map.CellToMap(cell), new Color(.25f, .6f, .25f));
            return true;
        }

        public void Tick(float unscaledDelta)
        {
            AdvanceFrame(unscaledDelta);
        }

        public int AdvanceFrame(float unscaledDelta)
        {
            if (State.Paused || State.Phase == GamePhase.Victory || State.Phase == GamePhase.Defeat)
            {
                ResetFrameAccumulator();
                return 0;
            }

            var safeDelta = float.IsNaN(unscaledDelta) || float.IsInfinity(unscaledDelta)
                ? 0f
                : Mathf.Clamp(unscaledDelta, 0f, MaxFrameDeltaSeconds);
            var scaledDelta = safeDelta * Mathf.Clamp(State.Speed, 1, 2);
            _frameAccumulator = Math.Min(MaxFrameDeltaSeconds, _frameAccumulator + scaledDelta);

            var steps = 0;
            while (steps < MaxStepsPerFrame && _frameAccumulator + AccumulatorEpsilon >= FixedStepSeconds)
            {
                if (!Step())
                {
                    ResetFrameAccumulator();
                    break;
                }

                _frameAccumulator -= FixedStepSeconds;
                if (_frameAccumulator < AccumulatorEpsilon) _frameAccumulator = 0d;
                steps++;
            }
            return steps;
        }

        public bool Step()
        {
            if (State.Paused || State.Phase == GamePhase.Victory || State.Phase == GamePhase.Defeat) return false;
            const float delta = FixedStepSeconds;
            State.Elapsed += delta;
            FadeVisuals(delta);
            foreach (var plant in State.Plants) plant.MoveCooldown = Mathf.Max(0f, plant.MoveCooldown - delta);
            if (State.Phase == GamePhase.BetweenWaves)
            {
                State.BetweenTimer = Mathf.Max(0f, State.BetweenTimer - delta);
                if (State.BetweenTimer <= 0f) StartWave(out _);
                return true;
            }
            if (State.Phase != GamePhase.Playing) return true;
            Spawn(delta);
            AdvanceZombies(delta);
            RunPlants(delta);
            AdvanceProjectiles(delta);
            SettleWave();
            return true;
        }

        public void ResetFrameAccumulator()
        {
            _frameAccumulator = 0d;
        }

        public void RestoreRandomState(uint state)
        {
            _random.RestoreState(state);
        }

        private void Spawn(float delta)
        {
            if (State.WaveSpawned >= State.WaveTotal) return;
            var firstSpawnOfWave = State.WaveSpawned == 0;
            var wave = GameConfig.GetWave(State.WaveIndex);
            State.SpawnCooldown -= delta;
            while (State.WaveSpawned < State.WaveTotal && State.SpawnCooldown <= 0f)
            {
                var kind = wave.Sequence[State.WaveSpawned];
                var stats = GameConfig.Zombie(kind);
                var hp = Mathf.Round(stats.Hp * wave.HpMultiplier);
                State.Zombies.Add(new Zombie
                {
                    Id = State.NextId++, Kind = kind, Hp = hp, MaxHp = hp,
                    Speed = stats.Speed * wave.SpeedMultiplier, Reward = stats.Reward, Threat = stats.Threat,
                });
                State.WaveSpawned++;
                State.SpawnCooldown += wave.SpawnInterval;
            }
            if (!firstSpawnOfWave || State.WaveSpawned <= 0) return;
            var iceSunflowers = State.Plants.Where(plant => plant.Kind == PlantKind.Sunflower
                && plant.Weapon == WeaponKind.Ice && plant.PotId >= 0).ToArray();
            if (iceSunflowers.Length == 0) return;
            foreach (var plant in iceSunflowers) BeginPlantAction(plant, .55f);
            foreach (var zombie in State.Zombies)
            {
                zombie.SlowUntil = Mathf.Max(zombie.SlowUntil, State.Elapsed + 2f);
                AddCombatEffect(CombatEffectKind.IceImpact, Map.Route.Sample(zombie.PathProgress), .45f);
            }
            AddGlobalFeedback("冰霜开场：全场减速", new Color(.35f, .75f, 1f));
        }

        private void AdvanceZombies(float delta)
        {
            for (var index = State.Zombies.Count - 1; index >= 0; index--)
            {
                var zombie = State.Zombies[index];
                var burnDamage = 0f;
                for (var burnIndex = zombie.Burns.Count - 1; burnIndex >= 0; burnIndex--)
                {
                    var burn = zombie.Burns[burnIndex];
                    burnDamage += burn.DamagePerSecond * Mathf.Min(delta, burn.Remaining);
                    burn.Remaining -= delta;
                    if (burn.Remaining <= 0f) zombie.Burns.RemoveAt(burnIndex);
                }
                zombie.Hp -= burnDamage;
                if (zombie.Hp <= 0f) { KillZombie(index, zombie); continue; }
                var frozen = zombie.FreezeUntil > State.Elapsed;
                var stunned = zombie.HitStunUntil > State.Elapsed;
                var slowed = zombie.SlowUntil > State.Elapsed;
                if (!frozen && !stunned) zombie.PathProgress += zombie.Speed * (slowed ? .55f : 1f) * delta;
                if (zombie.PathProgress < Map.Route.TotalLength) continue;
                State.Lives -= zombie.Threat;
                State.Zombies.RemoveAt(index);
                AddGlobalFeedback("果园受损 -" + zombie.Threat, new Color(.8f, .2f, .15f));
                if (State.Lives <= 0) { State.Lives = 0; State.Phase = GamePhase.Defeat; }
            }
        }

        private void RunPlants(float delta)
        {
            foreach (var plant in State.Plants)
            {
                if (plant.PotId < 0) continue;
                var pot = PotById(plant.PotId);
                if (pot == null) continue;
                var potPoint = PotPoint(pot);
                if (plant.Kind == PlantKind.Sunflower)
                {
                    plant.ProductionProgress += delta;
                    if (plant.ProductionProgress >= 10f)
                    {
                        plant.ProductionProgress -= 10f;
                        var amount = plant.Weapon == WeaponKind.Chili ? 2 : 1;
                        State.Sun += amount;
                        BeginPlantAction(plant, .55f);
                        AddCombatEffect(CombatEffectKind.SunBurst, potPoint, .65f);
                        if (plant.Weapon == WeaponKind.Chili)
                            AddCombatEffect(CombatEffectKind.ChiliImpact, potPoint, .45f);
                        AddFeedback("+" + amount + " 阳光", potPoint, new Color(.95f, .68f, .1f));
                    }
                    continue;
                }
                plant.AttackCooldown = Mathf.Max(0f, plant.AttackCooldown - delta);
                plant.BurstShotCooldown = Mathf.Max(0f, plant.BurstShotCooldown - delta);
                if (plant.Weapon == WeaponKind.Gatling && plant.BurstShotsRemaining > 0)
                {
                    if (plant.BurstShotCooldown > 0f || plant.MoveCooldown > 0f) continue;
                    var burstRange = GameConfig.Plant(plant.Kind).Range * GameConfig.StarRange(plant.Star);
                    var burstTarget = SelectTarget(potPoint, burstRange);
                    if (burstTarget != null)
                        SpawnProjectile(plant, potPoint, burstTarget,
                            GameConfig.Plant(plant.Kind).Damage * GameConfig.StarDamage(plant.Star), burstRange);
                    plant.BurstShotsRemaining--;
                    plant.BurstShotCooldown = plant.BurstShotsRemaining > 0 ? GatlingBurstInterval : 0f;
                    continue;
                }
                if (plant.AttackCooldown > 0f || plant.MoveCooldown > 0f) continue;
                var stats = GameConfig.Plant(plant.Kind);
                var range = stats.Range * GameConfig.StarRange(plant.Star);
                var target = SelectTarget(potPoint, range);
                if (target == null) continue;
                var damage = stats.Damage * GameConfig.StarDamage(plant.Star);
                var facing = Map.Route.Sample(target.PathProgress) - potPoint;
                if (facing.sqrMagnitude > .001f) plant.Facing = facing.normalized;
                if (plant.Kind == PlantKind.Durian)
                {
                    BeginPlantAction(plant, .7f);
                    AddCombatEffect(CombatEffectKind.DurianDrop, potPoint, .7f);
                    foreach (var zombie in State.Zombies.ToArray())
                        if (zombie.Hp > 0f && Vector2.Distance(potPoint, Map.Route.Sample(zombie.PathProgress)) <= range)
                            Damage(plant, zombie, damage, plant.Weapon);
                    AddFeedback("重击！", potPoint, new Color(.95f, .68f, .12f));
                }
                else
                {
                    SpawnProjectile(plant, potPoint, target, damage, range);
                }
                plant.AttackCooldown = stats.Interval / GameConfig.StarSpeed(plant.Star);
                if (plant.Weapon == WeaponKind.Gatling)
                {
                    plant.BurstShotsRemaining = GatlingBurstShots - 1;
                    plant.BurstShotCooldown = GatlingBurstInterval;
                }
            }
        }

        private Zombie SelectTarget(Vector2 origin, float range)
        {
            return State.Zombies
                .Where(zombie => zombie.Hp > 0f && Vector2.Distance(origin, Map.Route.Sample(zombie.PathProgress)) <= range)
                .OrderByDescending(zombie => zombie.PathProgress)
                .ThenBy(zombie => zombie.Hp)
                .ThenBy(zombie => zombie.Id)
                .FirstOrDefault();
        }

        private void SpawnProjectile(Plant plant, Vector2 origin, Zombie target, float damage, float range)
        {
            var targetPoint = Map.Route.Sample(target.PathProgress);
            var distance = Mathf.Max(.001f, Vector2.Distance(origin, targetPoint));
            var direction = (targetPoint - origin) / distance;
            plant.Facing = direction;
            BeginPlantAction(plant, plant.Kind == PlantKind.Watermelon ? .32f : .22f);
            if (plant.Weapon == WeaponKind.Gatling)
                AddCombatEffect(CombatEffectKind.GatlingMuzzle, origin + direction * GameConfig.MapDistance(2.4f), .22f);
            State.Projectiles.Add(new ProjectileFlash
            {
                Id = State.NextId++,
                PlantId = plant.Id,
                TargetId = plant.Kind == PlantKind.Watermelon ? -1 : target.Id,
                Kind = plant.Kind,
                Weapon = plant.Weapon,
                Origin = origin,
                Position = origin,
                TargetPoint = plant.Kind == PlantKind.Banana ? origin : targetPoint,
                Direction = direction,
                MaxDistance = plant.Kind == PlantKind.Banana ? range * BananaRangeMultiplier : distance,
                Damage = damage,
                Ttl = plant.Kind == PlantKind.Watermelon ? WatermelonFlightSeconds
                    : plant.Kind == PlantKind.Banana ? range * BananaRangeMultiplier * 2f / BananaSpeed + .3f
                    : 3f,
            });
        }

        private void AdvanceProjectiles(float delta)
        {
            for (var index = State.Projectiles.Count - 1; index >= 0; index--)
            {
                var projectile = State.Projectiles[index];
                projectile.Ttl -= delta;
                if (projectile.Kind == PlantKind.Pea)
                {
                    var target = State.Zombies.FirstOrDefault(zombie => zombie.Id == projectile.TargetId && zombie.Hp > 0f)
                        ?? State.Zombies.Where(zombie => zombie.Hp > 0f)
                            .OrderBy(zombie => Vector2.Distance(projectile.Position, Map.Route.Sample(zombie.PathProgress)))
                            .ThenByDescending(zombie => zombie.PathProgress)
                            .FirstOrDefault();
                    var targetPoint = target == null ? projectile.TargetPoint : Map.Route.Sample(target.PathProgress);
                    var gap = Vector2.Distance(projectile.Position, targetPoint);
                    var travel = ProjectileSpeed * delta;
                    if (gap <= travel + ZombieHitRadius)
                    {
                        if (target != null)
                        {
                            Damage(PlantById(projectile.PlantId), target, projectile.Damage, projectile.Weapon);
                            AddCombatEffect(CombatEffectKind.PeaImpact, targetPoint, .3f);
                        }
                        State.Projectiles.RemoveAt(index);
                        continue;
                    }
                    if (gap > .001f) projectile.Position += (targetPoint - projectile.Position) * (travel / gap);
                    projectile.TargetId = target == null ? -1 : target.Id;
                    projectile.TargetPoint = targetPoint;
                }
                else if (projectile.Kind == PlantKind.Watermelon)
                {
                    projectile.Progress = Mathf.Clamp01(projectile.Progress + delta / WatermelonFlightSeconds);
                    projectile.Position = SampleWatermelonArc(projectile.Origin, projectile.TargetPoint, projectile.Progress);
                    if (projectile.Progress >= 1f || projectile.Ttl <= 0f)
                    {
                        AddCombatEffect(CombatEffectKind.WatermelonBlast, projectile.TargetPoint, .65f);
                        foreach (var zombie in State.Zombies.ToArray())
                            if (zombie.Hp > 0f && Vector2.Distance(projectile.TargetPoint, Map.Route.Sample(zombie.PathProgress)) <= WatermelonBlastRadius)
                                Damage(PlantById(projectile.PlantId), zombie, projectile.Damage, projectile.Weapon);
                        State.Projectiles.RemoveAt(index);
                        continue;
                    }
                }
                else
                {
                    var previous = projectile.Position;
                    if (projectile.Returning)
                        projectile.Progress = Mathf.Max(0f, projectile.Progress - BananaSpeed * delta);
                    else
                    {
                        projectile.Progress = Mathf.Min(projectile.MaxDistance, projectile.Progress + BananaSpeed * delta);
                        if (projectile.Progress >= projectile.MaxDistance) projectile.Returning = true;
                    }
                    projectile.Position = projectile.Origin + projectile.Direction * projectile.Progress;
                    foreach (var zombie in State.Zombies.ToArray())
                    {
                        if (zombie.Hp <= 0f) continue;
                        var hitCount = projectile.HitIds.Count(id => id == zombie.Id);
                        var canHit = projectile.Returning ? hitCount < 2 : hitCount == 0;
                        if (!canHit || PointToSegmentDistance(Map.Route.Sample(zombie.PathProgress), previous, projectile.Position) > ZombieHitRadius) continue;
                        Damage(PlantById(projectile.PlantId), zombie, projectile.Damage, projectile.Weapon);
                        AddCombatEffect(CombatEffectKind.HitSpark, Map.Route.Sample(zombie.PathProgress), .24f);
                        if (projectile.Returning)
                            while (projectile.HitIds.Count(id => id == zombie.Id) < 2) projectile.HitIds.Add(zombie.Id);
                        else projectile.HitIds.Add(zombie.Id);
                    }
                    if (projectile.Returning && projectile.Progress <= 0f)
                    {
                        State.Projectiles.RemoveAt(index);
                        continue;
                    }
                }
                if (projectile.Ttl <= 0f) State.Projectiles.RemoveAt(index);
            }
        }

        private static Vector2 SampleWatermelonArc(Vector2 origin, Vector2 target, float progress)
        {
            var ratio = Mathf.Clamp01(progress);
            var arcHeight = Mathf.Min(GameConfig.MapDistance(18f),
                Mathf.Max(GameConfig.MapDistance(8f), Vector2.Distance(origin, target) * .35f));
            return Vector2.Lerp(origin, target, ratio) + Vector2.up * (-arcHeight * 4f * ratio * (1f - ratio));
        }

        private static float PointToSegmentDistance(Vector2 point, Vector2 from, Vector2 to)
        {
            var segment = to - from;
            if (segment.sqrMagnitude <= .0001f) return Vector2.Distance(point, from);
            var ratio = Mathf.Clamp01(Vector2.Dot(point - from, segment) / segment.sqrMagnitude);
            return Vector2.Distance(point, from + segment * ratio);
        }

        private void BeginPlantAction(Plant plant, float duration)
        {
            plant.ActionStartedAt = State.Elapsed;
            plant.ActionUntil = State.Elapsed + duration;
        }

        private void Damage(Plant plant, Zombie zombie, float damage, WeaponKind sourceWeapon)
        {
            if (zombie == null || zombie.Hp <= 0f) return;
            zombie.Hp = Mathf.Max(0f, zombie.Hp - damage);
            zombie.HitStunUntil = Mathf.Max(zombie.HitStunUntil, State.Elapsed + HitStunSeconds);
            var impactPoint = Map.Route.Sample(zombie.PathProgress);
            var weapon = plant == null ? sourceWeapon : plant.Weapon;
            if (weapon == WeaponKind.Ice)
            {
                zombie.SlowUntil = Mathf.Max(zombie.SlowUntil, State.Elapsed + 2f);
                zombie.IceHits++;
                if (zombie.IceHits >= 5) { zombie.IceHits = 0; zombie.FreezeUntil = Mathf.Max(zombie.FreezeUntil, State.Elapsed + 1f); }
                AddCombatEffect(CombatEffectKind.IceImpact, impactPoint, .36f);
            }
            else if (weapon == WeaponKind.Chili)
            {
                zombie.Burns.Add(new BurnStack { Remaining = 3f, DamagePerSecond = damage * .2f });
                while (zombie.Burns.Count > 3) zombie.Burns.RemoveAt(0);
                AddCombatEffect(CombatEffectKind.ChiliImpact, impactPoint, .38f);
            }
            AddFeedback("-" + Mathf.RoundToInt(damage), impactPoint, new Color(.88f, .25f, .18f));
            if (zombie.Hp <= 0f)
            {
                var index = State.Zombies.IndexOf(zombie);
                if (index >= 0) KillZombie(index, zombie);
            }
        }

        private void KillZombie(int index, Zombie zombie)
        {
            State.Sun += zombie.Reward;
            AddFeedback("击杀 +" + zombie.Reward, Map.Route.Sample(zombie.PathProgress), new Color(.95f, .68f, .1f));
            State.Zombies.RemoveAt(index);
        }

        private void SettleWave()
        {
            if (State.Phase != GamePhase.Playing || State.WaveSpawned < State.WaveTotal || State.Zombies.Count > 0) return;
            var completed = State.WaveIndex;
            var wave = GameConfig.GetWave(completed);
            State.Sun += wave.Reward;
            GrantMilestone(completed);
            AddGlobalFeedback("第 " + completed + " 波完成，奖励 +" + wave.Reward, new Color(.95f, .65f, .1f));
            if (completed >= GameConfig.MaxWaves) State.Phase = GamePhase.Victory;
            else { State.Phase = GamePhase.BetweenWaves; State.BetweenTimer = GameConfig.BetweenWaveSeconds; }
        }

        private void GrantMilestone(int wave)
        {
            if (wave == 3) State.Inventory.Gatling++;
            else if (wave == 6) State.Inventory.Ice++;
            else if (wave == 9) State.Inventory.Chili++;
            else if (wave == 12) { State.Inventory.Gatling++; State.Inventory.Ice++; State.Inventory.Chili++; }
            else return;
            State.Inventory.Pots++;
            AddGlobalFeedback("里程碑奖励：武器与花盆", new Color(.2f, .5f, .78f));
        }

        private void FadeVisuals(float delta)
        {
            for (var index = State.CombatEffects.Count - 1; index >= 0; index--)
            {
                State.CombatEffects[index].Ttl -= delta;
                if (State.CombatEffects[index].Ttl <= 0f) State.CombatEffects.RemoveAt(index);
            }
            for (var index = State.Feedback.Count - 1; index >= 0; index--)
            {
                State.Feedback[index].Ttl -= delta;
                if (State.Feedback[index].Ttl <= 0f) State.Feedback.RemoveAt(index);
            }
        }

        private void AddGlobalFeedback(string text, Color color) { AddFeedback(text, Map.Core, color); }
        private void AddCombatEffect(CombatEffectKind kind, Vector2 point, float duration)
        {
            State.CombatEffects.Add(new CombatEffect { Kind = kind, Position = point, Ttl = duration, Duration = duration });
        }
        private void AddFeedback(string text, Vector2 point, Color color)
        {
            State.Feedback.Add(new FloatingText { Text = text, Point = point, Color = color, Ttl = 1.8f });
        }
    }
}
