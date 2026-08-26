using System;
using System.Collections.Generic;
using System.Linq;
using FruitDefense.Content;
using FruitDefense.Core;
using UnityEngine;

namespace FruitDefense.Editor
{
    public static class ComposableBattleAbilitiesSmoke
    {
        public static void Run()
        {
            var catalog = Compile(BundledBattleContentFactory.Create());
            ValidateCompiledContractsAndCache(catalog);
            ValidateStructuredFailures();
            ValidateDelayedReleaseAndSemanticTiming(catalog);
            ValidateProjectilePayloadAndNoImplicitMovementLock(catalog);
            ValidateEquipmentAbilities(catalog);
            ValidatePeriodicDamageDoesNotRetriggerOnHit(catalog);
            ValidateDataOnlyAbility();
            ValidateStableIdRuntime();
            Debug.Log("FRUIT_DEFENSE_COMPOSABLE_ABILITIES_OK");
        }

        private static void ValidateCompiledContractsAndCache(CompiledBattleContentCatalog catalog)
        {
            var pea = catalog.RuntimeAbilities[BattleContentIds.Abilities.PeaAttack];
            Assert(pea.Activation.Kind == AbilityActivationKind.Cooldown
                && pea.Deliveries.Single().Kind == AbilityDeliveryKind.Projectile,
                "pea compiles as a cooldown projectile Ability");
            var durian = catalog.RuntimeAbilities[BattleContentIds.Abilities.DurianAttack];
            Assert(durian.Timeline.WindupTicks == 8 && durian.Timeline.RecoveryTicks == 6
                && Mathf.Approximately(durian.Deliveries.Single().Radius, 18f),
                "durian uses fixed release/recovery ticks and authored radius");
            Assert(catalog.RuntimeProjectiles[BattleContentIds.Projectiles.Watermelon].FlightTicks == 8,
                "watermelon arc compiles to eight fixed ticks");

            var first = catalog.ResolvePlantAbilities(BattleContentIds.Plants.Pea,
                BattleContentIds.Equipment.Gatling);
            var second = catalog.ResolvePlantAbilities(BattleContentIds.Plants.Pea,
                BattleContentIds.Equipment.Gatling);
            Assert(ReferenceEquals(first, second), "resolved loadout is reused from the immutable cache");
            var modified = first.Single(value => value.Id == BattleContentIds.Abilities.PeaAttack);
            Assert(modified.BurstCount == 4 && modified.BurstIntervalTicks == 4,
                "typed gatling modifiers produce four fixed-interval shots");

            var iceProducer = catalog.ResolvePlantAbilities(BattleContentIds.Plants.Sunflower,
                BattleContentIds.Equipment.Ice);
            Assert(iceProducer.Any(value => value.Id == BattleContentIds.Abilities.IceProducerOpening)
                && iceProducer.All(value => value.Id != BattleContentIds.Abilities.IceOnHit),
                "equipment grant tag selects only the producer event Ability");
            var chiliProducer = catalog.ResolvePlantAbilities(BattleContentIds.Plants.Sunflower,
                    BattleContentIds.Equipment.Chili)
                .Single(value => value.Id == BattleContentIds.Abilities.SunflowerProduce);
            Assert(chiliProducer.Deliveries.SelectMany(value => value.Payload)
                    .Single(value => value.Kind == AbilityPayloadEffectKind.GrantResource)
                    .ResourceAmount == 2,
                "typed resource modifier updates the cached payload");
        }

