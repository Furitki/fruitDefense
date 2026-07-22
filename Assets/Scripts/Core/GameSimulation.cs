using System;
using System.Collections.Generic;
using System.Linq;
using FruitDefense.Content;
using UnityEngine;

namespace FruitDefense.Core
{
    public sealed partial class GameSimulation
    {
        public const float FixedStepSeconds = .05f;
        public const float MaxFrameDeltaSeconds = .25f;
        public const int MaxStepsPerFrame = 5;
        private const double AccumulatorEpsilon = 0.0000001;
        private const int MaxPassiveActivationsPerRootEvent = 128;
        private readonly DeterministicRandom _random;
        private readonly CompiledBattleContentCatalog _content;
        private readonly IReadOnlyList<WaveDefinitionDto> _orderedWaves;
        private readonly LevelRuleSetDefinition _ruleSet;
        private readonly List<int> _lastNurseryPotSlots = new List<int>();
        private readonly BattlePresentationEventStream _presentationEvents = new BattlePresentationEventStream();
        private readonly HashSet<string> _passiveActivationKeys = new HashSet<string>(StringComparer.Ordinal);
        private double _frameAccumulator;
        private int _passiveDispatchDepth;
        private int _passiveActivationCount;
        private long _passiveRootEventSequence;
        public GameState State { get; private set; }
        public BattlefieldMapDefinition Map { get; private set; }
        public ResolvedLevelDefinition ActiveLevel { get; private set; }
        public LevelCompositeIdentity Identity { get; private set; }
        public IReadOnlyList<WaveDefinitionDto> OrderedWaves { get { return _orderedWaves; } }
        public LevelRuleSetDefinition RuleSet { get { return _ruleSet; } }
        public LevelPresentationThemeDefinition Theme
        {
            get { return ActiveLevel == null ? null : ActiveLevel.Theme; }
        }
        public int InitialSun { get { return _ruleSet.InitialSun; } }
        public int InitialLives { get { return _ruleSet.InitialLives; } }
        public int InitialPotCount { get { return _ruleSet.InitialPotCount; } }
        public int MaxWaves { get { return _ruleSet.MaxWaves; } }
        public float BetweenWaveSeconds { get { return _ruleSet.BetweenWaveSeconds; } }
        public int NurserySlotCount { get { return _ruleSet.NurserySlotCount; } }
        public float NurseryPotChance { get { return _ruleSet.NurseryPotChance; } }
        public IReadOnlyList<int> LastNurseryPotSlots { get { return _lastNurseryPotSlots; } }
        public double FrameAccumulatorSeconds { get { return _frameAccumulator; } }
        public uint RandomState { get { return _random.State; } }
        public CompiledBattleContentCatalog Content { get { return _content; } }
        public int PendingPresentationEventCount { get { return _presentationEvents.PendingCount; } }
        public long DroppedPresentationEventCount { get { return _presentationEvents.DroppedCount; } }

        public GameSimulation(int seed = 0, BattlefieldMapDefinition map = null)
            : this(CreateBundledContent(), seed, map)
        {
        }

        public GameSimulation(CompiledBattleContentCatalog content, int seed = 0, BattlefieldMapDefinition map = null)
            : this(content, seed, map, null, CreateLegacyWaveOrder(content),
                CreateLegacyRuleSet(content))
        {
        }

        public GameSimulation(ResolvedLevelDefinition resolvedLevel, int seed = 0)
            : this(RequireResolvedLevel(resolvedLevel).BattleContent, seed, resolvedLevel.Map,
                resolvedLevel, resolvedLevel.OrderedWaves, resolvedLevel.RuleSet)
        {
        }

        private GameSimulation(CompiledBattleContentCatalog content, int seed,
            BattlefieldMapDefinition map, ResolvedLevelDefinition resolvedLevel,
            IEnumerable<WaveDefinitionDto> orderedWaves, LevelRuleSetDefinition ruleSet)
        {
            _content = content ?? throw new ArgumentNullException(nameof(content));
            Map = map ?? GameConfig.DefaultBattlefield;
            ActiveLevel = resolvedLevel;
            Identity = resolvedLevel == null ? null : resolvedLevel.Identity;
            _ruleSet = ruleSet ?? throw new ArgumentNullException(nameof(ruleSet));
            _orderedWaves = Array.AsReadOnly((orderedWaves ?? throw new ArgumentNullException(nameof(orderedWaves)))
                .Select(CloneWave).ToArray());
            MapId = resolvedLevel == null
                ? ResolveMapIdentity(Map, map == null || ReferenceEquals(Map, GameConfig.DefaultBattlefield))
                : resolvedLevel.Identity.MapId;
            string mapReason;
            if (!Map.Validate(out mapReason)) throw new InvalidOperationException("Invalid battlefield map: " + mapReason);
            if (_orderedWaves.Count != _ruleSet.MaxWaves)
                throw new InvalidOperationException("Resolved wave count does not match the active rule set.");
            if (resolvedLevel != null)
            {
                if (!string.Equals(Map.MapId, resolvedLevel.Identity.MapId, StringComparison.Ordinal)
                    || !string.Equals(resolvedLevel.WaveSet.WaveSetId,
                        resolvedLevel.Identity.WaveSetId, StringComparison.Ordinal)
                    || !string.Equals(_ruleSet.RuleSetId,
                        resolvedLevel.Identity.RuleSetId, StringComparison.Ordinal)
                    || resolvedLevel.Theme == null
                    || !string.Equals(resolvedLevel.Theme.ThemeId,
                        resolvedLevel.Identity.ThemeId, StringComparison.Ordinal))
                    throw new InvalidOperationException("Resolved level bundle identity is inconsistent.");
            }
            _random = new DeterministicRandom(seed);
            Reset(seed);
        }

        private static ResolvedLevelDefinition RequireResolvedLevel(ResolvedLevelDefinition resolvedLevel)
        {
            if (resolvedLevel == null) throw new ArgumentNullException(nameof(resolvedLevel));
            return resolvedLevel;
        }

        private static LevelRuleSetDefinition CreateLegacyRuleSet(CompiledBattleContentCatalog content)
        {
            if (content == null) throw new ArgumentNullException(nameof(content));
            return new LevelRuleSetDefinition(content.BattleRules);
        }

        private static IReadOnlyList<WaveDefinitionDto> CreateLegacyWaveOrder(
            CompiledBattleContentCatalog content)
        {
            if (content == null) throw new ArgumentNullException(nameof(content));
            return Enumerable.Range(1, content.BattleRules.maxWaves)
                .Select(index => content.Waves["wave." + index.ToString("00")])
                .ToArray();
        }

        private static WaveDefinitionDto CloneWave(WaveDefinitionDto source)
        {
            if (source == null) throw new InvalidOperationException("Active wave set contains a null wave.");
            return new WaveDefinitionDto
            {
                id = source.id,
                index = source.index,
                healthMultiplier = source.healthMultiplier,
                speedMultiplier = source.speedMultiplier,
                spawnIntervalSeconds = source.spawnIntervalSeconds,
                completionReward = source.completionReward,
                enemyIds = (source.enemyIds ?? Array.Empty<string>()).ToArray(),
            };
        }

