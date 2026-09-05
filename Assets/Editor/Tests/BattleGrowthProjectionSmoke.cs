using System;
using System.Linq;
using FruitDefense.App.Services;
using FruitDefense.Battle;
using FruitDefense.Content;
using FruitDefense.Core;
using UnityEngine;

namespace FruitDefense.Editor
{
    public static class BattleGrowthProjectionSmoke
    {
        private const string Marker = "BATTLE_GROWTH_PROJECTION_OK";

        public static void Run()
        {
            var levels = BundledLevelCatalogFactory.CreateCompiled();
            if (!BundledGameContentLoader.TryLoadBundle(out var bundle,
                    out var contentValidation))
                throw new InvalidOperationException("Bundled content is invalid: "
                    + (contentValidation.Issues.Count == 0
                        ? "unknown"
                        : contentValidation.Issues[0].ToString()));

            ValidatePolicyFilteringAndFingerprint(levels, bundle.Outgame);
            ValidateAggregationAndCap(levels);
            ValidateLaunchRetryAndMutationIsolation(levels, bundle.Outgame);
            ValidateBaselineBeforeTransientStatus(levels);
            ValidateSnapshotGrowthRestoreIdentity(levels, bundle.Outgame);
            Debug.Log(Marker);
        }

        private static void ValidatePolicyFilteringAndFingerprint(
            CompiledLevelCatalog levels, CompiledOutgameContentCatalog outgame)
        {
            var profile = EquippedProfile(BundledLevelCatalogIds.Levels.Orchard01);
            var projection = PlayerProgressionProjection.Create(profile, outgame);
            var orchard01 = Resolve(levels, outgame, projection,
                BundledLevelCatalogIds.Levels.Orchard01);
            var repeat = Resolve(levels, outgame, projection,
                BundledLevelCatalogIds.Levels.Orchard01);
            Assert(orchard01.Fingerprint == repeat.Fingerprint
                    && orchard01.SourceRecords.Count == 2
                    && orchard01.SourceRecords[0].SourceId
                        == OutgameContentIds.CultivationNodes.VitalRoots
                    && orchard01.SourceRecords[1].SourceId
                        == OutgameContentIds.GrowthEquipment.SunleafEmblem
                    && orchard01.SourceRecords.All(value => value.Disposition
                        == BattleGrowthSourceDisposition.Applied),
                "same inputs produce stable ordinal source records and fingerprint");

            var orchard02 = Resolve(levels, outgame, projection,
                BundledLevelCatalogIds.Levels.Orchard02);
            Assert(orchard02.SourceRecords.Single(value => value.SourceId
                        == OutgameContentIds.CultivationNodes.VitalRoots).Reason
                    == BattleGrowthSourceReason.DomainNotPermitted
                && orchard02.SourceRecords.Single(value => value.SourceId
                        == OutgameContentIds.GrowthEquipment.SunleafEmblem).Disposition
                    == BattleGrowthSourceDisposition.Applied,
                "orchard-02 suppresses cultivation and applies its permitted equipment");

            var orchard03 = Resolve(levels, outgame, projection,
                BundledLevelCatalogIds.Levels.Orchard03);
            Assert(orchard03.SourceRecords.Single(value => value.SourceId
                        == OutgameContentIds.GrowthEquipment.SunleafEmblem).Reason
                    == BattleGrowthSourceReason.DomainNotPermitted
                && orchard03.SourceRecords.Single(value => value.SourceId
                        == OutgameContentIds.CultivationNodes.VitalRoots).Disposition
                    == BattleGrowthSourceDisposition.Applied
                && orchard01.Fingerprint != orchard02.Fingerprint
                && orchard02.Fingerprint != orchard03.Fingerprint,
                "each gameplay policy produces its own applied/suppressed projection");
        }

