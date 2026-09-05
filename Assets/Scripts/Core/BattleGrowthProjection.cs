using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using FruitDefense.App.Services;
using FruitDefense.Content;

namespace FruitDefense.Core
{
    public enum BattleGrowthSourceDisposition
    {
        Applied,
        Suppressed,
    }

    public enum BattleGrowthSourceReason
    {
        Applied,
        AppliedAtCap,
        DomainNotPermitted,
        SourceNotPermitted,
        AttributeNotPermitted,
        CapReached,
    }

    public sealed class BattleGrowthSourceRecord
    {
        internal BattleGrowthSourceRecord(string sourceId, string domainId, int rank,
            string attributeId, string operationId, float authoredValue,
            float appliedValue, BattleGrowthSourceDisposition disposition,
            BattleGrowthSourceReason reason)
        {
            SourceId = sourceId ?? string.Empty;
            DomainId = domainId ?? string.Empty;
            Rank = rank;
            AttributeId = attributeId ?? string.Empty;
            OperationId = operationId ?? string.Empty;
            AuthoredValue = authoredValue;
            AppliedValue = appliedValue;
            Disposition = disposition;
            Reason = reason;
        }

        public string SourceId { get; }
        public string DomainId { get; }
        public int Rank { get; }
        public string AttributeId { get; }
        public string OperationId { get; }
        public float AuthoredValue { get; }
        public float AppliedValue { get; }
        public BattleGrowthSourceDisposition Disposition { get; }
        public BattleGrowthSourceReason Reason { get; }

        internal BattleGrowthSourceRecord DeepCopy()
        {
            return new BattleGrowthSourceRecord(SourceId, DomainId, Rank, AttributeId,
                OperationId, AuthoredValue, AppliedValue, Disposition, Reason);
        }
    }

    public sealed class BattleGrowthAggregateModifier
    {
        internal BattleGrowthAggregateModifier(string attributeId, float flat,
            float additive, float multiplicative)
        {
            AttributeId = attributeId ?? string.Empty;
            Flat = flat;
            Additive = additive;
            Multiplicative = multiplicative;
        }

        public string AttributeId { get; }
        public float Flat { get; }
        public float Additive { get; }
        public float Multiplicative { get; }

        internal BattleGrowthAggregateModifier DeepCopy()
        {
            return new BattleGrowthAggregateModifier(AttributeId, Flat, Additive,
                Multiplicative);
        }

        public float Apply(float authoredBase)
        {
            return (authoredBase + Flat) * (1f + Additive) * Multiplicative;
        }
    }

    public sealed class BattleGrowthSnapshot
    {
        private readonly Dictionary<string, BattleGrowthAggregateModifier> _byAttribute;

        internal BattleGrowthSnapshot(string profileId, long profileRevision,
            string levelId, string battleContentVersion, string policyId,
            string outgameCatalogId, string outgameContentVersion,
            string outgameContentFingerprint,
            IEnumerable<BattleGrowthSourceRecord> sourceRecords,
            IEnumerable<BattleGrowthAggregateModifier> aggregateModifiers,
            string fingerprint)
        {
            ProfileId = profileId ?? string.Empty;
            ProfileRevision = profileRevision;
            LevelId = levelId ?? string.Empty;
            BattleContentVersion = battleContentVersion ?? string.Empty;
            PolicyId = policyId ?? string.Empty;
            OutgameCatalogId = outgameCatalogId ?? string.Empty;
            OutgameContentVersion = outgameContentVersion ?? string.Empty;
            OutgameContentFingerprint = outgameContentFingerprint ?? string.Empty;

            var records = (sourceRecords ?? Enumerable.Empty<BattleGrowthSourceRecord>())
                .Select(value => value == null ? null : value.DeepCopy()).ToArray();
            var aggregates = (aggregateModifiers
                    ?? Enumerable.Empty<BattleGrowthAggregateModifier>())
                .Select(value => value == null ? null : value.DeepCopy()).ToArray();
            SourceRecords = Array.AsReadOnly(records);
            AggregateModifiers = Array.AsReadOnly(aggregates);
            _byAttribute = aggregates.Where(value => value != null)
                .ToDictionary(value => value.AttributeId, value => value,
                    StringComparer.Ordinal);
            Fingerprint = fingerprint ?? string.Empty;
        }