        private static void ValidateStructuredFailures()
        {
            var unknown = BundledBattleContentFactory.Create();
            unknown.abilities[0].deliveries[0].modeId = "delivery.unknown";
            var result = BattleContentValidator.Validate(unknown);
            Assert(result.Issues.Any(issue => issue.code == "mechanism.unknown"
                    && issue.category == "abilities"),
                "unknown delivery is rejected before Battle");

            var zero = BundledBattleContentFactory.Create();
            zero.equipment.Single(value => value.id == BattleContentIds.Equipment.Gatling)
                .modifiers[0].targetAbilityTag = "ability.no-match";
            result = BattleContentValidator.Validate(zero);
            Assert(result.Issues.Any(issue => issue.code == "modifier.match.zero"),
                "zero-match typed modifier is rejected");

            var ambiguous = BundledBattleContentFactory.Create();
            var pea = ambiguous.plants.Single(value => value.id == BattleContentIds.Plants.Pea);
            pea.abilityIds = new[]
            {
                BattleContentIds.Abilities.PeaAttack,
                BattleContentIds.Abilities.WatermelonAttack,
            };
            result = BattleContentValidator.Validate(ambiguous);
            Assert(result.Issues.Any(issue => issue.code == "modifier.match.ambiguous"),
                "ambiguous typed modifier is rejected");

            var invalidResolvedValue = BundledBattleContentFactory.Create();
            invalidResolvedValue.equipment
                .Single(value => value.id == BattleContentIds.Equipment.Gatling)
                .modifiers.Single(value =>
                    value.attributeId == "ability-attribute.burst-interval").value = 0f;
            result = BattleContentValidator.Validate(invalidResolvedValue);
            Assert(result.Issues.Any(issue => issue.code == "modifier.result.invalid"),
                "modifier output cannot collapse required Ability timing to zero");

            var canonicalModifierOrder = BundledBattleContentFactory.Create();
            var gatling = canonicalModifierOrder.equipment.Single(value =>
                value.id == BattleContentIds.Equipment.Gatling);
            gatling.compatiblePlantIds = new[] { BattleContentIds.Plants.Pea };
            gatling.modifiers = gatling.modifiers.Concat(new[]
            {
                new AbilityModifierDefinitionDto
                {
                    id = "modifier.z-multiply-cooldown",
                    targetAbilityId = BattleContentIds.Abilities.PeaAttack,
                    attributeId = "ability-attribute.cooldown",
                    operationId = "ability-modifier.multiply",
                    value = .5f,
                },
                new AbilityModifierDefinitionDto
                {
                    id = "modifier.a-add-cooldown",
                    targetAbilityId = BattleContentIds.Abilities.PeaAttack,
                    attributeId = "ability-attribute.cooldown",
                    operationId = "ability-modifier.add",
                    value = 1f,
                },
            }).ToArray();
            var canonicalCompiled = Compile(canonicalModifierOrder);
            Assert(canonicalCompiled.ResolvePlantAbilities(BattleContentIds.Plants.Pea,
                    BattleContentIds.Equipment.Gatling).Single(value =>
                    value.Id == BattleContentIds.Abilities.PeaAttack)
                    .Activation.CooldownTicks == BattleAbilityTiming.SecondsToTicks(1f),
                "compiled runtime applies a-add before z-multiply regardless of author order");
            gatling.modifiers.Single(value =>
                value.id == "modifier.z-multiply-cooldown").value = 0f;
            result = BattleContentValidator.Validate(canonicalModifierOrder);
            Assert(result.Issues.Any(issue => issue.code == "modifier.result.invalid"),
                "derived validation applies non-commutative modifiers in canonical ID order");

            var quantizedCooldown = BundledBattleContentFactory.Create();
            quantizedCooldown.abilities.Single(value =>
                value.id == BattleContentIds.Abilities.PeaAttack).activation.cooldownSeconds = .01f;
            var cooldownEquipment = quantizedCooldown.equipment.Single(value =>
                value.id == BattleContentIds.Equipment.Gatling);
            cooldownEquipment.compatiblePlantIds = new[] { BattleContentIds.Plants.Pea };
            cooldownEquipment.modifiers = new[]
            {
                new AbilityModifierDefinitionDto
                {
                    id = "modifier.a-add-cooldown",
                    targetAbilityId = BattleContentIds.Abilities.PeaAttack,
                    attributeId = "ability-attribute.cooldown",
                    operationId = "ability-modifier.add",
                    value = -.02f,
                },
                new AbilityModifierDefinitionDto
                {
                    id = "modifier.b-multiply-cooldown",
                    targetAbilityId = BattleContentIds.Abilities.PeaAttack,
                    attributeId = "ability-attribute.cooldown",
                    operationId = "ability-modifier.multiply",
                    value = -1f,
                },
            };
            result = BattleContentValidator.Validate(quantizedCooldown);
            Assert(result.Issues.Any(issue => issue.code == "modifier.result.invalid"),
                "derived validation mirrors per-modifier cooldown tick quantization");

            var clampedResource = BundledBattleContentFactory.Create();
            var resourceEquipment = clampedResource.equipment.Single(value =>
                value.id == BattleContentIds.Equipment.Chili);
            resourceEquipment.compatiblePlantIds = new[] { BattleContentIds.Plants.Sunflower };
            resourceEquipment.modifiers = new[]
            {
                new AbilityModifierDefinitionDto
                {
                    id = "modifier.a-add-resource",
                    requiredPlantTag = "plant.producer",
                    targetAbilityId = BattleContentIds.Abilities.SunflowerProduce,
                    attributeId = "ability-attribute.resource-amount",
                    operationId = "ability-modifier.add",
                    value = -2f,
                },
                new AbilityModifierDefinitionDto
                {
                    id = "modifier.b-multiply-resource",
                    requiredPlantTag = "plant.producer",
                    targetAbilityId = BattleContentIds.Abilities.SunflowerProduce,
                    attributeId = "ability-attribute.resource-amount",
                    operationId = "ability-modifier.multiply",
                    value = -1f,
                },
            };
            result = BattleContentValidator.Validate(clampedResource);
            Assert(result.Issues.Any(issue => issue.code == "modifier.result.invalid"),
                "derived validation mirrors per-modifier resource rounding and non-negative clamp");

            var periodicCooldown = BundledBattleContentFactory.Create();
            var periodicEquipment = periodicCooldown.equipment.Single(value =>
                value.id == BattleContentIds.Equipment.Chili);
            periodicEquipment.compatiblePlantIds = new[] { BattleContentIds.Plants.Sunflower };
            periodicEquipment.modifiers = new[]
            {
                new AbilityModifierDefinitionDto
                {
                    id = "modifier.periodic-cooldown",
                    requiredPlantTag = "plant.producer",
                    targetAbilityId = BattleContentIds.Abilities.SunflowerProduce,
                    attributeId = "ability-attribute.cooldown",
                    operationId = "ability-modifier.add",
                    value = 1f,
                },
            };
            result = BattleContentValidator.Validate(periodicCooldown);
            Assert(result.Issues.Any(issue => issue.code == "modifier.attribute.inapplicable"),
                "periodic Ability rejects a cooldown modifier that its executor would ignore");

            var eventCooldown = BundledBattleContentFactory.Create();
            var iceEquipment = eventCooldown.equipment.Single(value =>
                value.id == BattleContentIds.Equipment.Ice);
            iceEquipment.compatiblePlantIds = new[] { BattleContentIds.Plants.Pea };
            iceEquipment.modifiers = new[]
            {
                new AbilityModifierDefinitionDto
                {
                    id = "modifier.event-cooldown",
                    requiredPlantTag = "plant.damage",
                    targetAbilityId = BattleContentIds.Abilities.IceOnHit,
                    attributeId = "ability-attribute.cooldown",
                    operationId = "ability-modifier.add",
                    value = 1f,
                },
            };
            result = BattleContentValidator.Validate(eventCooldown);
            Assert(result.IsValid, "combat-event Ability accepts its supported cooldown modifier");
            var eventCompiled = Compile(eventCooldown);
            Assert(eventCompiled.ResolvePlantAbilities(BattleContentIds.Plants.Pea,
                    BattleContentIds.Equipment.Ice).Single(value =>
                    value.Id == BattleContentIds.Abilities.IceOnHit)
                    .Activation.CooldownTicks == BattleAbilityTiming.SecondsToTicks(1f),
                "combat-event cooldown modifier is consumed by the runtime");

            var procCycle = BundledBattleContentFactory.Create();
            procCycle.statuses.Single(value =>
                value.id == BattleContentIds.Statuses.IceCount).procStatusId =
                BattleContentIds.Statuses.IceCount;
            result = BattleContentValidator.Validate(procCycle);
            Assert(result.Issues.Any(issue => issue.code == "status.proc.cycle"),
                "status proc graph cycles are rejected before recursive execution");

            var eventTargetWithoutEvent = BundledBattleContentFactory.Create();
            eventTargetWithoutEvent.abilities.Single(value =>
                value.id == BattleContentIds.Abilities.PeaAttack)
                .deliveries[0].targetId = "target.event-target";
            result = BattleContentValidator.Validate(eventTargetWithoutEvent);
            Assert(result.Issues.Any(issue => issue.code == "ability.target.unsupported"),
                "poll-driven Ability cannot declare an unavailable event target");

            var unsupportedEnemyExecutor = BundledBattleContentFactory.Create();
            unsupportedEnemyExecutor.enemies.Single(value =>
                value.id == BattleContentIds.Enemies.Normal).abilityIds =
                new[] { BattleContentIds.Abilities.PeaAttack };
            result = BattleContentValidator.Validate(unsupportedEnemyExecutor);
            Assert(result.Issues.Any(issue => issue.code == "ability.owner.unsupported"),
                "enemy assignments reject unpolled execution paths");

            var unsupportedEnemyTarget = BundledBattleContentFactory.Create();
            var enemyAbility = unsupportedEnemyTarget.abilities.Single(value =>
                value.id == BattleContentIds.Abilities.IceOnHit);
            enemyAbility.activation.eventId = "event.after-damage-taken";
            enemyAbility.activation.ownerRoleId = "owner.event-target";
            enemyAbility.deliveries[0].targetId = "target.front";
            unsupportedEnemyTarget.enemies.Single(value =>
                value.id == BattleContentIds.Enemies.Normal).abilityIds =
                new[] { enemyAbility.id };
            result = BattleContentValidator.Validate(unsupportedEnemyTarget);
            Assert(result.Issues.Any(issue => issue.code == "ability.target.unsupported"),
                "enemy event Ability rejects target selectors the executor cannot fulfill");
        }

