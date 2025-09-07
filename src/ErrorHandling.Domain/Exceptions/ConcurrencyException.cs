namespace ErrorHandling.Domain.Exceptions;

public class ConcurrencyException : DomainException
{
    public string EntityType { get; }
    public string EntityId { get; }
    public string ExpectedVersion { get; }
    public string ActualVersion { get; }

    public ConcurrencyException(
        string entityType,
        string entityId,
        string expectedVersion,
        string actualVersion
    )
        : base(
            $"Concurrency conflict on {entityType} '{entityId}'. Expected version: {expectedVersion}, Actual: {actualVersion}",
            "CONCURRENCY_CONFLICT",
            409
        )
    {
        EntityType = entityType;
        EntityId = entityId;
        ExpectedVersion = expectedVersion;
        ActualVersion = actualVersion;

        WithExtension("entityType", EntityType);
        WithExtension("entityId", EntityId);
        WithExtension("expectedVersion", ExpectedVersion);
        WithExtension("actualVersion", ActualVersion);
    }

    protected override string GetTitle() => "Concurrency Conflict";
}

public class OptimisticLockException : ConcurrencyException
{
    public OptimisticLockException(
        string entityType,
        string entityId,
        int expectedVersion,
        int actualVersion
    )
        : base(entityType, entityId, expectedVersion.ToString(), actualVersion.ToString()) { }
}
