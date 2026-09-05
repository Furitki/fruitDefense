using System;
using System.Collections;
using System.IO;
using System.Text;
using FruitDefense.App.Services;
using FruitDefense.Content;
using UnityEngine;

namespace FruitDefense.Editor
{
    public static class PlayerProgressionFixtureSmoke
    {
        private const string DustItemId = "item.growth.sun-dust";
        private const string SecondEquipmentId =
            "growth-equipment.rainbud-charm";
        private const string BranchNodeId = "cultivation.sunlit-branch";

        public static void Run()
        {
            var content = CreateContent();
            ValidateProfileSchema(content);
            ValidateEditorAndWebStores(content);
            ValidateClaimTransactions(content);
            ValidateGrowthTransactions(content);
            Debug.Log("PLAYER_PROGRESSION_FIXTURE_OK");
        }

        private static void ValidateProfileSchema(
            CompiledOutgameContentCatalog content)
        {
            var profile = CreatePopulatedProfile();
            var validation = PlayerProfileCodec.Validate(profile, content);
            Assert(validation.Success, "valid current profile is accepted");

            var json = PlayerProfileCodec.Serialize(profile, content);
            var roundTrip = PlayerProfileCodec.TryDeserialize(json, content,
                out var restored);
            Assert(roundTrip.Success
                && restored.itemBalances.Length == 2
                && restored.ownedGrowthEquipment.Length == 2
                && restored.itemBalances[0].itemId
                    == OutgameContentIds.Items.MorningDew
                && restored.ownedGrowthEquipment[0].growthEquipmentId
                    == SecondEquipmentId,
                "serialization round-trips and normalizes collections by ordinal ID");

            var clone = PlayerProfileCodec.Clone(restored, content);
            clone.itemBalances[0].quantity++;
            clone.ownedGrowthEquipment[0].rank = 1;
            Assert(restored.itemBalances[0].quantity
                    != clone.itemBalances[0].quantity
                && restored.ownedGrowthEquipment[0].rank
                    != clone.ownedGrowthEquipment[0].rank,
                "profile clone owns independent nested collection entries");

            var projection = PlayerProgressionProjection.Create(restored, content);
            restored.itemBalances[0].quantity = 999;
            Assert(projection.ItemQuantity(
                       OutgameContentIds.Items.MorningDew) == 30
                && projection.HasReceipt(
                    OutgameContentIds.Receipts.StarterSupplies)
                && projection.TryGetEquipped(
                    OutgameContentIds.GrowthSlots.Offense, out var equipped)
                && equipped == OutgameContentIds.GrowthEquipment.SunleafEmblem,
                "immutable projection is detached from mutable profile DTOs");

            AssertInvalid(content, profile,
                value => value.itemBalances = new[]
                {
                    new PlayerItemBalance
                    {
                        itemId = OutgameContentIds.Items.MorningDew,
                        quantity = 1,
                    },
                    new PlayerItemBalance
                    {
                        itemId = OutgameContentIds.Items.MorningDew,
                        quantity = 2,
                    },
                }, ProfileValidationCode.DuplicateItemBalance,
                "duplicate balances are rejected");
            AssertInvalid(content, profile,
                value => value.itemBalances[0].quantity = -1,
                ProfileValidationCode.InvalidItemQuantity,
                "negative balances are rejected");
            AssertInvalid(content, profile,
                value => value.itemBalances[0].itemId = "item.unknown",
                ProfileValidationCode.UnknownItem,
                "unknown item identities are rejected");
            AssertInvalid(content, profile,
                value => value.activityReceipts = new[]
                {
                    new PlayerActivityReceipt
                    {
                        receiptId = OutgameContentIds.Receipts.StarterSupplies,
                    },
                    new PlayerActivityReceipt
                    {
                        receiptId = OutgameContentIds.Receipts.StarterSupplies,
                    },
                }, ProfileValidationCode.DuplicateActivityReceipt,
                "duplicate receipts are rejected");
            AssertInvalid(content, profile,
                value => value.activityReceipts[0].receiptId = "receipt.unknown",
                ProfileValidationCode.UnknownActivityReceipt,
                "unknown receipts are rejected");
            AssertInvalid(content, profile,
                value => value.ownedGrowthEquipment[0].rank = 9,
                ProfileValidationCode.InvalidGrowthEquipmentRank,
                "out-of-range equipment ranks are rejected");
            AssertInvalid(content, profile,
                value => value.growthLoadout[0].slotId = "growth-slot.defense",
                ProfileValidationCode.InvalidGrowthEquipmentSlot,
                "illegal loadout slots are rejected");
            AssertInvalid(content, profile,
                value => value.cultivationRanks = new[]
                {
                    new PlayerCultivationRank
                    {
                        cultivationNodeId = BranchNodeId,
                        rank = 1,
                    },
                }, ProfileValidationCode.InvalidCultivationPrerequisite,
                "stored cultivation prerequisites are enforced");
            AssertInvalid(content, profile,
                value => value.itemBalances = null,
                ProfileValidationCode.MissingCollection,
                "missing profile collections are rejected");

            var unsupported =
                "{\"schemaVersion\":1,\"profileId\":\"ignored\"}";
            var unsupportedResult = PlayerProfileCodec.TryDeserialize(unsupported,
                content, out var unsupportedProfile);
            Assert(unsupportedResult.Code == ProfileValidationCode.UnsupportedSchema
                && unsupportedProfile == null,
                "obsolete schema is reported without interpretation or migration");
        }