        private static void ValidateDelayedReleaseAndSemanticTiming(
            CompiledBattleContentCatalog catalog)
        {
            var simulation = CreateScenario(catalog, BattleContentIds.Plants.Durian);
            simulation.Step();
            var target = simulation.State.Zombies.Single();
            Assert(Mathf.Approximately(target.Hp, 1000f),
                "durian activation does not damage before release");
            for (var tick = 0; tick < 7; tick++) simulation.Step();
            Assert(Mathf.Approximately(target.Hp, 1000f),
                "durian remains non-damaging through the authored windup");
            simulation.Step();
            Assert(target.Hp < 1000f, "durian damage resolves on its release tick");
            var events = Drain(simulation);
            var release = events.Last(value => value.Kind == BattlePresentationEventKind.AbilityReleased
                && value.AbilityId == BattleContentIds.Abilities.DurianAttack);
            var damage = events.Last(value => value.Kind == BattlePresentationEventKind.DamageResolved
                && value.AbilityId == BattleContentIds.Abilities.DurianAttack);
            Assert(release.LogicTick == damage.LogicTick,
                "durian semantic release and resolved damage share one logic tick");
        }

        private static void ValidateProjectilePayloadAndNoImplicitMovementLock(
            CompiledBattleContentCatalog catalog)
        {
            var simulation = CreateScenario(catalog, BattleContentIds.Plants.Pea);
            simulation.Step();
            Assert(simulation.State.Projectiles.Count == 1
                && simulation.State.Projectiles[0].AbilityId == BattleContentIds.Abilities.PeaAttack
                && simulation.State.Projectiles[0].DeliveryIndex == 0,
                "projectile stores its Ability delivery identity");
            TickUntilProjectilesFinish(simulation, 60);
            var target = simulation.State.Zombies.Single();
            Assert(target.Hp < 1000f && target.Statuses.All(status =>
                    !catalog.RuntimeStatuses[status.DefinitionId].BlocksMovement),
                "ordinary projectile damage adds no implicit movement-blocking status");
        }