        public string ProfileId { get; }
        public long ProfileRevision { get; }
        public string LevelId { get; }
        public string BattleContentVersion { get; }
        public string PolicyId { get; }
        public string OutgameCatalogId { get; }
        public string OutgameContentVersion { get; }
        public string OutgameContentFingerprint { get; }
        public ReadOnlyCollection<BattleGrowthSourceRecord> SourceRecords { get; }
        public ReadOnlyCollection<BattleGrowthAggregateModifier> AggregateModifiers { get; }
        public string Fingerprint { get; }

        public bool TryGetAggregate(string attributeId,
            out BattleGrowthAggregateModifier modifier)
        {
            return _byAttribute.TryGetValue(attributeId ?? string.Empty, out modifier);
        }

        public BattleGrowthSnapshot DeepCopy()
        {
            return new BattleGrowthSnapshot(ProfileId, ProfileRevision, LevelId,
                BattleContentVersion, PolicyId, OutgameCatalogId,
                OutgameContentVersion, OutgameContentFingerprint, SourceRecords,
                AggregateModifiers, Fingerprint);
        }
    }

    public enum BattleGrowthResolveCode
    {
        Success,
        CatalogRequired,
        LevelRequired,
        ProfileRequired,
        PolicyMissing,
        SourceInvalid,
        ContributionInvalid,
        AggregateInvalid,
    }

    public readonly struct BattleGrowthResolution
    {
        private BattleGrowthResolution(BattleGrowthResolveCode code,
            BattleGrowthSnapshot snapshot, string path, string message)
        {
            Code = code;
            Snapshot = snapshot;
            Path = path ?? string.Empty;
            Message = message ?? string.Empty;
        }

        public BattleGrowthResolveCode Code { get; }
        public BattleGrowthSnapshot Snapshot { get; }
        public string Path { get; }
        public string Message { get; }
        public bool Succeeded => Code == BattleGrowthResolveCode.Success
            && Snapshot != null;

        internal static BattleGrowthResolution Ok(BattleGrowthSnapshot snapshot)
        {
            return new BattleGrowthResolution(BattleGrowthResolveCode.Success,
                snapshot, string.Empty, string.Empty);
        }

        internal static BattleGrowthResolution Fail(BattleGrowthResolveCode code,
            string path, string message)
        {
            return new BattleGrowthResolution(code, null, path, message);
        }
    }

    public enum BattleGrowthValidationCode
    {
        Success,
        SnapshotRequired,
        IdentityInvalid,
        RecordInvalid,
        RecordOrderInvalid,
        AggregateInvalid,
        AggregateOrderInvalid,
        FingerprintMismatch,
        LevelMismatch,
        PolicyMismatch,
        BattleContentMismatch,
        OutgameCatalogMismatch,
        OutgameContentVersionMismatch,
        OutgameContentFingerprintMismatch,
    }

    public readonly struct BattleGrowthValidationResult
    {
        public BattleGrowthValidationResult(BattleGrowthValidationCode code,
            string path, string message)
        {
            Code = code;
            Path = path ?? string.Empty;
            Message = message ?? string.Empty;
        }

        public BattleGrowthValidationCode Code { get; }
        public string Path { get; }
        public string Message { get; }
        public bool Succeeded => Code == BattleGrowthValidationCode.Success;

        public static BattleGrowthValidationResult Ok()
        {
            return new BattleGrowthValidationResult(BattleGrowthValidationCode.Success,
                string.Empty, string.Empty);
        }
    }

    public static class BattleGrowthResolver
    {
        public const string FlatOperationId = "modifier.flat";
        public const string AdditiveOperationId = "modifier.additive";
        public const string MultiplicativeOperationId = "modifier.multiplicative";

