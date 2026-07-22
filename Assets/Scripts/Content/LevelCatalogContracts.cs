using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using FruitDefense.Core;

namespace FruitDefense.Content
{
    public sealed class LevelCompositeIdentity
    {
        public string LevelId { get; private set; }
        public string MapId { get; private set; }
        public string WaveSetId { get; private set; }
        public string RuleSetId { get; private set; }
        public string ThemeId { get; private set; }

        public LevelCompositeIdentity(string levelId, string mapId, string waveSetId,
            string ruleSetId, string themeId)
        {
            LevelId = levelId ?? string.Empty;
            MapId = mapId ?? string.Empty;
            WaveSetId = waveSetId ?? string.Empty;
            RuleSetId = ruleSetId ?? string.Empty;
            ThemeId = themeId ?? string.Empty;
        }

        public override string ToString()
        {
            return LevelId + "|" + MapId + "|" + WaveSetId + "|" + RuleSetId + "|" + ThemeId;
        }
    }

    public sealed class LevelDefinition
    {
        public string LevelId { get; private set; }
        public string MapId { get; private set; }
        public string WaveSetId { get; private set; }
        public string RuleSetId { get; private set; }
        public string ThemeId { get; private set; }
        public LevelCompositeIdentity Identity { get; private set; }

        public LevelDefinition(string levelId, string mapId, string waveSetId,
            string ruleSetId, string themeId)
        {
            LevelId = levelId ?? string.Empty;
            MapId = mapId ?? string.Empty;
            WaveSetId = waveSetId ?? string.Empty;
            RuleSetId = ruleSetId ?? string.Empty;
            ThemeId = themeId ?? string.Empty;
            Identity = new LevelCompositeIdentity(LevelId, MapId, WaveSetId, RuleSetId, ThemeId);
        }
    }

    public sealed class LevelWaveSetDefinition
    {
        public string WaveSetId { get; private set; }
        public IReadOnlyList<string> WaveIds { get; private set; }

        public LevelWaveSetDefinition(string waveSetId, IEnumerable<string> waveIds)
        {
            WaveSetId = waveSetId ?? string.Empty;
            WaveIds = Array.AsReadOnly((waveIds ?? Enumerable.Empty<string>())
                .Select(value => value ?? string.Empty).ToArray());
        }
    }

    public sealed class LevelMilestoneDefinition
    {
        public int Wave { get; private set; }
        public int PotCount { get; private set; }
        public IReadOnlyList<string> EquipmentIds { get; private set; }

        public LevelMilestoneDefinition(int wave, int potCount, IEnumerable<string> equipmentIds)
        {
            Wave = wave;
            PotCount = potCount;
            EquipmentIds = Array.AsReadOnly((equipmentIds ?? Enumerable.Empty<string>())
                .Select(value => value ?? string.Empty).ToArray());
        }
    }

    public sealed class LevelRuleSetDefinition
    {
        public string RuleSetId { get; private set; }
        public int InitialSun { get; private set; }
        public int InitialLives { get; private set; }
        public int MaxWaves { get; private set; }
        public int InitialPotCount { get; private set; }
        public float BetweenWaveSeconds { get; private set; }
        public int NurserySlotCount { get; private set; }
        public float NurseryPotChance { get; private set; }
        public int RefreshBaseCost { get; private set; }
        public int RefreshCostStep { get; private set; }
        public IReadOnlyList<LevelMilestoneDefinition> MilestoneRewards { get; private set; }

        public LevelRuleSetDefinition(BattleRulesDto rules)
        {
            rules = rules ?? new BattleRulesDto();
            RuleSetId = rules.id ?? string.Empty;
            InitialSun = rules.initialSun;
            InitialLives = rules.initialLives;
            MaxWaves = rules.maxWaves;
            InitialPotCount = rules.initialPotCount;
            BetweenWaveSeconds = rules.betweenWaveSeconds;
            NurserySlotCount = rules.nurserySlotCount;
            NurseryPotChance = rules.nurseryPotChance;
            RefreshBaseCost = rules.refreshBaseCost;
            RefreshCostStep = rules.refreshCostStep;
            MilestoneRewards = Array.AsReadOnly((rules.milestoneRewards
                    ?? Array.Empty<MilestoneRewardDefinitionDto>())
                .Select(value => value == null
                    ? new LevelMilestoneDefinition(0, 0, Array.Empty<string>())
                    : new LevelMilestoneDefinition(value.wave, value.potCount, value.equipmentIds))
                .ToArray());
        }