        private static void ValidateEquipmentAbilities(CompiledBattleContentCatalog catalog)
        {
            var gatling = CreateScenario(catalog, BattleContentIds.Plants.Pea,
                BattleContentIds.Equipment.Gatling);
            for (var tick = 0; tick < 14; tick++) gatling.Step();
            var events = Drain(gatling);
            Assert(events.Count(value => value.Kind == BattlePresentationEventKind.ProjectileLaunched
                    && value.AbilityId == BattleContentIds.Abilities.PeaAttack
                    && value.SourceEquipmentId == BattleContentIds.Equipment.Gatling) == 4,
                "gatling burst emits four launches with real equipment identity");

            var ice = CreateScenario(catalog, BattleContentIds.Plants.Pea,
                BattleContentIds.Equipment.Ice);
            ice.Step();
            TickUntilProjectilesFinish(ice, 60);
            Assert(ice.State.Zombies.Single().Statuses.Any(value =>
                    value.DefinitionId == BattleContentIds.Statuses.IceSlow)
                && ice.State.Zombies.Single().Statuses.Any(value =>
                    value.DefinitionId == BattleContentIds.Statuses.IceCount),
                "event-activated ice Ability observes projectile damage");

            var producer = CreateScenario(catalog, BattleContentIds.Plants.Sunflower,
                BattleContentIds.Equipment.Chili);
            producer.Step();
            var runtime = producer.State.Plants.Single().AbilityRuntimes
                .Single(value => value.AbilityId == BattleContentIds.Abilities.SunflowerProduce);
            producer.State.Sun = 0;
            runtime.PeriodicProgressTicks = 199;
            producer.Step();
            Assert(producer.State.Sun == 2,
                "periodic producer executes the modified resource payload");
        }