        private static void ValidateEditorAndWebStores(
            CompiledOutgameContentCatalog content)
        {
            var root = Path.Combine(Path.GetTempPath(),
                "fruit-defense-current-profile-" + Guid.NewGuid().ToString("N"));
            try
            {
                var backend = new EditorFileProfileBackend(root);
                var store = new LocalPlayerProfileStore(backend, content);
                ProfileLoadResult loaded = null;
                Drain(store.Load(value => loaded = value));
                Assert(loaded != null
                    && loaded.Status == ProfileLoadStatus.DefaultCreated
                    && loaded.HasProfile,
                    "Editor file store creates the current default profile");

                loaded.Profile.itemBalances = new[]
                {
                    new PlayerItemBalance
                    {
                        itemId = OutgameContentIds.Items.MorningDew,
                        quantity = 8,
                    },
                };
                ProfileSaveResult saved = null;
                Drain(store.Save(loaded.Profile, value => saved = value));
                Assert(saved != null && saved.Status == ProfileSaveStatus.Success
                    && saved.Profile.revision == loaded.Profile.revision + 1,
                    "Editor file store commits one complete new revision");

                ProfileLoadResult reloaded = null;
                Drain(store.Load(value => reloaded = value));
                Assert(reloaded.Status == ProfileLoadStatus.Success
                    && reloaded.Profile.itemBalances[0].quantity == 8,
                    "Editor file store round-trips progression collections");

                File.WriteAllText(backend.PrimaryPath,
                    "{\"schemaVersion\":1}", new UTF8Encoding(false));
                ProfileLoadResult unsupported = null;
                Drain(store.Load(value => unsupported = value));
                Assert(unsupported.Status == ProfileLoadStatus.UnsupportedSchema
                    && !unsupported.HasProfile
                    && File.Exists(backend.PrimaryPath),
                    "unsupported schema remains untouched until explicit reset");

                ProfileLoadResult reset = null;
                Drain(store.Reset(value => reset = value));
                Assert(reset.Status == ProfileLoadStatus.ResetCreated
                    && reset.HasProfile && reset.Profile.revision == 0,
                    "explicit reset replaces unsupported storage with a fresh profile");
            }
            finally
            {
                if (Directory.Exists(root)) Directory.Delete(root, true);
            }

            var webBackend = new WebPlayerPrefsProfileBackend(
                "fruit-defense.progression-fixture." + Guid.NewGuid().ToString("N"));
            try
            {
                var webStore = new LocalPlayerProfileStore(webBackend, content);
                ProfileLoadResult loaded = null;
                Drain(webStore.Load(value => loaded = value));
                loaded.Profile.activityReceipts = new[]
                {
                    new PlayerActivityReceipt
                    {
                        receiptId = OutgameContentIds.Receipts.StarterSupplies,
                    },
                };
                ProfileSaveResult saved = null;
                Drain(webStore.Save(loaded.Profile, value => saved = value));
                ProfileLoadResult reloaded = null;
                Drain(webStore.Load(value => reloaded = value));
                Assert(saved.Status == ProfileSaveStatus.Success
                    && reloaded.Profile.activityReceipts.Length == 1
                    && Encoding.UTF8.GetByteCount(webBackend.ReadPrimary().Json)
                        < WebPlayerPrefsProfileBackend.MaximumProfileBytes,
                    "Web PlayerPrefs store persists complete bounded current profiles");

                Assert(webBackend.TryWriteAtomically(
                        "{\"schemaVersion\":1}", out var writeError),
                    "Web fixture installs an unsupported payload: " + writeError);
                ProfileLoadResult unsupported = null;
                Drain(webStore.Load(value => unsupported = value));
                Assert(unsupported.Status == ProfileLoadStatus.UnsupportedSchema
                    && !unsupported.HasProfile,
                    "Web PlayerPrefs reports unsupported schema explicitly");
                ProfileLoadResult reset = null;
                Drain(webStore.Reset(value => reset = value));
                Assert(reset.Status == ProfileLoadStatus.ResetCreated
                    && reset.HasProfile,
                    "Web PlayerPrefs supports the same explicit reset workflow");
            }
            finally
            {
                webBackend.ClearForTesting();
            }
        }