        private static CompiledBattleContentCatalog CreateBundledContent()
        {
            CompiledBattleContentCatalog compiled;
            ContentValidationResult validation;
            if (BattleContentCompiler.TryCompile(BundledBattleContentFactory.Create(), out compiled, out validation)) return compiled;
            throw new InvalidOperationException("Bundled battle content is invalid: "
                + string.Join("\n", validation.Issues.Select(issue => issue.ToString()).ToArray()));
        }

        public void Reset(int seed = 0)
        {
            _random.Reset(seed);
            ResetFrameAccumulator();
            _presentationEvents.Reset();
            _passiveActivationKeys.Clear();
            _passiveDispatchDepth = 0;
            _passiveActivationCount = 0;
            _passiveRootEventSequence = 0;
            State = new GameState
            {
                Phase = GamePhase.Ready,
                Sun = _ruleSet.InitialSun,
                Lives = _ruleSet.InitialLives,
                RandomSeed = seed,
            };
            _lastNurseryPotSlots.Clear();
            AddInitialPots();
            AddGlobalFeedback("准备守护果园", new Color(.27f, .48f, .18f));
        }

        private void AddInitialPots()
        {
            var remaining = ActiveLevel == null ? int.MaxValue : _ruleSet.InitialPotCount;
            foreach (var groupName in Map.InitialPotGroupOrder)
            {
                var group = Map.InitialPotGroups[groupName];
                var cells = group.Cells.ToList();
                Shuffle(cells);
                var count = ActiveLevel == null
                    ? group.InitialCount
                    : Mathf.Min(group.InitialCount, remaining);
                for (var index = 0; index < count; index++) AddPot(cells[index]);
                if (ActiveLevel != null) remaining -= count;
            }
            if (ActiveLevel != null && remaining != 0)
                throw new InvalidOperationException("Map initial pot groups do not satisfy the active rule set.");
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
            var cost = RefreshCost(State.RefreshCount);
            if (State.Sun < cost) { reason = "阳光不足"; return false; }

            var overwritten = State.Plants.Where(plant => plant.NurseryIndex >= 0).ToList();
            foreach (var plant in overwritten)
                if (plant.Weapon != WeaponKind.None) State.Inventory.Add(plant.Weapon, 1);
            State.Plants.RemoveAll(plant => plant.NurseryIndex >= 0);

            var firstBatch = State.RefreshCount == 0;
            var results = new List<int>();
            var sunflowerCount = 0;
            for (var index = 0; index < _ruleSet.NurserySlotCount; index++)
            {
                var forceAttacker = firstBatch && index < 2;
                if (!forceAttacker && _random.NextUnitDouble() < _ruleSet.NurseryPotChance)
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
            for (var slot = 0; slot < _ruleSet.NurserySlotCount; slot++)
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
            var plantCount = _ruleSet.NurserySlotCount - _lastNurseryPotSlots.Count;
            reason = _lastNurseryPotSlots.Count > 0
                ? "刷新完成：水果 " + plantCount + " 株，花盆×" + _lastNurseryPotSlots.Count + " 已入库"
                : "获得 " + plantCount + " 株水果";
            AddGlobalFeedback(reason, new Color(.35f, .58f, .2f));
            return true;
        }

        public int RefreshCost(int refreshCount)
        {
            return _ruleSet.RefreshBaseCost + _ruleSet.RefreshCostStep * Mathf.Max(0, refreshCount);
        }

        public bool StartWave(out string reason)
        {
            if (State.Phase == GamePhase.Playing) { reason = "波次正在进行"; return false; }
            if (State.Phase == GamePhase.Victory || State.Phase == GamePhase.Defeat) { reason = "本局已经结束"; return false; }
            var next = State.WaveIndex + 1;
            if (next > _ruleSet.MaxWaves) { reason = "所有波次已经完成"; return false; }
            var wave = Wave(next);
            State.Phase = GamePhase.Playing;
            State.WaveIndex = next;
            State.WaveSpawned = 0;
            State.WaveTotal = wave.enemyIds.Length;
            State.SpawnCooldown = 0f;
            State.BetweenTimer = 0f;
            if (next == 1) RaisePassiveEvent(BattlePassiveTriggerKind.BattleStarted, null, null, 0f);
            reason = "第 " + next + " 波来袭";
            AddGlobalFeedback(reason, next == _ruleSet.MaxWaves ? new Color(.8f, .2f, .15f) : new Color(.2f, .42f, .7f));
            return true;
        }

        public void TogglePause() { State.Paused = !State.Paused; }
        public void SetSpeed(int speed) { State.Speed = Mathf.Clamp(speed, 1, 2); }

        public Plant PlantAtPot(int potId) { return State.Plants.FirstOrDefault(plant => plant.PotId == potId); }
        public Plant PlantAtNursery(int slot) { return State.Plants.FirstOrDefault(plant => plant.NurseryIndex == slot); }
        public Pot PotById(int id) { return State.Pots.FirstOrDefault(pot => pot.Id == id); }
        public Plant PlantById(int id) { return State.Plants.FirstOrDefault(plant => plant.Id == id); }
        public Zombie ZombieById(int id) { return State.Zombies.FirstOrDefault(zombie => zombie.Id == id); }
        public CombatEntityState EntityById(int id)
        {
            return (CombatEntityState)PlantById(id) ?? ZombieById(id);
        }

        public IReadOnlyList<CombatEntityState> CombatEntities()
        {
            return State.Plants.Cast<CombatEntityState>().Concat(State.Zombies)
                .OrderBy(entity => entity.Id).ToArray();
        }

        public float GetEffectiveAttribute(CombatEntityState entity, CombatAttributeKind attribute, float baseValue)
        {
            return CombatAttributeResolver.Resolve(baseValue, entity, attribute, _content);
        }

        public int RemoveStatuses(int entityId, string definitionId = "",
            CombatStatusPolarity? polarity = null, string tag = "")
        {
            var entity = EntityById(entityId);
            return entity == null ? 0 : CombatStatusRuntime.Remove(entity, _content, definitionId, polarity, tag);
        }
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
            if (slot < 0 || slot >= _ruleSet.NurserySlotCount) return new PlantDropStatus(false, PlantDropAction.Invalid, "这里不是苗圃槽位");
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
            plant.EquipmentId = LegacyBattleContentIds.Equipment(weapon);
            plant.SkillRuntimes.Clear();
            plant.PassiveRuntimes.Clear();
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
            var plantContentId = ResolvePlantId(plant);
            var equipmentId = LegacyBattleContentIds.Equipment(weapon);
            EquipmentDefinitionDto equipment;
            if (!_content.Equipment.TryGetValue(equipmentId, out equipment)
                || !equipment.compatiblePlantIds.Contains(plantContentId))
                return new InteractionStatus(false, "武器与该植物不兼容");
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
            State.LogicTick++;
            State.Elapsed += delta;
            foreach (var plant in State.Plants) plant.MoveCooldown = Mathf.Max(0f, plant.MoveCooldown - delta);
            if (State.Phase == GamePhase.BetweenWaves)
            {
                State.BetweenTimer = Mathf.Max(0f, State.BetweenTimer - delta);
                if (State.BetweenTimer <= 0f) StartWave(out _);
                return true;
            }
            if (State.Phase != GamePhase.Playing) return true;
            AdvanceCombatRuntime();
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

        public int DrainPresentationEvents(ICollection<BattlePresentationEvent> destination)
        {
            return _presentationEvents.DrainTo(destination);
        }

        public void DiscardPendingPresentationEvents()
        {
            _presentationEvents.DiscardPending();
        }

        private void Spawn(float delta)
        {
            if (State.WaveSpawned >= State.WaveTotal) return;
            var firstSpawnOfWave = State.WaveSpawned == 0;
            var wave = Wave(State.WaveIndex);
            State.SpawnCooldown -= delta;
            while (State.WaveSpawned < State.WaveTotal && State.SpawnCooldown <= 0f)
            {
                var enemyId = wave.enemyIds[State.WaveSpawned];
                var definition = _content.Enemies[enemyId];
                var kind = LegacyBattleContentIds.EnemyKindFromId(enemyId);
                var hp = Mathf.Round(definition.health * wave.healthMultiplier);
                State.Zombies.Add(new Zombie
                {
                    Id = State.NextId++, ContentId = enemyId, Kind = kind, Hp = hp, MaxHp = hp,
                    Speed = Map.FromLegacyDistance(definition.speedLegacyUnits) * wave.speedMultiplier,
                    Reward = definition.killReward, Threat = definition.threat,
                });
                State.WaveSpawned++;
                State.SpawnCooldown += wave.spawnIntervalSeconds;
            }
            if (!firstSpawnOfWave || State.WaveSpawned <= 0) return;
            RaisePassiveEvent(BattlePassiveTriggerKind.WaveFirstSpawned, null,
                State.Zombies.OrderBy(value => value.Id).FirstOrDefault(), 0f);
        }

        private void AdvanceZombies(float delta)
        {
            for (var index = State.Zombies.Count - 1; index >= 0; index--)
            {
                var zombie = State.Zombies[index];
                if (zombie.Hp <= 0f) { KillZombie(index, zombie); continue; }
                SyncLegacyStatusViews(zombie);
                var speed = GetEffectiveAttribute(zombie, CombatAttributeKind.MoveSpeed, zombie.Speed);
                if (!CombatStatusRuntime.BlocksMovement(zombie, _content)) zombie.PathProgress += speed * delta;
                if (zombie.PathProgress < Map.Route.TotalLength) continue;
                State.Lives -= zombie.Threat;
                State.Zombies.RemoveAt(index);
                AddGlobalFeedback("果园受损 -" + zombie.Threat, new Color(.8f, .2f, .15f));
                if (State.Lives <= 0) { State.Lives = 0; State.Phase = GamePhase.Defeat; }
            }
        }

        private void RunPlants(float delta)
        {
            foreach (var plant in State.Plants.OrderBy(value => value.Id))
            {
                if (plant.PotId < 0) continue;
                var pot = PotById(plant.PotId);
                if (pot == null) continue;
                var potPoint = PotPoint(pot);
                var range = PlantRange(plant);
                var skills = ResolveSkills(plant);
                EnsureSkillRuntimes(plant, skills);
                foreach (var skill in skills)
                {
                    var runtime = plant.SkillRuntimes.First(value => value.SkillId == skill.Id);
                    if (skill.Trigger == BattleTriggerKind.Periodic)
                    {
                        var legacyTicks = BattleSkillTiming.SecondsToTicks(plant.ProductionProgress);
                        if (legacyTicks > runtime.PeriodicProgressTicks) runtime.PeriodicProgressTicks = legacyTicks;
                        runtime.PeriodicProgressTicks++;
                        if (runtime.PeriodicProgressTicks >= skill.CooldownTicks && plant.MoveCooldown <= 0f)
                        {
                            runtime.PeriodicProgressTicks -= skill.CooldownTicks;
                            ExecuteSkill(plant, skill, null, potPoint, range);
                        }
                        plant.ProductionProgress = BattleSkillTiming.TicksToSeconds(runtime.PeriodicProgressTicks);
                        continue;
                    }
                    if (skill.Trigger != BattleTriggerKind.CooldownReady) continue;
                    var legacyCooldownTicks = BattleSkillTiming.SecondsToTicks(plant.AttackCooldown);
                    if (legacyCooldownTicks > runtime.CooldownTicks) runtime.CooldownTicks = legacyCooldownTicks;
                    if (runtime.CooldownTicks > 0) runtime.CooldownTicks--;
                    if (runtime.BurstIntervalTicks > 0) runtime.BurstIntervalTicks--;
                    if (runtime.BurstShotsRemaining > 0)
                    {
                        if (runtime.BurstIntervalTicks <= 0 && plant.MoveCooldown <= 0f)
                        {
                            var burstTarget = SelectTarget(potPoint, range);
                            if (burstTarget != null) ExecuteSkill(plant, skill, burstTarget, potPoint, range, false);
                            runtime.BurstShotsRemaining--;
                            runtime.BurstIntervalTicks = runtime.BurstShotsRemaining > 0 ? skill.BurstIntervalTicks : 0;
                        }
                        SyncLegacySkillViews(plant, runtime);
                        continue;
                    }
                    if (runtime.CooldownTicks > 0 || plant.MoveCooldown > 0f) { SyncLegacySkillViews(plant, runtime); continue; }
                    var target = SelectTarget(potPoint, range);
                    if (target == null) { SyncLegacySkillViews(plant, runtime); continue; }
                    ExecuteSkill(plant, skill, target, potPoint, range);
                    var interval = BattleSkillTiming.TicksToSeconds(skill.CooldownTicks)
                        / StarTier(plant).attackSpeedMultiplier;
                    runtime.CooldownTicks = Math.Max(1, BattleSkillTiming.SecondsToTicks(
                        GetEffectiveAttribute(plant, CombatAttributeKind.AttackInterval, interval)));
                    runtime.BurstShotsRemaining = Math.Max(0, skill.BurstCount - 1);
                    runtime.BurstIntervalTicks = runtime.BurstShotsRemaining > 0 ? skill.BurstIntervalTicks : 0;
                    SyncLegacySkillViews(plant, runtime);
                }
            }
        }

        private IReadOnlyList<CompiledBattleSkill> ResolveSkills(Plant plant)
        {
            var equipmentId = ResolveEquipmentId(plant);
            return _content.ResolvePlantSkills(ResolvePlantId(plant), equipmentId);
        }

        private void EnsureSkillRuntimes(Plant plant, IReadOnlyList<CompiledBattleSkill> skills)
        {
            var ids = new HashSet<string>(skills.Select(value => value.Id), StringComparer.Ordinal);
            plant.SkillRuntimes.RemoveAll(value => !ids.Contains(value.SkillId));
            foreach (var skill in skills.OrderBy(value => value.Id, StringComparer.Ordinal))
                if (plant.SkillRuntimes.All(value => value.SkillId != skill.Id))
                    plant.SkillRuntimes.Add(new SkillRuntimeState { SkillId = skill.Id });
            plant.SkillRuntimes.Sort((left, right) => StringComparer.Ordinal.Compare(left.SkillId, right.SkillId));
        }

        private IReadOnlyList<CompiledBattlePassive> ResolvePassives(CombatEntityState entity)
        {
            var plant = entity as Plant;
            if (plant != null)
                return _content.ResolvePlantPassives(ResolvePlantId(plant), ResolveEquipmentId(plant));
            var zombie = entity as Zombie;
            if (zombie != null) return _content.ResolveEnemyPassives(ResolveEnemyId(zombie));
            throw new InvalidOperationException("Unsupported combat entity type '" + entity.GetType().Name + "'.");
        }

        private void EnsurePassiveRuntimes(CombatEntityState entity,
            IReadOnlyList<CompiledBattlePassive> passives)
        {
            var ids = new HashSet<string>(passives.Select(value => value.Id), StringComparer.Ordinal);
            entity.PassiveRuntimes.RemoveAll(value => !ids.Contains(value.PassiveId));
            foreach (var passive in passives.OrderBy(value => value.Priority)
                         .ThenBy(value => value.Id, StringComparer.Ordinal))
                if (entity.PassiveRuntimes.All(value => value.PassiveId != passive.Id))
                    entity.PassiveRuntimes.Add(new PassiveRuntimeState { PassiveId = passive.Id });
            entity.PassiveRuntimes.Sort((left, right) =>
                StringComparer.Ordinal.Compare(left.PassiveId, right.PassiveId));
        }

        private void AdvanceCombatRuntime()
        {
            foreach (var entity in CombatEntities())
            {
                var passives = ResolvePassives(entity);
                EnsurePassiveRuntimes(entity, passives);
                foreach (var runtime in entity.PassiveRuntimes)
                    if (runtime.CooldownTicks > 0) runtime.CooldownTicks--;

                foreach (var status in entity.Statuses.OrderBy(value => value.Sequence).ToArray())
                {
                    CompiledStatusDefinition definition;
                    if (!_content.RuntimeStatuses.TryGetValue(status.DefinitionId, out definition))
                    {
                        entity.Statuses.Remove(status);
                        continue;
                    }
                    if (definition.PeriodicEffect == CombatPeriodicEffectKind.Damage && entity is Zombie)
                    {
                        status.TickProgress++;
                        var intervalTicks = Math.Max(1, definition.TickIntervalTicks);
                        while (status.TickProgress >= intervalTicks && entity.IsAlive)
                        {
                            status.TickProgress -= intervalTicks;
                            DealPeriodicDamage((Zombie)entity, status,
                                status.Magnitude * BattleSkillTiming.TicksToSeconds(intervalTicks));
                        }
                    }
                    if (definition.Stacking != BattleStatusStackingKind.ProcAfterHits)
                        status.RemainingTicks--;
                    if (definition.Stacking != BattleStatusStackingKind.ProcAfterHits
                        && status.RemainingTicks <= 0) entity.Statuses.Remove(status);
                    if (!entity.IsAlive) break;
                }
                var zombie = entity as Zombie;
                if (zombie != null && zombie.IsAlive) SyncLegacyStatusViews(zombie);
            }
        }

        private void DealPeriodicDamage(Zombie target, StatusInstance status, float rawDamage)
        {
            if (target == null || target.Hp <= 0f || rawDamage <= 0f) return;
            var source = EntityById(status.SourceEntityId);
            var damage = GetEffectiveAttribute(target, CombatAttributeKind.DamageTaken, rawDamage);
            target.Hp = Mathf.Max(0f, target.Hp - damage);
            AddFeedback("-" + Mathf.RoundToInt(damage), Map.Route.Sample(target.PathProgress),
                new Color(.88f, .25f, .18f));
            RaisePassiveEvent(BattlePassiveTriggerKind.AfterDamageTaken, source, target, damage);
            if (target.Hp <= 0f)
            {
                var index = State.Zombies.IndexOf(target);
                if (index >= 0) KillZombie(index, target, source);
            }
        }

        private void RaisePassiveEvent(BattlePassiveTriggerKind trigger,
            CombatEntityState eventSource, CombatEntityState eventTarget, float eventMagnitude)
        {
            RaisePassiveEvents(new[] { trigger }, eventSource, eventTarget, eventMagnitude);
        }

        private void RaisePassiveEvents(IReadOnlyCollection<BattlePassiveTriggerKind> triggers,
            CombatEntityState eventSource, CombatEntityState eventTarget, float eventMagnitude)
        {
            var isRoot = _passiveDispatchDepth == 0;
            if (isRoot)
            {
                _passiveRootEventSequence = State.NextCombatEventSequence++;
                _passiveActivationCount = 0;
                _passiveActivationKeys.Clear();
            }
            _passiveDispatchDepth++;
            try
            {
                var owners = CombatEntities().Where(entity => IsPassiveOwnerActive(entity)
                    || ReferenceEquals(entity, eventSource) || ReferenceEquals(entity, eventTarget))
                    .OrderBy(entity => entity.Id).ToArray();
                foreach (var owner in owners)
                {
                    var passives = ResolvePassives(owner);
                    EnsurePassiveRuntimes(owner, passives);
                    foreach (var passive in passives.Where(value => triggers.Contains(value.Trigger))
                                 .OrderBy(value => value.Priority)
                                 .ThenBy(value => value.Id, StringComparer.Ordinal))
                    {
                        if (!OwnerRoleMatches(passive.OwnerRole, owner, eventSource, eventTarget)) continue;
                        var runtime = owner.PassiveRuntimes.First(value => value.PassiveId == passive.Id);
                        if (runtime.CooldownTicks > 0) continue;
                        var activationKey = _passiveRootEventSequence + "|" + owner.Id + "|" + passive.Id;
                        if (!_passiveActivationKeys.Add(activationKey)) continue;
                        _passiveActivationCount++;
                        if (_passiveActivationCount > MaxPassiveActivationsPerRootEvent)
                            throw new InvalidOperationException("Passive activation budget exceeded for root event "
                                + _passiveRootEventSequence + ". Check authored passive feedback loops.");
                        runtime.LastRootEventSequence = _passiveRootEventSequence;
                        runtime.CooldownTicks = passive.CooldownTicks;
                        var ownerPlant = owner as Plant;
                        var context = new CombatEffectContext(_passiveRootEventSequence, owner,
                            eventSource, eventTarget, EntityPoint(owner),
                            ownerPlant == null ? 0f : PlantRange(ownerPlant), eventMagnitude);
                        ExecutePassive(passive, context);
                    }
                }
            }
            finally
            {
                _passiveDispatchDepth--;
                if (isRoot)
                {
                    _passiveRootEventSequence = 0;
                    _passiveActivationCount = 0;
                    _passiveActivationKeys.Clear();
                }
            }
        }

        private static bool OwnerRoleMatches(BattlePassiveOwnerRole role, CombatEntityState owner,
            CombatEntityState eventSource, CombatEntityState eventTarget)
        {
            if (role == BattlePassiveOwnerRole.Any) return true;
            if (role == BattlePassiveOwnerRole.EventSource) return ReferenceEquals(owner, eventSource);
            return ReferenceEquals(owner, eventTarget);
        }

        private static bool IsPassiveOwnerActive(CombatEntityState entity)
        {
            var plant = entity as Plant;
            return plant != null ? plant.PotId >= 0 : entity.IsAlive;
        }

        private void ExecutePassive(CompiledBattlePassive passive, CombatEffectContext context)
        {
            var targets = SelectPassiveTargets(passive.Target, context);
            ExecuteEffects(passive.Effects, context, targets, null, passive.Id);
        }

        private void ExecuteEffects(IReadOnlyList<CompiledSkillEffect> effects,
            CombatEffectContext context, IReadOnlyList<CombatEntityState> targets,
            CompiledBattleSkill skill, string actionId)
        {
            foreach (var effect in effects)
            {
                switch (effect.Kind)
                {
                    case BattleEffectKind.Damage:
                        foreach (var target in targets.OfType<Zombie>().ToArray())
                        {
                            if (skill != null)
                            {
                                var damagePlant = context.Owner as Plant;
                                if (damagePlant != null)
                                    Damage(damagePlant, target,
                                        PlantDamage(damagePlant) * skill.DamageMultiplier * effect.Magnitude);
                            }
                            else
                            {
                                var basis = context.EventMagnitude > 0f ? context.EventMagnitude : 1f;
                                DamageEntity(context.Owner, target, basis * effect.Magnitude, false);
                            }
                        }
                        break;
                    case BattleEffectKind.LaunchProjectile:
                        var plant = context.Owner as Plant;
                        var projectileTarget = targets.OfType<Zombie>().FirstOrDefault();
                        if (plant != null && projectileTarget != null)
                        {
                            var bridgeSkill = new CompiledBattleSkill
                            {
                                Id = actionId,
                                BurstCount = 1,
                                DamageMultiplier = 1f,
                            };
                            var projectileSkill = skill ?? bridgeSkill;
                            SpawnProjectile(plant, projectileSkill, effect, context.Origin, projectileTarget,
                                PlantDamage(plant) * projectileSkill.DamageMultiplier * effect.Magnitude,
                                context.Range);
                        }
                        break;
                    case BattleEffectKind.GrantResource:
                        var authoredAmount = skill != null && skill.ResourceAmount > 0
                            ? skill.ResourceAmount : effect.ResourceAmount;
                        var resource = Mathf.RoundToInt(GetEffectiveAttribute(context.Owner,
                            CombatAttributeKind.ResourceGain, authoredAmount));
                        State.Sun += resource;
                        AddFeedback("+" + resource + " 阳光", context.Origin,
                            new Color(.95f, .68f, .1f));
                        break;
                    case BattleEffectKind.ApplyStatus:
                        foreach (var target in targets.ToArray())
                        {
                            var magnitude = effect.Magnitude;
                            CompiledStatusDefinition statusDefinition;
                            if (_content.RuntimeStatuses.TryGetValue(effect.StatusId, out statusDefinition)
                                && statusDefinition.PeriodicEffect == CombatPeriodicEffectKind.Damage
                                && context.EventMagnitude > 0f)
                                magnitude *= context.EventMagnitude;
                            ApplyStatus(target, effect.StatusId, context.Owner.Id, magnitude);
                        }
                        break;
                    case BattleEffectKind.EmitCue:
                        if (targets.Count == 0)
                            EmitCue(effect.CueId, context.Owner.Id, 0, context.Origin);
                        else foreach (var target in targets)
                            EmitCue(effect.CueId, context.Owner.Id, target.Id, EntityPoint(target));
                        break;
                    default:
                        throw new InvalidOperationException("No executor registered for effect "
                            + effect.Kind + ".");
                }
            }
        }

        private List<CombatEntityState> SelectPassiveTargets(BattlePassiveTargetKind targetKind,
            CombatEffectContext context)
        {
            if (targetKind == BattlePassiveTargetKind.Self)
                return new List<CombatEntityState> { context.Owner };
            if (targetKind == BattlePassiveTargetKind.EventSource)
                return context.EventSource == null ? new List<CombatEntityState>()
                    : new List<CombatEntityState> { context.EventSource };
            if (targetKind == BattlePassiveTargetKind.EventTarget)
                return context.EventTarget == null ? new List<CombatEntityState>()
                    : new List<CombatEntityState> { context.EventTarget };
            var allies = targetKind == BattlePassiveTargetKind.AllAllies;
            return CombatEntities().Where(entity => IsPassiveOwnerActive(entity)
                    && (entity.Faction == context.Owner.Faction) == allies)
                .OrderBy(entity => entity.Id).ToList();
        }

        private Vector2 EntityPoint(CombatEntityState entity)
        {
            var plant = entity as Plant;
            if (plant != null) return plant.PotId < 0 ? Map.Core : PlantPoint(plant);
            var zombie = entity as Zombie;
            return zombie == null ? Map.Core : Map.Route.Sample(zombie.PathProgress);
        }

        private void SyncLegacySkillViews(Plant plant, SkillRuntimeState runtime)
        {
            plant.AttackCooldown = BattleSkillTiming.TicksToSeconds(runtime.CooldownTicks);
            plant.BurstShotsRemaining = runtime.BurstShotsRemaining;
            plant.BurstShotCooldown = BattleSkillTiming.TicksToSeconds(runtime.BurstIntervalTicks);
        }

        private void ExecuteSkill(Plant plant, CompiledBattleSkill skill, Zombie eventTarget, Vector2 origin,
            float range, bool startAction = true, float eventDamage = 0f)
        {
            var targets = SelectSkillTargets(skill.Target, eventTarget, origin, range);
            if (startAction && skill.ActionTicks > 0)
                BeginPlantAction(plant, BattleSkillTiming.TicksToSeconds(skill.ActionTicks));
            if (targets.Count > 0)
            {
                var facing = Map.Route.Sample(targets[0].PathProgress) - origin;
                if (facing.sqrMagnitude > .001f) plant.Facing = facing.normalized;
            }
            var context = new CombatEffectContext(0, plant, plant, eventTarget,
                origin, range, eventDamage);
            ExecuteEffects(skill.Effects, context,
                targets.Cast<CombatEntityState>().ToArray(), skill, skill.Id);
        }

        private List<Zombie> SelectSkillTargets(BattleTargetKind targetKind, Zombie eventTarget, Vector2 origin, float range)
        {
            switch (targetKind)
            {
                case BattleTargetKind.Self: return new List<Zombie>();
                case BattleTargetKind.EventTarget:
                case BattleTargetKind.LineFromCaster:
                    return eventTarget == null || eventTarget.Hp <= 0f
                        ? new List<Zombie>() : new List<Zombie> { eventTarget };
                case BattleTargetKind.FrontmostEnemyInRange:
                    var front = SelectTarget(origin, range);
                    return front == null ? new List<Zombie>() : new List<Zombie> { front };
                case BattleTargetKind.AllEnemiesInRadius:
                    return OrderedLivingEnemies().Where(zombie =>
                        Vector2.Distance(origin, Map.Route.Sample(zombie.PathProgress)) <= range).ToList();
                case BattleTargetKind.AllEnemies:
                    return OrderedLivingEnemies().ToList();
                default:
                    throw new InvalidOperationException("No selector registered for target " + targetKind + ".");
            }
        }

        private IEnumerable<Zombie> OrderedLivingEnemies()
        {
            return State.Zombies.Where(zombie => zombie.Hp > 0f)
                .OrderByDescending(zombie => zombie.PathProgress)
                .ThenBy(zombie => zombie.Hp)
                .ThenBy(zombie => zombie.Id);
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

        private void SpawnProjectile(Plant plant, CompiledBattleSkill skill, CompiledSkillEffect effect,
            Vector2 origin, Zombie target, float damage, float range)
        {
            var definition = _content.RuntimeProjectiles[effect.ProjectileId];
            var targetPoint = Map.Route.Sample(target.PathProgress);
            var distance = Mathf.Max(.001f, Vector2.Distance(origin, targetPoint));
            var direction = (targetPoint - origin) / distance;
            plant.Facing = direction;
            if (skill.BurstCount > 1)
                EmitCue(BattleContentIds.Cues.GatlingMuzzle, plant.Id, target.Id,
                    origin + direction * Map.FromLegacyDistance(2.4f));
            var totalTicks = definition.Mode == BattleProjectileMode.TimedArc
                ? definition.FlightTicks
                : definition.Mode == BattleProjectileMode.LinearReturn
                    ? BattleSkillTiming.SecondsToTicks(range * definition.RangeMultiplier * 2f
                        / Map.FromLegacyDistance(definition.Speed) + .3f)
                    : BattleSkillTiming.SecondsToTicks(3f);
            State.Projectiles.Add(new ProjectileFlash
            {
                Id = State.NextId++,
                PlantId = plant.Id,
                TargetId = definition.Mode == BattleProjectileMode.TimedArc ? -1 : target.Id,
                Kind = plant.Kind,
                Weapon = plant.Weapon,
                ProjectileId = definition.Id,
                VisualId = definition.VisualId,
                ImpactCueId = definition.ImpactCueId,
                Mode = definition.Mode,
                Origin = origin,
                Position = origin,
                TargetPoint = definition.Mode == BattleProjectileMode.LinearReturn ? origin : targetPoint,
                Direction = direction,
                MaxDistance = definition.Mode == BattleProjectileMode.LinearReturn ? range * definition.RangeMultiplier : distance,
                Damage = damage,
                TicksRemaining = totalTicks,
                FlightTicks = definition.FlightTicks,
                Ttl = BattleSkillTiming.TicksToSeconds(totalTicks),
            });
        }

        private void AdvanceProjectiles(float delta)
        {
            for (var index = State.Projectiles.Count - 1; index >= 0; index--)
            {
                var projectile = State.Projectiles[index];
                var definition = _content.RuntimeProjectiles[projectile.ProjectileId];
                projectile.TicksRemaining--;
                projectile.Ttl = BattleSkillTiming.TicksToSeconds(projectile.TicksRemaining);
                if (definition.Mode == BattleProjectileMode.Tracking)
                {
                    var target = State.Zombies.FirstOrDefault(zombie => zombie.Id == projectile.TargetId && zombie.Hp > 0f)
                        ?? State.Zombies.Where(zombie => zombie.Hp > 0f)
                            .OrderBy(zombie => Vector2.Distance(projectile.Position, Map.Route.Sample(zombie.PathProgress)))
                            .ThenByDescending(zombie => zombie.PathProgress)
                            .ThenBy(zombie => zombie.Id)
                            .FirstOrDefault();
                    var targetPoint = target == null ? projectile.TargetPoint : Map.Route.Sample(target.PathProgress);
                    var gap = Vector2.Distance(projectile.Position, targetPoint);
                    var travel = Map.FromLegacyDistance(definition.Speed) * delta;
                    if (gap <= travel + Map.FromLegacyDistance(definition.HitRadius))
                    {
                        if (target != null)
                        {
                            Damage(PlantById(projectile.PlantId), target, projectile.Damage);
                            EmitCue(projectile.ImpactCueId, projectile.PlantId, target.Id, targetPoint);
                        }
                        State.Projectiles.RemoveAt(index);
                        continue;
                    }
                    if (gap > .001f) projectile.Position += (targetPoint - projectile.Position) * (travel / gap);
                    projectile.TargetId = target == null ? -1 : target.Id;
                    projectile.TargetPoint = targetPoint;
                }
                else if (definition.Mode == BattleProjectileMode.TimedArc)
                {
                    projectile.Progress = Mathf.Clamp01((float)(projectile.FlightTicks - projectile.TicksRemaining)
                        / Mathf.Max(1, projectile.FlightTicks));
                    projectile.Position = SampleWatermelonArc(projectile.Origin, projectile.TargetPoint, projectile.Progress);
                    if (projectile.Progress >= 1f || projectile.TicksRemaining <= 0)
                    {
                        EmitCue(projectile.ImpactCueId, projectile.PlantId, 0, projectile.TargetPoint);
                        foreach (var zombie in State.Zombies.ToArray())
                            if (zombie.Hp > 0f && Vector2.Distance(projectile.TargetPoint, Map.Route.Sample(zombie.PathProgress))
                                <= Map.FromLegacyDistance(definition.BlastRadius))
                                Damage(PlantById(projectile.PlantId), zombie, projectile.Damage);
                        State.Projectiles.RemoveAt(index);
                        continue;
                    }
                }
                else
                {
                    var previous = projectile.Position;
                    var speed = Map.FromLegacyDistance(definition.Speed);
                    if (projectile.Returning)
                        projectile.Progress = Mathf.Max(0f, projectile.Progress - speed * delta);
                    else
                    {
                        projectile.Progress = Mathf.Min(projectile.MaxDistance, projectile.Progress + speed * delta);
                        if (projectile.Progress >= projectile.MaxDistance) projectile.Returning = true;
                    }
                    projectile.Position = projectile.Origin + projectile.Direction * projectile.Progress;
                    foreach (var zombie in State.Zombies.ToArray())
                    {
                        if (zombie.Hp <= 0f) continue;
                        var hitCount = projectile.HitIds.Count(id => id == zombie.Id);
                        var canHit = projectile.Returning ? hitCount < definition.MaxHitsPerTarget : hitCount == 0;
                        if (!canHit || PointToSegmentDistance(Map.Route.Sample(zombie.PathProgress), previous, projectile.Position)
                            > Map.FromLegacyDistance(definition.HitRadius)) continue;
                        Damage(PlantById(projectile.PlantId), zombie, projectile.Damage);
                        EmitCue(projectile.ImpactCueId, projectile.PlantId, zombie.Id,
                            Map.Route.Sample(zombie.PathProgress));
                        if (projectile.Returning)
                            while (projectile.HitIds.Count(id => id == zombie.Id) < definition.MaxHitsPerTarget)
                                projectile.HitIds.Add(zombie.Id);
                        else projectile.HitIds.Add(zombie.Id);
                    }
                    if (projectile.Returning && projectile.Progress <= 0f)
                    {
                        State.Projectiles.RemoveAt(index);
                        continue;
                    }
                }
                if (projectile.TicksRemaining <= 0) State.Projectiles.RemoveAt(index);
            }
        }

        private Vector2 SampleWatermelonArc(Vector2 origin, Vector2 target, float progress)
        {
            var ratio = Mathf.Clamp01(progress);
            var arcHeight = Mathf.Min(Map.FromLegacyDistance(18f),
                Mathf.Max(Map.FromLegacyDistance(8f), Vector2.Distance(origin, target) * .35f));
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

        private void Damage(Plant plant, Zombie zombie, float damage)
        {
            DamageEntity(plant, zombie, damage, true);
        }

        private void DamageEntity(CombatEntityState source, Zombie zombie, float rawDamage,
            bool applyDirectHitStatus)
        {
            if (zombie == null || zombie.Hp <= 0f) return;
            var damage = GetEffectiveAttribute(zombie, CombatAttributeKind.DamageTaken, rawDamage);
            zombie.Hp = Mathf.Max(0f, zombie.Hp - damage);
            var impactPoint = Map.Route.Sample(zombie.PathProgress);
            if (applyDirectHitStatus)
                ApplyStatus(zombie, BattleContentIds.Statuses.HitStun, source == null ? 0 : source.Id, 1f);
            RaisePassiveEvents(new[]
            {
                BattlePassiveTriggerKind.AfterDamageDealt,
                BattlePassiveTriggerKind.AfterDamageTaken,
            }, source, zombie, damage);
            AddFeedback("-" + Mathf.RoundToInt(damage), impactPoint, new Color(.88f, .25f, .18f));
            SyncLegacyStatusViews(zombie);
            if (zombie.Hp <= 0f)
            {
                var index = State.Zombies.IndexOf(zombie);
                if (index >= 0) KillZombie(index, zombie, source);
            }
        }

        public void ApplyStatus(CombatEntityState target, string statusId, int sourceEntityId,
            float magnitudeMultiplier = 1f)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            var definition = _content.RuntimeStatuses[statusId];
            var magnitude = definition.PeriodicEffect == CombatPeriodicEffectKind.Damage
                ? magnitudeMultiplier
                : definition.Magnitude * magnitudeMultiplier;
            var procStatusId = string.Empty;
            if (definition.Stacking == BattleStatusStackingKind.Refresh)
            {
                var existing = target.Statuses.FirstOrDefault(value => value.DefinitionId == statusId);
                if (existing == null)
                {
                    target.Statuses.Add(CreateStatus(definition, sourceEntityId, magnitude));
                }
                else
                {
                    existing.RemainingTicks = Math.Max(existing.RemainingTicks, definition.DurationTicks);
                    existing.Magnitude = magnitude;
                    existing.SourceEntityId = sourceEntityId;
                }
            }
            else if (definition.Stacking == BattleStatusStackingKind.Independent)
            {
                target.Statuses.Add(CreateStatus(definition, sourceEntityId, magnitude));
                var same = target.Statuses.Where(value => value.DefinitionId == statusId)
                    .OrderBy(value => value.Sequence).ToList();
                while (same.Count > definition.MaxStacks)
                {
                    target.Statuses.Remove(same[0]);
                    same.RemoveAt(0);
                }
            }
            else if (definition.Stacking == BattleStatusStackingKind.Additive)
            {
                var existing = target.Statuses.FirstOrDefault(value => value.DefinitionId == statusId);
                if (existing == null) target.Statuses.Add(CreateStatus(definition, sourceEntityId, magnitude));
                else
                {
                    existing.StackCount = Math.Min(definition.MaxStacks, existing.StackCount + 1);
                    existing.RemainingTicks = Math.Max(existing.RemainingTicks, definition.DurationTicks);
                    existing.Magnitude = magnitude;
                    existing.SourceEntityId = sourceEntityId;
                }
            }
            else
            {
                var counter = target.Statuses.FirstOrDefault(value => value.DefinitionId == statusId);
                if (counter == null)
                {
                    counter = CreateStatus(definition, sourceEntityId, magnitude);
                    counter.StackCount = 0;
                    target.Statuses.Add(counter);
                }
                counter.StackCount++;
                counter.SourceEntityId = sourceEntityId;
                if (counter.StackCount >= definition.HitsToProc)
                {
                    target.Statuses.Remove(counter);
                    procStatusId = definition.ProcStatusId;
                }
            }
            var zombie = target as Zombie;
            if (zombie != null) SyncLegacyStatusViews(zombie);
            RaisePassiveEvent(BattlePassiveTriggerKind.StatusApplied,
                EntityById(sourceEntityId), target, magnitude);
            if (!string.IsNullOrEmpty(procStatusId)) ApplyStatus(target, procStatusId, sourceEntityId, 1f);
        }

        private StatusInstance CreateStatus(CompiledStatusDefinition definition, int sourceEntityId, float magnitude)
        {
            return new StatusInstance
            {
                DefinitionId = definition.Id,
                SourceEntityId = sourceEntityId,
                RemainingTicks = definition.DurationTicks,
                StackCount = 1,
                Magnitude = magnitude,
                Sequence = State.NextStatusSequence++,
            };
        }

        private void SyncLegacyStatusViews(Zombie zombie)
        {
            zombie.SlowUntil = State.Elapsed;
            zombie.FreezeUntil = State.Elapsed;
            zombie.HitStunUntil = State.Elapsed;
            zombie.IceHits = 0;
            zombie.Burns.Clear();
            foreach (var status in zombie.Statuses.OrderBy(value => value.Sequence))
            {
                var definition = _content.RuntimeStatuses[status.DefinitionId];
                var until = State.Elapsed + BattleSkillTiming.TicksToSeconds(status.RemainingTicks);
                if (definition.Kind == BattleStatusKind.Slow) zombie.SlowUntil = Mathf.Max(zombie.SlowUntil, until);
                else if (definition.Kind == BattleStatusKind.Freeze) zombie.FreezeUntil = Mathf.Max(zombie.FreezeUntil, until);
                else if (definition.Kind == BattleStatusKind.Stun) zombie.HitStunUntil = Mathf.Max(zombie.HitStunUntil, until);
                else if (definition.Kind == BattleStatusKind.HitCount) zombie.IceHits = status.StackCount;
                else if (definition.Kind == BattleStatusKind.Burn)
                    zombie.Burns.Add(new BurnStack
                    {
                        Remaining = BattleSkillTiming.TicksToSeconds(status.RemainingTicks),
                        DamagePerSecond = status.Magnitude,
                    });
            }
        }

        private void KillZombie(int index, Zombie zombie, CombatEntityState killer = null)
        {
            RaisePassiveEvent(BattlePassiveTriggerKind.EntityDefeated, killer, zombie, 0f);
            State.Sun += zombie.Reward;
            AddFeedback("击杀 +" + zombie.Reward, Map.Route.Sample(zombie.PathProgress), new Color(.95f, .68f, .1f));
            State.Zombies.RemoveAt(index);
        }

        private void SettleWave()
        {
            if (State.Phase != GamePhase.Playing || State.WaveSpawned < State.WaveTotal || State.Zombies.Count > 0) return;
            var completed = State.WaveIndex;
            var wave = Wave(completed);
            State.Sun += wave.completionReward;
            GrantMilestone(completed);
            AddGlobalFeedback("第 " + completed + " 波完成，奖励 +" + wave.completionReward, new Color(.95f, .65f, .1f));
            if (completed >= _ruleSet.MaxWaves) State.Phase = GamePhase.Victory;
            else { State.Phase = GamePhase.BetweenWaves; State.BetweenTimer = _ruleSet.BetweenWaveSeconds; }
        }

        private void GrantMilestone(int wave)
        {
            var reward = _ruleSet.MilestoneRewards.FirstOrDefault(value => value.Wave == wave);
            if (reward == null) return;
            foreach (var equipmentId in reward.EquipmentIds)
                State.Inventory.Add(LegacyBattleContentIds.WeaponKindFromId(equipmentId), 1);
            State.Inventory.Pots += reward.PotCount;
            EmitCue(BattleContentIds.Cues.Milestone, 0, 0, Map.Core);
            AddGlobalFeedback("里程碑奖励：武器与花盆", new Color(.2f, .5f, .78f));
        }

        private WaveDefinitionDto Wave(int index)
        {
            if (index <= 0 || index > _orderedWaves.Count)
                throw new ArgumentOutOfRangeException(nameof(index));
            return _orderedWaves[index - 1];
        }

        private string ResolvePlantId(Plant plant)
        {
            if (string.IsNullOrEmpty(plant.ContentId)) plant.ContentId = LegacyBattleContentIds.Plant(plant.Kind);
            else
            {
                PlantKind legacyKind;
                if (LegacyBattleContentIds.TryPlantKindFromId(plant.ContentId, out legacyKind)) plant.Kind = legacyKind;
            }
            return plant.ContentId;
        }

        private string ResolveEquipmentId(Plant plant)
        {
            if (string.IsNullOrEmpty(plant.EquipmentId))
            {
                string equipmentId;
                if (LegacyBattleContentIds.TryEquipment(plant.Weapon, out equipmentId)) plant.EquipmentId = equipmentId;
            }
            else plant.Weapon = LegacyBattleContentIds.WeaponKindFromId(plant.EquipmentId);
            return plant.EquipmentId;
        }

        private string ResolveEnemyId(Zombie zombie)
        {
            if (string.IsNullOrEmpty(zombie.ContentId)) zombie.ContentId = LegacyBattleContentIds.Enemy(zombie.Kind);
            else
            {
                ZombieKind legacyKind;
                if (LegacyBattleContentIds.TryEnemyKindFromId(zombie.ContentId, out legacyKind))
                    zombie.Kind = legacyKind;
            }
            return zombie.ContentId;
        }

        private StarTierDefinitionDto StarTier(Plant plant)
        {
            return _content.StarTiers["star." + Mathf.Clamp(plant.Star, 1, 4)];
        }

        private float PlantDamage(Plant plant)
        {
            var baseValue = _content.Plants[ResolvePlantId(plant)].damage * StarTier(plant).damageMultiplier;
            return GetEffectiveAttribute(plant, CombatAttributeKind.Damage, baseValue);
        }

        private float PlantRange(Plant plant)
        {
            var baseValue = Map.FromLegacyDistance(_content.Plants[ResolvePlantId(plant)].rangeLegacyUnits)
                * StarTier(plant).rangeMultiplier;
            return GetEffectiveAttribute(plant, CombatAttributeKind.Range, baseValue);
        }

        private Vector2 PlantPoint(Plant plant)
        {
            return plant == null ? Map.Core : PotPoint(PotById(plant.PotId));
        }

        private void EmitCue(string cueId, int sourceEntityId, int targetEntityId, Vector2 position)
        {
            if (string.IsNullOrEmpty(cueId)) return;
            CombatEffectKind kind;
            float duration;
            var hasCombatEffect = TryLegacyCombatEffect(cueId, out kind, out duration);
            _presentationEvents.EmitCue(State.LogicTick, cueId, VisualIdForCue(cueId),
                sourceEntityId, targetEntityId, position, hasCombatEffect, kind, duration);
        }

        private static bool TryLegacyCombatEffect(string cueId, out CombatEffectKind kind, out float duration)
        {
            duration = .3f;
            if (cueId == BattleContentIds.Cues.PeaImpact) kind = CombatEffectKind.PeaImpact;
            else if (cueId == BattleContentIds.Cues.WatermelonBlast) { kind = CombatEffectKind.WatermelonBlast; duration = .65f; }
            else if (cueId == BattleContentIds.Cues.BananaHit) { kind = CombatEffectKind.HitSpark; duration = .24f; }
            else if (cueId == BattleContentIds.Cues.DurianDrop) { kind = CombatEffectKind.DurianDrop; duration = .7f; }
            else if (cueId == BattleContentIds.Cues.SunBurst) { kind = CombatEffectKind.SunBurst; duration = .65f; }
            else if (cueId == BattleContentIds.Cues.GatlingMuzzle) { kind = CombatEffectKind.GatlingMuzzle; duration = .22f; }
            else if (cueId == BattleContentIds.Cues.IceImpact) { kind = CombatEffectKind.IceImpact; duration = .36f; }
            else if (cueId == BattleContentIds.Cues.ChiliImpact) { kind = CombatEffectKind.ChiliImpact; duration = .38f; }
            else { kind = default(CombatEffectKind); return false; }
            return true;
        }

        private void AddGlobalFeedback(string text, Color color) { AddFeedback(text, Map.Core, color); }
        private static string VisualIdForCue(string cueId)
        {
            if (cueId == BattleContentIds.Cues.PeaImpact) return BattleContentIds.Visuals.Pea;
            if (cueId == BattleContentIds.Cues.WatermelonBlast) return BattleContentIds.Visuals.Watermelon;
            if (cueId == BattleContentIds.Cues.BananaHit) return BattleContentIds.Visuals.Banana;
            if (cueId == BattleContentIds.Cues.DurianDrop) return BattleContentIds.Visuals.Durian;
            if (cueId == BattleContentIds.Cues.SunBurst) return BattleContentIds.Visuals.Sunflower;
            if (string.IsNullOrEmpty(cueId)) return string.Empty;
            return cueId.StartsWith("cue.", StringComparison.Ordinal)
                ? "visual." + cueId.Substring("cue.".Length)
                : "visual." + cueId;
        }
        private void AddFeedback(string text, Vector2 point, Color color)
        {
            _presentationEvents.EmitFeedback(State.LogicTick, text, point, color, 1.8f);
        }
    }
}