        private static void ValidateAggregationAndCap(CompiledLevelCatalog levels)
        {
            var authored = LoadOutgameAuthoring();
            var equipment = authored.growthEquipment.Single(value => value.id
                == OutgameContentIds.GrowthEquipment.SunleafEmblem);
            equipment.ranks.Single(value => value.rank == 1).contributions = new[]
            {
                Contribution("modifier.flat", 2f),
                Contribution("modifier.additive", .5f),
                Contribution("modifier.multiplicative", 2f),
            };
            var policy = authored.growthPolicies.Single(value => value.id
                == OutgameContentIds.GrowthPolicies.Orchard01);
            policy.caps.Single(value => value.attributeId == "attribute.damage")
                .maximumValue = 100f;
            var outgame = Compile(authored);
            var profile = EquippedProfile(BundledLevelCatalogIds.Levels.Orchard01,
                cultivation: false);
            var snapshot = Resolve(levels, outgame,
                PlayerProgressionProjection.Create(profile, outgame),
                BundledLevelCatalogIds.Levels.Orchard01);
            var aggregate = snapshot.AggregateModifiers.Single();
            Assert(aggregate.Flat == 2f && aggregate.Additive == .5f
                    && aggregate.Multiplicative == 2f
                    && Mathf.Abs(aggregate.Apply(10f) - 36f) <= .0001f,
                "growth aggregates flat then additive then multiplicative");

            authored = LoadOutgameAuthoring();
            equipment = authored.growthEquipment.Single(value => value.id
                == OutgameContentIds.GrowthEquipment.SunleafEmblem);
            equipment.ranks.Single(value => value.rank == 1).contributions = new[]
            {
                Contribution("modifier.additive", .2f),
            };
            policy = authored.growthPolicies.Single(value => value.id
                == OutgameContentIds.GrowthPolicies.Orchard01);
            policy.caps.Single(value => value.attributeId == "attribute.damage")
                .maximumValue = .15f;
            outgame = Compile(authored);
            snapshot = Resolve(levels, outgame,
                PlayerProgressionProjection.Create(profile, outgame),
                BundledLevelCatalogIds.Levels.Orchard01);
            var capped = snapshot.SourceRecords.Single();
            Assert(capped.Reason == BattleGrowthSourceReason.AppliedAtCap
                    && Mathf.Abs(capped.AppliedValue - .15f) <= .0001f
                    && Mathf.Abs(snapshot.AggregateModifiers.Single().Additive
                        - .15f) <= .0001f,
                "policy cap deterministically truncates the applied source value");
        }

        private static void ValidateLaunchRetryAndMutationIsolation(
            CompiledLevelCatalog levels, CompiledOutgameContentCatalog outgame)
        {
            var profile = EquippedProfile(BundledLevelCatalogIds.Levels.Orchard01,
                cultivation: false);
            var snapshot = Resolve(levels, outgame,
                PlayerProgressionProjection.Create(profile, outgame),
                BundledLevelCatalogIds.Levels.Orchard01);
            var missing = new BattleLaunchRequest("missing-growth", snapshot.LevelId,
                1, snapshot.BattleContentVersion, BattleSessionMode.Standard, null);
            Assert(!missing.TryValidate(out var missingError)
                    && missingError
                        == BattleSessionInitializationResult.GrowthSnapshotRequired,
                "Standard launch rejects the removed no-growth path");

            var request = new BattleLaunchRequest("growth-launch", snapshot.LevelId,
                100, snapshot.BattleContentVersion, BattleSessionMode.Standard,
                snapshot);
            Assert(request.TryValidate(out var requestError)
                    && string.IsNullOrEmpty(requestError)
                    && !ReferenceEquals(request.GrowthSnapshot, snapshot)
                    && request.GrowthSnapshot.Fingerprint == snapshot.Fingerprint,
                "launch owns a validated deep-copied growth snapshot");
            var retry = BattleLaunchRequest.CreateRetry(request, "growth-retry", 101);
            Assert(retry.SessionId != request.SessionId && retry.Seed != request.Seed
                    && retry.LevelId == request.LevelId
                    && retry.ContentVersion == request.ContentVersion
                    && retry.GrowthSnapshot.Fingerprint
                        == request.GrowthSnapshot.Fingerprint
                    && !ReferenceEquals(retry.GrowthSnapshot,
                        request.GrowthSnapshot),
                "retry changes session/seed and deep-copies the completed growth identity");

            var originalFingerprint = snapshot.Fingerprint;
            profile.profileId = "22222222-2222-2222-2222-222222222222";
            profile.revision++;
            profile.growthLoadout = Array.Empty<PlayerGrowthLoadoutEntry>();
            Assert(snapshot.Fingerprint == originalFingerprint
                    && snapshot.SourceRecords.Count == 1,
                "post-resolution profile mutation cannot mutate the launch snapshot");

            var otherLevel = levels.Resolve(BundledLevelCatalogIds.Levels.Orchard02).Value;
            var mismatch = BattleGrowthSnapshotValidator.ValidateForLaunch(snapshot,
                otherLevel, outgame);
            Assert(!mismatch.Succeeded
                    && mismatch.Code == BattleGrowthValidationCode.LevelMismatch,
                "launch validation rejects a snapshot resolved for another gameplay");
        }