        public static BattleGrowthResolution Resolve(
            CompiledOutgameContentCatalog catalog,
            ResolvedLevelDefinition level,
            PlayerProgressionProjection profile)
        {
            if (catalog == null)
                return BattleGrowthResolution.Fail(BattleGrowthResolveCode.CatalogRequired,
                    "catalog", "Compiled outgame content is required.");
            if (level == null)
                return BattleGrowthResolution.Fail(BattleGrowthResolveCode.LevelRequired,
                    "level", "A resolved playable level is required.");
            if (profile == null)
                return BattleGrowthResolution.Fail(BattleGrowthResolveCode.ProfileRequired,
                    "profile", "A validated immutable profile projection is required.");

            if (!catalog.GrowthPolicies.TryGetValue(level.Identity.GrowthPolicyId,
                    out var policy) || policy == null)
                return BattleGrowthResolution.Fail(BattleGrowthResolveCode.PolicyMissing,
                    "level.growthPolicyId", "The level growth policy is unavailable.");

            if (!TryBuildCandidates(catalog, profile, out var candidates,
                    out var candidatesFailure))
                return candidatesFailure;

            var permittedDomains = new HashSet<string>(policy.permittedDomainIds
                ?? Array.Empty<string>(), StringComparer.Ordinal);
            var permittedAttributes = new HashSet<string>(policy.permittedAttributeIds
                ?? Array.Empty<string>(), StringComparer.Ordinal);
            var permittedSources = new HashSet<string>(policy.permittedSourceIds
                ?? Array.Empty<string>(), StringComparer.Ordinal);
            var caps = (policy.caps ?? Array.Empty<GrowthPolicyCapDto>())
                .Where(value => value != null)
                .ToDictionary(value => value.attributeId, value => value,
                    StringComparer.Ordinal);
            var consumedCap = new Dictionary<string, float>(StringComparer.Ordinal);
            var records = new List<BattleGrowthSourceRecord>();

            foreach (var source in candidates.OrderBy(value => value.SourceId,
                         StringComparer.Ordinal))
            {
                foreach (var contribution in source.Contributions
                             .OrderBy(value => value.attributeId, StringComparer.Ordinal)
                             .ThenBy(value => value.operationId, StringComparer.Ordinal))
                {
                    if (contribution == null || !Finite(contribution.value)
                        || contribution.value <= 0f
                        || !TryResolveOperation(contribution.operationId, out _)
                        || !TryResolveAttribute(contribution.attributeId, out _))
                        return BattleGrowthResolution.Fail(
                            BattleGrowthResolveCode.ContributionInvalid,
                            source.SourceId + ".contributions",
                            "A source contribution is not finite or supported.");
                    if (!string.Equals(contribution.domainId, source.DomainId,
                            StringComparison.Ordinal))
                        return BattleGrowthResolution.Fail(
                            BattleGrowthResolveCode.SourceInvalid,
                            source.SourceId + ".domainId",
                            "A source contribution does not match its owning domain.");

                    if (!permittedDomains.Contains(source.DomainId))
                    {
                        records.Add(Suppressed(source, contribution,
                            BattleGrowthSourceReason.DomainNotPermitted));
                        continue;
                    }
                    if (permittedSources.Count > 0
                        && !permittedSources.Contains(source.SourceId))
                    {
                        records.Add(Suppressed(source, contribution,
                            BattleGrowthSourceReason.SourceNotPermitted));
                        continue;
                    }
                    if (!permittedAttributes.Contains(contribution.attributeId))
                    {
                        records.Add(Suppressed(source, contribution,
                            BattleGrowthSourceReason.AttributeNotPermitted));
                        continue;
                    }

                    var appliedValue = contribution.value;
                    var appliedReason = BattleGrowthSourceReason.Applied;
                    if (caps.TryGetValue(contribution.attributeId, out var cap))
                    {
                        consumedCap.TryGetValue(contribution.attributeId,
                            out var alreadyApplied);
                        var available = Math.Max(0f, cap.maximumValue - alreadyApplied);
                        appliedValue = Math.Min(contribution.value, available);
                        if (appliedValue <= 0f)
                        {
                            records.Add(Suppressed(source, contribution,
                                BattleGrowthSourceReason.CapReached));
                            continue;
                        }
                        consumedCap[contribution.attributeId] = alreadyApplied
                            + appliedValue;
                        if (appliedValue < contribution.value)
                            appliedReason = BattleGrowthSourceReason.AppliedAtCap;
                    }

                    records.Add(new BattleGrowthSourceRecord(source.SourceId,
                        source.DomainId, source.Rank, contribution.attributeId,
                        contribution.operationId, contribution.value, appliedValue,
                        BattleGrowthSourceDisposition.Applied, appliedReason));
                }
            }

            if (!TryBuildAggregates(records, out var aggregates,
                    out var aggregatesFailure))
                return aggregatesFailure;
            var fingerprint = BattleGrowthFingerprint.Compute(profile.ProfileId,
                profile.Revision, level.Identity.LevelId,
                level.BattleContent.Header.contentVersion, policy.id,
                catalog.Header.catalogId, catalog.Header.contentVersion,
                catalog.Fingerprint, records, aggregates);
            var snapshot = new BattleGrowthSnapshot(profile.ProfileId,
                profile.Revision, level.Identity.LevelId,
                level.BattleContent.Header.contentVersion, policy.id,
                catalog.Header.catalogId, catalog.Header.contentVersion,
                catalog.Fingerprint, records, aggregates, fingerprint);
            var validation = BattleGrowthSnapshotValidator.ValidateCanonical(snapshot);
            return validation.Succeeded
                ? BattleGrowthResolution.Ok(snapshot)
                : BattleGrowthResolution.Fail(BattleGrowthResolveCode.AggregateInvalid,
                    validation.Path, validation.Message);
        }

