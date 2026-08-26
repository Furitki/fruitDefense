using System;
using System.Collections.Generic;
using System.Linq;
using FruitDefense.Content;
using FruitDefense.Core;
using UnityEngine;

namespace FruitDefense.Presentation
{
    public enum PresentationVfxKind
    {
        None,
        PeaImpact,
        WatermelonBlast,
        BananaHit,
        DurianImpact,
        SunBurst,
        GatlingMuzzle,
        IceImpact,
        FreezeProc,
        ChiliImpact,
        BurnTick,
        Defeat,
    }

    public enum CombatFeedbackPriority
    {
        Ambient = 0,
        Light = 10,
        Medium = 20,
        Heavy = 30,
        Control = 40,
        Defeat = 50,
    }

    public enum CombatImpactBeatRole
    {
        None = 0,
        Heavy = 1,
        Cluster = 2,
        Terminal = 3,
    }

    public readonly struct CombatImpactBeatStyle
    {
        public CombatImpactBeatStyle(CombatImpactBeatRole role, float amplitude,
            float duration, float flash, float oscillations)
        {
            Role = role;
            Amplitude = Mathf.Clamp(amplitude, 0f,
                CombatImpactBeatCatalog.MaximumAmplitude);
            Duration = Mathf.Clamp(duration, 0f,
                CombatImpactBeatCatalog.MaximumDuration);
            Flash = Mathf.Clamp(flash, 0f,
                CombatImpactBeatCatalog.MaximumFlash);
            Oscillations = Mathf.Clamp(oscillations, 0f, 2f);
        }

        public CombatImpactBeatRole Role { get; }
        public float Amplitude { get; }
        public float Duration { get; }
        public float Flash { get; }
        public float Oscillations { get; }
    }

    public static class CombatImpactBeatCatalog
    {
        public const float CooldownSeconds = .16f;
        public const float ClusterWindowSeconds = .12f;
        public const int ClusterMinimumCount = 3;
        public const float MaximumAmplitude = 3f;
        public const float MaximumDuration = .18f;
        public const float MaximumFlash = .12f;

        public static CombatImpactBeatStyle Resolve(CombatImpactBeatRole role)
        {
            switch (role)
            {
                case CombatImpactBeatRole.None:
                    return default;
                case CombatImpactBeatRole.Heavy:
                    return new CombatImpactBeatStyle(role, 2.2f, .16f, .08f, 1.5f);
                case CombatImpactBeatRole.Cluster:
                    return new CombatImpactBeatStyle(role, 2.5f, .17f, .1f, 1.35f);
                case CombatImpactBeatRole.Terminal:
                    return new CombatImpactBeatStyle(role, 3f, .18f, .12f, 1.25f);
                default:
                    throw new InvalidOperationException(
                        "Unknown combat impact-beat role: " + role);
            }
        }
    }

    public enum CombatAudioRoute
    {
        None,
        LightImpact,
        HeavyImpact,
        Projectile,
        Resource,
        Control,
        Defeat,
    }

    public enum CombatFeedbackPolicy
    {
        None,
        Profile,
    }

    public readonly struct CombatFeedbackKey : IEquatable<CombatFeedbackKey>
    {
        public CombatFeedbackKey(BattlePresentationEventKind kind, string semanticId,
            string sourceEquipmentId = "")
        {
            Kind = kind;
            SemanticId = semanticId ?? string.Empty;
            SourceEquipmentId = sourceEquipmentId ?? string.Empty;
        }

        public BattlePresentationEventKind Kind { get; }
        public string SemanticId { get; }
        public string SourceEquipmentId { get; }

        public bool Equals(CombatFeedbackKey other)
        {
            return Kind == other.Kind
                && string.Equals(SemanticId, other.SemanticId, StringComparison.Ordinal)
                && string.Equals(SourceEquipmentId, other.SourceEquipmentId,
                    StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is CombatFeedbackKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = ((int)Kind * 397)
                    ^ StringComparer.Ordinal.GetHashCode(SemanticId ?? string.Empty);
                return (hash * 397) ^ StringComparer.Ordinal.GetHashCode(
                    SourceEquipmentId ?? string.Empty);
            }
        }

        public override string ToString()
        {
            return Kind + "/" + SemanticId + (string.IsNullOrEmpty(SourceEquipmentId)
                ? string.Empty
                : "@" + SourceEquipmentId);
        }
    }