        private static void ValidatePeriodicDamageDoesNotRetriggerOnHit(
            CompiledBattleContentCatalog catalog)
        {
            var simulation = CreateScenario(catalog, BattleContentIds.Plants.Pea,
                BattleContentIds.Equipment.Chili);
            simulation.Step();
            simulation.State.Plants.Single().AbilityRuntimes
                .Single(value => value.AbilityId == BattleContentIds.Abilities.PeaAttack)
                .CooldownTicks = 10000;
            TickUntilProjectilesFinish(simulation, 60);

            var target = simulation.State.Zombies.Single();
            var burns = target.Statuses.Where(value =>
                value.DefinitionId == BattleContentIds.Statuses.ChiliBurn).ToArray();
            Assert(burns.Length == 1,
                "one direct chili-equipped hit creates exactly one burn stack");
            var initialRemainingTicks = burns[0].RemainingTicks;
            Assert(initialRemainingTicks == catalog.RuntimeStatuses[BattleContentIds.Statuses.ChiliBurn]
                    .DurationTicks,
                "new burn starts with its authored duration");

            for (var tick = 0; tick < initialRemainingTicks; tick++)
            {
                simulation.Step();
                burns = target.Statuses.Where(value =>
                    value.DefinitionId == BattleContentIds.Statuses.ChiliBurn).ToArray();
                if (tick + 1 >= initialRemainingTicks) continue;
                Assert(burns.Length == 1
                    && burns[0].RemainingTicks == initialRemainingTicks - tick - 1,
                    "periodic burn damage neither adds nor refreshes on-hit burn stacks");
            }
            Assert(target.Statuses.All(value =>
                    value.DefinitionId != BattleContentIds.Statuses.ChiliBurn),
                "burn clears after the authored fixed-tick duration");
        }