        internal static bool TryResolveAttribute(string id,
            out CombatAttributeKind attribute)
        {
            switch (id)
            {
                case "attribute.damage": attribute = CombatAttributeKind.Damage; return true;
                case "attribute.attack-interval": attribute = CombatAttributeKind.AttackInterval; return true;
                case "attribute.range": attribute = CombatAttributeKind.Range; return true;
                case "attribute.move-speed": attribute = CombatAttributeKind.MoveSpeed; return true;
                case "attribute.damage-taken": attribute = CombatAttributeKind.DamageTaken; return true;
                case "attribute.resource-gain": attribute = CombatAttributeKind.ResourceGain; return true;
                default: attribute = default; return false;
            }
        }

        internal static string AttributeId(CombatAttributeKind attribute)
        {
            switch (attribute)
            {
                case CombatAttributeKind.Damage: return "attribute.damage";
                case CombatAttributeKind.AttackInterval: return "attribute.attack-interval";
                case CombatAttributeKind.Range: return "attribute.range";
                case CombatAttributeKind.MoveSpeed: return "attribute.move-speed";
                case CombatAttributeKind.DamageTaken: return "attribute.damage-taken";
                case CombatAttributeKind.ResourceGain: return "attribute.resource-gain";
                default: throw new ArgumentOutOfRangeException(nameof(attribute));
            }
        }

        internal static bool TryResolveOperation(string id,
            out CombatModifierOperation operation)
        {
            switch (id)
            {
                case FlatOperationId: operation = CombatModifierOperation.Flat; return true;
                case AdditiveOperationId: operation = CombatModifierOperation.Additive; return true;
                case MultiplicativeOperationId:
                    operation = CombatModifierOperation.Multiplicative;
                    return true;
                default: operation = default; return false;
            }
        }

        private static bool TryBuildAggregates(
            IReadOnlyList<BattleGrowthSourceRecord> records,
            out BattleGrowthAggregateModifier[] aggregates,
            out BattleGrowthResolution failure)
        {
            var values = new List<BattleGrowthAggregateModifier>();
            foreach (var group in records.Where(value => value != null
                             && value.Disposition == BattleGrowthSourceDisposition.Applied)
                         .GroupBy(value => value.AttributeId, StringComparer.Ordinal)
                         .OrderBy(value => value.Key, StringComparer.Ordinal))
            {
                var flat = 0f;
                var additive = 0f;
                var multiplicative = 1f;
                foreach (var record in group)
                {
                    if (!TryResolveOperation(record.OperationId, out var operation))
                    {
                        aggregates = Array.Empty<BattleGrowthAggregateModifier>();
                        failure = BattleGrowthResolution.Fail(
                            BattleGrowthResolveCode.ContributionInvalid,
                            record.SourceId + ".operationId",
                            "A source contribution operation is unsupported.");
                        return false;
                    }
                    if (operation == CombatModifierOperation.Flat)
                        flat += record.AppliedValue;
                    else if (operation == CombatModifierOperation.Additive)
                        additive += record.AppliedValue;
                    else multiplicative *= record.AppliedValue;
                }
                if (!Finite(flat) || !Finite(additive) || !Finite(multiplicative))
                {
                    aggregates = Array.Empty<BattleGrowthAggregateModifier>();
                    failure = BattleGrowthResolution.Fail(
                        BattleGrowthResolveCode.AggregateInvalid, group.Key,
                        "Growth aggregation produced a non-finite value.");
                    return false;
                }
                values.Add(new BattleGrowthAggregateModifier(group.Key, flat,
                    additive, multiplicative));
            }
            aggregates = values.ToArray();
            failure = default;
            return true;
        }