    public sealed class CombatFeedbackProfile
    {
        public CombatFeedbackProfile(string id, PresentationVfxKind vfxKind,
            CombatFeedbackPriority priority, float duration,
            float attackerRecoil = 0f, float targetFlash = 0f,
            float targetSquash = 0f, float targetDisplacement = 0f,
            CombatFloatingTextRole floatingTextRole = CombatFloatingTextRole.None,
            float mergeWindow = .1f,
            CombatAudioRoute audioRoute = CombatAudioRoute.None,
            float minimumInterval = 0f,
            CombatImpactBeatRole beatRole = CombatImpactBeatRole.None)
        {
            Id = string.IsNullOrEmpty(id)
                ? throw new ArgumentException("A feedback profile id is required.", nameof(id))
                : id;
            VfxKind = vfxKind;
            Priority = priority;
            Duration = Mathf.Max(0f, duration);
            AttackerRecoil = Mathf.Max(0f, attackerRecoil);
            TargetFlash = Mathf.Clamp01(targetFlash);
            TargetSquash = Mathf.Clamp(targetSquash, 0f, .5f);
            TargetDisplacement = Mathf.Max(0f, targetDisplacement);
            FloatingTextRole = floatingTextRole;
            MergeWindow = Mathf.Max(0f, mergeWindow);
            AudioRoute = audioRoute;
            MinimumInterval = Mathf.Max(0f, minimumInterval);
            if (!Enum.IsDefined(typeof(CombatImpactBeatRole), beatRole))
                throw new ArgumentOutOfRangeException(nameof(beatRole), beatRole,
                    "Unknown combat impact-beat role.");
            BeatRole = beatRole;
        }

        public string Id { get; }
        public PresentationVfxKind VfxKind { get; }
        public CombatFeedbackPriority Priority { get; }
        public float Duration { get; }
        public float AttackerRecoil { get; }
        public float TargetFlash { get; }
        public float TargetSquash { get; }
        public float TargetDisplacement { get; }
        public CombatFloatingTextRole FloatingTextRole { get; }
        public float MergeWindow { get; }
        public CombatAudioRoute AudioRoute { get; }
        public float MinimumInterval { get; }
        public CombatImpactBeatRole BeatRole { get; }
    }

    public readonly struct CombatFeedbackCatalogEntry
    {
        private CombatFeedbackCatalogEntry(CombatFeedbackPolicy policy,
            CombatFeedbackProfile profile)
        {
            Policy = policy;
            Profile = profile;
        }

        public CombatFeedbackPolicy Policy { get; }
        public CombatFeedbackProfile Profile { get; }

        public static CombatFeedbackCatalogEntry None()
        {
            return new CombatFeedbackCatalogEntry(CombatFeedbackPolicy.None, null);
        }

        public static CombatFeedbackCatalogEntry Concrete(CombatFeedbackProfile profile)
        {
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            return new CombatFeedbackCatalogEntry(CombatFeedbackPolicy.Profile, profile);
        }
    }

    /// <summary>
    /// Presentation-owned mapping from stable gameplay identities to finite feedback policy.
    /// Every bundled key is deliberately concrete or explicitly silent.
    /// </summary>
    public sealed class CombatFeedbackCatalog
    {
        public const string SunResource = BattleContentIds.Resources.Sun;
        private readonly Dictionary<CombatFeedbackKey, CombatFeedbackCatalogEntry> _entries;
        private readonly HashSet<CombatFeedbackKey> _required;

        public CombatFeedbackCatalog(IEnumerable<CombatFeedbackKey> requiredKeys)
        {
            _entries = new Dictionary<CombatFeedbackKey, CombatFeedbackCatalogEntry>();
            _required = new HashSet<CombatFeedbackKey>(requiredKeys
                ?? throw new ArgumentNullException(nameof(requiredKeys)));
        }

