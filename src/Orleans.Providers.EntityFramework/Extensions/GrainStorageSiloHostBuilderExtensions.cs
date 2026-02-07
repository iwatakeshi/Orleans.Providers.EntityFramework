using Microsoft.EntityFrameworkCore;
using Orleans.Hosting;

namespace Orleans.Providers.EntityFramework.Extensions;

/// <summary>
/// Silo builder extensions for registering Entity Framework grain storage.
/// </summary>
public static class GrainStorageSiloHostBuilderExtensions
{
    /// <summary>
    /// Adds a default Entity Framework grain storage provider.
    /// </summary>
    /// <typeparam name="TContext">The DbContext type.</typeparam>
    /// <param name="builder">The silo builder.</param>
    /// <returns>The silo builder.</returns>
    public static ISiloBuilder AddEfGrainStorageAsDefault<TContext>(this ISiloBuilder builder)
        where TContext : DbContext
        => builder.AddEfGrainStorage<TContext>(StorageProviderConstants.DefaultStorageProviderName);

    /// <summary>
    /// Adds a named Entity Framework grain storage provider.
    /// </summary>
    /// <typeparam name="TContext">The DbContext type.</typeparam>
    /// <param name="builder">The silo builder.</param>
    /// <param name="providerName">The name of the storage provider.</param>
    /// <returns>The silo builder.</returns>
    public static ISiloBuilder AddEfGrainStorage<TContext>(this ISiloBuilder builder,
        string providerName)
        where TContext : DbContext
        => builder.ConfigureServices(services => services.AddEfGrainStorage<TContext>(providerName));
}