        public BattleRulesDto CreateBattleRules()
        {
            return new BattleRulesDto
            {
                id = RuleSetId,
                initialSun = InitialSun,
                initialLives = InitialLives,
                maxWaves = MaxWaves,
                initialPotCount = InitialPotCount,
                betweenWaveSeconds = BetweenWaveSeconds,
                nurserySlotCount = NurserySlotCount,
                nurseryPotChance = NurseryPotChance,
                refreshBaseCost = RefreshBaseCost,
                refreshCostStep = RefreshCostStep,
                milestoneRewards = MilestoneRewards.Select(value => new MilestoneRewardDefinitionDto
                {
                    wave = value.Wave,
                    potCount = value.PotCount,
                    equipmentIds = value.EquipmentIds.ToArray(),
                }).ToArray(),
            };
        }
    }

    public sealed class LevelPresentationThemeDefinition
    {
        public string ThemeId { get; private set; }
        public string DisplayName { get; private set; }
        public string BackgroundColor { get; private set; }
        public string GroundColor { get; private set; }
        public string RouteColor { get; private set; }
        public string RouteEdgeColor { get; private set; }
        public string PlantableColor { get; private set; }
        public string BlockedColor { get; private set; }
        public string CoreColor { get; private set; }
        public string AccentColor { get; private set; }
        public string TerrainPaletteId { get; private set; }

        public LevelPresentationThemeDefinition(string themeId, string displayName,
            string backgroundColor, string groundColor, string routeColor, string routeEdgeColor,
            string plantableColor, string blockedColor, string coreColor, string accentColor,
            string terrainPaletteId = null)
        {
            ThemeId = themeId ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            BackgroundColor = backgroundColor ?? string.Empty;
            GroundColor = groundColor ?? string.Empty;
            RouteColor = routeColor ?? string.Empty;
            RouteEdgeColor = routeEdgeColor ?? string.Empty;
            PlantableColor = plantableColor ?? string.Empty;
            BlockedColor = blockedColor ?? string.Empty;
            CoreColor = coreColor ?? string.Empty;
            AccentColor = accentColor ?? string.Empty;
            TerrainPaletteId = terrainPaletteId ?? string.Empty;
        }

        public IReadOnlyList<string> RequiredColors
        {
            get
            {
                return new[]
                {
                    BackgroundColor, GroundColor, RouteColor, RouteEdgeColor,
                    PlantableColor, BlockedColor, CoreColor, AccentColor,
                };
            }
        }
    }

    public sealed class LevelCatalogSource
    {
        public string CatalogId { get; private set; }
        public string ContentCatalogId { get; private set; }
        public string ContentVersion { get; private set; }
        public string DefaultLevelId { get; private set; }
        public IReadOnlyList<LevelDefinition> Levels { get; private set; }
        public IReadOnlyList<BattlefieldMapDefinition> Maps { get; private set; }
        public IReadOnlyList<LevelWaveSetDefinition> WaveSets { get; private set; }
        public IReadOnlyList<LevelRuleSetDefinition> RuleSets { get; private set; }
        public IReadOnlyList<LevelPresentationThemeDefinition> Themes { get; private set; }
        public IReadOnlyList<string> TerrainPaletteIds { get; private set; }

        public LevelCatalogSource(string catalogId, string contentCatalogId, string contentVersion,
            string defaultLevelId, IEnumerable<LevelDefinition> levels,
            IEnumerable<BattlefieldMapDefinition> maps, IEnumerable<LevelWaveSetDefinition> waveSets,
            IEnumerable<LevelRuleSetDefinition> ruleSets,
            IEnumerable<LevelPresentationThemeDefinition> themes,
            IEnumerable<string> terrainPaletteIds = null)
        {
            CatalogId = catalogId ?? string.Empty;
            ContentCatalogId = contentCatalogId ?? string.Empty;
            ContentVersion = contentVersion ?? string.Empty;
            DefaultLevelId = defaultLevelId ?? string.Empty;
            Levels = Array.AsReadOnly((levels ?? Enumerable.Empty<LevelDefinition>()).ToArray());
            Maps = Array.AsReadOnly((maps ?? Enumerable.Empty<BattlefieldMapDefinition>()).ToArray());
            WaveSets = Array.AsReadOnly((waveSets ?? Enumerable.Empty<LevelWaveSetDefinition>()).ToArray());
            RuleSets = Array.AsReadOnly((ruleSets ?? Enumerable.Empty<LevelRuleSetDefinition>()).ToArray());
            Themes = Array.AsReadOnly((themes ?? Enumerable.Empty<LevelPresentationThemeDefinition>()).ToArray());
            TerrainPaletteIds = Array.AsReadOnly((terrainPaletteIds ?? Enumerable.Empty<string>()).ToArray());
        }
    }