        private static bool TryBuildCandidates(
            CompiledOutgameContentCatalog catalog,
            PlayerProgressionProjection profile,
            out CandidateSource[] candidates,
            out BattleGrowthResolution failure)
        {
            var values = new List<CandidateSource>();
            foreach (var equipped in profile.GrowthLoadout)
            {
                if (!catalog.GrowthEquipment.TryGetValue(equipped.GrowthEquipmentId,
                        out var definition)
                    || !profile.TryGetGrowthEquipmentRank(equipped.GrowthEquipmentId,
                        out var rank))
                {
                    candidates = Array.Empty<CandidateSource>();
                    failure = BattleGrowthResolution.Fail(BattleGrowthResolveCode.SourceInvalid,
                        "profile.growthLoadout", "An equipped growth source is invalid.");
                    return false;
                }
                var rankDefinition = (definition.ranks
                        ?? Array.Empty<GrowthEquipmentRankDefinitionDto>())
                    .SingleOrDefault(value => value != null && value.rank == rank);
                if (rankDefinition == null)
                {
                    candidates = Array.Empty<CandidateSource>();
                    failure = BattleGrowthResolution.Fail(BattleGrowthResolveCode.SourceInvalid,
                        definition.id + ".rank", "The equipped growth rank is unavailable.");
                    return false;
                }
                values.Add(new CandidateSource(definition.id,
                    OutgameContentIds.GrowthDomains.Equipment, rank,
                    rankDefinition.contributions));
            }

            foreach (var cultivation in profile.CultivationRanks)
            {
                if (cultivation.Rank <= 0) continue;
                if (!catalog.CultivationNodes.TryGetValue(cultivation.CultivationNodeId,
                        out var definition))
                {
                    candidates = Array.Empty<CandidateSource>();
                    failure = BattleGrowthResolution.Fail(BattleGrowthResolveCode.SourceInvalid,
                        "profile.cultivationRanks", "A cultivation source is invalid.");
                    return false;
                }
                var rankDefinition = (definition.ranks
                        ?? Array.Empty<CultivationRankDefinitionDto>())
                    .SingleOrDefault(value => value != null
                        && value.rank == cultivation.Rank);
                if (rankDefinition == null)
                {
                    candidates = Array.Empty<CandidateSource>();
                    failure = BattleGrowthResolution.Fail(BattleGrowthResolveCode.SourceInvalid,
                        definition.id + ".rank", "The cultivation rank is unavailable.");
                    return false;
                }
                values.Add(new CandidateSource(definition.id,
                    OutgameContentIds.GrowthDomains.Cultivation, cultivation.Rank,
                    rankDefinition.contributions));
            }
            candidates = values.ToArray();
            failure = default;
            return true;
        }

        private static BattleGrowthSourceRecord Suppressed(CandidateSource source,
            GrowthContributionDto contribution, BattleGrowthSourceReason reason)
        {
            return new BattleGrowthSourceRecord(source.SourceId, source.DomainId,
                source.Rank, contribution.attributeId, contribution.operationId,
                contribution.value, 0f, BattleGrowthSourceDisposition.Suppressed,
                reason);
        }

        private static bool Finite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private sealed class CandidateSource
        {
            public CandidateSource(string sourceId, string domainId, int rank,
                GrowthContributionDto[] contributions)
            {
                SourceId = sourceId ?? string.Empty;
                DomainId = domainId ?? string.Empty;
                Rank = rank;
                Contributions = contributions ?? Array.Empty<GrowthContributionDto>();
            }

            public string SourceId { get; }
            public string DomainId { get; }
            public int Rank { get; }
            public GrowthContributionDto[] Contributions { get; }
        }
    }