        private static void ValidateBaselineBeforeTransientStatus(
            CompiledLevelCatalog levels)
        {
            var authored = LoadOutgameAuthoring();
            var equipment = authored.growthEquipment.Single(value => value.id
                == OutgameContentIds.GrowthEquipment.SunleafEmblem);
            equipment.ranks.Single(value => value.rank == 1).contributions = new[]
            {
                new GrowthContributionDto
                {
                    domainId = OutgameContentIds.GrowthDomains.Equipment,
                    attributeId = "attribute.move-speed",
                    operationId = "modifier.additive",
                    value = .25f,
                },
            };
            var policy = authored.growthPolicies.Single(value => value.id
                == OutgameContentIds.GrowthPolicies.Orchard01);
            policy.permittedAttributeIds = policy.permittedAttributeIds
                .Concat(new[] { "attribute.move-speed" }).ToArray();
            policy.caps = policy.caps.Concat(new[]
            {
                new GrowthPolicyCapDto
                {
                    attributeId = "attribute.move-speed",
                    minimumValue = 0f,
                    maximumValue = 1f,
                },
            }).ToArray();
            var outgame = Compile(authored);
            var profile = EquippedProfile(BundledLevelCatalogIds.Levels.Orchard01,
                cultivation: false);
            var snapshot = Resolve(levels, outgame,
                PlayerProgressionProjection.Create(profile, outgame),
                BundledLevelCatalogIds.Levels.Orchard01);
            var simulation = new GameSimulation(levels,
                BundledLevelCatalogIds.Levels.Orchard01, 6601, snapshot);
            var plant = new Plant
            {
                Id = 991,
                DefinitionId = BattleContentIds.Plants.Pea,
                Star = 1,
            };
            plant.Statuses.Add(new StatusInstance
            {
                DefinitionId = BattleContentIds.Statuses.IceSlow,
                RemainingTicks = 2,
                StackCount = 1,
                Magnitude = 1f,
                Sequence = 1,
            });
            var effective = simulation.GetEffectiveAttribute(plant,
                CombatAttributeKind.MoveSpeed, 10f);
            Assert(Mathf.Abs(effective - 6.875f) <= .0001f,
                "launch additive baseline applies before the transient 0.55 status");
        }