        public int Count { get { return _entries.Count; } }
        public IEnumerable<CombatFeedbackKey> RequiredKeys { get { return _required; } }

        public void Declare(CombatFeedbackKey key, CombatFeedbackCatalogEntry entry)
        {
            if (string.IsNullOrEmpty(key.SemanticId))
                throw new ArgumentException("A semantic feedback identity is required.", nameof(key));
            if (_entries.ContainsKey(key))
                throw new InvalidOperationException("Duplicate combat feedback key: " + key);
            _entries.Add(key, entry);
        }

        public bool TryResolve(BattlePresentationEvent value,
            out CombatFeedbackCatalogEntry entry)
        {
            if (value == null)
            {
                entry = default;
                return false;
            }
            var exact = new CombatFeedbackKey(value.Kind, value.SemanticId,
                value.SourceEquipmentId);
            if (_entries.TryGetValue(exact, out entry)) return true;
            return !string.IsNullOrEmpty(value.SourceEquipmentId)
                && _entries.TryGetValue(new CombatFeedbackKey(
                    value.Kind, value.SemanticId), out entry);
        }

        public bool CanResolve(CombatFeedbackKey key)
        {
            CombatFeedbackCatalogEntry entry;
            if (_entries.TryGetValue(key, out entry)) return true;
            return !string.IsNullOrEmpty(key.SourceEquipmentId)
                && _entries.ContainsKey(new CombatFeedbackKey(key.Kind, key.SemanticId));
        }

        public IReadOnlyList<string> Validate()
        {
            var issues = new List<string>();
            foreach (var required in _required)
            {
                CombatFeedbackCatalogEntry entry;
                if (!_entries.TryGetValue(required, out entry))
                {
                    issues.Add("missing-policy:" + required);
                    continue;
                }
                if (entry.Policy == CombatFeedbackPolicy.Profile && entry.Profile == null)
                    issues.Add("missing-profile:" + required);
                else if (entry.Policy == CombatFeedbackPolicy.None && entry.Profile != null)
                    issues.Add("none-policy-has-profile:" + required);
            }
            return issues.AsReadOnly();
        }

        public IReadOnlyList<string> ValidateCoverage(CompiledBattleContentCatalog content)
        {
            if (content == null) throw new ArgumentNullException(nameof(content));
            var issues = new List<string>(Validate());
            var emittable = new HashSet<CombatFeedbackKey>(EmittableKeys(content));
            foreach (var key in emittable.OrderBy(value => value.ToString(),
                         StringComparer.Ordinal))
            {
                if (!CanResolve(key)) issues.Add("missing-emittable-policy:" + key);
            }
            foreach (var declared in _entries.Keys.OrderBy(value => value.ToString(),
                         StringComparer.Ordinal))
            {
                if (!IsReachableDeclaration(declared, emittable))
                    issues.Add("unreachable-policy:" + declared);
            }
            return issues.AsReadOnly();
        }

        private static bool IsReachableDeclaration(CombatFeedbackKey declared,
            IEnumerable<CombatFeedbackKey> emittable)
        {
            foreach (var candidate in emittable)
            {
                if (candidate.Equals(declared)) return true;
                if (string.IsNullOrEmpty(declared.SourceEquipmentId)
                    && candidate.Kind == declared.Kind
                    && string.Equals(candidate.SemanticId, declared.SemanticId,
                        StringComparison.Ordinal))
                    return true;
            }
            return false;
        }

