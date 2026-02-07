using Microsoft.EntityFrameworkCore;

namespace Orleans.Providers.EntityFramework;

public interface IGrainStateEntryConfigurator<TContext, TEntity>
    where TContext : DbContext
    where TEntity : class
{
    void ConfigureSaveEntry(ConfigureSaveEntryContext<TContext, TEntity> context);
}