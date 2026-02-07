using Microsoft.EntityFrameworkCore;
using Orleans.Hosting;

namespace Orleans.Providers.EntityFramework.Extensions;

public static class GrainStorageSiloHostBuilderExtensions
{
    public static ISiloBuilder AddEfGrainStorageAsDefault<TContext>(this ISiloBuilder builder)
        where TContext : DbContext
        => builder.AddEfGrainStorage<TContext>(StorageProviderConstants.DefaultStorageProviderName);

    public static ISiloBuilder AddEfGrainStorage<TContext>(this ISiloBuilder builder,
        string providerName)
        where TContext : DbContext
        => builder.ConfigureServices(services => services.AddEfGrainStorage<TContext>(providerName));
}