        public static CombatFeedbackCatalog CreateBundled()
        {
            var catalog = new CombatFeedbackCatalog(Array.Empty<CombatFeedbackKey>());

            var pea = Impact("feedback.pea", PresentationVfxKind.PeaImpact,
                CombatFeedbackPriority.Light, .22f,
                CombatFloatingTextRole.NormalDamage,
                .08f, 1.5f, CombatImpactBeatRole.None);
            var watermelon = Impact("feedback.watermelon", PresentationVfxKind.WatermelonBlast,
                CombatFeedbackPriority.Heavy, .55f,
                CombatFloatingTextRole.HeavyDamage,
                .12f, 4f, CombatImpactBeatRole.Heavy);
            var banana = Impact("feedback.banana", PresentationVfxKind.BananaHit,
                CombatFeedbackPriority.Medium, .24f,
                CombatFloatingTextRole.NormalDamage,
                .1f, 2.3f, CombatImpactBeatRole.None);
            var durianRelease = new CombatFeedbackProfile(
                "feedback.durian.release", PresentationVfxKind.None,
                CombatFeedbackPriority.Heavy, .32f, attackerRecoil: 4f);
            var durianDamage = new CombatFeedbackProfile(
                "feedback.durian.damage", PresentationVfxKind.DurianImpact,
                CombatFeedbackPriority.Heavy, .42f, targetFlash: .85f,
                targetSquash: .16f, targetDisplacement: 5f,
                floatingTextRole: CombatFloatingTextRole.HeavyDamage,
                mergeWindow: .14f,
                audioRoute: CombatAudioRoute.HeavyImpact, minimumInterval: .04f,
                beatRole: CombatImpactBeatRole.Heavy);
            var gatlingRelease = new CombatFeedbackProfile(
                "feedback.gatling.release", PresentationVfxKind.GatlingMuzzle,
                CombatFeedbackPriority.Light, .14f, attackerRecoil: 1.6f,
                audioRoute: CombatAudioRoute.Projectile, minimumInterval: .06f);
            var ice = Impact("feedback.ice", PresentationVfxKind.IceImpact,
                CombatFeedbackPriority.Medium, .3f,
                CombatFloatingTextRole.None,
                .12f, 1.6f, CombatImpactBeatRole.None);
            var chili = Impact("feedback.chili", PresentationVfxKind.ChiliImpact,
                CombatFeedbackPriority.Medium, .32f,
                CombatFloatingTextRole.None,
                .12f, 1.8f, CombatImpactBeatRole.None);

            Declare(catalog, BattlePresentationEventKind.AbilityReleased,
                BattleContentIds.Abilities.PeaAttack,
                Recoil("feedback.pea.release", .18f, 2.5f));
            Declare(catalog, BattlePresentationEventKind.AbilityReleased,
                BattleContentIds.Abilities.WatermelonAttack,
                Recoil("feedback.watermelon.release", .28f, 3.4f));
            Declare(catalog, BattlePresentationEventKind.AbilityReleased,
                BattleContentIds.Abilities.BananaAttack,
                Recoil("feedback.banana.release", .24f, 3f));
            Declare(catalog, BattlePresentationEventKind.AbilityStarted,
                BattleContentIds.Abilities.DurianAttack,
                Recoil("feedback.durian.windup", .38f, 2f));
            Declare(catalog, BattlePresentationEventKind.AbilityReleased,
                BattleContentIds.Abilities.DurianAttack,
                CombatFeedbackCatalogEntry.Concrete(durianRelease));
            Declare(catalog, BattlePresentationEventKind.AbilityReleased,
                BattleContentIds.Abilities.SunflowerProduce,
                MotionOnly("feedback.sunflower.release", .3f, 2f));
            Declare(catalog, BattlePresentationEventKind.AbilityReleased,
                BattleContentIds.Abilities.IceOnHit,
                MotionOnly("feedback.ice.release", .16f, 1f));
            Declare(catalog, BattlePresentationEventKind.AbilityReleased,
                BattleContentIds.Abilities.IceProducerOpening,
                MotionOnly("feedback.ice.producer-release", .2f, 1f));
            Declare(catalog, BattlePresentationEventKind.AbilityReleased,
                BattleContentIds.Abilities.ChiliOnHit,
                MotionOnly("feedback.chili.release", .16f, 1f));
            foreach (var ability in new[]
            {
                BattleContentIds.Abilities.PeaAttack,
                BattleContentIds.Abilities.WatermelonAttack,
                BattleContentIds.Abilities.BananaAttack,
            })
                Declare(catalog, BattlePresentationEventKind.AbilityReleased, ability,
                    CombatFeedbackCatalogEntry.Concrete(gatlingRelease),
                    BattleContentIds.Equipment.Gatling);

            foreach (var ability in new[]
            {
                BattleContentIds.Abilities.PeaAttack,
                BattleContentIds.Abilities.WatermelonAttack,
                BattleContentIds.Abilities.BananaAttack,
                BattleContentIds.Abilities.SunflowerProduce,
                BattleContentIds.Abilities.IceOnHit,
                BattleContentIds.Abilities.IceProducerOpening,
                BattleContentIds.Abilities.ChiliOnHit,
            })
                Declare(catalog, BattlePresentationEventKind.AbilityStarted,
                    ability, CombatFeedbackCatalogEntry.None());

            Declare(catalog, BattlePresentationEventKind.ProjectileLaunched,
                BattleContentIds.Projectiles.Pea, CombatFeedbackCatalogEntry.None());
            Declare(catalog, BattlePresentationEventKind.ProjectileLaunched,
                BattleContentIds.Projectiles.Watermelon, CombatFeedbackCatalogEntry.None());
            Declare(catalog, BattlePresentationEventKind.ProjectileLaunched,
                BattleContentIds.Projectiles.Banana, CombatFeedbackCatalogEntry.None());

            Declare(catalog, BattlePresentationEventKind.DamageResolved,
                BattleContentIds.Abilities.PeaAttack, pea);
            Declare(catalog, BattlePresentationEventKind.DamageResolved,
                BattleContentIds.Abilities.WatermelonAttack, watermelon);
            Declare(catalog, BattlePresentationEventKind.DamageResolved,
                BattleContentIds.Abilities.BananaAttack, banana);
            Declare(catalog, BattlePresentationEventKind.DamageResolved,
                BattleContentIds.Abilities.DurianAttack,
                CombatFeedbackCatalogEntry.Concrete(durianDamage));
            Declare(catalog, BattlePresentationEventKind.DamageResolved,
                BattleContentIds.Statuses.ChiliBurn,
                CombatFeedbackCatalogEntry.Concrete(new CombatFeedbackProfile(
                    "feedback.burn.tick", PresentationVfxKind.BurnTick,
                    CombatFeedbackPriority.Ambient, .18f, targetFlash: .15f,
                    targetSquash: .025f,
                    floatingTextRole: CombatFloatingTextRole.PeriodicDamage,
                    mergeWindow: .12f,
                    audioRoute: CombatAudioRoute.LightImpact, minimumInterval: .18f)));

            Declare(catalog, BattlePresentationEventKind.StatusApplied,
                BattleContentIds.Statuses.IceSlow, ice);
            Declare(catalog, BattlePresentationEventKind.StatusApplied,
                BattleContentIds.Statuses.IceCount, CombatFeedbackCatalogEntry.None());
            Declare(catalog, BattlePresentationEventKind.StatusApplied,
                BattleContentIds.Statuses.ChiliBurn, chili);
            Declare(catalog, BattlePresentationEventKind.StatusApplied,
                BattleContentIds.Statuses.IceFreeze,
                CombatFeedbackCatalogEntry.None());
            Declare(catalog, BattlePresentationEventKind.StatusProcced,
                BattleContentIds.Statuses.IceFreeze,
                CombatFeedbackCatalogEntry.Concrete(new CombatFeedbackProfile(
                    "feedback.freeze.proc", PresentationVfxKind.FreezeProc,
                    CombatFeedbackPriority.Control, .58f, targetFlash: .85f,
                    targetSquash: .12f, targetDisplacement: 1f,
                    floatingTextRole: CombatFloatingTextRole.Control,
                    audioRoute: CombatAudioRoute.Control, minimumInterval: .08f)));

            Declare(catalog, BattlePresentationEventKind.ResourceGranted,
                SunResource, CombatFeedbackCatalogEntry.Concrete(new CombatFeedbackProfile(
                    "feedback.resource.sun", PresentationVfxKind.SunBurst,
                    CombatFeedbackPriority.Medium, .48f, attackerRecoil: 2f,
                    floatingTextRole: CombatFloatingTextRole.Resource,
                    mergeWindow: .1f,
                    audioRoute: CombatAudioRoute.Resource, minimumInterval: .08f)));

            var defeat = new CombatFeedbackProfile(
                "feedback.enemy.defeat", PresentationVfxKind.Defeat,
                CombatFeedbackPriority.Defeat, .62f,
                floatingTextRole: CombatFloatingTextRole.Defeat, mergeWindow: 0f,
                audioRoute: CombatAudioRoute.Defeat, minimumInterval: .05f,
                beatRole: CombatImpactBeatRole.Cluster);
            foreach (var enemy in new[]
            {
                BattleContentIds.Enemies.Normal, BattleContentIds.Enemies.Runner,
                BattleContentIds.Enemies.Armored,
            })
                Declare(catalog, BattlePresentationEventKind.EntityDefeated, enemy,
                    CombatFeedbackCatalogEntry.Concrete(defeat));
            Declare(catalog, BattlePresentationEventKind.EntityDefeated,
                BattleContentIds.Enemies.Boss,
                CombatFeedbackCatalogEntry.Concrete(new CombatFeedbackProfile(
                    "feedback.enemy.boss-defeat", PresentationVfxKind.Defeat,
                    CombatFeedbackPriority.Defeat, .62f,
                    floatingTextRole: CombatFloatingTextRole.Defeat, mergeWindow: 0f,
                    audioRoute: CombatAudioRoute.Defeat, minimumInterval: .05f,
                    beatRole: CombatImpactBeatRole.Terminal)));

            foreach (var stateId in BundledBattleStateIds())
                Declare(catalog, BattlePresentationEventKind.BattleStateChanged,
                    stateId, CombatFeedbackCatalogEntry.None());

            var issues = catalog.Validate();
            if (issues.Count > 0)
                throw new InvalidOperationException("Bundled combat feedback catalog is invalid: "
                    + string.Join("\n", issues));
            return catalog;
        }

