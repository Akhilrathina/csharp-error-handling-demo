namespace ErrorHandling.Domain.Exceptions;

public class EntityNotFoundException : DomainException
{
    public string EntityName { get; }
    public object EntityId { get; }

    public EntityNotFoundException(string entityName, object entityId)
        : base($"{entityName} with id '{entityId}' was not found", "ENTITY_NOT_FOUND", 404)
    {
        EntityName = entityName;
        EntityId = entityId;
        WithExtension("entityName", EntityName);
        WithExtension("entityId", EntityId);
    }

    public EntityNotFoundException(Type entityType, object entityId)
        : this(entityType.Name, entityId) { }

    protected override string GetTitle() => "Entity Not Found";
}

public class AggregateNotFoundException : DomainException
{
    public string AggregateType { get; }
    public string AggregateId { get; }

    public AggregateNotFoundException(Type aggregateType, string aggregateId)
        : base(
            $"{aggregateType.Name} with ID '{aggregateId}' was not found",
            "AGGREGATE_NOT_FOUND",
            404
        )
    {
        AggregateType = aggregateType.Name;
        AggregateId = aggregateId;
        WithExtension("aggregateType", AggregateType);
        WithExtension("aggregateId", AggregateId);
    }

    protected override string GetTitle() => "Aggregate Not Found";
}
