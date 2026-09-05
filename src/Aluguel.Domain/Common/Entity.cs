namespace Aluguel.Domain.Common;

/// <summary>Interfaces e tipos base do domínio (Clean Architecture / DDD).</summary>
public interface IDomainEvent { }

public abstract class Entity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();

    private readonly List<IDomainEvent> _domainEvents = new();
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void Raise(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);
    public void ClearDomainEvents() => _domainEvents.Clear();
}

public abstract class AggregateRoot : Entity;

public interface ITenantOwned { Guid TenantId { get; } }

public interface ISoftDeletable { DateTimeOffset? DeletedAt { get; } }

public interface IAuditable
{
    DateTimeOffset CreatedAt { get; }
    DateTimeOffset UpdatedAt { get; }
}