        private static IEnumerable<CombatFeedbackKey> EmittableKeys(
            CompiledBattleContentCatalog content)
        {
            var result = new HashSet<CombatFeedbackKey>();
            foreach (var plant in content.Plants.Values)
            {
                AddAbilityKeys(content.ResolvePlantAbilities(plant.id, string.Empty),
                    string.Empty, result);
                foreach (var equipment in content.Equipment.Values.Where(value =>
                             value.compatiblePlantIds.Contains(plant.id)))
                    AddAbilityKeys(content.ResolvePlantAbilities(plant.id, equipment.id),
                        equipment.id, result);
            }
            foreach (var enemy in content.Enemies.Values)
            {
                AddAbilityKeys(content.ResolveEnemyAbilities(enemy.id), string.Empty, result);
                result.Add(new CombatFeedbackKey(
                    BattlePresentationEventKind.EntityDefeated, enemy.id));
            }
            foreach (var status in content.RuntimeStatuses.Values)
            {
                if (status.PeriodicEffect == CombatPeriodicEffectKind.Damage)
                    result.Add(new CombatFeedbackKey(
                        BattlePresentationEventKind.DamageResolved, status.Id));
                if (!string.IsNullOrEmpty(status.ProcStatusId))
                {
                    result.Add(new CombatFeedbackKey(
                        BattlePresentationEventKind.StatusProcced, status.ProcStatusId));
                    result.Add(new CombatFeedbackKey(
                        BattlePresentationEventKind.StatusApplied, status.ProcStatusId));
                }
            }
            foreach (var stateId in BundledBattleStateIds())
                result.Add(new CombatFeedbackKey(
                    BattlePresentationEventKind.BattleStateChanged, stateId));
            return result;
        }