        private static void ValidateClaimTransactions(
            CompiledOutgameContentCatalog content)
        {
            var store = new ControlledProfileStore(content);
            var service = new PlayerProgressionService(store, content,
                PlayerProfile.CreateDefault());
            var publicationCount = 0;
            service.ProjectionPublished += _ => publicationCount++;

            PlayerProgressionCommandResult claimed = null;
            Drain(service.TryClaimActivity(
                OutgameContentIds.Activities.StarterSupplies,
                value => claimed = value));
            Assert(claimed.Succeeded && store.SaveCount == 1
                && publicationCount == 1 && service.Current.Revision == 1
                && service.Current.ItemQuantity(
                    OutgameContentIds.Items.MorningDew) == 30
                && service.Current.ItemQuantity(DustItemId) == 3
                && service.Current.HasReceipt(
                    OutgameContentIds.Receipts.StarterSupplies)
                && service.Current.TryGetGrowthEquipmentRank(
                    OutgameContentIds.GrowthEquipment.SunleafEmblem,
                    out var starterRank) && starterRank == 0,
                "claim commits every grant and one receipt in one revision/save");

            PlayerProgressionCommandResult duplicate = null;
            Drain(service.TryClaimActivity(
                OutgameContentIds.Activities.StarterSupplies,
                value => duplicate = value));
            Assert(duplicate.Status
                    == PlayerProgressionCommandStatus.AlreadyClaimed
                && store.SaveCount == 1 && publicationCount == 1
                && service.Current.Revision == 1,
                "duplicate claim after receipt grants and publishes nothing");

            var delayedStore = new ControlledProfileStore(content)
            {
                SaveDelayFrames = 2,
            };
            var delayedService = new PlayerProgressionService(delayedStore,
                content, PlayerProfile.CreateDefault());
            PlayerProgressionCommandResult first = null;
            var firstRoutine = delayedService.TryClaimActivity(
                OutgameContentIds.Activities.StarterSupplies,
                value => first = value);
            Assert(firstRoutine.MoveNext() && delayedService.CommandInProgress,
                "first claim remains explicitly in progress while saving");
            PlayerProgressionCommandResult repeatedClick = null;
            Drain(delayedService.TryClaimActivity(
                OutgameContentIds.Activities.StarterSupplies,
                value => repeatedClick = value));
            Assert(repeatedClick.Status
                    == PlayerProgressionCommandStatus.InProgress
                && delayedStore.SaveCount == 1,
                "duplicate click while saving does not create a second save");
            Drain(firstRoutine);
            Assert(first.Succeeded && delayedService.Current.Revision == 1,
                "the original delayed claim commits once");

            var failingStore = new ControlledProfileStore(content)
            {
                FailWrites = true,
            };
            var failingService = new PlayerProgressionService(failingStore,
                content, PlayerProfile.CreateDefault());
            PlayerProgressionCommandResult failed = null;
            Drain(failingService.TryClaimActivity(
                OutgameContentIds.Activities.StarterSupplies,
                value => failed = value));
            Assert(failed.Status
                    == PlayerProgressionCommandStatus.PersistenceFailed
                && failingService.Current.Revision == 0
                && failingService.Current.ItemBalances.Count == 0
                && failingService.Current.ActivityReceiptIds.Count == 0
                && failingService.Current.OwnedGrowthEquipment.Count == 0,
                "save failure rolls back grants, ownership, receipt, and revision");

            var capConflict = PlayerProfile.CreateDefault();
            capConflict.itemBalances = new[]
            {
                new PlayerItemBalance
                {
                    itemId = DustItemId,
                    quantity = 1000,
                },
            };
            var conflictStore = new ControlledProfileStore(content);
            var conflictService = new PlayerProgressionService(conflictStore,
                content, capConflict);
            PlayerProgressionCommandResult conflict = null;
            Drain(conflictService.TryClaimActivity(
                OutgameContentIds.Activities.StarterSupplies,
                value => conflict = value));
            Assert(conflict.Status == PlayerProgressionCommandStatus.GrantConflict
                && conflictStore.SaveCount == 0
                && conflictService.Current.ItemQuantity(
                    OutgameContentIds.Items.MorningDew) == 0
                && conflictService.Current.OwnedGrowthEquipment.Count == 0
                && !conflictService.Current.HasReceipt(
                    OutgameContentIds.Receipts.StarterSupplies),
                "later grant failure discards every earlier candidate mutation");
        }