    public static class BattleGrowthSnapshotValidator
    {
        public static BattleGrowthValidationResult ValidateCanonical(
            BattleGrowthSnapshot snapshot)
        {
            if (snapshot == null)
                return Fail(BattleGrowthValidationCode.SnapshotRequired,
                    "growthSnapshot", "Battle growth snapshot is required.");
            if (string.IsNullOrWhiteSpace(snapshot.ProfileId)
                || snapshot.ProfileRevision < 0
                || string.IsNullOrWhiteSpace(snapshot.LevelId)
                || string.IsNullOrWhiteSpace(snapshot.BattleContentVersion)
                || string.IsNullOrWhiteSpace(snapshot.PolicyId)
                || string.IsNullOrWhiteSpace(snapshot.OutgameCatalogId)
                || string.IsNullOrWhiteSpace(snapshot.OutgameContentVersion)
                || !Sha256(snapshot.OutgameContentFingerprint))
                return Fail(BattleGrowthValidationCode.IdentityInvalid,
                    "growthSnapshot.identity", "Growth snapshot identity is incomplete.");

            string previousRecordKey = null;
            foreach (var record in snapshot.SourceRecords)
            {
                if (record == null || string.IsNullOrWhiteSpace(record.SourceId)
                    || string.IsNullOrWhiteSpace(record.DomainId) || record.Rank < 0
                    || !BattleGrowthResolver.TryResolveAttribute(record.AttributeId, out _)
                    || !BattleGrowthResolver.TryResolveOperation(record.OperationId, out _)
                    || !Finite(record.AuthoredValue) || record.AuthoredValue <= 0f
                    || !Finite(record.AppliedValue) || record.AppliedValue < 0f
                    || (record.Disposition == BattleGrowthSourceDisposition.Applied
                        && (record.AppliedValue <= 0f
                            || (record.Reason != BattleGrowthSourceReason.Applied
                                && record.Reason != BattleGrowthSourceReason.AppliedAtCap)))
                    || (record.Disposition == BattleGrowthSourceDisposition.Suppressed
                        && (record.AppliedValue != 0f
                            || record.Reason == BattleGrowthSourceReason.Applied
                            || record.Reason == BattleGrowthSourceReason.AppliedAtCap)))
                    return Fail(BattleGrowthValidationCode.RecordInvalid,
                        "growthSnapshot.sourceRecords", "A growth source record is invalid.");
                var key = RecordOrderKey(record);
                if (previousRecordKey != null
                    && StringComparer.Ordinal.Compare(previousRecordKey, key) > 0)
                    return Fail(BattleGrowthValidationCode.RecordOrderInvalid,
                        "growthSnapshot.sourceRecords",
                        "Growth source records are not in canonical ordinal order.");
                previousRecordKey = key;
            }

            string previousAttribute = null;
            foreach (var modifier in snapshot.AggregateModifiers)
            {
                if (modifier == null
                    || !BattleGrowthResolver.TryResolveAttribute(
                        modifier.AttributeId, out _)
                    || !Finite(modifier.Flat) || !Finite(modifier.Additive)
                    || !Finite(modifier.Multiplicative)
                    || modifier.Multiplicative <= 0f)
                    return Fail(BattleGrowthValidationCode.AggregateInvalid,
                        "growthSnapshot.aggregateModifiers",
                        "A growth aggregate modifier is invalid.");
                if (previousAttribute != null
                    && StringComparer.Ordinal.Compare(previousAttribute,
                        modifier.AttributeId) >= 0)
                    return Fail(BattleGrowthValidationCode.AggregateOrderInvalid,
                        "growthSnapshot.aggregateModifiers",
                        "Growth aggregates are not unique canonical ordinal records.");
                previousAttribute = modifier.AttributeId;
            }

            var rebuilt = BuildExpectedAggregates(snapshot.SourceRecords);
            if (rebuilt.Length != snapshot.AggregateModifiers.Count)
                return Fail(BattleGrowthValidationCode.AggregateInvalid,
                    "growthSnapshot.aggregateModifiers",
                    "Growth aggregates do not match applied source records.");
            for (var index = 0; index < rebuilt.Length; index++)
            {
                var actual = snapshot.AggregateModifiers[index];
                var expected = rebuilt[index];
                if (!string.Equals(actual.AttributeId, expected.AttributeId,
                        StringComparison.Ordinal)
                    || actual.Flat != expected.Flat
                    || actual.Additive != expected.Additive
                    || actual.Multiplicative != expected.Multiplicative)
                    return Fail(BattleGrowthValidationCode.AggregateInvalid,
                        "growthSnapshot.aggregateModifiers[" + index + "]",
                        "Growth aggregate values do not match source records.");
            }

            var fingerprint = BattleGrowthFingerprint.Compute(snapshot.ProfileId,
                snapshot.ProfileRevision, snapshot.LevelId,
                snapshot.BattleContentVersion, snapshot.PolicyId,
                snapshot.OutgameCatalogId, snapshot.OutgameContentVersion,
                snapshot.OutgameContentFingerprint, snapshot.SourceRecords,
                snapshot.AggregateModifiers);
            if (!string.Equals(snapshot.Fingerprint, fingerprint,
                    StringComparison.Ordinal))
                return Fail(BattleGrowthValidationCode.FingerprintMismatch,
                    "growthSnapshot.fingerprint",
                    "Growth snapshot fingerprint is not canonical.");
            return BattleGrowthValidationResult.Ok();
        }