    public enum LevelResolutionErrorCode
    {
        None,
        InvalidLevelId,
        UnknownLevel,
        MissingMap,
        MissingWaveSet,
        MissingRuleSet,
        MissingTheme,
    }

    public sealed class LevelResolutionError
    {
        public LevelResolutionErrorCode Code { get; private set; }
        public string LevelId { get; private set; }
        public string Field { get; private set; }
        public string ReferencedId { get; private set; }
        public string Message { get; private set; }

        public LevelResolutionError(LevelResolutionErrorCode code, string levelId,
            string field, string referencedId, string message)
        {
            Code = code;
            LevelId = levelId ?? string.Empty;
            Field = field ?? string.Empty;
            ReferencedId = referencedId ?? string.Empty;
            Message = message ?? string.Empty;
        }

        public override string ToString()
        {
            return Code + " [" + LevelId + "." + Field + "] " + Message;
        }
    }

    public sealed class ResolvedLevelDefinition
    {
        public LevelDefinition Level { get; private set; }
        public LevelCompositeIdentity Identity { get { return Level.Identity; } }
        public BattlefieldMapDefinition Map { get; private set; }
        public LevelWaveSetDefinition WaveSet { get; private set; }
        public IReadOnlyList<WaveDefinitionDto> OrderedWaves { get; private set; }
        public LevelRuleSetDefinition RuleSet { get; private set; }
        public LevelPresentationThemeDefinition Theme { get; private set; }
        public CompiledBattleContentCatalog BattleContent { get; private set; }

        internal ResolvedLevelDefinition(LevelDefinition level, BattlefieldMapDefinition map,
            LevelWaveSetDefinition waveSet, IEnumerable<WaveDefinitionDto> orderedWaves,
            LevelRuleSetDefinition ruleSet, LevelPresentationThemeDefinition theme,
            CompiledBattleContentCatalog battleContent)
        {
            Level = level;
            Map = map;
            WaveSet = waveSet;
            OrderedWaves = Array.AsReadOnly(orderedWaves.ToArray());
            RuleSet = ruleSet;
            Theme = theme;
            BattleContent = battleContent;
        }
    }

    public sealed class LevelResolutionResult
    {
        public bool Succeeded { get { return Value != null && Error == null; } }
        public ResolvedLevelDefinition Value { get; private set; }
        public LevelResolutionError Error { get; private set; }

        private LevelResolutionResult(ResolvedLevelDefinition value, LevelResolutionError error)
        {
            Value = value;
            Error = error;
        }

        internal static LevelResolutionResult Success(ResolvedLevelDefinition value)
        {
            return new LevelResolutionResult(value, null);
        }

        internal static LevelResolutionResult Failure(LevelResolutionErrorCode code, string levelId,
            string field, string referencedId, string message)
        {
            return new LevelResolutionResult(null,
                new LevelResolutionError(code, levelId, field, referencedId, message));
        }
    }

    public sealed class LevelCatalogValidationIssue
    {
        public string Code { get; private set; }
        public string Category { get; private set; }
        public string ItemId { get; private set; }
        public string Field { get; private set; }
        public string Message { get; private set; }

        public LevelCatalogValidationIssue(string code, string category, string itemId,
            string field, string message)
        {
            Code = code;
            Category = category;
            ItemId = itemId;
            Field = field;
            Message = message;
        }

        public override string ToString()
        {
            return Code + " [" + Category + ":" + (string.IsNullOrEmpty(ItemId) ? "<catalog>" : ItemId)
                + "." + Field + "] " + Message;
        }
    }

    public sealed class LevelCatalogValidationResult
    {
        private readonly List<LevelCatalogValidationIssue> _issues = new List<LevelCatalogValidationIssue>();
        private readonly ReadOnlyCollection<LevelCatalogValidationIssue> _readOnlyIssues;

        public LevelCatalogValidationResult()
        {
            _readOnlyIssues = _issues.AsReadOnly();
        }

        public bool IsValid { get { return _issues.Count == 0; } }
        public IReadOnlyList<LevelCatalogValidationIssue> Issues { get { return _readOnlyIssues; } }

        internal void Add(string code, string category, string itemId, string field, string message)
        {
            _issues.Add(new LevelCatalogValidationIssue(code, category, itemId, field, message));
        }
    }
}