        private static IEnumerable<string> BundledBattleStateIds()
        {
            yield return BattleContentIds.BattleStates.Ready;
            yield return BattleContentIds.BattleStates.WaveStarted;
            yield return BattleContentIds.BattleStates.CoreDamaged;
            yield return BattleContentIds.BattleStates.WaveCompleted;
            yield return BattleContentIds.BattleStates.MilestoneReward;
            yield return BattleContentIds.BattleStates.PotExpanded;
            yield return BattleContentIds.BattleStates.PlantMoved;
            yield return BattleContentIds.BattleStates.PlantMerged;
            yield return BattleContentIds.BattleStates.EquipmentInstalled;
        }

        private static void AddAbilityKeys(
            IEnumerable<CompiledAbilityDefinition> abilities, string equipmentId,
            ISet<CombatFeedbackKey> result)
        {
            foreach (var ability in abilities)
            {
                result.Add(new CombatFeedbackKey(BattlePresentationEventKind.AbilityStarted,
                    ability.Id, equipmentId));
                result.Add(new CombatFeedbackKey(BattlePresentationEventKind.AbilityReleased,
                    ability.Id, equipmentId));
                foreach (var delivery in ability.Deliveries)
                {
                    if (delivery.Kind == AbilityDeliveryKind.Projectile)
                        result.Add(new CombatFeedbackKey(
                            BattlePresentationEventKind.ProjectileLaunched,
                            delivery.ProjectileId, equipmentId));
                    foreach (var effect in delivery.Payload)
                    {
                        if (effect.Kind == AbilityPayloadEffectKind.Damage)
                            result.Add(new CombatFeedbackKey(
                                BattlePresentationEventKind.DamageResolved,
                                ability.Id, equipmentId));
                        else if (effect.Kind == AbilityPayloadEffectKind.ApplyStatus)
                            result.Add(new CombatFeedbackKey(
                                BattlePresentationEventKind.StatusApplied,
                                effect.StatusId, equipmentId));
                        else if (effect.Kind == AbilityPayloadEffectKind.GrantResource)
                            result.Add(new CombatFeedbackKey(
                                BattlePresentationEventKind.ResourceGranted,
                                SunResource, equipmentId));
                    }
                }
            }
        }

