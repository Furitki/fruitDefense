using System;
using System.Collections.Generic;
using System.IO;

namespace FruitDefense.Editor
{
    public static class OutgameHubAcceptanceCatalogSmoke
    {
        private static readonly string[] RequiredStateIds =
        {
            "home-fresh",
            "home-policy-preview",
            "activity-claimable",
            "activity-claiming",
            "activity-claimed",
            "activity-error",
            "activity-save-failure",
            "equipment-owned",
            "equipment-selected",
            "equipment-locked",
            "equipment-insufficient",
            "equipment-maximum",
            "equipment-loading",
            "equipment-error",
            "equipment-save-failure",
            "cultivation-selected",
            "cultivation-locked",
            "cultivation-insufficient",
            "cultivation-maximum",
            "cultivation-loading",
            "cultivation-error",
            "cultivation-save-failure",
            "reward-to-battle",
        };

        public static void Run()
        {
            ValidateFiniteCatalog();
            ValidateTelemetryContract();
            ValidateCompileBoundary();
            ValidateBridgeFixtureContract();
        }

        private static void ValidateFiniteCatalog()
        {
            var all = AcceptanceHubStateCatalog.All;
            Assert(all.Count == RequiredStateIds.Length,
                "acceptance hub catalog has exactly the required finite states");

            var seen = new HashSet<string>(StringComparer.Ordinal);
            var pages = new HashSet<string>(StringComparer.Ordinal);
            var failureCount = 0;
            var sequenceCount = 0;
            for (var index = 0; index < all.Count; index++)
            {
                var definition = all[index];
                Assert(definition != null
                    && !string.IsNullOrWhiteSpace(definition.Id)
                    && !string.IsNullOrWhiteSpace(definition.Page)
                    && !string.IsNullOrWhiteSpace(definition.State),
                    "every acceptance hub state has finite identity, page, and state");
                Assert(seen.Add(definition.Id),
                    "acceptance hub state identities are ordinal-unique: "
                    + definition.Id);
                pages.Add(definition.Page);
                if (definition.EvidenceKind
                    == AcceptanceHubEvidenceKind.PersistenceFailure)
                    failureCount++;
                if (definition.EvidenceKind
                    == AcceptanceHubEvidenceKind.RealInteractionSequence)
                    sequenceCount++;
            }

            for (var index = 0; index < RequiredStateIds.Length; index++)
            {
                var expected = RequiredStateIds[index];
                Assert(string.Equals(all[index].Id, expected,
                        StringComparison.Ordinal),
                    "acceptance hub catalog preserves canonical order: " + expected);
                Assert(AcceptanceHubStateCatalog.TryGet(expected, out var resolved)
                    && ReferenceEquals(resolved, all[index]),
                    "acceptance hub catalog resolves canonical state: " + expected);
            }

            Assert(!AcceptanceHubStateCatalog.TryGet(
                    "activity-claimed-unknown", out _),
                "acceptance hub catalog rejects unknown state identities");
            Assert(pages.SetEquals(new[]
                {
                    "home", "activity", "equipment", "cultivation", "flow",
                }),
                "acceptance hub catalog covers every required page and real flow");
            Assert(failureCount == 3,
                "each persistence-changing hub domain has one save-failure fixture");
            Assert(sequenceCount == 1,
                "the catalog has one real fresh-profile reward-to-battle sequence");
        }

        private static void ValidateTelemetryContract()
        {
            var telemetry = new AcceptanceHubIdentityTelemetry();
            Assert(telemetry.schemaVersion == 1
                && telemetry.profileRevision == 0
                && telemetry.itemBalances.Length == 0
                && telemetry.growthEquipment.Length == 0
                && telemetry.loadout.Length == 0
                && telemetry.cultivation.Length == 0,
                "hub telemetry defaults are finite and serialization-safe");

            var fields = typeof(AcceptanceHubIdentityTelemetry).GetFields();
            var fieldNames = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < fields.Length; index++)
                fieldNames.Add(fields[index].Name);
            foreach (var required in new[]
                     {
                         "manifestId", "manifestVersion", "manifestFingerprint",
                         "fixtureActive", "fixtureId", "evidenceKind",
                         "route", "routeName", "sessionId", "seed",
                         "resolvedState",
                         "outgameContentId", "outgameContentVersion",
                         "outgameContentFingerprint", "battleContentId",
                         "battleContentVersion", "battleContentFingerprint",
                         "profileId", "profileRevision", "growthPolicyId",
                         "growthFingerprint", "appliedSourceCount",
                         "suppressedSourceCount", "receiptCount",
                         "launchGrowthProfileRevision", "launchGrowthPolicyId",
                         "launchGrowthFingerprint",
                         "committedRewardRevisionCount",
                         "committedGrowthRevisionCount", "commandInProgress",
                         "lastCommandStatus", "itemBalances", "growthEquipment",
                         "loadout", "cultivation",
                     })
            {
                Assert(fieldNames.Contains(required),
                    "hub telemetry records acceptance identity/outcome field: "
                    + required);
            }
        }

        private static void ValidateCompileBoundary()
        {
            var source = File.ReadAllText(Path.GetFullPath(
                "Assets/Scripts/AcceptanceHubContracts.cs"));
            Assert(source.StartsWith(
                    "#if FRUIT_DEFENSE_ACCEPTANCE || UNITY_EDITOR",
                    StringComparison.Ordinal)
                && source.TrimEnd().EndsWith("#endif", StringComparison.Ordinal),
                "hub acceptance catalog and bridge contract are excluded from ordinary runtime compilation");
            Assert(source.Contains("#if FRUIT_DEFENSE_ACCEPTANCE",
                    StringComparison.Ordinal)
                && source.Contains("interface IAcceptanceHubPort",
                    StringComparison.Ordinal),
                "mutable hub acceptance port exists only in the dedicated build");
        }

        private static void ValidateBridgeFixtureContract()
        {
            var source = File.ReadAllText(Path.GetFullPath(
                "Assets/Scripts/AcceptanceHubBridge.cs"));
            Assert(source.StartsWith("#if FRUIT_DEFENSE_ACCEPTANCE",
                    StringComparison.Ordinal)
                && source.TrimEnd().EndsWith("#endif", StringComparison.Ordinal),
                "hub bridge and fixtures compile only in the dedicated acceptance build");
            foreach (var required in new[]
                     {
                         "AcceptanceLaunchQuery.IsEnabled",
                         "acceptance-hub/",
                         "source.cultivationNodes =",
                         "_presenter.Initialize(_coordinator",
                         "request?.GrowthSnapshot",
                         "BattleGrowthResolver.Resolve",
                         "PlayerProfileCodec.Validate",
                     })
            {
                Assert(source.Contains(required, StringComparison.Ordinal),
                    "hub bridge preserves fixture/real-loop evidence boundary: "
                    + required);
            }
            foreach (var forbidden in new[]
                     {
                         "new LocalProfileStore",
                         "PlayerPrefs.SetString",
                         "File.WriteAllText",
                     })
            {
                Assert(!source.Contains(forbidden, StringComparison.Ordinal),
                    "hub static fixtures do not mutate real persistence: "
                    + forbidden);
            }
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(
                    "Outgame hub acceptance catalog smoke failed: " + message);
        }
    }
}
