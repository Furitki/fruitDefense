using System;

namespace FruitDefense.Core
{
    public enum BattleSnapshotExportCode
    {
        Success,
        UnsupportedSessionSource,
    }

    public readonly struct BattleSnapshotExportResult
    {
        public BattleSnapshotExportCode Code { get; }
        public BattleSnapshot Snapshot { get; }
        public string Path { get; }
        public string Message { get; }
        public bool Succeeded
        {
            get { return Code == BattleSnapshotExportCode.Success && Snapshot != null; }
        }

        private BattleSnapshotExportResult(BattleSnapshotExportCode code,
            BattleSnapshot snapshot, string path, string message)
        {
            Code = code;
            Snapshot = snapshot;
            Path = path ?? string.Empty;
            Message = message ?? string.Empty;
        }

        public static BattleSnapshotExportResult Success(BattleSnapshot snapshot)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            return new BattleSnapshotExportResult(BattleSnapshotExportCode.Success,
                snapshot, string.Empty, string.Empty);
        }

        public static BattleSnapshotExportResult Unsupported(string message)
        {
            return new BattleSnapshotExportResult(
                BattleSnapshotExportCode.UnsupportedSessionSource, null,
                "session.source", message);
        }

        public override string ToString()
        {
            return Succeeded ? "Success" : Code + " at " + Path + ": " + Message;
        }
    }

    public enum BattleSnapshotRestoreCode
    {
        Success,
        InvalidPayload,
        MissingRequiredField,
        UnsupportedSchema,
        UnsupportedSessionSource,
        SourceCatalogUnavailable,
        IncompatibleSource,
        UnknownDefinition,
        InvalidReference,
        InvalidNumericValue,
        InvalidIdentity,
    }

    public readonly struct BattleSnapshotRestoreResult
    {
        public BattleSnapshotRestoreCode Code { get; }
        public string Path { get; }
        public string Message { get; }
        public bool Succeeded { get { return Code == BattleSnapshotRestoreCode.Success; } }

        public BattleSnapshotRestoreResult(BattleSnapshotRestoreCode code, string path, string message)
        {
            Code = code;
            Path = path ?? string.Empty;
            Message = message ?? string.Empty;
        }

        public static BattleSnapshotRestoreResult Ok()
        {
            return new BattleSnapshotRestoreResult(BattleSnapshotRestoreCode.Success,
                string.Empty, string.Empty);
        }

        public override string ToString()
        {
            return Succeeded ? "Success" : Code + " at " + Path + ": " + Message;
        }
    }
}