        private static void ValidateGrowthTransactions(
            CompiledOutgameContentCatalog content)
        {
            var insufficient = PlayerProfile.CreateDefault();
            insufficient.itemBalances = new[]
            {
                new PlayerItemBalance
                {
                    itemId = OutgameContentIds.Items.MorningDew,
                    quantity = 30,
                },
                new PlayerItemBalance { itemId = DustItemId, quantity = 1 },
            };
            insufficient.ownedGrowthEquipment = new[]
            {
                new PlayerGrowthEquipment
                {
                    growthEquipmentId =
                        OutgameContentIds.GrowthEquipment.SunleafEmblem,
                    rank = 0,
                },
            };
            var insufficientStore = new ControlledProfileStore(content);
            var insufficientService = new PlayerProgressionService(
                insufficientStore, content, insufficient);
            var emptyStore = new ControlledProfileStore(content);
            var emptyService = new PlayerProgressionService(emptyStore, content,
                PlayerProfile.CreateDefault());
            PlayerProgressionCommandResult notOwned = null;
            Drain(emptyService.TryEquip(
                OutgameContentIds.GrowthEquipment.SunleafEmblem,
                OutgameContentIds.GrowthSlots.Offense,
                value => notOwned = value));
            Assert(notOwned.Status == PlayerProgressionCommandStatus.NotOwned
                && emptyStore.SaveCount == 0,
                "equip rejects equipment not owned by the profile");

            PlayerProgressionCommandResult insufficientResult = null;
            Drain(insufficientService.TryUpgradeGrowthEquipment(
                OutgameContentIds.GrowthEquipment.SunleafEmblem,
                value => insufficientResult = value));
            Assert(insufficientResult.Status
                    == PlayerProgressionCommandStatus.InsufficientCost
                && insufficientResult.RelatedIdentity == DustItemId
                && insufficientResult.RequiredQuantity == 2
                && insufficientResult.AvailableQuantity == 1
                && insufficientStore.SaveCount == 0
                && insufficientService.Current.ItemQuantity(
                    OutgameContentIds.Items.MorningDew) == 30,
                "incomplete multi-item cost debits nothing");

            var profile = CreatePopulatedProfile();
            profile.activityReceipts = Array.Empty<PlayerActivityReceipt>();
            profile.growthLoadout = new[]
            {
                new PlayerGrowthLoadoutEntry
                {
                    slotId = OutgameContentIds.GrowthSlots.Offense,
                    growthEquipmentId =
                        OutgameContentIds.GrowthEquipment.SunleafEmblem,
                },
            };
            var store = new ControlledProfileStore(content);
            var service = new PlayerProgressionService(store, content, profile);
            var publications = 0;
            service.ProjectionPublished += _ => publications++;

            PlayerProgressionCommandResult incompatible = null;
            Drain(service.TryEquip(SecondEquipmentId, "growth-slot.defense",
                value => incompatible = value));
            Assert(incompatible.Status
                    == PlayerProgressionCommandStatus.IncompatibleSlot
                && store.SaveCount == 0,
                "equip rejects an incompatible content-defined slot");

            PlayerProgressionCommandResult replaced = null;
            Drain(service.TryEquip(SecondEquipmentId,
                OutgameContentIds.GrowthSlots.Offense,
                value => replaced = value));
            Assert(replaced.Succeeded && service.Current.Revision == 1
                && service.Current.GrowthLoadout.Count == 1
                && service.Current.TryGetEquipped(
                    OutgameContentIds.GrowthSlots.Offense, out var equipped)
                && equipped == SecondEquipmentId,
                "equip transaction replaces exactly one compatible slot");
            PlayerProgressionCommandResult alreadyEquipped = null;
            Drain(service.TryEquip(SecondEquipmentId,
                OutgameContentIds.GrowthSlots.Offense,
                value => alreadyEquipped = value));
            Assert(alreadyEquipped.Status
                    == PlayerProgressionCommandStatus.AlreadyEquipped
                && service.Current.Revision == 1 && store.SaveCount == 1,
                "equipping the current assignment submits no duplicate save");

            PlayerProgressionCommandResult upgraded = null;
            Drain(service.TryUpgradeGrowthEquipment(
                OutgameContentIds.GrowthEquipment.SunleafEmblem,
                value => upgraded = value));
            Assert(upgraded.Succeeded && service.Current.Revision == 2
                && service.Current.TryGetGrowthEquipmentRank(
                    OutgameContentIds.GrowthEquipment.SunleafEmblem,
                    out var rank) && rank == 2
                && service.Current.ItemQuantity(
                    OutgameContentIds.Items.MorningDew) == 10
                && service.Current.ItemQuantity(DustItemId) == 6,
                "equipment upgrade debits all costs and increments one rank");

            PlayerProgressionCommandResult maximum = null;
            Drain(service.TryUpgradeGrowthEquipment(
                OutgameContentIds.GrowthEquipment.SunleafEmblem,
                value => maximum = value));
            Assert(maximum.Status == PlayerProgressionCommandStatus.MaximumRank
                && service.Current.Revision == 2 && store.SaveCount == 2,
                "maximum equipment rank submits no persistence command");

            var lockedProfile = PlayerProfile.CreateDefault();
            lockedProfile.itemBalances = new[]
            {
                new PlayerItemBalance
                {
                    itemId = OutgameContentIds.Items.MorningDew,
                    quantity = 20,
                },
            };
            var lockedStore = new ControlledProfileStore(content);
            var lockedService = new PlayerProgressionService(lockedStore,
                content, lockedProfile);
            PlayerProgressionCommandResult locked = null;
            Drain(lockedService.TryUpgradeCultivation(BranchNodeId,
                value => locked = value));
            Assert(locked.Status
                    == PlayerProgressionCommandStatus.PrerequisiteLocked
                && locked.RelatedIdentity
                    == OutgameContentIds.CultivationNodes.VitalRoots
                && lockedStore.SaveCount == 0,
                "cultivation prerequisite blocks without saving");

            PlayerProgressionCommandResult rootUpgrade = null;
            Drain(lockedService.TryUpgradeCultivation(
                OutgameContentIds.CultivationNodes.VitalRoots,
                value => rootUpgrade = value));
            PlayerProgressionCommandResult branchUpgrade = null;
            Drain(lockedService.TryUpgradeCultivation(BranchNodeId,
                value => branchUpgrade = value));
            Assert(rootUpgrade.Succeeded && branchUpgrade.Succeeded
                && lockedService.Current.Revision == 2
                && lockedService.Current.CultivationRank(
                    OutgameContentIds.CultivationNodes.VitalRoots) == 1
                && lockedService.Current.CultivationRank(BranchNodeId) == 1,
                "satisfied cultivation prerequisite and costs commit one rank each");
            Assert(publications == 2 && store.SaveCount == 2,
                "each successful growth command publishes exactly one revision");
        }