        private static void ValidateSnapshotGrowthRestoreIdentity(
            CompiledLevelCatalog levels, CompiledOutgameContentCatalog outgame)
        {
            var levelId = BundledLevelCatalogIds.Levels.Orchard01;
            var profile = EquippedProfile(levelId, cultivation: false);
            var snapshot = Resolve(levels, outgame,
                PlayerProgressionProjection.Create(profile, outgame), levelId);
            var source = BattleSnapshotSmoke.CreateScenario(levels, levelId, 7701,
                growthSnapshot: snapshot);
            source.Step();
            var exported = source.ExportSnapshot();
            Assert(exported.Succeeded
                    && exported.Snapshot.growthPolicyId == snapshot.PolicyId
                    && exported.Snapshot.growthContentCatalogId
                        == snapshot.OutgameCatalogId
                    && exported.Snapshot.growthContentFingerprint
                        == snapshot.OutgameContentFingerprint
                    && exported.Snapshot.growthProfileId == snapshot.ProfileId
                    && exported.Snapshot.growthProfileRevision
                        == snapshot.ProfileRevision
                    && exported.Snapshot.growthFingerprint == snapshot.Fingerprint,
                "battle snapshot exports complete launch-growth source identity");

            var target = BattleSnapshotSmoke.CreateScenario(levels, levelId, 7702,
                growthSnapshot: snapshot);
            var before = target.OutcomeStateChecksum();
            var corrupt = BattleSnapshotSmoke.Clone(exported.Snapshot);
            corrupt.growthFingerprint += "0";
            var rejected = target.RestoreSnapshot(corrupt, levels);
            Assert(!rejected.Succeeded
                    && rejected.Code == BattleSnapshotRestoreCode.IncompatibleSource
                    && rejected.Path == "growthFingerprint"
                    && target.OutcomeStateChecksum() == before,
                "restore rejects changed growth fingerprint atomically");

            var substituted = EquippedProfile(levelId, cultivation: false);
            substituted.profileId = "33333333-3333-3333-3333-333333333333";
            substituted.revision = profile.revision + 1;
            var substitutedSnapshot = Resolve(levels, outgame,
                PlayerProgressionProjection.Create(substituted, outgame), levelId);
            var substitutedSource = BattleSnapshotSmoke.CreateScenario(levels,
                levelId, 7703, growthSnapshot: substitutedSnapshot);
            var substitutedExport = substitutedSource.ExportSnapshot();
            rejected = target.RestoreSnapshot(substitutedExport.Snapshot, levels);
            Assert(!rejected.Succeeded
                    && rejected.Code == BattleSnapshotRestoreCode.IncompatibleSource
                    && (rejected.Path == "growthProfileId"
                        || rejected.Path == "growthProfileRevision"
                        || rejected.Path == "growthFingerprint")
                    && target.OutcomeStateChecksum() == before,
                "restore rejects post-launch profile substitution atomically");
        }

        private static PlayerProfile EquippedProfile(string levelId,
            bool cultivation = true)
        {
            var profile = PlayerProfile.CreateDefault();
            profile.profileId = "11111111-1111-1111-1111-111111111111";
            profile.revision = 12;
            profile.lastSelectedLevelId = levelId;
            profile.ownedGrowthEquipment = new[]
            {
                new PlayerGrowthEquipment
                {
                    growthEquipmentId =
                        OutgameContentIds.GrowthEquipment.SunleafEmblem,
                    rank = 1,
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
            profile.cultivationRanks = cultivation
                ? new[]
                {
                    new PlayerCultivationRank
                    {
                        cultivationNodeId =
                            OutgameContentIds.CultivationNodes.VitalRoots,
                        rank = 1,
                    },
                }
                : Array.Empty<PlayerCultivationRank>();
            return profile;
        }

        private static BattleGrowthSnapshot Resolve(CompiledLevelCatalog levels,
            CompiledOutgameContentCatalog outgame,
            PlayerProgressionProjection profile, string levelId)
        {
            var level = levels.Resolve(levelId);
            Assert(level.Succeeded && level.Value != null,
                "growth fixture resolves " + levelId);
            var result = BattleGrowthResolver.Resolve(outgame, level.Value, profile);
            Assert(result.Succeeded,
                "growth projection succeeds: " + result.Code + " " + result.Path);
            return result.Snapshot;
        }

        private static OutgameContentCatalogDto LoadOutgameAuthoring()
        {
            var text = Resources.Load<TextAsset>(
                "Content/outgame-content-bundled.v1");
            if (text == null) throw new InvalidOperationException(
                "Bundled outgame JSON is unavailable to the growth fixture.");
            return OutgameContentJson.Deserialize(text.text);
        }

        private static CompiledOutgameContentCatalog Compile(
            OutgameContentCatalogDto authored)
        {
            Assert(OutgameContentCompiler.TryCompile(authored, out var compiled,
                    out var validation),
                "custom growth content compiles: "
                + (validation.Issues.Count == 0
                    ? string.Empty
                    : validation.Issues[0].ToString()));
            return compiled;
        }

        private static GrowthContributionDto Contribution(string operationId,
            float value)
        {
            return new GrowthContributionDto
            {
                domainId = OutgameContentIds.GrowthDomains.Equipment,
                attributeId = "attribute.damage",
                operationId = operationId,
                value = value,
            };
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(
                "Battle growth projection fixture failed: " + message);
        }
    }
}
