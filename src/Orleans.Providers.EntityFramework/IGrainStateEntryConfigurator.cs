using Microsoft.EntityFrameworkCore;

namespace Orleans.Providers.EntityFramework;

/// <summary>
/// Configures the entity entry state before saving changes.
/// </summary>
/// <typeparam name="TContext">The DbContext type.</typeparam>
/// <typeparam name="TEntity">The entity type.</typeparam>
public interface IGrainStateEntryConfigurator<TContext, TEntity>
    where TContext : DbContext
    where TEntity : class
{
    /// <summary>
    /// Configures the entry state using the provided context.
    /// </summary>
    /// <param name="context">The configuration context.</param>
    void ConfigureSaveEntry(ConfigureSaveEntryContext<TContext, TEntity> context);
}