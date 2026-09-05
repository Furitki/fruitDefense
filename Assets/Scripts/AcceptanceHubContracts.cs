#if FRUIT_DEFENSE_ACCEPTANCE || UNITY_EDITOR
using System;
using System.Collections.Generic;

namespace FruitDefense
{
    public enum AcceptanceHubEvidenceKind
    {
        StaticState = 0,
        PersistenceFailure = 1,
        RealInteractionSequence = 2,
    }

    public sealed class AcceptanceHubStateDefinition
    {
        public AcceptanceHubStateDefinition(string id, string page,
            string state, AcceptanceHubEvidenceKind evidenceKind)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Page = page ?? throw new ArgumentNullException(nameof(page));
            State = state ?? throw new ArgumentNullException(nameof(state));
            EvidenceKind = evidenceKind;
        }

        public string Id { get; }
        public string Page { get; }
        public string State { get; }
        public AcceptanceHubEvidenceKind EvidenceKind { get; }
    }

    public static class AcceptanceHubStateCatalog
    {
        private static readonly AcceptanceHubStateDefinition[] Definitions =
        {
            State("home-fresh", "home", "fresh"),
            State("home-policy-preview", "home", "applied-suppressed"),
            State("activity-claimable", "activity", "claimable"),
            State("activity-claiming", "activity", "claiming"),
            State("activity-claimed", "activity", "claimed"),
            State("activity-error", "activity", "error"),
            Failure("activity-save-failure", "activity", "save-failure"),
            State("equipment-owned", "equipment", "owned"),
            State("equipment-selected", "equipment", "selected"),
            State("equipment-locked", "equipment", "locked"),
            State("equipment-insufficient", "equipment", "insufficient"),
            State("equipment-maximum", "equipment", "maximum"),
            State("equipment-loading", "equipment", "loading"),
            State("equipment-error", "equipment", "error"),
            Failure("equipment-save-failure", "equipment", "save-failure"),
            State("cultivation-selected", "cultivation", "selected"),
            State("cultivation-locked", "cultivation", "locked"),
            State("cultivation-insufficient", "cultivation", "insufficient"),
            State("cultivation-maximum", "cultivation", "maximum"),
            State("cultivation-loading", "cultivation", "loading"),
            State("cultivation-error", "cultivation", "error"),
            Failure("cultivation-save-failure", "cultivation", "save-failure"),
            Sequence("reward-to-battle", "flow", "fresh-profile-sequence"),
        };

        private static readonly Dictionary<string, AcceptanceHubStateDefinition>
            ById = BuildIndex();

        public static IReadOnlyList<AcceptanceHubStateDefinition> All => Definitions;

        public static bool TryGet(string id,
            out AcceptanceHubStateDefinition definition)
        {
            return ById.TryGetValue(id ?? string.Empty, out definition);
        }

        private static AcceptanceHubStateDefinition State(string id,
            string page, string state)
        {
            return new AcceptanceHubStateDefinition(id, page, state,
                AcceptanceHubEvidenceKind.StaticState);
        }

        private static AcceptanceHubStateDefinition Failure(string id,
            string page, string state)
        {
            return new AcceptanceHubStateDefinition(id, page, state,
                AcceptanceHubEvidenceKind.PersistenceFailure);
        }

        private static AcceptanceHubStateDefinition Sequence(string id,
            string page, string state)
        {
            return new AcceptanceHubStateDefinition(id, page, state,
                AcceptanceHubEvidenceKind.RealInteractionSequence);
        }

        private static Dictionary<string, AcceptanceHubStateDefinition> BuildIndex()
        {
            var result = new Dictionary<string, AcceptanceHubStateDefinition>(
                StringComparer.Ordinal);
            for (var index = 0; index < Definitions.Length; index++)
                result.Add(Definitions[index].Id, Definitions[index]);
            return result;
        }
    }

    [Serializable]
    public sealed class AcceptanceHubIdentityTelemetry
    {
        public int schemaVersion = 1;
        public string stateId = string.Empty;
        public bool fixtureActive;
        public string fixtureId = string.Empty;
        public string evidenceKind = string.Empty;
        public int route;
        public string routeName = string.Empty;
        public string sessionId = string.Empty;
        public int seed;
        public string page = string.Empty;
        public string growthPage = string.Empty;
        public string resolvedState = string.Empty;
        public string selectedLevelId = string.Empty;
        public string manifestId = string.Empty;
        public string manifestVersion = string.Empty;
        public string manifestFingerprint = string.Empty;
        public string outgameContentId = string.Empty;
        public string outgameContentVersion = string.Empty;
        public string outgameContentFingerprint = string.Empty;
        public string battleContentId = string.Empty;
        public string battleContentVersion = string.Empty;
        public string battleContentFingerprint = string.Empty;
        public string profileId = string.Empty;
        public long profileRevision;
        public string growthPolicyId = string.Empty;
        public string growthFingerprint = string.Empty;
        public long launchGrowthProfileRevision;
        public string launchGrowthPolicyId = string.Empty;
        public string launchGrowthFingerprint = string.Empty;
        public int appliedSourceCount;
        public int suppressedSourceCount;
        public int receiptCount;
        public int committedRewardRevisionCount;
        public int committedGrowthRevisionCount;
        public bool commandInProgress;
        public string lastCommand = string.Empty;
        public string lastCommandStatus = string.Empty;
        public string lastCommandError = string.Empty;
        public AcceptanceHubItemBalanceTelemetry[] itemBalances =
            Array.Empty<AcceptanceHubItemBalanceTelemetry>();
        public AcceptanceHubGrowthEquipmentTelemetry[] growthEquipment =
            Array.Empty<AcceptanceHubGrowthEquipmentTelemetry>();
        public AcceptanceHubLoadoutTelemetry[] loadout =
            Array.Empty<AcceptanceHubLoadoutTelemetry>();
        public AcceptanceHubCultivationTelemetry[] cultivation =
            Array.Empty<AcceptanceHubCultivationTelemetry>();
    }

    [Serializable]
    public sealed class AcceptanceHubItemBalanceTelemetry
    {
        public string itemId = string.Empty;
        public long quantity;
    }

    [Serializable]
    public sealed class AcceptanceHubGrowthEquipmentTelemetry
    {
        public string growthEquipmentId = string.Empty;
        public int rank;
    }

    [Serializable]
    public sealed class AcceptanceHubLoadoutTelemetry
    {
        public string slotId = string.Empty;
        public string growthEquipmentId = string.Empty;
    }

    [Serializable]
    public sealed class AcceptanceHubCultivationTelemetry
    {
        public string cultivationNodeId = string.Empty;
        public int rank;
    }

#if FRUIT_DEFENSE_ACCEPTANCE
    public interface IAcceptanceHubPort
    {
        string HubAcceptanceTelemetryJson { get; }
        AcceptanceCommandResult TryConfigureNamedHubState(string stateId);
    }
#endif
}
#endif