        private static CombatFeedbackCatalogEntry Recoil(string id, float duration,
            float amount)
        {
            return CombatFeedbackCatalogEntry.Concrete(new CombatFeedbackProfile(
                id, PresentationVfxKind.None, CombatFeedbackPriority.Light,
                duration, attackerRecoil: amount,
                audioRoute: CombatAudioRoute.Projectile, minimumInterval: .04f));
        }

        private static CombatFeedbackCatalogEntry MotionOnly(string id,
            float duration, float amount)
        {
            return CombatFeedbackCatalogEntry.Concrete(new CombatFeedbackProfile(
                id, PresentationVfxKind.None, CombatFeedbackPriority.Light,
                duration, attackerRecoil: amount));
        }

        private static CombatFeedbackCatalogEntry Impact(string id,
            PresentationVfxKind kind, CombatFeedbackPriority priority, float duration,
            CombatFloatingTextRole floatingTextRole, float mergeWindow, float displacement,
            CombatImpactBeatRole beatRole)
        {
            return CombatFeedbackCatalogEntry.Concrete(new CombatFeedbackProfile(
                id, kind, priority, duration, targetFlash: .72f,
                targetSquash: priority >= CombatFeedbackPriority.Heavy ? .15f : .08f,
                targetDisplacement: displacement, floatingTextRole: floatingTextRole,
                mergeWindow: mergeWindow, audioRoute: priority >= CombatFeedbackPriority.Heavy
                    ? CombatAudioRoute.HeavyImpact : CombatAudioRoute.LightImpact,
                minimumInterval: priority >= CombatFeedbackPriority.Heavy ? .04f : .08f,
                beatRole: beatRole));
        }

        private static void Declare(CombatFeedbackCatalog catalog,
            BattlePresentationEventKind kind, string semanticId,
            CombatFeedbackCatalogEntry entry, string sourceEquipmentId = "")
        {
            catalog.Declare(new CombatFeedbackKey(kind, semanticId,
                sourceEquipmentId), entry);
        }
    }
}