        private static void ValidateDataOnlyAbility()
        {
            var authored = BundledBattleContentFactory.Create();
            const string abilityId = "ability.test.frost-pea";
            const string plantId = "plant.test-frost-pea";
            authored.abilities = authored.abilities.Concat(new[]
            {
                new AbilityDefinitionDto
                {
                    id = abilityId,
                    activation = new AbilityActivationDefinitionDto
                    {
                        kindId = "activation.cooldown",
                        ownerRoleId = "owner.any",
                        cooldownSeconds = 1f,
                    },
                    timeline = new AbilityTimelineDefinitionDto(),
                    tags = new[] { "ability.damage", "ability.ranged.projectile" },
                    deliveries = new[]
                    {
                        new AbilityDeliveryDefinitionDto
                        {
                            targetId = "target.front",
                            modeId = "delivery.projectile",
                            projectileId = BattleContentIds.Projectiles.Pea,
                            payload = new[]
                            {
                                new AbilityPayloadEffectDefinitionDto
                                {
                                    kindId = "effect.damage",
                                    magnitude = 1f,
                                },
                                new AbilityPayloadEffectDefinitionDto
                                {
                                    kindId = "effect.apply-status",
                                    statusId = BattleContentIds.Statuses.IceSlow,
                                    magnitude = 1f,
                                },
                            },
                        },
                    },
                },
            }).ToArray();
            authored.plants = authored.plants.Concat(new[]
            {
                new PlantDefinitionDto
                {
                    id = plantId,
                    displayName = "Data-only frost pea",
                    description = "Smoke-only composition",
                    damage = 12f,
                    attackIntervalSeconds = 1f,
                    rangeLegacyUnits = 44f,
                    abilityIds = new[] { abilityId },
                    tags = new[] { "plant.damage", "plant.projectile", "plant.ranged" },
                },
            }).ToArray();
            var compiled = Compile(authored);
            var simulation = CreateScenario(compiled, plantId);
            simulation.Step();
            Assert(simulation.State.Zombies.Single().Statuses.Count == 0,
                "projectile status payload does not resolve at launch");
            simulation.State.Plants.Clear();
            var export = simulation.ExportSnapshot();
            Assert(!export.Succeeded
                && export.Code == BattleSnapshotExportCode.UnsupportedSessionSource,
                "content-direct custom simulation rejects snapshot export explicitly");
            TickUntilProjectilesFinish(simulation, 60);
            Assert(simulation.State.Zombies.Single().Statuses.Any(status =>
                    status.DefinitionId == BattleContentIds.Statuses.IceSlow),
                "data-only projectile resolves flat damage and status payload at impact");
        }

        private static void ValidateStableIdRuntime()
        {
            const string customEnemyId = "enemy.test.sprout";
            const string customEquipmentId = "equipment.test.focus";
            const string enemyAbilityId = "ability.test.enemy.guard";
            var authored = BundledBattleContentFactory.Create();
            authored.abilities = authored.abilities.Concat(new[]
            {
                new AbilityDefinitionDto
                {
                    id = enemyAbilityId,
                    activation = new AbilityActivationDefinitionDto
                    {
                        kindId = "activation.combat-event",
                        eventId = "event.after-damage-taken",
                        ownerRoleId = "owner.event-target",
                        cooldownSeconds = 1f,
                    },
                    timeline = new AbilityTimelineDefinitionDto(),
                    damageMultiplier = 0f,
                    tags = new[] { "ability.enemy.reactive" },
                    deliveries = new[]
                    {
                        new AbilityDeliveryDefinitionDto
                        {
                            targetId = "target.self",
                            modeId = "delivery.instant",
                            payload = new[]
                            {
                                new AbilityPayloadEffectDefinitionDto
                                {
                                    kindId = "effect.apply-status",
                                    statusId = BattleContentIds.Statuses.IceSlow,
                                    magnitude = 1f,
                                },
                            },
                        },
                    },
                },
            }).ToArray();
            authored.enemies.Single(value => value.id == BattleContentIds.Enemies.Normal)
                .abilityIds = new[] { enemyAbilityId };
            authored.enemies = authored.enemies.Concat(new[]
            {
                new EnemyDefinitionDto
                {
                    id = customEnemyId,
                    displayName = "Data-only sprout",
                    health = 50f,
                    speedLegacyUnits = 4f,
                    killReward = 2,
                    threat = 1,
                    tags = new[] { "enemy", "enemy.test" },
                },
            }).ToArray();
            authored.equipment = authored.equipment.Concat(new[]
            {
                new EquipmentDefinitionDto
                {
                    id = customEquipmentId,
                    displayName = "Data-only focus",
                    compatiblePlantIds = new[] { BattleContentIds.Plants.Pea },
                },
            }).ToArray();
            var customEquipmentPlant = authored.plants.Single(value =>
                value.id == BattleContentIds.Plants.Pea);
            customEquipmentPlant.allowedEquipmentIds = customEquipmentPlant.allowedEquipmentIds
                .Concat(new[] { customEquipmentId }).ToArray();
            authored.waves.Single(value => value.id == "wave.01").enemyIds =
                new[] { customEnemyId };
            var compiled = Compile(authored);

            var waveSimulation = new GameSimulation(compiled, 11);
            string reason;
            Assert(waveSimulation.StartWave(out reason), "custom wave starts");
            waveSimulation.Step();
            Assert(waveSimulation.State.Zombies.Single().DefinitionId == customEnemyId,
                "custom enemy wave spawn preserves its stable definition ID");

            var equipped = CreateScenario(compiled, BattleContentIds.Plants.Pea);
            equipped.State.Inventory.Add(customEquipmentId, 2);
            Assert(equipped.InstallEquipment(9001, customEquipmentId, out reason)
                && equipped.State.Plants.Single().EquipmentId == customEquipmentId
                && equipped.State.Inventory.Get(customEquipmentId) == 1,
                "custom equipment installs through the stable-ID inventory contract");
            var export = equipped.ExportSnapshot();
            Assert(!export.Succeeded
                && export.Code == BattleSnapshotExportCode.UnsupportedSessionSource
                && equipped.State.Plants.Single().EquipmentId == customEquipmentId
                && equipped.State.Inventory.Get(customEquipmentId) == 1,
                "content-direct custom equipment session rejects snapshots without changing state");

            var reactive = CreateScenario(compiled, BattleContentIds.Plants.Pea);
            reactive.Step();
            TickUntilProjectilesFinish(reactive, 60);
            Assert(reactive.State.Zombies.Single().Statuses.Any(value =>
                    value.DefinitionId == BattleContentIds.Statuses.IceSlow),
                "validated enemy event Ability executes its supported self-status payload");
        }

