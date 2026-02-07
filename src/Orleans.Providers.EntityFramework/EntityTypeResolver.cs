namespace Orleans.Providers.EntityFramework;

/// <summary>
/// Default implementation of <see cref="IEntityTypeResolver"/> that assumes the state type is the entity type.
/// </summary>
public class EntityTypeResolver : IEntityTypeResolver
{
    /// <inheritdoc />
    public virtual Type ResolveEntityType(Type stateType)
        => ResolveStateType(stateType);

    /// <inheritdoc />
    public virtual Type ResolveStateType(Type stateType)
    {
        ArgumentNullException.ThrowIfNull(stateType);
        return stateType;
    }
}