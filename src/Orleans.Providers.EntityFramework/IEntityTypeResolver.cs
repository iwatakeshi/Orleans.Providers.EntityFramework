namespace Orleans.Providers.EntityFramework;

/// <summary>
/// Resolves the entity type and state type mapping.
/// </summary>
public interface IEntityTypeResolver
{
    /// <summary>
    /// Resolves the storage entity type for a given grain state type.
    /// </summary>
    /// <param name="stateType">The grain state type.</param>
    /// <returns>The entity type to be stored in the database.</returns>
    Type ResolveEntityType(Type stateType);

    /// <summary>
    /// Resolves the grain state type (validates or maps input).
    /// </summary>
    /// <param name="stateType">The grain state type.</param>
    /// <returns>The resolved grain state type.</returns>
    Type ResolveStateType(Type stateType);
}