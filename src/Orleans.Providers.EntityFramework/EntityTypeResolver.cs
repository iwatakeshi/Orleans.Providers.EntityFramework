namespace Orleans.Providers.EntityFramework;

public class EntityTypeResolver : IEntityTypeResolver
{
    public virtual Type ResolveEntityType(Type stateType)
        => ResolveStateType(stateType);

    public virtual Type ResolveStateType(Type stateType)
    {
        ArgumentNullException.ThrowIfNull(stateType);
        return stateType;
    }
}