using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Orleans.Providers.EntityFramework;

/// <summary>
/// Default implementation of <see cref="IGrainStateEntryConfigurator{TContext, TEntity}"/>.
/// Sets the entity state to Modified or Added based on IsPersisted flag.
/// </summary>
/// <typeparam name="TContext">The DbContext type.</typeparam>
/// <typeparam name="TEntity">The entity type.</typeparam>
public class DefaultGrainStateEntryConfigurator<TContext, TEntity>
    : IGrainStateEntryConfigurator<TContext, TEntity>
    where TContext : DbContext
    where TEntity : class
{
    /// <inheritdoc />
    public void ConfigureSaveEntry(ConfigureSaveEntryContext<TContext, TEntity> context)
    {
        EntityEntry<TEntity> entry = context.DbContext.Entry(context.Entity);

        entry.State = context.IsPersisted
            ? EntityState.Modified
            : EntityState.Added;
    }
}