        public static BattleGrowthValidationResult ValidateForLaunch(
            BattleGrowthSnapshot snapshot, ResolvedLevelDefinition level,
            CompiledOutgameContentCatalog outgameCatalog)
        {
            var levelValidation = ValidateForResolvedLevel(snapshot, level);
            if (!levelValidation.Succeeded) return levelValidation;
            if (outgameCatalog == null)
                return Fail(BattleGrowthValidationCode.OutgameCatalogMismatch,
                    "outgameCatalog", "Compiled outgame content is required for launch validation.");
            if (!string.Equals(snapshot.OutgameCatalogId,
                    outgameCatalog.Header.catalogId, StringComparison.Ordinal))
                return Fail(BattleGrowthValidationCode.OutgameCatalogMismatch,
                    "growthSnapshot.outgameCatalogId",
                    "Growth snapshot catalog does not match current compiled outgame content.");
            if (!string.Equals(snapshot.OutgameContentVersion,
                    outgameCatalog.Header.contentVersion, StringComparison.Ordinal))
                return Fail(BattleGrowthValidationCode.OutgameContentVersionMismatch,
                    "growthSnapshot.outgameContentVersion",
                    "Growth snapshot content version does not match current compiled outgame content.");
            if (!string.Equals(snapshot.OutgameContentFingerprint,
                    outgameCatalog.Fingerprint, StringComparison.Ordinal))
                return Fail(BattleGrowthValidationCode.OutgameContentFingerprintMismatch,
                    "growthSnapshot.outgameContentFingerprint",
                    "Growth snapshot fingerprint does not match current compiled outgame content.");
            return BattleGrowthValidationResult.Ok();
        }

        internal static BattleGrowthValidationResult ValidateForResolvedLevel(
            BattleGrowthSnapshot snapshot, ResolvedLevelDefinition level)
        {
            var canonical = ValidateCanonical(snapshot);
            if (!canonical.Succeeded) return canonical;
            if (level == null)
                return Fail(BattleGrowthValidationCode.LevelMismatch,
                    "level", "Resolved level is required for growth validation.");
            if (!string.Equals(snapshot.LevelId, level.Identity.LevelId,
                    StringComparison.Ordinal))
                return Fail(BattleGrowthValidationCode.LevelMismatch,
                    "growthSnapshot.levelId",
                    "Growth snapshot level does not match the resolved level.");
            if (!string.Equals(snapshot.PolicyId, level.Identity.GrowthPolicyId,
                    StringComparison.Ordinal))
                return Fail(BattleGrowthValidationCode.PolicyMismatch,
                    "growthSnapshot.policyId",
                    "Growth snapshot policy does not match the resolved level.");
            if (!string.Equals(snapshot.BattleContentVersion,
                    level.BattleContent.Header.contentVersion,
                    StringComparison.Ordinal))
                return Fail(BattleGrowthValidationCode.BattleContentMismatch,
                    "growthSnapshot.battleContentVersion",
                    "Growth snapshot battle content does not match the resolved level.");
            return BattleGrowthValidationResult.Ok();
        }

        private static BattleGrowthAggregateModifier[] BuildExpectedAggregates(
            IReadOnlyList<BattleGrowthSourceRecord> records)
        {
            var values = new List<BattleGrowthAggregateModifier>();
            foreach (var group in records.Where(value => value != null
                             && value.Disposition == BattleGrowthSourceDisposition.Applied)
                         .GroupBy(value => value.AttributeId, StringComparer.Ordinal)
                         .OrderBy(value => value.Key, StringComparer.Ordinal))
            {
                var flat = 0f;
                var additive = 0f;
                var multiplicative = 1f;
                foreach (var record in group)
                {
                    BattleGrowthResolver.TryResolveOperation(record.OperationId,
                        out var operation);
                    if (operation == CombatModifierOperation.Flat)
                        flat += record.AppliedValue;
                    else if (operation == CombatModifierOperation.Additive)
                        additive += record.AppliedValue;
                    else multiplicative *= record.AppliedValue;
                }
                values.Add(new BattleGrowthAggregateModifier(group.Key, flat,
                    additive, multiplicative));
            }
            return values.ToArray();
        }

        private static string RecordOrderKey(BattleGrowthSourceRecord record)
        {
            return record.SourceId + "\n" + record.AttributeId + "\n"
                + record.OperationId;
        }

