namespace Core.Domain;

/// <summary>
/// Represents an entity
/// </summary>
/// <typeparam name="TId"></typeparam>
public interface IEntity<out TId>
{
    /// <summary>
    /// Gets the entity identifier
    /// </summary>
    TId Id { get; }
}