        private static GameSimulation CreateScenario(CompiledBattleContentCatalog catalog,
            string plantId, string equipmentId = "")
        {
            var simulation = new GameSimulation(catalog, 7777);
            simulation.State.Plants.Clear();
            simulation.State.Zombies.Clear();
            simulation.State.Projectiles.Clear();
            simulation.DiscardPendingPresentationEvents();
            simulation.State.Phase = GamePhase.Playing;
            simulation.State.WaveIndex = 1;
            simulation.State.WaveTotal = 1;
            simulation.State.WaveSpawned = 1;
            simulation.State.NextId = 10000;
            var pot = simulation.State.Pots[0];
            simulation.State.Plants.Add(new Plant
            {
                Id = 9001,
                DefinitionId = plantId,
                EquipmentId = equipmentId,
                Star = 1,
                PotId = pot.Id,
                NurseryIndex = -1,
            });
            simulation.State.Zombies.Add(new Zombie
            {
                Id = 9002,
                DefinitionId = BattleContentIds.Enemies.Normal,
                RouteId = simulation.Map.PrimaryRouteId,
                Hp = 1000f,
                MaxHp = 1000f,
                Speed = 0f,
                PathProgress = NearestPathProgress(simulation, simulation.PotPoint(pot)),
                Reward = 0,
                Threat = 1,
            });
            return simulation;
        }

        private static List<BattlePresentationEvent> Drain(GameSimulation simulation)
        {
            var events = new List<BattlePresentationEvent>();
            simulation.DrainPresentationEvents(events);
            return events;
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
                simulation.Step();
        }

        private static CompiledBattleContentCatalog Compile(BattleContentCatalogDto authored)
        {
            CompiledBattleContentCatalog compiled;
            ContentValidationResult validation;
            if (BattleContentCompiler.TryCompile(authored, out compiled, out validation)) return compiled;
            throw new InvalidOperationException("Catalog compile failed:\n"
                + string.Join("\n", validation.Issues.Select(issue => issue.ToString()).ToArray()));
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(
                "Composable Ability validation failed: " + message);
        }
    }
}