        private static PlayerProfile CreatePopulatedProfile()
        {
            var profile = PlayerProfile.CreateDefault();
            profile.itemBalances = new[]
            {
                new PlayerItemBalance { itemId = DustItemId, quantity = 10 },
                new PlayerItemBalance
                {
                    itemId = OutgameContentIds.Items.MorningDew,
                    quantity = 30,
                },
            };
            profile.activityReceipts = new[]
            {
                new PlayerActivityReceipt
                {
                    receiptId = OutgameContentIds.Receipts.StarterSupplies,
                },
            };
            profile.ownedGrowthEquipment = new[]
            {
                new PlayerGrowthEquipment
                {
                    growthEquipmentId =
                        OutgameContentIds.GrowthEquipment.SunleafEmblem,
                    rank = 1,
                },
                new PlayerGrowthEquipment
                {
                    growthEquipmentId = SecondEquipmentId,
                    rank = 0,
                },
            };
            profile.growthLoadout = new[]
            {
                new PlayerGrowthLoadoutEntry
                {
                    slotId = OutgameContentIds.GrowthSlots.Offense,
                    growthEquipmentId =
                        OutgameContentIds.GrowthEquipment.SunleafEmblem,
                },
            };
            profile.cultivationRanks = new[]
            {
                new PlayerCultivationRank
                {
                    cultivationNodeId =
                        OutgameContentIds.CultivationNodes.VitalRoots,
                    rank = 1,
                },
            };
            return profile;
        }

