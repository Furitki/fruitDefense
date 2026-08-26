using System;
using System.Collections.Generic;
using System.Linq;
using FruitDefense.Content;
using UnityEngine;

namespace FruitDefense.Core
{
    public enum BattleSimulationMode
    {
        Standard,
        GmStress,
    }

    public sealed partial class GameSimulation
    {
        private enum DamageEventPolicy
        {
            DirectAbilityHit,
            IndirectPeriodic,
        }

        public const float FixedStepSeconds = .05f;
        public const float MaxFrameDeltaSeconds = .25f;
        public const int MaxStepsPerFrame = 5;
        private const double AccumulatorEpsilon = 0.0000001;
        private const int MaxAbilityActivationsPerRootEvent = 128;
        private readonly DeterministicRandom _random;
        private readonly CompiledBattleContentCatalog _content;
        private readonly IReadOnlyList<WaveDefinitionDto> _orderedWaves;
        private readonly LevelRuleSetDefinition _ruleSet;
        private readonly List<int> _lastNurseryPotSlots = new List<int>();
        private readonly BattlePresentationEventStream _presentationEvents = new BattlePresentationEventStream();
        private readonly HashSet<string> _abilityActivationKeys = new HashSet<string>(StringComparer.Ordinal);
        private double _frameAccumulator;
        private int _abilityDispatchDepth;
        private int _abilityActivationCount;
        private long _abilityRootEventSequence;
        public GameState State { get; private set; }
        public BattleSimulationMode Mode { get; private set; }
        public int EscapedEnemyCount { get { return State.EscapedEnemies; } }
        public BattlefieldMapDefinition Map { get; private set; }
        public ResolvedLevelDefinition ActiveLevel { get; private set; }
        public LevelCompositeIdentity Identity { get; private set; }
        public ResolvedBattleSourceIdentity ResolvedSourceIdentity { get; private set; }
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
        public float PresentationInterpolationFraction
        {
            get
            {
                if (State.Paused || State.Phase == GamePhase.Victory
                    || State.Phase == GamePhase.Defeat) return 0f;
                return Mathf.Clamp01((float)(_frameAccumulator / FixedStepSeconds));
            }
        }
        public uint RandomState { get { return _random.State; } }
        public CompiledBattleContentCatalog Content { get { return _content; } }
        public int PendingPresentationEventCount { get { return _presentationEvents.PendingCount; } }
        public long DroppedPresentationEventCount { get { return _presentationEvents.DroppedCount; } }
        public event Action<GameSimulation> FixedStepStarting;

        public GameSimulation(int seed = 0, BattlefieldMapDefinition map = null)
            : this(CreateBundledContent(), seed, map, BattleSimulationMode.Standard)
        {
        }

        public GameSimulation(CompiledBattleContentCatalog content, int seed = 0, BattlefieldMapDefinition map = null)
            : this(content, seed, map, BattleSimulationMode.Standard)
        {
        }

        public GameSimulation(CompiledBattleContentCatalog content, int seed,
            BattlefieldMapDefinition map, BattleSimulationMode mode)
            : this(content, seed, map, mode, null, CreateDefaultWaveOrder(content),
                CreateDefaultRuleSet(content))
        {
        }

        public GameSimulation(ResolvedLevelDefinition resolvedLevel, int seed = 0)
            : this(RequireResolvedLevel(resolvedLevel).BattleContent, seed, resolvedLevel.Map,
                BattleSimulationMode.Standard,
                resolvedLevel, resolvedLevel.OrderedWaves, resolvedLevel.RuleSet)
        {
        }

        public GameSimulation(CompiledLevelCatalog levelCatalog, string levelId, int seed = 0)
            : this(ResolvedBattleSourceIdentity.Resolve(levelCatalog, levelId), seed)
        {
        }

        private GameSimulation(CatalogResolvedBattleSource source, int seed)
            : this(source.ResolvedLevel.BattleContent, seed, source.ResolvedLevel.Map,
                BattleSimulationMode.Standard, source.ResolvedLevel,
                source.ResolvedLevel.OrderedWaves, source.ResolvedLevel.RuleSet,
                source.Identity)
        {
        }

