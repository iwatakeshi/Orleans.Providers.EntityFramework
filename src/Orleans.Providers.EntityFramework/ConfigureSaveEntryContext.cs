namespace Orleans.Providers.EntityFramework;

/// <summary>
/// Context used to configure the entry state before saving changes.
/// </summary>
/// <typeparam name="TContext">The DbContext type.</typeparam>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <param name="dbContext">The DbContext instance.</param>
/// <param name="entity">The entity being saved.</param>
public class ConfigureSaveEntryContext<TContext, TEntity>(TContext dbContext, TEntity entity)
{
    /// <summary>
    /// Gets the DbContext.
    /// </summary>
    public TContext DbContext { get; } = dbContext;

    /// <summary>
    /// Gets the entity being saved.
    /// </summary>
    public TEntity Entity { get; } = entity;

    /// <summary>
    /// Gets or sets a boolean indicating if the entity is already persisted (exists in database).
    /// </summary>
    public bool IsPersisted { get; set; }
}