        private static CompiledOutgameContentCatalog CreateContent()
        {
            var item = new ItemDefinitionDto
            {
                id = OutgameContentIds.Items.MorningDew,
                presentationId = OutgameContentIds.Presentations.MorningDew,
                displayName = "晨露",
                description = "成长材料",
                maximumQuantity = 1000,
            };
            var dust = new ItemDefinitionDto
            {
                id = DustItemId,
                presentationId = "presentation.outgame.item.sun-dust",
                displayName = "日光粉",
                description = "测试成长材料",
                maximumQuantity = 1000,
            };
            var equipment = new GrowthEquipmentDefinitionDto
            {
                id = OutgameContentIds.GrowthEquipment.SunleafEmblem,
                presentationId =
                    OutgameContentIds.Presentations.SunleafEmblem,
                displayName = "日叶徽章",
                description = "测试成长装备",
                slotId = OutgameContentIds.GrowthSlots.Offense,
                ranks = new[]
                {
                    EquipmentRank(0),
                    EquipmentRank(1, Cost(item.id, 10), Cost(dust.id, 2)),
                    EquipmentRank(2, Cost(item.id, 20), Cost(dust.id, 4)),
                },
            };
            var secondEquipment = new GrowthEquipmentDefinitionDto
            {
                id = SecondEquipmentId,
                presentationId = "presentation.outgame.equipment.rainbud-charm",
                displayName = "雨芽符",
                description = "槽位替换测试装备",
                slotId = OutgameContentIds.GrowthSlots.Offense,
                ranks = new[] { EquipmentRank(0) },
            };
            var root = new CultivationNodeDefinitionDto
            {
                id = OutgameContentIds.CultivationNodes.VitalRoots,
                presentationId = OutgameContentIds.Presentations.VitalRoots,
                displayName = "活力根系",
                description = "基础养成节点",
                prerequisites = Array.Empty<CultivationPrerequisiteDto>(),
                ranks = new[]
                {
                    CultivationRank(1, Cost(item.id, 5)),
                    CultivationRank(2, Cost(item.id, 10)),
                },
            };
            var branch = new CultivationNodeDefinitionDto
            {
                id = BranchNodeId,
                presentationId = "presentation.outgame.cultivation.sunlit-branch",
                displayName = "向阳枝",
                description = "带前置条件的养成节点",
                prerequisites = new[]
                {
                    new CultivationPrerequisiteDto
                    {
                        nodeId = root.id,
                        requiredRank = 1,
                    },
                },
                ranks = new[] { CultivationRank(1, Cost(item.id, 5)) },
            };
            var activity = new ActivityDefinitionDto
            {
                id = OutgameContentIds.Activities.StarterSupplies,
                presentationId = OutgameContentIds.Presentations.StarterSupplies,
                displayName = "新手补给",
                description = "一次性测试奖励",
                bundledAvailable = true,
                receiptId = OutgameContentIds.Receipts.StarterSupplies,
                rewards = new[]
                {
                    new RewardGrantDto
                    {
                        operationId =
                            OutgameContentIds.RewardOperations.GrowthEquipment,
                        growthEquipmentId = equipment.id,
                        quantity = 1,
                        initialRank = 0,
                    },
                    new RewardGrantDto
                    {
                        operationId = OutgameContentIds.RewardOperations.Item,
                        itemId = item.id,
                        quantity = 30,
                    },
                    new RewardGrantDto
                    {
                        operationId = OutgameContentIds.RewardOperations.Item,
                        itemId = dust.id,
                        quantity = 3,
                    },
                },
            };
            var catalog = new OutgameContentCatalogDto
            {
                items = new[] { item, dust },
                activities = new[] { activity },
                growthEquipment = new[] { equipment, secondEquipment },
                cultivationNodes = new[] { root, branch },
                growthPolicies = new[]
                {
                    Policy(OutgameContentIds.GrowthPolicies.Orchard01),
                    Policy(OutgameContentIds.GrowthPolicies.Orchard02),
                    Policy(OutgameContentIds.GrowthPolicies.Orchard03),
                },
            };
            Assert(OutgameContentCompiler.TryCompile(catalog, out var compiled,
                    out var validation),
                "progression fixture content compiles: "
                + (validation.Issues.Count == 0
                    ? string.Empty : validation.Issues[0].ToString()));
            return compiled;
        }

