using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Orleans.Providers.EntityFramework.Conventions;
using Orleans.Runtime;
using Orleans.Storage;

namespace Orleans.Providers.EntityFramework.Extensions;

public static class GrainStorageServiceCollectionExtensions
{
    public static IServiceCollection ConfigureGrainStorageOptions<TContext, TState, TEntity>(
        this IServiceCollection services,
        Action<GrainStorageOptions<TContext, TState, TEntity>>? configureOptions = null,
        string? stateName = null)
        where TContext : DbContext
        where TState : class
        where TEntity : class
    {
        string optionsName = stateName ?? typeof(TState).FullName!;

        return services
            .AddSingleton<IPostConfigureOptions<GrainStorageOptions<TContext, TState, TEntity>>,
                GrainStoragePostConfigureOptions<TContext, TState, TEntity>>()
            .Configure<GrainStorageOptions<TContext, TState, TEntity>>(optionsName, options =>
            {
                configureOptions?.Invoke(options);
            });
    }

    public static IServiceCollection ConfigureGrainStorageOptions<TContext, TState>(
        this IServiceCollection services,
        Action<GrainStorageOptions<TContext, TState, TState>>? configureOptions = null,
        string? stateName = null)
        where TContext : DbContext
        where TState : class
        => services.ConfigureGrainStorageOptions<TContext, TState, TState>(configureOptions, stateName);

    public static IServiceCollection AddEfGrainStorage<TContext>(
        this IServiceCollection services,
        string providerName = StorageProviderConstants.DefaultStorageProviderName)
        where TContext : DbContext
    {
        services.TryAddSingleton(typeof(IEntityTypeResolver), typeof(EntityTypeResolver));
        services.TryAddSingleton(typeof(IGrainStorageConvention), typeof(GrainStorageConvention));
        services.TryAddSingleton(typeof(IGrainStateEntryConfigurator<,>),
            typeof(DefaultGrainStateEntryConfigurator<,>));
        services.AddSingleton(typeof(EntityFrameworkGrainStorage<TContext>));

        services.TryAddSingleton<IGrainStorage>(sp =>
            sp.GetRequiredKeyedService<IGrainStorage>(StorageProviderConstants.DefaultStorageProviderName));

        services.AddKeyedSingleton<IGrainStorage>(providerName,
            (sp, _) => sp.GetRequiredService<EntityFrameworkGrainStorage<TContext>>());

        return services;
    }
}