        private GameSimulation(CompiledBattleContentCatalog content, int seed,
            BattlefieldMapDefinition map, BattleSimulationMode mode,
            ResolvedLevelDefinition resolvedLevel,
            IEnumerable<WaveDefinitionDto> orderedWaves, LevelRuleSetDefinition ruleSet,
            ResolvedBattleSourceIdentity resolvedSourceIdentity = null)
        {
            _content = content ?? throw new ArgumentNullException(nameof(content));
            if (!Enum.IsDefined(typeof(BattleSimulationMode), mode))
                throw new ArgumentOutOfRangeException(nameof(mode));
            Mode = mode;
            Map = map ?? GameConfig.DefaultBattlefield;
            ActiveLevel = resolvedLevel;
            Identity = resolvedLevel == null ? null : resolvedLevel.Identity;
            ResolvedSourceIdentity = resolvedSourceIdentity;
            _ruleSet = ruleSet ?? throw new ArgumentNullException(nameof(ruleSet));
            _orderedWaves = Array.AsReadOnly((orderedWaves ?? throw new ArgumentNullException(nameof(orderedWaves)))
                .Select(CloneWave).ToArray());
            MapId = resolvedLevel == null
                ? ResolveMapIdentity(Map, map == null || ReferenceEquals(Map, GameConfig.DefaultBattlefield))
                : resolvedLevel.Identity.MapId;
            string mapReason;
            if (!Map.Validate(out mapReason)) throw new InvalidOperationException("Invalid battlefield map: " + mapReason);
            var expectedProfile = mode == BattleSimulationMode.Standard
                ? BattlefieldExecutionProfile.StandardRelease
                : BattlefieldExecutionProfile.GmMultiRoute;
            if (Map.ExecutionProfile != expectedProfile)
                throw new InvalidOperationException("Battle simulation mode " + mode
                    + " requires battlefield execution profile " + expectedProfile + ".");
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

        private static LevelRuleSetDefinition CreateDefaultRuleSet(CompiledBattleContentCatalog content)
        {
            if (content == null) throw new ArgumentNullException(nameof(content));
            return new LevelRuleSetDefinition(content.BattleRules);
        }

        private static IReadOnlyList<WaveDefinitionDto> CreateDefaultWaveOrder(
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
            _abilityActivationKeys.Clear();
            _abilityDispatchDepth = 0;
            _abilityActivationCount = 0;
            _abilityRootEventSequence = 0;
            State = new GameState
            {
                Phase = Mode == BattleSimulationMode.GmStress ? GamePhase.Playing : GamePhase.Ready,
                Sun = _ruleSet.InitialSun,
                Lives = _ruleSet.InitialLives,
                RandomSeed = seed,
            };
            _lastNurseryPotSlots.Clear();
            AddInitialPots();
            _presentationEvents.EmitBattleStateChanged(State.LogicTick,
                BattleContentIds.BattleStates.Ready, DefaultBattleAnchor());
        }

        private Vector2 DefaultBattleAnchor()
        {
            return Map.HasCore ? Map.Core : Map.SampleRoute(Map.RouteIds[0], 0f);
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
                if (!string.IsNullOrEmpty(plant.EquipmentId)) State.Inventory.Add(plant.EquipmentId, 1);
            State.Plants.RemoveAll(plant => plant.NurseryIndex >= 0);

            var firstBatch = State.RefreshCount == 0;
            var definitions = _content.Plants.Values
                .OrderBy(value => value.id, StringComparer.Ordinal).ToArray();
            var attackers = definitions.Where(value =>
                    (value.tags ?? Array.Empty<string>()).Contains("plant.damage"))
                .ToArray();
            if (definitions.Length == 0 || attackers.Length == 0)
                throw new InvalidOperationException(
                    "Nursery generation requires at least one plant and one plant.damage definition.");
            var results = new List<string>();
            var producerCount = 0;
            for (var index = 0; index < _ruleSet.NurserySlotCount; index++)
            {
                var forceAttacker = firstBatch && index < 2;
                if (!forceAttacker && _random.NextUnitDouble() < _ruleSet.NurseryPotChance)
                {
                    results.Add(string.Empty);
                    continue;
                }
                var pool = forceAttacker || producerCount >= 2 ? attackers : definitions;
                var definition = pool[_random.NextInt(pool.Length)];
                if ((definition.tags ?? Array.Empty<string>()).Contains("plant.producer")) producerCount++;
                results.Add(definition.id);
            }
            Shuffle(results);

            State.Sun -= cost;
            State.RefreshCount++;
            _lastNurseryPotSlots.Clear();
            for (var slot = 0; slot < _ruleSet.NurserySlotCount; slot++)
            {
                if (string.IsNullOrEmpty(results[slot]))
                {
                    _lastNurseryPotSlots.Add(slot);
                    State.Inventory.Pots++;
                    continue;
                }
                State.Plants.Add(new Plant
                {
                    Id = State.NextId++, DefinitionId = results[slot],
                    Star = 1, NurseryIndex = slot,
                });
            }
            var plantCount = _ruleSet.NurserySlotCount - _lastNurseryPotSlots.Count;
            reason = _lastNurseryPotSlots.Count > 0
                ? "刷新完成：水果 " + plantCount + " 株，花盆×" + _lastNurseryPotSlots.Count + " 已入库"
                : "获得 " + plantCount + " 株水果";
            return true;
        }

        public int RefreshCost(int refreshCount)
        {
            return _ruleSet.RefreshBaseCost + _ruleSet.RefreshCostStep * Mathf.Max(0, refreshCount);
        }

        public bool StartWave(out string reason)
        {
            if (Mode == BattleSimulationMode.GmStress)
            {
                reason = "GM stress battles do not execute automatic waves";
                return false;
            }
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
            if (next == 1) RaiseCombatEvent(CombatEventKind.BattleStarted, null, null, 0f);
            reason = "第 " + next + " 波来袭";
            _presentationEvents.EmitBattleStateChanged(State.LogicTick,
                BattleContentIds.BattleStates.WaveStarted,
                Map.SampleRoute(Map.PrimaryRouteId, 0f), next,
                State.WaveTotal);
            return true;
        }

        public void TogglePause() { State.Paused = !State.Paused; }
        public void SetSpeed(int speed) { State.Speed = Mathf.Clamp(speed, 1, 2); }

        public Plant PlantAtPot(int potId) { return State.Plants.FirstOrDefault(plant => plant.PotId == potId); }
        public Plant PlantAtNursery(int slot) { return State.Plants.FirstOrDefault(plant => plant.NurseryIndex == slot); }
        public Pot PotById(int id) { return State.Pots.FirstOrDefault(pot => pot.Id == id); }
        public Plant PlantById(int id) { return State.Plants.FirstOrDefault(plant => plant.Id == id); }
        public Zombie ZombieById(int id) { return State.Zombies.FirstOrDefault(zombie => zombie.Id == id); }
        public Vector2 ZombiePoint(Zombie zombie)
        {
            if (zombie == null) throw new ArgumentNullException(nameof(zombie));
            return Map.SampleRoute(zombie.RouteId, zombie.PathProgress);
        }

        public Zombie SpawnEnemy(string enemyDefinitionId, string routeId)
        {
            if (Mode != BattleSimulationMode.GmStress)
                throw new InvalidOperationException(
                    "Manual enemy spawning is available only in GM stress simulations.");
            return SpawnEnemy(enemyDefinitionId, routeId, 1f, 1f);
        }

        public Plant PlaceOrReplaceGmPlant(string plantDefinitionId, int potId)
        {
            if (Mode != BattleSimulationMode.GmStress)
                throw new InvalidOperationException(
                    "Free plant placement is available only in GM stress simulations.");
            if (string.IsNullOrWhiteSpace(plantDefinitionId)
                || !_content.Plants.ContainsKey(plantDefinitionId))
                throw new ArgumentException("Unknown plant definition ID '"
                    + plantDefinitionId + "'.", nameof(plantDefinitionId));
            var pot = PotById(potId);
            if (pot == null || !pot.Active)
                throw new ArgumentException("Unknown or inactive GM pot ID '" + potId + "'.",
                    nameof(potId));

            State.Plants.RemoveAll(plant => plant.PotId == potId);
            var placed = new Plant
            {
                Id = State.NextId++,
                DefinitionId = plantDefinitionId,
                Star = 1,
                PotId = potId,
                NurseryIndex = -1,
            };
            State.Plants.Add(placed);
            return placed;
        }

    }
}