        private static GrowthEquipmentRankDefinitionDto EquipmentRank(int rank,
            params GrowthCostDto[] costs)
        {
            return new GrowthEquipmentRankDefinitionDto
            {
                rank = rank,
                costs = costs ?? Array.Empty<GrowthCostDto>(),
                contributions = Array.Empty<GrowthContributionDto>(),
            };
        }

        private static CultivationRankDefinitionDto CultivationRank(int rank,
            params GrowthCostDto[] costs)
        {
            return new CultivationRankDefinitionDto
            {
                rank = rank,
                costs = costs ?? Array.Empty<GrowthCostDto>(),
                contributions = Array.Empty<GrowthContributionDto>(),
            };
        }

        private static GrowthCostDto Cost(string itemId, int quantity)
        {
            return new GrowthCostDto { itemId = itemId, quantity = quantity };
        }

        private static GrowthPolicyDefinitionDto Policy(string id)
        {
            return new GrowthPolicyDefinitionDto
            {
                id = id,
                displayName = id,
                permittedDomainIds = new[]
                {
                    OutgameContentIds.GrowthDomains.Equipment,
                    OutgameContentIds.GrowthDomains.Cultivation,
                },
                permittedAttributeIds = new[] { "attribute.damage" },
                permittedSourceIds = Array.Empty<string>(),
                caps = Array.Empty<GrowthPolicyCapDto>(),
            };
        }

        private static void AssertInvalid(CompiledOutgameContentCatalog content,
            PlayerProfile source, Action<PlayerProfile> mutate,
            ProfileValidationCode expected, string message)
        {
            var candidate = PlayerProfileCodec.Clone(source, content);
            mutate(candidate);
            var result = PlayerProfileCodec.Validate(candidate, content);
            Assert(result.Code == expected, message + " (actual " + result.Code + ")");
        }

        private static void Drain(IEnumerator routine)
        {
            while (routine.MoveNext()) { }
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(
                    "Player progression fixture failed: " + message);
        }

        private sealed class ControlledProfileStore : IPlayerProfileStore
        {
            private readonly CompiledOutgameContentCatalog _content;
            private PlayerProfile _persisted;

            public ControlledProfileStore(CompiledOutgameContentCatalog content)
            {
                _content = content;
                _persisted = PlayerProfile.CreateDefault();
            }

            public bool FailWrites { get; set; }
            public int SaveDelayFrames { get; set; } = 1;
            public int SaveCount { get; private set; }

            public IEnumerator Load(Action<ProfileLoadResult> completed)
            {
                yield return null;
                completed?.Invoke(new ProfileLoadResult(ProfileLoadStatus.Success,
                    PlayerProfileCodec.Clone(_persisted, _content)));
            }

            public IEnumerator Save(PlayerProfile profile,
                Action<ProfileSaveResult> completed)
            {
                SaveCount++;
                for (var frame = 0; frame < SaveDelayFrames; frame++)
                    yield return null;
                if (FailWrites)
                {
                    completed?.Invoke(new ProfileSaveResult(
                        ProfileSaveStatus.StorageError, null,
                        "fixture-write-failed"));
                    yield break;
                }
                var persisted = PlayerProfileCodec.Clone(profile, _content);
                persisted.revision++;
                persisted.updatedAtUtc = DateTimeOffset.UtcNow.ToString("o");
                _persisted = PlayerProfileCodec.Clone(persisted, _content);
                completed?.Invoke(new ProfileSaveResult(ProfileSaveStatus.Success,
                    PlayerProfileCodec.Clone(_persisted, _content)));
            }

            public IEnumerator Reset(Action<ProfileLoadResult> completed)
            {
                yield return null;
                _persisted = PlayerProfile.CreateDefault();
                completed?.Invoke(new ProfileLoadResult(
                    ProfileLoadStatus.ResetCreated,
                    PlayerProfileCodec.Clone(_persisted, _content)));
            }
        }
    }
}