        private static bool Sha256(string value)
        {
            if (value == null || value.Length != 64) return false;
            for (var index = 0; index < value.Length; index++)
            {
                var character = value[index];
                if (!((character >= '0' && character <= '9')
                    || (character >= 'a' && character <= 'f'))) return false;
            }
            return true;
        }

        private static bool Finite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static BattleGrowthValidationResult Fail(
            BattleGrowthValidationCode code, string path, string message)
        {
            return new BattleGrowthValidationResult(code, path, message);
        }
    }

    public static class BattleGrowthRuntime
    {
        // Canonical baseline order: authored base -> flat -> additive -> multiplicative.
        // The caller then applies battle-owned permanent rules and transient statuses.
        public static float ApplyBaseline(BattleGrowthSnapshot snapshot,
            CombatAttributeKind attribute, float authoredBase)
        {
            if (snapshot == null) return authoredBase;
            return snapshot.TryGetAggregate(BattleGrowthResolver.AttributeId(attribute),
                out var modifier)
                ? modifier.Apply(authoredBase)
                : authoredBase;
        }
    }

    internal static class BattleGrowthFingerprint
    {
        public static string Compute(string profileId, long profileRevision,
            string levelId, string battleContentVersion, string policyId,
            string outgameCatalogId, string outgameContentVersion,
            string outgameContentFingerprint,
            IEnumerable<BattleGrowthSourceRecord> records,
            IEnumerable<BattleGrowthAggregateModifier> aggregates)
        {
            var projection = new StringBuilder();
            Add(projection, "profileId", profileId);
            Add(projection, "profileRevision", profileRevision);
            Add(projection, "levelId", levelId);
            Add(projection, "battleContentVersion", battleContentVersion);
            Add(projection, "policyId", policyId);
            Add(projection, "outgameCatalogId", outgameCatalogId);
            Add(projection, "outgameContentVersion", outgameContentVersion);
            Add(projection, "outgameContentFingerprint", outgameContentFingerprint);
            var sourceValues = (records ?? Enumerable.Empty<BattleGrowthSourceRecord>())
                .ToArray();
            Add(projection, "sourceCount", sourceValues.Length);
            for (var index = 0; index < sourceValues.Length; index++)
            {
                var value = sourceValues[index];
                var prefix = "source[" + index.ToString(CultureInfo.InvariantCulture) + "]";
                Add(projection, prefix + ".sourceId", value.SourceId);
                Add(projection, prefix + ".domainId", value.DomainId);
                Add(projection, prefix + ".rank", value.Rank);
                Add(projection, prefix + ".attributeId", value.AttributeId);
                Add(projection, prefix + ".operationId", value.OperationId);
                Add(projection, prefix + ".authoredValue", value.AuthoredValue);
                Add(projection, prefix + ".appliedValue", value.AppliedValue);
                Add(projection, prefix + ".disposition", (int)value.Disposition);
                Add(projection, prefix + ".reason", (int)value.Reason);
            }
            var aggregateValues = (aggregates
                ?? Enumerable.Empty<BattleGrowthAggregateModifier>()).ToArray();
            Add(projection, "aggregateCount", aggregateValues.Length);
            for (var index = 0; index < aggregateValues.Length; index++)
            {
                var value = aggregateValues[index];
                var prefix = "aggregate[" + index.ToString(
                    CultureInfo.InvariantCulture) + "]";
                Add(projection, prefix + ".attributeId", value.AttributeId);
                Add(projection, prefix + ".flat", value.Flat);
                Add(projection, prefix + ".additive", value.Additive);
                Add(projection, prefix + ".multiplicative", value.Multiplicative);
            }
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(projection.ToString()));
                var result = new StringBuilder(bytes.Length * 2);
                foreach (var value in bytes)
                    result.Append(value.ToString("x2", CultureInfo.InvariantCulture));
                return result.ToString();
            }
        }

        private static void Add(StringBuilder projection, string key, object value)
        {
            var text = value is float number
                ? number.ToString("R", CultureInfo.InvariantCulture)
                : value is IFormattable formattable
                    ? formattable.ToString(null, CultureInfo.InvariantCulture)
                    : value == null ? string.Empty : value.ToString();
            projection.Append(key.Length.ToString(CultureInfo.InvariantCulture))
                .Append(':').Append(key).Append('=')
                .Append(text.Length.ToString(CultureInfo.InvariantCulture))
                .Append(':').Append(text).Append('\n');
        }
    }
}
